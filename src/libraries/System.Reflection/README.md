# System.Reflection
This is the primary reflection assembly. It is used for late-bound introspection and invocation of types that are loaded into the currently executing "runtime context". This introspection is exposed by various reflection types including [`Assembly`](https://learn.microsoft.com/dotnet/api/system.reflection.assembly), [`Type`](https://learn.microsoft.com/dotnet/api/system.type), [`ConstructorInfo`](https://learn.microsoft.com/dotnet/api/system.reflection.constructorinfo), [`MethodInfo`](https://learn.microsoft.com/dotnet/api/system.reflection.methodinfo) and [`FieldInfo`](https://learn.microsoft.com/dotnet/api/system.reflection.fieldinfo). The primary mechanism to invoking members in a late-bound manner is through [`MethodBase.Invoke`](https://learn.microsoft.com/dotnet/api/system.reflection.methodbase.invoke).

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.reflection.

## Status: [Active](../../libraries/README.md#development-statuses)
Although these common types are mature, the code base continues to evolve for better performance and to keep up with language and runtime enhancements such as byref-like types.

## Source

* CoreClr-specific: [../../coreclr/System.Private.CoreLib/src/System/Reflection](../../coreclr/System.Private.CoreLib/src/System/Reflection)
* Mono-specific: [../../mono/System.Private.CoreLib/src/System/Reflection](../../mono/System.Private.CoreLib/src/System/Reflection)
* Shared between CoreClr and Mono: [../System.Private.CoreLib/src/System/Reflection](../System.Private.CoreLib/src/System/Reflection)

## Deployment
[System.Relection](https://www.nuget.org/packages/System.Reflection) is included in the shared framework.
