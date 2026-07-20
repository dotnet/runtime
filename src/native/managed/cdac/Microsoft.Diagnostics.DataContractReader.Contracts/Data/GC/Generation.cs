// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Generation))]
internal sealed partial class Generation : IData<Generation>
{
    [Field] public GCAllocContext AllocationContext { get; }
    [Field] public TargetPointer StartSegment { get; }

    // Fields only exist segment GC builds
    [Field] public TargetPointer? AllocationStart { get; }
}
