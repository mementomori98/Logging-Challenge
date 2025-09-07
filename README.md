# Coding Challenge: Implement Logging in a Weather API

## Objective
You are given a simple Weather API built in .NET 9.  
Your task is to **implement structured, configurable, and reliable logging** throughout the system.

---

## Starter Project
The API currently exposes two endpoints:

- `GET /weather/current?city=...` → returns current weather.
- `GET /weather/forecast?city=...` → returns a 5-day forecast.

It uses a fake in-memory service to simulate weather data.  
⚠️ At the moment, **there is no logging at all**.

---

## Requirements

### 1. Structured Logging
- Use `ILogger` (built-in) or integrate a logging library (e.g., Serilog, NLog).
- Each log entry should include:
  - Timestamp (UTC)
  - Log level (Information, Warning, Error, Critical)
  - Message
  - Correlation/request ID
  - Contextual data (e.g., city name for requests)
  - Assembly Version
  - Environment
  - Api name

### 2. Log Levels
- `Information`: For successful requests and system events.
- `Warning`: For recoverable issues (e.g., incomplete forecast data).
- `Error`: For failed operations (e.g., weather service unavailable).
- `Critical`: For unhandled exceptions.

### 3. Request Tracing
- Generate a correlation ID for each request.
- Include the ID in all related log entries.
- Return the correlation ID in the API response headers.

### 4. Error Handling
- Log unhandled exceptions.
- Return safe, user-friendly error responses (no internal stack traces).

### 5. Configuration
- Log level should be configurable via `User-Secrets`.
- Default: Console logging.
- (Optional) Add file logging or JSON output.

---

## Challenge Steps

1. **Set up logging**
   - Configure logging providers in `Program.cs`.

2. **Add request/response logging**
   - Log incoming requests (method, path, city parameter).
   - Log outgoing responses (status code, duration).

3. **Add service logging**
   - Log at `Information` when weather is successfully fetched.
   - Log at `Warning` for partial/missing forecast data.
   - Log at `Error` when the fake service fails.
   - Log at `Critical` for unhandled exceptions.

4. **Add correlation IDs**
   - Generate one per request (middleware).
   - Pass through all logs for traceability.

---

## Bonus Challenges

1. **File or JSON Logging**
   - Write logs to a file in JSON format.

2. **Request Duration Metrics**
   - Log duration for each request.
   - Trigger a warning if it exceeds a threshold.

3. **Centralized Logging**
   - Integrate with Seq, ELK, or Application Insights.

4. **Log Filtering**
   - Allow filtering by log level or source.

---

## Evaluation Criteria
- Proper use of log levels.
- Consistent correlation ID handling.
- No sensitive data in logs.
- Configurable logging (easy to change providers/levels).
- Clean, maintainable, testable code.
