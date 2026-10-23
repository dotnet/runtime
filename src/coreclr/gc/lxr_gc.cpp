// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "lxr_gc.h"
#include "lxr_write_barrier.h"
#include "lxr_worker_policy.h"
#include "gcscan.h"
#include "gcref.h"
#include "gceventstatus.h"
#include "objecthandle.h"

#include <algorithm>
#include <new>
#ifndef TARGET_WINDOWS
#include <time.h>
#endif // !TARGET_WINDOWS

// Entry of the assembly write barrier slow path (defined below).
extern "C" void LxrBarrierSlowPath(Object** slot);

namespace
{
    bool ReadSetting(
        const char* name, size_t* value, unsigned base = 10, size_t maximum = SIZE_MAX,
        bool* specified = nullptr, bool boolean = false)
    {
        char publicKey[128];
        int length = snprintf(publicKey, sizeof(publicKey), "System.GC.%s",
            strncmp(name, "GCHeap", 6) == 0 ? name + 2 : name);
        if (length < 0 || static_cast<size_t>(length) >= sizeof(publicKey))
        {
            GCToEEInterface::LogErrorToHost("LXR configuration key exceeds its bounded storage.");
            return false;
        }
        for (const char* prefix : {"DOTNET_", "COMPlus_"})
        {
            char environmentKey[128];
            length = snprintf(environmentKey, sizeof(environmentKey), "%s%s", prefix, name);
            if (length < 0 || static_cast<size_t>(length) >= sizeof(environmentKey))
            {
                GCToEEInterface::LogErrorToHost("LXR environment key exceeds its bounded storage.");
                return false;
            }
#ifdef TARGET_WINDOWS
            SetLastError(ERROR_SUCCESS);
            DWORD environmentLength = GetEnvironmentVariableA(environmentKey, nullptr, 0);
            bool empty = environmentLength == 1 ||
                (environmentLength == 0 && GetLastError() != ERROR_ENVVAR_NOT_FOUND);
#else
            const char* environmentValue = getenv(environmentKey);
            bool empty = environmentValue != nullptr && *environmentValue == '\0';
#endif // TARGET_WINDOWS
            if (empty)
            {
                GCToEEInterface::LogErrorToHost(name);
                GCToEEInterface::LogErrorToHost("LXR configuration is present but empty.");
                return false;
            }
        }
        const char* text = nullptr;
        bool privateValue = GCToEEInterface::GetStringConfigValue(name, nullptr, &text);
        if (!privateValue && !GCToEEInterface::GetStringConfigValue(name, publicKey, &text))
        {
            int64_t integer;
            if (!GCToEEInterface::GetIntConfigValue(name, publicKey, &integer))
            {
                return true;
            }
            if (specified != nullptr) { *specified = true; }
            if (integer >= 0 && static_cast<uint64_t>(integer) <= maximum)
            {
                *value = static_cast<size_t>(integer);
                return true;
            }
            GCToEEInterface::LogErrorToHost(name);
            GCToEEInterface::LogErrorToHost("LXR configuration value is out of range.");
            return false;
        }
        if (specified != nullptr) { *specified = true; }
        if (!privateValue)
        {
            base = 10;
        }
        if (boolean && (strcmp(text, "true") == 0 || strcmp(text, "false") == 0))
        {
            size_t parsed = strcmp(text, "true") == 0 ? 1 : 0;
            GCToEEInterface::FreeStringConfigValue(text);
            if (parsed > maximum)
            {
                GCToEEInterface::LogErrorToHost(name);
                GCToEEInterface::LogErrorToHost("LXR configuration requests an unsupported feature.");
                return false;
            }
            *value = parsed;
            return true;
        }
        const char* cursor = text;
        if (base == 16 && cursor[0] == '0' && (cursor[1] == 'x' || cursor[1] == 'X'))
        {
            cursor += 2;
        }
        size_t parsed = 0;
        bool valid = *cursor != '\0';
        while (valid && *cursor != '\0')
        {
            unsigned digit = *cursor >= '0' && *cursor <= '9' ? *cursor - '0' :
                *cursor >= 'a' && *cursor <= 'f' ? *cursor - 'a' + 10 :
                *cursor >= 'A' && *cursor <= 'F' ? *cursor - 'A' + 10 : base;
            if (digit >= base || digit > maximum || parsed > (maximum - digit) / base)
            {
                valid = false;
                break;
            }
            parsed = parsed * base + digit;
            cursor++;
        }
        GCToEEInterface::FreeStringConfigValue(text);
        if (!valid)
        {
            GCToEEInterface::LogErrorToHost(name);
            GCToEEInterface::LogErrorToHost("LXR configuration must be an unsigned integer in the documented range.");
            return false;
        }
        *value = parsed;
        return true;
    }

    bool ReadBooleanSetting(const char* name, bool* value)
    {
        size_t parsed = *value ? 1 : 0;
        if (!ReadSetting(name, &parsed, 10, 1, nullptr, true))
        {
            return false;
        }
        *value = parsed != 0;
        return true;
    }
}

LxrGcHeap* LxrGcHeap::s_heap = nullptr;

struct LxrGcHeap::ScanCostScope
{
    LxrGcHeap* heap;
    size_t metric;
    int64_t start = 0;
    uint64_t cpu = 0;

    static uint64_t CpuNanoseconds(LxrGcHeap* heap)
    {
#ifdef TARGET_WINDOWS
        FILETIME created, exited, kernel, user;
        if (!GetThreadTimes(GetCurrentThread(), &created, &exited, &kernel, &user))
        {
            heap->Fatal("LXR scan diagnostics could not read current-thread CPU time.");
        }
        return ((static_cast<uint64_t>(kernel.dwHighDateTime) << 32 | kernel.dwLowDateTime) +
            (static_cast<uint64_t>(user.dwHighDateTime) << 32 | user.dwLowDateTime)) * 100;
#else
        timespec time{};
        if (clock_gettime(CLOCK_THREAD_CPUTIME_ID, &time) != 0)
        {
            heap->Fatal("LXR scan diagnostics could not read current-thread CPU time.");
        }
        return static_cast<uint64_t>(time.tv_sec) * 1000000000 + time.tv_nsec;
#endif // TARGET_WINDOWS
    }

    ScanCostScope(LxrGcHeap* owner, size_t index) : heap(owner), metric(index)
    {
        if (heap->m_storage_diagnostics_enabled.load(std::memory_order_relaxed))
        {
            cpu = CpuNanoseconds(heap);
            start = minipal_hires_ticks();
        }
    }

    ~ScanCostScope()
    {
        if (start != 0)
        {
            heap->m_scan_metrics[metric].fetch_add(
                minipal_hires_ticks() - start, std::memory_order_relaxed);
            heap->m_scan_cpu_nanoseconds[metric].fetch_add(
                CpuNanoseconds(heap) - cpu, std::memory_order_relaxed);
        }
    }
};

void LxrGcHeap::CountScanWork(size_t metric)
{
    if (m_storage_diagnostics_enabled.load(std::memory_order_relaxed))
    {
        m_scan_metrics[metric].fetch_add(1, std::memory_order_relaxed);
    }
}

struct LxrGcHeap::WalkContext
{
    LxrGcHeap* heap;
    walk_fn callback;
    void* context;
};

LxrGcHeap::LxrGcHeap()
    : m_engine(),
      m_enumeration{ObjectReferenceEnumerationRequestStatus::NotProcessed,
          ObjectReferenceEnumerationRequest::Enabled, nullptr},
      m_frozen{}, m_frozen_readers(0), m_cards(nullptr), m_bundles(nullptr),
      m_collection_lock(), m_gc_done(), m_trace_signal(), m_coordinator_done(), m_concurrent_done(),
      m_initialized(false), m_in_gc(false), m_shutdown(false), m_concurrent_enabled(true),
      m_collections(0), m_memory_info{}, m_memory_sequence{},
      m_current_pause_kind(LxrPauseKind::RefCount), m_heap_before(0), m_fragmentation_before(0),
      m_trace_heap_before(0), m_trace_fragmentation_before(0), m_initial_pause_ticks(0),
      m_scan_failure(LxrRcResult::Success), m_latency(1), m_loh_mode(1),
      m_total_pause_ticks(0), m_last_pause_ticks(0), m_pause_start(0), m_start_time(0),
      m_last_gc_start(0), m_last_gc_duration(0), m_last_heap_size(0), m_last_committed(0),
      m_last_fragmentation(0), m_last_memory_load(0), m_reserved_limit(0),
      m_barrier_bootstraps(0)
#ifdef TARGET_WINDOWS
      , m_process_exiting(nullptr)
#endif // TARGET_WINDOWS
{
    s_heap = this;
    m_header_bits.request = ObjectHeaderBitsRequest::Enabled;
    m_header_bits.version = GC_OBJECT_HEADER_BITS_PARAMETERS_VERSION;
    m_header_bits.size = sizeof(ObjectHeaderBitsParameters);
    m_header_bits.requested_bit_count = 2;
    m_header_bits.requested_state_count = 3;
    m_header_bits.requested_protocol = ObjectHeaderBitsProtocol::ClaimAndPublish;
    m_header_bits.required_atomic_operations =
        static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::Load) |
        static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::CompareExchange) |
        static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::Store) |
        static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::Wait);
    m_header_bits.required_memory_order = ObjectHeaderBitsMemoryOrder::SequentiallyConsistent;
}

void LxrGcHeap::Fatal(const char* message) const
{
    GCToEEInterface::LogErrorToHost(message);
    GCToEEInterface::HandleFatalError(static_cast<unsigned>(E_FAIL));
    abort();
}

void LxrGcHeap::Unsupported(const char* operation) const
{
    GCToEEInterface::LogErrorToHost(operation);
}

HRESULT LxrGcHeap::Initialize()
{
#ifdef TARGET_WINDOWS
    m_process_exiting = reinterpret_cast<BOOLEAN (WINAPI*)()>(
        GetProcAddress(GetModuleHandleW(L"ntdll.dll"), "RtlDllShutdownInProgress"));
    if (m_process_exiting == nullptr)
    {
        Unsupported("LXR cannot establish Windows process-exit reader lifetime.");
        return E_NOTIMPL;
    }
#endif // TARGET_WINDOWS
    if (m_enumeration.request_status != ObjectReferenceEnumerationRequestStatus::Accepted ||
        m_enumeration.get_loader_allocator_object_slot == nullptr ||
        m_header_bits.request_status != ObjectHeaderBitsRequestStatus::Accepted ||
        m_header_bits.object_byte_offset != -static_cast<int32_t>(sizeof(ObjHeader)) ||
        m_header_bits.storage_word_size != sizeof(uint32_t) ||
        m_header_bits.clear_state != 0 || m_header_bits.transition_state != 2 ||
        m_header_bits.published_state != 3)
    {
        Unsupported("LXR requires accepted allocation/reference-enumeration contracts.");
        return E_NOTIMPL;
    }
    if (!m_collection_lock.Initialize() || !m_gc_done.CreateManualEventNoThrow(true) ||
        !m_trace_signal.CreateAutoEventNoThrow(false) || !m_coordinator_done.CreateManualEventNoThrow(false) ||
        !m_concurrent_done.CreateManualEventNoThrow(true) ||
        !m_scan_signal.CreateAutoEventNoThrow(false) || !m_scan_ready.CreateManualEventNoThrow(false) ||
        !m_scan_done.CreateAutoEventNoThrow(false) || !m_scan_exited.CreateManualEventNoThrow(false))
    {
        return E_OUTOFMEMORY;
    }
    // P28 (F10). The default is 2 bits, as in the Java LXR (util/rc.rs: LOG_REF_COUNT_BITS = 1). The setting takes 2, 4 or 8.
    size_t bits = 2;
    if (!ReadSetting("LXRRCBits", &bits, 10, 8))
    {
        return E_FAIL;
    }
    if (bits != 2 && bits != 4 && bits != 8)
    {
        Unsupported("LXRRCBits must be 2, 4 or 8.");
        return E_FAIL;
    }
    LxrRcConfiguration configuration{
        ImmixBytes, LargeBytes, MaximumMutators, MaximumRoots, 4, static_cast<uint8_t>(bits),
    };
    configuration.evacuateNursery = true;
    configuration.evacuateMature = true;
    configuration.forwardingMask = m_header_bits.bit_mask;
    configuration.forwardingShift = m_header_bits.bit_shift;
    size_t immixInitial = 8;
    size_t largeInitial = 16;
    size_t immixReserve = 128;
    size_t largeReserve = 256;
    bool legacy = false;
    size_t youngMB = SIZE_MAX >> 20;
    size_t nurseryRatio = SIZE_MAX;
    size_t managedLimitMB = 0;
    size_t nativePlaneLimitMB = 4096;
    bool noRecycling = false;
    bool noMutatorRecycling = false;
    bool overshoot = true;
    bool phaseTimers = true;
    bool immixReserveSpecified = false;
    bool largeReserveSpecified = false;
    if (!ReadBooleanSetting("LXRLegacyCapacity", &legacy) ||
        !ReadSetting("LXRImmixInitialMB", &immixInitial, 10, LxrSideMetadataLayout::AddressLimit >> 20) ||
        !ReadSetting("LXRLOSInitialMB", &largeInitial, 10, LxrSideMetadataLayout::AddressLimit >> 20) ||
        !ReadSetting("LXRImmixReserveMB", &immixReserve, 10,
            LxrSideMetadataLayout::AddressLimit >> 20, &immixReserveSpecified) ||
        !ReadSetting("LXRLOSReserveMB", &largeReserve, 10,
            LxrSideMetadataLayout::AddressLimit >> 20, &largeReserveSpecified) ||
        !ReadSetting("LXRManagedHeapHardLimitMB", &managedLimitMB, 10, LxrSideMetadataLayout::AddressLimit >> 20) ||
        !ReadSetting("LXRNativePlaneLimitMB", &nativePlaneLimitMB, 10, SIZE_MAX >> 20) ||
        !ReadSetting("LXRTraceThreshold", &m_policy_configuration.traceThreshold, 10, 100) ||
        !ReadSetting("LXRRCStopPercent", &m_policy_configuration.rcStopPercent, 10, 100) ||
        !ReadSetting("LXRMaxSurvivalMB", &m_policy_configuration.maxSurvivalMB, 10, SIZE_MAX >> 20) ||
        !ReadSetting("LXRIncrementsLimit", &m_policy_configuration.incrementsLimit) ||
        !ReadSetting("LXRNurseryBlocks", &m_policy_configuration.nurseryBlocks) ||
        !ReadSetting("LXRYoungLimitMB", &youngMB, 10, SIZE_MAX >> 20) ||
        !ReadSetting("LXRNurseryRatio", &nurseryRatio, 10, SIZE_MAX - 1) ||
        !ReadSetting("LXRMinReuseLines", &configuration.minimumReuseLines, 10, ImmixBlockGeometry::LinesPerBlock) ||
        !ReadSetting("LXRBufferSize", &configuration.bufferSize, 10, LxrReferenceCounting::BufferCapacity) ||
        !ReadSetting("LXRBufferBytes", &configuration.allocationBufferBytes, 10, ImmixBlockGeometry::BlockBytes) ||
        !ReadBooleanSetting("LXRNoLineRecycling", &noRecycling) ||
        !ReadBooleanSetting("LXRNoMutatorLineRecycling", &noMutatorRecycling) ||
        !ReadBooleanSetting("LXROvershootToSpace", &overshoot) ||
        !ReadBooleanSetting("LXRLazyDecrements", &configuration.lazyDecrements) ||
        !ReadBooleanSetting("LXRWaitForLazy", &m_wait_for_lazy) ||
        !ReadBooleanSetting("LXRFastAllocation", &m_fast_enabled) ||
        !ReadBooleanSetting("LXREvacuateNursery", &configuration.evacuateNursery) ||
        !ReadBooleanSetting("LXREvacuateMature", &configuration.evacuateMature) ||
        !ReadSetting("LXRDefragHeadroomPercent", &configuration.defragHeadroomPercent, 10, 50) ||
        !ReadSetting("LXRChunkDefragPercent", &configuration.chunkDefragPercent, 10, 100) ||
        !ReadSetting("LXRMaxYoungEvacSize", &configuration.maximumYoungCopyBytes) ||
        !ReadBooleanSetting("LXRSurvivalPredictorWeighted", &m_policy_configuration.weighted) ||
        !ReadBooleanSetting("LXRSurvivalPredictorHarmonicMean", &m_policy_configuration.harmonic) ||
        !ReadBooleanSetting("LXRPhaseTimers", &phaseTimers))
    {
        return E_FAIL;
    }
    size_t gcThreads = 0;
    size_t concurrentThreads = 0;
    if (!ReadSetting("LXRGCThreads", &gcThreads, 10, LxrWorkerPolicy::MaximumWorkers) ||
        !ReadSetting("LXRConcGCThreads", &concurrentThreads, 10, LxrWorkerPolicy::MaximumWorkers))
    {
        return E_FAIL;
    }
    // P25 (F4). The worker count of the Java LXR (ParallelGCThreads) from the number of processors. A setting of 0 or no
    // setting selects the rule. P26 (F2, F4b). The concurrent trace runs on ConcGCThreads of these workers, as in the Java LXR
    // (LxrWorkerPolicy::ConcurrentThreads, 3 of 13), with a floor of 2 on small machines. This became the default when a pause no longer walks the trace backlog:
    // Measured, list suffix nursery with 3 trace workers needs the same time as with all workers. LXRConcGCThreads overrides it.
    configuration.workers = gcThreads != 0
        ? gcThreads : LxrWorkerPolicy::ParallelThreads(GCToEEInterface::GetCurrentProcessCpuCount());
    size_t defaultConcurrent = LxrWorkerPolicy::DefaultConcurrentTraceThreads(configuration.workers);
    configuration.concurrentTraceWorkers = concurrentThreads != 0 ? concurrentThreads : defaultConcurrent;
    if (configuration.concurrentTraceWorkers > configuration.workers)
    {
        configuration.concurrentTraceWorkers = configuration.workers;
    }
    m_engine.GetPauseTimers().Configure(phaseTimers);
    for (const char* unsupported : {
            "LXRMaxPauseMillis", "LXRTransparentHugepage",
            "GCHeapHardLimit", "GCHeapHardLimitPercent",
            "GCHeapHardLimitSOH", "GCHeapHardLimitLOH", "GCHeapHardLimitPOH",
            "GCHeapHardLimitSOHPercent", "GCHeapHardLimitLOHPercent", "GCHeapHardLimitPOHPercent"})
    {
        size_t value = 0;
        if (!ReadSetting(unsupported, &value, 10, 0, nullptr, true))
        {
            Unsupported("LXR does not support this setting. Standard GC hard limits include bookkeeping; use LXRManagedHeapHardLimitMB for the explicitly managed-capacity ceiling.");
            return E_NOTIMPL;
        }
    }
    if (legacy)
    {
        if ((immixReserveSpecified && immixReserve != immixInitial) ||
            (largeReserveSpecified && largeReserve != largeInitial))
        {
            Unsupported("LXRLegacyCapacity conflicts with the explicit reservation.");
            return E_FAIL;
        }
        immixReserve = immixInitial;
        largeReserve = largeInitial;
    }
    if (immixInitial == 0 || largeInitial == 0 || immixReserve < immixInitial ||
        largeReserve < largeInitial ||
        immixReserve > (LxrSideMetadataLayout::AddressLimit >> 20) - largeReserve ||
        configuration.minimumReuseLines == 0 ||
        configuration.allocationBufferBytes % ImmixBlockGeometry::LineBytes != 0 ||
        (configuration.bufferSize != 256 && configuration.bufferSize != 512 && configuration.bufferSize != 1024) ||
        m_policy_configuration.maxSurvivalMB == 0 || m_policy_configuration.incrementsLimit == 0 ||
        m_policy_configuration.nurseryBlocks == 0 || youngMB == 0 || nativePlaneLimitMB == 0)
    {
        Unsupported("LXR capacity and trigger limits must be positive, aligned and bounded.");
        return E_FAIL;
    }
    configuration.immixBytes = immixInitial << 20;
    configuration.largeObjectBytes = largeInitial << 20;
    configuration.immixReservationBytes = immixReserve << 20;
    configuration.largeObjectReservationBytes = largeReserve << 20;
    size_t reservation = configuration.immixReservationBytes + configuration.largeObjectReservationBytes;
    m_hard_limit = managedLimitMB == 0 ? reservation : managedLimitMB << 20;
    if (m_hard_limit < configuration.immixBytes + configuration.largeObjectBytes ||
        m_hard_limit > reservation ||
        (m_reserved_limit != 0 && reservation > m_reserved_limit))
    {
        Unsupported("LXR initial capacity must fit the hard limit, and the hard limit must fit the reservation.");
        return E_OUTOFMEMORY;
    }
    configuration.hardLimitBytes = m_hard_limit;
    m_native_plane_limit = nativePlaneLimitMB << 20;
    configuration.lineRecycling = !noRecycling && !noMutatorRecycling;
    configuration.copyLineRecycling = !noRecycling;
    // The engine enables overshoot only when there is no hard limit and the reservation is larger than the initial
    // capacity; this setting only switches it off. A hard limit below the reservation always disables it.
    configuration.overshootToSpace = overshoot;
    m_policy_configuration.youngLimitBytes = youngMB == (SIZE_MAX >> 20) ? SIZE_MAX : youngMB << 20;
    if (nurseryRatio != SIZE_MAX)
    {
        m_policy_configuration.nurseryBlocks =
            m_hard_limit / ImmixBlockGeometry::BlockBytes / (nurseryRatio + 1);
        if (m_policy_configuration.nurseryBlocks == 0)
        {
            Unsupported("LXRNurseryRatio leaves no nursery block.");
            return E_FAIL;
        }
    }
    m_policy.Configure(m_policy_configuration);
    m_heap_configuration = configuration;
    LxrPauseRuntimeAdapter runtime{Suspend, Resume, this, true};
    LxrRcObjectModel model{
        m_enumeration.get_loader_allocator_object_slot, true, FinishTrace, this, IsImmortal, FinishEpoch,
        nullptr, FinishLazy, CanAdmitRange,
    };
    model.finishRelocation = FinishRelocation;
    if (m_engine.Initialize(configuration, runtime, &model) != LxrRcResult::Success)
    {
        Unsupported("LXR could not reserve its owned heap, metadata or worker resources.");
        return E_OUTOFMEMORY;
    }
    if (m_engine.GetNativePlaneCommittedBytes() > m_native_plane_limit)
    {
        m_engine.Shutdown();
        Unsupported("LXR initial native planes exceed LXRNativePlaneLimitMB.");
        return E_OUTOFMEMORY;
    }
    m_cards = new (std::nothrow) uint8_t[m_engine.GetReservationBytes() / 2048 + 1]{};
    m_bundles = new (std::nothrow) uint8_t[m_engine.GetReservationBytes() / (2 * 1024 * 1024) + 1]{};
    if (m_cards == nullptr || m_bundles == nullptr)
    {
        m_engine.Shutdown();
        return E_OUTOFMEMORY;
    }
    g_gc_lowest_address = reinterpret_cast<uint8_t*>(m_engine.GetReservationStart());
    g_gc_highest_address = g_gc_lowest_address + m_engine.GetReservationBytes();
    g_gc_card_table = reinterpret_cast<uint32_t*>(
        reinterpret_cast<uintptr_t>(m_cards) - (m_engine.GetReservationStart() >> 11));
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    g_gc_card_bundle_table = reinterpret_cast<uint32_t*>(
        reinterpret_cast<uintptr_t>(m_bundles) - (m_engine.GetReservationStart() >> 21));
#endif
    g_gc_pFreeObjectMethodTable = GCToEEInterface::GetFreeObjectMethodTable();
    g_num_processors = 1;
    WriteBarrierParameters parameters{};
    parameters.operation = WriteBarrierOp::Initialize;
    parameters.is_runtime_suspended = true;
    parameters.card_table = g_gc_card_table;
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    parameters.card_bundle_table = g_gc_card_bundle_table;
#endif
    parameters.lowest_address = g_gc_lowest_address;
    parameters.highest_address = g_gc_highest_address;
    parameters.ephemeral_low = g_gc_lowest_address;
    parameters.ephemeral_high = g_gc_highest_address;
    // The runtime builds the barrier from GetAssignRefFunctions and keeps its patchable slots in sync with these
    // values; they must be set before the runtime installs the helpers.
    LxrWriteBarrierSetGlobals(
        m_engine.GetFieldMetadataBase(),
        m_engine.GetReservationStart(),
        m_engine.GetReservationBytes(),
        reinterpret_cast<void*>(&LxrBarrierSlowPath),
        LxrWriteBarrierComputeXsaveMask());
    GCToEEInterface::StompWriteBarrier(&parameters);
    gc_alloc_context* context = GCToEEInterface::GetAllocContext();
    if (context != nullptr && FindOrAttach(context, false) == nullptr)
    {
        m_engine.Shutdown();
        return E_OUTOFMEMORY;
    }
    m_start_time = minipal_lowres_ticks();
    GCScan::GcRuntimeStructuresValid(TRUE);
    if (!GCToEEInterface::CreateThread(ScanOnGcThread, this, false, "LXR root/lifetime scan"))
    {
        m_engine.Shutdown();
        Unsupported("LXR could not create its registered scan executor.");
        return E_OUTOFMEMORY;
    }
    if (m_scan_ready.Wait(Timeout, false) != WAIT_OBJECT_0)
    {
        Fatal("LXR scan executor did not become ready.");
    }
    if (!GCToEEInterface::CreateThread(CoordinateTrace, this, false, "LXR trace coordinator"))
    {
        m_scan_stopping.store(true, std::memory_order_release);
        m_scan_signal.Set();
        if (m_scan_exited.Wait(Timeout, false) != WAIT_OBJECT_0)
        {
            Fatal("LXR scan executor did not exit after coordinator creation failed.");
        }
        m_engine.Shutdown();
        return E_OUTOFMEMORY;
    }
    m_initialized.store(true, std::memory_order_release);
    return S_OK;
}

LxrRcMutator* LxrGcHeap::FindOrAttach(gc_alloc_context* context, bool barrier)
{
    if (context == nullptr)
    {
        Fatal("LXR managed operation has no runtime allocation context.");
    }
    LxrRcMutator* mutator = m_engine.FindCachedMutator(context->gc_reserved_1, context);
    if (mutator == nullptr)
    {
        mutator = m_engine.FindMutator(context);
    }
    if (mutator == nullptr)
    {
        LxrRcResult result = m_engine.AttachMutator(context, &mutator);
        if (result == LxrRcResult::CapacityExceeded && !barrier)
        {
            return nullptr;
        }
        if (result != LxrRcResult::Success)
        {
            Fatal("LXR mutator admission failed; the prototype supports at most 1024 simultaneous contexts.");
        }
        if (barrier)
        {
            m_barrier_bootstraps.fetch_add(1, std::memory_order_relaxed);
        }
    }
    context->gc_reserved_1 = mutator;
    return mutator;
}

Object* LxrGcHeap::Alloc(gc_alloc_context* context, size_t size, uint32_t flags)
{
    // The stock helpers bump alloc_ptr inside the buffer that this method gave to the thread, and call it only when the
    // buffer cannot hold the object, or for an object that needs the slow path (a finalizable object, a sampled
    // allocation, a large or pinned-object-heap object). A small object that fits is taken in place: the memory is
    // zero already and the buffer is still prepared and counted.
    if ((flags & GC_ALLOC_USER_OLD_HEAP) == 0 && context->alloc_ptr != nullptr &&
        size >= MIN_OBJECT_SIZE && size < LxrLargeObjectSpace::MinimumObjectBytes)
    {
        size_t aligned = (size + sizeof(uintptr_t) - 1) & ~(sizeof(uintptr_t) - 1);
        if (static_cast<size_t>(context->alloc_limit - context->alloc_ptr) >= aligned)
        {
            Object* inPlace = reinterpret_cast<Object*>(context->alloc_ptr);
            context->alloc_ptr += aligned;
            RegisterFinalizable(inPlace, flags);
            return inPlace;
        }
    }
    RetireAllocationSpan(context, true);
    if (size < MIN_OBJECT_SIZE || size > SIZE_MAX - (sizeof(uintptr_t) - 1))
    {
        return nullptr;
    }
    size = (size + sizeof(uintptr_t) - 1) & ~(sizeof(uintptr_t) - 1);
    if (!m_shutdown.load(std::memory_order_acquire))
    {
        if (GetCollectionCause(false) != LxrCollectionCause::None)
        {
            if (CollectForPressure(false, false) != LxrRcResult::Success)
            {
                Fatal("LXR allocation-pressure collection failed.");
            }
        }
    }
    LxrRcMutator* mutator = FindOrAttach(context, false);
    if (mutator == nullptr && !m_shutdown.load())
    {
        if (CollectForPressure(true, false) != LxrRcResult::Success)
        {
            Fatal("LXR admission-retry collection failed.");
        }
        mutator = FindOrAttach(context, false);
    }
    if (mutator == nullptr)
    {
        return nullptr;
    }
    Object* object = nullptr;
    // A pinned-object-heap object of a small size goes into a pinned block. The engine keeps those blocks apart, so
    // the pause needs no per-object flag to know that the object stays.
    bool pinned = (flags & GC_ALLOC_PINNED_OBJECT_HEAP) != 0;
    LxrRcResult result = m_engine.Allocate(mutator, size, &object, pinned);
    if (result == LxrRcResult::AllocationFailure && !m_shutdown.load())
    {
        // The lazy sweep after the last pause makes promoted blocks Reusable. A collection is needed only if the
        // allocation still fails after it has finished.
        bool cooperative = EnterCollectionLock();
        LxrRcResult drained = UpdatePolicyHints();
        LeaveCollectionLock(cooperative);
        if (drained != LxrRcResult::Success)
        {
            Fatal("LXR lazy sweep did not finish before an allocation retry.");
        }
        m_engine.ResetAllocationCursors();
        result = m_engine.Allocate(mutator, size, &object, pinned);
    }
    if (result == LxrRcResult::AllocationFailure && !m_shutdown.load())
    {
        if (CollectForPressure(false, false, true) != LxrRcResult::Success)
        {
            Fatal("LXR space-pressure collection failed before growth admission.");
        }
        result = m_engine.Allocate(mutator, size, &object, pinned);
    }
    if (result == LxrRcResult::AllocationFailure && !m_shutdown.load())
    {
        LxrRcResult grown = TryGrow(size >= LxrLargeObjectSpace::MinimumObjectBytes, size);
        if (grown == LxrRcResult::Success)
        {
            result = m_engine.Allocate(mutator, size, &object, pinned);
        }
        else if (grown != LxrRcResult::AllocationFailure && grown != LxrRcResult::OutOfMemory)
        {
            Fatal("LXR growth failed a storage admission invariant.");
        }
    }
    if (result == LxrRcResult::AllocationFailure && !m_shutdown.load())
    {
        if (CollectForPressure(true, false) != LxrRcResult::Success)
        {
            Fatal("LXR emergency full collection failed.");
        }
        result = m_engine.Allocate(mutator, size, &object, pinned);
    }
    if (result == LxrRcResult::AllocationFailure)
    {
        return nullptr;
    }
    if (result != LxrRcResult::Success)
    {
        Fatal("LXR owned allocator failed an allocation invariant.");
    }
    if ((flags & GC_ALLOC_USER_OLD_HEAP) != 0)
    {
        context->alloc_bytes_uoh += static_cast<int64_t>(size);
    }
    else
    {
        context->alloc_bytes += static_cast<int64_t>(size);
    }
    m_context_allocated_bytes.fetch_add(size, std::memory_order_relaxed);
    if (m_fast_enabled && size < LxrLargeObjectSpace::MinimumObjectBytes &&
        (flags & GC_ALLOC_USER_OLD_HEAP) == 0)
    {
        uintptr_t cursor;
        uintptr_t limit;
        if (m_engine.GetAllocationSpan(mutator, &cursor, &limit) != LxrRcResult::Success)
        {
            Fatal("LXR could not publish its initialized Immix allocation span.");
        }
        if (cursor != 0)
        {
            context->alloc_ptr = reinterpret_cast<uint8_t*>(cursor + sizeof(ObjHeader));
            context->alloc_limit = reinterpret_cast<uint8_t*>(limit + sizeof(ObjHeader));
            context->alloc_bytes += static_cast<int64_t>(limit - cursor);
            m_context_allocated_bytes.fetch_add(limit - cursor, std::memory_order_relaxed);
            m_fast_refills.fetch_add(1, std::memory_order_relaxed);
        }
    }
    RegisterFinalizable(object, flags);
    return object;
}

// A finalizable object is registered when it is allocated, as the standard collector does. The registry never reads the
// object before a pause, and a thread cannot be suspended between the allocation and the point where the runtime has
// written the MethodTable, so a pause sees a complete object. The critical bit of the finalizer is taken from the
// MethodTable when the pause captures the entry (see CaptureFinalizers). Objects of a collectible type, large objects
// and pinned-object-heap objects reach this method for each object too, so they need no further hook: the pause
// reads the type of an object when it first counts it.
void LxrGcHeap::RegisterFinalizable(Object* object, uint32_t flags)
{
    if ((flags & GC_ALLOC_FINALIZE) != 0 && !m_registry.Register(object, false))
    {
        Fatal("LXR could not register an object for finalization.");
    }
}

bool LxrGcHeap::CanAdmitRange(size_t, size_t, size_t nativeBytes, void* context)
{
    // The engine reports all native planes. The collector keeps no per-object table of its own since P17 S7.
    LxrGcHeap* heap = static_cast<LxrGcHeap*>(context);
    return nativeBytes <= heap->m_native_plane_limit;
}

LxrRcResult LxrGcHeap::TryGrow(bool large, size_t size)
{
    bool cooperative = EnterCollectionLock();
    LxrRcResult result = UpdatePolicyHints();
    if (result == LxrRcResult::Success)
    {
        result = m_engine.RequestGrowth(large, size);
    }
    if (result == LxrRcResult::Success)
    {
        result = CollectLocked(m_policy.Select(
            m_engine.GetTracePhase() == LxrTracePhase::Concurrent, m_engine.IsTraceExhausted(),
            false, false, m_concurrent_enabled.load()), m_policy.EmergencyHint(), true);
        if (result == LxrRcResult::Success)
        {
            result = m_engine.GetGrowthResult();
        }
    }
    LeaveCollectionLock(cooperative);
    if (result == LxrRcResult::OutOfMemory)
    {
        Unsupported("LXR growth could not admit the minimum native planes or commit requested storage.");
    }
    return result;
}

void LxrGcHeap::RetireAllocationSpan(gc_alloc_context* context, bool subtractUnused)
{
    if (context == nullptr || context->alloc_ptr == nullptr)
    {
        return;
    }
    LxrRcMutator* mutator = m_engine.FindCachedMutator(context->gc_reserved_1, context);
    if (mutator == nullptr || context->alloc_limit < context->alloc_ptr ||
        m_engine.RetireAllocationSpan(mutator,
            reinterpret_cast<uintptr_t>(context->alloc_ptr) - sizeof(ObjHeader)) != LxrRcResult::Success)
    {
        Fatal("LXR allocation span contains a foreign or inconsistent used point.");
    }
    size_t unused = static_cast<size_t>(context->alloc_limit - context->alloc_ptr);
    m_unused_span_bytes.fetch_add(unused, std::memory_order_relaxed);
    if (subtractUnused)
    {
        context->alloc_bytes -= static_cast<int64_t>(unused);
        m_context_allocated_bytes.fetch_sub(unused, std::memory_order_relaxed);
    }
    context->alloc_ptr = nullptr;
    context->alloc_limit = nullptr;
}

void LxrGcHeap::RetireAllocationContext(gc_alloc_context* context, void* heap)
{
    static_cast<LxrGcHeap*>(heap)->RetireAllocationSpan(context, true);
}

// Logs the old value of a field that has not been logged in this epoch. The old value is read BEFORE the claim in
// LogBeforeWrite: a racing writer that loses the claim stores only after the winner claimed, and the winner has
// read the field by then.
void LxrGcHeap::LogOldValue(Object** slot)
{
    LxrGcHeap* heap = s_heap;
    Object* oldTarget = VolatileLoad(slot);
    LxrRcMutator* mutator = heap->FindOrAttach(GCToEEInterface::GetAllocContext(), true);
    if (heap->m_engine.LogBeforeWrite(mutator, slot, oldTarget) != LxrRcResult::Success)
    {
        heap->Fatal("LXR pre-store logging failed before the managed field was overwritten.");
    }
}

void LxrGcHeap::BarrierSlowPath(Object** slot)
{
    LogOldValue(slot);
}

// Logs every field of the range whose unlog bit is set. The side metadata has one bit for every 8-byte slot, so a
// metadata byte covers eight fields; a range of fields that are all logged costs one byte load for eight slots.
void LxrGcHeap::LogOldValues(Object** destination, size_t count)
{
    const uint8_t* metadata = reinterpret_cast<const uint8_t*>(g_lxr_write_barrier_globals.metadata_base);
    uintptr_t first = reinterpret_cast<uintptr_t>(destination) >> 3;
    uintptr_t end = first + count;
    for (uintptr_t index = first; index < end;)
    {
        uintptr_t byteIndex = index >> 3;
        unsigned startBit = static_cast<unsigned>(index & 7);
        unsigned endBit = (byteIndex == ((end - 1) >> 3)) ? static_cast<unsigned>(((end - 1) & 7) + 1) : 8;
        unsigned bits = VolatileLoad(metadata + byteIndex);
        bits &= (0xFFu << startBit) & ((1u << endBit) - 1);
        for (unsigned bit = startBit; bits != 0 && bit < endBit; bit++)
        {
            if ((bits & (1u << bit)) != 0)
            {
                LogOldValue(reinterpret_cast<Object**>(((byteIndex << 3) + bit) << 3));
            }
        }
        index = (byteIndex + 1) << 3;
    }
}

bool LxrGcHeap::IsInHeap(void*, void* address)
{
    return reinterpret_cast<uintptr_t>(address) - g_lxr_write_barrier_globals.heap_low <
        g_lxr_write_barrier_globals.heap_size;
}

// The pre-store hook of the PR (check_escape), used by the runtime sites that store a reference without the JIT
// barrier. It is called BEFORE the store and reads the old value of *destination.
void LxrGcHeap::CheckEscape(Object** destination, Object*)
{
    if (!IsInHeap(nullptr, destination))
    {
        return;
    }
    const uint8_t* metadata = reinterpret_cast<const uint8_t*>(g_lxr_write_barrier_globals.metadata_base);
    uintptr_t address = reinterpret_cast<uintptr_t>(destination);
    if ((VolatileLoad(metadata + (address >> 6)) >> ((address >> 3) & 7) & 1) != 0)
    {
        LogOldValue(destination);
    }
}

// LXR logs before the store, so the post-store hook of the PR has nothing to do.
void LxrGcHeap::NoPostStoreBarrier(void*, Object**, Object*)
{
}

void LxrGcHeap::PreStoreRange(void*, Object** destination, Object**, size_t count)
{
    if (count != 0 && IsInHeap(nullptr, destination))
    {
        LogOldValues(destination, count);
    }
}

// The bulk-move hook of the PR, for ranges that hold only references. Old values are logged first, then the
// references are copied one pointer at a time (a concurrent reader never sees a torn reference).
void LxrGcHeap::BulkMoveWithWriteBarrier(void*, void* destination, const void* source, size_t length)
{
    if (destination == source || length == 0)
    {
        return;
    }
    if (IsInHeap(nullptr, destination))
    {
        LogOldValues(static_cast<Object**>(destination), length / sizeof(Object*));
    }
    size_t count = length / sizeof(uintptr_t);
    uintptr_t* to = static_cast<uintptr_t*>(destination);
    const uintptr_t* from = static_cast<const uintptr_t*>(source);
    if (reinterpret_cast<uintptr_t>(destination) - reinterpret_cast<uintptr_t>(source) >= length)
    {
        for (size_t index = 0; index < count; index++)
        {
            VolatileStore(to + index, VolatileLoad(from + index));
        }
    }
    else
    {
        for (size_t index = count; index != 0; index--)
        {
            VolatileStore(to + index - 1, VolatileLoad(from + index - 1));
        }
    }
}

// Entry of the assembly write barrier slow path. The assembly saves the whole vector state around this call.
extern "C" void LxrBarrierSlowPath(Object** slot)
{
    LxrGcHeap::BarrierSlowPath(slot);
}

void LxrGcHeap::FixAllocContext(gc_alloc_context* context, void*, void*)
{
    // The EE already charged the departing tail to dead_threads_non_alloc_bytes.
    RetireAllocationSpan(context, false);
    LxrRcMutator* mutator = m_engine.FindMutator(context);
    if (mutator != nullptr && m_engine.DetachMutator(mutator) != LxrRcResult::Success)
    {
        Fatal("LXR thread-detach buffer publication failed.");
    }
    context->gc_reserved_1 = nullptr;
    if (!m_shutdown.load() && m_engine.HasPendingDetaches() &&
        CollectPrototype(LxrPauseKind::RefCount) != LxrRcResult::Success)
    {
        Fatal("LXR detached allocator retirement failed before TLS reset.");
    }
}

bool LxrGcHeap::EnterCollectionLock()
{
    LxrPauseTimers& timers = m_engine.GetPauseTimers();
    int64_t waitStart = timers.Enabled() ? timers.Stamp() : 0;
    bool cooperative = GCToEEInterface::EnablePreemptiveGC();
    m_collection_lock.Enter();
    GCToEEInterface::DisablePreemptiveGC();
    timers.Begin(waitStart != 0 ? timers.Stamp() - waitStart : 0);
    return cooperative;
}

void LxrGcHeap::LeaveCollectionLock(bool cooperative)
{
    m_engine.GetPauseTimers().End();
    GCToEEInterface::EnablePreemptiveGC();
    m_collection_lock.Leave();
    if (cooperative)
    {
        GCToEEInterface::DisablePreemptiveGC();
    }
}

LxrRcResult LxrGcHeap::CollectLocked(LxrPauseKind kind, bool forcedBlocking, bool waitForLazy)
{
    if (m_shutdown.load(std::memory_order_acquire))
    {
        return LxrRcResult::InvalidPhase;
    }
    LxrRcResult hint = UpdatePolicyHints();
    if (hint != LxrRcResult::Success)
    {
        return hint;
    }
    if (kind == LxrPauseKind::InitialMark && m_engine.GetTracePhase() != LxrTracePhase::Inactive)
    {
        kind = LxrPauseKind::RefCount;
    }
    if (kind == LxrPauseKind::FinalMark && m_engine.GetTracePhase() != LxrTracePhase::Concurrent)
    {
        return m_engine.Drain(Timeout);
    }
    m_last_gc_start.store(GetNow());
    m_current_pause_kind = kind;
    m_forced_blocking = forcedBlocking;
    m_heap_before = m_last_heap_size.load();
    m_fragmentation_before = m_last_fragmentation.load();
    if (kind == LxrPauseKind::InitialMark)
    {
        m_trace_heap_before = m_heap_before;
        m_trace_fragmentation_before = m_fragmentation_before;
        m_concurrent_done.Reset();
    }
    LxrRcResult result;
    m_engine.GetPauseTimers().MarkPause(static_cast<uint8_t>(kind));
    switch (kind)
    {
        case LxrPauseKind::InitialMark: result = m_engine.BeginTrace(CaptureRoots, this, Timeout); break;
        case LxrPauseKind::FinalMark: result = m_engine.FinishTrace(CaptureRoots, this, Timeout); break;
        case LxrPauseKind::Full: result = m_engine.CollectFull(CaptureRoots, this, Timeout); break;
        default: result = m_engine.Collect(CaptureRoots, this, Timeout); break;
    }
    if (result == LxrRcResult::Success && kind != LxrPauseKind::InitialMark)
    {
        // P26 (F8). The Java LXR runs the lazy decrements and the lazy sweep on its workers while the program runs, and
        // nothing waits for them (plan/lxr/global.rs: schedule_collection only logs when the previous lazy jobs are not
        // finished). So the thread that made the pause goes back to the program at once. It waits for the lazy work only
        // when a decision needs its result: a Full pause or a forced or emergency collection (the Java LXR does their
        // decrements inside the pause), a caller that asks for it (a wait for the concurrent collection, a growth), the
        // next collection (UpdatePolicyHints, because the log banks are reused) and an allocation that found no memory
        // (Alloc waits for the lazy sweep before it collects). LXRWaitForLazy restores the old wait after every pause.
        bool wait = waitForLazy || forcedBlocking || kind == LxrPauseKind::Full || m_wait_for_lazy;
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F8_WAIT_CONTROL)
        wait = true;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F8_WAIT_CONTROL
        if (wait)
        {
            LxrPhaseScope postDrain(m_engine.GetPauseTimers(), LxrPauseTimers::PostDrain);
            result = m_engine.GetTracePhase() == LxrTracePhase::Concurrent
                ? LxrRcResult::Success : m_engine.Drain(Timeout);
        }
    }
    m_last_gc_duration.store(GetNow() - m_last_gc_start.load());
    if (result == LxrRcResult::Success && m_engine.GetTracePhase() == LxrTracePhase::Inactive)
    {
        m_concurrent_done.Set();
    }
    if (kind == LxrPauseKind::InitialMark)
    {
        m_trace_signal.Set();
    }
    return result;
}

LxrRcResult LxrGcHeap::CollectPrototype(LxrPauseKind kind, bool waitForLazy)
{
    bool cooperative = EnterCollectionLock();
    LxrRcResult result = CollectLocked(kind, false, waitForLazy);
    LeaveCollectionLock(cooperative);
    return result;
}

HRESULT LxrGcHeap::GarbageCollect(int, bool, int)
{
    if (CollectForPressure(false, true) != LxrRcResult::Success)
    {
        Fatal("LXR explicit full collection failed.");
    }
    return S_OK;
}

LxrCollectionCause LxrGcHeap::GetCollectionCause(bool heapFull) const
{
    size_t used = 0;
    size_t headroom = 0;
    if (m_engine.GetUsedPages(&used) != LxrRcResult::Success ||
        m_engine.GetDefragHeadroomBytes(&headroom) != LxrRcResult::Success)
    {
        Fatal("LXR could not read actual capacity for collection pressure.");
    }
    LxrPolicyPressure pressure{
        heapFull, m_engine.GetTracePhase() == LxrTracePhase::Concurrent, m_engine.IsTraceExhausted(),
        m_engine.GetYoungAllocationBytes(), m_engine.GetPendingIncrementEntries(),
        m_engine.GetCleanNurseryBlocks(), true, 0,
        used, m_engine.GetAdmittedBytes() / LxrLargeObjectSpace::PageBytes,
        m_heap_configuration.evacuateNursery ? 0 : GetCollectionReservedPages(headroom),
    };
    if (m_heap_configuration.evacuateNursery)
    {
        pressure.copyReservePages = GetCollectionReservedPages(headroom);
        pressure.immixFreePages = m_engine.GetImmixFreePages();
        // With overshoot the pause takes the to-space it needs from the reservation. The heap is full when no block is
        // free, and no reserve is kept. A hard limit disables overshoot in the engine, and then this stays as before.
        pressure.overshoot = m_engine.IsOvershootEnabled();
    }
    return m_policy.Trigger(pressure);
}

size_t LxrGcHeap::GetCollectionReservedPages(size_t headroomBytes) const
{
    if (m_heap_configuration.evacuateNursery)
    {
        // The young pause copies the survivors of clean nursery blocks into admitted free blocks, so they
        // must exist when it starts. Young objects in recycled blocks are promoted in place and need none.
        return m_policy.CollectionReservedPagesForCopy(
            m_engine.GetCleanNurseryBlocks() * ImmixBlockGeometry::BlockBytes, LxrLargeObjectSpace::PageBytes);
    }
    return m_policy.CollectionReservedPages(
        m_engine.GetCleanNurseryBlocks() * ImmixBlockGeometry::BlockBytes,
        headroomBytes / LxrLargeObjectSpace::PageBytes);
}

LxrRcResult LxrGcHeap::UpdatePolicyHints()
{
    LxrPhaseScope scope(m_engine.GetPauseTimers(), LxrPauseTimers::PolicyDrain);
    return m_engine.DrainLazySweep(Timeout);
}

LxrRcResult LxrGcHeap::FinishLazy(LxrReferenceCounting* engine, void* context)
{
    LxrGcHeap* heap = static_cast<LxrGcHeap*>(context);
    // The engine cleared its cycle-sweep flag before this callback, so the finalizers may run now.
    if (heap->m_finalization_deferred.exchange(false, std::memory_order_seq_cst))
    {
        GCToEEInterface::EnableFinalization(heap->m_registry.GetPendingCount() != 0);
    }
    // The lazy sweep of a FinalMark pause is no longer awaited by the pause (F8), so the end of the concurrent collection
    // is announced here, when the sweep has cleared the trace phase.
    if (engine->GetTracePhase() == LxrTracePhase::Inactive)
    {
        heap->m_concurrent_done.Set();
    }
    size_t mature;
    LxrRcResult result = engine->GetOccupiedPages(&mature);
    if (result != LxrRcResult::Success)
    {
        return result;
    }
    if (!heap->m_policy.FinishLazySweep(
            heap->m_current_pause_kind, engine->GetAdmittedBytes() / LxrLargeObjectSpace::PageBytes, mature,
            engine->GetTracePhase() == LxrTracePhase::Concurrent, heap->m_concurrent_enabled.load(), true))
    {
        return LxrRcResult::InvariantViolation;
    }
    // The lazy sweep has counted the free lines of the blocks that the pause promoted, so the figures are complete now.
    size_t heapBytes;
    size_t fragmentation;
    result = engine->GetHeapStatistics(&heapBytes, &fragmentation);
    if (result == LxrRcResult::Success)
    {
        heap->m_last_heap_size.store(heapBytes);
        heap->m_last_fragmentation.store(fragmentation);
    }
    return result;
}

LxrRcResult LxrGcHeap::CollectForPressure(bool emergency, bool user, bool heapFull)
{
    bool cooperative = EnterCollectionLock();
    LxrRcResult result = UpdatePolicyHints();
    LxrCollectionCause cause = GetCollectionCause(heapFull || emergency);
    if (result == LxrRcResult::Success && (emergency || user || cause != LxrCollectionCause::None))
    {
        m_collection_cause.store(cause, std::memory_order_release);
        result = CollectLocked(m_policy.Select(
            m_engine.GetTracePhase() == LxrTracePhase::Concurrent,
            m_engine.IsTraceExhausted(), emergency, user, m_concurrent_enabled.load()),
            emergency || user || m_policy.EmergencyHint());
    }
    LeaveCollectionLock(cooperative);
    return result;
}

void LxrGcHeap::Suspend(void* context)
{
    LxrGcHeap* heap = static_cast<LxrGcHeap*>(context);
    LxrPhaseScope suspend(heap->m_engine.GetPauseTimers(), LxrPauseTimers::SuspendRuntime);
    heap->m_in_gc.store(true, std::memory_order_release);
    heap->m_gc_done.Reset();
    heap->m_pause_start = minipal_hires_ticks();
    {
        LxrPhaseScope suspendEE(heap->m_engine.GetPauseTimers(), LxrPauseTimers::SuspendEE);
        GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);
    }
    GCToEEInterface::GcEnumAllocContexts(RetireAllocationContext, heap);
    heap->m_pause_young_bytes.store(heap->m_engine.GetYoungAllocationBytes(), std::memory_order_release);
    {
        size_t headroom = 0;
        if (heap->m_engine.GetDefragHeadroomBytes(&headroom) == LxrRcResult::Success)
        {
            heap->m_pause_free_pages.store(heap->m_engine.GetImmixFreePages(), std::memory_order_release);
            heap->m_pause_reserve_pages.store(
                heap->GetCollectionReservedPages(headroom), std::memory_order_release);
            if (heap->m_heap_configuration.evacuateNursery && heap->m_engine.IsOvershootEnabled())
            {
                // The engine admits the missing blocks right after it has flushed the mutators.
                heap->m_engine.RequestBorrow(heap->m_pause_reserve_pages.load(std::memory_order_acquire));
            }
        }
    }
    if (!heap->m_policy.BeginPause(
            heap->m_pause_young_bytes.load(std::memory_order_acquire),
            heap->m_engine.GetCleanNurseryBlocks() * ImmixBlockGeometry::BlockBytes))
    {
        heap->Fatal("LXR survival predictor allocation snapshot was not retired.");
    }
    heap->m_collections.fetch_add(1, std::memory_order_relaxed);
    GCToEEInterface::GcStartWork(2, 2);
}

void LxrGcHeap::Resume(bool finished, void* context)
{
    LxrGcHeap* heap = static_cast<LxrGcHeap*>(context);
    LxrPauseTimers& timers = heap->m_engine.GetPauseTimers();
    {
        LxrPhaseScope resume(timers, LxrPauseTimers::ResumeRuntime);
        // A tracing pause leaves a lazy sweep of dead cycles. It reads the type data of dead objects, which the runtime
        // frees only from a finalizer, so the finalizer thread is signalled when the sweep ends (FinishLazy). The flag
        // is set before the check, so exactly one of Resume and FinishLazy sends the signal.
        heap->m_finalization_deferred.store(true, std::memory_order_seq_cst);
        if (!heap->m_engine.IsCycleSweepPending() &&
            heap->m_finalization_deferred.exchange(false, std::memory_order_seq_cst))
        {
            GCToEEInterface::EnableFinalization(heap->m_registry.GetPendingCount() != 0);
        }
        GCToEEInterface::GcDone(2);
        GCToEEInterface::RestartEE(finished);
    }
    heap->m_in_gc.store(false, std::memory_order_release);
    int64_t elapsed = minipal_hires_ticks() - heap->m_pause_start;
    timers.Add(LxrPauseTimers::StwPause, elapsed);
    int64_t frequency = minipal_hires_tick_frequency();
    int64_t ticks = elapsed / frequency * 10000000 + elapsed % frequency * 10000000 / frequency;
    heap->m_last_pause_ticks.store(ticks);
    heap->m_total_pause_ticks.fetch_add(ticks);
    if (heap->m_current_pause_kind == LxrPauseKind::InitialMark)
    {
        heap->m_initial_pause_ticks = static_cast<uint64_t>(ticks);
    }
    else
    {
        heap->PublishMemoryInfo();
    }
    heap->m_gc_done.Set();
}

void LxrGcHeap::CoordinateTrace(void* context)
{
    LxrGcHeap* heap = static_cast<LxrGcHeap*>(context);
    while (!heap->m_shutdown.load(std::memory_order_acquire))
    {
        heap->m_trace_signal.Wait(10, false);
        if (!heap->m_shutdown.load() && heap->m_initialized.load() && heap->m_engine.IsTraceExhausted())
        {
            LxrRcResult result = heap->CollectPrototype(LxrPauseKind::FinalMark);
            if (result != LxrRcResult::Success &&
                !(result == LxrRcResult::InvalidPhase && heap->m_shutdown.load()))
            {
                heap->Fatal("LXR automatic FinalMark transition failed.");
            }
        }
    }
    heap->m_coordinator_done.Set();
}

void LxrGcHeap::Shutdown()
{
    if (!m_initialized.load() || m_shutdown.exchange(true))
    {
        return;
    }
#ifdef TARGET_WINDOWS
    if (m_process_exiting())
    {
        // ExitProcess has terminated peer threads before DLL_PROCESS_DETACH. Their events need not have been signaled.
        return;
    }
#endif // TARGET_WINDOWS
    m_trace_signal.Set();
    bool cooperative = GCToEEInterface::EnablePreemptiveGC();
    if (m_coordinator_done.Wait(Timeout, false) != WAIT_OBJECT_0)
    {
        Fatal("LXR shutdown could not stop its trace coordinator.");
    }
    // An admitted collection may still need the scan executor until it releases this lock.
    m_collection_lock.Enter();
    if (m_engine.StopWorkers() != LxrRcResult::Success)
    {
        Fatal("LXR shutdown could not drain its native readers.");
    }
    m_scan_stopping.store(true, std::memory_order_release);
    m_scan_signal.Set();
    if (m_scan_exited.Wait(Timeout, false) != WAIT_OBJECT_0)
    {
        Fatal("LXR shutdown could not stop its scan executor.");
    }
    m_collection_lock.Leave();
    if (cooperative)
    {
        GCToEEInterface::DisablePreemptiveGC();
    }
    // The EE may still have live background-thread references. Reservations outlive Shutdown, as in the GC event contract.
}

LxrRcResult LxrGcHeap::RunScan(uint8_t kind)
{
    ScanCostScope dispatch(this, 4 + kind);
    CountScanWork(kind);
    bool expected = false;
    if (kind > 3 || m_scan_stopping.load(std::memory_order_acquire) ||
        !m_scan_busy.compare_exchange_strong(expected, true, std::memory_order_acq_rel))
    {
        Fatal("LXR received an invalid, overlapping or reentrant scan operation.");
    }
    m_scan_failure.store(LxrRcResult::Success);
    m_scan_kind.store(kind, std::memory_order_release);
    m_scan_signal.Set();
    {
        ScanCostScope wait(this, 9);
        if (m_scan_done.Wait(Timeout, false) != WAIT_OBJECT_0)
        {
            // Returning would let the caller resume/reclaim while the scan might still access its epoch.
            Fatal("LXR scan executor did not complete its operation.");
        }
    }
    LxrRcResult result = m_scan_failure.load(std::memory_order_acquire);
    m_scan_busy.store(false, std::memory_order_release);
    return result;
}

LxrRcResult LxrGcHeap::CaptureRoots(LxrReferenceCounting*, void* context)
{
    return static_cast<LxrGcHeap*>(context)->RunScan(0);
}

LxrRcResult LxrGcHeap::FinishTrace(LxrReferenceCounting*, void* context)
{
    return static_cast<LxrGcHeap*>(context)->RunScan(1);
}

LxrRcResult LxrGcHeap::FinishEpoch(LxrReferenceCounting*, void* context)
{
    return static_cast<LxrGcHeap*>(context)->RunScan(2);
}

LxrRcResult LxrGcHeap::FinishRelocation(LxrReferenceCounting*, void* context)
{
    return static_cast<LxrGcHeap*>(context)->RunScan(3);
}

void LxrGcHeap::ScanOnGcThread(void* context)
{
    LxrGcHeap* heap = static_cast<LxrGcHeap*>(context);
    // The executor remains alive for the EE's post-start utility-thread naming and readiness handoff.
    heap->m_scan_ready.Set();
    for (;;)
    {
        if (heap->m_scan_signal.Wait(INFINITE, false) != WAIT_OBJECT_0)
        {
            heap->Fatal("LXR scan executor could not wait for work.");
        }
        if (heap->m_scan_stopping.load(std::memory_order_acquire))
        {
            break;
        }
        heap->ExecuteScan(heap->m_scan_kind.load(std::memory_order_acquire));
        heap->m_scan_done.Set();
    }
    heap->m_scan_exited.Set();
}

void LxrGcHeap::ExecuteScan(uint8_t kind)
{
    LxrGcHeap* heap = this;
    ScanCostScope execution(heap, 10 + kind);
    LxrPhaseScope scanTotal(heap->m_engine.GetPauseTimers(), LxrPauseTimers::Scan0 + kind);
    auto measure = [heap](size_t metric, size_t phase, const auto& callback)
    {
        ScanCostScope cost(heap, metric);
        LxrPhaseScope timer(heap->m_engine.GetPauseTimers(), phase);
        callback();
    };
    ScanContext scan{};
    scan.promotion = true;
    scan.thread_number = 0;
    scan.thread_count = 1;
    if (kind == 0)
    {
        bool timed = heap->m_storage_diagnostics_enabled.load(std::memory_order_relaxed);
        int64_t start = timed ? minipal_hires_ticks() : 0;
        measure(31, LxrPauseTimers::ScanBeforeRoots, [&]() { GCToEEInterface::BeforeGcScanRoots(2, false, false); });
        LxrRcResult result = LxrRcResult::Success;
        measure(14, LxrPauseTimers::ScanLeaseFinalizers, [&]() { heap->CaptureFinalizers(); });
        measure(15, LxrPauseTimers::ScanRoots, [&]() { GCScan::GcScanRoots(CopyRoot, 2, 2, &scan); });
        measure(16, LxrPauseTimers::ScanHandles, [&]() { GCScan::GcScanHandles(CopyRoot, 2, 2, &scan); });
        measure(17, LxrPauseTimers::ScanProfilerHandles, [&]() { GCScan::GcScanHandlesForProfilerAndETW(2, &scan, LeaseWeakHandle); });
        measure(18, LxrPauseTimers::ScanDependent, [&]()
        {
            // Profiler enumeration omits null-secondary primaries; the owning-slot visitor does not.
            scan.promotion = false;
            Ref_ScanDependentHandlesForRelocation(2, 2, &scan, LeaseDependentRoot);
            scan.promotion = true;
        });
        measure(19, LxrPauseTimers::ScanSyncBlock, [&]()
        {
            GCToEEInterface::SyncBlockCacheWeakPtrScan(LeaseSyncBlock,
                reinterpret_cast<uintptr_t>(&scan), reinterpret_cast<uintptr_t>(heap));
        });
        measure(20, LxrPauseTimers::ScanFrozen, [&]() { heap->ScanFrozenRoots(); });
        if (timed)
        {
            heap->m_root_capture_ticks.fetch_add(
                static_cast<uint64_t>(minipal_hires_ticks() - start),
                std::memory_order_relaxed);
        }
    }
    else if (kind == 1)
    {
        measure(21, LxrPauseTimers::ScanDhInitial, [&]()
        {
            GCScan::GcDhInitialScan(TraceRoot, 2, 2, &scan);
            heap->CountScanWork(32);
            while (GCScan::GcDhReScan(&scan)) { heap->CountScanWork(32); }
        });
        measure(22, LxrPauseTimers::ScanShortWeak, [&]() { GCScan::GcShortWeakPtrScan(2, 2, &scan); });
        measure(23, LxrPauseTimers::ScanQueueFinalizers, [&]() { heap->DecideFinalization(); });
        measure(25, LxrPauseTimers::ScanDhRescan, [&]()
        {
            GCScan::GcDhInitialScan(TraceRoot, 2, 2, &scan);
            heap->CountScanWork(32);
            while (GCScan::GcDhReScan(&scan)) { heap->CountScanWork(32); }
        });
        measure(26, LxrPauseTimers::ScanWeakPtr, [&]() { GCScan::GcWeakPtrScan(2, 2, &scan); });
        measure(27, LxrPauseTimers::ScanWeakPtrSingle, [&]() { GCScan::GcWeakPtrScanBySingleThread(2, 2, &scan); });
    }
    else if (kind == 3)
    {
        if (heap->m_heap_configuration.evacuateNursery || heap->m_heap_configuration.evacuateMature)
        {
            scan.promotion = false;
            measure(28, LxrPauseTimers::ScanRelocateRoots, [&]() { GCScan::GcScanRoots(RelocateRoot, 2, 2, &scan); });
            measure(29, LxrPauseTimers::ScanRelocateHandles, [&]() { GCScan::GcScanHandles(RelocateRoot, 2, 2, &scan); });
            heap->RelocateFinalizers();
            scan.promotion = true;
            GCToEEInterface::DiagWalkSurvivors(nullptr,
                heap->m_engine.GetNurseryMetrics().pauseCopiedBytes != 0 ||
                heap->m_engine.GetMatureMetrics().pauseCopiedBytes != 0);
        }
    }
    else
    {
        if (!heap->m_policy.RecordCopiedPromotion(heap->m_engine.GetNurseryMetrics().pauseCopiedBytes) ||
            !heap->m_policy.RecordSurvivors(heap->m_engine.GetPauseSurvivorBytes()) ||
            !heap->m_policy.ReleasePause())
        {
            heap->m_scan_failure.store(LxrRcResult::InvariantViolation);
        }
        measure(31, LxrPauseTimers::ScanPromotionsGranted, [&]()
        {
            GCScan::GcPromotionsGranted(2, 2, &scan);
            GCToEEInterface::AfterGcScanRoots(2, 2, &scan);
        });
        size_t heapBytes = 0;
        size_t fragmentation = 0;
        LxrRcResult result = LxrRcResult::Success;
        measure(30, LxrPauseTimers::ScanHeapStatistics, [&]() { result = heap->m_engine.GetHeapStatistics(&heapBytes, &fragmentation); });
        if (result == LxrRcResult::Success)
        {
            heap->m_last_heap_size.store(heapBytes);
            heap->m_last_fragmentation.store(fragmentation);
            heap->m_last_committed.store(heap->m_engine.GetCommittedBytes());
            heap->m_last_memory_load.store(heap->GetMemoryLoad());
        }
        else
        {
            heap->m_scan_failure.store(result);
        }
    }
}

void LxrGcHeap::AddRoot(Object* object, uint32_t flags, bool trace)
{
    if (object == nullptr)
    {
        return;
    }
    if (!m_engine.OwnsAddress(reinterpret_cast<uintptr_t>(object)))
    {
        if ((flags & GC_CALL_INTERIOR) != 0 || FrozenContains(reinterpret_cast<uintptr_t>(object), true))
        {
            return;
        }
        m_scan_failure.store(LxrRcResult::UnsupportedLifetime);
        return;
    }
    if ((flags & GC_CALL_INTERIOR) != 0)
    {
        Object* containing = nullptr;
        LxrRcResult result = m_engine.FindContainingObject(reinterpret_cast<uintptr_t>(object), &containing);
        if (result != LxrRcResult::Success)
        {
            m_scan_failure.store(result);
            return;
        }
        object = containing;
    }
    if ((flags & GC_CALL_PINNED) != 0)
    {
        // The pin list keeps the object where it is for this pause. The list is not a table by address: its size
        // follows the number of pinned roots.
        LxrRcResult pinned = m_engine.AddPinnedRoot(object, false, 0);
        if (pinned != LxrRcResult::Success)
        {
            m_scan_failure.store(pinned);
            return;
        }
    }
    LxrRcResult result = m_engine.AddRoot(object, false, trace);
    if (result != LxrRcResult::Success)
    {
        m_scan_failure.store(result);
    }
}

void LxrGcHeap::CopyRoot(Object** slot, ScanContext*, uint32_t flags)
{
    s_heap->CountScanWork(36);
    s_heap->AddRoot(*slot, flags, true);
}

void LxrGcHeap::RelocateRoot(Object** slot, ScanContext*, uint32_t flags)
{
    s_heap->CountScanWork(37);
    LxrRcResult result = s_heap->m_engine.RelocateRoot(
        slot, (flags & GC_CALL_INTERIOR) != 0, (flags & GC_CALL_PINNED) != 0);
    if (result != LxrRcResult::Success)
    {
        s_heap->m_scan_failure.store(result);
    }
}

void LxrGcHeap::TraceRoot(Object** slot, ScanContext*, uint32_t flags)
{
    s_heap->CountScanWork(39);
    Object* object = *slot;
    if (object == nullptr || s_heap->FrozenContains(reinterpret_cast<uintptr_t>(object), true))
    {
        return;
    }
    if ((flags & GC_CALL_INTERIOR) != 0)
    {
        object = s_heap->GetContainingObject(object, false);
    }
    LxrRcResult result = s_heap->m_engine.TraceRoot(object);
    if (result != LxrRcResult::Success)
    {
        s_heap->m_scan_failure.store(result);
    }
}

void LxrGcHeap::LeaseWeakHandle(
    Object** slot, Object*, uint32_t flags, ScanContext*, bool)
{
    s_heap->CountScanWork(38);
    if ((flags & kEtwGCRootFlagsRefCounted) != 0 && (flags & kEtwGCRootFlagsWeakRef) == 0)
    {
        s_heap->AddRoot(*slot, 0, true);
        return;
    }
    if ((flags & kEtwGCRootFlagsWeakRef) != 0)
    {
        s_heap->AddRoot(*slot, 0, false);
    }
}

void LxrGcHeap::LeaseDependentRoot(Object** slot, ScanContext*, uint32_t)
{
    s_heap->CountScanWork(38);
    s_heap->AddRoot(*slot, 0, false);
}

void CALLBACK LxrGcHeap::LeaseSyncBlock(
    PTR_UNCHECKED_OBJECTREF slot, uintptr_t*, uintptr_t, uintptr_t context)
{
    LxrGcHeap* heap = reinterpret_cast<LxrGcHeap*>(context);
    Object* object = *reinterpret_cast<Object**>(slot);
    heap->AddRoot(object, 0, false);
}

bool LxrGcHeap::IsFinalizationSuppressed(Object* object)
{
    return (object->GetHeader()->GetBits() & BIT_SBLK_FINALIZER_RUN) != 0;
}

void LxrGcHeap::CaptureFinalizers()
{
    CountScanWork(33);
    LxrRcResult result = LxrFinalizerCapture(
        m_engine, m_registry, m_current_pause_kind, IsFinalizationSuppressed, &m_registry_counters);
    if (result != LxrRcResult::Success)
    {
        m_scan_failure.store(result);
    }
}

void LxrGcHeap::DecideFinalization()
{
    ScanCostScope cost(this, 24);
    LxrPhaseScope timer(m_engine.GetPauseTimers(), LxrPauseTimers::ScanTraceFinalizers);
    LxrRcResult result = LxrFinalizerDecide(m_engine, m_registry, IsFinalizationSuppressed,
        [](Object* object) { return GCToEEInterface::EagerFinalized(object); }, &m_registry_counters);
    if (result != LxrRcResult::Success)
    {
        m_scan_failure.store(result);
    }
}

void LxrGcHeap::RelocateFinalizers()
{
    LxrFinalizerRelocate(m_engine, m_registry, m_heap_configuration.evacuateMature, m_current_pause_kind);
}

bool LxrGcHeap::FrozenContains(uintptr_t address, bool retired) const
{
    for (const FrozenSegment& segment : m_frozen)
    {
        uint8_t state = segment.state.load(std::memory_order_seq_cst);
        if ((state == 2 || (retired && state == 4)) && address >= segment.start &&
            address < segment.allocated.load(std::memory_order_acquire))
        {
            return true;
        }
    }
    return false;
}

bool LxrGcHeap::IsImmortal(Object* object, void* context)
{
    return static_cast<LxrGcHeap*>(context)->FrozenContains(reinterpret_cast<uintptr_t>(object), true);
}

void LxrGcHeap::ScanFrozenRoots()
{
    m_frozen_readers.fetch_add(1, std::memory_order_seq_cst);
    for (FrozenSegment& segment : m_frozen)
    {
        if (segment.state.load(std::memory_order_seq_cst) != 2)
        {
            continue;
        }
        uintptr_t end = segment.allocated.load(std::memory_order_acquire);
        for (uintptr_t cursor = segment.start; cursor < end;)
        {
            Object* object = reinterpret_cast<Object*>(cursor);
            size_t size = object->GetSize();
            if (size > SIZE_MAX - (sizeof(uintptr_t) - 1))
            {
                Fatal("LXR frozen-object extent is invalid.");
            }
            size = (size + sizeof(uintptr_t) - 1) & ~(sizeof(uintptr_t) - 1);
            if (size < MIN_OBJECT_SIZE || size > end - cursor)
            {
                Fatal("LXR frozen-object extent exceeds its published segment.");
            }
            GCReferenceRanges::Enumerate(object, [&](Object** first, size_t count)
            {
                for (size_t index = 0; index < count; index++)
                {
                    // RegisterFrozenSegment grants read-only storage, not a writable relocation slot.
                    AddRoot(first[index], GC_CALL_PINNED, true);
                }
                return true;
            });
            MethodTable* type = object->GetGCSafeMethodTable();
            if (type->Collectible())
            {
                Object** loader = m_enumeration.get_loader_allocator_object_slot(type);
                if (loader != nullptr)
                {
                    AddRoot(*loader, 0, true);
                }
            }
            cursor += size;
        }
    }
    m_frozen_readers.fetch_sub(1, std::memory_order_seq_cst);
}

int LxrGcHeap::GetNumberOfHeaps() { return 1; }
int LxrGcHeap::GetHomeHeapNumber() { return 0; }
size_t LxrGcHeap::GetPromotedBytes(int) { return m_engine.GetMarkedBytes(); }
bool LxrGcHeap::IsPromoted2(Object* object, bool) { return IsPromoted(object); }
bool LxrGcHeap::IsEphemeral(Object*) { return false; }
unsigned LxrGcHeap::GetCondemnedGeneration() { return 2; }
bool LxrGcHeap::IsThreadUsingAllocationContextHeap(gc_alloc_context* context, int thread)
{
    return context != nullptr && thread == 0;
}

bool LxrGcHeap::IsPromoted(Object* object)
{
    object = m_engine.ResolveForwarded(object);
    if (object == nullptr)
    {
        return true;
    }
    if (IsInFrozenSegment(object))
    {
        return true;
    }
    LxrTracePhase phase = m_engine.GetTracePhase();
    if (phase == LxrTracePhase::Full || phase == LxrTracePhase::FinalMark)
    {
        return m_engine.IsMarked(object);
    }
    uintptr_t count;
    return m_engine.IsObjectStart(object) &&
        m_engine.LoadCount(object, &count) == LxrRcResult::Success && count != 0;
}

bool LxrGcHeap::IsHeapPointer(void* object, bool smallOnly)
{
    uintptr_t address = reinterpret_cast<uintptr_t>(object);
    return FrozenContains(address, false) ||
        (m_engine.OwnsAddress(address) &&
            (!smallOnly || address - m_engine.GetReservationStart() < m_engine.GetImmixCapacityBytes()));
}

unsigned LxrGcHeap::WhichGeneration(Object* object)
{
    return m_engine.OwnsAddress(reinterpret_cast<uintptr_t>(object)) ? 2 : static_cast<unsigned>(INT32_MAX);
}

Object* LxrGcHeap::GetContainingObject(void* interior, bool)
{
    Object* object = nullptr;
    if (!m_engine.OwnsAddress(reinterpret_cast<uintptr_t>(interior)))
    {
        return nullptr;
    }
    LxrRcResult result = m_engine.FindContainingObject(reinterpret_cast<uintptr_t>(interior), &object);
    return result == LxrRcResult::Success ? object : nullptr;
}

bool LxrGcHeap::IsLargeObject(Object* object)
{
    return m_engine.IsLargeObject(object);
}

bool LxrGcHeap::IsGCInProgressHelper(bool considerStart)
{
    return m_in_gc.load(std::memory_order_acquire) || (considerStart && g_fSuspensionPending != 0);
}

void LxrGcHeap::SetGCInProgress(bool value) { m_in_gc.store(value, std::memory_order_release); }
void LxrGcHeap::SetSuspensionPending(bool value) { g_fSuspensionPending = value ? 1 : 0; }
bool LxrGcHeap::RuntimeStructuresValid() { return m_initialized.load(std::memory_order_acquire); }
void LxrGcHeap::SetYieldProcessorScalingFactor(float) {}
void LxrGcHeap::SetWaitForGCEvent() { m_gc_done.Set(); }
void LxrGcHeap::ResetWaitForGCEvent() { m_gc_done.Reset(); }

uint32_t LxrGcHeap::WaitUntilGCComplete(bool)
{
    bool cooperative = GCToEEInterface::EnablePreemptiveGC();
    uint32_t result = m_gc_done.Wait(INFINITE, false);
    if (cooperative)
    {
        GCToEEInterface::DisablePreemptiveGC();
    }
    return result;
}

bool LxrGcHeap::IsConcurrentGCInProgress()
{
    return !m_shutdown.load() && m_engine.GetTracePhase() != LxrTracePhase::Inactive;
}

bool LxrGcHeap::IsConcurrentGCEnabled() { return m_concurrent_enabled.load(); }
void LxrGcHeap::TemporaryEnableConcurrentGC() { m_concurrent_enabled.store(true); }
void LxrGcHeap::TemporaryDisableConcurrentGC()
{
    m_concurrent_enabled.store(false);
    WaitUntilConcurrentGCComplete();
}

void LxrGcHeap::WaitUntilConcurrentGCComplete()
{
    if (!m_shutdown.load() && IsConcurrentGCInProgress() &&
        CollectPrototype(LxrPauseKind::FinalMark, true) != LxrRcResult::Success)
    {
        Fatal("LXR concurrent-completion wait failed.");
    }
}

HRESULT LxrGcHeap::WaitUntilConcurrentGCCompleteAsync(int timeout)
{
    if (timeout < -1)
    {
        Unsupported("LXR concurrent-completion timeout must be -1 or nonnegative.");
        return E_FAIL;
    }
    uint32_t result = m_concurrent_done.Wait(timeout == -1 ? INFINITE : static_cast<uint32_t>(timeout), false);
    return result == WAIT_OBJECT_0 ? S_OK :
        result == WAIT_TIMEOUT ? HRESULT_FROM_WIN32(ERROR_TIMEOUT) : E_FAIL;
}

size_t LxrGcHeap::GetNumberOfFinalizable() { return m_registry.GetPendingCount(); }
FinalizerWorkItem* LxrGcHeap::GetExtraWorkForFinalization() { return nullptr; }

Object* LxrGcHeap::GetNextFinalizable()
{
    if (m_registry.GetPendingCount() == 0)
    {
        return nullptr;
    }
    // The runtime frees a loader allocator (and so the type data of its collectible types) only from a finalizer.
    // The lazy cycle sweep of a tracing pause reads the size of each dead cycle object from the object itself, so
    // no finalizer may run until that sweep has finished. Resume does not signal the finalizer thread while the
    // sweep is pending, and FinishLazy signals it once the sweep is done.
    if (m_engine.IsCycleSweepPending())
    {
        return nullptr;
    }
    Object* object = m_registry.TakeNext(IsFinalizationSuppressed);
    if (m_registry.GetFailures() != 0)
    {
        Fatal("LXR finalizer registry could not record a released count.");
    }
    if (object != nullptr)
    {
        if (!m_engine.IsObjectStart(object))
        {
            Fatal("LXR finalizer queue lost its managed allocation lease.");
        }
        CountScanWork(35);
    }
    return object;
}

bool LxrGcHeap::RegisterForFinalization(int, Object* object)
{
    if (!m_engine.IsOwnedObject(object))
    {
        Unsupported("LXR cannot register finalization for a nonowned or incomplete object.");
        return false;
    }
    object->GetHeader()->ClrBit(BIT_SBLK_FINALIZER_RUN);
    return m_registry.Register(object, object->GetGCSafeMethodTable()->HasCriticalFinalizer(), true);
}

void LxrGcHeap::SetFinalizationRun(Object* object)
{
    if (!m_engine.IsOwnedObject(object))
    {
        Fatal("LXR finalization suppression received a nonowned object.");
    }
    // The registry sees the header bit when it next looks at the entry (the next pause for an entry that is not
    // counted yet, the next tracing pause for a counted one, or the finalizer thread for a ready one).
    object->GetHeader()->SetBit(BIT_SBLK_FINALIZER_RUN);
}

size_t LxrGcHeap::GetValidSegmentSize(bool) { return m_engine.GetReservationBytes(); }
void LxrGcHeap::SetReservedVMLimit(size_t limit) { m_reserved_limit = limit; }
size_t LxrGcHeap::GetTotalBytesInUse() { return m_engine.GetUsedBytesEstimate(); }
uint64_t LxrGcHeap::GetTotalAllocatedBytes() { return m_context_allocated_bytes.load(); }
size_t LxrGcHeap::GetCurrentObjSize() { return m_engine.GetUsedBytesEstimate(); }
int LxrGcHeap::CollectionCount(int, int) { return static_cast<int>(m_collections.load()); }
unsigned LxrGcHeap::GetGcCount() { return m_collections.load(); }
int LxrGcHeap::GetGcLatencyMode() { return m_latency.load(); }
int LxrGcHeap::SetGcLatencyMode(int mode)
{
    m_latency.store(mode);
    m_concurrent_enabled.store(mode != 0);
    return 0;
}

int LxrGcHeap::GetLOHCompactionMode() { return m_loh_mode.load(); }
void LxrGcHeap::SetLOHCompactionMode(int mode)
{
    if (mode != 1)
    {
        Fatal("LXR is nonmoving and does not implement LOH compaction.");
    }
    m_loh_mode.store(mode);
}

int LxrGcHeap::GetLastGCPercentTimeInGC()
{
    uint64_t elapsed = GetNow() - m_start_time;
    return elapsed == 0 ? 0 : static_cast<int>(m_total_pause_ticks.load() / 10000 * 100 / elapsed);
}

size_t LxrGcHeap::GetLastGCGenerationSize(int generation)
{
    return generation == 2 ? m_last_heap_size.load() : 0;
}

size_t LxrGcHeap::GetLastGCStartTime(int) { return m_last_gc_start.load(); }
size_t LxrGcHeap::GetLastGCDuration(int) { return m_last_gc_duration.load(); }
size_t LxrGcHeap::GetNow() { return static_cast<size_t>(minipal_lowres_ticks()); }
int64_t LxrGcHeap::GetTotalPauseDuration() { return m_total_pause_ticks.load(); }
uint64_t LxrGcHeap::GetGenerationBudget(int) { return m_policy_configuration.youngLimitBytes; }
size_t LxrGcHeap::GetLOHThreshold() { return LxrLargeObjectSpace::MinimumObjectBytes; }

uint32_t LxrGcHeap::GetMemoryLoad()
{
    uint32_t load;
    uint64_t physical;
    uint64_t pageFile;
    GCToOSInterface::GetMemoryStatus(GCToOSInterface::GetPhysicalMemoryLimit(), &load, &physical, &pageFile);
    return load;
}

void LxrGcHeap::PublishMemoryInfo()
{
    uint64_t physical = GCToOSInterface::GetPhysicalMemoryLimit();
    uint64_t available = m_hard_limit;
    bool concurrent = m_current_pause_kind == LxrPauseKind::FinalMark && !m_forced_blocking;
    uint64_t values[MemoryFields]{
        available, physical / 10 * 9, physical / 100 * m_last_memory_load.load(),
        m_last_heap_size.load(), m_last_fragmentation.load(), m_last_committed.load(),
        m_engine.GetUsedBytesEstimate(), m_engine.GetPinnedRootCount(), m_registry.GetPendingCount(),
        m_collections.load(), 2, static_cast<uint64_t>(GetLastGCPercentTimeInGC()),
        concurrent ? 1u : 0u,
        m_engine.GetNurseryMetrics().pauseCopiedBytes != 0 ||
            m_engine.GetMatureMetrics().pauseCopiedBytes != 0 ? 1u : 0u,
        concurrent ? m_initial_pause_ticks : static_cast<uint64_t>(m_last_pause_ticks.load()),
        concurrent ? static_cast<uint64_t>(m_last_pause_ticks.load()) : 0,
        concurrent ? m_trace_heap_before : m_heap_before,
        concurrent ? m_trace_fragmentation_before : m_fragmentation_before,
    };
    for (size_t kind : {size_t{0}, concurrent ? size_t{3} : size_t{2}})
    {
        m_memory_sequence[kind].fetch_add(1);
        for (size_t field = 0; field < MemoryFields; field++)
        {
            m_memory_info[kind][field].store(values[field]);
        }
        m_memory_sequence[kind].fetch_add(1);
    }
}

void LxrGcHeap::GetMemoryInfo(
    uint64_t* highThreshold, uint64_t* available, uint64_t* load, uint64_t* heap,
    uint64_t* fragmentation, uint64_t* committed, uint64_t* promoted, uint64_t* pinned,
    uint64_t* pending, uint64_t* index, uint32_t* generation, uint32_t* pausePercent,
    bool* compacting, bool* concurrent, uint64_t* generations, uint64_t* pauses, int kind)
{
    if (kind < 0 || kind > 3)
    {
        Fatal("LXR memory-info collection kind is invalid.");
    }
    uint64_t values[MemoryFields];
    uint64_t sequence;
    do
    {
        sequence = m_memory_sequence[kind].load();
        for (size_t field = 0; field < MemoryFields; field++)
        {
            values[field] = m_memory_info[kind][field].load();
        }
    } while ((sequence & 1) != 0 || sequence != m_memory_sequence[kind].load());
    *available = values[Available];
    *highThreshold = values[HighThreshold];
    *load = values[Load];
    *heap = values[HeapSize];
    *fragmentation = values[Fragmentation];
    *committed = values[Committed];
    *promoted = values[Promoted];
    *pinned = values[Pins];
    *pending = values[Finalizers];
    *index = values[CollectionIndex];
    *generation = static_cast<uint32_t>(values[Generation]);
    *pausePercent = static_cast<uint32_t>(values[PausePercent]);
    *compacting = values[Compacted] != 0;
    *concurrent = values[Concurrent] != 0;
    memset(generations, 0, 20 * sizeof(uint64_t));
    generations[8] = values[HeapBefore];
    generations[9] = values[FragmentationBefore];
    generations[10] = *heap;
    generations[11] = *fragmentation;
    pauses[0] = values[PauseTicks];
    pauses[1] = values[SecondPauseTicks];
}

void LxrGcHeap::EnumerateConfigurationValues(void* context, ConfigurationValueFunc callback)
{
    callback(context, "LXRHeapBytes", "System.GC.LXRHeapBytes", GCConfigurationType::Int64,
        static_cast<int64_t>(m_engine.GetReservationBytes()));
    callback(context, "LXRManagedHeapHardLimitMB", "System.GC.LXRManagedHeapHardLimitMB", GCConfigurationType::Int64,
        static_cast<int64_t>(m_hard_limit >> 20));
    callback(context, "LXRNativePlaneLimitMB", "System.GC.LXRNativePlaneLimitMB", GCConfigurationType::Int64,
        static_cast<int64_t>(m_native_plane_limit >> 20));
    callback(context, "LXRTraceThreshold", "System.GC.LXRTraceThreshold", GCConfigurationType::Int64,
        static_cast<int64_t>(m_policy_configuration.traceThreshold));
    callback(context, "LXRRCStopPercent", "System.GC.LXRRCStopPercent", GCConfigurationType::Int64,
        static_cast<int64_t>(m_policy_configuration.rcStopPercent));
    callback(context, "LXRMaxSurvivalMB", "System.GC.LXRMaxSurvivalMB", GCConfigurationType::Int64,
        static_cast<int64_t>(m_policy_configuration.maxSurvivalMB));
    callback(context, "LXRImmixInitialMB", "System.GC.LXRImmixInitialMB", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.immixBytes >> 20));
    callback(context, "LXRLOSInitialMB", "System.GC.LXRLOSInitialMB", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.largeObjectBytes >> 20));
    callback(context, "LXRImmixReserveMB", "System.GC.LXRImmixReserveMB", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.immixReservationBytes >> 20));
    callback(context, "LXRLOSReserveMB", "System.GC.LXRLOSReserveMB", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.largeObjectReservationBytes >> 20));
    callback(context, "LXRMinReuseLines", "System.GC.LXRMinReuseLines", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.minimumReuseLines));
    callback(context, "LXREvacuateNursery", "System.GC.LXREvacuateNursery", GCConfigurationType::Boolean,
        m_heap_configuration.evacuateNursery);
    callback(context, "LXREvacuateMature", "System.GC.LXREvacuateMature", GCConfigurationType::Boolean,
        m_heap_configuration.evacuateMature);
    callback(context, "LXRDefragHeadroomPercent", "System.GC.LXRDefragHeadroomPercent", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.defragHeadroomPercent));
    callback(context, "LXRChunkDefragPercent", "System.GC.LXRChunkDefragPercent", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.chunkDefragPercent));
    callback(context, "LXRBufferSize", "System.GC.LXRBufferSize", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.bufferSize));
    callback(context, "LXRIncrementsLimit", "System.GC.LXRIncrementsLimit", GCConfigurationType::Int64,
        static_cast<int64_t>(m_policy_configuration.incrementsLimit));
    callback(context, "LXRNurseryBlocks", "System.GC.LXRNurseryBlocks", GCConfigurationType::Int64,
        static_cast<int64_t>(m_policy_configuration.nurseryBlocks));
    callback(context, "LXRYoungLimitBytes", "System.GC.LXRYoungLimitBytes", GCConfigurationType::Int64,
        static_cast<int64_t>(m_policy_configuration.youngLimitBytes));
    callback(context, "LXRSurvivalPredictorWeighted", "System.GC.LXRSurvivalPredictorWeighted",
        GCConfigurationType::Boolean, m_policy_configuration.weighted);
    callback(context, "LXRSurvivalPredictorHarmonicMean", "System.GC.LXRSurvivalPredictorHarmonicMean",
        GCConfigurationType::Boolean, m_policy_configuration.harmonic);
    callback(context, "LXRLazyDecrements", "System.GC.LXRLazyDecrements",
        GCConfigurationType::Boolean, m_heap_configuration.lazyDecrements);
    callback(context, "LXRLineRecycling", "System.GC.LXRLineRecycling",
        GCConfigurationType::Boolean, m_heap_configuration.lineRecycling);
    callback(context, "LXRFastAllocation", "System.GC.LXRFastAllocation", GCConfigurationType::Boolean, m_fast_enabled);
    callback(context, "LXROvershootToSpace", "System.GC.LXROvershootToSpace", GCConfigurationType::Boolean,
        m_engine.IsOvershootEnabled());
    callback(context, "LXRMaxMutators", "System.GC.LXRMaxMutators", GCConfigurationType::Int64, MaximumMutators);
    callback(context, "LXRGCThreads", "System.GC.LXRGCThreads", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.workers));
    callback(context, "LXRConcGCThreads", "System.GC.LXRConcGCThreads", GCConfigurationType::Int64,
        static_cast<int64_t>(m_heap_configuration.concurrentTraceWorkers));
    callback(context, "GCConcurrent", "System.GC.Concurrent", GCConfigurationType::Boolean, IsConcurrentGCEnabled());
}

int LxrGcHeap::RefreshMemoryLimit()
{
    Unsupported("LXR capacity can grow within startup limits; changing those limits at runtime is unsupported.");
    return refresh_hard_limit_invalid;
}

bool LxrGcHeap::RegisterForFullGCNotification(uint32_t, uint32_t)
{
    Unsupported("LXR prototype does not support full-GC notification subscriptions.");
    return false;
}

bool LxrGcHeap::CancelFullGCNotification()
{
    Unsupported("LXR prototype has no full-GC notification subscription to cancel.");
    return false;
}

int LxrGcHeap::WaitForFullGCApproach(int)
{
    Unsupported("LXR prototype does not support full-GC notification waits.");
    return 4;
}

int LxrGcHeap::WaitForFullGCComplete(int)
{
    Unsupported("LXR prototype does not support full-GC notification waits.");
    return 4;
}

int LxrGcHeap::StartNoGCRegion(uint64_t, bool, uint64_t, bool)
{
    Unsupported("LXR prototype cannot guarantee a no-GC reservation.");
    return start_no_gc_no_memory;
}

int LxrGcHeap::EndNoGCRegion() { return end_no_gc_not_in_progress; }
enable_no_gc_region_callback_status LxrGcHeap::EnableNoGCRegionCallback(
    NoGCRegionCallbackFinalizerWorkItem*, uint64_t)
{
    return not_started;
}

bool LxrGcHeap::StressHeap(gc_alloc_context*)
{
    Fatal("LXR prototype does not support GCStress.");
    return false;
}

segment_handle LxrGcHeap::RegisterFrozenSegment(segment_info* info)
{
    if (info == nullptr || info->ibFirstObject > info->ibAllocated ||
        info->ibAllocated > info->ibCommit || info->ibCommit > info->ibReserved ||
        info->ibReserved > UINTPTR_MAX - reinterpret_cast<uintptr_t>(info->pvMem))
    {
        Unsupported("LXR frozen-segment registration has invalid bounds.");
        return nullptr;
    }
    for (FrozenSegment& segment : m_frozen)
    {
        uint8_t expected = 0;
        if (!segment.state.compare_exchange_strong(expected, 1))
        {
            continue;
        }
        uintptr_t base = reinterpret_cast<uintptr_t>(info->pvMem);
        segment.start = base + info->ibFirstObject;
        segment.reserved = base + info->ibReserved;
        segment.allocated.store(base + info->ibAllocated);
        segment.committed.store(base + info->ibCommit);
        segment.state.store(2, std::memory_order_seq_cst);
        return reinterpret_cast<segment_handle>(&segment);
    }
    Unsupported("LXR prototype exhausted its 256 frozen-segment lifetime records.");
    return nullptr;
}

void LxrGcHeap::UnregisterFrozenSegment(segment_handle handle)
{
    FrozenSegment* segment = reinterpret_cast<FrozenSegment*>(handle);
    if (segment < m_frozen || segment >= m_frozen + MaximumFrozenSegments)
    {
        Fatal("LXR frozen-segment handle is invalid.");
    }
    uint8_t expected = 2;
    if (!segment->state.compare_exchange_strong(expected, 3, std::memory_order_seq_cst))
    {
        Fatal("LXR frozen segment was already retired.");
    }
    while (m_frozen_readers.load(std::memory_order_seq_cst) != 0)
    {
        GCToOSInterface::YieldThread(0);
    }
    // Historical address classification outlives queued SATB references, not the EE-owned payload.
    segment->state.store(4, std::memory_order_seq_cst);
}

bool LxrGcHeap::IsInFrozenSegment(Object* object)
{
    return FrozenContains(reinterpret_cast<uintptr_t>(object), false);
}

void LxrGcHeap::UpdateFrozenSegment(segment_handle handle, uint8_t* allocated, uint8_t* committed)
{
    FrozenSegment* segment = reinterpret_cast<FrozenSegment*>(handle);
    if (segment < m_frozen || segment >= m_frozen + MaximumFrozenSegments ||
        segment->state.load() != 2 || reinterpret_cast<uintptr_t>(allocated) < segment->start ||
        allocated > committed || reinterpret_cast<uintptr_t>(committed) > segment->reserved)
    {
        Fatal("LXR frozen-segment update has invalid bounds.");
    }
    segment->committed.store(reinterpret_cast<uintptr_t>(committed), std::memory_order_release);
    segment->allocated.store(reinterpret_cast<uintptr_t>(allocated), std::memory_order_release);
}

void LxrGcHeap::PublishObject(uint8_t* object)
{
    if (!m_engine.OwnsAddress(reinterpret_cast<uintptr_t>(object)))
    {
        Fatal("LXR object publication escaped its owned reservation.");
    }
}

void LxrGcHeap::ValidateObjectMember(Object* object)
{
    object = m_engine.ResolveForwarded(object);
    if (!m_engine.IsOwnedObject(object) && !IsInFrozenSegment(object))
    {
        Fatal("LXR object verification received a reclaimed or nonowned object.");
    }
    GCReferenceRanges::Enumerate(object, [&](Object** first, size_t count)
    {
        for (size_t index = 0; index < count; index++)
        {
            Object* child = first[index];
            if (child != nullptr && !m_engine.IsOwnedObject(child) && !IsInFrozenSegment(child))
            {
                Fatal("LXR object verification found an invalid managed child.");
            }
        }
        return true;
    });
}

void LxrGcHeap::ValidateWalk(const char* operation) const
{
    if (m_engine.GetPhase() != LxrRcPhase::Suspended)
    {
        Unsupported(operation);
        Fatal("LXR heap walking requires a suspended managed runtime.");
    }
}

bool LxrGcHeap::WalkHeapObject(Object* object, size_t, void* context)
{
    WalkContext* walk = static_cast<WalkContext*>(context);
    return !walk->heap->IsPromoted(object) || walk->callback(object, walk->context);
}

Object* LxrGcHeap::NextObj(Object* object)
{
    return m_engine.FindNextObject(object);
}

void LxrGcHeap::DiagWalkObject(Object* object, walk_fn callback, void* context)
{
    ValidateWalk("LXR DiagWalkObject requires suspension.");
    object = m_engine.ResolveForwarded(object);
    GCReferenceRanges::Enumerate(object, [&](Object** first, size_t count)
    {
        for (size_t index = 0; index < count; index++)
        {
            if (first[index] != nullptr && !callback(first[index], context))
            {
                return false;
            }
        }
        return true;
    });
}

void LxrGcHeap::DiagWalkObject2(Object* object, walk_fn2 callback, void* context)
{
    ValidateWalk("LXR DiagWalkObject2 requires suspension.");
    object = m_engine.ResolveForwarded(object);
    GCReferenceRanges::Enumerate(object, [&](Object** first, size_t count)
    {
        for (size_t index = 0; index < count; index++)
        {
            if (first[index] != nullptr &&
                !callback(first[index], reinterpret_cast<uint8_t**>(first + index), context))
            {
                return false;
            }
        }
        return true;
    });
}

void LxrGcHeap::DiagWalkHeap(walk_fn callback, void* context, int, bool)
{
    ValidateWalk("LXR DiagWalkHeap requires suspension.");
    WalkContext walk{this, callback, context};
    if (m_engine.WalkObjects(WalkHeapObject, &walk) != LxrRcResult::Success)
    {
        Fatal("LXR heap walk failed.");
    }
}

void LxrGcHeap::DiagWalkHeapWithACHandling(walk_fn callback, void* context, int generation, bool large)
{
    DiagWalkHeap(callback, context, generation, large);
}

void LxrGcHeap::DiagWalkSurvivorsWithType(
    void*, record_surv_fn callback, void* context, walk_surv_type, int)
{
    ValidateWalk("LXR DiagWalkSurvivorsWithType requires suspension.");
    struct SurvivorContext
    {
        LxrGcHeap* heap;
        record_surv_fn callback;
        void* context;
        LxrWordArray copies;
        bool failed;
    } walk{this, callback, context, {}, false};
    LxrRcResult forwarded = m_engine.WalkForwarded([](
        Object* source, Object* destination, size_t size, void* state)
    {
        SurvivorContext* walk = static_cast<SurvivorContext*>(state);
        uint8_t* start = reinterpret_cast<uint8_t*>(source);
        uintptr_t before = reinterpret_cast<uintptr_t>(source);
        uintptr_t after = reinterpret_cast<uintptr_t>(destination);
        ptrdiff_t displacement = after >= before
            ? static_cast<ptrdiff_t>(after - before) : -static_cast<ptrdiff_t>(before - after);
        walk->callback(start, start + size, displacement, walk->context, true, false);
        // The copies are known from the forwarding records. They are reported as moved, so the walk below skips them.
        walk->failed |= !walk->copies.Push(after);
        return true;
    }, &walk);
    if (forwarded != LxrRcResult::Success || walk.failed)
    {
        Fatal("LXR moved survivor walk failed.");
    }
    std::sort(walk.copies.Data(), walk.copies.Data() + walk.copies.Size());
    LxrRcResult result = m_engine.WalkObjects([](Object* object, size_t size, void* state)
    {
        SurvivorContext* walk = static_cast<SurvivorContext*>(state);
        const uintptr_t* copies = walk->copies.Data();
        if (walk->heap->IsPromoted(object) &&
            !std::binary_search(copies, copies + walk->copies.Size(), reinterpret_cast<uintptr_t>(object)))
        {
            uint8_t* start = reinterpret_cast<uint8_t*>(object);
            walk->callback(start, start + size, 0, walk->context, false, false);
        }
        return true;
    }, &walk);
    if (result != LxrRcResult::Success)
    {
        Fatal("LXR survivor walk failed.");
    }
}

void LxrGcHeap::DiagWalkFinalizeQueue(void*, fq_walk_fn callback)
{
    ValidateWalk("LXR DiagWalkFinalizeQueue requires suspension.");
    m_registry.ForEachReady([&](Object* object, bool critical) { callback(critical, object); });
}

void LxrGcHeap::DiagScanFinalizeQueue(fq_scan_fn callback, ScanContext* scan)
{
    ValidateWalk("LXR DiagScanFinalizeQueue requires suspension.");
    m_registry.ForEachReady([&](Object* object, bool)
    {
        Object* local = object;
        callback(&local, scan, 0);
    });
}

void LxrGcHeap::DiagScanHandles(handle_scan_fn callback, int, ScanContext* context)
{
    GCScan::GcScanHandlesForProfilerAndETW(2, context, callback);
}

void LxrGcHeap::DiagScanDependentHandles(handle_scan_fn callback, int, ScanContext* context)
{
    GCScan::GcScanDependentHandlesForProfilerAndETW(2, context, callback);
}

void LxrGcHeap::DiagDescrGenerations(gen_walk_fn callback, void* context)
{
    uint8_t* start = reinterpret_cast<uint8_t*>(m_engine.GetReservationStart()) + sizeof(ObjHeader);
    uint8_t* end = reinterpret_cast<uint8_t*>(m_engine.GetReservationStart() + m_engine.GetReservationBytes());
    callback(context, 2, start, end, end);
}

void LxrGcHeap::DiagTraceGCSegments()
{
    GCToEEInterface::EventSink()->FireGCCreateSegment_V1(
        reinterpret_cast<void*>(m_engine.GetReservationStart()), m_engine.GetReservationBytes(), 0);
}

void LxrGcHeap::DiagGetGCSettings(EtwGCSettingsInfo* settings)
{
    *settings = {};
    settings->loh_threshold = GetLOHThreshold();
    settings->concurrent_gc_p = IsConcurrentGCEnabled();
    settings->use_frozen_segments_p = true;
}

unsigned LxrGcHeap::GetGenerationWithRange(
    Object* object, uint8_t** start, uint8_t** allocated, uint8_t** reserved)
{
    uintptr_t address = reinterpret_cast<uintptr_t>(object);
    if (!m_engine.OwnsAddress(address))
    {
        *start = nullptr;
        *allocated = nullptr;
        *reserved = nullptr;
        for (const FrozenSegment& segment : m_frozen)
        {
            if (segment.state.load(std::memory_order_seq_cst) == 2 && address >= segment.start &&
                address < segment.allocated.load(std::memory_order_acquire))
            {
                *start = reinterpret_cast<uint8_t*>(segment.start);
                *allocated = reinterpret_cast<uint8_t*>(segment.allocated.load());
                *reserved = reinterpret_cast<uint8_t*>(segment.reserved);
                break;
            }
        }
        return static_cast<unsigned>(INT32_MAX);
    }
    *start = reinterpret_cast<uint8_t*>(m_engine.GetReservationStart()) + sizeof(ObjHeader);
    *allocated = reinterpret_cast<uint8_t*>(m_engine.GetReservationStart() + m_engine.GetReservationBytes());
    *reserved = *allocated;
    return 2;
}

void LxrGcHeap::ControlEvents(GCEventKeyword keyword, GCEventLevel level)
{
    GCEventStatus::Set(GCEventProvider_Default, keyword, level);
}

void LxrGcHeap::ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level)
{
    GCEventStatus::Set(GCEventProvider_Private, keyword, level);
}

void LxrGcHeap::NullBridgeObjectsWeakRefs(size_t, void*)
{
    Fatal("LXR prototype does not implement cross-runtime Java bridge reclamation.");
}

uintptr_t* LxrGcHeap::GetPendingBridgeHandles(size_t* count)
{
    *count = 0;
    return nullptr;
}

// LXR uses the barrier mechanism of PR #3316: it is a custom collector. The runtime asks for the barrier helpers in
// GetAssignRefFunctions and calls the hooks below. Because LXR needs the old value of every overwritten field, it
// also sets FASTGC_REQUIRES_OLD_VALUE and provides the pre-store range hook (interface 6.1).
void LxrGcHeap::GetFastGCFunctions(FastGCFunctions* functions)
{
    *functions = {};
    functions->context = this;
    functions->write_barrier = &LxrGcHeap::NoPostStoreBarrier;
    functions->is_in_gc_heap = &LxrGcHeap::IsInHeap;
    functions->check_escape = &LxrGcHeap::CheckEscape;
    functions->bulk_move_with_write_barrier = &LxrGcHeap::BulkMoveWithWriteBarrier;
    functions->pre_store_range = &LxrGcHeap::PreStoreRange;
    functions->flags = FASTGC_REQUIRES_OLD_VALUE;
}

GCHeapType LxrGcHeap::GetGCHeapType() { return GC_HEAP_CUSTOM; }

uint64_t LxrGcHeap::GetTotalAllocatedBytesPrecise() { return GetTotalAllocatedBytes(); }

static_assert(sizeof(LxrWriteBarrierPatchable) == sizeof(GCWriteBarrierPatchableValue) &&
                  offsetof(LxrWriteBarrierPatchable, slot) == offsetof(GCWriteBarrierPatchableValue, Slot) &&
                  offsetof(LxrWriteBarrierPatchable, source) == offsetof(GCWriteBarrierPatchableValue, Source),
              "LxrWriteBarrierPatchable must match GCWriteBarrierPatchableValue");

void LxrGcHeap::GetAssignRefFunctions(void** assignRef, size_t* assignRefSize, void** assignRefChecked, size_t* assignRefCheckedSize, uintptr_t** avLocationsList, GCWriteBarrierPatchableValue** patchableValues)
{
    LxrWriteBarrierBlockInfo block;
    LxrWriteBarrierGetBlockInfo(&block);
    *assignRef = block.assign_ref;
    *assignRefSize = block.assign_ref_size;
    *assignRefChecked = block.checked_assign_ref;
    *assignRefCheckedSize = block.checked_assign_ref_size;
    *avLocationsList = block.av_locations;
    *patchableValues = reinterpret_cast<GCWriteBarrierPatchableValue*>(block.patchable);
}

ObjectReferenceEnumerationParameters* LxrGcHeap::GetObjectReferenceEnumerationParameters() { return &m_enumeration; }
ObjectHeaderBitsParameters* LxrGcHeap::GetObjectHeaderBitsParameters() { return &m_header_bits; }

LxrRcResult LxrGcHeap::HoldPrototypeTrace(bool hold)
{
    bool cooperative = EnterCollectionLock();
    LxrRcResult result = m_engine.HoldTrace(hold);
    LeaveCollectionLock(cooperative);
    m_trace_signal.Set();
    return result;
}

LxrRcResult LxrGcHeap::ArmPrototypeScan(OBJECTHANDLE handle)
{
    bool cooperative = EnterCollectionLock();
    LxrRcResult result = m_engine.ArmScanCheckpoint(reinterpret_cast<Object*>(GetHandleAddress(handle)));
    LeaveCollectionLock(cooperative);
    return result;
}

bool LxrGcHeap::IsOwnedHandle(OBJECTHANDLE handle) const
{
    Object** slot = reinterpret_cast<Object**>(reinterpret_cast<uintptr_t>(handle) & ~static_cast<uintptr_t>(1));
    return slot != nullptr && m_engine.IsOwnedObject(*slot);
}

uintptr_t LxrGcHeap::GetOwnedStart() const { return m_engine.GetReservationStart(); }
size_t LxrGcHeap::GetOwnedBytes() const { return m_engine.GetReservationBytes(); }
uintptr_t LxrGcHeap::GetHandleAddress(OBJECTHANDLE handle) const
{
    Object** slot = reinterpret_cast<Object**>(reinterpret_cast<uintptr_t>(handle) & ~static_cast<uintptr_t>(1));
    return slot == nullptr ? 0 : reinterpret_cast<uintptr_t>(*slot);
}

size_t LxrGcHeap::GetAddressExtent(uintptr_t address) const
{
    return m_engine.GetLiveObjectSize(reinterpret_cast<Object*>(address));
}

segment_handle LxrGcHeap::RegisterPrototypeFrozenArray(
    void* memory, size_t capacity, OBJECTHANDLE array, OBJECTHANDLE target)
{
    bool cooperative = EnterCollectionLock();
    Object* templateObject = reinterpret_cast<Object*>(GetHandleAddress(array));
    Object* referent = reinterpret_cast<Object*>(GetHandleAddress(target));
    segment_handle result = nullptr;
    if (memory != nullptr && templateObject != nullptr && referent != nullptr &&
        templateObject->GetGCSafeMethodTable()->HasComponentSize() &&
        templateObject->GetGCSafeMethodTable()->RawGetComponentSize() == sizeof(Object*) &&
        templateObject->GetGCSafeMethodTable()->ContainsGCPointers() &&
        reinterpret_cast<ArrayBase*>(templateObject)->GetNumComponents() == 1 &&
        m_engine.IsOwnedObject(referent))
    {
        size_t size = templateObject->GetSize();
        if (size <= capacity && sizeof(ObjHeader) <= capacity - size)
        {
            memcpy(memory, templateObject->GetHeader(), size);
            Object* frozen = reinterpret_cast<Object*>(static_cast<uint8_t*>(memory) + sizeof(ObjHeader));
            size_t fields = 0;
            GCReferenceRanges::Enumerate(frozen, [&](Object** first, size_t count)
            {
                for (size_t index = 0; index < count; index++)
                {
                    first[index] = referent;
                    fields++;
                }
                return true;
            });
            if (fields == 1)
            {
                segment_info info{};
                info.pvMem = memory;
                info.ibFirstObject = sizeof(ObjHeader);
                info.ibAllocated = sizeof(ObjHeader) + size;
                info.ibCommit = capacity;
                info.ibReserved = capacity;
                result = RegisterFrozenSegment(&info);
            }
        }
    }
    if (result == nullptr)
    {
        Unsupported("LXR frozen validation requires writable owned fixture storage, a one-element reference array and an owned target.");
    }
    LeaveCollectionLock(cooperative);
    return result;
}

LxrRcResult LxrGcHeap::ArmPrototypeLifetime(OBJECTHANDLE handle)
{
    if (handle == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    bool cooperative = EnterCollectionLock();
    Object** slot = reinterpret_cast<Object**>(
        reinterpret_cast<uintptr_t>(handle) & ~static_cast<uintptr_t>(1));
    LxrRcResult result = m_engine.ArmLifetimeProbe(*slot);
    LeaveCollectionLock(cooperative);
    return result;
}

LxrRcResult LxrGcHeap::EnablePrototypeStorageDiagnostics()
{
    bool cooperative = EnterCollectionLock();
    LxrRcResult result = m_engine.EnableStorageDiagnostics(Timeout);
    if (result == LxrRcResult::Success)
    {
        m_storage_diagnostics_enabled.store(true, std::memory_order_release);
    }
    LeaveCollectionLock(cooperative);
    return result;
}

LxrRcResult LxrGcHeap::GetPrototypeStorageRange(
    uint32_t component, uintptr_t* start, size_t* bytes) const
{
    if (start == nullptr || bytes == nullptr)
    {
        return LxrRcResult::InvalidArgument;
    }
    if (component == 16)
    {
        // The per-object flag table was removed in P17 S7.
        *start = 0;
        *bytes = 0;
        return LxrRcResult::Success;
    }
    return m_engine.GetStorageRange(component, start, bytes);
}

uint64_t LxrGcHeap::GetPrototypeMetric(uint32_t metric) const
{
    LxrRcMetrics rc = m_engine.GetMetrics();
    LxrTraceMetrics trace = m_engine.GetTraceMetrics();
    LxrNurseryMetrics nursery = m_engine.GetNurseryMetrics();
    LxrMatureMetrics mature = m_engine.GetMatureMetrics();
    switch (metric)
    {
        case 0: return m_initialized.load();
        case 1: return rc.allocations;
        case 2: return rc.allocatedBytes;
        case 3: return m_engine.GetUsedBytesEstimate();
        case 4: return UINT64_MAX; // Per-object reclaimed bytes exist only in the test platform (P17 S3).
        case 5: return m_collections.load();
        case 6: return rc.largeObjectBytesReleased;
        case 7: return rc.blocksReleased;
        case 8: return rc.rootIncrements;
        case 9: return rc.deaths;
        case 10: return trace.initialMarks;
        case 11: return trace.finalMarks;
        case 12: return trace.fullMarks;
        case 13: return trace.objects;
        case 14: return trace.references;
        case 15: return trace.satbEntries;
        case 16: return trace.deletionMarks;
        case 17: return trace.forcedScans;
        case 18: return trace.splitPackets;
        case 19: return trace.cycleObjects;
        case 20: return trace.cycleBytes;
        case 21: return trace.interruptions;
        case 22: return rc.threadFlushes;
        case 23: return rc.incrementEntries;
        case 24: return rc.decrementEntries;
        case 25: return m_registry.GetPendingCount();
        case 26: return m_engine.GetCommittedBytes();
        case 27: return m_barrier_bootstraps.load();
        case 28: return static_cast<uint64_t>(m_engine.GetTracePhase());
        case 29: return trace.concurrentSlices;
        case 30: return m_engine.IsScanCheckpointActive();
        case 31: return trace.checkpointInterruptions;
        case 32: return m_engine.GetSpanMetric(0);
        case 33: return m_fast_refills.load();
        case 34: return m_unused_span_bytes.load();
        case 35: return m_engine.GetReservationBytes();
        case 36: return m_engine.GetAdmittedBytes();
        case 37: return m_engine.GetGrowthCount();
        case 38: return m_engine.GetNativePlaneCommittedBytes();
        case 39: return static_cast<uint64_t>(m_policy.SurvivalRatio() * 1000000000.0);
        case 40: return static_cast<uint64_t>(m_policy.MatureLivePages() * 1000.0);
        case 41: return m_policy.CycleHint();
        case 42: return m_policy.EmergencyHint();
        case 43: return static_cast<uint64_t>(m_collection_cause.load());
        case 44: return m_hard_limit;
        case 45: return m_native_plane_limit;
        case 46: return m_engine.GetLifetimeProbeBirths();
        case 47: return m_engine.GetLifetimeProbeReclaims();
        case 71: return 0; // The flag table of the collector (P17 S7 removed it).
        case 72: return m_engine.GetStorageMetric(23);
        case 73: return m_engine.GetStorageMetric(24);
        case 74: return m_engine.GetStorageMetric(25);
        case 75: return m_engine.GetStorageMetric(26);
        case 76: return 18 + LxrSideMetadataLayout::SpecCount;
        case 77: return m_engine.GetStorageMetric(27);
        case 78: return m_heap_configuration.evacuateNursery;
        case 79: return nursery.copiedObjects;
        case 80: return nursery.copiedBytes;
        case 81: return nursery.inPlacePinnedBytes;
        case 82: return nursery.copyFailures;
        case 83: return nursery.forwardingWaits;
        case 84: return nursery.retiredSourceBytes;
        case 85: return nursery.pauseCopiedBytes;
        case 86: return m_pause_young_bytes.load(std::memory_order_acquire);
        case 87: return m_heap_configuration.lineRecycling;
        case 88: return m_heap_configuration.copyLineRecycling;
        case 90: return m_heap_configuration.evacuateMature;
        case 91: return mature.eligibleBlocks;
        case 92: return mature.selectedBlocks;
        case 93: return mature.pinnedBlocks;
        case 94: return mature.estimatedCopyBytes;
        case 95: return mature.copiedObjects;
        case 96: return mature.copiedBytes;
        case 97: return mature.copyFailures;
        case 98: return mature.rememberedEntries;
        case 99: return mature.staleEntries;
        case 100: return mature.replayedEntries;
        case 101: return mature.retiredSourceBytes;
        case 102: return mature.releasedBlocks;
        case 103: return mature.invalidSlots;
        case 114: return minipal_hires_tick_frequency();
        case 115: return m_storage_diagnostics_enabled.load(std::memory_order_acquire)
            ? m_root_capture_ticks.load(std::memory_order_acquire) : UINT64_MAX;
        case 116: return mature.pauseCopiedBytes;
        default:
            if (metric >= 240 && metric < 240 + 4)
            {
                return m_engine.GetPauseTimers().GetMeta(metric - 240);
            }
            if (metric == 244)
            {
                return m_engine.GetPauseTimers().Enabled();
            }
            switch (metric)
            {
                case 245: return static_cast<uint64_t>(m_policy.MeasuredSurvivalRatio() * 1000000000.0);
                case 246: return m_engine.GetPauseSurvivorBytes();
                case 247: return m_pause_reserve_pages.load(std::memory_order_acquire);
                case 248: return m_pause_free_pages.load(std::memory_order_acquire);
                case 249: return ImmixBlockManager::GetFlipOverlaps();
                case 250: return ImmixBlockManager::GetFlipBlockedOperations();
                default: break;
            }
            switch (metric)
            {
                case 450: return m_registry.GetCountedEntries();
                case 451: return m_registry.GetRegistered();
                case 452: return m_registry_counters.retained.load(std::memory_order_relaxed);
                case 453: return m_registry_counters.released.load(std::memory_order_relaxed);
                case 454: return m_registry_counters.decisions.load(std::memory_order_relaxed);
                case 455: return m_registry.GetDropped();
                case 456: return m_registry.GetTaken();
                case 461: return m_registry.GetDuplicates();
                case 457: return m_registry_counters.walked.load(std::memory_order_relaxed);
                case 458: return m_engine.GetPinnedRootCount();
                case 459: return m_registry.GetCountedCapacity() * sizeof(uintptr_t);
                case 460: return m_registry.GetPendingCount();
                default: break;
            }
            if (metric >= 410 && metric < 410 + LxrReferenceCounting::OvershootMetricCount)
            {
                return m_engine.GetOvershootMetric(metric - 410);
            }
            if (metric == 410 + LxrReferenceCounting::OvershootMetricCount)
            {
                return m_engine.IsOvershootEnabled();
            }
            if (metric >= 256 && metric < 256 + LxrPauseTimers::PublishedCount)
            {
                return m_engine.GetPauseTimers().GetLast(metric - 256);
            }
            if (metric >= 340 && metric < 340 + LxrPauseTimers::PublishedCount)
            {
                return m_engine.GetPauseTimers().GetTotal(metric - 340);
            }
            if (metric >= 232 && metric < 237)
            {
                return m_engine.GetStorageMetric(metric - 180);
            }
            if (metric >= 208 && metric < 232)
            {
                return m_engine.GetStorageMetric(metric - 180);
            }
            if (metric >= 118 && metric < 118 + ScanMetricCount)
            {
                return m_storage_diagnostics_enabled.load(std::memory_order_acquire)
                    ? m_scan_metrics[metric - 118].load(std::memory_order_acquire) : UINT64_MAX;
            }
            if (metric >= 162 && metric < 162 + ScanMetricCount)
            {
                return m_storage_diagnostics_enabled.load(std::memory_order_acquire)
                    ? m_scan_cpu_nanoseconds[metric - 162].load(std::memory_order_acquire) : UINT64_MAX;
            }
            if (metric >= 104 && metric <= 113)
            {
                return m_engine.GetCostMetric(metric - 104);
            }
            if (metric >= 48 && metric <= 70)
            {
                return m_engine.GetStorageMetric(metric - 48);
            }
            Unsupported("LXR prototype metric index is invalid.");
            return UINT64_MAX;
    }
}
