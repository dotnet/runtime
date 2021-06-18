// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ProfilePriv.h
//

//
// Structures, etc. used by the Profiling API and throughout the EE
//

// ======================================================================================

#ifndef _ProfilePriv_h_
#define _ProfilePriv_h_


// Forward declarations
class EEToProfInterfaceImpl;
class ProfToEEInterfaceImpl;
class Object;
struct ScanContext;
enum EtwGCRootFlags: int32_t;
enum EtwGCRootKind: int32_t;
struct IAssemblyBindingClosure;
struct AssemblyReferenceClosureWalkContextForProfAPI;

#include "eventpipeadaptertypes.h"

#if defined (PROFILING_SUPPORTED_DATA) || defined(PROFILING_SUPPORTED)
#ifndef PROFILING_SUPPORTED_DATA
#define PROFILING_SUPPORTED_DATA 1
#endif  // PROFILING_SUPPORTED_DATA

#include "corprof.h"
#include "slist.h"

#define MAX_NOTIFICATION_PROFILERS 32

//---------------------------------------------------------------------------------------
// Enumerates the various init states of profiling.
//
// *** NOTE: The order is important here, as some of the status checks (e.g.,
// CORProfilerPresentOrInitializing) use ">" with these enum values. ***

enum ProfilerStatus
{
    kProfStatusNone                        = 0, // No profiler running.
    kProfStatusDetaching                   = 1, // Prof was running, is now detaching, but still loaded
    kProfStatusInitializingForStartupLoad  = 2, // Prof ready for (or in) its Initialize callback
    kProfStatusInitializingForAttachLoad   = 3, // Prof ready for (or in) its InitializeForAttach callback
    kProfStatusActive                      = 4, // Prof completed initialization and is actively running
    kProfStatusPreInitialize               = 5, // Prof is in LoadProfiler, but initialization has yet to occur
};

class CurrentProfilerStatus
{
private:
    // Why volatile?
    // See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
    Volatile<ProfilerStatus> m_profStatus;

public:
    void Init();
    ProfilerStatus Get();
    void Set(ProfilerStatus profStatus);
};

class EventMask
{
    friend class ProfControlBlock;
private:
    const UINT64 EventMaskLowMask           = 0x00000000FFFFFFFF;
    const UINT64 EventMaskHighShiftAmount   = 32;
    const UINT64 EventMaskHighMask          = 0xFFFFFFFF00000000;

    Volatile<UINT64> m_eventMask;

public:
    EventMask() :
        m_eventMask(0)
    {

    }

    EventMask& operator=(const EventMask& other);

    BOOL IsEventMaskSet(DWORD eventMask);
    DWORD GetEventMask();
    void SetEventMask(DWORD eventMask);
    BOOL IsEventMaskHighSet(DWORD eventMaskHigh);
    DWORD GetEventMaskHigh();
    void SetEventMaskHigh(DWORD eventMaskHigh);
};

class ProfilerInfo
{
public:
    // **** IMPORTANT!! ****
    // All uses of pProfInterface must be properly synchronized to avoid the profiler
    // from detaching while the EE attempts to call into it.  The recommended way to do
    // this is to use the (lockless) BEGIN_PROFILER_CALLBACK / END_PROFILER_CALLBACK macros.  See
    // code:BEGIN_PROFILER_CALLBACK for instructions.  For full details on how the
    // synchronization works, see
    // code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
    VolatilePtr<EEToProfInterfaceImpl> pProfInterface;
    // **** IMPORTANT!! ****

    CurrentProfilerStatus curProfStatus;
    
    EventMask eventMask;

    //---------------------------------------------------------------
    // m_dwProfilerEvacuationCounter keeps track of how many profiler
    // callback calls remain on the stack
    //---------------------------------------------------------------
    // Why volatile?
    // See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization.
    Volatile<DWORD> dwProfilerEvacuationCounter;

    Volatile<BOOL> inUse;

    // Reset those variables that is only for the current attach session
    void ResetPerSessionStatus();
    void Init();
};

enum class ProfilerCallbackType
{
    Active,
    ActiveOrInitializing
};

// We need a way to track which profilers are in active calls, to synchronize with detach.
// If we detached a profiler while it was actively in a callback there would be issues.
// However, we don't want to pin all profilers, because then a chatty profiler could
// cause another profiler to not be able to detach. We can't just check the event masks
// before and after the call because it is legal for a profiler to change its event mask,
// and then it would be possible for a profiler to permanently prevent itself from detaching.
// 
// WHEN IS EvacuationCounterHolder REQUIRED?
// Answer: any time you access a ProfilerInfo *. There is a specific sequence that must be followed:
//   - Do a dirty read of the Profiler interface
//   - Increment an evacuation counter by using EvacuationCounterHolder as a RAII guard class
//   - Now do a clean read of the ProfilerInfo's status - this will be changed during detach and
//     is always read with a memory barrier
//
// The DoProfilerCallback/IterateProfilers functions automate this process for you, you should use
// them unless you are absoultely sure you know what you're doing
class EvacuationCounterHolder
{
private:
    ProfilerInfo *m_pProfilerInfo;

public:
    EvacuationCounterHolder(ProfilerInfo *pProfilerInfo) :
        m_pProfilerInfo(pProfilerInfo)
    {
        _ASSERTE(m_pProfilerInfo != NULL);
        InterlockedIncrement((LONG *)(m_pProfilerInfo->dwProfilerEvacuationCounter.GetPointer()));
    }

    ~EvacuationCounterHolder()
    {
        InterlockedDecrement((LONG *)(m_pProfilerInfo->dwProfilerEvacuationCounter.GetPointer()));
    }
};

struct StoredProfilerNode
{
    CLSID guid;
    SString path;
    SLink m_Link;
};

typedef SList<StoredProfilerNode, true> STOREDPROFILERLIST;
// ---------------------------------------------------------------------------------------
// Global struct that lets the EE see the load status of the profiler, and provides a
// pointer (pProfInterface) through which profiler calls can be made
//
// When you are adding new session, please refer to
// code:ProfControlBlock::ResetPerSessionStatus#ProfileResetSessionStatus for more details.
class ProfControlBlock
{
private:
    // IsProfilerPresent(pProfilerInfo) returns whether or not a CLR Profiler is actively loaded
    // (meaning it's initialized and ready to receive callbacks).
    FORCEINLINE BOOL IsProfilerPresent(ProfilerInfo *pProfilerInfo)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return pProfilerInfo->curProfStatus.Get() >= kProfStatusActive;
    }

    FORCEINLINE BOOL IsProfilerPresentOrInitializing(ProfilerInfo *pProfilerInfo)
    {
        return pProfilerInfo->curProfStatus.Get() > kProfStatusDetaching;
    }

    template<typename Func, typename... Args>
    FORCEINLINE VOID DoOneProfilerIteration(ProfilerInfo *pProfilerInfo, ProfilerCallbackType callbackType, Func callback, Args... args)
    {
        // This is the dirty read
        if (pProfilerInfo->pProfInterface.Load() != NULL)
        {
#ifdef FEATURE_PROFAPI_ATTACH_DETACH
            // Now indicate we are accessing the profiler
            EvacuationCounterHolder evacuationCounter(pProfilerInfo);
#endif // FEATURE_PROFAPI_ATTACH_DETACH
            
            if ((callbackType == ProfilerCallbackType::Active && IsProfilerPresent(pProfilerInfo))
                || (callbackType == ProfilerCallbackType::ActiveOrInitializing && IsProfilerPresentOrInitializing(pProfilerInfo)))
            {
                callback(pProfilerInfo, args...);
            }
        }
    }

    template<typename Func, typename... Args>
    FORCEINLINE VOID IterateProfilers(ProfilerCallbackType callbackType, Func callback, Args... args)
    {
        DoOneProfilerIteration(&mainProfilerInfo, callbackType, callback, args...);
        
        if (notificationProfilerCount > 0)
        {
            for (SIZE_T i = 0; i < MAX_NOTIFICATION_PROFILERS; ++i)
            {
                ProfilerInfo *current = &(notificationOnlyProfilers[i]);
                DoOneProfilerIteration(current, callbackType, callback, args...);
            }
        }
    }

public:
    BOOL fGCInProgress;
    BOOL fBaseSystemClassesLoaded;

    STOREDPROFILERLIST storedProfilers;

    ProfilerInfo mainProfilerInfo;

    ProfilerInfo notificationOnlyProfilers[MAX_NOTIFICATION_PROFILERS];
    Volatile<LONG> notificationProfilerCount;

    EventMask globalEventMask;

#ifdef PROF_TEST_ONLY_FORCE_ELT_DATA
    // #TestOnlyELT This implements a test-only (and debug-only) hook that allows a test
    // profiler to ensure enter/leave/tailcall is enabled on startup even though no
    // profiler is loaded on startup. This allows an attach profiler to use ELT to build
    // shadow stacks for the sole purpose of verifying OTHER areas of the profiling API
    // (e.g., stack walking). When this BOOL is TRUE, the JIT will insert calls to the
    // slow-path profiling API enter/leave/tailcall hooks, which will forward the call to
    // a profiler if one is loaded (and do nothing otherwise).
    //
    // See code:AreCallbackStateFlagsSet#P2CLRRestrictionsOverview for general information
    // on how the test hooks lift restrictions normally in place for the Info functions.
    BOOL fTestOnlyForceEnterLeave;
#endif

#ifdef PROF_TEST_ONLY_FORCE_OBJECT_ALLOCATED_DATA
    // #TestOnlyObjectAllocated This implements a test-only (and debug-only) hook that allows
    // a test profiler to ensure ObjectAllocated callback is enabled on startup even though no
    // profiler is loaded on startup. This allows an attach profiler to use ObjectAllocated
    // callback for the sole purpose of verifying OTHER GC areas of the profiling API
    // (e.g., constructing a object graph). When this BOOL is TRUE, the JIT will use special
    // version of new allocators that issue object allocation notifications, which will forward
    // the notifications to a profiler if one is loaded (and do nothing otherwise).
    //
    // See code:AreCallbackStateFlagsSet#P2CLRRestrictionsOverview for general information
    // on how the test hooks lift restrictions normally in place for the Info functions.
    BOOL fTestOnlyForceObjectAllocated;
#endif

#ifdef _DEBUG
    // Test-only, debug-only code to allow attaching profilers to call ICorProfilerInfo inteface,
    // which would otherwise be disallowed for attaching profilers
    BOOL                    fTestOnlyEnableICorProfilerInfo;
#endif // _DEBUG

    // Whether we've turned off concurrent GC during attach
    Volatile<BOOL> fConcurrentGCDisabledForAttach;

    Volatile<BOOL> fProfControlBlockInitialized;

    Volatile<BOOL> fProfilerRequestedRuntimeSuspend;

    void Init();
    BOOL IsMainProfiler(EEToProfInterfaceImpl *pEEToProf);
    BOOL IsMainProfiler(ProfToEEInterfaceImpl *pProfToEE);
    ProfilerInfo *GetProfilerInfo(ProfToEEInterfaceImpl *pProfToEE);

    template<typename ConditionFunc, typename CallbackFunc, typename Data = void, typename... Args>
    FORCEINLINE HRESULT DoProfilerCallback(ProfilerCallbackType callbackType, ConditionFunc condition, Data *additionalData, CallbackFunc callback, Args... args)
    {
        HRESULT hr = S_OK;
        IterateProfilers(callbackType,
                         [](ProfilerInfo *pProfilerInfo, ConditionFunc condition, Data *additionalData, CallbackFunc callback, HRESULT *pHR, Args... args)
                            {
                                if (condition(pProfilerInfo))
                                {
                                    HRESULT innerHR = callback(additionalData, pProfilerInfo->pProfInterface, args...);
                                    if (FAILED(innerHR))
                                    {
                                        *pHR = innerHR;
                                    }
                                }
                            },
                         condition, additionalData, callback, &hr, args...);
        return hr;
    }

#ifndef DACCESS_COMPILE
    ProfilerInfo *FindNextFreeProfilerInfoSlot();
    void DeRegisterProfilerInfo(ProfilerInfo *pProfilerInfo);
    void UpdateGlobalEventMask();
#endif // DACCESS_COMPILE

    BOOL IsCallback3Supported();
    BOOL IsCallback5Supported();
    BOOL IsDisableTransparencySet();
    BOOL RequiresGenericsContextForEnterLeave();
    UINT_PTR EEFunctionIDMapper(FunctionID funcId, BOOL * pbHookFunction);
    
    void ThreadCreated(ThreadID threadID);
    void ThreadDestroyed(ThreadID threadID);
    void ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId);
    void ThreadNameChanged(ThreadID managedThreadId, ULONG cchName, WCHAR name[]);
    void Shutdown();
    void FunctionUnloadStarted(FunctionID functionId);
    void JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock);
    void JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock);
    void DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE pILHeader, ULONG cbILHeader);
    void DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock);
    void DynamicMethodUnloaded(FunctionID functionId);
    void JITCachedFunctionSearchStarted(FunctionID functionId, BOOL *pbUseCachedFunction);
    void JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result);
    HRESULT JITInlining(FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline);
    void ReJITCompilationStarted(FunctionID functionId, ReJITID reJitId, BOOL fIsSafeToBlock);
    HRESULT GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl);
    void ReJITCompilationFinished(FunctionID functionId, ReJITID reJitId, HRESULT hrStatus, BOOL fIsSafeToBlock);
    void ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus);
    void ModuleLoadStarted(ModuleID moduleId);
    void ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus);
    void ModuleUnloadStarted(ModuleID moduleId);
    void ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus);
    void ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId);
    void ModuleInMemorySymbolsUpdated(ModuleID moduleId);
    void ClassLoadStarted(ClassID classId);
    void ClassLoadFinished(ClassID classId, HRESULT hrStatus);
    void ClassUnloadStarted(ClassID classId);
    void ClassUnloadFinished(ClassID classId, HRESULT hrStatus);
    void AppDomainCreationStarted(AppDomainID appDomainId);
    void AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus);
    void AppDomainShutdownStarted(AppDomainID appDomainId);
    void AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus);
    void AssemblyLoadStarted(AssemblyID assemblyId);
    void AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus);
    void AssemblyUnloadStarted(AssemblyID assemblyId);
    void AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus);
    void UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason);
    void ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason);
    void ExceptionThrown(ObjectID thrownObjectId);
    void ExceptionSearchFunctionEnter(FunctionID functionId);
    void ExceptionSearchFunctionLeave();
    void ExceptionSearchFilterEnter(FunctionID funcId);
    void ExceptionSearchFilterLeave();
    void ExceptionSearchCatcherFound(FunctionID functionId);
    void ExceptionOSHandlerEnter(FunctionID funcId);
    void ExceptionOSHandlerLeave(FunctionID funcId);
    void ExceptionUnwindFunctionEnter(FunctionID functionId);
    void ExceptionUnwindFunctionLeave();
    void ExceptionUnwindFinallyEnter(FunctionID functionId);
    void ExceptionUnwindFinallyLeave();
    void ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId);
    void ExceptionCatcherLeave();
    void COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots);
    void RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason);
    void RuntimeSuspendFinished();
    void RuntimeSuspendAborted();
    void RuntimeResumeStarted();
    void RuntimeResumeFinished();
    void RuntimeThreadSuspended(ThreadID suspendedThreadId);
    void RuntimeThreadResumed(ThreadID resumedThreadId);
    void ObjectAllocated(ObjectID objectId, ClassID classId);
    void FinalizeableObjectQueued(BOOL isCritical, ObjectID objectID);
    void MovedReference(BYTE *pbMemBlockStart, BYTE *pbMemBlockEnd, ptrdiff_t cbRelocDistance, void *pHeapId, BOOL fCompacting);
    void EndMovedReferences(void *pHeapId);
    void RootReference2(BYTE *objectId, EtwGCRootKind dwEtwRootKind, EtwGCRootFlags dwEtwRootFlags, void *rootID, void *pHeapId);
    void EndRootReferences2(void *pHeapId);
    void ConditionalWeakTableElementReference(BYTE *primaryObjectId, BYTE *secondaryObjectId, void *rootID, void *pHeapId);
    void EndConditionalWeakTableElementReferences(void *pHeapId);
    void AllocByClass(ObjectID objId, ClassID classId, void *pHeapId);
    void EndAllocByClass(void *pHeapId);
    HRESULT ObjectReference(ObjectID objId, ClassID classId, ULONG cNumRefs, ObjectID *arrObjRef);
    void HandleCreated(UINT_PTR handleId, ObjectID initialObjectId);
    void HandleDestroyed(UINT_PTR handleId);
    void GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);
    void GarbageCollectionFinished();
    void GetAssemblyReferences(LPCWSTR wszAssemblyPath, IAssemblyBindingClosure *pClosure, AssemblyReferenceClosureWalkContextForProfAPI *pContext);
    void EventPipeEventDelivered(EventPipeProvider *provider, DWORD eventId, DWORD eventVersion, ULONG cbMetadataBlob, LPCBYTE metadataBlob, ULONG cbEventData, 
                                 LPCBYTE eventData, LPCGUID pActivityId, LPCGUID pRelatedActivityId, Thread *pEventThread, ULONG numStackFrames, UINT_PTR stackFrames[]);
    void EventPipeProviderCreated(EventPipeProvider *provider);
};


GVAL_DECL(ProfControlBlock, g_profControlBlock);

// Provides definitions of the CORProfilerTrack* functions that test whether a profiler
// is active and responding to various callbacks
#include "profilepriv.inl"

//---------------------------------------------------------------
// Bit flags used to track profiler callback execution state, such as which
// ICorProfilerCallback method we're currently executing. These help us enforce the
// invariants of which calls a profiler is allowed to make at given times. These flags
// are stored in Thread::m_profilerCallbackState.
//
// For now, we ensure:
//     * Only asynchronous-safe calls are made asynchronously (i.e., are made from
//         outside of profiler callbacks).
//     * GC_TRIGGERS info methods are not called from GC_NOTRIGGER callbacks
//
// Later, we may choose to enforce even more refined call trees and add more flags.
#define COR_PRF_CALLBACKSTATE_INCALLBACK                 0x1
#define COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE          0x2
#define COR_PRF_CALLBACKSTATE_FORCEGC_WAS_CALLED         0x4
#define COR_PRF_CALLBACKSTATE_REJIT_WAS_CALLED           0x8
//
//---------------------------------------------------------------

#endif // defined(PROFILING_SUPPORTED_DATA) || defined(PROFILING_SUPPORTED)

// This is the helper callback that the gc uses when walking the heap.
bool HeapWalkHelper(Object* pBO, void* pv);
void ScanRootsHelper(Object* pObj, Object** ppRoot, ScanContext *pSC, uint32_t dwUnused);
bool AllocByClassHelper(Object* pBO, void* pv);

#endif  // _ProfilePriv_h_

