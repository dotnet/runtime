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
    // P17 S5b. A pause may admit blocks beyond the admitted Immix capacity for its copy and give them back at its end.
    // The engine uses this only when no hard limit is set (the hard limit equals the whole reservation) and the
    // reservation is larger than the initial capacity. With a hard limit below the reservation it does nothing.
    bool overshootToSpace = false;
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
    // initialize: write the zero value into every element. A range that the operating system has just committed is
    // zero already, and a borrowed range is mostly never used, so the borrow skips the writes (P17 S5b).
    bool (*admitRange)(size_t firstSlot, size_t slotCount, bool initialize, void* context) = nullptr;
    LxrRcTraceClosure finishLazy = nullptr;
    bool (*canAdmitRange)(size_t firstSlot, size_t slotCount, size_t nativeBytes, void* context) = nullptr;
    bool (*isPinned)(Object* object, void* context) = nullptr;
    void (*didCopy)(Object* source, Object* destination, void* context) = nullptr;
    LxrRcTraceClosure finishRelocation = nullptr;
    // P17 S5b: releases the per-object storage of a range that the engine gives back (the reverse of admitRange).
    // tailIsLast: no admitted range follows the one that is given back in the storage, so a shared page may be released.
    bool (*releaseRange)(size_t firstSlot, size_t slotCount, bool tailIsLast, void* context) = nullptr;
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
        // Added in P17 S4: time from the end of the last pause packet (ReleaseEpoch) to the engine Resume callback, and
        // the time that Resume spends in PublishTraceWork. Both are inside the stop-the-world time.
        ResumeGap, PublishTrace,
        // Added in P17 S5b: admission of blocks beyond the capacity at the start of a pause, and the give-back of a free
        // tail at its end (including the decommit of the memory and of the planes).
        Borrow, GiveBack,
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
    // requireCounted: the address may lie outside every live object (a remembered slot of a dead holder). The
    // ValidObject bit of a dead young object stays set, and its memory may be overwritten, so only an object with a
    // count above zero may be read. Use it after the increments of a pause.
    LxrRcResult FindContainingObject(uintptr_t address, Object** object, bool requireCounted = false) const;
    LxrRcMetrics GetMetrics() const;
    LxrRcResult GetFailure() const;
    LxrRcPhase GetPhase() const;
    uintptr_t GetFieldMetadataBase() const;
    uintptr_t GetReservationStart() const;
    size_t GetReservationBytes() const;
    // Estimate for the .NET memory APIs: blocks in use times the block size, plus the committed large-object
    // bytes. It is updated at block state changes, never per object, so it includes dead objects that the
    // collector has not yet released.
    size_t GetUsedBytesEstimate() const;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    // Exact per-object accounting. Only the test platform keeps it, as an oracle for the sweep checks.
    size_t GetLiveBytes() const;
    uint64_t GetReclaimedBytes() const;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    bool OwnsAddress(uintptr_t address) const;
    // Whether the address is the start of an object that the engine registered: the ValidObject bit is set.
    // The bit stays set for a dead young object until its range is reused or its block is released, so
    // "start of an object" does not mean "live". Use the reference count (above zero) for liveness after the
    // increments of a pause.
    bool IsObjectStart(Object* object) const;
    // Size of a live object (ValidObject bit set and reference count above zero), or of a large object that is
    // registered in the large-object space (from its allocation record). Zero for anything else.
    size_t GetLiveObjectSize(Object* object) const;
    bool IsLargeObject(Object* object) const;
    // Set while a tracing pause has dead cycles whose lazy sweep has not finished. The sweep reads the size of a
    // dead object from the object itself, so the type data of its collectible loader allocator must stay valid.
    // The collector holds back finalization (the only place where the runtime frees a loader allocator) while this
    // is true.
    bool IsCycleSweepPending() const;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    // Size of each object, kept only by the test platform as an oracle: product code never reads it.
    size_t GetObjectExtent(Object* object) const;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
    // Next slot at or after first whose ValidObject bit is set, or the reservation size in slots when there is none.
    // Stale bits of dead young objects are included.
    size_t FindNextValidSlot(size_t first) const;
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
    size_t GetImmixFreePages() const;
    // P17 S5b: overshoot of the to-space. When it is enabled, the runtime suspend callback tells the engine how many
    // pages the pause may copy (RequestBorrow). If fewer Immix pages are free, the pause admits the missing blocks
    // from the reservation, and it gives the free tail of the capacity back before the epoch flips.
    bool IsOvershootEnabled() const { return m_overshoot; }
    void RequestBorrow(size_t copyPages) { m_borrow_request_pages = copyPages; }
    // Bytes of the capacity above the part that is kept (the initial capacity and regular growth).
    size_t GetBorrowedBytes() const;
    size_t GetImmixFloorBytes() const { return m_immix_floor; }
    enum OvershootMetric : size_t
    {
        BorrowPauses, BorrowBytesTotal, BorrowBytesLast, BorrowBytesMax, BorrowRefused, GiveBackAttempts,
        GiveBackSuccesses, GiveBackComplete, GiveBackBytesTotal, GiveBackBytesLast, BorrowedNow, BorrowNsTotal,
        GiveBackNsTotal, BorrowNsLast, GiveBackNsLast, PeakCapacityBytes, EmptyCopyBlocksReleased,
        BorrowRequestPages, BorrowFreePages, OvershootMetricCount,
    };
    uint64_t GetOvershootMetric(size_t metric) const;
    size_t GetPauseSurvivorBytes() const;
    LxrRcResult GetMaturePages(size_t* pages) const;
    LxrRcResult DrainLazySweep(uint32_t timeout);
    // Block counts and the estimate that the .NET memory APIs report. All of them are read from counters that block
    // state transitions maintain, so the call neither walks the heap nor touches a block.
    //   heapBytes: mature Immix blocks (state other than Unallocated) times the block size, plus the large-object
    //     pages that are not nursery.
    //   fragmentationBytes: free lines of Reusable blocks times the line size, plus the padding of large objects.
    //     A block that the last pause promoted in place is counted after the lazy sweep has looked at it.
    LxrRcResult GetHeapStatistics(size_t* heapBytes, size_t* fragmentationBytes) const;
    size_t GetNonFreeBlocks() const;
    // Compares the block counters with a walk over every block. For tests and Debug builds only.
    bool BlockCountersMatchWalk(size_t* walkedNonFree = nullptr, size_t* walkedFreeLines = nullptr) const;
    LxrRcResult StopWorkers();
    LxrRcResult WalkObjects(LxrRcWalkObject visit, void* context) const;
    // Visits every object start whose ValidObject bit is set, stale bits included, without reading the object.
    // The size passed to the callback is zero. Use it for per-object flag walks that run before the increments.
    LxrRcResult WalkObjectStarts(LxrRcWalkObject visit, void* context) const;
    LxrRcResult WalkForwarded(LxrRcWalkForwarded visit, void* context) const;
    bool HasPendingDetaches() const;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    LxrRcResult MeasureLogCopy(size_t width, size_t* copies, uint64_t* nanoseconds);
    LxrRcResult ValidateLogStorageForTest(uint8_t test);
    void HoldLogReaderForTest(bool hold);
    bool IsLogReaderHeldForTest() const;
    // Holds the lazy block sweep after it has started, so that a test can start a pause while it runs.
    void HoldSweepForTest(bool hold);
    bool IsSweepHeldForTest() const;
    LxrRcResult GetBlockStateForTest(
        size_t block, ImmixBlockState* state, uint8_t* unavailableLines, bool* nursery) const;
    size_t GetFreshBlocksReleasedInPauseForTest() const;
    // Whether the side metadata manager owns the data address (CommitDataRange has admitted it and nothing has given it back).
    bool IsDataOwnedForTest(uintptr_t address) const;
    LxrRcResult ProcessQueuedTraceReferenceForTest(uintptr_t address);
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
    struct WorkerCounters;
    struct CopyLog;
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
    void CountWorker(size_t worker, size_t counter, uint64_t amount);
    uint64_t SumWorker(size_t counter) const;
    LxrRcResult InitializeBuffers(size_t slots);
    bool CommitPlanes(size_t first, size_t count, bool commitTrace = true, bool initialize = true);
    bool DecommitPlanes(size_t first, size_t count, bool tailIsLast);
    bool CommitTracePlanes();
    size_t GetTracePlaneBudget(size_t immixBytes, size_t largeBytes) const;
    LxrRcResult GrowSuspended();
    // Whether the native planes and the object storage can take the range [current, current + candidate) of the Immix
    // or the large-object space. With a larger candidate than fits, the increment is lowered to a number of
    // granules that fits (but not below minimum). False when even minimum does not fit.
    bool FitAdmission(bool large, size_t current, size_t minimum, size_t* increment);
    // Admits increment bytes after the current end of the space: commits the planes and the storage, the arena
    // memory and the log capacity, and raises the capacity.
    LxrRcResult AdmitSuspended(bool large, size_t current, size_t increment, bool commitTrace, bool initialize);
    // P17 S5b. Pause only. Admit the Immix blocks that the copy may need beyond the free ones.
    LxrRcResult BorrowSuspended();
    // P17 S5b. Pause only, after the nursery blocks are released and before the epoch flips. Gives back the free tail
    // of the borrowed range.
    LxrRcResult GiveBackSuspended();
    LxrRcResult ReturnTailSuspended(size_t newCapacity);
    void CountOvershoot(OvershootMetric metric, uint64_t amount)
    {
        m_overshoot_metrics[metric].fetch_add(amount, std::memory_order_relaxed);
    }
    void SetOvershoot(OvershootMetric metric, uint64_t value)
    {
        m_overshoot_metrics[metric].store(value, std::memory_order_relaxed);
    }
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
    LxrRcResult ReserveCopyRecord(size_t slot);
    void CommitCopyRecord(size_t slot, uintptr_t start, size_t size);
    void DeleteCopyLogs();
    static size_t ObjectSize(Object* object);
    LxrRcResult ScanIncrements(Object* object, bool trace, ReferenceBatch* children, size_t worker);
    LxrRcResult ProcessDecrements(Work* work, LxrWorkPacket* packet, size_t worker);
    LxrRcResult ScanDecrements(Object* object, ReferenceBatch* children, size_t worker);
    LxrRcResult Sweep(const LxrBlockReleaseGuard& guard);
    LxrRcResult RefreshBlock(uintptr_t block, size_t index, const LxrBlockReleaseGuard& guard);
    // Releases the block when no count is left in it. Updates the release counters and the evacuation flag.
    // With a null guard it runs in a pause.
    LxrRcResult ReleaseBlockIfDead(
        uintptr_t block, size_t index, const LxrBlockReleaseGuard* guard, ImmixBlockOperationStatus* status);
    LxrRcResult ReleaseEpoch();
    // Per-object accounting after a death or a swept cycle (the lifetime probe and, in the test platform, the
    // oracle). The product keeps no per-object record, so this costs one load.
    LxrRcResult Reclaim(uintptr_t start);
    void NoteProbeReclaim(uintptr_t start);
    // The pause releases the clean blocks that mutators took since the last pause and that no object promoted.
    // It looks at those blocks only.
    LxrRcResult ReleaseNurseryBlocks();
    LxrRcResult SweepLargeNursery();
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    LxrRcResult ReclaimLargeNursery();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
    LxrRcResult GetObjectSize(Object* object, size_t* size, size_t worker = SIZE_MAX);
    bool IsImmix(uintptr_t start) const;
    bool IsReturnedImmix(uintptr_t start) const;
    bool IsLarge(uintptr_t start) const;
    static uintptr_t Start(Object* object);
    static Object* Reference(uintptr_t start);
    size_t GetLargeObjectSize(uintptr_t start) const;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    std::atomic<size_t>& ExtentSlot(size_t heapSlot) const;
    void OracleBirth(uintptr_t start, size_t size);
    LxrRcResult OracleReclaim(uintptr_t start);
    void OracleRetire(uintptr_t start);
    LxrRcResult OracleReclaimNursery(size_t block);
    size_t FindNextOracleSlot(size_t first) const;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
    // Inline fast-path accessors of the hot metadata kinds. They are built once after m_metadata is
    // initialized. Use them only for addresses of objects, lines and blocks that the engine admitted
    // (see SideMetadataAccessor); untrusted addresses go through m_metadata.
    SideMetadataAccessor m_rc_metadata;
    SideMetadataAccessor m_mark_metadata;
    SideMetadataAccessor m_unlogged_metadata;
    SideMetadataAccessor m_valid_metadata;
    SideMetadataAccessor m_defrag_metadata;
    SideMetadataAccessor m_line_reuse_metadata;
    SideMetadataAccessor m_page_reuse_metadata;
    ImmixBlockManager m_blocks;
    ReferenceCountTable m_counts;
    ImmixAllocationManager m_allocation;
    LxrLargeObjectSpace m_large;
    LxrWorkScheduler m_scheduler;
    LxrPauseCoordinator m_coordinator;
    void* m_arena;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    // The oracle of the test platform: the size of each object and one live bit per slot, as the port kept them
    // before P17 S3. Product code never reads them. They are not counted as native planes.
    std::atomic<size_t>* m_extents = nullptr;
    LxrVirtualArray<std::atomic<size_t>> m_extent_storage;
    LxrVirtualArray<std::atomic<uint64_t>> m_live_storage;
    std::atomic<uint64_t>* m_live_objects = nullptr;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    LxrVirtualArray<std::atomic<uint8_t>> m_trace_lock_storage;
    LxrVirtualArray<TraceScan*> m_trace_scan_storage;
    LxrVirtualArray<std::atomic<uint8_t>> m_allocated_block_storage;
    // The blocks that mutators took since the last pause (see ImmixAllocationManager::AcquireNotes).
    uint32_t* m_allocated_list = nullptr;
    std::atomic<size_t> m_allocated_count{0};
    std::atomic<size_t> m_immix_capacity{0};
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    std::atomic<bool> m_log_reader_hold{false};
    std::atomic<bool> m_log_reader_active{false};
    std::atomic<bool> m_sweep_hold{false};
    std::atomic<bool> m_sweep_holding{false};
    std::atomic<size_t> m_pause_released_blocks{0};
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    size_t m_initial_immix = 0;
    size_t m_initial_large = 0;
    size_t m_grow_bytes = 0;
    bool m_grow_large = false;
    LxrRcResult m_growth_result = LxrRcResult::Success;
    std::atomic<uint64_t> m_growth_count{0};
    // P17 S5b. The capacity that is never given back: the initial capacity and every regular growth. The part above it
    // is borrowed for a pause. Only a pause changes these values.
    bool m_overshoot = false;
    size_t m_immix_floor = 0;
    size_t m_borrow_request_pages = 0;
    std::atomic<uint64_t> m_overshoot_metrics[OvershootMetricCount]{};
    std::atomic<size_t> m_epoch_fresh_blocks{0};
    std::atomic<size_t> m_epoch_recycled_lines{0};
    std::atomic<int64_t> m_release_end_stamp{0};
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    std::atomic<size_t> m_live_bytes{0};
    std::atomic<uint64_t> m_reclaimed_bytes{0};
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    std::atomic<uintptr_t> m_probe_start{0};
    std::atomic<uint64_t> m_probe_births{0};
    std::atomic<bool> m_probe_retired{false};
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
    std::atomic<bool> m_cycle_sweep;
    LxrRcMutator* m_mutators;
    ImmixBumpAllocator* m_copy_allocators = nullptr;
    // One list of copied sources per worker, plus one shared slot for the mature copy. A source loses its first
    // field to the forwarding pointer, so the pause keeps its size here until RetireForwardedSources.
    CopyLog* m_copy_logs = nullptr;
    WorkerCounters* m_worker_counters = nullptr;
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
