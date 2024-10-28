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

    public int GetProcess(/*IXCLRDataProcess*/ void** process)
        => _legacyImpl is not null ? _legacyImpl.GetProcess(process) : HResults.E_NOTIMPL;
    public int GetCurrentAppDomain(/*IXCLRDataAppDomain*/ void** appDomain)
        => _legacyImpl is not null ? _legacyImpl.GetCurrentAppDomain(appDomain) : HResults.E_NOTIMPL;
    public int GetUniqueID(ulong* id)
        => _legacyImpl is not null ? _legacyImpl.GetUniqueID(id) : HResults.E_NOTIMPL;
    public int GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;
    public int IsSameObject(IXCLRDataTask* task)
        => _legacyImpl is not null ? _legacyImpl.IsSameObject(task) : HResults.E_NOTIMPL;
    public int GetManagedObject(/*IXCLRDataValue*/ void** value)
        => _legacyImpl is not null ? _legacyImpl.GetManagedObject(value) : HResults.E_NOTIMPL;
    public int GetDesiredExecutionState(uint* state)
        => _legacyImpl is not null ? _legacyImpl.GetDesiredExecutionState(state) : HResults.E_NOTIMPL;
    public int SetDesiredExecutionState(uint state)
        => _legacyImpl is not null ? _legacyImpl.SetDesiredExecutionState(state) : HResults.E_NOTIMPL;
    public int CreateStackWalk(uint flags, /*IXCLRDataStackWalk*/ void** stackWalk)
        => _legacyImpl is not null ? _legacyImpl.CreateStackWalk(flags, stackWalk) : HResults.E_NOTIMPL;
    public int GetOSThreadID(uint* id)
        => _legacyImpl is not null ? _legacyImpl.GetOSThreadID(id) : HResults.E_NOTIMPL;
    public int GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer)
        => _legacyImpl is not null ? _legacyImpl.GetContext(contextFlags, contextBufSize, contextSize, contextBuffer) : HResults.E_NOTIMPL;
    public int SetContext(uint contextSize, byte* context)
        => _legacyImpl is not null ? _legacyImpl.SetContext(contextSize, context) : HResults.E_NOTIMPL;
    public int GetCurrentExceptionState(/*IXCLRDataExceptionState*/ void** exception)
        => _legacyImpl is not null ? _legacyImpl.GetCurrentExceptionState(exception) : HResults.E_NOTIMPL;
    public int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
    public int GetName(uint bufLen, uint* nameLen, char* nameBuffer)
        => _legacyImpl is not null ? _legacyImpl.GetName(bufLen, nameLen, nameBuffer) : HResults.E_NOTIMPL;
    public int GetLastExceptionState(/*IXCLRDataExceptionState*/ void** exception)
        => _legacyImpl is not null ? _legacyImpl.GetLastExceptionState(exception) : HResults.E_NOTIMPL;
}
