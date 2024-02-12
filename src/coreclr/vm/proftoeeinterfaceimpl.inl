// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: ProfToEEInterfaceImpl.inl
//
// Inline implementation of portions of the code used to help implement the
// ICorProfilerInfo* interfaces, which allow the Profiler to communicate with the EE.
//

//
// ======================================================================================

#ifndef __PROFTOEEINTERFACEIMPL_INL__
#define __PROFTOEEINTERFACEIMPL_INL__

#ifdef PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
// Helpers


//---------------------------------------------------------------------------------------
//
// "Callback flags" are typically set on the current EE Thread object just before we
// call into a profiler (see SetCallbackStateFlagsHolder).  This helps us remember that
// we deliberately called into the profiler, as opposed to the profiler gaining control
// by hijacking a thread. This helper function is used in PROFILER_TO_CLR_ENTRYPOINT_SYNC
// to test the flags in order to authorize a profiler's call into us.  The macro is
// placed at the top of any call that's supposed to be synchronous-only.  If no flags are
// set, that implies the profiler hijacked the thread, so we reject the call.  In
// contrast, PROFILER_TO_CLR_ENTRYPOINT_ASYNC does NOT call this helper function, and
// thus deliberately allows the hijacked thread to continue calling back into the runtime.
//
// Arguments:
//      dwFlags - Flags to test
//
// Return Value:
//      If no EE Thread object: nonzero
//      If EE Thread object AND any of the specified flags are set on it: nonzero
//      Else zero (FALSE)
//

inline BOOL AreCallbackStateFlagsSet(DWORD dwFlags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    Thread * pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        // Not a managed thread; profiler can do whatever it wants
        return TRUE;
    }

    BOOL fRet;
    DWORD dwProfilerCallbackFullStateFlags = pThread->GetProfilerCallbackFullState();
    if (((dwProfilerCallbackFullStateFlags & COR_PRF_CALLBACKSTATE_FORCEGC_WAS_CALLED) != 0)
        || ((dwProfilerCallbackFullStateFlags & COR_PRF_CALLBACKSTATE_REJIT_WAS_CALLED) != 0))
    {
        // Threads on which ForceGC() or RequestReJIT() was successfully called should be treated just
        // like native threads.  Profiler can do whatever it wants
        return TRUE;
    }

    fRet = ((dwProfilerCallbackFullStateFlags & dwFlags) == dwFlags);
    return fRet;
}


//---------------------------------------------------------------------------------------
//
// Simple helper that returns nonzero iff the currently-executing function was called
// asynchronously (i.e., from outside of a callback, as hijacking profilers do)
//

inline BOOL IsCalledAsynchronously()
{
    LIMITED_METHOD_CONTRACT;
    return !(AreCallbackStateFlagsSet(COR_PRF_CALLBACKSTATE_INCALLBACK));
}


//---------------------------------------------------------------------------------------
//
// Simple helper that decides whether we should avoid calling into the host. Generally,
// host calls should be avoided if the current Info method was called asynchronously
// (i.e., from an F1-style hijack), for fear of re-entering the host (mainly SQL).
//
// Server GC threads are native (non-EE) threads, which therefore do not track enough
// state for us to determine if a call is made asynhronously on those threads. So we
// pessimistically assume that the current call on a server GC thread is from a hijack
// for the purposes of determining whether we may enter the host. Reasoning for this:
//     * SQL enables server-mode GC
//     * server GC threads are responsible for performing runtime suspension, and thus
//         call Thread::SuspendThread() which yields/sleeps and thus enters the host. So
//         server GC threads are examples of non-EE Threads that actually do spend time
//         in the host (this otherwise almost never happens for other non-EE threads).
//     * In spite of this pessimism, the effect on the profiler should be minimal. The
//         host calls we're avoiding are from the code manager's lock, which:
//             * a) Is only used when doing stack walks or translating IPs to functions
//             * b) Is only affected if it tries to yield/sleep when the code manager
//                 writer lock is taken, and that happens for incredibly tiny windows of
//                 time.
//

inline BOOL ShouldAvoidHostCalls()
{
    LIMITED_METHOD_CONTRACT;

    return
    (
        IsCalledAsynchronously() ||
        (
            (GetThreadNULLOk() == NULL) && IsGCSpecialThread()
        )
    );
}


//---------------------------------------------------------------------------------------
//
// Simple helper that returns nonzero iff the current thread is a non-EE thread in the
// process of doing a GC
//

inline BOOL NativeThreadInGC()
{
    LIMITED_METHOD_CONTRACT;
    return ((g_profControlBlock.fGCInProgress) && (GetThreadNULLOk() == NULL));
}

//---------------------------------------------------------------------------------------
//
// ProfToEE functions can use these overloads to determine whether a Thread should be
// visible to a profiler and thus be suitable for querying information about, by a
// profiler. If the Thread is non-NULL and is NOT a GCSpecial thread, then it's
// considered "managed", and is thus visible to the profiler.
//
// Arguments:
//      pThread or threadId - Thread to check
//
// Return Value:
//      nonzero iff the thread can run managed code

// Notes:
//      See code:Thread::m_fGCSpecial for more information
//

inline BOOL IsManagedThread(Thread * pThread)
{
    LIMITED_METHOD_CONTRACT;
    return ((pThread != NULL) && (!pThread->IsGCSpecial()));
}

inline BOOL IsManagedThread(ThreadID threadId)
{
    LIMITED_METHOD_CONTRACT;
    return IsManagedThread(reinterpret_cast<Thread *>(threadId));
}

//---------------------------------------------------------------------------------------
//
// ProfToEEInterfaceImpl ctor.
//

inline ProfToEEInterfaceImpl::ProfToEEInterfaceImpl()
{
    LIMITED_METHOD_CONTRACT;
};


inline BOOL IsClassOfMethodTableInited(MethodTable * pMethodTable)
{
    LIMITED_METHOD_CONTRACT;

    return ((pMethodTable->GetModuleForStatics() != NULL) &&
        (pMethodTable->GetDomainLocalModule() != NULL) &&
        pMethodTable->IsClassInited());
}


#endif // PROFILING_SUPPORTED

#endif // __PROFTOEEINTERFACEIMPL_INL__
