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

        JitMinOpts = target.Read<byte>(address + (ulong)type.Fields[nameof(JitMinOpts)].Offset) != 0;
        GenDebuggable = target.Read<byte>(address + (ulong)type.Fields[nameof(GenDebuggable)].Offset) != 0;
        TieredCompilation_DefaultTier = target.Read<uint>(address + (ulong)type.Fields[nameof(TieredCompilation_DefaultTier)].Offset);
    }

    public bool JitMinOpts { get; init; }
    public bool GenDebuggable { get; init; }
    public uint TieredCompilation_DefaultTier { get; init; }
}
