## About

<!-- A description of the package and where one can find more documentation -->

When an error occurs in a Windows machine, the system administrator or support representative must determine what caused the error, attempt to recover any lost data, and prevent the error from recurring. It is helpful if applications, the operating system, and other system services record important events, such as low-memory conditions or excessive attempts to access a disk. The system administrator can then use the Windows Event Log to help determine what conditions caused the error and identify the context in which it occurred.

This package provides the `System.Diagnostics.EventLog.dll` assembly, which contains types that allow applications to interact with the Windows Event Log service.

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

Under the [`System.Diagnostics`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics) namespace:

- [`System.Diagnostics.EntryWrittenEventArgs`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EntryWrittenEventArgs)
- [`System.Diagnostics.EntryWrittenEventHandler`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EntryWrittenEventHandler)
- [`System.Diagnostics.EventInstance`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventInstance)
- [`System.Diagnostics.EventLog`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventLog)
- [`System.Diagnostics.EventLogEntry`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventLogEntry)
- [`System.Diagnostics.EventLogEntryCollection`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventLogEntryCollection)
- [`System.Diagnostics.EventLogEntryType`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventLogEntryType)
- [`System.Diagnostics.EventLogTraceListener`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventLogTraceListener)
- [`System.Diagnostics.EventSourceCreationData`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventSourceCreationData)
- [`System.Diagnostics.OverflowAction`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.OverflowAction)

Under the[`System.Diagnostics.Eventing.Reader`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader) namespace:

- [`System.Diagnostics.Eventing.Reader.EventBookmark`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventBookmark)
- [`System.Diagnostics.Eventing.Reader.EventKeyword`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventKeyword)
- [`System.Diagnostics.Eventing.Reader.EventLevel`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLevel)
- [`System.Diagnostics.Eventing.Reader.EventLogConfiguration`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogConfiguration)
- [`System.Diagnostics.Eventing.Reader.EventLogException`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogException)
- [`System.Diagnostics.Eventing.Reader.EventLogInformation`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogInformation)
- [`System.Diagnostics.Eventing.Reader.EventLogInvalidDataException`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogInvalidDataException)
- [`System.Diagnostics.Eventing.Reader.EventLogIsolation`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogIsolation)
- [`System.Diagnostics.Eventing.Reader.EventLogLink`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogLink)
- [`System.Diagnostics.Eventing.Reader.EventLogMode`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogMode)
- [`System.Diagnostics.Eventing.Reader.EventLogNotFoundException`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogNotFoundException)
- [`System.Diagnostics.Eventing.Reader.EventLogPropertySelector`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogPropertySelector)
- [`System.Diagnostics.Eventing.Reader.EventLogProviderDisabledException`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogProviderDisabledException)
- [`System.Diagnostics.Eventing.Reader.EventLogQuery`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogQuery)
- [`System.Diagnostics.Eventing.Reader.EventLogReader`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogReader)
- [`System.Diagnostics.Eventing.Reader.EventLogReadingException`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogReadingException)
- [`System.Diagnostics.Eventing.Reader.EventLogRecord`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogRecord)
- [`System.Diagnostics.Eventing.Reader.EventLogSession`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogSession)
- [`System.Diagnostics.Eventing.Reader.EventLogStatus`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogStatus)
- [`System.Diagnostics.Eventing.Reader.EventLogType`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogType)
- [`System.Diagnostics.Eventing.Reader.EventLogWatcher`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventLogWatcher)
- [`System.Diagnostics.Eventing.Reader.EventMetadata`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventMetadata)
- [`System.Diagnostics.Eventing.Reader.EventOpcode`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventOpcode)
- [`System.Diagnostics.Eventing.Reader.EventProperty`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventProperty)
- [`System.Diagnostics.Eventing.Reader.EventRecord`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventRecord)
- [`System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs)
- [`System.Diagnostics.Eventing.Reader.EventTask`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.EventTask)
- [`System.Diagnostics.Eventing.Reader.PathType`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.PathType)
- [`System.Diagnostics.Eventing.Reader.ProviderMetadata`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.ProviderMetadata)
- [`System.Diagnostics.Eventing.Reader.SessionAuthentication`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.SessionAuthentication)
- [`System.Diagnostics.Eventing.Reader.StandardEventKeywords`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.StandardEventKeywords)
- [`System.Diagnostics.Eventing.Reader.StandardEventLevel`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.StandardEventLevel)
- [`System.Diagnostics.Eventing.Reader.StandardEventOpcode`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.StandardEventOpcode)
- [`System.Diagnostics.Eventing.Reader.StandardEventTask`](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.Eventing.Reader.StandardEventTask)

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

- [Microsoft Learn - System.Diagnostics.EventLog API reference](https://learn.microsoft.com/en-us/dotnet/api/System.Diagnostics.EventLog)
- [Windows App Development - Event logging](https://learn.microsoft.com/en-us/windows/win32/eventlog/event-logging)
- [GitHub - Source code](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Diagnostics.EventLog)

## Related Packages

<!-- The related packages associated with this package -->

- [System.Diagnostics.PerformanceCounter](https://www.nuget.org/packages/System.Diagnostics.PerformanceCounter)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

**System.Diagnostics.EventLog** is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
