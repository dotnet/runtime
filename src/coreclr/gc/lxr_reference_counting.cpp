// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcref.h"
#include "lxr_reference_counting.h"

#include <new>
#include <algorithm>
#include <minipal/utils.h>
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
#include <chrono>
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

namespace
{
    enum Metric : size_t
    {
        Allocations, AllocatedBytes, LoggedSlots, IncrementEntries, DecrementEntries,
        BufferFlushes, ThreadFlushes, Promotions, Deaths, RootIncrements, RootDecrements,
        ObjectScans, ObjectSizeQueries, OldTargetChecks, ChildRegistrations,
        BlocksReleased, LargeObjectBytesReleased, Epochs,
    };

    enum TraceMetric : size_t
    {
        InitialMarks, FinalMarks, FullMarks, TracedObjects, TracedReferences, SatbEntries,
        DeletionMarks, ForcedScans, SplitPackets, CycleObjects, CycleBytes, Interruptions,
        ConcurrentSlices, CheckpointInterruptions,
    };

    bool TryMultiply(size_t left, size_t right, size_t* value)
    {
        if (right != 0 && left > SIZE_MAX / right)
        {
            return false;
        }

        *value = left * right;
        return true;
    }

    constexpr uint8_t RetainedSource = 2;
    constexpr uint8_t CopiedSource = 4;
    constexpr uintptr_t LogReferenceMask = LxrSideMetadataLayout::AddressLimit - 1;
    constexpr uint8_t LogCountShift = LxrSideMetadataLayout::AddressBits;
    static_assert(sizeof(uintptr_t) == 8);
    static_assert(LxrReferenceCounting::BufferCapacity <= (UINTPTR_MAX >> LogCountShift));
}

enum class LxrReferenceCounting::WorkKind : uint8_t
{
    Slots,
    NurserySlots,
    Roots,
    Targets,
    Decrements,
    PreviousRoots,
    NurserySweep,
    MatureSweep,
    Satb,
    PrepareTrace,
    WeakClosure,
    TraceObjects,
    TraceScan,
    EpochClosure,
};

struct LxrReferenceCounting::LogChunk
{
    uintptr_t references[BufferCapacity]{};
    size_t count = 0;
    bool increment = false;
};

struct LxrReferenceCounting::LogStream
{
    uintptr_t* references = nullptr;
    std::atomic<uint16_t>* published = nullptr;
    std::atomic<size_t> reserved{0};
    size_t capacity = 0;
    LxrVirtualArray<uintptr_t> referenceStorage;
    LxrVirtualArray<std::atomic<uint16_t>> publicationStorage;
    std::atomic<size_t> highWater{0};
    std::atomic<uint64_t> flushes{0};
    std::atomic<uint64_t> copiedBytes{0};
    std::atomic<uint64_t> resetBytes{0};
};

struct LxrReferenceCounting::LogBank
{
    LogStream increments;
    LogStream decrements;
    size_t capacity = 0;
};

struct LxrReferenceCounting::RememberedChunk
{
    struct Entry
    {
        Object** slot;
        uint8_t reuse;
    };

    Entry entries[BufferCapacity]{};
    std::atomic<uint8_t> published[BufferCapacity]{};
    std::atomic<size_t> reserved{0};
    RememberedChunk* next = nullptr;
};

class LxrReferenceCounting::CostScope final
{
public:
    CostScope(LxrReferenceCounting* engine, size_t metric)
        : m_engine(engine), m_metric(metric), m_start(-1)
    {
        if (engine->m_storage_diagnostics.load(std::memory_order_relaxed))
        {
            m_start = Clock();
        }
    }

    ~CostScope()
    {
        if (m_start >= 0)
        {
            m_engine->m_cost_ticks[m_metric].fetch_add(
                static_cast<uint64_t>(Clock() - m_start), std::memory_order_relaxed);
        }
    }

private:
    static int64_t Clock()
    {
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        return std::chrono::duration_cast<std::chrono::nanoseconds>(
            std::chrono::steady_clock::now().time_since_epoch()).count();
#else
        return GCToOSInterface::QueryPerformanceCounter();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    }

    LxrReferenceCounting* m_engine;
    size_t m_metric;
    int64_t m_start;
};

int64_t LxrPauseTimers::Now()
{
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    return std::chrono::duration_cast<std::chrono::nanoseconds>(
        std::chrono::steady_clock::now().time_since_epoch()).count();
#else
    return GCToOSInterface::QueryPerformanceCounter();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
}

int64_t LxrPauseTimers::Frequency()
{
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    return 1000000000;
#else
    return GCToOSInterface::QueryPerformanceFrequency();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
}

uint64_t LxrPauseTimers::ToNanoseconds(int64_t ticks)
{
    if (ticks <= 0)
    {
        return 0;
    }
    uint64_t frequency = static_cast<uint64_t>(Frequency());
    uint64_t value = static_cast<uint64_t>(ticks);
    return value / frequency * 1000000000 + value % frequency * 1000000000 / frequency;
}

LxrPauseTimers::LxrPauseTimers()
    : m_scalar{}, m_last{}, m_total{}
{
}

void LxrPauseTimers::Begin(int64_t waited)
{
    if (!Enabled())
    {
        return;
    }
    for (std::atomic<int64_t>& value : m_scalar)
    {
        value.store(0, std::memory_order_relaxed);
    }
    for (GroupState& group : m_groups)
    {
        group.busy.store(0, std::memory_order_relaxed);
        group.first.store(INT64_MAX, std::memory_order_relaxed);
        group.last.store(0, std::memory_order_relaxed);
        group.packets.store(0, std::memory_order_relaxed);
    }
    m_scalar[LockWait].store(waited, std::memory_order_relaxed);
    m_paused.store(false, std::memory_order_relaxed);
    m_begin_timestamps = m_timestamps.load(std::memory_order_relaxed);
    m_begin = Stamp();
}

void LxrPauseTimers::AddPacket(Group group, int64_t start, int64_t end)
{
    GroupState& state = m_groups[group];
    state.busy.fetch_add(end - start, std::memory_order_relaxed);
    state.packets.fetch_add(1, std::memory_order_relaxed);
    int64_t seen = state.first.load(std::memory_order_relaxed);
    while (start < seen && !state.first.compare_exchange_weak(seen, start, std::memory_order_relaxed))
    {
    }
    seen = state.last.load(std::memory_order_relaxed);
    while (end > seen && !state.last.compare_exchange_weak(seen, end, std::memory_order_relaxed))
    {
    }
}

void LxrPauseTimers::MarkPause(uint8_t kind)
{
    m_kind.store(kind, std::memory_order_relaxed);
    m_paused.store(true, std::memory_order_relaxed);
}

void LxrPauseTimers::End()
{
    if (!Enabled() || !m_paused.load(std::memory_order_relaxed))
    {
        return;
    }
    m_scalar[Total].store(Stamp() - m_begin, std::memory_order_relaxed);
    uint64_t values[PublishedCount];
    for (size_t index = 0; index < ScalarCount; index++)
    {
        values[index] = ToNanoseconds(m_scalar[index].load(std::memory_order_relaxed));
    }
    for (size_t group = 0; group < GroupCount; group++)
    {
        uint64_t packets = m_groups[group].packets.load(std::memory_order_relaxed);
        int64_t first = m_groups[group].first.load(std::memory_order_relaxed);
        int64_t last = m_groups[group].last.load(std::memory_order_relaxed);
        size_t slot = ScalarCount + 3 * group;
        values[slot] = packets != 0 ? ToNanoseconds(last - first) : 0;
        values[slot + 1] = ToNanoseconds(m_groups[group].busy.load(std::memory_order_relaxed));
        values[slot + 2] = packets;
    }
    m_sequence.fetch_add(1, std::memory_order_acq_rel);
    for (size_t index = 0; index < PublishedCount; index++)
    {
        m_last[index].store(values[index], std::memory_order_relaxed);
        m_total[index].fetch_add(values[index], std::memory_order_relaxed);
    }
    m_published_timestamps.store(
        m_timestamps.load(std::memory_order_relaxed) - m_begin_timestamps, std::memory_order_relaxed);
    m_sequence.fetch_add(1, std::memory_order_release);
}

uint64_t LxrPauseTimers::GetMeta(size_t index) const
{
    switch (index)
    {
        case MetaSequence: return m_sequence.load(std::memory_order_acquire);
        case MetaKind: return m_kind.load(std::memory_order_relaxed);
        case MetaTimestamps: return m_published_timestamps.load(std::memory_order_relaxed);
        case MetaCost:
        {
            // Mean cost of one timestamp, measured on demand and never inside a pause.
            constexpr int Samples = 2000;
            int64_t start = Now();
            int64_t sink = 0;
            for (int sample = 0; sample < Samples; sample++)
            {
                sink += Now();
            }
            int64_t end = Now();
            return ToNanoseconds(end - start) / Samples + (sink == 0 ? 1 : 0);
        }
        default: return UINT64_MAX;
    }
}

uint64_t LxrPauseTimers::GetLast(size_t index) const
{
    return index < PublishedCount ? m_last[index].load(std::memory_order_relaxed) : UINT64_MAX;
}

uint64_t LxrPauseTimers::GetTotal(size_t index) const
{
    return index < PublishedCount ? m_total[index].load(std::memory_order_relaxed) : UINT64_MAX;
}

namespace
{
    class PacketScope final
    {
    public:
        PacketScope(LxrPauseTimers& timers, LxrPauseTimers::Group group, bool timed)
            : m_timers(timers), m_group(group), m_start(timed && timers.Enabled() ? timers.Stamp() : 0)
        {
        }

        ~PacketScope()
        {
            if (m_start != 0)
            {
                m_timers.AddPacket(m_group, m_start, m_timers.Stamp());
            }
        }

        PacketScope(const PacketScope&) = delete;
        PacketScope& operator=(const PacketScope&) = delete;

    private:
        LxrPauseTimers& m_timers;
        LxrPauseTimers::Group m_group;
        int64_t m_start;
    };
}

struct LxrReferenceCounting::Work
{
    LxrReferenceCounting* engine;
    WorkKind kind;
    Work* next;
    Work* traceNext;
    uintptr_t* ownedReferences;
    const uintptr_t* references;
    LxrWorkPacket packet;
    bool trace;
    TraceScan* scan;
    std::atomic<bool> traceSubmitted;
    std::atomic<bool> traceProcessed;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    bool watchedReader = false;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

    Work(LxrReferenceCounting* owner, uint64_t id, WorkKind workKind,
         const uintptr_t* values, size_t count, LxrWorkBucketStage stage,
         bool traceObjects, TraceScan* traceScan, bool borrow, bool logged)
        : engine(owner), kind(workKind), next(nullptr), traceNext(nullptr),
          ownedReferences(count == 0 || borrow ? nullptr : new (std::nothrow) uintptr_t[count]),
          references(borrow ? values : ownedReferences),
          packet(id, stage, references, count, Execute, this), trace(traceObjects), scan(traceScan),
          traceSubmitted(false), traceProcessed(false)
    {
        if (count != 0 && ownedReferences != nullptr)
        {
            memcpy(ownedReferences, values, count * sizeof(uintptr_t));
            if (logged)
            {
                ownedReferences[0] &= LogReferenceMask;
            }
        }
    }

    uintptr_t GetReference(size_t index) const
    {
        uintptr_t value = references[index];
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_LOG_DECODE_CONTROL)
        return value;
#else
        return index == 0 && ownedReferences == nullptr ? value & LogReferenceMask : value;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_LOG_DECODE_CONTROL
    }

    ~Work()
    {
        delete[] ownedReferences;
    }
};

struct LxrReferenceCounting::TraceScan
{
    Object* object;
    GCReferenceRangeIterator iterator;
    GCReferenceRange range;
    size_t cursor;
    Object* loaderAllocator;
    std::atomic<bool> complete;
    TraceScan* next;
    std::atomic<size_t> users{0};

    TraceScan(Object* target, Object* loader)
        : object(target), iterator(target), range{}, cursor(0),
          loaderAllocator(loader), complete(false), next(nullptr)
    {
    }
};

struct LxrReferenceCounting::ReferenceBatch
{
    LxrReferenceCounting* engine;
    WorkKind kind;
    const LxrWorkPacket* parent;
    uintptr_t references[BufferCapacity];
    size_t count = 0;
    bool trace = false;

    LxrRcResult Flush()
    {
        if (count == 0)
        {
            return LxrRcResult::Success;
        }
        LxrRcResult result = LxrRcResult::Success;
        LxrWorkBucketStage stage = LxrRCProcessIncs;
        if (kind == WorkKind::Decrements)
        {
            stage = engine->m_pause_kind == LxrPauseKind::Full || !engine->m_configuration.lazyDecrements
                ? LxrRCProcessDecs : LxrWorkBucketStage::Unconstrained;
            if (engine->m_marking.load(std::memory_order_acquire))
            {
                result = engine->QueueTrace(references, count);
            }
        }
        if (result == LxrRcResult::Success)
        {
            result = engine->SubmitWork(
                engine->NewWork(kind, references, count, stage, trace), parent);
        }
        if (result == LxrRcResult::Success)
        {
            count = 0;
        }
        return result;
    }

    LxrRcResult Append(uintptr_t reference, bool traceChildren)
    {
        if (count != 0 && trace != traceChildren)
        {
            LxrRcResult result = Flush();
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        trace = traceChildren;
        references[count++] = reference;
        return count == BufferCapacity ? Flush() : LxrRcResult::Success;
    }
};

struct LxrReferenceCounting::TraceBatch
{
    LxrReferenceCounting* engine;
    size_t count = 0;
    bool registered = false;
    bool accounted = false;
    uintptr_t references[FullTraceBatchCapacity];

    explicit TraceBatch(LxrReferenceCounting* owner) : engine(owner)
    {
        // Unpublished targets retain completion ownership until copied work takes over.
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_TRACE_BATCH_EARLY_CONTROL)
        uint64_t pending = engine->m_trace_pending.load(std::memory_order_relaxed);
        while (pending != UINT64_MAX &&
            !engine->m_trace_pending.compare_exchange_weak(
                pending, pending + 1, std::memory_order_acq_rel))
        {
        }
        if (pending == UINT64_MAX)
        {
            engine->Fail(LxrRcResult::CapacityExceeded);
            return;
        }
        registered = true;
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_TRACE_BATCH_EARLY_CONTROL
        if (engine->m_storage_diagnostics.load(std::memory_order_relaxed))
        {
            accounted = true;
            engine->m_trace_batch_metrics[0].fetch_add(1, std::memory_order_relaxed);
            uint64_t current = engine->m_trace_batch_metrics[3].fetch_add(
                sizeof(TraceBatch), std::memory_order_relaxed) + sizeof(TraceBatch);
            uint64_t high = engine->m_trace_batch_metrics[4].load(std::memory_order_relaxed);
            while (high < current && !engine->m_trace_batch_metrics[4].compare_exchange_weak(
                high, current, std::memory_order_relaxed))
            {
            }
            current = engine->m_storage_metrics[1].fetch_add(
                sizeof(TraceBatch), std::memory_order_relaxed) + sizeof(TraceBatch);
            high = engine->m_storage_metrics[2].load(std::memory_order_relaxed);
            while (high < current && !engine->m_storage_metrics[2].compare_exchange_weak(
                high, current, std::memory_order_relaxed))
            {
            }
        }
    }

    TraceBatch(const TraceBatch&) = delete;
    TraceBatch& operator=(const TraceBatch&) = delete;

    ~TraceBatch()
    {
        if (count != 0 && engine->GetFailure() == LxrRcResult::Success)
        {
            engine->Fail(LxrRcResult::InvariantViolation);
        }
        if (registered && engine->m_trace_pending.fetch_sub(1, std::memory_order_acq_rel) == 0)
        {
            engine->Fail(LxrRcResult::InvariantViolation);
        }
        if (accounted)
        {
            engine->m_trace_batch_metrics[3].fetch_sub(sizeof(TraceBatch), std::memory_order_relaxed);
            engine->m_storage_metrics[1].fetch_sub(sizeof(TraceBatch), std::memory_order_relaxed);
        }
    }

    LxrRcResult Append(uintptr_t target)
    {
        if (count == FullTraceBatchCapacity)
        {
            return engine->Fail(LxrRcResult::InvariantViolation);
        }
        if (engine->m_storage_diagnostics.load(std::memory_order_relaxed))
        {
            engine->m_trace_batch_metrics[1].fetch_add(1, std::memory_order_relaxed);
        }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_TRACE_BATCH_UNBATCHED_CONTROL)
        return engine->QueueTrace(&target, 1);
#else
        references[count++] = target;
        return count == FullTraceBatchCapacity ? Flush() : LxrRcResult::Success;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_TRACE_BATCH_UNBATCHED_CONTROL
    }

    LxrRcResult Flush()
    {
        if (count == 0)
        {
            return LxrRcResult::Success;
        }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_TRACE_BATCH_DROP_CONTROL)
        return LxrRcResult::Success;
#else
        LxrRcResult result = engine->QueueTrace(references, count);
        if (result == LxrRcResult::Success)
        {
            count = 0;
            if (engine->m_storage_diagnostics.load(std::memory_order_relaxed))
            {
                engine->m_trace_batch_metrics[2].fetch_add(1, std::memory_order_relaxed);
            }
        }
        return result;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_TRACE_BATCH_DROP_CONTROL
    }
};

struct LxrReferenceCounting::DeathContext
{
    LxrReferenceCounting* engine;
    ReferenceBatch* children;
    size_t worker;
};

// One cache line group per worker, written only by that worker with plain relaxed stores, and summed when
// read. The last entry is shared by callers that are not workers and is updated with atomic additions.
// The hot pause counters (IncrementEntries, DecrementEntries, Promotions, ObjectScans, ObjectSizeQueries,
// Deaths, RootIncrements, RootDecrements) use the slot of their Metric index.
struct alignas(64) LxrReferenceCounting::WorkerCounters
{
    std::atomic<uint64_t> values[24]{};
};

namespace
{
    constexpr size_t PromotedBytes = Epochs + 1;
    static_assert(PromotedBytes < 24);
}

LxrRcMutator::LxrRcMutator()
    : engine(nullptr), state(0), identity(0), owner(0), allocator(),
      increments(nullptr), decrements(nullptr), publishedCursor(0), publishedLimit(0), birthBlock(0),
      pendingFastStart(0), pendingFastSize(0)
{
}

LxrReferenceCounting::LxrReferenceCounting()
    : m_configuration{}, m_object_model{}, m_observer(false), m_runtime{},
      m_capture(nullptr), m_capture_context(nullptr),
      m_layout{}, m_metadata(), m_blocks(), m_counts(), m_allocation(), m_large(),
      m_scheduler(), m_coordinator(), m_arena(nullptr), m_extents(nullptr),
      m_live_bytes(0), m_reclaimed_bytes(0), m_trace_locks(nullptr), m_trace_scans(nullptr),
      m_scan_work(nullptr), m_trace_phase(LxrTracePhase::Inactive), m_marking(false),
      m_hold_trace(false), m_scan_checkpoint(0), m_scan_checkpoint_active(false),
      m_trace_pending(0), m_trace_metrics{},
      m_pause_kind(LxrPauseKind::RefCount), m_cycle_sweep(false),
      m_mutators(nullptr), m_mutator_chunks(nullptr),
      m_banks(nullptr), m_roots{nullptr, nullptr}, m_root_counts{0, 0},
      m_current_roots(0), m_dead_large(nullptr), m_mature_candidates(nullptr), m_dead_large_count(0),
      m_producer_bank(0), m_work(nullptr), m_trace_todo(nullptr), m_next_id(1),
      m_phase(LxrRcPhase::Uninitialized), m_failure(LxrRcResult::Success), m_metrics{}
{
}

LxrReferenceCounting::~LxrReferenceCounting()
{
    Shutdown();
}

LxrRcResult LxrReferenceCounting::Initialize(
    const LxrRcConfiguration& configuration, const LxrPauseRuntimeAdapter& runtime,
    const LxrRcObjectModel* objectModel)
{
    if (GetPhase() != LxrRcPhase::Uninitialized ||
        configuration.immixBytes == 0 ||
        configuration.immixBytes % ImmixBlockGeometry::BlockBytes != 0 ||
        configuration.largeObjectBytes < LxrLargeObjectSpace::MinimumObjectBytes ||
        configuration.largeObjectBytes % LxrLargeObjectSpace::PageBytes != 0 ||
        configuration.maximumMutators == 0 || configuration.maximumRoots == 0 ||
        configuration.workers == 0 || runtime.suspend == nullptr || runtime.resume == nullptr ||
        configuration.minimumReuseLines == 0 ||
        configuration.minimumReuseLines > ImmixBlockGeometry::LinesPerBlock ||
        configuration.bufferSize == 0 || configuration.bufferSize > BufferCapacity ||
        configuration.defragHeadroomPercent > 50 || configuration.chunkDefragPercent > 100 ||
        configuration.immixBytes > SIZE_MAX - configuration.largeObjectBytes)
    {
        return LxrRcResult::InvalidArgument;
    }
    uint8_t logBits;
    switch (configuration.referenceCountBits)
    {
        case 2: logBits = 1; break;
        case 4: logBits = 2; break;
        case 8: logBits = 3; break;
        default: return LxrRcResult::InvalidArgument;
    }
    m_configuration = configuration;
    m_initial_immix = configuration.immixBytes;
    m_initial_large = configuration.largeObjectBytes;
    m_configuration.immixBytes = configuration.immixReservationBytes == 0
        ? configuration.immixBytes : configuration.immixReservationBytes;
    m_configuration.largeObjectBytes = configuration.largeObjectReservationBytes == 0
        ? configuration.largeObjectBytes : configuration.largeObjectReservationBytes;
    if (m_configuration.immixBytes < m_initial_immix ||
        m_configuration.largeObjectBytes < m_initial_large ||
        m_configuration.immixBytes % ImmixBlockGeometry::BlockBytes != 0 ||
        m_configuration.largeObjectBytes % ImmixBlockGeometry::BlockBytes != 0 ||
        m_configuration.immixBytes > SIZE_MAX - m_configuration.largeObjectBytes)
    {
        return LxrRcResult::InvalidArgument;
    }
    if (m_configuration.hardLimitBytes == 0)
    {
        m_configuration.hardLimitBytes = GetReservationBytes();
    }
    if (m_initial_immix + m_initial_large > m_configuration.hardLimitBytes ||
        m_configuration.hardLimitBytes > GetReservationBytes())
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    m_immix_capacity.store(m_initial_immix, std::memory_order_relaxed);
    m_object_model = objectModel == nullptr ? LxrRcObjectModel{} : *objectModel;
    m_runtime = runtime;
    LxrRcResult buffers = InitializeBuffers(slots);
    if (buffers != LxrRcResult::Success)
    {
        return buffers;
    }
    if (m_layout.Create(logBits, &m_layout) != SideMetadataResult::Success ||
        m_metadata.Initialize(&m_layout) != SideMetadataResult::Success)
    {
        return Fail(LxrRcResult::MetadataFailure);
    }
    m_arena = GCToOSInterface::VirtualReserve(
        GetReservationBytes(), ImmixBlockGeometry::BlockBytes, VirtualReserveFlags::None);
    if (m_arena == nullptr ||
        reinterpret_cast<uintptr_t>(m_arena) >= LxrSideMetadataLayout::AddressLimit ||
        GetReservationBytes() > LxrSideMetadataLayout::AddressLimit -
            reinterpret_cast<uintptr_t>(m_arena) ||
        !GCToOSInterface::VirtualCommit(m_arena, m_initial_immix))
    {
        return Fail(LxrRcResult::AllocationFailure);
    }
    uintptr_t arena = reinterpret_cast<uintptr_t>(m_arena);
    if (m_metadata.CommitDataRange(arena, m_initial_immix) != SideMetadataResult::Success ||
        m_metadata.CommitDataRange(arena + m_configuration.immixBytes, m_initial_large) !=
            SideMetadataResult::Success ||
        m_blocks.Initialize(&m_metadata) != SideMetadataResult::Success ||
        m_blocks.RegisterBlockRange(arena, m_initial_immix) != SideMetadataResult::Success ||
        m_counts.Initialize(&m_metadata, &m_layout) != SideMetadataResult::Success ||
        m_allocation.Initialize(&m_metadata, &m_blocks, arena, m_initial_immix,
            configuration.minimumReuseLines) !=
            SideMetadataResult::Success ||
        m_large.Initialize(&m_metadata, m_configuration.largeObjectBytes,
            reinterpret_cast<void*>(arena + m_configuration.immixBytes), m_initial_large) !=
                LxrLargeObjectResult::Success)
    {
        return Fail(LxrRcResult::MetadataFailure);
    }
    if (m_scheduler.Initialize(configuration.workers, true, true,
            DecrementsDrained, OverallDrained, this) != LxrWorkSchedulerResult::Success)
    {
        return Fail(LxrRcResult::SchedulerFailure);
    }
    for (size_t index = 0; index < configuration.maximumMutators; index++)
    {
        if (m_mutators[index].allocator.Initialize(&m_allocation, index + 1) != SideMetadataResult::Success)
        {
            return Fail(LxrRcResult::AllocationFailure);
        }
    }
    if (configuration.evacuateNursery || configuration.evacuateMature)
    {
        size_t allocatorBytes;
        if (configuration.workers > (SIZE_MAX - configuration.maximumMutators) / 2 ||
            !TryMultiply(2 * configuration.workers, sizeof(ImmixBumpAllocator), &allocatorBytes) ||
            configuration.forwardingMask == 0 || configuration.forwardingShift >= 32 ||
            (configuration.forwardingMask >> configuration.forwardingShift) != 3)
        {
            return Fail(LxrRcResult::InvalidArgument);
        }
        m_copy_allocators = new (std::nothrow) ImmixBumpAllocator[2 * configuration.workers];
        if (m_copy_allocators == nullptr)
        {
            return Fail(LxrRcResult::OutOfMemory);
        }
        for (size_t index = 0; index < 2 * configuration.workers; index++)
        {
            if (m_copy_allocators[index].Initialize(
                    &m_allocation, configuration.maximumMutators + index + 1) != SideMetadataResult::Success)
            {
                return Fail(LxrRcResult::AllocationFailure);
            }
        }
    }
    LxrPauseRuntimeAdapter adapter{Suspend, Resume, this, runtime.finishedGc};
    if (m_coordinator.Initialize(&m_scheduler, adapter) != LxrPauseCoordinatorResult::Success)
    {
        return Fail(LxrRcResult::SchedulerFailure);
    }
    m_phase.store(LxrRcPhase::Mutator, std::memory_order_release);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::InitializeBuffers(size_t slots)
{
    size_t bytes;
    if (m_configuration.maximumMutators > SIZE_MAX / 2 ||
        !TryMultiply(2 * m_configuration.maximumMutators, sizeof(LogChunk), &bytes) ||
        !TryMultiply(slots, sizeof(uintptr_t), &bytes) ||
        !TryMultiply(slots, sizeof(std::atomic<uint16_t>), &bytes) ||
        !TryMultiply(slots, sizeof(std::atomic<size_t>), &bytes) ||
        !TryMultiply(slots, sizeof(TraceScan*), &bytes) ||
        !TryMultiply(m_configuration.maximumMutators, sizeof(LxrRcMutator), &bytes) ||
        !TryMultiply(m_configuration.maximumRoots, sizeof(uintptr_t), &bytes))
    {
        return LxrRcResult::CapacityExceeded;
    }
    m_worker_counters = new (std::nothrow) WorkerCounters[m_configuration.workers + 1];
    if (m_worker_counters == nullptr)
    {
        return LxrRcResult::OutOfMemory;
    }
    m_mutators = new (std::nothrow) LxrRcMutator[m_configuration.maximumMutators];
    m_mutator_chunks = new (std::nothrow) LogChunk[2 * m_configuration.maximumMutators];
    m_banks = new (std::nothrow) LogBank[2];
    if (!m_observer)
    {
        size_t objectSlots = GetObjectStorageSlots();
        if (!m_extent_storage.Reserve(objectSlots) || !m_trace_lock_storage.Reserve(objectSlots) ||
            !m_trace_scan_storage.Reserve(objectSlots) || !m_live_storage.Reserve(slots / 64) ||
            !m_allocated_block_storage.Reserve(m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes) ||
            !CommitPlanes(0, m_initial_immix / sizeof(uintptr_t)) ||
            !CommitPlanes(m_configuration.immixBytes / sizeof(uintptr_t),
                m_initial_large / sizeof(uintptr_t)))
        {
            return Fail(LxrRcResult::OutOfMemory);
        }
        m_extents = m_extent_storage.Get();
        m_trace_locks = m_trace_lock_storage.Get();
        m_trace_scans = m_trace_scan_storage.Get();
        m_live_objects = m_live_storage.Get();
    }
    m_roots[0] = new (std::nothrow) uintptr_t[m_configuration.maximumRoots]{};
    m_roots[1] = new (std::nothrow) uintptr_t[m_configuration.maximumRoots]{};
    if (m_configuration.largeObjectBytes != 0)
    {
        m_dead_large = new (std::nothrow) uintptr_t[
            m_configuration.largeObjectBytes / LxrLargeObjectSpace::PageBytes]{};
    }
    if (m_configuration.immixBytes != 0)
    {
        m_mature_candidates = new (std::nothrow) std::atomic<uint8_t>[
            m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes]{};
        if (m_configuration.evacuateMature)
        {
            m_evacuation_sources = new (std::nothrow) std::atomic<uint8_t>[
                m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes]{};
            if (m_evacuation_sources == nullptr)
            {
                return Fail(LxrRcResult::OutOfMemory);
            }
        }
    }
    if (m_mutators == nullptr || m_mutator_chunks == nullptr ||
        m_banks == nullptr || (!m_observer &&
            (m_extents == nullptr || m_trace_locks == nullptr || m_trace_scans == nullptr)) ||
        m_roots[0] == nullptr ||
        m_roots[1] == nullptr || (m_configuration.largeObjectBytes != 0 && m_dead_large == nullptr) ||
        (m_configuration.immixBytes != 0 && m_mature_candidates == nullptr))
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
    for (size_t index = 0; index < 2; index++)
    {
        LogBank& bank = m_banks[index];
        bank.capacity = m_observer ? slots : (m_initial_immix + m_initial_large) / sizeof(uintptr_t);
        for (LogStream* stream : {&bank.increments, &bank.decrements})
        {
            stream->capacity = bank.capacity;
            if (!stream->referenceStorage.Reserve(slots) ||
                !stream->referenceStorage.Commit(0, stream->capacity, false) ||
                (m_observer && (!stream->publicationStorage.Reserve(slots) ||
                    !stream->publicationStorage.Commit(0, stream->capacity))))
            {
                return Fail(LxrRcResult::OutOfMemory);
            }
            stream->references = stream->referenceStorage.Get();
            stream->published = stream->publicationStorage.Get();
            if (stream->references == nullptr || (m_observer && stream->published == nullptr))
            {
                return Fail(LxrRcResult::OutOfMemory);
            }
        }
    }
    for (size_t index = 0; index < m_configuration.maximumMutators; index++)
    {
        m_mutators[index].engine = this;
        m_mutators[index].owner = index + 1;
    }
    return LxrRcResult::Success;
}

bool LxrReferenceCounting::CommitPlanes(size_t first, size_t count)
{
    size_t objectFirst = GetObjectStorageIndex(first);
    size_t objectCount = GetObjectStorageIndex(first + count) - objectFirst;
    size_t immixSlots = m_configuration.immixBytes / sizeof(uintptr_t);
    size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
    return (first >= immixSlots || m_allocated_block_storage.Commit(first / blockSlots, count / blockSlots)) &&
        m_extent_storage.Commit(objectFirst, objectCount) &&
        m_trace_lock_storage.Commit(objectFirst, objectCount) &&
        m_trace_scan_storage.Commit(objectFirst, objectCount) &&
        m_live_storage.Commit(first / 64, count / 64);
}

bool LxrReferenceCounting::CommitTracePlanes()
{
    size_t immix = GetImmixCapacityBytes() / sizeof(uintptr_t);
    size_t largeFirst = m_configuration.immixBytes / sizeof(uintptr_t);
    size_t large = m_large.GetAdmittedBytes() / LxrLargeObjectSpace::PageBytes;
    return m_trace_lock_storage.Commit(0, immix) && m_trace_scan_storage.Commit(0, immix) &&
        m_trace_lock_storage.Commit(largeFirst, large) && m_trace_scan_storage.Commit(largeFirst, large);
}

size_t LxrReferenceCounting::GetTracePlaneBudget(size_t immixBytes, size_t largeBytes) const
{
    size_t page = GCToOSInterface::GetPageSize();
    size_t immix = immixBytes / sizeof(uintptr_t);
    size_t large = largeBytes / LxrLargeObjectSpace::PageBytes;
    size_t lockBytes = ((immix * sizeof(std::atomic<uint8_t>) + page - 1) & ~(page - 1)) +
        ((large * sizeof(std::atomic<uint8_t>) + page - 1) & ~(page - 1));
    size_t scanBytes = ((immix * sizeof(TraceScan*) + page - 1) & ~(page - 1)) +
        ((large * sizeof(TraceScan*) + page - 1) & ~(page - 1));
    return lockBytes + scanBytes;
}

size_t LxrReferenceCounting::GetObjectStorageIndex(size_t heapSlot) const
{
    size_t immixSlots = m_configuration.immixBytes / sizeof(uintptr_t);
    return heapSlot < immixSlots ? heapSlot :
        immixSlots + (heapSlot - immixSlots) / (LxrLargeObjectSpace::PageBytes / sizeof(uintptr_t));
}

size_t LxrReferenceCounting::GetObjectStorageSlots() const
{
    return m_configuration.immixBytes / sizeof(uintptr_t) +
        m_configuration.largeObjectBytes / LxrLargeObjectSpace::PageBytes;
}

std::atomic<size_t>& LxrReferenceCounting::ExtentSlot(size_t heapSlot) const
{
    return m_extents[GetObjectStorageIndex(heapSlot)];
}

std::atomic<uint8_t>& LxrReferenceCounting::TraceLockSlot(size_t heapSlot) const
{
    return m_trace_locks[GetObjectStorageIndex(heapSlot)];
}

LxrReferenceCounting::TraceScan*& LxrReferenceCounting::TraceScanSlot(size_t heapSlot) const
{
    return m_trace_scans[GetObjectStorageIndex(heapSlot)];
}

LxrRcResult LxrReferenceCounting::InitializeObserver(size_t admittedSlots, size_t maximumMutators)
{
    if (GetPhase() != LxrRcPhase::Uninitialized || admittedSlots == 0 || maximumMutators == 0)
    {
        return LxrRcResult::InvalidArgument;
    }
    m_observer = true;
    m_configuration = {0, 0, maximumMutators, 1, 0, 0};
    LxrRcResult result = InitializeBuffers(admittedSlots);
    if (result == LxrRcResult::Success)
    {
        m_phase.store(LxrRcPhase::Mutator, std::memory_order_release);
    }
    return result;
}

LxrRcResult LxrReferenceCounting::CaptureClaimedWrite(
    LxrRcMutator* mutator, Object** slot, Object* oldTarget)
{
    if (!m_observer || GetPhase() != LxrRcPhase::Mutator ||
        mutator == nullptr || mutator->engine != this ||
        mutator->state.load(std::memory_order_acquire) != 2 || slot == nullptr)
    {
        return Fail(LxrRcResult::InvalidArgument);
    }
    LxrRcResult result = oldTarget == nullptr ? LxrRcResult::Success :
        Append(mutator, false, reinterpret_cast<uintptr_t>(oldTarget));
    if (result == LxrRcResult::Success)
    {
        result = Append(mutator, true, reinterpret_cast<uintptr_t>(slot));
        m_metrics[LoggedSlots].fetch_add(1, std::memory_order_relaxed);
    }
    return result;
}

LxrRcResult LxrReferenceCounting::DrainObservedLogs(
    LxrRcUnlogObservedSlot unlog, LxrRcObservedEntry observe, void* context)
{
    if (!m_observer || GetPhase() != LxrRcPhase::Mutator ||
        unlog == nullptr || observe == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    LxrRcResult result = FlushMutators();
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    LogBank& bank = m_banks[m_producer_bank.load(std::memory_order_relaxed)];
    for (bool increment : {true, false})
    {
        LogStream& stream = increment ? bank.increments : bank.decrements;
        size_t limit = stream.reserved.load(std::memory_order_acquire);
        for (size_t cursor = 0; cursor < limit;)
        {
            size_t count;
            result = ReadPublishedRange(stream, cursor, limit, &count);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            for (size_t index = cursor; index < cursor + count; index++)
            {
                Object* current = nullptr;
                if (increment)
                {
                    Object** slot = reinterpret_cast<Object**>(stream.references[index]);
                    current = *slot;
                    unlog(slot, context);
                    CountWorker(SIZE_MAX, IncrementEntries, 1);
                }
                else
                {
                    CountWorker(SIZE_MAX, DecrementEntries, 1);
                }
                observe(increment, stream.references[index], current, context);
            }
            cursor += count;
        }
    }
    result = ResetBank(bank);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    return GetFailure();
}

void LxrReferenceCounting::Shutdown()
{
    if (GetPhase() == LxrRcPhase::Shutdown)
    {
        return;
    }
    m_phase.store(LxrRcPhase::Shutdown, std::memory_order_release);
    m_scheduler.Shutdown();
    DeleteRememberedChunks();
    DeleteWork();
    m_large.Shutdown();
    m_allocation.Shutdown();
    m_counts.Shutdown();
    m_blocks.Shutdown();
    m_metadata.Shutdown();
    if (m_arena != nullptr)
    {
        GCToOSInterface::VirtualRelease(m_arena, GetReservationBytes());
        m_arena = nullptr;
    }
    if (m_banks != nullptr)
    {
        for (size_t index = 0; index < 2; index++)
        {
            LogBank& bank = m_banks[index];
            bank.increments.referenceStorage.Shutdown();
            bank.increments.publicationStorage.Shutdown();
            bank.decrements.referenceStorage.Shutdown();
            bank.decrements.publicationStorage.Shutdown();
        }
    }
    delete[] m_banks;
    delete[] m_mutators;
    delete[] m_copy_allocators;
    delete[] m_worker_counters;
    delete[] m_evacuation_sources;
    delete[] m_mutator_chunks;
    m_extent_storage.Shutdown();
    m_trace_lock_storage.Shutdown();
    m_trace_scan_storage.Shutdown();
    m_live_storage.Shutdown();
    m_allocated_block_storage.Shutdown();
    delete[] m_roots[0];
    delete[] m_roots[1];
    delete[] m_dead_large;
    delete[] m_mature_candidates;
    m_banks = nullptr;
    m_mutators = nullptr;
    m_copy_allocators = nullptr;
    m_worker_counters = nullptr;
    m_evacuation_sources = nullptr;
    m_mutator_chunks = nullptr;
    m_extents = nullptr;
    m_trace_locks = nullptr;
    m_trace_scans = nullptr;
    m_roots[0] = nullptr;
    m_roots[1] = nullptr;
    m_dead_large = nullptr;
    m_mature_candidates = nullptr;
}

LxrRcResult LxrReferenceCounting::Fail(LxrRcResult result)
{
    LxrRcResult expected = LxrRcResult::Success;
    m_failure.compare_exchange_strong(expected, result, std::memory_order_seq_cst);
    m_phase.store(LxrRcPhase::Faulted, std::memory_order_release);
    return result;
}

LxrRcResult LxrReferenceCounting::FromMetadata(SideMetadataResult result)
{
    return result == SideMetadataResult::Success
        ? LxrRcResult::Success : Fail(LxrRcResult::MetadataFailure);
}

void LxrReferenceCounting::CountWorker(size_t worker, size_t counter, uint64_t amount)
{
    if (worker < m_configuration.workers)
    {
        std::atomic<uint64_t>& value = m_worker_counters[worker].values[counter];
        value.store(value.load(std::memory_order_relaxed) + amount, std::memory_order_relaxed);
    }
    else
    {
        m_worker_counters[m_configuration.workers].values[counter].fetch_add(amount, std::memory_order_relaxed);
    }
}

uint64_t LxrReferenceCounting::SumWorker(size_t counter) const
{
    uint64_t sum = 0;
    if (m_worker_counters != nullptr)
    {
        for (size_t worker = 0; worker <= m_configuration.workers; worker++)
        {
            sum += m_worker_counters[worker].values[counter].load(std::memory_order_relaxed);
        }
    }
    return sum;
}

size_t LxrReferenceCounting::GetPauseSurvivorBytes() const
{
    return static_cast<size_t>(SumWorker(PromotedBytes));
}

LxrRcResult LxrReferenceCounting::FromScheduler(LxrWorkSchedulerResult result)
{
    return result == LxrWorkSchedulerResult::Success
        ? LxrRcResult::Success : Fail(LxrRcResult::SchedulerFailure);
}

LxrRcMutator* LxrReferenceCounting::FindMutator(void* identity) const
{
    if (m_mutators == nullptr || identity == nullptr)
    {
        return nullptr;
    }
    uintptr_t key = reinterpret_cast<uintptr_t>(identity);
    for (size_t index = 0; index < m_configuration.maximumMutators; index++)
    {
        if (m_mutators[index].state.load(std::memory_order_acquire) == 2 &&
            m_mutators[index].identity.load(std::memory_order_relaxed) == key)
        {
            return &m_mutators[index];
        }
    }
    return nullptr;
}

LxrRcMutator* LxrReferenceCounting::FindCachedMutator(void* cached, void* identity) const
{
    uintptr_t address = reinterpret_cast<uintptr_t>(cached);
    uintptr_t base = reinterpret_cast<uintptr_t>(m_mutators);
    if (m_mutators == nullptr || address < base ||
        address - base >= m_configuration.maximumMutators * sizeof(LxrRcMutator) ||
        (address - base) % sizeof(LxrRcMutator) != 0)
    {
        return nullptr;
    }
    LxrRcMutator* mutator = static_cast<LxrRcMutator*>(cached);
    return mutator->state.load(std::memory_order_acquire) == 2 &&
        mutator->identity.load(std::memory_order_relaxed) == reinterpret_cast<uintptr_t>(identity)
        ? mutator : nullptr;
}

LxrRcResult LxrReferenceCounting::GetAllocationSpan(
    LxrRcMutator* mutator, uintptr_t* cursor, uintptr_t* limit)
{
    if (mutator == nullptr || mutator->engine != this ||
        mutator->state.load(std::memory_order_acquire) != 2 ||
        GetPhase() != LxrRcPhase::Mutator || cursor == nullptr || limit == nullptr ||
        mutator->publishedLimit != 0)
    {
        return LxrRcResult::InvalidArgument;
    }
    *cursor = 0;
    *limit = 0;
    uintptr_t first = mutator->allocator.GetCursor();
    uintptr_t end = mutator->allocator.GetLimit();
    if (end >= first && end - first >= MIN_OBJECT_SIZE)
    {
        mutator->publishedCursor = first;
        mutator->publishedLimit = end;
        *cursor = first;
        *limit = end;
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::RecordFastAllocation(
    LxrRcMutator* mutator, Object* object, size_t size, bool* fast)
{
    if (mutator == nullptr || mutator->engine != this || fast == nullptr ||
        mutator->state.load(std::memory_order_acquire) != 2)
    {
        return LxrRcResult::InvalidArgument;
    }
    *fast = false;
    mutator->pendingFastStart = 0;
    mutator->pendingFastSize = 0;
    uintptr_t start = Start(object);
    if (mutator->publishedLimit == 0 || start < mutator->publishedCursor)
    {
        return LxrRcResult::Success;
    }
    if (start != mutator->publishedCursor || size > mutator->publishedLimit - start ||
        size < MIN_OBJECT_SIZE || size % sizeof(uintptr_t) != 0)
    {
        return LxrRcResult::InvariantViolation;
    }
    mutator->publishedCursor += size;
    mutator->pendingFastStart = start;
    mutator->pendingFastSize = size;
    m_metrics[Allocations].fetch_add(1, std::memory_order_relaxed);
    m_metrics[AllocatedBytes].fetch_add(size, std::memory_order_relaxed);
    *fast = true;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::RetireAllocationSpan(LxrRcMutator* mutator, uintptr_t cursor)
{
    if (mutator == nullptr || mutator->engine != this ||
        mutator->publishedLimit == 0 || cursor != mutator->publishedCursor ||
        mutator->allocator.SetCursor(cursor) != SideMetadataResult::Success)
    {
        return LxrRcResult::InvariantViolation;
    }
    mutator->publishedCursor = 0;
    mutator->publishedLimit = 0;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::AttachMutator(void* identity, LxrRcMutator** mutator)
{
    if (identity == nullptr || mutator == nullptr || GetPhase() != LxrRcPhase::Mutator ||
        FindMutator(identity) != nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    *mutator = nullptr;
    for (size_t index = 0; index < m_configuration.maximumMutators; index++)
    {
        LxrRcMutator& candidate = m_mutators[index];
        uint8_t expected = 0;
        if (!candidate.state.compare_exchange_strong(expected, 1, std::memory_order_acq_rel))
        {
            continue;
        }
        candidate.engine = this;
        candidate.owner = index + 1;
        candidate.identity.store(reinterpret_cast<uintptr_t>(identity), std::memory_order_relaxed);
        candidate.increments = nullptr;
        candidate.decrements = nullptr;
        candidate.publishedCursor = 0;
        candidate.publishedLimit = 0;
        candidate.birthBlock = 0;
        candidate.pendingFastStart = 0;
        candidate.pendingFastSize = 0;
        candidate.state.store(2, std::memory_order_release);
        *mutator = &candidate;
        return LxrRcResult::Success;
    }
    return LxrRcResult::CapacityExceeded;
}

LxrRcResult LxrReferenceCounting::Publish(void** value)
{
    LogChunk* chunk = static_cast<LogChunk*>(*value);
    if (chunk == nullptr || chunk->count == 0)
    {
        return LxrRcResult::Success;
    }
    LogBank& bank = m_banks[m_producer_bank.load(std::memory_order_relaxed)];
    LogStream& stream = chunk->increment ? bank.increments : bank.decrements;
    LxrRcResult result = PublishReferences(stream, stream.capacity, chunk->references, chunk->count);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    chunk->count = 0;
    *value = nullptr;
    m_metrics[BufferFlushes].fetch_add(1, std::memory_order_relaxed);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::PublishReferences(
    LogStream& stream, size_t capacity, const uintptr_t* references, size_t count)
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_LOG_CAPACITY_CONTROL)
    capacity--;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_LOG_CAPACITY_CONTROL
    if (count == 0 || count > BufferCapacity)
    {
        return Fail(LxrRcResult::CapacityExceeded);
    }
    if (stream.published == nullptr)
    {
        for (size_t index = 0; index < count; index++)
        {
            if ((references[index] & ~LogReferenceMask) != 0)
            {
                return Fail(LxrRcResult::InvariantViolation);
            }
        }
    }
    size_t start = stream.reserved.load(std::memory_order_relaxed);
    while (true)
    {
        if (count == 0 || count > BufferCapacity ||
            count > capacity || start > capacity - count)
        {
            return Fail(LxrRcResult::CapacityExceeded);
        }
        if (stream.reserved.compare_exchange_weak(
                start, start + count, std::memory_order_relaxed))
        {
            break;
        }
    }
    if (stream.published != nullptr)
    {
        memcpy(stream.references + start, references, count * sizeof(uintptr_t));
        stream.published[start].store(static_cast<uint16_t>(count), std::memory_order_release);
    }
    else
    {
        memcpy(stream.references + start + 1, references + 1, (count - 1) * sizeof(uintptr_t));
        // The aligned first word is published atomically only after the other payload words.
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_LOG_PUBLICATION_CONTROL)
        VolatileStore(stream.references + start, references[0] | (static_cast<uintptr_t>(count) << LogCountShift));
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_LOG_PUBLICATION_CONTROL
    }
    if (m_storage_diagnostics.load(std::memory_order_relaxed))
    {
        size_t high = stream.highWater.load(std::memory_order_relaxed);
        while (high < start + count && !stream.highWater.compare_exchange_weak(
            high, start + count, std::memory_order_relaxed))
        {
        }
        stream.flushes.fetch_add(1, std::memory_order_relaxed);
        stream.copiedBytes.fetch_add(count * sizeof(uintptr_t), std::memory_order_relaxed);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReadPublishedRange(
    const LogStream& stream, size_t cursor, size_t limit, size_t* count)
{
    if (cursor >= limit || limit > stream.capacity)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    size_t value = stream.published != nullptr
        ? stream.published[cursor].load(std::memory_order_acquire)
        : VolatileLoad(stream.references + cursor) >> LogCountShift;
    if (value == 0 || value > BufferCapacity || value > limit - cursor)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    *count = value;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ResetBank(LogBank& bank)
{
    for (LogStream* stream : {&bank.increments, &bank.decrements})
    {
        size_t limit = stream->reserved.load(std::memory_order_acquire);
        for (size_t cursor = 0; cursor < limit;)
        {
            size_t count;
            LxrRcResult result = ReadPublishedRange(*stream, cursor, limit, &count);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            if (stream->published != nullptr)
            {
                stream->published[cursor].store(0, std::memory_order_relaxed);
            }
            else
            {
                stream->references[cursor] &= LogReferenceMask;
            }
            if (m_storage_diagnostics.load(std::memory_order_relaxed))
            {
                stream->resetBytes.fetch_add(stream->published != nullptr
                    ? sizeof(uint16_t) : sizeof(uintptr_t), std::memory_order_relaxed);
            }
            cursor += count;
        }
        stream->reserved.store(0, std::memory_order_relaxed);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::DetachMutator(LxrRcMutator* mutator)
{
    if (mutator == nullptr || mutator->engine != this ||
        mutator->state.load(std::memory_order_acquire) != 2)
    {
        return LxrRcResult::InvalidArgument;
    }
    LxrRcResult published = Publish(&mutator->decrements);
    if (published == LxrRcResult::Success)
    {
        published = Publish(&mutator->increments);
    }
    if (published != LxrRcResult::Success)
    {
        return published;
    }
    m_metrics[ThreadFlushes].fetch_add(1, std::memory_order_relaxed);
    if (m_observer)
    {
        mutator->identity.store(0, std::memory_order_relaxed);
        mutator->state.store(0, std::memory_order_release);
        return LxrRcResult::Success;
    }
    ImmixAllocationStatus status;
    SideMetadataResult result = mutator->allocator.Reset(&status);
    mutator->identity.store(0, std::memory_order_relaxed);
    mutator->state.store(
        result == SideMetadataResult::Success && status == ImmixAllocationStatus::Ready ? 0 : 3,
        std::memory_order_release);
    if (result != SideMetadataResult::Success)
    {
        return Fail(LxrRcResult::AllocationFailure);
    }
    if (status != ImmixAllocationStatus::Ready && status != ImmixAllocationStatus::Contended)
    {
        return Fail(LxrRcResult::AllocationFailure);
    }
    // A contended allocator remains engine-owned, not in the departing thread's TLS.
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::Append(LxrRcMutator* mutator, bool increment, uintptr_t value)
{
    void** active = increment ? &mutator->increments : &mutator->decrements;
    LogChunk* chunk = static_cast<LogChunk*>(*active);
    if (chunk == nullptr)
    {
        size_t index = 2 * (mutator->owner - 1) + (increment ? 0 : 1);
        chunk = &m_mutator_chunks[index];
        chunk->count = 0;
        chunk->increment = increment;
        *active = chunk;
    }
    chunk->references[chunk->count++] = value;
    if (chunk->count == m_configuration.bufferSize)
    {
        return Publish(active);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::LogBeforeWrite(
    LxrRcMutator* mutator, Object** slot, Object* oldTarget)
{
    if (GetPhase() != LxrRcPhase::Mutator)
    {
        return GetFailure() == LxrRcResult::Success ? LxrRcResult::InvalidPhase : GetFailure();
    }
    if (mutator == nullptr || mutator->engine != this ||
        mutator->state.load(std::memory_order_acquire) != 2 || slot == nullptr)
    {
        return Fail(LxrRcResult::InvalidArgument);
    }
    uintptr_t observed;
    bool exchanged;
    SideMetadataResult result = m_metadata.CompareExchange(
        LxrSideMetadataKind::FieldUnlogged, reinterpret_cast<uintptr_t>(slot),
        0, 1, SideMetadataMemoryOrder::SequentiallyConsistent, &observed, &exchanged);
    if (result != SideMetadataResult::Success)
    {
        return FromMetadata(result);
    }
    if (!exchanged)
    {
        return LxrRcResult::Success;
    }
    if (oldTarget != nullptr)
    {
        if (!OwnsAddress(Start(oldTarget)))
        {
            if (m_object_model.isImmortal == nullptr ||
                !m_object_model.isImmortal(oldTarget, m_object_model.context))
            {
                return Fail(LxrRcResult::UnsupportedLifetime);
            }
            oldTarget = nullptr;
        }
    }
    if (oldTarget != nullptr)
    {
        uintptr_t count;
        result = m_metadata.Load(LxrSideMetadataKind::ReferenceCount, Start(oldTarget),
            SideMetadataMemoryOrder::Relaxed, &count);
        if (result != SideMetadataResult::Success)
        {
            return FromMetadata(result);
        }
        m_metrics[OldTargetChecks].fetch_add(1, std::memory_order_relaxed);
        _ASSERTE(count != 0);
        if (count == 0)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
        LxrRcResult appended = Append(mutator, false, reinterpret_cast<uintptr_t>(oldTarget));
        if (appended != LxrRcResult::Success)
        {
            return appended;
        }
    }
    LxrRcResult appended = Append(mutator, true, reinterpret_cast<uintptr_t>(slot));
    if (appended == LxrRcResult::Success)
    {
        m_metrics[LoggedSlots].fetch_add(1, std::memory_order_relaxed);
    }
    return appended;
}

uintptr_t LxrReferenceCounting::Start(Object* object)
{
    return reinterpret_cast<uintptr_t>(object) - sizeof(ObjHeader);
}

Object* LxrReferenceCounting::Reference(uintptr_t start)
{
    return reinterpret_cast<Object*>(start + sizeof(ObjHeader));
}

bool LxrReferenceCounting::IsImmix(uintptr_t start) const
{
    uintptr_t arena = reinterpret_cast<uintptr_t>(m_arena);
    return start >= arena && start - arena < m_immix_capacity.load(std::memory_order_acquire);
}

bool LxrReferenceCounting::IsLarge(uintptr_t start) const
{
    uintptr_t arena = m_large.GetReservationStart();
    return start >= arena && start - arena < m_large.GetAdmittedBytes();
}

LxrRcResult LxrReferenceCounting::Allocate(
    LxrRcMutator* mutator, size_t completeSize, Object** object)
{
    if (object == nullptr || mutator == nullptr || mutator->engine != this ||
        mutator->state.load(std::memory_order_acquire) != 2 ||
        completeSize < MIN_OBJECT_SIZE || completeSize % sizeof(uintptr_t) != 0 ||
        GetPhase() != LxrRcPhase::Mutator || m_observer)
    {
        return LxrRcResult::InvalidArgument;
    }
    *object = nullptr;
    uintptr_t start = 0;
    if (LxrLargeObjectSpace::ShouldUseLargeObjectSpace(completeSize))
    {
        LxrLargeObjectAllocation allocation;
        if (m_large.Allocate(completeSize, sizeof(uintptr_t), 0, mutator->owner, &allocation) !=
            LxrLargeObjectResult::Success)
        {
            return LxrRcResult::AllocationFailure;
        }
        start = allocation.object;
    }
    else
    {
        ImmixAllocationRequest request{completeSize, sizeof(uintptr_t), 0};
        ImmixAllocationStatus status;
        SideMetadataResult result = mutator->allocator.TryAllocate(request, &start, &status);
        if (result != SideMetadataResult::Success)
        {
            return FromMetadata(result);
        }
        bool recycled = m_configuration.lineRecycling;
        size_t attempts = m_configuration.immixBytes / ImmixBlockGeometry::LineBytes +
            m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes;
        while (status != ImmixAllocationStatus::Allocated && attempts-- != 0)
        {
            result = mutator->allocator.TryRefill(
                recycled ? ImmixAllocationSource::MutatorRecycled : ImmixAllocationSource::MutatorFresh,
                m_marking.load(std::memory_order_acquire)
                    ? ImmixMarkingContext::ConcurrentOrFinalMark : ImmixMarkingContext::Inactive, &status);
            if (result == SideMetadataResult::Success && status == ImmixAllocationStatus::Exhausted && recycled)
            {
                recycled = false;
                result = mutator->allocator.TryRefill(
                    ImmixAllocationSource::MutatorFresh,
                    m_marking.load(std::memory_order_acquire)
                        ? ImmixMarkingContext::ConcurrentOrFinalMark : ImmixMarkingContext::Inactive, &status);
            }
            if (result != SideMetadataResult::Success || status != ImmixAllocationStatus::Ready)
            {
                return LxrRcResult::AllocationFailure;
            }
            result = mutator->allocator.TryAllocate(request, &start, &status);
            if (result != SideMetadataResult::Success)
            {
                return LxrRcResult::AllocationFailure;
            }
        }
        if (status != ImmixAllocationStatus::Allocated)
        {
            return LxrRcResult::AllocationFailure;
        }
    }
    *object = Reference(start);
    if (IsImmix(start))
    {
        uintptr_t block = start & ~ImmixBlockGeometry::BlockMask;
        if (mutator->birthBlock != block)
        {
            m_allocated_block_storage.Get()[(block - GetReservationStart()) /
                ImmixBlockGeometry::BlockBytes].store(1, std::memory_order_release);
            mutator->birthBlock = block;
        }
    }
    if (IsLarge(start) && m_marking.load(std::memory_order_acquire) &&
        FromMetadata(m_metadata.Store(LxrSideMetadataKind::ObjectMark, start, 1,
            SideMetadataMemoryOrder::SequentiallyConsistent)) != LxrRcResult::Success)
    {
        return GetFailure();
    }
    m_metrics[Allocations].fetch_add(1, std::memory_order_relaxed);
    m_metrics[AllocatedBytes].fetch_add(completeSize, std::memory_order_relaxed);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::CompleteAllocation(
    Object* object, size_t completeSize, AllocationCompleteFlags flags, LxrRcMutator* mutator)
{
    if (object == nullptr || completeSize < MIN_OBJECT_SIZE ||
        completeSize % sizeof(uintptr_t) != 0 || GetPhase() != LxrRcPhase::Mutator)
    {
        return Fail(LxrRcResult::InvalidArgument);
    }
    uint32_t unsupported = static_cast<uint32_t>(AllocationCompleteFlags::FrozenObjectHeap);
    if (!m_object_model.admitFinalizable)
    {
        unsupported |= static_cast<uint32_t>(AllocationCompleteFlags::Finalizable);
    }
    if (m_object_model.getLoaderAllocatorSlot == nullptr)
    {
        unsupported |= static_cast<uint32_t>(AllocationCompleteFlags::Collectible);
    }
    if ((static_cast<uint32_t>(flags) & unsupported) != 0)
    {
        return Fail(LxrRcResult::UnsupportedLifetime);
    }
    uintptr_t start = Start(object);
    bool cleared = mutator != nullptr && mutator->engine == this &&
        mutator->state.load(std::memory_order_acquire) == 2 &&
        mutator->pendingFastStart == start && mutator->pendingFastSize == completeSize;
    if (mutator != nullptr && mutator->engine == this)
    {
        mutator->pendingFastStart = 0;
        mutator->pendingFastSize = 0;
    }
    size_t index = (start - GetReservationStart()) / sizeof(uintptr_t);
    LxrRcResult result;
    if (IsImmix(start))
    {
        if (ExtentSlot(index).load(std::memory_order_acquire) != 0)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
        result = FromMetadata(cleared ? m_counts.RegisterObjectInClearedSpan(start, completeSize) :
            m_counts.InitializeObjectQuiescent(start, completeSize));
    }
    else if (IsLarge(start) && start % LxrLargeObjectSpace::PageBytes == 0 &&
        m_metadata.IsDataRangeOwned(start, completeSize))
    {
        uintptr_t count;
        result = FromMetadata(m_counts.Load(start, &count));
    }
    else
    {
        return Fail(LxrRcResult::InvalidArgument);
    }
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    if (ExtentSlot(index).exchange(completeSize, std::memory_order_release) != 0)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    m_live_objects[index / 64].fetch_or(UINT64_C(1) << (index % 64), std::memory_order_release);
    m_live_bytes.fetch_add(completeSize, std::memory_order_relaxed);
    if (m_probe_start.load(std::memory_order_acquire) == start)
    {
        m_probe_births.fetch_add(1, std::memory_order_release);
    }
    return LxrRcResult::Success;
}

LxrReferenceCounting::Work* LxrReferenceCounting::NewWork(
    WorkKind kind, const uintptr_t* references, size_t count, LxrWorkBucketStage stage,
    bool trace, TraceScan* scan, bool borrow, bool logged)
{
    size_t capacity = kind == WorkKind::TraceObjects ? FullTraceBatchCapacity : BufferCapacity;
    if (count > capacity || (count != 0 && references == nullptr))
    {
        Fail(LxrRcResult::InvalidArgument);
        return nullptr;
    }
    uint64_t id = m_next_id.load(std::memory_order_relaxed);
    while (id != UINT64_MAX &&
        !m_next_id.compare_exchange_weak(id, id + 1, std::memory_order_relaxed))
    {
    }
    if (id == UINT64_MAX)
    {
        Fail(LxrRcResult::CapacityExceeded);
        return nullptr;
    }
    Work* work = new (std::nothrow) Work(this, id, kind, references, count, stage, trace, scan, borrow, logged);
    if (work == nullptr || (count != 0 && work->references == nullptr))
    {
        delete work;
        Fail(LxrRcResult::OutOfMemory);
        return nullptr;
    }
    uint64_t payloadBytes = borrow ? 0 : count * sizeof(uintptr_t);
    uint64_t bytes = sizeof(Work) + payloadBytes;
    if (m_storage_diagnostics.load(std::memory_order_relaxed))
    {
        m_storage_metrics[0].fetch_add(1, std::memory_order_relaxed);
        uint64_t current = m_storage_metrics[1].fetch_add(bytes, std::memory_order_relaxed) + bytes;
        uint64_t high = m_storage_metrics[2].load(std::memory_order_relaxed);
        while (high < current && !m_storage_metrics[2].compare_exchange_weak(
            high, current, std::memory_order_relaxed))
        {
        }
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_STORAGE_SKIP_COPY_CONTROL)
        m_storage_metrics[3].fetch_add(payloadBytes, std::memory_order_relaxed);
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_STORAGE_SKIP_COPY_CONTROL
    }
    if (kind == WorkKind::TraceObjects || kind == WorkKind::TraceScan)
    {
        uint64_t pending = m_trace_pending.load(std::memory_order_relaxed);
        while (true)
        {
            if (pending == UINT64_MAX)
            {
                if (m_storage_diagnostics.load(std::memory_order_relaxed))
                {
                    m_storage_metrics[1].fetch_sub(bytes, std::memory_order_relaxed);
                }
                delete work;
                Fail(LxrRcResult::CapacityExceeded);
                return nullptr;
            }
            if (m_trace_pending.compare_exchange_weak(pending, pending + 1, std::memory_order_release))
            {
                break;
            }
        }
    }
    if (scan != nullptr)
    {
        scan->users.fetch_add(1, std::memory_order_relaxed);
    }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    if (kind == WorkKind::TraceObjects && count == 1 &&
        work->GetReference(0) == m_reader_target.load(std::memory_order_acquire) &&
        m_reader_held.load(std::memory_order_acquire))
    {
        Work* expected = nullptr;
        if (m_reader_work.compare_exchange_strong(expected, work, std::memory_order_acq_rel))
        {
            work->watchedReader = true;
            m_reader_initial.store(work->GetReference(0), std::memory_order_release);
            m_reader_id.store(work->packet.GetId(), std::memory_order_release);
        }
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    Work* head = m_work.load(std::memory_order_relaxed);
    do
    {
        work->next = head;
    } while (!m_work.compare_exchange_weak(
        head, work, std::memory_order_release, std::memory_order_relaxed));
    if (kind == WorkKind::TraceObjects || kind == WorkKind::TraceScan)
    {
        Work* pending = m_trace_todo.load(std::memory_order_relaxed);
        do
        {
            work->traceNext = pending;
        } while (!m_trace_todo.compare_exchange_weak(
            pending, work, std::memory_order_release, std::memory_order_relaxed));
    }
    return work;
}

void LxrReferenceCounting::ReleaseWork(Work* work)
{
    if (work->scan != nullptr)
    {
        size_t previous = work->scan->users.fetch_sub(1, std::memory_order_relaxed);
        if (previous == 0)
        {
            Fail(LxrRcResult::InvariantViolation);
        }
    }
    if (m_storage_diagnostics.load(std::memory_order_relaxed))
    {
        m_storage_metrics[1].fetch_sub(sizeof(Work) +
            (work->ownedReferences == nullptr ? 0 : work->packet.GetReferenceCount() * sizeof(uintptr_t)),
            std::memory_order_relaxed);
    }
    delete work;
}

void LxrReferenceCounting::DeleteWork()
{
    m_trace_todo.store(nullptr, std::memory_order_relaxed);
    Work* work = m_work.exchange(nullptr, std::memory_order_acquire);
    while (work != nullptr)
    {
        Work* next = work->next;
        ReleaseWork(work);
        work = next;
    }
    TraceScan* scan = m_scan_work.exchange(nullptr, std::memory_order_acquire);
    while (scan != nullptr)
    {
        TraceScan* next = scan->next;
        size_t index = (Start(scan->object) - GetReservationStart()) / sizeof(uintptr_t);
        TraceScanSlot(index) = nullptr;
        if (m_storage_diagnostics.load(std::memory_order_relaxed))
        {
            m_storage_metrics[5].fetch_sub(sizeof(TraceScan), std::memory_order_relaxed);
        }
        delete scan;
        scan = next;
    }
}

void LxrReferenceCounting::RetireCompletedWork()
{
    Work* kept = nullptr;
    Work* pending = nullptr;
    Work* work = m_work.load(std::memory_order_acquire);
    while (work != nullptr)
    {
        Work* next = work->next;
        LxrWorkPacketState state = work->packet.GetState();
        if (state == LxrWorkPacketState::Completed ||
#ifdef LXR_RETIRE_PENDING_CONTROL
            state == LxrWorkPacketState::CallerOwned ||
#endif // LXR_RETIRE_PENDING_CONTROL
            (state == LxrWorkPacketState::CallerOwned && work->traceProcessed.load(std::memory_order_acquire)))
        {
            ReleaseWork(work);
        }
        else
        {
            work->next = kept;
            kept = work;
            if ((work->kind == WorkKind::TraceObjects || work->kind == WorkKind::TraceScan) &&
                !work->traceProcessed.load(std::memory_order_acquire))
            {
                work->traceNext = pending;
                pending = work;
            }
        }
        work = next;
    }
    m_work.store(kept, std::memory_order_release);
    m_trace_todo.store(pending, std::memory_order_release);
    TraceScan* scans = nullptr;
    TraceScan* scan = m_scan_work.load(std::memory_order_acquire);
    while (scan != nullptr)
    {
        TraceScan* next = scan->next;
        if (scan->complete && scan->users.load(std::memory_order_acquire) == 0)
        {
            size_t index = (Start(scan->object) - GetReservationStart()) / sizeof(uintptr_t);
            if (TraceScanSlot(index) == scan)
            {
                TraceScanSlot(index) = nullptr;
            }
            if (m_storage_diagnostics.load(std::memory_order_relaxed))
            {
                m_storage_metrics[5].fetch_sub(sizeof(TraceScan), std::memory_order_relaxed);
            }
            delete scan;
        }
        else
        {
            scan->next = scans;
            scans = scan;
        }
        scan = next;
    }
    m_scan_work.store(scans, std::memory_order_release);
}

LxrRcResult LxrReferenceCounting::SubmitWork(Work* work, const LxrWorkPacket* parent)
{
    if (work == nullptr)
    {
        return GetFailure();
    }
    if (parent != nullptr)
    {
        m_metrics[ChildRegistrations].fetch_add(1, std::memory_order_relaxed);
        if (m_pause_kind == LxrPauseKind::Full || !m_configuration.lazyDecrements)
        {
            return FromScheduler(m_scheduler.Submit(&work->packet));
        }
        return FromScheduler(m_scheduler.SubmitTrackedChild(
            parent, &work->packet, LxrCompletionWorkKind::Decrement));
    }
    if (work->kind == WorkKind::Decrements || work->kind == WorkKind::PreviousRoots)
    {
        if (m_pause_kind == LxrPauseKind::Full || !m_configuration.lazyDecrements)
        {
            return FromScheduler(m_scheduler.Submit(&work->packet));
        }
        return FromScheduler(m_scheduler.SubmitDeferredConcurrentTracked(
            &work->packet, LxrCompletionWorkKind::Decrement, LxrDeferredWorkPriority::Prioritized));
    }
    return FromScheduler(m_scheduler.Submit(&work->packet));
}

LxrRcResult LxrReferenceCounting::FlushMutators()
{
    size_t active = 0;
    for (size_t index = 0; index < m_configuration.maximumMutators; index++)
    {
        LxrRcMutator& mutator = m_mutators[index];
        uint8_t state = mutator.state.load(std::memory_order_acquire);
        if (state == 2 || state == 3)
        {
            active++;
            LxrRcResult published = Publish(&mutator.decrements);
            if (published == LxrRcResult::Success)
            {
                published = Publish(&mutator.increments);
            }
            if (published != LxrRcResult::Success)
            {
                return published;
            }
            if (m_observer)
            {
                continue;
            }
            mutator.birthBlock = 0;
            mutator.pendingFastStart = 0;
            mutator.pendingFastSize = 0;
            if (mutator.publishedLimit != 0)
            {
                return Fail(LxrRcResult::InvariantViolation);
            }
            ImmixAllocationStatus status;
            SideMetadataResult result = mutator.allocator.Reset(&status);
            if (result != SideMetadataResult::Success || status != ImmixAllocationStatus::Ready)
            {
                return Fail(LxrRcResult::AllocationFailure);
            }
            if (state == 3)
            {
                mutator.state.store(0, std::memory_order_release);
            }
        }
    }
    if (m_storage_diagnostics.load(std::memory_order_relaxed))
    {
        m_storage_metrics[8].store(active, std::memory_order_release);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::PrepareWork()
{
    if (m_storage_diagnostics.load(std::memory_order_relaxed))
    {
        m_storage_metrics[7].store(m_root_counts[0] + m_root_counts[1], std::memory_order_release);
    }
    LogBank& bank = m_banks[m_producer_bank.load(std::memory_order_relaxed)];
    for (bool increment : {true, false})
    {
        LogStream& stream = increment ? bank.increments : bank.decrements;
        size_t limit = stream.reserved.load(std::memory_order_acquire);
        WorkKind kind = increment ? WorkKind::Slots : WorkKind::Decrements;
        LxrWorkBucketStage stage = increment ? LxrRCProcessIncs :
            (m_pause_kind == LxrPauseKind::Full || !m_configuration.lazyDecrements
                ? LxrRCProcessDecs : LxrWorkBucketStage::Unconstrained);
        for (size_t cursor = 0; cursor < limit;)
        {
            size_t count;
            LxrRcResult result = ReadPublishedRange(stream, cursor, limit, &count);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            result = SubmitWork(NewWork(kind, stream.references + cursor, count, stage, true, nullptr, true));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            if (!increment && m_trace_phase.load(std::memory_order_acquire) != LxrTracePhase::Inactive &&
                m_pause_kind != LxrPauseKind::InitialMark)
            {
                result = SubmitWork(NewWork(WorkKind::Satb, stream.references + cursor, count,
                    LxrWorkBucketStage::FinishConcurrentWork, true, nullptr, true));
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
            }
            cursor += count;
        }
    }
    for (size_t rootBank = 0; rootBank < 2; rootBank++)
    {
        bool current = rootBank == m_current_roots;
        for (size_t offset = 0; offset < m_root_counts[rootBank]; offset += BufferCapacity)
        {
            size_t count = m_root_counts[rootBank] - offset;
            if (count > BufferCapacity)
            {
                count = BufferCapacity;
            }
            LxrRcResult submitted = SubmitWork(NewWork(
                current ? WorkKind::Roots : WorkKind::PreviousRoots,
                m_roots[rootBank] + offset, count,
                current ? LxrRCProcessIncs :
                    (m_pause_kind == LxrPauseKind::Full || !m_configuration.lazyDecrements
                        ? LxrRCProcessDecs : LxrWorkBucketStage::Unconstrained)));
            if (submitted != LxrRcResult::Success)
            {
                return submitted;
            }
#ifdef LXR_RC_EARLY_ROOT_CONTROL
            if (current)
            {
                submitted = SubmitWork(NewWork(WorkKind::PreviousRoots,
                    m_roots[rootBank] + offset, count, LxrWorkBucketStage::Unconstrained));
                if (submitted != LxrRcResult::Success)
                {
                    return submitted;
                }
            }
#endif
        }
    }
    if (m_pause_kind != LxrPauseKind::RefCount || m_configuration.evacuateNursery ||
        m_configuration.evacuateMature)
    {
        LxrRcResult result = SubmitWork(NewWork(
            WorkKind::PrepareTrace, nullptr, 0, LxrWorkBucketStage::Prepare));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (m_pause_kind == LxrPauseKind::Full || m_pause_kind == LxrPauseKind::FinalMark)
        {
            result = SubmitWork(NewWork(WorkKind::WeakClosure, nullptr, 0, LxrWorkBucketStage::Closure));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
    }
    if (m_object_model.finishEpoch != nullptr || m_configuration.evacuateNursery ||
        m_configuration.evacuateMature)
    {
        LxrRcResult result = SubmitWork(NewWork(
            WorkKind::EpochClosure, nullptr, 0, LxrWorkBucketStage::Final));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    return SubmitWork(NewWork(
        WorkKind::NurserySweep, nullptr, 0, LxrWorkBucketStage::Release));
}

void LxrReferenceCounting::Suspend(void* context)
{
    LxrReferenceCounting* engine = static_cast<LxrReferenceCounting*>(context);
    engine->m_runtime.suspend(engine->m_runtime.context);
    int64_t prepareStart = engine->m_pause_timers.Enabled() ? engine->m_pause_timers.Stamp() : 0;
    engine->m_phase.store(LxrRcPhase::Suspended, std::memory_order_release);
    engine->m_copy_exhausted.store(false, std::memory_order_relaxed);
    engine->m_mature_copy_exhausted.store(false, std::memory_order_relaxed);
    engine->m_pause_copied_bytes.store(0, std::memory_order_relaxed);
    engine->m_pause_mature_copied_bytes.store(0, std::memory_order_relaxed);
    for (size_t worker = 0; worker <= engine->m_configuration.workers; worker++)
    {
        engine->m_worker_counters[worker].values[PromotedBytes].store(0, std::memory_order_relaxed);
    }
    engine->RetireCompletedWork();
    if (engine->FlushMutators() != LxrRcResult::Success)
    {
        return;
    }
    if (engine->m_grow_bytes != 0)
    {
        engine->m_growth_result = engine->GrowSuspended();
    }
    if (engine->m_pause_kind != LxrPauseKind::RefCount &&
        engine->GetTracePhase() == LxrTracePhase::Inactive && !engine->CommitTracePlanes())
    {
        engine->Fail(LxrRcResult::OutOfMemory);
        return;
    }
    if (engine->m_pause_kind == LxrPauseKind::InitialMark ||
        (engine->m_pause_kind == LxrPauseKind::Full &&
            engine->m_trace_phase.load(std::memory_order_acquire) == LxrTracePhase::Inactive))
    {
        if (engine->FromMetadata(engine->m_metadata.ResetAllDataRangesQuiescent(
                LxrSideMetadataKind::ObjectMark, 0)) != LxrRcResult::Success)
        {
            return;
        }
    }
    if (engine->m_pause_kind == LxrPauseKind::FinalMark || engine->m_pause_kind == LxrPauseKind::Full)
    {
        engine->m_marking.store(false, std::memory_order_release);
        engine->m_hold_trace.store(false, std::memory_order_release);
        engine->m_trace_phase.store(engine->m_pause_kind == LxrPauseKind::Full
            ? LxrTracePhase::Full : LxrTracePhase::FinalMark, std::memory_order_release);
        engine->m_cycle_sweep = true;
    }
    else if (engine->m_pause_kind == LxrPauseKind::InitialMark)
    {
        engine->m_trace_phase.store(LxrTracePhase::InitialMark, std::memory_order_release);
    }
    ImmixBlockOperationStatus status;
    if (engine->m_blocks.StartGcPause(&status) != SideMetadataResult::Success ||
        status != ImmixBlockOperationStatus::Updated)
    {
        engine->Fail(LxrRcResult::InvalidPhase);
        return;
    }
    LxrLargeObjectResult large = engine->m_large.GetPhase() == LxrLargeObjectPhase::ConcurrentMark
        ? engine->m_large.BeginRelease() : engine->m_large.BeginCollection(false,
            engine->m_pause_kind == LxrPauseKind::InitialMark || engine->m_pause_kind == LxrPauseKind::Full);
    if (large != LxrLargeObjectResult::Success)
    {
        engine->Fail(LxrRcResult::InvalidPhase);
        return;
    }
    if (engine->m_capture != nullptr)
    {
        if (prepareStart != 0)
        {
            engine->m_pause_timers.Add(
                LxrPauseTimers::SuspendPrepare, engine->m_pause_timers.Stamp() - prepareStart);
        }
        LxrPhaseScope capture(engine->m_pause_timers, LxrPauseTimers::RootCapture);
        LxrRcResult result = engine->m_capture(engine, engine->m_capture_context);
        if (result != LxrRcResult::Success)
        {
            engine->Fail(result);
            return;
        }
    }
    LxrPhaseScope prepareWork(engine->m_pause_timers, LxrPauseTimers::PrepareWork);
    if (engine->m_configuration.evacuateMature &&
        (engine->m_pause_kind == LxrPauseKind::InitialMark || engine->m_pause_kind == LxrPauseKind::Full))
    {
        if (engine->SelectMatureCandidates() != LxrRcResult::Success)
        {
            return;
        }
    }
    if (engine->m_configuration.evacuateMature &&
        (engine->m_pause_kind == LxrPauseKind::FinalMark || engine->m_pause_kind == LxrPauseKind::Full) &&
        engine->RetainPinnedMatureBlocks() != LxrRcResult::Success)
    {
        return;
    }
    engine->PrepareWork();
}

void LxrReferenceCounting::Resume(bool finishedGc, void* context)
{
    LxrReferenceCounting* engine = static_cast<LxrReferenceCounting*>(context);
    engine->m_phase.store(LxrRcPhase::Mutator, std::memory_order_release);
    if (engine->GetTracePhase() == LxrTracePhase::Concurrent &&
        engine->PublishTraceWork() != LxrRcResult::Success)
    {
        engine->Fail(LxrRcResult::SchedulerFailure);
    }
    engine->m_runtime.resume(finishedGc, engine->m_runtime.context);
}

LxrRcResult LxrReferenceCounting::Collect(
    LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds)
{
    return RunPause(LxrPauseKind::RefCount, capture, context, timeoutMilliseconds);
}

LxrRcResult LxrReferenceCounting::BeginTrace(
    LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds)
{
    if (GetTracePhase() != LxrTracePhase::Inactive || m_observer)
    {
        return LxrRcResult::InvalidPhase;
    }
    return RunPause(LxrPauseKind::InitialMark, capture, context, timeoutMilliseconds);
}

LxrRcResult LxrReferenceCounting::FinishTrace(
    LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds)
{
    if (GetTracePhase() != LxrTracePhase::Concurrent)
    {
        return LxrRcResult::InvalidPhase;
    }
    return RunPause(LxrPauseKind::FinalMark, capture, context, timeoutMilliseconds);
}

LxrRcResult LxrReferenceCounting::CollectFull(
    LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds)
{
    if (m_observer)
    {
        return LxrRcResult::InvalidPhase;
    }
    return RunPause(LxrPauseKind::Full, capture, context, timeoutMilliseconds);
}

LxrRcResult LxrReferenceCounting::RunPause(
    LxrPauseKind kind, LxrRcCaptureRoots capture, void* context, uint32_t timeoutMilliseconds)
{
    if (GetFailure() != LxrRcResult::Success)
    {
        return GetFailure();
    }
    if (GetPhase() != LxrRcPhase::Mutator)
    {
        return LxrRcResult::InvalidPhase;
    }
    if (m_scheduler.GetGeneration() >= UINT64_MAX - 1)
    {
        return Fail(LxrRcResult::CapacityExceeded);
    }
    LxrRcResult drained;
    {
        LxrPhaseScope preDrain(m_pause_timers, LxrPauseTimers::PreDrain);
        drained = GetTracePhase() == LxrTracePhase::Inactive
            ? Drain(timeoutMilliseconds) : DrainEpoch(timeoutMilliseconds);
    }
    if (drained != LxrRcResult::Success)
    {
        return drained;
    }
    if (GetTracePhase() == LxrTracePhase::Inactive)
    {
        DeleteWork();
    }
    else
    {
        m_trace_metrics[Interruptions].fetch_add(1, std::memory_order_relaxed);
        if (m_scan_checkpoint_active.load(std::memory_order_acquire))
        {
            m_trace_metrics[CheckpointInterruptions].fetch_add(1, std::memory_order_relaxed);
        }
        m_scan_checkpoint.store(0, std::memory_order_release);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_MATURE_READER_COMPLETION_CONTROL)
        // Force the previously permitted completion between checkpoint release and scheduler interruption.
        std::chrono::steady_clock::time_point deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(timeoutMilliseconds);
        for (TraceScan* scan = m_scan_work.load(std::memory_order_acquire); scan != nullptr; scan = scan->next)
        {
            while (!scan->complete.load(std::memory_order_acquire) &&
                std::chrono::steady_clock::now() < deadline)
            {
                GCToOSInterface::YieldThread(0);
            }
            if (!scan->complete.load(std::memory_order_acquire))
            {
                return Fail(LxrRcResult::InvariantViolation);
            }
        }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_MATURE_READER_COMPLETION_CONTROL
    }
    m_dead_large_count.store(0, std::memory_order_relaxed);
    m_capture = capture;
    m_capture_context = context;
    m_pause_kind = kind;
    if (m_coordinator.PreparePause(kind, !m_configuration.lazyDecrements,
            m_configuration.evacuateNursery || m_configuration.evacuateMature) !=
                LxrPauseCoordinatorResult::Success ||
        m_coordinator.StartPause(timeoutMilliseconds) != LxrPauseCoordinatorResult::Success)
    {
        return Fail(LxrRcResult::SchedulerFailure);
    }
    if (m_coordinator.CompletePause(timeoutMilliseconds) != LxrPauseCoordinatorResult::Success)
    {
        m_coordinator.AbortPause();
        return Fail(LxrRcResult::SchedulerFailure);
    }
    m_grow_bytes = 0;
    return GetFailure();
}

LxrRcResult LxrReferenceCounting::DrainEpoch(uint32_t timeoutMilliseconds)
{
    if (GetFailure() != LxrRcResult::Success)
    {
        return GetFailure();
    }
    if (m_scheduler.GetGeneration() != 0)
    {
        LxrRcResult result = FromScheduler(m_scheduler.WaitForCompletion(
            m_scheduler.GetGeneration(), timeoutMilliseconds));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    return FromScheduler(m_scheduler.WaitForBucketIdle(
        LxrWorkBucketStage::Unconstrained, timeoutMilliseconds));
}

LxrRcResult LxrReferenceCounting::Drain(uint32_t timeoutMilliseconds)
{
    LxrRcResult result = DrainEpoch(timeoutMilliseconds);
    if (result == LxrRcResult::Success)
    {
        result = FromScheduler(m_scheduler.WaitForIdle(timeoutMilliseconds));
    }
    if (result == LxrRcResult::Success && GetTracePhase() == LxrTracePhase::Inactive)
    {
        DeleteWork();
        if (!m_trace_scan_storage.DecommitAll() || !m_trace_lock_storage.DecommitAll())
        {
            return Fail(LxrRcResult::AllocationFailure);
        }
    }
    return result;
}

LxrRcResult LxrReferenceCounting::AddRoot(Object* object, bool interior, bool trace)
{
    if (GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    if (object == nullptr)
    {
        return LxrRcResult::Success;
    }
    if (interior)
    {
        LxrRcResult result = FindContainingObject(reinterpret_cast<uintptr_t>(object), &object);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    uintptr_t valid;
    SideMetadataResult result = m_metadata.Load(LxrSideMetadataKind::ValidObject,
        Start(object), SideMetadataMemoryOrder::Relaxed, &valid);
    if (result != SideMetadataResult::Success || valid != 1)
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t& count = m_root_counts[m_current_roots];
    if (count == m_configuration.maximumRoots)
    {
        return LxrRcResult::CapacityExceeded;
    }
    m_roots[m_current_roots][count++] = reinterpret_cast<uintptr_t>(object) | (trace ? 0 : 1);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GetObjectSize(Object* object, size_t* size, size_t worker)
{
    if (GetPhase() == LxrRcPhase::Sweeping)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    CountWorker(worker, ObjectSizeQueries, 1);
    size_t value = object->GetSize();
    if (value > SIZE_MAX - (sizeof(uintptr_t) - 1))
    {
        return Fail(LxrRcResult::InvalidArgument);
    }
    *size = (value + sizeof(uintptr_t) - 1) & ~(sizeof(uintptr_t) - 1);
    if (*size < MIN_OBJECT_SIZE)
    {
        return Fail(LxrRcResult::InvalidArgument);
    }
    return LxrRcResult::Success;
}

Object* LxrReferenceCounting::ResolveForwarded(Object* object) const
{
    if ((!m_configuration.evacuateNursery && !m_configuration.evacuateMature) ||
        object == nullptr || !IsImmix(Start(object)) ||
        GetObjectExtent(object) == 0)
    {
        return object;
    }
    ObjHeader* header = object->GetHeader();
    uint32_t state = header->GetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift);
    if (state == 2)
    {
        m_nursery_metrics[4].fetch_add(1, std::memory_order_relaxed);
        state = header->WaitWhileGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 2);
    }
    if (state == 3)
    {
        // Keep the MethodTable and synchronization word intact for EE diagnostics during relocation.
        Object** forwarding = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(object) + sizeof(uintptr_t));
        return Interlocked::CompareExchangePointer(forwarding, static_cast<Object*>(nullptr), nullptr);
    }
    _ASSERTE(state == 0);
    return object;
}

LxrRcResult LxrReferenceCounting::RelocateRoot(Object** slot, bool interior, bool pinned) const
{
    if (slot == nullptr || GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    Object* object = *slot;
    if (object == nullptr || !OwnsAddress(reinterpret_cast<uintptr_t>(object)))
    {
        return LxrRcResult::Success;
    }
    uintptr_t offset = 0;
    if (interior)
    {
        LxrRcResult result = FindContainingObject(reinterpret_cast<uintptr_t>(object), &object);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        offset = reinterpret_cast<uintptr_t>(*slot) - reinterpret_cast<uintptr_t>(object);
    }
    Object* destination = ResolveForwarded(object);
    if (pinned && destination != object)
    {
        return LxrRcResult::InvariantViolation;
    }
    *slot = reinterpret_cast<Object*>(reinterpret_cast<uintptr_t>(destination) + offset);
    return LxrRcResult::Success;
}

LxrNurseryMetrics LxrReferenceCounting::GetNurseryMetrics() const
{
    return {
        m_nursery_metrics[0].load(), m_nursery_metrics[1].load(), m_nursery_metrics[2].load(),
        m_nursery_metrics[3].load(), m_nursery_metrics[4].load(), m_nursery_metrics[5].load(),
        m_pause_copied_bytes.load(),
    };
}

LxrMatureMetrics LxrReferenceCounting::GetMatureMetrics() const
{
    return {
        m_mature_metrics[0].load(), m_mature_metrics[1].load(), m_mature_metrics[2].load(),
        m_mature_metrics[3].load(), m_mature_metrics[4].load(), m_mature_metrics[5].load(),
        m_mature_metrics[6].load(), m_mature_metrics[7].load(), m_mature_metrics[8].load(),
        m_mature_metrics[9].load(), m_mature_metrics[10].load(), m_mature_metrics[11].load(),
        m_mature_metrics[12].load(),
        m_pause_mature_copied_bytes.load(),
    };
}

uint64_t LxrReferenceCounting::GetCostMetric(size_t metric) const
{
    if (metric == 0)
    {
        return m_storage_diagnostics.load(std::memory_order_acquire);
    }
    if (metric > 9 || !m_storage_diagnostics.load(std::memory_order_acquire))
    {
        return UINT64_MAX;
    }
    return m_cost_ticks[metric - 1].load(std::memory_order_acquire);
}

LxrRcResult LxrReferenceCounting::GetDefragHeadroomBytes(size_t* bytes) const
{
    if (bytes == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    *bytes = 0;
    size_t blocks = m_allocation.GetBlocksInUse();
    *bytes = blocks * (ImmixBlockGeometry::BlockBytes / LxrLargeObjectSpace::PageBytes) *
        m_configuration.defragHeadroomPercent / 100 * LxrLargeObjectSpace::PageBytes;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GetUsedPages(size_t* pages) const
{
    if (pages == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t bytes = m_large.GetCommittedBytes() +
        m_allocation.GetBlocksInUse() * ImmixBlockGeometry::BlockBytes;
    *pages = bytes / LxrLargeObjectSpace::PageBytes;
    return LxrRcResult::Success;
}

bool LxrReferenceCounting::IsMatureCandidate(uintptr_t address)
{
    if (m_evacuation_sources == nullptr || !IsImmix(address) ||
        m_evacuation_sources[(address - GetReservationStart()) / ImmixBlockGeometry::BlockBytes].load(
            std::memory_order_acquire) == 0)
    {
        return false;
    }
    uintptr_t defrag = 0;
    if (m_metadata.Load(LxrSideMetadataKind::BlockDefrag, address,
            SideMetadataMemoryOrder::SequentiallyConsistent, &defrag) != SideMetadataResult::Success)
    {
        Fail(LxrRcResult::MetadataFailure);
        return false;
    }
    return defrag == UINT8_MAX;
}

LxrRcResult LxrReferenceCounting::SelectMatureCandidates()
{
    if (!m_configuration.evacuateMature)
    {
        return LxrRcResult::Success;
    }
    struct Candidate
    {
        uintptr_t block;
        size_t deadLines;
        size_t chunkScore;
    };
    size_t blocks = GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes;
    Candidate* candidates = new (std::nothrow) Candidate[blocks];
    if (candidates == nullptr)
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
    size_t count = 0;
    size_t freeBytes = 0;
    LxrRcResult result = LxrRcResult::Success;
    for (size_t index = 0; index < blocks; index++)
    {
        uintptr_t block = GetReservationStart() + index * ImmixBlockGeometry::BlockBytes;
        ImmixBlockState state;
        uint8_t unavailable;
        result = FromMetadata(m_blocks.GetState(block, &state, &unavailable));
        if (result != LxrRcResult::Success)
        {
            break;
        }
        if (state == ImmixBlockState::Unallocated)
        {
            bool nursery;
            result = FromMetadata(m_blocks.IsNursery(block, &nursery));
            if (result != LxrRcResult::Success)
            {
                break;
            }
            if (!nursery)
            {
                freeBytes += ImmixBlockGeometry::BlockBytes;
            }
            continue;
        }
        size_t deadLines = 0;
        for (size_t line = 0; line < ImmixBlockGeometry::LinesPerBlock; line++)
        {
            bool dead;
            result = FromMetadata(m_metadata.IsRangeZero(LxrSideMetadataKind::ReferenceCount,
                block + line * ImmixBlockGeometry::LineBytes, ImmixBlockGeometry::LineBytes,
                SideMetadataMemoryOrder::SequentiallyConsistent, &dead));
            if (result != LxrRcResult::Success)
            {
                break;
            }
            deadLines += dead ? 1 : 0;
        }
        if (result != LxrRcResult::Success)
        {
            break;
        }
        candidates[count++] = {block, deadLines, 0};
    }
    if (result == LxrRcResult::Success)
    {
        constexpr size_t ChunkBytes = size_t{1} << 22;
        constexpr size_t BlocksPerChunk = ChunkBytes / ImmixBlockGeometry::BlockBytes;
        size_t percent = m_configuration.chunkDefragPercent;
        if (m_pause_kind == LxrPauseKind::Full)
        {
            percent = percent > 50 ? 100 : percent * 2;
        }
        size_t threshold = BlocksPerChunk * percent / 100;
        threshold = threshold == 0 ? 1 : threshold;
        for (size_t index = 0; index < count; index++)
        {
            uintptr_t chunk = candidates[index].block & ~(static_cast<uintptr_t>(ChunkBytes) - 1);
            size_t occupied = 0;
            uintptr_t first = chunk > GetReservationStart() ? chunk : GetReservationStart();
            uintptr_t end = chunk + ChunkBytes;
            end = end < GetReservationStart() + GetImmixCapacityBytes()
                ? end : GetReservationStart() + GetImmixCapacityBytes();
            for (uintptr_t block = first; block < end; block += ImmixBlockGeometry::BlockBytes)
            {
                ImmixBlockState state;
                uint8_t unavailable;
                result = FromMetadata(m_blocks.GetState(block, &state, &unavailable));
                if (result != LxrRcResult::Success)
                {
                    delete[] candidates;
                    return result;
                }
                occupied += state == ImmixBlockState::Unallocated ? 0 : 1;
            }
            if (occupied < threshold)
            {
                candidates[index].chunkScore = BlocksPerChunk - occupied;
            }
        }
        size_t eligible = 0;
        for (size_t index = 0; index < count; index++)
        {
            if (candidates[index].chunkScore != 0 || m_pause_kind == LxrPauseKind::Full ||
                candidates[index].deadLines >= ImmixBlockGeometry::LinesPerBlock / 2)
            {
                candidates[eligible++] = candidates[index];
            }
        }
        count = eligible;
        m_mature_metrics[0].fetch_add(count, std::memory_order_relaxed);
        std::sort(candidates, candidates + count, [](const Candidate& left, const Candidate& right)
        {
            return left.chunkScore == right.chunkScore
                ? left.deadLines < right.deadLines : left.chunkScore < right.chunkScore;
        });
        size_t headroom;
        result = GetDefragHeadroomBytes(&headroom);
        if (result != LxrRcResult::Success)
        {
            delete[] candidates;
            return Fail(result);
        }
        size_t budget = m_pause_kind == LxrPauseKind::Full ? (freeBytes > headroom ? freeBytes : headroom) : headroom;
        // Unlike the reference's shared page resource, only admitted Immix pages are usable to-space here.
        budget = budget < freeBytes ? budget : freeBytes;
        size_t estimated = 0;
        while (count != 0 && budget != 0)
        {
            Candidate candidate = candidates[--count];
            size_t index = (candidate.block - GetReservationStart()) / ImmixBlockGeometry::BlockBytes;
            m_evacuation_sources[index].store(1, std::memory_order_release);
            result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::BlockDefrag, candidate.block,
                UINT8_MAX, SideMetadataMemoryOrder::SequentiallyConsistent));
            if (result != LxrRcResult::Success)
            {
                break;
            }
            m_mature_metrics[1].fetch_add(1, std::memory_order_relaxed);
            estimated += (ImmixBlockGeometry::LinesPerBlock - candidate.deadLines) *
                ImmixBlockGeometry::LineBytes / 2;
            if (estimated >= budget)
            {
                break;
            }
        }
        m_mature_metrics[3].fetch_add(estimated, std::memory_order_relaxed);
    }
    delete[] candidates;
    return result;
}

LxrRcResult LxrReferenceCounting::RetainPinnedMatureBlocks()
{
    if (!m_configuration.evacuateMature)
    {
        return LxrRcResult::Success;
    }
    size_t end = GetImmixCapacityBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(0); index < end; index = FindNextOccupiedSlot(index + 1))
    {
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        Object* object = Reference(start);
        if (IsMatureCandidate(start) && ResolveForwarded(object) == object &&
            ((object->GetHeader()->GetBits() & BIT_SBLK_GC_RESERVE) != 0 ||
             (m_object_model.isPinned != nullptr && m_object_model.isPinned(object, m_object_model.context))))
        {
            size_t block = index / (ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t));
            if ((m_evacuation_sources[block].fetch_or(RetainedSource, std::memory_order_acq_rel) &
                    RetainedSource) == 0)
            {
                m_mature_metrics[2].fetch_add(1, std::memory_order_relaxed);
            }
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::RememberMatureSlot(Object** slot, Object* target)
{
    uintptr_t address = reinterpret_cast<uintptr_t>(slot);
    if (!m_configuration.evacuateMature || !IsMatureCandidate(Start(target)) ||
        !(m_marking.load(std::memory_order_acquire) || m_pause_kind == LxrPauseKind::FinalMark))
    {
        return LxrRcResult::Success;
    }
    if (!OwnsAddress(address))
    {
        return Fail(LxrRcResult::UnsupportedLifetime);
    }
    uintptr_t reuse;
    LxrRcResult result = FromMetadata(m_metadata.Load(
        IsImmix(address) ? LxrSideMetadataKind::LineReuse : LxrSideMetadataKind::LargeObjectPageReuse,
        address, SideMetadataMemoryOrder::SequentiallyConsistent, &reuse));
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    for (;;)
    {
        RememberedChunk* head = m_remembered_chunks.load(std::memory_order_acquire);
        if (head != nullptr)
        {
            size_t index = head->reserved.load(std::memory_order_relaxed);
            while (index < BufferCapacity)
            {
                if (head->reserved.compare_exchange_weak(index, index + 1, std::memory_order_relaxed))
                {
                    head->entries[index] = {slot, static_cast<uint8_t>(reuse)};
                    head->published[index].store(1, std::memory_order_release);
                    m_mature_metrics[7].fetch_add(1, std::memory_order_relaxed);
                    return LxrRcResult::Success;
                }
            }
        }
        RememberedChunk* chunk = new (std::nothrow) RememberedChunk;
        if (chunk == nullptr)
        {
            return Fail(LxrRcResult::OutOfMemory);
        }
        chunk->next = head;
        if (!m_remembered_chunks.compare_exchange_strong(head, chunk, std::memory_order_acq_rel))
        {
            delete chunk;
        }
    }
}

void LxrReferenceCounting::DeleteRememberedChunks()
{
    RememberedChunk* chunk = m_remembered_chunks.exchange(nullptr, std::memory_order_acq_rel);
    while (chunk != nullptr)
    {
        RememberedChunk* next = chunk->next;
        delete chunk;
        chunk = next;
    }
}

LxrRcResult LxrReferenceCounting::ReplayMatureRememberedSlots()
{
    CostScope cost(this, 6);
    for (RememberedChunk* chunk = m_remembered_chunks.load(std::memory_order_acquire);
         chunk != nullptr; chunk = chunk->next)
    {
        for (size_t index = 0; index < chunk->reserved.load(std::memory_order_acquire); index++)
        {
            if (chunk->published[index].load(std::memory_order_acquire) != 1)
            {
                return Fail(LxrRcResult::InvariantViolation);
            }
            RememberedChunk::Entry entry = chunk->entries[index];
            uintptr_t address = reinterpret_cast<uintptr_t>(entry.slot);
            uintptr_t reuse;
            LxrRcResult result = FromMetadata(m_metadata.Load(
                IsImmix(address) ? LxrSideMetadataKind::LineReuse : LxrSideMetadataKind::LargeObjectPageReuse,
                address, SideMetadataMemoryOrder::SequentiallyConsistent, &reuse));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            if (reuse != entry.reuse)
            {
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_REMSET_STALE_CONTROL)
                m_mature_metrics[8].fetch_add(1, std::memory_order_relaxed);
                continue;
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_REMSET_STALE_CONTROL
            }
            if (IsImmix(address))
            {
                ImmixBlockState state;
                uint8_t unavailable;
                result = FromMetadata(m_blocks.GetState(address & ~ImmixBlockGeometry::BlockMask,
                    &state, &unavailable));
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
                if (state == ImmixBlockState::Unallocated)
                {
                    continue;
                }
            }
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_REMSET_SHAPE_CONTROL)
            Object* owner = nullptr;
            result = FindContainingObject(address, &owner);
            if (result == LxrRcResult::InvalidArgument)
            {
                m_mature_metrics[12].fetch_add(1, std::memory_order_relaxed);
                continue;
            }
            if (result != LxrRcResult::Success)
            {
                return Fail(result);
            }
            if (ResolveForwarded(owner) != owner)
            {
                continue;
            }
            uintptr_t ownerCount = 0;
            uintptr_t ownerMark = 0;
            result = FromMetadata(m_counts.Load(Start(owner), &ownerCount));
            if (result == LxrRcResult::Success)
            {
                result = FromMetadata(m_metadata.Load(LxrSideMetadataKind::ObjectMark, Start(owner),
                    SideMetadataMemoryOrder::SequentiallyConsistent, &ownerMark));
            }
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            if (ownerCount == 0 || ownerMark == 0)
            {
                m_mature_metrics[12].fetch_add(1, std::memory_order_relaxed);
                continue;
            }
            bool referenceField = false;
            // Byte tags may wrap. Inspect only a current metadata-live owner's shape, never stale payload.
            GCReferenceRanges::Enumerate(owner, [&](Object** first, size_t count)
            {
                uintptr_t start = reinterpret_cast<uintptr_t>(first);
                referenceField = address >= start && (address - start) / sizeof(Object*) < count &&
                    (address - start) % sizeof(Object*) == 0;
                return !referenceField;
            });
            if (!referenceField)
            {
                m_mature_metrics[12].fetch_add(1, std::memory_order_relaxed);
                continue;
            }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_REMSET_SHAPE_CONTROL
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
            if (m_watched_remembered_slot.load(std::memory_order_acquire) == address)
            {
                m_remembered_payload_reads.fetch_add(1, std::memory_order_relaxed);
            }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
            Object* target = *entry.slot;
            if (target == nullptr || !IsMatureCandidate(Start(target)))
            {
                continue;
            }
            uintptr_t count;
            result = LoadCount(target, &count);
            if (result != LxrRcResult::Success)
            {
                return Fail(result);
            }
            if (count == 0)
            {
                continue;
            }
            result = TryCopyMature(&target);
            if (result == LxrRcResult::Success)
            {
                *entry.slot = target;
                uintptr_t value = reinterpret_cast<uintptr_t>(target);
                result = QueueTrace(&value, 1);
                m_mature_metrics[9].fetch_add(1, std::memory_order_relaxed);
            }
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
    }
    LxrRcResult drained = DrainTraceInsidePause();
    if (drained == LxrRcResult::Success)
    {
        DeleteRememberedChunks();
    }
    return drained;
}

LxrRcResult LxrReferenceCounting::AllocateCopy(size_t size, size_t worker, uintptr_t* copyStart)
{
    _ASSERTE(worker < m_configuration.workers);
    *copyStart = 0;
    ImmixAllocationRequest request{size, sizeof(uintptr_t), 0};
    ImmixAllocationStatus status;
    ImmixBumpAllocator* allocator = &m_copy_allocators[2 * worker];
    SideMetadataResult allocated = allocator->TryAllocate(request, copyStart, &status);
    if (allocated == SideMetadataResult::Success && status != ImmixAllocationStatus::Allocated &&
        size > ImmixBlockGeometry::LineBytes)
    {
        allocator = &m_copy_allocators[2 * worker + 1];
        allocated = allocator->TryAllocate(request, copyStart, &status);
    }
    bool recycled = size <= ImmixBlockGeometry::LineBytes && m_configuration.copyLineRecycling;
    size_t attempts = GetImmixCapacityBytes() / ImmixBlockGeometry::LineBytes;
    while (allocated == SideMetadataResult::Success && status != ImmixAllocationStatus::Allocated &&
        attempts-- != 0)
    {
        allocated = allocator->TryRefill(
            recycled ? ImmixAllocationSource::GcCopyRecycled : ImmixAllocationSource::GcCopyFresh,
            GetTracePhase() == LxrTracePhase::Concurrent || GetTracePhase() == LxrTracePhase::FinalMark
                ? ImmixMarkingContext::ConcurrentOrFinalMark : ImmixMarkingContext::Inactive, &status);
        if (allocated == SideMetadataResult::Success && status == ImmixAllocationStatus::Exhausted && recycled)
        {
            recycled = false;
            continue;
        }
        if (allocated != SideMetadataResult::Success || status != ImmixAllocationStatus::Ready)
        {
            break;
        }
        allocated = allocator->TryAllocate(request, copyStart, &status);
    }
    if (allocated != SideMetadataResult::Success)
    {
        return FromMetadata(allocated);
    }
    return status == ImmixAllocationStatus::Allocated
        ? LxrRcResult::Success : LxrRcResult::AllocationFailure;
}

LxrRcResult LxrReferenceCounting::TryCopyMature(Object** reference)
{
    Object* source = *reference;
    *reference = ResolveForwarded(source);
    if (*reference != source || source == nullptr || !IsMatureCandidate(Start(source)) ||
        (GetTracePhase() != LxrTracePhase::Full && GetTracePhase() != LxrTracePhase::FinalMark))
    {
        return LxrRcResult::Success;
    }
    size_t size = GetObjectExtent(source);
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_MATURE_DEAD_SCAN_CONTROL)
    if (size == 0)
    {
        return LxrRcResult::Success;
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_MATURE_DEAD_SCAN_CONTROL
    size_t block = (Start(source) - GetReservationStart()) / ImmixBlockGeometry::BlockBytes;
    if ((m_evacuation_sources[block].load(std::memory_order_acquire) & RetainedSource) != 0 ||
        m_mature_copy_exhausted.load(std::memory_order_relaxed))
    {
        return LxrRcResult::Success;
    }
    uintptr_t count;
    LxrRcResult result = LoadCount(source, &count);
    if (result != LxrRcResult::Success || count == 0)
    {
        return result;
    }
    size_t index = (Start(source) - GetReservationStart()) / sizeof(uintptr_t);
    ObjHeader* header = source->GetHeader();
    uint32_t observed = header->CompareExchangeGCReservedBits(
        m_configuration.forwardingMask, m_configuration.forwardingShift, 2, 0);
    if (observed != 0)
    {
        *reference = ResolveForwarded(source);
        return LxrRcResult::Success;
    }
    uintptr_t copyStart;
    {
        CostScope cost(this, 3);
        result = AllocateCopy(size, 0, &copyStart);
    }
    if (result == LxrRcResult::AllocationFailure)
    {
        m_mature_copy_exhausted.store(true, std::memory_order_relaxed);
        m_evacuation_sources[block].fetch_or(RetainedSource, std::memory_order_acq_rel);
        m_mature_metrics[6].fetch_add(1, std::memory_order_relaxed);
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return LxrRcResult::Success;
    }
    if (result == LxrRcResult::Success)
    {
        CostScope cost(this, 4);
        result = FromMetadata(m_counts.CopyObjectQuiescent(Start(source), copyStart, size));
    }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_MATURE_COUNT_CONTROL)
    if (result == LxrRcResult::Success)
    {
        result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::ReferenceCount,
            copyStart, 1, SideMetadataMemoryOrder::SequentiallyConsistent));
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_MATURE_COUNT_CONTROL
    if (result != LxrRcResult::Success)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    Object* destination = Reference(copyStart);
    {
        CostScope cost(this, 5);
        memcpy(reinterpret_cast<void*>(copyStart), reinterpret_cast<void*>(Start(source)), size);
    }
    destination->GetHeader()->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
    size_t destinationIndex = (copyStart - GetReservationStart()) / sizeof(uintptr_t);
    ExtentSlot(destinationIndex).store(size, std::memory_order_release);
    m_live_objects[destinationIndex / 64].fetch_or(
        UINT64_C(1) << (destinationIndex % 64), std::memory_order_release);
    m_live_bytes.fetch_add(size, std::memory_order_relaxed);
    TraceScan* scan = TraceScanSlot(index);
    if (scan != nullptr)
    {
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_MATURE_SCAN_REBASE_CONTROL)
        scan->iterator.Relocate(destination);
        if (scan->range.start != nullptr)
        {
            uintptr_t offset = reinterpret_cast<uintptr_t>(scan->range.start) -
                reinterpret_cast<uintptr_t>(source);
            scan->range.start = reinterpret_cast<Object**>(reinterpret_cast<uintptr_t>(destination) + offset);
        }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_MATURE_SCAN_REBASE_CONTROL
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        m_mature_scan_receipt[0].store(reinterpret_cast<uintptr_t>(source), std::memory_order_release);
        m_mature_scan_receipt[1].store(reinterpret_cast<uintptr_t>(destination), std::memory_order_release);
        m_mature_scan_receipt[2].store(reinterpret_cast<uintptr_t>(scan->range.start), std::memory_order_release);
        m_mature_scan_receipt[3].store(scan->users.load(std::memory_order_acquire), std::memory_order_release);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        scan->object = destination;
        TraceScanSlot(destinationIndex) = scan;
        TraceScanSlot(index) = nullptr;
    }
    result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::ObjectMark, copyStart,
        0, SideMetadataMemoryOrder::SequentiallyConsistent));
    if (result != LxrRcResult::Success)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    if (m_object_model.didCopy != nullptr)
    {
        m_object_model.didCopy(source, destination, m_object_model.context);
    }
    uintptr_t watched = Start(source);
    m_probe_start.compare_exchange_strong(watched, copyStart, std::memory_order_acq_rel);
    Object** forwarding = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(source) + sizeof(uintptr_t));
    Interlocked::ExchangePointer(forwarding, destination);
    header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 3);
    m_mature_metrics[4].fetch_add(1, std::memory_order_relaxed);
    m_mature_metrics[5].fetch_add(size, std::memory_order_relaxed);
    m_evacuation_sources[block].fetch_or(CopiedSource, std::memory_order_release);
    m_pause_mature_copied_bytes.fetch_add(size, std::memory_order_relaxed);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_MATURE_PREDICTOR_CONTROL)
    m_pause_copied_bytes.fetch_add(size, std::memory_order_relaxed);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_MATURE_PREDICTOR_CONTROL
    *reference = destination;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::TryCopyNursery(
    Object* source, Object** destination, size_t worker, bool* copied,
    bool* incrementedInPlace, uintptr_t* previous)
{
    *destination = source;
    *copied = false;
    *incrementedInPlace = false;
    uintptr_t start = Start(source);
    uintptr_t count;
    LxrRcResult result = FromMetadata(m_counts.Load(start, &count));
    if (result != LxrRcResult::Success || count != 0)
    {
        *destination = ResolveForwarded(source);
        return result;
    }
    ObjHeader* header = source->GetHeader();
    uint32_t observed = header->CompareExchangeGCReservedBits(
        m_configuration.forwardingMask, m_configuration.forwardingShift, 2, 0);
    if (observed != 0)
    {
        *destination = ResolveForwarded(source);
        return LxrRcResult::Success;
    }
    // A different worker may have retained this source block in place before the claim.
    result = FromMetadata(m_counts.Load(start, &count));
    if (result != LxrRcResult::Success || count != 0)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    bool nursery = false;
    result = FromMetadata(m_blocks.IsNursery(start & ~ImmixBlockGeometry::BlockMask, &nursery));
    if (result != LxrRcResult::Success)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    if (!nursery || GetObjectExtent(source) >= m_configuration.maximumYoungCopyBytes ||
        m_copy_exhausted.load(std::memory_order_relaxed) ||
        (source->GetHeader()->GetBits() & BIT_SBLK_GC_RESERVE) != 0 ||
        (m_object_model.isPinned != nullptr && m_object_model.isPinned(source, m_object_model.context)))
    {
        SideMetadataUpdateStatus updated;
        result = FromMetadata(m_counts.Increment(start, previous, &updated));
        *incrementedInPlace = result == LxrRcResult::Success;
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    size_t size = GetObjectExtent(source);
    uintptr_t copyStart = 0;
    {
        CostScope cost(this, 0);
        result = AllocateCopy(size, worker, &copyStart);
    }
    if (result != LxrRcResult::Success && result != LxrRcResult::AllocationFailure)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    if (result == LxrRcResult::AllocationFailure)
    {
        // The oracle's to-space overflow is an in-place promotion, not an abandoned claim.
        m_copy_exhausted.store(true, std::memory_order_relaxed);
        m_nursery_metrics[3].fetch_add(1, std::memory_order_relaxed);
        SideMetadataUpdateStatus updated;
        result = FromMetadata(m_counts.Increment(start, previous, &updated));
        *incrementedInPlace = result == LxrRcResult::Success;
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    Object* copy = Reference(copyStart);
    {
        CostScope cost(this, 1);
        result = FromMetadata(m_counts.InitializeObjectQuiescent(copyStart, size));
        SideMetadataUpdateStatus updated;
        if (result == LxrRcResult::Success)
        {
            result = FromMetadata(m_counts.Increment(copyStart, previous, &updated));
        }
        if (result == LxrRcResult::Success)
        {
            result = FromMetadata(m_counts.MarkStraddleLinesQuiescent(copyStart, size));
        }
        if (result != LxrRcResult::Success)
        {
            header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
            return result;
        }
    }
    {
        CostScope cost(this, 2);
        memcpy(reinterpret_cast<void*>(copyStart), reinterpret_cast<void*>(start), size);
    }
    copy->GetHeader()->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
    size_t index = (copyStart - GetReservationStart()) / sizeof(uintptr_t);
    ExtentSlot(index).store(size, std::memory_order_release);
    m_live_objects[index / 64].fetch_or(UINT64_C(1) << (index % 64), std::memory_order_release);
    m_live_bytes.fetch_add(size, std::memory_order_relaxed);
    if (m_object_model.didCopy != nullptr)
    {
        m_object_model.didCopy(source, copy, m_object_model.context);
    }
    uintptr_t watched = start;
    m_probe_start.compare_exchange_strong(watched, copyStart, std::memory_order_acq_rel);
    Object** forwarding = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(source) + sizeof(uintptr_t));
    Interlocked::ExchangePointer(forwarding, copy);
    // Destination RC/VO/extent/flags and the full copied header precede publication.
    header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 3);
    m_nursery_metrics[0].fetch_add(1, std::memory_order_relaxed);
    m_nursery_metrics[1].fetch_add(size, std::memory_order_relaxed);
    m_pause_copied_bytes.fetch_add(size, std::memory_order_relaxed);
    *destination = copy;
    *copied = true;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ProcessIncrement(
    Object** reference, bool trace, ReferenceBatch* children, size_t worker, TraceBatch* targets)
{
    Object* object = *reference;
    if (object == nullptr)
    {
        return LxrRcResult::Success;
    }
    uintptr_t start = Start(object);
    if (!IsImmix(start) && !IsLarge(start))
    {
        if (m_object_model.isImmortal != nullptr &&
            m_object_model.isImmortal(object, m_object_model.context))
        {
            return LxrRcResult::Success;
        }
        return Fail(LxrRcResult::UnsupportedLifetime);
    }
    Object* source = object;
    object = ResolveForwarded(object);
    *reference = object;
    start = Start(object);
    MethodTable* type = object->GetGCSafeMethodTable();
    if ((type->Collectible() && m_object_model.getLoaderAllocatorSlot == nullptr) ||
        (type->HasFinalizer() && !m_object_model.admitFinalizable))
    {
        return Fail(LxrRcResult::UnsupportedLifetime);
    }
    bool copied = false;
    bool incrementedInPlace = false;
    uintptr_t previous = 0;
    if (m_configuration.evacuateNursery && IsImmix(start) && object == source)
    {
        LxrRcResult copying = TryCopyNursery(object, &object, worker, &copied, &incrementedInPlace, &previous);
        if (copying != LxrRcResult::Success)
        {
            return copying;
        }
        *reference = object;
        start = Start(object);
    }
    SideMetadataUpdateStatus status;
    LxrRcResult result = LxrRcResult::Success;
    if (!copied && !incrementedInPlace)
    {
        result = FromMetadata(m_counts.Increment(start, &previous, &status));
    }
    if (result == LxrRcResult::Success && trace && GetTracePhase() != LxrTracePhase::Inactive)
    {
        uintptr_t target = reinterpret_cast<uintptr_t>(object);
        result = targets == nullptr ? QueueTrace(&target, 1) : targets->Append(target);
    }
    if (result != LxrRcResult::Success || previous != 0)
    {
        return result;
    }
    size_t size;
    result = GetObjectSize(object, &size, worker);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    if (IsImmix(start))
    {
        bool nursery;
        result = FromMetadata(m_blocks.IsNursery(start & ~ImmixBlockGeometry::BlockMask, &nursery));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (copied || nursery)
        {
            // Only objects in clean nursery blocks can be evacuated, so only they size the to-space.
            CountWorker(worker, PromotedBytes, size);
        }
        if (nursery)
        {
            ImmixBlockOperationStatus promoted;
            do
            {
                result = FromMetadata(m_blocks.TryPromoteInPlace(
                    start & ~ImmixBlockGeometry::BlockMask, &promoted));
                if (promoted == ImmixBlockOperationStatus::Contended)
                {
                    YieldProcessor();
                }
            } while (result == LxrRcResult::Success && promoted == ImmixBlockOperationStatus::Contended);
            if (result != LxrRcResult::Success ||
                (promoted != ImmixBlockOperationStatus::Updated &&
                 promoted != ImmixBlockOperationStatus::Unchanged))
            {
                return Fail(LxrRcResult::MetadataFailure);
            }
        }
        if (!copied)
        {
            result = FromMetadata(m_counts.MarkStraddleLinesQuiescent(start, size));
            if (result == LxrRcResult::Success)
            {
                result = FromMetadata(m_metadata.ResetRangeQuiescent(
                    LxrSideMetadataKind::FieldUnlogged, start, size, 1));
            }
        }
    }
    else
    {
        LxrLargeObjectAllocation allocation;
        bool promoted;
        if (m_large.GetAllocation(start, &allocation) != LxrLargeObjectResult::Success ||
            m_large.TryMarkAndPromote(start, allocation.owner, &promoted) !=
                LxrLargeObjectResult::Success)
        {
            return Fail(LxrRcResult::MetadataFailure);
        }
    }
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    CountWorker(worker, Promotions, 1);
    if (!copied && m_configuration.evacuateNursery &&
        ((object->GetHeader()->GetBits() & BIT_SBLK_GC_RESERVE) != 0 ||
         (m_object_model.isPinned != nullptr && m_object_model.isPinned(object, m_object_model.context))))
    {
        m_nursery_metrics[2].fetch_add(size, std::memory_order_relaxed);
    }
    return ScanIncrements(object, trace || IsMarked(object), children, worker);
}

LxrRcResult LxrReferenceCounting::ScanIncrements(
    Object* object, bool trace, ReferenceBatch* children, size_t worker)
{
    LxrRcResult result = LxrRcResult::Success;
    CountWorker(worker, ObjectScans, 1);
    GCReferenceRanges::Enumerate(object, [&](Object** first, size_t length)
    {
        for (size_t index = 0; index < length; index++)
        {
            Object* child = first[index];
            if (child == nullptr)
            {
                continue;
            }
            result = RememberMatureSlot(first + index, child);
            if (result != LxrRcResult::Success)
            {
                return false;
            }
            result = children->Append(reinterpret_cast<uintptr_t>(first + index), trace);
            if (result != LxrRcResult::Success)
            {
                return false;
            }
        }
        return true;
    });
    Object* loaderAllocator = GetLoaderAllocator(object);
    if (result == LxrRcResult::Success && loaderAllocator != nullptr)
    {
        uintptr_t target = reinterpret_cast<uintptr_t>(loaderAllocator);
        result = SubmitWork(NewWork(WorkKind::Targets, &target, 1, LxrRCProcessIncs, trace));
    }
    return result;
}

LxrRcResult LxrReferenceCounting::ScanDecrements(Object* object, ReferenceBatch* children, size_t worker)
{
    LxrRcResult result = LxrRcResult::Success;
    CountWorker(worker, ObjectScans, 1);
    GCReferenceRanges::Enumerate(object, [&](Object** first, size_t length)
    {
        for (size_t index = 0; index < length; index++)
        {
            Object* child = first[index];
            if (child == nullptr)
            {
                continue;
            }
            result = RememberMatureSlot(first + index, child);
            if (result != LxrRcResult::Success)
            {
                return false;
            }
            result = children->Append(reinterpret_cast<uintptr_t>(child), true);
            if (result != LxrRcResult::Success)
            {
                return false;
            }
        }
        return true;
    });
    Object* loaderAllocator = GetLoaderAllocator(object);
    if (result == LxrRcResult::Success && loaderAllocator != nullptr)
    {
        result = children->Append(reinterpret_cast<uintptr_t>(loaderAllocator), true);
    }
    return result;
}

Object* LxrReferenceCounting::GetLoaderAllocator(Object* object) const
{
    MethodTable* type = object->GetGCSafeMethodTable();
    if (!type->Collectible())
    {
        return nullptr;
    }
    Object** slot = m_object_model.getLoaderAllocatorSlot(type);
    return slot == nullptr ? nullptr :
        Interlocked::CompareExchangePointer(slot, static_cast<Object*>(nullptr), nullptr);
}

SideMetadataResult LxrReferenceCounting::Death(uintptr_t start, void* context)
{
    DeathContext* death = static_cast<DeathContext*>(context);
    LxrReferenceCounting* engine = death->engine;
    Object* object = Reference(start);
    if (engine->m_marking.load(std::memory_order_acquire) &&
        engine->FinishDyingTraceObject(object) != LxrRcResult::Success)
    {
        return SideMetadataResult::InconsistentState;
    }
    if (engine->ScanDecrements(object, death->children, death->worker) != LxrRcResult::Success)
    {
        return SideMetadataResult::InconsistentState;
    }
    if (engine->IsImmix(start))
    {
        size_t size;
        if (engine->GetObjectSize(object, &size, death->worker) != LxrRcResult::Success)
        {
            return SideMetadataResult::InconsistentState;
        }
        SideMetadataResult result = engine->m_counts.ClearStraddleLinesQuiescent(start, size);
        if (result == SideMetadataResult::Success)
        {
            if (engine->Reclaim(start) != LxrRcResult::Success)
            {
                return SideMetadataResult::InconsistentState;
            }
            size_t block = (start - reinterpret_cast<uintptr_t>(engine->m_arena)) /
                ImmixBlockGeometry::BlockBytes;
            engine->m_mature_candidates[block].store(1, std::memory_order_release);
        }
        return result;
    }
    size_t index = engine->m_dead_large_count.fetch_add(1, std::memory_order_relaxed);
    if (index >= engine->m_configuration.largeObjectBytes / LxrLargeObjectSpace::PageBytes)
    {
        engine->Fail(LxrRcResult::CapacityExceeded);
        return SideMetadataResult::InconsistentState;
    }
    engine->m_dead_large[index] = start;
    if (engine->Reclaim(start) != LxrRcResult::Success)
    {
        return SideMetadataResult::InconsistentState;
    }
    return SideMetadataResult::Success;
}

LxrRcResult LxrReferenceCounting::ProcessDecrements(Work* work, LxrWorkPacket* packet, size_t worker)
{
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    if (work->ownedReferences == nullptr && m_log_reader_hold.load(std::memory_order_acquire))
    {
        m_log_reader_active.store(true, std::memory_order_release);
        while (m_log_reader_hold.load(std::memory_order_acquire))
        {
            GCToOSInterface::YieldThread(0);
        }
        m_log_reader_active.store(false, std::memory_order_release);
    }
#ifdef LXR_LOG_DROP_CONTROL
    if (work->ownedReferences == nullptr)
    {
        return LxrRcResult::Success;
    }
#endif // LXR_LOG_DROP_CONTROL
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    ReferenceBatch children{this, WorkKind::Decrements, packet, {}};
    DeathContext death{this, &children, worker};
    for (size_t index = 0; index < packet->GetReferenceCount(); index++)
    {
        uintptr_t reference = work->GetReference(index);
        if (work->kind == WorkKind::PreviousRoots)
        {
            reference &= ~static_cast<uintptr_t>(1);
        }
        Object* object = reinterpret_cast<Object*>(reference);
        if (object == nullptr)
        {
            continue;
        }
        if (!OwnsAddress(Start(object)))
        {
            if (m_object_model.isImmortal != nullptr &&
                m_object_model.isImmortal(object, m_object_model.context))
            {
                continue;
            }
            return Fail(LxrRcResult::UnsupportedLifetime);
        }
        if (m_cycle_sweep && !IsMarked(object))
        {
            continue;
        }
        uintptr_t start = Start(object);
        uintptr_t count;
        LxrRcResult result = FromMetadata(m_metadata.Load(
            LxrSideMetadataKind::ReferenceCount, start, SideMetadataMemoryOrder::Relaxed, &count));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        CountWorker(worker, DecrementEntries, 1);
        if (work->kind == WorkKind::PreviousRoots)
        {
            CountWorker(worker, RootDecrements, 1);
        }
        if (count == 0 || count == m_counts.GetMaximumCount())
        {
            continue;
        }
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        result = FromMetadata(m_counts.DecrementWithDeath(
            start, Death, &death, &previous, &status));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (status == SideMetadataUpdateStatus::Updated && previous == 1)
        {
            CountWorker(worker, Deaths, 1);
        }
    }
    return children.Flush();
}

LxrRcResult LxrReferenceCounting::Sweep(const LxrBlockReleaseGuard* guard)
{
    LxrRcPhase phase = GetPhase();
    if (guard != nullptr && m_cycle_sweep)
    {
        LxrRcResult result = SweepCycles();
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    if (guard == nullptr)
    {
        m_phase.store(LxrRcPhase::Sweeping, std::memory_order_release);
#ifdef LXR_RC_SWEEP_QUERY_CONTROL
        size_t forbidden;
        return GetObjectSize(nullptr, &forbidden);
#endif
    }
    uintptr_t arena = reinterpret_cast<uintptr_t>(m_arena);
    for (size_t offset = 0; offset < m_immix_capacity.load(std::memory_order_acquire);
         offset += ImmixBlockGeometry::BlockBytes)
    {
        uintptr_t block = arena + offset;
        size_t candidate = offset / ImmixBlockGeometry::BlockBytes;
        if (guard != nullptr && m_mature_candidates[candidate].load(std::memory_order_acquire) == 0)
        {
            continue;
        }
        ImmixBlockOperationStatus status;
        bool selected = IsMatureCandidate(block);
        bool copied = selected && (m_evacuation_sources[candidate].load(std::memory_order_acquire) &
            CopiedSource) != 0;
        LxrRcResult result = FromMetadata(m_blocks.TryReleaseIfRcDead(block, guard, &status));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (status == ImmixBlockOperationStatus::Updated)
        {
            if (FromMetadata(m_allocation.NotifyBlockReleased()) != LxrRcResult::Success)
            {
                return GetFailure();
            }
            m_metrics[BlocksReleased].fetch_add(1, std::memory_order_relaxed);
            if (copied)
            {
                m_mature_metrics[11].fetch_add(1, std::memory_order_relaxed);
            }
            if (selected)
            {
                m_evacuation_sources[candidate].store(0, std::memory_order_release);
            }
            m_mature_candidates[candidate].store(0, std::memory_order_release);
        }
        else if (guard == nullptr)
        {
            size_t freeLines = 0;
            size_t next = 0;
            while (next < ImmixBlockGeometry::LinesPerBlock)
            {
                ImmixHole hole;
                ImmixAllocationStatus found;
                result = FromMetadata(m_allocation.TryFindNextHole(block, next, &hole, &next, &found));
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
                if (found == ImmixAllocationStatus::Ready)
                {
                    freeLines += hole.line_count;
                }
            }
            if (freeLines != 0 && freeLines < ImmixBlockGeometry::LinesPerBlock)
            {
                result = FromMetadata(m_blocks.SetReusable(
                    block, static_cast<uint8_t>(ImmixBlockGeometry::LinesPerBlock - freeLines), &status));
                if (result != LxrRcResult::Success || status == ImmixBlockOperationStatus::InvalidTransition)
                {
                    return Fail(LxrRcResult::MetadataFailure);
                }
            }
        }
    }
    if (guard == nullptr)
    {
        size_t released;
        if (m_large.SweepNursery(&released) != LxrLargeObjectResult::Success)
        {
            return Fail(LxrRcResult::MetadataFailure);
        }
        m_metrics[LargeObjectBytesReleased].fetch_add(released, std::memory_order_relaxed);
        m_phase.store(phase, std::memory_order_release);
    }
    else
    {
        size_t count = m_dead_large_count.load(std::memory_order_acquire);
        for (size_t index = 0; index < count; index++)
        {
            LxrLargeObjectAllocation allocation;
            if (m_large.GetAllocation(m_dead_large[index], &allocation) != LxrLargeObjectResult::Success ||
                m_large.ReleaseAfterDecrements(allocation.object, allocation.owner, *guard) !=
                    LxrLargeObjectResult::Success)
            {
                return Fail(LxrRcResult::MetadataFailure);
            }
            m_metrics[LargeObjectBytesReleased].fetch_add(
                allocation.page_count * LxrLargeObjectSpace::PageBytes, std::memory_order_relaxed);
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::FixForwardingReaders()
{
    if (!m_configuration.evacuateNursery && !m_configuration.evacuateMature)
    {
        return LxrRcResult::Success;
    }
    for (size_t bank = 0; bank < 2; bank++)
    {
        for (size_t index = 0; index < m_root_counts[bank]; index++)
        {
            uintptr_t flags = m_roots[bank][index] & 1;
            Object* object = reinterpret_cast<Object*>(m_roots[bank][index] & ~static_cast<uintptr_t>(1));
            m_roots[bank][index] = reinterpret_cast<uintptr_t>(ResolveForwarded(object)) | flags;
        }
        LogStream& decrements = m_banks[bank].decrements;
        size_t limit = decrements.reserved.load(std::memory_order_acquire);
        for (size_t cursor = 0; cursor < limit;)
        {
            size_t count;
            LxrRcResult result = ReadPublishedRange(decrements, cursor, limit, &count);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            for (size_t index = cursor; index < cursor + count; index++)
            {
                uintptr_t value = decrements.references[index];
                uintptr_t publication = decrements.published == nullptr && index == cursor
                    ? value & ~LogReferenceMask : 0;
                decrements.references[index] = reinterpret_cast<uintptr_t>(
                    ResolveForwarded(reinterpret_cast<Object*>(value & LogReferenceMask))) | publication;
            }
            cursor += count;
        }
    }
    for (Work* work = m_work.load(std::memory_order_acquire); work != nullptr; work = work->next)
    {
        if (work->kind == WorkKind::Slots || work->kind == WorkKind::NurserySlots ||
            work->ownedReferences == nullptr)
        {
            continue;
        }
        for (size_t index = 0; index < work->packet.GetReferenceCount(); index++)
        {
            uintptr_t flags = work->kind == WorkKind::Roots || work->kind == WorkKind::PreviousRoots
                ? work->ownedReferences[index] & 1 : 0;
            Object* object = reinterpret_cast<Object*>(work->ownedReferences[index] & ~flags);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_PENDING_READER_REBASE_CONTROL)
            if (work->watchedReader)
            {
                continue;
            }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_PENDING_READER_REBASE_CONTROL
            work->ownedReferences[index] = reinterpret_cast<uintptr_t>(ResolveForwarded(object)) | flags;
        }
    }
    for (TraceScan* scan = m_scan_work.load(std::memory_order_acquire); scan != nullptr; scan = scan->next)
    {
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_MATURE_DEAD_SCAN_CONTROL)
        if (scan->complete.load(std::memory_order_acquire))
        {
            continue;
        }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_MATURE_DEAD_SCAN_CONTROL
        if (ResolveForwarded(scan->object) != scan->object)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
        scan->loaderAllocator = ResolveForwarded(scan->loaderAllocator);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::RetireForwardedSources()
{
    if (!m_configuration.evacuateNursery && !m_configuration.evacuateMature)
    {
        return LxrRcResult::Success;
    }
    for (size_t worker = 0; worker < 2 * m_configuration.workers; worker++)
    {
        ImmixAllocationStatus status;
        LxrRcResult result = FromMetadata(m_copy_allocators[worker].Reset(&status));
        if (result != LxrRcResult::Success || status != ImmixAllocationStatus::Ready)
        {
            return Fail(LxrRcResult::AllocationFailure);
        }
    }
    size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
    for (size_t block = 0; block < GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes; block++)
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_MATURE_EARLY_RELEASE_CONTROL)
        if (m_evacuation_sources != nullptr &&
            (m_evacuation_sources[block].load() & RetainedSource) != 0)
        {
            if (FromMetadata(m_counts.ResetBlockQuiescent(
                    GetReservationStart() + block * ImmixBlockGeometry::BlockBytes)) != LxrRcResult::Success)
            {
                return GetFailure();
            }
        }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_MATURE_EARLY_RELEASE_CONTROL
        if (m_allocated_block_storage.Get()[block].load(std::memory_order_acquire) == 0 &&
            !IsMatureCandidate(GetReservationStart() + block * ImmixBlockGeometry::BlockBytes))
        {
            continue;
        }
        for (size_t index = FindNextOccupiedSlot(block * blockSlots); index < (block + 1) * blockSlots;
             index = FindNextOccupiedSlot(index + 1))
        {
            uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
            Object* object = Reference(start);
            if (ResolveForwarded(object) == object)
            {
                continue;
            }
            uintptr_t count;
            LxrRcResult result = FromMetadata(m_counts.Load(start, &count));
            bool mature = IsMatureCandidate(start);
            if (result != LxrRcResult::Success || (count != 0 && !mature))
            {
                return Fail(LxrRcResult::InvariantViolation);
            }
            if (mature)
            {
                result = FromMetadata(m_counts.ClearStraddleLinesQuiescent(start, GetObjectExtent(object)));
                if (result == LxrRcResult::Success)
                {
                    result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::ReferenceCount,
                        start, 0, SideMetadataMemoryOrder::SequentiallyConsistent));
                }
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
            }
            size_t size = ExtentSlot(index).exchange(0, std::memory_order_acq_rel);
            m_live_objects[index / 64].fetch_and(~(UINT64_C(1) << (index % 64)), std::memory_order_acq_rel);
            m_live_bytes.fetch_sub(size, std::memory_order_relaxed);
            if (mature)
            {
                m_mature_metrics[10].fetch_add(size, std::memory_order_relaxed);
            }
            else
            {
                m_nursery_metrics[5].fetch_add(size, std::memory_order_relaxed);
            }
            result = FromMetadata(m_metadata.Store(
                LxrSideMetadataKind::ValidObject, start, 0, SideMetadataMemoryOrder::SequentiallyConsistent));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReleaseEpoch()
{
    LxrPhaseScope release(m_pause_timers, LxrPauseTimers::ReleaseEpoch);
    LxrRcResult result;
    {
        LxrPhaseScope scope(m_pause_timers, LxrPauseTimers::RetireSources);
        result = RetireForwardedSources();
    }
    if (result == LxrRcResult::Success)
    {
        LxrPhaseScope scope(m_pause_timers, LxrPauseTimers::ReclaimNursery);
        result = ReclaimNursery();
    }
    if (result == LxrRcResult::Success)
    {
        LxrPhaseScope scope(m_pause_timers, LxrPauseTimers::SweepBlocks);
        result = Sweep(nullptr);
    }
    if (result == LxrRcResult::Success && m_configuration.evacuateMature &&
        (m_pause_kind == LxrPauseKind::Full || m_pause_kind == LxrPauseKind::FinalMark))
    {
        DeleteRememberedChunks();
        for (size_t block = 0; block < GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes; block++)
        {
            if (m_evacuation_sources[block].exchange(0, std::memory_order_acq_rel) != 0)
            {
                uintptr_t address = GetReservationStart() + block * ImmixBlockGeometry::BlockBytes;
                result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::BlockDefrag,
                    address, 0, SideMetadataMemoryOrder::SequentiallyConsistent));
                if (result != LxrRcResult::Success)
                {
                    break;
                }
            }
        }
    }
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    m_current_roots ^= 1;
    m_root_counts[m_current_roots] = 0;
    ImmixBlockOperationStatus status;
    {
        LxrPhaseScope flip(m_pause_timers, LxrPauseTimers::ReleaseFlip);
        if (m_blocks.ReleaseGcPause(&status) != SideMetadataResult::Success ||
            status != ImmixBlockOperationStatus::Updated ||
            m_large.EndCollection() != LxrLargeObjectResult::Success)
        {
            return Fail(LxrRcResult::InvalidPhase);
        }
    }
    size_t bank = m_producer_bank.load(std::memory_order_relaxed) ^ 1;
    result = ResetBank(m_banks[bank]);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_LOG_REUSE_CONTROL)
    bank ^= 1;
    result = ResetBank(m_banks[bank]);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_LOG_REUSE_CONTROL
    m_producer_bank.store(bank, std::memory_order_release);
    m_metrics[Epochs].fetch_add(1, std::memory_order_relaxed);
    m_epoch_fresh_blocks.store(m_allocation.GetFreshBlocksAcquired(), std::memory_order_release);
    m_epoch_recycled_lines.store(m_allocation.GetRecycledLinesConsumed(), std::memory_order_release);
    if (m_pause_kind == LxrPauseKind::InitialMark)
    {
        m_trace_phase.store(LxrTracePhase::Concurrent, std::memory_order_release);
        m_marking.store(true, std::memory_order_release);
        m_trace_metrics[InitialMarks].fetch_add(1, std::memory_order_relaxed);
    }
    else if (m_pause_kind == LxrPauseKind::FinalMark || m_pause_kind == LxrPauseKind::Full)
    {
        m_trace_metrics[m_pause_kind == LxrPauseKind::Full ? FullMarks : FinalMarks].fetch_add(
            1, std::memory_order_relaxed);
    }
    if (m_marking.load(std::memory_order_acquire) &&
        m_large.BeginCollection(true, false) != LxrLargeObjectResult::Success)
    {
        return Fail(LxrRcResult::InvalidPhase);
    }
    return LxrRcResult::Success;
}

void LxrReferenceCounting::Execute(LxrWorkPacket* packet, size_t worker, void* context)
{
    Work* work = static_cast<Work*>(context);
    LxrReferenceCounting* engine = work->engine;
    if (engine->GetFailure() != LxrRcResult::Success)
    {
        return;
    }
    LxrPauseTimers::Group group = LxrPauseTimers::MatureSweep;
    bool timed = true;
    switch (work->kind)
    {
        case WorkKind::Slots:
        case WorkKind::NurserySlots:
        case WorkKind::Roots:
        case WorkKind::Targets: group = LxrPauseTimers::Increments; break;
        case WorkKind::Decrements:
        case WorkKind::PreviousRoots: group = LxrPauseTimers::Decrements; break;
        case WorkKind::Satb: group = LxrPauseTimers::Satb; break;
        case WorkKind::TraceObjects:
        case WorkKind::TraceScan: timed = false; break;
        case WorkKind::PrepareTrace: group = LxrPauseTimers::PrepareTrace; break;
        case WorkKind::WeakClosure: group = LxrPauseTimers::WeakClosure; break;
        case WorkKind::EpochClosure: group = LxrPauseTimers::EpochClosure; break;
        case WorkKind::NurserySweep: group = LxrPauseTimers::NurserySweep; break;
        default: group = LxrPauseTimers::MatureSweep; break;
    }
    PacketScope packetTimer(engine->m_pause_timers, group, timed);
    switch (work->kind)
    {
        case WorkKind::Slots:
        case WorkKind::NurserySlots:
        case WorkKind::Roots:
        case WorkKind::Targets:
        {
            if (engine->GetTracePhase() == LxrTracePhase::Full)
            {
                engine->ProcessFullIncrementWork(work, worker);
            }
            else
            {
                engine->ProcessIncrementWork(work, worker, nullptr);
            }
            break;
        }
        case WorkKind::Satb:
            engine->m_trace_metrics[SatbEntries].fetch_add(
                packet->GetReferenceCount(), std::memory_order_relaxed);
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_TRACE_SKIP_SATB_CONTROL)
            engine->QueueTrace(work->references, packet->GetReferenceCount(), nullptr, true);
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_TRACE_SKIP_SATB_CONTROL
            break;
        case WorkKind::PrepareTrace:
        {
            LxrRcResult result = engine->FixForwardingReaders();
            if (result == LxrRcResult::Success && engine->m_object_model.finishRelocation != nullptr)
            {
                result = engine->m_object_model.finishRelocation(engine, engine->m_object_model.context);
            }
            if (result == LxrRcResult::Success && engine->m_pause_kind != LxrPauseKind::RefCount)
            {
                if (engine->m_configuration.evacuateMature &&
                    (engine->m_pause_kind == LxrPauseKind::Full || engine->m_pause_kind == LxrPauseKind::FinalMark))
                {
                    result = engine->ReclaimLargeNursery();
                    size_t released = 0;
                    if (result == LxrRcResult::Success &&
                        engine->m_large.SweepNursery(&released) != LxrLargeObjectResult::Success)
                    {
                        result = engine->Fail(LxrRcResult::MetadataFailure);
                    }
                    engine->m_metrics[LargeObjectBytesReleased].fetch_add(released, std::memory_order_relaxed);
                }
                if (result == LxrRcResult::Success &&
                    (engine->m_pause_kind == LxrPauseKind::Full || engine->m_pause_kind == LxrPauseKind::FinalMark))
                {
                    result = engine->RetainPinnedMatureBlocks();
                }
            }
            if (result == LxrRcResult::Success && engine->m_pause_kind != LxrPauseKind::RefCount)
            {
                result = engine->PrepareTrace();
            }
            if (result == LxrRcResult::Success && engine->m_pause_kind == LxrPauseKind::FinalMark)
            {
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_REMSET_REPLAY_CONTROL)
                result = engine->ReplayMatureRememberedSlots();
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_REMSET_REPLAY_CONTROL
            }
            if (result == LxrRcResult::Success)
            {
                result = engine->FixForwardingReaders();
            }
            if (result == LxrRcResult::Success && engine->m_object_model.finishRelocation != nullptr)
            {
                result = engine->m_object_model.finishRelocation(engine, engine->m_object_model.context);
            }
            if (result != LxrRcResult::Success)
            {
                engine->Fail(result);
            }
            break;
        }
        case WorkKind::WeakClosure:
            if (engine->m_object_model.finishTrace != nullptr)
            {
                LxrRcResult result = engine->m_object_model.finishTrace(
                    engine, engine->m_object_model.context);
                if (result != LxrRcResult::Success)
                {
                    engine->Fail(result);
                }
            }
            if (engine->GetFailure() == LxrRcResult::Success)
            {
                LxrRcResult result = engine->FixForwardingReaders();
                if (result == LxrRcResult::Success && engine->m_object_model.finishRelocation != nullptr)
                {
                    result = engine->m_object_model.finishRelocation(engine, engine->m_object_model.context);
                }
                if (result != LxrRcResult::Success)
                {
                    engine->Fail(result);
                }
            }
            break;
        case WorkKind::TraceObjects:
        case WorkKind::TraceScan:
            engine->ProcessTraceWork(work);
            break;
        case WorkKind::EpochClosure:
        {
            LxrRcResult result = LxrRcResult::Success;
            if (engine->m_object_model.finishEpoch != nullptr)
            {
                result = engine->m_object_model.finishEpoch(engine, engine->m_object_model.context);
            }
            if (result != LxrRcResult::Success)
            {
                engine->Fail(result);
            }
            break;
        }
        case WorkKind::Decrements:
        case WorkKind::PreviousRoots:
            engine->ProcessDecrements(work, packet, worker);
            break;
        case WorkKind::NurserySweep:
            engine->ReleaseEpoch();
            break;
        case WorkKind::MatureSweep:
        {
            LxrBlockReleaseGuard guard;
            if (packet->CreateBlockReleaseGuard(&guard) != LxrCompletionResult::Success ||
                !guard.IsValid() || guard.GetGeneration() != packet->GetGeneration())
            {
                engine->Fail(LxrRcResult::InvariantViolation);
                return;
            }
            engine->Sweep(&guard);
            break;
        }
    }
}

LxrRcResult LxrReferenceCounting::ProcessIncrementWork(Work* work, size_t worker, TraceBatch* targets)
{
    ReferenceBatch children{this, WorkKind::NurserySlots, nullptr, {}};
    for (size_t index = 0; index < work->packet.GetReferenceCount(); index++)
    {
        Object* object;
        Object** ownedSlot = nullptr;
        bool trace = work->trace;
        if (work->kind == WorkKind::Roots || work->kind == WorkKind::Targets)
        {
            uintptr_t reference = work->GetReference(index);
            if (work->kind == WorkKind::Roots)
            {
                trace = (reference & 1) == 0;
                reference &= ~static_cast<uintptr_t>(1);
            }
            object = reinterpret_cast<Object*>(reference);
            if (work->kind == WorkKind::Roots)
            {
                CountWorker(worker, RootIncrements, 1);
            }
        }
        else
        {
            Object** slot = reinterpret_cast<Object**>(work->GetReference(index));
            ownedSlot = slot;
            object = *slot;
            if (work->kind == WorkKind::Slots &&
                FromMetadata(m_metadata.Store(LxrSideMetadataKind::FieldUnlogged,
                    reinterpret_cast<uintptr_t>(slot), 1, SideMetadataMemoryOrder::Relaxed)) !=
                    LxrRcResult::Success)
            {
                return GetFailure();
            }
            CountWorker(worker, IncrementEntries, 1);
        }
        LxrRcResult result = ProcessIncrement(&object, trace, &children, worker, targets);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (ownedSlot != nullptr && object != nullptr)
        {
            result = RememberMatureSlot(ownedSlot, object);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        if (ownedSlot != nullptr)
        {
            *ownedSlot = object;
        }
    }
    LxrRcResult result = children.Flush();
    return result == LxrRcResult::Success && targets != nullptr ? targets->Flush() : result;
}

NOINLINE LxrRcResult LxrReferenceCounting::ProcessFullIncrementWork(Work* work, size_t worker)
{
    TraceBatch targets(this);
    return GetFailure() == LxrRcResult::Success
        ? ProcessIncrementWork(work, worker, &targets) : GetFailure();
}

LxrCompletionCallbackResult LxrReferenceCounting::DecrementsDrained(
    LxrMatureSweepContinuation* continuation, void* context)
{
    LxrReferenceCounting* engine = static_cast<LxrReferenceCounting*>(context);
    if (engine->GetFailure() != LxrRcResult::Success)
    {
        return LxrCompletionCallbackResult::Failure;
    }
    Work* work = engine->NewWork(
        WorkKind::MatureSweep, nullptr, 0, LxrWorkBucketStage::Unconstrained);
    if (work == nullptr ||
        engine->FromScheduler(engine->m_scheduler.SubmitMatureSweep(continuation, &work->packet)) !=
            LxrRcResult::Success)
    {
        return LxrCompletionCallbackResult::Failure;
    }
    return LxrCompletionCallbackResult::Success;
}

LxrCompletionCallbackResult LxrReferenceCounting::OverallDrained(uint64_t, void* context)
{
    LxrReferenceCounting* engine = static_cast<LxrReferenceCounting*>(context);
    if (engine->m_cycle_sweep)
    {
        engine->m_cycle_sweep = false;
        engine->m_trace_phase.store(LxrTracePhase::Inactive, std::memory_order_release);
    }
    if (engine->m_object_model.finishLazy != nullptr)
    {
        LxrRcResult result = engine->m_object_model.finishLazy(engine, engine->m_object_model.context);
        if (result != LxrRcResult::Success)
        {
            engine->Fail(result);
        }
    }
    return engine->GetFailure() == LxrRcResult::Success
        ? LxrCompletionCallbackResult::Success : LxrCompletionCallbackResult::Failure;
}

LxrRcResult LxrReferenceCounting::LoadCount(Object* object, uintptr_t* count) const
{
    if (object == nullptr || count == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    SideMetadataResult result = m_metadata.Load(
        LxrSideMetadataKind::ReferenceCount, Start(object), SideMetadataMemoryOrder::Relaxed, count);
    return result == SideMetadataResult::Success ? LxrRcResult::Success : LxrRcResult::MetadataFailure;
}

LxrRcResult LxrReferenceCounting::FindContainingObject(uintptr_t address, Object** object) const
{
    if (object == nullptr || (!IsImmix(address) && !IsLarge(address)))
    {
        return LxrRcResult::InvalidArgument;
    }
    *object = nullptr;
    if (IsLarge(address))
    {
        LxrLargeObjectAllocation allocation;
        LxrLargeObjectResult result = m_large.GetContainingAllocation(address, &allocation);
        if (result == LxrLargeObjectResult::NotFound)
        {
            return LxrRcResult::InvalidArgument;
        }
        if (result != LxrLargeObjectResult::Success)
        {
            return LxrRcResult::MetadataFailure;
        }
        Object* candidate = Reference(allocation.object);
        if (address < reinterpret_cast<uintptr_t>(candidate) || GetObjectExtent(candidate) == 0)
        {
            return LxrRcResult::InvalidArgument;
        }
        *object = candidate;
        return LxrRcResult::Success;
    }
    uintptr_t lower = IsImmix(address)
        ? address & ~ImmixBlockGeometry::BlockMask : m_large.GetReservationStart();
    uintptr_t cursor = address & ~(sizeof(uintptr_t) - 1);
    while (cursor >= lower)
    {
        uintptr_t valid;
        if (m_metadata.Load(LxrSideMetadataKind::ValidObject, cursor,
                SideMetadataMemoryOrder::Relaxed, &valid) != SideMetadataResult::Success)
        {
            return LxrRcResult::MetadataFailure;
        }
        if (valid != 0)
        {
            Object* candidate = Reference(cursor);
            size_t size = ExtentSlot((cursor - GetReservationStart()) / sizeof(uintptr_t)).load(
                std::memory_order_acquire);
            if (size != 0 && address >= reinterpret_cast<uintptr_t>(candidate) &&
                address - cursor < size)
            {
                *object = candidate;
                return LxrRcResult::Success;
            }
            if (size != 0)
            {
                return LxrRcResult::InvalidArgument;
            }
        }
        if (cursor == lower)
        {
            break;
        }
        cursor -= sizeof(uintptr_t);
    }
    return LxrRcResult::InvalidArgument;
}

LxrRcMetrics LxrReferenceCounting::GetMetrics() const
{
    return {
        m_metrics[Allocations].load(), m_metrics[AllocatedBytes].load(),
        m_metrics[LoggedSlots].load(), SumWorker(IncrementEntries),
        SumWorker(DecrementEntries), m_metrics[BufferFlushes].load(),
        m_metrics[ThreadFlushes].load(), SumWorker(Promotions),
        SumWorker(Deaths), SumWorker(RootIncrements),
        SumWorker(RootDecrements), SumWorker(ObjectScans),
        SumWorker(ObjectSizeQueries), m_metrics[OldTargetChecks].load(),
        m_metrics[ChildRegistrations].load(), m_metrics[BlocksReleased].load(),
        m_metrics[LargeObjectBytesReleased].load(), m_metrics[Epochs].load(),
    };
}

LxrRcResult LxrReferenceCounting::GetFailure() const
{
    return m_failure.load(std::memory_order_acquire);
}

LxrRcPhase LxrReferenceCounting::GetPhase() const
{
    return m_phase.load(std::memory_order_acquire);
}

uintptr_t LxrReferenceCounting::GetFieldMetadataBase() const
{
    return m_metadata.GetBiasedBase(LxrSideMetadataKind::FieldUnlogged);
}

LxrRcResult LxrReferenceCounting::Reclaim(uintptr_t start)
{
    size_t index = (start - GetReservationStart()) / sizeof(uintptr_t);
    size_t size = ExtentSlot(index).exchange(0, std::memory_order_acq_rel);
    if (size == 0)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    m_live_objects[index / 64].fetch_and(~(UINT64_C(1) << (index % 64)), std::memory_order_acq_rel);
    m_live_bytes.fetch_sub(size, std::memory_order_relaxed);
    m_reclaimed_bytes.fetch_add(size, std::memory_order_relaxed);
    if (m_probe_start.load(std::memory_order_acquire) == start)
    {
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_PROBE_SKIP_RECLAIM_CONTROL)
        m_probe_reclaims.fetch_add(1, std::memory_order_release);
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_PROBE_SKIP_RECLAIM_CONTROL
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ArmLifetimeProbe(Object* object)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetObjectExtent(object) == 0)
    {
        return LxrRcResult::InvalidArgument;
    }
    uintptr_t expected = 0;
    return m_probe_start.compare_exchange_strong(expected, Start(object), std::memory_order_release)
        ? LxrRcResult::Success : LxrRcResult::InvalidPhase;
}

uint64_t LxrReferenceCounting::GetLifetimeProbeBirths() const
{
    return m_probe_births.load(std::memory_order_acquire);
}

uint64_t LxrReferenceCounting::GetLifetimeProbeReclaims() const
{
    return m_probe_reclaims.load(std::memory_order_acquire);
}

uint64_t LxrReferenceCounting::GetStorageMetric(size_t metric) const
{
    if (metric >= 52 && metric < 57)
    {
        return m_storage_diagnostics.load(std::memory_order_acquire)
            ? m_trace_batch_metrics[metric - 52].load(std::memory_order_acquire) : UINT64_MAX;
    }
    if (metric >= 28 && metric < 52 && m_banks != nullptr)
    {
        size_t index = (metric - 28) / 6;
        const LogBank& bank = m_banks[index / 2];
        const LogStream& stream = index % 2 == 0 ? bank.increments : bank.decrements;
        switch ((metric - 28) % 6)
        {
            case 0: return stream.capacity;
            case 1: return stream.reserved.load(std::memory_order_acquire);
            case 2: return m_storage_diagnostics.load() ? stream.highWater.load() : UINT64_MAX;
            case 3: return m_storage_diagnostics.load() ? stream.flushes.load() : UINT64_MAX;
            case 4: return m_storage_diagnostics.load() ? stream.copiedBytes.load() : UINT64_MAX;
            case 5: return m_storage_diagnostics.load() ? stream.resetBytes.load() : UINT64_MAX;
        }
    }
    if (metric < 7)
    {
        return m_storage_metrics[metric].load(std::memory_order_acquire);
    }
    switch (metric)
    {
        case 7: return m_extent_storage.GetCommittedBytes();
        case 8: return m_trace_lock_storage.GetCommittedBytes();
        case 9: return m_trace_scan_storage.GetCommittedBytes();
        case 10: return m_live_storage.GetCommittedBytes();
        case 23: return m_storage_metrics[7].load(std::memory_order_acquire);
        case 24: return m_storage_metrics[8].load(std::memory_order_acquire);
        case 25: return 2 * m_configuration.maximumRoots * sizeof(uintptr_t);
        case 26: return m_configuration.maximumMutators * (sizeof(LxrRcMutator) + 2 * sizeof(LogChunk));
        case 27: return m_allocated_block_storage.GetCommittedBytes();
        default: break;
    }
    size_t streamMetric = metric - 11;
    if (streamMetric < 12 && m_banks != nullptr)
    {
        if (streamMetric % 6 >= 4)
        {
            return 0;
        }
        const LogBank& bank = m_banks[streamMetric / 6];
        const LogStream* stream = streamMetric % 6 < 2 ? &bank.increments : &bank.decrements;
        return streamMetric % 2 == 0 ? stream->referenceStorage.GetCommittedBytes() :
            stream->publicationStorage.GetCommittedBytes();
    }
    return UINT64_MAX;
}

LxrRcResult LxrReferenceCounting::GetStorageRange(
    size_t component, uintptr_t* start, size_t* bytes) const
{
    if (start == nullptr || bytes == nullptr || m_arena == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    *start = 0;
    *bytes = 0;
    switch (component)
    {
        case 0:
            *start = reinterpret_cast<uintptr_t>(m_extent_storage.Get());
            *bytes = m_extent_storage.GetReservedBytes();
            return LxrRcResult::Success;
        case 1:
            *start = reinterpret_cast<uintptr_t>(m_trace_lock_storage.Get());
            *bytes = m_trace_lock_storage.GetReservedBytes();
            return LxrRcResult::Success;
        case 2:
            *start = reinterpret_cast<uintptr_t>(m_trace_scan_storage.Get());
            *bytes = m_trace_scan_storage.GetReservedBytes();
            return LxrRcResult::Success;
        case 3:
            *start = reinterpret_cast<uintptr_t>(m_live_storage.Get());
            *bytes = m_live_storage.GetReservedBytes();
            return LxrRcResult::Success;
        default: break;
    }
    if (component < 16)
    {
        size_t index = component - 4;
        if (index % 6 >= 4)
        {
            return LxrRcResult::Success;
        }
        const LogBank& bank = m_banks[index / 6];
        const LogStream* stream = index % 6 < 2 ? &bank.increments : &bank.decrements;
        if (index % 2 == 0)
        {
            *start = reinterpret_cast<uintptr_t>(stream->referenceStorage.Get());
            *bytes = stream->referenceStorage.GetReservedBytes();
        }
        else
        {
            *start = reinterpret_cast<uintptr_t>(stream->publicationStorage.Get());
            *bytes = stream->publicationStorage.GetReservedBytes();
        }
        return LxrRcResult::Success;
    }
    if (component >= 17 && component - 17 < LxrSideMetadataLayout::SpecCount)
    {
        const SideMetadataSpec& spec = m_layout.GetSpecs()[component - 17];
        uintptr_t first = GetReservationStart() >> spec.log_bytes_per_value;
        uintptr_t last = (GetReservationStart() + GetReservationBytes() - 1) >> spec.log_bytes_per_value;
        size_t valueBytes = 1;
        if (spec.log_bits_per_value <= 3)
        {
            first >>= 3 - spec.log_bits_per_value;
            last >>= 3 - spec.log_bits_per_value;
        }
        else
        {
            first <<= spec.log_bits_per_value - 3;
            last <<= spec.log_bits_per_value - 3;
            valueBytes <<= spec.log_bits_per_value - 3;
        }
        size_t page = GCToOSInterface::GetPageSize();
        *start = (spec.base_address + first) & ~(page - 1);
        uintptr_t end = (spec.base_address + last + valueBytes + page - 1) & ~(page - 1);
        *bytes = end - *start;
        return LxrRcResult::Success;
    }
    if (component == 17 + LxrSideMetadataLayout::SpecCount)
    {
        *start = reinterpret_cast<uintptr_t>(m_allocated_block_storage.Get());
        *bytes = m_allocated_block_storage.GetReservedBytes();
        return LxrRcResult::Success;
    }
    return LxrRcResult::InvalidArgument;
}

LxrRcResult LxrReferenceCounting::EnableStorageDiagnostics(uint32_t timeout)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Inactive ||
        m_storage_diagnostics.load(std::memory_order_acquire))
    {
        return LxrRcResult::InvalidPhase;
    }
    LxrRcResult result = Drain(timeout);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    uint64_t workBytes = 0;
    for (Work* work = m_work.load(std::memory_order_acquire); work != nullptr; work = work->next)
    {
        workBytes += sizeof(Work) +
            (work->ownedReferences == nullptr ? 0 : work->packet.GetReferenceCount() * sizeof(uintptr_t));
    }
    uint64_t scanBytes = 0;
    for (TraceScan* scan = m_scan_work.load(std::memory_order_acquire); scan != nullptr; scan = scan->next)
    {
        scanBytes += sizeof(TraceScan);
    }
    m_storage_metrics[1].store(workBytes, std::memory_order_relaxed);
    m_storage_metrics[2].store(workBytes, std::memory_order_relaxed);
    m_storage_metrics[5].store(scanBytes, std::memory_order_relaxed);
    m_storage_metrics[6].store(scanBytes, std::memory_order_relaxed);
    m_storage_diagnostics.store(true, std::memory_order_release);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReclaimNursery()
{
    size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
    for (size_t block = 0; block < GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes; block++)
    {
        if (m_allocated_block_storage.Get()[block].exchange(0, std::memory_order_acq_rel) == 0)
        {
            continue;
        }
        for (size_t index = FindNextOccupiedSlot(block * blockSlots); index < (block + 1) * blockSlots;
             index = FindNextOccupiedSlot(index + 1))
        {
            uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
            uintptr_t references;
            LxrRcResult result = FromMetadata(m_counts.Load(start, &references));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            if (references == 0 && Reclaim(start) != LxrRcResult::Success)
            {
                return GetFailure();
            }
        }
    }
    return ReclaimLargeNursery();
}

LxrRcResult LxrReferenceCounting::ReclaimLargeNursery()
{
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(m_configuration.immixBytes / sizeof(uintptr_t)); index < slots;
         index = FindNextOccupiedSlot(index + 1))
    {
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        uintptr_t references;
        LxrRcResult result = FromMetadata(m_counts.Load(start, &references));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (references == 0 && Reclaim(start) != LxrRcResult::Success)
        {
            return GetFailure();
        }
    }
    return LxrRcResult::Success;
}

uintptr_t LxrReferenceCounting::GetReservationStart() const
{
    return reinterpret_cast<uintptr_t>(m_arena);
}

size_t LxrReferenceCounting::GetReservationBytes() const
{
    return m_configuration.immixBytes + m_configuration.largeObjectBytes;
}

size_t LxrReferenceCounting::GetLiveBytes() const
{
    return m_live_bytes.load(std::memory_order_acquire);
}

uint64_t LxrReferenceCounting::GetReclaimedBytes() const
{
    return m_reclaimed_bytes.load(std::memory_order_acquire);
}

bool LxrReferenceCounting::OwnsAddress(uintptr_t address) const
{
    return m_arena != nullptr && (IsImmix(address) || IsLarge(address));
}

size_t LxrReferenceCounting::GetObjectExtent(Object* object) const
{
    uintptr_t start = Start(object);
    if (!OwnsAddress(start) || start % sizeof(uintptr_t) != 0)
    {
        return 0;
    }
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_OBJECT_INTERIOR_CONTROL)
    if (IsLarge(start) && start % LxrLargeObjectSpace::PageBytes != 0)
    {
        return 0;
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_OBJECT_INTERIOR_CONTROL
    return ExtentSlot((start - GetReservationStart()) / sizeof(uintptr_t)).load(std::memory_order_acquire);
}

Object* LxrReferenceCounting::FindNextObject(Object* object) const
{
    uintptr_t start = Start(object);
    if (!OwnsAddress(start) || start % sizeof(uintptr_t) != 0)
    {
        return nullptr;
    }
    if (GetPhase() == LxrRcPhase::Mutator)
    {
        size_t size = GetObjectExtent(object);
        if (size == 0 || size >= GetReservationBytes() - (start - GetReservationStart()))
        {
            return nullptr;
        }
        Object* next = Reference(start + size);
        return GetObjectExtent(next) == 0 ? nullptr : next;
    }
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    size_t first = (start - GetReservationStart()) / sizeof(uintptr_t) + 1;
    for (size_t index = FindNextOccupiedSlot(first); index < slots;
         index = FindNextOccupiedSlot(index + 1))
    {
        Object* candidate = Reference(GetReservationStart() + index * sizeof(uintptr_t));
        if (ExtentSlot(index).load(std::memory_order_acquire) != 0 && ResolveForwarded(candidate) == candidate)
        {
            return candidate;
        }
    }
    return nullptr;
}

size_t LxrReferenceCounting::GetMarkedBytes() const
{
    size_t bytes = 0;
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(0); index < slots;
         index = FindNextOccupiedSlot(index + 1))
    {
        size_t size = ExtentSlot(index).load(std::memory_order_acquire);
        if (size != 0 && IsMarked(Reference(GetReservationStart() + index * sizeof(uintptr_t))))
        {
            bytes += size;
        }
    }
    return bytes;
}

size_t LxrReferenceCounting::GetCommittedBytes() const
{
    return m_immix_capacity.load(std::memory_order_acquire) + m_large.GetCommittedBytes();
}

size_t LxrReferenceCounting::GetImmixReservationBytes() const
{
    return m_configuration.immixBytes;
}

size_t LxrReferenceCounting::GetImmixCapacityBytes() const
{
    return m_immix_capacity.load(std::memory_order_acquire);
}

size_t LxrReferenceCounting::GetAdmittedBytes() const
{
    return GetImmixCapacityBytes() + m_large.GetAdmittedBytes();
}

uint64_t LxrReferenceCounting::GetGrowthCount() const
{
    return m_growth_count.load(std::memory_order_acquire);
}

size_t LxrReferenceCounting::GetNativePlaneCommittedBytes() const
{
    size_t bytes = m_extent_storage.GetCommittedBytes() + m_trace_lock_storage.GetCommittedBytes() +
        m_trace_scan_storage.GetCommittedBytes() + m_live_storage.GetCommittedBytes() +
        m_allocated_block_storage.GetCommittedBytes();
    for (size_t bank = 0; bank < 2; bank++)
    {
        for (const LogStream* stream : {
                &m_banks[bank].increments, &m_banks[bank].decrements})
        {
            bytes += stream->referenceStorage.GetCommittedBytes() +
                stream->publicationStorage.GetCommittedBytes();
        }
    }
    return bytes;
}

size_t LxrReferenceCounting::FindNextOccupiedSlot(size_t first) const
{
    size_t total = GetReservationBytes() / sizeof(uintptr_t);
    size_t immixEnd = GetImmixCapacityBytes() / sizeof(uintptr_t);
    size_t largeStart = m_configuration.immixBytes / sizeof(uintptr_t);
    size_t largeEnd = largeStart + m_large.GetAdmittedBytes() / sizeof(uintptr_t);
    while (first < largeEnd)
    {
        if (first >= immixEnd && first < largeStart)
        {
            first = largeStart;
        }
        uint64_t bits = m_live_objects[first / 64].load(std::memory_order_acquire) &
            (UINT64_MAX << (first % 64));
#ifdef TARGET_WINDOWS
        DWORD bit;
#else
        uint32_t bit;
#endif // TARGET_WINDOWS
        if (BitScanForward64(&bit, bits))
        {
            return first / 64 * 64 + bit;
        }
        first = (first / 64 + 1) * 64;
    }
    return total;
}

LxrRcResult LxrReferenceCounting::RequestGrowth(bool large, size_t minimumBytes)
{
    if (GetPhase() != LxrRcPhase::Mutator || m_grow_bytes != 0 || minimumBytes == 0)
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t current = large ? m_large.GetAdmittedBytes() : GetImmixCapacityBytes();
    size_t reserved = large ? m_configuration.largeObjectBytes : m_configuration.immixBytes;
    size_t remaining = m_configuration.hardLimitBytes - GetAdmittedBytes();
    if (reserved == current || remaining == 0 ||
        (large ? minimumBytes > reserved : minimumBytes > reserved - current || minimumBytes > remaining))
    {
        return LxrRcResult::AllocationFailure;
    }
    m_grow_large = large;
    m_grow_bytes = minimumBytes;
    m_growth_result = LxrRcResult::AllocationFailure;
    return LxrRcResult::Success;
}

size_t LxrReferenceCounting::GetCleanNurseryBlocks() const
{
    return m_allocation.GetFreshBlocksAcquired() - m_epoch_fresh_blocks.load(std::memory_order_acquire);
}

size_t LxrReferenceCounting::GetYoungAllocationBytes() const
{
    size_t lines = m_allocation.GetRecycledLinesConsumed() -
        m_epoch_recycled_lines.load(std::memory_order_acquire);
    return GetCleanNurseryBlocks() * ImmixBlockGeometry::BlockBytes +
        (lines >> 4) * LxrLargeObjectSpace::PageBytes;
}

size_t LxrReferenceCounting::GetImmixFreePages() const
{
    size_t capacity = GetImmixCapacityBytes();
    size_t used = m_allocation.GetBlocksInUse() * ImmixBlockGeometry::BlockBytes;
    return used >= capacity ? 0 : (capacity - used) / LxrLargeObjectSpace::PageBytes;
}

size_t LxrReferenceCounting::GetPendingIncrementEntries() const
{
    return m_banks[m_producer_bank.load(std::memory_order_acquire)].increments.reserved.load(
        std::memory_order_acquire);
}

LxrRcResult LxrReferenceCounting::DrainLazySweep(uint32_t timeout)
{
    return DrainEpoch(timeout);
}

LxrRcResult LxrReferenceCounting::GetMaturePages(size_t* pages) const
{
    if (pages == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t bytes;
    if (m_large.GetMatureCommittedBytes(&bytes) != LxrLargeObjectResult::Success)
    {
        return LxrRcResult::MetadataFailure;
    }
    for (size_t offset = 0; offset < GetImmixCapacityBytes(); offset += ImmixBlockGeometry::BlockBytes)
    {
        ImmixBlockState state;
        uint8_t unavailable;
        if (m_blocks.GetState(GetReservationStart() + offset, &state, &unavailable) !=
            SideMetadataResult::Success)
        {
            return LxrRcResult::MetadataFailure;
        }
        if (state != ImmixBlockState::Unallocated)
        {
            bytes += ImmixBlockGeometry::BlockBytes;
        }
    }
    *pages = bytes / LxrLargeObjectSpace::PageBytes;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GetGrowthResult() const
{
    return m_growth_result;
}

LxrRcResult LxrReferenceCounting::Grow(
    bool large, size_t minimumBytes, LxrRcCaptureRoots capture, void* context, uint32_t timeout)
{
    LxrRcResult result = RequestGrowth(large, minimumBytes);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    result = Collect(capture, context, timeout);
    return result == LxrRcResult::Success ? GetGrowthResult() : result;
}

LxrRcResult LxrReferenceCounting::GrowSuspended()
{
    size_t current = m_grow_large ? m_large.GetAdmittedBytes() : GetImmixCapacityBytes();
    size_t reserved = m_grow_large ? m_configuration.largeObjectBytes : m_configuration.immixBytes;
    size_t granularity = m_grow_large ? LxrLargeObjectSpace::PageBytes : ImmixBlockGeometry::BlockBytes;
    size_t available = m_configuration.hardLimitBytes - GetAdmittedBytes();
    size_t maximum = current + (reserved - current < available ? reserved - current : available);
    maximum &= ~(granularity - 1);
    size_t required = m_grow_large ? m_large.GetRequiredGrowthBytes(m_grow_bytes) : m_grow_bytes;
    if (maximum <= current || required == 0 || required > maximum - current)
    {
        return LxrRcResult::AllocationFailure;
    }
    size_t minimum = (required + granularity - 1) & ~(granularity - 1);
    size_t increment = current > minimum ? current : minimum;
    increment = increment > maximum - current ? maximum - current : increment;
    size_t offset = m_grow_large ? m_configuration.immixBytes : 0;
    size_t first = (offset + current) / sizeof(uintptr_t);
    size_t previousLogCapacity = GetAdmittedBytes() / sizeof(uintptr_t);
    auto fits = [&](size_t candidate)
    {
        size_t slots = candidate / sizeof(uintptr_t);
        size_t objectFirst = GetObjectStorageIndex(first);
        size_t objectSlots = GetObjectStorageIndex(first + slots) - objectFirst;
        size_t nativeBytes = m_extent_storage.ProjectedCommittedBytes(objectFirst, objectSlots) +
            GetTracePlaneBudget(
                m_grow_large ? GetImmixCapacityBytes() : current + candidate,
                m_grow_large ? current + candidate : m_large.GetAdmittedBytes()) +
            m_live_storage.ProjectedCommittedBytes(first / 64, slots / 64);
        size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
        nativeBytes += m_grow_large ? m_allocated_block_storage.GetCommittedBytes() :
            m_allocated_block_storage.ProjectedCommittedBytes(first / blockSlots, slots / blockSlots);
        for (size_t bank = 0; bank < 2; bank++)
        {
            for (LogStream* stream : {
                    &m_banks[bank].increments, &m_banks[bank].decrements})
            {
                size_t desired = previousLogCapacity + slots;
                nativeBytes += stream->referenceStorage.ProjectedCommittedBytes(
                    stream->capacity, desired - stream->capacity);
            }
        }
        return m_object_model.canAdmitRange(first, slots, nativeBytes, m_object_model.context);
    };
    if (m_object_model.canAdmitRange != nullptr && !fits(increment))
    {
        if (!fits(minimum))
        {
            return LxrRcResult::OutOfMemory;
        }
        size_t low = minimum / granularity;
        size_t high = increment / granularity;
        while (low < high)
        {
            size_t middle = low + (high - low + 1) / 2;
            if (fits(middle * granularity))
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }
        increment = low * granularity;
    }
    size_t next = current + increment;
    size_t count = increment / sizeof(uintptr_t);
    size_t logIncrement = count;
    if (!CommitPlanes(first, count))
    {
        return LxrRcResult::OutOfMemory;
    }
    for (size_t bank = 0; bank < 2; bank++)
    {
        for (LogStream* stream : {
                &m_banks[bank].increments, &m_banks[bank].decrements})
        {
            size_t desired = previousLogCapacity + logIncrement;
            if (!stream->referenceStorage.Commit(stream->capacity, desired - stream->capacity, false))
            {
                return LxrRcResult::OutOfMemory;
            }
        }
    }
    if (m_object_model.admitRange != nullptr &&
        !m_object_model.admitRange(first, count, m_object_model.context))
    {
        return LxrRcResult::OutOfMemory;
    }
    uintptr_t start = GetReservationStart() + offset + current;
    if (m_metadata.CommitDataRange(start, increment) != SideMetadataResult::Success)
    {
        return LxrRcResult::OutOfMemory;
    }
    if (m_grow_large)
    {
        if (m_large.GrowQuiescent(next) != LxrLargeObjectResult::Success)
        {
            return LxrRcResult::OutOfMemory;
        }
    }
    else
    {
        if (!GCToOSInterface::VirtualCommit(reinterpret_cast<void*>(start), increment))
        {
            return LxrRcResult::OutOfMemory;
        }
        if (m_allocation.GrowQuiescent(next) != SideMetadataResult::Success)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
        m_immix_capacity.store(next, std::memory_order_release);
    }
    for (size_t bank = 0; bank < 2; bank++)
    {
        m_banks[bank].capacity = previousLogCapacity + logIncrement;
        m_banks[bank].increments.capacity = m_banks[bank].capacity;
        m_banks[bank].decrements.capacity = m_banks[bank].capacity;
    }
    m_growth_count.fetch_add(1, std::memory_order_release);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GetHeapStatistics(size_t* heapBytes, size_t* fragmentationBytes) const
{
    if (heapBytes == nullptr || fragmentationBytes == nullptr || GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    size_t heap = 0;
    size_t fragmentation = 0;
    bool traced = GetTracePhase() == LxrTracePhase::Full || GetTracePhase() == LxrTracePhase::FinalMark;
    size_t slotsPerBlock = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
    size_t currentBlock = SIZE_MAX;
    size_t end = 0;
    size_t live = 0;
    size_t first = m_configuration.immixBytes / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(0); index < first;
         index = FindNextOccupiedSlot(index + 1))
    {
        size_t size = ExtentSlot(index).load(std::memory_order_acquire);
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        if (size == 0 || (traced && !IsMarked(Reference(start))))
        {
            continue;
        }
        size_t block = index / slotsPerBlock;
        if (block != currentBlock)
        {
            if (live > end)
            {
                return LxrRcResult::InvariantViolation;
            }
            heap += end;
            fragmentation += end - live;
            currentBlock = block;
            end = live = 0;
        }
        size_t limit = (index % slotsPerBlock) * sizeof(uintptr_t) + size;
        end = end > limit ? end : limit;
        live += size;
    }
    if (live > end)
    {
        return LxrRcResult::InvariantViolation;
    }
    heap += end;
    fragmentation += end - live;
    size_t limit = GetReservationBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(first); index < limit;
         index = FindNextOccupiedSlot(index + 1))
    {
        size_t size = ExtentSlot(index).load(std::memory_order_acquire);
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        if (size == 0 || (traced && !IsMarked(Reference(start))))
        {
            continue;
        }
        LxrLargeObjectAllocation allocation;
        if (m_large.GetAllocation(start, &allocation) != LxrLargeObjectResult::Success)
        {
            return LxrRcResult::MetadataFailure;
        }
        size_t pages = allocation.page_count * LxrLargeObjectSpace::PageBytes;
        heap += pages;
        fragmentation += pages - size;
    }
    *heapBytes = heap;
    *fragmentationBytes = fragmentation;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::StopWorkers()
{
    m_scan_checkpoint.store(0, std::memory_order_release);
    return FromScheduler(m_scheduler.Shutdown());
}

bool LxrReferenceCounting::HasPendingDetaches() const
{
    for (size_t index = 0; index < m_configuration.maximumMutators; index++)
    {
        if (m_mutators[index].state.load(std::memory_order_acquire) == 3)
        {
            return true;
        }
    }
    return false;
}

LxrRcResult LxrReferenceCounting::WalkObjects(LxrRcWalkObject visit, void* context) const
{
    if (visit == nullptr || m_observer || GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(0); index < slots;
         index = FindNextOccupiedSlot(index + 1))
    {
        size_t size = ExtentSlot(index).load(std::memory_order_acquire);
        Object* object = Reference(GetReservationStart() + index * sizeof(uintptr_t));
        if (size != 0 && ResolveForwarded(object) == object && !visit(object, size, context))
        {
            break;
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::WalkForwarded(LxrRcWalkForwarded visit, void* context) const
{
    if (visit == nullptr || GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    if (!m_configuration.evacuateNursery && !m_configuration.evacuateMature)
    {
        return LxrRcResult::Success;
    }
    size_t slots = GetImmixCapacityBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(0); index < slots; index = FindNextOccupiedSlot(index + 1))
    {
        Object* source = Reference(GetReservationStart() + index * sizeof(uintptr_t));
        Object* destination = ResolveForwarded(source);
        if (destination != source &&
            !visit(source, destination, ExtentSlot(index).load(std::memory_order_acquire), context))
        {
            break;
        }
    }
    return LxrRcResult::Success;
}

void LxrReferenceCounting::EnterTraceLock(size_t index)
{
    uint8_t expected = 0;
    while (!TraceLockSlot(index).compare_exchange_weak(
        expected, 1, std::memory_order_acquire, std::memory_order_relaxed))
    {
        expected = 0;
        YieldProcessor();
    }
}

void LxrReferenceCounting::ExitTraceLock(size_t index)
{
    TraceLockSlot(index).store(0, std::memory_order_release);
}

LxrRcResult LxrReferenceCounting::QueueTrace(
    const uintptr_t* objects, size_t count, TraceScan* scan, bool logged)
{
    Work* work = NewWork(scan == nullptr ? WorkKind::TraceObjects : WorkKind::TraceScan,
        objects, count, LxrWorkBucketStage::ConcurrentResumable, true, scan, false, logged);
    if (work == nullptr)
    {
        return GetFailure();
    }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    if (work->watchedReader && m_reader_held.load(std::memory_order_acquire))
    {
        return LxrRcResult::Success;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    if (GetTracePhase() != LxrTracePhase::Concurrent ||
        GetPhase() != LxrRcPhase::Mutator || m_hold_trace.load(std::memory_order_acquire))
    {
        return LxrRcResult::Success;
    }
    bool expected = false;
    if (!work->traceSubmitted.compare_exchange_strong(expected, true, std::memory_order_acq_rel))
    {
        return LxrRcResult::Success;
    }
    LxrWorkSchedulerResult result = m_scheduler.Submit(&work->packet);
    if (result == LxrWorkSchedulerResult::StageDisabled ||
        result == LxrWorkSchedulerResult::Busy || result == LxrWorkSchedulerResult::InvalidState)
    {
        work->traceSubmitted.store(false, std::memory_order_release);
        return LxrRcResult::Success;
    }
    return FromScheduler(result);
}

LxrRcResult LxrReferenceCounting::PublishTraceWork()
{
    if (m_hold_trace.load(std::memory_order_acquire))
    {
        return LxrRcResult::Success;
    }
    LxrRcResult enabled = FromScheduler(m_scheduler.EnableConcurrentStage(
        LxrWorkBucketStage::ConcurrentResumable));
    if (enabled != LxrRcResult::Success)
    {
        return enabled;
    }
    for (Work* work = m_work.load(std::memory_order_acquire); work != nullptr; work = work->next)
    {
        if ((work->kind != WorkKind::TraceObjects && work->kind != WorkKind::TraceScan) ||
            work->traceProcessed.load(std::memory_order_acquire))
        {
            continue;
        }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        if (work->watchedReader && m_reader_held.load(std::memory_order_acquire))
        {
            continue;
        }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        bool expected = false;
        if (work->traceSubmitted.compare_exchange_strong(expected, true, std::memory_order_acq_rel))
        {
            LxrRcResult result = FromScheduler(m_scheduler.Submit(&work->packet));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ProcessTraceWork(Work* work)
{
    return GetTracePhase() == LxrTracePhase::Full
        ? ProcessFullTraceWork(work) : ProcessTraceWork(work, nullptr);
}

NOINLINE LxrRcResult LxrReferenceCounting::ProcessFullTraceWork(Work* work)
{
    TraceBatch targets(this);
    return GetFailure() == LxrRcResult::Success ? ProcessTraceWork(work, &targets) : GetFailure();
}

LxrRcResult LxrReferenceCounting::ProcessTraceWork(Work* work, TraceBatch* targets)
{
    bool expected = false;
    if (!work->traceProcessed.compare_exchange_strong(expected, true, std::memory_order_acq_rel))
    {
        return LxrRcResult::Success;
    }
    LxrRcResult result = LxrRcResult::Success;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    if (work->watchedReader)
    {
        m_reader_consumed.store(work->references[0], std::memory_order_release);
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    if (work->scan != nullptr)
    {
        result = ProcessTraceScan(work->scan, false, targets);
    }
    else
    {
        size_t directBudget = BufferCapacity;
        for (size_t index = 0; index < work->packet.GetReferenceCount(); index++)
        {
            if (targets != nullptr)
            {
                // Full batches join separate object packets, not their per-object scan limits.
                directBudget = BufferCapacity;
            }
            result = ProcessTraceObject(
                reinterpret_cast<Object*>(work->references[index]), &directBudget, targets);
            if (result != LxrRcResult::Success)
            {
                break;
            }
        }
    }
    if (result == LxrRcResult::Success && targets != nullptr)
    {
        result = targets->Flush();
    }
    uint64_t previous = m_trace_pending.fetch_sub(1, std::memory_order_acq_rel);
    if (previous == 0)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    if (work->watchedReader && result == LxrRcResult::Success)
    {
        m_reader_completions.fetch_add(1, std::memory_order_release);
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    return result;
}

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
LxrRcResult LxrReferenceCounting::ValidateTraceBatchForTest(Object* object, size_t count)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Inactive ||
        m_trace_pending.load() != 0 || m_work.load() != nullptr ||
        GetObjectExtent(object) == 0 || count == 0 || count > FullTraceBatchCapacity + 1)
    {
        return LxrRcResult::InvalidPhase;
    }
    if (!CommitTracePlanes())
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
    {
        TraceBatch targets(this);
        if (m_trace_pending.load() != 1)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
        for (size_t index = 0; index < count; index++)
        {
            LxrRcResult result = targets.Append(reinterpret_cast<uintptr_t>(object));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        if (m_trace_pending.load() == 0)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
        LxrRcResult result = targets.Flush();
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    if (GetFailure() != LxrRcResult::Success)
    {
        return GetFailure();
    }
    LxrRcResult result = DrainTraceInsidePause();
    if (result == LxrRcResult::Success && m_trace_pending.load() != 0)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    DeleteWork();
    return result;
}

LxrRcResult LxrReferenceCounting::WatchRememberedSlot(Object** slot)
{
    if (GetPhase() != LxrRcPhase::Mutator || slot == nullptr)
    {
        return LxrRcResult::InvalidPhase;
    }
    m_watched_remembered_slot.store(reinterpret_cast<uintptr_t>(slot), std::memory_order_release);
    m_remembered_payload_reads.store(0, std::memory_order_release);
    return LxrRcResult::Success;
}

uint64_t LxrReferenceCounting::GetRememberedPayloadReads() const
{
    return m_remembered_payload_reads.load(std::memory_order_acquire);
}

uintptr_t LxrReferenceCounting::GetMatureScanReceipt(size_t field) const
{
    return field < 4 ? m_mature_scan_receipt[field].load(std::memory_order_acquire) : UINTPTR_MAX;
}

LxrRcResult LxrReferenceCounting::HoldTracePacketsForTest(bool hold)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Concurrent)
    {
        return LxrRcResult::InvalidPhase;
    }
    m_hold_trace.store(hold, std::memory_order_release);
    return LxrRcResult::Success;
}

size_t LxrReferenceCounting::GetCompletedScanUsers(Object* object) const
{
    for (TraceScan* scan = m_scan_work.load(std::memory_order_acquire); scan != nullptr; scan = scan->next)
    {
        if (scan->object == object && scan->complete.load(std::memory_order_acquire))
        {
            return scan->users.load(std::memory_order_acquire);
        }
    }
    return 0;
}

LxrRcResult LxrReferenceCounting::ArmPendingReader(Object* object)
{
    uintptr_t count;
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Concurrent ||
        m_reader_target.load(std::memory_order_acquire) != 0 ||
        LoadCount(object, &count) != LxrRcResult::Success ||
        (count != 0 && !(m_configuration.evacuateMature && IsMatureCandidate(Start(object)))))
    {
        return LxrRcResult::InvalidPhase;
    }
    m_reader_target.store(reinterpret_cast<uintptr_t>(object), std::memory_order_release);
    m_reader_held.store(true, std::memory_order_release);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReleasePendingReader()
{
    if (m_reader_work.load(std::memory_order_acquire) == nullptr ||
        GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Concurrent)
    {
        return LxrRcResult::InvalidPhase;
    }
    m_reader_held.store(false, std::memory_order_release);
    m_reader_work.store(nullptr, std::memory_order_release);
    return PublishTraceWork();
}

uintptr_t LxrReferenceCounting::GetPendingReaderValue() const
{
    Work* work = m_reader_work.load(std::memory_order_acquire);
    return work == nullptr ? 0 : work->references[0];
}

uintptr_t LxrReferenceCounting::GetPendingReaderInitialValue() const
{
    return m_reader_initial.load(std::memory_order_acquire);
}

uintptr_t LxrReferenceCounting::GetPendingReaderConsumedValue() const
{
    return m_reader_consumed.load(std::memory_order_acquire);
}

uint64_t LxrReferenceCounting::GetPendingReaderId() const
{
    return m_reader_id.load(std::memory_order_acquire);
}

uint64_t LxrReferenceCounting::GetPendingReaderCompletions() const
{
    return m_reader_completions.load(std::memory_order_acquire);
}
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

LxrRcResult LxrReferenceCounting::ProcessTraceObject(
    Object* object, size_t* directBudget, TraceBatch* targets)
{
    object = ResolveForwarded(object);
    if (object == nullptr)
    {
        return LxrRcResult::Success;
    }
    uintptr_t start = Start(object);
    if (!OwnsAddress(start))
    {
        return m_object_model.isImmortal != nullptr &&
            m_object_model.isImmortal(object, m_object_model.context)
            ? LxrRcResult::Success : Fail(LxrRcResult::UnsupportedLifetime);
    }
    if (start % sizeof(uintptr_t) != 0)
    {
        return Fail(LxrRcResult::InvalidArgument);
    }
    if (m_configuration.evacuateMature)
    {
        LxrRcResult copied = TryCopyMature(&object);
        if (copied != LxrRcResult::Success)
        {
            return copied;
        }
        start = Start(object);
    }
    size_t index = (start - GetReservationStart()) / sizeof(uintptr_t);
    EnterTraceLock(index);
    uintptr_t count = 0;
    uintptr_t mark = 0;
    LxrRcResult result = FromMetadata(m_metadata.Load(LxrSideMetadataKind::ReferenceCount,
        start, SideMetadataMemoryOrder::SequentiallyConsistent, &count));
    if (result == LxrRcResult::Success)
    {
        result = FromMetadata(m_metadata.Load(LxrSideMetadataKind::ObjectMark,
            start, SideMetadataMemoryOrder::SequentiallyConsistent, &mark));
    }
    if (result != LxrRcResult::Success || count == 0 || mark != 0 ||
        ExtentSlot(index).load(std::memory_order_acquire) == 0)
    {
        ExitTraceLock(index);
        return result;
    }
    Object* loader = GetLoaderAllocator(object);
    size_t maximumReferences = object->GetGCSafeMethodTable()->ContainsGCPointers()
        ? ExtentSlot(index).load(std::memory_order_acquire) / sizeof(uintptr_t) + 1
        : (loader == nullptr ? 0 : 1);
    if (maximumReferences <= *directBudget)
    {
        result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::ObjectMark,
            start, 1, SideMetadataMemoryOrder::SequentiallyConsistent));
        if (result == LxrRcResult::Success)
        {
            m_trace_metrics[TracedObjects].fetch_add(1, std::memory_order_relaxed);
            TraceScan local(object, loader);
            result = ScanTraceLocked(&local, true, targets);
            *directBudget -= maximumReferences;
            if (GetPhase() == LxrRcPhase::Mutator && GetTracePhase() == LxrTracePhase::Concurrent)
            {
                m_trace_metrics[ConcurrentSlices].fetch_add(1, std::memory_order_relaxed);
            }
        }
        ExitTraceLock(index);
        return result;
    }
    TraceScan* scan = new (std::nothrow) TraceScan(object, loader);
    if (scan == nullptr)
    {
        ExitTraceLock(index);
        return Fail(LxrRcResult::OutOfMemory);
    }
    if (m_storage_diagnostics.load(std::memory_order_relaxed))
    {
        m_storage_metrics[4].fetch_add(1, std::memory_order_relaxed);
        uint64_t current = m_storage_metrics[5].fetch_add(sizeof(TraceScan), std::memory_order_relaxed) +
            sizeof(TraceScan);
        uint64_t high = m_storage_metrics[6].load(std::memory_order_relaxed);
        while (high < current && !m_storage_metrics[6].compare_exchange_weak(
            high, current, std::memory_order_relaxed))
        {
        }
    }
    TraceScan* head = m_scan_work.load(std::memory_order_relaxed);
    do
    {
        scan->next = head;
    } while (!m_scan_work.compare_exchange_weak(
        head, scan, std::memory_order_release, std::memory_order_relaxed));
    TraceScanSlot(index) = scan;
    result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::ObjectMark,
        start, 1, SideMetadataMemoryOrder::SequentiallyConsistent));
    if (result == LxrRcResult::Success)
    {
        m_trace_metrics[TracedObjects].fetch_add(1, std::memory_order_relaxed);
        result = QueueTrace(nullptr, 0, scan);
    }
    ExitTraceLock(index);
    return result;
}

LxrRcResult LxrReferenceCounting::ScanTraceLocked(TraceScan* scan, bool finish, TraceBatch* targets)
{
    CostScope cost(this, 8);
    uintptr_t children[BufferCapacity];
    size_t count = 0;
    size_t visited = 0;
    auto append = [&](uintptr_t target)
    {
        if (targets != nullptr)
        {
            return targets->Append(target);
        }
        children[count++] = target;
        if (count != BufferCapacity)
        {
            return LxrRcResult::Success;
        }
        LxrRcResult result = QueueTrace(children, count);
        count = 0;
        return result;
    };
    while (!scan->complete)
    {
        if (scan->cursor == scan->range.count)
        {
            scan->cursor = 0;
            if (!scan->iterator.Next(&scan->range))
            {
                if (scan->loaderAllocator != nullptr)
                {
                    LxrRcResult result = append(reinterpret_cast<uintptr_t>(scan->loaderAllocator));
                    if (result != LxrRcResult::Success)
                    {
                        return result;
                    }
                    scan->loaderAllocator = nullptr;
                }
                scan->complete = true;
                break;
            }
        }
        Object** slot = scan->range.start + scan->cursor++;
        Object* child = Interlocked::CompareExchangePointer(slot, static_cast<Object*>(nullptr), nullptr);
        visited++;
        m_trace_metrics[TracedReferences].fetch_add(1, std::memory_order_relaxed);
        if (child != nullptr)
        {
            if (m_configuration.evacuateMature)
            {
                LxrRcResult result = RememberMatureSlot(slot, child);
                if (result == LxrRcResult::Success)
                {
                    result = TryCopyMature(&child);
                }
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
                if (GetTracePhase() == LxrTracePhase::Full || GetTracePhase() == LxrTracePhase::FinalMark)
                {
                    *slot = child;
                }
            }
            LxrRcResult result = append(reinterpret_cast<uintptr_t>(child));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        if (!finish && visited == BufferCapacity)
        {
            break;
        }
    }
    if (count != 0)
    {
        LxrRcResult result = QueueTrace(children, count);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ProcessTraceScan(TraceScan* scan, bool finish, TraceBatch* targets)
{
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_MATURE_DEAD_SCAN_CONTROL)
    if (scan->complete.load(std::memory_order_acquire))
    {
        return LxrRcResult::Success;
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_MATURE_DEAD_SCAN_CONTROL
    Object* source = scan->object;
    LxrRcResult copied = TryCopyMature(&scan->object);
    if (copied != LxrRcResult::Success)
    {
        return copied;
    }
    if (source != scan->object)
    {
        uintptr_t destination = reinterpret_cast<uintptr_t>(scan->object);
        copied = targets == nullptr ? QueueTrace(&destination, 1) : targets->Append(destination);
        if (copied != LxrRcResult::Success)
        {
            return copied;
        }
    }
    size_t index = (Start(scan->object) - GetReservationStart()) / sizeof(uintptr_t);
    EnterTraceLock(index);
    LxrRcResult result = scan->complete ? LxrRcResult::Success : ScanTraceLocked(scan, finish, targets);
    bool complete = scan->complete;
    ExitTraceLock(index);
    if (!finish && GetPhase() == LxrRcPhase::Mutator && GetTracePhase() == LxrTracePhase::Concurrent)
    {
        m_trace_metrics[ConcurrentSlices].fetch_add(1, std::memory_order_relaxed);
        uintptr_t target = reinterpret_cast<uintptr_t>(scan->object);
        if (!complete && m_scan_checkpoint.load(std::memory_order_acquire) == target)
        {
            // Stop after a real slice, outside the object lock. A pause releases the checkpoint before quiescing workers.
            m_scan_checkpoint_active.store(true, std::memory_order_release);
            while (m_scan_checkpoint.load(std::memory_order_acquire) == target &&
                GetPhase() == LxrRcPhase::Mutator)
            {
                GCToOSInterface::YieldThread(0);
            }
            m_scan_checkpoint_active.store(false, std::memory_order_release);
        }
    }
    if (result == LxrRcResult::Success && !complete)
    {
        m_trace_metrics[SplitPackets].fetch_add(1, std::memory_order_relaxed);
        result = QueueTrace(nullptr, 0, scan);
    }
    return result;
}

LxrRcResult LxrReferenceCounting::FinishDyingTraceObject(Object* object)
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_TRACE_SKIP_DELETION_CONTROL)
    return LxrRcResult::Success;
#else
    uintptr_t start = Start(object);
    size_t index = (start - GetReservationStart()) / sizeof(uintptr_t);
    EnterTraceLock(index);
    uintptr_t mark;
    LxrRcResult result = FromMetadata(m_metadata.Load(LxrSideMetadataKind::ObjectMark,
        start, SideMetadataMemoryOrder::SequentiallyConsistent, &mark));
    if (result == LxrRcResult::Success && mark == 0)
    {
        result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::ObjectMark,
            start, 1, SideMetadataMemoryOrder::SequentiallyConsistent));
        m_trace_metrics[DeletionMarks].fetch_add(1, std::memory_order_relaxed);
    }
    TraceScan* scan = TraceScanSlot(index);
    if (result == LxrRcResult::Success && scan != nullptr && !scan->complete)
    {
        m_trace_metrics[ForcedScans].fetch_add(1, std::memory_order_relaxed);
        result = ScanTraceLocked(scan, true);
    }
    ExitTraceLock(index);
    // ScanDecrements copies every child into trace work before publishing zero.
    return result;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_TRACE_SKIP_DELETION_CONTROL
}

LxrRcResult LxrReferenceCounting::DrainTraceInsidePause()
{
    while (m_trace_pending.load(std::memory_order_acquire) != 0)
    {
        bool progressed = false;
        Work* batch = m_trace_todo.exchange(nullptr, std::memory_order_acquire);
        for (Work* work = batch; work != nullptr; work = work->traceNext)
        {
            if ((work->kind == WorkKind::TraceObjects || work->kind == WorkKind::TraceScan) &&
                !work->traceProcessed.load(std::memory_order_acquire))
            {
                LxrRcResult result = ProcessTraceWork(work);
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
                progressed = true;
            }
        }
        if (!progressed && m_trace_pending.load(std::memory_order_acquire) != 0)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::PrepareTrace()
{
    CostScope cost(this, 7);
    uintptr_t roots[BufferCapacity];
    size_t count = 0;
    for (size_t offset = 0; offset < m_root_counts[m_current_roots]; offset++)
    {
        uintptr_t root = m_roots[m_current_roots][offset];
        if ((root & 1) == 0)
        {
            roots[count++] = root;
            if (count == BufferCapacity)
            {
                LxrRcResult result = QueueTrace(roots, count);
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
                count = 0;
            }
        }
    }
    if (count != 0)
    {
        LxrRcResult result = QueueTrace(roots, count);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    return m_pause_kind == LxrPauseKind::InitialMark
        ? LxrRcResult::Success : DrainTraceInsidePause();
}

LxrRcResult LxrReferenceCounting::TraceRoot(Object* object)
{
    if (GetPhase() != LxrRcPhase::Suspended ||
        (GetTracePhase() != LxrTracePhase::Full && GetTracePhase() != LxrTracePhase::FinalMark))
    {
        return LxrRcResult::InvalidPhase;
    }
    uintptr_t root = reinterpret_cast<uintptr_t>(object);
    LxrRcResult result = QueueTrace(&root, 1);
    return result == LxrRcResult::Success ? DrainTraceInsidePause() : result;
}

bool LxrReferenceCounting::IsMarked(Object* object) const
{
    object = ResolveForwarded(object);
    if (object == nullptr || !OwnsAddress(Start(object)) || Start(object) % sizeof(uintptr_t) != 0)
    {
        return false;
    }
    uintptr_t mark;
    return GetObjectExtent(object) != 0 &&
        m_metadata.Load(LxrSideMetadataKind::ObjectMark, Start(object),
            SideMetadataMemoryOrder::SequentiallyConsistent, &mark) == SideMetadataResult::Success &&
        mark != 0;
}

LxrRcResult LxrReferenceCounting::SweepCycles()
{
    if (m_trace_pending.load(std::memory_order_acquire) != 0)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    // Root packets have drained and the next pause cannot start before this generation retires.
    // Remove leases before a swept address can become a different allocation.
    for (size_t bank = 0; bank < 2; bank++)
    {
        for (size_t index = 0; index < m_root_counts[bank]; index++)
        {
            uintptr_t reference = m_roots[bank][index] & ~static_cast<uintptr_t>(1);
            if (reference != 0 && !IsMarked(reinterpret_cast<Object*>(reference)))
            {
                m_roots[bank][index] = 0;
            }
        }
    }
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOccupiedSlot(0); index < slots;
         index = FindNextOccupiedSlot(index + 1))
    {
        size_t size = ExtentSlot(index).load(std::memory_order_acquire);
        if (size == 0)
        {
            continue;
        }
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        if (ResolveForwarded(Reference(start)) != Reference(start))
        {
            continue;
        }
        uintptr_t count = 0;
        uintptr_t mark = 0;
        LxrRcResult result = FromMetadata(m_metadata.Load(LxrSideMetadataKind::ReferenceCount,
            start, SideMetadataMemoryOrder::SequentiallyConsistent, &count));
        if (result == LxrRcResult::Success)
        {
            result = FromMetadata(m_metadata.Load(LxrSideMetadataKind::ObjectMark,
                start, SideMetadataMemoryOrder::SequentiallyConsistent, &mark));
        }
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (count == 0 || mark != 0)
        {
            continue;
        }
        if (IsImmix(start))
        {
            result = FromMetadata(m_counts.ClearStraddleLinesQuiescent(start, size));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            size_t block = (start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes;
            m_mature_candidates[block].store(1, std::memory_order_release);
        }
        else
        {
            size_t dead = m_dead_large_count.fetch_add(1, std::memory_order_relaxed);
            if (dead >= m_configuration.largeObjectBytes / LxrLargeObjectSpace::PageBytes)
            {
                return Fail(LxrRcResult::CapacityExceeded);
            }
            m_dead_large[dead] = start;
        }
        result = Reclaim(start);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        result = FromMetadata(m_metadata.Store(LxrSideMetadataKind::ReferenceCount,
            start, 0, SideMetadataMemoryOrder::SequentiallyConsistent));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        m_trace_metrics[CycleObjects].fetch_add(1, std::memory_order_relaxed);
        m_trace_metrics[CycleBytes].fetch_add(size, std::memory_order_relaxed);
    }
    return LxrRcResult::Success;
}

LxrTracePhase LxrReferenceCounting::GetTracePhase() const
{
    return m_trace_phase.load(std::memory_order_acquire);
}

bool LxrReferenceCounting::IsTraceExhausted() const
{
    return GetFailure() == LxrRcResult::Success && GetTracePhase() == LxrTracePhase::Concurrent &&
        m_trace_pending.load(std::memory_order_acquire) == 0;
}

LxrRcResult LxrReferenceCounting::HoldTrace(bool hold)
{
    if (GetPhase() != LxrRcPhase::Mutator || (hold && GetTracePhase() != LxrTracePhase::Inactive))
    {
        return LxrRcResult::InvalidPhase;
    }
    m_hold_trace.store(hold, std::memory_order_release);
    return !hold && GetTracePhase() == LxrTracePhase::Concurrent
        ? PublishTraceWork() : LxrRcResult::Success;
}

LxrTraceMetrics LxrReferenceCounting::GetTraceMetrics() const
{
    return {
        m_trace_metrics[InitialMarks].load(), m_trace_metrics[FinalMarks].load(),
        m_trace_metrics[FullMarks].load(), m_trace_metrics[TracedObjects].load(),
        m_trace_metrics[TracedReferences].load(), m_trace_metrics[SatbEntries].load(),
        m_trace_metrics[DeletionMarks].load(), m_trace_metrics[ForcedScans].load(),
        m_trace_metrics[SplitPackets].load(), m_trace_metrics[CycleObjects].load(),
        m_trace_metrics[CycleBytes].load(), m_trace_metrics[Interruptions].load(),
        m_trace_metrics[ConcurrentSlices].load(), m_trace_metrics[CheckpointInterruptions].load(),
    };
}

LxrRcResult LxrReferenceCounting::ArmScanCheckpoint(Object* object)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Inactive ||
        GetObjectExtent(object) == 0 || m_scan_checkpoint_active.load(std::memory_order_acquire))
    {
        return LxrRcResult::InvalidPhase;
    }
    m_scan_checkpoint.store(reinterpret_cast<uintptr_t>(object), std::memory_order_release);
    return LxrRcResult::Success;
}

bool LxrReferenceCounting::IsScanCheckpointActive() const
{
    return m_scan_checkpoint_active.load(std::memory_order_acquire);
}

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
void LxrReferenceCounting::HoldLogReaderForTest(bool hold)
{
    m_log_reader_hold.store(hold, std::memory_order_release);
}

bool LxrReferenceCounting::IsLogReaderHeldForTest() const
{
    return m_log_reader_active.load(std::memory_order_acquire);
}

LxrRcResult LxrReferenceCounting::ValidateLogStorageForTest(uint8_t test)
{
    if (GetPhase() != LxrRcPhase::Mutator || m_work.load() != nullptr)
    {
        return LxrRcResult::InvalidPhase;
    }
    LogBank& bank = m_banks[m_producer_bank.load()];
    LogStream& stream = bank.increments;
    if (test == 1)
    {
        stream.reserved.store(1);
        size_t count;
        return ReadPublishedRange(stream, 0, 1, &count);
    }
    uintptr_t values[BufferCapacity];
    for (size_t index = 0; index < BufferCapacity; index++)
    {
        values[index] = LxrSideMetadataLayout::AddressLimit - (index + 1) * sizeof(uintptr_t);
    }
    values[1] = 0;
    if (test == 2 || test == 6)
    {
        values[1] = UINTPTR_MAX;
        LxrRcResult result = PublishReferences(stream, stream.capacity, values, 2);
        if (result != LxrRcResult::Success || test == 2)
        {
            return result;
        }
        size_t count;
        return ReadPublishedRange(stream, 0, 2, &count) == LxrRcResult::Success &&
            count == 2 && stream.references[0] == values[0] && stream.references[1] == values[1]
            ? ResetBank(bank) : LxrRcResult::InvariantViolation;
    }
    if (test == 3 || test == 4)
    {
        size_t count = test == 3 ? BufferCapacity + 1 : 2;
        stream.references[0] = static_cast<uintptr_t>(count) << LogCountShift;
        stream.reserved.store(test == 3 ? count : 1);
        return ReadPublishedRange(stream, 0, stream.reserved.load(), &count);
    }
    if (m_observer)
    {
        return LxrRcResult::InvalidArgument;
    }
    for (size_t width = 1; width <= BufferCapacity; width++)
    {
        values[0] = width % 2 == 0 ? LxrSideMetadataLayout::AddressLimit - sizeof(uintptr_t) : 0;
        if (PublishReferences(stream, stream.capacity, values, width) != LxrRcResult::Success)
        {
            return LxrRcResult::InvariantViolation;
        }
        size_t count;
        if (ReadPublishedRange(stream, 0, width, &count) != LxrRcResult::Success || count != width)
        {
            return LxrRcResult::InvariantViolation;
        }
        Work borrowed(this, 1, WorkKind::Slots, stream.references, count, LxrRCProcessIncs,
            true, nullptr, true, false);
        Work copied(this, 2, WorkKind::Satb, stream.references, count,
            LxrWorkBucketStage::FinishConcurrentWork, true, nullptr, false, true);
        if (copied.ownedReferences == nullptr)
        {
            return LxrRcResult::OutOfMemory;
        }
        for (size_t index = 0; index < count; index++)
        {
            if (borrowed.GetReference(index) != values[index] || copied.GetReference(index) != values[index])
            {
                return LxrRcResult::InvariantViolation;
            }
        }
        if (ResetBank(bank) != LxrRcResult::Success || stream.reserved.load() != 0 ||
            stream.references[0] != values[0])
        {
            return LxrRcResult::InvariantViolation;
        }
    }
    // Every possible entry can be a separately published partial flush.
    for (size_t index = 0; index < stream.capacity; index++)
    {
        if (PublishReferences(stream, stream.capacity, values, 1) != LxrRcResult::Success)
        {
            return LxrRcResult::InvariantViolation;
        }
    }
    for (size_t index = 0; index < stream.capacity; index++)
    {
        size_t count;
        if (ReadPublishedRange(stream, index, stream.capacity, &count) != LxrRcResult::Success ||
            count != 1 || (stream.references[index] & LogReferenceMask) != values[0])
        {
            return LxrRcResult::InvariantViolation;
        }
    }
    return PublishReferences(stream, stream.capacity, values, 1);
}

LxrRcResult LxrReferenceCounting::MeasureLogCopy(size_t width, size_t* copies, uint64_t* nanoseconds)
{
    if (width == 0 || width > BufferCapacity || copies == nullptr || nanoseconds == nullptr ||
        GetPhase() != LxrRcPhase::Mutator || m_work.load() != nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    LogBank& bank = m_banks[m_producer_bank.load()];
    LogStream& stream = bank.increments;
    if (stream.reserved.load() != 0)
    {
        return LxrRcResult::InvalidPhase;
    }
    uintptr_t values[BufferCapacity];
    for (size_t index = 0; index < width; index++)
    {
        values[index] = index + 1;
    }
    *copies = bank.capacity / width < 256 ? bank.capacity / width : 256;
    std::chrono::steady_clock::time_point start = std::chrono::steady_clock::now();
    for (size_t copy = 0; copy < *copies; copy++)
    {
        LxrRcResult result = PublishReferences(stream, bank.capacity, values, width);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    *nanoseconds = std::chrono::duration_cast<std::chrono::nanoseconds>(
        std::chrono::steady_clock::now() - start).count();
    for (size_t copy = 0; copy < *copies; copy++)
    {
        size_t offset = copy * width;
        size_t count;
        if (ReadPublishedRange(stream, offset, *copies * width, &count) != LxrRcResult::Success ||
            count != width || (stream.references[offset] & LogReferenceMask) != values[0] ||
            (width > 1 && memcmp(stream.references + offset + 1, values + 1,
                (width - 1) * sizeof(uintptr_t)) != 0))
        {
            return LxrRcResult::InvariantViolation;
        }
    }
    return ResetBank(bank);
}
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
