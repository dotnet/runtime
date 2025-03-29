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

    return pMethodTable->IsClassInited();
}


#endif // PROFILING_SUPPORTED

#endif // __PROFTOEEINTERFACEIMPL_INL__
