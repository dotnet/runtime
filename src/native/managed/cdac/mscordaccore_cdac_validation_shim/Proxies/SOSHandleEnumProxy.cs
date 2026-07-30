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
/// Paired cDAC/DAC proxy for ISOSHandleEnum.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class SOSHandleEnumProxy
    : ShimProxy, ICustomQueryInterface, ISOSHandleEnum
{
    private readonly ISOSEnum? _cdacEnum;
    private readonly ISOSEnum? _legacyEnum;
    private readonly ISOSHandleEnum? _cdacHandleEnum;
    private readonly ISOSHandleEnum? _legacyHandleEnum;

    internal SOSHandleEnumProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacEnum = cdacObject as ISOSEnum;
        _legacyEnum = dacObject as ISOSEnum;
        _cdacHandleEnum = cdacObject as ISOSHandleEnum;
        _legacyHandleEnum = dacObject as ISOSHandleEnum;
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

        if (iid == typeof(ISOSEnum).GUID)
            return Support(_cdacEnum, _legacyEnum);
        if (iid == typeof(ISOSHandleEnum).GUID)
            return Support(_cdacHandleEnum, _legacyHandleEnum);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region ISOSEnum
    int ISOSEnum.Skip(uint count)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.Skip(count) : HResults.E_NOTIMPL;
#if DEBUG
            _legacyHandleEnum?.Skip(count);
#endif
        return hr;
    }

    int ISOSEnum.Reset()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.Reset() : HResults.E_NOTIMPL;
#if DEBUG
            _legacyHandleEnum?.Reset();
#endif
        return hr;
    }

    int ISOSEnum.GetCount(uint* pCount)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.GetCount(pCount) : HResults.E_NOTIMPL;
#if DEBUG
        if (hr == HResults.S_OK && _legacyHandleEnum is not null)
        {
            uint countLocal;
            _legacyHandleEnum.GetCount(&countLocal);
            Debug.Assert(countLocal == *pCount);
        }
#endif
        return hr;
    }

    #endregion ISOSEnum

    #region ISOSHandleEnum
    int ISOSHandleEnum.Next(uint count, SOSHandleData[] handles, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacHandleEnum is not null ? _cdacHandleEnum.Next(count, handles, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
            if (_legacyHandleEnum is not null)
            {
                SOSHandleData[] handlesLocal = new SOSHandleData[count];
                uint neededLocal;
                int hrLocal = _legacyHandleEnum.Next(count, handlesLocal, &neededLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK || hr == HResults.S_FALSE)
                {
                    Debug.Assert(*pNeeded == neededLocal, $"cDAC: {*pNeeded}, DAC: {neededLocal}");
                    for (int i = 0; i < neededLocal; i++)
                    {
                        Debug.Assert(handles[i].AppDomain == handlesLocal[i].AppDomain, $"cDAC: {handles[i].AppDomain:x}, DAC: {handlesLocal[i].AppDomain:x}");
                        Debug.Assert(handles[i].Handle == handlesLocal[i].Handle, $"cDAC: {handles[i].Handle:x}, DAC: {handlesLocal[i].Handle:x}");
                        Debug.Assert(handles[i].Secondary == handlesLocal[i].Secondary, $"cDAC: {handles[i].Secondary:x}, DAC: {handlesLocal[i].Secondary:x}");
                        Debug.Assert(handles[i].Type == handlesLocal[i].Type, $"cDAC: {handles[i].Type}, DAC: {handlesLocal[i].Type}");
                        Debug.Assert(handles[i].StrongReference == handlesLocal[i].StrongReference, $"cDAC: {handles[i].StrongReference}, DAC: {handlesLocal[i].StrongReference}");
                        Debug.Assert(handles[i].RefCount == handlesLocal[i].RefCount, $"cDAC: {handles[i].RefCount}, DAC: {handlesLocal[i].RefCount}");
                        Debug.Assert(handles[i].JupiterRefCount == handlesLocal[i].JupiterRefCount, $"cDAC: {handles[i].JupiterRefCount}, DAC: {handlesLocal[i].JupiterRefCount}");
                        Debug.Assert(handles[i].IsPegged == handlesLocal[i].IsPegged, $"cDAC: {handles[i].IsPegged}, DAC: {handlesLocal[i].IsPegged}");
                    }
                }
            }
#endif
        return hr;
    }

    #endregion ISOSHandleEnum

}
