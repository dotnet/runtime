// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeInstance : IXCLRDataTypeInstance
{
    private readonly Target _target;
    private readonly ITypeHandle _typeHandle;
    private readonly IXCLRDataTypeInstance? _legacyImpl;

    public ClrDataTypeInstance(Target target, ITypeHandle typeHandle, IXCLRDataTypeInstance? legacyImpl)
    {
        _target = target;
        _typeHandle = typeHandle;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataTypeInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        int hr = HResults.S_OK;
        try
        {
            if (flags != 0)
                throw new ArgumentException();

            StringBuilder builder = new();
            TypeNameBuilder.AppendType(_target, builder, _typeHandle, TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
            string result = builder.ToString();
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

    int IXCLRDataTypeInstance.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
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
            TargetPointer module = _target.Contracts.RuntimeTypeSystem.GetModule(_typeHandle);
            mod.Interface = new ClrDataModule(module, _target, legacyModule);
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

    int IXCLRDataTypeInstance.GetDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
    {
        IXCLRDataTypeDefinition? legacyDefinition = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> legacyOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetDefinition(legacyOut);
            legacyDefinition = legacyOut.Interface;
        }

        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            typeDefinition.Interface = new ClrDataTypeDefinition(_target, rts.GetModule(_typeHandle), rts.GetTypeDefToken(_typeHandle), legacyDefinition);
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

    int IXCLRDataTypeInstance.StartEnumMethodInstances(ulong* handle) => _legacyImpl?.StartEnumMethodInstances(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumMethodInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> methodInstance) => _legacyImpl?.EnumMethodInstance(handle, methodInstance) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumMethodInstances(ulong handle) => _legacyImpl?.EndEnumMethodInstances(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.StartEnumMethodInstancesByName(char* name, uint flags, ulong* handle) => _legacyImpl?.StartEnumMethodInstancesByName(name, flags, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method) => _legacyImpl?.EnumMethodInstanceByName(handle, method) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumMethodInstancesByName(ulong handle) => _legacyImpl?.EndEnumMethodInstancesByName(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetNumStaticFields(uint* numFields) => _legacyImpl?.GetNumStaticFields(numFields) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetStaticFieldByIndex(uint index, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf, uint* token) => _legacyImpl?.GetStaticFieldByIndex(index, tlsTask, field, bufLen, nameLen, nameBuf, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName(char* name, uint flags, IXCLRDataTask? tlsTask, ulong* handle) => _legacyImpl?.StartEnumStaticFieldsByName(name, flags, tlsTask, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value) => _legacyImpl?.EnumStaticFieldByName(handle, value) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName(ulong handle) => _legacyImpl?.EndEnumStaticFieldsByName(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetNumTypeArguments(uint* numTypeArgs) => _legacyImpl?.GetNumTypeArguments(numTypeArgs) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg) => _legacyImpl?.GetTypeArgumentByIndex(index, typeArg) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetFlags(uint* flags) => _legacyImpl?.GetFlags(flags) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.IsSameObject(IXCLRDataTypeInstance? type)
        => type is ClrDataTypeInstance other && other._typeHandle.Address == _typeHandle.Address
            ? HResults.S_OK
            : HResults.S_FALSE;
    int IXCLRDataTypeInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer) => _legacyImpl?.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetNumStaticFields2(uint flags, uint* numFields) => _legacyImpl?.GetNumStaticFields2(flags, numFields) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.StartEnumStaticFields(uint flags, IXCLRDataTask? tlsTask, ulong* handle) => _legacyImpl?.StartEnumStaticFields(flags, tlsTask, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticField(ulong* handle, DacComNullableByRef<IXCLRDataValue> value) => _legacyImpl?.EnumStaticField(handle, value) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumStaticFields(ulong handle) => _legacyImpl?.EndEnumStaticFields(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName2(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTask? tlsTask, ulong* handle) => _legacyImpl?.StartEnumStaticFieldsByName2(name, nameFlags, fieldFlags, tlsTask, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value) => _legacyImpl?.EnumStaticFieldByName2(handle, value) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName2(ulong handle) => _legacyImpl?.EndEnumStaticFieldsByName2(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetStaticFieldByToken(uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf) => _legacyImpl?.GetStaticFieldByToken(token, tlsTask, field, bufLen, nameLen, nameBuf) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetBase(DacComNullableByRef<IXCLRDataTypeInstance> @base) => _legacyImpl?.GetBase(@base) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticField2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, uint bufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token) => _legacyImpl?.EnumStaticField2(handle, value, bufLen, nameLen, nameBuf, tokenScope, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticFieldByName3(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token) => _legacyImpl?.EnumStaticFieldByName3(handle, value, tokenScope, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetStaticFieldByToken2(IXCLRDataModule? tokenScope, uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf) => _legacyImpl?.GetStaticFieldByToken2(tokenScope, token, tlsTask, field, bufLen, nameLen, nameBuf) ?? HResults.E_NOTIMPL;
}
