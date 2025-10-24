// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataTask : IXCLRDataTask
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
        => _legacyImpl is not null ? _legacyImpl.GetProcess(process) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetCurrentAppDomain(/*IXCLRDataAppDomain*/ void** appDomain)
        => _legacyImpl is not null ? _legacyImpl.GetCurrentAppDomain(appDomain) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetUniqueID(ulong* id)
        => _legacyImpl is not null ? _legacyImpl.GetUniqueID(id) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;
    int IXCLRDataTask.IsSameObject(IXCLRDataTask* task)
        => _legacyImpl is not null ? _legacyImpl.IsSameObject(task) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetManagedObject(/*IXCLRDataValue*/ void** value)
        => _legacyImpl is not null ? _legacyImpl.GetManagedObject(value) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetDesiredExecutionState(uint* state)
        => _legacyImpl is not null ? _legacyImpl.GetDesiredExecutionState(state) : HResults.E_NOTIMPL;
    int IXCLRDataTask.SetDesiredExecutionState(uint state)
        => _legacyImpl is not null ? _legacyImpl.SetDesiredExecutionState(state) : HResults.E_NOTIMPL;

    int IXCLRDataTask.CreateStackWalk(uint flags, out IXCLRDataStackWalk? stackWalk)
    {
        stackWalk = default;

        Contracts.ThreadData threadData = _target.Contracts.Thread.GetThreadData(_address);
        if (threadData.State.HasFlag(Contracts.ThreadState.Unstarted))
            return HResults.E_FAIL;

        IXCLRDataStackWalk? legacyStackWalk = null;
        if (_legacyImpl is not null)
        {
            int hr = _legacyImpl.CreateStackWalk(flags, out legacyStackWalk);
            if (hr < 0)
                return hr;
        }

        stackWalk = new ClrDataStackWalk(_address, flags, _target, legacyStackWalk);
        return HResults.S_OK;
    }

    int IXCLRDataTask.GetOSThreadID(uint* id)
        => _legacyImpl is not null ? _legacyImpl.GetOSThreadID(id) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer)
        => _legacyImpl is not null ? _legacyImpl.GetContext(contextFlags, contextBufSize, contextSize, contextBuffer) : HResults.E_NOTIMPL;
    int IXCLRDataTask.SetContext(uint contextSize, byte* context)
        => _legacyImpl is not null ? _legacyImpl.SetContext(contextSize, context) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetCurrentExceptionState(/*IXCLRDataExceptionState*/ void** exception)
        => _legacyImpl is not null ? _legacyImpl.GetCurrentExceptionState(exception) : HResults.E_NOTIMPL;
    int IXCLRDataTask.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetName(uint bufLen, uint* nameLen, char* nameBuffer)
        => _legacyImpl is not null ? _legacyImpl.GetName(bufLen, nameLen, nameBuffer) : HResults.E_NOTIMPL;
    int IXCLRDataTask.GetLastExceptionState(/*IXCLRDataExceptionState*/ void** exception)
        => _legacyImpl is not null ? _legacyImpl.GetLastExceptionState(exception) : HResults.E_NOTIMPL;
}
