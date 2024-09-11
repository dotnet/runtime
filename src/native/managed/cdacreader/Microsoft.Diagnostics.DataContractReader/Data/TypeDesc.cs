// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class TypeDesc : IData<TypeDesc>
{
    static TypeDesc IData<TypeDesc>.Create(ITarget target, TargetPointer address) => new TypeDesc((Target)target, address);
    public TypeDesc(Target target, TargetPointer address)
    {
        ITarget.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(TypeAndFlags)].Offset);
    }

    public uint TypeAndFlags { get; init; }
}

internal class ParamTypeDesc : IData<ParamTypeDesc>
{
    static ParamTypeDesc IData<ParamTypeDesc>.Create(ITarget target, TargetPointer address) => new ParamTypeDesc((Target)target, address);
    public ParamTypeDesc(Target target, TargetPointer address)
    {
        ITarget.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(TypeAndFlags)].Offset);

        type = target.GetTypeInfo(DataType.ParamTypeDesc);
        TypeArg = target.Read<ushort>(address + (ulong)type.Fields[nameof(TypeArg)].Offset);
    }

    public uint TypeAndFlags { get; init; }
    public TargetPointer TypeArg { get; init; }
}

internal class TypeVarTypeDesc : IData<TypeVarTypeDesc>
{
    static TypeVarTypeDesc IData<TypeVarTypeDesc>.Create(ITarget target, TargetPointer address) => new TypeVarTypeDesc((Target)target, address);
    public TypeVarTypeDesc(Target target, TargetPointer address)
    {
        ITarget.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(TypeAndFlags)].Offset);

        type = target.GetTypeInfo(DataType.TypeVarTypeDesc);

        Module = target.ReadPointer(address + (ulong)type.Fields[nameof(Module)].Offset);
        Token = target.Read<uint>(address + (ulong)type.Fields[nameof(Token)].Offset);
    }

    public uint TypeAndFlags { get; init; }
    public TargetPointer Module { get; init; }
    public uint Token { get; init; }
}

internal class FnPtrTypeDesc : IData<FnPtrTypeDesc>
{
    static FnPtrTypeDesc IData<FnPtrTypeDesc>.Create(ITarget target, TargetPointer address) => new FnPtrTypeDesc((Target)target, address);
    public FnPtrTypeDesc(Target target, TargetPointer address)
    {
        ITarget.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(TypeAndFlags)].Offset);

        type = target.GetTypeInfo(DataType.FnPtrTypeDesc);

        NumArgs = target.Read<uint>(address + (ulong)type.Fields[nameof(NumArgs)].Offset);
        CallConv = target.Read<uint>(address + (ulong)type.Fields[nameof(CallConv)].Offset);
        RetAndArgTypes = (TargetPointer)(address + (ulong)type.Fields[nameof(RetAndArgTypes)].Offset);
    }

    public uint TypeAndFlags { get; init; }
    public uint NumArgs {  get; init; }
    public uint CallConv { get; init; }
    public TargetPointer RetAndArgTypes { get; init; }
}
