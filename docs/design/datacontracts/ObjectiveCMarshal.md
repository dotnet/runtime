# Contract ObjectiveCMarshal

This contract is for getting information related to Objective-C interop marshaling.

## APIs of contract

``` csharp
// Get the tagged memory for an Objective-C tracked reference object.
// Returns TargetPointer.Null if the object does not have tagged memory.
// On success, size is set to the size of the tagged memory in bytes.
TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `InteropSyncBlockInfo` | `TaggedMemory` | Pointer to the tagged memory for the object (if it exists) |
| `Object` | `m_pMethTab` | Method table for the object |
| `ObjectHeader` | `SyncBlockValue` | Sync block value for the object header |
| `SyncTableEntry` | `SyncBlock` | Pointer to the sync block for the entry |

Globals used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `SyncTableEntries` | pointer | Pointer to the sync table entries array |
| `SyncBlockIsHashOrSyncBlockIndex` | uint32 | Bitmask: set if sync block value is a hash code or sync block index |
| `SyncBlockIsHashCode` | uint32 | Bitmask: set if sync block value is a hash code |
| `SyncBlockIndexMask` | uint32 | Mask to extract the sync block index from the sync block value |

``` csharp
TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size)
{
    size = new TargetNUInt(2 * target.PointerSize);

    ulong objectHeaderSize = /* ObjectHeader size */;
    uint syncBlockValue = target.Read<uint>(address - objectHeaderSize + /* ObjectHeader::SyncBlockValue offset */);

    // Check if the sync block value represents a sync block index
    if ((syncBlockValue & (SyncBlockIsHashCode | SyncBlockIsHashOrSyncBlockIndex))
            != SyncBlockIsHashOrSyncBlockIndex)
        return TargetPointer.Null;

    uint index = syncBlockValue & SyncBlockIndexMask;
    TargetPointer syncTableEntry = SyncTableEntries + index * /* SyncTableEntry size */;
    TargetPointer syncBlockPtr = target.ReadPointer(syncTableEntry + /* SyncTableEntry::SyncBlock offset */);
    if (syncBlockPtr == TargetPointer.Null)
        return TargetPointer.Null;

    TargetPointer interopInfoPtr = target.ReadPointer(syncBlockPtr + /* SyncBlock::InteropInfo offset */);
    if (interopInfoPtr == TargetPointer.Null)
        return TargetPointer.Null;

    return target.ReadPointer(interopInfoPtr + /* InteropSyncBlockInfo::TaggedMemory offset */);
}
```
