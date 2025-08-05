# Contract Object

This contract is for getting information about well-known managed objects

## APIs of contract

``` csharp
// Get the method table address for the object
TargetPointer GetMethodTableAddress(TargetPointer address);

// Get the string corresponding to a managed string object. Error if address does not represent a string.
string GetStringValue(TargetPointer address);

// Get the pointer to the data corresponding to a managed array object. Error if address does not represent a array.
TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);

// Get built-in COM data for the object if available. Returns false, if address does not represent a COM object using built-in COM
bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Array` | `m_NumComponents` | Number of items in the array |
| `InteropSyncBlockInfo` | `RCW` | Pointer to the RCW for the object (if it exists) |
| `InteropSyncBlockInfo` | `CCW` | Pointer to the CCW for the object (if it exists) |
| `Object` | `m_pMethTab` | Method table for the object |
| `String` | `m_FirstChar` | First character of the string - `m_StringLength` can be used to read the full string (encoded in UTF-16) |
| `String` | `m_StringLength` | Length of the string in characters (encoded in UTF-16) |
| `SyncBlock` | `InteropInfo` | Optional `InteropSyncBlockInfo` for the sync block |
| `SyncTableEntry` | `SyncBlock` | `SyncBlock` corresponding to the entry |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `ArrayBoundsZero` | TargetPointer | Known value for single dimensional, zero-lower-bound array |
| `ObjectHeaderSize` | uint32 | Size of the object header (sync block and alignment) |
| `ObjectToMethodTableUnmask` | uint8 | Bits to clear for converting to a method table address |
| `StringMethodTable` | TargetPointer | The method table for System.String |
| `SyncTableEntries` | TargetPointer | The `SyncTableEntry` list |
| `SyncBlockValueToObjectOffset` | uint16 | Offset from the sync block value (in the object header) to the object itself |

Contracts used:
| Contract Name |
| --- |
| `RuntimeTypeSystem` |

``` csharp
TargetPointer GetMethodTableAddress(TargetPointer address)
{
    TargetPointer mt = target.ReadPointer(address + /* Object::m_pMethTab offset */);
    return mt.Value & ~target.ReadGlobal<byte>("ObjectToMethodTableUnmask");
}

string GetStringValue(TargetPointer address)
{
    TargetPointer mt = GetMethodTableAddress(address);
    TargetPointer stringMethodTable = target.ReadPointer(target.ReadGlobalPointer("StringMethodTable"));
    if (mt != stringMethodTable)
        throw new ArgumentException("Address does not represent a string object", nameof(address));

    uint length = target.Read<uint>(address + /* String::m_StringLength offset */);
    Span<byte> span = stackalloc byte[(int)length * sizeof(char)];
    target.ReadBuffer(address + /* String::m_FirstChar offset */, span);
    return new string(MemoryMarshal.Cast<byte, char>(span));
}

TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds)
{
    TargetPointer mt = GetMethodTableAddress(address);
    Contracts.IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
    TypeHandle typeHandle = rts.GetTypeHandle(mt);
    uint rank;
    if (!rts.IsArray(typeHandle, out rank))
        throw new ArgumentException("Address does not represent an array object", nameof(address));

    count = target.Read<uint>(address + /* Array::m_NumComponents offset */;

    CorElementType corType = rts.GetSignatureCorElementType(typeHandle);
    if (corType == CorElementType.Array)
    {
        // Multi-dimensional - has bounds as part of the array object
        // The object is allocated with:
        //   << fields that are part of the array type info >>
        //   int32_t bounds[rank];
        //   int32_t lowerBounds[rank];
        boundsStart = address + /* Array size */;
        lowerBounds = boundsStart + (rank * sizeof(int));
    }
    else
    {
        // Single-dimensional, zero-based - doesn't have bounds
        boundsStart = address + /* Array::m_NumComponents offset */;
        lowerBounds = _target.ReadGlobalPointer("ArrayBoundsZero");
    }

    // Sync block is before `this` pointer, so substract the object header size
    ulong dataOffset = typeSystemContract.GetBaseSize(typeHandle) - _target.ReadGlobal<uint>("ObjectHeaderSize");
    return address + dataOffset;
}

bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw);
{
    uint syncBlockValue = target.Read<uint>(address - _target.ReadGlobal<ushort>("SyncBlockValueToObjectOffset"));

    // Check if the sync block value represents a sync block index
    if ((syncBlockValue & (uint)(SyncBlockValue.Bits.IsHashCodeOrSyncBlockIndex | SyncBlockValue.Bits.IsHashCode)) != (uint)SyncBlockValue.Bits.IsHashCodeOrSyncBlockIndex)
        return false;

    // Get the offset into the sync table entries
    uint index = syncBlockValue & SyncBlockValue.SyncBlockIndexMask;
    ulong offsetInSyncTableEntries = index * /* SyncTableEntry size */;

    TargetPointer syncBlock = target.ReadPointer(_syncTableEntries + offsetInSyncTableEntries + /* SyncTableEntry::SyncBlock offset */);
    if (syncBlock == TargetPointer.Null)
        return false;

    TargetPointer interopInfo = target.ReadPointer(syncBlock + /* SyncTableEntry::InteropInfo offset */);
    if (interopInfo == TargetPointer.Null)
        return false;

    rcw = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::RCW offset */);
    ccw = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::CCW offset */);
    return rcw != TargetPointer.Null && ccw != TargetPointer.Null;
}
```
