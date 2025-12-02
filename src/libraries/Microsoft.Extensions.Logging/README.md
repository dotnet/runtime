# Microsoft.Extensions.Logging

`Microsoft.Extensions.Logging` is combined with a core logging abstraction under `Microsoft.Extensions.Logging.Abstractions`. This abstraction is available in our basic built-in implementations like console, event log, and debug (Debug.WriteLine). Also note, there is no dedicated built-in solution for file-based logging.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/logging.

## Examples

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

###  Baggage and Tags for `ActivityTrackingOptions`

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

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) is not included in the shared framework. The package is deployed as out-of-band (OOB) and needs to be installed into projects directly.