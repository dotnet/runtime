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
/// Paired cDAC/DAC proxy for IXCLRDataAppDomain.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataAppDomainProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataAppDomain
{
    private readonly IXCLRDataAppDomain? _cdacImpl;
    private readonly IXCLRDataAppDomain? _legacyImpl;

    internal ClrDataAppDomainProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataAppDomain;
        _legacyImpl = dacObject as IXCLRDataAppDomain;
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

        if (iid == typeof(IXCLRDataAppDomain).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataAppDomain
    int IXCLRDataAppDomain.GetProcess(DacComNullableByRef<IXCLRDataProcess> process)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetProcess(process) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAppDomain.GetName(uint bufLen, uint* nameLen, char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetName(bufLen, nameLen, name) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal;
            char[] legacyNameBuf = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pLegacyName = legacyNameBuf)
            {
                hrLocal = _legacyImpl.GetName(bufLen, &nameLenLocal, name is not null ? pLegacyName : null);
            }

            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
            {
                if (nameLen is not null)
                    Debug.Assert(*nameLen == nameLenLocal, $"cDAC: {*nameLen}, DAC: {nameLenLocal}");

                if (name is not null && bufLen > 0)
                {
                    // On truncation (S_FALSE), nameLenLocal is the full required length
                    // which may exceed bufLen. Cap to the actual buffer size.
                    int compareLen = (int)Math.Min(nameLenLocal, bufLen) - 1;
                    if (compareLen > 0)
                    {
                        string dacName = new string(legacyNameBuf, 0, compareLen);
                        string cdacName = new string(name, 0, compareLen);
                        Debug.Assert(dacName == cdacName, $"cDAC: {cdacName}, DAC: {dacName}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataAppDomain.GetUniqueID(ulong* id)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetUniqueID(id) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null && hr >= 0)
        {
            ulong idLocal;
            int hrLocal = _legacyImpl.GetUniqueID(&idLocal);
            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(*id == idLocal, $"cDAC: {*id}, DAC: {idLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataAppDomain.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null && hr >= 0)
        {
            uint flagsLocal;
            int hrLocal = _legacyImpl.GetFlags(&flagsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(*flags == flagsLocal, $"cDAC: {*flags}, DAC: {flagsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataAppDomain.IsSameObject(IXCLRDataAppDomain* appDomain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameObject(appDomain) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.IsSameObject(appDomain);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr}, DAC: {hrLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataAppDomain.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetManagedObject(value) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAppDomain.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion IXCLRDataAppDomain

}
