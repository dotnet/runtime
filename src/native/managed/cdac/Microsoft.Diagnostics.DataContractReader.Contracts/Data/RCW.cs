// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RCW : IData<RCW>
{
    static RCW IData<RCW>.Create(Target target, TargetPointer address) => new RCW(target, address);
    public RCW(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RCW);

        // m_aInterfaceEntries is an inline array - the offset is the address of the first element
        InterfaceEntries = address + (ulong)type.Fields[nameof(InterfaceEntries)].Offset;
    }

    public TargetPointer InterfaceEntries { get; init; }
}
