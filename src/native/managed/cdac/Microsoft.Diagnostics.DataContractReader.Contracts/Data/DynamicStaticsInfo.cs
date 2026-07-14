// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DynamicStaticsInfo))]
internal sealed partial class DynamicStaticsInfo : IData<DynamicStaticsInfo>
{
    public TargetPointer GCStatics { get; private set; }
    public TargetPointer NonGCStatics { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicStaticsInfo);
        TargetPointer mask = target.ReadGlobalPointer(Constants.Globals.StaticsPointerMask);
        GCStatics = target.ReadPointerField(address, type, nameof(GCStatics)) & mask;
        NonGCStatics = target.ReadPointerField(address, type, nameof(NonGCStatics)) & mask;
    }
}
