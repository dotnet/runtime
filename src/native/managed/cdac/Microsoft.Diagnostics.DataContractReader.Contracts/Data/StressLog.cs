// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StressLog))]
internal sealed partial class StressLog : IData<StressLog>
{
    [Field] public uint LoggedFacilities { get; }
    [Field] public uint Level { get; }
    [Field] public uint MaxSizePerThread { get; }
    [Field] public uint MaxSizeTotal { get; }
    [Field] public int TotalChunks { get; }
    [Field] public ulong TickFrequency { get; }
    [Field] public ulong StartTimestamp { get; }
    [Field] public ulong StartTime { get; }
    [Field] public TargetNUInt ModuleOffset { get; }
    [Field] public TargetPointer? Modules { get; }
    [Field] public TargetPointer Logs { get; }
}
