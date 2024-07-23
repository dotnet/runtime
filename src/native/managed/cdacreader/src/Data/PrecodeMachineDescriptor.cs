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
        CodePointerToInstrPointerMask = target.ReadNUInt(address + (ulong)type.Fields[nameof(CodePointerToInstrPointerMask)].Offset);
        OffsetOfPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(OffsetOfPrecodeType)].Offset);
        ReadWidthOfPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(ReadWidthOfPrecodeType)].Offset);
        ShiftOfPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(ShiftOfPrecodeType)].Offset);
        InvalidPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(InvalidPrecodeType)].Offset);
        StubPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(StubPrecodeType)].Offset);
        if (target.ReadGlobal<byte>(HasNDirectImportPrecode) == 1)
        {
            NDirectImportPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(NDirectImportPrecodeType)].Offset);
        }
        if (target.ReadGlobal<byte>(HasFixupPrecode) == 1)
        {
            FixupPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(FixupPrecodeType)].Offset);
        }
    }

    public TargetNUInt CodePointerToInstrPointerMask { get; init; }
    public byte OffsetOfPrecodeType { get; init; }
    public byte ReadWidthOfPrecodeType { get; init; }
    public byte ShiftOfPrecodeType { get; init; }
    public byte InvalidPrecodeType { get; init; }
    public byte StubPrecodeType { get; init; }
    public byte? NDirectImportPrecodeType { get; init; }
    public byte? FixupPrecodeType { get; init; }
    private const string HasNDirectImportPrecode = nameof(HasNDirectImportPrecode);
    private const string HasFixupPrecode = nameof(HasFixupPrecode);
}
