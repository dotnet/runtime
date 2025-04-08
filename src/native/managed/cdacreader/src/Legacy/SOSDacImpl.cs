// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

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
      ISOSDacInterface11, ISOSDacInterface12, ISOSDacInterface13, ISOSDacInterface14, ISOSDacInterface15
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
    private readonly IXCLRDataProcess? _legacyProcess;
    private readonly IXCLRDataProcess2? _legacyProcess2;
    private readonly ICLRDataEnumMemoryRegions? _legacyEnumMemory;

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

            _legacyProcess = legacyObj as IXCLRDataProcess;
            _legacyProcess2 = legacyObj as IXCLRDataProcess2;

            _legacyEnumMemory = legacyObj as ICLRDataEnumMemoryRegions;
        }
    }

    #region ISOSDacInterface
    int ISOSDacInterface.GetAppDomainConfigFile(ulong appDomain, int count, char* configFile, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAppDomainConfigFile(appDomain, count, configFile, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAppDomainData(ulong addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetAppDomainData(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAppDomainList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAppDomainList(count, values, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAppDomainName(ulong addr, uint count, char* name, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAppDomainName(addr, count, name, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAppDomainStoreData(void* data)
        => _legacyImpl is not null ? _legacyImpl.GetAppDomainStoreData(data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetApplicationBase(ulong appDomain, int count, char* appBase, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetApplicationBase(appDomain, count, appBase, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAssemblyData(ulong baseDomainPtr, ulong assembly, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetAssemblyData(baseDomainPtr, assembly, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAssemblyList(ulong appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, int* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAssemblyList(appDomain, count, values, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAssemblyLocation(ulong assembly, int count, char* location, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAssemblyLocation(assembly, count, location, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAssemblyModuleList(ulong assembly, uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] modules, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAssemblyModuleList(assembly, count, modules, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetAssemblyName(ulong assembly, uint count, char* name, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetAssemblyName(assembly, count, name, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetCCWData(ulong ccw, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetCCWData(ccw, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetCCWInterfaces(ulong ccw, uint count, void* interfaces, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetCCWInterfaces(ccw, count, interfaces, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetClrWatsonBuckets(ulong thread, void* pGenericModeBlock)
        => _legacyImpl is not null ? _legacyImpl.GetClrWatsonBuckets(thread, pGenericModeBlock) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetCodeHeaderData(ulong ip, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetCodeHeaderData(ip, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetCodeHeapList(ulong jitManager, uint count, void* codeHeaps, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetCodeHeapList(jitManager, count, codeHeaps, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetDacModuleHandle(void* phModule)
        => _legacyImpl is not null ? _legacyImpl.GetDacModuleHandle(phModule) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetDomainFromContext(ulong context, ulong* domain)
        => _legacyImpl is not null ? _legacyImpl.GetDomainFromContext(context, domain) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetDomainLocalModuleData(ulong addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetDomainLocalModuleData(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetDomainLocalModuleDataFromAppDomain(ulong appDomainAddr, int moduleID, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetDomainLocalModuleDataFromAppDomain(appDomainAddr, moduleID, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetDomainLocalModuleDataFromModule(ulong moduleAddr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetDomainLocalModuleDataFromModule(moduleAddr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFailedAssemblyData(ulong assembly, uint* pContext, int* pResult)
        => _legacyImpl is not null ? _legacyImpl.GetFailedAssemblyData(assembly, pContext, pResult) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFailedAssemblyDisplayName(ulong assembly, uint count, char* name, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetFailedAssemblyDisplayName(assembly, count, name, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFailedAssemblyList(ulong appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetFailedAssemblyList(appDomain, count, values, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFailedAssemblyLocation(ulong assesmbly, uint count, char* location, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetFailedAssemblyLocation(assesmbly, count, location, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFieldDescData(ulong fieldDesc, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetFieldDescData(fieldDesc, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetFrameName(ulong vtable, uint count, char* frameName, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetFrameName(vtable, count, frameName, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetGCHeapData(void* data)
        => _legacyImpl is not null ? _legacyImpl.GetGCHeapData(data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetGCHeapDetails(ulong heap, void* details)
        => _legacyImpl is not null ? _legacyImpl.GetGCHeapDetails(heap, details) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetGCHeapList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] heaps, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetGCHeapList(count, heaps, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetGCHeapStaticData(void* data)
        => _legacyImpl is not null ? _legacyImpl.GetGCHeapStaticData(data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHandleEnum(void** ppHandleEnum)
        => _legacyImpl is not null ? _legacyImpl.GetHandleEnum(ppHandleEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHandleEnumForGC(uint gen, void** ppHandleEnum)
        => _legacyImpl is not null ? _legacyImpl.GetHandleEnumForGC(gen, ppHandleEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHandleEnumForTypes([In, MarshalUsing(CountElementName = "count")] uint[] types, uint count, void** ppHandleEnum)
        => _legacyImpl is not null ? _legacyImpl.GetHandleEnumForTypes(types, count, ppHandleEnum) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHeapAllocData(uint count, void* data, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetHeapAllocData(count, data, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHeapAnalyzeData(ulong addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetHeapAnalyzeData(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHeapAnalyzeStaticData(void* data)
        => _legacyImpl is not null ? _legacyImpl.GetHeapAnalyzeStaticData(data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHeapSegmentData(ulong seg, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetHeapSegmentData(seg, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetHillClimbingLogEntry(ulong addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetHillClimbingLogEntry(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetILForModule(ulong moduleAddr, int rva, ulong* il)
        => _legacyImpl is not null ? _legacyImpl.GetILForModule(moduleAddr, rva, il) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetJitHelperFunctionName(ulong ip, uint count, byte* name, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetJitHelperFunctionName(ip, count, name, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetJitManagerList(uint count, void* managers, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetJitManagerList(count, managers, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetJumpThunkTarget(void* ctx, ulong* targetIP, ulong* targetMD)
        => _legacyImpl is not null ? _legacyImpl.GetJumpThunkTarget(ctx, targetIP, targetMD) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetMethodDescData(ulong methodDesc, ulong ip, DacpMethodDescData* data, uint cRevertedRejitVersions, DacpReJitData* rgRevertedRejitData, uint* pcNeededRevertedRejitData)
    {
        if (methodDesc == 0)
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
                requestedNativeCodeVersion = nativeCodeContract.GetNativeCodeVersionForIP(new TargetCodePointer(ip));
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
                data->NativeCodeAddr = nativeCodeAddr;
            }
            else
            {
                data->bHasNativeCode = 0;
                data->NativeCodeAddr = 0xffffffff_fffffffful;
            }
            if (rtsContract.HasNativeCodeSlot(methodDescHandle))
            {
                data->AddressOfNativeCodeSlot = rtsContract.GetAddressOfNativeCodeSlot(methodDescHandle);
            }
            else
            {
                data->AddressOfNativeCodeSlot = 0;
            }
            data->MDToken = rtsContract.GetMethodToken(methodDescHandle);
            data->MethodDescPtr = methodDesc;
            TargetPointer methodTableAddr = rtsContract.GetMethodTable(methodDescHandle);
            data->MethodTablePtr = methodTableAddr;
            TypeHandle typeHandle = rtsContract.GetTypeHandle(methodTableAddr);
            data->ModulePtr = rtsContract.GetModule(typeHandle);

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
                data->GCStressCodeCopy = nativeCodeContract.GetGCStressCodeCopy(requestedNativeCodeVersion);
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
                hrLocal = _legacyImpl.GetMethodDescData(methodDesc, ip, &dataLocal, cRevertedRejitVersions, rgRevertedRejitDataLocalPtr, pcNeededRevertedRejitDataLocal);
            }
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
            {
                Debug.Assert(data->bHasNativeCode == dataLocal.bHasNativeCode);
                Debug.Assert(data->bIsDynamic == dataLocal.bIsDynamic);
                Debug.Assert(data->wSlotNumber == dataLocal.wSlotNumber);
                Debug.Assert(data->NativeCodeAddr == dataLocal.NativeCodeAddr);
                Debug.Assert(data->AddressOfNativeCodeSlot == dataLocal.AddressOfNativeCodeSlot);
                Debug.Assert(data->MethodDescPtr == dataLocal.MethodDescPtr);
                Debug.Assert(data->MethodTablePtr == dataLocal.MethodTablePtr);
                Debug.Assert(data->ModulePtr == dataLocal.ModulePtr);
                Debug.Assert(data->MDToken == dataLocal.MDToken);
                Debug.Assert(data->GCInfo == dataLocal.GCInfo);
                Debug.Assert(data->GCStressCodeCopy == dataLocal.GCStressCodeCopy);
                // managedDynamicMethodObject is not currently populated by the cDAC API and may differ from legacyImpl.
                Debug.Assert(data->managedDynamicMethodObject == 0);
                Debug.Assert(data->requestedIP == dataLocal.requestedIP);
                Debug.Assert(data->cJittedRejitVersions == dataLocal.cJittedRejitVersions);

                // rejitDataCurrent
                Debug.Assert(data->rejitDataCurrent.rejitID == dataLocal.rejitDataCurrent.rejitID);
                Debug.Assert(data->rejitDataCurrent.NativeCodeAddr == dataLocal.rejitDataCurrent.NativeCodeAddr);
                Debug.Assert(data->rejitDataCurrent.flags == dataLocal.rejitDataCurrent.flags);

                // rejitDataRequested
                Debug.Assert(data->rejitDataRequested.rejitID == dataLocal.rejitDataRequested.rejitID);
                Debug.Assert(data->rejitDataRequested.NativeCodeAddr == dataLocal.rejitDataRequested.NativeCodeAddr);
                Debug.Assert(data->rejitDataRequested.flags == dataLocal.rejitDataRequested.flags);

                // rgRevertedRejitData
                if (rgRevertedRejitData != null && rgRevertedRejitDataLocal != null)
                {
                    Debug.Assert(cNeededRevertedRejitDataLocal == *pcNeededRevertedRejitData);
                    for (ulong i = 0; i < cNeededRevertedRejitDataLocal; i++)
                    {
                        Debug.Assert(rgRevertedRejitData[i].rejitID == rgRevertedRejitDataLocal[i].rejitID);
                        Debug.Assert(rgRevertedRejitData[i].NativeCodeAddr == rgRevertedRejitDataLocal[i].NativeCodeAddr);
                        Debug.Assert(rgRevertedRejitData[i].flags == rgRevertedRejitDataLocal[i].flags);
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
        pReJitData->NativeCodeAddr = cv.GetNativeCode(nativeCodeVersion);

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

    int ISOSDacInterface.GetMethodDescFromToken(ulong moduleAddr, uint token, ulong* methodDesc)
        => _legacyImpl is not null ? _legacyImpl.GetMethodDescFromToken(moduleAddr, token, methodDesc) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetMethodDescName(ulong methodDesc, uint count, char* name, uint* pNeeded)
    {
        if (methodDesc == 0)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        if (pNeeded != null)
            *pNeeded = 0;
        try
        {
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
                        Contracts.ModuleHandle module = _target.Contracts.Loader.GetModuleHandle(modulePtr);
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

            if (hr ==  HResults.S_OK)
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
                hrLocal = _legacyImpl.GetMethodDescName(methodDesc, count, ptr, &neededLocal);
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

    int ISOSDacInterface.GetMethodDescPtrFromFrame(ulong frameAddr, ulong* ppMD)
        => _legacyImpl is not null ? _legacyImpl.GetMethodDescPtrFromFrame(frameAddr, ppMD) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetMethodDescPtrFromIP(ulong ip, ulong* ppMD)
    {
        if (ip == 0 || ppMD == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.E_NOTIMPL;

        try
        {
            IExecutionManager executionManager = _target.Contracts.ExecutionManager;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            CodeBlockHandle? handle = executionManager.GetCodeBlockHandle(new TargetCodePointer(ip));
            if (handle is CodeBlockHandle codeHandle)
            {
                TargetPointer methodDescAddr = executionManager.GetMethodDesc(codeHandle);

                try
                {
                    // Runs validation of MethodDesc
                    // if validation fails, should return E_INVALIDARG
                    rts.GetMethodDescHandle(methodDescAddr);

                    *ppMD = methodDescAddr.Value;
                    hr = HResults.S_OK;
                }
                catch (System.Exception)
                {
                    hr = HResults.E_INVALIDARG;
                }
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
            ulong ppMDLocal;
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

    int ISOSDacInterface.GetMethodDescTransparencyData(ulong methodDesc, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetMethodDescTransparencyData(methodDesc, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetMethodTableData(ulong mt, DacpMethodTableData* data)
    {
        if (mt == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem contract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTable = contract.GetTypeHandle(mt);

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
                result.module = contract.GetModule(methodTable);
                // Note: really the canonical method table, not the EEClass, which we don't expose
                result.klass = contract.GetCanonicalMethodTable(methodTable);
                result.parentMethodTable = contract.GetParentMethodTable(methodTable);
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
    int ISOSDacInterface.GetMethodTableFieldData(ulong mt, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetMethodTableFieldData(mt, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetMethodTableForEEClass(ulong eeClassReallyCanonMT, ulong* value)
    {
        if (eeClassReallyCanonMT == 0 || value == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem contract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTableHandle = contract.GetTypeHandle(eeClassReallyCanonMT);
            *value = methodTableHandle.Address;
        }
        catch (global::System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ulong valueLocal;
            int hrLocal = _legacyImpl.GetMethodTableForEEClass(eeClassReallyCanonMT, &valueLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
                Debug.Assert(*value == valueLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetMethodTableName(ulong mt, uint count, char* mtName, uint* pNeeded)
    {
        if (mt == 0)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IRuntimeTypeSystem typeSystemContract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTableHandle = typeSystemContract.GetTypeHandle(mt);
            if (typeSystemContract.IsFreeObjectMethodTable(methodTableHandle))
            {
                OutputBufferHelpers.CopyStringToBuffer(mtName, count, pNeeded, "Free");
                return HResults.S_OK;
            }

            // TODO(cdac) - The original code handles the case of the module being in the process of being unloaded. This is not yet handled

            System.Text.StringBuilder methodTableName = new();
            try
            {
                TargetPointer modulePointer = typeSystemContract.GetModule(methodTableHandle);
                TypeNameBuilder.AppendType(_target, methodTableName, methodTableHandle, TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
            }
            catch
            {
                try
                {
                    string? fallbackName = _target.Contracts.DacStreams.StringFromEEAddress(mt);
                    if (fallbackName != null)
                    {
                        methodTableName.Clear();
                        methodTableName.Append(fallbackName);
                    }
                }
                catch
                { }
            }
            OutputBufferHelpers.CopyStringToBuffer(mtName, count, pNeeded, methodTableName.ToString());
        }
        catch (global::System.Exception ex)
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

    int ISOSDacInterface.GetMethodTableSlot(ulong mt, uint slot, ulong* value)
        => _legacyImpl is not null ? _legacyImpl.GetMethodTableSlot(mt, slot, value) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetMethodTableTransparencyData(ulong mt, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetMethodTableTransparencyData(mt, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetModule(ulong addr, out IXCLRDataModule? mod)
    {
        mod = default;

        IXCLRDataModule? legacyModule = null;
        if (_legacyImpl is not null)
        {
            int hr = _legacyImpl.GetModule(addr, out legacyModule);
            if (hr < 0)
                return hr;
        }

        mod = new ClrDataModule(addr, _target, legacyModule);
        return HResults.S_OK;
    }

    int ISOSDacInterface.GetModuleData(ulong moduleAddr, DacpModuleData* data)
    {
        if (moduleAddr == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandle(moduleAddr);

            data->Address = moduleAddr;
            data->PEAssembly = moduleAddr; // Module address in .NET 9+ - correspondingly, SOS-DAC APIs for PE assemblies expect a module address
            data->Assembly = contract.GetAssembly(handle);

            Contracts.ModuleFlags flags = contract.GetFlags(handle);
            bool isReflectionEmit = flags.HasFlag(Contracts.ModuleFlags.ReflectionEmit);
            data->isReflection = (uint)(isReflectionEmit ? 1 : 0);
            data->isPEFile = (uint)(isReflectionEmit ? 0 : 1);      // ReflectionEmit module means it is not a PE file
            data->dwTransientFlags = (uint)flags;

            data->ilBase = contract.GetILBase(handle);
            TargetSpan readOnlyMetadata = _target.Contracts.EcmaMetadata.GetReadOnlyMetadataAddress(handle);
            data->metadataStart = readOnlyMetadata.Address;
            data->metadataSize = readOnlyMetadata.Size;

            data->LoaderAllocator = contract.GetLoaderAllocator(handle);
            data->ThunkHeap = contract.GetThunkHeap(handle);

            Target.TypeInfo lookupMapTypeInfo = _target.GetTypeInfo(DataType.ModuleLookupMap);
            ulong tableDataOffset = (ulong)lookupMapTypeInfo.Fields[Constants.FieldNames.ModuleLookupMap.TableData].Offset;

            Contracts.ModuleLookupTables tables = contract.GetLookupTables(handle);
            data->FieldDefToDescMap = _target.ReadPointer(tables.FieldDefToDesc + tableDataOffset);
            data->ManifestModuleReferencesMap = _target.ReadPointer(tables.ManifestModuleReferences + tableDataOffset);
            data->MemberRefToDescMap = _target.ReadPointer(tables.MemberRefToDesc + tableDataOffset);
            data->MethodDefToDescMap = _target.ReadPointer(tables.MethodDefToDesc + tableDataOffset);
            data->TypeDefToMethodTableMap = _target.ReadPointer(tables.TypeDefToMethodTable + tableDataOffset);
            data->TypeRefToMethodTableMap = _target.ReadPointer(tables.TypeRefToMethodTable + tableDataOffset);

            // Always 0 - .NET no longer has these concepts
            data->dwModuleID = 0;
            data->dwBaseClassIndex = 0;
            data->dwModuleIndex = 0;
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

    int ISOSDacInterface.GetNestedExceptionData(ulong exception, ulong* exceptionObject, ulong* nextNestedException)
    {
        if (exception == 0 || exceptionObject == null || nextNestedException == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IException contract = _target.Contracts.Exception;
            TargetPointer exceptionObjectLocal = contract.GetNestedExceptionInfo(exception, out TargetPointer nextNestedExceptionLocal);
            *exceptionObject = exceptionObjectLocal;
            *nextNestedException = nextNestedExceptionLocal;
        }
        catch (global::System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ulong exceptionObjectLocal;
            ulong nextNestedExceptionLocal;
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

    int ISOSDacInterface.GetObjectClassName(ulong obj, uint count, char* className, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetObjectClassName(obj, count, className, pNeeded) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetObjectData(ulong objAddr, DacpObjectData* data)
    {
        if (objAddr == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IObject objectContract = _target.Contracts.Object;
            Contracts.IRuntimeTypeSystem runtimeTypeSystemContract = _target.Contracts.RuntimeTypeSystem;

            TargetPointer mt = objectContract.GetMethodTableAddress(objAddr);
            TypeHandle handle = runtimeTypeSystemContract.GetTypeHandle(mt);

            data->MethodTable = mt;
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
                data->Size += (uint)objectContract.GetStringValue(objAddr).Length * data->dwComponentSize;
            }
            else if (mt == _objectMethodTable.Value)
            {
                data->ObjectType = DacpObjectType.OBJ_OBJECT;
            }
            else if (runtimeTypeSystemContract.IsArray(handle, out uint rank))
            {
                data->ObjectType = DacpObjectType.OBJ_ARRAY;
                data->dwRank = rank;

                TargetPointer arrayData = objectContract.GetArrayData(objAddr, out uint numComponents, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                data->ArrayDataPtr = arrayData;
                data->dwNumComponents = numComponents;
                data->ArrayBoundsPtr = boundsStart;
                data->ArrayLowerBoundsPtr = lowerBounds;

                // Update the size to include the array components
                data->Size += numComponents * data->dwComponentSize;

                // Get the type of the array elements
                TypeHandle element = runtimeTypeSystemContract.GetTypeParam(handle);
                data->ElementTypeHandle = element.Address;
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
                && objectContract.GetBuiltInComData(objAddr, out TargetPointer rcw, out TargetPointer ccw))
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

    int ISOSDacInterface.GetObjectStringData(ulong obj, uint count, char* stringData, uint* pNeeded)
    {
        if (obj == 0 || (stringData == null && pNeeded == null) || (stringData is not null && count <= 0))
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IObject contract = _target.Contracts.Object;
            string str = contract.GetStringValue(obj);
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

    int ISOSDacInterface.GetOOMData(ulong oomAddr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetOOMData(oomAddr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetOOMStaticData(void* data)
        => _legacyImpl is not null ? _legacyImpl.GetOOMStaticData(data) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetPEFileBase(ulong addr, ulong* peBase)
    {
        if (addr == 0 || peBase == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandle(addr);
            Contracts.ModuleFlags flags = contract.GetFlags(handle);

            if (!flags.HasFlag(Contracts.ModuleFlags.ReflectionEmit))
            {
                *peBase = contract.GetILBase(handle);
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
            ulong peBaseLocal;
            int hrLocal = _legacyImpl.GetPEFileBase(addr, &peBaseLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
                Debug.Assert(*peBase == peBaseLocal);
        }
#endif
        return hr;
    }

    int ISOSDacInterface.GetPEFileName(ulong addr, uint count, char* fileName, uint* pNeeded)
    {
        if (addr == 0 || (fileName == null && pNeeded == null) || (fileName is not null && count <= 0))
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandle(addr);
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

    int ISOSDacInterface.GetPrivateBinPaths(ulong appDomain, int count, char* paths, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetPrivateBinPaths(appDomain, count, paths, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetRCWData(ulong addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetRCWData(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetRCWInterfaces(ulong rcw, uint count, void* interfaces, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetRCWInterfaces(rcw, count, interfaces, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetRegisterName(int regName, uint count, char* buffer, uint* pNeeded)
        => _legacyImpl is not null ? _legacyImpl.GetRegisterName(regName, count, buffer, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetStackLimits(ulong threadPtr, ulong* lower, ulong* upper, ulong* fp)
        => _legacyImpl is not null ? _legacyImpl.GetStackLimits(threadPtr, lower, upper, fp) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetStackReferences(int osThreadID, void** ppEnum)
        => _legacyImpl is not null ? _legacyImpl.GetStackReferences(osThreadID, ppEnum) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetStressLogAddress(ulong* stressLog)
    {
        ulong stressLogAddress = _target.ReadGlobalPointer(Constants.Globals.StressLog);

#if DEBUG
        if (_legacyImpl is not null)
        {
            ulong legacyStressLog;
            Debug.Assert(HResults.S_OK == _legacyImpl.GetStressLogAddress(&legacyStressLog));
            Debug.Assert(legacyStressLog == stressLogAddress);
        }
#endif
        *stressLog = stressLogAddress;
        return HResults.S_OK;
    }

    int ISOSDacInterface.GetSyncBlockCleanupData(ulong addr, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetSyncBlockCleanupData(addr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetSyncBlockData(uint number, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetSyncBlockData(number, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetThreadAllocData(ulong thread, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetThreadAllocData(thread, data) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetThreadData(ulong thread, DacpThreadData* data)
    {
        if (thread == 0 || data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            Contracts.IThread contract = _target.Contracts.Thread;
            Contracts.ThreadData threadData = contract.GetThreadData(thread);
            data->corThreadId = (int)threadData.Id;
            data->osThreadId = (int)threadData.OSId.Value;
            data->state = (int)threadData.State;
            data->preemptiveGCDisabled = (uint)(threadData.PreemptiveGCDisabled ? 1 : 0);
            data->allocContextPtr = threadData.AllocContextPointer;
            data->allocContextLimit = threadData.AllocContextLimit;
            data->fiberData = 0;    // Always set to 0 - fibers are no longer supported

            TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            TargetPointer appDomain = _target.ReadPointer(appDomainPointer);
            data->context = appDomain;
            data->domain = appDomain;

            data->lockCount = -1;   // Always set to -1 - lock count was .NET Framework and no longer needed
            data->pFrame = threadData.Frame;
            data->firstNestedException = threadData.FirstNestedException;
            data->teb = threadData.TEB;
            data->lastThrownObjectHandle = threadData.LastThrownObjectHandle;
            data->nextThread = threadData.NextThread;
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
                Debug.Assert(data->corThreadId == dataLocal.corThreadId);
                Debug.Assert(data->osThreadId == dataLocal.osThreadId);
                Debug.Assert(data->state == dataLocal.state);
                Debug.Assert(data->preemptiveGCDisabled == dataLocal.preemptiveGCDisabled);
                Debug.Assert(data->allocContextPtr == dataLocal.allocContextPtr);
                Debug.Assert(data->allocContextLimit == dataLocal.allocContextLimit);
                Debug.Assert(data->fiberData == dataLocal.fiberData);
                Debug.Assert(data->context == dataLocal.context);
                Debug.Assert(data->domain == dataLocal.domain);
                Debug.Assert(data->lockCount == dataLocal.lockCount);
                Debug.Assert(data->pFrame == dataLocal.pFrame);
                Debug.Assert(data->firstNestedException == dataLocal.firstNestedException);
                Debug.Assert(data->teb == dataLocal.teb);
                Debug.Assert(data->lastThrownObjectHandle == dataLocal.lastThrownObjectHandle);
                Debug.Assert(data->nextThread == dataLocal.nextThread);
            }
        }
#endif
        return hr;
    }
    int ISOSDacInterface.GetThreadFromThinlockID(uint thinLockId, ulong* pThread)
        => _legacyImpl is not null ? _legacyImpl.GetThreadFromThinlockID(thinLockId, pThread) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetThreadLocalModuleData(ulong thread, uint index, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetThreadLocalModuleData(thread, index, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.GetThreadpoolData(void* data)
        => _legacyImpl is not null ? _legacyImpl.GetThreadpoolData(data) : HResults.E_NOTIMPL;

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
            data->firstThread = threadStoreData.FirstThread;
            data->finalizerThread = threadStoreData.FinalizerThread;
            data->gcThread = threadStoreData.GCThread;

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
        => _legacyImpl is not null ? _legacyImpl.GetTLSIndex(pIndex) : HResults.E_NOTIMPL;

    int ISOSDacInterface.GetUsefulGlobals(DacpUsefulGlobalsData* data)
    {
        if (data == null)
            return HResults.E_INVALIDARG;

        int hr = HResults.S_OK;
        try
        {
            data->ArrayMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ObjectArrayMethodTable));
            data->StringMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.StringMethodTable));
            data->ObjectMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable));
            data->ExceptionMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ExceptionMethodTable));
            data->FreeMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            DacpUsefulGlobalsData dataLocal;
            int hrLocal = _legacyImpl.GetUsefulGlobals(&dataLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (hr == HResults.S_OK)
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

    int ISOSDacInterface.GetWorkRequestData(ulong addrWorkRequest, void* data)
        => _legacyImpl is not null ? _legacyImpl.GetWorkRequestData(addrWorkRequest, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface.TraverseEHInfo(ulong ip, void* pCallback, void* token)
        => _legacyImpl is not null ? _legacyImpl.TraverseEHInfo(ip, pCallback, token) : HResults.E_NOTIMPL;
    int ISOSDacInterface.TraverseLoaderHeap(ulong loaderHeapAddr, void* pCallback)
        => _legacyImpl is not null ? _legacyImpl.TraverseLoaderHeap(loaderHeapAddr, pCallback) : HResults.E_NOTIMPL;
    int ISOSDacInterface.TraverseModuleMap(int mmt, ulong moduleAddr, void* pCallback, void* token)
        => _legacyImpl is not null ? _legacyImpl.TraverseModuleMap(mmt, moduleAddr, pCallback, token) : HResults.E_NOTIMPL;
    int ISOSDacInterface.TraverseRCWCleanupList(ulong cleanupListPtr, void* pCallback, void* token)
        => _legacyImpl is not null ? _legacyImpl.TraverseRCWCleanupList(cleanupListPtr, pCallback, token) : HResults.E_NOTIMPL;
    int ISOSDacInterface.TraverseVirtCallStubHeap(ulong pAppDomain, int heaptype, void* pCallback)
        => _legacyImpl is not null ? _legacyImpl.TraverseVirtCallStubHeap(pAppDomain, heaptype, pCallback) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface

    #region ISOSDacInterface2
    int ISOSDacInterface2.GetObjectExceptionData(ulong objectAddress, DacpExceptionObjectData* data)
    {
        try
        {
            Contracts.IException contract = _target.Contracts.Exception;
            Contracts.ExceptionData exceptionData = contract.GetExceptionData(objectAddress);
            data->Message = exceptionData.Message;
            data->InnerException = exceptionData.InnerException;
            data->StackTrace = exceptionData.StackTrace;
            data->WatsonBuckets = exceptionData.WatsonBuckets;
            data->StackTraceString = exceptionData.StackTraceString;
            data->RemoteStackTraceString = exceptionData.RemoteStackTraceString;
            data->HResult = exceptionData.HResult;
            data->XCode = exceptionData.XCode;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    int ISOSDacInterface2.IsRCWDCOMProxy(ulong rcwAddress, int* inDCOMProxy)
        => _legacyImpl2 is not null ? _legacyImpl2.IsRCWDCOMProxy(rcwAddress, inDCOMProxy) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface2

    #region ISOSDacInterface3
    int ISOSDacInterface3.GetGCInterestingInfoData(ulong interestingInfoAddr, /*struct DacpGCInterestingInfoData*/ void* data)
        => _legacyImpl3 is not null ? _legacyImpl3.GetGCInterestingInfoData(interestingInfoAddr, data) : HResults.E_NOTIMPL;
    int ISOSDacInterface3.GetGCInterestingInfoStaticData(/*struct DacpGCInterestingInfoData*/ void* data)
        => _legacyImpl3 is not null ? _legacyImpl3.GetGCInterestingInfoStaticData(data) : HResults.E_NOTIMPL;
    int ISOSDacInterface3.GetGCGlobalMechanisms(nuint* globalMechanisms)
        => _legacyImpl3 is not null ? _legacyImpl3.GetGCGlobalMechanisms(globalMechanisms) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface3

    #region ISOSDacInterface4
    int ISOSDacInterface4.GetClrNotification(ulong[] arguments, int count, int* pNeeded)
        => _legacyImpl4 is not null ? _legacyImpl4.GetClrNotification(arguments, count, pNeeded) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface4

    #region ISOSDacInterface5
    int ISOSDacInterface5.GetTieredVersions(ulong methodDesc, int rejitId, /*struct DacpTieredVersionData*/ void* nativeCodeAddrs, int cNativeCodeAddrs, int* pcNativeCodeAddrs)
        => _legacyImpl5 is not null ? _legacyImpl5.GetTieredVersions(methodDesc, rejitId, nativeCodeAddrs, cNativeCodeAddrs, pcNativeCodeAddrs) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface5

    #region ISOSDacInterface6
    int ISOSDacInterface6.GetMethodTableCollectibleData(ulong mt, /*struct DacpMethodTableCollectibleData*/ void* data)
        => _legacyImpl6 is not null ? _legacyImpl6.GetMethodTableCollectibleData(mt, data) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface6

    #region ISOSDacInterface7
    int ISOSDacInterface7.GetPendingReJITID(ulong methodDesc, int* pRejitId)
        => _legacyImpl7 is not null ? _legacyImpl7.GetPendingReJITID(methodDesc, pRejitId) : HResults.E_NOTIMPL;
    int ISOSDacInterface7.GetReJITInformation(ulong methodDesc, int rejitId, /*struct DacpReJitData2*/ void* pRejitData)
        => _legacyImpl7 is not null ? _legacyImpl7.GetReJITInformation(methodDesc, rejitId, pRejitData) : HResults.E_NOTIMPL;
    int ISOSDacInterface7.GetProfilerModifiedILInformation(ulong methodDesc, /*struct DacpProfilerILData*/ void* pILData)
        => _legacyImpl7 is not null ? _legacyImpl7.GetProfilerModifiedILInformation(methodDesc, pILData) : HResults.E_NOTIMPL;
    int ISOSDacInterface7.GetMethodsWithProfilerModifiedIL(ulong mod, ulong* methodDescs, int cMethodDescs, int* pcMethodDescs)
        => _legacyImpl7 is not null ? _legacyImpl7.GetMethodsWithProfilerModifiedIL(mod, methodDescs, cMethodDescs, pcMethodDescs) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface7

    #region ISOSDacInterface8
    int ISOSDacInterface8.GetNumberGenerations(uint* pGenerations)
        => _legacyImpl8 is not null ? _legacyImpl8.GetNumberGenerations(pGenerations) : HResults.E_NOTIMPL;

    // WKS
    int ISOSDacInterface8.GetGenerationTable(uint cGenerations, /*struct DacpGenerationData*/ void* pGenerationData, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetGenerationTable(cGenerations, pGenerationData, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface8.GetFinalizationFillPointers(uint cFillPointers, ulong* pFinalizationFillPointers, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetFinalizationFillPointers(cFillPointers, pFinalizationFillPointers, pNeeded) : HResults.E_NOTIMPL;

    // SVR
    int ISOSDacInterface8.GetGenerationTableSvr(ulong heapAddr, uint cGenerations, /*struct DacpGenerationData*/ void* pGenerationData, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetGenerationTableSvr(heapAddr, cGenerations, pGenerationData, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface8.GetFinalizationFillPointersSvr(ulong heapAddr, uint cFillPointers, ulong* pFinalizationFillPointers, uint* pNeeded)
        => _legacyImpl8 is not null ? _legacyImpl8.GetFinalizationFillPointersSvr(heapAddr, cFillPointers, pFinalizationFillPointers, pNeeded) : HResults.E_NOTIMPL;

    int ISOSDacInterface8.GetAssemblyLoadContext(ulong methodTable, ulong* assemblyLoadContext)
        => _legacyImpl8 is not null ? _legacyImpl8.GetAssemblyLoadContext(methodTable, assemblyLoadContext) : HResults.E_NOTIMPL;
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
    int ISOSDacInterface10.GetObjectComWrappersData(ulong objAddr, ulong* rcw, uint count, ulong* mowList, uint* pNeeded)
        => _legacyImpl10 is not null ? _legacyImpl10.GetObjectComWrappersData(objAddr, rcw, count, mowList, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.IsComWrappersCCW(ulong ccw, Interop.BOOL* isComWrappersCCW)
        => _legacyImpl10 is not null ? _legacyImpl10.IsComWrappersCCW(ccw, isComWrappersCCW) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.GetComWrappersCCWData(ulong ccw, ulong* managedObject, int* refCount)
        => _legacyImpl10 is not null ? _legacyImpl10.GetComWrappersCCWData(ccw, managedObject, refCount) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.IsComWrappersRCW(ulong rcw, Interop.BOOL* isComWrappersRCW)
        => _legacyImpl10 is not null ? _legacyImpl10.IsComWrappersRCW(rcw, isComWrappersRCW) : HResults.E_NOTIMPL;
    int ISOSDacInterface10.GetComWrappersRCWData(ulong rcw, ulong* identity)
        => _legacyImpl10 is not null ? _legacyImpl10.GetComWrappersRCWData(rcw, identity) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface10

    #region ISOSDacInterface11
    int ISOSDacInterface11.IsTrackedType(ulong objAddr, Interop.BOOL* isTrackedType, Interop.BOOL* hasTaggedMemory)
        => _legacyImpl11 is not null ? _legacyImpl11.IsTrackedType(objAddr, isTrackedType, hasTaggedMemory) : HResults.E_NOTIMPL;
    int ISOSDacInterface11.GetTaggedMemory(ulong objAddr, ulong* taggedMemory, nuint* taggedMemorySizeInBytes)
        => _legacyImpl11 is not null ? _legacyImpl11.GetTaggedMemory(objAddr, taggedMemory, taggedMemorySizeInBytes) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface11

    #region ISOSDacInterface12
    int ISOSDacInterface12.GetGlobalAllocationContext(ulong* allocPtr, ulong* allocLimit)
        => _legacyImpl12 is not null ? _legacyImpl12.GetGlobalAllocationContext(allocPtr, allocLimit) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface12

    #region ISOSDacInterface13
    int ISOSDacInterface13.TraverseLoaderHeap(ulong loaderHeapAddr, /*LoaderHeapKind*/ int kind, /*VISITHEAP*/ delegate* unmanaged<ulong, nuint, Interop.BOOL> pCallback)
        => _legacyImpl13 is not null ? _legacyImpl13.TraverseLoaderHeap(loaderHeapAddr, kind, pCallback) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetDomainLoaderAllocator(ulong domainAddress, ulong* pLoaderAllocator)
        => _legacyImpl13 is not null ? _legacyImpl13.GetDomainLoaderAllocator(domainAddress, pLoaderAllocator) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetLoaderAllocatorHeapNames(int count, char** ppNames, int* pNeeded)
        => _legacyImpl13 is not null ? _legacyImpl13.GetLoaderAllocatorHeapNames(count, ppNames, pNeeded) : HResults.E_NOTIMPL;
    int ISOSDacInterface13.GetLoaderAllocatorHeaps(ulong loaderAllocator, int count, ulong* pLoaderHeaps, /*LoaderHeapKind*/ int* pKinds, int* pNeeded)
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
    int ISOSDacInterface14.GetStaticBaseAddress(ulong methodTable, ulong* nonGCStaticsAddress, ulong* GCStaticsAddress)
        => _legacyImpl14 is not null ? _legacyImpl14.GetStaticBaseAddress(methodTable, nonGCStaticsAddress, GCStaticsAddress) : HResults.E_NOTIMPL;
    int ISOSDacInterface14.GetThreadStaticBaseAddress(ulong methodTable, ulong thread, ulong* nonGCStaticsAddress, ulong* GCStaticsAddress)
        => _legacyImpl14 is not null ? _legacyImpl14.GetThreadStaticBaseAddress(methodTable, thread, nonGCStaticsAddress, GCStaticsAddress) : HResults.E_NOTIMPL;
    int ISOSDacInterface14.GetMethodTableInitializationFlags(ulong methodTable, /*MethodTableInitializationFlags*/ int* initializationStatus)
        => _legacyImpl14 is not null ? _legacyImpl14.GetMethodTableInitializationFlags(methodTable, initializationStatus) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface14

    #region ISOSDacInterface15
    int ISOSDacInterface15.GetMethodTableSlotEnumerator(ulong mt, /*ISOSMethodEnum*/void** enumerator)
        => _legacyImpl15 is not null ? _legacyImpl15.GetMethodTableSlotEnumerator(mt, enumerator) : HResults.E_NOTIMPL;
    #endregion ISOSDacInterface15
}
