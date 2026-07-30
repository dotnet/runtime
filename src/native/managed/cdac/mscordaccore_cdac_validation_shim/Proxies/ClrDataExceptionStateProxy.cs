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
/// Paired cDAC/DAC proxy for IXCLRDataExceptionState.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataExceptionStateProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataExceptionState
{
    private readonly IXCLRDataExceptionState? _cdacImpl;
    private readonly IXCLRDataExceptionState? _legacyImpl;

    internal ClrDataExceptionStateProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataExceptionState;
        _legacyImpl = dacObject as IXCLRDataExceptionState;
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

        if (iid == typeof(IXCLRDataExceptionState).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataExceptionState
    int IXCLRDataExceptionState.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
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
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataExceptionState> exStateCDac = new(exState.IsNullRef);
        DacComNullableByRef<IXCLRDataExceptionState> exStateDac = new(exState.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetPrevious(exStateCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetPrevious(exStateDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!exState.IsNullRef)
            exState.Interface = ShimProxy.PairIXCLRDataExceptionState(_session, exStateCDac.Interface, exStateDac.Interface);
        return hr;
    }

    int IXCLRDataExceptionState.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetManagedObject(valueCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetManagedObject", "ClrDataExceptionState.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
            hr = _legacyImpl.GetManagedObject(valueDac);
            if (!value.IsNullRef)
                value.Interface = ShimProxy.PairIXCLRDataValue(_session, null, valueDac.Interface);
            return hr;
        }
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, null);
        return hr;
    }

    int IXCLRDataExceptionState.GetBaseType(/*CLRDataBaseExceptionType*/ uint* type)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetBaseType(type) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataExceptionState.GetCode(uint* code)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCode(code) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataExceptionState.GetString(uint bufLen, uint* strLen, char* str)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetString(bufLen, strLen, str) : HResults.E_NOTIMPL;
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
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
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
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameState(exRecord, contextSize, cxRecord) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.IsSameState(exRecord, contextSize, cxRecord);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataExceptionState.IsSameState2(uint flags, EXCEPTION_RECORD64* exRecord, uint contextSize, byte* cxRecord)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameState2(flags, exRecord, contextSize, cxRecord) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.IsSameState2(flags, exRecord, contextSize, cxRecord);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataExceptionState.GetTask(DacComNullableByRef<IXCLRDataTask> task)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTask> taskCDac = new(task.IsNullRef);
        DacComNullableByRef<IXCLRDataTask> taskDac = new(task.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetTask(taskCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetTask(taskDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!task.IsNullRef)
            task.Interface = ShimProxy.PairIXCLRDataTask(_session, taskCDac.Interface, taskDac.Interface);
        return hr;
    }

    #endregion IXCLRDataExceptionState

}
