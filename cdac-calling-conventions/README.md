# Managed Calling Conventions for cDAC Stack Walking

This folder documents the calling conventions used by managed code on each
supported platform, as implemented by the per-architecture `ArgIterator`
subclasses in
[`src/native/managed/cdac/.../StackWalk/CallingConvention/`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/).

The cDAC's argument iterator is the managed reimplementation of the legacy DAC's
sig-walking layer. For each platform it answers the question: **"given a method
signature, where (register or stack offset) is each argument located when the
method is invoked?"** This is consumed by diagnostic tools (stack walks, locals
inspection, SOS, ClrMD, etc.) to inspect live frames in a target process.

Each doc focuses on **what the managed CLR does**, with deltas vs. the native
platform ABI called out explicitly. They are not a substitute for the platform
ABI specs -- read those for the base rules, then read these for the managed
specials.

## Platform docs

| Platform | Doc | Iterator |
|---|---|---|
| x86 (Windows / Linux 32-bit) | [`x86.md`](./x86.md) | [`X86ArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/X86ArgIterator.cs) |
| AMD64 Windows | [`amd64-windows.md`](./amd64-windows.md) | [`AMD64WindowsArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/AMD64WindowsArgIterator.cs) |
| AMD64 Unix (Linux / macOS) | [`amd64-unix.md`](./amd64-unix.md) | [`AMD64UnixArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/AMD64UnixArgIterator.cs) |
| ARM32 (AAPCS, Linux armhf / Windows ARM) | [`arm32.md`](./arm32.md) | [`Arm32ArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/Arm32ArgIterator.cs) |
| ARM64 (AAPCS64, Linux / Windows / Apple) | [`arm64.md`](./arm64.md) | [`Arm64ArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/Arm64ArgIterator.cs) |
| RISC-V 64 / LoongArch 64 | [`riscv64-loongarch64.md`](./riscv64-loongarch64.md) | [`RiscV64LoongArch64ArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/RiscV64LoongArch64ArgIterator.cs) |

## Cross-cutting concepts

These apply to all platforms and are not repeated in each doc; consult the
upstream design doc [`docs/design/coreclr/botr/clr-abi.md`](../docs/design/coreclr/botr/clr-abi.md)
for full detail.

### Argument prefix order

Every managed method starts its argument list with zero or more **hidden
arguments**, in this fixed order, before any user arguments:

```
[this] [retBuf] [genericContext] [asyncContinuation] [varArgCookie] userArgs...
```

- **`this`**: Instance methods only. Always passed first (managed-specific --
  native C++ x64 reorders it after the ret buf on some platforms).
- **`retBuf`**: Hidden pointer for methods returning a value type that doesn't
  fit in the return registers (rules vary per platform). Callee writes the
  result through this pointer; on AMD64 the callee also returns the buffer
  address in the integer return register.
- **`genericContext`**: For *shared generic* methods, a `MethodDesc*` (generic
  methods) or `MethodTable*` (static methods on generic types) telling the
  callee which instantiation it's serving.
- **`asyncContinuation`**: For methods participating in the async stack
  protocol (new in the runtime-async work).
- **`varArgCookie`**: For managed varargs (`__arglist` /
  `IMAGE_CEE_CS_CALLCONV_VARARG`), a pointer to a runtime-parseable signature
  blob describing the variadic tail.

The cDAC base class counts these in `ArgIteratorBase.ComputeInitialNumRegistersUsed`
(see [`ArgIteratorBase.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/ArgIteratorBase.cs))
before user-arg iteration begins. x86 is an outlier -- it counts these
separately in its own `ComputeSizeOfArgStack` pass.

### TypedReference

`System.TypedReference` is `{ ref byte _value; IntPtr _type; }` = 16 bytes,
referenced in signatures by `ELEMENT_TYPE_TYPEDBYREF` (0x16) with no class
token. The runtime keeps a `g_TypedReferenceMT` global pointing at the
TypedReference MethodTable; the signature walker substitutes that MT whenever
it encounters `ELEMENT_TYPE_TYPEDBYREF`, then the iterator treats it as an
ordinary 16-byte value type.

In cDAC, the substitution lives in `ArgTypeInfoSignatureProvider.GetTypedReferenceInfo()`.
Each platform doc summarizes where a `TypedReference` parameter and return
value land.

### Implicit by-reference

On most platforms, value types whose size exceeds a per-platform threshold are
passed via a hidden pointer (the *implicit byref*) instead of by value. The
JIT must:

- Report the implicit-byref parameter as an interior pointer (GC `BYREF`) in
  the GC info, because the caller may legitimately point at the GC heap (not
  always a stack temp).
- Use checked write barriers for any stores through the pointer.

The per-platform threshold is encoded as `EnregisteredParamTypeMaxSize` on each
iterator (see the abstract property on `ArgIteratorBase`).

### Funclets, frame pointers, and other non-arg-iterator concerns

The exception-handling funclet model, frame-pointer policy, GC-info layout,
profiler hooks, and other CLR-internal contracts are documented in
[`docs/design/coreclr/botr/clr-abi.md`](../docs/design/coreclr/botr/clr-abi.md).
These don't affect argument iteration directly; they affect codegen and
stack walking elsewhere.

## Reference

- [`docs/design/coreclr/botr/clr-abi.md`](../docs/design/coreclr/botr/clr-abi.md) -- the
  authoritative CLR ABI design document.
- [`src/coreclr/vm/callingconvention.h`](../src/coreclr/vm/callingconvention.h) --
  the native VM's `ArgIteratorTemplate`, which each cDAC iterator mirrors.
- [`src/coreclr/tools/aot/ILCompiler.ReadyToRun/.../ArgIterator.cs`](../src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/DependencyAnalysis/ReadyToRun/ArgIterator.cs) --
  CrossGen2's managed port of the same logic.
