// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "event.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "rhassert.h"
#include "slist.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "Crst.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

//
// -----------------------------------------------------------------------------------------------------------
//
// CLR wrapper around native events. This version does not support host interception.
//

bool CLREventStatic::CreateManualEventNoThrow(bool bInitialState)
{
    m_hEvent = PAL_CreateEvent(NULL, true, bInitialState);
    m_fInitialized = true;
    return IsValid();
}

bool CLREventStatic::CreateAutoEventNoThrow(bool bInitialState)
{
    m_hEvent = PAL_CreateEvent(NULL, false, bInitialState);
    m_fInitialized = true;
    return IsValid();
}

bool CLREventStatic::CreateOSManualEventNoThrow(bool bInitialState)
{
    m_hEvent = PAL_CreateEvent(NULL, true, bInitialState);
    m_fInitialized = true;
    return IsValid();
}

bool CLREventStatic::CreateOSAutoEventNoThrow(bool bInitialState)
{
    m_hEvent = PAL_CreateEvent(NULL, false, bInitialState);
    m_fInitialized = true;
    return IsValid();
}

void CLREventStatic::CloseEvent()
{
    if (m_fInitialized && m_hEvent != NULL && m_hEvent != INVALID_HANDLE_VALUE)
    {
        PAL_CloseEvent(m_hEvent);
        m_hEvent = INVALID_HANDLE_VALUE;
    }
}

bool CLREventStatic::IsValid() const
{
    return m_fInitialized && m_hEvent != NULL && m_hEvent != INVALID_HANDLE_VALUE;
}

bool CLREventStatic::Set()
{
    if (!m_fInitialized)
        return false;
    return PAL_SetEvent(m_hEvent);
}

bool CLREventStatic::Reset()
{
    if (!m_fInitialized)
        return false;
    return PAL_ResetEvent(m_hEvent);
}

uint32_t CLREventStatic::Wait(uint32_t dwMilliseconds, bool bAlertable, bool bAllowReentrantWait)
{
    uint32_t result = WAIT_FAILED;

    if (m_fInitialized)
    {
        bool        disablePreemptive = false;
        Thread *    pCurThread  = ThreadStore::GetCurrentThreadIfAvailable();

        if (NULL != pCurThread)
        {
            if (pCurThread->IsCurrentThreadInCooperativeMode())
            {
                pCurThread->EnablePreemptiveMode();
                disablePreemptive = true;
            }
        }

#ifdef TARGET_WINDOWS
        if (bAllowReentrantWait)
        {
            result = PalCompatibleWaitAny(bAlertable, dwMilliseconds, 1, &m_hEvent, TRUE);
        }
        else
#endif // TARGET_WINDOWS
        {
            result = PAL_WaitForMultipleObjectsEx(1, &m_hEvent, false, dwMilliseconds, bAlertable);
        }

        if (disablePreemptive)
        {
            pCurThread->DisablePreemptiveMode();
        }
    }

    return result;
}

HANDLE CLREventStatic::GetOSEvent()
{
    if (!m_fInitialized)
        return INVALID_HANDLE_VALUE;
    return m_hEvent;
}
