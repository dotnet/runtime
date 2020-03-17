// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


#include "common.h"

#include "mscoree.h"
#include "corhost.h"
#include "threads.h"


#define countof(x) (sizeof(x) / sizeof(x[0]))


HANDLE g_ExecutableHeapHandle = NULL;

#undef VirtualAlloc
LPVOID ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FAILPOINTS_ENABLED
        if (RFS_HashStack ())
            return NULL;
#endif


#ifdef _DEBUG
        if (g_fEEStarted) {
            // On Debug build we make sure that a thread is not going to do memory allocation
            // after it suspends another thread, since the another thread may be suspended while
            // having OS Heap lock.
            _ASSERTE (Thread::Debug_AllowCallout());
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
#ifdef HOST_64BIT
            // Try to allocate memory all over the place when we are stressing relocations on HOST_64BIT.
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
#endif // HOST_64BIT
        }
        }
#endif // _DEBUG

        // Fall back to the default method if the forced relocation failed
        if (p == NULL)
        {
            p = ::VirtualAlloc (lpAddress, dwSize, flAllocationType, flProtect);
        }

        if(p == NULL){
             STRESS_LOG_OOM_STACK(dwSize);
        }

        return p;
    }

}
#define VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect) Dont_Use_VirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect)

#undef VirtualFree
BOOL ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return (BOOL)(BYTE)::VirtualFree (lpAddress, dwSize, dwFreeType);
}
#define VirtualFree(lpAddress, dwSize, dwFreeType) Dont_Use_VirtualFree(lpAddress, dwSize, dwFreeType)

#undef VirtualQuery
SIZE_T ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    {
        return ::VirtualQuery(lpAddress, lpBuffer, dwLength);
    }
}
#define VirtualQuery(lpAddress, lpBuffer, dwLength) Dont_Use_VirtualQuery(lpAddress, lpBuffer, dwLength)

#if defined(_DEBUG) && !defined(TARGET_UNIX)
static VolatilePtr<BYTE> s_pStartOfUEFSection = NULL;
static VolatilePtr<BYTE> s_pEndOfUEFSectionBoundary = NULL;
static Volatile<DWORD> s_dwProtection = 0;
#endif // _DEBUG && !TARGET_UNIX

#undef VirtualProtect
BOOL ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Get the UEF installation details - we will use these to validate
    // that the calls to ClrVirtualProtect are not going to affect the UEF.
    //
    // The OS UEF invocation mechanism was updated. When a UEF is setup,the OS captures
    // the following details about it:
    //  1) Protection of the pages in which the UEF lives
    //  2) The size of the region in which the UEF lives
    //  3) The region's Allocation Base
    //
    //  The OS verifies details surrounding the UEF before invocation.  For security reasons
    //  the page protection cannot change between SetUnhandledExceptionFilter and invocation.
    //
    // Prior to this change, the UEF lived in a common section of code_Seg, along with
    // JIT_PatchedCode. Thus, their pages have the same protection, they live
    //  in the same region (and thus, its size is the same).
    //
    // In EEStartupHelper, when we setup the UEF and then invoke InitJitHelpers1 and InitJitHelpers2,
    // they perform some optimizations that result in the memory page protection being changed. When
    // the UEF is to be invoked, the OS does the check on the UEF's cached details against the current
    // memory pages. This check used to fail when on 64bit retail builds when JIT_PatchedCode was
    // aligned after the UEF with a different memory page protection (post the optimizations by InitJitHelpers).
    // Thus, the UEF was never invoked.
    //
    // To circumvent this, we put the UEF in its own section in the code segment so that any modifications
    // to memory pages will not affect the UEF details that the OS cached. This is done in Excep.cpp
    // using the "#pragma code_seg" directives.
    //
    // Below, we double check that:
    //
    // 1) the address being protected does not lie in the region of of the UEF.
    // 2) the section after UEF is not having the same memory protection as UEF section.
    //
    // We assert if either of the two conditions above are true.

#if defined(_DEBUG) && !defined(TARGET_UNIX)
   // We do this check in debug/checked builds only

    // Do we have the UEF details?
    if (s_pEndOfUEFSectionBoundary.Load() == NULL)
    {
        CONTRACT_VIOLATION(ThrowsViolation);

        // Get reference to MSCORWKS image in memory...
        PEDecoder pe(g_hThisInst);

        // Find the UEF section from the image
        IMAGE_SECTION_HEADER* pUEFSection = pe.FindSection(CLR_UEF_SECTION_NAME);
        _ASSERTE(pUEFSection != NULL);
        if (pUEFSection)
        {
            // We got our section - get the start of the section
            BYTE* pStartOfUEFSection = static_cast<BYTE*>(pe.GetBase()) + pUEFSection->VirtualAddress;
            s_pStartOfUEFSection = pStartOfUEFSection;

            // Now we need the protection attributes for the memory region in which the
            // UEF section is...
            MEMORY_BASIC_INFORMATION uefInfo;
            if (ClrVirtualQuery(pStartOfUEFSection, &uefInfo, sizeof(uefInfo)) != 0)
            {
                // Calculate how many pages does the UEF section take to get to the start of the
                // next section. We dont calculate this as
                //
                // pStartOfUEFSection + uefInfo.RegionSize
                //
                // because the section following UEF will also be included in the region size
                // if it has the same protection as the UEF section.
                DWORD dwUEFSectionPageCount = ((pUEFSection->Misc.VirtualSize + GetOsPageSize() - 1) / GetOsPageSize());

                BYTE* pAddressOfFollowingSection = pStartOfUEFSection + (GetOsPageSize() * dwUEFSectionPageCount);

                // Ensure that the section following us is having different memory protection
                MEMORY_BASIC_INFORMATION nextSectionInfo;
                _ASSERTE(ClrVirtualQuery(pAddressOfFollowingSection, &nextSectionInfo, sizeof(nextSectionInfo)) != 0);
                _ASSERTE(nextSectionInfo.Protect != uefInfo.Protect);

                // save the memory protection details
                s_dwProtection = uefInfo.Protect;

                // Get the end of the UEF section
                BYTE* pEndOfUEFSectionBoundary = pAddressOfFollowingSection - 1;

                // Set the end of UEF section boundary
                FastInterlockExchangePointer(s_pEndOfUEFSectionBoundary.GetPointer(), pEndOfUEFSectionBoundary);
            }
            else
            {
                _ASSERTE(!"Unable to get UEF Details!");
            }
        }
    }

    if (s_pEndOfUEFSectionBoundary.Load() != NULL)
    {
        // Is the protection being changed?
        if (flNewProtect != s_dwProtection)
        {
            // Is the target address NOT affecting the UEF ? Possible cases:
            // 1) Starts and ends before the UEF start
            // 2) Starts after the UEF start

            void* pEndOfRangeAddr = static_cast<BYTE*>(lpAddress) + dwSize - 1;

            _ASSERTE_MSG(((pEndOfRangeAddr < s_pStartOfUEFSection.Load()) || (lpAddress > s_pEndOfUEFSectionBoundary.Load())),
                "Do not virtual protect the section in which UEF lives!");
        }
    }
#endif // _DEBUG && !TARGET_UNIX

    return ::VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
}
#define VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) Dont_Use_VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)

#undef GetProcessHeap
HANDLE ClrGetProcessHeap()
{
    // Note: this can be called a little early for real contracts, so we use static contracts instead.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    return GetProcessHeap();
}
#define GetProcessHeap() Dont_Use_GetProcessHeap()

#undef HeapCreate
HANDLE ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX

    {
        return ::HeapCreate(flOptions, dwInitialSize, dwMaximumSize);
    }
#else // !TARGET_UNIX
    return NULL;
#endif // !TARGET_UNIX
}
#define HeapCreate(flOptions, dwInitialSize, dwMaximumSize) Dont_Use_HeapCreate(flOptions, dwInitialSize, dwMaximumSize)

#undef HeapDestroy
BOOL ClrHeapDestroy(HANDLE hHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX

    {
        return ::HeapDestroy(hHeap);
    }
#else // !TARGET_UNIX
    UNREACHABLE();
#endif // !TARGET_UNIX
}
#define HeapDestroy(hHeap) Dont_Use_HeapDestroy(hHeap)

#ifdef _DEBUG
#ifdef TARGET_X86
#define OS_HEAP_ALIGN 8
#else
#define OS_HEAP_ALIGN 16
#endif
#endif


#undef HeapAlloc
LPVOID ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, S_SIZE_T dwBytes)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef FAILPOINTS_ENABLED
    if (RFS_HashStack ())
        return NULL;
#endif

    if (dwBytes.IsOverflow()) return NULL;


    {

        LPVOID p = NULL;
#ifdef _DEBUG
        // Store the heap handle to detect heap contamination
        p = ::HeapAlloc (hHeap, dwFlags, dwBytes.Value() + OS_HEAP_ALIGN);
        if(p)
        {
            *((HANDLE*)p) = hHeap;
            p = (BYTE*)p + OS_HEAP_ALIGN;
        }
#else
        p = ::HeapAlloc (hHeap, dwFlags, dwBytes.Value());
#endif

        if(p == NULL
           // If we have not created StressLog ring buffer, we should not try to use it.
           // StressLog is going to do a memory allocation.  We may enter an endless loop.
           && StressLog::t_pCurrentThreadLog != NULL )
        {
            STRESS_LOG_OOM_STACK(dwBytes.Value());
        }

        return p;
    }
}
#define HeapAlloc(hHeap, dwFlags, dwBytes) Dont_Use_HeapAlloc(hHeap, dwFlags, dwBytes)

LPVOID ClrHeapAllocInProcessHeap(DWORD dwFlags, SIZE_T dwBytes)
{
    WRAPPER_NO_CONTRACT;

    static HANDLE ProcessHeap = NULL;

    if (ProcessHeap == NULL)
        ProcessHeap = ClrGetProcessHeap();

    return ClrHeapAlloc(ProcessHeap,dwFlags,S_SIZE_T(dwBytes));
}

#undef HeapFree
BOOL ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    BOOL retVal = FALSE;

    {
#ifdef _DEBUG
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

BOOL ClrHeapFreeInProcessHeap(DWORD dwFlags, LPVOID lpMem)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    static HANDLE ProcessHeap = NULL;

    if (ProcessHeap == NULL)
        ProcessHeap = ClrGetProcessHeap();

    return ClrHeapFree(ProcessHeap,dwFlags,lpMem);
}

HANDLE ClrGetProcessExecutableHeap() {
    // Note: this can be called a little early for real contracts, so we use static contracts instead.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;


#ifndef TARGET_UNIX

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

#else // !TARGET_UNIX
    UNREACHABLE();
#endif // !TARGET_UNIX


    // TODO: implement hosted executable heap
    return g_ExecutableHeapHandle;
}


#undef SleepEx
#undef Sleep
DWORD ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
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
#ifdef TARGET_ARM
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

CRITSEC_COOKIE ClrCreateCriticalSection(CrstType crstType, CrstFlags flags) {
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

void ClrDeleteCriticalSection(CRITSEC_COOKIE cookie)
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_NOTRIGGER);
    }
    CONTRACTL_END;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    delete pCrst;
}

DEBUG_NOINLINE void ClrEnterCriticalSection(CRITSEC_COOKIE cookie) {

    // Entering a critical section has many different contracts
    // depending on the flags used to initialize the critical section.
    // See CrstBase::Enter() for the actual contract. It's much too
    // complex to repeat here.

    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    pCrst->Enter();
}

DEBUG_NOINLINE void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    pCrst->Leave();
}
