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

    // Release a block of virtual memory previously commited by the CommitDoubleMappedMemory
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
};
