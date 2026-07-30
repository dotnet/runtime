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
/// Paired cDAC/DAC proxy for ISOSStressLogMsgEnum.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class SOSStressLogMsgEnumProxy
    : ShimProxy, ICustomQueryInterface, ISOSStressLogMsgEnum
{
    private readonly ISOSEnum? _cdacEnum;
    private readonly ISOSEnum? _legacyEnum;
    private readonly ISOSStressLogMsgEnum? _cdacStressLogMsgEnum;
    private readonly ISOSStressLogMsgEnum? _legacyStressLogMsgEnum;

    internal SOSStressLogMsgEnumProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacEnum = cdacObject as ISOSEnum;
        _legacyEnum = dacObject as ISOSEnum;
        _cdacStressLogMsgEnum = cdacObject as ISOSStressLogMsgEnum;
        _legacyStressLogMsgEnum = dacObject as ISOSStressLogMsgEnum;
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
        if (iid == typeof(ISOSStressLogMsgEnum).GUID)
            return Support(_cdacStressLogMsgEnum, _legacyStressLogMsgEnum);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region ISOSEnum
    int ISOSEnum.Skip(uint count)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.Skip(count) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSEnum.Reset()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.Reset() : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSEnum.GetCount(uint* pCount)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnum is not null ? _cdacEnum.GetCount(pCount) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion ISOSEnum

    #region ISOSStressLogMsgEnum
    int ISOSStressLogMsgEnum.Next(uint count, SOSStressMsgData[] values, uint* pFetched)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacStressLogMsgEnum is not null ? _cdacStressLogMsgEnum.Next(count, values, pFetched) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSStressLogMsgEnum.GetArguments(uint messageIndex, uint argCount, ClrDataAddress[] args, uint* pFetched)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacStressLogMsgEnum is not null ? _cdacStressLogMsgEnum.GetArguments(messageIndex, argCount, args, pFetched) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion ISOSStressLogMsgEnum

}
