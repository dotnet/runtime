// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "frozenobjectheap.h"

// Size to reserve for a frozen segment
#define FOH_SEGMENT_SIZE (4 * 1024 * 1024)
// Size to commit on demand in that reserved space
#define FOH_COMMIT_SIZE (64 * 1024)

FrozenObjectHeapManager::FrozenObjectHeapManager():
    m_Crst(CrstFrozenObjectHeap, CRST_UNSAFE_COOPGC),
    m_CurrentSegment(nullptr),
    m_Enabled(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_UseFrozenObjectHeap) != 0)
{
}

// Allocates an object of the give size (including header) on a frozen segment.
// May return nullptr in the following cases:
//   1) DOTNET_UseFrozenObjectHeap is 0 (disabled)
//   2) Object is too large (large than FOH_COMMIT_SIZE)
// in such cases caller is responsible to find a more appropriate heap to allocate it
Object* FrozenObjectHeapManager::TryAllocateObject(PTR_MethodTable type, size_t objectSize)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END

#ifndef FEATURE_BASICFREEZE
    // GC is required to support frozen segments
    return nullptr;
#else // FEATURE_BASICFREEZE

    CrstHolder ch(&m_Crst);

    if (!m_Enabled)
    {
        // Disabled via DOTNET_UseFrozenObjectHeap=0
        return nullptr;
    }

    _ASSERT(type != nullptr);
    _ASSERT(FOH_COMMIT_SIZE >= MIN_OBJECT_SIZE);
    _ASSERT(FOH_SEGMENT_SIZE > FOH_COMMIT_SIZE);
    _ASSERT(FOH_SEGMENT_SIZE % FOH_COMMIT_SIZE == 0);

    // NOTE: objectSize is expected be the full size including header
    _ASSERT(objectSize >= MIN_OBJECT_SIZE);

    if (objectSize > FOH_COMMIT_SIZE)
    {
        // The current design doesn't allow objects larger than FOH_COMMIT_SIZE and
        // since FrozenObjectHeap is just an optimization, let's not fill it with huge objects.
        return nullptr;
    }

    if (m_CurrentSegment == nullptr)
    {
        // Create the first segment on first allocation
        m_CurrentSegment = new FrozenObjectSegment();
        m_FrozenSegments.Append(m_CurrentSegment);
        _ASSERT(m_CurrentSegment != nullptr);
    }

    Object* obj = m_CurrentSegment->TryAllocateObject(type, objectSize);

    // The only case where it can be null is when the current segment is full and we need
    // to create a new one
    if (obj == nullptr)
    {
        m_CurrentSegment = new FrozenObjectSegment();
        m_FrozenSegments.Append(m_CurrentSegment);

        // Try again
        obj = m_CurrentSegment->TryAllocateObject(type, objectSize);

        // This time it's not expected to be null
        _ASSERT(obj != nullptr);
    }
    return obj;
#endif // !FEATURE_BASICFREEZE
}


FrozenObjectSegment::FrozenObjectSegment():
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_SizeCommitted(0),
    m_SegmentHandle(nullptr)
    COMMA_INDEBUG(m_ObjectsCount(0))
{
    void* alloc = ClrVirtualAlloc(nullptr, FOH_SEGMENT_SIZE, MEM_RESERVE, PAGE_READWRITE);
    if (alloc == nullptr)
    {
        ThrowOutOfMemory();
    }

    // Commit a chunk in advance
    void* committedAlloc = ClrVirtualAlloc(alloc, FOH_COMMIT_SIZE, MEM_COMMIT, PAGE_READWRITE);
    if (committedAlloc == nullptr)
    {
        ClrVirtualFree(alloc, 0, MEM_RELEASE);
        ThrowOutOfMemory();
    }

    // ClrVirtualAlloc is expected to be PageSize-aligned so we can expect
    // DATA_ALIGNMENT alignment as well
    _ASSERT(IS_ALIGNED(committedAlloc, DATA_ALIGNMENT));

    segment_info si;
    si.pvMem = committedAlloc;
    si.ibFirstObject = sizeof(ObjHeader);
    si.ibAllocated = si.ibFirstObject;
    si.ibCommit = FOH_COMMIT_SIZE;
    si.ibReserved = FOH_SEGMENT_SIZE;

    m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&si);
    if (m_SegmentHandle == nullptr)
    {
        ClrVirtualFree(alloc, 0, MEM_RELEASE);
        ThrowOutOfMemory();
    }

    m_pStart = static_cast<uint8_t*>(committedAlloc);
    m_pCurrent = m_pStart;
    m_SizeCommitted = si.ibCommit;
    INDEBUG(m_ObjectsCount = 0);
    return;
}

Object* FrozenObjectSegment::TryAllocateObject(PTR_MethodTable type, size_t objectSize)
{
    _ASSERT(m_pStart != nullptr && FOH_SEGMENT_SIZE > 0 && m_SegmentHandle != nullptr); // Expected to be inited
    _ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));

    uint8_t* obj = m_pCurrent;
    if (reinterpret_cast<size_t>(m_pStart + FOH_SEGMENT_SIZE) < reinterpret_cast<size_t>(obj + objectSize))
    {
        // Segment is full
        return nullptr;
    }

    // Check if we need to commit a new chunk
    if (reinterpret_cast<size_t>(m_pStart + m_SizeCommitted) < reinterpret_cast<size_t>(obj + objectSize))
    {
        _ASSERT(m_SizeCommitted + FOH_COMMIT_SIZE <= FOH_SEGMENT_SIZE);
        if (ClrVirtualAlloc(m_pStart + m_SizeCommitted, FOH_COMMIT_SIZE, MEM_COMMIT, PAGE_READWRITE) == nullptr)
        {
            ClrVirtualFree(m_pStart, 0, MEM_RELEASE);
            ThrowOutOfMemory();
        }
        m_SizeCommitted += FOH_COMMIT_SIZE;
    }

    INDEBUG(m_ObjectsCount++);

    m_pCurrent = obj + objectSize;

    Object* object = reinterpret_cast<Object*>(obj + sizeof(ObjHeader));
    object->SetMethodTable(type);

    // Notify GC that we bumped the pointer and, probably, committed more memory in the reserved part
    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommitted);

    return object;
}
