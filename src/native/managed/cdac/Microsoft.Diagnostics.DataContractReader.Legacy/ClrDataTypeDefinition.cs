// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeDefinition : IXCLRDataTypeDefinition
{
    private readonly Target _target;
    private readonly TargetPointer _module;
    private readonly uint _token;
    private readonly IXCLRDataTypeDefinition? _legacyImpl;

    public ClrDataTypeDefinition(Target target, TargetPointer module, uint token, IXCLRDataTypeDefinition? legacyImpl)
    {
        _target = target;
        _module = module;
        _token = token;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataTypeDefinition.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
    {
        IXCLRDataModule? legacyModule = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataModule> legacyOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetModule(legacyOut);
            legacyModule = legacyOut.Interface;
        }

        int hr = HResults.S_OK;
        try
        {
            mod.Interface = new ClrDataModule(_module, _target, legacyModule);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif
        return hr;
    }

    int IXCLRDataTypeDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        int hr = HResults.S_OK;
        try
        {
            if (flags != 0)
                throw new ArgumentException();

            ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle module = loader.GetModuleHandleFromModulePtr(_module);
            MetadataReader reader = _target.Contracts.EcmaMetadata.GetMetadata(module)
                ?? throw new InvalidOperationException("Failed to get metadata reader");
            TypeDefinitionHandle handle = MetadataTokens.TypeDefinitionHandle((int)(_token & 0x00ffffff));
            string result = GetTypeName(reader, handle);
            OutputBufferHelpers.CopyStringToBuffer(nameBuf, bufLen, nameLen, result);
            if (nameBuf is not null && bufLen < result.Length + 1)
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal;
            char[] nameLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pNameLocal = nameLocal)
                hrLocal = _legacyImpl.GetName(flags, bufLen, &nameLenLocal, nameBuf is null ? null : pNameLocal);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    private static string GetTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var names = new List<string>();
        TypeDefinition definition = reader.GetTypeDefinition(handle);
        while (true)
        {
            names.Add(reader.GetString(definition.Name));
            if (!definition.IsNested)
                break;

            handle = definition.GetDeclaringType();
            definition = reader.GetTypeDefinition(handle);
        }

        names.Reverse();
        string name = string.Join('+', names);
        string @namespace = reader.GetString(definition.Namespace);
        return string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
    }

    int IXCLRDataTypeDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        int hr = HResults.S_OK;
        try
        {
            if (token is not null)
                *token = _token;
            if (!mod.IsNullRef)
                ((IXCLRDataTypeDefinition)this).GetModule(mod);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int IXCLRDataTypeDefinition.StartEnumMethodDefinitions(ulong* handle) => _legacyImpl?.StartEnumMethodDefinitions(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EnumMethodDefinition(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition) => _legacyImpl?.EnumMethodDefinition(handle, methodDefinition) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EndEnumMethodDefinitions(ulong handle) => _legacyImpl?.EndEnumMethodDefinitions(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle) => _legacyImpl?.StartEnumMethodDefinitionsByName(name, flags, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method) => _legacyImpl?.EnumMethodDefinitionByName(handle, method) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EndEnumMethodDefinitionsByName(ulong handle) => _legacyImpl?.EndEnumMethodDefinitionsByName(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetMethodDefinitionByToken(uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition) => _legacyImpl?.GetMethodDefinitionByToken(token, methodDefinition) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle) => _legacyImpl?.StartEnumInstances(appDomain, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> instance) => _legacyImpl?.EnumInstance(handle, instance) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EndEnumInstances(ulong handle) => _legacyImpl?.EndEnumInstances(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetCorElementType(uint* type) => _legacyImpl?.GetCorElementType(type) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetFlags(uint* flags) => _legacyImpl?.GetFlags(flags) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.IsSameObject(IXCLRDataTypeDefinition? type)
        => type is ClrDataTypeDefinition other && other._module == _module && other._token == _token
            ? HResults.S_OK
            : HResults.S_FALSE;
    int IXCLRDataTypeDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer) => _legacyImpl?.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetArrayRank(uint* rank) => _legacyImpl?.GetArrayRank(rank) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetBase(DacComNullableByRef<IXCLRDataTypeDefinition> @base) => _legacyImpl?.GetBase(@base) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetNumFields(uint flags, uint* numFields) => _legacyImpl?.GetNumFields(flags, numFields) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.StartEnumFields(uint flags, ulong* handle) => _legacyImpl?.StartEnumFields(flags, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EnumField(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token) => _legacyImpl?.EnumField(handle, nameBufLen, nameLen, nameBuf, type, flags, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EndEnumFields(ulong handle) => _legacyImpl?.EndEnumFields(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, ulong* handle) => _legacyImpl?.StartEnumFieldsByName(name, nameFlags, fieldFlags, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token) => _legacyImpl?.EnumFieldByName(handle, type, flags, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EndEnumFieldsByName(ulong handle) => _legacyImpl?.EndEnumFieldsByName(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetFieldByToken(uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags) => _legacyImpl?.GetFieldByToken(token, nameBufLen, nameLen, nameBuf, type, flags) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetTypeNotification(uint* flags) => _legacyImpl?.GetTypeNotification(flags) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.SetTypeNotification(uint flags) => _legacyImpl?.SetTypeNotification(flags) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EnumField2(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token) => _legacyImpl?.EnumField2(handle, nameBufLen, nameLen, nameBuf, type, flags, tokenScope, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.EnumFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token) => _legacyImpl?.EnumFieldByName2(handle, type, flags, tokenScope, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeDefinition.GetFieldByToken2(IXCLRDataModule? tokenScope, uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags) => _legacyImpl?.GetFieldByToken2(tokenScope, token, nameBufLen, nameLen, nameBuf, type, flags) ?? HResults.E_NOTIMPL;
}
