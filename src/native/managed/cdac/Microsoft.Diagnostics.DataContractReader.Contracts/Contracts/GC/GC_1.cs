// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct GC_1 : IGC
{
    private const uint WRK_HEAP_COUNT = 1;

    private enum GCType
    {
        Unknown,
        Workstation,
        Server,
    }

    private readonly Target _target;

    internal GC_1(Target target)
    {
        _target = target;
    }

    string[] IGC.GetGCIdentifiers()
    {
        string gcIdentifiers = _target.ReadGlobalString(Constants.Globals.GCIdentifiers);
        return gcIdentifiers.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    uint IGC.GetGCHeapCount()
    {
        switch (GetGCType())
        {
            case GCType.Workstation:
                return WRK_HEAP_COUNT; // Workstation GC has a single heap
            case GCType.Server:
                TargetPointer pNumHeaps = _target.ReadGlobalPointer(Constants.Globals.NumHeaps);
                return (uint)_target.Read<int>(pNumHeaps);
            default:
                throw new NotImplementedException("Unknown GC type");
        }
    }

    bool IGC.GetGCStructuresValid()
    {
        TargetPointer pInvalidCount = _target.ReadGlobalPointer(Constants.Globals.StructureInvalidCount);
        int invalidCount = _target.Read<int>(pInvalidCount);
        return invalidCount == 0; // Structures are valid if the count of invalid structures is zero
    }

    uint IGC.GetMaxGeneration()
    {
        TargetPointer pMaxGeneration = _target.ReadGlobalPointer(Constants.Globals.MaxGeneration);
        return _target.Read<uint>(pMaxGeneration);
    }

    void IGC.GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr)
    {
        minAddr = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCLowestAddress));
        maxAddr = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHighestAddress));
    }

    uint IGC.GetCurrentGCState()
    {
        if (!IsBackgroundGCEnabled())
            return 0;
        return _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.CurrentGCState));
    }

    bool IGC.TryGetGCDynamicAdaptationMode(out int mode)
    {
        mode = default;
        if (!IsDatasEnabled())
            return false;
        mode = _target.Read<int>(_target.ReadGlobalPointer(Constants.Globals.DynamicAdaptationMode));
        return true;
    }

    GCHeapSegmentData IGC.GetHeapSegmentData(TargetPointer segmentAddress)
    {
        Data.HeapSegment heapSegment = _target.ProcessedData.GetOrAdd<Data.HeapSegment>(segmentAddress);
        return new GCHeapSegmentData()
        {
            Allocated = heapSegment.Allocated,
            Committed = heapSegment.Committed,
            Reserved = heapSegment.Reserved,
            Used = heapSegment.Used,
            Mem = heapSegment.Mem,
            Flags = heapSegment.Flags,
            Next = heapSegment.Next,
            BackgroundAllocated = heapSegment.BackgroundAllocated,
            Heap = heapSegment.Heap ?? TargetPointer.Null,
        };
    }

    IReadOnlyList<TargetNUInt> IGC.GetGlobalMechanisms()
    {
        if (!_target.TryReadGlobalPointer(Constants.Globals.GCGlobalMechanisms, out TargetPointer? globalMechanismsArrayStart))
            return Array.Empty<TargetNUInt>();
        uint globalMechanismsLength = _target.ReadGlobal<uint>(Constants.Globals.GlobalMechanismsLength);
        return ReadGCHeapDataArray(globalMechanismsArrayStart.Value, globalMechanismsLength);
    }

    IEnumerable<TargetPointer> IGC.GetGCHeaps()
    {
        if (GetGCType() != GCType.Server)
            yield break; // Only server GC has multiple heaps

        uint heapCount = ((IGC)this).GetGCHeapCount();
        TargetPointer heapTable = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.Heaps));
        for (uint i = 0; i < heapCount; i++)
        {
            yield return _target.ReadPointer(heapTable + (i * (uint)_target.PointerSize));
        }
    }

    GCHeapData IGC.GetHeapData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("GetHeapData() is only valid for Workstation GC.");

        return GetGCHeapDataFromHeap(new GCHeapWKS(_target));
    }

    GCHeapData IGC.GetHeapData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetHeapData(TargetPointer heap) is only valid for Server GC.");

        Data.GCHeapSVR heap = _target.ProcessedData.GetOrAdd<Data.GCHeapSVR>(heapAddress);
        return GetGCHeapDataFromHeap(heap);
    }

    private GCHeapData GetGCHeapDataFromHeap(IGCHeap heap)
    {
        Data.CFinalize finalize = _target.ProcessedData.GetOrAdd<Data.CFinalize>(heap.FinalizeQueue);

        return new GCHeapData()
        {
            MarkArray = heap.MarkArray,
            NextSweepObject = heap.NextSweepObj,
            BackGroundSavedMinAddress = heap.BackgroundMinSavedAddr,
            BackGroundSavedMaxAddress = heap.BackgroundMaxSavedAddr,
            AllocAllocated = heap.AllocAllocated,
            EphemeralHeapSegment = heap.EphemeralHeapSegment,
            CardTable = heap.CardTable,
            GenerationTable = GetGenerationData(heap.GenerationTable).AsReadOnly(),
            FillPointers = GetFillPointers(finalize).AsReadOnly(),
            SavedSweepEphemeralSegment = heap.SavedSweepEphemeralSeg ?? TargetPointer.Null,
            SavedSweepEphemeralStart = heap.SavedSweepEphemeralStart ?? TargetPointer.Null,

            InternalRootArray = heap.InternalRootArray,
            InternalRootArrayIndex = heap.InternalRootArrayIndex,
            HeapAnalyzeSuccess = heap.HeapAnalyzeSuccess,

            InterestingData = ReadGCHeapDataArray(
                heap.InterestingData,
                _target.ReadGlobal<uint>(Constants.Globals.InterestingDataLength))
                .AsReadOnly(),
            CompactReasons = ReadGCHeapDataArray(
                heap.CompactReasons,
                _target.ReadGlobal<uint>(Constants.Globals.CompactReasonsLength))
                .AsReadOnly(),
            ExpandMechanisms = ReadGCHeapDataArray(
                heap.ExpandMechanisms,
                _target.ReadGlobal<uint>(Constants.Globals.ExpandMechanismsLength))
                .AsReadOnly(),
            InterestingMechanismBits = ReadGCHeapDataArray(
                heap.InterestingMechanismBits,
                _target.ReadGlobal<uint>(Constants.Globals.InterestingMechanismBitsLength))
                .AsReadOnly(),
        };
    }

    private List<GCGenerationData> GetGenerationData(TargetPointer generationTableArrayStart)
    {
        uint generationTableLength = _target.ReadGlobal<uint>(Constants.Globals.TotalGenerationCount);
        uint generationSize = _target.GetTypeInfo(DataType.Generation).Size ?? throw new InvalidOperationException("Type Generation has no size");
        List<Data.Generation> generationTable = [];
        for (uint i = 0; i < generationTableLength; i++)
        {
            TargetPointer generationAddress = generationTableArrayStart + i * generationSize;
            generationTable.Add(_target.ProcessedData.GetOrAdd<Data.Generation>(generationAddress));
        }
        List<GCGenerationData> generationDataList = generationTable.Select(gen =>
        new GCGenerationData()
        {
            StartSegment = gen.StartSegment,
            AllocationStart = gen.AllocationStart ?? 0,
            AllocationContextPointer = gen.AllocationContext.Pointer,
            AllocationContextLimit = gen.AllocationContext.Limit,
        }).ToList();
        return generationDataList;
    }

    private List<TargetPointer> GetFillPointers(Data.CFinalize cFinalize)
    {
        uint fillPointersLength = _target.ReadGlobal<uint>(Constants.Globals.CFinalizeFillPointersLength);
        TargetPointer fillPointersArrayStart = cFinalize.FillPointers;
        List<TargetPointer> fillPointers = [];
        for (uint i = 0; i < fillPointersLength; i++)
            fillPointers.Add(_target.ReadPointer(fillPointersArrayStart + i * (uint)_target.PointerSize));
        return fillPointers;
    }

    private List<TargetNUInt> ReadGCHeapDataArray(TargetPointer arrayStart, uint length)
    {
        List<TargetNUInt> arr = [];
        for (uint i = 0; i < length; i++)
            arr.Add(_target.ReadNUInt(arrayStart + (i * (uint)_target.PointerSize)));
        return arr;
    }

    GCOomData IGC.GetOomData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("GetOomData() is only valid for Workstation GC.");

        TargetPointer oomHistory = _target.ReadGlobalPointer(Constants.Globals.GCHeapOomData);
        Data.OomHistory oomHistoryData = _target.ProcessedData.GetOrAdd<Data.OomHistory>(oomHistory);
        return GetGCOomData(oomHistoryData);
    }

    GCOomData IGC.GetOomData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetOomData(TargetPointer heap) is only valid for Server GC.");

        Data.GCHeapSVR heap = _target.ProcessedData.GetOrAdd<Data.GCHeapSVR>(heapAddress);
        return GetGCOomData(heap.OomData);
    }

    private static GCOomData GetGCOomData(Data.OomHistory oomHistory)
        => new GCOomData()
        {
            Reason = oomHistory.Reason,
            AllocSize = oomHistory.AllocSize,
            Reserved = oomHistory.Reserved,
            Allocated = oomHistory.Allocated,
            GCIndex = oomHistory.GcIndex,
            Fgm = oomHistory.Fgm,
            Size = oomHistory.Size,
            AvailablePagefileMB = oomHistory.AvailablePagefileMb,
            LohP = oomHistory.LohP != 0,
        };

    void IGC.GetGlobalAllocationContext(out TargetPointer allocPtr, out TargetPointer allocLimit)
    {
        TargetPointer globalAllocContextAddress = _target.ReadGlobalPointer(Constants.Globals.GlobalAllocContext);
        Data.EEAllocContext eeAllocContext = _target.ProcessedData.GetOrAdd<Data.EEAllocContext>(globalAllocContextAddress);
        allocPtr = eeAllocContext.GCAllocationContext.Pointer;
        allocLimit = eeAllocContext.GCAllocationContext.Limit;
    }

    private GCType GetGCType()
    {
        string[] identifiers = ((IGC)this).GetGCIdentifiers();
        if (identifiers.Contains(GCIdentifiers.Workstation))
        {
            return GCType.Workstation;
        }
        else if (identifiers.Contains(GCIdentifiers.Server))
        {
            return GCType.Server;
        }
        else
        {
            return GCType.Unknown; // Unknown or unsupported GC type
        }
    }

    private bool IsBackgroundGCEnabled()
    {
        string[] identifiers = ((IGC)this).GetGCIdentifiers();
        return identifiers.Contains(GCIdentifiers.Background);
    }

    private bool IsDatasEnabled()
    {
        string[] identifiers = ((IGC)this).GetGCIdentifiers();
        return identifiers.Contains(GCIdentifiers.DynamicHeapCount);
    }

    private bool IsRegionsGC()
    {
        string[] identifiers = ((IGC)this).GetGCIdentifiers();
        return identifiers.Contains(GCIdentifiers.Regions);
    }

    IEnumerable<GCMemoryRegionData> IGC.GetHandleTableMemoryRegions()
    {
        List<GCMemoryRegionData> regions = [];

        int maxSlots = 1;
        if (GetGCType() == GCType.Server)
            maxSlots = (int)((IGC)this).GetGCHeapCount();

        uint handleSegmentSize = _target.ReadGlobal<uint>(Constants.Globals.HandleSegmentSize);
        uint initialHandleTableArraySize = _target.ReadGlobal<uint>(Constants.Globals.InitialHandleTableArraySize);

        int maxRegions = 8192;
        TargetPointer mapAddr = _target.ReadGlobalPointer(Constants.Globals.HandleTableMap);

        while (mapAddr != TargetPointer.Null && maxRegions >= 0)
        {
            Data.HandleTableMap map = _target.ProcessedData.GetOrAdd<Data.HandleTableMap>(mapAddr);

            for (int i = 0; i < initialHandleTableArraySize; i++)
            {
                TargetPointer bucketPtr = _target.ReadPointer(map.Buckets + (ulong)(i * _target.PointerSize));
                if (bucketPtr == TargetPointer.Null)
                    continue;

                Data.HandleTableBucket bucket = _target.ProcessedData.GetOrAdd<Data.HandleTableBucket>(bucketPtr);

                for (int j = 0; j < maxSlots; j++)
                {
                    TargetPointer tablePtr = _target.ReadPointer(bucket.Table + (ulong)(j * _target.PointerSize));
                    Data.HandleTable table = _target.ProcessedData.GetOrAdd<Data.HandleTable>(tablePtr);
                    TargetPointer firstSegment = table.SegmentList;
                    TargetPointer curr = firstSegment;

                    do
                    {
                        regions.Add(new GCMemoryRegionData()
                        {
                            Start = curr,
                            Size = handleSegmentSize,
                            Heap = j,
                        });

                        Data.HandleTableSegment segment = _target.ProcessedData.GetOrAdd<Data.HandleTableSegment>(curr);
                        curr = segment.NextSegment;
                    } while (curr != TargetPointer.Null && curr != firstSegment);
                }
            }

            mapAddr = map.Next;
            maxRegions--;
        }

        return regions;
    }

    IEnumerable<GCMemoryRegionData> IGC.GetGCBookkeepingMemoryRegions()
    {
        List<GCMemoryRegionData> regions = [];

        TargetPointer bookkeepingStartGlobal;
        if (GetGCType() == GCType.Server)
        {
            // For server GC, bookkeeping_start is not available as a workstation global
            // The bookkeeping_start is a per-heap static, but in practice it's the same global
            // We need to check for the global pointer
            if (!_target.TryReadGlobalPointer(Constants.Globals.GCHeapBookkeepingStart, out TargetPointer? bookkeepingPtr))
                return regions;
            bookkeepingStartGlobal = bookkeepingPtr.Value;
        }
        else
        {
            if (!_target.TryReadGlobalPointer(Constants.Globals.GCHeapBookkeepingStart, out TargetPointer? bookkeepingPtr))
                return regions;
            bookkeepingStartGlobal = bookkeepingPtr.Value;
        }

        TargetPointer ctiAddr = _target.ReadPointer(bookkeepingStartGlobal);
        if (ctiAddr == TargetPointer.Null)
            return regions;

        ulong cardTableInfoSize = _target.ReadGlobal<ulong>(Constants.Globals.CardTableInfoSize);

        Data.CardTableInfo cardTableInfo = _target.ProcessedData.GetOrAdd<Data.CardTableInfo>(ctiAddr);

        if (cardTableInfo.Recount != 0 && cardTableInfo.Size.Value != 0)
        {
            regions.Add(new GCMemoryRegionData()
            {
                Start = ctiAddr,
                Size = cardTableInfo.Size.Value,
            });
        }

        TargetPointer next = cardTableInfo.NextCardTable;

        int maxRegions = 32;
        while (next > cardTableInfoSize)
        {
            TargetPointer ctAddr = next - cardTableInfoSize;
            Data.CardTableInfo ct = _target.ProcessedData.GetOrAdd<Data.CardTableInfo>(ctAddr);

            if (ct.Recount != 0 && ct.Size.Value != 0)
            {
                regions.Add(new GCMemoryRegionData()
                {
                    Start = ctAddr,
                    Size = ct.Size.Value,
                });
            }

            next = ct.NextCardTable;
            if (next == cardTableInfo.NextCardTable)
                break;

            if (--maxRegions <= 0)
                break;
        }

        return regions;
    }

    IEnumerable<GCMemoryRegionData> IGC.GetGCFreeRegions()
    {
        List<GCMemoryRegionData> regions = [];

        int countFreeRegionKinds = _target.ReadGlobal<int>(Constants.Globals.CountFreeRegionKinds);
        countFreeRegionKinds = Math.Min(countFreeRegionKinds, 16);

        // Global free huge regions
        if (_target.TryReadGlobalPointer(Constants.Globals.GlobalFreeHugeRegions, out TargetPointer? globalFreeHugeRegionsPtr))
        {
            TargetPointer freeHugeRegionAddr = _target.ReadPointer(globalFreeHugeRegionsPtr.Value);
            AddFreeList(regions, freeHugeRegionAddr, FreeRegionKind.FreeGlobalHugeRegion);
        }

        // Global regions to decommit
        if (_target.TryReadGlobalPointer(Constants.Globals.GlobalRegionsToDecommit, out TargetPointer? globalRegionsToDecommitPtr))
        {
            AddFreeListArray(regions, globalRegionsToDecommitPtr.Value, countFreeRegionKinds, FreeRegionKind.FreeGlobalRegion);
        }

        if (GetGCType() == GCType.Server)
        {
            AddServerFreeRegions(regions, countFreeRegionKinds);
        }
        else
        {
            AddWorkstationFreeRegions(regions, countFreeRegionKinds);
        }

        return regions;
    }

    private void AddServerFreeRegions(List<GCMemoryRegionData> regions, int countFreeRegionKinds)
    {
        uint heapCount = ((IGC)this).GetGCHeapCount();
        TargetPointer heapTable = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.Heaps));

        uint regionFreeListSize = _target.GetTypeInfo(DataType.RegionFreeList).Size
            ?? throw new InvalidOperationException("RegionFreeList type has no size");

        for (uint i = 0; i < heapCount; i++)
        {
            TargetPointer heapAddress = _target.ReadPointer(heapTable + (i * (uint)_target.PointerSize));
            if (heapAddress == TargetPointer.Null)
                continue;

            Data.GCHeapSVR heap = _target.ProcessedData.GetOrAdd<Data.GCHeapSVR>(heapAddress);

            // Per-heap free regions
            if (((IGCHeap)heap).FreeRegions is TargetPointer freeRegionsAddr)
            {
                for (int j = 0; j < countFreeRegionKinds; j++)
                {
                    TargetPointer regionListAddr = freeRegionsAddr + (ulong)(j * regionFreeListSize);
                    Data.RegionFreeList regionFreeList = new Data.RegionFreeList(_target, regionListAddr);
                    AddSegmentList(regions, regionFreeList.HeadFreeRegion, FreeRegionKind.FreeRegion, (int)i);
                }
            }

            // Per-heap freeable segments
            if (((IGCHeap)heap).FreeableSohSegment is TargetPointer freeableSoh)
                AddSegmentList(regions, freeableSoh, FreeRegionKind.FreeSohSegment, (int)i);
            if (((IGCHeap)heap).FreeableUohSegment is TargetPointer freeableUoh)
                AddSegmentList(regions, freeableUoh, FreeRegionKind.FreeUohSegment, (int)i);
        }
    }

    private void AddWorkstationFreeRegions(List<GCMemoryRegionData> regions, int countFreeRegionKinds)
    {
        GCHeapWKS heap = new GCHeapWKS(_target);

        // Per-heap free regions
        if (heap.FreeRegions is TargetPointer freeRegionsAddr)
        {
            uint regionFreeListSize = _target.GetTypeInfo(DataType.RegionFreeList).Size
                ?? throw new InvalidOperationException("RegionFreeList type has no size");

            for (int i = 0; i < countFreeRegionKinds; i++)
            {
                TargetPointer regionListAddr = freeRegionsAddr + (ulong)(i * regionFreeListSize);
                Data.RegionFreeList regionFreeList = new Data.RegionFreeList(_target, regionListAddr);
                AddSegmentList(regions, regionFreeList.HeadFreeRegion, FreeRegionKind.FreeRegion);
            }
        }

        // Per-heap freeable segments
        if (heap.FreeableSohSegment is TargetPointer freeableSoh)
            AddSegmentList(regions, freeableSoh, FreeRegionKind.FreeSohSegment);
        if (heap.FreeableUohSegment is TargetPointer freeableUoh)
            AddSegmentList(regions, freeableUoh, FreeRegionKind.FreeUohSegment);
    }

    private void AddFreeListArray(List<GCMemoryRegionData> regions, TargetPointer arrayStart, int count, FreeRegionKind kind)
    {
        uint regionFreeListSize = _target.GetTypeInfo(DataType.RegionFreeList).Size
            ?? throw new InvalidOperationException("RegionFreeList type has no size");

        for (int i = 0; i < count; i++)
        {
            TargetPointer regionListAddr = arrayStart + (ulong)(i * regionFreeListSize);
            Data.RegionFreeList regionFreeList = new Data.RegionFreeList(_target, regionListAddr);
            AddFreeList(regions, regionFreeList.HeadFreeRegion, kind);
        }
    }

    private void AddFreeList(List<GCMemoryRegionData> regions, TargetPointer headFreeRegion, FreeRegionKind kind, int heap = 0)
    {
        AddSegmentList(regions, headFreeRegion, kind, heap);
    }

    private void AddSegmentList(List<GCMemoryRegionData> regions, TargetPointer start, FreeRegionKind kind, int heap = 0)
    {
        int iterationMax = 2048;
        TargetPointer curr = start;

        while (curr != TargetPointer.Null)
        {
            Data.HeapSegment segment = _target.ProcessedData.GetOrAdd<Data.HeapSegment>(curr);

            ulong regionStart = segment.Mem;
            ulong regionSize = 0;
            if (segment.Mem < segment.Committed)
                regionSize = segment.Committed - segment.Mem;

            if (regionStart != 0)
            {
                regions.Add(new GCMemoryRegionData()
                {
                    Start = regionStart,
                    Size = regionSize,
                    ExtraData = (ulong)kind,
                    Heap = heap,
                });
            }

            curr = segment.Next;
            if (curr == start)
                break;

            if (--iterationMax <= 0)
                break;
        }
    }
}
