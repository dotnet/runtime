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
// SatoriFinalizationQueue.cpp
//

#include "common.h"
#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "../env/gcenv.interlocked.h"

#include "SatoriFinalizationQueue.h"
#include "SatoriObject.h"
#include "SatoriHeap.h"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"
#include "SatoriObject.inl"

// limit the queue size to some large value just to have a limit.
// needing this many items is likely an indication that finalizer is not keeping up and nothing can help that.
static const size_t MAX_SIZE = 1 << 25;

#if _DEBUG
// smaller size in debug to have overflows
static const size_t INITIAL_SIZE = 1 << 5;
#else
// 4K items (65Kb) - roughly the size of 2 region headers
static const size_t INITIAL_SIZE = 1 << 12;
#endif

void SatoriFinalizationQueue::Initialize(SatoriHeap* heap)
{
    m_enqueue = 0;
    m_dequeue = 0;
    m_scanTicket = 0;
    m_overflowedGen = 0;
    m_heap = heap;
    m_newData = nullptr;

    size_t size = INITIAL_SIZE;

    m_sizeMask = size - 1;
    size_t allocSize = size * sizeof(Entry);
    size_t regionSize = SatoriRegion::RegionSizeForAlloc(allocSize);
    SatoriRegion* region = m_heap->Allocator()->GetRegion(regionSize);
    m_data = (Entry*)region->Allocate(allocSize, /*zeroInitialize*/false);

    // format as empty
    for (size_t i = 0; i < size; i++)
    {
        m_data[i].version = i;
    }
}

int SatoriFinalizationQueue::OverflowedGen()
{
    return m_overflowedGen;
}

void SatoriFinalizationQueue::SetOverflow(int generation)
{
    if (generation > m_overflowedGen)
    {
        m_overflowedGen = generation;
    }
}

void SatoriFinalizationQueue::TryResizeIfOverflowing()
{
    if (!m_overflowedGen || m_newData != nullptr)
        return;

    // claim resizing
    if (Interlocked::CompareExchangePointer((void**)&m_newData, (void*)m_data, (void*)nullptr) != nullptr)
        return;

    // try resizing
    size_t size = (m_sizeMask + 1) * 2;
    if (size <= MAX_SIZE)
    {
        size_t allocSize = size * sizeof(Entry);
        size_t regionSize = SatoriRegion::RegionSizeForAlloc(allocSize);
        SatoriRegion* newRegion = m_heap->Allocator()->GetRegion(regionSize);
        if (newRegion)
        {
            newRegion->TryDecommit();
            Entry* newData = (Entry*)newRegion->Allocate(allocSize, /*zeroInitialize*/false);
            if (!newData)
            {
                m_heap->Allocator()->AddRegion(newRegion);
                return;
            }

            // format as empty
            for (size_t i = 0; i < size; i++)
            {
                newData[i].version = i;
            }

            m_newData = newData;
        }
    }
}

void SatoriFinalizationQueue::ResetOverflow(int generation)
{
    if (!m_overflowedGen || generation < m_overflowedGen)
    {
        // no overflow or a minor collection after a major overflow.
        // either way not our job to fix things up.
        return;
    }

    if (m_newData == m_data)
    {
        // someone claimed, but failed to resize.
        m_newData = nullptr;
    }

    if (m_newData == nullptr)
    {
        // we will be pending what last failed, try get more space.
        TryResizeIfOverflowing();
        if (m_newData == m_data)
        {
            m_newData = nullptr;
        }
    }

    // if there was a resize, move things over.
    if (m_newData != nullptr)
    {
        Entry* newData = m_newData;
        m_newData = nullptr;

        // transfer items from the old queue
        size_t dst = 0;
        for (size_t src = m_dequeue; src != m_enqueue; src++)
        {
            Entry* e = &newData[dst];
            e->value = m_data[src & m_sizeMask].value;
            _ASSERTE(e->version == dst);
            e->version = dst + 1;
            dst++;
        }

        SatoriRegion* oldRegion = ((SatoriObject*)m_data)->ContainingRegion();
        oldRegion->MakeBlank();
        m_heap->Allocator()->ReturnRegion(oldRegion);

        m_data = newData;
        size_t size = (m_sizeMask + 1) * 2;
        m_sizeMask = size - 1;
        m_enqueue = dst;
        m_dequeue = 0;
    }

    // we are not in an overflow unless we see a full queue again.
    m_overflowedGen = 0;
}

bool SatoriFinalizationQueue::TryUpdateScanTicket(int currentScanTicket)
{
    int queueScanTicket = VolatileLoadWithoutBarrier(&m_scanTicket);
    if (queueScanTicket != currentScanTicket)
    {
        // claim the queue for scanning
        if (Interlocked::CompareExchange(&m_scanTicket, currentScanTicket, queueScanTicket) == queueScanTicket)
        {
            return true;
        }
    }

    return false;
}


// We are using MPSC circular queue with item versioning inspired by the algorithm outlined at:
// http://www.1024cores.net/home/lock-free-algorithms/queues/bounded-mpmc-queue

// called by multiple producers concurrently with a consumer
bool SatoriFinalizationQueue::TryScheduleForFinalization(SatoriObject* finalizable)
{
    Entry* e;
    size_t enq = m_enqueue;
    size_t i = 0;
    for (;;)
    {
        e = &m_data[enq & m_sizeMask];
        size_t version = VolatileLoad(&e->version);
        ptrdiff_t diff = version - enq;

        if (diff == 0)
        {
            if (Interlocked::CompareExchange(&m_enqueue, enq + 1, enq) == enq)
            {
                break;
            }
        }
        else if (diff < 0)
        {
            return false;
        }

        i++;
        size_t lim = i * i;
        for (size_t j = 0; j < lim; j++)
        {
            YieldProcessor();
        }

        enq = m_enqueue;
    }

    e->value = finalizable;
    VolatileStore(&e->version, enq + 1);
    return true;
}

// called by multiple producers with no active consumers
bool SatoriFinalizationQueue::TryReserveSpace(size_t count, size_t* index)
{
    size_t deq = m_dequeue;
    size_t mask = m_sizeMask;
    size_t enq;

    do
    {
        enq = m_enqueue;
        if (enq + count - deq >= mask)
            return false; // overflow
    }
    while (Interlocked::CompareExchange(&m_enqueue, enq + count, enq) != enq);

    *index = enq;
    return true;
}

void SatoriFinalizationQueue::ScheduleForFinalizationAt(size_t index, SatoriObject* finalizable)
{
    Entry* e = &m_data[index & m_sizeMask];
    e->value = finalizable;
    _ASSERTE(e->version == index);
    e->version = index + 1;
}

// called by a single consuming thread with possibly active producers
SatoriObject* SatoriFinalizationQueue::TryGetNextItem()
{
    size_t deq = m_dequeue;
    Entry* e = &m_data[deq & m_sizeMask];

    size_t version = VolatileLoad(&e->version);
    SatoriObject* value = e->value;

    ptrdiff_t diff = version - (deq + 1);
    if (diff < 0)
    {
        value = nullptr;
    }
    else
    {
        // since there is only one dequeuer, it cannot fall behind another one.
        _ASSERTE(diff == 0);
        m_dequeue = deq + 1;
        VolatileStore(&e->version, deq + m_sizeMask + 1);
    }

    return value;
}

// returns whether the queue has items
// only makes sense in quiescent state
bool SatoriFinalizationQueue::HasItems()
{
    return m_enqueue != m_dequeue;
}

// returns current number of items
// only makes sense in quiescent state
size_t SatoriFinalizationQueue::Count()
{
    return (m_enqueue - m_dequeue) & m_sizeMask;
}
