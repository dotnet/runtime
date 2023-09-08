## About
Contains abstractions to host user code in an application by encapsulating an application's resources and lifetime functionality including:
- Dependency injection (DI).
- Logging.
- Configuration.
- Starting, stopping and obtaining notifications.

This package is also used to wire up specific application models like ASP.NET Core that are built on top of hosting.

## Key Features
* Provides the `BackgroundService` base class and the `IHostedService` interface for implementing worker services.
* Provides interfaces used to configure and start\stop a host.
* Provides types to obtain environment settings such as an application name and paths.

## How to Use
See the Conceptual documentation below for using `BackgroundService` and `IHostedService` to host worker services.

## Main Types
The main types provided by this library are:

* `Microsoft.Extensions.Hosting.BackgroundService`
* `Microsoft.Extensions.Hosting.IHostBuilder`
* `Microsoft.Extensions.Hosting.IHostedService`

## Additional Documentation
* Conceptual documentation
  - [Worker services in .NET](https://learn.microsoft.com/dotnet/core/extensions/workers)
  - [Implement the IHostedService interface](https://learn.microsoft.com/dotnet/core/extensions/timer-service)
* API documentation
    - [BackgroundService](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.backgroundservice)
    - [IHostBuilder](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.ihostbuilder)
    - [IHostedService](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.ihostedservice)

## Related Packages
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Diagnostics.Abstractions`
- `Microsoft.Extensions.FileProviders.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`

## Feedback & Contributing
Microsoft.Extensions.Hosting.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
