// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDesc : IData<MethodDesc>
{
    static MethodDesc IData<MethodDesc>.Create(Target target, TargetPointer address) => new MethodDesc(target, address);
    public MethodDesc(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodDesc);

        ChunkIndex = target.Read<byte>(address + (ulong)type.Fields[nameof(ChunkIndex)].Offset);
        Slot = target.Read<ushort>(address + (ulong)type.Fields[nameof(Slot)].Offset);
        Flags = target.Read<ushort>(address + (ulong)type.Fields[nameof(Flags)].Offset);
    }

    public byte ChunkIndex { get; init; }
    public ushort Slot { get; init; }
    public ushort Flags { get; init; }
}
