// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "frozenobjectheap.h"
#include "memorypool.h"


FrozenObjectHeapManager::FrozenObjectHeapManager():
    m_CurrentHeap(nullptr)
{
    m_Crst.Init(CrstFrozenObjectHeap, CRST_UNSAFE_COOPGC);
    m_HeapCommitChunkSize = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_FrozenSegmentCommitSize);
    m_HeapSize = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_FrozenSegmentReserveSize);
}

// Allocates an object of the give size (including header) on a frozen segment.
// May return nullptr in the following cases:
//   1) DOTNET_FrozenSegmentReserveSize is 0 (disabled)
//   2) Object is too large (large than DOTNET_FrozenSegmentCommitSize)
// in such cases caller is responsible to find a more appropriate heap to allocate it
Object* FrozenObjectHeapManager::AllocateObject(PTR_MethodTable type, size_t objectSize)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END

#ifndef FEATURE_BASICFREEZE
    return nullptr;
#endif

    CrstHolder ch(&m_Crst);

    // Quick way to disable Frozen Heaps
    if (m_HeapSize == 0)
    {
        return nullptr;
    }

    _ASSERT(type != nullptr);
    _ASSERT(m_HeapCommitChunkSize >= MIN_OBJECT_SIZE);
    _ASSERT(m_HeapSize > m_HeapCommitChunkSize);
    _ASSERT(m_HeapSize % m_HeapCommitChunkSize == 0);

    // NOTE: objectSize is expected be the full size including header
    _ASSERT(objectSize >= MIN_OBJECT_SIZE);

    if (objectSize > m_HeapCommitChunkSize)
    {
        // The current design doesn't allow object larger than DOTNET_FrozenSegmentCommitSize and
        // since FrozenObjectHeap is just an optimization, let's not fill it with huge objects.
        return nullptr;
    }

    if (m_CurrentHeap == nullptr)
    {
        // Create the first heap on first allocation
        m_CurrentHeap = new FrozenObjectHeap(m_HeapSize, m_HeapCommitChunkSize);
        m_FrozenHeaps.Append(m_CurrentHeap);
        _ASSERT(m_CurrentHeap != nullptr);
    }

    Object* obj = m_CurrentHeap->AllocateObject(type, objectSize);

    // The only case where it might be null is when the current heap is full and we need
    // to create a new one
    if (obj == nullptr)
    {
        m_CurrentHeap = new FrozenObjectHeap(m_HeapSize, m_HeapCommitChunkSize);
        m_FrozenHeaps.Append(m_CurrentHeap);

        // Try again
        obj = m_CurrentHeap->AllocateObject(type, objectSize);

        // This time it's not expected to be null
        _ASSERT(obj != nullptr);
    }
    return obj;
}


FrozenObjectHeap::FrozenObjectHeap(size_t reserveSize, size_t commitChunkSize):
    m_pStart(nullptr),
    m_pCurrent(nullptr),
    m_CommitChunkSize(0),
    m_SizeCommitted(0),
    m_SegmentHandle(nullptr)
    COMMA_INDEBUG(m_ObjectsCount(0))
{
    m_SizeReserved = reserveSize;
    m_CommitChunkSize = commitChunkSize;

    void* alloc = ClrVirtualAlloc(nullptr, m_SizeReserved, MEM_RESERVE, PAGE_READWRITE);
    if (alloc == nullptr)
    {
        ThrowOutOfMemory();
    }

    // Commit a chunk in advance
    alloc = ClrVirtualAlloc(alloc, m_CommitChunkSize, MEM_COMMIT, PAGE_READWRITE);
    if (alloc == nullptr)
    {
        ThrowOutOfMemory();
    }

    // ClrVirtualAlloc is expected to be PageSize-aligned so we can expect
    // DATA_ALIGNMENT alignment as well
    _ASSERT(IS_ALIGNED(alloc, DATA_ALIGNMENT));

    segment_info si;
    si.pvMem = alloc;
    si.ibFirstObject = sizeof(ObjHeader);
    si.ibAllocated = si.ibFirstObject;
    si.ibCommit = m_CommitChunkSize;
    si.ibReserved = m_SizeReserved;

    m_SegmentHandle = GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&si);
    if (m_SegmentHandle == nullptr)
    {
        ThrowOutOfMemory();
    }

    m_pStart = static_cast<uint8_t*>(alloc);
    m_pCurrent = m_pStart;
    m_SizeCommitted = si.ibCommit;
    INDEBUG(m_ObjectsCount = 0);
    return;
}

Object* FrozenObjectHeap::AllocateObject(PTR_MethodTable type, size_t objectSize)
{
    _ASSERT(m_pStart != nullptr && m_SizeReserved > 0 && m_SegmentHandle != nullptr); // Expected to be inited
    _ASSERT(IS_ALIGNED(m_pCurrent, DATA_ALIGNMENT));

    uint8_t* obj = m_pCurrent;
    if (reinterpret_cast<size_t>(m_pStart + m_SizeReserved) < reinterpret_cast<size_t>(obj + objectSize))
    {
        // heap is full
        return nullptr;
    }

    // Check if we need to commit a new chunk
    if (reinterpret_cast<size_t>(m_pStart + m_SizeCommitted) < reinterpret_cast<size_t>(obj + objectSize))
    {
        _ASSERT(m_SizeCommitted + m_CommitChunkSize <= m_SizeReserved);
        if (ClrVirtualAlloc(m_pStart + m_SizeCommitted, m_CommitChunkSize, MEM_COMMIT, PAGE_READWRITE) == nullptr)
        {
            ThrowOutOfMemory();
        }
        m_SizeCommitted += m_CommitChunkSize;
    }

    INDEBUG(m_ObjectsCount++);

    m_pCurrent = obj + objectSize;

    Object* object = reinterpret_cast<Object*>(obj + sizeof(ObjHeader));
    object->SetMethodTable(type);

    // Notify GC that we bumped the pointer and, probably, committed more memory in the reserved part
    GCHeapUtilities::GetGCHeap()->UpdateFrozenSegment(m_SegmentHandle, m_pCurrent, m_pStart + m_SizeCommitted);

    return object;
}
