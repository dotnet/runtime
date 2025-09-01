# Contract DebugInfo

This contract is for fetching information related to DebugInfo associated with native code.

## APIs of contract

```csharp
[Flags]
public enum SourceTypes : uint
{
    SourceTypeInvalid = 0x00, // To indicate that nothing else applies
    StackEmpty = 0x01, // The stack is empty here
    CallInstruction = 0x02  // The actual instruction of a call.
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
// Given a code pointer, return the associated native/IL offset mapping and codeOffset.
// If preferUninstrumented, will always read the uninstrumented bounds.
// Otherwise will read the instrumented bounds and fallback to the uninstrumented bounds.
IEnumerable<OffsetMapping> GetMethodNativeMap(TargetCodePointer pCode, bool preferUninstrumented, out uint codeOffset);
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
| DEBUG_INFO_BOUNDS_HAS_INSTRUMENTED_BOUNDS | Indicates bounds data contains instrumented bounds | `0xFFFFFFFF` |
| EXTRA_DEBUG_INFO_PATCHPOINT | Indicates debug info contains patchpoint information | 0x1 |
| EXTRA_DEBUG_INFO_RICH | Indicates debug info contains rich information | 0x2 |

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

### Bounds Data Encoding (R2R Major Version 16+)

For R2R major version 16 and above, the bounds data uses a bit-packed encoding algorithm:

1. The bounds entry count, bits needed for native deltas, and bits needed for IL offsets are encoded using the nibble scheme above
2. Each bounds entry is then bit-packed with:
   - 2 bits for source type (SourceTypeInvalid=0, CallInstruction=1, StackEmpty=2, StackEmpty|CallInstruction=3)
   - Variable bits for native offset delta (accumulated from previous offset)
   - Variable bits for IL offset (with IL_OFFSET_BIAS applied)

The bit-packed data is read byte by byte, collecting bits until enough are available for each entry.

### Implementation

``` csharp
IEnumerable<OffsetMapping> IDebugInfo.GetMethodNativeMap(TargetCodePointer pCode, bool preferUninstrumented, out uint codeOffset)
{
    // Get the method's DebugInfo
    if (_eman.GetCodeBlockHandle(pCode) is not CodeBlockHandle cbh)
        throw new InvalidOperationException($"No CodeBlockHandle found for native code {pCode}.");
    TargetPointer debugInfo = _eman.GetDebugInfo(cbh, out bool hasFlagByte);

    TargetCodePointer nativeCodeStart = _eman.GetStartAddress(cbh);
    codeOffset = (uint)(CodePointerUtils.AddressFromCodePointer(pCode, _target) - CodePointerUtils.AddressFromCodePointer(nativeCodeStart, _target));

    return RestoreBoundaries(debugInfo, hasFlagByte, preferUninstrumented);
}

private IEnumerable<OffsetMapping> RestoreBoundaries(TargetPointer debugInfo, bool hasFlagByte, bool preferUninstrumented)
{
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

    NativeReader nibbleNativeReader = new(new TargetStream(_target, debugInfo, 24 /*maximum size of 4 32bit ints compressed*/), _target.IsLittleEndian);
    NibbleReader nibbleReader = new(nibbleNativeReader, 0);

    uint cbBounds = nibbleReader.ReadUInt();
    uint cbUninstrumentedBounds = 0;
    if (cbBounds == DEBUG_INFO_BOUNDS_HAS_INSTRUMENTED_BOUNDS)
    {
        // This means we have instrumented bounds.
        cbBounds = nibbleReader.ReadUInt();
        cbUninstrumentedBounds = nibbleReader.ReadUInt();
    }
    uint _ /*cbVars*/ = nibbleReader.ReadUInt();

    TargetPointer addrBounds = debugInfo + (uint)nibbleReader.GetNextByteOffset();
    // TargetPointer addrVars = addrBounds + cbBounds + cbUninstrumentedBounds;

    if (preferUninstrumented && cbUninstrumentedBounds != 0)
    {
        // If we have uninstrumented bounds, we will use them instead of the regular bounds.
        addrBounds += cbBounds;
        cbBounds = cbUninstrumentedBounds;
    }

    if (cbBounds > 0)
    {
        NativeReader boundsNativeReader = new(new TargetStream(_target, addrBounds, cbBounds), _target.IsLittleEndian);
        return DoBounds(boundsNativeReader);
    }

    return Enumerable.Empty<OffsetMapping>();
}

private static IEnumerable<OffsetMapping> DoBounds(NativeReader nativeReader)
{
    NibbleReader reader = new(nativeReader, 0);

    uint boundsEntryCount = reader.ReadUInt();

    uint bitsForNativeDelta = reader.ReadUInt() + 1; // Number of bits needed for native deltas
    uint bitsForILOffsets = reader.ReadUInt() + 1; // Number of bits needed for IL offsets

    uint bitsPerEntry = bitsForNativeDelta + bitsForILOffsets + 2; // 2 bits for source type
    ulong bitsMeaningfulMask = (1UL << ((int)bitsPerEntry)) - 1;
    int offsetOfActualBoundsData = reader.GetNextByteOffset();

    uint bitsCollected = 0;
    ulong bitTemp = 0;
    uint curBoundsProcessed = 0;

    uint previousNativeOffset = 0;

    while (curBoundsProcessed < boundsEntryCount)
    {
        bitTemp |= ((uint)nativeReader[offsetOfActualBoundsData++]) << (int)bitsCollected;
        bitsCollected += 8;
        while (bitsCollected >= bitsPerEntry)
        {
            ulong mappingDataEncoded = bitsMeaningfulMask & bitTemp;
            bitTemp >>= (int)bitsPerEntry;
            bitsCollected -= bitsPerEntry;

            SourceTypes sourceType = (mappingDataEncoded & 0x3) switch
            {
                0 => SourceTypes.SourceTypeInvalid,
                1 => SourceTypes.CallInstruction,
                2 => SourceTypes.StackEmpty,
                3 => SourceTypes.StackEmpty | SourceTypes.CallInstruction,
                _ => throw new InvalidOperationException($"Unknown source type encoding: {mappingDataEncoded & 0x3}")
            };

            mappingDataEncoded >>= 2;
            uint nativeOffsetDelta = (uint)(mappingDataEncoded & ((1UL << (int)bitsForNativeDelta) - 1));
            previousNativeOffset += nativeOffsetDelta;
            uint nativeOffset = previousNativeOffset;

            mappingDataEncoded >>= (int)bitsForNativeDelta;
            uint ilOffset = (uint)mappingDataEncoded + IL_OFFSET_BIAS;

            yield return new OffsetMapping()
            {
                NativeOffset = nativeOffset,
                ILOffset = ilOffset,
                SourceType = sourceType
            };
            curBoundsProcessed++;
        }
    }
}
```
