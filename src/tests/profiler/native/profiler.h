// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define NOMINMAX

#include <atomic>
#include <cstdio>
#include <thread>
#include <chrono>
#include "cor.h"
#include "corprof.h"
#include "holder.h"
#include "profilerstring.h"
#include "event.h"

#if WIN32
#define EXPORT
#else // WIN32
#define EXPORT __attribute__ ((visibility ("default")))
#endif // WIN32

#define SHORT_LENGTH    32
#define STRING_LENGTH  256
#define LONG_LENGTH   1024

typedef void (*ProfilerCallback) (void);

class ShutdownGuard
{

private:
    static std::atomic<bool> s_preventHooks;
    static std::atomic<int> s_hooksInProgress;

public:
    ShutdownGuard()
    {
        ++s_hooksInProgress;
    }

    ~ShutdownGuard()
    {
        --s_hooksInProgress;
    }

    static void Initialize()
    {
        s_preventHooks = false;
        s_hooksInProgress = 0;
    }

    static bool HasShutdownStarted()
    {
        return s_preventHooks.load();
    }

    static void WaitForInProgressHooks()
    {
        s_preventHooks = true;

        while (s_hooksInProgress.load() > 0)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }
    }
};

// Managed code can keep running after Shutdown() is called, and things like
// ELT hooks will continue to be called. We would AV if we tried to call
// in to freed resources.
#define SHUTDOWNGUARD()                         \
    ShutdownGuard shutdownGuard;                \
    if (ShutdownGuard::HasShutdownStarted())    \
    {                                           \
        return S_OK;                            \
    }

#define SHUTDOWNGUARD_RETVOID()                 \
    ShutdownGuard shutdownGuard;                \
    if (ShutdownGuard::HasShutdownStarted())    \
    {                                           \
        return;                                 \
    }

class Profiler : public ICorProfilerCallback11
{
private:
    std::atomic<int> refCount;
    static ProfilerCallback s_callback; 
    static ManualEvent s_callbackSet;
    

protected:
    static void NotifyManagedCodeViaCallback(ICorProfilerInfo14 *pCorProfilerInfo);

    String GetClassIDName(ClassID classId);
    String GetFunctionIDName(FunctionID funcId);
    String GetModuleIDName(ModuleID modId);

public:
    static Profiler *Instance;
    static void SetCallback(ProfilerCallback callback);

    ICorProfilerInfo14* pCorProfilerInfo;

    Profiler();
    virtual ~Profiler();
    HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
    HRESULT STDMETHODCALLTYPE Shutdown() override;
    HRESULT STDMETHODCALLTYPE AppDomainCreationStarted(AppDomainID appDomainId) override;
    HRESULT STDMETHODCALLTYPE AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE AppDomainShutdownStarted(AppDomainID appDomainId) override;
    HRESULT STDMETHODCALLTYPE AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE AssemblyLoadStarted(AssemblyID assemblyId) override;
    HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE AssemblyUnloadStarted(AssemblyID assemblyId) override;
    HRESULT STDMETHODCALLTYPE AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE ModuleLoadStarted(ModuleID moduleId) override;
    HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(ModuleID moduleId) override;
    HRESULT STDMETHODCALLTYPE ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId) override;
    HRESULT STDMETHODCALLTYPE ClassLoadStarted(ClassID classId) override;
    HRESULT STDMETHODCALLTYPE ClassLoadFinished(ClassID classId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE ClassUnloadStarted(ClassID classId) override;
    HRESULT STDMETHODCALLTYPE ClassUnloadFinished(ClassID classId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE FunctionUnloadStarted(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchStarted(FunctionID functionId, BOOL* pbUseCachedFunction) override;
    HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result) override;
    HRESULT STDMETHODCALLTYPE JITFunctionPitched(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline) override;
    HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override;
    HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
    HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;
    HRESULT STDMETHODCALLTYPE RemotingClientInvocationStarted() override;
    HRESULT STDMETHODCALLTYPE RemotingClientSendingMessage(GUID* pCookie, BOOL fIsAsync) override;
    HRESULT STDMETHODCALLTYPE RemotingClientReceivingReply(GUID* pCookie, BOOL fIsAsync) override;
    HRESULT STDMETHODCALLTYPE RemotingClientInvocationFinished() override;
    HRESULT STDMETHODCALLTYPE RemotingServerReceivingMessage(GUID* pCookie, BOOL fIsAsync) override;
    HRESULT STDMETHODCALLTYPE RemotingServerInvocationStarted() override;
    HRESULT STDMETHODCALLTYPE RemotingServerInvocationReturned() override;
    HRESULT STDMETHODCALLTYPE RemotingServerSendingReply(GUID* pCookie, BOOL fIsAsync) override;
    HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
    HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
    HRESULT STDMETHODCALLTYPE RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason) override;
    HRESULT STDMETHODCALLTYPE RuntimeSuspendFinished() override;
    HRESULT STDMETHODCALLTYPE RuntimeSuspendAborted() override;
    HRESULT STDMETHODCALLTYPE RuntimeResumeStarted() override;
    HRESULT STDMETHODCALLTYPE RuntimeResumeFinished() override;
    HRESULT STDMETHODCALLTYPE RuntimeThreadSuspended(ThreadID threadId) override;
    HRESULT STDMETHODCALLTYPE RuntimeThreadResumed(ThreadID threadId) override;
    HRESULT STDMETHODCALLTYPE MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[]) override;
    HRESULT STDMETHODCALLTYPE ObjectAllocated(ObjectID objectId, ClassID classId) override;
    HRESULT STDMETHODCALLTYPE ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[]) override;
    HRESULT STDMETHODCALLTYPE ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[]) override;
    HRESULT STDMETHODCALLTYPE RootReferences(ULONG cRootRefs, ObjectID rootRefIds[]) override;
    HRESULT STDMETHODCALLTYPE ExceptionThrown(ObjectID thrownObjectId) override;
    HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionEnter(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionLeave() override;
    HRESULT STDMETHODCALLTYPE ExceptionSearchFilterEnter(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE ExceptionSearchFilterLeave() override;
    HRESULT STDMETHODCALLTYPE ExceptionSearchCatcherFound(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE ExceptionOSHandlerEnter(UINT_PTR __unused) override;
    HRESULT STDMETHODCALLTYPE ExceptionOSHandlerLeave(UINT_PTR __unused) override;
    HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionLeave() override;
    HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyEnter(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyLeave() override;
    HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) override;
    HRESULT STDMETHODCALLTYPE ExceptionCatcherLeave() override;
    HRESULT STDMETHODCALLTYPE COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable, ULONG cSlots) override;
    HRESULT STDMETHODCALLTYPE COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable) override;
    HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherFound() override;
    HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherExecute() override;
    HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
    HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
    HRESULT STDMETHODCALLTYPE SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[]) override;
    HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;
    HRESULT STDMETHODCALLTYPE FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID) override;
    HRESULT STDMETHODCALLTYPE RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[]) override;
    HRESULT STDMETHODCALLTYPE HandleCreated(GCHandleID handleId, ObjectID initialObjectId) override;
    HRESULT STDMETHODCALLTYPE HandleDestroyed(GCHandleID handleId) override;
    HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData) override;
    HRESULT STDMETHODCALLTYPE ProfilerAttachComplete() override;
    HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded() override;
    HRESULT STDMETHODCALLTYPE ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl) override;
    HRESULT STDMETHODCALLTYPE ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
    HRESULT STDMETHODCALLTYPE SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
    HRESULT STDMETHODCALLTYPE ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[]) override;
    HRESULT STDMETHODCALLTYPE GetAssemblyReferences(const WCHAR* wszAssemblyPath, ICorProfilerAssemblyReferenceProvider* pAsmRefProvider) override;
    HRESULT STDMETHODCALLTYPE ModuleInMemorySymbolsUpdated(ModuleID moduleId) override;
    HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader) override;
    HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    HRESULT STDMETHODCALLTYPE DynamicMethodUnloaded(FunctionID functionId) override;
    HRESULT STDMETHODCALLTYPE EventPipeEventDelivered(
        EVENTPIPE_PROVIDER provider,
        DWORD eventId,
        DWORD eventVersion,
        ULONG cbMetadataBlob,
        LPCBYTE metadataBlob,
        ULONG cbEventData,
        LPCBYTE eventData,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        ThreadID eventThread,
        ULONG numStackFrames,
        UINT_PTR stackFrames[]) override;
    HRESULT STDMETHODCALLTYPE EventPipeProviderCreated(EVENTPIPE_PROVIDER provider) override;
    HRESULT STDMETHODCALLTYPE LoadAsNotificationOnly(BOOL *pbNotificationOnly) override;

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject) override;
    ULONG STDMETHODCALLTYPE AddRef(void) override;
    ULONG STDMETHODCALLTYPE Release(void) override;
};
