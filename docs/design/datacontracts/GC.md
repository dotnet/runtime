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

    public TargetPointer InternalRootArray { get; init; }
    public TargetNUInt InternalRootArrayIndex { get; init; }
    public bool HeapAnalyzeSuccess { get; init; }

    public IReadOnlyList<TargetNUInt> InterestingData { get; init; }
    public IReadOnlyList<TargetNUInt> CompactReasons { get; init; }
    public IReadOnlyList<TargetNUInt> ExpandMechanisms { get; init; }
    public IReadOnlyList<TargetNUInt> InterestingMechanismBits { get; init; }
}

public readonly struct GCGenerationData
{
    public TargetPointer StartSegment { get; init; }
    public TargetPointer AllocationStart { get; init; }
    public TargetPointer AllocationContextPointer { get; init; }
    public TargetPointer AllocationContextLimit { get; init; }
}

public readonly struct GCHeapSegmentData
{
    public TargetPointer Allocated { get; init; }
    public TargetPointer Committed { get; init; }
    public TargetPointer Reserved { get; init; }
    public TargetPointer Used { get; init; }
    public TargetPointer Mem { get; init; }
    public TargetNUInt Flags { get; init; }
    public TargetPointer Next { get; init; }
    public TargetPointer BackgroundAllocated { get; init; }
    public TargetPointer Heap { get; init; }
}

public readonly struct GCOomData
{
    public int Reason { get; init; }
    public TargetNUInt AllocSize { get; init; }
    public TargetPointer Reserved { get; init; }
    public TargetPointer Allocated { get; init; }
    public TargetNUInt GCIndex { get; init; }
    public int Fgm { get; init; }
    public TargetNUInt Size { get; init; }
    public TargetNUInt AvailablePagefileMB { get; init; }
    public bool LohP { get; init; }
}
```

```csharp
    // Return an array of strings identifying the GC type.
    // Current return values can include:
    // "workstation" or "server"
    // "segments" or "regions"
    // "background"
    // "dynamic_heap"
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
    // Gets the current GC heap dynamic adaptation mode
    bool TryGetDynamicAdaptationMode(out int mode);
    // Gets data on a GC heap segment
    GCHeapSegmentData GetHeapSegmentData(TargetPointer segmentAddress);
    // Gets the GlobalMechanisms list
    IReadOnlyList<TargetNUInt> GetGlobalMechanisms();
    // Returns pointers to all GC heaps
    IEnumerable<TargetPointer> GetGCHeaps();

    // The following APIs have both a workstation and serer variant.
    // The workstation variant implitly operates on the global heap.
    // The server variants allow passing in a heap pointer.

    // Gets data about a GC heap
    GCHeapData GetHeapData();
    GCHeapData GetHeapData(TargetPointer heapAddress);

    // Gets data about a managed OOM occurance
    GCOomData GetOomData();
    GCOomData GetOomData(TargetPointer heapAddress);

    // Gets all GC handles of specified types
    List<HandleData> GetHandles(HandleType[] types);
    // Gets the supported handle types
    HandleType[] GetSupportedHandleTypes();
    // Converts integer types into HandleType enum
    HandleType[] GetHandleTypes(uint[] types);
    // Gets the global allocation context pointer and limit
    void GetGlobalAllocationContext(out TargetPointer allocPtr, out TargetPointer allocLimit);
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
| `GCHeap` | OomData | GC | OOM related data in a struct (in sever builds) |
| `GCHeap` | InternalRootArray | GC | Data array stored per heap (in sever builds) |
| `GCHeap` | InternalRootArrayIndex | GC | Index into InternalRootArray (in sever builds) |
| `GCHeap` | HeapAnalyzeSuccess | GC | Boolean indicating if heap analyze succeeded (in sever builds) |
| `GCHeap` | InterestingData | GC | Data array stored per heap (in sever builds) |
| `GCHeap` | CompactReasons | GC | Data array stored per heap (in sever builds) |
| `GCHeap` | ExpandMechanisms | GC | Data array stored per heap (in sever builds) |
| `GCHeap` | InterestingMechanismBits | GC | Data array stored per heap (in sever builds) |
| `Generation` | AllocationContext | GC | A `GCAllocContext` struct |
| `Generation` | StartSegment | GC | Pointer to the start heap segment |
| `Generation` | AllocationStart | GC | Pointer to the allocation start |
| `CFinalize` | FillPointers | GC | Pointer to the start of an array containing `"CFinalizeFillPointersLength"` elements |
| `HeapSegment` | Allocated | GC | Pointer to the allocated memory in the heap segment |
| `HeapSegment` | Committed | GC | Pointer to the committed memory in the heap segment |
| `HeapSegment` | Reserved | GC | Pointer to the reserved memory in the heap segment |
| `HeapSegment` | Used | GC | Pointer to the used memory in the heap segment |
| `HeapSegment` | Mem | GC | Pointer to the start of the heap segment memory |
| `HeapSegment` | Flags | GC | Flags indicating the heap segment properties |
| `HeapSegment` | Next | GC | Pointer to the next heap segment |
| `HeapSegment` | BackgroundAllocated | GC | Pointer to the background allocated memory in the heap segment |
| `HeapSegment` | Heap | GC | Pointer to the heap that owns this segment (only in server builds) |
| `OomHistory` | Reason | GC | Reason code for the out-of-memory condition |
| `OomHistory` | AllocSize | GC | Size of the allocation that caused the OOM |
| `OomHistory` | Reserved | GC | Pointer to reserved memory at time of OOM |
| `OomHistory` | Allocated | GC | Pointer to allocated memory at time of OOM |
| `OomHistory` | GcIndex | GC | GC index when the OOM occurred |
| `OomHistory` | Fgm | GC | Foreground GC marker value |
| `OomHistory` | Size | GC | Size value related to the OOM condition |
| `OomHistory` | AvailablePagefileMb | GC | Available pagefile size in MB at time of OOM |
| `OomHistory` | LohP | GC | Large object heap flag indicating if OOM was related to LOH |
| `GCAllocContext` | Pointer | VM | Current GCAllocContext pointer |
| `GCAllocContext` | Limit | VM | Pointer to the GCAllocContext limit |
| `HandleTableMap` | BucketsPtr | GC | Pointer to the bucket pointer array |
| `HandleTableMap` | Next | GC | Pointer to the next handle table map in the linked list |
| `HandleTableBucket` | Table | GC | Pointer to per-heap `HandleTable*` array |
| `HandleTable` | SegmentList | GC | Head of linked list of handle table segments |
| `TableSegment` | NextSegment | GC | Pointer to the next segment |
| `TableSegment` | RgTail | GC | Tail block index per handle type |
| `TableSegment` | RgAllocation | GC | Circular block-list links per block |
| `TableSegment` | RgValue | GC | Start of handle value storage |
| `TableSegment` | RgUserData | GC | Auxiliary per-block metadata (e.g. secondary handle blocks) |
| `GCAllocContext` | AllocBytes | VM | Number of bytes allocated on SOH by this context |
| `GCAllocContext` | AllocBytesLoh | VM | Number of bytes allocated not on SOH by this context |
| `EEAllocContext` | GCAllocationContext | VM | The `GCAllocContext` struct within an `EEAllocContext` |

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
| `InterestingDataLength` | uint | GC | The number of elements in the `InterestingData` array |
| `CompactReasonsLength` | uint | GC | The number of elements in the `CompactReasons` array |
| `ExpandMechanismsLength` | uint | GC | The number of elements in the `ExpandMechanisms` array |
| `InterestingMechanismBitsLength` | uint | GC | The number of elements in the `InterestingMechanismBits` array |
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
| `GCHeapOomData` | TargetPointer | GC | OOM related data in a struct (in workstation builds) |
| `GCHeapInternalRootArray` | TargetPointer | GC | Data array stored per heap (in workstation builds) |
| `GCHeapInternalRootArrayIndex` | TargetPointer | GC | Index into InternalRootArray (in workstation builds) |
| `GCHeapHeapAnalyzeSuccess` | TargetPointer | GC | Boolean indicating if heap analyze succeeded (in workstation builds) |
| `GCHeapInterestingData` | TargetPointer | GC | Data array stored per heap (in workstation builds) |
| `GCHeapCompactReasons` | TargetPointer | GC | Data array stored per heap (in workstation builds) |
| `GCHeapExpandMechanisms` | TargetPointer | GC | Data array stored per heap (in workstation builds) |
| `GCHeapInterestingMechanismBits` | TargetPointer | GC | Data array stored per heap (in workstation builds) |
| `CurrentGCState` | uint | GC | `c_gc_state` enum value. Only available when `GCIdentifiers` contains `background`. |
| `DynamicAdaptationMode` | int | GC | GC heap dynamic adaptation mode. Only available when `GCIdentifiers` contains `dynamic_heap`. |
| `GCLowestAddress` | TargetPointer | VM | Lowest GC address as recorded by the VM/GC interface |
| `GCHighestAddress` | TargetPointer | VM | Highest GC address as recorded by the VM/GC interface |
| `HandleTableMap` | TargetPointer | GC | Pointer to the head of the handle table map linked list |
| `InitialHandleTableArraySize` | uint | GC | Number of bucket entries in each `HandleTableMap` |
| `HandleBlocksPerSegment` | uint | GC | Number of blocks in each `TableSegment` |
| `HandleMaxInternalTypes` | uint | GC | Number of handle types (length of `TableSegment.RgTail`) |
| `HandlesPerBlock` | uint | GC | Number of handles in each handle block |
| `BlockInvalid` | byte | GC | Sentinel value indicating an invalid handle block index |
| `DebugDestroyedHandleValue` | TargetPointer | GC | Sentinel handle value used for destroyed handles |
| `FeatureCOMInterop` | byte | VM | Non-zero when COM interop support is enabled |
| `FeatureComWrappers` | byte | VM | Non-zero when `ComWrappers` support is enabled |
| `FeatureObjCMarshal` | byte | VM | Non-zero when Objective-C marshal support is enabled |
| `FeatureJavaMarshal` | byte | VM | Non-zero when Java marshal support is enabled |
| `GlobalAllocContext` | TargetPointer | VM | Pointer to the global `EEAllocContext` |
| `TotalCpuCount` | uint | GC | Number of available processors |

Contracts used:
| Contract Name |
| --- |
| BuiltInCOM |
| Object |

Constants used:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `WRK_HEAP_COUNT` | uint | The number of heaps in the `workstation` GC type | `1` |

```csharp
GCHeapType IGC.GetGCIdentifiers()
{
    string gcIdentifiers = target.ReadGlobalString("GCIdentifiers");
    return gcIdentifiers.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
    if (gcIdentifiers.Contains("background"))
    {
        return target.Read<uint>(target.ReadGlobalPointer("CurrentGCState"));
    }

    return 0;
}

bool IGC.TryGetDynamicAdaptationMode(out int mode)
{
    mode = default;
    string[] gcIdentifiers = GetGCIdentifiers();
    if (!gcIdentifiers.Contains("dynamic_heap"))
    {
        return false;
    }

    mode = target.read<int>(target.ReadGlobalPointer("DynamicAdaptationMode"));
    return true;
}

GCHeapSegmentData IGC.GetGCHeapSegmentData(TargetPointer segmentAddress)
{
    GCHeapSegmentData data = default;

    data.Allocated = target.ReadPointer(segmentAddress + /* HeapSegment::Allocated offset */);
    data.Committed = target.ReadPointer(segmentAddress + /* HeapSegment::Committed offset */);
    data.Reserved = target.ReadPointer(segmentAddress + /* HeapSegment::Reserved offset */);
    data.Used = target.ReadPointer(segmentAddress + /* HeapSegment::Used offset */);
    data.Mem = target.ReadPointer(segmentAddress + /* HeapSegment::Mem offset */);
    data.Flags = target.ReadNUInt(segmentAddress + /* HeapSegment::Flags offset */);
    data.Next = target.ReadPointer(segmentAddress + /* HeapSegment::Next offset */);
    data.BackGroundAllocated = target.ReadPointer(segmentAddress + /* HeapSegment::BackGroundAllocated offset */);

    if (/* HeapSegment::Heap offset */)
    {
        data.Heap = target.ReadPointer(segmentAddress + /* HeapSegment::Heap offset */);
    }
    else
    {
        data.Heap = TargetPointer.Null;
    }

    return data;
}

IReadOnlyList<TargetNUInt> IGC.GetGlobalMechanisms()
{
    if (!target.TryReadGlobalPointer("GCGlobalMechanisms", out TargetPointer? globalMechanismsArrayStart))
        return Array.Empty<TargetNUInt>();
    uint globalMechanismsLength = target.ReadGlobal<uint>("GlobalMechanismsLength");
    return ReadGCHeapDataArray(globalMechanismsArrayStart.Value, globalMechanismsLength);
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

GetOomData
```csharp
GCOomData IGC.GetOomData()
{
    string[] gcIdentifiers = GetGCIdentifiers();
    if (!gcType.Contains("workstation"))
        throw new InvalidOperationException();

    TargetPointer oomHistory = target.ReadGlobalPointer("GCHeapOomData");
    return GetGCOomData(oomHistoryData);
}

GCOomData IGC.GetOomData(TargetPointer heapAddress)
{
    string[] gcIdentifiers = GetGCIdentifiers();
    if (!gcType.Contains("server"))
        throw new InvalidOperationException();

    TargetPointer oomHistory = target.ReadPointer(heapAddress + /* GCHeap::OomData offset */);
    return GetGCOomData(oomHistory);
}

private GCOomData GetGCOomData(TargetPointer oomHistory)
{
    GCOomData data = default;

    data.Reason = target.Read<int>(oomHistory + /* OomHistory::Reason offset */);
    data.AllocSize = target.ReadNUInt(oomHistory + /* OomHistory::AllocSize offset */);
    data.Reserved = target.ReadPointer(oomHistory + /* OomHistory::Reserved offset */);
    data.Allocated = target.ReadPointer(oomHistory + /* OomHistory::Allocated offset */);
    data.GcIndex = target.ReadNUInt(oomHistory + /* OomHistory::GcIndex offset */);
    data.Fgm = target.Read<int>(oomHistory + /* OomHistory::Fgm offset */);
    data.Size = target.ReadNUInt(oomHistory + /* OomHistory::Size offset */);
    data.AvailablePagefileMb = target.ReadNUInt(oomHistory + /* OomHistory::AvailablePagefileMb offset */);
    data.LohP = target.Read<uint>(oomHistory + /* OomHistory::LohP offset */);

    return data;
}
```

GetHeapData
```csharp
GCHeapData IGC.GetHeapData()
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

    // Read GenerationTable
    TargetPointer generationTableArrayStart = target.ReadGlobalPointer("GCHeapGenerationTable");
    data.GenerationTable = GetGenerationData(generationTableArrayStart);

    // Read finalize queue from global and CFinalize offsets
    TargetPointer finalizeQueue = target.ReadPointer(target.ReadGlobalPointer("GCHeapFinalizeQueue"));
    data.FillPointers = GetCFinalizeFillPointers(finalizeQueue);

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

    data.InternalRootArray = target.ReadPointer(target.ReadGlobalPointer("GCHeapInternalRootArray"));
    data.InternalRootArrayIndex = target.ReadNUInt(target.ReadGlobalPointer("GCHeapInternalRootArrayIndex"));
    data.HeapAnalyzeSuccess = target.Read<int>(target.ReadGlobalPointer("GCHeapHeapAnalyzeSuccess"));

    TargetPointer interestingDataStartAddr = target.ReadGlobalPointer("GCHeapInterestingData");
    data.InterestingData = ReadGCHeapDataArray(
        interestingDataStartAddr,
        target.ReadGlobal<uint>("InterestingDataLength"));
    TargetPointer compactReasonsStartAddr = target.ReadGlobalPointer("GCHeapCompactReasons");
    data.CompactReasons = ReadGCHeapDataArray(
        compactReasonsStartAddr,
        target.ReadGlobal<uint>("CompactReasonsLength"));
    TargetPointer expandMechanismsStartAddr = target.ReadGlobalPointer("GCHeapExpandMechanisms");
    data.ExpandMechanisms = ReadGCHeapDataArray(
        expandMechanismsStartAddr,
        target.ReadGlobal<uint>("ExpandMechanismsLength"));
    TargetPointer interestingMechanismBitsStartAddr = target.ReadGlobalPointer("GCHeapInterestingMechanismBits");
    data.InterestingMechanismBits = ReadGCHeapDataArray(
        interestingMechanismBitsStartAddr,
        target.ReadGlobal<uint>("InterestingMechanismBitsLength"));

    return data;
}

GCHeapData IGC.GetHeapData(TargetPointer heapAddress)
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

    // Read GenerationTable
    TargetPointer generationTableArrayStart = heapAddress + /* GCHeap::GenerationTable offset */;
    data.GenerationTable = GetGenerationData(generationTableArrayStart);

    // Read finalize queue fill pointers
    TargetPointer finalizeQueue = target.ReadPointer(heapAddress + /* GCHeap::FinalizeQueue offset */);
    data.FillPointers = GetCFinalizeFillPointers(finalizeQueue);


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

    data.InternalRootArray = target.ReadPointer(heapAddress + /* GCHeap::InternalRootArray offset */);
    data.InternalRootArrayIndex = target.ReadNUInt(heapAddress + /* GCHeap::InternalRootArrayIndex offset */);
    data.HeapAnalyzeSuccess = target.Read<int>(heapAddress + /* GCHeap::HeapAnalyzeSuccess offset */);

    TargetPointer interestingDataStartAddr = heapAddress + /* GCHeap::InterestingData offset */;
    data.InterestingData = ReadGCHeapDataArray(
        interestingDataStartAddr,
        target.ReadGlobal<uint>("InterestingDataLength"));
    TargetPointer compactReasonsStartAddr = heapAddress + /* GCHeap::CompactReasons offset */;
    data.CompactReasons = ReadGCHeapDataArray(
        compactReasonsStartAddr,
        target.ReadGlobal<uint>("CompactReasonsLength"));
    TargetPointer expandMechanismsStartAddr = heapAddress + /* GCHeap::ExpandMechanisms offset */;
    data.ExpandMechanisms = ReadGCHeapDataArray(
        expandMechanismsStartAddr,
        target.ReadGlobal<uint>("ExpandMechanismsLength"));
    TargetPointer interestingMechanismBitsStartAddr = heapAddress + /* GCHeap::InterestingMechanismBits offset */;
    data.InterestingMechanismBits = ReadGCHeapDataArray(
        interestingMechanismBitsStartAddr,
        target.ReadGlobal<uint>("InterestingMechanismBitsLength"));

    return data;
}

private List<GCGeneration> GetGenerationData(TargetPointer generationTableArrayStart)
{
    uint generationTableLength = target.ReadGlobal<uint>("TotalGenerationCount");
    uint generationSize = target.GetTypeInfo(DataType.Generation).Size;

    List<GCGenerationData> generationTable = [];
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

    return generationTable;
}

private List<TargetPointers> GetCFinalizeFillPointers(TargetPointer cfinalize)
{
    TargetPointer fillPointersArrayStart = cfinalize + /* CFinalize::FillPointers offset */;
    uint fillPointersLength = target.ReadGlobal<uint>("CFinalizeFillPointersLength");

    List<TargetPointer> fillPointers = [];
    for (uint i = 0; i < fillPointersLength; i++)
        fillPointers[i] = target.ReadPointer(fillPointersArrayStart + (i * target.PointerSize));

    return fillPointers;
}

private List<TargetNUInt> ReadGCHeapDataArray(TargetPointer arrayStart, uint length)
{
    List<TargetNUInt> arr = [];
    for (uint i = 0; i < length; i++)
        arr.Add(target.ReadNUInt(arrayStart + (i * target.PointerSize)));
    return arr;
}
```

GetHandles
```csharp
public enum HandleType
{
    WeakShort = 0,
    WeakLong = 1,
    Strong = 2,
    Pinned = 3,
    RefCounted = 5,
    Dependent = 6,
    WeakInteriorPointer = 10,
    CrossReference = 11,
}

List<HandleData> IGC.GetHandles(HandleType[] types)
{
    List<HandleData> handles = new();
    TargetPointer handleTableMap = target.ReadGlobalPointer("HandleTableMap");
    string[] gcIdentifiers = GetGCIdentifiers();
    uint tableCount = 0;
    if (!gcType.Contains("workstation"))
        tableCount = 1;
    else
        tableCount = _target.Read<uint>(_target.ReadGlobalPointer("TotalCpuCount")),
    // for each handleTableMap in the linked list
    while (handleTableMap != TargetPointer.Null)
    {
        TargetPointer bucketsPtr = target.ReadPointer(handleTableMap + /* HandleTableMap::BucketsPtr offset */);
        foreach (/* read global variable "InitialHandleTableArraySize" bucketPtrs starting at bucketsPtr */)
        {
            if (bucketPtr == TargetPointer.Null)
                continue;

            for (int j = 0; j < tableCount; j++)
            {
                // double dereference to iterate handle tables per array element per GC heap - native equivalent = map->pBuckets[i]->pTable[j] 
                TargetPointer table = target.ReadPointer(bucketPtr + /* HandleTableBucket::Table offset */);
                TargetPointer handleTablePtr = target.ReadPointer(table + (ulong)(j * target.PointerSize));
                if (handleTablePtr == TargetPointer.Null)
                    continue;

                foreach (HandleType type in types)
                {
                    // initialize segmentPtr and iterate through the linked list of segments.
                    TargetPointer segmentPtr = target.ReadPointer(handleTablePtr + /* HandleTable::SegmentList offset */);
                    if (segmentPtr == TargetPointer.Null)
                        continue;
                    do
                    {
                        GetHandlesForSegment(segmentPtr, type, handles);
                        segmentPtr = target.ReadPointer(segmentPtr + /* TableSegment::NextSegment offset */);
                    } while (segmentPtr != TargetPointer.Null);
                }
            }
        }
        handleTableMap = target.ReadPointer(handleTableMap + /* HandleTableMap::Next offset */);
    }
    return handles;
}

HandleType[] IGC.GetSupportedHandleTypes()
{
    // currently supported types: WeakShort, WeakLong, Strong, Pinned, Dependent, WeakInteriorPointer, RefCounted (conditional on at least one of global variables "FeatureCOMInterop", "FeatureComWrappers", and "FeatureObjCMarshal"), and CrossReference (conditional on global variable "FeatureJavaMarshal")
}

HandleType[] GetHandleTypes(uint[] types) => // map raw uint into HandleType enum

private void GetHandlesForSegment(TargetPointer segmentPtr, HandleType type, List<HandleData> handles)
{
    // GC handles are stored in circular linked lists per segment and handle type. 
    // RgTail = array of bytes that is global variable "HandleMaxInternalTypes" long.
    // Contains tail block indices for each GC handle type.
    // RgAllocation = byte array of block indices that are linked together to find all blocks for a given type. It is global variable "HandleBlocksPerSegment" long
    // RgUserData = byte array of block indices for extra handle info such as dependent handles. It is also "HandleBlocksPerSegment" long.
    // For example, target.Read<byte>(segmentPtr + TableSegment::RgTail offset + x); => RgTail[x];
    Debug.Assert(GetInternalHandleType(type) < target.ReadGlobal<uint>("HandleMaxInternalTypes"));
    byte uBlock = target.Read<byte>(segmentPtr + /* TableSegment::RgTail offset */ + GetInternalHandleType(type));
    if (uBlock == target.ReadGlobal<byte>("BlockInvalid"))
        return;
    uBlock = target.Read<byte>(segmentPtr + /* TableSegment::RgAllocation offset */ + uBlock);
    byte uHead = uBlock;
    do
    {
        GetHandlesForBlock(segmentPtr, uBlock, type, handles);
        // update uBlock
        uBlock = target.Read<byte>(segmentPtr + /* TableSegment::RgAllocation offset */ + uBlock);
    } while (uBlock != uHead);
}

private void GetHandlesForBlock(TargetPointer segmentPtr, byte uBlock, HandleType type, List<HandleData> handles)
{
    for (uint k = 0; k < target.ReadGlobal<byte>("HandlesPerBlock"); k++)
    {
        uint offset = uBlock * target.ReadGlobal<byte>("HandlesPerBlock") + k;
        TargetPointer handleAddress = segmentPtr + /* TableSegment::RgValue offset */ + offset * (uint)_target.PointerSize;
        TargetPointer handle = _target.ReadPointer(handleAddress);
        if (handle == TargetPointer.Null || handle == target.ReadGlobalPointer("DebugDestroyedHandleValue"))
            continue;
        handles.Add(CreateHandleData(handleAddress, uBlock, k, segmentPtr, type));
    }
}

private static bool IsStrongReference(uint type) => // Strong || Pinned;
private static bool HasSecondary(uint type) => // Dependent || WeakInteriorPointer || CrossReference;
private static bool IsRefCounted(uint type) => // RefCounted;
private static uint GetInternalHandleType(HandleType type) => // convert the HandleType enum to the corresponding runtime-dependent constant uint.

private HandleData CreateHandleData(TargetPointer handleAddress, byte uBlock, uint intraBlockIndex, TargetPointer segmentPtr, HandleType type)
{
    HandleData handleData = default;
    handleData.Handle = handleAddress;
    handleData.Type = GetInternalHandleType(type);
    handleData.JupiterRefCount = 0;
    handleData.IsPegged = false;
    handleData.StrongReference = IsStrongReference(type);
    if (HasSecondary(type))
    {
        byte blockIndex = target.Read<byte>(segmentPtr + /* TableSegment::RgUserData offset */ + uBlock);
        if (blockIndex == target.ReadGlobal<byte>("BlockInvalid"))
            handleData.Secondary = 0;
        else
        {
            uint offset = blockIndex * target.ReadGlobal<byte>("HandlesPerBlock") + intraBlockIndex;
            handleData.Secondary = target.ReadPointer(segmentPtr + /* TableSegment::RgValue offset */ + offset * target.PointerSize);
        }
    }
    else
    {
        handleData.Secondary = 0;
    }

    if (target.ReadGlobal<byte>("FeatureCOMInterop") != 0 && IsRefCounted(type))
    {
        IObject obj = target.Contracts.Object;
        TargetPointer handle = target.ReadPointer(handleAddress);
        obj.GetBuiltInComData(handle, out _, out TargetPointer ccw);
        if (ccw != TargetPointer.Null)
        {
            IBuiltInCOM builtInCOM = target.Contracts.BuiltInCOM;
            handleData.RefCount = (uint)builtInCOM.GetRefCount(ccw);
            handleData.StrongReference = handleData.StrongReference || (handleData.RefCount > 0 && !builtInCOM.IsHandleWeak(ccw));
        }
    }

    return handleData;
}
```

GetGlobalAllocationContext
```csharp
void IGC.GetGlobalAllocationContext(out TargetPointer allocPtr, out TargetPointer allocLimit)
{
    TargetPointer globalAllocContextAddress = target.ReadGlobalPointer("GlobalAllocContext");
    allocPtr = target.ReadPointer(globalAllocContextAddress + /* EEAllocContext::GCAllocationContext offset */ + /* GCAllocContext::Pointer offset */);
    allocLimit = target.ReadPointer(globalAllocContextAddress + /* EEAllocContext::GCAllocationContext offset */ + /* GCAllocContext::Limit offset */);
}
```
