// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ModuleLookupMap : IData<ModuleLookupMap>
{
    static ModuleLookupMap IData<ModuleLookupMap>.Create(Target target, TargetPointer address) => new ModuleLookupMap(target, address);

    private ModuleLookupMap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ModuleLookupMap);

        TableData = target.ReadPointer(address + (ulong)type.Fields[nameof(TableData)].Offset);

    }

    public TargetPointer TableData { get; init; }
}
