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
/// SOS-DAC and IXCLRDataProcess surface of a paired cDAC/DAC process object.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class SOSDacImplProxy
    : ShimProxy, ICustomQueryInterface, ISOSDacInterface, ISOSDacInterface2, ISOSDacInterface3, ISOSDacInterface4, ISOSDacInterface5, ISOSDacInterface6, ISOSDacInterface7, ISOSDacInterface8, ISOSDacInterface9, ISOSDacInterface10, ISOSDacInterface11, ISOSDacInterface12, ISOSDacInterface13, ISOSDacInterface14, ISOSDacInterface15, ISOSDacInterface16, ISOSDacInterface17, IXCLRDataProcess, IXCLRDataProcess2, IXCLRDataProcess3, ICLRDataEnumMemoryRegions
{
    private readonly ISOSDacInterface? _cdacImpl;
    private readonly ISOSDacInterface? _legacyImpl;
    private readonly ISOSDacInterface2? _cdacImpl2;
    private readonly ISOSDacInterface2? _legacyImpl2;
    private readonly ISOSDacInterface3? _cdacImpl3;
    private readonly ISOSDacInterface3? _legacyImpl3;
    private readonly ISOSDacInterface4? _cdacImpl4;
    private readonly ISOSDacInterface4? _legacyImpl4;
    private readonly ISOSDacInterface5? _cdacImpl5;
    private readonly ISOSDacInterface5? _legacyImpl5;
    private readonly ISOSDacInterface6? _cdacImpl6;
    private readonly ISOSDacInterface6? _legacyImpl6;
    private readonly ISOSDacInterface7? _cdacImpl7;
    private readonly ISOSDacInterface7? _legacyImpl7;
    private readonly ISOSDacInterface8? _cdacImpl8;
    private readonly ISOSDacInterface8? _legacyImpl8;
    private readonly ISOSDacInterface9? _cdacImpl9;
    private readonly ISOSDacInterface9? _legacyImpl9;
    private readonly ISOSDacInterface10? _cdacImpl10;
    private readonly ISOSDacInterface10? _legacyImpl10;
    private readonly ISOSDacInterface11? _cdacImpl11;
    private readonly ISOSDacInterface11? _legacyImpl11;
    private readonly ISOSDacInterface12? _cdacImpl12;
    private readonly ISOSDacInterface12? _legacyImpl12;
    private readonly ISOSDacInterface13? _cdacImpl13;
    private readonly ISOSDacInterface13? _legacyImpl13;
    private readonly ISOSDacInterface14? _cdacImpl14;
    private readonly ISOSDacInterface14? _legacyImpl14;
    private readonly ISOSDacInterface15? _cdacImpl15;
    private readonly ISOSDacInterface15? _legacyImpl15;
    private readonly ISOSDacInterface16? _cdacImpl16;
    private readonly ISOSDacInterface16? _legacyImpl16;
    private readonly ISOSDacInterface17? _cdacImpl17;
    private readonly ISOSDacInterface17? _legacyImpl17;
    private readonly IXCLRDataProcess? _cdacProcess;
    private readonly IXCLRDataProcess? _legacyProcess;
    private readonly IXCLRDataProcess2? _cdacProcess2;
    private readonly IXCLRDataProcess2? _legacyProcess2;
    private readonly IXCLRDataProcess3? _cdacProcess3;
    private readonly IXCLRDataProcess3? _legacyProcess3;
    private readonly ICLRDataEnumMemoryRegions? _cdacEnumMemory;
    private readonly ICLRDataEnumMemoryRegions? _legacyEnumMemory;

    internal SOSDacImplProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as ISOSDacInterface;
        _legacyImpl = dacObject as ISOSDacInterface;
        _cdacImpl2 = cdacObject as ISOSDacInterface2;
        _legacyImpl2 = dacObject as ISOSDacInterface2;
        _cdacImpl3 = cdacObject as ISOSDacInterface3;
        _legacyImpl3 = dacObject as ISOSDacInterface3;
        _cdacImpl4 = cdacObject as ISOSDacInterface4;
        _legacyImpl4 = dacObject as ISOSDacInterface4;
        _cdacImpl5 = cdacObject as ISOSDacInterface5;
        _legacyImpl5 = dacObject as ISOSDacInterface5;
        _cdacImpl6 = cdacObject as ISOSDacInterface6;
        _legacyImpl6 = dacObject as ISOSDacInterface6;
        _cdacImpl7 = cdacObject as ISOSDacInterface7;
        _legacyImpl7 = dacObject as ISOSDacInterface7;
        _cdacImpl8 = cdacObject as ISOSDacInterface8;
        _legacyImpl8 = dacObject as ISOSDacInterface8;
        _cdacImpl9 = cdacObject as ISOSDacInterface9;
        _legacyImpl9 = dacObject as ISOSDacInterface9;
        _cdacImpl10 = cdacObject as ISOSDacInterface10;
        _legacyImpl10 = dacObject as ISOSDacInterface10;
        _cdacImpl11 = cdacObject as ISOSDacInterface11;
        _legacyImpl11 = dacObject as ISOSDacInterface11;
        _cdacImpl12 = cdacObject as ISOSDacInterface12;
        _legacyImpl12 = dacObject as ISOSDacInterface12;
        _cdacImpl13 = cdacObject as ISOSDacInterface13;
        _legacyImpl13 = dacObject as ISOSDacInterface13;
        _cdacImpl14 = cdacObject as ISOSDacInterface14;
        _legacyImpl14 = dacObject as ISOSDacInterface14;
        _cdacImpl15 = cdacObject as ISOSDacInterface15;
        _legacyImpl15 = dacObject as ISOSDacInterface15;
        _cdacImpl16 = cdacObject as ISOSDacInterface16;
        _legacyImpl16 = dacObject as ISOSDacInterface16;
        _cdacImpl17 = cdacObject as ISOSDacInterface17;
        _legacyImpl17 = dacObject as ISOSDacInterface17;
        _cdacProcess = cdacObject as IXCLRDataProcess;
        _legacyProcess = dacObject as IXCLRDataProcess;
        _cdacProcess2 = cdacObject as IXCLRDataProcess2;
        _legacyProcess2 = dacObject as IXCLRDataProcess2;
        _cdacProcess3 = cdacObject as IXCLRDataProcess3;
        _legacyProcess3 = dacObject as IXCLRDataProcess3;
        _cdacEnumMemory = cdacObject as ICLRDataEnumMemoryRegions;
        _legacyEnumMemory = dacObject as ICLRDataEnumMemoryRegions;
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

        if (iid == typeof(ISOSDacInterface).GUID)
            return Support(_cdacImpl, _legacyImpl);
        if (iid == typeof(ISOSDacInterface2).GUID)
            return Support(_cdacImpl2, _legacyImpl2);
        if (iid == typeof(ISOSDacInterface3).GUID)
            return Support(_cdacImpl3, _legacyImpl3);
        if (iid == typeof(ISOSDacInterface4).GUID)
            return Support(_cdacImpl4, _legacyImpl4);
        if (iid == typeof(ISOSDacInterface5).GUID)
            return Support(_cdacImpl5, _legacyImpl5);
        if (iid == typeof(ISOSDacInterface6).GUID)
            return Support(_cdacImpl6, _legacyImpl6);
        if (iid == typeof(ISOSDacInterface7).GUID)
            return Support(_cdacImpl7, _legacyImpl7);
        if (iid == typeof(ISOSDacInterface8).GUID)
            return Support(_cdacImpl8, _legacyImpl8);
        if (iid == typeof(ISOSDacInterface9).GUID)
            return Support(_cdacImpl9, _legacyImpl9);
        if (iid == typeof(ISOSDacInterface10).GUID)
            return Support(_cdacImpl10, _legacyImpl10);
        if (iid == typeof(ISOSDacInterface11).GUID)
            return Support(_cdacImpl11, _legacyImpl11);
        if (iid == typeof(ISOSDacInterface12).GUID)
            return Support(_cdacImpl12, _legacyImpl12);
        if (iid == typeof(ISOSDacInterface13).GUID)
            return Support(_cdacImpl13, _legacyImpl13);
        if (iid == typeof(ISOSDacInterface14).GUID)
            return Support(_cdacImpl14, _legacyImpl14);
        if (iid == typeof(ISOSDacInterface15).GUID)
            return Support(_cdacImpl15, _legacyImpl15);
        if (iid == typeof(ISOSDacInterface16).GUID)
            return Support(_cdacImpl16, _legacyImpl16);
        if (iid == typeof(ISOSDacInterface17).GUID)
            return Support(_cdacImpl17, _legacyImpl17);
        if (iid == typeof(IXCLRDataProcess).GUID)
            return Support(_cdacProcess, _legacyProcess);
        if (iid == typeof(IXCLRDataProcess2).GUID)
            return Support(_cdacProcess2, _legacyProcess2);
        if (iid == typeof(IXCLRDataProcess3).GUID)
            return Support(_cdacProcess3, _legacyProcess3);
        if (iid == typeof(ICLRDataEnumMemoryRegions).GUID)
            return Support(_cdacEnumMemory, _legacyEnumMemory);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region ISOSDacInterface
    int ISOSDacInterface.GetThreadStoreData(DacpThreadStoreData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetThreadStoreData(data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpThreadStoreData dataLocal;
            int hrLocal = _legacyImpl.GetThreadStoreData(&dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->threadCount == dataLocal.threadCount);
                Debug.Assert(data->firstThread == dataLocal.firstThread);
                Debug.Assert(data->finalizerThread == dataLocal.finalizerThread);
                Debug.Assert(data->gcThread == dataLocal.gcThread);
                Debug.Assert(data->unstartedThreadCount == dataLocal.unstartedThreadCount);
                Debug.Assert(data->backgroundThreadCount == dataLocal.backgroundThreadCount);
                Debug.Assert(data->pendingThreadCount == dataLocal.pendingThreadCount);
                Debug.Assert(data->deadThreadCount == dataLocal.deadThreadCount);
                Debug.Assert(data->fHostConfig == dataLocal.fHostConfig);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetAppDomainStoreData(void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAppDomainStoreData(data) : HResults.E_NOTIMPL;
#if DEBUG
        {
            if (_legacyImpl is not null)
            {
                DacpAppDomainStoreData* appDomainStoreData = (DacpAppDomainStoreData*)data;
                DacpAppDomainStoreData dataLocal = default;
                int hrLocal = _legacyImpl.GetAppDomainStoreData(&dataLocal);
                Debug.ValidateHResult(hr, hrLocal);
                Debug.Assert(appDomainStoreData->sharedDomain == dataLocal.sharedDomain, $"cDAC: {appDomainStoreData->sharedDomain:x}, DAC: {dataLocal.sharedDomain:x}");
                Debug.Assert(appDomainStoreData->systemDomain == dataLocal.systemDomain, $"cDAC: {appDomainStoreData->systemDomain:x}, DAC: {dataLocal.systemDomain:x}");
                Debug.Assert(appDomainStoreData->DomainCount == dataLocal.DomainCount, $"cDAC: {appDomainStoreData->DomainCount}, DAC: {dataLocal.DomainCount}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetAppDomainList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[] values, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAppDomainList(count, values, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[] valuesLocal = new ClrDataAddress[count];
            uint neededLocal;
            int hrLocal = _legacyImpl.GetAppDomainList(count, valuesLocal, &neededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
            if (values is not null && values.Length > 0 && valuesLocal.Length > 0)
            {
                Debug.Assert(values[0] == valuesLocal[0], $"cDAC: {values[0]:x}, DAC: {valuesLocal[0]:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetAppDomainData(ClrDataAddress addr, DacpAppDomainData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAppDomainData(addr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpAppDomainData dataLocal = default;
            int hrLocal = _legacyImpl.GetAppDomainData(addr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->AppDomainPtr == dataLocal.AppDomainPtr);
                Debug.Assert(data->pHighFrequencyHeap == dataLocal.pHighFrequencyHeap);
                Debug.Assert(data->pLowFrequencyHeap == dataLocal.pLowFrequencyHeap);
                Debug.Assert(data->pStubHeap == dataLocal.pStubHeap);
                Debug.Assert(data->DomainLocalBlock == dataLocal.DomainLocalBlock);
                Debug.Assert(data->pDomainLocalModules == dataLocal.pDomainLocalModules);
                Debug.Assert(data->dwId == dataLocal.dwId);
                Debug.Assert(data->appDomainStage == dataLocal.appDomainStage);
                Debug.Assert(data->AssemblyCount == dataLocal.AssemblyCount);
                Debug.Assert(data->FailedAssemblyCount == dataLocal.FailedAssemblyCount);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetAppDomainName(ClrDataAddress addr, uint count, char* name, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAppDomainName(addr, count, name, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint neededLocal;
            char[] nameLocal = new char[count];
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyImpl.GetAppDomainName(addr, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(name)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetDomainFromContext(ClrDataAddress context, ClrDataAddress* domain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetDomainFromContext(context, domain) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress domainLocal;
            int hrLocal = _legacyImpl.GetDomainFromContext(context, &domainLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(domainLocal == context, $"cDAC: {context:x}, DAC: {domainLocal:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetAssemblyList(ClrDataAddress addr, int count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[]? values, int* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAssemblyList(addr, count, values, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[]? valuesLocal = values != null ? new ClrDataAddress[count] : null;
            int neededLocal;
            int hrLocal = _legacyImpl.GetAssemblyList(addr, count, valuesLocal, &neededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                if (values is not null)
                {
                    // in theory, these don't need to be in the same order, but for consistency it is
                    // easiest for consumers and verification if the DAC and cDAC return the same order
                    for (int i = 0; i < neededLocal; i++)
                    {
                        Debug.Assert(values[i] == valuesLocal![i], $"cDAC: {values[i]:x}, DAC: {valuesLocal[i]:x}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetAssemblyData(ClrDataAddress domain, ClrDataAddress assembly, DacpAssemblyData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAssemblyData(domain, assembly, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpAssemblyData dataLocal = default;
            int hrLocal = _legacyImpl.GetAssemblyData(domain, assembly, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->AssemblyPtr == dataLocal.AssemblyPtr, $"cDAC: {data->AssemblyPtr:x}, DAC: {dataLocal.AssemblyPtr:x}");
                Debug.Assert(data->ClassLoader == dataLocal.ClassLoader, $"cDAC: {data->ClassLoader:x}, DAC: {dataLocal.ClassLoader:x}");
                Debug.Assert(data->ParentDomain == dataLocal.ParentDomain, $"cDAC: {data->ParentDomain:x}, DAC: {dataLocal.ParentDomain:x}");
                Debug.Assert(data->DomainPtr == dataLocal.DomainPtr, $"cDAC: {data->DomainPtr:x}, DAC: {dataLocal.DomainPtr:x}");
                Debug.Assert(data->AssemblySecDesc == dataLocal.AssemblySecDesc, $"cDAC: {data->AssemblySecDesc:x}, DAC: {dataLocal.AssemblySecDesc:x}");
                Debug.Assert(data->isDynamic == dataLocal.isDynamic, $"cDAC: {data->isDynamic}, DAC: {dataLocal.isDynamic}");
                Debug.Assert(data->ModuleCount == dataLocal.ModuleCount, $"cDAC: {data->ModuleCount}, DAC: {dataLocal.ModuleCount}");
                Debug.Assert(data->LoadContext == dataLocal.LoadContext, $"cDAC: {data->LoadContext:x}, DAC: {dataLocal.LoadContext:x}");
                Debug.Assert(data->isDomainNeutral == dataLocal.isDomainNeutral, $"cDAC: {data->isDomainNeutral}, DAC: {dataLocal.isDomainNeutral}");
                Debug.Assert(data->dwLocationFlags == dataLocal.dwLocationFlags, $"cDAC: {data->dwLocationFlags:x}, DAC: {dataLocal.dwLocationFlags:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetAssemblyName(ClrDataAddress assembly, uint count, char* name, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAssemblyName(assembly, count, name, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] fileNameLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = fileNameLocal)
            {
                hrLocal = _legacyImpl.GetAssemblyName(assembly, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(name == null || new ReadOnlySpan<char>(fileNameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(name)));
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetModule(ClrDataAddress addr, DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> modDac = new(mod.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetModule(addr, modCDac) : HResults.E_NOTIMPL;
        if (_legacyImpl is not null)
        {
            _legacyImpl.GetModule(addr, modDac);
        }
        if (!mod.IsNullRef)
            mod.Interface = ShimProxy.PairIXCLRDataModule(_session, modCDac.Interface, modDac.Interface);
        return hr;
    }

    int ISOSDacInterface.GetModuleData(ClrDataAddress moduleAddr, DacpModuleData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetModuleData(moduleAddr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpModuleData dataLocal;
            int hrLocal = _legacyImpl.GetModuleData(moduleAddr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->Address == dataLocal.Address);
                Debug.Assert(data->PEAssembly == dataLocal.PEAssembly);
                Debug.Assert(data->Assembly == dataLocal.Assembly);
                Debug.Assert(data->isReflection == dataLocal.isReflection);
                Debug.Assert(data->isPEFile == dataLocal.isPEFile);
                Debug.Assert(data->dwTransientFlags == dataLocal.dwTransientFlags);
                Debug.Assert(data->ilBase == dataLocal.ilBase);
                Debug.Assert(data->metadataStart == dataLocal.metadataStart);
                Debug.Assert(data->metadataSize == dataLocal.metadataSize);
                Debug.Assert(data->LoaderAllocator == dataLocal.LoaderAllocator);
                Debug.Assert(data->ThunkHeap == dataLocal.ThunkHeap);
                Debug.Assert(data->FieldDefToDescMap == dataLocal.FieldDefToDescMap);
                Debug.Assert(data->ManifestModuleReferencesMap == dataLocal.ManifestModuleReferencesMap);
                Debug.Assert(data->MemberRefToDescMap == dataLocal.MemberRefToDescMap);
                Debug.Assert(data->MethodDefToDescMap == dataLocal.MethodDefToDescMap);
                Debug.Assert(data->TypeDefToMethodTableMap == dataLocal.TypeDefToMethodTableMap);
                Debug.Assert(data->TypeRefToMethodTableMap == dataLocal.TypeRefToMethodTableMap);
                Debug.Assert(data->dwModuleID == dataLocal.dwModuleID);
                Debug.Assert(data->dwBaseClassIndex == dataLocal.dwBaseClassIndex);
                Debug.Assert(data->dwModuleIndex == dataLocal.dwModuleIndex);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.TraverseModuleMap(ModuleMapType mmt, ClrDataAddress moduleAddr, delegate* unmanaged<uint, ulong, void*, void> pCallback, void* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        TraverseModuleMapRecordingContext? recordingContext = null;
        GCHandle recordingHandle = default;
        delegate* unmanaged<uint, ulong, void*, void> cdacCallback = pCallback;
        void* cdacToken = token;
        if (pCallback is not null)
        {
            recordingContext = new TraverseModuleMapRecordingContext { Callback = pCallback, Token = token };
            recordingHandle = GCHandle.Alloc(recordingContext);
            cdacCallback = &RecordingTraverseModuleMapCallback;
            cdacToken = GCHandle.ToIntPtr(recordingHandle).ToPointer();
        }
        int hr;
        try
        {
            hr = _cdacImpl is not null ? _cdacImpl.TraverseModuleMap(mmt, moduleAddr, cdacCallback, cdacToken) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (recordingHandle.IsAllocated)
                recordingHandle.Free();
        }
        if (_legacyImpl is not null)
        {
            Dictionary<ulong, uint> expectedElements = recordingContext?.ExpectedElements ?? [];
            uint expectedCount = (uint)expectedElements.Count;
            expectedElements[default] = 0;
            GCHandle expectedHandle = GCHandle.Alloc(expectedElements);
            try
            {
                void* tokenDebug = GCHandle.ToIntPtr(expectedHandle).ToPointer();
                delegate* unmanaged<uint, ulong, void*, void> callbackDebugPtr = &TraverseModuleMapCallback;
                int hrLocal = _legacyImpl.TraverseModuleMap(mmt, moduleAddr, callbackDebugPtr, tokenDebug);
                Debug.ValidateHResult(hr, hrLocal);
                Debug.Assert(expectedElements[default] == expectedCount, $"cDAC: {expectedCount} elements, DAC: {expectedElements[default]} elements");
            }
            finally
            {
                expectedHandle.Free();
            }
        }
#else
        int hr = _cdacImpl is not null ? _cdacImpl.TraverseModuleMap(mmt, moduleAddr, pCallback, token) : HResults.E_NOTIMPL;
#endif
        return hr;
    }
    int ISOSDacInterface.GetAssemblyModuleList(ClrDataAddress assembly, uint count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[]? modules, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAssemblyModuleList(assembly, count, modules!, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[] modulesLocal = new ClrDataAddress[(int)count];
            uint neededLocal;
            int hrLocal = _legacyImpl.GetAssemblyModuleList(assembly, count, modulesLocal, &neededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                if (modules is not null && modules.Length > 0)
                {
                    Debug.Assert(modules[0] == modulesLocal[0], $"cDAC: {modules[0]:x}, DAC: {modulesLocal[0]:x}");
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetILForModule(ClrDataAddress moduleAddr, int rva, ClrDataAddress* il)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetILForModule(moduleAddr, rva, il) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress ilLocal;
            int hrLocal = _legacyImpl.GetILForModule(moduleAddr, rva, &ilLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*il == ilLocal, $"cDAC: {*il:x}, DAC: {ilLocal:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetThreadData(ClrDataAddress thread, DacpThreadData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetThreadData(thread, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpThreadData dataLocal;
            int hrLocal = _legacyImpl.GetThreadData(thread, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->corThreadId == dataLocal.corThreadId, $"cDAC: {data->corThreadId}, DAC: {dataLocal.corThreadId}");
                Debug.Assert(data->osThreadId == dataLocal.osThreadId, $"cDAC: {data->osThreadId}, DAC: {dataLocal.osThreadId}");
                // The cDAC exposes only the subset of Thread::m_State bits wrapped by the
                // ThreadState contract enum; mask the legacy raw state the same way before comparing.
                // The shim has no reference to the contract assemblies, so the mask (the OR of every
                // value of Microsoft.Diagnostics.DataContractReader.Contracts.ThreadState in
                // Abstractions/Contracts/IThread.cs) is reproduced here and must stay in sync with it.
                const int wrappedStateMask = unchecked((int)0x8319E68E);
                Debug.Assert(data->state == (dataLocal.state & wrappedStateMask), $"cDAC: {data->state}, DAC: {dataLocal.state & wrappedStateMask}");
                Debug.Assert(data->preemptiveGCDisabled == dataLocal.preemptiveGCDisabled, $"cDAC: {data->preemptiveGCDisabled}, DAC: {dataLocal.preemptiveGCDisabled}");
                Debug.Assert(data->allocContextPtr == dataLocal.allocContextPtr, $"cDAC: {data->allocContextPtr:x}, DAC: {dataLocal.allocContextPtr:x}");
                Debug.Assert(data->allocContextLimit == dataLocal.allocContextLimit, $"cDAC: {data->allocContextLimit:x}, DAC: {dataLocal.allocContextLimit:x}");
                Debug.Assert(data->fiberData == dataLocal.fiberData, $"cDAC: {data->fiberData:x}, DAC: {dataLocal.fiberData:x}");
                Debug.Assert(data->context == dataLocal.context, $"cDAC: {data->context:x}, DAC: {dataLocal.context:x}");
                Debug.Assert(data->domain == dataLocal.domain, $"cDAC: {data->domain:x}, DAC: {dataLocal.domain:x}");
                Debug.Assert(data->lockCount == dataLocal.lockCount, $"cDAC: {data->lockCount}, DAC: {dataLocal.lockCount}");
                Debug.Assert(data->pFrame == dataLocal.pFrame, $"cDAC: {data->pFrame:x}, DAC: {dataLocal.pFrame:x}");
                Debug.Assert(data->firstNestedException == dataLocal.firstNestedException, $"cDAC: {data->firstNestedException:x}, DAC: {dataLocal.firstNestedException:x}");
                Debug.Assert(data->lastThrownObjectHandle == dataLocal.lastThrownObjectHandle, $"cDAC: {data->lastThrownObjectHandle:x}, DAC: {dataLocal.lastThrownObjectHandle:x}");
                Debug.Assert(data->nextThread == dataLocal.nextThread, $"cDAC: {data->nextThread:x}, DAC: {dataLocal.nextThread:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetThreadFromThinlockID(uint thinLockId, ClrDataAddress* pThread)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetThreadFromThinlockID(thinLockId, pThread) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress pThreadLocal;
            int hrLocal = _legacyImpl.GetThreadFromThinlockID(thinLockId, &pThreadLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pThread == pThreadLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetStackLimits(ClrDataAddress threadPtr, ClrDataAddress* lower, ClrDataAddress* upper, ClrDataAddress* fp)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetStackLimits(threadPtr, lower, upper, fp) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress lowerLocal, upperLocal, fpLocal;
            int hrLocal = _legacyImpl.GetStackLimits(threadPtr, &lowerLocal, &upperLocal, &fpLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(lower == null || *lower == lowerLocal, $"cDAC: {*lower:x}, DAC: {lowerLocal:x}");
                Debug.Assert(upper == null || *upper == upperLocal, $"cDAC: {*upper:x}, DAC: {upperLocal:x}");
                Debug.Assert(fp == null || *fp == fpLocal, $"cDAC: {*fp:x}, DAC: {fpLocal:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetMethodDescData(ClrDataAddress addr, ClrDataAddress ip, DacpMethodDescData* data, uint cRevertedRejitVersions, DacpReJitData* rgRevertedRejitData, uint* pcNeededRevertedRejitData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodDescData(addr, ip, data, cRevertedRejitVersions, rgRevertedRejitData, pcNeededRevertedRejitData) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpMethodDescData dataLocal = default;
            DacpReJitData[]? rgRevertedRejitDataLocal = null;
            if (rgRevertedRejitData != null)
            {
                rgRevertedRejitDataLocal = new DacpReJitData[cRevertedRejitVersions];
            }
            uint cNeededRevertedRejitDataLocal = 0;
            uint* pcNeededRevertedRejitDataLocal = null;
            if (pcNeededRevertedRejitData != null)
            {
                pcNeededRevertedRejitDataLocal = &cNeededRevertedRejitDataLocal;
            }
            int hrLocal;
            fixed (DacpReJitData* rgRevertedRejitDataLocalPtr = rgRevertedRejitDataLocal)
            {
                hrLocal = _legacyImpl.GetMethodDescData(addr, ip, &dataLocal, cRevertedRejitVersions, rgRevertedRejitDataLocalPtr, pcNeededRevertedRejitDataLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->bHasNativeCode == dataLocal.bHasNativeCode, $"cDAC: {data->bHasNativeCode}, DAC: {dataLocal.bHasNativeCode}");
                Debug.Assert(data->bIsDynamic == dataLocal.bIsDynamic, $"cDAC: {data->bIsDynamic}, DAC: {dataLocal.bIsDynamic}");
                Debug.Assert(data->wSlotNumber == dataLocal.wSlotNumber, $"cDAC: {data->wSlotNumber}, DAC: {dataLocal.wSlotNumber}");
                Debug.Assert(data->NativeCodeAddr == dataLocal.NativeCodeAddr, $"cDAC: {data->NativeCodeAddr:x}, DAC: {dataLocal.NativeCodeAddr:x}");
                Debug.Assert(data->AddressOfNativeCodeSlot == dataLocal.AddressOfNativeCodeSlot, $"cDAC: {data->AddressOfNativeCodeSlot:x}, DAC: {dataLocal.AddressOfNativeCodeSlot:x}");
                Debug.Assert(data->MethodDescPtr == dataLocal.MethodDescPtr, $"cDAC: {data->MethodDescPtr:x}, DAC: {dataLocal.MethodDescPtr:x}");
                Debug.Assert(data->MethodTablePtr == dataLocal.MethodTablePtr, $"cDAC: {data->MethodTablePtr:x}, DAC: {dataLocal.MethodTablePtr:x}");
                Debug.Assert(data->ModulePtr == dataLocal.ModulePtr, $"cDAC: {data->ModulePtr:x}, DAC: {dataLocal.ModulePtr:x}");
                Debug.Assert(data->MDToken == dataLocal.MDToken, $"cDAC: {data->MDToken:x}, DAC: {dataLocal.MDToken:x}");
                Debug.Assert(data->GCInfo == dataLocal.GCInfo, $"cDAC: {data->GCInfo:x}, DAC: {dataLocal.GCInfo:x}");
                Debug.Assert(data->GCStressCodeCopy == dataLocal.GCStressCodeCopy, $"cDAC: {data->GCStressCodeCopy:x}, DAC: {dataLocal.GCStressCodeCopy:x}");
                // managedDynamicMethodObject is not currently populated by the cDAC API and may differ from legacyImpl.
                Debug.Assert(data->managedDynamicMethodObject == 0);
                Debug.Assert(data->requestedIP == dataLocal.requestedIP, $"cDAC: {data->requestedIP:x}, DAC: {dataLocal.requestedIP:x}");
                Debug.Assert(data->cJittedRejitVersions == dataLocal.cJittedRejitVersions, $"cDAC: {data->cJittedRejitVersions}, DAC: {dataLocal.cJittedRejitVersions}");
                Debug.Assert(data->rejitDataCurrent.rejitID == dataLocal.rejitDataCurrent.rejitID, $"cDAC: {data->rejitDataCurrent.rejitID}, DAC: {dataLocal.rejitDataCurrent.rejitID}");
                Debug.Assert(data->rejitDataCurrent.NativeCodeAddr == dataLocal.rejitDataCurrent.NativeCodeAddr, $"cDAC: {data->rejitDataCurrent.NativeCodeAddr:x}, DAC: {dataLocal.rejitDataCurrent.NativeCodeAddr:x}");
                Debug.Assert(data->rejitDataCurrent.flags == dataLocal.rejitDataCurrent.flags, $"cDAC: {data->rejitDataCurrent.flags}, DAC: {dataLocal.rejitDataCurrent.flags}");
                Debug.Assert(data->rejitDataRequested.rejitID == dataLocal.rejitDataRequested.rejitID, $"cDAC: {data->rejitDataRequested.rejitID}, DAC: {dataLocal.rejitDataRequested.rejitID}");
                Debug.Assert(data->rejitDataRequested.NativeCodeAddr == dataLocal.rejitDataRequested.NativeCodeAddr, $"cDAC: {data->rejitDataRequested.NativeCodeAddr:x}, DAC: {dataLocal.rejitDataRequested.NativeCodeAddr:x}");
                Debug.Assert(data->rejitDataRequested.flags == dataLocal.rejitDataRequested.flags, $"cDAC: {data->rejitDataRequested.flags}, DAC: {dataLocal.rejitDataRequested.flags}");
                if (rgRevertedRejitData != null && rgRevertedRejitDataLocal != null)
                {
                    Debug.Assert(cNeededRevertedRejitDataLocal == *pcNeededRevertedRejitData, $"cDAC: {*pcNeededRevertedRejitData}, DAC: {cNeededRevertedRejitDataLocal}");
                    for (ulong i = 0; i < cNeededRevertedRejitDataLocal; i++)
                    {
                        Debug.Assert(rgRevertedRejitData[i].rejitID == rgRevertedRejitDataLocal[i].rejitID, $"cDAC: {rgRevertedRejitData[i].rejitID}, DAC: {rgRevertedRejitDataLocal[i].rejitID}");
                        Debug.Assert(rgRevertedRejitData[i].NativeCodeAddr == rgRevertedRejitDataLocal[i].NativeCodeAddr, $"cDAC: {rgRevertedRejitData[i].NativeCodeAddr:x}, DAC: {rgRevertedRejitDataLocal[i].NativeCodeAddr:x}");
                        Debug.Assert(rgRevertedRejitData[i].flags == rgRevertedRejitDataLocal[i].flags, $"cDAC: {rgRevertedRejitData[i].flags}, DAC: {rgRevertedRejitDataLocal[i].flags}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodDescPtrFromIP(ClrDataAddress ip, ClrDataAddress* ppMD)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodDescPtrFromIP(ip, ppMD) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress ppMDLocal;
            int hrLocal = _legacyImpl.GetMethodDescPtrFromIP(ip, &ppMDLocal);

            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*ppMD == ppMDLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodDescName(ClrDataAddress addr, uint count, char* name, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodDescName(addr, count, name, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] nameLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyImpl.GetMethodDescName(addr, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(name)), $"cDAC: {new string(name)}, DAC: {new string(nameLocal, 0, (int)neededLocal - 1)}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodDescPtrFromFrame(ClrDataAddress frameAddr, ClrDataAddress* ppMD)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodDescPtrFromFrame(frameAddr, ppMD) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress ppMDLocal;
            int hrLocal = _legacyImpl.GetMethodDescPtrFromFrame(frameAddr, &ppMDLocal);

            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*ppMD == ppMDLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodDescFromToken(ClrDataAddress moduleAddr, uint token, ClrDataAddress* methodDesc)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodDescFromToken(moduleAddr, token, methodDesc) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress methodDescLocal;
            int hrLocal = _legacyImpl.GetMethodDescFromToken(moduleAddr, token, &methodDescLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*methodDesc == methodDescLocal, $"cDAC: {*methodDesc:x}, DAC: {methodDescLocal:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodDescTransparencyData(ClrDataAddress methodDesc, DacpMethodDescTransparencyData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodDescTransparencyData(methodDesc, data) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface.GetCodeHeaderData(ClrDataAddress ip, DacpCodeHeaderData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCodeHeaderData(ip, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpCodeHeaderData dataLocal = default;
            int hrLocal = _legacyImpl.GetCodeHeaderData(ip, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->MethodDescPtr == dataLocal.MethodDescPtr, $"cDAC: {data->MethodDescPtr:x}, DAC: {dataLocal.MethodDescPtr:x}");
                Debug.Assert(data->JITType == dataLocal.JITType, $"cDAC: {data->JITType}, DAC: {dataLocal.JITType}");
                Debug.Assert(data->GCInfo == dataLocal.GCInfo, $"cDAC: {data->GCInfo:x}, DAC: {dataLocal.GCInfo:x}");
                Debug.Assert(data->MethodStart == dataLocal.MethodStart, $"cDAC: {data->MethodStart:x}, DAC: {dataLocal.MethodStart:x}");
                Debug.Assert(data->MethodSize == dataLocal.MethodSize, $"cDAC: {data->MethodSize}, DAC: {dataLocal.MethodSize}");
                Debug.Assert(data->HotRegionSize == dataLocal.HotRegionSize, $"cDAC: {data->HotRegionSize}, DAC: {dataLocal.HotRegionSize}");
                Debug.Assert(data->ColdRegionStart == dataLocal.ColdRegionStart, $"cDAC: {data->ColdRegionStart:x}, DAC: {dataLocal.ColdRegionStart:x}");
                Debug.Assert(data->ColdRegionSize == dataLocal.ColdRegionSize, $"cDAC: {data->ColdRegionSize}, DAC: {dataLocal.ColdRegionSize}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetJitManagerList(uint count, DacpJitManagerInfo* managers, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetJitManagerList(count, managers, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            if (managers is not null)
            {
                DacpJitManagerInfo managerLocal = default;
                int hrLocal = _legacyImpl.GetJitManagerList(count, &managerLocal, null);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK && count >= 1)
                {
                    Debug.Assert(managers->managerAddr == managerLocal.managerAddr);
                    Debug.Assert(managers->codeType == managerLocal.codeType);
                    Debug.Assert(managers->ptrHeapList == managerLocal.ptrHeapList);
                }
            }
            else
            {
                uint neededLocal;
                int hrLocal = _legacyImpl.GetJitManagerList(0, null, &neededLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK && pNeeded is not null)
                {
                    Debug.Assert(*pNeeded == neededLocal);
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetJitHelperFunctionName(ClrDataAddress ip, uint count, byte* name, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetJitHelperFunctionName(ip, count, name, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            byte[]? nameLocal = name != null && count > 0 ? new byte[count] : null;
            uint neededLocal;
            int hrLocal;
            fixed (byte* ptr = nameLocal)
            {
                hrLocal = _legacyImpl.GetJitHelperFunctionName(ip, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(name == null || new ReadOnlySpan<byte>(name, (int)neededLocal).SequenceEqual(nameLocal!.AsSpan(0, (int)neededLocal)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetJumpThunkTarget(void* ctx, ClrDataAddress* targetIP, ClrDataAddress* targetMD)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetJumpThunkTarget(ctx, targetIP, targetMD) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress targetIPLocal;
            ClrDataAddress targetMDLocal;
            int hrLocal = _legacyImpl.GetJumpThunkTarget(ctx, &targetIPLocal, &targetMDLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*targetIP == targetIPLocal, $"cDAC: {*targetIP:x}, DAC: {targetIPLocal:x}");
                Debug.Assert(*targetMD == targetMDLocal, $"cDAC: {*targetMD:x}, DAC: {targetMDLocal:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetThreadpoolData(void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetThreadpoolData(data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetThreadpoolData(data);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetWorkRequestData(ClrDataAddress addrWorkRequest, void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetWorkRequestData(addrWorkRequest, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetWorkRequestData(addrWorkRequest, data);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetHillClimbingLogEntry(ClrDataAddress addr, void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetHillClimbingLogEntry(addr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetHillClimbingLogEntry(addr, data);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetObjectData(ClrDataAddress objAddr, DacpObjectData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetObjectData(objAddr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpObjectData dataLocal;
            int hrLocal = _legacyImpl.GetObjectData(objAddr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->MethodTable == dataLocal.MethodTable);
                Debug.Assert(data->ObjectType == dataLocal.ObjectType);
                Debug.Assert(data->Size == dataLocal.Size);
                Debug.Assert(data->ElementTypeHandle == dataLocal.ElementTypeHandle);
                Debug.Assert(data->ElementType == dataLocal.ElementType);
                Debug.Assert(data->dwRank == dataLocal.dwRank);
                Debug.Assert(data->dwNumComponents == dataLocal.dwNumComponents);
                Debug.Assert(data->dwComponentSize == dataLocal.dwComponentSize);
                Debug.Assert(data->ArrayDataPtr == dataLocal.ArrayDataPtr);
                Debug.Assert(data->ArrayBoundsPtr == dataLocal.ArrayBoundsPtr);
                Debug.Assert(data->ArrayLowerBoundsPtr == dataLocal.ArrayLowerBoundsPtr);
                Debug.Assert(data->RCW == dataLocal.RCW);
                Debug.Assert(data->CCW == dataLocal.CCW);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetObjectStringData(ClrDataAddress obj, uint count, char* stringData, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetObjectStringData(obj, count, stringData, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] stringDataLocal = new char[count];
            uint neededLocal = 0;
            int hrLocal;
            fixed (char* ptr = stringDataLocal)
            {
                // Invoke the legacy DAC under the same argument contract the caller gave the cDAC:
                // only pass an output buffer when the caller did, and only request the size-out when
                // the caller did. This keeps the HRESULT comparison apples-to-apples.
                char* stringDataArg = stringData is null ? null : ptr;
                uint* pNeededArg = pNeeded is null ? null : &neededLocal;
                hrLocal = _legacyImpl.GetObjectStringData(obj, count, stringDataArg, pNeededArg);
            }

            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                // Compare against the legacy buffer using the cDAC string length: neededLocal is only
                // populated when a size-out was requested from the legacy DAC (mirroring the caller).
                Debug.Assert(stringData == null || new ReadOnlySpan<char>(stringDataLocal, 0, new string(stringData).Length).SequenceEqual(new string(stringData)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetObjectClassName(ClrDataAddress obj, uint count, char* className, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetObjectClassName(obj, count, className, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] classNameLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = classNameLocal)
            {
                hrLocal = _legacyImpl.GetObjectClassName(obj, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(className == null || new ReadOnlySpan<char>(classNameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(className)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodTableName(ClrDataAddress mt, uint count, char* mtName, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodTableName(mt, count, mtName, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] mtNameLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = mtNameLocal)
            {
                hrLocal = _legacyImpl.GetMethodTableName(mt, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(mtName == null || new ReadOnlySpan<char>(mtNameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(mtName)));
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetMethodTableData(ClrDataAddress mt, DacpMethodTableData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodTableData(mt, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpMethodTableData dataLocal;
            int hrLocal = _legacyImpl.GetMethodTableData(mt, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->module == dataLocal.module);
                Debug.Assert(data->klass == dataLocal.klass);
                Debug.Assert(data->parentMethodTable == dataLocal.parentMethodTable);
                Debug.Assert(data->wNumInterfaces == dataLocal.wNumInterfaces);
                Debug.Assert(data->wNumMethods == dataLocal.wNumMethods);
                Debug.Assert(data->wNumVtableSlots == dataLocal.wNumVtableSlots);
                Debug.Assert(data->wNumVirtuals == dataLocal.wNumVirtuals);
                Debug.Assert(data->cl == dataLocal.cl);
                Debug.Assert(data->dwAttrClass == dataLocal.dwAttrClass);
                Debug.Assert(data->bContainsGCPointers == dataLocal.bContainsGCPointers);
                Debug.Assert(data->bIsShared == dataLocal.bIsShared);
                Debug.Assert(data->bIsDynamic == dataLocal.bIsDynamic);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodTableSlot(ClrDataAddress mt, uint slot, ClrDataAddress* value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodTableSlot(mt, slot, value) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal;
            ClrDataAddress valueLocal;

            hrLocal = _legacyImpl.GetMethodTableSlot(mt, slot, &valueLocal);

            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*value == valueLocal, $"cDAC: {*value:x}, DAC: {valueLocal:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodTableFieldData(ClrDataAddress mt, DacpMethodTableFieldData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodTableFieldData(mt, data) : HResults.E_NOTIMPL;
#if DEBUG
        {
            if (_legacyImpl is not null)
            {
                DacpMethodTableFieldData mtFieldDataLocal = default;
                int hrLocal = _legacyImpl.GetMethodTableFieldData(mt, &mtFieldDataLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    Debug.Assert(data->wNumInstanceFields == mtFieldDataLocal.wNumInstanceFields);
                    Debug.Assert(data->wNumStaticFields == mtFieldDataLocal.wNumStaticFields);
                    Debug.Assert(data->wNumThreadStaticFields == mtFieldDataLocal.wNumThreadStaticFields);
                    Debug.Assert(data->wContextStaticOffset == mtFieldDataLocal.wContextStaticOffset);
                    Debug.Assert(data->wContextStaticsSize == mtFieldDataLocal.wContextStaticsSize);
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodTableTransparencyData(ClrDataAddress mt, DacpMethodTableTransparencyData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodTableTransparencyData(mt, data) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface.GetMethodTableForEEClass(ClrDataAddress eeClassReallyCanonMT, ClrDataAddress* value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetMethodTableForEEClass(eeClassReallyCanonMT, value) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress valueLocal;
            int hrLocal = _legacyImpl.GetMethodTableForEEClass(eeClassReallyCanonMT, &valueLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*value == valueLocal);
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetFieldDescData(ClrDataAddress fieldDesc, DacpFieldDescData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFieldDescData(fieldDesc, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpFieldDescData dataLocal = default;
            int hrLocal = _legacyImpl.GetFieldDescData(fieldDesc, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->Type == dataLocal.Type, $"cDAC: {data->Type}, DAC: {dataLocal.Type}");
                Debug.Assert(data->sigType == dataLocal.sigType, $"cDAC: {data->sigType}, DAC: {dataLocal.sigType}");
                Debug.Assert(data->TokenOfType == dataLocal.TokenOfType, $"cDAC: {data->TokenOfType:x}, DAC: {dataLocal.TokenOfType:x}");
                Debug.Assert(data->MTOfType == dataLocal.MTOfType, $"cDAC: {data->MTOfType:x}, DAC: {dataLocal.MTOfType:x}");
                Debug.Assert(data->ModuleOfType == dataLocal.ModuleOfType, $"cDAC: {data->ModuleOfType:x}, DAC: {dataLocal.ModuleOfType:x}");
                Debug.Assert(data->mb == dataLocal.mb, $"cDAC: {data->mb:x}, DAC: {dataLocal.mb:x}");
                Debug.Assert(data->MTOfEnclosingClass == dataLocal.MTOfEnclosingClass, $"cDAC: {data->MTOfEnclosingClass:x}, DAC: {dataLocal.MTOfEnclosingClass:x}");
                Debug.Assert(data->dwOffset == dataLocal.dwOffset, $"cDAC: {data->dwOffset:x}, DAC: {dataLocal.dwOffset:x}");
                Debug.Assert(data->bIsThreadLocal == dataLocal.bIsThreadLocal, $"cDAC: {data->bIsThreadLocal}, DAC: {dataLocal.bIsThreadLocal}");
                Debug.Assert(data->bIsContextLocal == dataLocal.bIsContextLocal, $"cDAC: {data->bIsContextLocal}, DAC: {dataLocal.bIsContextLocal}");
                Debug.Assert(data->bIsStatic == dataLocal.bIsStatic, $"cDAC: {data->bIsStatic}, DAC: {dataLocal.bIsStatic}");
                // For the last field in a type, the legacy DAC returns a pointer one element past the end of
                // the FieldDesc array (not a valid FieldDesc), whereas the cDAC's TryGetFieldDescNext reports
                // no next field, which we surface as 0. Tolerate that intentional difference.
                Debug.Assert(data->NextField == dataLocal.NextField || data->NextField == 0, $"cDAC: {data->NextField:x}, DAC: {dataLocal.NextField:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetFrameName(ClrDataAddress vtable, uint count, char* frameName, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFrameName(vtable, count, frameName, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] nameLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyImpl.GetFrameName(vtable, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(frameName == null || new ReadOnlySpan<char>(nameLocal, 0, (int)neededLocal).SequenceEqual(new string(frameName)),
                    $"cDAC: {new string(frameName)}, DAC: {new string(nameLocal, 0, (int)neededLocal)}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetPEFileBase(ClrDataAddress addr, ClrDataAddress* peBase)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetPEFileBase(addr, peBase) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress peBaseLocal;
            int hrLocal = _legacyImpl.GetPEFileBase(addr, &peBaseLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*peBase == peBaseLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetPEFileName(ClrDataAddress addr, uint count, char* fileName, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetPEFileName(addr, count, fileName, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] fileNameLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = fileNameLocal)
            {
                hrLocal = _legacyImpl.GetPEFileName(addr, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(fileName == null || new ReadOnlySpan<char>(fileNameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(fileName)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetGCHeapData(DacpGcHeapData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetGCHeapData(data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapData dataLocal = default;
            int hrLocal = _legacyImpl.GetGCHeapData(&dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->bServerMode == dataLocal.bServerMode, $"cDAC: {data->bServerMode}, DAC: {dataLocal.bServerMode}");
                Debug.Assert(data->bGcStructuresValid == dataLocal.bGcStructuresValid, $"cDAC: {data->bGcStructuresValid}, DAC: {dataLocal.bGcStructuresValid}");
                Debug.Assert(data->HeapCount == dataLocal.HeapCount, $"cDAC: {data->HeapCount}, DAC: {dataLocal.HeapCount}");
                Debug.Assert(data->g_max_generation == dataLocal.g_max_generation, $"cDAC: {data->g_max_generation}, DAC: {dataLocal.g_max_generation}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetGCHeapList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[] heaps, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetGCHeapList(count, heaps, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[] heapsLocal = new ClrDataAddress[(int)count];
            uint neededLocal;
            int hrLocal = _legacyImpl.GetGCHeapList(count, heapsLocal, &neededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                // in theory, these don't need to be in the same order, but for consistency it is
                // easiest for consumers and verification if the DAC and cDAC return the same order
                for (int i = 0; i < neededLocal; i++)
                {
                    Debug.Assert(heaps[i] == heapsLocal[i], $"cDAC: {heaps[i]:x}, DAC: {heapsLocal[i]:x}");
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetGCHeapDetails(ClrDataAddress heap, DacpGcHeapDetails* details)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetGCHeapDetails(heap, details) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapDetails detailsLocal = default;
            int hrLocal = _legacyImpl.GetGCHeapDetails(heap, &detailsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(details->heapAddr == detailsLocal.heapAddr, $"cDAC: {details->heapAddr:x}, DAC: {detailsLocal.heapAddr:x}");
                Debug.Assert(details->alloc_allocated == detailsLocal.alloc_allocated, $"cDAC: {details->alloc_allocated:x}, DAC: {detailsLocal.alloc_allocated:x}");
                Debug.Assert(details->mark_array == detailsLocal.mark_array, $"cDAC: {details->mark_array:x}, DAC: {detailsLocal.mark_array:x}");
                Debug.Assert(details->current_c_gc_state == detailsLocal.current_c_gc_state, $"cDAC: {details->current_c_gc_state:x}, DAC: {detailsLocal.current_c_gc_state:x}");
                Debug.Assert(details->next_sweep_obj == detailsLocal.next_sweep_obj, $"cDAC: {details->next_sweep_obj:x}, DAC: {detailsLocal.next_sweep_obj:x}");
                Debug.Assert(details->saved_sweep_ephemeral_seg == detailsLocal.saved_sweep_ephemeral_seg, $"cDAC: {details->saved_sweep_ephemeral_seg:x}, DAC: {detailsLocal.saved_sweep_ephemeral_seg:x}");
                Debug.Assert(details->saved_sweep_ephemeral_start == detailsLocal.saved_sweep_ephemeral_start, $"cDAC: {details->saved_sweep_ephemeral_start:x}, DAC: {detailsLocal.saved_sweep_ephemeral_start:x}");
                Debug.Assert(details->background_saved_lowest_address == detailsLocal.background_saved_lowest_address, $"cDAC: {details->background_saved_lowest_address:x}, DAC: {detailsLocal.background_saved_lowest_address:x}");
                Debug.Assert(details->background_saved_highest_address == detailsLocal.background_saved_highest_address, $"cDAC: {details->background_saved_highest_address:x}, DAC: {detailsLocal.background_saved_highest_address:x}");

                // Verify generation table data
                for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS; i++)
                {
                    Debug.Assert(details->generation_table[i].start_segment == detailsLocal.generation_table[i].start_segment, $"cDAC gen[{i}].start_segment: {details->generation_table[i].start_segment:x}, DAC: {detailsLocal.generation_table[i].start_segment:x}");
                    Debug.Assert(details->generation_table[i].allocation_start == detailsLocal.generation_table[i].allocation_start, $"cDAC gen[{i}].allocation_start: {details->generation_table[i].allocation_start:x}, DAC: {detailsLocal.generation_table[i].allocation_start:x}");
                    Debug.Assert(details->generation_table[i].allocContextPtr == detailsLocal.generation_table[i].allocContextPtr, $"cDAC gen[{i}].allocContextPtr: {details->generation_table[i].allocContextPtr:x}, DAC: {detailsLocal.generation_table[i].allocContextPtr:x}");
                    Debug.Assert(details->generation_table[i].allocContextLimit == detailsLocal.generation_table[i].allocContextLimit, $"cDAC gen[{i}].allocContextLimit: {details->generation_table[i].allocContextLimit:x}, DAC: {detailsLocal.generation_table[i].allocContextLimit:x}");
                }

                Debug.Assert(details->ephemeral_heap_segment == detailsLocal.ephemeral_heap_segment, $"cDAC: {details->ephemeral_heap_segment:x}, DAC: {detailsLocal.ephemeral_heap_segment:x}");

                // Verify finalization fill pointers
                for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS + 3; i++)
                {
                    Debug.Assert(details->finalization_fill_pointers[i] == detailsLocal.finalization_fill_pointers[i], $"cDAC finalization_fill_pointers[{i}]: {details->finalization_fill_pointers[i]:x}, DAC: {detailsLocal.finalization_fill_pointers[i]:x}");
                }

                Debug.Assert(details->lowest_address == detailsLocal.lowest_address, $"cDAC: {details->lowest_address:x}, DAC: {detailsLocal.lowest_address:x}");
                Debug.Assert(details->highest_address == detailsLocal.highest_address, $"cDAC: {details->highest_address:x}, DAC: {detailsLocal.highest_address:x}");
                Debug.Assert(details->card_table == detailsLocal.card_table, $"cDAC: {details->card_table:x}, DAC: {detailsLocal.card_table:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetGCHeapStaticData(DacpGcHeapDetails* details)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetGCHeapStaticData(details) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapDetails detailsLocal = default;
            int hrLocal = _legacyImpl.GetGCHeapStaticData(&detailsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(details->heapAddr == detailsLocal.heapAddr, $"cDAC: {details->heapAddr:x}, DAC: {detailsLocal.heapAddr:x}");
                Debug.Assert(details->alloc_allocated == detailsLocal.alloc_allocated, $"cDAC: {details->alloc_allocated:x}, DAC: {detailsLocal.alloc_allocated:x}");
                Debug.Assert(details->mark_array == detailsLocal.mark_array, $"cDAC: {details->mark_array:x}, DAC: {detailsLocal.mark_array:x}");
                Debug.Assert(details->current_c_gc_state == detailsLocal.current_c_gc_state, $"cDAC: {details->current_c_gc_state:x}, DAC: {detailsLocal.current_c_gc_state:x}");
                Debug.Assert(details->next_sweep_obj == detailsLocal.next_sweep_obj, $"cDAC: {details->next_sweep_obj:x}, DAC: {detailsLocal.next_sweep_obj:x}");
                Debug.Assert(details->saved_sweep_ephemeral_seg == detailsLocal.saved_sweep_ephemeral_seg, $"cDAC: {details->saved_sweep_ephemeral_seg:x}, DAC: {detailsLocal.saved_sweep_ephemeral_seg:x}");
                Debug.Assert(details->saved_sweep_ephemeral_start == detailsLocal.saved_sweep_ephemeral_start, $"cDAC: {details->saved_sweep_ephemeral_start:x}, DAC: {detailsLocal.saved_sweep_ephemeral_start:x}");
                Debug.Assert(details->background_saved_lowest_address == detailsLocal.background_saved_lowest_address, $"cDAC: {details->background_saved_lowest_address:x}, DAC: {detailsLocal.background_saved_lowest_address:x}");
                Debug.Assert(details->background_saved_highest_address == detailsLocal.background_saved_highest_address, $"cDAC: {details->background_saved_highest_address:x}, DAC: {detailsLocal.background_saved_highest_address:x}");
                for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS; i++)
                {
                    Debug.Assert(details->generation_table[i].start_segment == detailsLocal.generation_table[i].start_segment, $"cDAC gen[{i}].start_segment: {details->generation_table[i].start_segment:x}, DAC: {detailsLocal.generation_table[i].start_segment:x}");
                    Debug.Assert(details->generation_table[i].allocation_start == detailsLocal.generation_table[i].allocation_start, $"cDAC gen[{i}].allocation_start: {details->generation_table[i].allocation_start:x}, DAC: {detailsLocal.generation_table[i].allocation_start:x}");
                    Debug.Assert(details->generation_table[i].allocContextPtr == detailsLocal.generation_table[i].allocContextPtr, $"cDAC gen[{i}].allocContextPtr: {details->generation_table[i].allocContextPtr:x}, DAC: {detailsLocal.generation_table[i].allocContextPtr:x}");
                    Debug.Assert(details->generation_table[i].allocContextLimit == detailsLocal.generation_table[i].allocContextLimit, $"cDAC gen[{i}].allocContextLimit: {details->generation_table[i].allocContextLimit:x}, DAC: {detailsLocal.generation_table[i].allocContextLimit:x}");
                }
                Debug.Assert(details->ephemeral_heap_segment == detailsLocal.ephemeral_heap_segment, $"cDAC: {details->ephemeral_heap_segment:x}, DAC: {detailsLocal.ephemeral_heap_segment:x}");
                for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS + 3; i++)
                {
                    Debug.Assert(details->finalization_fill_pointers[i] == detailsLocal.finalization_fill_pointers[i], $"cDAC finalization_fill_pointers[{i}]: {details->finalization_fill_pointers[i]:x}, DAC: {detailsLocal.finalization_fill_pointers[i]:x}");
                }
                Debug.Assert(details->lowest_address == detailsLocal.lowest_address, $"cDAC: {details->lowest_address:x}, DAC: {detailsLocal.lowest_address:x}");
                Debug.Assert(details->highest_address == detailsLocal.highest_address, $"cDAC: {details->highest_address:x}, DAC: {detailsLocal.highest_address:x}");
                Debug.Assert(details->card_table == detailsLocal.card_table, $"cDAC: {details->card_table:x}, DAC: {detailsLocal.card_table:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetHeapSegmentData(ClrDataAddress seg, DacpHeapSegmentData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetHeapSegmentData(seg, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpHeapSegmentData dataLocal = default;
            int hrLocal = _legacyImpl.GetHeapSegmentData(seg, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->segmentAddr == dataLocal.segmentAddr, $"segmentAddr - cDAC: {data->segmentAddr:x}, DAC: {dataLocal.segmentAddr:x}");
                Debug.Assert(data->allocated == dataLocal.allocated, $"allocated - cDAC: {data->allocated:x}, DAC: {dataLocal.allocated:x}");
                Debug.Assert(data->committed == dataLocal.committed, $"committed - cDAC: {data->committed:x}, DAC: {dataLocal.committed:x}");
                Debug.Assert(data->reserved == dataLocal.reserved, $"reserved - cDAC: {data->reserved:x}, DAC: {dataLocal.reserved:x}");
                Debug.Assert(data->used == dataLocal.used, $"used - cDAC: {data->used:x}, DAC: {dataLocal.used:x}");
                Debug.Assert(data->mem == dataLocal.mem, $"mem - cDAC: {data->mem:x}, DAC: {dataLocal.mem:x}");
                Debug.Assert(data->next == dataLocal.next, $"next - cDAC: {data->next:x}, DAC: {dataLocal.next:x}");
                Debug.Assert(data->gc_heap == dataLocal.gc_heap, $"gc_heap - cDAC: {data->gc_heap:x}, DAC: {dataLocal.gc_heap:x}");
                Debug.Assert(data->highAllocMark == dataLocal.highAllocMark, $"highAllocMark - cDAC: {data->highAllocMark:x}, DAC: {dataLocal.highAllocMark:x}");
                Debug.Assert(data->flags == dataLocal.flags, $"flags - cDAC: {data->flags:x}, DAC: {dataLocal.flags:x}");
                Debug.Assert(data->background_allocated == dataLocal.background_allocated, $"background_allocated - cDAC: {data->background_allocated:x}, DAC: {dataLocal.background_allocated:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetOOMData(ClrDataAddress oomAddr, DacpOomData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetOOMData(oomAddr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpOomData dataLocal;
            int hrLocal = _legacyImpl.GetOOMData(oomAddr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->reason == dataLocal.reason, $"cDAC: {data->reason}, DAC: {dataLocal.reason}");
                Debug.Assert(data->alloc_size == dataLocal.alloc_size, $"cDAC: {data->alloc_size}, DAC: {dataLocal.alloc_size}");
                Debug.Assert(data->available_pagefile_mb == dataLocal.available_pagefile_mb, $"cDAC: {data->available_pagefile_mb}, DAC: {dataLocal.available_pagefile_mb}");
                Debug.Assert(data->gc_index == dataLocal.gc_index, $"cDAC: {data->gc_index}, DAC: {dataLocal.gc_index}");
                Debug.Assert(data->fgm == dataLocal.fgm, $"cDAC: {data->fgm}, DAC: {dataLocal.fgm}");
                Debug.Assert(data->size == dataLocal.size, $"cDAC: {data->size}, DAC: {dataLocal.size}");
                Debug.Assert(data->loh_p == dataLocal.loh_p, $"cDAC: {data->loh_p}, DAC: {dataLocal.loh_p}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetOOMStaticData(DacpOomData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetOOMStaticData(data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpOomData dataLocal;
            int hrLocal = _legacyImpl.GetOOMStaticData(&dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->reason == dataLocal.reason, $"cDAC: {data->reason}, DAC: {dataLocal.reason}");
                Debug.Assert(data->alloc_size == dataLocal.alloc_size, $"cDAC: {data->alloc_size}, DAC: {dataLocal.alloc_size}");
                Debug.Assert(data->available_pagefile_mb == dataLocal.available_pagefile_mb, $"cDAC: {data->available_pagefile_mb}, DAC: {dataLocal.available_pagefile_mb}");
                Debug.Assert(data->gc_index == dataLocal.gc_index, $"cDAC: {data->gc_index}, DAC: {dataLocal.gc_index}");
                Debug.Assert(data->fgm == dataLocal.fgm, $"cDAC: {data->fgm}, DAC: {dataLocal.fgm}");
                Debug.Assert(data->size == dataLocal.size, $"cDAC: {data->size}, DAC: {dataLocal.size}");
                Debug.Assert(data->loh_p == dataLocal.loh_p, $"cDAC: {data->loh_p}, DAC: {dataLocal.loh_p}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetHeapAnalyzeData(ClrDataAddress addr, DacpGcHeapAnalyzeData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetHeapAnalyzeData(addr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapAnalyzeData dataLocal = default;
            int hrLocal = _legacyImpl.GetHeapAnalyzeData(addr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->heapAddr == dataLocal.heapAddr, $"cDAC: {data->heapAddr:x}, DAC: {dataLocal.heapAddr:x}");
                Debug.Assert(data->internal_root_array == dataLocal.internal_root_array, $"cDAC: {data->internal_root_array:x}, DAC: {dataLocal.internal_root_array:x}");
                Debug.Assert(data->internal_root_array_index == dataLocal.internal_root_array_index, $"cDAC: {data->internal_root_array_index}, DAC: {dataLocal.internal_root_array_index}");
                Debug.Assert(data->heap_analyze_success == dataLocal.heap_analyze_success, $"cDAC: {data->heap_analyze_success}, DAC: {dataLocal.heap_analyze_success}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetHeapAnalyzeStaticData(DacpGcHeapAnalyzeData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetHeapAnalyzeStaticData(data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapAnalyzeData dataLocal = default;
            int hrLocal = _legacyImpl.GetHeapAnalyzeStaticData(&dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->heapAddr == dataLocal.heapAddr, $"cDAC: {data->heapAddr:x}, DAC: {dataLocal.heapAddr:x}");
                Debug.Assert(data->internal_root_array == dataLocal.internal_root_array, $"cDAC: {data->internal_root_array:x}, DAC: {dataLocal.internal_root_array:x}");
                Debug.Assert(data->internal_root_array_index == dataLocal.internal_root_array_index, $"cDAC: {data->internal_root_array_index}, DAC: {dataLocal.internal_root_array_index}");
                Debug.Assert(data->heap_analyze_success == dataLocal.heap_analyze_success, $"cDAC: {data->heap_analyze_success}, DAC: {dataLocal.heap_analyze_success}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetDomainLocalModuleData(ClrDataAddress addr, void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetDomainLocalModuleData(addr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetDomainLocalModuleData(addr, data);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetDomainLocalModuleDataFromAppDomain(ClrDataAddress appDomainAddr, int moduleID, void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetDomainLocalModuleDataFromAppDomain(appDomainAddr, moduleID, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetDomainLocalModuleDataFromAppDomain(appDomainAddr, moduleID, data);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetDomainLocalModuleDataFromModule(ClrDataAddress moduleAddr, void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetDomainLocalModuleDataFromModule(moduleAddr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetDomainLocalModuleDataFromModule(moduleAddr, data);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetThreadLocalModuleData(ClrDataAddress thread, uint index, void* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetThreadLocalModuleData(thread, index, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetThreadLocalModuleData(thread, index, data);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetSyncBlockData(uint number, DacpSyncBlockData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetSyncBlockData(number, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpSyncBlockData dataLocal;
            int hrLocal = _legacyImpl.GetSyncBlockData(number, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->Object == dataLocal.Object, $"cDAC: {data->Object:x}, DAC: {dataLocal.Object:x}");
                Debug.Assert(data->bFree == dataLocal.bFree, $"cDAC: {data->bFree}, DAC: {dataLocal.bFree}");
                Debug.Assert(data->SyncBlockPointer == dataLocal.SyncBlockPointer, $"cDAC: {data->SyncBlockPointer:x}, DAC: {dataLocal.SyncBlockPointer:x}");
                Debug.Assert(data->COMFlags == dataLocal.COMFlags, $"cDAC: {data->COMFlags}, DAC: {dataLocal.COMFlags}");
                Debug.Assert(data->MonitorHeld == dataLocal.MonitorHeld, $"cDAC: {data->MonitorHeld}, DAC: {dataLocal.MonitorHeld}");
                if (data->MonitorHeld != 0)
                {
                    Debug.Assert(data->Recursion == dataLocal.Recursion, $"cDAC: {data->Recursion}, DAC: {dataLocal.Recursion}");
                    Debug.Assert(data->HoldingThread == dataLocal.HoldingThread, $"cDAC: {data->HoldingThread:x}, DAC: {dataLocal.HoldingThread:x}");
                }
                Debug.Assert(data->AdditionalThreadCount == dataLocal.AdditionalThreadCount, $"cDAC: {data->AdditionalThreadCount}, DAC: {dataLocal.AdditionalThreadCount}");
                Debug.Assert(data->appDomainPtr == dataLocal.appDomainPtr, $"cDAC: {data->appDomainPtr:x}, DAC: {dataLocal.appDomainPtr:x}");
                Debug.Assert(data->SyncBlockCount == dataLocal.SyncBlockCount, $"cDAC: {data->SyncBlockCount}, DAC: {dataLocal.SyncBlockCount}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetSyncBlockCleanupData(ClrDataAddress addr, DacpSyncBlockCleanupData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetSyncBlockCleanupData(addr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpSyncBlockCleanupData dataLocal;
            int hrLocal = _legacyImpl.GetSyncBlockCleanupData(addr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->SyncBlockPointer == dataLocal.SyncBlockPointer, $"cDAC: {data->SyncBlockPointer:x}, DAC: {dataLocal.SyncBlockPointer:x}");
                Debug.Assert(data->nextSyncBlock == dataLocal.nextSyncBlock, $"cDAC: {data->nextSyncBlock:x}, DAC: {dataLocal.nextSyncBlock:x}");
                Debug.Assert(data->blockRCW == dataLocal.blockRCW, $"cDAC: {data->blockRCW:x}, DAC: {dataLocal.blockRCW:x}");
                Debug.Assert(data->blockClassFactory == dataLocal.blockClassFactory, $"cDAC: {data->blockClassFactory:x}, DAC: {dataLocal.blockClassFactory:x}");
                Debug.Assert(data->blockCCW == dataLocal.blockCCW, $"cDAC: {data->blockCCW:x}, DAC: {dataLocal.blockCCW:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetHandleEnum(DacComNullableByRef<ISOSHandleEnum> ppHandleEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<ISOSHandleEnum> ppHandleEnumCDac = new(ppHandleEnum.IsNullRef);
        DacComNullableByRef<ISOSHandleEnum> ppHandleEnumDac = new(ppHandleEnum.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetHandleEnum(ppHandleEnumCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetHandleEnum(ppHandleEnumDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!ppHandleEnum.IsNullRef)
            ppHandleEnum.Interface = ShimProxy.PairISOSHandleEnum(_session, ppHandleEnumCDac.Interface, ppHandleEnumDac.Interface);
        return hr;
    }

    int ISOSDacInterface.GetHandleEnumForTypes([In, MarshalUsing(CountElementName = "count")] uint[] types, uint count, DacComNullableByRef<ISOSHandleEnum> ppHandleEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<ISOSHandleEnum> ppHandleEnumCDac = new(ppHandleEnum.IsNullRef);
        DacComNullableByRef<ISOSHandleEnum> ppHandleEnumDac = new(ppHandleEnum.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetHandleEnumForTypes(types, count, ppHandleEnumCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetHandleEnumForTypes(types, count, ppHandleEnumDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!ppHandleEnum.IsNullRef)
            ppHandleEnum.Interface = ShimProxy.PairISOSHandleEnum(_session, ppHandleEnumCDac.Interface, ppHandleEnumDac.Interface);
        return hr;
    }
    int ISOSDacInterface.GetHandleEnumForGC(uint gen, DacComNullableByRef<ISOSHandleEnum> ppHandleEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetHandleEnumForGC(gen, ppHandleEnum) : HResults.E_NOTIMPL;
        return hr;
    }
    int ISOSDacInterface.TraverseEHInfo(ClrDataAddress ip, delegate* unmanaged<uint, uint, DACEHInfo*, void*, int> pCallback, void* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        TraverseEhInfoRecordingContext? recordingContext = null;
        GCHandle recordingHandle = default;
        delegate* unmanaged<uint, uint, DACEHInfo*, void*, int> cdacCallback = pCallback;
        void* cdacToken = token;
        if (pCallback is not null)
        {
            recordingContext = new TraverseEhInfoRecordingContext { Callback = pCallback, Token = token };
            recordingHandle = GCHandle.Alloc(recordingContext);
            cdacCallback = &RecordingTraverseEHInfoCallback;
            cdacToken = GCHandle.ToIntPtr(recordingHandle).ToPointer();
        }
        int hr;
        try
        {
            hr = _cdacImpl is not null ? _cdacImpl.TraverseEHInfo(ip, cdacCallback, cdacToken) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (recordingHandle.IsAllocated)
                recordingHandle.Free();
        }
        if (_legacyImpl is not null)
        {
            TraverseEhInfoExpected expected = new(recordingContext?.Elements ?? [], recordingContext?.ExpectAbort is true, recordingContext?.AbortIndex);
            GCHandle expectedHandle = GCHandle.Alloc(expected);
            try
            {
                void* tokenDebug = GCHandle.ToIntPtr(expectedHandle).ToPointer();
                delegate* unmanaged<uint, uint, DACEHInfo*, void*, int> callbackDebugPtr = &TraverseEHInfoCallback;
                int hrLocal = _legacyImpl.TraverseEHInfo(ip, callbackDebugPtr, tokenDebug);
                Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.Exact);
            }
            finally
            {
                expectedHandle.Free();
            }
        }
#else
        int hr = _cdacImpl is not null ? _cdacImpl.TraverseEHInfo(ip, pCallback, token) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int ISOSDacInterface.GetNestedExceptionData(ClrDataAddress exception, ClrDataAddress* exceptionObject, ClrDataAddress* nextNestedException)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNestedExceptionData(exception, exceptionObject, nextNestedException) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress exceptionObjectLocal;
            ClrDataAddress nextNestedExceptionLocal;
            int hrLocal = _legacyImpl.GetNestedExceptionData(exception, &exceptionObjectLocal, &nextNestedExceptionLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*exceptionObject == exceptionObjectLocal);
                Debug.Assert(*nextNestedException == nextNestedExceptionLocal);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetStressLogAddress(ClrDataAddress* stressLog)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetStressLogAddress(stressLog) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress legacyStressLog;
            Debug.Assert(HResults.S_OK == _legacyImpl.GetStressLogAddress(&legacyStressLog));
            Debug.Assert(legacyStressLog == *stressLog);
        }
#endif
        return hr;
    }
    int ISOSDacInterface.TraverseLoaderHeap(ClrDataAddress loaderHeapAddr, delegate* unmanaged<ulong, nuint, Interop.BOOL, void> pCallback)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        DebugTraverseLoaderHeapBlocks.Clear();
        _debugTraverseLoaderDebugCount = 0;
        TraverseLoaderHeapRecordingContext? previousContext = _recordingTraverseLoaderHeapContext;
        delegate* unmanaged<ulong, nuint, Interop.BOOL, void> cdacCallback = pCallback;
        if (pCallback is not null)
        {
            _recordingTraverseLoaderHeapContext = new TraverseLoaderHeapRecordingContext { Callback = pCallback };
            cdacCallback = &RecordingTraverseLoaderHeapCallback;
        }
        int hr;
        try
        {
            hr = _cdacImpl is not null ? _cdacImpl.TraverseLoaderHeap(loaderHeapAddr, cdacCallback) : HResults.E_NOTIMPL;
        }
        finally
        {
            _recordingTraverseLoaderHeapContext = previousContext;
        }
        if (_legacyImpl is not null)
        {
            int cdacCount = DebugTraverseLoaderHeapBlocks.Count;
            delegate* unmanaged<ulong, nuint, Interop.BOOL, void> debugCallbackPtr = &TraverseLoaderHeapDebugCallback;
            int hrLocal = _legacyImpl.TraverseLoaderHeap(loaderHeapAddr, debugCallbackPtr);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(DebugTraverseLoaderHeapBlocks.Count == 0,
                    $"cDAC found {cdacCount} blocks, DAC matched {_debugTraverseLoaderDebugCount}, {DebugTraverseLoaderHeapBlocks.Count} unmatched");
                Debug.Assert(_debugTraverseLoaderDebugCount == (uint)cdacCount,
                    $"cDAC: {cdacCount} blocks, DAC: {_debugTraverseLoaderDebugCount} blocks");
            }
        }
#else
        int hr = _cdacImpl is not null ? _cdacImpl.TraverseLoaderHeap(loaderHeapAddr, pCallback) : HResults.E_NOTIMPL;
#endif
        return hr;
    }
    int ISOSDacInterface.GetCodeHeapList(ClrDataAddress jitManager, uint count, [In, MarshalUsing(CountElementName = nameof(count)), Out] DacpJitCodeHeapInfo[]? codeHeaps, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCodeHeapList(jitManager, count, codeHeaps, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint neededLocal = 0;
            DacpJitCodeHeapInfo[]? legacyHeaps = codeHeaps is not null ? new DacpJitCodeHeapInfo[(int)count] : null;
            int hrLocal = _legacyImpl.GetCodeHeapList(jitManager, count, legacyHeaps, codeHeaps is null && pNeeded is null ? null : &neededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                if (pNeeded != null)
                {
                    Debug.Assert(*pNeeded == neededLocal, $"cDAC count: {(*pNeeded):x}, DAC count: {neededLocal:x}");
                }
                if (codeHeaps != null && legacyHeaps != null)
                {
                    for (uint i = 0; i < neededLocal && i < count; i++)
                    {
                        Debug.Assert(codeHeaps[i].codeHeapType == legacyHeaps[i].codeHeapType,
                            $"cDAC heap[{i}] type: {codeHeaps[i].codeHeapType}, DAC: {legacyHeaps[i].codeHeapType}");
                        if (codeHeaps[i].codeHeapType == DacpJitCodeHeapInfo.CodeHeapType.CODEHEAP_LOADER)
                            Debug.Assert(codeHeaps[i].LoaderHeap == legacyHeaps[i].LoaderHeap,
                                $"cDAC heap[{i}] LoaderHeap: {codeHeaps[i].LoaderHeap:x}, DAC: {legacyHeaps[i].LoaderHeap:x}");
                        else if (codeHeaps[i].codeHeapType == DacpJitCodeHeapInfo.CodeHeapType.CODEHEAP_HOST)
                        {
                            Debug.Assert(codeHeaps[i].baseAddr == legacyHeaps[i].baseAddr,
                                $"cDAC heap[{i}] baseAddr: {codeHeaps[i].baseAddr:x}, DAC: {legacyHeaps[i].baseAddr:x}");
                            Debug.Assert(codeHeaps[i].currentAddr == legacyHeaps[i].currentAddr,
                                $"cDAC heap[{i}] currentAddr: {codeHeaps[i].currentAddr:x}, DAC: {legacyHeaps[i].currentAddr:x}");
                        }
                    }
                }
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.TraverseVirtCallStubHeap(ClrDataAddress pAppDomain, VCSHeapType heaptype, delegate* unmanaged<ulong, nuint, Interop.BOOL, void> pCallback)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        DebugTraverseLoaderHeapBlocks.Clear();
        _debugTraverseLoaderDebugCount = 0;
        TraverseLoaderHeapRecordingContext? previousContext = _recordingTraverseLoaderHeapContext;
        delegate* unmanaged<ulong, nuint, Interop.BOOL, void> cdacCallback = pCallback;
        if (pCallback is not null)
        {
            _recordingTraverseLoaderHeapContext = new TraverseLoaderHeapRecordingContext { Callback = pCallback };
            cdacCallback = &RecordingTraverseLoaderHeapCallback;
        }
        int hr;
        try
        {
            hr = _cdacImpl is not null ? _cdacImpl.TraverseVirtCallStubHeap(pAppDomain, heaptype, cdacCallback) : HResults.E_NOTIMPL;
        }
        finally
        {
            _recordingTraverseLoaderHeapContext = previousContext;
        }
        if (_legacyImpl is not null)
        {
            int cdacCount = DebugTraverseLoaderHeapBlocks.Count;
            delegate* unmanaged<ulong, nuint, Interop.BOOL, void> debugCallbackPtr = &TraverseLoaderHeapDebugCallback;
            int hrLocal = _legacyImpl.TraverseVirtCallStubHeap(pAppDomain, heaptype, debugCallbackPtr);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(DebugTraverseLoaderHeapBlocks.Count == 0,
                    $"cDAC found {cdacCount} blocks, DAC matched {_debugTraverseLoaderDebugCount}, {DebugTraverseLoaderHeapBlocks.Count} unmatched");
                Debug.Assert(_debugTraverseLoaderDebugCount == (uint)cdacCount,
                    $"cDAC: {cdacCount} blocks, DAC: {_debugTraverseLoaderDebugCount} blocks");
            }
        }
#else
        int hr = _cdacImpl is not null ? _cdacImpl.TraverseVirtCallStubHeap(pAppDomain, heaptype, pCallback) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int ISOSDacInterface.GetUsefulGlobals(DacpUsefulGlobalsData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetUsefulGlobals(data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpUsefulGlobalsData dataLocal;
            int hrLocal = _legacyImpl.GetUsefulGlobals(&dataLocal);
            // SOS can call GetUsefulGlobals before the global pointers are initialized.
            // In the DAC, this behavior depends on the compiler.
            // MSVC builds: the DAC global table is a compile time constant and the DAC will return successfully.
            // Clang builds: the DAC global table is constructed at runtime and the DAC will fail.
            // Because of this variation, we cannot match the DAC behavior exactly.
            // As long as the returned data matches, it should be fine.
            if (hr == HResults.S_OK || hrLocal == HResults.S_OK)
            {
                Debug.Assert(data->ArrayMethodTable == dataLocal.ArrayMethodTable);
                Debug.Assert(data->StringMethodTable == dataLocal.StringMethodTable);
                Debug.Assert(data->ObjectMethodTable == dataLocal.ObjectMethodTable);
                Debug.Assert(data->ExceptionMethodTable == dataLocal.ExceptionMethodTable);
                Debug.Assert(data->FreeMethodTable == dataLocal.FreeMethodTable);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetClrWatsonBuckets(ClrDataAddress thread, void* pGenericModeBlock)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetClrWatsonBuckets(thread, pGenericModeBlock) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal;
            const int SizeOfGenericModeBlock = 5616;
            byte[] genericModeBlockLocal = new byte[SizeOfGenericModeBlock];
            fixed (byte* ptr = genericModeBlockLocal)
            {
                hrLocal = _legacyImpl.GetClrWatsonBuckets(thread, ptr);
            }

            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(new ReadOnlySpan<byte>(genericModeBlockLocal, 0, SizeOfGenericModeBlock).SequenceEqual(new Span<byte>(pGenericModeBlock, SizeOfGenericModeBlock)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetTLSIndex(uint* pIndex)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetTLSIndex(pIndex) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint indexLocal;
            int hrLocal = _legacyImpl.GetTLSIndex(&indexLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*pIndex == indexLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetDacModuleHandle(void* phModule)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetDacModuleHandle(phModule) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface.GetRCWData(ClrDataAddress addr, DacpRCWData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetRCWData(addr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpRCWData dataLocal;
            int hrLocal = _legacyImpl.GetRCWData(addr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->identityPointer == dataLocal.identityPointer, $"cDAC: {data->identityPointer:x}, DAC: {dataLocal.identityPointer:x}");
                Debug.Assert(data->unknownPointer == dataLocal.unknownPointer, $"cDAC: {data->unknownPointer:x}, DAC: {dataLocal.unknownPointer:x}");
                Debug.Assert(data->managedObject == dataLocal.managedObject, $"cDAC: {data->managedObject:x}, DAC: {dataLocal.managedObject:x}");
                Debug.Assert(data->vtablePtr == dataLocal.vtablePtr, $"cDAC: {data->vtablePtr:x}, DAC: {dataLocal.vtablePtr:x}");
                Debug.Assert(data->creatorThread == dataLocal.creatorThread, $"cDAC: {data->creatorThread:x}, DAC: {dataLocal.creatorThread:x}");
                Debug.Assert(data->ctxCookie == dataLocal.ctxCookie, $"cDAC: {data->ctxCookie:x}, DAC: {dataLocal.ctxCookie:x}");
                Debug.Assert(data->refCount == dataLocal.refCount, $"cDAC: {data->refCount}, DAC: {dataLocal.refCount}");
                Debug.Assert(data->interfaceCount == dataLocal.interfaceCount, $"cDAC: {data->interfaceCount}, DAC: {dataLocal.interfaceCount}");
                Debug.Assert(data->isAggregated == dataLocal.isAggregated, $"cDAC: {data->isAggregated}, DAC: {dataLocal.isAggregated}");
                Debug.Assert(data->isContained == dataLocal.isContained, $"cDAC: {data->isContained}, DAC: {dataLocal.isContained}");
                Debug.Assert(data->isFreeThreaded == dataLocal.isFreeThreaded, $"cDAC: {data->isFreeThreaded}, DAC: {dataLocal.isFreeThreaded}");
                Debug.Assert(data->isDisconnected == dataLocal.isDisconnected, $"cDAC: {data->isDisconnected}, DAC: {dataLocal.isDisconnected}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetRCWInterfaces(ClrDataAddress rcw, uint count, [In, MarshalUsing(CountElementName = nameof(count)), Out] DacpCOMInterfacePointerData[]? interfaces, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetRCWInterfaces(rcw, count, interfaces, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint pNeededLocal = 0;
            DacpCOMInterfacePointerData[]? interfacesLocal = interfaces != null ? new DacpCOMInterfacePointerData[(int)count] : null;
            int hrLocal = _legacyImpl.GetRCWInterfaces(rcw, count, interfacesLocal, pNeeded == null && interfacesLocal == null ? null : &pNeededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded is null || *pNeeded == pNeededLocal, $"cDAC: {(pNeeded is null ? "null" : (*pNeeded).ToString())}, DAC: {pNeededLocal}");
                if (interfacesLocal is not null && interfaces is not null)
                {
                    for (uint i = 0; i < pNeededLocal && i < count; i++)
                    {
                        Debug.Assert(interfaces[i].methodTable == interfacesLocal[i].methodTable, $"cDAC: {interfaces[i].methodTable:x}, DAC: {interfacesLocal[i].methodTable:x}");
                        Debug.Assert(interfaces[i].interfacePtr == interfacesLocal[i].interfacePtr, $"cDAC: {interfaces[i].interfacePtr:x}, DAC: {interfacesLocal[i].interfacePtr:x}");
                        Debug.Assert(interfaces[i].comContext == interfacesLocal[i].comContext, $"cDAC: {interfaces[i].comContext:x}, DAC: {interfacesLocal[i].comContext:x}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetCCWData(ClrDataAddress ccw, DacpCCWData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCCWData(ccw, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpCCWData dataLocal = default;
            int hrLocal = _legacyImpl.GetCCWData(ccw, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->outerIUnknown == dataLocal.outerIUnknown, $"cDAC outerIUnknown: {data->outerIUnknown:x}, DAC: {dataLocal.outerIUnknown:x}");
                Debug.Assert(data->managedObject == dataLocal.managedObject, $"cDAC managedObject: {data->managedObject:x}, DAC: {dataLocal.managedObject:x}");
                Debug.Assert(data->handle == dataLocal.handle, $"cDAC handle: {data->handle:x}, DAC: {dataLocal.handle:x}");
                Debug.Assert(data->ccwAddress == dataLocal.ccwAddress, $"cDAC ccwAddress: {data->ccwAddress:x}, DAC: {dataLocal.ccwAddress:x}");
                Debug.Assert(data->refCount == dataLocal.refCount, $"cDAC refCount: {data->refCount}, DAC: {dataLocal.refCount}");
                Debug.Assert(data->interfaceCount == dataLocal.interfaceCount, $"cDAC interfaceCount: {data->interfaceCount}, DAC: {dataLocal.interfaceCount}");
                Debug.Assert(data->isNeutered == dataLocal.isNeutered, $"cDAC isNeutered: {data->isNeutered}, DAC: {dataLocal.isNeutered}");
                Debug.Assert(data->jupiterRefCount == dataLocal.jupiterRefCount, $"cDAC jupiterRefCount: {data->jupiterRefCount}, DAC: {dataLocal.jupiterRefCount}");
                Debug.Assert(data->isPegged == dataLocal.isPegged, $"cDAC isPegged: {data->isPegged}, DAC: {dataLocal.isPegged}");
                Debug.Assert(data->isGlobalPegged == dataLocal.isGlobalPegged, $"cDAC isGlobalPegged: {data->isGlobalPegged}, DAC: {dataLocal.isGlobalPegged}");
                Debug.Assert(data->hasStrongRef == dataLocal.hasStrongRef, $"cDAC hasStrongRef: {data->hasStrongRef}, DAC: {dataLocal.hasStrongRef}");
                Debug.Assert(data->isExtendsCOMObject == dataLocal.isExtendsCOMObject, $"cDAC isExtendsCOMObject: {data->isExtendsCOMObject}, DAC: {dataLocal.isExtendsCOMObject}");
                Debug.Assert(data->isAggregated == dataLocal.isAggregated, $"cDAC isAggregated: {data->isAggregated}, DAC: {dataLocal.isAggregated}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetCCWInterfaces(ClrDataAddress ccw, uint count, [In, MarshalUsing(CountElementName = nameof(count)), Out] DacpCOMInterfacePointerData[]? interfaces, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetCCWInterfaces(ccw, count, interfaces, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpCOMInterfacePointerData[]? interfacesLocal = interfaces != null ? new DacpCOMInterfacePointerData[(int)count] : null;
            uint pNeededLocal = 0;
            int hrLocal = _legacyImpl.GetCCWInterfaces(ccw, count, interfacesLocal, pNeeded == null && interfacesLocal == null ? null : &pNeededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded is null || *pNeeded == pNeededLocal, $"cDAC count: {(pNeeded is null ? "null" : (*pNeeded).ToString())}, DAC count: {pNeededLocal}");
                if (interfaces != null && interfacesLocal != null)
                {
                    for (uint i = 0; i < pNeededLocal && i < count; i++)
                    {
                        Debug.Assert(interfaces[i].methodTable == interfacesLocal[i].methodTable, $"cDAC methodTable[{i}]: {interfaces[i].methodTable:x}, DAC: {interfacesLocal[i].methodTable:x}");
                        Debug.Assert(interfaces[i].interfacePtr == interfacesLocal[i].interfacePtr, $"cDAC interfacePtr[{i}]: {interfaces[i].interfacePtr:x}, DAC: {interfacesLocal[i].interfacePtr:x}");
                        Debug.Assert(interfaces[i].comContext == interfacesLocal[i].comContext, $"cDAC comContext[{i}]: {interfaces[i].comContext:x}, DAC: {interfacesLocal[i].comContext:x}");
                    }
                }
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.TraverseRCWCleanupList(ClrDataAddress cleanupListPtr, delegate* unmanaged<ulong, ulong, ulong, Interop.BOOL, void*, Interop.BOOL> pCallback, void* token)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        TraverseRCWCleanupListRecordingContext? recordingContext = null;
        GCHandle recordingHandle = default;
        delegate* unmanaged<ulong, ulong, ulong, Interop.BOOL, void*, Interop.BOOL> cdacCallback = pCallback;
        void* cdacToken = token;
        if (pCallback is not null)
        {
            recordingContext = new TraverseRCWCleanupListRecordingContext { Callback = pCallback, Token = token };
            recordingHandle = GCHandle.Alloc(recordingContext);
            cdacCallback = &RecordingTraverseRCWCleanupListCallback;
            cdacToken = GCHandle.ToIntPtr(recordingHandle).ToPointer();
        }
        int hr;
        try
        {
            hr = _cdacImpl is not null ? _cdacImpl.TraverseRCWCleanupList(cleanupListPtr, cdacCallback, cdacToken) : HResults.E_NOTIMPL;
        }
        finally
        {
            if (recordingHandle.IsAllocated)
                recordingHandle.Free();
        }
        if (_legacyImpl is not null)
        {
            Dictionary<ulong, ulong> expectedElements = recordingContext?.ExpectedElements ?? [];
            ulong expectedCount = (ulong)expectedElements.Count;
            expectedElements[default] = 0;
            GCHandle expectedHandle = GCHandle.Alloc(expectedElements);
            try
            {
                void* tokenDebug = GCHandle.ToIntPtr(expectedHandle).ToPointer();
                delegate* unmanaged<ulong, ulong, ulong, Interop.BOOL, void*, Interop.BOOL> callbackDebugPtr = &TraverseRCWCleanupListCallback;
                int hrLocal = _legacyImpl.TraverseRCWCleanupList(cleanupListPtr, callbackDebugPtr, tokenDebug);
                Debug.ValidateHResult(hr, hrLocal);
                Debug.Assert(expectedElements[default] == expectedCount, $"cDAC: {expectedCount} elements, DAC: {expectedElements[default]} elements");
            }
            finally
            {
                expectedHandle.Free();
            }
        }
#else
        int hr = _cdacImpl is not null ? _cdacImpl.TraverseRCWCleanupList(cleanupListPtr, pCallback, token) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int ISOSDacInterface.GetStackReferences(int osThreadID, DacComNullableByRef<ISOSStackRefEnum> ppEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<ISOSStackRefEnum> ppEnumCDac = new(ppEnum.IsNullRef);
        DacComNullableByRef<ISOSStackRefEnum> ppEnumDac = new(ppEnum.IsNullRef);
        int hr = _cdacImpl is not null ? _cdacImpl.GetStackReferences(osThreadID, ppEnumCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetStackReferences(osThreadID, ppEnumDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!ppEnum.IsNullRef)
            ppEnum.Interface = ShimProxy.PairISOSStackRefEnum(_session, ppEnumCDac.Interface, ppEnumDac.Interface);
        return hr;
    }

    int ISOSDacInterface.GetRegisterName(int regName, uint count, char* buffer, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetRegisterName(regName, count, buffer, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] bufferLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = bufferLocal)
            {
                hrLocal = _legacyImpl.GetRegisterName(regName, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(pNeeded is null || *pNeeded == neededLocal);
                Debug.Assert(buffer is null || new ReadOnlySpan<char>(bufferLocal, 0, (int)Math.Min(count, neededLocal)).SequenceEqual(new ReadOnlySpan<char>(buffer, (int)Math.Min(count, neededLocal))));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetThreadAllocData(ClrDataAddress thread, DacpAllocData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetThreadAllocData(thread, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpAllocData dataLocal = default;
            int hrLocal = _legacyImpl.GetThreadAllocData(thread, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->allocBytes == dataLocal.allocBytes, $"cDAC: {data->allocBytes:x}, DAC: {dataLocal.allocBytes:x}");
                Debug.Assert(data->allocBytesLoh == dataLocal.allocBytesLoh, $"cDAC: {data->allocBytesLoh:x}, DAC: {dataLocal.allocBytesLoh:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetHeapAllocData(uint count, void* data, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetHeapAllocData(count, data, pNeeded) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface.GetFailedAssemblyList(ClrDataAddress appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[] values, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFailedAssemblyList(appDomain, count, values, pNeeded) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface.GetPrivateBinPaths(ClrDataAddress appDomain, int count, char* paths, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetPrivateBinPaths(appDomain, count, paths, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetPrivateBinPaths(appDomain, count, paths, pNeeded);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetAssemblyLocation(ClrDataAddress assembly, int count, char* location, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAssemblyLocation(assembly, count, location, pNeeded) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface.GetAppDomainConfigFile(ClrDataAddress appDomain, int count, char* configFile, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAppDomainConfigFile(appDomain, count, configFile, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetAppDomainConfigFile(appDomain, count, configFile, pNeeded);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetApplicationBase(ClrDataAddress appDomain, int count, char* appBase, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetApplicationBase(appDomain, count, appBase, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetApplicationBase(appDomain, count, appBase, pNeeded);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetFailedAssemblyData(ClrDataAddress assembly, uint* pContext, int* pResult)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFailedAssemblyData(assembly, pContext, pResult) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface.GetFailedAssemblyLocation(ClrDataAddress assembly, uint count, char* location, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFailedAssemblyLocation(assembly, count, location, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] locationLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = locationLocal)
            {
                hrLocal = _legacyImpl.GetFailedAssemblyLocation(assembly, count, ptr, &neededLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(location == null || new ReadOnlySpan<char>(locationLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(location)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetFailedAssemblyDisplayName(ClrDataAddress assembly, uint count, char* name, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFailedAssemblyDisplayName(assembly, count, name, pNeeded) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion ISOSDacInterface

    #region ISOSDacInterface2
    int ISOSDacInterface2.GetObjectExceptionData(ClrDataAddress objectAddress, DacpExceptionObjectData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl2 is not null ? _cdacImpl2.GetObjectExceptionData(objectAddress, data) : HResults.E_NOTIMPL;
        return hr;
    }

    int ISOSDacInterface2.IsRCWDCOMProxy(ClrDataAddress rcwAddress, int* inDCOMProxy)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl2 is not null ? _cdacImpl2.IsRCWDCOMProxy(rcwAddress, inDCOMProxy) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl2 is not null)
        {
            int inDCOMProxyLocal;
            int hrLocal = _legacyImpl2.IsRCWDCOMProxy(rcwAddress, &inDCOMProxyLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*inDCOMProxy == inDCOMProxyLocal);
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface2

    #region ISOSDacInterface3
    int ISOSDacInterface3.GetGCInterestingInfoData(ClrDataAddress interestingInfoAddr, DacpGCInterestingInfoData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl3 is not null ? _cdacImpl3.GetGCInterestingInfoData(interestingInfoAddr, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl3 is not null)
        {
            DacpGCInterestingInfoData dataLocal = default;
            int hrLocal = _legacyImpl3.GetGCInterestingInfoData(interestingInfoAddr, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                VerifyGCInterestingInfoData(data, &dataLocal);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface3.GetGCInterestingInfoStaticData(DacpGCInterestingInfoData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl3 is not null ? _cdacImpl3.GetGCInterestingInfoStaticData(data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl3 is not null)
        {
            DacpGCInterestingInfoData dataLocal = default;
            int hrLocal = _legacyImpl3.GetGCInterestingInfoStaticData(&dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                VerifyGCInterestingInfoData(data, &dataLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface3.GetGCGlobalMechanisms(nuint* globalMechanisms)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl3 is not null ? _cdacImpl3.GetGCGlobalMechanisms(globalMechanisms) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl3 is not null)
        {
            nuint[] globalMechanismsLocal = new nuint[GCConstants.DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT];
            fixed (nuint* pLocal = globalMechanismsLocal)
            {
                int hrLocal = _legacyImpl3.GetGCGlobalMechanisms(pLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                {
                    for (int i = 0; i < GCConstants.DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT; i++)
                    {
                        Debug.Assert(globalMechanisms[i] == globalMechanismsLocal[i],
                            $"globalMechanisms[{i}] - cDAC: {globalMechanisms[i]}, DAC: {globalMechanismsLocal[i]}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface3

    #region ISOSDacInterface4
    int ISOSDacInterface4.GetClrNotification(ClrDataAddress[] arguments, int count, int* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl4 is not null ? _cdacImpl4.GetClrNotification(arguments, count, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl4 is not null)
        {
            ClrDataAddress[] argumentsLocal = new ClrDataAddress[count];
            int neededLocal;
            int hrLocal = _legacyImpl4.GetClrNotification(argumentsLocal, count, &neededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pNeeded == neededLocal);
                for (int i = 0; i < count && i < neededLocal; i++)
                {
                    Debug.Assert(arguments[i] == argumentsLocal[i]);
                }
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface4

    #region ISOSDacInterface5
    int ISOSDacInterface5.GetTieredVersions(ClrDataAddress methodDesc,
        int rejitId,
        [In, MarshalUsing(CountElementName = nameof(cNativeCodeAddrs)), Out] DacpTieredVersionData[]? nativeCodeAddrs,
        int cNativeCodeAddrs,
        int* pcNativeCodeAddrs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl5 is not null ? _cdacImpl5.GetTieredVersions(methodDesc, rejitId, nativeCodeAddrs, cNativeCodeAddrs, pcNativeCodeAddrs) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl5 is not null)
        {
            var legacyBuffer = new DacpTieredVersionData[cNativeCodeAddrs];
            int legacyCount;
            int hrLocal = _legacyImpl5.GetTieredVersions(methodDesc, rejitId, legacyBuffer, cNativeCodeAddrs, &legacyCount);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*pcNativeCodeAddrs == legacyCount, $"cDAC count: {*pcNativeCodeAddrs}, DAC count: {legacyCount}");
                if (nativeCodeAddrs is not null)
                {
                    for (int i = 0; i < *pcNativeCodeAddrs; i++)
                    {
                        Debug.Assert(nativeCodeAddrs[i].nativeCodeAddr == legacyBuffer[i].nativeCodeAddr,
                            $"[{i}] cDAC nativeCodeAddr: 0x{(ulong)nativeCodeAddrs[i].nativeCodeAddr:x}, DAC: 0x{(ulong)legacyBuffer[i].nativeCodeAddr:x}");
                        Debug.Assert(nativeCodeAddrs[i].nativeCodeVersionNodePtr == legacyBuffer[i].nativeCodeVersionNodePtr,
                            $"[{i}] cDAC nodePtr: 0x{(ulong)nativeCodeAddrs[i].nativeCodeVersionNodePtr:x}, DAC: 0x{(ulong)legacyBuffer[i].nativeCodeVersionNodePtr:x}");
                        Debug.Assert(nativeCodeAddrs[i].optimizationTier == legacyBuffer[i].optimizationTier,
                            $"[{i}] cDAC tier: {nativeCodeAddrs[i].optimizationTier}, DAC: {legacyBuffer[i].optimizationTier}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface5

    #region ISOSDacInterface6
    int ISOSDacInterface6.GetMethodTableCollectibleData(ClrDataAddress mt, DacpMethodTableCollectibleData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl6 is not null ? _cdacImpl6.GetMethodTableCollectibleData(mt, data) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl6 is not null)
        {
            DacpMethodTableCollectibleData dataLocal;
            int hrLocal = _legacyImpl6.GetMethodTableCollectibleData(mt, &dataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert((data->bCollectible == 0) == (dataLocal.bCollectible == 0), $"cDAC: {data->bCollectible}, DAC: {dataLocal.bCollectible}");
                Debug.Assert(data->LoaderAllocatorObjectHandle == dataLocal.LoaderAllocatorObjectHandle, $"cDAC: {data->LoaderAllocatorObjectHandle:x}, DAC: {dataLocal.LoaderAllocatorObjectHandle:x}");
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface6

    #region ISOSDacInterface7
    int ISOSDacInterface7.GetPendingReJITID(ClrDataAddress methodDesc, int* pRejitId)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl7 is not null ? _cdacImpl7.GetPendingReJITID(methodDesc, pRejitId) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl7 is not null)
        {
            int rejitIdLocal;
            int hrLocal = _legacyImpl7.GetPendingReJITID(methodDesc, &rejitIdLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pRejitId == rejitIdLocal);
            }
        }

#endif
        return hr;
    }

    int ISOSDacInterface7.GetReJITInformation(ClrDataAddress methodDesc, int rejitId, DacpReJitData2* pRejitData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl7 is not null ? _cdacImpl7.GetReJITInformation(methodDesc, rejitId, pRejitData) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl7 is not null)
        {
            DacpReJitData2 rejitDataLocal;
            int hrLocal = _legacyImpl7.GetReJITInformation(methodDesc, rejitId, &rejitDataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pRejitData->rejitID == rejitDataLocal.rejitID);
                Debug.Assert(pRejitData->il == rejitDataLocal.il);
                Debug.Assert(pRejitData->flags == rejitDataLocal.flags);
                Debug.Assert(pRejitData->ilCodeVersionNodePtr == rejitDataLocal.ilCodeVersionNodePtr);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface7.GetProfilerModifiedILInformation(ClrDataAddress methodDesc, DacpProfilerILData* pILData)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl7 is not null ? _cdacImpl7.GetProfilerModifiedILInformation(methodDesc, pILData) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl7 is not null)
        {
            DacpProfilerILData ilDataLocal;
            int hrLocal = _legacyImpl7.GetProfilerModifiedILInformation(methodDesc, &ilDataLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pILData->type == ilDataLocal.type, $"cDAC: {pILData->type}, DAC: {ilDataLocal.type}");
                Debug.Assert(pILData->rejitID == ilDataLocal.rejitID, $"cDAC: {pILData->rejitID}, DAC: {ilDataLocal.rejitID}");
                Debug.Assert(pILData->il == ilDataLocal.il, $"cDAC: {pILData->il:x}, DAC: {ilDataLocal.il:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface7.GetMethodsWithProfilerModifiedIL(ClrDataAddress mod, ClrDataAddress* methodDescs, int cMethodDescs, int* pcMethodDescs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl7 is not null ? _cdacImpl7.GetMethodsWithProfilerModifiedIL(mod, methodDescs, cMethodDescs, pcMethodDescs) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl7 is not null)
        {
            ClrDataAddress[] methodDescsLocal = new ClrDataAddress[cMethodDescs];
            int pcMethodDescsLocal;
            int hrLocal;
            fixed (ClrDataAddress* ptr = methodDescsLocal)
            {
                hrLocal = _legacyImpl7.GetMethodsWithProfilerModifiedIL(mod, ptr, cMethodDescs, &pcMethodDescsLocal);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pcMethodDescs == pcMethodDescsLocal, $"cDAC: {*pcMethodDescs}, DAC: {pcMethodDescsLocal}");
                for (int i = 0; i < *pcMethodDescs; i++)
                {
                    Debug.Assert(methodDescs[i] == methodDescsLocal[i], $"cDAC: {methodDescs[i]:x}, DAC: {methodDescsLocal[i]:x}");
                }
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface7

    #region ISOSDacInterface8
    int ISOSDacInterface8.GetNumberGenerations(uint* pGenerations)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl8 is not null ? _cdacImpl8.GetNumberGenerations(pGenerations) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl8 is not null)
        {
            uint pGenerationsLocal;
            int hrLocal = _legacyImpl8.GetNumberGenerations(&pGenerationsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pGenerations == pGenerationsLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface8.GetGenerationTable(uint cGenerations, DacpGenerationData* pGenerationData, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl8 is not null ? _cdacImpl8.GetGenerationTable(cGenerations, pGenerationData, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl8 is not null)
        {
            uint pNeededLocal;
            DacpGenerationData[]? genDataLocal = cGenerations > 0 ? new DacpGenerationData[cGenerations] : null;
            fixed (DacpGenerationData* pGenDataLocal = genDataLocal)
            {
                int hrLocal = _legacyImpl8.GetGenerationTable(cGenerations, pGenDataLocal, &pNeededLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (pNeeded is not null)
                {
                    Debug.Assert(*pNeeded == pNeededLocal);
                }
                if (hr == HResults.S_OK && pGenerationData is not null)
                {
                    for (int i = 0; i < (int)pNeededLocal; i++)
                    {
                        Debug.Assert(pGenDataLocal[i].start_segment == pGenerationData[i].start_segment);
                        Debug.Assert(pGenDataLocal[i].allocation_start == pGenerationData[i].allocation_start);
                        Debug.Assert(pGenDataLocal[i].allocContextPtr == pGenerationData[i].allocContextPtr);
                        Debug.Assert(pGenDataLocal[i].allocContextLimit == pGenerationData[i].allocContextLimit);
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface8.GetFinalizationFillPointers(uint cFillPointers, ClrDataAddress* pFinalizationFillPointers, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl8 is not null ? _cdacImpl8.GetFinalizationFillPointers(cFillPointers, pFinalizationFillPointers, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl8 is not null)
        {
            uint pNeededLocal;
            ClrDataAddress[]? fillPointersLocal = cFillPointers > 0 ? new ClrDataAddress[cFillPointers] : null;
            fixed (ClrDataAddress* pFillPointersLocal = fillPointersLocal)
            {
                int hrLocal = _legacyImpl8.GetFinalizationFillPointers(cFillPointers, pFillPointersLocal, &pNeededLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (pNeeded is not null)
                {
                    Debug.Assert(*pNeeded == pNeededLocal);
                }
                if (hr == HResults.S_OK && pFinalizationFillPointers is not null)
                {
                    for (int i = 0; i < (int)pNeededLocal; i++)
                    {
                        Debug.Assert(pFillPointersLocal[i] == pFinalizationFillPointers[i]);
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface8.GetGenerationTableSvr(ClrDataAddress heapAddr, uint cGenerations, DacpGenerationData* pGenerationData, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl8 is not null ? _cdacImpl8.GetGenerationTableSvr(heapAddr, cGenerations, pGenerationData, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl8 is not null)
        {
            uint pNeededLocal;
            DacpGenerationData[]? genDataLocal = cGenerations > 0 ? new DacpGenerationData[cGenerations] : null;
            fixed (DacpGenerationData* pGenDataLocal = genDataLocal)
            {
                int hrLocal = _legacyImpl8.GetGenerationTableSvr(heapAddr, cGenerations, pGenDataLocal, &pNeededLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (pNeeded is not null)
                {
                    Debug.Assert(*pNeeded == pNeededLocal);
                }
                if (hr == HResults.S_OK && pGenerationData is not null)
                {
                    for (int i = 0; i < (int)pNeededLocal; i++)
                    {
                        Debug.Assert(pGenDataLocal[i].start_segment == pGenerationData[i].start_segment);
                        Debug.Assert(pGenDataLocal[i].allocation_start == pGenerationData[i].allocation_start);
                        Debug.Assert(pGenDataLocal[i].allocContextPtr == pGenerationData[i].allocContextPtr);
                        Debug.Assert(pGenDataLocal[i].allocContextLimit == pGenerationData[i].allocContextLimit);
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface8.GetFinalizationFillPointersSvr(ClrDataAddress heapAddr, uint cFillPointers, ClrDataAddress* pFinalizationFillPointers, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl8 is not null ? _cdacImpl8.GetFinalizationFillPointersSvr(heapAddr, cFillPointers, pFinalizationFillPointers, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl8 is not null)
        {
            uint pNeededLocal;
            ClrDataAddress[]? fillPointersLocal = cFillPointers > 0 ? new ClrDataAddress[cFillPointers] : null;
            fixed (ClrDataAddress* pFillPointersLocal = fillPointersLocal)
            {
                int hrLocal = _legacyImpl8.GetFinalizationFillPointersSvr(heapAddr, cFillPointers, pFillPointersLocal, &pNeededLocal);
                Debug.ValidateHResult(hr, hrLocal);
                if (pNeeded is not null)
                {
                    Debug.Assert(*pNeeded == pNeededLocal);
                }
                if (hr == HResults.S_OK && pFinalizationFillPointers is not null)
                {
                    int fillPointersToCompare = (int)Math.Min(cFillPointers, pNeededLocal);
                    for (int i = 0; i < fillPointersToCompare; i++)
                    {
                        Debug.Assert(pFillPointersLocal[i] == pFinalizationFillPointers[i]);
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface8.GetAssemblyLoadContext(ClrDataAddress methodTable, ClrDataAddress* assemblyLoadContext)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl8 is not null ? _cdacImpl8.GetAssemblyLoadContext(methodTable, assemblyLoadContext) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl8 is not null)
        {
            ClrDataAddress assemblyLoadContextLocal;
            int hrLocal = _legacyImpl8.GetAssemblyLoadContext(methodTable, &assemblyLoadContextLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*assemblyLoadContext == assemblyLoadContextLocal);
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface8

    #region ISOSDacInterface9
    int ISOSDacInterface9.GetBreakingChangeVersion()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int version = _cdacImpl9 is not null ? _cdacImpl9.GetBreakingChangeVersion() : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl9 is not null)
        {
            Debug.Assert(version == _legacyImpl9.GetBreakingChangeVersion());
        }
#endif
        return version;
    }

    #endregion ISOSDacInterface9

    #region ISOSDacInterface10
    int ISOSDacInterface10.GetObjectComWrappersData(ClrDataAddress objAddr, ClrDataAddress* rcw, uint count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[]? mowList, uint* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl10 is not null ? _cdacImpl10.GetObjectComWrappersData(objAddr, rcw, count, mowList, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl10 is not null)
        {
            ClrDataAddress rcwLocal = 0;
            uint neededLocal = 0;
            ClrDataAddress[]? mowListLocal =  count > 0 ? new ClrDataAddress[count] : null;
            int hrLocal = _legacyImpl10.GetObjectComWrappersData(objAddr, rcw == null ? null : &rcwLocal, count, mowListLocal, &neededLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                if (rcw != null)
                    Debug.Assert(*rcw == rcwLocal);
                if (pNeeded != null)
                    Debug.Assert(*pNeeded == neededLocal);
                if (mowList != null)
                {
                    for (int i = 0; i < (int)neededLocal && i < count; i++)
                    {
                        Debug.Assert(mowList[i] == mowListLocal![i]);
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface10.IsComWrappersCCW(ClrDataAddress ccw, Interop.BOOL* isComWrappersCCW)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl10 is not null ? _cdacImpl10.IsComWrappersCCW(ccw, isComWrappersCCW) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl10 is not null)
        {
            Interop.BOOL isComWrappersCCWLocal;
            int hrLocal = _legacyImpl10.IsComWrappersCCW(ccw, &isComWrappersCCWLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*isComWrappersCCW == isComWrappersCCWLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface10.GetComWrappersCCWData(ClrDataAddress ccw, ClrDataAddress* managedObject, int* refCount)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl10 is not null ? _cdacImpl10.GetComWrappersCCWData(ccw, managedObject, refCount) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl10 is not null)
        {
            ClrDataAddress managedObjectLocal;
            int refCountLocal;
            int hrLocal = _legacyImpl10.GetComWrappersCCWData(ccw, &managedObjectLocal, &refCountLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                if (managedObject != null)
                    Debug.Assert(*managedObject == managedObjectLocal);
                if (refCount != null)
                    Debug.Assert(*refCount == refCountLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface10.IsComWrappersRCW(ClrDataAddress rcw, Interop.BOOL* isComWrappersRCW)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl10 is not null ? _cdacImpl10.IsComWrappersRCW(rcw, isComWrappersRCW) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl10 is not null)
        {
            Interop.BOOL isComWrappersRCWLocal;
            int hrLocal = _legacyImpl10.IsComWrappersRCW(rcw, &isComWrappersRCWLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*isComWrappersRCW == isComWrappersRCWLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface10.GetComWrappersRCWData(ClrDataAddress rcw, ClrDataAddress* identity)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl10 is not null ? _cdacImpl10.GetComWrappersRCWData(rcw, identity) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl10 is not null)
        {
            ClrDataAddress identityLocal;
            int hrLocal = _legacyImpl10.GetComWrappersRCWData(rcw, &identityLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*identity == identityLocal);
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface10

    #region ISOSDacInterface11
    int ISOSDacInterface11.IsTrackedType(ClrDataAddress objAddr, Interop.BOOL* isTrackedType, Interop.BOOL* hasTaggedMemory)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl11 is not null ? _cdacImpl11.IsTrackedType(objAddr, isTrackedType, hasTaggedMemory) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl11 is not null)
        {
            Interop.BOOL isTrackedTypeLocal;
            Interop.BOOL hasTaggedMemoryLocal;
            int hrLocal = _legacyImpl11.IsTrackedType(objAddr, &isTrackedTypeLocal, &hasTaggedMemoryLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*isTrackedType == isTrackedTypeLocal);
                Debug.Assert(*hasTaggedMemory == hasTaggedMemoryLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface11.GetTaggedMemory(ClrDataAddress objAddr, ClrDataAddress* taggedMemory, nuint* taggedMemorySizeInBytes)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl11 is not null ? _cdacImpl11.GetTaggedMemory(objAddr, taggedMemory, taggedMemorySizeInBytes) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl11 is not null)
        {
            ClrDataAddress taggedMemoryLocal;
            nuint taggedMemorySizeInBytesLocal;
            int hrLocal = _legacyImpl11.GetTaggedMemory(objAddr, &taggedMemoryLocal, &taggedMemorySizeInBytesLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*taggedMemory == taggedMemoryLocal);
                Debug.Assert(*taggedMemorySizeInBytes == taggedMemorySizeInBytesLocal);
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface11

    #region ISOSDacInterface12
    int ISOSDacInterface12.GetGlobalAllocationContext(ClrDataAddress* allocPtr, ClrDataAddress* allocLimit)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl12 is not null ? _cdacImpl12.GetGlobalAllocationContext(allocPtr, allocLimit) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl12 is not null)
        {
            ClrDataAddress allocPtrLocal = default;
            ClrDataAddress allocLimitLocal = default;
            int hrLocal = _legacyImpl12.GetGlobalAllocationContext(&allocPtrLocal, &allocLimitLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*allocPtr == allocPtrLocal);
                Debug.Assert(*allocLimit == allocLimitLocal);
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface12

    #region ISOSDacInterface13
    int ISOSDacInterface13.TraverseLoaderHeap(ClrDataAddress loaderHeapAddr, int kind, delegate* unmanaged<ulong, nuint, Interop.BOOL, void> pCallback)
    {
        using ShimCall shimCall = ShimCall.Enter();
#if DEBUG
        DebugTraverseLoaderHeapBlocks.Clear();
        _debugTraverseLoaderDebugCount = 0;
        TraverseLoaderHeapRecordingContext? previousContext = _recordingTraverseLoaderHeapContext;
        delegate* unmanaged<ulong, nuint, Interop.BOOL, void> cdacCallback = pCallback;
        if (pCallback is not null)
        {
            _recordingTraverseLoaderHeapContext = new TraverseLoaderHeapRecordingContext { Callback = pCallback };
            cdacCallback = &RecordingTraverseLoaderHeapCallback;
        }
        int hr;
        try
        {
            hr = _cdacImpl13 is not null ? _cdacImpl13.TraverseLoaderHeap(loaderHeapAddr, kind, cdacCallback) : HResults.E_NOTIMPL;
        }
        finally
        {
            _recordingTraverseLoaderHeapContext = previousContext;
        }
        if (_legacyImpl13 is not null)
        {
            int cdacCount = DebugTraverseLoaderHeapBlocks.Count;
            delegate* unmanaged<ulong, nuint, Interop.BOOL, void> debugCallbackPtr = &TraverseLoaderHeapDebugCallback;
            int hrLocal = _legacyImpl13.TraverseLoaderHeap(loaderHeapAddr, kind, debugCallbackPtr);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(DebugTraverseLoaderHeapBlocks.Count == 0,
                    $"cDAC found {cdacCount} blocks, DAC matched {_debugTraverseLoaderDebugCount}, {DebugTraverseLoaderHeapBlocks.Count} unmatched");
                Debug.Assert(_debugTraverseLoaderDebugCount == (uint)cdacCount,
                    $"cDAC: {cdacCount} blocks, DAC: {_debugTraverseLoaderDebugCount} blocks");
            }
        }
#else
        int hr = _cdacImpl13 is not null ? _cdacImpl13.TraverseLoaderHeap(loaderHeapAddr, kind, pCallback) : HResults.E_NOTIMPL;
#endif
        return hr;
    }

    int ISOSDacInterface13.GetDomainLoaderAllocator(ClrDataAddress domainAddress, ClrDataAddress* pLoaderAllocator)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl13 is not null ? _cdacImpl13.GetDomainLoaderAllocator(domainAddress, pLoaderAllocator) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl13 is not null)
        {
            ClrDataAddress pLoaderAllocatorLocal;
            int hrLocal = _legacyImpl13.GetDomainLoaderAllocator(domainAddress, &pLoaderAllocatorLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*pLoaderAllocator == pLoaderAllocatorLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface13.GetLoaderAllocatorHeapNames(int count, char** ppNames, int* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl13 is not null ? _cdacImpl13.GetLoaderAllocatorHeapNames(count, ppNames, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl13 is not null)
        {
            int pNeededLocal;
            _legacyImpl13.GetLoaderAllocatorHeapNames(0, null, &pNeededLocal);
            Debug.Assert(pNeeded is null || *pNeeded == pNeededLocal, $"cDAC needed: {(pNeeded != null ? *pNeeded : -1)}, DAC needed: {pNeededLocal}");
            if (hr >= 0 && ppNames != null && pNeededLocal > 0)
            {
                char** ppNamesLocal = stackalloc char*[pNeededLocal];
                _legacyImpl13.GetLoaderAllocatorHeapNames(pNeededLocal, ppNamesLocal, null);
                int compareCount = Math.Min(count, pNeededLocal);
                for (int i = 0; i < compareCount; i++)
                {
                    string cdacName = Marshal.PtrToStringAnsi((nint)ppNames[i])!;
                    string dacName = Marshal.PtrToStringAnsi((nint)ppNamesLocal[i])!;
                    Debug.Assert(cdacName == dacName, $"HeapName[{i}] - cDAC: {cdacName}, DAC: {dacName}");
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface13.GetLoaderAllocatorHeaps(ClrDataAddress loaderAllocator, int count, ClrDataAddress* pLoaderHeaps, /*LoaderHeapKind*/ int* pKinds, int* pNeeded)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl13 is not null ? _cdacImpl13.GetLoaderAllocatorHeaps(loaderAllocator, count, pLoaderHeaps, pKinds, pNeeded) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl13 is not null)
        {
            int pNeededLocal;
            int hrLocal = _legacyImpl13.GetLoaderAllocatorHeaps(loaderAllocator, 0, null, null, &pNeededLocal);
            Debug.Assert(pNeeded is null || *pNeeded == pNeededLocal, $"cDAC needed: {(pNeeded != null ? *pNeeded : -1)}, DAC needed: {pNeededLocal}");
            if (hr >= 0 && pLoaderHeaps != null && pNeededLocal > 0)
            {
                ClrDataAddress* pLoaderHeapsLocal = stackalloc ClrDataAddress[pNeededLocal];
                int* pKindsLocal = stackalloc int[pNeededLocal];
                hrLocal = _legacyImpl13.GetLoaderAllocatorHeaps(loaderAllocator, pNeededLocal, pLoaderHeapsLocal, pKindsLocal, null);
                Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
                if (hrLocal >= 0)
                {
                    for (int i = 0; i < pNeededLocal; i++)
                    {
                        Debug.Assert(pLoaderHeaps[i] == pLoaderHeapsLocal[i], $"Heap[{i}] - cDAC: {pLoaderHeaps[i]:x}, DAC: {pLoaderHeapsLocal[i]:x}");
                        Debug.Assert(pKinds[i] == pKindsLocal[i], $"Kind[{i}] - cDAC: {pKinds[i]}, DAC: {pKindsLocal[i]}");
                    }
                }
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface13.GetHandleTableMemoryRegions(DacComNullableByRef<ISOSMemoryEnum> ppEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<ISOSMemoryEnum> ppEnumCDac = new(ppEnum.IsNullRef);
        DacComNullableByRef<ISOSMemoryEnum> ppEnumDac = new(ppEnum.IsNullRef);
        int hr = _cdacImpl13 is not null ? _cdacImpl13.GetHandleTableMemoryRegions(ppEnumCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl13 is not null)
        {
            hrLocal = _legacyImpl13.GetHandleTableMemoryRegions(ppEnumDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!ppEnum.IsNullRef)
            ppEnum.Interface = ShimProxy.PairISOSMemoryEnum(_session, ppEnumCDac.Interface, ppEnumDac.Interface);
        return hr;
    }

    int ISOSDacInterface13.GetGCBookkeepingMemoryRegions(DacComNullableByRef<ISOSMemoryEnum> ppEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<ISOSMemoryEnum> ppEnumCDac = new(ppEnum.IsNullRef);
        DacComNullableByRef<ISOSMemoryEnum> ppEnumDac = new(ppEnum.IsNullRef);
        int hr = _cdacImpl13 is not null ? _cdacImpl13.GetGCBookkeepingMemoryRegions(ppEnumCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl13 is not null)
        {
            hrLocal = _legacyImpl13.GetGCBookkeepingMemoryRegions(ppEnumDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!ppEnum.IsNullRef)
            ppEnum.Interface = ShimProxy.PairISOSMemoryEnum(_session, ppEnumCDac.Interface, ppEnumDac.Interface);
        return hr;
    }

    int ISOSDacInterface13.GetGCFreeRegions(DacComNullableByRef<ISOSMemoryEnum> ppEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<ISOSMemoryEnum> ppEnumCDac = new(ppEnum.IsNullRef);
        DacComNullableByRef<ISOSMemoryEnum> ppEnumDac = new(ppEnum.IsNullRef);
        int hr = _cdacImpl13 is not null ? _cdacImpl13.GetGCFreeRegions(ppEnumCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl13 is not null)
        {
            hrLocal = _legacyImpl13.GetGCFreeRegions(ppEnumDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!ppEnum.IsNullRef)
            ppEnum.Interface = ShimProxy.PairISOSMemoryEnum(_session, ppEnumCDac.Interface, ppEnumDac.Interface);
        return hr;
    }
    int ISOSDacInterface13.LockedFlush()
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl13 is not null ? _cdacImpl13.LockedFlush() : HResults.E_NOTIMPL;
        if (_legacyImpl13 is not null)
        {
            _legacyImpl13.LockedFlush();
        }
        return hr;
    }

    #endregion ISOSDacInterface13

    #region ISOSDacInterface14
    int ISOSDacInterface14.GetStaticBaseAddress(ClrDataAddress methodTable, ClrDataAddress* nonGCStaticsAddress, ClrDataAddress* GCStaticsAddress)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl14 is not null ? _cdacImpl14.GetStaticBaseAddress(methodTable, nonGCStaticsAddress, GCStaticsAddress) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl14 is not null)
        {
            ClrDataAddress nonGCStaticsAddressLocal;
            ClrDataAddress GCStaticsAddressLocal;
            int hrLocal = _legacyImpl14.GetStaticBaseAddress(methodTable, &nonGCStaticsAddressLocal, &GCStaticsAddressLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                if (GCStaticsAddress != null)
                    Debug.Assert(*GCStaticsAddress == GCStaticsAddressLocal);
                if (nonGCStaticsAddress != null)
                    Debug.Assert(*nonGCStaticsAddress == nonGCStaticsAddressLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface14.GetThreadStaticBaseAddress(ClrDataAddress methodTable, ClrDataAddress thread, ClrDataAddress* nonGCStaticsAddress, ClrDataAddress* GCStaticsAddress)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl14 is not null ? _cdacImpl14.GetThreadStaticBaseAddress(methodTable, thread, nonGCStaticsAddress, GCStaticsAddress) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl14 is not null)
        {
            ClrDataAddress nonGCStaticsAddressLocal = default;
            ClrDataAddress GCStaticsAddressLocal = default;
            ClrDataAddress* nonGCStaticsAddressOrNull = nonGCStaticsAddress != null ? &nonGCStaticsAddressLocal : null;
            ClrDataAddress* gcStaticsAddressOrNull = GCStaticsAddress != null ? &GCStaticsAddressLocal : null;
            int hrLocal = _legacyImpl14.GetThreadStaticBaseAddress(methodTable, thread, nonGCStaticsAddressOrNull, gcStaticsAddressOrNull);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                if (nonGCStaticsAddress != null)
                    Debug.Assert(*nonGCStaticsAddress == nonGCStaticsAddressLocal);
                if (GCStaticsAddress != null)
                    Debug.Assert(*GCStaticsAddress == GCStaticsAddressLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface14.GetMethodTableInitializationFlags(ClrDataAddress methodTable, MethodTableInitializationFlags* initializationStatus)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl14 is not null ? _cdacImpl14.GetMethodTableInitializationFlags(methodTable, initializationStatus) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl14 is not null)
        {
            MethodTableInitializationFlags initializationStatusLocal;
            int hrLocal = _legacyImpl14.GetMethodTableInitializationFlags(methodTable, &initializationStatusLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*initializationStatus == initializationStatusLocal);
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface14

    #region ISOSDacInterface15
    int ISOSDacInterface15.GetMethodTableSlotEnumerator(ClrDataAddress mt, DacComNullableByRef<ISOSMethodEnum> enumerator)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<ISOSMethodEnum> enumeratorCDac = new(enumerator.IsNullRef);
        DacComNullableByRef<ISOSMethodEnum> enumeratorDac = new(enumerator.IsNullRef);
        int hr = _cdacImpl15 is not null ? _cdacImpl15.GetMethodTableSlotEnumerator(mt, enumeratorCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyImpl15 is not null)
        {
            hrLocal = _legacyImpl15.GetMethodTableSlotEnumerator(mt, enumeratorDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!enumerator.IsNullRef)
            enumerator.Interface = ShimProxy.PairISOSMethodEnum(_session, enumeratorCDac.Interface, enumeratorDac.Interface);
        return hr;
    }

    #endregion ISOSDacInterface15

    #region ISOSDacInterface16
    int ISOSDacInterface16.GetGCDynamicAdaptationMode(int* pDynamicAdaptationMode)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl16 is not null ? _cdacImpl16.GetGCDynamicAdaptationMode(pDynamicAdaptationMode) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyImpl16 is not null)
        {
            int dynamicAdaptationModeLocal;
            int hrLocal = _legacyImpl16.GetGCDynamicAdaptationMode(&dynamicAdaptationModeLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(pDynamicAdaptationMode == null || *pDynamicAdaptationMode == dynamicAdaptationModeLocal);
            }
        }
#endif
        return hr;
    }

    #endregion ISOSDacInterface16

    #region ISOSDacInterface17
    int ISOSDacInterface17.GetStressLogData(SOSStressLogData* data)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl17 is not null ? _cdacImpl17.GetStressLogData(data) : HResults.E_NOTIMPL;
        return hr;
    }
    int ISOSDacInterface17.GetStressLogThreadEnumerator(DacComNullableByRef<ISOSStressLogThreadEnum> ppEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl17 is not null ? _cdacImpl17.GetStressLogThreadEnumerator(ppEnum) : HResults.E_NOTIMPL;
        return hr;
    }
    int ISOSDacInterface17.GetStressLogMessageEnumerator(ClrDataAddress threadStressLogAddress,
        DacComNullableByRef<ISOSStressLogMsgEnum> ppEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl17 is not null ? _cdacImpl17.GetStressLogMessageEnumerator(threadStressLogAddress, ppEnum) : HResults.E_NOTIMPL;
        return hr;
    }
    int ISOSDacInterface17.GetStressLogMemoryRanges(DacComNullableByRef<ISOSMemoryEnum> ppEnum)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl17 is not null ? _cdacImpl17.GetStressLogMemoryRanges(ppEnum) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion ISOSDacInterface17

    #region IXCLRDataProcess
    int IXCLRDataProcess.StartEnumTasks(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.StartEnumTasks(handle) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.EnumTask(ulong* handle, DacComNullableByRef<IXCLRDataTask> task)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.EnumTask(handle, task) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.EndEnumTasks(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.EndEnumTasks(handle) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.GetTaskByOSThreadID(uint osThreadID, DacComNullableByRef<IXCLRDataTask> task)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTask> taskCDac = new(task.IsNullRef);
        DacComNullableByRef<IXCLRDataTask> taskDac = new(task.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.GetTaskByOSThreadID(osThreadID, taskCDac) : HResults.E_NOTIMPL;
        if (_legacyProcess is not null)
        {
            _legacyProcess.GetTaskByOSThreadID(osThreadID, taskDac);
        }
        if (!task.IsNullRef)
            task.Interface = ShimProxy.PairIXCLRDataTask(_session, taskCDac.Interface, taskDac.Interface);
        return hr;
    }

    int IXCLRDataProcess.GetTaskByUniqueID(ulong taskID, DacComNullableByRef<IXCLRDataTask> task)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataTask> taskCDac = new(task.IsNullRef);
        DacComNullableByRef<IXCLRDataTask> taskDac = new(task.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.GetTaskByUniqueID(taskID, taskCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.GetTaskByUniqueID(taskID, taskDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!task.IsNullRef)
            task.Interface = ShimProxy.PairIXCLRDataTask(_session, taskCDac.Interface, taskDac.Interface);
        return hr;
    }

    int IXCLRDataProcess.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetFlags(flags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataProcess.IsSameObject(IXCLRDataProcess* process)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.IsSameObject(process) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetManagedObject(value) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataProcess.GetDesiredExecutionState(uint* state)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetDesiredExecutionState(state) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataProcess.SetDesiredExecutionState(uint state)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.SetDesiredExecutionState(state) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataProcess.GetAddressType(ClrDataAddress address, CLRDataAddressType* type)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetAddressType(address, type) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyProcess is not null)
        {
            CLRDataAddressType typeLocal = default;
            int hrLocal = _legacyProcess.GetAddressType(address, type is null ? null : &typeLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
            {
                Debug.Assert(*type == typeLocal, $"cDAC: {*type}, DAC: {typeLocal}");
            }
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.GetRuntimeNameByAddress(ClrDataAddress address,
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        ClrDataAddress* displacement)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetRuntimeNameByAddress(address, flags, bufLen, nameLen, nameBuf, displacement) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyProcess is not null)
        {
            uint nameLenLocal = 0;
            char[] nameBufLocal = new char[bufLen > 0 ? bufLen : 1];
            ClrDataAddress displacementLocal = default;
            int hrLocal;
            fixed (char* pNameBufLocal = nameBufLocal)
            {
                hrLocal = _legacyProcess.GetRuntimeNameByAddress(
                    address, flags, bufLen,
                    nameLen is null ? null : &nameLenLocal,
                    nameBuf is null ? null : pNameBufLocal,
                    displacement is null ? null : &displacementLocal);
            }

            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(nameLen is null || *nameLen == nameLenLocal);
                if (nameBuf is not null)
                {
                    Debug.Assert(new ReadOnlySpan<char>(nameBuf, (int)nameLenLocal)
                        .SequenceEqual(nameBufLocal.AsSpan(0, (int)nameLenLocal)));
                }
                if (displacement is not null)
                {
                    Debug.Assert(*displacement == displacementLocal);
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.StartEnumAppDomains(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacProcess is not null ? _cdacProcess.StartEnumAppDomains(handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.StartEnumAppDomains(handle is null ? null : &dacHandle);
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

    int IXCLRDataProcess.EnumAppDomain(ulong* handle, DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataAppDomain> appDomainCDac = new(appDomain.IsNullRef);
        DacComNullableByRef<IXCLRDataAppDomain> appDomainDac = new(appDomain.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.EnumAppDomain(handle is null ? null : &cdacHandle, appDomainCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            hrLocal = _legacyProcess.EnumAppDomain(handle is null ? null : &dacHandle, appDomainDac);
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
        if (!appDomain.IsNullRef)
            appDomain.Interface = ShimProxy.PairIXCLRDataAppDomain(_session, appDomainCDac.Interface, appDomainDac.Interface);
        return hr;
    }
    int IXCLRDataProcess.EndEnumAppDomains(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacProcess is not null ? _cdacProcess.EndEnumAppDomains(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            _legacyProcess.EndEnumAppDomains(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataProcess.GetAppDomainByUniqueID(ulong id, DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataAppDomain> appDomainCDac = new(appDomain.IsNullRef);
        DacComNullableByRef<IXCLRDataAppDomain> appDomainDac = new(appDomain.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.GetAppDomainByUniqueID(id, appDomainCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.GetAppDomainByUniqueID(id, appDomainDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!appDomain.IsNullRef)
            appDomain.Interface = ShimProxy.PairIXCLRDataAppDomain(_session, appDomainCDac.Interface, appDomainDac.Interface);
        return hr;
    }
    int IXCLRDataProcess.StartEnumAssemblies(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.StartEnumAssemblies(handle) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.EnumAssembly(ulong* handle, DacComNullableByRef<IXCLRDataAssembly> assembly)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.EnumAssembly(handle, assembly) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.EndEnumAssemblies(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.EndEnumAssemblies(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataProcess.StartEnumModules(ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacProcess is not null ? _cdacProcess.StartEnumModules(handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.StartEnumModules(handle is null ? null : &dacHandle);
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

    int IXCLRDataProcess.EnumModule(ulong* handle, DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> modDac = new(mod.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.EnumModule(handle is null ? null : &cdacHandle, modCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            hrLocal = _legacyProcess.EnumModule(handle is null ? null : &dacHandle, modDac);
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
        if (!mod.IsNullRef)
            mod.Interface = ShimProxy.PairIXCLRDataModule(_session, modCDac.Interface, modDac.Interface);
        return hr;
    }
    int IXCLRDataProcess.EndEnumModules(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacProcess is not null ? _cdacProcess.EndEnumModules(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            _legacyProcess.EndEnumModules(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataProcess.GetModuleByAddress(ClrDataAddress address, DacComNullableByRef<IXCLRDataModule> mod)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataModule> modCDac = new(mod.IsNullRef);
        DacComNullableByRef<IXCLRDataModule> modDac = new(mod.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.GetModuleByAddress(address, modCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.GetModuleByAddress(address, modDac);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!mod.IsNullRef)
            mod.Interface = ShimProxy.PairIXCLRDataModule(_session, modCDac.Interface, modDac.Interface);
        return hr;
    }

    int IXCLRDataProcess.StartEnumMethodInstancesByAddress(ClrDataAddress address, IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacProcess is not null ? _cdacProcess.StartEnumMethodInstancesByAddress(address, ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.StartEnumMethodInstancesByAddress(address, ShimProxy.UnwrapDac<IXCLRDataAppDomain>(appDomain), handle is null ? null : &dacHandle);
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

    int IXCLRDataProcess.EnumMethodInstanceByAddress(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataMethodInstance> methodCDac = new(method.IsNullRef);
        DacComNullableByRef<IXCLRDataMethodInstance> methodDac = new(method.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.EnumMethodInstanceByAddress(handle is null ? null : &cdacHandle, methodCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            hrLocal = _legacyProcess.EnumMethodInstanceByAddress(handle is null ? null : &dacHandle, methodDac);
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
            method.Interface = ShimProxy.PairIXCLRDataMethodInstance(_session, methodCDac.Interface, methodDac.Interface);
        return hr;
    }
    int IXCLRDataProcess.EndEnumMethodInstancesByAddress(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacProcess is not null ? _cdacProcess.EndEnumMethodInstancesByAddress(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            _legacyProcess.EndEnumMethodInstancesByAddress(pair is null ? handle : pair.DacHandle);
        }
        return hr;
    }

    int IXCLRDataProcess.GetDataByAddress(ClrDataAddress address,
        uint flags,
        IXCLRDataAppDomain? appDomain,
        IXCLRDataTask? tlsTask,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataValue> value,
        ClrDataAddress* displacement)
    {
        using ShimCall shimCall = ShimCall.Enter();
        DacComNullableByRef<IXCLRDataValue> valueCDac = new(value.IsNullRef);
        DacComNullableByRef<IXCLRDataValue> valueDac = new(value.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.GetDataByAddress(address, flags, ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), bufLen, nameLen, nameBuf, valueCDac, displacement) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.GetDataByAddress(address, flags, ShimProxy.UnwrapDac<IXCLRDataAppDomain>(appDomain), ShimProxy.UnwrapDac<IXCLRDataTask>(tlsTask), bufLen, nameLen, nameBuf, valueDac, displacement);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        if (!value.IsNullRef)
            value.Interface = ShimProxy.PairIXCLRDataValue(_session, valueCDac.Interface, valueDac.Interface);
        return hr;
    }
    int IXCLRDataProcess.GetExceptionStateByExceptionRecord(EXCEPTION_RECORD64* record, DacComNullableByRef<IXCLRDataExceptionState> exState)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetExceptionStateByExceptionRecord(record, exState) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataProcess.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyProcess is not null && LegacyFallbackHelper.CanFallback("Request", "SOSDacImpl.IXCLRDataProcess.cs"))
        {
            return _legacyProcess.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);
        }
        return hr;
    }
    int IXCLRDataProcess.CreateMemoryValue(IXCLRDataAppDomain? appDomain,
        IXCLRDataTask? tlsTask,
        IXCLRDataTypeInstance? type,
        ClrDataAddress addr,
        DacComNullableByRef<IXCLRDataValue> value)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.CreateMemoryValue(ShimProxy.UnwrapCDac<IXCLRDataAppDomain>(appDomain), ShimProxy.UnwrapCDac<IXCLRDataTask>(tlsTask), ShimProxy.UnwrapCDac<IXCLRDataTypeInstance>(type), addr, value) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.SetAllTypeNotifications(IXCLRDataModule? mod, uint flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.SetAllTypeNotifications(ShimProxy.UnwrapCDac<IXCLRDataModule>(mod), flags) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.SetAllCodeNotifications(IXCLRDataModule? mod, uint flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.SetAllCodeNotifications(ShimProxy.UnwrapCDac<IXCLRDataModule>(mod), flags) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.GetTypeNotifications(uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[]? tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetTypeNotifications(numTokens, mods, ShimProxy.UnwrapCDac<IXCLRDataModule>(singleMod), tokens, flags) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.SetTypeNotifications(uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[]? tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags,
        uint singleFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.SetTypeNotifications(numTokens, mods, ShimProxy.UnwrapCDac<IXCLRDataModule>(singleMod), tokens, flags, singleFlags) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.GetCodeNotifications(uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef*/ uint[]? tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetCodeNotifications(numTokens, mods, ShimProxy.UnwrapCDac<IXCLRDataModule>(singleMod), tokens, flags) : HResults.E_NOTIMPL;
        return hr;
    }
    int IXCLRDataProcess.SetCodeNotifications(uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef */ uint[]? tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags,
        uint singleFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.SetCodeNotifications(numTokens, mods, ShimProxy.UnwrapCDac<IXCLRDataModule>(singleMod), tokens, flags, singleFlags) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataProcess.GetOtherNotificationFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.GetOtherNotificationFlags(flags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyProcess is not null)
        {
            uint flagsLocal;
            int hrLocal = _legacyProcess.GetOtherNotificationFlags(&flagsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(*flags == flagsLocal);
        }
#endif
        return hr;
    }
    int IXCLRDataProcess.SetOtherNotificationFlags(uint flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.SetOtherNotificationFlags(flags) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyProcess is not null)
        {
            _legacyProcess.SetOtherNotificationFlags(flags);
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.StartEnumMethodDefinitionsByAddress(ClrDataAddress address, ulong* handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        ulong cdacHandle = 0;
        ulong dacHandle = 0;
        int hr = _cdacProcess is not null ? _cdacProcess.StartEnumMethodDefinitionsByAddress(address, handle is null ? null : &cdacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.StartEnumMethodDefinitionsByAddress(address, handle is null ? null : &dacHandle);
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

    int IXCLRDataProcess.EnumMethodDefinitionByAddress(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = handle is null ? null : _session.LookupHandle(*handle);
        ulong cdacHandle = pair is null ? (handle is null ? 0 : *handle) : pair.CDacHandle;
        ulong dacHandle = pair is null ? 0 : pair.DacHandle;
        DacComNullableByRef<IXCLRDataMethodDefinition> methodCDac = new(method.IsNullRef);
        DacComNullableByRef<IXCLRDataMethodDefinition> methodDac = new(method.IsNullRef);
        int hr = _cdacProcess is not null ? _cdacProcess.EnumMethodDefinitionByAddress(handle is null ? null : &cdacHandle, methodCDac) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            hrLocal = _legacyProcess.EnumMethodDefinitionByAddress(handle is null ? null : &dacHandle, methodDac);
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

    int IXCLRDataProcess.EndEnumMethodDefinitionsByAddress(ulong handle)
    {
        using ShimCall shimCall = ShimCall.Enter();
        PairedHandle? pair = _session.ReleaseHandle(handle);
        int hr = _cdacProcess is not null ? _cdacProcess.EndEnumMethodDefinitionsByAddress(pair is null ? handle : pair.CDacHandle) : HResults.E_NOTIMPL;
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if ((pair is null || pair.HasDacHandle) && _legacyProcess is not null)
        {
            hrLocal = _legacyProcess.EndEnumMethodDefinitionsByAddress(pair is null ? handle : pair.DacHandle);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.FollowStub(uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.FollowStub(inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyProcess is not null && LegacyFallbackHelper.CanFallback("FollowStub", "SOSDacImpl.IXCLRDataProcess.cs"))
        {
            return _legacyProcess.FollowStub(inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags);
        }
        return hr;
    }

    int IXCLRDataProcess.FollowStub2(IXCLRDataTask? task,
        uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.FollowStub2(ShimProxy.UnwrapCDac<IXCLRDataTask>(task), inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;
        bool fellBack = false;
        if (hr == HResults.E_NOTIMPL && _legacyProcess is not null && LegacyFallbackHelper.CanFallback("FollowStub2", "SOSDacImpl.IXCLRDataProcess.cs"))
        {
            hr = _legacyProcess.FollowStub2(ShimProxy.UnwrapDac<IXCLRDataTask>(task), inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags);
            fellBack = true;
        }
        int hrLocal = HResults.S_OK;
        bool calledDac = false;
        if (!fellBack && _legacyProcess is not null)
        {
            hrLocal = _legacyProcess.FollowStub2(ShimProxy.UnwrapDac<IXCLRDataTask>(task), inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags);
            calledDac = true;
        }
#if DEBUG
        if (calledDac)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.DumpNativeImage(ClrDataAddress loadedBase,
        char* name,
        /*IXCLRDataDisplay*/ void* display,
        /*IXCLRLibrarySupport*/ void* libSupport,
        /*IXCLRDisassemblySupport*/ void* dis)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess is not null ? _cdacProcess.DumpNativeImage(loadedBase, name, display, libSupport, dis) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion IXCLRDataProcess

    #region IXCLRDataProcess2
    int IXCLRDataProcess2.GetGcNotification(GcEvtArgs* gcEvtArgs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess2 is not null ? _cdacProcess2.GetGcNotification(gcEvtArgs) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyProcess2 is not null)
        {
            int hrLocal = _legacyProcess2.GetGcNotification(gcEvtArgs);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataProcess2.SetGcNotification(GcEvtArgs gcEvtArgs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess2 is not null ? _cdacProcess2.SetGcNotification(gcEvtArgs) : HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyProcess2 is not null)
        {
            // update the DAC cache
            int hrLocal = _legacyProcess2.SetGcNotification(gcEvtArgs);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    #endregion IXCLRDataProcess2

    #region IXCLRDataProcess3
    int IXCLRDataProcess3.GetFunctionTable(ClrDataAddress tableAddress,
        uint bufferSize,
        byte* buffer,
        uint* bytesNeeded,
        uint* entries)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacProcess3 is not null ? _cdacProcess3.GetFunctionTable(tableAddress, bufferSize, buffer, bytesNeeded, entries) : HResults.E_NOTIMPL;
        return hr;
    }

    #endregion IXCLRDataProcess3

    #region ICLRDataEnumMemoryRegions
    int ICLRDataEnumMemoryRegions.EnumMemoryRegions(void* callback, uint miniDumpFlags, int clrFlags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacEnumMemory is not null ? _cdacEnumMemory.EnumMemoryRegions(callback, miniDumpFlags, clrFlags) : HResults.E_NOTIMPL;
        if (hr == HResults.E_NOTIMPL && _legacyEnumMemory is not null && LegacyFallbackHelper.CanFallback("EnumMemoryRegions", "SOSDacImpl.ICLRDataEnumMemoryRegions.cs"))
        {
            return _legacyEnumMemory.EnumMemoryRegions(callback, miniDumpFlags, clrFlags);
        }
        return hr;
    }

    #endregion ICLRDataEnumMemoryRegions

}
