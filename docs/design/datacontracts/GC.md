# Contract GC

This contract is for getting information about the garbage collector configuration and state.

## APIs of contract

```csharp
public enum GCHeapType
{
    Unknown,
    Workstation,
    Server,
}
```

```csharp
    // Return the type of the current GC heap
    GCHeapType GetGCHeapType();
    // Return the number of GC heaps
    uint GetGCHeapCount();
    // Return true if the GC structure is valid, otherwise return false
    bool GetGCStructuresValid();
    // Return the maximum generation of the current GC
    uint GetMaxGeneration();
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| _(none)_ | | |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `HeapType` | string | Type of GC heap ("workstation" or "server") |
| `NumHeaps` | TargetPointer | Pointer to the number of heaps for server GC (int) |
| `StructureInvalidCount` | TargetPointer | Pointer to the count of invalid GC structures (int) |
| `MaxGeneration` | TargetPointer | Pointer to the maximum generation number (uint) |

Contracts used:
| Contract Name |
| --- |
| _(none)_ |


Constants used:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `WRK_HEAP_COUNT` | uint | The number of heaps in the `workstation` GC type | `1` |

```csharp
GCHeapType GetGCHeapType()
{
    return target.ReadGlobalString("HeapType") switch
    {
        "workstation" => GCHeapType.Workstation,
        "server" => GCHeapType.Server,
        _ => GCHeapType.Unknown,
    };
}

uint GetGCHeapCount()
{
    switch (GetGCHeapType())
    {
        case GCHeapType.Workstation:
            return WRK_HEAP_COUNT;
        case GCHeapType.Server:
            TargetPointer pNumHeaps = target.ReadGlobalPointer("NumHeaps");
            return (uint)target.Read<int>(pNumHeaps);
        default:
            throw new NotImplementedException("Unknown GC heap type");
    }
}

bool GetGCStructuresValid()
{
    TargetPointer pInvalidCount = target.ReadGlobalPointer("StructureInvalidCount");
    int invalidCount = target.Read<int>(pInvalidCount);
    return invalidCount == 0; // Structures are valid if the count of invalid structures is zero
}

uint GetMaxGeneration()
{
    TargetPointer pMaxGeneration = target.ReadGlobalPointer("MaxGeneration");
    return target.Read<uint>(pMaxGeneration);
}
```
