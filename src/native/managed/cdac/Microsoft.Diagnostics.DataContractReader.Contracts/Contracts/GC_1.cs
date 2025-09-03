// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

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

    int IGC.GetDynamicAdaptationMode()
    {
        // not enabled = -1
        // dynamic_adaptation_default = 0,
        // dynamic_adaptation_to_application_sizes = 1,
        if (!IsDatasEnabled())
            return -1;
        return _target.Read<int>(_target.ReadGlobalPointer(Constants.Globals.DynamicAdaptationMode));
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

    GCHeapData IGC.WKSGetHeapData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("WKSGetHeapData is only valid for Workstation GC.");

        TargetPointer markArray = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapMarkArray));
        TargetPointer nextSweepObj = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapNextSweepObj));
        TargetPointer backgroundMinSavedAddr = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapBackgroundMinSavedAddr));
        TargetPointer backgroundMaxSavedAddr = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapBackgroundMaxSavedAddr));
        TargetPointer allocAllocated = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapAllocAllocated));
        TargetPointer ephemeralHeapSegment = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapEphemeralHeapSegment));
        TargetPointer cardTable = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapCardTable));

        TargetPointer finalizeQueue = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCHeapFinalizeQueue));
        Data.CFinalize finalize = _target.ProcessedData.GetOrAdd<Data.CFinalize>(finalizeQueue);
        TargetPointer generationTableArrayStart = _target.ReadGlobalPointer(Constants.Globals.GCHeapGenerationTable);

        TargetPointer? savedSweepEphemeralSeg = null;
        TargetPointer? savedSweepEphemeralStart = null;
        if (_target.TryReadGlobalPointer(Constants.Globals.GCHeapSavedSweepEphemeralSeg, out TargetPointer? savedSweepEphemeralSegPtr) &&
            _target.TryReadGlobalPointer(Constants.Globals.GCHeapSavedSweepEphemeralStart, out TargetPointer? savedSweepEphemeralStartPtr))
        {
            savedSweepEphemeralSeg = _target.ReadPointer(savedSweepEphemeralSegPtr.Value);
            savedSweepEphemeralStart = _target.ReadPointer(savedSweepEphemeralStartPtr.Value);
        }

        return new GCHeapData()
        {
            MarkArray = markArray,
            NextSweepObject = nextSweepObj,
            BackGroundSavedMinAddress = backgroundMinSavedAddr,
            BackGroundSavedMaxAddress = backgroundMaxSavedAddr,
            AllocAllocated = allocAllocated,
            EphemeralHeapSegment = ephemeralHeapSegment,
            CardTable = cardTable,
            GenerationTable = GetGenerationData(generationTableArrayStart).AsReadOnly(),
            FillPointers = GetFillPointers(finalize).AsReadOnly(),
            SavedSweepEphemeralSegment = savedSweepEphemeralSeg ?? TargetPointer.Null,
            SavedSweepEphemeralStart = savedSweepEphemeralStart ?? TargetPointer.Null,
        };
    }

    GCHeapData IGC.SVRGetHeapData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetHeapData is only valid for Server GC.");

        Data.GCHeap_svr heap = _target.ProcessedData.GetOrAdd<Data.GCHeap_svr>(heapAddress);
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
        };
    }

    GCOOMData IGC.WKSGetOOMData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("WKSGetHeapData is only valid for Workstation GC.");

        TargetPointer oomHistory = _target.ReadGlobalPointer(Constants.Globals.GCHeapOOMData);
        Data.OOMHistory oomHistoryData = _target.ProcessedData.GetOrAdd<Data.OOMHistory>(oomHistory);
        return GetGCOOMData(oomHistoryData);
    }

    GCOOMData IGC.SVRGetOOMData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetHeapData is only valid for Server GC.");

        Data.GCHeap_svr heap = _target.ProcessedData.GetOrAdd<Data.GCHeap_svr>(heapAddress);
        return GetGCOOMData(heap.OOMData);
    }

    private static GCOOMData GetGCOOMData(Data.OOMHistory oomHistory)
        => new GCOOMData()
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
            fillPointers.Add(_target.ReadPointer(fillPointersArrayStart + i * (ulong)_target.PointerSize));
        return fillPointers;
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
}
