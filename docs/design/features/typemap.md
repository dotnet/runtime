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
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
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
/// Declare a type that holds type map entries and should be inspected during type map building.
/// </summary>
/// <typeparam name="TTypeMapGroup">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
public sealed class TypeMapTypeTargetAttribute<TTypeMapGroup> : Attribute
{
    /// <summary>
    /// Provide the type to look for type mapping attributes.
    /// </summary>
    /// <param name="type">Type that holds type map entries</param>
    public TypeMapTypeTargetAttribute(Type type)
    { }
}

/// <summary>
/// Create a type association between a type and its proxy.
/// </summary>
/// <typeparam name="TTypeMapGroup">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = false)]
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

    /// <summary>
    /// Tries to get the value that the type mapped by a key in the external type map provides.
    /// </summary>
    /// <typeparam name="TTypeMapGroup">Type universe</typeparam>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <remarks>
    /// Call sites are treated as an intrinsic by the Trimmer.
    /// </remarks>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static bool TryGetValue<TTypeMapGroup, TValue>(string key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Tries to get the value that the type mapped by a key in the proxy type map provides.
    /// </summary>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static bool TryGetValue<TTypeMapGroup, TValue>(Type key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Tries to get the value that the type mapped by a key in the external type map provides, looking only at the entries that start at an owner type.
    /// </summary>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static bool TryGetValue<TTypeMapGroup, TTypeMapGroupOwner, TValue>(string key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Tries to get the value that the type mapped by a key in the proxy type map provides, looking only at the entries that start at an owner type.
    /// </summary>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static bool TryGetValue<TTypeMapGroup, TTypeMapGroupOwner, TValue>(Type key, [MaybeNullWhen(false)] out TValue value);
}

/// <summary>
/// Implemented by the types that are the target of a type map entry (or the proxy of an association) to provide a value of type <typeparamref name="TValue"/>.
/// </summary>
public interface ITypeMapValue<TValue>
{
    static abstract TValue Value { get; }
}
```

Given the above types the following would take place.

1. Types involved in unmanaged-to-managed interop operations would be referenced in a
`TypeMapAttribute` assembly attribute that declared the external type system name, a target
type, and optionally a "trim-target" to determine if the target
type should be included in the map. If the `TypeMapAttribute` constructor that doesn't
take a trim-target is used the entry will always be emitted into the type map.

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

## Typed values

`GetOrCreate*TypeMapping` return `Type` values, so every mapped type has to be visible to reflection, even if the application only reads a few values from it (for example through attributes).
`TypeMapping.TryGetValue` returns the data a mapped type provides instead of the type: a mapped type (the target of a `TypeMapAttribute` entry or the proxy of a `TypeMapAssociationAttribute` entry)
implements `ITypeMapValue<TValue>` for each `TValue` it provides, and `TryGetValue<TTypeMapGroup, TValue>(key, out value)` finds the entry the same way the dictionaries do (a `string` key for the external type map, a `Type` key
for the proxy type map) and returns the value of its static getter. The mapped type is never exposed, so it doesn't become visible to reflection because of the entry when the application is compiled ahead of time: only what the getter
references does.

* The entries are included under the same rules as the entries of the dictionaries (below), and the two kinds of APIs can be used with the same group: the group has one table per direction that serves both.
* `TryGetValue` returns `false` if the key is not in the map or the mapped type doesn't implement `ITypeMapValue<TValue>`. Mapped types that are interfaces or generic type definitions don't provide values.
* Trimming tools consider calls to `TypeMapping.TryGetValue<,>` as intrinsics too: the group and `TValue` have to be fully instantiated.
* The getter should be a simple expression without side effects: it can be evaluated by the build, not run at all, or run any number of times, concurrently and on any thread. A `TValue` that is a reference type
  (`string`, `Type`, an interface or a class) can be evaluated by the build when the getter returns `null`, a string literal, `typeof(X)`, or an object that a `static readonly` field of another type holds
  (for example `public static IFoo Value => FooImpl.Instance;`): the value is then the same object as the static. Whether a getter that allocates (`new Foo()`) gives a new object per call is unspecified (it does today, as
  it is not evaluated by the build): use a `static readonly` field in another type for an object that has to be shared.
* The implementations are implementation-defined like the maps. For NativeAOT (the flavors of a group, the format of the image and what the compiler evaluates ahead of time) see
[typemap-typed-values.md](../../../src/coreclr/nativeaot/docs/typemap-typed-values.md).

## Holder types and owner lookups

Assembly level attributes are commonly read at run time (for example `AssemblyMetadataAttribute`), and a type map with thousands of entries makes every such read go through all of them.
The entries (`TypeMapAttribute` and `TypeMapAssociationAttribute`) can also be declared on a class, and `TypeMapTypeTargetAttribute<TTypeMapGroup>(Type)`, on the assembly or on a class, points to such a *holder* type.
The type is only a container of attributes: it is never constructed or exposed, and it needs no reflection visibility.

* The holder types are followed for the group of the attribute, transitively (a holder can point to other holders, in a cycle too, and in other assemblies).
The attributes of a holder that are for another group are not read for this group.
* The entries of a holder are part of the type map of the group exactly like the assembly level entries: the same keys and duplicate rules, the same trimming rules (the trim target of an external entry, the key type of an association), and
`GetOrCreate*TypeMapping` and `TryGetValue<TTypeMapGroup, TValue>` see the union of the assembly level entries and of the entries of the holders that are reachable from them.
Only the holders that the assembly level attributes of the assemblies that are scanned (see below) point to, directly or transitively, are part of the type map of the group.
* `TryGetValue<TTypeMapGroup, TTypeMapGroupOwner, TValue>` starts at `TTypeMapGroupOwner` instead: it only considers the entries declared on that type and on the types reachable from it through
`TypeMapTypeTargetAttribute<TTypeMapGroup>`, transitively. The assembly level entries, the `TypeMapAssemblyTargetAttribute` targets and the other libraries are not considered, and the owner does not have to be reachable from the assembly.
This gives a library a local type map that doesn't need whole program analysis. The group, the owner and `TValue` have to be fully instantiated like for the other overloads.
In NativeAOT a type that is used as the owner is visible to reflection (see typemap-typed-values.md); the holders that are only linked are not.
The owner has to be a type that C# allows as a type argument: a `static` class can't be used (CS0718), so owner types that are meant for this overload can't be `static`.
* Entries on a holder behave the same everywhere else: a holder entry is trimmed by the same rules as an assembly level entry, and the type map attributes are removed from the output of trimming tools.
* The holders are not supported by ILLink and the Roslyn analyzer yet: ILLink doesn't read the attributes of the types, so a trimmed CoreCLR application doesn't have the entries of the holders.
* ReadyToRun: the type maps of a module only cover its assembly level entries. The holder types are always read from the attributes at run time, after the assemblies of the group, and their entries of the external type map are checked against the ones of the images for duplicates.

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
  - If the trimming tool can determine that this box does not escape and could be stack allocated, it can ignore this `box` instruction and any corresponding `unbox` or `unbox.any` instructions.
- The argument to the `mkrefany` instruction.
- The argument to the `refanyval` instruction.
- The argument to the `newarr` instruction.
- The type of a method argument to the `newobj` instruction if it is a class type.
- The owning type of an instance method argument to `call` or `ldftn`, or the owning type of any method argument to `callvirt` or `ldvirtftn`.
  - If the owning type is an interface and the trimming tool can determine that there is only one implementation of the interface, it is free to interpret the method token argument as though it is the method on the only implementing type.
- The generic argument to the `Activator.CreateInstance<T>` method.
- Calls to `Type.GetType` with a constant string representing the type name.

Many of these instructions can be passed a generic parameter. In that case, the trimming tool should consider type arguments of instantiations of that type as having met one of these rules and include any entries with those types as "trim target" types.

For pointer, byref, and array types as trim targets, the entries are preserved if the element type meets the requirements above to keep the entry. A trimming tool is free to remove entries if it can prove that the pointer, byref, or array type cannot be represented at runtime.

### Proxy Type Map

An entry in the Proxy Type Map is included when the "source type" is referenced in one of the following ways:

- The argument to the `ldtoken` IL instruction when `DynamicallyAccessedMembersAttribute` is specified with one of the flags that preserves constructors for the storage location.
- Calls to `Type.GetType` with a constant string representing the type name when `DynamicallyAccessedMembersAttribute` is specified with one of the flags that preserves constructors for the storage location.
- The type of a method argument to the `newobj` instruction.
- The generic argument to the `Activator.CreateInstance<T>` method.
- The argument to the `box` instruction.
- The argument to the `newarr` instruction.
- The argument to the `mkrefany` instruction.
- The argument to the `refanyval` instruction.

If the type is an interface type and the user could possibly see a `RuntimeTypeHandle` for the type as part of a casting or virtual method resolution operation (such as with `System.Runtime.InteropServices.IDynamicInterfaceCastable`), then the following cases also apply:

- The argument to the `isinst` IL instruction.
- The argument to the `castclass` IL instruction.
- The owning type of the method argument to `callvirt`, or `ldvirtftn`.

Finally, if the trimming tool determines that it is impossible to retrieve a `System.Type` instance the represents the "source type" at runtime, then the entry may be omitted from the Proxy Type Map as its existence is unobservable.

For pointer, byref, and array types as source types, the entries are preserved if the element type meets the requirements above to keep the entry. A trimming tool is free to remove entries if it can prove that the pointer, byref, or array type cannot be represented at runtime.
