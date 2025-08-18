// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEClass : IData<EEClass>
{
    static EEClass IData<EEClass>.Create(Target target, TargetPointer address) => new EEClass(target, address);
    public EEClass(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEClass);

        MethodTable = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodTable)].Offset);
        MethodDescChunk = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDescChunk)].Offset);
        NumMethods = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumMethods)].Offset);
        CorTypeAttr = target.Read<uint>(address + (ulong)type.Fields[nameof(CorTypeAttr)].Offset);
        InternalCorElementType = target.Read<byte>(address + (ulong)type.Fields[nameof(InternalCorElementType)].Offset);
        NumInstanceFields = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumInstanceFields)].Offset);
        NumStaticFields = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumStaticFields)].Offset);
        NumThreadStaticFields = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumThreadStaticFields)].Offset);
        FieldDescList = target.Read<ulong>(address + (ulong)type.Fields[nameof(FieldDescList)].Offset);
        NumNonVirtualSlots = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumNonVirtualSlots)].Offset);
    }

    public TargetPointer MethodTable { get; init; }
    public TargetPointer MethodDescChunk { get; init; }
    public ushort NumMethods { get; init; }
    public uint CorTypeAttr { get; init; }

    // An InternalCorElementType uses the enum values of a CorElementType to
    // indicate some of the information about the type of the type which uses
    // the EEClass
    //
    // In particular. All reference types are ELEMENT_TYPE_CLASS
    // Enums are the element type of their underlying type
    // ValueTypes which can exactly be represented as an element type are represented as such
    public byte InternalCorElementType { get; init; }
    public ushort NumInstanceFields { get; init; }
    public ushort NumStaticFields { get; init; }
    public ushort NumThreadStaticFields { get; init; }
    public TargetPointer FieldDescList { get; init; }
    public ushort NumNonVirtualSlots { get; init; }
}

internal sealed class ArrayClass : IData<ArrayClass>
{
    static ArrayClass IData<ArrayClass>.Create(Target target, TargetPointer address) => new ArrayClass(target, address);
    public ArrayClass(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ArrayClass);

        Rank = target.Read<byte>(address + (ulong)type.Fields[nameof(Rank)].Offset);
    }

    public byte Rank { get; init; }
}
