// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "frozenobjectheap.h"

// Default size to reserve for a frozen segment
#define FOH_SEGMENT_DEFAULT_SIZE (4 * 1024 * 1024)
// Size to commit on demand in that reserved space
#define FOH_COMMIT_SIZE (64 * 1024)

FrozenObjectHeapManager::FrozenObjectHeapManager():
    // This lock is used in both COOP and PREEMP (by profiler) modes
    m_Crst(CrstFrozenObjectHeap, CRST_UNSAFE_ANYMODE),
    // This lock is used only in COOP mode
    m_SegmentRegistrationCrst(CrstFrozenObjectHeap, CRST_UNSAFE_COOPGC),
    m_CurrentSegment(nullptr)
{
}

// Allocates an object of the give size (including header) on a frozen segment.
// May return nullptr if object is too large (larger than FOH_COMMIT_SIZE)
// in such cases caller is responsible to find a more appropriate heap to allocate it
Object* FrozenObjectHeapManager::TryAllocateObject(PTR_MethodTable type, size_t objectSize, bool publish)
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

    Object* obj = nullptr;
    FrozenObjectSegment* currentSegment = nullptr;
    {
        CrstHolder ch(&m_Crst);

        _ASSERT(type != nullptr);
        _ASSERT(FOH_COMMIT_SIZE >= MIN_OBJECT_SIZE);

        // Currently we don't support frozen objects with special alignment requirements
        // TODO: We should also give up on arrays of doubles on 32-bit platforms.
        // (we currently never allocate them on frozen segments)
    #ifdef FEATURE_64BIT_ALIGNMENT
        if (type->RequiresAlign8())
        {
            // Align8 objects are not supported yet
            return nullptr;
        }
    #endif

        // NOTE: objectSize is expected be the full size including header
        _ASSERT(objectSize >= MIN_OBJECT_SIZE);

        if (objectSize > FOH_COMMIT_SIZE)
        {
            // The current design doesn't allow objects larger than FOH_COMMIT_SIZE and
            // since FrozenObjectHeap is just an optimization, let's not fill it with huge objects.
            return nullptr;
        }

        obj = m_CurrentSegment == nullptr ? nullptr : m_CurrentSegment->TryAllocateObject(type, objectSize);
        // obj is nullptr if the current segment is full or hasn't been allocated yet
        if (obj == nullptr)
        {
            size_t newSegmentSize = FOH_SEGMENT_DEFAULT_SIZE;
            if (m_CurrentSegment != nullptr)
            {
                // Double the reserved size to reduce the number of frozen segments in apps with lots of frozen objects
                // Use the same size in case if prevSegmentSize*2 operation overflows.
                const size_t prevSegmentSize = m_CurrentSegment->GetSize();
                newSegmentSize = max(prevSegmentSize, prevSegmentSize * 2);
            }

            m_CurrentSegment = new FrozenObjectSegment(newSegmentSize);
            m_FrozenSegments.Append(m_CurrentSegment);

            // Try again
            obj = m_CurrentSegment->TryAllocateObject(type, objectSize);

            // This time it's not expected to be null
            _ASSERT(obj != nullptr);
        }
        currentSegment = m_CurrentSegment;
    }

    // If the currently used segment hasn't been registered yet, do it now.
    // We do it under a new lock because the main one (m_Crst) can be used by Profiler in a GC's thread
    // and that might cause deadlocks since RegisterFrozenSegment may stuck on GC's lock.
    if (!currentSegment->IsRegistered())
    {
        CrstHolder regLock(&m_SegmentRegistrationCrst);

        // Double-checked locking
        if (!currentSegment->IsRegistered())
        {
            currentSegment->Register();
        }
    }

    if (publish)
    {
        PublishFrozenObject(obj);
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
    m_IsRegistered(false),
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

    m_pStart = static_cast<uint8_t*>(committedAlloc);
    m_pCurrent = m_pStart + sizeof(ObjHeader);
    m_SizeCommitted = FOH_COMMIT_SIZE;
    INDEBUG(m_ObjectsCount = 0);

    // ClrVirtualAlloc is expected to be PageSize-aligned so we can expect
    // DATA_ALIGNMENT alignment as well
    _ASSERT(IS_ALIGNED(committedAlloc, DATA_ALIGNMENT));
}

void FrozenObjectSegment::Register()
{
    // Caller is expected to make sure it's not registered twice
    _ASSERT(!IsRegistered());

    segment_info si;
    si.pvMem = m_pStart;
    si.ibFirstObject = sizeof(ObjHeader);
    si.ibAllocated = (size_t)m_pCurrent; // there can be multiple objects already allocated
    si.ibCommit = m_SizeCommitted;
    si.ibReserved = m_Size;

    // NOTE: RegisterFrozenSegment may take a GC lock inside.
    m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&si);
    if (m_SegmentHandle == nullptr)
    {
        ClrVirtualFree(m_pStart, 0, MEM_RELEASE);
        ThrowOutOfMemory();
    }
    VolatileStore(&m_IsRegistered, true);
}

Object* FrozenObjectSegment::TryAllocateObject(PTR_MethodTable type, size_t objectSize)
{
    _ASSERT((m_pStart != nullptr) && (m_Size > 0));
    _ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));
    _ASSERT(IS_ALIGNED(objectSize, DATA_ALIGNMENT));
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

    if (IsRegistered())
    {
        // Notify GC that we bumped the pointer and, probably, committed more memory in the reserved part
        // NOTE: UpdateFrozenSegment is not expected to take any lock inside
        GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommitted);
    }
    else
    {
        // The segment is not yet registered so the upcoming RegisterFrozenSegment
        // will let GC know about this object (and all others) as is.
    }

    return object;
}

Object* FrozenObjectSegment::GetFirstObject() const
{
    if (m_pStart + sizeof(ObjHeader) == m_pCurrent)
    {
        // Segment is empty
        return nullptr;
    }
    return reinterpret_cast<Object*>(m_pStart + sizeof(ObjHeader));
}

Object* FrozenObjectSegment::GetNextObject(Object* obj) const
{
    // Input must not be null and should be within the segment
    _ASSERT(obj != nullptr);
    _ASSERT((uint8_t*)obj >= m_pStart + sizeof(ObjHeader) && (uint8_t*)obj < m_pCurrent);

    // FOH doesn't support objects with non-DATA_ALIGNMENT alignment yet.
    uint8_t* nextObj = (reinterpret_cast<uint8_t*>(obj) + ALIGN_UP(obj->GetSize(), DATA_ALIGNMENT));
    if (nextObj < m_pCurrent)
    {
        return reinterpret_cast<Object*>(nextObj);
    }

    // Current object is the last one in the segment
    return nullptr;
}
