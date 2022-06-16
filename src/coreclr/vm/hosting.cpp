// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "common.h"

#include "mscoree.h"
#include "corhost.h"
#include "threads.h"

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
        PEDecoder pe(GetClrModuleBase());

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
                InterlockedExchangeT(s_pEndOfUEFSectionBoundary.GetPointer(), pEndOfUEFSectionBoundary);
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

#undef SleepEx
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

// non-zero return value if this function causes the OS to switch to another thread
// See file:spinlock.h#SwitchToThreadSpinning for an explanation of dwSwitchCount
BOOL __SwitchToThread (DWORD dwSleepMSec, DWORD dwSwitchCount)
{
    // If you sleep for a long time, the thread should be in Preemptive GC mode.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(dwSleepMSec < 10000 || GetThreadNULLOk() == NULL || !GetThread()->PreemptiveGCDisabled());
    }
    CONTRACTL_END;

    if (dwSleepMSec > 0)
    {
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
        ClrSleepEx(1, FALSE);
    }

    return SwitchToThread();
}

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
