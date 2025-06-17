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
    // IF EH happens, this is still called, in which case
    // we should not bother

    if (m_pThread->RawGCNoTrigger())
        m_pThread->EndNoTriggerGC();
}

#endif // ENABLE_CONTRACTS
