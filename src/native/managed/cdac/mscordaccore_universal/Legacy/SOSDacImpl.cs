// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Implementation of ISOSDacInterface* interfaces intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
/// <remarks>
/// Functions on <see cref="ISOSDacInterface"/> are defined with PreserveSig. Target and Contracts
/// throw on errors. Implementations in this class should wrap logic in a try-catch and return the
/// corresponding error code.
/// </remarks>
[GeneratedComClass]
internal sealed unsafe partial class SOSDacImpl
    : ISOSDacInterface, ISOSDacInterface2, ISOSDacInterface3, ISOSDacInterface4, ISOSDacInterface5,
      ISOSDacInterface6, ISOSDacInterface7, ISOSDacInterface8, ISOSDacInterface9, ISOSDacInterface10,
      ISOSDacInterface11, ISOSDacInterface12, ISOSDacInterface13, ISOSDacInterface14, ISOSDacInterface15,
      ISOSDacInterface16
{
    private readonly Target _target;

    // When this class is created, the runtime may not have loaded the string and object method tables and set the global pointers.
    // This is also the case for the GetUsefulGlobals API, which can be called as part of load notifications before runtime start.
    // They should be set when actually requested via other DAC APIs, so we lazily read the global pointers.
    private readonly Lazy<TargetPointer> _stringMethodTable;
    private readonly Lazy<TargetPointer> _objectMethodTable;

    private readonly ISOSDacInterface? _legacyImpl;
    private readonly ISOSDacInterface2? _legacyImpl2;
    private readonly ISOSDacInterface3? _legacyImpl3;
    private readonly ISOSDacInterface4? _legacyImpl4;
    private readonly ISOSDacInterface5? _legacyImpl5;
    private readonly ISOSDacInterface6? _legacyImpl6;
    private readonly ISOSDacInterface7? _legacyImpl7;
    private readonly ISOSDacInterface8? _legacyImpl8;
    private readonly ISOSDacInterface9? _legacyImpl9;
    private readonly ISOSDacInterface10? _legacyImpl10;
    private readonly ISOSDacInterface11? _legacyImpl11;
    private readonly ISOSDacInterface12? _legacyImpl12;
    private readonly ISOSDacInterface13? _legacyImpl13;
    private readonly ISOSDacInterface14? _legacyImpl14;
    private readonly ISOSDacInterface15? _legacyImpl15;
    private readonly ISOSDacInterface16? _legacyImpl16;
    private readonly IXCLRDataProcess? _legacyProcess;
    private readonly IXCLRDataProcess2? _legacyProcess2;
    private readonly ICLRDataEnumMemoryRegions? _legacyEnumMemory;

    private enum CorTokenType: uint
    {
        mdtTypeRef = 0x01000000,
        mdtTypeDef = 0x02000000,
        mdtFieldDef = 0x04000000,
        mdtMethodDef = 0x06000000,
        typeMask = 0xff000000,
    }

    public SOSDacImpl(Target target, object? legacyObj)
    {
        _target = target;
        _stringMethodTable = new Lazy<TargetPointer>(
            () => _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.StringMethodTable)));

        _objectMethodTable = new Lazy<TargetPointer>(
            () => _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable)));

        // Get all the interfaces for delegating to the legacy DAC
        if (legacyObj is not null)
        {
            _legacyImpl = legacyObj as ISOSDacInterface;
            _legacyImpl2 = legacyObj as ISOSDacInterface2;
            _legacyImpl3 = legacyObj as ISOSDacInterface3;
            _legacyImpl4 = legacyObj as ISOSDacInterface4;
            _legacyImpl5 = legacyObj as ISOSDacInterface5;
            _legacyImpl6 = legacyObj as ISOSDacInterface6;
            _legacyImpl7 = legacyObj as ISOSDacInterface7;
            _legacyImpl8 = legacyObj as ISOSDacInterface8;
            _legacyImpl9 = legacyObj as ISOSDacInterface9;
            _legacyImpl10 = legacyObj as ISOSDacInterface10;
            _legacyImpl11 = legacyObj as ISOSDacInterface11;
            _legacyImpl12 = legacyObj as ISOSDacInterface12;
            _legacyImpl13 = legacyObj as ISOSDacInterface13;
            _legacyImpl14 = legacyObj as ISOSDacInterface14;
            _legacyImpl15 = legacyObj as ISOSDacInterface15;
            _legacyImpl16 = legacyObj as ISOSDacInterface16;

            _legacyProcess = legacyObj as IXCLRDataProcess;
            _legacyProcess2 = legacyObj as IXCLRDataProcess2;

            _legacyEnumMemory = legacyObj as ICLRDataEnumMemoryRegions;
        }
    }

    #region ISOSDacInterface
    int ISOSDacInterface.GetAppDomainConfigFile(ClrDataAddress appDomain, int count, char* configFile, uint* pNeeded)
    {
        // Method is not supported on CoreCLR
        int hr = HResults.E_FAIL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetAppDomainConfigFile(appDomain, count, configFile, pNeeded);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }
    int ISOSDacInterface.GetAppDomainData(ClrDataAddress addr, DacpAppDomainData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (addr == 0)
                throw new ArgumentException();

            *data = default;
            data->AppDomainPtr = addr;
            TargetPointer systemDomainPointer = _target.ReadGlobalPointer(Constants.Globals.SystemDomain);
            ClrDataAddress systemDomain = _target.ReadPointer(systemDomainPointer).ToClrDataAddress(_target);
            Contracts.ILoader loader = _target.Contracts.Loader;
            TargetPointer globalLoaderAllocator = loader.GetGlobalLoaderAllocator();
            data->pHighFrequencyHeap = loader.GetHighFrequencyHeap(globalLoaderAllocator).ToClrDataAddress(_target);
            data->pLowFrequencyHeap = loader.GetLowFrequencyHeap(globalLoaderAllocator).ToClrDataAddress(_target);
            data->pStubHeap = loader.GetStubHeap(globalLoaderAllocator).ToClrDataAddress(_target);
            data->appDomainStage = DacpAppDomainDataStage.STAGE_OPEN;
            if (addr != systemDomain)
            {
                TargetPointer pAppDomain = addr.ToTargetPointer(_target);
                data->dwId = _target.ReadGlobal<uint>(Constants.Globals.DefaultADID);

                IEnumerable<Contracts.ModuleHandle> modules = loader.GetModuleHandles(
                    pAppDomain,
                    AssemblyIterationFlags.IncludeLoading |
                    AssemblyIterationFlags.IncludeLoaded |
                    AssemblyIterationFlags.IncludeExecution);

                foreach (Contracts.ModuleHandle module in modules)
                {
                    if (loader.IsAssemblyLoaded(module))
                    {
                        data->AssemblyCount++;
                    }
                }

                IEnumerable<Contracts.ModuleHandle> failedModules = loader.GetModuleHandles(
                    pAppDomain,
                    AssemblyIterationFlags.IncludeFailedToLoad);
                data->FailedAssemblyCount = failedModules.Count();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpAppDomainData dataLocal = default;
            int hrLocal = _legacyImpl.GetAppDomainData(addr, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface.GetAppDomainList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[] values, uint* pNeeded)
    {
        int hr = HResults.S_OK;
        try
        {
            uint i = 0;
            TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            TargetPointer appDomain = _target.ReadPointer(appDomainPointer);

            if (appDomain != TargetPointer.Null && values.Length > 0)
            {
                values[0] = appDomain.ToClrDataAddress(_target);
                i = 1;
            }

            if (pNeeded is not null)
            {
                *pNeeded = i;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[] valuesLocal = new ClrDataAddress[count];
            uint neededLocal;
            int hrLocal = _legacyImpl.GetAppDomainList(count, valuesLocal, &neededLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
            if (values is not null && values.Length > 0 && valuesLocal.Length > 0)
            {
                Debug.Assert(values[0] == valuesLocal[0], $"cDAC: {values[0]:x}, DAC: {valuesLocal[0]:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetAppDomainName(ClrDataAddress addr, uint count, char* name, uint* pNeeded)
    {
        int hr = HResults.S_OK;
        try
        {
            ILoader loader = _target.Contracts.Loader;
            string friendlyName = loader.GetAppDomainFriendlyName();
            TargetPointer systemDomainPtr = _target.ReadGlobalPointer(Constants.Globals.SystemDomain);
            ClrDataAddress systemDomain = _target.ReadPointer(systemDomainPtr).ToClrDataAddress(_target);
            if (addr == systemDomain || friendlyName == string.Empty)
            {
                if (pNeeded is not null)
                {
                    *pNeeded = 1;
                }
                if (name is not null && count > 0)
                {
                    name[0] = '\0'; // Set the first character to null terminator
                }
            }
            else
            {
                if (pNeeded is not null)
                {
                    *pNeeded = (uint)(friendlyName.Length + 1); // +1 for null terminator
                }

                if (name is not null && count > 0)
                {
                    OutputBufferHelpers.CopyStringToBuffer(name, count, pNeeded, friendlyName);
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(name)));
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetAppDomainStoreData(void* data)
    {
        DacpAppDomainStoreData* appDomainStoreData = (DacpAppDomainStoreData*)data;
        int hr = HResults.S_OK;
        try
        {
            appDomainStoreData->sharedDomain = 0;
            TargetPointer systemDomainPtr = _target.ReadGlobalPointer(Constants.Globals.SystemDomain);
            appDomainStoreData->systemDomain = _target.ReadPointer(systemDomainPtr).ToClrDataAddress(_target);
            TargetPointer appDomainPtr = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            appDomainStoreData->DomainCount = _target.ReadPointer(appDomainPtr) != 0 ? 1 : 0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        {
            if (_legacyImpl is not null)
            {
                DacpAppDomainStoreData dataLocal = default;
                int hrLocal = _legacyImpl.GetAppDomainStoreData(&dataLocal);
                Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
                Debug.Assert(appDomainStoreData->sharedDomain == dataLocal.sharedDomain, $"cDAC: {appDomainStoreData->sharedDomain:x}, DAC: {dataLocal.sharedDomain:x}");
                Debug.Assert(appDomainStoreData->systemDomain == dataLocal.systemDomain, $"cDAC: {appDomainStoreData->systemDomain:x}, DAC: {dataLocal.systemDomain:x}");
                Debug.Assert(appDomainStoreData->DomainCount == dataLocal.DomainCount, $"cDAC: {appDomainStoreData->DomainCount}, DAC: {dataLocal.DomainCount}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetApplicationBase(ClrDataAddress appDomain, int count, char* appBase, uint* pNeeded)
    {
        // Method is not supported on CoreCLR
        int hr = HResults.E_FAIL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetApplicationBase(appDomain, count, appBase, pNeeded);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }

    int ISOSDacInterface.GetAssemblyData(ClrDataAddress domain, ClrDataAddress assembly, DacpAssemblyData* data)
    {
        int hr = HResults.S_OK;

        try
        {
            if (assembly == 0 && domain == 0)
                throw new ArgumentException();

            // Zero out data structure
            *data = default;

            data->AssemblyPtr = assembly;
            data->DomainPtr = domain;

            TargetPointer ppAppDomain = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            TargetPointer pAppDomain = _target.ReadPointer(ppAppDomain);
            data->ParentDomain = pAppDomain.ToClrDataAddress(_target);

            ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(assembly.ToTargetPointer(_target));
            data->isDynamic = loader.IsDynamic(moduleHandle) ? 1 : 0;

            // The DAC increments ModuleCount to 1 if assembly->GetModule() is valid,
            // the cDAC assumes that all assemblies will have a module and the above logic relies on that.
            // Therefore we always set ModuleCount to 1.
            data->ModuleCount = 1;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpAssemblyData dataLocal = default;
            int hrLocal = _legacyImpl.GetAssemblyData(domain, assembly, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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

    int ISOSDacInterface.GetAssemblyList(ClrDataAddress addr, int count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[]? values, int* pNeeded)
    {
        if (addr == 0)
        {
            return HResults.E_INVALIDARG;
        }

        int hr = HResults.S_OK;

        try
        {
            TargetPointer appDomain = addr.ToTargetPointer(_target);
            TargetPointer systemDomainPtr = _target.ReadGlobalPointer(Constants.Globals.SystemDomain);
            ClrDataAddress systemDomain = _target.ReadPointer(systemDomainPtr).ToClrDataAddress(_target);
            if (addr == systemDomain)
                // We shouldn't be asking for the assemblies in SystemDomain
                throw new ArgumentException();

            ILoader loader = _target.Contracts.Loader;
            List<Contracts.ModuleHandle> modules = loader.GetModuleHandles(
                appDomain,
                AssemblyIterationFlags.IncludeLoading |
                AssemblyIterationFlags.IncludeLoaded |
                AssemblyIterationFlags.IncludeExecution).ToList();

            int n = 0; // number of Assemblies that will be returned
            if (values is not null)
            {
                for (int i = 0; i < modules.Count && n < count; i++)
                {
                    Contracts.ModuleHandle module = modules[i];
                    if (loader.IsAssemblyLoaded(module))
                    {
                        values[n++] = loader.GetAssembly(module).ToClrDataAddress(_target);
                    }
                }
            }
            else
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    Contracts.ModuleHandle module = modules[i];
                    if (loader.IsAssemblyLoaded(module))
                    {
                        n++;
                    }
                }
            }

            if (pNeeded is not null)
            {
                *pNeeded = n;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[]? valuesLocal = values != null ? new ClrDataAddress[count] : null;
            int neededLocal;
            int hrLocal = _legacyImpl.GetAssemblyList(addr, count, valuesLocal, &neededLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface.GetAssemblyLocation(ClrDataAddress assembly, int count, char* location, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAssemblyLocation(assembly, count, location, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAssemblyModuleList(ClrDataAddress assembly, uint count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[]? modules, uint* pNeeded)
    {
        if (assembly == 0)
        {
            return HResults.E_INVALIDARG;
        }
        int hr = HResults.S_OK;
        try
        {
            if (modules is not null && modules.Length > 0 && count > 0)
            {
                TargetPointer addr = assembly.ToTargetPointer(_target);
                Contracts.ILoader loader = _target.Contracts.Loader;
                Contracts.ModuleHandle handle = loader.GetModuleHandleFromAssemblyPtr(addr);
                TargetPointer modulePointer = loader.GetModule(handle);
                modules[0] = modulePointer.ToClrDataAddress(_target);
            }

            if (pNeeded is not null)
            {
                *pNeeded = 1;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[] modulesLocal = new ClrDataAddress[count];
            uint neededLocal;
            int hrLocal = _legacyImpl.GetAssemblyModuleList(assembly, count, modulesLocal, &neededLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface.GetAssemblyName(ClrDataAddress assembly, uint count, char* name, uint* pNeeded)
    {
        int hr = HResults.S_OK;
        try
        {
            if (name is not null)
                name[0] = '\0';
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromAssemblyPtr(assembly.ToTargetPointer(_target));
            string path = contract.GetPath(handle);

            // Return not implemented for empty paths for non-reflection emit assemblies (for example, loaded from memory)
            if (string.IsNullOrEmpty(path))
            {
                Contracts.ModuleFlags flags = contract.GetFlags(handle);
                if (!flags.HasFlag(Contracts.ModuleFlags.ReflectionEmit))
                    hr = HResults.E_NOTIMPL;
                else
                    hr = HResults.E_FAIL;
            }

            OutputBufferHelpers.CopyStringToBuffer(name, count, pNeeded, path);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(name == null || new ReadOnlySpan<char>(fileNameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(name)));
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetCCWData(ClrDataAddress ccw, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetCCWData(ccw, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetCCWInterfaces(ClrDataAddress ccw, uint count, void* interfaces, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetCCWInterfaces(ccw, count, interfaces, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetClrWatsonBuckets(ClrDataAddress thread, void* pGenericModeBlock)
    {
        int hr = HResults.S_OK;
        Contracts.IThread threadContract = _target.Contracts.Thread;
        byte[] buckets = Array.Empty<byte>();
        try
        {
            if (_target.Contracts.RuntimeInfo.GetTargetOperatingSystem() == RuntimeInfoOperatingSystem.Unix)
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;
            else if (thread == 0 || pGenericModeBlock == null)
                throw new ArgumentException();

            buckets = threadContract.GetWatsonBuckets(thread.ToTargetPointer(_target));
            if (buckets.Length != 0)
            {
                var dest = new Span<byte>(pGenericModeBlock, buckets.Length);
                buckets.AsSpan().CopyTo(dest);
            }
            else
            {
                hr = HResults.S_FALSE; // No Watson buckets found
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal;
            int sizeOfGenericModeBlock = (int)_target.ReadGlobal<uint>(Constants.Globals.SizeOfGenericModeBlock);
            byte[] genericModeBlockLocal = new byte[sizeOfGenericModeBlock];
            fixed (byte* ptr = genericModeBlockLocal)
            {
                hrLocal = _legacyImpl.GetClrWatsonBuckets(thread, ptr);
            }

            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(new ReadOnlySpan<byte>(genericModeBlockLocal, 0, sizeOfGenericModeBlock).SequenceEqual(new Span<byte>(pGenericModeBlock, sizeOfGenericModeBlock)));
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetCodeHeaderData(ClrDataAddress ip, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetCodeHeaderData(ip, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetCodeHeapList(ClrDataAddress jitManager, uint count, void* codeHeaps, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetCodeHeapList(jitManager, count, codeHeaps, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetDacModuleHandle(void* phModule)
        => _legacyImpl is not null ? _legacyImpl.GetDacModuleHandle(phModule) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetDomainFromContext(ClrDataAddress context, ClrDataAddress* domain)
    {
        int hr = HResults.S_OK;
        if (context == 0 || domain == null)
        {
            return HResults.E_INVALIDARG;
        }
        try
        {
            *domain = context;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress domainLocal;
            int hrLocal = _legacyImpl.GetDomainFromContext(context, &domainLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(domainLocal == context, $"cDAC: {context:x}, DAC: {domainLocal:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetDomainLocalModuleData(ClrDataAddress addr, void* data)
    {
        // CoreCLR does not use domain local modules anymore
        int hr = HResults.E_NOTIMPL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetDomainLocalModuleData(addr, data);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }

    int ISOSDacInterface.GetDomainLocalModuleDataFromAppDomain(ClrDataAddress appDomainAddr, int moduleID, void* data)
    {
        // CoreCLR does not support multi-appdomain shared assembly loading. Thus, a non-pointer sized moduleID cannot exist.
        int hr = HResults.E_INVALIDARG;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetDomainLocalModuleDataFromAppDomain(appDomainAddr, moduleID, data);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }
    int ISOSDacInterface.GetDomainLocalModuleDataFromModule(ClrDataAddress moduleAddr, void* data)
    {
        // CoreCLR does not use domain local modules anymore
        int hr = HResults.E_NOTIMPL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetDomainLocalModuleDataFromModule(moduleAddr, data);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }
    int ISOSDacInterface.GetFailedAssemblyData(ClrDataAddress assembly, uint* pContext, int* pResult)
        => _legacyImpl is not null ? _legacyImpl.GetFailedAssemblyData(assembly, pContext, pResult) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFailedAssemblyDisplayName(ClrDataAddress assembly, uint count, char* name, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetFailedAssemblyDisplayName(assembly, count, name, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFailedAssemblyList(ClrDataAddress appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ClrDataAddress[] values, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetFailedAssemblyList(appDomain, count, values, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFailedAssemblyLocation(ClrDataAddress assembly, uint count, char* location, uint* pNeeded)
    {
        int hr = HResults.S_OK;
        try
        {
            if (assembly == 0 || (location == null && pNeeded == null) || (location != null && count == 0))
                throw new ArgumentException();

            if (pNeeded != null)
                *pNeeded = 1;
            if (location != null)
                location[0] = '\0';
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(location == null || new ReadOnlySpan<char>(locationLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(location)));
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetFieldDescData(ClrDataAddress fieldDesc, DacpFieldDescData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (fieldDesc == 0 || data == null)
                throw new ArgumentException();

            IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
            IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
            ISignatureDecoder signatureDecoder = _target.Contracts.SignatureDecoder;

            TargetPointer fieldDescTargetPtr = fieldDesc.ToTargetPointer(_target);
            CorElementType fieldDescType = rtsContract.GetFieldDescType(fieldDescTargetPtr);
            data->Type = fieldDescType;
            data->sigType = fieldDescType;

            uint token = rtsContract.GetFieldDescMemberDef(fieldDescTargetPtr);
            FieldDefinitionHandle fieldHandle = (FieldDefinitionHandle)MetadataTokens.Handle((int)token);

            TargetPointer enclosingMT = rtsContract.GetMTOfEnclosingClass(fieldDescTargetPtr);
            TypeHandle ctx = rtsContract.GetTypeHandle(enclosingMT);
            TargetPointer modulePtr = rtsContract.GetModule(ctx);
            Contracts.ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
            MetadataReader mdReader = ecmaMetadataContract.GetMetadata(moduleHandle)!;
            FieldDefinition fieldDef = mdReader.GetFieldDefinition(fieldHandle);
            try
            {
                // try to completely decode the signature
                TypeHandle foundTypeHandle = signatureDecoder.DecodeFieldSignature(fieldDef.Signature, moduleHandle, ctx);

                // get the MT of the type
                // This is an implementation detail of the DAC that we replicate here to get method tables for non-MT types
                // that we can return to SOS for pretty-printing.
                // In the future we may want to return a TypeHandle instead of a MethodTable, and modify SOS to do more complete pretty-printing.
                // DAC equivalent: src/coreclr/vm/typehandle.inl TypeHandle::GetMethodTable
                if (rtsContract.IsFunctionPointer(foundTypeHandle, out _, out _) || rtsContract.IsPointer(foundTypeHandle))
                    data->MTOfType = rtsContract.GetPrimitiveType(CorElementType.U).Address.ToClrDataAddress(_target);
                // array MTs
                else if (rtsContract.IsArray(foundTypeHandle, out _))
                    data->MTOfType = foundTypeHandle.Address.ToClrDataAddress(_target);
                else
                {
                    try
                    {
                        // value typedescs
                        TypeHandle paramTypeHandle = rtsContract.GetTypeParam(foundTypeHandle);
                        data->MTOfType = paramTypeHandle.Address.ToClrDataAddress(_target);
                    }
                    catch (ArgumentException)
                    {
                        // non-array MTs
                        data->MTOfType = foundTypeHandle.Address.ToClrDataAddress(_target);
                    }
                }
            }
            catch (VirtualReadException)
            {
                // if we can't find the MT (e.g in a minidump)
                data->MTOfType = 0;
            }

            // partial decoding of signature
            BlobReader blobReader = mdReader.GetBlobReader(fieldDef.Signature);
            SignatureHeader header = blobReader.ReadSignatureHeader();
            // read the header byte and check for correctness
            if (header.Kind != SignatureKind.Field)
                throw new BadImageFormatException();
            // read the top-level type
            CorElementType typeCode;
            EntityHandle entityHandle;
            // in a loop, read custom modifiers until we get to the underlying type
            do
            {
                typeCode = (CorElementType)blobReader.ReadByte();
                entityHandle = blobReader.ReadTypeHandle(); // consume the type
            } while (typeCode is CorElementType.CModReqd or CorElementType.CModOpt); // eat custom modifiers

            if (typeCode is CorElementType.Class or CorElementType.ValueType)
            {
                // if the typecode is class or value, we have been able to read the token that follows in the sig
                data->TokenOfType = (uint)MetadataTokens.GetToken(entityHandle);
            }
            else
            {
                // otherwise we have not found the token here, but we can encode the underlying type in sigType
                data->TokenOfType = (uint)CorTokenType.mdtTypeDef;
                if (data->MTOfType == 0)
                    data->sigType = typeCode;
            }

            data->ModuleOfType = modulePtr.ToClrDataAddress(_target);
            data->mb = token;
            data->MTOfEnclosingClass = ctx.Address.ToClrDataAddress(_target);
            data->dwOffset = rtsContract.GetFieldDescOffset(fieldDescTargetPtr, fieldDef);
            data->bIsThreadLocal = rtsContract.IsFieldDescThreadStatic(fieldDescTargetPtr) ? 1 : 0;
            data->bIsContextLocal = 0;
            data->bIsStatic = rtsContract.IsFieldDescStatic(fieldDescTargetPtr) ? 1 : 0;
            data->NextField = fieldDescTargetPtr + _target.GetTypeInfo(DataType.FieldDesc).Size!.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpFieldDescData dataLocal = default;
            int hrLocal = _legacyImpl.GetFieldDescData(fieldDesc, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
                Debug.Assert(data->NextField == dataLocal.NextField, $"cDAC: {data->NextField:x}, DAC: {dataLocal.NextField:x}");
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetFrameName(ClrDataAddress vtable, uint count, char* frameName, uint* pNeeded)
    {
        if (vtable == 0)
        {
            return HResults.E_INVALIDARG;
        }

        int hr = HResults.S_OK;
        try
        {
            IStackWalk stackWalk = _target.Contracts.StackWalk;
            string name = stackWalk.GetFrameName(new(vtable));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException();

            OutputBufferHelpers.CopyStringToBuffer(frameName, count, pNeeded, name);

            if (frameName is not null && pNeeded is not null)
            {
                // the DAC version of this API does not count the trailing null terminator
                // if a buffer is provided
                (*pNeeded)--;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface.GetGCHeapData(DacpGcHeapData* data)
    {
        int hr = HResults.S_OK;

        if (data == null)
        {
            return HResults.E_INVALIDARG;
        }

        try
        {
            IGC gc = _target.Contracts.GC;
            string[] heapType = gc.GetGCIdentifiers();
            if (!heapType.Contains(GCIdentifiers.Workstation) && !heapType.Contains(GCIdentifiers.Server))
            {
                // If the GC type is not recognized, we cannot provide heap data
                hr = HResults.E_FAIL;
            }
            else
            {
                data->g_max_generation = gc.GetMaxGeneration();
                data->bServerMode = heapType.Contains(GCIdentifiers.Server) ? 1 : 0;
                data->bGcStructuresValid = gc.GetGCStructuresValid() ? 1 : 0;
                data->HeapCount = gc.GetGCHeapCount();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapData dataLocal = default;
            int hrLocal = _legacyImpl.GetGCHeapData(&dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;

        try
        {
            IGC gc = _target.Contracts.GC;
            string[] heapType = gc.GetGCIdentifiers();
            if (!heapType.Contains(GCIdentifiers.Server))
            {
                // If GC type is not server, this API is not supported
                hr = HResults.E_FAIL;
            }
            else
            {
                uint heapCount = gc.GetGCHeapCount();
                if (pNeeded is not null)
                {
                    *pNeeded = heapCount;
                }

                if (heaps.Length == heapCount)
                {
                    List<TargetPointer> gcHeaps = gc.GetGCHeaps().ToList();
                    Debug.Assert(gcHeaps.Count == heapCount, "Expected the number of GC heaps to match the count returned by GetGCHeapCount");
                    for (uint i = 0; i < heapCount; i++)
                    {
                        heaps[i] = gcHeaps[(int)i].ToClrDataAddress(_target);
                    }
                }
                else if (heaps.Length != 0)
                {
                    hr = HResults.E_INVALIDARG;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress[] heapsLocal = new ClrDataAddress[count];
            uint neededLocal;
            int hrLocal = _legacyImpl.GetGCHeapList(count, heapsLocal, &neededLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;

        try
        {
            if (heap == 0 || details == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // doesn't make sense to call this on WKS mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Server))
                throw new ArgumentException();

            GCHeapData heapData = gc.GetHeapData(heap.ToTargetPointer(_target));

            details->heapAddr = heap;

            gc.GetGCBounds(out TargetPointer minAddress, out TargetPointer maxAddress);
            details->lowest_address = minAddress.ToClrDataAddress(_target);
            details->highest_address = maxAddress.ToClrDataAddress(_target);

            if (gcIdentifiers.Contains(GCIdentifiers.Background))
            {
                details->current_c_gc_state = gc.GetCurrentGCState();
                details->mark_array = heapData.MarkArray.ToClrDataAddress(_target);
                details->next_sweep_obj = heapData.NextSweepObject.ToClrDataAddress(_target);
                details->background_saved_lowest_address = heapData.BackGroundSavedMinAddress.ToClrDataAddress(_target);
                details->background_saved_highest_address = heapData.BackGroundSavedMaxAddress.ToClrDataAddress(_target);
            }
            else
            {
                details->current_c_gc_state = 0;
                details->mark_array = unchecked((ulong)-1);
                details->next_sweep_obj = 0;
                details->background_saved_lowest_address = 0;
                details->background_saved_highest_address = 0;
            }

            // now get information specific to this heap (server mode gives us several heaps; we're getting
            // information about only one of them.
            details->alloc_allocated = heapData.AllocAllocated.ToClrDataAddress(_target);
            details->ephemeral_heap_segment = heapData.EphemeralHeapSegment.ToClrDataAddress(_target);
            details->card_table = heapData.CardTable.ToClrDataAddress(_target);

            if (gcIdentifiers.Contains(GCIdentifiers.Regions))
            {
                // with regions, we don't have these variables anymore
                // use special value -1 in saved_sweep_ephemeral_seg to signal the region case
                details->saved_sweep_ephemeral_seg = unchecked((ulong)-1);
                details->saved_sweep_ephemeral_start = 0;
            }
            else
            {
                if (gcIdentifiers.Contains(GCIdentifiers.Background))
                {
                    details->saved_sweep_ephemeral_seg = heapData.SavedSweepEphemeralSegment.ToClrDataAddress(_target);
                    details->saved_sweep_ephemeral_start = heapData.SavedSweepEphemeralStart.ToClrDataAddress(_target);
                }
                else
                {
                    details->saved_sweep_ephemeral_seg = 0;
                    details->saved_sweep_ephemeral_start = 0;
                }
            }

            // get bounds for the different generations
            for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS && i < heapData.GenerationTable.Count; i++)
            {
                GCGenerationData genData = heapData.GenerationTable[i];
                details->generation_table[i].start_segment = genData.StartSegment.ToClrDataAddress(_target);
                details->generation_table[i].allocation_start = genData.AllocationStart.ToClrDataAddress(_target);
                details->generation_table[i].allocContextPtr = genData.AllocationContextPointer.ToClrDataAddress(_target);
                details->generation_table[i].allocContextLimit = genData.AllocationContextLimit.ToClrDataAddress(_target);
            }

            for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS + 3 && i < heapData.FillPointers.Count; i++)
            {
                details->finalization_fill_pointers[i] = heapData.FillPointers[i].ToClrDataAddress(_target);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapDetails detailsLocal = default;
            int hrLocal = _legacyImpl.GetGCHeapDetails(heap, &detailsLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;

        try
        {
            if (details == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // doesn't make sense to call this on SVR mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Workstation))
                throw new ArgumentException();

            GCHeapData heapData = gc.GetHeapData();

            details->heapAddr = 0;

            gc.GetGCBounds(out TargetPointer minAddress, out TargetPointer maxAddress);
            details->lowest_address = minAddress.ToClrDataAddress(_target);
            details->highest_address = maxAddress.ToClrDataAddress(_target);

            if (gcIdentifiers.Contains(GCIdentifiers.Background))
            {
                details->current_c_gc_state = gc.GetCurrentGCState();
                details->mark_array = heapData.MarkArray.ToClrDataAddress(_target);
                details->next_sweep_obj = heapData.NextSweepObject.ToClrDataAddress(_target);
                details->background_saved_lowest_address = heapData.BackGroundSavedMinAddress.ToClrDataAddress(_target);
                details->background_saved_highest_address = heapData.BackGroundSavedMaxAddress.ToClrDataAddress(_target);
            }
            else
            {
                details->current_c_gc_state = 0;
                details->mark_array = unchecked((ulong)-1);
                details->next_sweep_obj = 0;
                details->background_saved_lowest_address = 0;
                details->background_saved_highest_address = 0;
            }

            // now get information specific to this heap (server mode gives us several heaps; we're getting
            // information about only one of them.
            details->alloc_allocated = heapData.AllocAllocated.ToClrDataAddress(_target);
            details->ephemeral_heap_segment = heapData.EphemeralHeapSegment.ToClrDataAddress(_target);
            details->card_table = heapData.CardTable.ToClrDataAddress(_target);

            if (gcIdentifiers.Contains(GCIdentifiers.Regions))
            {
                // with regions, we don't have these variables anymore
                // use special value -1 in saved_sweep_ephemeral_seg to signal the region case
                details->saved_sweep_ephemeral_seg = unchecked((ulong)-1);
                details->saved_sweep_ephemeral_start = 0;
            }
            else
            {
                if (gcIdentifiers.Contains(GCIdentifiers.Background))
                {
                    details->saved_sweep_ephemeral_seg = heapData.SavedSweepEphemeralSegment.ToClrDataAddress(_target);
                    details->saved_sweep_ephemeral_start = heapData.SavedSweepEphemeralStart.ToClrDataAddress(_target);
                }
                else
                {
                    details->saved_sweep_ephemeral_seg = 0;
                    details->saved_sweep_ephemeral_start = 0;
                }
            }

            // get bounds for the different generations
            for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS && i < heapData.GenerationTable.Count; i++)
            {
                GCGenerationData genData = heapData.GenerationTable[i];
                details->generation_table[i].start_segment = genData.StartSegment.ToClrDataAddress(_target);
                details->generation_table[i].allocation_start = genData.AllocationStart.ToClrDataAddress(_target);
                details->generation_table[i].allocContextPtr = genData.AllocationContextPointer.ToClrDataAddress(_target);
                details->generation_table[i].allocContextLimit = genData.AllocationContextLimit.ToClrDataAddress(_target);
            }

            for (int i = 0; i < GCConstants.DAC_NUMBERGENERATIONS + 3 && i < heapData.FillPointers.Count; i++)
            {
                details->finalization_fill_pointers[i] = heapData.FillPointers[i].ToClrDataAddress(_target);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapDetails detailsLocal = default;
            int hrLocal = _legacyImpl.GetGCHeapStaticData(&detailsLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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

    int ISOSDacInterface.GetHandleEnum(void** ppHandleEnum)
        => _legacyImpl is not null ? _legacyImpl.GetHandleEnum(ppHandleEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHandleEnumForGC(uint gen, void** ppHandleEnum)
        => _legacyImpl is not null ? _legacyImpl.GetHandleEnumForGC(gen, ppHandleEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHandleEnumForTypes([In, MarshalUsing(CountElementName = "count")] uint[] types, uint count, void** ppHandleEnum)
        => _legacyImpl is not null ? _legacyImpl.GetHandleEnumForTypes(types, count, ppHandleEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHeapAllocData(uint count, void* data, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetHeapAllocData(count, data, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHeapAnalyzeData(ClrDataAddress addr, DacpGcHeapAnalyzeData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (addr == 0 || data == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // doesn't make sense to call this on WKS mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Server))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            GCHeapData heapData = gc.GetHeapData(addr.ToTargetPointer(_target));

            data->heapAddr = addr;
            data->internal_root_array = heapData.InternalRootArray.ToClrDataAddress(_target);
            data->internal_root_array_index = heapData.InternalRootArrayIndex.Value;
            data->heap_analyze_success = heapData.HeapAnalyzeSuccess ? (int)Interop.BOOL.TRUE : (int)Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapAnalyzeData dataLocal = default;
            int hrLocal = _legacyImpl.GetHeapAnalyzeData(addr, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;
        try
        {
            if (data == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // doesn't make sense to call this on SVR mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Workstation))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            // For workstation GC, use GetHeapData()
            GCHeapData heapData = gc.GetHeapData();

            data->heapAddr = 0; // Not applicable for static data
            data->internal_root_array = heapData.InternalRootArray.ToClrDataAddress(_target);
            data->internal_root_array_index = heapData.InternalRootArrayIndex.Value;
            data->heap_analyze_success = heapData.HeapAnalyzeSuccess ? (int)Interop.BOOL.TRUE : (int)Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpGcHeapAnalyzeData dataLocal = default;
            int hrLocal = _legacyImpl.GetHeapAnalyzeStaticData(&dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface.GetHeapSegmentData(ClrDataAddress seg, DacpHeapSegmentData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (seg == 0 || data == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();
            GCHeapSegmentData segmentData = gc.GetHeapSegmentData(seg.ToTargetPointer(_target));

            data->segmentAddr = seg;
            data->allocated = segmentData.Allocated.ToClrDataAddress(_target);
            data->committed = segmentData.Committed.ToClrDataAddress(_target);
            data->reserved = segmentData.Reserved.ToClrDataAddress(_target);
            data->used = segmentData.Used.ToClrDataAddress(_target);
            data->mem = segmentData.Mem.ToClrDataAddress(_target);
            data->next = segmentData.Next.ToClrDataAddress(_target);
            data->gc_heap = segmentData.Heap.ToClrDataAddress(_target);
            data->flags = (nuint)segmentData.Flags.Value;
            data->background_allocated = segmentData.BackgroundAllocated.ToClrDataAddress(_target);

            // TODO: Compute highAllocMark - need to determine if this is the ephemeral segment
            // and get the allocation mark from the appropriate heap data
            // For now, use allocated as a fallback (similar to non-ephemeral segments in legacy code)
            data->highAllocMark = data->allocated;

            GCHeapData heapData = gcIdentifiers.Contains(GCIdentifiers.Server) ? gc.GetHeapData(segmentData.Heap) : gc.GetHeapData();
            if (seg.ToTargetPointer(_target) == heapData.EphemeralHeapSegment)
            {
                data->highAllocMark = heapData.AllocAllocated.ToClrDataAddress(_target);
            }
            else
            {
                data->highAllocMark = data->allocated;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpHeapSegmentData dataLocal = default;
            int hrLocal = _legacyImpl.GetHeapSegmentData(seg, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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

    int ISOSDacInterface.GetHillClimbingLogEntry(ClrDataAddress addr, void* data)
    {
        // This API is not implemented by the legacy DAC
        int hr = HResults.E_NOTIMPL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetHillClimbingLogEntry(addr, data);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }
    int ISOSDacInterface.GetILForModule(ClrDataAddress moduleAddr, int rva, ClrDataAddress* il)
    {
        int hr = HResults.S_OK;
        if (moduleAddr == 0 || il == null)
        {
            hr = HResults.E_INVALIDARG;
        }
        else if (rva == 0)
            *il = 0;
        else
        {
            try
            {
                Contracts.ILoader loader = _target.Contracts.Loader;
                TargetPointer module = moduleAddr.ToTargetPointer(_target);
                Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(module);
                TargetPointer peAssemblyPtr = loader.GetPEAssembly(moduleHandle);
                *il = loader.GetILAddr(peAssemblyPtr, rva).ToClrDataAddress(_target);
            }
            catch (System.Exception ex)
            {
                hr = ex.HResult;
            }
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress ilLocal;
            int hrLocal = _legacyImpl.GetILForModule(moduleAddr, rva, &ilLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*il == ilLocal, $"cDAC: {*il:x}, DAC: {ilLocal:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetJitHelperFunctionName(ClrDataAddress ip, uint count, byte* name, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetJitHelperFunctionName(ip, count, name, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetJitManagerList(uint count, void* managers, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetJitManagerList(count, managers, pNeeded) : HResults.E_NOTIMPL;

    private bool IsJumpRel64(TargetPointer pThunk)
        => 0x48 == _target.Read<byte>(pThunk) &&
           0xB8 == _target.Read<byte>(pThunk + 1) &&
           0xFF == _target.Read<byte>(pThunk + 10) &&
           0xE0 == _target.Read<byte>(pThunk + 11);

    private TargetPointer DecodeJump64(TargetPointer pThunk)
    {
        Debug.Assert(IsJumpRel64(pThunk), "Expected a jump thunk");

        return _target.ReadPointer(pThunk + 2);
    }
    int ISOSDacInterface.GetJumpThunkTarget(void* ctx, ClrDataAddress* targetIP, ClrDataAddress* targetMD)
    {
        if (ctx == null || targetIP == null || targetMD == null)
        {
            return HResults.E_INVALIDARG;
        }

        int hr = HResults.S_OK;
        try
        {
            // API is implemented for x64 only
            if (_target.Contracts.RuntimeInfo.GetTargetArchitecture() != RuntimeInfoArchitecture.X64)
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);

            // Context is not stored in the target, but in our own process
            context.FillFromBuffer(new Span<byte>(ctx, (int)context.Size));
            TargetPointer pThunk = context.InstructionPointer;

            if (IsJumpRel64(pThunk))
            {
                *targetMD = 0;
                *targetIP = DecodeJump64(pThunk).ToClrDataAddress(_target);
            }
            else
            {
                hr = HResults.E_FAIL;
            }

        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress targetIPLocal;
            ClrDataAddress targetMDLocal;
            int hrLocal = _legacyImpl.GetJumpThunkTarget(ctx, &targetIPLocal, &targetMDLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*targetIP == targetIPLocal, $"cDAC: {*targetIP:x}, DAC: {targetIPLocal:x}");
                Debug.Assert(*targetMD == targetMDLocal, $"cDAC: {*targetMD:x}, DAC: {targetMDLocal:x}");
            }
        }
#endif

        return hr;
    }
    int ISOSDacInterface.GetMethodDescData(ClrDataAddress addr, ClrDataAddress ip, DacpMethodDescData* data, uint cRevertedRejitVersions, DacpReJitData* rgRevertedRejitData, uint* pcNeededRevertedRejitData)
    {
        if (addr == 0)
        {
            return HResults.E_INVALIDARG;
        }
        if (cRevertedRejitVersions != 0 && rgRevertedRejitData == null)
        {
            return HResults.E_INVALIDARG;
        }
        if (rgRevertedRejitData != null && pcNeededRevertedRejitData == null)
        {
            // If you're asking for reverted rejit data, you'd better ask for the number of
            // elements we return
            return HResults.E_INVALIDARG;
        }

        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;

            TargetPointer methodDesc = addr.ToTargetPointer(_target);
            Contracts.MethodDescHandle methodDescHandle = rtsContract.GetMethodDescHandle(methodDesc);
            Contracts.ICodeVersions nativeCodeContract = _target.Contracts.CodeVersions;
            Contracts.IReJIT rejitContract = _target.Contracts.ReJIT;

            if (rgRevertedRejitData != null)
            {
                NativeMemory.Clear(rgRevertedRejitData, (nuint)(sizeof(DacpReJitData) * cRevertedRejitVersions));
            }
            if (pcNeededRevertedRejitData != null)
            {
                *pcNeededRevertedRejitData = 0;
            }

            NativeCodeVersionHandle requestedNativeCodeVersion;
            NativeCodeVersionHandle? activeNativeCodeVersion = null;
            if (ip != 0)
            {
                requestedNativeCodeVersion = nativeCodeContract.GetNativeCodeVersionForIP(ip.ToTargetCodePointer(_target));
            }
            else
            {
                requestedNativeCodeVersion = nativeCodeContract.GetActiveNativeCodeVersion(new TargetPointer(methodDesc));
                activeNativeCodeVersion = requestedNativeCodeVersion;
            }

            data->requestedIP = ip;
            data->bIsDynamic = rtsContract.IsDynamicMethod(methodDescHandle) ? 1 : 0;
            data->wSlotNumber = rtsContract.GetSlotNumber(methodDescHandle);
            TargetCodePointer nativeCodeAddr = TargetCodePointer.Null;
            if (requestedNativeCodeVersion.Valid)
            {
                nativeCodeAddr = nativeCodeContract.GetNativeCode(requestedNativeCodeVersion);
            }
            if (nativeCodeAddr != TargetCodePointer.Null)
            {
                data->bHasNativeCode = 1;
                data->NativeCodeAddr = nativeCodeAddr.ToAddress(_target).ToClrDataAddress(_target);
            }
            else
            {
                data->bHasNativeCode = 0;
                data->NativeCodeAddr = 0xffffffff_fffffffful;
            }
            if (rtsContract.HasNativeCodeSlot(methodDescHandle))
            {
                data->AddressOfNativeCodeSlot = rtsContract.GetAddressOfNativeCodeSlot(methodDescHandle).ToClrDataAddress(_target);
            }
            else
            {
                data->AddressOfNativeCodeSlot = 0;
            }
            data->MDToken = rtsContract.GetMethodToken(methodDescHandle);
            data->MethodDescPtr = addr;
            TargetPointer methodTableAddr = rtsContract.GetMethodTable(methodDescHandle);
            data->MethodTablePtr = methodTableAddr.ToClrDataAddress(_target);
            TypeHandle typeHandle = rtsContract.GetTypeHandle(methodTableAddr);
            data->ModulePtr = rtsContract.GetModule(typeHandle).ToClrDataAddress(_target);

            // If rejit info is appropriate, get the following:
            //     * ReJitInfo for the current, active version of the method
            //     * ReJitInfo for the requested IP (for !ip2md and !u)
            //     * ReJitInfos for all reverted versions of the method (up to
            //         cRevertedRejitVersions)
            //
            // Minidumps will not have all this rejit info, and failure to get rejit info
            // should not be fatal.  So enclose all rejit stuff in a try.
            try
            {
                if (activeNativeCodeVersion is null || !activeNativeCodeVersion.Value.Valid)
                {
                    activeNativeCodeVersion = nativeCodeContract.GetActiveNativeCodeVersion(new TargetPointer(methodDesc));
                }

                if (activeNativeCodeVersion is null || !activeNativeCodeVersion.Value.Valid)
                {
                    throw new InvalidOperationException("No active native code version found");
                }

                // Active ReJitInfo
                CopyNativeCodeVersionToReJitData(
                    activeNativeCodeVersion.Value,
                    activeNativeCodeVersion.Value,
                    &data->rejitDataCurrent);

                // Requested ReJitInfo
                Debug.Assert(data->rejitDataRequested.rejitID == 0);
                if (ip != 0 && requestedNativeCodeVersion.Valid)
                {
                    CopyNativeCodeVersionToReJitData(
                        requestedNativeCodeVersion,
                        activeNativeCodeVersion.Value,
                        &data->rejitDataRequested);
                }

                // Total number of jitted rejit versions
                int cJittedRejitVersions = rejitContract.GetRejitIds(_target, methodDescHandle.Address).Count();
                data->cJittedRejitVersions = (uint)cJittedRejitVersions;

                // Reverted ReJitInfos
                if (rgRevertedRejitData == null)
                {
                    // No reverted rejit versions will be returned, but maybe caller wants a
                    // count of all versions
                    if (pcNeededRevertedRejitData != null)
                    {
                        *pcNeededRevertedRejitData = data->cJittedRejitVersions;
                    }
                }
                else
                {
                    // Caller wants some reverted rejit versions.  Gather reverted rejit version data to return

                    // Returns all available rejitids, including the rejitid for the one non-reverted
                    // current version.
                    List<TargetNUInt> reJitIds = rejitContract.GetRejitIds(_target, methodDescHandle.Address).ToList();

                    // Go through rejitids.  For each reverted one, populate a entry in rgRevertedRejitData
                    uint iRejitDataReverted = 0;
                    ILCodeVersionHandle activeVersion = nativeCodeContract.GetActiveILCodeVersion(methodDesc);
                    TargetNUInt activeVersionId = rejitContract.GetRejitId(activeVersion);
                    for (int i = 0; (i < reJitIds.Count) && (iRejitDataReverted < cRevertedRejitVersions); i++)
                    {
                        ILCodeVersionHandle ilCodeVersion = nativeCodeContract.GetILCodeVersions(methodDesc)
                            .FirstOrDefault(ilcode => rejitContract.GetRejitId(ilcode) == reJitIds[i],
                                ILCodeVersionHandle.Invalid);

                        if (!ilCodeVersion.IsValid || rejitContract.GetRejitId(ilCodeVersion) == activeVersionId)
                        {
                            continue;
                        }

                        NativeCodeVersionHandle activeRejitChild = nativeCodeContract.GetActiveNativeCodeVersionForILCodeVersion(methodDesc, ilCodeVersion);
                        CopyNativeCodeVersionToReJitData(
                            activeRejitChild,
                            activeNativeCodeVersion.Value,
                            &rgRevertedRejitData[iRejitDataReverted]);

                        iRejitDataReverted++;
                    }
                    // We already checked that pcNeededRevertedRejitData != NULL because rgRevertedRejitData != NULL
                    *pcNeededRevertedRejitData = iRejitDataReverted;
                }
            }
            catch (global::System.Exception)
            {
                if (pcNeededRevertedRejitData != null)
                {
                    *pcNeededRevertedRejitData = 0;
                }
            }

            // HAVE_GCCOVER
            if (requestedNativeCodeVersion.Valid)
            {
                // TargetPointer.Null if GCCover information is not available.
                // In certain minidumps, we won't save the GCCover information.
                // (it would be unwise to do so, it is heavy and not a customer scenario).
                data->GCStressCodeCopy = nativeCodeContract.GetGCStressCodeCopy(requestedNativeCodeVersion).ToClrDataAddress(_target);
            }

            // Unlike the legacy implementation, the cDAC does not currently populate
            // data->managedDynamicMethodObject. This field is unused in both SOS and CLRMD
            // and would require accessing CorLib bound managed fields which the cDAC does not
            // currently support. However, it must remain in the return type for compatibility.
            data->managedDynamicMethodObject = 0;
        }
        catch (global::System.Exception ex)
        {
            hr = ex.HResult;
        }

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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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

                // rejitDataCurrent
                Debug.Assert(data->rejitDataCurrent.rejitID == dataLocal.rejitDataCurrent.rejitID, $"cDAC: {data->rejitDataCurrent.rejitID}, DAC: {dataLocal.rejitDataCurrent.rejitID}");
                Debug.Assert(data->rejitDataCurrent.NativeCodeAddr == dataLocal.rejitDataCurrent.NativeCodeAddr, $"cDAC: {data->rejitDataCurrent.NativeCodeAddr:x}, DAC: {dataLocal.rejitDataCurrent.NativeCodeAddr:x}");
                Debug.Assert(data->rejitDataCurrent.flags == dataLocal.rejitDataCurrent.flags, $"cDAC: {data->rejitDataCurrent.flags}, DAC: {dataLocal.rejitDataCurrent.flags}");

                // rejitDataRequested
                Debug.Assert(data->rejitDataRequested.rejitID == dataLocal.rejitDataRequested.rejitID, $"cDAC: {data->rejitDataRequested.rejitID}, DAC: {dataLocal.rejitDataRequested.rejitID}");
                Debug.Assert(data->rejitDataRequested.NativeCodeAddr == dataLocal.rejitDataRequested.NativeCodeAddr, $"cDAC: {data->rejitDataRequested.NativeCodeAddr:x}, DAC: {dataLocal.rejitDataRequested.NativeCodeAddr:x}");
                Debug.Assert(data->rejitDataRequested.flags == dataLocal.rejitDataRequested.flags, $"cDAC: {data->rejitDataRequested.flags}, DAC: {dataLocal.rejitDataRequested.flags}");

                // rgRevertedRejitData
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

    private void CopyNativeCodeVersionToReJitData(
        NativeCodeVersionHandle nativeCodeVersion,
        NativeCodeVersionHandle activeNativeCodeVersion,
        DacpReJitData* pReJitData)
    {
        ICodeVersions cv = _target.Contracts.CodeVersions;
        IReJIT rejit = _target.Contracts.ReJIT;

        ILCodeVersionHandle ilCodeVersion = cv.GetILCodeVersion(nativeCodeVersion);

        pReJitData->rejitID = rejit.GetRejitId(ilCodeVersion).Value;
        pReJitData->NativeCodeAddr = cv.GetNativeCode(nativeCodeVersion).Value;

        if (nativeCodeVersion.CodeVersionNodeAddress != activeNativeCodeVersion.CodeVersionNodeAddress ||
            nativeCodeVersion.MethodDescAddress != activeNativeCodeVersion.MethodDescAddress)
        {
            pReJitData->flags = DacpReJitData.Flags.kReverted;
        }
        else
        {
            DacpReJitData.Flags flags = DacpReJitData.Flags.kUnknown;
            switch (rejit.GetRejitState(ilCodeVersion))
            {
                // kStateRequested
                case RejitState.Requested:
                    flags = DacpReJitData.Flags.kRequested;
                    break;
                // kStateActive
                case RejitState.Active:
                    flags = DacpReJitData.Flags.kActive;
                    break;
                default:
                    Debug.Fail("Unknown RejitState. cDAC should be updated to understand this new state.");
                    break;
            }
            pReJitData->flags = flags;
        }
    }

    int ISOSDacInterface.GetMethodDescFromToken(ClrDataAddress moduleAddr, uint token, ClrDataAddress* methodDesc)
    {
        int hr = HResults.S_OK;
        if (moduleAddr == 0 || methodDesc == null)
            hr = HResults.E_INVALIDARG;
        else
        {
            try
            {
                Contracts.ILoader loader = _target.Contracts.Loader;
                TargetPointer module = moduleAddr.ToTargetPointer(_target);
                Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(module);
                Contracts.ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
                switch ((CorTokenType)token & CorTokenType.typeMask)
                {
                    case CorTokenType.mdtFieldDef:
                        *methodDesc = loader.GetModuleLookupMapElement(lookupTables.FieldDefToDesc, token, out var _).ToClrDataAddress(_target);
                        break;
                    case CorTokenType.mdtMethodDef:
                        *methodDesc = loader.GetModuleLookupMapElement(lookupTables.MethodDefToDesc, token, out var _).ToClrDataAddress(_target);
                        break;
                    case CorTokenType.mdtTypeDef:
                        *methodDesc = loader.GetModuleLookupMapElement(lookupTables.TypeDefToMethodTable, token, out var _).ToClrDataAddress(_target);
                        break;
                    case CorTokenType.mdtTypeRef:
                        *methodDesc = loader.GetModuleLookupMapElement(lookupTables.TypeRefToMethodTable, token, out var _).ToClrDataAddress(_target);
                        break;
                    default:
                        hr = HResults.E_INVALIDARG;
                        break;
                }
            }
            catch (System.Exception ex)
            {
                hr = ex.HResult;
            }
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress methodDescLocal;
            int hrLocal = _legacyImpl.GetMethodDescFromToken(moduleAddr, token, &methodDescLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*methodDesc == methodDescLocal, $"cDAC: {*methodDesc:x}, DAC: {methodDescLocal:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetMethodDescName(ClrDataAddress addr, uint count, char* name, uint* pNeeded)
    {
        if (addr == 0)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        if (pNeeded != null)
            *pNeeded = 0;
        try
        {
            TargetPointer methodDesc = addr.ToTargetPointer(_target);
            StringBuilder stringBuilder = new StringBuilder();
            Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
            Contracts.MethodDescHandle methodDescHandle = rtsContract.GetMethodDescHandle(methodDesc);
            try
            {
                TypeNameBuilder.AppendMethodInternal(_target, stringBuilder, methodDescHandle, TypeNameFormat.FormatSignature | TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
            }
            catch
            {
                hr = HResults.E_FAIL;
                if (rtsContract.IsNoMetadataMethod(methodDescHandle, out _))
                {
                    // In heap dumps, trying to format the signature can fail
                    // in certain cases.
                    stringBuilder.Clear();
                    TypeNameBuilder.AppendMethodInternal(_target, stringBuilder, methodDescHandle, TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
                    hr = HResults.S_OK;
                }
                else
                {
                    string? fallbackNameString = _target.Contracts.DacStreams.StringFromEEAddress(methodDesc);
                    if (!string.IsNullOrEmpty(fallbackNameString))
                    {
                        stringBuilder.Clear();
                        stringBuilder.Append(fallbackNameString);
                        hr = HResults.S_OK;
                    }
                    else
                    {
                        TargetPointer modulePtr = rtsContract.GetModule(rtsContract.GetTypeHandle(rtsContract.GetMethodTable(methodDescHandle)));
                        Contracts.ModuleHandle module = _target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
                        string modulePath = _target.Contracts.Loader.GetPath(module);
                        ReadOnlySpan<char> moduleSpan = modulePath.AsSpan();
                        char directorySeparator = (char)_target.ReadGlobal<byte>(Constants.Globals.DirectorySeparator);

                        int pathNameSpanIndex = moduleSpan.LastIndexOf(directorySeparator);
                        if (pathNameSpanIndex != -1)
                        {
                            moduleSpan = moduleSpan.Slice(pathNameSpanIndex + 1);
                        }
                        stringBuilder.Clear();
                        stringBuilder.Append(moduleSpan);
                        stringBuilder.Append("!Unknown");
                        hr = HResults.S_OK;
                    }
                }
            }

            if (hr == HResults.S_OK)
            {
                OutputBufferHelpers.CopyStringToBuffer(name, count, pNeeded, stringBuilder.ToString());
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;
        try
        {
            if (frameAddr == 0 || ppMD == null)
                throw new ArgumentException();

            IStackWalk stackWalkContract = _target.Contracts.StackWalk;
            TargetPointer methodDescPtr = stackWalkContract.GetMethodDescPtr(frameAddr.ToTargetPointer(_target));
            if (methodDescPtr == TargetPointer.Null)
                throw new ArgumentException();

            _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(methodDescPtr); // validation
            *ppMD = methodDescPtr.ToClrDataAddress(_target);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress ppMDLocal;
            int hrLocal = _legacyImpl.GetMethodDescPtrFromFrame(frameAddr, &ppMDLocal);

            Debug.Assert(hrLocal == hr);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*ppMD == ppMDLocal);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetMethodDescPtrFromIP(ClrDataAddress ip, ClrDataAddress* ppMD)
    {
        if (ip == 0 || ppMD == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.E_NOTIMPL;

        try
        {
            IExecutionManager executionManager = _target.Contracts.ExecutionManager;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            CodeBlockHandle? handle = executionManager.GetCodeBlockHandle(ip.ToTargetCodePointer(_target));
            if (handle is not CodeBlockHandle codeHandle)
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            TargetPointer methodDescAddr = executionManager.GetMethodDesc(codeHandle);

            try
            {
                // Runs validation of MethodDesc
                // if validation fails, should return E_INVALIDARG
                rts.GetMethodDescHandle(methodDescAddr);

                *ppMD = methodDescAddr.ToClrDataAddress(_target);
                hr = HResults.S_OK;
            }
            catch (System.Exception)
            {
                hr = HResults.E_INVALIDARG;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress ppMDLocal;
            int hrLocal = _legacyImpl.GetMethodDescPtrFromIP(ip, &ppMDLocal);

            Debug.Assert(hrLocal == hr);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*ppMD == ppMDLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodDescTransparencyData(ClrDataAddress methodDesc, DacpMethodDescTransparencyData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (methodDesc == 0 || data is null)
                throw new ArgumentException();

            // Called for validation
            _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(methodDesc.ToTargetPointer(_target));

            // Zero memory
            *data = default;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int ISOSDacInterface.GetMethodTableData(ClrDataAddress mt, DacpMethodTableData* data)
    {
        if (mt == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem contract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTable = contract.GetTypeHandle(mt.ToTargetPointer(_target));

            DacpMethodTableData result = default;
            result.baseSize = contract.GetBaseSize(methodTable);
            // [compat] SOS DAC APIs added this base size adjustment for strings
            // due to: "2008/09/25 Title: New implementation of StringBuilder and improvements in String class"
            // which changed StringBuilder not to use a String as an internal buffer and in the process
            // changed the String internals so that StringObject::GetBaseSize() now includes the nul terminator character,
            // which is apparently not expected by SOS.
            if (contract.IsString(methodTable))
                result.baseSize -= sizeof(char);

            result.componentSize = contract.GetComponentSize(methodTable);
            bool isFreeObjectMT = contract.IsFreeObjectMethodTable(methodTable);
            result.bIsFree = isFreeObjectMT ? 1 : 0;
            if (!isFreeObjectMT)
            {
                result.module = contract.GetModule(methodTable).ToClrDataAddress(_target);
                // Note: really the canonical method table, not the EEClass, which we don't expose
                result.klass = contract.GetCanonicalMethodTable(methodTable).ToClrDataAddress(_target);
                result.parentMethodTable = contract.GetParentMethodTable(methodTable).ToClrDataAddress(_target);
                result.wNumInterfaces = contract.GetNumInterfaces(methodTable);
                result.wNumMethods = contract.GetNumMethods(methodTable);
                result.wNumVtableSlots = 0; // always return 0 since .NET 9
                result.wNumVirtuals = 0; // always return 0 since .NET 9
                result.cl = contract.GetTypeDefToken(methodTable);
                result.dwAttrClass = contract.GetTypeDefTypeAttributes(methodTable);
                result.bContainsGCPointers = contract.ContainsGCPointers(methodTable) ? 1 : 0;
                result.bIsShared = 0;
                result.bIsDynamic = contract.IsDynamicStatics(methodTable) ? 1 : 0;
            }
            *data = result;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpMethodTableData dataLocal;
            int hrLocal = _legacyImpl.GetMethodTableData(mt, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface.GetMethodTableFieldData(ClrDataAddress mt, DacpMethodTableFieldData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (mt == 0 || data == null)
                throw new ArgumentException();

            TargetPointer mtAddress = mt.ToTargetPointer(_target);
            Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
            TypeHandle typeHandle = rtsContract.GetTypeHandle(mtAddress);
            data->FirstField = rtsContract.GetFieldDescList(typeHandle).ToClrDataAddress(_target);
            data->wNumInstanceFields = rtsContract.GetNumInstanceFields(typeHandle);
            data->wNumStaticFields = rtsContract.GetNumStaticFields(typeHandle);
            data->wNumThreadStaticFields = rtsContract.GetNumThreadStaticFields(typeHandle);
            data->wContextStaticsSize = 0;
            data->wContextStaticOffset = 0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        {
            if (_legacyImpl is not null)
            {
                DacpMethodTableFieldData mtFieldDataLocal = default;
                int hrLocal = _legacyImpl.GetMethodTableFieldData(mt, &mtFieldDataLocal);
                Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface.GetMethodTableForEEClass(ClrDataAddress eeClassReallyCanonMT, ClrDataAddress* value)
    {
        if (eeClassReallyCanonMT == 0 || value == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem contract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTableHandle = contract.GetTypeHandle(eeClassReallyCanonMT.ToTargetPointer(_target));
            *value = methodTableHandle.Address.ToClrDataAddress(_target);
        }
        catch (global::System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress valueLocal;
            int hrLocal = _legacyImpl.GetMethodTableForEEClass(eeClassReallyCanonMT, &valueLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
                Debug.Assert(*value == valueLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodTableName(ClrDataAddress mt, uint count, char* mtName, uint* pNeeded)
    {
        int hr = HResults.S_OK;
        try
        {
            if (mt == 0)
                throw new ArgumentException();
            Contracts.IRuntimeTypeSystem typeSystemContract = _target.Contracts.RuntimeTypeSystem;
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.TypeHandle methodTableHandle = typeSystemContract.GetTypeHandle(mt.ToTargetPointer(_target, overrideCheck: true));
            if (typeSystemContract.IsFreeObjectMethodTable(methodTableHandle))
            {
                OutputBufferHelpers.CopyStringToBuffer(mtName, count, pNeeded, "Free");
            }
            else
            {
                TargetPointer modulePointer = typeSystemContract.GetModule(methodTableHandle);
                Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePointer);
                if (!loader.TryGetLoadedImageContents(moduleHandle, out _, out _, out _))
                {
                    OutputBufferHelpers.CopyStringToBuffer(mtName, count, pNeeded, "<Unloaded Type>");
                }
                else
                {
                    System.Text.StringBuilder methodTableName = new();
                    try
                    {
                        TypeNameBuilder.AppendType(_target, methodTableName, methodTableHandle, TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
                    }
                    catch
                    {
                        string? fallbackName = _target.Contracts.DacStreams.StringFromEEAddress(mt.ToTargetPointer(_target));
                        if (fallbackName != null)
                        {
                            methodTableName.Clear();
                            methodTableName.Append(fallbackName);
                        }
                    }
                    OutputBufferHelpers.CopyStringToBuffer(mtName, count, pNeeded, methodTableName.ToString());
                }
            }

        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(mtName == null || new ReadOnlySpan<char>(mtNameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(mtName)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodTableSlot(ClrDataAddress mt, uint slot, ClrDataAddress* value)
    {
        int hr = HResults.S_OK;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

        try
        {
            if (mt == 0 || value == null)
                throw new ArgumentException();

            TargetPointer methodTable = mt.ToTargetPointer(_target);
            TypeHandle methodTableHandle = rts.GetTypeHandle(methodTable); // validate MT

            ushort vtableSlots = rts.GetNumVtableSlots(methodTableHandle);

            if (slot < vtableSlots)
            {
                *value = rts.GetSlot(methodTableHandle, slot).ToClrDataAddress(_target);
                if (*value == 0)
                {
                    hr = HResults.S_FALSE;
                }
            }
            else
            {
                hr = HResults.E_INVALIDARG;
                foreach (TargetPointer mdAddr in rts.GetIntroducedMethodDescs(methodTableHandle))
                {
                    MethodDescHandle mdHandle = rts.GetMethodDescHandle(mdAddr);
                    if (rts.GetSlotNumber(mdHandle) == slot)
                    {
                        *value = rts.GetMethodEntryPointIfExists(mdHandle).ToClrDataAddress(_target);
                        if (*value == 0)
                        {
                            hr = HResults.S_FALSE;
                        }
                        else
                        {
                            hr = HResults.S_OK;
                        }
                        break;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal;
            ClrDataAddress valueLocal;

            hrLocal = _legacyImpl.GetMethodTableSlot(mt, slot, &valueLocal);

            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*value == valueLocal, $"cDAC: {*value:x}, DAC: {valueLocal:x}");
            }
        }
#endif

        return hr;
    }

    int ISOSDacInterface.GetMethodTableTransparencyData(ClrDataAddress mt, DacpMethodTableTransparencyData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (mt == 0 || data is null)
                throw new ArgumentException();

            // Called for validation
            _target.Contracts.RuntimeTypeSystem.GetTypeHandle(mt.ToTargetPointer(_target));

            // Zero memory
            *data = default;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int ISOSDacInterface.GetModule(ClrDataAddress addr, out IXCLRDataModule? mod)
    {
        mod = default;

        IXCLRDataModule? legacyModule = null;
        if (_legacyImpl is not null)
        {
            int hr = _legacyImpl.GetModule(addr, out legacyModule);
            if (hr < 0)
                return hr;
        }

        mod = new ClrDataModule(addr.ToTargetPointer(_target), _target, legacyModule);
        return HResults.S_OK;
    }

    int ISOSDacInterface.GetModuleData(ClrDataAddress moduleAddr, DacpModuleData* data)
    {
        if (moduleAddr == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr.ToTargetPointer(_target));

            data->Address = moduleAddr;
            data->PEAssembly = moduleAddr; // Module address in .NET 9+ - correspondingly, SOS-DAC APIs for PE assemblies expect a module address
            data->Assembly = contract.GetAssembly(handle).ToClrDataAddress(_target);

            Contracts.ModuleFlags flags = contract.GetFlags(handle);
            bool isReflectionEmit = flags.HasFlag(Contracts.ModuleFlags.ReflectionEmit);
            data->isReflection = (uint)(isReflectionEmit ? 1 : 0);
            data->isPEFile = (uint)(isReflectionEmit ? 0 : 1);      // ReflectionEmit module means it is not a PE file
            data->dwTransientFlags = (uint)flags;

            data->ilBase = contract.GetILBase(handle).ToClrDataAddress(_target);

            try
            {
                TargetSpan readOnlyMetadata = _target.Contracts.EcmaMetadata.GetReadOnlyMetadataAddress(handle);
                data->metadataStart = readOnlyMetadata.Address.Value;
                data->metadataSize = readOnlyMetadata.Size;
            }
            catch (System.Exception)
            {
                // if we are unable to read the metadata, to match the DAC behavior
                // set metadataStart and metadataSize to 0
                data->metadataStart = 0;
                data->metadataSize = 0;
            }

            data->LoaderAllocator = contract.GetLoaderAllocator(handle).ToClrDataAddress(_target);

            Target.TypeInfo lookupMapTypeInfo = _target.GetTypeInfo(DataType.ModuleLookupMap);
            ulong tableDataOffset = (ulong)lookupMapTypeInfo.Fields[Constants.FieldNames.ModuleLookupMap.TableData].Offset;

            Contracts.ModuleLookupTables tables = contract.GetLookupTables(handle);
            data->FieldDefToDescMap = _target.ReadPointer(tables.FieldDefToDesc + tableDataOffset).ToClrDataAddress(_target);
            data->ManifestModuleReferencesMap = _target.ReadPointer(tables.ManifestModuleReferences + tableDataOffset).ToClrDataAddress(_target);
            data->MemberRefToDescMap = _target.ReadPointer(tables.MemberRefToDesc + tableDataOffset).ToClrDataAddress(_target);
            data->MethodDefToDescMap = _target.ReadPointer(tables.MethodDefToDesc + tableDataOffset).ToClrDataAddress(_target);
            data->TypeDefToMethodTableMap = _target.ReadPointer(tables.TypeDefToMethodTable + tableDataOffset).ToClrDataAddress(_target);
            data->TypeRefToMethodTableMap = _target.ReadPointer(tables.TypeRefToMethodTable + tableDataOffset).ToClrDataAddress(_target);

            // Always 0 - .NET no longer has these concepts
            data->dwModuleID = 0;
            data->dwBaseClassIndex = 0;
            data->dwModuleIndex = 0;
            data->ThunkHeap = 0;
        }
        catch (global::System.Exception e)
        {
            hr = e.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpModuleData dataLocal;
            int hrLocal = _legacyImpl.GetModuleData(moduleAddr, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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

    int ISOSDacInterface.GetNestedExceptionData(ClrDataAddress exception, ClrDataAddress* exceptionObject, ClrDataAddress* nextNestedException)
    {
        if (exception == 0 || exceptionObject == null || nextNestedException == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IException contract = _target.Contracts.Exception;
            TargetPointer exceptionObjectLocal = contract.GetNestedExceptionInfo(
                exception.ToTargetPointer(_target),
                out TargetPointer nextNestedExceptionLocal);
            *exceptionObject = exceptionObjectLocal.ToClrDataAddress(_target);
            *nextNestedException = nextNestedExceptionLocal.Value;
        }
        catch (global::System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress exceptionObjectLocal;
            ClrDataAddress nextNestedExceptionLocal;
            int hrLocal = _legacyImpl.GetNestedExceptionData(exception, &exceptionObjectLocal, &nextNestedExceptionLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*exceptionObject == exceptionObjectLocal);
                Debug.Assert(*nextNestedException == nextNestedExceptionLocal);
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetObjectClassName(ClrDataAddress obj, uint count, char* className, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetObjectClassName(obj, count, className, pNeeded) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetObjectData(ClrDataAddress objAddr, DacpObjectData* data)
    {
        if (objAddr == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IObject objectContract = _target.Contracts.Object;
            Contracts.IRuntimeTypeSystem runtimeTypeSystemContract = _target.Contracts.RuntimeTypeSystem;

            TargetPointer objPtr = objAddr.ToTargetPointer(_target);
            TargetPointer mt = objectContract.GetMethodTableAddress(objPtr);
            TypeHandle handle = runtimeTypeSystemContract.GetTypeHandle(mt);

            data->MethodTable = mt.ToClrDataAddress(_target);
            data->Size = runtimeTypeSystemContract.GetBaseSize(handle);
            data->dwComponentSize = runtimeTypeSystemContract.GetComponentSize(handle);

            if (runtimeTypeSystemContract.IsFreeObjectMethodTable(handle))
            {
                data->ObjectType = DacpObjectType.OBJ_FREE;

                // Free objects have their component count explicitly set at the same offset as that for arrays
                // Update the size to include those components
                Target.TypeInfo arrayTypeInfo = _target.GetTypeInfo(DataType.Array);
                ulong numComponentsOffset = (ulong)_target.GetTypeInfo(DataType.Array).Fields[Constants.FieldNames.Array.NumComponents].Offset;
                data->Size += _target.Read<uint>(objAddr + numComponentsOffset) * data->dwComponentSize;
            }
            else if (mt == _stringMethodTable.Value)
            {
                data->ObjectType = DacpObjectType.OBJ_STRING;

                // Update the size to include the string character components
                data->Size += (uint)objectContract.GetStringValue(objPtr).Length * data->dwComponentSize;
            }
            else if (mt == _objectMethodTable.Value)
            {
                data->ObjectType = DacpObjectType.OBJ_OBJECT;
            }
            else if (runtimeTypeSystemContract.IsArray(handle, out uint rank))
            {
                data->ObjectType = DacpObjectType.OBJ_ARRAY;
                data->dwRank = rank;

                TargetPointer arrayData = objectContract.GetArrayData(objPtr, out uint numComponents, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                data->ArrayDataPtr = arrayData.ToClrDataAddress(_target);
                data->dwNumComponents = numComponents;
                data->ArrayBoundsPtr = boundsStart.ToClrDataAddress(_target);
                data->ArrayLowerBoundsPtr = lowerBounds.ToClrDataAddress(_target);

                // Update the size to include the array components
                data->Size += numComponents * data->dwComponentSize;

                // Get the type of the array elements
                TypeHandle element = runtimeTypeSystemContract.GetTypeParam(handle);
                data->ElementTypeHandle = element.Address.Value;
                data->ElementType = (uint)runtimeTypeSystemContract.GetSignatureCorElementType(element);

                // Validate the element type handles for arrays of arrays
                while (runtimeTypeSystemContract.IsArray(element, out _))
                {
                    element = runtimeTypeSystemContract.GetTypeParam(element);
                }
            }
            else
            {
                data->ObjectType = DacpObjectType.OBJ_OTHER;
            }

            // Populate COM data if this is a COM object
            if (_target.ReadGlobal<byte>(Constants.Globals.FeatureCOMInterop) != 0
                && objectContract.GetBuiltInComData(objPtr, out TargetPointer rcw, out TargetPointer ccw))
            {
                data->RCW = rcw;
                data->CCW = ccw;
            }

        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpObjectData dataLocal;
            int hrLocal = _legacyImpl.GetObjectData(objAddr, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        if (obj == 0 || (stringData == null && pNeeded == null) || (stringData is not null && count <= 0))
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IObject contract = _target.Contracts.Object;
            string str = contract.GetStringValue(obj.ToTargetPointer(_target));
            OutputBufferHelpers.CopyStringToBuffer(stringData, count, pNeeded, str);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            char[] stringDataLocal = new char[count];
            uint neededLocal;
            int hrLocal;
            fixed (char* ptr = stringDataLocal)
            {
                hrLocal = _legacyImpl.GetObjectStringData(obj, count, ptr, &neededLocal);
            }
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(stringData == null || new ReadOnlySpan<char>(stringDataLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(stringData)));
            }
        }
#endif

        return hr;
    }

    int ISOSDacInterface.GetOOMData(ClrDataAddress oomAddr, DacpOomData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (oomAddr == 0 || data == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // This method is only valid for server GC mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Server))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            GCOomData oomData = gc.GetOomData(oomAddr.ToTargetPointer(_target));

            data->reason = oomData.Reason;
            data->alloc_size = oomData.AllocSize.Value;
            data->available_pagefile_mb = oomData.AvailablePagefileMB.Value;
            data->gc_index = oomData.GCIndex.Value;
            data->fgm = oomData.Fgm;
            data->size = oomData.Size.Value;
            data->loh_p = oomData.LohP ? (int)Interop.BOOL.TRUE : (int)Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpOomData dataLocal;
            int hrLocal = _legacyImpl.GetOOMData(oomAddr, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;
        try
        {
            if (data == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // This method is only valid for workstation GC mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Workstation))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;
            GCOomData oomData = gc.GetOomData();

            data->reason = oomData.Reason;
            data->alloc_size = oomData.AllocSize.Value;
            data->available_pagefile_mb = oomData.AvailablePagefileMB.Value;
            data->gc_index = oomData.GCIndex.Value;
            data->fgm = oomData.Fgm;
            data->size = oomData.Size.Value;
            data->loh_p = oomData.LohP ? (int)Interop.BOOL.TRUE : (int)Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpOomData dataLocal;
            int hrLocal = _legacyImpl.GetOOMStaticData(&dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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

    int ISOSDacInterface.GetPEFileBase(ClrDataAddress addr, ClrDataAddress* peBase)
    {
        if (addr == 0 || peBase == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(addr.ToTargetPointer(_target));
            Contracts.ModuleFlags flags = contract.GetFlags(handle);

            if (!flags.HasFlag(Contracts.ModuleFlags.ReflectionEmit))
            {
                *peBase = contract.GetILBase(handle).ToClrDataAddress(_target);
            }
            else
            {
                *peBase = 0;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress peBaseLocal;
            int hrLocal = _legacyImpl.GetPEFileBase(addr, &peBaseLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
                Debug.Assert(*peBase == peBaseLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetPEFileName(ClrDataAddress addr, uint count, char* fileName, uint* pNeeded)
    {
        if (addr == 0 || (fileName == null && pNeeded == null) || (fileName is not null && count <= 0))
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(addr.ToTargetPointer(_target));
            string path = contract.GetPath(handle);

            // Return not implemented for empty paths for non-reflection emit assemblies (for example, loaded from memory)
            if (string.IsNullOrEmpty(path))
            {
                Contracts.ModuleFlags flags = contract.GetFlags(handle);
                if (!flags.HasFlag(Contracts.ModuleFlags.ReflectionEmit))
                {
                    return HResults.E_NOTIMPL;
                }
            }

            OutputBufferHelpers.CopyStringToBuffer(fileName, count, pNeeded, path);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(pNeeded == null || *pNeeded == neededLocal);
                Debug.Assert(fileName == null || new ReadOnlySpan<char>(fileNameLocal, 0, (int)neededLocal - 1).SequenceEqual(new string(fileName)));
            }
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetPrivateBinPaths(ClrDataAddress appDomain, int count, char* paths, uint* pNeeded)
    {
        // Method is not supported on CoreCLR
        int hr = HResults.E_NOTIMPL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetPrivateBinPaths(appDomain, count, paths, pNeeded);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }
    int ISOSDacInterface.GetRCWData(ClrDataAddress addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetRCWData(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetRCWInterfaces(ClrDataAddress rcw, uint count, void* interfaces, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetRCWInterfaces(rcw, count, interfaces, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetRegisterName(int regName, uint count, char* buffer, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetRegisterName(regName, count, buffer, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetStackLimits(ClrDataAddress threadPtr, ClrDataAddress* lower, ClrDataAddress* upper, ClrDataAddress* fp)
        => _legacyImpl is not null ? _legacyImpl.GetStackLimits(threadPtr, lower, upper, fp) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetStackReferences(int osThreadID, void** ppEnum)
        => _legacyImpl is not null ? _legacyImpl.GetStackReferences(osThreadID, ppEnum) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetStressLogAddress(ClrDataAddress* stressLog)
    {
        ulong stressLogAddress = _target.ReadGlobalPointer(Constants.Globals.StressLog);

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress legacyStressLog;
            Debug.Assert(HResults.S_OK == _legacyImpl.GetStressLogAddress(&legacyStressLog));
            Debug.Assert(legacyStressLog == stressLogAddress);
        }
#endif
        *stressLog = stressLogAddress;
        return HResults.S_OK;
    }

    int ISOSDacInterface.GetSyncBlockCleanupData(ClrDataAddress addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetSyncBlockCleanupData(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetSyncBlockData(uint number, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetSyncBlockData(number, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetThreadAllocData(ClrDataAddress thread, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetThreadAllocData(thread, data) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetThreadData(ClrDataAddress thread, DacpThreadData* data)
    {
        if (thread == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IThread contract = _target.Contracts.Thread;
            Contracts.ThreadData threadData = contract.GetThreadData(thread.ToTargetPointer(_target));
            data->corThreadId = (int)threadData.Id;
            data->osThreadId = (int)threadData.OSId.Value;
            data->state = (int)threadData.State;
            data->preemptiveGCDisabled = (uint)(threadData.PreemptiveGCDisabled ? 1 : 0);
            data->allocContextPtr = threadData.AllocContextPointer.ToClrDataAddress(_target);
            data->allocContextLimit = threadData.AllocContextLimit.ToClrDataAddress(_target);
            data->fiberData = 0;    // Always set to 0 - fibers are no longer supported

            TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            TargetPointer appDomain = _target.ReadPointer(appDomainPointer);
            data->context = appDomain.ToClrDataAddress(_target);
            data->domain = appDomain.ToClrDataAddress(_target);

            data->lockCount = -1;   // Always set to -1 - lock count was .NET Framework and no longer needed
            data->pFrame = threadData.Frame.ToClrDataAddress(_target);
            data->firstNestedException = threadData.FirstNestedException.ToClrDataAddress(_target);
            data->teb = threadData.TEB.ToClrDataAddress(_target);
            data->lastThrownObjectHandle = threadData.LastThrownObjectHandle.ToClrDataAddress(_target);
            data->nextThread = threadData.NextThread.ToClrDataAddress(_target);
        }
        catch (global::System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpThreadData dataLocal;
            int hrLocal = _legacyImpl.GetThreadData(thread, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->corThreadId == dataLocal.corThreadId, $"cDAC: {data->corThreadId}, DAC: {dataLocal.corThreadId}");
                Debug.Assert(data->osThreadId == dataLocal.osThreadId, $"cDAC: {data->osThreadId}, DAC: {dataLocal.osThreadId}");
                Debug.Assert(data->state == dataLocal.state, $"cDAC: {data->state}, DAC: {dataLocal.state}");
                Debug.Assert(data->preemptiveGCDisabled == dataLocal.preemptiveGCDisabled, $"cDAC: {data->preemptiveGCDisabled}, DAC: {dataLocal.preemptiveGCDisabled}");
                Debug.Assert(data->allocContextPtr == dataLocal.allocContextPtr, $"cDAC: {data->allocContextPtr:x}, DAC: {dataLocal.allocContextPtr:x}");
                Debug.Assert(data->allocContextLimit == dataLocal.allocContextLimit, $"cDAC: {data->allocContextLimit:x}, DAC: {dataLocal.allocContextLimit:x}");
                Debug.Assert(data->fiberData == dataLocal.fiberData, $"cDAC: {data->fiberData:x}, DAC: {dataLocal.fiberData:x}");
                Debug.Assert(data->context == dataLocal.context, $"cDAC: {data->context:x}, DAC: {dataLocal.context:x}");
                Debug.Assert(data->domain == dataLocal.domain, $"cDAC: {data->domain:x}, DAC: {dataLocal.domain:x}");
                Debug.Assert(data->lockCount == dataLocal.lockCount, $"cDAC: {data->lockCount}, DAC: {dataLocal.lockCount}");
                Debug.Assert(data->pFrame == dataLocal.pFrame, $"cDAC: {data->pFrame:x}, DAC: {dataLocal.pFrame:x}");
                Debug.Assert(data->firstNestedException == dataLocal.firstNestedException, $"cDAC: {data->firstNestedException:x}, DAC: {dataLocal.firstNestedException:x}");
                Debug.Assert(data->teb == dataLocal.teb, $"cDAC: {data->teb:x}, DAC: {dataLocal.teb:x}");
                Debug.Assert(data->lastThrownObjectHandle == dataLocal.lastThrownObjectHandle, $"cDAC: {data->lastThrownObjectHandle:x}, DAC: {dataLocal.lastThrownObjectHandle:x}");
                Debug.Assert(data->nextThread == dataLocal.nextThread, $"cDAC: {data->nextThread:x}, DAC: {dataLocal.nextThread:x}");
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetThreadFromThinlockID(uint thinLockId, ClrDataAddress* pThread)
    {
        int hr = HResults.S_OK;
        if (pThread == null)
            hr = HResults.E_INVALIDARG;
        try
        {
            TargetPointer threadPtr = _target.Contracts.Thread.IdToThread(thinLockId);
            *pThread = threadPtr.ToClrDataAddress(_target);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress pThreadLocal;
            int hrLocal = _legacyImpl.GetThreadFromThinlockID(thinLockId, &pThreadLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pThread == pThreadLocal);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetThreadLocalModuleData(ClrDataAddress thread, uint index, void* data)
    {
        // CoreCLR does not use thread local modules anymore
        int hr = HResults.E_NOTIMPL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetThreadLocalModuleData(thread, index, data);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }

    int ISOSDacInterface.GetThreadpoolData(void* data)
    {
        // This API is not implemented by the legacy DAC
        int hr = HResults.E_NOTIMPL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetThreadpoolData(data);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }

    int ISOSDacInterface.GetThreadStoreData(DacpThreadStoreData* data)
    {
        if (data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IThread thread = _target.Contracts.Thread;
            Contracts.ThreadStoreData threadStoreData = thread.GetThreadStoreData();
            data->threadCount = threadStoreData.ThreadCount;
            data->firstThread = threadStoreData.FirstThread.ToClrDataAddress(_target);
            data->finalizerThread = threadStoreData.FinalizerThread.ToClrDataAddress(_target);
            data->gcThread = threadStoreData.GCThread.ToClrDataAddress(_target);

            Contracts.ThreadStoreCounts threadCounts = thread.GetThreadCounts();
            data->unstartedThreadCount = threadCounts.UnstartedThreadCount;
            data->backgroundThreadCount = threadCounts.BackgroundThreadCount;
            data->pendingThreadCount = threadCounts.PendingThreadCount;
            data->deadThreadCount = threadCounts.DeadThreadCount;

            data->fHostConfig = 0; // Always 0 for non-Framework
        }
        catch (global::System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpThreadStoreData dataLocal;
            int hrLocal = _legacyImpl.GetThreadStoreData(&dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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

    int ISOSDacInterface.GetTLSIndex(uint* pIndex)
    {
        if (pIndex == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            uint TlsIndexBase = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.TlsIndexBase));
            uint OffsetOfCurrentThreadInfo = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.OffsetOfCurrentThreadInfo));
            uint CombinedTlsIndex = TlsIndexBase + (OffsetOfCurrentThreadInfo << 16) + 0x80000000;
            *pIndex = CombinedTlsIndex;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint indexLocal;
            int hrLocal = _legacyImpl.GetTLSIndex(&indexLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*pIndex == indexLocal);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetUsefulGlobals(DacpUsefulGlobalsData* data)
    {
        if (data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            data->ArrayMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ObjectArrayMethodTable))
                .ToClrDataAddress(_target);
            data->StringMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.StringMethodTable))
                .ToClrDataAddress(_target);
            data->ObjectMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable))
                .ToClrDataAddress(_target);
            data->ExceptionMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ExceptionMethodTable))
                .ToClrDataAddress(_target);
            data->FreeMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable))
                .ToClrDataAddress(_target);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;

            // There are some scenarios where SOS can call GetUsefulGlobals before the globals are initialized,
            // in these cases set the method table pointers to 0 and assert that the legacy DAC returns the same
            // uninitialized values.
            data->ArrayMethodTable = 0;
            data->StringMethodTable = 0;
            data->ObjectMethodTable = 0;
            data->ExceptionMethodTable = 0;
            data->FreeMethodTable = 0;
        }

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

    int ISOSDacInterface.GetWorkRequestData(ClrDataAddress addrWorkRequest, void* data)
    {
        // This API is not implemented by the legacy DAC
        int hr = HResults.E_NOTIMPL;

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.GetWorkRequestData(addrWorkRequest, data);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }
    int ISOSDacInterface.TraverseEHInfo(ClrDataAddress ip, void* pCallback, void* token)
        => _legacyImpl is not null ? _legacyImpl.TraverseEHInfo(ip, pCallback, token) : HResults.E_NOTIMPL;
    int ISOSDacInterface.TraverseLoaderHeap(ClrDataAddress loaderHeapAddr, void* pCallback)
        => _legacyImpl is not null ? _legacyImpl.TraverseLoaderHeap(loaderHeapAddr, pCallback) : HResults.E_NOTIMPL;

#if DEBUG
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static void TraverseModuleMapCallback(uint index, ulong moduleAddr, void* expectedElements)
    {
        var expectedElementsDict = (Dictionary<ulong, uint>)GCHandle.FromIntPtr((nint)expectedElements).Target!;
        if (expectedElementsDict.TryGetValue(moduleAddr, out uint expectedIndex) && expectedIndex == index)
        {
            expectedElementsDict[default]++; // Increment the count for verification
        }
        else
        {
            Debug.Assert(false, $"Unexpected module address {moduleAddr:x} at index {index}");
        }
    }
#endif
    int ISOSDacInterface.TraverseModuleMap(ModuleMapType mmt, ClrDataAddress moduleAddr, delegate* unmanaged[Stdcall]<uint, ulong, void*, void> pCallback, void* token)
    {
        int hr = HResults.S_OK;
        IEnumerable<(TargetPointer Address, uint Index)> elements = Enumerable.Empty<(TargetPointer, uint)>();
        if (moduleAddr == 0)
            hr = HResults.E_INVALIDARG;
        else
        {
            try
            {
                Contracts.ILoader loader = _target.Contracts.Loader;
                TargetPointer moduleAddrPtr = moduleAddr.ToTargetPointer(_target);
                Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(moduleAddrPtr);
                Contracts.ModuleLookupTables lookupTables = loader.GetLookupTables(moduleHandle);
                switch (mmt)
                {
                    case ModuleMapType.TYPEDEFTOMETHODTABLE:
                        elements = loader.EnumerateModuleLookupMap(lookupTables.TypeDefToMethodTable);
                        break;
                    case ModuleMapType.TYPEREFTOMETHODTABLE:
                        elements = loader.EnumerateModuleLookupMap(lookupTables.TypeRefToMethodTable);
                        break;
                    default:
                        hr = HResults.E_INVALIDARG;
                        break;
                }
                if (hr == HResults.S_OK)
                {
                    foreach ((TargetPointer element, uint index) in elements)
                    {
                        // Call the callback with each element
                        pCallback(index, element.ToClrDataAddress(_target).Value, token);
                    }
                }
            }
            catch (System.Exception ex)
            {
                hr = ex.HResult;
            }
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            Dictionary<ulong, uint> expectedElements = elements.ToDictionary(tuple => tuple.Address.ToClrDataAddress(_target).Value, tuple => tuple.Index);
            expectedElements.Add(default, 0);
            void* tokenDebug = GCHandle.ToIntPtr(GCHandle.Alloc(expectedElements)).ToPointer();
            delegate* unmanaged[Stdcall]<uint, ulong, void*, void> callbackDebugPtr = &TraverseModuleMapCallback;

            int hrLocal = _legacyImpl.TraverseModuleMap(mmt, moduleAddr, callbackDebugPtr, tokenDebug);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            Debug.Assert(expectedElements[default] == elements.Count(), $"cDAC: {elements.Count()} elements, DAC: {expectedElements[default]} elements");
            GCHandle.FromIntPtr((nint)tokenDebug).Free();
        }
#endif
        return hr;
    }
    int ISOSDacInterface.TraverseRCWCleanupList(ClrDataAddress cleanupListPtr, void* pCallback, void* token)
        => _legacyImpl is not null ? _legacyImpl.TraverseRCWCleanupList(cleanupListPtr, pCallback, token) : HResults.E_NOTIMPL;
    int ISOSDacInterface.TraverseVirtCallStubHeap(ClrDataAddress pAppDomain, int heaptype, void* pCallback)
        => _legacyImpl is not null ? _legacyImpl.TraverseVirtCallStubHeap(pAppDomain, heaptype, pCallback) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface

    #region ISOSDacInterface2
    int ISOSDacInterface2.GetObjectExceptionData(ClrDataAddress objectAddress, DacpExceptionObjectData* data)
    {
        try
        {
            Contracts.IException contract = _target.Contracts.Exception;
            Contracts.ExceptionData exceptionData = contract.GetExceptionData(objectAddress.ToTargetPointer(_target));
            data->Message = exceptionData.Message.ToClrDataAddress(_target);
            data->InnerException = exceptionData.InnerException.ToClrDataAddress(_target);
            data->StackTrace = exceptionData.StackTrace.ToClrDataAddress(_target);
            data->WatsonBuckets = exceptionData.WatsonBuckets.ToClrDataAddress(_target);
            data->StackTraceString = exceptionData.StackTraceString.ToClrDataAddress(_target);
            data->RemoteStackTraceString = exceptionData.RemoteStackTraceString.ToClrDataAddress(_target);
            data->HResult = exceptionData.HResult;
            data->XCode = exceptionData.XCode;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    int ISOSDacInterface2.IsRCWDCOMProxy(ClrDataAddress rcwAddress, int* inDCOMProxy)
    {
        int hr = HResults.S_OK;
        try
        {
            if (inDCOMProxy == null)
                throw new NullReferenceException(); // HResults.E_POINTER;

            *inDCOMProxy = (int)Interop.BOOL.FALSE;

            if (_target.ReadGlobal<byte>(Constants.Globals.FeatureCOMInterop) == 0)
            {
                hr = HResults.E_NOTIMPL;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl2 is not null)
        {
            int inDCOMProxyLocal;
            int hrLocal = _legacyImpl2.IsRCWDCOMProxy(rcwAddress, &inDCOMProxyLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;

        try
        {
            if (interestingInfoAddr == 0 || data == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // doesn't make sense to call this on WKS mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Server))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            // For server GC, use GetHeapData(TargetPointer heap)
            GCHeapData heapData = gc.GetHeapData(interestingInfoAddr.ToTargetPointer(_target));

            PopulateGCInterestingInfoData(heapData, data);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl3 is not null)
        {
            DacpGCInterestingInfoData dataLocal = default;
            int hrLocal = _legacyImpl3.GetGCInterestingInfoData(interestingInfoAddr, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;

        try
        {
            if (data == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            string[] gcIdentifiers = gc.GetGCIdentifiers();

            // doesn't make sense to call this on SVR mode
            if (!gcIdentifiers.Contains(GCIdentifiers.Workstation))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            // For workstation GC, use GetHeapData()
            GCHeapData heapData = gc.GetHeapData();

            PopulateGCInterestingInfoData(heapData, data);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl3 is not null)
        {
            DacpGCInterestingInfoData dataLocal = default;
            int hrLocal = _legacyImpl3.GetGCInterestingInfoStaticData(&dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                VerifyGCInterestingInfoData(data, &dataLocal);
            }
        }
#endif

        return hr;
    }

    private static void PopulateGCInterestingInfoData(GCHeapData heapData, DacpGCInterestingInfoData* data)
    {
        *data = default;

        // The DacpGCInterestingInfoData struct hardcodes platform sized ints.
        // This is problematic for new cross-bit scenarios.
        // If the target platform is 64-bits but the cDAC/SOS is 32-bits, the values will be truncated.

        // Copy interesting data points
        for (int i = 0; i < Math.Min(GCConstants.DAC_NUM_GC_DATA_POINTS, heapData.InterestingData.Count); i++)
            data->interestingDataPoints[i] = (nuint)heapData.InterestingData[i].Value;

        // Copy compact reasons
        for (int i = 0; i < Math.Min(GCConstants.DAC_MAX_COMPACT_REASONS_COUNT, heapData.CompactReasons.Count); i++)
            data->compactReasons[i] = (nuint)heapData.CompactReasons[i].Value;

        // Copy expand mechanisms
        for (int i = 0; i < Math.Min(GCConstants.DAC_MAX_EXPAND_MECHANISMS_COUNT, heapData.ExpandMechanisms.Count); i++)
            data->expandMechanisms[i] = (nuint)heapData.ExpandMechanisms[i].Value;

        // Copy interesting mechanism bits
        for (int i = 0; i < Math.Min(GCConstants.DAC_MAX_GC_MECHANISM_BITS_COUNT, heapData.InterestingMechanismBits.Count); i++)
            data->bitMechanisms[i] = (nuint)heapData.InterestingMechanismBits[i].Value;
    }

#if DEBUG
    private static void VerifyGCInterestingInfoData(DacpGCInterestingInfoData* cdacData, DacpGCInterestingInfoData* legacyData)
    {
        // Compare interesting data points array
        for (int i = 0; i < GCConstants.DAC_NUM_GC_DATA_POINTS; i++)
        {
            Debug.Assert(cdacData->interestingDataPoints[i] == legacyData->interestingDataPoints[i],
                $"interestingDataPoints[{i}] - cDAC: {cdacData->interestingDataPoints[i]}, DAC: {legacyData->interestingDataPoints[i]}");
        }

        // Compare compact reasons array
        for (int i = 0; i < GCConstants.DAC_MAX_COMPACT_REASONS_COUNT; i++)
        {
            Debug.Assert(cdacData->compactReasons[i] == legacyData->compactReasons[i],
                $"compactReasons[{i}] - cDAC: {cdacData->compactReasons[i]}, DAC: {legacyData->compactReasons[i]}");
        }

        // Compare expand mechanisms array
        for (int i = 0; i < GCConstants.DAC_MAX_EXPAND_MECHANISMS_COUNT; i++)
        {
            Debug.Assert(cdacData->expandMechanisms[i] == legacyData->expandMechanisms[i],
                $"expandMechanisms[{i}] - cDAC: {cdacData->expandMechanisms[i]}, DAC: {legacyData->expandMechanisms[i]}");
        }

        // Compare bit mechanisms array
        for (int i = 0; i < GCConstants.DAC_MAX_GC_MECHANISM_BITS_COUNT; i++)
        {
            Debug.Assert(cdacData->bitMechanisms[i] == legacyData->bitMechanisms[i],
                $"bitMechanisms[{i}] - cDAC: {cdacData->bitMechanisms[i]}, DAC: {legacyData->bitMechanisms[i]}");
        }

        // Compare global mechanisms array
        for (int i = 0; i < GCConstants.DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT; i++)
        {
            Debug.Assert(cdacData->globalMechanisms[i] == legacyData->globalMechanisms[i],
                $"globalMechanisms[{i}] - cDAC: {cdacData->globalMechanisms[i]}, DAC: {legacyData->globalMechanisms[i]}");
        }
    }
#endif

    int ISOSDacInterface3.GetGCGlobalMechanisms(nuint* globalMechanisms)
    {
        int hr = HResults.S_OK;
        try
        {
            if (globalMechanisms == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            IReadOnlyList<TargetNUInt> globalMechanismsData = gc.GetGlobalMechanisms();

            // Clear the array
            for (int i = 0; i < GCConstants.DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT; i++)
                globalMechanisms[i] = 0;

            // Copy global mechanisms data
            for (int i = 0; i < Math.Min(GCConstants.DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT, globalMechanismsData.Count); i++)
            {
                // This API hardcodes platform sized ints in the struct
                // This is problematic for new cross-bit scenarios.
                // If the target platform is 64-bits but the cDAC/SOS is 32-bits, the values will be truncated.
                globalMechanisms[i] = (nuint)globalMechanismsData[i].Value;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl3 is not null)
        {
            nuint[] globalMechanismsLocal = new nuint[GCConstants.DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT];
            fixed (nuint* pLocal = globalMechanismsLocal)
            {
                int hrLocal = _legacyImpl3.GetGCGlobalMechanisms(pLocal);
                Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;
        uint MaxClrNotificationArgs = _target.ReadGlobal<uint>(Constants.Globals.MaxClrNotificationArgs);
        try
        {
            *pNeeded = (int)MaxClrNotificationArgs;
            TargetPointer basePtr = _target.ReadGlobalPointer(Constants.Globals.ClrNotificationArguments);
            if (_target.ReadNUInt(basePtr).Value == 0)
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            for (int i = 0; i < count && i < MaxClrNotificationArgs; i++)
            {
                arguments[i] = _target.ReadNUInt(basePtr.Value + (ulong)(i * _target.PointerSize)).Value;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl4 is not null)
        {
            ClrDataAddress[] argumentsLocal = new ClrDataAddress[count];
            int neededLocal;
            int hrLocal = _legacyImpl4.GetClrNotification(argumentsLocal, count, &neededLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pNeeded == neededLocal);
                for (int i = 0; i < count && i < MaxClrNotificationArgs; i++)
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
    int ISOSDacInterface5.GetTieredVersions(ClrDataAddress methodDesc, int rejitId, /*struct DacpTieredVersionData*/ void* nativeCodeAddrs, int cNativeCodeAddrs, int* pcNativeCodeAddrs)
        => _legacyImpl5 is not null ? _legacyImpl5.GetTieredVersions(methodDesc, rejitId, nativeCodeAddrs, cNativeCodeAddrs, pcNativeCodeAddrs) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface5

    #region ISOSDacInterface6
    int ISOSDacInterface6.GetMethodTableCollectibleData(ClrDataAddress mt, DacpMethodTableCollectibleData* data)
    {
        int hr = HResults.S_OK;
        try
        {
            if (mt == 0 || data == null)
                throw new ArgumentException();

            Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
            ILoader loaderContract = _target.Contracts.Loader;
            Contracts.TypeHandle typeHandle = rtsContract.GetTypeHandle(mt.ToTargetPointer(_target));

            bool isCollectible = rtsContract.IsCollectible(typeHandle);
            if (isCollectible)
            {
                TargetPointer modulePtr = rtsContract.GetLoaderModule(typeHandle);
                Contracts.ModuleHandle moduleHandle = loaderContract.GetModuleHandleFromModulePtr(modulePtr);
                TargetPointer loaderAllocator = loaderContract.GetLoaderAllocator(moduleHandle);
                TargetPointer loaderAllocatorHandle = loaderContract.GetObjectHandle(loaderAllocator);
                data->LoaderAllocatorObjectHandle = loaderAllocatorHandle.ToClrDataAddress(_target);
            }
            data->bCollectible = isCollectible ? 1 : 0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl6 is not null)
        {
            DacpMethodTableCollectibleData dataLocal;
            int hrLocal = _legacyImpl6.GetMethodTableCollectibleData(mt, &dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        if (methodDesc == 0 || pRejitId == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IReJIT rejitContract = _target.Contracts.ReJIT;
            Contracts.ICodeVersions codeVersionsContract = _target.Contracts.CodeVersions;
            TargetPointer methodDescPtr = methodDesc.ToTargetPointer(_target);
            Contracts.ILCodeVersionHandle activeILCodeVersion = codeVersionsContract.GetActiveILCodeVersion(methodDescPtr);

            if (rejitContract.GetRejitState(activeILCodeVersion) == Contracts.RejitState.Requested)
            {
                *pRejitId = (int)rejitContract.GetRejitId(activeILCodeVersion).Value;
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl7 is not null)
        {
            int rejitIdLocal;
            int hrLocal = _legacyImpl7.GetPendingReJITID(methodDesc, &rejitIdLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;
        try
        {
            if (methodDesc == 0 || pRejitData == null || rejitId < 0)
                throw new ArgumentException();
            ICodeVersions cv = _target.Contracts.CodeVersions;
            IReJIT rejitContract = _target.Contracts.ReJIT;
            TargetPointer methodDescPtr = methodDesc.ToTargetPointer(_target);
            ILCodeVersionHandle ilCodeVersion = cv.GetILCodeVersions(methodDescPtr)
                .FirstOrDefault(ilcode => rejitContract.GetRejitId(ilcode).Value == (ulong)rejitId,
                    ILCodeVersionHandle.Invalid);

            if (!ilCodeVersion.IsValid)
                throw new ArgumentException();
            else
            {
                pRejitData->rejitID = (uint)rejitId;
                switch (rejitContract.GetRejitState(ilCodeVersion))
                {
                    case RejitState.Requested:
                        pRejitData->flags = DacpReJitData2.Flags.kRequested;
                        break;
                    case RejitState.Active:
                        pRejitData->flags = DacpReJitData2.Flags.kActive;
                        break;
                    default:
                        Debug.Assert(true, "Unknown SharedRejitInfo state.  cDAC should be updated to understand this new state.");
                        pRejitData->flags = DacpReJitData2.Flags.kUnknown;
                        break;
                }
                pRejitData->il = cv.GetIL(ilCodeVersion).ToClrDataAddress(_target);
                if (ilCodeVersion.IsExplicit)
                    pRejitData->ilCodeVersionNodePtr = ilCodeVersion.ILCodeVersionNode.ToClrDataAddress(_target);
                else
                    pRejitData->ilCodeVersionNodePtr = 0;
            }
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }
#if DEBUG
        if (_legacyImpl7 is not null)
        {
            DacpReJitData2 rejitDataLocal;
            int hrLocal = _legacyImpl7.GetReJITInformation(methodDesc, rejitId, &rejitDataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    int ISOSDacInterface7.GetProfilerModifiedILInformation(ClrDataAddress methodDesc, /*struct DacpProfilerILData*/ void* pILData)
        => _legacyImpl7 is not null ? _legacyImpl7.GetProfilerModifiedILInformation(methodDesc, pILData) : HResults.E_NOTIMPL;
    int ISOSDacInterface7.GetMethodsWithProfilerModifiedIL(ClrDataAddress mod, ClrDataAddress* methodDescs, int cMethodDescs, int* pcMethodDescs)
        => _legacyImpl7 is not null ? _legacyImpl7.GetMethodsWithProfilerModifiedIL(mod, methodDescs, cMethodDescs, pcMethodDescs) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface7

    #region ISOSDacInterface8
    int ISOSDacInterface8.GetNumberGenerations(uint* pGenerations)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pGenerations == null)
                throw new ArgumentException();

            // Read the total generation count from the global
            uint totalGenerationCount = _target.ReadGlobal<uint>(Constants.Globals.TotalGenerationCount);
            *pGenerations = totalGenerationCount;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl8 is not null)
        {
            uint pGenerationsLocal;
            int hrLocal = _legacyImpl8.GetNumberGenerations(&pGenerationsLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(*pGenerations == pGenerationsLocal);
            }
        }
#endif
        return hr;
    }

    // WKS
    int ISOSDacInterface8.GetGenerationTable(uint cGenerations, /*struct DacpGenerationData*/ void* pGenerationData, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetGenerationTable(cGenerations, pGenerationData, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface8.GetFinalizationFillPointers(uint cFillPointers, ClrDataAddress* pFinalizationFillPointers, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetFinalizationFillPointers(cFillPointers, pFinalizationFillPointers, pNeeded) : HResults.E_NOTIMPL;

    // SVR
    int ISOSDacInterface8.GetGenerationTableSvr(ClrDataAddress heapAddr, uint cGenerations, /*struct DacpGenerationData*/ void* pGenerationData, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetGenerationTableSvr(heapAddr, cGenerations, pGenerationData, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface8.GetFinalizationFillPointersSvr(ClrDataAddress heapAddr, uint cFillPointers, ClrDataAddress* pFinalizationFillPointers, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetFinalizationFillPointersSvr(heapAddr, cFillPointers, pFinalizationFillPointers, pNeeded) : HResults.E_NOTIMPL;

    int ISOSDacInterface8.GetAssemblyLoadContext(ClrDataAddress methodTable, ClrDataAddress* assemblyLoadContext)
    {
        int hr = HResults.S_OK;
        try
        {
            if (methodTable == 0 || assemblyLoadContext == null)
                throw new ArgumentException();

            Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
            Contracts.ILoader loaderContract = _target.Contracts.Loader;
            Contracts.TypeHandle methodTableHandle = rtsContract.GetTypeHandle(methodTable.ToTargetPointer(_target));
            Contracts.ModuleHandle moduleHandle = loaderContract.GetModuleHandleFromModulePtr(rtsContract.GetModule(methodTableHandle));
            TargetPointer alc = loaderContract.GetAssemblyLoadContext(moduleHandle);
            *assemblyLoadContext = alc.ToClrDataAddress(_target);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl8 is not null)
        {
            ClrDataAddress assemblyLoadContextLocal;
            int hrLocal = _legacyImpl8.GetAssemblyLoadContext(methodTable, &assemblyLoadContextLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int version = _target.ReadGlobal<byte>(Constants.Globals.SOSBreakingChangeVersion);

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
    int ISOSDacInterface10.GetObjectComWrappersData(ClrDataAddress objAddr, ClrDataAddress* rcw, uint count, ClrDataAddress* mowList, uint* pNeeded)
        => _legacyImpl10 is not null ? _legacyImpl10.GetObjectComWrappersData(objAddr, rcw, count, mowList, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.IsComWrappersCCW(ClrDataAddress ccw, Interop.BOOL* isComWrappersCCW)
        => _legacyImpl10 is not null ? _legacyImpl10.IsComWrappersCCW(ccw, isComWrappersCCW) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.GetComWrappersCCWData(ClrDataAddress ccw, ClrDataAddress* managedObject, int* refCount)
        => _legacyImpl10 is not null ? _legacyImpl10.GetComWrappersCCWData(ccw, managedObject, refCount) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.IsComWrappersRCW(ClrDataAddress rcw, Interop.BOOL* isComWrappersRCW)
        => _legacyImpl10 is not null ? _legacyImpl10.IsComWrappersRCW(rcw, isComWrappersRCW) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.GetComWrappersRCWData(ClrDataAddress rcw, ClrDataAddress* identity)
    {
        int hr = HResults.S_OK;
        try
        {
            ulong rcwMask = 1UL;
            Contracts.IComWrappers comWrappersContract = _target.Contracts.ComWrappers;
            if (rcw == 0 || identity == null)
                throw new ArgumentException();
            else if ((rcw & rcwMask) == 0)
                *identity = 0;
            else if (identity != null)
            {
                TargetPointer identityPtr = comWrappersContract.GetComWrappersIdentity((rcw.ToTargetPointer(_target) & ~rcwMask));
                *identity = identityPtr.ToClrDataAddress(_target);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl10 is not null)
        {
            ClrDataAddress identityLocal;
            int hrLocal = _legacyImpl10.GetComWrappersRCWData(rcw, &identityLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        => _legacyImpl11 is not null ? _legacyImpl11.IsTrackedType(objAddr, isTrackedType, hasTaggedMemory) : HResults.E_NOTIMPL;
    int ISOSDacInterface11.GetTaggedMemory(ClrDataAddress objAddr, ClrDataAddress* taggedMemory, nuint* taggedMemorySizeInBytes)
        => _legacyImpl11 is not null ? _legacyImpl11.GetTaggedMemory(objAddr, taggedMemory, taggedMemorySizeInBytes) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface11

    #region ISOSDacInterface12
    int ISOSDacInterface12.GetGlobalAllocationContext(ClrDataAddress* allocPtr, ClrDataAddress* allocLimit)
        => _legacyImpl12 is not null ? _legacyImpl12.GetGlobalAllocationContext(allocPtr, allocLimit) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface12

    #region ISOSDacInterface13
    int ISOSDacInterface13.TraverseLoaderHeap(ClrDataAddress loaderHeapAddr, /*LoaderHeapKind*/ int kind, /*VISITHEAP*/ delegate* unmanaged<ulong, nuint, Interop.BOOL> pCallback)
        => _legacyImpl13 is not null ? _legacyImpl13.TraverseLoaderHeap(loaderHeapAddr, kind, pCallback) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetDomainLoaderAllocator(ClrDataAddress domainAddress, ClrDataAddress* pLoaderAllocator)
    {
        int hr = HResults.S_OK;
        try
        {
            if (pLoaderAllocator == null)
                throw new ArgumentException();

            if (domainAddress == 0)
            {
                *pLoaderAllocator = 0;
                hr = HResults.S_FALSE;
            }
            else
            {
                // The one and only app domain uses the global loader allocator
                Contracts.ILoader contract = _target.Contracts.Loader;
                TargetPointer globalLoaderAllocator = contract.GetGlobalLoaderAllocator();
                *pLoaderAllocator = globalLoaderAllocator.ToClrDataAddress(_target);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl13 is not null)
        {
            ClrDataAddress pLoaderAllocatorLocal;
            int hrLocal = _legacyImpl13.GetDomainLoaderAllocator(domainAddress, &pLoaderAllocatorLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(*pLoaderAllocator == pLoaderAllocatorLocal);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface13.GetLoaderAllocatorHeapNames(int count, char** ppNames, int* pNeeded)
        => _legacyImpl13 is not null ? _legacyImpl13.GetLoaderAllocatorHeapNames(count, ppNames, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetLoaderAllocatorHeaps(ClrDataAddress loaderAllocator, int count, ClrDataAddress* pLoaderHeaps, /*LoaderHeapKind*/ int* pKinds, int* pNeeded)
        => _legacyImpl13 is not null ? _legacyImpl13.GetLoaderAllocatorHeaps(loaderAllocator, count, pLoaderHeaps, pKinds, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetHandleTableMemoryRegions(/*ISOSMemoryEnum*/ void** ppEnum)
        => _legacyImpl13 is not null ? _legacyImpl13.GetHandleTableMemoryRegions(ppEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetGCBookkeepingMemoryRegions(/*ISOSMemoryEnum*/ void** ppEnum)
        => _legacyImpl13 is not null ? _legacyImpl13.GetGCBookkeepingMemoryRegions(ppEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetGCFreeRegions(/*ISOSMemoryEnum*/ void** ppEnum)
        => _legacyImpl13 is not null ? _legacyImpl13.GetGCFreeRegions(ppEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.LockedFlush()
        => _legacyImpl13 is not null ? _legacyImpl13.LockedFlush() : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface13

    #region ISOSDacInterface14
    int ISOSDacInterface14.GetStaticBaseAddress(ClrDataAddress methodTable, ClrDataAddress* nonGCStaticsAddress, ClrDataAddress* GCStaticsAddress)
    {
        int hr = HResults.S_OK;
        if (nonGCStaticsAddress == null && GCStaticsAddress == null)
            hr = HResults.E_POINTER;
        else if (methodTable == 0)
            hr = HResults.E_INVALIDARG;
        else
        {
            try
            {
                Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
                Contracts.TypeHandle typeHandle = rtsContract.GetTypeHandle(methodTable.ToTargetPointer(_target));
                if (GCStaticsAddress != null)
                    *GCStaticsAddress = rtsContract.GetGCStaticsBasePointer(typeHandle).ToClrDataAddress(_target);
                if (nonGCStaticsAddress != null)
                    *nonGCStaticsAddress = rtsContract.GetNonGCStaticsBasePointer(typeHandle).ToClrDataAddress(_target);
            }
            catch (System.Exception ex)
            {
                hr = ex.HResult;
            }
        }
#if DEBUG
        if (_legacyImpl14 is not null)
        {
            ClrDataAddress nonGCStaticsAddressLocal;
            ClrDataAddress GCStaticsAddressLocal;
            int hrLocal = _legacyImpl14.GetStaticBaseAddress(methodTable, &nonGCStaticsAddressLocal, &GCStaticsAddressLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;
        if (nonGCStaticsAddress == null && GCStaticsAddress == null)
            hr = HResults.E_POINTER;
        else if (methodTable == 0 || thread == 0)
            hr = HResults.E_INVALIDARG;
        else
        {
            try
            {
                Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
                TargetPointer methodTablePtr = methodTable.ToTargetPointer(_target);
                TargetPointer threadPtr = thread.ToTargetPointer(_target);
                Contracts.TypeHandle typeHandle = rtsContract.GetTypeHandle(methodTablePtr);
                ushort numThreadStaticFields = rtsContract.GetNumThreadStaticFields(typeHandle);
                if (numThreadStaticFields == 0)
                {
                    if (GCStaticsAddress != null)
                        *GCStaticsAddress = 0;
                    if (nonGCStaticsAddress != null)
                        *nonGCStaticsAddress = 0;
                }
                else
                {
                    if (GCStaticsAddress != null)
                        *GCStaticsAddress = rtsContract.GetGCThreadStaticsBasePointer(typeHandle, threadPtr).ToClrDataAddress(_target);
                    if (nonGCStaticsAddress != null)
                        *nonGCStaticsAddress = rtsContract.GetNonGCThreadStaticsBasePointer(typeHandle, threadPtr).ToClrDataAddress(_target);
                }
            }
            catch (System.Exception ex)
            {
                hr = ex.HResult;
            }
        }
#if DEBUG
        if (_legacyImpl14 is not null)
        {
            ClrDataAddress nonGCStaticsAddressLocal = default;
            ClrDataAddress GCStaticsAddressLocal = default;
            ClrDataAddress* nonGCStaticsAddressOrNull = nonGCStaticsAddress != null ? &nonGCStaticsAddressLocal : null;
            ClrDataAddress* gcStaticsAddressOrNull = GCStaticsAddress != null ? &GCStaticsAddressLocal : null;
            int hrLocal = _legacyImpl14.GetThreadStaticBaseAddress(methodTable, thread, nonGCStaticsAddressOrNull, gcStaticsAddressOrNull);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
        int hr = HResults.S_OK;
        if (methodTable == 0)
            hr = HResults.E_INVALIDARG;
        else if (initializationStatus == null)
            hr = HResults.E_POINTER;

        else
        {
            try
            {
                Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
                Contracts.TypeHandle methodTableHandle = rtsContract.GetTypeHandle(methodTable.ToTargetPointer(_target));
                *initializationStatus = (MethodTableInitializationFlags)0;
                if (rtsContract.IsClassInited(methodTableHandle))
                    *initializationStatus = MethodTableInitializationFlags.MethodTableInitialized;
                if (rtsContract.IsInitError(methodTableHandle))
                    *initializationStatus |= MethodTableInitializationFlags.MethodTableInitializationFailed;
            }
            catch (System.Exception ex)
            {
                hr = ex.HResult;
            }
        }
#if DEBUG
        if (_legacyImpl14 is not null)
        {
            MethodTableInitializationFlags initializationStatusLocal;
            int hrLocal = _legacyImpl14.GetMethodTableInitializationFlags(methodTable, &initializationStatusLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
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
    [GeneratedComClass]
    internal sealed unsafe partial class SOSMethodEnum : ISOSMethodEnum
    {
        private readonly Target _target;
        private readonly IRuntimeTypeSystem _rts;
        private readonly TypeHandle _methodTable;

        private readonly ISOSMethodEnum? _legacyMethodEnum;

        private uint _iteratorIndex;
        private List<SOSMethodData> _methods = [];

        public SOSMethodEnum(Target target, TypeHandle methodTable, ISOSMethodEnum? legacyMethodEnum)
        {
            _target = target;
            _rts = _target.Contracts.RuntimeTypeSystem;
            _methodTable = methodTable;
            _legacyMethodEnum = legacyMethodEnum;

            PopulateMethods();
        }

        private void PopulateMethods()
        {
            ushort numVtableSlots = _rts.GetNumVtableSlots(_methodTable);

            for (ushort i = 0; i < numVtableSlots; i++)
            {
                SOSMethodData methodData = default;
                TargetPointer mdAddr = TargetPointer.Null;
                try
                {
                    mdAddr = _rts.GetMethodDescForSlot(_methodTable, i);
                }
                catch (System.Exception)
                {
                    // Ignore exceptions reading method data
                }

                if (mdAddr != TargetPointer.Null)
                {
                    MethodDescHandle mdh = _rts.GetMethodDescHandle(mdAddr);

                    methodData.MethodDesc = mdAddr.ToClrDataAddress(_target);

                    TargetPointer mtAddr = _rts.GetMethodTable(mdh);
                    methodData.DefiningMethodTable = mtAddr.ToClrDataAddress(_target);

                    TypeHandle typeHandle = _rts.GetTypeHandle(mtAddr);
                    methodData.DefiningModule = _rts.GetModule(typeHandle).ToClrDataAddress(_target);
                    methodData.Token = _rts.GetMethodToken(mdh);
                }

                methodData.Entrypoint = _rts.GetSlot(_methodTable, i).ToClrDataAddress(_target);
                methodData.Slot = i;

                _methods.Add(methodData);
            }

            foreach (TargetPointer mdAddr in _rts.GetIntroducedMethodDescs(_methodTable))
            {
                MethodDescHandle mdh = _rts.GetMethodDescHandle(mdAddr);
                ushort slot = _rts.GetSlotNumber(mdh);
                if (slot >= numVtableSlots)
                {
                    SOSMethodData methodData = default;
                    methodData.MethodDesc = mdAddr.ToClrDataAddress(_target);
                    methodData.Entrypoint = _rts.GetMethodEntryPointIfExists(mdh).ToClrDataAddress(_target);

                    TargetPointer mtAddr = _rts.GetMethodTable(mdh);
                    methodData.DefiningMethodTable = mtAddr.ToClrDataAddress(_target);

                    TypeHandle typeHandle = _rts.GetTypeHandle(mtAddr);
                    methodData.DefiningModule = _rts.GetModule(typeHandle).ToClrDataAddress(_target);
                    methodData.Token = _rts.GetMethodToken(mdh);

                    if (slot == ushort.MaxValue)
                        methodData.Slot = uint.MaxValue;
                    else
                        methodData.Slot = slot;

                    _methods.Add(methodData);
                }
            }
        }

        int ISOSMethodEnum.Next(uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] SOSMethodData[] values, uint* pNeeded)
        {
            int hr = HResults.S_OK;
            try
            {
                if (pNeeded is null)
                    throw new NullReferenceException();
                if (values is null)
                    throw new NullReferenceException();

                uint i = 0;
                while (i < count && _iteratorIndex < _methods.Count)
                {
                    values[i++] = _methods[(int)_iteratorIndex++];
                }

                *pNeeded = i;

                hr = i < count ? HResults.S_FALSE : HResults.S_OK;
            }
            catch (System.Exception ex)
            {
                hr = ex.HResult;
            }

#if DEBUG
            if (_legacyMethodEnum is not null)
            {
                SOSMethodData[] valuesLocal = new SOSMethodData[count];
                uint neededLocal;
                int hrLocal = _legacyMethodEnum.Next(count, valuesLocal, &neededLocal);

                Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
                if (hr == HResults.S_OK || hr == HResults.S_FALSE)
                {
                    Debug.Assert(*pNeeded == neededLocal, $"cDAC: {*pNeeded}, DAC: {neededLocal}");
                    for (uint i = 0; i < *pNeeded; i++)
                    {
                        Debug.Assert(values[i].MethodDesc == valuesLocal[i].MethodDesc, $"cDAC: {values[i].MethodDesc:x}, DAC: {valuesLocal[i].MethodDesc:x}");
                        Debug.Assert(values[i].DefiningMethodTable == valuesLocal[i].DefiningMethodTable, $"cDAC: {values[i].DefiningMethodTable:x}, DAC: {valuesLocal[i].DefiningMethodTable:x}");
                        Debug.Assert(values[i].DefiningModule == valuesLocal[i].DefiningModule, $"cDAC: {values[i].DefiningModule:x}, DAC: {valuesLocal[i].DefiningModule:x}");
                        Debug.Assert(values[i].Token == valuesLocal[i].Token, $"cDAC: {values[i].Token}, DAC: {valuesLocal[i].Token}");
                        Debug.Assert(values[i].Entrypoint == valuesLocal[i].Entrypoint, $"cDAC: {values[i].Entrypoint:x}, DAC: {valuesLocal[i].Entrypoint:x}");
                        Debug.Assert(values[i].Slot == valuesLocal[i].Slot, $"cDAC: {values[i].Slot}, DAC: {valuesLocal[i].Slot}");
                    }
                }
            }
#endif

            return hr;
        }

        int ISOSEnum.Skip(uint count)
        {
            _iteratorIndex += count;
#if DEBUG
            _legacyMethodEnum?.Skip(count);
#endif
            return HResults.S_OK;
        }

        int ISOSEnum.Reset()
        {
            _iteratorIndex = 0;
#if DEBUG
            _legacyMethodEnum?.Reset();
#endif
            return HResults.S_OK;
        }

        int ISOSEnum.GetCount(uint* pCount)
        {
            if (pCount == null)
                return HResults.E_POINTER;
#if DEBUG
            if (_legacyMethodEnum is not null)
            {
                uint countLocal;
                _legacyMethodEnum.GetCount(&countLocal);
                Debug.Assert(countLocal == (uint)_methods.Count);
            }
#endif
            *pCount = (uint)_methods.Count;
            return HResults.S_OK;
        }
    }

    int ISOSDacInterface15.GetMethodTableSlotEnumerator(ClrDataAddress mt, out ISOSMethodEnum? enumerator)
    {
        int hr = HResults.S_OK;
        enumerator = default;

        try
        {
            if (mt == 0)
                throw new ArgumentException();

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            TypeHandle methodTableHandle = rts.GetTypeHandle(mt.ToTargetPointer(_target));

            ISOSMethodEnum? legacyMethodEnum = null;
#if DEBUG
            if (_legacyImpl15 is not null)
            {
                int hrLocal = _legacyImpl15.GetMethodTableSlotEnumerator(mt, out legacyMethodEnum);
                Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            }
#endif

            enumerator = new SOSMethodEnum(_target, methodTableHandle, legacyMethodEnum);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }
    #endregion ISOSDacInterface15

    #region ISOSDacInterface16
    int ISOSDacInterface16.GetGCDynamicAdaptationMode(int* pDynamicAdaptationMode)
    {
        int hr = HResults.S_OK;

        try
        {
            if (pDynamicAdaptationMode == null)
                throw new ArgumentException();

            IGC gc = _target.Contracts.GC;
            if (gc.TryGetGCDynamicAdaptationMode(out int mode))
            {
                *pDynamicAdaptationMode = mode;
            }
            else
            {
                *pDynamicAdaptationMode = -1;
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl16 is not null)
        {
            int dynamicAdaptationModeLocal;
            int hrLocal = _legacyImpl16.GetGCDynamicAdaptationMode(&dynamicAdaptationModeLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK || hr == HResults.S_FALSE)
            {
                Debug.Assert(pDynamicAdaptationMode == null || *pDynamicAdaptationMode == dynamicAdaptationModeLocal);
            }
        }
#endif
        return hr;
    }
    #endregion ISOSDacInterface16
}
