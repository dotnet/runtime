// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: Canary.cpp
//

//
// Canary for debugger helper thread. This will sniff out if it's safe to take locks.
//
//*****************************************************************************

#include "stdafx.h"


//-----------------------------------------------------------------------------
// Ctor for HelperCanary class
//-----------------------------------------------------------------------------
HelperCanary::HelperCanary()
{
    m_hCanaryThread = NULL;
    m_CanaryThreadId = 0;
    m_RequestCounter = 0;
    m_AnswerCounter = 0;
    m_fStop = false;

    m_fCachedValid = false;
    m_fCachedAnswer = false;
    m_initialized = false;
}

//-----------------------------------------------------------------------------
// Dtor for class
//-----------------------------------------------------------------------------
HelperCanary::~HelperCanary()
{
    // Since we're deleting this memory, we need to kill the canary thread.
    m_fStop = true;
    SetEvent(m_hPingEvent);

    // m_hPingEvent dtor will close handle
    WaitForSingleObject(m_hCanaryThread, INFINITE);
}

//-----------------------------------------------------------------------------
// Clear the cached value for AreLocksAvailable();
//-----------------------------------------------------------------------------
void HelperCanary::ClearCache()
{
    _ASSERTE(ThisIsHelperThreadWorker());
    m_fCachedValid = false;
}

//-----------------------------------------------------------------------------
// The helper thread can call this to determine if it can safely take a certain
// set of locks (mainly the heap lock(s)). The canary thread will go off and
// try and take these and report back to the helper w/o ever blocking the
// helper.
//
// Returns 'true' if it's safe for helper to take locks; else false.
// We err on the side of safety (returning false).
//-----------------------------------------------------------------------------
bool HelperCanary::AreLocksAvailable()
{
    // If we're not on the helper thread, then we're guaranteed safe.
    // We check this to support MaybeHelperThread code.
    if (!ThisIsHelperThreadWorker())
    {
        return true;
    }

    if (m_fCachedValid)
    {
        return m_fCachedAnswer;
    }

    // Cache the answer.
    m_fCachedAnswer = AreLocksAvailableWorker();
    m_fCachedValid = true;

#ifdef _DEBUG
    // For managed-only debugging, we should always be safe.
    if (!g_pRCThread->GetDCB()->m_rightSideIsWin32Debugger)
    {
        _ASSERTE(m_fCachedAnswer || !"Canary returned false in Managed-debugger");
    }

    // For debug, nice to be able to enable an assert that tells us if this situation is actually happening.
    if (!m_fCachedAnswer)
    {
        static BOOL shouldBreak = -1;
        if (shouldBreak == -1)
        {
            shouldBreak = UnsafeGetConfigDWORD(CLRConfig::INTERNAL_DbgBreakIfLocksUnavailable);
        }
        if (shouldBreak)
        {
            _ASSERTE(!"Potential deadlock detected.\nLocks that the helper thread may need are currently held by other threads.");
        }
    }
#endif // _DEBUG

    return m_fCachedAnswer;
}

//-----------------------------------------------------------------------------
// Creates the canary thread and signaling events.
//-----------------------------------------------------------------------------
void HelperCanary::Init()
{
    // You can only run the init code once. The debugger attempts to lazy-init
    // the canary at several points but if the canary is already inited then
    // we just eagerly return. See issue 841005 for more details.
    if(m_initialized)
    {
        return;
    }
    else
    {
        m_initialized = true;
    }

    m_hPingEvent = WszCreateEvent(NULL, (BOOL) kAutoResetEvent, FALSE, NULL);
    if (m_hPingEvent == NULL)
    {
        STRESS_LOG1(LF_CORDB, LL_ALWAYS, "Canary failed to create ping event. gle=%d\n", GetLastError());
        // in the past if we failed to start the thread we just assumed it was unsafe
        // so I am preserving that behavior. However I am going to assert that this
        // doesn't really happen
        _ASSERTE(!"Canary failed to create ping event");
        return;
    }

    m_hWaitEvent = WszCreateEvent(NULL, (BOOL) kManualResetEvent, FALSE, NULL);
    if (m_hWaitEvent == NULL)
    {
        STRESS_LOG1(LF_CORDB, LL_ALWAYS, "Canary failed to create wait event. gle=%d\n", GetLastError());
        // in the past if we failed to start the thread we just assumed it was unsafe
        // so I am preserving that behavior. However I am going to assert that this
        // doesn't really happen
        _ASSERTE(!"Canary failed to create wait event");
        return;
    }

    // Spin up the canary. This will call dllmain, but that's ok because it just
    // degenerates to our timeout case.
    const DWORD flags = CREATE_SUSPENDED;
    m_hCanaryThread = CreateThread(NULL, 0,
        HelperCanary::ThreadProc, this,
        flags, &m_CanaryThreadId);

    // in the past if we failed to start the thread we just assumed it was unsafe
    // so I am preserving that behavior. However I am going to assert that this
    // doesn't really happen
    if(m_hCanaryThread == NULL)
    {
        _ASSERTE(!"CreateThread() failed to create Canary thread");
        return;
    }

    // Capture the Canary thread's TID so that the RS can mark it as a can't-stop region.
    // This is essential so that the RS doesn't view it as some external thread to be suspended when we hit
    // debug events.
    _ASSERTE(g_pRCThread != NULL);
    g_pRCThread->GetDCB()->m_CanaryThreadId = m_CanaryThreadId;

    ResumeThread(m_hCanaryThread);
}


//-----------------------------------------------------------------------------
// Does real work for AreLocksAvailable(), minus caching.
//-----------------------------------------------------------------------------
bool HelperCanary::AreLocksAvailableWorker()
{
#if _DEBUG
    // For debugging, allow a way to force the canary to fail, and thus test our
    // failure paths.
    static BOOL fShortcut= -1;
    if (fShortcut == -1)
    {
        fShortcut = UnsafeGetConfigDWORD(CLRConfig::INTERNAL_DbgShortcutCanary);
    }
    if (fShortcut == 1)
    {
        return false;
    }
    if (fShortcut == 2)
    {
        return true;
    }
#endif

    // We used to do lazy init but that is dangerous... CreateThread
    // allocates some memory which can block on a lock, exactly the
    // situation we are attempting to detect and not block on.
    // Instead we spin up the canary in advance and if that failed then
    // assume unsafe
    if(m_CanaryThreadId == 0)
    {
        _ASSERTE(!"We shouldn't be lazy initing the canary anymore");
        return false;
    }

    // Canary will take the locks of interest and then set the Answer counter equal to our request counter.
    m_RequestCounter = m_RequestCounter + 1;
    ResetEvent(m_hWaitEvent);
    SetEvent(m_hPingEvent);

    // Spin waiting for answer. If canary gets back to us, then the locks must be free and so it's safe for helper-thread.
    // If we timeout, then we err on the side of safety and assume canary blocked on a lock and so it's not safe
    // for the helper thread to take those locks.
    // We explicitly have a simple spin-wait instead of using win32 events because we want something simple and
    // provably correct. Since we already need the spin-wait for the counters, adding an extra win32 event
    // to get rid of the sleep would be additional complexity and race windows without a clear benefit.

    // We need to track what iteration of "AreLocksAvailable" the helper is on. Say canary sniffs two locks, now Imagine if:
    // 1) Helper calls AreLocksAvailable,
    // 2) the canary does get blocked on lock #1,
    // 3) process resumes, canary now gets + releases lock #1,
    // 4) another random thread takes lock #1
    // 5) then helper calls AreLocksAvailable again later
    // 6) then the canary finally finishes. Note it's never tested lock #1 on the 2nd iteration.
    // We don't want the canary's response initiated from the 1st request to impact the Helper's 2nd request.
    // Thus we keep a request / answer counter to make sure that the canary tests all locks on the same iteration.
    DWORD retry = 0;

    const DWORD msSleepSteadyState = 150; // sleep time in ms
    const DWORD maxRetry = 15; // number of times to try.
    DWORD msSleep = 80; // how much to sleep on first iteration.

    while(m_RequestCounter != m_AnswerCounter)
    {
        retry ++;
        if (retry > maxRetry)
        {
            STRESS_LOG0(LF_CORDB, LL_ALWAYS, "Canary timed out!\n");
            return false;
        }

        // We'll either timeout (in which case it's like a Sleep(), or
        // get the event, which shortcuts the sleep.
        WaitForSingleObject(m_hWaitEvent, msSleep);

        // In case a stale answer sets the wait event high, reset it now to avoid us doing
        // a live spin-lock.
        ResetEvent(m_hWaitEvent);


        msSleep = msSleepSteadyState;
    }

    // Canary made it on same Request iteration, so it must be safe!
    return true;
}

//-----------------------------------------------------------------------------
// Real OS thread proc for Canary thread.
// param - 'this' pointer for HelperCanary
// return value - meaningless, but threads need to return something.
//-----------------------------------------------------------------------------
DWORD HelperCanary::ThreadProc(LPVOID param)
{
    _ASSERTE(!ThisIsHelperThreadWorker());

    STRESS_LOG0(LF_CORDB, LL_ALWAYS, "Canary thread spun up\n");
    HelperCanary * pThis = reinterpret_cast<HelperCanary*> (param);
    pThis->ThreadProc();
    _ASSERTE(pThis->m_fStop);
    STRESS_LOG0(LF_CORDB, LL_ALWAYS, "Canary thread exiting\n");

    return 0;
}

//-----------------------------------------------------------------------------
// Real implementation of Canary Thread.
// Single canary thread is reused after creation.
//-----------------------------------------------------------------------------
void HelperCanary::ThreadProc()
{
    _ASSERTE(m_CanaryThreadId == GetCurrentThreadId());

    while(true)
    {
        WaitForSingleObject(m_hPingEvent, INFINITE);

        m_AnswerCounter = 0;
        DWORD dwRequest = m_RequestCounter;

        if (m_fStop)
        {
            return;
        }
        STRESS_LOG2(LF_CORDB, LL_ALWAYS, "stage:%d,req:%d", 0, dwRequest);

        // Now take the locks of interest. This could block indefinitely. If this blocks, we may even get multiple requests.
        TakeLocks();

        m_AnswerCounter = dwRequest;

        // Set wait event to let Requesting thread shortcut its spin lock. This is purely an
        // optimization because requesting thread will still check Answer/Request counters.
        // That protects us from recyling bugs.
        SetEvent(m_hWaitEvent);
    }
}

//-----------------------------------------------------------------------------
// Try and take locks.
//-----------------------------------------------------------------------------
void HelperCanary::TakeLocks()
{
    _ASSERTE(::GetThreadNULLOk() == NULL); // Canary Thread should always be outside the runtime.
    _ASSERTE(m_CanaryThreadId == GetCurrentThreadId());

    // Call new, which will take whatever standard heap locks there are.
    // We don't care about what memory we get; we just want to take the heap lock(s).
    DWORD * p = new (nothrow) DWORD();
    delete p;

    STRESS_LOG1(LF_CORDB, LL_ALWAYS, "canary stage:%d\n", 1);
}


