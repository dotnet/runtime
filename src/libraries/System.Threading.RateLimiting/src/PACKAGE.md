## About

<!-- A description of the package and where one can find more documentation -->

Provides a set of types that enable application developers to control the rate of operations.
This can be used to ensure that applications do not exceed certain limits when interacting with resources or services.

## Key Features

<!-- The key features of this package -->

* Flexible rate-limiting primitives that can be applied to various scenarios.
* Supports token bucket, fixed window, and sliding window strategies.
*

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

This is an example of an HttpClient that does client side rate limiting.

Define a rate limiter.

```csharp
internal sealed class ClientSideRateLimitedHandler : DelegatingHandler, IAsyncDisposable
{
    private readonly RateLimiter _rateLimiter;

    public ClientSideRateLimitedHandler(RateLimiter limiter)
        : base(new HttpClientHandler())
    {
        _rateLimiter = limiter;
    }

    // Override the SendAsync method to apply rate limiting.
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Try to acquire a token from the rate limiter.
        using RateLimitLease lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);

        // If a token is acquired, proceed with sending the request.
        if (lease.IsAcquired)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // If no token could be acquired, simulate a 429 Too Many Requests response.
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        // Add a 'Retry-After' header if the rate limiter provides a retry delay.
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            response.Headers.Add("Retry-After", ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
        }

        return response;
    }

    // Implement IAsyncDisposable to allow for asynchronous cleanup of resources.
    public async ValueTask DisposeAsync()
    {
        // Dispose of the rate limiter asynchronously.
        await _rateLimiter.DisposeAsync().ConfigureAwait(false);

        // Call the base Dispose method.
        Dispose(disposing: false);

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    // Dispose pattern to clean up the rate limiter.
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // Synchronously dispose of the rate limiter if disposing is true.
            _rateLimiter.Dispose();
        }
    }
}
```

Using the rate limiter.

```csharp
using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;

// Initialize the rate limiter options.
// TokenLimit: Maximum number of tokens that can be acquired at once.
// QueueProcessingOrder: The order in which queued requests will be processed.
// QueueLimit: Maximum number of queued requests.
// ReplenishmentPeriod: How often tokens are replenished.
// TokensPerPeriod: Number of tokens added each period.
// AutoReplenishment: If true, tokens are replenished automatically in the background.
var options = new TokenBucketRateLimiterOptions
{
    TokenLimit = 4,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    QueueLimit = 2,
    ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
    TokensPerPeriod = 2,
    AutoReplenishment = true
};

// Create a new instance of the TokenBucketRateLimiter with the defined options.
TokenBucketRateLimiter tokenBucketRateLimiter = new TokenBucketRateLimiter(options);

// A custom HttpMessageHandler that limits the rate of outgoing HTTP requests.
ClientSideRateLimitedHandler clientsideRateLimitedHandler = new ClientSideRateLimitedHandler(tokenBucketRateLimiter);

// Create an HttpClient that uses the rate-limited handler.
using HttpClient client = new HttpClient(clientsideRateLimitedHandler);

// Generate a list of dummy URLs for testing the rate limiter.
var oneHundredUrls = Enumerable.Range(0, 100).Select(i => $"https://example.com?iteration={i:00}");

// Issue concurrent HTTP GET requests using the HttpClient.
// The rate limiter will control how many requests are sent based on the defined limits.
await Parallel.ForEachAsync(oneHundredUrls.Take(0..100), async (url, cancellationToken) =>
{
    using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
    Console.WriteLine($"URL: {url}, HTTP status code: {response.StatusCode} ({(int)response.StatusCode})");
});
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Threading.RateLimiting.RateLimiter`
* `System.Threading.RateLimiting.ConcurrencyLimiter`
* `System.Threading.RateLimiting.FixedWindowRateLimiter`
* `System.Threading.RateLimiting.ReplenishingRateLimiter`
* `System.Threading.RateLimiting.SlidingWindowRateLimiter`
* `System.Threading.RateLimiting.TokenBucketRateLimiter`
* `System.Threading.RateLimiting.PartitionedRateLimiter<TResource>`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/http-ratelimiter)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.threading.ratelimiting)

## Related Packages

<!-- The related packages associated with this package -->

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Threading.RateLimiting is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
