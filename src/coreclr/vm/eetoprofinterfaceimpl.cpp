// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// EEToProfInterfaceImpl.cpp
//

//
// This module implements wrappers around calling the profiler's
// ICorProfilerCallaback* interfaces. When code in the EE needs to call the
// profiler, it goes through EEToProfInterfaceImpl to do so.
//
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE!
//
// PLEASE READ!
//
// There are strict rules for how to implement ICorProfilerCallback* wrappers.  Please read
// https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/profilability.md
// to understand the rules and why they exist.
//
// As a reminder, here is a short summary of your responsibilities.  Every PUBLIC
// ENTRYPOINT (from EE to profiler) must have:
//
// - An entrypoint macro at the top.  Your choices are:
//      CLR_TO_PROFILER_ENTRYPOINT (typical choice)
//          This is used for calling ICorProfilerCallback* methods that either have no
//          ThreadID parameters, or if they do have a ThreadID parameter, the parameter's
//          value is always the *current* ThreadID (i.e., param == GetThreadNULLOk()).  This will
//          also force a mode switch to preemptive before calling the profiler.
//      CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD
//          Similar to above, except these are used for ICorProfilerCallback* methods that
//          specify a ThreadID parameter whose value may not always be the *current*
//          ThreadID.  You must specify the ThreadID as the first parameter to these
//          macros.  The macro will then use your ThreadID rather than that of the current
//          GetThreadNULLOk(), to assert that the callback is currently allowed for that
//          ThreadID (i.e., that we have not yet issued a ThreadDestroyed() for that
//          ThreadID).
//
// - A complete contract block with comments over every contract choice.  Wherever
//   possible, use the preferred contracts (if not possible, you must comment why):
//       NOTHROW
//              All callbacks are really NOTHROW, but that's enforced partially by
//              the profiler, whose try/catch blocks aren't visible to the
//              contract system.  So you'll need to put a scoped
//              PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout)
//              around the call to the profiler
//       GC_TRIGGERS
//       MODE_PREEMPTIVE (MODE_COOPERATIVE if passing an ObjectID)
//              If you use MODE_ANY, you must comment why you don't want an exact mode.
//       CAN_TAKE_LOCK
//       ASSERT_NO_EE_LOCKS_HELD()
//   Note that the preferred contracts in this file are DIFFERENT than the preferred
//   contracts for proftoeeinterfaceimpl.cpp.
//
// Private helper functions in this file do not have the same preferred contracts as
// public entrypoints, and they should be contracted following the same guidelines
// as per the rest of the EE.
//
// NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//

// ======================================================================================

#include "common.h"

#ifdef PROFILING_SUPPORTED


#include "eetoprofinterfaceimpl.h"
#include "eetoprofinterfaceimpl.inl"
#include "contract.h"
#include "proftoeeinterfaceimpl.h"
#include "proftoeeinterfaceimpl.inl"
#include "profilinghelper.inl"
#include "profdetach.h"
#include "simplerwlock.hpp"
#include "eeconfig.h"

#ifdef FEATURE_PERFTRACING
#include "eventpipeadapter.h"
#endif // FEATURE_PERFTRACING

//---------------------------------------------------------------------------------------
// Helpers

// Bitmask of flags that may be passed to the CLR_TO_PROFILER_ENTRYPOINT* macros
// to constrain when the callback may be issued
enum ClrToProfEntrypointFlags
{
    // Default
    kEE2PNone                           = 0x00000000,

    // Callback is allowable even for detaching profilers
    kEE2PAllowableWhileDetaching        = 0x00000001,

    // Callback is allowable even for initializing profilers
    kEE2PAllowableWhileInitializing     = 0x00000002,

    // Callback is made while in a GC_NOTRIGGER contract.  Whereas contracts are
    // debug-only, this flag is used in retail builds as well.
    kEE2PNoTrigger                      = 0x00000004,
};

#define ASSERT_EVAC_COUNTER_NONZERO()   \
    _ASSERTE(m_pProfilerInfo->dwProfilerEvacuationCounter.Load() > 0)

#define CHECK_PROFILER_STATUS(ee2pFlags)                                                \
    /* If one of these asserts fires, perhaps you forgot to use                     */  \
    /* BEGIN/END_PROFILER_CALLBACK                                                  */  \
    ASSERT_EVAC_COUNTER_NONZERO();                                                      \
    /* Either we are initializing, or we have the ProfToEEInterfaceImpl             */  \
    _ASSERTE((((ee2pFlags) & kEE2PAllowableWhileInitializing) != 0) || (m_pProfilerInfo->pProfInterface.Load() != NULL));   \
    /* If we are initializing, null is fine. Otherwise we want to make sure we haven't messed up the association between */ \
    /* EEToProfInterfaceImpl/ProfToEEInterfaceImpl somehow.                         */  \
    _ASSERTE((((ee2pFlags) & kEE2PAllowableWhileInitializing) != 0) || (m_pProfilerInfo->pProfInterface == this));          \
    /* Early abort if...                                                            */  \
    if (                                                                                \
        /* Profiler isn't active,                                                   */  \
        !CORProfilerPresent() &&                                                        \
                                                                                        \
        /* and it's not the case that both a) this callback is allowed              */  \
        /* on a detaching profiler, and b) the profiler is detaching                */  \
        !(                                                                              \
            (((ee2pFlags) & kEE2PAllowableWhileDetaching) != 0) &&                      \
            (m_pProfilerInfo->curProfStatus.Get() == kProfStatusDetaching)            \
         ) &&                                                                           \
                                                                                        \
        /* and it's not the case that both a) this callback is allowed              */  \
        /* on an initializing profiler, and b) the profiler is initializing         */  \
        !(                                                                              \
            (((ee2pFlags) & kEE2PAllowableWhileInitializing) != 0) &&                   \
            (                                                                           \
              (m_pProfilerInfo->curProfStatus.Get()                                   \
                  == kProfStatusInitializingForStartupLoad) ||                          \
              (m_pProfilerInfo->curProfStatus.Get()                                   \
                  == kProfStatusInitializingForAttachLoad)                              \
            )                                                                           \
         )                                                                              \
       )                                                                                \
    {                                                                                   \
        return S_OK;                                                                    \
    }

// Least common denominator for the callback wrappers.  Logs, records in EE Thread object
// that we're in a callback, and asserts that we're allowed to issue callbacks for the
// specified ThreadID (i.e., no ThreadDestroyed callback has been issued for the
// ThreadID).
//
#define CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD_EX(ee2pFlags, threadId, logParams)        \
    INCONTRACT(AssertTriggersContract(!((ee2pFlags) & kEE2PNoTrigger)));                \
    CHECK_PROFILER_STATUS(ee2pFlags);                                                   \
    LOG(logParams);                                                                     \
    _ASSERTE(m_pCallback2 != NULL);                                                     \
    /* Normally, set COR_PRF_CALLBACKSTATE_INCALLBACK |                              */ \
    /* COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE in the callback state, but omit       */ \
    /* COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE if we're in a GC_NOTRIGGERS callback  */ \
    SetCallbackStateFlagsHolder __csf(                                                  \
        (((ee2pFlags) & kEE2PNoTrigger) != 0) ?                                         \
            COR_PRF_CALLBACKSTATE_INCALLBACK :                                          \
            COR_PRF_CALLBACKSTATE_INCALLBACK | COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE  \
        );                                                                              \
    _ASSERTE(ProfilerCallbacksAllowedForThread((Thread *) (threadId)))

#define CLR_TO_PROFILER_ENTRYPOINT_EX(ee2pFlags, logParams)                             \
    CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD_EX(ee2pFlags, GetThreadNULLOk(), logParams)

// Typical entrypoint macro you'll use. Checks that we're allowed to issue
// callbacks for the current thread (i.e., no ThreadDestroyed callback has been
// issued for the current thread).
#define CLR_TO_PROFILER_ENTRYPOINT(logParams)                                           \
        CLR_TO_PROFILER_ENTRYPOINT_EX(kEE2PNone, logParams)
#define CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD(threadId, logParams)                      \
        CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD_EX(kEE2PNone, threadId, logParams)


//---------------------------------------------------------------------------------------
//
// Wrapper around Thread::ProfilerCallbacksAllowed
//
// Arguments:
//      pThread - Thread on which we need to determine whether callbacks are allowed
//
// Return Value:
//      TRUE if the profiler portion has marked this thread as allowable, else FALSE.
//

inline BOOL ProfilerCallbacksAllowedForThread(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    return ((pThread == NULL) || (pThread->ProfilerCallbacksAllowed()));
}


//---------------------------------------------------------------------------------------
//
// Wrapper around Thread::SetProfilerCallbacksAllowed
//
// Arguments:
//      pThread - Thread on which we're setting whether callbacks shall be allowed
//      fValue - The value to store.
//

inline void SetProfilerCallbacksAllowedForThread(Thread * pThread, BOOL fValue)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pThread != NULL);
    pThread->SetProfilerCallbacksAllowed(fValue);
}


//---------------------------------------------------------------------------------------
//
// Low-level function to find and CoCreateInstance the profiler's DLL. Called when
// initializing via EEToProfInterfaceImpl::Init()
//
// Arguments:
//      * pClsid - [in] Profiler's CLSID
//      * wszClsid - [in] String form of CLSID or progid of profiler to load.
//      * wszProfileDLL - [in] Path to profiler DLL
//      * ppCallback - [out] Pointer to profiler's ICorProfilerCallback2 interface
//      * phmodProfilerDLL - [out] HMODULE of profiler's DLL.
//
// Return Value:
//    HRESULT indicating success or failure.
//
// Notes:
//     * This function (or one of its callees) will log an error to the event log if
//         there is a failure

static HRESULT CoCreateProfiler(
    const CLSID * pClsid,
    __in_z LPCWSTR wszClsid,
    __in_z LPCWSTR wszProfileDLL,
    ICorProfilerCallback2 ** ppCallback,
    HMODULE * phmodProfilerDLL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

    } CONTRACTL_END;

    _ASSERTE(pClsid != NULL);
    _ASSERTE(wszClsid != NULL);
    _ASSERTE(ppCallback != NULL);
    _ASSERTE(phmodProfilerDLL != NULL);

    LOG((LF_CORPROF, LL_INFO10, "**PROF: Entered CoCreateProfiler.\n"));

    HRESULT hr;
    *phmodProfilerDLL = NULL;

    // This is the ICorProfilerCallback2 ptr we get back from the profiler's class
    // factory's CreateInstance()
    ReleaseHolder<ICorProfilerCallback2> pCallback2FromCreateInstance;

    // This is the ICorProfilerCallback2 ptr we get back from the profiler's QI (see its
    // first use below for an explanation on why this is necessary).
    ReleaseHolder<ICorProfilerCallback2> pCallback2FromQI;

    // Create an instance of the profiler
    hr = FakeCoCreateInstanceEx(*pClsid,
                                wszProfileDLL,
                                IID_ICorProfilerCallback2,
                                (LPVOID *) &pCallback2FromCreateInstance,
                                phmodProfilerDLL);

    // (pCallback2FromCreateInstance == NULL) should be considered an error!
    if ((pCallback2FromCreateInstance == NULL) && SUCCEEDED(hr))
    {
        hr = E_NOINTERFACE;
    }

    if (hr == E_NOINTERFACE)
    {
        // Helpful message for a potentially common problem
        ProfilingAPIUtility::LogNoInterfaceError(IID_ICorProfilerCallback2, wszClsid);
    }
    else if (hr == CORPROF_E_PROFILER_CANCEL_ACTIVATION)
    {
        // Profiler didn't encounter a bad error, but is voluntarily choosing not to
        // profile this runtime.  Profilers that need to set system environment
        // variables to be able to profile services may use this HRESULT to avoid
        // profiling all the other managed apps on the box.
        ProfilingAPIUtility::LogProfInfo(IDS_PROF_CANCEL_ACTIVATION, wszClsid);
    }
    else if (FAILED(hr))
    {
        // Catch-all error for other CoCreateInstance failures
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_CCI_FAILED, wszClsid, hr);
    }

    // Now that hr is normalized (set to error if pCallback2FromCreateInstance == NULL),
    // LOG and abort if there was a problem.
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF,
            LL_INFO10,
            "**PROF: Unable to CoCreateInstance profiler class %S.  hr=0x%x.\n",
            wszClsid,
            hr));
        return hr;
    }

    // Redundantly QI for ICorProfilerCallback2.  This keeps CLR behavior consistent
    // with Whidbey, and works around the following bug in some profilers' class factory
    // CreateInstance:
    //     * CreateInstance() ignores the IID it's given
    //     * CreateInstance() returns a pointer to the object it created, even though
    //         that object might not support the IID passed to CreateInstance().
    // Whidbey CLR worked around this problem by redundantly QI'ing for the same IID
    // again after CreateInstance() returned.  In this redudant QI, the profiler code would
    // finally realize it didn't support that IID, and return an error there.  Without
    // the redundant QI, the CLR would accept what it got from CreateInstance(), and
    // start calling into it using the unsupported interface's vtable, which would
    // cause an AV.
    //
    // There were many MSDN samples (for example
    // http://msdn.microsoft.com/msdnmag/issues/03/01/NETProfilerAPI/) which
    // unfortunately had this CreateInstance() bug, so many profilers might have been
    // generated based on this code.  Since it's easy & cheap to work around the
    // problem, we do so here with the redundant QI.
    hr = pCallback2FromCreateInstance->QueryInterface(
        IID_ICorProfilerCallback2,
        (LPVOID *) &pCallback2FromQI);

    // (pCallback2FromQI == NULL) should be considered an error!
    if ((pCallback2FromQI == NULL) && SUCCEEDED(hr))
    {
        hr = E_NOINTERFACE;
    }

    // Any error at this stage implies IID_ICorProfilerCallback2 is not supported
    if (FAILED(hr))
    {
        // Helpful message for a potentially common problem
        ProfilingAPIUtility::LogNoInterfaceError(IID_ICorProfilerCallback2, wszClsid);
        return hr;
    }

    // Ok, safe to transfer ownership to caller's [out] param
    *ppCallback = pCallback2FromQI.Extract();
    pCallback2FromQI = NULL;

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Implementation of CHashTableImpl functions.  This class a simple implementation of
// CHashTable to provide a very simple implementation of the Cmp pure virtual function
//

EEToProfInterfaceImpl::CHashTableImpl::CHashTableImpl(ULONG iBuckets)
    : CHashTable(iBuckets)
{
    WRAPPER_NO_CONTRACT;
}

EEToProfInterfaceImpl::CHashTableImpl::~CHashTableImpl()
{
    WRAPPER_NO_CONTRACT;
}

//---------------------------------------------------------------------------------------
//
// Comparison function for hash table of ClassIDs
//
// Arguments:
//      pc1 - hash key to compare
//      pc2 - hash value to compare
//
// Return Value:
//      TRUE if the key & value refer to the same ClassID; otherwise FALSE
//

BOOL EEToProfInterfaceImpl::CHashTableImpl::Cmp(SIZE_T k1, const HASHENTRY * pc2)
{
    LIMITED_METHOD_CONTRACT;

    ClassID key = (ClassID) k1;
    ClassID val = ((CLASSHASHENTRY *)pc2)->m_clsId;

    return (key != val);
}


//---------------------------------------------------------------------------------------
// Private maintenance functions for initialization, cleanup, etc.

EEToProfInterfaceImpl::AllocByClassData *EEToProfInterfaceImpl::m_pSavedAllocDataBlock = NULL;

//---------------------------------------------------------------------------------------
//
// EEToProfInterfaceImpl ctor just sets initial values
//

EEToProfInterfaceImpl::EEToProfInterfaceImpl() :
    m_pCallback2(NULL),
    m_pCallback3(NULL),
    m_pCallback4(NULL),
    m_pCallback5(NULL),
    m_pCallback6(NULL),
    m_pCallback7(NULL),
    m_pCallback8(NULL),
    m_pCallback9(NULL),
    m_pCallback10(NULL),
    m_pCallback11(NULL),
    m_hmodProfilerDLL(NULL),
    m_fLoadedViaAttach(FALSE),
    m_pProfToEE(NULL),
    m_pProfilersFuncIDMapper(NULL),
    m_pProfilersFuncIDMapper2(NULL),
    m_pProfilersFuncIDMapper2ClientData(NULL),
    m_pGCRefDataFreeList(NULL),
    m_csGCRefDataFreeList(NULL),
    m_pEnter(NULL),
    m_pLeave(NULL),
    m_pTailcall(NULL),
    m_pEnter2(NULL),
    m_pLeave2(NULL),
    m_pTailcall2(NULL),
    m_fIsClientIDToFunctionIDMappingEnabled(TRUE),
    m_pEnter3(NULL),
    m_pLeave3(NULL),
    m_pTailcall3(NULL),
    m_pEnter3WithInfo(NULL),
    m_pLeave3WithInfo(NULL),
    m_pTailcall3WithInfo(NULL),
    m_fUnrevertiblyModifiedIL(FALSE),
    m_fModifiedRejitState(FALSE),
    m_pProfilerInfo(NULL),
    m_pFunctionIDHashTable(NULL),
    m_pFunctionIDHashTableRWLock(NULL),
    m_dwConcurrentGCWaitTimeoutInMs(INFINITE),
    m_bHasTimedOutWaitingForConcurrentGC(FALSE)
{
    // Also NULL out this static.  (Note: consider making this a member variable.)
    m_pSavedAllocDataBlock = NULL;
    LIMITED_METHOD_CONTRACT;
}

//
//---------------------------------------------------------------------------------------
//
// Post-constructor initialization of EEToProfInterfaceImpl. Sets everything up,
// including creating the profiler.
//
// Parameters:
//      * pProfToEE - A newly-created ProfToEEInterfaceImpl instance that will be passed
//          to the profiler as the ICorProfilerInfo3 interface implementation.
//      * pClsid - Profiler's CLSID
//      * wszClsid - String form of CLSID or progid of profiler to load
//      * wszProfileDLL - Path to profiler DLL
//      * fLoadedViaAttach - TRUE iff the profiler is being attach-loaded (else
//             profiler is being startup-loaded)
//
// Return Value:
//      HRESULT indicating success or failure.
//
// Notes:
//      This function (or one of its callees) will log an error to the event log if there
//      is a failure
//


HRESULT EEToProfInterfaceImpl::Init(
    ProfToEEInterfaceImpl * pProfToEE,
    const CLSID * pClsid,
    __in_z LPCWSTR wszClsid,
    __in_z LPCWSTR wszProfileDLL,
    BOOL fLoadedViaAttach,
    DWORD dwConcurrentGCWaitTimeoutInMs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;

    _ASSERTE(pProfToEE != NULL);

    m_fLoadedViaAttach = fLoadedViaAttach;
    m_dwConcurrentGCWaitTimeoutInMs = dwConcurrentGCWaitTimeoutInMs;

    // The rule sez your Crst should switch to preemptive when it's taken.  We intentionally
    // break this rule with CRST_UNSAFE_ANYMODE, because this Crst is taken DURING A GC
    // (see AllocateMovedReferencesData(), called by MovedReference(), called by the GC),
    // and we don't want to be switching modes in the middle of a GC!  Indeed, on server there
    // may not even be a mode in the first place.
    CRITSEC_AllocationHolder csGCRefDataFreeList(ClrCreateCriticalSection(CrstProfilerGCRefDataFreeList, CRST_UNSAFE_ANYMODE));
    if (csGCRefDataFreeList == NULL)
    {
        LOG((LF_CORPROF,
            LL_ERROR,
            "**PROF: Failed to create Crst during initialization.\n"));

        // A specialized event log entry for this failure would be confusing and
        // unhelpful.  So just log a generic internal failure event
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_INTERNAL_INIT, wszClsid, E_FAIL);
        return E_FAIL;
    }

    // CEEInfo::GetProfilingHandle will be PREEMPTIVE mode when trying to update
    // m_pFunctionIDHashTable while ProfileEnter, ProfileLeave and ProfileTailcall
    // and LookupClientIDFromCache all will be in COOPERATIVE mode when trying
    // to read m_pFunctionIDHashTable, so pFunctionIDHashTableRWLock must be created
    // with COOPERATIVE_OR_PREEMPTIVE.  It is safe to so do because FunctionIDHashTable,
    // synchronized by m_pFunctionIDHashTableRWLock runs only native code and uses
    // only native heap.
    NewHolder<SimpleRWLock> pFunctionIDHashTableRWLock(new (nothrow) SimpleRWLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT));

    NewHolder<FunctionIDHashTable> pFunctionIDHashTable(new (nothrow) FunctionIDHashTable());

    if ((pFunctionIDHashTable == NULL) || (pFunctionIDHashTableRWLock == NULL))
    {
        LOG((LF_CORPROF,
            LL_ERROR,
            "**PROF: Failed to create FunctionIDHashTable or FunctionIDHashTableRWLock during initialization.\n"));

        // A specialized event log entry for this failure would be confusing and
        // unhelpful.  So just log a generic internal failure event
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_INTERNAL_INIT, wszClsid, E_OUTOFMEMORY);

        return E_OUTOFMEMORY;
    }

    // This wraps the following profiler calls in a try / catch:
    // * ClassFactory::CreateInstance
    // * AddRef/Release/QueryInterface
    // Although most profiler calls are not protected, these creation calls are
    // protected here since it's cheap to do so (this is only done once per load of a
    // profiler), and it would be nice to avoid tearing down the entire process when
    // attaching a profiler that may pass back bogus vtables.
    EX_TRY
    {
        // CoCreate the profiler (but don't call its Initialize() method yet)
        hr = CreateProfiler(pClsid, wszClsid, wszProfileDLL);
    }
    EX_CATCH
    {
        hr = E_UNEXPECTED;
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_UNHANDLED_EXCEPTION_ON_LOAD, wszClsid);
    }
    // Intentionally swallowing all exceptions, as we don't want a poorly-written
    // profiler that throws or AVs on attach to cause the entire process to go away.
    EX_END_CATCH(SwallowAllExceptions);


    if (FAILED(hr))
    {
        // CreateProfiler (or catch clause above) has already logged an event to the
        // event log on failure
        return hr;
    }

    m_pProfToEE = pProfToEE;

    m_csGCRefDataFreeList = csGCRefDataFreeList.Extract();
    csGCRefDataFreeList = NULL;

    m_pFunctionIDHashTable = pFunctionIDHashTable.Extract();
    pFunctionIDHashTable = NULL;

    m_pFunctionIDHashTableRWLock = pFunctionIDHashTableRWLock.Extract();
    pFunctionIDHashTableRWLock = NULL;

    return S_OK;
}

void EEToProfInterfaceImpl::SetProfilerInfo(ProfilerInfo *pProfilerInfo)
{
    LIMITED_METHOD_CONTRACT;
    m_pProfilerInfo = pProfilerInfo;
    m_pProfToEE->SetProfilerInfo(pProfilerInfo);
}

//---------------------------------------------------------------------------------------
//
// This is used by Init() to load the user-specified profiler (but not to call
// its Initialize() method).
//
// Arguments:
//      pClsid - Profiler's CLSID
//      wszClsid - String form of CLSID or progid of profiler to load
//      wszProfileDLL - Path to profiler DLL
//
// Return Value:
//    HRESULT indicating success / failure.  If this is successful, m_pCallback2 will be
//    set to the profiler's ICorProfilerCallback2 interface on return.  m_pCallback3,4
//    will be set to the profiler's ICorProfilerCallback3 interface on return if
//    ICorProfilerCallback3,4 is supported.
//
// Assumptions:
//    Although the profiler has not yet been instantiated, it is assumed that the internal
//    profiling API structures have already been created
//
// Notes:
//    This function (or one of its callees) will log an error to the event log
//    if there is a failure

HRESULT EEToProfInterfaceImpl::CreateProfiler(
    const CLSID * pClsid,
    __in_z LPCWSTR wszClsid,
    __in_z LPCWSTR wszProfileDLL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Always called before Thread created.
    _ASSERTE(GetThreadNULLOk() == NULL);

    // Try and CoCreate the registered profiler
    ReleaseHolder<ICorProfilerCallback2> pCallback2;
    HModuleHolder hmodProfilerDLL;
    HRESULT hr = CoCreateProfiler(
        pClsid,
        wszClsid,
        wszProfileDLL,
        &pCallback2,
        &hmodProfilerDLL);
    if (FAILED(hr))
    {
        // CoCreateProfiler logs events to the event log on failures
        return hr;
    }

    // CoCreateProfiler ensures that if it succeeds, we get some valid pointers
    _ASSERTE(pCallback2 != NULL);
    _ASSERTE(hmodProfilerDLL != NULL);

    // Save profiler pointers into this.  The reference ownership now
    // belongs to this class, so NULL out locals without allowing them to release
    m_pCallback2 = pCallback2.Extract();
    pCallback2 = NULL;
    m_hmodProfilerDLL = hmodProfilerDLL.Extract();
    hmodProfilerDLL = NULL;

    // ATTENTION: Please update EEToProfInterfaceImpl::~EEToProfInterfaceImpl() after adding the next ICorProfilerCallback interface here !!!

    // The profiler may optionally support ICorProfilerCallback3,4,5,6,7,8,9,10,11.  Let's check.
    ReleaseHolder<ICorProfilerCallback11> pCallback11;
    hr = m_pCallback2->QueryInterface(
        IID_ICorProfilerCallback11,
        (LPVOID *)&pCallback11);
    if (SUCCEEDED(hr) && (pCallback11 != NULL))
    {
        _ASSERTE(m_pCallback11 == NULL);
        m_pCallback11 = pCallback11.Extract();
        pCallback11 = NULL;
    }

    if (m_pCallback11 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback10> pCallback10;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback10,
            (LPVOID *)&pCallback10);
        if (SUCCEEDED(hr) && (pCallback10 != NULL))
        {
            _ASSERTE(m_pCallback10 == NULL);
            m_pCallback10 = pCallback10.Extract();
            pCallback10 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback10 == NULL);
        m_pCallback10 = static_cast<ICorProfilerCallback10 *>(m_pCallback11);
        m_pCallback10->AddRef();
    }


    // Due to inheritance, if we have an interface we must also have
    // all the previous versions
    if (m_pCallback10 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback9> pCallback9;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback9,
            (LPVOID *)&pCallback9);
        if (SUCCEEDED(hr) && (pCallback9 != NULL))
        {
            _ASSERTE(m_pCallback9 == NULL);
            m_pCallback9 = pCallback9.Extract();
            pCallback9 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback9 == NULL);
        m_pCallback9 = static_cast<ICorProfilerCallback9 *>(m_pCallback10);
        m_pCallback9->AddRef();
    }

    if (m_pCallback9 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback8> pCallback8;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback8,
            (LPVOID *)&pCallback8);
        if (SUCCEEDED(hr) && (pCallback8 != NULL))
        {
            _ASSERTE(m_pCallback8 == NULL);
            m_pCallback8 = pCallback8.Extract();
            pCallback8 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback8 == NULL);
        m_pCallback8 = static_cast<ICorProfilerCallback8 *>(m_pCallback9);
        m_pCallback8->AddRef();
    }

    if (m_pCallback8 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback7> pCallback7;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback7,
            (LPVOID *)&pCallback7);
        if (SUCCEEDED(hr) && (pCallback7 != NULL))
        {
            _ASSERTE(m_pCallback7 == NULL);
            m_pCallback7 = pCallback7.Extract();
            pCallback7 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback7 == NULL);
        m_pCallback7 = static_cast<ICorProfilerCallback7 *>(m_pCallback8);
        m_pCallback7->AddRef();
    }

    if (m_pCallback7 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback6> pCallback6;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback6,
            (LPVOID *)&pCallback6);
        if (SUCCEEDED(hr) && (pCallback6 != NULL))
        {
            _ASSERTE(m_pCallback6 == NULL);
            m_pCallback6 = pCallback6.Extract();
            pCallback6 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback6 == NULL);
        m_pCallback6 = static_cast<ICorProfilerCallback6 *>(m_pCallback7);
        m_pCallback6->AddRef();
    }

    if (m_pCallback6 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback5> pCallback5;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback5,
            (LPVOID *)&pCallback5);
        if (SUCCEEDED(hr) && (pCallback5 != NULL))
        {
            _ASSERTE(m_pCallback5 == NULL);
            m_pCallback5 = pCallback5.Extract();
            pCallback5 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback5 == NULL);
        m_pCallback5 = static_cast<ICorProfilerCallback5 *>(m_pCallback6);
        m_pCallback5->AddRef();
    }

    if (m_pCallback5 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback4> pCallback4;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback4,
            (LPVOID *)&pCallback4);
        if (SUCCEEDED(hr) && (pCallback4 != NULL))
        {
            _ASSERTE(m_pCallback4 == NULL);
            m_pCallback4 = pCallback4.Extract();
            pCallback4 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback4 == NULL);
        m_pCallback4 = static_cast<ICorProfilerCallback4 *>(m_pCallback5);
        m_pCallback4->AddRef();
    }

    if (m_pCallback4 == NULL)
    {
        ReleaseHolder<ICorProfilerCallback3> pCallback3;
        hr = m_pCallback2->QueryInterface(
            IID_ICorProfilerCallback3,
            (LPVOID *)&pCallback3);
        if (SUCCEEDED(hr) && (pCallback3 != NULL))
        {
            _ASSERTE(m_pCallback3 == NULL);
            m_pCallback3 = pCallback3.Extract();
            pCallback3 = NULL;
        }
    }
    else
    {
        _ASSERTE(m_pCallback3 == NULL);
        m_pCallback3 = static_cast<ICorProfilerCallback3 *>(m_pCallback4);
        m_pCallback3->AddRef();
    }

    return S_OK;
}




//---------------------------------------------------------------------------------------
//
// Performs cleanup for EEToProfInterfaceImpl, including releasing the profiler's
// callback interface.  Called on termination of a profiler connection.
//

EEToProfInterfaceImpl::~EEToProfInterfaceImpl()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        // When we release the profiler's callback interface
        // below, it may well perform cleanup that takes locks.
        // Example:  profiler may release a metadata interface, which
        // causes it to take a reader lock
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Make sure there's no pointer about to dangle once we disappear.
    // FUTURE: For reattach-with-neutered-profilers feature crew, change this assert to
    // scan through list of detaching profilers to make sure none of them give a
    // GetEEToProfPtr() equal to this
#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    _ASSERTE(!ProfilingAPIDetach::IsEEToProfPtrRegisteredForDetach(this));
#endif // FEATURE_PROFAPI_ATTACH_DETACH

    // Release user-specified profiler DLL
    // NOTE: If we're tearing down the process, then do nothing related
    // to cleaning up the profiler DLL, as the DLL may no longer
    // be present.
    if (!IsAtProcessExit())
    {
        if (m_pCallback2 != NULL)
        {
            m_pCallback2->Release();
            m_pCallback2 = NULL;
        }

        BOOL fIsV4Profiler = (m_pCallback3 != NULL);

        if (fIsV4Profiler)
        {
            m_pCallback3->Release();
            m_pCallback3 = NULL;
        }

        if (m_pCallback4 != NULL)
        {
            m_pCallback4->Release();
            m_pCallback4 = NULL;
        }

        if (m_pCallback5 != NULL)
        {
            m_pCallback5->Release();
            m_pCallback5 = NULL;
        }

        if (m_pCallback6 != NULL)
        {
            m_pCallback6->Release();
            m_pCallback6 = NULL;
        }

        if (m_pCallback7 != NULL)
        {
            m_pCallback7->Release();
            m_pCallback7 = NULL;
        }

        if (m_pCallback8 != NULL)
        {
            m_pCallback8->Release();
            m_pCallback8 = NULL;
        }

        if (m_pCallback9 != NULL)
        {
            m_pCallback9->Release();
            m_pCallback9 = NULL;
        }

        if (m_pCallback10 != NULL)
        {
            m_pCallback10->Release();
            m_pCallback10 = NULL;
        }

        if (m_pCallback11 != NULL)
        {
            m_pCallback11->Release();
            m_pCallback11 = NULL;
        }

        // Only unload the V4 profiler if this is not part of shutdown.  This protects
        // Whidbey profilers that aren't used to being FreeLibrary'd.
        if (fIsV4Profiler && !g_fEEShutDown)
        {
            if (m_hmodProfilerDLL != NULL)
            {
                FreeLibrary(m_hmodProfilerDLL);
                m_hmodProfilerDLL = NULL;
            }

            // Now that the profiler is destroyed, it is no longer referencing our
            // ProfToEEInterfaceImpl, so it's safe to destroy that, too.
            if (m_pProfToEE != NULL)
            {
                delete m_pProfToEE;
                m_pProfToEE = NULL;
            }
        }
    }

    // Delete the structs associated with GC moved references
    while (m_pGCRefDataFreeList)
    {
        GCReferencesData * pDel = m_pGCRefDataFreeList;
        m_pGCRefDataFreeList = m_pGCRefDataFreeList->pNext;
        delete pDel;
    }

    if (m_pSavedAllocDataBlock)
    {
#ifdef HOST_64BIT
        _ASSERTE((UINT_PTR)m_pSavedAllocDataBlock != 0xFFFFFFFFFFFFFFFF);
#else
        _ASSERTE((UINT_PTR)m_pSavedAllocDataBlock != 0xFFFFFFFF);
#endif

        _ASSERTE(m_pSavedAllocDataBlock->pHashTable != NULL);
        // Get rid of the hash table
        if (m_pSavedAllocDataBlock->pHashTable)
            delete m_pSavedAllocDataBlock->pHashTable;

        // Get rid of the two arrays used to hold class<->numinstance info
        if (m_pSavedAllocDataBlock->cLength != 0)
        {
            _ASSERTE(m_pSavedAllocDataBlock->arrClsId != NULL);
            _ASSERTE(m_pSavedAllocDataBlock->arrcObjects != NULL);

            delete [] m_pSavedAllocDataBlock->arrClsId;
            delete [] m_pSavedAllocDataBlock->arrcObjects;
        }

        // Get rid of the hash array used by the hash table
        if (m_pSavedAllocDataBlock->arrHash)
        {
            delete [] m_pSavedAllocDataBlock->arrHash;
        }

        m_pSavedAllocDataBlock = NULL;
    }

    if (m_csGCRefDataFreeList != NULL)
    {
        ClrDeleteCriticalSection(m_csGCRefDataFreeList);
        m_csGCRefDataFreeList = NULL;
    }

    if (m_pFunctionIDHashTable != NULL)
    {
        delete m_pFunctionIDHashTable;
        m_pFunctionIDHashTable = NULL;
    }

    if (m_pFunctionIDHashTableRWLock != NULL)
    {
        delete m_pFunctionIDHashTableRWLock;
        m_pFunctionIDHashTableRWLock = NULL;
    }
}

//---------------------------------------------------------------------------------------
//
// Wrapper around calling profiler's FunctionIDMapper hook.  Called by JIT.
//
// Arguments:
//      funcId - FunctionID for profiler to map
//      pbHookFunction - [out] Specifies whether the profiler wants to hook (enter/leave)
//                             this function
//
// Return Value:
//      The profiler-specified value that we should use to identify this function
//      in future hooks (enter/leave).
//      If the remapped ID returned by the profiler is NULL, we will replace it with
//      funcId.  Thus, this function will never return NULL.
//

UINT_PTR EEToProfInterfaceImpl::EEFunctionIDMapper(FunctionID funcId, BOOL * pbHookFunction)
{
    // This isn't a public callback via ICorProfilerCallback*, but it's close (a
    // public callback via a function pointer).  So we'll aim to have the preferred
    // contracts here.
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // ListLockEntry typically held during this callback (thanks to
        // MethodTable::DoRunClassInitThrowing).

    }
    CONTRACTL_END;

    // only called when CORProfilerFunctionIDMapperEnabled() is true,
    // which means either m_pProfilersFuncIDMapper or m_pProfilersFuncIDMapper2 should not be NULL;
    _ASSERTE((m_pProfilersFuncIDMapper != NULL) || (m_pProfilersFuncIDMapper2 != NULL));

    UINT_PTR clientId = NULL;

    if (m_pProfilersFuncIDMapper2 != NULL)
    {
        CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                    LL_INFO100,
                                    "**PROF: Calling profiler's FunctionIDMapper2. funcId: 0x%p. clientData: 0x%p.\n",
                                    funcId,
                                    m_pProfilersFuncIDMapper2ClientData));

        // The attached profiler may not want to hook this function, so ask it
        clientId = m_pProfilersFuncIDMapper2(funcId, m_pProfilersFuncIDMapper2ClientData, pbHookFunction);

    }
    else
    {
        CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                    LL_INFO100,
                                    "**PROF: Calling profiler's FunctionIDMapper. funcId: 0x%p.\n",
                                    funcId));

        // The attached profiler may not want to hook this function, so ask it
        clientId = m_pProfilersFuncIDMapper(funcId, pbHookFunction);
    }

    static LONG s_lIsELT2Enabled = -1;
    if (s_lIsELT2Enabled == -1)
    {
        LONG lEnabled = ((m_pEnter2    != NULL) ||
                         (m_pLeave2    != NULL) ||
                         (m_pTailcall2 != NULL));

        InterlockedCompareExchange(&s_lIsELT2Enabled, lEnabled, -1);
    }

    // We need to keep track the mapping between ClientID and FunctionID for ELT2
    if (s_lIsELT2Enabled != 0)
    {
        FunctionIDAndClientID functionIDAndClientID;
        functionIDAndClientID.functionID = funcId;
        functionIDAndClientID.clientID   = clientId;

        // ClientID Hash table may throw OUTOFMEMORY exception, which is not expected by the caller.
        EX_TRY
        {
            SimpleWriteLockHolder writeLockHolder(m_pFunctionIDHashTableRWLock);
            m_pFunctionIDHashTable->AddOrReplace(functionIDAndClientID);
        }
        EX_CATCH
        {
            // Running out of heap memory means we no longer can maintain the integrity of the mapping table.
            // All ELT2 fast-path hooks are disabled since we cannot report correct FunctionID to the
            // profiler at this moment.
            m_fIsClientIDToFunctionIDMappingEnabled = FALSE;
        }
        EX_END_CATCH(RethrowTerminalExceptions);

        // If ELT2 is in use, FunctionID will be returned to the JIT to be embedded into the ELT3 probes
        // instead of using clientID because the profiler may map several functionIDs to a clientID to
        // do things like code coverage analysis.  FunctionID to clientID has the one-on-one relationship,
        // while the reverse may not have this one-on-one mapping.  Therefore, FunctionID is used as the
        // key to retrieve the corresponding clientID from the internal FunctionID hash table.
        return funcId;
    }

    // For profilers that support ELT3, clientID will be embedded into the ELT3 probes
    return clientId;
}


//---------------------------------------------------------------------------------------
//
// Private functions called by GC so we can cache data for later notification to
// the profiler
//

//---------------------------------------------------------------------------------------
//
// Called lazily to allocate or use a recycled GCReferencesData.
//
// Return Value:
//      GCReferencesData * requested by caller.
//
// Notes:
//      Uses m_csGCRefDataFreeList to find a recycleable GCReferencesData
//      Called by GC callbacks that need to record GC references reported
//          to the callbacks by the GC as the GC walks the heap.
//

EEToProfInterfaceImpl::GCReferencesData * EEToProfInterfaceImpl::AllocateMovedReferencesData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // We directly take m_csGCRefDataFreeList around accessing the free list below
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this call
    }
    CONTRACTL_END;

    GCReferencesData *pData = NULL;

    // SCOPE: Lock m_csGCRefDataFreeList for access to the free list
    {
        CRITSEC_Holder csh(m_csGCRefDataFreeList);

        // Anything on the free list for us to grab?
        if (m_pGCRefDataFreeList != NULL)
        {
            // Yup, get the first element from the free list
            pData = m_pGCRefDataFreeList;
            m_pGCRefDataFreeList = m_pGCRefDataFreeList->pNext;
        }
    }

    if (pData == NULL)
    {
        // Still not set, so the free list must not have had anything
        // available.  Go ahead and allocate a struct directly.
        pData = new (nothrow) GCReferencesData;
        if (!pData)
        {
            return NULL;
        }
    }

    // Now init the new block
    _ASSERTE(pData != NULL);

    // Set our index to the beginning
    pData->curIdx = 0;
    pData->compactingCount = 0;

    return pData;
}

//---------------------------------------------------------------------------------------
//
// After reporting references to the profiler, this recycles the GCReferencesData
// that was used.  See EEToProfInterfaceImpl::EndRootReferences2.
//
// Arguments:
//      pData - Pointer to GCReferencesData to recycle
//

void EEToProfInterfaceImpl::FreeMovedReferencesData(GCReferencesData * pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        // We directly take m_csGCRefDataFreeList around accessing the free list below
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    // SCOPE: Lock m_csGCRefDataFreeList for access to the free list
    {
        CRITSEC_Holder csh(m_csGCRefDataFreeList);
        pData->pNext = m_pGCRefDataFreeList;
        m_pGCRefDataFreeList = pData;
    }
}

//---------------------------------------------------------------------------------------
//
// Called by the GC to notify profapi of a moved reference.  We cache the
// info here so we can later notify the profiler of all moved references
// in bulk.
//
// Arguments:
//      pbMemBlockStart - Start of moved block
//      pbMemBlockEnd - End of moved block
//      cbRelocDistance - Offset from pbMemBlockStart of where the block
//                        was moved to
//      pHeapId - GCReferencesData * used to record the block
//      fCompacting - Is this a compacting collection?
//
// Return Value:
//      HRESULT indicating success or failure
//

HRESULT EEToProfInterfaceImpl::MovedReference(BYTE * pbMemBlockStart,
                                              BYTE * pbMemBlockEnd,
                                              ptrdiff_t cbRelocDistance,
                                              void * pHeapId,
                                              BOOL fCompacting)
{
    CONTRACTL
    {
        NOTHROW;

        // Called during a GC
        GC_NOTRIGGER;
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Thread store lock normally held during this callback
    }
    CONTRACTL_END;

    _ASSERTE(pHeapId);
    _ASSERTE(*((size_t *)pHeapId) != (size_t)(-1));

    // Get a pointer to the data for this heap
    GCReferencesData *pData = (GCReferencesData *)(*((size_t *)pHeapId));

    // If this is the first notification of a moved reference for this heap
    // in this particular gc activation, then we need to get a ref data block
    // from the free list of blocks, or if that's empty then we need to
    // allocate a new one.
    if (pData == NULL)
    {
        pData = AllocateMovedReferencesData();
        if (pData == NULL)
        {
            return E_OUTOFMEMORY;
        }

        // Set the cookie so that we will be provided it on subsequent
        // callbacks
        ((*((size_t *)pHeapId))) = (size_t)pData;
    }

    _ASSERTE(pData->curIdx >= 0 && pData->curIdx <= kcReferencesMax);

    // If the struct has been filled, then we need to notify the profiler of
    // these moved references and clear the struct for the next load of
    // moved references
    if (pData->curIdx == kcReferencesMax)
    {
        MovedReferences(pData);
        pData->curIdx = 0;
        pData->compactingCount = 0;
    }

    // Now save the information in the struct
    pData->arrpbMemBlockStartOld[pData->curIdx] = pbMemBlockStart;
    pData->arrpbMemBlockStartNew[pData->curIdx] = pbMemBlockStart + cbRelocDistance;
    pData->arrMemBlockSize[pData->curIdx] = pbMemBlockEnd - pbMemBlockStart;

    // Increment the index into the parallel arrays
    pData->curIdx += 1;

    // Keep track of whether this is a compacting collection
    if (fCompacting)
    {
        pData->compactingCount += 1;
        // The gc is supposed to make up its mind whether this is a compacting collection or not
        // Thus if this one is compacting, everything so far had to say compacting
        _ASSERTE(pData->compactingCount == pData->curIdx);
    }
    else
    {
        // The gc is supposed to make up its mind whether this is a compacting collection or not
        // Thus if this one is non-compacting, everything so far had to say non-compacting
        _ASSERTE(pData->compactingCount == 0 && cbRelocDistance == 0);
    }
    return (S_OK);
}

//---------------------------------------------------------------------------------------
//
// Called by the GC to indicate that the GC is finished calling
// EEToProfInterfaceImpl::MovedReference for this collection.  This function will
// call into the profiler to notify it of all the moved references we've cached.
//
// Arguments:
//      pHeapId - Casted to a GCReferencesData * that contains the moved reference
//                data we've cached.
//
// Return Value:
//      HRESULT indicating success or failure.
//

HRESULT EEToProfInterfaceImpl::EndMovedReferences(void * pHeapId)
{
    CONTRACTL
    {
        NOTHROW;

        // Called during a GC
        GC_NOTRIGGER;
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // We directly take m_csGCRefDataFreeList around accessing the free list below
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback
    }
    CONTRACTL_END;

    _ASSERTE(pHeapId);
    _ASSERTE((*((size_t *)pHeapId)) != (size_t)(-1));

    HRESULT hr = S_OK;

    // Get a pointer to the data for this heap
    GCReferencesData *pData = (GCReferencesData *)(*((size_t *)pHeapId));

    // If there were no moved references, profiler doesn't need to know
    if (!pData)
        return (S_OK);

    // Communicate the moved references to the profiler
    _ASSERTE(pData->curIdx> 0);
    hr = MovedReferences(pData);

    // Now we're done with the data block, we can shove it onto the free list
    // SCOPE: Lock m_csGCRefDataFreeList for access to the free list
    {
        CRITSEC_Holder csh(m_csGCRefDataFreeList);
        pData->pNext = m_pGCRefDataFreeList;
        m_pGCRefDataFreeList = pData;
    }

#ifdef _DEBUG
    // Set the cookie to an invalid number
    (*((size_t *)pHeapId)) = (size_t)(-1);
#endif // _DEBUG

    return (hr);
}


#define HASH_ARRAY_SIZE_INITIAL 1024
#define HASH_ARRAY_SIZE_INC     256
#define HASH_NUM_BUCKETS        32
#define HASH(x)       ( (ULONG) ((SIZE_T)x) )  // A simple hash function

//---------------------------------------------------------------------------------------
//
// Callback used by the GC when walking the heap (via AllocByClassHelper in
// ProfToEEInterfaceImpl.cpp).
//
// Arguments:
//      objId - Object reference encountered during heap walk
//      classId - ClassID for objID
//      pHeapId - heap walk context used by this function; it's interpreted
//                as an AllocByClassData * to keep track of objects on the
//                heap by class.
//
// Return Value:
//      HRESULT indicating whether to continue with the heap walk (i.e.,
//      success HRESULT) or abort it (i.e., failure HRESULT).
//

HRESULT EEToProfInterfaceImpl::AllocByClass(ObjectID objId, ClassID clsId, void * pHeapId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    // This is a slight attempt to make sure that this is never called in a multi-threaded
    // manner.  This heap walk should be done by one thread at a time only.
    static DWORD dwProcId = 0xFFFFFFFF;
#endif

    _ASSERTE(pHeapId != NULL);
    _ASSERTE((*((size_t *)pHeapId)) != (size_t)(-1));

    // The heapId they pass in is really a AllocByClassData struct ptr.
    AllocByClassData *pData = (AllocByClassData *)(*((size_t *)pHeapId));

    // If it's null, need to allocate one
    if (pData == NULL)
    {
#ifdef _DEBUG
        // This is a slight attempt to make sure that this is never called in a multi-threaded
        // manner.  This heap walk should be done by one thread at a time only.
        dwProcId = GetCurrentProcessId();
#endif

        // See if we've saved a data block from a previous GC
        if (m_pSavedAllocDataBlock != NULL)
            pData = m_pSavedAllocDataBlock;

        // This means we need to allocate all the memory to keep track of the info
        else
        {
            // Get a new alloc data block
            pData = new (nothrow) AllocByClassData;
            if (pData == NULL)
                return (E_OUTOFMEMORY);

            // Create a new hash table
            pData->pHashTable = new (nothrow) CHashTableImpl(HASH_NUM_BUCKETS);
            if (!pData->pHashTable)
            {
                delete pData;
                return (E_OUTOFMEMORY);
            }

            // Get the memory for the array that the hash table is going to use
            pData->arrHash = new (nothrow) CLASSHASHENTRY[HASH_ARRAY_SIZE_INITIAL];
            if (pData->arrHash == NULL)
            {
                delete pData->pHashTable;
                delete pData;
                return (E_OUTOFMEMORY);
            }

            // Save the number of elements in the array
            pData->cHash = HASH_ARRAY_SIZE_INITIAL;

            // Now initialize the hash table
            HRESULT hr = pData->pHashTable->NewInit((BYTE *)pData->arrHash, sizeof(CLASSHASHENTRY));
            if (hr == E_OUTOFMEMORY)
            {
                delete [] pData->arrHash;
                delete pData->pHashTable;
                delete pData;
                return (E_OUTOFMEMORY);
            }
            _ASSERTE(pData->pHashTable->IsInited());

            // Null some entries
            pData->arrClsId = NULL;
            pData->arrcObjects = NULL;
            pData->cLength = 0;

            // Hold on to the structure
            m_pSavedAllocDataBlock = pData;
        }

        // Got some memory and hash table to store entries, yay!
        *((size_t *)pHeapId) = (size_t)pData;

        // Initialize the data
        pData->iHash = 0;
        pData->pHashTable->Clear();
    }

    _ASSERTE(pData->iHash <= pData->cHash);
    _ASSERTE(dwProcId == GetCurrentProcessId());

    // Lookup to see if this class already has an entry
    CLASSHASHENTRY * pEntry =
        reinterpret_cast<CLASSHASHENTRY *>(pData->pHashTable->Find(HASH(clsId), (SIZE_T)clsId));

    // If this class has already been encountered, just increment the counter.
    if (pEntry)
        pEntry->m_count++;

    // Otherwise, need to add this one as a new entry in the hash table
    else
    {
        // If we're full, we need to realloc
        if (pData->iHash == pData->cHash)
        {
            // Try to realloc the memory
            CLASSHASHENTRY     *tmp = new (nothrow) CLASSHASHENTRY[pData->cHash + HASH_ARRAY_SIZE_INC];
            if (!tmp)
            {
                return (E_OUTOFMEMORY);
            }

            _ASSERTE(pData->arrHash);
            memcpy (tmp, pData->arrHash, pData->cHash*sizeof(CLASSHASHENTRY));
            delete [] pData->arrHash;
            pData->arrHash = tmp;
            // Tell the hash table that the memory location of the array has changed
            pData->pHashTable->SetTable((BYTE *)pData->arrHash);

            // Save the new size of the array
            pData->cHash += HASH_ARRAY_SIZE_INC;
        }

        // Now add the new entry
        CLASSHASHENTRY *pNewEntry = (CLASSHASHENTRY *) pData->pHashTable->Add(HASH(clsId), pData->iHash++);

        pNewEntry->m_clsId = clsId;
        pNewEntry->m_count = 1;
    }

    // Indicate success
    return (S_OK);
}

HRESULT EEToProfInterfaceImpl::EndAllocByClass(void *pHeapId)
{
    _ASSERTE(pHeapId != NULL);
    _ASSERTE((*((size_t *)pHeapId)) != (size_t)(-1));

    HRESULT hr = S_OK;

    AllocByClassData *pData = (AllocByClassData *)(*((size_t *)pHeapId));

    // Notify the profiler if there are elements to notify it of
    if (pData != NULL)
        hr = NotifyAllocByClass(pData);

#ifdef _DEBUG
    (*((size_t *)pHeapId)) = (size_t)(-1);
#endif // _DEBUG

    return (hr);
}

//---------------------------------------------------------------------------------------
//
// Convert ETW-style root flag bitmask to ProfAPI-stye root flag bitmask
//
// Arguments:
//      dwEtwRootFlags - ETW-style root flag bitmask
//
// Return Value:
//      The corresponding ProfAPI-stye root flag bitmask
//

DWORD EtwRootFlagsToProfApiRootFlags(DWORD dwEtwRootFlags)
{
    LIMITED_METHOD_CONTRACT;

    // If a new ETW flag is added, adjust this assert, and add a case below.
    _ASSERTE((dwEtwRootFlags &
        ~(kEtwGCRootFlagsPinning | kEtwGCRootFlagsWeakRef | kEtwGCRootFlagsInterior | kEtwGCRootFlagsRefCounted))
                    == 0);

    DWORD dwProfApiRootFlags = 0;

    if ((dwEtwRootFlags & kEtwGCRootFlagsPinning) != 0)
    {
        dwProfApiRootFlags |= COR_PRF_GC_ROOT_PINNING;
    }
    if ((dwEtwRootFlags & kEtwGCRootFlagsWeakRef) != 0)
    {
        dwProfApiRootFlags |= COR_PRF_GC_ROOT_WEAKREF;
    }
    if ((dwEtwRootFlags & kEtwGCRootFlagsInterior) != 0)
    {
        dwProfApiRootFlags |= COR_PRF_GC_ROOT_INTERIOR;
    }
    if ((dwEtwRootFlags & kEtwGCRootFlagsRefCounted) != 0)
    {
        dwProfApiRootFlags |= COR_PRF_GC_ROOT_REFCOUNTED;
    }
    return dwProfApiRootFlags;
}

//---------------------------------------------------------------------------------------
//
// Convert ETW-style root kind enum to ProfAPI-stye root kind enum
//
// Arguments:
//      dwEtwRootKind - ETW-style root kind enum
//
// Return Value:
//      Corresponding ProfAPI-stye root kind enum
//

DWORD EtwRootKindToProfApiRootKind(EtwGCRootKind dwEtwRootKind)
{
    LIMITED_METHOD_CONTRACT;

    switch(dwEtwRootKind)
    {
    default:
        // If a new ETW root kind is added, create a profapi root kind as well, and add
        // the appropriate case below
        _ASSERTE(!"Unrecognized ETW root kind");
        // Deliberately fall through to kEtwGCRootKindOther
        FALLTHROUGH;

    case kEtwGCRootKindOther:
        return COR_PRF_GC_ROOT_OTHER;

    case  kEtwGCRootKindStack:
        return COR_PRF_GC_ROOT_STACK;

    case kEtwGCRootKindFinalizer:
        return COR_PRF_GC_ROOT_FINALIZER;

    case kEtwGCRootKindHandle:
        return COR_PRF_GC_ROOT_HANDLE;
    }
}

//---------------------------------------------------------------------------------------
//
// Callback used by the GC when scanning the roots (via ScanRootsHelper in
// ProfToEEInterfaceImpl.cpp).
//
// Arguments:
//      objectId - Root object reference encountered
//      dwEtwRootKind - ETW enum describing what kind of root objectId is
//      dwEtwRootFlags - ETW flags describing the root qualities of objectId
//      rootID - Root's methoddesc if dwEtwRootKind==kEtwGCRootKindStack, else NULL
//      pHeapId - Used as a GCReferencesData * to keep track of the GC references
//
// Return Value:
//      HRESULT indicating success or failure.
//

HRESULT EEToProfInterfaceImpl::RootReference2(BYTE * objectId,
                                              EtwGCRootKind dwEtwRootKind,
                                              EtwGCRootFlags dwEtwRootFlags,
                                              void * rootID,
                                              void * pHeapId)
{
    _ASSERTE(pHeapId);
    _ASSERTE(*((size_t *)pHeapId) != (size_t)(-1));

    LOG((LF_CORPROF, LL_INFO100000, "**PROF: Root Reference. "
            "ObjectID:0x%p dwEtwRootKind:0x%x dwEtwRootFlags:0x%x rootId:0x%p HeadId:0x%p\n",
            objectId, dwEtwRootKind, dwEtwRootFlags, rootID, pHeapId));

    DWORD dwProfApiRootFlags = EtwRootFlagsToProfApiRootFlags(dwEtwRootFlags);
    DWORD dwProfApiRootKind = EtwRootKindToProfApiRootKind((EtwGCRootKind) dwEtwRootKind);

    // Get a pointer to the data for this heap
    GCReferencesData *pData = (GCReferencesData *)(*((size_t *)pHeapId));

    // If this is the first notification of an extended root reference for this heap
    // in this particular gc activation, then we need to get a ref data block
    // from the free list of blocks, or if that's empty then we need to
    // allocate a new one.
    if (pData == NULL)
    {
        pData = AllocateMovedReferencesData();
        if (pData == NULL)
            return (E_OUTOFMEMORY);

        // Set the cookie so that we will be provided it on subsequent
        // callbacks
        ((*((size_t *)pHeapId))) = (size_t)pData;
    }

    _ASSERTE(pData->curIdx >= 0 && pData->curIdx <= kcReferencesMax);

    // If the struct has been filled, then we need to notify the profiler of
    // these root references and clear the struct for the next load of
    // root references
    if (pData->curIdx == kcReferencesMax)
    {
        RootReferences2(pData);
        pData->curIdx = 0;
    }

    // Now save the information in the struct
    pData->arrpbMemBlockStartOld[pData->curIdx] = objectId;
    pData->arrpbMemBlockStartNew[pData->curIdx] = (BYTE *)rootID;

    // assert that dwProfApiRootKind and dwProfApiRootFlags both fit in 16 bits, so we can
    // pack both into a 32-bit word
    _ASSERTE((dwProfApiRootKind & 0xffff) == dwProfApiRootKind && (dwProfApiRootFlags & 0xffff) == dwProfApiRootFlags);

    pData->arrULONG[pData->curIdx] = (dwProfApiRootKind << 16) | dwProfApiRootFlags;

    // Increment the index into the parallel arrays
    pData->curIdx += 1;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Called by the GC to indicate that the GC is finished calling
// EEToProfInterfaceImpl::RootReference2 for this collection.  This function will
// call into the profiler to notify it of all the root references we've cached.
//
// Arguments:
//      pHeapId - Casted to a GCReferencesData * that contains the root references
//                we've cached.
//
// Return Value:
//      HRESULT indicating success or failure.
//

HRESULT EEToProfInterfaceImpl::EndRootReferences2(void * pHeapId)
{
    _ASSERTE(pHeapId);
    _ASSERTE((*((size_t *)pHeapId)) != (size_t)(-1));

    HRESULT hr = S_OK;

    // Get a pointer to the data for this heap
    GCReferencesData *pData = (GCReferencesData *)(*((size_t *)pHeapId));

    // If there were no moved references, profiler doesn't need to know
    if (!pData)
        return (S_OK);

    // Communicate the moved references to the profiler
    _ASSERTE(pData->curIdx> 0);
    hr = RootReferences2(pData);

    // Now we're done with the data block, we can shove it onto the free list
    FreeMovedReferencesData(pData);

#ifdef _DEBUG
    // Set the cookie to an invalid number
    (*((size_t *)pHeapId)) = (size_t)(-1);
#endif // _DEBUG

    return (hr);
}

//---------------------------------------------------------------------------------------
//
// Callback used by the GC when scanning the roots (via
// Ref_ScanDependentHandlesForProfilerAndETW in ObjectHandle.cpp).
//
// Arguments:
//      primaryObjectId   - Primary object reference in the DependentHandle
//      secondaryObjectId - Secondary object reference in the DependentHandle
//      rootID            - The DependentHandle maintaining the dependency relationship
//      pHeapId           - Used as a GCReferencesData * to keep track of the GC references
//
// Return Value:
//      HRESULT indicating success or failure.
//

HRESULT EEToProfInterfaceImpl::ConditionalWeakTableElementReference(BYTE * primaryObjectId,
                        BYTE * secondaryObjectId,
                        void * rootID,
                        void * pHeapId)
{
    _ASSERTE(pHeapId);
    _ASSERTE(*((size_t *)pHeapId) != (size_t)(-1));

    // Callers must ensure the profiler asked to be notified about dependent handles,
    // since this is only available for profilers implementing ICorProfilerCallback5 and
    // greater.
    _ASSERTE(CORProfilerTrackConditionalWeakTableElements());

    LOG((LF_CORPROF, LL_INFO100000, "**PROF: Root Dependent Handle. "
            "PrimaryObjectID:0x%p SecondaryObjectID:0x%p rootId:0x%p HeadId:0x%p\n",
            primaryObjectId, secondaryObjectId, rootID, pHeapId));

    // Get a pointer to the data for this heap
    GCReferencesData *pData = (GCReferencesData *)(*((size_t *)pHeapId));

    // If this is the first notification of a dependent handle reference in
    // this particular gc activation, then we need to get a ref data block
    // from the free list of blocks, or if that's empty then we need to
    // allocate a new one.
    if (pData == NULL)
    {
        pData = AllocateMovedReferencesData();
        if (pData == NULL)
            return (E_OUTOFMEMORY);

        // Set the cookie so that we will be provided it on subsequent
        // callbacks
        ((*((size_t *)pHeapId))) = (size_t)pData;
    }

    _ASSERTE(pData->curIdx >= 0 && pData->curIdx <= kcReferencesMax);

    // If the struct has been filled, then we need to notify the profiler of
    // these dependent handle references and clear the struct for the next
    // load of dependent handle references
    if (pData->curIdx == kcReferencesMax)
    {
        ConditionalWeakTableElementReferences(pData);
        pData->curIdx = 0;
    }

    // Now save the information in the struct
    pData->arrpbMemBlockStartOld[pData->curIdx] = primaryObjectId;
    pData->arrpbMemBlockStartNew[pData->curIdx] = secondaryObjectId;
    pData->arrpbRootId[pData->curIdx]           = (BYTE*) rootID;

    // Increment the index into the parallel arrays
    pData->curIdx += 1;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Called by the GC to indicate that the GC is finished calling
// EEToProfInterfaceImpl::ConditionalWeakTableElementReference for this collection.  This
// function will call into the profiler to notify it of all the DependentHandle references
// we've cached.
//
// Arguments:
//      pHeapId - Casted to a GCReferencesData * that contains the dependent handle
//                references we've cached.
//
// Return Value:
//      HRESULT indicating success or failure.
//

HRESULT EEToProfInterfaceImpl::EndConditionalWeakTableElementReferences(void * pHeapId)
{
    _ASSERTE(pHeapId);
    _ASSERTE((*((size_t *)pHeapId)) != (size_t)(-1));

    // Callers must ensure the profiler asked to be notified about dependent handles,
    // since this is only available for profilers implementing ICorProfilerCallback5 and
    // greater.
    _ASSERTE(CORProfilerTrackConditionalWeakTableElements());

    HRESULT hr = S_OK;

    // Get a pointer to the data for this heap
    GCReferencesData *pData = (GCReferencesData *)(*((size_t *)pHeapId));

    // If there were no dependent handles, profiler doesn't need to know
    if (!pData)
        return (S_OK);

    // Communicate the dependent handle references to the profiler
    _ASSERTE(pData->curIdx > 0);
    hr = ConditionalWeakTableElementReferences(pData);

    // Now we're done with the data block, we can shove it onto the free list
    FreeMovedReferencesData(pData);

#ifdef _DEBUG
    // Set the cookie to an invalid number
    (*((size_t *)pHeapId)) = (size_t)(-1);
#endif // _DEBUG

    return (hr);
}



//---------------------------------------------------------------------------------------
//
// Returns whether the profiler performed unrevertible acts, such as instrumenting
// code or requesting ELT hooks.  RequestProfilerDetach uses this function before
// performing any sealing or evacuation checks to determine whether it's even possible
// for the profiler ever to detach.
//
// Return Value:
//    * S_OK if it's safe to attempt a detach.  Evacuation checks must still be performed
//        before actually unloading the profiler.
//    * else, an HRESULT error value indicating what the profiler did that made it
//        undetachable.  This is a public HRESULT suitable for returning from the
//        RequestProfilerDetach API.
//

HRESULT EEToProfInterfaceImpl::EnsureProfilerDetachable()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pProfilerInfo->eventMask.IsEventMaskSet(COR_PRF_MONITOR_IMMUTABLE) ||
        m_pProfilerInfo->eventMask.IsEventMaskHighSet(COR_PRF_HIGH_MONITOR_IMMUTABLE))
    {
        LOG((
            LF_CORPROF,
            LL_ERROR,
            "**PROF: Profiler may not detach because it set an immutable flag.\n"));

        return CORPROF_E_IMMUTABLE_FLAGS_SET;
    }

    if ((m_pEnter != NULL)             ||
        (m_pLeave != NULL)             ||
        (m_pTailcall != NULL)          ||
        (m_pEnter2 != NULL)            ||
        (m_pLeave2 != NULL)            ||
        (m_pTailcall2 != NULL)         ||
        (m_pEnter3 != NULL)            ||
        (m_pEnter3WithInfo != NULL)    ||
        (m_pLeave3 != NULL)            ||
        (m_pLeave3WithInfo != NULL)    ||
        (m_pTailcall3 != NULL)         ||
        (m_pTailcall3WithInfo != NULL))
    {
        LOG((
            LF_CORPROF,
            LL_ERROR,
            "**PROF: Profiler may not detach because it set an ELT(2) hook.\n"));

        return CORPROF_E_IRREVERSIBLE_INSTRUMENTATION_PRESENT;
    }

    if (m_fUnrevertiblyModifiedIL)
    {
        LOG((
            LF_CORPROF,
            LL_ERROR,
            "**PROF: Profiler may not detach because it called SetILFunctionBody.\n"));

        return CORPROF_E_IRREVERSIBLE_INSTRUMENTATION_PRESENT;
    }

    if (m_fModifiedRejitState)
    {
        LOG((
            LF_CORPROF,
            LL_ERROR,
            "**PROF: Profiler may not detach because it enabled Rejit.\n"));

        return CORPROF_E_IRREVERSIBLE_INSTRUMENTATION_PRESENT;
    }

    return S_OK;
}

// Declarations for asm wrappers of profiler callbacks
EXTERN_C void STDMETHODCALLTYPE ProfileEnterNaked(FunctionIDOrClientID functionIDOrClientID);
EXTERN_C void STDMETHODCALLTYPE ProfileLeaveNaked(FunctionIDOrClientID functionIDOrClientID);
EXTERN_C void STDMETHODCALLTYPE ProfileTailcallNaked(FunctionIDOrClientID functionIDOrClientID);
#define PROFILECALLBACK(name) name##Naked

//---------------------------------------------------------------------------------------
//
// Determines the hooks (slow path vs. fast path) to which the JIT shall
// insert calls, and then tells the JIT which ones we want
//
// Return Value:
//      HRESULT indicating success or failure
//

HRESULT EEToProfInterfaceImpl::DetermineAndSetEnterLeaveFunctionHooksForJit()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // We're doing all ELT3 hooks, all-Whidbey hooks or all-Everett hooks.  No mixing and matching.
    BOOL fCLRv4Hooks = (m_pEnter3 != NULL)             ||
                       (m_pLeave3 != NULL)             ||
                       (m_pTailcall3 != NULL)          ||
                       (m_pEnter3WithInfo != NULL)     ||
                       (m_pLeave3WithInfo != NULL)     ||
                       (m_pTailcall3WithInfo != NULL);

    BOOL fWhidbeyHooks = (m_pEnter2 != NULL)     ||
                         (m_pLeave2 != NULL)     ||
                         (m_pTailcall2 != NULL);

    // If no hooks were set (e.g., SetEventMask called with COR_PRF_MONITOR_ENTERLEAVE,
    // but SetEnterLeaveFunctionHooks(*) never called), then nothing to do
    if (!fCLRv4Hooks           &&
        !fWhidbeyHooks         &&
        (m_pEnter == NULL)     &&
        (m_pLeave == NULL)     &&
        (m_pTailcall == NULL))
    {
        return S_OK;
    }


    HRESULT hr = S_OK;

    EX_TRY
    {
        if (fCLRv4Hooks)
        {
            // For each type of hook (enter/leave/tailcall) we must determine if we can use the
            // happy lucky fast path (i.e., direct call from JITd code right into the profiler's
            // hook or the JIT default stub (see below)), or the slow path (i.e., call into an
            // intermediary FCALL which then calls the profiler's hook) with extra information
            // about the current function.

            hr = SetEnterLeaveFunctionHooksForJit(
                (m_pEnter3WithInfo != NULL) ?
                    PROFILECALLBACK(ProfileEnter) :
                    m_pEnter3,
                (m_pLeave3WithInfo != NULL) ?
                    PROFILECALLBACK(ProfileLeave) :
                    m_pLeave3,
                (m_pTailcall3WithInfo != NULL) ?
                    PROFILECALLBACK(ProfileTailcall) :
                    m_pTailcall3);
        }
        else
        {
            //
            // Everett or Whidbey hooks.
            //

            // When using Everett or Whidbey hooks, the check looks like this:
            //
            // IF       Hook exists
            // THEN     Use slow path
            //
            // Why?
            //
            // - If the profiler wants the old-style Whidbey or Everett hooks, we need a wrapper
            // to convert from the ELT3 prototype the JIT expects to the Whidbey or Everett
            // prototype the profiler expects. It applies to Whidbey fast-path hooks.   And due
            // to the overhead of looking up FunctionID from cache and using lock to synchronize
            // cache accesses, the so-called Whidbey fast-path hooks are much slower than they
            // used to be.  Whidbey and Everett hooks are supported to keep existing profiler
            // running, but the profiler writers are encouraged to use ELT3 interface for the
            // best performance.
            //
            // Implicit in the above logic is if one of the hook types has no hook pointer
            // specified, then we pass NULL as the hook pointer to the JIT, in which case the JIT
            // just generates a call to the default stub (a single ret) w/out invoking the slow-path
            // wrapper.  I call this the "fast path to nowhere"

            BOOL fEnter = (m_pEnter != NULL) || (m_pEnter2 != NULL);
            BOOL fLeave = (m_pLeave != NULL) || (m_pLeave2 != NULL);
            BOOL fTailcall = (m_pTailcall != NULL) || (m_pTailcall2 != NULL);

            hr = SetEnterLeaveFunctionHooksForJit(
                fEnter ?
                    PROFILECALLBACK(ProfileEnter) :
                    NULL,
                fLeave ?
                    PROFILECALLBACK(ProfileLeave) :
                    NULL,
                fTailcall ?
                    PROFILECALLBACK(ProfileTailcall) :
                    NULL);
        }
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    // We need to swallow all exceptions, because we will lock otherwise (in addition to
    // the IA64-only lock while allocating stub space!).  For example, specifying
    // RethrowTerminalExceptions forces us to test to see if the caught exception is
    // terminal and Exception::IsTerminal() can lock if we get a handle table cache miss
    // while getting a handle for the exception.  It is good to minimize locks from
    // profiler Info functions (and their callees), and this is a dumb lock to have,
    // given that we can avoid it altogether by just having terminal exceptions be
    // swallowed here, and returning the failure to the profiler.  For those who don't
    // like swallowing terminal exceptions, this is mitigated by the fact that,
    // currently, an exception only gets thrown from SetEnterLeaveFunctionHooksForJit on
    // IA64.  But to keep consistent (and in case the world changes), we'll do this on
    // all platforms.
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}


//---------------------------------------------------------------------------------------
//
// The Info method SetEventMask() simply defers to this function to do the real work.
//
// Arguments:
//      dwEventMask - Event mask specified by the profiler
//
// Return Value:
//     HRESULT indicating success / failure to return straight through to the profiler
//

HRESULT EEToProfInterfaceImpl::SetEventMask(DWORD dwEventMask, DWORD dwEventMaskHigh)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    BOOL isMainProfiler = g_profControlBlock.IsMainProfiler(this);

    if (!isMainProfiler &&
        ((dwEventMask & ~COR_PRF_ALLOWABLE_NOTIFICATION_PROFILER)
            || (dwEventMaskHigh & ~COR_PRF_HIGH_ALLOWABLE_NOTIFICATION_PROFILER)))
    {
        return E_INVALIDARG;
    }

    static const DWORD kEventFlagsRequiringSlowPathEnterLeaveHooks =
        COR_PRF_ENABLE_FUNCTION_ARGS   |
        COR_PRF_ENABLE_FUNCTION_RETVAL |
        COR_PRF_ENABLE_FRAME_INFO
        ;

    static const DWORD kEventFlagsAffectingEnterLeaveHooks =
        COR_PRF_MONITOR_ENTERLEAVE     |
        kEventFlagsRequiringSlowPathEnterLeaveHooks
        ;

    HRESULT hr;

#ifdef _DEBUG
    // Some tests need to enable immutable flags after startup, when a profiler is
    // attached. These flags enable features that are used solely to verify the
    // correctness of other, MUTABLE features. Examples: enable immutable ELT to create
    // shadow stacks to verify stack walks (which can be done mutably via manual
    // EBP-frame walking), or enable immutable DSS to gather IP addresses to verify the
    // mutable GetFunctionFromIP.
    //
    // Similarly, test profilers may need to extend the set of flags allowable on attach
    // to enable features that help verify other parts of the profapi that ARE allowed
    // on attach.
    //
    // See code:#P2CLRRestrictionsOverview for more information
    DWORD dwImmutableEventFlags = COR_PRF_MONITOR_IMMUTABLE;
    DWORD dwAllowableAfterAttachEventFlags = COR_PRF_ALLOWABLE_AFTER_ATTACH;
    DWORD dwTestOnlyAllowedEventMask = 0;
    dwTestOnlyAllowedEventMask = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TestOnlyAllowedEventMask);
    if (dwTestOnlyAllowedEventMask != 0)
    {
        // Remove from the immutable flag list those flags that a test-only profiler may
        // need to set post-startup (specified via COMPlus_TestOnlyAllowedEventMask)
        dwImmutableEventFlags &= ~dwTestOnlyAllowedEventMask;

        // And add to the "allowable after attach" list the same test-only flags.
        dwAllowableAfterAttachEventFlags |= dwTestOnlyAllowedEventMask;

        LOG((LF_CORPROF, LL_INFO10, "**PROF: TestOnlyAllowedEventMask=0x%x. New immutable flags=0x%x.  New AllowableAfterAttach flags=0x%x\n",
            dwTestOnlyAllowedEventMask,
            dwImmutableEventFlags,
            dwAllowableAfterAttachEventFlags));
    }
#endif //_DEBUG

    // If we're not in initialization or shutdown, make sure profiler is
    // not trying to set an immutable attribute
    // FUTURE: If we add immutable flags to the high event mask, this would be a good
    // place to check for them as well.
    if (m_pProfilerInfo->curProfStatus.Get() != kProfStatusInitializingForStartupLoad)
    {
#ifdef _DEBUG
        if (((dwEventMask & dwImmutableEventFlags) !=
                (m_pProfilerInfo->eventMask.GetEventMask() & dwImmutableEventFlags)) ||
#else //!_DEBUG
        if (((dwEventMask & COR_PRF_MONITOR_IMMUTABLE) !=
                (m_pProfilerInfo->eventMask.GetEventMask() & COR_PRF_MONITOR_IMMUTABLE)) ||
#endif //_DEBUG
            ((dwEventMaskHigh & COR_PRF_HIGH_MONITOR_IMMUTABLE) !=
                (m_pProfilerInfo->eventMask.GetEventMaskHigh() & COR_PRF_HIGH_MONITOR_IMMUTABLE)))
        {
            // FUTURE: Should we have a dedicated HRESULT for setting immutable flag?
            return E_FAIL;
        }
    }

    // If this is an attaching profiler, make sure the profiler only sets flags
    // allowable after an attach
    if (m_fLoadedViaAttach &&
#ifdef _DEBUG
        (((dwEventMask & (~dwAllowableAfterAttachEventFlags)) != 0) ||
#else //!_DEBUG
        (((dwEventMask & (~COR_PRF_ALLOWABLE_AFTER_ATTACH)) != 0) ||
#endif //_DEBUG
        (dwEventMaskHigh & (~COR_PRF_HIGH_ALLOWABLE_AFTER_ATTACH))))
    {
        return CORPROF_E_UNSUPPORTED_FOR_ATTACHING_PROFILER;
    }

    // After fast path ELT hooks are set in Initial callback, the startup profiler is not allowed to change flags
    // that require slow path ELT hooks or disable ELT hooks.
    if ((m_pProfilerInfo->curProfStatus.Get() == kProfStatusInitializingForStartupLoad) &&
        (
            (m_pEnter3    != NULL) ||
            (m_pLeave3    != NULL) ||
            (m_pTailcall3 != NULL)
        ) &&
        (
            ((dwEventMask & kEventFlagsRequiringSlowPathEnterLeaveHooks) != 0) ||
            ((dwEventMask & COR_PRF_MONITOR_ENTERLEAVE) == 0)
        )
       )
    {
        _ASSERTE(!m_pProfilerInfo->eventMask.IsEventMaskSet(kEventFlagsRequiringSlowPathEnterLeaveHooks));
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }

    // After slow path ELT hooks are set in Initial callback, the startup profiler is not allowed to remove
    // all flags that require slow path ELT hooks or to change the flag to disable the ELT hooks.
    if ((m_pProfilerInfo->curProfStatus.Get() == kProfStatusInitializingForStartupLoad) &&
        (
            (m_pEnter3WithInfo    != NULL) ||
            (m_pLeave3WithInfo    != NULL) ||
            (m_pTailcall3WithInfo != NULL)
        ) &&
        (
            ((dwEventMask & kEventFlagsRequiringSlowPathEnterLeaveHooks) == 0) ||
            ((dwEventMask & COR_PRF_MONITOR_ENTERLEAVE) == 0)
        )
       )
    {
        _ASSERTE(m_pProfilerInfo->eventMask.IsEventMaskSet(kEventFlagsRequiringSlowPathEnterLeaveHooks));
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }


    // Note whether the caller is changing flags that affect enter leave hooks
    BOOL fEnterLeaveHooksAffected =
        // Did any of the relevant flags change?
        (
            (
                // Old flags
                ((m_pProfilerInfo->eventMask.GetEventMask() & kEventFlagsAffectingEnterLeaveHooks) ^
                // XORed w/ the new flags
                (dwEventMask & kEventFlagsAffectingEnterLeaveHooks))
            ) != 0
        ) &&
        // And are any enter/leave hooks set?
        (
            (m_pEnter3            != NULL) ||
            (m_pEnter3WithInfo    != NULL) ||
            (m_pEnter2            != NULL) ||
            (m_pEnter             != NULL) ||
            (m_pLeave3            != NULL) ||
            (m_pLeave3WithInfo    != NULL) ||
            (m_pLeave2            != NULL) ||
            (m_pLeave             != NULL) ||
            (m_pTailcall3         != NULL) ||
            (m_pTailcall3WithInfo != NULL) ||
            (m_pTailcall2         != NULL) ||
            (m_pTailcall          != NULL)
        );

    if (fEnterLeaveHooksAffected && !isMainProfiler)
    {
        return E_INVALIDARG;
    }

    BOOL fNeedToTurnOffConcurrentGC = FALSE;

    if (((dwEventMask & COR_PRF_MONITOR_GC) != 0) &&
        ((m_pProfilerInfo->eventMask.GetEventMask() & COR_PRF_MONITOR_GC) == 0))
    {
        // We don't need to worry about startup load as we'll turn off concurrent GC later
        if (m_pProfilerInfo->curProfStatus.Get() != kProfStatusInitializingForStartupLoad)
        {
            // Since we're not an initializing startup profiler, the EE must be fully started up
            // so we can check whether concurrent GC is on
            if (!g_fEEStarted)
            {
                return CORPROF_E_RUNTIME_UNINITIALIZED;
            }

            // We don't want to change the flag before GC is fully initialized,
            // otherwise the concurrent GC setting would be overwritten
            // Make sure GC is fully initialized before proceed
            if (!IsGarbageCollectorFullyInitialized())
            {
                return CORPROF_E_NOT_YET_AVAILABLE;
            }

            // If we are attaching and we are turning on COR_PRF_MONITOR_GC, turn off concurrent GC later
            // in this function
            if (m_pProfilerInfo->curProfStatus.Get() == kProfStatusInitializingForAttachLoad)
            {
                if (GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled())
                {
                    // We only allow turning off concurrent GC in the profiler attach thread inside
                    // InitializeForAttach, otherwise we would be vulnerable to weird races such as
                    // SetEventMask running on a separate thread and trying to turn off concurrent GC.
                    // The best option here is to fail with CORPROF_E_CONCURRENT_GC_NOT_PROFILABLE.
                    // Existing Dev10 profilers should be prepared to handle such case.
                    if (IsProfilerAttachThread())
                    {
                        fNeedToTurnOffConcurrentGC = TRUE;
                    }
                    else
                    {
                        return CORPROF_E_CONCURRENT_GC_NOT_PROFILABLE;
                    }
                }
            }
            else
            {
                // Fail if concurrent GC is enabled
                // This should only happen for attach profilers if user didn't turn on COR_PRF_MONITOR_GC
                // at attach time
                if (GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled())
                {
                    return CORPROF_E_CONCURRENT_GC_NOT_PROFILABLE;
                }
            }
        }
    }

    if ((dwEventMask & COR_PRF_ENABLE_REJIT) != 0)
    {
        if ((m_pProfilerInfo->curProfStatus.Get() != kProfStatusInitializingForStartupLoad) && !ReJitManager::IsReJITEnabled())
        {
            return CORPROF_E_REJIT_NOT_ENABLED;
        }

        m_pProfilerInfo->pProfInterface->SetModifiedRejitState();
    }

    // High event bits

    if (((dwEventMaskHigh & COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES) != 0) &&
        !IsCallback6Supported())
    {
        return CORPROF_E_CALLBACK6_REQUIRED;
    }

    if (((dwEventMaskHigh & COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED) != 0) &&
        !IsCallback7Supported())
    {
        return CORPROF_E_CALLBACK7_REQUIRED;
    }

    // Now save the modified masks
    m_pProfilerInfo->eventMask.SetEventMask(dwEventMask);
    m_pProfilerInfo->eventMask.SetEventMaskHigh(dwEventMaskHigh);

    g_profControlBlock.UpdateGlobalEventMask();

    if (fEnterLeaveHooksAffected)
    {
        hr = DetermineAndSetEnterLeaveFunctionHooksForJit();
        if (FAILED(hr))
        {
            return hr;
        }
    }

    // Turn off concurrent GC as the last step so that we don't need to turn it back on if something
    // else failed after that
    if (fNeedToTurnOffConcurrentGC)
    {
        // Remember that we've turned off concurrent GC and we'll turn it back on in TerminateProfiling
        g_profControlBlock.fConcurrentGCDisabledForAttach = TRUE;

        // Turn off concurrent GC if it is on so that user can walk the heap safely in GC callbacks
        IGCHeap * pGCHeap = GCHeapUtilities::GetGCHeap();

        LOG((LF_CORPROF, LL_INFO10, "**PROF: Turning off concurrent GC at attach.\n"));

        // First turn off concurrent GC
        pGCHeap->TemporaryDisableConcurrentGC();

        //
        // Then wait until concurrent GC to finish if concurrent GC is in progress
        // User can use a timeout that can be set by environment variable if the GC turns out
        // to be too long. The default value is INFINITE.
        //
        // NOTE:
        // If we don't do it in this order there might be a new concurrent GC started
        // before we actually turn off concurrent GC
        //
        hr = pGCHeap->WaitUntilConcurrentGCCompleteAsync(m_dwConcurrentGCWaitTimeoutInMs);
        if (FAILED(hr))
        {
            if (hr == HRESULT_FROM_WIN32(ERROR_TIMEOUT))
            {
                // Convert it to a more specific HRESULT
                hr = CORPROF_E_TIMEOUT_WAITING_FOR_CONCURRENT_GC;

                // Since we cannot call LogProfEvent here due to contact violations, we'll need to
                // remember the fact that we've failed, and report the failure later after InitializeForAttach
                m_bHasTimedOutWaitingForConcurrentGC = TRUE;
            }

            // TODO: think about race conditions... I am pretty sure there is one
            // Remember that we've turned off concurrent GC and we'll turn it back on in TerminateProfiling
            g_profControlBlock.fConcurrentGCDisabledForAttach = FALSE;
            pGCHeap->TemporaryEnableConcurrentGC();
            
            return hr;
        }

        LOG((LF_CORPROF, LL_INFO10, "**PROF: Concurrent GC has been turned off at attach.\n"));
    }

    // Return success
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// The Info method SetEnterLeaveFunctionHooks() simply defers to this function to do the
// real work.
//
// Arguments:
//     (same as specified in the public API docs)
//
// Return Value:
//     HRESULT indicating success / failure to return straight through to the profiler
//

HRESULT EEToProfInterfaceImpl::SetEnterLeaveFunctionHooks(FunctionEnter * pFuncEnter,
                                                          FunctionLeave * pFuncLeave,
                                                          FunctionTailcall * pFuncTailcall)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // You have to be setting at least one hook
    if ((pFuncEnter == NULL) && (pFuncLeave == NULL) && (pFuncTailcall == NULL))
    {
        return E_INVALIDARG;
    }

    // ELT3 hooks beat Whidbey and Whidbey hooks beat Everett hooks.  So if any ELT3 or
    // Whidbey hooks were set (SetEnterLeaveFunctionHooks3(WithInfo) or SetEnterLeaveFunctionHooks2),
    // this should be a noop
    if ((m_pEnter3            != NULL) ||
        (m_pEnter3WithInfo    != NULL) ||
        (m_pLeave3            != NULL) ||
        (m_pLeave3WithInfo    != NULL) ||
        (m_pTailcall3         != NULL) ||
        (m_pTailcall3WithInfo != NULL) ||
        (m_pEnter2            != NULL) ||
        (m_pLeave2            != NULL) ||
        (m_pTailcall2         != NULL))
    {
        return S_OK;
    }

    // Always save onto the function pointers, since we won't know if the profiler
    // is going to tracking enter/leave until after it returns from Initialize
    m_pEnter = pFuncEnter;
    m_pLeave = pFuncLeave;
    m_pTailcall = pFuncTailcall;

    return DetermineAndSetEnterLeaveFunctionHooksForJit();
}

//---------------------------------------------------------------------------------------
//
// The Info method SetEnterLeaveFunctionHooks2() simply defers to this function to do the
// real work.
//
// Arguments:
//     (same as specified in the public API docs)
//
// Return Value:
//     HRESULT indicating success / failure to return straight through to the profiler
//

HRESULT EEToProfInterfaceImpl::SetEnterLeaveFunctionHooks2(FunctionEnter2 * pFuncEnter,
                                                           FunctionLeave2 * pFuncLeave,
                                                           FunctionTailcall2 * pFuncTailcall)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // You have to be setting at least one hook
    if ((pFuncEnter == NULL) && (pFuncLeave == NULL) && (pFuncTailcall == NULL))
    {
        return E_INVALIDARG;
    }

    // ELT3 hooks beat Whidbey.  So if any ELT3 hooks were set (SetEnterLeaveFunctionHooks3(WithInfo)),
    // this should be a noop
    if ((m_pEnter3            != NULL) ||
        (m_pEnter3WithInfo    != NULL) ||
        (m_pLeave3            != NULL) ||
        (m_pLeave3WithInfo    != NULL) ||
        (m_pTailcall3         != NULL) ||
        (m_pTailcall3WithInfo != NULL))
    {
        return S_OK;
    }

    // Always save onto the function pointers, since we won't know if the profiler
    // is going to track enter/leave until after it returns from Initialize
    m_pEnter2 = pFuncEnter;
    m_pLeave2 = pFuncLeave;
    m_pTailcall2 = pFuncTailcall;

    // Whidbey hooks override Everett hooks
    m_pEnter = NULL;
    m_pLeave = NULL;
    m_pTailcall = NULL;

    return DetermineAndSetEnterLeaveFunctionHooksForJit();
}

//---------------------------------------------------------------------------------------
//
// The Info method SetEnterLeaveFunctionHooks3() simply defers to this function to do the
// real work.
//
// Arguments:
//     (same as specified in the public API docs)
//
// Return Value:
//     HRESULT indicating success / failure to return straight through to the profiler
//

HRESULT EEToProfInterfaceImpl::SetEnterLeaveFunctionHooks3(FunctionEnter3 * pFuncEnter3,
                                                           FunctionLeave3 * pFuncLeave3,
                                                           FunctionTailcall3 * pFuncTailcall3)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // You have to be setting at least one hook
    if ((pFuncEnter3    == NULL) &&
        (pFuncLeave3    == NULL) &&
        (pFuncTailcall3 == NULL))
    {
        return E_INVALIDARG;
    }

    if (CORProfilerELT3SlowPathEnabled())
    {
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }

    // Always save onto the function pointers, since we won't know if the profiler
    // is going to track enter/leave until after it returns from Initialize
    m_pEnter3    = pFuncEnter3;
    m_pLeave3    = pFuncLeave3;
    m_pTailcall3 = pFuncTailcall3;
    m_pEnter3WithInfo    = NULL;
    m_pLeave3WithInfo    = NULL;
    m_pTailcall3WithInfo = NULL;

    // ELT3 hooks override Whidbey hooks and Everett hooks.
    m_pEnter2    = NULL;
    m_pLeave2    = NULL;
    m_pTailcall2 = NULL;
    m_pEnter     = NULL;
    m_pLeave     = NULL;
    m_pTailcall  = NULL;

    return DetermineAndSetEnterLeaveFunctionHooksForJit();
}


//---------------------------------------------------------------------------------------
//
// The Info method SetEnterLeaveFunctionHooks3() simply defers to this function to do the
// real work.
//
// Arguments:
//     (same as specified in the public API docs)
//
// Return Value:
//     HRESULT indicating success / failure to return straight through to the profiler
//

HRESULT EEToProfInterfaceImpl::SetEnterLeaveFunctionHooks3WithInfo(FunctionEnter3WithInfo * pFuncEnter3WithInfo,
                                                                   FunctionLeave3WithInfo * pFuncLeave3WithInfo,
                                                                   FunctionTailcall3WithInfo * pFuncTailcall3WithInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // You have to be setting at least one hook
    if ((pFuncEnter3WithInfo    == NULL) &&
        (pFuncLeave3WithInfo    == NULL) &&
        (pFuncTailcall3WithInfo == NULL))
    {
        return E_INVALIDARG;
    }

    if (!CORProfilerELT3SlowPathEnabled())
    {
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }

    // Always save onto the function pointers, since we won't know if the profiler
    // is going to track enter/leave until after it returns from Initialize
    m_pEnter3WithInfo    = pFuncEnter3WithInfo;
    m_pLeave3WithInfo    = pFuncLeave3WithInfo;
    m_pTailcall3WithInfo = pFuncTailcall3WithInfo;
    m_pEnter3    = NULL;
    m_pLeave3    = NULL;
    m_pTailcall3 = NULL;

    // ELT3 hooks override Whidbey hooks and Everett hooks.
    m_pEnter2    = NULL;
    m_pLeave2    = NULL;
    m_pTailcall2 = NULL;
    m_pEnter     = NULL;
    m_pLeave     = NULL;
    m_pTailcall  = NULL;

    return DetermineAndSetEnterLeaveFunctionHooksForJit();
}



//---------------------------------------------------------------------------------------
//
// ************************
// Public callback wrappers
// ************************
//
// NOTE: All public callback wrappers must follow the rules stated at the top
// of this file!

// See corprof.idl / MSDN for detailed comments about each of these public
// functions, their parameters, return values, etc.



//---------------------------------------------------------------------------------------
// INITIALIZE CALLBACKS
//

HRESULT EEToProfInterfaceImpl::Initialize()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(kEE2PAllowableWhileInitializing,
        (LF_CORPROF,
         LL_INFO10,
         "**PROF: Calling profiler's Initialize() method.\n"));

    _ASSERTE(m_pProfToEE != NULL);

    // Startup initialization occurs before an EEThread object is created for this
    // thread.
    _ASSERTE(GetThreadNULLOk() == NULL);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->Initialize(m_pProfToEE);
    }
}


HRESULT EEToProfInterfaceImpl::InitializeForAttach(void * pvClientData, UINT cbClientData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(kEE2PAllowableWhileInitializing,
        (LF_CORPROF,
         LL_INFO10,
         "**PROF: Calling profiler's InitializeForAttach() method.\n"));

    _ASSERTE(m_pProfToEE != NULL);

    // Attach initialization occurs on the AttachThread, which does not have an EEThread
    // object
    _ASSERTE(GetThreadNULLOk() == NULL);

    // Should only be called on profilers that support ICorProfilerCallback3
    _ASSERTE(m_pCallback3 != NULL);

    HRESULT hr = E_UNEXPECTED;

    // This wraps the profiler's InitializeForAttach callback in a try / catch. Although
    // most profiler calls are not protected, this initial callback IS, since it's cheap
    // to do so (this is only called once per attach of a profiler), and it would be nice to
    // avoid tearing down the entire process when attaching a profiler that may pass back
    // bogus vtables.
    EX_TRY
    {
        hr = m_pCallback3->InitializeForAttach(m_pProfToEE, pvClientData, cbClientData);
    }
    EX_CATCH
    {
        hr = E_UNEXPECTED;
    }
    // Intentionally swallowing all exceptions, as we don't want a poorly-written
    // profiler that throws or AVs on attach to cause the entire process to go away.
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}

HRESULT EEToProfInterfaceImpl::ProfilerAttachComplete()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: Calling profiler's ProfilerAttachComplete() method.\n"));

    // Attach initialization occurs on the AttachThread, which does not have an EEThread
    // object
    _ASSERTE(GetThreadNULLOk() == NULL);

    // Should only be called on profilers that support ICorProfilerCallback3
    _ASSERTE(m_pCallback3 != NULL);

    HRESULT hr = E_UNEXPECTED;

    // This wraps the profiler's ProfilerAttachComplete callback in a try / catch.
    // Although most profiler calls are not protected, this early callback IS, since it's
    // cheap to do so (this is only called once per attach of a profiler), and it would be
    // nice to avoid tearing down the entire process when attaching a profiler that has
    // serious troubles initializing itself (e.g., in this case, with processing catch-up
    // information).
    EX_TRY
    {
        hr = m_pCallback3->ProfilerAttachComplete();
    }
    EX_CATCH
    {
        hr = E_UNEXPECTED;
    }
    // Intentionally swallowing all exceptions, as we don't want a poorly-written
    // profiler that throws or AVs on attach to cause the entire process to go away.
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}


//---------------------------------------------------------------------------------------
// THREAD EVENTS
//


HRESULT EEToProfInterfaceImpl::ThreadCreated(ThreadID threadId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Preemptive mode is particularly important here.  See comment in
        // EEToProfInterfaceImpl::ThreadDestroyed for more information.
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    // Normally these callback wrappers ask IsGCSpecial() and return without calling the
    // profiler if true. However, ThreadCreated() is the special case where no caller
    // should even get this far for GC Special threads, since our callers need to know to
    // avoid the GCX_PREEMP around the call to this function in the first place. See
    // code:Thread::m_fGCSpecial
    _ASSERTE(!reinterpret_cast<Thread *>(threadId)->IsGCSpecial());

    CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD(threadId,
                                          (LF_CORPROF,
                                           LL_INFO100,
                                           "**PROF: Notifying profiler of created thread. ThreadId: 0x%p.\n",
                                           threadId));

    // Notify the profiler of the newly created thread.
    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ThreadCreated(threadId);
    }
}

HRESULT EEToProfInterfaceImpl::ThreadDestroyed(ThreadID threadId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // See comment below
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    if (reinterpret_cast<Thread *>(threadId)->IsGCSpecial())
        return S_OK;

    // In general, we like callbacks to switch to preemptive before calling into the
    // profiler.  And this is particularly important to do in the ThreadCreated &
    // ThreadDestroyed callbacks.
    //
    // The profiler will typically block in the ThreadDestroyed callback, because
    // it must coordinate the use of this threadid amongst all profiler
    // threads.  For instance, if a separate thread A is walking "this" (via DoStackSnapshot),
    // then the profiler must block in ThreadDestroyed until A is finished.  Otherwise,
    // "this" will complete its destruction before A's walk is complete.
    //
    // Since the profiler will block indefinitely in ThreadDestroyed, we need
    // to switch to preemptive mode.  Otherwise, if another thread B needs to suspend
    // the runtime (due to appdomain unload, GC, etc.), thread B will block
    // waiting for "this" (assuming we allow "this" to remain in cooperative mode),
    // while the profiler forces "this" to block on thread A from
    // the example above.  And thread A may need to block on thread B, since
    // the stackwalking occasionally needs to switch to cooperative to access a
    // hash map (thus DoStackSnapshot forces the switch to cooperative up-front, before
    // the target thread to be walked gets suspended (yet another deadlock possibility)),
    // and switching to cooperative requires a wait until an in-progress GC or
    // EE suspension is complete.  In other words, allowing "this" to remain
    // in cooperative mode could lead to a 3-way deadlock:
    //      "this" waits on A
    //      A waits on B
    //      B waits on "this".
    CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD(threadId,
                                          (LF_CORPROF,
                                           LL_INFO100,
                                           "**PROF: Notifying profiler of destroyed thread. ThreadId: 0x%p.\n",
                                           threadId));

    // From now on, issue no more callbacks for this thread
    SetProfilerCallbacksAllowedForThread((Thread *) threadId, FALSE);

    // Notify the profiler of the destroyed thread
    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ThreadDestroyed(threadId);
    }
}

HRESULT EEToProfInterfaceImpl::ThreadAssignedToOSThread(ThreadID managedThreadId,
                                                        DWORD osThreadId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by notrigger Thread::DetachThread & CorHost::SwitchOutLogicalThreadState
        // which do look to be dangerous times to be triggering a GC
        GC_NOTRIGGER;

        // This is called in notrigger zones (see above), so it's not safe to switch to preemptive
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    if (reinterpret_cast<Thread *>(managedThreadId)->IsGCSpecial())
        return S_OK;

    CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD_EX(
        kEE2PNoTrigger,
        managedThreadId,
        (LF_CORPROF,
        LL_INFO100,
        "**PROF: Notifying profiler of thread assignment.  ThreadId: 0x%p, OSThreadId: 0x%08x\n",
        managedThreadId,
        osThreadId));

    // Notify the profiler of the thread being assigned to the OS thread
    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ThreadAssignedToOSThread(managedThreadId, osThreadId);
    }
}

HRESULT EEToProfInterfaceImpl::ThreadNameChanged(ThreadID managedThreadId,
                                                 ULONG cchName,
                                                 __in_ecount_opt(cchName) WCHAR name[])
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    if (reinterpret_cast<Thread *>(managedThreadId)->IsGCSpecial())
        return S_OK;

    CLR_TO_PROFILER_ENTRYPOINT_FOR_THREAD(managedThreadId,
                                          (LF_CORPROF,
                                           LL_INFO100,
                                           "**PROF: Notifying profiler of thread name change.\n"));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ThreadNameChanged(managedThreadId, cchName, name);
    }
}

//---------------------------------------------------------------------------------------
// EE STARTUP/SHUTDOWN EVENTS
//

HRESULT EEToProfInterfaceImpl::Shutdown()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: Notifying profiler that shutdown is beginning.\n"));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->Shutdown();
    }
}

//---------------------------------------------------------------------------------------
// JIT/FUNCTION EVENTS
//

HRESULT EEToProfInterfaceImpl::FunctionUnloadStarted(FunctionID functionId)
{
    _ASSERTE(!"FunctionUnloadStarted() callback no longer issued");
    return S_OK;
}

HRESULT EEToProfInterfaceImpl::JITCompilationFinished(FunctionID functionId,
                                                      HRESULT hrStatus,
                                                      BOOL fIsSafeToBlock)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: JITCompilationFinished 0x%p, hr=0x%08x.\n",
                                functionId,
                                hrStatus));

    _ASSERTE(functionId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->JITCompilationFinished(functionId, hrStatus, fIsSafeToBlock);
    }
}


HRESULT EEToProfInterfaceImpl::JITCompilationStarted(FunctionID functionId,
                                                     BOOL fIsSafeToBlock)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: JITCompilationStarted 0x%p.\n",
                                functionId));

    // Currently JITCompilationStarted is always called with fIsSafeToBlock==TRUE.  If this ever changes,
    // it's safe to remove this assert, but this should serve as a trigger to change our
    // public documentation to state that this callback is no longer called in preemptive mode all the time.
    _ASSERTE(fIsSafeToBlock);

    _ASSERTE(functionId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->JITCompilationStarted(functionId, fIsSafeToBlock);
    }
}

HRESULT EEToProfInterfaceImpl::DynamicMethodUnloaded(FunctionID functionId)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // RuntimeMethodHandle::Destroy (the caller) moves from QCALL to GCX_COOP
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
        LL_INFO1000,
        "**PROF: DynamicMethodUnloaded 0x%p.\n",
        functionId));

    _ASSERTE(functionId);

    if (m_pCallback9 == NULL)
    {
        return S_OK;
    }

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback9->DynamicMethodUnloaded(functionId);
    }
}

HRESULT EEToProfInterfaceImpl::DynamicMethodJITCompilationFinished(FunctionID functionId,
                                                                   HRESULT hrStatus,
                                                                   BOOL fIsSafeToBlock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: DynamicMethodJITCompilationFinished 0x%p.\n",
                                functionId));

    _ASSERTE(functionId);

    if (m_pCallback8 == NULL)
    {
        return S_OK;
    }

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback8->DynamicMethodJITCompilationFinished(functionId, hrStatus, fIsSafeToBlock);
    }
}

HRESULT EEToProfInterfaceImpl::DynamicMethodJITCompilationStarted(FunctionID functionId,
                                                                  BOOL fIsSafeToBlock,
                                                                  LPCBYTE pILHeader,
                                                                  ULONG cbILHeader)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: DynamicMethodJITCompilationStarted 0x%p.\n",
                                functionId));

    _ASSERTE(functionId);

    // Currently DynamicMethodJITCompilationStarted is always called with fIsSafeToBlock==TRUE.  If this ever changes,
    // it's safe to remove this assert, but this should serve as a trigger to change our
    // public documentation to state that this callback is no longer called in preemptive mode all the time.
    _ASSERTE(fIsSafeToBlock);

    if (m_pCallback8 == NULL)
    {
        return S_OK;
    }

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback8->DynamicMethodJITCompilationStarted(functionId, fIsSafeToBlock, pILHeader, cbILHeader);
    }
}

HRESULT EEToProfInterfaceImpl::JITCachedFunctionSearchStarted(
                                    /* [in] */  FunctionID functionId,
                                    /* [out] */ BOOL       *pbUseCachedFunction)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: JITCachedFunctionSearchStarted 0x%p.\n",
                                functionId));
    _ASSERTE(functionId);
    _ASSERTE(pbUseCachedFunction != NULL);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->JITCachedFunctionSearchStarted(functionId, pbUseCachedFunction);
    }
}

HRESULT EEToProfInterfaceImpl::JITCachedFunctionSearchFinished(
                                    /* [in] */  FunctionID functionId,
                                    /* [in] */  COR_PRF_JIT_CACHE result)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: JITCachedFunctionSearchFinished 0x%p, %s.\n",
                                functionId,
                                (result == COR_PRF_CACHED_FUNCTION_FOUND ?
                                    "Cached function found" :
                                    "Cached function not found")));

    _ASSERTE(functionId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->JITCachedFunctionSearchFinished(functionId, result);
    }
}


HRESULT EEToProfInterfaceImpl::JITFunctionPitched(FunctionID functionId)
{
    _ASSERTE(!"JITFunctionPitched() callback no longer issued");
    return S_OK;
}

HRESULT EEToProfInterfaceImpl::JITInlining(
    /* [in] */  FunctionID    callerId,
    /* [in] */  FunctionID    calleeId,
    /* [out] */ BOOL *        pfShouldInline)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: JITInlining caller: 0x%p, callee: 0x%p.\n",
                                callerId,
                                calleeId));

    _ASSERTE(callerId);
    _ASSERTE(calleeId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->JITInlining(callerId, calleeId, pfShouldInline);
    }
}

HRESULT EEToProfInterfaceImpl::ReJITCompilationStarted(
    /* [in] */  FunctionID    functionId,
    /* [in] */  ReJITID       reJitId,
    /* [in] */  BOOL          fIsSafeToBlock)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // The JIT / MethodDesc code likely hold locks while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ReJITCompilationStarted 0x%p 0x%p.\n",
                                functionId, reJitId));

    // Should only be called on profilers that support ICorProfilerCallback4
    _ASSERTE(m_pCallback4 != NULL);

    // Currently ReJITCompilationStarted is always called with fIsSafeToBlock==TRUE.  If this ever changes,
    // it's safe to remove this assert, but this should serve as a trigger to change our
    // public documentation to state that this callback is no longer called in preemptive mode all the time.
    _ASSERTE(fIsSafeToBlock);

    _ASSERTE(functionId);
    _ASSERTE(reJitId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback4->ReJITCompilationStarted(functionId, reJitId, fIsSafeToBlock);
    }
}

HRESULT EEToProfInterfaceImpl::GetReJITParameters(
    /* [in] */  ModuleID      moduleId,
    /* [in] */  mdMethodDef   methodId,
    /* [in] */  ICorProfilerFunctionControl *
                                  pFunctionControl)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // The ReJIT code holds a lock while this callback is made

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: GetReJITParameters 0x%p 0x%p.\n",
                                moduleId, methodId));

    // Should only be called on profilers that support ICorProfilerCallback4
    _ASSERTE(m_pCallback4 != NULL);

    _ASSERTE(moduleId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback4->GetReJITParameters(moduleId, methodId, pFunctionControl);
    }
}

HRESULT EEToProfInterfaceImpl::ReJITCompilationFinished(
    /* [in] */  FunctionID    functionId,
    /* [in] */  ReJITID       reJitId,
    /* [in] */  HRESULT       hrStatus,
    /* [in] */  BOOL          fIsSafeToBlock)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // ReJit holds a lock as well as possibly others...

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ReJITCompilationFinished 0x%p 0x%p hr=0x%x.\n",
                                functionId, reJitId, hrStatus));

    // Should only be called on profilers that support ICorProfilerCallback4
    _ASSERTE(m_pCallback4 != NULL);

    _ASSERTE(functionId);
    _ASSERTE(reJitId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback4->ReJITCompilationFinished(functionId, reJitId, hrStatus, fIsSafeToBlock);
    }
}


HRESULT EEToProfInterfaceImpl::ReJITError(
    /* [in] */  ModuleID      moduleId,
    /* [in] */  mdMethodDef   methodId,
    /* [in] */  FunctionID    functionId,
    /* [in] */  HRESULT       hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ReJITError 0x%p 0x%x 0x%p 0x%x.\n",
                                moduleId, methodId, functionId, hrStatus));

    // Should only be called on profilers that support ICorProfilerCallback4
    _ASSERTE(m_pCallback4 != NULL);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback4->ReJITError(moduleId, methodId, functionId, hrStatus);
    }
}

//---------------------------------------------------------------------------------------
// MODULE EVENTS
//

HRESULT EEToProfInterfaceImpl::ModuleLoadStarted(ModuleID moduleId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // This has historically not run in preemptive, and is called from cooperative-mode
        // functions. However, since we're triggers, it might actually be safe to consider
        // letting this run in preemptive mode.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: ModuleLoadStarted 0x%p.\n",
                                moduleId));

    _ASSERTE(moduleId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ModuleLoadStarted(moduleId);
    }
}


HRESULT EEToProfInterfaceImpl::ModuleLoadFinished(
    ModuleID    moduleId,
    HRESULT        hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: ModuleLoadFinished 0x%p.\n",
                                moduleId));

    _ASSERTE(moduleId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ModuleLoadFinished(moduleId, hrStatus);
    }
}



HRESULT EEToProfInterfaceImpl::ModuleUnloadStarted(
    ModuleID    moduleId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: ModuleUnloadStarted 0x%p.\n",
                                moduleId));

    _ASSERTE(moduleId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ModuleUnloadStarted(moduleId);
    }
}


HRESULT EEToProfInterfaceImpl::ModuleUnloadFinished(
    ModuleID    moduleId,
    HRESULT        hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: ModuleUnloadFinished 0x%p.\n",
                                moduleId));
    _ASSERTE(moduleId != 0);
    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ModuleUnloadFinished(moduleId, hrStatus);
    }
}


HRESULT EEToProfInterfaceImpl::ModuleAttachedToAssembly(
    ModuleID    moduleId,
    AssemblyID  AssemblyId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: ModuleAttachedToAssembly 0x%p, 0x%p.\n",
                                moduleId,
                                AssemblyId));

    _ASSERTE(moduleId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ModuleAttachedToAssembly(moduleId, AssemblyId);
    }
}

HRESULT EEToProfInterfaceImpl::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
        LL_INFO10,
        "**PROF: ModuleInMemorySymbolsUpdated.  moduleId: 0x%p.\n",
        moduleId
        ));
    HRESULT hr = S_OK;

    _ASSERTE(IsCallback7Supported());

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        hr = m_pCallback7->ModuleInMemorySymbolsUpdated(moduleId);
    }

    return hr;
}

//---------------------------------------------------------------------------------------
// CLASS EVENTS
//

HRESULT EEToProfInterfaceImpl::ClassLoadStarted(
    ClassID     classId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // UnresolvedClassLock typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: ClassLoadStarted 0x%p.\n",
                                classId));

    _ASSERTE(classId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ClassLoadStarted(classId);
    }
}


HRESULT EEToProfInterfaceImpl::ClassLoadFinished(
    ClassID     classId,
    HRESULT     hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // UnresolvedClassLock typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: ClassLoadFinished 0x%p, 0x%08x.\n",
                                classId,
                                hrStatus));

    _ASSERTE(classId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ClassLoadFinished(classId, hrStatus);
    }
}


HRESULT EEToProfInterfaceImpl::ClassUnloadStarted(
    ClassID     classId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Although not typical, it's possible for UnresolvedClassLock to be held
        // during this callback.  This can occur if, during the class load, an
        // exception is thrown, and EEClass::Destruct is called from the catch clause
        // inside ClassLoader::CreateTypeHandleForTypeDefThrowing.

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: ClassUnloadStarted 0x%p.\n",
                                classId));

    _ASSERTE(classId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ClassUnloadStarted(classId);
    }
}


HRESULT EEToProfInterfaceImpl::ClassUnloadFinished(
    ClassID     classId,
    HRESULT     hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Locks can be held when this is called.  See comment in ClassUnloadStarted

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: ClassUnloadFinished 0x%p, 0x%08x.\n",
                                classId,
                                hrStatus));

    _ASSERTE(classId);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ClassUnloadFinished(classId, hrStatus);
    }
}

//---------------------------------------------------------------------------------------
// APPDOMAIN EVENTS
//

HRESULT EEToProfInterfaceImpl::AppDomainCreationStarted(
    AppDomainID appDomainId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AppDomainCreationStarted 0x%p.\n",
                                appDomainId));

    _ASSERTE(appDomainId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AppDomainCreationStarted(appDomainId);
    }
}


HRESULT EEToProfInterfaceImpl::AppDomainCreationFinished(
    AppDomainID appDomainId,
    HRESULT     hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AppDomainCreationFinished 0x%p, 0x%08x.\n",
                                appDomainId,
                                hrStatus));

    _ASSERTE(appDomainId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AppDomainCreationFinished(appDomainId, hrStatus);
    }
}

HRESULT EEToProfInterfaceImpl::AppDomainShutdownStarted(
    AppDomainID appDomainId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AppDomainShutdownStarted 0x%p.\n",
                                appDomainId));

    _ASSERTE(appDomainId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AppDomainShutdownStarted(appDomainId);
    }
}

HRESULT EEToProfInterfaceImpl::AppDomainShutdownFinished(
    AppDomainID appDomainId,
    HRESULT     hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AppDomainShutdownFinished 0x%p, 0x%08x.\n",
                                appDomainId,
                                hrStatus));

    _ASSERTE(appDomainId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AppDomainShutdownFinished(appDomainId, hrStatus);
    }
}

//---------------------------------------------------------------------------------------
// ASSEMBLY EVENTS
//

HRESULT EEToProfInterfaceImpl::AssemblyLoadStarted(
    AssemblyID  assemblyId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // This has historically not run in preemptive, and is called from cooperative-mode
        // functions. However, since we're triggers, it might actually be safe to consider
        // letting this run in preemptive mode.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AssemblyLoadStarted 0x%p.\n",
                                assemblyId));

    _ASSERTE(assemblyId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AssemblyLoadStarted(assemblyId);
    }
}

HRESULT EEToProfInterfaceImpl::AssemblyLoadFinished(
    AssemblyID  assemblyId,
    HRESULT     hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // This has historically not run in preemptive, and is called from cooperative-mode
        // functions. However, since we're triggers, it might actually be safe to consider
        // letting this run in preemptive mode.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AssemblyLoadFinished 0x%p, 0x%08x.\n",
                                assemblyId,
                                hrStatus));

    _ASSERTE(assemblyId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AssemblyLoadFinished(assemblyId, hrStatus);
    }
}

HRESULT EEToProfInterfaceImpl::AssemblyUnloadStarted(
    AssemblyID  assemblyId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AssemblyUnloadStarted 0x%p.\n",
                                assemblyId));

    _ASSERTE(assemblyId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AssemblyUnloadStarted(assemblyId);
    }
}

HRESULT EEToProfInterfaceImpl::AssemblyUnloadFinished(
    AssemblyID  assemblyId,
    HRESULT     hrStatus)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AssemblyUnloadFinished 0x%p, 0x%08x.\n",
                                assemblyId,
                                hrStatus));

    _ASSERTE(assemblyId != 0);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->AssemblyUnloadFinished(assemblyId, hrStatus);
    }
}

//---------------------------------------------------------------------------------------
// TRANSITION EVENTS
//

HRESULT EEToProfInterfaceImpl::UnmanagedToManagedTransition(
    FunctionID functionId,
    COR_PRF_TRANSITION_REASON reason)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10000,
                                "**PROF: UnmanagedToManagedTransition 0x%p.\n",
                                functionId));

    _ASSERTE(reason == COR_PRF_TRANSITION_CALL || reason == COR_PRF_TRANSITION_RETURN);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->UnmanagedToManagedTransition(functionId, reason);
    }
}

HRESULT EEToProfInterfaceImpl::ManagedToUnmanagedTransition(
    FunctionID functionId,
    COR_PRF_TRANSITION_REASON reason)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    _ASSERTE(reason == COR_PRF_TRANSITION_CALL || reason == COR_PRF_TRANSITION_RETURN);

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10000,
                                "**PROF: ManagedToUnmanagedTransition 0x%p.\n",
                                functionId));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ManagedToUnmanagedTransition(functionId, reason);
    }
}

//---------------------------------------------------------------------------------------
// EXCEPTION EVENTS
//

HRESULT EEToProfInterfaceImpl::ExceptionThrown(
    ObjectID thrownObjectId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Preemptive mode would be bad, dude.  There's an objectId in the param list!
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ExceptionThrown. ObjectID: 0x%p. ThreadID: 0x%p\n",
                                thrownObjectId,
                                GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionThrown(thrownObjectId);
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionSearchFunctionEnter(
    FunctionID functionId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ExceptionSearchFunctionEnter. ThreadID: 0x%p, functionId: 0x%p\n",
                                GetThreadNULLOk(),
                                functionId));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionSearchFunctionEnter(functionId);
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionSearchFunctionLeave()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ExceptionSearchFunctionLeave. ThreadID: 0x%p\n",
                                GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionSearchFunctionLeave();
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionSearchFilterEnter(FunctionID functionId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ExceptionSearchFilterEnter. ThreadID: 0x%p, functionId: 0x%p\n",
                                GetThreadNULLOk(),
                                functionId));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionSearchFilterEnter(functionId);
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionSearchFilterLeave()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ExceptionFilterLeave. ThreadID: 0x%p\n",
                                GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionSearchFilterLeave();
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionSearchCatcherFound(FunctionID functionId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ExceptionSearchCatcherFound.  ThreadID: 0x%p\n",
                                GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionSearchCatcherFound(functionId);
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionOSHandlerEnter(FunctionID functionId)
{
    _ASSERTE(!"ExceptionOSHandlerEnter() callback no longer issued");
    return S_OK;
}

HRESULT EEToProfInterfaceImpl::ExceptionOSHandlerLeave(FunctionID functionId)
{
    _ASSERTE(!"ExceptionOSHandlerLeave() callback no longer issued");
    return S_OK;
}

HRESULT EEToProfInterfaceImpl::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by COMPlusUnwindCallback, which is notrigger
        GC_NOTRIGGER;

        // Cannot enable preemptive GC here, since the stack may not be in a GC-friendly state.
        // Thus, the profiler cannot block on this call.
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: ExceptionUnwindFunctionEnter. ThreadID: 0x%p, functionId: 0x%p\n",
        GetThreadNULLOk(),
        functionId));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionUnwindFunctionEnter(functionId);
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionUnwindFunctionLeave()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by COMPlusUnwindCallback, which is notrigger
        GC_NOTRIGGER;

        // Cannot enable preemptive GC here, since the stack may not be in a GC-friendly state.
        // Thus, the profiler cannot block on this call.
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: ExceptionUnwindFunctionLeave. ThreadID: 0x%p\n",
        GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionUnwindFunctionLeave();
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by COMPlusUnwindCallback, which is notrigger
        GC_NOTRIGGER;

        // Cannot enable preemptive GC here, since the stack may not be in a GC-friendly state.
        // Thus, the profiler cannot block on this call.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: ExceptionUnwindFinallyEnter. ThreadID: 0x%p, functionId: 0x%p\n",
        GetThreadNULLOk(),
        functionId));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionUnwindFinallyEnter(functionId);
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionUnwindFinallyLeave()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by COMPlusUnwindCallback, which is notrigger
        GC_NOTRIGGER;

        // Cannot enable preemptive GC here, since the stack may not be in a GC-friendly state.
        // Thus, the profiler cannot block on this call.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: ExceptionUnwindFinallyLeave. ThreadID: 0x%p\n",
        GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionUnwindFinallyLeave();
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by COMPlusUnwindCallback, which is notrigger
        GC_NOTRIGGER;

        // Cannot enable preemptive GC here, since the stack may not be in a GC-friendly state.
        // Thus, the profiler cannot block on this call.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO1000, "**PROF: ExceptionCatcherEnter.        ThreadID: 0x%p, functionId: 0x%p\n",
        GetThreadNULLOk(),
        functionId));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionCatcherEnter(functionId, objectId);
    }
}

HRESULT EEToProfInterfaceImpl::ExceptionCatcherLeave()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Cannot enable preemptive GC here, since the stack may not be in a GC-friendly state.
        // Thus, the profiler cannot block on this call.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ExceptionCatcherLeave.        ThreadID: 0x%p\n",
                                GetThreadNULLOk()));


    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ExceptionCatcherLeave();
    }
}


//---------------------------------------------------------------------------------------
// COM Callable Wrapper EVENTS
//

HRESULT EEToProfInterfaceImpl::COMClassicVTableCreated(
    /* [in] */ ClassID classId,
    /* [in] */ REFGUID implementedIID,
    /* [in] */ void *pVTable,
    /* [in] */ ULONG cSlots)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: COMClassicWrapperCreated %#x %#08x... %#x %d.\n",
                                classId,
                                implementedIID.Data1,
                                pVTable,
                                cSlots));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->COMClassicVTableCreated(classId, implementedIID, pVTable, cSlots);
    }
}

HRESULT EEToProfInterfaceImpl::COMClassicVTableDestroyed(
    /* [in] */ ClassID classId,
    /* [in] */ REFGUID implementedIID,
    /* [in] */ void *pVTable)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Yay!
        ASSERT_NO_EE_LOCKS_HELD();

    }
    CONTRACTL_END;

    // NOTE: There is no problem with this code, and it is ready and willing
    // to be called.  However, this callback is intentionally not being
    // issued currently.  See comment in ComMethodTable::Cleanup() for more
    // information.

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: COMClassicWrapperDestroyed %#x %#08x... %#x.\n",
                                classId,
                                implementedIID.Data1,
                                pVTable));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->COMClassicVTableDestroyed(classId, implementedIID, pVTable);
    }
}


//---------------------------------------------------------------------------------------
// GC THREADING EVENTS
//

HRESULT EEToProfInterfaceImpl::RuntimeSuspendStarted(
    COR_PRF_SUSPEND_REASON suspendReason)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Although the contract system doesn't yell if I mark this GC_TRIGGERS, it's safest
        // not to allow a GC to occur while we're suspending / resuming the runtime, this is
        // the thread trying to do a GC.  So if the profiler tries to trigger another GC from
        // this thread at this time, we might see potential recursion or deadlock.
        GC_NOTRIGGER;

        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO100,
        "**PROF: RuntimeSuspendStarted. ThreadID 0x%p.\n",
        GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->RuntimeSuspendStarted(suspendReason);
    }
}

HRESULT EEToProfInterfaceImpl::RuntimeSuspendFinished()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Although the contract system doesn't yell if I mark this GC_TRIGGERS, it's safest
        // not to allow a GC to occur while we're suspending / resuming the runtime, this is
        // the thread trying to do a GC.  So if the profiler tries to trigger another GC from
        // this thread at this time, we might see potential recursion or deadlock.
        GC_NOTRIGGER;

        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO100,
        "**PROF: RuntimeSuspendFinished. ThreadID 0x%p.\n",
        GetThreadNULLOk()));


    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->RuntimeSuspendFinished();
    }
}

HRESULT EEToProfInterfaceImpl::RuntimeSuspendAborted()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Although the contract system doesn't yell if I mark this GC_TRIGGERS, it's safest
        // not to allow a GC to occur while we're suspending / resuming the runtime, this is
        // the thread trying to do a GC.  So if the profiler tries to trigger another GC from
        // this thread at this time, we might see potential recursion or deadlock.
        GC_NOTRIGGER;

        // NOTE: I have no empirical data for gc mode: none of the self-host BVTs call this
        // So for now, assume this is callable in any mode.
        // This has historically not caused a mode change to preemptive, and is called from
        // cooperative-mode functions.  Also, switching to preemptive while we're suspending
        // the runtime just seems like a bad idea.
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO100,
        "**PROF: RuntimeSuspendAborted. ThreadID 0x%p.\n",
        GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->RuntimeSuspendAborted();
    }
}

HRESULT EEToProfInterfaceImpl::RuntimeResumeStarted()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // This has historically not caused a mode change to preemptive, and is called from
        // cooperative-mode functions.  Also, switching to preemptive while we're resuming
        // the runtime just seems like a bad idea.
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: RuntimeResumeStarted. ThreadID 0x%p.\n",
                                GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->RuntimeResumeStarted();
    }
}

HRESULT EEToProfInterfaceImpl::RuntimeResumeFinished()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: RuntimeResumeFinished. ThreadID 0x%p.\n",
                                GetThreadNULLOk()));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->RuntimeResumeFinished();
    }
}

HRESULT EEToProfInterfaceImpl::RuntimeThreadSuspended(ThreadID suspendedThreadId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by Thread::SuspendThread, which is notrigger.
        GC_NOTRIGGER;

        // Although I've verified we're called from both coop and preemp, we need to
        // avoid switching to preemptive to satisfy our notrigger paths.
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    if (reinterpret_cast<Thread *>(suspendedThreadId)->IsGCSpecial())
        return S_OK;

    // NOTE: We cannot use the standard CLR_TO_PROFILER_ENTRYPOINT macro here because
    // we might be called at a time when profiler callbacks have been disallowed for
    // this thread.  So we cannot simply ASSERT that callbacks are allowed (as this macro
    // does).  Instead, we must explicitly check for this condition and return gracefully
    // if callbacks are disallowed.  So the macro is unwrapped here manually

    CHECK_PROFILER_STATUS(kEE2PNone);

    LOG((LF_CORPROF, LL_INFO1000, "**PROF: RuntimeThreadSuspended. ThreadID 0x%p.\n",
         suspendedThreadId));

    // NOTE: We're notrigger, so we cannot switch to preemptive mode.

    // We may have already indicated to the profiler that this thread has died, but
    // the runtime may continue to suspend this thread during the process of destroying
    // the thread, so we do not want to indicate to the profiler these suspensions.
    if (!ProfilerCallbacksAllowedForThread((Thread *) suspendedThreadId))
    {
        return S_OK;
    }

    // Remaining essentials from our entrypoint macros with kEE2PNoTrigger flag
    SetCallbackStateFlagsHolder csf(COR_PRF_CALLBACKSTATE_INCALLBACK);
    _ASSERTE(m_pCallback2 != NULL);

    {
        // SCOPE: ForbidSuspendThreadHolder

        // The ForbidSuspendThreadHolder prevents deadlocks under the following scenario:
        // 1) Thread A blocks waiting for the current GC to complete (this can happen if A is trying to
        //      switch to cooperative during a GC).
        // 2) This causes us to send a RuntimeThreadSuspended callback to the profiler.  (Although
        //      A isn't technically being "suspended", this blocking is considered suspension as far as the
        //      profapi is concerned.)
        // 3) Profiler, in turn, may take one of its own private locks to synchronize this callback with
        //      the profiler's attempt to hijack thread A.  Specifically, the profiler knows it's not allowed
        //      to hijack A if A is getting suspended by the runtime, because this suspension might be due to
        //      the GC trying to hijack A.  And if the GC tries to hijack A at the same time as the profiler
        //      hijacking A and the profiler wins, then GC asserts because A is no longer at the IP that
        //      the GC thought (VsWhidbey 428477, 429741)
        // 4) Meanwhile, thread B (GC thread) is suspending the runtime, and calls Thread::SuspendThread()
        //      on A.  This is the bad thing we're trying to avoid, because when this happens, we call into
        //      the profiler AGAIN with RuntimeThreadSuspended for thread A, and the profiler again
        //      tries to grab the lock it acquired in step 3).   Yes, at this point we now have two simultaneous
        //      calls into the profiler's RuntimeThreadSuspended() callback.  One saying A is suspending A
        //      (3 above), and one saying B is suspending A (this step (4)).  The problem is that A is now officially
        //      hard suspended, OS-style, so the lock acquired on 3) ain't never getting released until
        //      A is resumed.  But A won't be resumed until B resumes it.  And B won't resume A until
        //      the profiler returns from its RuntimeThreadSuspended callback.  And  the profiler
        //      can't return from its RuntimeThreadSuspended callback until it acquires this lock it tried to
        //      acquire in 4).  And it can't acquire this lock until A is finally resumed so that the acquire
        //      from 3) is released.  Have we gone in a circle yet?
        // In order to avoid 4) we inc the ForbidSuspendThread count during 3) to prevent the hard suspension
        // (4) from occurring until 3) is completely done.  It's sufficient to determine we're in 3) by noting
        // whether the callback is reporting that a thread is "suspending itself" (i.e., suspendedThreadId == threadId)

        ForbidSuspendThreadHolder forbidSuspendThread((Thread *) suspendedThreadId == GetThreadNULLOk());

        {
            // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
            // whose try/catch blocks aren't visible to the contract system
            PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
            return m_pCallback2->RuntimeThreadSuspended(suspendedThreadId);
        }
    }
}

HRESULT EEToProfInterfaceImpl::RuntimeThreadResumed(ThreadID resumedThreadId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This gets called in response to another profapi function:
        // ICorProfilerInfo2::DoStackSnapshot!  And that dude is called asynchronously and
        // must therefore never cause a GC.
        // Other reasons for notrigger: also called by notrigger dudes Thread::SysStartSuspendForDebug,
        // CheckSuspended, Thread::IsExecutingWithinCer, Thread::IsExecutingWithinCer,
        // UnwindFrames
        GC_NOTRIGGER;

        // Although we cannot trigger, verified empirically that this called coop & preemp
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    if (reinterpret_cast<Thread *>(resumedThreadId)->IsGCSpecial())
        return S_OK;

    // NOTE: We cannot use the standard CLR_TO_PROFILER_ENTRYPOINT macro here because
    // we might be called at a time when profiler callbacks have been disallowed for
    // this thread.  So we cannot simply ASSERT that callbacks are allowed (as this macro
    // does).  Instead, we must explicitly check for this condition and return gracefully
    // if callbacks are disallowed.  So the macro is unwrapped here manually

    CHECK_PROFILER_STATUS(kEE2PNone);

    LOG((LF_CORPROF, LL_INFO1000, "**PROF: RuntimeThreadResumed. ThreadID 0x%p.\n", resumedThreadId));

    // NOTE: We're notrigger, so we cannot switch to preemptive mode.

    // We may have already indicated to the profiler that this thread has died, but
    // the runtime may resume this thread during the process of destroying
    // the thread, so we do not want to indicate to the profiler these resumes.
    if (!ProfilerCallbacksAllowedForThread((Thread *) resumedThreadId))
    {
        return S_OK;
    }

    // Remaining essentials from our entrypoint macros with kEE2PNoTrigger flag
    SetCallbackStateFlagsHolder csf(COR_PRF_CALLBACKSTATE_INCALLBACK);
    _ASSERTE(m_pCallback2 != NULL);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->RuntimeThreadResumed(resumedThreadId);
    }
}

//---------------------------------------------------------------------------------------
// GC EVENTS
//

HRESULT EEToProfInterfaceImpl::ObjectAllocated(
    /* [in] */ ObjectID objectId,
    /* [in] */ ClassID classId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Preemptive mode would be bad, dude.  There's an objectId in the param list!
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // CrstAppDomainHandleTable can be held while this is called

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO1000,
                                "**PROF: ObjectAllocated. ObjectID: 0x%p.  ClassID: 0x%p\n",
                                objectId,
                                classId));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ObjectAllocated(objectId, classId);
    }
}


HRESULT EEToProfInterfaceImpl::MovedReferences(GCReferencesData *pData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This is called by the thread doing a GC WHILE it does the GC
        GC_NOTRIGGER;

        // This is called by the thread doing a GC WHILE it does the GC
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: MovedReferences.\n"));

    _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());

    if (pData->curIdx == 0)
    {
        return S_OK;
    }

    HRESULT hr = S_OK;

    if (pData->compactingCount != 0)
    {
        _ASSERTE(pData->curIdx == pData->compactingCount);

        if (m_pCallback4 != NULL)
        {
            // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
            // whose try/catch blocks aren't visible to the contract system
            PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
            hr = m_pCallback4->MovedReferences2((ULONG)pData->curIdx,
                                                (ObjectID *)pData->arrpbMemBlockStartOld,
                                                (ObjectID *)pData->arrpbMemBlockStartNew,
                                                (SIZE_T *)pData->arrMemBlockSize);
            if (FAILED(hr))
                return hr;
        }

#ifdef HOST_64BIT
        // Recompute sizes as ULONGs for legacy callback
        for (ULONG i = 0; i < pData->curIdx; i++)
            pData->arrULONG[i] = (pData->arrMemBlockSize[i] > UINT32_MAX) ? UINT32_MAX : (ULONG)pData->arrMemBlockSize[i];
#endif

        {
            // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
            // whose try/catch blocks aren't visible to the contract system
            PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
            hr = m_pCallback2->MovedReferences((ULONG)pData->curIdx,
                                               (ObjectID *)pData->arrpbMemBlockStartOld,
                                               (ObjectID *)pData->arrpbMemBlockStartNew,
                                               pData->arrULONG);
        }
    }
    else
    {
        if (m_pCallback4 != NULL)
        {
            // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
            // whose try/catch blocks aren't visible to the contract system
            PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
            hr = m_pCallback4->SurvivingReferences2((ULONG)pData->curIdx,
                                                    (ObjectID *)pData->arrpbMemBlockStartOld,
                                                    (SIZE_T *)pData->arrMemBlockSize);
            if (FAILED(hr))
                return hr;
        }

#ifdef HOST_64BIT
        // Recompute sizes as ULONGs for legacy callback
        for (ULONG i = 0; i < pData->curIdx; i++)
            pData->arrULONG[i] = (pData->arrMemBlockSize[i] > UINT32_MAX) ? UINT32_MAX : (ULONG)pData->arrMemBlockSize[i];
#endif

        {
            // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
            // whose try/catch blocks aren't visible to the contract system
            PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
            hr = m_pCallback2->SurvivingReferences((ULONG)pData->curIdx,
                                                   (ObjectID *)pData->arrpbMemBlockStartOld,
                                                   pData->arrULONG);
        }
    }

    return hr;
}

HRESULT EEToProfInterfaceImpl::NotifyAllocByClass(AllocByClassData *pData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This is called by the thread doing a GC WHILE it does the GC
        GC_NOTRIGGER;

        // This is called by the thread doing a GC WHILE it does the GC
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: ObjectsAllocatedByClass.\n"));

    _ASSERTE(pData != NULL);
    _ASSERTE(pData->iHash > 0);

    // If the arrays are not long enough, get rid of them.
    if (pData->cLength != 0 && pData->iHash > pData->cLength)
    {
        _ASSERTE(pData->arrClsId != NULL && pData->arrcObjects != NULL);
        delete [] pData->arrClsId;
        delete [] pData->arrcObjects;
        pData->cLength = 0;
    }

    // If there are no arrays, must allocate them.
    if (pData->cLength == 0)
    {
        pData->arrClsId = new (nothrow) ClassID[pData->iHash];
        if (pData->arrClsId == NULL)
        {
            return E_OUTOFMEMORY;
        }

        pData->arrcObjects = new (nothrow) ULONG[pData->iHash];
        if (pData->arrcObjects == NULL)
        {
            delete [] pData->arrClsId;
            pData->arrClsId= NULL;

            return E_OUTOFMEMORY;
        }

        // Indicate that the memory was successfully allocated
        pData->cLength = pData->iHash;
    }

    // Now copy all the data
    HASHFIND hFind;
    CLASSHASHENTRY * pCur = (CLASSHASHENTRY *) pData->pHashTable->FindFirstEntry(&hFind);
    size_t iCur = 0;    // current index for arrays

    while (pCur != NULL)
    {
        _ASSERTE(iCur < pData->iHash);

        pData->arrClsId[iCur] = pCur->m_clsId;
        pData->arrcObjects[iCur] = (DWORD) pCur->m_count;

        // Move to the next entry
        iCur++;
        pCur = (CLASSHASHENTRY *) pData->pHashTable->FindNextEntry(&hFind);
    }

    _ASSERTE(iCur == pData->iHash);

    // Now communicate the results to the profiler
    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ObjectsAllocatedByClass((ULONG)pData->iHash, pData->arrClsId, pData->arrcObjects);
    }
}

HRESULT EEToProfInterfaceImpl::ObjectReference(ObjectID objId,
                                               ClassID classId,
                                               ULONG cNumRefs,
                                               ObjectID *arrObjRef)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This is called by the thread doing a GC WHILE it does the GC
        GC_NOTRIGGER;

        // This is called by the thread doing a GC WHILE it does the GC
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO100000,
        "**PROF: ObjectReferences.\n"));

    _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->ObjectReferences(objId, classId, cNumRefs, arrObjRef);
    }
}


HRESULT EEToProfInterfaceImpl::FinalizeableObjectQueued(BOOL isCritical, ObjectID objectID)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Can't be in preemptive when we're dealing in objectIDs!
        // However, it's possible we're on a non-EE Thread--that happens when this
        // is a server-mode GC thread.
        MODE_COOPERATIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO100,
                                "**PROF: Notifying profiler of finalizeable object.\n"));

    _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->FinalizeableObjectQueued(isCritical ? COR_PRF_FINALIZER_CRITICAL : 0, objectID);
    }
}


HRESULT EEToProfInterfaceImpl::RootReferences2(GCReferencesData *pData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This is called by the thread doing a GC WHILE it does the GC
        GC_NOTRIGGER;

        // This is called by the thread doing a GC WHILE it does the GC
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: RootReferences2.\n"));

    _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());

    HRESULT hr = S_OK;

    COR_PRF_GC_ROOT_FLAGS flags[kcReferencesMax];

    _ASSERTE(pData->curIdx <= kcReferencesMax);
    for (ULONG i = 0; i < pData->curIdx; i++)
    {
        flags[i] = (COR_PRF_GC_ROOT_FLAGS)(pData->arrULONG[i] & 0xffff);
        pData->arrULONG[i] >>= 16;
    }

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        hr = m_pCallback2->RootReferences2((ULONG)pData->curIdx,
                                          (ObjectID *)pData->arrpbMemBlockStartOld,
                                          (COR_PRF_GC_ROOT_KIND *)pData->arrULONG,
                                          flags,
                                          (ObjectID *)pData->arrpbMemBlockStartNew);
        if (FAILED(hr))
            return hr;
    }

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        hr = m_pCallback2->RootReferences((ULONG)pData->curIdx, (ObjectID *)pData->arrpbMemBlockStartOld);
    }

    return hr;
}


HRESULT EEToProfInterfaceImpl::ConditionalWeakTableElementReferences(GCReferencesData * pData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This is called by the thread doing a GC WHILE it does the GC
        GC_NOTRIGGER;

        // This is called by the thread doing a GC WHILE it does the GC
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: ConditionalWeakTableElementReferences.\n"));

    _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());

    HRESULT hr = S_OK;

    _ASSERTE(pData->curIdx <= kcReferencesMax);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        hr = m_pCallback5->ConditionalWeakTableElementReferences(
                                          (ULONG)pData->curIdx,
                                          (ObjectID *)pData->arrpbMemBlockStartOld,
                                          (ObjectID *)pData->arrpbMemBlockStartNew,
                                          (GCHandleID *)pData->arrpbRootId);
    }

    return hr;
}

HRESULT EEToProfInterfaceImpl::HandleCreated(UINT_PTR handleId, ObjectID initialObjectId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by HndCreateHandle which is notrigger
        GC_NOTRIGGER;

        // This can be called in preemptive mode if initialObjectId is NULL.
        // Otherwise, this will be in cooperative mode.  Note that, although this
        // can be called in preemptive, when it's called in cooperative we must not
        // switch to preemptive (as we normally do in callbacks) and must not trigger,
        // as this would really tick off some of our callers (as well as invalidating
        // initialObjectId).
        if (initialObjectId != NULL)
        {
            MODE_COOPERATIVE;
        }
        else
        {
            MODE_ANY;
        }

        // Yay!
        CAN_TAKE_LOCK;

        // CrstAppDomainHandleTable can be held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: HandleCreated.\n"));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->HandleCreated(handleId, initialObjectId);
    }
}

HRESULT EEToProfInterfaceImpl::HandleDestroyed(UINT_PTR handleId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Called by HndDestroyHandle, which is notrigger.  But HndDestroyHandle is also
        // MODE_ANY, so perhaps we can change the whole call path to be triggers?
        GC_NOTRIGGER;

        // Although we're called from a notrigger function, I verified empirically that
        // this is called coop & preemp
        MODE_ANY;

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock is typically held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: HandleDestroyed.\n"));

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->HandleDestroyed(handleId);
    }
}

HRESULT EEToProfInterfaceImpl::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This is called by the thread doing a GC WHILE it does the GC
        GC_NOTRIGGER;

        // This is called by the thread doing a GC WHILE it does the GC
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: GarbageCollectionStarted.\n"));

    _ASSERTE(!CORProfilerTrackGC() || !GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->GarbageCollectionStarted(cGenerations, generationCollected, reason);
    }
}

HRESULT EEToProfInterfaceImpl::GarbageCollectionFinished()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This is called by the thread doing a GC WHILE it does the GC
        GC_NOTRIGGER;

        // This is called by the thread doing a GC WHILE it does the GC
        if (GetThreadNULLOk()) { MODE_COOPERATIVE; }

        // Yay!
        CAN_TAKE_LOCK;

        // Thread store lock normally held during this callback

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO10000,
        "**PROF: GarbageCollectionFinished.\n"));

    _ASSERTE(!CORProfilerTrackGC() || !GCHeapUtilities::GetGCHeap()->IsConcurrentGCEnabled());

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback2->GarbageCollectionFinished();
    }
}

HRESULT EEToProfInterfaceImpl::ProfilerDetachSucceeded()
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

        // ProfilingAPIUtility::s_csStatus is held while this callback is issued.

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(kEE2PAllowableWhileDetaching,
        (LF_CORPROF,
         LL_INFO10,
         "**PROF: ProfilerDetachSucceeded.\n"));

    // Should only be called on profilers that support ICorProfilerCallback3
    _ASSERTE(m_pCallback3 != NULL);

    {
        // All callbacks are really NOTHROW, but that's enforced partially by the profiler,
        // whose try/catch blocks aren't visible to the contract system
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonProfilerCallout);
        return m_pCallback3->ProfilerDetachSucceeded();
    }
}



HRESULT EEToProfInterfaceImpl::GetAssemblyReferences(LPCWSTR wszAssemblyPath, IAssemblyBindingClosure * pClosure, AssemblyReferenceClosureWalkContextForProfAPI * pContext)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_TRIGGERS;

        // Yay!
        MODE_PREEMPTIVE;

        // Yay!
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: AssemblyReferenceClosureWalkStarted.  wszAssemblyPath: 0x%p.\n",
                                wszAssemblyPath
                                ));
    HRESULT hr = S_OK;


    return hr;
}

HRESULT EEToProfInterfaceImpl::EventPipeEventDelivered(
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
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT_EX(
        kEE2PNoTrigger,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeEventDelivered.\n"));

#ifdef FEATURE_PERFTRACING
    if (m_pCallback10 == NULL)
    {
        return S_OK;
    }

    EVENTPIPE_PROVIDER providerID = (EVENTPIPE_PROVIDER)provider;
    return m_pCallback10->EventPipeEventDelivered(providerID,
                                                  eventId,
                                                  eventVersion,
                                                  cbMetadataBlob,
                                                  metadataBlob,
                                                  cbEventData,
                                                  eventData,
                                                  pActivityId,
                                                  pRelatedActivityId,
                                                  reinterpret_cast<ThreadID>(pEventThread),
                                                  numStackFrames,
                                                  stackFrames);
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT EEToProfInterfaceImpl::EventPipeProviderCreated(EventPipeProvider *provider)
{
    CONTRACTL
    {
        NOTHROW;
        // The profiler will likely call back in to the other EventPipe apis,
        // some of which trigger
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CLR_TO_PROFILER_ENTRYPOINT((LF_CORPROF,
                                LL_INFO10,
                                "**PROF: EventPipeProviderCreated.\n"
                                ));

#ifdef FEATURE_PERFTRACING
    if (m_pCallback10 == NULL)
    {
        return S_OK;
    }

    EVENTPIPE_PROVIDER providerID = (EVENTPIPE_PROVIDER)provider;
    return m_pCallback10->EventPipeProviderCreated(providerID);
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT EEToProfInterfaceImpl::LoadAsNotficationOnly(BOOL *pbNotificationOnly)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This one API is special, we call in to the profiler before we've set up any of our 
    // machinery to do asserts (m_pProfilerInfo in specific). So we can't use 
    // CLR_TO_PROFILER_ENTRYPOINT here.

    LOG((LF_CORPROF,
        LL_INFO1000,
        "**PROF: LoadAsNotficationOnly.\n"));

    if (m_pCallback11 == NULL)
    {
        *pbNotificationOnly = FALSE;
        return S_OK;
    }

    return m_pCallback11->LoadAsNotficationOnly(pbNotificationOnly);
}

#endif // PROFILING_SUPPORTED
