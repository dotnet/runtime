// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef _MSC_VER
// a workaround to prevent tls_CurrentThread from becoming dynamically checked/initialized.
EXTERN_C __declspec(selectany) __declspec(thread) RuntimeThreadLocals tls_CurrentThread;
#else
EXTERN_C __thread RuntimeThreadLocals tls_CurrentThread;
#endif

// static
inline Thread * ThreadStore::RawGetCurrentThread()
{
    return (Thread *) &tls_CurrentThread;
}

// static
inline Thread * ThreadStore::GetCurrentThread()
{
    Thread * pCurThread = RawGetCurrentThread();

    // If this assert fires, and you only need the Thread pointer if the thread has ever previously
    // entered the runtime, then you should be using GetCurrentThreadIfAvailable instead.
    ASSERT(pCurThread->IsInitialized());
    return pCurThread;
}

// static
inline Thread * ThreadStore::GetCurrentThreadIfAvailable()
{
    Thread * pCurThread = RawGetCurrentThread();
    if (pCurThread->IsInitialized())
        return pCurThread;

    return NULL;
}

EXTERN_C volatile uint32_t RhpTrapThreads;

// static
inline bool ThreadStore::IsTrapThreadsRequested()
{
    return (RhpTrapThreads & (uint32_t)TrapThreadsFlags::TrapThreads) != 0;
}
