using RestSharp;
using ScoringSystem.API.Context;
using ScoringSystem.API.Dtos;
using System.Diagnostics;
using System.Text.Json;

namespace ScoringSystem.API.Services
{
    public class ScoringService
    {
        private RestClient? _restClient;

        public ScoringService()
        {
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
                // Initialize RestClient with base URL and SSL bypass
                var options = new RestClientOptions(testCaseRequest.BaseUrl)
                {
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                    MaxTimeout = 30000 // 30 seconds timeout
                };
                _restClient = new RestClient(options);

                foreach (var testCase in testCaseRequest.TestCases)
                {
                    var result = await ExecuteTestCaseAsync(testCase);
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

        private async Task<TestResult> ExecuteTestCaseAsync(Request testCase)
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
                // Create request
                var request = new RestRequest(testCase.Url, GetRestSharpMethod(testCase.Method));

                // Add Authorization header if token exists
                if (!string.IsNullOrEmpty(AuthContext.Token))
                {
                    request.AddHeader("Authorization", $"Bearer {AuthContext.Token}");
                }

                // Add request body if provided
                if (testCase.RequestBody != null)
                {
                    request.AddJsonBody(testCase.RequestBody);
                }

                // Execute request
                var response = await _restClient!.ExecuteAsync(request);
                sw.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseTimeMs = sw.ElapsedMilliseconds;
                result.Response = response.Content ?? "";

                // Log response for debugging
                Console.WriteLine($"[{testCase.Name}] Status: {result.StatusCode}, Content Length: {response.Content?.Length ?? 0}");
                if (!string.IsNullOrWhiteSpace(response.Content))
                {
                    Console.WriteLine($"[{testCase.Name}] Response Preview: {response.Content.Substring(0, Math.Min(200, response.Content.Length))}");
                }

                // Handle special cases (login, etc.)
                if (testCase.Special?.ToLower() == "login")
                {
                    result.Passed = HandleLoginResponse(response, result);
                }
                else
                {
                    // Check status code
                    if (testCase.ExpectedStatusCode.HasValue && result.StatusCode != testCase.ExpectedStatusCode)
                    {
                        result.Message = $"Expected status code {testCase.ExpectedStatusCode}, got {result.StatusCode}";
                        return result;
                    }

                    // Check response format if expected response is provided
                    if (testCase.ExpectedResponse != null)
                    {
                        result.Passed = ValidateResponseFormat(response.Content ?? "", testCase.ExpectedResponse, result);
                    }
                    else
                    {
                        result.Passed = response.IsSuccessful;
                    }

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

        private bool HandleLoginResponse(RestResponse response, TestResult result)
        {
            try
            {
                // Check if response is successful
                if (!response.IsSuccessful)
                {
                    result.Message = $"Login failed with status code {response.StatusCode}";
                    return false;
                }

                var responseContent = response.Content;

                // Check if response is empty
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    result.Message = "Login response is empty";
                    return false;
                }

                // Check if response is HTML (error page)
                if (responseContent.TrimStart().StartsWith("<") || responseContent.Contains("<!DOCTYPE"))
                {
                    result.Message = "Login response is HTML (possible error page), not JSON";
                    return false;
                }

                // Try to parse JSON
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
                            AuthContext.Token = token;
                            result.Message = "Login successful, token saved";
                            result.Passed = true;
                            Console.WriteLine($"✅ Token saved: {token.Substring(0, Math.Min(20, token.Length))}...");
                            return true;
                        }
                    }

                    // Check for accessToken
                    if (root.TryGetProperty("accessToken", out var accessTokenElement))
                    {
                        var token = accessTokenElement.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            AuthContext.Token = token;
                            result.Message = "Login successful, accessToken saved";
                            result.Passed = true;
                            Console.WriteLine($"✅ AccessToken saved: {token.Substring(0, Math.Min(20, token.Length))}...");
                            return true;
                        }
                    }

                    // Check for data.token (nested)
                    if (root.TryGetProperty("data", out var dataElement))
                    {
                        if (dataElement.TryGetProperty("token", out var nestedTokenElement))
                        {
                            var token = nestedTokenElement.GetString();
                            if (!string.IsNullOrEmpty(token))
                            {
                                AuthContext.Token = token;
                                result.Message = "Login successful, nested token saved";
                                result.Passed = true;
                                Console.WriteLine($"✅ Nested Token saved: {token.Substring(0, Math.Min(20, token.Length))}...");
                                return true;
                            }
                        }
                    }

                    result.Message = "Login response valid JSON but no token found. Response: " + responseContent.Substring(0, Math.Min(200, responseContent.Length));
                    result.Passed = false;
                }
                catch (JsonException jsonEx)
                {
                    result.Message = $"Login response is not valid JSON: {jsonEx.Message}. Content: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}";
                    result.Passed = false;
                }
            }
            catch (Exception ex)
            {
                result.Message = $"Error handling login response: {ex.Message}";
                result.Passed = false;
            }

            return false;
        }

        private bool ValidateResponseFormat(string responseContent, Dictionary<string, object> expectedResponse, TestResult result)
        {
            try
            {
                // Check if response is empty
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    result.Message = "Response is empty";
                    return false;
                }

                // Check if response is HTML
                if (responseContent.TrimStart().StartsWith("<"))
                {
                    result.Message = "Response is HTML, not JSON";
                    return false;
                }

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

        private Method GetRestSharpMethod(string method)
        {
            return method.ToUpper() switch
            {
                "GET" => Method.Get,
                "POST" => Method.Post,
                "PUT" => Method.Put,
                "DELETE" => Method.Delete,
                "PATCH" => Method.Patch,
                _ => Method.Get
            };
        }
    }
}
