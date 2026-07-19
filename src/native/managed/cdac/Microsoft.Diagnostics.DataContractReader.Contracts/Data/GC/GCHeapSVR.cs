// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts.GCHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GCHeap))]
internal sealed partial class GCHeapSVR : IData<GCHeapSVR>, IGCHeap
{
    // Fields only exist in background GC builds
    [Field] public TargetPointer? MarkArray { get; }
    [Field] public TargetPointer? NextSweepObj { get; }
    [Field] public TargetPointer? BackgroundMinSavedAddr { get; }
    [Field] public TargetPointer? BackgroundMaxSavedAddr { get; }
    [Field] public TargetPointer AllocAllocated { get; }
    [Field] public TargetPointer EphemeralHeapSegment { get; }
    [Field] public TargetPointer CardTable { get; }
    [Field] public TargetPointer FinalizeQueue { get; }

    [FieldAddress]
    public TargetPointer GenerationTable { get; }

    // Fields only exist in segment GC builds with background GC
    [Field] public TargetPointer? SavedSweepEphemeralSeg { get; }
    [Field] public TargetPointer? SavedSweepEphemeralStart { get; }

    [Field] public OomHistory OomData { get; }

    [Field] public TargetPointer? InternalRootArray { get; }
    [Field] public TargetNUInt? InternalRootArrayIndex { get; }
    [Field(UnderlyingBoolType = typeof(int))] public bool? HeapAnalyzeSuccess { get; }

    [FieldAddress] public TargetPointer InterestingData { get; }
    [FieldAddress] public TargetPointer CompactReasons { get; }
    [FieldAddress] public TargetPointer ExpandMechanisms { get; }
    [FieldAddress] public TargetPointer InterestingMechanismBits { get; }

    [Field] public TargetPointer? FreeableSohSegment { get; }
    [Field] public TargetPointer? FreeableUohSegment { get; }
    [Field] public TargetPointer? FreeRegions { get; }
}
