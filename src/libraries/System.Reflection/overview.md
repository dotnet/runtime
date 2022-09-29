# Summary

System.Reflection is used as an umbrella term that is used for late-bound introspection, invocation and code generation. It contains:
- **Runtime introspection and invocation** through [System.Reflection](readme.md). This is what most consider to be "reflection" and supports inspecting assemblies and types that are loaded into the currently executing "runtime context". This introspection is exposed by various reflection types including [`Assembly`](https://learn.microsoft.com/dotnet/api/system.reflection.assembly), [`Type`](https://learn.microsoft.com/dotnet/api/system.type), [`ConstructorInfo`](https://learn.microsoft.com/dotnet/api/system.reflection.constructorinfo), [`MethodInfo`](https://learn.microsoft.com/dotnet/api/system.reflection.methodinfo) and [`FieldInfo`](https://learn.microsoft.com/dotnet/api/system.reflection.fieldinfo). Also supported is the ability to invoke members in a late-bound manner such as through [`MethodBase.Invoke`](https://learn.microsoft.com/dotnet/api/system.reflection.methodbase.invoke).
- **Static introspection** through [System.Reflection.MetadataLoadContext](..\System.Reflection.MetadataLoadContext\readme.md). This exposes the same reflection types as runtime introspection, but they are loaded into a "metadata context" where the types returned for introspection do not support invocation, and they are different instances from the types returned from runtime introspection. Because of this, this allows any assembly to be read, even if it's for a different platform provided that all assembly references are accounted for.  
- **Runtime generation of IL for invocation** through [System.Reflection.Emit](..\System.Reflection.Emit\readme.md). This is used to generate types dynamically which is typically used to generate proxies and other dynamic classes that may not be known ahead-of-time (in which case C# generation could be used, for example). It is also used to invoke members more efficiently than the standard runtime invoke mechanism, although due to recent work in 7.0 (which is to be extended in 8.0) which does this IL generation automatically, that need is becoming less useful.
- **Low-level reading and writing of assemblies**  through [System.Reflection.Metadata](..\System.Reflection.Metadata\readme.md). This is used for tooling and compiler-like scenarios.
- Other reflection libraries that extend the functionality above or serve as a container assembly to support NetStandard or types emitted by compilers:
  - [System.Reflection.Context](..\System.Reflection.Context\readme.md)
  - [System.Reflection.DispatchProxy](..\System.Reflection.DispatchProxy\readme.md)
  - [System.Reflection.Extensions](..\System.Reflection.Extensions\readme.md)
  - [System.Reflection.Primitives](..\System.Reflection.Primitives\readme.md)
  - [System.Reflection.TypeExtensions](..\System.Reflection.TypeExtensions\readme.md)

## <a name="status"></a>Status descriptions
The referenced libraries above each contain a current status of:
- **Legacy**  
Not under development; maintained for compatibility.  
PRs are unlikely to be accepted.  
Issues are likely to be closed without fix.
- **Inactive**  
Under minimal development; quality is maintained.  
PRs for both features and fixes will be considered.  
Issues will be considered for fix or addition to the backlog.
- **Active**  
Under active development by the team.
PRs for both features and fixes will be considered and championed.  
Issues will be considered for fix or addition to the backlog.

## Futures
The [8.0 roadmap](https://github.com/dotnet/runtime/issues/75358) covers planned work for this release.

In general, active reflection investments include:
- Keeping up with new language and runtime features, including byref-like types and AOT environments.
- Larger, missing features requested by the community, including an `AssemblyBuilder.Save()` mechanism to persist generated IL to an assembly.
- Performance improvements, such as automatically generating IL for invocation cases.
- Bug fixes and smaller features.
