// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ExceptionObject : IData<ExceptionObject>
{
    static ExceptionObject IData<ExceptionObject>.Create(Target target, TargetPointer address)
        => new ExceptionObject(target, address);

    public ExceptionObject(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ExceptionObject);

        Message = target.ReadPointer(address + (ulong)type.Fields["_message"].Offset);
        InnerException = target.ReadPointer(address + (ulong)type.Fields["_innerException"].Offset);
        StackTrace = target.ReadPointer(address + (ulong)type.Fields["_stackTrace"].Offset);
        WatsonBuckets = target.ReadPointer(address + (ulong)type.Fields["_watsonBuckets"].Offset);
        StackTraceString = target.ReadPointer(address + (ulong)type.Fields["_stackTraceString"].Offset);
        RemoteStackTraceString = target.ReadPointer(address + (ulong)type.Fields["_remoteStackTraceString"].Offset);
        HResult = target.Read<int>(address + (ulong)type.Fields["_HResult"].Offset);
        XCode = target.Read<int>(address + (ulong)type.Fields["_xcode"].Offset);
    }

    public TargetPointer Message { get; init; }
    public TargetPointer InnerException { get; init; }
    public TargetPointer StackTrace { get; init; }
    public TargetPointer WatsonBuckets { get; init; }
    public TargetPointer StackTraceString { get; init; }
    public TargetPointer RemoteStackTraceString { get; init; }
    public int HResult { get; init; }
    public int XCode { get; init; }
}
