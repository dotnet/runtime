# Microsoft.Extensions.Diagnostics

`Microsoft.Extensions.Diagnostics` contains the default implementation of meter factory and extension methods for registering this default meter factory to the DI.

Commonly Used APIS:
- MetricsServiceExtensions.AddMetrics(this IServiceCollection services)
- MeterFactoryExtensions.Create(this IMeterFactory, string name, string? version = null, IEnumerable<KeyValuePair<string,object?>> tags = null,  object? scope = null)

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](https://github.com/dotnet/runtime/tree/main/src/libraries#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Diagnostics](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.