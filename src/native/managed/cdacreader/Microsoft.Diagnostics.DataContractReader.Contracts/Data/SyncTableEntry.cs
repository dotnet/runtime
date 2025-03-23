// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SyncTableEntry : IData<SyncTableEntry>
{
    static SyncTableEntry IData<SyncTableEntry>.Create(Target target, TargetPointer address)
        => new SyncTableEntry(target, address);

    public SyncTableEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncTableEntry);

        TargetPointer syncBlockPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(SyncBlock)].Offset);
        if (syncBlockPointer != TargetPointer.Null)
            SyncBlock = target.ProcessedData.GetOrAdd<SyncBlock>(syncBlockPointer);
    }

    public SyncBlock? SyncBlock { get; init; }
}
