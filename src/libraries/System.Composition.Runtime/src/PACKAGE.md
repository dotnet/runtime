## About

<!-- A description of the package and where one can find more documentation -->

`System.Composition.Runtime` is part of the Managed Extensibility Framework (MEF) 2.0, a composition library for .NET that enables dependency injection through attributes or conventions.

This package enables the discovery and composition of parts in applications using MEF 2.0.
It offers the runtime implementation needed for managing composable parts, resolving dependencies, and dynamically wiring components together.

## Key Features

<!-- The key features of this package -->

* Facilitates runtime discovery and composition of parts.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Resolve dependencies on the fly and can be useful for dynamically loaded components or plugins.

```csharp
using System.Composition;
using System.Composition.Hosting;

var configuration = new ContainerConfiguration()
    .WithPart<Service>();

using CompositionHost container = configuration.CreateContainer();
 
var consumer = new Consumer(container);
consumer.Execute();
// Service is running.

public interface IService
{
    void Run();
}

[Export(typeof(IService))]
public class Service : IService
{
    public void Run() => Console.WriteLine("Service is running.");
}

public class Consumer(CompositionContext context)
{
    public void Execute()
    {
        // Use the context to resolve the service
        var service = context.GetExport<IService>();
        service.Run();
    }
}
```

## Main Types

<!-- The main types provided in this library -->

The main type provided by this library is:

* `System.Composition.CompositionContext`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.composition.compositioncontext)
* [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/dotnet/framework/mef/)

## Related Packages

<!-- The related packages associated with this package -->

* [System.Composition](https://www.nuget.org/packages/System.Composition)
* [System.Composition.AttributedModel](https://www.nuget.org/packages/System.Composition.AttributedModel)
* [System.Composition.Convention](https://www.nuget.org/packages/System.Composition.Convention)
* [System.Composition.Hosting](https://www.nuget.org/packages/System.Composition.Hosting)
* [System.Composition.TypedParts](https://www.nuget.org/packages/System.Composition.TypedParts)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Composition.Runtime is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
