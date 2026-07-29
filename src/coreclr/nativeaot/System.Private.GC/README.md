# System.Private.GC

This library is the in-progress C# port of the garbage collector that NativeAOT currently
compiles from C++ (`src/coreclr/gc`). The goal is a GC that is compiled by ILC alongside the
rest of the runtime, so that no C++ toolchain is required to build or modify it.

The port proceeds bottom-up: leaf modules with no dependency on the GC/EE interface or on the
`gcpriv.h` data structures are ported first, then the environment layer, then the heap itself.
Each source file here corresponds to one or more files in `src/coreclr/gc`; the header comment
of every file records which ones.

## Rules for code in this library

The GC runs while the world is suspended and while the heap is in an inconsistent state, so
managed code here is severely restricted. Code in this library must:

* Never allocate managed memory, and never hold or produce a GC reference. All heap addresses
  are `byte*`/`nuint`, never `object`. This keeps the ported code compilable without a GC
  underneath it.
* Use `unsafe` pointer code that mirrors the C++ pointer arithmetic one-for-one. Fidelity to
  the original is more important than idiomatic C#: a mechanical translation can be diffed
  against the C++ when the C++ changes.
* Avoid anything that requires runtime services that are unavailable during a collection:
  exceptions, type loading, virtual dispatch through managed interfaces, static constructors
  (use explicitly initialized statics), `string`, LINQ, and generics over reference types.
* Keep the C++ names (including `snake_case` where the C++ uses it) when porting a type whose
  layout or naming is load-bearing, so the correspondence stays reviewable. New helper APIs
  follow normal .NET naming.

## Status

Ported so far:

| C# file | Ported from |
| --- | --- |
| `GCEventEnums.cs` | `gcinterface.h` (event level/keyword/provider enums) |
| `GCEventStatus.cs` | `gceventstatus.h`, `gceventstatus.cpp` |
| `IntroSort.cs` | `introsort.h` |

Nothing here is wired into the runtime build yet.
