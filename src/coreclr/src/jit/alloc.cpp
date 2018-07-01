// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"

#if defined(_MSC_VER)
#pragma hdrstop
#endif // defined(_MSC_VER)

//------------------------------------------------------------------------
// SinglePagePool: Manage a single, default-sized page pool for ArenaAllocator.
//
//    Allocating a page is slightly costly as it involves the JIT host and
//    possibly the operating system as well. This pool avoids allocation
//    in many cases (i.e. for all non-concurrent method compilations).
//
class ArenaAllocator::SinglePagePool
{
    // The page maintained by this pool
    PageDescriptor* m_page;
    // The page available for allocation (either m_page or &m_shutdownPage if shutdown was called)
    PageDescriptor* m_availablePage;
    // A dummy page that is made available during shutdown
    PageDescriptor m_shutdownPage;

public:
    // Attempt to acquire the page managed by this pool.
    PageDescriptor* tryAcquirePage(IEEMemoryManager* memoryManager)
    {
        assert(memoryManager != nullptr);

        PageDescriptor* page = InterlockedExchangeT(&m_availablePage, nullptr);
        if ((page != nullptr) && (page->m_memoryManager != memoryManager))
        {
            // The pool page belongs to a different memory manager, release it.
            releasePage(page, page->m_memoryManager);
            page = nullptr;
        }

        assert((page == nullptr) || isPoolPage(page));

        return page;
    }

    // Attempt to pool the specified page.
    void tryPoolPage(PageDescriptor* page)
    {
        assert(page != &m_shutdownPage);

        // Try to pool this page, give up if another thread has already pooled a page.
        InterlockedCompareExchangeT(&m_page, page, nullptr);
    }

    // Check if a page is pooled.
    bool isEmpty()
    {
        return (m_page == nullptr);
    }

    // Check if the specified page is pooled.
    bool isPoolPage(PageDescriptor* page)
    {
        return (m_page == page);
    }

    // Release the specified page.
    PageDescriptor* releasePage(PageDescriptor* page, IEEMemoryManager* memoryManager)
    {
        // tryAcquirePage may end up releasing the shutdown page if shutdown was called.
        assert((page == &m_shutdownPage) || isPoolPage(page));
        assert((page == &m_shutdownPage) || (memoryManager != nullptr));

        // Normally m_availablePage should be null when releasePage is called but it can
        // be the shutdown page if shutdown is called while the pool page is in use.
        assert((m_availablePage == nullptr) || (m_availablePage == &m_shutdownPage));

        PageDescriptor* next = page->m_next;
        // Update the page's memory manager (replaces m_next that's not needed in this state).
        page->m_memoryManager = memoryManager;
        // Try to make the page available. This will fail if the pool was shutdown
        // and then we need to free the page here.
        PageDescriptor* shutdownPage = InterlockedCompareExchangeT(&m_availablePage, page, nullptr);
        if (shutdownPage != nullptr)
        {
            assert(shutdownPage == &m_shutdownPage);
            freeHostMemory(memoryManager, page);
        }

        // Return the next page for caller's convenience.
        return next;
    }

    // Free the pooled page.
    void shutdown()
    {
        // If the pool page is available then acquire it now so it can be freed.
        // Also make the shutdown page available so that:
        // - tryAcquirePage won't be return it because it has a null memory manager
        // - releasePage won't be able to make the pool page available and instead will free it
        PageDescriptor* page = InterlockedExchangeT(&m_availablePage, &m_shutdownPage);

        assert(page != &m_shutdownPage);
        assert((page == nullptr) || isPoolPage(page));

        if ((page != nullptr) && (page->m_memoryManager != nullptr))
        {
            freeHostMemory(page->m_memoryManager, page);
        }
    }
};

ArenaAllocator::SinglePagePool ArenaAllocator::s_pagePool = {};

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
    : m_memoryManager(nullptr)
    , m_firstPage(nullptr)
    , m_lastPage(nullptr)
    , m_nextFreeByte(nullptr)
    , m_lastFreeByte(nullptr)
{
    assert(!isInitialized());
}

//------------------------------------------------------------------------
// ArenaAllocator::initialize:
//    Initializes the arena allocator.
//
// Arguments:
//    memoryManager - The `IEEMemoryManager` instance that will be used to
//                    allocate memory for arena pages.
void ArenaAllocator::initialize(IEEMemoryManager* memoryManager)
{
    assert(!isInitialized());
    m_memoryManager = memoryManager;
    assert(isInitialized());

#if MEASURE_MEM_ALLOC
    memset(&m_stats, 0, sizeof(m_stats));
    memset(&m_statsAllocators, 0, sizeof(m_statsAllocators));
#endif // MEASURE_MEM_ALLOC
}

bool ArenaAllocator::isInitialized()
{
    return m_memoryManager != nullptr;
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
    assert(isInitialized());

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

    PageDescriptor* newPage        = nullptr;
    bool            tryPoolNewPage = false;

    if (!bypassHostAllocator())
    {
        // Round to the nearest multiple of OS page size
        pageSize = roundUp(pageSize, DEFAULT_PAGE_SIZE);

        // If this is the first time we allocate a page then try to use the pool page.
        if ((m_firstPage == nullptr) && (pageSize == DEFAULT_PAGE_SIZE))
        {
            newPage = s_pagePool.tryAcquirePage(m_memoryManager);

            if (newPage == nullptr)
            {
                // If there's no pool page yet then try to pool the newly allocated page.
                tryPoolNewPage = s_pagePool.isEmpty();
            }
            else
            {
                assert(newPage->m_memoryManager == m_memoryManager);
                assert(newPage->m_pageBytes == DEFAULT_PAGE_SIZE);
            }
        }
    }

    if (newPage == nullptr)
    {
        // Allocate the new page
        newPage = static_cast<PageDescriptor*>(allocateHostMemory(m_memoryManager, pageSize));

        if (newPage == nullptr)
        {
            NOMEM();
        }

        if (tryPoolNewPage)
        {
            s_pagePool.tryPoolPage(newPage);
        }
    }

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
    assert(isInitialized());

    PageDescriptor* page = m_firstPage;

    // If the first page is the pool page then return it to the pool.
    if ((page != nullptr) && s_pagePool.isPoolPage(page))
    {
        page = s_pagePool.releasePage(page, m_memoryManager);
    }

    // Free all of the allocated pages
    for (PageDescriptor* next; page != nullptr; page = next)
    {
        assert(!s_pagePool.isPoolPage(page));
        next = page->m_next;
        freeHostMemory(m_memoryManager, page);
    }

    // Clear out the allocator's fields
    m_memoryManager = nullptr;
    m_firstPage     = nullptr;
    m_lastPage      = nullptr;
    m_nextFreeByte  = nullptr;
    m_lastFreeByte  = nullptr;
    assert(!isInitialized());
}

// The debug version of the allocator may allocate directly from the
// OS rather than going through the hosting APIs. In order to do so,
// it must undef the macros that are usually in place to prevent
// accidental uses of the OS allocator.
#if defined(DEBUG)
#undef GetProcessHeap
#undef HeapAlloc
#undef HeapFree
#endif

//------------------------------------------------------------------------
// ArenaAllocator::allocateHostMemory:
//    Allocates memory from the host (or the OS if `bypassHostAllocator()`
//    returns `true`).
//
// Arguments:
//    size - The number of bytes to allocate.
//
// Return Value:
//    A pointer to the allocated memory.
void* ArenaAllocator::allocateHostMemory(IEEMemoryManager* memoryManager, size_t size)
{
    assert(memoryManager != nullptr);

#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        return ::HeapAlloc(GetProcessHeap(), 0, size);
    }
    else
    {
        return ClrAllocInProcessHeap(0, S_SIZE_T(size));
    }
#else  // defined(DEBUG)
    return memoryManager->ClrVirtualAlloc(nullptr, size, MEM_COMMIT, PAGE_READWRITE);
#endif // !defined(DEBUG)
}

//------------------------------------------------------------------------
// ArenaAllocator::freeHostMemory:
//    Frees memory allocated by a previous call to `allocateHostMemory`.
//
// Arguments:
//    block - A pointer to the memory to free.
void ArenaAllocator::freeHostMemory(IEEMemoryManager* memoryManager, void* block)
{
    assert(memoryManager != nullptr);

#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        ::HeapFree(GetProcessHeap(), 0, block);
    }
    else
    {
        ClrFreeInProcessHeap(0, block);
    }
#else  // defined(DEBUG)
    memoryManager->ClrVirtualFree(block, 0, MEM_RELEASE);
#endif // !defined(DEBUG)
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
    assert(isInitialized());

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
    assert(isInitialized());

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

//------------------------------------------------------------------------
// ArenaAllocator::shutdown:
//    Performs any necessary teardown for the arena allocator subsystem.
void ArenaAllocator::shutdown()
{
    s_pagePool.shutdown();
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
