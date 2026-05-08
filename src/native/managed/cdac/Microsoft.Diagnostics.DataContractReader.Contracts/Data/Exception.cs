// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Exception : IData<Exception>
{
    static Exception IData<Exception>.Create(Target target, TargetPointer address)
        => new Exception(target, address);

    public Exception(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Exception);

        Message = target.ReadPointerField(address, type, "_message");
        InnerException = target.ReadPointerField(address, type, "_innerException");
        StackTrace = target.ReadPointerField(address, type, "_stackTrace");
        WatsonBuckets = target.ReadPointerField(address, type, "_watsonBuckets");
        StackTraceString = target.ReadPointerField(address, type, "_stackTraceString");
        RemoteStackTraceString = target.ReadPointerField(address, type, "_remoteStackTraceString");
        HResult = target.ReadField<int>(address, type, "_HResult");
        XCode = target.ReadField<int>(address, type, "_xcode");
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
