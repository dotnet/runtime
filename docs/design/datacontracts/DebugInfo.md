# Contract DebugInfo

This contract is for fetching information related to DebugInfo associated with native code.

## APIs of contract

```csharp
[Flags]
public enum SourceTypes : uint
{
    Default = 0x00, // To indicate that nothing else applies
    StackEmpty = 0x01, // The stack is empty here
    CallInstruction = 0x02  // The actual instruction of a call
    Async = 0x04 // Indicates suspension/resumption for an async call
}
```

```csharp
public readonly struct OffsetMapping
{
    public uint NativeOffset { get; init; }
    public uint ILOffset { get; init; }
    public SourceTypes SourceType { get; init; }
}
```

```csharp
// Returns true if the method at pCode has debug info associated with it.
// Methods such as ILStubs may be JIT-compiled but have no debug metadata.
bool HasDebugInfo(TargetCodePointer pCode);

// Given a code pointer, return the associated native/IL offset mapping and codeOffset.
// If preferUninstrumented, will always read the uninstrumented bounds.
// Otherwise will read the instrumented bounds and fallback to the uninstrumented bounds.
IEnumerable<OffsetMapping> GetMethodNativeMap(TargetCodePointer pCode, bool preferUninstrumented, out uint codeOffset);
```

## Version 1

<!-- BEGIN GENERATED: usage contract=DebugInfo version=c1 -->
### Data descriptors used

_None._

### Global variables used

_None._

### Contracts used

| Contract Name |
| --- |
| `CodeVersions` |
| `ExecutionManager` |
| `PlatformMetadata` |
| `RuntimeInfo` |
<!-- END GENERATED: usage contract=DebugInfo version=c1 -->

### Constants

| Constant Name | Meaning | Value |
| --- | --- | --- |
| `IL_OFFSET_BIAS` | Bias used to encode IL offsets | `0xfffffffd` (-3) |
| `DEBUG_INFO_FAT` | Marker value in first nibble-coded integer indicating a fat header follows | `0x0` |
| `SOURCE_TYPE_BITS` | Number of bits per bounds entry used to encode source type flags | `3` |
| `MAX_ILNUM` | Bias for adjusted encoding of variable numbers | `0xfffffffa` (-6) |
| `CALL_RETURN_ILNUM` | Special variable number identifying a call-return-value entry | `0xfffffffb` (-5) |
| `VLT_REG` | Variable is in a register | `0` |
| `VLT_REG_BYREF` | Address of the variable is in a register | `1` |
| `VLT_REG_FP` | Variable is in an FP register | `2` |
| `VLT_STK` | Variable is on the stack | `3` |
| `VLT_STK_BYREF` | Address of the variable is on the stack | `4` |
| `VLT_REG_REG` | Variable lives in two registers | `5` |
| `VLT_REG_STK` | Variable lives partly in a register and partly on the stack | `6` |
| `VLT_STK_REG` | Reverse of `VLT_REG_STK` | `7` |
| `VLT_STK2` | Variable lives in two stack slots | `8` |
| `VLT_FPSTK` | Variable is on the floating-point stack | `9` |
| `VLT_FIXED_VA` | Fixed argument in a varargs function | `10` |
| `VLT_COUNT` | Number of valid `VarLocType` values | `11` |
| `VLT_INVALID` | Sentinel for invalid locations | `12` |

### DebugInfo Stream Encoding

The DebugInfo stream is encoded using variable length 32-bit values with the following scheme:

A value can be stored using one or more nibbles (a nibble is a 4-bit value). 3 bits of a nibble are used to store 3 bits of the value, and the top bit indicates if  the following nibble contains rest of the value. If the top bit is not set, then this nibble is the last part of the value. The higher bits of the value are written out first, and the lowest 3 bits are written out last.

In the encoded stream of bytes, the lower nibble of a byte is used before the high nibble.

A binary value ABCDEFGHI (where A is the highest bit) is encoded as
the follow two bytes : 1DEF1ABC XXXX0GHI

Examples:
| Decimal Value | Hex Value | Encoded Result |
| --- | --- | --- |
| 0 | 0x0 | X0 |
| 1 | 0x1 | X1 |
| 7 | 0x7 | X7 |
| 8 | 0x8 | 09 |
| 9 | 0x9 | 19 |
| 63 | 0x3F | 7F |
| 64 | 0x40 | F9 X0 |
| 65 | 0x41 | F9 X1 |
| 511 | 0x1FF | FF X7 |
| 512 | 0x200 | 89 08 |
| 513 | 0x201 | 89 18 |

Based on the encoding specification, we use a decoder defined originally for r2r dump `NibbleReader.cs`

### Header Encoding

The first nibble-decoded unsigned integer (`countBoundsOrFatMarker`):

* If `countBoundsOrFatMarker == DEBUG_INFO_FAT` (0), the header is FAT and the next 6 nibble-decoded unsigned integers are, in order:
    1. `BoundsSize`
    2. `VarsSize`
    3. `UninstrumentedBoundsSize`
    4. `PatchpointInfoSize`
    5. `RichDebugInfoSize`
    6. `AsyncInfoSize`
* Otherwise (SLIM header), the value is `BoundsSize` and the next nibble-decoded unsigned integer is `VarsSize`; all other sizes are implicitly 0.

After decoding sizes, chunk start addresses are computed by linear accumulation beginning at the first byte after the header stream:

```
BoundsStart = debugInfo + headerBytesConsumed
VarsStart = BoundsStart + BoundsSize
UninstrumentedBoundsStart = VarsStart + VarsSize
PatchpointInfoStart = UninstrumentedBoundsStart + UninstrumentedBoundsSize
RichDebugInfoStart = PatchpointInfoStart + PatchpointInfoSize
AsyncInfoStart = RichDebugInfoStart + RichDebugInfoSize
DebugInfoEnd = AsyncInfoStart + AsyncInfoSize
```

### Bounds Entry Encoding

Each bounds entry uses three independent flag bits for source type:
`[3 bits sourceFlags][nativeDeltaBits][ilOffsetBits]`.

Source type bits (low -> high):
| Bit | Mask | Meaning |
| --- | --- | --- |
| 0 | 0x1 | `CallInstruction` |
| 1 | 0x2 | `StackEmpty` |
| 2 | 0x4 | `Async` |

`SourceTypeInvalid` is represented by all three bits clear (0). Combinations are produced by OR-ing masks (e.g., `StackEmpty | CallInstruction`).

Pseudo-code for source type extraction:
```csharp
SourceTypes sourceType = 0;
if ((encoded & 0x1) != 0) sourceType |= SourceTypes.CallInstruction;
if ((encoded & 0x2) != 0) sourceType |= SourceTypes.StackEmpty;
if ((encoded & 0x4) != 0) sourceType |= SourceTypes.Async;
```

After masking the 3 bits, shift them out before reading native delta and IL offset fields as before.

### Variable Location APIs

The contract decodes native variable location information from the Vars section of the debug info blob.

Additional APIs:
```csharp
// Describes the kind of location where a variable is stored.
public enum DebugVarLocKind
{
    Register,
    Stack,
    RegisterRegister,
    RegisterStack,
    StackRegister,
    DoubleStack,
    FloatingPointStack,
    FixedVarArg,
}

public readonly struct DebugVarInfo
{
    public uint StartOffset { get; init; }
    public uint EndOffset { get; init; }
    public uint VarNumber { get; init; }
    public DebugVarLocKind Kind { get; init; }
    public bool IsByRef { get; init; }
    public bool IsFloatingPoint { get; init; }
    public uint Register { get; init; }
    public uint Register2 { get; init; }
    public uint BaseRegister { get; init; }
    public int StackOffset { get; init; }
    public uint BaseRegister2 { get; init; }
    public int StackOffset2 { get; init; }
    public uint FloatingPointStackRegister { get; init; }
    public uint FixedVarArgOffset { get; init; }
    public uint CallReturnValueILOffset { get; init; }
}

// Given a code pointer, return the variable location info for the method.
IEnumerable<DebugVarInfo> GetMethodVarInfo(TargetCodePointer pCode, out uint codeOffset);
```

### Vars Data Encoding

Each variable entry in the Vars section is nibble-encoded as follows:

1. `varNumber` — encoded as adjusted unsigned (`value - MAX_ILNUM`)
2. `startOffset` — encoded unsigned 32-bit integer
3. The next field depends on `varNumber`:
   - If `varNumber == CALL_RETURN_ILNUM`: `callReturnValueILOffset` — encoded unsigned 32-bit integer (IL offset of the call site whose return value this entry describes). `endOffset` is implicit and equals `startOffset + 1`.
   - Otherwise: `endOffset` — encoded as delta from `startOffset` (unsigned). `callReturnValueILOffset` is implicit and equals `0`.
4. `VarLocType` — encoded unsigned 32-bit integer
5. Location fields depend on the `VarLocType`:

| VarLocType | Fields (in encoding order) |
| --- | --- |
| `VLT_REG`, `VLT_REG_FP`, `VLT_REG_BYREF` | register (encoded unsigned) |
| `VLT_STK`, `VLT_STK_BYREF` | baseRegister (encoded unsigned), stackOffset (encoded signed, x86: ×4) |
| `VLT_REG_REG` | register1 (encoded unsigned), register2 (encoded unsigned) |
| `VLT_REG_STK` | register (encoded unsigned), baseRegister (encoded unsigned), stackOffset (encoded signed, x86: ×4) |
| `VLT_STK_REG` | stackOffset (encoded signed, x86: ×4), baseRegister (encoded unsigned), register (encoded unsigned) |
| `VLT_STK2` | baseRegister (encoded unsigned), stackOffset (encoded signed, x86: ×4) |
| `VLT_FPSTK` | fpRegister (encoded unsigned) |
| `VLT_FIXED_VA` | offset (encoded unsigned) |

Signed integers are encoded using the same unsigned scheme, with the sign bit stored in bit 0 (`value = unsigned >> 1`, negate if `unsigned & 1`). On x86, stack offsets are DWORD-aligned and stored divided by `sizeof(DWORD)`.

### Async Suspension Point APIs

We also support decoding async suspension points (and their captured continuation-object locals) from the `AsyncInfo` chunk of the debug info blob. The chunk is present only for methods that the JIT compiled with runtime-async suspension points; for all other methods, `AsyncInfoSize` is `0` in the FAT header and the API returns an empty list.

Additional types:
```csharp
// A native code location at which an async method may suspend, together with
// the continuation-object locals captured at that point.
public readonly struct AsyncSuspensionInfo
{
    public uint NativeOffset { get; init; }
    public IReadOnlyList<AsyncLocalInfo> Locals { get; init; }
}

// A single local captured into the continuation object at a suspension point.
public readonly struct AsyncLocalInfo
{
    // Offset of the local within the continuation object's data area.
    public uint Offset { get; init; }
    // IL var number of the local (or a synthetic marker such as MAX_ILNUM-relative values).
    public uint ILVarNumber { get; init; }
}
```

```csharp
IReadOnlyList<AsyncSuspensionInfo> GetAsyncSuspensionPoints(TargetCodePointer pCode);
```

### AsyncInfo Data Encoding

Each entry is nibble-encoded as follows:

1. `NumSuspensionPoints` — encoded unsigned 32-bit integer.
2. Total var count across all suspension points — encoded unsigned 32-bit integer. Informational only; the decoder reads it but does not need it, since the per-suspension-point counts read in step 3 already cover the entire flat var list.
3. For each of the `NumSuspensionPoints` suspension points (in order):
   * `DiagnosticNativeOffset` — encoded signed delta from the previous suspension point's offset (the first delta is from `0`). Deltas are not required to be monotonic.
   * `NumContinuationVars` — encoded unsigned 32-bit integer giving the number of continuation locals captured at this suspension point.
4. For each var (a single flat sequence, partitioned by the `NumContinuationVars` counts from step 3, in suspension-point order):
   * `VarNumber - MAX_ILNUM` — encoded unsigned 32-bit integer. The `MAX_ILNUM` bias keeps the synthetic negative IL var numbers (e.g. `VARARGS_HND_ILNUM`, `RETBUF_ILNUM`, `TYPECTXT_ILNUM`) representable as unsigned values; the decoder reverses the bias by adding `MAX_ILNUM` back.
   * `Offset` — encoded unsigned 32-bit integer giving the byte offset of the local within the continuation object's data area.

`AsyncSuspensionInfo.Locals[i]` for the `n`-th suspension point therefore corresponds to the `i`-th var in the flat sequence whose flat index is the prefix sum of `NumContinuationVars[0..n-1]` plus `i`.
