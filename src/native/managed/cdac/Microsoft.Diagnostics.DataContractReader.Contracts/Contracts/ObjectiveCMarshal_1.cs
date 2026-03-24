// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ObjectiveCMarshal_1 : IObjectiveCMarshal
{
    private readonly Target _target;
    private readonly uint _syncBlockIsHashOrSyncBlockIndex;
    private readonly uint _syncBlockIsHashCode;
    private readonly uint _syncBlockIndexMask;

    internal ObjectiveCMarshal_1(Target target,
        uint syncBlockIsHashOrSyncBlockIndex, uint syncBlockIsHashCode, uint syncBlockIndexMask)
    {
        _target = target;
        _syncBlockIsHashOrSyncBlockIndex = syncBlockIsHashOrSyncBlockIndex;
        _syncBlockIsHashCode = syncBlockIsHashCode;
        _syncBlockIndexMask = syncBlockIndexMask;
    }

    public bool TryGetTaggedMemory(TargetPointer address, out TargetNUInt size, out TargetPointer taggedMemory)
    {
        size = default;
        taggedMemory = TargetPointer.Null;

        ulong objectHeaderSize = _target.GetTypeInfo(DataType.ObjectHeader).Size!.Value;
        Data.ObjectHeader header = _target.ProcessedData.GetOrAdd<Data.ObjectHeader>(address - objectHeaderSize);
        uint syncBlockValue = header.SyncBlockValue;

        // Check if the sync block value represents a sync block index
        if ((syncBlockValue & (_syncBlockIsHashCode | _syncBlockIsHashOrSyncBlockIndex))
                != _syncBlockIsHashOrSyncBlockIndex)
            return false;

        uint index = syncBlockValue & _syncBlockIndexMask;
        TargetPointer syncBlock = _target.Contracts.SyncBlock.GetSyncBlock(index);
        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);

        taggedMemory = sb.InteropInfo?.TaggedMemory ?? TargetPointer.Null;
        if (taggedMemory != TargetPointer.Null)
            size = new TargetNUInt(2 * (ulong)_target.PointerSize);
        return taggedMemory != TargetPointer.Null;
    }
}
