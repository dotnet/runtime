// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HeapSegment))]
internal sealed partial class HeapSegment : IData<HeapSegment>
{
    [Field] public TargetPointer Allocated { get; }
    [Field] public TargetPointer Committed { get; }
    [Field] public TargetPointer Reserved { get; }
    [Field] public TargetPointer Used { get; }
    [Field] public TargetPointer Mem { get; }
    [Field] public TargetNUInt Flags { get; }
    [Field] public TargetPointer Next { get; }
    [Field] public TargetPointer BackgroundAllocated { get; }

    // Field only exists in MULTIPLE_HEAPS builds
    [Field] public TargetPointer? Heap { get; }
}
