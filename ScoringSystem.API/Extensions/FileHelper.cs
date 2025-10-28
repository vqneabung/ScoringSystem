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

        public string FindProjectFolderWithSln(string rootPath)
        {
            try
            {
                var directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
                foreach (var dir in directories)
                {
                    var slnFiles = Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly);
                    if (slnFiles.Length > 0)
                    {
                        return dir;
                    }
                }
                return rootPath; // Fallback to rootPath if no sln found
            } catch
            {
                return string.Empty;
            }
        }

        //Tim vi tri appsettings.json
        public string? FindAppSettingsJson(string rootPath)
        {
            try
            {
                var appSettingsFiles = Directory.GetFiles(rootPath, "appsettings.json", SearchOption.AllDirectories);
                return appSettingsFiles.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

    }
}
