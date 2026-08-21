// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EEClass))]
internal sealed partial class EEClass : IData<EEClass>
{
    [Field] public partial TargetPointer MethodTable { get; }
    [Field] public partial TargetPointer MethodDescChunk { get; }
    [Field] public partial ushort NumMethods { get; }
    [Field] public partial uint CorTypeAttr { get; }

    // An InternalCorElementType uses the enum values of a CorElementType to
    // indicate some of the information about the type of the type which uses
    // the EEClass
    //
    // In particular. All reference types are ELEMENT_TYPE_CLASS
    // Enums are the element type of their underlying type
    // ValueTypes which can exactly be represented as an element type are represented as such
    [Field] public partial byte InternalCorElementType { get; }
    [Field] public partial ushort NumInstanceFields { get; }
    [Field] public partial ushort NumStaticFields { get; }
    [Field] public partial ushort NumThreadStaticFields { get; }
    [Field] public partial TargetPointer FieldDescList { get; }
    [Field] public partial ushort NumNonVirtualSlots { get; }
    [Field] public partial byte BaseSizePadding { get; }
    [Field] public partial TargetPointer OptionalFields { get; }

    // EEClass::VMFLAG_HASLAYOUT -- the EEClass is a LayoutEEClass carrying managed layout info.
    private const uint HasLayoutFlag = 0x00000040;

    // Descriptor-optional for runtimes that predate GetClassAlignmentRequirement.
    [CustomInit(nameof(InitVMFlags))] public partial uint VMFlags { get; }

    [DataDescriptorDependency(nameof(VMFlags), "uint32")]
    private partial uint InitVMFlags(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEClass);
        return target.ReadFieldOrDefault<uint>(address, type, nameof(VMFlags));
    }

    /// <summary>
    /// Mirrors <c>EEClass::HasLayout</c>. When true the class is a <see cref="LayoutEEClass"/> and
    /// its <see cref="LayoutEEClass.LayoutInfo"/> may be read.
    /// </summary>
    public bool HasLayout => (VMFlags & HasLayoutFlag) != 0;
}
