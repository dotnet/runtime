// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//
//

//
// ==--==

#include "stdafx.h"

#include "mscoree.h"
#include "clrinternal.h"
#include "hostimpl.h"

// to avoid to include clrhost.h in this file
#ifdef FAILPOINTS_ENABLED
extern int RFS_HashStack();
#endif

#ifdef SELF_NO_HOST
HANDLE (*g_fnGetExecutableHeapHandle)();
#endif

thread_local size_t t_ThreadType;

HRESULT STDMETHODCALLTYPE UtilExecutionEngine::QueryInterface(REFIID id, void **pInterface)
{
    if (!pInterface)
        return E_POINTER;

    *pInterface = NULL;

    if (id == IID_IExecutionEngine)
        *pInterface = (IExecutionEngine *)this;
    else if (id == IID_IEEMemoryManager)
        *pInterface = (IEEMemoryManager *)this;
    else if (id == IID_IUnknown)
        *pInterface = (IUnknown *)(IExecutionEngine *)this;
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
} // UtilExecutionEngine::QueryInterface

//
// lifetime of this object is that of the app it lives in so no point in AddRef/Release
//
ULONG STDMETHODCALLTYPE UtilExecutionEngine::AddRef()
{
    return 1;
}

ULONG STDMETHODCALLTYPE UtilExecutionEngine::Release()
{
    return 1;
}

CRITSEC_COOKIE STDMETHODCALLTYPE UtilExecutionEngine::CreateLock(LPCSTR szTag, LPCSTR level, CrstFlags flags)
{
    CRITICAL_SECTION *cs = (CRITICAL_SECTION*)malloc(sizeof(CRITICAL_SECTION));
    InitializeCriticalSection(cs);
    return (CRITSEC_COOKIE)cs;
}

void STDMETHODCALLTYPE UtilExecutionEngine::DestroyLock(CRITSEC_COOKIE lock)
{
    _ASSERTE(lock);
    DeleteCriticalSection((CRITICAL_SECTION*)lock);
    free(lock);
}

void STDMETHODCALLTYPE UtilExecutionEngine::AcquireLock(CRITSEC_COOKIE lock)
{
    _ASSERTE(lock);
    EnterCriticalSection((CRITICAL_SECTION*)lock);
}

void STDMETHODCALLTYPE UtilExecutionEngine::ReleaseLock(CRITSEC_COOKIE lock)
{
    _ASSERTE(lock);
    LeaveCriticalSection((CRITICAL_SECTION*)lock);
}

EVENT_COOKIE STDMETHODCALLTYPE UtilExecutionEngine::CreateAutoEvent(BOOL bInitialState)
{
    HANDLE handle = WszCreateEvent(NULL, FALSE, bInitialState, NULL);
    _ASSERTE(handle);
    return (EVENT_COOKIE)handle;
}

EVENT_COOKIE STDMETHODCALLTYPE UtilExecutionEngine::CreateManualEvent(BOOL bInitialState)
{
    HANDLE handle = WszCreateEvent(NULL, TRUE, bInitialState, NULL);
    _ASSERTE(handle);
    return (EVENT_COOKIE)handle;
}

void STDMETHODCALLTYPE UtilExecutionEngine::CloseEvent(EVENT_COOKIE event)
{
    _ASSERTE(event);
    CloseHandle((HANDLE)event);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrSetEvent(EVENT_COOKIE event)
{
    _ASSERTE(event);
    return SetEvent((HANDLE)event);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrResetEvent(EVENT_COOKIE event)
{
    _ASSERTE(event);
    return ResetEvent((HANDLE)event);
}

DWORD STDMETHODCALLTYPE UtilExecutionEngine::WaitForEvent(EVENT_COOKIE event, DWORD dwMilliseconds, BOOL bAlertable)
{
    _ASSERTE(event);
    return WaitForSingleObjectEx((HANDLE)event, dwMilliseconds, bAlertable);
}

DWORD STDMETHODCALLTYPE UtilExecutionEngine::WaitForSingleObject(HANDLE handle, DWORD dwMilliseconds)
{
    _ASSERTE(handle);
    return WaitForSingleObjectEx(handle, dwMilliseconds, FALSE);
}

SEMAPHORE_COOKIE STDMETHODCALLTYPE UtilExecutionEngine::ClrCreateSemaphore(DWORD dwInitial, DWORD dwMax)
{
    HANDLE handle = WszCreateSemaphore(NULL, (LONG)dwInitial, (LONG)dwMax, NULL);
    _ASSERTE(handle);
    return (SEMAPHORE_COOKIE)handle;
}

void STDMETHODCALLTYPE UtilExecutionEngine::ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore)
{
    _ASSERTE(semaphore);
    CloseHandle((HANDLE)semaphore);
}

DWORD STDMETHODCALLTYPE UtilExecutionEngine::ClrWaitForSemaphore(SEMAPHORE_COOKIE semaphore, DWORD dwMilliseconds, BOOL bAlertable)
{
    _ASSERTE(semaphore);
    return WaitForSingleObjectEx((HANDLE)semaphore, dwMilliseconds, bAlertable);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore, LONG lReleaseCount, LONG *lpPreviousCount)
{
    _ASSERTE(semaphore);
    return ReleaseSemaphore((HANDLE)semaphore, lReleaseCount, lpPreviousCount);
}

MUTEX_COOKIE STDMETHODCALLTYPE UtilExecutionEngine::ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                                                                BOOL bInitialOwner,
                                                                LPCTSTR lpName)
{
    return (MUTEX_COOKIE)WszCreateMutex(lpMutexAttributes,bInitialOwner,lpName);
}

void STDMETHODCALLTYPE UtilExecutionEngine::ClrCloseMutex(MUTEX_COOKIE mutex)
{
    _ASSERTE(mutex);
    CloseHandle((HANDLE)mutex);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrReleaseMutex(MUTEX_COOKIE mutex)
{
    _ASSERTE(mutex);
    return ReleaseMutex((HANDLE)mutex);
}

DWORD STDMETHODCALLTYPE UtilExecutionEngine::ClrWaitForMutex(MUTEX_COOKIE mutex,
                                                          DWORD dwMilliseconds,
                                                          BOOL bAlertable)
{
    _ASSERTE(mutex);
    return WaitForSingleObjectEx ((HANDLE)mutex, dwMilliseconds, bAlertable);
}

DWORD STDMETHODCALLTYPE UtilExecutionEngine::ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    return SleepEx (dwMilliseconds, bAlertable);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrAllocationDisallowed()
{
    return FALSE;
}

LPVOID STDMETHODCALLTYPE UtilExecutionEngine::ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect)
{
#ifdef FAILPOINTS_ENABLED
        if (RFS_HashStack ())
            return NULL;
#endif
    return VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType)
{
    return VirtualFree(lpAddress, dwSize, dwFreeType);
}

SIZE_T STDMETHODCALLTYPE UtilExecutionEngine::ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength)
{
    return VirtualQuery(lpAddress, lpBuffer, dwLength);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect)
{
    return VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
}

HANDLE STDMETHODCALLTYPE UtilExecutionEngine::ClrGetProcessHeap()
{
    return GetProcessHeap();
}

HANDLE STDMETHODCALLTYPE UtilExecutionEngine::ClrGetProcessExecutableHeap()
{
#ifndef CROSSGEN_COMPILE
    _ASSERTE(g_fnGetExecutableHeapHandle);
    return (g_fnGetExecutableHeapHandle != NULL) ? g_fnGetExecutableHeapHandle() : NULL;
#else
    return GetProcessHeap();
#endif
}

HANDLE STDMETHODCALLTYPE UtilExecutionEngine::ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize)
{
#ifdef TARGET_UNIX
    return NULL;
#else
    return HeapCreate(flOptions, dwInitialSize, dwMaximumSize);
#endif
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrHeapDestroy(HANDLE hHeap)
{
#ifdef TARGET_UNIX
    return FALSE;
#else
    return HeapDestroy(hHeap);
#endif
}

LPVOID STDMETHODCALLTYPE UtilExecutionEngine::ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, SIZE_T dwBytes)
{
#ifdef FAILPOINTS_ENABLED
        if (RFS_HashStack ())
            return NULL;
#endif
    return HeapAlloc(hHeap, dwFlags, dwBytes);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem)
{
    return HeapFree(hHeap, dwFlags, lpMem);
}

BOOL STDMETHODCALLTYPE UtilExecutionEngine::ClrHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem)
{
#ifdef TARGET_UNIX
    return FALSE;
#else
    return HeapValidate(hHeap, dwFlags, lpMem);
#endif
}


//------------------------------------------------------------------------------
// Helper function to get an exception from outside the exception.  In
//  the CLR, it may be from the Thread object.  Non-CLR users have no thread object,
//  and it will do nothing.

void UtilExecutionEngine::GetLastThrownObjectExceptionFromThread(void **ppvException)
{
    // Declare class so we can declare Exception**
    class Exception;

    // Cast to our real type.
    Exception **ppException = reinterpret_cast<Exception**>(ppvException);

    *ppException = NULL;
} // UtilExecutionEngine::GetLastThrownObjectExceptionFromThread

