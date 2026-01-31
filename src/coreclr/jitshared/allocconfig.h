// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ALLOCCONFIG_H_
#define _ALLOCCONFIG_H_

#include <cstddef>

// IAllocatorConfig provides an abstraction layer for host-specific memory operations.
// This allows the ArenaAllocator to be used in different contexts (JIT, interpreter)
// with different underlying memory management strategies.

class IAllocatorConfig
{
public:
    // Returns true if the allocator should bypass the host allocator and use direct malloc/free.
    // This is typically used for debugging purposes (e.g., to take advantage of pageheap).
    virtual bool bypassHostAllocator() = 0;

    // Returns true if the allocator should inject faults for testing purposes.
    virtual bool shouldInjectFault() = 0;

    // Allocates a block of memory from the host.
    // size: The requested size in bytes.
    // pActualSize: On return, contains the actual size allocated (may be larger than requested).
    // Returns a pointer to the allocated memory, or calls outOfMemory() on failure.
    virtual void* allocateHostMemory(size_t size, size_t* pActualSize) = 0;

    // Frees a block of memory previously allocated by allocateHostMemory.
    // block: Pointer to the memory to free.
    // size: The size of the block (as returned in pActualSize from allocateHostMemory).
    virtual void freeHostMemory(void* block, size_t size) = 0;

    // Fills a memory block with an uninitialized pattern (for DEBUG builds).
    // This helps catch use-before-init bugs.
    // block: Pointer to the memory to fill.
    // size: The size of the block in bytes.
    virtual void fillWithUninitializedPattern(void* block, size_t size) = 0;

    // Called when the allocator runs out of memory.
    // This should not return (either throws an exception or terminates).
    virtual void outOfMemory() = 0;
};

#endif // _ALLOCCONFIG_H_
