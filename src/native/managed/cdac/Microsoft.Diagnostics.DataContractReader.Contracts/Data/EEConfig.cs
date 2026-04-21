// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEConfig : IData<EEConfig>
{
    static EEConfig IData<EEConfig>.Create(Target target, TargetPointer address)
        => new EEConfig(target, address);

    public EEConfig(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEConfig);

        ModifiableAssemblies = target.ReadField<uint>(address, type, nameof(ModifiableAssemblies));
    }

    public uint ModifiableAssemblies { get; init; }
}
