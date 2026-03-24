# Contract ObjectiveCMarshal

This contract is for getting information related to Objective-C interop marshaling.

## APIs of contract

``` csharp
// Get the tagged memory for an Objective-C tracked reference object.
// Returns false if the object does not have tagged memory.
// On success, size is set to the size of the tagged memory in bytes; otherwise size is set to default.
bool TryGetTaggedMemory(TargetPointer address, out TargetNUInt size, out TargetPointer taggedMemory);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `InteropSyncBlockInfo` | `TaggedMemory` | Pointer to the tagged memory for the object (if it exists) |
| `Object` | `m_pMethTab` | Method table for the object |
| `ObjectHeader` | `SyncBlockValue` | Sync block value for the object header |

Contracts used:
| Contract Name |
| --- |
| `SyncBlock` |

Globals used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `SyncBlockIsHashOrSyncBlockIndex` | uint32 | Bitmask: set if sync block value is a hash code or sync block index |
| `SyncBlockIsHashCode` | uint32 | Bitmask: set if sync block value is a hash code |
| `SyncBlockIndexMask` | uint32 | Mask to extract the sync block index from the sync block value |

``` csharp
bool TryGetTaggedMemory(TargetPointer address, out TargetNUInt size, out TargetPointer taggedMemory)
{
    size = default;
    taggedMemory = TargetPointer.Null;

    ulong objectHeaderSize = /* ObjectHeader size */;
    uint syncBlockValue = target.Read<uint>(address - objectHeaderSize + /* ObjectHeader::SyncBlockValue offset */);

    // Check if the sync block value represents a sync block index
    if ((syncBlockValue & (SyncBlockIsHashCode | SyncBlockIsHashOrSyncBlockIndex))
            != SyncBlockIsHashOrSyncBlockIndex)
        return false;

    uint index = syncBlockValue & SyncBlockIndexMask;
    TargetPointer syncBlockPtr = target.Contracts.SyncBlock.GetSyncBlock(index);

    TargetPointer interopInfoPtr = target.ReadPointer(syncBlockPtr + /* SyncBlock::InteropInfo offset */);
    if (interopInfoPtr == TargetPointer.Null)
        return false;

    taggedMemory = target.ReadPointer(interopInfoPtr + /* InteropSyncBlockInfo::TaggedMemory offset */);
    if (taggedMemory != TargetPointer.Null)
        size = new TargetNUInt(2 * target.PointerSize);
    return taggedMemory != TargetPointer.Null;
}
```
