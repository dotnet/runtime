// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "common.h"

#include "corhost.h"
#include "synch.h"

void CLREventBase::CreateAutoEvent(bool initialState
                                )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION(!IsValid());
    }
    CONTRACTL_END;

    if (!CreateAutoEventNoThrow(initialState))
    {
        ThrowOutOfMemory();
    }
}

void CLREventBase::CreateManualEvent(bool initialState
                                )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION(!IsValid());
    }
    CONTRACTL_END;

    if (!CreateManualEventNoThrow(initialState))
    {
        ThrowOutOfMemory();
    }
}

static DWORD CLREventWaitHelper2(CLREventBase *event, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_THROWS;

    return event->Wait(dwMilliseconds, alertable);
}

static DWORD CLREventWaitHelper(CLREventBase *event, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        CLREventBase *event;
        DWORD dwMilliseconds;
        BOOL alertable;
        DWORD result;
    } param;
    param.event = event;
    param.dwMilliseconds = dwMilliseconds;
    param.alertable = alertable;
    param.result = WAIT_FAILED;

    // Can not use EX_TRY/CATCH.  EX_CATCH toggles GC mode.  This function is called
    // through RareDisablePreemptiveGC.  EX_CATCH breaks profiler callback.
    PAL_TRY(Param *, pParam, &param)
    {
        // Need to move to another helper (cannot have SEH and C++ destructors
        // on automatic variables in one function)
        pParam->result = CLREventWaitHelper2(pParam->event, pParam->dwMilliseconds, pParam->alertable);
    }
    PAL_EXCEPT (EXCEPTION_EXECUTE_HANDLER)
    {
        param.result = WAIT_FAILED;
    }
    PAL_ENDTRY;

    return param.result;
}


uint32_t CLREventBase::Wait(uint32_t dwMilliseconds, bool alertable, bool allowReentrantWait)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!allowReentrantWait);
    return WaitEx(dwMilliseconds, alertable?WaitMode_Alertable:WaitMode_None);
}


uint32_t CLREventBase::WaitEx(uint32_t dwMilliseconds, uint32_t mode)
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
        PRECONDITION(IsValid());
    }
    CONTRACTL_END;


    _ASSERTE(Thread::Debug_AllowCallout());

    Thread * pThread = GetThreadNULLOk();

    _ASSERTE((pThread != NULL) || !g_fEEStarted || dbgOnly_IsSpecialEEThread());

    {
        if (pThread && alertable) {
            GCX_PREEMP();
#ifdef TARGET_UNIX
            return Wait(dwMilliseconds, alertable);
#else
            return pThread->DoReentrantWaitWithRetry(m_handle, dwMilliseconds, static_cast<WaitMode>(mode));
#endif // TARGET_UNIX
        }
        else {
            return CLREventWaitHelper(this, dwMilliseconds, alertable);
        }
    }
}
