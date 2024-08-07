// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDescChunk : IData<MethodDescChunk>
{
    static MethodDescChunk IData<MethodDescChunk>.Create(Target target, TargetPointer address) => new MethodDescChunk(target, address);
    public MethodDescChunk(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodDescChunk);

        MethodTable = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodTable)].Offset);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        Size = target.Read<byte>(address + (ulong)type.Fields[nameof(Size)].Offset);
        Count = target.Read<byte>(address + (ulong)type.Fields[nameof(Count)].Offset);
    }

    public TargetPointer MethodTable { get; init; }
    public TargetPointer Next { get; init; }
    public byte Size { get; init; }
    public byte Count { get; init; }
}
