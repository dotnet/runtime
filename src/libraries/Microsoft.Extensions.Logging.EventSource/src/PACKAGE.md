## About

`Microsoft.Extensions.Logging.EventSource` sends application logs from `ILogger` to the `Microsoft-Extensions-Logging` EventSource. You can collect the events with EventPipe or, on Windows, Event Tracing for Windows (ETW).

## Key Features

* Registers an EventSource provider with `AddEventSourceLogger()`.
* Supports formatted messages and structured event payloads selected by event keywords.

## How to Use

Register the provider and write a log message:

```csharp
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddEventSourceLogger());
ILogger logger = loggerFactory.CreateLogger("Worker");
logger.LogInformation("Started worker {WorkerId}", 42);
```

To collect the formatted messages, enable the `Microsoft-Extensions-Logging` provider with the `FormattedMessage` keyword (value `4`). See the provider documentation for keyword and filtering options.

## Main Types

* `EventSourceLoggerProvider`
* `LoggingEventSource`
* `EventSourceLoggerFactoryExtensions`

## Additional Documentation

* [EventSource logging provider](https://learn.microsoft.com/dotnet/core/extensions/logging-providers#event-source)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.eventsource)

## Related Packages

* [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/)

## Feedback & Contributing

This library is [MIT licensed](https://licenses.nuget.org/MIT). Report issues or contribute at the [.NET Runtime repository](https://github.com/dotnet/runtime).
