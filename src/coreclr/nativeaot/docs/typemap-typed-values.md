# Type map typed values

`TypeMapping.TryGetValue` returns data that the types of a type map entry provide, without exposing the types themselves.

## Problem

`TypeMapping.GetOrCreateExternalTypeMapping<TGroup>()` and `TypeMapping.GetOrCreateProxyTypeMapping<TGroup>()`
return `Type` values. Consumers then read the information they need from the returned types with reflection
(`GetCustomAttribute`, `IsDefined`, ...). In NativeAOT this has two consequences:

* every mapped value type (the proxy types and the external type map targets) needs a type definition in the
  reflection metadata and a constructed MethodTable "with metadata";
* every attribute class read this way is reflection visible, with its virtual methods and everything they reach.

For an application with a very large number of mapped types, this keeps a very large number of types and members reflection
visible, even though the only thing the program does with them is read a few values.

## API

```csharp
namespace System.Runtime.InteropServices;

public interface ITypeMapValue<TValue>
{
    static abstract TValue Value { get; }
}

public static class TypeMapping
{
    public static bool TryGetValue<TTypeMapGroup, TValue>(string key, [MaybeNullWhen(false)] out TValue value);   // external type map
    public static bool TryGetValue<TTypeMapGroup, TValue>(Type key, [MaybeNullWhen(false)] out TValue value);     // proxy type map
}
```

The mapped types (the targets of `TypeMap` entries and the proxies of `TypeMapAssociation` entries) implement `ITypeMapValue<TValue>` for each
`TValue` they provide (explicitly if there is more than one). A group can be used with any number of `TValue` types, and with the other APIs
(`GetOrCreate*TypeMapping`). The calls need to know the group and `TValue` statically.

* `TryGetValue` returns `false` if the key is not in the map, or if the mapped type does not implement `ITypeMapValue<TValue>` (a mapped type only pays for the
  flavors it implements, and callers can probe). There is no throwing `GetValue`: callers that want one can build it on top of `TryGetValue` with the group and
  `TValue` known statically.
* The getter returns data. The mapped type is never exposed, so it is not reflection visible because of the type map. What the getter references is exactly as
  visible as the code says: a `typeof(X)` makes `X` reflection visible, a function pointer keeps its method, a string literal is a string.
* A value type `TValue` (evaluated at compile time, see below) can contain unmanaged data, `nint`/function pointers, strings, `Type` and nested structs. Avoid fields that the program doesn't need: every entry that
  implements the flavor pays for the whole struct. Use one small struct per kind of mapped type instead of one wide struct. For an external type map, the key is
  often the string the entry needs, so it doesn't need to be stored in the value.
* The getter must be a simple, side effect free expression. It can be evaluated at compile time, and then does not run in the program at all.

## ILC

### Flavors and requests

`TypeMapping.TryGetValue` is an intrinsic of the dataflow analysis (`TypeMapping_TryGetValue_External` for the string key overloads,
`..._Proxy` for the `Type` key overloads), like the existing `GetOrCreate*TypeMapping`. A call with a statically known group and `TValue`
adds an `ExternalTypeMapValuesRequestNode` / `ProxyTypeMapValuesRequestNode` (group and `TValue`). The existing APIs add an `ExternalTypeMapRequestNode` / `ProxyTypeMapRequestNode`
for the `Types` flavor. The flavors are independent: every request keeps the type symbol of the group alive, because the tables of the image refer to it.

`UsageBasedTypeMapManager.AllTypeMapsNode` roots, per group, a `Types` map node that is conditional on the `Types` request. The `Values` map nodes are
created per group and `TValue` when the `Values` request is marked. Entries have the same conditions in all flavors:

* proxy map: an entry exists if the key type is maximally constructable, plus the type loader candidate gate (`TypeLoaderArgumentGateNode`);
* external map: an entry without a trim target is unconditional, an entry with a trim target exists if the trim target is marked, plus the
  type loader dependencies of the trim target.

What differs is what the entry keeps alive:

| Flavor | Entry value | Dependency |
|---|---|---|
| Types | the mapped type | `MetadataTypeSymbol(value)` (reflection visible) |
| Values | data or a getter stub, per `TValue` | the data node or `MethodEntrypoint(TypeMapGetValueStub)`, none if the mapped type does not provide the value |

A group used only with `TryGetValue` therefore never marks `MetadataTypeSymbol(value)`. A `Values` flavor has at most the entries of the `Types`
flavor, and exactly the same ones for the same program.

### One hash table for all flavors

The ILC graph has nodes per flavor, but the image has one hash table per group (the same frozen hash table section as for the `Types` flavor alone).
When emitting, the nodes of a group are merged (`UsageBasedTypeMapManager.GetExternalTypeMaps` / `GetProxyTypeMaps`): if only the `Types` flavor was
requested the node is emitted as it is (the same bytes as without typed values), otherwise one node describes the flavors and the typed value flavors that were requested.
The scan phase (`ILScanner`) and the compile phase share the same logic: `ToAnalysisBasedNode` records the flavors and the marked entries of the merged
group, and the compile phase roots exactly them.

The group tuple is `(group, state, payload)`. For a valid group `state` is the set of flavors present (1 = `Types`, 2 = `Values`, 3 = both)
and `payload` is the hash table. State 0 is an invalid group, with the throwing stub as payload. An entry is the key, the mapped type if the state has
`Types`, then the data of each typed value flavor (below). The state 1 layout is the layout that existed before typed values.

Entry data: for each typed value flavor (in header order) the entry has a varint `(index << 2) | kind`:

| kind | meaning |
|---|---|
| 0 | the data is a constant: `index` is the position in the data table of the flavor |
| 1 | ILC does not evaluate the getter (it could not, or `TValue` is a reference type): `index` is the external reference of a stub (`TypeMapGetValueStub`) that calls it |
| 2 | the mapped type does not implement the interface (no data, no stub) |

If the state has the bit 2, the state is followed by the number of typed value flavors, and for each the
`TValue` type and the index + 1 of the data table (0: none), then the hash table. Groups without typed value flavors have the same layout as before.

The data table of a flavor is a dense array of `TValue` (`TypeMapValueArrayNode`), made of one embedded node (`TypeMapValueDataNode`) per mapped type, with the
exact layout of the struct (references are pointers to frozen strings and frozen `RuntimeType` objects, function pointers are relocations to the callable address
of the method). The runtime copies the bytes into the `TValue` with the regular write barriers of managed code. The references point to frozen objects, so the GC never moves or collects them.

`TValue` can be any type, but only value types are evaluated at compile time. For a reference type (for example `string` or `Type`) the entry always has kind 1: the program calls the
getter through a stub, as ILC does not create a shared constant object for a getter that could allocate. Like for any other `TValue`, the contract of `ITypeMapValue<TValue>` does not say
how many times the getter runs or whether it runs at all. The stub is named after the mapped type and the value type, as a mapped type can provide several values. A small struct that wraps
the reference gets its data at compile time and does not need the stub or the getter code, so it is the better choice when the value is a constant.

### Evaluation

`TypePreinit.ScanGetter` interprets the implementation of the getter. The interpreter models structs that contain references and function pointers field by field
(`FieldwiseStructValue`) only in this mode, so the preinitialization of static constructors is unchanged. What it supports: `initobj`/`ldloca`/`stfld`/`ldfld`/`ldflda`
on the struct (and nested structs), `newobj` of structs (including `Guid`), `ldftn` (also followed by `conv.i`), `ldstr`, `ldtoken` + `GetTypeFromHandle`, calls
to methods it can interpret, reads of `static readonly` fields (the read only static policy, also of other types whose static constructor can be evaluated with the same rules).
What makes it give up (the entry then gets a stub, `kind 1`): everything the interpreter can't model (extern methods, exceptions handling, allocations of classes, floating point arithmetic: the
interpreter keeps a `float` in double precision), stores into statics or reads of statics that are written by the program, a static
constructor on the type that declares the getter (for a default interface member this is the interface, not the mapped type), `ldftn` of methods that are P/Invokes, abstract, generic definitions, intrinsic or instance methods,
a struct that has references or function pointers and a layout the field by field model can't write (explicit, packed, with a size or an inline array), and anything the compiler would have to analyze in the compiled code:
`Requires*` attributes on methods, fields or types it refers to, dataflow annotations (signatures and generic arguments), intrinsics that the dataflow analysis handles, `DynamicDependency`. The compiled code is not generated, so
what the analysis would produce (warnings, roots) would be lost.
Preinitialization disabled (`--nopreinitstatics`, no optimization) also gives up. A getter that takes the address of a field of a struct and then overwrites the whole struct is
also left to the compiled code, as the interpreter replaces the values of the fields.

The values are evaluated once, by the first thing that needs them in the analysis phase (and the result is what the compilation phase emits), so both phases agree.
Getters of value types that fell back are logged with `--verbose`. The result of the evaluation does not keep the interpreter alive.

## Holder types and owner scopes

The entries of a group can be declared on holder types (`TypeMapTypeTargetAttribute<TGroup>`, see [typemap.md](../../../../docs/design/features/typemap.md)).

* `TypeMapMetadata` reads the type map attributes from *sources*: an assembly (as before) or a holder type. A `TypeMapTypeTarget` attribute of a source enqueues the holder it names for the same group, with the same
pending map logic as the assembly targets (a holder can have attributes of other groups, they are kept as a pending map until the holder is reached for that group). The sources are identified by module and
metadata handle, and each source is scanned once per group, so cycles are fine. A holder that is not an Ecma type (an array, for example) is ignored, like on CoreCLR. A holder that can't be resolved makes both maps of the group invalid, like an
unresolved assembly target. In `Record` mode (ReadyToRun) the `TypeMapTypeTarget` attributes are ignored: the runtime reads the holders from the attributes. A `TypeMapAssemblyTarget` on a holder is ignored (the attribute is for assemblies).
An attribute that fails to resolve only invalidates the map that it is for, so a bad entry on a holder for another group does not affect the group that traverses the holder.
* The group tables of the image are the same as before: the entries of the holders are in the same table as the assembly level entries, with the same conditions, so `GetOrCreate*TypeMapping`, `TryGetValue<TGroup, TValue>` and the shared
frozen table for a group used with both are unchanged. A program without holders has the same image as before.
* `TryGetValue<TGroup, TOwner, TValue>` is its own pair of intrinsics (`TypeMapping_TryGetValue_Owner_External` and `..._Owner_Proxy`). The map that starts at an owner is described in the image by a table of its own, and the group key of
that table is the type `TypeMapOwnerScope<TGroup, TOwner>` of System.Private.CoreLib (never instantiated): the compiler makes the scope type from the group and the owner, and everything that is keyed by the group (the request nodes, the flavor nodes,
the hash table of the groups, the data tables of the values) is keyed by the scope type instead. The map of a scope is made by `TypeMapMetadata.CreateFromOwnerType`, which starts the scan at the owner: the assembly level entries and the other assemblies are not read.
An owner without entries (or one that is not an Ecma type) has an empty map. The scope maps never have the `Types` flavor: only `TryGetValue` can ask for them.
* The requests of the scope maps are the same nodes as the requests of the other maps (the group is the scope type), so the same evaluation of the getters is shared: a mapped type that is in the group table and in a scope table is evaluated once, and it has
one data entry in each table that has it (the data is small, and a table has to be dense). Preinitialization of the values works the same for the entries of holders and scopes.
* The sections of the image are emitted when the program has scope maps even if there are no assembly level type maps (the manager can't know before the analysis is done): `ShouldSkipEmittingObjectNode` of the section nodes makes a program without any
type map keep not having the sections.

At run time (`TypeMapLazyDictionary.NativeAot.cs`) the overloads with an owner use `typeof(TypeMapOwnerScope<TGroup, TOwner>)` as the group handle: the same lookup of the group, the same flavors and the same decoding of the values.
A scope that was not requested at compile time (the call is not seen: through reflection or a delegate) throws the missing metadata exception like any other group.

## Runtime

NativeAOT (`TypeMapLazyDictionary.NativeAot.cs`): the readers of the type valued maps (`GetOrCreate*TypeMapping`) read the state of the group, skip the descriptors of the typed value flavors and use the hash table, so an application that only
uses them does not pay for the typed value reader. `TryGetValue` finds the group (cached per group type), checks that the group has the requested flavor
(otherwise it throws the same "missing metadata" exception as a group that was never requested), finds the `TValue` among the typed value flavors of the group
(same exception if there is none), then does the same hash table lookup as the existing dictionaries and decodes the data of the entry.

If a program uses only the `Values` flavor of a group, `GetOrCreate*TypeMapping` for it cannot exist in the program (it would have requested the `Types` flavor),
so there is no observable difference for existing code.

CoreCLR (non AOT, and the trimmed case): reflection based. The lazy maps that exist for the attribute based type maps are reused (cached per group) and the getter
of the mapped type is called through a cached open static generic helper (`T.Value` with `where T : ITypeMapValue<TValue>`). For ILLink and the Roslyn analyzer the new
methods are treated like the existing ones (the group is seen, the generic arguments must be statically known). Mono: `NotSupportedException`, like the existing type map APIs.

The CoreCLR lazy maps read the holder types too: after the assemblies are processed, the native reader (`TypeMapLazyDictionary_ProcessAttributes`) follows the `TypeMapTypeTarget` attributes of the assemblies it parsed (the type name is resolved
relative to the assembly that declares the attribute) and reads the entries on each holder once (a set of the processed types). The overloads with an owner use `TypeMapLazyDictionary_ProcessAttributesFromType`, which starts at the owner type instead of at the
assembly. The maps are cached per group and per (group, owner) with `ConditionalWeakTable`s.

## Known limits

* The group type and `TValue` must be statically known at the call site: a group or `TValue` that is, or contains, a generic parameter of the enclosing method or type gets the existing
  "cannot be statically determined" warning and no request, and the call fails at runtime with a missing metadata exception (a generic method that calls `TryGetValue` with concrete types is fine).
  Calls through reflection or delegates fail the same way, like the existing APIs. Calling a `Types` API on a group that is only used with `TryGetValue` fails the same way.
* A mapped type that is an interface or a generic type definition does not provide values (`TryGetValue` returns `false`), as the getter can't be called through it on either runtime.
  A mapped type that can't be loaded does not provide values on NativeAOT, while CoreCLR throws when it loads the type.
* On CoreCLR the lazy maps are cached per group type (a collectible group type does not stay alive because of them). The getters of mapped types are cached for the lifetime
  of the process, except for collectible types.
* ReadyToRun type maps are not extended (the API is NativeAOT and CoreCLR only).

## Soundness notes

* A mapped type that does not provide a value gets no data and no code.
* The data only points to frozen objects, so copying it into a managed location is GC safe.
* Structs that have references or function pointers and are packed (or have another layout that can't be written field by field) are not evaluated at compile time (they could have unaligned pointers).
  Packed structs of unmanaged data only are evaluated as bytes.
* Floating point arithmetic in a getter is not evaluated at compile time. The interpreter of static constructors keeps `float` values in double precision, and a getter that is evaluated must not give a different
  answer than the one it computes at run time.
