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
class Object;
struct ScanContext;

#if defined (PROFILING_SUPPORTED_DATA) || defined(PROFILING_SUPPORTED)
#ifndef PROFILING_SUPPORTED_DATA
#define PROFILING_SUPPORTED_DATA 1
#endif  // PROFILING_SUPPORTED_DATA

#include "corprof.h"

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

// ---------------------------------------------------------------------------------------
// Global struct that lets the EE see the load status of the profiler, and provides a
// pointer (pProfInterface) through which profiler calls can be made
//
// When you are adding new session, please refer to
// code:ProfControlBlock::ResetPerSessionStatus#ProfileResetSessionStatus for more details.
struct ProfControlBlock
{
    // **** IMPORTANT!! ****
    // All uses of pProfInterface must be properly synchronized to avoid the profiler
    // from detaching while the EE attempts to call into it.  The recommended way to do
    // this is to use the (lockless) BEGIN_PIN_PROFILER / END_PIN_PROFILER macros.  See
    // code:BEGIN_PIN_PROFILER for instructions.  For full details on how the
    // synchronization works, see
    // code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
    VolatilePtr<EEToProfInterfaceImpl> pProfInterface;
    // **** IMPORTANT!! ****

    DWORD dwEventMask;          // Original low event mask bits
    DWORD dwEventMaskHigh;      // New high event mask bits
    CurrentProfilerStatus curProfStatus;
    BOOL fGCInProgress;
    BOOL fBaseSystemClassesLoaded;

    BOOL fIsStoredProfilerRegistered;
    CLSID clsStoredProfilerGuid;
    SString sStoredProfilerPath;

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
    BOOL fConcurrentGCDisabledForAttach;

    Volatile<BOOL> fProfControlBlockInitialized;

    Volatile<BOOL> fProfilerRequestedRuntimeSuspend;

    void Init();
    void ResetPerSessionStatus();
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

