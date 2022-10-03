# System.Reflection.Emit
Supports generating code dynamically and then running it in-memory. The primary class is [`AssemblyBuilder`](https://learn.microsoft.com/dotnet/api/system.reflection.emit.assemblybuilder).

Not all platforms and runtimes support IL.Emit.

This library is often used with [`System.Reflection.Emit.ILGeneration`](../system.reflection.emit.ilgeneration/readme.md).

## Status: [Inactive](../system.reflection/overview.md#status)
The APIs and functionality are mature, but do get extended occasionally.

## Deployment
Inbox  
https://www.nuget.org/packages/System.Reflection.Emit

### Source
Runtime code for CoreClr:
https://github.com/dotnet/runtime/tree/main/src/coreclr/System.Private.CoreLib/src/System/Reflection/Emit  
for Mono:  
https://github.com/dotnet/runtime/tree/main/src/mono/System.Private.CoreLib/src/System/Reflection/Emit  
Shared code between CoreClr and Mono:
https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Reflection/Emit

### See also

[`System.Reflection.Emit.Lightweight`](..\system.reflection.emit.lightweight.readme.md)
