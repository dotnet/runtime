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

The contract decodes method signatures through the
[TypeInformation](./TypeInformation.md) contract. Each parameter therefore
has both:

- signature facts used for GC classification, including `Class`, `Byref`,
  array, pointer, and generic shape
- an optional exact loaded `ITypeHandle` used for value-type layout and ABI
  classification

An unresolved TypeDef or TypeRef encoded as `ELEMENT_TYPE_CLASS` is still
reported as a GC reference. A generic instantiation whose exact constructed
type is unavailable retains its generic definition and type arguments.

CallingConvention does not infer value-type ABI layout from an open generic
definition. If an argument or return value requires value-type size, alignment,
HFA, GCDesc, or platform ABI classification and no exact loaded type is
available, `TryComputeArgGCRefMapBlob` returns `false`.

For byref-like value types, field traversal uses `TypeInformation.GetFieldTypeInfo`
with the current generic type context. The exact type is used when available;
otherwise the generic definition supplies the instantiation-independent field
shape while recursively decoded type arguments preserve nested generic context.

Contracts used:

| Contract Name |
| --- |
| RuntimeInfo |
| RuntimeTypeSystem |
| TypeInformation |
