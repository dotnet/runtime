## About

<!-- A description of the package and where one can find more documentation -->
Provides the System.ServiceProcess.ServiceController API, which allows to connect to a Windows service, manipulate it, or get information about it. Not supported on other platforms.

## Key Features

<!-- The key features of this package -->

* Retrieve information from Windows services
* Connect to and manipulate Windows services (start, pause, stop or other operations)

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### Retrieve Windows service information
```C#
using System.ServiceProcess;

// Loop through all installed Windows services and print the name, status and display name.
foreach (ServiceController serviceController in ServiceController.GetServices())
{
    Console.WriteLine("Name: " + serviceController.ServiceName);
    Console.WriteLine("Status: " + serviceController.Status.ToString());
    Console.WriteLine("Display name: " + serviceController.DisplayName);
}

// Loop through all installed device driver services
foreach (ServiceController serviceController in ServiceController.GetDevices())
{
    Console.WriteLine("Name: " + serviceController.ServiceName);
    Console.WriteLine("Status: " + serviceController.Status.ToString());
    Console.WriteLine("Display name: " + serviceController.DisplayName);
}
```

### Manipulate a Windows service
```C#
using System.ServiceProcess;

ServiceController service = new("TestServiceName");
if (service.CanStop && service.Status != ServiceControllerStatus.Stopped && service.Status != ServiceControllerStatus.StopPending)
{
    service.Stop();
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.ServiceProcess.ServiceController`
* `System.ServiceProcess.ServiceControllerStatus`
* `System.ServiceProcess.ServiceType`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [System.ServiceController API documentation](https://learn.microsoft.com/dotnet/api/system.serviceprocess.servicecontroller?view=dotnet-plat-ext-7.0)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.ServiceProcess.ServiceController is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
