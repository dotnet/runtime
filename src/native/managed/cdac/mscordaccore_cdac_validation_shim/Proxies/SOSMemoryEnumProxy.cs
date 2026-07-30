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
/// Paired cDAC/DAC proxy for ISOSMemoryEnum.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class SOSMemoryEnumProxy
    : ShimProxy, ICustomQueryInterface, ISOSMemoryEnum
{
    private readonly ISOSEnum? _cdacEnum;
    private readonly ISOSEnum? _legacyEnum;
    private readonly ISOSMemoryEnum? _cdacMemoryEnum;
    private readonly ISOSMemoryEnum? _legacyMemoryEnum;

    internal SOSMemoryEnumProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacEnum = cdacObject as ISOSEnum;
        _legacyEnum = dacObject as ISOSEnum;
        _cdacMemoryEnum = cdacObject as ISOSMemoryEnum;
        _legacyMemoryEnum = dacObject as ISOSMemoryEnum;
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
        if (iid == typeof(ISOSMemoryEnum).GUID)
            return Support(_cdacMemoryEnum, _legacyMemoryEnum);

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
        _legacyMemoryEnum?.Skip(count);
#endif
        return hr;
    }

    int ISOSEnum.Reset()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.Reset() : HResults.E_NOTIMPL;
#if DEBUG
        _legacyMemoryEnum?.Reset();
#endif
        return hr;
    }

    int ISOSEnum.GetCount(uint* pCount)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.GetCount(pCount) : HResults.E_NOTIMPL;
#if DEBUG
        if (hr == HResults.S_OK && _legacyMemoryEnum is not null)
        {
            uint countLocal;
            _legacyMemoryEnum.GetCount(&countLocal);
            Debug.Assert(countLocal == *pCount);
        }
#endif
        return hr;
    }

    #endregion ISOSEnum

    #region ISOSMemoryEnum
    int ISOSMemoryEnum.Next(uint count, SOSMemoryRegion[] memRegions, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacMemoryEnum is not null ? _cdacMemoryEnum.Next(count, memRegions, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
            if (_legacyMemoryEnum is not null)
            {
                SOSMemoryRegion[] regionsLocal = new SOSMemoryRegion[count];
                uint neededLocal;
                int hrLocal = _legacyMemoryEnum.Next(count, regionsLocal, &neededLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK || hr == HResults.S_FALSE)
                {
                    Debug.Assert(*pNeeded == neededLocal, $"cDAC: {*pNeeded}, DAC: {neededLocal}");
                    for (int i = 0; i < neededLocal; i++)
                    {
                        Debug.Assert(memRegions[i].Start == regionsLocal[i].Start, $"cDAC: {memRegions[i].Start:x}, DAC: {regionsLocal[i].Start:x}");
                        Debug.Assert(memRegions[i].Size == regionsLocal[i].Size, $"cDAC: {memRegions[i].Size:x}, DAC: {regionsLocal[i].Size:x}");
                        Debug.Assert(memRegions[i].ExtraData == regionsLocal[i].ExtraData, $"cDAC: {memRegions[i].ExtraData:x}, DAC: {regionsLocal[i].ExtraData:x}");
                        Debug.Assert(memRegions[i].Heap == regionsLocal[i].Heap, $"cDAC: {memRegions[i].Heap}, DAC: {regionsLocal[i].Heap}");
                    }
                }
            }
#endif
        return hr;
    }

    #endregion ISOSMemoryEnum

}
