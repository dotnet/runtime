// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//


#include "common.h"

#include "hosting.h"
#include "mscoree.h"
#include "mscoreepriv.h"
#include "corhost.h"
#include "threads.h"

#if defined(FEATURE_CLICKONCE)
#include "isolationpriv.h"
#include "shlwapi.h"
#endif

#define countof(x) (sizeof(x) / sizeof(x[0]))

//Copied from winbase.h
#ifndef STARTF_TITLEISAPPID
#define STARTF_TITLEISAPPID     	  0x00001000
#endif
#ifndef STARTF_PREVENTPINNING
#define STARTF_PREVENTPINNING    0x00002000
#endif

//Flags encoded in the first parameter of CorLaunchApplication.
#define MASK_NOTPINNABLE 	0x80000000
#define MASK_HOSTTYPE 		0x00000003
#define MASK_DONT_SHOW_INSTALL_DIALOG 	0x00000100

#ifdef _DEBUG
// This function adds a static annotation read by SCAN to indicate HOST_CALLS. Its
// purpose is to be called from the BEGIN_SO_TOLERANT_CODE_CALLING_HOST macro, to
// effectively mark all functions that use BEGIN_SO_TOLERANT_CODE_CALLING_HOST as being
// HOST_CALLS. If you hit a SCAN violation that references AddHostCallsStaticMarker, then
// you have a function marked as HOST_NOCALLS that eventually calls into a function that
// uses BEGIN_SO_TOLERANT_CODE_CALLING_HOST.
DEBUG_NOINLINE void AddHostCallsStaticMarker()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_HOST_CALLS;

    METHOD_CANNOT_BE_FOLDED_DEBUG;
}
#endif  //_DEBUG

//
// memory management functions
//

// global debug only tracking utilities
#ifdef _DEBUG

static const LONG MaxGlobalAllocCount = 8;

class GlobalAllocStore {
public:
    static void AddAlloc (LPVOID p)
    {
        LIMITED_METHOD_CONTRACT;

        if (!p) {
            return;
        }
        if (m_Disabled) {
            return;
        }

        //InterlockedIncrement (&numMemWriter);
        //if (CheckMemFree) {
        //    goto Return;
        //}

        //m_Count is number of allocation we've ever tried, it's OK to be bigger than
        //size of m_Alloc[]
        InterlockedIncrement (&m_Count);

        //this is by no means an accurate record of heap allocation.
        //the algorithm used here can't guarantee an allocation is saved in
        //m_Alloc[] even there's enough free space. However this is only used
        //for debugging purpose and most importantly, m_Count is accurate.
        for (size_t n = 0; n < countof(m_Alloc); n ++) {
            if (m_Alloc[n] == 0) {
                if (InterlockedCompareExchangeT(&m_Alloc[n],p,0) == 0) {
                    return;
                }
            }
        }
        
        //InterlockedDecrement (&numMemWriter);
    }

    //this is called in non-host case where we don't care the free after
    //alloc store is disabled
    static BOOL RemoveAlloc (LPVOID p)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_Disabled)
        {
            return TRUE;
        }
        //decrement the counter even we might not find the allocation
        //in m_Alloc. Because it's possible for an allocation not to be saved
        //in the array
        InterlockedDecrement (&m_Count);
        // Binary search        
        for (size_t n = 0; n < countof(m_Alloc); n ++) {
            if (m_Alloc[n] == p) {
                m_Alloc[n] = 0;
                return TRUE;
            }
        }
        return FALSE;
    }

    //this is called in host case where if the store is disabled, we want to 
    //guarantee we don't try to free anything the host doesn't know about
    static void ValidateFree(LPVOID p)
    {
        LIMITED_METHOD_CONTRACT;

        if (p == 0) {
            return;
        }
        if (m_Disabled) {
            for (size_t n = 0; n < countof(m_Alloc); n ++) {
                //there could be miss, because an allocation might not be saved
                //in the array
                if (m_Alloc[n] == p) {
                    _ASSERTE (!"Free a memory that host interface does not know");
                    return;
                }
            }
        }
    }

    static void Validate()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_Count > MaxGlobalAllocCount) {
            _ASSERTE (!"Using too many memory allocator before Host Interface is set up");
        }       
        
        //while (numMemWriter != 0) {
        //    Sleep(5);
        //}
        //qsort (GlobalMemAddr, (MemAllocCount>MaxAllocCount)?MaxAllocCount:MemAllocCount, sizeof(LPVOID), MemAddrCompare);
    }

    static void Disable ()
    {
        LIMITED_METHOD_CONTRACT;
        if (!m_Disabled) 
        {
            // Let all threads know
            InterlockedIncrement((LONG*)&m_Disabled);
        }
    }

private:
    static BOOL m_Disabled;    
    static LPVOID m_Alloc[MaxGlobalAllocCount];
    //m_Count is number of allocation we tried, it's legal to be bigger than
    //size of m_Alloc[]
    static LONG m_Count;
    // static LONG numMemWriter = 0;
};

// used from corhost.cpp
void ValidateHostInterface()
{
    WRAPPER_NO_CONTRACT;

    GlobalAllocStore::Validate();
    GlobalAllocStore::Disable();    
}

void DisableGlobalAllocStore ()
{
    WRAPPER_NO_CONTRACT;
    GlobalAllocStore::Disable();
}
LPVOID GlobalAllocStore::m_Alloc[MaxGlobalAllocCount];
LONG GlobalAllocStore::m_Count = 0;
BOOL GlobalAllocStore::m_Disabled = FALSE;

#endif

#if defined(_DEBUG) && !defined(FEATURE_CORECLR)
// The helper thread can't call regular new / delete b/c of interop-debugging deadlocks.
// It must use the (InteropSafe) heap from debugger.h, you also can't allocate normally
// when we have any other thread hard-suspended.

// Telesto doesn't support interop-debugging, so this won't be an issue.

void AssertAllocationAllowed();
#endif


HANDLE g_ExecutableHeapHandle = NULL;

#undef VirtualAlloc
LPVOID EEVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifdef FAILPOINTS_ENABLED
        if (RFS_HashStack ())
            return NULL;
#endif

#if defined(_DEBUG) && !defined(FEATURE_CORECLR)
    AssertAllocationAllowed();
#endif

#ifdef _DEBUG
        if (g_fEEStarted) {
            _ASSERTE (!EEAllocationDisallowed());
        }
        _ASSERTE (lpAddress || (dwSize % g_SystemInfo.dwAllocationGranularity) == 0);
#endif

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMemoryManager *pMM = CorHost2::GetHostMemoryManager();
    if (pMM) {
        LPVOID pMem;
        EMemoryCriticalLevel eLevel = eTaskCritical;
        if (!g_fEEStarted)
        {
            eLevel = eProcessCritical;
        }
        else
        {
            Thread *pThread = GetThread();
            if (pThread && pThread->HasLockInCurrentDomain())
            {
                if (GetAppDomain()->IsDefaultDomain())
                {
                    eLevel = eProcessCritical;
                }
                else
                {
                    eLevel = eAppDomainCritical;
                }
            }
        }
        HRESULT hr = S_OK;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pMM->VirtualAlloc (lpAddress, dwSize, flAllocationType, flProtect, eLevel, &pMem);
        END_SO_TOLERANT_CODE_CALLING_HOST;

        if(hr != S_OK)
        {
            STRESS_LOG_OOM_STACK(dwSize);
        }

        return (hr == S_OK) ? pMem : NULL;
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {

        LPVOID p = NULL;

#ifdef _DEBUG
        {
            DEBUG_ONLY_REGION();

        if (lpAddress == NULL && (flAllocationType & MEM_RESERVE) != 0 && PEDecoder::GetForceRelocs())
        {
#ifdef _WIN64
            // Try to allocate memory all over the place when we are stressing relocations on _WIN64.
            // This will make sure that we generate jump stubs correctly among other things.
            static BYTE* ptr = (BYTE*)0x234560000;
            ptr += 0x123450000;
            // Wrap around
            if (ptr < (BYTE *)BOT_MEMORY || ptr > (BYTE *)TOP_MEMORY) 
            {
                // Make sure to keep the alignment of the ptr so that we are not 
                // trying the same places over and over again
                ptr = (BYTE*)BOT_MEMORY + (((SIZE_T)ptr) & 0xFFFFFFFF);
            }
            p = ::VirtualAlloc(ptr, dwSize, flAllocationType, flProtect);
#else
            // Allocate memory top to bottom to stress ngen fixups with LARGEADDRESSAWARE support.
            p = ::VirtualAlloc(lpAddress, dwSize, flAllocationType | MEM_TOP_DOWN, flProtect);
#endif // _WIN64
        }
        }
#endif // _DEBUG

        // Fall back to the default method if the forced relocation failed
        if (p == NULL)
        {
            p = ::VirtualAlloc (lpAddress, dwSize, flAllocationType, flProtect);
        }

#ifdef _DEBUG
        GlobalAllocStore::AddAlloc (p);
#endif

        if(p == NULL){
             STRESS_LOG_OOM_STACK(dwSize);
        }

        return p;
    }

}
#define VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect) Dont_Use_VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect)

#undef VirtualFree
BOOL EEVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    BOOL retVal = FALSE;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMemoryManager *pMM = CorHost2::GetHostMemoryManager();
    if (pMM) {
#ifdef _DEBUG
        GlobalAllocStore::ValidateFree(lpAddress);
#endif

        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        retVal = pMM->VirtualFree (lpAddress, dwSize, dwFreeType) == S_OK;
        END_SO_TOLERANT_CODE_CALLING_HOST;
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
#ifdef _DEBUG
        GlobalAllocStore::RemoveAlloc (lpAddress);
#endif

        retVal = (BOOL)(BYTE)::VirtualFree (lpAddress, dwSize, dwFreeType);
    }

    return retVal;
}
#define VirtualFree(lpAddress, dwSize, dwFreeType) Dont_Use_VirtualFree(lpAddress, dwSize, dwFreeType)

#undef VirtualQuery
SIZE_T EEVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMemoryManager *pMM = CorHost2::GetHostMemoryManager();
    if (pMM) {
        SIZE_T result;
        HRESULT hr = S_OK;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pMM->VirtualQuery((void*)lpAddress, lpBuffer, dwLength, &result);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (FAILED(hr))
            return 0;
        return result;
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return ::VirtualQuery(lpAddress, lpBuffer, dwLength);
    }
}
#define VirtualQuery(lpAddress, lpBuffer, dwLength) Dont_Use_VirtualQuery(lpAddress, lpBuffer, dwLength)

#undef VirtualProtect
BOOL EEVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMemoryManager *pMM = CorHost2::GetHostMemoryManager();
    if (pMM) {
        BOOL result = FALSE;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        result = pMM->VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) == S_OK;
        END_SO_TOLERANT_CODE_CALLING_HOST;
        return result;
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return ::VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
    }
}
#define VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) Dont_Use_VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)

#undef GetProcessHeap
HANDLE EEGetProcessHeap() 
{
    // Note: this can be called a little early for real contracts, so we use static contracts instead.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMemoryManager *pMM = CorHost2::GetHostMemoryManager();
    if (pMM) {
        return (HANDLE)1; // pretending we return an handle is ok because handles are ignored by the hosting api
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return GetProcessHeap();
    }
}
#define GetProcessHeap() Dont_Use_GetProcessHeap()

#undef HeapCreate
HANDLE EEHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifndef FEATURE_PAL
    
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMalloc *pHM = CorHost2::GetHostMalloc();
    if (pHM)
    {
        return NULL;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return ::HeapCreate(flOptions, dwInitialSize, dwMaximumSize);
    }
#else // !FEATURE_PAL
    return NULL;
#endif // !FEATURE_PAL
}
#define HeapCreate(flOptions, dwInitialSize, dwMaximumSize) Dont_Use_HeapCreate(flOptions, dwInitialSize, dwMaximumSize)

#undef HeapDestroy
BOOL EEHeapDestroy(HANDLE hHeap) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifndef FEATURE_PAL

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMalloc *pHM = CorHost2::GetHostMalloc();
    if (pHM)
    {
        return TRUE;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return ::HeapDestroy(hHeap);
    }
#else // !FEATURE_PAL
    UNREACHABLE();
#endif // !FEATURE_PAL
}
#define HeapDestroy(hHeap) Dont_Use_HeapDestroy(hHeap)

#ifdef _DEBUG
#ifdef _TARGET_X86_
#define OS_HEAP_ALIGN 8
#else
#define OS_HEAP_ALIGN 16
#endif
#endif

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
LPVOID EEHeapAllocHosted(IHostMalloc * pHM, SIZE_T dwBytes)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SO_INTOLERANT;

    Thread * pThread = GetThreadNULLOk();
    EMemoryCriticalLevel eLevel = eTaskCritical;
    if (!g_fEEStarted)
    {
        eLevel = eProcessCritical;
    }
    else
    {
        if (pThread && pThread->HasLockInCurrentDomain())
        {
            if (GetAppDomain()->IsDefaultDomain())
            {
                eLevel = eProcessCritical;
            }
            else
            {
                eLevel = eAppDomainCritical;
            }
        }
    }
    LPVOID pMem = NULL;
    HRESULT hr = S_OK;
    {
        CantAllocHolder caHolder;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(pThread);
        hr = pHM->Alloc(dwBytes, eLevel, &pMem);
        END_SO_TOLERANT_CODE_CALLING_HOST;
    }

    if(hr != S_OK 
        //under OOM, we might not be able to get Execution Engine and can't access stress log
        && GetExecutionEngine ()
        // If we have not created StressLog ring buffer, we should not try to use it.
        // StressLog is going to do a memory allocation.  We may enter an endless loop.
        && ClrFlsGetValue(TlsIdx_StressLog) != NULL )
    {
        STRESS_LOG_OOM_STACK(dwBytes);
    }

    return (hr == S_OK) ? pMem : NULL;
}
#endif // FEATURE_INCLUDE_ALL_INTERFACES

#undef HeapAlloc
LPVOID EEHeapAlloc(HANDLE hHeap, DWORD dwFlags, SIZE_T dwBytes) 
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SO_INTOLERANT;

#ifdef FAILPOINTS_ENABLED
    if (RFS_HashStack ())
        return NULL;
#endif

#if defined(_DEBUG) && !defined(FEATURE_CORECLR)
    AssertAllocationAllowed();
#endif

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMalloc *pHM = CorHost2::GetHostMalloc();

    // TODO: implement hosted executable heap
    if (pHM && hHeap != g_ExecutableHeapHandle)
    {
        return EEHeapAllocHosted(pHM, dwBytes);
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES    
    {

        LPVOID p = NULL;
#ifdef _DEBUG
        // Store the heap handle to detect heap contamination
        p = ::HeapAlloc (hHeap, dwFlags, dwBytes + OS_HEAP_ALIGN);
        if(p)
        {
            *((HANDLE*)p) = hHeap;
            p = (BYTE*)p + OS_HEAP_ALIGN;
        }
        GlobalAllocStore::AddAlloc (p);
#else
        p = ::HeapAlloc (hHeap, dwFlags, dwBytes);
#endif

        if(p == NULL
            //under OOM, we might not be able to get Execution Engine and can't access stress log
            && GetExecutionEngine ()
           // If we have not created StressLog ring buffer, we should not try to use it.
           // StressLog is going to do a memory allocation.  We may enter an endless loop.
           && ClrFlsGetValue(TlsIdx_StressLog) != NULL )
        {
            STRESS_LOG_OOM_STACK(dwBytes);
        }

        return p;
    }
}
#define HeapAlloc(hHeap, dwFlags, dwBytes) Dont_Use_HeapAlloc(hHeap, dwFlags, dwBytes)

LPVOID EEHeapAllocInProcessHeap(DWORD dwFlags, SIZE_T dwBytes)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    static HANDLE ProcessHeap = NULL;

    // We need to guarentee a very small stack consumption in allocating.  And we can't allow
    // an SO to happen while calling into the host.  This will force a hard SO which is OK because
    // we shouldn't ever get this close inside the EE in SO-intolerant code, so this should
    // only fail if we call directly in from outside the EE, such as the JIT.
    MINIMAL_STACK_PROBE_CHECK_THREAD(GetThread());

    if (ProcessHeap == NULL)
        ProcessHeap = EEGetProcessHeap();

    return EEHeapAlloc(ProcessHeap,dwFlags,dwBytes);
}

#undef HeapFree
BOOL EEHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;

    // @todo -  Need a backout validation here.
    CONTRACT_VIOLATION(SOToleranceViolation);

#if defined(_DEBUG) && !defined(FEATURE_CORECLR)
    AssertAllocationAllowed();
#endif

    BOOL retVal = FALSE;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMalloc *pHM = CorHost2::GetHostMalloc();

    // TODO: implement hosted executable heap
    if (pHM && hHeap != g_ExecutableHeapHandle)
    {
        if (lpMem == NULL) {
            retVal = TRUE;
        }
#ifdef _DEBUG
        GlobalAllocStore::ValidateFree(lpMem);
#endif
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        retVal = pHM->Free(lpMem) == S_OK;
        END_SO_TOLERANT_CODE_CALLING_HOST;
    }
    else 
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
#ifdef _DEBUG
        GlobalAllocStore::RemoveAlloc (lpMem);

        // Check the heap handle to detect heap contamination
        lpMem = (BYTE*)lpMem - OS_HEAP_ALIGN;
        HANDLE storedHeapHandle = *((HANDLE*)lpMem);
        if(storedHeapHandle != hHeap)
            _ASSERTE(!"Heap contamination detected! HeapFree was called on a heap other than the one that memory was allocated from.\n"
                      "Possible cause: you used new (executable) to allocate the memory, but didn't use DeleteExecutable() to free it.");
#endif
        // DON'T REMOVE THIS SEEMINGLY USELESS CAST
        //
        // On AMD64 the OS HeapFree calls RtlFreeHeap which returns a 1byte
        // BOOLEAN, HeapFree then doesn't correctly clean the return value
        // so the other 3 bytes which come back can be junk and in that case
        // this return value can never be false.
        retVal =  (BOOL)(BYTE)::HeapFree (hHeap, dwFlags, lpMem);
    }

    return retVal;
}
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)

BOOL EEHeapFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take a look at comment in EEHeapFree and EEHeapAllocInProcessHeap, obviously someone
    // needs to take a little time to think more about this code.
    //CONTRACT_VIOLATION(SOToleranceViolation);

    static HANDLE ProcessHeap = NULL;

    if (ProcessHeap == NULL)
        ProcessHeap = EEGetProcessHeap();

    return EEHeapFree(ProcessHeap,dwFlags,lpMem);
}


#undef HeapValidate
BOOL EEHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem) {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifndef FEATURE_PAL

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostMalloc *pHM = CorHost2::GetHostMalloc();
    if (pHM)
    {
        return TRUE;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return ::HeapValidate(hHeap, dwFlags, lpMem);
    }
#else // !FEATURE_PAL
    return TRUE;
#endif // !FEATURE_PAL
}
#define HeapValidate(hHeap, dwFlags, lpMem) Dont_Use_HeapValidate(hHeap, dwFlags, lpMem)

HANDLE EEGetProcessExecutableHeap() {
    // Note: this can be called a little early for real contracts, so we use static contracts instead.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef FEATURE_CORECLR

#ifndef FEATURE_PAL

    //
    // Create the executable heap lazily
    //
#undef HeapCreate
#undef HeapDestroy
    if (g_ExecutableHeapHandle == NULL)
    {

        HANDLE ExecutableHeapHandle = HeapCreate(
                                    HEAP_CREATE_ENABLE_EXECUTE,                 // heap allocation attributes
                                    0,                                          // initial heap size
                                    0                                           // maximum heap size; 0 == growable
                                    );

        if (ExecutableHeapHandle == NULL)
            return NULL;

        HANDLE ExistingValue = InterlockedCompareExchangeT(&g_ExecutableHeapHandle, ExecutableHeapHandle, NULL);
        if (ExistingValue != NULL)
        {
            HeapDestroy(ExecutableHeapHandle);
        }
    }

#define HeapCreate(flOptions, dwInitialSize, dwMaximumSize) Dont_Use_HeapCreate(flOptions, dwInitialSize, dwMaximumSize)
#define HeapDestroy(hHeap) Dont_Use_HeapDestroy(hHeap)

#else // !FEATURE_PAL
    UNREACHABLE();
#endif // !FEATURE_PAL

#else // FEATURE_CORECLR

    //
    // Use process executable heap created by the shim
    //
    if (g_ExecutableHeapHandle == NULL)
    {
        extern HANDLE GetProcessExecutableHeap();
        g_ExecutableHeapHandle = GetProcessExecutableHeap();
    }

#endif // FEATURE_CORECLR

    // TODO: implement hosted executable heap
    return g_ExecutableHeapHandle;
}


#undef SleepEx
#undef Sleep
DWORD EESleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    DWORD res;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostTaskManager *provider = CorHost2::GetHostTaskManager();
    if ((provider != NULL)){
        DWORD option = 0;
        if (bAlertable)
        {
            option = WAIT_ALERTABLE;
        }


        HRESULT hr;

        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = provider->Sleep(dwMilliseconds, option);
        END_SO_TOLERANT_CODE_CALLING_HOST;

        if (hr == S_OK) {
            res = WAIT_OBJECT_0;
        }
        else if (hr == HOST_E_INTERRUPTED) {
            _ASSERTE(bAlertable);
            Thread *pThread = GetThread();
            if (pThread)
            {
                pThread->UserInterruptAPC(APC_Code);
            }
            res = WAIT_IO_COMPLETION;
        }
        else
        {
            _ASSERTE (!"Unknown return from host Sleep\n");
            res = WAIT_OBJECT_0;
        }
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        res = ::SleepEx(dwMilliseconds, bAlertable);
    }

    return res;
}
#define SleepEx(dwMilliseconds,bAlertable) \
        Dont_Use_SleepEx(dwMilliseconds,bAlertable)
#define Sleep(a) Dont_Use_Sleep(a)

// non-zero return value if this function causes the OS to switch to another thread
// See file:spinlock.h#SwitchToThreadSpinning for an explanation of dwSwitchCount
BOOL __SwitchToThread (DWORD dwSleepMSec, DWORD dwSwitchCount)
{
  CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
	
    return  __DangerousSwitchToThread(dwSleepMSec, dwSwitchCount, FALSE);
}

#undef SleepEx
BOOL __DangerousSwitchToThread (DWORD dwSleepMSec, DWORD dwSwitchCount, BOOL goThroughOS)
{
    // If you sleep for a long time, the thread should be in Preemptive GC mode.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        PRECONDITION(dwSleepMSec < 10000 || GetThread() == NULL || !GetThread()->PreemptiveGCDisabled());
    }
    CONTRACTL_END;

    if (CLRTaskHosted())
    {
        Thread *pThread = GetThread();
        if (pThread && pThread->HasThreadState(Thread::TS_YieldRequested))
        {
            pThread->ResetThreadState(Thread::TS_YieldRequested);
        }
    }

    if (dwSleepMSec > 0)
    {
        // when called with goThroughOS make sure to not call into the host. This function
        // may be called from GetRuntimeFunctionCallback() which is called by the OS to determine
        // the personality routine when it needs to unwind managed code off the stack. when this
        // happens in the context of an SO we want to avoid calling into the host
        if (goThroughOS)
            ::SleepEx(dwSleepMSec, FALSE);
        else
            ClrSleepEx(dwSleepMSec,FALSE);
        return TRUE;
    }

    // In deciding when to insert sleeps, we wait until we have been spinning
    // for a long time and then always sleep.  The former is to let short perf-critical
    // __SwitchToThread loops avoid context switches.  The latter is to ensure
    // that if many threads are spinning waiting for a lower-priority thread
    // to run that they will eventually all be asleep at the same time.
    // 
    // The specific values are derived from the NDP 2.0 SP1 fix: it waits for 
    // 8 million cycles of __SwitchToThread calls where each takes ~300-500,
    // which means we should wait in the neighborhood of 25000 calls.
    // 
    // As of early 2011, ARM CPUs are much slower, so we need a lower threshold.
    // The following two values appear to yield roughly equivalent spin times
    // on their respective platforms.
    //
#ifdef _TARGET_ARM_
    #define SLEEP_START_THRESHOLD (5 * 1024)
#else
    #define SLEEP_START_THRESHOLD (32 * 1024)
#endif

    _ASSERTE(CALLER_LIMITS_SPINNING < SLEEP_START_THRESHOLD);
    if (dwSwitchCount >= SLEEP_START_THRESHOLD)
    {
        if (goThroughOS)
            ::SleepEx(1, FALSE);
        else
            ClrSleepEx(1, FALSE);
    }

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostTaskManager *provider = CorHost2::GetHostTaskManager();
    if ((provider != NULL) && (goThroughOS == FALSE))
    {
        DWORD option = 0;

        HRESULT hr;

        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = provider->SwitchToTask(option);
        END_SO_TOLERANT_CODE_CALLING_HOST;

        return hr == S_OK;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        return SwitchToThread();
    }
}
#define SleepEx(dwMilliseconds,bAlertable) \
        Dont_Use_SleepEx(dwMilliseconds,bAlertable)

// Locking routines supplied by the EE to the other DLLs of the CLR.  In a _DEBUG
// build of the EE, we poison the Crst as a poor man's attempt to do some argument
// validation.
#define POISON_BITS 3

static inline CRITSEC_COOKIE CrstToCookie(Crst * pCrst) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((((uintptr_t) pCrst) & POISON_BITS) == 0);
#ifdef _DEBUG
    if (pCrst)
    {
    pCrst = (Crst *) (((uintptr_t) pCrst) | POISON_BITS);
    }
#endif
    return (CRITSEC_COOKIE) pCrst;
}

static inline Crst *CookieToCrst(CRITSEC_COOKIE cookie) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    cookie = (CRITSEC_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
#endif
    return (Crst *) cookie;
}

CRITSEC_COOKIE EECreateCriticalSection(CrstType crstType, CrstFlags flags) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CRITSEC_COOKIE ret = NULL;

    EX_TRY
    {
    // This may be controversial, but seems like the correct discipline.  If the
    // EE has called out to any other DLL of the CLR in cooperative mode, we
    // arbitrarily force lock acquisition to occur in preemptive mode.  See our
    // treatment of AcquireLock below.
    //_ASSERTE((flags & (CRST_UNSAFE_COOPGC | CRST_UNSAFE_ANYMODE)) == 0);
        ret = CrstToCookie(new Crst(crstType, flags));
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    // Note: we'll return NULL if the create fails. That's a true NULL, not a poisoned NULL.
    return ret;
}

void EEDeleteCriticalSection(CRITSEC_COOKIE cookie)
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_NOTRIGGER);
        SO_TOLERANT;
    }
    CONTRACTL_END;

    VALIDATE_BACKOUT_STACK_CONSUMPTION;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    delete pCrst;
}

DEBUG_NOINLINE void EEEnterCriticalSection(CRITSEC_COOKIE cookie) {

    // Entering a critical section has many different contracts
    // depending on the flags used to initialize the critical section.
    // See CrstBase::Enter() for the actual contract. It's much too
    // complex to repeat here.

    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    pCrst->Enter();
}

DEBUG_NOINLINE void EELeaveCriticalSection(CRITSEC_COOKIE cookie)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    pCrst->Leave();
}

LPVOID EETlsGetValue(DWORD slot)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

    //
    // @todo: we don't want TlsGetValue to throw, but CheckThreadState throws right now. Either modify
    // CheckThreadState to not throw, or catch any exception and just return NULL.
    //
    //CONTRACT_VIOLATION(ThrowsViolation);
    SCAN_IGNORE_THROW;

    void **pTlsData = CExecutionEngine::CheckThreadState(slot, FALSE);

    if (pTlsData)
        return pTlsData[slot];
    else
        return NULL;
}

BOOL EETlsCheckValue(DWORD slot, LPVOID * pValue)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;

    //
    // @todo: we don't want TlsGetValue to throw, but CheckThreadState throws right now. Either modify
    // CheckThreadState to not throw, or catch any exception and just return NULL.
    //
    //CONTRACT_VIOLATION(ThrowsViolation);
    SCAN_IGNORE_THROW;

    void **pTlsData = CExecutionEngine::CheckThreadState(slot, FALSE);

    if (pTlsData)
    {
        *pValue = pTlsData[slot];
        return TRUE;
    }

    return FALSE;
}

VOID EETlsSetValue(DWORD slot, LPVOID pData)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;

    void **pTlsData = CExecutionEngine::CheckThreadState(slot);

    if (pTlsData)  // Yes, CheckThreadState(slot, TRUE) can return NULL now.
    {
        pTlsData[slot] = pData;
    }
}

BOOL EEAllocationDisallowed()
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    // On Debug build we make sure that a thread is not going to do memory allocation
    // after it suspends another thread, since the another thread may be suspended while
    // having OS Heap lock.
    return !Thread::Debug_AllowCallout();
#else
    return FALSE;
#endif
}

#ifdef FEATURE_CLICKONCE

HRESULT GetApplicationManifest (LPCWSTR pwzAppFullName,
                                DWORD dwManifestPaths,
                                LPCWSTR *ppwzManifestPaths,
                                __out_z __deref_out_opt LPWSTR *ppwzApplicationFolderPath,
				    __out_z __deref_out_opt LPWSTR *ppszKeyForm,                                                                
                                ICMS **ppApplicationManifest)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pwzAppFullName));
        PRECONDITION(CheckPointer(ppwzManifestPaths, NULL_OK));
        PRECONDITION(CheckPointer(ppApplicationManifest));
    } CONTRACTL_END;

    ReleaseHolder<IStore> pStore(NULL);
    ReleaseHolder<IAppIdAuthority> pAppIdAuth(NULL);
    ReleaseHolder<IDefinitionAppId> pDefinitionIdentity(NULL);
    ReleaseHolder<IEnumDefinitionIdentity> pEnumDefinitionIdentity(NULL);
    ReleaseHolder<IDefinitionIdentity> pDeploymentDefinitionIdentity(NULL);
    ReleaseHolder<IDefinitionIdentity> pApplicationDefinitionIdentity(NULL);
    ReleaseHolder<IDefinitionIdentity> pSubscriptionIdentity(NULL);	
    ReleaseHolder<IDefinitionAppId> pSubscriptionAppId(NULL);

    ReleaseHolder<IUnknown> TempFetched(NULL);
    HRESULT hr = S_OK;

    // Maybe this is not an installed application. Grab the manifest path if specified and parse the manifest.
    if (dwManifestPaths > 0) {
        if (dwManifestPaths < 2)
            goto ErrExit;

        hr = ParseManifest(ppwzManifestPaths[1], NULL, __uuidof(ICMS), &TempFetched);
        if (TempFetched == NULL)
        {
            goto ErrExit;
        }

        IfFailGo(TempFetched->QueryInterface(__uuidof(ICMS), (void**) ppApplicationManifest));
        TempFetched.Release();

        // Set the application directory to be the location of the application manifest.
        if (ppwzApplicationFolderPath) {
            LPCWSTR pszSlash;
            if (((pszSlash = wcsrchr(ppwzManifestPaths[1], W('\\'))) != NULL) || ((pszSlash = wcsrchr(ppwzManifestPaths[1], W('/'))) != NULL)) {
                DWORD cchDirectory = (DWORD) (pszSlash - ppwzManifestPaths[1] + 1);
                *ppwzApplicationFolderPath = (LPWSTR) CoTaskMemAlloc(2 * (cchDirectory + 1));

                if (*ppwzApplicationFolderPath == NULL)
                {
                    hr = E_OUTOFMEMORY;
                    goto ErrExit;
                }

                memcpy(*ppwzApplicationFolderPath, ppwzManifestPaths[1], 2 * cchDirectory);
                (*ppwzApplicationFolderPath)[cchDirectory] = W('\0');
            }
        }
        goto ErrExit;
    }

    // Get the user store.
    IfFailGo(GetUserStore(0, NULL, __uuidof(IStore), &pStore));

    // Get the AppId authority
    IfFailGo(GetAppIdAuthority(&pAppIdAuth));

    // Get the IDefintionIdentity of the application full name passed in as an argument.
    IfFailGo(pAppIdAuth->TextToDefinition(0, pwzAppFullName, &pDefinitionIdentity));

    // Get the ICMS object representing the application manifest.
    IfFailGo(pDefinitionIdentity->EnumAppPath(&pEnumDefinitionIdentity));
    IfFailGo(pEnumDefinitionIdentity->Reset());
    ULONG numItems = 0;
    IfFailGo(pEnumDefinitionIdentity->Next(1, &pDeploymentDefinitionIdentity, &numItems));
    if (numItems < 1) {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        goto ErrExit;
    }
    IfFailGo(pEnumDefinitionIdentity->Next(1, &pApplicationDefinitionIdentity, &numItems));
    if (numItems < 1) {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        goto ErrExit;
    }

   if (ppszKeyForm){
	    // Create subscription identity from deployment identity.
	    IfFailGo(pDeploymentDefinitionIdentity->Clone(0,NULL,&pSubscriptionIdentity));
	    IfFailGo(pSubscriptionIdentity->SetAttribute(NULL,W("version"),NULL));
		
	    // Create the subscription app id.
	    IfFailGo(pAppIdAuth->CreateDefinition(&pSubscriptionAppId));
		
	    IDefinitionIdentity *defIdentityArray[1];
	    defIdentityArray[0] = pSubscriptionIdentity;	

	    IfFailGo(pSubscriptionAppId->SetAppPath(1,defIdentityArray));   
	    IfFailGo(pAppIdAuth->GenerateDefinitionKey(0,pSubscriptionAppId,ppszKeyForm));
   }  
	
    hr = pStore->GetAssemblyInformation(0, pApplicationDefinitionIdentity, __uuidof(ICMS), &TempFetched);
    if (SUCCEEDED(hr)) {
        if (ppwzApplicationFolderPath) {
            // Get the application folder path.
            LPVOID cookie = NULL;
            IfFailGo(pStore->LockApplicationPath(0, pDefinitionIdentity, &cookie, ppwzApplicationFolderPath));
            IfFailGo(pStore->ReleaseApplicationPath(cookie));
        }
    }
    IfFailGo(TempFetched->QueryInterface(__uuidof(ICMS), (void**) ppApplicationManifest));
    TempFetched.Release();

ErrExit:
    pStore.Release();
    pAppIdAuth.Release();
    pDefinitionIdentity.Release();
    pEnumDefinitionIdentity.Release();
    pDeploymentDefinitionIdentity.Release();
    pApplicationDefinitionIdentity.Release();
    pSubscriptionIdentity.Release();
    pSubscriptionAppId.Release();	

    return hr;
}

BOOL DoesMarkOfTheWebExist (LPCWSTR pwzAppFullName)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pwzAppFullName));
    } CONTRACTL_END;

    HANDLE alternateStreamHandle = INVALID_HANDLE_VALUE;

    StackSString alternateStreamPath(pwzAppFullName);
    alternateStreamPath.Append(W(":Zone.Identifier"));

    // Try to open alternate file stream
    alternateStreamHandle = WszCreateFile(
                    alternateStreamPath.GetUnicode(),
                    GENERIC_READ,
                    FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                    NULL,
                    OPEN_EXISTING,
                    0,
                    NULL);

    if (INVALID_HANDLE_VALUE != alternateStreamHandle)
    {
        CloseHandle(alternateStreamHandle);

        // We only check if MOTW (alternate stream) is present,
        // no matter what the zone is.
        return TRUE;
    }

    return FALSE;
}

HRESULT GetApplicationEntryPointInfo (LPCWSTR pwzAppFullName,
                                      DWORD dwManifestPaths,
                                      LPCWSTR *ppwzManifestPaths,
                                      __out_z __deref_out_opt LPWSTR *ppwzApplicationFolderPath,
                                      LPCWSTR *ppwzCodeBase,
                                      LPCWSTR *ppwzParameters,
                                      __out_z __deref_out_opt LPWSTR *ppwzProcessorArch,
                                      __out_z __deref_out_opt LPWSTR *ppwzAppIdKeyForm)                                      
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pwzAppFullName));
        PRECONDITION(CheckPointer(ppwzManifestPaths, NULL_OK));
        PRECONDITION(CheckPointer(ppwzCodeBase, NULL_OK));
        PRECONDITION(CheckPointer(ppwzParameters, NULL_OK));
        PRECONDITION(CheckPointer(ppwzProcessorArch, NULL_OK));
    } CONTRACTL_END;

    ReleaseHolder<ICMS> pApplicationManifest(NULL);
    ReleaseHolder<ISection> pEntrySection(NULL);
    ReleaseHolder<IEnumUnknown> pEntryEnum(NULL);
    ReleaseHolder<IEntryPointEntry> pEntry(NULL);
    ReleaseHolder<IReferenceIdentity> pReferenceId(NULL);
    ReleaseHolder<ISectionWithStringKey> pNamedRefSection(NULL);
    ReleaseHolder<ISectionWithReferenceIdentityKey> pRefSection(NULL);
    ReleaseHolder<IAssemblyReferenceEntry> pRefEntry(NULL);
    ReleaseHolder<IAssemblyReferenceDependentAssemblyEntry> pDependentAssemblyEntry(NULL);
    CoTaskMemHolder<WCHAR> pwszDependencyName = NULL;

    ReleaseHolder<IUnknown> TempFetched(NULL);
    ReleaseHolder<ISection> TempFetchedSection(NULL);
    HRESULT hr = S_OK;

    // Get the ICMS object representing the application manifest.
    IfFailGo(GetApplicationManifest(pwzAppFullName, dwManifestPaths, ppwzManifestPaths, ppwzApplicationFolderPath, ppwzAppIdKeyForm,&pApplicationManifest));

    // Get the app entry point section.
    IfFailGo(pApplicationManifest->get_EntryPointSection(&pEntrySection));
    if (pEntrySection == NULL) {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        goto ErrExit;
    }

    // Get the entry point enum.
    IfFailGo(pEntrySection->get__NewEnum(&TempFetched));
    IfFailGo(TempFetched->QueryInterface(__uuidof(IEnumUnknown), &pEntryEnum));
    TempFetched.Release();

    // Get the first entry point.
    ULONG numItems = 0;
    IfFailGo(pEntryEnum->Next(1, &TempFetched, &numItems));
    if (numItems < 1) {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        goto ErrExit;
    }
    IfFailGo(TempFetched->QueryInterface(__uuidof(IEntryPointEntry), &pEntry));
    TempFetched.Release();

    // We support both name and identity based entry points.
    IfFailGo(pEntry->get_Identity(&pReferenceId));
    if (pReferenceId == NULL) {
         hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
         goto ErrExit;
     }

    // Get the assembly reference section.
    IfFailGo(pApplicationManifest->get_AssemblyReferenceSection(&TempFetchedSection));
    IfFailGo(TempFetchedSection->QueryInterface(__uuidof(ISectionWithReferenceIdentityKey), &pRefSection));
    TempFetchedSection.Release();    

#ifdef CLICKONCE_LONGHORN_RELATED 
    //
    // If a reference assembly matching entry point does not exist, use the codebase
    // of command line file.
    // 
    if (FAILED(pRefSection->Lookup(pReferenceId, &TempFetched)))
    {
        if (ppwzCodeBase) {
            IfFailGo(pEntry->get_CommandLine_File(ppwzCodeBase));
        }
    }
    else
#endif
    {
        // Lookup the assembly reference entry.
        IfFailGo(pRefSection->Lookup(pReferenceId, &TempFetched));
        IfFailGo(TempFetched->QueryInterface(__uuidof(IAssemblyReferenceEntry), &pRefEntry));
        TempFetched.Release();

        // Get the assembly codebase. Codebase may either come from <dependentAssembly> or <installFrom>.
        // In a valid reference there should always be a <dependentAssembly> section.
        IfFailGo(pRefEntry->get_DependentAssembly(&pDependentAssemblyEntry));

        if (ppwzCodeBase) {
            IfFailGo(pDependentAssemblyEntry->get_Codebase(ppwzCodeBase));
        }
    }

    // Get the parameters
    if (ppwzParameters)
        IfFailGo(pEntry->get_CommandLine_Parameters(ppwzParameters));

    // Get the processor architecture requested in the app manifest
    if (ppwzProcessorArch)
        IfFailGo(pReferenceId->GetAttribute(NULL, W("processorArchitecture"), ppwzProcessorArch));

ErrExit:
    pApplicationManifest.Release();
    pEntrySection.Release();
    pEntryEnum.Release();
    pEntry.Release();
    pReferenceId.Release();
    pNamedRefSection.Release();
    pRefSection.Release();
    pRefEntry.Release();
    pDependentAssemblyEntry.Release();
    pwszDependencyName.Release();

    return hr;
}

//
// Export used in the ClickOnce installer for launching manifest-based applications.
//

typedef struct _tagNameMap {
    LPWSTR  pwszProcessorArch;
    DWORD   dwRuntimeInfoFlag;
} NAME_MAP;

DWORD g_DfSvcSpinLock = 0;
void EnterDfSvcSpinLock () {
    WRAPPER_NO_CONTRACT;
    while (1) {
        if (InterlockedExchange ((LPLONG)&g_DfSvcSpinLock, 1) == 1)
            ClrSleepEx (5, FALSE);
        else
            return;
    }
}

void LeaveDfSvcSpinLock () {
    InterlockedExchange ((LPLONG)&g_DfSvcSpinLock, 0);
}

//
// ThreadProc used by SHCreateProcess call - to activate ClickOnce app with ShellExecuteEx
// ShellExecuteEx can only be used from STA threads - we are creating our own STA thread
//
DWORD CorLaunchApplication_ThreadProc(void*)
{
    return 0;
}

//
// This callback is executed as the sync-callback on SHCreateThread.
// SHCreateThread does not return till this callback returns.
//
DWORD CorLaunchApplication_Callback(void* pv)
{
    SHELLEXECUTEINFO *pSei = static_cast<SHELLEXECUTEINFO *>(pv);
    IUnknown* pDummyUnknown;
    CreateStreamOnHGlobal(NULL, TRUE, (LPSTREAM*) &pDummyUnknown);
    
    if (RunningOnWin8())
    {
        // When SEE_MASK_FLAG_HINST_IS_SITE is specified SHELLEXECUTEINFO.hInstApp is used as an
        // _In_ parameter and specifies a IUnknown* to be used as a site pointer. The site pointer
        // is used to provide services to shell execute, the handler binding process and the verb handlers
        // once they are invoked.
        //
        // SEE_MASK_HINST_IS_SITE is available on Win8+
        // Defining it locally in Win8-conditioned code
        //
        const ULONG SEE_MASK_HINST_IS_SITE = 0x08000000;

        pSei->fMask = SEE_MASK_HINST_IS_SITE;
        pSei->hInstApp = reinterpret_cast<HINSTANCE>(pDummyUnknown);
    }

    WszShellExecuteEx(pSei);
    // We ignore all errors from ShellExecute.
    //
    // This may change with Win8:783168

    if (pDummyUnknown)
    {
        pDummyUnknown->Release();
    }

    return 0;
}

//-----------------------------------------------------------------------------
// WszSHCreateThread
//
// @func calls SHCreateThread with the provided parameters
//
// @rdesc Result
//-----------------------------------------------------------------------------------
HRESULT WszSHCreateThread(
    LPTHREAD_START_ROUTINE pfnThreadProc,
    void *pData,
    SHCT_FLAGS dwFlags,
    LPTHREAD_START_ROUTINE pfnCallback
)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    HMODULE _hmodShlwapi = 0;

    typedef BOOL (*PFNSHCREATETHREAD) (
              __in      LPTHREAD_START_ROUTINE pfnThreadProc,
              __in_opt  void *pData,
              __in      SHCT_FLAGS dwFlags,
              __in_opt  LPTHREAD_START_ROUTINE pfnCallback
            );

    static PFNSHCREATETHREAD pfnW = NULL;
    if (NULL == pfnW)
    {
        _hmodShlwapi = CLRLoadLibrary(W("shlwapi.dll"));
    
        if (_hmodShlwapi)
        {
            pfnW = (PFNSHCREATETHREAD)GetProcAddress(_hmodShlwapi, "SHCreateThread");
        }
    }

    if (pfnW)
    {
        BOOL bRet = pfnW(pfnThreadProc, pData, dwFlags, pfnCallback);

        if (!bRet)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }
    else
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
    }
    
    // NOTE: We leak the module handles and let the OS gather them at process shutdown.

    return hr;
}

STDAPI CorLaunchApplication (HOST_TYPE               dwClickOnceHost,
                             LPCWSTR                 pwzAppFullName,
                             DWORD                   dwManifestPaths,
                             LPCWSTR                 *ppwzManifestPaths,
                             DWORD                   dwActivationData,
                             LPCWSTR                 *ppwzActivationData,
                             LPPROCESS_INFORMATION   lpProcessInformation)
{
    // HostType is encoded in the lowest 2 bits.
    unsigned hostType = dwClickOnceHost & MASK_HOSTTYPE;
	 
    // NoPinnableBit is the highest bit.
    unsigned notPinnableBit = dwClickOnceHost & MASK_NOTPINNABLE;	

    // DontShowInstallDialog bit
    unsigned dontShowInstallDialog = dwClickOnceHost & MASK_DONT_SHOW_INSTALL_DIALOG;

    bool bUseShellExecute = false;
	 
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pwzAppFullName, NULL_OK));
        PRECONDITION(CheckPointer(ppwzManifestPaths, NULL_OK));
        PRECONDITION(CheckPointer(ppwzActivationData, NULL_OK));
        PRECONDITION(hostType == HOST_TYPE_DEFAULT || hostType == HOST_TYPE_APPLAUNCH || hostType == HOST_TYPE_CORFLAG);
        PRECONDITION(CheckPointer(lpProcessInformation));
    } CONTRACTL_END;


    if (pwzAppFullName == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    LPVOID lpEnvironment = NULL;
    EX_TRY
    {
        StackSString commandLine(StackSString::Ascii, "\""); // put quotes around path to the command line.
        StackSString appEntryPath(W("")); // the path to the entry point(the exe to run) of the application, initialized to empty string
        NewArrayHolder<WCHAR> wszDirectory(NULL);
        NewArrayHolder<WCHAR> wszVersion(NULL);
        CoTaskMemHolder<WCHAR> pwszApplicationFolderPath(NULL);
        CoTaskMemHolder<WCHAR> pwszAppIdKeyForm(NULL);			
        CoTaskMemHolder<WCHAR> pwszCodebase(NULL);
        CoTaskMemHolder<WCHAR> pwszParameters(NULL);
        CoTaskMemHolder<WCHAR> pwszProcessorArch(NULL);

        hr = GetApplicationEntryPointInfo(pwzAppFullName, dwManifestPaths, ppwzManifestPaths, (LPWSTR*) (void*) &pwszApplicationFolderPath, (LPCWSTR*) (void*) &pwszCodebase, (LPCWSTR*) (void*) &pwszParameters, (LPWSTR*) (void*) &pwszProcessorArch,(LPWSTR*) (void*) &pwszAppIdKeyForm);

        if (SUCCEEDED(hr)) {
            // construct the application Entry Path
            if (pwszApplicationFolderPath != NULL) {
                appEntryPath.Append(pwszApplicationFolderPath);
                SString::CIterator i = appEntryPath.End()-1;
                if (i[0] != '\\')
                    appEntryPath.Append(W("\\"));
            }
            appEntryPath.Append(pwszCodebase);
        
        if (hostType == HOST_TYPE_CORFLAG) {
                // construct the command line
                commandLine.Append(appEntryPath);
                commandLine.Append(W("\""));

                if (RunningOnWin8() &&
                    DoesMarkOfTheWebExist(appEntryPath.GetUnicode()))
                {
                    // We will use ShellExecute for any zone set in MOTW stream.
                    // ShellExecute would call Application Reputation API if the zone is the one
                    // that requires AppRep validation. At the moment, they would do this for Internet Zone only,
                    // but there are talks about changing the behavior to include some of the other zones.
                    // By not checking the zone here we leave to AppRep/ShellExecute to decide,
                    // which is exactly what we want.
                    bUseShellExecute = true;
                }
                else
                {
                    if (pwszParameters != NULL) {
                        commandLine.Append(W(" "));
                        commandLine.Append(pwszParameters);
                    }
                }

                // now construct the environment variables
                EnterDfSvcSpinLock();
                WszSetEnvironmentVariable(g_pwzClickOnceEnv_FullName, pwzAppFullName);

                if (dwManifestPaths > 0 && ppwzManifestPaths) {
                    for (DWORD i=0; i<dwManifestPaths; i++) {
                        StackSString manifestFile(g_pwzClickOnceEnv_Manifest);
                        StackSString buf;
                        COUNT_T size = buf.GetUnicodeAllocation();
                        _itow_s(i, buf.OpenUnicodeBuffer(size), size, 10);
                        buf.CloseBuffer();
                        manifestFile.Append(buf);
                        WszSetEnvironmentVariable(manifestFile.GetUnicode(), *ppwzManifestPaths++);
                    }
                }

                if (dwActivationData > 0 && ppwzActivationData) {
                    for (DWORD i=0; i<dwActivationData; i++) {
                        StackSString activationData(g_pwzClickOnceEnv_Parameter);
                        StackSString buf;
                        COUNT_T size = buf.GetUnicodeAllocation();
                        _itow_s(i, buf.OpenUnicodeBuffer(size), size, 10);
                        buf.CloseBuffer();
                        activationData.Append(buf);
                        WszSetEnvironmentVariable(activationData.GetUnicode(), *ppwzActivationData++);
                    }
                }

#undef GetEnvironmentStrings
#undef GetEnvironmentStringsW
                lpEnvironment = (LPVOID) GetEnvironmentStringsW();
#define GetEnvironmentStringsW() Use_WszGetEnvironmentStrings()
#define GetEnvironmentStrings() Use_WszGetEnvironmentStrings()
            } else {
                // application folder is required to determine appEntryPath for framework version selection, 
                // but should not be used as working directory for partial trust apps
                pwszApplicationFolderPath.Clear();

                // find the architecture from manifest and required version from the application itself
                static const NAME_MAP g_NameMapArray[] = {
                    {W("x86"), RUNTIME_INFO_REQUEST_X86},
                    {W("ia64"), RUNTIME_INFO_REQUEST_IA64},
                    {W("amd64"), RUNTIME_INFO_REQUEST_AMD64},
                };

                DWORD dwRuntimeInfoFlags = RUNTIME_INFO_UPGRADE_VERSION |
                                           RUNTIME_INFO_CONSIDER_POST_2_0 |
                                           RUNTIME_INFO_EMULATE_EXE_LAUNCH;

                // We want to control whether shim should show install dialog or not,
				// and not leave this decision to the Shim.
				if (dontShowInstallDialog > 0)
                {
                    dwRuntimeInfoFlags |= RUNTIME_INFO_DONT_SHOW_ERROR_DIALOG;
                }
                else
                {
                    // show even if SEM_CRITICAL is set
                    dwRuntimeInfoFlags |= METAHOST_POLICY_IGNORE_ERROR_MODE;
                }

                if (pwszProcessorArch) {
                    for (DWORD index = 0; index < sizeof(g_NameMapArray) / sizeof(NAME_MAP); index++) {
                        if (SString::_wcsicmp(g_NameMapArray[index].pwszProcessorArch, pwszProcessorArch) == 0) {
                            dwRuntimeInfoFlags |= g_NameMapArray[index].dwRuntimeInfoFlag;
                            break;
                        }
                    }
                }
                wszDirectory = new WCHAR[MAX_LONGPATH + 1];
                wszVersion = new WCHAR[MAX_PATH_FNAME + 1];
                wszVersion[0] = 0; // we don't prefer any version
                DWORD cchBuffer = MAX_LONGPATH;

                // Use GetRequestedRuntimeInfo because MetaHost APIs do not yet support architecture arguments.
                // Calls to GetRequestedRuntimeInfo() will goes to a local copy inside clr.dll, 
                // have to call mscoree::GetRequestedRuntimeInfo.
                typedef HRESULT (*PFNGetRequestedRuntimeInfo)(LPCWSTR pExe,
                                                              LPCWSTR pwszVersion,
                                                              LPCWSTR pConfigurationFile,
                                                              DWORD startupFlags,
                                                              DWORD runtimeInfoFlags,
                                                              LPWSTR pDirectory,
                                                              DWORD dwDirectory,
                                                              DWORD *dwDirectoryLength,
                                                              LPWSTR pVersion,
                                                              DWORD cchBuffer,
                                                              DWORD* dwlength);
                PFNGetRequestedRuntimeInfo pfnGetRequestedRuntimeInfo = NULL;
                HMODULE hMscoree = GetModuleHandleW( W("mscoree.dll") );  // mscoree.dll should have already been loaded
                if( hMscoree != NULL )
                    pfnGetRequestedRuntimeInfo = (PFNGetRequestedRuntimeInfo)GetProcAddress( hMscoree, "GetRequestedRuntimeInfo" );
                if( pfnGetRequestedRuntimeInfo == NULL )
                        pfnGetRequestedRuntimeInfo = GetRequestedRuntimeInfoInternal;  // in case mscoree has not been loaded, use the built in function
                hr = pfnGetRequestedRuntimeInfo(appEntryPath.GetUnicode(),    // Use the image path to guide all version binding
                                                NULL,                         // Do not prime with any preferred version
                                                NULL,                         // No explicit config file - pick up on one next to image if there.
                                                0,                            // startupFlags
                                                dwRuntimeInfoFlags,           // Will bind to post-v2 runtimes if EXE PE runtime version is post-v2
                                                                              // or EXE has config file binding to post-v2 runtime.
                                                wszDirectory, MAX_LONGPATH, NULL, // Retrieve bound directory
                                                wszVersion, MAX_PATH_FNAME, NULL);  // Retrieve bound version

                if (SUCCEEDED(hr)) {
                    commandLine.Append(wszDirectory);
                    commandLine.Append(wszVersion);
                    commandLine.Append(W("\\applaunch.exe"));
                    commandLine.Append(W("\" /activate \""));
                    commandLine.Append(pwzAppFullName);
                    commandLine.Append(W("\" "));

                    if (dwManifestPaths > 0 && ppwzManifestPaths) {
                        commandLine.Append(W("/manifests "));
                        for (DWORD i=0; i<dwManifestPaths; i++) {
                            commandLine.Append(W("\""));
                            commandLine.Append(*ppwzManifestPaths++);
                            commandLine.Append(W("\" "));
                        }
                    }

                    if (dwActivationData > 0 && ppwzActivationData) {
                        commandLine.Append(W("/parameters "));
                        for (DWORD i=0; i<dwActivationData; i++) {
                            commandLine.Append(W("\""));
                            commandLine.Append(*ppwzActivationData++);
                            commandLine.Append(W("\" "));
                        }
                    }
                }
            }
        }

        if (SUCCEEDED(hr)) {
            // CreateProcess won't let this parameter be const
            // (it writes a NULL in the middle), so we create a writable version
            LPCWSTR wszCommandLineNonWritable = commandLine.GetUnicode();
            size_t len = wcslen(wszCommandLineNonWritable);
            NewArrayHolder<WCHAR> wszCommandLine(new WCHAR[len + 1]);
            memcpy(wszCommandLine, wszCommandLineNonWritable, len * sizeof(WCHAR));
            wszCommandLine[len] = W('\0');

            STARTUPINFO sui;
            memset(&sui, 0, sizeof(STARTUPINFO));
            sui.cb = sizeof(STARTUPINFO);
	        sui.lpTitle = pwszAppIdKeyForm;	     
	        sui.dwFlags = STARTF_TITLEISAPPID;		 

	        if (notPinnableBit>0)
		        sui.dwFlags |= STARTF_PREVENTPINNING;

            // ClickOnce uses ShellExecute to utilize Win8+ Application Reputation service.
            // Application Reputation validates applications coming from the Internet.
            // ClickOnce will use ShellExecute only if there is a Mark-of-the-Web file-stream for the executable.
            // In all other cases we continue to use CreateProcess. CreateProcess does not use AppRep service.
            if (bUseShellExecute)
            {
                SHELLEXECUTEINFO sei;
                memset(&sei, 0, sizeof(SHELLEXECUTEINFO));
                sei.cbSize = sizeof(SHELLEXECUTEINFO);
                sei.hwnd = NULL;
                sei.lpVerb = NULL;
                sei.lpFile = wszCommandLine;
                sei.lpParameters = pwszParameters;
                sei.lpDirectory = pwszApplicationFolderPath;
                sei.nShow = SW_SHOWDEFAULT;
                sei.hInstApp = NULL;

                // Application Reputation is a COM Shell Extension that requires a calling thread to be an STA
                // CorLaunchApplication_Callback calls ShellExecuteEx.
                hr = WszSHCreateThread((LPTHREAD_START_ROUTINE) CorLaunchApplication_ThreadProc, &sei, CTF_COINIT_STA,
                                    (LPTHREAD_START_ROUTINE) CorLaunchApplication_Callback);
            }
            else
            {
                // Launch the child process
                BOOL result = WszCreateProcess(NULL,
                                               wszCommandLine,
                                               NULL, NULL, FALSE,
                                               (lpEnvironment) ? NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT | CREATE_DEFAULT_ERROR_MODE : NORMAL_PRIORITY_CLASS | CREATE_DEFAULT_ERROR_MODE,
                                               lpEnvironment, pwszApplicationFolderPath,
                                               &sui, lpProcessInformation);
                if (!result)
                    hr = HRESULT_FROM_GetLastError();
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    // cleanup
    if (hostType == HOST_TYPE_CORFLAG) {
        // free the environment block
#undef FreeEnvironmentStringsA
#undef FreeEnvironmentStringsW
        if (NULL != lpEnvironment) {
                FreeEnvironmentStringsW((LPWSTR) lpEnvironment);
        }
#define FreeEnvironmentStringsW(lpEnvironment) Use_WszFreeEnvironmentStrings(lpEnvironment)
#define FreeEnvironmentStringsA(lpEnvironment) Use_WszFreeEnvironmentStrings(lpEnvironment)
        // reset the environment variables
        WszSetEnvironmentVariable(g_pwzClickOnceEnv_FullName, NULL);
        EX_TRY
        {
            if (dwManifestPaths > 0 && ppwzManifestPaths) {
                for (DWORD i=0; i<dwManifestPaths; i++) {
                    StackSString manifestFile(g_pwzClickOnceEnv_Manifest);
                    StackSString buf;
                    COUNT_T size = buf.GetUnicodeAllocation();
                    _itow_s(i, buf.OpenUnicodeBuffer(size), size, 10);
                    buf.CloseBuffer();
                    manifestFile.Append(buf);
                    WszSetEnvironmentVariable(manifestFile.GetUnicode(), NULL);
                }
            }
            if (dwActivationData > 0 && ppwzActivationData) {
                for (DWORD i=0; i<dwActivationData; i++) {
                    StackSString activationData(g_pwzClickOnceEnv_Parameter);
                    StackSString buf;
                    COUNT_T size = buf.GetUnicodeAllocation();
                    _itow_s(i, buf.OpenUnicodeBuffer(size), size, 10);
                    buf.CloseBuffer();
                    activationData.Append(buf);
                    WszSetEnvironmentVariable(activationData.GetUnicode(), NULL);
                }
            }
        }
        EX_CATCH_HRESULT(hr);
        // leave the spin lock so other requests can be served.
        LeaveDfSvcSpinLock();
    }

    END_ENTRYPOINT_NOTHROW;

    return hr;
}
#endif // FEATURE_CLICKONCE
