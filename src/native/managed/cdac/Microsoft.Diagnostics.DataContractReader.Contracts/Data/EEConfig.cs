// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEConfig : IData<EEConfig>
{
    static EEConfig IData<EEConfig>.Create(Target target, TargetPointer address) => new EEConfig(target, address);
    public EEConfig(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEConfig);

        JitMinOpts = target.Read<byte>(address + (ulong)type.Fields[nameof(JitMinOpts)].Offset) != 0;
        Debuggable = target.Read<byte>(address + (ulong)type.Fields[nameof(Debuggable)].Offset) != 0;
        TieredPGO = target.Read<byte>(address + (ulong)type.Fields[nameof(TieredPGO)].Offset) != 0;
        TieredPGO_InstrumentOnlyHotCode = target.Read<byte>(address + (ulong)type.Fields[nameof(TieredPGO_InstrumentOnlyHotCode)].Offset) != 0;
    }

    public bool JitMinOpts { get; init; }
    public bool Debuggable { get; init; }
    public bool TieredPGO { get; init; }
    public bool TieredPGO_InstrumentOnlyHotCode { get; init; }
}
