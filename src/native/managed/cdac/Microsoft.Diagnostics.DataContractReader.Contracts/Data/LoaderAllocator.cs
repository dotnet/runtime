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

        ReferenceCount = target.ReadField<uint>(address, type, nameof(ReferenceCount));
        HighFrequencyHeap = target.ReadPointerField(address, type, nameof(HighFrequencyHeap));
        LowFrequencyHeap = target.ReadPointerField(address, type, nameof(LowFrequencyHeap));
        StaticsHeap = target.ReadPointerField(address, type, nameof(StaticsHeap));
        StubHeap = target.ReadPointerField(address, type, nameof(StubHeap));
        ExecutableHeap = target.ReadPointerField(address, type, nameof(ExecutableHeap));

        if (type.Fields.ContainsKey(nameof(FixupPrecodeHeap)))
            FixupPrecodeHeap = target.ReadPointerField(address, type, nameof(FixupPrecodeHeap));
        if (type.Fields.ContainsKey(nameof(NewStubPrecodeHeap)))
            NewStubPrecodeHeap = target.ReadPointerField(address, type, nameof(NewStubPrecodeHeap));
        if (type.Fields.ContainsKey(nameof(DynamicHelpersStubHeap)))
            DynamicHelpersStubHeap = target.ReadPointerField(address, type, nameof(DynamicHelpersStubHeap));

        VirtualCallStubManager = target.ReadPointerField(address, type, nameof(VirtualCallStubManager));

        ObjectHandle = target.ReadDataField<ObjectHandle>(address, type, nameof(ObjectHandle));
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
