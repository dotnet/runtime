// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "common.h"
#include "simplerwlock.hpp"

BOOL SimpleRWLock::TryEnterRead()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

#ifdef _DEBUG
    PreEnter();
#endif //_DEBUG

    LONG RWLock;

    do {
        RWLock = m_RWLock;
        if( RWLock == -1 ) return FALSE;
        _ASSERTE (RWLock >= 0);
    } while( RWLock != InterlockedCompareExchange( &m_RWLock, RWLock+1, RWLock ));

    EE_LOCK_TAKEN(this);

#ifdef _DEBUG
    PostEnter();
#endif //_DEBUG

    return TRUE;
}

//=====================================================================
void SimpleRWLock::EnterRead()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    // Custom contract is needed for PostEnter()'s unscoped GC_NoTrigger counter change
#ifdef ENABLE_CONTRACTS_IMPL
    CheckGCNoTrigger();
#endif //ENABLE_CONTRACTS_IMPL

    GCX_MAYBE_PREEMP(m_gcMode == PREEMPTIVE);

#ifdef _DEBUG
    PreEnter();
#endif //_DEBUG

    DWORD dwSwitchCount = 0;

    while (TRUE)
    {
        // prevent writers from being starved. This assumes that writers are rare and
        // dont hold the lock for a long time.
        while (IsWriterWaiting())
        {
            int spinCount = m_spinCount;
            if (spinCount > 0) {
                YieldProcessorNormalizedForPreSkylakeCount(spinCount);
            }
            __SwitchToThread(0, ++dwSwitchCount);
        }

        if (TryEnterRead())
        {
            return;
        }

        DWORD i = g_SpinConstants.dwInitialDuration;
        do
        {
            if (TryEnterRead())
            {
                return;
            }

            if (g_SystemInfo.dwNumberOfProcessors <= 1)
            {
                break;
            }

            // Delay by approximately 2*i clock cycles (Pentium III).
            YieldProcessorNormalizedForPreSkylakeCount(i);

            // exponential backoff: wait a factor longer in the next iteration
            i *= g_SpinConstants.dwBackoffFactor;
        }
        while (i < g_SpinConstants.dwMaximumDuration);

        __SwitchToThread(0, ++dwSwitchCount);
    }
}

//=====================================================================
BOOL SimpleRWLock::TryEnterWrite()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

#ifdef _DEBUG
    PreEnter();
#endif //_DEBUG

    LONG RWLock = InterlockedCompareExchange( &m_RWLock, -1, 0 );

    _ASSERTE (RWLock >= 0 || RWLock == -1);

    if( RWLock ) {
        return FALSE;
    }

    EE_LOCK_TAKEN(this);

#ifdef _DEBUG
    PostEnter();
#endif //_DEBUG

    ResetWriterWaiting();

    return TRUE;
}

//=====================================================================
void SimpleRWLock::EnterWrite()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    // Custom contract is needed for PostEnter()'s unscoped GC_NoTrigger counter change
#ifdef ENABLE_CONTRACTS_IMPL
    CheckGCNoTrigger();
#endif //ENABLE_CONTRACTS_IMPL

    GCX_MAYBE_PREEMP(m_gcMode == PREEMPTIVE);

#ifdef _DEBUG
    PreEnter();
#endif //_DEBUG

    BOOL set = FALSE;

    DWORD dwSwitchCount = 0;

    while (TRUE)
    {
        if (TryEnterWrite())
        {
            return;
        }

        // set the writer waiting word, if not already set, to notify potential
        // readers to wait. Remember, if the word is set, so it can be reset later.
        if (!IsWriterWaiting())
        {
            SetWriterWaiting();
            set = TRUE;
        }

        DWORD i = g_SpinConstants.dwInitialDuration;
        do
        {
            if (TryEnterWrite())
            {
                return;
            }

            if (g_SystemInfo.dwNumberOfProcessors <= 1)
            {
                break;
            }

            // Delay by approximately 2*i clock cycles (Pentium III).
            YieldProcessorNormalizedForPreSkylakeCount(i);

            // exponential backoff: wait a factor longer in the next iteration
            i *= g_SpinConstants.dwBackoffFactor;
        }
        while (i < g_SpinConstants.dwMaximumDuration);

        __SwitchToThread(0, ++dwSwitchCount);
    }
}

#ifdef ENABLE_CONTRACTS_IMPL
//=========================================================================
// Asserts if lock mode is PREEMPTIVE and thread in a GC_NOTRIGGER contract
//=========================================================================
void SimpleRWLock::CheckGCNoTrigger()
{
    STATIC_CONTRACT_NOTHROW;

    // On PREEMPTIVE locks we'll toggle the GC mode, so we better not be in a GC_NOTRIGGERS region
    if (m_gcMode == PREEMPTIVE)
    {
        ClrDebugState *pClrDebugState = CheckClrDebugState();
        if (pClrDebugState)
        {
            if (pClrDebugState->GetGCNoTriggerCount())
            {
                // If we have no thread object, we won't be toggling the GC.  This is the case,
                // for example, on the debugger helper thread which is always GC_NOTRIGGERS.
                if (GetThreadNULLOk() != NULL)
                {
                    if (!( (GCViolation|BadDebugState) & pClrDebugState->ViolationMask()))
                    {
                        CONTRACT_ASSERT("You cannot enter a lock in a GC_NOTRIGGER region.",
                                        Contract::GC_NoTrigger,
                                        Contract::GC_Mask,
                                        __FUNCTION__,
                                        __FILE__,
                                        __LINE__);
                    }
                }
            }

            // The mode checks and enforcement of GC_NOTRIGGER during the lock are done in SimpleRWLock::PostEnter().
        }
    }
}
#endif  //ENABLE_CONTRACTS_IMPL

#ifdef _DEBUG
//=====================================================================
// GC mode assertions before acquiring a lock based on its mode.
//=====================================================================
void SimpleRWLock::PreEnter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    if (m_gcMode == PREEMPTIVE)
        _ASSERTE(!GetThreadNULLOk() || !GetThread()->PreemptiveGCDisabled());
    else if (m_gcMode == COOPERATIVE)
        _ASSERTE(!GetThreadNULLOk() || GetThread()->PreemptiveGCDisabled());
}

//=====================================================================
// GC checks after lock acquisition for avoiding deadlock scenarios.
//=====================================================================
void SimpleRWLock::PostEnter()
{
    WRAPPER_NO_CONTRACT;

    if ((m_gcMode == COOPERATIVE) || (m_gcMode == COOPERATIVE_OR_PREEMPTIVE))
    {
        Thread * pThread = GetThreadNULLOk();
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
            InterlockedIncrement(&m_countNoTriggerGC);
            INCONTRACT(pThread->BeginNoTriggerGC(__FILE__, __LINE__));
        }
    }
}

//=====================================================================
// GC checks before lock release for avoiding deadlock scenarios.
//=====================================================================
void SimpleRWLock::PreLeave()
{
    WRAPPER_NO_CONTRACT;

    if (m_countNoTriggerGC > 0)
    {
        DWORD countNoTriggerGC = InterlockedDecrement(&m_countNoTriggerGC);
        _ASSERTE(countNoTriggerGC >= 0);

        Thread * pThread = GetThreadNULLOk();
        if (pThread != NULL)
        {
            INCONTRACT(pThread->EndNoTriggerGC());
        }
    }
}
#endif //_DEBUG
