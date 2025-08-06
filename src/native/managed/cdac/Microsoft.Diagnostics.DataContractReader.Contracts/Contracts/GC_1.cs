// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct GC_1 : IGC
{
    private const string GC_TYPE_SVR = "server";
    private const string GC_TYPE_WRK = "workstation";

    private const string HEAP_TYPE_RGN = "regions";
    private const string HEAP_TYPE_SEG = "segments";

    private const uint WRK_HEAP_COUNT = 1;

    private enum GCType
    {
        Unknown,
        Workstation,
        Server,
    }

    private enum HeapType
    {
        Unknown,
        Segments,
        Regions,
    }

    private readonly Target _target;

    internal GC_1(Target target)
    {
        _target = target;
    }

    string[] IGC.GetGCType() => [_target.ReadGlobalString("GCType"), _target.ReadGlobalString("HeapType")];

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

    private GCType GetGCType()
    {
        return _target.ReadGlobalString(Constants.Globals.GCType) switch
        {
            GC_TYPE_WRK => GCType.Workstation,
            GC_TYPE_SVR => GCType.Server,
            _ => GCType.Unknown,
        };
    }

    private HeapType GetHeapType()
    {
        return _target.ReadGlobalString(Constants.Globals.HeapType) switch
        {
            HEAP_TYPE_RGN => HeapType.Regions,
            HEAP_TYPE_SEG => HeapType.Segments,
            _ => HeapType.Unknown,
        };
    }}
