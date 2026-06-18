# Contract ConditionalWeakTable

This contract provides the ability to look up values in a `ConditionalWeakTable<TKey, TValue>` managed object by key identity.

## APIs of contract

``` csharp
// Try to find the value associated with the given key in the conditional weak table.
// Returns true and sets value if found, false otherwise.
bool TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value);
```

## Version 1

This contract reads the field layout of `ConditionalWeakTable<TKey, TValue>` and its nested types
(`Container`, `Container+Entry`) via the [`ManagedTypeSource`](ManagedTypeSource.md) contract rather
than cDAC data descriptors. Field offsets are resolved by name at runtime.

### Data descriptors used

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Array` | `m_NumComponents` | Number of elements in the array |

### Managed types used

| Fully-qualified name | Module | Members read | Purpose |
| --- | --- | --- | --- |
| ``System.Runtime.CompilerServices.ConditionalWeakTable`2`` | `System.Private.CoreLib` | `_container` | Pointer to the active container |
| ``System.Runtime.CompilerServices.ConditionalWeakTable`2+Container`` | `System.Private.CoreLib` | `_buckets`, `_entries` | `int[]` bucket map and `Entry[]` storage |
| ``System.Runtime.CompilerServices.ConditionalWeakTable`2+Entry`` | `System.Private.CoreLib` | `HashCode`, `Next`, `depHnd` | Hash code, next-in-chain index, and dependent handle for each entry |

### Contracts used

| Contract Name |
| --- |
| `Object` |
| `GC` |
| `ManagedTypeSource` |
| `RuntimeTypeSystem` |

The algorithm looks up the `_container` field of the `ConditionalWeakTable` object, then reads the
`_buckets` and `_entries` fields from the container. It resolves `Entry` field offsets (`HashCode`,
`Next`, `depHnd`) via `ManagedTypeSource` and determines the entry stride from the entries array's
component size.

``` csharp
bool TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value)
{
    value = TargetPointer.Null;

    // Resolve field offsets by name via ManagedTypeSource.
    IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
    IManagedTypeSource mts = target.Contracts.ManagedTypeSource;
    Target.TypeInfo cwtType = mts.GetTypeInfo("System.Runtime.CompilerServices.ConditionalWeakTable`2");
    Target.TypeInfo containerType = mts.GetTypeInfo("System.Runtime.CompilerServices.ConditionalWeakTable`2+Container");
    Target.TypeInfo entryType = mts.GetTypeInfo("System.Runtime.CompilerServices.ConditionalWeakTable`2+Entry");

    uint containerOffset = (uint)cwtType.Fields["_container"].Offset;
    uint bucketsOffset   = (uint)containerType.Fields["_buckets"].Offset;
    uint entriesOffset   = (uint)containerType.Fields["_entries"].Offset;
    uint hashCodeOffset  = (uint)entryType.Fields["HashCode"].Offset;
    uint nextOffset      = (uint)entryType.Fields["Next"].Offset;
    uint depHndOffset    = (uint)entryType.Fields["depHnd"].Offset;

    // Navigate from the ConditionalWeakTable object to its container
    TargetPointer container = target.ReadPointer(conditionalWeakTable + /* Object data offset */ + containerOffset);

    // Read the container's buckets and entries array pointers
    TargetPointer bucketsPtr = target.ReadPointer(container + /* Object data offset */ + bucketsOffset);
    TargetPointer entriesPtr = target.ReadPointer(container + /* Object data offset */ + entriesOffset);

    // Get the runtime default hash code for the key object (returns 0 if none assigned)
    int hashCode = target.Contracts.Object.TryGetHashCode(key);
    if (hashCode == 0)
        return false;

    hashCode &= int.MaxValue;

    // Read the buckets array length and find the bucket (bucketCount is a power of 2)
    uint bucketCount = target.Read<uint>(bucketsPtr + /* Array::m_NumComponents offset */);
    int bucket = hashCode & (int)(bucketCount - 1);
    int entriesIndex = target.Read<int>(bucketsPtr + /* Array header size */ + bucket * sizeof(int));

    // Get entry size from the entries array's component size
    TargetPointer entriesMT = target.Contracts.Object.GetMethodTableAddress(entriesPtr);
    uint entrySize = rts.GetComponentSize(rts.GetTypeHandle(entriesMT));

    // Walk the chain
    while (entriesIndex != -1)
    {
        TargetPointer entryAddr = entriesPtr + /* Array header size */ + (uint)entriesIndex * entrySize;
        int entryHashCode = target.Read<int>(entryAddr + hashCodeOffset);

        if (entryHashCode == hashCode)
        {
            // depHnd is an OBJECTHANDLE — a pointer to a pointer to the object
            TargetPointer depHnd = target.ReadPointer(entryAddr + depHndOffset);
            TargetPointer handleTarget = target.ReadPointer(depHnd);
            if (handleTarget == key)
            {
                value = target.Contracts.GC.GetHandleExtraInfo(depHnd);
                return true;
            }
        }

        entriesIndex = target.Read<int>(entryAddr + nextOffset);
    }

    return false;
}
```
