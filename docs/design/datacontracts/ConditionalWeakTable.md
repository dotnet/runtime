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
(`Container`, `Container+Entry`) via the `RuntimeTypeSystem` contract rather than cDAC data descriptors.
Field offsets are resolved by name at runtime.

Contract constants:
| Constant | Value | Meaning |
| --- | --- | --- |
| `CWTNamespace` | `System.Runtime.CompilerServices` | Namespace of the `ConditionalWeakTable` type |
| `CWTTypeName` | ``ConditionalWeakTable`2`` | Name of the `ConditionalWeakTable<TKey, TValue>` type |
| `ContainerTypeName` | ``ConditionalWeakTable`2+Container`` | Name of the nested `Container` type |
| `EntryTypeName` | ``ConditionalWeakTable`2+Entry`` | Name of the nested `Entry` value type |
| `ContainerFieldName` | `_container` | Field on `ConditionalWeakTable` pointing to the active container |
| `BucketsFieldName` | `_buckets` | Field on `Container` pointing to the `int[]` buckets array |
| `EntriesFieldName` | `_entries` | Field on `Container` pointing to the `Entry[]` entries array |
| `HashCodeFieldName` | `HashCode` | Field on `Entry` storing the hash code (masked to positive int) |
| `NextFieldName` | `Next` | Field on `Entry` storing the next index in the chain, or -1 |
| `DepHndFieldName` | `depHnd` | Field on `Entry` storing the dependent handle |

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Array` | `m_NumComponents` | Number of elements in the array |

Contracts used:
| Contract Name |
| --- |
| `Object` |
| `GC` |
| `RuntimeTypeSystem` |

The algorithm looks up the `_container` field of the `ConditionalWeakTable` object, then reads the
`_buckets` and `_entries` fields from the container. It resolves `Entry` field offsets (`HashCode`,
`Next`, `depHnd`) via `RuntimeTypeSystem` and determines the entry stride from the entries array's
component size.

``` csharp
bool TryGetValue(TargetPointer conditionalWeakTable, TargetPointer key, out TargetPointer value)
{
    value = TargetPointer.Null;

    // Resolve field offsets by name from CoreLib via RuntimeTypeSystem.
    // GetCoreLibFieldDescAndDef returns a FieldDesc address and FieldDefinition;
    // GetFieldDescOffset extracts the byte offset from those.
    IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

    rts.GetCoreLibFieldDescAndDef(CWTNamespace, CWTTypeName, ContainerFieldName, out fd, out fDef);
    uint containerOffset = rts.GetFieldDescOffset(fd, fDef);

    rts.GetCoreLibFieldDescAndDef(CWTNamespace, ContainerTypeName, BucketsFieldName, out fd, out fDef);
    uint bucketsOffset = rts.GetFieldDescOffset(fd, fDef);

    rts.GetCoreLibFieldDescAndDef(CWTNamespace, ContainerTypeName, EntriesFieldName, out fd, out fDef);
    uint entriesOffset = rts.GetFieldDescOffset(fd, fDef);

    rts.GetCoreLibFieldDescAndDef(CWTNamespace, EntryTypeName, HashCodeFieldName, out fd, out fDef);
    uint hashCodeOffset = rts.GetFieldDescOffset(fd, fDef);

    rts.GetCoreLibFieldDescAndDef(CWTNamespace, EntryTypeName, NextFieldName, out fd, out fDef);
    uint nextOffset = rts.GetFieldDescOffset(fd, fDef);

    rts.GetCoreLibFieldDescAndDef(CWTNamespace, EntryTypeName, DepHndFieldName, out fd, out fDef);
    uint depHndOffset = rts.GetFieldDescOffset(fd, fDef);

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
