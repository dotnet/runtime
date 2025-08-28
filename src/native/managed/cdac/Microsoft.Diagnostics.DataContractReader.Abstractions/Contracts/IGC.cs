// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public static class GCIdentifiers
{
    public const string Server = "server";
    public const string Workstation = "workstation";

    public const string Regions = "regions";
    public const string Segments = "segments";

    public const string Background = "background";
}

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

public interface IGC : IContract
{
    static string IContract.Name { get; } = nameof(GC);

    string[] GetGCIdentifiers() => throw new NotImplementedException();

    uint GetGCHeapCount() => throw new NotImplementedException();
    bool GetGCStructuresValid() => throw new NotImplementedException();
    uint GetMaxGeneration() => throw new NotImplementedException();
    void GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr) => throw new NotImplementedException();
    uint GetCurrentGCState() => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetGCHeaps() => throw new NotImplementedException();

    /* WKS only APIs */
    GCHeapData WKSGetHeapData() => throw new NotImplementedException();

    /* SVR only APIs */
    GCHeapData SVRGetHeapData(TargetPointer heapAddress) => throw new NotImplementedException();
}

public readonly struct GC : IGC
{
    // Everything throws NotImplementedException
}
