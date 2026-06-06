// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTask : IXCLRDataTask
{
    private readonly TargetPointer _address;
    private readonly Target _target;
    private readonly IXCLRDataTask? _legacyImpl;

    public ClrDataTask(TargetPointer address, Target target, IXCLRDataTask? legacyImpl)
    {
        _address = address;
        _target = target;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataTask.GetProcess(/*IXCLRDataProcess*/ void** process)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetProcess(process) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetCurrentAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
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
            TargetPointer currentAppDomain = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.AppDomain));
            appDomain.Interface = new ClrDataAppDomain(_target, currentAppDomain, legacyAppDomain);
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
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetUniqueID(id) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetFlags(uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;
    int IXCLRDataTask.IsSameObject(IXCLRDataTask* task)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.IsSameObject(task) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetManagedObject(value) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetDesiredExecutionState(uint* state)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetDesiredExecutionState(state) : HResults.E_NOTIMPL;
    int IXCLRDataTask.SetDesiredExecutionState(uint state)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.SetDesiredExecutionState(state) : HResults.E_NOTIMPL;

    int IXCLRDataTask.CreateStackWalk(uint flags, DacComNullableByRef<IXCLRDataStackWalk> stackWalk)
    {
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

        stackWalk.Interface = new ClrDataStackWalk(_address, flags, _target, legacyStackWalk);
        return HResults.S_OK;
    }

    int IXCLRDataTask.GetOSThreadID(uint* id)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetOSThreadID(id) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetContext(contextFlags, contextBufSize, contextSize, contextBuffer) : HResults.E_NOTIMPL;
    int IXCLRDataTask.SetContext(uint contextSize, byte* context)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.SetContext(contextSize, context) : HResults.E_NOTIMPL;

    int IXCLRDataTask.GetCurrentExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
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
                exception.Interface = new ClrDataExceptionState(_target, _address, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT, thrownObjectHandle, threadData.FirstNestedException, legacyExceptionState);
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
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetName(uint bufLen, uint* nameLen, char* nameBuffer)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetName(bufLen, nameLen, nameBuffer) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetLastExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
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
                exception.Interface = new ClrDataExceptionState(_target, _address, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL, thrownObjectHandle, TargetPointer.Null, legacyExceptionState);
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
