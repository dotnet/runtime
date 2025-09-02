// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SyncBlockCache : IData<SyncBlockCache>
{
    static SyncBlockCache IData<SyncBlockCache>.Create(Target target, TargetPointer address)
        => new SyncBlockCache(target, address);

    public SyncBlockCache(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncBlockCache);

        FreeSyncTableIndex = target.Read<uint>(address + (ulong)type.Fields[nameof(FreeSyncTableIndex)].Offset);
    }

    public uint FreeSyncTableIndex { get; }
}
