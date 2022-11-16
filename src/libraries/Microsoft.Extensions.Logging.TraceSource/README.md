# Microsoft.Extensions.Logging.TraceSource

`Microsoft.Extensions.Logging.TraceSource` provides a basic implementation for the built-in TraceSource logger provider. This logger logs messages to a trace listener by writing messages with `System.Diagnostics.TraceSource.TraceEvent()`.

Documentation can be found at https://learn.microsoft.com/en-us/dotnet/core/extensions/logging.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging.TraceSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.TraceSource) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.