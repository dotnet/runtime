// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcref.h"
#include "lxr_reference_counting.h"
#include "lxr_prefetch.h"

#include <new>
#include <algorithm>
#include <minipal/utils.h>
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
#include <chrono>
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
#include <thread>
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
        return minipal_hires_ticks();
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
    return minipal_hires_ticks();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
}

int64_t LxrPauseTimers::Frequency()
{
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    return 1000000000;
#else
    return minipal_hires_tick_frequency();
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
    std::atomic<bool> traceProcessed;
    std::atomic<bool> doneQueued;
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
          traceProcessed(false), doneQueued(false)
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
    // Key and chain of the index of split scans (see FindTraceScan). Changed only under the lock of the bucket.
    size_t indexSlot = SIZE_MAX;
    TraceScan* indexNext = nullptr;

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

// A copied source keeps its first field only until the copy is published, so the size of a forwarded source
// cannot be read from the source afterwards (an array or string length is that field). The copy path stores the
// size here, and RetireForwardedSources consumes the records. Only the worker that owns a list appends to it;
// the shared slot (mature copy) takes a spin lock.
struct alignas(64) LxrReferenceCounting::CopyLog
{
    struct Chunk
    {
        static constexpr size_t Capacity = 1024;
        struct Entry
        {
            uintptr_t start;
            size_t size;
        };

        Entry entries[Capacity];
        size_t count = 0;
        Chunk* next = nullptr;
    };

    Chunk* head = nullptr;
    std::atomic_flag lock = ATOMIC_FLAG_INIT;
};

namespace
{
    // P28 (F10). The copy counters are per worker, as the survival counters of the Java LXR (SurvivalRatioPredictorLocal: a local
    // copy that is summed when it is read), not three shared atomics that every worker adds to for every copied object.
    constexpr size_t CopiedObjects = Epochs + 1;
    constexpr size_t CopiedBytes = Epochs + 2;
    constexpr size_t PauseCopiedBytes = Epochs + 3;
    constexpr size_t PromotedBytes = Epochs + 4;
    static_assert(PromotedBytes < 24);
}

LxrRcMutator::LxrRcMutator()
    : engine(nullptr), state(0), identity(0), owner(0), allocator(),
      increments(nullptr), decrements(nullptr), publishedCursor(0), publishedLimit(0),
      spanStart(0), spans()
{
}

LxrReferenceCounting::LxrReferenceCounting()
    : m_configuration{}, m_object_model{}, m_observer(false), m_runtime{},
      m_capture(nullptr), m_capture_context(nullptr),
      m_layout{}, m_metadata(), m_blocks(), m_counts(), m_allocation(), m_large(),
      m_scheduler(), m_coordinator(), m_arena(nullptr),
      m_trace_locks(nullptr), m_scan_buckets(nullptr),
      m_scan_work(nullptr), m_trace_phase(LxrTracePhase::Inactive), m_marking(false),
      m_hold_trace(false), m_scan_checkpoint(0), m_scan_checkpoint_active(false),
      m_trace_pending(0), m_trace_metrics{},
      m_pause_kind(LxrPauseKind::RefCount), m_cycle_sweep(false),
      m_mutators(nullptr), m_mutator_chunks(nullptr),
      m_banks(nullptr), m_roots{nullptr, nullptr}, m_root_counts{0, 0}, m_root_capacity{0, 0},
      m_current_roots(0), m_dead_large(nullptr), m_mature_candidates(nullptr), m_dead_large_count(0),
      m_producer_bank(0), m_work(nullptr), m_trace_unsubmitted(nullptr), m_trace_done(nullptr), m_next_id(1),
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
    m_immix_floor = m_initial_immix;
    // Overshoot needs room in the reservation and no hard limit below it. With a hard limit the engine never admits
    // more than the configuration allows, so this mode keeps the reserve of the trigger and nothing else.
    m_overshoot = configuration.overshootToSpace && (configuration.evacuateNursery || configuration.evacuateMature) &&
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_S5B_IGNORE_HARD_LIMIT_CONTROL)
        m_configuration.hardLimitBytes == GetReservationBytes() &&
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_S5B_IGNORE_HARD_LIMIT_CONTROL
        m_configuration.immixBytes > m_initial_immix;
    m_overshoot_metrics[PeakCapacityBytes].store(m_initial_immix, std::memory_order_relaxed);
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
        !(m_rc_metadata = m_metadata.GetAccessor(LxrSideMetadataKind::ReferenceCount)).IsValid() ||
        !(m_mark_metadata = m_metadata.GetAccessor(LxrSideMetadataKind::ObjectMark)).IsValid() ||
        !(m_unlogged_metadata = m_metadata.GetAccessor(LxrSideMetadataKind::FieldUnlogged)).IsValid() ||
        !(m_valid_metadata = m_metadata.GetAccessor(LxrSideMetadataKind::ValidObject)).IsValid() ||
        !(m_defrag_metadata = m_metadata.GetAccessor(LxrSideMetadataKind::BlockDefrag)).IsValid() ||
        !(m_line_reuse_metadata = m_metadata.GetAccessor(LxrSideMetadataKind::LineReuse)).IsValid() ||
        !(m_page_reuse_metadata = m_metadata.GetAccessor(LxrSideMetadataKind::LargeObjectPageReuse)).IsValid() ||
        m_allocation.Initialize(&m_metadata, &m_blocks, arena, m_initial_immix,
            configuration.minimumReuseLines) !=
            SideMetadataResult::Success ||
        m_large.Initialize(&m_metadata, m_configuration.largeObjectBytes,
            reinterpret_cast<void*>(arena + m_configuration.immixBytes), m_initial_large) !=
                LxrLargeObjectResult::Success)
    {
        return Fail(LxrRcResult::MetadataFailure);
    }
    if (m_overshoot)
    {
        m_allocation.EnablePlacementQuiescent(m_immix_floor / ImmixBlockGeometry::BlockBytes);
    }
    if (m_scheduler.Initialize(configuration.workers, true, true,
            DecrementsDrained, OverallDrained, this) != LxrWorkSchedulerResult::Success)
    {
        return Fail(LxrRcResult::SchedulerFailure);
    }
    if (m_scheduler.SetCancelledPacketCallback(ReleaseCancelledPacket, this) != LxrWorkSchedulerResult::Success)
    {
        return Fail(LxrRcResult::SchedulerFailure);
    }
    if (configuration.concurrentTraceWorkers != 0 &&
        m_scheduler.ConfigureConcurrentTraceWorkers(
            configuration.concurrentTraceWorkers < configuration.workers
                ? configuration.concurrentTraceWorkers : configuration.workers) != LxrWorkSchedulerResult::Success)
    {
        return Fail(LxrRcResult::SchedulerFailure);
    }
    m_allocation.SetAcquireNotes(
        {m_allocated_block_storage.Get(), m_allocated_list, &m_allocated_count, m_mature_candidates, m_block_flags});
    for (size_t index = 0; index < configuration.maximumMutators; index++)
    {
        if (m_mutators[index].allocator.Initialize(&m_allocation, index + 1) != SideMetadataResult::Success)
        {
            return Fail(LxrRcResult::AllocationFailure);
        }
    }
    // The pinned allocator takes the owner number after every mutator and every copy allocator.
    if (m_pinned_allocator.Initialize(
            &m_allocation, configuration.maximumMutators + 2 * configuration.workers + 1) !=
        SideMetadataResult::Success)
    {
        return Fail(LxrRcResult::AllocationFailure);
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
    m_copy_logs = new (std::nothrow) CopyLog[m_configuration.workers + 1];
    if (m_copy_logs == nullptr)
    {
        return LxrRcResult::OutOfMemory;
    }
    m_mutators = new (std::nothrow) LxrRcMutator[m_configuration.maximumMutators];
    m_mutator_chunks = new (std::nothrow) LogChunk[2 * m_configuration.maximumMutators];
    m_banks = new (std::nothrow) LogBank[2];
    if (!m_observer)
    {
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        size_t objectSlots = GetObjectStorageSlots();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        if (
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
            !m_extent_storage.Reserve(objectSlots) || !m_live_storage.Reserve(slots / 64) ||
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
            !m_allocated_block_storage.Reserve(m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes) ||
            !CommitPlanes(0, m_initial_immix / sizeof(uintptr_t)) ||
            !CommitPlanes(m_configuration.immixBytes / sizeof(uintptr_t),
                m_initial_large / sizeof(uintptr_t)))
        {
            return Fail(LxrRcResult::OutOfMemory);
        }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        m_extents = m_extent_storage.Get();
        m_live_objects = m_live_storage.Get();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        m_trace_locks = new (std::nothrow) std::atomic<uint8_t>[TraceLockCount]{};
        m_scan_buckets = new (std::nothrow) TraceScanBucket[TraceScanBucketCount];
    }
    m_roots[0] = new (std::nothrow) uintptr_t[m_configuration.maximumRoots]{};
    m_roots[1] = new (std::nothrow) uintptr_t[m_configuration.maximumRoots]{};
    m_root_capacity[0] = m_configuration.maximumRoots;
    m_root_capacity[1] = m_configuration.maximumRoots;
    if (m_configuration.largeObjectBytes != 0)
    {
        m_dead_large = new (std::nothrow) uintptr_t[
            m_configuration.largeObjectBytes / LxrLargeObjectSpace::PageBytes]{};
    }
    if (m_configuration.immixBytes != 0)
    {
        m_mature_candidates = new (std::nothrow) std::atomic<uint8_t>[
            m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes]{};
        m_block_flags = new (std::nothrow) std::atomic<uint8_t>[
            m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes]{};
        // Not initialized: the entries are written before they are read, and the pages stay untouched until used.
        m_allocated_list = new (std::nothrow) uint32_t[m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes];
        if (m_allocated_list == nullptr || m_block_flags == nullptr)
        {
            return Fail(LxrRcResult::OutOfMemory);
        }
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
            (m_trace_locks == nullptr || m_scan_buckets == nullptr)) ||
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

bool LxrReferenceCounting::CommitPlanes(size_t first, size_t count, bool initialize)
{
    size_t immixSlots = m_configuration.immixBytes / sizeof(uintptr_t);
    size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    size_t objectFirst = GetObjectStorageIndex(first);
    size_t objectCount = GetObjectStorageIndex(first + count) - objectFirst;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    return (first >= immixSlots ||
            m_allocated_block_storage.Commit(first / blockSlots, count / blockSlots, initialize))
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        && m_extent_storage.Commit(objectFirst, objectCount, initialize) &&
        m_live_storage.Commit(first / 64, count / 64, initialize)
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        ;
}

bool LxrReferenceCounting::DecommitPlanes(size_t first, size_t count, bool tailIsLast)
{
    size_t immixSlots = m_configuration.immixBytes / sizeof(uintptr_t);
    size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    size_t objectFirst = GetObjectStorageIndex(first);
    size_t objectCount = GetObjectStorageIndex(first + count) - objectFirst;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    return (first >= immixSlots ||
            m_allocated_block_storage.Decommit(first / blockSlots, count / blockSlots, tailIsLast))
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        && m_extent_storage.Decommit(objectFirst, objectCount, tailIsLast) &&
        m_live_storage.Decommit(first / 64, count / 64, tailIsLast)
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        ;
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

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
std::atomic<size_t>& LxrReferenceCounting::ExtentSlot(size_t heapSlot) const
{
    return m_extents[GetObjectStorageIndex(heapSlot)];
}
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

std::atomic<uint8_t>& LxrReferenceCounting::TraceLockSlot(size_t heapSlot) const
{
    // Fibonacci hashing spreads neighbouring objects over the locks.
    size_t stripe = static_cast<size_t>(
        (static_cast<uint64_t>(heapSlot) * UINT64_C(0x9E3779B97F4A7C15)) >> (64 - 14));
    static_assert(TraceLockCount == (size_t{1} << 14), "The hash above yields 14 bits.");
    return m_trace_locks[stripe];
}

namespace
{
    template <typename Bucket>
    void LockBucket(Bucket& bucket)
    {
        while (bucket.lock.test_and_set(std::memory_order_acquire))
        {
            YieldProcessor();
        }
    }
}

// The index of the scans of split objects. A bucket lock is a leaf lock: nothing is called while it is held.
// scan->indexSlot is the key of an indexed scan and SIZE_MAX for a scan that is not in the index.
LxrReferenceCounting::TraceScan* LxrReferenceCounting::FindTraceScan(size_t heapSlot) const
{
    if (m_scan_index_count.load(std::memory_order_acquire) == 0)
    {
        return nullptr;
    }
    TraceScanBucket& bucket = m_scan_buckets[
        static_cast<size_t>((static_cast<uint64_t>(heapSlot) * UINT64_C(0x9E3779B97F4A7C15)) >> (64 - 12))];
    static_assert(TraceScanBucketCount == (size_t{1} << 12), "The hash above yields 12 bits.");
    LockBucket(bucket);
    TraceScan* found = bucket.head;
    while (found != nullptr && found->indexSlot != heapSlot)
    {
        found = found->indexNext;
    }
    bucket.lock.clear(std::memory_order_release);
    return found;
}

void LxrReferenceCounting::LinkTraceScan(size_t heapSlot, TraceScan* scan)
{
    TraceScanBucket& bucket = m_scan_buckets[
        static_cast<size_t>((static_cast<uint64_t>(heapSlot) * UINT64_C(0x9E3779B97F4A7C15)) >> (64 - 12))];
    LockBucket(bucket);
    // An entry for the same address that is still here belongs to a dead object whose scan has not been retired.
    for (TraceScan** link = &bucket.head; *link != nullptr; link = &(*link)->indexNext)
    {
        if ((*link)->indexSlot == heapSlot)
        {
            TraceScan* stale = *link;
            *link = stale->indexNext;
            stale->indexNext = nullptr;
            stale->indexSlot = SIZE_MAX;
            m_scan_index_count.fetch_sub(1, std::memory_order_acq_rel);
            break;
        }
    }
    scan->indexSlot = heapSlot;
    scan->indexNext = bucket.head;
    bucket.head = scan;
    m_scan_index_count.fetch_add(1, std::memory_order_acq_rel);
    bucket.lock.clear(std::memory_order_release);
}

void LxrReferenceCounting::UnlinkTraceScan(TraceScan* scan)
{
    size_t heapSlot = scan->indexSlot;
    if (heapSlot == SIZE_MAX)
    {
        return;
    }
    TraceScanBucket& bucket = m_scan_buckets[
        static_cast<size_t>((static_cast<uint64_t>(heapSlot) * UINT64_C(0x9E3779B97F4A7C15)) >> (64 - 12))];
    LockBucket(bucket);
    if (scan->indexSlot == heapSlot)
    {
        for (TraceScan** link = &bucket.head; *link != nullptr; link = &(*link)->indexNext)
        {
            if (*link == scan)
            {
                *link = scan->indexNext;
                scan->indexNext = nullptr;
                scan->indexSlot = SIZE_MAX;
                m_scan_index_count.fetch_sub(1, std::memory_order_acq_rel);
                break;
            }
        }
    }
    bucket.lock.clear(std::memory_order_release);
}

void LxrReferenceCounting::MoveTraceScan(size_t fromSlot, size_t toSlot, TraceScan* scan)
{
    // Both buckets are locked in address order, so a reader finds the scan under the old or the new address, never
    // under neither.
    TraceScanBucket* from = &m_scan_buckets[
        static_cast<size_t>((static_cast<uint64_t>(fromSlot) * UINT64_C(0x9E3779B97F4A7C15)) >> (64 - 12))];
    TraceScanBucket* to = &m_scan_buckets[
        static_cast<size_t>((static_cast<uint64_t>(toSlot) * UINT64_C(0x9E3779B97F4A7C15)) >> (64 - 12))];
    TraceScanBucket* first = from < to ? from : to;
    TraceScanBucket* second = from < to ? to : from;
    LockBucket(*first);
    if (second != first)
    {
        LockBucket(*second);
    }
    if (scan->indexSlot == fromSlot)
    {
        for (TraceScan** link = &from->head; *link != nullptr; link = &(*link)->indexNext)
        {
            if (*link == scan)
            {
                *link = scan->indexNext;
                break;
            }
        }
        for (TraceScan** link = &to->head; *link != nullptr; link = &(*link)->indexNext)
        {
            if ((*link)->indexSlot == toSlot)
            {
                TraceScan* stale = *link;
                *link = stale->indexNext;
                stale->indexNext = nullptr;
                stale->indexSlot = SIZE_MAX;
                m_scan_index_count.fetch_sub(1, std::memory_order_acq_rel);
                break;
            }
        }
        scan->indexSlot = toSlot;
        scan->indexNext = to->head;
        to->head = scan;
    }
    if (second != first)
    {
        second->lock.clear(std::memory_order_release);
    }
    first->lock.clear(std::memory_order_release);
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
    DeleteCopyLogs();
    delete[] m_copy_logs;
    delete[] m_worker_counters;
    delete[] m_evacuation_sources;
    delete[] m_mutator_chunks;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    m_extent_storage.Shutdown();
    m_live_storage.Shutdown();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    m_allocated_block_storage.Shutdown();
    delete[] m_roots[0];
    delete[] m_roots[1];
    delete[] m_dead_large;
    delete[] m_mature_candidates;
    delete[] m_block_flags;
    delete[] m_allocated_list;
    delete[] m_trace_locks;
    delete[] m_scan_buckets;
    m_banks = nullptr;
    m_mutators = nullptr;
    m_copy_allocators = nullptr;
    m_copy_logs = nullptr;
    m_worker_counters = nullptr;
    m_evacuation_sources = nullptr;
    m_mutator_chunks = nullptr;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    m_extents = nullptr;
    m_live_objects = nullptr;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    m_trace_locks = nullptr;
    m_scan_buckets = nullptr;
    m_block_flags = nullptr;
    m_pins.Clear();
    m_roots[0] = nullptr;
    m_roots[1] = nullptr;
    m_root_capacity[0] = 0;
    m_root_capacity[1] = 0;
    m_retained_roots.Truncate(0);
    m_released_roots.Truncate(0);
    m_dead_large = nullptr;
    m_mature_candidates = nullptr;
    m_allocated_list = nullptr;
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
    size_t cap = m_configuration.allocationBufferBytes;
    if (cap != 0 && end >= first && end - first > cap)
    {
        end = first + cap;
    }
    if (end >= first && end - first >= MIN_OBJECT_SIZE)
    {
        if (mutator->spanStart == 0)
        {
            mutator->spanStart = first;
        }
        mutator->publishedCursor = first;
        mutator->publishedLimit = end;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        mutator->lastPublishedLimit = end;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        *cursor = first;
        *limit = end;
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::CloseSpan(LxrRcMutator* mutator, uintptr_t end)
{
    uintptr_t start = mutator->spanStart;
    mutator->spanStart = 0;
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_D2_SPAN_TO_LIMIT_CONTROL)
    // Control: the span covers the unused tail of the buffer too.
    if (mutator->lastPublishedLimit > end)
    {
        end = mutator->lastPublishedLimit;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_D2_SPAN_TO_LIMIT_CONTROL
    if (start == 0 || end <= start)
    {
        return LxrRcResult::Success;
    }
    if (!mutator->spans.Push(start, end))
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
    m_span_metrics[0].fetch_add(1, std::memory_order_relaxed);
    m_span_metrics[1].fetch_add(end - start, std::memory_order_relaxed);
    m_metrics[AllocatedBytes].fetch_add(end - start, std::memory_order_relaxed);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::RetireAllocationSpan(LxrRcMutator* mutator, uintptr_t cursor)
{
    // The thread used the buffer up to cursor. Whatever lies between the used point and the end of the buffer is
    // zero memory that no object occupies. The allocator goes on from the used point, so the span stays open and
    // covers the objects of the next buffer of the same hole too.
    if (mutator == nullptr || mutator->engine != this ||
        mutator->publishedLimit == 0 || cursor < mutator->publishedCursor || cursor > mutator->publishedLimit ||
        cursor % sizeof(uintptr_t) != 0 ||
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
        candidate.spanStart = 0;
        candidate.spans.Clear();
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
    // The thread leaves with its spans. They go to the shared list until the next pause, because the slot of the
    // mutator can be taken by another thread before that.
    LxrRcResult closed = CloseSpan(mutator, mutator->allocator.GetCursor());
    if (closed != LxrRcResult::Success)
    {
        return closed;
    }
    if (mutator->spans.Size() != 0
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_D2_DETACH_DROP_CONTROL)
        && false
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_D2_DETACH_DROP_CONTROL
        )
    {
        while (m_detached_lock.test_and_set(std::memory_order_acquire))
        {
            YieldProcessor();
        }
        bool taken = m_detached_spans.Take(mutator->spans);
        m_detached_lock.clear(std::memory_order_release);
        if (!taken)
        {
            return Fail(LxrRcResult::OutOfMemory);
        }
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

bool LxrReferenceCounting::IsReturnedImmix(uintptr_t start) const
{
    // Queued work can name an object of a block that the give-back returned. The block was free, so the object
    // is dead. Its metadata reads as zero, as it does for a free block that is still admitted.
#ifdef LXR_S5B_NO_STALE_TOLERANCE_CONTROL
    return false;
#endif // LXR_S5B_NO_STALE_TOLERANCE_CONTROL
    uintptr_t arena = reinterpret_cast<uintptr_t>(m_arena);
    return m_overshoot && start >= arena && start - arena < m_configuration.immixBytes &&
        start - arena >= m_immix_capacity.load(std::memory_order_acquire);
}

bool LxrReferenceCounting::IsLarge(uintptr_t start) const
{
    uintptr_t arena = m_large.GetReservationStart();
    return start >= arena && start - arena < m_large.GetAdmittedBytes();
}

LxrRcResult LxrReferenceCounting::Allocate(
    LxrRcMutator* mutator, size_t completeSize, Object** object, bool pinned)
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
    else if (pinned
#if defined(LXR_S7_POH_NORMAL_CONTROL)
        && false
#endif // LXR_S7_POH_NORMAL_CONTROL
        )
    {
        // One allocator serves every thread, so the call takes a lock. Pinned-object-heap allocations are rare.
        while (m_pinned_lock.test_and_set(std::memory_order_acquire))
        {
            YieldProcessor();
        }
        ImmixAllocationRequest request{completeSize, sizeof(uintptr_t), 0};
        ImmixAllocationStatus status;
        SideMetadataResult result = m_pinned_allocator.TryAllocate(request, &start, &status);
        bool recycled = m_configuration.lineRecycling;
        for (size_t attempts = m_configuration.immixBytes / ImmixBlockGeometry::LineBytes +
                 m_configuration.immixBytes / ImmixBlockGeometry::BlockBytes;
             result == SideMetadataResult::Success && status != ImmixAllocationStatus::Allocated && attempts-- != 0;)
        {
            ImmixMarkingContext marking = m_marking.load(std::memory_order_acquire)
                ? ImmixMarkingContext::ConcurrentOrFinalMark : ImmixMarkingContext::Inactive;
            result = m_pinned_allocator.TryRefill(
                recycled ? ImmixAllocationSource::MutatorPinnedRecycled : ImmixAllocationSource::MutatorPinnedFresh,
                marking, &status);
            if (result == SideMetadataResult::Success && status == ImmixAllocationStatus::Exhausted && recycled)
            {
                recycled = false;
                result = m_pinned_allocator.TryRefill(ImmixAllocationSource::MutatorPinnedFresh, marking, &status);
            }
            if (result != SideMetadataResult::Success || status != ImmixAllocationStatus::Ready)
            {
                break;
            }
            result = m_pinned_allocator.TryAllocate(request, &start, &status);
        }
        m_pinned_lock.clear(std::memory_order_release);
        if (result != SideMetadataResult::Success || status != ImmixAllocationStatus::Allocated)
        {
            return result == SideMetadataResult::Success ? LxrRcResult::AllocationFailure : FromMetadata(result);
        }
        // A pinned object is allocated one by one and is found by its bit (interior pointers into it included). The
        // ranges of the block were prepared when the hole was taken, so the bit is all that is left to set.
        if (
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_D2_NO_PINNED_VALID_CONTROL)
            FromMetadata(m_counts.RegisterObjectInClearedSpan(start, completeSize)) != LxrRcResult::Success
#else
            false
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_D2_NO_PINNED_VALID_CONTROL
            )
        {
            return GetFailure();
        }
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
            // The hole is left behind: the span of its objects ends at the used point.
            if (CloseSpan(mutator, mutator->allocator.GetCursor()) != LxrRcResult::Success)
            {
                return GetFailure();
            }
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
    if (!pinned && IsImmix(start) && mutator->spanStart == 0)
    {
        mutator->spanStart = start;
    }
    if (IsLarge(start) && m_marking.load(std::memory_order_acquire) &&
        FromMetadata(m_metadata.Store(LxrSideMetadataKind::ObjectMark, start, 1,
            SideMetadataMemoryOrder::SequentiallyConsistent)) != LxrRcResult::Success)
    {
        return GetFailure();
    }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    {
        // The oracle of the test platform has the size of every object that went through this call.
        size_t index = (start - GetReservationStart()) / sizeof(uintptr_t);
        if (ExtentSlot(index).load(std::memory_order_acquire) != 0)
        {
            return Fail(LxrRcResult::InvariantViolation);
        }
        OracleBirth(start, completeSize);
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    if (m_probe_start.load(std::memory_order_acquire) == start)
    {
        m_probe_retired.store(false, std::memory_order_release);
        m_probe_births.fetch_add(1, std::memory_order_release);
    }
    m_metrics[Allocations].fetch_add(1, std::memory_order_relaxed);
    if (pinned || IsLarge(start))
    {
        // The bytes of a young object are counted when its span closes.
        m_metrics[AllocatedBytes].fetch_add(completeSize, std::memory_order_relaxed);
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
    if (kind == WorkKind::TraceObjects || kind == WorkKind::TraceScan)
    {
        // P26 (F2). A trace packet is owned by the scheduler or by m_trace_unsubmitted until it is processed. It is in no
        // list that a pause walks.
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_F2_WALK_BACKLOG_CONTROL)
        return work;
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_F2_WALK_BACKLOG_CONTROL
    }
    Work* head = m_work.load(std::memory_order_relaxed);
    do
    {
        work->next = head;
    } while (!m_work.compare_exchange_weak(
        head, work, std::memory_order_release, std::memory_order_relaxed));
    return work;
}

void LxrReferenceCounting::PushTraceUnsubmitted(Work* work)
{
    Work* head = m_trace_unsubmitted.load(std::memory_order_relaxed);
    do
    {
        work->traceNext = head;
    } while (!m_trace_unsubmitted.compare_exchange_weak(
        head, work, std::memory_order_release, std::memory_order_relaxed));
}

void LxrReferenceCounting::RetireTraceWork(Work* work)
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F2_WALK_BACKLOG_CONTROL)
    // Control: the behaviour before P26. m_work owns the trace packets, and Retire frees the finished ones by walking it.
    (void)work;
    return;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F2_WALK_BACKLOG_CONTROL
    if (work->doneQueued.exchange(true, std::memory_order_acq_rel))
    {
        Fail(LxrRcResult::InvariantViolation);
        return;
    }
    Work* head = m_trace_done.load(std::memory_order_relaxed);
    do
    {
        work->next = head;
    } while (!m_trace_done.compare_exchange_weak(
        head, work, std::memory_order_release, std::memory_order_relaxed));
}

void LxrReferenceCounting::ReleaseCancelledPacket(LxrWorkPacket* packet, void* context)
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F2_WALK_BACKLOG_CONTROL)
    (void)packet;
    (void)context; // Control: m_work owns every packet and frees it in DeleteWork.
    return;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F2_WALK_BACKLOG_CONTROL
    Work* work = static_cast<Work*>(packet->GetContext());
    if (work->kind == WorkKind::TraceObjects || work->kind == WorkKind::TraceScan)
    {
        static_cast<LxrReferenceCounting*>(context)->ReleaseWork(work);
    }
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
    Work* work = m_work.exchange(nullptr, std::memory_order_acquire);
    while (work != nullptr)
    {
        Work* next = work->next;
        ReleaseWork(work);
        work = next;
    }
    work = m_trace_done.exchange(nullptr, std::memory_order_acquire);
    while (work != nullptr)
    {
        Work* next = work->next;
        ReleaseWork(work);
        work = next;
    }
    work = m_trace_unsubmitted.exchange(nullptr, std::memory_order_acquire);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F2_WALK_BACKLOG_CONTROL)
    work = nullptr; // Control: m_work owns these packets and has freed them above.
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F2_WALK_BACKLOG_CONTROL
    while (work != nullptr)
    {
        Work* next = work->traceNext;
        ReleaseWork(work);
        work = next;
    }
    TraceScan* scan = m_scan_work.exchange(nullptr, std::memory_order_acquire);
    while (scan != nullptr)
    {
        TraceScan* next = scan->next;
        UnlinkTraceScan(scan);
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
    // P26 (F2). The trace packets that were processed since the last pause are in m_trace_done. The pending ones are
    // in the scheduler or in m_trace_unsubmitted, and this function does not touch them. So the cost follows the work
    // of the last epoch, not the trace backlog. m_work holds no trace packet.
    Work* done = m_trace_done.exchange(nullptr, std::memory_order_acquire);
    while (done != nullptr)
    {
        Work* next = done->next;
        m_walk[0].fetch_add(1, std::memory_order_relaxed);
        ReleaseWork(done);
        done = next;
    }
    Work* kept = nullptr;
    Work* work = m_work.load(std::memory_order_acquire);
    while (work != nullptr)
    {
        Work* next = work->next;
        m_walk[0].fetch_add(1, std::memory_order_relaxed);
        LxrWorkPacketState state = work->packet.GetState();
        if (state == LxrWorkPacketState::Completed
#ifdef LXR_RETIRE_PENDING_CONTROL
            || state == LxrWorkPacketState::CallerOwned
#endif // LXR_RETIRE_PENDING_CONTROL
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F2_WALK_BACKLOG_CONTROL)
            || (state == LxrWorkPacketState::CallerOwned && work->traceProcessed.load(std::memory_order_acquire))
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F2_WALK_BACKLOG_CONTROL
            )
        {
            ReleaseWork(work);
        }
        else
        {
            work->next = kept;
            kept = work;
        }
        work = next;
    }
    m_work.store(kept, std::memory_order_release);
    TraceScan* scans = nullptr;
    TraceScan* scan = m_scan_work.load(std::memory_order_acquire);
    while (scan != nullptr)
    {
        TraceScan* next = scan->next;
        if (scan->complete && scan->users.load(std::memory_order_acquire) == 0)
        {
            UnlinkTraceScan(scan);
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
            if (mutator.publishedLimit != 0)
            {
                return Fail(LxrRcResult::InvariantViolation);
            }
            // The buffer is retired, so the cursor of the allocator is the used point: the span of the thread ends there.
            LxrRcResult closed = CloseSpan(&mutator, mutator.allocator.GetCursor());
            if (closed != LxrRcResult::Success)
            {
                return closed;
            }
            if (!m_young_spans.Take(mutator.spans))
            {
                return Fail(LxrRcResult::OutOfMemory);
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
    if (!m_observer)
    {
        while (m_detached_lock.test_and_set(std::memory_order_acquire))
        {
            YieldProcessor();
        }
        bool merged = m_young_spans.Take(m_detached_spans);
        m_detached_lock.clear(std::memory_order_release);
        if (!merged)
        {
            return Fail(LxrRcResult::OutOfMemory);
        }
        m_young_spans.Sort();
        m_span_metrics[2].store(m_young_spans.Size(), std::memory_order_relaxed);
        CheckProbeInSpans();
        // The pinned allocator belongs to no mutator. It gives its block back like a mutator allocator does.
        ImmixAllocationStatus status;
        if (m_pinned_allocator.Reset(&status) != SideMetadataResult::Success ||
            status != ImmixAllocationStatus::Ready)
        {
            return Fail(LxrRcResult::AllocationFailure);
        }
    }
    if (m_storage_diagnostics.load(std::memory_order_relaxed))
    {
        m_storage_metrics[8].store(active, std::memory_order_release);
    }
    return LxrRcResult::Success;
}

void LxrReferenceCounting::MarkPromotedStart(uintptr_t start)
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_D2_NO_PROMOTE_VALID_CONTROL)
    // Control: a survivor gets no ValidObject bit.
    (void)start;
    return;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_D2_NO_PROMOTE_VALID_CONTROL
    // Only the first count of an object needs the bit. The count is in the same metadata line as the one that the
    // increment reads next, so the test is cheap.
    if (m_rc_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) == 0)
    {
        m_valid_metadata.Store(start, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
    }
}

LxrRcResult LxrReferenceCounting::FindInYoungSpans(uintptr_t address, Object** object, bool requireCounted) const
{
    const LxrSpan* span = m_young_spans.Find(address);
    if (span == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    // Every object of a span is complete: a thread cannot be suspended inside the bump-pointer helper, and the
    // pause has cut the buffers at their used point. The walk reads the size of each object from the object. A
    // source that the pause has copied has lost its first field to the forwarding pointer, so its size is read from
    // the copy, which has the same size.
    uintptr_t cursor = span->start;
    while (cursor < span->end)
    {
        Object* candidate = Reference(cursor);
        size_t size = ObjectSize(ResolveForwarded(candidate));
        if (size < MIN_OBJECT_SIZE || size > span->end - cursor)
        {
            return LxrRcResult::InvalidArgument;
        }
        if (address < cursor + size)
        {
            if (address < reinterpret_cast<uintptr_t>(candidate) ||
                (requireCounted && m_rc_metadata.Load(cursor, SideMetadataMemoryOrder::Relaxed) == 0))
            {
                return LxrRcResult::InvalidArgument;
            }
            *object = candidate;
            return LxrRcResult::Success;
        }
        cursor += size;
    }
    return LxrRcResult::InvalidArgument;
}

void LxrReferenceCounting::CheckProbeInSpans()
{
    // The probe is armed on a live object. A span that starts an object at the probed address is a birth only after
    // the probed object has retired: the object that is there before is the probed one.
    uintptr_t probe = m_probe_start.load(std::memory_order_acquire);
    const LxrSpan* span = probe == 0 || !m_probe_retired.load(std::memory_order_acquire)
        ? nullptr : m_young_spans.Find(probe);
    if (span == nullptr)
    {
        return;
    }
    uintptr_t cursor = span->start;
    while (cursor < span->end && cursor < probe)
    {
        size_t size = ObjectSize(Reference(cursor));
        if (size < MIN_OBJECT_SIZE || size > span->end - cursor)
        {
            return;
        }
        cursor += size;
    }
    if (cursor == probe)
    {
        m_probe_retired.store(false, std::memory_order_release);
        m_probe_births.fetch_add(1, std::memory_order_release);
    }
}

bool LxrReferenceCounting::IsOwnedObject(Object* object) const
{
    uintptr_t start = Start(object);
    if (!OwnsAddress(start) || start % sizeof(uintptr_t) != 0)
    {
        return false;
    }
    return !IsLarge(start) || IsObjectStart(object);
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
    for (bool retain : {true, false})
    {
        LxrWordArray& array = retain ? m_retained_roots : m_released_roots;
        for (size_t offset = 0; offset < array.Size(); offset += BufferCapacity)
        {
            size_t count = array.Size() - offset;
            if (count > BufferCapacity)
            {
                count = BufferCapacity;
            }
            LxrRcResult submitted = SubmitWork(NewWork(
                retain ? WorkKind::Roots : WorkKind::PreviousRoots, &array[offset], count,
                retain ? LxrRCProcessIncs :
                    (m_pause_kind == LxrPauseKind::Full || !m_configuration.lazyDecrements
                        ? LxrRCProcessDecs : LxrWorkBucketStage::Unconstrained)));
            if (submitted != LxrRcResult::Success)
            {
                return submitted;
            }
        }
        array.Truncate(0);
        array.Trim();
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
        engine->m_worker_counters[worker].values[PauseCopiedBytes].store(0, std::memory_order_relaxed);
    }
    for (std::atomic<uint64_t>& walk : engine->m_walk)
    {
        walk.store(0, std::memory_order_relaxed);
    }
    engine->RetireCompletedWork();
    if (engine->FlushMutators() != LxrRcResult::Success)
    {
        return;
    }
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_F9_NO_PAUSE_RESET_CONTROL)
    engine->m_allocation.ResetCursors();
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_F9_NO_PAUSE_RESET_CONTROL
    if (engine->m_grow_bytes != 0)
    {
        engine->m_growth_result = engine->GrowSuspended();
    }
    if (engine->m_overshoot)
    {
        engine->m_allocation.ResetCopyCursorQuiescent();
        if (engine->BorrowSuspended() != LxrRcResult::Success)
        {
            return;
        }
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
        engine->m_cycle_sweep.store(true, std::memory_order_release);
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
    // The root capture has found every pinned root. From here to the end of the pause the pin list does not change.
    if (engine->SealPins() != LxrRcResult::Success)
    {
        return;
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
    engine->m_young_spans.Clear();
    if (engine->m_pause_timers.Enabled())
    {
        int64_t released = engine->m_release_end_stamp.exchange(0, std::memory_order_relaxed);
        if (released != 0)
        {
            engine->m_pause_timers.Add(LxrPauseTimers::ResumeGap, engine->m_pause_timers.Stamp() - released);
        }
    }
    engine->m_phase.store(LxrRcPhase::Mutator, std::memory_order_release);
    if (engine->GetTracePhase() == LxrTracePhase::Concurrent)
    {
        LxrPhaseScope publish(engine->m_pause_timers, LxrPauseTimers::PublishTrace);
        if (engine->PublishTraceWork() != LxrRcResult::Success)
        {
            engine->Fail(LxrRcResult::SchedulerFailure);
        }
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
#ifdef LXR_S4_NO_SWEEP_WAIT_CONTROL
        drained = LxrRcResult::Success;
#else
        drained = GetTracePhase() == LxrTracePhase::Inactive
            ? Drain(timeoutMilliseconds) : DrainEpoch(timeoutMilliseconds);
#endif // LXR_S4_NO_SWEEP_WAIT_CONTROL
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        m_gate_armed.store(false, std::memory_order_release);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
    }
    return result;
}

LxrRcResult LxrReferenceCounting::AddPinnedRoot(Object* object, bool interior, size_t shard)
{
    if (GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    if (object == nullptr || m_block_flags == nullptr)
    {
        return LxrRcResult::Success;
    }
    if (interior)
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S7_PIN_INTERIOR_CONTROL)
        // Control: the address itself goes on the list, not the object that contains it.
#else
        LxrRcResult found = FindContainingObject(reinterpret_cast<uintptr_t>(object), &object);
        if (found != LxrRcResult::Success)
        {
            return found;
        }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S7_PIN_INTERIOR_CONTROL
    }
    uintptr_t start = Start(object);
    if (!m_pins.Add(shard, reinterpret_cast<uintptr_t>(object)))
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
    if (IsImmix(start))
    {
        // Large objects never move, so only an Immix block needs the pause bit.
#if !defined(LXR_S7_NO_BLOCK_BYTE_CONTROL)
        m_block_flags[(start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes].fetch_or(
            PausePinFlag, std::memory_order_release);
#endif // !LXR_S7_NO_BLOCK_BYTE_CONTROL
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::SealPins()
{
    uint64_t count = m_pins.Seal();
    if (m_pins.HasFailed())
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
    m_pause_pinned_roots.store(count, std::memory_order_release);
    return LxrRcResult::Success;
}

void LxrReferenceCounting::ClearPins()
{
    if (m_block_flags != nullptr)
    {
        for (size_t index = 0; index < m_pins.Size(); index++)
        {
            uintptr_t start = Start(reinterpret_cast<Object*>(m_pins[index]));
            if (IsImmix(start))
            {
#if !defined(LXR_S7_NO_BLOCK_CLEAR_CONTROL)
                m_block_flags[(start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes].fetch_and(
                    static_cast<uint8_t>(~PausePinFlag), std::memory_order_release);
#endif // !LXR_S7_NO_BLOCK_CLEAR_CONTROL
            }
        }
    }
    m_pins.Clear();
}

bool LxrReferenceCounting::IsPinnedObject(Object* object) const
{
#if defined(LXR_S7_IGNORE_PIN_CONTROL)
    // Control: the pause never knows a pin.
    (void)object;
    return false;
#else
    uintptr_t start = Start(object);
    if (m_block_flags == nullptr || !IsImmix(start))
    {
        return false;
    }
    uint8_t flags = m_block_flags[(start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes].load(
        std::memory_order_acquire);
    if (flags == 0)
    {
        return false;
    }
    if ((flags & ImmixAllocationManager::PinnedBlockFlag) != 0)
    {
        return true;
    }
    return (flags & PausePinFlag) != 0 && m_pins.Contains(reinterpret_cast<uintptr_t>(object));
#endif // LXR_S7_IGNORE_PIN_CONTROL
}

bool LxrReferenceCounting::IsPinnedForPause(Object* object) const
{
    return object != nullptr && IsPinnedObject(object);
}

bool LxrReferenceCounting::IsPinnedBlock(uintptr_t address) const
{
    return m_block_flags != nullptr && IsImmix(address) &&
        (m_block_flags[(address - GetReservationStart()) / ImmixBlockGeometry::BlockBytes].load(
            std::memory_order_acquire) & ImmixAllocationManager::PinnedBlockFlag) != 0;
}

size_t LxrReferenceCounting::CountPausePinnedBlocks() const
{
    size_t count = 0;
    if (m_block_flags != nullptr)
    {
        for (size_t block = 0; block < GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes; block++)
        {
            count += (m_block_flags[block].load(std::memory_order_acquire) & PausePinFlag) != 0 ? 1 : 0;
        }
    }
    return count;
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
    // A root may point to a young object, which has no ValidObject bit. The runtime reports exact roots, so the value
    // only has to lie in the owned space.
    if (!IsOwnedObject(object))
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t& count = m_root_counts[m_current_roots];
    if (count == m_root_capacity[m_current_roots])
    {
        // The roots of a pause are not limited by the initial size: a registry or a handle table can hold more.
        size_t capacity = m_root_capacity[m_current_roots];
        if (capacity >= RootGrowthLimit)
        {
            return LxrRcResult::CapacityExceeded;
        }
        capacity = capacity == 0 ? BufferCapacity : capacity * 2;
        uintptr_t* grown = new (std::nothrow) uintptr_t[capacity];
        if (grown == nullptr)
        {
            return LxrRcResult::OutOfMemory;
        }
        memcpy(grown, m_roots[m_current_roots], count * sizeof(uintptr_t));
        delete[] m_roots[m_current_roots];
        m_roots[m_current_roots] = grown;
        m_root_capacity[m_current_roots] = capacity;
    }
    m_roots[m_current_roots][count++] = reinterpret_cast<uintptr_t>(object) | (trace ? 0 : 1);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::RetainRoot(Object* object)
{
    if (GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    if (object == nullptr)
    {
        return LxrRcResult::Success;
    }
    if (!IsOwnedObject(object))
    {
        return LxrRcResult::InvalidArgument;
    }
    // The low bit tells the increment not to trace the object. Tracing is a decision of the caller.
    return m_retained_roots.Push(reinterpret_cast<uintptr_t>(object) | 1)
        ? LxrRcResult::Success : LxrRcResult::OutOfMemory;
}

LxrRcResult LxrReferenceCounting::ReleaseRoot(Object* object)
{
    if (GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    if (object == nullptr)
    {
        return LxrRcResult::Success;
    }
    return m_released_roots.Push(reinterpret_cast<uintptr_t>(object))
        ? LxrRcResult::Success : LxrRcResult::OutOfMemory;
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
    if ((!m_configuration.evacuateNursery && !m_configuration.evacuateMature) || object == nullptr)
    {
        return object;
    }
    uintptr_t start = Start(object);
    // P22 (D2). A young object has no ValidObject bit, but a source that the pause copies gets the bit before the
    // claim (TryCopyNursery) and keeps it until RetireForwardedSources. So the bit tells a source from an arbitrary
    // or stale address (the trace queue and the decrement stream can name a dead object, and the memory may now hold
    // part of a new object) without a per-object size table.
    if (!IsImmix(start) || start % sizeof(uintptr_t) != 0 ||
        m_valid_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) == 0)
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
        SumWorker(CopiedObjects), SumWorker(CopiedBytes), m_nursery_metrics[2].load(),
        m_nursery_metrics[3].load(), m_nursery_metrics[4].load(), m_nursery_metrics[5].load(),
        m_pause_copied_bytes.load() + SumWorker(PauseCopiedBytes),
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
    return m_defrag_metadata.Load(address, SideMetadataMemoryOrder::SequentiallyConsistent) == UINT8_MAX;
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
        result = FromMetadata(m_blocks.GetStateFast(block, &state, &unavailable));
        if (result != LxrRcResult::Success)
        {
            break;
        }
        if (state == ImmixBlockState::Unallocated)
        {
            bool nursery;
            result = FromMetadata(m_blocks.IsNurseryFast(block, &nursery));
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
        if (IsPinnedBlock(block))
        {
            // The objects of a pinned block never move, so it is never a source of the evacuation.
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
#if defined(LXR_S7_NO_RETAIN_CONTROL)
    // Control: a pinned mature block stays a candidate.
    return LxrRcResult::Success;
#endif // LXR_S7_NO_RETAIN_CONTROL
    if (!m_configuration.evacuateMature)
    {
        return LxrRcResult::Success;
    }
    for (size_t index = 0; index < m_pins.Size(); index++)
    {
        Object* object = reinterpret_cast<Object*>(m_pins[index]);
        uintptr_t start = Start(object);
        if (!IsMatureCandidate(start))
        {
            continue;
        }
        size_t block = (start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes;
        if ((m_evacuation_sources[block].load(std::memory_order_acquire) & RetainedSource) != 0)
        {
            continue;
        }
        // A pinned young object has no count before the increments of the pause. It is promoted in place and never
        // moves, so only a counted object keeps its block here; the call that follows the increments sees the rest.
        if (m_rc_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) == 0 ||
            ResolveForwarded(object) != object)
        {
            continue;
        }
        if ((m_evacuation_sources[block].fetch_or(RetainedSource, std::memory_order_acq_rel) &
                RetainedSource) == 0)
        {
            m_mature_metrics[2].fetch_add(1, std::memory_order_relaxed);
        }
    }
    // The old walk also kept the block of an object whose header carries BIT_SBLK_GC_RESERVE. In this collector no
    // code sets that bit (only Satori does), so no list is kept for it. The nursery copy decision still reads it.
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
    uintptr_t reuse = (IsImmix(address) ? m_line_reuse_metadata : m_page_reuse_metadata).Load(
        address, SideMetadataMemoryOrder::SequentiallyConsistent);
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
            uintptr_t reuse = (IsImmix(address) ? m_line_reuse_metadata : m_page_reuse_metadata).Load(
                address, SideMetadataMemoryOrder::SequentiallyConsistent);
            LxrRcResult result = LxrRcResult::Success;
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
                result = FromMetadata(m_blocks.GetStateFast(address & ~ImmixBlockGeometry::BlockMask,
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
            result = FindContainingObject(address, &owner, true);
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
            result = FromMetadata(m_counts.LoadFast(Start(owner), &ownerCount));
            if (result == LxrRcResult::Success)
            {
                ownerMark = m_mark_metadata.Load(Start(owner), SideMetadataMemoryOrder::SequentiallyConsistent);
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
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_MATURE_DEAD_SCAN_CONTROL)
    if (!IsObjectStart(source))
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
    uintptr_t count = m_rc_metadata.Load(Start(source), SideMetadataMemoryOrder::Relaxed);
    LxrRcResult result = LxrRcResult::Success;
    if (count == 0)
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
    // The source is claimed and its first field is intact, so the size comes from the object.
    size_t size = ObjectSize(source);
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
        result = ReserveCopyRecord(m_configuration.workers);
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    OracleBirth(copyStart, size);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    TraceScan* scan = FindTraceScan(index);
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
#if !defined(LXR_S7_SCAN_MOVE_CONTROL)
        MoveTraceScan(index, destinationIndex, scan);
#else
        (void)destinationIndex;
#endif // !LXR_S7_SCAN_MOVE_CONTROL
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        // Receipts 4 and 5: the index holds the scan under the new address and no longer under the old one.
        m_mature_scan_receipt[4].store(FindTraceScan(destinationIndex) == scan ? 1 : 0, std::memory_order_release);
        m_mature_scan_receipt[5].store(FindTraceScan(index) == scan ? 1 : 0, std::memory_order_release);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    }
    m_mark_metadata.Store(copyStart, 0, SideMetadataMemoryOrder::SequentiallyConsistent);
    uintptr_t watched = Start(source);
    m_probe_start.compare_exchange_strong(watched, copyStart, std::memory_order_acq_rel);
    CommitCopyRecord(m_configuration.workers, Start(source), size);
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
    LxrRcResult result = FromMetadata(m_counts.LoadFast(start, &count));
    if (result != LxrRcResult::Success || count != 0)
    {
        *destination = ResolveForwarded(source);
        return result;
    }
    ObjHeader* header = source->GetHeader();
    // P22 (D2). The source gets its ValidObject bit before the claim, so that every reader that sees the forwarding
    // state also sees the bit (ResolveForwarded). RetireForwardedSources clears the bit.
    MarkPromotedStart(start);
    uint32_t observed = header->CompareExchangeGCReservedBits(
        m_configuration.forwardingMask, m_configuration.forwardingShift, 2, 0);
    if (observed != 0)
    {
        *destination = ResolveForwarded(source);
        return LxrRcResult::Success;
    }
    // A different worker may have retained this source block in place before the claim.
    result = FromMetadata(m_counts.LoadFast(start, &count));
    if (result != LxrRcResult::Success || count != 0)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    bool nursery = false;
    result = FromMetadata(m_blocks.IsNurseryFast(start & ~ImmixBlockGeometry::BlockMask, &nursery));
    if (result != LxrRcResult::Success)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    size_t size = 0;
    bool copyable = nursery && !m_copy_exhausted.load(std::memory_order_relaxed) &&
        (source->GetHeader()->GetBits() & BIT_SBLK_GC_RESERVE) == 0 &&
        !IsPinnedObject(source);
    if (copyable)
    {
        // The source is claimed and its first field is intact, so the size comes from the object.
        size = ObjectSize(source);
        copyable = size < m_configuration.maximumYoungCopyBytes;
    }
    if (!copyable)
    {
        SideMetadataUpdateStatus updated;
        MarkPromotedStart(start);
        result = FromMetadata(m_counts.IncrementFast(start, previous, &updated));
        *incrementedInPlace = result == LxrRcResult::Success;
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
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
        MarkPromotedStart(start);
        result = FromMetadata(m_counts.IncrementFast(start, previous, &updated));
        *incrementedInPlace = result == LxrRcResult::Success;
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    result = ReserveCopyRecord(worker);
    if (result != LxrRcResult::Success)
    {
        header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 0);
        return result;
    }
    Object* copy = Reference(copyStart);
    {
        CostScope cost(this, 1);
        // P28 (F10). The destination is a range that a copy refill has prepared (the ValidObject plane is clear; the counts and the
        // straddle marks are zero, F9). The Java LXR does nothing to the metadata of the destination before it counts the copy
        // (rc.rs: process_inc_and_evacuate: inc(new)), so only the ValidObject bit (forced by .NET) is set; Debug builds check
        // the range as before.
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F10_COPY_NO_VALID_CONTROL)
        // Control: the copy gets no ValidObject bit.
        result = LxrRcResult::Success;
#else
        result = FromMetadata(m_counts.RegisterObjectInClearedSpan(copyStart, size));
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F10_COPY_NO_VALID_CONTROL
        SideMetadataUpdateStatus updated;
        if (result == LxrRcResult::Success)
        {
            result = FromMetadata(m_counts.IncrementFast(copyStart, previous, &updated));
        }
        if (result == LxrRcResult::Success)
        {
            result = FromMetadata(m_counts.MarkStraddleLinesFast(copyStart, size));
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    OracleBirth(copyStart, size);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    uintptr_t watched = start;
    m_probe_start.compare_exchange_strong(watched, copyStart, std::memory_order_acq_rel);
    CommitCopyRecord(worker, start, size);
    Object** forwarding = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(source) + sizeof(uintptr_t));
    Interlocked::ExchangePointer(forwarding, copy);
    // Destination RC/VO/extent/flags and the full copied header precede publication.
    header->SetGCReservedBits(m_configuration.forwardingMask, m_configuration.forwardingShift, 3);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F10_SHARED_COPY_COUNTERS_CONTROL)
    // Control: the copy counters are not counted.
#else
    CountWorker(worker, CopiedObjects, 1);
    CountWorker(worker, CopiedBytes, size);
    CountWorker(worker, PauseCopiedBytes, size);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F10_SHARED_COPY_COUNTERS_CONTROL
    *destination = copy;
    *copied = true;
    return LxrRcResult::Success;
}

// The unlog bits of the lines that an object promoted in place covers. One line of 256 bytes has 32 bits of unlog table, so one
// 32-bit store sets a line (rc.rs: scan_nursery_object). The first field is 16 bytes after the start (header and method table).
// A line that holds a young object holds no old object: holes are whole lines.
void LxrReferenceCounting::SetUnloggedLines(uintptr_t start, size_t size)
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F10_NO_INPLACE_UNLOG_CONTROL)
    (void)start;
    (void)size;
    return;
#else
    uintptr_t line = (start + (2 * sizeof(uintptr_t))) & ~ImmixBlockGeometry::LineMask;
    uintptr_t end = (start + size + ImmixBlockGeometry::LineMask) & ~ImmixBlockGeometry::LineMask;
    SideMetadataLocation location = m_unlogged_metadata.Locate(line);
    uint32_t* bits = reinterpret_cast<uint32_t*>(const_cast<uintptr_t*>(location.word)) + (location.shift >> 5);
    for (size_t index = 0; index < (end - line) / ImmixBlockGeometry::LineBytes; index++)
    {
        VolatileStoreWithoutBarrier(bits + index, 0xffffffffu);
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F10_NO_INPLACE_UNLOG_CONTROL
}

LxrRcResult LxrReferenceCounting::ProcessIncrement(
    Object** reference, bool trace, ReferenceBatch* children, size_t worker)
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
    // P28 (F10). The order of the Java LXR (rc.rs: process_inc_and_evacuate, dont_evacuate): an object that has a count is
    // not evacuated and is counted where it is, before anything else is asked of it.
    uintptr_t existing = 0;
    if (m_configuration.evacuateNursery && IsImmix(start) && object == source &&
        FromMetadata(m_counts.LoadFast(start, &existing)) == LxrRcResult::Success && existing == 0)
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
        if (IsImmix(start))
        {
            MarkPromotedStart(start);
        }
        result = FromMetadata(m_counts.IncrementFast(start, &previous, &status));
    }
    if (result == LxrRcResult::Success && trace && NeedsIncrementTrace(start))
    {
        uintptr_t target = reinterpret_cast<uintptr_t>(object);
        m_increment_traces.fetch_add(1, std::memory_order_relaxed);
        result = QueueTrace(&target, 1);
    }
    if (result != LxrRcResult::Success || previous != 0)
    {
        return result;
    }
#ifdef _DEBUG
    // P25 (F1). Black allocation, checked as in the Java LXR (rc.rs: promote, debug_assert is_marked): an object that is
    // counted for the first time inside the window of a trace was allocated or copied after the snapshot, so its mark
    // bit is set. If it is not, a trace packet is the only thing that can still mark it.
    if (IsIncrementTraceWindow() && m_mark_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) == 0)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
#endif // _DEBUG
    size_t size;
    result = GetObjectSize(object, &size, worker);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    if (IsImmix(start))
    {
        bool nursery;
        result = FromMetadata(m_blocks.IsNurseryFast(start & ~ImmixBlockGeometry::BlockMask, &nursery));
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
            if (promoted == ImmixBlockOperationStatus::Updated)
            {
                // The block stays; the lazy sweep after the pause finds out which of its lines are free.
                m_mature_candidates[(start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes].store(
                    1, std::memory_order_release);
#ifdef LXR_S4_EARLY_REUSABLE_CONTROL
                ImmixBlockOperationStatus early;
                m_blocks.SetReusable(start & ~ImmixBlockGeometry::BlockMask, 1, &early);
#endif // LXR_S4_EARLY_REUSABLE_CONTROL
            }
        }
        if (!copied)
        {
            result = FromMetadata(m_counts.MarkStraddleLinesFast(start, size));
            // P28 (F10). As in the Java LXR (rc.rs: scan_nursery_object, in_place_promotion): an object without reference fields
            // needs no unlog bits, and the bits of the others are set one line at a time with a store of 32 bits.
            if (result == LxrRcResult::Success && type->ContainsGCPointers())
            {
                SetUnloggedLines(start, size);
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
        ((object->GetHeader()->GetBits() & BIT_SBLK_GC_RESERVE) != 0 || IsPinnedObject(object)))
    {
        m_nursery_metrics[2].fetch_add(size, std::memory_order_relaxed);
    }
    // The children of a black object are traced when they have no mark: an unmarked old object can hide behind it
    // (a weak target that a program stored into the newborn). The test of the child's own mark bit keeps the common
    // case, a black child, free of any packet.
    bool childrenTrace = trace ||
        (IsIncrementTraceWindow() && m_mark_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) != 0);
    return ScanIncrements(object, childrenTrace, children, worker);
}

bool LxrReferenceCounting::IsIncrementTraceWindow() const
{
    LxrTracePhase phase = GetTracePhase();
    return phase == LxrTracePhase::Concurrent || phase == LxrTracePhase::FinalMark;
}

bool LxrReferenceCounting::NeedsIncrementTrace(uintptr_t start) const
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F1_QUEUE_ALL_CONTROL)
    // Control: the behaviour before P25 (F1). Every counted object gets a packet while a trace is active.
    (void)start;
    return GetTracePhase() != LxrTracePhase::Inactive;
#elif defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F1_SKIP_ALL_CONTROL)
    // Control: the skip is too wide. An unmarked object is skipped as well.
    (void)start;
    return false;
#else
    return IsIncrementTraceWindow() && m_mark_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) == 0;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F1_QUEUE_ALL_CONTROL
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
            // P28 (F10). As in the Java LXR (rc.rs: scan_nursery_object) a child that has a count already is counted here, from
            // its count alone, and only a child without a count (a young object) becomes a slot for the next level. The header
            // of an old child is not read. The block of the child must not be a source of the mature evacuation (a source can be
            // forwarded, and the slot would need the copy); large objects take the longer path.
            uintptr_t childStart = Start(child);
            uintptr_t childCount;
            if (IsImmix(childStart) && !IsMatureCandidate(childStart) &&
                FromMetadata(m_counts.LoadFast(childStart, &childCount)) == LxrRcResult::Success && childCount != 0)
            {
                CountWorker(worker, IncrementEntries, 1);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F10_INLINE_NO_INC_CONTROL)
                // Control: the increment of a counted child is left out.
                if (false)
#else
                if (childCount != m_counts.GetMaximumCount())
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F10_INLINE_NO_INC_CONTROL
                {
                    uintptr_t previous;
                    SideMetadataUpdateStatus status;
                    result = FromMetadata(m_counts.IncrementFast(childStart, &previous, &status));
                    if (result != LxrRcResult::Success)
                    {
                        return false;
                    }
                }
                if (trace && NeedsIncrementTrace(childStart))
                {
                    uintptr_t target = reinterpret_cast<uintptr_t>(child);
                    m_increment_traces.fetch_add(1, std::memory_order_relaxed);
                    result = QueueTrace(&target, 1);
                    if (result != LxrRcResult::Success)
                    {
                        return false;
                    }
                }
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
    // P28 (F10). As in the Java LXR (rc.rs: process_dead_object) a child whose count is zero or at the maximum gets no decrement,
    // so it is not queued. During a trace every child of a dead object belongs to the snapshot and is queued.
    const bool queueAll = m_marking.load(std::memory_order_acquire);
    const uintptr_t maximumCount = m_counts.GetMaximumCount();
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
            if (!queueAll && OwnsAddress(Start(child)))
            {
                uintptr_t childCount = m_rc_metadata.Load(Start(child), SideMetadataMemoryOrder::Relaxed);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F10_SKIP_DECS_CONTROL)
                // Control: the skip is too wide: a child with count 1 gets no decrement either.
                if (childCount <= 1 || childCount == maximumCount)
#else
                if (childCount == 0 || childCount == maximumCount)
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F10_SKIP_DECS_CONTROL
                {
                    continue;
                }
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
        SideMetadataResult result = engine->m_counts.ClearStraddleLinesFast(start, size);
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
        bool previousRoot = work->kind == WorkKind::PreviousRoots;
        if (previousRoot)
        {
            reference &= ~static_cast<uintptr_t>(1);
        }
        LxrRcResult result = ProcessDecrementEntry(reference, previousRoot, &death, worker);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        // The Java LXR prefetches the object of the entry that is 8 ahead (rc.rs: process_decs).
        LxrPrefetch::Ahead(work->references, packet->GetReferenceCount(), index, false, previousRoot ? 1 : 0);
    }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_NO_RECURSION_CONTROL)
    // Control: the behaviour before P25 (F4). The children of dead objects become a new packet.
    return children.Flush();
#else
    // P25 (F4). The children of the dead objects are processed in this packet, one level after the other, as the Java LXR
    // does (rc.rs ProcessDecs::do_work). A full buffer still becomes a new packet (ReferenceBatch::Append), so that other
    // workers can take it.
    uintptr_t objects[BufferCapacity];
    while (children.count != 0)
    {
        size_t count = children.count;
        if (m_marking.load(std::memory_order_acquire))
        {
            // The children of a dead object are part of the snapshot (SATB), as in ReferenceBatch::Flush.
            LxrRcResult queued = QueueTrace(children.references, count);
            if (queued != LxrRcResult::Success)
            {
                return queued;
            }
        }
        memcpy(objects, children.references, count * sizeof(uintptr_t));
        children.count = 0;
        for (size_t index = 0; index < count; index++)
        {
            LxrRcResult result = ProcessDecrementEntry(objects[index], false, &death, worker);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            LxrPrefetch::Ahead(objects, count, index, false);
        }
    }
    return LxrRcResult::Success;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_NO_RECURSION_CONTROL
}

LxrRcResult LxrReferenceCounting::ProcessDecrementEntry(
    uintptr_t reference, bool previousRoot, DeathContext* death, size_t worker)
{
    Object* object = reinterpret_cast<Object*>(reference);
    if (object == nullptr)
    {
        return LxrRcResult::Success;
    }
    if (!OwnsAddress(Start(object)))
    {
        if (IsReturnedImmix(Start(object)) || (m_object_model.isImmortal != nullptr &&
            m_object_model.isImmortal(object, m_object_model.context)))
        {
            return LxrRcResult::Success;
        }
        return Fail(LxrRcResult::UnsupportedLifetime);
    }
    if (m_cycle_sweep.load(std::memory_order_acquire) && !IsMarked(object))
    {
        return LxrRcResult::Success;
    }
    uintptr_t start = Start(object);
    uintptr_t count = m_rc_metadata.Load(start, SideMetadataMemoryOrder::Relaxed);
    CountWorker(worker, DecrementEntries, 1);
    if (previousRoot)
    {
        CountWorker(worker, RootDecrements, 1);
    }
    if (count == 0 || count == m_counts.GetMaximumCount())
    {
        return LxrRcResult::Success;
    }
    uintptr_t previous;
    SideMetadataUpdateStatus status;
    LxrRcResult result = FromMetadata(m_counts.DecrementWithDeathFast(
        start, Death, death, &previous, &status));
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    if (status == SideMetadataUpdateStatus::Updated && previous == 1)
    {
        CountWorker(worker, Deaths, 1);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReleaseBlockIfDead(
    uintptr_t block, size_t index, const LxrBlockReleaseGuard* guard, ImmixBlockOperationStatus* status)
{
    bool selected = IsMatureCandidate(block);
    bool copied = selected && (m_evacuation_sources[index].load(std::memory_order_acquire) & CopiedSource) != 0;
    LxrRcResult result = FromMetadata(m_blocks.TryReleaseIfRcDead(block, guard, status));
    if (result != LxrRcResult::Success || *status != ImmixBlockOperationStatus::Updated)
    {
        return result;
    }
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
        m_evacuation_sources[index].store(0, std::memory_order_release);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::RefreshBlock(uintptr_t block, size_t index, const LxrBlockReleaseGuard& guard)
{
    ImmixBlockState state;
    uint8_t unavailable;
    LxrRcResult result = FromMetadata(m_blocks.GetState(block, &state, &unavailable));
    if (result != LxrRcResult::Success || state == ImmixBlockState::Unallocated)
    {
        return result;
    }
    bool reusing;
    result = FromMetadata(m_blocks.IsReusing(block, &reusing));
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    if (reusing)
    {
        // An allocator took the block in this epoch. Its young objects have no counts yet, so the lines cannot
        // be counted before the next pause.
        m_mature_candidates[index].store(1, std::memory_order_release);
        return LxrRcResult::Success;
    }
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
    if (freeLines == 0 || freeLines >= ImmixBlockGeometry::LinesPerBlock)
    {
        return LxrRcResult::Success;
    }
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_S4_NO_REFRESH_CONTROL)
    ImmixBlockOperationStatus status;
    result = FromMetadata(m_blocks.TrySetReusableLazily(
        block, &guard, static_cast<uint8_t>(ImmixBlockGeometry::LinesPerBlock - freeLines), &status));
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    if (status == ImmixBlockOperationStatus::Contended || status == ImmixBlockOperationStatus::Stale)
    {
        m_mature_candidates[index].store(1, std::memory_order_release);
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_S4_NO_REFRESH_CONTROL
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::Sweep(const LxrBlockReleaseGuard& guard)
{
    if (m_cycle_sweep.load(std::memory_order_acquire))
    {
        LxrRcResult result = SweepCycles();
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    uintptr_t arena = reinterpret_cast<uintptr_t>(m_arena);
    size_t blocks = m_immix_capacity.load(std::memory_order_acquire) / ImmixBlockGeometry::BlockBytes;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    bool held = false;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    // Only the blocks that a death, a promotion in place, a copy or an allocator touched carry a flag. The flag is
    // taken before the block is looked at: a flag that is set again meanwhile, or by RefreshBlock for a block that
    // is not ready, stays for the next sweep.
    for (size_t candidate = 0; candidate < blocks; candidate++)
    {
        if (m_mature_candidates[candidate].load(std::memory_order_acquire) == 0)
        {
            continue;
        }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        if (!held)
        {
            held = true;
            m_sweep_holding.store(true, std::memory_order_release);
            while (m_sweep_hold.load(std::memory_order_acquire))
            {
                GCToOSInterface::YieldThread(0);
            }
            m_sweep_holding.store(false, std::memory_order_release);
        }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        m_mature_candidates[candidate].store(0, std::memory_order_release);
        uintptr_t block = arena + candidate * ImmixBlockGeometry::BlockBytes;
        ImmixBlockOperationStatus status;
        LxrRcResult result = ReleaseBlockIfDead(block, candidate, &guard, &status);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (status == ImmixBlockOperationStatus::Contended)
        {
            m_mature_candidates[candidate].store(1, std::memory_order_release);
        }
        else if (status != ImmixBlockOperationStatus::Updated)
        {
            result = RefreshBlock(block, candidate, guard);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
    }
    size_t count = m_dead_large_count.load(std::memory_order_acquire);
    for (size_t index = 0; index < count; index++)
    {
        LxrLargeObjectAllocation allocation;
        if (m_large.GetAllocation(m_dead_large[index], &allocation) != LxrLargeObjectResult::Success ||
            m_large.ReleaseAfterDecrements(allocation.object, allocation.owner, guard) !=
                LxrLargeObjectResult::Success)
        {
            return Fail(LxrRcResult::MetadataFailure);
        }
        m_metrics[LargeObjectBytesReleased].fetch_add(
            allocation.page_count * LxrLargeObjectSpace::PageBytes, std::memory_order_relaxed);
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::SweepLargeNursery()
{
    LxrRcPhase phase = GetPhase();
    m_phase.store(LxrRcPhase::Sweeping, std::memory_order_release);
#ifdef LXR_RC_SWEEP_QUERY_CONTROL
    size_t forbidden;
    return GetObjectSize(nullptr, &forbidden);
#endif
    size_t released;
    if (m_large.SweepNursery(&released) != LxrLargeObjectResult::Success)
    {
        return Fail(LxrRcResult::MetadataFailure);
    }
    m_metrics[LargeObjectBytesReleased].fetch_add(released, std::memory_order_relaxed);
    m_phase.store(phase, std::memory_order_release);
    return LxrRcResult::Success;
}

void LxrReferenceCounting::FixPacketReferences(Work* work)
{
    if (work->kind == WorkKind::Slots || work->kind == WorkKind::NurserySlots || work->ownedReferences == nullptr)
    {
        return;
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
        Object* fixedObject = ResolveForwarded(object);
        if (fixedObject != object)
        {
            m_walk[4].fetch_add(1, std::memory_order_relaxed);
        }
        work->ownedReferences[index] = reinterpret_cast<uintptr_t>(fixedObject) | flags;
    }
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
            Object* fixedRoot = ResolveForwarded(object);
            if (fixedRoot != object)
            {
                m_walk[4].fetch_add(1, std::memory_order_relaxed);
            }
            m_roots[bank][index] = reinterpret_cast<uintptr_t>(fixedRoot) | flags;
        }
    }
    // P26 (F2). The decrement log. The Java LXR resolves a forwarded target when it uses it (rc.rs: ProcessDecs, only for
    // the objects that a mature evacuation moves) and never walks the log in a pause. Here:
    //  - the log bank that the lazy decrements of the last pause have consumed is not read again;
    //  - the log of this pause holds the values that the fields had at the last pause. These are counted objects, which the
    //    nursery copy does not move (it copies objects with count 0). So a pause that copies only young objects does not
    //    walk it. Debug builds walk it and check that no entry names a source of this pause.
    //  - a pause that can copy mature objects (FinalMark, Full) rewrites it, as before.
    bool copiesMature = m_configuration.evacuateMature &&
        (m_pause_kind == LxrPauseKind::Full || m_pause_kind == LxrPauseKind::FinalMark);
    (void)copiesMature;
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F2_WALK_LOGS_CONTROL)
    // Control: the walk of both banks, as before P26.
    for (size_t bank = 0; bank < 2; bank++)
#else
    size_t bank = m_producer_bank.load(std::memory_order_relaxed);
#ifndef _DEBUG
    if (copiesMature)
#endif // !_DEBUG
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F2_WALK_LOGS_CONTROL
    {
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
                m_walk[2].fetch_add(1, std::memory_order_relaxed);
                uintptr_t value = decrements.references[index];
                uintptr_t publication = decrements.published == nullptr && index == cursor
                    ? value & ~LogReferenceMask : 0;
                Object* target = reinterpret_cast<Object*>(value & LogReferenceMask);
                Object* fixedDec = ResolveForwarded(target);
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_F2_WALK_LOGS_CONTROL)
                if (!copiesMature)
                {
                    if (fixedDec != target)
                    {
                        return Fail(LxrRcResult::InvariantViolation);
                    }
                    continue;
                }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_F2_WALK_LOGS_CONTROL
                if (fixedDec != target)
                {
                    m_walk[4].fetch_add(1, std::memory_order_relaxed);
                }
                decrements.references[index] = reinterpret_cast<uintptr_t>(fixedDec) | publication;
            }
            cursor += count;
        }
    }
    // The packets that the pause itself made are in m_work (the packets of earlier pauses are freed at Suspend) and in
    // m_trace_unsubmitted. The trace packets that wait in the scheduler are not visited: ProcessTraceObject checks the
    // address when it uses it (see there), so a packet that names a moved young source needs no rewrite.
    for (Work* work = m_work.load(std::memory_order_acquire); work != nullptr; work = work->next)
    {
        m_walk[1].fetch_add(1, std::memory_order_relaxed);
        FixPacketReferences(work);
    }
    for (Work* work = m_trace_unsubmitted.load(std::memory_order_acquire); work != nullptr; work = work->traceNext)
    {
        m_walk[1].fetch_add(1, std::memory_order_relaxed);
        FixPacketReferences(work);
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
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_MATURE_EARLY_RELEASE_CONTROL)
    for (size_t block = 0; block < GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes; block++)
    {
        if (m_evacuation_sources != nullptr &&
            (m_evacuation_sources[block].load() & RetainedSource) != 0)
        {
            if (FromMetadata(m_counts.ResetBlockQuiescent(
                    GetReservationStart() + block * ImmixBlockGeometry::BlockBytes)) != LxrRcResult::Success)
            {
                return GetFailure();
            }
        }
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_MATURE_EARLY_RELEASE_CONTROL
    // The copy path logged every forwarded source with its size, so no walk over the heap is needed.
    for (size_t slot = 0; slot <= m_configuration.workers; slot++)
    {
        CopyLog& log = m_copy_logs[slot];
        for (CopyLog::Chunk* chunk = log.head; chunk != nullptr; chunk = chunk->next)
        {
            for (size_t entry = 0; entry < chunk->count; entry++)
            {
                uintptr_t start = chunk->entries[entry].start;
                size_t size = chunk->entries[entry].size;
                uintptr_t count;
                LxrRcResult result = FromMetadata(m_counts.LoadFast(start, &count));
                bool mature = IsMatureCandidate(start);
                if (result != LxrRcResult::Success || (count != 0 && !mature))
                {
                    return Fail(LxrRcResult::InvariantViolation);
                }
                if (mature)
                {
                    result = FromMetadata(m_counts.ClearStraddleLinesFast(start, size));
                    if (result == LxrRcResult::Success)
                    {
                        m_rc_metadata.Store(start, 0, SideMetadataMemoryOrder::SequentiallyConsistent);
                        m_mature_candidates[(start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes].store(
                            1, std::memory_order_release);
                    }
                    if (result != LxrRcResult::Success)
                    {
                        return result;
                    }
                }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
                OracleRetire(start);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
                if (mature)
                {
                    m_mature_metrics[10].fetch_add(size, std::memory_order_relaxed);
                }
                else
                {
                    m_nursery_metrics[5].fetch_add(size, std::memory_order_relaxed);
                }
                m_valid_metadata.Store(start, 0, SideMetadataMemoryOrder::SequentiallyConsistent);
            }
        }
    }
    DeleteCopyLogs();
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReserveCopyRecord(size_t slot)
{
    CopyLog& log = m_copy_logs[slot];
    if (slot == m_configuration.workers)
    {
        while (log.lock.test_and_set(std::memory_order_acquire))
        {
            YieldProcessor();
        }
    }
    LxrRcResult result = LxrRcResult::Success;
    if (log.head == nullptr || log.head->count == CopyLog::Chunk::Capacity)
    {
        CopyLog::Chunk* chunk = new (std::nothrow) CopyLog::Chunk;
        if (chunk == nullptr)
        {
            result = Fail(LxrRcResult::OutOfMemory);
        }
        else
        {
            chunk->next = log.head;
            log.head = chunk;
        }
    }
    if (slot == m_configuration.workers)
    {
        log.lock.clear(std::memory_order_release);
    }
    return result;
}

void LxrReferenceCounting::CommitCopyRecord(size_t slot, uintptr_t start, size_t size)
{
    CopyLog& log = m_copy_logs[slot];
    if (slot == m_configuration.workers)
    {
        while (log.lock.test_and_set(std::memory_order_acquire))
        {
            YieldProcessor();
        }
    }
    // ReserveCopyRecord left room in the head chunk. Only one thread appends to a worker slot.
    log.head->entries[log.head->count++] = {start, size};
    if (slot == m_configuration.workers)
    {
        log.lock.clear(std::memory_order_release);
    }
}

void LxrReferenceCounting::DeleteCopyLogs()
{
    if (m_copy_logs == nullptr)
    {
        return;
    }
    for (size_t slot = 0; slot <= m_configuration.workers; slot++)
    {
        CopyLog::Chunk* chunk = m_copy_logs[slot].head;
        while (chunk != nullptr)
        {
            CopyLog::Chunk* next = chunk->next;
            delete chunk;
            chunk = next;
        }
        m_copy_logs[slot].head = nullptr;
    }
}

size_t LxrReferenceCounting::ObjectSize(Object* object)
{
    return (object->GetSize() + sizeof(uintptr_t) - 1) & ~(sizeof(uintptr_t) - 1);
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
        result = ReleaseNurseryBlocks();
    }
    if (result == LxrRcResult::Success)
    {
        LxrPhaseScope scope(m_pause_timers, LxrPauseTimers::SweepBlocks);
        result = SweepLargeNursery();
    }
    if (result == LxrRcResult::Success && m_configuration.evacuateMature &&
        (m_pause_kind == LxrPauseKind::Full || m_pause_kind == LxrPauseKind::FinalMark))
    {
        DeleteRememberedChunks();
        // The sources of the evacuation have lost their copied objects. A source that is dead now is released here,
        // as before; a source that stays loses its defrag mark.
        for (size_t block = 0; block < GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes; block++)
        {
            if (m_evacuation_sources[block].load(std::memory_order_acquire) == 0)
            {
                continue;
            }
            uintptr_t address = GetReservationStart() + block * ImmixBlockGeometry::BlockBytes;
            ImmixBlockOperationStatus released;
            result = ReleaseBlockIfDead(address, block, nullptr, &released);
            if (result != LxrRcResult::Success)
            {
                break;
            }
            if (released == ImmixBlockOperationStatus::Updated)
            {
                m_mature_candidates[block].store(0, std::memory_order_release);
                continue;
            }
            if (m_evacuation_sources[block].exchange(0, std::memory_order_acq_rel) != 0)
            {
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
    if (m_overshoot)
    {
        result = GiveBackSuspended();
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    m_current_roots ^= 1;
    m_root_counts[m_current_roots] = 0;
    ClearPins();
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
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_F9_NO_PAUSE_RESET_CONTROL)
    m_allocation.ResetCursors();
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_F9_NO_PAUSE_RESET_CONTROL
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
    if (m_pause_timers.Enabled())
    {
        m_release_end_stamp.store(m_pause_timers.Stamp(), std::memory_order_relaxed);
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    if (work->kind == WorkKind::Decrements || work->kind == WorkKind::PreviousRoots || work->kind == WorkKind::MatureSweep)
    {
        uint32_t delay = engine->m_lazy_delay_us.load(std::memory_order_acquire);
        if (delay != 0 && packet->GetStage() == LxrWorkBucketStage::Unconstrained)
        {
            std::this_thread::sleep_for(std::chrono::microseconds(delay));
        }
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    switch (work->kind)
    {
        case WorkKind::Slots:
        case WorkKind::NurserySlots:
        case WorkKind::Roots:
        case WorkKind::Targets:
        {
            engine->ProcessIncrementWork(work, worker);
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
                    result = engine->ReclaimLargeNursery();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
            // The scheduler is still busy with the packet, so the memory is freed at the next Suspend, not here.
            engine->RetireTraceWork(work);
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
            engine->Sweep(guard);
            break;
        }
    }
}

LxrRcResult LxrReferenceCounting::ProcessIncrementWork(Work* work, size_t worker)
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
            if (work->kind == WorkKind::Slots)
            {
                m_unlogged_metadata.Store(
                    reinterpret_cast<uintptr_t>(slot), 1, SideMetadataMemoryOrder::Relaxed);
            }
            CountWorker(worker, IncrementEntries, 1);
        }
        LxrRcResult result = ProcessIncrement(&object, trace, &children, worker);
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
        // The Java LXR prefetches the object of the entry that is 8 ahead (rc.rs: process_incs).
        LxrPrefetch::Ahead(
            work->references, work->packet.GetReferenceCount(), index, ownedSlot != nullptr,
            work->kind == WorkKind::Roots ? 1 : 0);
    }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_NO_RECURSION_CONTROL)
    // Control: the behaviour before P25 (F4). The children of the packet become a new packet: one packet for each level.
    return children.Flush();
#else
    return ProcessChildIncrements(&children, worker);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_NO_RECURSION_CONTROL
}

// P25 (F4). The increments of the children of the counted objects run in the packet that counted their parents, one level
// after the other (Java LXR: rc.rs ProcessIncs::do_work, the loop over new_incs). A chain of 63 young nodes costs one packet
// and no hand-off. A buffer that fills up (1 024 slots) still becomes a new packet (ReferenceBatch::Append), so a wide
// object gives work to the other workers.
LxrRcResult LxrReferenceCounting::ProcessChildIncrements(ReferenceBatch* children, size_t worker)
{
    uintptr_t slots[BufferCapacity];
    while (children->count != 0)
    {
        size_t count = children->count;
        bool trace = children->trace;
        memcpy(slots, children->references, count * sizeof(uintptr_t));
        children->count = 0;
        for (size_t index = 0; index < count; index++)
        {
            Object** slot = reinterpret_cast<Object**>(slots[index]);
            Object* object = *slot;
            CountWorker(worker, IncrementEntries, 1);
            LxrRcResult result = ProcessIncrement(&object, trace, children, worker);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            if (object != nullptr)
            {
                result = RememberMatureSlot(slot, object);
                if (result != LxrRcResult::Success)
                {
                    return result;
                }
            }
            *slot = object;
            LxrPrefetch::Ahead(slots, count, index, true);
        }
    }
    return LxrRcResult::Success;
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
    if (engine->m_cycle_sweep.load(std::memory_order_acquire))
    {
        engine->m_trace_phase.store(LxrTracePhase::Inactive, std::memory_order_release);
        // The sweep has read every dead object it needed. Only now may the runtime free type data.
        engine->m_cycle_sweep.store(false, std::memory_order_release);
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

LxrRcResult LxrReferenceCounting::FindContainingObject(uintptr_t address, Object** object, bool requireCounted) const
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
        if (address < reinterpret_cast<uintptr_t>(candidate) || !IsObjectStart(candidate))
        {
            return LxrRcResult::InvalidArgument;
        }
        *object = candidate;
        return LxrRcResult::Success;
    }
    uintptr_t lower = IsImmix(address)
        ? address & ~ImmixBlockGeometry::BlockMask : m_large.GetReservationStart();
    uintptr_t upper = lower + ImmixBlockGeometry::BlockBytes;
    if (m_young_spans.Size() != 0
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_D2_NO_SPAN_LOOKUP_CONTROL)
        && false
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_D2_NO_SPAN_LOOKUP_CONTROL
        &&
        FindInYoungSpans(address, object, requireCounted) == LxrRcResult::Success)
    {
        return LxrRcResult::Success;
    }
    uintptr_t cursor = address & ~(sizeof(uintptr_t) - 1);
    // The nearest ValidObject bit at or below the address is the start of the object that contains it. An object
    // range is cleared (ValidObject and counts) before an allocation or a copy reuses it, and only the start of a
    // promoted object is set afterwards, so no stale bit lies between the start of a live object and an address
    // inside it. The size comes from the object, which is intact: it is live. A stale bit of a dead object can be
    // the nearest bit only for an address outside every live object; the checks below turn most such addresses into
    // an error, but the result is not defined for an address that no live object contains. A young object has no
    // bit: an address inside it is found above, in the spans of the pause.
    while (cursor >= lower)
    {
        // The address is inside the admitted Immix space and the scan stays in its block.
        if (m_valid_metadata.Load(cursor, SideMetadataMemoryOrder::Relaxed) != 0)
        {
            if (requireCounted && m_rc_metadata.Load(cursor, SideMetadataMemoryOrder::Relaxed) == 0)
            {
                return LxrRcResult::InvalidArgument;
            }
            Object* candidate = Reference(cursor);
            // A forwarded source has its first field overwritten by the forwarding pointer, so an array or string
            // length is lost. The copy has the same size.
            Object* sizeSource = ResolveForwarded(candidate);
            if (sizeSource != candidate && !IsObjectStart(sizeSource))
            {
                return LxrRcResult::InvalidArgument;
            }
            size_t size = ObjectSize(sizeSource);
            if (size < MIN_OBJECT_SIZE || size > upper - cursor)
            {
                return LxrRcResult::InvalidArgument;
            }
            if (address >= reinterpret_cast<uintptr_t>(candidate) && address - cursor < size)
            {
                *object = candidate;
                return LxrRcResult::Success;
            }
            return LxrRcResult::InvalidArgument;
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    LxrRcResult result = OracleReclaim(start);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    NoteProbeReclaim(start);
    return LxrRcResult::Success;
}

void LxrReferenceCounting::NoteProbeReclaim(uintptr_t start)
{
    // The product has no per-object record of a young death, so the probe is also checked once per pause
    // (ClearAllocatedBlocks). The flag makes the two sources count one retirement per birth.
    if (m_probe_start.load(std::memory_order_acquire) == start &&
        !m_probe_retired.exchange(true, std::memory_order_acq_rel))
    {
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_PROBE_SKIP_RECLAIM_CONTROL)
        m_probe_reclaims.fetch_add(1, std::memory_order_release);
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_PROBE_SKIP_RECLAIM_CONTROL
    }
}

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
void LxrReferenceCounting::OracleBirth(uintptr_t start, size_t size)
{
    size_t index = (start - GetReservationStart()) / sizeof(uintptr_t);
    ExtentSlot(index).store(size, std::memory_order_release);
    m_live_objects[index / 64].fetch_or(UINT64_C(1) << (index % 64), std::memory_order_release);
    m_live_bytes.fetch_add(size, std::memory_order_relaxed);
}

LxrRcResult LxrReferenceCounting::OracleReclaim(uintptr_t start)
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
    return LxrRcResult::Success;
}

void LxrReferenceCounting::OracleRetire(uintptr_t start)
{
    size_t index = (start - GetReservationStart()) / sizeof(uintptr_t);
    size_t size = ExtentSlot(index).exchange(0, std::memory_order_acq_rel);
    m_live_objects[index / 64].fetch_and(~(UINT64_C(1) << (index % 64)), std::memory_order_acq_rel);
    m_live_bytes.fetch_sub(size, std::memory_order_relaxed);
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

size_t LxrReferenceCounting::FindNextOracleSlot(size_t first) const
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
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

LxrRcResult LxrReferenceCounting::ArmLifetimeProbe(Object* object)
{
    if (GetPhase() != LxrRcPhase::Mutator || !IsOwnedObject(object))
    {
        return LxrRcResult::InvalidArgument;
    }
    uintptr_t expected = 0;
    if (!m_probe_start.compare_exchange_strong(expected, Start(object), std::memory_order_release))
    {
        return LxrRcResult::InvalidPhase;
    }
    m_probe_retired.store(false, std::memory_order_release);
    return LxrRcResult::Success;
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
    if (metric == 57)
    {
        return m_increment_traces.load(std::memory_order_acquire);
    }
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
        // Components 7 and 10 were the size table and the live-object bitmap. P17 S3 removed them from the product;
        // the test platform keeps them as an oracle.
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        case 7: return m_extent_storage.GetCommittedBytes();
#else
        case 7: return 0;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        case 8: return 0;
        case 9: return 0;
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        case 10: return m_live_storage.GetCommittedBytes();
#else
        case 10: return 0;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
            *start = reinterpret_cast<uintptr_t>(m_extent_storage.Get());
            *bytes = m_extent_storage.GetReservedBytes();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
            return LxrRcResult::Success;
        case 1:
        case 2:
            // The lock table and the scan table were removed in P17 S7. Nothing is indexed by heap address now.
            return LxrRcResult::Success;
        case 3:
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
            *start = reinterpret_cast<uintptr_t>(m_live_storage.Get());
            *bytes = m_live_storage.GetReservedBytes();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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

LxrRcResult LxrReferenceCounting::ReleaseNurseryBlocks()
{
    // Mutators are suspended, so nothing adds to the list while it is read.
    size_t count = m_allocated_count.exchange(0, std::memory_order_acq_rel);
    for (size_t entry = 0; entry < count; entry++)
    {
        size_t block = m_allocated_list[entry];
        m_allocated_block_storage.Get()[block].store(0, std::memory_order_release);
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
        LxrRcResult result = OracleReclaimNursery(block);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
#ifndef LXR_S4_NO_NURSERY_RELEASE_CONTROL
        ImmixBlockOperationStatus status;
        LxrRcResult released = FromMetadata(m_blocks.TryReleaseNursery(
            GetReservationStart() + block * ImmixBlockGeometry::BlockBytes, &status));
        if (released != LxrRcResult::Success)
        {
            return released;
        }
        if (status == ImmixBlockOperationStatus::Updated)
        {
            if (FromMetadata(m_allocation.NotifyBlockReleased()) != LxrRcResult::Success)
            {
                return GetFailure();
            }
            m_metrics[BlocksReleased].fetch_add(1, std::memory_order_relaxed);
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
            m_pause_released_blocks.fetch_add(1, std::memory_order_relaxed);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
        }
        else if (status != ImmixBlockOperationStatus::Unchanged)
        {
            return Fail(LxrRcResult::MetadataFailure);
        }
#endif // !LXR_S4_NO_NURSERY_RELEASE_CONTROL
    }
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    LxrRcResult large = ReclaimLargeNursery();
    if (large != LxrRcResult::Success)
    {
        return large;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    // Every live young object has its first increment by now, so a probed object with count zero is dead.
    uintptr_t probe = m_probe_start.load(std::memory_order_acquire);
    if (probe != 0 && !m_probe_retired.load(std::memory_order_acquire) && OwnsAddress(probe) &&
        m_rc_metadata.Load(probe, SideMetadataMemoryOrder::Relaxed) == 0)
    {
        NoteProbeReclaim(probe);
    }
    return LxrRcResult::Success;
}

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
LxrRcResult LxrReferenceCounting::OracleReclaimNursery(size_t block)
{
    size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
    for (size_t index = FindNextOracleSlot(block * blockSlots); index < (block + 1) * blockSlots;
         index = FindNextOracleSlot(index + 1))
    {
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        uintptr_t references;
        LxrRcResult result = FromMetadata(m_counts.LoadFast(start, &references));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (references == 0 && OracleReclaim(start) != LxrRcResult::Success)
        {
            return GetFailure();
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReclaimLargeNursery()
{
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextOracleSlot(m_configuration.immixBytes / sizeof(uintptr_t)); index < slots;
         index = FindNextOracleSlot(index + 1))
    {
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        uintptr_t references;
        LxrRcResult result = FromMetadata(m_counts.LoadFast(start, &references));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (references == 0 && OracleReclaim(start) != LxrRcResult::Success)
        {
            return GetFailure();
        }
    }
    return LxrRcResult::Success;
}
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

uintptr_t LxrReferenceCounting::GetReservationStart() const
{
    return reinterpret_cast<uintptr_t>(m_arena);
}

size_t LxrReferenceCounting::GetReservationBytes() const
{
    return m_configuration.immixBytes + m_configuration.largeObjectBytes;
}

size_t LxrReferenceCounting::GetUsedBytesEstimate() const
{
    return m_allocation.GetBlocksInUse() * ImmixBlockGeometry::BlockBytes + m_large.GetCommittedBytes();
}

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
size_t LxrReferenceCounting::GetLiveBytes() const
{
    return m_live_bytes.load(std::memory_order_acquire);
}

uint64_t LxrReferenceCounting::GetReclaimedBytes() const
{
    return m_reclaimed_bytes.load(std::memory_order_acquire);
}
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

bool LxrReferenceCounting::OwnsAddress(uintptr_t address) const
{
    return m_arena != nullptr && (IsImmix(address) || IsLarge(address));
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
        // Outside a pause the neighbour is found from the size of the object. The size of a registered object
        // is intact. A stale bit can make the neighbour a dead young object; the caller filters by liveness.
        if (!IsObjectStart(object))
        {
            return nullptr;
        }
        size_t size = IsLarge(start) ? GetLargeObjectSize(start) : ObjectSize(object);
        if (size == 0 || size >= GetReservationBytes() - (start - GetReservationStart()))
        {
            return nullptr;
        }
        Object* next = Reference(start + size);
        return IsObjectStart(next) ? next : nullptr;
    }
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    size_t first = (start - GetReservationStart()) / sizeof(uintptr_t) + 1;
    for (size_t index = FindNextValidSlot(first); index < slots;
         index = FindNextValidSlot(index + 1))
    {
        uintptr_t candidateStart = GetReservationStart() + index * sizeof(uintptr_t);
        Object* candidate = Reference(candidateStart);
        // A stale bit belongs to a dead young object, and its count is zero.
        if (m_rc_metadata.Load(candidateStart, SideMetadataMemoryOrder::Relaxed) != 0 &&
            ResolveForwarded(candidate) == candidate)
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
    for (size_t index = FindNextValidSlot(0); index < slots;
         index = FindNextValidSlot(index + 1))
    {
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        Object* object = Reference(start);
        if (IsMarked(object))
        {
            bytes += IsLarge(start) ? GetLargeObjectSize(start) : ObjectSize(object);
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
    size_t bytes = m_allocated_block_storage.GetCommittedBytes();
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

size_t LxrReferenceCounting::FindNextValidSlot(size_t first) const
{
    size_t total = GetReservationBytes() / sizeof(uintptr_t);
    size_t immixEnd = GetImmixCapacityBytes() / sizeof(uintptr_t);
    size_t largeStart = m_configuration.immixBytes / sizeof(uintptr_t);
    size_t largeEnd = largeStart + m_large.GetAdmittedBytes() / sizeof(uintptr_t);
    uintptr_t arena = GetReservationStart();
    while (first < largeEnd)
    {
        if (first >= immixEnd && first < largeStart)
        {
            first = largeStart;
        }
        // One ValidObject word covers 64 slots, and the arena start is aligned to a block, so a slot index and
        // the bit position in its word agree.
        SideMetadataLocation location = m_valid_metadata.Locate(arena + first * sizeof(uintptr_t));
        uint64_t bits = static_cast<uint64_t>(
            SideMetadataAccessor::LoadWord(location.word, SideMetadataMemoryOrder::Relaxed)) >> location.shift;
#ifdef TARGET_WINDOWS
        DWORD bit;
#else
        uint32_t bit;
#endif // TARGET_WINDOWS
        if (BitScanForward64(&bit, bits))
        {
            std::atomic_thread_fence(std::memory_order_acquire);
            return first + bit;
        }
        first += 64 - location.shift;
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
    bytes += GetNonFreeBlocks() * ImmixBlockGeometry::BlockBytes;
    *pages = bytes / LxrLargeObjectSpace::PageBytes;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GetOccupiedPages(size_t* pages) const
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F5_BLOCK_PAGES_CONTROL)
    // Control: count whole blocks again (the behaviour before P25 F5).
    return GetMaturePages(pages);
#else
    if (pages == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t bytes;
    if (m_large.GetMatureCommittedBytes(&bytes) != LxrLargeObjectResult::Success)
    {
        return LxrRcResult::MetadataFailure;
    }
    size_t blockBytes = GetNonFreeBlocks() * ImmixBlockGeometry::BlockBytes;
    size_t freeBytes = m_blocks.GetReusableFreeLines() * ImmixBlockGeometry::LineBytes;
    bytes += blockBytes > freeBytes ? blockBytes - freeBytes : 0;
    *pages = bytes / LxrLargeObjectSpace::PageBytes;
    return LxrRcResult::Success;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F5_BLOCK_PAGES_CONTROL
}

size_t LxrReferenceCounting::GetNonFreeBlocks() const
{
    return m_blocks.GetNonFreeBlocks();
}

bool LxrReferenceCounting::BlockCountersMatchWalk(size_t* walkedNonFree, size_t* walkedFreeLines) const
{
    size_t nonFree = 0;
    size_t freeLines = 0;
    for (size_t offset = 0; offset < GetImmixCapacityBytes(); offset += ImmixBlockGeometry::BlockBytes)
    {
        ImmixBlockState state;
        uint8_t unavailable;
        if (m_blocks.GetState(GetReservationStart() + offset, &state, &unavailable) != SideMetadataResult::Success)
        {
            return false;
        }
        if (state != ImmixBlockState::Unallocated)
        {
            nonFree++;
        }
        if (state == ImmixBlockState::Reusable)
        {
            freeLines += ImmixBlockGeometry::LinesPerBlock - unavailable;
        }
    }
    if (walkedNonFree != nullptr)
    {
        *walkedNonFree = nonFree;
    }
    if (walkedFreeLines != nullptr)
    {
        *walkedFreeLines = freeLines;
    }
    return nonFree == m_blocks.GetNonFreeBlocks() && freeLines == m_blocks.GetReusableFreeLines();
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
    // With overshoot the capacity above the floor is borrowed to-space, so the doubling starts from the kept part.
    size_t base = m_overshoot && !m_grow_large && m_immix_floor != 0 ? m_immix_floor : current;
    size_t increment = base > minimum ? base : minimum;
    increment = increment > maximum - current ? maximum - current : increment;
    if (!FitAdmission(m_grow_large, current, minimum, &increment))
    {
        return LxrRcResult::OutOfMemory;
    }
    LxrRcResult result = AdmitSuspended(m_grow_large, current, increment, true);
    if (result == LxrRcResult::Success && !m_grow_large)
    {
        // A regular growth is permanent: the borrowed range below it stays with the heap.
        m_immix_floor = GetImmixCapacityBytes();
        m_allocation.SetMutatorFreshBlocksQuiescent(m_immix_floor / ImmixBlockGeometry::BlockBytes);
    }
    return result;
}

bool LxrReferenceCounting::FitAdmission(bool large, size_t current, size_t minimum, size_t* increment)
{
    size_t granularity = large ? LxrLargeObjectSpace::PageBytes : ImmixBlockGeometry::BlockBytes;
    size_t offset = large ? m_configuration.immixBytes : 0;
    size_t first = (offset + current) / sizeof(uintptr_t);
    size_t previousLogCapacity = GetAdmittedBytes() / sizeof(uintptr_t);
    auto fits = [&](size_t candidate)
    {
        size_t slots = candidate / sizeof(uintptr_t);
        size_t nativeBytes = 0;
        size_t blockSlots = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
        nativeBytes += large ? m_allocated_block_storage.GetCommittedBytes() :
            m_allocated_block_storage.ProjectedCommittedBytes(first / blockSlots, slots / blockSlots);
        for (size_t bank = 0; bank < 2; bank++)
        {
            for (LogStream* stream : {
                    &m_banks[bank].increments, &m_banks[bank].decrements})
            {
                size_t desired = previousLogCapacity + slots;
                nativeBytes += desired > stream->capacity
                    ? stream->referenceStorage.ProjectedCommittedBytes(stream->capacity, desired - stream->capacity)
                    : stream->referenceStorage.GetCommittedBytes();
            }
        }
        return m_object_model.canAdmitRange(first, slots, nativeBytes, m_object_model.context);
    };
    if (m_object_model.canAdmitRange != nullptr && !fits(*increment))
    {
        if (!fits(minimum))
        {
            return false;
        }
        size_t low = minimum / granularity;
        size_t high = *increment / granularity;
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
        *increment = low * granularity;
    }
    return true;
}

LxrRcResult LxrReferenceCounting::AdmitSuspended(
    bool large, size_t current, size_t increment, bool initialize)
{
    size_t next = current + increment;
    size_t offset = large ? m_configuration.immixBytes : 0;
    size_t first = (offset + current) / sizeof(uintptr_t);
    size_t previousLogCapacity = GetAdmittedBytes() / sizeof(uintptr_t);
    size_t count = increment / sizeof(uintptr_t);
    size_t logIncrement = count;
    if (!CommitPlanes(first, count, initialize))
    {
        return LxrRcResult::OutOfMemory;
    }
    for (size_t bank = 0; bank < 2; bank++)
    {
        for (LogStream* stream : {
                &m_banks[bank].increments, &m_banks[bank].decrements})
        {
            // The log capacity never shrinks (see ReturnTailSuspended), so a range that was given back and is admitted
            // again may need no new storage.
            size_t desired = previousLogCapacity + logIncrement;
            if (desired > stream->capacity &&
                !stream->referenceStorage.Commit(stream->capacity, desired - stream->capacity, false))
            {
                return LxrRcResult::OutOfMemory;
            }
        }
    }
    if (m_object_model.admitRange != nullptr &&
        !m_object_model.admitRange(first, count, initialize, m_object_model.context))
    {
        return LxrRcResult::OutOfMemory;
    }
    uintptr_t start = GetReservationStart() + offset + current;
    if (m_metadata.CommitDataRange(start, increment) != SideMetadataResult::Success)
    {
        return LxrRcResult::OutOfMemory;
    }
    if (large)
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
        size_t desired = previousLogCapacity + logIncrement;
        if (desired > m_banks[bank].capacity)
        {
            m_banks[bank].capacity = desired;
            m_banks[bank].increments.capacity = desired;
            m_banks[bank].decrements.capacity = desired;
        }
    }
    m_growth_count.fetch_add(1, std::memory_order_release);
    return LxrRcResult::Success;
}

namespace
{
uint64_t TicksToNanoseconds(int64_t ticks)
{
    int64_t frequency = LxrPauseTimers::Frequency();
    return static_cast<uint64_t>(ticks / frequency * 1000000000 + ticks % frequency * 1000000000 / frequency);
}
}

size_t LxrReferenceCounting::GetBorrowedBytes() const
{
    size_t capacity = GetImmixCapacityBytes();
    return capacity > m_immix_floor ? capacity - m_immix_floor : 0;
}

uint64_t LxrReferenceCounting::GetOvershootMetric(size_t metric) const
{
    if (metric >= OvershootMetricCount)
    {
        return UINT64_MAX;
    }
    if (metric == BorrowedNow)
    {
        return GetBorrowedBytes();
    }
    return m_overshoot_metrics[metric].load(std::memory_order_relaxed);
}

LxrRcResult LxrReferenceCounting::BorrowSuspended()
{
    size_t request = m_borrow_request_pages;
    m_borrow_request_pages = 0;
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_NO_BORROW_CONTROL)
    return LxrRcResult::Success;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S5B_NO_BORROW_CONTROL
    size_t free = GetImmixFreePages();
    SetOvershoot(BorrowRequestPages, request);
    SetOvershoot(BorrowFreePages, free);
    SetOvershoot(BorrowBytesLast, 0);
    SetOvershoot(BorrowNsLast, 0);
    if (request <= free)
    {
        return LxrRcResult::Success;
    }
    LxrPhaseScope scope(m_pause_timers, LxrPauseTimers::Borrow);
    int64_t started = LxrPauseTimers::Now();
    size_t current = GetImmixCapacityBytes();
    size_t block = ImmixBlockGeometry::BlockBytes;
    size_t missing = ((request - free) * LxrLargeObjectSpace::PageBytes + block - 1) & ~(block - 1);
    // The hard limit equals the whole reservation here (see m_overshoot), so the room is what is left of it.
    size_t room = m_configuration.immixBytes - current;
    size_t limitRoom = m_configuration.hardLimitBytes - GetAdmittedBytes();
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_IGNORE_HARD_LIMIT_CONTROL)
    limitRoom = room;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S5B_IGNORE_HARD_LIMIT_CONTROL
    size_t available = (room < limitRoom ? room : limitRoom) & ~(block - 1);
    size_t increment = missing < available ? missing : available;
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_IGNORE_PLANE_LIMIT_CONTROL)
    bool fitted = increment != 0;
#else
    bool fitted = increment != 0 && FitAdmission(false, current, block, &increment);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S5B_IGNORE_PLANE_LIMIT_CONTROL
    if (!fitted)
    {
        CountOvershoot(BorrowRefused, 1);
        return LxrRcResult::Success;
    }
    // The planes of a borrowed range are zero from the operating system, and most of the range is never used, so the
    // storage is committed without writing to it.
    LxrRcResult result = AdmitSuspended(false, current, increment, false);
    if (result != LxrRcResult::Success)
    {
        // A failed commit leaves the capacity as it was; the pause copies into what is free, as without overshoot.
        CountOvershoot(BorrowRefused, 1);
        return result == LxrRcResult::OutOfMemory ? LxrRcResult::Success : result;
    }
    uint64_t nanoseconds = TicksToNanoseconds(LxrPauseTimers::Now() - started);
    CountOvershoot(BorrowPauses, 1);
    CountOvershoot(BorrowBytesTotal, increment);
    SetOvershoot(BorrowBytesLast, increment);
    if (increment > m_overshoot_metrics[BorrowBytesMax].load(std::memory_order_relaxed))
    {
        SetOvershoot(BorrowBytesMax, increment);
    }
    CountOvershoot(BorrowNsTotal, nanoseconds);
    SetOvershoot(BorrowNsLast, nanoseconds);
    if (GetImmixCapacityBytes() > m_overshoot_metrics[PeakCapacityBytes].load(std::memory_order_relaxed))
    {
        SetOvershoot(PeakCapacityBytes, GetImmixCapacityBytes());
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GiveBackSuspended()
{
    size_t capacity = GetImmixCapacityBytes();
    SetOvershoot(GiveBackBytesLast, 0);
    SetOvershoot(GiveBackNsLast, 0);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_NO_GIVEBACK_CONTROL)
    return LxrRcResult::Success;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S5B_NO_GIVEBACK_CONTROL
    if (capacity <= m_immix_floor)
    {
        return LxrRcResult::Success;
    }
    LxrPhaseScope scope(m_pause_timers, LxrPauseTimers::GiveBack);
    int64_t started = LxrPauseTimers::Now();
    CountOvershoot(GiveBackAttempts, 1);
    size_t block = ImmixBlockGeometry::BlockBytes;
    size_t index = capacity / block;
    size_t floorBlocks = m_immix_floor / block;
    uintptr_t arena = GetReservationStart();
    // Only a free tail can go back: the walk stops at the first block that holds an object or that an allocator owns.
    while (index > floorBlocks)
    {
        size_t candidate = index - 1;
        uintptr_t address = arena + candidate * block;
        bool free;
        LxrRcResult result = FromMetadata(m_blocks.IsFreeBlock(address, &free));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        if (!free)
        {
            // A copy block that stayed empty is released now, as the lazy sweep would do after the pause. The sweep
            // runs while mutators allocate, so it could not give the block back.
            if (m_mature_candidates[candidate].load(std::memory_order_acquire) == 0)
            {
                break;
            }
            ImmixBlockOperationStatus status;
            result = ReleaseBlockIfDead(address, candidate, nullptr, &status);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            if (status != ImmixBlockOperationStatus::Updated)
            {
                break;
            }
            CountOvershoot(EmptyCopyBlocksReleased, 1);
        }
        index = candidate;
    }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_ALWAYS_RETURN_CONTROL)
    index = floorBlocks;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S5B_ALWAYS_RETURN_CONTROL
    size_t newCapacity = index * block;
    if (newCapacity < capacity)
    {
        LxrRcResult result = ReturnTailSuspended(newCapacity);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
        CountOvershoot(GiveBackSuccesses, 1);
        CountOvershoot(GiveBackBytesTotal, capacity - newCapacity);
        SetOvershoot(GiveBackBytesLast, capacity - newCapacity);
        if (newCapacity == m_immix_floor)
        {
            CountOvershoot(GiveBackComplete, 1);
        }
    }
    uint64_t nanoseconds = TicksToNanoseconds(LxrPauseTimers::Now() - started);
    CountOvershoot(GiveBackNsTotal, nanoseconds);
    SetOvershoot(GiveBackNsLast, nanoseconds);
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ReturnTailSuspended(size_t newCapacity)
{
    size_t oldCapacity = GetImmixCapacityBytes();
    if (GetPhase() != LxrRcPhase::Suspended || newCapacity < m_immix_floor || newCapacity >= oldCapacity ||
        newCapacity % ImmixBlockGeometry::BlockBytes != 0)
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t increment = oldCapacity - newCapacity;
    for (size_t block = newCapacity / ImmixBlockGeometry::BlockBytes;
         block < oldCapacity / ImmixBlockGeometry::BlockBytes; block++)
    {
        // The blocks hold no object, so no flag of the lazy sweep can still be meant for them.
        m_mature_candidates[block].store(0, std::memory_order_release);
        if (m_evacuation_sources != nullptr)
        {
            m_evacuation_sources[block].store(0, std::memory_order_release);
        }
    }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_S5B_NO_ALLOC_SHRINK_CONTROL)
    m_immix_capacity.store(newCapacity, std::memory_order_release);
    return LxrRcResult::Success;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_S5B_NO_ALLOC_SHRINK_CONTROL
    // Lower the capacity first: from here on the range is not an Immix range, so a stale lookup is refused by
    // IsImmix and the checked metadata accessors, and no allocator can pick a block of it.
    if (m_allocation.ShrinkQuiescent(newCapacity) != SideMetadataResult::Success)
    {
        return Fail(LxrRcResult::InvariantViolation);
    }
    m_immix_capacity.store(newCapacity, std::memory_order_release);
    size_t first = newCapacity / sizeof(uintptr_t);
    size_t count = increment / sizeof(uintptr_t);
    // No Immix block follows the old end when the capacity is below the reservation, so the storage pages that hold
    // the last slots of the range are not shared with an admitted neighbour.
    bool tailIsLast = oldCapacity < m_configuration.immixBytes;
    if ((m_object_model.releaseRange != nullptr &&
            !m_object_model.releaseRange(first, count, tailIsLast, m_object_model.context)) ||
        !DecommitPlanes(first, count, tailIsLast))
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
    uintptr_t start = GetReservationStart() + newCapacity;
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_S5B_NO_PLANE_DECOMMIT_CONTROL)
    if (m_metadata.DecommitDataRange(start, increment) != SideMetadataResult::Success)
    {
        return Fail(LxrRcResult::MetadataFailure);
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_S5B_NO_PLANE_DECOMMIT_CONTROL
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_S5B_NO_DATA_DECOMMIT_CONTROL)
    if (!GCToOSInterface::VirtualDecommit(reinterpret_cast<void*>(start), increment))
    {
        return Fail(LxrRcResult::OutOfMemory);
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_S5B_NO_DATA_DECOMMIT_CONTROL
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GetHeapStatistics(size_t* heapBytes, size_t* fragmentationBytes) const
{
    if (heapBytes == nullptr || fragmentationBytes == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    size_t largeBytes;
    size_t largeSlack;
    if (m_large.GetMatureStatistics(&largeBytes, &largeSlack) != LxrLargeObjectResult::Success)
    {
        return LxrRcResult::MetadataFailure;
    }
    // Before P17 S4 this walked every counted object. A walk needs the paused heap and costs time in every pause, so
    // the figures come from counters that block transitions maintain. Heap size counts whole blocks; fragmentation
    // counts the free lines of Reusable blocks (found by the lazy sweep) and the unused tail pages of large objects.
    *heapBytes = GetNonFreeBlocks() * ImmixBlockGeometry::BlockBytes + largeBytes;
    *fragmentationBytes = m_blocks.GetReusableFreeLines() * ImmixBlockGeometry::LineBytes + largeSlack;
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::StopWorkers()
{
    m_scan_checkpoint.store(0, std::memory_order_release);
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    m_gate_armed.store(false, std::memory_order_release);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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
    for (size_t index = FindNextValidSlot(0); index < slots;
         index = FindNextValidSlot(index + 1))
    {
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        // A count of zero marks a dead young object with a stale bit, or a young object that has no increment yet.
        if (m_rc_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) == 0)
        {
            continue;
        }
        Object* object = Reference(start);
        if (ResolveForwarded(object) != object)
        {
            continue;
        }
        size_t size = IsLarge(start) ? GetLargeObjectSize(start) : ObjectSize(object);
        if (size != 0 && !visit(object, size, context))
        {
            break;
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::WalkObjectStarts(LxrRcWalkObject visit, void* context) const
{
    if (visit == nullptr || m_observer || GetPhase() != LxrRcPhase::Suspended)
    {
        return LxrRcResult::InvalidPhase;
    }
    size_t slots = GetReservationBytes() / sizeof(uintptr_t);
    for (size_t index = FindNextValidSlot(0); index < slots;
         index = FindNextValidSlot(index + 1))
    {
        Object* object = Reference(GetReservationStart() + index * sizeof(uintptr_t));
        if (!visit(object, 0, context))
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
    // The copy path logged every forwarded source with its size; RetireForwardedSources consumes the log later.
    for (size_t slot = 0; slot <= m_configuration.workers; slot++)
    {
        for (CopyLog::Chunk* chunk = m_copy_logs[slot].head; chunk != nullptr; chunk = chunk->next)
        {
            for (size_t entry = 0; entry < chunk->count; entry++)
            {
                Object* source = Reference(chunk->entries[entry].start);
                Object* destination = ResolveForwarded(source);
                if (destination != source && !visit(source, destination, chunk->entries[entry].size, context))
                {
                    return LxrRcResult::Success;
                }
            }
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
        PushTraceUnsubmitted(work);
        return LxrRcResult::Success;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    if (GetTracePhase() != LxrTracePhase::Concurrent ||
        GetPhase() != LxrRcPhase::Mutator || m_hold_trace.load(std::memory_order_acquire))
    {
        PushTraceUnsubmitted(work);
        return LxrRcResult::Success;
    }
    // The packet may run and be retired as soon as Submit returns, so it is not touched after this call.
    LxrWorkSchedulerResult result = m_scheduler.Submit(&work->packet);
    if (result == LxrWorkSchedulerResult::Success)
    {
        return LxrRcResult::Success;
    }
    PushTraceUnsubmitted(work);
    if (result == LxrWorkSchedulerResult::StageDisabled ||
        result == LxrWorkSchedulerResult::Busy || result == LxrWorkSchedulerResult::InvalidState)
    {
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
    // P26 (F2). Only the packets that could not go to the scheduler are visited (m_trace_unsubmitted). The pending packets
    // that are queued in the scheduler stay where they are, so the cost does not follow the trace backlog.
    Work* batch = m_trace_unsubmitted.exchange(nullptr, std::memory_order_acquire);
    LxrRcResult status = LxrRcResult::Success;
    while (batch != nullptr)
    {
        Work* work = batch;
        batch = work->traceNext;
        if (status == LxrRcResult::Success)
        {
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
            if (work->watchedReader && m_reader_held.load(std::memory_order_acquire))
            {
                PushTraceUnsubmitted(work);
                continue;
            }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
            m_walk[3].fetch_add(1, std::memory_order_relaxed);
            LxrWorkSchedulerResult result = m_scheduler.Submit(&work->packet);
            if (result == LxrWorkSchedulerResult::Success)
            {
                continue;
            }
            status = FromScheduler(result);
        }
        PushTraceUnsubmitted(work);
    }
    return status;
}

LxrRcResult LxrReferenceCounting::ProcessTraceWork(Work* work)
{
    // P26 (F4b). Every trace packet collects its children in one batch and loops over it (see ProcessTraceWork below).
    return ProcessFullTraceWork(work);
}

NOINLINE LxrRcResult LxrReferenceCounting::ProcessFullTraceWork(Work* work)
{
    TraceBatch targets(this);
    return GetFailure() == LxrRcResult::Success ? ProcessTraceWork(work, &targets) : GetFailure();
}

// P26 (F4b). The trace loop of a packet stops when a pause wants the workers: the scheduler has been asked to interrupt the
// concurrent work (the pause waits for this packet), or the engine left the mutator phase. In a pause the loop never stops.
bool LxrReferenceCounting::ShouldStopTraceLoop() const
{
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4B_NO_TRACE_LOOP_CONTROL)
    return true;
#else
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    if (m_trace_loop_off.load(std::memory_order_acquire))
    {
        return true;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    return GetFailure() != LxrRcResult::Success ||
        (GetPhase() == LxrRcPhase::Mutator && GetTracePhase() == LxrTracePhase::Concurrent &&
            m_scheduler.IsInterruptionRequested());
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4B_NO_TRACE_LOOP_CONTROL
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
            // The Java LXR prefetches the object that is 8 ahead in its trace loops too (cm.rs).
            LxrPrefetch::Ahead(work->references, work->packet.GetReferenceCount(), index, false);
        }
    }
    // P26 (F4b). The Java LXR trace packet loops: it traces its objects, swaps in the children that it found, and goes on
    // until no child is left (cm.rs: LXRConcurrentTraceObjects::do_work). A chain of children costs one packet. Only a
    // batch that fills up (8 192 children) becomes a new packet, so that other workers can take it. Before, every
    // object that had children made its own packet.
    while (result == LxrRcResult::Success && targets != nullptr && targets->count != 0 && !ShouldStopTraceLoop())
    {
        size_t levelCount = targets->count;
        uintptr_t level[FullTraceBatchCapacity];
        memcpy(level, targets->references, levelCount * sizeof(uintptr_t));
        targets->count = 0;
        size_t directBudget = BufferCapacity;
        for (size_t index = 0; index < levelCount; index++)
        {
            directBudget = BufferCapacity;
            result = ProcessTraceObject(reinterpret_cast<Object*>(level[index]), &directBudget, targets);
            if (result != LxrRcResult::Success)
            {
                break;
            }
            LxrPrefetch::Ahead(level, levelCount, index, false);
            if ((index & 255) == 255 && index + 1 < levelCount && ShouldStopTraceLoop())
            {
                for (size_t rest = index + 1; rest < levelCount && result == LxrRcResult::Success; rest++)
                {
                    result = targets->Append(level[rest]);
                }
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
    if (m_gate_armed.load(std::memory_order_acquire) && GetPhase() == LxrRcPhase::Mutator &&
        GetTracePhase() == LxrTracePhase::Concurrent && m_gate_remaining.fetch_sub(1, std::memory_order_acq_rel) <= 1)
    {
        m_gate_blocked.store(true, std::memory_order_release);
        while (m_gate_armed.load(std::memory_order_acquire) && GetPhase() == LxrRcPhase::Mutator)
        {
            GCToOSInterface::YieldThread(0);
        }
        m_gate_blocked.store(false, std::memory_order_release);
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
    return result;
}

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
void LxrReferenceCounting::ArmTraceGateForTest(uint64_t packets)
{
    m_gate_remaining.store(static_cast<int64_t>(packets), std::memory_order_release);
    m_gate_armed.store(true, std::memory_order_release);
}

LxrRcResult LxrReferenceCounting::QueueTraceBacklogForTest(Object* object, size_t count)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Concurrent)
    {
        return LxrRcResult::InvalidPhase;
    }
    uintptr_t reference = reinterpret_cast<uintptr_t>(object);
    for (size_t index = 0; index < count; index++)
    {
        LxrRcResult result = QueueTrace(&reference, 1);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    return LxrRcResult::Success;
}
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
LxrRcResult LxrReferenceCounting::ValidateTraceBatchForTest(Object* object, size_t count)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Inactive ||
        m_trace_pending.load() != 0 || m_work.load() != nullptr ||
        !IsObjectStart(object) || count == 0 || count > FullTraceBatchCapacity + 1)
    {
        return LxrRcResult::InvalidPhase;
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
    return field < 6 ? m_mature_scan_receipt[field].load(std::memory_order_acquire) : UINTPTR_MAX;
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
        if (IsReturnedImmix(start))
        {
            return LxrRcResult::Success;
        }
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
    uintptr_t count = m_rc_metadata.Load(start, SideMetadataMemoryOrder::SequentiallyConsistent);
    uintptr_t mark = m_mark_metadata.Load(start, SideMetadataMemoryOrder::SequentiallyConsistent);
    LxrRcResult result = LxrRcResult::Success;
    if (count == 0 || mark != 0 || !IsObjectStart(object))
    {
        ExitTraceLock(index);
        return result;
    }
    Object* loader = GetLoaderAllocator(object);
    size_t maximumReferences = object->GetGCSafeMethodTable()->ContainsGCPointers()
        ? ObjectSize(object) / sizeof(uintptr_t) + 1
        : (loader == nullptr ? 0 : 1);
    if (maximumReferences <= *directBudget)
    {
        m_mark_metadata.Store(start, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
        m_trace_metrics[TracedObjects].fetch_add(1, std::memory_order_relaxed);
        TraceScan local(object, loader);
        result = ScanTraceLocked(&local, true, targets);
        *directBudget -= maximumReferences;
        if (GetPhase() == LxrRcPhase::Mutator && GetTracePhase() == LxrTracePhase::Concurrent)
        {
            m_trace_metrics[ConcurrentSlices].fetch_add(1, std::memory_order_relaxed);
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
#if !defined(LXR_S7_SCAN_LINK_CONTROL)
    LinkTraceScan(index, scan);
#endif // !LXR_S7_SCAN_LINK_CONTROL
    m_mark_metadata.Store(start, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
    m_trace_metrics[TracedObjects].fetch_add(1, std::memory_order_relaxed);
    result = QueueTrace(nullptr, 0, scan);
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
            // The children of the slice must be visible while the scan waits (the packets are made before the wait).
            if (result == LxrRcResult::Success && targets != nullptr)
            {
                result = targets->Flush();
            }
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
    LxrRcResult result = LxrRcResult::Success;
    if (m_mark_metadata.Load(start, SideMetadataMemoryOrder::SequentiallyConsistent) == 0)
    {
        m_mark_metadata.Store(start, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
        m_trace_metrics[DeletionMarks].fetch_add(1, std::memory_order_relaxed);
    }
    TraceScan* scan = FindTraceScan(index);
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
        // P26 (F2). The pending trace packets are in two places: the packets that were made in this pause (or never went to
        // the scheduler) are in m_trace_unsubmitted; the packets of the concurrent phase wait in the scheduler. The stage is
        // closed in a pause, so the pause thread runs them itself.
        Work* batch = m_trace_unsubmitted.exchange(nullptr, std::memory_order_acquire);
        while (batch != nullptr)
        {
            Work* work = batch;
            batch = work->traceNext;
            LxrRcResult result = ProcessTraceWork(work);
            RetireTraceWork(work);
            if (result != LxrRcResult::Success)
            {
                while (batch != nullptr)
                {
                    Work* rest = batch;
                    batch = rest->traceNext;
                    PushTraceUnsubmitted(rest);
                }
                return result;
            }
            progressed = true;
        }
        uint64_t ran = 0;
        LxrWorkSchedulerResult inlineResult = m_scheduler.RunQueuedInline(
            LxrWorkBucketStage::ConcurrentResumable, m_configuration.workers, &ran);
        // InvalidState: the stage is open or a packet runs. Outside a pause nothing is queued that the caller must run.
        if (inlineResult != LxrWorkSchedulerResult::Success && inlineResult != LxrWorkSchedulerResult::InvalidState)
        {
            return FromScheduler(inlineResult);
        }
        if (ran != 0)
        {
            progressed = true;
        }
        if (GetFailure() != LxrRcResult::Success)
        {
            return GetFailure();
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
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F1_NO_ROOT_TRACE_CONTROL)
    // Control: the roots of the pause are not queued. A pause that does not queue increment targets then marks nothing.
    return m_pause_kind == LxrPauseKind::InitialMark ? LxrRcResult::Success : DrainTraceInsidePause();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F1_NO_ROOT_TRACE_CONTROL
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

LxrRcResult LxrReferenceCounting::TraceRoots(const uintptr_t* objects, size_t count)
{
    if (GetPhase() != LxrRcPhase::Suspended ||
        (GetTracePhase() != LxrTracePhase::Full && GetTracePhase() != LxrTracePhase::FinalMark))
    {
        return LxrRcResult::InvalidPhase;
    }
    for (size_t offset = 0; offset < count; offset += BufferCapacity)
    {
        size_t batch = count - offset;
        if (batch > BufferCapacity)
        {
            batch = BufferCapacity;
        }
        LxrRcResult result = QueueTrace(objects + offset, batch);
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    return count == 0 ? LxrRcResult::Success : DrainTraceInsidePause();
}

bool LxrReferenceCounting::IsMarked(Object* object) const
{
    object = ResolveForwarded(object);
    if (object == nullptr || !OwnsAddress(Start(object)) || Start(object) % sizeof(uintptr_t) != 0)
    {
        return false;
    }
    // A black-allocated young object keeps its mark and its ValidObject bit after it dies, so liveness also needs a
    // count above zero. Every live object has one by the time a pause scans weak references.
    return IsObjectStart(object) &&
        m_rc_metadata.Load(Start(object), SideMetadataMemoryOrder::Relaxed) != 0 &&
        m_mark_metadata.Load(Start(object), SideMetadataMemoryOrder::SequentiallyConsistent) != 0;
}

bool LxrReferenceCounting::IsObjectStart(Object* object) const
{
    uintptr_t start = Start(object);
    if (!OwnsAddress(start) || start % sizeof(uintptr_t) != 0)
    {
        return false;
    }
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_OBJECT_INTERIOR_CONTROL)
    if (IsLarge(start) && start % LxrLargeObjectSpace::PageBytes != 0)
    {
        return false;
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_OBJECT_INTERIOR_CONTROL
    return m_valid_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) != 0;
}

bool LxrReferenceCounting::IsLargeObject(Object* object) const
{
    return IsObjectStart(object) && IsLarge(Start(object));
}

size_t LxrReferenceCounting::GetLargeObjectSize(uintptr_t start) const
{
    LxrLargeObjectAllocation allocation;
    return m_large.GetAllocation(start, &allocation) == LxrLargeObjectResult::Success
        ? allocation.object_bytes : 0;
}

size_t LxrReferenceCounting::GetLiveObjectSize(Object* object) const
{
    if (!IsObjectStart(object))
    {
        return 0;
    }
    uintptr_t start = Start(object);
    if (IsLarge(start))
    {
        return GetLargeObjectSize(start);
    }
    return m_rc_metadata.Load(start, SideMetadataMemoryOrder::Relaxed) != 0 ? ObjectSize(object) : 0;
}

bool LxrReferenceCounting::IsCycleSweepPending() const
{
#if defined(LXR_CYCLE_GATE_CONTROL)
    return false;
#else
    return m_cycle_sweep.load(std::memory_order_acquire);
#endif // LXR_CYCLE_GATE_CONTROL
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
    for (size_t index = FindNextValidSlot(0); index < slots;
         index = FindNextValidSlot(index + 1))
    {
        uintptr_t start = GetReservationStart() + index * sizeof(uintptr_t);
        // Only an object with a count and without a mark is a dead cycle. A stale bit of a dead young object has
        // count zero, so the sweep never reads such an object.
        uintptr_t count = m_rc_metadata.Load(start, SideMetadataMemoryOrder::SequentiallyConsistent);
        uintptr_t mark = m_mark_metadata.Load(start, SideMetadataMemoryOrder::SequentiallyConsistent);
        if (count == 0 || mark != 0)
        {
            continue;
        }
        Object* object = Reference(start);
        if (ResolveForwarded(object) != object)
        {
            continue;
        }
        LxrRcResult result;
        size_t size;
        if (IsImmix(start))
        {
            // The size comes from the dead object, as in the Java LXR. For an object of a collectible type this
            // reads the type data of a loader allocator that the runtime may free once it knows that the loader is
            // dead. The runtime frees it only from a finalizer, and the collector holds finalization back while
            // m_cycle_sweep is set (see IsCycleSweepPending), so the data is valid until this sweep has finished.
            size = ObjectSize(object);
            result = FromMetadata(m_counts.ClearStraddleLinesFast(start, size));
            if (result != LxrRcResult::Success)
            {
                return result;
            }
            size_t block = (start - GetReservationStart()) / ImmixBlockGeometry::BlockBytes;
            m_mature_candidates[block].store(1, std::memory_order_release);
        }
        else
        {
            size = GetLargeObjectSize(start);
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
        m_rc_metadata.Store(start, 0, SideMetadataMemoryOrder::SequentiallyConsistent);
        m_valid_metadata.Store(start, 0, SideMetadataMemoryOrder::SequentiallyConsistent);
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

LxrPauseWalkMetrics LxrReferenceCounting::GetPauseWalkMetrics() const
{
    return {
        m_walk[0].load(std::memory_order_relaxed), m_walk[1].load(std::memory_order_relaxed),
        m_walk[2].load(std::memory_order_relaxed), m_walk[3].load(std::memory_order_relaxed),
        m_walk[4].load(std::memory_order_relaxed),
    };
}

LxrRcResult LxrReferenceCounting::ArmScanCheckpoint(Object* object)
{
    if (GetPhase() != LxrRcPhase::Mutator || GetTracePhase() != LxrTracePhase::Inactive ||
        !IsOwnedObject(object) || m_scan_checkpoint_active.load(std::memory_order_acquire))
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

void LxrReferenceCounting::HoldSweepForTest(bool hold)
{
    m_sweep_hold.store(hold, std::memory_order_release);
}

bool LxrReferenceCounting::IsSweepHeldForTest() const
{
    return m_sweep_holding.load(std::memory_order_acquire);
}

LxrRcResult LxrReferenceCounting::GetBlockStateForTest(
    size_t block, ImmixBlockState* state, uint8_t* unavailableLines, bool* nursery) const
{
    uintptr_t address = GetReservationStart() + block * ImmixBlockGeometry::BlockBytes;
    if (state == nullptr || unavailableLines == nullptr || nursery == nullptr ||
        block >= GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes)
    {
        return LxrRcResult::InvalidArgument;
    }
    SideMetadataResult result = m_blocks.GetState(address, state, unavailableLines);
    return result == SideMetadataResult::Success && m_blocks.IsNursery(address, nursery) == SideMetadataResult::Success
        ? LxrRcResult::Success : LxrRcResult::MetadataFailure;
}

size_t LxrReferenceCounting::GetFreshBlocksReleasedInPauseForTest() const
{
    return m_pause_released_blocks.load(std::memory_order_acquire);
}

bool LxrReferenceCounting::IsDataOwnedForTest(uintptr_t address) const
{
    return m_metadata.IsDataAddressOwned(address);
}

LxrRcResult LxrReferenceCounting::PoisonRangeForTest(uintptr_t start, size_t bytes, bool counts)
{
    for (uintptr_t address = start; address < start + bytes; address += sizeof(uintptr_t))
    {
        m_valid_metadata.Store(address, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
        if (counts)
        {
            m_rc_metadata.Store(address, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
        }
        m_unlogged_metadata.Store(address, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
        m_mark_metadata.Store(address, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
    }
    for (uintptr_t line = start; counts && line < start + bytes; line += ImmixBlockGeometry::LineBytes)
    {
        LxrRcResult result = FromMetadata(m_metadata.Store(
            LxrSideMetadataKind::ReferenceCountStraddle, line, 1, SideMetadataMemoryOrder::SequentiallyConsistent));
        if (result != LxrRcResult::Success)
        {
            return result;
        }
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::GetRangeStateForTest(
    uintptr_t start, size_t bytes, RangeStateForTest* state)
{
    *state = {};
    for (uintptr_t address = start; address < start + bytes; address += sizeof(uintptr_t))
    {
        state->validSlots += m_valid_metadata.Load(address, SideMetadataMemoryOrder::Relaxed) != 0;
        state->countedSlots += m_rc_metadata.Load(address, SideMetadataMemoryOrder::Relaxed) != 0;
        state->unloggedSlots += m_unlogged_metadata.Load(address, SideMetadataMemoryOrder::Relaxed) != 0;
        state->markedSlots += m_mark_metadata.Load(address, SideMetadataMemoryOrder::Relaxed) != 0;
    }
    for (uintptr_t line = start; line < start + bytes; line += ImmixBlockGeometry::LineBytes)
    {
        uintptr_t straddle = 0;
        if (m_metadata.Load(LxrSideMetadataKind::ReferenceCountStraddle, line,
                SideMetadataMemoryOrder::Relaxed, &straddle) != SideMetadataResult::Success)
        {
            return LxrRcResult::MetadataFailure;
        }
        state->straddleLines += straddle != 0;
    }
    return LxrRcResult::Success;
}

LxrRcResult LxrReferenceCounting::ProcessQueuedTraceReferenceForTest(uintptr_t address)
{
    size_t budget = BufferCapacity;
    return ProcessTraceObject(reinterpret_cast<Object*>(address), &budget, nullptr);
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
