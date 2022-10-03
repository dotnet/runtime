# System.Reflection.MetadataLoadContext
This provides a high-level API for inspecting raw assembly contents according to ECMA-335. Types from an assembly are loaded into the provided [`MetadataLoadContext`](https://learn.microsoft.com/dotnet/api/system.reflection.metadataloadcontext) instance which provide for introspection purposes by returning `System.Type`, `System.MemberInfo`, etc. However, these reflection types do not support invocation.

This library takes a dependency on `System.Reflection.Metadata` for reading the assembly.

## Status: [Inactive](../system.reflection/overview.md#status)
The APIs and functionality are mature, but do get extended occasionally.

## Source
https://github.com/dotnet/runtime/tree/main/src/libraries/System.Reflection.Metadata

## Deployment
Inbox  
https://www.nuget.org/packages/System.Reflection.MetadataLoadContext (contains full code for OOB scenarios).
