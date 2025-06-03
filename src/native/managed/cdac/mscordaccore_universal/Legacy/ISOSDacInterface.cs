// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

// This file contains managed declarations for the SOS-DAC interfaces.
// See src/coreclr/inc/sospriv.idl

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
internal struct DacpThreadStoreData
{
    public int threadCount;
    public int unstartedThreadCount;
    public int backgroundThreadCount;
    public int pendingThreadCount;
    public int deadThreadCount;
    public ClrDataAddress firstThread;
    public ClrDataAddress finalizerThread;
    public ClrDataAddress gcThread;
    public int fHostConfig; // Uses hosting flags defined above
};

internal struct DacpThreadData
{
    public int corThreadId;
    public int osThreadId;
    public int state;
    public uint preemptiveGCDisabled;
    public ClrDataAddress allocContextPtr;
    public ClrDataAddress allocContextLimit;
    public ClrDataAddress context;
    public ClrDataAddress domain;
    public ClrDataAddress pFrame;
    public int lockCount;
    public ClrDataAddress firstNestedException;  // Pass this pointer to DacpNestedExceptionInfo
    public ClrDataAddress teb;
    public ClrDataAddress fiberData;
    public ClrDataAddress lastThrownObjectHandle;
    public ClrDataAddress nextThread;
}

internal struct DacpModuleData
{
    public ClrDataAddress Address;
    public ClrDataAddress PEAssembly; // Actually the module address in .NET 9+
    public ClrDataAddress ilBase;
    public ClrDataAddress metadataStart;
    public ulong metadataSize;
    public ClrDataAddress Assembly; // Assembly pointer
    public uint isReflection;
    public uint isPEFile;

    public ulong dwBaseClassIndex; // Always 0 - .NET no longer has this
    public ulong dwModuleID; // Always 0 - .NET no longer has this

    public uint dwTransientFlags;

    public ClrDataAddress TypeDefToMethodTableMap;
    public ClrDataAddress TypeRefToMethodTableMap;
    public ClrDataAddress MethodDefToDescMap;
    public ClrDataAddress FieldDefToDescMap;
    public ClrDataAddress MemberRefToDescMap;
    public ClrDataAddress FileReferencesMap;
    public ClrDataAddress ManifestModuleReferencesMap;

    public ClrDataAddress LoaderAllocator;
    public ClrDataAddress ThunkHeap;

    public ulong dwModuleIndex; // Always 0 - .NET no longer has this
}

internal struct DacpMethodTableData
{
    public int bIsFree; // everything else is NULL if this is true.
    public ClrDataAddress module;
    public ClrDataAddress klass;
    public ClrDataAddress parentMethodTable;
    public ushort wNumInterfaces;
    public ushort wNumMethods;
    public ushort wNumVtableSlots;
    public ushort wNumVirtuals;
    public uint baseSize;
    public uint componentSize;
    public uint /*mdTypeDef*/ cl; // Metadata token
    public uint dwAttrClass; // cached metadata
    public int bIsShared;  // Always false, preserved for backward compatibility
    public int bIsDynamic;
    public int bContainsGCPointers;
}

internal enum DacpObjectType
{
    OBJ_STRING = 0,
    OBJ_FREE,
    OBJ_OBJECT,
    OBJ_ARRAY,
    OBJ_OTHER
};

internal struct DacpObjectData
{
    public ClrDataAddress MethodTable;
    public DacpObjectType ObjectType;
    public ulong Size;
    public ClrDataAddress ElementTypeHandle;
    public uint ElementType;
    public uint dwRank;
    public ulong dwNumComponents;
    public ulong dwComponentSize;
    public ClrDataAddress ArrayDataPtr;
    public ClrDataAddress ArrayBoundsPtr;
    public ClrDataAddress ArrayLowerBoundsPtr;
    public ulong RCW;
    public ulong CCW;
}

internal struct DacpUsefulGlobalsData
{
    public ClrDataAddress ArrayMethodTable;
    public ClrDataAddress StringMethodTable;
    public ClrDataAddress ObjectMethodTable;
    public ClrDataAddress ExceptionMethodTable;
    public ClrDataAddress FreeMethodTable;
}
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

internal struct DacpReJitData
{
    // FIXME[cdac]: the C++ definition enum doesn't have an explicit underlying type or constant values for the flags
    public enum Flags : uint
    {
        kUnknown = 0,
        kRequested = 1,
        kActive = 2,
        kReverted = 3,
    };

    public ClrDataAddress rejitID;
    public Flags flags; /* = Flags::kUnknown*/
    public ClrDataAddress NativeCodeAddr;
};

internal struct DacpMethodDescData
{
    public int bHasNativeCode;
    public int bIsDynamic;
    public ushort wSlotNumber;
    public ClrDataAddress NativeCodeAddr;
    // Useful for breaking when a method is jitted.
    public ClrDataAddress AddressOfNativeCodeSlot;

    public ClrDataAddress MethodDescPtr;
    public ClrDataAddress MethodTablePtr;
    public ClrDataAddress ModulePtr;

    public uint /*mdToken*/ MDToken;
    public ClrDataAddress GCInfo;
    public ClrDataAddress GCStressCodeCopy;

    // This is only valid if bIsDynamic is true
    public ClrDataAddress managedDynamicMethodObject;

    public ClrDataAddress requestedIP;

    // Gives info for the single currently active version of a method
    public DacpReJitData rejitDataCurrent;

    // Gives info corresponding to requestedIP (for !ip2md)
    public DacpReJitData rejitDataRequested;

    // Total number of rejit versions that have been jitted
    public uint /*ULONG*/ cJittedRejitVersions;

}

[GeneratedComInterface]
[Guid("436f00f2-b42a-4b9f-870c-e73db66ae930")]
internal unsafe partial interface ISOSDacInterface
{
    // All functions are explicitly PreserveSig so that we can just return E_NOTIMPL instead of throwing
    // as the cDAC slowly replaces parts of the DAC.

    // ThreadStore
    [PreserveSig]
    int GetThreadStoreData(DacpThreadStoreData* data);

    // AppDomains
    [PreserveSig]
    int GetAppDomainStoreData(/*struct DacpAppDomainStoreData*/ void* data);
    [PreserveSig]
    int GetAppDomainList(uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] values, uint* pNeeded);
    [PreserveSig]
    int GetAppDomainData(ulong addr, /*struct DacpAppDomainData*/ void* data);
    [PreserveSig]
    int GetAppDomainName(ulong addr, uint count, char* name, uint* pNeeded);
    [PreserveSig]
    int GetDomainFromContext(ulong context, ClrDataAddress* domain);

    // Assemblies
    [PreserveSig]
    int GetAssemblyList(ulong appDomain, int count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[]? values, int* pNeeded);
    [PreserveSig]
    int GetAssemblyData(ulong baseDomainPtr, ulong assembly, /*struct DacpAssemblyData*/ void* data);
    [PreserveSig]
    int GetAssemblyName(ulong assembly, uint count, char* name, uint* pNeeded);

    // Modules
    [PreserveSig]
    int GetModule(ulong addr, out IXCLRDataModule? mod);
    [PreserveSig]
    int GetModuleData(ulong moduleAddr, DacpModuleData* data);
    [PreserveSig]
    int TraverseModuleMap(/*ModuleMapType*/ int mmt, ulong moduleAddr, /*MODULEMAPTRAVERSE*/ void* pCallback, void* token);
    [PreserveSig]
    int GetAssemblyModuleList(ulong assembly, uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] modules, uint* pNeeded);
    [PreserveSig]
    int GetILForModule(ulong moduleAddr, int rva, ClrDataAddress* il);

    // Threads
    [PreserveSig]
    int GetThreadData(ulong thread, DacpThreadData *data);
    [PreserveSig]
    int GetThreadFromThinlockID(uint thinLockId, ClrDataAddress* pThread);
    [PreserveSig]
    int GetStackLimits(ulong threadPtr, ClrDataAddress* lower, ClrDataAddress* upper, ClrDataAddress* fp);

    // MethodDescs
    [PreserveSig]
    int GetMethodDescData(ulong methodDesc, ulong ip, DacpMethodDescData* data, uint cRevertedRejitVersions, DacpReJitData* rgRevertedRejitData, uint* pcNeededRevertedRejitData);
    [PreserveSig]
    int GetMethodDescPtrFromIP(ulong ip, ClrDataAddress* ppMD);
    [PreserveSig]
    int GetMethodDescName(ulong methodDesc, uint count, char* name, uint* pNeeded);
    [PreserveSig]
    int GetMethodDescPtrFromFrame(ulong frameAddr, ClrDataAddress* ppMD);
    [PreserveSig]
    int GetMethodDescFromToken(ulong moduleAddr, /*mdToken*/ uint token, ClrDataAddress* methodDesc);
    [PreserveSig]
    int GetMethodDescTransparencyData(ulong methodDesc, /*struct DacpMethodDescTransparencyData*/ void* data);

    // JIT Data
    [PreserveSig]
    int GetCodeHeaderData(ulong ip, /*struct DacpCodeHeaderData*/ void* data);
    [PreserveSig]
    int GetJitManagerList(uint count, /*struct DacpJitManagerInfo*/ void* managers, uint* pNeeded);
    [PreserveSig]
    int GetJitHelperFunctionName(ulong ip, uint count, byte* name, uint* pNeeded);
    [PreserveSig]
    int GetJumpThunkTarget(/*T_CONTEXT*/void* ctx, ClrDataAddress* targetIP, ClrDataAddress* targetMD);

    // ThreadPool
    [PreserveSig]
    int GetThreadpoolData(/*struct DacpThreadpoolData*/ void* data);
    [PreserveSig]
    int GetWorkRequestData(ulong addrWorkRequest, /*struct DacpWorkRequestData*/ void* data);
    [PreserveSig]
    int GetHillClimbingLogEntry(ulong addr, /*struct DacpHillClimbingLogEntry*/ void* data);

    // Objects
    [PreserveSig]
    int GetObjectData(ulong objAddr, DacpObjectData* data);
    [PreserveSig]
    int GetObjectStringData(ulong obj, uint count, char* stringData, uint* pNeeded);
    [PreserveSig]
    int GetObjectClassName(ulong obj, uint count, char* className, uint* pNeeded);

    // MethodTable
    [PreserveSig]
    int GetMethodTableName(ulong mt, uint count, char* mtName, uint* pNeeded);
    [PreserveSig]
    int GetMethodTableData(ulong mt, DacpMethodTableData* data);
    [PreserveSig]
    int GetMethodTableSlot(ulong mt, uint slot, ClrDataAddress* value);
    [PreserveSig]
    int GetMethodTableFieldData(ulong mt, /*struct DacpMethodTableFieldData*/ void* data);
    [PreserveSig]
    int GetMethodTableTransparencyData(ulong mt, /*struct DacpMethodTableTransparencyData*/ void* data);

    // EEClass
    [PreserveSig]
    int GetMethodTableForEEClass(ulong eeClass, ClrDataAddress* value);

    // FieldDesc
    [PreserveSig]
    int GetFieldDescData(ulong fieldDesc, /*struct DacpFieldDescData*/ void* data);

    // Frames
    [PreserveSig]
    int GetFrameName(ulong vtable, uint count, char* frameName, uint* pNeeded);

    // PEFiles
    [PreserveSig]
    int GetPEFileBase(ulong addr, ClrDataAddress* peBase);
    [PreserveSig]
    int GetPEFileName(ulong addr, uint count, char* fileName, uint* pNeeded);

    // GC
    [PreserveSig]
    int GetGCHeapData(/*struct DacpGcHeapData*/ void* data);
    [PreserveSig]
    int GetGCHeapList(uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] heaps, uint* pNeeded); // svr only
    [PreserveSig]
    int GetGCHeapDetails(ulong heap, /*struct DacpGcHeapDetails */ void* details); // wks only
    [PreserveSig]
    int GetGCHeapStaticData(/*struct DacpGcHeapDetails */ void* data);
    [PreserveSig]
    int GetHeapSegmentData(ulong seg, /*struct DacpHeapSegmentData */ void* data);
    [PreserveSig]
    int GetOOMData(ulong oomAddr, /*struct DacpOomData */ void* data);
    [PreserveSig]
    int GetOOMStaticData(/*struct DacpOomData */ void* data);
    [PreserveSig]
    int GetHeapAnalyzeData(ulong addr, /*struct DacpGcHeapAnalyzeData */ void* data);
    [PreserveSig]
    int GetHeapAnalyzeStaticData(/*struct DacpGcHeapAnalyzeData */ void* data);

    // DomainLocal
    [PreserveSig]
    int GetDomainLocalModuleData(ulong addr, /*struct DacpDomainLocalModuleData */ void* data);
    [PreserveSig]
    int GetDomainLocalModuleDataFromAppDomain(ulong appDomainAddr, int moduleID, /*struct DacpDomainLocalModuleData */ void* data);
    [PreserveSig]
    int GetDomainLocalModuleDataFromModule(ulong moduleAddr, /*struct DacpDomainLocalModuleData */ void* data);

    // ThreadLocal
    [PreserveSig]
    int GetThreadLocalModuleData(ulong thread, uint index, /*struct DacpThreadLocalModuleData */ void* data);

    // SyncBlock
    [PreserveSig]
    int GetSyncBlockData(uint number, /*struct DacpSyncBlockData */ void* data);
    [PreserveSig]
    int GetSyncBlockCleanupData(ulong addr, /*struct DacpSyncBlockCleanupData */ void* data);

    // Handles
    [PreserveSig]
    int GetHandleEnum(/*ISOSHandleEnum*/ void** ppHandleEnum);
    [PreserveSig]
    int GetHandleEnumForTypes([In, MarshalUsing(CountElementName = nameof(count))] uint[] types, uint count, /*ISOSHandleEnum*/ void** ppHandleEnum);
    [PreserveSig]
    int GetHandleEnumForGC(uint gen, /*ISOSHandleEnum*/ void** ppHandleEnum);

    // EH
    [PreserveSig]
    int TraverseEHInfo(ulong ip, /*DUMPEHINFO*/ void* pCallback, void* token);
    [PreserveSig]
    int GetNestedExceptionData(ulong exception, ClrDataAddress* exceptionObject, ClrDataAddress* nextNestedException);

    // StressLog
    [PreserveSig]
    int GetStressLogAddress(ClrDataAddress* stressLog);

    // Heaps
    [PreserveSig]
    int TraverseLoaderHeap(ulong loaderHeapAddr, /*VISITHEAP*/ void* pCallback);
    [PreserveSig]
    int GetCodeHeapList(ulong jitManager, uint count, /*struct DacpJitCodeHeapInfo*/ void* codeHeaps, uint* pNeeded);
    [PreserveSig]
    int TraverseVirtCallStubHeap(ulong pAppDomain, /*VCSHeapType*/ int heaptype, /*VISITHEAP*/ void* pCallback);

    // Other
    [PreserveSig]
    int GetUsefulGlobals(DacpUsefulGlobalsData* data);
    [PreserveSig]
    int GetClrWatsonBuckets(ulong thread, void* pGenericModeBlock);
    [PreserveSig]
    int GetTLSIndex(uint* pIndex);
    [PreserveSig]
    int GetDacModuleHandle(/*HMODULE*/ void* phModule);

    // COM
    [PreserveSig]
    int GetRCWData(ulong addr, /*struct DacpRCWData */ void* data);
    [PreserveSig]
    int GetRCWInterfaces(ulong rcw, uint count, /*struct DacpCOMInterfacePointerData*/ void* interfaces, uint* pNeeded);
    [PreserveSig]
    int GetCCWData(ulong ccw, /*struct DacpCCWData */ void* data);
    [PreserveSig]
    int GetCCWInterfaces(ulong ccw, uint count, /*struct DacpCOMInterfacePointerData*/ void* interfaces, uint* pNeeded);
    [PreserveSig]
    int TraverseRCWCleanupList(ulong cleanupListPtr, /*VISITRCWFORCLEANUP*/ void* pCallback, void* token);

    // GC Reference Functions

    /*      GetStackReferences
     * Enumerates all references on a given callstack.
     */
    [PreserveSig]
    int GetStackReferences(int osThreadID, /*ISOSStackRefEnum*/ void** ppEnum);
    [PreserveSig]
    int GetRegisterName(int regName, uint count, char* buffer, uint* pNeeded);

    [PreserveSig]
    int GetThreadAllocData(ulong thread, /*struct DacpAllocData */ void* data);
    [PreserveSig]
    int GetHeapAllocData(uint count, /*struct DacpGenerationAllocData */ void* data, uint* pNeeded);

    // For BindingDisplay plugin
    [PreserveSig]
    int GetFailedAssemblyList(ulong appDomain, int count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] values, uint* pNeeded);
    [PreserveSig]
    int GetPrivateBinPaths(ulong appDomain, int count, char* paths, uint* pNeeded);
    [PreserveSig]
    int GetAssemblyLocation(ulong assembly, int count, char* location, uint* pNeeded);
    [PreserveSig]
    int GetAppDomainConfigFile(ulong appDomain, int count, char* configFile, uint* pNeeded);
    [PreserveSig]
    int GetApplicationBase(ulong appDomain, int count, char* appBase, uint* pNeeded);
    [PreserveSig]
    int GetFailedAssemblyData(ulong assembly, uint* pContext, int* pResult);
    [PreserveSig]
    int GetFailedAssemblyLocation(ulong assesmbly, uint count, char* location, uint* pNeeded);
    [PreserveSig]
    int GetFailedAssemblyDisplayName(ulong assembly, uint count, char* name, uint* pNeeded);
};

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
internal struct DacpExceptionObjectData
{
    public ulong Message;
    public ulong InnerException;
    public ulong StackTrace;
    public ulong WatsonBuckets;
    public ulong StackTraceString;
    public ulong RemoteStackTraceString;
    public int HResult;
    public int XCode;
}
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

[GeneratedComInterface]
[Guid("A16026EC-96F4-40BA-87FB-5575986FB7AF")]
internal unsafe partial interface ISOSDacInterface2
{
    [PreserveSig]
    int GetObjectExceptionData(ulong objectAddress, DacpExceptionObjectData* data);
    [PreserveSig]
    int IsRCWDCOMProxy(ulong rcwAddress, int* inDCOMProxy);
}

[GeneratedComInterface]
[Guid("B08C5CDC-FD8A-49C5-AB38-5FEEF35235B4")]
internal unsafe partial interface ISOSDacInterface3
{
    [PreserveSig]
    int GetGCInterestingInfoData(ulong interestingInfoAddr, /*struct DacpGCInterestingInfoData*/ void* data);
    [PreserveSig]
    int GetGCInterestingInfoStaticData(/*struct DacpGCInterestingInfoData*/ void* data);
    [PreserveSig]
    int GetGCGlobalMechanisms(nuint* globalMechanisms);
};

[GeneratedComInterface]
[Guid("74B9D34C-A612-4B07-93DD-5462178FCE11")]
internal unsafe partial interface ISOSDacInterface4
{
    [PreserveSig]
    int GetClrNotification([In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] arguments, int count, int* pNeeded);
};

[GeneratedComInterface]
[Guid("127d6abe-6c86-4e48-8e7b-220781c58101")]
internal unsafe partial interface ISOSDacInterface5
{
    [PreserveSig]
    int GetTieredVersions(ulong methodDesc, int rejitId, /*struct DacpTieredVersionData*/void* nativeCodeAddrs, int cNativeCodeAddrs, int* pcNativeCodeAddrs);
};

[GeneratedComInterface]
[Guid("11206399-4B66-4EDB-98EA-85654E59AD45")]
internal unsafe partial interface ISOSDacInterface6
{
    [PreserveSig]
    int GetMethodTableCollectibleData(ulong mt, /*struct DacpMethodTableCollectibleData*/ void* data);
};

[GeneratedComInterface]
[Guid("c1020dde-fe98-4536-a53b-f35a74c327eb")]
internal unsafe partial interface ISOSDacInterface7
{
    [PreserveSig]
    int GetPendingReJITID(ulong methodDesc, int* pRejitId);
    [PreserveSig]
    int GetReJITInformation(ulong methodDesc, int rejitId, /*struct DacpReJitData2*/ void* pRejitData);
    [PreserveSig]
    int GetProfilerModifiedILInformation(ulong methodDesc, /*struct DacpProfilerILData*/ void* pILData);
    [PreserveSig]
    int GetMethodsWithProfilerModifiedIL(ulong mod, ClrDataAddress* methodDescs, int cMethodDescs, int* pcMethodDescs);
};

[GeneratedComInterface]
[Guid("c12f35a9-e55c-4520-a894-b3dc5165dfce")]
internal unsafe partial interface ISOSDacInterface8
{
    [PreserveSig]
    int GetNumberGenerations(uint* pGenerations);

    // WKS
    [PreserveSig]
    int GetGenerationTable(uint cGenerations, /*struct DacpGenerationData*/ void* pGenerationData, uint* pNeeded);
    [PreserveSig]
    int GetFinalizationFillPointers(uint cFillPointers, ClrDataAddress* pFinalizationFillPointers, uint* pNeeded);

    // SVR
    [PreserveSig]
    int GetGenerationTableSvr(ulong heapAddr, uint cGenerations, /*struct DacpGenerationData*/ void* pGenerationData, uint* pNeeded);
    [PreserveSig]
    int GetFinalizationFillPointersSvr(ulong heapAddr, uint cFillPointers, ClrDataAddress* pFinalizationFillPointers, uint* pNeeded);

    [PreserveSig]
    int GetAssemblyLoadContext(ulong methodTable, ClrDataAddress* assemblyLoadContext);
}

[GeneratedComInterface]
[Guid("4eca42d8-7e7b-4c8a-a116-7bfbf6929267")]
internal partial interface ISOSDacInterface9
{
    int GetBreakingChangeVersion();
}

[GeneratedComInterface]
[Guid("90B8FCC3-7251-4B0A-AE3D-5C13A67EC9AA")]
internal unsafe partial interface ISOSDacInterface10
{
    [PreserveSig]
    int GetObjectComWrappersData(ulong objAddr, ClrDataAddress* rcw, uint count, ClrDataAddress* mowList, uint* pNeeded);
    [PreserveSig]
    int IsComWrappersCCW(ulong ccw, Interop.BOOL* isComWrappersCCW);
    [PreserveSig]
    int GetComWrappersCCWData(ulong ccw, ClrDataAddress* managedObject, int* refCount);
    [PreserveSig]
    int IsComWrappersRCW(ulong rcw, Interop.BOOL* isComWrappersRCW);
    [PreserveSig]
    int GetComWrappersRCWData(ulong rcw, ClrDataAddress* identity);
}

[GeneratedComInterface]
[Guid("96BA1DB9-14CD-4492-8065-1CAAECF6E5CF")]
internal unsafe partial interface ISOSDacInterface11
{
    [PreserveSig]
    int IsTrackedType(ulong objAddr, Interop.BOOL* isTrackedType, Interop.BOOL* hasTaggedMemory);
    [PreserveSig]
    int GetTaggedMemory(ulong objAddr, ClrDataAddress* taggedMemory, nuint* taggedMemorySizeInBytes);
}

[GeneratedComInterface]
[Guid("1b93bacc-8ca4-432d-943a-3e6e7ec0b0a3")]
internal unsafe partial interface ISOSDacInterface12
{
    [PreserveSig]
    int GetGlobalAllocationContext(ClrDataAddress* allocPtr, ClrDataAddress* allocLimit);
}

[GeneratedComInterface]
[Guid("3176a8ed-597b-4f54-a71f-83695c6a8c5e")]
internal unsafe partial interface ISOSDacInterface13
{
    [PreserveSig]
    int TraverseLoaderHeap(ulong loaderHeapAddr, /*LoaderHeapKind*/ int kind, /*VISITHEAP*/ delegate* unmanaged<ulong, nuint, Interop.BOOL> pCallback);
    [PreserveSig]
    int GetDomainLoaderAllocator(ulong domainAddress, ClrDataAddress* pLoaderAllocator);
    [PreserveSig]
    int GetLoaderAllocatorHeapNames(int count, char** ppNames, int* pNeeded);
    [PreserveSig]
    int GetLoaderAllocatorHeaps(ulong loaderAllocator, int count, ClrDataAddress* pLoaderHeaps, /*LoaderHeapKind*/ int* pKinds, int* pNeeded);
    [PreserveSig]
    int GetHandleTableMemoryRegions(/*ISOSMemoryEnum*/ void** ppEnum);
    [PreserveSig]
    int GetGCBookkeepingMemoryRegions(/*ISOSMemoryEnum*/ void** ppEnum);
    [PreserveSig]
    int GetGCFreeRegions(/*ISOSMemoryEnum*/ void** ppEnum);
    [PreserveSig]
    int LockedFlush();
}

[GeneratedComInterface]
[Guid("9aa22aca-6dc6-4a0c-b4e0-70d2416b9837")]
internal unsafe partial interface ISOSDacInterface14
{
    [PreserveSig]
    int GetStaticBaseAddress(ulong methodTable, ClrDataAddress* nonGCStaticsAddress, ClrDataAddress* GCStaticsAddress);
    [PreserveSig]
    int GetThreadStaticBaseAddress(ulong methodTable, ulong thread, ClrDataAddress* nonGCStaticsAddress, ClrDataAddress* GCStaticsAddress);
    [PreserveSig]
    int GetMethodTableInitializationFlags(ulong methodTable, /*MethodTableInitializationFlags*/ int* initializationStatus);
}

[GeneratedComInterface]
[Guid("7ed81261-52a9-4a23-a358-c3313dea30a8")]
internal unsafe partial interface ISOSDacInterface15
{
    [PreserveSig]
    int GetMethodTableSlotEnumerator(ulong mt, /*ISOSMethodEnum*/void** enumerator);
}
