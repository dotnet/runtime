// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataMethodInstance : IXCLRDataMethodInstance
{
    private readonly Target _target;
    private readonly MethodDescHandle _methodDesc;
    private readonly TargetPointer _appDomain;
    private readonly IXCLRDataMethodInstance? _legacyImpl;
    public ClrDataMethodInstance(
        Target target,
        MethodDescHandle methodDesc,
        TargetPointer appDomain,
        IXCLRDataMethodInstance? legacyImpl)
    {
        _target = target;
        _methodDesc = methodDesc;
        _appDomain = appDomain;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataMethodInstance.GetTypeInstance(void** typeInstance)
        => _legacyImpl is not null ? _legacyImpl.GetTypeInstance(typeInstance) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetDefinition(void** methodDefinition)
        => _legacyImpl is not null ? _legacyImpl.GetDefinition(methodDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetTokenAndScope(uint* token, void** /*IXCLRDataModule*/ mod)
    {
        int hr = HResults.S_OK;
        StrategyBasedComWrappers cw = new();

        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            if (token is not null)
            {
                *token = rts.GetMethodToken(_methodDesc);
            }
            if (mod is not null)
            {
                IXCLRDataModule? legacyMod = null;
                if (_legacyImpl is not null)
                {
                    void* legacyModPtr = null;
                    int hrLegacy = _legacyImpl.GetTokenAndScope(token, &legacyModPtr);
                    if (hrLegacy < 0)
                        return hrLegacy;
                    object obj = cw.GetOrCreateObjectForComInstance((nint)legacyModPtr, CreateObjectFlags.None);
                    if (obj is not IXCLRDataModule)
                    {
                        throw new ArgumentException("Invalid module object", nameof(mod));
                    }
                    legacyMod = obj as IXCLRDataModule;
                }

                TargetPointer mtAddr = rts.GetMethodTable(_methodDesc);
                TypeHandle mainMT = rts.GetTypeHandle(mtAddr);
                TargetPointer module = rts.GetModule(mainMT);
                IXCLRDataModule modImpl = new ClrDataModule(module, _target, legacyMod);
                nint modImplPtr = cw.GetOrCreateComInterfaceForObject(modImpl, CreateComInterfaceFlags.None);
                Marshal.QueryInterface(modImplPtr, typeof(IXCLRDataModule).GUID, out nint ptrToMod);
                Marshal.Release(modImplPtr);
                *mod = (void*)ptrToMod;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            bool validateToken = token is not null;
            bool validateMod = mod is not null;

            uint tokenLocal = 0;
            void* legacyModPtr = null;
            int hrLocal = _legacyImpl.GetTokenAndScope(validateToken ? &tokenLocal : null, validateMod ? &legacyModPtr : null);

            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");

            if (validateToken)
            {
                Debug.Assert(tokenLocal == *token, $"cDAC: {*token:x}, DAC: {tokenLocal:x}");
            }

            if (validateMod && hr == HResults.S_OK)
            {
                Marshal.Release((nint)legacyModPtr); // release the legacy module
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
        => _legacyImpl is not null ? _legacyImpl.GetName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.IsSameObject(IXCLRDataMethodInstance* method)
        => _legacyImpl is not null ? _legacyImpl.IsSameObject(method) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetEnCVersion(uint* version)
        => _legacyImpl is not null ? _legacyImpl.GetEnCVersion(version) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetNumTypeArguments(uint* numTypeArgs)
        => _legacyImpl is not null ? _legacyImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetTypeArgumentByIndex(uint index, void** typeArg)
        => _legacyImpl is not null ? _legacyImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetILOffsetsByAddress(ClrDataAddress address, uint offsetsLen, uint* offsetsNeeded, uint* ilOffsets)
        => _legacyImpl is not null ? _legacyImpl.GetILOffsetsByAddress(address, offsetsLen, offsetsNeeded, ilOffsets) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetAddressRangesByILOffset(uint ilOffset, uint rangesLen, uint* rangesNeeded, void* addressRanges)
        => _legacyImpl is not null ? _legacyImpl.GetAddressRangesByILOffset(ilOffset, rangesLen, rangesNeeded, addressRanges) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetILAddressMap(uint mapLen, uint* mapNeeded, void* maps)
        => _legacyImpl is not null ? _legacyImpl.GetILAddressMap(mapLen, mapNeeded, maps) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.StartEnumExtents(ulong* handle)
        => _legacyImpl is not null ? _legacyImpl.StartEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.EnumExtent(ulong* handle, void* extent)
        => _legacyImpl is not null ? _legacyImpl.EnumExtent(handle, extent) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.EndEnumExtents(ulong handle)
        => _legacyImpl is not null ? _legacyImpl.EndEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetRepresentativeEntryAddress(ClrDataAddress* addr)
    {
        int hr = HResults.S_OK;

        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            TargetCodePointer addrCode = rts.GetNativeCode(_methodDesc);

            if (addrCode.Value != 0)
            {
                *addr = addrCode.Value;
            }
            else
            {
                hr = unchecked((int)0x8000FFFF); // E_UNEXPECTED
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress addrLocal;
            int hrLocal = _legacyImpl.GetRepresentativeEntryAddress(&addrLocal);

            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            Debug.Assert(addrLocal == *addr, $"cDAC: {*addr:x}, DAC: {addrLocal:x}");
        }
#endif

        return hr;
    }
}
