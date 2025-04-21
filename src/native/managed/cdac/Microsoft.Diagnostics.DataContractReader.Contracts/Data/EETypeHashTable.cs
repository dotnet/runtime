// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EETypeHashTable : IData<EETypeHashTable>
{
    static EETypeHashTable IData<EETypeHashTable>.Create(Target target, TargetPointer address) => new EETypeHashTable(target, address);
    public EETypeHashTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EETypeHashTable);

        DacEnumerableHash baseHashTable = new(target, address, type);

        Entries = baseHashTable.Entries;
    }

    public IReadOnlyList<TargetPointer> Entries { get; init; } = [];
}
