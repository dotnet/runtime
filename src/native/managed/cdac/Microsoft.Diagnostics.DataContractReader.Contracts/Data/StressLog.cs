// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StressLog))]
internal sealed partial class StressLog : IData<StressLog>
{
    [Field] public partial uint LoggedFacilities { get; }
    [Field] public partial uint Level { get; }
    [Field] public partial uint MaxSizePerThread { get; }
    [Field] public partial uint MaxSizeTotal { get; }
    [Field] public partial int TotalChunks { get; }
    [Field] public partial ulong TickFrequency { get; }
    [Field] public partial ulong StartTimestamp { get; }
    [Field] public partial ulong StartTime { get; }
    [Field] public partial TargetNUInt ModuleOffset { get; }
    [FieldAddress] public partial TargetPointer? Modules { get; }
    [Field] public partial TargetPointer Logs { get; }
}
