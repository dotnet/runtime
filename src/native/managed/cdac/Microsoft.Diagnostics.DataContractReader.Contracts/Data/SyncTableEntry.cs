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

        TargetPointer objectPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(Object)].Offset);
        if (objectPointer != TargetPointer.Null && (objectPointer & 1) == 0) // Defensive check: if the lowest bit is set, this is a free sync block entry and the pointer is not valid.
            Object = target.ProcessedData.GetOrAdd<Object>(objectPointer);
    }

    public SyncBlock? SyncBlock { get; init; }
    public Object? Object { get; init; }
}
