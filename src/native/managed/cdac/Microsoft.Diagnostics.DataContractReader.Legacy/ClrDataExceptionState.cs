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
    private readonly TargetPointer _thrownObjectHandle;
    private readonly TargetPointer _previousExInfoAddress;
    private readonly IXCLRDataExceptionState? _legacyImpl;

    public ClrDataExceptionState(
        Target target,
        TargetPointer threadAddress,
        uint flags,
        TargetPointer thrownObjectHandle,
        TargetPointer previousExInfoAddress,
        IXCLRDataExceptionState? legacyImpl)
    {
        _target = target;
        _threadAddress = threadAddress;
        _flags = flags;
        _thrownObjectHandle = thrownObjectHandle;
        _previousExInfoAddress = previousExInfoAddress;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataExceptionState.GetFlags(uint* flags)
    {
        int hr = HResults.S_OK;
        try
        {
            *flags = _flags;
            if (_previousExInfoAddress != TargetPointer.Null)
                *flags |= (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint legacyFlags;
            int hrLocal = _legacyImpl.GetFlags(&legacyFlags);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*flags == legacyFlags, $"cDAC flags: {*flags:x}, DAC flags: {legacyFlags:x}");
        }
#endif

        return hr;
    }

    int IXCLRDataExceptionState.GetPrevious(IXCLRDataExceptionState** exState)
        => _legacyImpl is not null ? _legacyImpl.GetPrevious(exState) : HResults.E_NOTIMPL;

    int IXCLRDataExceptionState.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
        => _legacyImpl is not null ? _legacyImpl.GetManagedObject(value) : HResults.E_NOTIMPL;

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
                    *strLen = 0;
                if (bufLen >= 1)
                    str[0] = '\0';
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
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] strLocal = new char[(int)bufLen];
            uint legacyStrLen;
            int hrLocal;
            fixed (char* strLocalPtr = strLocal)
            {
                hrLocal = _legacyImpl.GetString(bufLen, &legacyStrLen, strLocalPtr);
            }
            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(strLen == null || *strLen == legacyStrLen);
            int cmpLen = Math.Min((int)legacyStrLen, (int)bufLen) - 1;
            Debug.Assert(str == null || cmpLen <= 0 || new ReadOnlySpan<char>(strLocal, 0, cmpLen).SequenceEqual(new ReadOnlySpan<char>(str, cmpLen)));
        }
#endif

        return hr;
    }

    int IXCLRDataExceptionState.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        if (reqCode == (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION)
        {
            if (inBufferSize != 0 || inBuffer is not null || outBufferSize != sizeof(uint))
                return HResults.E_INVALIDARG;

            *(uint*)outBuffer = 2;
            return HResults.S_OK;
        }

        return HResults.E_INVALIDARG;
    }

    int IXCLRDataExceptionState.IsSameState(/*EXCEPTION_RECORD64*/ void* exRecord, uint contextSize, byte* cxRecord)
        => _legacyImpl is not null ? _legacyImpl.IsSameState(exRecord, contextSize, cxRecord) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.IsSameState2(uint flags, /*EXCEPTION_RECORD64*/ void* exRecord, uint contextSize, byte* cxRecord)
        => _legacyImpl is not null ? _legacyImpl.IsSameState2(flags, exRecord, contextSize, cxRecord) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.GetTask(DacComNullableByRef<IXCLRDataTask> task)
    {
        int hr = HResults.S_OK, hrLocal = HResults.S_OK;

        IXCLRDataTask? legacyTask = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataTask> legacyTaskOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetTask(legacyTaskOut);
            legacyTask = legacyTaskOut.Interface;
        }
        try
        {
            task.Interface = new ClrDataTask(_threadAddress, _target, legacyTask);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }
}
