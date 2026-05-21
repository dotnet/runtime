# AMD64 Windows Managed Calling Convention

**Iterator:** [`AMD64WindowsArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/AMD64WindowsArgIterator.cs)
**Applies to:** Windows x64 (managed code on the Microsoft x64 ABI).
**Base ABI:** Microsoft x64 calling convention -- see
[x64 Software Conventions](https://learn.microsoft.com/cpp/build/x64-software-conventions).

## Register set

| Use | Registers |
|---|---|
| Integer arg | `RCX, RDX, R8, R9` (**4 slots, shared with FP slots by position**) |
| Float arg | `XMM0, XMM1, XMM2, XMM3` (**same 4 slot positions** -- each arg consumes either an int or an FP register at its slot index, not both) |
| Integer return | `RAX` |
| Float return | `XMM0` |
| Volatile | `RAX, RCX, RDX, R8-R11, XMM0-XMM5` |
| Non-volatile | `RBX, RBP, RDI, RSI, R12-R15, XMM6-XMM15` |
| Stack slot size | 8 bytes |
| Shadow space | 32 bytes immediately above the return address (caller reserves homes for the 4 register args) |
| Stack alignment | 16 B at call site |

## Argument placement rules

**Every argument consumes exactly one 8-byte slot** -- there is no splitting,
no multi-register passing, no eightbyte classification.

- The first 4 slots (positions 0-3) are passed in registers; the choice
  between `RCX..R9` and `XMM0..3` depends on the arg's type:
  - `R4` / `R8` (float, double) at position N -> `XMM<N>`.
  - Anything else at position N -> integer register `RCX/RDX/R8/R9` for N=0..3.
- Slot 4+ goes on the stack at `[RSP + 32 + (N - 4) * 8]` (above shadow space).
- A value type whose size is **not in {1, 2, 4, 8} bytes** (i.e. 3, 5, 6, 7,
  or >= 9) is passed by an **implicit hidden pointer** rather than by value.
  The caller materializes the struct (usually on its own stack) and passes a
  pointer.

In cDAC this is encoded as:

```csharp
EnregisteredParamTypeMaxSize = 8;
IsArgPassedByRefBySize(size) = size > 8 || !IsPow2(size);
```

## Return values

| Return shape | Where |
|---|---|
| Integer / pointer / reference / value type with size in {1, 2, 4, 8} | `RAX` |
| `R4` / `R8` | `XMM0` |
| Value type not in {1, 2, 4, 8} (incl. `TypedByRef` = 16 B), or non-power-of-2 size | Caller-allocated return buffer; pointer passed as first hidden arg; callee returns the buffer address in `RAX` |

## Managed-specific behavior

### `this` is always in `RCX`

Native C++ on Microsoft x64 pushes the ret buf into `RCX` and bumps `this` to
`RDX` when the function returns a large struct. **Managed code always uses
`RCX` for `this`** and `RDX` for the ret buf, regardless of whether a ret buf
is present. (This wasn't always the case -- up to .NET Framework 4.5 the
managed convention matched native; it changed for consistency with other
managed platforms.)

The cDAC inherits this from `ArgIteratorBase.GetRetBuffArgOffset`, which
returns `argumentRegistersOffset + (hasThis ? PointerSize : 0)`.

### Hidden argument prefix

The CLR's standard prefix order applies:

```
[this:RCX] [retBuf:RDX] [genericContext] [asyncContinuation] [varArgCookie] userArgs...
```

Each takes the next available register slot in `RCX..R9`, then spills to the
stack. So a method with `this` + ret buf + generic context + 1 user arg lays
out as `RCX=this, RDX=retBuf, R8=genericContext, R9=userArg0`.

The vararg cookie, async continuation, and generic context don't exist in
native; the cDAC counts them in `ArgIteratorBase.ComputeInitialNumRegistersUsed`.

### Varargs

Managed varargs (`IMAGE_CEE_CS_CALLCONV_VARARG`) follow the Microsoft
"duplicate-into-int-reg" rule for FP args: any `R4`/`R8` argument in the first
4 slots is duplicated into the matching `RCX..R9` slot as well as `XMM0..3`.
The cDAC iterator's per-arg logic is the same for fixed and variadic methods
-- the duplication is handled by the JIT, not represented in the iteration
output.

### Implicit by-reference: GC-tracked pointers

Unlike native, the implicit-byref pointer may legitimately point into the GC
heap (reflection/remoting paths), so the JIT:

- Reports the implicit-byref parameter as a GC `BYREF` (interior pointer).
- Uses checked write barriers for stores through the pointer.

### Empty structs go on the stack

A managed struct with **zero instance fields** is passed by value on the stack
(never in a register), regardless of its declared size. Native C++ has no
equivalent since `sizeof(EmptyStruct) >= 1`.

### Frame pointer

Unlike System V x64 (which always uses RBP since CoreCLR PR
[dotnet/coreclr#4019](https://github.com/dotnet/coreclr/pull/4019)) and ARM/ARM64
(which require a frame pointer), **Windows x64 typically omits the frame
pointer**. Unwinding uses PDATA/XDATA records, not frame chaining. The JIT
allocates RBP only when the function genuinely needs one (e.g., funclets,
`alloca`).

### Funclets

Catch / finally / filter handlers are emitted as separate functions
(*funclets*) with their own PDATA entries, looking to the OS like first-class
functions. The catch funclet receives the `System.Exception` reference in
`RCX`. This is a CLR construct; native SEH passes an `EXCEPTION_RECORD*`.

### Secret VM-to-JIT register conventions

Several "secret" registers carry runtime data for special call shapes:

| Use | Register |
|---|---|
| Virtual stub dispatch (VSD) | `R11` (stub indirection cell) |
| `calli` P/Invoke target | `R10` |
| `calli` P/Invoke signature cookie | `R11` |
| Normal P/Invoke MethodDesc param | `R10` |

`R10` and `R11` are volatile in the Microsoft x64 ABI and unused as argument
registers, which makes them safe choices.

### Small primitives are zero/sign-extended

Native Microsoft x64 leaves the upper bits of small return values
**undefined**. Managed code defines them: signed small types (`sbyte`,
`short`) are sign-extended to 32/64 bits; unsigned (`byte`, `ushort`, `bool`)
are zero-extended. The JIT relies on this when reading values back at call
sites.

## TypedReference

`TypedReference` is 16 bytes. Since 16 is not in {1, 2, 4, 8}, the
implicit-byref rule applies:

- **As a parameter**: passed by hidden pointer; the slot in `RCX..R9` (or on
  the stack) holds a pointer to a `TypedReference` value the caller has
  materialized.
- **As a return value**: triggers a return buffer.

## References

- [docs/design/coreclr/botr/clr-abi.md - Special parameters](../docs/design/coreclr/botr/clr-abi.md) -- `this`, generics, varargs, async continuation.
- [src/coreclr/vm/callingconvention.h](../src/coreclr/vm/callingconvention.h) -- search for `TARGET_AMD64` and `UNIX_AMD64_ABI` to find the Windows-vs-Unix split (Windows is the `#else` arm).
- [Microsoft x64 calling convention](https://learn.microsoft.com/cpp/build/x64-calling-convention) -- base ABI documentation.
