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
    }

    public TargetPointer Node { get; init; }
}
