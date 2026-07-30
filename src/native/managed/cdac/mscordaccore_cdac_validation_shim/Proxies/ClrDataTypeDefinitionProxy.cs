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
/// Paired cDAC/DAC proxy for IXCLRDataTypeDefinition.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataTypeDefinitionProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataTypeDefinition
{
    private readonly IXCLRDataTypeDefinition? _cdacImpl;
    private readonly IXCLRDataTypeDefinition? _legacyImpl;

    internal ClrDataTypeDefinitionProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataTypeDefinition;
        _legacyImpl = dacObject as IXCLRDataTypeDefinition;
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

        if (iid == typeof(IXCLRDataTypeDefinition).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataTypeDefinition
    int IXCLRDataTypeDefinition.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetModule(modCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetModule", "ClrDataTypeDefinition.cs"))
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

    int IXCLRDataTypeDefinition.StartEnumMethodDefinitions(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumMethodDefinitions(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumMethodDefinitions", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.StartEnumMethodDefinitions(handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.EnumMethodDefinition(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionCDac = new(methodDefinition.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumMethodDefinition(handle, methodDefinitionCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumMethodDefinition", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionDac = new(methodDefinition.IsNullRef);
            hr = _legacyImpl.EnumMethodDefinition(handle, methodDefinitionDac);
            if (!methodDefinition.IsNullRef)
                methodDefinition.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, null, methodDefinitionDac.Interface);
            return hr;
        }
        if (!methodDefinition.IsNullRef)
            methodDefinition.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, methodDefinitionCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.EndEnumMethodDefinitions(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumMethodDefinitions(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumMethodDefinitions", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.EndEnumMethodDefinitions(handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumMethodDefinitionsByName(name, flags, handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumMethodDefinitionsByName", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.StartEnumMethodDefinitionsByName(name, flags, handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodDefinition> methodCDac = new(method.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumMethodDefinitionByName(handle, methodCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumMethodDefinitionByName", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataMethodDefinition> methodDac = new(method.IsNullRef);
            hr = _legacyImpl.EnumMethodDefinitionByName(handle, methodDac);
            if (!method.IsNullRef)
                method.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, null, methodDac.Interface);
            return hr;
        }
        if (!method.IsNullRef)
            method.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, methodCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.EndEnumMethodDefinitionsByName(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumMethodDefinitionsByName(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumMethodDefinitionsByName", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.EndEnumMethodDefinitionsByName(handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetMethodDefinitionByToken(uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionCDac = new(methodDefinition.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodDefinitionByToken(token, methodDefinitionCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetMethodDefinitionByToken", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionDac = new(methodDefinition.IsNullRef);
            hr = _legacyImpl.GetMethodDefinitionByToken(token, methodDefinitionDac);
            if (!methodDefinition.IsNullRef)
                methodDefinition.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, null, methodDefinitionDac.Interface);
            return hr;
        }
        if (!methodDefinition.IsNullRef)
            methodDefinition.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, methodDefinitionCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumInstances(ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumInstances", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.StartEnumInstances(ShimProxy.UnwrapDac<IXCLRDataAppDomain>(appDomain), handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> instance)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeInstance> instanceCDac = new(instance.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumInstance(handle, instanceCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumInstance", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeInstance> instanceDac = new(instance.IsNullRef);
            hr = _legacyImpl.EnumInstance(handle, instanceDac);
            if (!instance.IsNullRef)
                instance.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, null, instanceDac.Interface);
            return hr;
        }
        if (!instance.IsNullRef)
            instance.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, instanceCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.EndEnumInstances(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumInstances(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumInstances", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.EndEnumInstances(handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetName", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.GetName(flags, bufLen, nameLen, nameBuf);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetTokenAndScope(token, modCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetTokenAndScope", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataModule> modDac = new(mod.IsNullRef);
            hr = _legacyImpl.GetTokenAndScope(token, modDac);
            if (!mod.IsNullRef)
                mod.Interface = ShimProxy.PairIXCLRDataModule(_session, null, modDac.Interface);
            return hr;
        }
        if (!mod.IsNullRef)
            mod.Interface = ShimProxy.PairIXCLRDataModule(_session, modCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetCorElementType(uint* type)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCorElementType(type) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetCorElementType", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.GetCorElementType(type);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetFlags", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.GetFlags(flags);
        return hr;
    }

    int IXCLRDataTypeDefinition.IsSameObject(IXCLRDataTypeDefinition? type)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.IsSameObject(ShimProxy.UnwrapCDac<IXCLRDataTypeDefinition>(type)) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("IsSameObject", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.IsSameObject(ShimProxy.UnwrapDac<IXCLRDataTypeDefinition>(type));
        return hr;
    }

    int IXCLRDataTypeDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("Request", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetArrayRank(uint* rank)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetArrayRank(rank) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetArrayRank", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.GetArrayRank(rank);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetBase(DacComNullableByRef<IXCLRDataTypeDefinition> @base)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> baseCDac = new(@base.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetBase(baseCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetBase", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> baseDac = new(@base.IsNullRef);
            hr = _legacyImpl.GetBase(baseDac);
            if (!@base.IsNullRef)
                @base.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, baseDac.Interface);
            return hr;
        }
        if (!@base.IsNullRef)
            @base.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, baseCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetNumFields(uint flags, uint* numFields)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumFields(flags, numFields) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetNumFields", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.GetNumFields(flags, numFields);
        return hr;
    }

    int IXCLRDataTypeDefinition.StartEnumFields(uint flags, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumFields(flags, handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumFields", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.StartEnumFields(flags, handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.EnumField(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> typeCDac = new(type.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumField(handle, nameBufLen, nameLen, nameBuf, typeCDac, flags, token) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumField", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDac = new(type.IsNullRef);
            hr = _legacyImpl.EnumField(handle, nameBufLen, nameLen, nameBuf, typeDac, flags, token);
            if (!type.IsNullRef)
                type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDac.Interface);
            return hr;
        }
        if (!type.IsNullRef)
            type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.EndEnumFields(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumFields(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumFields", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.EndEnumFields(handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumFieldsByName(name, nameFlags, fieldFlags, handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("StartEnumFieldsByName", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.StartEnumFieldsByName(name, nameFlags, fieldFlags, handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> typeCDac = new(type.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumFieldByName(handle, typeCDac, flags, token) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumFieldByName", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDac = new(type.IsNullRef);
            hr = _legacyImpl.EnumFieldByName(handle, typeDac, flags, token);
            if (!type.IsNullRef)
                type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDac.Interface);
            return hr;
        }
        if (!type.IsNullRef)
            type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.EndEnumFieldsByName(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumFieldsByName(handle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EndEnumFieldsByName", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.EndEnumFieldsByName(handle);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetFieldByToken(uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> typeCDac = new(type.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetFieldByToken(token, nameBufLen, nameLen, nameBuf, typeCDac, flags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetFieldByToken", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDac = new(type.IsNullRef);
            hr = _legacyImpl.GetFieldByToken(token, nameBufLen, nameLen, nameBuf, typeDac, flags);
            if (!type.IsNullRef)
                type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDac.Interface);
            return hr;
        }
        if (!type.IsNullRef)
            type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetTypeNotification(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetTypeNotification(flags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetTypeNotification", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.GetTypeNotification(flags);
        return hr;
    }

    int IXCLRDataTypeDefinition.SetTypeNotification(uint flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.SetTypeNotification(flags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("SetTypeNotification", "ClrDataTypeDefinition.cs"))
            return _legacyImpl.SetTypeNotification(flags);
        return hr;
    }

    int IXCLRDataTypeDefinition.EnumField2(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> typeCDac = new(type.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> tokenScopeCDac = new(tokenScope.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumField2(handle, nameBufLen, nameLen, nameBuf, typeCDac, flags, tokenScopeCDac, token) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumField2", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDac = new(type.IsNullRef);
            DacComNullableByRef<IXCLRDataModule> tokenScopeDac = new(tokenScope.IsNullRef);
            hr = _legacyImpl.EnumField2(handle, nameBufLen, nameLen, nameBuf, typeDac, flags, tokenScopeDac, token);
            if (!type.IsNullRef)
                type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDac.Interface);
            if (!tokenScope.IsNullRef)
                tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, null, tokenScopeDac.Interface);
            return hr;
        }
        if (!type.IsNullRef)
            type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeCDac.Interface, null);
        if (!tokenScope.IsNullRef)
            tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, tokenScopeCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.EnumFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> typeCDac = new(type.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> tokenScopeCDac = new(tokenScope.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.EnumFieldByName2(handle, typeCDac, flags, tokenScopeCDac, token) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("EnumFieldByName2", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDac = new(type.IsNullRef);
            DacComNullableByRef<IXCLRDataModule> tokenScopeDac = new(tokenScope.IsNullRef);
            hr = _legacyImpl.EnumFieldByName2(handle, typeDac, flags, tokenScopeDac, token);
            if (!type.IsNullRef)
                type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDac.Interface);
            if (!tokenScope.IsNullRef)
                tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, null, tokenScopeDac.Interface);
            return hr;
        }
        if (!type.IsNullRef)
            type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeCDac.Interface, null);
        if (!tokenScope.IsNullRef)
            tokenScope.Interface = ShimProxy.PairIXCLRDataModule(_session, tokenScopeCDac.Interface, null);
        return hr;
    }

    int IXCLRDataTypeDefinition.GetFieldByToken2(IXCLRDataModule? tokenScope, uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTypeDefinition> typeCDac = new(type.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetFieldByToken2(ShimProxy.UnwrapCDac<IXCLRDataModule>(tokenScope), token, nameBufLen, nameLen, nameBuf, typeCDac, flags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyImpl is not null && LegacyFallbackHelper.CanFallback("GetFieldByToken2", "ClrDataTypeDefinition.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDac = new(type.IsNullRef);
            hr = _legacyImpl.GetFieldByToken2(ShimProxy.UnwrapDac<IXCLRDataModule>(tokenScope), token, nameBufLen, nameLen, nameBuf, typeDac, flags);
            if (!type.IsNullRef)
                type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDac.Interface);
            return hr;
        }
        if (!type.IsNullRef)
            type.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeCDac.Interface, null);
        return hr;
    }

    #endregion IXCLRDataTypeDefinition

}
