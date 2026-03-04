// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataExceptionState : IXCLRDataExceptionState
{
    private readonly Target _target;
    private readonly TargetPointer _threadAddress;
    private readonly TargetPointer _exceptionInfoAddress;
    private readonly IXCLRDataExceptionState? _legacyImpl;

    public ClrDataExceptionState(
        Target target,
        TargetPointer threadAddress,
        TargetPointer exceptionInfoAddress,
        IXCLRDataExceptionState? legacyImpl)
    {
        _target = target;
        _threadAddress = threadAddress;
        _exceptionInfoAddress = exceptionInfoAddress;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataExceptionState.GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.GetPrevious(IXCLRDataExceptionState** exState)
        => _legacyImpl is not null ? _legacyImpl.GetPrevious(exState) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.GetManagedObject(IXCLRDataValue** value)
        => _legacyImpl is not null ? _legacyImpl.GetManagedObject(value) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.GetBaseType(/*CLRDataBaseExceptionType*/ uint* type)
        => _legacyImpl is not null ? _legacyImpl.GetBaseType(type) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.GetCode(uint* code)
        => _legacyImpl is not null ? _legacyImpl.GetCode(code) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.GetString(uint bufLen, uint* strLen, char* str)
        => _legacyImpl is not null ? _legacyImpl.GetString(bufLen, strLen, str) : HResults.E_NOTIMPL;

    int IXCLRDataExceptionState.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataExceptionState.IsSameState(/*EXCEPTION_RECORD64*/ void* exRecord, uint contextSize, byte* cxRecord)
        => _legacyImpl is not null ? _legacyImpl.IsSameState(exRecord, contextSize, cxRecord) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.IsSameState2(uint flags, /*EXCEPTION_RECORD64*/ void* exRecord, uint contextSize, byte* cxRecord)
        => _legacyImpl is not null ? _legacyImpl.IsSameState2(flags, exRecord, contextSize, cxRecord) : HResults.E_NOTIMPL;
    int IXCLRDataExceptionState.GetTask(IXCLRDataTask** task)
        => _legacyImpl is not null ? _legacyImpl.GetTask(task) : HResults.E_NOTIMPL;
}
