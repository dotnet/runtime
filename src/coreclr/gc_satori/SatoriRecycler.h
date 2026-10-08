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
// SatoriRecycler.h
//

#ifndef __SATORI_RECYCLER_H__
#define __SATORI_RECYCLER_H__

#include "common.h"
#include "../gc.h"
#include "SatoriRegionQueue.h"
#include "SatoriWorkList.h"
#include "SatoriGate.h"

class SatoriHeap;
class SatoriTrimmer;
class SatoriRegion;
class MarkContext;

struct LastRecordedGcInfo
{
    size_t m_index;
    size_t m_pauseDurations[2];
    uint32_t m_pausePercentage;
    uint8_t m_condemnedGeneration;
    bool m_compaction;
    bool m_concurrent;
};

class SatoriRecycler
{
    friend class MarkContext;

public:
    void Initialize(SatoriHeap* heap);

    void AddEphemeralRegion(SatoriRegion* region);
    void AddTenuredRegion(SatoriRegion* region);

    size_t GetNowMillis();
    size_t GetNowUsecs();

    bool& IsLowLatencyMode();

    void Collect(int generation, bool force, bool blocking);
    int GetCondemnedGeneration();
    int GetRootScanTicket();
    size_t IncrementGen0Count();
    int64_t GetCollectionCount(int gen);

    void TryStartGC(int generation, gc_reason reason);
    void HelpOnce();
    void MaybeTriggerGC(gc_reason reason);
    bool IsBlockingPhase();

    bool ShouldDoConcurrent(int generation);
    void ConcurrentWorkerFn();
    void ShutDown();

    void BlockingMarkForConcurrentImpl();
    void BlockingMarkForConcurrent();
    void MaybeAskForHelp();

    SatoriRegion* TryGetReusable();
    SatoriRegion* TryGetReusableForLarge();

    void ReportThreadAllocBytes(int64_t bytes, bool isLive);
    int64_t GetTotalAllocatedBytes();

    void RecordOccupancy(int generation, size_t size);
    void RecordDemotedOccupancy(ptrdiff_t demotedOccupancy);
    void UpdateGenerationOccupancies();
    size_t GetTotalOccupancy();
    size_t GetOccupancy(int i);
    size_t GetGcStartMillis(int generation);
    size_t GetGcDurationMillis(int generation);
    size_t GetGcAccumulatingDurationMillis(int generation);

    int64_t GlobalGcIndex();

    void ScheduleMarkAsChildRanges(SatoriObject* o);
    bool ScheduleUpdateAsChildRanges(SatoriObject* o);

    inline bool IsBarrierConcurrent()
    {
        return m_isBarrierConcurrent;
    }

    inline bool IsNextGcFullGc()
    {
        return m_nextGcIsFullGc;
    }

    inline int GetPercentTimeInGcSinceLastGc()
    {
        return m_percentTimeInGcSinceLastGc;
    }

    LastRecordedGcInfo* GetLastGcInfo(gc_kind kind)
    {
        if (kind == gc_kind_ephemeral)
            return &m_lastEphemeralGcInfo;

        if (kind == gc_kind_full_blocking)
            return &m_lastTenuredGcInfo; // no concept of background GC, every GC has blocking part.

        if (kind == gc_kind_background)
            return GetLastGcInfo(gc_kind_any); // no concept of background GC, cant have 2 GCs at a time.

        // if (kind == gc_kind_any)
        return m_lastTenuredGcInfo.m_index > m_lastEphemeralGcInfo.m_index ?
            &m_lastTenuredGcInfo :
            &m_lastEphemeralGcInfo;
    };

private:
    SatoriHeap* m_heap;

    int m_rootScanTicket;
    uint8_t m_cardScanTicket;

    SatoriWorkList* m_workList;
    SatoriTrimmer* m_trimmer;

    // regions owned by recycler
    SatoriRegionQueue* m_ephemeralRegions;
    SatoriRegionQueue* m_ephemeralFinalizationTrackingRegions;
    SatoriRegionQueue* m_ephemeralWithUnmarkedDemoted;

    SatoriRegionQueue* m_tenuredRegions;
    SatoriRegionQueue* m_tenuredFinalizationTrackingRegions;

    // temporary store while processing finalizables
    SatoriRegionQueue* m_finalizationPendingRegions;

    // temporary store for planning and relocating
    SatoriRegionQueue* m_stayingRegions;
    SatoriRegionQueue* m_relocatingRegions;
    SatoriRegionQueue* m_relocationTargets[Satori::FREELIST_COUNT];
    SatoriRegionQueue* m_relocatedRegions;
    SatoriRegionQueue* m_relocatedToHigherGenRegions;

    // store regions for concurrent sweep
    SatoriRegionQueue* m_deferredSweepRegions;

    // regions that could be reused for Gen1
    SatoriRegionQueue* m_reusableRegions;
    SatoriRegionQueue* m_reusableRegionsAlternate;

    static const int GC_STATE_NONE = 0;
    static const int GC_STATE_CONCURRENT = 1;
    static const int GC_STATE_BLOCKING = 2;
    static const int GC_STATE_BLOCKED = 3;

    volatile int m_gcState;

    static const int CC_MARK_STATE_NONE = 0;
    static const int CC_MARK_STATE_SUSPENDING_EE = 1;
    static const int CC_MARK_STATE_MARKING = 2;
    static const int CC_MARK_STATE_DONE = 3;

    static const int CC_CLEAN_STATE_NOT_READY = 0;
    static const int CC_CLEAN_STATE_WAIT_FOR_HELPERS = 1;
    static const int CC_CLEAN_STATE_SETTING_UP = 2;
    static const int CC_CLEAN_STATE_CLEANING = 3;
    static const int CC_CLEAN_STATE_DONE = 4;

    volatile int m_ccStackMarkState;
    volatile int m_ccStackMarkingThreadsNum;

    volatile int m_ccHelpersNum;

    int m_syncBlockCacheScanDone;

    int m_condemnedGeneration;

    bool m_concurrentCardsDone;
    bool m_concurrentHandlesDone;
    volatile int m_concurrentCleaningState;

    bool m_isRelocating;
    bool m_isLowLatencyMode;
    bool m_promoteAllRegions;
    volatile bool m_isBarrierConcurrent;

    int m_prevCondemnedGeneration;

    int64_t m_gcCount[3];
    int64_t m_compactingGcCount[3];

    int64_t m_gcStartMillis[3];
    int64_t m_gcDurationUsecs[3];
    int64_t m_gcAccmulatingDurationUsecs[3];

    int64_t m_totalTimeAtLastGcEnd;
    int m_percentTimeInGcSinceLastGc;

    size_t m_gen1Budget;
    size_t m_totalLimit;
    bool m_nextGcIsFullGc;

    size_t m_condemnedRegionsCount;
    size_t m_deferredSweepCount;
    size_t m_gen1AddedSinceLastCollection;
    size_t m_gen2AddedSinceLastCollection;
    size_t m_gen1CountAtLastGen2;
    size_t m_gcNextTimeTarget;

    size_t m_occupancy[3];
    size_t m_occupancyAcc[3];
    size_t m_demotedOccupancyAcc;
    bool m_occupancyReportingEnabled;

    size_t m_estimatedEphemeralReclaim;
    size_t m_estimatedTenuredReclaim;
    size_t m_promotionEstimate;

    int64_t m_currentAllocBytesLiveThreads;
    int64_t m_currentAllocBytesDeadThreads;
    int64_t m_totalAllocBytes;

    int64_t m_perfCounterTicksPerMilli;
    int64_t m_perfCounterTicksPerMicro;

    SatoriGate* m_workerGate;

    volatile int m_gateSignaled;
    volatile int m_workerWoken;
    volatile int m_activeWorkers;
    volatile int m_totalWorkers;

    void(SatoriRecycler::* volatile m_activeWorkerFn)();

    int64_t m_noWorkSince;

    LastRecordedGcInfo m_lastEphemeralGcInfo;
    LastRecordedGcInfo m_lastTenuredGcInfo;
    LastRecordedGcInfo* m_CurrentGcInfo;

    size_t m_startMillis;

private:
    size_t Gen1RegionCount();
    size_t Gen2RegionCount();
    size_t RegionCount();

    static void DeactivateFn(gc_alloc_context* context, void* param);
    static void ConcurrentPhasePrepFn(gc_alloc_context* gcContext, void* param);

    template <bool isConservative>
    static void MarkFn(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags);

    template <bool isConservative>
    static void UpdateFn(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags);

    template <bool isConservative>
    static void MarkFnConcurrent(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags);

    static void WorkerThreadMainLoop(void* param);
    int MaxWorkers();
    int64_t HelpQuantum();
    void AskForHelp();
    void RunWithHelp(void(SatoriRecycler::* method)());
    bool HelpOnceCore(bool minQuantum);
    bool HelpOnceCoreInner(bool minQuantum);

    void PushToEphemeralQueues(SatoriRegion* region);
    void PushToTenuredQueues(SatoriRegion* region);

    void AdjustHeuristics();
    void DeactivateAllocatingRegions();

    void IncrementRootScanTicket();
    void IncrementCardScanTicket();
    uint8_t GetCardScanTicket();

    void MarkOwnStack(gc_alloc_context* aContext, MarkContext* markContext);
    void MarkThroughCards();
    bool MarkThroughCardsConcurrent(int64_t deadline);
    void MarkDemoted(SatoriRegion* curRegion, MarkContext* markContext);
    void MarkAllStacksFinalizationAndDemotedRoots();

    void PushToMarkQueuesSlow(SatoriWorkChunk*& currentWorkChunk, SatoriObject* o);
    void DrainMarkQueues(SatoriWorkChunk* srcChunk = nullptr);
    void MarkOwnStackAndDrainQueues();
    void MarkOwnStackOrDrainQueuesConcurrent(int64_t deadline);
    bool MarkDemotedAndDrainQueuesConcurrent(int64_t deadline);
    void PushOrReturnWorkChunk(SatoriWorkChunk * srcChunk);
    bool DrainMarkQueuesConcurrent(SatoriWorkChunk* srcChunk = nullptr, int64_t deadline = 0);

    bool HasDirtyCards();
    bool CleanCardsConcurrent(int64_t deadline);
    void CleanCards();
    bool MarkHandles(int64_t deadline = 0);
    void ShortWeakPtrScan();
    void ShortWeakPtrScanWorker();
    void LongWeakPtrScan();
    void LongWeakPtrScanWorker();

    void ScanFinalizables();
    void ScanFinalizableRegions(SatoriRegionQueue* regions, MarkContext* markContext);
    void ScanAllFinalizableRegionsWorker();
    void QueueCriticalFinalizablesWorker();

    void DependentHandlesScan();
    void DependentHandlesInitialScan();
    void DependentHandlesInitialScanWorker();
    void DependentHandlesRescan();
    void DependentHandlesRescanWorker();

    void BlockingCollect();
    // for profiling purposes Gen1 and Gen2 GC have distinct entrypoints, but the same implementation
    void BlockingCollect1();
    void BlockingCollect2();
    void BlockingCollectImpl();

    void BlockingMark();
    void MarkNewReachable();
    void DrainAndCleanWorker();
    void MarkStrongReferences();
    void MarkStrongReferencesWorker();

    void Plan();
    void PlanWorker();
    void PlanRegions(SatoriRegionQueue* regions);
    void DenyRelocation();

    void AddTenuredRegionsToPlan(SatoriRegionQueue* regions);
    void AddRelocationTarget(SatoriRegion* region);
    SatoriRegion* TryGetRelocationTarget(size_t size, bool existingRegionOnly);
    SatoriRegion* GetOrAddRelocationTarget(SatoriRegion * region, size_t allocSize);

    void Relocate();
    void RelocateWorker();
    void RelocateRegion(SatoriRegion* region);
    void FreeLogicallyEmptyRegion(SatoriRegion* curRegion, bool hasMarks, bool noLock);
    void FreeRelocatedRegionsWorker();

    void PromoteHandlesAndFreeRelocatedRegions();
    void PromoteSurvivedHandlesAndFreeRelocatedRegionsWorker();

    void Update();
    void UpdateRootsWorker();
    void UpdateRegionsWorker();
    void UpdatePointersThroughCards();
    void UpdatePointersInObjectRanges();
    void UpdatePointersInPromotedObjects();
    void UpdateRegions(SatoriRegionQueue* queue);

    void KeepRegion(SatoriRegion* curRegion);
    void DrainDeferredSweepQueue();
    bool DrainDeferredSweepQueueConcurrent(int64_t deadline = 0);
    void DrainDeferredSweepQueueWorkerFn();
    void SweepAndReturnRegion(SatoriRegion* curRegion);

    void UpdateGcCounters(int64_t blockingStart);

    void ASSERT_NO_WORK();
};

#endif
