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

    TargetPointer IException.GetExceptionInfo(TargetPointer exception, out TargetPointer nextNestedException)
    {
        Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exception);
        nextNestedException = exceptionInfo.PreviousNestedInfo;
        return exceptionInfo.ThrownObject.Object;
    }
}
