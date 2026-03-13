// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InterfaceEntry : IData<InterfaceEntry>
{
    static InterfaceEntry IData<InterfaceEntry>.Create(Target target, TargetPointer address) => new InterfaceEntry(target, address);
    public InterfaceEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterfaceEntry);

        MethodTable = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodTable)].Offset);
        Unknown = target.ReadPointer(address + (ulong)type.Fields[nameof(Unknown)].Offset);
    }

    public TargetPointer MethodTable { get; init; }
    public TargetPointer Unknown { get; init; }
}
