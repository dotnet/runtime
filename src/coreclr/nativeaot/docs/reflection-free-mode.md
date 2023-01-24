# Reflection-free mode

Reflection-free mode is a mode of the NativeAOT compiler and runtime that greatly reduces the functionality of the reflection APIs and brings a couple interesting benefits as a result. The benefits of this mode are:

* Greatly reduced size of self contained deployments - a fully self-contained "Hello world" style app compiles to a 1 MB file (on x64) with _no dependencies_.
* Reduced working set and better code locality - parts of the program are more tightly packed together.
* Less metadata for people to reverse engineer - apps compiled in reflection-free mode are as hard to reverse engineer as apps written in e.g. C++.

Of course the benefits come with a drawback: not all .NET code can work in such environment. In fact, most of the existing code probably won't. Use this mode with caution. https://github.com/dotnet/runtime/issues/67193 tracks potential improvements of this mode.

To enable reflection-free mode in a project that is already using NativeAOT, add the following property to a `PropertyGroup` in your project file:

```xml
<PropertyGroup>
  <IlcDisableReflection>true</IlcDisableReflection>
</PropertyGroup>
```

(More switches are documented in the [Optimizing NativeAOT](optimizing.md) document.)

## What's different at compile time with reflection disabled

When reflection is disabled, the AOT compiler stops emitting data structures that are required to make reflection work at runtime and stops enforcing policies that makes the code more reflection friendly.

Think of:
* Names of methods, types, parameters are no longer generated into the executable.
* Additional metadata, like list of method parameter types, is no longer generated.
* The compiler no longer generates standalone method bodies that would be suitable for reflection invoke.
* Mapping tables that map method names to native code are no longer generated.
* The implementation of reflection is no longer compiled.

## Reflection APIs that work in reflection-free mode

Reflection-free mode **supports a limited set of reflection APIs** that keep their expected semantics.

* `typeof(SomeType)` will return a `System.Type` that can be compared with results of other `typeof` expressions or results of `Object.GetType()` calls. The patterns commonly used in perf optimizations of generic code (e.g. `typeof(T) == typeof(byte)`) will work fine, and so will `obj.GetType() == typeof(SomeType)`.
* Following APIs on `System.Type` work: `TypeHandle`, `UnderlyingSystemType`, `BaseType`, `IsByRefLike`, `IsValueType`, `GetTypeCode`, `GetHashCode`, `GetElementType`, `GetInterfaces`, `HasElementType`, `IsArray`, `IsByRef`, `IsPointer`, `IsPrimitive`, `IsAssignableFrom`, `IsAssignableTo`, `IsInstanceOfType`.
* `Activator.CreateInstance<T>()` will work. The compiler statically analyzes and expands this to efficient code at compile time. No reflection is involved at runtime.
* `Assembly.GetExecutingAssembly()` will return a `System.Reflection.Assembly` that can be compared with other runtime `Assembly` instances. This is mostly to make it possible to use the `NativeLibrary.SetDllImportResolver` API.

## Reflection APIs that are up for discussion

We might be able to add support for the following APIs without sacrificing too much of the goals of the reflection-free mode:

* `Enum.Parse` and `Enum.ToString`. These methods currently only work with integers (`ToString` returns an integer, and `Parse` can parse integers). Note that `Enum.GetValues` would still not work.
* Type names (`Type.Name`, `Type.Namespace`, `Type.FullName`): We could probably add this back if there's good use cases in an otherwise reflection-free mode.
* `Delegate.DynamicInvoke` can be made to work if there's a good use case too.

## APIs that don't work and will not work

* APIs that require dynamic code generation: `Reflection.Emit`, `Assembly.Load` and friends
* Obvious program introspection APIs: APIs on `Type` and `Assembly` not mentioned above, `MethodBase`, `MethodInfo`, `ConstructorInfo`, `FieldInfo`, `PropertyInfo`, `EventInfo`. These APIs will throw at runtime.
* APIs building on top of reflection APIs. Too many to enumerate.

## Shimming

Sometimes reflection is used in non-critical paths to retrieve type names. In reflection-free mode, accessing type names throws an exception. To help moving such code to reflection-free mode, we have an AppContext switch to generate fake names and namespaces for types. With this mode enabled, accessing the `Name` and `Namespace` property will not throw, but won't return the actual name either.

To enable this mode, add following item to an `ItemGroup` in your project file:

```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="Switch.System.Reflection.Disabled.DoNotThrowForNames" Value="true" />
  </ItemGroup>
```

To achieve similar result for when querying for ``Assembly`` (will instead give the ExecutingAssembly):

```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="Switch.System.Reflection.Disabled.DoNotThrowForAssembly" Value="true" />
  </ItemGroup>
```

And here for CustomAttributes (will return an empty array):


```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="Switch.System.Reflection.Disabled.DoNotThrowForAttributes" Value="true" />
  </ItemGroup>
```

Note:

To make ``NativeLibrary`` API, and on the same occasion``Socket``, to work, you'll need:

```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="Switch.System.Reflection.Disabled.DoNotThrowForAssembly" Value="true" />
    <RuntimeHostConfigurationOption Include="Switch.System.Reflection.Disabled.DoNotThrowForAttributes" Value="true" />
  </ItemGroup>
```

