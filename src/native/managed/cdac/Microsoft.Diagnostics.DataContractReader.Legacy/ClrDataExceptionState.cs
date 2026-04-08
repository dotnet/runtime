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

    int IXCLRDataExceptionState.GetPrevious(DacComNullableByRef<IXCLRDataExceptionState> exState)
    {
        int hr = HResults.S_OK, hrLocal = HResults.S_OK;
        IXCLRDataExceptionState? legacyPrevious = null;

        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataExceptionState> legacyPreviousOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetPrevious(legacyPreviousOut);
            legacyPrevious = legacyPreviousOut.Interface;
        }
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
                    prevExThrownObjectHandle,
                    nextNestedException,
                    legacyPrevious
                );
            }
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
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] strLocal = new char[Math.Max((int)bufLen, 1)];
            uint legacyStrLen;
            int hrLocal;
            fixed (char* strLocalPtr = strLocal)
            {
                hrLocal = _legacyImpl.GetString(bufLen, &legacyStrLen, str is null ? null : strLocalPtr);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
            {
                Debug.Assert(strLen == null || *strLen == legacyStrLen);
                int cmpLen = Math.Min((int)legacyStrLen, (int)bufLen) - 1;
                Debug.Assert(str == null || cmpLen <= 0 || new ReadOnlySpan<char>(strLocal, 0, cmpLen).SequenceEqual(new ReadOnlySpan<char>(str, cmpLen)));
            }
        }
#endif

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
#if DEBUG
        if (_legacyImpl is not null)
        {
            byte[] localBuffer = new byte[(int)outBufferSize];
            fixed (byte* localOutBuffer = localBuffer)
            {
                int hrLocal = _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, localOutBuffer);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK && reqCode == (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION)
                {
                    Debug.Assert(outBufferSize == sizeof(uint) && outBuffer is not null);
                    uint legacyRevision = *(uint*)localOutBuffer;
                    uint revision = *(uint*)outBuffer;
                    Debug.Assert(revision == legacyRevision);
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataExceptionState.IsSameState(EXCEPTION_RECORD64* exRecord, uint contextSize, byte* cxRecord)
        => _legacyImpl is not null ? _legacyImpl.IsSameState(exRecord, contextSize, cxRecord) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.IsSameState2(uint flags, EXCEPTION_RECORD64* exRecord, uint contextSize, byte* cxRecord)
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
