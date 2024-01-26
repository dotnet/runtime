// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"                     // Precompiled header key.
#include "loaderheap.h"
#include "loaderheap_shared.h"
#include "ex.h"
#include "pedecoder.h"
#define DONOT_DEFINE_ETW_CALLBACK
#include "eventtracebase.h"

#ifndef DACCESS_COMPILE

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
        EMEM_OUT(("MEM: RangeListBlock %p - %p\n", range->start, range->end));
        DacEnumMemoryRegion(range->start, size, false);
    }
}

#endif // #ifdef DACCESS_COMPILE


