// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_GC_H
#define LXR_GC_H

#include "gc.h"
#include "lxr_reference_counting.h"
#include "lxr_finalizer_pause.h"
#include "lxr_collection_policy.h"

class LxrGcHeap final : public IGCHeapInternal
{
public:
    LxrGcHeap();

    HRESULT Initialize() override;
    Object* Alloc(gc_alloc_context* context, size_t size, uint32_t flags) override;
    void FixAllocContext(gc_alloc_context* context, void* arg, void* heap) override;
    void PublishObject(uint8_t* object) override;
    HRESULT GarbageCollect(int generation, bool lowMemory, int mode) override;
    void Shutdown() override;

    int GetNumberOfHeaps() override;
    int GetHomeHeapNumber() override;
    size_t GetPromotedBytes(int heap) override;
    bool IsPromoted2(Object* object, bool verifyHeader) override;
    bool IsPromoted(Object* object) override;
    bool IsHeapPointer(void* object, bool smallOnly) override;
    bool IsEphemeral(Object* object) override;
    unsigned WhichGeneration(Object* object) override;
    unsigned GetCondemnedGeneration() override;
    bool IsThreadUsingAllocationContextHeap(gc_alloc_context* context, int thread) override;
    Object* GetContainingObject(void* interior, bool collectedOnly) override;
    Object* NextObj(Object* object) override;
    bool IsLargeObject(Object* object) override;
    void ValidateObjectMember(Object* object) override;

    bool IsGCInProgressHelper(bool considerStart) override;
    void SetGCInProgress(bool value) override;
    void SetSuspensionPending(bool value) override;
    bool RuntimeStructuresValid() override;
    void SetYieldProcessorScalingFactor(float value) override;
    uint32_t WaitUntilGCComplete(bool considerStart) override;
    void SetWaitForGCEvent() override;
    void ResetWaitForGCEvent() override;
    bool IsConcurrentGCInProgress() override;
    bool IsConcurrentGCEnabled() override;
    void TemporaryEnableConcurrentGC() override;
    void TemporaryDisableConcurrentGC() override;
    void WaitUntilConcurrentGCComplete() override;
    HRESULT WaitUntilConcurrentGCCompleteAsync(int timeout) override;

    size_t GetNumberOfFinalizable() override;
    Object* GetNextFinalizable() override;
    bool RegisterForFinalization(int generation, Object* object) override;
    void SetFinalizationRun(Object* object) override;
    FinalizerWorkItem* GetExtraWorkForFinalization() override;

    size_t GetValidSegmentSize(bool large) override;
    void SetReservedVMLimit(size_t limit) override;
    size_t GetTotalBytesInUse() override;
    uint64_t GetTotalAllocatedBytes() override;
    size_t GetCurrentObjSize() override;
    int CollectionCount(int generation, int kind) override;
    unsigned GetGcCount() override;
    uint32_t GetMemoryLoad() override;
    int GetGcLatencyMode() override;
    int SetGcLatencyMode(int mode) override;
    int GetLOHCompactionMode() override;
    void SetLOHCompactionMode(int mode) override;
    int GetLastGCPercentTimeInGC() override;
    size_t GetLastGCGenerationSize(int generation) override;
    size_t GetLastGCStartTime(int generation) override;
    size_t GetLastGCDuration(int generation) override;
    size_t GetNow() override;
    int64_t GetTotalPauseDuration() override;
    uint64_t GetGenerationBudget(int generation) override;
    size_t GetLOHThreshold() override;
    void EnumerateConfigurationValues(void* context, ConfigurationValueFunc callback) override;
    int RefreshMemoryLimit() override;
    void GetMemoryInfo(
        uint64_t* highThreshold, uint64_t* available, uint64_t* load, uint64_t* heap,
        uint64_t* fragmentation, uint64_t* committed, uint64_t* promoted, uint64_t* pinned,
        uint64_t* pending, uint64_t* index, uint32_t* generation, uint32_t* pausePercent,
        bool* compacting, bool* concurrent, uint64_t* generations, uint64_t* pauses, int kind) override;

    bool RegisterForFullGCNotification(uint32_t generation, uint32_t large) override;
    bool CancelFullGCNotification() override;
    int WaitForFullGCApproach(int timeout) override;
    int WaitForFullGCComplete(int timeout) override;
    int StartNoGCRegion(uint64_t total, bool hasLarge, uint64_t large, bool disallow) override;
    int EndNoGCRegion() override;
    enable_no_gc_region_callback_status EnableNoGCRegionCallback(
        NoGCRegionCallbackFinalizerWorkItem* callback, uint64_t threshold) override;
    bool StressHeap(gc_alloc_context* context) override;

    segment_handle RegisterFrozenSegment(segment_info* info) override;
    void UnregisterFrozenSegment(segment_handle segment) override;
    bool IsInFrozenSegment(Object* object) override;
    void UpdateFrozenSegment(segment_handle segment, uint8_t* allocated, uint8_t* committed) override;

    void DiagWalkObject(Object* object, walk_fn callback, void* context) override;
    void DiagWalkObject2(Object* object, walk_fn2 callback, void* context) override;
    void DiagWalkHeap(walk_fn callback, void* context, int generation, bool large) override;
    void DiagWalkHeapWithACHandling(walk_fn callback, void* context, int generation, bool large) override;
    void DiagWalkSurvivorsWithType(
        void* gcContext, record_surv_fn callback, void* context, walk_surv_type type, int generation) override;
    void DiagWalkFinalizeQueue(void* gcContext, fq_walk_fn callback) override;
    void DiagScanFinalizeQueue(fq_scan_fn callback, ScanContext* context) override;
    void DiagScanHandles(handle_scan_fn callback, int generation, ScanContext* context) override;
    void DiagScanDependentHandles(handle_scan_fn callback, int generation, ScanContext* context) override;
    void DiagDescrGenerations(gen_walk_fn callback, void* context) override;
    void DiagTraceGCSegments() override;
    void DiagGetGCSettings(EtwGCSettingsInfo* settings) override;
    unsigned GetGenerationWithRange(Object* object, uint8_t** start, uint8_t** allocated, uint8_t** reserved) override;
    void ControlEvents(GCEventKeyword keyword, GCEventLevel level) override;
    void ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level) override;
    void NullBridgeObjectsWeakRefs(size_t count, void* handles) override;
    uintptr_t* GetPendingBridgeHandles(size_t* count) override;
    void GetFastGCFunctions(FastGCFunctions* functions) override;
    GCHeapType GetGCHeapType() override;
    uint64_t GetTotalAllocatedBytesPrecise() override;
    void GetAssignRefFunctions(void** assignRef, size_t* assignRefSize, void** assignRefChecked, size_t* assignRefCheckedSize, uintptr_t** avLocationsList, GCWriteBarrierPatchableValue** patchableValues) override;

    // Called by the assembly write barrier (through LxrBarrierSlowPath) when the unlog bit of a field is set.
    static void BarrierSlowPath(Object** slot);

    ObjectReferenceEnumerationParameters* GetObjectReferenceEnumerationParameters() override;
    ObjectHeaderBitsParameters* GetObjectHeaderBitsParameters() override;

    LxrRcResult CollectPrototype(LxrPauseKind kind, bool waitForLazy = false);
    LxrRcResult HoldPrototypeTrace(bool hold);
    LxrRcResult ArmPrototypeScan(OBJECTHANDLE handle);
    LxrRcResult ArmPrototypeLifetime(OBJECTHANDLE handle);
    LxrRcResult EnablePrototypeStorageDiagnostics();
    LxrRcResult GetPrototypeStorageRange(uint32_t component, uintptr_t* start, size_t* bytes) const;
    uint64_t GetPrototypeMetric(uint32_t metric) const;
    bool IsOwnedHandle(OBJECTHANDLE handle) const;
    uintptr_t GetOwnedStart() const;
    size_t GetOwnedBytes() const;
    uintptr_t GetHandleAddress(OBJECTHANDLE handle) const;
    size_t GetAddressExtent(uintptr_t address) const;
    segment_handle RegisterPrototypeFrozenArray(
        void* memory, size_t capacity, OBJECTHANDLE array, OBJECTHANDLE target);

private:
    static constexpr size_t ImmixBytes = 8 * 1024 * 1024;
    static constexpr size_t LargeBytes = 16 * 1024 * 1024;
    static constexpr size_t MaximumMutators = 1024;
    static constexpr size_t MaximumRoots = 131072;
    static constexpr size_t MaximumFrozenSegments = 256;
    static constexpr uint32_t Timeout = 30000;

    struct FrozenSegment
    {
        std::atomic<uint8_t> state{0};
        uintptr_t start = 0;
        uintptr_t reserved = 0;
        std::atomic<uintptr_t> allocated{0};
        std::atomic<uintptr_t> committed{0};
    };

    struct ScanCostScope;
    struct WalkContext;

    static LxrGcHeap* s_heap;
    static void Suspend(void* context);
    static void Resume(bool finished, void* context);
    static void RetireAllocationContext(gc_alloc_context* context, void* heap);
    static bool CanAdmitRange(size_t firstSlot, size_t slotCount, size_t nativeBytes, void* heap);
    static LxrRcResult CaptureRoots(LxrReferenceCounting* engine, void* context);
    static LxrRcResult FinishTrace(LxrReferenceCounting* engine, void* context);
    static LxrRcResult FinishEpoch(LxrReferenceCounting* engine, void* context);
    static LxrRcResult FinishRelocation(LxrReferenceCounting* engine, void* context);
    static LxrRcResult FinishLazy(LxrReferenceCounting* engine, void* context);
    static void ScanOnGcThread(void* context);
    static void CoordinateTrace(void* context);
    static void CopyRoot(Object** slot, ScanContext* context, uint32_t flags);
    static void TraceRoot(Object** slot, ScanContext* context, uint32_t flags);
    static void RelocateRoot(Object** slot, ScanContext* context, uint32_t flags);
    static void LeaseWeakHandle(Object** slot, Object* secondary, uint32_t flags, ScanContext* context, bool dependent);
    static void LeaseDependentRoot(Object** slot, ScanContext* context, uint32_t flags);
    static void CALLBACK LeaseSyncBlock(
        PTR_UNCHECKED_OBJECTREF slot, uintptr_t* extra, uintptr_t scan, uintptr_t heap);
    static bool IsFinalizationSuppressed(Object* object);
    static bool WalkHeapObject(Object* object, size_t size, void* context);
    static bool IsImmortal(Object* object, void* context);

    // The hooks that LXR hands to the runtime in FastGCFunctions (PR #3316 interface plus the 6.1 extension), and the
    // function that the assembly write barrier calls. All of them log the OLD value of a field before it is
    // overwritten; none of them changes the field.
    static void LogOldValue(Object** slot);
    static void LogOldValues(Object** destination, size_t count);
    static void CheckEscape(Object** destination, Object* newReference);
    static bool IsInHeap(void* context, void* address);
    static void NoPostStoreBarrier(void* context, Object** destination, Object* newReference);
    static void PreStoreRange(void* context, Object** destination, Object** source, size_t count);
    static void BulkMoveWithWriteBarrier(void* context, void* destination, const void* source, size_t length);

    void Fatal(const char* message) const;
    void Unsupported(const char* operation) const;
    LxrRcMutator* FindOrAttach(gc_alloc_context* context, bool barrier);
    LxrRcResult RunScan(uint8_t kind);
    void ExecuteScan(uint8_t kind);
    void AddRoot(Object* object, uint32_t flags, bool trace);
    // P17 S6: the finalizer registry takes part in a pause at three places. CaptureFinalizers runs in the root
    // capture of every pause. DecideFinalization runs after marking in a tracing pause only. RelocateFinalizers
    // follows the relocation of roots and handles when objects were copied.
    void CaptureFinalizers();
    void DecideFinalization();
    void RelocateFinalizers();
    void CountScanWork(size_t metric);
    bool FrozenContains(uintptr_t address, bool retired) const;
    void ScanFrozenRoots();
    LxrRcResult CollectLocked(LxrPauseKind kind, bool forcedBlocking = false, bool waitForLazy = false);
    bool EnterCollectionLock();
    void LeaveCollectionLock(bool cooperative);
    void ValidateWalk(const char* operation) const;
    void PublishMemoryInfo();
    void RetireAllocationSpan(gc_alloc_context* context, bool subtractUnused);
    void RegisterFinalizable(Object* object, uint32_t flags);
    LxrRcResult TryGrow(bool large, size_t size);
    LxrRcResult CollectForPressure(bool emergency, bool user, bool heapFull = false);
    LxrRcResult UpdatePolicyHints();
    LxrCollectionCause GetCollectionCause(bool heapFull) const;
    size_t GetCollectionReservedPages(size_t headroomBytes) const;

    enum MemoryField : size_t
    {
        Available, HighThreshold, Load, HeapSize, Fragmentation, Committed, Promoted,
        Pins, Finalizers, CollectionIndex, Generation, PausePercent, Concurrent, Compacted,
        PauseTicks, SecondPauseTicks, HeapBefore, FragmentationBefore, MemoryFields,
    };

    LxrReferenceCounting m_engine;
    LxrCollectionPolicy m_policy;
    LxrPolicyConfiguration m_policy_configuration{};
    LxrRcConfiguration m_heap_configuration{};
    bool m_fast_enabled = true;
    // P26 (F8). Wait for the lazy decrements and the lazy sweep after every pause (the behaviour before F8).
    bool m_wait_for_lazy = false;
    size_t m_native_plane_limit = 0;
    size_t m_hard_limit = 0;
    std::atomic<LxrCollectionCause> m_collection_cause{LxrCollectionCause::None};
    ObjectReferenceEnumerationParameters m_enumeration;
    ObjectHeaderBitsParameters m_header_bits{};
    FrozenSegment m_frozen[MaximumFrozenSegments];
    std::atomic<uint32_t> m_frozen_readers;
    uint8_t* m_cards;
    uint8_t* m_bundles;
    CLRCriticalSection m_collection_lock;
    GCEvent m_gc_done;
    GCEvent m_trace_signal;
    GCEvent m_coordinator_done;
    GCEvent m_concurrent_done;
    GCEvent m_scan_signal;
    GCEvent m_scan_ready;
    GCEvent m_scan_done;
    GCEvent m_scan_exited;
    std::atomic<uint8_t> m_scan_kind{0};
    std::atomic<bool> m_scan_busy{false};
    std::atomic<bool> m_scan_stopping{false};
    std::atomic<bool> m_initialized;
    std::atomic<bool> m_in_gc;
    std::atomic<bool> m_shutdown;
    std::atomic<bool> m_concurrent_enabled;
    std::atomic<unsigned> m_collections;
    LxrFinalizerRegistry m_registry;
    LxrFinalizerPauseCounters m_registry_counters;
    // Set by Resume when it did not signal the finalizer thread because a cycle sweep was pending (see
    // GetNextFinalizable). FinishLazy sends the signal once the sweep has finished.
    std::atomic<bool> m_finalization_deferred{false};
    std::atomic<uint64_t> m_memory_info[4][MemoryFields];
    std::atomic<uint64_t> m_memory_sequence[4];
    LxrPauseKind m_current_pause_kind;
    bool m_forced_blocking = false;
    uint64_t m_heap_before;
    uint64_t m_fragmentation_before;
    uint64_t m_trace_heap_before;
    uint64_t m_trace_fragmentation_before;
    uint64_t m_initial_pause_ticks;
    std::atomic<LxrRcResult> m_scan_failure;
    std::atomic<int> m_latency;
    std::atomic<int> m_loh_mode;
    std::atomic<int64_t> m_total_pause_ticks;
    std::atomic<int64_t> m_last_pause_ticks;
    int64_t m_pause_start;
    uint64_t m_start_time;
    std::atomic<size_t> m_last_gc_start;
    std::atomic<size_t> m_last_gc_duration;
    std::atomic<size_t> m_last_heap_size;
    std::atomic<size_t> m_last_committed;
    std::atomic<size_t> m_last_fragmentation;
    std::atomic<uint32_t> m_last_memory_load;
    size_t m_reserved_limit;
    std::atomic<uint64_t> m_barrier_bootstraps;
    std::atomic<uint64_t> m_fast_refills{0};
    std::atomic<uint64_t> m_unused_span_bytes{0};
    std::atomic<uint64_t> m_context_allocated_bytes{0};
    std::atomic<uint64_t> m_pause_young_bytes{0};
    std::atomic<uint64_t> m_pause_reserve_pages{0};
    std::atomic<uint64_t> m_pause_free_pages{0};
    std::atomic<bool> m_storage_diagnostics_enabled{false};
    std::atomic<uint64_t> m_root_capture_ticks{0};
    static constexpr size_t ScanMetricCount = 44;
    std::atomic<uint64_t> m_scan_metrics[ScanMetricCount]{};
    std::atomic<uint64_t> m_scan_cpu_nanoseconds[ScanMetricCount]{};
#ifdef TARGET_WINDOWS
    BOOLEAN (WINAPI *m_process_exiting)();
#endif // TARGET_WINDOWS
};

#endif // LXR_GC_H
