# Microsoft.Extensions.Logging.EventSource

`Microsoft.Extensions.Logging.EventSource` provides a basic implementation for the built-in event source logger provider. Using `Microsoft.Extensions.Logging.EventSource.LoggingEventSource` which is the bridge from all ILogger-based logging to EventSource/EventListener logging, logging can be enabled by enabling the event source called "Microsoft-Extensions-Logging".

Documentation can be found at https://learn.microsoft.com/en-us/dotnet/core/extensions/logging.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging.EventSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventSource) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.