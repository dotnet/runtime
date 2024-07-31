# System.Reflection.Extensions
This supports forwarding types to the runtime including [CustomAttributeExtensions](https://learn.microsoft.com/dotnet/api/system.reflection.CustomAttributeExtensions), [InterfaceMapping](https://learn.microsoft.com/dotnet/api/system.reflection.InterfaceMapping) and [RuntimeReflectionExtensions](https://learn.microsoft.com/dotnet/api/system.reflection.RuntimeReflectionExtensions).

## Contribution Bar
- [x] [We only consider fixes that unblock critical issues](/src/libraries/README.md#primary-bar)

This package was used in the past to expose common functionality across different runtimes in a uniform way. It continues to ship as part of the shared framework, but it is frozen for compatibility.

## Deployment
[System.Reflection.Extensions](https://www.nuget.org/packages/System.Reflection.Extensions) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.
