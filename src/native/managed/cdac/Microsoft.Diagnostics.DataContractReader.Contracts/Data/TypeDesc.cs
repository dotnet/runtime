// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TypeDesc))]
internal sealed partial class TypeDesc : IData<TypeDesc>
{
    [Field] public uint TypeAndFlags { get; }
}

[CdacType(nameof(DataType.ParamTypeDesc))]
internal sealed partial class ParamTypeDesc : IData<ParamTypeDesc>
{
    public uint TypeAndFlags { get; private set; }
    [Field] public TargetPointer TypeArg { get; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.ReadField<uint>(address, type, nameof(TypeAndFlags));
    }
}

[CdacType(nameof(DataType.TypeVarTypeDesc))]
internal sealed partial class TypeVarTypeDesc : IData<TypeVarTypeDesc>
{
    public uint TypeAndFlags { get; private set; }
    [Field] public TargetPointer Module { get; }
    [Field] public uint Token { get; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.ReadField<uint>(address, type, nameof(TypeAndFlags));
    }
}

[CdacType(nameof(DataType.FnPtrTypeDesc))]
internal sealed partial class FnPtrTypeDesc : IData<FnPtrTypeDesc>
{
    public uint TypeAndFlags { get; private set; }
    [Field] public uint NumArgs { get; }
    [Field] public uint CallConv { get; }

    [FieldAddress]
    public TargetPointer RetAndArgTypes { get; }

    [Field] public TargetPointer LoaderModule { get; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.ReadField<uint>(address, type, nameof(TypeAndFlags));
    }
}
