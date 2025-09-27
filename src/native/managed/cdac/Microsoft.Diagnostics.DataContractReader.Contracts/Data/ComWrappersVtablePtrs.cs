// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ComWrappersVtablePtrs : IData<ComWrappersVtablePtrs>
{
    static ComWrappersVtablePtrs IData<ComWrappersVtablePtrs>.Create(Target target, TargetPointer address) => new ComWrappersVtablePtrs(target, address);
    public ComWrappersVtablePtrs(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ComWrappersVtablePtrs);

        MowQueryInterface = target.ReadPointer(address + (ulong)type.Fields[nameof(MowQueryInterface)].Offset);
        TtQueryInterface = target.ReadPointer(address + (ulong)type.Fields[nameof(TtQueryInterface)].Offset);
    }

    public TargetPointer MowQueryInterface { get; init; }
    public TargetPointer TtQueryInterface { get; init; }
}
