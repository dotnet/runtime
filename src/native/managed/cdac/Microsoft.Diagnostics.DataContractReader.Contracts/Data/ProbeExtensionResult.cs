// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ProbeExtensionResult : IData<ProbeExtensionResult>
{
    static ProbeExtensionResult IData<ProbeExtensionResult>.Create(Target target, TargetPointer address) => new ProbeExtensionResult(target, address);
    public ProbeExtensionResult(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ProbeExtensionResult);

        Type = target.Read<int>(address + (ulong)type.Fields[nameof(Type)].Offset);
    }

    public int Type { get; init; }
}
