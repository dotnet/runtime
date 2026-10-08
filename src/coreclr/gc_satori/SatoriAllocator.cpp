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
// SatoriAllocator.cpp
//

#include "common.h"
#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "../gceventstatus.h"

#include "SatoriUtil.h"
#include "SatoriHeap.h"
#include "SatoriObject.h"
#include "SatoriObject.inl"
#include "SatoriPage.h"
#include "SatoriAllocator.h"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"
#include "SatoriRegionQueue.h"
#include "SatoriAllocationContext.h"
#include "SatoriWorkChunk.h"
#include "SatoriWorkList.h"

void SatoriAllocator::Initialize(SatoriHeap* heap)
{
    m_heap = heap;

    for (int i = 0; i < Satori::ALLOCATOR_BUCKET_COUNT; i++)
    {
        m_queues[i] = SatoriRegionQueue::AllocAligned(QueueKind::Allocator);
    }

    m_workChunks = SatoriWorkList::AllocAligned();

    m_immortalRegion = nullptr;
    m_immortalAllocLock.Initialize();

    m_pinnedRegion = nullptr;
    m_pinnedAllocLock.Initialize();

    m_largeRegion = nullptr;
    m_largeAllocLock.Initialize();

    m_regularRegion = nullptr;
    m_regularAllocLock.Initialize();

    m_singePageAdders = 0;
}

SatoriRegion* SatoriAllocator::GetRegion(size_t regionSize)
{
    _ASSERTE(regionSize % Satori::REGION_SIZE_GRANULARITY == 0);
    int64_t newPageDeadline = 0;
    int newPageSpin = 0;

tryAgain:
    SatoriRegion* putBack = nullptr;

    int bucket = SizeToBucket(regionSize);

    SatoriRegion* region = (bucket == 0) ?
                            m_queues[bucket]->TryPop():
                            m_queues[bucket]->TryRemoveWithSize(regionSize, putBack);

    if (region)
    {
        if (putBack)
        {
            AddRegion(putBack);
        }

        return region;
    }

    while (++bucket < Satori::ALLOCATOR_BUCKET_COUNT)
    {
        region = m_queues[bucket]->TryPopWithSize(regionSize, putBack);
        if (region)
        {
            if (putBack)
            {
                // we took a bite out of a relatively large region
                // and noone could use it while we were taking our piece.
                // split the remaining portion in two so that two threads could take a piece next time.
                if (putBack->Size() > region->Size() * 2)
                {
                    SatoriRegion* half = putBack->TrySplit(ALIGN_DOWN((putBack->Size() / 2), Satori::REGION_SIZE_GRANULARITY));
                    if (half)
                    {
                        AddRegion(half);
                    }
                }

                AddRegion(putBack);
            }

            return region;
        }
    }

    // for regions < 1/2 page granularity, just allocate a regular page
    // this is the most common case.
    // for bigger ones, use more complex helper that may allocate a larger
    // page, if needed.
    if (regionSize < Satori::PAGE_SIZE_GRANULARITY / 2)
    {
        // Reserving a regular-sized Page.
        // We will often come here on multiple threads and we do not want all threads to reserve a page.
        // If someone else is reserving, we will allow 1 msec of retrying before reserving a page eagerly.
        if (newPageDeadline == 0)
        {
            newPageDeadline = minipal_hires_ticks() + minipal_hires_tick_frequency() / 1000;
        }

        if (m_singePageAdders != 0 ||
            Interlocked::CompareExchange(&m_singePageAdders, 1, 0) != 0)
        {
            // someone is adding.
            if (minipal_hires_ticks() - newPageDeadline > 0)
            {
                // timed out
                Interlocked::Increment(&m_singePageAdders);
            }
            else
            {
                newPageSpin = newPageSpin * 2 + 1;
                for (int i = 0; i < newPageSpin; i++)
                {
                    YieldProcessor();
                }

                goto tryAgain;
            }
        }

        SatoriPage* page = nullptr;
        if (m_heap->TryAddRegularPage(page))
        {
            if (page)
            {
                putBack = page->MakeInitialRegion();
                region = putBack->TrySplit(regionSize);
                AddRegion(putBack);
                Interlocked::Decrement(&m_singePageAdders);
                return region;
            }

            // Someone added a page, but not us. There could be more regions.
            Interlocked::Decrement(&m_singePageAdders);
            goto tryAgain;
        }
    }
    else
    {
        SatoriPage* page = m_heap->AddLargePage(regionSize);
        if (page)
        {
            region = page->MakeInitialRegion();
            _ASSERTE(region->Size() >= regionSize);
            if (region->Size() > regionSize)
            {
                putBack = region->TrySplit(region->Size() - regionSize);
                if (putBack)
                {
                    AddRegion(putBack);
                }
                else
                {
                    // OOM
                    AddRegion(region);
                    region = nullptr;
                }
            }

            return region;
        }
    }

    // most likely OOM
    return nullptr;
}

void SatoriAllocator::AddRegion(SatoriRegion* region)
{
    _ASSERTE(region->Generation() == -1);
    m_queues[SizeToBucket(region->Size())]->Enqueue(region);
}

// same as AddRegion, just puts it in front of the queue
// this is for recently used regions
void SatoriAllocator::ReturnRegion(SatoriRegion* region)
{
    _ASSERTE(region->IsAttachedToAllocatingOwner() == false);
    _ASSERTE(region->Generation() == -1);
    m_queues[SizeToBucket(region->Size())]->Push(region);
}

// can be used if no concurrent pops from the queue.
void SatoriAllocator::ReturnRegionNoLock(SatoriRegion* region)
{
    _ASSERTE(region->IsAttachedToAllocatingOwner() == false);
    _ASSERTE(region->Generation() == -1);
    _ASSERTE(m_heap->Recycler()->IsBlockingPhase());

    m_queues[SizeToBucket(region->Size())]->PushNoLock(region);
}

void SatoriAllocator::AllocationTickIncrement(AllocationTickKind allocationTickKind, size_t totalAdded, SatoriObject* obj, size_t objSize)
{
    size_t& tickAmout = allocationTickKind == AllocationTickKind::Small ?
        m_smallAllocTickAmount :
        allocationTickKind == AllocationTickKind::Large ?
            m_largeAllocTickAmount :
            m_pinnedAllocTickAmount;

    const size_t etw_allocation_tick = 100 * 1024;
    size_t current = Interlocked::ExchangeAdd64(&tickAmout, totalAdded) + totalAdded;

    if ((int64_t)current > etw_allocation_tick &&
        Interlocked::CompareExchange(&tickAmout, (size_t)0, current) == current)
    {
        FIRE_EVENT(GCAllocationTick_V4,
            current,
            /*kind*/ (uint32_t)allocationTickKind,
            /*heap_number*/ 0,
            (void*)obj,
            objSize);
    }
}

void SatoriAllocator::AllocationTickDecrement(size_t totalUnused)
{
    if (EVENT_ENABLED(GCAllocationTick_V4))
    {
        return;
    }

    Interlocked::ExchangeAdd64(&m_smallAllocTickAmount, (size_t)(-(ptrdiff_t)totalUnused));
}

Object* SatoriAllocator::Alloc(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    size = ALIGN_UP(size, Satori::OBJECT_ALIGNMENT);

    if ((flags & (GC_ALLOC_IMMORTAL | GC_ALLOC_PINNED_OBJECT_HEAP | GC_ALLOC_LARGE_OBJECT_HEAP)) == 0)
    {
        if (context->alloc_ptr + size <= context->alloc_limit)
        {
            SatoriObject* result = (SatoriObject*)context->alloc_ptr;
            if ((flags & GC_ALLOC_FINALIZE) && !result->ContainingRegion()->RegisterForFinalization(result))
            {
                result = nullptr;
            }

            context->alloc_ptr += size;
            return result;
        }
        else
        {
            return AllocRegular(context, size, flags);
        }
    }

    if (flags & GC_ALLOC_PINNED_OBJECT_HEAP)
    {
        SatoriObject* result = AllocPinned(context, size, flags);
        if (result != nullptr)
        {
            result->SetUnmovable();
        }

        return result;
    }

    if (flags & GC_ALLOC_LARGE_OBJECT_HEAP)
    {
        return AllocLarge(context, size, flags);
    }

    if (flags & GC_ALLOC_IMMORTAL)
    {
        return AllocImmortal(context, size, flags);
    }

    return nullptr;
}

#ifdef _DEBUG
// allow more frequent allocs from shared in debug
const size_t minSharedAllocDelay = 5;
#else
// This is how often would we want to take a lock in usecs for 2k or more.
// If we come for more sooner, arrange an own region.
// Shared is for slow allocators. This is basically the threshold for slow.
const size_t minSharedAllocDelay = 50;
#endif

thread_local
size_t t_lastAllocBytes;

thread_local
size_t t_lastAllocBytesAtGCcheck;

thread_local
struct AllocStats
{
    size_t lastRegularAllocBytes;
    size_t lastAllocRecordUsec;
    // in byte/microsecond, same as MB/sec
    size_t regularAllocRate;
} t_allocStats;

void SatoriAllocator::UpdateAllocStatsAndHelpIfNeeded(SatoriAllocationContext* context)
{
    size_t curAllocBytes = context->alloc_bytes + context->alloc_bytes_uoh;
    if (curAllocBytes == 0)
    {
        m_heap->Recycler()->MaybeTriggerGC(gc_reason::reason_alloc_soh);
        return;
    }

    size_t lastAllocBytes = t_lastAllocBytes;
    // sometimes we release bytes back, wait until we go forward.
    if (curAllocBytes < lastAllocBytes)
        return;

    size_t change = curAllocBytes - lastAllocBytes;
    if (change < Satori::PACE_BUDGET)
    {
        // too soon. 
        return;
    }

    if (curAllocBytes - t_lastAllocBytesAtGCcheck >= Satori::REGION_SIZE_GRANULARITY)
    {
        m_heap->Recycler()->MaybeTriggerGC(gc_reason::reason_alloc_soh);
        t_lastAllocBytesAtGCcheck = curAllocBytes;
    }
    else
    {
        m_heap->Recycler()->HelpOnce();
    }

    t_lastAllocBytes = curAllocBytes;

    // This is after possible helping or doing GC, but since this is sampling
    // of overal alloc rate it does not matter if we take time before or after.
    size_t curUsec = m_heap->Recycler()->GetNowUsecs();
    size_t curRegularAllocBytes = context->alloc_bytes;
    if (curRegularAllocBytes > t_allocStats.lastRegularAllocBytes &&
        curUsec > t_allocStats.lastAllocRecordUsec)
    {
        t_allocStats.regularAllocRate =
            (curRegularAllocBytes - t_allocStats.lastRegularAllocBytes) /
            (curUsec - t_allocStats.lastAllocRecordUsec);
        t_allocStats.lastRegularAllocBytes = curRegularAllocBytes;
        t_allocStats.lastAllocRecordUsec = curUsec; 
    }
}

SatoriObject* SatoriAllocator::AllocRegular(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    // when allocations cross certain thresholds, check if GC should start or help is needed.
    UpdateAllocStatsAndHelpIfNeeded(context);

tryAgain: 

    if (!context->RegularRegion())
    {
        // If was allocating from shared, close the alloc.
        // If we continue from the same shared region, we will return the remainder, once we have the lock.
        // If not we will just leave the free obj unreturned. It is probably too small anyways.
        SatoriObject* freeObj = context->alloc_ptr != 0 ?
            context->FinishAllocFromShared() : nullptr;

        // TUNING: may be 10 or 50 ? Is it important?
        // we will consider 100+ MB/s as fast-allocating and try getting a non-shared region
        if (t_allocStats.regularAllocRate < 100)
        {
            //m_regularAllocLock.Enter();
            if (m_regularAllocLock.TryEnter())
            {
                if (freeObj && freeObj->ContainingRegion() == m_regularRegion)
                {
                    size_t size = freeObj->FreeObjSize();
                    m_regularRegion->SetOccupancy(m_regularRegion->Occupancy() - size);
                    m_regularRegion->ReturnFreeSpace(freeObj, size);
                }

                return AllocRegularShared(context, size, flags);
            }
        }
    }

    SatoriRegion* region = context->RegularRegion();
    _ASSERTE(region == nullptr || region->IsAttachedToAllocatingOwner());

    while (true)
    {
        if (region != nullptr)
        {
            _ASSERTE((size_t)context->alloc_limit == region->GetAllocStart());
            size_t moreSpace = context->alloc_ptr + size - context->alloc_limit;

            // try allocate more to form a contiguous chunk to fit size
            size_t allocRemaining = region->GetAllocRemaining();
            if (moreSpace <= allocRemaining)
            {
                bool zeroInitialize = !(flags & GC_ALLOC_ZEROING_OPTIONAL);
                if (zeroInitialize)
                {
                    // We do not want to pre-zero extra bytes if they may not fit the next allocation,
                    // but if we will have more than LARGE_OBJECT_THRESHOLD remaining, everything will fit.
                    // In such case we will zero out extra.
                    if (moreSpace + Satori::LARGE_OBJECT_THRESHOLD < allocRemaining && 
                        moreSpace < SatoriUtil::MinZeroInitSize())
                    {
                        moreSpace = min(SatoriUtil::MinZeroInitSize(), allocRemaining - Satori::LARGE_OBJECT_THRESHOLD);
                    }

                    // " +/- sizeof(size_t)" here is to intentionally misalign alloc_limit on the index granularity
                    // to improve chances that the object that is allocated here will be indexed
                    size_t misAlignedOnIndexEnd = ALIGN_UP(region->GetAllocStart() + moreSpace + sizeof(size_t), Satori::INDEX_GRANULARITY) - sizeof(size_t);
                    size_t misAlignedMoreSpace = misAlignedOnIndexEnd - region->GetAllocStart();

                    if (misAlignedMoreSpace <= allocRemaining)
                    {
                        moreSpace = misAlignedMoreSpace;
                    }
                }

                if (region->Allocate(moreSpace, zeroInitialize))
                {
                    context->alloc_limit += moreSpace;
                    context->alloc_bytes += moreSpace;

                    SatoriObject* result = (SatoriObject*)(size_t)context->alloc_ptr;
                    if ((flags & GC_ALLOC_FINALIZE) &&
                        !region->RegisterForFinalization(result))
                    {
                        return nullptr;
                    }

                    context->alloc_ptr += size;
                    result->CleanSyncBlock();
                    region->SetIndicesForObject(result, result->Start() + size);

                    if (EVENT_ENABLED(GCAllocationTick_V4))
                    {
                        AllocationTickIncrement(AllocationTickKind::Small, moreSpace, result, size);
                    }

                    return result;
                }
                else
                {
                    //OOM
                    return nullptr;
                }
            }

            // unclaim unused.
            size_t unused = context->alloc_limit - context->alloc_ptr;
            context->alloc_bytes -= unused;
            AllocationTickDecrement(unused);

            if (region->IsAllocating())
            {
                region->StopAllocating((size_t)context->alloc_ptr);
            }
            else
            {
                _ASSERTE(context->alloc_ptr == nullptr);
            }

            // try get from the free list
            if (region->StartAllocating(size))
            {
                // we have enough free space in the region to continue
                context->alloc_ptr = context->alloc_limit = (uint8_t*)region->GetAllocStart();
                continue;
            }

            if (region->IsEscapeTracking())
            {
                // try performing thread local collection and see if we have enough space after that.
                if (region->ThreadLocalCollect(context->alloc_bytes))
                {
                    if (region->StartAllocating(size))
                    {
                        // we have enough free space in the region to continue
                        context->alloc_ptr = context->alloc_limit = (uint8_t*)region->GetAllocStart();
                        continue;
                    }
                }
            }

            context->alloc_ptr = context->alloc_limit = nullptr;
            region->DetachFromAlocatingOwnerRelease();
            m_heap->Recycler()->AddEphemeralRegion(region);

            // could not allocate, start over with trying to get a region
            goto tryAgain;
        }

        TryGetRegularRegion(region);

        if (region == nullptr)
        {
            //OOM
            return nullptr;
        }

        // the order of assignments:
        // 1) Attach  (optional: set escape tag)
        // 2) Set Generation to 0 or 1
        // 3) Reset ReusableFor
        // <objects allocated>
        // 4) (optional: clear escape tag) Detach       

        region->AttachToAllocatingOwner(&context->RegularRegion());
        if (SatoriUtil::IsGen0Enabled())
        {
            switch (region->ReusableFor())
            {
            case SatoriRegion::ReuseLevel::Gen0:
                region->EscapeAll();
                goto fallthrough;
            case SatoriRegion::ReuseLevel::None:
            fallthrough:
                //NB: sets Generation to 0
                region->StartEscapeTrackingRelease(SatoriUtil::GetCurrentThreadTag());
                break;
            case SatoriRegion::ReuseLevel::Gen1:
                region->SetGenerationRelease(1);
                break;
            }
        }
        else
        {
            region->SetGenerationRelease(1);
        }

        region->ResetReusableForRelease();
        context->alloc_ptr = context->alloc_limit = (uint8_t*)region->GetAllocStart();
    }
}

SatoriObject* SatoriAllocator::AllocRegularShared(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    SatoriRegion* region = m_regularRegion;
    _ASSERTE(region == nullptr || region->IsAttachedToAllocatingOwner());

    while (true)
    {
        if (region != nullptr)
        {
            // get extra MIN_FREE_SIZE as other threads will allocate after us
            // and when we are done we need a parseability separator.
            size_t moreSpace = size + Satori::MIN_FREE_SIZE;
            size_t allocRemaining = region->GetAllocRemaining();
            if (allocRemaining >= moreSpace)
            {
                // we have enough free space in the region to continue
                bool zeroInitialize = !(flags & GC_ALLOC_ZEROING_OPTIONAL);
                if (zeroInitialize)
                {
                    // We do not want to pre-zero too much in shared case.
                    // Just aligning on index is good enough.
                    // Also we do not want the threads to hold on to unfinished allocs for too long.

                    // " +/- sizeof(size_t)" here is to intentionally misalign alloc_limit on the index granularity
                    // to improve chances that the object that is allocated here will be indexed
                    size_t misAlignedOnIndexEnd = ALIGN_UP(region->GetAllocStart() + moreSpace + sizeof(size_t), Satori::INDEX_GRANULARITY) - sizeof(size_t);
                    size_t misAlignedMoreSpace = misAlignedOnIndexEnd - region->GetAllocStart();

                    if (misAlignedMoreSpace <= allocRemaining)
                    {
                        moreSpace = misAlignedMoreSpace;
                    }
                    else
                    {
                        // In nonshared we can pick the tail with single object allocs.
                        // In shared, just pick the tail. We do not want allocs in shared that are too small.
                        moreSpace = allocRemaining;
                    }
                }

                // do not zero-initialize just yet, we will do that after leaving the lock.
                SatoriObject* result = (SatoriObject*)region->Allocate(moreSpace, /*zeroInitialize*/ false);
                if (!result)
                {
                    //OOM, nothing to undo.
                    m_regularAllocLock.Leave();
                    return nullptr;
                }

                if (flags & GC_ALLOC_FINALIZE)
                {
                    if (!region->RegisterForFinalization(result))
                    {
                        // OOM, undo the allocation
                        region->StopAllocating(result->Start());
                        m_regularAllocLock.Leave();
                        return nullptr;
                    }
                }

                // Conservatively assume the region may get finalizable objects.
                // This is for initial placement into queues, which may happen before
                // all objects in the buffer are allocated.
                // It is ok to be conservative, also this will self-correct after sweeping.
                region->SetHasFinalizables();

                // region stays unparsable until "moreSpace" is consumed.
                region->IncrementUnfinishedAlloc();

                // done with region modifications.
                m_regularAllocLock.Leave();

                context->alloc_ptr = (uint8_t*)result + size;
                context->alloc_bytes += moreSpace;
                // leave some space at the end for the free object
                moreSpace -= Satori::MIN_FREE_SIZE;
                context->alloc_limit = (uint8_t*)result + moreSpace;

                result->CleanSyncBlock();
                region->SetIndicesForObject(result, result->Start() + size);
                if (zeroInitialize)
                {
                    memset((uint8_t*)result + sizeof(size_t), 0, moreSpace - 2 * sizeof(size_t));
                }

                if (EVENT_ENABLED(GCAllocationTick_V4))
                {
                    AllocationTickIncrement(AllocationTickKind::Small, moreSpace, result, size);
                }

                return result;
            }

            if (region->IsAllocating())
            {
                region->StopAllocating();
            }

            // try get from the free list
            size_t desiredFreeSpace = size + Satori::MIN_FREE_SIZE;
            if (region->StartAllocating(desiredFreeSpace))
            {
                // we have enough free space in the region to continue
                continue;
            }

            region->DetachFromAlocatingOwnerRelease();
            m_heap->Recycler()->AddEphemeralRegion(region);
        }

        TryGetRegularRegion(region);
        if (region == nullptr)
        {
            //OOM
            return nullptr;
        }

        region->TryCommit();

        region->AttachToAllocatingOwner(&m_regularRegion);
        region->SetGenerationRelease(1);
        region->ResetReusableForRelease();
    }
}

void SatoriAllocator::TryGetRegularRegion(SatoriRegion*& region)
{
    // opportunistically try get the next region, if available
    SatoriRegion* next = nullptr;
    SatoriQueue<SatoriRegion>* nextContainingQueue = nullptr;

    if (region)
    {
        next = region->NextInPage();
        if (next)
        {
            nextContainingQueue = next->ContainingQueue();
            if (nextContainingQueue &&
                nextContainingQueue->Kind() == QueueKind::RecyclerReusable &&
                nextContainingQueue->TryRemove(next))
            {
                region = next;
                size_t occupancy = region->Occupancy();
                _ASSERTE(occupancy < Satori::REGION_SIZE_GRANULARITY);
                region->OccupancyAtReuse() = (int32_t)occupancy;
                return;
            }
        }
    }

    region = m_heap->Recycler()->TryGetReusable();

    if (region == nullptr)
    {
        if (nextContainingQueue &&
            nextContainingQueue->Kind() == QueueKind::Allocator &&
            next->Size() == Satori::REGION_SIZE_GRANULARITY &&
            nextContainingQueue->TryRemove(next))
        {
            if (next->Size() != Satori::REGION_SIZE_GRANULARITY)
            {
                ReturnRegion(next);
            }
            else
            {
                region = next;
                return;
            }
        }

        region = GetRegion(Satori::REGION_SIZE_GRANULARITY);
        _ASSERTE(region == nullptr || region->NothingMarked());
    }
}

SatoriObject* SatoriAllocator::AllocLarge(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    // handle huge allocation. It can't be from the current region.
    if (size > SatoriRegion::MAX_LARGE_OBJ_SIZE)
    {
        return AllocHuge(context, size, flags);
    }

    // when allocations cross certain thresholds, check if GC should start or help is needed.
    UpdateAllocStatsAndHelpIfNeeded(context);

tryAgain:

    if (!context->LargeRegion() &&
        size < Satori::REGION_SIZE_GRANULARITY / 2)
    {
        //m_largeAllocLock.Enter();
        if (m_largeAllocLock.TryEnter())
        {
            return AllocLargeShared(context, size, flags);
        }
    }

    SatoriRegion* region = context->LargeRegion();
    while (true)
    {
        if (region)
        {
            size_t allocRemaining = region->GetAllocRemaining();
            if (allocRemaining >= size)
            {
                bool zeroInitialize = !(flags & GC_ALLOC_ZEROING_OPTIONAL);
                SatoriObject* result = (SatoriObject*)region->Allocate(size, zeroInitialize);
                if (!result)
                {
                    // OOM, nothing to undo
                    return nullptr;
                }

                if (flags & GC_ALLOC_FINALIZE)
                {
                    if (!region->RegisterForFinalization(result))
                    {
                        // OOM, undo allocation
                        region->StopAllocating(result->Start());
                        return nullptr;
                    }

                    region->SetHasFinalizables();
                }

                // success
                context->alloc_bytes_uoh += size;
                result->CleanSyncBlock();
                region->SetIndicesForObject(result, result->Start() + size);

                if (EVENT_ENABLED(GCAllocationTick_V4))
                {
                    AllocationTickIncrement(AllocationTickKind::Large, size, result, size);
                }

                return result;
            }
        }

        // check if existing region has space or drop it into recycler
        if (region)
        {
            if (region->IsAllocating())
            {
                region->StopAllocating();
            }

            // try get from the free list
            if (region->StartAllocating(size))
            {
                // we have enough free space in the region to continue
                continue;
            }

            region->DetachFromAlocatingOwnerRelease();
            m_heap->Recycler()->AddEphemeralRegion(region);

            // retry
            goto tryAgain;
        }

        // get a new regular region.
        _ASSERTE(SatoriRegion::RegionSizeForAlloc(size) == Satori::REGION_SIZE_GRANULARITY);

        region = nullptr;
        if (size < Satori::REGION_SIZE_GRANULARITY / 2)
        {
            region = m_heap->Recycler()->TryGetReusableForLarge();
        }

        if (!region)
        {
            region = GetRegion(Satori::REGION_SIZE_GRANULARITY);
            if (!region)
            {
                //OOM
                return (SatoriObject*)nullptr;
            }

            _ASSERTE(region->NothingMarked());
        }

        region->AttachToAllocatingOwner(&context->LargeRegion());
        region->SetGenerationRelease(1);
        region->ResetReusableForRelease();
    }
}

SatoriObject* SatoriAllocator::AllocLargeShared(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    _ASSERTE(flags & (GC_ALLOC_LARGE_OBJECT_HEAP | GC_ALLOC_PINNED_OBJECT_HEAP));

    SatoriRegion* region = m_largeRegion;
    _ASSERTE(region == nullptr || region->IsAttachedToAllocatingOwner());

    while (true)
    {
        if (region != nullptr)
        {
            if (region->GetAllocRemaining() >= size)
            {
                // do not zero-initialize just yet, we will do that after leaving the lock.
                SatoriObject* result = (SatoriObject*)region->Allocate(size, /*zeroInitialize*/ false);
                if (!result)
                {
                    //OOM, nothing to undo
                    m_largeAllocLock.Leave();
                    return nullptr;
                }

                if (flags & GC_ALLOC_FINALIZE)
                {
                    if (!region->RegisterForFinalization(result))
                    {
                        // OOM, undo the allocation
                        region->StopAllocating(result->Start());
                        m_largeAllocLock.Leave();
                        return nullptr;
                    }

                    region->SetHasFinalizables();
                }

                // region stays unparsable until allocation is complete.
                region->IncrementUnfinishedAlloc();
                // done with region modifications.
                m_largeAllocLock.Leave();

                context->alloc_bytes_uoh += size;
                result->CleanSyncBlockAndSetUnfinished();
                region->SetIndicesForObject(result, result->Start() + size);
                if (!(flags & GC_ALLOC_ZEROING_OPTIONAL))
                {
                    memset((uint8_t*)result + sizeof(size_t), 0, size - 2 * sizeof(size_t));
                }

                if (EVENT_ENABLED(GCAllocationTick_V4))
                {
                    AllocationTickIncrement(AllocationTickKind::Large, size, result, size);
                }

                return result;
            }

            if (region->IsAllocating())
            {
                region->StopAllocating();
            }

            // try get from the free list
            if (region->StartAllocating(size))
            {
                // we have enough free space in the region to continue
                continue;
            }

            region->DetachFromAlocatingOwnerRelease();
            m_heap->Recycler()->AddEphemeralRegion(region);
        }

        // get a new regular region.
        _ASSERTE(SatoriRegion::RegionSizeForAlloc(size) == Satori::REGION_SIZE_GRANULARITY);

        region = nullptr;
        if (size < Satori::REGION_SIZE_GRANULARITY / 2)
        {
            region = m_heap->Recycler()->TryGetReusableForLarge();
            _ASSERTE(region == nullptr || !region->IsAllocating());
        }

        if (!region)
        {
            region = GetRegion(Satori::REGION_SIZE_GRANULARITY);
            if (!region)
            {
                //OOM
                m_largeAllocLock.Leave();
                return nullptr;
            }

            _ASSERTE(region->NothingMarked());
        }

        region->TryCommit();

        region->AttachToAllocatingOwner(&m_largeRegion);
        region->SetGenerationRelease(1);
        region->ResetReusableForRelease();
    }
}

SatoriObject* SatoriAllocator::AllocHuge(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    // when allocations cross certain thresholds, check if GC should start or help is needed.
    UpdateAllocStatsAndHelpIfNeeded(context);

    size_t regionSize = SatoriRegion::RegionSizeForAlloc(size);
    _ASSERTE(regionSize > Satori::REGION_SIZE_GRANULARITY);
    SatoriRegion* hugeRegion = GetRegion(regionSize);
    if (!hugeRegion)
    {
        //OOM
        return nullptr;
    }

    _ASSERTE(hugeRegion->NothingMarked());
    bool zeroInitialize = !(flags & GC_ALLOC_ZEROING_OPTIONAL);
    SatoriObject* result = (SatoriObject*)hugeRegion->AllocateHuge(size, zeroInitialize);
    if (!result)
    {
        ReturnRegion(hugeRegion);
        // OOM
        return nullptr;
    }

    result->CleanSyncBlock();
    hugeRegion->SetIndicesForObject(result, hugeRegion->Start() + Satori::REGION_SIZE_GRANULARITY);

    // huge regions are not attached to contexts and in gen0+ would appear parseable,
    // but this one is not parseable yet since the new object has no MethodTable
    // we will keep the region in gen -1 for now and make it gen1 or gen2 in PublishObject.
    hugeRegion->StopAllocating();

    if ((flags & GC_ALLOC_FINALIZE) &&
        !hugeRegion->RegisterForFinalization(result))
    {
        hugeRegion->MakeBlank();
        ReturnRegion(hugeRegion);
        return nullptr;
    }

    context->alloc_bytes_uoh += size;
    if (EVENT_ENABLED(GCAllocationTick_V4))
    {
        AllocationTickIncrement(AllocationTickKind::Large, size, result, size);
    }

    return result;
}

SatoriObject* SatoriAllocator::AllocPinned(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    _ASSERTE(flags & GC_ALLOC_PINNED_OBJECT_HEAP);

    if (size > SatoriRegion::MAX_LARGE_OBJ_SIZE)
    {
        return AllocHuge(context, size, flags);
    }

    // if can't get a lock, let AllocLarge handle this.
    if (!m_pinnedAllocLock.TryEnter())
    {
        return AllocLarge(context, size, flags);
    }

    // when allocations cross certain thresholds, check if GC should start or help is needed.
    UpdateAllocStatsAndHelpIfNeeded(context);

    SatoriRegion* region = m_pinnedRegion;
    while (true)
    {
        if (region != nullptr)
        {
            if (region->GetAllocRemaining() >= size)
            {
                // do not zero-initialize just yet, we will do that after leaving the lock.
                SatoriObject* result = (SatoriObject*)region->Allocate(size, /*zeroInitialize*/ false);
                if (!result)
                {
                    //OOM, nothing to undo
                    m_pinnedAllocLock.Leave();
                    return nullptr;
                }

                if (flags & GC_ALLOC_FINALIZE)
                {
                    if (!region->RegisterForFinalization(result))
                    {
                        // OOM, undo the allocation
                        region->StopAllocating(result->Start());
                        m_pinnedAllocLock.Leave();
                        return nullptr;
                    }

                    region->SetHasFinalizables();
                }

                // region stays unparsable until allocation is complete.
                region->IncrementUnfinishedAlloc();
                // done with region modifications.
                m_pinnedAllocLock.Leave();

                context->alloc_bytes_uoh += size;
                result->CleanSyncBlockAndSetUnfinished();
                region->SetIndicesForObject(result, result->Start() + size);
                if (!(flags & GC_ALLOC_ZEROING_OPTIONAL))
                {
                    memset((uint8_t*)result + sizeof(size_t), 0, size - 2 * sizeof(size_t));
                }

                if (EVENT_ENABLED(GCAllocationTick_V4))
                {
                    AllocationTickIncrement(AllocationTickKind::Pinned, size, result, size);
                }

                return result;
            }

            if (region->IsAllocating())
            {
                region->StopAllocating();
            }

            // try get from the free list
            if (region->StartAllocating(size))
            {
                // we have enough free space in the region to continue
                continue;
            }

            region->DetachFromAlocatingOwnerRelease();
            m_heap->Recycler()->AddEphemeralRegion(region);
        }

        // get a new regular region.
        _ASSERTE(SatoriRegion::RegionSizeForAlloc(size) == Satori::REGION_SIZE_GRANULARITY);

        region = nullptr;
        if (size < Satori::REGION_SIZE_GRANULARITY / 2)
        {
            region = m_heap->Recycler()->TryGetReusableForLarge();
            _ASSERTE(region == nullptr || !region->IsAllocating());
        }

        if (!region)
        {
            region = GetRegion(Satori::REGION_SIZE_GRANULARITY);
            if (!region)
            {
                //OOM
                m_pinnedAllocLock.Leave();
                return nullptr;
            }

            _ASSERTE(region->NothingMarked());
        }

        region->TryCommit();

        region->AttachToAllocatingOwner(&m_pinnedRegion);
        region->SetGenerationRelease(1);
        region->ResetReusableForRelease();
    }
}

SatoriObject* SatoriAllocator::AllocImmortal(SatoriAllocationContext* context, size_t size, uint32_t flags)
{
    // immortal allocs should be way less than region size.
    _ASSERTE(size < Satori::REGION_SIZE_GRANULARITY / 2);

    SatoriLockHolder holder(&m_immortalAllocLock);
    SatoriRegion* region = m_immortalRegion;

    while (true)
    {
        if (region)
        {
            size_t allocRemaining = region->GetAllocRemaining();
            if (allocRemaining >= size)
            {
                bool zeroInitialize = !(flags & GC_ALLOC_ZEROING_OPTIONAL);
                SatoriObject* result = (SatoriObject*)region->Allocate(size, zeroInitialize);
                if (result)
                {
                    context->alloc_bytes_uoh += size;
                    region->SetIndicesForObject(result, result->Start() + size);

                    if (EVENT_ENABLED(GCAllocationTick_V4))
                    {
                        AllocationTickIncrement(AllocationTickKind::Pinned, size, result, size);
                    }
                }
                else
                {
                    // OOM
                }

                return result;
            }

            if (region->IsAllocating())
            {
                region->StopAllocating();
            }
            else
            {
                if (region->StartAllocating(size))
                {
                    // we have enough free space in the region to continue
                    continue;
                }
            }
        }

        // get a new regular region.
        region = GetRegion(Satori::REGION_SIZE_GRANULARITY);
        if (!region)
        {
            //OOM
            return nullptr;
        }

        _ASSERTE(region->NothingMarked());
        // Clear the dirty part of the region eagerly to not clear for each allocated (and typically small) object.
        // And, while at that, link the previous one in case we have a reason to iterate old regions.
        region->ZeroInitAndLink(m_immortalRegion);
        if (m_immortalRegion)
        {
            m_immortalRegion->DetachFromAlocatingOwnerRelease();
        }

        region->AttachToAllocatingOwner(&m_immortalRegion);
        region->SetGenerationRelease(3);
    }

    return nullptr;
}

void SatoriAllocator::DeactivateSharedRegion(SatoriRegion* region, bool promoteAllRegions)
{
    if (region)
    {
        if (region->IsAllocating())
        {
            region->StopAllocating();
        }

        if (promoteAllRegions)
        {
            region->DetachFromAlocatingOwnerRelease();
        }

        m_heap->Recycler()->AddEphemeralRegion(region);
    }
}

void SatoriAllocator::DeactivateSharedRegions(bool promoteAllRegions)
{
    SatoriRegion* immortalRegion = m_immortalRegion;
    if (immortalRegion && immortalRegion->IsAllocating())
    {
        immortalRegion->StopAllocating();
    }

    DeactivateSharedRegion(m_pinnedRegion, promoteAllRegions);
    DeactivateSharedRegion(m_largeRegion, promoteAllRegions);
    DeactivateSharedRegion(m_regularRegion, promoteAllRegions);
}

SatoriWorkChunk* SatoriAllocator::TryGetWorkChunk()
{
#if _DEBUG
    static int i = 0;
    // simulate low memory case once in a while
    // This is just to force more overflows. Otherwise they are very rare.
    if (i++ % 2 == 0)
    {
        return nullptr;
    }
#endif
    SatoriWorkChunk* chunk = m_workChunks->TryPop();

    while (!chunk && AddMoreWorkChunks())
    {
        chunk = m_workChunks->TryPop();
    }

    _ASSERTE(chunk->Count() == 0);
    return chunk;
}

// returns NULL only in OOM case
SatoriWorkChunk* SatoriAllocator::GetWorkChunk()
{
    SatoriWorkChunk* chunk = m_workChunks->TryPop();
    while (!chunk && AddMoreWorkChunks())
    {
        chunk = m_workChunks->TryPop();
    }

    _ASSERTE(chunk->Count() == 0);
    return chunk;
}

bool SatoriAllocator::AddMoreWorkChunks()
{
    SatoriRegion* region = GetRegion(Satori::REGION_SIZE_GRANULARITY);
    if (!region)
    {
        return false;
    }

    while (true)
    {
        size_t mem = region->Allocate(Satori::MARK_CHUNK_SIZE, /*zeroInitialize*/ false);
        if (!mem)
        {
            break;
        }

        SatoriWorkChunk* chunk = SatoriWorkChunk::InitializeAt(mem);
        m_workChunks->Push(chunk);
    }

    return true;
}

void SatoriAllocator::ReturnWorkChunk(SatoriWorkChunk* chunk)
{
    _ASSERTE(chunk->Count() == 0);
    m_workChunks->Push(chunk);
}
