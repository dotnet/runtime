// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 

#include "common.h"

#include "corhost.h"
#include "synch.h"

void CLREventBase::CreateAutoEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;           
        GC_NOTRIGGER;
        SO_TOLERANT;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.Net uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));        
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    SetAutoEvent();

    {
        HANDLE h = UnsafeCreateEvent(NULL,FALSE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
    
}

BOOL CLREventBase::CreateAutoEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.Net uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE)); 
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateAutoEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CreateManualEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;           
        GC_NOTRIGGER;
        SO_TOLERANT;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.Net uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));        
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    {
        HANDLE h = UnsafeCreateEvent(NULL,TRUE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
}

BOOL CLREventBase::CreateManualEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.Net uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateManualEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CreateMonitorEvent(SIZE_T Cookie)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((g_fEEStarted));
        PRECONDITION((GetThread() != NULL));
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    // thread-safe SetAutoEvent
    FastInterlockOr(&m_dwFlags, CLREVENT_FLAGS_AUTO_EVENT);

    {
        HANDLE h = UnsafeCreateEvent(NULL,FALSE,FALSE,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        if (FastInterlockCompareExchangePointer(&m_handle,
                                                h,
                                                INVALID_HANDLE_VALUE) != INVALID_HANDLE_VALUE)
        {
            // We lost the race
            CloseHandle(h);
        }
    }
    
    // thread-safe SetInDeadlockDetection
    FastInterlockOr(&m_dwFlags, CLREVENT_FLAGS_IN_DEADLOCK_DETECTION);

    for (;;)
    {
        LONG oldFlags = m_dwFlags;

        if (oldFlags & CLREVENT_FLAGS_MONITOREVENT_ALLOCATED)
        {
            // Other thread has set the flag already. Nothing left for us to do.
            break;
        }

        LONG newFlags = oldFlags | CLREVENT_FLAGS_MONITOREVENT_ALLOCATED;
        if (FastInterlockCompareExchange((LONG*)&m_dwFlags, newFlags, oldFlags) != oldFlags)
        {
            // We lost the race
            continue;
        }

        // Because we set the allocated bit, we are the ones to do the signalling
        if (oldFlags & CLREVENT_FLAGS_MONITOREVENT_SIGNALLED)
        {
            // We got the honour to signal the event
            Set();
        }
        break;
    }
}


void CLREventBase::SetMonitorEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // SetMonitorEvent is robust against initialization races. It is possible to
    // call CLREvent::SetMonitorEvent on event that has not been initialialized yet by CreateMonitorEvent.
    // CreateMonitorEvent will signal the event once it is created if it happens.

    for (;;)
    {
        LONG oldFlags = m_dwFlags;

        if (oldFlags & CLREVENT_FLAGS_MONITOREVENT_ALLOCATED)
        {
            // Event has been allocated already. Use the regular codepath.
            Set();
            break;
        }

        LONG newFlags = oldFlags | CLREVENT_FLAGS_MONITOREVENT_SIGNALLED;
        if (FastInterlockCompareExchange((LONG*)&m_dwFlags, newFlags, oldFlags) != oldFlags)
        {
            // We lost the race
            continue;
        }
        break;
    }
}



void CLREventBase::CreateOSAutoEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;           
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));        
    }
    CONTRACTL_END;

    // Can not assert here. ASP.Net uses our Threadpool before EE is started.
    //_ASSERTE (g_fEEStarted);

    SetOSEvent();
    SetAutoEvent();

    HANDLE h = UnsafeCreateEvent(NULL,FALSE,bInitialState,NULL);
    if (h == NULL) {
        ThrowOutOfMemory();
    }
    m_handle = h;
}

BOOL CLREventBase::CreateOSAutoEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));        
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateOSAutoEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CreateOSManualEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;           
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));        
    }
    CONTRACTL_END;

    // Can not assert here. ASP.Net uses our Threadpool before EE is started.
    //_ASSERTE (g_fEEStarted);

    SetOSEvent();

    HANDLE h = UnsafeCreateEvent(NULL,TRUE,bInitialState,NULL);
    if (h == NULL) {
        ThrowOutOfMemory();
    }
    m_handle = h;
}

BOOL CLREventBase::CreateOSManualEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW; 
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateOSManualEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CloseEvent()
{
    CONTRACTL
    {
      NOTHROW;
      if (IsInDeadlockDetection()) {GC_TRIGGERS;} else {GC_NOTRIGGER;}
      SO_TOLERANT;
    }
    CONTRACTL_END;

    GCX_MAYBE_PREEMP(IsInDeadlockDetection() && IsValid());

    _ASSERTE(Thread::Debug_AllowCallout());

    if (m_handle != INVALID_HANDLE_VALUE) {
        {
            CloseHandle(m_handle);
        }

        m_handle = INVALID_HANDLE_VALUE;
    }
    m_dwFlags = 0;
}


BOOL CLREventBase::Set()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      SO_TOLERANT;
      PRECONDITION((m_handle != INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    _ASSERTE(Thread::Debug_AllowCallout());

    {    
        return UnsafeSetEvent(m_handle);
    }

}


BOOL CLREventBase::Reset()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      SO_TOLERANT;
      PRECONDITION((m_handle != INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    _ASSERTE(Thread::Debug_AllowCallout());

    // We do not allow Reset on AutoEvent
    _ASSERTE (!IsAutoEvent() ||
              !"Can not call Reset on AutoEvent");

    {
        return UnsafeResetEvent(m_handle);
    }
}


static DWORD CLREventWaitHelper2(HANDLE handle, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_SO_TOLERANT;
    
    return WaitForSingleObjectEx(handle,dwMilliseconds,alertable);
}

static DWORD CLREventWaitHelper(HANDLE handle, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SO_TOLERANT;
    
    struct Param
    {
        HANDLE handle;
        DWORD dwMilliseconds;
        BOOL alertable;
        DWORD result;
    } param;
    param.handle = handle;
    param.dwMilliseconds = dwMilliseconds;
    param.alertable = alertable;
    param.result = WAIT_FAILED;

    // Can not use EX_TRY/CATCH.  EX_CATCH toggles GC mode.  This function is called
    // through RareDisablePreemptiveGC.  EX_CATCH breaks profiler callback.
    PAL_TRY(Param *, pParam, &param)
    {
        // Need to move to another helper (cannot have SEH and C++ destructors
        // on automatic variables in one function)
        pParam->result = CLREventWaitHelper2(pParam->handle, pParam->dwMilliseconds, pParam->alertable);
    }
    PAL_EXCEPT (EXCEPTION_EXECUTE_HANDLER)
    {
        param.result = WAIT_FAILED;
    }
    PAL_ENDTRY;

    return param.result;
}


DWORD CLREventBase::Wait(DWORD dwMilliseconds, BOOL alertable, PendingSync *syncState) 
{
    WRAPPER_NO_CONTRACT;
    return WaitEx(dwMilliseconds, alertable?WaitMode_Alertable:WaitMode_None,syncState);
}


DWORD CLREventBase::WaitEx(DWORD dwMilliseconds, WaitMode mode, PendingSync *syncState) 
{
    BOOL alertable = (mode & WaitMode_Alertable)!=0;
    CONTRACTL
    {
        if (alertable)
        {
            THROWS;               // Thread::DoAppropriateWait can throw   
        }
        else
        {
            NOTHROW;
        }
        if (GetThread())
        {
            if (alertable)
                GC_TRIGGERS;
            else 
                GC_NOTRIGGER;
        }
        else
        {
            DISABLED(GC_TRIGGERS);        
        }
        SO_TOLERANT;
        PRECONDITION(m_handle != INVALID_HANDLE_VALUE); // Handle has to be valid
    }
    CONTRACTL_END;


    _ASSERTE(Thread::Debug_AllowCallout());

    Thread * pThread = GetThread();    
    
#ifdef _DEBUG
    // If a CLREvent is OS event only, we can not wait for the event on a managed thread
    if (IsOSEvent())
        _ASSERTE (pThread == NULL);
#endif
    _ASSERTE((pThread != NULL) || !g_fEEStarted || dbgOnly_IsSpecialEEThread());

    {
        if (pThread && alertable) {
            DWORD dwRet = WAIT_FAILED;
            BEGIN_SO_INTOLERANT_CODE_NOTHROW (pThread, return WAIT_FAILED;);
            dwRet = pThread->DoAppropriateWait(1, &m_handle, FALSE, dwMilliseconds, 
                                              mode, 
                                              syncState);
            END_SO_INTOLERANT_CODE;
            return dwRet;
        }
        else {
            _ASSERTE (syncState == NULL);
            return CLREventWaitHelper(m_handle,dwMilliseconds,alertable);
        }
    }
}

void CLRSemaphore::Create (DWORD dwInitial, DWORD dwMax)
{
    CONTRACTL
    {
      THROWS;
      GC_NOTRIGGER;
      SO_TOLERANT;
      PRECONDITION(m_handle == INVALID_HANDLE_VALUE);
    }
    CONTRACTL_END;

    {
        HANDLE h = UnsafeCreateSemaphore(NULL,dwInitial,dwMax,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
}


void CLRSemaphore::Close()
{
    LIMITED_METHOD_CONTRACT;

    if (m_handle != INVALID_HANDLE_VALUE) {
        CloseHandle(m_handle);
        m_handle = INVALID_HANDLE_VALUE;
    }
}

BOOL CLRSemaphore::Release(LONG lReleaseCount, LONG *lpPreviousCount)
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      SO_TOLERANT;
      PRECONDITION(m_handle != INVALID_HANDLE_VALUE);
    }
    CONTRACTL_END;

    {
        return ::UnsafeReleaseSemaphore(m_handle, lReleaseCount, lpPreviousCount);
    }
}


DWORD CLRSemaphore::Wait(DWORD dwMilliseconds, BOOL alertable)
{
    CONTRACTL
    {
        if (GetThread() && alertable)
        {
            THROWS;               // Thread::DoAppropriateWait can throw       
        }
        else
        {
            NOTHROW;
        }
        if (GetThread())
        {
            if (alertable)
                GC_TRIGGERS;
            else 
                GC_NOTRIGGER;
        }
        else
        {
            DISABLED(GC_TRIGGERS);        
        }
        SO_TOLERANT;
        PRECONDITION(m_handle != INVALID_HANDLE_VALUE); // Invalid to have invalid handle
    }
    CONTRACTL_END;

    
    Thread *pThread = GetThread();
    _ASSERTE (pThread || !g_fEEStarted || dbgOnly_IsSpecialEEThread());

    {
        // TODO wwl: if alertable is FALSE, do we support a host to break a deadlock?
        // Currently we can not call through DoAppropriateWait because of CannotThrowComplusException.
        // We should re-consider this after our code is exception safe.
        if (pThread && alertable) {
            return pThread->DoAppropriateWait(1, &m_handle, FALSE, dwMilliseconds, 
                                              alertable?WaitMode_Alertable:WaitMode_None,
                                              NULL);
        }
        else {
            DWORD result = WAIT_FAILED;
            EX_TRY
            {
                result = WaitForSingleObjectEx(m_handle,dwMilliseconds,alertable);
            }
            EX_CATCH
            {
                result = WAIT_FAILED;
            }
            EX_END_CATCH(SwallowAllExceptions);
            return result;
        }
    }
}

void CLRMutex::Create(LPSECURITY_ATTRIBUTES lpMutexAttributes, BOOL bInitialOwner, LPCTSTR lpName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION(m_handle == INVALID_HANDLE_VALUE && m_handle != NULL);
    }
    CONTRACTL_END;

    m_handle = WszCreateMutex(lpMutexAttributes,bInitialOwner,lpName);
    if (m_handle == NULL)
    {
        ThrowOutOfMemory();
    }
}

void CLRMutex::Close()
{
    LIMITED_METHOD_CONTRACT;

    if (m_handle != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_handle);
        m_handle = INVALID_HANDLE_VALUE;
    }
}

BOOL CLRMutex::Release()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      SO_TOLERANT;
      PRECONDITION(m_handle != INVALID_HANDLE_VALUE && m_handle != NULL);
    }
    CONTRACTL_END;

    BOOL fRet = ReleaseMutex(m_handle);
    if (fRet)
    {
        EE_LOCK_RELEASED(this);
    }
    return fRet;
}

DWORD CLRMutex::Wait(DWORD dwMilliseconds, BOOL bAlertable)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        PRECONDITION(m_handle != INVALID_HANDLE_VALUE && m_handle != NULL);
    }
    CONTRACTL_END;

    DWORD fRet = WaitForSingleObjectEx(m_handle, dwMilliseconds, bAlertable);

    if (fRet == WAIT_OBJECT_0)
    {
        EE_LOCK_TAKEN(this);
    }

    return fRet;
}
