## About

<!-- A description of the package and where one can find more documentation -->

This package provides types that allow applications to interact with the Windows Event Log service.

When an error occurs in a Windows machine, the system administrator or support representative must determine what caused the error, attempt to recover any lost data, and prevent the error from recurring. It is helpful if applications, the operating system, and other system services record important events, such as low-memory conditions or excessive attempts to access a disk. The system administrator can then use the Windows Event Log to help determine what conditions caused the error and identify the context in which it occurred.

## Key Features

<!-- The key features of this package -->

* Allows reading from existing logs.
* Allows writing entries to logs.
* Can create or delete event sources.
* Can delete logs.
* Can respond to log entries.
* Can create new logs when creating an event source.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```cs
if(!EventLog.SourceExists("MySource"))
{
    // An event log source should not be created and immediately used.
    // There is a latency time to enable the source, it should be created
    // prior to executing the application that uses the source.
    // Execute this sample a second time to use the new source.
    EventLog.CreateEventSource("MySource", "MyNewLog");
    Console.WriteLine("Event source created. Exiting, execute the application a second time to use the source.");
    // The source is created. Exit the application to allow it to be registered.
    return;
}

EventLog myLog = new();
myLog.Source = "MySource";
myLog.WriteEntry("Writing an informational entry to the event log.");
```

Notes:

- This assembly is only supported on Windows operating systems.
- Starting with Windows Vista, you must run the application as an administrator to interact with the Windows Event Log service using the `System.Diagnostics.EventLog` class.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

Under the [`System.Diagnostics`](https://learn.microsoft.com/dotnet/api/System.Diagnostics) namespace, the main types are:

- [`System.Diagnostics.EventLog`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.EventLog)
- [`System.Diagnostics.EventLogEntry`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.EventLogEntry)
- [`System.Diagnostics.EventLogEntryCollection`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.EventLogEntryCollection)
- [`System.Diagnostics.EventLogEntryType`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.EventLogEntryType)

Under the [`System.Diagnostics.Eventing.Reader`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.Eventing.Reader) namespace, the main types are:

- [`System.Diagnostics.Eventing.Reader.EventLogQuery`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogQuery)
- [`System.Diagnostics.Eventing.Reader.EventLogReader`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogReader)
- [`System.Diagnostics.Eventing.Reader.EventLogRecord`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogRecord)
- [`System.Diagnostics.Eventing.Reader.EventLogSession`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogSession)
- [`System.Diagnostics.Eventing.Reader.EventLogType`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogType)
- [`System.Diagnostics.Eventing.Reader.EventRecord`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.Eventing.Reader.EventRecord)

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

- [Microsoft Learn - System.Diagnostics.EventLog API reference](https://learn.microsoft.com/dotnet/api/System.Diagnostics.EventLog)
- [Windows App Development - Event logging](https://learn.microsoft.com/windows/win32/eventlog/event-logging)
- [GitHub - Source code](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Diagnostics.EventLog)

## Related Packages

<!-- The related packages associated with this package -->

- [System.Diagnostics.PerformanceCounter](https://www.nuget.org/packages/System.Diagnostics.PerformanceCounter)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Diagnostics.EventLog is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
