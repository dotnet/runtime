// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "frozenobjectheap.h"

#ifndef DACCESS_COMPILE
// Default size to reserve for a frozen segment
#define FOH_SEGMENT_DEFAULT_SIZE (4 * 1024 * 1024)
// Size to commit on demand in that reserved space
#define FOH_COMMIT_SIZE (64 * 1024)

FrozenObjectHeapManager::FrozenObjectHeapManager():
    m_Crst(CrstFrozenObjectHeap, CRST_UNSAFE_COOPGC),
    m_FirstSegment(nullptr),
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

        if (m_FirstSegment == nullptr)
        {
            m_FirstSegment = new FrozenObjectSegment(FOH_SEGMENT_DEFAULT_SIZE);
            m_CurrentSegment = m_FirstSegment;
            _ASSERT(m_CurrentSegment->GetNextSegment() == nullptr);
        }

        obj = m_CurrentSegment->TryAllocateObject(type, objectSize);

        // The only case where it can be null is when the current segment is full and we need
        // to create a new one
        if (obj == nullptr)
        {
            // Double the reserved size to reduce the number of frozen segments in apps with lots of frozen objects
            // Use the same size in case if prevSegmentSize*2 operation overflows.
            const size_t prevSegmentSize = m_CurrentSegment->GetSize();

            m_CurrentSegment->m_NextSegment = new FrozenObjectSegment(max(prevSegmentSize, prevSegmentSize * 2));
            m_CurrentSegment = m_CurrentSegment->m_NextSegment;

            // Try again
            obj = m_CurrentSegment->TryAllocateObject(type, objectSize);

            // This time it's not expected to be null
            _ASSERT(obj != nullptr);
        }
    }
    if (publish)
    {
        PublishFrozenObject(obj);
    }

    return obj;
#endif // !FEATURE_BASICFREEZE
}

static void* ReserveMemory(size_t size)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // We have plenty of space in-range on X86/AMD64 so we can afford keeping
    // FOH segments there so e.g. JIT can use relocs for frozen objects.
    return ExecutableAllocator::Instance()->Reserve(size);
#else
    return ClrVirtualAlloc(nullptr, size, MEM_RESERVE, PAGE_READWRITE);
#endif
}

static void* CommitMemory(void* ptr, size_t size)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    return ExecutableAllocator::Instance()->Commit(ptr, size, /*isExecutable*/ false);
#else
    return ClrVirtualAlloc(ptr, size, MEM_COMMIT, PAGE_READWRITE);
#endif
}

static void ReleaseMemory(void* ptr)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    ExecutableAllocator::Instance()->Release(ptr);
#else
    ClrVirtualFree(ptr, 0, MEM_RELEASE);
#endif
}

// Reserve sizeHint bytes of memory for the given frozen segment.
// The requested size can be be ignored in case of memory pressure and FOH_SEGMENT_DEFAULT_SIZE is used instead.
FrozenObjectSegment::FrozenObjectSegment(size_t sizeHint) :
    m_NextSegment(nullptr),
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_SizeCommitted(0),
    m_Size(sizeHint),
    m_SegmentHandle(nullptr)
{
    _ASSERT(m_Size > FOH_COMMIT_SIZE);
    _ASSERT(m_Size % FOH_COMMIT_SIZE == 0);

    void* alloc = ReserveMemory(m_Size);
    if (alloc == nullptr)
    {
        // Try again with the default FOH size
        if (m_Size > FOH_SEGMENT_DEFAULT_SIZE)
        {
            m_Size = FOH_SEGMENT_DEFAULT_SIZE;
            _ASSERT(m_Size > FOH_COMMIT_SIZE);
            _ASSERT(m_Size % FOH_COMMIT_SIZE == 0);
            alloc = ReserveMemory(m_Size);
        }

        if (alloc == nullptr)
        {
            ThrowOutOfMemory();
        }
    }

    // Commit a chunk in advance
    void* committedAlloc = CommitMemory(alloc, FOH_COMMIT_SIZE);
    if (committedAlloc == nullptr)
    {
        ReleaseMemory(alloc);
        ThrowOutOfMemory();
    }

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
        ReleaseMemory(alloc);
        ThrowOutOfMemory();
    }

    m_pStart = static_cast<uint8_t*>(committedAlloc);
    m_pCurrent = m_pStart + sizeof(ObjHeader);
    m_SizeCommitted = si.ibCommit;
    return;
}

Object* FrozenObjectSegment::TryAllocateObject(PTR_MethodTable type, size_t objectSize)
{
    _ASSERT(m_pStart != nullptr && m_Size > 0 && m_SegmentHandle != nullptr); // Expected to be inited
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

        if (CommitMemory(m_pStart + m_SizeCommitted, FOH_COMMIT_SIZE) == nullptr)
        {
            ReleaseMemory(m_pStart);
            ThrowOutOfMemory();
        }
        m_SizeCommitted += FOH_COMMIT_SIZE;
    }

    Object* object = reinterpret_cast<Object*>(m_pCurrent);
    object->SetMethodTable(type);

    m_pCurrent += objectSize;

    // Notify GC that we bumped the pointer and, probably, committed more memory in the reserved part
    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommitted);

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
#else // !DACCESS_COMPILE
void FrozenObjectHeapManager::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    PTR_FrozenObjectSegment curr = m_FirstSegment;
    while (curr != nullptr)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(curr->m_pStart), curr->m_pCurrent - curr->m_pStart);
        curr = curr->m_NextSegment;
    }
}
#endif // DACCESS_COMPILE
