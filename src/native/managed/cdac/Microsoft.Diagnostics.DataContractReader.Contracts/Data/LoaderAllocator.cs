// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class LoaderAllocator : IData<LoaderAllocator>
{
    static LoaderAllocator IData<LoaderAllocator>.Create(Target target, TargetPointer address)
        => new LoaderAllocator(target, address);

    public LoaderAllocator(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LoaderAllocator);

        ReferenceCount = target.Read<uint>(address + (ulong)type.Fields[nameof(ReferenceCount)].Offset);
        HighFrequencyHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(HighFrequencyHeap)].Offset);
        LowFrequencyHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(LowFrequencyHeap)].Offset);
        StaticsHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(StaticsHeap)].Offset);
        StubHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(StubHeap)].Offset);
        ExecutableHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(ExecutableHeap)].Offset);

        if (type.Fields.ContainsKey(nameof(FixupPrecodeHeap)))
            FixupPrecodeHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(FixupPrecodeHeap)].Offset);
        if (type.Fields.ContainsKey(nameof(NewStubPrecodeHeap)))
            NewStubPrecodeHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(NewStubPrecodeHeap)].Offset);
        if (type.Fields.ContainsKey(nameof(DynamicHelpersStubHeap)))
            DynamicHelpersStubHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(DynamicHelpersStubHeap)].Offset);

        VirtualCallStubManager = target.ReadPointer(address + (ulong)type.Fields[nameof(VirtualCallStubManager)].Offset);

        ObjectHandle = target.ProcessedData.GetOrAdd<ObjectHandle>(
            target.ReadPointer(address + (ulong)type.Fields[nameof(ObjectHandle)].Offset));
    }

    public uint ReferenceCount { get; init; }
    public TargetPointer HighFrequencyHeap { get; init; }
    public TargetPointer LowFrequencyHeap { get; init; }
    public TargetPointer StaticsHeap { get; init; }
    public TargetPointer StubHeap { get; init; }
    public TargetPointer ExecutableHeap { get; init; }
    public TargetPointer? FixupPrecodeHeap { get; init; }
    public TargetPointer? NewStubPrecodeHeap { get; init; }
    public TargetPointer? DynamicHelpersStubHeap { get; init; }
    public TargetPointer VirtualCallStubManager { get; init; }
    public ObjectHandle ObjectHandle { get; init; }

    public bool IsAlive => ReferenceCount != 0;
}
