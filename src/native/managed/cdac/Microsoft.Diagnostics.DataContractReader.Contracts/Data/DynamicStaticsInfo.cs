// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class DynamicStaticsInfo : IData<DynamicStaticsInfo>
{
    static DynamicStaticsInfo IData<DynamicStaticsInfo>.Create(Target target, TargetPointer address) => new DynamicStaticsInfo(target, address);
    public DynamicStaticsInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicStaticsInfo);
        TargetPointer mask = target.ReadGlobalPointer(Constants.Globals.StaticsPointerMask);
        GCStatics = target.ReadPointerField(address, type, nameof(GCStatics)) & mask;
        NonGCStatics = target.ReadPointerField(address, type, nameof(NonGCStatics)) & mask;
    }
    public TargetPointer GCStatics { get; init; }
    public TargetPointer NonGCStatics { get; init; }
}
