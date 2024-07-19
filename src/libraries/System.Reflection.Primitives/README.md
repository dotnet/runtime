# System.Reflection.Primitives
Contains reflection types such as [TypeAttributes](https://learn.microsoft.com/dotnet/api/system.reflection.typeattributes). Some of these types are forwarded to the runtime. In the future, the non-forwarded types may be lifted to the runtime and then type forwards added.

## Contribution Bar
- [x] [We only consider fixes that unblock critical issues](/src/libraries/README.md#primary-bar)

This package was used in the past to expose common functionality across different runtimes in a uniform way. It continues to ship as part of the shared framework, but it is frozen for compatibility.

## Deployment
[System.Reflection.Primitives](https://www.nuget.org/packages/System.Reflection.Primitives) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.
