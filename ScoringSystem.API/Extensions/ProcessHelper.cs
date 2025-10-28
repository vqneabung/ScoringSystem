using System.Diagnostics;

namespace ScoringSystem.API.Extensions
{
    public class ProcessHelper
    {
        public async Task<bool> RunProcess(string fileName, string arguments, Func<Task>? asyncFunction = null, Action? function = null, bool? exitProcess = false)
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

            // Khi có dòng output mới — sẽ được gọi ngay
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)  // nếu không phải là dòng kết thúc
                {
                    Console.WriteLine(e.Data);
                }
            };

            // Khi có dòng error mới
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine("ERR: " + e.Data);
                }
            };

            process.Start();

            // Bắt đầu đọc output & error bất đồng bộ
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            //Thực thi hàm truyền vào trong khi process đang chạy
            if (asyncFunction != null)   
            {
                try
                {
                    // ⭐ AWAIT async function properly
                    await asyncFunction();
                    Console.WriteLine("Async function completed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during async function execution: " + ex.Message);
                }
            }
            else if (function != null)
            {
                try
                {
                    function();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during function execution: " + ex.Message);
                }
            }

            // Kill process if requested
            if (exitProcess == true)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        Console.WriteLine("Process killed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error killing process: " + ex.Message);
                }
            }

            // Chờ process kết thúc
            process.WaitForExit();

            // (Nếu bạn muốn chắc chắn các dòng cuối được xử lý)
            process.CancelOutputRead();
            process.CancelErrorRead();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Process exited with code {process.ExitCode}");
                return false;
            }

            return true;
        }

        // Một hàm polling đơn giản
       

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

        public void KillProcessByName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                    Console.WriteLine($"Killed process: {process.ProcessName} (ID: {process.Id})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to kill process: " + ex.Message);
            }
        }
    }
}
