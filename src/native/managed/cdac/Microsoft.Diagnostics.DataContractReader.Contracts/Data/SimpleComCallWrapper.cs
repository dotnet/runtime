// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SimpleComCallWrapper : IData<SimpleComCallWrapper>
{
    static SimpleComCallWrapper IData<SimpleComCallWrapper>.Create(Target target, TargetPointer address) => new SimpleComCallWrapper(target, address);
    public SimpleComCallWrapper(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SimpleComCallWrapper);

        RefCount = target.Read<ulong>(address + (ulong)type.Fields[nameof(RefCount)].Offset);
        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
    }

    public ulong RefCount { get; init; }
    public uint Flags { get; init; }
}
