// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPFRAMEALLOCATOR_H_
#define _INTERPFRAMEALLOCATOR_H_

struct FrameDataFragment
{
    // The start of the fragment
    uint8_t *start;
    // The end of the fragment
    uint8_t *end;
    // The current position in the fragment
    uint8_t *pos;
    // The next fragment in the list
    FrameDataFragment *pNext;

    FrameDataFragment(size_t size);
    ~FrameDataFragment();
};

struct FrameDataInfo
{
    // The frame that this data belongs to
    InterpreterFrame *pFrame;
    // Pointers for restoring the localloc memory:
    // pFrag - the current allocation fragment at frame entry
    // pos - the fragment pointer at frame entry
    // When the frame returns, we use these to roll back any local allocations
    FrameDataFragment *pFrag;
    uint8_t *pos;

    FrameDataInfo(InterpreterFrame *pFrame, FrameDataFragment *pFrag, uint8_t *pos);
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

    void *Alloc(InterpreterFrame *pFrame, size_t size);
    void FreeFragments(FrameDataFragment *pFrag);
    void PushInfo(InterpreterFrame *pFrame);
    void PopInfo(InterpreterFrame *pFrame);
};

#endif
