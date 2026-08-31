## About

<!-- A description of the package and where one can find more documentation -->

`System.Composition.TypedParts` is part of the Managed Extensibility Framework (MEF) 2.0, a composition library for .NET that enables dependency injection through attributes or conventions.

Provides `ContainerConfiguration` and some extension methods for the Managed Extensibility Framework (MEF).

## Key Features

<!-- The key features of this package -->

* Provides container configuration.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Register parts from an entire assembly.

```csharp
using System.Composition;
using System.Composition.Hosting;
using System.Reflection;

// Register all parts from the current assembly
var configuration = new ContainerConfiguration()
    .WithAssembly(Assembly.GetExecutingAssembly());

using CompositionHost container = configuration.CreateContainer();

var handlers = container.GetExports<IHandler>();
foreach (var handler in handlers)
{
    handler.Handle();
}
// HandlerA is handling.
// HandlerB is handling.

public interface IHandler
{
    void Handle();
}

[Export(typeof(IHandler))]
public class HandlerA : IHandler
{
    public void Handle() => Console.WriteLine("HandlerA is handling.");
}

[Export(typeof(IHandler))]
public class HandlerB : IHandler
{
    public void Handle() => Console.WriteLine("HandlerB is handling.");
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Composition.Hosting.ContainerConfiguration`
* `System.Composition.CompositionContextExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/dotnet/framework/mef/)

## Related Packages

<!-- The related packages associated with this package -->

* [System.Composition](https://www.nuget.org/packages/System.Composition)
* [System.Composition.AttributedModel](https://www.nuget.org/packages/System.Composition.AttributedModel)
* [System.Composition.Convention](https://www.nuget.org/packages/System.Composition.Convention)
* [System.Composition.Hosting](https://www.nuget.org/packages/System.Composition.Hosting)
* [System.Composition.Runtime](https://www.nuget.org/packages/System.Composition.Runtime)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Composition.TypedParts is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
