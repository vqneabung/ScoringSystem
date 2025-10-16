namespace ScoringSystem.API.Extensions
{
    public class FileHelper
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FileHelper(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }


        public async Task<string> SaveFile(IFormFile file)
        {
            // Ensure the uploads directory exists
            var uploadsDir = Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }
            
            // Create the full file path
            var filePath = Path.Combine(uploadsDir, file.FileName);
            
            // Save the file
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);
            return filePath;
        }


    }
}
