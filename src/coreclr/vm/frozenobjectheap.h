// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FROZENOBJECTHEAP_H
#define _FROZENOBJECTHEAP_H

#include "common.h"
#include "gcinterface.h"

class FrozenObjectHeap
{
public:
    FrozenObjectHeap();
    ~FrozenObjectHeap();
    Object* AllocateObject(size_t objectSize);
    bool IsInHeap(Object* object);

private:
    bool Initialize();

    uint8_t* m_pStart;
    uint8_t* m_pCurrent;
    uint8_t* m_pCommited;
    size_t m_Size;
    size_t m_PageSize;
    segment_handle m_SegmentHandle;
    CrstExplicitInit m_Crst;
    bool m_FailedToInit;
    INDEBUG(size_t m_ObjectsCount);
};

#endif // _FROZENOBJECTHEAP_H

