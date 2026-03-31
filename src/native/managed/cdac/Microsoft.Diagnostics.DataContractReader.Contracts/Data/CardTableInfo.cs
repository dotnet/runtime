// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CardTableInfo : IData<CardTableInfo>
{
    static CardTableInfo IData<CardTableInfo>.Create(Target target, TargetPointer address)
        => new CardTableInfo(target, address);

    public CardTableInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CardTableInfo);
        Recount = target.Read<uint>(address + (ulong)type.Fields[nameof(Recount)].Offset);
        Size = target.ReadNUInt(address + (ulong)type.Fields[nameof(Size)].Offset);
        NextCardTable = target.ReadPointer(address + (ulong)type.Fields[nameof(NextCardTable)].Offset);
    }

    public uint Recount { get; init; }
    public TargetNUInt Size { get; init; }
    public TargetPointer NextCardTable { get; init; }
}
