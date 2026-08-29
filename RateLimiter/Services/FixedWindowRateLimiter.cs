using Microsoft.Extensions.Options;
using APIRateLimiter.Models;
using System.Collections.Concurrent;

namespace APIRateLimiter.Services
{
    public class FixedWindowRateLimiter:IRateLimiter
    {
        private readonly RateLimitOptions _options;

        private readonly ConcurrentDictionary<string, ClientState> _clients = new();

        public FixedWindowRateLimiter(IOptions<RateLimitOptions> options)
        {
            _options = options.Value;
        }

        public RateLimitResult Check(string clientKey)
        {
            var settings = GetSettings(clientKey);
            var now = DateTimeOffset.UtcNow;

            var state = _clients.GetOrAdd(
                clientKey,
                _ => new ClientState(now));

            lock (state)
            {
                // Start a new window if the current one has expired.
                if (now >= state.WindowStart.AddSeconds(settings.WindowSeconds))
                {
                    state.WindowStart = now;
                    state.RequestCount = 0;
                }

                if (state.RequestCount >= settings.Limit)
                {
                    var retryAfter = state.WindowStart
                        .AddSeconds(settings.WindowSeconds) - now;

                    return new RateLimitResult
                    {
                        Allowed = false,
                        Limit = settings.Limit,
                        Remaining = 0,
                        RetryAfterSeconds = Math.Max(
                            1,
                            (int)Math.Ceiling(retryAfter.TotalSeconds))
                    };
                }

                state.RequestCount++;

                return new RateLimitResult
                {
                    Allowed = true,
                    Limit = settings.Limit,
                    Remaining = Math.Max(
                        0,
                        settings.Limit - state.RequestCount),
                    RetryAfterSeconds = 0
                };
            }
        }

        public RateLimitResult GetStatus(string clientKey)
        {
            var settings = GetSettings(clientKey);
            var now = DateTimeOffset.UtcNow;

            var state = _clients.GetOrAdd(
                clientKey,
                _ => new ClientState(now));

            lock (state)
            {
                if (now >= state.WindowStart.AddSeconds(settings.WindowSeconds))
                {
                    state.WindowStart = now;
                    state.RequestCount = 0;
                }

                var retryAfter = state.WindowStart
                    .AddSeconds(settings.WindowSeconds) - now;

                return new RateLimitResult
                {
                    Allowed = true,
                    Limit = settings.Limit,
                    Remaining = Math.Max(
                        0,
                        settings.Limit - state.RequestCount),
                    RetryAfterSeconds = Math.Max(
                        0,
                        (int)Math.Ceiling(retryAfter.TotalSeconds))
                };
            }
        }

        private RateLimitSettings GetSettings(string clientKey)
        {
            if (_options.Clients.TryGetValue(clientKey, out var clientSettings))
            {
                return clientSettings;
            }

            return _options.Default;
        }

        private sealed class ClientState
        {
            public ClientState(DateTimeOffset windowStart)
            {
                WindowStart = windowStart;
            }

            public DateTimeOffset WindowStart { get; set; }

            public int RequestCount { get; set; }
        }
    }
}
