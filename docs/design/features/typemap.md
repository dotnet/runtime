# Interop Type Map

## Background

When interop between languages/platforms involves the projection of types, some kind of type mapping logic must often exist. This mapping mechanism is used to determine what .NET type should be used to project a type from language X and vice versa.

The most common mechanism for this is the generation of a large look-up table at build time, which is then injected into the application or Assembly. If injected into the Assembly, there is typically some registration mechanism for the mapping data. Additional modifications and optimizations can be applied based on the user experience or scenarios constraints (that is, build time, execution environment limitations, etc).

Prior to .NET 10 there were at least three (3) bespoke mechanisms for this in the .NET ecosystem:

* C#/WinRT - [Built-in mappings](https://github.com/microsoft/CsWinRT/b1733e95c6d35b551fc8cf6fe04e2a0c287346dd/master/src/WinRT.Runtime/Projections.CustomTypeMappings.tt), [Generation of vtables for AOT](https://github.com/microsoft/CsWinRT/blob/b1733e95c6d35b551fc8cf6fe04e2a0c287346dd/src/Authoring/WinRT.SourceGenerator/AotOptimizer.cs#L1597).

* .NET For Android - [Assembly Store doc](https://github.com/dotnet/android/blob/b8d0669e951d683443c19ecac06dc96363791820/Documentation/project-docs/AssemblyStores.md), [Assembly Store generator](https://github.com/dotnet/android/blob/b8d0669e951d683443c19ecac06dc96363791820/src/Xamarin.Android.Build.Tasks/Utilities/TypeMappingReleaseNativeAssemblyGenerator.cs), [unmanaged Assembly Store types](https://github.com/dotnet/android/blob/b8d0669e951d683443c19ecac06dc96363791820/src/native/xamarin-app-stub/xamarin-app.hh).

* Objective-C - [Registrar](https://github.com/dotnet/macios/blob/cee75657955e29981ded2fb0c6f0ee832db9a8d3/src/ObjCRuntime/Registrar.cs#L87), [Managed Static Registrar](https://github.com/dotnet/macios/blob/cee75657955e29981ded2fb0c6f0ee832db9a8d3/docs/managed-static-registrar.md).

## Priorties

1) Trimmer friendly - AOT compatible.
2) Usable from both managed and unmanaged environments.
3) Low impact to application start-up and/or Assembly load.
4) Be composable - handle multiple type mappings.

## APIs

The below .NET APIs represents only part of the feature. The complete scenario would involve additional steps and tooling.

**Provided by BCL (that is, NetCoreApp)**
```csharp
namespace System.Runtime.InteropServices;

/// <summary>
/// Type mapping between a string and a type.
/// </summary>
/// <typeparam name="TTypeMapGroup">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeMapAttribute<TTypeMapGroup> : Attribute
{
    /// <summary>
    /// Create a mapping between a value and a <see cref="System.Type"/>.
    /// </summary>
    /// <param name="value">String representation of key</param>
    /// <param name="target">Type value</param>
    /// <remarks>
    /// This mapping is unconditionally inserted into the type map.
    /// </remarks>
    public TypeMapAttribute(string value, Type target)
    { }

    /// <summary>
    /// Create a mapping between a value and a <see cref="System.Type"/>.
    /// </summary>
    /// <param name="value">String representation of key</param>
    /// <param name="target">Type value</param>
    /// <param name="trimTarget">Type used by Trimmer to determine type map inclusion.</param>
    /// <remarks>
    /// This mapping is only included in the type map if the Trimmer observes a type check
    /// using the <see cref="System.Type"/> represented by <paramref name="trimTarget"/>.
    /// </remarks>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public TypeMapAttribute(string value, Type target, Type trimTarget)
    { }
}

/// <summary>
/// Declare an assembly that should be inspected during type map building.
/// </summary>
/// <typeparam name="TTypeMapGroup">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeMapAssemblyTargetAttribute<TTypeMapGroup> : Attribute
{
    /// <summary>
    /// Provide the assembly to look for type mapping attributes.
    /// </summary>
    /// <param name="assemblyName">Assembly to reference</param>
    public TypeMapAssemblyTargetAttribute(string assemblyName)
    { }
}

/// <summary>
/// Create a type association between a type and its proxy.
/// </summary>
/// <typeparam name="TTypeMapGroup">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class TypeMapAssociationAttribute<TTypeMapGroup> : Attribute
{
    /// <summary>
    /// Create an association between two types in the type map.
    /// </summary>
    /// <param name="source">Target type.</param>
    /// <param name="proxy">Type to associated with <paramref name="source"/>.</param>
    /// <remarks>
    /// This mapping will only exist in the type map if the Trimmer observes
    /// an allocation using the <see cref="System.Type"/> represented by <paramref name="source"/>.
    /// </remarks>
    public TypeMapAssociationAttribute(Type source, Type proxy)
    { }
}

/// <summary>
/// Entry type for interop type mapping logic.
/// </summary>
public static class TypeMapping
{
    /// <summary>
    /// Returns the External type type map generated for the current application.
    /// </summary>
    /// <typeparam name="TTypeMapGroup">Type universe</typeparam>
    /// <param name="map">Requested type map</param>
    /// <returns>True if the map is returned, otherwise false.</returns>
    /// <remarks>
    /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
    /// </remarks>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup>();

    /// <summary>
    /// Returns the associated type type map generated for the current application.
    /// </summary>
    /// <typeparam name="TTypeMapGroup">Type universe</typeparam>
    /// <param name="map">Requested type map</param>
    /// <returns>True if the map is returned, otherwise false.</returns>
    /// <remarks>
    /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
    /// </remarks>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static IReadOnlyDictionary<Type, Type> GetOrCreateProxyTypeMapping<TTypeMapGroup>();
}
```

Given the above types the following would take place.

1. Types involved in unmanaged-to-managed interop operations would be referenced in a
`TypeMapAttribute` assembly attribute that declared the external type system name, a target
type, and optionally a "trim-target" to determine if the target
type should be included in the map. If the `TypeMapAttribute` constructor that doesn't
take a trim-target is used, the "target type" will be treated as the "trim-target".

2. Types used in a managed-to-unmanaged interop operation would use `TypeMapAssociationAttribute`
to define a conditional link between the source and proxy type. In other words, if the
source is kept, so is the proxy type. If the Trimmer observes an explicit allocation of the source
type, the entry will be inserted into the map.

3. During application build, source would be generated and injected into the application
that defines appropriate `TypeMapAssemblyTargetAttribute` instances. This attribute would help the
Trimmer know other assemblies to examine for `TypeMapAttribute` and `TypeMapAssociationAttribute`
instances. These linked assemblies could also be used in the non-Trimmed scenario whereby we
avoid creating the map at build-time and create a dynamic map at run-time instead.

4. The Trimmer will build two maps based on the above attributes from the application reference
closure.

    **(a)** Using `TypeMapAttribute` a map from `string` to target `Type`.

    **(b)** Using `TypeMapAssociationAttribute` a map from `Type` to `Type` (source to proxy).

> [!IMPORTANT]
> Conflicting key/value mappings are not allowed.

> [!NOTE]
> The underlying format of the produced maps is implementation-defined. Different .NET form factors may use different formats.
>
> Additionally, it is not guaranteed that the `TypeMapAttribute`, `TypeMapAssociationAttribute`, and `TypeMapAssemblyTargetAttribute` attributes are present in the final image after a trimming tool has been run.


5. Trimming tools will consider calls to `TypeMapping.GetOrCreateExternalTypeMapping<>` and
`TypeMapping.GetOrCreateProxyTypeMapping<>` as intrinsics (for example, Java via `JavaTypeMapGroup`). As a result, it is not trim-compatible to call either of these methods with non-fully-instantiated generic (such as a type argument or a type that is instantiated over a type argument).

## Type Map entry trimming rules

This section provides the minimum rules for entries to be included in a given type map by a trimming tool (ie. ILLink or NativeAOT). Due to restrictions in some form factors, some trimming tools may include more entries than would be included based on the rules described below.

The following rules only apply to code that is considered "reachable" from the entry-point method. Code that a trimming tool determines is unreachable does not contribute to determining if a type map entry is preserved.

### Type Map Assembly Target probing

The process of building type maps starts at the entry-point method of the app (the `Main` method). The initial entries for the type maps are collected from the assembly containing the entry-point for the app. From that assembly, any assembly names that are mentioned in a `TypeMapAssemblyTargetAttribute` are scanned. This process then repeats for those assemblies until all assemblies transitively referenced by `TypeMapAssemblyTargetAttribute`s have been scanned.

An assembly name mentioned in the `TypeMapAssemblyTargetAttribute` does not need to map to an `AssemblyRef` row in the module's metadata. As long as a given name can be resolved by the runtime or by whatever trimming tool is run on the application, it can be used.

### External Type Map

An entry in an External Type Map is included when the "trim target" type is referenced in one of the following ways:

- The argument to the `ldtoken` IL instruction.
- The argument to the `unbox` IL instruction.
- The argument to the `unbox.any` IL instruction.
- The argument to the `isinst` IL instruction.
- The argument to the `castclass` IL instruction.
- The argument to the `box` instruction.
- The argument to the `mkrefany` instruction.
- The argument to the `refanyval` instruction.
- The argument to the `newarr` instruction.
- The argument to the `ldobj` instruction.
- The argument to the `stobj` instruction.
- The argument to the `.constrained` instruction prefix.
- The type of a method argument to the `newobj` instruction.
- The owning type of the method argument to `call`, `callvirt`, `ldftn`, or `ldvirtftn`.
  - If the owning type is an interface and the trimming tool can determine that there is only one implementation of the interface, it is free to interpret the method token argument as though it is the method on the only implementing type.
- The generic argument to the `Activator.CreateInstance<T>` method.
- Calls to `Type.GetType` with a constant string representing the type name.

Many of these instructions can be passed a generic parameter. In that case, the trimming tool should consider type arguments of instantiations of that type as having met one of these rules and include any entries with those types as "trim target" types.

### Proxy Type Map

An entry in the Proxy Type Map is included when the "source type" is referenced in one of the following ways:

- The argument to the `ldtoken` IL instruction when `DynamicallyAccessedMembersAttribute` is specified with one of the flags that preserves constructors for the storage location.
- Calls to `Type.GetType` with a constant string representing the type name when `DynamicallyAccessedMembersAttribute` is specified with one of the flags that preserves constructors for the storage location.
- The type of a method argument to the `newobj` instruction.
- The generic argument to the `Activator.CreateInstance<T>` method.
- The argument to the `box` instruction.
- The argument to the `newarr` instruction.
- The argument to the `.constrained` instruction prefix.
- The argument to the `mkrefany` instruction.
- The argument to the `refanyval` instruction.

If the type is an interface type and the user could possibly see a `RuntimeTypeHandle` for the type as part of a casting or virtual method resolution operation (such as with `System.Runtime.InteropServices.IDynamicInterfaceCastable`), then the following cases also apply:

- The argument to the `isinst` IL instruction.
- The argument to the `castclass` IL instruction.
- The owning type of the method argument to `callvirt`, or `ldvirtftn`.
