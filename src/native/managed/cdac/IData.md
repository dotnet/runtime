# IData

`IData<TSelf>` is the cdac abstraction for a *typed view* over a region of
target memory. Each implementation reads its fields out of the target by
consulting the cdac type descriptor (or, for managed types, the runtime
metadata via [`ManagedTypeSource`](ManagedTypeSource.md)), and exposes a
strongly-typed C# property surface that contracts and consumers can use
without re-deriving offsets.

```csharp
public interface IData<TSelf> where TSelf : IData<TSelf>
{
    static abstract TSelf Create(Target target, TargetPointer address);
}
```

Instances are produced lazily and cached by the target's
`ProcessedData.GetOrAdd<T>(address)` helper, so a given (`T`, `address`)
pair is materialized at most once per target session.

## Authoring an IData class

There are two approaches to writing an `IData<T>` implementation,
listed in order of preference:

1. **Source-generated** (recommended) -- declare the C# property surface,
   decorate it with cdac attributes, and the source generator emits the
   constructor, `IData<T>.Create`, optional `TypeHandle`, optional
   `Write{Name}` write-back methods, static field accessors, and the
   `Address` property. Use this for all new types unless the declarative
   surface cannot express the required logic.

2. **Source-generated with `OnInit`** -- when the declarative attributes
   cover most of the type but a few fields need custom logic (e.g.
   stripping a tag bit from a pointer, reading from a second descriptor,
   variable-count loops, raw byte buffers, or multiple
   `Target.TypeInfo` lookups), add a
   `partial void OnInit(Target target, TargetPointer address)`
   implementation. The generator calls it at the end of the constructor
   after all `[Field]` reads are complete. This covers all scenarios
   that the declarative surface cannot express.

This document describes the source-generated path.

## The source generator at a glance

`Microsoft.Diagnostics.DataContractReader.DataGenerator` is a Roslyn
`IIncrementalGenerator` wired into
`Microsoft.Diagnostics.DataContractReader.Contracts` as a build-time
analyzer. It scans for classes carrying `[CdacType]` and emits a
`partial` companion containing:

* A `public TargetPointer Address { get; }` property (always emitted --
  the instance remembers the address it was constructed from).
* A `public {Name}(Target target, TargetPointer address)` constructor
  that resolves the type name against native descriptors and managed
  metadata, then does per-field reads through the `LayoutSet` cascade.
* A `static {Name} IData<{Name}>.Create(...) => new {Name}(target, address);`
  one-liner.
* A `private static readonly string[] _typeNames = { ... }` array
  holding the candidate type names from `[CdacType]`.
* For types with `HasTypeHandle = true`: a
  `public static ITypeHandle TypeHandle(Target target)` accessor.
* For each `[Field(Writable = true)]` property: a
  `public void Write{Name}(T value)` method. The class captures the
  `Target` in a private `_target` field when any writable fields exist.
* For each `[StaticAddress]` / `[StaticReference]` partial method
  declaration: a corresponding implementation that tries native globals
  first (`TypeName.fieldName`), then falls back to `ManagedTypeSource`.
* A `partial void OnInit(Target target, TargetPointer address);`
  declaration plus a call to it at the end of the constructor.

The user provides the property declarations and (optionally) the
`OnInit` implementation. Everything else is emitted.

### User-side conventions

* Mark the class `internal sealed partial`.
* Implement `IData<T>` on the class declaration.
* Declare data properties as `public T Prop { get; }` (get-only auto
  properties). For properties that the generator or hand-written code
  needs to assign outside the constructor, use
  `{ get; private set; }`. For non-nullable reference-typed properties
  that are populated only inside `OnInit`, prefer annotating `OnInit`
  with `[MemberNotNull(nameof(X), ...)]` over `required` or
  `= null!;` -- it lets the compiler verify the property is assigned
  along every path through `OnInit` without forcing callers to use
  object-initializer syntax or accepting a deliberately-lying null.

## Attribute surface

All attributes live in the root
`Microsoft.Diagnostics.DataContractReader` namespace (declared in
`CdacAttributes.cs` inside the
`Microsoft.Diagnostics.DataContractReader.Abstractions` assembly).

### Class-level: `[CdacType]`

Marks a class for source generation. The constructor accepts any number
of candidate type names (`params string[] names`). At runtime the
cascade tries each name against native data descriptors first, then
managed metadata. The first match wins.

```csharp
// One or more candidate type names.
[CdacType("MethodTable")]
[CdacType("Lock", "System.Threading.Lock")]
[CdacType(nameof(DataType.Exception), "System.Exception")]

// Parameterless -- no descriptor lookup. Use with [RawOffset] or OnInit only.
[CdacType]

// HasTypeHandle -- emits a TypeHandle(Target) accessor.
[CdacType("Lock", "System.Threading.Lock", HasTypeHandle = true)]
```

### Property-level: `[Field]`

Reads a descriptor field. The read kind is inferred from the property
type:

| Property type | Generated read |
|---|---|
| `uint` / `int` / `byte` / `ushort` / ... | `target.ReadField<T>(address, type, "name")` |
| `bool` | `target.ReadField<byte>(address, type, "name") != 0` |
| `TargetPointer` | `target.ReadPointerField(address, type, "name")` |
| `TargetNUInt` | `target.ReadNUIntField(address, type, "name")` |
| `TargetNInt` | `target.ReadNIntField(address, type, "name")` |
| `TargetCodePointer` | `target.ReadCodePointerField(address, type, "name")` |
| `T` where `T : IData<T>` (in-place struct) | `target.ReadDataField<T>(address, type, "name")` (i.e. `ProcessedData.GetOrAdd<T>(address + offset)`) |
| `T?` (Nullable<T> on a value type) | wrapped in a `type.Fields.ContainsKey(...)` guard; missing descriptor field yields `default(T?)` (i.e. `null`) |

Parameters:

* `[Field("name", ...)]` -- one or more candidate descriptor field names,
  passed as positional `params string[]`. The cdac generator's runtime
  cascade tries each name first against the native descriptor, then against
  the managed descriptor. The first match wins.

  By default, the C# property name is **always appended as the lowest-priority
  candidate** (de-duped if already in the list). This means `[Field]` with no
  names is equivalent to `[Field("<PropertyName>")]`, and an explicit name list
  still falls back to the property name if none of the listed names matched.
  Set `UsePropertyName = false` to opt out of the property-name fallback
  (rare; useful when the C# property name happens to collide with an
  unrelated descriptor field).

  Examples:
  - `[Field] public uint Id` -- candidates `["Id"]`.
  - `[Field("_state")] public uint State` -- candidates `["_state", "State"]`.
    On a managed-only class, native is absent so the cascade falls to managed
    and finds `_state`.
  - `[Field("State", "_state")] public uint State` -- candidates
    `["State", "_state"]` (property name already present, not appended again).
  - `[Field("Id", "m_id")] public uint Id` -- runtime-version rename;
    candidates `["Id", "m_id"]`.
  - `[Field("X", UsePropertyName = false)] public uint State` -- candidates
    `["X"]` only; the property name `State` is not tried.
* `[Field(Pointer = true)]` -- for `IData<T>`-typed properties: read a
  pointer from the descriptor field, then materialize the pointee via
  `target.ProcessedData.GetOrAdd<T>(pointer)`. Use only when the
  pointer is guaranteed non-null; otherwise expose the property as a
  raw `TargetPointer` and let the consumer materialize on demand.
* `[Field(Writable = true)]` -- emit a
  `public void Write{Name}(T value)` method that writes the value back
  to the target's memory and updates the in-memory snapshot. When any
  writable fields exist, the generator emits a `private readonly Target
  _target` field that is captured in the constructor, so Write methods
  do not need a `Target` parameter. The property must have a setter
  (`set` or `private set`), the read kind must be `Primitive`, `Bool`,
  or `NUInt`, and the class must use a descriptor (`[CdacType("Name")]`
  or `[CdacType("Name1", "Name2")]` -- writes go through the descriptor
  field offset regardless of which side supplied it).

### Property-level: `[RawOffset]`

Bypasses the descriptor. Reads from a hardcoded byte offset relative
to `address`. The read kind is inferred from the property type the same
way `[Field]` infers it.

| Form | Generated |
|---|---|
| `[RawOffset(12)] public uint X { get; }` | `X = target.Read<uint>(address + 12);` |
| `[RawOffset(60, LittleEndian = true)] public int Lfanew { get; }` | `Lfanew = target.ReadLittleEndian<int>(address + 60);` |
| `[RawOffset(4)] public ImageFileHeader Hdr { get; }` | `Hdr = target.ProcessedData.GetOrAdd<ImageFileHeader>(address + 4);` |

Used for well-known external file-format layouts (PE/COFF, Webcil)
where the offsets are fixed by the format spec rather than the runtime
descriptor. The `LittleEndian` flag is required for PE/COFF, which is
always little-endian regardless of the target architecture.

### Property-level: `[FieldAddress]`

Materialize a `TargetPointer` to the *address* of a descriptor field,
without reading its contents.

```csharp
[FieldAddress] public TargetPointer Header { get; }
// generates: Header = address + (ulong)type.Fields["Header"].Offset;
```

Use with `[FieldAddress("name")]` if the C# property name differs from
the descriptor name. Common for sub-structures and arrays that the
consumer wants to iterate themselves.

### Property-level: `[InstanceDataStart]`

Generates `address + type.Size!.Value` -- the byte after the type's
last declared field. Used by container types (e.g. `Array`,
`MethodDescChunk`, `Object`) to expose the start of the per-instance
payload.

```csharp
[InstanceDataStart]
public TargetPointer Data { get; }
// generates: Data = address + type.Size!.Value;
```

### Method-level (`static partial`): static-field accessors

These attributes target `static partial` method declarations. The
generator emits the implementation, which first tries native globals
(using the naming scheme `TypeName.fieldName` for each candidate type
name), then falls back to `ManagedTypeSource`.

| Attribute | Generated method body |
|---|---|
| `[StaticAddress("field")]` | Tries `target.TryReadGlobalPointer("TypeName.field")` for each candidate type name. Falls back to `target.Contracts.ManagedTypeSource.TryGetStaticFieldAddress(name, "field")`. Returns the address of the static slot. |
| `[StaticReference("field")]` | Same resolution as `StaticAddress`, but dereferences the result: `target.ReadPointer(addr)`. Returns `TargetPointer.Null` if neither source has the static. |

### Method-level (`static partial`): thread-static field accessor

| Attribute | Generated method body |
|---|---|
| `[ThreadStaticAddress("field")]` | Tries `target.Contracts.ManagedTypeSource.TryGetThreadStaticFieldAddress(name, "field", thread)` for each candidate type name. The generated method takes an additional `TargetPointer thread` parameter identifying the thread whose TLS slot to read. |

Example:

```csharp
[CdacType("ComWrappers", "System.Runtime.InteropServices.ComWrappers")]
internal sealed partial class ComWrappers : IData<ComWrappers>
{
    [StaticReference("s_allManagedObjectWrapperTable")]
    public static partial TargetPointer AllManagedObjectWrapperTable(Target target);

    [StaticReference("s_nativeObjectWrapperTable")]
    public static partial TargetPointer NativeObjectWrapperTable(Target target);
}
```

## The `OnInit` escape hatch

Every generator-emitted constructor ends with a call to
`partial void OnInit(Target target, TargetPointer address)`. If the user
provides no implementation, the C# compiler elides both the call and
the signature. When the user *does* provide an implementation, it runs
after all the declarative `[Field]` / `[FieldAddress]` / etc.
assignments and can perform any custom reads.

Use `OnInit` for any pattern the declarative surface can't express:

* Variable-count loops over arrays whose length is a global or another
  field. (`Bucket`, `RCW`, `ComCallWrapper`, ...)
* Reading raw byte buffers via `target.ReadBuffer(...)`. (`TableSegment`,
  `PrecodeMachineDescriptor`, `ComInterfaceEntry`'s GUID.)
* Reads that compute values from other read fields (bitmask cleanup,
  derived flags). (`RangeSectionFragment.Next & ~1ul`,
  `DynamicStaticsInfo.GCStatics & mask`.)
* Conditional reads with custom predicates beyond `ContainsKey`.
  (`SyncBlock`, `SyncTableEntry`.)
* Reading fields from a *different* `Target.TypeInfo` than the one the
  class is anchored on. (TypeDesc subclasses reading the base
  `TypeAndFlags` from `DataType.TypeDesc`.)

For properties populated only inside `OnInit`, declare them as
`{ get; private set; }` and annotate `OnInit` with
`[MemberNotNull(nameof(X), ...)]` so the compiler is satisfied without
the `= null!;` workaround:

```csharp
[CdacType(nameof(DataType.Bucket))]
internal sealed partial class Bucket : IData<Bucket>
{
    public TargetPointer[] Keys { get; private set; }
    public TargetPointer[] Values { get; private set; }

    [MemberNotNull(nameof(Keys), nameof(Values))]
    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Bucket);
        uint numSlots = target.ReadGlobal<uint>(Constants.Globals.HashMapSlotsPerBucket);
        Keys = new TargetPointer[numSlots];
        Values = new TargetPointer[numSlots];
        // ... populate
    }
}
```

## Write-back

Add `Writable = true` to a `[Field]` to emit a `Write{Name}` method
that mutates the target's memory and updates the in-memory snapshot:

```csharp
[CdacType(nameof(DataType.Module))]
internal sealed partial class Module : IData<Module>
{
    [Field(Writable = true)] public uint Flags { get; private set; }
    // ...
}

// Generated (the class captures _target when any writable fields exist):
public void WriteFlags(uint value)
{
    LayoutSet layouts = LayoutSet.Resolve(_target, _typeNames);
    layouts.Select(Address, out var t, out var b, out var n, "Flags");
    _target.WriteField<uint>(b, t, n, value);
    Flags = value;
}
```

Rules:

* Property type must be a primitive integer, `bool`, `TargetNUInt`, or `TargetNInt`.
* Property must have an explicit setter (`set` or `private set`, not
  `init`).
* Class must use a descriptor source (`[CdacType("Name")]` or
  `[CdacType("Name1", "Name2")]`); writes go through the
  descriptor field offset regardless of which side supplied it.
* Plain `[RawOffset]` is not writable (no descriptor entry to update).

## Fallback

`[Field]` accepts one or more candidate field names. At runtime the
generator's emitted ctor tries every candidate name against the native
descriptor first, then every candidate name against the managed metadata.
The first match wins. The `[CdacType]` bag of names lists candidate type
names that are tried against both sources -- if any candidate resolves
to a native descriptor or managed metadata, that source is used.

### Why it exists

The team has decided some types (e.g. `Exception`, `String`) live in the
native cdac descriptor for fast, header-free access during basic dump
analysis. Other types (`Lock`, several COM wrappers) get their layout
from managed metadata via `IManagedTypeSource`. The classification is
reviewed per-type and may change in follow-ups; the per-field cascade
lets a single IData class survive a type moving from one source to the
other (or being added to the native descriptor incrementally, one field
at a time) without C# changes.

### How the cascade works

1. The generator emits a call to
   `LayoutSet.Resolve(target, typeNames)` at the top of the ctor.
   This tries each candidate type name against the native descriptors
   first, then against managed metadata. It returns a `LayoutSet`
   carrying whichever `Target.TypeInfo`(s) exist.

2. Every `[Field]` read calls
   `LayoutSet.Select(address, out type, out baseAddr, out name, ...names)`.
   The helper walks `names` against the native `TypeInfo`'s `Fields`
   map first; if a name matches, the read is anchored at `address`
   (native offsets already include the object header by convention). If
   no name matches against native (or the native descriptor wasn't
   available), the helper walks `names` against the managed `TypeInfo`'s
   `Fields` map; if a name matches, the read is anchored at `address`
   as well (managed offsets are pre-adjusted by `ManagedTypeSource` to
   include the object header). The generated code then calls the
   appropriate `target.ReadField<T>(baseAddr, type, name)` overload.

3. If no name matches in either source, `LayoutSet.Select` throws
   `InvalidOperationException` with the candidate name list in the
   message. If the resolution at step 1 found *neither* source, the
   ctor itself throws.

### Cross-source: same field names

When native and managed metadata use the same field names, a single
name per field is sufficient. The cascade tries the name against native
first, then managed:

```csharp
[CdacType("Lock", "System.Threading.Lock")]
internal sealed partial class Lock : IData<Lock>
{
    [Field("_owningThreadId")] public int  OwningThreadId  { get; }
    [Field("_state")]          public uint State           { get; }
    [Field("_recursionCount")] public uint RecursionCount  { get; }
}
```

When the native cdac descriptor for `Lock` is present, the cascade
finds `"_owningThreadId"` in the native type info and reads at
`address + nativeOffset`. When only managed metadata is present, it
finds `"_owningThreadId"` in the managed type info instead.

### Cross-source: different field names

When native and managed metadata disagree on field names, list both
candidates. The cascade picks the first one that matches in either
source:

```csharp
[CdacType("Lock", "System.Threading.Lock")]
internal sealed partial class Lock : IData<Lock>
{
    [Field("OwningThreadId", "_owningThreadId")] public int  OwningThreadId  { get; }
    [Field("State",          "_state")]          public uint State           { get; }
    [Field("RecursionCount", "_recursionCount")] public uint RecursionCount  { get; }
}
```

When the native descriptor uses `"OwningThreadId"`, the cascade finds
it first. When only managed metadata is present, it falls past the
unmatched `"OwningThreadId"` on the native side, then tries
`"_owningThreadId"` against managed.

### Cross-version aliases

When a field has been renamed across runtime or BCL versions, simply
list the candidates in priority order. The cascade tries them in order
against each source:

```csharp
[CdacType("Thread")]
internal sealed partial class Thread : IData<Thread>
{
    // Native field was renamed from "m_id" to "Id" in a recent runtime.
    [Field("Id", "m_id")] public uint Id { get; }
}
```

There is no distinction between a "primary" name and an "alias" --
all candidates are equal entries in one list, tried in declaration order.

### Address-base rule for reference types

For a managed reference type the metadata's field offsets are anchored
at the *first instance-data byte*, i.e. *after* the object header
(MethodTable pointer + sync block index). `ManagedTypeSource` pre-adjusts
these offsets by adding `Object.Size`, so all reads use `address + offset`
uniformly -- the caller does not need to know whether the field came from
a native descriptor or managed metadata.

The native cdac descriptor's field offsets, by convention, already include
the header for managed reference types -- so native reads also use
`address + offset` directly.

### `[FieldAddress]` and `[InstanceDataStart]` under fallback

`[FieldAddress]` accepts the same `params string[]` name list as
`[Field]`, and works through `LayoutSet.Select` to obtain the
resolved type info and base address, then computes the absolute
field address regardless of which source resolves the field.

`[InstanceDataStart]` returns `address + layouts.InstanceSize`,
where `InstanceSize` is whichever side's `Size` is populated (native
preferred when both have one). For pure native-only classes this
reduces to `address + type.Size` exactly as before.

## Examples

### Pure native descriptor read

```csharp
[CdacType(nameof(DataType.MethodTable))]
internal sealed partial class MethodTable : IData<MethodTable>
{
    [Field] public uint MTFlags { get; }
    [Field] public uint BaseSize { get; }
    [Field] public TargetPointer EEClassOrCanonMT { get; }
    [Field] public TargetPointer Module { get; }
    // ...
}
```

### Native descriptor + StoreAddress + Pointer + InstanceDataStart

```csharp
[CdacType(nameof(DataType.Object))]
internal sealed partial class Object : IData<Object>
{
    [Field("m_pMethTab", Pointer = true)]
    public MethodTable MethodTable { get; }

    [InstanceDataStart]
    public TargetPointer Data { get; }
}
```

### Managed type with field aliasing

```csharp
[CdacType("System.Threading.Lock")]
internal sealed partial class Lock : IData<Lock>
{
    [Field("_state")]          public uint State { get; }
    [Field("_owningThreadId")] public int OwningThreadId { get; }
    [Field("_recursionCount")] public uint RecursionCount { get; }
}
```

### Hybrid native + managed

```csharp
[CdacType(nameof(DataType.Exception), "System.Exception")]
internal sealed partial class Exception : IData<Exception>
{
    [Field("_message")]          public TargetPointer Message { get; }
    [Field("_innerException")]   public TargetPointer InnerException { get; }
    [Field("_HResult")]          public int HResult { get; }
    // ...
}
```

The class exposes both `Exception.TypeHandle(target)` (managed
identity) and the field reads at descriptor-defined offsets (native
layout).

### Inline value type (no object header)

```csharp
[CdacType("...+Entry")]
internal sealed partial class ConditionalWeakTableEntry : IData<ConditionalWeakTableEntry>
{
    [Field("HashCode")] public int HashCode { get; }
    [Field("Next")]     public int Next { get; }
    [FieldAddress("depHnd")]
    public TargetPointer DepHndAddress { get; }
}
```

### Hardcoded offsets (file format)

```csharp
[CdacType]
internal sealed partial class ImageDosHeader : IData<ImageDosHeader>
{
    [RawOffset(60, LittleEndian = true)]
    public int Lfanew { get; }
}
```

### Statics-only type

```csharp
[CdacType("ComWrappers", "System.Runtime.InteropServices.ComWrappers")]
internal sealed partial class ComWrappers : IData<ComWrappers>
{
    [StaticReference("s_allManagedObjectWrapperTable")]
    public static partial TargetPointer AllManagedObjectWrapperTable(Target target);

    [StaticReference("s_nativeObjectWrapperTable")]
    public static partial TargetPointer NativeObjectWrapperTable(Target target);
}
```

The generator emits both a `Create` (returning an empty instance) and
the two static accessor implementations; no `[Field]` properties are
required.

### Writable native field

```csharp
[CdacType(nameof(DataType.Module))]
internal sealed partial class Module : IData<Module>
{
    [Field(Writable = true)] public uint Flags { get; private set; }
    [Field] public TargetPointer Assembly { get; }
    // ...
}

// usage
Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(addr);
module.WriteFlags(newFlags);
```

### Source-generated with `OnInit` custom logic

```csharp
[CdacType(nameof(DataType.RangeSectionFragment))]
internal sealed partial class RangeSectionFragment : IData<RangeSectionFragment>
{
    [Field] public TargetPointer RangeBegin { get; }
    [Field] public TargetPointer RangeEndOpen { get; }
    [Field] public TargetPointer RangeSection { get; }

    // The Next pointer uses the low bit as a collectible flag; strip it.
    public TargetPointer Next { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RangeSectionFragment);
        Next = target.ReadPointerField(address, type, nameof(Next)) & ~1ul;
    }
}
```

## Good practices for authoring IData classes

The IData abstraction works best when classes are kept as **dumb data
views** -- one C# property per descriptor field, no derived logic, no
expensive work hidden behind the constructor. Following these rules
keeps the per-`Create` cost predictable, the descriptor surface
visible at the property level, and the consumer in control of what
gets materialized.

### Data exposed should not be modifiable

IData properties should not expose mutable collections or types that
allow external callers to change the snapshot. The only legitimate
mutation path is through `Write{Name}` methods for write-back fields.

For collection-typed properties populated in `OnInit`, expose them as
`IReadOnlyList<T>` (or another read-only interface) with
`{ get; private set; }` and assign a `List<T>` built inside `OnInit`.
This prevents callers from accidentally mutating the cached snapshot.

Bad:

```csharp
public List<TargetPointer> Elements { get; } = [];

partial void OnInit(Target target, TargetPointer address)
{
    Elements.Add(...);
}
```

Good:

```csharp
public IReadOnlyList<TargetPointer> Elements { get; private set; } = [];

[MemberNotNull(nameof(Elements))]
partial void OnInit(Target target, TargetPointer address)
{
    List<TargetPointer> elements = [];
    elements.Add(...);
    Elements = elements;
}
```

### Avoid algorithm logic in IData classes

The constructor (and `OnInit`) should be limited to reads from the
target -- enough to populate the declared properties. Derived
computations, interpretation, and any contract algorithms belong in
the consuming contract implementation (`Contracts\*.cs`), not in the
IData class. Bad:

```csharp
[CdacType(nameof(DataType.Thread))]
internal sealed partial class Thread : IData<Thread>
{
    [Field] public uint State { get; }

    // BAD: classifies state into an enum-shaped result; that's
    // contract-level interpretation, not a field read.
    public ThreadKind Kind => (State & 0xF) switch { ... };
}
```

Better:

```csharp
[CdacType(nameof(DataType.Thread))]
internal sealed partial class Thread : IData<Thread>
{
    [Field] public uint State { get; }
}

// In Contracts\Thread_1.cs:
public ThreadKind GetThreadKind(TargetPointer addr)
{
    Data.Thread t = _target.ProcessedData.GetOrAdd<Data.Thread>(addr);
    return (t.State & 0xF) switch { ... };
}
```

Expression-bodied properties for *trivial* projections of an already-
read field (e.g. `bool IsAlive => ReferenceCount != 0;`) are fine --
they don't add target reads or hide work.

### Match the descriptor's field type verbatim

The C# property's declared type should follow the descriptor's
declared type without widening, narrowing, or sign-flipping:

| Descriptor type     | C# property type    |
|---------------------|---------------------|
| `T_UINT8`           | `byte` (or `bool` -- see below) |
| `T_INT8`            | `sbyte`             |
| `T_UINT16`          | `ushort`            |
| `T_INT16`           | `short`             |
| `T_UINT32`          | `uint`              |
| `T_INT32`           | `int`               |
| `T_UINT64`          | `ulong`             |
| `T_INT64`           | `long`              |
| `T_NUINT`           | `TargetNUInt`       |
| `T_NINT`            | `TargetNInt`        |
| `T_PTR`             | `TargetPointer`     |

Sign-flipping silently corrupts write-back: declaring a `T_INT32`
field as `uint` and then calling `Write{Name}(target, value)` will
write a re-interpreted bit pattern that the runtime may treat as a
different value. Widening (`byte` -> `int`) hides the underlying
storage size and makes the C# code unable to round-trip data through
the `Write{Name}` path safely.

`bool` is the one allowed deviation: a `T_UINT8` field declared as
`bool` is treated by the generator as a "non-zero" view of the byte
(`ReadField<byte>(...) != 0`) with the corresponding write back as
`(byte)(value ? 1 : 0)`.

### Avoid eagerly dereferencing pointers to other IData classes

When a field is a `TargetPointer` that points to another IData
type, expose it as a plain `TargetPointer` and let the consumer
materialize on demand. Avoid using `[Field(Pointer = true)]` to
auto-`GetOrAdd` the pointee.

The problem is **null semantics**. A `TargetPointer` field can
legitimately be `TargetPointer.Null` at runtime, but with
`Pointer = true` the generator can't represent that cleanly:

* If the property is non-nullable (`MethodTable MT`), a null pointer
  has to materialize *something* -- typically `GetOrAdd(null)`, which
  silently constructs a bogus instance reading from address zero.
* If the property is nullable (`RuntimeThreadLocals?`), the read shape
  has to grow a `if (ptr == Null) null else GetOrAdd(ptr)` branch and
  every caller deals with the resulting `T?`. Two levels of
  nullability (descriptor-optional vs runtime-null vs property-
  declared) collide and become hard to reason about.

Storing the raw pointer leaves both signals in the same shape
(`TargetPointer` with `TargetPointer.Null` as the absence sentinel)
and pushes the "should I materialize this" decision to the consumer,
which already has the context to decide.

The only cases where `[Field(Pointer = true)]` is appropriate:

* The pointee is **guaranteed non-null** at all times (e.g.
  `Object.MethodTable` -- every managed object has a non-null MT).
* The cost of always materializing it is acceptable for *every*
  reader of the parent type.

When in doubt, expose a `TargetPointer` and let the caller decide.

Avoid:

```csharp
[CdacType(nameof(DataType.Thread))]
internal sealed partial class Thread : IData<Thread>
{
    // BAD: forces a runtime-null-vs-non-null story onto every consumer
    // through the IData property's nullability.
    [Field(Pointer = true)]
    public RuntimeThreadLocals? RuntimeThreadLocals { get; }
}
```

Prefer:

```csharp
[CdacType(nameof(DataType.Thread))]
internal sealed partial class Thread : IData<Thread>
{
    // Pointer only. Caller materializes if/when needed:
    // if (thread.RuntimeThreadLocals != TargetPointer.Null)
    //     target.ProcessedData.GetOrAdd<RuntimeThreadLocals>(thread.RuntimeThreadLocals)
    [Field] public TargetPointer RuntimeThreadLocals { get; }
}
```

### Inline IData fields are fine

A `[Field]` whose property type is an `IData<T>` **without**
`Pointer = true` reads the in-place sub-struct (`address + offset`,
via `ReadDataField<T>`). There's no pointer indirection -- the bytes
are physically part of the enclosing type -- and the cost is bounded
by the embedded struct's own field list. Use this freely:

```csharp
[CdacType(nameof(DataType.LoaderAllocator))]
internal sealed partial class LoaderAllocator : IData<LoaderAllocator>
{
    // OK: ObjectHandle is laid out inline inside LoaderAllocator;
    // ReadDataField<ObjectHandle> materializes the embedded struct.
    [Field] public ObjectHandle ObjectHandle { get; }
}
```

### One IData class, one descriptor

Don't read fields from a different `Target.TypeInfo` than the one the
class is anchored on. If a type's layout genuinely spans two
descriptors (e.g. `ParamTypeDesc` inheriting `TypeAndFlags` from the
base `TypeDesc` descriptor), use `OnInit` to do the cross-descriptor
read explicitly -- don't try to express it through `[CdacType]`
alone.

## Migrating types between sources

The IData source generator is designed so that types can move between
managed metadata and native data descriptors without changing the C#
IData class. The bag-of-names `[CdacType]` constructor and the
field-name cascade make migration transparent to contract consumers.

### Migrating managed types to native

When a type currently backed by managed metadata needs to move to a
native data descriptor (e.g. for dump analysis without managed metadata),
add a native data descriptor entry using the managed type's fully
qualified name and field names. No changes to the IData class are needed.

**Steps:**

1. In `datadescriptor.inc`, add a `CDAC_TYPE_BEGIN` / `CDAC_TYPE_FIELD`
   block using the fully qualified managed type name as the descriptor
   name:

   ```cpp
   CDAC_TYPE_BEGIN(System.Threading.Lock)
   CDAC_TYPE_FIELD(System.Threading.Lock, /*fieldtype*/, _state,
                   cdac_data<System.Threading.Lock>::_state)
   CDAC_TYPE_END(System.Threading.Lock)
   ```

2. The IData class already has the managed name in its `[CdacType]`
   names list. The cascade will find the native descriptor first and
   read from it; the managed fallback remains available if the native
   descriptor is absent (e.g. older runtimes).

**Statics:** Static fields are supported as native globals using the
naming scheme `TypeName.fieldName`. For example, a managed static
`s_instance` on type `System.Foo.Bar` becomes a native global named
`System.Foo.Bar.s_instance`. The `[StaticAddress]` and
`[StaticReference]` attributes try native globals first, then fall back
to `ManagedTypeSource`.

**Thread statics:** Thread-static fields are supported via the
`[ThreadStaticAddress]` attribute. The generated method takes a
`TargetPointer thread` parameter and resolves the field through
`ManagedTypeSource.TryGetThreadStaticFieldAddress`. Native global
thread statics are not currently supported.

### Migrating native types to managed

> **TODO:** A type forwarding system for native-to-managed migration is
> planned but not yet implemented. This section will be updated when the
> forwarding mechanism is available.

