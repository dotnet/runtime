// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct WindowsErrorReporting_1 : IWindowsErrorReporting
{
    private readonly Target _target;

    public WindowsErrorReporting_1(Target target)
    {
        _target = target;
    }

    byte[] IWindowsErrorReporting.GetWatsonBuckets(TargetPointer threadPointer)
    {
        TargetPointer readFrom;
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);
        TargetPointer exceptionTrackerPtr = _target.ReadPointer(thread.ExceptionTracker);
        Data.ExceptionInfo? exceptionInfo = (exceptionTrackerPtr == TargetPointer.Null) ? null : _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exceptionTrackerPtr);
        if (exceptionInfo == null)
            return Array.Empty<byte>();
        TargetPointer thrownObject = exceptionInfo.ThrownObject;
        if (thrownObject != TargetPointer.Null)
        {
            Data.Exception exception = _target.ProcessedData.GetOrAdd<Data.Exception>(thrownObject);
            if (exception.WatsonBuckets != TargetPointer.Null)
            {
                readFrom = _target.Contracts.Object.GetArrayData(exception.WatsonBuckets, out _, out _, out _);
            }
            else
            {
                readFrom = thread.UEWatsonBucketTrackerBuckets ?? TargetPointer.Null;
                if (readFrom == TargetPointer.Null)
                {
                    readFrom = exceptionInfo.ExceptionWatsonBucketTrackerBuckets ?? TargetPointer.Null;
                }
                else
                {
                    return Array.Empty<byte>();
                }
            }
        }
        else
        {
            readFrom = thread.UEWatsonBucketTrackerBuckets ?? TargetPointer.Null;
        }

        if (readFrom == TargetPointer.Null)
            return Array.Empty<byte>();

        byte[] rval = new byte[_target.ReadGlobal<uint>(Constants.Globals.SizeOfGenericModeBlock)];
        _target.ReadBuffer(readFrom, rval);
        return rval;
    }
}
