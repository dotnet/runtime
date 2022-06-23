// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"                     // Precompiled header key.
#include "loaderheap.h"
#include "ex.h"
#include "pedecoder.h"
#define DONOT_DEFINE_ETW_CALLBACK
#include "eventtracebase.h"

#ifndef DACCESS_COMPILE

INDEBUG(DWORD UnlockedLoaderHeap::s_dwNumInstancesOfLoaderHeaps = 0;)

#ifdef RANDOMIZE_ALLOC
#include <time.h>
static class Random
{
public:
    Random() { seed = (unsigned int)time(NULL); }
    unsigned int Next()
    {
        return ((seed = seed * 214013L + 2531011L) >> 16) & 0x7fff;
    }
private:
    unsigned int seed;
} s_random;
#endif

namespace
{
#if !defined(SELF_NO_HOST) // ETW available only in the runtime
    inline void EtwAllocRequest(UnlockedLoaderHeap * const pHeap, void* ptr, size_t dwSize)
    {
        FireEtwAllocRequest(pHeap, ptr, static_cast<unsigned int>(dwSize), 0, 0, GetClrInstanceId());
    }
#else
#define EtwAllocRequest(pHeap, ptr, dwSize) ((void)0)
#endif // SELF_NO_HOST
}

//
// RangeLists are constructed so they can be searched from multiple
// threads without locking.  They do require locking in order to
// be safely modified, though.
//

RangeList::RangeList()
{
    WRAPPER_NO_CONTRACT;

    InitBlock(&m_starterBlock);

    m_firstEmptyBlock = &m_starterBlock;
    m_firstEmptyRange = 0;
}

RangeList::~RangeList()
{
    LIMITED_METHOD_CONTRACT;

    RangeListBlock *b = m_starterBlock.next;

    while (b != NULL)
    {
        RangeListBlock *bNext = b->next;
        delete b;
        b = bNext;
    }
}

void RangeList::InitBlock(RangeListBlock *b)
{
    LIMITED_METHOD_CONTRACT;

    Range *r = b->ranges;
    Range *rEnd = r + RANGE_COUNT;
    while (r < rEnd)
        r++->id = NULL;

    b->next = NULL;
}

BOOL RangeList::AddRangeWorker(const BYTE *start, const BYTE *end, void *id)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END

    _ASSERTE(id != NULL);

    RangeListBlock *b = m_firstEmptyBlock;
    Range *r = b->ranges + m_firstEmptyRange;
    Range *rEnd = b->ranges + RANGE_COUNT;

    while (TRUE)
    {
        while (r < rEnd)
        {
            if (r->id == NULL)
            {
                r->start = (TADDR)start;
                r->end = (TADDR)end;
                r->id = (TADDR)id;

                r++;

                m_firstEmptyBlock = b;
                m_firstEmptyRange = r - b->ranges;

                return TRUE;
            }
            r++;
        }

        //
        // If there are no more blocks, allocate a
        // new one.
        //

        if (b->next == NULL)
        {
            RangeListBlock *newBlock = new (nothrow) RangeListBlock;

            if (newBlock == NULL)
            {
                m_firstEmptyBlock = b;
                m_firstEmptyRange = r - b->ranges;
                return FALSE;
            }

            InitBlock(newBlock);

            newBlock->next = NULL;
            b->next = newBlock;
        }

        //
        // Next block
        //

        b = b->next;
        r = b->ranges;
        rEnd = r + RANGE_COUNT;
    }
}

void RangeList::RemoveRangesWorker(void *id, const BYTE* start, const BYTE* end)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    RangeListBlock *b = &m_starterBlock;
    Range *r = b->ranges;
    Range *rEnd = r + RANGE_COUNT;

    //
    // Find the first free element, & mark it.
    //

    while (TRUE)
    {
        //
        // Clear entries in this block.
        //

        while (r < rEnd)
        {
            if (r->id != NULL)
            {
                if (start != NULL)
                {
                    _ASSERTE(end != NULL);

                    if (r->start >= (TADDR)start && r->start < (TADDR)end)
                    {
                        CONSISTENCY_CHECK_MSGF(r->end >= (TADDR)start &&
                                               r->end <= (TADDR)end,
                                               ("r: %p start: %p end: %p", r, start, end));
                        r->id = NULL;
                    }
                }
                else if (r->id == (TADDR)id)
                {
                    r->id = NULL;
                }
            }

            r++;
        }

        //
        // If there are no more blocks, we're done.
        //

        if (b->next == NULL)
        {
            m_firstEmptyRange = 0;
            m_firstEmptyBlock = &m_starterBlock;

            return;
        }

        //
        // Next block.
        //

        b = b->next;
        r = b->ranges;
        rEnd = r + RANGE_COUNT;
    }
}

#endif // #ifndef DACCESS_COMPILE

BOOL RangeList::IsInRangeWorker(TADDR address, TADDR *pID /* = NULL */)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        FORBID_FAULT;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    SUPPORTS_DAC;

    RangeListBlock* b = &m_starterBlock;
    Range* r = b->ranges;
    Range* rEnd = r + RANGE_COUNT;

    //
    // Look for a matching element
    //

    while (TRUE)
    {
        while (r < rEnd)
        {
            if (r->id != NULL &&
                address >= r->start
                && address < r->end)
            {
                if (pID != NULL)
                {
                    *pID = r->id;
                }
                return TRUE;
            }
            r++;
        }

        //
        // If there are no more blocks, we're done.
        //

        if (b->next == NULL)
            return FALSE;

        //
        // Next block.
        //

        b = b->next;
        r = b->ranges;
        rEnd = r + RANGE_COUNT;
    }
}

#ifdef DACCESS_COMPILE

void
RangeList::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;

    // This class is almost always contained in something
    // else so there's no enumeration of 'this'.

    RangeListBlock* block = &m_starterBlock;
    block->EnumMemoryRegions(flags);

    while (block->next.IsValid())
    {
        block->next.EnumMem();
        block = block->next;

        block->EnumMemoryRegions(flags);
    }
}

void
RangeList::RangeListBlock::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    Range*          range;
    TADDR           BADFOOD;
    TSIZE_T         size;
    int             i;

    // The code below iterates each range stored in the RangeListBlock and
    // dumps the memory region represented by each range.
    // It is too much memory for a mini-dump, so we just bail out for mini-dumps.
    if (flags == CLRDATA_ENUM_MEM_MINI || flags == CLRDATA_ENUM_MEM_TRIAGE)
    {
        return;
    }

    BIT64_ONLY( BADFOOD = 0xbaadf00dbaadf00d; );
    NOT_BIT64(  BADFOOD = 0xbaadf00d;         );

    for (i=0; i<RANGE_COUNT; i++)
    {
        range = &(this->ranges[i]);
        if (range->id == NULL || range->start == NULL || range->end == NULL ||
            // just looking at the lower 4bytes is good enough on WIN64
            range->start == BADFOOD || range->end == BADFOOD)
        {
            break;
        }

        size = range->end - range->start;
        _ASSERTE( size < UINT32_MAX );    // ranges should be less than 4gig!

        // We can't be sure this entire range is mapped.  For example, the code:StubLinkStubManager
        // keeps track of all ranges in the code:BaseDomain::m_pStubHeap LoaderHeap, and
        // code:LoaderHeap::UnlockedReservePages adds a range for the entire reserved region, instead
        // of updating the RangeList when pages are committed.  But in that case, the committed region of
        // memory will be enumerated by the LoaderHeap anyway, so it's OK if this fails
        DacEnumMemoryRegion(range->start, size, false);
    }
}

#endif // #ifdef DACCESS_COMPILE


//=====================================================================================
// In DEBUG builds only, we tag live blocks with the requested size and the type of
// allocation (AllocMem, AllocAlignedMem, AllocateOntoReservedMem). This is strictly
// to validate that those who call Backout* are passing in the right values.
//
// For simplicity, we'll use one LoaderHeapValidationTag structure for all types even
// though not all fields are applicable to all types.
//=====================================================================================
#ifdef _DEBUG
enum AllocationType
{
    kAllocMem = 1,
    kFreedMem = 4,
};

struct LoaderHeapValidationTag
{
    size_t         m_dwRequestedSize;      // What the caller requested (not what was actually allocated)
    AllocationType m_allocationType;       // Which api allocated this block.
    const char *   m_szFile;               // Who allocated me
    int            m_lineNum;              // Who allocated me

};
#endif //_DEBUG





//=====================================================================================
// These classes do detailed loaderheap sniffing to help in debugging heap crashes
//=====================================================================================
#ifdef _DEBUG

// This structure logs the results of an Alloc or Free call. They are stored in reverse time order
// with UnlockedLoaderHeap::m_pEventList pointing to the most recent event.
struct LoaderHeapEvent
{
    LoaderHeapEvent *m_pNext;
    AllocationType   m_allocationType;      //Which api was called
    const char      *m_szFile;              //Caller Id
    int              m_lineNum;             //Caller Id
    const char      *m_szAllocFile;         //(BackoutEvents): Who allocated the block?
    int              m_allocLineNum;        //(BackoutEvents): Who allocated the block?
    void            *m_pMem;                //Starting address of block
    size_t           m_dwRequestedSize;     //Requested size of block
    size_t           m_dwSize;              //Actual size of block (including validation tags, padding, everything)


    void Describe(SString *pSString)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            DISABLED(NOTHROW);
            GC_NOTRIGGER;
        }
        CONTRACTL_END

        pSString->AppendASCII("\n");

        {
            StackSString buf;
            if (m_allocationType == kFreedMem)
            {
                buf.Printf("    Freed at:         %s (line %d)\n", m_szFile, m_lineNum);
                buf.Printf("       (block originally allocated at %s (line %d)\n", m_szAllocFile, m_allocLineNum);
            }
            else
            {
                buf.Printf("    Allocated at:     %s (line %d)\n", m_szFile, m_lineNum);
            }
            pSString->Append(buf);
        }

        if (!QuietValidate())
        {
            pSString->AppendASCII("    *** THIS BLOCK HAS BEEN CORRUPTED ***\n");
        }



        {
            StackSString buf;
            buf.Printf("    Type:          ");
            switch (m_allocationType)
            {
                case kAllocMem:
                    buf.AppendASCII("AllocMem()\n");
                    break;
                case kFreedMem:
                    buf.AppendASCII("Free\n");
                    break;
                default:
                    break;
            }
            pSString->Append(buf);
        }


        {
            StackSString buf;
            buf.Printf("    Start of block:       0x%p\n", m_pMem);
            pSString->Append(buf);
        }

        {
            StackSString buf;
            buf.Printf("    End of block:         0x%p\n", ((BYTE*)m_pMem) + m_dwSize - 1);
            pSString->Append(buf);
        }

        {
            StackSString buf;
            buf.Printf("    Requested size:       %lu (0x%lx)\n", (ULONG)m_dwRequestedSize, (ULONG)m_dwRequestedSize);
            pSString->Append(buf);
        }

        {
            StackSString buf;
            buf.Printf("    Actual size:          %lu (0x%lx)\n", (ULONG)m_dwSize, (ULONG)m_dwSize);
            pSString->Append(buf);
        }

        pSString->AppendASCII("\n");
    }



    BOOL QuietValidate();

};


class LoaderHeapSniffer
{
    public:
        static DWORD InitDebugFlags()
        {
            WRAPPER_NO_CONTRACT;

            DWORD dwDebugFlags = 0;
            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_LoaderHeapCallTracing))
            {
                dwDebugFlags |= UnlockedLoaderHeap::kCallTracing;
            }
            return dwDebugFlags;
        }


        static VOID RecordEvent(UnlockedLoaderHeap *pHeap,
                                AllocationType allocationType,
                                _In_ const char *szFile,
                                int            lineNum,
                                _In_ const char *szAllocFile,
                                int            allocLineNum,
                                void          *pMem,
                                size_t         dwRequestedSize,
                                size_t         dwSize
                                );

        static VOID ClearEvents(UnlockedLoaderHeap *pHeap)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_FORBID_FAULT;

            LoaderHeapEvent *pEvent = pHeap->m_pEventList;
            while (pEvent)
            {
                LoaderHeapEvent *pNext = pEvent->m_pNext;
                delete pEvent;
                pEvent = pNext;
            }
            pHeap->m_pEventList = NULL;
        }


        static VOID CompactEvents(UnlockedLoaderHeap *pHeap)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_FORBID_FAULT;

            LoaderHeapEvent **ppEvent = &(pHeap->m_pEventList);
            while (*ppEvent)
            {
                LoaderHeapEvent *pEvent = *ppEvent;
                if (pEvent->m_allocationType != kFreedMem)
                {
                    ppEvent = &(pEvent->m_pNext);
                }
                else
                {
                    LoaderHeapEvent **ppWalk = &(pEvent->m_pNext);
                    BOOL fMatchFound = FALSE;
                    while (*ppWalk && !fMatchFound)
                    {
                        LoaderHeapEvent *pWalk = *ppWalk;
                        if (pWalk->m_allocationType  != kFreedMem &&
                            pWalk->m_pMem            == pEvent->m_pMem &&
                            pWalk->m_dwRequestedSize == pEvent->m_dwRequestedSize)
                        {
                            // Delete matched pairs

                            // Order is important here - updating *ppWalk may change pEvent->m_pNext, and we want
                            // to get the updated value when we unlink pEvent.
                            *ppWalk = pWalk->m_pNext;
                            *ppEvent = pEvent->m_pNext;

                            delete pEvent;
                            delete pWalk;
                            fMatchFound = TRUE;
                        }
                        else
                        {
                            ppWalk = &(pWalk->m_pNext);
                        }
                    }

                    if (!fMatchFound)
                    {
                        ppEvent = &(pEvent->m_pNext);
                    }
                }
            }
        }
        static VOID PrintEvents(UnlockedLoaderHeap *pHeap)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_FORBID_FAULT;

            printf("\n------------- LoaderHeapEvents (in reverse time order!) --------------------");

            LoaderHeapEvent *pEvent = pHeap->m_pEventList;
            while (pEvent)
            {
                printf("\n");
                switch (pEvent->m_allocationType)
                {
                    case kAllocMem:         printf("AllocMem        "); break;
                    case kFreedMem:         printf("BackoutMem      "); break;

                }
                printf(" ptr = 0x%-8p", pEvent->m_pMem);
                printf(" rqsize = 0x%-8x", (DWORD)pEvent->m_dwRequestedSize);
                printf(" actsize = 0x%-8x", (DWORD)pEvent->m_dwSize);
                printf(" (at %s@%d)", pEvent->m_szFile, pEvent->m_lineNum);
                if (pEvent->m_allocationType == kFreedMem)
                {
                    printf(" (original allocation at %s@%d)", pEvent->m_szAllocFile, pEvent->m_allocLineNum);
                }

                pEvent = pEvent->m_pNext;

            }
            printf("\n------------- End of LoaderHeapEvents --------------------------------------");
            printf("\n");

        }


        static VOID PitchSniffer(SString *pSString)
        {
            WRAPPER_NO_CONTRACT;
            pSString->AppendASCII("\n"
                             "\nBecause call-tracing wasn't turned on, we couldn't provide details about who last owned the affected memory block. To get more precise diagnostics,"
                             "\nset the following registry DWORD value:"
                             "\n"
                             "\n    HKLM\\Software\\Microsoft\\.NETFramework\\LoaderHeapCallTracing = 1"
                             "\n"
                             "\nand rerun the scenario that crashed."
                             "\n"
                             "\n");
        }

        static LoaderHeapEvent *FindEvent(UnlockedLoaderHeap *pHeap, void *pAddr)
        {
            LIMITED_METHOD_CONTRACT;

            LoaderHeapEvent *pEvent = pHeap->m_pEventList;
            while (pEvent)
            {
                if (pAddr >= pEvent->m_pMem && pAddr <= ( ((BYTE*)pEvent->m_pMem) + pEvent->m_dwSize - 1))
                {
                    return pEvent;
                }
                pEvent = pEvent->m_pNext;
            }
            return NULL;

        }


        static void ValidateFreeList(UnlockedLoaderHeap *pHeap);

        static void WeGotAFaultNowWhat(UnlockedLoaderHeap *pHeap)
        {
            WRAPPER_NO_CONTRACT;
            ValidateFreeList(pHeap);

            //If none of the above popped up an assert, pop up a generic one.
            _ASSERTE(!("Unexpected AV inside LoaderHeap. The usual reason is that someone overwrote the end of a block or wrote into a freed block.\n"));

        }

};


#endif


#ifdef _DEBUG
#define LOADER_HEAP_BEGIN_TRAP_FAULT BOOL __faulted = FALSE; EX_TRY {
#define LOADER_HEAP_END_TRAP_FAULT   } EX_CATCH {__faulted = TRUE; } EX_END_CATCH(SwallowAllExceptions) if (__faulted) LoaderHeapSniffer::WeGotAFaultNowWhat(pHeap);
#else
#define LOADER_HEAP_BEGIN_TRAP_FAULT
#define LOADER_HEAP_END_TRAP_FAULT
#endif


size_t AllocMem_TotalSize(size_t dwRequestedSize, UnlockedLoaderHeap *pHeap);

//=====================================================================================
// This freelist implementation is a first cut and probably needs to be tuned.
// It should be tuned with the following assumptions:
//
//    - Freeing LoaderHeap memory is done primarily for OOM backout. LoaderHeaps
//      weren't designed to be general purpose heaps and shouldn't be used that way.
//
//    - And hence, when memory is freed, expect it to be freed in large clumps and in a
//      LIFO order. Since the LoaderHeap normally hands out memory with sequentially
//      increasing addresses, blocks will typically be freed with sequentially decreasing
//      addresses.
//
// The first cut of the freelist is a single-linked list of free blocks using first-fit.
// Assuming the above alloc-free pattern holds, the list will end up mostly sorted
// in increasing address order. When a block is freed, we'll attempt to coalesce it
// with the first block in the list. We could also choose to be more aggressive about
// sorting and coalescing but this should probably catch most cases in practice.
//=====================================================================================

// When a block is freed, we place this structure on the first bytes of the freed block (Allocations
// are bumped in size if necessary to make sure there's room.)
struct LoaderHeapFreeBlock
{
    public:
        LoaderHeapFreeBlock   *m_pNext;         // Pointer to next block on free list
        size_t                 m_dwSize;        // Total size of this block
        void                  *m_pBlockAddress; // Virtual address of the block

#ifndef DACCESS_COMPILE
        static void InsertFreeBlock(LoaderHeapFreeBlock **ppHead, void *pMem, size_t dwTotalSize, UnlockedLoaderHeap *pHeap)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_GC_NOTRIGGER;

            // The new "nothrow" below failure is handled in a non-fault way, so
            // make sure that callers with FORBID_FAULT can call this method without
            // firing the contract violation assert.
            PERMANENT_CONTRACT_VIOLATION(FaultViolation, ReasonContractInfrastructure);

            LOADER_HEAP_BEGIN_TRAP_FAULT

            // It's illegal to insert a free block that's smaller than the minimum sized allocation -
            // it may stay stranded on the freelist forever.
#ifdef _DEBUG
            if (!(dwTotalSize >= AllocMem_TotalSize(1, pHeap)))
            {
                LoaderHeapSniffer::ValidateFreeList(pHeap);
                _ASSERTE(dwTotalSize >= AllocMem_TotalSize(1, pHeap));
            }

            if (!(0 == (dwTotalSize & ALLOC_ALIGN_CONSTANT)))
            {
                LoaderHeapSniffer::ValidateFreeList(pHeap);
                _ASSERTE(0 == (dwTotalSize & ALLOC_ALIGN_CONSTANT));
            }
#endif

#ifdef DEBUG
            if (!pHeap->IsInterleaved())
            {
                void* pMemRW = pMem;
                ExecutableWriterHolderNoLog<void> memWriterHolder;
                if (pHeap->IsExecutable())
                {
                    memWriterHolder.AssignExecutableWriterHolder(pMem, dwTotalSize);
                    pMemRW = memWriterHolder.GetRW();
                }

                memset(pMemRW, 0xcc, dwTotalSize);
            }
            else
            {
                memset((BYTE*)pMem + GetOsPageSize(), 0xcc, dwTotalSize);
            }
#endif // DEBUG            

            LoaderHeapFreeBlock *pNewBlock = new (nothrow) LoaderHeapFreeBlock;
            // If we fail allocating the LoaderHeapFreeBlock, ignore the failure and don't insert the free block at all.
            if (pNewBlock != NULL)
            {
                pNewBlock->m_pNext  = *ppHead;
                pNewBlock->m_dwSize = dwTotalSize;
                pNewBlock->m_pBlockAddress = pMem;
                *ppHead = pNewBlock;
                MergeBlock(pNewBlock, pHeap);
            }

            LOADER_HEAP_END_TRAP_FAULT
        }

        static void *AllocFromFreeList(LoaderHeapFreeBlock **ppHead, size_t dwSize, UnlockedLoaderHeap *pHeap)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_GC_NOTRIGGER;

            INCONTRACT(_ASSERTE_IMPL(!ARE_FAULTS_FORBIDDEN()));

            void *pResult = NULL;
            LOADER_HEAP_BEGIN_TRAP_FAULT

            LoaderHeapFreeBlock **ppWalk = ppHead;
            while (*ppWalk)
            {
                LoaderHeapFreeBlock *pCur = *ppWalk;
                size_t dwCurSize = pCur->m_dwSize;
                if (dwCurSize == dwSize)
                {
                    pResult = pCur->m_pBlockAddress;
                    // Exact match. Hooray!
                    *ppWalk = pCur->m_pNext;
                    delete pCur;
                    break;
                }
                else if (dwCurSize > dwSize && (dwCurSize - dwSize) >= AllocMem_TotalSize(1, pHeap))
                {
                    // Partial match. Ok...
                    pResult = pCur->m_pBlockAddress;
                    *ppWalk = pCur->m_pNext;
                    InsertFreeBlock(ppWalk, ((BYTE*)pCur->m_pBlockAddress) + dwSize, dwCurSize - dwSize, pHeap );
                    delete pCur;
                    break;
                }

                // Either block is too small or splitting the block would leave a remainder that's smaller than
                // the minimum block size. Onto next one.

                ppWalk = &( pCur->m_pNext );
            }

            if (pResult)
            {
                void *pResultRW = pResult;
                ExecutableWriterHolderNoLog<void> resultWriterHolder;
                if (pHeap->IsExecutable())
                {
                    resultWriterHolder.AssignExecutableWriterHolder(pResult, dwSize);
                    pResultRW = resultWriterHolder.GetRW();
                }
                // Callers of loaderheap assume allocated memory is zero-inited so we must preserve this invariant!
                memset(pResultRW, 0, dwSize);
            }
            LOADER_HEAP_END_TRAP_FAULT
            return pResult;
        }

    private:
        // Try to merge pFreeBlock with its immediate successor. Return TRUE if a merge happened. FALSE if no merge happened.
        static BOOL MergeBlock(LoaderHeapFreeBlock *pFreeBlock, UnlockedLoaderHeap *pHeap)
        {
            STATIC_CONTRACT_NOTHROW;

            BOOL result = FALSE;

            LOADER_HEAP_BEGIN_TRAP_FAULT

            LoaderHeapFreeBlock *pNextBlock = pFreeBlock->m_pNext;
            size_t               dwSize     = pFreeBlock->m_dwSize;

            if (pNextBlock == NULL || ((BYTE*)pNextBlock->m_pBlockAddress) != (((BYTE*)pFreeBlock->m_pBlockAddress) + dwSize))
            {
                result = FALSE;
            }
            else
            {
                size_t dwCombinedSize = dwSize + pNextBlock->m_dwSize;
                LoaderHeapFreeBlock *pNextNextBlock = pNextBlock->m_pNext;
                void *pMemRW = pFreeBlock->m_pBlockAddress;
                ExecutableWriterHolderNoLog<void> memWriterHolder;
                if (pHeap->IsExecutable())
                {
                    memWriterHolder.AssignExecutableWriterHolder(pFreeBlock->m_pBlockAddress, dwCombinedSize);
                    pMemRW = memWriterHolder.GetRW();
                }
                INDEBUG(memset(pMemRW, 0xcc, dwCombinedSize);)
                pFreeBlock->m_pNext  = pNextNextBlock;
                pFreeBlock->m_dwSize = dwCombinedSize;
                delete pNextBlock;

                result = TRUE;
            }

            LOADER_HEAP_END_TRAP_FAULT
            return result;

        }
#endif // DACCESS_COMPILE
};




//=====================================================================================
// These helpers encapsulate the actual layout of a block allocated by AllocMem
// and UnlockedAllocMem():
//
// ==> Starting address is always pointer-aligned.
//
//   - x  bytes of user bytes        (where "x" is the actual dwSize passed into AllocMem)
//
//   - y  bytes of "EE" (DEBUG-ONLY) (where "y" == LOADER_HEAP_DEBUG_BOUNDARY (normally 0))
//   - z  bytes of pad  (DEBUG-ONLY) (where "z" is just enough to pointer-align the following byte)
//   - a  bytes of tag  (DEBUG-ONLY) (where "a" is sizeof(LoaderHeapValidationTag)
//
//   - b  bytes of pad               (where "b" is just enough to pointer-align the following byte)
//
// ==> Following address is always pointer-aligned
//=====================================================================================

// Convert the requested size into the total # of bytes we'll actually allocate (including padding)
inline size_t AllocMem_TotalSize(size_t dwRequestedSize, UnlockedLoaderHeap *pHeap)
{
    LIMITED_METHOD_CONTRACT;

    size_t dwSize = dwRequestedSize;

    // Interleaved heap cannot ad any extra to the requested size
    if (!pHeap->IsInterleaved())
    {
#ifdef _DEBUG
        dwSize += LOADER_HEAP_DEBUG_BOUNDARY;
        dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
#endif

        if (!pHeap->m_fExplicitControl)
        {
#ifdef _DEBUG
            dwSize += sizeof(LoaderHeapValidationTag);
#endif
        }
        dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
    }

    return dwSize;
}


#ifdef _DEBUG
LoaderHeapValidationTag *AllocMem_GetTag(LPVOID pBlock, size_t dwRequestedSize)
{
    LIMITED_METHOD_CONTRACT;

    size_t dwSize = dwRequestedSize;
    dwSize += LOADER_HEAP_DEBUG_BOUNDARY;
    dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
    return (LoaderHeapValidationTag *)( ((BYTE*)pBlock) + dwSize );
}
#endif





//=====================================================================================
// UnlockedLoaderHeap methods
//=====================================================================================

#ifndef DACCESS_COMPILE

UnlockedLoaderHeap::UnlockedLoaderHeap(DWORD dwReserveBlockSize,
                                       DWORD dwCommitBlockSize,
                                       const BYTE* dwReservedRegionAddress,
                                       SIZE_T dwReservedRegionSize,
                                       RangeList *pRangeList,
                                       HeapKind kind,
                                       void (*codePageGenerator)(BYTE* pageBase, BYTE* pageBaseRX),
                                       DWORD dwGranularity)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_pFirstBlock                = NULL;

    m_dwReserveBlockSize         = dwReserveBlockSize;
    m_dwCommitBlockSize          = dwCommitBlockSize;

    m_pPtrToEndOfCommittedRegion = NULL;
    m_pEndReservedRegion         = NULL;
    m_pAllocPtr                  = NULL;

    m_pRangeList                 = pRangeList;

    // Round to VIRTUAL_ALLOC_RESERVE_GRANULARITY
    m_dwTotalAlloc               = 0;

    m_dwGranularity = dwGranularity;

#ifdef _DEBUG
    m_dwDebugWastedBytes         = 0;
    s_dwNumInstancesOfLoaderHeaps++;
    m_pEventList                 = NULL;
    m_dwDebugFlags               = LoaderHeapSniffer::InitDebugFlags();
    m_fPermitStubsWithUnwindInfo = FALSE;
    m_fStubUnwindInfoUnregistered= FALSE;
#endif

    m_kind = kind;

    _ASSERTE((kind != HeapKind::Interleaved) || (codePageGenerator != NULL));
    m_codePageGenerator = codePageGenerator;

    m_pFirstFreeBlock            = NULL;

    if (dwReservedRegionAddress != NULL && dwReservedRegionSize > 0)
    {
        m_reservedBlock.Init((void *)dwReservedRegionAddress, dwReservedRegionSize, FALSE);
    }
}

// ~LoaderHeap is not synchronised (obviously)
UnlockedLoaderHeap::~UnlockedLoaderHeap()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE(!m_fPermitStubsWithUnwindInfo || m_fStubUnwindInfoUnregistered);

    if (m_pRangeList != NULL)
        m_pRangeList->RemoveRanges((void *) this);

    LoaderHeapBlock *pSearch, *pNext;

    for (pSearch = m_pFirstBlock; pSearch; pSearch = pNext)
    {
        void *  pVirtualAddress;
        BOOL    fReleaseMemory;

        pVirtualAddress = pSearch->pVirtualAddress;
        fReleaseMemory = pSearch->m_fReleaseMemory;
        pNext = pSearch->pNext;

        if (fReleaseMemory)
        {
            ExecutableAllocator::Instance()->Release(pVirtualAddress);
        }

        delete pSearch;
    }

    if (m_reservedBlock.m_fReleaseMemory)
    {
        ExecutableAllocator::Instance()->Release(m_reservedBlock.pVirtualAddress);
    }

    INDEBUG(s_dwNumInstancesOfLoaderHeaps --;)
}

void UnlockedLoaderHeap::UnlockedSetReservedRegion(BYTE* dwReservedRegionAddress, SIZE_T dwReservedRegionSize, BOOL fReleaseMemory)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_reservedBlock.pVirtualAddress == NULL);
    m_reservedBlock.Init((void *)dwReservedRegionAddress, dwReservedRegionSize, fReleaseMemory);
}

#endif // #ifndef DACCESS_COMPILE

#if 0
// Disables access to all pages in the heap - useful when trying to determine if someone is
// accessing something in the low frequency heap
void UnlockedLoaderHeap::DebugGuardHeap()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapBlock *pSearch, *pNext;

    for (pSearch = m_pFirstBlock; pSearch; pSearch = pNext)
    {
        void *  pResult;
        void *  pVirtualAddress;

        pVirtualAddress = pSearch->pVirtualAddress;
        pNext = pSearch->pNext;

        pResult = ClrVirtualAlloc(pVirtualAddress, pSearch->dwVirtualSize, MEM_COMMIT, PAGE_NOACCESS);
        _ASSERTE(pResult != NULL);
    }
}
#endif

size_t UnlockedLoaderHeap::GetBytesAvailCommittedRegion()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pAllocPtr < m_pPtrToEndOfCommittedRegion)
        return (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);
    else
        return 0;
}

size_t UnlockedLoaderHeap::GetBytesAvailReservedRegion()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pAllocPtr < m_pEndReservedRegion)
        return (size_t)(m_pEndReservedRegion- m_pAllocPtr);
    else
        return 0;
}

#ifndef DACCESS_COMPILE

void ReleaseReservedMemory(BYTE* value)
{
    if (value)
    {
        ExecutableAllocator::Instance()->Release(value);
    }
}

using ReservedMemoryHolder = SpecializedWrapper<BYTE, ReleaseReservedMemory>;

BOOL UnlockedLoaderHeap::UnlockedReservePages(size_t dwSizeToCommit)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END;

    size_t dwSizeToReserve;

    // Round to page size again
    dwSizeToCommit = ALIGN_UP(dwSizeToCommit, GetOsPageSize());

    ReservedMemoryHolder pData = NULL;
    BOOL fReleaseMemory = TRUE;

    // We were provided with a reserved memory block at instance creation time, so use it if it's big enough.
    if (m_reservedBlock.pVirtualAddress != NULL &&
        m_reservedBlock.dwVirtualSize >= dwSizeToCommit)
    {
        // Get the info out of the block.
        pData = (PTR_BYTE)m_reservedBlock.pVirtualAddress;
        dwSizeToReserve = m_reservedBlock.dwVirtualSize;
        fReleaseMemory = m_reservedBlock.m_fReleaseMemory;

        // Zero the block so this memory doesn't get used again.
        m_reservedBlock.Init(NULL, 0, FALSE);
    }
    // The caller is asking us to allocate the memory
    else
    {
        if (m_fExplicitControl)
        {
            return FALSE;
        }

        // Figure out how much to reserve
        dwSizeToReserve = max(dwSizeToCommit, m_dwReserveBlockSize);

        // Round to VIRTUAL_ALLOC_RESERVE_GRANULARITY
        dwSizeToReserve = ALIGN_UP(dwSizeToReserve, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

        _ASSERTE(dwSizeToCommit <= dwSizeToReserve);

        //
        // Reserve pages
        //

        // Reserve the memory for even non-executable stuff close to the executable code, as it has profound effect
        // on e.g. a static variable access performance.
        pData = (BYTE *)ExecutableAllocator::Instance()->Reserve(dwSizeToReserve);
        if (pData == NULL)
        {
            _ASSERTE(!"Unable to reserve memory range for a loaderheap");
            return FALSE;
        }
    }

    // When the user passes in the reserved memory, the commit size is 0 and is adjusted to be the sizeof(LoaderHeap).
    // If for some reason this is not true then we just catch this via an assertion and the dev who changed code
    // would have to add logic here to handle the case when committed mem is more than the reserved mem. One option
    // could be to leak the users memory and reserve+commit a new block, Another option would be to fail the alloc mem
    // and notify the user to provide more reserved mem.
    _ASSERTE((dwSizeToCommit <= dwSizeToReserve) && "Loaderheap tried to commit more memory than reserved by user");

    if (!fReleaseMemory)
    {
        pData.SuppressRelease();
    }

    size_t dwSizeToCommitPart = dwSizeToCommit;
    if (IsInterleaved())
    {
        // For interleaved heaps, we perform two commits, each being half of the requested size
        dwSizeToCommitPart /= 2;
    }

    // Commit first set of pages, since it will contain the LoaderHeapBlock
    void *pTemp = ExecutableAllocator::Instance()->Commit(pData, dwSizeToCommitPart, IsExecutable());
    if (pTemp == NULL)
    {
        _ASSERTE(!"Unable to commit a loaderheap code page");

        return FALSE;
    }

    if (IsInterleaved())
    {
        _ASSERTE(dwSizeToCommitPart == GetOsPageSize());

        void *pTemp = ExecutableAllocator::Instance()->Commit((BYTE*)pData + dwSizeToCommitPart, dwSizeToCommitPart, FALSE);
        if (pTemp == NULL)
        {
            _ASSERTE(!"Unable to commit a loaderheap data page");

            return FALSE;
        }

        ExecutableWriterHolder<BYTE> codePageWriterHolder(pData, GetOsPageSize());
        m_codePageGenerator(codePageWriterHolder.GetRW(), pData);
        FlushInstructionCache(GetCurrentProcess(), pData, GetOsPageSize());
    }

    // Record reserved range in range list, if one is specified
    // Do this AFTER the commit - otherwise we'll have bogus ranges included.
    if (m_pRangeList != NULL)
    {
        if (!m_pRangeList->AddRange((const BYTE *) pData,
                                    ((const BYTE *) pData) + dwSizeToReserve,
                                    (void *) this))
        {
            return FALSE;
        }
    }

    LoaderHeapBlock *pNewBlock = new (nothrow) LoaderHeapBlock;
    if (pNewBlock == NULL)
    {
        return FALSE;
    }

    m_dwTotalAlloc += dwSizeToCommit;

    pData.SuppressRelease();

    pNewBlock->dwVirtualSize    = dwSizeToReserve;
    pNewBlock->pVirtualAddress  = pData;
    pNewBlock->pNext            = m_pFirstBlock;
    pNewBlock->m_fReleaseMemory = fReleaseMemory;

    // Add to the linked list
    m_pFirstBlock = pNewBlock;

    if (IsInterleaved())
    {
        dwSizeToCommit /= 2;
    }

    m_pPtrToEndOfCommittedRegion = (BYTE *) (pData) + (dwSizeToCommit);         \
    m_pAllocPtr                  = (BYTE *) (pData);                            \
    m_pEndReservedRegion         = (BYTE *) (pData) + (dwSizeToReserve);

    return TRUE;
}

// Get some more committed pages - either commit some more in the current reserved region, or, if it
// has run out, reserve another set of pages.
// Returns: FALSE if we can't get any more memory
// TRUE: We can/did get some more memory - check to see if it's sufficient for
//       the caller's needs (see UnlockedAllocMem for example of use)
BOOL UnlockedLoaderHeap::GetMoreCommittedPages(size_t dwMinSize)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END;

    // If we have memory we can use, what are you doing here!
    _ASSERTE(dwMinSize > (SIZE_T)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr));

    if (IsInterleaved())
    {
        // This mode interleaves data and code pages 1:1. So the code size is required to be smaller than
        // or equal to the page size to ensure that the code range is consecutive.
        _ASSERTE(dwMinSize <= GetOsPageSize());
        // For interleaved heap, we always get two memory pages - one for code and one for data
        dwMinSize = 2 * GetOsPageSize();
    }

    // Does this fit in the reserved region?
    if (dwMinSize <= (size_t)(m_pEndReservedRegion - m_pAllocPtr))
    {
        SIZE_T dwSizeToCommit;

        if (IsInterleaved())
        {
            // For interleaved heaps, the allocation cannot cross page boundary since there are data and executable
            // pages interleaved in a 1:1 fashion.
            dwSizeToCommit = dwMinSize;
        }
        else
        {
            dwSizeToCommit = (m_pAllocPtr + dwMinSize) - m_pPtrToEndOfCommittedRegion;
        }

        size_t unusedRemainder = (size_t)((BYTE*)m_pPtrToEndOfCommittedRegion - m_pAllocPtr);

        if (IsInterleaved())
        {
            // The end of commited region for interleaved heaps points to the end of the executable
            // page and the data pages goes right after that. So we skip the data page here.
            m_pPtrToEndOfCommittedRegion += GetOsPageSize();
        }
        else
        {
            if (dwSizeToCommit < m_dwCommitBlockSize)
                dwSizeToCommit = min((SIZE_T)(m_pEndReservedRegion - m_pPtrToEndOfCommittedRegion), (SIZE_T)m_dwCommitBlockSize);

            // Round to page size
            dwSizeToCommit = ALIGN_UP(dwSizeToCommit, GetOsPageSize());
        }

        size_t dwSizeToCommitPart = dwSizeToCommit;
        if (IsInterleaved())
        {
            // For interleaved heaps, we perform two commits, each being half of the requested size
            dwSizeToCommitPart /= 2;
        }

        // Yes, so commit the desired number of reserved pages
        void *pData = ExecutableAllocator::Instance()->Commit(m_pPtrToEndOfCommittedRegion, dwSizeToCommitPart, IsExecutable());
        if (pData == NULL)
        {
            _ASSERTE(!"Unable to commit a loaderheap page");
            return FALSE;
        }

        if (IsInterleaved())
        {
            // Commit a data page after the code page
            ExecutableAllocator::Instance()->Commit(m_pPtrToEndOfCommittedRegion + dwSizeToCommitPart, dwSizeToCommitPart, FALSE);

            ExecutableWriterHolder<BYTE> codePageWriterHolder((BYTE*)pData, GetOsPageSize());
            m_codePageGenerator(codePageWriterHolder.GetRW(), (BYTE*)pData);
            FlushInstructionCache(GetCurrentProcess(), pData, GetOsPageSize());

            // If the remaning bytes are large enough to allocate data of the allocation granularity, add them to the free
            // block list.
            // Otherwise the remaining bytes that are available will be wasted.
            if (unusedRemainder >= m_dwGranularity)
            {
                LoaderHeapFreeBlock::InsertFreeBlock(&m_pFirstFreeBlock, m_pAllocPtr, unusedRemainder, this);
            }
            else
            {
                INDEBUG(m_dwDebugWastedBytes += unusedRemainder;)
            }

            // For interleaved heaps, further allocations will start from the newly committed page as they cannot
            // cross page boundary.
            m_pAllocPtr = (BYTE*)pData;
        }

        m_pPtrToEndOfCommittedRegion += dwSizeToCommitPart;
        m_dwTotalAlloc += dwSizeToCommit;

        return TRUE;
    }

    // Need to allocate a new set of reserved pages that will be located likely at a nonconsecutive virtual address.
    // If the remaning bytes are large enough to allocate data of the allocation granularity, add them to the free
    // block list.
    // Otherwise the remaining bytes that are available will be wasted.
    size_t unusedRemainder = (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);
    if (unusedRemainder >= AllocMem_TotalSize(m_dwGranularity, this))
    {
        LoaderHeapFreeBlock::InsertFreeBlock(&m_pFirstFreeBlock, m_pAllocPtr, unusedRemainder, this);
    }
    else
    {
        INDEBUG(m_dwDebugWastedBytes += (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);)
    }

    // Note, there are unused reserved pages at end of current region -can't do much about that
    // Provide dwMinSize here since UnlockedReservePages will round up the commit size again
    // after adding in the size of the LoaderHeapBlock header.
    return UnlockedReservePages(dwMinSize);
}

void *UnlockedLoaderHeap::UnlockedAllocMem(size_t dwSize
                                           COMMA_INDEBUG(_In_ const char *szFile)
                                           COMMA_INDEBUG(int  lineNum))
{
    CONTRACT(void*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(ThrowOutOfMemory(););
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    void *pResult = UnlockedAllocMem_NoThrow(
        dwSize COMMA_INDEBUG(szFile) COMMA_INDEBUG(lineNum));

    if (pResult == NULL)
        ThrowOutOfMemory();

    RETURN pResult;
}

#ifdef _DEBUG
static DWORD ShouldInjectFault()
{
    static DWORD fInjectFault = 99;

    if (fInjectFault == 99)
        fInjectFault = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InjectFault) != 0);
    return fInjectFault;
}

#define SHOULD_INJECT_FAULT(return_statement)   \
    do {                                        \
        if (ShouldInjectFault() & 0x1)          \
        {                                       \
            char *a = new (nothrow) char;       \
            if (a == NULL)                      \
            {                                   \
                return_statement;               \
            }                                   \
            delete a;                           \
        }                                       \
    } while (FALSE)

#else

#define SHOULD_INJECT_FAULT(return_statement) do { (void)((void *)0); } while (FALSE)

#endif

void *UnlockedLoaderHeap::UnlockedAllocMem_NoThrow(size_t dwSize
                                                   COMMA_INDEBUG(_In_ const char *szFile)
                                                   COMMA_INDEBUG(int lineNum))
{
    CONTRACT(void*)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(CONTRACT_RETURN NULL;);
        PRECONDITION(dwSize != 0);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    SHOULD_INJECT_FAULT(RETURN NULL);

    INDEBUG(size_t dwRequestedSize = dwSize;)

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

#ifdef RANDOMIZE_ALLOC
    if (!m_fExplicitControl && !IsInterleaved())
        dwSize += s_random.Next() % 256;
#endif

    dwSize = AllocMem_TotalSize(dwSize, this);

again:

    {
        // Any memory available on the free list?
        void *pData = LoaderHeapFreeBlock::AllocFromFreeList(&m_pFirstFreeBlock, dwSize, this);
        if (!pData)
        {
            // Enough bytes available in committed region?
            if (dwSize <= GetBytesAvailCommittedRegion())
            {
                pData = m_pAllocPtr;
                m_pAllocPtr += dwSize;
            }
        }

        if (pData)
        {
#ifdef _DEBUG
            BYTE *pAllocatedBytes = (BYTE*)pData;
            ExecutableWriterHolderNoLog<void> dataWriterHolder;
            if (IsExecutable())
            {
                dataWriterHolder.AssignExecutableWriterHolder(pData, dwSize);
                pAllocatedBytes = (BYTE *)dataWriterHolder.GetRW();
            }

#if LOADER_HEAP_DEBUG_BOUNDARY > 0
            // Don't fill the memory we allocated - it is assumed to be zeroed - fill the memory after it
            memset(pAllocatedBytes + dwRequestedSize, 0xEE, LOADER_HEAP_DEBUG_BOUNDARY);
#endif
            if (dwRequestedSize > 0)
            {
                _ASSERTE_MSG(pAllocatedBytes[0] == 0 && memcmp(pAllocatedBytes, pAllocatedBytes + 1, dwRequestedSize - 1) == 0,
                    "LoaderHeap must return zero-initialized memory");
            }

            if (!m_fExplicitControl && !IsInterleaved())
            {
                LoaderHeapValidationTag *pTag = AllocMem_GetTag(pAllocatedBytes, dwRequestedSize);
                pTag->m_allocationType  = kAllocMem;
                pTag->m_dwRequestedSize = dwRequestedSize;
                pTag->m_szFile          = szFile;
                pTag->m_lineNum         = lineNum;
            }

            if (m_dwDebugFlags & kCallTracing)
            {
                LoaderHeapSniffer::RecordEvent(this,
                                               kAllocMem,
                                               szFile,
                                               lineNum,
                                               szFile,
                                               lineNum,
                                               pData,
                                               dwRequestedSize,
                                               dwSize
                                               );
            }

#endif

            EtwAllocRequest(this, pData, dwSize);
            RETURN pData;
        }
    }

    // Need to commit some more pages in reserved region.
    // If we run out of pages in the reserved region, ClrVirtualAlloc some more pages
    if (GetMoreCommittedPages(dwSize))
        goto again;

    // We could not satisfy this allocation request
    RETURN NULL;
}

void UnlockedLoaderHeap::UnlockedBackoutMem(void *pMem,
                                            size_t dwRequestedSize
                                            COMMA_INDEBUG(_In_ const char *szFile)
                                            COMMA_INDEBUG(int  lineNum)
                                            COMMA_INDEBUG(_In_ const char *szAllocFile)
                                            COMMA_INDEBUG(int  allocLineNum))
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // Because the primary use of this function is backout, we'll be nice and
    // define Backout(NULL) be a legal NOP.
    if (pMem == NULL)
    {
        return;
    }

#ifdef _DEBUG
    if (!IsInterleaved())
    {
        DEBUG_ONLY_REGION();

        LoaderHeapValidationTag *pTag = AllocMem_GetTag(pMem, dwRequestedSize);

        if (pTag->m_dwRequestedSize != dwRequestedSize || pTag->m_allocationType != kAllocMem)
        {
            CONTRACT_VIOLATION(ThrowsViolation|FaultViolation); // We're reporting a heap corruption - who cares about violations

            StackSString message;
            message.Printf("HEAP VIOLATION: Invalid BackoutMem() call made at:\n"
                           "\n"
                           "     File: %s\n"
                           "     Line: %d\n"
                           "\n"
                           "Attempting to free block originally allocated at:\n"
                           "\n"
                           "     File: %s\n"
                           "     Line: %d\n"
                           "\n"
                           "The arguments to BackoutMem() were:\n"
                           "\n"
                           "     Pointer: 0x%p\n"
                           "     Size:    %lu (0x%lx)\n"
                           "\n"
                           ,szFile
                           ,lineNum
                           ,szAllocFile
                           ,allocLineNum
                           ,pMem
                           ,(ULONG)dwRequestedSize
                           ,(ULONG)dwRequestedSize
                          );


            if (m_dwDebugFlags & kCallTracing)
            {
                message.AppendASCII("*** CALLTRACING ENABLED ***\n");
                LoaderHeapEvent *pEvent = LoaderHeapSniffer::FindEvent(this, pMem);
                if (!pEvent)
                {
                    message.AppendASCII("This pointer doesn't appear to have come from this LoaderHeap.\n");
                }
                else
                {
                    message.AppendASCII(pMem == pEvent->m_pMem ? "We have the following data about this pointer:" : "This pointer points to the middle of the following block:");
                    pEvent->Describe(&message);
                }
            }

            if (pTag->m_dwRequestedSize != dwRequestedSize)
            {
                StackSString buf;
                buf.Printf(
                        "Possible causes:\n"
                        "\n"
                        "   - This pointer wasn't allocated from this loaderheap.\n"
                        "   - This pointer was allocated by AllocAlignedMem and you didn't adjust for the \"extra.\"\n"
                        "   - This pointer has already been freed.\n"
                        "   - You passed in the wrong size. You must pass the exact same size you passed to AllocMem().\n"
                        "   - Someone wrote past the end of this block making it appear as if one of the above were true.\n"
                        );
                message.Append(buf);

            }
            else
            {
                message.AppendASCII("This memory block is completely unrecognizable.\n");
            }


            if (!(m_dwDebugFlags & kCallTracing))
            {
                LoaderHeapSniffer::PitchSniffer(&message);
            }

            StackScratchBuffer scratch;
            DbgAssertDialog(szFile, lineNum, (char*) message.GetANSI(scratch));

        }
    }
#endif

    size_t dwSize = AllocMem_TotalSize(dwRequestedSize, this);

#ifdef _DEBUG
    if ((m_dwDebugFlags & kCallTracing) && !IsInterleaved())
    {
        DEBUG_ONLY_REGION();

        LoaderHeapValidationTag *pTag = m_fExplicitControl ? NULL : AllocMem_GetTag(pMem, dwRequestedSize);


        LoaderHeapSniffer::RecordEvent(this,
                                       kFreedMem,
                                       szFile,
                                       lineNum,
                                       (pTag && (allocLineNum < 0)) ? pTag->m_szFile  : szAllocFile,
                                       (pTag && (allocLineNum < 0)) ? pTag->m_lineNum : allocLineNum,
                                       pMem,
                                       dwRequestedSize,
                                       dwSize
                                       );
    }
#endif

    if (m_pAllocPtr == ( ((BYTE*)pMem) + dwSize ))
    {
        if (IsInterleaved())
        {
            // Clear the RW page
            memset((BYTE*)pMem + GetOsPageSize(), 0x00, dwSize); // Fill freed region with 0
        }
        else
        {
            void *pMemRW = pMem;
            ExecutableWriterHolderNoLog<void> memWriterHolder;
            if (IsExecutable())
            {
                memWriterHolder.AssignExecutableWriterHolder(pMem, dwSize);
                pMemRW = memWriterHolder.GetRW();
            }

            // Cool. This was the last block allocated. We can just undo the allocation instead
            // of going to the freelist.
            memset(pMemRW, 0x00, dwSize); // Fill freed region with 0
        }
        m_pAllocPtr = (BYTE*)pMem;
    }
    else
    {
        LoaderHeapFreeBlock::InsertFreeBlock(&m_pFirstFreeBlock, pMem, dwSize, this);
    }
}


// Allocates memory aligned on power-of-2 boundary.
//
// The return value is a pointer that's guaranteed to be aligned.
//
// FREEING THIS BLOCK: Underneath, the actual block allocated may
// be larger and start at an address prior to the one you got back.
// It is this adjusted size and pointer that you pass to BackoutMem.
// The required adjustment is passed back thru the pdwExtra pointer.
//
// Here is how to properly backout the memory:
//
//   size_t dwExtra;
//   void *pMem = UnlockedAllocAlignedMem(dwRequestedSize, alignment, &dwExtra);
//   _ASSERTE( 0 == (pMem & (alignment - 1)) );
//   UnlockedBackoutMem( ((BYTE*)pMem) - dExtra, dwRequestedSize + dwExtra );
//
// If you use the AllocMemHolder or AllocMemTracker, all this is taken care of
// behind the scenes.
//
//
void *UnlockedLoaderHeap::UnlockedAllocAlignedMem_NoThrow(size_t  dwRequestedSize,
                                                          size_t  alignment,
                                                          size_t *pdwExtra
                                                          COMMA_INDEBUG(_In_ const char *szFile)
                                                          COMMA_INDEBUG(int  lineNum))
{
    CONTRACT(void*)
    {
        NOTHROW;

        // Macro syntax can't handle this INJECT_FAULT expression - we'll use a precondition instead
        //INJECT_FAULT( do{ if (*pdwExtra) {*pdwExtra = 0} RETURN NULL; } while(0) );

        PRECONDITION( alignment != 0 );
        PRECONDITION(0 == (alignment & (alignment - 1))); // require power of 2
        PRECONDITION((dwRequestedSize % m_dwGranularity) == 0);
        POSTCONDITION( (RETVAL) ?
                       (0 == ( ((UINT_PTR)(RETVAL)) & (alignment - 1))) : // If non-null, pointer must be aligned
                       (pdwExtra == NULL || 0 == *pdwExtra)    //   or else *pdwExtra must be set to 0
                     );
    }
    CONTRACT_END

    STATIC_CONTRACT_FAULT;

    // Set default value
            if (pdwExtra)
            {
                *pdwExtra = 0;
            }

    SHOULD_INJECT_FAULT(RETURN NULL);

    void *pResult;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    // Check for overflow if we align the allocation
    if (dwRequestedSize + alignment < dwRequestedSize)
    {
        RETURN NULL;
    }

    // We don't know how much "extra" we need to satisfy the alignment until we know
    // which address will be handed out which in turn we don't know because we don't
    // know whether the allocation will fit within the current reserved range.
    //
    // Thus, we'll request as much heap growth as is needed for the worst case (extra == alignment)
    size_t dwRoomSize = AllocMem_TotalSize(dwRequestedSize + alignment, this);
    if (dwRoomSize > GetBytesAvailCommittedRegion())
    {
        if (!GetMoreCommittedPages(dwRoomSize))
        {
            RETURN NULL;
        }
    }

    pResult = m_pAllocPtr;

    size_t extra = alignment - ((size_t)pResult & ((size_t)alignment - 1));
    if ((IsInterleaved()))
    {
        _ASSERTE(alignment == 1);
        extra = 0;
    }

// On DEBUG, we force a non-zero extra so people don't forget to adjust for it on backout
#ifndef _DEBUG
    if (extra == alignment)
    {
        extra = 0;
    }
#endif

    S_SIZE_T cbAllocSize = S_SIZE_T( dwRequestedSize ) + S_SIZE_T( extra );
    if( cbAllocSize.IsOverflow() )
    {
        RETURN NULL;
    }

    size_t dwSize = AllocMem_TotalSize( cbAllocSize.Value(), this);
    m_pAllocPtr += dwSize;


    ((BYTE*&)pResult) += extra;

#ifdef _DEBUG
    BYTE *pAllocatedBytes = (BYTE *)pResult;
    ExecutableWriterHolderNoLog<void> resultWriterHolder;
    if (IsExecutable())
    {
        resultWriterHolder.AssignExecutableWriterHolder(pResult, dwSize - extra);
        pAllocatedBytes = (BYTE *)resultWriterHolder.GetRW();
    }

#if LOADER_HEAP_DEBUG_BOUNDARY > 0
    // Don't fill the entire memory - we assume it is all zeroed -just the memory after our alloc
    memset(pAllocatedBytes + dwRequestedSize, 0xee, LOADER_HEAP_DEBUG_BOUNDARY);
#endif

    if (dwRequestedSize != 0 && !IsInterleaved())
    {
        _ASSERTE_MSG(pAllocatedBytes[0] == 0 && memcmp(pAllocatedBytes, pAllocatedBytes + 1, dwRequestedSize - 1) == 0,
            "LoaderHeap must return zero-initialized memory");
    }

    if (m_dwDebugFlags & kCallTracing)
    {
        LoaderHeapSniffer::RecordEvent(this,
                                       kAllocMem,
                                       szFile,
                                       lineNum,
                                       szFile,
                                       lineNum,
                                       ((BYTE*)pResult) - extra,
                                       dwRequestedSize + extra,
                                       dwSize
                                       );
    }

    EtwAllocRequest(this, pResult, dwSize);

    if (!m_fExplicitControl && !IsInterleaved())
    {
        LoaderHeapValidationTag *pTag = AllocMem_GetTag(pAllocatedBytes - extra, dwRequestedSize + extra);
        pTag->m_allocationType  = kAllocMem;
        pTag->m_dwRequestedSize = dwRequestedSize + extra;
        pTag->m_szFile          = szFile;
        pTag->m_lineNum         = lineNum;
    }
#endif //_DEBUG

    if (pdwExtra)
    {
        *pdwExtra = extra;
    }

    RETURN pResult;

}



void *UnlockedLoaderHeap::UnlockedAllocAlignedMem(size_t  dwRequestedSize,
                                                  size_t  dwAlignment,
                                                  size_t *pdwExtra
                                                  COMMA_INDEBUG(_In_ const char *szFile)
                                                  COMMA_INDEBUG(int  lineNum))
{
    CONTRACTL
    {
        THROWS;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END

    void *pResult = UnlockedAllocAlignedMem_NoThrow(dwRequestedSize,
                                                    dwAlignment,
                                                    pdwExtra
                                                    COMMA_INDEBUG(szFile)
                                                    COMMA_INDEBUG(lineNum));

    if (!pResult)
    {
        ThrowOutOfMemory();
    }

    return pResult;


}



void *UnlockedLoaderHeap::UnlockedAllocMemForCode_NoThrow(size_t dwHeaderSize, size_t dwCodeSize, DWORD dwCodeAlignment, size_t dwReserveForJumpStubs)
{
    CONTRACT(void*)
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(CONTRACT_RETURN NULL;);
        PRECONDITION(0 == (dwCodeAlignment & (dwCodeAlignment - 1))); // require power of 2
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    _ASSERTE(m_fExplicitControl);

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    // We don't know how much "extra" we need to satisfy the alignment until we know
    // which address will be handed out which in turn we don't know because we don't
    // know whether the allocation will fit within the current reserved range.
    //
    // Thus, we'll request as much heap growth as is needed for the worst case (we request an extra dwCodeAlignment - 1 bytes)

    S_SIZE_T cbAllocSize = S_SIZE_T(dwHeaderSize) + S_SIZE_T(dwCodeSize) + S_SIZE_T(dwCodeAlignment - 1) + S_SIZE_T(dwReserveForJumpStubs);
    if( cbAllocSize.IsOverflow() )
    {
        RETURN NULL;
    }

    if (cbAllocSize.Value() > GetBytesAvailCommittedRegion())
    {
        if (GetMoreCommittedPages(cbAllocSize.Value()) == FALSE)
        {
            RETURN NULL;
        }
    }

    BYTE *pResult = (BYTE *)ALIGN_UP(m_pAllocPtr + dwHeaderSize, dwCodeAlignment);
    EtwAllocRequest(this, pResult, (pResult + dwCodeSize) - m_pAllocPtr);
    m_pAllocPtr = pResult + dwCodeSize;

    RETURN pResult;
}


#endif // #ifndef DACCESS_COMPILE

BOOL UnlockedLoaderHeap::IsExecutable()
{
    return (m_kind == HeapKind::Executable) || IsInterleaved();
}

BOOL UnlockedLoaderHeap::IsInterleaved()
{
    return m_kind == HeapKind::Interleaved;
}

#ifdef DACCESS_COMPILE

void UnlockedLoaderHeap::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    DAC_ENUM_DTHIS();

    PTR_LoaderHeapBlock block = m_pFirstBlock;
    while (block.IsValid())
    {
        // All we know is the virtual size of this block.  We don't have any way to tell how
        // much of this space was actually comitted, so don't expect that this will always
        // succeed.
        // @dbgtodo : Ideally we'd reduce the risk of corruption causing problems here.
        //   We could extend LoaderHeapBlock to track a commit size,
        //   but it seems wasteful (eg. makes each AppDomain objects 32 bytes larger on x64).
        TADDR addr = dac_cast<TADDR>(block->pVirtualAddress);
        TSIZE_T size = block->dwVirtualSize;
        DacEnumMemoryRegion(addr, size, false);

        block = block->pNext;
    }
}

#endif // #ifdef DACCESS_COMPILE


void UnlockedLoaderHeap::EnumPageRegions (EnumPageRegionsCallback *pCallback, PTR_VOID pvArgs)
{
    WRAPPER_NO_CONTRACT;

    PTR_LoaderHeapBlock block = m_pFirstBlock;
    while (block)
    {
        if ((*pCallback)(pvArgs, block->pVirtualAddress, block->dwVirtualSize))
        {
            break;
        }

        block = block->pNext;
    }
}


#ifdef _DEBUG

void UnlockedLoaderHeap::DumpFreeList()
{
    LIMITED_METHOD_CONTRACT;
    if (m_pFirstFreeBlock == NULL)
    {
        printf("FREEDUMP: FreeList is empty\n");
    }
    else
    {
        LoaderHeapFreeBlock *pBlock = m_pFirstFreeBlock;
        while (pBlock != NULL)
        {
            size_t dwsize = pBlock->m_dwSize;
            BOOL ccbad = FALSE;
            BOOL sizeunaligned = FALSE;

            if ( 0 != (dwsize & ALLOC_ALIGN_CONSTANT) )
            {
                sizeunaligned = TRUE;
            }

            for (size_t i = sizeof(LoaderHeapFreeBlock); i < dwsize; i++)
            {
                if ( ((BYTE*)pBlock)[i] != 0xcc )
                {
                    ccbad = TRUE;
                    break;
                }
            }

            printf("Addr = %pxh, Size = %lxh", pBlock, ((ULONG)dwsize));
            if (ccbad) printf(" *** ERROR: NOT CC'd ***");
            if (sizeunaligned) printf(" *** ERROR: size not a multiple of ALLOC_ALIGN_CONSTANT ***");
            printf("\n");

            pBlock = pBlock->m_pNext;
        }
    }
}


void UnlockedLoaderHeap::UnlockedClearEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::ClearEvents(this);
}

void UnlockedLoaderHeap::UnlockedCompactEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::CompactEvents(this);
}

void UnlockedLoaderHeap::UnlockedPrintEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::PrintEvents(this);
}


#endif //_DEBUG

//************************************************************************************
// LOADERHEAP SNIFFER METHODS
//************************************************************************************
#ifdef _DEBUG

/*static*/ VOID LoaderHeapSniffer::RecordEvent(UnlockedLoaderHeap *pHeap,
                                               AllocationType allocationType,
                                               _In_ const char *szFile,
                                               int            lineNum,
                                               _In_ const char *szAllocFile,
                                               int            allocLineNum,
                                               void          *pMem,
                                               size_t         dwRequestedSize,
                                               size_t         dwSize
                                              )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;  //If we OOM in here, we just throw the event away.
    }
    CONTRACTL_END

    LoaderHeapEvent *pNewEvent;
    {
        {
            FAULT_NOT_FATAL();
            pNewEvent = new (nothrow) LoaderHeapEvent;
        }
        if (!pNewEvent)
        {
            if (!(pHeap->m_dwDebugFlags & pHeap->kEncounteredOOM))
            {
                pHeap->m_dwDebugFlags |= pHeap->kEncounteredOOM;
                _ASSERTE(!"LOADERHEAPSNIFFER: Failed allocation of LoaderHeapEvent. Call tracing information will be incomplete.");
            }
        }
        else
        {
            pNewEvent->m_allocationType     = allocationType;
            pNewEvent->m_szFile             = szFile;
            pNewEvent->m_lineNum            = lineNum;
            pNewEvent->m_szAllocFile        = szAllocFile;
            pNewEvent->m_allocLineNum       = allocLineNum;
            pNewEvent->m_pMem               = pMem;
            pNewEvent->m_dwRequestedSize    = dwRequestedSize;
            pNewEvent->m_dwSize             = dwSize;

            pNewEvent->m_pNext              = pHeap->m_pEventList;
            pHeap->m_pEventList             = pNewEvent;
        }
    }
}



/*static*/
void LoaderHeapSniffer::ValidateFreeList(UnlockedLoaderHeap *pHeap)
{
    CANNOT_HAVE_CONTRACT;

    // No contract. This routine is only called if we've AV'd inside the
    // loaderheap. The system is already toast. We're trying to be a hero
    // and produce the best diagnostic info we can. Last thing we need here
    // is a secondary assert inside the contract stuff.
    //
    // This contract violation is permanent.
    CONTRACT_VIOLATION(ThrowsViolation|FaultViolation|GCViolation|ModeViolation);  // This violation won't be removed

    LoaderHeapFreeBlock *pFree     = pHeap->m_pFirstFreeBlock;
    LoaderHeapFreeBlock *pPrev     = NULL;


    void                *pBadAddr = NULL;
    LoaderHeapFreeBlock *pProbeThis = NULL;
    const char          *pExpected = NULL;

    while (pFree != NULL)
    {
        if ( 0 != ( ((ULONG_PTR)pFree) & ALLOC_ALIGN_CONSTANT ))
        {
            // Not aligned - can't be a valid freeblock. Most likely we followed a bad pointer from the previous block.
            pProbeThis = pPrev;
            pBadAddr = pPrev ? &(pPrev->m_pNext) : &(pHeap->m_pFirstFreeBlock);
            pExpected = "a pointer to a valid LoaderHeapFreeBlock";
            break;
        }

        size_t dwSize = pFree->m_dwSize;
        if (dwSize < AllocMem_TotalSize(1, pHeap) ||
            0 != (dwSize & ALLOC_ALIGN_CONSTANT))
        {
            // Size is not a valid value (out of range or unaligned.)
            pProbeThis = pFree;
            pBadAddr = &(pFree->m_dwSize);
            pExpected = "a valid block size (multiple of pointer size)";
            break;
        }

        size_t i;
        for (i = sizeof(LoaderHeapFreeBlock); i < dwSize; i++)
        {
            if ( ((BYTE*)pFree)[i] != 0xcc )
            {
                pProbeThis = pFree;
                pBadAddr = i + ((BYTE*)pFree);
                pExpected = "0xcc (our fill value for free blocks)";
                break;
            }
        }
        if (i != dwSize)
        {
            break;
        }



        pPrev = pFree;
        pFree = pFree->m_pNext;
    }

    if (pFree == NULL)
    {
        return; // No problems found
    }

    {
        StackSString message;

        message.Printf("A loaderheap freelist has been corrupted. The bytes at or near address 0x%p appears to have been overwritten. We expected to see %s here.\n"
                       "\n"
                       "    LoaderHeap:                 0x%p\n"
                       "    Suspect address at:         0x%p\n"
                       "    Start of suspect freeblock: 0x%p\n"
                       "\n"
                       , pBadAddr
                       , pExpected
                       , pHeap
                       , pBadAddr
                       , pProbeThis
                       );

        if (!(pHeap->m_dwDebugFlags & pHeap->kCallTracing))
        {
            message.AppendASCII("\nThe usual reason is that someone wrote past the end of a block or wrote into a block after freeing it."
                           "\nOf course, the culprit is long gone so it's probably too late to debug this now. Try turning on call-tracing"
                           "\nand reproing. We can attempt to find out who last owned the surrounding pieces of memory."
                           "\n"
                           "\nTo turn on call-tracing, set the following registry DWORD value:"
                           "\n"
                           "\n    HKLM\\Software\\Microsoft\\.NETFramework\\LoaderHeapCallTracing = 1"
                           "\n"
                           );

        }
        else
        {
            LoaderHeapEvent *pBadAddrEvent = FindEvent(pHeap, pBadAddr);

            message.AppendASCII("*** CALL TRACING ENABLED ***\n\n");

            if (pBadAddrEvent)
            {
                message.AppendASCII("\nThe last known owner of the corrupted address was:\n");
                pBadAddrEvent->Describe(&message);
            }
            else
            {
                message.AppendASCII("\nNo known owner of last corrupted address.\n");
            }

            LoaderHeapEvent *pPrevEvent = FindEvent(pHeap, ((BYTE*)pProbeThis) - 1);

            int count = 3;
            while (count-- &&
                   pPrevEvent != NULL &&
                   ( ((UINT_PTR)pProbeThis) - ((UINT_PTR)(pPrevEvent->m_pMem)) + pPrevEvent->m_dwSize ) < 1024)
            {
                message.AppendASCII("\nThis block is located close to the corruption point. ");
                if (!pHeap->IsInterleaved() && pPrevEvent->QuietValidate())
                {
                    message.AppendASCII("If it was overrun, it might have caused this.");
                }
                else
                {
                    message.AppendASCII("*** CORRUPTION DETECTED IN THIS BLOCK ***");
                }
                pPrevEvent->Describe(&message);
                pPrevEvent = FindEvent(pHeap, ((BYTE*)(pPrevEvent->m_pMem)) - 1);
            }


        }

        StackScratchBuffer scratch;
        DbgAssertDialog(__FILE__, __LINE__, (char*) message.GetANSI(scratch));

    }



}


BOOL LoaderHeapEvent::QuietValidate()
{
    WRAPPER_NO_CONTRACT;

    if (m_allocationType == kAllocMem)
    {
        LoaderHeapValidationTag *pTag = AllocMem_GetTag(m_pMem, m_dwRequestedSize);
        return (pTag->m_allocationType == m_allocationType && pTag->m_dwRequestedSize == m_dwRequestedSize);
    }
    else
    {
        // We can't easily validate freed blocks.
        return TRUE;
    }
}


#endif //_DEBUG

#ifndef DACCESS_COMPILE

AllocMemTracker::AllocMemTracker()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END

    m_FirstBlock.m_pNext    = NULL;
    m_FirstBlock.m_nextFree = 0;
    m_pFirstBlock = &m_FirstBlock;

    m_fReleased   = FALSE;
}

AllocMemTracker::~AllocMemTracker()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    if (!m_fReleased)
    {
        AllocMemTrackerBlock *pBlock = m_pFirstBlock;
        while (pBlock)
        {
            // Do the loop in reverse - loaderheaps work best if
            // we allocate and backout in LIFO order.
            for (int i = pBlock->m_nextFree - 1; i >= 0; i--)
            {
                AllocMemTrackerNode *pNode = &(pBlock->m_Node[i]);
                pNode->m_pHeap->RealBackoutMem(pNode->m_pMem
                                               ,pNode->m_dwRequestedSize
#ifdef _DEBUG
                                               ,__FILE__
                                               ,__LINE__
                                               ,pNode->m_szAllocFile
                                               ,pNode->m_allocLineNum
#endif
                                              );

            }

            pBlock = pBlock->m_pNext;
        }
    }

// We have seen evidence of memory corruption in this data structure.
// https://github.com/dotnet/runtime/issues/54469
// m_pFirstBlock is intended to be a linked list terminating with
// &m_FirstBlock but we are finding a nullptr in the list before
// that point. In order to investigate further we need to observe
// the corrupted memory block(s) before they are deleted below
#ifdef _DEBUG
    AllocMemTrackerBlock* pDebugBlock = m_pFirstBlock;
    for (int i = 0; pDebugBlock != &m_FirstBlock; i++)
    {
        CONSISTENCY_CHECK_MSGF(i < 10000, ("Linked list is much longer than expected, memory corruption likely\n"));
        CONSISTENCY_CHECK_MSGF(pDebugBlock != nullptr, ("Linked list pointer == NULL, memory corruption likely\n"));
        pDebugBlock = pDebugBlock->m_pNext;
    }
#endif

    AllocMemTrackerBlock *pBlock = m_pFirstBlock;
    while (pBlock != &m_FirstBlock)
    {
        AllocMemTrackerBlock *pNext = pBlock->m_pNext;
        delete pBlock;
        pBlock = pNext;
    }

    INDEBUG(memset(this, 0xcc, sizeof(*this));)
}

void *AllocMemTracker::Track(TaggedMemAllocPtr tmap)
{
    CONTRACTL
    {
        THROWS;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END

    void *pv = Track_NoThrow(tmap);
    if (!pv)
    {
        ThrowOutOfMemory();
    }
    return pv;
}

void *AllocMemTracker::Track_NoThrow(TaggedMemAllocPtr tmap)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END

    // Calling Track() after calling SuppressRelease() is almost certainly a bug. You're supposed to call SuppressRelease() only after you're
    // sure no subsequent failure will force you to backout the memory.
    _ASSERTE( (!m_fReleased) && "You've already called SuppressRelease on this AllocMemTracker which implies you've passed your point of no failure. Why are you still doing allocations?");


    if (tmap.m_pMem != NULL)
    {
        AllocMemHolder<void*> holder(tmap);  // If anything goes wrong in here, this holder will backout the allocation for the caller.
        if (m_fReleased)
        {
            holder.SuppressRelease();
        }
        AllocMemTrackerBlock *pBlock = m_pFirstBlock;
        if (pBlock->m_nextFree == kAllocMemTrackerBlockSize)
        {
            AllocMemTrackerBlock *pNewBlock = new (nothrow) AllocMemTrackerBlock;
            if (!pNewBlock)
            {
                return NULL;
            }

            pNewBlock->m_pNext = m_pFirstBlock;
            pNewBlock->m_nextFree = 0;

            m_pFirstBlock = pNewBlock;

            pBlock = pNewBlock;
        }

        // From here on, we can't fail
        pBlock->m_Node[pBlock->m_nextFree].m_pHeap           = tmap.m_pHeap;
        pBlock->m_Node[pBlock->m_nextFree].m_pMem            = tmap.m_pMem;
        pBlock->m_Node[pBlock->m_nextFree].m_dwRequestedSize = tmap.m_dwRequestedSize;
#ifdef _DEBUG
        pBlock->m_Node[pBlock->m_nextFree].m_szAllocFile     = tmap.m_szFile;
        pBlock->m_Node[pBlock->m_nextFree].m_allocLineNum    = tmap.m_lineNum;
#endif

        pBlock->m_nextFree++;

        holder.SuppressRelease();


    }
    return (void *)tmap;
}


void AllocMemTracker::SuppressRelease()
{
    LIMITED_METHOD_CONTRACT;

    m_fReleased = TRUE;
}

#endif //#ifndef DACCESS_COMPILE
