// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class DomainAssembly : IData<DomainAssembly>
{
    static DomainAssembly IData<DomainAssembly>.Create(Target target, TargetPointer address)
        => new DomainAssembly(target, address);

    public DomainAssembly(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DomainAssembly);

        Assembly = target.ReadPointer(address + (ulong)type.Fields[nameof(Assembly)].Offset);
    }

    public TargetPointer Assembly { get; init; }
}
