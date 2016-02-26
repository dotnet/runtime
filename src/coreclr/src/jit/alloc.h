// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _ALLOC_H_
#define _ALLOC_H_

#if !defined(_HOST_H_)
#include "host.h"
#endif // defined(_HOST_H_)

struct ArenaAllocator
{
private:
    struct MarkDescriptor
    {
        void* m_page;
        BYTE* m_next;
        BYTE* m_last;
    };

    struct PageDescriptor
    {
        PageDescriptor* m_next;
        PageDescriptor* m_previous;

        size_t m_pageBytes; // # of bytes allocated
        size_t m_usedBytes; // # of bytes actually used. (This is only valid when we've allocated a new page.)
                            // See ArenaAllocator::allocateNewPage.

        BYTE m_contents[];
    };

    // Anything less than 64K leaves VM holes since the OS allocates address space in this size.
    // Thus if we want to make this smaller, we need to do a reserve / commit scheme
    enum
    {
        DEFAULT_PAGE_SIZE = 16 * OS_page_size,
        MIN_PAGE_SIZE = sizeof(PageDescriptor)
    };

    static size_t s_defaultPageSize;
    static ArenaAllocator* s_pooledAllocator;
    static MarkDescriptor s_pooledAllocatorMark;
    static LONG s_isPooledAllocatorInUse;

    PageDescriptor* m_firstPage;
    PageDescriptor* m_lastPage;

    // These two pointers (when non-null) will always point into 'm_lastPage'.
    BYTE* m_nextFreeByte; 
    BYTE* m_lastFreeByte;

    IEEMemoryManager* m_memoryManager;

    void* allocateNewPage(size_t size);

    // The following methods are used for mark/release operation.
    void mark(MarkDescriptor& mark);
    void reset(MarkDescriptor& mark);

    void* allocateHostMemory(size_t size);
    void freeHostMemory(void* block);

public:
    bool initialize(IEEMemoryManager* memoryManager, bool shouldPreallocate);
    void destroy();

#if defined(DEBUG)
    void* allocateMemory(size_t sz);
#else // defined(DEBUG)
    inline void* allocateMemory(size_t size)
    {
        void* block = m_nextFreeByte;
        m_nextFreeByte += size;

        if (m_nextFreeByte > m_lastFreeByte)
        {
            block = allocateNewPage(size);
        }

        return block;
    }
#endif // !defined(DEBUG)

    size_t getTotalBytesAllocated();
    size_t getTotalBytesUsed();

    static bool bypassHostAllocator();
    static size_t getDefaultPageSize();

    static void startup();
    static void shutdown();

    // Gets the pooled allocator if it is available. Returns `nullptr` if the
    // pooled allocator is already in use.
    static ArenaAllocator* getPooledAllocator(IEEMemoryManager* memoryManager);

    // Returns the pooled allocator for use by others.
    static void returnPooledAllocator(ArenaAllocator* allocator);
};

#endif // _ALLOC_H_
