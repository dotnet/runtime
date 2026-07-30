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
/// Paired cDAC/DAC proxy for IXCLRDataAssembly.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataAssemblyProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataAssembly
{
    private readonly IXCLRDataAssembly? _cdacImpl;
    private readonly IXCLRDataAssembly? _legacyImpl;

    internal ClrDataAssemblyProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataAssembly;
        _legacyImpl = dacObject as IXCLRDataAssembly;
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

        if (iid == typeof(IXCLRDataAssembly).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataAssembly
    int IXCLRDataAssembly.StartEnumModules(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumModules(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.EnumModule(ulong* handle, DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EnumModule(handle, mod) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.EndEnumModules(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumModules(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.GetName(uint bufLen, uint* nameLen, char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetName(bufLen, nameLen, name) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.GetFileName(uint bufLen, uint* nameLen, char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFileName(bufLen, nameLen, name) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.IsSameObject(IXCLRDataAssembly? assembly)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameObject(ShimProxy.UnwrapCDac<IXCLRDataAssembly>(assembly)) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.StartEnumAppDomains(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumAppDomains(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.EnumAppDomain(ulong* handle, void** appDomain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EnumAppDomain(handle, appDomain) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.EndEnumAppDomains(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumAppDomains(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataAssembly.GetDisplayName(uint bufLen, uint* nameLen, char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetDisplayName(bufLen, nameLen, name) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion IXCLRDataAssembly

}
