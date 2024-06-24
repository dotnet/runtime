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

        MTFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(MTFlags)].Offset);
        BaseSize = target.Read<uint>(address + (ulong)type.Fields[nameof(BaseSize)].Offset);
        MTFlags2 = target.Read<uint>(address + (ulong)type.Fields[nameof(MTFlags2)].Offset);
        EEClassOrCanonMT = target.ReadPointer(address + (ulong)type.Fields[nameof(EEClassOrCanonMT)].Offset);
        Module = target.ReadPointer(address + (ulong)type.Fields[nameof(Module)].Offset);
        ParentMethodTable = target.ReadPointer(address + (ulong)type.Fields[nameof(ParentMethodTable)].Offset);
        NumInterfaces = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumInterfaces)].Offset);
        NumVirtuals = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumVirtuals)].Offset);
    }

    public uint MTFlags { get; init; }
    public uint BaseSize { get; init; }
    public uint MTFlags2 { get; init; }
    public TargetPointer EEClassOrCanonMT { get; init; }
    public TargetPointer Module { get; init; }
    public TargetPointer ParentMethodTable { get; init; }
    public ushort NumInterfaces { get; init; }
    public ushort NumVirtuals { get; init; }
}
