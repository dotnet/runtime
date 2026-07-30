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
/// Paired cDAC/DAC proxy for IXCLRDataFrame.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataFrameProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataFrame, IXCLRDataFrame2
{
    private readonly IXCLRDataFrame? _cdacImpl;
    private readonly IXCLRDataFrame? _legacyImpl;
    private readonly IXCLRDataFrame2? _cdacImpl2;
    private readonly IXCLRDataFrame2? _legacyImpl2;

    internal ClrDataFrameProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataFrame;
        _legacyImpl = dacObject as IXCLRDataFrame;
        _cdacImpl2 = cdacObject as IXCLRDataFrame2;
        _legacyImpl2 = dacObject as IXCLRDataFrame2;
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

        if (iid == typeof(IXCLRDataFrame).GUID)
            return Support(_cdacImpl, _legacyImpl);
        if (iid == typeof(IXCLRDataFrame2).GUID)
            return Support(_cdacImpl2, _legacyImpl2);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataFrame
    int IXCLRDataFrame.GetFrameType(uint* simpleType, uint* detailedType)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFrameType(simpleType, detailedType) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataFrame.GetContext(uint contextFlags,
        uint contextBufSize,
        uint* contextSize,
        [Out, MarshalUsing(CountElementName = nameof(contextBufSize))] byte[] contextBuf)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetContext(contextFlags, contextBufSize, contextSize, contextBuf) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            byte[] localContextBuf = new byte[contextBufSize];
            uint localContextSize = 0;
            int hrLocal = _legacyImpl.GetContext(contextFlags, contextBufSize, &localContextSize, localContextBuf);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK)
            {
                // The pre-refactor cDAC compared the two contexts through IPlatformAgnosticContext,
                // which requires a cDAC Target the shim does not have. The context buffers use a
                // fixed, packed layout, so comparing the meaningful bytes (the length the DAC
                // reported) is equivalent to the structural comparison.
                uint compareLen = localContextSize <= contextBufSize ? localContextSize : contextBufSize;
                for (uint i = 0; i < compareLen; i++)
                {
                    Debug.Assert(contextBuf[i] == localContextBuf[i], $"context byte {i} - cDAC: {contextBuf[i]:x}, DAC: {localContextBuf[i]:x}");
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataAppDomain> appDomainCDac = new(appDomain.IsNullRef);
        DacComNullableByRef<IXCLRDataAppDomain> appDomainDac = new(appDomain.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetAppDomain(appDomainCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetAppDomain(appDomainDac);
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

    int IXCLRDataFrame.GetNumArguments(uint* numArgs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumArguments(numArgs) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numArgsLocal;
            int hrLocal = _legacyImpl.GetNumArguments(&numArgsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*numArgs == numArgsLocal, $"cDAC: {*numArgs}, DAC: {numArgsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetArgumentByIndex(uint index,
        DacComNullableByRef<IXCLRDataValue> arg,
        uint bufLen,
        uint* nameLen,
        char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> argCDac = new(arg.IsNullRef);
        // The pre-refactor cDAC always requested the legacy value (isNullRef: false) and passed
        // null for the name-length/name buffers so the legacy DAC would not overwrite the cDAC's
        // output; the legacy value is embedded in the paired child instead.
        DacComNullableByRef<IXCLRDataValue> argDac = new(isNullRef: false);
        int hr = _cdacImpl is not null ? _cdacImpl.GetArgumentByIndex(index, argCDac, bufLen, nameLen, name) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetArgumentByIndex(index, argDac, bufLen, null, null);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            // See AllowCdacSuccess in DebugExtensions.cs — the native DAC's MetaSig
            // constructor can fail on certain frames (e.g., EH dispatch) where the cDAC
            // succeeds via contract-based metadata access.
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowCdacSuccess);
        }
#endif
        if (!arg.IsNullRef)
            arg.Interface = ShimProxy.PairIXCLRDataValue(_session, argCDac.Interface, argDac.Interface);
        return hr;
    }

    int IXCLRDataFrame.GetNumLocalVariables(uint* numLocals)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumLocalVariables(numLocals) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numLocalsLocal;
            int hrLocal = _legacyImpl.GetNumLocalVariables(&numLocalsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*numLocals == numLocalsLocal, $"cDAC: {*numLocals}, DAC: {numLocalsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetLocalVariableByIndex(uint index,
        DacComNullableByRef<IXCLRDataValue> localVariable,
        uint bufLen,
        uint* nameLen,
        char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> localVariableCDac = new(localVariable.IsNullRef);
        // See GetArgumentByIndex: request the legacy value with null name buffers so it does not
        // overwrite the cDAC's output.
        DacComNullableByRef<IXCLRDataValue> localVariableDac = new(isNullRef: false);
        int hr = _cdacImpl is not null ? _cdacImpl.GetLocalVariableByIndex(index, localVariableCDac, bufLen, nameLen, name) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetLocalVariableByIndex(index, localVariableDac, bufLen, null, null);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            // See comment in GetArgumentByIndex.
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowCdacSuccess);
        }
#endif
        if (!localVariable.IsNullRef)
            localVariable.Interface = ShimProxy.PairIXCLRDataValue(_session, localVariableCDac.Interface, localVariableDac.Interface);
        return hr;
    }

    int IXCLRDataFrame.GetCodeName(uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCodeName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetCodeName", "ClrDataFrame.cs"))
        {
            return _legacyImpl.GetCodeName(flags, bufLen, nameLen, nameBuf);
        }
        return hr;
    }

    int IXCLRDataFrame.GetMethodInstance(DacComNullableByRef<IXCLRDataMethodInstance> method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodInstance> methodCDac = new(method.IsNullRef);
        DacComNullableByRef<IXCLRDataMethodInstance> methodDac = new(method.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodInstance(methodCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetMethodInstance(methodDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!method.IsNullRef)
            method.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, methodCDac.Interface, methodDac.Interface);
        return hr;
    }

    int IXCLRDataFrame.Request(uint reqCode,
        uint inBufferSize,
        byte* inBuffer,
        uint outBufferSize,
        byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("Request", "ClrDataFrame.cs"))
        {
            return _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);
        }
        return hr;
    }

    int IXCLRDataFrame.GetNumTypeArguments(uint* numTypeArgs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataFrame.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion IXCLRDataFrame

    #region IXCLRDataFrame2
    int IXCLRDataFrame2.GetExactGenericArgsToken(DacComNullableByRef<IXCLRDataValue> genericToken)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl2 is not null ? _cdacImpl2.GetExactGenericArgsToken(genericToken) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion IXCLRDataFrame2

}
