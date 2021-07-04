// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "frozenobjectheap.h"
#include "memorypool.h"
#include "memorypool.h"

bool FrozenObjectHeap::Init(CrstExplicitInit crst, size_t fohSize)
{
    m_PageSize = GetOsPageSize();
    _ASSERT(fohSize >= m_PageSize);

    // TODO: Implement COMMIT on demand.
    void* alloc = ClrVirtualAllocAligned(nullptr, fohSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE, m_PageSize);
    ZeroMemory(alloc, fohSize); // will remove it, was just testing.

    if (alloc != nullptr)
    {
        m_SegmentHandle = GCInterface::RegisterFrozenSegment(alloc, fohSize);
        if (m_SegmentHandle != nullptr)
        {
            m_Crst = crst;
            m_pStart = static_cast<uint8_t*>(alloc);
            m_pCurrent = m_pStart;
            m_pCommited = m_pStart;
            m_Size = fohSize;

            INDEBUG(m_ObjectsCount = 0);

            ASSERT((intptr_t)m_pCurrent % DATA_ALIGNMENT == 0);
            return true;
        }
    }
    return false;
}

Object* FrozenObjectHeap::AllocateObject(size_t objectSize)
{
    CrstHolder ch(&m_Crst);

    if (objectSize > m_PageSize)
    {
        // Since FrozenObjectHeap is just an optimization, let's not fill it with large objects.
        return nullptr;
    }

    _ASSERT(IS_ALIGNED(objectSize, DATA_ALIGNMENT));
    _ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));

    uint8_t* obj = ALIGN_UP(m_pCurrent, DATA_ALIGNMENT);
    if ((obj + objectSize + OBJHEADER_SIZE) > (m_pStart + m_Size))
    {
        // heap is full
        return nullptr;
    }

    INDEBUG(m_ObjectsCount++);
    m_pCurrent = obj + OBJECT_BASESIZE + objectSize;
    ZeroMemory(obj, objectSize); // is it needed?
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
