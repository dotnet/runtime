// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ObjectiveCMarshal_1 : IObjectiveCMarshal
{
    private readonly Target _target;
    private readonly TargetPointer _syncTableEntries;
    private readonly uint _syncBlockIsHashOrSyncBlockIndex;
    private readonly uint _syncBlockIsHashCode;
    private readonly uint _syncBlockIndexMask;

    internal ObjectiveCMarshal_1(Target target, TargetPointer syncTableEntries,
        uint syncBlockIsHashOrSyncBlockIndex, uint syncBlockIsHashCode, uint syncBlockIndexMask)
    {
        _target = target;
        _syncTableEntries = syncTableEntries;
        _syncBlockIsHashOrSyncBlockIndex = syncBlockIsHashOrSyncBlockIndex;
        _syncBlockIsHashCode = syncBlockIsHashCode;
        _syncBlockIndexMask = syncBlockIndexMask;
    }

    public TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size)
    {
        size = new TargetNUInt(2 * (ulong)_target.PointerSize);

        ulong objectHeaderSize = _target.GetTypeInfo(DataType.ObjectHeader).Size!.Value;
        Data.ObjectHeader header = _target.ProcessedData.GetOrAdd<Data.ObjectHeader>(address - objectHeaderSize);
        uint syncBlockValue = header.SyncBlockValue;

        // Check if the sync block value represents a sync block index
        if ((syncBlockValue & (_syncBlockIsHashCode | _syncBlockIsHashOrSyncBlockIndex))
                != _syncBlockIsHashOrSyncBlockIndex)
            return TargetPointer.Null;

        uint index = syncBlockValue & _syncBlockIndexMask;
        ulong offsetInSyncTableEntries = index * (ulong)_target.GetTypeInfo(DataType.SyncTableEntry).Size!;
        Data.SyncTableEntry entry = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + offsetInSyncTableEntries);

        return entry.SyncBlock?.InteropInfo?.TaggedMemory ?? TargetPointer.Null;
    }
}
