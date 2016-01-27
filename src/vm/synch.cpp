// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 

#include "common.h"

#include "corhost.h"
#include "synch.h"
#include "rwlock.h"

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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostSyncManager *pManager = CorHost2::GetHostSyncManager();
    if (pManager != NULL) {
        IHostAutoEvent *pEvent;
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pManager->CreateAutoEvent(&pEvent);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (hr != S_OK) {
            _ASSERTE (hr == E_OUTOFMEMORY);
            ThrowOutOfMemory();
        }
        if (bInitialState) 
        {
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
            pEvent->Set();
            END_SO_TOLERANT_CODE_CALLING_HOST;
        }
        m_handle = (HANDLE)pEvent;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        HANDLE h = UnsafeCreateEvent(NULL,FALSE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
    
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostSyncManager *pManager = CorHost2::GetHostSyncManager();
    if (pManager != NULL){
        IHostManualEvent *pEvent;
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pManager->CreateManualEvent(bInitialState, &pEvent);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (hr != S_OK) {
            _ASSERTE (hr == E_OUTOFMEMORY);
            ThrowOutOfMemory();
        }
        m_handle = (HANDLE)pEvent;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        HANDLE h = UnsafeCreateEvent(NULL,TRUE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostSyncManager *pManager = CorHost2::GetHostSyncManager();
    if (pManager != NULL){
        IHostAutoEvent *pEvent;
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pManager->CreateMonitorEvent(Cookie,&pEvent);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (hr != S_OK) {
            _ASSERTE (hr == E_OUTOFMEMORY);
            ThrowOutOfMemory();
        }
        if (FastInterlockCompareExchangePointer(&m_handle,
                                                reinterpret_cast<HANDLE>(pEvent),
                                                INVALID_HANDLE_VALUE) != INVALID_HANDLE_VALUE)
        {
            // We lost the race
            pEvent->Release();
        }
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
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

#ifdef FEATURE_RWLOCK
void CLREventBase::CreateRWLockWriterEvent (BOOL bInitialState,  // If TRUE, initial state is signalled
                                        CRWLock *pRWLock
                                )
{
    CONTRACTL
    {
        THROWS;           
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((g_fEEStarted));        
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));        
        PRECONDITION((GetThread() != NULL));        
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    SetAutoEvent();

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostSyncManager *pManager = CorHost2::GetHostSyncManager();
    if (pManager != NULL){
        // Need to have a fixed cookie.  Use a weak handle for this purpose.
        IHostAutoEvent *pEvent;
        HRESULT hr;
        SIZE_T cookie = (SIZE_T)pRWLock->GetObjectHandle();
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pManager->CreateRWLockWriterEvent(cookie, &pEvent);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (hr != S_OK) {
            _ASSERTE (hr == E_OUTOFMEMORY);
            ThrowOutOfMemory();
        }
        if (bInitialState) 
        {
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
            pEvent->Set();
            END_SO_TOLERANT_CODE_CALLING_HOST;
        }
        m_handle = (HANDLE)pEvent;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        HANDLE h = UnsafeCreateEvent(NULL,FALSE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }

    SetInDeadlockDetection();
}

void CLREventBase::CreateRWLockReaderEvent (BOOL bInitialState,  // If TRUE, initial state is signalled
                                        CRWLock *pRWLock
                                )
{
    CONTRACTL
    {
        THROWS;           
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((g_fEEStarted));        
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));        
        PRECONDITION((GetThread() != NULL));        
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    IHostSyncManager *pManager = CorHost2::GetHostSyncManager();
    if (pManager == NULL) {
        HANDLE h = UnsafeCreateEvent(NULL,TRUE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
    else {
        IHostManualEvent *pEvent;
        HRESULT hr;
        SIZE_T cookie = (SIZE_T)pRWLock->GetObjectHandle();
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pManager->CreateRWLockReaderEvent(bInitialState, cookie, &pEvent);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (hr != S_OK) {
            _ASSERTE (hr == E_OUTOFMEMORY);
            ThrowOutOfMemory();
        }
        m_handle = (HANDLE)pEvent;
    }

    SetInDeadlockDetection();
}
#endif // FEATURE_RWLOCK


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
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
        if (!IsOSEvent() && CLRSyncHosted())
        {
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());

            if (IsAutoEvent()) {
                ((IHostAutoEvent*)m_handle)->Release();
            }
            else {
                ((IHostManualEvent*)m_handle)->Release();
            }

            END_SO_TOLERANT_CODE_CALLING_HOST;
        }
        else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (!IsOSEvent() && CLRSyncHosted())
    {
        if (IsAutoEvent()) {
            HRESULT hr;
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
            hr = ((IHostAutoEvent*)m_handle)->Set();
            END_SO_TOLERANT_CODE_CALLING_HOST;
            return hr == S_OK;
        }
        else {
            HRESULT hr;
            BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
            hr = ((IHostManualEvent*)m_handle)->Set();
            END_SO_TOLERANT_CODE_CALLING_HOST;
            return hr == S_OK;
        }
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (!IsOSEvent() && CLRSyncHosted())
    {
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = ((IHostManualEvent*)m_handle)->Reset();
        END_SO_TOLERANT_CODE_CALLING_HOST;
        return hr == S_OK;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return UnsafeResetEvent(m_handle);
    }
}

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
static DWORD HostAutoEventWait (void *args, DWORD timeout, DWORD option)
{
    BOOL alertable = (option & WAIT_ALERTABLE);
    CONTRACTL
    {
      if (alertable)
      {
          THROWS;
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
      PRECONDITION(CheckPointer(args));
    }
    CONTRACTL_END;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    HRESULT hr;
    BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
    hr = ((IHostAutoEvent*)args)->Wait(timeout,option);
    END_SO_TOLERANT_CODE_CALLING_HOST;
#ifdef _DEBUG
    if (FAILED(hr) && timeout == INFINITE) {
        _ASSERTE (option & WAIT_ALERTABLE);
    }
#endif
    if (hr == S_OK) {
        return WAIT_OBJECT_0;
    }
    else if (hr == HOST_E_DEADLOCK) {
        RaiseDeadLockException();
    }
    else if (hr == HOST_E_INTERRUPTED) {
        _ASSERTE (option & WAIT_ALERTABLE);
        Thread *pThread = GetThread();
        if (pThread)
        {
            Thread::UserInterruptAPC(APC_Code);
        }
        return WAIT_IO_COMPLETION;
    }
    else if (hr == HOST_E_TIMEOUT) {
        return WAIT_TIMEOUT;
    }
    else if (hr == HOST_E_ABANDONED) {
        return WAIT_ABANDONED;
    }
    else if (hr == E_FAIL) {
        _ASSERTE (!"Unknown host wait failure");
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        _ASSERTE (!"Unknown host wait return");
    }
    return 0;
}

static DWORD HostManualEventWait (void *args, DWORD timeout, DWORD option)
{
    CONTRACTL
    {
        if (option & WAIT_ALERTABLE)
        {
            THROWS;
        }
        else
        {
            NOTHROW;
        }
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(args));
    }
    CONTRACTL_END;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    HRESULT hr;
    BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
    hr = ((IHostManualEvent*)args)->Wait(timeout,option);
    END_SO_TOLERANT_CODE_CALLING_HOST;

    if (hr == COR_E_STACKOVERFLOW)
    {
        Thread *pThread = GetThread();
        if (pThread && pThread->HasThreadStateNC(Thread::TSNC_WaitUntilGCFinished))
        {
            return hr;
        }
    }
#ifdef _DEBUG
    if (FAILED(hr) && timeout == INFINITE) {
        _ASSERTE (option & WAIT_ALERTABLE);
    }
#endif
    if (hr == S_OK) {
        return WAIT_OBJECT_0;
    }
    else if (hr == HOST_E_DEADLOCK) {
        RaiseDeadLockException();
    }
    else if (hr == HOST_E_INTERRUPTED) {
        _ASSERTE (option & WAIT_ALERTABLE);
        Thread *pThread = GetThread();
        if (pThread)
        {
            Thread::UserInterruptAPC(APC_Code);
        }
        return WAIT_IO_COMPLETION;
    }
    else if (hr == HOST_E_TIMEOUT) {
        return WAIT_TIMEOUT;
    }
    else if (hr == HOST_E_ABANDONED) {
        return WAIT_ABANDONED;
    }
    else if (hr == E_FAIL) {
        _ASSERTE (!"Unknown host wait failure");
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        _ASSERTE (!"Unknown host wait return");
    }
    return 0;
}
#endif // FEATURE_INCLUDE_ALL_INTERFACES

static DWORD CLREventWaitHelper2(HANDLE handle, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_SO_TOLERANT;
    
    LeaveRuntimeHolder holder((size_t)WaitForSingleObjectEx);
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (!IsOSEvent() && CLRSyncHosted())
    {
       if ((pThread != NULL) && alertable) {
            DWORD dwRet = WAIT_FAILED;
            BEGIN_SO_INTOLERANT_CODE_NOTHROW (pThread, return WAIT_FAILED;);
            dwRet = pThread->DoAppropriateWait(IsAutoEvent()?HostAutoEventWait:HostManualEventWait,
                                              m_handle,dwMilliseconds,
                                              mode,
                                              syncState);
            END_SO_INTOLERANT_CODE;
            return dwRet;
        }
        else {
            _ASSERTE (syncState == NULL);
            DWORD option = 0;
            if (alertable) {
                option |= WAIT_ALERTABLE;
            }
            if (IsAutoEvent()) {
                return HostAutoEventWait((IHostAutoEvent*)m_handle,dwMilliseconds, option);
            }
            else {
                return HostManualEventWait((IHostManualEvent*)m_handle,dwMilliseconds, option);
            }
        }
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostSyncManager *pManager = CorHost2::GetHostSyncManager();
    if (pManager != NULL) {
        IHostSemaphore *pSemaphore;
        #undef CreateSemaphore
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pManager->CreateSemaphore(dwInitial,dwMax,&pSemaphore);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        #define CreateSemaphore(lpSemaphoreAttributes, lInitialCount, lMaximumCount, lpName) \
                Dont_Use_CreateSemaphore(lpSemaphoreAttributes, lInitialCount, lMaximumCount, lpName)
        if (hr != S_OK) {
            _ASSERTE(hr == E_OUTOFMEMORY);
            ThrowOutOfMemory();
        }
        m_handle = (HANDLE)pSemaphore;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
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
        if (!CLRSyncHosted()) {
            CloseHandle(m_handle);
        }
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
        else {
            ((IHostSemaphore*)m_handle)->Release();
        }
#endif // FEATURE_INCLUDE_ALL_INTERFACES
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (CLRSyncHosted())
    {
        #undef ReleaseSemaphore
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = ((IHostSemaphore*)m_handle)->ReleaseSemaphore(lReleaseCount,lpPreviousCount);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        #define ReleaseSemaphore(hSemaphore, lReleaseCount, lpPreviousCount) \
                Dont_Use_ReleaseSemaphore(hSemaphore, lReleaseCount, lpPreviousCount)
        return hr == S_OK;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return ::UnsafeReleaseSemaphore(m_handle, lReleaseCount, lpPreviousCount);
    }
}

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
static DWORD HostSemaphoreWait (void *args, DWORD timeout, DWORD option)
{
    CONTRACTL
    {
        if ((option & WAIT_ALERTABLE))
        {
            THROWS;               // Thread::DoAppropriateWait can throw   
        }
        else
        {
            NOTHROW;
        }
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(args));
    }
    CONTRACTL_END;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    HRESULT hr;
    BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
    hr = ((IHostSemaphore*)args)->Wait(timeout,option);
    END_SO_TOLERANT_CODE_CALLING_HOST;
    if (hr == S_OK) {
        return WAIT_OBJECT_0;
    }
    else if (hr == HOST_E_INTERRUPTED) {
        _ASSERTE (option & WAIT_ALERTABLE);
        Thread *pThread = GetThread();
        if (pThread)
        {
            Thread::UserInterruptAPC(APC_Code);
        }
        return WAIT_IO_COMPLETION;
    }
    else if (hr == HOST_E_TIMEOUT) {
        return WAIT_TIMEOUT;
    }
    else if (hr == E_FAIL) {
        _ASSERTE (!"Unknown host wait failure");
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        _ASSERTE (!"Unknown host wait return");
    }
    return 0;
}
#endif // FEATURE_INCLUDE_ALL_INTERFACES

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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (CLRSyncHosted())
    {
        if (pThread && alertable) {
            return pThread->DoAppropriateWait(HostSemaphoreWait,
                                              m_handle,dwMilliseconds,
                                              alertable?WaitMode_Alertable:WaitMode_None,
                                              NULL);
        }
        else {
            DWORD option = 0;
            if (alertable) {
                option |= WAIT_ALERTABLE;
            }
            return HostSemaphoreWait((IHostSemaphore*)m_handle,dwMilliseconds,option);
        }
    }
    else
#endif // !FEATURE_INCLUDE_ALL_INTERFACES
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
                LeaveRuntimeHolder holder((size_t)WaitForSingleObjectEx);
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

    if (bInitialOwner)
    {
        Thread::BeginThreadAffinity();
    }
    m_handle = WszCreateMutex(lpMutexAttributes,bInitialOwner,lpName);
    if (m_handle == NULL)
    {
        if (bInitialOwner)
        {
            Thread::EndThreadAffinity();
        }
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
        Thread::EndThreadAffinity();
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

    Thread::BeginThreadAffinity();
    DWORD fRet = WaitForSingleObjectEx(m_handle, dwMilliseconds, bAlertable);
    if ((fRet != WAIT_OBJECT_0) && (fRet != WAIT_ABANDONED))
    {
        Thread::EndThreadAffinity();
    }

    if (fRet == WAIT_OBJECT_0)
    {
        EE_LOCK_TAKEN(this);
    }

    return fRet;
}
