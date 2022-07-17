// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/virtual.h

Abstract:
    Header file for virtual memory management.



--*/

#ifndef _PAL_VIRTUAL_H_
#define _PAL_VIRTUAL_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _CMI {

    struct _CMI * pNext;        /* Link to the next entry. */
    struct _CMI * pPrevious;    /* Link to the previous entry. */

    UINT_PTR startBoundary;     /* Starting location of the region. */
    SIZE_T   memSize;           /* Size of the entire region.. */

    DWORD  accessProtection;    /* Initial allocation access protection. */
    DWORD  allocationType;      /* Initial allocation type. */

    BYTE * pAllocState;         /* Individual allocation type tracking for each */
                                /* page in the region. */

    BYTE * pProtectionState;    /* Individual allocation type tracking for each */
                                /* page in the region. */

} CMI, * PCMI;

enum VIRTUAL_CONSTANTS
{
    /* Allocation type. */
    VIRTUAL_COMMIT_ALL_BITS     = 0xFF,
    VIRTUAL_RESERVE_ALL_BITS    = 0x0,

    /* Protection Type. */
    VIRTUAL_READONLY,
    VIRTUAL_READWRITE,
    VIRTUAL_EXECUTE_READWRITE,
    VIRTUAL_NOACCESS,
    VIRTUAL_EXECUTE,
    VIRTUAL_EXECUTE_READ,

    VIRTUAL_64KB            = 0x10000
};

size_t GetVirtualPageSize();

/*++
Function :
    VIRTUALInitialize

    Initialize the critical sections.

Return value:
    TRUE  if initialization succeeded
    FALSE otherwise.
--*/
BOOL VIRTUALInitialize(bool initializeExecutableMemoryAllocator);

/*++
Function :
    VIRTUALCleanup

    Deletes the critical sections.

--*/
void VIRTUALCleanup( void );

#ifdef __cplusplus
}

/*++
Class:
    ExecutableMemoryAllocator

    This class implements a virtual memory allocator for JIT'ed code.
    The purpose of this allocator is to opportunistically reserve a chunk of virtual memory
    that is located near the coreclr library (within 2GB range) that can be later used by
    JIT. Having executable memory close to the coreclr library allows JIT to generate more
    efficient code (by avoiding usage of jump stubs) and thus it can significantly improve
    performance of the application.

    This allocator is integrated with the VirtualAlloc/Reserve code. If VirtualAlloc has been
    called with the MEM_RESERVE_EXECUTABLE flag then it will first try to obtain the requested size
    of virtual memory from ExecutableMemoryAllocator. If ExecutableMemoryAllocator runs out of
    the reserved memory (or fails to allocate it during initialization) then VirtualAlloc/Reserve code
    will simply fall back to reserving memory using OS APIs.

    Notes:
        - the memory allocated by this class is NOT committed by default. It is responsibility
          of the caller to commit the virtual memory before accessing it.
        - in addition, this class does not provide ability to free the reserved memory. The caller
          has full control of the memory it got from this allocator (i.e. the caller becomes
          the owner of the allocated memory), so it is caller's responsibility to free the memory
          if it is no longer needed.
--*/
class ExecutableMemoryAllocator
{
public:
    /*++
    Function:
        Initialize

        This function initializes the allocator. It should be called early during process startup
        (when process address space is pretty much empty) in order to have a chance to reserve
        sufficient amount of memory that is close to the coreclr library.
    --*/
    void Initialize();

    /*++
    Function:
        AllocateMemory

        This function attempts to allocate the requested amount of memory from its reserved virtual
        address space. The function will return null if the allocation request cannot
        be satisfied by the memory that is currently available in the allocator.
    --*/
    void* AllocateMemory(SIZE_T allocationSize);

    /*++
    Function:
        AllocateMemory

        This function attempts to allocate the requested amount of memory from its reserved virtual
        address space, if memory is available within the specified range. The function will return
        null if the allocation request cannot satisfied by the memory that is currently available in
        the allocator.
    --*/
    void *AllocateMemoryWithinRange(const void *beginAddress, const void *endAddress, SIZE_T allocationSize);

    /*++
    Function:
        GetPreferredRange

        Gets the preferred range, which is the range that the allocator will try to put code into.
        When this range is close to libcoreclr, it will additionally include libcoreclr's memory
        range, the purpose being that this can be used to check if we expect code to be close enough
        to libcoreclr to use IP-relative addressing.
    --*/
    void GetPreferredRange(void **start, void **end)
    {
        *start = m_preferredRangeStart;
        *end = m_preferredRangeEnd;
    }

private:
    /*++
    Function:
        TryReserveInitialMemory

        This function is called during initialization. It opportunistically tries to reserve
        a large chunk of virtual memory that can be later used to store JIT'ed code.
    --*/
    void TryReserveInitialMemory();

    /*++
    Function:
        GenerateRandomStartOffset

        This function returns a random offset (in multiples of the virtual page size)
        at which the allocator should start allocating memory from its reserved memory range.
    --*/
    int32_t GenerateRandomStartOffset();

private:

    // There does not seem to be an easy way find the size of a library on Unix.
    // So this constant represents an approximation of the libcoreclr size
    // that can be used to calculate an approximate location of the memory that
    // is in 2GB range from the coreclr library. In addition, having precise size of libcoreclr
    // is not necessary for the calculations.
    static const int32_t CoreClrLibrarySize = 16 * 1024 * 1024;

    // This constant represent the max size of the virtual memory that this allocator
    // will try to reserve during initialization. We want all JIT-ed code and the
    // entire libcoreclr to be located in a 2GB range on x86
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // It seems to be more difficult to reserve a 2Gb chunk on arm so we'll try smaller one
    static const int32_t MaxExecutableMemorySize = 1024 * 1024 * 1024;
#else
    static const int32_t MaxExecutableMemorySize = 0x7FFF0000;
#endif

    static const int32_t MaxExecutableMemorySizeNearCoreClr = MaxExecutableMemorySize - CoreClrLibrarySize;

    // Start address of the reserved virtual address space
    void* m_startAddress = NULL;

    // Next available address in the reserved address space
    void* m_nextFreeAddress = NULL;

    // Total size of the virtual memory that the allocator has been able to
    // reserve during its initialization.
    int32_t m_totalSizeOfReservedMemory = 0;

    // Remaining size of the reserved virtual memory that can be used to satisfy allocation requests.
    int32_t m_remainingReservedMemory = 0;

    // Preferred range to report back to EE for where the allocator will put code.
    void* m_preferredRangeStart = NULL;
    void* m_preferredRangeEnd = NULL;
};

#endif // __cplusplus

/*++
Function :
    ReserveMemoryFromExecutableAllocator

    This function is used to reserve a region of virual memory (not committed)
    that is located close to the coreclr library. The memory comes from the virtual
    address range that is managed by ExecutableMemoryAllocator.
--*/
void* ReserveMemoryFromExecutableAllocator(CorUnix::CPalThread* pthrCurrent, SIZE_T allocationSize);

#endif /* _PAL_VIRTUAL_H_ */







