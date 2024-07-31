# System.Reflection.DispatchProxy
Supports the [`DispatchProxy`](https://learn.microsoft.com/dotnet/api/system.reflection.dispatchproxy) class which is used to forward methods to another class.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.reflection.dispatchproxy.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](/src/libraries/README.md#primary-bar)

Although used for key scenarios by the community, it has remained relatively unchanged. Internally it uses [Emit](../System.Reflection.Emit/README.md) to generate the proxies so it doesn't work on all platforms.

## Deployment
[System.Reflection.DispatchProxy](https://www.nuget.org/packages/System.Reflection.DispatchProxy) NuGet package
