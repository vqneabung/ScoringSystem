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

                //If wrong password
                var wrongPassword = "wrong_password";

                //Neu page van o trang login thi dung
                try
                {
                    await page.FillAsync("input#UserName", username);
                    await page.FillAsync("input#Password", wrongPassword);
                    await page.Locator("button:has-text(\"Login\")").ClickAsync();

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

                    // Neu hien thi loi "Invalid Email or Password!"
                    var errorMessage = await page.GetByText("Invalid Email or Password!").InnerTextAsync();

                    if (errorMessage == "Invalid Email or Password!")
                    {
                        Console.Write("Error message displayed correctly: " + errorMessage);
                        Task.Delay(2000).Wait();
                    }
                    else
                    {
                        Console.Write("Error message not displayed as expected.");
                    }
                    Console.WriteLine("Wrong password test passed.");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Wrong password test failed: Navigated away from login page.");
                }

                //Return login page
                await page.GotoAsync("https://localhost:7124/login");

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

                /* 
                 	Implement the View List function for LionProfile with the following:
	List all of the items in the LionProfile table. Display each record with its associated LionTypeName.
	Paginate the list to show 3 records per page.
                 */

                await page.GotoAsync("https://localhost:7124/LionProfile/Index");
                // Wait for the table to load
                await page.WaitForSelectorAsync("table");
                // Count the number of rows in the table body
                var rows = await page.QuerySelectorAllAsync("table tbody tr");
                if (rows.Count > 0)
                {
                    Console.WriteLine($"Table loaded with {rows.Count} records.");
                }
                else
                {
                    Console.WriteLine("Table did not load any records.");
                }
                Task.Delay(2000).Wait();

            }
            catch (PlaywrightException ex)
            {
                Console.WriteLine("Error navigating to the page: " + ex.Message);
            }

            /*
             Question 3 (2.5 points)
	Implement the Create function to add new LionProfile into the database with the following validations:
	All fields are required.
	LionName: Minimum 4 characters, Each word starts with a capital letter. No special characters (#, @, &, (, )).
	Weight must be greater than 30.
	LionTypeId must be selected from a dropdown sourced from the LionType table
	The newly added item should appear at the top of the list.

             */

            await page.GotoAsync("https://localhost:7124/LionProfile/Create");
            // Wait for the form to load
            //Test validation for empty fields
            //Button Create has value "Create"
            await page.Locator("input:has-text(\"Create\")").ClickAsync();

            // Contain validation messages
            var validationMessages = await page.Locator(".text-danger").AllInnerTextsAsync();
            if (validationMessages.Count >= 4) // Assuming there are at least 4 required fields
            {
                Console.WriteLine("Validation messages displayed for empty fields.");
                await page.PauseAsync();
            }
            else
            {
                Console.WriteLine("Validation messages not displayed as expected.");
            }

            //Test LionName validation
            //Get input fields by their name attributes
            await page.Locator("input[name*='LionName']").FillAsync("li");
            await page.Locator("input[name*='Weight']").FillAsync("25");
            await page.Locator("select[name*='LionTypeId']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
            //Input has type datetime-local
            await page.Locator("input[name*='ModifiedDate']").FillAsync("2023-01-01T10:00");
            await page.Locator("input:has-text(\"Create\")").ClickAsync();

            if (await page.Locator(".text-danger").CountAsync() >= 2) // Assuming there are at least 2 validation messages
            {
                Console.WriteLine("Validation messages displayed for LionName and Weight.");
                await page.PauseAsync();
            }
            else
            {
                Console.WriteLine("Validation messages for LionName and Weight not displayed as expected.");
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
