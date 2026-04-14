// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class GCHeapSVR : IData<GCHeapSVR>, IGCHeap
{
    static GCHeapSVR IData<GCHeapSVR>.Create(Target target, TargetPointer address) => new GCHeapSVR(target, address);
    public GCHeapSVR(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.GCHeap);

        MarkArray = target.ReadPointerField(address, type, nameof(MarkArray));
        NextSweepObj = target.ReadPointerField(address, type, nameof(NextSweepObj));
        BackgroundMinSavedAddr = target.ReadPointerField(address, type, nameof(BackgroundMinSavedAddr));
        BackgroundMaxSavedAddr = target.ReadPointerField(address, type, nameof(BackgroundMaxSavedAddr));
        AllocAllocated = target.ReadPointerField(address, type, nameof(AllocAllocated));
        EphemeralHeapSegment = target.ReadPointerField(address, type, nameof(EphemeralHeapSegment));
        CardTable = target.ReadPointerField(address, type, nameof(CardTable));
        FinalizeQueue = target.ReadPointerField(address, type, nameof(FinalizeQueue));
        GenerationTable = address + (ulong)type.Fields[nameof(GenerationTable)].Offset;

        // Fields only exist segment GC builds
        if (type.Fields.ContainsKey(nameof(SavedSweepEphemeralSeg)))
            SavedSweepEphemeralSeg = target.ReadPointerField(address, type, nameof(SavedSweepEphemeralSeg));
        if (type.Fields.ContainsKey(nameof(SavedSweepEphemeralStart)))
            SavedSweepEphemeralStart = target.ReadPointerField(address, type, nameof(SavedSweepEphemeralStart));

        OomData = target.ProcessedData.GetOrAdd<OomHistory>(address + (ulong)type.Fields[nameof(OomData)].Offset);

        InternalRootArray = target.ReadPointerField(address, type, nameof(InternalRootArray));
        InternalRootArrayIndex = target.ReadNUIntField(address, type, nameof(InternalRootArrayIndex));
        HeapAnalyzeSuccess = target.ReadField<int>(address, type, nameof(HeapAnalyzeSuccess)) != 0;

        InterestingData = address + (ulong)type.Fields[nameof(InterestingData)].Offset;
        CompactReasons = address + (ulong)type.Fields[nameof(CompactReasons)].Offset;
        ExpandMechanisms = address + (ulong)type.Fields[nameof(ExpandMechanisms)].Offset;
        InterestingMechanismBits = address + (ulong)type.Fields[nameof(InterestingMechanismBits)].Offset;

        if (type.Fields.ContainsKey(nameof(FreeableSohSegment)))
            FreeableSohSegment = target.ReadPointerField(address, type, nameof(FreeableSohSegment));
        if (type.Fields.ContainsKey(nameof(FreeableUohSegment)))
            FreeableUohSegment = target.ReadPointerField(address, type, nameof(FreeableUohSegment));
        if (type.Fields.ContainsKey(nameof(FreeRegions)))
            FreeRegions = address + (ulong)type.Fields[nameof(FreeRegions)].Offset;
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

    public OomHistory OomData { get; }

    public TargetPointer InternalRootArray { get; }
    public TargetNUInt InternalRootArrayIndex { get; }
    public bool HeapAnalyzeSuccess { get; }

    public TargetPointer InterestingData { get; }
    public TargetPointer CompactReasons { get; }
    public TargetPointer ExpandMechanisms { get; }
    public TargetPointer InterestingMechanismBits { get; }

    public TargetPointer? FreeableSohSegment { get; }
    public TargetPointer? FreeableUohSegment { get; }
    public TargetPointer? FreeRegions { get; }
}
