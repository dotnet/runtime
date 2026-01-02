// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "DebugMacrosExt.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "Crst.h"
#include "allocheap.h"

#include "CommonMacros.inl"
#include "slist.inl"

//-------------------------------------------------------------------------------------------------
AllocHeap::AllocHeap()
    : m_blockList(),
      m_pNextFree(NULL),
      m_pFreeCommitEnd(NULL),
      m_pFreeReserveEnd(NULL),
      m_pbInitialMem(NULL),
      m_fShouldFreeInitialMem(false),
      m_lock(CrstAllocHeap)
      COMMA_INDEBUG(m_fIsInit(false))
{
}

//-------------------------------------------------------------------------------------------------
bool AllocHeap::Init()
{
    ASSERT(!m_fIsInit);
    INDEBUG(m_fIsInit = true;)

    return true;
}

//-------------------------------------------------------------------------------------------------
// This is for using pre-allocated memory on heap construction.
// Should never use this more than once, and should always follow construction of heap.

bool AllocHeap::Init(
    uint8_t *    pbInitialMem,
    uintptr_t cbInitialMemCommit,
    uintptr_t cbInitialMemReserve,
    bool       fShouldFreeInitialMem)
{
    ASSERT(!m_fIsInit);

    BlockListElem *pBlock = reinterpret_cast<BlockListElem*>(pbInitialMem);
    pBlock->m_pNext = NULL;
    pBlock->m_cbMem = cbInitialMemReserve;
    m_blockList.PushHead(pBlock);

    uint8_t* dataStart = pBlock->GetDataStart();
    if (!_UpdateMemPtrs(dataStart,
                        pbInitialMem + cbInitialMemCommit,
                        pbInitialMem + cbInitialMemReserve))
    {
        return false;
    }

    m_pbInitialMem = pbInitialMem;
    m_fShouldFreeInitialMem = fShouldFreeInitialMem;

    INDEBUG(m_fIsInit = true;)
    return true;
}

//-------------------------------------------------------------------------------------------------
AllocHeap::~AllocHeap()
{
    while (!m_blockList.IsEmpty())
    {
        BlockListElem *pCur = m_blockList.PopHead();
        if (pCur->GetStart() != m_pbInitialMem || m_fShouldFreeInitialMem)
            delete[] pCur->GetStart();
    }
}

//-------------------------------------------------------------------------------------------------
uint8_t * AllocHeap::_Alloc(
    uintptr_t cbMem,
    uintptr_t alignment
    )
{
    ASSERT((alignment & (alignment - 1)) == 0); // Power of 2 only.
    ASSERT(alignment <= BLOCK_SIZE);            // Can't handle this right now.

    CrstHolder lock(&m_lock);

    uint8_t * pbMem = _AllocFromCurBlock(cbMem, alignment);
    if (pbMem != NULL)
        return pbMem;

    // Must allocate new block
    if (!_AllocNewBlock(cbMem))
        return NULL;

    pbMem = _AllocFromCurBlock(cbMem, alignment);
    ASSERT_MSG(pbMem != NULL, "AllocHeap::Alloc: failed to alloc mem after new block alloc");

    return pbMem;
}

//-------------------------------------------------------------------------------------------------
uint8_t * AllocHeap::Alloc(
    uintptr_t cbMem)
{
    return _Alloc(cbMem, 1);
}

//-------------------------------------------------------------------------------------------------
uint8_t * AllocHeap::AllocAligned(
    uintptr_t cbMem,
    uintptr_t alignment)
{
    return _Alloc(cbMem, alignment);
}

//-------------------------------------------------------------------------------------------------
bool AllocHeap::_UpdateMemPtrs(uint8_t* pNextFree, uint8_t* pFreeCommitEnd, uint8_t* pFreeReserveEnd)
{
    ASSERT(ALIGN_DOWN(pFreeCommitEnd, BLOCK_SIZE) == pFreeCommitEnd);
    ASSERT(ALIGN_DOWN(pFreeReserveEnd, BLOCK_SIZE) == pFreeReserveEnd);

    m_pNextFree = pNextFree;
    m_pFreeCommitEnd = pFreeCommitEnd;
    m_pFreeReserveEnd = pFreeReserveEnd;
    return true;
}

//-------------------------------------------------------------------------------------------------
bool AllocHeap::_UpdateMemPtrs(uint8_t* pNextFree, uint8_t* pFreeCommitEnd)
{
    return _UpdateMemPtrs(pNextFree, pFreeCommitEnd, m_pFreeReserveEnd);
}

//-------------------------------------------------------------------------------------------------
bool AllocHeap::_UpdateMemPtrs(uint8_t* pNextFree)
{
    return _UpdateMemPtrs(pNextFree, m_pFreeCommitEnd);
}

//-------------------------------------------------------------------------------------------------
bool AllocHeap::_AllocNewBlock(uintptr_t cbMem)
{
    // Calculate block size: ensure it's at least BLOCK_SIZE and aligned to BLOCK_SIZE
    // Also need room for BlockListElem header
    uintptr_t cbBlockSize = ALIGN_UP(cbMem + sizeof(BlockListElem), BLOCK_SIZE);
    if (cbBlockSize < BLOCK_SIZE)
        cbBlockSize = BLOCK_SIZE;

    uint8_t * pbMem = new (nothrow) uint8_t[cbBlockSize];

    if (pbMem == NULL)
        return false;

    BlockListElem *pBlockListElem = reinterpret_cast<BlockListElem*>(pbMem);
    pBlockListElem->m_pNext = NULL;
    pBlockListElem->m_cbMem = cbBlockSize;

    // Add to the list. While there is no race for writers (we hold the lock) we have the
    // possibility of simultaneous readers, and using the interlocked version creates a
    // memory barrier to make sure any reader sees a consistent list.
    m_blockList.PushHeadInterlocked(pBlockListElem);

    uint8_t* dataStart = pBlockListElem->GetDataStart();
    return _UpdateMemPtrs(dataStart, pbMem + cbBlockSize, pbMem + cbBlockSize);
}

//-------------------------------------------------------------------------------------------------
uint8_t * AllocHeap::_AllocFromCurBlock(
    uintptr_t cbMem,
    uintptr_t alignment)
{
    uint8_t * pbMem = NULL;

    cbMem += (uint8_t *)ALIGN_UP(m_pNextFree, alignment) - m_pNextFree;

    if (m_pNextFree + cbMem <= m_pFreeCommitEnd ||
        _CommitFromCurBlock(cbMem))
    {
        ASSERT(cbMem + m_pNextFree <= m_pFreeCommitEnd);

        pbMem = ALIGN_UP(m_pNextFree, alignment);

        if (!_UpdateMemPtrs(m_pNextFree + cbMem))
            return NULL;
    }

    return pbMem;
}

//-------------------------------------------------------------------------------------------------
bool AllocHeap::_CommitFromCurBlock(uintptr_t cbMem)
{
    ASSERT(m_pFreeCommitEnd < m_pNextFree + cbMem);

    if (m_pNextFree + cbMem <= m_pFreeReserveEnd)
    {
        uintptr_t cbMemToCommit = ALIGN_UP(cbMem, BLOCK_SIZE);
        return _UpdateMemPtrs(m_pNextFree, m_pFreeCommitEnd + cbMemToCommit);
    }

    return false;
}

//-------------------------------------------------------------------------------------------------
void * __cdecl operator new(size_t n, AllocHeap * alloc)
{
    return alloc->Alloc(n);
}

//-------------------------------------------------------------------------------------------------
void * __cdecl operator new[](size_t n, AllocHeap * alloc)
{
    return alloc->Alloc(n);
}

