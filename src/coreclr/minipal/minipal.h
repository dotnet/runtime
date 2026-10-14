// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef CORECLR_MINIPAL_H
#define CORECLR_MINIPAL_H

#include <stddef.h>
#include <stdint.h>

#if !defined(TARGET_WINDOWS) && !defined(TARGET_WASM)
#include <sys/mman.h>
#endif // !TARGET_WINDOWS && !TARGET_WASM

// Interface between the runtime and platform specific functionality
class VMToOSInterface
{
private:
    ~VMToOSInterface() {}
public:
#ifdef TARGET_WINDOWS
    using MemoryMapHandle = HANDLE;
    static inline const MemoryMapHandle InvalidFileHandle = INVALID_HANDLE_VALUE;
    static constexpr MemoryMapHandle InvalidMemoryMapHandle = nullptr;
    static constexpr bool MappingFailureAllowsCopyFallback = false;
#else
    using MemoryMapHandle = int;
    static constexpr MemoryMapHandle InvalidFileHandle = -1;
    static constexpr MemoryMapHandle InvalidMemoryMapHandle = -1;
    static constexpr bool MappingFailureAllowsCopyFallback = true;
#endif // TARGET_WINDOWS

#ifndef TARGET_WASM
    enum class MemoryProtection
    {
#ifdef TARGET_WINDOWS
        NoAccess = PAGE_NOACCESS,
        ReadOnly = PAGE_READONLY,
        ReadWrite = PAGE_READWRITE,
        WriteCopy = PAGE_WRITECOPY,
        Execute = PAGE_EXECUTE,
        ExecuteRead = PAGE_EXECUTE_READ,
        ExecuteReadWrite = PAGE_EXECUTE_READWRITE
#else
        NoAccess = PROT_NONE,
        ReadOnly = PROT_READ,
        ReadWrite = PROT_READ | PROT_WRITE,
        // The PAL never offered true copy-on-write page protection outside of file mappings;
        // treat write-copy the same as ordinary read-write on Unix.
        WriteCopy = PROT_READ | PROT_WRITE,
        Execute = PROT_EXEC,
        ExecuteRead = PROT_READ | PROT_EXEC,
        ExecuteReadWrite = PROT_READ | PROT_WRITE | PROT_EXEC
#endif // TARGET_WINDOWS
    };

    // Unix mappings borrow the supplied descriptor without duplicating or closing it.
    // The caller must keep it open until all required views have been mapped.
    static MemoryMapHandle CreateMemoryMapping(MemoryMapHandle fileHandle, uint64_t size, MemoryProtection protection);
    static bool CloseMemoryMapping(MemoryMapHandle mapping);

    // The caller retains ownership of the mapping handle or file descriptor.
    // Non-null addresses replace Windows placeholders or caller-owned Unix reservations.
    // Unix handle -1 requests anonymous private memory at offset zero.
    // Private writable file views use copy-on-write protection.
    // Failures return nullptr or -1 and set GetLastError() on Windows or errno on Unix.
    static void* MapMemory(MemoryMapHandle mapping, uint64_t offset, void* address, size_t size, MemoryProtection protection, bool shared);
    static int UnmapMemory(void* address, size_t size);

    // Windows executable backing allows later executable views without making this view executable.
    static void* MapAnonymousMemory(size_t size, MemoryProtection protection, bool executableBacking = false);
    static int32_t GetLastMemoryMappingError();

    // Allocate zero-initialized, page-aligned memory.
    // The size must be a nonzero multiple of the system page size.
    // Release the allocation with ReleaseMemoryReservation and its original size.
    static void* AllocateMemory(void* address, size_t size, MemoryProtection protection);

    // Allocate a page-aligned, nonzero-sized reservation whose starting address lies
    // within [rangeStart, rangeEnd), when a range is specified. Passing both rangeStart
    // and rangeEnd as nullptr requests an allocation anywhere in the address space, and
    // is equivalent to calling ReserveMemoryPlaceholder(nullptr, size).
    // Release the allocation with ReleaseMemoryReservation and its original size.
    static void* ReserveMemoryWithinRange(void* rangeStart, void* rangeEnd, size_t size);

    // Commit a page-aligned, nonzero-sized range previously returned by
    // ReserveMemoryWithinRange. Unlike CommitMemoryPlaceholder, the reservation does not
    // need placeholder support and is not split or replaced; the existing address range
    // simply becomes accessible with the requested protection.
    static void* CommitReservedMemory(void* address, size_t size, MemoryProtection protection);

    // Decommit (but do not release) a page-aligned, nonzero-sized range previously
    // committed via AllocateMemory, CommitMemoryPlaceholder, CommitReservedMemory, or
    // PopulateMemoryPlaceholder. Decommitted memory still belongs to the caller's
    // reservation; it is zero-filled on a later recommit and no longer occupies
    // physical/backing storage in the meantime.
    static bool DecommitMemory(void* address, size_t size);

    // Use placeholders to map file-backed sections at fixed offsets within one contiguous address range.
    // Replacing parts of the reservation preserves ownership of those addresses, avoiding races with other
    // allocations and allowing unmodified file-backed pages to stay shared instead of copying the image.
    static bool SupportsPlaceholderMapping();
    static void* ReserveMemoryPlaceholder(void* address, size_t size);
    static bool SplitMemoryPlaceholder(void* address, size_t size);
    static void* CommitMemoryPlaceholder(void* address, size_t size, MemoryProtection protection);
    static bool ReleaseMemoryReservation(void* address, size_t size);
    static bool ProtectMemory(void* address, size_t size, MemoryProtection protection);
    static bool PopulateMemoryPlaceholder(void* address, size_t allocationSize, const void* source,
        size_t copySize, MemoryProtection protection);
    static bool UseSharedReadOnlyMapping(size_t sectionAlignment);

    class MemoryMappingHolder
    {
        MemoryMapHandle m_mapping = InvalidMemoryMapHandle;

    public:
        MemoryMappingHolder() = default;
        ~MemoryMappingHolder()
        {
#ifndef DACCESS_COMPILE
            Reset();
#endif // !DACCESS_COMPILE
        }

        MemoryMappingHolder(const MemoryMappingHolder&) = delete;
        MemoryMappingHolder& operator=(const MemoryMappingHolder&) = delete;

        void Assign(MemoryMapHandle mapping);
        void Reset();
        MemoryMapHandle GetValue() const { return m_mapping; }
    };

    class MemoryMappedViewHolder
    {
        void* m_address = nullptr;
        size_t m_size = 0;

    public:
        MemoryMappedViewHolder() = default;
        ~MemoryMappedViewHolder()
        {
#ifndef DACCESS_COMPILE
            Reset();
#endif // !DACCESS_COMPILE
        }

        MemoryMappedViewHolder(const MemoryMappedViewHolder&) = delete;
        MemoryMappedViewHolder& operator=(const MemoryMappedViewHolder&) = delete;

        void Assign(void* address, size_t size);
        void Swap(MemoryMappedViewHolder& other);
        void Reset();
        void* GetValue() const { return m_address; }
    };

    class MemoryReservationHolder
    {
        void* m_address = nullptr;
        size_t m_size = 0;

    public:
        MemoryReservationHolder() = default;
        ~MemoryReservationHolder()
        {
#ifndef DACCESS_COMPILE
            Reset();
#endif // !DACCESS_COMPILE
        }

        MemoryReservationHolder(const MemoryReservationHolder&) = delete;
        MemoryReservationHolder& operator=(const MemoryReservationHolder&) = delete;

        void Assign(void* address, size_t size);
        void Reset();
        void* ExtractPrefix(size_t size);
        void* GetValue() const { return m_address; }
        size_t GetSize() const { return m_size; }
    };
#endif // !TARGET_WASM

#if defined(TARGET_UNIX) && !defined(TARGET_WASM)
    // Returns an error number directly, without discarding modified private pages.
    static int MarkMemoryAsNotNeeded(void* address, size_t size);
#endif // TARGET_UNIX && !TARGET_WASM

#ifdef TARGET_APPLE
    static bool IsMapJitFlagNeeded();
#endif // TARGET_APPLE

    // Create double mapped memory mapper
    // Parameters:
    //  pHandle                - receives handle of the double mapped memory mapper
    //  pMaxExecutableCodeSize - receives the maximum executable memory size it can map
    // Return:
    //  true if it succeeded, false if it failed
    static bool CreateDoubleMemoryMapper(void **pHandle, size_t *pMaxExecutableCodeSize);

    // Destroy the double mapped memory mapper represented by the passed in handle
    // Parameters:
    //  mapperHandle - handle of the double mapped memory mapper to destroy
    static void DestroyDoubleMemoryMapper(void *mapperHandle);

    // Reserve a block of memory that can be double mapped.
    // Parameters:
    //  mapperHandle - handle of the double mapped memory mapper to use
    //  offset       - offset in the underlying shared memory
    //  size         - size of the block to reserve
    //  rangeStart
    //  rangeEnd     - Requests reserving virtual memory in the specified range.
    //                 Setting both rangeStart and rangeEnd to 0 means that the
    //                 requested range is not limited.
    //                 When a specific range is requested, it is obligatory.
    // Return:
    //  starting virtual address of the reserved memory or NULL if it failed
    static void* ReserveDoubleMappedMemory(void *mapperHandle, size_t offset, size_t size, const void *rangeStart, const void* rangeEnd);

    // Commit a block of memory in the range previously reserved by the ReserveDoubleMappedMemory
    // Parameters:
    //  pStart       - start address of the virtual address range to commit
    //  size         - size of the memory block to commit
    //  isExecutable - true means that the mapping should be RX, false means RW
    // Return:
    //  Committed range start
    static void* CommitDoubleMappedMemory(void* pStart, size_t size, bool isExecutable);

    // Release a block of virtual memory previously committed by the CommitDoubleMappedMemory
    // Parameters:
    //  mapperHandle - handle of the double mapped memory mapper to use
    //  pStart       - start address of the virtual address range to release. It must be one
    //                 that was previously returned by the CommitDoubleMappedMemory
    //  offset       - offset in the underlying shared memory
    //  size         - size of the memory block to release
    // Return:
    //  true if it succeeded, false if it failed
    static bool ReleaseDoubleMappedMemory(void *mapperHandle, void* pStart, size_t offset, size_t size);

    // Get a RW mapping for the RX block specified by the arguments
    // Parameters:
    //  mapperHandle - handle of the double mapped memory mapper to use
    //  pStart       - start address of the RX virtual address range.
    //  offset       - offset in the underlying shared memory
    //  size         - size of the memory block to map as RW
    // Return:
    //  Starting virtual address of the RW mapping.
    static void* GetRWMapping(void *mapperHandle, void* pStart, size_t offset, size_t size);

    // Release RW mapping of the block specified by the arguments
    // Parameters:
    //  pStart       - Start address of the RW virtual address range. It must be an address
    //                 previously returned by the GetRWMapping.
    //  size         - Size of the memory block to release. It must be the size previously
    //                 passed to the GetRWMapping that returned the pStart.
    // Return:
    //  true if it succeeded, false if it failed
    static bool ReleaseRWMapping(void* pStart, size_t size);

    // Create a template for use by AllocateThunksFromTemplate
    // Parameters:
    //  pImageTemplate    - Address of start of template in the image for coreclr. (All addresses passed to the api in a process must be from the same module, if any call uses a pImageTemplate, all calls MUST)
    //  templateSize      - Size of the template
    //  codePageGenerator - If the system is unable to use pImageTemplate, use this parameter to generate the code page instead
    //
    // Return:
    //  NULL if creating the template fails
    //  Non-NULL, a pointer to the template
    static void* CreateTemplate(void* pImageTemplate, size_t templateSize, void (*codePageGenerator)(uint8_t* pageBase, uint8_t* pageBaseRX, size_t size));

    // Indicate if the AllocateThunksFromTemplate function respects the pStart address passed to AllocateThunksFromTemplate on this platform
    // Return:
    //  true if the parameter is respected, false if not
    static bool AllocateThunksFromTemplateRespectsStartAddress();

    // Allocate thunks from template
    // Parameters:
    //  pTemplate    - Value returned from CreateTemplate
    //  templateSize - Size of the templates block in the image
    //  pStart       - Where to allocate (Specify NULL if no particular address is required). If non-null, this must be an address returned by ReserveDoubleMappedMemory
    //  dataPageGenerator - If non-null fill the data page of the template using this function. This function is called BEFORE the code page is mapped into memory.
    //
    // Return:
    //  NULL if the allocation fails
    //  Non-NULL, a pointer to the allocated region.
    static void* AllocateThunksFromTemplate(void* pTemplate, size_t templateSize, void* pStart, void (*dataPageGenerator)(uint8_t* pageBase, size_t size));

    // Free thunks allocated from template
    // Parameters:
    //  pThunks      - Address previously returned by AllocateThunksFromTemplate
    //  templateSize - Size of the templates block in the image
    // Return:
    //  true if it succeeded, false if it failed
    static bool FreeThunksFromTemplate(void* thunks, size_t templateSize);

    // Allocate aligned memory block
    // Parameters:
    //  alignment - The desired alignment for the memory block.
    //  size      - The total number of bytes to allocate
    // Return:
    //  NULL if allocation fails
    //  Non-Null, a pointer to the allocated region
    static void* AlignedAllocate(size_t alignment, size_t size);

    // Free aligned memory block
    // Parameters:
    //  memblock - Memory block previously returned by AlignedAllocate
    static void AlignedFree(void* memblock);
};

#if defined(HOST_64BIT) && defined(FEATURE_CACHED_INTERFACE_DISPATCH)
EXTERN_C uint8_t _InterlockedCompareExchange128(int64_t volatile *, int64_t, int64_t, int64_t *);

#if defined(HOST_WINDOWS)
#pragma intrinsic(_InterlockedCompareExchange128)
#endif

FORCEINLINE uint8_t PalInterlockedCompareExchange128(_Inout_ int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparandAndResult)
{
    return _InterlockedCompareExchange128(pDst, iValueHigh, iValueLow, pComparandAndResult);
}
#endif // defined(HOST_64BIT) && defined(FEATURE_CACHED_INTERFACE_DISPATCH)

#endif // CORECLR_MINIPAL_H
