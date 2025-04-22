// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpexec.h"
#include "interpframeallocator.h"

FrameDataFragment::FrameDataFragment(size_t size)
{
    if (size < INTERP_STACK_FRAGMENT_SIZE)
    {
        size = INTERP_STACK_FRAGMENT_SIZE;
    }

    void *pMemory = malloc(sizeof(FrameDataFragment) + size);
    assert(pMemory && "Failed to allocate FrameDataFragment");

    start = (uint8_t*)pMemory + sizeof(FrameDataFragment);
    end = start + size;
    pos = start;
    pNext = nullptr;
}

FrameDataAllocator::FrameDataAllocator(size_t size)
{
    pFirst = new FrameDataFragment(size);
    assert(pFirst && "Failed to allocate initial fragment");
    pCurrent = pFirst;
    pInfos = nullptr;
    infosLen = 0;
    infosCapacity = 0;
}

FrameDataAllocator::~FrameDataAllocator()
{
    assert(pCurrent == pFirst && pCurrent->pos == pCurrent->start);
    FreeFragments(pFirst);
    free(pInfos);
}

void FrameDataAllocator::FreeFragments(FrameDataFragment *pFrag)
{
    while (pFrag)
    {
        FrameDataFragment *pNext = pFrag->pNext;
        delete pFrag;
        pFrag = pNext;
    }
}

void FrameDataAllocator::PushInfo(InterpreterFrame *pFrame)
{
    if (infosLen == infosCapacity)
    {
        size_t newCapacity = infosCapacity == 0 ? 8 : infosCapacity * 2;
        pInfos = (FrameDataInfo*)realloc(pInfos, newCapacity * sizeof(FrameDataInfo));
        assert(pInfos && "Failed to reallocate frame info");
        infosCapacity = newCapacity;
    }

    FrameDataInfo *pInfo = &pInfos[infosLen++];
    pInfo->frame = pFrame;
    pInfo->pFrag = pCurrent;
    pInfo->pos = pCurrent->pos;
}

void *FrameDataAllocator::Alloc(InterpreterFrame *pFrame, size_t size)
{
    if (!infosLen || (infosLen > 0 && pInfos[infosLen - 1].frame != pFrame))
    {
        PushInfo(pFrame);
    }

    uint8_t *pos = pCurrent->pos;

    if (pos + size > pCurrent->end)
    {
        if (pCurrent->pNext && ((pCurrent->pNext->start + size) <= pCurrent->pNext->end))
        {
            pCurrent = pCurrent->pNext;
            pos = pCurrent->pos = pCurrent->start;
        }
        else
        {
            FreeFragments(pCurrent->pNext);
            FrameDataFragment *pNewFrag = new FrameDataFragment(size);
            assert(pNewFrag && "Failed to allocate new fragment");
            pCurrent->pNext = pNewFrag;
            pCurrent = pNewFrag;

            pos = pNewFrag->pos;
        }
    }

    void *result = (void*)pos;
    pCurrent->pos = (uint8_t*)(pos + size);
    return result;
}

void FrameDataAllocator::PopInfo(InterpreterFrame *pFrame)
{
    int top = infosLen - 1;
    if (top >= 0 && pInfos[top].frame == pFrame)
    {
        FrameDataInfo *pInfo = &pInfos[--infosLen];
        pCurrent = pInfo->pFrag;
        pCurrent->pos = pInfo->pos;
    }
}
