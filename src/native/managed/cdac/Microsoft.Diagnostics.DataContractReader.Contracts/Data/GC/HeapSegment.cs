// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HeapSegment : IData<HeapSegment>
{
    static HeapSegment IData<HeapSegment>.Create(Target target, TargetPointer address) => new HeapSegment(target, address);
    public HeapSegment(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HeapSegment);

        Allocated = target.ReadPointerField(address, type, nameof(Allocated));
        Committed = target.ReadPointerField(address, type, nameof(Committed));
        Reserved = target.ReadPointerField(address, type, nameof(Reserved));
        Used = target.ReadPointerField(address, type, nameof(Used));
        Mem = target.ReadPointerField(address, type, nameof(Mem));
        Flags = target.ReadNUIntField(address, type, nameof(Flags));
        Next = target.ReadPointerField(address, type, nameof(Next));
        BackgroundAllocated = target.ReadPointerField(address, type, nameof(BackgroundAllocated));

        // Field only exists in MULTIPLE_HEAPS builds
        if (type.Fields.ContainsKey(nameof(Heap)))
            Heap = target.ReadPointerField(address, type, nameof(Heap));
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
