// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StgPoolSeg))]
internal sealed partial class StgPoolSeg : IData<StgPoolSeg>
{
    [Field] public TargetPointer SegData { get; }
    [Field] public TargetPointer NextSegment { get; }
    [Field] public uint DataSize { get; }
}
