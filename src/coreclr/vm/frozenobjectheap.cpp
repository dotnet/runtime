// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "frozenobjectheap.h"
#include "memorypool.h"

bool FrozenObjectHeap::Init(size_t fohSize)
{
    _ASSERT(fohSize > (FOX_MAX_OBJECT_SIZE * 2));

    // TODO: remove commit
    void* alloc = ClrVirtualAlloc(nullptr, fohSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    if (alloc != nullptr)
    {
        m_SegmentHandle = GCInterface::RegisterFrozenSegment(alloc, fohSize);
        if (m_SegmentHandle != nullptr)
        {
            m_pStart = static_cast<uint8_t*>(alloc);
            m_pCurrent = m_pStart;
            m_Size = fohSize;
            return true;
        }
    }
    return false;
}

Object* FrozenObjectHeap::AllocateObject(size_t objectSize)
{
    if (objectSize > FOX_MAX_OBJECT_SIZE)
    {
        // object is to big
        return nullptr;
    }

    uint8_t* obj = ALIGN_UP(m_pCurrent, DATA_ALIGNMENT);
    if ((obj + objectSize + OBJHEADER_SIZE) > (m_pStart + m_Size))
    {
        // heap is full
        return nullptr;
    }
    m_pCurrent = obj + OBJECT_BASESIZE + objectSize;
    ZeroMemory(obj, objectSize);
    return reinterpret_cast<Object*>(obj + OBJECT_BASESIZE);
}

bool FrozenObjectHeap::IsInHeap(Object* object)
{
    auto ptr = reinterpret_cast<uint8_t*>(object);
    if (ptr >= m_pStart && ptr < m_pCurrent)
    {
        _ASSERT(GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(object));
        return true;
    }
    return false;
}

FrozenObjectHeap::~FrozenObjectHeap()
{
    if (m_SegmentHandle != nullptr)
    {
        GCInterface::UnregisterFrozenSegment(m_SegmentHandle);
        ClrVirtualFree(m_pStart, 0, MEM_RELEASE);
    }
}
