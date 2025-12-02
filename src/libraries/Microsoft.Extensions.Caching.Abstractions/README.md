# Microsoft.Extensions.Caching

Caching is combined with a core caching abstraction under `Microsoft.Extensions.Caching.Abstractions` that allows for implementing general purpose memory and distributed caches, with integration for Redis and SqlServer.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/caching.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)

The APIs and functionality need more investment in the upcoming .NET releases.

## Deployment
[Microsoft.Extensions.Caching.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Abstractions) is not included in the shared framework. The package is deployed as out-of-band (OOB) and needs to be installed into projects directly.
