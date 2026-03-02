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

// Get built-in COM data for the object if available. Returns false if address does not represent a COM object using built-in COM.
bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Array` | `m_NumComponents` | Number of items in the array |
| `Object` | `m_pMethTab` | Method table for the object |
| `String` | `m_FirstChar` | First character of the string - `m_StringLength` can be used to read the full string (encoded in UTF-16) |
| `String` | `m_StringLength` | Length of the string in characters (encoded in UTF-16) |
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
| `SyncBlockIsHashOrSyncBlockIndex` | uint32 | Check bit indicating that the sync block value represents either a hash code or a sync block index rather than a thin-lock state. |
| `SyncBlockIsHashCode` | uint32 | Check bit that, when `SyncBlockIsHashOrSyncBlockIndex` is set, specifies that the remaining bits hold the hash code; when clear, the remaining bits hold the sync block index. |
| `SyncBlockIndexMask` | uint32 | The mask for sync block index field. |

Contracts used:
| Contract Name |
| --- |
| `RuntimeTypeSystem` |
| `SyncBlock` |

``` csharp
TargetPointer GetMethodTableAddress(TargetPointer address)
{
    TargetPointer mt = target.ReadPointer(address + /* Object::m_pMethTab offset */);
    return mt.Value & ~target.ReadGlobal<byte>("ObjectToMethodTableUnmask");
}

string GetStringValue(TargetPointer address)
{
    TargetPointer mt = GetMethodTableAddress(address);
    if (mt == TargetPointer.Null)
        throw new ArgumentException("Address represents a set-free object");
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
    if (mt == TargetPointer.Null)
        throw new ArgumentException("Address represents a set-free object");
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
        lowerBounds = target.ReadGlobalPointer("ArrayBoundsZero");
    }

    // Sync block is before `this` pointer, so substract the object header size
    ulong dataOffset = typeSystemContract.GetBaseSize(typeHandle) - target.ReadGlobal<uint>("ObjectHeaderSize");
    return address + dataOffset;
}

bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf)
{
    rcw = TargetPointer.Null;
    ccw = TargetPointer.Null;
    ccf = TargetPointer.Null;

    uint syncBlockValue = target.Read<uint>(address - target.ReadGlobal<ushort>("SyncBlockValueToObjectOffset"));

    // Check if the sync block value represents a sync block index
    if ((syncBlockValue & (target.ReadGlobal<uint>("SyncBlockIsHashCode") | target.ReadGlobal<uint>("SyncBlockIsHashOrSyncBlockIndex")))
            != target.ReadGlobal<uint>("SyncBlockIsHashOrSyncBlockIndex"))
        return false;

    uint index = syncBlockValue & target.ReadGlobal<uint>("SyncBlockIndexMask");
    ulong offsetInSyncTableEntries = index * /* SyncTableEntry size */;

    TargetPointer syncBlockPtr = target.ReadPointer(_syncTableEntries + offsetInSyncTableEntries + /* SyncTableEntry::SyncBlock offset */);
    if (syncBlockPtr == TargetPointer.Null)
        return false;

    // Delegate to the SyncBlock contract so that the interop data can also be read directly
    // from a sync block address without going through the object (e.g. during cleanup).
    return target.Contracts.SyncBlock.GetBuiltInComData(syncBlockPtr, out rcw, out ccw, out ccf);
}
```
