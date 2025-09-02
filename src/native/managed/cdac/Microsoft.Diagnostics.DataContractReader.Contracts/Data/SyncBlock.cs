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

        Monitor = target.ProcessedData.GetOrAdd<AwareLock>(address + (ulong)type.Fields[nameof(Monitor)].Offset);
        InteropInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(InteropInfo)].Offset);
        Link = target.ReadPointer(address + (ulong)type.Fields[nameof(Link)].Offset);
    }

    public AwareLock Monitor { get; }
    public TargetPointer InteropInfo { get; }
    public TargetPointer Link { get; }
}
