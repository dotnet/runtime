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

    public const string DynamicHeapCount = "dynamic_heap";
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
    public TargetPointer SavedSweepEphemeralSegment { get; init; } /* Only valid in segment GC builds */
    public TargetPointer SavedSweepEphemeralStart { get; init; } /* Only valid in segment GC builds */

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

public readonly struct GCOOMData
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

public interface IGC : IContract
{
    static string IContract.Name { get; } = nameof(GC);

    string[] GetGCIdentifiers() => throw new NotImplementedException();

    uint GetGCHeapCount() => throw new NotImplementedException();
    bool GetGCStructuresValid() => throw new NotImplementedException();
    uint GetMaxGeneration() => throw new NotImplementedException();
    void GetGCBounds(out TargetPointer minAddr, out TargetPointer maxAddr) => throw new NotImplementedException();
    uint GetCurrentGCState() => throw new NotImplementedException();
    bool TryGetGCDynamicAdaptationMode(out int mode) => throw new NotImplementedException();
    GCHeapSegmentData GetHeapSegmentData(TargetPointer segmentAddress) => throw new NotImplementedException();
    IReadOnlyList<TargetNUInt> GetGlobalMechanisms() => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetGCHeaps() => throw new NotImplementedException();

    /* WKS only APIs */
    GCHeapData WKSGetHeapData() => throw new NotImplementedException();
    GCOOMData WKSGetOOMData() => throw new NotImplementedException();

    /* SVR only APIs */
    GCHeapData SVRGetHeapData(TargetPointer heapAddress) => throw new NotImplementedException();
    GCOOMData SVRGetOOMData(TargetPointer heapAddress) => throw new NotImplementedException();
}

public readonly struct GC : IGC
{
    // Everything throws NotImplementedException
}
