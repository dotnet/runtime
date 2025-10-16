// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#include <stddef.h>

// Interface between the runtime and platform specific functionality
class VMToOSInterface
{
private:
    ~VMToOSInterface() {}
public:
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