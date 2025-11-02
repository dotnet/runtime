// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ExceptionInfo : IData<ExceptionInfo>
{
    static ExceptionInfo IData<ExceptionInfo>.Create(Target target, TargetPointer address)
        => new ExceptionInfo(target, address);

    public ExceptionInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ExceptionInfo);

        PreviousNestedInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(PreviousNestedInfo)].Offset);
        ThrownObjectHandle = target.ReadPointer(address + (ulong)type.Fields[nameof(ThrownObjectHandle)].Offset);
        if (type.Fields.ContainsKey(nameof(ExceptionWatsonBucketTrackerBuckets)))
            ExceptionWatsonBucketTrackerBuckets = target.ReadPointer(address + (ulong)type.Fields[nameof(ExceptionWatsonBucketTrackerBuckets)].Offset);
    }

    public TargetPointer PreviousNestedInfo { get; init; }
    public TargetPointer ThrownObjectHandle { get; init; }
    public TargetPointer ExceptionWatsonBucketTrackerBuckets { get; init; }
}
