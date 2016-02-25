// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************/


#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
/*****************************************************************************/

/*****************************************************************************/
void                allocatorCodeSizeBeg(){}
/*****************************************************************************/
#ifdef  DEBUG
/*****************************************************************************/

void    __cdecl     debugStop(const char *why, ...)
{
    va_list     args;

    va_start(args, why);

    printf("NOTIFICATION: ");
    if  (why)
        vprintf(why, args);
    else
        printf("debugStop(0)");

    printf("\n");

    va_end(args);

    BreakIfDebuggerPresent();
}

/*****************************************************************************/

/* 
 * Does this constant need to be bigger?
 */
static  size_t    blockStop    = 99999999;

/*****************************************************************************/
#endif // DEBUG
/*****************************************************************************/

size_t THE_ALLOCATOR_BASE_SIZE  = 0;

bool   norls_allocator::nraInit(IEEMemoryManager* pMemoryManager, size_t pageSize, int preAlloc)
{
    bool    result = false;

    nraMemoryManager = pMemoryManager;

    nraPageList  =
    nraPageLast  = 0;

    nraFreeNext  =
    nraFreeLast  = 0;

    assert(THE_ALLOCATOR_BASE_SIZE != 0);

    nraPageSize  = pageSize ? pageSize : THE_ALLOCATOR_BASE_SIZE;

#ifdef DEBUG
    nraShouldInjectFault = JitConfig.ShouldInjectFault() != 0;
#endif    

    if  (preAlloc)
    {
        /* Grab the initial page(s) */

        setErrorTrap(NULL, norls_allocator *, pThis, this)  // ERROR TRAP: Start normal block
        {
            pThis->nraAllocNewPage(0);
        }
        impJitErrorTrap()  // ERROR TRAP: The following block handles errors
        {
            result = true;
        }
        endErrorTrap()  // ERROR TRAP: End
    }

    return  result;
}

/*---------------------------------------------------------------------------*/

void    *   norls_allocator::nraAllocNewPage(size_t sz)
{
    norls_pagdesc * newPage;
    size_t          sizPage;

    size_t          realSize = sz + sizeof(norls_pagdesc);
    if (realSize < sz) 
        NOMEM();   // Integer overflow

    /* Do we have a page that's now full? */

    if  (nraPageLast)
    {
        /* Undo the "+=" done in nraAlloc() */

        nraFreeNext -= sz;

        /* Save the actual used size of the page */

        nraPageLast->nrpUsedSize = nraFreeNext - nraPageLast->nrpContents;
    }

    /* Make sure we grab enough to satisfy the allocation request */

    sizPage = nraPageSize;

    if  (sizPage < realSize)
    {
        /* The allocation doesn't fit in a default-sized page */

#ifdef  DEBUG
//      if  (nraPageLast) printf("NOTE: wasted %u bytes in last page\n", nraPageLast->nrpPageSize - nraPageLast->nrpUsedSize);
#endif

        sizPage = realSize;
    }

    /* Round to the nearest multiple of OS page size */

    if (!nraDirectAlloc())
    {
        sizPage +=  (DEFAULT_PAGE_SIZE - 1);
        sizPage &= ~(DEFAULT_PAGE_SIZE - 1);
    }

    /* Allocate the new page */

    newPage = (norls_pagdesc *)nraVirtualAlloc(0, sizPage, MEM_COMMIT, PAGE_READWRITE);
    if  (!newPage)
        NOMEM();

#ifdef DEBUG
    newPage->nrpSelfPtr = newPage;
#endif

    /* Append the new page to the end of the list */

    newPage->nrpNextPage = 0;
    newPage->nrpPageSize = sizPage;
    newPage->nrpPrevPage = nraPageLast;
    newPage->nrpUsedSize = 0;  // nrpUsedSize is meaningless until a new page is allocated.
                               // Instead of letting it contain garbage (so to confuse us),
                               // set it to zero.

    if  (nraPageLast)
        nraPageLast->nrpNextPage = newPage;
    else
        nraPageList              = newPage;
    nraPageLast = newPage;

    /* Set up the 'next' and 'last' pointers */

    nraFreeNext = newPage->nrpContents + sz;
    nraFreeLast = newPage->nrpPageSize + (BYTE *)newPage;

    assert(nraFreeNext <= nraFreeLast);

    return  newPage->nrpContents;
}

// This method walks the nraPageList forward and release the pages.
// Be careful no other thread is doing nraToss at the same time.
// Otherwise, the page specified by temp could be double-freed (VSW 600919).

void        norls_allocator::nraFree(void)
{
    /* Free all of the allocated pages */

    while   (nraPageList)
    {
        norls_pagdesc * temp;

        temp = nraPageList;
               nraPageList = temp->nrpNextPage;

        nraVirtualFree(temp, 0, MEM_RELEASE);
    }
}

// This method walks the nraPageList backward and release the pages.
// Be careful no other thread is doing nraFree as the same time.
// Otherwise, the page specified by temp could be double-freed (VSW 600919).
void        norls_allocator::nraToss(nraMarkDsc &mark)
{
    void    *   last = mark.nmPage;

    if  (!last)
    {
        if  (!nraPageList)
            return;

        nraFreeNext  = nraPageList->nrpContents;
        nraFreeLast  = nraPageList->nrpPageSize + (BYTE *)nraPageList;

        return;
    }

    /* Free up all the new pages we've added at the end of the list */

    while (nraPageLast != last)
    {
        norls_pagdesc * temp;

        /* Remove the last page from the end of the list */

        temp = nraPageLast;
               nraPageLast = temp->nrpPrevPage;

        /* The new last page has no 'next' page */

        nraPageLast->nrpNextPage = 0;

        nraVirtualFree(temp, 0, MEM_RELEASE);
    }

    nraFreeNext = mark.nmNext;
    nraFreeLast = mark.nmLast;
}

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************/
void    *           norls_allocator::nraAlloc(size_t sz)
{
    void    *   block;

    assert(sz != 0 && (sz & (sizeof(int) - 1)) == 0);
#ifdef _WIN64
    //Ensure that we always allocate in pointer sized increments.
    /* TODO-Cleanup:
     * This is wasteful.  We should add alignment requirements to the allocations so we don't waste space in
     * the heap.
     */
    sz = (unsigned)roundUp(sz, sizeof(size_t));
#endif

#ifdef DEBUG
    if (nraShouldInjectFault)
    {
        // Force the underlying memory allocator (either the OS or the CLR hoster) 
        // to allocate the memory. Any fault injection will kick in.
        void * p = DbgNew(1); 
        if (p) 
        {
            DbgDelete(p);
        }
        else 
        {
            NOMEM();  // Throw!
        }
    }
#endif    

    block = nraFreeNext;
            nraFreeNext += sz;

    if  ((size_t)block == blockStop) debugStop("Block at %08X allocated", block);

    if  (nraFreeNext > nraFreeLast)
        block = nraAllocNewPage(sz);

#ifdef DEBUG
    memset(block, UninitializedWord<char>(), sz);
#endif

    return  block;
}

/*****************************************************************************/
#endif
/*****************************************************************************/

size_t              norls_allocator::nraTotalSizeAlloc()
{
    norls_pagdesc * page;
    size_t          size = 0;

    for (page = nraPageList; page; page = page->nrpNextPage)
        size += page->nrpPageSize;

    return  size;
}

size_t              norls_allocator::nraTotalSizeUsed()
{
    norls_pagdesc * page;
    size_t          size = 0;

    if  (nraPageLast)
        nraPageLast->nrpUsedSize = nraFreeNext - nraPageLast->nrpContents;

    for (page = nraPageList; page; page = page->nrpNextPage)
        size += page->nrpUsedSize;

    return  size;
}

/*****************************************************************************
 * We try to use this allocator instance as much as possible. It will always
 * keep a page handy so small methods won't have to call VirtualAlloc()
 * But we may not be able to use it if another thread/reentrant call
 * is already using it.
 */

static norls_allocator *nraTheAllocator;
static nraMarkDsc       nraTheAllocatorMark;
static LONG             nraTheAllocatorIsInUse = 0;

// The static instance which we try to reuse for all non-simultaneous requests

static norls_allocator  theAllocator;

/*****************************************************************************/

void                nraInitTheAllocator()
{
    THE_ALLOCATOR_BASE_SIZE = norls_allocator::nraDirectAlloc() ? 
        (size_t)norls_allocator::MIN_PAGE_SIZE : (size_t)norls_allocator::DEFAULT_PAGE_SIZE;
}

void                nraTheAllocatorDone()
{   
    // We chose not to call nraTheAllocator->nraFree() and let the memory leak.
    // Below is the reason (VSW 600919).

    // The following race-condition exists during ExitProcess.
    // Thread A calls ExitProcess, which causes thread B to terminate.
    // Thread B terminated in the middle of nraToss() 
    // (through the call-chain of nraFreeTheAllocator()  ==> nraRlsm() ==> nraToss())
    // And then thread A comes along to call nraTheAllocator->nraFree() which will cause the double-free 
    // of page specified by "temp".

    // These are possible fixes:
    // 1. Thread A tries to get hold on nraTheAllocatorIsInUse lock before
    //    calling theAllocator.nraFree(). However, this could cause the deadlock because thread B
    //    has already gone and therefore it can't release nraTheAllocatorIsInUse.
    // 2. Fix the logic in nraToss() and nraFree() to update nraPageList and nraPageLast in a thread safe way.
    //    But it needs careful work to make it high performant (e.g. not holding a lock?)
    // 3. The scenario of dynamically unloading clrjit.dll cleanly is unimportant at this time.
    //    We will leak the memory associated with other instances of morls_allocator anyway.
    
    // Therefore we decided not to call the cleanup code when unloading the jit. 
    
}

/*****************************************************************************/

norls_allocator *   nraGetTheAllocator(IEEMemoryManager* pMemoryManager)
{
    if (InterlockedExchange(&nraTheAllocatorIsInUse, 1))
    {
        // Its being used by another Compiler instance
        return NULL;
    }

    if (nraTheAllocator == NULL)
    {
        // Not initialized yet

        bool res = theAllocator.nraInit(pMemoryManager, 0, 1);

        if (res)
        {
            // failed to initialize
            InterlockedExchange(&nraTheAllocatorIsInUse, 0);            
            return NULL;
        }

        nraTheAllocator = &theAllocator;
        
        assert(nraTheAllocator->nraTotalSizeAlloc() == THE_ALLOCATOR_BASE_SIZE);
        nraTheAllocator->nraMark(nraTheAllocatorMark);    
    }
    else
    {
        if (nraTheAllocator->nraGetMemoryManager() != pMemoryManager)
        {
            // already initialize with a different memory manager
            InterlockedExchange(&nraTheAllocatorIsInUse, 0);            
            return NULL;
        }
    }

    assert(nraTheAllocator->nraTotalSizeAlloc() == THE_ALLOCATOR_BASE_SIZE);
    return nraTheAllocator;
}


void                nraFreeTheAllocator()
{
    assert (nraTheAllocator != NULL);
    assert(nraTheAllocatorIsInUse == 1);

    nraTheAllocator->nraRlsm(nraTheAllocatorMark);
    assert(nraTheAllocator->nraTotalSizeAlloc() == THE_ALLOCATOR_BASE_SIZE);

    InterlockedExchange(&nraTheAllocatorIsInUse, 0);
}

/*****************************************************************************/
