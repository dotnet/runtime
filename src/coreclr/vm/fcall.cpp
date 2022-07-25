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

NOINLINE LPVOID __FCThrow(LPVOID __me, RuntimeExceptionKind reKind, UINT resID, LPCWSTR arg1, LPCWSTR arg2, LPCWSTR arg3)
{
    STATIC_CONTRACT_THROWS;
    // This isn't strictly true... But the guarantee that we make here is
    // that we won't trigger without having setup a frame.
    // STATIC_CONTRACT_TRIGGER
    STATIC_CONTRACT_GC_NOTRIGGER;

    // side effect the compiler can't remove
    if (FC_NO_TAILCALL != 1)
        return (LPVOID)(SIZE_T)(FC_NO_TAILCALL + 1);

    FC_CAN_TRIGGER_GC();
    INCONTRACT(FCallCheck __fCallCheck(__FILE__, __LINE__));
    FC_GC_POLL_NOT_NEEDED();

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_NOPOLL(Frame::FRAME_ATTR_CAPTURE_DEPTH_2);
    // Now, we can construct & throw.

    // In V1, throwing an ExecutionEngineException actually never really threw anything... its was the same as a
    // fatal error in the runtime, and we will most probably would have ripped the process down. Starting in
    // Whidbey, this behavior has changed a lot. Its not really legal to try to throw an
    // ExecutionEngineException with this function.
    _ASSERTE((reKind != kExecutionEngineException) ||
             !"Don't throw kExecutionEngineException from here. Go to EEPolicy directly, or throw something better.");

    if (resID == 0)
    {
        // If we have an string to add use NonLocalized otherwise just throw the exception.
        if (arg1)
            COMPlusThrowNonLocalized(reKind, arg1); //COMPlusThrow(reKind,arg1);
        else
            COMPlusThrow(reKind);
    }
    else
        COMPlusThrow(reKind, resID, arg1, arg2, arg3);

    HELPER_METHOD_FRAME_END();
    FC_CAN_TRIGGER_GC_END();
    _ASSERTE(!"Throw returned");
    return NULL;
}

NOINLINE LPVOID __FCThrowArgument(LPVOID __me, RuntimeExceptionKind reKind, LPCWSTR argName, LPCWSTR resourceName)
{
    STATIC_CONTRACT_THROWS;
    // This isn't strictly true... But the guarantee that we make here is
    // that we won't trigger without having setup a frame.
    // STATIC_CONTRACT_TRIGGER
    STATIC_CONTRACT_GC_NOTRIGGER;

    // side effect the compiler can't remove
    if (FC_NO_TAILCALL != 1)
        return (LPVOID)(SIZE_T)(FC_NO_TAILCALL + 1);

    FC_CAN_TRIGGER_GC();
    INCONTRACT(FCallCheck __fCallCheck(__FILE__, __LINE__));
    FC_GC_POLL_NOT_NEEDED();     // throws always open up for GC

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_NOPOLL(Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    switch (reKind) {
        case kArgumentNullException:
            if (resourceName) {
                COMPlusThrowArgumentNull(argName, resourceName);
            } else {
                COMPlusThrowArgumentNull(argName);
            }
            break;

        case kArgumentOutOfRangeException:
            COMPlusThrowArgumentOutOfRange(argName, resourceName);
            break;

        case kArgumentException:
            COMPlusThrowArgumentException(argName, resourceName);
            break;

        default:
            // If you see this assert, add a case for your exception kind above.
            _ASSERTE(argName == NULL);
            COMPlusThrow(reKind, resourceName);
    }

    HELPER_METHOD_FRAME_END();
    FC_CAN_TRIGGER_GC_END();
    _ASSERTE(!"Throw returned");
    return NULL;
}

/**************************************************************************************/
/* erect a frame in the FCALL and then poll the GC, objToProtect will be protected
   during the poll and the updated object returned.  */

NOINLINE Object* FC_GCPoll(void* __me, Object* objToProtect)
{
    CONTRACTL {
        THROWS;
        // This isn't strictly true... But the guarantee that we make here is
        // that we won't trigger without having setup a frame.
        UNCHECKED(GC_NOTRIGGER);
    } CONTRACTL_END;

    FC_CAN_TRIGGER_GC();
    INCONTRACT(FCallCheck __fCallCheck(__FILE__, __LINE__));

    Thread  *thread = GetThread();
    if (thread->CatchAtSafePointOpportunistic())    // Does someone want this thread stopped?
    {
        HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objToProtect);

#ifdef _DEBUG
        BOOL GCOnTransition = FALSE;
        if (g_pConfig->FastGCStressLevel()) {
            GCOnTransition = GC_ON_TRANSITIONS (FALSE);
        }
#endif
        CommonTripThread();
#ifdef _DEBUG
        if (g_pConfig->FastGCStressLevel()) {
            GC_ON_TRANSITIONS (GCOnTransition);
        }
#endif

        HELPER_METHOD_FRAME_END();
    }

    FC_CAN_TRIGGER_GC_END();

    return objToProtect;
}

#ifdef ENABLE_CONTRACTS

/**************************************************************************************/
#if defined(TARGET_X86) && defined(ENABLE_PERF_COUNTERS)
static __int64 getCycleCount() {

    LIMITED_METHOD_CONTRACT;
    return GET_CYCLE_COUNT();
}
#else
static __int64 getCycleCount() { LIMITED_METHOD_CONTRACT; return(0); }
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
    //
    // If you don't have a helper frame you can used
    //
    //      FC_GC_POLL_AND_RETURN_OBJREF        or
    //      FC_GC_POLL                          or
    //      FC_GC_POLL_RET
    //
    // Note that these must be at GC safe points.  In particular
    // all object references that are NOT protected will be trashed.


    // There is a special poll called FC_GC_POLL_NOT_NEEDED
    // which says the code path is short enough that a GC poll is not need
    // you should not use this in most cases.

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
