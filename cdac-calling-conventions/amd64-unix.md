# AMD64 Unix (System V) Managed Calling Convention

**Iterator:** [`AMD64UnixArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/AMD64UnixArgIterator.cs)
**Classifier:** [`SystemVStructClassifier.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/SystemVStructClassifier.cs)
**Applies to:** Linux x64, macOS x64 (managed code on the System V AMD64 ABI).
**Base ABI:** System V AMD64 ABI -- see
[the spec](https://gitlab.com/x86-psABIs/x86-64-ABI) (§3.2.1 registers,
§3.2.3 parameter passing / classification, §3.3 vector types).

## Register set

| Use | Registers |
|---|---|
| Integer arg | `RDI, RSI, RDX, RCX, R8, R9` (6 slots) |
| Float arg | `XMM0`-`XMM7` (8 slots) |
| **Bank independence** | The integer and FP banks are tracked **independently** -- a float arg does **not** consume an int slot (unlike Windows x64) |
| Integer return | `RAX` (and `RDX` for the second eightbyte) |
| Float return | `XMM0` (and `XMM1` for the second eightbyte) |
| Volatile | `RAX, RCX, RDX, RSI, RDI, R8-R11, XMM0-XMM15` |
| Non-volatile | `RBX, RBP, R12-R15` |
| Stack slot size | 8 bytes |
| Stack alignment | 16 B at call site |
| Red zone | 128 bytes below `RSP` may be used by leaf functions without explicit allocation |

## Argument placement rules: the eightbyte classifier

For value types up to 16 bytes, the System V ABI defines a per-byte
**eightbyte classification** algorithm:

1. Aggregates > 16 bytes -> passed in memory (on the stack, **by value -- no
   hidden pointer**, unlike Windows x64).
2. Aggregates with a misaligned field -> in memory.
3. Otherwise the struct is partitioned into 1 or 2 eightbytes (bytes 0-7,
   optionally 8-15), and each eightbyte gets a class:
   - `INTEGER` (incl. CLR's `IntegerReference` for object refs and `IntegerByRef`
     for managed pointers)
   - `SSE` (float / double)
   - `NO_CLASS` (padding / empty slot -- in the CLR currently promoted to
     `INTEGER`; see TODO in `SystemVStructClassifier`)
   - `MEMORY` (forces the whole struct to memory)
4. Merge rules (when two fields share a byte / eightbyte):
   - Either side `INTEGER` -> `INTEGER` (INTEGER dominates SSE).
   - Both SSE -> `SSE`.
   - Either side `MEMORY` -> `MEMORY`.
5. Register assignment per eightbyte:
   - `INTEGER`/`IntegerReference`/`IntegerByRef` -> next free `RDI..R9` slot.
   - `SSE` -> next free `XMM0..XMM7` slot.
6. **All-or-nothing**: if even one eightbyte can't find its required register
   (bank exhausted), the *entire struct* spills to the stack and no registers
   are consumed by it.

Examples:

| Struct | Eightbytes | Classes | Placement |
|---|---|---|---|
| `{ int x; int y; }` (8 B) | 1 | `[INTEGER]` | 1 GP reg (`RDI`) |
| `{ int x; double d; }` (16 B) | 2 | `[INTEGER, SSE]` | 1 GP + 1 FP (`RDI, XMM0`) |
| `{ double a; double b; }` (16 B) | 2 | `[SSE, SSE]` | 2 FP (`XMM0, XMM1`) |
| `{ float a; float b; }` (8 B) | 1 | `[SSE]` (floats packed in low 64 bits of one XMM) | 1 FP (`XMM0`) |
| `{ int x; float f; }` (8 B, both in eightbyte 0) | 1 | `[INTEGER]` (INTEGER dominates) | 1 GP (`RDI`) |
| `{ long a; long b; long c; }` (24 B) | -- | `MEMORY` | Stack by value |

See [the SysV struct passing research doc](../C:/Users/maxcharlamb/.copilot/session-state/52879186-8fcc-4ed2-9048-3fb6ef3bf6b3/research/can-you-explain-the-sysv-struct-passing-convetion-.md)
for a full deep-dive with code citations.

## Return values

| Return shape | Where |
|---|---|
| Integer / pointer / reference | `RAX` |
| `R4` / `R8` | `XMM0` |
| Value type <= 16 B that classifies in registers | Same banks as parameters, in order: eightbyte 0 -> `RAX`/`XMM0`, eightbyte 1 -> `RDX`/`XMM1` (with appropriate fallback if the two eightbytes use different banks) |
| Value type > 16 B or classified `MEMORY` | Caller-allocated return buffer; pointer passed as first hidden arg (`RDI`); callee returns the buffer address in `RAX` |

## Managed-specific behavior

### `this` is in `RDI`

The first user-arg register is `RDI`, so `this` (always the first managed
arg) lands there. If there's a ret buf, ret buf -> `RDI` and `this` -> `RSI`.

### Hidden argument prefix

Standard CLR prefix applies. Each hidden arg consumes the next integer slot
(`RDI`, then `RSI`, ...):

```
[this:RDI] [retBuf:RSI] [genericContext:RDX] [asyncContinuation:RCX] [varArgCookie:R8] userArgs...
```

### Implicit by-reference is NOT used

Unlike Windows x64, the System V ABI passes large structs **by value on the
stack**, not via a hidden pointer. The cDAC encodes this as:

```csharp
public override bool IsArgPassedByRefBySize(int size) => false;
protected override bool IsArgPassedByRefArchSpecific() => false;
```

This means no GC-byref tracking is needed for value-type parameters on SysV.

### CLR uses a subset of the ABI's classes

The spec defines `NO_CLASS, INTEGER, SSE, SSEUP, X87, X87UP, COMPLEX_X87, MEMORY`.
The CLR uses only `NO_CLASS, INTEGER, SSE, MEMORY` plus two extensions
(`IntegerReference`, `IntegerByRef`) that carry GC liveness info. `SSEUP`,
`X87`, `X87UP`, `COMPLEX_X87` are not used:

- `Vector128/256/512<T>` and `Vector64<T>` bypass the SysV classifier entirely
  -- they are handled by the JIT as SIMD intrinsic types in single
  XMM/YMM/ZMM registers.
- The CLR has no `long double` / `__float128` / x87 types.

See the enum in [`SystemVStructDescriptor.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/SystemVStructDescriptor.cs).

### Empty structs

Managed structs with zero instance fields are passed by value on the stack
(matching the broader "explicit-layout / unusual struct -> stack" rule called
out in [`clr-abi.md:569`](../docs/design/coreclr/botr/clr-abi.md)).

### Frame pointer

System V x64 managed frames **always allocate a frame pointer** (RBP), since
CoreCLR PR [dotnet/coreclr#4019](https://github.com/dotnet/coreclr/pull/4019).
This makes stack walking via frame chains viable, unlike Windows x64 where
unwinding goes through PDATA/XDATA.

### Funclets

Same managed-EH model as other platforms. The catch funclet receives the
exception object in `RSI` (vs. `RCX` on Windows x64).

### Known TODO: NoClass eightbytes

The classifier currently promotes `NoClass` eightbytes (pure padding slots)
to `Integer` because the JIT mishandles `NoClass` (see TODO at
`SystemVStructClassifier.cs:439` and the mirrored TODO at
`src/coreclr/vm/methodtable.cpp:2660`). This is a known divergence from the
ABI spec; a small minority of structs may waste a GP register on a padding
slot as a result.

## TypedReference

`TypedReference = { ref byte _value; IntPtr _type; }` = 16 bytes.
Classification: `[IntegerByRef, Integer]` -> passed in **2 GP registers**
(typically `RDI, RSI`). Returned in `RAX, RDX`.

The cDAC's `ArgTypeInfoSignatureProvider` substitutes the `g_TypedReferenceMT`
MethodTable when the signature contains `ELEMENT_TYPE_TYPEDBYREF`, so the
classifier walks its layout as if it were an ordinary 16-byte value type.

## References

- [System V AMD64 ABI spec](https://gitlab.com/x86-psABIs/x86-64-ABI) -- §3.2.3 classification & passing.
- [docs/design/coreclr/botr/clr-abi.md - "System V x86_64 support"](../docs/design/coreclr/botr/clr-abi.md) -- CLR deviations.
- [docs/design/coreclr/jit/struct-abi.md](../docs/design/coreclr/jit/struct-abi.md) -- struct-passing design notes.
- [src/coreclr/vm/callingconvention.h](../src/coreclr/vm/callingconvention.h) -- `UNIX_AMD64_ABI` branches.
- [src/coreclr/vm/methodtable.cpp](../src/coreclr/vm/methodtable.cpp) -- `ClassifyEightBytesWithManagedLayout` (the C++ classifier).
- [src/coreclr/tools/Common/JitInterface/SystemVStructClassificator.cs](../src/coreclr/tools/Common/JitInterface/SystemVStructClassificator.cs) -- CrossGen2's managed mirror.
