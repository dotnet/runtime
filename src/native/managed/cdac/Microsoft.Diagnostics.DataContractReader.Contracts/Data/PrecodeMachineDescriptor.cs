// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PrecodeMachineDescriptor))]
internal sealed partial class PrecodeMachineDescriptor : IData<PrecodeMachineDescriptor>
{
    [Field] public partial byte InvalidPrecodeType { get; }
    [Field] public partial byte StubPrecodeType { get; }
    [Field] public partial uint StubCodePageSize { get; }

    [DataDescriptorDependency(nameof(OffsetOfPrecodeType), "uint8")]
    public byte? OffsetOfPrecodeType { get; private set; } // Not present for version 3 and above

    [DataDescriptorDependency(nameof(ReadWidthOfPrecodeType), "uint8")]
    public byte? ReadWidthOfPrecodeType { get; private set; } // Not present for version 3 and above

    [DataDescriptorDependency(nameof(ShiftOfPrecodeType), "uint8")]
    public byte? ShiftOfPrecodeType { get; private set; } // Not present for version 3 and above

    [DataDescriptorDependency(nameof(PInvokeImportPrecodeType), "uint8")]
    public byte? PInvokeImportPrecodeType { get; private set; }

    [DataDescriptorDependency(nameof(FixupPrecodeType), "uint8")]
    public byte? FixupPrecodeType { get; private set; }

    [DataDescriptorDependency(nameof(ThisPointerRetBufPrecodeType), "uint8")]
    public byte? ThisPointerRetBufPrecodeType { get; private set; }

    [DataDescriptorDependency(nameof(InterpreterPrecodeType), "uint8")]
    public byte? InterpreterPrecodeType { get; private set; } // May be present for version 3 and above

    [DataDescriptorDependency(nameof(UMEntryPrecodeType), "uint8")]
    public byte? UMEntryPrecodeType { get; private set; } // May be present for version 3 and above

    [DataDescriptorDependency(nameof(DynamicHelperPrecodeType), "uint8")]
    public byte? DynamicHelperPrecodeType { get; private set; } // May be present for version 3 and above

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    public byte? FixupStubPrecodeSize { get; private set; } // Present for version 3 and above

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(FixupBytes), "uint8[]")]
    public byte[]? FixupBytes { get; private set; } // Present for version 3 and above

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(FixupIgnoredBytes), "uint8[]")]
    public byte[]? FixupIgnoredBytes { get; private set; } // Present for version 3 and above

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    public byte? StubPrecodeSize { get; private set; } // Present for version 3 and above

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(StubBytes), "uint8[]")]
    public byte[]? StubBytes { get; private set; } // Present for version 3 and above

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(StubIgnoredBytes), "uint8[]")]
    public byte[]? StubIgnoredBytes { get; private set; } // Present for version 3 and above

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PrecodeMachineDescriptor);
        if (type.Fields.ContainsKey(nameof(OffsetOfPrecodeType)))
        {
            OffsetOfPrecodeType = target.ReadField<byte>(address, type, nameof(OffsetOfPrecodeType));
            ReadWidthOfPrecodeType = target.ReadField<byte>(address, type, nameof(ReadWidthOfPrecodeType));
            ShiftOfPrecodeType = target.ReadField<byte>(address, type, nameof(ShiftOfPrecodeType));
        }

        if (type.Fields.ContainsKey(nameof(FixupStubPrecodeSize)))
        {
            FixupStubPrecodeSize = target.ReadField<byte>(address, type, nameof(FixupStubPrecodeSize));
            FixupBytes = new byte[FixupStubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(FixupBytes)].Offset, FixupBytes);
            FixupIgnoredBytes = new byte[FixupStubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(FixupIgnoredBytes)].Offset, FixupIgnoredBytes);
        }

        if (type.Fields.ContainsKey(nameof(StubPrecodeSize)))
        {
            StubPrecodeSize = target.ReadField<byte>(address, type, nameof(StubPrecodeSize));
            StubBytes = new byte[StubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(StubBytes)].Offset, StubBytes);
            StubIgnoredBytes = new byte[StubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(StubIgnoredBytes)].Offset, StubIgnoredBytes);
        }

        PInvokeImportPrecodeType = MaybeGetPrecodeType(target, address, type, nameof(PInvokeImportPrecodeType));
        FixupPrecodeType = MaybeGetPrecodeType(target, address, type, nameof(FixupPrecodeType));
        ThisPointerRetBufPrecodeType = MaybeGetPrecodeType(target, address, type, nameof(ThisPointerRetBufPrecodeType));
        InterpreterPrecodeType = MaybeGetPrecodeType(target, address, type, nameof(InterpreterPrecodeType));
        UMEntryPrecodeType = MaybeGetPrecodeType(target, address, type, nameof(UMEntryPrecodeType));
        DynamicHelperPrecodeType = MaybeGetPrecodeType(target, address, type, nameof(DynamicHelperPrecodeType));

        static byte? MaybeGetPrecodeType(Target target, TargetPointer address, Target.TypeInfo type, string fieldName)
            => type.Fields.ContainsKey(fieldName)
                ? target.Read<byte>(address + (ulong)type.Fields[fieldName].Offset)
                : null;
    }
}
