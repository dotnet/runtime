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
// SatoriRegionQueue.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "SatoriRegionQueue.h"
#include "SatoriLock.h"
#include "SatoriUtil.h"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"

SatoriRegion* SatoriRegionQueue::TryPopWithSize(size_t regionSize, SatoriRegion*& putBack)
{
    m_lock.Enter();

    SatoriRegion* result = m_head;
    if (result == nullptr)
    {
        m_lock.Leave();
        return nullptr;
    }

    _ASSERTE(result->ValidateBlank());
    _ASSERTE(result->Size() >= regionSize);

    if (result->Size() - SatoriUtil::RoundDownPwr2(result->Size()) > regionSize &&
        result->CanSplitWithoutCommit(regionSize))
    {
        // inplace case
        // split "size" and return as a new region
        size_t nextStart, nextCommitted, nextUsed;
        result->SplitCore(regionSize, nextStart, nextCommitted, nextUsed);
        SatoriPage* containgPage = result->ContainingPage();
        _ASSERTE(result->ValidateBlank());
        m_lock.Leave();

        result = SatoriRegion::InitializeAt(containgPage, nextStart, regionSize, nextCommitted, nextUsed);
        _ASSERTE(result);
        _ASSERTE(result->ValidateBlank());
        putBack = nullptr;
    }
    else 
    {
        // take out the result
        m_count--;
        result->m_containingQueue = nullptr;
        m_head = result->m_next;
        if (m_head == nullptr)
        {
            m_tail = nullptr;
        }
        else
        {
            m_head->m_prev = nullptr;
        }

        m_lock.Leave();

        _ASSERTE(result->m_prev == nullptr);
        result->m_next = nullptr;

        if (result->Size() > regionSize)
        {
            // if there is a diff, split what is needed and put the rest back to appropriate queue.
            putBack = result;
            result = putBack->TrySplit(regionSize);
        }
    }

    return result;
}

SatoriRegion* SatoriRegionQueue::TryRemoveWithSize(size_t regionSize, SatoriRegion*& putBack)
{
    m_lock.Enter();

    SatoriRegion* result = m_head;
    while (true)
    {
        if (result == nullptr)
        {
            m_lock.Leave();
            return nullptr;
        }

        if (result->Size() >= regionSize)
        {
            break;
        }

        result = result->m_next;
    }

    _ASSERTE(result->ValidateBlank());

    if (result->Size() - SatoriUtil::RoundDownPwr2(result->Size()) > regionSize &&
        result->CanSplitWithoutCommit(regionSize))
    {
        // inplace case
        // split "size" and return as a new region
        size_t nextStart, nextCommitted, nextUsed;
        result->SplitCore(regionSize, nextStart, nextCommitted, nextUsed);
        SatoriPage* containgPage = result->ContainingPage();
        _ASSERTE(result->ValidateBlank());
        m_lock.Leave();

        result = SatoriRegion::InitializeAt(containgPage, nextStart, regionSize, nextCommitted, nextUsed);
        _ASSERTE(result);
        _ASSERTE(result->ValidateBlank());
        putBack = nullptr;
    }
    else
    {
        // take out the result
        m_count--;
        result->m_containingQueue = nullptr;
        if (result->m_prev == nullptr)
        {
            m_head = result->m_next;
        }
        else
        {
            result->m_prev->m_next = result->m_next;
        }

        if (result->m_next == nullptr)
        {
            m_tail = result->m_prev;
        }
        else
        {
            result->m_next->m_prev = result->m_prev;
        }

        m_lock.Leave();

        result->m_prev = nullptr;
        result->m_next = nullptr;

        if (result->Size() > regionSize)
        {
             // if there is a diff, split what is needed and put the rest back to appropriate queue.
            putBack = result;
            result = putBack->TrySplit(regionSize);
        }
    }

    return result;
}

SatoriRegion* SatoriRegionQueue::TryDequeueIfHasFreeSpaceInTopBucket()
{
    if (!m_tail)
    {
        return nullptr;
    }

    m_lock.Enter();

    SatoriRegion* result = m_tail;
    if (result == nullptr || !result->HasFreeSpaceInTopBucket())
    {
        m_lock.Leave();
        return nullptr;
    }

    // take out the result
    m_count--;
    result->m_containingQueue = nullptr;
    m_tail = result->m_prev;
    if (m_tail == nullptr)
    {
        m_head = nullptr;
    }
    else
    {
        m_tail->m_next = nullptr;
    }

    m_lock.Leave();

    _ASSERTE(result->m_next == nullptr);
    result->m_prev = nullptr;
    return result;
}

SatoriRegionQueue* SatoriRegionQueue::AllocAligned(QueueKind kind)
{
    const size_t align =
#if defined(TARGET_AMD64)
        64;
#else
        128;
#endif

#ifdef _MSC_VER
    void* buffer = _aligned_malloc(sizeof(SatoriRegionQueue), align);
#else
    void* buffer = malloc(sizeof(SatoriRegionQueue) + align);
    buffer = (void*)ALIGN_UP((size_t)buffer, align);
#endif
    return new(buffer)SatoriRegionQueue(kind);
}

