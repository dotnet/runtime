## About

<!-- A description of the package and where one can find more documentation -->

`System.Composition.Hosting` is part of the Managed Extensibility Framework (MEF) 2.0, a composition library for .NET that enables dependency injection through attributes or conventions.

This package provides core services for creating composition containers.
It offers tools to configure and manage the composition of parts within your application, facilitating dependency injection and enabling modular architectures.

## Key Features

<!-- The key features of this package -->

* Create and manage composition containers for dynamic dependency injection.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Create a composition host and compose parts.

```csharp
using System.Composition;
using System.Composition.Hosting;

// Create a container configuration
var configuration = new ContainerConfiguration()
    .WithPart<Service>();

// Create the composition host (container)
using CompositionHost container = configuration.CreateContainer();

// Get an instance of the service
var service = container.GetExport<IService>();
service.Run();
// Service is running!

public interface IService
{
    void Run();
}

[Export(typeof(IService))]
public class Service : IService
{
    public void Run() => Console.WriteLine("Service is running!");
}
```

## Main Types

<!-- The main types provided in this library -->

The main type provided by this library is:

* `System.Composition.CompositionHost`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.composition.hosting)
* [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/dotnet/framework/mef/)

## Related Packages

<!-- The related packages associated with this package -->

* [System.Composition](https://www.nuget.org/packages/System.Composition)
* [System.Composition.AttributedModel](https://www.nuget.org/packages/System.Composition.AttributedModel)
* [System.Composition.Convention](https://www.nuget.org/packages/System.Composition.Convention)
* [System.Composition.Runtime](https://www.nuget.org/packages/System.Composition.Runtime)
* [System.Composition.TypedParts](https://www.nuget.org/packages/System.Composition.TypedParts)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Composition.Hosting is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
