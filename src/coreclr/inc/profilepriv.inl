// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ProfilePriv.inl
//

//
// Inlined functions used by the Profiling API and throughout the EE. Most notably are
// the CORProfilerTrack* functions that test whether a profiler is active and responding
// to various callbacks
//

// ======================================================================================
#ifndef _ProfilePriv_inl_
#define _ProfilePriv_inl_

#include "eetoprofinterfaceimpl.h"
#ifdef PROFILING_SUPPORTED
#include "profilinghelper.h"
#endif // PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
// CurrentProfilerStatus
//---------------------------------------------------------------------------------------

inline void CurrentProfilerStatus::Init()
{
    LIMITED_METHOD_CONTRACT;
    m_profStatus = kProfStatusNone;
}

inline ProfilerStatus CurrentProfilerStatus::Get()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_profStatus;
}

inline EventMask& EventMask::operator=(const EventMask& other)
{
    m_eventMask = other.m_eventMask;
    return *this;
}

inline BOOL EventMask::IsEventMaskSet(DWORD eventMask)
{
    return (GetEventMask() & eventMask) != 0;
}

inline DWORD EventMask::GetEventMask()
{
    return (DWORD)(m_eventMask & EventMaskLowMask);
}

inline void EventMask::SetEventMask(DWORD eventMask)
{
    m_eventMask = (m_eventMask & EventMaskHighMask) | (UINT64)eventMask;
}

inline BOOL EventMask::IsEventMaskHighSet(DWORD eventMaskHigh)
{
    return (GetEventMaskHigh() & eventMaskHigh) != 0;
}

inline DWORD EventMask::GetEventMaskHigh()
{
    return (DWORD)((m_eventMask & EventMaskHighMask) >> EventMaskHighShiftAmount);
}

inline void EventMask::SetEventMaskHigh(DWORD eventMaskHigh)
{
    m_eventMask = (m_eventMask & EventMaskLowMask) | ((UINT64)eventMaskHigh << EventMaskHighShiftAmount);
}

// Reset those variables that is only for the current attach session
inline void ProfilerInfo::ResetPerSessionStatus()
{
    LIMITED_METHOD_CONTRACT;

    pProfInterface = NULL;
    eventMask.SetEventMask(COR_PRF_MONITOR_NONE);
    eventMask.SetEventMaskHigh(COR_PRF_HIGH_MONITOR_NONE);
}

inline void ProfilerInfo::Init()
{
    curProfStatus.Init();
    ResetPerSessionStatus();
    inUse = FALSE;
}

inline HRESULT AnyProfilerPassesConditionHelper(EEToProfInterfaceImpl *profInterface, BOOL *pAnyPassed)
{
    *pAnyPassed = TRUE;
    return S_OK;
}

template<typename ConditionFunc>
FORCEINLINE BOOL AnyProfilerPassesCondition(ConditionFunc condition)
{
    BOOL anyPassed = FALSE;
    (&g_profControlBlock)->DoProfilerCallback(ProfilerCallbackType::ActiveOrInitializing,
                                              condition,
                                              &AnyProfilerPassesConditionHelper,
                                              &anyPassed);

    return anyPassed;
}

//---------------------------------------------------------------------------------------
// ProfControlBlock
//---------------------------------------------------------------------------------------

inline void ProfControlBlock::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    mainProfilerInfo.Init();
    // Special magic value for the main one
    mainProfilerInfo.slot = MAX_NOTIFICATION_PROFILERS;

    for (SIZE_T i = 0; i < MAX_NOTIFICATION_PROFILERS; ++i)
    {
        notificationOnlyProfilers[i].Init();
        notificationOnlyProfilers[i].slot = (DWORD)i;
    }

    globalEventMask.SetEventMask(COR_PRF_MONITOR_NONE);
    globalEventMask.SetEventMaskHigh(COR_PRF_HIGH_MONITOR_NONE);

    fGCInProgress = FALSE;
    fBaseSystemClassesLoaded = FALSE;
#ifdef PROF_TEST_ONLY_FORCE_ELT
    fTestOnlyForceEnterLeave = FALSE;
#endif

#ifdef PROF_TEST_ONLY_FORCE_OBJECT_ALLOCATED
    fTestOnlyForceObjectAllocated = FALSE;
#endif

#ifdef _DEBUG
    fTestOnlyEnableICorProfilerInfo = FALSE;
#endif // _DEBUG

    fConcurrentGCDisabledForAttach = FALSE;

    mainProfilerInfo.ResetPerSessionStatus();

    fProfControlBlockInitialized = TRUE;

    fProfilerRequestedRuntimeSuspend = FALSE;
}


inline BOOL ProfControlBlock::IsMainProfiler(EEToProfInterfaceImpl *pEEToProf)
{
    EEToProfInterfaceImpl *pProfInterface = mainProfilerInfo.pProfInterface.Load();
    return pProfInterface == pEEToProf;
}

inline BOOL ProfControlBlock::IsMainProfiler(ProfToEEInterfaceImpl *pProfToEE)
{
    EEToProfInterfaceImpl *pProfInterface = mainProfilerInfo.pProfInterface.Load();
    return pProfInterface != NULL && pProfInterface->m_pProfToEE == pProfToEE;
}

inline void GetProfilerInfoHelper(ProfilerInfo *pProfilerInfo, ProfToEEInterfaceImpl *pProfToEE, ProfilerInfo **ppFoundProfilerInfo)
{
    if (pProfilerInfo->pProfInterface->GetProfToEE() == pProfToEE)
    {
        *ppFoundProfilerInfo = pProfilerInfo;
    }
}

inline ProfilerInfo *ProfControlBlock::GetProfilerInfo(ProfToEEInterfaceImpl *pProfToEE)
{
    ProfilerInfo *pProfilerInfo = NULL;
    IterateProfilers(ProfilerCallbackType::ActiveOrInitializing,
                      &GetProfilerInfoHelper,
                      pProfToEE,
                      &pProfilerInfo);

    return pProfilerInfo;
}

#ifndef DACCESS_COMPILE
inline ProfilerInfo *ProfControlBlock::FindNextFreeProfilerInfoSlot()
{
    for (SIZE_T i = 0; i < MAX_NOTIFICATION_PROFILERS; ++i)
    {
        if (InterlockedCompareExchange((LONG *)notificationOnlyProfilers[i].inUse.GetPointer(), TRUE, FALSE) == FALSE)
        {
            InterlockedIncrement(notificationProfilerCount.GetPointer());
            return &(notificationOnlyProfilers[i]);
        }
    }

    return NULL;
}

inline void ProfControlBlock::DeRegisterProfilerInfo(ProfilerInfo *pProfilerInfo)
{
    pProfilerInfo->inUse = FALSE;
    InterlockedDecrement(notificationProfilerCount.GetPointer());
}

inline void UpdateGlobalEventMaskHelper(ProfilerInfo *pProfilerInfo, DWORD *pEventMask)
{
    *pEventMask |= pProfilerInfo->eventMask.GetEventMask();
}

inline void UpdateGlobalEventMaskHighHelper(ProfilerInfo *pProfilerInfo, DWORD *pEventMaskHigh)
{
    *pEventMaskHigh |= pProfilerInfo->eventMask.GetEventMaskHigh();
}

inline void ProfControlBlock::UpdateGlobalEventMask()
{
    while (true)
    {
        UINT64 originalEventMask = globalEventMask.m_eventMask;
        UINT64 qwEventMask = 0;

        IterateProfilers(ProfilerCallbackType::ActiveOrInitializing,
                        [](ProfilerInfo *pProfilerInfo, UINT64 *pEventMask)
                          {
                              *pEventMask |= pProfilerInfo->eventMask.m_eventMask;
                          },
                          &qwEventMask);

        // We are relying on the memory barrier introduced by InterlockedCompareExchange64 to observer any
        // change to the global event mask.
        if ((UINT64)InterlockedCompareExchange64((LONG64 *)&(globalEventMask.m_eventMask), (LONG64)qwEventMask, (LONG64)originalEventMask) == originalEventMask)
        {
            break;
        }
    }
}
#endif // DACCESS_COMPILE

inline BOOL IsCallback3SupportedHelper(ProfilerInfo *pProfilerInfo)
{
    return pProfilerInfo->pProfInterface->IsCallback3Supported(); 
}

FORCEINLINE BOOL ProfControlBlock::IsCallback3Supported()
{
    return AnyProfilerPassesCondition(&IsCallback3SupportedHelper);
}

inline BOOL IsCallback5SupportedHelper(ProfilerInfo *pProfilerInfo)
{
    return pProfilerInfo->pProfInterface->IsCallback5Supported();
}

FORCEINLINE BOOL ProfControlBlock::IsCallback5Supported()
{
    return AnyProfilerPassesCondition(&IsCallback5SupportedHelper);
}

inline BOOL RequiresGenericsContextForEnterLeaveHelper(ProfilerInfo *pProfilerInfo)
{
    return pProfilerInfo->pProfInterface->RequiresGenericsContextForEnterLeave();
}

FORCEINLINE BOOL ProfControlBlock::RequiresGenericsContextForEnterLeave()
{
    return AnyProfilerPassesCondition(&RequiresGenericsContextForEnterLeaveHelper); 
}

inline bool DoesProfilerWantEEFunctionIDMapper(ProfilerInfo *pProfilerInfo)
{
    return ((pProfilerInfo->pProfInterface->GetFunctionIDMapper()  != NULL) ||
             (pProfilerInfo->pProfInterface->GetFunctionIDMapper2() != NULL));
}

inline void EEFunctionIDMapperHelper(ProfilerInfo *pProfilerInfo, FunctionID funcId, BOOL *pbHookFunction, UINT_PTR *pPtr)
{
   if (DoesProfilerWantEEFunctionIDMapper(pProfilerInfo))
   {
       *pPtr = pProfilerInfo->pProfInterface->EEFunctionIDMapper(funcId, pbHookFunction);
   }
}

inline UINT_PTR ProfControlBlock::EEFunctionIDMapper(FunctionID funcId, BOOL *pbHookFunction)
{
    LIMITED_METHOD_CONTRACT;
    UINT_PTR ptr = NULL;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          &EEFunctionIDMapperHelper,
                          funcId, pbHookFunction, &ptr);

    return ptr;
}

inline BOOL IsProfilerTrackingThreads(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_THREADS);
}

inline HRESULT ThreadCreatedHelper(EEToProfInterfaceImpl *profInterface, ThreadID threadID)
{
    return profInterface->ThreadCreated(threadID);
}

inline void ProfControlBlock::ThreadCreated(ThreadID threadID)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       &ThreadCreatedHelper,
                       threadID);
}

inline HRESULT ThreadDestroyedHelper(EEToProfInterfaceImpl *profInterface, ThreadID threadID)
{
    return profInterface->ThreadDestroyed(threadID);
}

inline void ProfControlBlock::ThreadDestroyed(ThreadID threadID)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       &ThreadDestroyedHelper,
                       threadID);
}

inline HRESULT ThreadAssignedToOSThreadHelper(EEToProfInterfaceImpl *profInterface, ThreadID managedThreadId, DWORD osThreadId)
{
    return profInterface->ThreadAssignedToOSThread(managedThreadId, osThreadId);
}

inline void ProfControlBlock::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       &ThreadAssignedToOSThreadHelper,
                       managedThreadId, osThreadId);
}

inline HRESULT ThreadNameChangedHelper(EEToProfInterfaceImpl *profInterface, ThreadID managedThreadId, ULONG cchName, WCHAR name[])
{
    return profInterface->ThreadNameChanged(managedThreadId, cchName, name);
}

inline void ProfControlBlock::ThreadNameChanged(ThreadID managedThreadId, ULONG cchName, WCHAR name[])
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       &ThreadNameChangedHelper,
                       managedThreadId, cchName, name);
}

inline HRESULT ShutdownHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->Shutdown();
}

inline void ProfControlBlock::Shutdown()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       [](ProfilerInfo *pProfilerInfo) { return true; },
                       &ShutdownHelper);
}

inline BOOL IsProfilerTrackingJITInfo(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_JIT_COMPILATION);
}

inline HRESULT JITCompilationFinishedHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return profInterface->JITCompilationFinished(functionId, hrStatus, fIsSafeToBlock);
}

inline void ProfControlBlock::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       &JITCompilationFinishedHelper,
                       functionId, hrStatus, fIsSafeToBlock);
}

inline HRESULT JITCompilationStartedHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, BOOL fIsSafeToBlock)
{
    return profInterface->JITCompilationStarted(functionId, fIsSafeToBlock);
}

inline void ProfControlBlock::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       &JITCompilationStartedHelper,
                       functionId, fIsSafeToBlock);
}

inline HRESULT DynamicMethodJITCompilationStartedHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE pILHeader, ULONG cbILHeader)
{
    return profInterface->DynamicMethodJITCompilationStarted(functionId, fIsSafeToBlock, pILHeader, cbILHeader);
}

inline void ProfControlBlock::DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE pILHeader, ULONG cbILHeader)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       &DynamicMethodJITCompilationStartedHelper,
                       functionId, fIsSafeToBlock, pILHeader, cbILHeader);
}

inline HRESULT DynamicMethodJITCompilationFinishedHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return profInterface->DynamicMethodJITCompilationFinished(functionId, hrStatus, fIsSafeToBlock);
}

inline void ProfControlBlock::DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       &DynamicMethodJITCompilationFinishedHelper,
                       functionId, hrStatus, fIsSafeToBlock);
}

inline BOOL IsProfilerMonitoringDynamicFunctionUnloads(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS);
}

inline HRESULT DynamicMethodUnloadedHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId)
{
    return profInterface->DynamicMethodUnloaded(functionId);
}

inline void ProfControlBlock::DynamicMethodUnloaded(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerMonitoringDynamicFunctionUnloads,
                       &DynamicMethodUnloadedHelper,
                       functionId);
}

inline BOOL IsProfilerTrackingCacheSearches(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_CACHE_SEARCHES);
}

inline HRESULT JITCachedFunctionSearchStartedHelper(EEToProfInterfaceImpl *profInterface, BOOL *pAllTrue, FunctionID functionId)
{
    BOOL fUseCachedFunction = TRUE;
    HRESULT hr = profInterface->JITCachedFunctionSearchStarted(functionId, &fUseCachedFunction);
    *pAllTrue = *pAllTrue && fUseCachedFunction;
    return hr;
}

inline void ProfControlBlock::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL *pbUseCachedFunction)
{
    LIMITED_METHOD_CONTRACT;

    BOOL allTrue = TRUE;
    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingCacheSearches,
                       &JITCachedFunctionSearchStartedHelper,
                       &allTrue, functionId);

    // If any reject it, consider it rejected.
    *pbUseCachedFunction = allTrue;
}

inline HRESULT JITCachedFunctionSearchFinishedHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    return profInterface->JITCachedFunctionSearchFinished(functionId, result);
}

inline void ProfControlBlock::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingCacheSearches,
                       &JITCachedFunctionSearchFinishedHelper,
                       functionId, result);
}

inline HRESULT JITInliningHelper(EEToProfInterfaceImpl *profInterface, BOOL *pAllTrue, FunctionID callerId, FunctionID calleeId)
{
    BOOL fShouldInline = TRUE;
    HRESULT hr = profInterface->JITInlining(callerId, calleeId, &fShouldInline);
    *pAllTrue = *pAllTrue && fShouldInline;
    return hr;
}

inline HRESULT ProfControlBlock::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline)
{
    LIMITED_METHOD_CONTRACT;

    *pfShouldInline = TRUE;
    BOOL allTrue = TRUE;
    HRESULT hr =  DoProfilerCallback(ProfilerCallbackType::Active,
                                     IsProfilerTrackingJITInfo,
                                     &JITInliningHelper,
                                     &allTrue, callerId, calleeId);

    // If any reject it, consider it rejected.
    *pfShouldInline = allTrue;
    return hr;
}

inline BOOL IsRejitEnabled(ProfilerInfo *pProfilerInfo)
{
    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_ENABLE_REJIT);
}

inline void ReJITCompilationStartedHelper(ProfilerInfo *pProfilerInfo, FunctionID functionId, ReJITID reJitId, BOOL fIsSafeToBlock)
{
    if (IsRejitEnabled(pProfilerInfo))
    {
        pProfilerInfo->pProfInterface->ReJITCompilationStarted(functionId, reJitId, fIsSafeToBlock);
    }
}

inline void ProfControlBlock::ReJITCompilationStarted(FunctionID functionId, ReJITID reJitId, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          &ReJITCompilationStartedHelper,
                          functionId, reJitId, fIsSafeToBlock);
}

inline void GetReJITParametersHelper(ProfilerInfo *pProfilerInfo, ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl, HRESULT *pHr)
{
    if (IsRejitEnabled(pProfilerInfo))
    {
        *pHr = pProfilerInfo->pProfInterface->GetReJITParameters(moduleId, methodId, pFunctionControl);
    }
}

inline HRESULT ProfControlBlock::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          &GetReJITParametersHelper,
                          moduleId, methodId, pFunctionControl, &hr);
    return hr;
}

inline void ReJITCompilationFinishedHelper(ProfilerInfo *pProfilerInfo, FunctionID functionId, ReJITID reJitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    if (IsRejitEnabled(pProfilerInfo))
    {
        pProfilerInfo->pProfInterface->ReJITCompilationFinished(functionId, reJitId, hrStatus, fIsSafeToBlock);
    }
}

inline void ProfControlBlock::ReJITCompilationFinished(FunctionID functionId, ReJITID reJitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          &ReJITCompilationFinishedHelper,
                          functionId, reJitId, hrStatus, fIsSafeToBlock);
}

inline void ReJITErrorHelper(ProfilerInfo *pProfilerInfo, ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    if (IsRejitEnabled(pProfilerInfo))
    {
        pProfilerInfo->pProfInterface->ReJITError(moduleId, methodId, functionId, hrStatus);
    }
}

inline void ProfControlBlock::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          &ReJITErrorHelper,
                          moduleId, methodId, functionId, hrStatus);
}

inline BOOL IsProfilerTrackingModuleLoads(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_MODULE_LOADS);
}

inline HRESULT ModuleLoadStartedHelper(EEToProfInterfaceImpl *profInterface, ModuleID moduleId)
{
    return profInterface->ModuleLoadStarted(moduleId);
}

inline void ProfControlBlock::ModuleLoadStarted(ModuleID moduleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       &ModuleLoadStartedHelper,
                       moduleId);
}

inline HRESULT ModuleLoadFinishedHelper(EEToProfInterfaceImpl *profInterface, ModuleID moduleId, HRESULT hrStatus)
{
    return profInterface->ModuleLoadFinished(moduleId, hrStatus);
}

inline void ProfControlBlock::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       &ModuleLoadFinishedHelper,
                       moduleId, hrStatus);
}

inline HRESULT ModuleUnloadStartedHelper(EEToProfInterfaceImpl *profInterface, ModuleID moduleId)
{
    return profInterface->ModuleUnloadStarted(moduleId);
}

inline void ProfControlBlock::ModuleUnloadStarted(ModuleID moduleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       &ModuleUnloadStartedHelper,
                       moduleId);
}

inline HRESULT ModuleUnloadFinishedHelper(EEToProfInterfaceImpl *profInterface, ModuleID moduleId, HRESULT hrStatus)
{
    return profInterface->ModuleUnloadFinished(moduleId, hrStatus);
}

inline void ProfControlBlock::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       &ModuleUnloadFinishedHelper,
                       moduleId, hrStatus);
}

inline HRESULT ModuleAttachedToAssemblyHelper(EEToProfInterfaceImpl *profInterface, ModuleID moduleId, AssemblyID AssemblyId)
{
    return profInterface->ModuleAttachedToAssembly(moduleId, AssemblyId);
}

inline void ProfControlBlock::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       &ModuleAttachedToAssemblyHelper,
                       moduleId, AssemblyId);
}

inline BOOL IsProfilerTrackingInMemorySymbolsUpdatesEnabled(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskHighSet(COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED);
}

inline HRESULT ModuleInMemorySymbolsUpdatedHelper(EEToProfInterfaceImpl *profInterface, ModuleID moduleId)
{
    return profInterface->ModuleInMemorySymbolsUpdated(moduleId);
}

inline void ProfControlBlock::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingInMemorySymbolsUpdatesEnabled,
                       &ModuleInMemorySymbolsUpdatedHelper,
                       moduleId);
}

inline BOOL IsProfilerTrackingClasses(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_CLASS_LOADS);
}

inline HRESULT ClassLoadStartedHelper(EEToProfInterfaceImpl *profInterface, ClassID classId)
{
    return profInterface->ClassLoadStarted(classId);
}

inline void ProfControlBlock::ClassLoadStarted(ClassID classId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       &ClassLoadStartedHelper,
                       classId);
}

inline HRESULT ClassLoadFinishedHelper(EEToProfInterfaceImpl *profInterface, ClassID classId, HRESULT hrStatus)
{
    return profInterface->ClassLoadFinished(classId, hrStatus);
}

inline void ProfControlBlock::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       &ClassLoadFinishedHelper,
                       classId, hrStatus);
}

inline HRESULT ClassUnloadStartedHelper(EEToProfInterfaceImpl *profInterface, ClassID classId)
{
    return profInterface->ClassUnloadStarted(classId);
}

inline void ProfControlBlock::ClassUnloadStarted(ClassID classId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       &ClassUnloadStartedHelper,
                       classId);
}

inline HRESULT ClassUnloadFinishedHelper(EEToProfInterfaceImpl *profInterface, ClassID classId, HRESULT hrStatus)
{
    return profInterface->ClassUnloadFinished(classId, hrStatus);
}

inline void ProfControlBlock::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       &ClassUnloadFinishedHelper,
                       classId, hrStatus);
}

inline BOOL IsProfilerTrackingAppDomainLoads(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_APPDOMAIN_LOADS);
}

inline HRESULT AppDomainCreationStartedHelper(EEToProfInterfaceImpl *profInterface, AppDomainID appDomainId)
{
    return profInterface->AppDomainCreationStarted(appDomainId);
}

inline void ProfControlBlock::AppDomainCreationStarted(AppDomainID appDomainId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       &AppDomainCreationStartedHelper,
                       appDomainId);
}

inline HRESULT AppDomainCreationFinishedHelper(EEToProfInterfaceImpl *profInterface, AppDomainID appDomainId, HRESULT hrStatus)
{
    return profInterface->AppDomainCreationFinished(appDomainId, hrStatus);
}

inline void ProfControlBlock::AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       &AppDomainCreationFinishedHelper,
                       appDomainId, hrStatus);
}

inline HRESULT AppDomainShutdownStartedHelper(EEToProfInterfaceImpl *profInterface, AppDomainID appDomainId)
{
    return profInterface->AppDomainShutdownStarted(appDomainId);
}

inline void ProfControlBlock::AppDomainShutdownStarted(AppDomainID appDomainId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       &AppDomainShutdownStartedHelper,
                       appDomainId);
}

inline HRESULT AppDomainShutdownFinishedHelper(EEToProfInterfaceImpl *profInterface, AppDomainID appDomainId, HRESULT hrStatus)
{
    return profInterface->AppDomainShutdownFinished(appDomainId, hrStatus);
}

inline void ProfControlBlock::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       &AppDomainShutdownFinishedHelper,
                       appDomainId, hrStatus);
}

inline BOOL IsProfilerTrackingAssemblyLoads(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_ASSEMBLY_LOADS);
}

inline HRESULT AssemblyLoadStartedHelper(EEToProfInterfaceImpl *profInterface, AssemblyID assemblyId)
{
    return profInterface->AssemblyLoadStarted(assemblyId);
}

inline void ProfControlBlock::AssemblyLoadStarted(AssemblyID assemblyId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       &AssemblyLoadStartedHelper,
                       assemblyId);
}

inline HRESULT AssemblyLoadFinishedHelper(EEToProfInterfaceImpl *profInterface, AssemblyID assemblyId, HRESULT hrStatus)
{
    return profInterface->AssemblyLoadFinished(assemblyId, hrStatus);
}

inline void ProfControlBlock::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       &AssemblyLoadFinishedHelper,
                       assemblyId, hrStatus);
}

inline HRESULT AssemblyUnloadStartedHelper(EEToProfInterfaceImpl *profInterface, AssemblyID assemblyId)
{
    return profInterface->AssemblyUnloadStarted(assemblyId);
}

inline void ProfControlBlock::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       &AssemblyUnloadStartedHelper,
                       assemblyId);
}

inline HRESULT AssemblyUnloadFinishedHelper(EEToProfInterfaceImpl *profInterface, AssemblyID assemblyId, HRESULT hrStatus)
{
    return profInterface->AssemblyUnloadFinished(assemblyId, hrStatus);
}

inline void ProfControlBlock::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       &AssemblyUnloadFinishedHelper,
                       assemblyId, hrStatus);
}

inline BOOL IsProfilerTrackingTransitions(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_CODE_TRANSITIONS);
}

inline HRESULT UnmanagedToManagedTransitionHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return profInterface->UnmanagedToManagedTransition(functionId, reason);
}

inline void ProfControlBlock::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingTransitions,
                       &UnmanagedToManagedTransitionHelper,
                       functionId, reason);
}

inline HRESULT ManagedToUnmanagedTransitionHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return profInterface->ManagedToUnmanagedTransition(functionId, reason);
}

inline void ProfControlBlock::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingTransitions,
                       &ManagedToUnmanagedTransitionHelper,
                       functionId, reason);
}

inline BOOL IsProfilerTrackingExceptions(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_EXCEPTIONS);
}

inline HRESULT ExceptionThrownHelper(EEToProfInterfaceImpl *profInterface, ObjectID thrownObjectId)
{
    return profInterface->ExceptionThrown(thrownObjectId);
}

inline void ProfControlBlock::ExceptionThrown(ObjectID thrownObjectId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionThrownHelper,
                       thrownObjectId);
}

inline HRESULT ExceptionSearchFunctionEnterHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId)
{
    return profInterface->ExceptionSearchFunctionEnter(functionId);
}

inline void ProfControlBlock::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionSearchFunctionEnterHelper,
                       functionId);
}

inline HRESULT ExceptionSearchFunctionLeaveHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->ExceptionSearchFunctionLeave();
}

inline void ProfControlBlock::ExceptionSearchFunctionLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionSearchFunctionLeaveHelper);
}

inline HRESULT ExceptionSearchFilterEnterHelper(EEToProfInterfaceImpl *profInterface, FunctionID funcId)
{
    return profInterface->ExceptionSearchFilterEnter(funcId);
}

inline void ProfControlBlock::ExceptionSearchFilterEnter(FunctionID funcId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionSearchFilterEnterHelper,
                       funcId);
}

inline HRESULT ExceptionSearchFilterLeaveHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->ExceptionSearchFilterLeave();
}

inline void ProfControlBlock::ExceptionSearchFilterLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionSearchFilterLeaveHelper);
}

inline HRESULT ExceptionSearchCatcherFoundHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId)
{
    return profInterface->ExceptionSearchCatcherFound(functionId);
}

inline void ProfControlBlock::ExceptionSearchCatcherFound(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionSearchCatcherFoundHelper,
                       functionId);
}

inline HRESULT ExceptionOSHandlerEnterHelper(EEToProfInterfaceImpl *profInterface, FunctionID funcId)
{
    return profInterface->ExceptionOSHandlerEnter(funcId);
}

inline void ProfControlBlock::ExceptionOSHandlerEnter(FunctionID funcId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionOSHandlerEnterHelper,
                       funcId);
}

inline HRESULT ExceptionOSHandlerLeaveHelper(EEToProfInterfaceImpl *profInterface, FunctionID funcId)
{
    return profInterface->ExceptionOSHandlerLeave(funcId);
}

inline void ProfControlBlock::ExceptionOSHandlerLeave(FunctionID funcId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionOSHandlerLeaveHelper,
                       funcId);
}

inline HRESULT ExceptionUnwindFunctionEnterHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId)
{
    return profInterface->ExceptionUnwindFunctionEnter(functionId);
}

inline void ProfControlBlock::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionUnwindFunctionEnterHelper,
                       functionId);
}

inline HRESULT ExceptionUnwindFunctionLeaveHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->ExceptionUnwindFunctionLeave();
}

inline void ProfControlBlock::ExceptionUnwindFunctionLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionUnwindFunctionLeaveHelper);
}

inline HRESULT ExceptionUnwindFinallyEnterHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId)
{
    return profInterface->ExceptionUnwindFinallyEnter(functionId);
}

inline void ProfControlBlock::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionUnwindFinallyEnterHelper,
                       functionId);
}

inline HRESULT ExceptionUnwindFinallyLeaveHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->ExceptionUnwindFinallyLeave();
}

inline void ProfControlBlock::ExceptionUnwindFinallyLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionUnwindFinallyLeaveHelper);
}

inline HRESULT ExceptionCatcherEnterHelper(EEToProfInterfaceImpl *profInterface, FunctionID functionId, ObjectID objectId)
{
    return profInterface->ExceptionCatcherEnter(functionId, objectId);
}

inline void ProfControlBlock::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionCatcherEnterHelper,
                       functionId, objectId);
}

inline HRESULT ExceptionCatcherLeaveHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->ExceptionCatcherLeave();
}

inline void ProfControlBlock::ExceptionCatcherLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       &ExceptionCatcherLeaveHelper);
}

inline BOOL IsProfilerTrackingCCW(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_CCW);
}

inline HRESULT COMClassicVTableCreatedHelper(EEToProfInterfaceImpl *profInterface, ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
{
    return profInterface->COMClassicVTableCreated(wrappedClassId, implementedIID, pVTable, cSlots);
}

inline void ProfControlBlock::COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingCCW,
                       &COMClassicVTableCreatedHelper,
                       wrappedClassId, implementedIID, pVTable, cSlots);
}

inline BOOL IsProfilerTrackingSuspends(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_SUSPENDS);
}

inline HRESULT RuntimeSuspendStartedHelper(EEToProfInterfaceImpl *profInterface, COR_PRF_SUSPEND_REASON suspendReason)
{
    return profInterface->RuntimeSuspendStarted(suspendReason);
}

inline void ProfControlBlock::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       &RuntimeSuspendStartedHelper,
                       suspendReason);
}

inline HRESULT RuntimeSuspendFinishedHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->RuntimeSuspendFinished();
}

inline void ProfControlBlock::RuntimeSuspendFinished()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       &RuntimeSuspendFinishedHelper);
}

inline HRESULT RuntimeSuspendAbortedHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->RuntimeSuspendAborted();
}

inline void ProfControlBlock::RuntimeSuspendAborted()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       &RuntimeSuspendAbortedHelper);
}

inline HRESULT RuntimeResumeStartedHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->RuntimeResumeStarted();
}

inline void ProfControlBlock::RuntimeResumeStarted()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       &RuntimeResumeStartedHelper);
}

inline HRESULT RuntimeResumeFinishedHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->RuntimeResumeFinished();
}

inline void ProfControlBlock::RuntimeResumeFinished()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       &RuntimeResumeFinishedHelper);
}

inline HRESULT RuntimeThreadSuspendedHelper(EEToProfInterfaceImpl *profInterface, ThreadID suspendedThreadId)
{
    return profInterface->RuntimeThreadSuspended(suspendedThreadId);
}

inline void ProfControlBlock::RuntimeThreadSuspended(ThreadID suspendedThreadId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       &RuntimeThreadSuspendedHelper,
                       suspendedThreadId);
}

inline HRESULT RuntimeThreadResumedHelper(EEToProfInterfaceImpl *profInterface, ThreadID resumedThreadId)
{
    return profInterface->RuntimeThreadResumed(resumedThreadId);
}

inline void ProfControlBlock::RuntimeThreadResumed(ThreadID resumedThreadId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       &RuntimeThreadResumedHelper,
                       resumedThreadId);
}

inline BOOL IsProfilerTrackingAllocations(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_ENABLE_OBJECT_ALLOCATED)
                || pProfilerInfo->eventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED));
}

inline HRESULT ObjectAllocatedHelper(EEToProfInterfaceImpl *profInterface, ObjectID objectId, ClassID classId)
{
    return profInterface->ObjectAllocated(objectId, classId);
}

inline void ProfControlBlock::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAllocations,
                       &ObjectAllocatedHelper,
                       objectId, classId);
}


inline BOOL IsProfilerTrackingGC(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_GC);
}

inline HRESULT FinalizeableObjectQueuedHelper(EEToProfInterfaceImpl *profInterface, BOOL isCritical, ObjectID objectID)
{
    return profInterface->FinalizeableObjectQueued(isCritical, objectID);
}

inline void ProfControlBlock::FinalizeableObjectQueued(BOOL isCritical, ObjectID objectID)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &FinalizeableObjectQueuedHelper,
                       isCritical, objectID);
}

inline HRESULT MovedReferenceHelper(EEToProfInterfaceImpl *profInterface, BYTE *pbMemBlockStart, BYTE *pbMemBlockEnd, ptrdiff_t cbRelocDistance, void *pHeapId, BOOL fCompacting)
{
    return profInterface->MovedReference(pbMemBlockStart, pbMemBlockEnd, cbRelocDistance, pHeapId, fCompacting);
}

inline void ProfControlBlock::MovedReference(BYTE *pbMemBlockStart, BYTE *pbMemBlockEnd, ptrdiff_t cbRelocDistance, void *pHeapId, BOOL fCompacting)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &MovedReferenceHelper,
                       pbMemBlockStart, pbMemBlockEnd, cbRelocDistance, pHeapId, fCompacting);
}

inline HRESULT EndMovedReferencesHelper(EEToProfInterfaceImpl *profInterface, void *pHeapId)
{
    return profInterface->EndMovedReferences(pHeapId);
}

inline void ProfControlBlock::EndMovedReferences(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &EndMovedReferencesHelper,
                       pHeapId);
}

inline HRESULT RootReference2Helper(EEToProfInterfaceImpl *profInterface, BYTE *objectId, EtwGCRootKind dwEtwRootKind, EtwGCRootFlags dwEtwRootFlags, void *rootID, void *pHeapId)
{
    return profInterface->RootReference2(objectId, dwEtwRootKind, dwEtwRootFlags, rootID, pHeapId);
}

inline void ProfControlBlock::RootReference2(BYTE *objectId, EtwGCRootKind dwEtwRootKind, EtwGCRootFlags dwEtwRootFlags, void *rootID, void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &RootReference2Helper,
                       objectId, dwEtwRootKind, dwEtwRootFlags, rootID, pHeapId);
}

inline HRESULT EndRootReferences2Helper(EEToProfInterfaceImpl *profInterface, void *pHeapId)
{
    return profInterface->EndRootReferences2(pHeapId);
}

inline void ProfControlBlock::EndRootReferences2(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &EndRootReferences2Helper,
                       pHeapId);
}

inline HRESULT ConditionalWeakTableElementReferenceHelper(EEToProfInterfaceImpl *profInterface, BYTE *primaryObjectId, BYTE *secondaryObjectId, void *rootID, void *pHeapId)
{
    if (!profInterface->IsCallback5Supported())
    {
        return S_OK;
    }

    return profInterface->ConditionalWeakTableElementReference(primaryObjectId, secondaryObjectId, rootID, pHeapId);
}

inline void ProfControlBlock::ConditionalWeakTableElementReference(BYTE *primaryObjectId, BYTE *secondaryObjectId, void *rootID, void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &ConditionalWeakTableElementReferenceHelper,
                       primaryObjectId, secondaryObjectId, rootID, pHeapId);
}

inline HRESULT EndConditionalWeakTableElementReferencesHelper(EEToProfInterfaceImpl *profInterface, void *pHeapId)
{
    if (!profInterface->IsCallback5Supported())
    {
        return S_OK;
    }

    return profInterface->EndConditionalWeakTableElementReferences(pHeapId);
}

inline void ProfControlBlock::EndConditionalWeakTableElementReferences(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &EndConditionalWeakTableElementReferencesHelper,
                       pHeapId);
}

inline HRESULT AllocByClassHelper(EEToProfInterfaceImpl *profInterface, ObjectID objId, ClassID classId, void *pHeapId)
{
    return profInterface->AllocByClass(objId, classId, pHeapId);
}

inline void ProfControlBlock::AllocByClass(ObjectID objId, ClassID classId, void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &AllocByClassHelper,
                       objId, classId, pHeapId);
}

inline HRESULT EndAllocByClassHelper(EEToProfInterfaceImpl *profInterface, void *pHeapId)
{
    return profInterface->EndAllocByClass(pHeapId);
}

inline void ProfControlBlock::EndAllocByClass(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &EndAllocByClassHelper,
                       pHeapId);
}

inline HRESULT ObjectReferenceHelper(EEToProfInterfaceImpl *profInterface, ObjectID objId, ClassID classId, ULONG cNumRefs, ObjectID *arrObjRef)
{
    return profInterface->ObjectReference(objId, classId, cNumRefs, arrObjRef);
}

inline HRESULT ProfControlBlock::ObjectReference(ObjectID objId, ClassID classId, ULONG cNumRefs, ObjectID *arrObjRef)
{
    LIMITED_METHOD_CONTRACT;

    return DoProfilerCallback(ProfilerCallbackType::Active,
                              IsProfilerTrackingGC,
                              &ObjectReferenceHelper,
                              objId, classId, cNumRefs, arrObjRef);
}

inline HRESULT HandleCreatedHelper(EEToProfInterfaceImpl *profInterface, UINT_PTR handleId, ObjectID initialObjectId)
{
    return profInterface->HandleCreated(handleId, initialObjectId);
}

inline void ProfControlBlock::HandleCreated(UINT_PTR handleId, ObjectID initialObjectId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &HandleCreatedHelper,
                       handleId, initialObjectId);
}

inline HRESULT HandleDestroyedHelper(EEToProfInterfaceImpl *profInterface, UINT_PTR handleId)
{
    return profInterface->HandleDestroyed(handleId);
}

inline void ProfControlBlock::HandleDestroyed(UINT_PTR handleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       &HandleDestroyedHelper,
                       handleId);
}


inline BOOL IsProfilerTrackingBasicGC(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskHighSet(COR_PRF_HIGH_BASIC_GC);
}

inline BOOL IsProfilerTrackingMovedObjects(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS);
}

inline BOOL IsProfilerTrackingGCOrBasicGC(ProfilerInfo *pProfilerInfo)
{
    return IsProfilerTrackingGC(pProfilerInfo) || IsProfilerTrackingBasicGC(pProfilerInfo);
}

inline BOOL IsProfilerTrackingGCOrMovedObjects(ProfilerInfo *pProfilerInfo)
{
    return IsProfilerTrackingGC(pProfilerInfo) || IsProfilerTrackingMovedObjects(pProfilerInfo);
}

inline HRESULT GarbageCollectionStartedHelper(EEToProfInterfaceImpl *profInterface, int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    return profInterface->GarbageCollectionStarted(cGenerations, generationCollected, reason);
}

inline void ProfControlBlock::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGCOrBasicGC,
                       &GarbageCollectionStartedHelper,
                       cGenerations, generationCollected, reason);
}

inline HRESULT GarbageCollectionFinishedHelper(EEToProfInterfaceImpl *profInterface)
{
    return profInterface->GarbageCollectionFinished();
}

inline void ProfControlBlock::GarbageCollectionFinished()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGCOrBasicGC,
                       &GarbageCollectionFinishedHelper);
}


inline BOOL IsProfilerMonitoringEventPipe(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return pProfilerInfo->eventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_EVENT_PIPE);
}

inline HRESULT EventPipeEventDeliveredHelper(EEToProfInterfaceImpl *profInterface,
                                             EventPipeProvider *provider,
                                             DWORD eventId,
                                             DWORD eventVersion,
                                             ULONG cbMetadataBlob,
                                             LPCBYTE metadataBlob,
                                             ULONG cbEventData,
                                             LPCBYTE eventData,
                                             LPCGUID pActivityId,
                                             LPCGUID pRelatedActivityId,
                                             Thread *pEventThread,
                                             ULONG numStackFrames,
                                             UINT_PTR stackFrames[])
{
    return profInterface->EventPipeEventDelivered(provider,
                                           eventId,
                                           eventVersion,
                                           cbMetadataBlob,
                                           metadataBlob,
                                           cbEventData,
                                           eventData,
                                           pActivityId,
                                           pRelatedActivityId,
                                           pEventThread,
                                           numStackFrames,
                                           stackFrames);
}

inline void ProfControlBlock::EventPipeEventDelivered(EventPipeProvider *provider,
                                                      DWORD eventId,
                                                      DWORD eventVersion,
                                                      ULONG cbMetadataBlob,
                                                      LPCBYTE metadataBlob,
                                                      ULONG cbEventData,
                                                      LPCBYTE eventData,
                                                      LPCGUID pActivityId,
                                                      LPCGUID pRelatedActivityId,
                                                      Thread *pEventThread,
                                                      ULONG numStackFrames,
                                                      UINT_PTR stackFrames[])
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerMonitoringEventPipe,
                       &EventPipeEventDeliveredHelper,
                        provider,
                        eventId,
                        eventVersion,
                        cbMetadataBlob,
                        metadataBlob,
                        cbEventData,
                        eventData,
                        pActivityId,
                        pRelatedActivityId,
                        pEventThread,
                        numStackFrames,
                        stackFrames);
}

inline HRESULT EventPipeProviderCreatedHelper(EEToProfInterfaceImpl *profInterface, EventPipeProvider *provider)
{
    return profInterface->EventPipeProviderCreated(provider);
}

inline void ProfControlBlock::EventPipeProviderCreated(EventPipeProvider *provider)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerMonitoringEventPipe,
                       &EventPipeProviderCreatedHelper,
                       provider);
}

//---------------------------------------------------------------------------------------
// Inlined helpers used throughout the runtime to check for the profiler's load status
// and what features it enabled callbacks for.
//---------------------------------------------------------------------------------------

FORCEINLINE BOOL CORProfilerPresent()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->mainProfilerInfo.pProfInterface.Load() != NULL 
            || (&g_profControlBlock)->notificationProfilerCount.Load() > 0;
}

FORCEINLINE BOOL CORMainProfilerPresent()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->mainProfilerInfo.curProfStatus.Get() >= kProfStatusActive;
}

// These return whether a CLR Profiler is actively loaded AND has requested the
// specified callback or functionality

FORCEINLINE BOOL CORProfilerFunctionIDMapperEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (CORMainProfilerPresent() &&
        (
         ((&g_profControlBlock)->mainProfilerInfo.pProfInterface->GetFunctionIDMapper()  != NULL) ||
         ((&g_profControlBlock)->mainProfilerInfo.pProfInterface->GetFunctionIDMapper2() != NULL)
        ));
}

FORCEINLINE BOOL CORProfilerTrackJITInfo()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_JIT_COMPILATION);
}

FORCEINLINE BOOL CORProfilerTrackCacheSearches()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CACHE_SEARCHES);
}

FORCEINLINE BOOL CORProfilerTrackModuleLoads()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_MODULE_LOADS);
}

FORCEINLINE BOOL CORProfilerTrackAssemblyLoads()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_ASSEMBLY_LOADS);
}

FORCEINLINE BOOL CORProfilerTrackAppDomainLoads()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_APPDOMAIN_LOADS);
}

FORCEINLINE BOOL CORProfilerTrackThreads()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_THREADS);
}

FORCEINLINE BOOL CORProfilerTrackClasses()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CLASS_LOADS);
}

FORCEINLINE BOOL CORProfilerTrackGC()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_GC);
}

FORCEINLINE BOOL CORProfilerTrackAllocationsEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return
        (
#ifdef PROF_TEST_ONLY_FORCE_OBJECT_ALLOCATED
            (&g_profControlBlock)->fTestOnlyForceObjectAllocated ||
#endif
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_OBJECT_ALLOCATED)
        );
}

FORCEINLINE BOOL CORProfilerTrackAllocations()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_OBJECT_ALLOCATED);
}

FORCEINLINE BOOL CORProfilerTrackLargeAllocations()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED);
}

FORCEINLINE BOOL CORProfilerTrackPinnedAllocations()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_PINNEDOBJECT_ALLOCATED);
}

FORCEINLINE BOOL CORProfilerEnableRejit()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_REJIT);
}

FORCEINLINE BOOL CORProfilerTrackExceptions()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_EXCEPTIONS);
}

FORCEINLINE BOOL CORProfilerTrackTransitions()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CODE_TRANSITIONS);
}

FORCEINLINE BOOL CORProfilerTrackEnterLeave()
{
    STATIC_CONTRACT_LIMITED_METHOD;

#ifdef PROF_TEST_ONLY_FORCE_ELT
    if ((&g_profControlBlock)->fTestOnlyForceEnterLeave)
        return TRUE;
#endif // PROF_TEST_ONLY_FORCE_ELT

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_ENTERLEAVE);
}

FORCEINLINE BOOL CORProfilerTrackCCW()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CCW);
}

FORCEINLINE BOOL CORProfilerTrackSuspends()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_SUSPENDS);
}

FORCEINLINE BOOL CORProfilerDisableInlining()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_DISABLE_INLINING);
}

FORCEINLINE BOOL CORProfilerDisableOptimizations()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_DISABLE_OPTIMIZATIONS);
}

FORCEINLINE BOOL CORProfilerUseProfileImages()
{
    STATIC_CONTRACT_LIMITED_METHOD;

#ifdef PROF_TEST_ONLY_FORCE_ELT
    if ((&g_profControlBlock)->fTestOnlyForceEnterLeave)
        return TRUE;
#endif // PROF_TEST_ONLY_FORCE_ELT

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_REQUIRE_PROFILE_IMAGE);
}

FORCEINLINE BOOL CORProfilerDisableAllNGenImages()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_DISABLE_ALL_NGEN_IMAGES);
}

FORCEINLINE BOOL CORProfilerTrackConditionalWeakTableElements()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return CORProfilerTrackGC() && (&g_profControlBlock)->IsCallback5Supported();
}

// These return whether a CLR Profiler has requested the specified functionality.
//
// Note that, unlike the above functions, a profiler that's not done loading (and is
// still somewhere in the initialization phase) still counts. This is only safe because
// these functions are not used to determine whether to issue a callback. These functions
// are used primarily during the initialization path to choose between slow / fast-path
// ELT hooks (and later on as part of asserts).

FORCEINLINE BOOL CORProfilerELT3SlowPathEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO));
}

FORCEINLINE BOOL CORProfilerELT3SlowPathEnterEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FRAME_INFO));
}

FORCEINLINE BOOL CORProfilerELT3SlowPathLeaveEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO));
}

FORCEINLINE BOOL CORProfilerELT3SlowPathTailcallEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FRAME_INFO));
}

FORCEINLINE BOOL CORProfilerELT2FastPathEnterEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return !((&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FRAME_INFO)));
}

FORCEINLINE BOOL CORProfilerELT2FastPathLeaveEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return !((&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO)));
}

FORCEINLINE BOOL CORProfilerELT2FastPathTailcallEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return !((&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FRAME_INFO)));
}

FORCEINLINE BOOL CORProfilerFunctionArgsEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_FUNCTION_ARGS);
}

FORCEINLINE BOOL CORProfilerFunctionReturnValueEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_FUNCTION_RETVAL);
}

FORCEINLINE BOOL CORProfilerFrameInfoEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_FRAME_INFO);
}

FORCEINLINE BOOL CORProfilerStackSnapshotEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_STACK_SNAPSHOT);
}

FORCEINLINE BOOL CORProfilerInMemorySymbolsUpdatesEnabled()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED);
}

FORCEINLINE BOOL CORProfilerTrackDynamicFunctionUnloads()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS);
}

FORCEINLINE BOOL CORProfilerDisableTieredCompilation()
{
    STATIC_CONTRACT_LIMITED_METHOD;


    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_DISABLE_TIERED_COMPILATION);
}

FORCEINLINE BOOL CORProfilerTrackBasicGC()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_BASIC_GC);
}

FORCEINLINE BOOL CORProfilerTrackGCMovedObjects()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS);
}

FORCEINLINE BOOL CORProfilerTrackEventPipe()
{
    STATIC_CONTRACT_LIMITED_METHOD;

    return (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_EVENT_PIPE);
}

#if defined(PROFILING_SUPPORTED)

//---------------------------------------------------------------------------------------
// These macros must be placed around any callbacks to g_profControlBlock by
// the EE. Example:
//    {
//        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAppDomainLoads;
//        g_profControlBlock.AppDomainCreationStarted(MyAppDomainID);
//        END_PROFILER_CALLBACK();
//    }
// The parameter to the BEGIN_PROFILER_CALLBACK is the condition you want to check for, to
// determine whether the profiler is loaded and requesting the callback you're about to
// issue. Typically, this will be a call to one of the inline functions in
// profilepriv.inl. If the condition is true, the macro will increment an evacuation
// counter that effectively pins the profiler, recheck the condition, and (if still
// true), execute whatever code you place inside the BEGIN/END_PROFILER_CALLBACK block. If
// your condition is more complex than a simple profiler status check, then place the
// profiler status check as parameter to the macro, and add a separate if inside the
// block. Example:
//
//    {
//        BEGIN_PROFILER_CALLBACK(CorProfilerTrackTransitions);
//        if (!pNSL->pMD->IsQCall())
//        {
//            g_profControlBlock.
//                ManagedToUnmanagedTransition((FunctionID) pNSL->pMD,
//                COR_PRF_TRANSITION_CALL);
//        }
//        END_PROFILER_CALLBACK();
//    }
//
// This ensures that the extra condition check (in this case "if
// (!pNSL->pMD->IsQCall())") is only evaluated if the profiler is loaded. That way, we're
// not executing extra, unnecessary instructions when no profiler is present.
//
// See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
// for more details about how the synchronization works.
#define BEGIN_PROFILER_CALLBACK(condition)                                                  \
    /* Do a cheap check of the condition (dirty-read)                                   */  \
    if (condition)                                                                          \
    {                                                                                       \

#define END_PROFILER_CALLBACK()  }

#else // PROFILING_SUPPORTED

// Profiling feature not supported

#define BEGIN_PROFILER_CALLBACK(condition)       if (false) {
#define END_PROFILER_CALLBACK()                  }

#endif // PROFILING_SUPPORTED

#endif // _ProfilePriv_inl_

