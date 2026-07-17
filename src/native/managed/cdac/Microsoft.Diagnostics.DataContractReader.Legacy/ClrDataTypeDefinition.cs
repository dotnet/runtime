// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeDefinition : IXCLRDataTypeDefinition
{
    private readonly Target _target;
    private readonly TargetPointer _module;
    private readonly uint _token;
    private readonly TypeHandle? _typeHandle;
    private readonly IXCLRDataTypeDefinition? _legacyImpl;

    public ClrDataTypeDefinition(
        Target target,
        TargetPointer module,
        uint token,
        TypeHandle? typeHandle,
        IXCLRDataTypeDefinition? legacyImpl)
    {
        _target = target;
        _module = module;
        _token = token;
        _typeHandle = typeHandle;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataTypeDefinition.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
    {
        try
        {
            IXCLRDataModule? legacyModule = null;
            if (_legacyImpl is not null)
            {
                DacComNullableByRef<IXCLRDataModule> legacyOut = new(isNullRef: false);
                int hr = _legacyImpl.GetModule(legacyOut);
                if (hr < 0)
                    return hr;
                legacyModule = legacyOut.Interface;
            }

            mod.Interface = new ClrDataModule(_module, _target, legacyModule);
            return HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }
    }

    int IXCLRDataTypeDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        try
        {
            if (flags != 0)
                throw new ArgumentException();

            string name = GetMetadataName();
            OutputBufferHelpers.CopyStringToBuffer(nameBuf, bufLen, nameLen, name);
            return nameBuf is not null && bufLen < name.Length + 1 ? HResults.S_FALSE : HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }
    }

    int IXCLRDataTypeDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        try
        {
            if (token is not null)
                *token = _token;

            if (!mod.IsNullRef)
                return ((IXCLRDataTypeDefinition)this).GetModule(mod);

            return HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }
    }

    private string GetMetadataName()
    {
        ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(_module);
        MetadataReader reader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)
            ?? throw new InvalidOperationException();
        TypeDefinitionHandle handle = MetadataTokens.TypeDefinitionHandle((int)(_token & 0x00ff_ffff));
        StringBuilder builder = new();
        AppendTypeName(reader, handle, builder);
        return builder.ToString();
    }

    private static void AppendTypeName(MetadataReader reader, TypeDefinitionHandle handle, StringBuilder builder)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        TypeDefinitionHandle declaringType = type.GetDeclaringType();
        if (!declaringType.IsNil)
        {
            AppendTypeName(reader, declaringType, builder);
            builder.Append('+');
        }
        else
        {
            string @namespace = reader.GetString(type.Namespace);
            if (@namespace.Length != 0)
            {
                builder.Append(@namespace);
                builder.Append('.');
            }
        }

        builder.Append(reader.GetString(type.Name));
    }

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
    int IXCLRDataTypeDefinition.GetCorElementType(uint* type)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetCorElementType(type) : HResults.E_NOTIMPL;
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
