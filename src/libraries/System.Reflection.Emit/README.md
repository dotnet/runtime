# System.Reflection.Emit
Supports generating code dynamically and then running it in-memory. This is used to generate types dynamically which are commonly used to generate proxies and other dynamic classes that may not be known ahead-of-time (in which case C# generation could be used, for example). It is also used to invoke members more efficiently than the [`standard runtime invoke mechanism`](https://learn.microsoft.com/dotnet/api/system.reflection.methodbase.invoke). However, starting in 7.0, most IL generation is now done automatically so the need to manually generate IL for performance is reduced.

Not all platforms and runtimes support IL.Emit.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.reflection.emit. The primary class is [`AssemblyBuilder`](https://learn.microsoft.com/dotnet/api/system.reflection.emit.AssemblyBuilder).

## Status: [Inactive](../../libraries/README.md#development-statuses)
The APIs and functionality are mature, but do get extended occasionally.

## Deployment
Inbox<br/>
[System.Reflection.Emit NuGet Package](https://www.nuget.org/packages/System.Reflection.Emit)

## Source
Runtime code for CoreClr:
https://github.com/dotnet/runtime/tree/main/src/coreclr/System.Private.CoreLib/src/System/Reflection/Emit<br/>
for Mono:<br/>
https://github.com/dotnet/runtime/tree/main/src/mono/System.Private.CoreLib/src/System/Reflection/Emit<br/>
Shared code between CoreClr and Mono:
https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Reflection/Emit

## See also
- [`System.Reflection.Emit.Lightweight`](../System.Reflection.Emit.Lightweight/README.md)
- [`System.Reflection.Emit.ILGeneration`](../System.Reflection.Emit.ILGeneration/README.md)
