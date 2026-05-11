// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ComInterfaceEntry : IData<ComInterfaceEntry>
{
    static ComInterfaceEntry IData<ComInterfaceEntry>.Create(Target target, TargetPointer address)
        => new ComInterfaceEntry(target, address);

    public ComInterfaceEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComInterfaceEntry);
        // IID is a 16-byte GUID
        byte[] iidBytes = new byte[16];
        target.ReadBuffer(address + (ulong)type.Fields[nameof(IID)].Offset, iidBytes);
        IID = new Guid(iidBytes);
    }

    public Guid IID { get; init; }
}
