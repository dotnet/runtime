# Contract ObjectiveCMarshal

This contract is for getting information related to Objective-C interop marshaling.

## APIs of contract

``` csharp
// Get the tagged memory for an Objective-C tracked reference object.
// Returns TargetPointer.Null if the object does not have tagged memory.
// On success, size is set to the size of the tagged memory in bytes; otherwise size is set to default.
TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `SyncBlock` | `InteropInfo` | Pointer to interop info (RCW, tagged memory, etc) |
| `InteropSyncBlockInfo` | `TaggedMemory` | Pointer to the tagged memory for the object (if it exists) |

Contracts used:
| Contract Name |
| --- |
| `Object` |

``` csharp
TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size)
{
    size = default;

    TargetPointer syncBlockPtr = target.Contracts.Object.GetSyncBlockAddress(address);
    if (syncBlockPtr == TargetPointer.Null)
        return TargetPointer.Null;

    TargetPointer interopInfoPtr = target.ReadPointer(syncBlockPtr + /* SyncBlock::InteropInfo offset */);
    if (interopInfoPtr == TargetPointer.Null)
        return TargetPointer.Null;

    TargetPointer taggedMemory = target.ReadPointer(interopInfoPtr + /* InteropSyncBlockInfo::TaggedMemory offset */);
    if (taggedMemory != TargetPointer.Null)
        size = new TargetNUInt(2 * target.PointerSize);
    return taggedMemory;
}
```
