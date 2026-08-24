// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HeapSegment))]
internal sealed partial class HeapSegment : IData<HeapSegment>
{
    [Field] public partial TargetPointer Allocated { get; }
    [Field] public partial TargetPointer Committed { get; }
    [Field] public partial TargetPointer Reserved { get; }
    [Field] public partial TargetPointer Used { get; }
    [Field] public partial TargetPointer Mem { get; }
    [Field] public partial TargetNUInt Flags { get; }
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TargetPointer BackgroundAllocated { get; }

    // Field only exists in MULTIPLE_HEAPS builds
    [Field] public partial TargetPointer? Heap { get; }
}
