// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEClass : IData<EEClass>
{
    static EEClass IData<EEClass>.Create(Target target, TargetPointer address) => new EEClass(target, address);
    public EEClass(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEClass);

        MethodTable = target.ReadPointerField(address, type, nameof(MethodTable));
        MethodDescChunk = target.ReadPointerField(address, type, nameof(MethodDescChunk));
        NumMethods = target.ReadField<ushort>(address, type, nameof(NumMethods));
        CorTypeAttr = target.ReadField<uint>(address, type, nameof(CorTypeAttr));
        InternalCorElementType = target.ReadField<byte>(address, type, nameof(InternalCorElementType));
        NumInstanceFields = target.ReadField<ushort>(address, type, nameof(NumInstanceFields));
        NumStaticFields = target.ReadField<ushort>(address, type, nameof(NumStaticFields));
        NumThreadStaticFields = target.ReadField<ushort>(address, type, nameof(NumThreadStaticFields));
        FieldDescList = target.ReadPointerField(address, type, nameof(FieldDescList));
        NumNonVirtualSlots = target.ReadField<ushort>(address, type, nameof(NumNonVirtualSlots));
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
