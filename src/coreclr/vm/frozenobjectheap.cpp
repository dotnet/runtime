// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "frozenobjectheap.h"
#include "memorypool.h"

FrozenObjectHeap::FrozenObjectHeap():
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_SizeCommited(0),
    m_Size(0),
    m_SegmentHandle(nullptr)
    COMMA_INDEBUG(m_ObjectsCount(0))
{
    m_PageSize = GetOsPageSize();
    m_Crst.Init(CrstFrozenObjectHeap, CRST_UNSAFE_COOPGC);
}

#define FOH_RESERVE_PAGES 1024 // e.g. reserve 4Mb of virtual memory
#define FOH_COMMIT_PAGES 32  // e.g. commit 128Kb chunks

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
    m_Size = m_PageSize * FOH_RESERVE_PAGES;

    _ASSERT(FOH_RESERVE_PAGES > FOH_COMMIT_PAGES);
    _ASSERT(FOH_RESERVE_PAGES % FOH_COMMIT_PAGES == 0);
    _ASSERT(m_PageSize > MIN_OBJECT_SIZE);
    _ASSERT(m_SegmentHandle == nullptr);
    _ASSERT(m_pStart == nullptr);

    void* alloc = ClrVirtualAllocAligned(nullptr, m_Size, MEM_RESERVE, PAGE_READWRITE, m_PageSize);
    if (alloc != nullptr)
    {
        // Commit FOH_COMMIT_PAGES pages in advance
        alloc = ClrVirtualAllocAligned(alloc, m_PageSize * FOH_COMMIT_PAGES, MEM_COMMIT, PAGE_READWRITE, m_PageSize);
    }

    if (alloc != nullptr)
    {
        segment_info si;
        si.pvMem = alloc;
        si.ibFirstObject = sizeof(ObjHeader);
        si.ibAllocated = si.ibFirstObject;
        si.ibCommit = m_PageSize * FOH_COMMIT_PAGES;
        si.ibReserved = m_Size;

        m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&si);
        if (m_SegmentHandle != nullptr)
        {
            m_pStart = static_cast<uint8_t*>(alloc);
            m_pCurrent = m_pStart;
            m_SizeCommited = si.ibCommit;
            INDEBUG(m_ObjectsCount = 0);
            ASSERT((intptr_t)m_pCurrent % DATA_ALIGNMENT == 0);
            return true;
        }

        // GC refused to register frozen segment (OOM?)
        ClrVirtualFree(m_pStart, 0, MEM_RELEASE);
        m_pStart = nullptr;
    }
    return false;
}


Object* FrozenObjectHeap::AllocateObject(size_t objectSize)
{
    // NOTE: objectSize is expected be the full size including header
    _ASSERT(objectSize >= MIN_OBJECT_SIZE);

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

    uint8_t* obj = m_pCurrent;
    if ((size_t)(m_pStart + m_Size) < (size_t)(obj + objectSize))
    {
        // heap is full
        return nullptr;
    }

    // Check if we need to commit a new chunk
    if ((size_t)(m_pStart + m_SizeCommited) < (size_t)(obj + objectSize))
    {
        if (ClrVirtualAllocAligned(m_pCurrent, m_PageSize * FOH_COMMIT_PAGES, MEM_COMMIT, PAGE_READWRITE, m_PageSize) == nullptr)
        {
            // We failed to commit a new chunk of the reserved memory
            return nullptr;
        }
        m_SizeCommited += m_PageSize * FOH_COMMIT_PAGES;
    }

    ZeroMemory(obj, objectSize);
    INDEBUG(m_ObjectsCount++);
    m_pCurrent = obj + objectSize;

    // Notify GC that we bumped the pointer
    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommited);

    // Skip object header
    return reinterpret_cast<Object*>(obj + sizeof(ObjHeader));
}
