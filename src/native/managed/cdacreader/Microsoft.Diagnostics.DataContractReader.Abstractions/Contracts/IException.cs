// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public record struct ExceptionData(
    TargetPointer Message,
    TargetPointer InnerException,
    TargetPointer StackTrace,
    TargetPointer WatsonBuckets,
    TargetPointer StackTraceString,
    TargetPointer RemoteStackTraceString,
    int HResult,
    int XCode);

public interface IException : IContract
{
    static string IContract.Name { get; } = nameof(Exception);

    public virtual TargetPointer GetNestedExceptionInfo(TargetPointer exception, out TargetPointer nextNestedException) => throw new NotImplementedException();
    public virtual ExceptionData GetExceptionData(TargetPointer managedException) => throw new NotImplementedException();
}

public readonly struct Exception : IException
{
    // Everything throws NotImplementedException
}
