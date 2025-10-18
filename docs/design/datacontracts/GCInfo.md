# Contract GCInfo

This contract is for fetching information related to GCInfo associated with native code. Currently, this contract does not support x86 architecture.

## APIs of contract

```csharp
public interface IGCInfoHandle { }
```

```csharp
// Decodes GCInfo with a given address and version
IGCInfoHandle DecodeGCInfo(TargetPointer gcInfoAddress, uint gcVersion);

/* Methods to query information from the GCInfo */

// Fetches length of code as reported in GCInfo
uint GetCodeLength(IGCInfoHandle handle);
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


## Implementation

The GCInfo contract has platform specific implementations as GCInfo differs per architecture. With the exception of x86, all platforms have a common encoding scheme with different encoding lengths and normalization functions for data. x86 uses an entirely different scheme which is not currently supported by this contract.

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
| **Code Length** | `length << 2` | `length >> 2` |
| **Code Offset** | `offset << 2` | `offset >> 2` |
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

The GCInfo contract implementation follows this process:

```csharp
IGCInfoHandle DecodeGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
{
    // Create a new decoder instance for the specified platform traits
    return new GcInfoDecoder<PlatformTraits>(target, gcInfoAddress, gcVersion);
}

uint GetCodeLength(IGCInfoHandle handle)
{
    // Cast to the appropriate decoder type and return the decoded code length
    GcInfoDecoder<PlatformTraits> decoder = (GcInfoDecoder<PlatformTraits>)handle;
    return decoder.GetCodeLength();
}
```

The decoder reads and parses the GCInfo data structure sequentially, using the platform-specific encoding bases and normalization rules to reconstruct the original method metadata.
