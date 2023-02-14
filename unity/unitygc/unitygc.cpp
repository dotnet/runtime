//#include "common.h"

//#include "palclr.h"
#if defined(TARGET_WINDOWS)
#include "windows.h"
#else
 #include <sys/mman.h>
#endif

#include <cassert>
#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <mutex>
#include <unordered_map>

#include "gcenv.base.h"
#include "gcinterface.h"

#define UGC_ALIGN(size, align) \
    (((size)+((align)-1)) & ~((align)-1))

// we cannot include internal gc headers easily, this is a static assert
// in gcenv.object.h that we depend upon.
#define SIZEOF_OBJ_HEADER sizeof(uintptr_t)

static const char* sUnityGC = "UnityGC";

#ifdef unitygc_EXPORTS
#if defined(TARGET_WINDOWS)
#define UNITYGC_EXPORT __declspec(dllexport)
#else
#define UNITYGC_EXPORT
#endif
#else
#define UNITYGC_EXPORT
#endif

class GCHeap : public IGCHeap
{
public:
    /*
    ===========================================================================
    Hosting APIs. These are used by GC hosting. The code that
    calls these methods may possibly be moved behind the interface -
    today, the VM handles the setting of segment size and max gen 0 size.
    (See src/vm/corehost.cpp)
    ===========================================================================
    */

    // Returns whether or not the given size is a valid segment size.
    virtual bool IsValidSegmentSize(size_t size)
    {
        assert(0);
        return true;
    }

    // Returns whether or not the given size is a valid gen 0 max size.
    virtual bool IsValidGen0MaxSize(size_t size)
    {
        assert(0);
        return true;
    }

    // Gets a valid segment size.
    virtual size_t GetValidSegmentSize(bool large_seg = false)
    {
        assert(0);
        return 0;
    }

    // Sets the limit for reserved virtual memory.
    virtual void SetReservedVMLimit(size_t vmlimit)
    {
        assert(0);
    }

    /*
    ===========================================================================
    Concurrent GC routines. These are used in various places in the VM
    to synchronize with the GC, when the VM wants to update something that
    the GC is potentially using, if it's doing a background GC.

    Concrete examples of this are profiling/ETW scenarios.
    ===========================================================================
    */

    // Blocks until any running concurrent GCs complete.
    virtual void WaitUntilConcurrentGCComplete()
    {
        assert(0);
    }

    // Returns true if a concurrent GC is in progress, false otherwise.
    virtual bool IsConcurrentGCInProgress()
    {
        return false;
    }

    // Temporarily enables concurrent GC, used during profiling.
    virtual void TemporaryEnableConcurrentGC()
    {
        assert(0);
    }

    // Temporarily disables concurrent GC, used during profiling.
    virtual void TemporaryDisableConcurrentGC()
    {
        assert(0);
    }

    // Returns whether or not Concurrent GC is enabled.
    virtual bool IsConcurrentGCEnabled()
    {
        assert(0);
        return false;
    }

    // Wait for a concurrent GC to complete if one is in progress, with the given timeout.
    virtual HRESULT WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout)
    {
        assert(0);
        return E_FAIL;
    }    // Use in native threads. TRUE if succeed. FALSE if failed or timeout


    /*
    ===========================================================================
    Finalization routines. These are used by the finalizer thread to communicate
    with the GC.
    ===========================================================================
    */

    // Gets the number of finalizable objects.
    virtual size_t GetNumberOfFinalizable()
    {
        return 0;
    }

    // Gets the next finalizable object.
    virtual Object* GetNextFinalizable()
    {
        return NULL;
    }

    /*
    ===========================================================================
    BCL routines. These are routines that are directly exposed by CoreLib
    as a part of the `System.GC` class. These routines behave in the same
    manner as the functions on `System.GC`.
    ===========================================================================
    */

    // Gets memory related information the last GC observed. Depending on the last arg, this could
    // be any last GC that got recorded, or of the kind specified by this arg. All info below is
    // what was observed by that last GC.
    //
    // highMemLoadThreshold - physical memory load (in percentage) when GC will start to
    //   react aggressively to reclaim memory.
    // totalPhysicalMem - the total amount of phyiscal memory available on the machine and the memory
    //   limit set on the container if running in a container.
    // lastRecordedMemLoad - physical memory load in percentage.
    // lastRecordedHeapSizeBytes - total managed heap size.
    // lastRecordedFragmentation - total fragmentation in the managed heap.
    // totalCommittedBytes - total committed bytes by the managed heap.
    // promotedBytes - promoted bytes.
    // pinnedObjectCount - # of pinned objects observed.
    // finalizationPendingCount - # of objects ready for finalization.
    // index - the index of the GC.
    // generation - the generation the GC collected.
    // pauseTimePct - the % pause time in GC so far since process started.
    // isCompaction - compacted or not.
    // isConcurrent - concurrent or not.
    // genInfoRaw - info about each generation.
    // pauseInfoRaw - pause info.
    virtual void GetMemoryInfo(uint64_t* highMemLoadThresholdBytes,
                               uint64_t* totalAvailableMemoryBytes,
                               uint64_t* lastRecordedMemLoadBytes,
                               uint64_t* lastRecordedHeapSizeBytes,
                               uint64_t* lastRecordedFragmentationBytes,
                               uint64_t* totalCommittedBytes,
                               uint64_t* promotedBytes,
                               uint64_t* pinnedObjectCount,
                               uint64_t* finalizationPendingCount,
                               uint64_t* index,
                               uint32_t* generation,
                               uint32_t* pauseTimePct,
                               bool* isCompaction,
                               bool* isConcurrent,
                               uint64_t* genInfoRaw,
                               uint64_t* pauseInfoRaw,
                               int kind)
    {
        assert(0);
    }

    // Get the last memory load in percentage observed by the last GC.
    virtual uint32_t GetMemoryLoad()
    {
        assert(0);
        return 0;
    }

    // Gets the current GC latency mode.
    virtual int GetGcLatencyMode()
    {
        assert(0);
        return 0;
    }

    // Sets the current GC latency mode. newLatencyMode has already been
    // verified by CoreLib to be valid.
    virtual int SetGcLatencyMode(int newLatencyMode)
    {
        assert(0);
        return 0;
    }

    // Gets the current LOH compaction mode.
    virtual int GetLOHCompactionMode()
    {
        assert(0);
        return 0;
    }

    // Sets the current LOH compaction mode. newLOHCompactionMode has
    // already been verified by CoreLib to be valid.
    virtual void SetLOHCompactionMode(int newLOHCompactionMode)
    {
        assert(0);
    }

    // Registers for a full GC notification, raising a notification if the gen 2 or
    // LOH object heap thresholds are exceeded.
    virtual bool RegisterForFullGCNotification(uint32_t gen2Percentage, uint32_t lohPercentage)
    {
        return false;
    }

    // Cancels a full GC notification that was requested by `RegisterForFullGCNotification`.
    virtual bool CancelFullGCNotification()
    {
        assert(0);
        return false;
    }

    // Returns the status of a registered notification for determining whether a blocking
    // Gen 2 collection is about to be initiated, with the given timeout.
    virtual int WaitForFullGCApproach(int millisecondsTimeout)
    {
        assert(0);
        return 0;
    }

    // Returns the status of a registered notification for determining whether a blocking
    // Gen 2 collection has completed, with the given timeout.
    virtual int WaitForFullGCComplete(int millisecondsTimeout)
    {
        assert(0);
        return 0;
    }

    // Returns the generation in which obj is found. Also used by the VM
    // in some places, in particular syncblk code.
    virtual unsigned WhichGeneration(Object* obj)
    {
        assert(0);
        return 0;
    }

    // Returns the number of GCs that have transpired in the given generation
    // since the beginning of the life of the process. Also used by the VM
    // for debug code.
    virtual int CollectionCount(int generation, int get_bgc_fgc_coutn = 0)
    {
        return 0;
    }

    // Begins a no-GC region, returning a code indicating whether entering the no-GC
    // region was successful.
    virtual int StartNoGCRegion(uint64_t totalSize, bool lohSizeKnown, uint64_t lohSize, bool disallowFullBlockingGC)
    {
        assert(0);
        return 0;
    }

    // Exits a no-GC region.
    virtual int EndNoGCRegion()
    {
        assert(0);
        return 0;
    }

    // Gets the total number of bytes in use.
    virtual size_t GetTotalBytesInUse()
    {
        assert(0);
        return 0;
    }

    virtual uint64_t GetTotalAllocatedBytes()
    {
        assert(0);
        return 0;
    }

    // Forces a garbage collection of the given generation. Also used extensively
    // throughout the VM.
    virtual HRESULT GarbageCollect(int generation = -1, bool low_memory_p = false, int mode = collection_blocking)
    {
        return S_OK;
    }

    // Gets the largest GC generation. Also used extensively throughout the VM.
    virtual unsigned GetMaxGeneration()
    {
        // TODO: can this be 0?
        return 2;
    }

    // Indicates that an object's finalizer should not be run upon the object's collection.
    virtual void SetFinalizationRun(Object* obj)
    {
    }

    // Indicates that an object's finalizer should be run upon the object's collection.
    virtual bool RegisterForFinalization(int gen, Object* obj)
    {
        assert(0);
        return true;
    }

    virtual int GetLastGCPercentTimeInGC()
    {
        assert(0);
        return 0;
    }

    virtual size_t GetLastGCGenerationSize(int gen)
    {
        assert(0);
        return 0;
    }

    /*
    ===========================================================================
    Miscellaneous routines used by the VM.
    ===========================================================================
    */

    // Initializes the GC heap, returning whether or not the initialization
    // was successful.
    virtual HRESULT Initialize()
    {
        return S_OK;
    }

    // Returns whether nor this GC was promoted by the last GC.
    virtual bool IsPromoted(Object* object)
    {
        assert(0);
        return false;
    }

    // Returns true if this pointer points into a GC heap, false otherwise.
    virtual bool IsHeapPointer(void* object, bool small_heap_only = false)
    {
        return m_pGlobalHeapStart <= object && object <= m_pGlobalHeapCurrent;
    }

    // Return the generation that has been condemned by the current GC.
    virtual unsigned GetCondemnedGeneration()
    {
        assert(0);
        return 0;
    }

    // Returns whether or not a GC is in progress.
    virtual bool IsGCInProgressHelper(bool bConsiderGCStart = false)
    {
        return m_bGCInProgress;
    }

    // Returns the number of GCs that have occured. Mainly used for
    // sanity checks asserting that a GC has not occured.
    virtual unsigned GetGcCount()
    {
        return 0;
    }

    // Gets whether or not the home heap of this alloc context matches the heap
    // associated with this thread.
    virtual bool IsThreadUsingAllocationContextHeap(gc_alloc_context* acontext, int thread_number)
    {
        assert(0);
        return false;
    }

    // Returns whether or not this object resides in an ephemeral generation.
    virtual bool IsEphemeral(Object* object)
    {
        assert(0);
        return false;
    }

    // Blocks until a GC is complete, returning a code indicating the wait was successful.
    virtual uint32_t WaitUntilGCComplete(bool bConsiderGCStart = false)
    {
        return NOERROR;
    }

    // "Fixes" an allocation context by binding its allocation pointer to a
    // location on the heap.
    virtual void FixAllocContext(gc_alloc_context* acontext, void* arg, void* heap)
    {
        // TODO: is it okay to do nothing here?
    }

    // Gets the total survived size plus the total allocated bytes on the heap.
    virtual size_t GetCurrentObjSize()
    {
        assert(0);
        return 0;
    }

    // Sets whether or not a GC is in progress.
    virtual void SetGCInProgress(bool fInProgress)
    {
        m_bGCInProgress = fInProgress;
    }

    // Gets whether or not the GC runtime structures are in a valid state for heap traversal.
    virtual bool RuntimeStructuresValid()
    {
        return true;
    }

    // Tells the GC when the VM is suspending threads.
    virtual void SetSuspensionPending(bool fSuspensionPending)
    {
    }

    // Tells the GC how many YieldProcessor calls are equal to one scaled yield processor call.
    virtual void SetYieldProcessorScalingFactor(float yieldProcessorScalingFactor)
    {
    }

    // Flush the log and close the file if GCLog is turned on.
    virtual void Shutdown()
    {
    }

    /*
    ============================================================================
    Add/RemoveMemoryPressure support routines. These are on the interface
    for now, but we should move Add/RemoveMemoryPressure from the VM to the GC.
    When that occurs, these three routines can be removed from the interface.
    ============================================================================
    */

    // Get the timestamp corresponding to the last GC that occured for the
    // given generation.
    virtual size_t GetLastGCStartTime(int generation)
    {
        assert(0);
        return 0;
    }

    // Gets the duration of the last GC that occured for the given generation.
    virtual size_t GetLastGCDuration(int generation)
    {
        assert(0);
        return 0;
    }

    // Gets a timestamp for the current moment in time.
    virtual size_t GetNow()
    {
        assert(0);
        return 0;
    }

    /*
    ===========================================================================
    Allocation routines. These all call into the GC's allocator and may trigger a garbage
    collection. All allocation routines return NULL when the allocation request
    couldn't be serviced due to being out of memory.
    ===========================================================================
    */

    // Allocates an object on the given allocation context with the given size and flags.
    // It is the responsibility of the caller to ensure that the passed-in alloc context is
    // owned by the thread that is calling this function. If using per-thread alloc contexts,
    // no lock is needed; callers not using per-thread alloc contexts will need to acquire
    // a lock to ensure that the calling thread has unique ownership over this alloc context;
    virtual Object* Alloc(gc_alloc_context* acontext, size_t size, uint32_t flags)
    {
        const size_t k16K = 1ULL << 14;
        size_t actualSize = size + SIZEOF_OBJ_HEADER;
        // align to 8 minimallly
        actualSize = UGC_ALIGN(actualSize,sizeof(void*));

        if (!acontext->alloc_ptr || acontext->alloc_ptr + actualSize > acontext->alloc_limit)
        {
            size_t allocationSize = k16K;
            if (actualSize > allocationSize)
                allocationSize = UGC_ALIGN(actualSize,k16K);
#if defined(TARGET_WINDOWS)
            uint8_t* allocationEnd = (uint8_t*)_InlineInterlockedAdd64((LONG64*)&m_pGlobalHeapCurrent, allocationSize);
#else
            uint8_t* allocationEnd = (uint8_t*)__sync_add_and_fetch((intptr_t*)&m_pGlobalHeapCurrent, (intptr_t)allocationSize);
#endif
            uint8_t* allocationStart = allocationEnd - allocationSize;

#if defined(TARGET_WINDOWS)
            acontext->alloc_ptr = (uint8_t*)VirtualAlloc(allocationStart, allocationSize, MEM_COMMIT, PAGE_READWRITE);
#else
            int result = mprotect(allocationStart, allocationSize, PROT_READ | PROT_WRITE);
            assert(result == 0);
            acontext->alloc_ptr = allocationStart;
#endif
            acontext->alloc_limit = allocationEnd;
        }

        assert(acontext->alloc_ptr + actualSize <= acontext->alloc_limit);
        uint8_t* bytes = acontext->alloc_ptr;
        acontext->alloc_ptr += actualSize;

        return (Object*)(bytes + SIZEOF_OBJ_HEADER);
    }

    // This is for the allocator to indicate it's done allocating a large object during a
    // background GC as the BGC threads also need to walk UOH.
    virtual void PublishObject(uint8_t* obj)
    {
    }

    // Signals the WaitForGCEvent event, indicating that a GC has completed.
    virtual void SetWaitForGCEvent()
    {
    }

    // Resets the state of the WaitForGCEvent back to an unsignalled state.
    virtual void ResetWaitForGCEvent()
    {
    }

    /*
    ===========================================================================
    Heap verification routines. These are used during heap verification only.
    ===========================================================================
    */
    // Returns whether or not this object is too large for SOH.
    virtual bool IsLargeObject(Object* pObj)
    {
        return false;
    }

    // Walks an object and validates its members.
    virtual void ValidateObjectMember(Object* obj)
    {
    }

    // Retrieves the next object after the given object. When the EE
    // is not suspended, the result is not accurate - if the input argument
    // is in Gen0, the function could return zeroed out memory as the next object.
    virtual Object* NextObj(Object* object)
    {
        return NULL;
    }

    // Given an interior pointer, return a pointer to the object
    // containing that pointer. This is safe to call only when the EE is suspended.
    // When fCollectedGenOnly is true, it only returns the object if it's found in
    // the generation(s) that are being collected.
    virtual Object* GetContainingObject(void* pInteriorPtr, bool fCollectedGenOnly)
    {
        assert(0);
        return NULL;
    }

    /*
    ===========================================================================
    Profiling routines. Used for event tracing and profiling to broadcast
    information regarding the heap.
    ===========================================================================
    */

    // Walks an object, invoking a callback on each member.
    virtual void DiagWalkObject(Object* obj, walk_fn fn, void* context)
    {
        assert(0);
    }

    // Walks an object, invoking a callback on each member.
    virtual void DiagWalkObject2(Object* obj, walk_fn2 fn, void* context)
    {
        assert(0);
    }

    // Walk the heap object by object.
    virtual void DiagWalkHeap(walk_fn fn, void* context, int gen_number, bool walk_large_object_heap_p)
    {
        assert(0);
    }

    // Walks the survivors and get the relocation information if objects have moved.
    // gen_number is used when type == walk_for_uoh, otherwise ignored
    virtual void DiagWalkSurvivorsWithType(void* gc_context, record_surv_fn fn, void* diag_context, walk_surv_type type, int gen_number=-1)
    {
        assert(0);
    }

    // Walks the finalization queue.
    virtual void DiagWalkFinalizeQueue(void* gc_context, fq_walk_fn fn)
    {
        assert(0);
    }

    // Scan roots on finalizer queue. This is a generic function.
    virtual void DiagScanFinalizeQueue(fq_scan_fn fn, ScanContext* context)
    {
        assert(0);
    }

    // Scan handles for profiling or ETW.
    virtual void DiagScanHandles(handle_scan_fn fn, int gen_number, ScanContext* context)
    {
        assert(0);
    }

    // Scan dependent handles for profiling or ETW.
    virtual void DiagScanDependentHandles(handle_scan_fn fn, int gen_number, ScanContext* context)
    {
        assert(0);
    }

    // Describes all generations to the profiler, invoking a callback on each generation.
    virtual void DiagDescrGenerations(gen_walk_fn fn, void* context)
    {
        assert(0);
    }

    // Traces all GC segments and fires ETW events with information on them.
    virtual void DiagTraceGCSegments()
    {
        assert(0);
    }

    // Get GC settings for tracing purposes. These are settings not obvious from a trace.
    virtual void DiagGetGCSettings(EtwGCSettingsInfo* settings)
    {
        assert(0);
    }

    /*
    ===========================================================================
    GC Stress routines. Used only when running under GC Stress.
    ===========================================================================
    */

    // Returns TRUE if GC actually happens, otherwise FALSE. The passed alloc context
    // must not be null.
    virtual bool StressHeap(gc_alloc_context* acontext)
    {
        assert(0);
        return false;
    }

    /*
    ===========================================================================
    Routines to register read only segments for frozen objects.
    Only valid if FEATURE_BASICFREEZE is defined.
    ===========================================================================
    */

    // Registers a frozen segment with the GC.
    virtual segment_handle RegisterFrozenSegment(segment_info *pseginfo)
    {
        assert(0);
        return NULL;
    }

    // Unregisters a frozen segment.
    virtual void UnregisterFrozenSegment(segment_handle seg)
    {
        assert(0);
    }

    // Indicates whether an object is in a frozen segment.
    virtual bool IsInFrozenSegment(Object *object)
    {
        assert(0);
        return false;
    }

    /*
    ===========================================================================
    Routines for informing the GC about which events are enabled.
    ===========================================================================
    */

    // Enables or disables the given keyword or level on the default event provider.
    virtual void ControlEvents(GCEventKeyword keyword, GCEventLevel level)
    {
    }

    // Enables or disables the given keyword or level on the private event provider.
    virtual void ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level)
    {
    }

    virtual unsigned int GetGenerationWithRange(Object* object, uint8_t** ppStart, uint8_t** ppAllocated, uint8_t** ppReserved)
    {
        assert(0);
        return 0;
    }

    virtual int64_t GetTotalPauseDuration()
    {
        assert(0);
        return 0;
    };

    virtual void EnumerateConfigurationValues(void* context, ConfigurationValueFunc configurationValueFunc)
    {
    };

    GCHeap(IGCToCLR* pGCToCLR) :
        m_pGCToCLR(pGCToCLR),
        m_bGCInProgress(false)
    {

        // setup global heap we can bump allocation from
        const size_t k16GB = 1ULL << 34;
#if defined(TARGET_WINDOWS)
        m_pGlobalHeapStart = (uint8_t*)VirtualAlloc(NULL, k16GB, MEM_RESERVE, PAGE_READWRITE);
#else
        m_pGlobalHeapStart = (uint8_t*)mmap(NULL, k16GB, PROT_NONE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
#endif
        m_pGlobalHeapCurrent = m_pGlobalHeapStart;
        m_pGlobalHeapEnd = m_pGlobalHeapStart + k16GB;

        // setup a small unused range to avoid triggering write barriers which crashes accessing a card table we didn't setup
        WriteBarrierParameters args = {};
        args.operation = WriteBarrierOp::Initialize;
        args.is_runtime_suspended = true;
        args.requires_upper_bounds_check = false;

        args.lowest_address = (uint8_t*)m_pGlobalHeapEnd-1;
        args.highest_address = (uint8_t*)m_pGlobalHeapEnd;
        args.ephemeral_low = (uint8_t*)m_pGlobalHeapEnd-1;
        args.ephemeral_high = (uint8_t*)m_pGlobalHeapEnd;
        // how large does this need to be?
        args.card_table = (uint32_t*)calloc(1024*1024, sizeof(char));

        m_pGCToCLR->StompWriteBarrier(&args);
    }
    virtual ~GCHeap() {}

    private:
        IGCToCLR* m_pGCToCLR;
        uint8_t* m_pGlobalHeapStart;
        uint8_t* m_pGlobalHeapCurrent;
        uint8_t* m_pGlobalHeapEnd;
        bool m_bGCInProgress;

};

class GCHandleStore : public IGCHandleStore
{
    public:
    virtual void Uproot()
    {

    }

    virtual bool ContainsHandle(OBJECTHANDLE handle)
    {
        return false;
    }

    virtual OBJECTHANDLE CreateHandleOfType(Object* object, HandleType type)
    {
        Object** handle = (Object**)malloc(sizeof(Object*));
        *handle = object;
        return (OBJECTHANDLE)handle;
    }

    virtual OBJECTHANDLE CreateHandleOfType(Object* object, HandleType type, int heapToAffinitizeTo)
    {
        assert(0);
        return 0;
    }

    virtual OBJECTHANDLE CreateHandleWithExtraInfo(Object* object, HandleType type, void* pExtraInfo)
    {
        assert(0);
        return 0;
    }

    virtual OBJECTHANDLE CreateDependentHandle(Object* primary, Object* secondary)
    {
        auto handle = CreateHandleOfType(primary, HNDTYPE_DEFAULT);
        {
            std::lock_guard<std::mutex> lock(m_lock);
            m_dependentHandleSecondary.insert(std::unordered_map<OBJECTHANDLE, Object*>::value_type(handle, secondary));
        }

        return handle;
    }

    // helpers
    Object* GetDependentHandleSecondary(OBJECTHANDLE handle)
    {
        {
            std::lock_guard<std::mutex> lock(m_lock);
            auto iter = m_dependentHandleSecondary.find(handle);
            if (iter != m_dependentHandleSecondary.end())
                return iter->second;
        }

        return NULL;
    }

    void DestroyHandle(OBJECTHANDLE handle)
    {
        *(Object**)handle = NULL;
        free(handle);
    }


    void StoreObjectInHandle(OBJECTHANDLE handle, Object* object)
    {
        *(Object**)handle = object;
    }

    GCHandleStore()
    {
        static_assert(sizeof(OBJECTHANDLE) == sizeof(Object*),
            "Expected OBJECTHANDLE to be pointer sized.");
    }

    virtual ~GCHandleStore()
    {

    }
    private:
        std::mutex m_lock;
        std::unordered_map<OBJECTHANDLE, Object*> m_dependentHandleSecondary;
};

class GCHandleManager : public IGCHandleManager
{
    virtual bool Initialize()
    {
        m_pHandleStore = new GCHandleStore();
        return true;
    }

    virtual void Shutdown()
    {
    }

    virtual IGCHandleStore* GetGlobalHandleStore()
    {
        return m_pHandleStore;
    }

    virtual IGCHandleStore* CreateHandleStore()
    {
        assert(0);
        return NULL;
    }

    virtual void DestroyHandleStore(IGCHandleStore* store)
    {
        assert(0);
    }

    virtual OBJECTHANDLE CreateGlobalHandleOfType(Object* object, HandleType type)
    {
        return m_pHandleStore->CreateHandleOfType(object, type);
    }

    virtual OBJECTHANDLE CreateDuplicateHandle(OBJECTHANDLE handle)
    {
        assert(0);
        return NULL;
    }

    virtual void DestroyHandleOfType(OBJECTHANDLE handle, HandleType type)
    {
        m_pHandleStore->DestroyHandle(handle);
    }

    virtual void DestroyHandleOfUnknownType(OBJECTHANDLE handle)
    {
        m_pHandleStore->DestroyHandle(handle);
    }

    virtual void SetExtraInfoForHandle(OBJECTHANDLE handle, HandleType type, void* pExtraInfo)
    {
        assert(0);
    }

    virtual void* GetExtraInfoFromHandle(OBJECTHANDLE handle)
    {
        assert(0);
        return NULL;
    }

    virtual void StoreObjectInHandle(OBJECTHANDLE handle, Object* object)
    {
        m_pHandleStore->StoreObjectInHandle(handle, object);
    }

    virtual bool StoreObjectInHandleIfNull(OBJECTHANDLE handle, Object* object)
    {
        assert(0);
        return false;
    }

    virtual void SetDependentHandleSecondary(OBJECTHANDLE handle, Object* object)
    {
        assert(0);
    }

    virtual Object* GetDependentHandleSecondary(OBJECTHANDLE handle)
    {
        return m_pHandleStore->GetDependentHandleSecondary(handle);
    }

    virtual Object* InterlockedCompareExchangeObjectInHandle(OBJECTHANDLE handle, Object* object, Object* comparandObject)
    {
#if defined(TARGET_WINDOWS)
        return (Object*)InterlockedCompareExchangePointer((PVOID*)handle, object, comparandObject);
#else
        return (Object*)__sync_val_compare_and_swap ((Object**)handle, object, comparandObject);
#endif
    }

    virtual HandleType HandleFetchType(OBJECTHANDLE handle)
    {
        assert(0);
        return HNDTYPE_DEFAULT;
    }

    virtual void TraceRefCountedHandles(HANDLESCANPROC callback, uintptr_t param1, uintptr_t param2)
    {
        assert(0);
    }
    private:
        GCHandleStore* m_pHandleStore;

};

extern "C" UNITYGC_EXPORT void GC_VersionInfo(
    /* Out */ VersionInfo* versionInfo
)
{
    versionInfo->MajorVersion = GC_INTERFACE_MAJOR_VERSION;
    versionInfo->MinorVersion = GC_INTERFACE_MINOR_VERSION;
    versionInfo->BuildVersion = 0;
    versionInfo->Name = sUnityGC;
}

extern "C" UNITYGC_EXPORT HRESULT GC_Initialize(
    /* In  */ IGCToCLR* pGCToCLR,
    /* Out */ IGCHeap** ppGCHeap,
    /* Out */ IGCHandleManager** ppGCHandleManager,
    /* Out */ GcDacVars* pGcDacVars
)
{
    *ppGCHeap = new GCHeap(pGCToCLR);
    *ppGCHandleManager = new GCHandleManager();

    return S_OK;
}
