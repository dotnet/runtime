// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#if defined(_MSC_VER)
#pragma hdrstop
#endif // defined(_MSC_VER)

//------------------------------------------------------------------------
// ArenaAllocator::bypassHostAllocator:
//    Indicates whether or not the ArenaAllocator should bypass the JIT
//    host when allocating memory for arena pages.
//
// Return Value:
//    True if the JIT should bypass the JIT host; false otherwise.
bool ArenaAllocator::bypassHostAllocator()
{
#if defined(DEBUG)
    // When JitDirectAlloc is set, all JIT allocations requests are forwarded
    // directly to the OS. This allows taking advantage of pageheap and other gflag
    // knobs for ensuring that we do not have buffer overruns in the JIT.

    return JitConfig.JitDirectAlloc() != 0;
#else  // defined(DEBUG)
    return false;
#endif // !defined(DEBUG)
}

//------------------------------------------------------------------------
// ArenaAllocator::getDefaultPageSize:
//    Returns the default size of an arena page.
//
// Return Value:
//    The default size of an arena page.
size_t ArenaAllocator::getDefaultPageSize()
{
    return DEFAULT_PAGE_SIZE;
}

//------------------------------------------------------------------------
// ArenaAllocator::ArenaAllocator:
//    Default-constructs an arena allocator.
ArenaAllocator::ArenaAllocator()
    : m_firstPage(nullptr), m_lastPage(nullptr), m_nextFreeByte(nullptr), m_lastFreeByte(nullptr)
{
#if MEASURE_MEM_ALLOC
    memset(&m_stats, 0, sizeof(m_stats));
    memset(&m_statsAllocators, 0, sizeof(m_statsAllocators));
#endif // MEASURE_MEM_ALLOC
}

//------------------------------------------------------------------------
// ArenaAllocator::allocateNewPage:
//    Allocates a new arena page.
//
// Arguments:
//    size - The number of bytes that were requested by the allocation
//           that triggered this request to allocate a new arena page.
//
// Return Value:
//    A pointer to the first usable byte of the newly allocated page.
void* ArenaAllocator::allocateNewPage(size_t size)
{
    size_t pageSize = sizeof(PageDescriptor) + size;

    // Check for integer overflow
    if (pageSize < size)
    {
        NOMEM();
    }

    // If the current page is now full, update a few statistics
    if (m_lastPage != nullptr)
    {
        // Undo the "+=" done in allocateMemory()
        m_nextFreeByte -= size;

        // Save the actual used size of the page
        m_lastPage->m_usedBytes = m_nextFreeByte - m_lastPage->m_contents;
    }

    if (!bypassHostAllocator())
    {
        // Round to the nearest multiple of default page size
        pageSize = roundUp(pageSize, DEFAULT_PAGE_SIZE);
    }

    // Allocate the new page
    PageDescriptor* newPage = static_cast<PageDescriptor*>(allocateHostMemory(pageSize, &pageSize));

    // Append the new page to the end of the list
    newPage->m_next      = nullptr;
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
    m_lastFreeByte = (BYTE*)newPage + pageSize;
    assert((m_lastFreeByte - m_nextFreeByte) >= 0);

    return newPage->m_contents;
}

//------------------------------------------------------------------------
// ArenaAllocator::destroy:
//    Performs any necessary teardown for an `ArenaAllocator`.
void ArenaAllocator::destroy()
{
    PageDescriptor* page = m_firstPage;

    // Free all of the allocated pages
    for (PageDescriptor* next; page != nullptr; page = next)
    {
        next = page->m_next;
        freeHostMemory(page, page->m_pageBytes);
    }

    // Clear out the allocator's fields
    m_firstPage    = nullptr;
    m_lastPage     = nullptr;
    m_nextFreeByte = nullptr;
    m_lastFreeByte = nullptr;
}

//------------------------------------------------------------------------
// ArenaAllocator::allocateHostMemory:
//    Allocates memory from the host (or the OS if `bypassHostAllocator()`
//    returns `true`).
//
// Arguments:
//    size - The number of bytes to allocate.
//    pActualSize - The number of byte actually allocated.
//
// Return Value:
//    A pointer to the allocated memory.
void* ArenaAllocator::allocateHostMemory(size_t size, size_t* pActualSize)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        *pActualSize = size;
        if (size == 0)
        {
            size = 1;
        }
        void* p = malloc(size);
        if (p == nullptr)
        {
            NOMEM();
        }
        return p;
    }
#endif // !defined(DEBUG)

    return g_jitHost->allocateSlab(size, pActualSize);
}

//------------------------------------------------------------------------
// ArenaAllocator::freeHostMemory:
//    Frees memory allocated by a previous call to `allocateHostMemory`.
//
// Arguments:
//    block - A pointer to the memory to free.
void ArenaAllocator::freeHostMemory(void* block, size_t size)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        free(block);
        return;
    }
#endif // !defined(DEBUG)

    g_jitHost->freeSlab(block, size);
}

//------------------------------------------------------------------------
// ArenaAllocator::getTotalBytesAllocated:
//    Gets the total number of bytes allocated for all of the arena pages
//    for an `ArenaAllocator`.
//
// Return Value:
//    See above.
size_t ArenaAllocator::getTotalBytesAllocated()
{
    size_t bytes = 0;
    for (PageDescriptor* page = m_firstPage; page != nullptr; page = page->m_next)
    {
        bytes += page->m_pageBytes;
    }

    return bytes;
}

//------------------------------------------------------------------------
// ArenaAllocator::getTotalBytesAllocated:
//    Gets the total number of bytes used in all of the arena pages for
//    an `ArenaAllocator`.
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
size_t ArenaAllocator::getTotalBytesUsed()
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

#if MEASURE_MEM_ALLOC
CritSecObject                     ArenaAllocator::s_statsLock;
ArenaAllocator::AggregateMemStats ArenaAllocator::s_aggStats;
ArenaAllocator::MemStats          ArenaAllocator::s_maxStats;

const char* ArenaAllocator::MemStats::s_CompMemKindNames[] = {
#define CompMemKindMacro(kind) #kind,
#include "compmemkind.h"
};

void ArenaAllocator::MemStats::Print(FILE* f)
{
    fprintf(f, "count: %10u, size: %10llu, max = %10llu\n", allocCnt, allocSz, allocSzMax);
    fprintf(f, "allocateMemory: %10llu, nraUsed: %10llu\n", nraTotalSizeAlloc, nraTotalSizeUsed);
    PrintByKind(f);
}

void ArenaAllocator::MemStats::PrintByKind(FILE* f)
{
    fprintf(f, "\nAlloc'd bytes by kind:\n  %20s | %10s | %7s\n", "kind", "size", "pct");
    fprintf(f, "  %20s-+-%10s-+-%7s\n", "--------------------", "----------", "-------");
    float allocSzF = static_cast<float>(allocSz);
    for (int cmk = 0; cmk < CMK_Count; cmk++)
    {
        float pct = 100.0f * static_cast<float>(allocSzByKind[cmk]) / allocSzF;
        fprintf(f, "  %20s | %10llu | %6.2f%%\n", s_CompMemKindNames[cmk], allocSzByKind[cmk], pct);
    }
    fprintf(f, "\n");
}

void ArenaAllocator::AggregateMemStats::Print(FILE* f)
{
    fprintf(f, "For %9u methods:\n", nMethods);
    if (nMethods == 0)
    {
        return;
    }
    fprintf(f, "  count:       %12u (avg %7u per method)\n", allocCnt, allocCnt / nMethods);
    fprintf(f, "  alloc size : %12llu (avg %7llu per method)\n", allocSz, allocSz / nMethods);
    fprintf(f, "  max alloc  : %12llu\n", allocSzMax);
    fprintf(f, "\n");
    fprintf(f, "  allocateMemory   : %12llu (avg %7llu per method)\n", nraTotalSizeAlloc, nraTotalSizeAlloc / nMethods);
    fprintf(f, "  nraUsed    : %12llu (avg %7llu per method)\n", nraTotalSizeUsed, nraTotalSizeUsed / nMethods);
    PrintByKind(f);
}

ArenaAllocator::MemStatsAllocator* ArenaAllocator::getMemStatsAllocator(CompMemKind kind)
{
    assert(kind < CMK_Count);

    if (m_statsAllocators[kind].m_arena == nullptr)
    {
        m_statsAllocators[kind].m_arena = this;
        m_statsAllocators[kind].m_kind  = kind;
    }

    return &m_statsAllocators[kind];
}

void ArenaAllocator::finishMemStats()
{
    m_stats.nraTotalSizeAlloc = getTotalBytesAllocated();
    m_stats.nraTotalSizeUsed  = getTotalBytesUsed();

    CritSecHolder statsLock(s_statsLock);
    s_aggStats.Add(m_stats);
    if (m_stats.allocSz > s_maxStats.allocSz)
    {
        s_maxStats = m_stats;
    }
}

void ArenaAllocator::dumpMemStats(FILE* file)
{
    m_stats.Print(file);
}

void ArenaAllocator::dumpAggregateMemStats(FILE* file)
{
    s_aggStats.Print(file);
}

void ArenaAllocator::dumpMaxMemStats(FILE* file)
{
    s_maxStats.Print(file);
}
#endif // MEASURE_MEM_ALLOC
