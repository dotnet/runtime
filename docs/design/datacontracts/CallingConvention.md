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
