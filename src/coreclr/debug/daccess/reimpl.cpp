// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: reimpl.cpp
//

//
// Data-access-specific reimplementations of standard code.
//
//*****************************************************************************

#include "stdafx.h"

//
// Get the Thread instance for a specific OS thread ID
//
// Arguments:
//     osThread - the OS thread ID of interest.
//
// Return value:
//     A Thread object marshalled from the target corresponding to the specified OS thread
//     ID, or NULL if there is no such Thread.
//
// Notes:
//     We used to accept a thread ID of '0' to mean "use the current thread", which was based on
//     ICLRDataTarget::GetCurrentThreadID.  But this is error-prone and not well-defined (many data targets
//     don't implement that API).  It's better to require explicit thread IDs to be passed down when they
//     are needed.
//
Thread* __stdcall
DacGetThread(ULONG32 osThread)
{
    _ASSERTE(osThread > 0);

    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    // Note that if we had access to TLS, we could get this at index gThreadTLSIndex for the specified
    // thread.  However, this is the only place we might want to use TLS, and it's not performance critical,
    // so we haven't added TLS support to ICorDebugDataTarget (the legacy ICLRDataTarget interface has it though)

    // Scan the whole thread store to see if there's a matching thread.

    if (!ThreadStore::s_pThreadStore)
    {
        return NULL;
    }

    Thread* thread = ThreadStore::s_pThreadStore->m_ThreadList.GetHead();
    while (thread)
    {
        if (thread->GetOSThreadId() == osThread)
        {
            return thread;
        }

        thread = ThreadStore::s_pThreadStore->m_ThreadList.GetNext(thread);
    }

    return NULL;
}

Thread* GetThread()
{
    // In dac mode it's unlikely that the thread calling dac
    // is actually the same "current thread" that the runtime cares
    // about.  Fail all queries of the current thread by
    // the runtime code to catch any inadvertent usage.
    // Enumerating the ThreadStore is the proper way to get access
    // to specific Thread objects.
    DacError(E_UNEXPECTED);
    return NULL;
}

Thread* GetThreadNULLOk()
{
    return GetThread();
}

BOOL
DacGetThreadContext(Thread* thread, T_CONTEXT* context)
{
    SUPPORTS_DAC;

    if (!g_dacImpl)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    // XXX Microsoft - How do you retrieve the context for
    // a Thread that's not running?
    if (!thread->GetOSThreadId() ||
        thread->GetOSThreadId() == 0xbaadf00d)
    {
        DacError(E_UNEXPECTED);
        UNREACHABLE();
    }

    ULONG32 contextFlags;

    contextFlags = DT_CONTEXT_ALL;

    HRESULT status =
        g_dacImpl->m_pTarget->
        GetThreadContext(thread->GetOSThreadId(), contextFlags,
                         sizeof(DT_CONTEXT), (PBYTE)context);
    if (status != S_OK)
    {
        DacError(status);
        UNREACHABLE();
    }

    return TRUE;
}

