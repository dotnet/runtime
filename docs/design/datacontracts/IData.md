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

There are two ways to write an `IData<T>` implementation:

1. **Source-generated** -- recommended for the common case. The user
   declares only the C# property surface, decorates it with cdac
   attributes, and the source generator emits the constructor,
   `IData<T>.Create`, optional `TypeHandle`, optional `Write{Name}`
   write-back methods, and the `Address` property.
2. **Hand-written** -- used when the class needs logic that doesn't fit
   the declarative attribute surface (variable-count loops, raw byte
   buffers from descriptor-driven sizes, multiple `Target.TypeInfo`
   lookups in one constructor). A class can also be partially generated
   + partially hand-written by combining `[CdacType]` attributes with the
   `partial void OnInit(Target, TargetPointer)` escape hatch.

This document describes the source-generated path. Hand-written classes
follow the same `IData<T>` contract but provide all the read/write logic
themselves.

## The source generator at a glance

`Microsoft.Diagnostics.DataContractReader.SourceGenerator` is a Roslyn
`IIncrementalGenerator` wired into
`Microsoft.Diagnostics.DataContractReader.Contracts` as a build-time
analyzer. It scans for classes carrying `[CdacType]` and emits a
`partial` companion containing:

* A `public TargetPointer Address { get; }` property (always emitted --
  the instance remembers the address it was constructed from).
* A `public {Name}(Target target, TargetPointer address)` constructor
  that does the descriptor lookup and per-field reads.
* A `static {Name} IData<{Name}>.Create(...) => new {Name}(target, address);`
  one-liner.
* For managed types: a `private const string FullyQualifiedName = "..."`
  and `public static TypeHandle TypeHandle(Target target)` accessor.
* For each `[Field(Writable = true)]` property on a class using a native
  descriptor: a `public void Write{Name}(Target target, T value)` method.
* For each `[StaticAddress]` / `[StaticReference]` /
  `[ThreadStaticAddress]` partial method declaration in the user
  source: a corresponding implementation that routes through
  `ManagedTypeSource`.
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
  `{ get; private set; }`. Don't use `init`, `required`, or `= null!;`.
* If the user needs to populate properties from custom logic, add
  `[MemberNotNull(nameof(X), ...)] partial void OnInit(...)` -- the
  generator-emitted constructor calls `OnInit` at the end, and the
  `MemberNotNull` annotation flows through so the compiler stops
  complaining about uninitialized non-nullable members.

## Attribute surface

All attributes live in
`Microsoft.Diagnostics.DataContractReader.Generated`.

### Class-level: `[CdacType]`

Marks a class for source generation. Choose **one** descriptor source:

| Constructor / property | Meaning |
|---|---|
| `[CdacType(DataType.X)]` | Native cdac descriptor by enum value. |
| `[CdacType("DescriptorName")]` | Native cdac descriptor by name (rare; for types not in the `DataType` enum). |
| `[CdacType]` (parameterless) | No descriptor lookup at all. Use with `[FieldOffset]` properties or `OnInit` only. |
| `[CdacType(ManagedFullName = "System.X.Y")]` | Pure managed type; layout from `ManagedTypeSource`. Generator emits a `TypeHandle` accessor and applies the standard `address + Object.Size` offset to access instance fields. |
| `[CdacType(DataType.X, ManagedFullName = "System.X.Y")]` | Hybrid: field offsets come from the *native* descriptor (so they're anchored at the object pointer, not after the header), but the class also exposes a `TypeHandle` accessor. Used by types like `Exception` that have both representations. |
| `IsValueType = true` | (Managed only.) The class wraps an inline value type with no object header. The generator reads fields starting at `address` rather than `address + Object.Size`. |

### Property-level: `[Field]`

Reads a descriptor field. The read kind is inferred from the property
type:

| Property type | Generated read |
|---|---|
| `uint` / `int` / `byte` / `ushort` / ... | `target.ReadField<T>(address, type, "name")` |
| `bool` | `target.ReadField<byte>(address, type, "name") != 0` |
| `TargetPointer` | `target.ReadPointerField(address, type, "name")` |
| `TargetNUInt` | `target.ReadNUIntField(address, type, "name")` |
| `TargetCodePointer` | `target.ReadCodePointerField(address, type, "name")` |
| `T` where `T : IData<T>` (in-place struct) | `target.ReadDataField<T>(address, type, "name")` (i.e. `ProcessedData.GetOrAdd<T>(address + offset)`) |
| `T?` (Nullable<T> on a value type) | wrapped in a `type.Fields.ContainsKey(...)` guard; missing descriptor field yields `default(T?)` (i.e. `null`) |

Parameters:

* `[Field("descriptor_name")]` -- map to a descriptor field whose name
  differs from the C# property name (common for managed types whose
  field names start with `_`, e.g. `[Field("_state")] public uint State`).
* `[Field(Pointer = true)]` -- for `IData<T>`-typed properties: read a
  pointer from the descriptor field, then materialize the pointee via
  `target.ProcessedData.GetOrAdd<T>(pointer)`. Use only when the
  pointer is guaranteed non-null; otherwise expose the property as a
  raw `TargetPointer` and let the consumer materialize on demand.
* `[Field(Writable = true)]` -- emit a
  `public void Write{Name}(Target target, T value)` method that writes
  the value back to the target's memory and updates the in-memory
  snapshot. The property must have a setter (`set` or `private set`),
  the read kind must be `Primitive` or `Bool`, and the class must use a
  native descriptor.

### Property-level: `[FieldOffset]`

Bypasses the descriptor. Reads from a hardcoded byte offset relative
to `address`. The read kind is inferred from the property type the same
way `[Field]` infers it.

| Form | Generated |
|---|---|
| `[FieldOffset(12)] public uint X { get; }` | `X = target.Read<uint>(address + 12);` |
| `[FieldOffset(60, LittleEndian = true)] public int Lfanew { get; }` | `Lfanew = target.ReadLittleEndian<int>(address + 60);` |
| `[FieldOffset(4)] public ImageFileHeader Hdr { get; }` | `Hdr = target.ProcessedData.GetOrAdd<ImageFileHeader>(address + 4);` |

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

These attributes target `static partial` method declarations on a
class with `ManagedFullName` set. The generator emits the
implementation, routed through `ManagedTypeSource`.

| Attribute | Generated method body |
|---|---|
| `[StaticAddress("field")]` | `target.Contracts.ManagedTypeSource.GetStaticFieldAddress(FullyQualifiedName, "field")` |
| `[StaticReference("field")]` | `target.Contracts.ManagedTypeSource.TryGetStaticFieldAddress(...) ? target.ReadPointer(addr) : TargetPointer.Null` -- for managed object references; returns `Null` if the static slot isn't allocated. |
| `[ThreadStaticAddress("field")]` | `target.Contracts.ManagedTypeSource.GetThreadStaticFieldAddress(FullyQualifiedName, "field", thread)` -- the method must also take a `TargetPointer thread` parameter. |

Example:

```csharp
[CdacType(ManagedFullName = "System.Runtime.InteropServices.ComWrappers")]
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
[CdacType(DataType.Bucket)]
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
[CdacType(DataType.Module)]
internal sealed partial class Module : IData<Module>
{
    [Field(Writable = true)] public uint Flags { get; private set; }
    // ...
}

// Generated:
public void WriteFlags(Target target, uint value)
{
    Target.TypeInfo type = target.GetTypeInfo(DataType.Module);
    target.WriteField<uint>(Address, type, "Flags", value);
    Flags = value;
}
```

Rules:

* Property type must be a primitive integer or `bool`.
* Property must have an explicit setter (`set` or `private set`, not
  `init`).
* Class must use a native descriptor (`DataType.X` or `("Name")`);
  the `ManagedTypeSource` is read-only.
* Plain `[FieldOffset]` is not writable (no descriptor entry to update).

## Worked examples

### Pure native descriptor read

```csharp
[CdacType(DataType.MethodTable)]
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
[CdacType(DataType.Object)]
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
[CdacType(ManagedFullName = "System.Threading.Lock")]
internal sealed partial class Lock : IData<Lock>
{
    [Field("_state")]          public uint State { get; }
    [Field("_owningThreadId")] public int OwningThreadId { get; }
    [Field("_recursionCount")] public uint RecursionCount { get; }
}
```

### Hybrid native + managed

```csharp
[CdacType(DataType.Exception, ManagedFullName = "System.Exception")]
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
[CdacType(ManagedFullName = "...+Entry", IsValueType = true)]
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
    [FieldOffset(60, LittleEndian = true)]
    public int Lfanew { get; }
}
```

### Statics-only managed type

```csharp
[CdacType(ManagedFullName = "System.Runtime.InteropServices.ComWrappers")]
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
[CdacType(DataType.Module)]
internal sealed partial class Module : IData<Module>
{
    [Field(Writable = true)] public uint Flags { get; private set; }
    [Field] public TargetPointer Assembly { get; }
    // ...
}

// usage
Data.Module module = target.ProcessedData.GetOrAdd<Data.Module>(addr);
module.WriteFlags(target, newFlags);
```

### Hand-written extension via `OnInit`

```csharp
[CdacType(DataType.RangeSectionFragment)]
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

## Good practices

The IData abstraction works best when classes are kept as **dumb data
views** -- one C# property per descriptor field, no derived logic, no
expensive work hidden behind the constructor. Following these rules
keeps the per-`Create` cost predictable, the descriptor surface
visible at the property level, and the consumer in control of what
gets materialized.

### Don't put algorithm logic in IData classes

The constructor (and `OnInit`) should be limited to reads from the
target -- enough to populate the declared properties. Derived
computations, interpretation, and any contract algorithms belong in
the consuming contract implementation (`Contracts\*.cs`), not in the
IData class. Bad:

```csharp
[CdacType(DataType.Thread)]
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
[CdacType(DataType.Thread)]
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

### Don't eagerly dereference pointers to other IData classes

When a field is a `TargetPointer` that points to another IData
type, expose it as a plain `TargetPointer` and let the consumer
materialize on demand. Don't use `[Field(Pointer = true)]` to
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

Avoid:

```csharp
[CdacType(DataType.Thread)]
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
[CdacType(DataType.Thread)]
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
[CdacType(DataType.LoaderAllocator)]
internal sealed partial class LoaderAllocator : IData<LoaderAllocator>
{
    // OK: ObjectHandle is laid out inline inside LoaderAllocator;
    // ReadDataField<ObjectHandle> materializes the embedded struct.
    [Field] public ObjectHandle ObjectHandle { get; }
}
```

### Use `[Field(Pointer = true)]` sparingly

The only cases where eager dereference is appropriate:

* The pointee is **guaranteed non-null** at all times (e.g.
  `Object.MethodTable` -- every managed object has a non-null MT).
* The cost of always materializing it is acceptable for *every*
  reader of the parent type.

When in doubt, expose a `TargetPointer` and let the caller decide.

### One IData class, one descriptor

Don't read fields from a different `Target.TypeInfo` than the one the
class is anchored on. If a type's layout genuinely spans two
descriptors (e.g. `ParamTypeDesc` inheriting `TypeAndFlags` from the
base `TypeDesc` descriptor), use `OnInit` to do the cross-descriptor
read explicitly -- don't try to express it through `[CdacType]`
alone.

### Keep `OnInit` small

`OnInit` is an escape hatch, not a second constructor. If you find
yourself writing more than ~10 lines, consider whether the logic
belongs in the consumer contract instead.

## When not to use the source generator

Some classes' constructors are too unique to fit the generator surface
without significant gymnastics. Keep them hand-written:

* Classes whose properties are mutable through unrelated setter logic
  that doesn't map to a single `Write{Name}` (e.g. legacy types that
  expose `{ get; set; }` for testing-only assignment after the IData
  factory completes).
* Classes that need to call non-IData helpers (`Target.IDataCache`,
  custom proxy types) in ways that don't fit the
  read-then-OnInit pattern.

These should still implement `IData<T>.Create` themselves and provide
their own constructor.
