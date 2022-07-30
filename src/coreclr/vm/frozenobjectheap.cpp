// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "frozenobjectheap.h"
#include "memorypool.h"

FrozenObjectHeap::FrozenObjectHeap():
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_CommitChunkSize(0),
    m_SizeCommitted(0),
    m_SizeReserved(0),
    m_SegmentHandle(nullptr)
    COMMA_INDEBUG(m_ObjectsCount(0))
{
    m_Crst.Init(CrstFrozenObjectHeap, CRST_UNSAFE_COOPGC);
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
    // For internal testing
    m_CommitChunkSize = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_FrozenSegmentCommitSize);
    m_SizeReserved = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_FrozenSegmentReserveSize);

    if (m_SizeReserved == 0)
    {
        // A way to disable FrozenObjectHeap
        return false;
    }

    _ASSERT(m_SizeReserved > m_CommitChunkSize);
    _ASSERT(m_SizeReserved % m_CommitChunkSize == 0);
    _ASSERT(m_SegmentHandle == nullptr);
    _ASSERT(m_pStart == nullptr);

    void* alloc = ClrVirtualAllocAligned(nullptr, m_SizeReserved, MEM_RESERVE, PAGE_READWRITE, DATA_ALIGNMENT);
    if (alloc != nullptr)
    {
        // Commit FOH_COMMIT_SIZE chunk in advance
        alloc = ClrVirtualAlloc(alloc, m_CommitChunkSize, MEM_COMMIT, PAGE_READWRITE);
    }

    if (alloc != nullptr)
    {
        segment_info si;
        si.pvMem = alloc;
        si.ibFirstObject = sizeof(ObjHeader);
        si.ibAllocated = si.ibFirstObject;
        si.ibCommit = m_CommitChunkSize;
        si.ibReserved = m_SizeReserved;

        m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&si);
        if (m_SegmentHandle != nullptr)
        {
            m_pStart = static_cast<uint8_t*>(alloc);
            m_pCurrent = m_pStart;
            m_SizeCommitted = si.ibCommit;
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

    if (m_pStart == nullptr)
    {
        // m_SizeReserved > 0 means we already tried to init and it failed.
        // so bail out to avoid doing Alloc again.
        if ((m_SizeReserved > 0) || !Initialize())
        {
            return nullptr;
        }
    }

    if (objectSize > m_CommitChunkSize)
    {
        // The current design doesn't allow object larger than FOH_COMMIT_CHUNK_SIZE and
        // since FrozenObjectHeap is just an optimization, let's not fill it with huge objects.
        return nullptr;
    }

    _ASSERT(m_pStart != nullptr);
    _ASSERT(m_SegmentHandle != nullptr);
    _ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));

    uint8_t* obj = m_pCurrent;
    if ((size_t)(m_pStart + m_SizeReserved) < (size_t)(obj + objectSize))
    {
        // heap is full, caller is expected to switch to other heaps
        // TODO: register a new frozen segment
        return nullptr;
    }

    // Check if we need to commit a new chunk
    if ((size_t)(m_pStart + m_SizeCommitted) < (size_t)(obj + objectSize))
    {
        if (ClrVirtualAlloc(m_pCurrent, m_CommitChunkSize, MEM_COMMIT, PAGE_READWRITE) == nullptr)
        {
            // We failed to commit a new chunk of the reserved memory
            return nullptr;
        }
        m_SizeCommitted += m_CommitChunkSize;
    }

    INDEBUG(m_ObjectsCount++);
    m_pCurrent = obj + objectSize;

    // Notify GC that we bumped the pointer and, probably, committed more memory in the reserved part
    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommitted);

    // Skip object header
    return reinterpret_cast<Object*>(obj + sizeof(ObjHeader));
}
