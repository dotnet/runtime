# Microsoft.Extensions.DependencyInjection.Abstractions

`Microsoft.Extensions.DependencyInjection.Abstractions` contains a core DI abstraction that allows for building different kinds of dependency injection containers to retrieve services from that have been registered with different lifetimes.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/dependency-injection.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](/src/libraries/README.md#primary-bar)

The APIs and functionality need more investment in the upcoming .NET releases.

## Deployment
[Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.

## Using other containers with Microsoft.Extensions.DependencyInjection

* [**Autofac**](https://autofac.readthedocs.org/en/latest/integration/aspnetcore.html)
* [**DryIoc**](https://www.nuget.org/packages/DryIoc.Microsoft.DependencyInjection)
* [**Grace**](https://www.nuget.org/packages/Grace.DependencyInjection.Extensions)
* [**Lamar**](https://www.nuget.org/packages/Lamar.Microsoft.DependencyInjection)
* [**LightInject**](https://github.com/seesharper/LightInject.Microsoft.DependencyInjection)
* [**StructureMap**](https://github.com/structuremap/StructureMap.Microsoft.DependencyInjection)
* [**Stashbox**](https://github.com/z4kn4fein/stashbox-extensions-dependencyinjection)
* [**Unity**](https://www.nuget.org/packages/Unity.Microsoft.DependencyInjection/)
