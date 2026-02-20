# ReadyToRun Preinitialized Statics

Author: Enze He ([@hez2010](https://github.com/hez2010)) - 2026

## Instruction

Preinitialized statics feature was added in R2R format version **18.2**.

ReadyToRun now can interpret eligible `.cctor` bodies at compile time, serialize the resulting static state into the R2R image, and mark types as preinitialized so runtime class-init can be skipped.

The preinitialization interpreter is shared with the NativeAOT ILCompiler, so class constructors that can be preinitialized for NativeAOT are also supported in R2R, with some additional constraints. There're various limitations on what can be preinitialized, and the supported scenarios are listed as follows.

| Scenario | Support |
| --- | --- |
| Non-GC statics | Supported |
| GC statics | Supported, including object-graph materialization from serialized templates |
| Generic instantiations | Concrete, non-canonical instantiations that can be statically resolved |
| Delegates | Supported for closed delegates |

A restriction for R2R is that the preinitializer cannot inline any methods that cross the version bubble.

## Enabling

In crossgen2, preinitialized statics can be controlled with:

- `--preinitstatics`: enable compile-time interpretation of eligible static constructors.
- `--nopreinitstatics`: disable preinitialized statics even when optimization would otherwise enable it.

When optimization is enabled, preinitialized statics is enabled by default unless explicitly disabled via `--nopreinitstatics`.

## Native format

A new optional section `TypePreinitializationMap` has been added to the R2R image:

```cpp
enum class ReadyToRunTypePreinitializationFlags : uint32_t
{
    None = 0x0,
    TypeIsPreinitialized = 0x1,
};

struct READYTORUN_TYPE_PREINITIALIZATION_MAP_ENTRY
{
    DWORD TypeDefRid;

    union
    {
        struct
        {
            DWORD Index;
            DWORD Count;
        } Instantiation;

        struct
        {
            DWORD Rva;
            DWORD Size;
        } NonGCData;
    };

    ReadyToRunTypePreinitializationFlags Flags;
};

struct READYTORUN_TYPE_PREINITIALIZATION_MAP_INSTANTIATION_ENTRY
{
    DWORD TypeSignatureOffset;
    DWORD TypeSignatureLength;

    DWORD NonGCDataRva;
    DWORD NonGCDataSize;
    ReadyToRunTypePreinitializationFlags Flags;
};
```

Section payload emitted by `TypePreinitializationMapNode`:

1. `uint32 TypeCount`
2. `TypeCount * READYTORUN_TYPE_PREINITIALIZATION_MAP_ENTRY`
3. `uint32 InstantiationEntryCount`
4. `InstantiationEntryCount * READYTORUN_TYPE_PREINITIALIZATION_MAP_INSTANTIATION_ENTRY` in TypeDef-order
5. Concatenated instantiation type-signature blob bytes

The section contains two tables: a TypeDef table and an instantiation table. For each TypeDef row, payload fields are interpreted as either `NonGCData.Rva/Size` (when the type is not a generic definition) or `Instantiation.Index/Count` (generic definition type). Generic definitions do not have their own statics storage.

TypeDef rows are sorted by `TypeDefRid`. Instantiation rows are sorted first by owner `TypeDefRid`, then by lexicographic signature bytes.

Runtime locates TypeDef rows by `TypeDefRid`, then uses `Instantiation.Index`/`Instantiation.Count` to linearly compare signatures in that range.

If a module has any preinitialized types, all TypeDef rows in the module are present in the map, even those that are not preinitialized. This allows the runtime to locate the map entry directly without searching by the rid.

## Preinitialized static payload format

Per-type static payload is emitted by `TypePreinitializedStaticsDataNode`.

Payload layout:

```text
[ Non-GC static bytes ][ padding to pointer alignment ][ GC static handle slots ]
```

The RVA and size of the non-GC static region is recorded in the map, and the GC static region immediately follows the non-GC region with padding to ensure pointer alignment.

Runtime derives GC payload size from metadata (`GetNumHandleRegularStatics() * sizeof(TADDR)`), not from the map.

For GC statics whose field type is a value type (boxed GC statics), the serialized payload stores a pointer to a serialized boxed object template.

## Fixup encoding format

### Fixup signature blob header

For each import signature (`SignatureBuilder.EmitFixup`):

1. Emit 1-byte `kind`.
2. If the target module is not the local context, set `kind |= READYTORUN_FIXUP_ModuleOverride (0x80)` and emit compressed `uint moduleIndex`.

At runtime, the resolver can determine the fixup kind and target module from the signature blob header before decoding the rest of the signature.

### Fixup payloads used in preinitialization

| Fixup kind | Payload | Purpose |
| --- | --- | --- |
| `READYTORUN_FIXUP_TypeHandle` | Encoded type signature | Method table / runtime type references in serialized templates |
| `READYTORUN_FIXUP_StringHandle` | Compressed string token RID | Preinitialized string references |
| `READYTORUN_FIXUP_MethodEntry` plus optimized forms | Method-signature encoding (`ReadyToRunMethodSigFlags`, token RID, optional instantiation) | Delegate targets and function-pointer-like values |

For method entry imports, compact optimized encodings are only used when unboxing/constrained metadata is not required.

### Import cell addressing and addend encoding

Serialized payload pointers can represent either a template-object address in R2R data or an import-cell address with an addend.

Runtime resolver (in `TryResolveReadyToRunImportCellAddress`):

1. Validate that the encoded address is inside an R2R import section.
2. Compute `importIndex` and `importDelta` from section `EntrySize`.
3. Ensure the cell is fixed up.
4. Return `resolved = importCellValue + importDelta`.

This allows payload fields to encode "base + offset" against import cells.

## Implementation details

### Record creation and dependency rooting

`ReadyToRunPreinitializationManager.GetTypeRecord(type)` computes and caches whether a type is preinitialized, the non-GC payload size, an optional statics payload node, and an optional failure reason. If a record has emitted static payload data, that payload node is rooted in the dependency graph.

This is best-effort: if serialization/validation of the preinitialized graph fails (for example, unsupported layout or invalid serialized shape), the type is downgraded to non-preinitialized and the failure reason is recorded for diagnostics/statistics.

### Generic instantiation coverage

Generic instantiations are supported on a best-effort basis. Only concrete, non-canonical, non-runtime-determined instantiations that are statically referenced from the code are recorded in the map and have preinitialization support.

To ensure the map covers needed generic instantiations, static-base helper paths trigger record materialization in `ReadyToRunSymbolNodeFactory`.

### Serialized object templates

Object graphs for GC statics are emitted as templates where the first pointer is the object type handle/method table fixup and subsequent bytes encode fields/elements. Reference fields are emitted via relocations to other serialized templates, string imports, or runtime-type imports. Pointer-like non-reference fields can carry encoded import pointers.

Delegate serialization in R2R mode supports both closed static and closed instance delegates. Open static delegates are rejected due to no available token, as the emitted IL stub is not an `EcmaMethod`; and open instance delegates are currently not implemented.

### Loader and map attachment

The runtime locates TypePreinitializationMap (124) section and loads it to `Module`.

`Module` exposes lookup helpers:

| Helper | Purpose |
| --- | --- |
| `IsReadyToRunTypePreinitialized` | Query preinitialized flag for a type |
| `TryGetReadyToRunPreinitializedNonGCStaticsData` | Resolve non-GC payload pointer/size |
| `TryGetReadyToRunPreinitializedGCStaticsData` | Resolve GC payload pointer/size |

These runtime lookups apply to ReadyToRun method tables owned by the same module and not shared canonical generic instantiations.

### Static allocation

`MethodTable::EnsureStaticDataAllocated` keeps existing allocation behavior and then conditionally applies preinitialized data:

1. Non-GC bytes are copied when available and size-compatible. Pointers within the non-GC region (including those nested in value types) are fixed up by resolving R2R import cells.
2. GC static handles are materialized from the preinitialized GC region when available and size-compatible.

Before applying data, runtime validates map lookups, expected payload sizes, and image-bounds safety checks.

### Class-init skipping

`MethodTable::IsInitedIfStaticDataAllocated` can now return true even for types that have a cctor when the map marks the type as preinitialized and both non-GC and GC payload sizes match runtime layout expectations.

This allows class-init checks to be skipped for eligible types. Compilation will also be skipped for preinitialized types, and call sites will directly reference the target addresses without cctor triggers.

### Materialization

The materialization is done in several passes:

1. Non-GC static bytes are copied and fixed up.
2. GC static objects are allocated and initialized from the preinitialized GC region.
3. Nested value-type fields are handled recursively.

A cache of materialized objects is used to ensure object identity is preserved for reference fields that point to the same template object.

When decoding encoded references in GC payloads, runtime currently accepts import-based references for strings and runtime type objects (`READYTORUN_FIXUP_StringHandle`, `READYTORUN_FIXUP_TypeHandle`, and `READYTORUN_FIXUP_TypeDictionary`), otherwise the payload is rejected as invalid.

### GC object allocation strategy

The preinitializer will reject cases where a GC static field contains a reference to non-frozen objects that cannot be serialized. As a result we can safely allocate GC static objects in the frozen object heap, and allow them to be accessed directly. But it's possible that a GC type may contain GC pointers which can fail `TryAllocateFrozenObject`, in which case we will fall back to regular heap allocation.

