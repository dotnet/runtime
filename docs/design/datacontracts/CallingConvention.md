# Contract CallingConvention

This contract walks a method's argument signature using the runtime's
calling-convention rules so consumers can locate each argument on the
caller's transition frame and reason about which slots hold GC references.

The actual ABI (which registers hold which arguments, what alignment and
padding rules apply, how structs are promoted to registers vs spilled, how
varargs are passed, etc.) is documented in the CLR ABI specs and is not
re-described here:

- [Common CLR ABI conventions](../coreclr/botr/clr-abi.md)

This contract's responsibility is to surface the *result* of that walk in
a form the cDAC can use, byte-for-byte compatible with what the runtime
itself produces.

## APIs of contract

``` csharp
// Encode the argument GCRefMap blob for `methodDesc` byte-for-byte
// compatible with the runtime's ComputeCallRefMap (frames.cpp).
// Returns false when this contract declines to encode the method
// (e.g. an unported ABI path); callers should map false to E_NOTIMPL.
// When false, the value of `blob` is unspecified.
bool TryComputeArgGCRefMapBlob(MethodDescHandle methodDesc, out byte[] blob);
```

## Version 1
<!-- BEGIN GENERATED: usage contract=CallingConvention version=c1 -->
### Data descriptors used

_None._

### Global variables used

_None._

### Contracts used

| Contract Name |
| --- |
| `EcmaMetadata` |
| `Loader` |
| `RuntimeInfo` |
| `RuntimeTypeSystem` |
<!-- END GENERATED: usage contract=CallingConvention version=c1 -->


The single API is implemented by walking the shared `ArgIterator`
(`src/coreclr/tools/Common/CallingConvention/ArgIterator.cs`) and feeding
the per-argument result into a GCRefMap encoder that mirrors
`GCRefMapBuilder` (`src/coreclr/inc/gcrefmap.h`).

`TryComputeArgGCRefMapBlob` returns `false` for any method whose
signature, ABI path, or generic context the encoder hasn't been taught
yet. The cdacstress harness (`src/coreclr/vm/cdacstress.cpp`,
`ARGITER` sub-check) uses byte-for-byte comparison of the returned blob
against the runtime's `ComputeCallRefMap` output as its correctness
oracle.

## Signature decoding

The contract first obtains the owning method table and module for `methodDesc`.
It reads the method signature through `RuntimeTypeSystem.TryGetMethodSignature`
and obtains the module's `MetadataReader` through `EcmaMetadata`. Conceptually,
the signature is decoded using
`System.Reflection.Metadata.SignatureDecoder<SignatureTypeInfo, SignatureTypeContext>`.
The actual decoder also understands the runtime-internal signature element
types, but otherwise follows the SRM decoding model.

The type provider is initialized with:

- the module that owns the signature, so it can resolve `TypeDef` and `TypeRef`
  tokens with `Loader.GetModuleLookupMapElement` and the
  `TypeDefToMethodTable` and `TypeRefToMethodTable` lookup-map kinds;
- the method's `MethodDescHandle`, for method generic parameters; and
- the structural type information of the owning type, for type generic
  parameters.

For a `TypeDef` or `TypeRef`, the lookup map may not yet contain a target
type handle. The provider must still retain whether the signature encoded
`ELEMENT_TYPE_CLASS` or `ELEMENT_TYPE_VALUETYPE`. This distinction is enough
to classify a reference argument without forcing the type to load. A value
type whose exact type handle is unavailable has indeterminate layout and
cannot be passed to ABI paths that need its size or field layout.

## Signature type information

Each decoded type is represented as:

| Information | Source | Purpose |
| --- | --- | --- |
| Outer element type | Signature element type | Classifies primitives, references, pointers, byrefs, and value types even when a target type is not fully loaded. |
| Exact type handle, when available | Module lookup map, generic instantiation, or runtime-internal signature element | Provides target-backed runtime layout and classification. |
| Generic type definition, when available | Decoded generic type | Preserves the definition when an exact constructed type handle is unavailable. |
| Generic arguments | Recursive signature decoding | Supplies the structural generic context used when decoding fields of nested value types. |

An exact `ITypeHandle` represents a target-backed `MethodTable` or `TypeDesc`.
It is intentionally optional here: a signature can describe a type before the
runtime has produced an exact handle for it.

When the walk needs a field type, it obtains the field's metadata signature
from the enclosing type's module and decodes it with the structural information
of the containing type as the generic context. This substitutes generic
parameters in nested fields even when no exact constructed method table exists.

## Type information required by ArgIterator

`ArgIterator` consumes a small type abstraction to apply the target ABI. The
CallingConvention contract adapts the decoded type information and target
contracts to the following operations:

| ArgIterator operation | Source | Behavior when exact layout is unavailable |
| --- | --- | --- |
| `IsNull` | Absence of both a signature element type and exact type handle | Reports no type. |
| `GetCorElementType` | Signature element type; exact handle only to normalize an enum value type to its underlying primitive | Uses the structural element type whenever possible. |
| `IsValueType` and `IsPointerType` | Signature element type; exact handle as a fallback | Uses the structural classification. |
| `PointerSize` | `Target.PointerSize` | Always available. |
| `GetSize` and `HasIndeterminateSize` | `RuntimeTypeSystem.GetBaseSize` for an exact value type | An unresolved value type has indeterminate size; size-dependent ABI paths are declined. |
| `RequiresAlign8` | `RuntimeTypeSystem.RequiresAlign8` | Returns false without an exact handle. |
| `IsHomogeneousAggregate` and `GetHomogeneousAggregateElementSize` | `RuntimeTypeSystem.TryGetHFAElementSize` | Returns false without an exact handle. |
| `GetSystemVAmd64PassStructInRegisterDescriptor` | `RuntimeTypeSystem.TryGetSystemVAmd64EightByteClassification` | Reports that the struct is not register-classified without an exact handle. |
| `IsTrivialPointerSizedStruct` | Exact value-type size plus its instance `FieldDesc` list and field signatures | Returns false unless the exact layout proves the x86 special case. |
| `GetFpStructInRegistersInfo` | Target-specific RISC-V and LoongArch64 ABI classification | Not yet implemented; the contract declines that ABI path. |
| `GetFieldAlignment` | Target-specific LoongArch64 and WASM layout | Not yet implemented; the contract declines that ABI path. |

After constructing this adapter for each parameter and return type, the
contract initializes `ArgIterator` with the target `TransitionBlock`, method
calling convention, instance/varargs state, and generic-context argument
state. It walks the resulting argument offsets, then encodes the locations and
GC classifications into the GCRefMap blob.
