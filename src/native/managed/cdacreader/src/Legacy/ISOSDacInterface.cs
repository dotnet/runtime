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
    public ulong firstThread;
    public ulong finalizerThread;
    public ulong gcThread;
    public int fHostConfig; // Uses hosting flags defined above
};

internal struct DacpThreadData
{
    public int corThreadId;
    public int osThreadId;
    public int state;
    public uint preemptiveGCDisabled;
    public ulong allocContextPtr;
    public ulong allocContextLimit;
    public ulong context;
    public ulong domain;
    public ulong pFrame;
    public int lockCount;
    public ulong firstNestedException;  // Pass this pointer to DacpNestedExceptionInfo
    public ulong teb;
    public ulong fiberData;
    public ulong lastThrownObjectHandle;
    public ulong nextThread;
}
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

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
    int GetAppDomainList(uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ulong[] values, uint* pNeeded);
    [PreserveSig]
    int GetAppDomainData(ulong addr, /*struct DacpAppDomainData*/ void* data);
    [PreserveSig]
    int GetAppDomainName(ulong addr, uint count, char* name, uint* pNeeded);
    [PreserveSig]
    int GetDomainFromContext(ulong context, ulong* domain);

    // Assemblies
    [PreserveSig]
    int GetAssemblyList(ulong appDomain, int count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ulong[] values, int* pNeeded);
    [PreserveSig]
    int GetAssemblyData(ulong baseDomainPtr, ulong assembly, /*struct DacpAssemblyData*/ void* data);
    [PreserveSig]
    int GetAssemblyName(ulong assembly, uint count, char* name, uint* pNeeded);

    // Modules
    [PreserveSig]
    int GetModule(ulong addr, /*IXCLRDataModule*/ void** mod);
    [PreserveSig]
    int GetModuleData(ulong moduleAddr, /*struct DacpModuleData*/ void* data);
    [PreserveSig]
    int TraverseModuleMap(/*ModuleMapType*/ int mmt, ulong moduleAddr, /*MODULEMAPTRAVERSE*/ void* pCallback, void* token);
    [PreserveSig]
    int GetAssemblyModuleList(ulong assembly, uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ulong[] modules, uint* pNeeded);
    [PreserveSig]
    int GetILForModule(ulong moduleAddr, int rva, ulong* il);

    // Threads
    [PreserveSig]
    int GetThreadData(ulong thread, DacpThreadData *data);
    [PreserveSig]
    int GetThreadFromThinlockID(uint thinLockId, ulong* pThread);
    [PreserveSig]
    int GetStackLimits(ulong threadPtr, ulong* lower, ulong* upper, ulong* fp);

    // MethodDescs
    [PreserveSig]
    int GetMethodDescData(ulong methodDesc, ulong ip, /*struct DacpMethodDescData*/ void* data, uint cRevertedRejitVersions, /*struct DacpReJitData*/ void* rgRevertedRejitData, uint* pcNeededRevertedRejitData);
    [PreserveSig]
    int GetMethodDescPtrFromIP(ulong ip, ulong* ppMD);
    [PreserveSig]
    int GetMethodDescName(ulong methodDesc, uint count, char* name, uint* pNeeded);
    [PreserveSig]
    int GetMethodDescPtrFromFrame(ulong frameAddr, ulong* ppMD);
    [PreserveSig]
    int GetMethodDescFromToken(ulong moduleAddr, /*mdToken*/ uint token, ulong* methodDesc);
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
    int GetJumpThunkTarget(/*T_CONTEXT*/void* ctx, ulong* targetIP, ulong* targetMD);

    // ThreadPool
    [PreserveSig]
    int GetThreadpoolData(/*struct DacpThreadpoolData*/ void* data);
    [PreserveSig]
    int GetWorkRequestData(ulong addrWorkRequest, /*struct DacpWorkRequestData*/ void* data);
    [PreserveSig]
    int GetHillClimbingLogEntry(ulong addr, /*struct DacpHillClimbingLogEntry*/ void* data);

    // Objects
    [PreserveSig]
    int GetObjectData(ulong objAddr, /*struct DacpObjectData*/ void* data);
    [PreserveSig]
    int GetObjectStringData(ulong obj, uint count, char* stringData, uint* pNeeded);
    [PreserveSig]
    int GetObjectClassName(ulong obj, uint count, char* className, uint* pNeeded);

    // MethodTable
    [PreserveSig]
    int GetMethodTableName(ulong mt, uint count, char* mtName, uint* pNeeded);
    [PreserveSig]
    int GetMethodTableData(ulong mt, /*struct DacpMethodTableData*/ void* data);
    [PreserveSig]
    int GetMethodTableSlot(ulong mt, uint slot, ulong* value);
    [PreserveSig]
    int GetMethodTableFieldData(ulong mt, /*struct DacpMethodTableFieldData*/ void* data);
    [PreserveSig]
    int GetMethodTableTransparencyData(ulong mt, /*struct DacpMethodTableTransparencyData*/ void* data);

    // EEClass
    [PreserveSig]
    int GetMethodTableForEEClass(ulong eeClass, ulong* value);

    // FieldDesc
    [PreserveSig]
    int GetFieldDescData(ulong fieldDesc, /*struct DacpFieldDescData*/ void* data);

    // Frames
    [PreserveSig]
    int GetFrameName(ulong vtable, uint count, char* frameName, uint* pNeeded);

    // PEFiles
    [PreserveSig]
    int GetPEFileBase(ulong addr, ulong* peBase);
    [PreserveSig]
    int GetPEFileName(ulong addr, uint count, char* fileName, uint* pNeeded);

    // GC
    [PreserveSig]
    int GetGCHeapData(/*struct DacpGcHeapData*/ void* data);
    [PreserveSig]
    int GetGCHeapList(uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ulong[] heaps, uint* pNeeded); // svr only
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
    int GetNestedExceptionData(ulong exception, ulong* exceptionObject, ulong* nextNestedException);

    // StressLog
    [PreserveSig]
    int GetStressLogAddress(ulong* stressLog);

    // Heaps
    [PreserveSig]
    int TraverseLoaderHeap(ulong loaderHeapAddr, /*VISITHEAP*/ void* pCallback);
    [PreserveSig]
    int GetCodeHeapList(ulong jitManager, uint count, /*struct DacpJitCodeHeapInfo*/ void* codeHeaps, uint* pNeeded);
    [PreserveSig]
    int TraverseVirtCallStubHeap(ulong pAppDomain, /*VCSHeapType*/ int heaptype, /*VISITHEAP*/ void* pCallback);

    // Other
    [PreserveSig]
    int GetUsefulGlobals(/*struct DacpUsefulGlobalsData */ void* data);
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
    int GetFailedAssemblyList(ulong appDomain, int count, [In, Out, MarshalUsing(CountElementName = nameof(count))] ulong[] values, uint* pNeeded);
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

[GeneratedComInterface]
[Guid("4eca42d8-7e7b-4c8a-a116-7bfbf6929267")]
internal partial interface ISOSDacInterface9
{
    int GetBreakingChangeVersion();
}
