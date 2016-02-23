// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************/

#ifndef _ALLOC_H_
#define _ALLOC_H_
/*****************************************************************************/
#ifndef _HOST_H_
#include "host.h"
#endif
/*****************************************************************************/

#ifdef _MSC_VER
#pragma warning(disable:4200)
#endif

/*****************************************************************************/
#if defined(DEBUG)

#include "malloc.h"

inline void * DbgNew(size_t size)
{
    return ClrAllocInProcessHeap(0, S_SIZE_T(size));
}

inline void DbgDelete(void * ptr)
{
    (void)ClrFreeInProcessHeap(0, ptr);
}

#endif // DEBUG

/*****************************************************************************/

struct nraMarkDsc

{
    void    *       nmPage;
    BYTE    *       nmNext;
    BYTE    *       nmLast;
};

struct norls_allocator
{
private:
    struct norls_pagdesc
    {
        norls_pagdesc * nrpNextPage;
        norls_pagdesc * nrpPrevPage;
#ifdef DEBUG
        void    *       nrpSelfPtr;
#endif
        size_t          nrpPageSize;    // # of bytes allocated
        size_t          nrpUsedSize;    // # of bytes actually used. (This is only valid when we've allocated a new page.)
                                        // See norls_allocator::nraAllocNewPage.
        BYTE            nrpContents[];
    };

    norls_pagdesc * nraPageList;
    norls_pagdesc * nraPageLast;

    BYTE    *       nraFreeNext;        // these two (when non-zero) will
    BYTE    *       nraFreeLast;        // always point into 'nraPageLast'

    size_t          nraPageSize;

#ifdef DEBUG
    bool            nraShouldInjectFault; // Should we inject fault?
#endif

    IEEMemoryManager* nraMemoryManager;

    void    *       nraAllocNewPage(size_t sz);

public:
    // Anything less than 64K leaves VM holes since the OS allocates address space in this size.
    // Thus if we want to make this smaller, we need to do a reserve / commit scheme
    enum { DEFAULT_PAGE_SIZE = (16 * OS_page_size) };
    enum { MIN_PAGE_SIZE = sizeof(norls_pagdesc) };

    bool            nraInit (IEEMemoryManager* pMemoryManager, size_t pageSize = 0, int preAlloc = 0);

    void            nraFree (void);

    void    *       nraAlloc(size_t sz);

    /* The following used for mark/release operation */

    void            nraMark(nraMarkDsc &mark)
    {
        mark.nmPage = nraPageLast;
        mark.nmNext = nraFreeNext;
        mark.nmLast = nraFreeLast;
    }

private:

    void            nraToss(nraMarkDsc &mark);

    LPVOID          nraVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect)
    {
#if defined(DEBUG)
        assert(lpAddress == 0 && flAllocationType == MEM_COMMIT && flProtect == PAGE_READWRITE);
        if (nraDirectAlloc())
        {
#undef GetProcessHeap
#undef HeapAlloc
            return ::HeapAlloc(GetProcessHeap(), 0, dwSize);
        }
        else
            return DbgNew(dwSize);
#else
        return nraMemoryManager->ClrVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
#endif
    }

    void            nraVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType)
    {
#if defined(DEBUG)
        assert(dwSize == 0 && dwFreeType == MEM_RELEASE);
        if (nraDirectAlloc())
        {
#undef GetProcessHeap
#undef HeapFree
            ::HeapFree(GetProcessHeap(), 0, lpAddress);
        }
        else
            DbgDelete(lpAddress);
#else
        nraMemoryManager->ClrVirtualFree(lpAddress, dwSize, dwFreeType);
#endif
    }

public:

    void            nraRlsm(nraMarkDsc &mark)
    {
        if (nraPageLast != mark.nmPage)
        {
            nraToss(mark);
        }
        else
        {
            nraFreeNext = mark.nmNext;
            nraFreeLast = mark.nmLast;
        }
    }

    size_t          nraTotalSizeAlloc();
    size_t          nraTotalSizeUsed ();

    IEEMemoryManager * nraGetMemoryManager()
    {
        return nraMemoryManager;
    }

    static bool     nraDirectAlloc();

#ifdef _TARGET_AMD64_
    /*
     * IGcInfoEncoderAllocator implementation (protected)
     *   - required to use GcInfoEncoder
     */
protected:
    void* Alloc(size_t size)
    {
        //GcInfoEncoder likes to allocate things of 0-size when m_NumSlots == 0
        //but nraAlloc doesn't like to allocate 0-size things.. so lets not let it
        return size ? nraAlloc(size) : NULL;
    }
    void Free( void* ) {}
#endif // _TARGET_AMD64_
};

#if !defined(DEBUG)

inline
void    *           norls_allocator::nraAlloc(size_t sz)
{
    void    *   block;

    block = nraFreeNext;
            nraFreeNext += sz;

    if  (nraFreeNext > nraFreeLast)
        block = nraAllocNewPage(sz);

    return  block;
}

#endif

/*****************************************************************************/
/*****************************************************************************
 * If most uses of the norls_alloctor are going to be non-simultaneous,
 * we keep a single instance handy and preallocate 1 chunk of 64K
 * Then most uses won't need to call VirtualAlloc() for the first page.
 */


#if defined(DEBUG)

inline bool norls_allocator::nraDirectAlloc()
{
    // When JitDirectAlloc is set, all JIT allocations requests are forwarded
    // directly to the OS. This allows taking advantage of pageheap and other gflag
    // knobs for ensuring that we do not have buffer overruns in the JIT.

    static ConfigDWORD fJitDirectAlloc;
    return (fJitDirectAlloc.val(CLRConfig::INTERNAL_JitDirectAlloc) != 0);
}

#else  // RELEASE

inline bool norls_allocator::nraDirectAlloc()
{
    return false;
}
#endif

extern size_t THE_ALLOCATOR_BASE_SIZE;

void                nraInitTheAllocator();  // One-time initialization
void                nraTheAllocatorDone();  // One-time completion code

// returns NULL if the single instance is already in use. 
// User will need to allocate a new instance of the norls_allocator

norls_allocator *   nraGetTheAllocator(IEEMemoryManager* pMemoryManager);

// Should be called after we are done with the current use, so that the
// next user can reuse it, instead of allocating a new instance

void                nraFreeTheAllocator();


/*****************************************************************************/
#endif  //  _ALLOC_H_
/*****************************************************************************/
