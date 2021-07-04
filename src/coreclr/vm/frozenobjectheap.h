// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FROZENOBJECTHEAP_H
#define _FROZENOBJECTHEAP_H

#define FOX_MAX_OBJECT_SIZE 2 * 1024
#define FOH_DEFAULT_SIZE    1 * 1024 * 1024

class FrozenObjectHeap
{
public:
    bool Init(size_t fohSize = FOH_DEFAULT_SIZE);

    Object* AllocateObject(size_t objectSize);

    bool IsInHeap(Object* object);

    ~FrozenObjectHeap();

private:
    uint8_t* m_pStart;
    uint8_t* m_pCurrent;
    size_t m_Size;
    void* m_SegmentHandle;
};

#endif // _FROZENOBJECTHEAP_H

