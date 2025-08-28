# Contract GC

This contract is for getting information about the garbage collector configuration and state.

## APIs of contract

```csharp
public readonly struct GCHeapData
{
    public TargetPointer MarkArray { get; init; }
    public TargetPointer NextSweepObject { get; init; }
    public TargetPointer BackGroundSavedMinAddress { get; init; }
    public TargetPointer BackGroundSavedMaxAddress { get; init; }

    public TargetPointer AllocAllocated { get; init; }
    public TargetPointer EphemeralHeapSegment { get; init; }
    public TargetPointer CardTable { get; init; }
    public IReadOnlyList<GCGenerationData> GenerationTable { get; init; }

    public IReadOnlyList<TargetPointer> FillPointers { get; init; }

    // Fields only valid in segment GC builds
    public TargetPointer SavedSweepEphemeralSegment { get; init; }
    public TargetPointer SavedSweepEphemeralStart { get; init; }
}

public readonly struct GCGenerationData
{
    public TargetPointer StartSegment { get; init; }
    public TargetPointer AllocationStart { get; init; }
    public TargetPointer AllocationContextPointer { get; init; }
    public TargetPointer AllocationContextLimit { get; init; }
}
```

```csharp
    // Return an array of strings identifying the GC type.
    // Current return values can include:
    // "workstation" or "server"
    // "segments" or "regions"
    // "background"
    string[] GetGCIdentifiers();

    // Return the number of GC heaps
    uint GetGCHeapCount();
    // Return true if the GC structure is valid, otherwise return false
    bool GetGCStructuresValid();
    // Return the maximum generation of the current GC
    uint GetMaxGeneration();
    // Gets the minimum and maximum GC address
    void GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr);
    // Gets the current GC state enum value
    uint GetCurrentGCState();
    // Returns pointers to all GC heaps.
    IEnumerable<TargetPointer> GetGCHeaps();

    /* WKS only APIs */
    GCHeapData WKSGetHeapData();

    /* SVR only APIs */
    GCHeapData SVRGetHeapData(TargetPointer heapAddress);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Source | Meaning |
| --- | --- | --- | --- |
| `GCHeap` | MarkArray | GC | Pointer to the heap's MarkArray (in sever builds) |
| `GCHeap` | NextSweepObj | GC | Pointer to the heap's NextSweepObj (in sever builds) |
| `GCHeap` | BackgroundMinSavedAddr | GC | Heap's background saved lowest address (in sever builds) |
| `GCHeap` | BackgroundMaxSavedAddr | GC | Heap's background saved highest address (in sever builds) |
| `GCHeap` | AllocAllocated | GC | Heap's highest address allocated by Alloc (in sever builds) |
| `GCHeap` | EphemeralHeapSegment | GC | Pointer to the heap's ephemeral heap segment (in sever builds) |
| `GCHeap` | CardTable | GC | Pointer to the heap's bookkeeping GC data structure (in sever builds) |
| `GCHeap` | FinalizeQueue | GC | Pointer to the heap's CFinalize data structure (in sever builds) |
| `GCHeap` | GenerationTable | GC | Pointer to the start of an array containing `"TotalGenerationCount"` `Generation` structures (in sever builds) |
| `GCHeap` | SavedSweepEphemeralSeg | GC | Pointer to the heap's saved sweep ephemeral segment (only in server builds with segment) |
| `GCHeap` | SavedSweepEphemeralStart | GC | Start of the heap's sweep ephemeral segment (only in server builds with segment) |
| `Generation` | AllocationContext | GC | A `GCAllocContext` struct |
| `Generation` | StartSegment | GC | Pointer to the start heap segment |
| `Generation` | AllocationStart | GC | Pointer to the allocation start |
| `CFinalize` | FillPointers | GC | Pointer to the start of an array containing `"CFinalizeFillPointersLength"` elements |
| `GCAllocContext` | Pointer | VM | Current GCAllocContext pointer |
| `GCAllocContext` | Limit | VM | Pointer to the GCAllocContext limit |

Global variables used:
| Global Name | Type | Source | Purpose |
| --- | --- | --- | --- |
| `GCIdentifiers` | string | GC | CSV string containing identifiers of the GC. Current values are "server", "workstation", "regions", and "segments" |
| `NumHeaps` | TargetPointer | GC | Pointer to the number of heaps for server GC (int) |
| `Heaps` | TargetPointer | GC | Pointer to an array of pointers to heaps |
| `StructureInvalidCount` | TargetPointer | GC | Pointer to the count of invalid GC structures (int) |
| `MaxGeneration` | TargetPointer | GC | Pointer to the maximum generation number (uint) |
| `TotalGenerationCount` | uint | GC | The total number of generations in the GC |
| `CFinalizeFillPointersLength` | uint | GC | The number of elements in the `CFinalize::FillPointers` array |
| `GCHeapMarkArray` | TargetPointer | GC | Pointer to the static heap's MarkArray (in workstation builds) |
| `GCHeapNextSweepObj` | TargetPointer | GC | Pointer to the static heap's NextSweepObj (in workstation builds) |
| `GCHeapBackgroundMinSavedAddr` | TargetPointer | GC | Background saved lowest address (in workstation builds) |
| `GCHeapBackgroundMaxSavedAddr` | TargetPointer | GC | Background saved highest address (in workstation builds) |
| `GCHeapAllocAllocated` | TargetPointer | GC | Highest address allocated by Alloc (in workstation builds) |
| `GCHeapEphemeralHeapSegment` | TargetPointer | GC | Pointer to an ephemeral heap segment (in workstation builds) |
| `GCHeapCardTable` | TargetPointer | GC | Pointer to the static heap's bookkeeping GC data structure (in workstation builds) |
| `GCHeapFinalizeQueue` | TargetPointer | GC | Pointer to the static heap's CFinalize data structure (in workstation builds) |
| `GCHeapGenerationTable` | TargetPointer | GC | Pointer to the start of an array containing `"TotalGenerationCount"` `Generation` structures (in workstation builds) | 
| `GCHeapSavedSweepEphemeralSeg` | TargetPointer | GC | Pointer to the static heap's saved sweep ephemeral segment (in workstation builds with segment) |
| `GCHeapSavedSweepEphemeralStart` | TargetPointer | GC | Start of the static heap's sweep ephemeral segment (in workstation builds with segment) |
| `GCLowestAddress` | TargetPointer | VM | Lowest GC address as recorded by the VM/GC interface |
| `GCHighestAddress` | TargetPointer | VM | Highest GC address as recorded by the VM/GC interface |

Contracts used:
| Contract Name |
| --- |
| _(none)_ |


Constants used:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `WRK_HEAP_COUNT` | uint | The number of heaps in the `workstation` GC type | `1` |

```csharp
GCHeapType IGC.GetGCIdentifiers()
{
    string gcIdentifiers = target.ReadGlobalString("GCIdentifiers");
    return gcIdentifiers.Split(", ");
}

uint IGC.GetGCHeapCount()
{
    string[] gcIdentifiers = GetGCIdentifiers()
    if (gcType.Contains("workstation"))
    {
        return WRK_HEAP_COUNT;
    }
    if (gcType.Contains("server"))
    {
        TargetPointer pNumHeaps = target.ReadGlobalPointer("NumHeaps");
        return (uint)target.Read<int>(pNumHeaps);
    }

    throw new NotImplementedException("Unknown GC heap type");
}

bool IGC.GetGCStructuresValid()
{
    TargetPointer pInvalidCount = target.ReadGlobalPointer("StructureInvalidCount");
    int invalidCount = target.Read<int>(pInvalidCount);
    return invalidCount == 0; // Structures are valid if the count of invalid structures is zero
}

uint IGC.GetMaxGeneration()
{
    TargetPointer pMaxGeneration = target.ReadGlobalPointer("MaxGeneration");
    return target.Read<uint>(pMaxGeneration);
}

void IGC.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr)
{
    minAddr = target.ReadPointer(target.ReadGlobalPointer("GCLowestAddress"));
    maxAddr = target.ReadPointer(target.ReadGlobalPointer("GCHighestAddress"));
}

uint IGC.GetCurrentGCState()
{
    string[] gcIdentifiers = GetGCIdentifiers();
    if (gcType.Contains("background"))
    {
        return target.Read<uint>(target.ReadGlobalPointer("CurrentGCState"));
    }

    return 0;
}

IEnumerable<TargetPointer> IGC.GetGCHeaps()
{
    string[] gcIdentifiers = GetGCIdentifiers();
    if (!gcType.Contains("server"))
        yield break; // Only server GC has multiple heaps

    uint heapCount = GetGCHeapCount();
    TargetPointer heapTable = TargetPointer.ReadPointer(target.ReadGlobalPointer("Heaps"));
    // heapTable is an array of pointers to heaps
    // it must be heapCount in length
    for (uint i = 0; i < heapCount; i++)
    {
        yield return target.ReadPointer(heapTable + (i * target.PointerSize));
    }
}
```

workstation GC only APIs
```csharp
GCHeapData IGC.WKSGetHeapData(TargetPointer heapAddress)
{
    string[] gcIdentifiers = GetGCIdentifiers();
    if (!gcType.Contains("workstation"))
        throw new InvalidOperationException();

    GCHeapData data;

    // Read fields directly from globals
    data.MarkArray = target.ReadPointer(target.ReadGlobalPointer("GCHeapMarkArray"));
    data.NextSweepObj = target.ReadPointer(target.ReadGlobalPointer("GCHeapNextSweepObj"));
    data.BackgroundMinSavedAddr = target.ReadPointer(target.ReadGlobalPointer("GCHeapBackgroundMinSavedAddr"));
    data.BackgroundMaxSavedAddr = target.ReadPointer(target.ReadGlobalPointer("GCHeapBackgroundMaxSavedAddr"));
    data.AllocAllocated = target.ReadPointer(target.ReadGlobalPointer("GCHeapAllocAllocated"));
    data.EphemeralHeapSegment = target.ReadPointer(target.ReadGlobalPointer("GCHeapEphemeralHeapSegment"));
    data.CardTable = target.ReadPointer(target.ReadGlobalPointer("GCHeapCardTable"));

    if (target.TryReadGlobalPointer("GCHeapSavedSweepEphemeralSeg", out TargetPointer? savedSweepEphemeralSegPtr))
    {
        data.SavedSweepEphemeralSeg = target.ReadPointer(savedSweepEphemeralSegPtr.Value);
    }
    else
    {
        data.SavedSweepEphemeralSeg = 0;
    }

    if (target.TryReadGlobalPointer("GCHeapSavedSweepEphemeralStart", out TargetPointer? savedSweepEphemeralStartPtr))
    {
        data.SavedSweepEphemeralStart = target.ReadPointer(savedSweepEphemeralStartPtr.Value);
    }
    else
    {
        data.SavedSweepEphemeralStart = 0;
    }

    // Read GenerationTable
    TargetPointer generationTableArrayStart = target.ReadGlobalPointer("GCHeapGenerationTable");
    uint generationTableLength = target.ReadGlobal<uint>("TotalGenerationCount");
    uint generationSize = target.GetTypeInfo(DataType.Generation).Size;

    List<GCGenerationData> generationTable = []
    for (uint i = 0; i < generationTableLength; i++)
    {
        GCGenerationData generationData;
        TargetPointer generationAddress = generationTableArrayStart + (i * generationSize);
        generationData.StartSegment = target.ReadPointer(generationAddress + /* Generation::StartSegment offset */);
        if (/* Generation::AllocationStart is present */)
            generationData.AllocationStart = target.ReadPointer(generationAddress + /* Generation::AllocationStart offset */)
        else
            generationData.AllocationStart = -1;

        generationData.AllocationContextPointer =
            target.ReadPointer(generationAddress + /* Generation::AllocationContext offset */ + /* GCAllocContext::Pointer offset */);
        generationData.AllocationContextLimit =
            target.ReadPointer(generationAddress + /* Generation::AllocationContext offset */ + /* GCAllocContext::Limit offset */);

        generationTable.Add(generationData);
    }
    data.GenerationTable = generationTable;

    // Read finalize queue from global and CFinalize offsets
    TargetPointer finalizeQueue = target.ReadPointer(target.ReadGlobalPointer("GCHeapFinalizeQueue"));
    TargetPointer fillPointersArrayStart = finalizeQueue + /* CFinalize::FillPointers offset */;
    uint fillPointersLength = target.ReadGlobal<uint>("CFinalizeFillPointersLength");

    List<TargetPointer> fillPointers = [];
    for (uint i = 0; i < fillPointersLength; i++)
        fillPointers[i] = target.ReadPointer(fillPointersArrayStart + (i * target.PointerSize));
    
    data.FillPointers = fillPointers;

    return data;
}
```

server GC only APIs
```csharp
GCHeapData IGC.SVRGetHeapData(TargetPointer heapAddress)
{
    string[] gcIdentifiers = GetGCIdentifiers();
    if (!gcType.Contains("server"))
        throw new InvalidOperationException();

    GCHeapData data;

    // Read fields directly from heap
    data.MarkArray = target.ReadPointer(heapAddress + /* GCHeap::MarkArray offset */);
    data.NextSweepObj = target.ReadPointer(heapAddress + /* GCHeap::NextSweepObj offset */);
    data.BackgroundMinSavedAddr = target.ReadPointer(heapAddress + /* GCHeap::BackgroundMinSavedAddr offset */);
    data.BackgroundMaxSavedAddr = target.ReadPointer(heapAddress + /* GCHeap::BackgroundMaxSavedAddr offset */);
    data.AllocAllocated = target.ReadPointer(heapAddress + /* GCHeap::AllocAllocated offset */);
    data.EphemeralHeapSegment = target.ReadPointer(heapAddress + /* GCHeap::EphemeralHeapSegment offset */);
    data.CardTable = target.ReadPointer(heapAddress + /* GCHeap::CardTable offset */);

    if (/* GCHeap::SavedSweepEphemeralSeg is present */)
    {
        data.SavedSweepEphemeralSeg = target.ReadPointer(heapAddress + /* GCHeap::SavedSweepEphemeralSeg offset */);
    }
    else
    {
        data.SavedSweepEphemeralSeg = 0;
    }

    if (/* GCHeap::SavedSweepEphemeralStart is present */)
    {
        data.SavedSweepEphemeralStart = target.ReadPointer(heapAddress + /* GCHeap::SavedSweepEphemeralStart offset */);
    }
    else
    {
        data.SavedSweepEphemeralStart = 0;
    }

    // Read GenerationTable
    TargetPointer generationTableArrayStart = heapAddress + /* GCHeap::GenerationTable offset */;
    uint generationTableLength = target.ReadGlobal<uint>("TotalGenerationCount");
    uint generationSize = target.GetTypeInfo(DataType.Generation).Size;

    List<GCGenerationData> generationTable = []
    for (uint i = 0; i < generationTableLength; i++)
    {
        GCGenerationData generationData;
        TargetPointer generationAddress = generationTableArrayStart + (i * generationSize);
        generationData.StartSegment = target.ReadPointer(generationAddress + /* Generation::StartSegment offset */);
        if (/* Generation::AllocationStart is present */)
            generationData.AllocationStart = target.ReadPointer(generationAddress + /* Generation::AllocationStart offset */)
        else
            generationData.AllocationStart = -1;

        generationData.AllocationContextPointer =
            target.ReadPointer(generationAddress + /* Generation::AllocationContext offset */ + /* GCAllocContext::Pointer offset */);
        generationData.AllocationContextLimit =
            target.ReadPointer(generationAddress + /* Generation::AllocationContext offset */ + /* GCAllocContext::Limit offset */);

        generationTable.Add(generationData);
    }
    data.GenerationTable = generationTable;

    // Read finalize queue from global and CFinalize offsets
    TargetPointer finalizeQueue = target.ReadPointer(heapAddress + /* GCHeap::FinalizeQueue offset */);
    TargetPointer fillPointersArrayStart = finalizeQueue + /* CFinalize::FillPointers offset */;
    uint fillPointersLength = target.ReadGlobal<uint>("CFinalizeFillPointersLength");

    List<TargetPointer> fillPointers = [];
    for (uint i = 0; i < fillPointersLength; i++)
        fillPointers[i] = target.ReadPointer(fillPointersArrayStart + (i * target.PointerSize));
    
    data.FillPointers = fillPointers;

    return data;
}
```

