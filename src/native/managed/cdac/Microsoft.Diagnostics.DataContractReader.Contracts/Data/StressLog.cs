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

        LoggedFacilities = target.Read<uint>(address + (ulong)type.Fields[nameof(LoggedFacilities)].Offset);
        Level = target.Read<uint>(address + (ulong)type.Fields[nameof(Level)].Offset);
        MaxSizePerThread = target.Read<uint>(address + (ulong)type.Fields[nameof(MaxSizePerThread)].Offset);
        MaxSizeTotal = target.Read<uint>(address + (ulong)type.Fields[nameof(MaxSizeTotal)].Offset);
        TotalChunks = target.Read<int>(address + (ulong)type.Fields[nameof(TotalChunks)].Offset);
        TickFrequency = target.Read<ulong>(address + (ulong)type.Fields[nameof(TickFrequency)].Offset);
        StartTimestamp = target.Read<ulong>(address + (ulong)type.Fields[nameof(StartTimestamp)].Offset);
        ModuleOffset = target.ReadNUInt(address + (ulong)type.Fields[nameof(ModuleOffset)].Offset);

        Logs = target.ReadPointer(address + (ulong)type.Fields[nameof(Logs)].Offset);
    }

    public uint LoggedFacilities { get; init; }

    public uint Level { get; init; }

    public uint MaxSizePerThread { get; init; }

    public uint MaxSizeTotal { get; init; }

    public int TotalChunks { get; init; }

    public ulong TickFrequency { get; init; }

    public ulong StartTimestamp { get; init; }

    public TargetNUInt ModuleOffset { get; init; }

    public TargetPointer Logs { get; init; }
}
