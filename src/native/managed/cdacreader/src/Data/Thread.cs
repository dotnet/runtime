// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Thread
{
    public Thread(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Thread);

        Id = target.Read<uint>(address + (ulong)type.Fields[nameof(Id)].Offset);
        LinkNext = target.ReadPointer(address + (ulong)type.Fields[nameof(LinkNext)].Offset);
    }

    public uint Id { get; init; }
    internal TargetPointer LinkNext { get; init; }
}
