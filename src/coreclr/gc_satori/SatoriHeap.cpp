// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriHeap.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "../env/gcenv.ee.h"

#include "SatoriUtil.h"
#include "SatoriHeap.h"
#include "SatoriRegion.h"
#include "SatoriObject.h"
#include "SatoriPage.h"
#include "SatoriPage.inl"

int8_t* SatoriHeap::s_pageByteMap;

static void InitWriteBarrier(void* pageMap, void* pageByteMap, size_t highest_address)
{
    WriteBarrierParameters args = {};
    args.operation = WriteBarrierOp::Initialize;
    args.is_runtime_suspended = true;
    args.card_table = (uint32_t*)pageMap;
    args.card_bundle_table = (uint32_t*)pageByteMap;
    args.highest_address = (uint8_t*)highest_address;

    g_gc_card_table = args.card_table;
    g_gc_card_bundle_table = args.card_bundle_table;

    // dummy values to make asserts happy, we will not use this
    args.lowest_address = (uint8_t*)1;
    args.ephemeral_low = (uint8_t*)-1;
    args.ephemeral_high = (uint8_t*)-1;

    GCToEEInterface::StompWriteBarrier(&args);
}

static void UpdateWriteBarrier(void* pageMap, void* pageByteMap, size_t highest_address)
{
    WriteBarrierParameters args = {};
    args.operation = WriteBarrierOp::StompResize;
    args.is_runtime_suspended = false;
    args.card_table = (uint32_t*)pageMap;
    args.card_bundle_table = (uint32_t*)pageByteMap;
    args.highest_address = (uint8_t*)highest_address;

    g_gc_card_table = args.card_table;
    g_gc_card_bundle_table = args.card_bundle_table;

    // dummy values to make asserts happy, we will not use this
    args.lowest_address = (uint8_t*)1;
    args.ephemeral_low = (uint8_t*)-1;
    args.ephemeral_high = (uint8_t*)-1;

    GCToEEInterface::StompWriteBarrier(&args);
}

SatoriHeap* SatoriHeap::Create()
{
    const int mapSize = (1 << pageCountBits) * sizeof(SatoriPage*);
    size_t reserveSize = mapSize + offsetof(SatoriHeap, m_pageMap);
    reserveSize = ALIGN_UP(reserveSize, SatoriUtil::CommitGranularity());
    void* reserved = GCToOSInterface::VirtualReserve(nullptr, reserveSize, SatoriUtil::UseTHP());
    if (reserved == nullptr)
    {
        return nullptr;
    }

    size_t commitSize = offsetof(SatoriHeap, m_pageMap) + sizeof(SatoriPage*);
    commitSize = ALIGN_UP(commitSize, SatoriUtil::CommitGranularity());
    _ASSERTE(commitSize <= reserveSize);
    if (!GCToOSInterface::VirtualCommit(reserved, commitSize))
    {
        // failure
        return nullptr;
    }

    SatoriHeap* heap = (SatoriHeap*)reserved;
    SatoriHeap::s_pageByteMap = heap->m_pageByteMap;
    heap->m_reservedMapSize = mapSize;
    heap->m_committedMapSize = commitSize - offsetof(SatoriHeap, m_pageMap);
    heap->m_committedBytes = commitSize;
    InitWriteBarrier(heap->m_pageMap, s_pageByteMap, heap->CommittedMapLength() * Satori::PAGE_SIZE_GRANULARITY - 1);
    heap->m_mapLock.Initialize();
    heap->m_nextPageIndex = 1;
    heap->m_usedMapLength = 1;

    heap->m_allocator.Initialize(heap);
    heap->m_recycler.Initialize(heap);
    heap->m_finalizationQueue.Initialize(heap);
    return heap;
}

bool SatoriHeap::CommitMoreMap(size_t currentCommittedMapSize)
{
    SatoriLockHolder holder(&m_mapLock);
    if (currentCommittedMapSize == m_committedMapSize)
    {
        void* commitFrom = (void*)((size_t)&m_pageMap + currentCommittedMapSize);
        size_t commitSize = SatoriUtil::CommitGranularity();
        _ASSERTE(m_committedMapSize + commitSize <= m_reservedMapSize);
        if (GCToOSInterface::VirtualCommit(commitFrom, commitSize))
        {
            // we did the commit
            m_committedMapSize += commitSize;
            IncBytesCommitted(commitSize);
            UpdateWriteBarrier(m_pageMap, s_pageByteMap, CommittedMapLength() * Satori::PAGE_SIZE_GRANULARITY - 1);
        }
    }
    else
    {
        _ASSERTE(m_committedMapSize > currentCommittedMapSize);
    }

    // either we did commit or someone else did, otherwise this is a failure.
    return m_committedMapSize > currentCommittedMapSize;
}

bool SatoriHeap::TryAddRegularPage(SatoriPage*& newPage)
{
    size_t nextPageIndex = m_nextPageIndex;
    size_t maxIndex = m_reservedMapSize / sizeof(SatoriPage*);
    for (size_t i = nextPageIndex; i < maxIndex; i++)
    {
        size_t currentCommittedMapSize = m_committedMapSize;
        size_t requiredMapSize = (i + 1) * sizeof(SatoriPage*);
        if (requiredMapSize > currentCommittedMapSize && !CommitMoreMap(currentCommittedMapSize))
        {
            break;
        }

        if (m_pageMap[i] == 0)
        {
            size_t pageAddress = i << Satori::PAGE_BITS;
            newPage = SatoriPage::InitializeAt(pageAddress, Satori::PAGE_SIZE_GRANULARITY, this);
            if (newPage)
            {
                // SYNCRONIZATION:
                // A page map update must be seen by all threads before seeing objects allocated
                // in the new page, otherwise checked barriers may consider the objects not in the heap.
                //
                // If another thread checks if object is in heap, its read of the map element is dependent on object,
                // therefore the read will happen after the object is obtained.
                // Also the object must be published before other thread could see it, and publishing is a release.
                // Thus an ordinary write is ok even for weak memory cases.
                m_pageMap[i] = newPage;
                s_pageByteMap[i] = 1;

                // ensure the next is advanced to at least i + 1
                while ((nextPageIndex = m_nextPageIndex) < i + 1 &&
                    Interlocked::CompareExchange(&m_nextPageIndex, i + 1, nextPageIndex) != nextPageIndex);

                size_t usedMapLength;
                // ensure the m_usedMapLength is advanced to at least i + 1
                while ((usedMapLength = m_usedMapLength) < i + 1 &&
                    Interlocked::CompareExchange(&m_usedMapLength, i + 1, usedMapLength) != usedMapLength);

                return true;
            }
        }

        // check if someone else added a page
        if (m_nextPageIndex != nextPageIndex)
        {
            return true;
        }
    }

    // most likely OOM
    return false;
}

// it is harder to find a contiguous space for a large page
// also unlikely that we cross-use one if just comitted.
// we scan ahead and claim, but do not move m_nextPageIndex
// unless we claimed contiguously.
SatoriPage* SatoriHeap::AddLargePage(size_t minSize)
{
    // Add the overhead of card table (1/512).
    minSize += minSize / Satori::BYTES_PER_CARD_BYTE;
    size_t pageSize = ALIGN_UP(minSize, Satori::PAGE_SIZE_GRANULARITY);
    size_t mapMarkCount = pageSize / Satori::PAGE_SIZE_GRANULARITY;

    size_t maxIndex = m_reservedMapSize / sizeof(SatoriPage*);
    for (size_t i = m_nextPageIndex; i < maxIndex; i++)
    {
        size_t currentCommittedMapSize;
        size_t requiredMapSize = (i + mapMarkCount) * sizeof(SatoriPage*);
        while (requiredMapSize > ((currentCommittedMapSize = m_committedMapSize)))
        {
            if (!CommitMoreMap(currentCommittedMapSize))
            {
                return nullptr;
            }
        }

        if (m_pageMap[i] == 0)
        {
            size_t pageAddress = i << Satori::PAGE_BITS;
            SatoriPage* newPage = SatoriPage::InitializeAt(pageAddress, pageSize, this);
            if (newPage)
            {
                // mark the map, before an object can be allocated in the new page and
                // may be seen in a GC barrier
                for (size_t j = 0; j < mapMarkCount; j++)
                {
                    m_pageMap[i + j] = newPage;
                    s_pageByteMap[i + j] = 1;
                }

                size_t usedMapLength;
                // ensure the m_usedMapLength is advanced to at least i + mapMarkCount
                while ((usedMapLength = m_usedMapLength) < i + mapMarkCount &&
                    Interlocked::CompareExchange(&m_usedMapLength, i + mapMarkCount, usedMapLength) != usedMapLength);

                // advance next if contiguous
                // Note: it may become contiguous after we check, but it should be rare.
                //       advancing m_nextPageIndex is not needed for correctness, so we do not retry.
                if (i == m_nextPageIndex)
                {
                    Interlocked::CompareExchange(&m_nextPageIndex, i + mapMarkCount, i);
                }

                return newPage;
            }
        }
    }

    return nullptr;
}

SatoriRegion* SatoriHeap::RegionForAddressChecked(size_t address)
{
    SatoriPage* page = PageForAddressChecked(address);
    if (page)
    {
        return page->RegionForAddressChecked(address);
    }

    return nullptr;
}
