// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DynamicStaticsInfo))]
internal sealed partial class DynamicStaticsInfo : IData<DynamicStaticsInfo>
{
    [CustomInit(nameof(InitGCStatics))] public partial TargetPointer GCStatics { get; }
    [CustomInit(nameof(InitNonGCStatics))] public partial TargetPointer NonGCStatics { get; }

    [DataDescriptorDependency(nameof(GCStatics), "pointer")]
    private partial TargetPointer InitGCStatics(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicStaticsInfo);
        TargetPointer mask = target.ReadGlobalPointer(Constants.Globals.StaticsPointerMask);
        return target.ReadPointerField(address, type, nameof(GCStatics)) & mask;
    }

    [DataDescriptorDependency(nameof(NonGCStatics), "pointer")]
    private partial TargetPointer InitNonGCStatics(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicStaticsInfo);
        TargetPointer mask = target.ReadGlobalPointer(Constants.Globals.StaticsPointerMask);
        return target.ReadPointerField(address, type, nameof(NonGCStatics)) & mask;
    }
}
