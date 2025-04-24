// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPFRAMEALLOCATOR_H_
#define _INTERPFRAMEALLOCATOR_H_

struct FrameDataFragment
{
    // The start of the fragment
    uint8_t *pFrameStart;
    // The end of the fragment
    uint8_t *pFrameEnd;
    // The current position in the fragment
    uint8_t *pFramePos;
    // The next fragment in the list
    FrameDataFragment *pNext;

    FrameDataFragment(size_t size);
    ~FrameDataFragment();
};

struct FrameDataInfo
{
    // The frame that this data belongs to
    InterpMethodContextFrame *pFrame;
    // Pointers for restoring the localloc memory:
    // pFrag - the current allocation fragment at frame entry
    // pos - the fragment pointer at frame entry
    // When the frame returns, we use these to roll back any local allocations
    FrameDataFragment *pFrag;
    uint8_t *pFramePos;

    FrameDataInfo(InterpMethodContextFrame *pFrame, FrameDataFragment *pFrag, uint8_t *pFramePos);
};

struct FrameDataAllocator
{
    FrameDataFragment *pFirst;
    FrameDataFragment *pCurrent;
    FrameDataInfo *pInfos;
    size_t infosLen;
    size_t infosCapacity;

    FrameDataAllocator(size_t size);
    ~FrameDataAllocator();

    void *Alloc(InterpMethodContextFrame *pFrame, size_t size);
    void FreeFragments(FrameDataFragment *pFrag);
    bool PushInfo(InterpMethodContextFrame *pFrame);
    void PopInfo(InterpMethodContextFrame *pFrame);
};

#endif
