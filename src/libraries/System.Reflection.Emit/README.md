# System.Reflection.Emit
Supports generating code dynamically and then running it in-memory. This is used to generate types dynamically which are commonly used to generate proxies and other dynamic classes that may not be known ahead-of-time (in which case C# generation could be used, for example). It is also used to invoke members more efficiently than the [`standard runtime invoke mechanism`](https://learn.microsoft.com/dotnet/api/system.reflection.methodbase.invoke). However, starting in 7.0, most IL generation is now done automatically so the need to manually generate IL for performance is reduced.

Not all platforms and runtimes support IL.Emit.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.reflection.emit. The primary class is [`AssemblyBuilder`](https://learn.microsoft.com/dotnet/api/system.reflection.emit.AssemblyBuilder).

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](/src/libraries/README.md#primary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Reflection.Emit+label%3A%22help+wanted%22) issues.

The primary new feature under consideration is [AssemblyBuilder.Save()](https://github.com/dotnet/runtime/issues/62956).

## Deployment
[System.Reflection.Emit](https://www.nuget.org/packages/System.Reflection.Emit) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.1; it only needs to be installed when targeting .NET Standard 2.0.

## Source

* CoreClr-specific: [/src/coreclr/System.Private.CoreLib/src/System/Reflection/Emit](/src/coreclr/System.Private.CoreLib/src/System/Reflection/Emit)
* Mono-specific: [/src/mono/System.Private.CoreLib/src/System/Reflection/Emit](/src/mono/System.Private.CoreLib/src/System/Reflection/Emit)
* Shared between CoreClr and Mono: [../System.Private.CoreLib/src/System/Reflection/Emit](/src/libraries/System.Private.CoreLib/src/System/Reflection/Emit)

## See also
- [`System.Reflection.Emit.Lightweight`](../System.Reflection.Emit.Lightweight/README.md)
- [`System.Reflection.Emit.ILGeneration`](../System.Reflection.Emit.ILGeneration/README.md)
