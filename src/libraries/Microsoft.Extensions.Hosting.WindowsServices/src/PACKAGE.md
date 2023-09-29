## About
Supports using Windows Services with the hosting infrastructure.

## Key Features
* Can configure a host to be a Windows Service.

## How to Use
From a Worker Service app created using the Visual Studio template:
```cs
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    // Configure as a Windows Service
    .UseWindowsService(options =>
    {
        options.ServiceName = "My Service";
    })
    .Build();

host.Run();
```

## Main Types
The main types provided by this library are:
* `Microsoft.Extensions.Hosting.WindowsServiceLifetimeHostBuilderExtensions`
* `Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceLifetime`

## Additional Documentation
* [WindowsServiceLifetime](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.windowsservices.windowsservicelifetime)
* [WindowsServiceLifetimeHostBuilderExtensions](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.windowsservicelifetimehostbuilderextensions)
* [Create Windows Service using BackgroundService](https://learn.microsoft.com/dotnet/core/extensions/windows-service)
* [Host ASP.NET Core in a Windows Service](https://learn.microsoft.com/aspnet/core/host-and-deploy/windows-service?tabs=visual-studio)

## Related Packages
- `Microsoft.Extensions.Hosting`
- `System.ServiceProcess.ServiceController`

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Hosting.WindowsServices is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
