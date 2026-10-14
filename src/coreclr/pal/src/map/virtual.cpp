// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    virtual.cpp

Abstract:

    Implementation of virtual memory management functions.



--*/

#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(VIRTUAL); // some headers have code with asserts, so do this first

#include "pal/thread.hpp"
#include "pal/file.hpp"
#include "pal/seh.hpp"
#include "pal/virtual.h"
#include "pal/map.h"
#include "pal/init.h"
#include "pal/utils.h"
#include "common.h"
#include <clrconfignocache.h>

#include <sys/resource.h>
#include <sys/types.h>
#include <sys/mman.h>
#include <errno.h>
#include <string.h>
#include <unistd.h>
#include <limits.h>
#include <dlfcn.h>
#include <minipal/utils.h>
#include <minipal/ospagesize.h>

using namespace CorUnix;

minipal_mutex virtual_critsec;

// The first node in our list of allocated blocks.
static PCMI pVirtualMemory;

static size_t s_virtualPageSize = 0;

/* We need MAP_ANON. However on some platforms like HP-UX, it is defined as MAP_ANONYMOUS */
#if !defined(MAP_ANON) && defined(MAP_ANONYMOUS)
#define MAP_ANON MAP_ANONYMOUS
#endif

/*++
Function:
    ReserveVirtualMemory()

    Helper function used by the ExecutableMemoryAllocator to reserve
    virtual memory from the OS.

--*/
static LPVOID ReserveVirtualMemory(
                IN CPalThread *pthrCurrent, /* Currently executing thread */
                IN LPVOID lpAddress,        /* Region to reserve or commit */
                IN SIZE_T dwSize,           /* Size of Region */
                IN DWORD fAllocationType);  /* Allocation Type */


// A memory allocator that allocates memory from a pre-reserved region
// of virtual memory that is located near the CoreCLR library.
static ExecutableMemoryAllocator g_executableMemoryAllocator;

//
//
// Virtual Memory Logging
//
// We maintain a lightweight in-memory circular buffer recording virtual
// memory operations so that we can better diagnose failures and crashes
// caused by one of these operations mishandling memory in some way.
//
//
namespace VirtualMemoryLogging
{
    // Specifies the operation being logged
    enum class VirtualOperation
    {
        Allocate = 0x10,
        Reserve = 0x20,
        Commit = 0x30,
        Decommit = 0x40,
        Release = 0x50,
        ReserveFromExecutableMemoryAllocatorWithinRange = 0x70
    };

    // Indicates that the attempted operation has failed
    const DWORD FailedOperationMarker = 0x80000000;

    // An entry in the in-memory log
    struct LogRecord
    {
        ULONG RecordId;
        DWORD Operation;
        LPVOID CurrentThread;
        LPVOID RequestedAddress;
        LPVOID ReturnedAddress;
        SIZE_T Size;
        DWORD AllocationType;
        DWORD Protect;
    };

    // Maximum number of records in the in-memory log
    const ULONG MaxRecords = 128;

    // Buffer used to store the logged data
    volatile LogRecord logRecords[MaxRecords];

    // Current record number. Use (recordNumber % MaxRecords) to determine
    // the current position in the circular buffer.
    volatile ULONG recordNumber = 0;

    // Record an entry in the in-memory log
    void LogVaOperation(
        IN VirtualOperation operation,
        IN LPVOID requestedAddress,
        IN SIZE_T size,
        IN DWORD flAllocationType,
        IN DWORD flProtect,
        IN LPVOID returnedAddress,
        IN BOOL result)
    {
        ULONG i = (ULONG)InterlockedIncrement((LONG *)&recordNumber) - 1;
        LogRecord* curRec = (LogRecord*)&logRecords[i % MaxRecords];

        curRec->RecordId = i;
        curRec->CurrentThread = reinterpret_cast<LPVOID>(pthread_self());
        curRec->RequestedAddress = requestedAddress;
        curRec->ReturnedAddress = returnedAddress;
        curRec->Size = size;
        curRec->AllocationType = flAllocationType;
        curRec->Protect = flProtect;
        curRec->Operation = static_cast<DWORD>(operation) | (result ? 0 : FailedOperationMarker);
    }
}

/*++
Function:
    VIRTUALInitialize()

    Initializes this section's critical section.

Return value:
    TRUE  if initialization succeeded
    FALSE otherwise.

--*/
extern "C"
BOOL
VIRTUALInitialize(bool initializeExecutableMemoryAllocator)
{
    s_virtualPageSize = minipal_getpagesize();

    TRACE("Initializing the Virtual Critical Sections. \n");

    minipal_mutex_init(&virtual_critsec);

    pVirtualMemory = NULL;

    if (initializeExecutableMemoryAllocator)
    {
        g_executableMemoryAllocator.Initialize();
    }

    return TRUE;
}

/***
 *
 * VIRTUALCleanup()
 *      Deletes this section's critical section.
 *
 */
extern "C"
void VIRTUALCleanup()
{
    PCMI pEntry;
    PCMI pTempEntry;
    minipal_mutex_enter(&virtual_critsec);

    // Clean up the allocated memory.
    pEntry = pVirtualMemory;
    while ( pEntry )
    {
        WARN( "The memory at %d was not freed through a call to VirtualFree.\n",
              pEntry->startBoundary );
        pTempEntry = pEntry;
        pEntry = pEntry->pNext;
        free(pTempEntry );
    }
    pVirtualMemory = NULL;

    minipal_mutex_leave(&virtual_critsec);

    TRACE( "Deleting the Virtual Critical Sections. \n" );
    minipal_mutex_destroy( &virtual_critsec );
}

/***
 *
 *  VIRTUALContainsInvalidProtectionFlags()
 *          Returns TRUE if an invalid flag is specified. FALSE otherwise.
 */
static BOOL VIRTUALContainsInvalidProtectionFlags( IN DWORD flProtect )
{
    if ( ( flProtect & ~( PAGE_NOACCESS | PAGE_READONLY |
                          PAGE_READWRITE | PAGE_EXECUTE | PAGE_EXECUTE_READ |
                          PAGE_EXECUTE_READWRITE ) ) != 0 )
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}


#ifdef DEBUG
void VerifyRightEntry(PCMI pEntry)
{
    volatile PCMI pRight = pEntry->pNext;
    SIZE_T endAddress;
    if (pRight != nullptr)
    {
        endAddress = ((SIZE_T)pEntry->startBoundary) + pEntry->memSize;
        _ASSERTE(endAddress <= (SIZE_T)pRight->startBoundary);
    }
}

void VerifyLeftEntry(PCMI pEntry)
{
    volatile PCMI pLeft = pEntry->pPrevious;
    SIZE_T endAddress;
    if (pLeft != NULL)
    {
        endAddress = ((SIZE_T)pLeft->startBoundary) + pLeft->memSize;
        _ASSERTE(endAddress <= (SIZE_T)pEntry->startBoundary);
    }
}
#endif // DEBUG

/****
 *  VIRTUALStoreAllocationInfo()
 *
 *      Stores the allocation information in the linked list.
 *      NOTE: The caller must own the critical section.
 */
static BOOL VIRTUALStoreAllocationInfo(
            IN UINT_PTR startBoundary,  /* Start of the region. */
            IN SIZE_T memSize,          /* Size of the region. */
            IN DWORD flAllocationType,  /* Allocation Types. */
            IN DWORD flProtection )     /* Protections flags on the memory. */
{
    PCMI pNewEntry       = nullptr;
    PCMI pMemInfo        = nullptr;
    SIZE_T nBufferSize   = 0;

    if (!IS_ALIGNED(memSize, GetVirtualPageSize()))
    {
        ERROR("The memory size was not a multiple of the page size. \n");
        return FALSE;
    }

    if (!(pNewEntry = (PCMI)malloc(sizeof(*pNewEntry))))
    {
        ERROR( "Unable to allocate memory for the structure.\n");
        return FALSE;
    }

    pNewEntry->startBoundary    = startBoundary;
    pNewEntry->memSize          = memSize;
    pNewEntry->allocationType   = flAllocationType;
    pNewEntry->accessProtection = flProtection;

    pMemInfo = pVirtualMemory;

    if (pMemInfo && pMemInfo->startBoundary < startBoundary)
    {
        /* Look for the correct insert point */
        TRACE("Looking for the correct insert location.\n");
        while (pMemInfo->pNext && (pMemInfo->pNext->startBoundary < startBoundary))
        {
            pMemInfo = pMemInfo->pNext;
        }

        pNewEntry->pNext = pMemInfo->pNext;
        pNewEntry->pPrevious = pMemInfo;

        if (pNewEntry->pNext)
        {
            pNewEntry->pNext->pPrevious = pNewEntry;
        }

        pMemInfo->pNext = pNewEntry;
    }
    else
    {
        /* This is the first entry in the list. */
        pNewEntry->pNext = pMemInfo;
        pNewEntry->pPrevious = nullptr;

        if (pNewEntry->pNext)
        {
            pNewEntry->pNext->pPrevious = pNewEntry;
        }

        pVirtualMemory = pNewEntry ;
    }

#ifdef DEBUG
    VerifyRightEntry(pNewEntry);
    VerifyLeftEntry(pNewEntry);
#endif // DEBUG

    return TRUE;
}

/******
 *
 *  ReserveVirtualMemory() - Helper function used by the ExecutableMemoryAllocator
 *  to reserve virtual memory from the OS.
 *
 */
static LPVOID ReserveVirtualMemory(
                IN CPalThread *pthrCurrent, /* Currently executing thread */
                IN LPVOID lpAddress,        /* Region to reserve or commit */
                IN SIZE_T dwSize,           /* Size of Region */
                IN DWORD fAllocationType)   /* Allocation type */
{
    UINT_PTR StartBoundary = (UINT_PTR)lpAddress;
    SIZE_T MemSize = dwSize;

    TRACE( "Reserving the memory now.\n");

#ifdef TARGET_WASM
    // WASM cannot honor address hints - ignore lpAddress and allocate
    // at whatever address posix_memalign returns. Callers must handle
    // getting a different address than requested (same as Linux with
    // some SELinux settings).
    (void)StartBoundary;
    (void)fAllocationType; // Large pages / executable flags are N/A on WASM.

    // WASM has no virtual memory - mmap(PROT_NONE) still consumes linear memory,
    // munmap of partial ranges doesn't return memory, and MAP_FIXED is broken.
    // Use posix_memalign/free instead.

    LPVOID pRetVal = nullptr;
    if (posix_memalign(&pRetVal, GetVirtualPageSize(), MemSize) != 0 || pRetVal == nullptr)
    {
        ERROR( "Failed due to insufficient memory.\n" );
        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return nullptr;
    }

    // posix_memalign may return either freshly grown linear memory (zeroed by the
    // WASM spec) or a recycled block from the allocator's free list (which may
    // contain stale data from a previous free()). Always zero to match the
    // "reserved memory starts zeroed" contract of this function.
    memset(pRetVal, 0, MemSize);
#else // !TARGET_WASM
    // Most platforms will only commit memory if it is dirtied,
    // so this should not consume too much swap space.
    int mmapFlags = MAP_ANON | MAP_PRIVATE;

    if ((fAllocationType & MEM_LARGE_PAGES) != 0)
    {
#if HAVE_MAP_HUGETLB
        mmapFlags |= MAP_HUGETLB;
        TRACE("MAP_HUGETLB flag set\n");
#elif HAVE_VM_FLAGS_SUPERPAGE_SIZE_ANY
        mmapFlags |= VM_FLAGS_SUPERPAGE_SIZE_ANY;
        TRACE("VM_FLAGS_SUPERPAGE_SIZE_ANY flag set\n");
#else
        TRACE("Large Pages requested, but not supported in this PAL configuration\n");
#endif
    }

#if defined(HOST_OSX)
    if ((fAllocationType & MEM_RESERVE_EXECUTABLE) && IsRunningOnMojaveHardenedRuntime())
    {
        mmapFlags |= MAP_JIT;
    }
#endif

    LPVOID pRetVal = mmap((LPVOID) StartBoundary,
                          MemSize,
                          PROT_NONE,
                          mmapFlags,
                          -1 /* fd */,
                          0  /* offset */);

    if (pRetVal == MAP_FAILED)
    {
        ERROR( "Failed due to insufficient memory.\n" );

        pthrCurrent->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return nullptr;
    }

    /* Check to see if the region is what we asked for. */
    if (lpAddress != nullptr && StartBoundary != (UINT_PTR)pRetVal)
    {
        ERROR("We did not get the region we asked for from mmap!\n");
        pthrCurrent->SetLastError(ERROR_INVALID_ADDRESS);
        munmap(pRetVal, MemSize);
        return nullptr;
    }

#if MMAP_ANON_IGNORES_PROTECTION
    if (mprotect(pRetVal, MemSize, PROT_NONE) != 0)
    {
        ERROR("mprotect failed to protect the region!\n");
        pthrCurrent->SetLastError(ERROR_INVALID_ADDRESS);
        munmap(pRetVal, MemSize);
        return nullptr;
    }
#endif  // MMAP_ANON_IGNORES_PROTECTION

#if defined(MADV_DONTDUMP)
    // Do not include reserved uncommitted memory in coredump.
    if (!(fAllocationType & MEM_COMMIT))
    {
        madvise(pRetVal, MemSize, MADV_DONTDUMP);
    }
#endif
#endif // !TARGET_WASM

    return pRetVal;
}

/*++
Function:
  PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange

  This function attempts to allocate the requested amount of memory in the specified address range, from the executable memory
  allocator. If unable to do so, the function returns nullptr and does not set the last error.

  lpBeginAddress - Inclusive beginning of range
  lpEndAddress - Exclusive end of range
  dwSize - Number of bytes to allocate
  fStoreAllocationInfo - TRUE to indicate that the allocation should be registered in the PAL allocation list
--*/
LPVOID
PALAPI
PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(
    IN LPCVOID lpBeginAddress,
    IN LPCVOID lpEndAddress,
    IN SIZE_T dwSize,
    IN BOOL fStoreAllocationInfo)
{
#ifdef HOST_64BIT
    PERF_ENTRY(PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange);
    ENTRY(
        "PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(lpBeginAddress = %p, lpEndAddress = %p, dwSize = %zu, fStoreAllocationInfo = %d)\n",
        lpBeginAddress,
        lpEndAddress,
        dwSize,
        fStoreAllocationInfo);

    _ASSERTE(lpBeginAddress <= lpEndAddress);

    // Alignment to a 64 KB granularity should not be necessary (alignment to page size should be sufficient), but see
    // ExecutableMemoryAllocator::AllocateMemory() for the reason why it is done
    SIZE_T reservationSize = ALIGN_UP(dwSize, VIRTUAL_64KB);

    minipal_mutex_enter(&virtual_critsec);

    void *address = g_executableMemoryAllocator.AllocateMemoryWithinRange(lpBeginAddress, lpEndAddress, reservationSize);
    if (address != nullptr)
    {
        _ASSERTE(IS_ALIGNED(address, GetVirtualPageSize()));
        if (fStoreAllocationInfo && !VIRTUALStoreAllocationInfo((UINT_PTR)address, reservationSize, MEM_RESERVE | MEM_RESERVE_EXECUTABLE, PAGE_NOACCESS))
        {
            ASSERT("Unable to store the structure in the list.\n");
            munmap(address, reservationSize);
            address = nullptr;
        }
    }

    LogVaOperation(
        VirtualMemoryLogging::VirtualOperation::ReserveFromExecutableMemoryAllocatorWithinRange,
        nullptr,
        dwSize,
        MEM_RESERVE | MEM_RESERVE_EXECUTABLE,
        PAGE_NOACCESS,
        address,
        TRUE);

    minipal_mutex_leave(&virtual_critsec);

    LOGEXIT("PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange returning %p\n", address);
    PERF_EXIT(PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange);
    return address;
#else // !HOST_64BIT
    return nullptr;
#endif // HOST_64BIT
}

/*++
Function:
  PAL_GetExecutableMemoryAllocatorPreferredRange

  This function gets the preferred range used by the executable memory allocator.
  This is the range that the memory allocator will prefer to allocate memory in,
  including (if nearby) the libcoreclr memory range.

  lpBeginAddress - Inclusive beginning of range
  lpEndAddress - Exclusive end of range
  dwSize - Number of bytes to allocate
--*/
void
PALAPI
PAL_GetExecutableMemoryAllocatorPreferredRange(
    OUT LPVOID *start,
    OUT LPVOID *end)
{
    g_executableMemoryAllocator.GetPreferredRange(start, end);
}

#if defined(HOST_OSX) && defined(HOST_ARM64)
PALAPI VOID PAL_JitWriteProtect(bool writeEnable)
{
    thread_local int enabledCount = 0;
    if (writeEnable)
    {
        if (enabledCount++ == 0)
        {
            pthread_jit_write_protect_np(0);
        }
    }
    else
    {
        if (--enabledCount == 0)
        {
            pthread_jit_write_protect_np(1);
        }
        _ASSERTE(enabledCount >= 0);
    }
}
#endif // HOST_OSX && HOST_ARM64

size_t GetVirtualPageSize()
{
    _ASSERTE(s_virtualPageSize);
    return s_virtualPageSize;
}

/*++
Function :
    ReserveMemoryFromExecutableAllocator

    This function is used to reserve a region of virual memory (not committed)
    that is located close to the coreclr library. The memory comes from the virtual
    address range that is managed by ExecutableMemoryAllocator.
--*/
void* ReserveMemoryFromExecutableAllocator(CPalThread* pThread, SIZE_T allocationSize)
{
#ifdef HOST_64BIT
    minipal_mutex_enter(&virtual_critsec);
    void* mem = g_executableMemoryAllocator.AllocateMemory(allocationSize);
    minipal_mutex_leave(&virtual_critsec);

    return mem;
#else // !HOST_64BIT
    return nullptr;
#endif // HOST_64BIT
}

/*++
Function:
    ExecutableMemoryAllocator::Initialize()

    This function initializes the allocator. It should be called early during process startup
    (when process address space is pretty much empty) in order to have a chance to reserve
    sufficient amount of memory that is close to the coreclr library.

--*/
void ExecutableMemoryAllocator::Initialize()
{
    // Enable the executable memory allocator on 64-bit platforms only
    // because 32-bit platforms have limited amount of virtual address space.
#ifdef HOST_64BIT
    TryReserveInitialMemory();
#endif // HOST_64BIT

}

/*++
Function:
    ExecutableMemoryAllocator::TryReserveInitialMemory()

    This function is called during PAL initialization. It opportunistically tries to reserve
    a large chunk of virtual memory that can be later used to store JIT'ed code.\

--*/
void ExecutableMemoryAllocator::TryReserveInitialMemory()
{
    CPalThread* pthrCurrent = InternalGetCurrentThread();
    int32_t preferredStartAddressIncrement;
    UINT_PTR preferredStartAddress;
    UINT_PTR coreclrLoadAddress;

    int32_t sizeOfAllocation = MaxExecutableMemorySizeNearCoreClr;
    int32_t initialReserveLimit = -1;
#if defined(TARGET_WASI)
    // WASI has neither RLIMIT_AS nor RLIMIT_DATA nor RLIM_INFINITY; wasm32 has a
    // hard 4 GiB limit and no separate VM accounting. Skip the rlimit-based
    // sizing and let sizeOfAllocation stay at MaxExecutableMemorySizeNearCoreClr.
#else
#ifdef RLIMIT_AS
    int addressSpace = RLIMIT_AS;
#else
    int addressSpace = RLIMIT_DATA;
#endif
    rlimit addressSpaceLimit;
    if ((getrlimit(addressSpace, &addressSpaceLimit) == 0) && (addressSpaceLimit.rlim_cur != RLIM_INFINITY))
    {
        // By default reserve max 20% of the available virtual address space
        rlim_t initialExecMemoryPerc = 20;
        CLRConfigNoCache defInitialExecMemoryPerc = CLRConfigNoCache::Get("InitialExecMemoryPercent", /*noprefix*/ false, &getenv);
        if (defInitialExecMemoryPerc.IsSet())
        {
            DWORD perc;
            if (defInitialExecMemoryPerc.TryAsInteger(16, perc))
            {
                initialExecMemoryPerc = perc;
            }
        }

        initialReserveLimit = addressSpaceLimit.rlim_cur * initialExecMemoryPerc / 100;
        if (initialReserveLimit < sizeOfAllocation)
        {
            sizeOfAllocation = initialReserveLimit;
        }
    }
#endif // !TARGET_WASI
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // Smaller steps on ARM because we try hard finding a spare memory in a 128Mb
    // distance from coreclr so e.g. all calls from corelib to coreclr could use relocs
    const int32_t AddressProbingIncrement = 8 * 1024 * 1024;
#else
    const int32_t AddressProbingIncrement = 128 * 1024 * 1024;
#endif
    const int32_t SizeProbingDecrement = 128 * 1024 * 1024;

    // Try to find and reserve an available region of virtual memory that is located
    // within 2GB range (defined by the MaxExecutableMemorySizeNearCoreClr constant) from the
    // location of the coreclr library.
    // Potentially, as a possible future improvement, we can get precise information
    // about available memory ranges by parsing data from '/proc/self/maps'.
    // But since this code is called early during process startup, the user address space
    // is pretty much empty so the simple algorithm that is implemented below is sufficient
    // for this purpose.

    // First of all, we need to determine the current address of libcoreclr. Please note that depending on
    // the OS implementation, the library is usually loaded either at the end or at the start of the user
    // address space. If the library is loaded at low addresses then try to reserve memory above libcoreclr
    // (thus avoiding reserving memory below 4GB; besides some operating systems do not allow that).
    // If libcoreclr is loaded at high addresses then try to reserve memory below its location.
    coreclrLoadAddress = (UINT_PTR)PAL_GetSymbolModuleBase((void*)&VIRTUALInitialize);
    if ((coreclrLoadAddress < 0xFFFFFFFF) || ((coreclrLoadAddress - MaxExecutableMemorySizeNearCoreClr) < 0xFFFFFFFF))
    {
        // Try to allocate above the location of libcoreclr
        preferredStartAddress = coreclrLoadAddress + CoreClrLibrarySize;
        preferredStartAddressIncrement = AddressProbingIncrement;
    }
    else
    {
        // Try to allocate below the location of libcoreclr
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
        // For arm for the "high address" case it only makes sense to try to reserve 128Mb
        // and if it doesn't work - we'll reserve a full-sized region in a random location
        sizeOfAllocation = SizeProbingDecrement;
#endif

        preferredStartAddress = coreclrLoadAddress - sizeOfAllocation;
        preferredStartAddressIncrement = 0;
    }

    // Do actual memory reservation.
    do
    {
        m_startAddress = ReserveVirtualMemory(pthrCurrent, (void*)preferredStartAddress, sizeOfAllocation, MEM_RESERVE_EXECUTABLE);
        if (m_startAddress != nullptr)
        {
            break;
        }

        // Try to allocate a smaller region
        sizeOfAllocation -= SizeProbingDecrement;
        preferredStartAddress += preferredStartAddressIncrement;

    } while (sizeOfAllocation >= SizeProbingDecrement);

    if (m_startAddress == nullptr)
    {
        // We were not able to reserve any memory near libcoreclr. Try to reserve approximately 2 GB of address space somewhere
        // anyway:
        //   - This sets aside address space that can be used for executable code, such that jumps/calls between such code may
        //     continue to use short relative addresses instead of long absolute addresses that would currently require jump
        //     stubs.
        //   - The inability to allocate memory in a specific range for jump stubs is an unrecoverable problem. This reservation
        //     would mitigate such issues that can become prevalent depending on which security features are enabled and to what
        //     extent, such as in particular, PaX's RANDMMAP:
        //       - https://en.wikibooks.org/wiki/Grsecurity/Appendix/Grsecurity_and_PaX_Configuration_Options
        //   - Jump stubs for executable code residing in this region can request memory from this allocator
        //   - Native images can be loaded into this address space, including any jump stubs that are required for its helper
        //     table. This satisfies the vast majority of practical cases where the total amount of loaded native image memory
        //     does not exceed approximately 2 GB.
        //   - The code heap allocator for the JIT can allocate from this address space. Beyond this reservation, one can use
        //     the DOTNET_CodeHeapReserveForJumpStubs environment variable to reserve space for jump stubs.
        sizeOfAllocation = (initialReserveLimit != -1) ? initialReserveLimit : MaxExecutableMemorySize;
        m_startAddress = ReserveVirtualMemory(pthrCurrent, nullptr, sizeOfAllocation, MEM_RESERVE_EXECUTABLE);
        if (m_startAddress == nullptr)
        {
            return;
        }

        m_preferredRangeStart = m_startAddress;
        m_preferredRangeEnd = (char*)m_startAddress + sizeOfAllocation;
    }
    else
    {
        // We managed to allocate memory close to libcoreclr, so include its memory address in the preferred range to allow
        // generated code to use IP-relative addressing.
        if ((char*)m_startAddress < (char*)coreclrLoadAddress)
        {
            m_preferredRangeStart = (void*)m_startAddress;
            m_preferredRangeEnd = (char*)coreclrLoadAddress + CoreClrLibrarySize;
        }
        else
        {
            m_preferredRangeStart = (void*)coreclrLoadAddress;
            m_preferredRangeEnd = (char*)m_startAddress + sizeOfAllocation;
        }

        _ASSERTE((char*)m_preferredRangeEnd - (char*)m_preferredRangeStart <= INT_MAX);
    }

    // Memory has been successfully reserved.
    m_totalSizeOfReservedMemory = sizeOfAllocation;

    // Randomize the location at which we start allocating from the reserved memory range. Alignment to a 64 KB granularity
    // should not be necessary, but see AllocateMemory() for the reason why it is done.
    int32_t randomOffset = GenerateRandomStartOffset();
    m_nextFreeAddress = ALIGN_UP((void*)(((UINT_PTR)m_startAddress) + randomOffset), VIRTUAL_64KB);
    _ASSERTE(sizeOfAllocation >= (int32_t)((UINT_PTR)m_nextFreeAddress - (UINT_PTR)m_startAddress));
    m_remainingReservedMemory =
        ALIGN_DOWN(sizeOfAllocation - ((UINT_PTR)m_nextFreeAddress - (UINT_PTR)m_startAddress), VIRTUAL_64KB);
}

/*++
Function:
    ExecutableMemoryAllocator::AllocateMemory

    This function attempts to allocate the requested amount of memory from its reserved virtual
    address space. The function will return null if the allocation request cannot
    be satisfied by the memory that is currently available in the allocator.

    Note: This function MUST be called with the virtual_critsec lock held.

--*/
void* ExecutableMemoryAllocator::AllocateMemory(SIZE_T allocationSize)
{
#ifdef HOST_64BIT
    void* allocatedMemory = nullptr;

    // Alignment to a 64 KB granularity should not be necessary (alignment to page size should be sufficient), but
    // ReserveVirtualMemory() aligns down the specified address to a 64 KB granularity, and as long as that is necessary, the
    // reservation size here must be aligned to a 64 KB granularity to guarantee that all returned addresses are also aligned to
    // a 64 KB granularity. Otherwise, attempting to reserve memory starting from an unaligned address returned by this function
    // would fail in ReserveVirtualMemory.
    _ASSERTE(IS_ALIGNED(allocationSize, VIRTUAL_64KB));

    // The code below assumes that the caller owns the virtual_critsec lock.
    // So the calculations are not done in thread-safe manner.
    if ((allocationSize > 0) && (allocationSize <= (SIZE_T)m_remainingReservedMemory))
    {
        allocatedMemory = m_nextFreeAddress;
        m_nextFreeAddress = (void*)(((UINT_PTR)m_nextFreeAddress) + allocationSize);
        m_remainingReservedMemory -= allocationSize;
    }

    return allocatedMemory;
#else // !HOST_64BIT
    return nullptr;
#endif // HOST_64BIT
}

/*++
Function:
    AllocateMemory

    This function attempts to allocate the requested amount of memory from its reserved virtual
    address space, if memory is available within the specified range. The function will return
    null if the allocation request cannot satisfied by the memory that is currently available in
    the allocator.

    Note: This function MUST be called with the virtual_critsec lock held.
--*/
void *ExecutableMemoryAllocator::AllocateMemoryWithinRange(const void *beginAddress, const void *endAddress, SIZE_T allocationSize)
{
#ifdef HOST_64BIT
    _ASSERTE(beginAddress <= endAddress);

    // Alignment to a 64 KB granularity should not be necessary (alignment to page size should be sufficient), but see
    // AllocateMemory() for the reason why it is necessary
    _ASSERTE(IS_ALIGNED(allocationSize, VIRTUAL_64KB));

    // The code below assumes that the caller owns the virtual_critsec lock.
    // So the calculations are not done in thread-safe manner.

    if (allocationSize == 0 || allocationSize > (SIZE_T)m_remainingReservedMemory)
    {
        return nullptr;
    }

    void *address = m_nextFreeAddress;
    if (address < beginAddress)
    {
        return nullptr;
    }

    void *nextFreeAddress = (void *)((UINT_PTR)address + allocationSize);
    if (nextFreeAddress > endAddress)
    {
        return nullptr;
    }

    m_nextFreeAddress = nextFreeAddress;
    m_remainingReservedMemory -= allocationSize;
    return address;
#else // !HOST_64BIT
    return nullptr;
#endif // HOST_64BIT
}

/*++
Function:
    ExecutableMemoryAllocator::GenerateRandomStartOffset()

    This function returns a random offset (in multiples of the virtual page size)
    at which the allocator should start allocating memory from its reserved memory range.

--*/
#ifdef __sun
// The upper limit of the random() function on SunOS derived operating systems is not RAND_MAX, but 2^31-1.
#define OFFSET_RAND_MAX 0x7FFFFFFF
#else
#define OFFSET_RAND_MAX RAND_MAX
#endif
int32_t ExecutableMemoryAllocator::GenerateRandomStartOffset()
{
    int32_t pageCount;
    const int32_t MaxStartPageOffset = 64;

    // This code is similar to what coreclr runtime does on Windows.
    // It generates a random number of pages to skip between 0...MaxStartPageOffset.
    srandom(time(NULL));
    pageCount = (int32_t)(MaxStartPageOffset * (int64_t)random() / OFFSET_RAND_MAX);

    return pageCount * GetVirtualPageSize();
}
