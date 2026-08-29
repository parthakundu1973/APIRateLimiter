using Microsoft.AspNetCore.Mvc;
using APIRateLimiter.Services;

namespace APIRateLimiter.Controllers
{
    [ApiController]
    [Route("api")]
    public class ProcessController : ControllerBase
    {


        private readonly IRateLimiter _rateLimiter;

        public ProcessController(IRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        [HttpPost("process")]
        public IActionResult Process()
        {
            var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            var result = _rateLimiter.Check(apiKey);

            Response.Headers["X-RateLimit-Limit"] =
                result.Limit.ToString();

            Response.Headers["X-RateLimit-Remaining"] =
                result.Remaining.ToString();

            if (!result.Allowed)
            {
                Response.Headers["Retry-After"] =
                    result.RetryAfterSeconds.ToString();


                return StatusCode(StatusCodes.Status429TooManyRequests);

            }

            return StatusCode(StatusCodes.Status200OK);
        }

        [HttpGet("limits")]
        public IActionResult GetLimits([FromQuery] string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return BadRequest();
            }

            var result = _rateLimiter.GetStatus(apiKey);

            return Ok(new
            {
                apiKey,
                limit = result.Limit,
                remaining = result.Remaining
            });
        }
    }
}
