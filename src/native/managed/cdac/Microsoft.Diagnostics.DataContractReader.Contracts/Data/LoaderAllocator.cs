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
        StubHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(StubHeap)].Offset);
        ObjectHandle = target.ProcessedData.GetOrAdd<ObjectHandle>(
            target.ReadPointer(address + (ulong)type.Fields[nameof(ObjectHandle)].Offset));
        CallCountingManager = target.ReadPointer(address + (ulong)type.Fields[nameof(CallCountingManager)].Offset);
    }

    public uint ReferenceCount { get; init; }
    public TargetPointer HighFrequencyHeap { get; init; }
    public TargetPointer LowFrequencyHeap { get; init; }
    public TargetPointer StubHeap { get; init; }
    public ObjectHandle ObjectHandle { get; init; }
    public TargetPointer CallCountingManager { get; init; }

    public bool IsAlive => ReferenceCount != 0;
}
