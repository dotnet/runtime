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
/// Paired cDAC/DAC proxy for IXCLRDataModule.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataModuleProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataModule, IXCLRDataModule2
{
    private readonly IXCLRDataModule? _cdacModule;
    private readonly IXCLRDataModule? _legacyModule;
    private readonly IXCLRDataModule2? _cdacModule2;
    private readonly IXCLRDataModule2? _legacyModule2;

    internal ClrDataModuleProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacModule = cdacObject as IXCLRDataModule;
        _legacyModule = dacObject as IXCLRDataModule;
        _cdacModule2 = cdacObject as IXCLRDataModule2;
        _legacyModule2 = dacObject as IXCLRDataModule2;
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

        if (iid == typeof(IXCLRDataModule).GUID)
            return Support(_cdacModule, _legacyModule);
        if (iid == typeof(IXCLRDataModule2).GUID)
            return Support(_cdacModule2, _legacyModule2);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataModule

    int IXCLRDataModule.StartEnumAssemblies(ulong* handle)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired enumeration handle here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.StartEnumAssemblies(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.EnumAssembly(ulong* handle, DacComNullableByRef<IXCLRDataAssembly> assembly)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.EnumAssembly(handle, assembly) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.EndEnumAssemblies(ulong handle)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired enumeration handle here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.EndEnumAssemblies(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.StartEnumTypeDefinitions(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        int hr = _cdacModule is not null ? _cdacModule.StartEnumTypeDefinitions(handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("StartEnumTypeDefinitions", "ClrDataModule.cs"))
        {
            ulong dacHandle = 0;
            hr = _legacyModule.StartEnumTypeDefinitions(handle is null ? null : &dacHandle);
            if (handle is not null && hr >= 0)
                *handle = _session.RegisterHandle(0, dacHandle, hasDacHandle: true);
            return hr;
        }
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, 0, hasDacHandle: false);
        return hr;
    }

    int IXCLRDataModule.EnumTypeDefinition(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinitionCDac = new(typeDefinition.IsNullRef);
        int hr = _cdacModule is not null ? _cdacModule.EnumTypeDefinition(handle is null ? null : &cdacHandle, typeDefinitionCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EnumTypeDefinition", "ClrDataModule.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinitionDac = new(typeDefinition.IsNullRef);
            hr = _legacyModule.EnumTypeDefinition(handle is null ? null : &dacHandle, typeDefinitionDac);
            if (pair is not null)
            {
                pair.DacHandle = dacHandle;
                pair.HasDacHandle = hr >= 0;
            }
            if (!typeDefinition.IsNullRef)
                typeDefinition.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, null, typeDefinitionDac.Interface);
            return hr;
        }
        if (pair is not null)
            pair.CDacHandle = cdacHandle;
        if (!typeDefinition.IsNullRef)
            typeDefinition.Interface = ShimProxy.PairIXCLRDataTypeDefinition(_session, typeDefinitionCDac.Interface, null);
        return hr;
    }

    int IXCLRDataModule.EndEnumTypeDefinitions(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacModule is not null ? _cdacModule.EndEnumTypeDefinitions(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EndEnumTypeDefinitions", "ClrDataModule.cs"))
        {
            return _legacyModule.EndEnumTypeDefinitions(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataModule.StartEnumTypeInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        int hr = _cdacModule is not null ? _cdacModule.StartEnumTypeInstances(ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("StartEnumTypeInstances", "ClrDataModule.cs"))
        {
            ulong dacHandle = 0;
            hr = _legacyModule.StartEnumTypeInstances(ShimProxy.UnwrapDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &dacHandle);
            if (handle is not null && hr >= 0)
                *handle = _session.RegisterHandle(0, dacHandle, hasDacHandle: true);
            return hr;
        }
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, 0, hasDacHandle: false);
        return hr;
    }

    int IXCLRDataModule.EnumTypeInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> typeInstance)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataTypeInstance> typeInstanceCDac = new(typeInstance.IsNullRef);
        int hr = _cdacModule is not null ? _cdacModule.EnumTypeInstance(handle is null ? null : &cdacHandle, typeInstanceCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EnumTypeInstance", "ClrDataModule.cs"))
        {
            DacComNullableByRef<IXCLRDataTypeInstance> typeInstanceDac = new(typeInstance.IsNullRef);
            hr = _legacyModule.EnumTypeInstance(handle is null ? null : &dacHandle, typeInstanceDac);
            if (pair is not null)
            {
                pair.DacHandle = dacHandle;
                pair.HasDacHandle = hr >= 0;
            }
            if (!typeInstance.IsNullRef)
                typeInstance.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, null, typeInstanceDac.Interface);
            return hr;
        }
        if (pair is not null)
            pair.CDacHandle = cdacHandle;
        if (!typeInstance.IsNullRef)
            typeInstance.Interface = ShimProxy.PairIXCLRDataTypeInstance(_session, typeInstanceCDac.Interface, null);
        return hr;
    }

    int IXCLRDataModule.EndEnumTypeInstances(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacModule is not null ? _cdacModule.EndEnumTypeInstances(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EndEnumTypeInstances", "ClrDataModule.cs"))
        {
            return _legacyModule.EndEnumTypeInstances(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataModule.StartEnumTypeDefinitionsByName(char* name, uint flags, ulong* handle)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired enumeration handle here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.StartEnumTypeDefinitionsByName(name, flags, handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.EnumTypeDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.EnumTypeDefinitionByName(handle, type) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.EndEnumTypeDefinitionsByName(ulong handle)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired enumeration handle here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.EndEnumTypeDefinitionsByName(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.StartEnumTypeInstancesByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired enumeration handle here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.StartEnumTypeInstancesByName(name, flags, ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.EnumTypeInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> type)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.EnumTypeInstanceByName(handle, type) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.EndEnumTypeInstancesByName(ulong handle)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired enumeration handle here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.EndEnumTypeInstancesByName(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.GetTypeDefinitionByToken(/*mdTypeDef*/ uint token, DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
    {
        // The pre-refactor cDAC returned E_NOTIMPL and never touched the legacy DAC, so there is
        // no comparison and no paired child object here.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.GetTypeDefinitionByToken(token, typeDefinition) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataModule.StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacModule is not null ? _cdacModule.StartEnumMethodDefinitionsByName(name, flags, handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyModule is not null)
        {
            hrLocal = _legacyModule.StartEnumMethodDefinitionsByName(name, flags, handle is null ? null : &dacHandle);
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
        return hr;
    }

    int IXCLRDataModule.EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataMethodDefinition> methodCDac = new(method.IsNullRef);
        DacComNullableByRef<IXCLRDataMethodDefinition> methodDac = new(method.IsNullRef);
        int hr = _cdacModule is not null ? _cdacModule.EnumMethodDefinitionByName(handle is null ? null : &cdacHandle, methodCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyModule is not null)
        {
            hrLocal = _legacyModule.EnumMethodDefinitionByName(handle is null ? null : &dacHandle, methodDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (pair is not null)
        {
            pair.CDacHandle = cdacHandle;
            if (calledDac)
                pair.DacHandle = dacHandle;
        }
        if (!method.IsNullRef)
            method.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, methodCDac.Interface, methodDac.Interface);
        return hr;
    }

    int IXCLRDataModule.EndEnumMethodDefinitionsByName(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacModule is not null ? _cdacModule.EndEnumMethodDefinitionsByName(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if ((pair is null || pair.HasDacHandle) && _legacyModule is not null)
        {
            _legacyModule.EndEnumMethodDefinitionsByName(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataModule.StartEnumMethodInstancesByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        int hr = _cdacModule is not null ? _cdacModule.StartEnumMethodInstancesByName(name, flags, ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("StartEnumMethodInstancesByName", "ClrDataModule.cs"))
        {
            ulong dacHandle = 0;
            hr = _legacyModule.StartEnumMethodInstancesByName(name, flags, ShimProxy.UnwrapDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &dacHandle);
            if (handle is not null && hr >= 0)
                *handle = _session.RegisterHandle(0, dacHandle, hasDacHandle: true);
            return hr;
        }
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, 0, hasDacHandle: false);
        return hr;
    }

    int IXCLRDataModule.EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataMethodInstance> methodCDac = new(method.IsNullRef);
        int hr = _cdacModule is not null ? _cdacModule.EnumMethodInstanceByName(handle is null ? null : &cdacHandle, methodCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EnumMethodInstanceByName", "ClrDataModule.cs"))
        {
            DacComNullableByRef<IXCLRDataMethodInstance> methodDac = new(method.IsNullRef);
            hr = _legacyModule.EnumMethodInstanceByName(handle is null ? null : &dacHandle, methodDac);
            if (pair is not null)
            {
                pair.DacHandle = dacHandle;
                pair.HasDacHandle = hr >= 0;
            }
            if (!method.IsNullRef)
                method.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, null, methodDac.Interface);
            return hr;
        }
        if (pair is not null)
            pair.CDacHandle = cdacHandle;
        if (!method.IsNullRef)
            method.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, methodCDac.Interface, null);
        return hr;
    }

    int IXCLRDataModule.EndEnumMethodInstancesByName(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacModule is not null ? _cdacModule.EndEnumMethodInstancesByName(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EndEnumMethodInstancesByName", "ClrDataModule.cs"))
        {
            return _legacyModule.EndEnumMethodInstancesByName(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataModule.GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionCDac = new(methodDefinition.IsNullRef);
        DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinitionDac = new(methodDefinition.IsNullRef);
        int hr = _cdacModule is not null ? _cdacModule.GetMethodDefinitionByToken(token, methodDefinitionCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyModule is not null)
        {
            hrLocal = _legacyModule.GetMethodDefinitionByToken(token, methodDefinitionDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!methodDefinition.IsNullRef)
            methodDefinition.Interface = ShimProxy.PairIXCLRDataMethodDefinition(_session, methodDefinitionCDac.Interface, methodDefinitionDac.Interface);
        return hr;
    }

    int IXCLRDataModule.StartEnumDataByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, IXCLRDataTask? tlsTask, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        int hr = _cdacModule is not null ? _cdacModule.StartEnumDataByName(name, flags, ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("StartEnumDataByName", "ClrDataModule.cs"))
        {
            ulong dacHandle = 0;
            hr = _legacyModule.StartEnumDataByName(name, flags, ShimProxy.UnwrapDac<IXCLRDataAppDomain>(appDomain), ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), handle is null ? null : &dacHandle);
            if (handle is not null && hr >= 0)
                *handle = _session.RegisterHandle(0, dacHandle, hasDacHandle: true);
            return hr;
        }
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, 0, hasDacHandle: false);
        return hr;
    }

    int IXCLRDataModule.EnumDataByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        int hr = _cdacModule is not null ? _cdacModule.EnumDataByName(handle is null ? null : &cdacHandle, valueCDac) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EnumDataByName", "ClrDataModule.cs"))
        {
            DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
            hr = _legacyModule.EnumDataByName(handle is null ? null : &dacHandle, valueDac);
            if (pair is not null)
            {
                pair.DacHandle = dacHandle;
                pair.HasDacHandle = hr >= 0;
            }
            if (!value.IsNullRef)
                value.Interface = ShimProxy.PairIXCLRDataValue(_session, null, valueDac.Interface);
            return hr;
        }
        if (pair is not null)
            pair.CDacHandle = cdacHandle;
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, null);
        return hr;
    }

    int IXCLRDataModule.EndEnumDataByName(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacModule is not null ? _cdacModule.EndEnumDataByName(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EndEnumDataByName", "ClrDataModule.cs"))
        {
            return _legacyModule.EndEnumDataByName(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataModule.GetName(uint bufLen, uint* nameLen, char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.GetName(bufLen, nameLen, name) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyModule is not null)
        {
            char[] nameLocal = new char[bufLen];
            uint nameLenLocal;
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyModule.GetName(bufLen, &nameLenLocal, ptr);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(nameLen == null || *nameLen == nameLenLocal);
                Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)nameLenLocal - 1).SequenceEqual(new string(name)));
            }
        }
#endif
        return hr;
    }

    int IXCLRDataModule.GetFileName(uint bufLen, uint* nameLen, char* name)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.GetFileName(bufLen, nameLen, name) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyModule is not null)
        {
            char[] nameLocal = new char[bufLen];
            uint nameLenLocal;
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyModule.GetFileName(bufLen, &nameLenLocal, ptr);
            }
            Debug.Assert(hrLocal == HResults.S_OK);
            Debug.Assert(nameLen == null || *nameLen == nameLenLocal);
            Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)nameLenLocal - 1).SequenceEqual(new string(name)));
        }
#endif
        return hr;
    }

    int IXCLRDataModule.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.GetFlags(flags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyModule is not null)
        {
            uint flagsLocal;
            int hrLocal = _legacyModule.GetFlags(&flagsLocal);
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
            Debug.Assert(flagsLocal == *flags, $"cDAC: {*flags}, DAC: {flagsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataModule.IsSameObject(IXCLRDataModule* mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.IsSameObject(mod) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("IsSameObject", "ClrDataModule.cs"))
        {
            return _legacyModule.IsSameObject(mod);
        }
        return hr;
    }

    int IXCLRDataModule.StartEnumExtents(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacModule is not null ? _cdacModule.StartEnumExtents(handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyModule is not null)
        {
            hrLocal = _legacyModule.StartEnumExtents(handle is null ? null : &dacHandle);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
        }
#endif
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, dacHandle, (calledDac) && hrLocal >= 0);
        return hr;
    }

    int IXCLRDataModule.EnumExtent(ulong* handle, /*CLRDATA_MODULE_EXTENT*/ void* extent)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        int hr = _cdacModule is not null ? _cdacModule.EnumExtent(handle is null ? null : &cdacHandle, extent) : HResults.E_NOTIMPL;
        CLRDataModuleExtent dataModuleExtentLocal = default;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyModule is not null)
        {
            hrLocal = _legacyModule.EnumExtent(handle is null ? null : &dacHandle, &dataModuleExtentLocal);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.Assert(hr == hrLocal, $"cDAC: {hr}, DAC: {hrLocal}");
            if (hr == HResults.S_OK)
            {
                CLRDataModuleExtent* dataModuleExtent = (CLRDataModuleExtent*)extent;
                Debug.Assert(dataModuleExtent->baseAddress == dataModuleExtentLocal.baseAddress, $"cDAC: {dataModuleExtent->baseAddress}, DAC: {dataModuleExtentLocal.baseAddress}");
                Debug.Assert(dataModuleExtent->length == dataModuleExtentLocal.length, $"cDAC: {dataModuleExtent->length}, DAC: {dataModuleExtentLocal.length}");
                Debug.Assert(dataModuleExtent->type == dataModuleExtentLocal.type, $"cDAC: {dataModuleExtent->type}, DAC: {dataModuleExtentLocal.type}");
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

    int IXCLRDataModule.EndEnumExtents(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacModule is not null ? _cdacModule.EndEnumExtents(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyModule is not null)
        {
            hrLocal = _legacyModule.EndEnumExtents(pair is null ? handle : pair.DacHandle);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataModule.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyModule is not null)
        {
            byte[] localBuffer = new byte[(int)outBufferSize];
            fixed (byte* localOutBuffer = localBuffer)
            {
                int hrLocal = _legacyModule.Request(reqCode, inBufferSize, inBuffer, outBufferSize, localOutBuffer);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                    Debug.Assert(new ReadOnlySpan<byte>(outBuffer, (int)outBufferSize).SequenceEqual(localBuffer));
            }
        }
#endif
        return hr;
    }

    int IXCLRDataModule.StartEnumAppDomains(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        int hr = _cdacModule is not null ? _cdacModule.StartEnumAppDomains(handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("StartEnumAppDomains", "ClrDataModule.cs"))
        {
            ulong dacHandle = 0;
            hr = _legacyModule.StartEnumAppDomains(handle is null ? null : &dacHandle);
            if (handle is not null && hr >= 0)
                *handle = _session.RegisterHandle(0, dacHandle, hasDacHandle: true);
            return hr;
        }
        if (handle is not null && hr >= 0)
            *handle = _session.RegisterHandle(cdacHandle, 0, hasDacHandle: false);
        return hr;
    }

    int IXCLRDataModule.EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        int hr = _cdacModule is not null ? _cdacModule.EnumAppDomain(handle is null ? null : &cdacHandle, appDomain) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EnumAppDomain", "ClrDataModule.cs"))
        {
            hr = _legacyModule.EnumAppDomain(handle is null ? null : &dacHandle, appDomain);
            if (pair is not null)
            {
                pair.DacHandle = dacHandle;
                pair.HasDacHandle = hr >= 0;
            }
            return hr;
        }
        if (pair is not null)
            pair.CDacHandle = cdacHandle;
        return hr;
    }

    int IXCLRDataModule.EndEnumAppDomains(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacModule is not null ? _cdacModule.EndEnumAppDomains(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("EndEnumAppDomains", "ClrDataModule.cs"))
        {
            return _legacyModule.EndEnumAppDomains(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataModule.GetVersionId(Guid* vid)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule is not null ? _cdacModule.GetVersionId(vid) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyModule is not null && LegacyFallbackHelper.CanFallback("GetVersionId", "ClrDataModule.cs"))
        {
            return _legacyModule.GetVersionId(vid);
        }
        return hr;
    }

    #endregion IXCLRDataModule

    #region IXCLRDataModule2
    int IXCLRDataModule2.SetJITCompilerFlags(uint flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacModule2 is not null ? _cdacModule2.SetJITCompilerFlags(flags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyModule2 is not null)
        {
            int hrLocal = _legacyModule2.SetJITCompilerFlags(flags);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    #endregion IXCLRDataModule2

}
