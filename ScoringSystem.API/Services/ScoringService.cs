using RestSharp;
using ScoringSystem.API.Context;
using ScoringSystem.API.Dtos;
using System.Diagnostics;
using System.Text.Json;

namespace ScoringSystem.API.Services
{
    public class ScoringService
    {
        private readonly HttpClient _httpClient;

        public ScoringService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true;
            _httpClient = new HttpClient(handler);
        }

        public async Task<ScoringResponse> ScoreTestCasesAsync(TestCaseRequest testCaseRequest)
        {
            var response = new ScoringResponse
            {
                Success = true,
                Results = new List<TestResult>()
            };

            try
            {
                foreach (var testCase in testCaseRequest.TestCases)
                {
                    var result = await ExecuteTestCaseAsync(testCase, testCaseRequest.BaseUrl);
                    response.Results.Add(result);

                    if (result.Passed)
                        response.PassedTests++;
                    else
                        response.FailedTests++;
                }

                response.TotalTests = testCaseRequest.TestCases.Count;
                response.Score = response.TotalTests > 0
                  ? (double)response.PassedTests / response.TotalTests * 100
                : 0;
                response.Message = $"Completed {response.TotalTests} tests. Passed: {response.PassedTests}, Failed: {response.FailedTests}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        private async Task<TestResult> ExecuteTestCaseAsync(Request testCase, string baseUrl)
        {
            var result = new TestResult
            {
                TestCaseName = testCase.Name ?? "Unnamed Test",
                Url = testCase.Url,
                Method = testCase.Method,
                Passed = false
            };

            var sw = Stopwatch.StartNew();

            try
            {
                var fullUrl = $"{baseUrl.TrimEnd('/')}/{testCase.Url.TrimStart('/')}";
                var request = new HttpRequestMessage(new HttpMethod(testCase.Method), fullUrl);

                // Add Authorization header if token exists
                if (!string.IsNullOrEmpty(AuthContext.Token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthContext.Token);
                }

                // Add request body if provided
                if (testCase.RequestBody != null)
                {
                    var jsonContent = JsonSerializer.Serialize(testCase.RequestBody);
                    request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                }

                var httpResponse = await _httpClient.SendAsync(request);
                sw.Stop();

                result.StatusCode = (int)httpResponse.StatusCode;
                result.ResponseTimeMs = sw.ElapsedMilliseconds;

                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                // Handle special cases (login, etc.)
                if (testCase.Special?.ToLower() == "login")
                {
                    result.Passed = HandleLoginResponse(responseContent, result);
                }
                else
                {
                    // Check status code
                    if (testCase.ExpectedStatusCode.HasValue && result.StatusCode != testCase.ExpectedStatusCode)
                    {
                        result.Message = $"Expected status code {testCase.ExpectedStatusCode}, got {result.StatusCode}";
                        result.Response = responseContent;
                        return result;
                    }

                    // Check response format if expected response is provided
                    if (testCase.ExpectedResponse != null)
                    {
                        result.Passed = ValidateResponseFormat(responseContent, testCase.ExpectedResponse, result);
                    }
                    else
                    {
                        result.Passed = httpResponse.IsSuccessStatusCode;
                    }

                    result.Response = responseContent;
                    result.Message = result.Passed ? "Test passed" : "Response validation failed";
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.ResponseTimeMs = sw.ElapsedMilliseconds;
                result.Message = $"Error: {ex.Message}";
                result.StatusCode = 0;
                result.Passed = false;
            }

            return result;
        }

        private bool HandleLoginResponse(string responseContent, TestResult result)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(responseContent);
                var root = jsonDocument.RootElement;

                // Check if response contains token
                if (root.TryGetProperty("token", out var tokenElement))
                {
                    var token = tokenElement.GetString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Save token to AuthContext
                        AuthContext.Token = token;
                        result.Message = "Login successful, token saved";
                        result.Passed = true;
                        return true;
                    }
                }

                // Check for common response structure
                if (root.TryGetProperty("accessToken", out var accessTokenElement))
                {
                    var token = accessTokenElement.GetString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        AuthContext.Token = token;
                        result.Message = "Login successful, token saved";
                        result.Passed = true;
                        return true;
                    }
                }

                result.Message = "Login response format incorrect, no token found";
                result.Passed = false;
            }
            catch (Exception ex)
            {
                result.Message = $"Error parsing login response: {ex.Message}";
                result.Passed = false;
            }

            return false;
        }

        private bool ValidateResponseFormat(string responseContent, Dictionary<string, object> expectedResponse, TestResult result)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(responseContent);
                var root = jsonDocument.RootElement;

                foreach (var key in expectedResponse.Keys)
                {
                    if (!root.TryGetProperty(key, out _))
                    {
                        result.Message = $"Expected response property '{key}' not found";
                        return false;
                    }
                }

                result.Message = "Response format valid";
                return true;
            }
            catch (Exception ex)
            {
                result.Message = $"Error validating response: {ex.Message}";
                return false;
            }
        }
    }
}
