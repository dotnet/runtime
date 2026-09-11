// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ExceptionInfo))]
internal sealed partial class ExceptionInfo : IData<ExceptionInfo>
{
    [Field] public partial TargetPointer PreviousNestedInfo { get; }
    [Field] public partial TargetPointer ThrownObject { get; }
    [Field] public partial uint ExceptionFlags { get; }
    [Field] public partial TargetPointer StackLowBound { get; }
    [Field] public partial TargetPointer StackHighBound { get; }
    [Field] public partial TargetPointer ExceptionRecord { get; }
    [Field] public partial TargetPointer ContextRecord { get; }

    // Only present on Windows platforms
    [Field] public partial TargetPointer? ExceptionWatsonBucketTrackerBuckets { get; }
    [Field] public partial byte PassNumber { get; }
    [Field] public partial TargetPointer CSFEHClause { get; }
    [Field] public partial TargetPointer CSFEnclosingClause { get; }
    [Field] public partial TargetPointer CallerOfActualHandlerFrame { get; }
    [Field] public partial uint ClauseForCatchHandlerStartPC { get; }
    [Field] public partial uint ClauseForCatchHandlerEndPC { get; }
}
