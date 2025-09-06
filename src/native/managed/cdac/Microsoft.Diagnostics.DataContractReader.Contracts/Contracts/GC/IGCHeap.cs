// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCHelpers;

internal interface IGCHeap
{
    TargetPointer MarkArray { get; }
    TargetPointer NextSweepObj { get; }
    TargetPointer BackgroundMinSavedAddr { get; }
    TargetPointer BackgroundMaxSavedAddr { get; }
    TargetPointer AllocAllocated { get; }
    TargetPointer EphemeralHeapSegment { get; }
    TargetPointer CardTable { get; }
    TargetPointer FinalizeQueue { get; }
    TargetPointer GenerationTable { get; }

    TargetPointer? SavedSweepEphemeralSeg { get; }
    TargetPointer? SavedSweepEphemeralStart { get; }

    Data.OOMHistory OOMData { get; }

    TargetPointer InternalRootArray { get; }
    TargetNUInt InternalRootArrayIndex { get; }
    bool HeapAnalyzeSuccess { get; }

    TargetPointer InterestingData { get; }
    TargetPointer CompactReasons { get; }
    TargetPointer ExpandMechanisms { get; }
    TargetPointer InterestingMechanismBits { get; }
}
