## About

<!-- A description of the package and where one can find more documentation -->

Provides the Managed Extensibility Framework (MEF) 2.0, a lightweight, attribute-driven Dependency Injection (DI) container.

MEF simplifies the composition of applications by allowing components to be loosely coupled and dynamically discovered.
This package supports the development of modular and maintainable applications by enabling parts to be composed at runtime.

## Key Features

<!-- The key features of this package -->

* Components are discovered and composed using attributes.
* Provides dependency injection capabilities for loosely coupled modules.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Running code from a discovered component.

```csharp
using System.Composition;
using System.Composition.Hosting;

var configuration = new ContainerConfiguration().WithPart<Service>();

using var container = configuration.CreateContainer();

var service = container.GetExport<Service>();
service.Execute();
// Output: Service is running!

[Export]
public class Service
{
    public void Execute() => Console.WriteLine("Service is running!");
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Composition.ExportAttribute`
* `System.Composition.ImportAttribute`
* `System.Composition.Convention.ConventionBuilder`
* `System.Composition.Hosting.CompositionHost`
* `System.Composition.CompositionContext`
* `System.Composition.CompositionContextExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.composition)
* [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/dotnet/framework/mef/)

## Related Packages

<!-- The related packages associated with this package -->

* [System.Composition.AttributedModel](https://www.nuget.org/packages/System.Composition.AttributedModel)
* [System.Composition.Convention](https://www.nuget.org/packages/System.Composition.Convention)
* [System.Composition.Hosting](https://www.nuget.org/packages/System.Composition.Hosting)
* [System.Composition.Runtime](https://www.nuget.org/packages/System.Composition.Runtime)
* [System.Composition.TypedParts](https://www.nuget.org/packages/System.Composition.TypedParts)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Composition is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
