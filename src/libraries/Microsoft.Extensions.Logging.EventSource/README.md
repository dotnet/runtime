# Microsoft.Extensions.Logging.EventSource

`Microsoft.Extensions.Logging.EventSource` provides a basic implementation for the built-in event source logger provider. Using `Microsoft.Extensions.Logging.EventSource.LoggingEventSource` which is the bridge from all ILogger-based logging to EventSource/EventListener logging, logging can be enabled by enabling the event source called "Microsoft-Extensions-Logging".

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/logging.

## Important Notes

**Logger Instance Caching**: The `EventSourceLoggerProvider` does not cache logger instances internally. Each call to `CreateLogger(string categoryName)` creates and returns a new `EventSourceLogger` instance. This is different from other logging providers like the console logger provider, which caches logger instances by category name.

If your application creates loggers for the same category name multiple times, you should cache the logger instances yourself to avoid creating unnecessary instances. The standard `ILoggerFactory` implementation already caches logger instances per category name, so this is typically not a concern when using the logging framework in the normal way (e.g., through dependency injection).

However, if you are directly calling `ILoggerProvider.CreateLogger` multiple times with the same category name, you may want to implement your own caching mechanism to prevent creating multiple logger instances.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging.EventSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventSource) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.