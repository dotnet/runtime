// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ComMethodTable : IData<ComMethodTable>
{
    static ComMethodTable IData<ComMethodTable>.Create(Target target, TargetPointer address) => new ComMethodTable(target, address);
    public ComMethodTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComMethodTable);

        Flags = target.ReadNUInt(address + (ulong)type.Fields[nameof(Flags)].Offset);
        MethodTable = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodTable)].Offset);
    }

    public TargetNUInt Flags { get; init; }
    public TargetPointer MethodTable { get; init; }
}
