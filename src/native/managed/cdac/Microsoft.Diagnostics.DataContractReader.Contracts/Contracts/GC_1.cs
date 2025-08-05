// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct GC_1 : IGC
{
    private const string GC_TYPE_SVR = "server";
    private const string GC_TYPE_WRK = "workstation";
    private const uint WRK_HEAP_COUNT = 1;
    private readonly Target _target;

    internal GC_1(Target target)
    {
        _target = target;
    }

    GCHeapType IGC.GetGCHeapType() => GetGCHeapType();

    uint IGC.GetGCHeapCount()
    {
        switch (GetGCHeapType())
        {
            case GCHeapType.Workstation:
                return WRK_HEAP_COUNT; // Workstation GC has a single heap
            case GCHeapType.Server:
                TargetPointer pNumHeaps = _target.ReadGlobalPointer(Constants.Globals.NumHeaps);
                return (uint)_target.Read<int>(pNumHeaps);
            default:
                throw new NotImplementedException("Unknown GC heap type");
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

    private GCHeapType GetGCHeapType()
    {
        return _target.ReadGlobalString(Constants.Globals.HeapType) switch
        {
            GC_TYPE_WRK => GCHeapType.Workstation,
            GC_TYPE_SVR => GCHeapType.Server,
            _ => GCHeapType.Unknown,
        };
    }
}
