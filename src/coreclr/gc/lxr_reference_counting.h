// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_REFERENCE_COUNTING_H
#define LXR_REFERENCE_COUNTING_H

#include "immix_allocation.h"
#include "large_object_space.h"
#include "reference_count.h"
#include "lxr_pause_coordinator.h"
#include "lxr_virtual_array.h"
#include "lxr_finalizer_registry.h"
#include "lxr_pin_list.h"
#include "lxr_young_spans.h"

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
    // P22 (D2). The largest allocation buffer that a mutator gets from a hole of an Immix block, in bytes. Zero means
    // the whole hole (a block is 32 KiB, so a buffer never crosses a block). A smaller value is rounded down to a
    // multiple of the line size and must be at least one line.
    size_t allocationBufferBytes = 0;
    // P25 (F4). Workers that may run the packets of the concurrent trace while the program runs. 0 means all workers.
    size_t concurrentTraceWorkers = 0;
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
    // P17 S7: the pin callback and the copy callback are gone. The engine keeps the pin list of the pause itself
    // (AddPinnedRoot), and a profiler walk reads the copies from the forwarding records (WalkForwarded).
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

// P26 (F2). What the last pause visited in the walks that used to follow the trace backlog. The counters count visited
// packets or entries, so a test can check that they do not grow with the number of pending trace packets.
struct LxrPauseWalkMetrics
{
    uint64_t retireVisited;       // packets that RetireCompletedWork looked at (it frees the finished ones)
    uint64_t fixPackets;          // packets that FixForwardingReaders looked at
    uint64_t fixLogEntries;       // decrement log entries that FixForwardingReaders looked at
    uint64_t publishVisited;      // trace packets that PublishTraceWork submitted
    uint64_t fixRewritten;        // references that FixForwardingReaders changed
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
    // The buffer that the stock bump-pointer helpers use: [publishedCursor, publishedLimit) when published.
    uintptr_t publishedCursor;
    uintptr_t publishedLimit;
    // Start of the span that is open now: the first object that the engine allocated in the current hole. Zero when no
    // span is open. The span closes when the buffer is retired, when the hole is replaced, or at a pause.
    uintptr_t spanStart;
    // The closed spans of this thread since the last pause. A pause merges them into the list of the engine.
    LxrSpanList spans;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    uintptr_t lastPublishedLimit = 0;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
};

class LxrReferenceCounting final
{
public:
    static constexpr size_t BufferCapacity = 1024;
    static constexpr size_t FullTraceBatchCapacity = 8192;
    // Largest number of roots of one kind in one pause (the roots array grows from its initial size).
    static constexpr size_t RootGrowthLimit = size_t{1} << 27;

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
    // P22 (D2). The mutator's buffer ends at cursor (the used point: the address of the next object header). The span of
    // the objects that the thread allocated in the hole is recorded as [start, cursor).
    LxrRcResult RetireAllocationSpan(LxrRcMutator* mutator, uintptr_t cursor);
    // pinned: the object belongs to the pinned object heap. A small one goes into a pinned block (a block that holds
    // such objects only, and that no pause copies from). A large one goes into the large-object space, which never
    // moves objects.
    // P22 (D2). A small non-pinned object is a young object: the engine records no bit for it. The memory is zero and
    // its ranges are prepared (see ImmixAllocationManager). Large and pinned objects get their ValidObject bit here,
    // because they are allocated one by one and are found by it.
    LxrRcResult Allocate(LxrRcMutator* mutator, size_t completeSize, Object** object, bool pinned = false);
    LxrRcResult LogBeforeWrite(
        LxrRcMutator* mutator, Object** slot, Object* oldTarget);
    LxrRcResult Collect(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult BeginTrace(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult FinishTrace(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult CollectFull(LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds);
    LxrRcResult AddRoot(Object* object, bool interior = false, bool trace = true);
    // P17 S6: a count that stays until ReleaseRoot takes it away. AddRoot gives a count for one pause only. Both calls
    // are for the root capture of a pause. The increment (or the decrement) runs in that pause, as for any root.
    // The number of such counts is not limited.
    LxrRcResult RetainRoot(Object* object);
    LxrRcResult ReleaseRoot(Object* object);
    // P17 S7: tells the engine that the root capture found a pinned root. With interior set, the address may lie
    // inside the object, and the engine pins the object that contains it. The call is for the root capture of a
    // pause, and may run on several threads at once when each one uses its own shard number. The engine seals the
    // list when the capture returns. The object must still be given to AddRoot: pinning keeps the object in place,
    // it does not keep it alive.
    LxrRcResult AddPinnedRoot(Object* object, bool interior = false, size_t shard = 0);
    // Whether the pause that runs now must not move the object: it is in the pin list, or in a pinned block. Valid
    // from the end of the root capture to the end of the pause.
    bool IsPinnedForPause(Object* object) const;
    // Number of different pinned roots of the last pause that ran its root capture.
    uint64_t GetPinnedRootCount() const { return m_pause_pinned_roots.load(std::memory_order_acquire); }
    // Whether the block of the address is a pinned block (it holds objects of the pinned object heap).
    bool IsPinnedBlock(uintptr_t address) const;
    // Number of blocks whose pause bit is set (0 outside a pause). For tests and Debug builds.
    size_t CountPausePinnedBlocks() const;
    LxrRcResult TraceRoot(Object* object);
    // Like TraceRoot for many objects, with one drain at the end. The values are object addresses.
    LxrRcResult TraceRoots(const uintptr_t* objects, size_t count);
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
    LxrPauseWalkMetrics GetPauseWalkMetrics() const;
    LxrRcResult HoldTrace(bool hold);
    LxrRcResult ArmScanCheckpoint(Object* object);
    bool IsScanCheckpointActive() const;
    LxrRcResult Drain(uint32_t timeoutMilliseconds);
    // The block cursors go back to the start of the arena. The allocation path calls this when a request has failed and the lazy
    // sweep has finished, so that blocks that the sweep freed behind the cursors can be taken (the Java LXR gets a new chunk).
    void ResetAllocationCursors() { m_allocation.ResetCursors(); }
    LxrRcResult LoadCount(Object* object, uintptr_t* count) const;
    // requireCounted: the address may lie outside every live object (a remembered slot of a dead holder). Only an
    // object with a count above zero may be read. Use it after the increments of a pause.
    // P22 (D2). A promoted object is found by its ValidObject bit. A young object has no bit. During a pause, the
    // address is looked up in the spans that the mutators filled since the last pause (the engine walks the objects of
    // the span from its start, by size). Outside a pause the engine has no span list, so an address inside a young
    // object is not found.
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
    // The test harness plays the stock bump-pointer helper. A product object that a helper bumps has no call into the
    // engine, so the harness tells the oracle about it with this call.
    void RecordHelperObjectForTest(Object* object, size_t size) { OracleBirth(Start(object), size); }
    // P22 (D2) tests. Fills the planes of a block range with values that a clean range must not have, and reads
    // them back. The refill of a buffer has to clear them all.
    struct RangeStateForTest
    {
        size_t validSlots;
        size_t countedSlots;
        size_t unloggedSlots;
        size_t markedSlots;
        size_t straddleLines;
    };
    LxrRcResult PoisonRangeForTest(uintptr_t start, size_t bytes, bool counts = true);
    LxrRcResult GetRangeStateForTest(uintptr_t start, size_t bytes, RangeStateForTest* state);
    bool IsMarkingForTest() const { return m_marking.load(std::memory_order_acquire); }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    bool OwnsAddress(uintptr_t address) const;
    // Whether the address is the start of an object that the engine registered: the ValidObject bit is set.
    // P22 (D2). The bit is set when an Immix object is promoted (its first increment) or copied, when a large object or
    // a pinned-object-heap object is allocated, and never for a young object. So this is false for a young object that
    // has no increment yet. Use IsOwnedObject for a value that may be young. A dead object keeps its bit until
    // Death or the cycle sweep clears it. Use the reference count (above zero) for liveness after the increments of
    // a pause.
    bool IsObjectStart(Object* object) const;
    // Whether the value can be an object of the engine: it lies in the owned space and is aligned. For a large
    // object the ValidObject bit must also be set. It is true for a young object. It does not read the object.
    bool IsOwnedObject(Object* object) const;
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
    // Stale bits of dead promoted objects are included.
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
    // P22 (D2). Span statistics: 0 = spans closed, 1 = bytes in closed spans, 2 = spans of the last pause.
    uint64_t GetSpanMetric(size_t metric) const { return m_span_metrics[metric].load(std::memory_order_relaxed); }
    // Number of spans of the pause that runs now (0 outside a pause) and the span at an index. For tests.
    size_t GetYoungSpanCount() const { return m_young_spans.Size(); }
    bool GetYoungSpan(size_t index, uintptr_t* start, uintptr_t* end) const
    {
        if (index >= m_young_spans.Size()) { return false; }
        *start = m_young_spans[index].start;
        *end = m_young_spans[index].end;
        return true;
    }
    LxrRcResult GetMaturePages(size_t* pages) const;
    // P25 (F5). Pages that hold data: the pages of the non-free blocks minus the free lines of the reusable blocks, plus
    // the mature large-object pages. The Java LXR counts whole blocks (GetMaturePages); it can, because a pause copies
    // the young survivors into dense blocks. Where survivors stay in place, a block with one live object is still one
    // block, and only the lines tell how much data the heap holds. The two figures are equal for dense blocks.
    LxrRcResult GetOccupiedPages(size_t* pages) const;
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
    // P22 (D2). Young objects have no bit, so they are not visited.
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
    // P26 (F2) tests. The gate blocks the worker of the concurrent trace after it has processed `packets` packets. It lets
    // a test keep a large trace backlog queued in the scheduler (with one trace worker). A pause opens the gate.
    void ArmTraceGateForTest(uint64_t packets);
    // P26 (F4b). The trace packet loops over its children (the Java LXR does). A test that needs a child packet to exist
    // switches the loop off; the packet then ends after its own objects and makes packets of the children.
    void SetTraceLoopForTest(bool enabled) { m_trace_loop_off.store(!enabled, std::memory_order_release); }
    LxrRcResult QueueTraceBacklogForTest(Object* object, size_t count);
    bool IsTraceGateBlockedForTest() const { return m_gate_blocked.load(std::memory_order_acquire); }
    uint64_t GetTracePendingForTest() const { return m_trace_pending.load(std::memory_order_acquire); }
    // P26 (F8) tests. Packets of the lazy decrements and the lazy sweep that have not finished (the Unconstrained stage).
    uint64_t GetLazyPendingForTest() const { return m_scheduler.GetStageOutstanding(LxrWorkBucketStage::Unconstrained); }
    // Every lazy packet (decrements, previous roots, mature sweep) sleeps this long before it runs, so a test can keep lazy work
    // pending while the mutators run.
    void SetLazyDelayForTest(uint32_t microseconds) { m_lazy_delay_us.store(microseconds, std::memory_order_release); }
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
    bool CommitPlanes(size_t first, size_t count, bool initialize = true);
    bool DecommitPlanes(size_t first, size_t count, bool tailIsLast);
    LxrRcResult GrowSuspended();
    // Whether the native planes and the object storage can take the range [current, current + candidate) of the Immix
    // or the large-object space. With a larger candidate than fits, the increment is lowered to a number of
    // granules that fits (but not below minimum). False when even minimum does not fit.
    bool FitAdmission(bool large, size_t current, size_t minimum, size_t* increment);
    // Admits increment bytes after the current end of the space: commits the planes and the storage, the arena
    // memory and the log capacity, and raises the capacity.
    LxrRcResult AdmitSuspended(bool large, size_t current, size_t increment, bool initialize);
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
    // P22 (D2). Closes the open span of a mutator at end (an object start). An empty span is not recorded.
    LxrRcResult CloseSpan(LxrRcMutator* mutator, uintptr_t end);
    // Looks for the object that contains the address in the spans of the pause. Returns InvalidArgument when no span
    // contains the address.
    LxrRcResult FindInYoungSpans(uintptr_t address, Object** object, bool requireCounted) const;
    // Counts a birth of the lifetime probe when an object of a recorded span starts at the probed address.
    void CheckProbeInSpans();
    // P22 (D2). The ValidObject bit of an Immix object that is about to get its first count. It must be visible before
    // the count is, because another worker that sees the count may trace the object.
    void MarkPromotedStart(uintptr_t start);
    LxrRcResult PrepareWork();
    Work* NewWork(
        WorkKind kind, const uintptr_t* references, size_t count, LxrWorkBucketStage stage,
        bool trace = true, TraceScan* scan = nullptr, bool borrow = false, bool logged = false);
    LxrRcResult SubmitWork(Work* work, const LxrWorkPacket* parent = nullptr);
    void DeleteWork();
    void ReleaseWork(Work* work);
    void RetireCompletedWork();
    void PushTraceUnsubmitted(Work* work);
    void RetireTraceWork(Work* work);
    static void ReleaseCancelledPacket(LxrWorkPacket* packet, void* context);
    LxrRcResult ProcessIncrementWork(Work* work, size_t worker);
    LxrRcResult ProcessChildIncrements(ReferenceBatch* children, size_t worker);
    LxrRcResult ProcessDecrementEntry(uintptr_t reference, bool previousRoot, DeathContext* death, size_t worker);
    LxrRcResult ProcessIncrement(
        Object** object, bool trace, ReferenceBatch* children, size_t worker);
    // P25 (F1). True while a pause or the mutators run in the window of a concurrent trace after its snapshot (the trace
    // phase is Concurrent or FinalMark). Allocation is black in this window.
    bool IsIncrementTraceWindow() const;
    // P25 (F1). Whether a counted object needs a trace packet: the window is open and no mark bit is set. A marked object
    // (black allocation, or already traced) is skipped, as in the Java LXR.
    bool NeedsIncrementTrace(uintptr_t start) const;
    LxrRcResult TryCopyNursery(
        Object* source, Object** destination, size_t worker, bool* copied,
        bool* incrementedInPlace, uintptr_t* previous);
    LxrRcResult AllocateCopy(size_t size, size_t worker, uintptr_t* start);
    LxrRcResult TryCopyMature(Object** object);
    LxrRcResult SelectMatureCandidates();
    // P17 S7: marks the mature blocks that hold a pinned root as retained. It reads the pin list, so its cost follows
    // the number of pinned roots and not the number of objects.
    LxrRcResult RetainPinnedMatureBlocks();
    // The pause bit of a block and the pin list. SealPins runs once when the root capture returns; ClearPins runs at
    // the end of the pause.
    LxrRcResult SealPins();
    void ClearPins();
    bool IsPinnedObject(Object* object) const;
    bool IsMatureCandidate(uintptr_t address);
    LxrRcResult RememberMatureSlot(Object** slot, Object* target);
    LxrRcResult ReplayMatureRememberedSlots();
    void DeleteRememberedChunks();
    LxrRcResult FixForwardingReaders();
    void FixPacketReferences(Work* work);
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
    bool ShouldStopTraceLoop() const;
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
    void SetUnloggedLines(uintptr_t start, size_t size);
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
    // The per-object trace state is not a table any more (P17 S7). A fixed set of spin locks protects the trace of
    // an object (the lock of an object is chosen by a hash of its address), and a fixed hash index holds the scans
    // of the objects whose trace was split (only large arrays), keyed by the address of the object.
    std::atomic<uint8_t>& TraceLockSlot(size_t heapSlot) const;
    TraceScan* FindTraceScan(size_t heapSlot) const;
    void LinkTraceScan(size_t heapSlot, TraceScan* scan);
    void UnlinkTraceScan(TraceScan* scan);
    void MoveTraceScan(size_t fromSlot, size_t toSlot, TraceScan* scan);

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
    // P25 (F1). Number of trace packets that a pause queued for a counted object (metric 57 of GetStorageMetric).
    std::atomic<uint64_t> m_increment_traces{0};
    std::atomic<bool> m_storage_diagnostics{false};
    static constexpr size_t TraceLockCount = 16384;
    static constexpr size_t TraceScanBucketCount = 4096;
    struct alignas(64) TraceScanBucket
    {
        std::atomic_flag lock = ATOMIC_FLAG_INIT;
        TraceScan* head = nullptr;
    };
    std::atomic<uint8_t>* m_trace_locks;
    TraceScanBucket* m_scan_buckets;
    std::atomic<size_t> m_scan_index_count{0};
    // P17 S7. Pinning. m_block_flags has one byte per Immix block: ImmixAllocationManager::PinnedBlockFlag (the block
    // holds pinned-object-heap objects) and PausePinFlag (the pause found a pinned root in the block).
    static constexpr uint8_t PausePinFlag = 2;
    LxrPinList m_pins;
    std::atomic<uint8_t>* m_block_flags = nullptr;
    ImmixBumpAllocator m_pinned_allocator;
    std::atomic_flag m_pinned_lock = ATOMIC_FLAG_INIT;
    std::atomic<uint64_t> m_pause_pinned_roots{0};
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
    // P22 (D2). The spans that mutators filled since the last pause, merged and sorted at the start of the pause and
    // dropped when the pause ends. Only a pause reads it.
    LxrSpanList m_young_spans;
    // The spans of threads that left since the last pause (see DetachMutator).
    LxrSpanList m_detached_spans;
    std::atomic_flag m_detached_lock = ATOMIC_FLAG_INIT;
    std::atomic<uint64_t> m_span_metrics[4]{};
    ImmixBumpAllocator* m_copy_allocators = nullptr;
    // One list of copied sources per worker, plus one shared slot for the mature copy. A source loses its first
    // field to the forwarding pointer, so the pause keeps its size here until RetireForwardedSources.
    CopyLog* m_copy_logs = nullptr;
    WorkerCounters* m_worker_counters = nullptr;
    std::atomic<bool> m_copy_exhausted{false};
    std::atomic<bool> m_mature_copy_exhausted{false};
    mutable std::atomic<uint64_t> m_nursery_metrics[6]{};
    // P26 (F2). Walk counters of the last pause (see LxrPauseWalkMetrics).
    std::atomic<uint64_t> m_walk[5]{};
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
    size_t m_root_capacity[2];
    LxrWordArray m_retained_roots;
    LxrWordArray m_released_roots;
    size_t m_current_roots;
    uintptr_t* m_dead_large;
    std::atomic<uint8_t>* m_mature_candidates;
    std::atomic<size_t> m_dead_large_count;
    std::atomic<size_t> m_producer_bank;
    std::atomic<Work*> m_work;
    // P26 (F2). Trace packets are not in m_work. A trace packet that cannot go to the scheduler yet (it was made inside a
    // pause, the trace is held, or the stage is closed) waits in m_trace_unsubmitted, which PublishTraceWork empties
    // at Resume. A trace packet that was processed goes to m_trace_done; the next Suspend frees it. A pending
    // packet in the scheduler is in no list: the cost of a pause does not follow the number of such packets.
    std::atomic<Work*> m_trace_unsubmitted;
    std::atomic<Work*> m_trace_done;
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
    std::atomic<uintptr_t> m_mature_scan_receipt[6]{};
    std::atomic<bool> m_gate_armed{false};
    std::atomic<uint32_t> m_lazy_delay_us{0};
    std::atomic<bool> m_trace_loop_off{false};
    std::atomic<bool> m_gate_blocked{false};
    std::atomic<int64_t> m_gate_remaining{0};
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
};

static_assert(LxrReferenceCounting::BufferCapacity == LxrDefaultRcPacketReferenceCapacity);
static_assert(LxrReferenceCounting::BufferCapacity <= UINT16_MAX);

#endif // LXR_REFERENCE_COUNTING_H
