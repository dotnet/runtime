// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class IdDispenser : IData<IdDispenser>
{
    static IdDispenser IData<IdDispenser>.Create(Target target, TargetPointer address) => new IdDispenser(target, address);
    public IdDispenser(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.IdDispenser);
        IdToThread = target.ReadPointer(address + (ulong)type.Fields[nameof(IdToThread)].Offset);
        HighestId = target.Read<uint>(address + (ulong)type.Fields[nameof(HighestId)].Offset);
    }

    public TargetPointer IdToThread { get; init; }
    public uint HighestId { get; init; }
}
