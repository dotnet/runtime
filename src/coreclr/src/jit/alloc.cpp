// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"

#if defined(_MSC_VER)
#pragma hdrstop
#endif // defined(_MSC_VER)

size_t ArenaAllocator::s_defaultPageSize = 0;
ArenaAllocator* ArenaAllocator::s_pooledAllocator;
ArenaAllocator::MarkDescriptor ArenaAllocator::s_pooledAllocatorMark;
LONG ArenaAllocator::s_isPooledAllocatorInUse = 0;

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

    static ConfigDWORD s_jitDirectAlloc;
    return s_jitDirectAlloc.val(CLRConfig::INTERNAL_JitDirectAlloc) != 0;
#else // defined(DEBUG)
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
    return s_defaultPageSize;
}

//------------------------------------------------------------------------
// ArenaAllocator::initialize:
//    Intializes an arena allocator.
//
// Arguments:
//    memoryManager - The `IEEMemoryManager` instance that will be used to
//                    allocate memory for arena pages.
//
//    shuoldPreallocate - True if the allocator should allocate an initial
//                        arena page as part of initialization.
//
// Return Value:
//    True if initialization succeeded; false otherwise.
bool ArenaAllocator::initialize(IEEMemoryManager* memoryManager, bool shouldPreallocate)
{
    assert(s_defaultPageSize != 0);

    m_memoryManager = memoryManager;

    m_firstPage = nullptr;
    m_lastPage = nullptr;
    m_nextFreeByte = nullptr;
    m_lastFreeByte = nullptr;

    bool result = true;
    if (shouldPreallocate)
    {
        // Grab the initial page.
        setErrorTrap(NULL, ArenaAllocator*, thisPtr, this)  // ERROR TRAP: Start normal block
        {
            thisPtr->allocateNewPage(0);
        }
        impJitErrorTrap()  // ERROR TRAP: The following block handles errors
        {
            result = false;
        }
        endErrorTrap()  // ERROR TRAP: End
    }

    return result;
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

    // Round up to a default-sized page if necessary
    if (pageSize <= s_defaultPageSize)
    {
        pageSize = s_defaultPageSize;
    }

    // Round to the nearest multiple of OS page size if necessary
    if (!bypassHostAllocator())
    {
        pageSize = roundUp(pageSize, DEFAULT_PAGE_SIZE);
    }

    // Allocate the new page
    PageDescriptor* newPage = (PageDescriptor*)allocateHostMemory(pageSize);
    if (newPage == nullptr)
    {
        NOMEM();
    }

    // Append the new page to the end of the list
    newPage->m_next = nullptr;
    newPage->m_pageBytes = pageSize;
    newPage->m_previous = m_lastPage;
    newPage->m_usedBytes = 0;  // m_usedBytes is meaningless until a new page is allocated.
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
//
// Notes:
//    This method walks from `m_firstPage` forward and releases the pages.
//    Be careful no other thread has called `reset` at the same time.
//    Otherwise, the page specified by `page` could be double-freed
//    (VSW 600919).
void ArenaAllocator::destroy()
{
    // Free all of the allocated pages
    for (PageDescriptor* page = m_firstPage, *next; page != nullptr; page = next)
    {
        next = page->m_next;
        freeHostMemory(page);
    }

    // Clear out the allocator's fields
    m_firstPage = nullptr;
    m_lastPage = nullptr;
    m_nextFreeByte = nullptr;
    m_lastFreeByte = nullptr;
}

//------------------------------------------------------------------------
// ArenaAllocator::mark:
//    Stores the current position of an `ArenaAllocator` in the given mark.
//
// Arguments:
//    mark - The mark that will store the current position of the
//           allocator.
void ArenaAllocator::mark(MarkDescriptor& mark)
{
    mark.m_page = m_lastPage;
    mark.m_next = m_nextFreeByte;
    mark.m_last = m_lastFreeByte;
}

//------------------------------------------------------------------------
// ArenaAllocator::reset:
//    Resets the current position of an `ArenaAllocator` to the given
//    mark, freeing any unused pages.
//
// Arguments:
//    mark - The mark that stores the desired position for the allocator.
//
// Notes:
//    This method may walk the page list backward and release the pages.
//    Be careful no other thread is doing `destroy` as the same time.
//    Otherwise, the page specified by `temp` could be double-freed
//    (VSW 600919).
void ArenaAllocator::reset(MarkDescriptor& mark)
{
    // If the active page hasn't changed, just reset the position into the
    // page and return.
    if (m_lastPage == mark.m_page)
    {
        m_nextFreeByte = mark.m_next;
        m_lastFreeByte = mark.m_last;
        return;
    }

    // Otherwise, free any new pages that were added.
    void* last = mark.m_page;

    if (last == nullptr)
    {
        if (m_firstPage == nullptr)
        {
            return;
        }

        m_nextFreeByte = m_firstPage->m_contents;
        m_lastFreeByte = m_firstPage->m_pageBytes + (BYTE*)m_firstPage;
        return;
    }

    while (m_lastPage != last)
    {
        // Remove the last page from the end of the list
        PageDescriptor* temp = m_lastPage;
        m_lastPage = temp->m_previous;

        // The new last page has no next page
        m_lastPage->m_next = nullptr;

        freeHostMemory(temp);
    }

    m_nextFreeByte = mark.m_next;
    m_lastFreeByte = mark.m_last;
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
void* ArenaAllocator::allocateHostMemory(size_t size)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        return ::HeapAlloc(GetProcessHeap(), 0, size);
    }
    else
    {
        return ClrAllocInProcessHeap(0, S_SIZE_T(size));
    }
#else // defined(DEBUG)
    return m_memoryManager->ClrVirtualAlloc(nullptr, size, MEM_COMMIT, PAGE_READWRITE);
#endif // !defined(DEBUG)
}

//------------------------------------------------------------------------
// ArenaAllocator::freeHostMemory:
//    Frees memory allocated by a previous call to `allocateHostMemory`.
//
// Arguments:
//    block - A pointer to the memory to free.
void ArenaAllocator::freeHostMemory(void* block)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        ::HeapFree(GetProcessHeap(), 0, block);
    }
    else
    {
        ClrFreeInProcessHeap(0, block);
    }
#else // defined(DEBUG)
    m_memoryManager->ClrVirtualFree(block, 0, MEM_RELEASE);
#endif // !defined(DEBUG)
}

#if defined(DEBUG)
//------------------------------------------------------------------------
// ArenaAllocator::alloateMemory:
//    Allocates memory using an `ArenaAllocator`.
//
// Arguments:
//    size - The number of bytes to allocate.
//
// Return Value:
//    A pointer to the allocated memory.
//
// Note:
//    This is the DEBUG-only version of `allocateMemory`; the release
//    version of this method is defined in the corresponding header file.
//    This version of the method has some abilities that the release
//    version does not: it may inject faults into the allocator and
//    seeds all allocations with a specified pattern to help catch
//    use-before-init problems.
void* ArenaAllocator::allocateMemory(size_t size)
{
    assert(size != 0 && (size & (sizeof(int) - 1)) == 0);

    // Ensure that we always allocate in pointer sized increments.
    size = (size_t)roundUp(size, sizeof(size_t));

    static ConfigDWORD s_shouldInjectFault;
    if (s_shouldInjectFault.val(CLRConfig::INTERNAL_InjectFault) != 0)
    {
        // Force the underlying memory allocator (either the OS or the CLR hoster) 
        // to allocate the memory. Any fault injection will kick in.
        void* p = ClrAllocInProcessHeap(0, S_SIZE_T(1));
        if (p != nullptr)
        {
            ClrFreeInProcessHeap(0, p);
        }
        else 
        {
            NOMEM();  // Throw!
        }
    }

    void* block = m_nextFreeByte;
    m_nextFreeByte += size;

    if (m_nextFreeByte > m_lastFreeByte)
    {
        block = allocateNewPage(size);
    }

    memset(block, UninitializedWord<char>(), size);
    return block;
}
#endif // defined(DEBUG)

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

//------------------------------------------------------------------------
// ArenaAllocator::startup:
//    Performs any necessary initialization for the arena allocator
//    subsystem.
void ArenaAllocator::startup()
{
    s_defaultPageSize = bypassHostAllocator()
        ? (size_t)MIN_PAGE_SIZE
        : (size_t)DEFAULT_PAGE_SIZE;
}

//------------------------------------------------------------------------
// ArenaAllocator::shutdown:
//    Performs any necessary teardown for the arena allocator subsystem.
//
// Notes:
//    We chose not to call s_pooledAllocator->destroy() and let the memory leak.
//    Below is the reason (VSW 600919).
//    
//    The following race-condition exists during ExitProcess.
//    Thread A calls ExitProcess, which causes thread B to terminate.
//    Thread B terminated in the middle of reset() 
//    (through the call-chain of returnPooledAllocator()  ==> reset())
//    And then thread A comes along to call s_pooledAllocator->destroy() which will cause the double-free 
//    of page specified by "temp".
//    
//    These are possible fixes:
//    1. Thread A tries to get hold on s_isPooledAllocatorInUse lock before
//       calling s_theAllocator.destroy(). However, this could cause the deadlock because thread B
//       has already gone and therefore it can't release s_isPooledAllocatorInUse.
//    2. Fix the logic in reset() and destroy() to update m_firstPage and m_lastPage in a thread safe way.
//       But it needs careful work to make it high performant (e.g. not holding a lock?)
//    3. The scenario of dynamically unloading clrjit.dll cleanly is unimportant at this time.
//       We will leak the memory associated with other instances of ArenaAllocator anyway.
//    
//    Therefore we decided not to call the cleanup code when unloading the jit. 
void ArenaAllocator::shutdown()
{
}

// The static instance which we try to reuse for all non-simultaneous requests.
//
// We try to use this allocator instance as much as possible. It will always
// keep a page handy so small methods won't have to call VirtualAlloc()
// But we may not be able to use it if another thread/reentrant call
// is already using it.
static ArenaAllocator s_theAllocator;

//------------------------------------------------------------------------
// ArenaAllocator::getPooledAllocator:
//    Returns the pooled allocator if it is not already in use.
//
// Arguments:
//    memoryManager: The `IEEMemoryManager` instance in use by the caller.
//
// Return Value:
//    A pointer to the pooled allocator if it is available or `nullptr`
//    if it is already in use.
//
// Notes:
//    The returned `ArenaAllocator` should be given back to the pool by
//    calling `ArenaAllocator::returnPooledAllocator` when the caller has
//    finished using it.
ArenaAllocator* ArenaAllocator::getPooledAllocator(IEEMemoryManager* memoryManager)
{
    if (InterlockedExchange(&s_isPooledAllocatorInUse, 1))
    {
        // Its being used by another Compiler instance
        return nullptr;
    }

    if (s_pooledAllocator == nullptr)
    {
        // Not initialized yet

        bool res = s_theAllocator.initialize(memoryManager, true);
        if (!res)
        {
            // failed to initialize
            InterlockedExchange(&s_isPooledAllocatorInUse, 0);            
            return nullptr;
        }

        s_pooledAllocator = &s_theAllocator;
        
        assert(s_pooledAllocator->getTotalBytesAllocated() == s_defaultPageSize);
        s_pooledAllocator->mark(s_pooledAllocatorMark);    
    }
    else
    {
        if (s_pooledAllocator->m_memoryManager != memoryManager)
        {
            // already initialize with a different memory manager
            InterlockedExchange(&s_isPooledAllocatorInUse, 0);            
            return nullptr;
        }
    }

    assert(s_pooledAllocator->getTotalBytesAllocated() == s_defaultPageSize);
    return s_pooledAllocator;
}

//------------------------------------------------------------------------
// ArenaAllocator::returnPooledAllocator:
//    Returns the pooled allocator after the callee has finished using it.
//
// Arguments:
//    allocator - The pooled allocator instance. This must be an instance
//                that was obtained by a previous call to
//                `ArenaAllocator::getPooledAllocator`.
void ArenaAllocator::returnPooledAllocator(ArenaAllocator* allocator)
{
    assert(s_pooledAllocator != nullptr);
    assert(s_isPooledAllocatorInUse == 1);
    assert(allocator == s_pooledAllocator);

    s_pooledAllocator->reset(s_pooledAllocatorMark);
    assert(s_pooledAllocator->getTotalBytesAllocated() == s_defaultPageSize);

    InterlockedExchange(&s_isPooledAllocatorInUse, 0);
}
