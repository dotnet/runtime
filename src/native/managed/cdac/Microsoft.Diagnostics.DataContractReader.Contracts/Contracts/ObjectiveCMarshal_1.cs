// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Data;

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

        TargetPointer? objectsTable = Data.ObjectiveCMarshal.ObjectTrackingInfoTable(_target);
        if (objectsTable is null || objectsTable.Value == TargetPointer.Null)
            return TargetPointer.Null;

        IConditionalWeakTable cwt = _target.Contracts.ConditionalWeakTable;
        if (cwt.TryGetValue(objectsTable.Value, address, out TargetPointer trackingInfoAddress))
        {
            ObjcTrackingInformation trackingInfo = _target.ProcessedData.GetOrAdd<ObjcTrackingInformation>(trackingInfoAddress);
            if (trackingInfo.Memory != TargetPointer.Null)
            {
                const int TAGGED_MEMORY_SIZE_IN_POINTERS = 2;
                size = new TargetNUInt(TAGGED_MEMORY_SIZE_IN_POINTERS * (ulong)_target.PointerSize);
                return trackingInfo.Memory;
            }
        }

        return TargetPointer.Null;
    }
}
