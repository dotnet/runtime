## About

<!-- A description of the package and where one can find more documentation -->

Provides the Managed Extensibility Framework (MEF) 1.0, an attribute based composition container for creating extensible, loosely coupled, and modular applications.

MEF simplifies application composition by enabling components to declare dependencies and capabilities through attributes (`[Export]` and `[Import]`). At runtime, these components are automatically discovered via catalogs and composed by a container without hardcoded dependencies.

For lightweight dependency injection or modern application development, consider using [System.Composition](https://www.nuget.org/packages/System.Composition) (MEF 2.0) or [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection).

## Key Features

<!-- The key features of this package -->

* Attribute based discovery and composition using `[Export]`, `[Import]`, and `[ImportMany]`.
* Flexible discovery through catalogs, including `AssemblyCatalog`, `DirectoryCatalog`, `TypeCatalog`, and `AggregateCatalog`.
* Dynamic composition, lifetime management, and dependency injection via `CompositionContainer`.
* Support for metadata and strongly typed metadata views.
* Recomposition support when new parts become available.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example demonstrates building an extensible application using `AggregateCatalog`, declaring operations with `[Export]`, and dynamically discovering and importing all available extensions via `[ImportMany]`:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

// Use an AggregateCatalog to discover parts from assemblies and/or plugin directories
var catalog = new AggregateCatalog();
catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
// Optional: catalog.Catalogs.Add(new DirectoryCatalog("./extensions"));

using var container = new CompositionContainer(catalog);

var calculator = new SimpleCalculator();
container.ComposeParts(calculator);

Console.WriteLine($"10 + 5 = {calculator.Compute("+", 10, 5)}");
Console.WriteLine($"10 - 5 = {calculator.Compute("-", 10, 5)}");
// Output:
// 10 + 5 = 15
// 10 - 5 = 5

public interface IOperation
{
    string Symbol { get; }
    int Operate(int left, int right);
}

[Export(typeof(IOperation))]
public class AddOperation : IOperation
{
    public string Symbol => "+";
    public int Operate(int left, int right) => left + right;
}

[Export(typeof(IOperation))]
public class SubtractOperation : IOperation
{
    public string Symbol => "-";
    public int Operate(int left, int right) => left - right;
}

public class SimpleCalculator
{
    // MEF automatically discovers and injects all exported operations
    [ImportMany(typeof(IOperation))]
    public IEnumerable<IOperation> Operations { get; set; } = [];

    public int Compute(string symbol, int left, int right)
    {
        foreach (var operation in Operations)
        {
            if (operation.Symbol == symbol)
            {
                return operation.Operate(left, right);
            }
        }

        throw new InvalidOperationException($"Operation '{symbol}' not found.");
    }
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.ComponentModel.Composition.ExportAttribute`
* `System.ComponentModel.Composition.ImportAttribute`
* `System.ComponentModel.Composition.ImportManyAttribute`
* `System.ComponentModel.Composition.PartCreationPolicyAttribute`
* `System.ComponentModel.Composition.AttributedModelServices`
* `System.ComponentModel.Composition.Hosting.CompositionContainer`
* `System.ComponentModel.Composition.Hosting.AggregateCatalog`
* `System.ComponentModel.Composition.Hosting.AssemblyCatalog`
* `System.ComponentModel.Composition.Hosting.DirectoryCatalog`
* `System.ComponentModel.Composition.Hosting.TypeCatalog`
* `System.ComponentModel.Composition.Primitives.ComposablePart`
* `System.ComponentModel.Composition.Primitives.ComposablePartCatalog`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.componentmodel.composition)
* [Managed Extensibility Framework (MEF) overview](https://learn.microsoft.com/dotnet/framework/mef/)

## Related Packages

<!-- The related packages associated with this package -->

* Convention based configuration for MEF: [System.ComponentModel.Composition.Registration](https://www.nuget.org/packages/System.ComponentModel.Composition.Registration)
* Lightweight MEF 2.0 container: [System.Composition](https://www.nuget.org/packages/System.Composition)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.ComponentModel.Composition is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
