// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "arenaallocator.h"

//------------------------------------------------------------------------
// ArenaAllocator::ArenaAllocator:
//    Constructs an arena allocator with the specified configuration.
//
// Arguments:
//    config - Configuration interface for host-specific operations.
//
ArenaAllocator::ArenaAllocator(IAllocatorConfig* config)
    : m_firstPage(nullptr)
    , m_lastPage(nullptr)
    , m_nextFreeByte(nullptr)
    , m_lastFreeByte(nullptr)
    , m_config(config)
{
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
        // This will call the appropriate NOMEM handler
        m_config->allocateHostMemory(SIZE_MAX, nullptr);
        return nullptr; // unreachable
    }

    // If the current page is now full, update a few statistics
    if (m_lastPage != nullptr)
    {
        // Undo the "+=" done in allocateMemory()
        m_nextFreeByte -= size;

        // Save the actual used size of the page
        m_lastPage->m_usedBytes = m_nextFreeByte - m_lastPage->m_contents;
    }

    if (!m_config->bypassHostAllocator())
    {
        // Round to the nearest multiple of default page size
        pageSize = roundUp(pageSize, DEFAULT_PAGE_SIZE);
    }

    // Allocate the new page
    PageDescriptor* newPage = static_cast<PageDescriptor*>(m_config->allocateHostMemory(pageSize, &pageSize));

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
    m_lastFreeByte = (uint8_t*)newPage + pageSize;
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
        m_config->freeHostMemory(page, page->m_pageBytes);
    }

    // Clear out the allocator's fields
    m_firstPage = nullptr;
    m_lastPage = nullptr;
    m_nextFreeByte = nullptr;
    m_lastFreeByte = nullptr;
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
// ArenaAllocator::getTotalBytesUsed:
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
