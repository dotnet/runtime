## About

<!-- A description of the package and where one can find more documentation -->

Provides access to a rich set of management information and management events about the system, devices, and applications instrumented to the Windows Management Instrumentation (WMI) infrastructure. Not supported on other platforms.

## Key Features

<!-- The key features of this package -->

* Consume Windows Management Instrumentation (WMI) data and events
* High performance extensible event mechanism

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### Retrieve management information
```C#
using System.Management;

// Get the WMI class
ManagementClass managementClass = new("Win32_Processor");

// Loop through the WMI class instances and print the processor information found
foreach (ManagementObject managementObject in managementClass.GetInstances())
{
    Console.WriteLine("--- Processor information ---");
    Console.WriteLine($"Name: {managementObject["Name"]}");
    Console.WriteLine($"Architecture: {managementObject["Architecture"]}");
}
```

### Query management information via the SelectQuery type
```C#
using System.Management;

// Search for win32 services with a stopped state
SelectQuery selectQuery = new("Win32_Service", "State = 'Stopped'");
ManagementObjectSearcher managementObjectSearcher = new(selectQuery);

foreach (ManagementObject service in managementObjectSearcher.Get())
{
    Console.WriteLine(service.ToString());
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Management.ManagementClass`
* `System.Management.ManagementObject`
* `System.Management.SelectQuery`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/windows/win32/wmisdk/wmi-start-page)
* [System.Management API documentation](https://learn.microsoft.com/dotnet/api/system.management?view=dotnet-plat-ext-7.0)
* [System.Management.ManagementClass documentation](https://learn.microsoft.com/dotnet/api/system.management.managementclass.-ctor?view=dotnet-plat-ext-7.0)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Management is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
