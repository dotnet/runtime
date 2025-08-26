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
        return gcIdentifiers.Split(", ");
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

    IEnumerable<TargetPointer> IGC.GetGCHeaps()
    {
        if (GetGCType() != GCType.Server)
        {
            yield break; // Only server GC has multiple heaps
        }

        uint heapCount = ((IGC)this).GetGCHeapCount();
        TargetPointer ppHeapTable = _target.ReadGlobalPointer(Constants.Globals.Heaps);
        TargetPointer pHeapTable = _target.ReadPointer(ppHeapTable);
        for (uint i = 0; i < heapCount; i++)
        {
            yield return _target.ReadPointer(pHeapTable + (i * (uint)_target.PointerSize));
        }
    }

    GCHeapData IGC.SVRGetHeapData(TargetPointer heapAddress)
    {
        if (GetGCType() != GCType.Server)
            throw new InvalidOperationException("GetHeapData is only valid for Server GC.");

        Data.GCHeap_svr heap = _target.ProcessedData.GetOrAdd<Data.GCHeap_svr>(heapAddress);
        Data.CFinalize finalize = _target.ProcessedData.GetOrAdd<Data.CFinalize>(heap.FinalizeQueue);

        IList<GCGenerationData> generationDataList = heap.GenerationTable.Select(gen =>
            new GCGenerationData()
            {
                StartSegment = gen.StartSegment,
                AllocationStart = gen.AllocationStart ?? unchecked((ulong)-1),
                AllocationContextPointer = gen.AllocationContext.Pointer,
                AllocationContextLimit = gen.AllocationContext.Limit,
            }).ToList();

        return new GCHeapData()
        {
            MarkArray = heap.MarkArray,
            NextSweepObject = heap.NextSweepObj,
            BackGroundSavedMinAddress = heap.BackgroundMinSavedAddr,
            BackGroundSavedMaxAddress = heap.BackgroundMaxSavedAddr,
            AllocAllocated = heap.AllocAllocated,
            EphemeralHeapSegment = heap.EphemeralHeapSegment,
            CardTable = heap.CardTable,
            GenerationTable = generationDataList.AsReadOnly(),
            FillPointers = finalize.FillPointers,
            SavedSweepEphemeralSegment = heap.SavedSweepEphemeralSeg ?? TargetPointer.Null,
            SavedSweepEphemeralStart = heap.SavedSweepEphemeralStart ?? TargetPointer.Null,
        };
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

        uint generationTableLength = _target.ReadGlobal<uint>(Constants.Globals.TotalGenerationCount);
        uint generationSize = _target.GetTypeInfo(DataType.Generation).Size ?? throw new InvalidOperationException("Type Generation has no size");
        TargetPointer generationTableArrayStart = _target.ReadGlobalPointer(Constants.Globals.GCHeapGenerationTable);
        List<Data.Generation> generationTable = [];
        for (uint i = 0; i < generationTableLength; i++)
        {
            TargetPointer generationAddress = generationTableArrayStart + i * generationSize;
            generationTable.Add(_target.ProcessedData.GetOrAdd<Data.Generation>(generationAddress));
        }
        IList<GCGenerationData> generationDataList = generationTable.Select(gen =>
        new GCGenerationData()
        {
            StartSegment = gen.StartSegment,
            AllocationStart = gen.AllocationStart ?? unchecked((ulong)-1),
            AllocationContextPointer = gen.AllocationContext.Pointer,
            AllocationContextLimit = gen.AllocationContext.Limit,
        }).ToList();

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
            GenerationTable = generationDataList.AsReadOnly(),
            FillPointers = finalize.FillPointers,
            SavedSweepEphemeralSegment = savedSweepEphemeralSeg ?? TargetPointer.Null,
            SavedSweepEphemeralStart = savedSweepEphemeralStart ?? TargetPointer.Null,
        };
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
}
