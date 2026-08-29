using APIRateLimiter.Models;

namespace APIRateLimiter.Services
{
    public interface IRateLimiter
    {
        RateLimitResult Check(string clientKey);

        RateLimitResult GetStatus(string clientKey);

    }
}
