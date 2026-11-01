## About

<!-- A description of the package and where one can find more documentation -->

`Microsoft.Extensions.Logging.EventLog` provides a logger provider for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/) that writes log messages to the Windows Event Log, where they can be viewed in Event Viewer or collected by monitoring tools. It is supported on Windows only.

Applications built with the .NET Generic Host on Windows get this provider automatically, filtered to `Warning` and above by default, so the package is mainly used directly when configuring logging without the host or when customizing the event log settings.

## Key Features

<!-- The key features of this package -->

* Write log messages to a Windows event log, by default the `Application` log with the `.NET Runtime` source.
* Configure the log name, source name, machine name and a log level filter through `EventLogSettings`.
* Map log levels to event log entry types: `Error` and `Critical` to Error, `Warning` to Warning, and other levels to Information.
* Include the logger category, the event ID and any active scopes in each event's message.
* Split messages that are longer than the event log limit into several entries.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The following example writes events to the `Application` log with the default `.NET Runtime` source:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddEventLog(new EventLogSettings
    {
        Filter = (category, level) => level >= LogLevel.Information
    });
});

ILogger logger = loggerFactory.CreateLogger("Sample");

logger.LogInformation("Order {OrderId} was processed.", 42);
logger.LogWarning("Disk space is running low.");
logger.LogError(new InvalidOperationException("Payment failed."), "Order {OrderId} could not be completed.", 43);
```

When `SourceName` is not set, every event is written with event ID 1000. To use your own source, set `SourceName`; the event ID passed to the log call is then used. The source must exist before the first event is written, and creating it requires administrator rights, so it is usually registered once during installation, for example with [EventLog.CreateEventSource](https://learn.microsoft.com/dotnet/api/system.diagnostics.eventlog.createeventsource). If the source cannot be created, the logger stops writing and reports the failure once in the `Application` log.

When using the .NET Generic Host, the provider is configured through the `EventLog` section of the logging configuration, for example in `appsettings.json`:

```json
{
  "Logging": {
    "EventLog": {
      "SourceName": "MyApp",
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider`
* `Microsoft.Extensions.Logging.EventLog.EventLogSettings`
* `Microsoft.Extensions.Logging.EventLoggerFactoryExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/logging-providers#windows-eventlog)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.eventlog)

## Related Packages

<!-- The related packages associated with this package -->

* [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
* [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
* [Microsoft.Extensions.Logging.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Configuration)
* [System.Diagnostics.EventLog](https://www.nuget.org/packages/System.Diagnostics.EventLog)
* [Microsoft.Extensions.Hosting.WindowsServices](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.WindowsServices)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Logging.EventLog is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
