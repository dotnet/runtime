//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// IExecutionEngine.h - core shim implementation for IEE stuff
//----------------------------------------------------------
#ifndef _IExecutionEngine
#define _IExecutionEngine

#include "ieememorymanager.h"

class interceptor_IEE : public IExecutionEngine
{
private:
    //***************************************************************************
    // IUnknown methods
    //***************************************************************************
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void** pInterface);
    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();

    //***************************************************************************
    // IExecutionEngine methods for TLS
    //***************************************************************************
    // Associate a callback for cleanup with a TLS slot
    VOID STDMETHODCALLTYPE TLS_AssociateCallback(DWORD slot, PTLS_CALLBACK_FUNCTION callback);
    // Get the TLS block for fast Get/Set operations
    LPVOID* STDMETHODCALLTYPE TLS_GetDataBlock();
    // Get the value at a slot
    LPVOID STDMETHODCALLTYPE TLS_GetValue(DWORD slot);
    // Get the value at a slot, return FALSE if TLS info block doesn't exist
    BOOL STDMETHODCALLTYPE TLS_CheckValue(DWORD slot, LPVOID* pValue);
    // Set the value at a slot
    VOID STDMETHODCALLTYPE TLS_SetValue(DWORD slot, LPVOID pData);
    // Free TLS memory block and make callback
    VOID STDMETHODCALLTYPE TLS_ThreadDetaching();

    //***************************************************************************
    // IExecutionEngine methods for locking
    //***************************************************************************
    CRITSEC_COOKIE STDMETHODCALLTYPE CreateLock(LPCSTR szTag, LPCSTR level, CrstFlags flags);
    void STDMETHODCALLTYPE DestroyLock(CRITSEC_COOKIE lock);
    void STDMETHODCALLTYPE AcquireLock(CRITSEC_COOKIE lock);
    void STDMETHODCALLTYPE ReleaseLock(CRITSEC_COOKIE lock);
    EVENT_COOKIE STDMETHODCALLTYPE CreateAutoEvent(BOOL bInitialState);
    EVENT_COOKIE STDMETHODCALLTYPE CreateManualEvent(BOOL bInitialState);
    void STDMETHODCALLTYPE CloseEvent(EVENT_COOKIE event);
    BOOL STDMETHODCALLTYPE ClrSetEvent(EVENT_COOKIE event);
    BOOL STDMETHODCALLTYPE ClrResetEvent(EVENT_COOKIE event);
    DWORD STDMETHODCALLTYPE WaitForEvent(EVENT_COOKIE event, DWORD dwMilliseconds, BOOL bAlertable);
    DWORD STDMETHODCALLTYPE WaitForSingleObject(HANDLE handle, DWORD dwMilliseconds);
    SEMAPHORE_COOKIE STDMETHODCALLTYPE ClrCreateSemaphore(DWORD dwInitial, DWORD dwMax);
    void STDMETHODCALLTYPE ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore);
    DWORD STDMETHODCALLTYPE ClrWaitForSemaphore(SEMAPHORE_COOKIE semaphore, DWORD dwMilliseconds, BOOL bAlertable);
    BOOL STDMETHODCALLTYPE ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore, LONG lReleaseCount, LONG* lpPreviousCount);
    MUTEX_COOKIE STDMETHODCALLTYPE ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                                                  BOOL                  bInitialOwner,
                                                  LPCTSTR               lpName);
    void STDMETHODCALLTYPE ClrCloseMutex(MUTEX_COOKIE mutex);
    BOOL STDMETHODCALLTYPE ClrReleaseMutex(MUTEX_COOKIE mutex);
    DWORD STDMETHODCALLTYPE ClrWaitForMutex(MUTEX_COOKIE mutex, DWORD dwMilliseconds, BOOL bAlertable);
    DWORD STDMETHODCALLTYPE ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable);
    BOOL STDMETHODCALLTYPE ClrAllocationDisallowed();
    void STDMETHODCALLTYPE GetLastThrownObjectExceptionFromThread(void** ppvException);

public:
    IExecutionEngine* original_IEE; // Our extra value that holds a pointer to the original IEE we'll pass calls along
                                    // to
};

#endif