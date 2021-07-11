// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "frozenobjectheap.h"
#include "memorypool.h"

FrozenObjectHeap::FrozenObjectHeap():
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_pCommited(nullptr),
    m_Size(0),
    m_SegmentHandle(nullptr),
    m_ObjectsCount(0)
{
    m_PageSize = GetOsPageSize();
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

bool FrozenObjectHeap::Initialize()
{
    m_Size = m_PageSize * 1024;

    _ASSERT(m_PageSize > MIN_OBJECT_SIZE);
    _ASSERT(m_SegmentHandle == nullptr);
    _ASSERT(m_pStart == nullptr);

    // TODO: Implement COMMIT on demand.
    void* alloc = ClrVirtualAllocAligned(nullptr, m_Size, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE, m_PageSize);
    ZeroMemory(alloc, m_Size); // Will remove, was just testing.

    if (alloc != nullptr)
    {
        segment_info si{};
        si.pvMem = m_pStart;
        si.ibFirstObject = sizeof(ObjHeader);
        si.ibAllocated = m_Size;
        si.ibCommit = m_Size;
        si.ibReserved = m_Size;

        m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&si);
        if (m_SegmentHandle != nullptr)
        {
            m_pStart = static_cast<uint8_t*>(alloc);
            m_pCurrent = m_pStart;
            m_pCommited = m_pStart;
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

    if (m_pStart == nullptr)
    {
        // m_Size > 0 means we already tried to init and it failed.
        // so bail out to avoid doing Alloc again.
        if ((m_Size > 0) || !Initialize())
        {
            return nullptr;
        }
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
    m_pCurrent = obj + sizeof(ObjHeader) + objectSize;
    ZeroMemory(obj, objectSize); // is it needed?
    return reinterpret_cast<Object*>(obj + sizeof(ObjHeader));
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
