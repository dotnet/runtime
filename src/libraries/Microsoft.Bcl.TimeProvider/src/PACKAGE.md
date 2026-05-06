## About

Microsoft.Bcl.TimeProvider provides time abstraction support for apps targeting .NET 7 and earlier, as well as those intended for the .NET Framework. For apps targeting .NET 8 and newer versions, referencing this package is unnecessary, as the types it contains are already included in the .NET 8 and higher platform versions.

## Key Features

* Provides a common abstraction for time-related operations.

## How to Use

```csharp
using System;

// A class that uses TimeProvider to get the current time in Utc coordinates
public class UtcClock
{
    private readonly TimeProvider _timeProvider;

    // Constructor that takes a TimeProvider as a dependency
    public Clock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    // A method that returns the current time as a string
    public string GetTime()
    {
        return _timeProvider.GetLocalNow().ToString("HH:mm:ss");
    }
}

// A class that inherits from TimeProvider and overrides the GetLocalNow method
public class UtcTimeProvider : TimeProvider
{
    // Override the GetLocalNow method to always return UTC time
    public override DateTimeOffset GetLocalNow()
    {
        return TimeProvider.System.GetUtcNow();
    }
}

```

## Main Types

The main types provided by this library are:

* `TimeProvider`
* `TimeProviderTaskExtensions`

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.timeprovider)

## Feedback & Contributing

Microsoft.Bcl.TimeProvider is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).