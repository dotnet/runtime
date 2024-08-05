# .NETCoreApp shared framework facade assemblies ("shims")

The projects under this directory are pure facade assemblies (also called shims) and don't contain any source code aside from `System.Runtime.CompilerServices.TypeForwardedTo` assembly attributes.

Currently, the following types of facades are checked-in:
- .NET Framework facade assemblies (i.e. mscorlib.dll) which enable loading .NET Framework compiled assemblies into a .NETCoreApp environment.
- .NET Standard facade assembly (netstandard.dll) which enables loading .NET Standard compiled assemblies into a .NETCoreApp environment.
- .NET facade assemblies (i.e. System.AppContext.dll) which enable loading assemblies compiled against a previous version of the .NETCoreApp shared framework.

Some facade folders contain reference source projects ("ref") in addition to source projects ("src"). Those might exist because of the source project referencing internal types or type forwarding to a different destination assembly. If only a source project exists, the compiled assembly will be included in both the targeting and the runtime pack.