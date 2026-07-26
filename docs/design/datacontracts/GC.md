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

    public TargetPointer? InternalRootArray { get; init; }
    public TargetNUInt? InternalRootArrayIndex { get; init; }
    public bool? HeapAnalyzeSuccess { get; init; }

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
    bool TryGetGCDynamicAdaptationMode(out int mode);
    // Gets data on a GC heap segment
    GCHeapSegmentData GetHeapSegmentData(TargetPointer segmentAddress);
    // Gets the GlobalMechanisms list
    IReadOnlyList<TargetNUInt> GetGlobalMechanisms();
    // Returns pointers to all GC heaps
    IEnumerable<TargetPointer> GetGCHeaps();

    // The following APIs have both a workstation and server variant.
    // The workstation variant implicitly operates on the global heap.
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
    // Gets the extra info (user data) associated with a dependent handle
    TargetNUInt GetHandleExtraInfo(TargetPointer handle);
    // Gets the global allocation context pointer and limit
    void GetGlobalAllocationContext(out TargetPointer allocPtr, out TargetPointer allocLimit);

    // Gets handle table memory regions (segments)
    IReadOnlyList<GCMemoryRegionData> GetHandleTableMemoryRegions();
    // Gets GC bookkeeping memory regions (card table info linked list)
    IReadOnlyList<GCMemoryRegionData> GetGCBookkeepingMemoryRegions();
    // Gets GC free regions (free region lists and freeable segments)
    IReadOnlyList<GCMemoryRegionData> GetGCFreeRegions();

    // Enumerates every GC heap segment for the supplied heap data. Each yielded GCHeapSegmentInfo
    // describes a single segment with the inclusive start and exclusive end of its memory range
    // and its generation tag (or Ephemeral).
    IEnumerable<GCHeapSegmentInfo> EnumerateHeapSegments(GCHeapData heapData);

    // Given the current probe address within a heap segment and the (aligned) size of the
    // object that lives at that address, returns the next candidate object address.
    // Implementations may consult cached per-target allocation-context state.
    TargetPointer GetPotentialNextObjectAddress(
        TargetPointer currentAddress,
        ulong currentObjectSize,
        GCHeapSegmentInfo segment);

    // Aligns an object's raw size (base size + component bytes) to the alignment required by its containing segment
    ulong AlignObjectSize(ulong size, GCSegmentClassification generation);
```

```csharp
public enum FreeRegionKind
{
    FreeUnknownRegion = 0,
    FreeGlobalHugeRegion = 1,
    FreeGlobalRegion = 2,
    FreeRegion = 3,
    FreeSohSegment = 4,
    FreeUohSegment = 5,
}

public readonly struct GCMemoryRegionData
{
    public TargetPointer Start { get; init; }
    public ulong Size { get; init; }
    public ulong ExtraData { get; init; }
    public int Heap { get; init; }
}

public enum GCSegmentClassification
{
    Unknown,
    Gen0,
    Gen1,
    Gen2,
    LOH,
    POH,
    NonGC,
    // Segments-GC only: marker used by IGC.EnumerateHeapSegments to denote the ephemeral
    // segment on the gen2 list. The caller is responsible for splitting it into the Gen1
    // piece and an optional Gen2 prefix.
    Ephemeral,
}

public readonly record struct GCHeapSegmentInfo(
    TargetPointer Start,
    TargetPointer End,
    GCSegmentClassification Generation);
```

## Version 1

<!-- BEGIN GENERATED: usage contract=GC version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `CardTableInfo` | `NextCardTable` | `pointer` | Pointer to the next card table in the linked list |
| `CardTableInfo` | `Recount` | `uint32` | Reference count for the card table |
| `CardTableInfo` | `Size` | `nuint` | Total size of the bookkeeping allocation |
| `CFinalize` | `FillPointers` | `pointer` | Pointer to the start of an array containing "CFinalizeFillPointersLength" elements |
| `EEAllocContext` | `GCAllocationContext` | `GCAllocContext` | Embedded GC allocation context for the thread |
| `GCAllocContext` | `Limit` | `pointer` | Allocation limit pointer |
| `GCAllocContext` | `Pointer` | `pointer` | GC allocation pointer |
| `GCHeap` | `AllocAllocated` | `pointer` | Heap's highest address allocated by Alloc (in sever builds) |
| `GCHeap` | `BackgroundMaxSavedAddr` | `pointer` | Heap's background saved highest address (only in server builds with background GC) |
| `GCHeap` | `BackgroundMinSavedAddr` | `pointer` | Heap's background saved lowest address (only in server builds with background GC) |
| `GCHeap` | `CardTable` | `pointer` | Pointer to the heap's bookkeeping GC data structure (in sever builds) |
| `GCHeap` | `CompactReasons` | `pointer` | Data array stored per heap (in sever builds) |
| `GCHeap` | `EphemeralHeapSegment` | `pointer` | Pointer to the heap's ephemeral heap segment (in sever builds) |
| `GCHeap` | `ExpandMechanisms` | `pointer` | Data array stored per heap (in sever builds) |
| `GCHeap` | `FinalizeQueue` | `pointer` | Pointer to the heap's CFinalize data structure (in sever builds) |
| `GCHeap` | `FreeableSohSegment` | `pointer` | Head of the freeable SOH segment linked list (server builds, background GC) |
| `GCHeap` | `FreeableUohSegment` | `pointer` | Head of the freeable UOH segment linked list (server builds, background GC) |
| `GCHeap` | `FreeRegions` | `pointer` | Start of the per-heap free region list array (server builds, region GC) |
| `GCHeap` | `GenerationTable` | `pointer` | Pointer to the start of an array containing "TotalGenerationCount" Generation structures (in sever builds) |
| `GCHeap` | `HeapAnalyzeSuccess` | `int32` | Boolean indicating if heap analyze succeeded (in sever builds) |
| `GCHeap` | `InterestingData` | `pointer` | Data array stored per heap (in sever builds) |
| `GCHeap` | `InterestingMechanismBits` | `pointer` | Data array stored per heap (in sever builds) |
| `GCHeap` | `InternalRootArray` | `pointer` | Data array stored per heap (in sever builds) |
| `GCHeap` | `InternalRootArrayIndex` | `nuint` | Index into InternalRootArray (in sever builds) |
| `GCHeap` | `MarkArray` | `pointer` | Pointer to the heap's MarkArray (only in server builds with background GC) |
| `GCHeap` | `NextSweepObj` | `pointer` | Pointer to the heap's NextSweepObj (only in server builds with background GC) |
| `GCHeap` | `OomData` | `OomHistory` | OOM related data in a struct (in sever builds) |
| `GCHeap` | `SavedSweepEphemeralSeg` | `pointer` | Pointer to the heap's saved sweep ephemeral segment (only in server builds with segment and background GC) |
| `GCHeap` | `SavedSweepEphemeralStart` | `pointer` | Start of the heap's sweep ephemeral segment (only in server builds with segment and background GC) |
| `Generation` | *(type size)* | `uint32` | Size in bytes of each entry in the GC generation table |
| `Generation` | `AllocationContext` | `GCAllocContext` | A GCAllocContext struct |
| `Generation` | `AllocationStart` | `pointer` | Pointer to the allocation start |
| `Generation` | `StartSegment` | `pointer` | Pointer to the start heap segment |
| `HandleTable` | `SegmentList` | `pointer` | Head of linked list of handle table segments |
| `HandleTableBucket` | `Table` | `pointer` | Pointer to per-heap HandleTable* array |
| `HandleTableMap` | `BucketsPtr` | `pointer` | Pointer to the bucket pointer array |
| `HandleTableMap` | `Next` | `pointer` | Pointer to the next handle table map in the linked list |
| `HeapSegment` | `Allocated` | `pointer` | Pointer to the allocated memory in the heap segment |
| `HeapSegment` | `BackgroundAllocated` | `pointer` | Pointer to the background allocated memory in the heap segment |
| `HeapSegment` | `Committed` | `pointer` | Pointer to the committed memory in the heap segment |
| `HeapSegment` | `Flags` | `nuint` | Flags indicating the heap segment properties |
| `HeapSegment` | `Heap` | `pointer` | Pointer to the heap that owns this segment (only in server builds) |
| `HeapSegment` | `Mem` | `pointer` | Pointer to the start of the heap segment memory |
| `HeapSegment` | `Next` | `pointer` | Pointer to the next heap segment |
| `HeapSegment` | `Reserved` | `pointer` | Pointer to the reserved memory in the heap segment |
| `HeapSegment` | `Used` | `pointer` | Pointer to the used memory in the heap segment |
| `OomHistory` | `Allocated` | `pointer` | Pointer to allocated memory at time of OOM |
| `OomHistory` | `AllocSize` | `nuint` | Size of the allocation that caused the OOM |
| `OomHistory` | `AvailablePagefileMb` | `nuint` | Available pagefile size in MB at time of OOM |
| `OomHistory` | `Fgm` | `int32` | Foreground GC marker value |
| `OomHistory` | `GcIndex` | `nuint` | GC index when the OOM occurred |
| `OomHistory` | `LohP` | `uint32` | Large object heap flag indicating if OOM was related to LOH |
| `OomHistory` | `Reason` | `int32` | Reason code for the out-of-memory condition |
| `OomHistory` | `Reserved` | `pointer` | Pointer to reserved memory at time of OOM |
| `OomHistory` | `Size` | `nuint` | Size value related to the OOM condition |
| `RegionFreeList` | *(type size)* | `uint32` | Size in bytes of each region free-list structure |
| `RegionFreeList` | `HeadFreeRegion` | `pointer` | Head of the free region segment list |
| `TableSegment` | `NextSegment` | `pointer` | Pointer to the next segment |
| `TableSegment` | `RgAllocation` | `uint8[]` | Circular block-list links per block |
| `TableSegment` | `RgTail` | `uint8[]` | Tail block index per handle type |
| `TableSegment` | `RgUserData` | `uint8[]` | Auxiliary per-block metadata (e.g. secondary handle blocks) |
| `TableSegment` | `RgValue` | `pointer` | Start of handle value storage |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `BlockInvalid` | `uint8` | Sentinel value indicating an invalid handle block index |
| `BookkeepingStart` | `pointer` | Pointer to the bookkeeping start address |
| `CardTableInfoSize` | `uint32` | Size of the dac_card_table_info structure |
| `CFinalizeFillPointersLength` | `uint32` | The number of elements in the CFinalize::FillPointers array |
| `CompactReasonsLength` | `uint32` | The number of elements in the CompactReasons array |
| `CountFreeRegionKinds` | `uint32` | Number of free region kinds (basic, large, huge) |
| `CurrentGCState` | `pointer` | c_gc_state enum value. Only available when GCIdentifiers contains background. |
| `DebugDestroyedHandleValue` | `pointer` | Sentinel handle value used for destroyed handles |
| `DynamicAdaptationMode` | `pointer` | GC heap dynamic adaptation mode. Only available when GCIdentifiers contains dynamic_heap. |
| `ExpandMechanismsLength` | `uint32` | The number of elements in the ExpandMechanisms array |
| `GCGlobalMechanisms` | `pointer` | Pointer to counters recording use of global GC mechanisms |
| `GCHeapAllocAllocated` | `pointer` | Highest address allocated by Alloc (in workstation builds) |
| `GCHeapBackgroundMaxSavedAddr` | `pointer` | Background saved highest address (in workstation builds with background GC) |
| `GCHeapBackgroundMinSavedAddr` | `pointer` | Background saved lowest address (in workstation builds with background GC) |
| `GCHeapCardTable` | `pointer` | Pointer to the static heap's bookkeeping GC data structure (in workstation builds) |
| `GCHeapCompactReasons` | `pointer` | Data array stored per heap (in workstation builds) |
| `GCHeapEphemeralHeapSegment` | `pointer` | Pointer to an ephemeral heap segment (in workstation builds) |
| `GCHeapExpandMechanisms` | `pointer` | Data array stored per heap (in workstation builds) |
| `GCHeapFinalizeQueue` | `pointer` | Pointer to the static heap's CFinalize data structure (in workstation builds) |
| `GCHeapFreeableSohSegment` | `pointer` | Pointer to the freeable SOH segment head (workstation builds) |
| `GCHeapFreeableUohSegment` | `pointer` | Pointer to the freeable UOH segment head (workstation builds) |
| `GCHeapFreeRegions` | `pointer` | Pointer to the free regions array (workstation builds) |
| `GCHeapGenerationTable` | `pointer` | Pointer to the start of an array containing "TotalGenerationCount" Generation structures (in workstation builds) |
| `GCHeapHeapAnalyzeSuccess` | `pointer` | Boolean indicating if heap analyze succeeded (in workstation builds) |
| `GCHeapInterestingData` | `pointer` | Data array stored per heap (in workstation builds) |
| `GCHeapInterestingMechanismBits` | `pointer` | Data array stored per heap (in workstation builds) |
| `GCHeapInternalRootArray` | `pointer` | Data array stored per heap (in workstation builds) |
| `GCHeapInternalRootArrayIndex` | `pointer` | Index into InternalRootArray (in workstation builds) |
| `GCHeapMarkArray` | `pointer` | Pointer to the static heap's MarkArray (in workstation builds with background GC) |
| `GCHeapNextSweepObj` | `pointer` | Pointer to the static heap's NextSweepObj (in workstation builds with background GC) |
| `GCHeapOomData` | `pointer` | OOM related data in a struct (in workstation builds) |
| `GCHeapSavedSweepEphemeralSeg` | `pointer` | Pointer to the static heap's saved sweep ephemeral segment (in workstation builds with segment and background GC) |
| `GCHeapSavedSweepEphemeralStart` | `pointer` | Start of the static heap's sweep ephemeral segment (in workstation builds with segment and background GC) |
| `GCHighestAddress` | `pointer` | Highest GC address as recorded by the VM/GC interface |
| `GCIdentifiers` | `string` | CSV string containing identifiers of the GC. Current values are "server", "workstation", "regions", and "segments" |
| `GCLowestAddress` | `pointer` | Lowest GC address as recorded by the VM/GC interface |
| `GlobalAllocContext` | `pointer` | Pointer to the global EEAllocContext |
| `GlobalFreeHugeRegions` | `pointer` | Pointer to the global free huge region list |
| `GlobalMechanismsLength` | `uint32` | Number of counters in the global GC mechanisms array |
| `GlobalRegionsToDecommit` | `pointer` | Pointer to the global regions-to-decommit array |
| `HandleBlocksPerSegment` | `uint32` | Number of blocks in each TableSegment |
| `HandleMaxInternalTypes` | `uint32` | Number of handle types (length of TableSegment.RgTail) |
| `HandleSegmentSize` | `uint32` | Size of a handle table segment |
| `HandlesPerBlock` | `uint32` | Number of handles in each handle block |
| `HandleTableMap` | `pointer` | Pointer to the head of the handle table map linked list |
| `Heaps` | `pointer` | Pointer to an array of pointers to heaps |
| `InitialHandleTableArraySize` | `uint32` | Number of bucket entries in each HandleTableMap |
| `InterestingDataLength` | `uint32` | The number of elements in the InterestingData array |
| `InterestingMechanismBitsLength` | `uint32` | The number of elements in the InterestingMechanismBits array |
| `MaxGeneration` | `pointer` | Pointer to the maximum generation number (uint) |
| `NumHeaps` | `pointer` | Pointer to the number of heaps for server GC (int) |
| `StructureInvalidCount` | `pointer` | Pointer to the count of invalid GC structures (int) |
| `TotalCpuCount` | `pointer` | Number of available processors |
| `TotalGenerationCount` | `uint32` | The total number of generations in the GC |

### Contracts used

| Contract Name |
| --- |
| `BuiltInCOM` |
| `FeatureFlags` |
| `Object` |
| `Thread` |
<!-- END GENERATED: usage contract=GC version=c1 -->


Constants used:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `WRK_HEAP_COUNT` | uint | The number of heaps in the `workstation` GC type | `1` |
| `HEAP_SEGMENT_FLAGS_READONLY` | ulong | `HeapSegment.Flags` bit identifying a readonly (e.g. frozen, non-GC) segment. | `1` |
| `ALIGNCONST` | uint | Alignment mask for small object heaps | Target pointer size - 1 |
| `ALIGNCONST_LARGE` | uint | Alignment mask for large/pinned object heaps | `7` |

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

    // Read background GC globals - these are absent when background GC is disabled (e.g., on WebAssembly).
    if (target.TryReadGlobalPointer("GCHeapMarkArray", out TargetPointer? markArrayPtr))
    {
        data.MarkArray = target.ReadPointer(markArrayPtr.Value);
    }
    else
    {
        data.MarkArray = 0;
    }
    if (target.TryReadGlobalPointer("GCHeapNextSweepObj", out TargetPointer? nextSweepObjPtr))
    {
        data.NextSweepObj = target.ReadPointer(nextSweepObjPtr.Value);
    }
    else
    {
        data.NextSweepObj = 0;
    }
    if (target.TryReadGlobalPointer("GCHeapBackgroundMinSavedAddr", out TargetPointer? bgMinPtr))
    {
        data.BackgroundMinSavedAddr = target.ReadPointer(bgMinPtr.Value);
    }
    else
    {
        data.BackgroundMinSavedAddr = 0;
    }
    if (target.TryReadGlobalPointer("GCHeapBackgroundMaxSavedAddr", out TargetPointer? bgMaxPtr))
    {
        data.BackgroundMaxSavedAddr = target.ReadPointer(bgMaxPtr.Value);
    }
    else
    {
        data.BackgroundMaxSavedAddr = 0;
    }
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

    // Read background GC heap fields - these fields are absent when background GC is disabled (e.g., on WebAssembly).
    // Check whether the field exists in the type layout before reading; default to 0 if not present.
    Target.TypeInfo gcHeapType = target.GetTypeInfo(DataType.GCHeap);
    if (gcHeapType.Fields.ContainsKey("MarkArray"))
    {
        data.MarkArray = target.ReadPointer(heapAddress + /* GCHeap::MarkArray offset */);
    }
    else
    {
        data.MarkArray = 0;
    }
    if (gcHeapType.Fields.ContainsKey("NextSweepObj"))
    {
        data.NextSweepObj = target.ReadPointer(heapAddress + /* GCHeap::NextSweepObj offset */);
    }
    else
    {
        data.NextSweepObj = 0;
    }
    if (gcHeapType.Fields.ContainsKey("BackgroundMinSavedAddr"))
    {
        data.BackgroundMinSavedAddr = target.ReadPointer(heapAddress + /* GCHeap::BackgroundMinSavedAddr offset */);
    }
    else
    {
        data.BackgroundMinSavedAddr = 0;
    }
    if (gcHeapType.Fields.ContainsKey("BackgroundMaxSavedAddr"))
    {
        data.BackgroundMaxSavedAddr = target.ReadPointer(heapAddress + /* GCHeap::BackgroundMaxSavedAddr offset */);
    }
    else
    {
        data.BackgroundMaxSavedAddr = 0;
    }
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
    if (gcType.Contains("workstation"))
        tableCount = 1;
    else
        tableCount = target.Read<uint>(target.ReadGlobalPointer("TotalCpuCount"));
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
        obj.GetBuiltInComData(handle, out _, out TargetPointer ccw, out _);
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

GetHandleTableMemoryRegions
```csharp
IReadOnlyList<GCMemoryRegionData> IGC.GetHandleTableMemoryRegions()
{
    List<GCMemoryRegionData> regions = new();
    uint handleSegmentSize = /* global value "HandleSegmentSize" */;
    uint tableCount = isServerGC
        ? /* global value "TotalCpuCount" */
        : 1;

    // Safety caps matching native DAC
    const int MaxHandleTableRegions = 8192;
    const int MaxBookkeepingRegions = 32;
    const int MaxSegmentListIterations = 65536;

    int maxRegions = MaxHandleTableRegions;
    TargetPointer handleTableMap = target.ReadGlobalPointer("HandleTableMap");
    while (handleTableMap != TargetPointer.Null && maxRegions >= 0)
    {
        TargetPointer bucketsPtr = target.ReadPointer(handleTableMap + /* HandleTableMap::BucketsPtr offset */);
        foreach (/* read global variable "InitialHandleTableArraySize" bucketPtrs starting at bucketsPtr */)
        {
            if (bucketPtr == TargetPointer.Null) continue;
            TargetPointer table = target.ReadPointer(bucketPtr + /* HandleTableBucket::Table offset */);
            for (uint j = 0; j < tableCount; j++)
            {
                TargetPointer htPtr = target.ReadPointer(table + j * target.PointerSize);
                if (htPtr == TargetPointer.Null) continue;
                TargetPointer segList = target.ReadPointer(htPtr + /* HandleTable::SegmentList offset */);
                if (segList == TargetPointer.Null) continue;
                TargetPointer seg = segList;
                TargetPointer first = seg;
                do
                {
                    regions.Add(new GCMemoryRegionData { Start = seg, Size = handleSegmentSize, Heap = (int)j });
                    seg = target.ReadPointer(seg + /* TableSegment::NextSegment offset */);
                } while (seg != TargetPointer.Null && seg != first);
            }
        }
        handleTableMap = target.ReadPointer(handleTableMap + /* HandleTableMap::Next offset */);
        maxRegions--;
    }
    return regions;
}
```

GetGCBookkeepingMemoryRegions
```csharp
IReadOnlyList<GCMemoryRegionData> IGC.GetGCBookkeepingMemoryRegions()
{
    List<GCMemoryRegionData> regions = new();
    TargetPointer bkGlobal = target.ReadGlobalPointer("BookkeepingStart");
    if (bkGlobal == TargetPointer.Null) throw E_FAIL;
    TargetPointer bookkeepingStart = target.ReadPointer(bkGlobal);
    if (bookkeepingStart == TargetPointer.Null) throw E_FAIL;

    uint cardTableInfoSize = /* global value "CardTableInfoSize" */;
    uint recount = target.ReadNUInt(bookkeepingStart + /* CardTableInfo::Recount offset */);
    ulong size = target.ReadNUInt(bookkeepingStart + /* CardTableInfo::Size offset */);
    if (recount != 0 && size != 0)
        regions.Add(new GCMemoryRegionData { Start = bookkeepingStart, Size = size });

    TargetPointer next = target.ReadPointer(bookkeepingStart + /* CardTableInfo::NextCardTable offset */);
    TargetPointer firstNext = next;
    int maxRegions = MaxBookkeepingRegions;
    // Compare next > cardTableInfoSize to guard against underflow when subtracting
    // cardTableInfoSize. Matches native DAC: `while (next > card_table_info_size)`.
    while (next != TargetPointer.Null && next > cardTableInfoSize && maxRegions > 0)
    {
        TargetPointer ctAddr = next - cardTableInfoSize;
        recount = target.ReadNUInt(ctAddr + /* CardTableInfo::Recount offset */);
        size = target.ReadNUInt(ctAddr + /* CardTableInfo::Size offset */);
        if (recount != 0 && size != 0)
            regions.Add(new GCMemoryRegionData { Start = ctAddr, Size = size });
        next = target.ReadPointer(ctAddr + /* CardTableInfo::NextCardTable offset */);
        if (next == firstNext) break;
        maxRegions--;
    }
    return regions;
}
```

GetGCFreeRegions
```csharp
IReadOnlyList<GCMemoryRegionData> IGC.GetGCFreeRegions()
{
    List<GCMemoryRegionData> regions = new();
    uint countFreeRegionKinds = min(/* global value "CountFreeRegionKinds" */, 16);
    uint regionFreeListSize = /* size of RegionFreeList data descriptor */;

    // Global free huge regions
    if (target.TryReadGlobalPointer("GlobalFreeHugeRegions", out TargetPointer? globalHuge))
        AddFreeList(globalHuge, FreeGlobalHugeRegion, regions);

    // Global regions to decommit
    if (target.TryReadGlobalPointer("GlobalRegionsToDecommit", out TargetPointer? globalDecommit))
        for (int i = 0; i < countFreeRegionKinds; i++)
            AddFreeList(globalDecommit + i * regionFreeListSize, FreeGlobalRegion, regions);

    if (isServerGC)
    {
        // For each server heap: enumerate per-heap free regions + freeable segments
        for each heap in server heaps:
            TargetPointer freeRegionsBase = heapAddress + /* GCHeap::FreeRegions offset */;
            if (freeRegionsBase != TargetPointer.Null)
                for (int j = 0; j < countFreeRegionKinds; j++)
                    AddFreeList(freeRegionsBase + j * regionFreeListSize, FreeRegion, regions, heapIndex);
            TargetPointer sohSeg = target.ReadPointer(heapAddress + /* GCHeap::FreeableSohSegment offset */);
            AddSegmentList(sohSeg, FreeSohSegment, regions, heapIndex);
            TargetPointer uohSeg = target.ReadPointer(heapAddress + /* GCHeap::FreeableUohSegment offset */);
            AddSegmentList(uohSeg, FreeUohSegment, regions, heapIndex);
    }
    else
    {
        // Workstation: use globals for free regions and freeable segments
        if (target.TryReadGlobalPointer("GCHeapFreeRegions", out TargetPointer? freeRegions))
            for (int i = 0; i < countFreeRegionKinds; i++)
                AddFreeList(freeRegions + i * regionFreeListSize, FreeRegion, regions);
        if (target.TryReadGlobalPointer("GCHeapFreeableSohSegment", out TargetPointer? soh))
            AddSegmentList(target.ReadPointer(soh), FreeSohSegment, regions);
        if (target.TryReadGlobalPointer("GCHeapFreeableUohSegment", out TargetPointer? uoh))
            AddSegmentList(target.ReadPointer(uoh), FreeUohSegment, regions);
    }
    return regions;
}

void AddFreeList(TargetPointer freeListAddr, FreeRegionKind kind, List<GCMemoryRegionData> regions, int heap = 0)
{
    TargetPointer headFreeRegion = target.ReadPointer(freeListAddr + /* RegionFreeList::HeadFreeRegion offset */);
    if (headFreeRegion != TargetPointer.Null)
        AddSegmentList(headFreeRegion, kind, regions, heap);
}

void AddSegmentList(TargetPointer start, FreeRegionKind kind, List<GCMemoryRegionData> regions, int heap = 0)
{
    int iterationMax = MaxSegmentListIterations;
    TargetPointer curr = start;
    while (curr != TargetPointer.Null)
    {
        TargetPointer mem = target.ReadPointer(curr + /* HeapSegment::Mem offset */);
        if (mem != TargetPointer.Null)
        {
            TargetPointer committed = target.ReadPointer(curr + /* HeapSegment::Committed offset */);
            ulong size = (mem < committed) ? committed - mem : 0;
            regions.Add(new GCMemoryRegionData { Start = mem, Size = size, ExtraData = kind, Heap = heap });
        }
        curr = target.ReadPointer(curr + /* HeapSegment::Next offset */);
        if (curr == start) break;
        if (iterationMax-- <= 0) break;
    }
}
```

GetHandleExtraInfo
```csharp
TargetNUInt IGC.GetHandleExtraInfo(TargetPointer handle)
{
    // Handle table segments are aligned to their size ("HandleSegmentSize").
    // The segment base is found by masking the handle address.
    // User data blocks are stored in TableSegment.RgUserData, indexed by block number.
    // The block and intra-block index are computed from the handle's position within the segment.

    uint segmentSize = target.ReadGlobal<uint>("HandleSegmentSize");
    TargetPointer segment = handle & ~(ulong)(segmentSize - 1);

    uint headerSize = /* TableSegment::RgValue offset */;
    uint handlesPerBlock = target.ReadGlobal<uint>("HandlesPerBlock");

    uint handleIndex = (uint)((handle - segment - headerSize) / (uint)target.PointerSize);
    uint block = handleIndex / handlesPerBlock;
    uint intraBlockIndex = handleIndex % handlesPerBlock;

    byte userDataBlockIndex = target.Read<byte>(segment + /* TableSegment::RgUserData offset */ + block);
    if (userDataBlockIndex == target.ReadGlobal<byte>("BlockInvalid"))
        return new TargetNUInt(0);

    uint offset = userDataBlockIndex * handlesPerBlock + intraBlockIndex;
    TargetPointer extraInfoAddr = segment + headerSize + offset * (uint)target.PointerSize;

    return target.ReadNUInt(extraInfoAddr);
}
```

EnumerateHeapSegments

Returns the raw GC heap segments for a single heap by walking the per-generation segment
lists.
```csharp
IEnumerable<GCHeapSegmentInfo> IGC.EnumerateHeapSegments(GCHeapData heapData)
{
    // The generation table is laid out as gen0, gen1, gen2, LOH, POH (plus optional extras).
    var gens = heapData.GenerationTable;
    bool regions = GetGCIdentifiers().Contains("regions");

    TargetPointer ephemeralSegment = heapData.EphemeralHeapSegment;
    TargetPointer allocAllocated   = heapData.AllocAllocated;

    if (regions)
    {
        // In regions mode each generation has its own segment list. Readonly entries on
        // the gen2 list represent non-GC (e.g. frozen) regions and are reported as NonGC.
        foreach (var (seg, _) in WalkSegmentList(gens[2].StartSegment))
        {
            var type = (seg.Flags & HEAP_SEGMENT_FLAGS_READONLY) != 0
                ? GCSegmentClassification.NonGC
                : GCSegmentClassification.Gen2;
            yield return new GCHeapSegmentInfo(seg.Mem, seg.Allocated, type);
        }
        foreach (var (seg, _) in WalkSegmentList(gens[1].StartSegment))
            yield return new GCHeapSegmentInfo(seg.Mem, seg.Allocated, GCSegmentClassification.Gen1);
        foreach (var (seg, segAddr) in WalkSegmentList(gens[0].StartSegment))
        {
            // For the gen0 segment that matches the ephemeral_heap_segment, end is alloc_allocated.
            TargetPointer end = segAddr == ephemeralSegment ? allocAllocated : seg.Allocated;
            yield return new GCHeapSegmentInfo(seg.Mem, end, GCSegmentClassification.Gen0);
        }
    }
    else
    {
        // In segments mode the gen2 list contains every SOH segment. The ephemeral
        // segment is tagged Ephemeral as the layer-2 split marker; non-ephemeral entries
        // are reported with their true generation (Gen2 or NonGC for readonly).
        foreach (var (seg, segAddr) in WalkSegmentList(gens[2].StartSegment))
        {
            GCSegmentClassification type;
            if (segAddr == ephemeralSegment)
                type = GCSegmentClassification.Ephemeral;
            else if ((seg.Flags & HEAP_SEGMENT_FLAGS_READONLY) != 0)
                type = GCSegmentClassification.NonGC;
            else
                type = GCSegmentClassification.Gen2;
            TargetPointer end = segAddr == ephemeralSegment ? allocAllocated : seg.Allocated;
            yield return new GCHeapSegmentInfo(seg.Mem, end, type);
        }
    }

    // LOH and POH segments are always reported as-is regardless of GC mode.
    foreach (var (seg, _) in WalkSegmentList(gens[3].StartSegment))
        yield return new GCHeapSegmentInfo(seg.Mem, seg.Allocated, GCSegmentClassification.LOH);
    foreach (var (seg, _) in WalkSegmentList(gens[4].StartSegment))
        yield return new GCHeapSegmentInfo(seg.Mem, seg.Allocated, GCSegmentClassification.POH);
}

IEnumerable<(HeapSegment Segment, TargetPointer Address)> WalkSegmentList(TargetPointer startSegment)
{
    // Bounded traversal of the singly-linked HeapSegment list, guarding against cycles or
    // corrupt links via a fixed iteration cap (MaxSegmentListIterations = 65536).
    int iterationMax = MaxSegmentListIterations;
    TargetPointer current = startSegment;
    while (current != TargetPointer.Null)
    {
        HeapSegment seg = /* read HeapSegment at current */;
        yield return (seg, current);
        current = seg.Next;
        if (iterationMax-- <= 0) throw /* cycle detected */;
    }
}
```

GetPotentialNextObjectAddress

Computes the next candidate object address when walking a Gen0/Ephemeral segment.
Active allocation contexts (per-thread, the global non-thread-local context, and
the per-heap Gen0 context) carve out reserved-but-not-yet-allocated ranges inside
such segments; when the naive `current + size` lands on one of those ranges the
walk must skip past it. The contexts are collected via `IThread.GetThreadStoreData`
and `IThread.GetThreadData` (per-thread contexts), `IGC.GetGlobalAllocationContext`
(global context), and `IGC.GetGCIdentifiers` + `IGC.GetGCHeaps` + `IGC.GetHeapData`
(per-heap Gen0 contexts).

```csharp
TargetPointer IGC.GetPotentialNextObjectAddress(
    TargetPointer currentAddress,
    ulong currentObjectSize,
    GCHeapSegmentInfo segment)
{
    TargetPointer next = new TargetPointer(currentAddress.Value + currentObjectSize);

    if (segment.Generation is not (GCSegmentClassification.Gen0 or GCSegmentClassification.Ephemeral))
        return next;

    ulong minObjSize = AlignForSmallObject((ulong)_target.PointerSize * 3);
    foreach (/* context in allocation contexts */ )
    {
        if (next == /* context pointer */)
            return new TargetPointer(/* context limit */ + minObjSize);
    }
    return next;
}
```

AlignObjectSize

Aligns a raw object size to the alignment required by its containing segment. SOH segments
use pointer-sized alignment; LOH/POH use 8-byte alignment.

```csharp
ulong IGC.AlignObjectSize(ulong size, GCSegmentClassification generation)
{
    return generation is GCSegmentClassification.LOH or GCSegmentClassification.POH
        ? AlignForLargeObject(size)     // (size + ALIGNCONST_LARGE) & ~ALIGNCONST_LARGE
        : AlignForSmallObject(size);    // (size + ALIGNCONST) & ~ALIGNCONST
}
```
