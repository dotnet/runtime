// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Exception), "System.Exception")]
internal sealed partial class Exception : IData<Exception>
{
    [Field("_message")]
    public partial TargetPointer Message { get; }

    [Field("_innerException")]
    public partial TargetPointer InnerException { get; }

    [Field("_stackTrace")]
    public partial TargetPointer StackTrace { get; }

    [Field("_watsonBuckets")]
    public partial TargetPointer WatsonBuckets { get; }

    [Field("_stackTraceString")]
    public partial TargetPointer StackTraceString { get; }

    [Field("_remoteStackTraceString")]
    public partial TargetPointer RemoteStackTraceString { get; }

    [Field("_HResult")]
    public partial int HResult { get; }

    [Field("_xcode")]
    public partial int XCode { get; }
}
