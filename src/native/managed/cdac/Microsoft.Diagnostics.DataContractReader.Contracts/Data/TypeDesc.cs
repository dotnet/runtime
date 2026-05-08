// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class TypeDesc : IData<TypeDesc>
{
    static TypeDesc IData<TypeDesc>.Create(Target target, TargetPointer address) => new TypeDesc(target, address);
    public TypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.ReadField<uint>(address, type, nameof(TypeAndFlags));
    }

    public uint TypeAndFlags { get; init; }
}

internal sealed class ParamTypeDesc : IData<ParamTypeDesc>
{
    static ParamTypeDesc IData<ParamTypeDesc>.Create(Target target, TargetPointer address) => new ParamTypeDesc(target, address);
    public ParamTypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.ReadField<uint>(address, type, nameof(TypeAndFlags));

        type = target.GetTypeInfo(DataType.ParamTypeDesc);
        TypeArg = target.ReadPointerField(address, type, nameof(TypeArg));
    }

    public uint TypeAndFlags { get; init; }
    public TargetPointer TypeArg { get; init; }
}

internal sealed class TypeVarTypeDesc : IData<TypeVarTypeDesc>
{
    static TypeVarTypeDesc IData<TypeVarTypeDesc>.Create(Target target, TargetPointer address) => new TypeVarTypeDesc(target, address);
    public TypeVarTypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.ReadField<uint>(address, type, nameof(TypeAndFlags));

        type = target.GetTypeInfo(DataType.TypeVarTypeDesc);

        Module = target.ReadPointerField(address, type, nameof(Module));
        Token = target.ReadField<uint>(address, type, nameof(Token));
    }

    public uint TypeAndFlags { get; init; }
    public TargetPointer Module { get; init; }
    public uint Token { get; init; }
}

internal sealed class FnPtrTypeDesc : IData<FnPtrTypeDesc>
{
    static FnPtrTypeDesc IData<FnPtrTypeDesc>.Create(Target target, TargetPointer address) => new FnPtrTypeDesc(target, address);
    public FnPtrTypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.ReadField<uint>(address, type, nameof(TypeAndFlags));

        type = target.GetTypeInfo(DataType.FnPtrTypeDesc);

        NumArgs = target.ReadField<uint>(address, type, nameof(NumArgs));
        CallConv = target.ReadField<uint>(address, type, nameof(CallConv));
        RetAndArgTypes = (TargetPointer)(address + (ulong)type.Fields[nameof(RetAndArgTypes)].Offset);
        LoaderModule = target.ReadPointerField(address, type, nameof(LoaderModule));
    }

    public uint TypeAndFlags { get; init; }
    public uint NumArgs {  get; init; }
    public uint CallConv { get; init; }
    public TargetPointer RetAndArgTypes { get; init; }
    public TargetPointer LoaderModule { get; init; }
}
