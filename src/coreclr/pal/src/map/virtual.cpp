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
#include "pal/cs.hpp"
#include "pal/malloc.hpp"
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

#if HAVE_VM_ALLOCATE
#include <mach/vm_map.h>
#include <mach/mach_init.h>
#endif // HAVE_VM_ALLOCATE

using namespace CorUnix;

CRITICAL_SECTION virtual_critsec;

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

    Helper function that is used by Virtual* APIs and ExecutableMemoryAllocator
    to reserve virtual memory from the OS.

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
    s_virtualPageSize = getpagesize();

    TRACE("Initializing the Virtual Critical Sections. \n");

    InternalInitializeCriticalSection(&virtual_critsec);

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
    CPalThread * pthrCurrent = InternalGetCurrentThread();

    InternalEnterCriticalSection(pthrCurrent, &virtual_critsec);

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

    InternalLeaveCriticalSection(pthrCurrent, &virtual_critsec);

    TRACE( "Deleting the Virtual Critical Sections. \n" );
    DeleteCriticalSection( &virtual_critsec );
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


/****
 *
 * VIRTUALFindRegionInformation( )
 *
 *          IN UINT_PTR address - The address to look for.
 *
 *          Returns the PCMI if found, NULL otherwise.
 */
static PCMI VIRTUALFindRegionInformation( IN UINT_PTR address )
{
    PCMI pEntry = NULL;

    TRACE( "VIRTUALFindRegionInformation( %#x )\n", address );

    pEntry = pVirtualMemory;

    while( pEntry )
    {
        if ( pEntry->startBoundary > address )
        {
            /* Gone past the possible location in the list. */
            pEntry = NULL;
            break;
        }
        if ( pEntry->startBoundary + pEntry->memSize > address )
        {
            break;
        }

        pEntry = pEntry->pNext;
    }
    return pEntry;
}

/*++
Function :

    VIRTUALReleaseMemory

    Removes a PCMI entry from the list.

    Returns true on success. FALSE otherwise.
--*/
static BOOL VIRTUALReleaseMemory( PCMI pMemoryToBeReleased )
{
    BOOL bRetVal = TRUE;

    if ( !pMemoryToBeReleased )
    {
        ASSERT( "Invalid pointer.\n" );
        return FALSE;
    }

    if ( pMemoryToBeReleased == pVirtualMemory )
    {
        /* This is either the first entry, or the only entry. */
        pVirtualMemory = pMemoryToBeReleased->pNext;
        if ( pMemoryToBeReleased->pNext )
        {
            pMemoryToBeReleased->pNext->pPrevious = NULL;
        }
    }
    else /* Could be anywhere in the list. */
    {
        /* Delete the entry from the linked list. */
        if ( pMemoryToBeReleased->pPrevious )
        {
            pMemoryToBeReleased->pPrevious->pNext = pMemoryToBeReleased->pNext;
        }

        if ( pMemoryToBeReleased->pNext )
        {
            pMemoryToBeReleased->pNext->pPrevious = pMemoryToBeReleased->pPrevious;
        }
    }

    free( pMemoryToBeReleased );
    pMemoryToBeReleased = NULL;

    return bRetVal;
}

/***
 *  Displays the linked list.
 *
 */
#if defined _DEBUG
static void VIRTUALDisplayList( void  )
{
    if (!DBG_ENABLED(DLI_TRACE, defdbgchan))
        return;

    PCMI p;
    SIZE_T count;
    SIZE_T index;
    CPalThread * pthrCurrent = InternalGetCurrentThread();

    InternalEnterCriticalSection(pthrCurrent, &virtual_critsec);

    p = pVirtualMemory;
    count = 0;
    while ( p ) {

        DBGOUT( "Entry %d : \n", count );
        DBGOUT( "\t startBoundary %#x \n", p->startBoundary );
        DBGOUT( "\t memSize %d \n", p->memSize );

        DBGOUT( "\t accessProtection %d \n", p->accessProtection );
        DBGOUT( "\t allocationType %d \n", p->allocationType );
        DBGOUT( "\t pNext %p \n", p->pNext );
        DBGOUT( "\t pLast %p \n", p->pPrevious );

        count++;
        p = p->pNext;
    }

    InternalLeaveCriticalSection(pthrCurrent, &virtual_critsec);
}
#endif

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

    if (!(pNewEntry = (PCMI)InternalMalloc(sizeof(*pNewEntry))))
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
 *  VIRTUALReserveMemory() - Helper function that actually reserves the memory.
 *
 *      NOTE: I call SetLastError in here, because many different error states
 *              exists, and that would be very complicated to work around.
 *
 */
static LPVOID VIRTUALReserveMemory(
                IN CPalThread *pthrCurrent, /* Currently executing thread */
                IN LPVOID lpAddress,        /* Region to reserve or commit */
                IN SIZE_T dwSize,           /* Size of Region */
                IN DWORD flAllocationType,  /* Type of allocation */
                IN DWORD flProtect,         /* Type of access protection */
                OUT BOOL *newMemory = NULL) /* Set if new virtual memory is allocated */
{
    LPVOID pRetVal      = NULL;
    UINT_PTR StartBoundary;
    SIZE_T MemSize;

    TRACE( "Reserving the memory now..\n");

    if (newMemory != NULL)
    {
        *newMemory = false;
    }

    // First, figure out where we're trying to reserve the memory and
    // how much we need. On most systems, requests to mmap must be
    // page-aligned and at multiples of the page size. Unlike on Windows, on
    // Unix, the allocation granularity is the page size, so the memory size to
    // reserve is not aligned to 64 KB. Nor should the start boundary need to
    // to be aligned down to 64 KB, but it is expected that there are other
    // components that rely on this alignment when providing a specific address
    // (note that mmap itself does not make any such guarantees).
    StartBoundary = (UINT_PTR)ALIGN_DOWN(lpAddress, VIRTUAL_64KB);
    MemSize = ALIGN_UP((UINT_PTR)lpAddress + dwSize, GetVirtualPageSize()) - StartBoundary;

    // If this is a request for special executable (JIT'ed) memory then, first of all,
    // try to get memory from the executable memory allocator to satisfy the request.
    if (((flAllocationType & MEM_RESERVE_EXECUTABLE) != 0) && (lpAddress == NULL))
    {
        // Alignment to a 64 KB granularity should not be necessary (alignment to page size should be sufficient), but see
        // ExecutableMemoryAllocator::AllocateMemory() for the reason why it is done
        SIZE_T reservationSize = ALIGN_UP(MemSize, VIRTUAL_64KB);
        pRetVal = g_executableMemoryAllocator.AllocateMemory(reservationSize);
        if (pRetVal != nullptr)
        {
            MemSize = reservationSize;
        }
    }

    if (pRetVal == NULL)
    {
        // Try to reserve memory from the OS
        if ((flProtect & 0xff) == PAGE_EXECUTE_READWRITE)
        {
             flAllocationType |= MEM_RESERVE_EXECUTABLE;
        }
        pRetVal = ReserveVirtualMemory(pthrCurrent, (LPVOID)StartBoundary, MemSize, flAllocationType);

        if (newMemory != NULL && pRetVal != NULL)
        {
            *newMemory = true;
        }
    }

    if (pRetVal != NULL)
    {
        if ( !lpAddress )
        {
            /* Compute the real values instead of the null values. */
            StartBoundary = (UINT_PTR) ALIGN_DOWN(pRetVal, GetVirtualPageSize());
            MemSize = ALIGN_UP((UINT_PTR)pRetVal + dwSize, GetVirtualPageSize()) - StartBoundary;
        }

        if ( !VIRTUALStoreAllocationInfo( StartBoundary, MemSize,
                                   flAllocationType, flProtect ) )
        {
            ASSERT( "Unable to store the structure in the list.\n");
            pthrCurrent->SetLastError( ERROR_INTERNAL_ERROR );
            munmap( pRetVal, MemSize );
            pRetVal = NULL;
        }
    }

    LogVaOperation(
        VirtualMemoryLogging::VirtualOperation::Reserve,
        lpAddress,
        dwSize,
        flAllocationType,
        flProtect,
        pRetVal,
        pRetVal != NULL);

    return pRetVal;
}

/******
 *
 *  ReserveVirtualMemory() - Helper function that is used by Virtual* APIs
 *  and ExecutableMemoryAllocator to reserve virtual memory from the OS.
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

#ifdef __APPLE__
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

#ifdef MADV_DONTDUMP
    // Do not include reserved uncommitted memory in coredump.
    if (!(fAllocationType & MEM_COMMIT))
    {
        madvise(pRetVal, MemSize, MADV_DONTDUMP);
    }
#endif

    return pRetVal;
}

/******
 *
 *  VIRTUALCommitMemory() - Helper function that actually commits the memory.
 *
 *      NOTE: I call SetLastError in here, because many different error states
 *              exists, and that would be very complicated to work around.
 *
 */
static LPVOID
VIRTUALCommitMemory(
                IN CPalThread *pthrCurrent, /* Currently executing thread */
                IN LPVOID lpAddress,        /* Region to reserve or commit */
                IN SIZE_T dwSize,           /* Size of Region */
                IN DWORD flAllocationType,  /* Type of allocation */
                IN DWORD flProtect)         /* Type of access protection */
{
    UINT_PTR StartBoundary      = 0;
    SIZE_T MemSize              = 0;
    PCMI pInformation           = 0;
    LPVOID pRetVal              = NULL;
    BOOL IsLocallyReserved      = FALSE;
    BOOL IsNewMemory            = FALSE;
    INT nProtect;

    if ( lpAddress )
    {
        StartBoundary = (UINT_PTR) ALIGN_DOWN(lpAddress, GetVirtualPageSize());
        MemSize = ALIGN_UP((UINT_PTR)lpAddress + dwSize, GetVirtualPageSize()) - StartBoundary;
    }
    else
    {
        MemSize = ALIGN_UP(dwSize, GetVirtualPageSize());
    }

    /* See if we have already reserved this memory. */
    pInformation = VIRTUALFindRegionInformation( StartBoundary );

    if ( !pInformation )
    {
        /* According to the new MSDN docs, if MEM_COMMIT is specified,
        and the memory is not reserved, you reserve and then commit.
        */
        LPVOID pReservedMemory =
                VIRTUALReserveMemory( pthrCurrent, lpAddress, dwSize,
                                      flAllocationType, flProtect, &IsNewMemory );

        TRACE( "Reserve and commit the memory!\n " );

        if ( pReservedMemory )
        {
            /* Re-align the addresses and try again to find the memory. */
            StartBoundary = (UINT_PTR) ALIGN_DOWN(pReservedMemory, GetVirtualPageSize());
            MemSize = ALIGN_UP((UINT_PTR)pReservedMemory + dwSize, GetVirtualPageSize()) - StartBoundary;

            pInformation = VIRTUALFindRegionInformation( StartBoundary );

            if ( !pInformation )
            {
                ASSERT( "Unable to locate the region information.\n" );
                pthrCurrent->SetLastError( ERROR_INTERNAL_ERROR );
                pRetVal = NULL;
                goto done;
            }
            IsLocallyReserved = TRUE;
        }
        else
        {
            ERROR( "Unable to reserve the memory.\n" );
            /* Don't set last error here, it will already be set. */
            pRetVal = NULL;
            goto done;
        }
    }

    TRACE( "Committing the memory now..\n");

    nProtect = W32toUnixAccessControl(flProtect);

    // Commit the pages
    if (mprotect((void *) StartBoundary, MemSize, nProtect) != 0)
    {
        ERROR("mprotect() failed! Error(%d)=%s\n", errno, strerror(errno));
        goto error;
    }

#ifdef MADV_DODUMP
    // Include committed memory in coredump. Any newly allocated memory included by default.
    if (!IsNewMemory)
    {
        madvise((void *) StartBoundary, MemSize, MADV_DODUMP);
    }
#endif

    pRetVal = (void *) StartBoundary;
    goto done;

error:
    if ( flAllocationType & MEM_RESERVE || IsLocallyReserved )
    {
        munmap( pRetVal, MemSize );
        if ( VIRTUALReleaseMemory( pInformation ) == FALSE )
        {
            ASSERT( "Unable to remove the PCMI entry from the list.\n" );
            pthrCurrent->SetLastError( ERROR_INTERNAL_ERROR );
            pRetVal = NULL;
            goto done;
        }
    }

    pInformation = NULL;
    pRetVal = NULL;
done:

    LogVaOperation(
        VirtualMemoryLogging::VirtualOperation::Commit,
        lpAddress,
        dwSize,
        flAllocationType,
        flProtect,
        pRetVal,
        pRetVal != NULL);

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

    CPalThread *currentThread = InternalGetCurrentThread();
    InternalEnterCriticalSection(currentThread, &virtual_critsec);

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

    InternalLeaveCriticalSection(currentThread, &virtual_critsec);

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

/*++
Function:
  VirtualAlloc

Note:
  MEM_TOP_DOWN, MEM_PHYSICAL, MEM_WRITE_WATCH are not supported.
  Unsupported flags are ignored.

  Page size on i386 is set to 4k.

See MSDN doc.
--*/
LPVOID
PALAPI
VirtualAlloc(
         IN LPVOID lpAddress,       /* Region to reserve or commit */
         IN SIZE_T dwSize,          /* Size of Region */
         IN DWORD flAllocationType, /* Type of allocation */
         IN DWORD flProtect)        /* Type of access protection */
{
    LPVOID  pRetVal       = NULL;
    CPalThread *pthrCurrent;

    PERF_ENTRY(VirtualAlloc);
    ENTRY("VirtualAlloc(lpAddress=%p, dwSize=%u, flAllocationType=%#x, \
          flProtect=%#x)\n", lpAddress, dwSize, flAllocationType, flProtect);

    pthrCurrent = InternalGetCurrentThread();

    if ( ( flAllocationType & MEM_WRITE_WATCH )  != 0 )
    {
        pthrCurrent->SetLastError( ERROR_INVALID_PARAMETER );
        goto done;
    }

    /* Test for un-supported flags. */
    if ( ( flAllocationType & ~( MEM_COMMIT | MEM_RESERVE | MEM_TOP_DOWN | MEM_RESERVE_EXECUTABLE | MEM_LARGE_PAGES ) ) != 0 )
    {
        ASSERT( "flAllocationType can be one, or any combination of MEM_COMMIT, \
               MEM_RESERVE, MEM_TOP_DOWN, MEM_RESERVE_EXECUTABLE, or MEM_LARGE_PAGES.\n" );
        pthrCurrent->SetLastError( ERROR_INVALID_PARAMETER );
        goto done;
    }
    if ( VIRTUALContainsInvalidProtectionFlags( flProtect ) )
    {
        ASSERT( "flProtect can be one of PAGE_READONLY, PAGE_READWRITE, or \
               PAGE_EXECUTE_READWRITE || PAGE_NOACCESS. \n" );

        pthrCurrent->SetLastError( ERROR_INVALID_PARAMETER );
        goto done;
    }
    if ( flAllocationType & MEM_TOP_DOWN )
    {
        WARN( "Ignoring the allocation flag MEM_TOP_DOWN.\n" );
    }

    LogVaOperation(
        VirtualMemoryLogging::VirtualOperation::Allocate,
        lpAddress,
        dwSize,
        flAllocationType,
        flProtect,
        NULL,
        TRUE);

    if ( flAllocationType & MEM_RESERVE )
    {
        InternalEnterCriticalSection(pthrCurrent, &virtual_critsec);
        pRetVal = VIRTUALReserveMemory( pthrCurrent, lpAddress, dwSize, flAllocationType, flProtect );
        InternalLeaveCriticalSection(pthrCurrent, &virtual_critsec);

        if ( !pRetVal )
        {
            /* Error messages are already displayed, just leave. */
            goto done;
        }
    }

    if ( flAllocationType & MEM_COMMIT )
    {
        InternalEnterCriticalSection(pthrCurrent, &virtual_critsec);
        if ( pRetVal != NULL )
        {
            /* We are reserving and committing. */
            pRetVal = VIRTUALCommitMemory( pthrCurrent, pRetVal, dwSize,
                                    flAllocationType, flProtect );
        }
        else
        {
            /* Just a commit. */
            pRetVal = VIRTUALCommitMemory( pthrCurrent, lpAddress, dwSize,
                                    flAllocationType, flProtect );
        }
        InternalLeaveCriticalSection(pthrCurrent, &virtual_critsec);
    }

done:
#if defined _DEBUG
    VIRTUALDisplayList();
#endif
    LOGEXIT("VirtualAlloc returning %p\n ", pRetVal  );
    PERF_EXIT(VirtualAlloc);
    return pRetVal;
}

/*++
Function:
  VirtualFree

See MSDN doc.
--*/
BOOL
PALAPI
VirtualFree(
        IN LPVOID lpAddress,    /* Address of region. */
        IN SIZE_T dwSize,       /* Size of region. */
        IN DWORD dwFreeType )   /* Operation type. */
{
    BOOL bRetVal = TRUE;
    CPalThread *pthrCurrent;

    PERF_ENTRY(VirtualFree);
    ENTRY("VirtualFree(lpAddress=%p, dwSize=%u, dwFreeType=%#x)\n",
          lpAddress, dwSize, dwFreeType);

    pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &virtual_critsec);

    /* Sanity Checks. */
    if ( !lpAddress )
    {
        ERROR( "lpAddress cannot be NULL. You must specify the base address of\
               regions to be de-committed. \n" );
        pthrCurrent->SetLastError( ERROR_INVALID_ADDRESS );
        bRetVal = FALSE;
        goto VirtualFreeExit;
    }

    if ( !( dwFreeType & MEM_RELEASE ) && !(dwFreeType & MEM_DECOMMIT ) )
    {
        ERROR( "dwFreeType must contain one of the following: \
               MEM_RELEASE or MEM_DECOMMIT\n" );
        pthrCurrent->SetLastError( ERROR_INVALID_PARAMETER );
        bRetVal = FALSE;
        goto VirtualFreeExit;
    }
    /* You cannot release and decommit in one call.*/
    if ( dwFreeType & MEM_RELEASE && dwFreeType & MEM_DECOMMIT )
    {
        ERROR( "MEM_RELEASE cannot be combined with MEM_DECOMMIT.\n" );
        bRetVal = FALSE;
        goto VirtualFreeExit;
    }

    if ( dwFreeType & MEM_DECOMMIT )
    {
        UINT_PTR StartBoundary  = 0;
        SIZE_T MemSize        = 0;

        if ( dwSize == 0 )
        {
            ERROR( "dwSize cannot be 0. \n" );
            pthrCurrent->SetLastError( ERROR_INVALID_PARAMETER );
            bRetVal = FALSE;
            goto VirtualFreeExit;
        }
        /*
         * A two byte range straddling 2 pages caues both pages to be either
         * released or decommitted. So round the dwSize up to the next page
         * boundary and round the lpAddress down to the next page boundary.
         */
        StartBoundary = (UINT_PTR) ALIGN_DOWN(lpAddress, GetVirtualPageSize());
        MemSize = ALIGN_UP((UINT_PTR)lpAddress + dwSize, GetVirtualPageSize()) - StartBoundary;

        PCMI pUnCommittedMem;
        pUnCommittedMem = VIRTUALFindRegionInformation( StartBoundary );
        if (!pUnCommittedMem)
        {
            ASSERT( "Unable to locate the region information.\n" );
            pthrCurrent->SetLastError( ERROR_INTERNAL_ERROR );
            bRetVal = FALSE;
            goto VirtualFreeExit;
        }

        TRACE( "Un-committing the following page(s) %d to %d.\n",
               StartBoundary, MemSize );

        // Explicitly calling mmap instead of mprotect here makes it
        // that much more clear to the operating system that we no
        // longer need these pages.
        if ( mmap( (LPVOID)StartBoundary, MemSize, PROT_NONE,
                   MAP_FIXED | MAP_ANON | MAP_PRIVATE, -1, 0 ) != MAP_FAILED )
        {
#if (MMAP_ANON_IGNORES_PROTECTION)
            if (mprotect((LPVOID) StartBoundary, MemSize, PROT_NONE) != 0)
            {
                ASSERT("mprotect failed to protect the region!\n");
                pthrCurrent->SetLastError(ERROR_INTERNAL_ERROR);
                munmap((LPVOID) StartBoundary, MemSize);
                bRetVal = FALSE;
                goto VirtualFreeExit;
            }
#endif  // MMAP_ANON_IGNORES_PROTECTION

#ifdef MADV_DONTDUMP
            // Do not include freed memory in coredump.
            madvise((LPVOID) StartBoundary, MemSize, MADV_DONTDUMP);
#endif
            goto VirtualFreeExit;
        }
        else
        {
            ASSERT( "mmap() returned an abnormal value.\n" );
            bRetVal = FALSE;
            pthrCurrent->SetLastError( ERROR_INTERNAL_ERROR );
            goto VirtualFreeExit;
        }
    }

    if ( dwFreeType & MEM_RELEASE )
    {
        PCMI pMemoryToBeReleased =
            VIRTUALFindRegionInformation( (UINT_PTR)lpAddress );

        if ( !pMemoryToBeReleased )
        {
            ERROR( "lpAddress must be the base address returned by VirtualAlloc.\n" );
            pthrCurrent->SetLastError( ERROR_INVALID_ADDRESS );
            bRetVal = FALSE;
            goto VirtualFreeExit;
        }
        if ( dwSize != 0 )
        {
            ERROR( "dwSize must be 0 if you are releasing the memory.\n" );
            pthrCurrent->SetLastError( ERROR_INVALID_PARAMETER );
            bRetVal = FALSE;
            goto VirtualFreeExit;
        }

        TRACE( "Releasing the following memory %d to %d.\n",
               pMemoryToBeReleased->startBoundary, pMemoryToBeReleased->memSize );

        if ( munmap( (LPVOID)pMemoryToBeReleased->startBoundary,
                     pMemoryToBeReleased->memSize ) == 0 )
        {
            if ( VIRTUALReleaseMemory( pMemoryToBeReleased ) == FALSE )
            {
                ASSERT( "Unable to remove the PCMI entry from the list.\n" );
                pthrCurrent->SetLastError( ERROR_INTERNAL_ERROR );
                bRetVal = FALSE;
                goto VirtualFreeExit;
            }
            pMemoryToBeReleased = NULL;
        }
        else
        {
            ASSERT( "Unable to unmap the memory, munmap() returned an abnormal value.\n" );
            pthrCurrent->SetLastError( ERROR_INTERNAL_ERROR );
            bRetVal = FALSE;
            goto VirtualFreeExit;
        }
    }

VirtualFreeExit:

    LogVaOperation(
        (dwFreeType & MEM_DECOMMIT) ? VirtualMemoryLogging::VirtualOperation::Decommit
                                    : VirtualMemoryLogging::VirtualOperation::Release,
        lpAddress,
        dwSize,
        dwFreeType,
        0,
        NULL,
        bRetVal);

    InternalLeaveCriticalSection(pthrCurrent, &virtual_critsec);
    LOGEXIT( "VirtualFree returning %s.\n", bRetVal == TRUE ? "TRUE" : "FALSE" );
    PERF_EXIT(VirtualFree);
    return bRetVal;
}


/*++
Function:
  VirtualProtect

See MSDN doc.
--*/
BOOL
PALAPI
VirtualProtect(
           IN LPVOID lpAddress,
           IN SIZE_T dwSize,
           IN DWORD flNewProtect,
           OUT PDWORD lpflOldProtect)
{
    BOOL     bRetVal = FALSE;
    PCMI     pEntry = NULL;
    SIZE_T   MemSize = 0;
    UINT_PTR StartBoundary = 0;
    SIZE_T   Index = 0;
    SIZE_T   NumberOfPagesToChange = 0;
    SIZE_T   OffSet = 0;
    CPalThread * pthrCurrent;

    PERF_ENTRY(VirtualProtect);
    ENTRY("VirtualProtect(lpAddress=%p, dwSize=%u, flNewProtect=%#x, "
          "flOldProtect=%p)\n",
          lpAddress, dwSize, flNewProtect, lpflOldProtect);

    pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &virtual_critsec);

    StartBoundary = (UINT_PTR) ALIGN_DOWN(lpAddress, GetVirtualPageSize());
    MemSize = ALIGN_UP((UINT_PTR)lpAddress + dwSize, GetVirtualPageSize()) - StartBoundary;

    if ( VIRTUALContainsInvalidProtectionFlags( flNewProtect ) )
    {
        ASSERT( "flProtect can be one of PAGE_NOACCESS, PAGE_READONLY, "
               "PAGE_READWRITE, PAGE_EXECUTE, PAGE_EXECUTE_READ "
               ", or PAGE_EXECUTE_READWRITE. \n" );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto ExitVirtualProtect;
    }

    if ( !lpflOldProtect)
    {
        ERROR( "lpflOldProtect was invalid.\n" );
        SetLastError( ERROR_NOACCESS );
        goto ExitVirtualProtect;
    }

    pEntry = VIRTUALFindRegionInformation( StartBoundary );
    if ( 0 == mprotect( (LPVOID)StartBoundary, MemSize,
                   W32toUnixAccessControl( flNewProtect ) ) )
    {
        /* Reset the access protection. */
        TRACE( "Number of pages to change %d, starting page %d \n",
               NumberOfPagesToChange, OffSet );
        /*
         * Set the old protection flags. We only use the first flag, so
         * if there were several regions with each with different flags only the
         * first region's protection flag will be returned.
         */
        *lpflOldProtect = PAGE_EXECUTE_READWRITE;

#ifdef MADV_DONTDUMP
        // Include or exclude memory from coredump based on the protection.
        int advise = flNewProtect == PAGE_NOACCESS ? MADV_DONTDUMP : MADV_DODUMP;
        madvise((LPVOID)StartBoundary, MemSize, advise);
#endif

        bRetVal = TRUE;
    }
    else
    {
        ERROR( "%s\n", strerror( errno ) );
        if ( errno == EINVAL )
        {
            SetLastError( ERROR_INVALID_ADDRESS );
        }
        else if ( errno == EACCES )
        {
            SetLastError( ERROR_INVALID_ACCESS );
        }
    }
ExitVirtualProtect:
    InternalLeaveCriticalSection(pthrCurrent, &virtual_critsec);

#if defined _DEBUG
    VIRTUALDisplayList();
#endif
    LOGEXIT( "VirtualProtect returning %s.\n", bRetVal == TRUE ? "TRUE" : "FALSE" );
    PERF_EXIT(VirtualProtect);
    return bRetVal;
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

#if HAVE_VM_ALLOCATE
//---------------------------------------------------------------------------------------
//
// Convert a vm_prot_t flag on the Mach kernel to the corresponding memory protection on Windows.
//
// Arguments:
//    protection - Mach protection to be converted
//
// Return Value:
//    Return the corresponding memory protection on Windows (e.g. PAGE_READ_WRITE, etc.)
//

static DWORD VirtualMapMachProtectToWinProtect(vm_prot_t protection)
{
    if (protection & VM_PROT_READ)
    {
        if (protection & VM_PROT_WRITE)
        {
            if (protection & VM_PROT_EXECUTE)
            {
                return PAGE_EXECUTE_READWRITE;
            }
            else
            {
                return PAGE_READWRITE;
            }
        }
        else
        {
            if (protection & VM_PROT_EXECUTE)
            {
                return PAGE_EXECUTE_READ;
            }
            else
            {
                return PAGE_READONLY;
            }
        }
    }
    else
    {
        if (protection & VM_PROT_WRITE)
        {
            if (protection & VM_PROT_EXECUTE)
            {
                return PAGE_EXECUTE_WRITECOPY;
            }
            else
            {
                return PAGE_WRITECOPY;
            }
        }
        else
        {
            if (protection & VM_PROT_EXECUTE)
            {
                return PAGE_EXECUTE;
            }
            else
            {
                return PAGE_NOACCESS;
            }
        }
    }
}

static void VM_ALLOCATE_VirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer)
{
    kern_return_t MachRet;
    vm_address_t vm_address;
    vm_size_t vm_size;
    vm_region_flavor_t vm_flavor;
    mach_msg_type_number_t infoCnt;
    mach_port_t object_name;
#ifdef HOST_64BIT
    vm_region_basic_info_data_64_t info;
    infoCnt = VM_REGION_BASIC_INFO_COUNT_64;
    vm_flavor = VM_REGION_BASIC_INFO_64;
#else
    vm_region_basic_info_data_t info;
    infoCnt = VM_REGION_BASIC_INFO_COUNT;
    vm_flavor = VM_REGION_BASIC_INFO;
#endif

    vm_address = (vm_address_t)lpAddress;
#ifdef HOST_64BIT
    MachRet = vm_region_64(
#else
    MachRet = vm_region(
#endif
                        mach_task_self(),
                        &vm_address,
                        &vm_size,
                        vm_flavor,
                        (vm_region_info_t)&info,
                        &infoCnt,
                        &object_name);
    if (MachRet != KERN_SUCCESS) {
        return;
    }

    if (vm_address > (vm_address_t)lpAddress) {
        /* lpAddress was pointing into a free region */
        lpBuffer->State = MEM_FREE;
        return;
    }

    lpBuffer->BaseAddress = (PVOID)vm_address;

    // We don't actually have any information on the Mach kernel which maps to AllocationProtect.
    lpBuffer->AllocationProtect = VM_PROT_NONE;

    lpBuffer->RegionSize = (SIZE_T)vm_size;

    if (info.reserved)
    {
        lpBuffer->State = MEM_RESERVE;
    }
    else
    {
        lpBuffer->State = MEM_COMMIT;
    }

    lpBuffer->Protect = VirtualMapMachProtectToWinProtect(info.protection);

    /* Note that if a mapped region and a private region are adjacent, this
        will return MEM_PRIVATE but the region size will span
        both the mapped and private regions. */
    if (!info.shared)
    {
        lpBuffer->Type = MEM_PRIVATE;
    }
    else
    {
        // What should this be?  It's either MEM_MAPPED or MEM_IMAGE, but without an image list,
        // we can't determine which one it is.
        lpBuffer->Type = MEM_MAPPED;
    }
}
#endif // HAVE_VM_ALLOCATE

/*++
Function:
  VirtualQuery

See MSDN doc.
--*/
SIZE_T
PALAPI
VirtualQuery(
         IN LPCVOID lpAddress,
         OUT PMEMORY_BASIC_INFORMATION lpBuffer,
         IN SIZE_T dwLength)
{
    PCMI     pEntry = NULL;
    UINT_PTR StartBoundary = 0;
    CPalThread * pthrCurrent;

    PERF_ENTRY(VirtualQuery);
    ENTRY("VirtualQuery(lpAddress=%p, lpBuffer=%p, dwLength=%u)\n",
          lpAddress, lpBuffer, dwLength);

    pthrCurrent = InternalGetCurrentThread();
    InternalEnterCriticalSection(pthrCurrent, &virtual_critsec);

    if ( !lpBuffer)
    {
        ERROR( "lpBuffer has to be a valid pointer.\n" );
        pthrCurrent->SetLastError( ERROR_NOACCESS );
        goto ExitVirtualQuery;
    }
    if ( dwLength < sizeof( *lpBuffer ) )
    {
        ERROR( "dwLength cannot be smaller then the size of *lpBuffer.\n" );
        pthrCurrent->SetLastError( ERROR_BAD_LENGTH );
        goto ExitVirtualQuery;
    }

    StartBoundary = ALIGN_DOWN((SIZE_T)lpAddress, GetVirtualPageSize());

#if MMAP_IGNORES_HINT
    // Make sure we have memory to map before we try to query it.
    VIRTUALGetBackingFile(pthrCurrent);

    // If we're suballocating, claim that any memory that isn't in our
    // suballocated block is already allocated. This keeps callers from
    // using these results to try to allocate those blocks and failing.
    if (StartBoundary < (UINT_PTR) gBackingBaseAddress ||
        StartBoundary >= (UINT_PTR) gBackingBaseAddress + BACKING_FILE_SIZE)
    {
        if (StartBoundary < (UINT_PTR) gBackingBaseAddress)
        {
            lpBuffer->RegionSize = (UINT_PTR) gBackingBaseAddress - StartBoundary;
        }
        else
        {
            lpBuffer->RegionSize = -StartBoundary;
        }
        lpBuffer->BaseAddress = (void *) StartBoundary;
        lpBuffer->State = MEM_COMMIT;
        lpBuffer->Type = MEM_MAPPED;
        lpBuffer->AllocationProtect = 0;
        lpBuffer->Protect = 0;
        goto ExitVirtualQuery;
    }
#endif  // MMAP_IGNORES_HINT

    /* Find the entry. */
    pEntry = VIRTUALFindRegionInformation( StartBoundary );

    if ( !pEntry )
    {
        /* Can't find a match, or no list present. */
        /* Next, looking for this region in file maps */
        if (!MAPGetRegionInfo((LPVOID)StartBoundary, lpBuffer))
        {
            // When all else fails, call vm_region() if it's available.

            // Initialize the State to be MEM_FREE, in which case AllocationBase, AllocationProtect,
            // Protect, and Type are all undefined.
            lpBuffer->BaseAddress = (LPVOID)StartBoundary;
            lpBuffer->RegionSize = 0;
            lpBuffer->State = MEM_FREE;
#if HAVE_VM_ALLOCATE
            VM_ALLOCATE_VirtualQuery(lpAddress, lpBuffer);
#endif
        }
    }
    else
    {
        /* Fill the structure.*/
        lpBuffer->AllocationProtect = pEntry->accessProtection;
        lpBuffer->BaseAddress = (LPVOID)StartBoundary;

        lpBuffer->Protect = pEntry->allocationType == MEM_COMMIT ?
            pEntry->accessProtection : 0;
        lpBuffer->RegionSize = pEntry->memSize;
        lpBuffer->State = pEntry->allocationType == MEM_COMMIT ?
            MEM_COMMIT : MEM_RESERVE;
        WARN( "Ignoring lpBuffer->Type. \n" );
    }

ExitVirtualQuery:

    InternalLeaveCriticalSection(pthrCurrent, &virtual_critsec);

    LOGEXIT( "VirtualQuery returning %d.\n", sizeof( *lpBuffer ) );
    PERF_EXIT(VirtualQuery);
    return sizeof( *lpBuffer );
}

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
    InternalEnterCriticalSection(pThread, &virtual_critsec);
    void* mem = g_executableMemoryAllocator.AllocateMemory(allocationSize);
    InternalLeaveCriticalSection(pThread, &virtual_critsec);

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
    rlimit addressSpaceLimit;
    if ((getrlimit(RLIMIT_AS, &addressSpaceLimit) == 0) && (addressSpaceLimit.rlim_cur != RLIM_INFINITY))
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
    coreclrLoadAddress = (UINT_PTR)PAL_GetSymbolModuleBase((void*)VirtualAlloc);
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
    // VIRTUALReserveMemory() aligns down the specified address to a 64 KB granularity, and as long as that is necessary, the
    // reservation size here must be aligned to a 64 KB granularity to guarantee that all returned addresses are also aligned to
    // a 64 KB granularity. Otherwise, attempting to reserve memory starting from an unaligned address returned by this function
    // would fail in VIRTUALReserveMemory.
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
