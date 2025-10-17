using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScoringSystem.API.Extensions;
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

        public ScoresController(FileHelper fileHelper, ProcessHelper processHelper)
        {
            _fileHelper = fileHelper;
            _processHelper = processHelper;
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

            var restoreResult = _processHelper.RunProcess("dotnet", $"restore \"{projectFolder}\"");

            if (!restoreResult) return BadRequest("Restore failed");

            var buildResult = _processHelper.RunProcess("dotnet", $"build \"{projectFolder}\"");

            if (!buildResult) return BadRequest("Build Failed!");

            ////Dotnet run
            string[] webProjectFolderPaths = Directory.GetFiles(projectFolder, "program.cs", SearchOption.AllDirectories);
            if (webProjectFolderPaths.Length > 0)
            {
                string webProjectFolderPath = Path.GetDirectoryName(webProjectFolderPaths[0]!);
                var runResult = _processHelper.RunProcess("dotnet", "run --urls \"https://localhost:5000\" --launch-profile https .");
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

    }
}
