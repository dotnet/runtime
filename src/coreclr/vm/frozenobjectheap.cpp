// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "frozenobjectheap.h"

// Default size to reserve for a frozen segment
#define FOH_SEGMENT_DEFAULT_SIZE (4 * 1024 * 1024)
// Size to commit on demand in that reserved space
#define FOH_COMMIT_SIZE (64 * 1024)

FrozenObjectHeapManager::FrozenObjectHeapManager():
    m_Crst(CrstFrozenObjectHeap, CRST_UNSAFE_COOPGC),
    m_CurrentSegment(nullptr)
{
}

// Allocates an object of the give size (including header) on a frozen segment.
// May return nullptr if object is too large (larger than FOH_COMMIT_SIZE)
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

    _ASSERT(type != nullptr);
    _ASSERT(FOH_COMMIT_SIZE >= MIN_OBJECT_SIZE);

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
        m_CurrentSegment = new FrozenObjectSegment(FOH_SEGMENT_DEFAULT_SIZE);
        m_FrozenSegments.Append(m_CurrentSegment);
        _ASSERT(m_CurrentSegment != nullptr);
    }

    Object* obj = m_CurrentSegment->TryAllocateObject(type, objectSize);

    // The only case where it can be null is when the current segment is full and we need
    // to create a new one
    if (obj == nullptr)
    {
        // Double the reserved size to reduce the number of frozen segments in apps with lots of frozen objects
        // Use the same size in case if prevSegmentSize*2 operation overflows.
        size_t prevSegmentSize = m_CurrentSegment->GetSize();
        m_CurrentSegment = new FrozenObjectSegment(max(prevSegmentSize, prevSegmentSize * 2));
        m_FrozenSegments.Append(m_CurrentSegment);

        // Try again
        obj = m_CurrentSegment->TryAllocateObject(type, objectSize);

        // This time it's not expected to be null
        _ASSERT(obj != nullptr);
    }
    return obj;
#endif // !FEATURE_BASICFREEZE
}

// Reserve sizeHint bytes of memory for the given frozen segment.
// The requested size can be be ignored in case of memory pressure and FOH_SEGMENT_DEFAULT_SIZE is used instead.
FrozenObjectSegment::FrozenObjectSegment(size_t sizeHint) :
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_SizeCommitted(0),
    m_Size(sizeHint),
    m_SegmentHandle(nullptr)
    COMMA_INDEBUG(m_ObjectsCount(0))
{
    _ASSERT(m_Size > FOH_COMMIT_SIZE);
    _ASSERT(m_Size % FOH_COMMIT_SIZE == 0);

    void* alloc = ClrVirtualAlloc(nullptr, m_Size, MEM_RESERVE, PAGE_READWRITE);
    if (alloc == nullptr)
    {
        // Try again with the default FOH size
        if (m_Size > FOH_SEGMENT_DEFAULT_SIZE)
        {
            m_Size = FOH_SEGMENT_DEFAULT_SIZE;
            _ASSERT(m_Size > FOH_COMMIT_SIZE);
            _ASSERT(m_Size % FOH_COMMIT_SIZE == 0);
            alloc = ClrVirtualAlloc(nullptr, m_Size, MEM_RESERVE, PAGE_READWRITE);
        }

        if (alloc == nullptr)
        {
            ThrowOutOfMemory();
        }
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
    si.ibReserved = m_Size;

    m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&si);
    if (m_SegmentHandle == nullptr)
    {
        ClrVirtualFree(alloc, 0, MEM_RELEASE);
        ThrowOutOfMemory();
    }

    m_pStart = static_cast<uint8_t*>(committedAlloc);
    m_pCurrent = m_pStart + sizeof(ObjHeader);
    m_SizeCommitted = si.ibCommit;
    INDEBUG(m_ObjectsCount = 0);
    return;
}

Object* FrozenObjectSegment::TryAllocateObject(PTR_MethodTable type, size_t objectSize)
{
    _ASSERT(m_pStart != nullptr && m_Size > 0 && m_SegmentHandle != nullptr); // Expected to be inited
    _ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));
    _ASSERT(objectSize <= FOH_COMMIT_SIZE);
    _ASSERT(m_pCurrent >= m_pStart + sizeof(ObjHeader));

    const size_t spaceUsed = (size_t)(m_pCurrent - m_pStart);
    const size_t spaceLeft = m_Size - spaceUsed;

    _ASSERT(spaceUsed >= sizeof(ObjHeader));
    _ASSERT(spaceLeft >= sizeof(ObjHeader));

    // Test if we have a room for the given object (including extra sizeof(ObjHeader) for next object)
    if (spaceLeft - sizeof(ObjHeader) < objectSize)
    {
        return nullptr;
    }

    // Check if we need to commit a new chunk
    if (spaceUsed + objectSize + sizeof(ObjHeader) > m_SizeCommitted)
    {
        // Make sure we don't go out of bounds during this commit
        _ASSERT(m_SizeCommitted + FOH_COMMIT_SIZE <= m_Size);

        if (ClrVirtualAlloc(m_pStart + m_SizeCommitted, FOH_COMMIT_SIZE, MEM_COMMIT, PAGE_READWRITE) == nullptr)
        {
            ClrVirtualFree(m_pStart, 0, MEM_RELEASE);
            ThrowOutOfMemory();
        }
        m_SizeCommitted += FOH_COMMIT_SIZE;
    }

    INDEBUG(m_ObjectsCount++);

    Object* object = reinterpret_cast<Object*>(m_pCurrent);
    object->SetMethodTable(type);

    m_pCurrent += objectSize;

    // Notify GC that we bumped the pointer and, probably, committed more memory in the reserved part
    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommitted);

    return object;
}
