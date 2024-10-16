// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ILCodeVersioningState : IData<ILCodeVersioningState>
{
    static ILCodeVersioningState IData<ILCodeVersioningState>.Create(Target target, TargetPointer address)
        => new ILCodeVersioningState(target, address);

    public ILCodeVersioningState(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ILCodeVersioningState);

        Node = target.ReadPointer(address + (ulong)type.Fields[nameof(Node)].Offset);
        ActiveVersionKind = target.Read<uint>(address + (ulong)type.Fields[nameof(ActiveVersionKind)].Offset);
        ActiveVersionNode = target.ReadPointer(address + (ulong)type.Fields[nameof(ActiveVersionNode)].Offset);
        ActiveVersionModule = target.ReadPointer(address + (ulong)type.Fields[nameof(ActiveVersionModule)].Offset);
        ActiveVersionMethodDef = target.Read<uint>(address + (ulong)type.Fields[nameof(ActiveVersionMethodDef)].Offset);
    }

    public TargetPointer Node { get; init; }
    public uint ActiveVersionKind { get; set; }
    public TargetPointer ActiveVersionNode { get; set; }
    public TargetPointer ActiveVersionModule { get; set; }
    public uint ActiveVersionMethodDef { get; set; }
}
