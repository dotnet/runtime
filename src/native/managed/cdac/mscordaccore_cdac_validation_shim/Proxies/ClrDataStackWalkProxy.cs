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
/// Paired cDAC/DAC proxy for IXCLRDataStackWalk.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataStackWalkProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataStackWalk
{
    private readonly IXCLRDataStackWalk? _cdacImpl;
    private readonly IXCLRDataStackWalk? _legacyImpl;

    internal ClrDataStackWalkProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataStackWalk;
        _legacyImpl = dacObject as IXCLRDataStackWalk;
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

        if (iid == typeof(IXCLRDataStackWalk).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataStackWalk
    int IXCLRDataStackWalk.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, [MarshalUsing(CountElementName = "contextBufSize"), Out] byte[] contextBuf)
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

    int IXCLRDataStackWalk.SetContext(uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.SetContext(contextSize, context) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataStackWalk.Next()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Next() : HResults.E_NOTIMPL;

        // Advance the legacy stack walk to keep it in sync with the cDAC walk.
        // GetFrame() pairs the legacy frame with the cDAC frame, and the paired
        // ClrDataFrameProxy delegates GetArgumentByIndex/GetLocalVariableByIndex and
        // Request(DACSTACKPRIV_REQUEST_FRAME_DATA) to it. If we don't advance the legacy
        // walk here, those calls operate on the wrong frame.
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.Next();
#if DEBUG
            Debug.ValidateHResult(hr, hrLocal);
#endif
        }

        return hr;
    }

    int IXCLRDataStackWalk.GetStackSizeSkipped(ulong* stackSizeSkipped)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetStackSizeSkipped(stackSizeSkipped) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetStackSizeSkipped", "ClrDataStackWalk.cs"))
        {
            return _legacyImpl.GetStackSizeSkipped(stackSizeSkipped);
        }
        return hr;
    }

    int IXCLRDataStackWalk.GetFrameType(uint* simpleType, uint* detailedType)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFrameType(simpleType, detailedType) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataStackWalk.GetFrame(DacComNullableByRef<IXCLRDataFrame> frame)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataFrame> frameCDac = new(frame.IsNullRef);
        DacComNullableByRef<IXCLRDataFrame> frameDac = new(frame.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetFrame(frameCDac) : HResults.E_NOTIMPL;
        // The pre-refactor cDAC had no comparison here: it called the legacy DAC only to obtain the
        // legacy frame it embedded in the returned ClrDataFrame. The shim pairs the two frames
        // instead, so the paired ClrDataFrameProxy can compare them on subsequent calls.
        if (_legacyImpl is not null)
        {
            _legacyImpl.GetFrame(frameDac);
        }
        if (!frame.IsNullRef)
            frame.Interface = ShimProxy.PairIXCLRDataFrame(_session, frameCDac.Interface, frameDac.Interface);
        return hr;
    }

    int IXCLRDataStackWalk.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal;
            byte[] localOutBuffer = new byte[outBufferSize];
            fixed (byte* localOutBufferPtr = localOutBuffer)
            {
                hrLocal = _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, localOutBufferPtr);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                for (int i = 0; i < outBufferSize; i++)
                {
                    Debug.Assert(localOutBuffer[i] == outBuffer[i], $"cDAC: {outBuffer[i]:x}, DAC: {localOutBuffer[i]:x}");
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataStackWalk.SetContext2(uint flags, uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.SetContext2(flags, contextSize, context) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion IXCLRDataStackWalk

}
