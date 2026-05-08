// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StressLog : IData<StressLog>
{
    static StressLog IData<StressLog>.Create(Target target, TargetPointer address)
        => new StressLog(target, address);

    public StressLog(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StressLog);

        LoggedFacilities = target.ReadField<uint>(address, type, nameof(LoggedFacilities));
        Level = target.ReadField<uint>(address, type, nameof(Level));
        MaxSizePerThread = target.ReadField<uint>(address, type, nameof(MaxSizePerThread));
        MaxSizeTotal = target.ReadField<uint>(address, type, nameof(MaxSizeTotal));
        TotalChunks = target.ReadField<int>(address, type, nameof(TotalChunks));
        TickFrequency = target.ReadField<ulong>(address, type, nameof(TickFrequency));
        StartTimestamp = target.ReadField<ulong>(address, type, nameof(StartTimestamp));
        ModuleOffset = target.ReadNUIntField(address, type, nameof(ModuleOffset));

        if (type.Fields.ContainsKey(nameof(Modules)))
            Modules = target.ReadPointerField(address, type, nameof(Modules));

        Logs = target.ReadPointerField(address, type, nameof(Logs));
    }

    public uint LoggedFacilities { get; init; }

    public uint Level { get; init; }

    public uint MaxSizePerThread { get; init; }

    public uint MaxSizeTotal { get; init; }

    public int TotalChunks { get; init; }

    public ulong TickFrequency { get; init; }

    public ulong StartTimestamp { get; init; }

    public TargetNUInt ModuleOffset { get; init; }

    public TargetPointer? Modules { get; init; }

    public TargetPointer Logs { get; init; }
}
