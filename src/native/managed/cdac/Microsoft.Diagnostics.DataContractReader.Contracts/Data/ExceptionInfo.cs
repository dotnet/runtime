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
        ExceptionFlags = target.Read<uint>(address + (ulong)type.Fields[nameof(ExceptionFlags)].Offset);
        StackLowBound = target.ReadPointer(address + (ulong)type.Fields[nameof(StackLowBound)].Offset);
        StackHighBound = target.ReadPointer(address + (ulong)type.Fields[nameof(StackHighBound)].Offset);
        if (type.Fields.ContainsKey(nameof(ExceptionWatsonBucketTrackerBuckets)))
            ExceptionWatsonBucketTrackerBuckets = target.ReadPointer(address + (ulong)type.Fields[nameof(ExceptionWatsonBucketTrackerBuckets)].Offset);
    }

    public TargetPointer PreviousNestedInfo { get; }
    public TargetPointer ThrownObjectHandle { get; }
    public uint ExceptionFlags { get; }
    public TargetPointer StackLowBound { get; }
    public TargetPointer StackHighBound { get; }
    public TargetPointer ExceptionWatsonBucketTrackerBuckets { get; }
}
