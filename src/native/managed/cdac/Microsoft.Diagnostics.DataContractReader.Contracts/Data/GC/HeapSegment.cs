// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HeapSegment : IData<HeapSegment>
{
    static HeapSegment IData<HeapSegment>.Create(Target target, TargetPointer address) => new HeapSegment(target, address);
    public HeapSegment(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HeapSegment);

        Allocated = target.ReadPointer(address + (ulong)type.Fields[nameof(Allocated)].Offset);
        Committed = target.ReadPointer(address + (ulong)type.Fields[nameof(Committed)].Offset);
        Reserved = target.ReadPointer(address + (ulong)type.Fields[nameof(Reserved)].Offset);
        Used = target.ReadPointer(address + (ulong)type.Fields[nameof(Used)].Offset);
        Mem = target.ReadPointer(address + (ulong)type.Fields[nameof(Mem)].Offset);
        Flags = target.ReadNUInt(address + (ulong)type.Fields[nameof(Flags)].Offset);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        BackgroundAllocated = target.ReadPointer(address + (ulong)type.Fields[nameof(BackgroundAllocated)].Offset);

        // Field only exists in MULTIPLE_HEAPS builds
        if (type.Fields.ContainsKey(nameof(Heap)))
            Heap = target.ReadPointer(address + (ulong)type.Fields[nameof(Heap)].Offset);
    }

    public TargetPointer Allocated { get; }
    public TargetPointer Committed { get; }
    public TargetPointer Reserved { get; }
    public TargetPointer Used { get; }
    public TargetPointer Mem { get; }
    public TargetNUInt Flags { get; }
    public TargetPointer Next { get; }
    public TargetPointer BackgroundAllocated { get; }
    public TargetPointer? Heap { get; }
}
