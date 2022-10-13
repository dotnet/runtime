// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

///////////////////////////////////////////////////////////////////////////////
//
// File:
//     cs.cpp
//
// Purpose:
//     Implementation of critical sections
//
///////////////////////////////////////////////////////////////////////////////

#include "pal/thread.hpp"
#include "pal/cs.hpp"
#include "pal/malloc.hpp"
#include "pal/list.h"
#include "pal/dbgmsg.h"
#include "pal/init.h"
#include "pal/process.h"

#include <sched.h>
#include <pthread.h>

using namespace CorUnix;

//
// Uncomment the following line to turn CS behavior from
// unfair to fair lock
//
// #define PALCS_TRANSFER_OWNERSHIP_ON_RELEASE

//
// Uncomment the following line to enable simple mutex based CSs
// Note: when MUTEX_BASED_CSS is defined, PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
// has no effect
//
// #define MUTEX_BASED_CSS

//
// Important notes on critical sections layout/semantics on Unix
//
// 1) The PAL_CRITICAL_SECTION structure below must match the size of the
//    CRITICAL_SECTION defined in pal.h. Besides the "windows part"
//    of both the structures must be identical.
// 2) Both PAL_CRITICAL_SECTION and CRITICAL_SECTION currently do not match
//    the size of the Windows' CRITICAL_SECTION.
//    - From unmanaged code point of view, one should never make assumptions
//      on the size and layout of the CRITICAL_SECTION structure, and anyway
//      on Unix PAL's CRITICAL_SECTION extends the  Windows one, so that some
//      assumptions may still work.
//    - From managed code point of view, one could try to interop directly
//      to unmanaged critical sections APIs (though that would be quite
//      meaningless). In order to do that, one would need to define a copy
//      of the CRITICAL_SECTION structure in one's code, and that may lead
//      to access random data beyond the structure limit, if that managed
//      code is compiled on Unix.
//      In case such scenario should be supported, the  current implementation
//      will have to be modified in a way to go back to the original Windows
//      CRITICAL_SECTION layout. That would require to dynamically allocate
//      the native data and use LockSemaphore as a pointer to it. The current
//      solution intentionally avoids that since an effort has been made to
//      make CSs objects completely independent from any other PAL subsystem,
//      so that they can be used during initialization and shutdown.
//      In case the "dynamically allocate native data" solution should be
//      implemented, CSs would acquire a dependency on memory allocation and
//      thread suspension subsystems, since the first contention on a specific
//      CS would trigger the native data allocation.
// 3) The semantics of the LockCount field has not been kept compatible with
//    the Windows implementation.
//    Both on Windows and Unix the lower bit of LockCount indicates
//    whether or not the CS is locked (for both fair and unfair lock
//    solution), the second bit indicates whether or not currently there is a
//    waiter that has been awakened and that is trying to acquire the CS
//    (only unfair lock solution, unused in the fair one); starting from the
//    third bit, LockCount represents the number of waiter threads currently
//    waiting on the CS.
//    Windows, anyway, implements this semantics in negative logic, so that
//    an unlocked CS is represented by a LockCount == -1 (i.e. 0xFFFFFFFF,
//    all the bits set), while on Unix an unlocked CS has LockCount == 0.
//    Windows needs to use negative logic to support legacy code bad enough
//    to directly access CS's fields making the assumption that
//    LockCount == -1 means CS unlocked. Unix will not support that, and
//    it uses positive logic.
// 4) The CRITICAL_SECTION_DEBUG_INFO layout on Unix is intentionally not
//    compatible with the Windows layout.
// 5) For legacy code dependencies issues similar to those just described for
//    the LockCount field, Windows CS code maintains a per-process list of
//    debug info for all the CSs, both on debug and free/retail builds. On
//    Unix such a list is maintained only on debug builds, and no debug
//    info structure is allocated on free/retail builds
//

SET_DEFAULT_DEBUG_CHANNEL(CRITSEC);

#ifdef TRACE_CS_LOGIC
#define CS_TRACE TRACE
#else
#ifdef __GNUC__
#define CS_TRACE(args...)
#else
#define CS_TRACE(...)
#endif
#endif // TRACE_CS_LOGIC

//
// Note: PALCS_LOCK_WAITER_INC must be 2 *  PALCS_LOCK_AWAKENED_WAITER
//
#define PALCS_LOCK_INIT                0
#define PALCS_LOCK_BIT                 1
#define PALCS_LOCK_AWAKENED_WAITER     2
#define PALCS_LOCK_WAITER_INC          4

#define PALCS_GETLBIT(val)      ((int)(0!=(PALCS_LOCK_BIT&val)))
#define PALCS_GETAWBIT(val)     ((int)(0!=(PALCS_LOCK_AWAKENED_WAITER&val)))
#define PALCS_GETWCOUNT(val)    (val/PALCS_LOCK_WAITER_INC)

enum PalCsInitState
{
    PalCsNotInitialized,    // Critical section not initialized (InitializedCriticalSection
                            // has not yet been called, or DeleteCriticalsection has been
                            // called).
    PalCsUserInitialized,   // Critical section initialized from the user point of view,
                            // i.e. InitializedCriticalSection has been called.
    PalCsFullyInitializing, // A thread found the CS locked, this is the first contention on
                            // this CS, and the thread is initializing the CS's native data.
    PalCsFullyInitialized   // Internal CS's native data has been fully initialized.
};

enum PalCsWaiterReturnState
{
    PalCsReturnWaiterAwakened,
    PalCsWaiterDidntWait
};

struct _PAL_CRITICAL_SECTION; // fwd declaration

typedef struct _CRITICAL_SECTION_DEBUG_INFO
{
    LIST_ENTRY Link;
    struct _PAL_CRITICAL_SECTION * pOwnerCS;
    Volatile<ULONG> lAcquireCount;
    Volatile<ULONG> lEnterCount;
    Volatile<LONG> lContentionCount;
} CRITICAL_SECTION_DEBUG_INFO, *PCRITICAL_SECTION_DEBUG_INFO;

typedef struct _PAL_CRITICAL_SECTION_NATIVE_DATA
{
    pthread_mutex_t mutex;
    pthread_cond_t condition;
    int iPredicate;
} PAL_CRITICAL_SECTION_NATIVE_DATA, *PPAL_CRITICAL_SECTION_NATIVE_DATA;

typedef struct _PAL_CRITICAL_SECTION {
    // Windows part
    PCRITICAL_SECTION_DEBUG_INFO DebugInfo;
    Volatile<LONG> LockCount;
    LONG RecursionCount;
    SIZE_T OwningThread;
    ULONG_PTR SpinCount;
    // Private Unix part
#ifdef PAL_TRACK_CRITICAL_SECTIONS_DATA
    BOOL fInternal;
#endif // PAL_TRACK_CRITICAL_SECTIONS_DATA
    Volatile<PalCsInitState> cisInitState;
    PAL_CRITICAL_SECTION_NATIVE_DATA csndNativeData;
} PAL_CRITICAL_SECTION, *PPAL_CRITICAL_SECTION, *LPPAL_CRITICAL_SECTION;

#ifdef _DEBUG
namespace CorUnix
{
    PAL_CRITICAL_SECTION g_csPALCSsListLock;
    LIST_ENTRY g_PALCSList = { &g_PALCSList, &g_PALCSList};
}
#endif // _DEBUG

#define ObtainCurrentThreadId(thread) ObtainCurrentThreadIdImpl(thread, __func__)
static SIZE_T ObtainCurrentThreadIdImpl(CPalThread *pCurrentThread, const char *callingFuncName)
{
    SIZE_T threadId;
    if(pCurrentThread)
    {
        threadId = pCurrentThread->GetThreadId();
        _ASSERTE(threadId == THREADSilentGetCurrentThreadId());
    }
    else
    {
        threadId = THREADSilentGetCurrentThreadId();
        CS_TRACE("Early %s, no pthread data, getting TID internally\n", callingFuncName);
    }
    _ASSERTE(0 != threadId);

    return threadId;
}


/*++
Function:
  InitializeCriticalSection

See MSDN doc.
--*/
void InitializeCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    PERF_ENTRY(InitializeCriticalSection);
    ENTRY("InitializeCriticalSection(lpCriticalSection=%p)\n",
          lpCriticalSection);

    InternalInitializeCriticalSectionAndSpinCount(lpCriticalSection,
                                                  0, false);

    LOGEXIT("InitializeCriticalSection returns void\n");
    PERF_EXIT(InitializeCriticalSection);
}

/*++
Function:
  InitializeCriticalSectionAndSpinCount

See MSDN doc.
--*/
BOOL InitializeCriticalSectionAndSpinCount(LPCRITICAL_SECTION lpCriticalSection,
                                           DWORD dwSpinCount)
{
    BOOL bRet = TRUE;
    PERF_ENTRY(InitializeCriticalSectionAndSpinCount);
    ENTRY("InitializeCriticalSectionAndSpinCount(lpCriticalSection=%p, "
          "dwSpinCount=%u)\n", lpCriticalSection, dwSpinCount);

    InternalInitializeCriticalSectionAndSpinCount(lpCriticalSection,
                                                  dwSpinCount, false);

    LOGEXIT("InitializeCriticalSectionAndSpinCount returns BOOL %d\n",
            bRet);
    PERF_EXIT(InitializeCriticalSectionAndSpinCount);
    return bRet;
}

/*++
Function:
  DeleteCriticalSection

See MSDN doc.
--*/
void DeleteCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    PERF_ENTRY(DeleteCriticalSection);
    ENTRY("DeleteCriticalSection(lpCriticalSection=%p)\n", lpCriticalSection);

    InternalDeleteCriticalSection(lpCriticalSection);

    LOGEXIT("DeleteCriticalSection returns void\n");
    PERF_EXIT(DeleteCriticalSection);
}

/*++
Function:
  EnterCriticalSection

See MSDN doc.
--*/
void EnterCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    PERF_ENTRY(EnterCriticalSection);
    ENTRY("EnterCriticalSection(lpCriticalSection=%p)\n", lpCriticalSection);

    CPalThread * pThread = InternalGetCurrentThread();

    InternalEnterCriticalSection(pThread, lpCriticalSection);

    LOGEXIT("EnterCriticalSection returns void\n");
    PERF_EXIT(EnterCriticalSection);
}

/*++
Function:
  LeaveCriticalSection

See MSDN doc.
--*/
VOID LeaveCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    PERF_ENTRY(LeaveCriticalSection);
    ENTRY("LeaveCriticalSection(lpCriticalSection=%p)\n", lpCriticalSection);

    CPalThread * pThread = InternalGetCurrentThread();

    InternalLeaveCriticalSection(pThread, lpCriticalSection);

    LOGEXIT("LeaveCriticalSection returns void\n");
    PERF_EXIT(LeaveCriticalSection);
}

/*++
Function:
  InternalInitializeCriticalSection

Initializes a critical section. It assumes the CS is an internal one,
i.e. thread entering it will be marked unsafe for suspension
--*/
VOID InternalInitializeCriticalSection(CRITICAL_SECTION *pcs)
{
    InternalInitializeCriticalSectionAndSpinCount(pcs, 0, true);
}

/*++
Function:
  InternalDeleteCriticalSection

Deletes a critical section
--*/
VOID InternalDeleteCriticalSection(
    PCRITICAL_SECTION pCriticalSection)
{
    PAL_CRITICAL_SECTION * pPalCriticalSection =
        reinterpret_cast<PAL_CRITICAL_SECTION*>(pCriticalSection);

    _ASSERT_MSG(PalCsUserInitialized == pPalCriticalSection->cisInitState ||
                PalCsFullyInitialized == pPalCriticalSection->cisInitState,
                "CS %p is not initialized", pPalCriticalSection);

#ifdef _DEBUG
    CPalThread * pThread =
        (PALIsThreadDataInitialized() ? GetCurrentPalThread() : NULL);

    if (0 != pPalCriticalSection->LockCount)
    {
        SIZE_T tid;
        tid = ObtainCurrentThreadId(pThread);
        int iWaiterCount = (int)PALCS_GETWCOUNT(pPalCriticalSection->LockCount);

        if (0 != (PALCS_LOCK_BIT & pPalCriticalSection->LockCount))
        {
            // CS is locked
            if (tid != pPalCriticalSection->OwningThread)
            {
                // not owner
                ASSERT("Thread tid=%u deleting a CS owned by thread tid=%u\n",
                       tid, pPalCriticalSection->OwningThread);
            }
            else
            {
                // owner
                if (0 != iWaiterCount)
                {
                    ERROR("Thread tid=%u is deleting a CS with %d threads waiting on it\n",
                          tid, iWaiterCount);
                }
                else
                {
                    WARN("Thread tid=%u is deleting a critical section it still owns\n",
                         tid);
                }
            }
        }
        else
        {
            // CS is not locked
            if (0 != iWaiterCount)
            {
                ERROR("Deleting a CS with %d threads waiting on it\n",
                      iWaiterCount);
            }
            else
            {
                ERROR("Thread tid=%u is deleting a critical section currently not "
                      "owned, but with one waiter awakened\n", tid);
            }
        }
    }

    if (NULL != pPalCriticalSection->DebugInfo)
    {
        if (pPalCriticalSection != &CorUnix::g_csPALCSsListLock)
        {
            InternalEnterCriticalSection(pThread,
                reinterpret_cast<CRITICAL_SECTION*>(&g_csPALCSsListLock));
            RemoveEntryList(&pPalCriticalSection->DebugInfo->Link);
            InternalLeaveCriticalSection(pThread,
                reinterpret_cast<CRITICAL_SECTION*>(&g_csPALCSsListLock));
        }
        else
        {
            RemoveEntryList(&pPalCriticalSection->DebugInfo->Link);
        }

#ifdef PAL_TRACK_CRITICAL_SECTIONS_DATA
        LONG lVal, lNewVal;
        Volatile<LONG> * plDest;

        // Update delete count
        InterlockedIncrement(pPalCriticalSection->fInternal ?
            &g_lPALCSInternalDeleteCount : &g_lPALCSDeleteCount);

        // Update acquire count
        plDest = pPalCriticalSection->fInternal ?
            &g_lPALCSInternalAcquireCount : &g_lPALCSAcquireCount;
        do {
            lVal = *plDest;
            lNewVal = lVal + pPalCriticalSection->DebugInfo->lAcquireCount;
            lNewVal = InterlockedCompareExchange(plDest, lNewVal, lVal);
        } while (lVal != lNewVal);

        // Update enter count
        plDest = pPalCriticalSection->fInternal ?
            &g_lPALCSInternalEnterCount : &g_lPALCSEnterCount;
        do {
            lVal = *plDest;
            lNewVal = lVal + pPalCriticalSection->DebugInfo->lEnterCount;
            lNewVal = InterlockedCompareExchange(plDest, lNewVal, lVal);
        } while (lVal != lNewVal);

        // Update contention count
        plDest = pPalCriticalSection->fInternal ?
            &g_lPALCSInternalContentionCount : &g_lPALCSContentionCount;
        do {
            lVal = *plDest;
            lNewVal = lVal + pPalCriticalSection->DebugInfo->lContentionCount;
            lNewVal = InterlockedCompareExchange(plDest, lNewVal, lVal);
        } while (lVal != lNewVal);

#endif // PAL_TRACK_CRITICAL_SECTIONS_DATA

        InternalDelete(pPalCriticalSection->DebugInfo);
        pPalCriticalSection->DebugInfo = NULL;
    }
#endif // _DEBUG

    if (PalCsFullyInitialized == pPalCriticalSection->cisInitState)
    {
        int iRet;

        // destroy condition
        iRet = pthread_cond_destroy(&pPalCriticalSection->csndNativeData.condition);
        _ASSERT_MSG(0 == iRet, "Failed destroying condition in CS @ %p "
                    "[err=%d]\n", pPalCriticalSection, iRet);

        // destroy mutex
        iRet = pthread_mutex_destroy(&pPalCriticalSection->csndNativeData.mutex);
        _ASSERT_MSG(0 == iRet, "Failed destroying mutex in CS @ %p "
                    "[err=%d]\n", pPalCriticalSection, iRet);
    }

    // Reset critical section state
    pPalCriticalSection->cisInitState = PalCsNotInitialized;
}

// The following PALCEnterCriticalSection and PALCLeaveCriticalSection
// functions are intended to provide CorUnix's InternalEnterCriticalSection
// and InternalLeaveCriticalSection functionalities to legacy C code,
// which has no knowledge of CPalThread, classes and namespaces.

/*++
Function:
  PALCEnterCriticalSection

Provides CorUnix's InternalEnterCriticalSection functionality to legacy C code,
which has no knowledge of CPalThread, classes and namespaces.
--*/
VOID PALCEnterCriticalSection(CRITICAL_SECTION * pcs)
{
    CPalThread * pThread =
        (PALIsThreadDataInitialized() ? GetCurrentPalThread() : NULL);
    CorUnix::InternalEnterCriticalSection(pThread, pcs);
}

/*++
Function:
  PALCLeaveCriticalSection

Provides CorUnix's InternalLeaveCriticalSection functionality to legacy C code,
which has no knowledge of CPalThread, classes and namespaces.
--*/
VOID PALCLeaveCriticalSection(CRITICAL_SECTION * pcs)
{
    CPalThread * pThread =
        (PALIsThreadDataInitialized() ? GetCurrentPalThread() : NULL);
    CorUnix::InternalLeaveCriticalSection(pThread, pcs);
}

namespace CorUnix
{
    static PalCsWaiterReturnState PALCS_WaitOnCS(
        PAL_CRITICAL_SECTION * pPalCriticalSection,
        LONG lInc);
    static PAL_ERROR PALCS_DoActualWait(PAL_CRITICAL_SECTION * pPalCriticalSection);
    static PAL_ERROR PALCS_WakeUpWaiter(PAL_CRITICAL_SECTION * pPalCriticalSection);
    static bool PALCS_FullyInitialize(PAL_CRITICAL_SECTION * pPalCriticalSection);

#ifdef _DEBUG
    enum CSSubSysInitState
    {
        CSSubSysNotInitialized,
        CSSubSysInitializing,
        CSSubSysInitialized
    };
    static Volatile<CSSubSysInitState> csssInitState = CSSubSysNotInitialized;

#ifdef PAL_TRACK_CRITICAL_SECTIONS_DATA
    static Volatile<LONG> g_lPALCSInitializeCount         = 0;
    static Volatile<LONG> g_lPALCSDeleteCount             = 0;
    static Volatile<LONG> g_lPALCSAcquireCount            = 0;
    static Volatile<LONG> g_lPALCSEnterCount              = 0;
    static Volatile<LONG> g_lPALCSContentionCount         = 0;
    static Volatile<LONG> g_lPALCSInternalInitializeCount = 0;
    static Volatile<LONG> g_lPALCSInternalDeleteCount     = 0;
    static Volatile<LONG> g_lPALCSInternalAcquireCount    = 0;
    static Volatile<LONG> g_lPALCSInternalEnterCount      = 0;
    static Volatile<LONG> g_lPALCSInternalContentionCount = 0;
#endif // PAL_TRACK_CRITICAL_SECTIONS_DATA
#endif // _DEBUG


    /*++
    Function:
      CorUnix::CriticalSectionSubSysInitialize

    Initializes CS subsystem
    --*/
    void CriticalSectionSubSysInitialize()
    {
        static_assert(sizeof(CRITICAL_SECTION) >= sizeof(PAL_CRITICAL_SECTION),
            "PAL fatal internal error: sizeof(CRITICAL_SECTION) is "
            "smaller than sizeof(PAL_CRITICAL_SECTION)");

#ifdef _DEBUG
        LONG lRet = InterlockedCompareExchange((LONG *)&csssInitState,
                                               (LONG)CSSubSysInitializing,
                                               (LONG)CSSubSysNotInitialized);
        if ((LONG)CSSubSysNotInitialized == lRet)
        {
            InitializeListHead(&g_PALCSList);

            InternalInitializeCriticalSectionAndSpinCount(
                reinterpret_cast<CRITICAL_SECTION*>(&g_csPALCSsListLock),
                0, true);
            InterlockedExchange((LONG *)&csssInitState,
                                (LONG)CSSubSysInitialized);
        }
        else
        {
            while (csssInitState != CSSubSysInitialized)
            {
                sched_yield();
            }
        }
#endif // _DEBUG
    }

    /*++
    Function:
      CorUnix::InternalInitializeCriticalSectionAndSpinCount

    Initializes a CS with the given spin count. If 'fInternal' is true
    the CS will be treatead as an internal one for its whole lifetime,
    i.e. any thread that will enter it will be marked as unsafe for
    suspension as long as it holds the CS
    --*/
    void InternalInitializeCriticalSectionAndSpinCount(
        PCRITICAL_SECTION pCriticalSection,
        DWORD dwSpinCount,
        bool fInternal)
    {
        PAL_CRITICAL_SECTION * pPalCriticalSection =
            reinterpret_cast<PAL_CRITICAL_SECTION*>(pCriticalSection);

#ifndef PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
        // Make sure bits are defined in a usable way
        _ASSERTE(PALCS_LOCK_AWAKENED_WAITER * 2 == PALCS_LOCK_WAITER_INC);
#endif // !PALCS_TRANSFER_OWNERSHIP_ON_RELEASE

        // Make sure structure sizes are compatible
        _ASSERTE(sizeof(CRITICAL_SECTION) >= sizeof(PAL_CRITICAL_SECTION));

#ifdef _DEBUG
        if (sizeof(CRITICAL_SECTION) > sizeof(PAL_CRITICAL_SECTION))
        {
            WARN("PAL_CS_NATIVE_DATA_SIZE appears to be defined to a value (%d) "
                 "larger than needed on this platform (%d).\n",
                 sizeof(CRITICAL_SECTION), sizeof(PAL_CRITICAL_SECTION));
        }
#endif // _DEBUG

        // Init CS data
        pPalCriticalSection->DebugInfo         = NULL;
        pPalCriticalSection->LockCount         = 0;
        pPalCriticalSection->RecursionCount    = 0;
        pPalCriticalSection->SpinCount         = dwSpinCount;
        pPalCriticalSection->OwningThread      = 0;

#ifdef _DEBUG
        CPalThread * pThread =
            (PALIsThreadDataInitialized() ? GetCurrentPalThread() : NULL);

        pPalCriticalSection->DebugInfo = InternalNew<CRITICAL_SECTION_DEBUG_INFO>();
        _ASSERT_MSG(NULL != pPalCriticalSection->DebugInfo,
                    "Failed to allocate debug info for new CS\n");

        // Init debug info data
        pPalCriticalSection->DebugInfo->lAcquireCount    = 0;
        pPalCriticalSection->DebugInfo->lEnterCount      = 0;
        pPalCriticalSection->DebugInfo->lContentionCount = 0;
        pPalCriticalSection->DebugInfo->pOwnerCS         = pPalCriticalSection;

        // Insert debug info struct in global list
        if (pPalCriticalSection != &g_csPALCSsListLock)
        {
            InternalEnterCriticalSection(pThread,
                reinterpret_cast<CRITICAL_SECTION*>(&g_csPALCSsListLock));
            InsertTailList(&g_PALCSList, &pPalCriticalSection->DebugInfo->Link);
            InternalLeaveCriticalSection(pThread,
                reinterpret_cast<CRITICAL_SECTION*>(&g_csPALCSsListLock));
        }
        else
        {
            InsertTailList(&g_PALCSList, &pPalCriticalSection->DebugInfo->Link);
        }

#ifdef PAL_TRACK_CRITICAL_SECTIONS_DATA
        pPalCriticalSection->fInternal         = fInternal;
        InterlockedIncrement(fInternal ?
            &g_lPALCSInternalInitializeCount : &g_lPALCSInitializeCount);
#endif // PAL_TRACK_CRITICAL_SECTIONS_DATA
#endif // _DEBUG

        // Set initializazion state
        pPalCriticalSection->cisInitState = PalCsUserInitialized;

#ifdef MUTEX_BASED_CSS
        bool fInit;
        do
        {
            fInit = PALCS_FullyInitialize(pPalCriticalSection);
            _ASSERTE(fInit);
        } while (!fInit && 0 == sched_yield());

        if (fInit)
        {
            // Set initializazion state
            pPalCriticalSection->cisInitState = PalCsFullyInitialized;
        }
#endif // MUTEX_BASED_CSS
    }

#ifndef MUTEX_BASED_CSS
    /*++
    Function:
      CorUnix::InternalEnterCriticalSection

    Enters a CS, causing the thread to block if the CS is owned by
    another thread
    --*/
    void InternalEnterCriticalSection(
        CPalThread * pThread,
        PCRITICAL_SECTION pCriticalSection)
    {
        PAL_CRITICAL_SECTION * pPalCriticalSection =
            reinterpret_cast<PAL_CRITICAL_SECTION*>(pCriticalSection);

        LONG lSpinCount;
        LONG lVal, lNewVal;
        LONG lBitsToChange, lWaitInc;
        PalCsWaiterReturnState cwrs;
        SIZE_T threadId;

        _ASSERTE(PalCsNotInitialized != pPalCriticalSection->cisInitState);

        threadId = ObtainCurrentThreadId(pThread);


        // Check if the current thread already owns the CS
        //
        // Note: there is no need for this double check to be atomic. In fact
        // if the first check fails, the second doesn't count (and it's not
        // even executed). If the first one succeeds and the second one
        // doesn't, it doesn't matter if LockCount has already changed by the
        // time OwningThread is tested. Instead, if the first one succeeded,
        // and the second also succeeds, LockCount cannot have changed in the
        // meanwhile, since this is the owning thread and only the owning
        // thread can change the lock bit when the CS is owned.
        if ((pPalCriticalSection->LockCount & PALCS_LOCK_BIT) &&
            (pPalCriticalSection->OwningThread == threadId))
        {
            pPalCriticalSection->RecursionCount += 1;
#ifdef _DEBUG
            if (NULL != pPalCriticalSection->DebugInfo)
            {
                pPalCriticalSection->DebugInfo->lEnterCount += 1;
            }
#endif // _DEBUG
            goto IECS_exit;
        }

        // Set bits to change and waiter increment for an incoming thread
        lBitsToChange = PALCS_LOCK_BIT;
        lWaitInc = PALCS_LOCK_WAITER_INC;
        lSpinCount = pPalCriticalSection->SpinCount;

        while (TRUE)
        {
            // Either this is an incoming thread, and therefore lBitsToChange
            // is just PALCS_LOCK_BIT, or this is an awakened waiter
            _ASSERTE(PALCS_LOCK_BIT == lBitsToChange ||
                     (PALCS_LOCK_BIT | PALCS_LOCK_AWAKENED_WAITER) == lBitsToChange);

            // Make sure the waiter increment is in a valid range
            _ASSERTE(PALCS_LOCK_WAITER_INC == lWaitInc ||
                     PALCS_LOCK_AWAKENED_WAITER == lWaitInc);

            do {
                lVal = pPalCriticalSection->LockCount;

                while (0 == (lVal & PALCS_LOCK_BIT))
                {
                    // CS is not locked: try lo lock it

                    // Make sure that whether we are an incoming thread
                    // or the PALCS_LOCK_AWAKENED_WAITER bit is set
                    _ASSERTE((PALCS_LOCK_BIT == lBitsToChange) ||
                             (PALCS_LOCK_AWAKENED_WAITER & lVal));

                    lNewVal = lVal ^ lBitsToChange;

                    // Make sure we are actually trying to lock
                    _ASSERTE(lNewVal & PALCS_LOCK_BIT);

                    CS_TRACE("[ECS %p] Switching from {%d, %d, %d} to "
                        "{%d, %d, %d} ==>\n", pPalCriticalSection,
                        PALCS_GETWCOUNT(lVal), PALCS_GETAWBIT(lVal), PALCS_GETLBIT(lVal),
                        PALCS_GETWCOUNT(lNewVal), PALCS_GETAWBIT(lNewVal), PALCS_GETLBIT(lNewVal));

                    // Try to switch the value
                    lNewVal = InterlockedCompareExchange (&pPalCriticalSection->LockCount,
                                                         lNewVal, lVal);

                    CS_TRACE("[ECS %p] ==> %s LockCount={%d, %d, %d} "
                        "lVal={%d, %d, %d}\n", pPalCriticalSection,
                        (lNewVal == lVal) ? "OK" : "NO",
                        PALCS_GETWCOUNT(pPalCriticalSection->LockCount),
                        PALCS_GETAWBIT(pPalCriticalSection->LockCount),
                        PALCS_GETLBIT(pPalCriticalSection->LockCount),
                        PALCS_GETWCOUNT(lVal), PALCS_GETAWBIT(lVal), PALCS_GETLBIT(lVal));

                    if (lNewVal == lVal)
                    {
                        // CS successfully acquired
                        goto IECS_set_ownership;
                    }

                    // Acquisition failed, some thread raced with us;
                    // update value for next loop
                    lVal = lNewVal;
                }

                if (0 < lSpinCount)
                {
                    sched_yield();
                }
            } while (0 <= --lSpinCount);

            cwrs = PALCS_WaitOnCS(pPalCriticalSection, lWaitInc);

            if (PalCsReturnWaiterAwakened == cwrs)
            {
#ifdef PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                //
                // Fair Critical Sections
                //
                // In the fair lock case, when a waiter wakes up the CS
                // must be locked (i.e. ownership passed on to the waiter)
                _ASSERTE(0 != (PALCS_LOCK_BIT & pPalCriticalSection->LockCount));

                // CS successfully acquired
                goto IECS_set_ownership;

#else // PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                //
                // Unfair Critical Sections
                //
                _ASSERTE(PALCS_LOCK_AWAKENED_WAITER & pPalCriticalSection->LockCount);

                lBitsToChange = PALCS_LOCK_BIT | PALCS_LOCK_AWAKENED_WAITER;
                lWaitInc = PALCS_LOCK_AWAKENED_WAITER;
#endif // PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
            }
        }

    IECS_set_ownership:
        // Critical section acquired: set ownership data
        pPalCriticalSection->OwningThread = threadId;
        pPalCriticalSection->RecursionCount = 1;
#ifdef _DEBUG
        if (NULL != pPalCriticalSection->DebugInfo)
        {
            pPalCriticalSection->DebugInfo->lAcquireCount += 1;
            pPalCriticalSection->DebugInfo->lEnterCount += 1;
        }
#endif // _DEBUG

    IECS_exit:
        return;
    }

    /*++
    Function:
      CorUnix::InternalLeaveCriticalSection

    Leaves a currently owned CS
    --*/
    void InternalLeaveCriticalSection(CPalThread * pThread,
                                      PCRITICAL_SECTION pCriticalSection)
    {
        PAL_CRITICAL_SECTION * pPalCriticalSection =
            reinterpret_cast<PAL_CRITICAL_SECTION*>(pCriticalSection);
        LONG lVal, lNewVal;

#ifdef _DEBUG
        SIZE_T threadId;

        _ASSERTE(PalCsNotInitialized != pPalCriticalSection->cisInitState);

        threadId = ObtainCurrentThreadId(pThread);
        _ASSERTE(threadId == pPalCriticalSection->OwningThread);
#endif // _DEBUG

        _ASSERT_MSG(PALCS_LOCK_BIT & pPalCriticalSection->LockCount,
                    "Trying to release an unlocked CS\n");
        _ASSERT_MSG(0 < pPalCriticalSection->RecursionCount,
                    "Trying to release an unlocked CS\n");

        if (--pPalCriticalSection->RecursionCount > 0)
        {
            // Recursion was > 1, still owning the CS
            goto ILCS_cs_exit;
        }

        // Reset CS ownership
        pPalCriticalSection->OwningThread = 0;

        // Load the current LockCount value
        lVal = pPalCriticalSection->LockCount;

        while (true)
        {
            _ASSERT_MSG(0 != (PALCS_LOCK_BIT & lVal),
                      "Trying to release an unlocked CS\n");

            // NB: In the fair lock case (PALCS_TRANSFER_OWNERSHIP_ON_RELEASE) the
            // PALCS_LOCK_AWAKENED_WAITER bit is not used
            if ( (PALCS_LOCK_BIT == lVal)
#ifndef PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                 || (PALCS_LOCK_AWAKENED_WAITER & lVal)
#endif // !PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                )
            {
                // Whether there are no waiters (PALCS_LOCK_BIT == lVal)
                // or a waiter has already been awakened, therefore we
                // just need to reset the lock bit and return
                lNewVal = lVal & ~PALCS_LOCK_BIT;
                CS_TRACE("[LCS-UN %p] Switching from {%d, %d, %d} to "
                    "{%d, %d, %d} ==>\n", pPalCriticalSection,
                    PALCS_GETWCOUNT(lVal), PALCS_GETAWBIT(lVal), PALCS_GETLBIT(lVal),
                    PALCS_GETWCOUNT(lNewVal), PALCS_GETAWBIT(lNewVal), PALCS_GETLBIT(lNewVal));

                lNewVal = InterlockedCompareExchange(&pPalCriticalSection->LockCount,
                                                     lNewVal, lVal);

                CS_TRACE("[LCS-UN %p] ==> %s\n", pPalCriticalSection,
                               (lNewVal == lVal) ? "OK" : "NO");

                if (lNewVal == lVal)
                {
                    goto ILCS_cs_exit;
                }
            }
            else
            {
                // There is at least one waiter, we need to wake it up

#ifdef PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                // Fair lock case: passing ownership on to the first waiter.
                // Here we need only to decrement the waiters count. CS will
                // remain locked and ownership will be passed to the waiter,
                // which will take care of setting ownership data as soon as
                // it wakes up
                lNewVal = lVal - PALCS_LOCK_WAITER_INC;
#else // PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                // Unfair lock case: we need to atomically decrement the waiters
                // count (we are about ot wake up one of them), set the
                // "waiter awakened" bit and to reset the "CS locked" bit.
                // Note that, since we know that at this time PALCS_LOCK_BIT
                // is set and PALCS_LOCK_AWAKENED_WAITER is not set, none of
                // the addenda will affect bits other than its target bit(s),
                // i.e. PALCS_LOCK_BIT will not affect PALCS_LOCK_AWAKENED_WAITER,
                // PALCS_LOCK_AWAKENED_WAITER will not affect the actual
                // count of waiters, and the latter will not change the two
                // former ones
                lNewVal = lVal - PALCS_LOCK_WAITER_INC +
                    PALCS_LOCK_AWAKENED_WAITER - PALCS_LOCK_BIT;
#endif // PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                CS_TRACE("[LCS-CN %p] Switching from {%d, %d, %d} to {%d, %d, %d} ==>\n",
                    pPalCriticalSection,
                    PALCS_GETWCOUNT(lVal), PALCS_GETAWBIT(lVal), PALCS_GETLBIT(lVal),
                    PALCS_GETWCOUNT(lNewVal), PALCS_GETAWBIT(lNewVal), PALCS_GETLBIT(lNewVal));

                lNewVal = InterlockedCompareExchange(&pPalCriticalSection->LockCount,
                                                     lNewVal, lVal);

                CS_TRACE("[LCS-CN %p] ==> %s\n", pPalCriticalSection,
                            (lNewVal == lVal) ? "OK" : "NO");

                if (lNewVal == lVal)
                {
                    // Wake up the waiter
                    PALCS_WakeUpWaiter (pPalCriticalSection);

#ifdef PALCS_TRANSFER_OWNERSHIP_ON_RELEASE
                    // In the fair lock case, we need to yield here to defeat
                    // the inherently unfair nature of the condition/predicate
                    // construct
                    sched_yield();
#endif // PALCS_TRANSFER_OWNERSHIP_ON_RELEASE

                    goto ILCS_cs_exit;
                }
            }

            // CS unlock failed due to race with another thread trying to
            // register as waiter on it. We need to keep on looping. We
            // intentionally do not yield here in order to reserve higher
            // priority for the releasing thread.
            //
            // At this point lNewVal contains the latest LockCount value
            // retrieved by one of the two InterlockedCompareExchange above;
            // we can use this value as expected LockCount for the next loop,
            // without the need to fetch it again.
            lVal = lNewVal;
        }

    ILCS_cs_exit:
        return;
    }

#endif // MUTEX_BASED_CSS

    /*++
    Function:
      CorUnix::PALCS_FullyInitialize

    Fully initializes a CS which was previously initialized in InitializeCriticalSection.
    This method is called at the first contention on the target CS
    --*/
    bool PALCS_FullyInitialize(PAL_CRITICAL_SECTION * pPalCriticalSection)
    {
        LONG lVal, lNewVal;
        bool fRet = true;

        lVal = pPalCriticalSection->cisInitState;
        if (PalCsFullyInitialized == lVal)
        {
            goto PCDI_exit;
        }
        if (PalCsUserInitialized == lVal)
        {
            int iRet;
            lNewVal = (LONG)PalCsFullyInitializing;
            lNewVal = InterlockedCompareExchange(
                (LONG *)&pPalCriticalSection->cisInitState, lNewVal, lVal);
            if (lNewVal != lVal)
            {
                if (PalCsFullyInitialized == lNewVal)
                {
                    // Another thread did initialize this CS: we can
                    // safely return 'true'
                    goto PCDI_exit;
                }

                // Another thread is still initializing this CS: yield and
                // spin by returning 'false'
                sched_yield();
                fRet = false;
                goto PCDI_exit;
            }

            //
            // Actual native initialization
            //
            // Mutex
            iRet = pthread_mutex_init(&pPalCriticalSection->csndNativeData.mutex, NULL);
            if (0 != iRet)
            {
                ASSERT("Failed initializing mutex in CS @ %p [err=%d]\n",
                        pPalCriticalSection, iRet);
                pPalCriticalSection->cisInitState = PalCsUserInitialized;
                fRet = false;
                goto PCDI_exit;
            }
#ifndef MUTEX_BASED_CSS
            // Condition
            iRet = pthread_cond_init(&pPalCriticalSection->csndNativeData.condition, NULL);
            if (0 != iRet)
            {
                ASSERT("Failed initializing condition in CS @ %p [err=%d]\n",
                       pPalCriticalSection, iRet);
                pthread_mutex_destroy(&pPalCriticalSection->csndNativeData.mutex);
                pPalCriticalSection->cisInitState = PalCsUserInitialized;
                fRet = false;
                goto PCDI_exit;
            }
            // Predicate
            pPalCriticalSection->csndNativeData.iPredicate = 0;
#endif

            pPalCriticalSection->cisInitState = PalCsFullyInitialized;
        }
        else if (PalCsFullyInitializing == lVal)
        {
            // Another thread is still initializing this CS: yield and
            // spin by returning 'false'
            sched_yield();
            fRet = false;
            goto PCDI_exit;
        }
        else
        {
            ASSERT("CS %p is not initialized", pPalCriticalSection);
            fRet = false;
            goto PCDI_exit;
        }

    PCDI_exit:
        return fRet;
    }


    /*++
    Function:
      CorUnix::PALCS_WaitOnCS

    Waits on a CS owned by another thread. It returns PalCsReturnWaiterAwakened
    if the thread actually waited on the CS and it has been awakened on CS
    release. It returns PalCsWaiterDidntWait if another thread is currently
    fully-initializing the CS and therefore the current thread couldn't wait
    on it
    --*/
    PalCsWaiterReturnState PALCS_WaitOnCS(PAL_CRITICAL_SECTION * pPalCriticalSection,
                                          LONG lInc)
    {
        DWORD lVal, lNewVal;
        PAL_ERROR palErr = NO_ERROR;

        if (PalCsFullyInitialized != pPalCriticalSection->cisInitState)
        {
            // First contention, the CS native wait support need to be
            // initialized at this time
            if (!PALCS_FullyInitialize(pPalCriticalSection))
            {
                // The current thread failed the full initialization of the CS,
                // whether because another thread is race-initializing it, or
                // there are no enough memory/resources at this time, or
                // InitializeCriticalSection has never been called. By
                // returning we will cause the thread to spin on CS trying
                // again until the CS is initialized
                return PalCsWaiterDidntWait;
            }
        }

        // Make sure we have a valid waiter increment
        _ASSERTE(PALCS_LOCK_WAITER_INC == lInc ||
                 PALCS_LOCK_AWAKENED_WAITER == lInc);

        do {
            lVal = pPalCriticalSection->LockCount;

            // Make sure the waiter increment is compatible with the
            // awakened waiter bit value
            _ASSERTE(PALCS_LOCK_WAITER_INC == lInc ||
                     PALCS_LOCK_AWAKENED_WAITER & lVal);

            if (0 == (lVal & PALCS_LOCK_BIT))
            {
                // the CS is no longer locked, let's bail out
                return PalCsWaiterDidntWait;
            }

            lNewVal = lVal + lInc;

            // Make sure that this thread was whether an incoming one or it
            // was an awakened waiter and, in this case, we are now going to
            // turn off the awakened waiter bit
            _ASSERT_MSG(PALCS_LOCK_WAITER_INC == lInc ||
                        0 == (PALCS_LOCK_AWAKENED_WAITER & lNewVal));

            CS_TRACE("[WCS %p] Switching from {%d, %d, %d} to "
                "{%d, %d, %d} ==> ", pPalCriticalSection,
                PALCS_GETWCOUNT(lVal), PALCS_GETAWBIT(lVal), PALCS_GETLBIT(lVal),
                PALCS_GETWCOUNT(lNewVal), PALCS_GETAWBIT(lNewVal), PALCS_GETLBIT(lNewVal));

            lNewVal = InterlockedCompareExchange (&pPalCriticalSection->LockCount,
                                                  lNewVal, lVal);

            CS_TRACE("[WCS %p] ==> %s\n", pPalCriticalSection,
                           (lNewVal == lVal) ? "OK" : "NO");

        } while (lNewVal != lVal);

#ifdef _DEBUG
        if (NULL != pPalCriticalSection->DebugInfo)
        {
            pPalCriticalSection->DebugInfo->lContentionCount += 1;
        }
#endif // _DEBUG

        // Do the actual native wait
        palErr = PALCS_DoActualWait(pPalCriticalSection);
        _ASSERT_MSG(NO_ERROR == palErr, "Native CS wait failed\n");

        return PalCsReturnWaiterAwakened;
    }

    /*++
    Function:
      CorUnix::PALCS_DoActualWait

    Performs the actual native wait on the CS
    --*/
    PAL_ERROR PALCS_DoActualWait(PAL_CRITICAL_SECTION * pPalCriticalSection)
    {
        int iRet;
        PAL_ERROR palErr = NO_ERROR;

        CS_TRACE("Trying to go to sleep [CS=%p]\n", pPalCriticalSection);

        // Lock the mutex
        iRet = pthread_mutex_lock(&pPalCriticalSection->csndNativeData.mutex);
        if (0 != iRet)
        {
            palErr = ERROR_INTERNAL_ERROR;
            goto PCDAW_exit;
        }

        CS_TRACE("Actually Going to sleep [CS=%p]\n", pPalCriticalSection);

        while (0 == pPalCriticalSection->csndNativeData.iPredicate)
        {
            // Wait on the condition
            iRet = pthread_cond_wait(&pPalCriticalSection->csndNativeData.condition,
                                     &pPalCriticalSection->csndNativeData.mutex);

            CS_TRACE("Got a signal on condition [pred=%d]!\n",
                           pPalCriticalSection->csndNativeData.iPredicate);
            if (0 != iRet)
            {
                // Failed: unlock the mutex and bail out
                ASSERT("Failed waiting on condition in CS %p [err=%d]\n",
                       pPalCriticalSection, iRet);
                pthread_mutex_unlock(&pPalCriticalSection->csndNativeData.mutex);
                palErr = ERROR_INTERNAL_ERROR;
                goto PCDAW_exit;
            }
        }

        // Reset the predicate
        pPalCriticalSection->csndNativeData.iPredicate = 0;

        // Unlock the mutex
        iRet = pthread_mutex_unlock(&pPalCriticalSection->csndNativeData.mutex);
        if (0 != iRet)
        {
            palErr = ERROR_INTERNAL_ERROR;
            goto PCDAW_exit;
        }

    PCDAW_exit:

        CS_TRACE("Just woken up [CS=%p]\n", pPalCriticalSection);

        return palErr;
    }

    /*++
    Function:
      CorUnix::PALCS_WakeUpWaiter

    Wakes up the first thread waiting on the CS
    --*/
    PAL_ERROR PALCS_WakeUpWaiter(PAL_CRITICAL_SECTION * pPalCriticalSection)
    {
        int iRet;
        PAL_ERROR palErr = NO_ERROR;

        _ASSERT_MSG(PalCsFullyInitialized == pPalCriticalSection->cisInitState,
                    "Trying to wake up a waiter on CS not fully initialized\n");

        // Lock the mutex
        iRet = pthread_mutex_lock(&pPalCriticalSection->csndNativeData.mutex);
        if (0 != iRet)
        {
            palErr = ERROR_INTERNAL_ERROR;
            goto PCWUW_exit;
        }

        // Set the predicate
        pPalCriticalSection->csndNativeData.iPredicate = 1;

        CS_TRACE("Signaling condition/predicate [pred=%d]!\n",
                 pPalCriticalSection->csndNativeData.iPredicate);

        // Signal the condition
        iRet = pthread_cond_signal(&pPalCriticalSection->csndNativeData.condition);
        if (0 != iRet)
        {
            // Failed: set palErr, but continue in order to unlock
            // the mutex anyway
            ASSERT("Failed setting condition in CS %p [ret=%d]\n",
                   pPalCriticalSection, iRet);
            palErr = ERROR_INTERNAL_ERROR;
        }

        // Unlock the mutex
        iRet = pthread_mutex_unlock(&pPalCriticalSection->csndNativeData.mutex);
        if (0 != iRet)
        {
            palErr = ERROR_INTERNAL_ERROR;
            goto PCWUW_exit;
        }

    PCWUW_exit:
        return palErr;
    }

#ifdef _DEBUG
    /*++
    Function:
      CorUnix::PALCS_ReportStatisticalData

    Report creation/acquisition/contention statistical data for the all the
    CSs so far existed and no longer existing in the current process
    --*/
    void PALCS_ReportStatisticalData()
    {
#ifdef PAL_TRACK_CRITICAL_SECTIONS_DATA
        CPalThread * pThread = InternalGetCurrentThread();

        if (NULL == pThread) DebugBreak();

        // Take the lock for the global list of CS debug infos
        InternalEnterCriticalSection(pThread, (CRITICAL_SECTION*)&g_csPALCSsListLock);

        LONG lPALCSInitializeCount         = g_lPALCSInitializeCount;
        LONG lPALCSDeleteCount             = g_lPALCSDeleteCount;
        LONG lPALCSAcquireCount            = g_lPALCSAcquireCount;
        LONG lPALCSEnterCount              = g_lPALCSEnterCount;
        LONG lPALCSContentionCount         = g_lPALCSContentionCount;
        LONG lPALCSInternalInitializeCount = g_lPALCSInternalInitializeCount;
        LONG lPALCSInternalDeleteCount     = g_lPALCSInternalDeleteCount;
        LONG lPALCSInternalAcquireCount    = g_lPALCSInternalAcquireCount;
        LONG lPALCSInternalEnterCount      = g_lPALCSInternalEnterCount;
        LONG lPALCSInternalContentionCount = g_lPALCSInternalContentionCount;

        PLIST_ENTRY pItem = g_PALCSList.Flink;
        while (&g_PALCSList != pItem)
        {
            PCRITICAL_SECTION_DEBUG_INFO pDebugInfo =
                (PCRITICAL_SECTION_DEBUG_INFO)pItem;

            if (pDebugInfo->pOwnerCS->fInternal)
            {
                lPALCSInternalAcquireCount    += pDebugInfo->lAcquireCount;
                lPALCSInternalEnterCount      += pDebugInfo->lEnterCount;
                lPALCSInternalContentionCount += pDebugInfo->lContentionCount;
            }
            else
            {
                lPALCSAcquireCount            += pDebugInfo->lAcquireCount;
                lPALCSEnterCount              += pDebugInfo->lEnterCount;
                lPALCSContentionCount         += pDebugInfo->lContentionCount;
            }

            pItem = pItem->Flink;
        }

        // Release the lock for the global list of CS debug infos
        InternalLeaveCriticalSection(pThread, (CRITICAL_SECTION*)&g_csPALCSsListLock);

        TRACE("Critical Sections Statistical Data:\n");
        TRACE("{\n");
        TRACE("    Client code CSs:\n");
        TRACE("    {\n");
        TRACE("        Initialize Count: %d\n", lPALCSInitializeCount);
        TRACE("        Delete Count:     %d\n", lPALCSDeleteCount);
        TRACE("        Acquire Count:    %d\n", lPALCSAcquireCount);
        TRACE("        Enter Count:      %d\n", lPALCSEnterCount);
        TRACE("        Contention Count: %d\n", lPALCSContentionCount);
        TRACE("    }\n");
        TRACE("    Internal PAL CSs:\n");
        TRACE("    {\n");
        TRACE("        Initialize Count: %d\n", lPALCSInternalInitializeCount);
        TRACE("        Delete Count:     %d\n", lPALCSInternalDeleteCount);
        TRACE("        Acquire Count:    %d\n", lPALCSInternalAcquireCount);
        TRACE("        Enter Count:      %d\n", lPALCSInternalEnterCount);
        TRACE("        Contention Count: %d\n", lPALCSInternalContentionCount);
        TRACE("    }\n");
        TRACE("}\n");
#endif // PAL_TRACK_CRITICAL_SECTIONS_DATA
    }

    /*++
    Function:
      CorUnix::PALCS_DumpCSList

    Dumps the list of all the CS currently existing in this process.
    --*/
    void PALCS_DumpCSList()
    {
        CPalThread * pThread = InternalGetCurrentThread();

        // Take the lock for the global list of CS debug infos
        InternalEnterCriticalSection(pThread, (CRITICAL_SECTION*)&g_csPALCSsListLock);

        PLIST_ENTRY pItem = g_PALCSList.Flink;
        while (&g_PALCSList != pItem)
        {
            PCRITICAL_SECTION_DEBUG_INFO pDebugInfo =
                (PCRITICAL_SECTION_DEBUG_INFO)pItem;
            PPAL_CRITICAL_SECTION pCS = pDebugInfo->pOwnerCS;

            printf("CS @ %p \n"
                   "{\tDebugInfo = %p -> \n",
                   pCS, pDebugInfo);

            printf("\t{\n\t\t[Link]\n\t\tpOwnerCS = %p\n"
                   "\t\tAcquireCount \t= %d\n"
                   "\t\tEnterCount \t= %d\n"
                   "\t\tContentionCount = %d\n",
                   pDebugInfo->pOwnerCS, pDebugInfo->lAcquireCount.Load(),
                   pDebugInfo->lEnterCount.Load(), pDebugInfo->lContentionCount.Load());
            printf("\t}\n");

            printf("\tLockCount \t= %#x\n"
                   "\tRecursionCount \t= %d\n"
                   "\tOwningThread \t= %p\n"
                   "\tSpinCount \t= %u\n"
                   "\tfInternal \t= %d\n"
                   "\teInitState \t= %u\n"
                   "\tpNativeData \t= %p ->\n",
                   pCS->LockCount.Load(), pCS->RecursionCount, (void *)pCS->OwningThread,
                   (unsigned)pCS->SpinCount,
#ifdef PAL_TRACK_CRITICAL_SECTIONS_DATA
                   (int)pCS->fInternal,
#else
                   (int)0,
#endif // PAL_TRACK_CRITICAL_SECTIONS_DATA
                   pCS->cisInitState.Load(), &pCS->csndNativeData);

            printf("\t{\n\t\t[mutex]\n\t\t[condition]\n"
                   "\t\tPredicate \t= %d\n"
                   "\t}\n}\n",pCS->csndNativeData.iPredicate);

            printf("}\n");

            pItem = pItem->Flink;
        }

        // Release the lock for the global list of CS debug infos
        InternalLeaveCriticalSection(pThread, (CRITICAL_SECTION*)&g_csPALCSsListLock);
    }
#endif // _DEBUG


#if defined(MUTEX_BASED_CSS) || defined(_DEBUG)
    /*++
    Function:
      CorUnix::InternalEnterCriticalSection

    Enters a CS, causing the thread to block if the CS is owned by
    another thread
    --*/
#ifdef MUTEX_BASED_CSS
    void InternalEnterCriticalSection(
        CPalThread * pThread,
        PCRITICAL_SECTION pCriticalSection)
#else // MUTEX_BASED_CSS
    void MTX_InternalEnterCriticalSection(
        CPalThread * pThread,
        PCRITICAL_SECTION pCriticalSection)
#endif // MUTEX_BASED_CSS

    {
        PAL_CRITICAL_SECTION * pPalCriticalSection =
            reinterpret_cast<PAL_CRITICAL_SECTION*>(pCriticalSection);
        int iRet;
        SIZE_T threadId;

        _ASSERTE(PalCsNotInitialized != pPalCriticalSection->cisInitState);

        threadId = ObtainCurrentThreadId(pThread);

        /* check if the current thread already owns the criticalSection */
        if (pPalCriticalSection->OwningThread == threadId)
        {
            _ASSERTE(0 < pPalCriticalSection->RecursionCount);
            pPalCriticalSection->RecursionCount += 1;
            return;
        }

        iRet = pthread_mutex_lock(&pPalCriticalSection->csndNativeData.mutex);
        _ASSERTE(0 == iRet);

        pPalCriticalSection->OwningThread = threadId;
        pPalCriticalSection->RecursionCount = 1;
    }


    /*++
    Function:
      CorUnix::InternalLeaveCriticalSection

    Leaves a currently owned CS
    --*/
#ifdef MUTEX_BASED_CSS
    void InternalLeaveCriticalSection(
        CPalThread * pThread,
        PCRITICAL_SECTION pCriticalSection)
#else // MUTEX_BASED_CSS
    void MTX_InternalLeaveCriticalSection(
        CPalThread * pThread,
        PCRITICAL_SECTION pCriticalSection)
#endif // MUTEX_BASED_CSS
    {
        PAL_CRITICAL_SECTION * pPalCriticalSection =
            reinterpret_cast<PAL_CRITICAL_SECTION*>(pCriticalSection);
        int iRet;
#ifdef _DEBUG
        SIZE_T threadId;

        _ASSERTE(PalCsNotInitialized != pPalCriticalSection->cisInitState);

        threadId = ObtainCurrentThreadId(pThread);
        _ASSERTE(threadId == pPalCriticalSection->OwningThread);

        if (0 >= pPalCriticalSection->RecursionCount)
            DebugBreak();

        _ASSERTE(0 < pPalCriticalSection->RecursionCount);
#endif // _DEBUG

        if (0 < --pPalCriticalSection->RecursionCount)
            return;

        pPalCriticalSection->OwningThread = 0;

        iRet = pthread_mutex_unlock(&pPalCriticalSection->csndNativeData.mutex);
        _ASSERTE(0 == iRet);
    }

#endif // MUTEX_BASED_CSS || _DEBUG
}
