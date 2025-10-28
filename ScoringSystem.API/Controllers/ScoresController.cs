using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScoringSystem.API.Extensions;
using ScoringSystem.API.Test;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ScoringSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoresController : ControllerBase
    {
        private readonly FileHelper _fileHelper;
        private readonly ProcessHelper _processHelper;
        private readonly InteractWebsite _interactWebsite;

        public ScoresController(FileHelper fileHelper, ProcessHelper processHelper, InteractWebsite interactWebsite)
        {
            _fileHelper = fileHelper;
            _processHelper = processHelper;
            _interactWebsite = interactWebsite;
        }

        [HttpPost]
        public async Task<IActionResult> CalculateScore(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Use the FileHelper to save the file
            var filePath = await _fileHelper.SaveFile(file);

            // Unzip and process the file here
            System.IO.Compression.ZipFile.ExtractToDirectory(filePath, Path.GetDirectoryName(filePath) ?? string.Empty);


            return Ok();
        }

        [HttpGet("test/playwright")]
        public async Task<IActionResult> TestPlaywright()
        {
            await _interactWebsite.TestRun();

            return Ok();
        }

        [HttpGet("test/file")]
        public async Task<IActionResult> TestFile()
        {
            var fileName = "PE_PRN222_SU25_TrialTest_NguyenVQ.zip";
            var folderNameFromFile = Path.GetFileNameWithoutExtension(fileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "PE_PRN222_SU25_TrialTest_NguyenVQ.zip");
            var unzipPath = Path.GetDirectoryName(filePath) + "\\..\\..\\..\\Project\\";
            var projectFolder = unzipPath + folderNameFromFile;

            // Get all cs file except obj and bin
            var allCsFiles = Directory.GetFiles(projectFolder, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));

            // Get appsetting.json files except obj and bin
            var appSettingFile = Directory.GetFiles(projectFolder, "*appsettings.json", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));

            //Find file cs contain dbcontext and get class name
            var dbContextFiles = new List<string>();
            foreach (var csFile in allCsFiles)
            {
                var content = await System.IO.File.ReadAllTextAsync(csFile);
                if (content.Contains("DbContext"))
                {
                    dbContextFiles.Add(csFile);
                }
            }

            var dbContextClassNames = new List<string>();
            foreach (var className in dbContextFiles)
            {
                var content = await System.IO.File.ReadAllTextAsync(className);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("class") && line.Contains("DbContext"))
                    {
                        var parts = line.Trim().Split(' ');
                        var index = Array.IndexOf(parts, "class");
                        if (index >= 0 && index < parts.Length - 1)
                        {
                            dbContextClassNames.Add(parts[index + 1]);
                        }
                    }
                }
            }

            //Show cac folder trong project
            var projectFolders = Directory.GetDirectories(projectFolder, "*", SearchOption.TopDirectoryOnly);

            //Trong cac folder do, folder nao chua program.cs (Project chinh)
            var mainProjectFolders = new List<string>();
            foreach (var folder in projectFolders)
            {
                var programCsFiles = Directory.GetFiles(folder, "program.cs", SearchOption.AllDirectories);
                if (programCsFiles.Length > 0)
                {
                    mainProjectFolders.Add(folder);
                }
            }

            //Trong project chinh, neu co file cs nao chua dbcontext thi ghi lai de xac dinh la project co su dung dbcontext khong?
            var mainProjectDbContextFiles = new List<string>();
            foreach (var folder in mainProjectFolders)
            {
                var csFiles = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));
                foreach (var csFile in csFiles)
                {
                    var content = await System.IO.File.ReadAllTextAsync(csFile);
                    if (content.Contains(dbContextClassNames[0]))
                    {
                        mainProjectDbContextFiles.Add(csFile);
                    }
                }

                if (mainProjectDbContextFiles.Count > 0)
                {
                    break;
                }
            }

            return Ok(new
            {
                allCsFile = allCsFiles.ToList(),
                appSettingFile = appSettingFile.ToList(),
                dbContextFiles = dbContextFiles,
                dbContextClassNames = dbContextClassNames,
                projectFolders = projectFolders.ToList(),
                mainProjectDbContextFiles = mainProjectDbContextFiles
            });;
        }

        // Su dung file san de test project
        [HttpGet("test")]
        public async Task<IActionResult> TestCalculateScore()
        {
            var fileName = "PE_PRN222_SU25_TrialTest_NguyenVQ.zip";
            var folderNameFromFile = Path.GetFileNameWithoutExtension(fileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "PE_PRN222_SU25_TrialTest_NguyenVQ.zip");
            var unzipPath = Path.GetDirectoryName(filePath) + "\\..\\..\\..\\Project\\";
            var projectFolder = unzipPath + folderNameFromFile;
            // Unzip and process the file here

            if (!Path.Exists(projectFolder))
        {
           Console.WriteLine("Extracting file: " + filePath);
     Console.WriteLine("Extracted to: " + unzipPath);
           System.IO.Compression.ZipFile.ExtractToDirectory(filePath, unzipPath ?? string.Empty);
       }

            //Check folder exists
Console.WriteLine("Project folder: " + projectFolder);
 Console.WriteLine("Is this path existed: " + Path.Exists(projectFolder));

            Console.WriteLine("Process...");

var restoreResult = await _processHelper.RunProcess("dotnet", $"restore \"{projectFolder}\"");

            if (!restoreResult) return BadRequest("Restore failed");

            var buildResult = await _processHelper.RunProcess("dotnet", $"build \"{projectFolder}\"");

         if (!buildResult) return BadRequest("Build Failed!");

   ////Dotnet run
            string[] webProjectFolderPaths = Directory.GetFiles(projectFolder, "program.cs", SearchOption.AllDirectories);
            if (webProjectFolderPaths.Length > 0)
    {
       string webProjectFolderPath = Path.GetDirectoryName(webProjectFolderPaths[0]!) ?? "";
          var runResult = await _processHelper.RunProcess("dotnet",
         $"run --urls \"https://localhost:5000\" --launch-profile https --project {webProjectFolderPath}", async () => 
     await _interactWebsite.Run());

    }

      //Test open a browser
            //_processHelper.OpenUrlInBrowser("https://www.google.com/");

  return Ok();
        }

        [HttpPost("upload")]    
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Use the FileHelper to save the file
            var filePath = await _fileHelper.SaveFile(file);
            return Ok(new { file.FileName, file.Length });
        }

        [HttpGet("process/kill/test")]
        public async Task<IActionResult> KillProcessTest()
        {
       
            _processHelper.KillProcessByName("LionPetManagement_NguyenVQ");

            return Ok();
        }

    }
}
