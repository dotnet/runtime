# Microsoft.Extensions.Logging.TraceSource

`Microsoft.Extensions.Logging.TraceSource` provides a basic implementation for the built-in TraceSource logger provider. This logger logs messages to a trace listener by writing messages with `System.Diagnostics.TraceSource.TraceEvent()`.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/logging.

## Example

The following example shows how to display logs to a trace listener.

```cs
using System;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main(string[] args)
    {
        using (var textWriterTraceListener = new TextWriterTraceListener(@"C:\logs\trace.log"))
        using (var consoleTraceListener = new ConsoleTraceListener())
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddTraceSource(new SourceSwitch("Something") { Level = SourceLevels.All }, consoleTraceListener)
                    .AddTraceSource(new SourceSwitch("HouseKeeping") { Level = SourceLevels.All }, textWriterTraceListener);
                    // writer: Console.Out));
            });

            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("LogInformation information");
            logger.LogWarning("LogWarning warning");

            var ts = new TraceSource("HouseKeeping", SourceLevels.All);
            ts.Listeners.Add(consoleTraceListener);
            ts.Listeners.Add(textWriterTraceListener);
            ts.TraceEvent(TraceEventType.Error, 0, "trace error");
        }
    }
}
```

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging.TraceSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.TraceSource) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.