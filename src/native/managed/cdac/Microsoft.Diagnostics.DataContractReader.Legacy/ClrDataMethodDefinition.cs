// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataMethodDefinition : IXCLRDataMethodDefinition
{
    private readonly Target _target;
    private readonly TargetPointer _module;
    private readonly uint _token;
    private readonly IXCLRDataMethodDefinition? _legacyImpl;
    public ClrDataMethodDefinition(
        Target target,
        TargetPointer module,
        uint token,
        IXCLRDataMethodDefinition? legacyImpl)
    {
        _target = target;
        _module = module;
        _token = token;
        _legacyImpl = legacyImpl;
    }
    int IXCLRDataMethodDefinition.GetTypeDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
        => _legacyImpl is not null ? _legacyImpl.GetTypeDefinition(typeDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
        => _legacyImpl is not null ? _legacyImpl.StartEnumInstances(appDomain, handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> instance)
        => _legacyImpl is not null ? _legacyImpl.EnumInstance(handle, instance) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EndEnumInstances(ulong handle)
        => _legacyImpl is not null ? _legacyImpl.EndEnumInstances(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* name)
        => _legacyImpl is not null ? _legacyImpl.GetName(flags, bufLen, nameLen, name) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        int hr = HResults.S_OK;
        try
        {
            if (token is not null)
            {
                *token = _token;
            }
            if (!mod.IsNullRef)
            {
                IXCLRDataModule? legacyMod = null;
                if (_legacyImpl is not null)
                {
                    DacComNullableByRef<IXCLRDataModule> legacyModOut = new(isNullRef: false);
                    int hrLegacy = _legacyImpl.GetTokenAndScope(null, legacyModOut);
                    if (hrLegacy < 0)
                        return hrLegacy;
                    legacyMod = legacyModOut.Interface;
                }

                mod.Interface = new ClrDataModule(_module, _target, legacyMod);
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

        return hr;
    }

    int IXCLRDataMethodDefinition.GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.IsSameObject(IXCLRDataMethodDefinition? method)
        => _legacyImpl is not null ? _legacyImpl.IsSameObject(method) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetLatestEnCVersion(uint* version)
        => _legacyImpl is not null ? _legacyImpl.GetLatestEnCVersion(version) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.StartEnumExtents(ulong* handle)
        => _legacyImpl is not null ? _legacyImpl.StartEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EnumExtent(ulong* handle, ClrDataMethodDefinitionExtent* extent)
        => _legacyImpl is not null ? _legacyImpl.EnumExtent(handle, extent) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EndEnumExtents(ulong handle)
        => _legacyImpl is not null ? _legacyImpl.EndEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetCodeNotification(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetCodeNotification(flags) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.SetCodeNotification(uint flags)
        => _legacyImpl is not null ? _legacyImpl.SetCodeNotification(flags) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetRepresentativeEntryAddress(ClrDataAddress* addr)
        => _legacyImpl is not null ? _legacyImpl.GetRepresentativeEntryAddress(addr) : HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.HasClassOrMethodInstantiation(int* bGeneric)
        => _legacyImpl is not null ? _legacyImpl.HasClassOrMethodInstantiation(bGeneric) : HResults.E_NOTIMPL;
}
