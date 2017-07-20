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


#ifdef _DEBUG
        if (g_fEEStarted) {
            _ASSERTE (!EEAllocationDisallowed());
        }
        _ASSERTE (lpAddress || (dwSize % g_SystemInfo.dwAllocationGranularity) == 0);
#endif

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


#undef HeapAlloc
LPVOID EEHeapAlloc(HANDLE hHeap, DWORD dwFlags, SIZE_T dwBytes) 
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SO_INTOLERANT;

#ifdef FAILPOINTS_ENABLED
    if (RFS_HashStack ())
        return NULL;
#endif


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

#ifdef _DEBUG
    // Check whether (indispensable) implicit casting in ClrAllocInProcessHeapBootstrap is safe.
    static FastAllocInProcessHeapFunc pFunc = EEHeapAllocInProcessHeap;
#endif

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


    BOOL retVal = FALSE;

    {
#ifdef _DEBUG
        GlobalAllocStore::RemoveAlloc (lpMem);

        if (lpMem != NULL)
        {
            // Check the heap handle to detect heap contamination
            lpMem = (BYTE*)lpMem - OS_HEAP_ALIGN;
            HANDLE storedHeapHandle = *((HANDLE*)lpMem);
            if(storedHeapHandle != hHeap)
                _ASSERTE(!"Heap contamination detected! HeapFree was called on a heap other than the one that memory was allocated from.\n"
                         "Possible cause: you used new (executable) to allocate the memory, but didn't use DeleteExecutable() to free it.");
        }
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

#ifdef _DEBUG
    // Check whether (indispensable) implicit casting in ClrFreeInProcessHeapBootstrap is safe.
    static FastFreeInProcessHeapFunc pFunc = EEHeapFreeInProcessHeap;
#endif

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

