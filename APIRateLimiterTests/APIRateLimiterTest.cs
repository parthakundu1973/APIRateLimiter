using APIRateLimiter.Models;
using APIRateLimiter.Services;
using Microsoft.Extensions.Options;

namespace APIRateLimiterTests
{
    public class APIRateLimiterTest
    {
        [Fact]
        public void AllowsRequestsUpToConfiguredLimit()
        {
            var limiter = CreateLimiter(
                limit: 3,
                windowSeconds: 60);

            var first = limiter.Check("client_a");
            var second = limiter.Check("client_a");
            var third = limiter.Check("client_a");

            Assert.True(first.Allowed);
            Assert.True(second.Allowed);
            Assert.True(third.Allowed);

            Assert.Equal(2, first.Remaining);
            Assert.Equal(1, second.Remaining);
            Assert.Equal(0, third.Remaining);
        }

        [Fact]
        public void RejectsRequestAfterLimitIsReached()
        {
            var limiter = CreateLimiter(
                limit: 2,
                windowSeconds: 60);

            Assert.True(limiter.Check("client_a").Allowed);
            Assert.True(limiter.Check("client_a").Allowed);

            var result = limiter.Check("client_a");

            Assert.False(result.Allowed);
            Assert.Equal(2, result.Limit);
            Assert.Equal(0, result.Remaining);
            Assert.True(result.RetryAfterSeconds > 0);
        }

        [Fact]
        public async Task AllowsRequestsAgainAfterWindowExpires()
        {
            var limiter = CreateLimiter(
                limit: 2,
                windowSeconds: 1);

            Assert.True(limiter.Check("client_a").Allowed);
            Assert.True(limiter.Check("client_a").Allowed);
            Assert.False(limiter.Check("client_a").Allowed);

            await Task.Delay(1100);

            var result = limiter.Check("client_a");

            Assert.True(result.Allowed);
            Assert.Equal(1, result.Remaining);
        }

        [Fact]
        public void DifferentClientsHaveIndependentLimits()
        {
            var limiter = CreateLimiter(
                limit: 2,
                windowSeconds: 60);

            Assert.True(limiter.Check("client_a").Allowed);
            Assert.True(limiter.Check("client_a").Allowed);
            Assert.False(limiter.Check("client_a").Allowed);

            // client_b has its own independent limit.
            Assert.True(limiter.Check("client_b").Allowed);
            Assert.True(limiter.Check("client_b").Allowed);
            Assert.False(limiter.Check("client_b").Allowed);
        }

        [Fact]
        public void UsesDefaultConfigurationForUnknownClient()
        {
            var limiter = CreateLimiter(
                limit: 3,
                windowSeconds: 60);

            var first = limiter.Check("unknown_client");
            var second = limiter.Check("unknown_client");
            var third = limiter.Check("unknown_client");
            var fourth = limiter.Check("unknown_client");

            Assert.True(first.Allowed);
            Assert.True(second.Allowed);
            Assert.True(third.Allowed);
            Assert.False(fourth.Allowed);

            Assert.Equal(3, first.Limit);
        }

        [Fact]
        public void UsesClientSpecificConfigurationWhenConfigured()
        {
            var options = new RateLimitOptions
            {
                Default = new RateLimitSettings
                {
                    Limit = 2,
                    WindowSeconds = 60
                },
                Clients = new Dictionary<string, RateLimitSettings>
                {
                    ["client_a"] = new RateLimitSettings
                    {
                        Limit = 5,
                        WindowSeconds = 60
                    }
                }
            };

            var limiter = new FixedWindowRateLimiter(
                Options.Create(options));

            // client_a gets its custom limit of 5.
            for (var i = 0; i < 5; i++)
            {
                Assert.True(limiter.Check("client_a").Allowed);
            }

            Assert.False(limiter.Check("client_a").Allowed);

            // client_b gets the default limit of 2.
            Assert.True(limiter.Check("client_b").Allowed);
            Assert.True(limiter.Check("client_b").Allowed);
            Assert.False(limiter.Check("client_b").Allowed);
        }

        [Fact]
        public void GetStatusDoesNotConsumeRequest()
        {
            var limiter = CreateLimiter(
                limit: 3,
                windowSeconds: 60);

            var statusBefore = limiter.GetStatus("client_a");

            Assert.Equal(3, statusBefore.Remaining);

            limiter.Check("client_a");

            var statusAfter = limiter.GetStatus("client_a");

            Assert.Equal(2, statusAfter.Remaining);
        }

        private static FixedWindowRateLimiter CreateLimiter(
            int limit,
            int windowSeconds)
        {
            var options = new RateLimitOptions
            {
                Default = new RateLimitSettings
                {
                    Limit = limit,
                    WindowSeconds = windowSeconds
                }
            };

            return new FixedWindowRateLimiter(
                Options.Create(options));
        }
    }
}
