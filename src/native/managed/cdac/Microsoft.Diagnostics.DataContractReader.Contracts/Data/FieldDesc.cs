// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class FieldDesc : IData<FieldDesc>
{
    static FieldDesc IData<FieldDesc>.Create(Target target, TargetPointer address) => new FieldDesc(target, address);
    public FieldDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.FieldDesc);
        DWord1 = target.Read<uint>(address + (ulong)type.Fields[nameof(DWord1)].Offset);
        DWord2 = target.Read<uint>(address + (ulong)type.Fields[nameof(DWord2)].Offset);
        MTOfEnclosingClass = target.ReadPointer(address + (ulong)type.Fields[nameof(MTOfEnclosingClass)].Offset);
    }

    public uint DWord1 { get; init; }
    public uint DWord2 { get; init; }
    public TargetPointer MTOfEnclosingClass { get; init; }
}
