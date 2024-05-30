// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodTable : IData<MethodTable>
{
    static MethodTable IData<MethodTable>.Create(Target target, TargetPointer address) => new MethodTable(target, address);
    public MethodTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodTable);

        //Id = target.Read<uint>(address + (ulong)type.Fields[nameof(Id)].Offset);
        //LinkNext = target.ReadPointer(address + (ulong)type.Fields[nameof(LinkNext)].Offset);
        DwFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(DwFlags)].Offset);
        BaseSize = target.Read<uint>(address + (ulong)type.Fields[nameof(BaseSize)].Offset);
        DwFlags2 = target.Read<uint>(address + (ulong)type.Fields[nameof(DwFlags2)].Offset);
    }

    public uint DwFlags { get; init; }
    public uint BaseSize { get; init; }
    public uint DwFlags2 { get; init; }
}
