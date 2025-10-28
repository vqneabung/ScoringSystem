namespace ScoringSystem.API.Dtos
{
    public class Request
    {
        public string? Name { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "GET"; // GET, POST, PUT, DELETE
        public string? Special { get; set; } // login, etc.
        public Dictionary<string, object>? RequestBody { get; set; }
        public Dictionary<string, object>? ExpectedResponse { get; set; }
        public int? ExpectedStatusCode { get; set; } = 200;
    }

    public class TestCaseRequest
    {
        public List<Request> TestCases { get; set; } = new();
        public string BaseUrl { get; set; } = "https://localhost:5000";
    }
}
