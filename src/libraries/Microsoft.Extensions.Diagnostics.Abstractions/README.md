# Microsoft.Extensions.Diagnostics.Abstractions

`Microsoft.Extensions.Diagnostics.Abstractions` provides abstractions of diagnostics. Interfaces defined in this package are implemented by classes in [Microsoft.Extensions.Diagnostics](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics/) and other diagnostics packages.

Commonly Used Types:
- `Microsoft.Extensions.Diagnostics.Metrics.IMetricsBuilder`
- `Microsoft.Extensions.Diagnostics.Metrics.IMetricsListener`
- `Microsoft.Extensions.Diagnostics.Metrics.InstrumentRule`
- `Microsoft.Extensions.Diagnostics.Metrics.MeterScope`
- `Microsoft.Extensions.Diagnostics.Metrics.MetricsBuilderExtensions`
- `Microsoft.Extensions.Diagnostics.Metrics.MetricsOptions`

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/diagnostics.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](/src/libraries/README.md#primary-bar)

The APIs and functionality are new in .NET 8 and will continue to be developed.

## Deployment
[Microsoft.Extensions.Diagnostics.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.Abstractions) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.
