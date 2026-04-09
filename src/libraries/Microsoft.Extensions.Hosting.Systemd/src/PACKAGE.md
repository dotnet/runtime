## About

<!-- A description of the package and where one can find more documentation -->

Provides an implementation for hosting an application as a Linux *systemd* service.

The package ensures proper communication between .NET applications and *systemd*, making it easier to build, deploy, and run applications as Linux services.

## Key Features

<!-- The key features of this package -->

* Systemd service integration
* Health check support
* Logging and diagnostics with different log levels
* Notify systemd on application state changes

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Wherever you configure your host, add the `UseSystemd` method to the builder chain:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSystemd() // Enable running as a Systemd service
        .ConfigureServices((hostContext, services) =>
        {
            ...
        });
```

The `UseSystemd` method will no-op when not running as a daemon, allowing normal debugging or production use both with and without *systemd*.

Registering the service requires a special file, called a unit file, to be added to the `/etc/systemd/system` directory.

For more information on this part, check the [run your app as a Linux service with systemd](https://learn.microsoft.com/dotnet/architecture/grpc-for-wcf-developers/self-hosted#run-your-app-as-a-linux-service-with-systemd) guide.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Hosting.Systemd.SystemdNotifier`
* `Microsoft.Extensions.Hosting.Systemd.SystemdLifetime`
* `Microsoft.Extensions.Hosting.SystemdHostBuilderExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/architecture/grpc-for-wcf-developers/self-hosted#run-your-app-as-a-linux-service-with-systemd)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.systemd)

## Related Packages

<!-- The related packages associated with this package -->

* [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Hosting.Systemd is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
