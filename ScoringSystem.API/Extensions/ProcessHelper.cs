using System.Diagnostics;

namespace ScoringSystem.API.Extensions
{
    public class ProcessHelper
    {
        public bool RunProcess(string fileName, string arguments)
        {
            var processInfo = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process
            {
                StartInfo = processInfo,
                EnableRaisingEvents = true
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Error: " + error);
                Console.WriteLine("Output: " + output);
                Console.WriteLine($"Process exited with code {process.ExitCode}");
                return false;
            }
            Console.WriteLine(output);
            return true;
        }

        //Run browser to open url
        public void OpenUrlInBrowser(string url)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open URL: " + ex.Message);
            }
        }
    }
}
