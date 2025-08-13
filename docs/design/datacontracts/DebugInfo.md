# Contract DebugInfo

This contract is for fetching information related to DebugInfo associated with native code.

## APIs of contract

```csharp
public enum SourceTypes : uint
{
    SourceTypeInvalid = 0x00, // To indicate that nothing else applies
    SequencePoint = 0x01, // The debugger asked for it.
    StackEmpty = 0x02, // The stack is empty here
    CallSite = 0x04, // This is a call site.
    NativeEndOffsetUnknown = 0x08, // Indicates a epilog endpoint
    CallInstruction = 0x10  // The actual instruction of a call.
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
// Given a code pointer, return the associated native/IL offset mapping
IEnumerable<OffsetMapping> GetMethodNativeMap(TargetCodePointer pCode, out uint codeOffset);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `PatchpointInfo` | `LocalCount` | Number of locals in the method associated with the patchpoint. |

Contracts used:
| Contract Name |
| --- |
| `CodeVersions` |
| `ExecutionManager` |

Constants:
| Constant Name | Meaning | Value |
| --- | --- | --- |
| IL_OFFSET_BIAS | IL offsets are encoded in the DebugInfo with this bias. | `0xfffffffd` (-3) |
| EXTRA_DEBUG_INFO_PATCHPOINT | Indicates debug info contains patchpoint information | 0x1 |
| EXTRA_DEBUG_INFO_RICH | Indicates debug info contains rich information | 0x2 |
| SourceTypeInvalid | To indicate that nothing else applies | 0x00 |
| SourceTypeSequencePoint | Source type indicating the debugger asked for it | 0x01 |
| SourceTypeStackEmpty | Source type indicating the stack is empty here | 0x02 |
| SourceTypeCallSite | Source type indicating This is a call site | 0x04 |
| SourceTypeNativeEndOffsetUnknown | Source type indicating a epilog endpoint | 0x08 |
| SourceTypeCallInstruction | Source type indicating the actual instruction of a call | 0x10 |

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

Based on the encoding specification, we use a decoder defined original for r2r dump `NibbleReader.cs`

### Implementation

``` csharp
IEnumerable<OffsetMapping> IDebugInfo.GetMethodNativeMap(TargetCodePointer pCode, out uint codeOffset)
{
    // Get the method's DebugInfo
    if (/*ExecutionManager*/.GetCodeBlockHandle(pCode) is not CodeBlockHandle cbh)
        throw NotValid // pCode must point to a valid code block
    TargetPointer debugInfo = /*ExecutionManager*/.GetDebugInfo(cbh, out bool hasFlagByte);

    TargetCodePointer nativeCodeStart = /*ExecutionManager*/.GetStartAddress(cbh);

    TargetPointer startAddress = /*convert nativeCodeStart to a TargetPointer*/
    TargetPointer currAddress = /*convert pCode to a TargetPointer*/
    codeOffset = currAddress - startAddress;

    if (hasFlagByte)
    {
        // Check flag byte and skip over any patchpoint info
        byte flagByte = _target.Read<byte>(debugInfo++);

        if ((flagByte & EXTRA_DEBUG_INFO_PATCHPOINT) != 0)
        {
            uint localCount = _target.Read<uint>(debugInfo + /*PatchpointInfo::LocalCount offset*/)
            debugInfo += /*size of PatchpointInfo*/ + (localCount * 4);
        }

        if ((flagByte & EXTRA_DEBUG_INFO_RICH) != 0)
        {
            uint richDebugInfoSize = _target.Read<uint>(debugInfo);
            debugInfo += 4;
            debugInfo += richDebugInfoSize;
        }
    }

    DebugStreamReader reader = new(debugInfo, 12 /*maximum size of 2 32bit ints compressed*/);

    uint cbBounds = reader.ReadEncodedU32();
    uint _ /*cbVars*/ = reader.ReadEncodedU32();

    TargetPointer addrBounds = debugInfo + reader.NextByteIndex;

    if (cbBounds == 0)
        // No bounds data was found, return an empty enumerable
        return Enumerable.Empty<IOffsetMapping>();

    // Create a DebugInfo stream decoder with the start address and size of bounds data
    DebugStreamReader boundsReader = new(addrBounds, cbBounds);

    uint countEntries = boundsReader.ReadEncodedU32();
    uint nativeOffset = 0;
    for (uint i = 0; i < count; i++)
    {
        // native offsets are encoded as a delta from the previous offset
        nativeOffset += boundsReader.ReadEncodedU32();

        // il offsets are encoded with a bias of IL_OFFSET_BIAS
        uint ilOffset = unchecked(boundsReader.ReadEncodedU32() + IL_OFFSET_BIAS);

        uint sourceType = boundsReader.ReadEncodedU32();

        yield return new OffsetMapping_1
        {
            NativeOffset = nativeOffset,
            ILOffset = ilOffset,
            InternalSourceType = sourceType
        };
    }
}
```
