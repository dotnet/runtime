// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "vars.hpp"
#include "fcall.h"
#include "excep.h"
#include "frames.h"
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
    // to a poll.

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

#endif // ENABLE_CONTRACTS
