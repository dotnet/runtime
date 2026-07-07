// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ExceptionInfo))]
internal sealed partial class ExceptionInfo : IData<ExceptionInfo>
{
    [Field] public TargetPointer PreviousNestedInfo { get; }
    [Field] public TargetPointer ThrownObject { get; }
    [Field] public uint ExceptionFlags { get; }
    [Field] public TargetPointer StackLowBound { get; }
    [Field] public TargetPointer StackHighBound { get; }
    [Field] public TargetPointer ExceptionRecord { get; }
    [Field] public TargetPointer ContextRecord { get; }

    // Only present on Windows platforms
    [Field] public TargetPointer? ExceptionWatsonBucketTrackerBuckets { get; }
    [Field] public byte PassNumber { get; }
    [Field] public TargetPointer CSFEHClause { get; }
    [Field] public TargetPointer CSFEnclosingClause { get; }
    [Field] public TargetPointer CallerOfActualHandlerFrame { get; }
    [Field] public uint ClauseForCatchHandlerStartPC { get; }
    [Field] public uint ClauseForCatchHandlerEndPC { get; }
}
