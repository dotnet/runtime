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
    int GetAppDomainData(ClrDataAddress addr, /*struct DacpAppDomainData*/ void* data);
    [PreserveSig]
    int GetAppDomainName(ClrDataAddress addr, uint count, char* name, uint* pNeeded);
    [PreserveSig]
    int GetDomainFromContext(ClrDataAddress context, ClrDataAddress* domain);

    // Assemblies
    [PreserveSig]
    int GetAssemblyList(ClrDataAddress appDomain, int count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[]? values, int* pNeeded);
    [PreserveSig]
    int GetAssemblyData(ClrDataAddress baseDomainPtr, ClrDataAddress assembly, /*struct DacpAssemblyData*/ void* data);
    [PreserveSig]
    int GetAssemblyName(ClrDataAddress assembly, uint count, char* name, uint* pNeeded);

    // Modules
    [PreserveSig]
    int GetModule(ClrDataAddress addr, out IXCLRDataModule? mod);
    [PreserveSig]
    int GetModuleData(ClrDataAddress moduleAddr, DacpModuleData* data);
    [PreserveSig]
    int TraverseModuleMap(/*ModuleMapType*/ int mmt, ClrDataAddress moduleAddr, /*MODULEMAPTRAVERSE*/ void* pCallback, void* token);
    [PreserveSig]
    int GetAssemblyModuleList(ClrDataAddress assembly, uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] modules, uint* pNeeded);
    [PreserveSig]
    int GetILForModule(ClrDataAddress moduleAddr, int rva, ClrDataAddress* il);

    // Threads
    [PreserveSig]
    int GetThreadData(ClrDataAddress thread, DacpThreadData *data);
    [PreserveSig]
    int GetThreadFromThinlockID(uint thinLockId, ClrDataAddress* pThread);
    [PreserveSig]
    int GetStackLimits(ClrDataAddress threadPtr, ClrDataAddress* lower, ClrDataAddress* upper, ClrDataAddress* fp);

    // MethodDescs
    [PreserveSig]
    int GetMethodDescData(ClrDataAddress methodDesc, ClrDataAddress ip, DacpMethodDescData* data, uint cRevertedRejitVersions, DacpReJitData* rgRevertedRejitData, uint* pcNeededRevertedRejitData);
    [PreserveSig]
    int GetMethodDescPtrFromIP(ClrDataAddress ip, ClrDataAddress* ppMD);
    [PreserveSig]
    int GetMethodDescName(ClrDataAddress methodDesc, uint count, char* name, uint* pNeeded);
    [PreserveSig]
    int GetMethodDescPtrFromFrame(ClrDataAddress frameAddr, ClrDataAddress* ppMD);
    [PreserveSig]
    int GetMethodDescFromToken(ClrDataAddress moduleAddr, /*mdToken*/ uint token, ClrDataAddress* methodDesc);
    [PreserveSig]
    int GetMethodDescTransparencyData(ClrDataAddress methodDesc, /*struct DacpMethodDescTransparencyData*/ void* data);

    // JIT Data
    [PreserveSig]
    int GetCodeHeaderData(ClrDataAddress ip, /*struct DacpCodeHeaderData*/ void* data);
    [PreserveSig]
    int GetJitManagerList(uint count, /*struct DacpJitManagerInfo*/ void* managers, uint* pNeeded);
    [PreserveSig]
    int GetJitHelperFunctionName(ClrDataAddress ip, uint count, byte* name, uint* pNeeded);
    [PreserveSig]
    int GetJumpThunkTarget(/*T_CONTEXT*/void* ctx, ClrDataAddress* targetIP, ClrDataAddress* targetMD);

    // ThreadPool
    [PreserveSig]
    int GetThreadpoolData(/*struct DacpThreadpoolData*/ void* data);
    [PreserveSig]
    int GetWorkRequestData(ClrDataAddress addrWorkRequest, /*struct DacpWorkRequestData*/ void* data);
    [PreserveSig]
    int GetHillClimbingLogEntry(ClrDataAddress addr, /*struct DacpHillClimbingLogEntry*/ void* data);

    // Objects
    [PreserveSig]
    int GetObjectData(ClrDataAddress objAddr, DacpObjectData* data);
    [PreserveSig]
    int GetObjectStringData(ClrDataAddress obj, uint count, char* stringData, uint* pNeeded);
    [PreserveSig]
    int GetObjectClassName(ClrDataAddress obj, uint count, char* className, uint* pNeeded);

    // MethodTable
    [PreserveSig]
    int GetMethodTableName(ClrDataAddress mt, uint count, char* mtName, uint* pNeeded);
    [PreserveSig]
    int GetMethodTableData(ClrDataAddress mt, DacpMethodTableData* data);
    [PreserveSig]
    int GetMethodTableSlot(ClrDataAddress mt, uint slot, ClrDataAddress* value);
    [PreserveSig]
    int GetMethodTableFieldData(ClrDataAddress mt, /*struct DacpMethodTableFieldData*/ void* data);
    [PreserveSig]
    int GetMethodTableTransparencyData(ClrDataAddress mt, /*struct DacpMethodTableTransparencyData*/ void* data);

    // EEClass
    [PreserveSig]
    int GetMethodTableForEEClass(ClrDataAddress eeClass, ClrDataAddress* value);

    // FieldDesc
    [PreserveSig]
    int GetFieldDescData(ClrDataAddress fieldDesc, /*struct DacpFieldDescData*/ void* data);

    // Frames
    [PreserveSig]
    int GetFrameName(ClrDataAddress vtable, uint count, char* frameName, uint* pNeeded);

    // PEFiles
    [PreserveSig]
    int GetPEFileBase(ClrDataAddress addr, ClrDataAddress* peBase);
    [PreserveSig]
    int GetPEFileName(ClrDataAddress addr, uint count, char* fileName, uint* pNeeded);

    // GC
    [PreserveSig]
    int GetGCHeapData(/*struct DacpGcHeapData*/ void* data);
    [PreserveSig]
    int GetGCHeapList(uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] heaps, uint* pNeeded); // svr only
    [PreserveSig]
    int GetGCHeapDetails(ClrDataAddress heap, /*struct DacpGcHeapDetails */ void* details); // wks only
    [PreserveSig]
    int GetGCHeapStaticData(/*struct DacpGcHeapDetails */ void* data);
    [PreserveSig]
    int GetHeapSegmentData(ClrDataAddress seg, /*struct DacpHeapSegmentData */ void* data);
    [PreserveSig]
    int GetOOMData(ClrDataAddress oomAddr, /*struct DacpOomData */ void* data);
    [PreserveSig]
    int GetOOMStaticData(/*struct DacpOomData */ void* data);
    [PreserveSig]
    int GetHeapAnalyzeData(ClrDataAddress addr, /*struct DacpGcHeapAnalyzeData */ void* data);
    [PreserveSig]
    int GetHeapAnalyzeStaticData(/*struct DacpGcHeapAnalyzeData */ void* data);

    // DomainLocal
    [PreserveSig]
    int GetDomainLocalModuleData(ClrDataAddress addr, /*struct DacpDomainLocalModuleData */ void* data);
    [PreserveSig]
    int GetDomainLocalModuleDataFromAppDomain(ClrDataAddress appDomainAddr, int moduleID, /*struct DacpDomainLocalModuleData */ void* data);
    [PreserveSig]
    int GetDomainLocalModuleDataFromModule(ClrDataAddress moduleAddr, /*struct DacpDomainLocalModuleData */ void* data);

    // ThreadLocal
    [PreserveSig]
    int GetThreadLocalModuleData(ClrDataAddress thread, uint index, /*struct DacpThreadLocalModuleData */ void* data);

    // SyncBlock
    [PreserveSig]
    int GetSyncBlockData(uint number, /*struct DacpSyncBlockData */ void* data);
    [PreserveSig]
    int GetSyncBlockCleanupData(ClrDataAddress addr, /*struct DacpSyncBlockCleanupData */ void* data);

    // Handles
    [PreserveSig]
    int GetHandleEnum(/*ISOSHandleEnum*/ void** ppHandleEnum);
    [PreserveSig]
    int GetHandleEnumForTypes([In, MarshalUsing(CountElementName = nameof(count))] uint[] types, uint count, /*ISOSHandleEnum*/ void** ppHandleEnum);
    [PreserveSig]
    int GetHandleEnumForGC(uint gen, /*ISOSHandleEnum*/ void** ppHandleEnum);

    // EH
    [PreserveSig]
    int TraverseEHInfo(ClrDataAddress ip, /*DUMPEHINFO*/ void* pCallback, void* token);
    [PreserveSig]
    int GetNestedExceptionData(ClrDataAddress exception, ClrDataAddress* exceptionObject, ClrDataAddress* nextNestedException);

    // StressLog
    [PreserveSig]
    int GetStressLogAddress(ClrDataAddress* stressLog);

    // Heaps
    [PreserveSig]
    int TraverseLoaderHeap(ClrDataAddress loaderHeapAddr, /*VISITHEAP*/ void* pCallback);
    [PreserveSig]
    int GetCodeHeapList(ClrDataAddress jitManager, uint count, /*struct DacpJitCodeHeapInfo*/ void* codeHeaps, uint* pNeeded);
    [PreserveSig]
    int TraverseVirtCallStubHeap(ClrDataAddress pAppDomain, /*VCSHeapType*/ int heaptype, /*VISITHEAP*/ void* pCallback);

    // Other
    [PreserveSig]
    int GetUsefulGlobals(DacpUsefulGlobalsData* data);
    [PreserveSig]
    int GetClrWatsonBuckets(ClrDataAddress thread, void* pGenericModeBlock);
    [PreserveSig]
    int GetTLSIndex(uint* pIndex);
    [PreserveSig]
    int GetDacModuleHandle(/*HMODULE*/ void* phModule);

    // COM
    [PreserveSig]
    int GetRCWData(ClrDataAddress addr, /*struct DacpRCWData */ void* data);
    [PreserveSig]
    int GetRCWInterfaces(ClrDataAddress rcw, uint count, /*struct DacpCOMInterfacePointerData*/ void* interfaces, uint* pNeeded);
    [PreserveSig]
    int GetCCWData(ClrDataAddress ccw, /*struct DacpCCWData */ void* data);
    [PreserveSig]
    int GetCCWInterfaces(ClrDataAddress ccw, uint count, /*struct DacpCOMInterfacePointerData*/ void* interfaces, uint* pNeeded);
    [PreserveSig]
    int TraverseRCWCleanupList(ClrDataAddress cleanupListPtr, /*VISITRCWFORCLEANUP*/ void* pCallback, void* token);

    // GC Reference Functions

    /*      GetStackReferences
     * Enumerates all references on a given callstack.
     */
    [PreserveSig]
    int GetStackReferences(int osThreadID, /*ISOSStackRefEnum*/ void** ppEnum);
    [PreserveSig]
    int GetRegisterName(int regName, uint count, char* buffer, uint* pNeeded);

    [PreserveSig]
    int GetThreadAllocData(ClrDataAddress thread, /*struct DacpAllocData */ void* data);
    [PreserveSig]
    int GetHeapAllocData(uint count, /*struct DacpGenerationAllocData */ void* data, uint* pNeeded);

    // For BindingDisplay plugin
    [PreserveSig]
    int GetFailedAssemblyList(ClrDataAddress appDomain, int count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ClrDataAddress[] values, uint* pNeeded);
    [PreserveSig]
    int GetPrivateBinPaths(ClrDataAddress appDomain, int count, char* paths, uint* pNeeded);
    [PreserveSig]
    int GetAssemblyLocation(ClrDataAddress assembly, int count, char* location, uint* pNeeded);
    [PreserveSig]
    int GetAppDomainConfigFile(ClrDataAddress appDomain, int count, char* configFile, uint* pNeeded);
    [PreserveSig]
    int GetApplicationBase(ClrDataAddress appDomain, int count, char* appBase, uint* pNeeded);
    [PreserveSig]
    int GetFailedAssemblyData(ClrDataAddress assembly, uint* pContext, int* pResult);
    [PreserveSig]
    int GetFailedAssemblyLocation(ClrDataAddress assesmbly, uint count, char* location, uint* pNeeded);
    [PreserveSig]
    int GetFailedAssemblyDisplayName(ClrDataAddress assembly, uint count, char* name, uint* pNeeded);
};

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
internal struct DacpExceptionObjectData
{
    public ClrDataAddress Message;
    public ClrDataAddress InnerException;
    public ClrDataAddress StackTrace;
    public ClrDataAddress WatsonBuckets;
    public ClrDataAddress StackTraceString;
    public ClrDataAddress RemoteStackTraceString;
    public int HResult;
    public int XCode;
}
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

[GeneratedComInterface]
[Guid("A16026EC-96F4-40BA-87FB-5575986FB7AF")]
internal unsafe partial interface ISOSDacInterface2
{
    [PreserveSig]
    int GetObjectExceptionData(ClrDataAddress objectAddress, DacpExceptionObjectData* data);
    [PreserveSig]
    int IsRCWDCOMProxy(ClrDataAddress rcwAddress, int* inDCOMProxy);
}

[GeneratedComInterface]
[Guid("B08C5CDC-FD8A-49C5-AB38-5FEEF35235B4")]
internal unsafe partial interface ISOSDacInterface3
{
    [PreserveSig]
    int GetGCInterestingInfoData(ClrDataAddress interestingInfoAddr, /*struct DacpGCInterestingInfoData*/ void* data);
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
    int GetTieredVersions(ClrDataAddress methodDesc, int rejitId, /*struct DacpTieredVersionData*/void* nativeCodeAddrs, int cNativeCodeAddrs, int* pcNativeCodeAddrs);
};

[GeneratedComInterface]
[Guid("11206399-4B66-4EDB-98EA-85654E59AD45")]
internal unsafe partial interface ISOSDacInterface6
{
    [PreserveSig]
    int GetMethodTableCollectibleData(ClrDataAddress mt, /*struct DacpMethodTableCollectibleData*/ void* data);
};

[GeneratedComInterface]
[Guid("c1020dde-fe98-4536-a53b-f35a74c327eb")]
internal unsafe partial interface ISOSDacInterface7
{
    [PreserveSig]
    int GetPendingReJITID(ClrDataAddress methodDesc, int* pRejitId);
    [PreserveSig]
    int GetReJITInformation(ClrDataAddress methodDesc, int rejitId, /*struct DacpReJitData2*/ void* pRejitData);
    [PreserveSig]
    int GetProfilerModifiedILInformation(ClrDataAddress methodDesc, /*struct DacpProfilerILData*/ void* pILData);
    [PreserveSig]
    int GetMethodsWithProfilerModifiedIL(ClrDataAddress mod, ClrDataAddress* methodDescs, int cMethodDescs, int* pcMethodDescs);
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
    int GetGenerationTableSvr(ClrDataAddress heapAddr, uint cGenerations, /*struct DacpGenerationData*/ void* pGenerationData, uint* pNeeded);
    [PreserveSig]
    int GetFinalizationFillPointersSvr(ClrDataAddress heapAddr, uint cFillPointers, ClrDataAddress* pFinalizationFillPointers, uint* pNeeded);

    [PreserveSig]
    int GetAssemblyLoadContext(ClrDataAddress methodTable, ClrDataAddress* assemblyLoadContext);
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
    int GetObjectComWrappersData(ClrDataAddress objAddr, ClrDataAddress* rcw, uint count, ClrDataAddress* mowList, uint* pNeeded);
    [PreserveSig]
    int IsComWrappersCCW(ClrDataAddress ccw, Interop.BOOL* isComWrappersCCW);
    [PreserveSig]
    int GetComWrappersCCWData(ClrDataAddress ccw, ClrDataAddress* managedObject, int* refCount);
    [PreserveSig]
    int IsComWrappersRCW(ClrDataAddress rcw, Interop.BOOL* isComWrappersRCW);
    [PreserveSig]
    int GetComWrappersRCWData(ClrDataAddress rcw, ClrDataAddress* identity);
}

[GeneratedComInterface]
[Guid("96BA1DB9-14CD-4492-8065-1CAAECF6E5CF")]
internal unsafe partial interface ISOSDacInterface11
{
    [PreserveSig]
    int IsTrackedType(ClrDataAddress objAddr, Interop.BOOL* isTrackedType, Interop.BOOL* hasTaggedMemory);
    [PreserveSig]
    int GetTaggedMemory(ClrDataAddress objAddr, ClrDataAddress* taggedMemory, nuint* taggedMemorySizeInBytes);
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
    int TraverseLoaderHeap(ClrDataAddress loaderHeapAddr, /*LoaderHeapKind*/ int kind, /*VISITHEAP*/ delegate* unmanaged<ulong, nuint, Interop.BOOL> pCallback);
    [PreserveSig]
    int GetDomainLoaderAllocator(ClrDataAddress domainAddress, ClrDataAddress* pLoaderAllocator);
    [PreserveSig]
    int GetLoaderAllocatorHeapNames(int count, char** ppNames, int* pNeeded);
    [PreserveSig]
    int GetLoaderAllocatorHeaps(ClrDataAddress loaderAllocator, int count, ClrDataAddress* pLoaderHeaps, /*LoaderHeapKind*/ int* pKinds, int* pNeeded);
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
    int GetStaticBaseAddress(ClrDataAddress methodTable, ClrDataAddress* nonGCStaticsAddress, ClrDataAddress* GCStaticsAddress);
    [PreserveSig]
    int GetThreadStaticBaseAddress(ClrDataAddress methodTable, ClrDataAddress thread, ClrDataAddress* nonGCStaticsAddress, ClrDataAddress* GCStaticsAddress);
    [PreserveSig]
    int GetMethodTableInitializationFlags(ClrDataAddress methodTable, /*MethodTableInitializationFlags*/ int* initializationStatus);
}

[GeneratedComInterface]
[Guid("7ed81261-52a9-4a23-a358-c3313dea30a8")]
internal unsafe partial interface ISOSDacInterface15
{
    [PreserveSig]
    int GetMethodTableSlotEnumerator(ClrDataAddress mt, /*ISOSMethodEnum*/void** enumerator);
}
