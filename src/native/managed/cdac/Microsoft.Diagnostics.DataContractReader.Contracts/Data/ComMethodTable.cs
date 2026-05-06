// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ComMethodTable : IData<ComMethodTable>
{
    static ComMethodTable IData<ComMethodTable>.Create(Target target, TargetPointer address) => new ComMethodTable(target, address);
    public ComMethodTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComMethodTable);

        Flags = target.ReadNUIntField(address, type, nameof(Flags));
        MethodTable = target.ReadPointerField(address, type, nameof(MethodTable));
    }

    public TargetNUInt Flags { get; init; }
    public TargetPointer MethodTable { get; init; }
}
