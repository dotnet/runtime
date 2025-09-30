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

        PassNumber = target.Read<byte>(address + (ulong)type.Fields[nameof(PassNumber)].Offset);
        CSFEHClause = target.ReadPointer(address + (ulong)type.Fields[nameof(CSFEHClause)].Offset);
        CSFEnclosingClause = target.ReadPointer(address + (ulong)type.Fields[nameof(CSFEnclosingClause)].Offset);
        CallerOfActualHandlerFrame = target.ReadPointer(address + (ulong)type.Fields[nameof(CallerOfActualHandlerFrame)].Offset);
        LastReportedFuncletInfo = target.ProcessedData.GetOrAdd<Data.LastReportedFuncletInfo>(address + (ulong)type.Fields[nameof(LastReportedFuncletInfo)].Offset);
    }

    public TargetPointer PreviousNestedInfo { get; }
    public TargetPointer ThrownObjectHandle { get; }
    public uint ExceptionFlags { get; }
    public TargetPointer StackLowBound { get; }
    public TargetPointer StackHighBound { get; }
    public TargetPointer ExceptionWatsonBucketTrackerBuckets { get; }
    public byte PassNumber { get; }
    public TargetPointer CSFEHClause { get; }
    public TargetPointer CSFEnclosingClause { get; }
    public TargetPointer CallerOfActualHandlerFrame { get; }
    public LastReportedFuncletInfo LastReportedFuncletInfo { get; }
}
