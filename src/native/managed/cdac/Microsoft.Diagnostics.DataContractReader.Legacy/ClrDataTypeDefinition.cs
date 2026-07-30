// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeDefinition : IXCLRDataTypeDefinition
{
    private readonly Target _target;
    private readonly TargetPointer _module;
    private readonly uint _token;
    private readonly ITypeHandle? _typeHandle;
    private readonly IXCLRDataTypeDefinition? _legacyImpl;

    public ClrDataTypeDefinition(
        Target target,
        TargetPointer module,
        uint token,
        ITypeHandle? typeHandle,
        IXCLRDataTypeDefinition? legacyImpl)
    {
        _target = target;
        _module = module;
        _token = token;
        _typeHandle = typeHandle;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataTypeDefinition.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetModule(mod) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumMethodDefinitions(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumMethodDefinitions(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumMethodDefinition(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumMethodDefinition(handle, methodDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumMethodDefinitions(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumMethodDefinitions(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumMethodDefinitionsByName(name, flags, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumMethodDefinitionByName(handle, method) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumMethodDefinitionsByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumMethodDefinitionsByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetMethodDefinitionByToken(uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetMethodDefinitionByToken(token, methodDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumInstances(appDomain, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> instance)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumInstance(handle, instance) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumInstances(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumInstances(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        int hr = HResults.S_OK;
        try
        {
            if (flags != 0)
                throw new ArgumentException();

            string name;
            if (_typeHandle is null)
            {
                Contracts.ModuleHandle module = _target.Contracts.Loader.GetModuleHandleFromModulePtr(_module);
                MetadataReader reader = _target.Contracts.EcmaMetadata.GetMetadata(module) ?? throw new NotImplementedException();
                TypeDefinitionHandle typeDefinitionHandle = MetadataTokens.TypeDefinitionHandle((int)EcmaMetadataUtils.GetRowId(_token));
                TypeDefinition typeDefinition = reader.GetTypeDefinition(typeDefinitionHandle);
                string typeName = reader.GetString(typeDefinition.Name);
                string typeNamespace = reader.GetString(typeDefinition.Namespace);
                name = string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";
            }
            else
            {
                name = _typeHandle.GetName(_target);
            }

            OutputBufferHelpers.CopyStringToBuffer(nameBuf, bufLen, nameLen, name, out bool truncated);
            if (nameBuf is not null && truncated)
                throw Marshal.GetExceptionForHR(CorDbgHResults.ERROR_INSUFFICIENT_BUFFER)!;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal = 0;
            char[] nameBufLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pNameBufLocal = nameBufLocal)
            {
                hrLocal = _legacyImpl.GetName(flags, bufLen, &nameLenLocal, nameBuf is null ? null : pNameBufLocal);

                Debug.ValidateHResult(hr, hrLocal);
                if (hr >= 0)
                {
                    if (nameLen is not null)
                        Debug.Assert(nameLenLocal == *nameLen, $"cDAC: {*nameLen:x}, DAC: {nameLenLocal:x}");

                    if (nameBuf is not null)
                    {
                        string dacName = new(pNameBufLocal);
                        string cdacName = new(nameBuf);
                        Debug.Assert(dacName == cdacName, $"cDAC: {cdacName}, DAC: {dacName}");
                    }
                }
            }
        }
#endif

        return hr;
    }

    int IXCLRDataTypeDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        int hr = HResults.S_OK;
        try
        {
            if (token is not null)
                *token = _token;

            if (!mod.IsNullRef)
            {
                IXCLRDataModule? legacyMod = null;
                if (LegacyFallbackHelper.CanFallback() && _legacyImpl is not null)
                {
                    DacComNullableByRef<IXCLRDataModule> legacyModOut = new(isNullRef: false);
                    int hrLegacy = _legacyImpl.GetTokenAndScope(null, legacyModOut);
                    Marshal.ThrowExceptionForHR(hrLegacy);
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
            uint tokenLocal = 0;
            DacComNullableByRef<IXCLRDataModule> legacyModOut = new(isNullRef: mod.IsNullRef);
            int hrLocal = _legacyImpl.GetTokenAndScope(validateToken ? &tokenLocal : null, legacyModOut);

            Debug.ValidateHResult(hr, hrLocal);
            if (validateToken && hr >= 0)
                Debug.Assert(tokenLocal == *token, $"cDAC: {*token:x}, DAC: {tokenLocal:x}");
        }
#endif

        return hr;
    }

    int IXCLRDataTypeDefinition.GetCorElementType(uint* type)
    {
        int hr = HResults.S_OK;
        try
        {
            if (type is null)
                throw new NullReferenceException();

            if (_typeHandle is null)
                throw new NotImplementedException();

            *type = (uint)_target.Contracts.RuntimeTypeSystem.GetInternalCorElementType(_typeHandle);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint typeLocal = 0;
            int hrLocal = _legacyImpl.GetCorElementType(type is null ? null : &typeLocal);

            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
                Debug.Assert(typeLocal == *type, $"cDAC: {*type:x}, DAC: {typeLocal:x}");
        }
#endif

        return hr;
    }

    int IXCLRDataTypeDefinition.GetFlags(uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.IsSameObject(IXCLRDataTypeDefinition? type)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.IsSameObject(type) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetArrayRank(uint* rank)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetArrayRank(rank) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetBase(DacComNullableByRef<IXCLRDataTypeDefinition> @base)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetBase(@base) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetNumFields(uint flags, uint* numFields)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetNumFields(flags, numFields) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumFields(uint flags, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumFields(flags, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumField(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumField(handle, nameBufLen, nameLen, nameBuf, type, flags, token) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumFields(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumFields(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumFieldsByName(name, nameFlags, fieldFlags, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumFieldByName(handle, type, flags, token) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumFieldsByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumFieldsByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetFieldByToken(uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetFieldByToken(token, nameBufLen, nameLen, nameBuf, type, flags) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetTypeNotification(uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetTypeNotification(flags) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.SetTypeNotification(uint flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.SetTypeNotification(flags) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumField2(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumField2(handle, nameBufLen, nameLen, nameBuf, type, flags, tokenScope, token) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumFieldByName2(handle, type, flags, tokenScope, token) : HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetFieldByToken2(IXCLRDataModule? tokenScope, uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetFieldByToken2(tokenScope, token, nameBufLen, nameLen, nameBuf, type, flags) : HResults.E_NOTIMPL;
}
