// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts.GCHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GCHeap))]
internal sealed partial class GCHeapSVR : IData<GCHeapSVR>, IGCHeap
{
    // Fields only exist in background GC builds
    [Field] public partial TargetPointer? MarkArray { get; }
    [Field] public partial TargetPointer? NextSweepObj { get; }
    [Field] public partial TargetPointer? BackgroundMinSavedAddr { get; }
    [Field] public partial TargetPointer? BackgroundMaxSavedAddr { get; }
    [Field] public partial TargetPointer AllocAllocated { get; }
    [Field] public partial TargetPointer EphemeralHeapSegment { get; }
    [Field] public partial TargetPointer CardTable { get; }
    [Field] public partial TargetPointer FinalizeQueue { get; }

    [FieldAddress]
    public partial TargetPointer GenerationTable { get; }

    // Fields only exist in segment GC builds with background GC
    [Field] public partial TargetPointer? SavedSweepEphemeralSeg { get; }
    [Field] public partial TargetPointer? SavedSweepEphemeralStart { get; }

    [Field] public partial OomHistory OomData { get; }

    [Field] public partial TargetPointer? InternalRootArray { get; }
    [Field] public partial TargetNUInt? InternalRootArrayIndex { get; }
    [Field(UnderlyingBoolType = typeof(int))] public partial bool? HeapAnalyzeSuccess { get; }

    [FieldAddress] public partial TargetPointer InterestingData { get; }
    [FieldAddress] public partial TargetPointer CompactReasons { get; }
    [FieldAddress] public partial TargetPointer ExpandMechanisms { get; }
    [FieldAddress] public partial TargetPointer InterestingMechanismBits { get; }

    [Field] public partial TargetPointer? FreeableSohSegment { get; }
    [Field] public partial TargetPointer? FreeableUohSegment { get; }
    [Field] public partial TargetPointer? FreeRegions { get; }
}
