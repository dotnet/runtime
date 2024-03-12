## About

<!-- A description of the package and where one can find more documentation -->
`Microsoft.Extensions.Logging.Debug` provides a Debug output logger provider implementation for Microsoft.Extensions.Logging. This logger logs messages to a debugger monitor by writing messages with `System.Diagnostics.Debug.WriteLine()`.

## Key Features

<!-- The key features of this package -->

* Allow logging to the debugger output.
* Provide extensions method for the [ILoggingBuilder](https://docs.microsoft.com/dotnet/api/microsoft.extensions.logging.iloggingbuilder) class to easily enable this Debug logger.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->
```csharp
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace DebugLoggerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a logger factory with a debug provider
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());

            // Create a logger with the category name of the current class
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            // Log some messages with different log levels and message templates
            logger.LogTrace("This is a trace message.");
            logger.LogDebug("This is a debug message.");
            logger.LogInformation("Hello {Name}!", "World");
            logger.LogWarning("This is a warning message.");
            logger.LogError("This is an error message.");
            logger.LogCritical("This is a critical message.");

            // Use structured logging to capture complex data
            var person = new Person { Name = "Alice", Age = 25 };
            logger.LogInformation("Created a new person: {@Person}", person);

            // Use exception logging to capture the details of an exception
            try
            {
                throw new Exception("Something went wrong.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occurred.");
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    // A simple class to demonstrate structured logging
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}

```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `DebugLoggerProvider`
* `DebugLoggerFactoryExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/logging)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging)

## Related Packages
[Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
[Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
[Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console)
[Microsoft.Extensions.Logging.EventSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventSource)
[Microsoft.Extensions.Logging.EventLog](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventLog)
[Microsoft.Extensions.Logging.TraceSource](https://www.nuget.org/packages/Microsoft.Extensions.Logging.TraceSource)

<!-- The related packages associated with this package -->

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Logging.Debug is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).