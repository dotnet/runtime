// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class IJitManager : IData<IJitManager>
{
    static IJitManager IData<IJitManager>.Create(Target target, TargetPointer address)
        => new IJitManager(target, address);

    public IJitManager(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.IJitManager);
        JitManagerKind = target.Read<uint>(address + (ulong)type.Fields[nameof(JitManagerKind)].Offset);
    }

    public uint JitManagerKind { get; init; }
}
