// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

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
}
