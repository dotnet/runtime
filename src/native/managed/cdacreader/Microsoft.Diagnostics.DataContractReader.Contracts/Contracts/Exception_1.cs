// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Exception_1 : IException
{
    private readonly Target _target;

    internal Exception_1(Target target)
    {
        _target = target;
    }

    TargetPointer IException.GetNestedExceptionInfo(TargetPointer exceptionInfoAddr, out TargetPointer nextNestedExceptionInfo)
    {
        Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exceptionInfoAddr);
        nextNestedExceptionInfo = exceptionInfo.PreviousNestedInfo;
        return exceptionInfo.ThrownObject.Object;
    }

    ExceptionData IException.GetExceptionData(TargetPointer exceptionAddr)
    {
        Data.Exception exception = _target.ProcessedData.GetOrAdd<Data.Exception>(exceptionAddr);
        return new ExceptionData(
            exception.Message,
            exception.InnerException,
            exception.StackTrace,
            exception.WatsonBuckets,
            exception.StackTraceString,
            exception.RemoteStackTraceString,
            exception.HResult,
            exception.XCode);
    }
}
