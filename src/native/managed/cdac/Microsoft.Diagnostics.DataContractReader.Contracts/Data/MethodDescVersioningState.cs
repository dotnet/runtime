// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDescVersioningState : IData<MethodDescVersioningState>
{
    static MethodDescVersioningState IData<MethodDescVersioningState>.Create(Target target, TargetPointer address) => new MethodDescVersioningState(target, address);
    public MethodDescVersioningState(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodDescVersioningState);

        NativeCodeVersionNode = target.ReadPointer(address + (ulong)type.Fields[nameof(NativeCodeVersionNode)].Offset);
        Flags = target.Read<byte>(address + (ulong)type.Fields[nameof(Flags)].Offset);
    }

    public TargetPointer NativeCodeVersionNode { get; init; }
    public byte Flags { get; init; }
}
