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
/// Paired cDAC/DAC proxy for IXCLRDataMethodInstance.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataMethodInstanceProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataMethodInstance
{
    private readonly IXCLRDataMethodInstance? _cdacImpl;
    private readonly IXCLRDataMethodInstance? _legacyImpl;

    internal ClrDataMethodInstanceProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataMethodInstance;
        _legacyImpl = dacObject as IXCLRDataMethodInstance;
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

        if (iid == typeof(IXCLRDataMethodInstance).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataMethodInstance
    int IXCLRDataMethodInstance.GetTypeInstance(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetTypeInstance(typeInstance) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodInstance.GetDefinition(DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionCDac = new(methodDefinition.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetDefinition(methodDefinitionCDac) : HResults.E_NOTIMPL;
        // The pre-refactor cDAC delegated GetDefinition to the legacy DAC without comparing; when the
        // cDAC does not implement it, fall back to the legacy DAC and hand back its object.
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetDefinition", "ClrDataMethodInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionDac = new(methodDefinition.IsNullRef);
            hr = _legacyImpl.GetDefinition(methodDefinitionDac);
            if (!methodDefinition.IsNullRef)
                methodDefinition.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, null, methodDefinitionDac.Interface);
            return hr;
        }
        if (!methodDefinition.IsNullRef)
            methodDefinition.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, methodDefinitionCDac.Interface, null);
        return hr;
    }

    int IXCLRDataMethodInstance.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> modDac = new(mod.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetTokenAndScope(token, modCDac) : HResults.E_NOTIMPL;
        uint tokenLocal = 0;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            // Give the legacy DAC a private token buffer so it cannot overwrite the cDAC's
            // authoritative *token output.
            hrLocal = _legacyImpl.GetTokenAndScope(token is null ? null : &tokenLocal, modDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
            if (token is not null)
                Debug.Assert(tokenLocal == *token, $"cDAC: {*token:x}, DAC: {tokenLocal:x}");
        }
#endif
        if (!mod.IsNullRef)
            mod.Interface = ShimProxy.PairIXCLRDataModule(_session, modCDac.Interface, modDac.Interface);
        return hr;
    }

    int IXCLRDataMethodInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal = 0;
            char[] nameBufLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pNameBufLocal = nameBufLocal)
            {
                hrLocal = _legacyImpl.GetName(flags, bufLen, &nameLenLocal, nameBuf is null ? null : pNameBufLocal);
            }

            Debug.ValidateHResult(hr, hrLocal);
            if (nameLen is not null)
                Debug.Assert(nameLenLocal == *nameLen, $"cDAC: {*nameLen:x}, DAC: {nameLenLocal:x}");

            if (nameBuf is not null)
            {
                string dacName = new string(nameBufLocal, 0, (int)nameLenLocal - 1);
                string cdacName = new string(nameBuf);
                Debug.Assert(dacName == cdacName, $"cDAC: {cdacName}, DAC: {dacName}");
            }
        }
#endif
        return hr;
    }

    int IXCLRDataMethodInstance.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodInstance.IsSameObject(IXCLRDataMethodInstance* method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameObject(method) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodInstance.GetEnCVersion(uint* version)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetEnCVersion(version) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodInstance.GetNumTypeArguments(uint* numTypeArgs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodInstance.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodInstance.GetILOffsetsByAddress(ClrDataAddress address, uint offsetsLen, uint* offsetsNeeded, uint* ilOffsets)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetILOffsetsByAddress(address, offsetsLen, offsetsNeeded, ilOffsets) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal;

            bool validateOffsetsNeeded = offsetsNeeded is not null;
            uint localOffsetsNeeded = 0;

            bool validateIlOffsets = ilOffsets is not null;
            uint[] localIlOffsets = new uint[offsetsLen];

            fixed (uint* localIlOffsetsPtr = localIlOffsets)
            {
                hrLocal = _legacyImpl.GetILOffsetsByAddress(
                    address,
                    offsetsLen,
                    validateOffsetsNeeded ? &localOffsetsNeeded : null,
                    validateIlOffsets ? localIlOffsetsPtr : null);
            }

            // AllowCdacSuccess: the DAC fails on interpreted code.
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowCdacSuccess);

            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                if (validateOffsetsNeeded)
                {
                    Debug.Assert(localOffsetsNeeded == *offsetsNeeded, $"cDAC: {*offsetsNeeded:x}, DAC: {localOffsetsNeeded:x}");
                }

                if (validateIlOffsets)
                {
                    for (int i = 0; i < localIlOffsets.Length; i++)
                    {
                        Debug.Assert(localIlOffsets[i] == ilOffsets[i], $"cDAC: {localIlOffsets[i]:x}, DAC: {ilOffsets[i]:x}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataMethodInstance.GetAddressRangesByILOffset(uint ilOffset, uint rangesLen, uint* rangesNeeded, void* addressRanges)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAddressRangesByILOffset(ilOffset, rangesLen, rangesNeeded, addressRanges) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataMethodInstance.GetILAddressMap(uint mapLen, uint* mapNeeded, [In, Out, MarshalUsing(CountElementName = "mapLen")] ClrDataILAddressMap[]? maps)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetILAddressMap(mapLen, mapNeeded, maps) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint mapNeededLocal;
            ClrDataILAddressMap[]? mapsLocal = mapLen > 0 ? new ClrDataILAddressMap[mapLen] : null;
            int hrLocal = _legacyImpl.GetILAddressMap(mapLen, &mapNeededLocal, mapsLocal);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK)
            {
                Debug.Assert(mapNeeded == null || *mapNeeded == mapNeededLocal);
                if (mapsLocal is not null)
                {
                    int countToCheck = Math.Min(mapsLocal.Length, (int)mapNeededLocal);
                    for (int i = 0; i < countToCheck; i++)
                    {
                        Debug.Assert(mapsLocal[i].ilOffset == maps![i].ilOffset, $"ILOffset - cDAC: {maps[i].ilOffset:x}, DAC: {mapsLocal[i].ilOffset:x}");
                        Debug.Assert(mapsLocal[i].startAddress == maps[i].startAddress, $"StartAddress - cDAC: {maps[i].startAddress:x}, DAC: {mapsLocal[i].startAddress:x}");
                        Debug.Assert(mapsLocal[i].endAddress == maps[i].endAddress, $"EndAddress - cDAC: {maps[i].endAddress:x}, DAC: {mapsLocal[i].endAddress:x}");
                        Debug.Assert(mapsLocal[i].type == maps[i].type, $"Type - cDAC: {maps[i].type:x}, DAC: {mapsLocal[i].type:x}");
                    }
                }
            }
        }

#endif
        return hr;
    }

    int IXCLRDataMethodInstance.StartEnumExtents(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumExtents(handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.StartEnumExtents(handle is null ? null : &dacHandle);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, dacHandle, (calledDac) && hrLocal >= 0);
        else if (calledDac && hrLocal >= 0 && _legacyImpl is not null)
        {
            // The cDAC did not start an enumeration but the legacy DAC did; end the orphaned
            // legacy enumeration so it does not leak (matches the pre-refactor cDAC).
            _legacyImpl.EndEnumExtents(dacHandle);
        }
        return hr;
    }

    int IXCLRDataMethodInstance.EnumExtent(ulong* handle, ClrDataAddressRange* extent)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        int hr = _cdacImpl is not null ? _cdacImpl.EnumExtent(handle is null ? null : &cdacHandle, extent) : HResults.E_NOTIMPL;
        ClrDataAddressRange extentLocal = default;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyImpl is not null)
        {
            // Give the legacy DAC a private extent buffer so it cannot overwrite the cDAC's
            // authoritative *extent output.
            hrLocal = _legacyImpl.EnumExtent(handle is null ? null : &dacHandle, &extentLocal);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(extent->startAddress == extentLocal.startAddress, $"StartAddress - cDAC: {extent->startAddress:x}, DAC: {extentLocal.startAddress:x}");
                Debug.Assert(extent->endAddress == extentLocal.endAddress, $"EndAddress - cDAC: {extent->endAddress:x}, DAC: {extentLocal.endAddress:x}");
            }
        }
#endif
        if (pair is not null)
        {
            pair.CDacHandle = cdacHandle;
            if (calledDac)
                pair.DacHandle = dacHandle;
        }
        return hr;
    }

    int IXCLRDataMethodInstance.EndEnumExtents(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumExtents(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyImpl is not null)
        {
            hrLocal = _legacyImpl.EndEnumExtents(pair is null ? handle : pair.DacHandle);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataMethodInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
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

    int IXCLRDataMethodInstance.GetRepresentativeEntryAddress(ClrDataAddress* addr)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetRepresentativeEntryAddress(addr) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress addrLocal;
            int hrLocal = _legacyImpl.GetRepresentativeEntryAddress(&addrLocal);

            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(addrLocal == *addr, $"cDAC: {*addr:x}, DAC: {addrLocal:x}");
        }
#endif
        return hr;
    }

    #endregion IXCLRDataMethodInstance

}
