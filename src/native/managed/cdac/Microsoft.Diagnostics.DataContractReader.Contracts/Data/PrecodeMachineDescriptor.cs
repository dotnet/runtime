// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PrecodeMachineDescriptor))]
internal sealed partial class PrecodeMachineDescriptor : IData<PrecodeMachineDescriptor>
{
    [Field] public byte InvalidPrecodeType { get; }
    [Field] public byte StubPrecodeType { get; }
    [Field] public uint StubCodePageSize { get; }

    public byte? OffsetOfPrecodeType { get; private set; } // Not present for version 3 and above
    public byte? ReadWidthOfPrecodeType { get; private set; } // Not present for version 3 and above
    public byte? ShiftOfPrecodeType { get; private set; } // Not present for version 3 and above

    public byte? PInvokeImportPrecodeType { get; private set; }
    public byte? FixupPrecodeType { get; private set; }
    public byte? ThisPointerRetBufPrecodeType { get; private set; }
    public byte? InterpreterPrecodeType { get; private set; } // May be present for version 3 and above
    public byte? UMEntryPrecodeType { get; private set; } // May be present for version 3 and above
    public byte? DynamicHelperPrecodeType { get; private set; } // May be present for version 3 and above

    public byte? FixupStubPrecodeSize { get; private set; } // Present for version 3 and above
    public byte[]? FixupBytes { get; private set; } // Present for version 3 and above
    public byte[]? FixupIgnoredBytes { get; private set; } // Present for version 3 and above

    public byte? StubPrecodeSize { get; private set; } // Present for version 3 and above
    public byte[]? StubBytes { get; private set; } // Present for version 3 and above
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
