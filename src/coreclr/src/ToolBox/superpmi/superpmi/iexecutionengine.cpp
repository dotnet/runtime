//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "spmiutil.h"
#include "iexecutionengine.h"

LPVOID TLS_Slots[MAX_PREDEFINED_TLS_SLOT];
class MyIEE;
IExecutionEngine* pIEE = nullptr;

//***************************************************************************
// IUnknown methods
//***************************************************************************

HRESULT STDMETHODCALLTYPE MyIEE::QueryInterface(REFIID id, void** pInterface)
{
    // TODO-Cleanup: check the rid
    *pInterface = InitIEEMemoryManager(nullptr);
    return 0;
}
ULONG STDMETHODCALLTYPE MyIEE::AddRef()
{
    DebugBreakorAV(142);
    return 0;
}
ULONG STDMETHODCALLTYPE MyIEE::Release()
{
    DebugBreakorAV(143);
    return 0;
}

//***************************************************************************
// IExecutionEngine methods for TLS
//***************************************************************************

DWORD TlsIndex = 42;

// Associate a callback for cleanup with a TLS slot
VOID STDMETHODCALLTYPE MyIEE::TLS_AssociateCallback(DWORD slot, PTLS_CALLBACK_FUNCTION callback)
{
    // TODO-Cleanup: figure an appropriate realish value for this
}

// Get the TLS block for fast Get/Set operations
LPVOID* STDMETHODCALLTYPE MyIEE::TLS_GetDataBlock()
{
    // We were previously allocating a TlsIndex with
    // the master slot index set to a nullptr
    // so in the new version we just return nullptr
    // and it seems to be working for now
    return nullptr;
}

// Get the value at a slot
LPVOID STDMETHODCALLTYPE MyIEE::TLS_GetValue(DWORD slot)
{
    /*  if(slot>MAX_PREDEFINED_TLS_SLOT)
            __debugbreak();
            void *thing = TlsGetValue(TlsIndex);

            //  if(slot == 0x9)
            //return 0; //trick out the contract system to be as off as possible.
            //TODO-Cleanup: does anything beyond contracts care?  This seems like a pretty thin mock.
            */
    return TLS_Slots[slot];
}

// Get the value at a slot, return FALSE if TLS info block doesn't exist
BOOL STDMETHODCALLTYPE MyIEE::TLS_CheckValue(DWORD slot, LPVOID* pValue)
{
    DebugBreakorAV(144);
    // TODO-Cleanup: does anything beyond contracts care?  This seems like a pretty thin mock.
    return true;
}
// Set the value at a slot
VOID STDMETHODCALLTYPE MyIEE::TLS_SetValue(DWORD slot, LPVOID pData)
{
    if (slot > MAX_PREDEFINED_TLS_SLOT)
    {
        DebugBreakorAV(143);
        return;
    }
    void* thing = TlsGetValue(TlsIndex); // TODO-Cleanup: this seems odd.. explain?

    // TODO-Cleanup: does anything beyond contracts care?  This seems like a pretty thin mock.
    TLS_Slots[slot] = pData;
}
// Free TLS memory block and make callback
VOID STDMETHODCALLTYPE MyIEE::TLS_ThreadDetaching()
{
    DebugBreakorAV(145);
}

//***************************************************************************
// IExecutionEngine methods for locking
//***************************************************************************

CRITSEC_COOKIE STDMETHODCALLTYPE MyIEE::CreateLock(LPCSTR szTag, LPCSTR level, CrstFlags flags)
{
    return (CRITSEC_COOKIE)(size_t)0xbad01241;
}
void STDMETHODCALLTYPE MyIEE::DestroyLock(CRITSEC_COOKIE lock)
{
    DebugBreakorAV(146);
}
void STDMETHODCALLTYPE MyIEE::AcquireLock(CRITSEC_COOKIE lock)
{
}
void STDMETHODCALLTYPE MyIEE::ReleaseLock(CRITSEC_COOKIE lock)
{
}

EVENT_COOKIE STDMETHODCALLTYPE MyIEE::CreateAutoEvent(BOOL bInitialState)
{
    DebugBreakorAV(147);
    return 0;
}
EVENT_COOKIE STDMETHODCALLTYPE MyIEE::CreateManualEvent(BOOL bInitialState)
{
    DebugBreakorAV(148);
    return 0;
}
void STDMETHODCALLTYPE MyIEE::CloseEvent(EVENT_COOKIE event)
{
    DebugBreakorAV(149);
}
BOOL STDMETHODCALLTYPE MyIEE::ClrSetEvent(EVENT_COOKIE event)
{
    DebugBreakorAV(150);
    return 0;
}
BOOL STDMETHODCALLTYPE MyIEE::ClrResetEvent(EVENT_COOKIE event)
{
    DebugBreakorAV(151);
    return 0;
}
DWORD STDMETHODCALLTYPE MyIEE::WaitForEvent(EVENT_COOKIE event, DWORD dwMilliseconds, BOOL bAlertable)
{
    DebugBreakorAV(152);
    return 0;
}
DWORD STDMETHODCALLTYPE MyIEE::WaitForSingleObject(HANDLE handle, DWORD dwMilliseconds)
{
    DebugBreakorAV(153);
    return 0;
}
SEMAPHORE_COOKIE STDMETHODCALLTYPE MyIEE::ClrCreateSemaphore(DWORD dwInitial, DWORD dwMax)
{
    DebugBreakorAV(154);
    return 0;
}
void STDMETHODCALLTYPE MyIEE::ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore)
{
    DebugBreakorAV(155);
}
DWORD STDMETHODCALLTYPE MyIEE::ClrWaitForSemaphore(SEMAPHORE_COOKIE semaphore, DWORD dwMilliseconds, BOOL bAlertable)
{
    DebugBreakorAV(156);
    return 0;
}
BOOL STDMETHODCALLTYPE MyIEE::ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore, LONG lReleaseCount, LONG* lpPreviousCount)
{
    DebugBreakorAV(157);
    return 0;
}
MUTEX_COOKIE STDMETHODCALLTYPE MyIEE::ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                                                     BOOL                  bInitialOwner,
                                                     LPCTSTR               lpName)
{
    DebugBreakorAV(158);
    return 0;
}
void STDMETHODCALLTYPE MyIEE::ClrCloseMutex(MUTEX_COOKIE mutex)
{
    DebugBreakorAV(159);
}
BOOL STDMETHODCALLTYPE MyIEE::ClrReleaseMutex(MUTEX_COOKIE mutex)
{
    DebugBreakorAV(160);
    return 0;
}
DWORD STDMETHODCALLTYPE MyIEE::ClrWaitForMutex(MUTEX_COOKIE mutex, DWORD dwMilliseconds, BOOL bAlertable)
{
    DebugBreakorAV(161);
    return 0;
}

DWORD STDMETHODCALLTYPE MyIEE::ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    DebugBreakorAV(162);
    return 0;
}
BOOL STDMETHODCALLTYPE MyIEE::ClrAllocationDisallowed()
{
    DebugBreakorAV(163);
    return 0;
}
void STDMETHODCALLTYPE MyIEE::GetLastThrownObjectExceptionFromThread(void** ppvException)
{
    DebugBreakorAV(164);
}

MyIEE* InitIExecutionEngine()
{
    MyIEE* iee = new MyIEE();
    pIEE       = iee;
    return iee;
}
