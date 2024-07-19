# Microsoft.Extensions.DependencyInjection

`Microsoft.Extensions.DependencyInjection` is combined with a core DI abstraction under `Microsoft.Extensions.DependencyInjection.Abstractions` that allows for building different kinds of dependency injection containers to retrieve services from that have been registered with different lifetimes.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/dependency-injection.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](/src/libraries/README.md#primary-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) is not included in the shared framework. The package is deployed as out-of-band (OOB) and needs to be installed into projects directly.

## Using other containers with Microsoft.Extensions.DependencyInjection

* [**Autofac**](https://autofac.readthedocs.org/en/latest/integration/aspnetcore.html)
* [**DryIoc**](https://www.nuget.org/packages/DryIoc.Microsoft.DependencyInjection)
* [**Grace**](https://www.nuget.org/packages/Grace.DependencyInjection.Extensions)
* [**Lamar**](https://www.nuget.org/packages/Lamar.Microsoft.DependencyInjection)
* [**LightInject**](https://github.com/seesharper/LightInject.Microsoft.DependencyInjection)
* [**StructureMap**](https://github.com/structuremap/StructureMap.Microsoft.DependencyInjection)
* [**Stashbox**](https://github.com/z4kn4fein/stashbox-extensions-dependencyinjection)
* [**Unity**](https://www.nuget.org/packages/Unity.Microsoft.DependencyInjection/)
