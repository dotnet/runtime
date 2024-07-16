// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class TypeDesc : IData<TypeDesc>
{
    static TypeDesc IData<TypeDesc>.Create(Target target, TargetPointer address) => new TypeDesc(target, address);
    public TypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(TypeAndFlags)].Offset);
    }

    public uint TypeAndFlags { get; init; }
}

internal class ParamTypeDesc : IData<ParamTypeDesc>
{
    static ParamTypeDesc IData<ParamTypeDesc>.Create(Target target, TargetPointer address) => new ParamTypeDesc(target, address);
    public ParamTypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
        TypeAndFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(TypeAndFlags)].Offset);

        type = target.GetTypeInfo(DataType.ParamTypeDesc);
        TypeArg = target.Read<ushort>(address + (ulong)type.Fields[nameof(TypeArg)].Offset);
    }

    public uint TypeAndFlags { get; init; }
    public TargetPointer TypeArg { get; init; }
}

internal class TypeVarTypeDesc : IData<TypeVarTypeDesc>
{
    static TypeVarTypeDesc IData<TypeVarTypeDesc>.Create(Target target, TargetPointer address) => new TypeVarTypeDesc(target, address);
    public TypeVarTypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
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
    static FnPtrTypeDesc IData<FnPtrTypeDesc>.Create(Target target, TargetPointer address) => new FnPtrTypeDesc(target, address);
    public FnPtrTypeDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TypeDesc);
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
