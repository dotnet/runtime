// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_REFERENCE_COUNTING_H
#define LXR_REFERENCE_COUNTING_H

#include "immix_allocation.h"
#include "large_object_space.h"
#include "reference_count.h"
#include "lxr_pause_coordinator.h"
#include "lxr_virtual_array.h"

class Object;
class LxrReferenceCounting;

enum class LxrRcResult : uint8_t
{
    Success,
    InvalidArgument,
    InvalidPhase,
    NotInitialized,
    OutOfMemory,
    CapacityExceeded,
    MetadataFailure,
    AllocationFailure,
    SchedulerFailure,
    UnsupportedLifetime,
    InvariantViolation,
};

enum class LxrRcPhase : uint8_t
{
    Uninitialized,
    Mutator,
    Suspended,
    Sweeping,
    Faulted,
    Shutdown,
};

struct LxrRcConfiguration
{
    size_t immixBytes;
    size_t largeObjectBytes;
    size_t maximumMutators;
    size_t maximumRoots;
    size_t workers;
    uint8_t referenceCountBits;
    size_t immixReservationBytes = 0;
    size_t largeObjectReservationBytes = 0;
    size_t hardLimitBytes = 0;
    size_t minimumReuseLines = 1;
    size_t bufferSize = 1024;
    bool lazyDecrements = true;
    bool lineRecycling = true;
    bool copyLineRecycling = true;
    bool evacuateNursery = false;
    size_t maximumYoungCopyBytes = SIZE_MAX;
    uint32_t forwardingMask = 3;
    uint32_t forwardingShift = 0;
    bool evacuateMature = false;
    size_t defragHeadroomPercent = 5;
    size_t chunkDefragPercent = 32;
};

struct LxrRcMetrics
{
    uint64_t allocations;
    uint64_t allocatedBytes;
    uint64_t loggedSlots;
    uint64_t incrementEntries;
    uint64_t decrementEntries;
    uint64_t bufferFlushes;
    uint64_t threadFlushes;
    uint64_t promotions;
    uint64_t deaths;
    uint64_t rootIncrements;
    uint64_t rootDecrements;
    uint64_t objectScans;
    uint64_t objectSizeQueries;
    uint64_t oldTargetChecks;
    uint64_t childRegistrations;
    uint64_t blocksReleased;
    uint64_t largeObjectBytesReleased;
    uint64_t epochs;
};

using LxrRcCaptureRoots = LxrRcResult (*)(LxrReferenceCounting* engine, void* context);
using LxrRcObservedEntry = void (*)(bool increment, uintptr_t value, Object* current, void* context);
using LxrRcUnlogObservedSlot = void (*)(Object** slot, void* context);
using LxrRcWalkObject = bool (*)(Object* object, size_t completeSize, void* context);
using LxrRcWalkForwarded = bool (*)(Object* source, Object* destination, size_t completeSize, void* context);
using LxrRcTraceClosure = LxrRcResult (*)(LxrReferenceCounting* engine, void* context);

struct LxrRcObjectModel
{
    GetLoaderAllocatorObjectSlot getLoaderAllocatorSlot;
    bool admitFinalizable;
    LxrRcTraceClosure finishTrace;
    void* context;
    bool (*isImmortal)(Object* object, void* context);
    LxrRcTraceClosure finishEpoch;
    bool (*admitRange)(size_t firstSlot, size_t slotCount, void* context) = nullptr;
    LxrRcTraceClosure finishLazy = nullptr;
    bool (*canAdmitRange)(size_t firstSlot, size_t slotCount, size_t nativeBytes, void* context) = nullptr;
    bool (*isPinned)(Object* object, void* context) = nullptr;
    void (*didCopy)(Object* source, Object* destination, void* context) = nullptr;
    LxrRcTraceClosure finishRelocation = nullptr;
};

struct LxrNurseryMetrics
{
    uint64_t copiedObjects;
    uint64_t copiedBytes;
    uint64_t inPlacePinnedBytes;
    uint64_t copyFailures;
    uint64_t forwardingWaits;
    uint64_t retiredSourceBytes;
    size_t pauseCopiedBytes;
};

struct LxrMatureMetrics
{
    uint64_t eligibleBlocks;
    uint64_t selectedBlocks;
    uint64_t pinnedBlocks;
    uint64_t estimatedCopyBytes;
    uint64_t copiedObjects;
    uint64_t copiedBytes;
    uint64_t copyFailures;
    uint64_t rememberedEntries;
    uint64_t staleEntries;
    uint64_t replayedEntries;
    uint64_t retiredSourceBytes;
    uint64_t releasedBlocks;
    uint64_t invalidSlots;
    size_t pauseCopiedBytes;
};

enum class LxrTracePhase : uint8_t
{
    Inactive,
    InitialMark,
    Concurrent,
    FinalMark,
    Full,
};

struct LxrTraceMetrics
{
    uint64_t initialMarks;
    uint64_t finalMarks;
    uint64_t fullMarks;
    uint64_t objects;
    uint64_t references;
    uint64_t satbEntries;
    uint64_t deletionMarks;
    uint64_t forcedScans;
    uint64_t splitPackets;
    uint64_t cycleObjects;
    uint64_t cycleBytes;
    uint64_t interruptions;
    uint64_t concurrentSlices;
    uint64_t checkpointInterruptions;
};

// Wall-clock timers for the phases of one collection. They use a few timestamps at phase and
// packet boundaries, never one per object. They only observe; no collector decision reads them.
class LxrPauseTimers final
{
public:
    enum Phase : size_t
    {
        Total, LockWait, PolicyDrain, PreDrain, SuspendRuntime, SuspendEE, SuspendPrepare,
        RootCapture, PrepareWork, ReleaseEpoch, RetireSources, ReclaimNursery, SweepBlocks, ReleaseFlip,
        Scan0, Scan1, Scan2, Scan3, ScanBeforeRoots, ScanLeaseFinalizers, ScanRoots, ScanHandles,
        ScanProfilerHandles, ScanDependent, ScanSyncBlock, ScanFrozen, ScanDhInitial, ScanShortWeak,
        ScanQueueFinalizers, ScanTraceFinalizers, ScanDhRescan, ScanWeakPtr, ScanWeakPtrSingle,
        ScanRelocateRoots, ScanRelocateHandles, ScanPromotionsGranted, ScanHeapStatistics,
        ResumeRuntime, StwPause, PostDrain,
        ScalarCount,
    };

    // Each group publishes wall span (first packet start to last packet end), busy time and packet count.
    // Trace packets are not timed: a trace cycle runs hundreds of thousands of them, and two timestamps per
    // packet cost a measurable share of the run (see P17). Satb packets are the only trace-related group.
    enum Group : size_t
    {
        Increments, Decrements, Satb, PrepareTrace, WeakClosure, EpochClosure, NurserySweep, MatureSweep,
        GroupCount,
    };

    static constexpr size_t PublishedCount = ScalarCount + 3 * GroupCount;
    static constexpr size_t MetaSequence = 0;
    static constexpr size_t MetaKind = 1;
    static constexpr size_t MetaTimestamps = 2;
    static constexpr size_t MetaCost = 3;

    static int64_t Now();
    static int64_t Frequency();

    LxrPauseTimers();
    void Configure(bool enabled) { m_enabled.store(enabled, std::memory_order_relaxed); }
    bool Enabled() const { return m_enabled.load(std::memory_order_relaxed); }
    int64_t Stamp()
    {
        m_timestamps.fetch_add(1, std::memory_order_relaxed);
        return Now();
    }
    void Begin(int64_t waited);
    void Add(size_t phase, int64_t ticks)
    {
        m_scalar[phase].fetch_add(ticks, std::memory_order_relaxed);
    }
    void AddPacket(Group group, int64_t start, int64_t end);
    void MarkPause(uint8_t kind);
    void End();
    uint64_t GetMeta(size_t index) const;
    uint64_t GetLast(size_t index) const;
    uint64_t GetTotal(size_t index) const;

private:
    struct GroupState
    {
        std::atomic<int64_t> busy{0};
        std::atomic<int64_t> first{INT64_MAX};
        std::atomic<int64_t> last{0};
        std::atomic<uint64_t> packets{0};
    };

    static uint64_t ToNanoseconds(int64_t ticks);

    std::atomic<bool> m_enabled{true};
    std::atomic<int64_t> m_scalar[ScalarCount];
    GroupState m_groups[GroupCount];
    std::atomic<uint64_t> m_sequence{0};
    std::atomic<uint64_t> m_kind{0};
    std::atomic<bool> m_paused{false};
    std::atomic<uint64_t> m_timestamps{0};
    std::atomic<uint64_t> m_published_timestamps{0};
    std::atomic<uint64_t> m_last[PublishedCount];
    std::atomic<uint64_t> m_total[PublishedCount];
    int64_t m_begin = 0;
    uint64_t m_begin_timestamps = 0;
};

class LxrPhaseScope final
{
public:
    LxrPhaseScope(LxrPauseTimers& timers, size_t phase)
        : m_timers(timers), m_phase(phase), m_start(timers.Enabled() ? timers.Stamp() : 0)
    {
    }

    ~LxrPhaseScope()
    {
        if (m_start != 0)
        {
            m_timers.Add(m_phase, m_timers.Stamp() - m_start);
        }
    }

    LxrPhaseScope(const LxrPhaseScope&) = delete;
    LxrPhaseScope& operator=(const LxrPhaseScope&) = delete;

private:
    LxrPauseTimers& m_timers;
    size_t m_phase;
    int64_t m_start;
};

class LxrRcMutator final
{
public:
    LxrRcMutator();
    LxrRcMutator(const LxrRcMutator&) = delete;
    LxrRcMutator& operator=(const LxrRcMutator&) = delete;

private:
    friend class LxrReferenceCounting;
    LxrReferenceCounting* engine;
    std::atomic<uint8_t> state;
    std::atomic<uintptr_t> identity;
    uintptr_t owner;
    ImmixBumpAllocator allocator;
    void* increments;
    void* decrements;
    uintptr_t publishedCursor;
    uintptr_t publishedLimit;
    uintptr_t birthBlock;
    uintptr_t pendingFastStart;
    size_t pendingFastSize;
};

class LxrReferenceCounting final
{
public:
    static constexpr size_t BufferCapacity = 1024;
    static constexpr size_t FullTraceBatchCapacity = 8192;

    LxrReferenceCounting();
    ~LxrReferenceCounting();
    LxrReferenceCounting(const LxrReferenceCounting&) = delete;
    LxrReferenceCounting& operator=(const LxrReferenceCounting&) = delete;

    LxrRcResult Initialize(
        const LxrRcConfiguration& configuration,
        const LxrPauseRuntimeAdapter& runtime,
        const LxrRcObjectModel* objectModel = nullptr);
    LxrRcResult InitializeObserver(size_t admittedSlots, size_t maximumMutators);
    LxrRcResult CaptureClaimedWrite(LxrRcMutator* mutator, Object** slot, Object* oldTarget);
    LxrRcResult DrainObservedLogs(
        LxrRcUnlogObservedSlot unlog, LxrRcObservedEntry observe, void* context);
    void Shutdown();
    LxrRcResult AttachMutator(void* identity, LxrRcMutator** mutator);
    LxrRcResult DetachMutator(LxrRcMutator* mutator);
    LxrRcMutator* FindMutator(void* identity) const;
    LxrRcMutator* FindCachedMutator(void* cached, void* identity) const;
    LxrRcResult GetAllocationSpan(LxrRcMutator* mutator, uintptr_t* cursor, uintptr_t* limit);
    LxrRcResult RetireAllocationSpan(LxrRcMutator* mutator, uintptr_t cursor);
    LxrRcResult RecordFastAllocation(LxrRcMutator* mutator, Object* object, size_t size, bool* fast);
    LxrRcResult Allocate(LxrRcMutator* mutator, size_t completeSize, Object** object);
    LxrRcResult CompleteAllocation(
        Object* object, size_t completeSize, AllocationCompleteFlags flags, LxrRcMutator* mutator = nullptr);
    LxrRcResult LogBeforeWrite(
        LxrRcMutator* mutator, Object** slot, Object* oldTarget);
    LxrRcResult Collect(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult BeginTrace(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult FinishTrace(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult CollectFull(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult AddRoot(Object* object, bool interior = false, bool trace = true);
    LxrRcResult TraceRoot(Object* object);
    Object* ResolveForwarded(Object* object) const;
    LxrRcResult RelocateRoot(Object** slot, bool interior, bool pinned) const;
    LxrNurseryMetrics GetNurseryMetrics() const;
    LxrMatureMetrics GetMatureMetrics() const;
    LxrRcResult GetDefragHeadroomBytes(size_t* bytes) const;
    LxrRcResult GetUsedPages(size_t* pages) const;
    uint64_t GetCostMetric(size_t metric) const;
    LxrPauseTimers& GetPauseTimers() { return m_pause_timers; }
    const LxrPauseTimers& GetPauseTimers() const { return m_pause_timers; }
    bool IsMarked(Object* object) const;
    bool IsTraceExhausted() const;
    LxrTracePhase GetTracePhase() const;
    LxrTraceMetrics GetTraceMetrics() const;
    LxrRcResult HoldTrace(bool hold);
    LxrRcResult ArmScanCheckpoint(Object* object);
    bool IsScanCheckpointActive() const;
    LxrRcResult Drain(uint32_t timeoutMilliseconds);
    LxrRcResult LoadCount(Object* object, uintptr_t* count) const;
    LxrRcResult FindContainingObject(uintptr_t address, Object** object) const;
    LxrRcMetrics GetMetrics() const;
    LxrRcResult GetFailure() const;
    LxrRcPhase GetPhase() const;
    uintptr_t GetFieldMetadataBase() const;
    uintptr_t GetReservationStart() const;
    size_t GetReservationBytes() const;
    size_t GetLiveBytes() const;
    uint64_t GetReclaimedBytes() const;
    bool OwnsAddress(uintptr_t address) const;
    size_t GetObjectExtent(Object* object) const;
    LxrRcResult ArmLifetimeProbe(Object* object);
    uint64_t GetLifetimeProbeBirths() const;
    uint64_t GetLifetimeProbeReclaims() const;
    uint64_t GetStorageMetric(size_t metric) const;
    LxrRcResult EnableStorageDiagnostics(uint32_t timeout);
    LxrRcResult GetStorageRange(size_t component, uintptr_t* start, size_t* bytes) const;
    size_t GetObjectStorageIndex(size_t heapSlot) const;
    size_t GetObjectStorageSlots() const;
    Object* FindNextObject(Object* object) const;
    size_t GetMarkedBytes() const;
    size_t GetCommittedBytes() const;
    size_t GetAdmittedBytes() const;
    size_t GetNativePlaneCommittedBytes() const;
    size_t FindNextOccupiedSlot(size_t first) const;
    LxrRcResult Grow(
        bool large, size_t minimumBytes, LxrRcCaptureRoots capture, void* context, uint32_t timeout);
    uint64_t GetGrowthCount() const;
    LxrRcResult RequestGrowth(bool large, size_t minimumBytes);
    LxrRcResult GetGrowthResult() const;
    size_t GetImmixReservationBytes() const;
    size_t GetImmixCapacityBytes() const;
    size_t GetYoungAllocationBytes() const;
    size_t GetCleanNurseryBlocks() const;
    size_t GetPendingIncrementEntries() const;
    LxrRcResult GetMaturePages(size_t* pages) const;
    LxrRcResult DrainLazySweep(uint32_t timeout);
    LxrRcResult GetHeapStatistics(size_t* heapBytes, size_t* fragmentationBytes) const;
    LxrRcResult StopWorkers();
    LxrRcResult WalkObjects(LxrRcWalkObject visit, void* context) const;
    LxrRcResult WalkForwarded(LxrRcWalkForwarded visit, void* context) const;
    bool HasPendingDetaches() const;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    LxrRcResult MeasureLogCopy(size_t width, size_t* copies, uint64_t* nanoseconds);
    LxrRcResult ValidateLogStorageForTest(uint8_t test);
    void HoldLogReaderForTest(bool hold);
    bool IsLogReaderHeldForTest() const;
    LxrRcResult ArmPendingReader(Object* object);
    LxrRcResult ReleasePendingReader();
    uintptr_t GetPendingReaderValue() const;
    uintptr_t GetPendingReaderInitialValue() const;
    uintptr_t GetPendingReaderConsumedValue() const;
    uint64_t GetPendingReaderId() const;
    uint64_t GetPendingReaderCompletions() const;
    LxrRcResult WatchRememberedSlot(Object** slot);
    uint64_t GetRememberedPayloadReads() const;
    uintptr_t GetMatureScanReceipt(size_t field) const;
    LxrRcResult HoldTracePacketsForTest(bool hold);
    size_t GetCompletedScanUsers(Object* object) const;
    LxrRcResult ValidateTraceBatchForTest(Object* object, size_t count);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

private:
    struct LogChunk;
    struct LogStream;
    struct LogBank;
    struct Work;
    struct DeathContext;
    struct ReferenceBatch;
    struct TraceBatch;
    struct TraceScan;
    struct RememberedChunk;
    class CostScope;
    enum class WorkKind : uint8_t;

    static void Suspend(void* context);
    static void Resume(bool finishedGc, void* context);
    static void Execute(LxrWorkPacket* packet, size_t worker, void* context);
    static LxrCompletionCallbackResult DecrementsDrained(
        LxrMatureSweepContinuation* continuation, void* context);
    static LxrCompletionCallbackResult OverallDrained(uint64_t generation, void* context);
    static SideMetadataResult Death(uintptr_t start, void* context);

    LxrRcResult Fail(LxrRcResult result);
    LxrRcResult InitializeBuffers(size_t slots);
    bool CommitPlanes(size_t first, size_t count);
    bool CommitTracePlanes();
    size_t GetTracePlaneBudget(size_t immixBytes, size_t largeBytes) const;
    LxrRcResult GrowSuspended();
    LxrRcResult FromMetadata(SideMetadataResult result);
    LxrRcResult FromScheduler(LxrWorkSchedulerResult result);
    LxrRcResult Append(LxrRcMutator* mutator, bool increment, uintptr_t value);
    LxrRcResult Publish(void** chunk);
    LxrRcResult ResetBank(LogBank& bank);
    LxrRcResult ReadPublishedRange(
        const LogStream& stream, size_t cursor, size_t limit, size_t* count);
    LxrRcResult PublishReferences(
        LogStream& stream, size_t capacity, const uintptr_t* references, size_t count);
    LxrRcResult FlushMutators();
    LxrRcResult PrepareWork();
    Work* NewWork(
        WorkKind kind, const uintptr_t* references, size_t count, LxrWorkBucketStage stage,
        bool trace = true, TraceScan* scan = nullptr, bool borrow = false, bool logged = false);
    LxrRcResult SubmitWork(Work* work, const LxrWorkPacket* parent = nullptr);
    void DeleteWork();
    void ReleaseWork(Work* work);
    void RetireCompletedWork();
    LxrRcResult ProcessIncrementWork(Work* work, size_t worker, TraceBatch* targets);
    LxrRcResult ProcessFullIncrementWork(Work* work, size_t worker);
    LxrRcResult ProcessIncrement(
        Object** object, bool trace, ReferenceBatch* children, size_t worker, TraceBatch* targets);
    LxrRcResult TryCopyNursery(
        Object* source, Object** destination, size_t worker, bool* copied,
        bool* incrementedInPlace, uintptr_t* previous);
    LxrRcResult AllocateCopy(size_t size, size_t worker, uintptr_t* start);
    LxrRcResult TryCopyMature(Object** object);
    LxrRcResult SelectMatureCandidates();
    LxrRcResult RetainPinnedMatureBlocks();
    bool IsMatureCandidate(uintptr_t address);
    LxrRcResult RememberMatureSlot(Object** slot, Object* target);
    LxrRcResult ReplayMatureRememberedSlots();
    void DeleteRememberedChunks();
    LxrRcResult FixForwardingReaders();
    LxrRcResult RetireForwardedSources();
    LxrRcResult ScanIncrements(Object* object, bool trace, ReferenceBatch* children);
    LxrRcResult ProcessDecrements(Work* work, LxrWorkPacket* packet);
    LxrRcResult ScanDecrements(Object* object, ReferenceBatch* children);
    LxrRcResult Sweep(const LxrBlockReleaseGuard* guard);
    LxrRcResult ReleaseEpoch();
    LxrRcResult Reclaim(uintptr_t start);
    LxrRcResult ReclaimNursery();
    LxrRcResult ReclaimLargeNursery();
    Object* GetLoaderAllocator(Object* object) const;
    LxrRcResult RunPause(
        LxrPauseKind kind, LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult DrainEpoch(uint32_t timeoutMilliseconds);
    LxrRcResult QueueTrace(
        const uintptr_t* objects, size_t count, TraceScan* scan = nullptr, bool logged = false);
    LxrRcResult PublishTraceWork();
    LxrRcResult ProcessTraceWork(Work* work);
    LxrRcResult ProcessFullTraceWork(Work* work);
    LxrRcResult ProcessTraceWork(Work* work, TraceBatch* targets);
    LxrRcResult ProcessTraceObject(Object* object, size_t* directBudget, TraceBatch* targets);
    LxrRcResult ProcessTraceScan(TraceScan* scan, bool finish, TraceBatch* targets = nullptr);
    LxrRcResult ScanTraceLocked(TraceScan* scan, bool finish, TraceBatch* targets = nullptr);
    LxrRcResult FinishDyingTraceObject(Object* object);
    LxrRcResult DrainTraceInsidePause();
    LxrRcResult PrepareTrace();
    LxrRcResult SweepCycles();
    void EnterTraceLock(size_t index);
    void ExitTraceLock(size_t index);
    LxrRcResult GetObjectSize(Object* object, size_t* size);
    bool IsImmix(uintptr_t start) const;
    bool IsLarge(uintptr_t start) const;
    static uintptr_t Start(Object* object);
    static Object* Reference(uintptr_t start);
    std::atomic<size_t>& ExtentSlot(size_t heapSlot) const;
    std::atomic<uint8_t>& TraceLockSlot(size_t heapSlot) const;
    TraceScan*& TraceScanSlot(size_t heapSlot) const;

    LxrRcConfiguration m_configuration;
    LxrRcObjectModel m_object_model;
    bool m_observer;
    LxrPauseRuntimeAdapter m_runtime;
    LxrRcCaptureRoots m_capture;
    void* m_capture_context;
    LxrSideMetadataLayout m_layout;
    SideMetadataManager m_metadata;
    ImmixBlockManager m_blocks;
    ReferenceCountTable m_counts;
    ImmixAllocationManager m_allocation;
    LxrLargeObjectSpace m_large;
    LxrWorkScheduler m_scheduler;
    LxrPauseCoordinator m_coordinator;
    void* m_arena;
    std::atomic<size_t>* m_extents;
    LxrVirtualArray<std::atomic<size_t>> m_extent_storage;
    LxrVirtualArray<std::atomic<uint8_t>> m_trace_lock_storage;
    LxrVirtualArray<TraceScan*> m_trace_scan_storage;
    LxrVirtualArray<std::atomic<uint64_t>> m_live_storage;
    LxrVirtualArray<std::atomic<uint8_t>> m_allocated_block_storage;
    std::atomic<uint64_t>* m_live_objects = nullptr;
    std::atomic<size_t> m_immix_capacity{0};
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    std::atomic<bool> m_log_reader_hold{false};
    std::atomic<bool> m_log_reader_active{false};
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    size_t m_initial_immix = 0;
    size_t m_initial_large = 0;
    size_t m_grow_bytes = 0;
    bool m_grow_large = false;
    LxrRcResult m_growth_result = LxrRcResult::Success;
    std::atomic<uint64_t> m_growth_count{0};
    std::atomic<size_t> m_epoch_fresh_blocks{0};
    std::atomic<size_t> m_epoch_recycled_lines{0};
    std::atomic<size_t> m_live_bytes;
    std::atomic<uint64_t> m_reclaimed_bytes;
    std::atomic<uintptr_t> m_probe_start{0};
    std::atomic<uint64_t> m_probe_births{0};
    std::atomic<uint64_t> m_probe_reclaims{0};
    std::atomic<uint64_t> m_storage_metrics[9]{};
    std::atomic<uint64_t> m_trace_batch_metrics[5]{};
    std::atomic<bool> m_storage_diagnostics{false};
    std::atomic<uint8_t>* m_trace_locks;
    TraceScan** m_trace_scans;
    std::atomic<TraceScan*> m_scan_work;
    std::atomic<LxrTracePhase> m_trace_phase;
    std::atomic<bool> m_marking;
    std::atomic<bool> m_hold_trace;
    std::atomic<uintptr_t> m_scan_checkpoint;
    std::atomic<bool> m_scan_checkpoint_active;
    std::atomic<uint64_t> m_trace_pending;
    std::atomic<uint64_t> m_trace_metrics[14];
    LxrPauseKind m_pause_kind;
    bool m_cycle_sweep;
    LxrRcMutator* m_mutators;
    ImmixBumpAllocator* m_copy_allocators = nullptr;
    std::atomic<bool> m_copy_exhausted{false};
    std::atomic<bool> m_mature_copy_exhausted{false};
    mutable std::atomic<uint64_t> m_nursery_metrics[6]{};
    std::atomic<size_t> m_pause_copied_bytes{0};
    std::atomic<uint8_t>* m_evacuation_sources = nullptr;
    std::atomic<RememberedChunk*> m_remembered_chunks{nullptr};
    std::atomic<uint64_t> m_mature_metrics[13]{};
    std::atomic<uint64_t> m_cost_ticks[9]{};
    LxrPauseTimers m_pause_timers;
    std::atomic<size_t> m_pause_mature_copied_bytes{0};
    LogChunk* m_mutator_chunks;
    LogBank* m_banks;
    uintptr_t* m_roots[2];
    size_t m_root_counts[2];
    size_t m_current_roots;
    uintptr_t* m_dead_large;
    std::atomic<uint8_t>* m_mature_candidates;
    std::atomic<size_t> m_dead_large_count;
    std::atomic<size_t> m_producer_bank;
    std::atomic<Work*> m_work;
    std::atomic<Work*> m_trace_todo;
    std::atomic<uint64_t> m_next_id;
    std::atomic<LxrRcPhase> m_phase;
    std::atomic<LxrRcResult> m_failure;
    std::atomic<uint64_t> m_metrics[18];
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    std::atomic<uintptr_t> m_reader_target{0};
    std::atomic<Work*> m_reader_work{nullptr};
    std::atomic<bool> m_reader_held{false};
    std::atomic<uintptr_t> m_reader_initial{0};
    std::atomic<uintptr_t> m_reader_consumed{0};
    std::atomic<uint64_t> m_reader_id{0};
    std::atomic<uint64_t> m_reader_completions{0};
    std::atomic<uintptr_t> m_watched_remembered_slot{0};
    std::atomic<uint64_t> m_remembered_payload_reads{0};
    std::atomic<uintptr_t> m_mature_scan_receipt[4]{};
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
};

static_assert(LxrReferenceCounting::BufferCapacity == LxrDefaultRcPacketReferenceCapacity);
static_assert(LxrReferenceCounting::BufferCapacity <= UINT16_MAX);

#endif // LXR_REFERENCE_COUNTING_H
