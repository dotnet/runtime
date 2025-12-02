// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InteropSyncBlockInfo : IData<InteropSyncBlockInfo>
{
    static InteropSyncBlockInfo IData<InteropSyncBlockInfo>.Create(Target target, TargetPointer address)
        => new InteropSyncBlockInfo(target, address);

    public InteropSyncBlockInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InteropSyncBlockInfo);

        RCW = type.Fields.TryGetValue(nameof(RCW), out Target.FieldInfo rcwField)
            ? target.ReadPointer(address + (ulong)rcwField.Offset)
            : TargetPointer.Null;
        CCW = type.Fields.TryGetValue(nameof(CCW), out Target.FieldInfo ccwField)
            ? target.ReadPointer(address + (ulong)ccwField.Offset)
            : TargetPointer.Null;
    }

    public TargetPointer RCW { get; init; }
    public TargetPointer CCW { get; init; }
}
