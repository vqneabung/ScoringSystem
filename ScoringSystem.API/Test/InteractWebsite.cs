using Microsoft.Playwright;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ScoringSystem.API.Test
{
    public class InteractWebsite
    {

        public async Task Run()
        {
            using var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false // Set to true if you want to run in headless mode
            });


            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            var page = await context.NewPageAsync();

            try
            {

                await WaitUntilServerReady("https://localhost:5000/login", timeout: TimeSpan.FromSeconds(30));
                await page.GotoAsync("https://localhost:5000/login");

            }
            catch (PlaywrightException ex)
            {
                Console.WriteLine("Error navigating to the page: " + ex.Message);
            }


        }

        public async Task TestRun()
        {
            using var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false // Set to true if you want to run in headless mode
            });


            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            var page = await context.NewPageAsync();

            try
            {

                await page.GotoAsync("https://localhost:7124/login");

                var username = "manager";
                var password = "@1";

                // Test login page
                await page.FillAsync("input#UserName", username);
                await page.FillAsync("input#Password", password);
                await page.Locator("button:has-text(\"Login\")").ClickAsync();

                try
                {
                    await page.WaitForURLAsync("https://localhost:7124/LionProfile/Index");
                    Console.WriteLine("Login test passed.");
                    Task.Delay(2000).Wait();
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Login test failed: Did not navigate to the expected URL.");
                }

                //Return login page
                await page.GotoAsync("https://localhost:7124/login");

                //If wrong password
                var wrongPassword = "wrong_password";

                //Neu page van o trang login thi dung
                try
                {
                    var currentURL = page.Url;
                    if (currentURL.ToLower() == "https://localhost:7124/Login".ToLower())
                    {
                        Console.WriteLine("Still on login page after wrong password, as expected.");
                    }
                    else
                    {
                        Console.WriteLine("Wrong password test failed: Navigated away from login page.");
                        return;
                    }

                    await page.FillAsync("input#UserName", username);
                    await page.FillAsync("input#Password", wrongPassword);
                    await page.Locator("button:has-text(\"Login\")").ClickAsync();

                    // Neu hien thi loi "Invalid Email or Password!"
                    var errorMessage = await page.GetByText("Invalid Email or Password!").InnerTextAsync();

                    if (errorMessage == "Invalid Email or Password!")
                    {
                        Console.Write("Error message displayed correctly: " + errorMessage);
                    }
                    else
                    {
                        Console.Write("Error message not displayed as expected.");
                    }
                    Console.WriteLine("Wrong password test passed.");
                    await page.PauseAsync();
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Wrong password test failed: Navigated away from login page.");
                }

            }
            catch (PlaywrightException ex)
            {
                Console.WriteLine("Error navigating to the page: " + ex.Message);
            }


        }

        private async Task WaitUntilServerReady(string url, TimeSpan timeout)
        {
            var client = new HttpClient();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    var resp = await client.GetAsync(url);
                    if (resp.IsSuccessStatusCode)
                        return;
                }
                catch
                {
                    // ignore, chưa ready
                }
                await Task.Delay(500);
            }
            throw new TimeoutException($"Server not ready at {url} after {timeout}");
        }
    }
}
