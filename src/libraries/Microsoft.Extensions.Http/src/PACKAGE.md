## About

<!-- A description of the package and where one can find more documentation -->

[Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) package provides `AddHttpClient` extension methods for `IServiceCollection`, `IHttpClientFactory` interface and its default implementation. This provides the ability to set up named `HttpClient` configurations in a DI container and later retrieve them via an injected `IHttpClientFactory` instance.

## Key Features

<!-- The key features of this package -->

* The package allows to fluently set up multiple `HttpClient` configurations for applications that use DI via `AddHttpClient` extension method.
* `HttpClientFactory` caches `HttpMessageHandler` instances per configuration name, which allows to reuse resources between `HttpClient` instances to avoid port exhaustion.
* `HttpClientFactory` manages lifetime of `HttpMessageHandler` instances and recycles connections to track DNS changes.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Note that lifetime management of `HttpClient` instances created by `HttpClientFactory` is completely different from instances created manually. The strategies are to use either short-lived clients created by `HttpClientFactory` or long-lived clients with `PooledConnectionLifetime` set up. For more information, see the [HttpClient lifetime management section](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory#httpclient-lifetime-management) in the conceptual docs and [Guidelines for using HTTP clients](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines).

### Configuring HttpClient

```c#
builder.Services.AddHttpClient("foo"); // adding an HttpClient named "foo" with a default configuration

builder.Services.AddHttpClient("example", c => c.BaseAddress = new Uri("https://www.example.com")) // configuring HttpClient itself
    .AddHttpMessageHandler<MyAuthHandler>() // adding additional delegating handlers to form a message handler chain
    .ConfigurePrimaryHttpMessageHandler(b => new HttpClientHandler() { AllowAutoRedirect = false }) // configuring primary handler
    .SetHandlerLifetime(TimeSpan.FromMinutes(30)); // changing the handler recycling interval
```

### Using the configured HttpClient

```c#
public class MyService
{
    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory; // injecting the factory
    }

    private Task<string> GetExampleAsync(Uri uri, CancellationToken ct)
    {
        HttpClient exampleClient = _httpClientFactory.CreateClient("example"); // creating the client for the specified name
        return exampleClient.GetStringAsync(uri, ct); // using the client
    }
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `IHttpClientFactory`
* `IHttpMessageHandlerFactory`
* `HttpClientFactoryServiceCollectionExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)
    * Also see [HttpClient guidelines](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines) conceptual doc
* [API documentation](https://learn.microsoft.com/dotnet/api/system.net.http?view=dotnet-plat-ext-7.0)
    * Also see [`AddHttpClient` extension method](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.httpclientfactoryservicecollectionextensions?view=dotnet-plat-ext-7.0) API doc

## Related Packages

<!-- The related packages associated with this package -->

* [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/)
* [Microsoft.Extensions.Http.Polly](https://www.nuget.org/packages/Microsoft.Extensions.Http.Polly)
* [Microsoft.Extensions.Http.Telemetry](https://www.nuget.org/packages/Microsoft.Extensions.Http.Telemetry)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Http is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
