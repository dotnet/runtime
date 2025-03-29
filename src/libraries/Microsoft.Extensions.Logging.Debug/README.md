# Microsoft.Extensions.Logging.Debug

`Microsoft.Extensions.Logging.Debug` provides a Debug output logger provider implementation for Microsoft.Extensions.Logging. This logger logs messages to a debugger monitor by writing messages with `System.Diagnostics.Debug.WriteLine()`.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/logging.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging.Debug](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Debug) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.