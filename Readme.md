# API Rate Limiter

A small ASP.NET Core MVC application demonstrating an in-process, per-client API rate limiter.

The application exposes a rate-limited HTTP endpoint and a simple browser UI for exercising and observing the limiter.

# Technology

- C# / ASP.NET Core MVC
- .NET 10
- In-memory rate-limit state
- Fixed-window rate-limiting algorithm
- Plain HTML, CSS and JavaScript for the UI
- xUnit for automated tests

I chose ASP.NET Core MVC rather than a separate frontend and API project because the requirements specifically ask for the UI and HTTP endpoint to be served by the same application and port.

The UI is intentionally simple. The assignment prioritises the rate limiter rather than visual design or frontend complexity.

# Running the Application

From the project directory:

dotnet restore
dotnet run --urls http://localhost:8080

The application will be available at:

http://localhost:8080

The default port is 8080.

The port can be changed without modifying the application code:

dotnet run --urls http://localhost:5000

# UI
Open:

http://localhost:8080/

The UI allows you to:

Enter an API key.
Send a single request.
Send a burst of requests.
See the HTTP status returned for each request.
See when each request occurred.
See how long each request took.
See the number of allowed requests.
See the number of rate-limited requests.
See the current limit and remaining requests for the selected API key.
The UI uses the same application and port as the API. No separate frontend development server or CORS configuration is required.

# API
POST /api/process
The endpoint accepts a JSON request body and an X-API-Key header.

# Example
On Windows PowerShell, use curl.exe:

curl.exe -i -X POST http://localhost:8080/api/process `
  -H "X-API-Key: client_a" `
  -H "Content-Type: application/json" `
  -d '{"event_id":"evt_1","type":"property.updated","occurred_at":"2026-06-22T10:00:00Z","payload":{}}'

The request body is deliberately not validated or processed because it is not relevant to the task.


# Response Codes
Status	Meaning
200 OK	Request was allowed by the rate limiter
400 Bad Request	X-API-Key was missing
429 Too Many Requests	The client's rate limit was exceeded

POST /api/process does not return a response body.

When rate limiting is applied, the response also includes:

X-RateLimit-Limit
X-RateLimit-Remaining
Retry-After
These headers are provided to make the limiter's behaviour observable without changing the required response-body contract.

# Rate Limiting
The application uses a fixed-window rate-limiting algorithm.

For example, with:

{
  "Default": {
    "Limit": 5,
    "WindowSeconds": 10
  }
}

a client can make five requests during a ten-second window.

The sixth request is rejected with 429 Too Many Requests.

After the window expires, the client can make requests again.

Example
client_a -> 200
client_a -> 200
client_a -> 200
client_a -> 200
client_a -> 200
client_a -> 429

[window expires]

client_a -> 200

Different API keys have independent rate-limit state.

For example, exhausting the limit for client_a does not affect client_b.

# Configuration
Rate-limit settings are configured in appsettings.json.

The default configuration is:

"RateLimiting": {
  "Default": {
    "Limit": 5,
    "WindowSeconds": 10
  },
  "Clients": {
    "client_a": {
      "Limit": 10,
      "WindowSeconds": 30
    }
  }
}

The default configuration applies to clients that do not have an explicit configuration.

Individual clients can have different limits and windows.

For example:

Client	Limit	Window
Default	5 requests	10 seconds
client_a	10 requests	30 seconds

Changing these values does not require a code change.

# Rate Limiter Design
The rate limiter is implemented separately from the HTTP controller.

The main components are:

ProcessController
       |
       v
  IRateLimiter
       |
       v
FixedWindowRateLimiter
       |
       v
In-memory client state

The limiter stores state per API key.

Each client has:

The start time of its current window.
The number of requests used during that window.
A lock is used around the check-and-increment operation for an individual client's state. This prevents concurrent requests from both observing the same available slot and exceeding the configured limit.

The limiter is registered as a singleton so that all requests handled by the application share the same in-memory state.

# Why a Fixed Window?
I chose a fixed-window algorithm because it provides a small and easy-to-understand implementation that satisfies the requirements of the task.

A sliding-window or token-bucket implementation could provide smoother traffic behaviour, but would add complexity that I did not think was justified for this exercise.

The fixed-window approach also makes the behaviour straightforward to test and explain.

# In-Memory State and Distributed Deployment
The assignment explicitly states that no external services or databases should be used, so the limiter stores state in application memory.

This means the implementation is suitable for demonstrating the behaviour in a single application process, but it is not globally consistent across multiple application instances.

For example, if two instances were running:

              Load Balancer
              /           \
             /             \
            v               v
       Instance A       Instance B
          state             state

Each instance would have its own counters.

In a real distributed deployment, I would move the rate-limit state into a shared store such as Redis and use an atomic operation to perform the check and increment.

# Tests
The project includes automated tests for the main rate-limiting behaviour.

Run them with:

dotnet test

The tests cover:

Requests are allowed up to the configured limit.
Requests are rejected after the limit is reached.
Requests are allowed again after the window expires.
Different API keys have independent limits.
Unknown clients use the default configuration.
Explicitly configured clients use their own limits.
Reading the current limit does not consume a request.
The HTTP layer should also be tested to verify the actual /api/process contract, particularly that the endpoint returns an empty body for both successful and rate-limited requests.

# Frontend Testing
The UI is intentionally implemented using plain JavaScript rather than a frontend framework.

I chose this because the UI requirements are small and do not justify introducing a separate frontend build pipeline.

The frontend code is responsible for:

Sending requests with the selected API key.
Sending bursts of requests.
Recording the result of each request.
Tracking allowed and rate-limited requests.
Displaying the current limit.
At least one automated UI test is included to verify the UI behaviour.

# Key Decisions
In-Memory State
Chosen because the task explicitly prohibits external databases and services.

Fixed-Window Algorithm
Chosen for simplicity and because it satisfies the stated requirements without unnecessary complexity.

Per-Client State
The API key is used as the client identity, with each key maintaining independent rate-limit state.

Separate Rate Limiter Service
The rate-limiting algorithm is kept independent from the HTTP controller so that it can be tested without requiring HTTP requests.

# Simple UI
Plain HTML and JavaScript were used because the UI is intended to demonstrate the API rather than be a production dashboard.

# Known Limitations
This is intentionally not a production-ready distributed rate limiter.

# Known limitations include:

Rate-limit state is lost when the application restarts.
Multiple application instances would have independent limits.
A large number of unique API keys could cause the in-memory state to grow.
A fixed-window algorithm can allow bursts around a window boundary.
There is no authentication or validation of API keys because authentication is outside the scope of the task.
The request body is not processed because the task explicitly says it should be ignored.
# What I Would Improve With More Time
If this were being developed beyond the scope of the exercise, I would consider:

Moving state into a shared Redis store for multi-instance deployments.
Using atomic Redis operations for the rate-limit check/increment.
Considering a sliding-window or token-bucket algorithm depending on traffic requirements.
Improving cleanup of inactive client state.
Adding more comprehensive integration and browser tests.
Introducing a controllable clock abstraction to make expiry tests deterministic rather than waiting for real time.
Adding metrics and structured logging around rate-limit decisions.
I intentionally did not implement these because the assignment asks for a focused solution and specifically states that a production-ready distributed system is not required.