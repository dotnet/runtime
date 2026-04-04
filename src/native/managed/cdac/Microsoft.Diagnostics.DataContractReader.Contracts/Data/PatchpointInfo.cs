// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PatchpointInfo : IData<PatchpointInfo>
{
    static PatchpointInfo IData<PatchpointInfo>.Create(Target target, TargetPointer address)
        => new PatchpointInfo(target, address);

    public PatchpointInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PatchpointInfo);

        LocalCount = target.Read<uint>(address + (ulong)type.Fields[nameof(LocalCount)].Offset);
    }

    public uint LocalCount { get; }
}
