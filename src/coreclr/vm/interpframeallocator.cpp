// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_INTERPRETER

#include "interpexec.h"
#include "interpframeallocator.h"

FrameDataAllocator::FrameDataFragment::FrameDataFragment(size_t size)
{
    // Amortize allocation cost by allocating a larger chunk of memory
    if (size < INTERP_STACK_FRAGMENT_SIZE)
    {
        size = INTERP_STACK_FRAGMENT_SIZE;
    }

    pFrameStart = (uint8_t*)malloc(size);
    if (pFrameStart != nullptr)
    {
        pFrameEnd = pFrameStart + size;
        pFramePos = pFrameStart;
    }
    pNext = nullptr;
}

FrameDataAllocator::FrameDataFragment::~FrameDataFragment()
{
    free(pFrameStart);
}

FrameDataAllocator::FrameDataAllocator()
{
    pFirst = pCurrent = nullptr;
    pInfos = nullptr;
    infosLen = 0;
    infosCapacity = 0;
}

FrameDataAllocator::~FrameDataAllocator()
{
    if (pFirst != nullptr)
    {
        assert(pCurrent == pFirst && pCurrent->pFramePos == pCurrent->pFrameStart);
        FreeFragments(pFirst);
        free(pInfos);
    }
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

bool FrameDataAllocator::PushInfo(InterpMethodContextFrame *pFrame)
{
    if (infosLen == infosCapacity)
    {
        size_t newCapacity = infosCapacity == 0 ? 8 : infosCapacity * 2;
        if (newCapacity > SIZE_MAX / sizeof(FrameDataInfo))
        {
            return false;
        }
        FrameDataInfo* newInfos = (FrameDataInfo*)realloc(pInfos, newCapacity * sizeof(FrameDataInfo));
        if (newInfos == nullptr)
        {
            return false;
        }
        pInfos = newInfos;
        infosCapacity = newCapacity;
    }

    FrameDataInfo *pInfo = &pInfos[infosLen++];
    pInfo->pFrame = pFrame;
    pInfo->pFrag = pCurrent;
    pInfo->pFramePos = pCurrent->pFramePos;
    return true;
}

void *FrameDataAllocator::Alloc(InterpMethodContextFrame *pFrame, size_t size)
{
    size = ALIGN_UP(size, sizeof(void*));

    if (pFirst == nullptr)
    {
        pFirst = new (nothrow) FrameDataFragment(size);
        if (pFirst == nullptr || pFirst->pFrameStart == nullptr)
        {
            return nullptr;
        }
        pCurrent = pFirst;
    }

    if (!infosLen || pInfos[infosLen - 1].pFrame != pFrame)
    {
        if (!PushInfo(pFrame))
        {
            return nullptr;
        }
    }

    uint8_t *pFramePos = pCurrent->pFramePos;

    if (pFramePos + size > pCurrent->pFrameEnd)
    {
        // Move to the next fragment or create a new one if necessary
        if (pCurrent->pNext && ((pCurrent->pNext->pFrameStart + size) <= pCurrent->pNext->pFrameEnd))
        {
            pCurrent = pCurrent->pNext;
            pFramePos = pCurrent->pFramePos = pCurrent->pFrameStart;
        }
        else
        {
            FreeFragments(pCurrent->pNext);
            pCurrent->pNext = nullptr;

            FrameDataFragment *pNewFrag = new (nothrow) FrameDataFragment(size);
            if (pNewFrag == nullptr || pNewFrag->pFrameStart == nullptr)
            {
                return nullptr;
            }

            pCurrent->pNext = pNewFrag;
            pCurrent = pNewFrag;
            pFramePos = pNewFrag->pFramePos;
        }
    }

    void *pMemory = (void*)pFramePos;
    pCurrent->pFramePos = (uint8_t*)(pCurrent->pFramePos + size);
    return pMemory;
}

void FrameDataAllocator::PopInfo(InterpMethodContextFrame *pFrame)
{
    if (infosLen > 0 && pInfos[infosLen - 1].pFrame == pFrame)
    {
        FrameDataInfo *pInfo = &pInfos[--infosLen];
        pCurrent = pInfo->pFrag;
        pCurrent->pFramePos = pInfo->pFramePos;
    }
}

#endif // FEATURE_INTERPRETER
