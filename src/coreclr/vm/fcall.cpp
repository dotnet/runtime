// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// FCALL.CPP
//

//


#include "common.h"
#include "vars.hpp"
#include "fcall.h"
#include "excep.h"
#include "frames.h"
#include "gms.h"
#include "ecall.h"
#include "eeconfig.h"

#ifdef ENABLE_CONTRACTS

/**************************************************************************************/
#if defined(TARGET_X86) && defined(ENABLE_PERF_COUNTERS)
static int64_t getCycleCount() {

    LIMITED_METHOD_CONTRACT;
    return GET_CYCLE_COUNT();
}
#else
static int64_t getCycleCount() { LIMITED_METHOD_CONTRACT; return(0); }
#endif

/**************************************************************************************/
// No contract here: The contract destructor restores the thread contract state to what it was
// soon after constructing the contract. This would have the effect of reverting the contract
// state change made by the call to BeginForbidGC.
DEBUG_NOINLINE ForbidGC::ForbidGC(const char *szFile, int lineNum)
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    m_pThread = GetThread();
    m_pThread->BeginForbidGC(szFile, lineNum);
}

/**************************************************************************************/
// No contract here: The contract destructor restores the thread contract state to what it was
// soon after constructing the contract. This would have the effect of reverting the contract
// state change made by the call to BeginForbidGC.
DEBUG_NOINLINE ForbidGC::~ForbidGC()
{
    SCAN_SCOPE_END;

    // IF EH happens, this is still called, in which case
    // we should not bother

    if (m_pThread->RawGCNoTrigger())
        m_pThread->EndNoTriggerGC();
}

/**************************************************************************************/
DEBUG_NOINLINE FCallCheck::FCallCheck(const char *szFile, int lineNum) : ForbidGC(szFile, lineNum)
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

#ifdef _DEBUG
    unbreakableLockCount = m_pThread->GetUnbreakableLockCount();
#endif
    didGCPoll = false;
    notNeeded = false;
    startTicks = getCycleCount();
}

/**************************************************************************************/
DEBUG_NOINLINE FCallCheck::~FCallCheck()
{
    SCAN_SCOPE_END;

    // Confirm that we don't starve the GC or thread-abort.
    // Basically every control flow path through an FCALL must
    // to a poll.   If you hit the assert below, you can fix it by
    //
    // If you erect a HELPER_METHOD_FRAME, you can
    //
    //      Call    HELPER_METHOD_POLL()
    //      or use  HELPER_METHOD_FRAME_END_POLL

    _ASSERTE(unbreakableLockCount == m_pThread->GetUnbreakableLockCount() ||
             (!m_pThread->HasUnbreakableLock() && !m_pThread->HasThreadStateNC(Thread::TSNC_OwnsSpinLock)));

    if (notNeeded) {

        /*<TODO>    TODO, we want to actually measure the time to make certain we are not too far off

		unsigned delta  = unsigned(getCycleCount() - startTicks);
        </TODO>*/
    }
    else if (!didGCPoll) {
        // <TODO>TODO turn this on!!! _ASSERTE(!"FCALL without a GC poll in it somewhere!");</TODO>
    }

}


#if defined(TARGET_AMD64)


FCallTransitionState::FCallTransitionState ()
{
    WRAPPER_NO_CONTRACT;

    m_pThread = GetThread();
    m_pPreviousHelperMethodFrameCallerList = m_pThread->m_pHelperMethodFrameCallerList;
    m_pThread->m_pHelperMethodFrameCallerList = NULL;
}


FCallTransitionState::~FCallTransitionState ()
{
    WRAPPER_NO_CONTRACT;

    m_pThread->m_pHelperMethodFrameCallerList = m_pPreviousHelperMethodFrameCallerList;
}


PermitHelperMethodFrameState::PermitHelperMethodFrameState ()
{
    WRAPPER_NO_CONTRACT;

    m_pThread = GetThread();
    CONSISTENCY_CHECK_MSG((HelperMethodFrameCallerList*)-1 != m_pThread->m_pHelperMethodFrameCallerList,
                          "fcall entry point is missing a FCALL_TRANSITION_BEGIN or a FCIMPL\n");

    m_ListEntry.pCaller = m_pThread->m_pHelperMethodFrameCallerList;
    m_pThread->m_pHelperMethodFrameCallerList = &m_ListEntry;
}


PermitHelperMethodFrameState::~PermitHelperMethodFrameState ()
{
    WRAPPER_NO_CONTRACT;

    m_pThread->m_pHelperMethodFrameCallerList = m_ListEntry.pCaller;
}


VOID PermitHelperMethodFrameState::CheckHelperMethodFramePermitted ()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    } CONTRACTL_END;

    //
    // Get current context and unwind to caller
    //

    CONTEXT ctx;

    ClrCaptureContext(&ctx);
    Thread::VirtualUnwindCallFrame(&ctx);

    //
    // Make sure each unmanaged frame used PERMIT_HELPER_METHOD_FRAME_BEGIN.
    // If we hit NULL before we reach managed code, then the caller of the
    // fcall was not managed.
    //

    Thread *pThread = GetThread();
    HelperMethodFrameCallerList *pList = pThread->m_pHelperMethodFrameCallerList;
    PCODE CurrentIP;
    TADDR CurrentSP;

    do
    {
        CurrentSP = GetSP(&ctx);
        CurrentIP = GetIP(&ctx);

        Thread::VirtualUnwindCallFrame(&ctx);

        TADDR CallerSP = GetSP(&ctx);

        unsigned nAssociatedListEntries = 0;

        while (   (SIZE_T)pList >= (SIZE_T)CurrentSP
               && (SIZE_T)pList <  (SIZE_T)CallerSP)
        {
            nAssociatedListEntries++;
            pList = pList->pCaller;
        }

        if (!nAssociatedListEntries)
        {
            char szFunction[cchMaxAssertStackLevelStringLen];
            GetStringFromAddr((DWORD_PTR)CurrentIP, szFunction);

            CONSISTENCY_CHECK_MSGF(false, ("Unmanaged caller %s at sp %p/ip %p is missing a "
                                           "PERMIT_HELPER_METHOD_FRAME_BEGIN, or this function "
                                           "is calling an fcall entry point that is missing a "
                                           "FCALL_TRANSITION_BEGIN or a FCIMPL\n", szFunction, CurrentSP, CurrentIP));
        }
    }
    while (pList && !ExecutionManager::IsManagedCode(GetIP(&ctx)));

    //
    // We should have exhausted the list.  If not, the list was not reset at
    // the transition from managed code.
    //

    if (pList)
    {
        char szFunction[cchMaxAssertStackLevelStringLen];
        GetStringFromAddr((DWORD_PTR)CurrentIP, szFunction);

        CONSISTENCY_CHECK_MSGF(false, ("fcall entry point %s at sp %p/ip %p is missing a "
                                       "FCALL_TRANSITION_BEGIN or a FCIMPL\n", szFunction, CurrentSP, CurrentIP));
    }
}


CompletedFCallTransitionState::CompletedFCallTransitionState ()
{
    WRAPPER_NO_CONTRACT;

    Thread *pThread = GetThread();
    m_pLastHelperMethodFrameCallerList = pThread->m_pHelperMethodFrameCallerList;
    pThread->m_pHelperMethodFrameCallerList = (HelperMethodFrameCallerList*)-1;
}


CompletedFCallTransitionState::~CompletedFCallTransitionState ()
{
    WRAPPER_NO_CONTRACT;

    Thread *pThread = GetThread();
    pThread->m_pHelperMethodFrameCallerList = m_pLastHelperMethodFrameCallerList;
}


#endif // TARGET_AMD64

#endif // ENABLE_CONTRACTS
