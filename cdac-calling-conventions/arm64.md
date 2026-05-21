# ARM64 (AAPCS64) Managed Calling Convention

**Iterator:** [`Arm64ArgIterator.cs`](../src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/StackWalk/CallingConvention/Arm64ArgIterator.cs)
**Applies to:** Linux ARM64, Windows on ARM64, Apple (macOS/iOS) ARM64.
**Base ABI:** AAPCS64 -- see [Procedure Call Standard for the Arm 64-bit Architecture](https://github.com/ARM-software/abi-aa/blob/main/aapcs64/aapcs64.rst).

## Register set

| Use | Registers |
|---|---|
| Integer arg | `X0`-`X7` (8 slots) |
| Float / SIMD arg | `V0`-`V7` (8 slots, 16 bytes each) |
| Bank independence | The integer and FP banks are tracked **independently** (like SysV x64, unlike Windows x64) |
| Indirect result location | `X8` (return buffer pointer; **separate from arg regs**) |
| Integer return | `X0` (and `X1` for 16-byte structs) |
| Float return | `V0` (and `V1..V3` for HFAs) |
| Volatile | `X0-X17, V0-V7, V16-V31` |
| Non-volatile | `X19-X28, X29 (FP), X30 (LR), V8-V15` |
| Stack slot size | 8 bytes (4 bytes on Apple for natural-alignment packing) |
| Stack alignment | 16 B at call site |

## Argument placement rules

### Integer / pointer / reference args

- An arg of size <= 8 takes the next free `X0..X7` slot.
- An arg of size 9-16 takes two consecutive integer registers (a "consecutive
  pair").
- If both halves don't fit in the remaining int registers:
  - **Linux/Apple**: the entire arg goes on the stack; no registers are
    consumed.
  - **Windows**: special split rule -- the head goes into the remaining X
    register(s) and the tail goes on the stack. This is **only** an issue for
    variadic methods on Windows. (See "Windows varargs" below; not yet
    implemented in cDAC.)
- Anything larger than 16 bytes is passed via **implicit by-reference**: the
  caller materializes the value and passes a pointer in the next int slot.

### Float / double / HFA args

- `R4` (float) / `R8` (double) takes one V register slot at 16 bytes per slot
  (low bits of `Vn`).
- **HFAs** (1-4 floats or doubles or vectors with all identical element
  type) get spread across consecutive `V` registers, each in its own slot.
  E.g. a `Vector4` (4 floats) takes `V0..V3`.
- If the HFA doesn't fit in remaining V slots, it goes on the stack (no V
  registers consumed).

The check is:

```csharp
if (cFPRegs > 0 && !IsVarArg) { ... try V regs ... }
```

### Varargs

Variadic methods diverge from the fixed-arg rules:

- **All variadic args go through the X-register / stack path**, not V regs.
  `R4`/`R8` arguments are widened to 64 bits and placed in `X0..X7` or on the
  stack. The cDAC encodes this as the `!IsVarArg` guard at the FP branch.
- **HFAs lose their HFA-ness**: a homogeneous float aggregate is treated as
  an ordinary composite for variadic calls. cDAC implements this by also
  forcing the implicit-byref path for >16-byte HFAs under varargs:
  ```csharp
  protected override bool IsArgPassedByRefArchSpecific()
      => _argType == CorElementType.ValueType
          && _argSize > EnregisteredParamTypeMaxSize
          && (!_argTypeHandle.IsHomogeneousAggregate || IsVarArg);
  ```
- **Apple ARM64**: variadic args go *entirely on the stack* (Linux/AAPCS64
  starts on stack only after `X7` is filled; Apple skips registers
  altogether). Not yet specifically handled in cDAC -- the iterator only
  applies Apple's natural-alignment stack-packing rule, not the
  "all-on-stack" varargs rule.
- **Windows ARM64**: a variadic arg whose start fits in a remaining `X`
  register but whose tail spills past `X7` is **split** between regs and
  stack (the first 64 bytes of the stack are loaded into `X0..X7` and the
  rest is contiguous). The CoreCLR VM has this code at
  [`callingconvention.h:1740-1756`](../src/coreclr/vm/callingconvention.h);
  the cDAC iterator does **not** yet implement it (known gap).

### Apple ARM64 stack packing

On Apple ARM64 (Darwin), stack arguments use **natural alignment** (smaller
than 8 bytes) rather than the AAPCS64 8-byte slot. The cDAC handles this in
`StackElemSize` and the per-arg alignment computation:

```csharp
if (_isAppleArm64ABI) {
    int alignment = isValueType ? (isFloatHFA ? 4 : 8) : cbArg;
    _ofsStack = AlignUp(_ofsStack, alignment);
}
```

## Return values

| Return shape | Where |
|---|---|
| Integer / pointer / reference / size <= 8 value type | `X0` |
| 16-byte value type | `X0, X1` |
| `R4` / `R8` | `V0` (full SIMD reg) |
| HFA (up to 4 floats/doubles) | `V0..V3` |
| Value type > 16 bytes (and not an HFA) | Caller passes an indirect result pointer in `X8`; callee writes through it; `X8` is **separate** from the regular arg registers |

## Managed-specific behavior

### Return buffer is in `X8`

Unlike the other platforms where the ret buf consumes an argument register
slot, ARM64's AAPCS64 reserves `X8` (the "Indirect Result Location Register")
specifically for the return-buffer pointer. This means **the ret buf does
*not* consume an X0..X7 slot**, and `this` lands in `X0` even when a ret buf
is present.

The cDAC reflects this with:

```csharp
public override bool IsRetBuffPassedAsFirstArg => false;
public override int GetRetBuffArgOffset(bool hasThis) => (int)_layout.FirstGCRefMapSlot;
```

### Hidden argument prefix

Standard CLR prefix applies, but the ret buf goes in `X8` rather than `X0`:

```
X8 = retBuf                                                (separate reg)
[this:X0] [genericContext:X1] [asyncContinuation:X2] [varArgCookie:X3] userArgs...
```

### Implicit by-reference for large value types

`EnregisteredParamTypeMaxSize = 16`. Value types larger than 16 bytes that
are *not* HFAs go via implicit by-reference; the caller may legitimately
point into the GC heap, so the JIT reports the pointer as a GC `BYREF` and
uses checked write barriers.

HFAs that are also > 16 bytes (e.g. 4 doubles = 32 bytes) are passed in V
registers when *not* varargs, and by implicit-byref when varargs. See the
`IsArgPassedByRefArchSpecific` override above.

### Frame pointer

ARM64 always allocates a frame pointer (`X29`), partly for AAPCS64 frame
chaining and partly for the InlinedCallFrame P/Invoke mechanism. Funclets
share the parent function's `X29` to access its locals.

### Funclets

Same managed-EH funclet model. The catch funclet receives the exception
object in `X0`.

## TypedReference

`TypedReference` is 16 bytes. It is passed in **2 GP registers** -- typically
`X0, X1` -- since 16 <= `EnregisteredParamTypeMaxSize`. It is *not* an HFA so
the FP branch doesn't apply. Returned in `X0, X1`.

The cDAC's `ArgTypeInfoSignatureProvider` substitutes the `g_TypedReferenceMT`
MethodTable when the signature contains `ELEMENT_TYPE_TYPEDBYREF`, so the
iterator treats it as an ordinary 16-byte value type.

## Known gaps in cDAC

The iterator's correctness is high for fixed-arg calls but has known holes:

1. **Windows ARM64 varargs split** (the `X7 -> stack` boundary case) is not
   implemented. CoreCLR has this at `callingconvention.h:1740-1756`.
2. **Apple ARM64 varargs** ("all variadic args on stack") is not specifically
   handled; only the stack-packing portion is.
3. Tests for these gaps are present but marked `[Skip("audit gap")]`:
   `Windows_VarArgs_StructSpansX7AndStack_AuditGap4`,
   `HFA_FourFloats_ShouldReportFourFPSlots`.

## References

- [AAPCS64](https://github.com/ARM-software/abi-aa/blob/main/aapcs64/aapcs64.rst) -- §6.4 parameter passing, §6.8 variadic functions.
- [Apple ARM64 documentation](https://developer.apple.com/documentation/xcode/writing-arm64-code-for-apple-platforms) -- the deviations from AAPCS64.
- [Microsoft ARM64 ABI](https://learn.microsoft.com/cpp/build/arm64-windows-abi-conventions) -- Windows-specific varargs split.
- [src/coreclr/vm/callingconvention.h](../src/coreclr/vm/callingconvention.h) -- `TARGET_ARM64` branches (especially the varargs handling around lines 1700-1760).
- [docs/design/coreclr/botr/clr-abi.md](../docs/design/coreclr/botr/clr-abi.md) -- CLR-wide ABI notes.
