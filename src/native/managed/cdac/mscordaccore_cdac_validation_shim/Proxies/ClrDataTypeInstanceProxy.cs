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
/// Paired cDAC/DAC proxy for IXCLRDataTypeInstance.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataTypeInstanceProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataTypeInstance
{
    private readonly IXCLRDataTypeInstance? _cdacImpl;
    private readonly IXCLRDataTypeInstance? _legacyImpl;

    internal ClrDataTypeInstanceProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataTypeInstance;
        _legacyImpl = dacObject as IXCLRDataTypeInstance;
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

        if (iid == typeof(IXCLRDataTypeInstance).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataTypeInstance
    int IXCLRDataTypeInstance.StartEnumMethodInstances(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumMethodInstances(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumMethodInstances", "ClrDataTypeInstance.cs"))
            return _legacyImpl.StartEnumMethodInstances(handle);
        return hr;
    }

    int IXCLRDataTypeInstance.EnumMethodInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> methodInstance)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodInstance> methodInstanceCDac = new(methodInstance.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumMethodInstance(handle, methodInstanceCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumMethodInstance", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataMethodInstance> methodInstanceDac = new(methodInstance.IsNullRef);
            hr = _legacyImpl.EnumMethodInstance(handle, methodInstanceDac);
            if (!methodInstance.IsNullRef)
                methodInstance.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, null, methodInstanceDac.Interface);
            return hr;
        }
        if (!methodInstance.IsNullRef)
            methodInstance.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, methodInstanceCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.EndEnumMethodInstances(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumMethodInstances(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumMethodInstances", "ClrDataTypeInstance.cs"))
            return _legacyImpl.EndEnumMethodInstances(handle);
        return hr;
    }

    int IXCLRDataTypeInstance.StartEnumMethodInstancesByName(char* name, uint flags, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumMethodInstancesByName(name, flags, handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumMethodInstancesByName", "ClrDataTypeInstance.cs"))
            return _legacyImpl.StartEnumMethodInstancesByName(name, flags, handle);
        return hr;
    }

    int IXCLRDataTypeInstance.EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodInstance> methodCDac = new(method.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumMethodInstanceByName(handle, methodCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumMethodInstanceByName", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataMethodInstance> methodDac = new(method.IsNullRef);
            hr = _legacyImpl.EnumMethodInstanceByName(handle, methodDac);
            if (!method.IsNullRef)
                method.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, null, methodDac.Interface);
            return hr;
        }
        if (!method.IsNullRef)
            method.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, methodCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.EndEnumMethodInstancesByName(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumMethodInstancesByName(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumMethodInstancesByName", "ClrDataTypeInstance.cs"))
            return _legacyImpl.EndEnumMethodInstancesByName(handle);
        return hr;
    }

    int IXCLRDataTypeInstance.GetNumStaticFields(uint* numFields)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumStaticFields(numFields) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetNumStaticFields", "ClrDataTypeInstance.cs"))
            return _legacyImpl.GetNumStaticFields(numFields);
        return hr;
    }

    int IXCLRDataTypeInstance.GetStaticFieldByIndex(uint index, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf, uint* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> fieldCDac = new(field.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetStaticFieldByIndex(index, ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), fieldCDac, bufLen, nameLen, nameBuf, token) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetStaticFieldByIndex", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> fieldDac = new(field.IsNullRef);
            hr = _legacyImpl.GetStaticFieldByIndex(index, ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), fieldDac, bufLen, nameLen, nameBuf, token);
            if (!field.IsNullRef)
                field.Interface = ShimProxy.PairIXCLRDataValue(_session, null, fieldDac.Interface);
            return hr;
        }
        if (!field.IsNullRef)
            field.Interface = ShimProxy.PairIXCLRDataValue(_session, fieldCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName(char* name, uint flags, IXCLRDataTask? tlsTask, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumStaticFieldsByName(name, flags, ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumStaticFieldsByName", "ClrDataTypeInstance.cs"))
            return _legacyImpl.StartEnumStaticFieldsByName(name, flags, ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), handle);
        return hr;
    }

    int IXCLRDataTypeInstance.EnumStaticFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumStaticFieldByName(handle, valueCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumStaticFieldByName", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
            hr = _legacyImpl.EnumStaticFieldByName(handle, valueDac);
            if (!value.IsNullRef)
                value.Interface = ShimProxy.PairIXCLRDataValue(_session, null, valueDac.Interface);
            return hr;
        }
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumStaticFieldsByName(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumStaticFieldsByName", "ClrDataTypeInstance.cs"))
            return _legacyImpl.EndEnumStaticFieldsByName(handle);
        return hr;
    }

    int IXCLRDataTypeInstance.GetNumTypeArguments(uint* numTypeArgs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetNumTypeArguments", "ClrDataTypeInstance.cs"))
            return _legacyImpl.GetNumTypeArguments(numTypeArgs);
        return hr;
    }

    int IXCLRDataTypeInstance.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeInstance> typeArgCDac = new(typeArg.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetTypeArgumentByIndex(index, typeArgCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetTypeArgumentByIndex", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeInstance> typeArgDac = new(typeArg.IsNullRef);
            hr = _legacyImpl.GetTypeArgumentByIndex(index, typeArgDac);
            if (!typeArg.IsNullRef)
                typeArg.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, null, typeArgDac.Interface);
            return hr;
        }
        if (!typeArg.IsNullRef)
            typeArg.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, typeArgCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetName", "ClrDataTypeInstance.cs"))
            return _legacyImpl.GetName(flags, bufLen, nameLen, nameBuf);
        return hr;
    }

    int IXCLRDataTypeInstance.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetModule(modCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetModule", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataModule> modDac = new(mod.IsNullRef);
            hr = _legacyImpl.GetModule(modDac);
            if (!mod.IsNullRef)
                mod.Interface = ShimProxy.PairIXCLRDataModule(_session, null, modDac.Interface);
            return hr;
        }
        if (!mod.IsNullRef)
            mod.Interface = ShimProxy.PairIXCLRDataModule(_session, modCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.GetDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinitionCDac = new(typeDefinition.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetDefinition(typeDefinitionCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetDefinition", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinitionDac = new(typeDefinition.IsNullRef);
            hr = _legacyImpl.GetDefinition(typeDefinitionDac);
            if (!typeDefinition.IsNullRef)
                typeDefinition.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDefinitionDac.Interface);
            return hr;
        }
        if (!typeDefinition.IsNullRef)
            typeDefinition.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeDefinitionCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetFlags", "ClrDataTypeInstance.cs"))
            return _legacyImpl.GetFlags(flags);
        return hr;
    }

    int IXCLRDataTypeInstance.IsSameObject(IXCLRDataTypeInstance? type)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameObject(ShimProxy.UnwrapCDac<IXCLRDataTypeInstance>(type)) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("IsSameObject", "ClrDataTypeInstance.cs"))
            return _legacyImpl.IsSameObject(ShimProxy.UnwrapDac<IXCLRDataTypeInstance>(type));
        return hr;
    }

    int IXCLRDataTypeInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("Request", "ClrDataTypeInstance.cs"))
            return _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);
        return hr;
    }

    int IXCLRDataTypeInstance.GetNumStaticFields2(uint flags, uint* numFields)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumStaticFields2(flags, numFields) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetNumStaticFields2", "ClrDataTypeInstance.cs"))
            return _legacyImpl.GetNumStaticFields2(flags, numFields);
        return hr;
    }

    int IXCLRDataTypeInstance.StartEnumStaticFields(uint flags, IXCLRDataTask? tlsTask, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumStaticFields(flags, ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumStaticFields", "ClrDataTypeInstance.cs"))
            return _legacyImpl.StartEnumStaticFields(flags, ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), handle);
        return hr;
    }

    int IXCLRDataTypeInstance.EnumStaticField(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumStaticField(handle, valueCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumStaticField", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
            hr = _legacyImpl.EnumStaticField(handle, valueDac);
            if (!value.IsNullRef)
                value.Interface = ShimProxy.PairIXCLRDataValue(_session, null, valueDac.Interface);
            return hr;
        }
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.EndEnumStaticFields(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumStaticFields(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumStaticFields", "ClrDataTypeInstance.cs"))
            return _legacyImpl.EndEnumStaticFields(handle);
        return hr;
    }

    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName2(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTask? tlsTask, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumStaticFieldsByName2(name, nameFlags, fieldFlags, ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumStaticFieldsByName2", "ClrDataTypeInstance.cs"))
            return _legacyImpl.StartEnumStaticFieldsByName2(name, nameFlags, fieldFlags, ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), handle);
        return hr;
    }

    int IXCLRDataTypeInstance.EnumStaticFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumStaticFieldByName2(handle, valueCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumStaticFieldByName2", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
            hr = _legacyImpl.EnumStaticFieldByName2(handle, valueDac);
            if (!value.IsNullRef)
                value.Interface = ShimProxy.PairIXCLRDataValue(_session, null, valueDac.Interface);
            return hr;
        }
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName2(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumStaticFieldsByName2(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumStaticFieldsByName2", "ClrDataTypeInstance.cs"))
            return _legacyImpl.EndEnumStaticFieldsByName2(handle);
        return hr;
    }

    int IXCLRDataTypeInstance.GetStaticFieldByToken(uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> fieldCDac = new(field.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetStaticFieldByToken(token, ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), fieldCDac, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetStaticFieldByToken", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> fieldDac = new(field.IsNullRef);
            hr = _legacyImpl.GetStaticFieldByToken(token, ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), fieldDac, bufLen, nameLen, nameBuf);
            if (!field.IsNullRef)
                field.Interface = ShimProxy.PairIXCLRDataValue(_session, null, fieldDac.Interface);
            return hr;
        }
        if (!field.IsNullRef)
            field.Interface = ShimProxy.PairIXCLRDataValue(_session, fieldCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.GetBase(DacComNullableByRef<IXCLRDataTypeInstance> @base)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeInstance> baseCDac = new(@base.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetBase(baseCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetBase", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeInstance> baseDac = new(@base.IsNullRef);
            hr = _legacyImpl.GetBase(baseDac);
            if (!@base.IsNullRef)
                @base.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, null, baseDac.Interface);
            return hr;
        }
        if (!@base.IsNullRef)
            @base.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, baseCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.EnumStaticField2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, uint bufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> tokenScopeCDac = new(tokenScope.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumStaticField2(handle, valueCDac, bufLen, nameLen, nameBuf, tokenScopeCDac, token) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumStaticField2", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
            DacComNullableByRef<IXCLRDataModule> tokenScopeDac = new(tokenScope.IsNullRef);
            hr = _legacyImpl.EnumStaticField2(handle, valueDac, bufLen, nameLen, nameBuf, tokenScopeDac, token);
            if (!value.IsNullRef)
                value.Interface = ShimProxy.PairIXCLRDataValue(_session, null, valueDac.Interface);
            if (!tokenScope.IsNullRef)
                tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, null, tokenScopeDac.Interface);
            return hr;
        }
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, null);
        if (!tokenScope.IsNullRef)
            tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, tokenScopeCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.EnumStaticFieldByName3(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> tokenScopeCDac = new(tokenScope.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumStaticFieldByName3(handle, valueCDac, tokenScopeCDac, token) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumStaticFieldByName3", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
            DacComNullableByRef<IXCLRDataModule> tokenScopeDac = new(tokenScope.IsNullRef);
            hr = _legacyImpl.EnumStaticFieldByName3(handle, valueDac, tokenScopeDac, token);
            if (!value.IsNullRef)
                value.Interface = ShimProxy.PairIXCLRDataValue(_session, null, valueDac.Interface);
            if (!tokenScope.IsNullRef)
                tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, null, tokenScopeDac.Interface);
            return hr;
        }
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, null);
        if (!tokenScope.IsNullRef)
            tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, tokenScopeCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeInstance.GetStaticFieldByToken2(IXCLRDataModule? tokenScope, uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> fieldCDac = new(field.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetStaticFieldByToken2(ShimProxy.UnwrapCDac<IXCLRDataModule>(tokenScope), token, ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), fieldCDac, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetStaticFieldByToken2", "ClrDataTypeInstance.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> fieldDac = new(field.IsNullRef);
            hr = _legacyImpl.GetStaticFieldByToken2(ShimProxy.UnwrapDac<IXCLRDataModule>(tokenScope), token, ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), fieldDac, bufLen, nameLen, nameBuf);
            if (!field.IsNullRef)
                field.Interface = ShimProxy.PairIXCLRDataValue(_session, null, fieldDac.Interface);
            return hr;
        }
        if (!field.IsNullRef)
            field.Interface = ShimProxy.PairIXCLRDataValue(_session, fieldCDac.Interface, null);
        return hr;
    }

    #endregion IXCLRDataTypeInstance

}
