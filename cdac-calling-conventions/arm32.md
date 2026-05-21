# ARM32 (AAPCS) Managed Calling Convention

**Iterator:** [`Arm32ArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/Arm32ArgIterator.cs)
**Applies to:** Linux armhf (hard-float), Windows on ARM (32-bit). Linux armel
(soft-float) is not yet implemented in cDAC -- see TODO on the iterator class.
**Base ABI:** ARM AAPCS / AAPCS-VFP -- see
[Procedure Call Standard for the ARM Architecture](https://github.com/ARM-software/abi-aa/blob/main/aapcs32/aapcs32.rst).

## Register set

| Use | Registers |
|---|---|
| Integer arg | `R0, R1, R2, R3` (4 slots) |
| Float arg (hard-float) | `S0`-`S15` (16 single-precision slots, or `D0`-`D7` paired as 8 double slots) |
| Integer return | `R0` (and `R1` for 64-bit) |
| Float return | `S0` (`R4`) / `D0` (`R8`) |
| Volatile | `R0-R3, R12, S0-S15`/`D0-D7` |
| Non-volatile | `R4-R11, LR, S16-S31`/`D8-D15` |
| Stack slot size | 4 bytes |
| Stack alignment | 8 B at call site (16 B at function entry on some targets) |

## Argument placement rules

### Integer / pointer / reference args

- Scanned left-to-right.
- 32-bit args take the next free `R0..R3`, then spill to stack 4-byte slots.
- **64-bit args (`I8`, `U8`, `R8`) require 8-byte alignment** in both the
  register file and the stack:
  - If the next free register is odd-numbered (`R1` or `R3`), it's skipped
    and the 64-bit value goes in the next aligned pair (`R2:R3` or `[stack +
    8]`).
  - Stack offsets for 64-bit args are aligned up to 8 bytes.
- **Split between regs and stack**: a 64-bit arg that starts in `R3` is
  passed half in `R3` and half on the stack (the "co-processor register
  split" rule). This is the *only* case where a single arg spans the boundary
  on ARM32.

### Float / double / HFA args (hard-float path only)

The hard-float (AAPCS-VFP) ABI uses a **bitmap allocator** over `S0..S15` so
that floats and doubles can interleave with gaps:

- `R4` (float) takes one S-register slot; `R8` (double) takes one D-register
  slot = 2 S-register slots.
- **HFAs** (Homogeneous Floating-point Aggregates -- structs of 1-4 identical
  floats or doubles) are placed in consecutive S/D slots.
- If the bitmap can't fit the FP arg, all subsequent FP args go on the stack
  (the FP bank is marked exhausted: `_wFPRegs = 0xffff`).

The bitmap walk lives in `Arm32ArgIterator.GetNextOffsetForArg`, lines 79-103.

### Varargs

For variadic methods, **the FP register path is skipped entirely** -- all
args (including floats / doubles / HFAs) go through the integer/stack path.
This is checked via `!IsVarArg` in the FP allocation guard.

## Return values

| Return shape | Where |
|---|---|
| Integer / pointer / reference / 32-bit value type | `R0` |
| `I8` / `U8` | `R0:R1` (low in `R0`, high in `R1`) |
| `R4` / `R8` | `S0` / `D0` (or `R0` / `R0:R1` under softfp; not yet handled) |
| HFA (1-4 floats or doubles, hard-float) | `S0..S3` / `D0..D3` |
| Other value types | Caller-allocated return buffer; pointer passed as a hidden first arg in `R0` (or `R1` if `this` is present); callee uses the buffer |

## Managed-specific behavior

### Hidden argument prefix

Standard CLR prefix applies. Each hidden arg consumes the next integer slot:

```
[this:R0] [retBuf:R1] [genericContext:R2] [asyncContinuation:R3] [varArgCookie] userArgs...
```

If there's no `this`, the ret buf takes `R0`.

### Implicit by-reference: not used

ARM32 sets `EnregisteredParamTypeMaxSize = 0`, meaning the iterator does not
apply an implicit-byref transformation. Value types are passed by value
according to the rules above.

### HFA detection comes from `ArgTypeInfo`

The iterator consults `_argTypeHandle.IsHomogeneousAggregate` and
`_argTypeHandle.RequiresAlign8` (computed by `ArgTypeInfo.FromTypeHandle`
based on `IRuntimeTypeSystem.IsHFA` and `RequiresAlign8`). On ARM32 the HFA
element size is determined entirely by alignment: 8-byte alignment -> double
HFA; 4-byte alignment -> float HFA (see `ArgTypeInfo.ComputeHfaElementSize`).

### 64-bit alignment tracking

The iterator records `_requires64BitAlignment` per arg so that downstream
consumers (e.g. SOS, ClrMD) can correctly compute frame offsets even when
register skipping occurs (e.g. an `I8` in `R2:R3` after a single-slot arg in
`R1`).

### Frame pointer

ARM/ARM64 always allocate a frame pointer (`R11` on ARM32) for both managed
frames and to support the InlinedCallFrame mechanism for P/Invokes
([`clr-abi.md:172`](../docs/design/coreclr/botr/clr-abi.md)).

### Funclets

Same managed-EH funclet model as other platforms. The catch funclet receives
the exception object in `R0`.

### Softfp (armel) not yet supported

The Linux armel calling convention uses integer registers for all args
including floats (no S/D registers used for argument passing). The cDAC
iterator currently hard-codes `IsArmhfABI = true`; a TODO on the class flags
the need to detect armel and disable the FP-register path.

## TypedReference

`TypedReference` is 16 bytes. On ARM32 it does not enregister (value types
generally don't get split across registers on ARM32; the iterator routes
them through the integer slots and stack). A `TypedReference` parameter
consumes 4 pointer-sized slots (16 bytes total) starting at the next 8-byte
alignment boundary. The current cDAC handling depends on the substitution
applied by `ArgTypeInfoSignatureProvider`; refer to the iterator's per-arg
logic for the concrete placement.

## References

- [AAPCS32](https://github.com/ARM-software/abi-aa/blob/main/aapcs32/aapcs32.rst) -- ARM 32-bit ABI.
- [Overview of ARM32 ABI Conventions (MSDN)](https://learn.microsoft.com/cpp/build/overview-of-arm-abi-conventions) -- Windows on ARM specifics.
- [src/coreclr/vm/callingconvention.h](../src/coreclr/vm/callingconvention.h) -- `TARGET_ARM` branches (search for `#elif defined(TARGET_ARM)`).
- [docs/design/coreclr/botr/clr-abi.md](../docs/design/coreclr/botr/clr-abi.md) -- ARM-specific notes throughout.
