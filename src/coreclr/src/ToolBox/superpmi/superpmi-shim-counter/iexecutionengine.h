//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _IExecutionEngine
#define _IExecutionEngine

#include "ieememorymanager.h"

/*
interface IExecutionEngine : IUnknown
{
    // Thread Local Storage is based on logical threads.  The underlying
    // implementation could be threads, fibers, or something more exotic.
    // Slot numbers are predefined.  This is not a general extensibility
    // mechanism.

    // Associate a callback function for releasing TLS on thread/fiber death.
    // This can be NULL.
    void TLS_AssociateCallback([in] DWORD slot, [in] PTLS_CALLBACK_FUNCTION callback)

    // May be called once to get the master TLS block slot for fast Get/Set operations
    DWORD TLS_GetMasterSlotIndex()

    // Get the value at a slot
    PVOID TLS_GetValue([in] DWORD slot)

    // Get the value at a slot, return FALSE if TLS info block doesn't exist
    BOOL TLS_CheckValue([in] DWORD slot, [out] PVOID * pValue)

    // Set the value at a slot
    void TLS_SetValue([in] DWORD slot, [in] PVOID pData)

    // Free TLS memory block and make callback
    void TLS_ThreadDetaching()

    // Critical Sections are sometimes exposed to the host and therefore need to be
    // reflected from all CLR DLLs to the EE.
    //
    // In addition, we always monitor interactions between the lock & the GC, based
    // on the GC mode in which the lock is acquired and we restrict what operations
    // are permitted while holding the lock based on this.
    //
    // Finally, we we rank all our locks to prevent deadlock across all the DLLs of
    // the CLR.  This is the level argument to CreateLock.
    //
    // All usage of these locks must be exception-safe.  To achieve this, we suggest
    // using Holders (see holder.h & crst.h).  In fact, within the EE code cannot
    // hold locks except by using exception-safe holders.

    CRITSEC_COOKIE CreateLock([in] LPCSTR szTag, [in] LPCSTR level, [in] CrstFlags flags)

    void DestroyLock([in] CRITSEC_COOKIE lock)

    void AcquireLock([in] CRITSEC_COOKIE lock)

    void ReleaseLock([in] CRITSEC_COOKIE lock)

    EVENT_COOKIE CreateAutoEvent([in] BOOL bInitialState)
    EVENT_COOKIE CreateManualEvent([in] BOOL bInitialState)
    void CloseEvent([in] EVENT_COOKIE event)
    BOOL ClrSetEvent([in] EVENT_COOKIE event)
    BOOL ClrResetEvent([in] EVENT_COOKIE event)
    DWORD WaitForEvent([in] EVENT_COOKIE event, [in] DWORD dwMilliseconds, [in] BOOL bAlertable)
    DWORD WaitForSingleObject([in] HANDLE handle, [in] DWORD dwMilliseconds)

    // OS header file defines CreateSemaphore.
    SEMAPHORE_COOKIE ClrCreateSemaphore([in] DWORD dwInitial, [in] DWORD dwMax)
    void ClrCloseSemaphore([in] SEMAPHORE_COOKIE semaphore)
    DWORD ClrWaitForSemaphore([in] SEMAPHORE_COOKIE semaphore, [in] DWORD dwMilliseconds, [in] BOOL bAlertable)
    BOOL ClrReleaseSemaphore([in] SEMAPHORE_COOKIE semaphore, [in] LONG lReleaseCount, [in] LONG *lpPreviousCount)

    MUTEX_COOKIE ClrCreateMutex([in]LPSECURITY_ATTRIBUTES lpMutexAttributes, [in]BOOL bInitialOwner, [in]LPCTSTR lpName)
    DWORD ClrWaitForMutex([in] MUTEX_COOKIE mutex, [in] DWORD dwMilliseconds, [in] BOOL bAlertable)
    BOOL ClrReleaseMutex([in] MUTEX_COOKIE mutex)
    void ClrCloseMutex([in] MUTEX_COOKIE mutex)

    DWORD ClrSleepEx([in] DWORD dwMilliseconds, [in] BOOL bAlertable)

    BOOL ClrAllocationDisallowed()

    void GetLastThrownObjectExceptionFromThread([out] void **ppvException)

};  // interface IExecutionEngine
*/

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
    IExecutionEngine* original_IEE;
};

#endif