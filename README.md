# BigBlueButton .NET Client

A high-performance, production-ready .NET client library for BigBlueButton API integration. Built with performance, reliability, and ease of use in mind.

## Features

- 🚀 **High Performance**: Connection pooling, response caching, and optimized serialization
- 🔒 **Security**: Built-in checksum generation following BigBlueButton security model
- 🛡️ **Reliability**: Retry policies, circuit breaker pattern, and comprehensive error handling
- 📦 **Easy Integration**: Simple dependency injection setup and fluent API
- 🔧 **Configurable**: Extensive configuration options for different deployment scenarios
- 📚 **Well Documented**: Comprehensive XML documentation and examples

## Installation

### NuGet Package
```bash
dotnet add package BigBlueButton.Client
```

### Local Development
```bash
# Clone the repository
git clone https://github.com/yourusername/BigBlueButton.git
cd BigBlueButton

# Build the package
dotnet pack src/BigBlueButton/BigBlueButton.csproj

# Install locally
dotnet add package BigBlueButton.Client --source ./src/BigBlueButton/bin/Debug/
```

## Quick Start

### 1. Add Services to DI Container

```csharp
// In Program.cs or Startup.cs
using BigBlueButton;

var builder = WebApplication.CreateBuilder(args);

// Add BigBlueButton services with configuration
builder.Services.AddBigBlueButton(builder.Configuration);

// Or configure manually
builder.Services.AddBigBlueButton(options =>
{
    options.ServerUrl = "https://your-bbb-server.com/bigbluebutton/api/";
    options.SharedSecret = "your-shared-secret";
    options.MaxRetryAttempts = 3;
    options.TimeoutSeconds = 30;
    options.EnableResponseCaching = true;
});
```

### 2. Configuration

Add to your `appsettings.json`:

```json
{
  "BigBlueButton": {
    "ServerUrl": "https://your-bbb-server.com/bigbluebutton/api/",
    "SharedSecret": "your-shared-secret-here",
    "DefaultMeetingDuration": 120,
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 1000,
    "TimeoutSeconds": 30,
    "SHAVersion": 1,
    "MaxConcurrentRequests": 10,
    "ConnectionPoolSize": 100,
    "EnableResponseCaching": true,
    "CacheExpirationSeconds": 300
  }
}
```

### 3. Use in Your Code

```csharp
[ApiController]
[Route("api/[controller]")]
public class MeetingsController : ControllerBase
{
    private readonly IBigBlueButtonService _bbbService;

    public MeetingsController(IBigBlueButtonService bbbService)
    {
        _bbbService = bbbService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequest request)
    {
        try
        {
            var response = await _bbbService.CreateMeetingAsync(request, cancellationToken: HttpContext.RequestAborted);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

## Performance Features

### Connection Pooling
- HTTP/2 support with multiple connections
- Configurable connection lifetime and idle timeout
- Optimized for high-concurrency scenarios

### Response Caching
- In-memory caching with configurable expiration
- Automatic cache invalidation
- Reduces API calls for repeated requests

### Concurrency Control
- Semaphore-based request throttling
- Configurable maximum concurrent requests
- Prevents overwhelming the BBB server

### Optimized Serialization
- Cached XML serializers
- JSON fallback for compatibility
- Efficient parameter conversion

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServerUrl` | string | Required | BigBlueButton server API endpoint |
| `SharedSecret` | string | Required | Server shared secret for security |
| `DefaultMeetingDuration` | int | 120 | Default meeting duration in minutes |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts for failed requests |
| `RetryDelayMilliseconds` | int | 1000 | Base delay between retries |
| `TimeoutSeconds` | int | 30 | HTTP request timeout |
| `SHAVersion` | int | 1 | SHA algorithm version (1, 256, 384, 512) |
| `MaxConcurrentRequests` | int | 10 | Maximum concurrent API requests |
| `ConnectionPoolSize` | int | 100 | HTTP connection pool size |
| `EnableResponseCaching` | bool | true | Enable response caching |
| `CacheExpirationSeconds` | int | 300 | Cache expiration time |

## Error Handling

The library provides comprehensive error handling:

- **Retry Policies**: Automatic retry with exponential backoff
- **Circuit Breaker**: Prevents cascading failures
- **Detailed Logging**: Structured logging for debugging
- **Exception Types**: Specific exceptions for different error scenarios

## Security

- **Checksum Generation**: Automatic checksum calculation following BBB spec
- **SHA Support**: Multiple SHA algorithm support (SHA1, SHA256, SHA384, SHA512)
- **Parameter Validation**: Input validation and sanitization
- **Secure Headers**: Proper HTTP headers for security

## Best Practices

### 1. Configuration
- Use strong, unique shared secrets
- Configure appropriate timeouts for your network
- Enable caching for read-heavy workloads
- Set reasonable concurrency limits

### 2. Error Handling
- Always handle exceptions in your application code
- Use cancellation tokens for long-running operations
- Monitor retry patterns and adjust configuration accordingly

### 3. Performance
- Use connection pooling for high-traffic scenarios
- Enable response caching for frequently accessed data
- Monitor memory usage with caching enabled
- Adjust concurrency limits based on server capacity

### 4. Monitoring
- Log API response times
- Monitor retry and failure rates
- Track cache hit/miss ratios
- Set up alerts for circuit breaker trips

## Examples

### Advanced Configuration

```csharp
builder.Services.AddBigBlueButton(options =>
{
    options.ServerUrl = "https://bbb.example.com/api/";
    options.SharedSecret = Environment.GetEnvironmentVariable("BBB_SHARED_SECRET");
    options.MaxRetryAttempts = 5;
    options.RetryDelayMilliseconds = 500;
    options.TimeoutSeconds = 60;
    options.SHAVersion = 256; // Use SHA256
    options.MaxConcurrentRequests = 20;
    options.EnableResponseCaching = true;
    options.CacheExpirationSeconds = 600; // 10 minutes
});
```

### Custom HTTP Client Configuration with Policies

```csharp
// Add Polly policies
builder.Services.AddBigBlueButton(options =>
{
    options.ServerUrl = "https://bbb.example.com/api/";
    options.SharedSecret = "your-shared-secret";
}, httpClientBuilder =>
{
    // Configure custom headers
    httpClientBuilder.ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.Add("X-Custom-Header", "value");
    });
    
    // Add retry policy
    httpClientBuilder.AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
    
    // Add circuit breaker policy
    httpClientBuilder.AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
});
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/BigBlueButton/issues)
- **Documentation**: [Wiki](https://github.com/yourusername/BigBlueButton/wiki)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/BigBlueButton/discussions)

## Changelog

### Version 1.0.0
- Initial release
- High-performance HTTP client with connection pooling
- Response caching support
- Comprehensive error handling and retry policies
- Full BigBlueButton API support
- Production-ready configuration options
