//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "iexecutionengine.h"
#include "superpmi-shim-counter.h"

//***************************************************************************
// IUnknown methods
//***************************************************************************
HRESULT STDMETHODCALLTYPE interceptor_IEE::QueryInterface(REFIID id, void** pInterface)
{
    return original_IEE->QueryInterface(id, pInterface);
}
ULONG STDMETHODCALLTYPE interceptor_IEE::AddRef()
{
    return original_IEE->AddRef();
}
ULONG STDMETHODCALLTYPE interceptor_IEE::Release()
{
    return original_IEE->Release();
}

//***************************************************************************
// IExecutionEngine methods for TLS
//***************************************************************************
// Associate a callback for cleanup with a TLS slot
VOID STDMETHODCALLTYPE interceptor_IEE::TLS_AssociateCallback(DWORD slot, PTLS_CALLBACK_FUNCTION callback)
{
    original_IEE->TLS_AssociateCallback(slot, callback);
}
// Get the TLS block for fast Get/Set operations
LPVOID* STDMETHODCALLTYPE interceptor_IEE::TLS_GetDataBlock()
{
    return original_IEE->TLS_GetDataBlock();
}
// Get the value at a slot
LPVOID STDMETHODCALLTYPE interceptor_IEE::TLS_GetValue(DWORD slot)
{
    return original_IEE->TLS_GetValue(slot);
}

// Get the value at a slot, return FALSE if TLS info block doesn't exist
BOOL STDMETHODCALLTYPE interceptor_IEE::TLS_CheckValue(DWORD slot, LPVOID* pValue)
{
    return original_IEE->TLS_CheckValue(slot, pValue);
}
// Set the value at a slot
VOID STDMETHODCALLTYPE interceptor_IEE::TLS_SetValue(DWORD slot, LPVOID pData)
{
    original_IEE->TLS_SetValue(slot, pData);
}
// Free TLS memory block and make callback
VOID STDMETHODCALLTYPE interceptor_IEE::TLS_ThreadDetaching()
{
    original_IEE->TLS_ThreadDetaching();
}

//***************************************************************************
// IExecutionEngine methods for locking
//***************************************************************************
CRITSEC_COOKIE STDMETHODCALLTYPE interceptor_IEE::CreateLock(LPCSTR szTag, LPCSTR level, CrstFlags flags)
{
    return original_IEE->CreateLock(szTag, level, flags);
}
void STDMETHODCALLTYPE interceptor_IEE::DestroyLock(CRITSEC_COOKIE lock)
{
    original_IEE->DestroyLock(lock);
}
void STDMETHODCALLTYPE interceptor_IEE::AcquireLock(CRITSEC_COOKIE lock)
{
    original_IEE->AcquireLock(lock);
}
void STDMETHODCALLTYPE interceptor_IEE::ReleaseLock(CRITSEC_COOKIE lock)
{
    original_IEE->ReleaseLock(lock);
}

EVENT_COOKIE STDMETHODCALLTYPE interceptor_IEE::CreateAutoEvent(BOOL bInitialState)
{
    return original_IEE->CreateAutoEvent(bInitialState);
}
EVENT_COOKIE STDMETHODCALLTYPE interceptor_IEE::CreateManualEvent(BOOL bInitialState)
{
    return original_IEE->CreateManualEvent(bInitialState);
}
void STDMETHODCALLTYPE interceptor_IEE::CloseEvent(EVENT_COOKIE event)
{
    original_IEE->CloseEvent(event);
}
BOOL STDMETHODCALLTYPE interceptor_IEE::ClrSetEvent(EVENT_COOKIE event)
{
    return original_IEE->ClrSetEvent(event);
}
BOOL STDMETHODCALLTYPE interceptor_IEE::ClrResetEvent(EVENT_COOKIE event)
{
    return original_IEE->ClrResetEvent(event);
}
DWORD STDMETHODCALLTYPE interceptor_IEE::WaitForEvent(EVENT_COOKIE event, DWORD dwMilliseconds, BOOL bAlertable)
{
    return original_IEE->WaitForEvent(event, dwMilliseconds, bAlertable);
}
DWORD STDMETHODCALLTYPE interceptor_IEE::WaitForSingleObject(HANDLE handle, DWORD dwMilliseconds)
{
    return original_IEE->WaitForSingleObject(handle, dwMilliseconds);
}
SEMAPHORE_COOKIE STDMETHODCALLTYPE interceptor_IEE::ClrCreateSemaphore(DWORD dwInitial, DWORD dwMax)
{
    return original_IEE->ClrCreateSemaphore(dwInitial, dwMax);
}
void STDMETHODCALLTYPE interceptor_IEE::ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore)
{
    original_IEE->ClrCloseSemaphore(semaphore);
}
DWORD STDMETHODCALLTYPE interceptor_IEE::ClrWaitForSemaphore(SEMAPHORE_COOKIE semaphore,
                                                             DWORD            dwMilliseconds,
                                                             BOOL             bAlertable)
{
    return original_IEE->ClrWaitForSemaphore(semaphore, dwMilliseconds, bAlertable);
}
BOOL STDMETHODCALLTYPE interceptor_IEE::ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore,
                                                            LONG             lReleaseCount,
                                                            LONG*            lpPreviousCount)
{
    return original_IEE->ClrReleaseSemaphore(semaphore, lReleaseCount, lpPreviousCount);
}
MUTEX_COOKIE STDMETHODCALLTYPE interceptor_IEE::ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                                                               BOOL                  bInitialOwner,
                                                               LPCTSTR               lpName)
{
    return original_IEE->ClrCreateMutex(lpMutexAttributes, bInitialOwner, lpName);
}
void STDMETHODCALLTYPE interceptor_IEE::ClrCloseMutex(MUTEX_COOKIE mutex)
{
    original_IEE->ClrCloseMutex(mutex);
}
BOOL STDMETHODCALLTYPE interceptor_IEE::ClrReleaseMutex(MUTEX_COOKIE mutex)
{
    return original_IEE->ClrReleaseMutex(mutex);
}
DWORD STDMETHODCALLTYPE interceptor_IEE::ClrWaitForMutex(MUTEX_COOKIE mutex, DWORD dwMilliseconds, BOOL bAlertable)
{
    return original_IEE->ClrWaitForMutex(mutex, dwMilliseconds, bAlertable);
}

DWORD STDMETHODCALLTYPE interceptor_IEE::ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    return original_IEE->ClrSleepEx(dwMilliseconds, bAlertable);
}
BOOL STDMETHODCALLTYPE interceptor_IEE::ClrAllocationDisallowed()
{
    return original_IEE->ClrAllocationDisallowed();
}
void STDMETHODCALLTYPE interceptor_IEE::GetLastThrownObjectExceptionFromThread(void** ppvException)
{
    original_IEE->GetLastThrownObjectExceptionFromThread(ppvException);
}
