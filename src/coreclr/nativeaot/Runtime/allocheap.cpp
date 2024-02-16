// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "DebugMacrosExt.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "Crst.h"
#include "Range.h"
#include "allocheap.h"

#include "CommonMacros.inl"
#include "slist.inl"

using namespace rh::util;

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

    BlockListElem *pBlock = new (nothrow) BlockListElem(pbInitialMem, cbInitialMemReserve);
    if (pBlock == NULL)
        return false;
    m_blockList.PushHead(pBlock);

    if (!_UpdateMemPtrs(pbInitialMem,
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
            PalVirtualFree(pCur->GetStart(), pCur->GetLength());
        delete pCur;
    }
}

//-------------------------------------------------------------------------------------------------
uint8_t * AllocHeap::_Alloc(
    uintptr_t cbMem,
    uintptr_t alignment
    )
{
    ASSERT((alignment & (alignment - 1)) == 0); // Power of 2 only.
    ASSERT((int32_t)alignment <= OS_PAGE_SIZE);          // Can't handle this right now.

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
bool AllocHeap::Contains(void* pvMem, uintptr_t cbMem)
{
    MemRange range(pvMem, cbMem);
    for (BlockList::Iterator it = m_blockList.Begin(); it != m_blockList.End(); ++it)
    {
        if (it->Contains(range))
        {
            return true;
        }
    }
    return false;
}

//-------------------------------------------------------------------------------------------------
bool AllocHeap::_UpdateMemPtrs(uint8_t* pNextFree, uint8_t* pFreeCommitEnd, uint8_t* pFreeReserveEnd)
{
    ASSERT(MemRange(pNextFree, pFreeReserveEnd).Contains(MemRange(pNextFree, pFreeCommitEnd)));
    ASSERT(ALIGN_DOWN(pFreeCommitEnd, OS_PAGE_SIZE) == pFreeCommitEnd);
    ASSERT(ALIGN_DOWN(pFreeReserveEnd, OS_PAGE_SIZE) == pFreeReserveEnd);

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
    cbMem = ALIGN_UP(cbMem, OS_PAGE_SIZE);

    uint8_t * pbMem = reinterpret_cast<uint8_t*>
        (PalVirtualAlloc(cbMem, PAGE_READWRITE));

    if (pbMem == NULL)
        return false;

    BlockListElem *pBlockListElem = new (nothrow) BlockListElem(pbMem, cbMem);
    if (pBlockListElem == NULL)
    {
        PalVirtualFree(pbMem, cbMem);
        return false;
    }

    // Add to the list. While there is no race for writers (we hold the lock) we have the
    // possibility of simultaneous readers, and using the interlocked version creates a
    // memory barrier to make sure any reader sees a consistent list.
    m_blockList.PushHeadInterlocked(pBlockListElem);

    return _UpdateMemPtrs(pbMem, pbMem + cbMem, pbMem + cbMem);
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
        uintptr_t cbMemToCommit = ALIGN_UP(cbMem, OS_PAGE_SIZE);
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

