// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// 
// ==--==
//

//
//-----------------------------------------------------------------------------
// Stack Probe Header for inline functions
// Used to setup stack guards
//-----------------------------------------------------------------------------
#ifndef __STACKPROBE_inl__
#define __STACKPROBE_inl__

#include "stackprobe.h"
#include "common.h"

#if defined(FEATURE_STACK_PROBE) && !defined(DACCESS_COMPILE)

// want to inline in retail, but out of line into stackprobe.cpp in debug
#if !defined(_DEBUG) || defined(INCLUDE_RETAIL_STACK_PROBE)

#ifndef _DEBUG
#define INLINE_NONDEBUG_ONLY FORCEINLINE
#else
#define INLINE_NONDEBUG_ONLY
#endif

INLINE_NONDEBUG_ONLY BOOL ShouldProbeOnThisThread()
{
    // we only want to probe on user threads, not any of our special threads
    return GetCurrentTaskType() == TT_USER;
}

#if defined(_DEBUG) && defined(STACK_GUARDS_DEBUG)

DEBUG_NOINLINE void DebugSOTolerantTransitionHandler::EnterSOTolerantCode(Thread *pThread) 
{
    SCAN_SCOPE_BEGIN;
    ANNOTATION_FN_SO_TOLERANT;

    if (pThread)
    {
        m_clrDebugState = pThread->GetClrDebugState();
    }
    else
    {
        m_clrDebugState = GetClrDebugState();
    }
    if (m_clrDebugState)
        m_prevSOTolerantState = m_clrDebugState->BeginSOTolerant();
}

DEBUG_NOINLINE void DebugSOTolerantTransitionHandler::ReturnFromSOTolerantCode()
{
    SCAN_SCOPE_END;

    if (m_clrDebugState)
        m_clrDebugState->SetSOTolerance(m_prevSOTolerantState);
}

#endif

// Keep the main body out of line to keep code size down.
NOINLINE BOOL RetailStackProbeNoThrowWorker(unsigned int n, Thread *pThread);
NOINLINE void RetailStackProbeWorker(unsigned int n, Thread *pThread);

INLINE_NONDEBUG_ONLY 
BOOL RetailStackProbeNoThrow(unsigned int n, Thread *pThread)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

#ifdef STACK_GUARDS_RELEASE
    if(!IsStackProbingEnabled())
    {
        return TRUE;
    }
#endif

    return RetailStackProbeNoThrowWorker(n, pThread);
}

INLINE_NONDEBUG_ONLY 
void RetailStackProbe(unsigned int n, Thread *pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

#ifdef STACK_GUARDS_RELEASE
    if(!IsStackProbingEnabled())
    {
        return;
    }
#endif

    if (RetailStackProbeNoThrowWorker(n, pThread))
    {
        return;
    }
    ReportStackOverflow();
}

INLINE_NONDEBUG_ONLY 
void RetailStackProbe(unsigned int n)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

#ifdef STACK_GUARDS_RELEASE
    if(!IsStackProbingEnabled())
    {
        return;
    }
#endif

    if (RetailStackProbeNoThrowWorker(n, GetThread()))
    {
        return;
    }
    ReportStackOverflow();
}

#endif
#endif


#endif  // __STACKPROBE_inl__
