// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

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
internal sealed partial class SOSDacImpl : ISOSDacInterface, ISOSDacInterface9
{
    private readonly Target _target;

    public SOSDacImpl(Target target)
    {
        _target = target;
    }

    public unsafe int GetAppDomainConfigFile(ulong appDomain, int count, char* configFile, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainName(ulong addr, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainStoreData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetApplicationBase(ulong appDomain, int count, char* appBase, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyData(ulong baseDomainPtr, ulong assembly, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyList(ulong appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, int* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyLocation(ulong assembly, int count, char* location, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyModuleList(ulong assembly, uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] modules, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyName(ulong assembly, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;

    public int GetBreakingChangeVersion()
    {
        return _target.ReadGlobal<byte>(Constants.Globals.SOSBreakingChangeVersion);
    }

    public unsafe int GetCCWData(ulong ccw, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetCCWInterfaces(ulong ccw, uint count, void* interfaces, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetClrWatsonBuckets(ulong thread, void* pGenericModeBlock) => HResults.E_NOTIMPL;
    public unsafe int GetCodeHeaderData(ulong ip, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetCodeHeapList(ulong jitManager, uint count, void* codeHeaps, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetDacModuleHandle(void* phModule) => HResults.E_NOTIMPL;
    public unsafe int GetDomainFromContext(ulong context, ulong* domain) => HResults.E_NOTIMPL;
    public unsafe int GetDomainLocalModuleData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetDomainLocalModuleDataFromAppDomain(ulong appDomainAddr, int moduleID, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetDomainLocalModuleDataFromModule(ulong moduleAddr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyData(ulong assembly, uint* pContext, int* pResult) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyDisplayName(ulong assembly, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyList(ulong appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyLocation(ulong assesmbly, uint count, char* location, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetFieldDescData(ulong fieldDesc, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetFrameName(ulong vtable, uint count, char* frameName, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapDetails(ulong heap, void* details) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] heaps, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapStaticData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHandleEnum(void** ppHandleEnum) => HResults.E_NOTIMPL;
    public unsafe int GetHandleEnumForGC(uint gen, void** ppHandleEnum) => HResults.E_NOTIMPL;
    public unsafe int GetHandleEnumForTypes([In, MarshalUsing(CountElementName = "count")] uint[] types, uint count, void** ppHandleEnum) => HResults.E_NOTIMPL;
    public unsafe int GetHeapAllocData(uint count, void* data, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetHeapAnalyzeData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHeapAnalyzeStaticData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHeapSegmentData(ulong seg, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHillClimbingLogEntry(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetILForModule(ulong moduleAddr, int rva, ulong* il) => HResults.E_NOTIMPL;
    public unsafe int GetJitHelperFunctionName(ulong ip, uint count, byte* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetJitManagerList(uint count, void* managers, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetJumpThunkTarget(void* ctx, ulong* targetIP, ulong* targetMD) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescData(ulong methodDesc, ulong ip, void* data, uint cRevertedRejitVersions, void* rgRevertedRejitData, uint* pcNeededRevertedRejitData) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescFromToken(ulong moduleAddr, uint token, ulong* methodDesc) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescName(ulong methodDesc, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescPtrFromFrame(ulong frameAddr, ulong* ppMD) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescPtrFromIP(ulong ip, ulong* ppMD) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescTransparencyData(ulong methodDesc, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableData(ulong mt, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableFieldData(ulong mt, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableForEEClass(ulong eeClass, ulong* value) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableName(ulong mt, uint count, char* mtName, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableSlot(ulong mt, uint slot, ulong* value) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableTransparencyData(ulong mt, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetModule(ulong addr, void** mod) => HResults.E_NOTIMPL;
    public unsafe int GetModuleData(ulong moduleAddr, void* data) => HResults.E_NOTIMPL;

    public unsafe int GetNestedExceptionData(ulong exception, ulong* exceptionObject, ulong* nextNestedException)
    {
        try
        {
            Contracts.IException contract = _target.Contracts.Exception;
            TargetPointer exceptionObjectLocal = contract.GetExceptionInfo(exception, out TargetPointer nextNestedExceptionLocal);
            *exceptionObject = exceptionObjectLocal;
            *nextNestedException = nextNestedExceptionLocal;
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetObjectClassName(ulong obj, uint count, char* className, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetObjectData(ulong objAddr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetObjectStringData(ulong obj, uint count, char* stringData, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetOOMData(ulong oomAddr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetOOMStaticData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetPEFileBase(ulong addr, ulong* peBase) => HResults.E_NOTIMPL;
    public unsafe int GetPEFileName(ulong addr, uint count, char* fileName, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetPrivateBinPaths(ulong appDomain, int count, char* paths, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetRCWData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetRCWInterfaces(ulong rcw, uint count, void* interfaces, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetRegisterName(int regName, uint count, char* buffer, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetStackLimits(ulong threadPtr, ulong* lower, ulong* upper, ulong* fp) => HResults.E_NOTIMPL;
    public unsafe int GetStackReferences(int osThreadID, void** ppEnum) => HResults.E_NOTIMPL;
    public unsafe int GetStressLogAddress(ulong* stressLog) => HResults.E_NOTIMPL;
    public unsafe int GetSyncBlockCleanupData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetSyncBlockData(uint number, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetThreadAllocData(ulong thread, void* data) => HResults.E_NOTIMPL;

    public unsafe int GetThreadData(ulong thread, DacpThreadData* data)
    {
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
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }
    public unsafe int GetThreadFromThinlockID(uint thinLockId, ulong* pThread) => HResults.E_NOTIMPL;
    public unsafe int GetThreadLocalModuleData(ulong thread, uint index, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetThreadpoolData(void* data) => HResults.E_NOTIMPL;

    public unsafe int GetThreadStoreData(DacpThreadStoreData* data)
    {
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
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetTLSIndex(uint* pIndex) => HResults.E_NOTIMPL;
    public unsafe int GetUsefulGlobals(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetWorkRequestData(ulong addrWorkRequest, void* data) => HResults.E_NOTIMPL;
    public unsafe int TraverseEHInfo(ulong ip, void* pCallback, void* token) => HResults.E_NOTIMPL;
    public unsafe int TraverseLoaderHeap(ulong loaderHeapAddr, void* pCallback) => HResults.E_NOTIMPL;
    public unsafe int TraverseModuleMap(int mmt, ulong moduleAddr, void* pCallback, void* token) => HResults.E_NOTIMPL;
    public unsafe int TraverseRCWCleanupList(ulong cleanupListPtr, void* pCallback, void* token) => HResults.E_NOTIMPL;
    public unsafe int TraverseVirtCallStubHeap(ulong pAppDomain, int heaptype, void* pCallback) => HResults.E_NOTIMPL;
}
