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
        ThrownObject = target.ProcessedData.GetOrAdd<ObjectHandle>(
            target.ReadPointer(address + (ulong)type.Fields[nameof(ThrownObject)].Offset));
    }

    public TargetPointer PreviousNestedInfo { get; init; }
    public ObjectHandle ThrownObject { get; init; }
}
