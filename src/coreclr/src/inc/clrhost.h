// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//


#ifndef __CLRHOST_H__
#define __CLRHOST_H__

#include "windows.h" // worth to include before mscoree.h so we are guaranteed to pick few definitions
#ifdef CreateSemaphore
#undef CreateSemaphore
#endif
#include "mscoree.h"
#include "clrinternal.h"
#include "switches.h"
#include "holder.h"
#include "new.hpp"
#include "staticcontract.h"
#include "predeftlsslot.h"
#include "safemath.h"
#include "debugreturn.h"

#if !defined(_DEBUG_IMPL) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define _DEBUG_IMPL 1
#endif

#define BEGIN_PRESERVE_LAST_ERROR \
    { \
        DWORD __dwLastError = ::GetLastError(); \
        DEBUG_ASSURE_NO_RETURN_BEGIN(PRESERVE_LAST_ERROR); \
            {

#define END_PRESERVE_LAST_ERROR \
            } \
        DEBUG_ASSURE_NO_RETURN_END(PRESERVE_LAST_ERROR); \
        ::SetLastError(__dwLastError); \
    }

//
// TRASH_LASTERROR macro sets bogus last error in debug builds to help find places that fail to save it
//
#ifdef _DEBUG

#define LAST_ERROR_TRASH_VALUE 42424

#define TRASH_LASTERROR \
    SetLastError(LAST_ERROR_TRASH_VALUE)

#else // _DEBUG

#define TRASH_LASTERROR

#endif // _DEBUG

#ifndef CLR_STANDALONE_BINDER
IExecutionEngine *GetExecutionEngine();
#endif
IEEMemoryManager *GetEEMemoryManager();

LPVOID ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect);
BOOL ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType);
SIZE_T ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength);
BOOL ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect);
LPVOID ClrDebugAlloc (size_t size, LPCSTR pszFile, int iLineNo);
HANDLE ClrGetProcessHeap();
HANDLE ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize);
BOOL ClrHeapDestroy(HANDLE hHeap);
LPVOID ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, S_SIZE_T dwBytes);
BOOL ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem);
BOOL ClrHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem);
HANDLE ClrGetProcessExecutableHeap();


#ifdef FAILPOINTS_ENABLED
extern int RFS_HashStack();
#endif


void ClrFlsAssociateCallback(DWORD slot, PTLS_CALLBACK_FUNCTION callback);

typedef LPVOID* (*CLRFLSGETBLOCK)();
extern CLRFLSGETBLOCK __ClrFlsGetBlock;

#ifndef CLR_STANDALONE_BINDER
// Combining getter/setter into a single call
inline void ClrFlsIncrementValue(DWORD slot, int increment)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

    _ASSERTE(increment != 0);
    
    void **block = (*__ClrFlsGetBlock)();
    size_t value;

    if (block != NULL)
    {
        value = (size_t) block[slot];

        _ASSERTE((increment > 0) || (value + increment < value));
        block[slot] = (void *) (value + increment);
    }
    else
    {
        BEGIN_PRESERVE_LAST_ERROR;

        ANNOTATION_VIOLATION(SOToleranceViolation);

        IExecutionEngine * pEngine = GetExecutionEngine();
        value = (size_t) pEngine->TLS_GetValue(slot);

        _ASSERTE((increment > 0) || (value + increment < value));
        pEngine->TLS_SetValue(slot, (void *) (value + increment));

        END_PRESERVE_LAST_ERROR;
    }
}


inline void * ClrFlsGetValue (DWORD slot)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

	void **block = (*__ClrFlsGetBlock)();
	if (block != NULL)
    {
        return block[slot];
    }
    else
    {
        ANNOTATION_VIOLATION(SOToleranceViolation);

        void * value = GetExecutionEngine()->TLS_GetValue(slot);
        return value;
    }
}


inline BOOL ClrFlsCheckValue(DWORD slot, void ** pValue)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;

#ifdef _DEBUG
    *pValue = ULongToPtr(0xcccccccc);
#endif //_DEBUG 
	void **block = (*__ClrFlsGetBlock)();
	if (block != NULL)
    {
        *pValue = block[slot];
        return TRUE;    
    }
    else
    {
        ANNOTATION_VIOLATION(SOToleranceViolation);    
        BOOL result = GetExecutionEngine()->TLS_CheckValue(slot, pValue);
        return result;
    }
}

inline void ClrFlsSetValue(DWORD slot, void *pData)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

	void **block = (*__ClrFlsGetBlock)();
	if (block != NULL)
    {
        block[slot] = pData;
    }
    else
    {
        BEGIN_PRESERVE_LAST_ERROR;

        ANNOTATION_VIOLATION(SOToleranceViolation);
        GetExecutionEngine()->TLS_SetValue(slot, pData);

        END_PRESERVE_LAST_ERROR;
    }
}
#endif //!CLR_STANDALONE_BINDER

typedef LPVOID (*FastAllocInProcessHeapFunc)(DWORD dwFlags, SIZE_T dwBytes);
extern FastAllocInProcessHeapFunc __ClrAllocInProcessHeap;
inline LPVOID ClrAllocInProcessHeap(DWORD dwFlags, S_SIZE_T dwBytes)
{
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;
    if (dwBytes.IsOverflow())
    {
        return NULL;
    }

#ifndef SELF_NO_HOST
    return __ClrAllocInProcessHeap(dwFlags, dwBytes.Value());
#else
#undef HeapAlloc
#undef GetProcessHeap
    static HANDLE ProcessHeap = NULL;
    if (ProcessHeap == NULL)
        ProcessHeap = GetProcessHeap();
    return ::HeapAlloc(ProcessHeap,dwFlags,dwBytes.Value());
#define HeapAlloc(hHeap, dwFlags, dwBytes) Dont_Use_HeapAlloc(hHeap, dwFlags, dwBytes)
#define GetProcessHeap() Dont_Use_GetProcessHeap()
#endif
}

typedef BOOL (*FastFreeInProcessHeapFunc)(DWORD dwFlags, LPVOID lpMem);
extern FastFreeInProcessHeapFunc __ClrFreeInProcessHeap;
inline BOOL ClrFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem)
{
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;
#ifndef SELF_NO_HOST
    return __ClrFreeInProcessHeap(dwFlags, lpMem);
#else
#undef HeapFree
#undef GetProcessHeap
    static HANDLE ProcessHeap = NULL;
    if (ProcessHeap == NULL)
        ProcessHeap = GetProcessHeap();
    return (BOOL)(BYTE)::HeapFree(ProcessHeap, dwFlags, lpMem);
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)
#define GetProcessHeap() Dont_Use_GetProcessHeap()
#endif
}

// Critical section support for CLR DLLs other than the the EE.
// Include the header defining each Crst type and its corresponding level (relative rank). This is
// auto-generated from a tool that takes a high-level description of each Crst type and its dependencies.
#include "crsttypes.h"

// critical section api
CRITSEC_COOKIE ClrCreateCriticalSection(CrstType type, CrstFlags flags);
HRESULT ClrDeleteCriticalSection(CRITSEC_COOKIE cookie);
void ClrEnterCriticalSection(CRITSEC_COOKIE cookie);
void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie);

// event api
EVENT_COOKIE ClrCreateAutoEvent(BOOL bInitialState);
EVENT_COOKIE ClrCreateManualEvent(BOOL bInitialState);
void ClrCloseEvent(EVENT_COOKIE event);
BOOL ClrSetEvent(EVENT_COOKIE event);
BOOL ClrResetEvent(EVENT_COOKIE event);
DWORD ClrWaitEvent(EVENT_COOKIE event, DWORD dwMilliseconds, BOOL bAlertable);

// semaphore api
SEMAPHORE_COOKIE ClrCreateSemaphore(DWORD dwInitial, DWORD dwMax);
void ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore);
BOOL ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore, LONG lReleaseCount, LONG *lpPreviousCount);
DWORD ClrWaitSemaphore(SEMAPHORE_COOKIE semaphore, DWORD dwMilliseconds, BOOL bAlertable);

// mutex api
MUTEX_COOKIE ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,BOOL bInitialOwner,LPCTSTR lpName);
void ClrCloseMutex(MUTEX_COOKIE mutex);
BOOL ClrReleaseMutex(MUTEX_COOKIE mutex);
DWORD ClrWaitForMutex(MUTEX_COOKIE mutex,DWORD dwMilliseconds,BOOL bAlertable);
DWORD ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable);

// Rather than use the above APIs directly, it is recommended that holder classes
// be used.  This guarantees that the locks will be vacated when the scope is popped,
// either on exception or on return.

typedef Holder<CRITSEC_COOKIE, ClrEnterCriticalSection, ClrLeaveCriticalSection, NULL> CRITSEC_Holder;

// Use this holder to manage CRITSEC_COOKIE allocation to ensure it will be released if anything goes wrong
FORCEINLINE void VoidClrDeleteCriticalSection(CRITSEC_COOKIE cs) { if (cs != NULL) ClrDeleteCriticalSection(cs); }
typedef Wrapper<CRITSEC_COOKIE, DoNothing<CRITSEC_COOKIE>, VoidClrDeleteCriticalSection, NULL> CRITSEC_AllocationHolder;

class Event {
public:
    Event ()
    : m_event(NULL)
    {STATIC_CONTRACT_LEAF;}
    ~Event ()
    {
        STATIC_CONTRACT_WRAPPER;
        CloseEvent();
    }

    void CreateAutoEvent(BOOL bInitialState)
    {
        STATIC_CONTRACT_WRAPPER;
        m_event = ClrCreateAutoEvent(bInitialState);
    }
    void CreateManualEvent(BOOL bInitialState)
    {
        STATIC_CONTRACT_WRAPPER;
        m_event = ClrCreateManualEvent(bInitialState);
    }
    void CloseEvent()
    {
        STATIC_CONTRACT_WRAPPER;
        if (m_event != NULL)
            ClrCloseEvent(m_event);
        m_event = NULL;
    }

    BOOL Set()
    {
        STATIC_CONTRACT_WRAPPER;
        return ClrSetEvent(m_event);
    }
    BOOL Reset()
    {
        STATIC_CONTRACT_WRAPPER;
        return ClrResetEvent(m_event);
    }
    DWORD Wait(DWORD dwMilliseconds, BOOL bAlertable)
    {
        STATIC_CONTRACT_WRAPPER;
        return ClrWaitEvent(m_event, dwMilliseconds, bAlertable);
    }

private:
    EVENT_COOKIE m_event;
};

class Semaphore {
public:
    Semaphore ()
    : m_semaphore(NULL)
    {STATIC_CONTRACT_LEAF;}
    ~Semaphore ()
    {
        STATIC_CONTRACT_WRAPPER;
        Close();
    }

    void Create(DWORD dwInitial, DWORD dwMax)
    {
        STATIC_CONTRACT_WRAPPER;
        m_semaphore = ClrCreateSemaphore(dwInitial, dwMax);
    }
    void Close()
    {
        STATIC_CONTRACT_WRAPPER;
        if (m_semaphore != NULL)
            ClrCloseSemaphore(m_semaphore);
        m_semaphore = NULL;
    }

    BOOL Release(LONG lReleaseCount, LONG* lpPreviousCount)
    {
        STATIC_CONTRACT_WRAPPER;
        return ClrReleaseSemaphore(m_semaphore, lReleaseCount, lpPreviousCount);
    }
    DWORD Wait(DWORD dwMilliseconds, BOOL bAlertable)
    {
        STATIC_CONTRACT_WRAPPER;
        return ClrWaitSemaphore(m_semaphore, dwMilliseconds, bAlertable);
    }

private:
    SEMAPHORE_COOKIE m_semaphore;
};

#if defined(FEATURE_CORECLR) || !defined(SELF_NO_HOST) || defined(DACCESS_COMPILE)
HMODULE GetCLRModule ();
#endif // defined(FEATURE_CORECLR) || !defined(SELF_NO_HOST) || defined(DACCESS_COMPILE)

#ifndef FEATURE_NO_HOST
/*
    Here we start the list of functions we want to deprecate.
    We use a define to generate a linker error.
    We must insure to include the header file that has the definition we are about
    to deprecate before we use the #define otherwise we will run into a linker error
    when legitimately undef'ing the function
*/

//
// following are windows deprecates
//
#include <windows.h>

/*
    If you are reading this, you have probably tracked down the fact that memory Alloc,
    etc. don't work inside your DLL because you are using src\inc & src\utilcode
    services.
    You need to use the ClrXXX equivalent functions to properly guarantee your code
    works correctly wrt hosting and others execution engine requirements.
    Check the list of Clr functions above
*/

#define GetProcessHeap() \
        Dont_Use_GetProcessHeap()

#define VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect) \
        Dont_Use_VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect)

#define VirtualFree(lpAddress, dwSize, dwFreeType) \
        Dont_Use_VirtualFree(lpAddress, dwSize, dwFreeType)

#define VirtualQuery(lpAddress, lpBuffer, dwLength) \
        Dont_Use_VirtualQuery(lpAddress, lpBuffer, dwLength)

#define VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) \
        Dont_Use_VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)

#define HeapCreate(flOptions, dwInitialSize, dwMaximumSize) \
        Dont_Use_HeapCreate(flOptions, dwInitialSize, dwMaximumSize)

#define HeapDestroy(hHeap) \
        Dont_Use_HeapDestroy(hHeap)

#define HeapAlloc(hHeap, dwFlags, dwBytes) \
        Dont_Use_HeapAlloc(hHeap, dwFlags, dwBytes)

#define HeapReAlloc(hHeap, dwFlags, lpMem, dwBytes) \
        Dont_Use_HeapReAlloc(hHeap, dwFlags, lpMem, dwBytes)

#define HeapFree(hHeap, dwFlags, lpMem) \
        Dont_Use_HeapFree(hHeap, dwFlags, lpMem)

#define HeapValidate(hHeap, dwFlags, lpMem) \
        Dont_Use_HeapValidate(hHeap, dwFlags, lpMem)

#define LocalAlloc(uFlags, uBytes) \
        Dont_Use_LocalAlloc(uFlags, uBytes)

#define LocalFree(hMem) \
        Dont_Use_LocalFree(hMem)

#define LocalReAlloc(hMem, uBytes, uFlags) \
        Dont_Use_LocalReAlloc(hMem, uBytes, uFlags)

#define GlobalAlloc(uFlags, dwBytes) \
        Dont_Use_GlobalAlloc(uFlags, dwBytes)

#define GlobalFree(hMem) \
        Dont_Use_GlobalFree(hMem)

//#define ExitThread          Dont_Use_ExitThread

#define ExitProcess         Dont_Use_ExitProcess

/*
    If you are reading this, you have probably tracked down the fact that TlsAlloc,
    etc. don't work inside your DLL because you are using src\inc & src\utilcode
    services.

    This is because the CLR can operate in a fiberized environment under host control.
    When this is the case, logical thread local storage must be fiber-relative rather
    than thread-relative.

    Although the OS provides FLS routines on .NET Server, it does not yet provide
    those services on WinXP or Win2K.  So you cannot just use fiber routines from
    the OS.

    Instead, you must use the ClrFls_ routines described above.  However, there are
    some important differences between these EE-provided services and the OS TLS
    services that you are used to:

    1)  There is no TlsAlloc/FlsAlloc equivalent.  You must statically describe
        your needs via the PredefinedTlsSlots below.  If you have dynamic requirements,
        you should give yourself a single static slot and then build your dynamic
        requirements on top of this.  The lack of a dynamic API is a deliberate
        choice on my part, rather than lack of time.

    2)  You can provide a cleanup routine, which we will call on your behalf.  However,
        this can be called on a different thread than the "thread" (fiber or thread)
        which holds the data.  It can be called after that thread has actually been
        terminated.  It can be called before you see a DLL_THREAD_DETACH on your
        physical thread.  The circumstances vary based on whether the process is hosted
        and based on whether TS_WeOwn is set on the internal Thread object.  Make
        no assumptions here.
*/
#define TlsAlloc() \
        Dont_Use_TlsAlloc()

#define TlsSetValue(dwTlsIndex, lpTlsValue) \
        Dont_Use_TlsSetValue(dwTlsIndex, lpTlsValue)

#define TlsGetValue(dwTlsIndex) \
        Dont_Use_TlsGetValue(dwTlsIndex)

#define TlsFree(dwTlsIndex) \
        Dont_Use_TlsFree(dwTlsIndex)


/*
    If you are reading this, you have probably tracked down the fact that synchronization objects
    and critical sections don't work inside your DLL because you are using src\inc & src\utilcode services.
    Please refer to the ClrXXX functions described above to make proper use of synchronization obejct, etc.

    Also it's extremely useful to look at the Holder classes defined above.
    Those classes provide a nice encapsulation for synchronization object that allows automatic release of locks.
*/
#define InitializeCriticalSection(lpCriticalSection) \
        Dont_Use_InitializeCriticalSection(lpCriticalSection)

#define InitializeCriticalSectionAndSpinCount(lpCriticalSection, dwSpinCount) \
        Dont_Use_InitializeCriticalSectionAndSpinCount(lpCriticalSection, dwSpinCount)

#define DeleteCriticalSection(lpCriticalSection) \
        Dont_Use_DeleteCriticalSection(lpCriticalSection)

#define EnterCriticalSection(lpCriticalSection) \
        Dont_Use_EnterCriticalSection(lpCriticalSection)

#define TryEnterCriticalSection(lpCriticalSection) \
        Dont_Use_TryEnterCriticalSection(lpCriticalSection)

#define LeaveCriticalSection(lpCriticalSection) \
        Dont_Use_LeaveCriticalSection(lpCriticalSection)

#ifdef CreateEvent
#undef CreateEvent
#endif
#define CreateEvent(lpEventAttributes, bManualReset, bInitialState, lpName) \
        Dont_Use_CreateEvent(lpEventAttributes, bManualReset, bInitialState, lpName)

#ifdef OpenEvent
#undef OpenEvent
#endif
#define OpenEvent(dwDesiredAccess, bInheritHandle, lpName) \
        Dont_Use_OpenEvent(dwDesiredAccess, bInheritHandle, lpName)

#define ResetEvent(hEvent) \
        Dont_Use_ResetEvent(hEvent)

#define SetEvent(hEvent) \
        Dont_Use_SetEvent(hEvent)

#define PulseEvent(hEvent) \
        Dont_Use_PulseEvent(hEvent)

#ifdef CreateSemaphore
#undef CreateSemaphore
#endif
#define CreateSemaphore(lpSemaphoreAttributes, lInitialCount, lMaximumCount, lpName) \
        Dont_Use_CreateSemaphore(lpSemaphoreAttributes, lInitialCount, lMaximumCount, lpName)

#ifdef OpenSemaphore
#undef OpenSemaphore
#endif
#define OpenSemaphore(dwDesiredAccess, bInheritHandle, lpName) \
        Dont_Use_OpenSemaphore(dwDesiredAccess, bInheritHandle, lpName)

#define ReleaseSemaphore(hSemaphore, lReleaseCount, lpPreviousCount) \
        Dont_Use_ReleaseSemaphore(hSemaphore, lReleaseCount, lpPreviousCount)

#ifdef Sleep
#undef Sleep
#endif
#define Sleep(dwMilliseconds) \
        Dont_Use_Sleep(dwMilliseconds)

#ifdef SleepEx
#undef SleepEx
#endif
#define SleepEx(dwMilliseconds,bAlertable) \
        Dont_Use_SleepEx(dwMilliseconds,bAlertable)

//
// following are clib deprecates
//
#include <stdlib.h>
#include <malloc.h>

#ifdef malloc
#undef malloc
#endif

#define _CRT_EXCEPTION_NO_MALLOC
#define malloc(size) \
        Dont_Use_malloc(size)

#ifdef realloc
#undef realloc
#endif
#define realloc(memblock, size) \
        Dont_Use_realloc(memblock, size)

#ifdef free
#undef free
#endif
#define free(memblock) \
        Dont_Use_free(memblock)

#endif //!FEATURE_NO_HOST

extern void IncCantAllocCount();
extern void DecCantAllocCount();

class CantAllocHolder
{
public:
    CantAllocHolder ()
    {
        IncCantAllocCount ();
    }
    ~CantAllocHolder()
    {
	    DecCantAllocCount ();
    }
};

// At places where want to allocate stress log, we need to first check if we are allowed to do so.
// If ClrTlsInfo doesn't exist for this thread, we take it as can alloc
#ifndef CLR_STANDALONE_BINDER
inline bool IsInCantAllocRegion ()
{
    size_t count = 0;
    if (ClrFlsCheckValue(TlsIdx_CantAllocCount, (LPVOID *)&count))
    {        
        _ASSERTE (count >= 0);
        return count > 0;
    }
    return false;
}
// for stress log the rule is more restrict, we have to check the global counter too
extern BOOL IsInCantAllocStressLogRegion();
#endif // !CLR_STANDALONE_BINDER

#include "genericstackprobe.inl"

#endif
