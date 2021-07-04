// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FROZENOBJECTHEAP_H
#define _FROZENOBJECTHEAP_H

#include "common.h"

#define FOX_MAX_OBJECT_SIZE 2 * 1024
#define FOH_DEFAULT_SIZE    1 * 1024 * 1024

class FrozenObjectHeap
{
public:
    bool Init(CrstExplicitInit crst, size_t fohSize = FOH_DEFAULT_SIZE);

    Object* AllocateObject(size_t objectSize);

    bool IsInHeap(Object* object);

    ~FrozenObjectHeap();

private:
    uint8_t* m_pStart;
    uint8_t* m_pCurrent;
    uint8_t* m_pCommited;
    size_t m_Size;
    size_t m_PageSize;
    void* m_SegmentHandle;
    CrstExplicitInit m_Crst;

    INDEBUG(size_t m_ObjectsCount);
};

#endif // _FROZENOBJECTHEAP_H

