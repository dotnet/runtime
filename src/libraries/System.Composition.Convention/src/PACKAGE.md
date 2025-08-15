## About

<!-- A description of the package and where one can find more documentation -->

`System.Composition.Convention` is part of the Managed Extensibility Framework (MEF) 2.0, a composition library for .NET that enables dependency injection through attributes or conventions.

This package simplifies the process of applying consistent patterns for part exports, imports, and metadata by using convention-based configurations.
It is useful for scenarios where you want to avoid repetitive attribute-based decoration and instead define conventions for registering types in your composition container.

## Key Features

<!-- The key features of this package -->

* Configure exports, imports, and metadata for parts using conventions rather than attributes.
* Allows defining conventions through a fluent API, making configuration more flexible and readable.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Configure parts for composition without using attributes.

```csharp
using System.Composition.Convention;
using System.Composition.Hosting;

var conventions = new ConventionBuilder();

// Apply conventions: any class that implements ILogger will be exported as ILogger
conventions
    .ForTypesDerivedFrom<ILogger>()
    .Export<ILogger>();

var configuration = new ContainerConfiguration()
    .WithPart<FileLogger>(conventions)
    .WithPart<ConsoleLogger>(conventions);

using CompositionHost container = configuration.CreateContainer();
    
var loggers = container.GetExports<ILogger>();

foreach (var logger in loggers)
{
    logger.Log("Hello, World!");
}
// FileLogger: Hello, World!
// ConsoleLogger: Hello, World!

public interface ILogger
{
    void Log(string message);
}

public class FileLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"FileLogger: {message}");
}

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"ConsoleLogger: {message}");
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Composition.Convention.ConventionBuilder`
* `System.Composition.Convention.PartConventionBuilder`
* `System.Composition.Convention.ParameterImportConventionBuilder`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.composition.convention)
* [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/dotnet/framework/mef/)

## Related Packages

<!-- The related packages associated with this package -->

* [System.Composition](https://www.nuget.org/packages/System.Composition)
* [System.Composition.AttributedModel](https://www.nuget.org/packages/System.Composition.AttributedModel)
* [System.Composition.Hosting](https://www.nuget.org/packages/System.Composition.Hosting)
* [System.Composition.Runtime](https://www.nuget.org/packages/System.Composition.Runtime)
* [System.Composition.TypedParts](https://www.nuget.org/packages/System.Composition.TypedParts)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Composition.Convention is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
