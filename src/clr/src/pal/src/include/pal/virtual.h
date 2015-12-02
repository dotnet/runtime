//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
    struct _CMI * pLast;        /* Link to the previous entry. */

    UINT_PTR   startBoundary;   /* Starting location of the region. */
    SIZE_T   memSize;         /* Size of the entire region.. */

    DWORD  accessProtection;    /* Initial allocation access protection. */
    DWORD  allocationType;      /* Initial allocation type. */

    BYTE * pAllocState;         /* Individual allocation type tracking for each */
                                /* page in the region. */

    BYTE * pProtectionState;    /* Individual allocation type tracking for each */
                                /* page in the region. */
#if MMAP_DOESNOT_ALLOW_REMAP
    BYTE * pDirtyPages;         /* Pages that need to be cleared if re-committed */
#endif // MMAP_DOESNOT_ALLOW_REMAP

}CMI, * PCMI;

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
    
    /* Page manipulation constants. */
#ifdef __sparc__
    VIRTUAL_PAGE_SIZE       = 0x2000,
#else   // __sparc__
    VIRTUAL_PAGE_SIZE       = 0x1000,
#endif  // __sparc__
    VIRTUAL_PAGE_MASK       = VIRTUAL_PAGE_SIZE - 1,
    BOUNDARY_64K    = 0xffff
};

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

/*++
Function :
    VIRTUALOwnedRegion

    Returns whether the space in question is owned the VIRTUAL system.

--*/
BOOL VIRTUALOwnedRegion( IN UINT_PTR address );


#ifdef __cplusplus
}

/// <summary>
/// This class implements a virtual memory allocator for JIT'ed code.
/// The purpose of this allocator is to opportunistically reserve a chuck of virtual memory
/// that is located near the coreclr library (within 2GB range) that can be later used by
/// JIT. Having executable memory close to the coreclr library allows JIT to generate more
/// efficient code (by avoiding usage of jump stubs) and thus it can significantly improve
/// performance of the application.
///
/// This allocator is integrated with the VirtualAlloc/Reserve code. If VirtualAlloc has been
/// called with the MEM_RESERVE_EXECUTABLE flag then it will first try to obtain the requested size
/// of virtual memory from ExecutableMemoryAllocator. If ExecutableMemoryAllocator runs out of
/// the reserved memory (or fails to allocate it during initialization) then VirtualAlloc/Reserve code
/// will simply fall back to reserving memory using OS APIs.
///
/// Notes:
///     - the memory allocated by this class is NOT committed by default. It is responsibility
///       of the caller to commit the virtual memory before accessing it.
///     - in addition, this class does not provide ability to free the reserved memory. The caller
///       has full control of the memory it got from this allocator (i.e. the caller becomes
///       the owner of the allocated memory), so it is caller's responsibility to free the memory
///       if it is no longer needed.
/// </summary>
class ExecutableMemoryAllocator
{
public:
    /// <summary>
    /// This function initializes the allocator. It should be called early during process startup
    /// (when process address space is pretty much empty) in order to have a chance to reserve
    /// sufficient amount of memory that is close to the coreclr library.
    /// </summary>
    void Initialize();

    /// <summary>
    /// This function attempts to allocate the requested amount of memory from its reserved virtual
    /// address space. The function will return NULL if the allocation request cannot
    /// be satisfied by the memory that is currently available in the allocator.
    /// </summary>
    LPVOID AllocateMemory(int32_t allocationSize);

private:
    /// <summary>
    /// This function is called during initialization. It opportunistically tries to reserve
    /// a large chunk of virtual memory that can be later used to store JIT'ed code.
    /// </summary>
    void TryReserveInitialMemory();

    /// <summary>
    /// This function returns a random offset (in multiples of the virtual page size)
    /// at which the allocator should start allocating memory from its reserved memory range.
    /// </summary>
    int32_t GenerateRandomStartOffset();

private:
    /// <summary>
    /// There does not seem to be an easy way find the size of a library on Unix.
    /// So this constant represents an approximation of the libcoreclr size (on debug build)
    /// that can be used to calculate an approximate location of the memory that
    /// is in 2GB range from the coreclr library. In addition, having precise size of libcoreclr
    /// is not necessary for the calculations.
    /// </summary>
    const int32_t CoreClrLibrarySize = 100 * 1024 * 1024;

    /// <summary>
    /// This constant represent the max size of the virtual memory that this allocator
    /// will try to reserve during initialization. We want all JIT-ed code and the
    /// entire libcoreclr to be located in a 2GB range.
    /// </summary>
    const int32_t MaxExecutableMemorySize = 0x7FFF0000 - CoreClrLibrarySize;

    /// <summary>Start address of the reserved virtual address space</summary>
    LPVOID m_startAddress;

    /// <summary>Next available address in the reserved address space</summary>
    LPVOID m_nextFreeAddress;

    /// <summary>
    /// Total size of the virtual memory that the allocator has been able to
    /// reserve during its initialization.
    /// </summary>
    int32_t m_totalSizeOfReservedMemory;

    /// <summary>
    /// Remaining size of the reserved virtual memory that can be used to satisfy allocation requests.
    /// </summary>
    int32_t m_remainingReservedMemory;
};

#endif // __cplusplus

#endif /* _PAL_VIRTUAL_H_ */







