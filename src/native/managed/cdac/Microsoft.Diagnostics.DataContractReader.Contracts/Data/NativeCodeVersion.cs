// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class NativeCodeVersion : IData<NativeCodeVersion>
{
    static NativeCodeVersion IData<NativeCodeVersion>.Create(Target target, TargetPointer address) => new NativeCodeVersion(target, address);
    public NativeCodeVersion(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.NativeCodeVersion);

        StorageKind = target.Read<uint>(address + (ulong)type.Fields[nameof(StorageKind)].Offset);
        MethodDescOrNode = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDescOrNode)].Offset);
    }

    public NativeCodeVersion(uint storageKind, TargetPointer methodDescOrNode)
    {
        StorageKind = storageKind;
        MethodDescOrNode = methodDescOrNode;
    }

    public uint StorageKind { get; init; }
    public TargetPointer MethodDescOrNode { get; init; }
}
