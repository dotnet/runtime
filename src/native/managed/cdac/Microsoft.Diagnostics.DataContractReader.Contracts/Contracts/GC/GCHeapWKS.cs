// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCHelpers;

internal sealed class GCHeapWKS : IGCHeap
{
    public GCHeapWKS(Target target)
    {
        MarkArray = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapMarkArray));
        NextSweepObj = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapNextSweepObj));
        BackgroundMinSavedAddr = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapBackgroundMinSavedAddr));
        BackgroundMaxSavedAddr = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapBackgroundMaxSavedAddr));
        AllocAllocated = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapAllocAllocated));
        EphemeralHeapSegment = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapEphemeralHeapSegment));
        CardTable = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapCardTable));
        FinalizeQueue = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapFinalizeQueue));
        GenerationTable = target.ReadGlobalPointer(Constants.Globals.GCHeapGenerationTable);

        if (target.TryReadGlobalPointer(Constants.Globals.GCHeapSavedSweepEphemeralSeg, out TargetPointer? savedSweepEphemeralSegPtr))
            SavedSweepEphemeralSeg = target.ReadPointer(savedSweepEphemeralSegPtr.Value);
        if (target.TryReadGlobalPointer(Constants.Globals.GCHeapSavedSweepEphemeralStart, out TargetPointer? savedSweepEphemeralStartPtr))
            SavedSweepEphemeralStart = target.ReadPointer(savedSweepEphemeralStartPtr.Value);

        OOMData = target.ProcessedData.GetOrAdd<Data.OOMHistory>(target.ReadGlobalPointer(Constants.Globals.GCHeapOOMData));

        InternalRootArray = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.GCHeapInternalRootArray));
        InternalRootArrayIndex = target.ReadNUInt(target.ReadGlobalPointer(Constants.Globals.GCHeapInternalRootArrayIndex));
        HeapAnalyzeSuccess = target.Read<int>(target.ReadGlobalPointer(Constants.Globals.GCHeapHeapAnalyzeSuccess)) != 0;

        InterestingData = target.ReadGlobalPointer(Constants.Globals.GCHeapInterestingData);
        CompactReasons = target.ReadGlobalPointer(Constants.Globals.GCHeapCompactReasons);
        ExpandMechanisms = target.ReadGlobalPointer(Constants.Globals.GCHeapExpandMechanisms);
        InterestingMechanismBits = target.ReadGlobalPointer(Constants.Globals.GCHeapInterestingMechanismBits);
    }

    public TargetPointer MarkArray { get; }
    public TargetPointer NextSweepObj { get; }
    public TargetPointer BackgroundMinSavedAddr { get; }
    public TargetPointer BackgroundMaxSavedAddr { get; }
    public TargetPointer AllocAllocated { get; }
    public TargetPointer EphemeralHeapSegment { get; }
    public TargetPointer CardTable { get; }
    public TargetPointer FinalizeQueue { get; }
    public TargetPointer GenerationTable { get; }

    public TargetPointer? SavedSweepEphemeralSeg { get; }
    public TargetPointer? SavedSweepEphemeralStart { get; }

    public Data.OOMHistory OOMData { get; }

    public TargetPointer InternalRootArray { get; }
    public TargetNUInt InternalRootArrayIndex { get; }
    public bool HeapAnalyzeSuccess { get; }

    public TargetPointer InterestingData { get; }
    public TargetPointer CompactReasons { get; }
    public TargetPointer ExpandMechanisms { get; }
    public TargetPointer InterestingMechanismBits { get; }
}
