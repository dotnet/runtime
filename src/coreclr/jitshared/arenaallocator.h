// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ARENAALLOCATOR_H_
#define _ARENAALLOCATOR_H_

#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include "minipal/utils.h"

#include "jitshared.h"
#include "memstats.h"

// ArenaAllocator is a page-based arena allocator that provides fast allocation
// with bulk deallocation. Individual allocations cannot be freed; instead, all
// memory is released when destroy() is called.
//
// This allocator is designed for compilation scenarios where many small allocations
// are made during a single compilation unit, and all memory can be released at once
// when compilation completes.
//
// Template parameters:
//   TMemKindTraits - A traits struct that must provide:
//     - MemKind: The enum type for memory kinds
//     - Count: A static constexpr int giving the number of enum values
//     - Names: A static const char* const[] array of names for each kind
//     - static bool bypassHostAllocator(): Whether to bypass host allocator
//     - static bool shouldInjectFault(): Whether to inject faults for testing
//     - static void* allocateHostMemory(size_t size, size_t* pActualSize): Allocate memory
//     - static void freeHostMemory(void* block, size_t size): Free memory
//     - static void fillWithUninitializedPattern(void* block, size_t size): Fill pattern
//     - static void outOfMemory(): Called on allocation failure (does not return)

template <typename TMemKindTraits>
class ArenaAllocatorT
{
public:
    using MemKind = typename TMemKindTraits::MemKind;

private:
    ArenaAllocatorT(const ArenaAllocatorT& other) = delete;
    ArenaAllocatorT& operator=(const ArenaAllocatorT& other) = delete;
    ArenaAllocatorT& operator=(ArenaAllocatorT&& other) = delete;

    struct PageDescriptor
    {
        PageDescriptor* m_next;

        size_t m_pageBytes; // # of bytes allocated
        size_t m_usedBytes; // # of bytes actually used. (This is only valid when we've allocated a new page.)
                            // See ArenaAllocatorT::allocateNewPage.

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

    NOINLINE
    void* allocateNewPage(size_t size);

#if MEASURE_MEM_ALLOC
public:
    // MemStatsAllocator is a helper that wraps an ArenaAllocatorT and tracks
    // allocations of a specific memory kind.
    struct MemStatsAllocator
    {
        ArenaAllocatorT* m_arena;
        MemKind          m_kind;

        void* allocateMemory(size_t sz)
        {
            m_arena->m_stats.AddAlloc(sz, m_kind);
            return m_arena->allocateMemory(sz);
        }
    };

private:
    MemStats<TMemKindTraits> m_stats;
    MemStatsAllocator m_statsAllocators[TMemKindTraits::Count];

public:
    MemStatsAllocator* getMemStatsAllocator(MemKind kind)
    {
        int kindIndex = static_cast<int>(kind);
        assert(kindIndex < TMemKindTraits::Count);

        if (m_statsAllocators[kindIndex].m_arena == nullptr)
        {
            m_statsAllocators[kindIndex].m_arena = this;
            m_statsAllocators[kindIndex].m_kind = kind;
        }

        return &m_statsAllocators[kindIndex];
    }

    void finishMemStats()
    {
        m_stats.nraTotalSizeAlloc = getTotalBytesAllocated();
        m_stats.nraTotalSizeUsed = getTotalBytesUsed();
    }

    MemStats<TMemKindTraits>& getStats() { return m_stats; }
    const MemStats<TMemKindTraits>& getStats() const { return m_stats; }

    void dumpMemStats(FILE* file)
    {
        m_stats.Print(file);
    }
#endif // MEASURE_MEM_ALLOC

public:
    ArenaAllocatorT()
        : m_firstPage(nullptr)
        , m_lastPage(nullptr)
        , m_nextFreeByte((uint8_t*)&m_firstPage)
        , m_lastFreeByte((uint8_t*)&m_firstPage)
    {
#if MEASURE_MEM_ALLOC
        memset(&m_statsAllocators, 0, sizeof(m_statsAllocators));
#endif
    }

    // NOTE: it would be nice to have a destructor on this type to ensure that any value that
    //       goes out of scope is either uninitialized or has been torn down via a call to
    //       destroy(), but this interacts badly in methods that use SEH. #3058 tracks
    //       revisiting EH in the JIT; such a destructor could be added if SEH is removed
    //       as part of that work. For situations where a destructor can be used, use ArenaAllocatorWithDestructorT instead.

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

template <typename TMemKindTraits>
class ArenaAllocatorWithDestructorT : public ArenaAllocatorT<TMemKindTraits>
{
public:
    ~ArenaAllocatorWithDestructorT()
    {
        this->destroy();
    }
};

//------------------------------------------------------------------------
// ArenaAllocatorT::allocateMemory:
//    Allocates memory using an `ArenaAllocatorT`.
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
template <typename TMemKindTraits>
inline void* ArenaAllocatorT<TMemKindTraits>::allocateMemory(size_t size)
{
    assert(size != 0);

    // Ensure that we always allocate in pointer sized increments.
    size = roundUp(size, sizeof(size_t));

#if defined(DEBUG)
    if (TMemKindTraits::shouldInjectFault())
    {
        // Force the underlying memory allocator (either the OS or the CLR hoster)
        // to allocate the memory. Any fault injection will kick in.
        size_t actualSize;
        void* p = TMemKindTraits::allocateHostMemory(1, &actualSize);
        TMemKindTraits::freeHostMemory(p, actualSize);
    }
#endif

    void* block = m_nextFreeByte;
    m_nextFreeByte += size;

    if (m_nextFreeByte > m_lastFreeByte)
    {
        block = allocateNewPage(size);
    }

#if defined(DEBUG)
    TMemKindTraits::fillWithUninitializedPattern(block, size);
#endif

    return block;
}

//------------------------------------------------------------------------
// ArenaAllocatorT::allocateNewPage:
//    Allocates a new arena page.
//
// Arguments:
//    size - The number of bytes that were requested by the allocation
//           that triggered this request to allocate a new arena page.
//
// Return Value:
//    A pointer to the first usable byte of the newly allocated page.
template <typename TMemKindTraits>
NOINLINE
void* ArenaAllocatorT<TMemKindTraits>::allocateNewPage(size_t size)
{
    size_t pageSize = sizeof(PageDescriptor) + size;

    // Check for integer overflow
    if (pageSize < size)
    {
        TMemKindTraits::outOfMemory();
    }

    // If the current page is now full, update a few statistics
    if (m_lastPage != nullptr)
    {
        // Undo the "+=" done in allocateMemory()
        m_nextFreeByte -= size;

        // Save the actual used size of the page
        m_lastPage->m_usedBytes = m_nextFreeByte - m_lastPage->m_contents;
    }

    if (!TMemKindTraits::bypassHostAllocator())
    {
        // Round to the nearest multiple of default page size
        pageSize = roundUp(pageSize, DEFAULT_PAGE_SIZE);
    }

    // Allocate the new page
    PageDescriptor* newPage = static_cast<PageDescriptor*>(TMemKindTraits::allocateHostMemory(pageSize, &pageSize));

    // Append the new page to the end of the list
    newPage->m_next = nullptr;
    newPage->m_pageBytes = pageSize;
    newPage->m_usedBytes = 0; // m_usedBytes is meaningless until a new page is allocated.
                              // Instead of letting it contain garbage (so to confuse us),
                              // set it to zero.

    if (m_lastPage != nullptr)
    {
        m_lastPage->m_next = newPage;
    }
    else
    {
        m_firstPage = newPage;
    }

    m_lastPage = newPage;

    // Adjust the next/last free byte pointers
    m_nextFreeByte = newPage->m_contents + size;
    m_lastFreeByte = reinterpret_cast<uint8_t*>(newPage) + pageSize;
    assert((m_lastFreeByte - m_nextFreeByte) >= 0);

    return newPage->m_contents;
}

//------------------------------------------------------------------------
// ArenaAllocatorT::destroy:
//    Performs any necessary teardown for an `ArenaAllocatorT`.
template <typename TMemKindTraits>
void ArenaAllocatorT<TMemKindTraits>::destroy()
{
    PageDescriptor* page = m_firstPage;

    // Free all of the allocated pages
    for (PageDescriptor* next; page != nullptr; page = next)
    {
        next = page->m_next;
        TMemKindTraits::freeHostMemory(page, page->m_pageBytes);
    }

    // Clear out the allocator's fields
    m_firstPage = nullptr;
    m_lastPage = nullptr;
    m_nextFreeByte = nullptr;
    m_lastFreeByte = nullptr;
}

//------------------------------------------------------------------------
// ArenaAllocatorT::getTotalBytesAllocated:
//    Gets the total number of bytes allocated for all of the arena pages
//    for an `ArenaAllocatorT`.
//
// Return Value:
//    See above.
template <typename TMemKindTraits>
size_t ArenaAllocatorT<TMemKindTraits>::getTotalBytesAllocated()
{
    size_t bytes = 0;
    for (PageDescriptor* page = m_firstPage; page != nullptr; page = page->m_next)
    {
        bytes += page->m_pageBytes;
    }

    return bytes;
}

//------------------------------------------------------------------------
// ArenaAllocatorT::getTotalBytesUsed:
//    Gets the total number of bytes used in all of the arena pages for
//    an `ArenaAllocatorT`.
//
// Return Value:
//    See above.
//
// Notes:
//    An arena page may have unused space at the very end. This happens
//    when an allocation request comes in (via a call to `allocateMemory`)
//    that will not fit in the remaining bytes for the current page.
//    Another way to understand this method is as returning the total
//    number of bytes allocated for arena pages minus the number of bytes
//    that are unused across all area pages.
template <typename TMemKindTraits>
size_t ArenaAllocatorT<TMemKindTraits>::getTotalBytesUsed()
{
    if (m_lastPage != nullptr)
    {
        m_lastPage->m_usedBytes = m_nextFreeByte - m_lastPage->m_contents;
    }

    size_t bytes = 0;
    for (PageDescriptor* page = m_firstPage; page != nullptr; page = page->m_next)
    {
        bytes += page->m_usedBytes;
    }

    return bytes;
}

#endif // _ARENAALLOCATOR_H_
