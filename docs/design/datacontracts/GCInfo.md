# Contract GCInfo

This contract is for fetching information related to GCInfo associated with native code. Currently, this contract does not support x86 architecture.

The GCInfo contract has platform specific implementations as GCInfo differs per architecture. With the exception of x86, all platforms have a common encoding scheme with different encoding lengths and normalization functions for data. x86 uses an entirely different scheme which is not currently supported by this contract.

## APIs of contract

```csharp
public interface IGCInfoHandle { }
```

```csharp
// Decodes GCInfo using the platform-specific encoding for the target architecture
IGCInfoHandle DecodePlatformSpecificGCInfo(TargetPointer gcInfoAddress, uint gcVersion);

// Decodes GCInfo using the interpreter encoding, regardless of target architecture
IGCInfoHandle DecodeInterpreterGCInfo(TargetPointer gcInfoAddress, uint gcVersion);

/* Methods to query information from the GCInfo */

// Fetches length of code as reported in GCInfo
uint GetCodeLength(IGCInfoHandle handle);

// Returns the stack base register number decoded from GCInfo
uint GetStackBaseRegister(IGCInfoHandle handle);

// Returns the list of interruptible code offset ranges from the GCInfo
IReadOnlyList<InterruptibleRange> GetInterruptibleRanges(IGCInfoHandle handle);

// Returns all live GC slots at the given instruction offset
IReadOnlyList<LiveSlot> EnumerateLiveSlots(IGCInfoHandle handle, uint instructionOffset, GcSlotEnumerationOptions options);
```

```csharp
// Describes a code region where the GC can safely interrupt execution.
public readonly record struct InterruptibleRange(
    uint StartOffset,   // Start of the interruptible region (byte offset from method start)
    uint EndOffset);    // End of the interruptible region, exclusive (byte offset from method start)

// Describes a live GC slot at a given instruction offset.
public readonly record struct LiveSlot(
    bool IsRegister,       // True if the slot is a CPU register; false if stack location
    uint RegisterNumber,   // Register number (meaningful only when IsRegister is true)
    int SpOffset,          // Stack offset from the base (meaningful only when IsRegister is false)
    uint SpBase,           // Stack base: 0 = CALLER_SP_REL, 1 = SP_REL, 2 = FRAMEREG_REL
    uint GcFlags);         // GC slot flags: 0x1 = interior pointer, 0x2 = pinned

// Options controlling which GC slots are reported by EnumerateLiveSlots.
public record struct GcSlotEnumerationOptions
{
    bool IsActiveFrame;                  // True if this is the active (leaf) stack frame
    bool IsExecutionAborted;             // True if execution was interrupted by an exception
    bool IsParentOfFuncletStackFrame;    // True if a funclet already reported GC references
    bool SuppressUntrackedSlots;         // True to suppress untracked slots (e.g., filter funclets)
    bool ReportFPBasedSlotsOnly;         // True to report only frame-register-relative stack slots
}
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| _none_ |  | |

Contracts used:
| Contract Name |
| --- |
| _none_ |

Constants:
| Constant Name | Meaning | Value |
| --- | --- | --- |
| `NO_GS_COOKIE` | Indicates no GS cookie is present | -1 |
| `NO_STACK_BASE_REGISTER` | Indicates no stack base register is used | 0xFFFFFFFF |
| `NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA` | Indicates no Edit and Continue preserved area | 0xFFFFFFFF |
| `NO_GENERICS_INST_CONTEXT` | Indicates no generics instantiation context | -1 |
| `NO_REVERSE_PINVOKE_FRAME` | Indicates no reverse P/Invoke frame | -1 |
| `NO_PSP_SYM` | Indicates no PSP symbol | -1 |


### GCInfo Format

The GCInfo format consists of a header structure and following data types. The header is either 'slim' for simple methods that can use the compact encoding scheme or a 'fat' header containing more details.

#### GCInfo Header

##### Slim Header
| Name | Bits | Meaning | Condition |
| --- | --- | --- | --- |
| IsSlimHeader | 1 | If 0, this GCInfo uses the slim header encoding | |
| UsingStackBaseRegister | 1 | If true, has stack base register of normalized value `0`. Otherwise this GCInfo has no stack base register. Controls `GC_INFO_HAS_STACK_BASE_REGISTER` of HeaderFlags | |
| CodeLength | `CODE_LENGTH_ENCBASE` | Normalized method length | |
| NumSafePoints | `NUM_SAFE_POINTS_ENCBASE` | Number of safe points/callsites | #ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED |

All other values are assumed to be `0`.

#### Fat Header

The fat header is used for methods that cannot be encoded using the compact slim header format. It contains additional flags and optional fields based on method characteristics.

| Name | Bits | Meaning | Condition |
| --- | --- | --- | --- |
| IsSlimHeader | 1 | If 1, this GCInfo uses the fat header encoding | |
| HeaderFlags | `GC_INFO_FLAGS_BIT_SIZE` (10) | Bitfield containing various method flags | |
| CodeLength | `CODE_LENGTH_ENCBASE` | Normalized method length | |
| PrologSize | `NORM_PROLOG_SIZE_ENCBASE` | Normalized prolog size - 1 | If HeaderFlags has `GC_INFO_HAS_GS_COOKIE` or `HeaderFlags & GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK != 0` |
| EpilogSize | `NORM_EPILOG_SIZE_ENCBASE` | Normalized epilog size | If HeaderFlags has `GC_INFO_HAS_GS_COOKIE` |
| GSCookieStackSlot | `GS_COOKIE_STACK_SLOT_ENCBASE` | Normalized stack slot for GS cookie | If HeaderFlags has `GC_INFO_HAS_GS_COOKIE` |
| GenericsInstContextStackSlot | `GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE` | Normalized stack slot for generics context | `HeaderFlags & GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK != 0` |
| StackBaseRegister | `STACK_BASE_REGISTER_ENCBASE` | Normalized stack base register number | If HeaderFlags has GC_INFO_HAS_STACK_BASE_REGISTER |
| SizeOfEditAndContinuePreservedArea | `SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE` | Size of EnC preserved area | If HeaderFlags has `GC_INFO_HAS_EDIT_AND_CONTINUE_INFO` |
| SizeOfEditAndContinueFixedStackFrame | `SIZE_OF_EDIT_AND_CONTINUE_FIXED_STACK_FRAME_ENCBASE` | Size of EnC fixed stack frame | If HeaderFlags has `GC_INFO_HAS_EDIT_AND_CONTINUE_INFO` and platform is ARM64 |
| ReversePInvokeFrameSlot | `REVERSE_PINVOKE_FRAME_ENCBASE` | Normalized reverse P/Invoke frame slot | If GC_INFO_REVERSE_PINVOKE_FRAME |
| SizeOfStackOutgoingAndScratchArea | `SIZE_OF_STACK_AREA_ENCBASE` | Size of stack parameter area | Platform dependent |
| NumSafePoints | `NUM_SAFE_POINTS_ENCBASE` | Number of safe points/callsites | #ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED |
| NumInterruptibleRanges | `NUM_INTERRUPTIBLE_RANGES_ENCBASE` | Number of interruptible ranges | |

##### Header Flags

The HeaderFlags field contains the following bit flags:

| Flag | Bit Position | Meaning |
| --- | --- | --- |
| GC_INFO_IS_VARARG | 0x1 | Method uses variable arguments |
| GC_INFO_HAS_SECURITY_OBJECT | 0x2 | Method has security object (deprecated) |
| GC_INFO_HAS_GS_COOKIE | 0x4 | Method has GS cookie for stack protection |
| GC_INFO_HAS_PSP_SYM | 0x8 | Method has PSP symbol (deprecated) |
| GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK | 0x30 | Mask for generics instantiation context type |
| GC_INFO_HAS_STACK_BASE_REGISTER | 0x40 | Method uses a stack base register |
| GC_INFO_WANTS_REPORT_ONLY_LEAF | 0x80 | AMD64: Report only leaf frames; ARM/ARM64: Has tail calls |
| GC_INFO_HAS_EDIT_AND_CONTINUE_INFO | 0x100 | Method has Edit and Continue information |
| GC_INFO_REVERSE_PINVOKE_FRAME | 0x200 | Method has reverse P/Invoke frame |

### GCInfo Body

Following the header, the GCInfo body contains several data sections in the following order:

1. **Call Sites Offsets** - Encoded offsets of call sites/safe points (if PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED)
2. **Interruptible Ranges** - Ranges where the method can be interrupted for GC
3. **Slot Table** - Information about GC-tracked slots (registers and stack locations)

The rest of the GCInfo body is not yet decoded in the cDAC.

#### Interruptible Ranges

Interruptible ranges define code regions where garbage collection can safely occur. Each range is encoded as:

- **StartOffset** - Normalized code offset where the range begins
- **Length** - Normalized length of the interruptible region

The ranges are encoded using delta compression, where each range's start offset is relative to the previous range's end offset.

#### Slot Table

The slot table describes all GC-tracked locations (registers and stack slots) used by the method. It consists of three sections:

1. **Register Slots** - GC-tracked CPU registers
2. **Stack Slots** - GC-tracked stack locations
3. **Untracked Slots** - Stack locations that are not GC-tracked

Each slot entry contains:
- **Location** - Register number or stack offset
- **Flags** - GC slot type (base pointer, interior pointer, pinned, untracked)
- **Base** - For stack slots: caller SP relative, SP relative, or frame register relative

Slots use delta encoding where consecutive entries encode only the difference from the previous entry when flags match.

### Platform specific constants

#### AMD64

| Encoding Base | Value | Purpose |
| --- | --- | --- |
| `GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE` | 6 | Base bits for generics instantiation context stack slot |
| `GS_COOKIE_STACK_SLOT_ENCBASE` | 6 | Base bits for GS cookie stack slot |
| `CODE_LENGTH_ENCBASE` | 8 | Base bits for encoding method code length |
| `STACK_BASE_REGISTER_ENCBASE` | 3 | Base bits for stack base register number |
| `SIZE_OF_STACK_AREA_ENCBASE` | 3 | Base bits for stack parameter area size |
| `SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE` | 4 | Base bits for Edit and Continue preserved area size |
| `REVERSE_PINVOKE_FRAME_ENCBASE` | 6 | Base bits for reverse P/Invoke frame slot |
| `NUM_REGISTERS_ENCBASE` | 2 | Base bits for number of register slots |
| `NUM_STACK_SLOTS_ENCBASE` | 2 | Base bits for number of stack slots |
| `NUM_UNTRACKED_SLOTS_ENCBASE` | 1 | Base bits for number of untracked slots |
| `NORM_PROLOG_SIZE_ENCBASE` | 5 | Base bits for normalized prolog size |
| `NORM_EPILOG_SIZE_ENCBASE` | 3 | Base bits for normalized epilog size |
| `INTERRUPTIBLE_RANGE_DELTA1_ENCBASE` | 6 | Base bits for first interruptible range delta |
| `INTERRUPTIBLE_RANGE_DELTA2_ENCBASE` | 6 | Base bits for second interruptible range delta |
| `REGISTER_ENCBASE` | 3 | Base bits for register slot encoding |
| `REGISTER_DELTA_ENCBASE` | 2 | Base bits for register slot delta encoding |
| `STACK_SLOT_ENCBASE` | 6 | Base bits for stack slot encoding |
| `STACK_SLOT_DELTA_ENCBASE` | 4 | Base bits for stack slot delta encoding |
| `NUM_SAFE_POINTS_ENCBASE` | 2 | Base bits for number of safe points |
| `NUM_INTERRUPTIBLE_RANGES_ENCBASE` | 1 | Base bits for number of interruptible ranges |

##### AMD64 Normalization/Denormalization Rules

| Operation | Normalization (Encode) | Denormalization (Decode) |
| --- | --- | --- |
| **Stack Base Register** | `reg ^ 0x5` | `reg ^ 0x5` |
| **Code Length** | No change | No change |
| **Code Offset** | No change | No change |
| **Stack Slot** | `offset >> 3` | `offset << 3` |
| **Stack Area Size** | `size >> 3` | `size << 3` |

#### ARM64

| Encoding Base | Value | Purpose |
| --- | --- | --- |
| `GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE` | 6 | Base bits for generics instantiation context stack slot |
| `GS_COOKIE_STACK_SLOT_ENCBASE` | 6 | Base bits for GS cookie stack slot |
| `CODE_LENGTH_ENCBASE` | 8 | Base bits for encoding method code length |
| `STACK_BASE_REGISTER_ENCBASE` | 2 | Base bits for stack base register number |
| `SIZE_OF_STACK_AREA_ENCBASE` | 3 | Base bits for stack parameter area size |
| `SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE` | 4 | Base bits for Edit and Continue preserved area size |
| `SIZE_OF_EDIT_AND_CONTINUE_FIXED_STACK_FRAME_ENCBASE` | 4 | Base bits for Edit and Continue fixed stack frame size (ARM64 only) |
| `REVERSE_PINVOKE_FRAME_ENCBASE` | 6 | Base bits for reverse P/Invoke frame slot |
| `NUM_REGISTERS_ENCBASE` | 3 | Base bits for number of register slots |
| `NUM_STACK_SLOTS_ENCBASE` | 2 | Base bits for number of stack slots |
| `NUM_UNTRACKED_SLOTS_ENCBASE` | 1 | Base bits for number of untracked slots |
| `NORM_PROLOG_SIZE_ENCBASE` | 5 | Base bits for normalized prolog size |
| `NORM_EPILOG_SIZE_ENCBASE` | 3 | Base bits for normalized epilog size |
| `INTERRUPTIBLE_RANGE_DELTA1_ENCBASE` | 6 | Base bits for first interruptible range delta |
| `INTERRUPTIBLE_RANGE_DELTA2_ENCBASE` | 6 | Base bits for second interruptible range delta |
| `REGISTER_ENCBASE` | 3 | Base bits for register slot encoding |
| `REGISTER_DELTA_ENCBASE` | 2 | Base bits for register slot delta encoding |
| `STACK_SLOT_ENCBASE` | 6 | Base bits for stack slot encoding |
| `STACK_SLOT_DELTA_ENCBASE` | 4 | Base bits for stack slot delta encoding |
| `NUM_SAFE_POINTS_ENCBASE` | 3 | Base bits for number of safe points |
| `NUM_INTERRUPTIBLE_RANGES_ENCBASE` | 1 | Base bits for number of interruptible ranges |

##### ARM64 Normalization/Denormalization Rules

| Operation | Normalization (Encode) | Denormalization (Decode) |
| --- | --- | --- |
| **Stack Base Register** | `reg ^ 0x29` | `reg ^ 0x29` |
| **Code Length** | `length >> 2` | `length << 2` |
| **Code Offset** | `offset >> 2` | `offset << 2` |
| **Stack Slot** | `offset >> 3` | `offset << 3` |
| **Stack Area Size** | `size >> 3` | `size << 3` |

#### ARM (32-bit)

| Encoding Base | Value | Purpose |
| --- | --- | --- |
| `GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE` | 5 | Base bits for generics instantiation context stack slot |
| `GS_COOKIE_STACK_SLOT_ENCBASE` | 5 | Base bits for GS cookie stack slot |
| `CODE_LENGTH_ENCBASE` | 7 | Base bits for encoding method code length |
| `STACK_BASE_REGISTER_ENCBASE` | 1 | Base bits for stack base register number |
| `SIZE_OF_STACK_AREA_ENCBASE` | 3 | Base bits for stack parameter area size |
| `SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE` | 3 | Base bits for Edit and Continue preserved area size |
| `REVERSE_PINVOKE_FRAME_ENCBASE` | 5 | Base bits for reverse P/Invoke frame slot |
| `NUM_REGISTERS_ENCBASE` | 2 | Base bits for number of register slots |
| `NUM_STACK_SLOTS_ENCBASE` | 3 | Base bits for number of stack slots |
| `NUM_UNTRACKED_SLOTS_ENCBASE` | 3 | Base bits for number of untracked slots |
| `NORM_PROLOG_SIZE_ENCBASE` | 5 | Base bits for normalized prolog size |
| `NORM_EPILOG_SIZE_ENCBASE` | 3 | Base bits for normalized epilog size |
| `INTERRUPTIBLE_RANGE_DELTA1_ENCBASE` | 4 | Base bits for first interruptible range delta |
| `INTERRUPTIBLE_RANGE_DELTA2_ENCBASE` | 6 | Base bits for second interruptible range delta |
| `REGISTER_ENCBASE` | 2 | Base bits for register slot encoding |
| `REGISTER_DELTA_ENCBASE` | 1 | Base bits for register slot delta encoding |
| `STACK_SLOT_ENCBASE` | 6 | Base bits for stack slot encoding |
| `STACK_SLOT_DELTA_ENCBASE` | 4 | Base bits for stack slot delta encoding |
| `NUM_SAFE_POINTS_ENCBASE` | 3 | Base bits for number of safe points |
| `NUM_INTERRUPTIBLE_RANGES_ENCBASE` | 2 | Base bits for number of interruptible ranges |

##### ARM (32-bit) Normalization/Denormalization Rules

| Operation | Normalization (Encode) | Denormalization (Decode) |
| --- | --- | --- |
| **Stack Base Register** | `((reg - 4) & 7) ^ 7` | `(reg ^ 7) + 4` |
| **Code Length** | `length >> 1` | `length << 1` |
| **Code Offset** | `offset >> 1` | `offset << 1` |
| **Stack Slot** | `offset >> 2` | `offset << 2` |
| **Stack Area Size** | `size >> 2` | `size << 2` |

#### Interpreter (WASM / FEATURE_INTERPRETER)

The interpreter uses a platform-independent encoding where all normalization and denormalization functions are identity (no transformation). This encoding is used for WASM targets (where `TargetGcInfoEncoding` is `InterpreterGcInfoEncoding`) and on any architecture when `FEATURE_INTERPRETER` is enabled.

| Encoding Base | Value | Purpose |
| --- | --- | --- |
| `GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE` | 6 | Base bits for generics instantiation context stack slot |
| `GS_COOKIE_STACK_SLOT_ENCBASE` | 6 | Base bits for GS cookie stack slot |
| `CODE_LENGTH_ENCBASE` | 8 | Base bits for encoding method code length |
| `STACK_BASE_REGISTER_ENCBASE` | 3 | Base bits for stack base register number |
| `SIZE_OF_STACK_AREA_ENCBASE` | 3 | Base bits for stack parameter area size |
| `SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE` | 4 | Base bits for Edit and Continue preserved area size |
| `REVERSE_PINVOKE_FRAME_ENCBASE` | 6 | Base bits for reverse P/Invoke frame slot |
| `NUM_REGISTERS_ENCBASE` | 2 | Base bits for number of register slots |
| `NUM_STACK_SLOTS_ENCBASE` | 2 | Base bits for number of stack slots |
| `NUM_UNTRACKED_SLOTS_ENCBASE` | 1 | Base bits for number of untracked slots |
| `NORM_PROLOG_SIZE_ENCBASE` | 5 | Base bits for normalized prolog size |
| `NORM_EPILOG_SIZE_ENCBASE` | 3 | Base bits for normalized epilog size |
| `INTERRUPTIBLE_RANGE_DELTA1_ENCBASE` | 6 | Base bits for first interruptible range delta |
| `INTERRUPTIBLE_RANGE_DELTA2_ENCBASE` | 6 | Base bits for second interruptible range delta |
| `REGISTER_ENCBASE` | 3 | Base bits for register slot encoding |
| `REGISTER_DELTA_ENCBASE` | 2 | Base bits for register slot delta encoding |
| `STACK_SLOT_ENCBASE` | 6 | Base bits for stack slot encoding |
| `STACK_SLOT_DELTA_ENCBASE` | 4 | Base bits for stack slot delta encoding |
| `NUM_SAFE_POINTS_ENCBASE` | 2 | Base bits for number of safe points |
| `NUM_INTERRUPTIBLE_RANGES_ENCBASE` | 1 | Base bits for number of interruptible ranges |

##### Interpreter Normalization/Denormalization Rules

All normalization and denormalization operations are identity functions (no transformation):

| Operation | Normalization (Encode) | Denormalization (Decode) |
| --- | --- | --- |
| **Stack Base Register** | No change | No change |
| **Code Length** | No change | No change |
| **Code Offset** | No change | No change |
| **Stack Slot** | No change | No change |
| **Stack Area Size** | No change | No change |

The interpreter does not have a fixed stack parameter scratch area (`HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA = false`).

### Encoding Scheme

GCInfo uses a variable-length encoding scheme to efficiently store numeric values. The encoding is designed to use fewer bits for smaller, more common values.

#### Variable Length Unsigned Encoding

Numbers are encoded using a base number of bits plus extension bits when needed:

1. **Base Encoding**: Use `base` bits to store the value
2. **Extension Bit**: If the value doesn't fit in `base` bits, set bit `base` to 1 and use `base+1` additional bits
3. **Continuation**: This process continues until the value fits

The minimum encoding uses `base+1` bits, where bit `base` indicates if more bits follow.

**Example with base=3:**
- Value 0-6: Encoded in 4 bits as `0XXX` (where XXX is the 3-bit value)
- Value 7-14: Encoded in 8 bits as `1XXX YYYY` (where XXX are the low 3 bits, YYYY are the next 4 bits)
- Value 15+: Uses additional extension groups

#### Variable Length Signed Encoding

Signed values use the same encoding as unsigned, but with sign considerations:

- A number fits in `base` bits if the topmost bit of the base-bit chunk matches the sign of the entire number
- This ensures proper sign extension when decoding

### Implementation

The GCInfo decoder uses **lazy sequential decoding** — data is decoded on demand as APIs are called, and each section of the bitstream is decoded at most once. The decoder tracks a set of `DecodePoints` that represent completion of each section. When an API like `GetCodeLength()` or `GetInterruptibleRanges()` is called, the decoder advances through the bitstream until the requested data has been decoded.

```csharp
IGCInfoHandle DecodePlatformSpecificGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
{
    // Create a new decoder instance for the specified platform traits
    return new GcInfoDecoder<PlatformTraits>(target, gcInfoAddress, gcVersion);
}

IGCInfoHandle DecodeInterpreterGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
{
    // Create a new decoder instance using the interpreter encoding
    return new GcInfoDecoder<InterpreterGCInfoTraits>(target, gcInfoAddress, gcVersion);
}
```

#### Header Decoding

The first bit of the GCInfo bitstream determines whether the header is **slim** or **fat**.

**Slim Header** (first bit = 0):

The slim header is a compact encoding for simple methods. It reads only a few fields:

```
isSlimHeader = ReadBits(1)  // 0 = slim
usingStackBaseRegister = ReadBits(1)
if usingStackBaseRegister:
    stackBaseRegister = DenormalizeStackBaseRegister(0)
codeLength = DenormalizeCodeLength(DecodeVarLengthUnsigned(CODE_LENGTH_ENCBASE))
numSafePoints = DecodeVarLengthUnsigned(NUM_SAFE_POINTS_ENCBASE)
numInterruptibleRanges = 0  // slim header never has interruptible ranges
```

All optional fields (GS cookie, PSP symbol, generics context, EnC info, reverse P/Invoke) default to their sentinel "not present" values.

**Fat Header** (first bit = 1):

The fat header contains a full flags bitfield and conditionally-present optional fields:

```
isSlimHeader = ReadBits(1)  // 1 = fat
headerFlags = ReadBits(GC_INFO_FLAGS_BIT_SIZE)  // 10 bits
codeLength = DenormalizeCodeLength(DecodeVarLengthUnsigned(CODE_LENGTH_ENCBASE))

// Prolog/epilog sizes (conditional on GS cookie or generics context)
if HAS_GS_COOKIE:
    normPrologSize = DecodeVarLengthUnsigned(NORM_PROLOG_SIZE_ENCBASE) + 1
    normEpilogSize = DecodeVarLengthUnsigned(NORM_EPILOG_SIZE_ENCBASE)
elif HAS_GENERICS_INST_CONTEXT:
    normPrologSize = DecodeVarLengthUnsigned(NORM_PROLOG_SIZE_ENCBASE) + 1

// Optional fields (each conditional on its header flag)
if HAS_GS_COOKIE:
    gsCookieStackSlot = DenormalizeStackSlot(DecodeVarLengthSigned(GS_COOKIE_STACK_SLOT_ENCBASE))
if HAS_GENERICS_INST_CONTEXT:
    genericsInstContextStackSlot = DenormalizeStackSlot(DecodeVarLengthSigned(...))
if HAS_STACK_BASE_REGISTER:
    stackBaseRegister = DenormalizeStackBaseRegister(DecodeVarLengthUnsigned(...))
if HAS_EDIT_AND_CONTINUE_INFO:
    sizeOfEnCPreservedArea = DecodeVarLengthUnsigned(...)
    if ARM64: sizeOfEnCFixedStackFrame = DecodeVarLengthUnsigned(...)
if REVERSE_PINVOKE_FRAME:
    reversePInvokeFrameStackSlot = DenormalizeStackSlot(DecodeVarLengthSigned(...))
if HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA:  // platform-dependent
    fixedStackParameterScratchArea = DenormalizeSizeOfStackArea(DecodeVarLengthUnsigned(...))

numSafePoints = DecodeVarLengthUnsigned(NUM_SAFE_POINTS_ENCBASE)
numInterruptibleRanges = DecodeVarLengthUnsigned(NUM_INTERRUPTIBLE_RANGES_ENCBASE)
```

#### Body Decoding

Following the header, the GCInfo body contains data sections that must be decoded in strict order:

##### 1. Safe Point Offsets

Safe points (also called call sites) are code offsets where the GC can safely interrupt execution for partially-interruptible methods. Each offset is encoded as a fixed-width bitfield:

```
numBitsPerOffset = CeilOfLog2(NormalizeCodeOffset(codeLength))
for each safe point:
    offset = ReadBits(numBitsPerOffset)  // normalized code offset
```

The offsets are stored in sorted order to enable binary search during `EnumerateLiveSlots`.

##### 2. Interruptible Ranges

Interruptible ranges define code regions where the method is **fully interruptible** — the GC can interrupt at any instruction within these ranges. Each range is encoded as a pair of delta-compressed, normalized offsets:

```
lastStopNormalized = 0

for each range:
    startDelta = DecodeVarLengthUnsigned(INTERRUPTIBLE_RANGE_DELTA1_ENCBASE)
    stopDelta  = DecodeVarLengthUnsigned(INTERRUPTIBLE_RANGE_DELTA2_ENCBASE) + 1

    startNormalized = lastStopNormalized + startDelta
    stopNormalized  = startNormalized + stopDelta

    startOffset = DenormalizeCodeOffset(startNormalized)
    stopOffset  = DenormalizeCodeOffset(stopNormalized)

    emit InterruptibleRange(startOffset, stopOffset)
    lastStopNormalized = stopNormalized
```

##### 3. Slot Table

The slot table describes all GC-tracked locations used by the method. It has three sections decoded in order: register slots, tracked stack slots, and untracked stack slots.

**Slot counts** are encoded with presence bits:

```
if ReadBits(1):  // has register slots
    numRegisters = DecodeVarLengthUnsigned(NUM_REGISTERS_ENCBASE)
if ReadBits(1):  // has stack/untracked slots
    numStackSlots = DecodeVarLengthUnsigned(NUM_STACK_SLOTS_ENCBASE)
    numUntrackedSlots = DecodeVarLengthUnsigned(NUM_UNTRACKED_SLOTS_ENCBASE)
```

**Register slots** use delta encoding when consecutive slots share the same flags:

```
// First slot: absolute register number + 2-bit flags
regNum = DecodeVarLengthUnsigned(REGISTER_ENCBASE)
flags = ReadBits(2)

// Subsequent slots:
if previousFlags != 0:
    regNum = DecodeVarLengthUnsigned(REGISTER_ENCBASE)  // absolute
    flags = ReadBits(2)
else:
    regNum += DecodeVarLengthUnsigned(REGISTER_DELTA_ENCBASE) + 1  // delta
    // flags inherited from previous
```

**Stack slots** follow a similar delta encoding pattern:

```
// First slot: base (2 bits) + normalized offset + flags (2 bits)
spBase = ReadBits(2)  // CALLER_SP_REL, SP_REL, or FRAMEREG_REL
normSpOffset = DecodeVarLengthSigned(STACK_SLOT_ENCBASE)
spOffset = DenormalizeStackSlot(normSpOffset)
flags = ReadBits(2)

// Subsequent slots:
spBase = ReadBits(2)
if previousFlags != 0:
    normSpOffset = DecodeVarLengthSigned(STACK_SLOT_ENCBASE)  // absolute
    flags = ReadBits(2)
else:
    normSpOffset += DecodeVarLengthUnsigned(STACK_SLOT_DELTA_ENCBASE)  // delta
    // flags inherited from previous
```

Untracked slots use the same encoding as tracked stack slots.

The 2-bit slot flags are:

| Flag | Value | Meaning |
| --- | --- | --- |
| `GC_SLOT_BASE` | 0x0 | Normal object reference |
| `GC_SLOT_INTERIOR` | 0x1 | Interior pointer (points inside an object) |
| `GC_SLOT_PINNED` | 0x2 | Pinned object reference |

##### 4. Live State Data

Following the slot table, the remaining bitstream contains per-safe-point and per-chunk liveness information used by `EnumerateLiveSlots` to determine which slots are live at a given instruction offset. This data uses either a direct 1-bit-per-slot encoding or RLE (run-length encoding) compression for methods with many tracked slots.

For **partially interruptible** methods (at safe points), each safe point has a bitvector indicating which tracked slots are live. An optional indirection table allows sharing identical bitvectors across safe points.

For **fully interruptible** methods (within interruptible ranges), the interruptible region is divided into fixed-size chunks (`NUM_NORM_CODE_OFFSETS_PER_CHUNK = 64` normalized offsets). Each chunk records a "could be live" bitvector, a final state bitvector, and transition points within the chunk where slot liveness changes.

### EnumerateLiveSlots

`EnumerateLiveSlots` determines which GC-tracked slots (registers and stack locations) are live at a given instruction offset, then reports each live slot via a callback. The algorithm handles two distinct cases depending on whether the instruction offset falls at a **safe point** (partially-interruptible) or within an **interruptible range** (fully-interruptible).

**Input**: instruction offset, `GcSlotEnumerationOptions`, slot report callback.

**Step 1 — Find safe point**: Search the safe point offset table for an exact match against the normalized instruction offset. If found, the safe point index is used for the partially-interruptible path.

**Step 2 — Partially-interruptible path** (safe point found, not `ExecutionAborted`):

Each safe point has a bitvector with one bit per tracked slot. If the bit is set, the slot is live. An optional **indirection table** allows sharing identical bitvectors across safe points — when present, each safe point stores an offset into a deduplicated bitvector table. The bitvectors may use either direct 1-bit-per-slot encoding or **RLE** (run-length encoding) for methods with many tracked slots.

**Step 3 — Fully-interruptible path** (no safe point match, offset is within an interruptible range):

The total interruptible length is computed by summing all interruptible range sizes. A **pseudo-offset** maps the instruction offset into this linear space. The interruptible region is divided into fixed-size **chunks** of 64 normalized offsets each.

For each chunk, the encoding stores:
- A **couldBeLive** bitvector identifying which slots may be live anywhere in the chunk (1-bit-per-slot or RLE).
- A **finalState** bit per couldBeLive slot indicating liveness at the end of the chunk.
- **Transition points** within the chunk where each slot's liveness toggles.

To determine liveness at the target offset: start from the chunk's final state, then apply any transitions that occur *after* the target offset (toggling the state backwards). A slot is live if its final state (after toggle adjustment) is 1.

**Step 4 — Report untracked slots**: Untracked slots are always live (they represent stack locations the JIT doesn't track at each safe point). They are reported unconditionally unless `ParentOfFuncletStackFrame` or `NoReportUntracked` flags are set. Untracked slots are reported with `reportScratchSlots=true` since the JIT may produce untracked scratch register slots for interior pointers.

**Slot filtering**: Before reporting any slot, the algorithm checks:
- **Scratch registers**: Only reported for the active/leaf frame (`ActiveStackFrame` flag).
- **Scratch stack slots**: Only reported for the active/leaf frame (slots in the outgoing/scratch area).
- **FP-based-only mode** (`ReportFPBasedSlotsOnly`): Only frame-register-relative stack slots are reported; all register slots and non-frame-relative stack slots are skipped.

#### API Implementations

All APIs use lazy decoding — the GCInfo bitstream is decoded up to the required point on first access, and cached for subsequent calls.

```csharp
uint GetCodeLength(IGCInfoHandle handle)
{
    // Ensure header is decoded, then return the code length field.
}

uint GetStackBaseRegister(IGCInfoHandle handle)
{
    // Ensure header is decoded through the stack base register field,
    // then return the denormalized register number (e.g., RBP on x64).
}

IReadOnlyList<InterruptibleRange> GetInterruptibleRanges(IGCInfoHandle handle)
{
    // Ensure header and body are decoded through interruptible ranges,
    // then return the decoded range list.
}

IReadOnlyList<LiveSlot> EnumerateLiveSlots(IGCInfoHandle handle,
    uint instructionOffset, GcSlotEnumerationOptions options)
{
    // Ensure header, body, and slot table are fully decoded.
    // Then execute the EnumerateLiveSlots algorithm described above:
    //   1. Find safe point match for the normalized instruction offset
    //   2. If found: read the per-safe-point bitvector (partially-interruptible path)
    //   3. If not found: compute pseudo-offset into interruptible ranges,
    //      locate the chunk, read couldBeLive/finalState/transitions
    //      (fully-interruptible path)
    //   4. Report untracked slots unconditionally (unless SuppressUntrackedSlots)
    //   5. Apply slot filtering (scratch registers, FP-based-only mode)
    // Collect each live slot into a list and return it.
}
```
