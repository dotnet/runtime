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

    public ClrDataTask(TargetPointer address, Target target)
    {
        _address = address;
        _target = target;
    }

    int IXCLRDataTask.GetProcess(/*IXCLRDataProcess*/ void** process)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.GetCurrentAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        int hr = HResults.S_OK;

        try
        {
            TargetPointer currentAppDomain = _target.Contracts.Loader.GetAppDomain();
            appDomain.Interface = new ClrDataAppDomain(_target, currentAppDomain);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        return hr;
    }
    int IXCLRDataTask.GetUniqueID(ulong* id)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.GetFlags(uint* flags)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.IsSameObject(IXCLRDataTask* task)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.GetDesiredExecutionState(uint* state)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.SetDesiredExecutionState(uint state)
        => HResults.E_NOTIMPL;

    int IXCLRDataTask.CreateStackWalk(uint flags, DacComNullableByRef<IXCLRDataStackWalk> stackWalk)
    {
        Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(_address);
        if (threadData.State.HasFlag(Contracts.ThreadState.Unstarted))
            return HResults.E_FAIL;

        stackWalk.Interface = new ClrDataStackWalk(_address, flags, _target);
        return HResults.S_OK;
    }

    int IXCLRDataTask.GetOSThreadID(uint* id)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.SetContext(uint contextSize, byte* context)
        => HResults.E_NOTIMPL;

    int IXCLRDataTask.GetCurrentExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
        int hr = HResults.S_OK;

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
                exception.Interface = new ClrDataExceptionState(_target, _address, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT, TargetPointer.Null, thrownObjectHandle, threadData.FirstNestedException);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        return hr;
    }

    int IXCLRDataTask.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
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


        return hr;
    }
    int IXCLRDataTask.GetName(uint bufLen, uint* nameLen, char* nameBuffer)
        => HResults.E_NOTIMPL;
    int IXCLRDataTask.GetLastExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
        int hr = HResults.S_OK;

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
                exception.Interface = new ClrDataExceptionState(_target, _address, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL, TargetPointer.Null, thrownObjectHandle, TargetPointer.Null);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        return hr;
    }
}
