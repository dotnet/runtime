## About

<!-- A description of the package and where one can find more documentation -->

`Microsoft.Extensions.Logging.Abstractions` provides abstractions of logging. Interfaces defined in this package are implemented by classes in [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/) and other logging packages.

This package includes a logging source generator that produces highly efficient and optimized code for logging message methods.

## Key Features

<!-- The key features of this package -->

* Define main logging abstraction interfaces like ILogger, ILoggerFactory, ILoggerProvider, etc.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

#### Custom logger provider implementation example

```C#
using Microsoft.Extensions.Logging;

public sealed class ColorConsoleLogger : ILogger
{
    private readonly string _name;
    private readonly Func<ColorConsoleLoggerConfiguration> _getCurrentConfig;

    public ColorConsoleLogger(
        string name,
        Func<ColorConsoleLoggerConfiguration> getCurrentConfig) =>
        (_name, _getCurrentConfig) = (name, getCurrentConfig);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) =>
        _getCurrentConfig().LogLevelToColorMap.ContainsKey(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ColorConsoleLoggerConfiguration config = _getCurrentConfig();
        if (config.EventId == 0 || config.EventId == eventId.Id)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            Console.ForegroundColor = config.LogLevelToColorMap[logLevel];
            Console.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

            Console.ForegroundColor = originalColor;
            Console.Write($"     {_name} - ");

            Console.ForegroundColor = config.LogLevelToColorMap[logLevel];
            Console.Write($"{formatter(state, exception)}");

            Console.ForegroundColor = originalColor;
            Console.WriteLine();
        }
    }
}

```

#### Create logs

```csharp

// Worker class that uses logger implementation of teh interface ILogger<T>

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger) =>
        _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.UtcNow);
            await Task.Delay(1_000, stoppingToken);
        }
    }
}

```

#### Use source generator

```csharp
public static partial class Log
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Critical,
        Message = "Could not open socket to `{hostName}`")]
    public static partial void CouldNotOpenSocket(this ILogger logger, string hostName);
}

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

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Logging.ILogger`
* `Microsoft.Extensions.Logging.ILoggerProvider`
* `Microsoft.Extensions.Logging.ILoggerFactory`
* `Microsoft.Extensions.Logging.ILogger<TCategoryName>`
* `Microsoft.Extensions.Logging.LogLevel`
* `Microsoft.Extensions.Logging.Logger<T>`
* `Microsoft.Extensions.Logging.LoggerMessage`
* `Microsoft.Extensions.Logging.Abstractions.NullLogger`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/logging)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Related Packages

<!-- The related packages associated with this package -->
[Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
[Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console)
[Microsoft.Extensions.Logging.Debug](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Debug)
[Microsoft.Extensions.Logging.EventSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventSource)
[Microsoft.Extensions.Logging.EventLog](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventLog)
[Microsoft.Extensions.Logging.TraceSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.TraceSource)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Logging.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).