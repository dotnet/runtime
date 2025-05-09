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

        TargetPointer interopInfoPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(InteropInfo)].Offset);
        if (interopInfoPointer != TargetPointer.Null)
            InteropInfo = target.ProcessedData.GetOrAdd<InteropSyncBlockInfo>(interopInfoPointer);
    }

    public InteropSyncBlockInfo? InteropInfo { get; init; }
}
