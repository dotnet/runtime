// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ComInterfaceEntry))]
internal sealed partial class ComInterfaceEntry : IData<ComInterfaceEntry>
{
    [CustomInit(nameof(InitIID))] public partial Guid IID { get; }

    [DataDescriptorDependency(nameof(IID), "nuint")]
    private partial Guid InitIID(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComInterfaceEntry);
        // IID is a 16-byte GUID
        byte[] iidBytes = new byte[16];
        target.ReadBuffer(address + (ulong)type.Fields[nameof(IID)].Offset, iidBytes);
        return new Guid(iidBytes);
    }
}
