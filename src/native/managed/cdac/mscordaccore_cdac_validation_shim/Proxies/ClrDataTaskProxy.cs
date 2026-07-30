// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Validation-shim proxy. Each method calls the production cDAC first (its result is always the one
// returned to the caller), then calls the legacy DAC and compares. The `#if DEBUG` comparison blocks
// are the pre-refactor cDAC blocks, recovered verbatim from the implementations that hosted the
// legacy DAC before the production decoupling; `hr` is the production cDAC result and the `_legacy*`
// fields are the legacy DAC's interfaces, exactly as they were in the original code.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Paired cDAC/DAC proxy for IXCLRDataTask.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataTaskProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataTask
{
    private readonly IXCLRDataTask? _cdacImpl;
    private readonly IXCLRDataTask? _legacyImpl;

    internal ClrDataTaskProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataTask;
        _legacyImpl = dacObject as IXCLRDataTask;
    }

    /// <summary>
    /// Mirrors the production cDAC object's QueryInterface surface exactly: an interface is only
    /// exposed to the caller when the object being proxied exposes it, so consumers cannot observe
    /// a capability the cDAC does not actually have.
    /// </summary>
    public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
    {
        ppv = default;
        CustomQueryInterfaceResult? custom = null;
        GetCustomInterface(ref iid, ref ppv, ref custom);
        if (custom is not null)
            return custom.Value;

        if (iid == typeof(IXCLRDataTask).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataTask
    int IXCLRDataTask.GetProcess(/*IXCLRDataProcess*/ void** process)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetProcess(process) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.GetCurrentAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataAppDomain> appDomainCDac = new(appDomain.IsNullRef);
        DacComNullableByRef<IXCLRDataAppDomain> appDomainDac = new(appDomain.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetCurrentAppDomain(appDomainCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetCurrentAppDomain(appDomainDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!appDomain.IsNullRef)
            appDomain.Interface = ShimProxy.PairIXCLRDataAppDomain(_session, appDomainCDac.Interface, appDomainDac.Interface);
        return hr;
    }

    int IXCLRDataTask.GetUniqueID(ulong* id)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetUniqueID(id) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.IsSameObject(IXCLRDataTask* task)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameObject(task) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetManagedObject(value) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.GetDesiredExecutionState(uint* state)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetDesiredExecutionState(state) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.SetDesiredExecutionState(uint state)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.SetDesiredExecutionState(state) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.CreateStackWalk(uint flags, DacComNullableByRef<IXCLRDataStackWalk> stackWalk)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataStackWalk> stackWalkCDac = new(stackWalk.IsNullRef);
        DacComNullableByRef<IXCLRDataStackWalk> stackWalkDac = new(stackWalk.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.CreateStackWalk(flags, stackWalkCDac) : HResults.E_NOTIMPL;
        if (hr >= 0 && _legacyImpl is not null)
        {
            _legacyImpl.CreateStackWalk(flags, stackWalkDac);
        }
        if (!stackWalk.IsNullRef)
            stackWalk.Interface = ShimProxy.PairIXCLRDataStackWalk(_session, stackWalkCDac.Interface, stackWalkDac.Interface);
        return hr;
    }

    int IXCLRDataTask.GetOSThreadID(uint* id)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetOSThreadID(id) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, byte* contextBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetContext(contextFlags, contextBufSize, contextSize, contextBuffer) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.SetContext(uint contextSize, byte* context)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.SetContext(contextSize, context) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.GetCurrentExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataExceptionState> exceptionCDac = new(exception.IsNullRef);
        DacComNullableByRef<IXCLRDataExceptionState> exceptionDac = new(exception.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetCurrentExceptionState(exceptionCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetCurrentExceptionState(exceptionDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!exception.IsNullRef)
            exception.Interface = ShimProxy.PairIXCLRDataExceptionState(_session, exceptionCDac.Interface, exceptionDac.Interface);
        return hr;
    }

    int IXCLRDataTask.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
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
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetName(bufLen, nameLen, nameBuffer) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataTask.GetLastExceptionState(DacComNullableByRef<IXCLRDataExceptionState> exception)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataExceptionState> exceptionCDac = new(exception.IsNullRef);
        DacComNullableByRef<IXCLRDataExceptionState> exceptionDac = new(exception.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetLastExceptionState(exceptionCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetLastExceptionState(exceptionDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!exception.IsNullRef)
            exception.Interface = ShimProxy.PairIXCLRDataExceptionState(_session, exceptionCDac.Interface, exceptionDac.Interface);
        return hr;
    }

    #endregion IXCLRDataTask

}
