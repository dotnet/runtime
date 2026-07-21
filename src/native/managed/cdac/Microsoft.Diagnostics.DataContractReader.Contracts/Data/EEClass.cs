// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EEClass))]
internal sealed partial class EEClass : IData<EEClass>
{
    [Field] public TargetPointer MethodTable { get; }
    [Field] public TargetPointer MethodDescChunk { get; }
    [Field] public ushort NumMethods { get; }
    [Field] public uint CorTypeAttr { get; }

    // An InternalCorElementType uses the enum values of a CorElementType to
    // indicate some of the information about the type of the type which uses
    // the EEClass
    //
    // In particular. All reference types are ELEMENT_TYPE_CLASS
    // Enums are the element type of their underlying type
    // ValueTypes which can exactly be represented as an element type are represented as such
    [Field] public byte InternalCorElementType { get; }
    [Field] public ushort NumInstanceFields { get; }
    [Field] public ushort NumStaticFields { get; }
    [Field] public ushort NumThreadStaticFields { get; }
    [Field] public TargetPointer FieldDescList { get; }
    [Field] public ushort NumNonVirtualSlots { get; }
    [Field] public byte BaseSizePadding { get; }
    [Field] public TargetPointer OptionalFields { get; }
}
