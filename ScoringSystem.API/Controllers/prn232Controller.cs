using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using RestSharp;
using ScoringSystem.API.Dtos;
using ScoringSystem.API.Extensions;
using ScoringSystem.API.Services;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace ScoringSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Prn232Controller : ControllerBase
    {
        private readonly FileHelper _fileHelper;
        private readonly ProcessHelper _processHelper;
        private readonly ScoringService _scoringService;

        public Prn232Controller(FileHelper fileHelper, ProcessHelper processHelper, ScoringService scoringService)
        {
            _fileHelper = fileHelper;
            _processHelper = processHelper;
            _scoringService = scoringService;
        }

        [HttpPost]
        public async Task<IActionResult> ScoringPRN232([FromForm] IFormFile file, [FromForm] IFormFile sqlFile ,[FromForm] string? testCaseJson = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            try
            {
                var processResult = await ProcessPrn232File(file);
                if (!processResult.Success)
                {
                    return BadRequest(new { message = "File processing failed." });
                }

                var projectPath = processResult.ProjectPath;
                var programCsFolder = FindProgramCsFolder(projectPath);

                if (string.IsNullOrEmpty(programCsFolder))
                {
                    return BadRequest(new { message = "Could not find Program.cs in project." });
                }

                //Thay doi connection string trong appsettings.json
                ChangeConnectionString(projectPath, "Data Source=(local);Initial Catalog=SU25LeopardDB;User ID=sa;Password=1234567890;Trust Server Certificate=True");

                try
                {
                    DeleteDatabase("SU25LeopardDB");
                    CreateDatabase("SU25LeopardDB", sqlFile);

                } catch (Exception ex)
                {
                    return BadRequest(new { message = $"Database setup failed: {ex.Message}" });
                }

                ScoringResponse? scoringResult = null;
                Exception? processException = null;

                // Run with https://localhost:5000
                // Thực thi function chấm bài khi process đang chạy
                var runResult = await _processHelper.RunProcess(
                    "dotnet",
                    $"run --project \"{programCsFolder}\" --urls \"https://localhost:5000\"",
                    async () =>
                    {
                        try
                        {
                            // Wait a bit for server to start
                            await Task.Delay(3000);

                            // Parse test cases from query parameter or use default
                            var testCases = GetTestCasesForPRN232(testCaseJson);

                            // Create test case request with student's API URL
                            var testCaseRequest = new TestCaseRequest
                            {
                                BaseUrl = "https://localhost:5000/api",
                                TestCases = testCases
                            };

                            // Score the test cases
                            scoringResult = await _scoringService.ScoreTestCasesAsync(testCaseRequest);

                            Console.WriteLine($"Scoring completed: {scoringResult.Score}%");
                        }
                        catch (Exception ex)
                        {
                            processException = ex;
                            Console.WriteLine($"Error during scoring: {ex.Message}");
                        }
                    },
                    true
                );

                // Check if there was an exception during scoring
                if (processException != null)
                {
                    return StatusCode(500, new { message = $"Error during scoring: {processException.Message}" });
                }

                // Return scoring result
                if (scoringResult != null)
                {
                    return Ok(new
                    {
                        success = scoringResult.Success,
                        score = scoringResult.Score,
                        totalTests = scoringResult.TotalTests,
                        passedTests = scoringResult.PassedTests,
                        failedTests = scoringResult.FailedTests,
                        message = scoringResult.Message,
                        results = scoringResult.Results
                    });
                }
                return StatusCode(500, new { message = "Scoring process did not complete." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get default test cases for PRN232
        /// Can be overridden by passing testCaseJson parameter
        /// </summary>
        [NonAction]
        private List<Request> GetTestCasesForPRN232(string? testCaseJson = null)
        {
            // If custom test cases provided, parse them
            if (!string.IsNullOrEmpty(testCaseJson))
            {
                try
                {
                    var testCases = JsonSerializer.Deserialize<List<Request>>(testCaseJson);
                    if (testCases != null)
                        return testCases;
                }
                catch
                {
                    Console.WriteLine("Failed to parse custom test cases, using defaults");
                }
            }

            // Default test cases for PRN232 (Tasks 2.1-2.4)
            return new List<Request>
           {
                                // 2.1 - Authentication & Authorization
                      new Request
                           {
                            Name = "2.1.1 - Login with valid credentials",
                            Url = "/auth",
                            Method = "POST",
                            Special = "login",
                            RequestBody = new Dictionary<string, object> { { "email", "administrator@leopard.com" }, { "password", "@1" } },
                            ExpectedStatusCode = 200
                   }
                            };
        }

        //Gom code xu ly file o day
        public async Task<(bool Success, string ProjectPath)> ProcessPrn232File(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "");

            var fileName = file.FileName;
            var folderNameFromFile = Path.GetFileNameWithoutExtension(fileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", fileName);
            //Save file to Uploads folder
            await _fileHelper.SaveFile(file);

            var unzipPath = Path.GetDirectoryName(filePath) + "\\..\\..\\..\\Project\\Solution";
            //Delete file and foleder in Solution folder if exists
            if (Directory.Exists(unzipPath))
            {
                Directory.Delete(unzipPath, true);
            }

            // Unzip and process the file here

            Console.WriteLine("Extracting file: " + filePath);
            Console.WriteLine("Extracted to: " + unzipPath);
            System.IO.Compression.ZipFile.ExtractToDirectory(filePath, unzipPath ?? string.Empty, overwriteFiles: true);

            var projectFolder = _fileHelper.FindProjectFolderWithSln(unzipPath!);


            //Check folder exists
            Console.WriteLine("Project folder: " + projectFolder);
            Console.WriteLine("Is this path existed: " + Path.Exists(projectFolder));

            Console.WriteLine("Process...");


            var restoreResult = await _processHelper.RunProcess("dotnet", $"restore \"{projectFolder}\"");

            if (!restoreResult) return (false, projectFolder);

            var buildResult = await _processHelper.RunProcess("dotnet", $"build \"{projectFolder}\"");

            if (!buildResult) return (false, projectFolder);

            return (true, projectFolder);
        }

        [NonAction]
        //Tim thu muc chua file program.cs
        private string FindProgramCsFolder(string rootPath)
        {
            var directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
            foreach (var dir in directories)
            {
                var programCsPath = Path.Combine(dir, "Program.cs");
                if (System.IO.File.Exists(programCsPath))
                {
                    return dir;
                }
            }
            return string.Empty;
        }

        //Change ConnectionString in appsettings.json
        [NonAction]
        private void ChangeConnectionString(string projectPath, string newConnectionString)
        {
            //Tim file appsettings.json
            var appSettingsPath = _fileHelper.FindAppSettingsJson(projectPath);

            var json = System.IO.File.ReadAllText(appSettingsPath!);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json)!;
            //Neu trong connection string co ten khac DefaultConnect thi tim key do va sua value
            if (jsonObj.ConnectionStrings != null)
            {
                foreach (var key in jsonObj.ConnectionStrings)
                {
                    if (key.Name != "DefaultConnection")
                    {
                        jsonObj.ConnectionStrings[key.Name] = newConnectionString;
                    }
                    else
                    {
                        jsonObj.ConnectionStrings.DefaultConnection = newConnectionString;
                    }
                }
            }

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(appSettingsPath, output);

        }

        private void DeleteDatabase(string databaseName)
        {
            var connectionString = "Data Source=(local);Initial Catalog=master;User ID=sa;Password=1234567890;Trust Server Certificate=True";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $@"
                    IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
                    BEGIN
                        ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{databaseName}];
                    END";
                command.ExecuteNonQuery();
            }
        }

        private void CreateDatabase(string databaseName, IFormFile sqlFile)
        {
            // 1️⃣ Đọc nội dung file .sql
            string sqlScript;
            using (var reader = new StreamReader(sqlFile.OpenReadStream()))
            {
                sqlScript = reader.ReadToEnd();
            }

            // 2️⃣ Kết nối tới master (vì DB chưa tồn tại)
            var masterConnectionString = "Data Source=(local);Initial Catalog=master;User ID=sa;Password=1234567890;Trust Server Certificate=True";

            using (var connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();

                // 3️⃣ Nếu DB đã tồn tại thì xóa (tùy nhu cầu, có thể bỏ)
                using (var dropCmd = new SqlCommand($@"
            IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END", connection))
                {
                    dropCmd.ExecuteNonQuery();
                }

                // 4️⃣ Tạo DB mới
                using (var createCmd = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection))
                {
                    createCmd.ExecuteNonQuery();
                }

                // 5️⃣ Chuyển sang DB mới
                connection.ChangeDatabase(databaseName);

                // 6️⃣ Xử lý file SQL: chia các batch theo "GO"
                var batches = Regex.Split(sqlScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch))
                        continue;

                    using (var cmd = new SqlCommand(batch, connection))
                    {
                        cmd.CommandTimeout = 0; // tránh timeout với script lớn
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            Console.WriteLine($"✅ Database [{databaseName}] đã được tạo thành công từ file SQL!");
        }
    }
}
