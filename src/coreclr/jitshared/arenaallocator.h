// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ARENAALLOCATOR_H_
#define _ARENAALLOCATOR_H_

#include <cstddef>
#include <cstdint>
#include <cassert>

#include "jitshared.h"
#include "allocconfig.h"

// ArenaAllocator is a page-based arena allocator that provides fast allocation
// with bulk deallocation. Individual allocations cannot be freed; instead, all
// memory is released when destroy() is called.
//
// This allocator is designed for compilation scenarios where many small allocations
// are made during a single compilation unit, and all memory can be released at once
// when compilation completes.

class ArenaAllocator
{
private:
    ArenaAllocator(const ArenaAllocator& other) = delete;
    ArenaAllocator& operator=(const ArenaAllocator& other) = delete;
    ArenaAllocator& operator=(ArenaAllocator&& other) = delete;

    struct PageDescriptor
    {
        PageDescriptor* m_next;

        size_t m_pageBytes; // # of bytes allocated
        size_t m_usedBytes; // # of bytes actually used. (This is only valid when we've allocated a new page.)
                            // See ArenaAllocator::allocateNewPage.

        uint8_t m_contents[];
    };

    enum
    {
        DEFAULT_PAGE_SIZE = 0x10000,
    };

    PageDescriptor* m_firstPage;
    PageDescriptor* m_lastPage;

    // These two pointers (when non-null) will always point into 'm_lastPage'.
    uint8_t* m_nextFreeByte;
    uint8_t* m_lastFreeByte;

    // Configuration interface for host-specific operations.
    IAllocatorConfig* m_config;

    void* allocateNewPage(size_t size);

public:
    ArenaAllocator(IAllocatorConfig* config);

    // NOTE: it would be nice to have a destructor on this type to ensure that any value that
    //       goes out of scope is either uninitialized or has been torn down via a call to
    //       destroy(), but this interacts badly in methods that use SEH. #3058 tracks
    //       revisiting EH in the JIT; such a destructor could be added if SEH is removed
    //       as part of that work.

    void destroy();

    void* allocateMemory(size_t sz);

    size_t getTotalBytesAllocated();
    size_t getTotalBytesUsed();

    static size_t getDefaultPageSize()
    {
        return DEFAULT_PAGE_SIZE;
    }

    // Helper to round up to pointer-size alignment
    static size_t roundUp(size_t size, size_t alignment)
    {
        return (size + (alignment - 1)) & ~(alignment - 1);
    }
};

//------------------------------------------------------------------------
// ArenaAllocator::allocateMemory:
//    Allocates memory using an `ArenaAllocator`.
//
// Arguments:
//    size - The number of bytes to allocate.
//
// Return Value:
//    A pointer to the allocated memory.
//
// Note:
//    The DEBUG version of the method has some abilities that the release
//    version does not: it may inject faults into the allocator and
//    seeds all allocations with a specified pattern to help catch
//    use-before-init problems.
//
inline void* ArenaAllocator::allocateMemory(size_t size)
{
    assert(size != 0);

    // Ensure that we always allocate in pointer sized increments.
    size = roundUp(size, sizeof(size_t));

#if defined(DEBUG)
    if (m_config->shouldInjectFault())
    {
        // Force the underlying memory allocator (either the OS or the CLR hoster)
        // to allocate the memory. Any fault injection will kick in.
        size_t actualSize;
        void* p = m_config->allocateHostMemory(1, &actualSize);
        m_config->freeHostMemory(p, actualSize);
    }
#endif

    void* block = m_nextFreeByte;
    m_nextFreeByte += size;

    if (m_nextFreeByte > m_lastFreeByte)
    {
        block = allocateNewPage(size);
    }

#if defined(DEBUG)
    m_config->fillWithUninitializedPattern(block, size);
#endif

    return block;
}

#endif // _ARENAALLOCATOR_H_
