// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ---------------------------------------------------------------------------
// CLREx.h
// ---------------------------------------------------------------------------


#ifndef _CLREX_INL_
#define _CLREX_INL_

inline CLRException::HandlerState::HandlerState(Thread * pThread)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    m_pThread = pThread;
    if (m_pThread == NULL)
    {
        m_pFrame = NULL;
        m_fPreemptiveGCDisabled = FALSE;
    }
    else
    {
        m_pFrame = m_pThread->GetFrame();
        m_fPreemptiveGCDisabled = m_pThread->PreemptiveGCDisabled();
    }
}

inline CLRException::HandlerState::HandlerState(Thread * pThread, CLRException::HandlerState::NonNullThread dummy)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    _ASSERTE(pThread != NULL);

    m_pThread = pThread;
    m_pFrame = m_pThread->GetFrame();
    m_fPreemptiveGCDisabled = m_pThread->PreemptiveGCDisabled();
}


#endif // _CLREX_INL_
