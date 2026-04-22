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

        PreviousNestedInfo = target.ReadPointerField(address, type, nameof(PreviousNestedInfo));
        ThrownObjectHandle = target.ReadDataField<ObjectHandle>(address, type, nameof(ThrownObjectHandle));
        if (type.Fields.ContainsKey(nameof(ExceptionWatsonBucketTrackerBuckets)))
            ExceptionWatsonBucketTrackerBuckets = target.ReadPointerField(address, type, nameof(ExceptionWatsonBucketTrackerBuckets));
        ExceptionFlags = target.ReadField<uint>(address, type, nameof(ExceptionFlags));
        StackLowBound = target.ReadPointerField(address, type, nameof(StackLowBound));
        StackHighBound = target.ReadPointerField(address, type, nameof(StackHighBound));
        PassNumber = target.ReadField<byte>(address, type, nameof(PassNumber));
        CSFEHClause = target.ReadPointerField(address, type, nameof(CSFEHClause));
        CSFEnclosingClause = target.ReadPointerField(address, type, nameof(CSFEnclosingClause));
        CallerOfActualHandlerFrame = target.ReadPointerField(address, type, nameof(CallerOfActualHandlerFrame));
    }

    public TargetPointer PreviousNestedInfo { get; }
    public ObjectHandle ThrownObjectHandle { get; }
    public uint ExceptionFlags { get; }
    public TargetPointer StackLowBound { get; }
    public TargetPointer StackHighBound { get; }
    public TargetPointer ExceptionWatsonBucketTrackerBuckets { get; }
    public byte PassNumber { get; }
    public TargetPointer CSFEHClause { get; }
    public TargetPointer CSFEnclosingClause { get; }
    public TargetPointer CallerOfActualHandlerFrame { get; }
}
