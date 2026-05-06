## About

<!-- A description of the package and where one can find more documentation -->

`Microsoft.Extensions.Logging` is combined with a core logging abstraction under `Microsoft.Extensions.Logging.Abstractions`. This abstraction is available in our basic built-in implementations like console, event log, and debug (Debug.WriteLine) logging.

## Key Features

<!-- The key features of this package -->

* Provide concrete implementations of ILoggerFactory
* Provide extension methods for service collections, logger builder, and activity tracking
* Provide logging filtering extension methods for logger builder

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->
Prior to .NET 6, we only had two forms possible for doing logging, using `Microsoft.Extensions.Logging`:

```cs
public class LoggingSample1
{
    private ILogger _logger;

    public LoggingSample1(ILogger logger)
    {
        _logger = logger;
    }

    public void LogMethod(string name)
    {
        _logger.LogInformation("Hello {name}", name);
    }
}
```

Here are some problems with the LoggingSample1 sample using `LogInformation`, `LogWarning`, etc.:

1. We can provide event ID through these APIs, but they are not required today. Which leads to bad usages in real systems that want to react or detect specific event issues being logged.
2. Parameters passed are processed before LogLevel checks; this leads to unnecessary code paths getting triggered even when logging is disabled for a log level.
3. It requires parsing of message string on every use to find templates to substitute.

Because of these problems, the more efficient runtime approach recommended as best practices is to use LoggerMessage.Define APIs instead, illustrated below with LoggingSample2:

```cs
public class LoggingSample2
{
    private ILogger _logger;

    public LoggingSample2(ILogger logger)
    {
        _logger = logger;
    }

    public void LogMethod(string name)
    {
        Log.LogName(_logger, name);
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception> _logName = LoggerMessage.Define<string>(LogLevel.Information, 0, @"Hello {name}");

        public static void LogName(ILogger logger, string name)
        {
            _logName(logger, name, null!);
        }
    }
}
```

To reach a balance between performance and usability we added the compile-time logging source generator feature in .NET 6, to learn more about it and learn how to use a source generator to create log messages check out [this documentation](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator).

```csharp

public partial class InstanceLoggingExample
{
    private readonly ILogger _logger;

    public InstanceLoggingExample(ILogger logger)
    {
        _logger = logger;
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Critical,
        Message = "Could not open socket to `{hostName}`")]
    public partial void CouldNotOpenSocket(string hostName);
}
```

####  Baggage and Tags for `ActivityTrackingOptions`

.NET 5.0 exposed a new feature that allows configuring the logger builder with the `ActivityTrackingOption` to add the tracing context Span Id, Trace Id, Parent Id, Trace state, and Trace flags to the logging scope. The tracing context usually carried in `Activity.Current`.

.NET 6.0 Preview 1 extended this feature to include more tracing context properties which are the Baggage and the Tags:

```cs
  var loggerFactory = LoggerFactory.Create(logging =>
  {
      logging.Configure(options =>
      {
          options.ActivityTrackingOptions = ActivityTrackingOptions.Tags | ActivityTrackingOptions.Baggage;
      }).AddSimpleConsole(options =>
      {
          options.IncludeScopes = true;
      });
  });
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* LoggingServiceCollectionExtensions
* LoggerFactory
* LoggerFactoryOptions
* LoggingBuilderExtensions
* ActivityTrackingOptions
* FilterLoggingBuilderExtensions

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/logging)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Related Packages

<!-- The related packages associated with this package -->
[Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
[Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console)
[Microsoft.Extensions.Logging.Debug](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Debug)
[Microsoft.Extensions.Logging.EventSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventSource)
[Microsoft.Extensions.Logging.EventLog](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventLog)
[Microsoft.Extensions.Logging.TraceSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.TraceSource)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Logging is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).