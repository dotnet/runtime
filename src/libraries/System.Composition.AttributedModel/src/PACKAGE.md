## About

<!-- A description of the package and where one can find more documentation -->

`System.Composition.AttributedModel` is part of the Managed Extensibility Framework (MEF) 2.0, a composition library for .NET that enables dependency injection through attributes or conventions.

This package provides the foundational attributes that allow you to declare parts for composition, such as imports, exports, and metadata.
It is used to mark classes, properties, methods, and constructors for MEF's discovery and composition process.

## Key Features

<!-- The key features of this package -->

* Provides attributes for declaring composable parts, importing dependencies, metadata, and creating shared or non-shared parts

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Mark classes for export and import using attributes.

```csharp
using System.Composition;
using System.Composition.Hosting;

var configuration = new ContainerConfiguration()
    .WithPart<Service>()
    .WithPart<Application>();

using CompositionHost container = configuration.CreateContainer();

Application app = container.GetExport<Application>();
app.Service.Execute();
// Service is running!

[Export]
public class Application
{
    [Import]
    public Service Service { get; set; }
}

[Export]
public class Service
{
    public void Execute() => Console.WriteLine("Service is running!");
}
```

Metadata can be used to differentiate between multiple exports of the same contract.

```csharp
using System.Composition;
using System.Composition.Hosting;

ContainerConfiguration configuration = new ContainerConfiguration()
    .WithPart<FileLogger>()
    .WithPart<ConsoleLogger>()
    .WithPart<Application>();

using CompositionHost container = configuration.CreateContainer();

Application app = container.GetExport<Application>();
app.Run();
// Using FileLogger to log.
// FileLogger: Hello, World!
// Using ConsoleLogger to log.
// ConsoleLogger: Hello, World!

public interface ILogger
{
    void Log(string message);
}

[Export(typeof(ILogger))]
[ExportMetadata("Name", "FileLogger")]
public class FileLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"FileLogger: {message}");
}

[Export(typeof(ILogger))]
[ExportMetadata("Name", "ConsoleLogger")]
public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"ConsoleLogger: {message}");
}

[Export]
public class Application
{
    [ImportMany]
    public required IEnumerable<Lazy<ILogger, IDictionary<string, object>>> Loggers { get; set; }

    public void Run()
    {
        foreach (var logger in Loggers)
        {
            var name = logger.Metadata["Name"];

            Console.WriteLine($"Using {name} to log.");
            logger.Value.Log("Hello, World!");
        }
    }
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Composition.ExportAttribute`
* `System.Composition.ImportAttribute`
* `System.Composition.ExportMetadataAttribute`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.composition)
* [Managed Extensibility Framework (MEF)](https://learn.microsoft.com/dotnet/framework/mef/)

## Related Packages

<!-- The related packages associated with this package -->

* [System.Composition](https://www.nuget.org/packages/System.Composition)
* [System.Composition.Convention](https://www.nuget.org/packages/System.Composition.Convention)
* [System.Composition.Hosting](https://www.nuget.org/packages/System.Composition.Hosting)
* [System.Composition.Runtime](https://www.nuget.org/packages/System.Composition.Runtime)
* [System.Composition.TypedParts](https://www.nuget.org/packages/System.Composition.TypedParts)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Composition.AttributedModel is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
