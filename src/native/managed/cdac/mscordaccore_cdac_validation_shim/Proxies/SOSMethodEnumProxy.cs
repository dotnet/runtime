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
/// Paired cDAC/DAC proxy for ISOSMethodEnum.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class SOSMethodEnumProxy
    : ShimProxy, ICustomQueryInterface, ISOSMethodEnum
{
    private readonly ISOSEnum? _cdacEnum;
    private readonly ISOSEnum? _legacyEnum;
    private readonly ISOSMethodEnum? _cdacMethodEnum;
    private readonly ISOSMethodEnum? _legacyMethodEnum;

    internal SOSMethodEnumProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacEnum = cdacObject as ISOSEnum;
        _legacyEnum = dacObject as ISOSEnum;
        _cdacMethodEnum = cdacObject as ISOSMethodEnum;
        _legacyMethodEnum = dacObject as ISOSMethodEnum;
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
        if (iid == typeof(ISOSMethodEnum).GUID)
            return Support(_cdacMethodEnum, _legacyMethodEnum);

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
        _legacyMethodEnum?.Skip(count);
#endif
        return hr;
    }

    int ISOSEnum.Reset()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.Reset() : HResults.E_NOTIMPL;
#if DEBUG
        _legacyMethodEnum?.Reset();
#endif
        return hr;
    }

    int ISOSEnum.GetCount(uint* pCount)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.GetCount(pCount) : HResults.E_NOTIMPL;
#if DEBUG
        if (hr == HResults.S_OK && _legacyMethodEnum is not null)
        {
            uint countLocal;
            _legacyMethodEnum.GetCount(&countLocal);
            Debug.Assert(countLocal == *pCount);
        }
#endif
        return hr;
    }

    #endregion ISOSEnum

    #region ISOSMethodEnum
    int ISOSMethodEnum.Next(uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] SOSMethodData[] values, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacMethodEnum is not null ? _cdacMethodEnum.Next(count, values, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
            if (_legacyMethodEnum is not null)
            {
                SOSMethodData[] valuesLocal = new SOSMethodData[count];
                uint neededLocal;
                int hrLocal = _legacyMethodEnum.Next(count, valuesLocal, &neededLocal);

                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK || hr == HResults.S_FALSE)
                {
                    Debug.Assert(*pNeeded == neededLocal, $"cDAC: {*pNeeded}, DAC: {neededLocal}");
                    for (uint i = 0; i < *pNeeded; i++)
                    {
                        Debug.Assert(values[i].MethodDesc == valuesLocal[i].MethodDesc, $"cDAC: {values[i].MethodDesc:x}, DAC: {valuesLocal[i].MethodDesc:x}");
                        Debug.Assert(values[i].DefiningMethodTable == valuesLocal[i].DefiningMethodTable, $"cDAC: {values[i].DefiningMethodTable:x}, DAC: {valuesLocal[i].DefiningMethodTable:x}");
                        Debug.Assert(values[i].DefiningModule == valuesLocal[i].DefiningModule, $"cDAC: {values[i].DefiningModule:x}, DAC: {valuesLocal[i].DefiningModule:x}");
                        Debug.Assert(values[i].Token == valuesLocal[i].Token, $"cDAC: {values[i].Token}, DAC: {valuesLocal[i].Token}");
                        Debug.Assert(values[i].Entrypoint == valuesLocal[i].Entrypoint, $"cDAC: {values[i].Entrypoint:x}, DAC: {valuesLocal[i].Entrypoint:x}");
                        Debug.Assert(values[i].Slot == valuesLocal[i].Slot, $"cDAC: {values[i].Slot}, DAC: {valuesLocal[i].Slot}");
                    }
                }
            }
#endif
        return hr;
    }

    #endregion ISOSMethodEnum

}
