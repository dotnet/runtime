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

    GCHeapData IGC.WKSGetHeapData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("WKSGetHeapData is only valid for Workstation GC.");

        return GetGCHeapDataFromHeap(new GCHeapWKS(_target));
    }

    GCHeapData IGC.SVRGetHeapData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetHeapData is only valid for Server GC.");

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

    GCOomData IGC.WKSGetOomData()
    {
        if (GetGCType() != GCType.Workstation)
            throw new InvalidOperationException("WKSGetHeapData is only valid for Workstation GC.");

        TargetPointer oomHistory = _target.ReadGlobalPointer(Constants.Globals.GCHeapOomData);
        Data.OomHistory oomHistoryData = _target.ProcessedData.GetOrAdd<Data.OomHistory>(oomHistory);
        return GetGCOomData(oomHistoryData);
    }

    GCOomData IGC.SVRGetOomData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetHeapData is only valid for Server GC.");

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

    private sealed class GCHeap_WKS
    {

    }
}
