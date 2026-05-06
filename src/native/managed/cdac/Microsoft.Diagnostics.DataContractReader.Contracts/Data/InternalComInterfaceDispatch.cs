// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InternalComInterfaceDispatch : IData<InternalComInterfaceDispatch>
{
    static InternalComInterfaceDispatch IData<InternalComInterfaceDispatch>.Create(Target target, TargetPointer address)
        => new InternalComInterfaceDispatch(target, address);

    public InternalComInterfaceDispatch(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InternalComInterfaceDispatch);
        Entries = address + (ulong)type.Fields[nameof(Entries)].Offset;
    }

    public TargetPointer Entries { get; init; }
}
