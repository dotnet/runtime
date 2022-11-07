# System.Runtime.CompilerServices
Provides functionality for compiler writers who use managed code to specify attributes in metadata that affect the run-time behavior of the common language runtime.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

## Source

* CoreClr-specific: [../../coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices](../../coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices)
* Mono-specific: [../../mono/System.Private.CoreLib/src/System/Runtime/CompilerServices](../../mono/System.Private.CoreLib/src/System/Runtime/CompilerServices)
* Shared between CoreClr and Mono: [../System.Private.CoreLib/src/System/Runtime/CompilerServices](../System.Private.CoreLib/src/System/Runtime/CompilerServices)

## Deployment
[System.Runtime.CompilerServices.VisualC](https://www.nuget.org/packages/System.Runtime.CompilerServices.VisualC) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.