// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Assembly : IData<Assembly>
{
    static Assembly IData<Assembly>.Create(Target target, TargetPointer address) => new Assembly(target, address);
    public Assembly(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Assembly);

        Module = target.ReadPointer(address + (ulong)type.Fields[nameof(Module)].Offset);
        IsCollectible = target.Read<byte>(address + (ulong)type.Fields[nameof(IsCollectible)].Offset);
        IsDynamic = target.Read<byte>(address + (ulong)type.Fields[nameof(IsDynamic)].Offset) != 0;
        Error = target.ReadPointer(address + (ulong)type.Fields[nameof(Error)].Offset);
        NotifyFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(NotifyFlags)].Offset);
        Level = target.Read<uint>(address + (ulong)type.Fields[nameof(Level)].Offset);
    }

    public TargetPointer Module { get; init; }
    public byte IsCollectible { get; init; }
    public bool IsDynamic { get; init; }
    public TargetPointer Error { get; init; }
    public uint NotifyFlags { get; init; }
    public uint Level { get; init; }

    public bool IsError => Error != TargetPointer.Null;
}
