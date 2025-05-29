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
        OffsetOfPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(OffsetOfPrecodeType)].Offset);
        ReadWidthOfPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(ReadWidthOfPrecodeType)].Offset);
        ShiftOfPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(ShiftOfPrecodeType)].Offset);
        InvalidPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(InvalidPrecodeType)].Offset);
        StubPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(StubPrecodeType)].Offset);
        if (type.Fields.ContainsKey(nameof(PInvokeImportPrecodeType)))
        {
            PInvokeImportPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(PInvokeImportPrecodeType)].Offset);
        }
        else
        {
            PInvokeImportPrecodeType = null;
        }
        if (type.Fields.ContainsKey(nameof(FixupPrecodeType)))
        {
            FixupPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(FixupPrecodeType)].Offset);
        }
        else
        {
            FixupPrecodeType = null;
        }
        if (type.Fields.ContainsKey(nameof(ThisPointerRetBufPrecodeType)))
        {
            ThisPointerRetBufPrecodeType = target.Read<byte>(address + (ulong)type.Fields[nameof(ThisPointerRetBufPrecodeType)].Offset);
        }
        else
        {
            ThisPointerRetBufPrecodeType = null;
        }
        StubCodePageSize = target.Read<uint>(address + (ulong)type.Fields[nameof(StubCodePageSize)].Offset);
    }

    public byte OffsetOfPrecodeType { get; init; }
    public byte ReadWidthOfPrecodeType { get; init; }
    public byte ShiftOfPrecodeType { get; init; }
    public byte InvalidPrecodeType { get; init; }
    public byte StubPrecodeType { get; init; }
    public byte? PInvokeImportPrecodeType { get; init; }
    public byte? FixupPrecodeType { get; init; }
    public byte? ThisPointerRetBufPrecodeType { get; init; }

    public uint StubCodePageSize { get; init; }
}
