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
// SatoriRecycler.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "../gceventstatus.h"

// TODO: fixme
#include "../../inc/enum_class_flags.h"

#include "SatoriHandlePartitioner.h"
#include "SatoriHeap.h"
#include "SatoriTrimmer.h"
#include "SatoriPage.h"
#include "SatoriPage.inl"
#include "SatoriRecycler.h"
#include "SatoriObject.h"
#include "SatoriObject.inl"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"
#include "SatoriWorkChunk.h"
#include "SatoriAllocationContext.h"
#include "SatoriFinalizationQueue.h"
#include "../gcscan.h"

#include <minipal/memorybarrierprocesswide.h>

//#pragma optimize("", off)

#ifdef memcpy
#undef memcpy
#endif //memcpy

static const int MIN_GEN1_BUDGET = 2 * Satori::REGION_SIZE_GRANULARITY;

extern "C" uint8_t *g_write_barrier_state = 0;

void ToggleWriteBarrier(bool concurrent, bool skipCards, bool eeSuspended)
{
    WriteBarrierParameters args = {};

    args.operation = WriteBarrierOp::StompCustom;
    args.parameters_mask = WriteBarrierParametersMask::WriteWatchTable | WriteBarrierParametersMask::IsRuntimeSuspended | WriteBarrierParametersMask::SwWwEnabledForGcHeap;

    if (concurrent)
    {
        args.write_watch_table = (uint8_t*)1; // barrier state --> concurrent
        args.sw_ww_enabled_for_gc_heap = true;
    }
    else
    {
        args.write_watch_table = skipCards ? (uint8_t*)2 : (uint8_t*)0;
        args.sw_ww_enabled_for_gc_heap = false;
    }

    g_write_barrier_state = args.write_watch_table;

    args.is_runtime_suspended = eeSuspended;
    GCToEEInterface::StompWriteBarrier(&args);

    if (concurrent && !eeSuspended)
    {
        // If runtime is not suspended, force all threads to see the changed state before observing future allocations.
        minipal_memory_barrier_process_wide();
    }    
}

void SatoriRecycler::Initialize(SatoriHeap* heap)
{
    m_workerGate = new (nothrow) SatoriGate();
    m_gateSignaled = 0;
    m_workerWoken = 0;
    m_activeWorkers= 0;
    m_totalWorkers = 0;

    m_noWorkSince = 0;

    m_perfCounterTicksPerMilli = minipal_hires_tick_frequency() / 1000;
    m_perfCounterTicksPerMicro = minipal_hires_tick_frequency() / 1000000;

    m_heap = heap;
    m_trimmer = new (nothrow) SatoriTrimmer(heap);

    m_ephemeralRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerEphemeral);
    m_ephemeralFinalizationTrackingRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerEphemeralFinalizationTracking);
    m_tenuredRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerTenured);
    m_tenuredFinalizationTrackingRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerTenuredFinalizationTracking);

    m_finalizationPendingRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerFinalizationPending);

    m_stayingRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerStaying);
    m_relocatingRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerRelocating);
    m_relocatedRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerRelocated);
    m_relocatedToHigherGenRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerRelocatedToHigherGen);

    for (int i = 0; i < Satori::FREELIST_COUNT; i++)
    {
        m_relocationTargets[i] = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerRelocationTarget);
    }

    m_deferredSweepRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerDeferredSweep);
    m_deferredSweepCount = 0;
    m_gen1AddedSinceLastCollection = 0;
    m_gen2AddedSinceLastCollection = 0;

    m_reusableRegions = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerReusable);
    m_ephemeralWithUnmarkedDemoted = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerDemoted);
    m_reusableRegionsAlternate = SatoriRegionQueue::AllocAligned(QueueKind::RecyclerReusable);

    m_workList = SatoriWorkList::AllocAligned();
    m_gcState = GC_STATE_NONE;
    m_isBarrierConcurrent = false;

    m_gcCount[0] = m_gcCount[1] = m_gcCount[2] = 0;
    m_compactingGcCount[0] = m_compactingGcCount[1] = m_compactingGcCount[2] = 0;

    m_condemnedGeneration = 0;

    m_estimatedEphemeralReclaim = 0;
    m_estimatedTenuredReclaim = 0;
    m_promotionEstimate = 0;

    m_occupancy[0] = m_occupancyAcc[0] = 0;
    m_occupancy[1] = m_occupancyAcc[1] = 0;
    m_occupancy[2] = m_occupancyAcc[2] = 0;
    m_demotedOccupancyAcc = 0;
    m_occupancyReportingEnabled = false;

    m_gen1CountAtLastGen2 = 0;
    m_gen1Budget = MIN_GEN1_BUDGET;
    m_totalLimit = MIN_GEN1_BUDGET;
    m_nextGcIsFullGc = true;
    m_prevCondemnedGeneration = 2;
    m_gcNextTimeTarget = 0;

    m_activeWorkerFn = nullptr;
    m_rootScanTicket = 0;
    m_cardScanTicket = 0;
    m_concurrentCardsDone = true;
    m_concurrentCleaningState = CC_CLEAN_STATE_NOT_READY;
    m_concurrentHandlesDone = true;
    m_ccStackMarkingThreadsNum = 0;

    m_isLowLatencyMode = SatoriUtil::IsLowLatencyMode();

    for (int i = 0; i < 3; i++)
    {
        m_gcStartMillis[i] = m_gcDurationUsecs[i] = m_gcAccmulatingDurationUsecs[i] = 0;
    }

    m_lastEphemeralGcInfo = { 0 };
    m_lastTenuredGcInfo   = { 0 };
    m_CurrentGcInfo = nullptr;
    m_startMillis = GetNowMillis();
}

void SatoriRecycler::ShutDown()
{
    m_activeWorkerFn = nullptr;
}

/* static */
void SatoriRecycler::WorkerThreadMainLoop(void* param)
{
    SatoriRecycler* recycler = (SatoriRecycler*)param;
    Interlocked::Increment(&recycler->m_activeWorkers);

    for (;;)
    {
        Interlocked::Decrement(&recycler->m_activeWorkers);

        for (;;)
        {
            if (recycler->m_gateSignaled &&
               Interlocked::CompareExchange(&recycler->m_gateSignaled, 0, 1) == 1)
            {
                goto released;
            }

            // spin for ~10 microseconds (GcSpin)
            int64_t limit = minipal_hires_ticks() +
                recycler->m_perfCounterTicksPerMicro * SatoriUtil::GcSpin();

            int i = 0;
            do
            {
                i++;
                i = min(30, i);
                int j = (1 << i);
                while (--j > 0)
                {
                    YieldProcessor();

                    if (recycler->m_gateSignaled &&
                        Interlocked::CompareExchange(&recycler->m_gateSignaled, 0, 1) == 1)
                    {
                        goto released;
                    }
                }
            }
            while(minipal_hires_ticks() < limit);

            // Wait returns true if was woken up.
            if (!recycler->m_workerGate->TimedWait(10000))
            {
                // we timed out (very rare case)
                // we did not consume the wake, but there could be no more workers sleeping.
                Interlocked::Decrement(&recycler->m_totalWorkers);
                return;
            }

            // if there is a wake, clear it as it may no longer wake anything
            Interlocked::And(&recycler->m_workerWoken, 0);
        }

    released:
        Interlocked::Increment(&recycler->m_activeWorkers);

        auto activeWorkerFn = recycler->m_activeWorkerFn;
        if (activeWorkerFn)
        {
            (recycler->*activeWorkerFn)();
        }
    }
}

// not interlocked. this is not done concurrently. 
void SatoriRecycler::IncrementRootScanTicket()
{
    m_rootScanTicket++;
    // make sure the ticket is not 0
    if (!m_rootScanTicket)
    {
        m_rootScanTicket++;
    }
}

void SatoriRecycler::IncrementCardScanTicket()
{
    m_cardScanTicket++;
    // make sure the ticket is not 0
    if (!m_cardScanTicket)
    {
        m_cardScanTicket++;
    }
}

int SatoriRecycler::GetRootScanTicket()
{
    return m_rootScanTicket;
}

uint8_t SatoriRecycler::GetCardScanTicket()
{
    return m_cardScanTicket;
}

int64_t SatoriRecycler::GetCollectionCount(int gen)
{
    return m_gcCount[gen];
}

int SatoriRecycler::GetCondemnedGeneration()
{
    return m_condemnedGeneration;
}

size_t SatoriRecycler::Gen1RegionCount()
{
    return m_ephemeralFinalizationTrackingRegions->Count() +
        m_ephemeralRegions->Count() +
        m_ephemeralWithUnmarkedDemoted->Count();
}

size_t SatoriRecycler::Gen2RegionCount()
{
    return m_tenuredFinalizationTrackingRegions->Count() + m_tenuredRegions->Count();
}

size_t SatoriRecycler::RegionCount()
{
    return Gen1RegionCount() + Gen2RegionCount();
}

SatoriRegion* SatoriRecycler::TryGetReusable()
{
    SatoriRegion* reusable = m_reusableRegions->TryPopWithTryEnter();
    if (reusable)
    {
        size_t occupancy = reusable->Occupancy();
        _ASSERTE(occupancy < Satori::REGION_SIZE_GRANULARITY);
        reusable->OccupancyAtReuse() = (int32_t)occupancy;
    }

    return reusable;
}

SatoriRegion* SatoriRecycler::TryGetReusableForLarge()
{
    SatoriRegion* reusable = m_reusableRegions->TryDequeueIfHasFreeSpaceInTopBucket();
    if (reusable)
    {
        size_t occupancy = reusable->Occupancy();
        _ASSERTE(occupancy < Satori::REGION_SIZE_GRANULARITY);
        reusable->OccupancyAtReuse() = (int32_t)occupancy;
    }

    return  reusable;
}

void SatoriRecycler::PushToEphemeralQueues(SatoriRegion* region)
{
    if (region->HasUnmarkedDemotedObjects())
    {
        m_ephemeralWithUnmarkedDemoted->Push(region);
    }
    else
    {
        // we do not know, so conservatively assume that the next GC may promote
        size_t estimatedReclaim = region->ReclaimSizeIfRelocated(m_nextGcIsFullGc);
        if (estimatedReclaim  > 0)
        {
            Interlocked::ExchangeAdd64(&m_estimatedEphemeralReclaim, estimatedReclaim);
        }

        if (region->IsPromotionCandidate())
        {
            Interlocked::Increment(&m_promotionEstimate);
        }

        if (region->HasFinalizables())
        {
            m_ephemeralFinalizationTrackingRegions->Push(region);
        }
        else
        {
            m_ephemeralRegions->Push(region);
        }
    }
}

void SatoriRecycler::PushToTenuredQueues(SatoriRegion* region)
{
    // we do not know, so conservatively assume that the next GC may promote
    size_t estimatedReclaim = region->ReclaimSizeIfRelocated(m_nextGcIsFullGc);
    if (estimatedReclaim  > 0)
    {
        Interlocked::ExchangeAdd64(&m_estimatedEphemeralReclaim, estimatedReclaim);
    }

    if (region->HasFinalizables())
    {
        m_tenuredFinalizationTrackingRegions->Push(region);
    }
    else
    {
        m_tenuredRegions->Push(region);
    }
}

// NOTE: recycler owns nursery regions only temporarily for the duration of GC when mutators are stopped.
//       we do not keep them because an active nursery region may change its finalizability classification
//       concurrently by allocating a finalizable object.
void SatoriRecycler::AddEphemeralRegion(SatoriRegion* region)
{
    _ASSERTE(region->GetAllocStart() == 0);
    _ASSERTE(region->GetAllocRemaining() == 0);
    _ASSERTE(region->Generation() == 0 || region->Generation() == 1);
    _ASSERTE(!region->HasMarksSet());
    _ASSERTE(!region->DoNotSweep());
    _ASSERTE(!region->IsAttachedToAllocatingOwner() || IsBlockingPhase());

    PushToEphemeralQueues(region);
    size_t allocatedBytes = region->Occupancy() - region->OccupancyAtReuse();
    _ASSERTE(allocatedBytes <= region->Occupancy());
    region->OccupancyAtReuse() = 0;
    Interlocked::ExchangeAdd64(&m_gen1AddedSinceLastCollection, allocatedBytes);
    if (region->IsEscapeTracking())
    {
        _ASSERTE(IsBlockingPhase());
        _ASSERTE(!region->HasPinnedObjects());
        region->ClearMarks();
    }

    // When concurrent marking is allowed we may have marks already.
    // Demoted regions could be pre-marked
#if _DEBUG
    if (!region->MaybeAllocatingAcquire())
    {
        region->Verify(/* allowMarked */ region->IsDemoted() || SatoriUtil::IsConcurrentEnabled());
    }
#endif
}

void SatoriRecycler::AddTenuredRegion(SatoriRegion* region)
{
    _ASSERTE(region->GetAllocStart() == 0);
    _ASSERTE(region->GetAllocRemaining() == 0);
    _ASSERTE(!region->IsEscapeTracking());
    _ASSERTE(!region->HasMarksSet());
    _ASSERTE(!region->DoNotSweep());

    region->Verify(/* allowMarked */ SatoriUtil::IsConcurrentEnabled() && SatoriUtil::IsConservativeMode());
    PushToTenuredQueues(region);
    _ASSERTE(region->Generation() == 2);
    Interlocked::ExchangeAdd64(&m_gen2AddedSinceLastCollection, region->Occupancy());
    region->RearmCardsForTenured();
}

size_t SatoriRecycler::GetNowMillis()
{
    int64_t t = minipal_hires_ticks();
    return (size_t)(t / m_perfCounterTicksPerMilli);
}

size_t SatoriRecycler::GetNowUsecs()
{
    int64_t t = minipal_hires_ticks();
    return (size_t)(t / m_perfCounterTicksPerMicro);
}

size_t SatoriRecycler::IncrementGen0Count()
{
    // duration of Gen0 is typically << msec, so we will not record that.
    m_gcStartMillis[0] = GetNowMillis();
    return Interlocked::Increment((size_t*)&m_gcCount[0]);
}

// if generation is too small we do not bother with concurrent marking.
// the extra costs of concurrent marking may not justify using it with tiny heaps
// unless we run in low-latency mode, then we always prefer concurrent.
#if _DEBUG
static const int concMarkThreshold = 0;
#else
// Assuming very roughly 100ns per live object, 10K object graph marks in 1ms by a single thread.
static const int concMarkThreshold = 1024  * 10 * MIN_OBJECT_SIZE;
#endif

bool SatoriRecycler::ShouldDoConcurrent(int generation)
{
    if (IsLowLatencyMode())
    {
        return true;
    }

    if (this->GetTotalOccupancy() < concMarkThreshold)
    {
        return false;
    }

    return true;
}

void SatoriRecycler::TryStartGC(int generation, gc_reason reason)
{
    if (m_nextGcIsFullGc)
    {
        generation = 2;
    }

    int newState = SatoriUtil::IsConcurrentEnabled() && ShouldDoConcurrent(generation)?
        GC_STATE_CONCURRENT :
        GC_STATE_BLOCKING;

    if (m_gcState == GC_STATE_NONE &&
        Interlocked::CompareExchange(&m_gcState, newState, GC_STATE_NONE) == GC_STATE_NONE)
    {
        m_CurrentGcInfo = generation == 2 ? &m_lastTenuredGcInfo :&m_lastEphemeralGcInfo;
        m_CurrentGcInfo->m_condemnedGeneration = (uint8_t)generation;
        m_CurrentGcInfo->m_concurrent = m_gcState == GC_STATE_CONCURRENT;

        FIRE_EVENT(GCTriggered, (uint32_t)reason);

        m_trimmer->SetStopSuggested();

        // Here we have a short window when other threads may notice that gc is in progress,
        // but will find no helping opportunities until we set m_condemnedGeneration.
        // that is ok.

        // in concurrent case there is an extra pass over stacks and handles
        // which happens before the blocking stage
        if (newState == GC_STATE_CONCURRENT)
        {
            m_ccStackMarkState = CC_MARK_STATE_NONE;
            IncrementRootScanTicket();
            m_concurrentCardsDone = (generation == 2);
            m_concurrentCleaningState = CC_CLEAN_STATE_NOT_READY;
            m_concurrentHandlesDone = false;
            SatoriHandlePartitioner::StartNextScan();
            m_activeWorkerFn = &SatoriRecycler::ConcurrentWorkerFn;
        }

        if (generation != 2)
        {
            IncrementCardScanTicket();
        }

        // publishing condemned generation will enable helping.
        // it should happen after the writes above.
        // just to make sure it is all published when the barrier is updated, which is fully synchronizing.
        VolatileStore(&m_condemnedGeneration, generation);

        // if we are doing blocking GC, just go and start it
        if (newState == GC_STATE_BLOCKING)
        {
            BlockingCollect();
        }
    }
}

bool IsWorkerThread()
{
    return GCToEEInterface::WasCurrentThreadCreatedByGC();
}

int64_t SatoriRecycler::HelpQuantum()
{
        return m_perfCounterTicksPerMilli /
            (IsWorkerThread() ?
                8:  // 125 usec
                64); // 15 usec
}

bool SatoriRecycler::HelpOnceCoreInner(bool minQuantum)
{

    if (m_ccStackMarkState == CC_MARK_STATE_MARKING)
    {
        _ASSERTE(m_isBarrierConcurrent);
        // help with marking stacks and f-queue, this is urgent since EE is stopped for this.
        BlockingMarkForConcurrentImpl();
    }

    int64_t timeStamp = minipal_hires_ticks();
    int64_t deadline = timeStamp + (minQuantum ? 0: HelpQuantum());

    // this should be done before scanning stacks or cards
    // since the regions must be swept before we can use FindObject
    if (m_ccStackMarkState == CC_MARK_STATE_NONE)
    {
        if (DrainDeferredSweepQueueConcurrent(deadline))
        {
            return true;
        }
    }

    // make sure the barrier is toggled to concurrent before marking
    if (!m_isBarrierConcurrent)
    {
        // toggling is a ProcessWide fence.
        ToggleWriteBarrier(true, /* skipCards */ false, /* eeSuspended */ false);
        m_isBarrierConcurrent = true;
    }

    if (m_ccStackMarkState != CC_MARK_STATE_DONE)
    {
        MarkOwnStackOrDrainQueuesConcurrent(deadline);
    }

    if (m_ccStackMarkState == CC_MARK_STATE_SUSPENDING_EE && !IsWorkerThread())
    {
        // this is a mutator thread and we are suspending them, leave and suspend.
        return true;
    }

    if (MarkDemotedAndDrainQueuesConcurrent(deadline))
    {
        return true;
    }

    if (!m_concurrentHandlesDone)
    {
        if (MarkHandles(deadline))
        {
            return true;
        }
        else
        {
            m_concurrentHandlesDone = true;
        }
    }

    if (!m_concurrentCardsDone)
    {
        _ASSERTE(m_condemnedGeneration != 2);
        if (MarkThroughCardsConcurrent(deadline))
        {
            return true;
        }
        else
        {
            m_concurrentCardsDone = true;
        }
    }

    // if stacks are not marked yet, start suspending EE
    if (m_ccStackMarkState == CC_MARK_STATE_NONE)
    {
        // only one thread will win and drive this stage, others may help.
        BlockingMarkForConcurrent();
    }

    if (m_ccStackMarkState == CC_MARK_STATE_MARKING)
    {
        _ASSERTE(IsWorkerThread());
        // do not leave (and start blocking gc), come back and help
        return true;
    }

    if (m_concurrentCleaningState == CC_CLEAN_STATE_CLEANING)
    {
        if (CleanCardsConcurrent(deadline))
        {
            return true;
        }
        else
        {
            m_concurrentCleaningState = CC_CLEAN_STATE_DONE;
        }
    }

    // if queues are empty we see no more work
    return DrainMarkQueuesConcurrent(nullptr, deadline);
}

bool SatoriRecycler::HelpOnceCore(bool minQuantum)
{
    if (m_condemnedGeneration == 0)
    {
        // GC has not started yet, come again later.
        return true;
    }

    int64_t start = minipal_hires_ticks();

    bool moreWork = !m_concurrentCardsDone ||
        m_ccStackMarkState != CC_MARK_STATE_DONE ||
        m_concurrentCleaningState == CC_CLEAN_STATE_CLEANING ||
        !m_workList->IsEmpty();

    if (moreWork && m_concurrentCleaningState != CC_CLEAN_STATE_WAIT_FOR_HELPERS)
    {
        Interlocked::Increment(&m_ccHelpersNum);
        if (m_concurrentCleaningState != CC_CLEAN_STATE_WAIT_FOR_HELPERS)
        {
            moreWork = HelpOnceCoreInner(minQuantum);
        }
        Interlocked::Decrement(&m_ccHelpersNum);
    }

    if (!moreWork)
    {
        // if did not try to clean yet, tell new helpers to not enter
        if (m_concurrentCleaningState == CC_CLEAN_STATE_NOT_READY)
        {
            // there will be more work soon.
            moreWork = true;
            Interlocked::CompareExchange(&m_concurrentCleaningState, CC_CLEAN_STATE_WAIT_FOR_HELPERS, CC_CLEAN_STATE_NOT_READY);
        }
    }

    // we need the helpers to be out before cleaning as we need to switch scan ticket
    // and we need to read the state before reading the helper count
    if (VolatileLoad(&m_concurrentCleaningState) == CC_CLEAN_STATE_WAIT_FOR_HELPERS)
    {
        // we can't help at this time, but there is more work.
        moreWork = true;

        // if we see no workers after seeing the wait state, the worker count is effectively 0
        // as new worker will check the state after incrementing m_ccHelpersNum and not start helping if sees the state.
        int ccHelpersNum = m_ccHelpersNum;

        // last helper initiates cleaning
        if (ccHelpersNum == 0 &&
            Interlocked::CompareExchange(&m_concurrentCleaningState, CC_CLEAN_STATE_SETTING_UP, CC_CLEAN_STATE_WAIT_FOR_HELPERS) == CC_CLEAN_STATE_WAIT_FOR_HELPERS)
        {
            IncrementCardScanTicket();
            m_concurrentCleaningState = CC_CLEAN_STATE_CLEANING;
        }
    }

    // If we are done cleaning and see no work, start blocking collection.
    if (m_concurrentCleaningState == CC_CLEAN_STATE_DONE &&
        m_workList->IsEmpty())
    {
        // was it long enough since last time we saw work?
        if (start - m_noWorkSince > HelpQuantum() * 4)
        {
            // 4 help quantums without work, seems like we are done
            // we may have some helpers draining long chains and not sharing anything
            // in such degenerate case we still may want to wrap it up and and block.
            if (Interlocked::CompareExchange(&m_gcState, GC_STATE_BLOCKING, GC_STATE_CONCURRENT) == GC_STATE_CONCURRENT)
            {
                BlockingCollect();
            }
        }
    }

    return moreWork || !m_workList->IsEmpty();
}

void SatoriRecycler::HelpOnce()
{
    _ASSERTE(!IsWorkerThread());

    if (m_gcState != GC_STATE_NONE)
    {
        if (m_gcState == GC_STATE_CONCURRENT &&
            (SatoriUtil::IsPacingEnabled() || m_activeWorkers == 0))
        {
            if (m_condemnedGeneration == 0)
            {
                // not started yet.
                return;
            }

            int64_t start = minipal_hires_ticks();

            bool moreWork = HelpOnceCore(/*minQuantum*/ false);
            if (moreWork)
            {
                m_noWorkSince = minipal_hires_ticks();
            }
            else
            {
                // TODO: VS is this possible? should we do something like block?
                //if (!moreWork && start - m_noWorkSince > m_perfCounterTicksPerMilli)
                //{
                //    printf("PANIC\n");
                //}
            }

            // Check if we need to pace. Even if we see more work, it could be not a timeout.
            // Only makes sense if we can use extra workers.
            if (IsLowLatencyMode() && MaxWorkers() > 0 && m_gcState == GC_STATE_CONCURRENT)
            {
                // if we did not use all the quantum in Low Latency mode,
                // consume what roughly remains for pacing reasons.
                int64_t deadline = start + HelpQuantum() / 2;
                int iters = 1;
                while (minipal_hires_ticks() < deadline &&
                    m_ccStackMarkState != CC_MARK_STATE_SUSPENDING_EE)
                {
                    iters *= 2;
                    for (int i = 0; i < iters; i++)
                    {
                        YieldProcessor();
                    }
                }
            }
        }

        GCToEEInterface::GcPoll();
    }
    else if (!m_deferredSweepRegions->IsEmpty())
    {
        int64_t timeStamp = minipal_hires_ticks();
        int64_t deadline = timeStamp + HelpQuantum();
        DrainDeferredSweepQueueConcurrent(deadline);
    }
}

void SatoriRecycler::ConcurrentWorkerFn()
{
    // workers have deadline too, just to come here and check the stage.
    while (m_gcState == GC_STATE_CONCURRENT)
    {
        if (HelpOnceCore(/*minQuantum*/ false))
        {
            m_noWorkSince = minipal_hires_ticks();
        }
        else if (m_concurrentCleaningState == CC_CLEAN_STATE_NOT_READY)
        {
            // we saw no work available and may not be coming soon, so will exit.
            // if more work is generated we will be invited back.
            break;
        }

        // past CC_CLEAN_STATE_NOT_READY stage we spin while GC_STATE_CONCURRENT - to check
        // for more work or for completion condition.
        MaybeAskForHelp();
        break;
    }
}

class MarkContext
{
    friend class SatoriRecycler;

public:
    MarkContext(SatoriRecycler* recycler)
        : m_WorkChunk()
    {
        m_recycler = recycler;
        m_condemnedGeneration = recycler->m_condemnedGeneration;
        m_heap = recycler->m_heap;
    }

    void PushToMarkQueues(SatoriObject* o)
    {
        if (m_WorkChunk && m_WorkChunk->TryPush(o))
        {
            return;
        }

        m_recycler->PushToMarkQueuesSlow(m_WorkChunk, o);
    }

private:
    int m_condemnedGeneration;
    SatoriWorkChunk* m_WorkChunk;
    SatoriHeap* m_heap;
    SatoriRecycler* m_recycler;
};

void SatoriRecycler::BlockingMarkForConcurrentImpl()
{
    Interlocked::Increment(&m_ccStackMarkingThreadsNum);
    // check state again it could have changed if there were no marking threads
    if (m_ccStackMarkState == CC_MARK_STATE_MARKING)
    {
        MarkAllStacksFinalizationAndDemotedRoots();
    }

    Interlocked::Decrement(&m_ccStackMarkingThreadsNum);
}

/* static */
void SatoriRecycler::ConcurrentPhasePrepFn(gc_alloc_context* gcContext, void* param)
{
    SatoriAllocationContext* context = (SatoriAllocationContext*)gcContext;
    MarkContext* markContext = (MarkContext*)param;
    SatoriRecycler* recycler = markContext->m_recycler;

    SatoriRegion* region = context->RegularRegion();
    if (region)
    {
        if (region->IsEscapeTracking())
        {
            region->StopEscapeTracking();
        }

        if (region->HasUnmarkedDemotedObjects())
        {
            recycler->MarkDemoted(region, markContext);
        }
    }

    region = context->LargeRegion();
    if (region && region->HasUnmarkedDemotedObjects())
    {
        recycler->MarkDemoted(region, markContext);
    }
}

void SatoriRecycler::BlockingMarkForConcurrent()
{
    int targetState = CC_MARK_STATE_SUSPENDING_EE;
    if (Interlocked::CompareExchange(&m_ccStackMarkState, CC_MARK_STATE_SUSPENDING_EE, CC_MARK_STATE_NONE) == CC_MARK_STATE_NONE)
    {
        size_t blockingStart = minipal_hires_ticks();
        GCToEEInterface::SuspendEE(SUSPEND_FOR_GC_PREP);

        // swap reusable and alternate so that we could filter through reusables.
        // the swap needs to be done when EE is stopped, but before marking has started.
        SatoriRegionQueue* alternate = m_reusableRegionsAlternate;
        _ASSERTE(alternate->IsEmpty());
        m_reusableRegionsAlternate = m_reusableRegions;
        m_reusableRegions = alternate;

        // signal to everybody to start marking roots
        VolatileStore((int*)&m_ccStackMarkState, CC_MARK_STATE_MARKING);
        MaybeAskForHelp();

        // mark demoted regions if any attached to thread contexts
        MarkContext c(this);
        GCToEEInterface::GcEnumAllocContexts(ConcurrentPhasePrepFn, &c);
        if (c.m_WorkChunk != nullptr)
        {
            m_workList->Push(c.m_WorkChunk);
        }

        // now join everybody else and mark some roots
        MarkAllStacksFinalizationAndDemotedRoots();

        // done, wait for marking to finish and restart EE
        Interlocked::Exchange(&m_ccStackMarkState, CC_MARK_STATE_DONE);
        while (m_ccStackMarkingThreadsNum)
        {
            // since we are waiting anyways, try helping
            if (!HelpOnceCore(/*minQuantum*/ true))
            {
                YieldProcessor();
            }
        }

        size_t blockingDuration = (minipal_hires_ticks() - blockingStart);
        m_CurrentGcInfo->m_pauseDurations[1] = blockingDuration / m_perfCounterTicksPerMicro;
        m_gcAccmulatingDurationUsecs[m_condemnedGeneration] += blockingDuration / m_perfCounterTicksPerMicro;
        UpdateGcCounters(blockingStart);

        GCToEEInterface::RestartEE(false);
    }
}

int SatoriRecycler::MaxWorkers()
{
    int workerCount = SatoriUtil::MaxWorkersCount();
    if (workerCount < 0)
    {
        int cpuCount = GCToOSInterface::GetTotalProcessorCount();

        // TUNING: should this be more dynamic? check CPU load and such.
        workerCount = cpuCount - 1;
        if (!IsBlockingPhase() && IsLowLatencyMode())
        {
            workerCount = (cpuCount + 1) / 2;
        }
    }

    return workerCount;
}

void SatoriRecycler::MaybeAskForHelp()
{
    if (m_activeWorkerFn && m_activeWorkers < MaxWorkers())
    {
        AskForHelp();
    }
}

void SatoriRecycler::AskForHelp()
{
    if (!m_gateSignaled && Interlocked::CompareExchange(&m_gateSignaled, 1, 0) == 0)
    {
        // we signaled the gate, but need to make sure a worker will see it, even if all workers have gone to sleep.
        // if there is no pending wake, lets wake one thread.
        if (!m_workerWoken && Interlocked::CompareExchange(&m_workerWoken, 1, 0) == 0)
        {
            // wake can fold with other wakes, but the presence of wake ensures that something will wake up and check for work.
            // except if a worker times out and exits, which is very rare, then we may have noone to wake.
            m_workerGate->WakeOne();
        }
    }

    // if all workers are busy, make another one, up to a limit.
    // if workers are spining/sleeping, we will not add more workers.
    int totalWorkers = m_totalWorkers;
    if (m_activeWorkers >= totalWorkers &&
        totalWorkers < MaxWorkers() &&
        Interlocked::CompareExchange(&m_totalWorkers, totalWorkers + 1, totalWorkers) == totalWorkers)
    {
        if (!GCToEEInterface::CreateThread(WorkerThreadMainLoop, this, false, "Satori GC Worker Thread"))
        {
            Interlocked::Decrement(&m_totalWorkers);
            return;
        }
    }
}

void SatoriRecycler::Collect(int generation, bool force, bool blocking)
{
    // only blocked & forced GC is strictly observable - via finalization.
    // otherwise it is kind of a suggestion to maybe start collecting

    if (!force)
    {
        // just check if it is time
        MaybeTriggerGC(gc_reason::reason_induced_noforce);
        return;
    }

    int64_t& collectionNumRef = (generation == 1 ? m_gcCount[1] : m_gcCount[2]);
    int64_t desiredCollectionNum = collectionNumRef + 1;

    // If we need to block for GC, a GC that is already in progress does not count.
    // It may have marked what is not alive at the time of the Collect call and
    // that is observably incorrect.
    // We will wait until the completion of the next one.
    if (m_gcState != GC_STATE_NONE)
    {
        desiredCollectionNum++;
    }

    do
    {
        TryStartGC(generation, gc_reason::reason_induced);
        HelpOnce();
    } while (blocking && (desiredCollectionNum > collectionNumRef));
}

bool SatoriRecycler::IsBlockingPhase()
{
    return m_gcState == GC_STATE_BLOCKED;
}

//TUNING: We use a very simplistic approach for GC triggering here.
//        By default: 
//          SatoriUtil::Gen2Target() triggers Gen2 GC when heap size doubles.
//          SatoriUtil::Gen1Target() triggers Gen1 GC when ephemeral size quadruples.
// 
//        There could be a lot of room to improve in this area:
//        - could consider current CPU/memory load and adjust accordingly
//        - could collect and use past history of the program behavior
//        - could consider user input as to favor latency or throughput
//        - ??

void SatoriRecycler::MaybeTriggerGC(gc_reason reason)
{
    int generation = 0;

    if (GetNowMillis() < m_gcNextTimeTarget)
    {
        // too early
        return;
    }

    if (m_gen1AddedSinceLastCollection > m_gen1Budget)
    {
        generation = 1;
    }

    // gen2 allocations are rare and do not count towards gen1 budget since gen1 will not help with that
    // they can be big though, so check if by any chance gen2 allocs alone pushed us over the limit
    if (m_gen2AddedSinceLastCollection * SatoriUtil::Gen2Target() / 100 > m_totalLimit)
    {
        generation = 2;
    }

    if (generation != 0)
    {
        TryStartGC(generation, reason);
    }

    HelpOnce();
}

size_t GetAvailableMemory()
{
    uint64_t available;
    uint64_t total;
    GCToOSInterface::GetMemoryStatus(0, nullptr, &available, &total);

    // we will not use the last 5% of physical memory
    uint64_t reserve = total * 5 / 100;
    if (available > reserve)
    {
        return available - reserve;
    }

    return 0;
}

void SatoriRecycler::AdjustHeuristics()
{
    // All sweeping should be done by now and occupancies should reflect live set as of last GC.
    // m_gen1AddedSinceLastCollection collects direct allocs into gen1 and is not reflected in that.
    // m_gen2AddedSinceLastCollection collects direct allocs into gen2 and is not included in m_occupancy[2]
    // m_gen2AddedSinceLastCollection may accumulate across several collections, name is a bit misleading.
    // These counts are not included in occupancy since these are new allocs and the objects may not be alive.
    // The purpose of these counters is just to track new allocs and trigger GC.
    // 
    // Sum of all occupancies and allocs added should be the total occupancy in tenured and ephemeral lists.
    // TODO: VS worth asserting?
    //
    // Nursery regions are not included in the numbers as we collect these stats when region is passed to recycler,
    // but Nursery regions are still owned by threads/allocator - we do not detach them until we are sure we will
    // promote en-masse.  (thus the alloc counts may have a lag up to 2Mb per thread, which seems ok).

    size_t ephemeralOccupancy = m_occupancy[1] + m_occupancy[0];
    size_t tenuredOccupancy = m_occupancy[2];
    size_t occupancy = tenuredOccupancy + ephemeralOccupancy;

    if (m_prevCondemnedGeneration == 2)
    {
        m_totalLimit = occupancy * SatoriUtil::Gen2Target() / 100;
    }

    // we trigger GC when estimated ephemeral size grows to SatoriUtil::Gen1Target(),
    // the budget is the diff to reach that
    // NOTE: after gen2 ephemeralOccupancy is 0. GC.Collect can also see small gen1 size.
    //       that is ok to happen occasionally as we have min limits and smoothing.
    size_t newGen1Budget = max((size_t)MIN_GEN1_BUDGET, ephemeralOccupancy * (SatoriUtil::Gen1Target() - 100) / 100);

    // TUNING: any reasons we might want smaller budget?
    // alternatively we allow gen1 allocs up to 1/16 of total limit.
    size_t altNewGen1Budget = max((size_t)MIN_GEN1_BUDGET, m_totalLimit / 16);

    // take max of both budgets
    newGen1Budget = max(newGen1Budget, altNewGen1Budget);

    // smooth the budget a bit
    // TUNING: using exponential smoothing with alpha == 1/2. is it a good smooth/lag balance?
    m_gen1Budget = (m_gen1Budget + newGen1Budget) / 2;

    // if the heap size will definitely be over the limit at next GC, make the next GC a full GC,
    // unless we already doing gen2 GC
    if ((m_condemnedGeneration != 2 && occupancy + m_gen1Budget > m_totalLimit) ||
        !SatoriUtil::IsGen1Enabled())
    {
        // NOTE: If gen1 is big, we will still use gen1 target, but then do gen2 GC.
        //       We may exceed the total goal temporarily.
        //       That is ok, but try not to overrun by too much.
        m_nextGcIsFullGc = true;
        if (occupancy > m_totalLimit)
        {
            m_gen1Budget = (size_t)MIN_GEN1_BUDGET;
        }
        else
        {
            m_gen1Budget = max(m_totalLimit - occupancy, (size_t)MIN_GEN1_BUDGET);
        }
    }
    else
    {
        m_nextGcIsFullGc = false;
    }

    // limit budget to available memory
    size_t available = GetAvailableMemory();
    if (available < m_gen1Budget)
    {
        m_gen1Budget = available;
    }

    // now figure if we will promote
    m_promoteAllRegions = false;
    size_t promotionEstimate = m_promotionEstimate;
    m_promotionEstimate = 0;

    if (m_condemnedGeneration == 2)
    {
        m_promoteAllRegions = true;
        m_gen1CountAtLastGen2 = (int)m_gcCount[1];
    }
    else
    {
        // ensure that sometimes we do gen2
        if ((int)m_gcCount[1] - m_gen1CountAtLastGen2 > 128)
        {
           m_nextGcIsFullGc = true;
        }

        if (promotionEstimate > Gen1RegionCount() / 2)
        {
            // will promote too many, just promote all
            m_promoteAllRegions = true;
        }
    }
}

void SatoriRecycler::BlockingCollect()
{
    if (m_condemnedGeneration == 2)
    {
        BlockingCollect2();
    }
    else
    {
        BlockingCollect1();
    }
}

NOINLINE
void SatoriRecycler::BlockingCollect1()
{
    size_t blockingStart = minipal_hires_ticks();

    // stop other threads.
    GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);

    BlockingCollectImpl();

    size_t blockingDuration = (minipal_hires_ticks() - blockingStart);
    m_CurrentGcInfo->m_pauseDurations[0] = blockingDuration / m_perfCounterTicksPerMicro;
    m_gcDurationUsecs[1] = blockingDuration / m_perfCounterTicksPerMicro;
    m_gcAccmulatingDurationUsecs[1] += blockingDuration / m_perfCounterTicksPerMicro;

    size_t fromStartMillis = GetNowMillis() - m_startMillis;
    m_CurrentGcInfo->m_pausePercentage = (uint32_t)(m_gcAccmulatingDurationUsecs[1] / (int64_t)fromStartMillis / 10);

    m_CurrentGcInfo = nullptr;
    UpdateGcCounters(blockingStart);

    // restart VM
    GCToEEInterface::RestartEE(true);
}

NOINLINE
void SatoriRecycler::BlockingCollect2()
{
    size_t blockingStart = minipal_hires_ticks();

    // stop other threads.
    GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);

    BlockingCollectImpl();

    size_t blockingDuration = (minipal_hires_ticks() - blockingStart);
    m_CurrentGcInfo->m_pauseDurations[0] = blockingDuration / m_perfCounterTicksPerMicro;
    m_gcDurationUsecs[2] = blockingDuration / m_perfCounterTicksPerMicro;
    m_gcAccmulatingDurationUsecs[2] += blockingDuration / m_perfCounterTicksPerMicro;

    size_t fromStartMillis = GetNowMillis() - m_startMillis;
    m_CurrentGcInfo->m_pausePercentage = (uint32_t)( m_gcAccmulatingDurationUsecs[2] / (int64_t)fromStartMillis / 10);

    m_CurrentGcInfo = nullptr;
    UpdateGcCounters(blockingStart);

    // restart VM
    GCToEEInterface::RestartEE(true);
}

NOINLINE
void SatoriRecycler::UpdateGenerationOccupancies()
{
    _ASSERTE(m_occupancyReportingEnabled);

    // We do not need to defer updating gen0, but we will defer for consistency,
    // So that all counts would appear as published at once, more or less.
    m_occupancy[0] = m_occupancyAcc[0];
    m_occupancyAcc[0] = 0;

    // Accumulators are cleared in Plan if corresponding generation is rebuilt in that GC,
    // then occupancy accumulates as GC adds to the gen.
    // Note: gen1 may add to gen2 if promotes, gen1 is always rebuilt.
    // When a GC is completely "done", which is possibly at the start of the next one,
    // we publish accumulated values as the new known generation sizes.
    // The value is not seen while it fills up or updated.
    m_occupancy[1] = m_occupancyAcc[1] - m_demotedOccupancyAcc;
    m_occupancy[2] = m_occupancyAcc[2] + m_demotedOccupancyAcc;

    // No more reporting or accumulating untill enabled again
    m_occupancyReportingEnabled = false;
}

void SatoriRecycler::BlockingCollectImpl()
{
    _ASSERTE(!m_nextGcIsFullGc || m_condemnedGeneration ==2);

    FIRE_EVENT(GCStart_V2, (uint32_t)GlobalGcIndex() + 1, (uint32_t)m_condemnedGeneration, (uint32_t)reason_empty, (uint32_t)gc_etw_type_ngc);
    m_gcStartMillis[m_condemnedGeneration] = GetNowMillis();
    m_activeWorkerFn = nullptr;
    if (IsWorkerThread())
    {
        // do not consider ourselves a worker to not wait forever when we need workers to leave.
        Interlocked::Decrement(&m_activeWorkers);
    }
    else
    {
        // make sure everyone sees the new Fn before waiting for workers to drain.
        MemoryBarrier();
    }

    while (m_activeWorkers > 0)
    {
        YieldProcessor();
    }

    m_gcState = GC_STATE_BLOCKED;

    // assume that we will relocate. we will rethink later.
    m_isRelocating = m_condemnedGeneration == 2 ? SatoriUtil::IsRelocatingInGen2() : SatoriUtil::IsRelocatingInGen1();
    if (IsLowLatencyMode())
    {
        m_isRelocating = false;
    }

    RunWithHelp(&SatoriRecycler::DrainDeferredSweepQueue);

    // all sweeping should be done by now
    if (m_occupancyReportingEnabled)
    {
        UpdateGenerationOccupancies();
    }

    _ASSERTE(m_deferredSweepRegions->IsEmpty());

    // now we know survivorship after the last GC
    // and we can figure what we want to do in this GC and when we will do the next one
    // NOTE: AdjustHeuristics is in the context of previous GC, which is complete by now and m_occupancy
    //       was updated accordingly.
    //       That is intentional. We could glance into current state (i.e. m_gen1AddedSinceLastCollection + Nursery regions),
    //       but we have only vague idea of how much of that is alive until we mark. And if we
    //       need to guess, we'd guess "same as before" - thus counts at last GC are close enough.
    AdjustHeuristics();
    
    // Here we know if the next GC will surely be a full GC.
    ToggleWriteBarrier(false, /* skipCards */ m_nextGcIsFullGc, /* eeSuspended */ true);
    m_isBarrierConcurrent = false;

    // tell EE that we are starting
    // this needs to be called on a "GC" thread while EE is stopped.
    GCToEEInterface::GcStartWork(m_condemnedGeneration, max_generation);

    // this will make the heap officially parseable.
    DeactivateAllocatingRegions();

    m_condemnedRegionsCount = m_condemnedGeneration == 2 ?
        RegionCount() :
        Gen1RegionCount();

    BlockingMark();
    Plan();
    Relocate();
    Update();

    // we are done using workers.
    // undo the adjustment if we had to do one
    if (IsWorkerThread())
    {
        // interlocked because even though we saw no workers after m_activeWorkerFn was reset,
        // some delayed workers may later still come and look for work.
        Interlocked::Increment(&m_activeWorkers);
    }

    m_gcCount[0]++;
    m_gcCount[1]++;
    if (m_condemnedGeneration == 2)
    {
        m_gcCount[2]++;
        if (m_isRelocating)
        {
            m_compactingGcCount[2]++;
        }
    }
    else
    {
        if (m_isRelocating)
        {
            m_compactingGcCount[1]++;
        }
    }

    m_CurrentGcInfo->m_index = GlobalGcIndex();

    // we may still have some deferred sweeping to do, but
    // that is unobservable to EE, so tell EE that we are done
    GCToEEInterface::GcDone(m_condemnedGeneration);

    FIRE_EVENT(GCEnd_V1, (uint32_t)GlobalGcIndex(), (uint32_t)m_condemnedGeneration);

//void FireGCHeapStats_V2(uint64_t generationSize0,
//    uint64_t totalPromotedSize0,
//    uint64_t generationSize1,
//    uint64_t totalPromotedSize1,
//    uint64_t generationSize2,
//    uint64_t totalPromotedSize2,
//    uint64_t generationSize3,
//    uint64_t totalPromotedSize3,
//    uint64_t generationSize4,
//    uint64_t totalPromotedSize4,
//    uint64_t finalizationPromotedSize,
//    uint64_t finalizationPromotedCount,
//    uint32_t pinnedObjectCount,
//    uint32_t sinkBlockCount,
//    uint32_t gcHandleCount);

    FIRE_EVENT(GCHeapStats_V2,
        m_occupancy[0], m_occupancy[0],
        m_occupancy[1], m_occupancy[1],
        m_occupancy[2], m_occupancy[2],
        (size_t)0, (size_t)0,
        (size_t)0, (size_t)0,
        (size_t)0,
        (size_t)m_heap->FinalizationQueue()->Count(),
        (uint32_t)0,
        (uint32_t)0,
        (uint32_t)0);

    m_prevCondemnedGeneration = m_condemnedGeneration;
    m_condemnedGeneration = 0;
    m_gcState = GC_STATE_NONE;

    if (SatoriUtil::IsConcurrentEnabled() && !m_deferredSweepRegions->IsEmpty())
    {
        m_deferredSweepCount = m_deferredSweepRegions->Count();
        m_activeWorkerFn = &SatoriRecycler::DrainDeferredSweepQueueWorkerFn;
        MaybeAskForHelp();
    }
    else
    {
        // no deferred sweep, can update occupancy earlier (this is optional)
        UpdateGenerationOccupancies();
    }

    m_trimmer->SetOkToRun();

    m_gcNextTimeTarget = GetNowMillis() + SatoriUtil::GcRate();
}

void SatoriRecycler::RunWithHelp(void(SatoriRecycler::* method)())
{
    m_activeWorkerFn = method;
    do
    {
        (this->*method)();
        YieldProcessor();
    } while (m_activeWorkers > 0);

    m_activeWorkerFn = nullptr;
    // make sure everyone sees the new Fn before waiting for workers to drain.
    MemoryBarrier();
    while (m_activeWorkers > 0)
    {
        // TUNING: are we wasting too many cycles here?
        //         should we find something more useful to do than mmpause,
        //         or perhaps Sleep(0) after a few spins?
        YieldProcessor();
    }
}

void SatoriRecycler::BlockingMark()
{
    // tell EE we will be marking
    // NB: we may have done some marking concurrently already, but anything that watches
    //     for GC marking only cares about blocking/final marking phase.
    GCToEEInterface::BeforeGcScanRoots(m_condemnedGeneration, /* is_bgc */ false, /* is_concurrent */ false);

    MarkStrongReferences();
    ASSERT_NO_WORK();
    DependentHandlesScan();
    ASSERT_NO_WORK();

    // Tell EE we have done marking strong references before scanning finalizables.
    // What actually happens here is detaching COM wrappers when exposed object is not reachable.
    // The object may stay around for finalization and become F-reachable, so the check needs to happen here.
    ScanContext sc;
    GCToEEInterface::AfterGcScanRoots(m_condemnedGeneration, max_generation, &sc);

    ShortWeakPtrScan();
    ScanFinalizables();
    MarkNewReachable();
    ASSERT_NO_WORK();
    LongWeakPtrScan();
}

void SatoriRecycler::DrainAndCleanWorker()
{
    do
    {
        DrainMarkQueues();
        CleanCards();
    } while (!m_workList->IsEmpty() || HasDirtyCards());
}

void SatoriRecycler::MarkNewReachable()
{
    // it is nearly impossible to have dirty cards and empty work queue.
    // basically only if we completely ran out of chunks.
    // we will just check for simplicity.
    while (!m_workList->IsEmpty() || HasDirtyCards())
    {
        RunWithHelp(&SatoriRecycler::DrainAndCleanWorker);
        DependentHandlesRescan();
    }
}

void SatoriRecycler::DependentHandlesScan()
{
    DependentHandlesInitialScan();
    MarkNewReachable();
}

void SatoriRecycler::MarkStrongReferences()
{
    // stack and handles do not track dirtying writes,
    // therefore we must rescan even after concurrent mark completed its work
    IncrementRootScanTicket();
    SatoriHandlePartitioner::StartNextScan();

    RunWithHelp(&SatoriRecycler::MarkStrongReferencesWorker);
}

void SatoriRecycler::MarkStrongReferencesWorker()
{
    // in concurrent case the current stack is unlikely to have anything unmarked
    // it is still preferred to look at own stack on the same thread.
    // this will also ask for help.
    MarkOwnStackAndDrainQueues();
    MarkHandles();
    MarkAllStacksFinalizationAndDemotedRoots();

    if (m_condemnedGeneration == 1)
    {
        DrainMarkQueues();
        MarkThroughCards();
    }

    DrainAndCleanWorker();
}

void SatoriRecycler::ASSERT_NO_WORK()
{
    _ASSERTE(m_workList->IsEmpty());

    m_heap->ForEachPage(
        [&](SatoriPage* page)
        {
            _ASSERTE(page->CardState() < Satori::CardState::PROCESSING);
        }
    );
}

void SatoriRecycler::ReportThreadAllocBytes(int64_t bytes, bool isLive)
{
    if (isLive)
    {
        _ASSERTE(IsBlockingPhase());
        m_currentAllocBytesLiveThreads += bytes;
    }
    else
    {
        Interlocked::ExchangeAdd64(&m_currentAllocBytesDeadThreads, bytes);
    }
}

int64_t SatoriRecycler::GetTotalAllocatedBytes()
{
    return m_totalAllocBytes;
}

/* static */
void SatoriRecycler::DeactivateFn(gc_alloc_context* gcContext, void* param)
{
    SatoriAllocationContext* context = (SatoriAllocationContext*)gcContext;
    SatoriRecycler* recycler = (SatoriRecycler*)param;

    context->Deactivate(recycler, /*detach*/ recycler->m_promoteAllRegions);
    recycler->ReportThreadAllocBytes(context->alloc_bytes + context->alloc_bytes_uoh, /*islive*/ true);
}

void SatoriRecycler::DeactivateAllocatingRegions()
{
    m_currentAllocBytesLiveThreads = 0;
    GCToEEInterface::GcEnumAllocContexts(DeactivateFn, m_heap->Recycler());
    m_totalAllocBytes = m_currentAllocBytesLiveThreads + m_currentAllocBytesDeadThreads;

    // make shared regions parseable, in case we have byrefs pointing to them.
    m_heap->Allocator()->DeactivateSharedRegions(m_promoteAllRegions);
}

void SatoriRecycler::PushToMarkQueuesSlow(SatoriWorkChunk*& currentWorkChunk, SatoriObject* o)
{
    _ASSERTE(o->ContainingRegion()->Generation() <= m_condemnedGeneration);

    if (currentWorkChunk)
    {
        m_workList->Push(currentWorkChunk);
        MaybeAskForHelp();
    }

    currentWorkChunk = m_heap->Allocator()->TryGetWorkChunk();
    if (currentWorkChunk)
    {
        currentWorkChunk->Push(o);
    }
    else
    {
        // handle mark overflow by dirtying the cards
        o->DirtyCardsForContent();

        // since this o will not be popped from the work queue,
        // check for unmovable here
        if (o->IsUnmovable())
        {
            o->ContainingRegion()->HasPinnedObjects() = true;
        }
    }
}

/* static */
template <bool isConservative>
void SatoriRecycler::MarkFn(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags)
{
    size_t location = (size_t)*ppObject;
    if (location == 0)
    {
        return;
    }

    MarkContext* markContext = (MarkContext*)sc->_unused1;
    SatoriObject* o = (SatoriObject*)location;

    if (flags & GC_CALL_INTERIOR)
    {
        // byrefs may point to stack, use checked here
        SatoriRegion* containingRegion = markContext->m_heap->RegionForAddressChecked(location);
        if (!containingRegion ||
            (isConservative && (containingRegion->Generation() < 0)))
        {
            return;
        }

        o = containingRegion->FindObject(location);
        if (isConservative && o->IsFree())
        {
            return;
        }
    }
    else if (o->IsExternal())
    {
        return;
    }

    if (o->ContainingRegion()->Generation() <= markContext->m_condemnedGeneration)
    {
        if (!o->IsMarked())
        {
            o->SetMarkedAtomic();
            markContext->PushToMarkQueues(o);
        }

        if (flags & GC_CALL_PINNED)
        {
            o->ContainingRegion()->HasPinnedObjects() = true;
        }
    }
};

/* static */
template <bool isConservative>
void SatoriRecycler::UpdateFn(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags)
{
    size_t location = (size_t)*ppObject;
    if (location == 0)
    {
        return;
    }

    SatoriObject* o = (SatoriObject*)location;
    if (flags & GC_CALL_INTERIOR)
    {
        MarkContext* markContext = (MarkContext*)sc->_unused1;
        // byrefs may point to stack, use checked here
        SatoriRegion* containingRegion = markContext->m_heap->RegionForAddressChecked(location);
        if (!containingRegion ||
            (isConservative && (containingRegion->Generation() < 0)))
        {
            return;
        }

        o = containingRegion->FindObject(location);
        if (isConservative && o->IsFree())
        {
            return;
        }
    }

    SatoriObject* newLocation;
    if (o->IsRelocatedTo(&newLocation))
    {
        if (flags & GC_CALL_INTERIOR)
        {
            *ppObject = (PTR_Object)(location + ((size_t)newLocation - o->Start()));
        }
        else
        {
            *ppObject = (PTR_Object)newLocation;
        }
    }
};

template <bool isConservative>
void SatoriRecycler::MarkFnConcurrent(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags)
{
    size_t location = (size_t)VolatileLoadWithoutBarrier(ppObject);
    if (location == 0)
    {
        return;
    }

#ifdef DEBUG_DestroyedHandleValue
    // we can race with destroy handle during concurrent scan
    if (location == (size_t)DEBUG_DestroyedHandleValue)
        return;
#endif //DEBUG_DestroyedHandleValue

    SatoriRegion* containingRegion;
    MarkContext* markContext = (MarkContext*)sc->_unused1;
    SatoriObject* o = (SatoriObject*)location;

    if (flags & GC_CALL_INTERIOR)
    {
        // byrefs may point to stack, use checked here
        containingRegion = markContext->m_heap->RegionForAddressChecked(location);
        if (!containingRegion)
        {
            return;
        }

        // since this is concurrent, in a conservative case an allocation could have caused a split
        // that shortened the found region and the region no longer contains the ref (which means the ref is not real).
        // Note that the split could only happen before we are done with allocator queue (which is synchronising)
        // and assign 0+ gen to the region.
        // So check here in the opposite order - first that region is 0+ gen and then that the size is still right.
        if (isConservative &&
            (containingRegion->GenerationAcquire() < 0 || location >= containingRegion->End()))
        {
            return;
        }

        // Concurrent FindObject is unsafe in active regions. While ref may be in a real obj,
        // the path to it from the first obj or prev indexed may cross unparsable ranges.
        // The check must acquire to be sure we check before actually doing FindObject.
        if (containingRegion->MaybeAllocatingAcquire())
        {
            return;
        }

        o = containingRegion->FindObject(location);
        if (isConservative && o->IsFree())
        {
            return;
        }
    }
    else
    {
        if (o->IsExternal())
        {
            return;
        }

        containingRegion = o->ContainingRegion();
        // can't mark in regions which are tracking escapes, bitmap is in use
        if (containingRegion->MaybeEscapeTrackingAcquire())
        {
            return;
        }
    }

    // no need for fences here. a published obj cannot become gen2 concurrently. 
    if (containingRegion->Generation() <= markContext->m_condemnedGeneration)
    {
        if (!o->IsMarked())
        {
            o->SetMarkedAtomic();
            markContext->PushToMarkQueues(o);
        }

        if (flags & GC_CALL_PINNED)
        {
            o->ContainingRegion()->HasPinnedObjects() = true;
        }
    }
};

bool SatoriRecycler::MarkDemotedAndDrainQueuesConcurrent(int64_t deadline)
{
    _ASSERTE(!IsBlockingPhase());
    _ASSERTE(deadline > 0);

    MarkContext markContext = MarkContext(this);

    // Going through demoted could be contentious.
    // If there are workers, let workers do that.
    if (IsWorkerThread() || m_activeWorkers == 0)
    {
        // in blocking case we go through demoted together with marking all stacks
        // in concurrent case we do it here, since going through demoted does not need EE stopped.
        SatoriRegion* curRegion = m_ephemeralWithUnmarkedDemoted->TryPop();
        if (curRegion)
        {
            MaybeAskForHelp();
            do
            {
                MarkDemoted(curRegion, &markContext);
                PushToEphemeralQueues(curRegion);

                if ((minipal_hires_ticks() - deadline) > 0)
                {
                    if (markContext.m_WorkChunk != nullptr)
                    {
                        m_workList->Push(markContext.m_WorkChunk);
                    }

                    return true;
                }
            } while ((curRegion = m_ephemeralWithUnmarkedDemoted->TryPop()));
        }
    }

    return DrainMarkQueuesConcurrent(markContext.m_WorkChunk, deadline) ||
        !m_ephemeralWithUnmarkedDemoted->IsEmpty();
}

void SatoriRecycler::MarkOwnStackAndDrainQueues()
{
    MarkContext markContext = MarkContext(this);

    if (!IsWorkerThread())
    {
        gc_alloc_context* aContext = GCToEEInterface::GetAllocContext();
        int threadScanTicket = VolatileLoadWithoutBarrier(&aContext->alloc_count);
        int currentScanTicket = GetRootScanTicket();
        if (threadScanTicket != currentScanTicket)
        {
            // claim our own stack for scanning
            if (Interlocked::CompareExchange(&aContext->alloc_count, currentScanTicket, threadScanTicket) == threadScanTicket)
            {
                MaybeAskForHelp();
                MarkOwnStack(aContext, &markContext);

                // in concurrent prep stage we do not drain after self-scanning as we prefer to suspend quickly
                if (!IsBlockingPhase())
                {
                    if (markContext.m_WorkChunk != nullptr)
                    {
                        m_workList->Push(markContext.m_WorkChunk);
                    }

                    return;
                }
            }
        }
    }

    DrainMarkQueues(markContext.m_WorkChunk);
}

void SatoriRecycler::MarkOwnStackOrDrainQueuesConcurrent(int64_t deadline)
{
    MarkContext markContext = MarkContext(this);

    if (!IsWorkerThread())
    {
        gc_alloc_context* aContext = GCToEEInterface::GetAllocContext();
        int threadScanTicket = VolatileLoadWithoutBarrier(&aContext->alloc_count);
        int currentScanTicket = GetRootScanTicket();
        if (threadScanTicket != currentScanTicket)
        {
            // claim our own stack for scanning
            if (Interlocked::CompareExchange(&aContext->alloc_count, currentScanTicket, threadScanTicket) == threadScanTicket)
            {
                MaybeAskForHelp();
                MarkOwnStack(aContext, &markContext);

                // in concurrent prep stage we do not drain after self-scanning as we prefer to suspend quickly
                if (!IsBlockingPhase())
                {
                    if (markContext.m_WorkChunk != nullptr)
                    {
                        m_workList->Push(markContext.m_WorkChunk);
                    }

                    return;
                }
            }
        }
    }

    DrainMarkQueuesConcurrent(markContext.m_WorkChunk, deadline);
}

void SatoriRecycler::MarkOwnStack(gc_alloc_context* aContext, MarkContext* markContext)
{
    bool isBlockingPhase = IsBlockingPhase();
    if (!isBlockingPhase)
    {
        SatoriRegion* region = ((SatoriAllocationContext*)aContext)->RegularRegion();
        if (region && region->IsEscapeTracking())
        {
            region->StopEscapeTracking();
        }
    }

    ScanContext sc;
    sc.promotion = TRUE;
    sc._unused1 = markContext;

    if (SatoriUtil::IsConservativeMode())
        GCToEEInterface::GcScanCurrentStackRoots(isBlockingPhase ? MarkFn<true> : MarkFnConcurrent<true>, &sc);
    else
        GCToEEInterface::GcScanCurrentStackRoots(isBlockingPhase ? MarkFn<false> : MarkFnConcurrent<false>, &sc);
}

void SatoriRecycler::MarkDemoted(SatoriRegion* curRegion, MarkContext* markContext)
{
    _ASSERTE(curRegion->Generation() == 1);
    curRegion->HasUnmarkedDemotedObjects() = false;

    if (m_condemnedGeneration == 1)
    {
        _ASSERTE(!curRegion->MaybeEscapeTrackingAcquire());
        curRegion->HasPinnedObjects() = true;

        SatoriWorkChunk* gen2Objects = curRegion->DemotedObjects();
        while (gen2Objects)
        {
            for (int i = 0; i < gen2Objects->Count(); i++)
            {
                SatoriObject* o = gen2Objects->Item(i);
                o->SetMarkedAtomic();
                markContext->PushToMarkQueues(o);
            }

            gen2Objects = gen2Objects->Next();
        }
    }
    else
    {
        curRegion->FreeDemotedTrackers();
    }
}

void SatoriRecycler::MarkAllStacksFinalizationAndDemotedRoots()
{
    // mark roots for all stacks
    ScanContext sc;
    sc.promotion = TRUE;
    MarkContext markContext = MarkContext(this);
    sc._unused1 = &markContext;

    bool isBlockingPhase = IsBlockingPhase();

    if (SatoriUtil::IsConservativeMode())
        //generations are meaningless here, so we pass -1
        GCToEEInterface::GcScanRoots(isBlockingPhase ? MarkFn<true> : MarkFnConcurrent<true>, -1, -1, &sc);
    else
        GCToEEInterface::GcScanRoots(isBlockingPhase ? MarkFn<false> : MarkFnConcurrent<false>, -1, -1, &sc);

    SatoriFinalizationQueue* fQueue = m_heap->FinalizationQueue();
    if (fQueue->TryUpdateScanTicket(this->GetRootScanTicket()) &&
        fQueue->HasItems())
    {
        fQueue->ForEachObjectRef(
            [&](SatoriObject** ppObject)
            {
                SatoriObject* o = *ppObject;

                // can't mark in regions which are tracking escapes, bitmap is in use
                if (!isBlockingPhase && o->ContainingRegion()->MaybeEscapeTrackingAcquire())
                {
                    return;
                }

                if (!o->IsMarkedOrOlderThan(m_condemnedGeneration))
                {
                    o->SetMarkedAtomic();
                    markContext.PushToMarkQueues(o);
                }
            }
        );
    }

    // EE is stopped here for root marking, but this could be either
    // a part of the blocking phase or a part of concurrent GC 
    if (isBlockingPhase)
    {
        SatoriRegion* curRegion = m_ephemeralWithUnmarkedDemoted->TryPop();
        if (curRegion)
        {
            MaybeAskForHelp();
            do
            {
                MarkDemoted(curRegion, &markContext);
                PushToEphemeralQueues(curRegion);
            } while ((curRegion = m_ephemeralWithUnmarkedDemoted->TryPop()));
        }
    }
    else
    {
        // going through demoted queue does not require EE stopped,
        // so in concurrent case we will do that later.
        // but there could be demoted regions in reusable queue
        // we can mark tenured objects in those.
        SatoriRegion* curRegion = m_reusableRegionsAlternate->TryPop();
        if (curRegion)
        {
            MaybeAskForHelp();
            do
            {
                if (curRegion->HasUnmarkedDemotedObjects() &&
                    curRegion->ReusableFor() != SatoriRegion::ReuseLevel::Gen0)
                {
                    MarkDemoted(curRegion, &markContext);
                }

                if (curRegion->HasFreeSpaceInTopBucket())
                {
                    m_reusableRegions->Enqueue(curRegion);
                }
                else
                {
                    m_reusableRegions->Push(curRegion);
                }
            } while ((curRegion = m_reusableRegionsAlternate->TryPop()));
        }
    }

    if (markContext.m_WorkChunk != nullptr)
    {
        m_workList->Push(markContext.m_WorkChunk);
    }
}

void SatoriRecycler::PushOrReturnWorkChunk(SatoriWorkChunk * chunk)
{
    if (chunk)
    {
        if (chunk->Count() > 0)
        {
            m_workList->Push(chunk);
        }
        else
        {
            m_heap->Allocator()->ReturnWorkChunk(chunk);
        }
    }
}

bool SatoriRecycler::DrainMarkQueuesConcurrent(SatoriWorkChunk* srcChunk, int64_t deadline)
{
    _ASSERTE(deadline > 0);

    if (!srcChunk)
    {
        srcChunk = m_workList->TryPop();
    }

    if (!m_workList->IsEmpty())
    {
        MaybeAskForHelp();
    }

    SatoriWorkChunk* dstChunk = nullptr;
    SatoriObject* o = nullptr;

    auto markChildFn = [&](SatoriObject** ref)
    {
        SatoriObject* child = VolatileLoadWithoutBarrier(ref);
        if (child && !child->IsExternal())
        {
            SatoriRegion* childRegion = child->ContainingRegion();
            if (!childRegion->MaybeEscapeTrackingAcquire())
            {
                if (!child->IsMarkedOrOlderThan(m_condemnedGeneration))
                {
                    child->SetMarkedAtomic();
                    if (!dstChunk || !dstChunk->TryPush(child))
                    {
                        this->PushToMarkQueuesSlow(dstChunk, child);
                    }
                }
                return;
            }

            // cannot mark the child in a thread local region. just mark the ref as dirty to visit later.
            // if ref is outside of the containing region, it is a fake ref to collectible allocator.
            // dirty the MT location as if it points to the allocator object.
            // technically it does reference the allocator, by indirection.
            SatoriRegion* parentRegion = o->ContainingRegion();
            if ((size_t)ref - parentRegion->Start() > parentRegion->Size())
            {
                ref = (SatoriObject**)o->Start();
            }

            parentRegion->ContainingPage()->DirtyCardForAddressConcurrent((size_t)ref);
        }
    };

    while (srcChunk)
    {
        if (srcChunk->IsRange())
        {
            size_t start, end;
            srcChunk->GetRange(o, start, end);
            srcChunk->Clear();
            if (!dstChunk)
            {
                dstChunk = srcChunk;
            }
            else
            {
                m_heap->Allocator()->ReturnWorkChunk(srcChunk);
            }

            srcChunk = nullptr;
            // mark children in the range
            o->ForEachObjectRef(markChildFn, start, end);
        }
        else
        {
            // share half of work if work list is empty
            if (srcChunk->Count() > Satori::SHARE_WORK_THRESHOLD &&
                m_workList->IsEmpty())
            {
                size_t half = srcChunk->Count() / 2;
                SatoriWorkChunk* halfChunk = m_heap->Allocator()->TryGetWorkChunk();
                if (halfChunk)
                {
                    halfChunk->TakeFrom(srcChunk, half);
                    m_workList->Push(srcChunk);
                    srcChunk = halfChunk;
                    MaybeAskForHelp();
                }
            }

            // drain srcChunk to dst chunk
            while (srcChunk->Count() > 0)
            {
                o = srcChunk->Pop();
                srcChunk->PrefetchNext(1);

                _ASSERTE(o->IsMarked());
                if (o->IsUnmovable())
                {
                    o->ContainingRegion()->HasPinnedObjects() = true;
                }

                // do not get engaged with big objects, reschedule them as child ranges.
                size_t size = o->Size();
                if (size > Satori::MARK_RANGE_THRESHOLD)
                {
                    ScheduleMarkAsChildRanges(o);
                    continue;
                }

                o->ForEachObjectRef(markChildFn, size, /* includeCollectibleAllocator */ true);
            }
        }

        _ASSERTE(srcChunk == nullptr || srcChunk->Count() == 0);

        // every once in a while check for the deadline.
        // check after processing one chunk to:
        // - amortize cost of QueryPerformanceCounter() and
        // - establish the minimum amount of work per help quantum
        if ((minipal_hires_ticks() - deadline) > 0)
        {
            PushOrReturnWorkChunk(srcChunk);
            PushOrReturnWorkChunk(dstChunk);
            return true;
        }

        // done with srcChunk
        // if we have nonempty dstChunk (i.e. produced more work),
        // swap src and dst and continue
        if (dstChunk && dstChunk->Count() > 0)
        {
            SatoriWorkChunk* tmp = srcChunk;
            srcChunk = dstChunk;
            dstChunk = tmp;
        }
        else
        {
            if (srcChunk)
            {
                if (!dstChunk)
                {
                    dstChunk = srcChunk;
                }
                else
                {
                    m_heap->Allocator()->ReturnWorkChunk(srcChunk);
                }
            }

            srcChunk = m_workList->TryPop();
        }
    }

    if (dstChunk)
    {
        _ASSERTE(dstChunk->Count() == 0);
        m_heap->Allocator()->ReturnWorkChunk(dstChunk);
    }

    return false;
}

void SatoriRecycler::ScheduleMarkAsChildRanges(SatoriObject* o)
{
    if (o->RawGetMethodTable()->ContainsGCPointersOrCollectible())
    {
        size_t start = o->Start();
        size_t remains = o->Size();
        size_t chunkSize = remains > Satori::REGION_SIZE_GRANULARITY ?
            Satori::REGION_SIZE_GRANULARITY:
            Satori::MARK_RANGE_THRESHOLD;

            while (remains > 0)
        {
            SatoriWorkChunk* chunk = m_heap->Allocator()->TryGetWorkChunk();
            if (chunk == nullptr)
            {
                o->ContainingRegion()->ContainingPage()->DirtyCardsForRange(start, start + remains);
                remains = 0;
                break;
            }

            size_t len = min(chunkSize, remains);
            chunk->SetRange(o, start, start + len);
            start += len;
            remains -= len;
            m_workList->Push(chunk);
        }

        // done with current object
        _ASSERTE(remains == 0);
    }
}

bool SatoriRecycler::ScheduleUpdateAsChildRanges(SatoriObject* o)
{
    if (o->RawGetMethodTable()->ContainsGCPointers())
    {
        size_t start = o->Start() + sizeof(size_t);
        size_t remains = o->Size() - sizeof(size_t);
        while (remains > 0)
        {
            SatoriWorkChunk* chunk = m_heap->Allocator()->TryGetWorkChunk();
            if (chunk == nullptr)
            {
                return false;
            }

            size_t len = min(Satori::REGION_SIZE_GRANULARITY, remains);
            chunk->SetRange(o, start, start + len);
            start += len;
            remains -= len;
            m_workList->Push(chunk);
        }

        // done with current object
        _ASSERTE(remains == 0);
    }

    return true;
}

void SatoriRecycler::DrainMarkQueues(SatoriWorkChunk* srcChunk)
{
    if (!srcChunk)
    {
        srcChunk = m_workList->TryPop();
    }

    if (!m_workList->IsEmpty())
    {
        MaybeAskForHelp();
    }

    SatoriWorkChunk* dstChunk = nullptr;

    auto markChildFn = [&](SatoriObject** ref)
    {
        SatoriObject* child = *ref;
        if (child &&
            !child->IsExternal() &&
            !child->IsMarkedOrOlderThan(m_condemnedGeneration))
        {
            child->SetMarkedAtomic();
            // put more work, if found, into dstChunk
            if (!dstChunk || !dstChunk->TryPush(child))
            {
                this->PushToMarkQueuesSlow(dstChunk, child);
            }
        }
    };

    while (srcChunk)
    {
        if (srcChunk->IsRange())
        {
            SatoriObject* o;
            size_t start, end;
            srcChunk->GetRange(o, start, end);
            srcChunk->Clear();
            if (!dstChunk)
            {
                dstChunk = srcChunk;
            }
            else
            {
                m_heap->Allocator()->ReturnWorkChunk(srcChunk);
            }

            srcChunk = nullptr;
            // mark children in the range
            o->ForEachObjectRef(markChildFn, start, end);
        }
        else
        {
            // share half of work if work list is empty
            if (srcChunk->Count() > Satori::SHARE_WORK_THRESHOLD &&
                m_workList->IsEmpty())
            {
                size_t half = srcChunk->Count() / 2;
                SatoriWorkChunk* halfChunk = m_heap->Allocator()->TryGetWorkChunk();
                if (halfChunk)
                {
                    halfChunk->TakeFrom(srcChunk, half);
                    m_workList->Push(srcChunk);
                    srcChunk = halfChunk;
                    MaybeAskForHelp();
                }
            }

            // mark objects in the chunk
            while (srcChunk->Count() > 0)
            {
                SatoriObject* o = srcChunk->Pop();
                srcChunk->PrefetchNext(1);

                _ASSERTE(o->IsMarked());
                if (o->IsUnmovable())
                {
                    o->ContainingRegion()->HasPinnedObjects() = true;
                }

                // do not get engaged with big objects, reschedule them as child ranges.
                size_t size = o->Size();
                if (size > Satori::MARK_RANGE_THRESHOLD)
                {
                    ScheduleMarkAsChildRanges(o);
                    continue;
                }

                o->ForEachObjectRef(markChildFn, size, /* includeCollectibleAllocator */ true);
            }
        }

        _ASSERTE(!srcChunk || srcChunk->Count() == 0);

        // done with srcChunk
        // if we have nonempty dstChunk (i.e. produced more work),
        // swap src and dst and continue
        if (dstChunk && dstChunk->Count() > 0)
        {
            SatoriWorkChunk* tmp = srcChunk;
            srcChunk = dstChunk;
            dstChunk = tmp;
        }
        else
        {
            if (srcChunk)
            {
                m_heap->Allocator()->ReturnWorkChunk(srcChunk);
            }

            srcChunk = m_workList->TryPop();
        }
    }

    if (dstChunk)
    {
        _ASSERTE(dstChunk->Count() == 0);
        m_heap->Allocator()->ReturnWorkChunk(dstChunk);
    }
}

// Just a number that is likely be different for different threads making the same call.
// Mix in the gcId to not get stuck with the same combination across threads, in case
// it is not a good combination. (most combinations are good, but there can be rare bad ones)
size_t ThreadSpecificNumber(int64_t gcId)
{
    size_t result = (((size_t)&result ^ (size_t)gcId) * 11400714819323198485llu) >> 32;
    return result;
}

int64_t SatoriRecycler::GlobalGcIndex()
{
    return m_gcCount[0];
}

struct ConcurrentCardsRestart
{
    int64_t gcId;
    SatoriPage* page;
    size_t ii;
    size_t rndOffset;
};

thread_local
ConcurrentCardsRestart t_concurrentCardState;

bool SatoriRecycler::MarkThroughCardsConcurrent(int64_t deadline)
{
    SatoriWorkChunk* dstChunk = nullptr;
    bool revisit = false;

    // Use Gen1 count to identify the current GC. Not Gen0 as that could be changing concurrently.
    // Multiply by 2 to not intersect with concurrent card cleaner, which uses the same restart state.
    int64_t gcId = m_gcCount[1] * 2;
    ConcurrentCardsRestart* pRestart = &t_concurrentCardState;
    if (pRestart->gcId != gcId)
    {
        pRestart->gcId = gcId;
        pRestart->page = nullptr;
        pRestart->ii = 0;
        pRestart->rndOffset = ThreadSpecificNumber(gcId);
    }

    m_heap->ForEachPageUntil(
        [&](SatoriPage* page)
        {
            int8_t pageState = page->CardState();
            // Visit and advance tickets of all non-blank pages and groups, not just remembered as that also sets them for
            // revisit in concurrent clearing. Anything non-blank can be or become dirty, so clear will take a look.
            // Blank ones will not match any tickets and will be visited by clearing pass, if become dirty.
            if (pageState != Satori::CardState::BLANK)
            {
                uint8_t currentScanTicket = GetCardScanTicket();
                if (page->ScanTicket() == currentScanTicket)
                {
                    // this is not a timeout, continue to next page
                    return false;
                }

                // there is unfinished page. Maybe should ask for help
                MaybeAskForHelp();

                size_t groupCount = page->CardGroupCount();

                // if restarting with the same page, continue with the last ii and offset
                size_t offset = pRestart->rndOffset;
                size_t ii = pRestart->ii;
                if (pRestart->page != page)
                {
                    // it is a new page
                    pRestart->page = page;
                    ii = 0;
                }

                while (ii < groupCount)
                {
                    size_t i = (ii + offset) % groupCount;
                    ii++;

                    int8_t groupState = page->CardGroupState(i);
                    // Visit and advance tickets of all non-blank groups, not just remembered as that also
                    // sets them for clearing later.
                    if (groupState != Satori::CardState::BLANK)
                    {
                        uint8_t groupTicket = page->CardGroupScanTicket(i);
                        if (groupTicket == currentScanTicket)
                        {
                            continue;
                        }

                        // claim the group as complete
                        page->CardGroupScanTicket(i) = currentScanTicket;

                        // now we have to finish, since we have clamed the group
                        // NB: two threads may claim the same group and do overlapping work
                        //     that is correct, but redundant. We could claim using interlocked operation
                        //     and avoid that, but such collisions appear to be too rare to worry about.
                        //     It may be worth watching this in the future though.

                        // NB: It is safe to get a region even if region map may be changing because
                        //    a region with remembered/dirty marks must be there and cannot be destroyed.
                        SatoriRegion* region = page->RegionForCardGroup(i);
                        _ASSERTE(region->Generation() >= 0);

                        // we should not be marking when there could be dead objects
                        _ASSERTE(!region->HasMarksSet());

                        // sometimes we set cards without knowing dst generation, but REMEMBERED only has meaning in tenured
                        if (region->Generation() < 2)
                        {
                            // Wiping the group if ephemeral+remembered is an optimization.
                            // If the group is not covered by a tenured region, we wipe the group, to not look at it again in the next scans.
                            // The actual cards do not matter, we will look at cards in ephemeral regions only if group is dirty,
                            // and then cleaner will reset them appropriately.
                            // There is no marking work in this region, so ticket is also irrelevant. It will be wiped if region gets promoted.
                            // As a result cards that do not belong to tenured regions may occasionally have remembered state set.
                            // It is ignorable and we are too lazy to clean that.until the region is promoted.
                            if (groupState == Satori::CardState::REMEMBERED)
                            {
                                Interlocked::CompareExchange(&page->CardGroupState(i), Satori::CardState::BLANK, Satori::CardState::REMEMBERED);
                            }

                            continue;
                        }

                        // invariant check: when marking through cards the gen2 remset stays remset, thus should be marked through on every GC
                        // and should not fall far behind the tickets
                        _ASSERTE(groupTicket == 0 || groupTicket == 0xff || (uint8_t)(currentScanTicket - groupTicket) == 1);
                        int8_t resetValue = Satori::CardState::REMEMBERED;
                        int8_t* cards = page->CardsForGroup(i);
                        for (size_t j = 0; j < Satori::CARD_BYTES_IN_CARD_GROUP; j++)
                        {
                            // cards are often sparsely set, if j is aligned, check the entire size_t for unset value
                            if (((j & (sizeof(size_t) - 1)) == 0) && *((size_t*)&cards[j]) == 0)
                            {
                                j += sizeof(size_t) - 1;
                                continue;
                            }

                            // skip unset cards
                            int8_t origValue = cards[j];
                            if (origValue <= Satori::CardState::BLANK)
                            {
                                continue;
                            }

                            size_t start = page->LocationForCard(&cards[j]);
                            bool cleaned = false;
                            do
                            {
                                // since we are going to look at the card, we can clean its dirty state.
                                // just need to make sure we do the state change before looking at objects - in case it gets dirty again.
                                if (cards[j] == Satori::CardState::DIRTY)
                                {
                                    cleaned = true;
                                    cards[j] = resetValue;
                                }
                            } while (++j < Satori::CARD_BYTES_IN_CARD_GROUP && cards[j] > Satori::CardState::BLANK);

                            size_t end = page->LocationForCard(&cards[j]);
                            size_t objLimit = min(end, region->Start() + Satori::REGION_SIZE_GRANULARITY);
                            SatoriObject* o = region->FindObject(start);

                            if (cleaned)
                            {
                                // do not allow card cleaning to delay until after checking IsMarked
                                MemoryBarrier();
                            }

                            do
                            {
                                // everything is effectively marked in gen2 remembered set
                                // if (o->IsMarked())
                                {
                                    o->ForEachObjectRef(
                                    [&](SatoriObject** ref)
                                    {
                                        SatoriObject* child = VolatileLoadWithoutBarrier(ref);
                                        if (child && !child->IsExternal())
                                        {
                                            SatoriRegion* childRegion = child->ContainingRegion();
                                            if (!childRegion->MaybeEscapeTrackingAcquire())
                                            {
                                                if (!child->IsMarkedOrOlderThan(1))
                                                {
                                                    child->SetMarkedAtomic();
                                                    if (!dstChunk || !dstChunk->TryPush(child))
                                                    {
                                                        this->PushToMarkQueuesSlow(dstChunk, child);
                                                    }
                                                }
                                                return;
                                            }

                                            // cannot mark stuff in thread local regions. just mark as dirty to visit later.
                                            // if ref is outside of the containing region, it is a fake ref to collectible allocator.
                                            // dirty the MT location as if it points to the allocator object
                                            // technically it does reference the allocator, by indirection.
                                            SatoriRegion* parentRegion = o->ContainingRegion();
                                            if ((size_t)ref - parentRegion->Start() > parentRegion->Size())
                                            {
                                                ref = (SatoriObject**)o->Start();
                                            }

                                            parentRegion->ContainingPage()->DirtyCardForAddressConcurrent((size_t)ref);
                                        }
                                    }, start, end);
                                }
                                o = o->Next();
                            } while (o->Start() < objLimit);
                        }

                        _ASSERTE(deadline != 0);
                        if (minipal_hires_ticks() - deadline > 0)
                        {
                            // timed out, there could be more work
                            // save where we would restart if we see this page again
                            pRestart->ii = ii;
                            revisit = true;
                            return true;
                        }
                    }
                }

                // All groups/cards are accounted in this page - either visited or claimed.
                // No marking work is left here, set the ticket to indicate that.
                page->ScanTicket() = currentScanTicket;
            }

            // not a timeout, continue iterating
            return false;
        }
    );

    if (dstChunk)
    {
        m_workList->Push(dstChunk);
    }

    return revisit;
}

bool SatoriRecycler::CleanCardsConcurrent(int64_t deadline)
{
    SatoriWorkChunk* dstChunk = nullptr;
    bool revisit = false;

    // Use Gen1 count to identify the current GC. Not Gen0 as that could be changing concurrently.
    // Multiply by 2 and add 1 to not intersect with concurrent marking, which uses the same restart state.
    int64_t gcId = m_gcCount[1] * 2 + 1;
    ConcurrentCardsRestart* pRestart = &t_concurrentCardState;
    if (pRestart->gcId != gcId)
    {
        pRestart->gcId = gcId;
        pRestart->page = nullptr;
        pRestart->ii = 0;
        pRestart->rndOffset = ThreadSpecificNumber(gcId);
    }

    m_heap->ForEachPageUntil(
        [&](SatoriPage* page)
        {
            int8_t pageState = page->CardState();
            // visit and advance all non-blank pages/groups, not just dirty as that would leave remembered for nonconcurrent visit.
            // the completion criteria is all non-blank pages/groups checked for dirty, cleaned if needed and advanced to current ticket
            // Effectively we want to look at each dirty thing to have less work in blocking stage, but only once.
            // If it gets dirty again - blocking stage will have to deal with it.
            if (pageState != Satori::CardState::BLANK)
            {
                uint8_t currentScanTicket = GetCardScanTicket();
                if (page->ScanTicket() == currentScanTicket)
                {
                    // this is not a timeout, continue to next page
                    return false;
                }

                // there is unfinished page. Maybe should ask for help
                MaybeAskForHelp();

                size_t groupCount = page->CardGroupCount();

                // if restarting with the same page, continue with the last ii and offset
                size_t offset = pRestart->rndOffset;
                size_t ii = pRestart->ii;
                if (pRestart->page != page)
                {
                    // it is a new page
                    pRestart->page = page;
                    ii = 0;
                }

                while (ii < groupCount)
                {
                    size_t i = (ii + offset) % groupCount;
                    ii++;

                    int8_t groupState = page->CardGroupState(i);
                    // visit and advance all non-blank GROUPS, not just dirty as we must advance remembered as well.
                    if (groupState != Satori::CardState::BLANK)
                    {
                        uint8_t groupTicket = page->CardGroupScanTicket(i);
                        if (groupTicket == currentScanTicket)
                        {
                            continue;
                        }

                        // claim the group as complete, now it is ours
                        page->CardGroupScanTicket(i) = currentScanTicket;

                        // Unlike marking we do not have to actually clean cards or commit to finish anything.
                        // The blocking cleaner does not use tickets for completion and will deal with what we might miss here.
                        // For our completion tracking we need to ensure that all non-blank gen2 groups advance
                        // and we have just done that for this group.
                        // Other than that we would like to make a good effort at cleaning cards if the group is dirty.
                        // The race with dirtying is obviously unavoidable, but unimportant here.
                        if (groupState != Satori::CardState::DIRTY)
                        {
                            continue;
                        }

                        // NB: two threads may claim the same group and do overlapping work
                        //     that is correct, but redundant. We could claim using interlocked operation
                        //     and avoid that, but such collisions appear to be too rare to worry about.
                        //     It may be worth watching this in the future though.

                        // NB: It is safe to get a region even if region map may be changing because
                        //    a region with remembered/dirty marks must be there and cannot be destroyed.
                        SatoriRegion* region = page->RegionForCardGroup(i);
                        _ASSERTE(region->Generation() >= 0);

                        // we should not be marking when there could be dead objects
                        _ASSERTE(!region->HasMarksSet());

                        int8_t resetValue = Satori::CardState::REMEMBERED;
                        if (region->Generation() < 2)
                        {
                            // We will not wipe remembered+ephemeral here.
                            // If we are going gen2 GC, we will not care about rememebered set after this and cards will be reset in the end,
                            // otherwise the wiping must have been done already.
                            if (groupState == Satori::CardState::REMEMBERED)
                            {
                                _ASSERTE(m_condemnedGeneration == 2);
                                // we will not find anything dirty here.
                                continue;
                            }

                            if (region->MaybeAllocatingAcquire())
                            {
                                // Allocating region is not parseable. Can't do much here.
                                continue;
                            }

                            resetValue = Satori::CardState::EPHEMERAL;
                        }

                        // invariant check: when marking through cards the gen2 remset stays remset, thus should be marked through on every GC
                        // and should not fall far behind the tickets
                        _ASSERTE(region->Generation() != 2 || groupTicket == 0 || groupTicket == 0xff || (uint8_t)(currentScanTicket - groupTicket) == 1);

                        bool considerAllMarked = region->Generation() > m_condemnedGeneration;
                        int8_t* cards = page->CardsForGroup(i);

                        _ASSERTE(Satori::CardState::DIRTY == (int8_t)0x04);
                        const size_t dirtyBits = 0x0404040404040404;
                        for (size_t j = 0; j < Satori::CARD_BYTES_IN_CARD_GROUP; j++)
                        {
                            // cards are often sparsely set, if j is aligned, check if any card in entire size_t is dirty
                            if (((j & (sizeof(size_t) - 1)) == 0) && (*((size_t*)&cards[j]) & dirtyBits) == 0)
                            {
                                j += sizeof(size_t) - 1;
                                continue;
                            }

                            // skip nondirty cards
                            if (cards[j] != Satori::CardState::DIRTY)
                            {
                                continue;
                            }

                            size_t start = page->LocationForCard(&cards[j]);
                            do
                            {
                                // clean the card since it is going to be visited and marked through.
                                cards[j++] = resetValue;
                            } while (j < Satori::CARD_BYTES_IN_CARD_GROUP && cards[j] == Satori::CardState::DIRTY);

                            size_t end = page->LocationForCard(&cards[j]);
                            size_t objLimit = min(end, region->Start() + Satori::REGION_SIZE_GRANULARITY);
                            SatoriObject* o = region->FindObject(start);

                            // do not allow card cleaning to delay until after checking IsMarked or fetching children
                            MemoryBarrier();

                            do
                            {
                                if (!considerAllMarked)
                                {
                                    o = region->SkipUnmarked(o, objLimit);
                                    if (o->Start() == objLimit)
                                    {
                                        break;
                                    }
                                }

                                // if (considerAllMarked || o->IsMarked())
                                {
                                    o->ForEachObjectRef(
                                        [&](SatoriObject** ref)
                                        {
                                            SatoriObject* child = VolatileLoadWithoutBarrier(ref);
                                            if (child &&
                                                !child->IsExternal())
                                            {
                                                SatoriRegion* childRegion = child->ContainingRegion();
                                                if (!childRegion->MaybeEscapeTrackingAcquire())
                                                {
                                                    if (!child->IsMarkedOrOlderThan(m_condemnedGeneration))
                                                    {
                                                        child->SetMarkedAtomic();
                                                        if (!dstChunk || !dstChunk->TryPush(child))
                                                        {
                                                            this->PushToMarkQueuesSlow(dstChunk, child);
                                                        }
                                                    }

                                                    return;
                                                }

                                                // cannot mark stuff in thread local regions. just mark as dirty to visit later.
                                                // if ref is outside of the containing region, it is a fake ref to collectible allocator.
                                                // dirty the MT location as if it points to the allocator object
                                                // technically it does reference the allocator, by indirection.
                                                SatoriRegion* parentRegion = o->ContainingRegion();
                                                if ((size_t)ref - parentRegion->Start() > parentRegion->Size())
                                                {
                                                    ref = (SatoriObject**)o->Start();
                                                }

                                                parentRegion->ContainingPage()->DirtyCardForAddressConcurrent((size_t)ref);
                                            }
                                        }, start, end);
                                }
                                o = o->Next();
                            } while (o->Start() < objLimit);
                        }

                        _ASSERTE(deadline != 0);
                        if (minipal_hires_ticks() - deadline > 0)
                        {
                            // timed out, there could be more work
                            // save where we would restart if we see this page again
                            pRestart->ii = ii;
                            revisit = true;
                            return true;
                        }
                    }
                }

                // All groups/cards are accounted in this page - either visited or claimed.
                // No marking work is left here, set the ticket to indicate that.
                page->ScanTicket() = currentScanTicket;
            }

            // not a timeout, continue iterating
            return false;
        }
    );

    if (dstChunk)
    {
        m_workList->Push(dstChunk);
    }

    return revisit;
}

void SatoriRecycler::MarkThroughCards()
{
    SatoriWorkChunk* dstChunk = nullptr;

    m_heap->ForEachPage(
        [&](SatoriPage* page)
        {
            int8_t pageState = page->CardState();
            if (pageState != Satori::CardState::BLANK)
            {
                uint8_t currentScanTicket = GetCardScanTicket();
                if (page->ScanTicket() == currentScanTicket)
                {
                    // continue to next page
                    return;
                }

                // there is unfinished page. Maybe should ask for help
                MaybeAskForHelp();

                size_t groupCount = page->CardGroupCount();
                // add thread specific offset, to separate somewhat what threads read
                size_t offset = ThreadSpecificNumber(GlobalGcIndex());
                for (size_t ii = 0; ii < groupCount; ii++)
                {
                    size_t i = (offset + ii) % groupCount;
                    int8_t groupState = page->CardGroupState(i);
                    if (groupState != Satori::CardState::BLANK)
                    {
                        uint8_t groupTicket = page->CardGroupScanTicket(i);
                        if (groupTicket == currentScanTicket)
                        {
                            continue;
                        }

                        // claim the group as complete, now we have to finish
                        page->CardGroupScanTicket(i) = currentScanTicket;

                        //NB: It is safe to get a region even if region map may be changing because
                        //    a region with remembered/dirty mark must be there and cannot be destroyed.
                        SatoriRegion* region = page->RegionForCardGroup(i);
                        _ASSERTE(region->Generation() >= 0);

                        // we should not be marking when there could be dead objects
                        _ASSERTE(!region->HasMarksSet());

                        // sometimes we set cards without knowing dst generation, but REMEMBERED only has meaning in tenured
                        if (region->Generation() < 2)
                        {
                            // Wiping the group if ephemeral+remembered is an optimization.
                            // If the group is not covered by a tenured region, we wipe the group, to not look at it again in the next scans.
                            // It must use interlocked, even if not concurrent - in case it gets dirty by mark ovrflow (it can since it is < gen2)
                            // The actual cards do not matter, we will look at cards in ephemeral regions only if group is dirty,
                            // and then cleaner will reset them appropriately.
                            // There is no marking work in this region, so ticket is also irrelevant. It will be wiped if region gets promoted.
                            // As a result cards that do not belong to tenured regions may occasionally have remembered state set.
                            // It is ignorable and we are too lazy to clean that.until the region is promoted.

                            if (groupState == Satori::CardState::REMEMBERED)
                            {
                                Interlocked::CompareExchange(&page->CardGroupState(i), Satori::CardState::BLANK, Satori::CardState::REMEMBERED);
                            }

                            continue;
                        }

                        // invariant check: when marking through cards the gen2 remset stays remset, thus should be marked through on every GC
                        // and should not fall far behind the tickets
                        _ASSERTE(groupTicket == 0 || groupTicket == 0xff || (uint8_t)(currentScanTicket - groupTicket) == 1);
                        const int8_t resetValue = Satori::CardState::REMEMBERED;
                        int8_t* cards = page->CardsForGroup(i);

                        // clean the group if dirty, but must do that before reading the cards.
                        if (groupState == Satori::CardState::DIRTY)
                        {
                            Interlocked::CompareExchange(&page->CardGroupState(i), resetValue, Satori::CardState::DIRTY);
                        }

                        for (size_t j = 0; j < Satori::CARD_BYTES_IN_CARD_GROUP; j++)
                        {
                            // cards are often sparsely set, if j is aligned, check the entire size_t for 0
                            if (((j & (sizeof(size_t) - 1)) == 0) && *((size_t*)&cards[j]) == 0)
                            {
                                j += sizeof(size_t) - 1;
                                continue;
                            }

                            // skip empty cards
                            if (!cards[j])
                            {
                                continue;
                            }

                            size_t start = page->LocationForCard(&cards[j]);
                            do
                            {
                                if (cards[j] != resetValue)
                                {
                                    // clean the card since it is going to be visited and marked through.
                                    // order relative to visiting the objects is unimportant since fields are not changing
                                    cards[j] = resetValue;
                                }
                                j++;
                            } while (j < Satori::CARD_BYTES_IN_CARD_GROUP && cards[j]);

                            size_t end = page->LocationForCard(&cards[j]);
                            size_t objLimit = min(end, region->Start() + Satori::REGION_SIZE_GRANULARITY);
                            SatoriObject* o = region->FindObject(start);
                            do
                            {
                                o->ForEachObjectRef(
                                    [&](SatoriObject** ref)
                                    {
                                        SatoriObject* child = VolatileLoadWithoutBarrier(ref);
                                        if (child &&
                                            !child->IsExternal() &&
                                            !child->IsMarkedOrOlderThan(1))
                                        {
                                            child->SetMarkedAtomic();
                                            if (!dstChunk || !dstChunk->TryPush(child))
                                            {
                                                this->PushToMarkQueuesSlow(dstChunk, child);
                                            }
                                        }
                                    }, start, end);
                                o = o->Next();
                            } while (o->Start() < objLimit);
                        }
                    }
                }

                // All groups/cards are accounted in this page - either visited or claimed.
                // No updating work is left for this page, set the ticket to indicate that.
                page->ScanTicket() = currentScanTicket;
            }
        }
    );

    if (dstChunk)
    {
        m_workList->Push(dstChunk);
    }
}

bool SatoriRecycler::HasDirtyCards()
{
    bool isDirty = false;
    m_heap->ForEachPageUntil([&](SatoriPage* page) { return page->CardState() == Satori::CardState::DIRTY ? (isDirty = true) : false; });
    return isDirty;
}

// cleaning is not concurrent, but could be parallel
void SatoriRecycler::CleanCards()
{
    SatoriWorkChunk* dstChunk = nullptr;

    m_heap->ForEachPage(
        [&](SatoriPage* page)
        {
            // NOTE: we may concurrently make cards dirty due to mark stack overflow.
            // Thus the page must be checked first, then group, then cards.
            // Dirtying due to overflow will have to do writes in the opposite order. (the barrier dirtying only orders cards after assignments)
            int8_t pageState = page->CardState();
            if (pageState >= Satori::CardState::PROCESSING)
            {
                // move the page to PROCESSING state and make sure it does not reorder past reading the card group states
                pageState = Interlocked::CompareExchange(& page->CardState(), Satori::CardState::PROCESSING, Satori::CardState::DIRTY);
                if (pageState < Satori::CardState::PROCESSING)
                {
                    return;
                }

                // there is unfinished page. Maybe should ask for help
                MaybeAskForHelp();

                size_t groupCount = page->CardGroupCount();
                // add thread specific offset, to separate somewhat what threads read
                size_t offset = ThreadSpecificNumber(GlobalGcIndex());

                for (size_t ii = 0; ii < groupCount; ii++)
                {
                    size_t i = (offset + ii) % groupCount;
                    int8_t groupState = page->CardGroupState(i);
                    if (groupState == Satori::CardState::DIRTY)
                    {
                        SatoriRegion* region = page->RegionForCardGroup(i);
                        _ASSERTE(region->Generation() >= 0);

                        // clean the group, but must do that before reading the cards.
                        const int8_t groupResetValue = region->Generation() >= 2 ? Satori::CardState::REMEMBERED : Satori::CardState::BLANK;
                        if (Interlocked::CompareExchange(&page->CardGroupState(i), groupResetValue, Satori::CardState::DIRTY) != Satori::CardState::DIRTY)
                        {
                            // at this point in time the card group is no longer dirty, try the next one
                            continue;
                        }

                        bool considerAllMarked = region->Generation() > m_condemnedGeneration;

                        int8_t* cards = page->CardsForGroup(i);
                        const int8_t resetValue = region->Generation() >= 2 ? Satori::CardState::REMEMBERED : Satori::CardState::EPHEMERAL;

                        _ASSERTE(Satori::CardState::DIRTY == (int8_t)0x04);
                        const size_t dirtyBits = 0x0404040404040404;
                        for (size_t j = 0; j < Satori::CARD_BYTES_IN_CARD_GROUP; j++)
                        {
                            // cards are often sparsely set, if j is aligned, check if any card in entire size_t is dirty
                            if (((j & (sizeof(size_t) - 1)) == 0) && (*((size_t*)&cards[j]) & dirtyBits) == 0)
                            {
                                j += sizeof(size_t) - 1;
                                continue;
                            }

                            // skip nondirty
                            if (cards[j] != Satori::CardState::DIRTY)
                            {
                                continue;
                            }

                            size_t start = page->LocationForCard(&cards[j]);
                            do
                            {
                                // clean the card since it is going to be visited and marked through.
                                cards[j++] = resetValue;
                            } while (j < Satori::CARD_BYTES_IN_CARD_GROUP && cards[j] == Satori::CardState::DIRTY);

                            size_t end = page->LocationForCard(&cards[j]);
                            size_t objLimit = min(end, region->Start() + Satori::REGION_SIZE_GRANULARITY);
                            SatoriObject* o = region->FindObject(start);

                            // do not allow card cleaning to delay until after checking IsMarked
                            if (!considerAllMarked)
                            {
                                MemoryBarrier();
                            }

                            do
                            {
                                if (!considerAllMarked)
                                {
                                    o = region->SkipUnmarked(o, objLimit);
                                    if (o->Start() == objLimit)
                                    {
                                        break;
                                    }
                                }

                                // if (considerAllMarked || o->IsMarked())
                                {
                                    o->ForEachObjectRef(
                                        [&](SatoriObject** ref)
                                        {
                                            SatoriObject* child = *ref;
                                            if (child &&
                                                !child->IsExternal() &&
                                                !child->IsMarkedOrOlderThan(m_condemnedGeneration))
                                            {
                                                child->SetMarkedAtomic();
                                                if (!dstChunk || !dstChunk->TryPush(child))
                                                {
                                                    this->PushToMarkQueuesSlow(dstChunk, child);
                                                }
                                            }
                                        }, start, end);
                                }
                                o = o->Next();
                            } while (o->Start() < objLimit);
                        }
                    }
                }

                // we do not see more cleaning work so clean the page state, use interlocked in case the page went dirty while we were working on it
                int8_t origState = Interlocked::CompareExchange(&page->CardState(), Satori::CardState::REMEMBERED, Satori::CardState::PROCESSING);
                _ASSERTE(origState != Satori::CardState::BLANK);
            }
        }
    );

    if (dstChunk)
    {
        m_workList->Push(dstChunk);
    }
}

void SatoriRecycler::UpdatePointersThroughCards()
{
    SatoriWorkChunk* dstChunk = nullptr;
    m_heap->ForEachPage(
        [&](SatoriPage* page)
        {
            int8_t pageState = page->CardState();
            _ASSERTE(pageState < Satori::CardState::PROCESSING);
            if (pageState == Satori::CardState::REMEMBERED)
            {
                uint8_t currentScanTicket = GetCardScanTicket();
                if (page->ScanTicket() == currentScanTicket)
                {
                    // continue to next page
                    return;
                }

                // there is unfinished page. Maybe should ask for help
                MaybeAskForHelp();

                size_t groupCount = page->CardGroupCount();
                // add thread specific offset, to separate somewhat what threads read
                size_t offset = ThreadSpecificNumber(GlobalGcIndex());
                for (size_t ii = 0; ii < groupCount; ii++)
                {
                    size_t i = (offset + ii) % groupCount;
                    int8_t groupState = page->CardGroupState(i);
                    _ASSERTE(groupState < Satori::CardState::PROCESSING);
                    if (groupState == Satori::CardState::REMEMBERED)
                    {
                        uint8_t groupTicket = page->CardGroupScanTicket(i);
                        if (groupTicket == currentScanTicket)
                        {
                            continue;
                        }

                        // claim the group as complete, now we have to finish
                        page->CardGroupScanTicket(i) = currentScanTicket;

                        SatoriRegion* region = page->RegionForCardGroup(i);
                        _ASSERTE(region->Generation() >= 0);

                        // Wiping the group if ephemeral+remembered is an optimization,
                        // but, since we do that, we expect that it is done by now.
                        _ASSERTE(region->Generation() >= 2);

                        // invariant check: when marking/updating through cards the gen2 remset stays remset,
                        // thus should be marked through on every GC and should not fall far behind the tickets
                        _ASSERTE(groupTicket == 0 || groupTicket == 0xff || (uint8_t)(currentScanTicket - groupTicket) == 1);
                        int8_t* cards = page->CardsForGroup(i);
                        for (size_t j = 0; j < Satori::CARD_BYTES_IN_CARD_GROUP; j++)
                        {
                            // cards are often sparsely set, if j is aligned, check the entire size_t for 0
                            if (((j & (sizeof(size_t) - 1)) == 0) && *((size_t*)&cards[j]) == 0)
                            {
                                j += sizeof(size_t) - 1;
                                continue;
                            }

                            _ASSERTE(cards[j] < Satori::CardState::PROCESSING);
                            if (cards[j] != Satori::CardState::REMEMBERED)
                            {
                                continue;
                            }

                            size_t start = page->LocationForCard(&cards[j]);
                            do
                            {
                                _ASSERTE(cards[j] < Satori::CardState::PROCESSING);
                                // wipe the card. if the card is still valid we will set it back shortly.
                                // this is an optimization. we do not need this for correctness.
                                cards[j] = Satori::CardState::BLANK;
                                j++;
                            } while (j < Satori::CARD_BYTES_IN_CARD_GROUP && cards[j] == Satori::CardState::REMEMBERED);

                            size_t end = page->LocationForCard(&cards[j]);
                            size_t objLimit = min(end, region->Start() + Satori::REGION_SIZE_GRANULARITY);
                            SatoriObject* o = region->FindObject(start);
                            do
                            {
                                o->ForEachObjectRef(
                                    [&](SatoriObject** ppObject)
                                    {
                                        // prevent re-reading o, someone else could be doing the same update.
                                        SatoriObject* child = VolatileLoadWithoutBarrier(ppObject);
                                        // ignore common cases - child is null or in the same region, also skip externals
                                        if (child &&
                                            (((size_t)ppObject ^ (size_t)child) >= Satori::REGION_SIZE_GRANULARITY) &&
                                            !child->IsExternal())
                                        {
                                            SatoriObject* newLocation;
                                            if (child->IsRelocatedTo</*notExternal*/true>(&newLocation))
                                            {
                                                VolatileStoreWithoutBarrier(ppObject, newLocation);
                                            }

                                            if (child->ContainingRegion()->Generation() < 2)
                                            {
                                                page->SetCardForAddressOnly((size_t)ppObject);
                                            }
                                        }
                                    },
                                    max(start, o->Start() + 1),  // do not include allocator object.
                                    end
                                );
                                o = o->Next();
                            } while (o->Start() < objLimit);
                        }
                    }
                }

                // All groups/cards are accounted in this page - either visited or claimed.
                // No updating work is left for this page, set the ticket to indicate that.
                page->ScanTicket() = currentScanTicket;
            }
        }
    );
}

bool SatoriRecycler::MarkHandles(int64_t deadline)
{
    ScanContext sc;
    sc.promotion = TRUE;
    sc.concurrent = !IsBlockingPhase();
    MarkContext c = MarkContext(this);
    sc._unused1 = &c;

    bool revisit = SatoriHandlePartitioner::ForEachUnscannedPartition(
        [&](int p)
        {
            // there is work for us, so maybe there is more
            MaybeAskForHelp();
            _ASSERTE(m_condemnedGeneration > 0);

            sc.thread_number = p;
            GCScan::GcScanHandles(
                IsBlockingPhase() ? MarkFn</*isConservative*/ false> : MarkFnConcurrent</*isConservative*/ false>,
                m_condemnedGeneration,
                2,
                &sc);
        },
        deadline
    );

    if (c.m_WorkChunk != nullptr)
    {
        m_workList->Push(c.m_WorkChunk);
    }

    return revisit;
}

void SatoriRecycler::ShortWeakPtrScan()
{
    SatoriHandlePartitioner::StartNextScan();
    RunWithHelp(&SatoriRecycler::ShortWeakPtrScanWorker);
}

void SatoriRecycler::ShortWeakPtrScanWorker()
{
    ScanContext sc;
    sc.promotion = TRUE;
    SatoriHandlePartitioner::ForEachUnscannedPartition(
        [&](int p)
        {
            sc.thread_number = p;
            GCScan::GcShortWeakPtrScan(m_condemnedGeneration, 2, &sc);
        }
    );
}

void SatoriRecycler::LongWeakPtrScan()
{
    SatoriHandlePartitioner::StartNextScan();
    m_syncBlockCacheScanDone = 0;
    RunWithHelp(&SatoriRecycler::LongWeakPtrScanWorker);
}

void SatoriRecycler::LongWeakPtrScanWorker()
{
    ScanContext sc;
    sc.promotion = TRUE;
    SatoriHandlePartitioner::ForEachUnscannedPartition(
        [&](int p)
        {
            sc.thread_number = p;
            GCScan::GcWeakPtrScan(m_condemnedGeneration, 2, &sc);
        }
    );

    if (!m_syncBlockCacheScanDone &&
        Interlocked::CompareExchange(&m_syncBlockCacheScanDone, 1, 0) == 0)
    {
        // scan for deleted entries in the syncblk cache
        // sc is not really used, but must have "promotion == TRUE" if HeapVerify is on.
        GCScan::GcWeakPtrScanBySingleThread(m_condemnedGeneration, 2, &sc);
    }
}

void SatoriRecycler::ScanFinalizables()
{
    m_heap->FinalizationQueue()->ResetOverflow(m_condemnedGeneration);
    RunWithHelp(&SatoriRecycler::ScanAllFinalizableRegionsWorker);
    RunWithHelp(&SatoriRecycler::QueueCriticalFinalizablesWorker);
}

void SatoriRecycler::ScanAllFinalizableRegionsWorker()
{
    MarkContext c = MarkContext(this);

    ScanFinalizableRegions(m_ephemeralFinalizationTrackingRegions, &c);
    _ASSERTE(m_reusableRegions->IsEmpty());
    
    if (m_condemnedGeneration == 2)
    {
        ScanFinalizableRegions(m_tenuredFinalizationTrackingRegions, &c);
    }

    if (c.m_WorkChunk != nullptr)
    {
        // some dead finalizables were added to F-queue and made reachable.
        // need to trace them, but also finalizer thread has work to do
        GCToEEInterface::EnableFinalization(true);
        m_workList->Push(c.m_WorkChunk);
    }
}

void SatoriRecycler::ScanFinalizableRegions(SatoriRegionQueue* queue, MarkContext* markContext)
{
    SatoriRegion* region = queue->TryPop();
    if (region)
    {
        MaybeAskForHelp();
        do
        {
            _ASSERTE(region->Generation() <= m_condemnedGeneration);
            bool hasCF = region->PendFinalizables(
                [&](SatoriObject* finalizable)
                {
                    // Only us look at finalizables from this region right now
                    // and no other marking is happening.
                    // No need for this to be atomic.
                    finalizable->SetMarked();
                    markContext->PushToMarkQueues(finalizable);
                },
                m_condemnedGeneration
            );

            if (hasCF)
            {
                m_finalizationPendingRegions->Push(region);
            }
            else
            {
                if (region->Generation() == 2)
                {
                    m_tenuredRegions->Push(region);
                }
                else
                {
                    m_ephemeralRegions->Push(region);
                }
            }
        } while ((region = queue->TryPop()));
    }
}

void SatoriRecycler::QueueCriticalFinalizablesWorker()
{
    SatoriRegion* region = m_finalizationPendingRegions->TryPop();
    if (region)
    {
        MaybeAskForHelp();
        do
        {
            region->PendCfFinalizables(m_condemnedGeneration);
            if (region->Generation() == 2)
            {
                m_tenuredRegions->Push(region);
            }
            else
            {
                m_ephemeralRegions->Push(region);
            }
        } while ((region = m_finalizationPendingRegions->TryPop()));
    }
}

void SatoriRecycler::DependentHandlesInitialScan()
{
    SatoriHandlePartitioner::StartNextScan();
    RunWithHelp(&SatoriRecycler::DependentHandlesInitialScanWorker);
}

void SatoriRecycler::DependentHandlesInitialScanWorker()
{
    ScanContext sc;
    sc.promotion = TRUE;
    MarkContext c = MarkContext(this);
    sc._unused1 = &c;

    SatoriHandlePartitioner::ForEachUnscannedPartition(
        [&](int p)
        {
            sc.thread_number = p;
            GCScan::GcDhInitialScan(MarkFn</*isConservative*/ false>, m_condemnedGeneration, 2, &sc);
        }
    );

    if (c.m_WorkChunk != nullptr)
    {
        m_workList->Push(c.m_WorkChunk);
    }
}

void SatoriRecycler::DependentHandlesRescan()
{
    SatoriHandlePartitioner::StartNextScan();
    RunWithHelp(&SatoriRecycler::DependentHandlesRescanWorker);
}

void SatoriRecycler::DependentHandlesRescanWorker()
{
    ScanContext sc;
    sc.promotion = TRUE;
    MarkContext c = MarkContext(this);
    sc._unused1 = &c;

    SatoriHandlePartitioner::ForEachUnscannedPartition(
        [&](int p)
        {
            sc.thread_number = p;
            if (GCScan::GcDhUnpromotedHandlesExist(&sc))
            {
                GCScan::GcDhReScan(&sc);
            }
        }
    );

    if (c.m_WorkChunk != nullptr)
    {
        m_workList->Push(c.m_WorkChunk);
    }
}

void SatoriRecycler::PromoteHandlesAndFreeRelocatedRegions()
{
    // NB: we may promote some objects in gen1 gc, but it is ok if handles stay young.
    //     promote handles only after en-masse promotion.
    if (m_promoteAllRegions)
    {
        SatoriHandlePartitioner::StartNextScan();
    }

    RunWithHelp(&SatoriRecycler::PromoteSurvivedHandlesAndFreeRelocatedRegionsWorker);
}

void SatoriRecycler::PromoteSurvivedHandlesAndFreeRelocatedRegionsWorker()
{
    if (m_promoteAllRegions)
    {
        ScanContext sc;
        sc.promotion = TRUE;
        // no need for scan context. we do not create more work here.
        sc._unused1 = nullptr;

        SatoriHandlePartitioner::ForEachUnscannedPartition(
            [&](int p)
            {
                sc.thread_number = p;
                // promote gen1 handles
                GCScan::GcPromotionsGranted(1, 2, &sc);
            }
        );
    }

    FreeRelocatedRegionsWorker();
}

// noLock - if the current stage can only add more regions to the allocator
void SatoriRecycler::FreeLogicallyEmptyRegion(SatoriRegion* curRegion, bool hasMarks, bool noLock)
{
    // Blank and return an unoccupied region.
    // The biggest cost here could be clearing marks, which is often is not needed
    // as logically empty regions would have nothing marked. (one exception: evacuated regions)
    // The rest is a fairly relatively cheap wiping of metadata fields.
    // Thus we do not defer freeing - once we know a region is empty, we free it.

    _ASSERTE(!curRegion->HasPinnedObjects());

    // NOTE: MakeBlank will assert that nothing is marked.
    if (hasMarks)
    {
        curRegion->ClearMarks();
    }

    bool isNurseryRegion = curRegion->IsAttachedToAllocatingOwner();
    if (isNurseryRegion)
    {
        curRegion->DetachFromAlocatingOwnerRelease();
    }

    curRegion->MakeBlank();
    if (noLock)
    {
        m_heap->Allocator()->ReturnRegionNoLock(curRegion);
    }
    else
    {
        m_heap->Allocator()->ReturnRegion(curRegion);
    }
}

void SatoriRecycler::FreeRelocatedRegionsWorker()
{
    SatoriRegion* curRegion = m_relocatedRegions->TryPop();
    if (curRegion)
    {
        MaybeAskForHelp();
        do
        {
            FreeLogicallyEmptyRegion(curRegion, /* hasMarks */ true, /* noLock */ true);
        } while ((curRegion = m_relocatedRegions->TryPop()));
    }
}

NOINLINE
void SatoriRecycler::Plan()
{
    _ASSERTE(m_ephemeralFinalizationTrackingRegions->IsEmpty());
    if (m_condemnedGeneration == 2)
    {
        _ASSERTE(m_tenuredFinalizationTrackingRegions->IsEmpty());
    }

#if _DEBUG
    auto setHasMarksFn = [&](SatoriRegion* region)
    {
        _ASSERTE(!region->HasMarksSet());
        region->HasMarksSet() = true;
    };

    // we have just marked all condemened generations
    m_ephemeralRegions->ForEachRegion(setHasMarksFn);
    if (m_condemnedGeneration == 2)
    {
        m_tenuredRegions->ForEachRegion(setHasMarksFn);
    }
#endif

    // these counters are since last blocking collection, clear them
    m_gen1AddedSinceLastCollection = 0;
    m_gen2AddedSinceLastCollection = 0;

    size_t estimatedReclaim = m_estimatedEphemeralReclaim;
    if (m_condemnedGeneration == 2)
    {
        estimatedReclaim += m_estimatedTenuredReclaim;
    }

    // gen1 is always rebuilt, clear counters
    m_occupancyAcc[1] = 0;
    m_estimatedEphemeralReclaim = 0;

    if (m_promoteAllRegions)
    {
        // we will be rebuilding gen2, one way or another
        m_occupancyAcc[2] = 0;
        m_estimatedTenuredReclaim = 0;
        m_demotedOccupancyAcc = 0;
    }

    // If we do relocation, we are committed to do pointer updates.
    // At an extreme we do not want to relocate one region
    // and then go through 100 regions and update pointers.
    //
    // As crude criteria, we will do relocations if at least some fraction
    // of condemned regions wants to participate. And at least 2 total.
    // And an average region would yield 1/2 region free space.
    // NOTE: Regions must be no more than 1/2 full to be considered candidates,
    //       but for presweepable we only guess that they would have that much.
    //       After presweep we may find otherwise. Too many mispredicted cases
    //       could add up to denying relocation after planning. That is not a
    //       problem, just some regions would be swept in STW and the rest
    //       concurrently. It is actually when we compact, then presweeping is
    //       a bit of a waste if we end up relocating the swept region.
    //       We pay presweep price for the quality of relocation. Relocation
    //       is not cheap, so better be effective.
    size_t desiredReclaim = 0;
    if (!SatoriUtil::IsForceCompact())
    {
        // Note: this is relative to region count not occupancy,
        //       since relocation has no effect on occipancy.
        if (m_condemnedGeneration == 2)
        {
            // gen2 free space may hang around for a while, so we compact if can reclaim ~1/6 of space
            desiredReclaim = (m_condemnedRegionsCount / 3 + 2) * Satori::REGION_SIZE_GRANULARITY / 2;
        }
        else
        {
            // gen1 can reuse most of free space, so compact only if ~1/2 can be reclaimed
            desiredReclaim = (m_condemnedRegionsCount / 1 + 2) * Satori::REGION_SIZE_GRANULARITY / 2;
        }
    }

    if (m_isRelocating == false || estimatedReclaim < desiredReclaim)
    {
        DenyRelocation();
        return;
    }

    // plan relocations
    RunWithHelp(&SatoriRecycler::PlanWorker);

    // The actual relocatable number could be less than the estimate due to pinning
    // or if this is gen1, which can reuse more.
    // Check again if it we are still meeting the relocation criteria.
    size_t relocatableActual = m_estimatedEphemeralReclaim;
    m_estimatedEphemeralReclaim = 0;

    // This can happen when we are too optimistic about preswept candidates
    // and they did not result in 1/2 reclaim as we thought.
    // We could have skipped presweeping, if we could know.
    //    _ASSERTE(relocatableActual <= relocatableEstimate);
    if (relocatableActual <= desiredReclaim)
    {
        m_stayingRegions->AppendUnsafe(m_relocatingRegions);
        DenyRelocation();
    }
}

void SatoriRecycler::DenyRelocation()
{
    m_isRelocating = false;

    // put all affected regions into staying queue
    if (m_promoteAllRegions)
    {
        m_stayingRegions->AppendUnsafe(m_ephemeralRegions);
        m_stayingRegions->AppendUnsafe(m_tenuredRegions);
        m_stayingRegions->AppendUnsafe(m_tenuredFinalizationTrackingRegions);
    }
    else
    {
        m_stayingRegions->AppendUnsafe(m_ephemeralRegions);
    }
}

void SatoriRecycler::PlanWorker()
{
    PlanRegions(m_ephemeralRegions);

    if (m_condemnedGeneration == 2)
    {
        PlanRegions(m_tenuredRegions);
    }
}

void SatoriRecycler::PlanRegions(SatoriRegionQueue* regions)
{
    _ASSERTE(m_isRelocating);

    SatoriRegion* curRegion = regions->TryPop();
    if (curRegion)
    {
        MaybeAskForHelp();

        do
        {
            _ASSERTE(curRegion->Generation() <= m_condemnedGeneration);

            if (curRegion->IsPreSweepCandidate(m_condemnedGeneration == 2))
            {
                curRegion->PreSweep();
            }

            if (curRegion->Occupancy() == 0)
            {
                FreeLogicallyEmptyRegion(curRegion, /* hasMarks */ false, /*noLock*/ true);
            }
            // select relocation candidates and relocation targets according to sizes.
            else
            {
                size_t reclaim = curRegion->ReclaimSizeIfRelocated(m_promoteAllRegions);
                if (reclaim)
                {
                    // we use m_estimatedEphemeralReclaim to accumulate current estimate
                    // which is likely to come up with a better estimate if we did any presweeping.
                    Interlocked::ExchangeAdd64(&m_estimatedEphemeralReclaim, reclaim);

                    // When relocating, we want to start with larger regions
                    // They are harder to fit and if cannot fit, we get extra targets.
                    // It is better to get extra targets early, to have more ways to use them.
                    if (curRegion->Occupancy() > Satori::REGION_SIZE_GRANULARITY * 2 / 5)
                    {
                        m_relocatingRegions->Push(curRegion);
                    }
                    else
                    {
                        m_relocatingRegions->Enqueue(curRegion);
                    }

                    continue;
                }
                else
                {
                    // no point to relocate
                    AddRelocationTarget(curRegion);
                }
            }
        } while ((curRegion = regions->TryPop()));
    }
}

void SatoriRecycler::AddRelocationTarget(SatoriRegion* region)
{
    int maxFreeBucket = region->GetMaxFreeBucket();
    if (maxFreeBucket < 0)
    {
        m_stayingRegions->Push(region);
    }
    else
    {
        _ASSERTE(maxFreeBucket < Satori::FREELIST_COUNT);
        // within the same bucket, we'd prefer to fill up pinned ones first
        if (region->HasPinnedObjects())
        {
            m_relocationTargets[maxFreeBucket]->Push(region);
        }
        else
        {
            m_relocationTargets[maxFreeBucket]->Enqueue(region);
        }
    }
}

SatoriRegion* SatoriRecycler::GetOrAddRelocationTarget(SatoriRegion* region, size_t allocSize)
{
    int maxFreeBucket = region->GetMaxFreeBucket();
    _ASSERTE(maxFreeBucket >= 0);
    _ASSERTE(maxFreeBucket < Satori::FREELIST_COUNT);
    _ASSERTE(!region->HasPinnedObjects());

    SatoriRegion* result = m_relocationTargets[maxFreeBucket]->PopOrPush(region);
    if (result)
    {
        size_t allocStart = result->StartAllocating(allocSize);
        _ASSERTE(allocStart);
    }

    return result;
}

SatoriRegion* SatoriRecycler::TryGetRelocationTarget(size_t allocSize, bool existingRegionOnly)
{
    //make this occasionally fail in debug to be sure we can handle low memory case.
#if _DEBUG
    if (allocSize % 1024 == 0)
    {
        return nullptr;
    }
#endif

    int32_t bucket = SatoriUtil::BucketForAlloc(allocSize);
    _ASSERTE(bucket >= 0);

    // Note: If we ever want to relocate more than half region (to force compaction or whatever),
    // we will not find any suitable buckets.
    // That is ok, it will have to be to a fresh region, which will always fit.
    for (; bucket < Satori::FREELIST_COUNT; bucket++)
    {
        SatoriRegionQueue* queue = m_relocationTargets[bucket];
        if (queue)
        {
            SatoriRegion* region = queue->TryPop();
            if (region)
            {
                size_t allocStart = region->StartAllocating(allocSize);
                _ASSERTE(allocStart);
                return region;
            }
        }
    }

    if (existingRegionOnly)
    {
        return nullptr;
    }

    _ASSERTE(SatoriRegion::RegionSizeForAlloc(allocSize) == Satori::REGION_SIZE_GRANULARITY);
    SatoriRegion* newRegion = m_heap->Allocator()->GetRegion(Satori::REGION_SIZE_GRANULARITY);
    if (newRegion)
    {
        newRegion->SetGeneration(m_condemnedGeneration);
        newRegion->DoNotSweep() = true;
        if (newRegion->Generation() == 2)
        {
            newRegion->RearmCardsForTenured();
        }
    }

    return newRegion;
}

void SatoriRecycler::Relocate()
{
    if (m_isRelocating)
    {
        RunWithHelp(&SatoriRecycler::RelocateWorker);
    }
}

void SatoriRecycler::AddTenuredRegionsToPlan(SatoriRegionQueue* regions)
{
    _ASSERTE(m_isRelocating);

    SatoriRegion* curRegion = regions->TryPop();
    if (curRegion)
    {
        MaybeAskForHelp();
        do
        {
            // we mark only condemened generations
            _ASSERTE(!curRegion->HasMarksSet());
            // condemned regions should go through Plan
            _ASSERTE(curRegion->Generation() > m_condemnedGeneration);
            AddRelocationTarget(curRegion);
        } while ((curRegion = regions->TryPop()));
    }
}

void SatoriRecycler::RelocateWorker()
{
    _ASSERTE(m_isRelocating);

    if (m_condemnedGeneration != 2 && m_promoteAllRegions)
    {
        AddTenuredRegionsToPlan(m_tenuredRegions);
        AddTenuredRegionsToPlan(m_tenuredFinalizationTrackingRegions);
    }

    SatoriRegion* curRegion = m_relocatingRegions->TryPop();
    if (curRegion)
    {
        MaybeAskForHelp();
        do
        {
            RelocateRegion(curRegion);
        } while ((curRegion = m_relocatingRegions->TryPop()));
    }
}

void SatoriRecycler::RelocateRegion(SatoriRegion* relocationSource)
{
    // the region must be in after marking and before sweeping state
    _ASSERTE(relocationSource->HasMarksSet());
    // TODO: VS we could relocate demoted if next GC is Full GC,
    //       but would need to free the gen2 trackers.
    //       Also see ReclaimSizeIfRelocated, which assumes these do not relocate.
    //       Think if that is worth the trouble.
    _ASSERTE(!relocationSource->IsDemoted());

    relocationSource->Verify(true);

    size_t maxBytesToCopy = relocationSource->Occupancy();

    // If the region could be used as a target for itself,
    // there could be more like this, do not ask for a free region.
    // We would rather make this one a target of relocations.
    bool existingRegionOnly = relocationSource->GetMaxFreeBucket() >= SatoriUtil::BucketForAlloc(maxBytesToCopy);
    SatoriRegion* relocationTarget = TryGetRelocationTarget(maxBytesToCopy, existingRegionOnly);

    if (!relocationTarget)
    {
        if (existingRegionOnly)
        {
            // we did not found a target, but the region could fit into its own free.
            // we will make it a target instead.
            // however, there could be be more like this, and we do not want all become targets,
            // so we use GetOrAdd.
            relocationTarget = GetOrAddRelocationTarget(relocationSource, maxBytesToCopy);
            if (!relocationTarget)
            {
                return;
            }
        }
        else
        {
            // we could not get a region. we must be low on available memory.
            // we can still try using the source region as a target for other relocations.
            AddRelocationTarget(relocationSource);
            return;
        }
    }

    // transfer finalization trackers if we have any
    relocationTarget->TakeFinalizerInfoFrom(relocationSource);

    // allocate space for relocated objects
    size_t dst = relocationTarget->Allocate(maxBytesToCopy, /*zeroInitialize*/ false);

    // actually relocate src objects into the allocated space.
    size_t dstOrig = dst;
    size_t objLimit = relocationSource->Start() + Satori::REGION_SIZE_GRANULARITY;
    SatoriObject* o = relocationSource->FirstObject();

    // the target is typically marked, unless it is freshly allocated or in a higher generation
    // preserve marked state by copying marks
    bool relocationIsPromotion = relocationTarget->Generation() > m_condemnedGeneration;
    bool needToCopyMarks = !(relocationIsPromotion || relocationTarget->DoNotSweep());
    _ASSERTE(relocationTarget->HasMarksSet() == needToCopyMarks);

    int32_t objectsRelocated = 0;
    do
    {
        o = relocationSource->SkipUnmarked(o);
        if (o->Start() >= objLimit)
        {
            _ASSERTE(o->Start() == objLimit);
            break;
        }

        _ASSERTE(o->IsMarked());
        _ASSERTE(!o->IsFree());
        _ASSERTE(!o->IsUnmovable());

        size_t size = o->Size();
        memcpy((void*)(dst - sizeof(size_t)), (void*)(o->Start() - sizeof(size_t)), size);
        // record the new location of the object by storing it in the syncblock space.
        *((size_t*)o - 1) = dst;

        if (needToCopyMarks)
        {
            ((SatoriObject*)dst)->SetMarked();
        }

        relocationTarget->SetIndicesForObject((SatoriObject*)dst, dst + size);

        dst += size;
        objectsRelocated++;
        o = (SatoriObject*)(o->Start() + size);
    } while (o->Start() < objLimit);

    size_t used = dst - dstOrig;
    // we have new marks, but have not swept at this point, so could use less.
    _ASSERTE(used <= maxBytesToCopy);

    relocationTarget->StopAllocating(dst);

    // just update the obj count, if target is not marked this will be correct
    // otherwise sweep will update anyways.
    relocationTarget->SetOccupancy(relocationTarget->Occupancy(), relocationTarget->ObjCount() + objectsRelocated);

    // the target may yet have more space and be a target for more relocations.
    AddRelocationTarget(relocationTarget);

    if (objectsRelocated > 0)
    {
        relocationSource->IsRelocated() = true;
        if (relocationIsPromotion)
        {
            relocationTarget->AcceptedPromotedObjects() = true;
            m_relocatedToHigherGenRegions->Push(relocationSource);
        }
        else
        {
            m_relocatedRegions->Push(relocationSource);
        }
    }
    else
    {
        // relocationSource happened to be empty
        FreeLogicallyEmptyRegion(relocationSource, /* hasMarks */ false, /* noLock */ false);
    }
}

NOINLINE
void SatoriRecycler::Update()
{
    // if we ended up not moving anything, this is no longer a relocating GC.
    m_isRelocating = m_relocatedRegions->Count() > 0 ||
        m_relocatedToHigherGenRegions->Count() > 0;

    m_CurrentGcInfo->m_compaction = m_isRelocating;

    if (m_isRelocating)
    {
        IncrementRootScanTicket();
        SatoriHandlePartitioner::StartNextScan();
        if (m_condemnedGeneration != 2)
        {
            IncrementCardScanTicket();
        }

        m_workerGate->WakeAll();
    }

    RunWithHelp(&SatoriRecycler::UpdateRootsWorker);

    // regions must me updated 
    // after updating through cards since region update may change generations

    _ASSERTE(m_ephemeralRegions->IsEmpty());
    _ASSERTE(m_ephemeralFinalizationTrackingRegions->IsEmpty());

    // The following must be reset in Plan, just want to be sure
    // it did not change since then.
    // We will be rebuilding these counters, possibly in deferred sweep.
    _ASSERTE(m_occupancyAcc[0] == 0);
    _ASSERTE(m_occupancyAcc[1] == 0);
    _ASSERTE(m_estimatedEphemeralReclaim == 0);
    _ASSERTE(m_gen1AddedSinceLastCollection == 0);
    _ASSERTE(m_gen2AddedSinceLastCollection == 0);

    _ASSERTE(m_promoteAllRegions || m_condemnedGeneration != 2);
    if (m_promoteAllRegions)
    {
        _ASSERTE(m_tenuredRegions->IsEmpty());
        _ASSERTE(m_tenuredFinalizationTrackingRegions->IsEmpty());
        _ASSERTE(m_occupancyAcc[2] == 0);
        _ASSERTE(m_demotedOccupancyAcc == 0);
        _ASSERTE(m_estimatedTenuredReclaim == 0);
    }

    // enable accumulating and reporting
    m_occupancyReportingEnabled = true;

    RunWithHelp(&SatoriRecycler::UpdateRegionsWorker);

    // promoting handles must be after handles are updated (since update needs to know unpromoted generations).
    // relocated regions can be freed after live regions are updated (since update gets new locations from relocated regions).
    // we will combine these two passes here after both prerequisites are complete.
    PromoteHandlesAndFreeRelocatedRegions();

    if (m_promoteAllRegions)
    {
        // the following must be after cards are reset
        m_heap->ForEachPage(
            [](SatoriPage* page)
            {
                page->CardState() = Satori::CardState::BLANK;
                page->ScanTicket() = 0;
            }
        );
    }

}

void SatoriRecycler::UpdateRootsWorker()
{
    if (m_isRelocating)
    {
        ScanContext sc;
        sc.promotion = FALSE;
        MarkContext c = MarkContext(this);
        sc._unused1 = &c;

        SatoriHandlePartitioner::ForEachUnscannedPartition(
            [&](int p)
            {
                // there is work for us, so maybe there is more
                MaybeAskForHelp();

                sc.thread_number = p;
                GCScan::GcScanHandles(UpdateFn</*isConservative*/ false>, m_condemnedGeneration, 2, &sc);
            }
        );

        if (SatoriUtil::IsConservativeMode())
            //generations are meaningless here, so we pass -1
            GCToEEInterface::GcScanRoots(UpdateFn<true>, -1, -1, &sc);
        else
            GCToEEInterface::GcScanRoots(UpdateFn<false>, -1, -1, &sc);

        SatoriFinalizationQueue* fQueue = m_heap->FinalizationQueue();
        if (fQueue->TryUpdateScanTicket(this->GetRootScanTicket()) &&
            fQueue->HasItems())
        {
            fQueue->ForEachObjectRef(
                [&](SatoriObject** ppObject)
                {
                    SatoriObject* o = *ppObject;
                    SatoriObject* newLocation;
                    if (o->IsRelocatedTo</*notExternal*/true>(&newLocation))
                    {
                        *ppObject = newLocation;
                    }
                }
            );
        }

        _ASSERTE(c.m_WorkChunk == nullptr);

        UpdatePointersInPromotedObjects();

        if (m_condemnedGeneration != 2)
        {
            UpdatePointersThroughCards();
        }
   }
}

void SatoriRecycler::UpdateRegionsWorker()
{
    // update and return target regions
    for (int i = 0; i < Satori::FREELIST_COUNT; i++)
    {
        UpdateRegions(m_relocationTargets[i]);
    }

    // update and return staying regions
    UpdateRegions(m_stayingRegions);

    // if we saw large objects we may have ranges to update
    if (!m_workList->IsEmpty())
    {
        UpdatePointersInObjectRanges();
    }
}

void SatoriRecycler::UpdatePointersInObjectRanges()
{
    SatoriWorkChunk* srcChunk = m_workList->TryPop();
    if (srcChunk)
    {
        MaybeAskForHelp();
        do
        {
            _ASSERTE(srcChunk->IsRange());
            SatoriObject* o;
            size_t start, end;
            srcChunk->GetRange(o, start, end);
            srcChunk->Clear();
            m_heap->Allocator()->ReturnWorkChunk(srcChunk);

            // update children in the range
            o->ForEachObjectRef(
                [](SatoriObject** ppObject)
                {
                    SatoriObject* child = *ppObject;
                    // ignore common cases - child is null or in the same region
                    if (child &&
                        (((size_t)ppObject ^ (size_t)child) >= Satori::REGION_SIZE_GRANULARITY))
                    {
                        SatoriObject* newLocation;
                        if (child->IsRelocatedTo(&newLocation))
                        {
                            *ppObject = newLocation;
                        }
                    }
                },
                start,
                end);
        } while ((srcChunk = m_workList->TryPop()));
    }
}

void SatoriRecycler::UpdatePointersInPromotedObjects()
{
    SatoriRegion* curRegion;
    while ((curRegion = m_relocatedToHigherGenRegions->TryPop()))
    {
        m_promoteAllRegions ?
            curRegion->UpdatePointersInPromotedObjects<true>():
            curRegion->UpdatePointersInPromotedObjects<false>();

        m_relocatedRegions->Push(curRegion);
    }
}

void SatoriRecycler::UpdateRegions(SatoriRegionQueue* queue)
{
    SatoriRegion* curRegion = queue->TryPop();
    if (curRegion)
    {
        MaybeAskForHelp();
        do
        {
            if (!m_promoteAllRegions && curRegion->IsPromotionCandidate())
            {
                RecordDemotedOccupancy(-(ptrdiff_t)curRegion->DemotedOccupancy());
                curRegion->IndividuallyPromote();
            }

            if (curRegion->IsPreSwept())
            {
                _ASSERTE (curRegion->Generation() <= m_condemnedGeneration);
                curRegion->FinishSweepForPreSwept();
            }

            if (curRegion->Generation() > m_condemnedGeneration &&
                !curRegion->IndividuallyPromoted())
            {
                // this can only happen when promoting in gen1
                _ASSERTE(m_promoteAllRegions);
                curRegion->DoNotSweep() = true;
                if (curRegion->AcceptedPromotedObjects())
                {
                    curRegion->UpdateFinalizableTrackers();
                    curRegion->AcceptedPromotedObjects() = false;
                }
            }
            else if (m_isRelocating)
            {
                // this can happen when relocation adds a region.
                if (curRegion->DoNotSweep())
                {
                    curRegion->UpdatePointers();
                }
                else
                {
                    if (!curRegion->Sweep</*updatePointers*/ true>())
                    {
                        FreeLogicallyEmptyRegion(curRegion, /* hasMarks */ false, /*noLock*/ true);
                        continue;
                    }
                }

                curRegion->UpdateFinalizableTrackers();
            }

            // recycler owns nursery regions only temporarily, we should not keep them.
            if (curRegion->IsAttachedToAllocatingOwner())
            {
                // when promoting, all nursery regions should be detached
                _ASSERTE(!m_promoteAllRegions);
                if (!m_isRelocating)
                {
                    curRegion->Sweep</*updatePointers*/ false>();
                }

                if (curRegion->Occupancy() == 0)
                {
                    FreeLogicallyEmptyRegion(curRegion, /* hasMarks */ false, /*noLock*/ true);
                }
                else
                {
                    RecordOccupancy(curRegion->Generation(), curRegion->Occupancy());
                    curRegion->DoNotSweep() = false;
                }

                continue;
            }

            if (m_promoteAllRegions)
            {
                curRegion->SetGeneration(2);
                curRegion->RearmCardsForTenured();
            }

            // make sure the region is swept and returned now, or later
            if (!SatoriUtil::IsConcurrentEnabled())
            {
                SweepAndReturnRegion(curRegion);
                continue;
            }

            // this happens when relocation allocates a region or promotes.         
            if (curRegion->DoNotSweep() && curRegion->Occupancy() > 0)
            {
                // no point to defer these, we will not do anything more with them
                KeepRegion(curRegion);
                continue;
            }

            // these we can sweep/return later
            if (curRegion->IsReuseCandidate())
            {
                m_deferredSweepRegions->Push(curRegion);
            }
            else
            {
                m_deferredSweepRegions->Enqueue(curRegion);
            }
        } while ((curRegion = queue->TryPop()));
    }
}

void SatoriRecycler::KeepRegion(SatoriRegion* curRegion)
{
    _ASSERTE(curRegion->Occupancy() > 0);
    _ASSERTE(curRegion->Generation() > 0);

    curRegion->DoNotSweep() = false;
    curRegion->ReusableFor() = SatoriRegion::ReuseLevel::None;
    if (curRegion->IsReuseCandidate())
    {
        _ASSERTE(curRegion->Size() == Satori::REGION_SIZE_GRANULARITY);
        if ((curRegion->Generation() == 2) && curRegion->TryDemote(m_nextGcIsFullGc))
        {
            RecordDemotedOccupancy(curRegion->DemotedOccupancy());
        }

        if (curRegion->Generation() == 1)
        {
#if _DEBUG
            // just split 50%/50% for testing purposes.
            curRegion->ReusableFor() = curRegion->ObjCount() % 2 == 0 ?
                SatoriRegion::ReuseLevel::Gen0 :
                SatoriRegion::ReuseLevel::Gen1;

#else
            // TUNING: heuristic for gen0
            //         the cost here is inability to trace concurrently at all.
            curRegion->ReusableFor() = curRegion->Occupancy() < (Satori::MAX_ESCAPE_SIZE / 4)?
                                            SatoriRegion::ReuseLevel::Gen0 :
                                            SatoriRegion::ReuseLevel::Gen1;
#endif
            // we can't mark through gen0 candidates concurrently, so we would not put demoted there
            // it would work, but could reduce concurrent gen2 efficiency.
            if (!SatoriUtil::IsGen0Enabled() || curRegion->IsDemoted())
            {
                curRegion->ReusableFor() = SatoriRegion::ReuseLevel::Gen1;
            }
        }
    }

    //
    // finally return the region back into recycler queues
    //

    RecordOccupancy(curRegion->Generation(), curRegion->Occupancy());
    if (curRegion->Generation() >= 2)
    {
        PushToTenuredQueues(curRegion);
    }
    else
    {
        if (curRegion->IsReusable())
        {
            _ASSERTE(curRegion->Size() <= Satori::REGION_SIZE_GRANULARITY);
            if (curRegion->HasFreeSpaceInTopBucket())
            {
                m_reusableRegions->Enqueue(curRegion);
            }
            else
            {
                m_reusableRegions->Push(curRegion);
            }
        }
        else
        {
            PushToEphemeralQueues(curRegion);
        }
    }
}

void SatoriRecycler::DrainDeferredSweepQueue()
{
    SatoriRegion* curDeferredRegion = m_deferredSweepRegions->TryPop();
    if (curDeferredRegion)
    {
        MaybeAskForHelp();
        do
        {
            SweepAndReturnRegion(curDeferredRegion);
            Interlocked::Decrement(&m_deferredSweepCount);
        } while ((curDeferredRegion = m_deferredSweepRegions->TryPop()));
    }

    // we are blocked, we no longer need reusables.
    _ASSERTE(IsBlockingPhase());
    SatoriRegion* curReusableRegion = m_reusableRegions->TryPop();
    if (curReusableRegion)
    {
        MaybeAskForHelp();
        do
        {
            curReusableRegion->ReusableFor() = SatoriRegion::ReuseLevel::None;
            PushToEphemeralQueues(curReusableRegion);
        } while ((curReusableRegion = m_reusableRegions->TryPop()));
    }

    if (SatoriUtil::IsConservativeMode())
    {
        m_trimmer->WaitForStop();
    }
}

bool SatoriRecycler::DrainDeferredSweepQueueConcurrent(int64_t deadline)
{
    bool isWorkerGCThread = IsWorkerThread();

    if (isWorkerGCThread)
    {
        SatoriFinalizationQueue* q = m_heap->FinalizationQueue();
        if (q->OverflowedGen() != 0)
        {
            MaybeAskForHelp();
            q->TryResizeIfOverflowing();
        }
    }

    // Sweeping and returning could be contentious.
    // If there are workers and we are in a low latency mode, do not participate.
    if (isWorkerGCThread || m_activeWorkers <= 0 || !m_isLowLatencyMode)
    {
        SatoriRegion* curRegion = m_deferredSweepRegions->TryPop();
        if (curRegion)
        {
            MaybeAskForHelp();
            do
            {
                SweepAndReturnRegion(curRegion);
                Interlocked::Decrement(&m_deferredSweepCount);

                // ignore deadline on worker threads, we can't do anything else anyways.
                if (!isWorkerGCThread && deadline && (minipal_hires_ticks() - deadline > 0))
                {
                    break;
                }
            } while ((curRegion = m_deferredSweepRegions->TryPop()));
        }
    }

    // no work that we can claim, but we must wait for sweeping to finish and
    // in conservative mode we can't start marking while trimming may change regions,
    // let other threads do their things.
    int cycles = 0;
    while (m_deferredSweepCount ||
        (SatoriUtil::IsConservativeMode() && m_trimmer->IsActive()))
    {
        // user threads should not wait, just say we have more work
        if (!isWorkerGCThread)
        {
            return true;
        }

        YieldProcessor();
        if ((++cycles & 127) == 0)
        {
            GCToOSInterface::YieldThread(0);
        }
    }

    _ASSERTE(m_deferredSweepCount == 0 &&
        (!SatoriUtil::IsConservativeMode() || !m_trimmer->IsActive()));

    return false;
}

void SatoriRecycler::DrainDeferredSweepQueueWorkerFn()
{
    _ASSERTE(IsWorkerThread());

    SatoriRegion* curRegion = m_deferredSweepRegions->TryPop();
    if (curRegion)
    {
        MaybeAskForHelp();
        do
        {
            SweepAndReturnRegion(curRegion);
            Interlocked::Decrement(&m_deferredSweepCount);
        } while (m_activeWorkerFn && (curRegion = m_deferredSweepRegions->TryPop()));
    }
}

void SatoriRecycler::SweepAndReturnRegion(SatoriRegion* curRegion)
{
    if (!curRegion->DoNotSweep())
    {
        curRegion->Sweep</*updatePointers*/ false>();
    }

    if (curRegion->Occupancy() == 0)
    {
        FreeLogicallyEmptyRegion(curRegion, /* hasMarks */ false, /*noLock*/ IsBlockingPhase());
    }
    else
    {
        KeepRegion(curRegion);
    }
}

void SatoriRecycler::RecordOccupancy(int generation, size_t occupancy)
{
    _ASSERTE(m_occupancyReportingEnabled);
    Interlocked::ExchangeAdd64(&m_occupancyAcc[generation], occupancy);
}

void SatoriRecycler::RecordDemotedOccupancy(ptrdiff_t demotedOccupancy)
{
    _ASSERTE(m_occupancyReportingEnabled);
    if (demotedOccupancy != 0)
    {
        Interlocked::ExchangeAdd64(&m_demotedOccupancyAcc, (size_t)demotedOccupancy);
    }
}

size_t SatoriRecycler::GetOccupancy(int i)
{
    if (i < 0 || i > 2)
        return 0;

    return m_occupancy[i];
}

size_t SatoriRecycler::GetTotalOccupancy()
{
    return m_occupancy[0] +
           m_occupancy[1] +
           m_occupancy[2];
}

size_t SatoriRecycler::GetGcStartMillis(int generation)
{
    return m_gcStartMillis[generation];
}

size_t SatoriRecycler::GetGcDurationMillis(int generation)
{
    return m_gcDurationUsecs[generation] / 1000;
}

size_t SatoriRecycler::GetGcAccumulatingDurationMillis(int generation)
{
    return m_gcAccmulatingDurationUsecs[generation];
}

bool& SatoriRecycler::IsLowLatencyMode()
{
    return m_isLowLatencyMode;
}

void SatoriRecycler::UpdateGcCounters(int64_t blockingStart)
{
    // Compute Time in GC
    int64_t currentPerfCounterTimer = minipal_hires_ticks();

    int64_t totalTimeInCurrentGc = currentPerfCounterTimer - blockingStart;
    int64_t timeSinceLastGcEnded = currentPerfCounterTimer - m_totalTimeAtLastGcEnd;

    // should always hold unless we switch to a nonmonotonic timer.
    _ASSERTE(timeSinceLastGcEnded >= totalTimeInCurrentGc);

    m_percentTimeInGcSinceLastGc = timeSinceLastGcEnded != 0 ? (int)(totalTimeInCurrentGc * 100 / timeSinceLastGcEnded) : 0;
    m_totalTimeAtLastGcEnd = currentPerfCounterTimer;
}
