## About
Contains the .NET Generic Host `HostBuilder` which layers on the `Microsoft.Extensions.Hosting.Abstractions` package.

A host encapsulates an app's resources and lifetime functionality, such as:
- Dependency injection (DI).
- Logging.
- Configuration.
- The `IHostedService` implementation which supports starting and stopping.

## Key Features
* Contains the .NET Generic Host `HostBuilder`.

## How to Use
For a console app project:
```cs
    using (IHost host = new HostBuilder().Build())
    {
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    
        lifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("Started");
        });
        lifetime.ApplicationStopping.Register(() =>
        {
            Console.WriteLine("Stopping firing");
            Console.WriteLine("Stopping end");
        });
        lifetime.ApplicationStopped.Register(() =>
        {
            Console.WriteLine("Stopped firing");
            Console.WriteLine("Stopped end");
        });
    
        host.Start();
    
        // Listens for Ctrl-C.
        host.WaitForShutdown();
    }
```

## Main Types
The main types provided by this library are:
* `Microsoft.Extensions.Host`.
* `Microsoft.Extensions.Hosting.HostApplicationBuilder`
* `Microsoft.Extensions.Hosting.HostBuilder`
* `Microsoft.Extensions.Hosting.IHostedService`
* `Microsoft.Extensions.Hosting.IHostedLifecycleService`

## Additional Documentation
* [Generic host](https://learn.microsoft.com/dotnet/core/extensions/generic-host)
* API documentation
  - [Host](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.host)
  - [HostApplicationBuilder](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.hostapplicationbuilder)
  - [HostBuilder](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.hostbuilder)

## Related Packages
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Options`

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Hosting is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
