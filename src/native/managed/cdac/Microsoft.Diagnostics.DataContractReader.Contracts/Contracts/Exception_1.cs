// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Exception_1 : IException
{
    // STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE flag value from src/coreclr/vm/clrex.h.
    private const int STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE = 0x0001;

    private readonly Target _target;

    internal Exception_1(Target target)
    {
        _target = target;
    }

    TargetPointer IException.GetNestedExceptionInfo(TargetPointer exceptionInfoAddr, out TargetPointer nextNestedExceptionInfo, out TargetPointer thrownObjectHandle)
    {
        Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exceptionInfoAddr);
        nextNestedExceptionInfo = exceptionInfo.PreviousNestedInfo;
        // ThrownObject is a direct object pointer stored in ExInfo::m_exception.
        // Return the address of the field as a "handle" - reading through it yields the
        // exception Object*. This has the same lifetime as the ExInfo (both are invalidated
        // when PopExInfos calls ReleaseResources). See dacimpl.h for the equivalent native
        // DAC documentation.
        Target.TypeInfo type = _target.GetTypeInfo(DataType.ExceptionInfo);
        thrownObjectHandle = exceptionInfoAddr + (ulong)type.Fields[nameof(Data.ExceptionInfo.ThrownObject)].Offset;
        return exceptionInfo.ThrownObject;
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

    IEnumerable<ExceptionStackFrameInfo> IException.GetExceptionStackFrames(TargetPointer exceptionAddr)
    {
        Data.Exception exception = _target.ProcessedData.GetOrAdd<Data.Exception>(exceptionAddr);
        TargetPointer stackTraceObj = exception.StackTrace;
        if (stackTraceObj == TargetPointer.Null)
            yield break;

        // Path 1: the stack trace object's MethodTable ContainsGCPointers. The object is a
        //         combined object[] whose first slot is the actual stack-trace I1Array and
        //         whose subsequent slots are the keep-alive references. We unwrap to slot 0.
        // Path 2: the stack trace object is itself the I1Array payload.
        IObject objectContract = _target.Contracts.Object;
        IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;

        TargetPointer mt = objectContract.GetMethodTableAddress(stackTraceObj);
        if (mt == TargetPointer.Null)
            throw new InvalidOperationException($"Stack trace object 0x{stackTraceObj.Value:x} has no MethodTable.");
        ITypeHandle stackTraceHandle = rtsContract.GetTypeHandle(mt);

        TargetPointer i1ArrayAddr;
        if (rtsContract.ContainsGCPointers(stackTraceHandle))
        {
            // Combined PTRArray; slot 0 holds the I1Array pointer.
            Data.Array combinedArray = _target.ProcessedData.GetOrAdd<Data.Array>(stackTraceObj);
            i1ArrayAddr = _target.ReadPointer(combinedArray.DataPointer);
        }
        else
        {
            i1ArrayAddr = stackTraceObj;
        }

        if (i1ArrayAddr == TargetPointer.Null)
            yield break;

        Data.Array i1Array = _target.ProcessedData.GetOrAdd<Data.Array>(i1ArrayAddr);
        TargetPointer payload = i1Array.DataPointer;

        Data.StackTraceArrayHeader header = _target.ProcessedData.GetOrAdd<Data.StackTraceArrayHeader>(payload);
        uint frameCount = header.Size;
        if (frameCount == 0)
            yield break;

        Target.TypeInfo elementTypeInfo = _target.GetTypeInfo(DataType.StackTraceElement);
        ulong elementSize = elementTypeInfo.Size!.Value;

        uint headerSize = _target.GetTypeInfo(DataType.StackTraceArrayHeader).Size!.Value;
        TargetPointer cursor = payload + headerSize;
        for (uint i = 0; i < frameCount; i++)
        {
            Data.StackTraceElement element = _target.ProcessedData.GetOrAdd<Data.StackTraceElement>(cursor);
            yield return new ExceptionStackFrameInfo(
                element.Ip,
                element.MethodDesc,
                (element.Flags & STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE) != 0);
            cursor += elementSize;
        }
    }
}
