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
      m_pCurBlock(NULL),
      m_cbCurBlockUsed(0),
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
AllocHeap::~AllocHeap()
{
    while (!m_blockList.IsEmpty())
    {
        BlockListElem *pCur = m_blockList.PopHead();
        delete[] (uint8_t*)pCur;
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
bool AllocHeap::_AllocNewBlock(uintptr_t cbMem)
{
    uintptr_t cbBlockSize = ALIGN_UP(cbMem + sizeof(BlockListElem), BLOCK_SIZE);

    uint8_t * pbMem = new (nothrow) uint8_t[cbBlockSize];
    if (pbMem == NULL)
        return false;

    BlockListElem *pBlockListElem = reinterpret_cast<BlockListElem*>(pbMem);
    pBlockListElem->m_pNext = NULL;
    pBlockListElem->m_cbMem = cbBlockSize;

    m_blockList.PushHead(pBlockListElem);

    m_pCurBlock = pBlockListElem;
    m_cbCurBlockUsed = sizeof(BlockListElem);

    return true;
}

//-------------------------------------------------------------------------------------------------
uint8_t * AllocHeap::_AllocFromCurBlock(
    uintptr_t cbMem,
    uintptr_t alignment)
{
    if (m_pCurBlock == NULL)
        return NULL;

    uint8_t* pBlockStart = (uint8_t*)m_pCurBlock;
    uint8_t* pAlloc = (uint8_t*)ALIGN_UP(pBlockStart + m_cbCurBlockUsed, alignment);
    uintptr_t cbAllocEnd = pAlloc + cbMem - pBlockStart;

    if (cbAllocEnd > m_pCurBlock->m_cbMem)
        return NULL;

    m_cbCurBlockUsed = cbAllocEnd;
    return pAlloc;
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

