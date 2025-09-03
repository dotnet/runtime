// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class GCHeap_svr : IData<GCHeap_svr>
{
    static GCHeap_svr IData<GCHeap_svr>.Create(Target target, TargetPointer address) => new GCHeap_svr(target, address);
    public GCHeap_svr(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.GCHeap);

        MarkArray = target.ReadPointer(address + (ulong)type.Fields[nameof(MarkArray)].Offset);
        NextSweepObj = target.ReadPointer(address + (ulong)type.Fields[nameof(NextSweepObj)].Offset);
        BackgroundMinSavedAddr = target.ReadPointer(address + (ulong)type.Fields[nameof(BackgroundMinSavedAddr)].Offset);
        BackgroundMaxSavedAddr = target.ReadPointer(address + (ulong)type.Fields[nameof(BackgroundMaxSavedAddr)].Offset);
        AllocAllocated = target.ReadPointer(address + (ulong)type.Fields[nameof(AllocAllocated)].Offset);
        EphemeralHeapSegment = target.ReadPointer(address + (ulong)type.Fields[nameof(EphemeralHeapSegment)].Offset);
        CardTable = target.ReadPointer(address + (ulong)type.Fields[nameof(CardTable)].Offset);
        FinalizeQueue = target.ReadPointer(address + (ulong)type.Fields[nameof(FinalizeQueue)].Offset);
        GenerationTable = address + (ulong)type.Fields[nameof(GenerationTable)].Offset;

        // Fields only exist segment GC builds
        if (type.Fields.ContainsKey(nameof(SavedSweepEphemeralSeg)))
            SavedSweepEphemeralSeg = target.ReadPointer(address + (ulong)type.Fields[nameof(SavedSweepEphemeralSeg)].Offset);
        if (type.Fields.ContainsKey(nameof(SavedSweepEphemeralStart)))
            SavedSweepEphemeralStart = target.ReadPointer(address + (ulong)type.Fields[nameof(SavedSweepEphemeralStart)].Offset);
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
}
