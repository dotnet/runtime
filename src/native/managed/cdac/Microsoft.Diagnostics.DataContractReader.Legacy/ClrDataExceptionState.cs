// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataExceptionState : IXCLRDataExceptionState
{
    private readonly Target _target;
    private readonly TargetPointer _threadAddress;
    private readonly uint _flags;
    private readonly TargetPointer _exceptionInfoAddress;
    private readonly TargetPointer _thrownObjectHandle;
    private readonly TargetPointer _previousExInfoAddress;

    public ClrDataExceptionState(
        Target target,
        TargetPointer threadAddress,
        uint flags,
        TargetPointer exceptionInfoAddress,
        TargetPointer thrownObjectHandle,
        TargetPointer previousExInfoAddress)
    {
        _target = target;
        _threadAddress = threadAddress;
        _flags = flags;
        _exceptionInfoAddress = exceptionInfoAddress;
        _thrownObjectHandle = thrownObjectHandle;
        _previousExInfoAddress = previousExInfoAddress;
    }

    int IXCLRDataExceptionState.GetFlags(uint* flags)
    {
        int hr = HResults.S_OK;
        try
        {
            if (flags is null)
                throw new ArgumentNullException(nameof(flags));

            *flags = _flags;
            if (_previousExInfoAddress != TargetPointer.Null)
                *flags |= (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int IXCLRDataExceptionState.GetPrevious(DacComNullableByRef<IXCLRDataExceptionState> exState)
    {
        int hr = HResults.S_OK;

        try
        {
            if (_previousExInfoAddress == TargetPointer.Null)
            {
                hr = HResults.S_FALSE;
            }
            else
            {
                _target.Contracts.Exception.GetNestedExceptionInfo(
                    _previousExInfoAddress,
                    out TargetPointer nextNestedException,
                    out TargetPointer prevExThrownObjectHandle);
                exState.Interface = new ClrDataExceptionState(
                    _target,
                    _threadAddress,
                    (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
                    _previousExInfoAddress,
                    prevExThrownObjectHandle,
                    nextNestedException
                );
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int IXCLRDataExceptionState.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
        => HResults.E_NOTIMPL;

    int IXCLRDataExceptionState.GetBaseType(/*CLRDataBaseExceptionType*/ uint* type) => HResults.E_NOTIMPL;

    int IXCLRDataExceptionState.GetCode(uint* code) => HResults.E_NOTIMPL;

    int IXCLRDataExceptionState.GetString(uint bufLen, uint* strLen, char* str)
    {
        int hr = HResults.S_OK;
        try
        {
            TargetPointer exceptionObject = _target.ReadPointer(_thrownObjectHandle);
            ExceptionData exceptionData = _target.Contracts.Exception.GetExceptionData(exceptionObject);
            if (exceptionData.Message == TargetPointer.Null)
            {
                if (strLen is not null)
                {
                    *strLen = 0;
                }

                if (bufLen >= 1)
                {
                    if (str is null)
                    {
                        hr = HResults.E_INVALIDARG;
                    }
                    else
                    {
                        str[0] = '\0';
                    }
                }
            }
            else
            {
                string message = _target.Contracts.Object.GetStringValue(exceptionData.Message);
                OutputBufferHelpers.CopyStringToBuffer(str, bufLen, strLen, message);
                if (str is not null && bufLen < (uint)(message.Length + 1))
                    hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int IXCLRDataExceptionState.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        int hr = HResults.E_INVALIDARG;

        if (reqCode == (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION)
        {
            if (inBufferSize == 0 && inBuffer is null && outBufferSize == sizeof(uint) && outBuffer is not null)
            {
                *(uint*)outBuffer = 2;
                hr = HResults.S_OK;
            }
        }
        return hr;
    }

    int IXCLRDataExceptionState.IsSameState(EXCEPTION_RECORD64* exRecord, uint contextSize, byte* cxRecord)
    {
        int hr = IsSameState2((uint)CLRDataExceptionSameFlag.CLRDATA_EXSAME_SECOND_CHANCE, exRecord);
        return hr;
    }

    int IXCLRDataExceptionState.IsSameState2(uint flags, EXCEPTION_RECORD64* exRecord, uint contextSize, byte* cxRecord)
    {
        int hr = IsSameState2(flags, exRecord);
        return hr;
    }

    private int IsSameState2(uint flags, EXCEPTION_RECORD64* exRecord)
    {
        int hr = HResults.S_FALSE;
        try
        {
            if ((flags & ~(uint)(CLRDataExceptionSameFlag.CLRDATA_EXSAME_SECOND_CHANCE | CLRDataExceptionSameFlag.CLRDATA_EXSAME_FIRST_CHANCE)) != 0)
                throw new ArgumentException();

            if ((_flags & (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL) != 0)
            {
                if ((flags & (uint)CLRDataExceptionSameFlag.CLRDATA_EXSAME_FIRST_CHANCE) != 0)
                    hr = HResults.S_OK;
            }
            else
            {
                if (exRecord is null)
                    throw new NullReferenceException();

                TargetPointer exceptionRecord;
                if (_exceptionInfoAddress != TargetPointer.Null)
                {
                    Target.TypeInfo exceptionInfoType = _target.GetTypeInfo(DataType.ExceptionInfo);
                    exceptionRecord = _target.ReadPointer(
                        _exceptionInfoAddress + (ulong)exceptionInfoType.Fields["ExceptionRecord"].Offset);
                }
                else
                {
                    ThreadData threadData = _target.Contracts.Thread.GetThreadData(_threadAddress);
                    exceptionRecord = threadData.OSExceptionRecord;
                }

                TargetPointer exceptionAddress = _target.ReadPointer(
                    exceptionRecord + (ulong)(sizeof(uint) * 2 + _target.PointerSize));
                TargetPointer requestedAddress = new(
                    _target.PointerSize == sizeof(ulong)
                        ? exRecord->ExceptionAddress
                        : (uint)exRecord->ExceptionAddress);

                if (exceptionAddress == requestedAddress)
                    hr = HResults.S_OK;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int IXCLRDataExceptionState.GetTask(DacComNullableByRef<IXCLRDataTask> task)
    {
        int hr = HResults.S_OK;

        try
        {
            task.Interface = new ClrDataTask(_threadAddress, _target);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        return hr;
    }
}
