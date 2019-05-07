// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
BOOL CORProfilerBypassSecurityChecks();
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

    curProfStatus.Init();

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

    ResetPerSessionStatus();
}

// Reset those variables that is only for the current attach session
inline void ProfControlBlock::ResetPerSessionStatus()
{
    LIMITED_METHOD_CONTRACT;
    
    pProfInterface = NULL;
    dwEventMask = COR_PRF_MONITOR_NONE;
    dwEventMaskHigh = COR_PRF_HIGH_MONITOR_NONE;
}

//---------------------------------------------------------------------------------------
// Inlined helpers used throughout the runtime to check for the profiler's load status
// and what features it enabled callbacks for.
//---------------------------------------------------------------------------------------


// CORProfilerPresent() returns whether or not a CLR Profiler is actively loaded
// (meaning it's initialized and ready to receive callbacks).
inline BOOL CORProfilerPresent()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ((&g_profControlBlock)->curProfStatus.Get() == kProfStatusActive);
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

    return (CORProfilerPresent() && 
        (
         ((&g_profControlBlock)->pProfInterface->GetFunctionIDMapper()  != NULL) || 
         ((&g_profControlBlock)->pProfInterface->GetFunctionIDMapper2() != NULL)
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_JIT_COMPILATION));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_CACHE_SEARCHES));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_MODULE_LOADS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_ASSEMBLY_LOADS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_APPDOMAIN_LOADS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_THREADS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_CLASS_LOADS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_GC));
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
                ((&g_profControlBlock)->dwEventMask & COR_PRF_ENABLE_OBJECT_ALLOCATED))
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_OBJECT_ALLOCATED));
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
            ((&g_profControlBlock)->dwEventMaskHigh & COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED));
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

    return (CORProfilerPresent() &&
            ((&g_profControlBlock)->dwEventMask & COR_PRF_ENABLE_REJIT));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_EXCEPTIONS));
}

inline BOOL CORProfilerTrackCLRExceptions()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_CLR_EXCEPTIONS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_CODE_TRANSITIONS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_ENTERLEAVE));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_CCW));
}

inline BOOL CORProfilerTrackRemoting()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_REMOTING));
}

inline BOOL CORProfilerTrackRemotingCookie()
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
        (((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_REMOTING_COOKIE)
                             == COR_PRF_MONITOR_REMOTING_COOKIE));
}

inline BOOL CORProfilerTrackRemotingAsync()
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
        (((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_REMOTING_ASYNC)
                             == COR_PRF_MONITOR_REMOTING_ASYNC));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_MONITOR_SUSPENDS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_DISABLE_INLINING));
}

inline BOOL CORProfilerJITMapEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
            ((&g_profControlBlock)->dwEventMask & COR_PRF_ENABLE_JIT_MAPS));
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
            ((&g_profControlBlock)->dwEventMask & COR_PRF_DISABLE_OPTIMIZATIONS));
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

    if (!CORProfilerPresent())
        return FALSE;

    if (((&g_profControlBlock)->dwEventMask & 
            COR_PRF_REQUIRE_PROFILE_IMAGE) == 0)
        return FALSE;

    return TRUE;
}

inline BOOL CORProfilerDisableAllNGenImages()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (CORProfilerPresent() &&
            ((&g_profControlBlock)->dwEventMask & COR_PRF_DISABLE_ALL_NGEN_IMAGES));
}

inline BOOL CORProfilerTrackConditionalWeakTableElements()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return CORProfilerTrackGC() && (&g_profControlBlock)->pProfInterface->IsCallback5Supported();
}

// CORProfilerPresentOrInitializing() returns nonzero iff a CLR Profiler is actively
// loaded and ready to receive callbacks OR a CLR Profiler has loaded just enough that it
// is ready to receive (or is currently executing inside) its Initialize() callback. 
// Typically, you'll want to use code:CORProfilerPresent instead of this.  But there is
// some internal profiling API code that wants to test for event flags for a profiler
// that may still be initializing, and this function is appropriate for that code.
inline BOOL CORProfilerPresentOrInitializing()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return ((&g_profControlBlock)->curProfStatus.Get() > kProfStatusDetaching);
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
        ((&g_profControlBlock)->dwEventMask & (COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO)));
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
        ((&g_profControlBlock)->dwEventMask & (COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FRAME_INFO)));
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
        ((&g_profControlBlock)->dwEventMask & (COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO)));
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
        ((&g_profControlBlock)->dwEventMask & (COR_PRF_ENABLE_FRAME_INFO)));
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
        (!((&g_profControlBlock)->dwEventMask & (COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FUNCTION_ARGS | COR_PRF_ENABLE_FRAME_INFO))));
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
        (!((&g_profControlBlock)->dwEventMask & (COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FUNCTION_RETVAL | COR_PRF_ENABLE_FRAME_INFO))));
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
        (!((&g_profControlBlock)->dwEventMask & (COR_PRF_ENABLE_STACK_SNAPSHOT | COR_PRF_ENABLE_FRAME_INFO))));
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
        ((&g_profControlBlock)->dwEventMask & COR_PRF_ENABLE_FUNCTION_ARGS));
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
        ((&g_profControlBlock)->dwEventMask & COR_PRF_ENABLE_FUNCTION_RETVAL));
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
        ((&g_profControlBlock)->dwEventMask & COR_PRF_ENABLE_FRAME_INFO));
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
        ((&g_profControlBlock)->dwEventMask & COR_PRF_ENABLE_STACK_SNAPSHOT));
}

inline BOOL CORProfilerAddsAssemblyReferences()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() && 
        ((&g_profControlBlock)->dwEventMaskHigh & COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES));
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
        ((&g_profControlBlock)->dwEventMaskHigh & COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED));
}

inline BOOL CORProfilerIsMonitoringDynamicFunctionUnloads()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return (CORProfilerPresent() &&
        ((&g_profControlBlock)->dwEventMaskHigh & COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS));
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
         ((&g_profControlBlock)->dwEventMaskHigh & COR_PRF_HIGH_DISABLE_TIERED_COMPILATION));
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
         ((&g_profControlBlock)->dwEventMaskHigh & COR_PRF_HIGH_BASIC_GC));
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
         ((&g_profControlBlock)->dwEventMaskHigh & COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS));
}

#if defined(PROFILING_SUPPORTED) && !defined(CROSSGEN_COMPILE)

#if defined(FEATURE_PROFAPI_ATTACH_DETACH)

//---------------------------------------------------------------------------------------
// When EE calls into the profiler, an EvacuationCounterHolder object is instantiated on
// the stack to increment the evacuation counter inside the EE Thread. Upon returning to
// EE, this EvacuationCounterHolder object when being destroyed decreases the evacuation
// counter by one.
// 
// Do not use this object directly.  Use BEGIN_PIN_PROFILER / END_PIN_PROFILER defined
// below.
// 
// See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization.
// 
typedef Wrapper<Thread *, ProfilingAPIUtility::IncEvacuationCounter, ProfilingAPIUtility::DecEvacuationCounter, 
    (UINT_PTR)0, CompareDefault<Thread *>> EvacuationCounterHolder;


//---------------------------------------------------------------------------------------
// These macros must be placed around any access to g_profControlBlock.pProfInterface by
// the EE. Example:
//    {
//        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
//        g_profControlBlock.pProfInterface->AppDomainCreationStarted(MyAppDomainID);
//        END_PIN_PROFILER();
//    }
// The parameter to the BEGIN_PIN_PROFILER is the condition you want to check for, to
// determine whether the profiler is loaded and requesting the callback you're about to
// issue. Typically, this will be a call to one of the inline functions in
// profilepriv.inl. If the condition is true, the macro will increment an evacuation
// counter that effectively pins the profiler, recheck the condition, and (if still
// true), execute whatever code you place inside the BEGIN/END_PIN_PROFILER block. If
// your condition is more complex than a simple profiler status check, then place the
// profiler status check as parameter to the macro, and add a separate if inside the
// block. Example:
//
//    {
//        BEGIN_PIN_PROFILER(CORProfilerTrackTransitions());
//        if (!pNSL->pMD->IsQCall())
//        {
//            g_profControlBlock.pProfInterface->
//                ManagedToUnmanagedTransition((FunctionID) pNSL->pMD,
//                COR_PRF_TRANSITION_CALL);
//        }
//        END_PIN_PROFILER();
//    }
//        
// This ensures that the extra condition check (in this case "if
// (!pNSL->pMD->IsQCall())") is only evaluated if the profiler is loaded. That way, we're
// not executing extra, unnecessary instructions when no profiler is present.
// 
// See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
// for more details about how the synchronization works.
#define BEGIN_PIN_PROFILER(condition)                                                       \
    /* Do a cheap check of the condition (dirty-read)                                   */  \
    if (condition)                                                                          \
    {                                                                                       \
        EvacuationCounterHolder __evacuationCounter(GetThreadNULLOk());                     \
        /* Now that the evacuation counter is incremented, the condition re-check       */  \
        /* below is a clean read.  There's no MemoryBarrier() here, but that's ok       */  \
        /* as writes to the profiler status force a FlushStoreBuffers().                */  \
        if (condition)                                                                      \
        {
#define END_PIN_PROFILER()  } }

#else // FEATURE_PROFAPI_ATTACH_DETACH

// For builds that include profiling but not attach / detach (e.g., Silverlight 2), the
// *PIN_PROFILER macros should just check the condition without using the evacuation
// counters.

#define BEGIN_PIN_PROFILER(condition)       if (condition) {
#define END_PIN_PROFILER()                  }

#endif // FEATURE_PROFAPI_ATTACH_DETACH

#else // PROFILING_SUPPORTED && !CROSSGEN_COMPILE

// Profiling feature not supported

#define BEGIN_PIN_PROFILER(condition)       if (false) {
#define END_PIN_PROFILER()                  }

#endif // PROFILING_SUPPORTED && !CROSSGEN_COMPILE

#endif // _ProfilePriv_inl_

