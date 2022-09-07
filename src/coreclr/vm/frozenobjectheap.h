// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FROZENOBJECTHEAP_H
#define _FROZENOBJECTHEAP_H

#include "gcinterface.h"
#include <sarray.h>

class FrozenObjectHeap;

class FrozenObjectHeapManager
{
public:
    FrozenObjectHeapManager();
    Object* TryAllocateObject(PTR_MethodTable type, size_t objectSize);

private:
    CrstExplicitInit m_Crst;
    SArray<FrozenObjectHeap*> m_FrozenHeaps;
    FrozenObjectHeap* m_CurrentHeap;
    size_t m_HeapCommitChunkSize;
    size_t m_HeapSize;
};

class FrozenObjectHeap
{
public:
    FrozenObjectHeap(size_t reserveSize, size_t commitChunkSize);
    Object* TryAllocateObject(PTR_MethodTable type, size_t objectSize);

private:
    uint8_t* m_pStart;
    uint8_t* m_pCurrent;
    size_t m_CommitChunkSize;
    size_t m_SizeCommitted;
    size_t m_SizeReserved;
    segment_handle m_SegmentHandle;
    INDEBUG(size_t m_ObjectsCount);
};

#endif // _FROZENOBJECTHEAP_H

