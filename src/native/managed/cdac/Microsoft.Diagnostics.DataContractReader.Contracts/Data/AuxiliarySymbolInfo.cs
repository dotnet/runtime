// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class AuxiliarySymbolInfo : IData<AuxiliarySymbolInfo>
{
    static AuxiliarySymbolInfo IData<AuxiliarySymbolInfo>.Create(Target target, TargetPointer address)
        => new AuxiliarySymbolInfo(target, address);

    public AuxiliarySymbolInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.AuxiliarySymbolInfo);

        Address = target.ReadCodePointer(address + (ulong)type.Fields[nameof(Address)].Offset);
        Name = target.ReadPointer(address + (ulong)type.Fields[nameof(Name)].Offset);
    }

    public TargetCodePointer Address { get; init; }
    public TargetPointer Name { get; init; }
}
