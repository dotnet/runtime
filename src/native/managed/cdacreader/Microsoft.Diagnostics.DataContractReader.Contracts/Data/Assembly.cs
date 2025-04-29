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

        IsCollectible = target.Read<byte>(address + (ulong)type.Fields[nameof(IsCollectible)].Offset);
    }

    public byte IsCollectible { get; init; }
}
