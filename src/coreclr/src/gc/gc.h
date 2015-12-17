//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


/*++

Module Name:

    gc.h

--*/

#ifndef __GC_H
#define __GC_H

#ifndef BINDER

#ifdef PROFILING_SUPPORTED
#define GC_PROFILING       //Turn on profiling
#endif // PROFILING_SUPPORTED

#endif

/*
 * Promotion Function Prototypes
 */
typedef void enum_func (Object*);

// callback functions for heap walkers
typedef void object_callback_func(void * pvContext, void * pvDataLoc);

// stub type to abstract a heap segment
struct gc_heap_segment_stub;
typedef gc_heap_segment_stub *segment_handle;

struct segment_info
{
    LPVOID pvMem; // base of the allocation, not the first object (must add ibFirstObject)
    size_t ibFirstObject;   // offset to the base of the first object in the segment
    size_t ibAllocated; // limit of allocated memory in the segment (>= firstobject)
    size_t ibCommit; // limit of committed memory in the segment (>= alllocated)
    size_t ibReserved; // limit of reserved memory in the segment (>= commit)
};

/*!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
/* If you modify failure_get_memory and         */
/* oom_reason be sure to make the corresponding */
/* changes in toolbox\sos\strike\strike.cpp.    */
/*!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
enum failure_get_memory
{
    fgm_no_failure = 0,
    fgm_reserve_segment = 1,
    fgm_commit_segment_beg = 2,
    fgm_commit_eph_segment = 3,
    fgm_grow_table = 4,
    fgm_commit_table = 5
};

struct fgm_history
{
    failure_get_memory fgm;
    size_t size;
    size_t available_pagefile_mb;
    BOOL loh_p;

    void set_fgm (failure_get_memory f, size_t s, BOOL l)
    {
        fgm = f;
        size = s;
        loh_p = l;
    }
};

enum oom_reason
{
    oom_no_failure = 0,
    oom_budget = 1,
    oom_cant_commit = 2,
    oom_cant_reserve = 3,
    oom_loh = 4,
    oom_low_mem = 5,
    oom_unproductive_full_gc = 6
};

struct oom_history
{
    oom_reason reason;
    size_t alloc_size;
    uint8_t* reserved;
    uint8_t* allocated;
    size_t gc_index;
    failure_get_memory fgm;
    size_t size;
    size_t available_pagefile_mb;
    BOOL loh_p;
};

/* forward declerations */
class CObjectHeader;
class Object;

class GCHeap;

/* misc defines */
#define LARGE_OBJECT_SIZE ((size_t)(85000))

GPTR_DECL(GCHeap, g_pGCHeap);

#ifdef GC_CONFIG_DRIVEN
#define MAX_GLOBAL_GC_MECHANISMS_COUNT 6
GARY_DECL(size_t, gc_global_mechanisms, MAX_GLOBAL_GC_MECHANISMS_COUNT);
#endif //GC_CONFIG_DRIVEN

#ifndef DACCESS_COMPILE
extern "C" {
#endif
GPTR_DECL(uint8_t,g_lowest_address);
GPTR_DECL(uint8_t,g_highest_address);
GPTR_DECL(uint32_t,g_card_table);
#ifndef DACCESS_COMPILE
}
#endif

#ifdef DACCESS_COMPILE
class DacHeapWalker;
#endif

#ifdef _DEBUG
#define  _LOGALLOC
#endif

#ifdef WRITE_BARRIER_CHECK
//always defined, but should be 0 in Server GC
extern uint8_t* g_GCShadow;
extern uint8_t* g_GCShadowEnd;
// saves the g_lowest_address in between GCs to verify the consistency of the shadow segment
extern uint8_t* g_shadow_lowest_address;
#endif

#define MP_LOCKS

extern "C" uint8_t* g_ephemeral_low;
extern "C" uint8_t* g_ephemeral_high;

namespace WKS {
    ::GCHeap* CreateGCHeap();
    class GCHeap;
    class gc_heap;
    }

#if defined(FEATURE_SVR_GC)
namespace SVR {
    ::GCHeap* CreateGCHeap();
    class GCHeap;
    class gc_heap;
}
#endif // defined(FEATURE_SVR_GC)

/*
 * Ephemeral Garbage Collected Heap Interface
 */


struct alloc_context 
{
    friend class WKS::gc_heap;
#if defined(FEATURE_SVR_GC)
    friend class SVR::gc_heap;
    friend class SVR::GCHeap;
#endif // defined(FEATURE_SVR_GC)
    friend struct ClassDumpInfo;

    uint8_t*       alloc_ptr;
    uint8_t*       alloc_limit;
    int64_t        alloc_bytes; //Number of bytes allocated on SOH by this context
    int64_t        alloc_bytes_loh; //Number of bytes allocated on LOH by this context
#if defined(FEATURE_SVR_GC)
    SVR::GCHeap*   alloc_heap;
    SVR::GCHeap*   home_heap;
#endif // defined(FEATURE_SVR_GC)
    int            alloc_count;
public:

    void init()
    {
        LIMITED_METHOD_CONTRACT;

        alloc_ptr = 0;
        alloc_limit = 0;
        alloc_bytes = 0;
        alloc_bytes_loh = 0;
#if defined(FEATURE_SVR_GC)
        alloc_heap = 0;
        home_heap = 0;
#endif // defined(FEATURE_SVR_GC)
        alloc_count = 0;
    }
};

struct ScanContext
{
    Thread* thread_under_crawl;
    int thread_number;
    BOOL promotion; //TRUE: Promotion, FALSE: Relocation.
    BOOL concurrent; //TRUE: concurrent scanning 
#if CHECK_APP_DOMAIN_LEAKS || defined (FEATURE_APPDOMAIN_RESOURCE_MONITORING) || defined (DACCESS_COMPILE)
    AppDomain *pCurrentDomain;
#endif //CHECK_APP_DOMAIN_LEAKS || FEATURE_APPDOMAIN_RESOURCE_MONITORING || DACCESS_COMPILE

#ifndef FEATURE_REDHAWK
#if defined(GC_PROFILING) || defined (DACCESS_COMPILE)
    MethodDesc *pMD;
#endif //GC_PROFILING || DACCESS_COMPILE
#endif // FEATURE_REDHAWK
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    EtwGCRootKind dwEtwRootKind;
#endif // GC_PROFILING || FEATURE_EVENT_TRACE
    
    ScanContext()
    {
        LIMITED_METHOD_CONTRACT;

        thread_under_crawl = 0;
        thread_number = -1;
        promotion = FALSE;
        concurrent = FALSE;
#ifdef GC_PROFILING
        pMD = NULL;
#endif //GC_PROFILING
#ifdef FEATURE_EVENT_TRACE
        dwEtwRootKind = kEtwGCRootKindOther;
#endif // FEATURE_EVENT_TRACE
    }
};

typedef BOOL (* walk_fn)(Object*, void*);
typedef void (* gen_walk_fn)(void *context, int generation, uint8_t *range_start, uint8_t * range_end, uint8_t *range_reserved);

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
struct ProfilingScanContext : ScanContext
{
    BOOL fProfilerPinned;
    LPVOID pvEtwContext;
    void *pHeapId;
    
    ProfilingScanContext(BOOL fProfilerPinnedParam) : ScanContext()
    {
        LIMITED_METHOD_CONTRACT;

        pHeapId = NULL;
        fProfilerPinned = fProfilerPinnedParam;
        pvEtwContext = NULL;
#ifdef FEATURE_CONSERVATIVE_GC
        // To not confuse GCScan::GcScanRoots
        promotion = g_pConfig->GetGCConservative();
#endif
    }
};
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

#ifdef STRESS_HEAP
#define IN_STRESS_HEAP(x) x
#define STRESS_HEAP_ARG(x) ,x
#else // STRESS_HEAP
#define IN_STRESS_HEAP(x)
#define STRESS_HEAP_ARG(x)
#endif // STRESS_HEAP


//dynamic data interface
struct gc_counters
{
    size_t current_size;
    size_t promoted_size;
    size_t collection_count;
};

// !!!!!!!!!!!!!!!!!!!!!!!
// make sure you change the def in bcl\system\gc.cs 
// if you change this!
enum collection_mode
{
    collection_non_blocking = 0x00000001,
    collection_blocking = 0x00000002,
    collection_optimized = 0x00000004,
    collection_compacting = 0x00000008
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

enum bgc_state
{
    bgc_not_in_process = 0,
    bgc_initialized,
    bgc_reset_ww,
    bgc_mark_handles,
    bgc_mark_stack,
    bgc_revisit_soh,
    bgc_revisit_loh,
    bgc_overflow_soh,
    bgc_overflow_loh,
    bgc_final_marking,
    bgc_sweep_soh,
    bgc_sweep_loh,
    bgc_plan_phase
};

enum changed_seg_state
{
    seg_deleted,
    seg_added
};

void record_changed_seg (uint8_t* start, uint8_t* end,
                         size_t current_gc_index,
                         bgc_state current_bgc_state,
                         changed_seg_state changed_state);

#ifdef GC_CONFIG_DRIVEN
void record_global_mechanism (int mech_index);
#endif //GC_CONFIG_DRIVEN

//constants for the flags parameter to the gc call back

#define GC_CALL_INTERIOR            0x1
#define GC_CALL_PINNED              0x2
#define GC_CALL_CHECK_APP_DOMAIN    0x4

//flags for GCHeap::Alloc(...)
#define GC_ALLOC_FINALIZE 0x1
#define GC_ALLOC_CONTAINS_REF 0x2
#define GC_ALLOC_ALIGN8_BIAS 0x4
#define GC_ALLOC_ALIGN8 0x8

class GCHeap {
    friend struct ::_DacGlobals;
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif
    
public:

    virtual ~GCHeap() {}

    static GCHeap *GetGCHeap()
    {
#ifdef CLR_STANDALONE_BINDER
        return NULL;
#else
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(g_pGCHeap != NULL);
        return g_pGCHeap;
#endif
    }
    
#ifndef CLR_STANDALONE_BINDER

#ifndef DACCESS_COMPILE
    static BOOL IsGCInProgress(BOOL bConsiderGCStart = FALSE)
    {
        WRAPPER_NO_CONTRACT;

        return (IsGCHeapInitialized() ? GetGCHeap()->IsGCInProgressHelper(bConsiderGCStart) : false);
    }   
#endif
    
    static BOOL IsGCHeapInitialized()
    {
        LIMITED_METHOD_CONTRACT;

        return (g_pGCHeap != NULL);
    }

    static void WaitForGCCompletion(BOOL bConsiderGCStart = FALSE)
    {
        WRAPPER_NO_CONTRACT;

        if (IsGCHeapInitialized())
            GetGCHeap()->WaitUntilGCComplete(bConsiderGCStart);
    }   

    // The runtime needs to know whether we're using workstation or server GC 
    // long before the GCHeap is created.  So IsServerHeap cannot be a virtual 
    // method on GCHeap.  Instead we make it a static method and initialize 
    // gcHeapType before any of the calls to IsServerHeap.  Note that this also 
    // has the advantage of getting the answer without an indirection
    // (virtual call), which is important for perf critical codepaths.

    #ifndef DACCESS_COMPILE
    static void InitializeHeapType(bool bServerHeap)
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_SVR_GC
        gcHeapType = bServerHeap ? GC_HEAP_SVR : GC_HEAP_WKS;
#ifdef WRITE_BARRIER_CHECK
        if (gcHeapType == GC_HEAP_SVR)
        {
            g_GCShadow = 0;
            g_GCShadowEnd = 0;
        }
#endif
#else // FEATURE_SVR_GC
        UNREFERENCED_PARAMETER(bServerHeap);
        CONSISTENCY_CHECK(bServerHeap == false);
#endif // FEATURE_SVR_GC
    }
    #endif
    
    static BOOL IsValidSegmentSize(size_t cbSize)
    {
        //Must be aligned on a Mb and greater than 4Mb
        return (((cbSize & (1024*1024-1)) ==0) && (cbSize >> 22));
    }

    static BOOL IsValidGen0MaxSize(size_t cbSize)
    {
        return (cbSize >= 64*1024);
    }

    inline static bool IsServerHeap()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_SVR_GC
        _ASSERTE(gcHeapType != GC_HEAP_INVALID);
        return (gcHeapType == GC_HEAP_SVR);
#else // FEATURE_SVR_GC
        return false;
#endif // FEATURE_SVR_GC
    }

    inline static bool UseAllocationContexts()
    {
        WRAPPER_NO_CONTRACT;
#ifdef FEATURE_REDHAWK
        // SIMPLIFY:  only use allocation contexts
        return true;
#else
#if defined(_TARGET_ARM_) || defined(FEATURE_PAL)
        return true;
#else
        return ((IsServerHeap() ? true : (g_SystemInfo.dwNumberOfProcessors >= 2)));
#endif
#endif 
    }

   inline static bool MarkShouldCompeteForStatics()
    {
        WRAPPER_NO_CONTRACT;

        return IsServerHeap() && g_SystemInfo.dwNumberOfProcessors >= 2;
    }
    
#ifndef DACCESS_COMPILE
    static GCHeap * CreateGCHeap()
    {
        WRAPPER_NO_CONTRACT;

        GCHeap * pGCHeap;

#if defined(FEATURE_SVR_GC)
        pGCHeap = (IsServerHeap() ? SVR::CreateGCHeap() : WKS::CreateGCHeap());
#else
        pGCHeap = WKS::CreateGCHeap();
#endif // defined(FEATURE_SVR_GC)

        g_pGCHeap = pGCHeap;
        return pGCHeap;
    }
#endif // DACCESS_COMPILE

#endif // !CLR_STANDALONE_BINDER

private:
    typedef enum
    {
        GC_HEAP_INVALID = 0,
        GC_HEAP_WKS     = 1,
        GC_HEAP_SVR     = 2
    } GC_HEAP_TYPE;
    
#ifdef FEATURE_SVR_GC
    SVAL_DECL(uint32_t,gcHeapType);
#endif // FEATURE_SVR_GC

public:
        // TODO Synchronization, should be moved out
    virtual BOOL    IsGCInProgressHelper (BOOL bConsiderGCStart = FALSE) = 0;
    virtual uint32_t    WaitUntilGCComplete (BOOL bConsiderGCStart = FALSE) = 0;
    virtual void SetGCInProgress(BOOL fInProgress) = 0;
    virtual CLREventStatic * GetWaitForGCEvent() = 0;

    virtual void    SetFinalizationRun (Object* obj) = 0;
    virtual Object* GetNextFinalizable() = 0;
    virtual size_t GetNumberOfFinalizable() = 0;

    virtual void SetFinalizeQueueForShutdown(BOOL fHasLock) = 0;
    virtual BOOL FinalizeAppDomain(AppDomain *pDomain, BOOL fRunFinalizers) = 0;
    virtual BOOL ShouldRestartFinalizerWatchDog() = 0;

    //wait for concurrent GC to finish
    virtual void WaitUntilConcurrentGCComplete () = 0;                                  // Use in managed threads
#ifndef DACCESS_COMPILE    
    virtual HRESULT WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout) = 0;    // Use in native threads. TRUE if succeed. FALSE if failed or timeout
#endif    
    virtual BOOL IsConcurrentGCInProgress() = 0;

    // Enable/disable concurrent GC    
    virtual void TemporaryEnableConcurrentGC() = 0;
    virtual void TemporaryDisableConcurrentGC() = 0;
    virtual BOOL IsConcurrentGCEnabled() = 0;

    virtual void FixAllocContext (alloc_context* acontext, BOOL lockp, void* arg, void *heap) = 0;
    virtual Object* Alloc (alloc_context* acontext, size_t size, uint32_t flags) = 0;

    // This is safe to call only when EE is suspended.
    virtual Object* GetContainingObject(void *pInteriorPtr) = 0;

        // TODO Should be folded into constructor
    virtual HRESULT Initialize () = 0;

    virtual HRESULT GarbageCollect (int generation = -1, BOOL low_memory_p=FALSE, int mode = collection_blocking) = 0;
    virtual Object*  Alloc (size_t size, uint32_t flags) = 0;
#ifdef FEATURE_64BIT_ALIGNMENT
    virtual Object*  AllocAlign8 (size_t size, uint32_t flags) = 0;
    virtual Object*  AllocAlign8 (alloc_context* acontext, size_t size, uint32_t flags) = 0;
private:
    virtual Object*  AllocAlign8Common (void* hp, alloc_context* acontext, size_t size, uint32_t flags) = 0;
public:
#endif // FEATURE_64BIT_ALIGNMENT
    virtual Object*  AllocLHeap (size_t size, uint32_t flags) = 0;
    virtual void     SetReservedVMLimit (size_t vmlimit) = 0;
    virtual void SetCardsAfterBulkCopy( Object**, size_t ) = 0;
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    virtual void WalkObject (Object* obj, walk_fn fn, void* context) = 0;
#endif //defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

    virtual bool IsThreadUsingAllocationContextHeap(alloc_context* acontext, int thread_number) = 0;
    virtual int GetNumberOfHeaps () = 0; 
    virtual int GetHomeHeapNumber () = 0;
    
    virtual int CollectionCount (int generation, int get_bgc_fgc_count = 0) = 0;

        // Finalizer queue stuff (should stay)
    virtual bool    RegisterForFinalization (int gen, Object* obj) = 0;

        // General queries to the GC
    virtual BOOL    IsPromoted (Object *object) = 0;
    virtual unsigned WhichGeneration (Object* object) = 0;
    virtual BOOL    IsEphemeral (Object* object) = 0;
    virtual BOOL    IsHeapPointer (void* object, BOOL small_heap_only = FALSE) = 0;

    virtual unsigned GetCondemnedGeneration() = 0;
    virtual int GetGcLatencyMode() = 0;
    virtual int SetGcLatencyMode(int newLatencyMode) = 0;

    virtual int GetLOHCompactionMode() = 0;
    virtual void SetLOHCompactionMode(int newLOHCompactionyMode) = 0;

    virtual BOOL RegisterForFullGCNotification(uint32_t gen2Percentage,
                                               uint32_t lohPercentage) = 0;
    virtual BOOL CancelFullGCNotification() = 0;
    virtual int WaitForFullGCApproach(int millisecondsTimeout) = 0;
    virtual int WaitForFullGCComplete(int millisecondsTimeout) = 0;

    virtual int StartNoGCRegion(uint64_t totalSize, BOOL lohSizeKnown, uint64_t lohSize, BOOL disallowFullBlockingGC) = 0;
    virtual int EndNoGCRegion() = 0;

    virtual BOOL IsObjectInFixedHeap(Object *pObj) = 0;
    virtual size_t  GetTotalBytesInUse () = 0;
    virtual size_t  GetCurrentObjSize() = 0;
    virtual size_t  GetLastGCStartTime(int generation) = 0;
    virtual size_t  GetLastGCDuration(int generation) = 0;
    virtual size_t  GetNow() = 0;
    virtual unsigned GetGcCount() = 0;
    virtual void TraceGCSegments() = 0;

    virtual void PublishObject(uint8_t* obj) = 0;

    // static if since restricting for all heaps is fine
    virtual size_t GetValidSegmentSize(BOOL large_seg = FALSE) = 0;

    static BOOL IsLargeObject(MethodTable *mt) {
        WRAPPER_NO_CONTRACT;

        return mt->GetBaseSize() >= LARGE_OBJECT_SIZE;
    }

    static unsigned GetMaxGeneration() {
        LIMITED_METHOD_DAC_CONTRACT;  
        return max_generation;
    }

    virtual size_t GetPromotedBytes(int heap_index) = 0;

private:
    enum {
        max_generation  = 2,
    };
    
public:

#ifdef FEATURE_BASICFREEZE
    // frozen segment management functions
    virtual segment_handle RegisterFrozenSegment(segment_info *pseginfo) = 0;
    virtual void UnregisterFrozenSegment(segment_handle seg) = 0;
#endif //FEATURE_BASICFREEZE

        // debug support 
#ifndef FEATURE_REDHAWK // Redhawk forces relocation a different way
#ifdef STRESS_HEAP
    //return TRUE if GC actually happens, otherwise FALSE
    virtual BOOL    StressHeap(alloc_context * acontext = 0) = 0;
#endif
#endif // FEATURE_REDHAWK
#ifdef VERIFY_HEAP
    virtual void    ValidateObjectMember (Object *obj) = 0;
#endif

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    virtual void DescrGenerationsToProfiler (gen_walk_fn fn, void *context) = 0;
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

protected: 
#ifdef VERIFY_HEAP
public:
    // Return NULL if can't find next object. When EE is not suspended,
    // the result is not accurate: if the input arg is in gen0, the function could 
    // return zeroed out memory as next object
    virtual Object * NextObj (Object * object) = 0;
#ifdef FEATURE_BASICFREEZE
    // Return TRUE if object lives in frozen segment
    virtual BOOL IsInFrozenSegment (Object * object) = 0;
#endif //FEATURE_BASICFREEZE
#endif //VERIFY_HEAP    
};

extern VOLATILE(int32_t) m_GCLock;

// Go through and touch (read) each page straddled by a memory block.
void TouchPages(LPVOID pStart, uint32_t cb);

// For low memory notification from host
extern int32_t g_bLowMemoryFromHost;

#ifdef WRITE_BARRIER_CHECK
void updateGCShadow(Object** ptr, Object* val);
#endif

// the method table for the WeakReference class
extern MethodTable  *pWeakReferenceMT;
// The canonical method table for WeakReference<T>
extern MethodTable  *pWeakReferenceOfTCanonMT;
extern void FinalizeWeakReference(Object * obj);

#endif // __GC_H
