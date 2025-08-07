# Contract ComWrappers

This contract is for getting information related to COM wrappers.

## APIs of contract

``` csharp
// Get the address of the external COM object
TargetPointer GetComWrappersRCWIdentity(TargetPointer rcw);
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
| `NativeObjectWrapperObject` | `ExternalComObject` | Address of the external COM object |

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
string StringFromEEAddress(TargetPointer address)
{
    TargetPointer miniMetaDataBuffAddress = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffAddress));
    uint miniMetaDataBuffMaxSize = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffMaxSize));

    // Parse MiniMetadataStream according the the format described above to produce a dictionary from pointer to string from the EENameStream.
    // Then lookup in the dictionary, to produce a result if it was present in the table.
    // In general, since this api is intended for fallback scenarios, implementations of this api should attempt
    // to return null instead of producing errors.
    // Since in normal execution of the runtime no stream is constructed, it is normal when examining full dumps and live process state without a stream encoded.
}
```
