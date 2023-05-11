// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GC_INTERFACE_H_
#define _GC_INTERFACE_H_

// The major version of the IGCHeap interface. Breaking changes to this interface
// require bumps in the major version number.
#define GC_INTERFACE_MAJOR_VERSION 5

// The minor version of the IGCHeap interface. Non-breaking changes are required
// to bump the minor version number. GCs and EEs with minor version number
// mismatches can still interoperate correctly, with some care.
#define GC_INTERFACE_MINOR_VERSION 1

// The major version of the IGCToCLR interface. Breaking changes to this interface
// require bumps in the major version number.
#define EE_INTERFACE_MAJOR_VERSION 1

struct ScanContext;
struct gc_alloc_context;
class CrawlFrame;

// Callback passed to GcScanRoots.
typedef void promote_func(PTR_PTR_Object, ScanContext*, uint32_t);

// Callback passed to GcEnumAllocContexts.
typedef void enum_alloc_context_func(gc_alloc_context*, void*);

// Callback passed to CreateBackgroundThread.
typedef uint32_t (__stdcall *GCBackgroundThreadFunction)(void* param);

// Struct often used as a parameter to callbacks.
typedef struct
{
    promote_func*  f;
    ScanContext*   sc;
    CrawlFrame *   cf;
} GCCONTEXT;

// SUSPEND_REASON is the reason why the GC wishes to suspend the EE,
// used as an argument to IGCToCLR::SuspendEE.
typedef enum
{
    SUSPEND_FOR_GC = 1,
    SUSPEND_FOR_GC_PREP = 6
} SUSPEND_REASON;

typedef enum
{
    walk_for_gc = 1,
    walk_for_bgc = 2,
    walk_for_uoh = 3
} walk_surv_type;

// Different operations that can be done by GCToEEInterface::StompWriteBarrier
enum class WriteBarrierOp
{
    StompResize,
    StompEphemeral,
    Initialize,
    SwitchToWriteWatch,
    SwitchToNonWriteWatch
};

// Arguments to GCToEEInterface::StompWriteBarrier
struct WriteBarrierParameters
{
    // The operation that StompWriteBarrier will perform.
    WriteBarrierOp operation;

    // Whether or not the runtime is currently suspended. If it is not,
    // the EE will need to suspend it before bashing the write barrier.
    // Used for all operations.
    bool is_runtime_suspended;

    // Whether or not the GC has moved the ephemeral generation to no longer
    // be at the top of the heap. When the ephemeral generation is at the top
    // of the heap, and the write barrier observes that a pointer is greater than
    // g_ephemeral_low, it does not need to check that the pointer is less than
    // g_ephemeral_high because there is nothing in the GC heap above the ephemeral
    // generation. When this is not the case, however, the GC must inform the EE
    // so that the EE can switch to a write barrier that checks that a pointer
    // is both greater than g_ephemeral_low and less than g_ephemeral_high.
    // Used for WriteBarrierOp::StompResize.
    bool requires_upper_bounds_check;

    // The new card table location. May or may not be the same as the previous
    // card table. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint32_t* card_table;

    // The new card bundle table location. May or may not be the same as the previous
    // card bundle table. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint32_t* card_bundle_table;

    // The heap's new low boundary. May or may not be the same as the previous
    // value. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint8_t* lowest_address;

    // The heap's new high boundary. May or may not be the same as the previous
    // value. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint8_t* highest_address;

    // The new start of the ephemeral generation.
    // Used for WriteBarrierOp::StompEphemeral.
    uint8_t* ephemeral_low;

    // The new end of the ephemeral generation.
    // Used for WriteBarrierOp::StompEphemeral.
    uint8_t* ephemeral_high;

    // The new write watch table, if we are using our own write watch
    // implementation. Used for WriteBarrierOp::SwitchToWriteWatch only.
    uint8_t* write_watch_table;

    // mapping table from region index to generation
    uint8_t* region_to_generation_table;

    // shift count - how many bits to shift right to obtain region index from address
    uint8_t  region_shr;

    // whether to use the more precise but slower write barrier
    bool region_use_bitwise_write_barrier;
};

struct EtwGCSettingsInfo
{
    size_t heap_hard_limit;
    size_t loh_threshold;
    size_t physical_memory_from_config;
    size_t gen0_min_budget_from_config;
    size_t gen0_max_budget_from_config;
    uint32_t high_mem_percent_from_config;
    bool concurrent_gc_p;
    bool use_large_pages_p;
    bool use_frozen_segments_p;
    // If this is false, it means the hardlimit was set implicitly by the container.
    bool hard_limit_config_p;
    bool no_affinitize_p;
};

// Opaque type for tracking object pointers
#ifndef DACCESS_COMPILE
struct OBJECTHANDLE__
{
    void* unused;
};
typedef struct OBJECTHANDLE__* OBJECTHANDLE;
#else
typedef uintptr_t OBJECTHANDLE;
#endif

 /*
  * Scanning callback.
  */
typedef void (CALLBACK *HANDLESCANPROC)(PTR_UNCHECKED_OBJECTREF pref, uintptr_t *pExtraInfo, uintptr_t param1, uintptr_t param2);

#include "gcinterface.ee.h"

// The allocation context must be known to the VM for use in the allocation
// fast path and known to the GC for performing the allocation. Every Thread
// has its own allocation context that it hands to the GC when allocating.
struct gc_alloc_context
{
    uint8_t*       alloc_ptr;
    uint8_t*       alloc_limit;
    int64_t        alloc_bytes; //Number of bytes allocated on SOH by this context
    int64_t        alloc_bytes_uoh; //Number of bytes allocated not on SOH by this context
    // These two fields are deliberately not exposed past the EE-GC interface.
    void*          gc_reserved_1;
    void*          gc_reserved_2;
    int            alloc_count;
public:

    void init()
    {
        LIMITED_METHOD_CONTRACT;

        alloc_ptr = 0;
        alloc_limit = 0;
        alloc_bytes = 0;
        alloc_bytes_uoh = 0;
        gc_reserved_1 = 0;
        gc_reserved_2 = 0;
        alloc_count = 0;
    }
};

#include "gcinterface.dac.h"

// stub type to abstract a heap segment
struct gc_heap_segment_stub;
typedef gc_heap_segment_stub *segment_handle;

struct segment_info
{
    void * pvMem; // base of the allocation, not the first object (must add ibFirstObject)
    size_t ibFirstObject;   // offset to the base of the first object in the segment
    size_t ibAllocated; // limit of allocated memory in the segment (>= firstobject)
    size_t ibCommit; // limit of committed memory in the segment (>= allocated)
    size_t ibReserved; // limit of reserved memory in the segment (>= commit)
};

#ifdef PROFILING_SUPPORTED
#define GC_PROFILING       //Turn on profiling
#endif // PROFILING_SUPPORTED

#define LARGE_OBJECT_SIZE ((size_t)(85000))

// The minimum size of an object is three pointers wide: one for the syncblock,
// one for the object header, and one for the first field in the object.
#define min_obj_size ((sizeof(uint8_t*) + sizeof(uintptr_t) + sizeof(size_t)))

// The bit shift used to convert a memory address into an index into the
// Software Write Watch table.
#define SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift 0xc

class Object;
class IGCHeap;
class IGCHandleManager;

#ifdef WRITE_BARRIER_CHECK
//always defined, but should be 0 in Server GC
extern uint8_t* g_GCShadow;
extern uint8_t* g_GCShadowEnd;
// saves the g_lowest_address in between GCs to verify the consistency of the shadow segment
extern uint8_t* g_shadow_lowest_address;
#endif

/*
 * GCEventProvider represents one of the two providers that the GC can
 * fire events from: the default and private providers.
 */
enum GCEventProvider
{
    GCEventProvider_Default = 0,
    GCEventProvider_Private = 1
};

// Event levels corresponding to events that can be fired by the GC.
enum GCEventLevel
{
    GCEventLevel_None = 0,
    GCEventLevel_Fatal = 1,
    GCEventLevel_Error = 2,
    GCEventLevel_Warning = 3,
    GCEventLevel_Information = 4,
    GCEventLevel_Verbose = 5,
    GCEventLevel_Max = 6,
    GCEventLevel_LogAlways = 255
};

// Event keywords corresponding to events that can be fired by the GC. These
// numbers come from the ETW manifest itself - please make changes to this enum
// if you add, remove, or change keyword sets that are used by the GC!
enum GCEventKeyword
{
    GCEventKeyword_None                          =       0x0,
    GCEventKeyword_GC                            =       0x1,
    // Duplicate on purpose, GCPrivate is the same keyword as GC,
    // with a different provider
    GCEventKeyword_GCPrivate                     =       0x1,
    GCEventKeyword_GCHandle                      =       0x2,
    GCEventKeyword_GCHandlePrivate               =    0x4000,
    GCEventKeyword_GCHeapDump                    =  0x100000,
    GCEventKeyword_GCSampledObjectAllocationHigh =  0x200000,
    GCEventKeyword_GCHeapSurvivalAndMovement     =  0x400000,
    GCEventKeyword_GCHeapCollect                 =  0x800000,
    GCEventKeyword_GCHeapAndTypeNames            = 0x1000000,
    GCEventKeyword_GCSampledObjectAllocationLow  = 0x2000000,
    GCEventKeyword_All = GCEventKeyword_GC
      | GCEventKeyword_GCPrivate
      | GCEventKeyword_GCHandle
      | GCEventKeyword_GCHandlePrivate
      | GCEventKeyword_GCHeapDump
      | GCEventKeyword_GCSampledObjectAllocationHigh
      | GCEventKeyword_GCHeapSurvivalAndMovement
      | GCEventKeyword_GCHeapCollect
      | GCEventKeyword_GCHeapAndTypeNames
      | GCEventKeyword_GCSampledObjectAllocationLow
};

// !!!!!!!!!!!!!!!!!!!!!!!
// make sure you change the def in bcl\system\gc.cs
// if you change this!
enum collection_mode
{
    collection_non_blocking = 0x00000001,
    collection_blocking = 0x00000002,
    collection_optimized = 0x00000004,
    collection_compacting = 0x00000008,
    collection_aggressive = 0x00000010
#ifdef STRESS_HEAP
    , collection_gcstress = 0x80000000
#endif // STRESS_HEAP
};

// !!!!!!!!!!!!!!!!!!!!!!!
// make sure you change the def in bcl\system\gc.cs
// if you change this!
enum wait_full_gc_status
{
    wait_full_gc_success = 0,
    wait_full_gc_failed = 1,
    wait_full_gc_cancelled = 2,
    wait_full_gc_timeout = 3,
    wait_full_gc_na = 4
};

// !!!!!!!!!!!!!!!!!!!!!!!
// make sure you change the def in bcl\system\gc.cs
// if you change this!
enum start_no_gc_region_status
{
    start_no_gc_success = 0,
    start_no_gc_no_memory = 1,
    start_no_gc_too_large = 2,
    start_no_gc_in_progress = 3
};

enum end_no_gc_region_status
{
    end_no_gc_success = 0,
    end_no_gc_not_in_progress = 1,
    end_no_gc_induced = 2,
    end_no_gc_alloc_exceeded = 3
};

enum gc_kind
{
    gc_kind_any = 0,           // any of the following kind
    gc_kind_ephemeral = 1,     // gen0 or gen1 GC
    gc_kind_full_blocking = 2, // blocking gen2 GC
    gc_kind_background = 3     // background GC (always gen2)
};

typedef enum
{
    /*
     * WEAK HANDLES
     *
     * Weak handles are handles that track an object as long as it is alive,
     * but do not keep the object alive if there are no strong references to it.
     *
     */

    /*
     * SHORT-LIVED WEAK HANDLES
     *
     * Short-lived weak handles are weak handles that track an object until the
     * first time it is detected to be unreachable.  At this point, the handle is
     * severed, even if the object will be visible from a pending finalization
     * graph.  This further implies that short weak handles do not track
     * across object resurrections.
     *
     */
    HNDTYPE_WEAK_SHORT   = 0,

    /*
     * LONG-LIVED WEAK HANDLES
     *
     * Long-lived weak handles are weak handles that track an object until the
     * object is actually reclaimed.  Unlike short weak handles, long weak handles
     * continue to track their referents through finalization and across any
     * resurrections that may occur.
     *
     */
    HNDTYPE_WEAK_LONG    = 1,
    HNDTYPE_WEAK_DEFAULT = 1,

    /*
     * STRONG HANDLES
     *
     * Strong handles are handles which function like a normal object reference.
     * The existence of a strong handle for an object will cause the object to
     * be promoted (remain alive) through a garbage collection cycle.
     *
     */
    HNDTYPE_STRONG       = 2,
    HNDTYPE_DEFAULT      = 2,

    /*
     * PINNED HANDLES
     *
     * Pinned handles are strong handles which have the added property that they
     * prevent an object from moving during a garbage collection cycle.  This is
     * useful when passing a pointer to object innards out of the runtime while GC
     * may be enabled.
     *
     * NOTE:  PINNING AN OBJECT IS EXPENSIVE AS IT PREVENTS THE GC FROM ACHIEVING
     *        OPTIMAL PACKING OF OBJECTS DURING EPHEMERAL COLLECTIONS.  THIS TYPE
     *        OF HANDLE SHOULD BE USED SPARINGLY!
     */
    HNDTYPE_PINNED       = 3,

    /*
     * VARIABLE HANDLES
     *
     * Variable handles are handles whose type can be changed dynamically.  They
     * are larger than other types of handles, and are scanned a little more often,
     * but are useful when the handle owner needs an efficient way to change the
     * strength of a handle on the fly.
     *
     */
    HNDTYPE_VARIABLE     = 4,

    /*
     * REFCOUNTED HANDLES
     *
     * Refcounted handles are handles that behave as strong handles while the
     * refcount on them is greater than 0 and behave as weak handles otherwise.
     *
     * N.B. These are currently NOT general purpose.
     *      The implementation is tied to COM Interop.
     *
     */
    HNDTYPE_REFCOUNTED   = 5,

    /*
     * DEPENDENT HANDLES
     *
     * Dependent handles are two handles that need to have the same lifetime.  One handle refers to a secondary object
     * that needs to have the same lifetime as the primary object. The secondary object should not cause the primary
     * object to be referenced, but as long as the primary object is alive, so must be the secondary
     *
     * They are currently used for EnC for adding new field members to existing instantiations under EnC modes where
     * the primary object is the original instantiation and the secondary represents the added field.
     *
     * They are also used to implement the managed ConditionalWeakTable class. If you want to use
     * these from managed code, they are exposed to BCL through the managed DependentHandle class.
     *
     *
     */
    HNDTYPE_DEPENDENT    = 6,

    /*
     * PINNED HANDLES for asynchronous operation
     *
     * Pinned handles are strong handles which have the added property that they
     * prevent an object from moving during a garbage collection cycle.  This is
     * useful when passing a pointer to object innards out of the runtime while GC
     * may be enabled.
     *
     * NOTE:  PINNING AN OBJECT IS EXPENSIVE AS IT PREVENTS THE GC FROM ACHIEVING
     *        OPTIMAL PACKING OF OBJECTS DURING EPHEMERAL COLLECTIONS.  THIS TYPE
     *        OF HANDLE SHOULD BE USED SPARINGLY!
     */
    HNDTYPE_ASYNCPINNED  = 7,

    /*
     * SIZEDREF HANDLES
     *
     * SizedRef handles are strong handles. Each handle has a piece of user data associated
     * with it that stores the size of the object this handle refers to. These handles
     * are scanned as strong roots during each GC but only during full GCs would the size
     * be calculated.
     *
     */
    HNDTYPE_SIZEDREF     = 8,

    /*
     * NATIVE WEAK HANDLES
     *
     * Native weak reference handles hold two different types of weak handles to any
     * RCW with an underlying COM object that implements IWeakReferenceSource.  The
     * object reference itself is a short weak handle to the RCW.  In addition an
     * IWeakReference* to the underlying COM object is stored, allowing the handle
     * to create a new RCW if the existing RCW is collected.  This ensures that any
     * code holding onto a native weak reference can always access an RCW to the
     * underlying COM object as long as it has not been released by all of its strong
     * references.
     *
     * NOTE: HNDTYPE_WEAK_NATIVE_COM is no longer used in the VM starting .NET 8
     *       but we are keeping it here for backward compatibility purposes"
     *
     */
    HNDTYPE_WEAK_NATIVE_COM   = 9
} HandleType;

typedef enum
{
    GC_HEAP_INVALID = 0,
    GC_HEAP_WKS     = 1,
    GC_HEAP_SVR     = 2
} GCHeapType;

typedef bool (* walk_fn)(Object*, void*);
typedef bool (* walk_fn2)(Object*, uint8_t**, void*);
typedef void (* gen_walk_fn)(void* context, int generation, uint8_t* range_start, uint8_t* range_end, uint8_t* range_reserved);
typedef void (* record_surv_fn)(uint8_t* begin, uint8_t* end, ptrdiff_t reloc, void* context, bool compacting_p, bool bgc_p);
typedef void (* fq_walk_fn)(bool, void*);
typedef void (* fq_scan_fn)(Object** ppObject, ScanContext *pSC, uint32_t dwFlags);
typedef void (* handle_scan_fn)(Object** pRef, Object* pSec, uint32_t flags, ScanContext* context, bool isDependent);
typedef bool (* async_pin_enum_fn)(Object* object, void* context);

// Implement pure virtual for NativeAOT Unix (for -p:LinkStandardCPlusPlusLibrary=false the default),
// to avoid linker requiring __cxa_pure_virtual.
#if defined(FEATURE_NATIVEAOT) && !defined(TARGET_WINDOWS)
// `while(true);` is to satisfy the missing `return` statement. It will be optimized away by the compiler.
#define PURE_VIRTUAL { assert(!"pure virtual function called"); while(true); }
#else
#define PURE_VIRTUAL = 0;
#endif

class IGCHandleStore {
public:

    virtual void Uproot() PURE_VIRTUAL

    virtual bool ContainsHandle(OBJECTHANDLE handle) PURE_VIRTUAL

    virtual OBJECTHANDLE CreateHandleOfType(Object* object, HandleType type) PURE_VIRTUAL

    virtual OBJECTHANDLE CreateHandleOfType(Object* object, HandleType type, int heapToAffinitizeTo) PURE_VIRTUAL

    virtual OBJECTHANDLE CreateHandleWithExtraInfo(Object* object, HandleType type, void* pExtraInfo) PURE_VIRTUAL

    virtual OBJECTHANDLE CreateDependentHandle(Object* primary, Object* secondary) PURE_VIRTUAL

    virtual ~IGCHandleStore() {};
};

class IGCHandleManager {
public:

    virtual bool Initialize() PURE_VIRTUAL

    virtual void Shutdown() PURE_VIRTUAL

    virtual IGCHandleStore* GetGlobalHandleStore() PURE_VIRTUAL

    virtual IGCHandleStore* CreateHandleStore() PURE_VIRTUAL

    virtual void DestroyHandleStore(IGCHandleStore* store) PURE_VIRTUAL

    virtual OBJECTHANDLE CreateGlobalHandleOfType(Object* object, HandleType type) PURE_VIRTUAL

    virtual OBJECTHANDLE CreateDuplicateHandle(OBJECTHANDLE handle) PURE_VIRTUAL

    virtual void DestroyHandleOfType(OBJECTHANDLE handle, HandleType type) PURE_VIRTUAL

    virtual void DestroyHandleOfUnknownType(OBJECTHANDLE handle) PURE_VIRTUAL

    virtual void SetExtraInfoForHandle(OBJECTHANDLE handle, HandleType type, void* pExtraInfo) PURE_VIRTUAL

    virtual void* GetExtraInfoFromHandle(OBJECTHANDLE handle) PURE_VIRTUAL

    virtual void StoreObjectInHandle(OBJECTHANDLE handle, Object* object) PURE_VIRTUAL

    virtual bool StoreObjectInHandleIfNull(OBJECTHANDLE handle, Object* object) PURE_VIRTUAL

    virtual void SetDependentHandleSecondary(OBJECTHANDLE handle, Object* object) PURE_VIRTUAL

    virtual Object* GetDependentHandleSecondary(OBJECTHANDLE handle) PURE_VIRTUAL

    virtual Object* InterlockedCompareExchangeObjectInHandle(OBJECTHANDLE handle, Object* object, Object* comparandObject) PURE_VIRTUAL

    virtual HandleType HandleFetchType(OBJECTHANDLE handle) PURE_VIRTUAL

    virtual void TraceRefCountedHandles(HANDLESCANPROC callback, uintptr_t param1, uintptr_t param2) PURE_VIRTUAL
};

// Enum representing the type to be passed to GC.CoreCLR.cs used to deduce the type of configuration.
enum class GCConfigurationType
{
    Int64,
    StringUtf8,
    Boolean
};

using ConfigurationValueFunc = void (*)(void* context, void* name, void* publicKey, GCConfigurationType type, int64_t data);

const int REFRESH_MEMORY_SUCCEED = 0;
const int REFRESH_MEMORY_HARD_LIMIT_TOO_LOW = 1;
const int REFRESH_MEMORY_HARD_LIMIT_INVALID = 2;

// IGCHeap is the interface that the VM will use when interacting with the GC.
class IGCHeap {
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
    virtual bool IsValidSegmentSize(size_t size) PURE_VIRTUAL

    // Returns whether or not the given size is a valid gen 0 max size.
    virtual bool IsValidGen0MaxSize(size_t size) PURE_VIRTUAL

    // Gets a valid segment size.
    virtual size_t GetValidSegmentSize(bool large_seg = false) PURE_VIRTUAL

    // Sets the limit for reserved virtual memory.
    virtual void SetReservedVMLimit(size_t vmlimit) PURE_VIRTUAL

    /*
    ===========================================================================
    Concurrent GC routines. These are used in various places in the VM
    to synchronize with the GC, when the VM wants to update something that
    the GC is potentially using, if it's doing a background GC.

    Concrete examples of this are profiling/ETW scenarios.
    ===========================================================================
    */

    // Blocks until any running concurrent GCs complete.
    virtual void WaitUntilConcurrentGCComplete() PURE_VIRTUAL

    // Returns true if a concurrent GC is in progress, false otherwise.
    virtual bool IsConcurrentGCInProgress() PURE_VIRTUAL

    // Temporarily enables concurrent GC, used during profiling.
    virtual void TemporaryEnableConcurrentGC() PURE_VIRTUAL

    // Temporarily disables concurrent GC, used during profiling.
    virtual void TemporaryDisableConcurrentGC() PURE_VIRTUAL

    // Returns whether or not Concurrent GC is enabled.
    virtual bool IsConcurrentGCEnabled() PURE_VIRTUAL

    // Wait for a concurrent GC to complete if one is in progress, with the given timeout.
    virtual HRESULT WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout) PURE_VIRTUAL    // Use in native threads. TRUE if succeed. FALSE if failed or timeout


    /*
    ===========================================================================
    Finalization routines. These are used by the finalizer thread to communicate
    with the GC.
    ===========================================================================
    */

    // Gets the number of finalizable objects.
    virtual size_t GetNumberOfFinalizable() PURE_VIRTUAL

    // Gets the next finalizable object.
    virtual Object* GetNextFinalizable() PURE_VIRTUAL

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
                               int kind) PURE_VIRTUAL;

    // Get the last memory load in percentage observed by the last GC.
    virtual uint32_t GetMemoryLoad() PURE_VIRTUAL

    // Gets the current GC latency mode.
    virtual int GetGcLatencyMode() PURE_VIRTUAL

    // Sets the current GC latency mode. newLatencyMode has already been
    // verified by CoreLib to be valid.
    virtual int SetGcLatencyMode(int newLatencyMode) PURE_VIRTUAL

    // Gets the current LOH compaction mode.
    virtual int GetLOHCompactionMode() PURE_VIRTUAL

    // Sets the current LOH compaction mode. newLOHCompactionMode has
    // already been verified by CoreLib to be valid.
    virtual void SetLOHCompactionMode(int newLOHCompactionMode) PURE_VIRTUAL

    // Registers for a full GC notification, raising a notification if the gen 2 or
    // LOH object heap thresholds are exceeded.
    virtual bool RegisterForFullGCNotification(uint32_t gen2Percentage, uint32_t lohPercentage) PURE_VIRTUAL

    // Cancels a full GC notification that was requested by `RegisterForFullGCNotification`.
    virtual bool CancelFullGCNotification() PURE_VIRTUAL

    // Returns the status of a registered notification for determining whether a blocking
    // Gen 2 collection is about to be initiated, with the given timeout.
    virtual int WaitForFullGCApproach(int millisecondsTimeout) PURE_VIRTUAL

    // Returns the status of a registered notification for determining whether a blocking
    // Gen 2 collection has completed, with the given timeout.
    virtual int WaitForFullGCComplete(int millisecondsTimeout) PURE_VIRTUAL

    // Returns the generation in which obj is found. Also used by the VM
    // in some places, in particular syncblk code.
    // Returns INT32_MAX if obj belongs to a non-GC heap.
    virtual unsigned WhichGeneration(Object* obj) PURE_VIRTUAL

    // Returns the number of GCs that have transpired in the given generation
    // since the beginning of the life of the process. Also used by the VM
    // for debug code.
    virtual int CollectionCount(int generation, int get_bgc_fgc_coutn = 0) PURE_VIRTUAL

    // Begins a no-GC region, returning a code indicating whether entering the no-GC
    // region was successful.
    virtual int StartNoGCRegion(uint64_t totalSize, bool lohSizeKnown, uint64_t lohSize, bool disallowFullBlockingGC) PURE_VIRTUAL

    // Exits a no-GC region.
    virtual int EndNoGCRegion() PURE_VIRTUAL

    // Gets the total number of bytes in use.
    virtual size_t GetTotalBytesInUse() PURE_VIRTUAL

    virtual uint64_t GetTotalAllocatedBytes() PURE_VIRTUAL

    // Forces a garbage collection of the given generation. Also used extensively
    // throughout the VM.
    virtual HRESULT GarbageCollect(int generation = -1, bool low_memory_p = false, int mode = collection_blocking) PURE_VIRTUAL

    // Gets the largest GC generation. Also used extensively throughout the VM.
    virtual unsigned GetMaxGeneration() PURE_VIRTUAL

    // Indicates that an object's finalizer should not be run upon the object's collection.
    virtual void SetFinalizationRun(Object* obj) PURE_VIRTUAL

    // Indicates that an object's finalizer should be run upon the object's collection.
    virtual bool RegisterForFinalization(int gen, Object* obj) PURE_VIRTUAL

    virtual int GetLastGCPercentTimeInGC() PURE_VIRTUAL

    virtual size_t GetLastGCGenerationSize(int gen) PURE_VIRTUAL

    /*
    ===========================================================================
    Miscellaneous routines used by the VM.
    ===========================================================================
    */

    // Initializes the GC heap, returning whether or not the initialization
    // was successful.
    virtual HRESULT Initialize() PURE_VIRTUAL

    // Returns whether nor this GC was promoted by the last GC.
    virtual bool IsPromoted(Object* object) PURE_VIRTUAL

    // Returns true if this pointer points into a GC heap, false otherwise.
    virtual bool IsHeapPointer(void* object, bool small_heap_only = false) PURE_VIRTUAL

    // Return the generation that has been condemned by the current GC.
    virtual unsigned GetCondemnedGeneration() PURE_VIRTUAL

    // Returns whether or not a GC is in progress.
    virtual bool IsGCInProgressHelper(bool bConsiderGCStart = false) PURE_VIRTUAL

    // Returns the number of GCs that have occurred. Mainly used for
    // sanity checks asserting that a GC has not occurred.
    virtual unsigned GetGcCount() PURE_VIRTUAL

    // Gets whether or not the home heap of this alloc context matches the heap
    // associated with this thread.
    virtual bool IsThreadUsingAllocationContextHeap(gc_alloc_context* acontext, int thread_number) PURE_VIRTUAL

    // Returns whether or not this object resides in an ephemeral generation.
    virtual bool IsEphemeral(Object* object) PURE_VIRTUAL

    // Blocks until a GC is complete, returning a code indicating the wait was successful.
    virtual uint32_t WaitUntilGCComplete(bool bConsiderGCStart = false) PURE_VIRTUAL

    // "Fixes" an allocation context by binding its allocation pointer to a
    // location on the heap.
    virtual void FixAllocContext(gc_alloc_context* acontext, void* arg, void* heap) PURE_VIRTUAL

    // Gets the total survived size plus the total allocated bytes on the heap.
    virtual size_t GetCurrentObjSize() PURE_VIRTUAL

    // Sets whether or not a GC is in progress.
    virtual void SetGCInProgress(bool fInProgress) PURE_VIRTUAL

    // Gets whether or not the GC runtime structures are in a valid state for heap traversal.
    virtual bool RuntimeStructuresValid() PURE_VIRTUAL

    // Tells the GC when the VM is suspending threads.
    virtual void SetSuspensionPending(bool fSuspensionPending) PURE_VIRTUAL

    // Tells the GC how many YieldProcessor calls are equal to one scaled yield processor call.
    virtual void SetYieldProcessorScalingFactor(float yieldProcessorScalingFactor) PURE_VIRTUAL

    // Flush the log and close the file if GCLog is turned on.
    virtual void Shutdown() PURE_VIRTUAL

    /*
    ============================================================================
    Add/RemoveMemoryPressure support routines. These are on the interface
    for now, but we should move Add/RemoveMemoryPressure from the VM to the GC.
    When that occurs, these three routines can be removed from the interface.
    ============================================================================
    */

    // Get the timestamp corresponding to the last GC that occurred for the
    // given generation.
    virtual size_t GetLastGCStartTime(int generation) PURE_VIRTUAL

    // Gets the duration of the last GC that occurred for the given generation.
    virtual size_t GetLastGCDuration(int generation) PURE_VIRTUAL

    // Gets a timestamp for the current moment in time.
    virtual size_t GetNow() PURE_VIRTUAL

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
    virtual Object* Alloc(gc_alloc_context* acontext, size_t size, uint32_t flags) PURE_VIRTUAL

    // This is for the allocator to indicate it's done allocating a large object during a
    // background GC as the BGC threads also need to walk UOH.
    virtual void PublishObject(uint8_t* obj) PURE_VIRTUAL

    // Signals the WaitForGCEvent event, indicating that a GC has completed.
    virtual void SetWaitForGCEvent() PURE_VIRTUAL

    // Resets the state of the WaitForGCEvent back to an unsignalled state.
    virtual void ResetWaitForGCEvent() PURE_VIRTUAL

    /*
    ===========================================================================
    Heap verification routines. These are used during heap verification only.
    ===========================================================================
    */
    // Returns whether or not this object is too large for SOH.
    virtual bool IsLargeObject(Object* pObj) PURE_VIRTUAL

    // Walks an object and validates its members.
    virtual void ValidateObjectMember(Object* obj) PURE_VIRTUAL

    // Retrieves the next object after the given object. When the EE
    // is not suspended, the result is not accurate - if the input argument
    // is in Gen0, the function could return zeroed out memory as the next object.
    virtual Object* NextObj(Object* object) PURE_VIRTUAL

    // Given an interior pointer, return a pointer to the object
    // containing that pointer. This is safe to call only when the EE is suspended.
    // When fCollectedGenOnly is true, it only returns the object if it's found in
    // the generation(s) that are being collected.
    virtual Object* GetContainingObject(void* pInteriorPtr, bool fCollectedGenOnly) PURE_VIRTUAL

    /*
    ===========================================================================
    Profiling routines. Used for event tracing and profiling to broadcast
    information regarding the heap.
    ===========================================================================
    */

    // Walks an object, invoking a callback on each member.
    virtual void DiagWalkObject(Object* obj, walk_fn fn, void* context) PURE_VIRTUAL

    // Walks an object, invoking a callback on each member.
    virtual void DiagWalkObject2(Object* obj, walk_fn2 fn, void* context) PURE_VIRTUAL

    // Walk the heap object by object.
    virtual void DiagWalkHeap(walk_fn fn, void* context, int gen_number, bool walk_large_object_heap_p) PURE_VIRTUAL

    // Walks the survivors and get the relocation information if objects have moved.
    // gen_number is used when type == walk_for_uoh, otherwise ignored
    virtual void DiagWalkSurvivorsWithType(void* gc_context, record_surv_fn fn, void* diag_context, walk_surv_type type, int gen_number=-1) PURE_VIRTUAL

    // Walks the finalization queue.
    virtual void DiagWalkFinalizeQueue(void* gc_context, fq_walk_fn fn) PURE_VIRTUAL

    // Scan roots on finalizer queue. This is a generic function.
    virtual void DiagScanFinalizeQueue(fq_scan_fn fn, ScanContext* context) PURE_VIRTUAL

    // Scan handles for profiling or ETW.
    virtual void DiagScanHandles(handle_scan_fn fn, int gen_number, ScanContext* context) PURE_VIRTUAL

    // Scan dependent handles for profiling or ETW.
    virtual void DiagScanDependentHandles(handle_scan_fn fn, int gen_number, ScanContext* context) PURE_VIRTUAL

    // Describes all generations to the profiler, invoking a callback on each generation.
    virtual void DiagDescrGenerations(gen_walk_fn fn, void* context) PURE_VIRTUAL

    // Traces all GC segments and fires ETW events with information on them.
    virtual void DiagTraceGCSegments() PURE_VIRTUAL

    // Get GC settings for tracing purposes. These are settings not obvious from a trace.
    virtual void DiagGetGCSettings(EtwGCSettingsInfo* settings) PURE_VIRTUAL

    /*
    ===========================================================================
    GC Stress routines. Used only when running under GC Stress.
    ===========================================================================
    */

    // Returns TRUE if GC actually happens, otherwise FALSE. The passed alloc context
    // must not be null.
    virtual bool StressHeap(gc_alloc_context* acontext) PURE_VIRTUAL

    /*
    ===========================================================================
    Routines to register read only segments for frozen objects.
    Only valid if FEATURE_BASICFREEZE is defined.
    ===========================================================================
    */

    // Registers a frozen segment with the GC.
    virtual segment_handle RegisterFrozenSegment(segment_info *pseginfo) PURE_VIRTUAL

    // Unregisters a frozen segment.
    virtual void UnregisterFrozenSegment(segment_handle seg) PURE_VIRTUAL

    // Indicates whether an object is in a frozen segment.
    virtual bool IsInFrozenSegment(Object *object) PURE_VIRTUAL

    /*
    ===========================================================================
    Routines for informing the GC about which events are enabled.
    ===========================================================================
    */

    // Enables or disables the given keyword or level on the default event provider.
    virtual void ControlEvents(GCEventKeyword keyword, GCEventLevel level) PURE_VIRTUAL

    // Enables or disables the given keyword or level on the private event provider.
    virtual void ControlPrivateEvents(GCEventKeyword keyword, GCEventLevel level) PURE_VIRTUAL

    // Get the segment/region associated with an address together with its generation for the profiler.
    virtual unsigned int GetGenerationWithRange(Object* object, uint8_t** ppStart, uint8_t** ppAllocated, uint8_t** ppReserved) PURE_VIRTUAL

    IGCHeap() {}

    // The virtual destructors for the IGCHeap class hierarchy is intentionally omitted.
    // This is to ensure we have a stable virtual function table for this interface for
    // version resilience purposes.

    // Get the total paused duration.
    virtual int64_t GetTotalPauseDuration() PURE_VIRTUAL

    // Gets all the names and values of the GC configurations.
    virtual void EnumerateConfigurationValues(void* context, ConfigurationValueFunc configurationValueFunc) PURE_VIRTUAL

    // Updates given frozen segment
    virtual void UpdateFrozenSegment(segment_handle seg, uint8_t* allocated, uint8_t* committed) PURE_VIRTUAL

    // Refresh the memory limit
    virtual int RefreshMemoryLimit() PURE_VIRTUAL
};

#ifdef WRITE_BARRIER_CHECK
void updateGCShadow(Object** ptr, Object* val);
#endif

//constants for the flags parameter to the gc call back

#define GC_CALL_INTERIOR            0x1
#define GC_CALL_PINNED              0x2

// keep in sync with GC_ALLOC_FLAGS in GC.CoreCLR.cs
enum GC_ALLOC_FLAGS
{
    GC_ALLOC_NO_FLAGS           = 0,
    GC_ALLOC_FINALIZE           = 1,
    GC_ALLOC_CONTAINS_REF       = 2,
    GC_ALLOC_ALIGN8_BIAS        = 4,
    GC_ALLOC_ALIGN8             = 8,
    GC_ALLOC_ZEROING_OPTIONAL   = 16,
    GC_ALLOC_LARGE_OBJECT_HEAP  = 32,
    GC_ALLOC_PINNED_OBJECT_HEAP = 64,
    GC_ALLOC_USER_OLD_HEAP      = GC_ALLOC_LARGE_OBJECT_HEAP | GC_ALLOC_PINNED_OBJECT_HEAP,
};

inline GC_ALLOC_FLAGS operator|(GC_ALLOC_FLAGS a, GC_ALLOC_FLAGS b)
{return (GC_ALLOC_FLAGS)((int)a | (int)b);}

inline GC_ALLOC_FLAGS operator&(GC_ALLOC_FLAGS a, GC_ALLOC_FLAGS b)
{return (GC_ALLOC_FLAGS)((int)a & (int)b);}

inline GC_ALLOC_FLAGS operator~(GC_ALLOC_FLAGS a)
{return (GC_ALLOC_FLAGS)(~(int)a);}

inline GC_ALLOC_FLAGS& operator|=(GC_ALLOC_FLAGS& a, GC_ALLOC_FLAGS b)
{return (GC_ALLOC_FLAGS&)((int&)a |= (int)b);}

inline GC_ALLOC_FLAGS& operator&=(GC_ALLOC_FLAGS& a, GC_ALLOC_FLAGS b)
{return (GC_ALLOC_FLAGS&)((int&)a &= (int)b);}

#if defined(USE_CHECKED_OBJECTREFS) && !defined(_NOVM)
#define OBJECTREF_TO_UNCHECKED_OBJECTREF(objref)    (*((_UNCHECKED_OBJECTREF*)&(objref)))
#define UNCHECKED_OBJECTREF_TO_OBJECTREF(obj)       (OBJECTREF(obj))
#else
#define OBJECTREF_TO_UNCHECKED_OBJECTREF(objref)    (objref)
#define UNCHECKED_OBJECTREF_TO_OBJECTREF(obj)       (obj)
#endif

struct ScanContext
{
    Thread* thread_under_crawl;
    int thread_number;
    uintptr_t stack_limit; // Lowest point on the thread stack that the scanning logic is permitted to read
    bool promotion; //TRUE: Promotion, FALSE: Relocation.
    bool concurrent; //TRUE: concurrent scanning
    void* _unused1;
    void* pMD;
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    EtwGCRootKind dwEtwRootKind;
#else
    EtwGCRootKind _unused3;
#endif // GC_PROFILING || FEATURE_EVENT_TRACE

    ScanContext()
    {
        LIMITED_METHOD_CONTRACT;

        thread_under_crawl = 0;
        thread_number = -1;
        stack_limit = 0;
        promotion = false;
        concurrent = false;
        pMD = NULL;
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
        dwEtwRootKind = kEtwGCRootKindOther;
#endif
    }
};

// These types are used as part of the loader protocol between the EE
// and the GC.
struct VersionInfo {
    uint32_t MajorVersion;
    uint32_t MinorVersion;
    uint32_t BuildVersion;
    const char* Name;
};

#ifdef TARGET_X86
#define LOCALGC_CALLCONV __cdecl
#else
#define LOCALGC_CALLCONV
#endif

typedef void (LOCALGC_CALLCONV *GC_VersionInfoFunction)(
    /* Out */ VersionInfo*
);

typedef HRESULT (LOCALGC_CALLCONV *GC_InitializeFunction)(
    /* In  */ IGCToCLR*,
    /* Out */ IGCHeap**,
    /* Out */ IGCHandleManager**,
    /* Out */ GcDacVars*
);

#endif // _GC_INTERFACE_H_
