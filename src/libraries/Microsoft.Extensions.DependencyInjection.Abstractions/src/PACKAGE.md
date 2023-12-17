## About
Supports the lower-level abstractions for the dependency injection (DI) software design pattern which is a technique for achieving Inversion of Control (IoC) between classes and their dependencies.

## Key Features
- Interfaces for DI implementations which are provided in other packages including `Microsoft.Extensions.DependencyInjection`.
- An implementation of a service collection, which is used to add services to and later retrieve them either directly or through constructor injection.
- Interfaces, attributes and extensions methods to support various DI concepts including specifying a service's lifetime and supporting keyed services.

## How to Use
This package is typically used with an implementation of the DI abstractions, such as `Microsoft.Extensions.DependencyInjection`.

## Main Types
The main types provided by this library are:
* `Microsoft.Extensions.DependencyInjection.ActivatorUtilities`
* `Microsoft.Extensions.DependencyInjection.IServiceCollection`
* `Microsoft.Extensions.DependencyInjection.ServiceCollection`
* `Microsoft.Extensions.DependencyInjection.ServiceCollectionDescriptorExtensions`
* `Microsoft.Extensions.DependencyInjection.ServiceDescriptor`
* `Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder>`

## Additional Documentation
* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
* API documentation
  - [ActivatorUtilities](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.defaultserviceproviderfactory)
  - [ServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.servicecollection)
  - [ServiceDescriptor](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.servicedescriptor)

## Related Packages
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Options`

## Feedback & Contributing
Microsoft.Extensions.DependencyInjection.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
