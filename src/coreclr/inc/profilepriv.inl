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
    dwProfilerEvacuationCounter = 0;
    ResetPerSessionStatus();
    inUse = FALSE;
}

template<typename ConditionFunc>
inline BOOL AnyProfilerPassesCondition(ConditionFunc condition)
{
    BOOL anyPassed = FALSE;
    (&g_profControlBlock)->DoProfilerCallback(ProfilerCallbackType::ActiveOrInitializing,
                                              condition, 
                                              &anyPassed,
                                              [](BOOL *pAnyPassed, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                                              {
                                                   *pAnyPassed = TRUE;
                                                   return S_OK;
                                              });

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

    for (SIZE_T i = 0; i < MAX_NOTIFICATION_PROFILERS; ++i)
    {
        notificationOnlyProfilers[i].Init();
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

inline ProfilerInfo *ProfControlBlock::GetProfilerInfo(ProfToEEInterfaceImpl *pProfToEE)
{
    ProfilerInfo *pProfilerInfo = NULL;
    IterateProfilers(ProfilerCallbackType::Active,
                    [](ProfilerInfo *pProfilerInfo, ProfToEEInterfaceImpl *pProfToEE, ProfilerInfo **ppFoundProfilerInfo)
                      {
                          if (pProfilerInfo->pProfInterface->m_pProfToEE == pProfToEE)
                          {
                              *ppFoundProfilerInfo = pProfilerInfo;
                          }
                      },
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

inline BOOL ProfControlBlock::IsCallback3Supported()
{
    return AnyProfilerPassesCondition([](ProfilerInfo *pProfilerInfo) { return pProfilerInfo->pProfInterface->IsCallback3Supported(); });
}

inline BOOL ProfControlBlock::IsCallback5Supported()
{
    return AnyProfilerPassesCondition([](ProfilerInfo *pProfilerInfo) { return pProfilerInfo->pProfInterface->IsCallback5Supported(); });
}

inline BOOL ProfControlBlock::IsDisableTransparencySet()
{
    return AnyProfilerPassesCondition([](ProfilerInfo *pProfilerInfo) { return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST); });
}

inline BOOL ProfControlBlock::RequiresGenericsContextForEnterLeave()
{
    return AnyProfilerPassesCondition([](ProfilerInfo *pProfilerInfo) { return pProfilerInfo->pProfInterface->RequiresGenericsContextForEnterLeave(); }); 
}

inline bool DoesProfilerWantEEFunctionIDMapper(ProfilerInfo *pProfilerInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return ((pProfilerInfo->pProfInterface->GetFunctionIDMapper()  != NULL) ||
             (pProfilerInfo->pProfInterface->GetFunctionIDMapper2() != NULL));
}

inline UINT_PTR ProfControlBlock::EEFunctionIDMapper(FunctionID funcId, BOOL *pbHookFunction)
{
    LIMITED_METHOD_CONTRACT;
    UINT_PTR ptr = NULL;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          [](ProfilerInfo *pProfilerInfo, FunctionID funcId, BOOL *pbHookFunction, UINT_PTR *pPtr)
                          {
                               if (DoesProfilerWantEEFunctionIDMapper(pProfilerInfo))
                               {
                                   *pPtr = pProfilerInfo->pProfInterface->EEFunctionIDMapper(funcId, pbHookFunction);
                               }
                          },
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

inline void ProfControlBlock::ThreadCreated(ThreadID threadID)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ThreadID threadID)
                        {
                            return profInterface->ThreadCreated(threadID);
                        },
                        threadID);
}

inline void ProfControlBlock::ThreadDestroyed(ThreadID threadID)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ThreadID threadID)
                        {
                            return profInterface->ThreadDestroyed(threadID);
                        },
                        threadID);
}

inline void ProfControlBlock::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ThreadID managedThreadId, DWORD osThreadId)
                        {
                            return profInterface->ThreadAssignedToOSThread(managedThreadId, osThreadId);
                        },
                        managedThreadId, osThreadId);
}

inline void ProfControlBlock::ThreadNameChanged(ThreadID managedThreadId, ULONG cchName, WCHAR name[])
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingThreads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ThreadID managedThreadId, ULONG cchName, WCHAR name[])
                        {
                            return profInterface->ThreadNameChanged(managedThreadId, cchName, name);
                        },
                        managedThreadId, cchName, name);
}

inline void ProfControlBlock::Shutdown()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       [](ProfilerInfo *pProfilerInfo) { return true; },
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->Shutdown();
                        });
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

inline void ProfControlBlock::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
                        {
                            return profInterface->JITCompilationFinished(functionId, hrStatus, fIsSafeToBlock);
                        },
                        functionId, hrStatus, fIsSafeToBlock);
}

inline void ProfControlBlock::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, BOOL fIsSafeToBlock)
                        {
                            return profInterface->JITCompilationStarted(functionId, fIsSafeToBlock);
                        },
                        functionId, fIsSafeToBlock);
}

inline void ProfControlBlock::DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE pILHeader, ULONG cbILHeader)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE pILHeader, ULONG cbILHeader)
                        {
                            return profInterface->DynamicMethodJITCompilationStarted(functionId, fIsSafeToBlock, pILHeader, cbILHeader);
                        },
                        functionId, fIsSafeToBlock, pILHeader, cbILHeader);
}

inline void ProfControlBlock::DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingJITInfo,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
                        {
                            return profInterface->DynamicMethodJITCompilationFinished(functionId, hrStatus, fIsSafeToBlock);
                        },
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

inline void ProfControlBlock::DynamicMethodUnloaded(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerMonitoringDynamicFunctionUnloads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId)
                        {
                            return profInterface->DynamicMethodUnloaded(functionId);
                        },
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

inline void ProfControlBlock::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL *pbUseCachedFunction)
{
    LIMITED_METHOD_CONTRACT;

    BOOL allTrue = TRUE;
    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingCacheSearches,
                       &allTrue,
                       [](BOOL *pAllTrue, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, BOOL *pbUseCachedFunction)
                        {
                            HRESULT hr = profInterface->JITCachedFunctionSearchStarted(functionId, pbUseCachedFunction);
                            *pAllTrue &= *pbUseCachedFunction;
                            return hr;
                        },
                        functionId, pbUseCachedFunction);

    // If any reject it, consider it rejected.
    *pbUseCachedFunction = allTrue;
}

inline void ProfControlBlock::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingCacheSearches,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, COR_PRF_JIT_CACHE result)
                        {
                            return profInterface->JITCachedFunctionSearchFinished(functionId, result);
                        },
                        functionId, result);
}

inline HRESULT ProfControlBlock::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline)
{
    LIMITED_METHOD_CONTRACT;

    BOOL allTrue = TRUE;
    HRESULT hr =  DoProfilerCallback(ProfilerCallbackType::Active,
                                     IsProfilerTrackingJITInfo,
                                     &allTrue,
                                     [](BOOL *pAllTrue, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline)
                                      {
                                          HRESULT hr = profInterface->JITInlining(callerId, calleeId, pfShouldInline);
                                          *pAllTrue &= *pfShouldInline;
                                          return hr;
                                      },
                                      callerId, calleeId, pfShouldInline);

    // If any reject it, consider it rejected.
    *pfShouldInline = allTrue;
    return hr;
}

inline BOOL IsRejitEnabled(ProfilerInfo *pProfilerInfo)
{
    return pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_ENABLE_REJIT);
}

inline void ProfControlBlock::ReJITCompilationStarted(FunctionID functionId, ReJITID reJitId, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          [](ProfilerInfo *pProfilerInfo, FunctionID functionId, ReJITID reJitId, BOOL fIsSafeToBlock)
                          {
                               if (IsRejitEnabled(pProfilerInfo))
                               {
                                   pProfilerInfo->pProfInterface->ReJITCompilationStarted(functionId, reJitId, fIsSafeToBlock);
                               }
                          },
                          functionId, reJitId, fIsSafeToBlock);
}

inline HRESULT ProfControlBlock::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          [](ProfilerInfo *pProfilerInfo, ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl, HRESULT *pHr)
                          {
                               if (IsRejitEnabled(pProfilerInfo))
                               {
                                   *pHr = pProfilerInfo->pProfInterface->GetReJITParameters(moduleId, methodId, pFunctionControl);
                               }
                          },
                          moduleId, methodId, pFunctionControl, &hr);
    return hr;
}

inline void ProfControlBlock::ReJITCompilationFinished(FunctionID functionId, ReJITID reJitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    LIMITED_METHOD_CONTRACT;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          [](ProfilerInfo *pProfilerInfo, FunctionID functionId, ReJITID reJitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
                          {
                               if (IsRejitEnabled(pProfilerInfo))
                               {
                                   pProfilerInfo->pProfInterface->ReJITCompilationFinished(functionId, reJitId, hrStatus, fIsSafeToBlock);
                               }
                          },
                          functionId, reJitId, hrStatus, fIsSafeToBlock);
}

inline void ProfControlBlock::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;
    DoOneProfilerIteration(&mainProfilerInfo,
                          ProfilerCallbackType::Active,
                          [](ProfilerInfo *pProfilerInfo, ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
                          {
                               if (IsRejitEnabled(pProfilerInfo))
                               {
                                   pProfilerInfo->pProfInterface->ReJITError(moduleId, methodId, functionId, hrStatus);
                               }
                          },
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

inline void ProfControlBlock::ModuleLoadStarted(ModuleID moduleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ModuleID moduleId)
                        {
                            return profInterface->ModuleLoadStarted(moduleId);
                        },
                        moduleId);
}

inline void ProfControlBlock::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ModuleID moduleId, HRESULT hrStatus)
                        {
                            return profInterface->ModuleLoadFinished(moduleId, hrStatus);
                        },
                        moduleId, hrStatus);
}

inline void ProfControlBlock::ModuleUnloadStarted(ModuleID moduleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ModuleID moduleId)
                        {
                            return profInterface->ModuleUnloadStarted(moduleId);
                        },
                        moduleId);
}

inline void ProfControlBlock::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ModuleID moduleId, HRESULT hrStatus)
                        {
                            return profInterface->ModuleUnloadFinished(moduleId, hrStatus);
                        },
                        moduleId, hrStatus);
}

inline void ProfControlBlock::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingModuleLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ModuleID moduleId, AssemblyID AssemblyId)
                        {
                            return profInterface->ModuleAttachedToAssembly(moduleId, AssemblyId);
                        },
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

inline void ProfControlBlock::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingInMemorySymbolsUpdatesEnabled,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ModuleID moduleId)
                        {
                            return profInterface->ModuleInMemorySymbolsUpdated(moduleId);
                        },
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

inline void ProfControlBlock::ClassLoadStarted(ClassID classId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ClassID classId)
                        {
                            return profInterface->ClassLoadStarted(classId);
                        },
                        classId);
}

inline void ProfControlBlock::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ClassID classId, HRESULT hrStatus)
                        {
                            return profInterface->ClassLoadFinished(classId, hrStatus);
                        },
                        classId, hrStatus);
}

inline void ProfControlBlock::ClassUnloadStarted(ClassID classId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ClassID classId)
                        {
                            return profInterface->ClassUnloadStarted(classId);
                        },
                        classId);
}

inline void ProfControlBlock::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingClasses,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ClassID classId, HRESULT hrStatus)
                        {
                            return profInterface->ClassUnloadFinished(classId, hrStatus);
                        },
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

inline void ProfControlBlock::AppDomainCreationStarted(AppDomainID appDomainId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AppDomainID appDomainId)
                        {
                            return profInterface->AppDomainCreationStarted(appDomainId);
                        },
                        appDomainId);
}

inline void ProfControlBlock::AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AppDomainID appDomainId, HRESULT hrStatus)
                        {
                            return profInterface->AppDomainCreationFinished(appDomainId, hrStatus);
                        },
                        appDomainId, hrStatus);
}

inline void ProfControlBlock::AppDomainShutdownStarted(AppDomainID appDomainId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AppDomainID appDomainId)
                        {
                            return profInterface->AppDomainShutdownStarted(appDomainId);
                        },
                        appDomainId);
}

inline void ProfControlBlock::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAppDomainLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AppDomainID appDomainId, HRESULT hrStatus)
                        {
                            return profInterface->AppDomainShutdownFinished(appDomainId, hrStatus);
                        },
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

inline void ProfControlBlock::AssemblyLoadStarted(AssemblyID assemblyId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AssemblyID assemblyId)
                        {
                            return profInterface->AssemblyLoadStarted(assemblyId);
                        },
                        assemblyId);
}

inline void ProfControlBlock::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AssemblyID assemblyId, HRESULT hrStatus)
                        {
                            return profInterface->AssemblyLoadFinished(assemblyId, hrStatus);
                        },
                        assemblyId, hrStatus);
}

inline void ProfControlBlock::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AssemblyID assemblyId)
                        {
                            return profInterface->AssemblyUnloadStarted(assemblyId);
                        },
                        assemblyId);
}

inline void ProfControlBlock::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAssemblyLoads,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, AssemblyID assemblyId, HRESULT hrStatus)
                        {
                            return profInterface->AssemblyUnloadFinished(assemblyId, hrStatus);
                        },
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

inline void ProfControlBlock::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingTransitions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
                        {
                            return profInterface->UnmanagedToManagedTransition(functionId, reason);
                        },
                        functionId, reason);
}

inline void ProfControlBlock::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingTransitions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
                        {
                            return profInterface->ManagedToUnmanagedTransition(functionId, reason);
                        },
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

inline void ProfControlBlock::ExceptionThrown(ObjectID thrownObjectId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ObjectID thrownObjectId)
                        {
                            return profInterface->ExceptionThrown(thrownObjectId);
                        },
                        thrownObjectId);
}

inline void ProfControlBlock::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId)
                        {
                            return profInterface->ExceptionSearchFunctionEnter(functionId);
                        },
                        functionId);
}

inline void ProfControlBlock::ExceptionSearchFunctionLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->ExceptionSearchFunctionLeave();
                        });
}

inline void ProfControlBlock::ExceptionSearchFilterEnter(FunctionID funcId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID funcId)
                        {
                            return profInterface->ExceptionSearchFilterEnter(funcId);
                        },
                        funcId);
}

inline void ProfControlBlock::ExceptionSearchFilterLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->ExceptionSearchFilterLeave();
                        });
}

inline void ProfControlBlock::ExceptionSearchCatcherFound(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId)
                        {
                            return profInterface->ExceptionSearchCatcherFound(functionId);
                        },
                        functionId);
}

inline void ProfControlBlock::ExceptionOSHandlerEnter(FunctionID funcId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID funcId)
                        {
                            return profInterface->ExceptionOSHandlerEnter(funcId);
                        },
                        funcId);
}

inline void ProfControlBlock::ExceptionOSHandlerLeave(FunctionID funcId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID funcId)
                        {
                            return profInterface->ExceptionOSHandlerLeave(funcId);
                        },
                        funcId);
}

inline void ProfControlBlock::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId)
                        {
                            return profInterface->ExceptionUnwindFunctionEnter(functionId);
                        },
                        functionId);
}

inline void ProfControlBlock::ExceptionUnwindFunctionLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->ExceptionUnwindFunctionLeave();
                        });
}

inline void ProfControlBlock::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId)
                        {
                            return profInterface->ExceptionUnwindFinallyEnter(functionId);
                        },
                        functionId);
}

inline void ProfControlBlock::ExceptionUnwindFinallyLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->ExceptionUnwindFinallyLeave();
                        });
}

inline void ProfControlBlock::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, FunctionID functionId, ObjectID objectId)
                        {
                            return profInterface->ExceptionCatcherEnter(functionId, objectId);
                        },
                        functionId, objectId);
}

inline void ProfControlBlock::ExceptionCatcherLeave()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingExceptions,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->ExceptionCatcherLeave();
                        });
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

inline void ProfControlBlock::COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingCCW,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
                        {
                            return profInterface->COMClassicVTableCreated(wrappedClassId, implementedIID, pVTable, cSlots);
                        },
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

inline void ProfControlBlock::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, COR_PRF_SUSPEND_REASON suspendReason)
                        {
                            return profInterface->RuntimeSuspendStarted(suspendReason);
                        },
                        suspendReason);
}

inline void ProfControlBlock::RuntimeSuspendFinished()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->RuntimeSuspendFinished();
                        });
}

inline void ProfControlBlock::RuntimeSuspendAborted()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->RuntimeSuspendAborted();
                        });
}

inline void ProfControlBlock::RuntimeResumeStarted()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->RuntimeResumeStarted();
                        });
}

inline void ProfControlBlock::RuntimeResumeFinished()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->RuntimeResumeFinished();
                        });
}

inline void ProfControlBlock::RuntimeThreadSuspended(ThreadID suspendedThreadId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ThreadID suspendedThreadId)
                        {
                            return profInterface->RuntimeThreadSuspended(suspendedThreadId);
                        },
                        suspendedThreadId);
}

inline void ProfControlBlock::RuntimeThreadResumed(ThreadID resumedThreadId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingSuspends,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ThreadID resumedThreadId)
                        {
                            return profInterface->RuntimeThreadResumed(resumedThreadId);
                        },
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

inline void ProfControlBlock::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingAllocations,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ObjectID objectId, ClassID classId)
                        {
                            return profInterface->ObjectAllocated(objectId, classId);
                        },
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

inline void ProfControlBlock::FinalizeableObjectQueued(BOOL isCritical, ObjectID objectID)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, BOOL isCritical, ObjectID objectID)
                        {
                            return profInterface->FinalizeableObjectQueued(isCritical, objectID);
                        },
                        isCritical, objectID);
}

inline void ProfControlBlock::MovedReference(BYTE *pbMemBlockStart, BYTE *pbMemBlockEnd, ptrdiff_t cbRelocDistance, void *pHeapId, BOOL fCompacting)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, BYTE *pbMemBlockStart, BYTE *pbMemBlockEnd, ptrdiff_t cbRelocDistance, void *pHeapId, BOOL fCompacting)
                        {
                            return profInterface->MovedReference(pbMemBlockStart, pbMemBlockEnd, cbRelocDistance, pHeapId, fCompacting);
                        },
                        pbMemBlockStart, pbMemBlockEnd, cbRelocDistance, pHeapId, fCompacting);
}

inline void ProfControlBlock::EndMovedReferences(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, void *pHeapId)
                        {
                            return profInterface->EndMovedReferences(pHeapId);
                        },
                        pHeapId);
}

inline void ProfControlBlock::RootReference2(BYTE *objectId, EtwGCRootKind dwEtwRootKind, EtwGCRootFlags dwEtwRootFlags, void *rootID, void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, BYTE *objectId, EtwGCRootKind dwEtwRootKind, EtwGCRootFlags dwEtwRootFlags, void *rootID, void *pHeapId)
                        {
                            return profInterface->RootReference2(objectId, dwEtwRootKind, dwEtwRootFlags, rootID, pHeapId);
                        },
                        objectId, dwEtwRootKind, dwEtwRootFlags, rootID, pHeapId);
}

inline void ProfControlBlock::EndRootReferences2(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, void *pHeapId)
                        {
                            return profInterface->EndRootReferences2(pHeapId);
                        },
                        pHeapId);
}

inline void ProfControlBlock::ConditionalWeakTableElementReference(BYTE *primaryObjectId, BYTE *secondaryObjectId, void *rootID, void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, BYTE *primaryObjectId, BYTE *secondaryObjectId, void *rootID, void *pHeapId)
                        {
                            if (!profInterface->IsCallback5Supported())
                            {
                                return S_OK;
                            }

                            return profInterface->ConditionalWeakTableElementReference(primaryObjectId, secondaryObjectId, rootID, pHeapId);
                        },
                        primaryObjectId, secondaryObjectId, rootID, pHeapId);
}

inline void ProfControlBlock::EndConditionalWeakTableElementReferences(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, void *pHeapId)
                        {
                            if (!profInterface->IsCallback5Supported())
                            {
                                return S_OK;
                            }

                            return profInterface->EndConditionalWeakTableElementReferences(pHeapId);
                        },
                        pHeapId);
}

inline void ProfControlBlock::AllocByClass(ObjectID objId, ClassID classId, void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ObjectID objId, ClassID classId, void *pHeapId)
                        {
                            return profInterface->AllocByClass(objId, classId, pHeapId);
                        },
                        objId, classId, pHeapId);
}

inline void ProfControlBlock::EndAllocByClass(void *pHeapId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, void *pHeapId)
                        {
                            return profInterface->EndAllocByClass(pHeapId);
                        },
                        pHeapId);
}

inline HRESULT ProfControlBlock::ObjectReference(ObjectID objId, ClassID classId, ULONG cNumRefs, ObjectID *arrObjRef)
{
    LIMITED_METHOD_CONTRACT;

    return DoProfilerCallback(ProfilerCallbackType::Active,
                              IsProfilerTrackingGC,
                              (void *)NULL,
                              [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, ObjectID objId, ClassID classId, ULONG cNumRefs, ObjectID *arrObjRef)
                               {
                                   return profInterface->ObjectReference(objId, classId, cNumRefs, arrObjRef);
                               },
                               objId, classId, cNumRefs, arrObjRef);
}

inline void ProfControlBlock::HandleCreated(UINT_PTR handleId, ObjectID initialObjectId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, UINT_PTR handleId, ObjectID initialObjectId)
                        {
                            return profInterface->HandleCreated(handleId, initialObjectId);
                        },
                        handleId, initialObjectId);
}

inline void ProfControlBlock::HandleDestroyed(UINT_PTR handleId)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, UINT_PTR handleId)
                        {
                            return profInterface->HandleDestroyed(handleId);
                        },
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

inline void ProfControlBlock::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGCOrBasicGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
                        {
                            return profInterface->GarbageCollectionStarted(cGenerations, generationCollected, reason);
                        },
                        cGenerations, generationCollected, reason);
}

inline void ProfControlBlock::GarbageCollectionFinished()
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerTrackingGCOrBasicGC,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface)
                        {
                            return profInterface->GarbageCollectionFinished();
                        });
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
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, EventPipeProvider *provider,
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
                        },
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

inline void ProfControlBlock::EventPipeProviderCreated(EventPipeProvider *provider)
{
    LIMITED_METHOD_CONTRACT;

    DoProfilerCallback(ProfilerCallbackType::Active,
                       IsProfilerMonitoringEventPipe,
                       (void *)NULL,
                       [](void *additionalData, VolatilePtr<EEToProfInterfaceImpl> profInterface, EventPipeProvider *provider)
                        {
                            return profInterface->EventPipeProviderCreated(provider);
                        },
                        provider);
}

//---------------------------------------------------------------------------------------
// Inlined helpers used throughout the runtime to check for the profiler's load status
// and what features it enabled callbacks for.
//---------------------------------------------------------------------------------------

inline BOOL CORProfilerPresent()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return AnyProfilerPassesCondition([](ProfilerInfo *pProfilerInfo) { return pProfilerInfo->curProfStatus.Get() >= kProfStatusActive; });
}

inline BOOL CORMainProfilerPresent()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (&g_profControlBlock)->mainProfilerInfo.curProfStatus.Get() >= kProfStatusActive;
}

// These return whether a CLR Profiler is actively loaded AND has requested the
// specified callback or functionality

inline BOOL CORProfilerFunctionIDMapperEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORMainProfilerPresent() &&
        (
         ((&g_profControlBlock)->mainProfilerInfo.pProfInterface->GetFunctionIDMapper()  != NULL) ||
         ((&g_profControlBlock)->mainProfilerInfo.pProfInterface->GetFunctionIDMapper2() != NULL)
        ));
}

inline BOOL CORProfilerTrackJITInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_JIT_COMPILATION));
}

inline BOOL CORProfilerTrackCacheSearches()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CACHE_SEARCHES));
}

inline BOOL CORProfilerTrackModuleLoads()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_MODULE_LOADS));
}

inline BOOL CORProfilerTrackAssemblyLoads()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_ASSEMBLY_LOADS));
}

inline BOOL CORProfilerTrackAppDomainLoads()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_APPDOMAIN_LOADS));
}

inline BOOL CORProfilerTrackThreads()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_THREADS));
}

inline BOOL CORProfilerTrackClasses()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CLASS_LOADS));
}

inline BOOL CORProfilerTrackGC()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_GC));
}

inline BOOL CORProfilerTrackAllocationsEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return
        (
#ifdef PROF_TEST_ONLY_FORCE_OBJECT_ALLOCATED
            (&g_profControlBlock)->fTestOnlyForceObjectAllocated ||
#endif
            (CORProfilerPresent() &&
                (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_OBJECT_ALLOCATED))
        );
}

inline BOOL CORProfilerTrackAllocations()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return
            (CORProfilerTrackAllocationsEnabled() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_OBJECT_ALLOCATED));
}

inline BOOL CORProfilerTrackLargeAllocations()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return
            (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED));
}

inline BOOL CORProfilerTrackPinnedAllocations()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return
            (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_PINNEDOBJECT_ALLOCATED));
}

inline BOOL CORProfilerEnableRejit()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORMainProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_REJIT));
}

inline BOOL CORProfilerTrackExceptions()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_EXCEPTIONS));
}

inline BOOL CORProfilerTrackTransitions()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CODE_TRANSITIONS));
}

inline BOOL CORProfilerTrackEnterLeave()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifdef PROF_TEST_ONLY_FORCE_ELT
    if ((&g_profControlBlock)->fTestOnlyForceEnterLeave)
        return TRUE;
#endif // PROF_TEST_ONLY_FORCE_ELT

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_ENTERLEAVE));
}

inline BOOL CORProfilerTrackCCW()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_CCW));
}

inline BOOL CORProfilerTrackSuspends()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_MONITOR_SUSPENDS));
}

inline BOOL CORProfilerDisableInlining()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_DISABLE_INLINING));
}

inline BOOL CORProfilerDisableOptimizations()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_DISABLE_OPTIMIZATIONS));
}

inline BOOL CORProfilerUseProfileImages()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifdef PROF_TEST_ONLY_FORCE_ELT
    if ((&g_profControlBlock)->fTestOnlyForceEnterLeave)
        return TRUE;
#endif // PROF_TEST_ONLY_FORCE_ELT

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_REQUIRE_PROFILE_IMAGE));
}

inline BOOL CORProfilerDisableAllNGenImages()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (CORProfilerPresent() &&
            (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_DISABLE_ALL_NGEN_IMAGES));
}

inline BOOL CORProfilerTrackConditionalWeakTableElements()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return CORProfilerTrackGC() && (&g_profControlBlock)->IsCallback5Supported();
}

// CORProfilerPresentOrInitializing() returns nonzero iff a CLR Profiler is actively
// loaded and ready to receive callbacks OR a CLR Profiler has loaded just enough that it
// is ready to receive (or is currently executing inside) its Initialize() callback.
// Typically, you'll want to use code:CORProfilerPresent instead of this.  But there is
// some internal profiling API code that wants to test for event flags for a profiler
// that may still be initializing, and this function is appropriate for that code.
inline BOOL CORProfilerPresentOrInitializing()
{
    LIMITED_METHOD_CONTRACT;
    return AnyProfilerPassesCondition([](ProfilerInfo *pProfilerInfo) { return pProfilerInfo->curProfStatus.Get() > kProfStatusDetaching; });
}

// These return whether a CLR Profiler has requested the specified functionality.
//
// Note that, unlike the above functions, a profiler that's not done loading (and is
// still somewhere in the initialization phase) still counts. This is only safe because
// these functions are not used to determine whether to issue a callback. These functions
// are used primarily during the initialization path to choose between slow / fast-path
// ELT hooks (and later on as part of asserts).

inline BOOL CORProfilerELT3SlowPathEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO)));
}

inline BOOL CORProfilerELT3SlowPathEnterEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FRAME_INFO)));
}

inline BOOL CORProfilerELT3SlowPathLeaveEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO)));
}

inline BOOL CORProfilerELT3SlowPathTailcallEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_FRAME_INFO)));
}

inline BOOL CORProfilerELT2FastPathEnterEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        !((&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FRAME_INFO))));
}

inline BOOL CORProfilerELT2FastPathLeaveEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        !((&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO))));
}

inline BOOL CORProfilerELT2FastPathTailcallEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        !((&g_profControlBlock)->globalEventMask.IsEventMaskSet((COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FRAME_INFO))));
}

inline BOOL CORProfilerFunctionArgsEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_FUNCTION_ARGS));
}

inline BOOL CORProfilerFunctionReturnValueEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_FUNCTION_RETVAL));
}

inline BOOL CORProfilerFrameInfoEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_FRAME_INFO));
}

inline BOOL CORProfilerStackSnapshotEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresentOrInitializing() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_STACK_SNAPSHOT));
}

inline BOOL CORProfilerInMemorySymbolsUpdatesEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED));
}

inline BOOL CORProfilerTrackDynamicFunctionUnloads()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS));
}

inline BOOL CORProfilerDisableTieredCompilation()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;


    return (CORProfilerPresent() &&
         (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_DISABLE_TIERED_COMPILATION));
}

inline BOOL CORProfilerTrackBasicGC()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
         (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_BASIC_GC));
}

inline BOOL CORProfilerTrackGCMovedObjects()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
         (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS));
}

inline BOOL CORProfilerTrackEventPipe()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
        (&g_profControlBlock)->globalEventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_EVENT_PIPE));
}

#if defined(PROFILING_SUPPORTED) && !defined(CROSSGEN_COMPILE)

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

#else // PROFILING_SUPPORTED && !CROSSGEN_COMPILE

// Profiling feature not supported

#define BEGIN_PROFILER_CALLBACK(condition)       if (false) {
#define END_PROFILER_CALLBACK()                  }

#endif // PROFILING_SUPPORTED && !CROSSGEN_COMPILE

#endif // _ProfilePriv_inl_

