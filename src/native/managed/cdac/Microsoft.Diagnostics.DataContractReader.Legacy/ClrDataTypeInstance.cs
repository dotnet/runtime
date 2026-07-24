// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeInstance : IXCLRDataTypeInstance
{
    private readonly Target _target;
    private readonly IXCLRDataTypeInstance? _legacyImpl;

    public ClrDataTypeInstance(Target target, IXCLRDataTypeInstance? legacyImpl)
    {
        _target = target;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataTypeInstance.StartEnumMethodInstances(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumMethodInstances(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumMethodInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> methodInstance)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumMethodInstance(handle, methodInstance) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumMethodInstances(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumMethodInstances(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumMethodInstancesByName(char* name, uint flags, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumMethodInstancesByName(name, flags, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumMethodInstanceByName(handle, method) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumMethodInstancesByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumMethodInstancesByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetNumStaticFields(uint* numFields)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetNumStaticFields(numFields) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetStaticFieldByIndex(uint index, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf, uint* token)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetStaticFieldByIndex(index, tlsTask, field, bufLen, nameLen, nameBuf, token) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName(char* name, uint flags, IXCLRDataTask? tlsTask, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumStaticFieldsByName(name, flags, tlsTask, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumStaticFieldByName(handle, value) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumStaticFieldsByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetNumTypeArguments(uint* numTypeArgs)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetModule(mod) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetDefinition(typeDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetFlags(uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.IsSameObject(IXCLRDataTypeInstance? type)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.IsSameObject(type) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetNumStaticFields2(uint flags, uint* numFields)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetNumStaticFields2(flags, numFields) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumStaticFields(uint flags, IXCLRDataTask? tlsTask, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumStaticFields(flags, tlsTask, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticField(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumStaticField(handle, value) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumStaticFields(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumStaticFields(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName2(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTask? tlsTask, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.StartEnumStaticFieldsByName2(name, nameFlags, fieldFlags, tlsTask, handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumStaticFieldByName2(handle, value) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName2(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EndEnumStaticFieldsByName2(handle) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetStaticFieldByToken(uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetStaticFieldByToken(token, tlsTask, field, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetBase(DacComNullableByRef<IXCLRDataTypeInstance> @base)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetBase(@base) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticField2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, uint bufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumStaticField2(handle, value, bufLen, nameLen, nameBuf, tokenScope, token) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticFieldByName3(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.EnumStaticFieldByName3(handle, value, tokenScope, token) : HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetStaticFieldByToken2(IXCLRDataModule? tokenScope, uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetStaticFieldByToken2(tokenScope, token, tlsTask, field, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
}
