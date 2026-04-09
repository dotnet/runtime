# Microsoft.Extensions.Logging.Abstractions

`Microsoft.Extensions.Logging.Abstractions` provides abstractions of logging. Interfaces defined in this package are implemented by classes in [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/) and other logging packages.

Commonly Used Types:
- `Microsoft.Extensions.Logging.ILogger`
- `Microsoft.Extensions.Logging.ILoggerFactory`
- `Microsoft.Extensions.Logging.ILogger<TCategoryName>`
- `Microsoft.Extensions.Logging.LogLevel`
- `Microsoft.Extensions.Logging.Logger<T>`
- `Microsoft.Extensions.Logging.LoggerMessage`
- `Microsoft.Extensions.Logging.Abstractions.NullLogger`

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/logging.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.