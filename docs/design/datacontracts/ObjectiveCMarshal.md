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

<!-- BEGIN GENERATED: usage contract=ObjectiveCMarshal version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal+ObjcTrackingInformation` | `_memory` | `pointer` | Pointer to the tagged memory block associated with the tracked Objective-C object |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `ObjectiveCMarshal.s_objects` | `pointer` | Address of ObjectiveCMarshal.s_objects, the ConditionalWeakTable mapping tracked objects to tracking information |
| `System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal.s_objects` | `pointer` | Address of ObjectiveCMarshal.s_objects, the ConditionalWeakTable mapping tracked objects to tracking information |

### Contracts used

| Contract Name |
| --- |
| `ConditionalWeakTable` |
<!-- END GENERATED: usage contract=ObjectiveCMarshal version=c1 -->

### Contract Constants

| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `TaggedMemorySizeInPointers` | `int` | Number of target pointer-sized elements in the tagged memory block | `2` |

``` csharp
const int TaggedMemorySizeInPointers = 2;

TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size)
{
    size = default;

    TargetPointer objectsTable = target.ReadPointer(
        /* System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal.s_objects static field address */);
    if (objectsTable == TargetPointer.Null)
        return TargetPointer.Null;

    if (target.Contracts.ConditionalWeakTable.TryGetValue(
        objectsTable,
        address,
        out TargetPointer trackingInfoAddress))
    {
        TargetPointer taggedMemory = target.ReadPointer(
            trackingInfoAddress +
            /* Object data offset */ +
            /* System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal+ObjcTrackingInformation::_memory offset */);
        if (taggedMemory != TargetPointer.Null)
        {
            size = new TargetNUInt(TaggedMemorySizeInPointers * (ulong)target.PointerSize);
            return taggedMemory;
        }
    }

    return TargetPointer.Null;
}
```
