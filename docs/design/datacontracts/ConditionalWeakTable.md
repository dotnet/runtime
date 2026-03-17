# Contract ConditionalWeakTable

This contract provides the ability to look up values in a `ConditionalWeakTable<TKey, TValue>` managed object by key identity.

## APIs of contract

``` csharp
// Try to find the value associated with the given key in the conditional weak table.
// Returns true and sets value if found, false otherwise.
bool TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ConditionalWeakTableObject` | `Container` | Pointer to the active `ConditionalWeakTableContainerObject` |
| `ConditionalWeakTableContainerObject` | `Buckets` | Pointer to the `int[]` buckets array |
| `ConditionalWeakTableContainerObject` | `Entries` | Pointer to the `Entry[]` entries array |
| `ConditionalWeakTableEntry` | `HashCode` | Hash code of the entry (masked to positive int) |
| `ConditionalWeakTableEntry` | `Next` | Index of the next entry in the chain, or -1 |
| `ConditionalWeakTableEntry` | `DepHnd` | Dependent handle; key is the handle target, value is the extra info |
| `Array` | `m_NumComponents` | Number of elements in the array |

Contracts used:
| Contract Name |
| --- |
| `Object` |
| `GC` |

``` csharp
bool TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value)
{
    value = TargetPointer.Null;

    // Navigate from the ConditionalWeakTableObject to its container
    TargetPointer container = target.ReadPointer(conditionalWeakTable + /* ConditionalWeakTableObject::Container offset */);

    // Read the container's buckets and entries array pointers
    TargetPointer bucketsPtr = target.ReadPointer(container + /* ConditionalWeakTableContainerObject::Buckets offset */);
    TargetPointer entriesPtr = target.ReadPointer(container + /* ConditionalWeakTableContainerObject::Entries offset */);

    // Get the hash code for the key object (returns 0 if none assigned)
    int hashCode = target.Contracts.Object.TryGetHashCode(key);
    if (hashCode == 0)
        return false;

    hashCode &= int.MaxValue;

    // Read the buckets array length and data
    uint bucketCount = target.Read<uint>(bucketsPtr + /* Array::m_NumComponents offset */);
    TargetPointer bucketsData = bucketsPtr + /* Array header size */;

    // Find the bucket for this hash code (bucketCount is a power of 2)
    int bucket = hashCode & (int)(bucketCount - 1);
    int entriesIndex = target.Read<int>(bucketsData + bucket * sizeof(int));

    // Walk the chain
    uint entrySize = /* ConditionalWeakTableEntry size */;
    TargetPointer entriesData = entriesPtr + /* Array header size */;

    while (entriesIndex != -1)
    {
        TargetPointer entryAddr = entriesData + (uint)entriesIndex * entrySize;
        int entryHashCode = target.Read<int>(entryAddr + /* ConditionalWeakTableEntry::HashCode offset */);
        int entryNext = target.Read<int>(entryAddr + /* ConditionalWeakTableEntry::Next offset */);
        TargetPointer depHnd = target.ReadPointer(entryAddr + /* ConditionalWeakTableEntry::DepHnd offset */);

        if (entryHashCode == hashCode)
        {
            // Check if the handle's target matches the key
            TargetPointer handleTarget = target.ReadPointer(depHnd); // ObjectFromHandle
            if (handleTarget == key)
            {
                // The value is the handle's extra info (secondary dependent handle value)
                value = target.Contracts.GC.GetHandleExtraInfo(depHnd);
                return true;
            }
        }

        entriesIndex = entryNext;
    }

    return false;
}
```
