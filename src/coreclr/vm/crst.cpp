// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// CRST.CPP
//

//


#include "common.h"

#include "crst.h"
#include "log.h"
#include "corhost.h"

// We need to know if we're on the helper thread.  We need this header for g_pDebugInterface.
#include "dbginterface.h"
#include "threadsuspend.h"

#define __IN_CRST_CPP
#include <crsttypes_generated.h>
#undef __IN_CRST_CPP

#if defined(DACCESS_COMPILE) && defined(TARGET_UNIX) && !defined(CROSS_COMPILE)
    // Validate the DAC T_CRITICAL_SECTION matches the runtime CRITICAL section when we are not cross compiling.
    // This is important when we are cross OS compiling the DAC
    static_assert(PAL_CS_NATIVE_DATA_SIZE == DAC_CS_NATIVE_DATA_SIZE,     T_CRITICAL_SECTION_VALIDATION_MESSAGE);
    static_assert(sizeof(CRITICAL_SECTION) == sizeof(T_CRITICAL_SECTION), T_CRITICAL_SECTION_VALIDATION_MESSAGE);

    static_assert(offsetof(CRITICAL_SECTION, DebugInfo)      == offsetof(T_CRITICAL_SECTION, DebugInfo),      T_CRITICAL_SECTION_VALIDATION_MESSAGE);
    static_assert(offsetof(CRITICAL_SECTION, LockCount)      == offsetof(T_CRITICAL_SECTION, LockCount),      T_CRITICAL_SECTION_VALIDATION_MESSAGE);
    static_assert(offsetof(CRITICAL_SECTION, RecursionCount) == offsetof(T_CRITICAL_SECTION, RecursionCount), T_CRITICAL_SECTION_VALIDATION_MESSAGE);
    static_assert(offsetof(CRITICAL_SECTION, OwningThread)   == offsetof(T_CRITICAL_SECTION, OwningThread),   T_CRITICAL_SECTION_VALIDATION_MESSAGE);
    static_assert(offsetof(CRITICAL_SECTION, SpinCount)      == offsetof(T_CRITICAL_SECTION, SpinCount),      T_CRITICAL_SECTION_VALIDATION_MESSAGE);
#endif // defined(DACCESS_COMPILE) && defined(TARGET_UNIX) && !defined(CROSS_COMPILE)

#ifndef DACCESS_COMPILE
Volatile<LONG> g_ShutdownCrstUsageCount = 0;

//-----------------------------------------------------------------
// Initialize critical section
//-----------------------------------------------------------------
VOID CrstBase::InitWorker(INDEBUG_COMMA(CrstType crstType) CrstFlags flags)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
    } CONTRACTL_END;

    _ASSERTE((flags & CRST_INITIALIZED) == 0);

    {
        SetOSCritSec ();
    }

    {
        InitializeCriticalSection(&m_criticalsection);
    }

    SetFlags(flags);
    SetCrstInitialized();

#ifdef _DEBUG
    DebugInit(crstType, flags);
#endif
}

//-----------------------------------------------------------------
// Clean up critical section
//-----------------------------------------------------------------
void CrstBase::Destroy()
{
    WRAPPER_NO_CONTRACT;

    // nothing to do if not initialized
    if (!IsCrstInitialized())
        return;

    // If this assert fired, a crst got deleted while some thread
    // still owned it.  This can happen if the process detaches from
    // our DLL.
#ifdef _DEBUG
    EEThreadId holderthreadid = m_holderthreadid;
    _ASSERTE(holderthreadid.IsUnknown() || IsAtProcessExit() || g_fEEShutDown);
#endif

    // If a lock is host breakable, a host is required to block the release call until
    // deadlock detection is finished.
    GCPreemp __gcHolder((m_dwFlags & CRST_HOST_BREAKABLE) == CRST_HOST_BREAKABLE);

    {
        DeleteCriticalSection(&m_criticalsection);
    }

    LOG((LF_SYNC, INFO3, "CrstBase::Destroy %p\n", this));
#ifdef _DEBUG
    DebugDestroy();
#endif

    ResetFlags();
}

extern void WaitForEndOfShutdown();

//-----------------------------------------------------------------
// If we're in shutdown (as determined by caller since each lock needs its
// own shutdown flag) and this is a non-special thread (not helper/finalizer/shutdown),
// then release the crst and block forever.
// See the prototype for more details.
//-----------------------------------------------------------------
void CrstBase::ReleaseAndBlockForShutdownIfNotSpecialThread()
{
    CONTRACTL {
        NOTHROW;

        // We're almost always MODE_PREEMPTIVE, but if it's a thread suspending for GC,
        // then we might be MODE_COOPERATIVE. Fortunately in that case, we don't block on shutdown.
        // We assert this below.
        MODE_ANY;
        GC_NOTRIGGER;

        PRECONDITION(this->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    if ((t_ThreadType & (ThreadType_Finalizer|ThreadType_DbgHelper|ThreadType_Shutdown|ThreadType_GC)) == 0)
    {
        // The process is shutting down. Release the lock and just block forever.
        this->Leave();

        // is this safe to use here since we never return?
        GCX_ASSERT_PREEMP();

        WaitForEndOfShutdown();
        __SwitchToThread(INFINITE, CALLER_LIMITS_SPINNING);
        _ASSERTE (!"Can not reach here");
    }
}

#endif // DACCESS_COMPILE


//-----------------------------------------------------------------
// Acquire the lock.
//-----------------------------------------------------------------
#ifdef DACCESS_COMPILE
// In DAC builds, we will not actually take the lock. Instead, we just need to determine
// whether the LS holds the lock. If it does, we assume the locked data is in an inconsistent
// state and throw, rather than using erroneous values.
// Argument:
//     input: noLevelCheckFlag - indicates whether to check the crst level
// Note: Throws
void CrstBase::Enter(INDEBUG(NoLevelCheckFlag noLevelCheckFlag/* = CRST_LEVEL_CHECK*/))
{
#ifdef _DEBUG
    if (m_entercount != 0)
    {
        ThrowHR(CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
    }
#endif
}
#else // !DACCESS_COMPILE




void CrstBase::Enter(INDEBUG(NoLevelCheckFlag noLevelCheckFlag/* = CRST_LEVEL_CHECK*/))
{
    //-------------------------------------------------------------------------------------------
    // What, no CONTRACT?
    //
    // We can't put an actual CONTRACT here as PostEnter() makes unscoped changes to the GC_NoTrigger
    // counter. But we do perform the equivalent checks manually.
    //
    // What's worse, the implied contract differs for different flavors of crst.
    //
    // THROWS/FAULT
    //
    //     A crst can be HOST_BREAKBALE or not. A HOST_BREAKABLE crst can throw on an attempt to enter
    //     (due to deadlock breaking by the host.) A non-breakable crst will never
    //     throw or OOM or fail an enter.
    //
    //
    //
    //
    // GC/MODE
    //     Orthogonally, a crst can be one of the following flavors. We only want to see the
    //     "normal" type used in new code. Other types, kept for legacy reasons, are listed in
    //     order from least objectionable to most objectionable.
    //
    //         normal - This is the preferred type of crst. Enter() will force-switch your thread
    //            into preemptive mode if it isn't already. Thus, the effective contract is:
    //
    //            MODE_ANY
    //            GC_TRIGGERS
    //
    //
    //
    //         CRST_UNSAFE_COOPGC - You can only attempt to acquire this crst if you're already
    //            in coop mode. It is guaranteed no GC will occur while waiting to acquire the lock.
    //            While you hold the lock, your thread is in a GCFORBID state.
    //
    //            MODE_COOP
    //            GC_NOTRIGGER
    //
    //
    //
    //         CRST_UNSAFE_ANYMODE - You can attempt to acquire this in either mode. Entering the
    //            crst will not change your thread mode but it will increment the GCNoTrigger count.
    //
    //            MODE_ANY
    //            GC_NOTRIGGER
    //------------------------------------------------------------------------------------------------

#ifdef ENABLE_CONTRACTS_IMPL
    ClrDebugState *pClrDebugState = CheckClrDebugState();
    if (pClrDebugState)
    {
        if (m_dwFlags & CRST_HOST_BREAKABLE)
        {
            if (pClrDebugState->IsFaultForbid() &&
                !(pClrDebugState->ViolationMask() & (FaultViolation|FaultNotFatal|BadDebugState)))
            {
                CONTRACT_ASSERT("You cannot enter a HOST_BREAKABLE lock in a FAULTFORBID region.",
                                Contract::FAULT_Forbid,
                                Contract::FAULT_Mask,
                                __FUNCTION__,
                                __FILE__,
                                __LINE__);
            }

            if (!(pClrDebugState->CheckOkayToThrowNoAssert()))
            {
                CONTRACT_ASSERT("You cannot enter a HOST_BREAKABLE lock in a NOTHROW region.",
                                Contract::THROWS_No,
                                Contract::THROWS_Mask,
                                __FUNCTION__,
                                __FILE__,
                                __LINE__);
            }
        }

        // If we might want to toggle the GC mode, then we better not be in a GC_NOTRIGGERS region
        if (!(m_dwFlags & (CRST_UNSAFE_COOPGC | CRST_UNSAFE_ANYMODE | CRST_GC_NOTRIGGER_WHEN_TAKEN)))
        {
            if (pClrDebugState->GetGCNoTriggerCount())
            {
                // If we have no thread object, we won't be toggling the GC.  This is the case,
                // for example, on the debugger helper thread which is always GC_NOTRIGGERS.
                if (GetThreadNULLOk() != NULL)
                {
                    // Will we really need to change GC mode COOPERATIVE to PREEMPTIVE?
                    if (GetThreadNULLOk()->PreemptiveGCDisabled())
                    {
                        if (!((GCViolation | BadDebugState) & pClrDebugState->ViolationMask()))
                        {
                            CONTRACT_ASSERT("You cannot enter a lock in a GC_NOTRIGGER + MODE_COOPERATIVE region.",
                                            Contract::GC_NoTrigger,
                                            Contract::GC_Mask,
                                            __FUNCTION__,
                                            __FILE__,
                                            __LINE__);
                        }
                    }
                }
            }
        }

        // The mode checks and enforcement of GC_NOTRIGGER during the lock are done in CrstBase::PostEnter().

    }
#endif  //ENABLE_CONTRACTS_IMPL



    SCAN_IGNORE_THROW;
    SCAN_IGNORE_FAULT;
    SCAN_IGNORE_TRIGGER;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    _ASSERTE(IsCrstInitialized());

    // Is Critical Section entered?
    // We could have perhaps used m_criticalsection.LockCount, but
    // while spinning, we want to fire the ETW event only once
    BOOL fIsCriticalSectionEnteredAfterFailingOnce = FALSE;

    Thread * pThread;
    BOOL fToggle;

    pThread = GetThreadNULLOk();
    fToggle = ((m_dwFlags & (CRST_UNSAFE_ANYMODE | CRST_UNSAFE_COOPGC | CRST_GC_NOTRIGGER_WHEN_TAKEN)) == 0)   // condition normally false
              && pThread &&  pThread->PreemptiveGCDisabled();

    if (fToggle) {
        pThread->EnablePreemptiveGC();
    }

#ifdef _DEBUG
    PreEnter ();
#endif

    _ASSERTE(noLevelCheckFlag == CRST_NO_LEVEL_CHECK || IsSafeToTake() || g_fEEShutDown);

    // Check for both rare case using one if-check
    if (m_dwFlags & (CRST_TAKEN_DURING_SHUTDOWN | CRST_DEBUGGER_THREAD))
    {
        if (m_dwFlags & CRST_TAKEN_DURING_SHUTDOWN)
        {
            // increment the usage count of locks that can be taken during shutdown
            InterlockedIncrement(&g_ShutdownCrstUsageCount);
        }

        // If this is a debugger lock, bump up the "Can't-Stop" count.
        // We'll bump it down when we release the lock.
        if (m_dwFlags & CRST_DEBUGGER_THREAD)
        {
            IncCantStopCount();
        }
    }

    EnterCriticalSection(&m_criticalsection);

#ifdef _DEBUG
    PostEnter();
#endif

    if (fToggle)
    {
        pThread->DisablePreemptiveGC();
    }
}

//-----------------------------------------------------------------
// Release the lock.
//-----------------------------------------------------------------
void CrstBase::Leave()
{
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    _ASSERTE(IsCrstInitialized());

#ifdef _DEBUG
    PreLeave ();
#endif //_DEBUG

#if defined(_DEBUG)
    Thread * pThread = GetThreadNULLOk();
#endif

    LeaveCriticalSection(&m_criticalsection);

    // Check for both rare case using one if-check
    if (m_dwFlags & (CRST_TAKEN_DURING_SHUTDOWN | CRST_DEBUGGER_THREAD))
    {
        // If this is a debugger lock, restore the "Can't-Stop" count.
        // We bumped it up when we Entered the lock.
        if (m_dwFlags & CRST_DEBUGGER_THREAD)
        {
            DecCantStopCount();
        }

        if (m_dwFlags & CRST_TAKEN_DURING_SHUTDOWN)
        {
            // decrement the usage count of locks that can be taken during shutdown
            _ASSERTE_MSG(g_ShutdownCrstUsageCount.Load() > 0, "Attempting to leave a lock that was never taken!");
            InterlockedDecrement(&g_ShutdownCrstUsageCount);
        }
    }

#ifdef _DEBUG
    //_ASSERTE(m_cannotLeave==0 || OwnedByCurrentThread());

    if ((pThread != NULL) &&
        (m_dwFlags & CRST_DEBUG_ONLY_CHECK_FORBID_SUSPEND_THREAD))
    {   // The lock requires ForbidSuspendRegion while it is taken
        CONSISTENCY_CHECK_MSGF(pThread->IsInForbidSuspendRegion(), ("ForbidSuspend region was released before the lock:'%s'", m_tag));
    }
#endif //_DEBUG
} // CrstBase::Leave


#ifdef _DEBUG

thread_local CrstBase* t_pOwnedCrstsChain;

void CrstBase::PreEnter()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    // Are we in the shutdown sequence and in phase 2 of it?
    if (g_fProcessDetach && (g_fEEShutDown & ShutDown_Phase2))
    {
        // Ensure that this lock has been flagged to be taken during shutdown
        _ASSERTE_MSG(CanBeTakenDuringShutdown(), "Attempting to take a lock at shutdown that is not CRST_TAKEN_DURING_SHUTDOWN");
    }

    Thread * pThread = GetThreadNULLOk();

    if (pThread)
    {
        // If the thread has SpinLock, it can not take Crst.
        _ASSERTE ((pThread->m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);
    }

    // If we're on the debugger helper thread, we can only take helper thread locks.
    bool fIsHelperThread = (g_pDebugInterface == NULL) ? false : g_pDebugInterface->ThisIsHelperThread();
    bool fIsDebuggerLock = (m_dwFlags & CRST_DEBUGGER_THREAD) != 0;

    // don't enforce this check during regular process exit or fail fast
    if (fIsHelperThread && !fIsDebuggerLock && !IsAtProcessExit() && !g_fFastExitProcess)
    {
        CONSISTENCY_CHECK_MSGF(false, ("Helper thread taking non-helper lock:'%s'", m_tag));
    }

    // If a thread suspends another thread, it cannot acquire locks.
    if ((pThread != NULL)
        && (pThread->Debug_GetUnsafeSuspendeeCount() != 0))
    {
        CONSISTENCY_CHECK_MSGF(false, ("Suspender thread taking non-suspender lock:'%s'", m_tag));
    }

    if ((ThreadStore::s_pThreadStore != NULL)
        && ThreadStore::s_pThreadStore->IsCrstForThreadStore(this))
    {
        return;
    }

    if (m_dwFlags & CRST_UNSAFE_COOPGC)
    {
        CONSISTENCY_CHECK (IsGCThread ()
                          || (pThread != NULL && pThread->PreemptiveGCDisabled())
                           // If GC heap has not been initialized yet, there is no need to synchronize with GC.
                           // This check is mainly for code called from EEStartup.
                          || (pThread == NULL && !GCHeapUtilities::IsGCHeapInitialized()) );
    }

    if ((pThread != NULL) &&
        (m_dwFlags & CRST_DEBUG_ONLY_CHECK_FORBID_SUSPEND_THREAD))
    {
        CONSISTENCY_CHECK_MSGF(pThread->IsInForbidSuspendRegion(), ("The lock '%s' can be taken only in ForbidSuspend region.", m_tag));
    }
}

void CrstBase::PostEnter()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if ((m_dwFlags & CRST_HOST_BREAKABLE) != 0)
    {
        HOST_BREAKABLE_CRST_TAKEN(this);
    }
    else
    {
        EE_LOCK_TAKEN(this);
    }

    _ASSERTE((m_entercount == 0 && m_holderthreadid.IsUnknown()) ||
             m_holderthreadid.IsCurrentThread() ||
             IsAtProcessExit());
    m_holderthreadid.SetToCurrentThread();
    m_entercount++;

    if (m_entercount == 1)
    {
        _ASSERTE((m_next == NULL) && (m_prev == NULL));

        // Link this Crst into the Thread's chain of OwnedCrsts
        CrstBase *pcrst = t_pOwnedCrstsChain;
        if (pcrst == NULL)
        {
            t_pOwnedCrstsChain = this;
        }
        else
        {
            while (pcrst->m_next != NULL)
                pcrst = pcrst->m_next;
            pcrst->m_next = this;
            m_prev = pcrst;
        }
    }

    Thread * pThread = GetThreadNULLOk();
    if ((m_dwFlags & CRST_HOST_BREAKABLE) == 0)
    {
        if (pThread)
        {
            pThread->IncUnbreakableLockCount();
        }
    }

    if ((ThreadStore::s_pThreadStore != NULL)
        && ThreadStore::s_pThreadStore->IsCrstForThreadStore(this))
    {
        return;
    }

    if (m_dwFlags & (CRST_UNSAFE_ANYMODE | CRST_UNSAFE_COOPGC | CRST_GC_NOTRIGGER_WHEN_TAKEN))
    {
        if (pThread == NULL)
        {
            // Cannot set NoTrigger.  This could conceivably turn into
            // A GC hole if the thread is created and then a GC rendezvous happens
            // while the lock is still held.
        }
        else
        {
            // Keep a count, since the thread may change from NULL to non-NULL and
            // we don't want to have unbalanced NoTrigger calls
            m_countNoTriggerGC++;
            INCONTRACT(pThread->BeginNoTriggerGC(__FILE__, __LINE__));
        }
    }
}

void CrstBase::PreLeave()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(OwnedByCurrentThread());
    _ASSERTE(m_entercount > 0);
    m_entercount--;
    if (!m_entercount) {
        m_holderthreadid.Clear();

        // Delink it from the Thread's chain of OwnedChain
        if (m_prev)
            m_prev->m_next = m_next;
        else
            t_pOwnedCrstsChain = m_next;

        if (m_next)
            m_next->m_prev = m_prev;

        m_next = NULL;
        m_prev = NULL;
    }

    Thread * pThread = GetThreadNULLOk();

    if ((m_dwFlags & CRST_HOST_BREAKABLE) == 0)
    {
        if (pThread)
        {
            pThread->DecUnbreakableLockCount();
        }
    }

    if (m_countNoTriggerGC > 0 && !ThreadStore::s_pThreadStore->IsCrstForThreadStore(this))
    {
        m_countNoTriggerGC--;
        if (pThread != NULL)
        {
            INCONTRACT(pThread->EndNoTriggerGC());
        }
    }

    if ((m_dwFlags & CRST_HOST_BREAKABLE) != 0)
    {
        HOST_BREAKABLE_CRST_RELEASED(this);
    }
    else
    {
        EE_LOCK_RELEASED(this);
    }

    // Are we in the shutdown sequence and in phase 2 of it?
    if (g_fProcessDetach && (g_fEEShutDown & ShutDown_Phase2))
    {
        // Ensure that this lock has been flagged to be taken during shutdown
        _ASSERTE_MSG(CanBeTakenDuringShutdown(), "Attempting to leave a lock at shutdown that is not CRST_TAKEN_DURING_SHUTDOWN");
    }

}

// We have seen several times that a Crst is not destroyed before its memory is freed.  This corrupts
// our chain, and also causes memory leak.  The following structure is to track what Crst exists.
// If our chain is broken, find out which Crst causes problem, then lookup this array.  The problematic
// Crst can be identified with crstType.
struct CrstDebugInfo
{
    CrstBase *pAddress;
    CrstType  crstType;
};
const int crstDebugInfoCount = 4000;
CrstDebugInfo crstDebugInfo[crstDebugInfoCount];

void CrstBase::DebugInit(CrstType crstType, CrstFlags flags)
{
    LIMITED_METHOD_CONTRACT;

    m_crstType = crstType;
    m_tag = GetCrstName(crstType);
    m_crstlevel = GetCrstLevel(crstType);
    m_holderthreadid.Clear();
    m_entercount       = 0;
    m_next = NULL;
    m_prev = NULL;
    m_cannotLeave=0;

    _ASSERTE((m_dwFlags & ~(CRST_REENTRANCY |
                          CRST_UNSAFE_SAMELEVEL |
                          CRST_UNSAFE_COOPGC |
                          CRST_UNSAFE_ANYMODE |
                          CRST_DEBUGGER_THREAD |
                          CRST_HOST_BREAKABLE |
                          CRST_OS_CRIT_SEC |
                          CRST_INITIALIZED |
                          CRST_TAKEN_DURING_SHUTDOWN |
                          CRST_GC_NOTRIGGER_WHEN_TAKEN |
                          CRST_DEBUG_ONLY_CHECK_FORBID_SUSPEND_THREAD)) == 0);

    // @todo - Any Crst w/ CRST_DEBUGGER_THREAD must be on a special blessed list. Check that here.

    LOG((LF_SYNC, INFO3, "CrstBase::DebugInit %p\n", this));

    for (int i = 0; i < crstDebugInfoCount; i++)
    {
        if (crstDebugInfo[i].pAddress == NULL)
        {
            crstDebugInfo[i].pAddress = this;
            crstDebugInfo[i].crstType = crstType;
            break;
        }
    }

    m_countNoTriggerGC = 0;
}

void CrstBase::DebugDestroy()
{
    LIMITED_METHOD_CONTRACT;

    // Ideally, when we destroy the crst, it wouldn't be held.
    // This is violated if a thread holds a lock and is asynchronously killed
    // (such as what happens on ExitProcess).
    // Delink it from the Thread's chain of OwnedChain
    if (IsAtProcessExit())
    {
        // In shutdown scenario, crst may or may not be held.
        if (m_prev == NULL)
        {
            if (!m_holderthreadid.IsUnknown()) // Crst taken!
            {
                if (m_next)
                    m_next->m_prev = NULL; // workaround: break up the chain
                t_pOwnedCrstsChain = NULL;
            }
        }
        else
        {
            m_prev->m_next = m_next;
            if (m_next)
                m_next->m_prev = m_prev;
        }
    }
    else
    {
        // Crst is destroyed while being held.
        CONSISTENCY_CHECK_MSGF(
            ((m_prev == NULL) && (m_next == NULL) && m_holderthreadid.IsUnknown()),
            ("CRST '%s' is destroyed while being held in non-shutdown scenario.\n"
            "this=0x%p, m_prev=0x%p. m_next=0x%p", m_tag, this, this->m_prev, this->m_next));
    }

    FillMemory(&m_criticalsection, sizeof(m_criticalsection), 0xcc);
    m_holderthreadid.Clear();
    m_entercount     = 0xcccccccc;

    m_next = (CrstBase*)POISONC;
    m_prev = (CrstBase*)POISONC;

    for (int i = 0; i < crstDebugInfoCount; i++)
    {
        if (crstDebugInfo[i].pAddress == this)
        {
            crstDebugInfo[i].pAddress = NULL;
            crstDebugInfo[i].crstType = kNumberOfCrstTypes;
            break;
        }
    }
}

//-----------------------------------------------------------------
// Check if attempting to take the lock would violate level order.
//-----------------------------------------------------------------
BOOL CrstBase::IsSafeToTake()
{
    CONTRACTL {
        DEBUG_ONLY;
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
    } CONTRACTL_END;

    // If mscoree.dll is being detached
    if (IsAtProcessExit())
        return TRUE;

    // Cannot take a Crst in cooperative mode unless CRST_UNSAFE_COOPGC is set, in
    // which case it must always be taken in this mode.
    // If there is no thread object, we ignore the check since this thread isn't
    // coordinated with the GC.
    Thread * pThread = GetThreadNULLOk();

    _ASSERTE(pThread == NULL ||
             (pThread->PreemptiveGCDisabled() == ((m_dwFlags & CRST_UNSAFE_COOPGC) != 0)) ||
             ((m_dwFlags & (CRST_UNSAFE_ANYMODE | CRST_GC_NOTRIGGER_WHEN_TAKEN)) != 0) ||
             (GCHeapUtilities::IsGCInProgress() && pThread == ThreadSuspend::GetSuspensionThread()));


    if (m_holderthreadid.IsCurrentThread())
    {
        // If we already hold it, we can't violate level order.
        // Check if client wanted to allow reentrancy.
        if ((m_dwFlags & CRST_REENTRANCY) == 0)
        {
            LOG((LF_SYNC, INFO3, "Crst Reentrancy violation on %s\n", m_tag));
            // So that we can debug here.
            _ASSERTE (g_fEEShutDown || !"Crst Reentrancy violation");
        }
        return ((m_dwFlags & CRST_REENTRANCY) != 0);
    }

    // Is the current Crst exempt from the Crst ranking enforcement?
    if (m_crstlevel == CRSTUNORDERED
        // when the thread is doing a stressing GC, some Crst violations could be ignored
        // also, we want to keep an explicit list of Crst's that we may take during GC stress
        || (pThread && pThread->GetGCStressing ()
            && (m_crstType == CrstThreadStore || m_crstType == CrstHandleTable
                || m_crstType == CrstSyncBlockCache || m_crstType == CrstIbcProfile
                || m_crstType == CrstAvailableParamTypes || m_crstType == CrstSystemDomainDelayedUnloadList
                || m_crstType == CrstAssemblyList || m_crstType == CrstJumpStubCache
                || m_crstType == CrstSingleUseLock)
           )
        || (pThread && pThread->GetUniqueStacking ())
    )
    {
        return TRUE;
    }

    // See if the current thread already owns a lower or sibling lock.
    BOOL fSafe = TRUE;
    for (CrstBase *pcrst = t_pOwnedCrstsChain; pcrst != NULL; pcrst = pcrst->m_next)
    {
        fSafe =
            !pcrst->m_holderthreadid.IsCurrentThread()
            || (pcrst->m_crstlevel == CRSTUNORDERED)
            || (pcrst->m_crstlevel > m_crstlevel)
            || (pcrst->m_crstlevel == m_crstlevel && (m_dwFlags & CRST_UNSAFE_SAMELEVEL) != 0);
        if (!fSafe)
        {
            LOG((LF_SYNC, INFO3, "Crst Level violation: Can't take level %lu lock %s because you already holding level %lu lock %s\n",
                (ULONG)m_crstlevel, m_tag, (ULONG)(pcrst->m_crstlevel), pcrst->m_tag));
            // So that we can debug here.
            if (!g_fEEShutDown)
            {
                CONSISTENCY_CHECK_MSGF(false, ("Crst Level violation: Can't take level %lu lock %s because you already holding level %lu lock %s\n",
                                               (ULONG)m_crstlevel,
                                               m_tag,
                                               (ULONG)(pcrst->m_crstlevel),
                                               pcrst->m_tag));
            }
            break;
        }
    }
    return fSafe;
}

#endif // _DEBUG

CrstBase::CrstAndForbidSuspendForDebuggerHolder::CrstAndForbidSuspendForDebuggerHolder(CrstBase *pCrst)
    : m_pCrst(pCrst), m_pThreadForExitingForbidRegion(nullptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (pCrst == nullptr)
    {
        return;
    }

    // Reentrant locks are currently not supported
    _ASSERTE((pCrst->m_dwFlags & CRST_REENTRANCY) == 0);

    Thread *pThread = GetThreadNULLOk();
    if (pThread == nullptr || pThread->IsInForbidSuspendForDebuggerRegion())
    {
        AcquireLock(pCrst);
        return;
    }

    while (true)
    {
        // Enter the forbid region and make that state change visible to other threads (memory barrier) before checking for the
        // TS_DebugSuspendPending state. The other interacting thread in SysStartSuspendForDebug(), sets TS_DebugSuspendPending
        // and makes that state change visible to other threads (memory barrier) before checking for whether this thread is in
        // the forbid region. This ensures that in race conditions where both threads update the respective state and make the
        // state change visible to other threads, at least one of those threads will see the state change made by the other
        // thread. If this thread sees the state change (TS_DebugSuspendPending), it will avoid entering the forbid region by
        // exiting the lock and pulsing the GC mode to try suspending for the debugger. If SysStartSuspendForDebug() sees the
        // state change (that this thread is in the forbid region), then it will flag this thread appropriately to sync for
        // suspend, and the debugger will later identify this thread as synced after this thread leaves the forbid region.
        //
        // The forbid region could be entered after acquiring the lock, but an additional memory barrier would be necessary. It
        // is entered before the lock just to make use of the implicit memory barrier from acquiring the lock. It is anyway a
        // prerequisite for entering a lock along with entering a forbid-suspend-for-debugger region, that the lock is not held
        // for too long such that the thread can suspend for the debugger in reasonable time.
        pThread->EnterForbidSuspendForDebuggerRegion();
        AcquireLock(pCrst); // implicit full memory barrier on all supported platforms

        // This can be an opportunistic check (instead of a volatile load), because if the GC mode change below does not suspend
        // for the debugger (which is also possible with a volatile load), it will just loop around and try again if necessary
        if (!pThread->HasThreadStateOpportunistic(Thread::TS_DebugSuspendPending))
        {
            m_pThreadForExitingForbidRegion = pThread;
            return;
        }

        // Cannot enter the forbid region when a suspend for the debugger is pending because there are likely to be subsequent
        // changes to the GC mode inside the lock, and this thread needs to suspend for the debugger in reasonable time. Exit
        // the lock and pulse the GC mode to suspend for the debugger.
        ReleaseLock(pCrst);
        pThread->ExitForbidSuspendForDebuggerRegion();
        GCX_COOP();
    }
}

CrstBase::CrstAndForbidSuspendForDebuggerHolder::~CrstAndForbidSuspendForDebuggerHolder()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (m_pCrst == nullptr)
    {
        return;
    }

    ReleaseLock(m_pCrst);
    if (m_pThreadForExitingForbidRegion != nullptr)
    {
        m_pThreadForExitingForbidRegion->ExitForbidSuspendForDebuggerRegion();
    }
}

#endif // !DACCESS_COMPILE

#ifdef TEST_DATA_CONSISTENCY
// used for test purposes. Determines if a crst is held.
// Arguments:
//     input: pLock - the lock to test
// Note: Throws if the lock is held

void DebugTryCrst(CrstBase * pLock)
{
    SUPPORTS_DAC;

    if (g_pConfig && g_pConfig->TestDataConsistency())
    {
        CrstHolder crstHolder (pLock);
    }
}
#endif

