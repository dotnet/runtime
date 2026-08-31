# Microsoft.Extensions.Hosting

`Microsoft.Extensions.Hosting` is combined with a core hosting abstraction under `Microsoft.Extensions.Hosting.Abstractions` that provides the pattern for using the extensions libraries to host user code in an application. Hosting helps configure Logging, Configuration, DI, and to wire up specific application models like ASP.NET Core that are built on top of hosting.

Hosting provides as a primitive the concept of a hosted service, which is how application models like ASP.NET Core integrate with the host. Users often write hosted services as to handle their own application concerns.

Hosting provides good integration for long-running console applications, windows services, ASP.NET Core.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/generic-host.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)

The APIs and functionality are mature and there is no active plan for investment but we are open to explore ideas to invest in it in more depth in the future. The ideal future investments here may be to:

- Support all .NET Core application models like: WinForms, WPF, UWP, Xamarin, Short-running (batch) console jobs, Blazor (client)
- Support for idle/pause in hosted services.
- Support more base-classes for hosted services like timer-based and trigger-based

## Deployment
[Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) is not included in the shared framework. The package is deployed as out-of-band (OOB) and needs to be installed into projects directly.

