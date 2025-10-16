using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScoringSystem.API.Extensions;

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
        public IActionResult CalculateScore()
        {
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
