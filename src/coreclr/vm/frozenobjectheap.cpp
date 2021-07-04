// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "frozenobjectheap.h"
#include "memorypool.h"

FrozenObjectHeap::FrozenObjectHeap():
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_pCommited(nullptr),
    m_Size(0),
    m_PageSize(0),
    m_SegmentHandle(nullptr),
    m_ObjectsCount(0)
{
    m_Crst.Init(CrstFrozenObjectHeap, CRST_UNSAFE_ANYMODE);
}

FrozenObjectHeap::~FrozenObjectHeap()
{
    if (m_SegmentHandle != nullptr)
    {
        GCHeapUtilities::GetGCHeap()->UnregisterFrozenSegment(m_SegmentHandle);
    }

    if (m_pStart != nullptr)
    {
        ClrVirtualFree(m_pStart, 0, MEM_RELEASE);
    }
}

bool FrozenObjectHeap::Init(size_t fohSize)
{
    m_PageSize = GetOsPageSize();
    _ASSERT(fohSize >= m_PageSize);
    _ASSERT(m_SegmentHandle == nullptr);
    _ASSERT(m_pStart == nullptr);

    // TODO: Implement COMMIT on demand.
    void* alloc = ClrVirtualAllocAligned(nullptr, fohSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE, m_PageSize);

    if (alloc != nullptr)
    {
        segment_info seginfo{};
        seginfo.pvMem = m_pStart;
        seginfo.ibFirstObject = sizeof(ObjHeader);
        seginfo.ibAllocated = m_Size;
        seginfo.ibCommit = seginfo.ibAllocated;
        seginfo.ibReserved = seginfo.ibAllocated;

        m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&seginfo);
        if (m_SegmentHandle != nullptr)
        {
            m_pStart = static_cast<uint8_t*>(alloc);
            m_pCurrent = m_pStart;
            m_pCommited = m_pStart;
            m_Size = fohSize;

            INDEBUG(m_ObjectsCount = 0);

            ASSERT((intptr_t)m_pCurrent % DATA_ALIGNMENT == 0);
            return true;
        }

        // GC refused to register frozen segment
        ClrVirtualFree(m_pStart, 0, MEM_RELEASE);
        m_pStart = nullptr;
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

    if ((m_pStart == nullptr) && !Init())
    {
        return nullptr;
    }

    _ASSERT(m_pStart != nullptr);
    _ASSERT(m_SegmentHandle != nullptr);

    _ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));
    _ASSERT(IS_ALIGNED(objectSize, DATA_ALIGNMENT));

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
    const auto ptr = reinterpret_cast<uint8_t*>(object);
    if (ptr >= m_pStart && ptr < m_pCurrent)
    {
        _ASSERT(GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(object));
        return true;
    }
    return false;
}
