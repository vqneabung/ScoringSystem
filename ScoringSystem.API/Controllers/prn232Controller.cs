using Blog.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using ScoringSystem.API.Extensions;
using System.Diagnostics;

namespace ScoringSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Prn232Controller : ControllerBase
    {
        private readonly FileHelper _fileHelper;
        private readonly ProcessHelper _processHelper;
        private readonly RestClient _restClientOptions;

        public Prn232Controller(FileHelper fileHelper, ProcessHelper processHelper)
        {
            _fileHelper = fileHelper;
            _processHelper = processHelper;
            var restClientOptions = new RestClientOptions("https://localhost:5000");
            _restClient = new RestClient(restClientOptions);
        }

        [HttpPost]
        public async Task<IActionResult> UploadPrn232File(IFormFile file)
        {
            var processResult = await ProcessPrn232File(file);
            if (!processResult.Success)
            {
                return BadRequest("File processing failed.");
            }

            var projectPath = processResult.ProjectPath;

            //Run with https://localhost:5000
            var runResult = _processHelper.RunProcess("dotnet", $"run --project \"{FindProgramCsFolder(projectPath)}\" --urls \"https://localhost:5000\" ", () =>
            {
                // Thu chay khoan 5 giay roi return 
                Task.Delay(100000).Wait();
            }, true
            );


            //Template for further processing if needed
            return Ok("File processed successfully.");

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
            Directory.Delete(unzipPath, true);

            // Unzip and process the file here

            Console.WriteLine("Extracting file: " + filePath);
            Console.WriteLine("Extracted to: " + unzipPath);
            System.IO.Compression.ZipFile.ExtractToDirectory(filePath, unzipPath ?? string.Empty, overwriteFiles: true);

            var projectFolder = _fileHelper.FindProjectFolderWithSln(unzipPath!);


            //Check folder exists
            Console.WriteLine("Project folder: " + projectFolder);
            Console.WriteLine("Is this path existed: " + Path.Exists(projectFolder));

            Console.WriteLine("Process...");


            var restoreResult = _processHelper.RunProcess("dotnet", $"restore \"{projectFolder}\"");

            if (!restoreResult) return (false, projectFolder);

            var buildResult = _processHelper.RunProcess("dotnet", $"build \"{projectFolder}\"");

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

        [NonAction]
        public async Task<(bool, object)> HandleApiResponse<TRequest, TResponse>(Method method, string resource = "", TRequest? request = null, string? error = "Error")
            where TRequest : class
        {
            try
            {
                var client = _restClientOptions;

                try
                {
                    TResponse? response;

                    switch (method)
                    {
                        case Method.Get:

                            if (request != null)
                            {
                                Console.WriteLine("Warning: Only resource is necessary for GET method, request will be ignored.");
                                Console.WriteLine("Please use query parameters in the resource URL if needed.");
                                Console.WriteLine("Example: resource = \"endpoint?param1=value1&param2=value2\"");
                            }

                            response = await client.GetAsync<TResponse>(resource);
                            break;
                        case Method.Post:
                            response = await client.PostJsonAsync<TRequest, TResponse>(resource, request);
                            break;
                        case Method.Put:
                            response = await client.PutJsonAsync<TRequest, TResponse>(resource, request);
                            break;
                        case Method.Delete:
                            if (request != null)
                            {
                                Console.WriteLine("Request is not necessary for DELETE method, request will be ignored.");
                                Console.WriteLine("Please use query parameters in the resource URL if needed.");
                                Console.WriteLine("Example: resource = \"endpoint?param1=value1&param2=value2\"");
                            }

                            response = await client.DeleteAsync<TResponse>(resource);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    if (response == null)
                    {
                        return (false, new object());
                    }

                    return (true, response!);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(error + " " + ex.Message);
                }

                return (false, new object());
            }
            catch (Exception ex)
            {
                return (false, new object());
            }
        }


    }
}
