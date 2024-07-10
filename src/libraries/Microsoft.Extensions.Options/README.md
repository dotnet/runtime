# Microsoft.Extensions.Options

`Microsoft.Extensions.Options` provides a strongly typed way of specifying and accessing settings using dependency injection and acts as a bridge between configuration, DI, and higher level libraries. This library is the glue for how an app developer uses DI to configure the behavior of a library like HttpClient Factory. This also enables user to get a strongly-typed view of their configuration.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/options.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)

Although the types are mature, the code base continues to evolve for better performance.

## Deployment
[Microsoft.Extensions.Options](https://www.nuget.org/packages/Microsoft.Extensions.Options) is not included in the shared framework. The package is deployed as out-of-band (OOB) and needs to be installed into projects directly.
