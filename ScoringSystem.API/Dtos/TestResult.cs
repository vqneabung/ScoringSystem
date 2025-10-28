namespace ScoringSystem.API.Dtos
{
    public class TestResult
    {
      public string TestCaseName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Response { get; set; }
        public long ResponseTimeMs { get; set; }
    }

    public class ScoringResponse
    {
        public bool Success { get; set; }
        public List<TestResult> Results { get; set; } = new();
     public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
    public double Score { get; set; } // Percentage
        public string Message { get; set; } = string.Empty;
    }
}
