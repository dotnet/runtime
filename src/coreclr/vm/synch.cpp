// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    {
        HANDLE h = CreateEvent(NULL,FALSE,bInitialState,NULL);
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
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateAutoEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH

    return IsValid();
}

void CLREventBase::CreateManualEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    {
        HANDLE h = CreateEvent(NULL,TRUE,bInitialState,NULL);
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
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateManualEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH

    return IsValid();
}

void CLREventBase::CloseEvent()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(Thread::Debug_AllowCallout());

    if (m_handle != INVALID_HANDLE_VALUE) {
        {
            CloseHandle(m_handle);
        }

        m_handle = INVALID_HANDLE_VALUE;
    }
}


BOOL CLREventBase::Set()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      PRECONDITION((m_handle != INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    _ASSERTE(Thread::Debug_AllowCallout());

    {
        return SetEvent(m_handle);
    }

}


BOOL CLREventBase::Reset()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      PRECONDITION((m_handle != INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    _ASSERTE(Thread::Debug_AllowCallout());

    {
        return ResetEvent(m_handle);
    }
}


static DWORD CLREventWaitHelper2(HANDLE handle, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_THROWS;

    return WaitForSingleObjectEx(handle,dwMilliseconds,alertable);
}

static DWORD CLREventWaitHelper(HANDLE handle, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_NOTHROW;

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


DWORD CLREventBase::Wait(DWORD dwMilliseconds, BOOL alertable)
{
    WRAPPER_NO_CONTRACT;
    return WaitEx(dwMilliseconds, alertable?WaitMode_Alertable:WaitMode_None);
}


DWORD CLREventBase::WaitEx(DWORD dwMilliseconds, WaitMode mode)
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
        if (GetThreadNULLOk())
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
        PRECONDITION(m_handle != INVALID_HANDLE_VALUE); // Handle has to be valid
    }
    CONTRACTL_END;


    _ASSERTE(Thread::Debug_AllowCallout());

    Thread * pThread = GetThreadNULLOk();

    _ASSERTE((pThread != NULL) || !g_fEEStarted || dbgOnly_IsSpecialEEThread());

    {
        if (pThread && alertable) {
            GCX_PREEMP();
            return pThread->DoReentrantWaitWithRetry(m_handle, dwMilliseconds, mode);
        }
        else {
            return CLREventWaitHelper(m_handle,dwMilliseconds,alertable);
        }
    }
}
