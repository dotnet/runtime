// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SyncBlock : IData<SyncBlock>
{
    static SyncBlock IData<SyncBlock>.Create(Target target, TargetPointer address)
        => new SyncBlock(target, address);

    public SyncBlock(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncBlock);

        Address = address;
        TargetPointer interopInfoPointer = target.ReadPointerField(address, type, nameof(InteropInfo));
        if (interopInfoPointer != TargetPointer.Null)
            InteropInfo = target.ProcessedData.GetOrAdd<InteropSyncBlockInfo>(interopInfoPointer);
        ObjectHandle lockHandle = target.ReadDataField<ObjectHandle>(address, type, nameof(Lock));
        if (lockHandle.Handle != TargetPointer.Null)
            Lock = lockHandle;

        ThinLock = target.ReadField<uint>(address, type, nameof(ThinLock));
        LinkNext = target.ReadPointerField(address, type, nameof(LinkNext));
        HashCode = target.ReadField<uint>(address, type, nameof(HashCode));
    }

    public TargetPointer Address { get; init; }
    public InteropSyncBlockInfo? InteropInfo { get; init; }
    public ObjectHandle? Lock { get; init; }
    public uint ThinLock { get; init; }
    public TargetPointer LinkNext { get; init; }
    public uint HashCode { get; init; }
}
