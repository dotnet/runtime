// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_INTERPRETER

#include "interpexec.h"
#include "interpframeallocator.h"

FrameDataFragment::FrameDataFragment(size_t size)
{
    if (size < INTERP_STACK_FRAGMENT_SIZE)
    {
        size = INTERP_STACK_FRAGMENT_SIZE;
    }

    start = (uint8_t*)malloc(size);
    if (start == nullptr)
    {
        // Interpreter-TODO: Throw OutOfMemory exception
        assert(start && "Failed to allocate FrameDataFragment");
    }
    end = start + size;
    pos = start;
    pNext = nullptr;
}

FrameDataFragment::~FrameDataFragment()
{
    free(start);
}

FrameDataAllocator::FrameDataAllocator(size_t size)
{
    pFirst = new FrameDataFragment(size);
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

void FrameDataAllocator::PushInfo(InterpMethodContextFrame *pFrame)
{
    if (infosLen == infosCapacity)
    {
        size_t newCapacity = infosCapacity == 0 ? 8 : infosCapacity * 2;
        pInfos = (FrameDataInfo*)realloc(pInfos, newCapacity * sizeof(FrameDataInfo));
        if (pInfos == nullptr)
        {
            // Interpreter-TODO: Throw OutOfMemory exception
            assert(pInfos && "Failed to allocate FrameDataInfo");
        }
        infosCapacity = newCapacity;
    }

    FrameDataInfo *pInfo = &pInfos[infosLen++];
    pInfo->pFrame = pFrame;
    pInfo->pFrag = pCurrent;
    pInfo->pos = pCurrent->pos;
}

void *FrameDataAllocator::Alloc(InterpMethodContextFrame *pFrame, size_t size)
{
    if (!infosLen || (infosLen > 0 && pInfos[infosLen - 1].pFrame != pFrame))
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
            pCurrent->pNext = pNewFrag;
            pCurrent = pNewFrag;

            pos = pNewFrag->pos;
        }
    }

    void *result = (void*)pos;
    pCurrent->pos = (uint8_t*)(pos + size);
    return result;
}

void FrameDataAllocator::PopInfo(InterpMethodContextFrame *pFrame)
{
    size_t top = infosLen - 1;
    if (top >= 0 && pInfos[top].pFrame == pFrame)
    {
        FrameDataInfo *pInfo = &pInfos[--infosLen];
        pCurrent = pInfo->pFrag;
        pCurrent->pos = pInfo->pos;
    }
}

#endif // FEATURE_INTERPRETER
