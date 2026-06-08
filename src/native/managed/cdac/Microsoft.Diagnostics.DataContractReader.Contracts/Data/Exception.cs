// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Exception), "System.Exception")]
internal sealed partial class Exception : IData<Exception>
{
    [Field("_message")]
    public TargetPointer Message { get; }

    [Field("_innerException")]
    public TargetPointer InnerException { get; }

    [Field("_stackTrace")]
    public TargetPointer StackTrace { get; }

    [Field("_watsonBuckets")]
    public TargetPointer WatsonBuckets { get; }

    [Field("_stackTraceString")]
    public TargetPointer StackTraceString { get; }

    [Field("_remoteStackTraceString")]
    public TargetPointer RemoteStackTraceString { get; }

    [Field("_HResult")]
    public int HResult { get; }

    [Field("_xcode")]
    public int XCode { get; }
}
