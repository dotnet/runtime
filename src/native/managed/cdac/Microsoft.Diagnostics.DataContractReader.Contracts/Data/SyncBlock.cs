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
        TargetPointer interopInfoPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(InteropInfo)].Offset);
        if (interopInfoPointer != TargetPointer.Null)
            InteropInfo = target.ProcessedData.GetOrAdd<InteropSyncBlockInfo>(interopInfoPointer);
        TargetPointer lockPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(Lock)].Offset);
        if (lockPointer != TargetPointer.Null)
            Lock = target.ProcessedData.GetOrAdd<Object>(lockPointer);

        ThinLock = target.Read<uint>(address + (ulong)type.Fields[nameof(ThinLock)].Offset);
        LinkNext = target.ReadPointer(address + (ulong)type.Fields[nameof(LinkNext)].Offset);
    }

    public TargetPointer Address { get; init; }
    public InteropSyncBlockInfo? InteropInfo { get; init; }
    public Object? Lock { get; init; }
    public uint ThinLock { get; init; }
    public TargetPointer LinkNext { get; init; }
}
