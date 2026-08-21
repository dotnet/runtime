// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTask : IXCLRDataTask
{
    private readonly Lock _apiLock;
    private readonly TargetPointer _address;
    private readonly Target _target;
    private readonly IXCLRDataTask? _legacyImpl;

    public ClrDataTask(TargetPointer address, Target target, IXCLRDataTask? legacyImpl, Lock apiLock)
    {
        _apiLock = apiLock;
        _address = address;
        _target = target;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataTask.GetProcess(/*IXCLRDataProcess*/ void** process)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.GetCurrentAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK, hrLocal = HResults.S_OK;
        IXCLRDataAppDomain? legacyAppDomain = null;

        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataAppDomain> legacyOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetCurrentAppDomain(legacyOut);
            legacyAppDomain = legacyOut.Interface;
        }
        try
        {
            TargetPointer currentAppDomain = _target.Contracts.Loader.GetAppDomain();
            appDomain.Interface = new ClrDataAppDomain(_target, currentAppDomain, legacyAppDomain, _apiLock);
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
    int IXCLRDataTask.GetUniqueID(ulong* id)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.GetFlags(uint* flags)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.IsSameObject(IXCLRDataTask* task)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.GetDesiredExecutionState(uint* state)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.SetDesiredExecutionState(uint state)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataTask.CreateStackWalk(CLRDataStackWalkFlag flags, DacComNullableByRef<IXCLRDataStackWalk> stackWalk)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(_address);
        if (threadData.State.HasFlag(Contracts.ThreadState.Unstarted))
            return HResults.E_FAIL;

        IXCLRDataStackWalk? legacyStackWalk = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataStackWalk> legacyStackWalkOut = new(isNullRef: false);
            int hr = _legacyImpl.CreateStackWalk(flags, legacyStackWalkOut);
            if (hr < 0)
                return hr;
            legacyStackWalk = legacyStackWalkOut.Interface;
        }

        stackWalk.Interface = new ClrDataStackWalk(_address, flags, _target, legacyStackWalk, _apiLock);
        return HResults.S_OK;
    }

    int IXCLRDataTask.GetOSThreadID(uint* id)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.SetContext(uint contextSize, byte* context)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataTask.GetCurrentExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK, hrLocal = HResults.S_OK;
        IXCLRDataExceptionState? legacyExceptionState = null;

        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataExceptionState> legacyExceptionStateOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetCurrentExceptionState(legacyExceptionStateOut);
            legacyExceptionState = legacyExceptionStateOut.Interface;
        }
        try
        {
            TargetPointer thrownObjectHandle = _target.Contracts.Thread.GetCurrentExceptionHandle(_address);
            if (thrownObjectHandle == TargetPointer.Null)
            {
                throw Marshal.GetExceptionForHR(/*E_NOINTERFACE*/ HResults.COR_E_INVALIDCAST)!;
            }
            else
            {
                Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(_address);
                exception.Interface = new ClrDataExceptionState(_target, _address, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT, TargetPointer.Null, thrownObjectHandle, threadData.FirstNestedException, legacyExceptionState, _apiLock);
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

    int IXCLRDataTask.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;

        try
        {
            if (reqCode != (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION
                || inBufferSize != 0
                || inBuffer is not null
                || outBufferSize != sizeof(uint))
            {
                throw new ArgumentException("Invalid request parameters.");
            }

            if (outBuffer is null)
                throw new NullReferenceException("The output buffer is null.");

            *(uint*)outBuffer = 3;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint revisionLocal = 0;
            int hrLocal = _legacyImpl.Request(
                reqCode,
                inBufferSize,
                inBuffer,
                outBufferSize,
                outBuffer is null ? null : (byte*)&revisionLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*(uint*)outBuffer == revisionLocal);
        }
#endif

        return hr;
    }
    int IXCLRDataTask.GetName(uint bufLen, uint* nameLen, char* nameBuffer)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }
    int IXCLRDataTask.GetLastExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK, hrLocal = HResults.S_OK;
        IXCLRDataExceptionState? legacyExceptionState = null;

        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataExceptionState> legacyExceptionStateOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetLastExceptionState(legacyExceptionStateOut);
            legacyExceptionState = legacyExceptionStateOut.Interface;
        }
        try
        {
            Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(_address);
            TargetPointer thrownObjectHandle = threadData.LastThrownObjectHandle;
            if (thrownObjectHandle == TargetPointer.Null)
            {
                throw Marshal.GetExceptionForHR(/*E_NOINTERFACE*/ HResults.COR_E_INVALIDCAST)!;
            }
            else
            {
                exception.Interface = new ClrDataExceptionState(_target, _address, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL, TargetPointer.Null, thrownObjectHandle, TargetPointer.Null, legacyExceptionState, _apiLock);
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
}
