// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ObjectiveCMarshal_2 : IObjectiveCMarshal
{
    private readonly Target _target;

    internal ObjectiveCMarshal_2(Target target)
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
        if (!cwt.TryGetValue(objectsTable.Value, address, out TargetPointer trackingInfoAddress))
            return TargetPointer.Null;

        ObjcTrackingInformation trackingInfo = _target.ProcessedData.GetOrAdd<ObjcTrackingInformation>(trackingInfoAddress);
        if (trackingInfo.Memory == TargetPointer.Null)
            return TargetPointer.Null;

        size = new TargetNUInt(2 * (ulong)_target.PointerSize);
        return trackingInfo.Memory;
    }
}
