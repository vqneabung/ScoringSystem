using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScoringSystem.API.Extensions;
using System.Diagnostics;

namespace ScoringSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoresController : ControllerBase
    {
        private readonly FileHelper _fileHelper;

        public ScoresController(FileHelper fileHelper)
        {
            _fileHelper = fileHelper;
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
            var unzipPath = Path.GetDirectoryName(filePath) + "\\..\\..\\Project\\";
            // Unzip and process the file here
            Console.WriteLine("Extracting file: " + filePath);
            //System.IO.Compression.ZipFile.ExtractToDirectory(filePath, unzipPath ?? string.Empty);


            Console.WriteLine("Extracted to: " + unzipPath);
            //Check folder exists
            var projectFolder = unzipPath + folderNameFromFile;
            Console.WriteLine("Project folder: " + projectFolder);
            Console.WriteLine("Is this path existed: " + Path.Exists(projectFolder));

            //var psiRes = new ProcessStartInfo("dotnet", $"restore \"{filePath}\"")
            //{ RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            //var pres = Process.Start(psiRes);
            //pres?.WaitForExit();
            //if (pres?.ExitCode != 0) return BadRequest("Restore failed");

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
