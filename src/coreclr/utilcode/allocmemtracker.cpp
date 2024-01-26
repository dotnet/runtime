// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"                     // Precompiled header key.
#include "loaderheap.h"
#include "ex.h"
#include "pedecoder.h"
#define DONOT_DEFINE_ETW_CALLBACK
#include "eventtracebase.h"

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
