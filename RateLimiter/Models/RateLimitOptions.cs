namespace APIRateLimiter.Models
{
  

    public class RateLimitOptions
    {
        public RateLimitSettings Default { get; set; } = new();

        public Dictionary<string, RateLimitSettings> Clients { get; set; } = new();
    }

    public class RateLimitSettings
    {
        public int Limit { get; set; } = 5;

        public int WindowSeconds { get; set; } = 10;
    }
}
