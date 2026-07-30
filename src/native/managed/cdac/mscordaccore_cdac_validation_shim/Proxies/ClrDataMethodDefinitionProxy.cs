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
/// Paired cDAC/DAC proxy for IXCLRDataMethodDefinition.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataMethodDefinitionProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataMethodDefinition
{
    private readonly IXCLRDataMethodDefinition? _cdacImpl;
    private readonly IXCLRDataMethodDefinition? _legacyImpl;

    internal ClrDataMethodDefinitionProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataMethodDefinition;
        _legacyImpl = dacObject as IXCLRDataMethodDefinition;
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

        if (iid == typeof(IXCLRDataMethodDefinition).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataMethodDefinition

    int IXCLRDataMethodDefinition.GetTypeDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetTypeDefinition(typeDefinition) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumInstances(ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.StartEnumInstances(ShimProxy.UnwrapDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &dacHandle);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (handle is not null && hr == HResults.S_OK)
            *handle = _session.RegisterHandle(cdacHandle, dacHandle, (calledDac) && hrLocal >= 0);
        else if (calledDac && hrLocal >= 0 && _legacyImpl is not null)
        {
            _legacyImpl.EndEnumInstances(dacHandle);
        }
        return hr;
    }

    int IXCLRDataMethodDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> instance)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataMethodInstance> instanceCDac = new(instance.IsNullRef);
        DacComNullableByRef<IXCLRDataMethodInstance> instanceDac = new(instance.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumInstance(handle is null ? null : &cdacHandle, instanceCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyImpl is not null)
        {
            hrLocal = _legacyImpl.EnumInstance(handle is null ? null : &dacHandle, instanceDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (pair is not null)
        {
            pair.CDacHandle = cdacHandle;
            if (calledDac)
                pair.DacHandle = dacHandle;
        }
        if (!instance.IsNullRef)
            instance.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, instanceCDac.Interface, instanceDac.Interface);
        return hr;
    }

    int IXCLRDataMethodDefinition.EndEnumInstances(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumInstances(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if ((pair is null || pair.HasDacHandle) && _legacyImpl is not null)
        {
            _legacyImpl.EndEnumInstances(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataMethodDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetName(flags, bufLen, nameLen, name) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal = 0;
            char[] nameBufLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pNameBufLocal = nameBufLocal)
            {
                hrLocal = _legacyImpl.GetName(flags, bufLen, &nameLenLocal, name is null ? null : pNameBufLocal);
            }

            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0 && hrLocal >= 0)
            {
                if (nameLen is not null)
                    Debug.Assert(nameLenLocal == *nameLen, $"cDAC: {*nameLen:x}, DAC: {nameLenLocal:x}");

                if (name is not null && nameLenLocal > 0)
                {
                    string dacName = new string(nameBufLocal, 0, (int)nameLenLocal - 1);
                    string cdacName = new string(name);
                    Debug.Assert(dacName == cdacName, $"cDAC: {cdacName}, DAC: {dacName}");
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataMethodDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetTokenAndScope(token, modCDac) : HResults.E_NOTIMPL;
        IXCLRDataModule? legacyMod = null;
        if (!mod.IsNullRef && _legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataModule> legacyModOut = new(isNullRef: false);
            int hrLegacy = _legacyImpl.GetTokenAndScope(null, legacyModOut);
            if (hrLegacy >= 0)
                legacyMod = legacyModOut.Interface;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            bool validateToken = token is not null;
            bool validateMod = !mod.IsNullRef;

            uint tokenLocal = 0;
            DacComNullableByRef<IXCLRDataModule> legacyModOutLocal = new(isNullRef: !validateMod);
            int hrLocal = _legacyImpl.GetTokenAndScope(validateToken ? &tokenLocal : null, legacyModOutLocal);

            Debug.ValidateHResult(hr, hrLocal);

            if (validateToken)
            {
                Debug.Assert(tokenLocal == *token, $"cDAC: {*token:x}, DAC: {tokenLocal:x}");
            }
        }
#endif
        if (!mod.IsNullRef)
            mod.Interface = ShimProxy.PairIXCLRDataModule(_session, modCDac.Interface, legacyMod);
        return hr;
    }

    int IXCLRDataMethodDefinition.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodDefinition.IsSameObject(IXCLRDataMethodDefinition? method)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameObject(ShimProxy.UnwrapCDac<IXCLRDataMethodDefinition>(method)) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodDefinition.GetLatestEnCVersion(uint* version)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetLatestEnCVersion(version) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodDefinition.StartEnumExtents(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumExtents(handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumExtents", "ClrDataMethodDefinition.cs"))
        {
            ulong dacHandle = 0;
            hr = _legacyImpl.StartEnumExtents(handle is null ? null : &dacHandle);
            if (handle is not null && hr >= 0)
                *handle = _session.RegisterHandle(0, dacHandle, hasDacHandle: true);
            return hr;
        }
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, 0, hasDacHandle: false);
        return hr;
    }

    int IXCLRDataMethodDefinition.EnumExtent(ulong* handle, ClrDataMethodDefinitionExtent* extent)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        int hr = _cdacImpl is not null ? _cdacImpl.EnumExtent(handle is null ? null : &cdacHandle, extent) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumExtent", "ClrDataMethodDefinition.cs"))
        {
            hr = _legacyImpl.EnumExtent(handle is null ? null : &dacHandle, extent);
            if (pair is not null)
            {
                pair.DacHandle = dacHandle;
                pair.HasDacHandle = hr >= 0;
            }
            return hr;
        }
        if (pair is not null)
            pair.CDacHandle = cdacHandle;
        return hr;
    }

    int IXCLRDataMethodDefinition.EndEnumExtents(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumExtents(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumExtents", "ClrDataMethodDefinition.cs"))
        {
            return _legacyImpl.EndEnumExtents(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataMethodDefinition.GetCodeNotification(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCodeNotification(flags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodDefinition.SetCodeNotification(uint flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.SetCodeNotification(flags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
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

    int IXCLRDataMethodDefinition.GetRepresentativeEntryAddress(ClrDataAddress* addr)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetRepresentativeEntryAddress(addr) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetRepresentativeEntryAddress", "ClrDataMethodDefinition.cs"))
        {
            return _legacyImpl.GetRepresentativeEntryAddress(addr);
        }
        return hr;
    }

    int IXCLRDataMethodDefinition.HasClassOrMethodInstantiation(int* bGeneric)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.HasClassOrMethodInstantiation(bGeneric) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int bGenericLocal = 0;
            int hrLocal = _legacyImpl.HasClassOrMethodInstantiation(&bGenericLocal);

            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0 && hrLocal >= 0 && bGeneric is not null)
            {
                Debug.Assert(bGenericLocal == *bGeneric, $"cDAC: {*bGeneric}, DAC: {bGenericLocal}");
            }
        }
#endif
        return hr;
    }

    #endregion IXCLRDataMethodDefinition

}
