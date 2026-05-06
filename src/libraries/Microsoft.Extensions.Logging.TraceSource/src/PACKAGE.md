## About

<!-- A description of the package and where one can find more documentation -->

Implements a trace logger provider for the .NET logging infrastructre facilitating enhanced logging capabilities and trace-level diagnostics in application by writing messages to a trace listener using System.Diagnostic.TraceSource.

## Key Features

<!-- The key features of this package -->

* Seamless integration with .NET logging infrastructure.
* Fine-grained control over trace messages using SourceSwitch.
* A set of builder methods to configure logging infrastructure.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The Microsoft.Extensions.Logging.TraceSource library provides extension methods to the logger factory and the logger builder to add a trace source with trace listeners.

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;

using var consoleTraceListener = new ConsoleTraceListener();
using var textWriterTraceListener = new TextWriterTraceListener("/traces.txt");
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddTraceSource(new SourceSwitch("Something") { Level = SourceLevels.All }, consoleTraceListener)
        .AddTraceSource(new SourceSwitch("HouseKeeping") { Level = SourceLevels.All }, textWriterTraceListener);
});

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Information message.");
// Program Information: 0 : Information message.
logger.LogWarning("Warning message.");
// Program Warning: 0 : Warning message.

var traceSource = new TraceSource("HouseKeeping", SourceLevels.All);
traceSource.Listeners.Add(consoleTraceListener);
traceSource.Listeners.Add(textWriterTraceListener);

traceSource.TraceEvent(TraceEventType.Error, 0, "Error message.");
//HouseKeeping Error: 0 : Error message.
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Logging.TraceSource.TraceSourceLoggerProvider`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.tracesource)

## Related Packages

<!-- The related packages associated with this package -->

* Abstractions for dependency injection: [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/)
* Default implementation of logging infrastructure: [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/)
* Abstractions for logging: [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Logging.TraceSource is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).