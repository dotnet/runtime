// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PrecodeMachineDescriptor : IData<PrecodeMachineDescriptor>
{
    static PrecodeMachineDescriptor IData<PrecodeMachineDescriptor>.Create(Target target, TargetPointer address)
        => new PrecodeMachineDescriptor(target, address);

    public PrecodeMachineDescriptor(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PrecodeMachineDescriptor);
        if (type.Fields.ContainsKey(nameof(OffsetOfPrecodeType)))
        {
            OffsetOfPrecodeType = target.ReadField<byte>(address, type, nameof(OffsetOfPrecodeType));
            ReadWidthOfPrecodeType = target.ReadField<byte>(address, type, nameof(ReadWidthOfPrecodeType));
            ShiftOfPrecodeType = target.ReadField<byte>(address, type, nameof(ShiftOfPrecodeType));
        }
        else
        {
            OffsetOfPrecodeType = null;
            ReadWidthOfPrecodeType = null;
            ShiftOfPrecodeType = null;
        }
        InvalidPrecodeType = target.ReadField<byte>(address, type, nameof(InvalidPrecodeType));
        StubPrecodeType = target.ReadField<byte>(address, type, nameof(StubPrecodeType));

        if (type.Fields.ContainsKey(nameof(FixupStubPrecodeSize)))
        {
            FixupStubPrecodeSize = target.ReadField<byte>(address, type, nameof(FixupStubPrecodeSize));
            FixupBytes = new byte[FixupStubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(FixupBytes)].Offset, FixupBytes);
            FixupIgnoredBytes = new byte[FixupStubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(FixupIgnoredBytes)].Offset, FixupIgnoredBytes);
        }
        else
        {
            FixupStubPrecodeSize = null;
            FixupBytes = null;
            FixupIgnoredBytes = null;
        }

        if (type.Fields.ContainsKey(nameof(StubPrecodeSize)))
        {
            StubPrecodeSize = target.ReadField<byte>(address, type, nameof(StubPrecodeSize));
            StubBytes = new byte[StubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(StubBytes)].Offset, StubBytes);
            StubIgnoredBytes = new byte[StubPrecodeSize.Value];
            target.ReadBuffer(address + (ulong)type.Fields[nameof(StubIgnoredBytes)].Offset, StubIgnoredBytes);
        }
        else
        {
            StubPrecodeSize = null;
            StubBytes = null;
            StubIgnoredBytes = null;
        }

        PInvokeImportPrecodeType = MaybeGetPrecodeType(target, address, nameof(PInvokeImportPrecodeType));
        FixupPrecodeType = MaybeGetPrecodeType(target, address, nameof(FixupPrecodeType));
        ThisPointerRetBufPrecodeType = MaybeGetPrecodeType(target, address, nameof(ThisPointerRetBufPrecodeType));
        InterpreterPrecodeType = MaybeGetPrecodeType(target, address, nameof(InterpreterPrecodeType));
        UMEntryPrecodeType = MaybeGetPrecodeType(target, address, nameof(UMEntryPrecodeType));
        DynamicHelperPrecodeType = MaybeGetPrecodeType(target, address, nameof(DynamicHelperPrecodeType));

        StubCodePageSize = target.ReadField<uint>(address, type, nameof(StubCodePageSize));

        static byte? MaybeGetPrecodeType(Target target, TargetPointer address, string fieldName)
        {
            if (target.GetTypeInfo(DataType.PrecodeMachineDescriptor).Fields.ContainsKey(fieldName))
            {
                return target.Read<byte>(address + (ulong)target.GetTypeInfo(DataType.PrecodeMachineDescriptor).Fields[fieldName].Offset);
            }
            else
            {
                return null;
            }
        }
    }

    public byte? OffsetOfPrecodeType { get; init; } // Not present for version 3 and above
    public byte? ReadWidthOfPrecodeType { get; init; } // Not present for version 3 and above
    public byte? ShiftOfPrecodeType { get; init; } // Not present for version 3 and above
    public byte InvalidPrecodeType { get; init; }
    public byte StubPrecodeType { get; init; }
    public byte? PInvokeImportPrecodeType { get; init; }
    public byte? FixupPrecodeType { get; init; }
    public byte? ThisPointerRetBufPrecodeType { get; init; }

    public byte? FixupStubPrecodeSize { get; init; } // Present for version 3 and above
    public byte[]? FixupBytes { get; init; } // Present for version 3 and above
    public byte[]? FixupIgnoredBytes { get; init; } // Present for version 3 and above

    public byte? StubPrecodeSize { get; init; } // Present for version 3 and above
    public byte[]? StubBytes { get; init; } // Present for version 3 and above
    public byte[]? StubIgnoredBytes { get; init; } // Present for version 3 and above

    public byte? InterpreterPrecodeType { get; init; } // May be present for version 3 and above
    public byte? UMEntryPrecodeType { get; init; } // May be present for version 3 and above
    public byte? DynamicHelperPrecodeType { get; init; } // May be present for version 3 and above

    public uint StubCodePageSize { get; init; }
}
