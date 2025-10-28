using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public Prn232Controller(FileHelper fileHelper, ProcessHelper processHelper)
        {
            _fileHelper = fileHelper;
            _processHelper = processHelper;
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

            //Run
            var runResult = _processHelper.RunProcess("dotnet", $"run --project \"{FindProgramCsFolder(projectPath)}\"", () =>
            {
                // Thu chay khoan 5 giay roi return 
                Task.Delay(5000).Wait();
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

    }
}
