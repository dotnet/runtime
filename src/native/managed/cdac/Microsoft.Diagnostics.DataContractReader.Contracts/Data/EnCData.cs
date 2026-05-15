// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EnCData : IData<EnCData>
{
    static EnCData IData<EnCData>.Create(Target target, TargetPointer address)
        => new EnCData(target, address);

    public EnCData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EnCData);

        AddrOfCode = target.ReadPointerField(address, type, nameof(AddrOfCode));
        Token = target.ReadField<uint>(address, type, nameof(Token));
        EnCVersion = target.ReadNUInt(address + (ulong)type.Fields[nameof(EnCVersion)].Offset);
        Next = target.ReadPointerField(address, type, nameof(Next));
    }

    public TargetPointer AddrOfCode { get; init; }
    public uint Token { get; init; }
    public TargetNUInt EnCVersion { get; init; }
    public TargetPointer Next { get; init; }
}
