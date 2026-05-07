// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ObjectiveCMarshal_1 : IObjectiveCMarshal
{
    private readonly Target _target;

    internal ObjectiveCMarshal_1(Target target)
    {
        _target = target;
    }

    public TargetPointer GetTaggedMemory(TargetPointer address, out TargetNUInt size)
    {
        size = default;

        TargetPointer syncBlock = _target.Contracts.Object.GetSyncBlockAddress(address);
        if (syncBlock == TargetPointer.Null)
            return TargetPointer.Null;

        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);
        TargetPointer taggedMemory = sb.InteropInfo?.TaggedMemory ?? TargetPointer.Null;
        if (taggedMemory != TargetPointer.Null)
            size = new TargetNUInt(2 * (ulong)_target.PointerSize);
        return taggedMemory;
    }
}
