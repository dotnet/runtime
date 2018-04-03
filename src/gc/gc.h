// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*++

Module Name:

    gc.h

--*/

#ifndef __GC_H
#define __GC_H

#include "gcinterface.h"
#include "env/gcenv.os.h"

#ifdef BUILD_AS_STANDALONE
#include "gcenv.ee.standalone.inl"

// GCStress does not currently work with Standalone GC
#ifdef STRESS_HEAP
 #undef STRESS_HEAP
#endif // STRESS_HEAP
#else
#include "env/gcenv.ee.h"
#endif // BUILD_AS_STANDALONE
#include "gcconfig.h"

/*
 * Promotion Function Prototypes
 */
typedef void enum_func (Object*);

// callback functions for heap walkers
typedef void object_callback_func(void * pvContext, void * pvDataLoc);

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

// TODO : it would be easier to make this an ORed value
enum gc_reason
{
    reason_alloc_soh = 0,
    reason_induced = 1,
    reason_lowmemory = 2,
    reason_empty = 3,
    reason_alloc_loh = 4,
    reason_oos_soh = 5,
    reason_oos_loh = 6,
    reason_induced_noforce = 7, // it's an induced GC and doesn't have to be blocking.
    reason_gcstress = 8,        // this turns into reason_induced & gc_mechanisms.stress_induced = true
    reason_lowmemory_blocking = 9,
    reason_induced_compacting = 10,
    reason_lowmemory_host = 11,
    reason_max
};

// Types of GCs, emitted by the GCStart ETW event.
enum gc_etw_type
{
   gc_etw_type_ngc = 0,
   gc_etw_type_bgc = 1,
   gc_etw_type_fgc = 2
};

// Types of segments, emitted by the GCCreateSegment ETW event.
enum gc_etw_segment_type
{
    gc_etw_segment_small_object_heap = 0,
    gc_etw_segment_large_object_heap = 1,
    gc_etw_segment_read_only_heap = 2
};

// Types of allocations, emitted by the GCAllocationTick ETW event.
enum gc_etw_alloc_kind
{
    gc_etw_alloc_soh = 0,
    gc_etw_alloc_loh = 1
};

/* forward declerations */
class CObjectHeader;
class Object;

class IGCHeapInternal;

/* misc defines */
#define LARGE_OBJECT_SIZE ((size_t)(85000))
#define max_generation 2

#ifdef GC_CONFIG_DRIVEN
#define MAX_GLOBAL_GC_MECHANISMS_COUNT 6
extern size_t gc_global_mechanisms[MAX_GLOBAL_GC_MECHANISMS_COUNT];
#endif //GC_CONFIG_DRIVEN

#ifdef DACCESS_COMPILE
class DacHeapWalker;
#endif

#ifdef _DEBUG
#define  _LOGALLOC
#endif

#define MP_LOCKS

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
extern "C" uint32_t* g_gc_card_bundle_table;
#endif

#if defined(ENABLE_PERF_COUNTERS) || defined(FEATURE_EVENT_TRACE)
// Note this is not updated in a thread safe way so the value may not be accurate. We get
// it accurately in full GCs if the handle count is requested.
extern DWORD g_dwHandles;
#endif // ENABLE_PERF_COUNTERS || FEATURE_EVENT_TRACE

extern "C" uint32_t* g_gc_card_table;
extern "C" uint8_t* g_gc_lowest_address;
extern "C" uint8_t* g_gc_highest_address;
extern "C" GCHeapType g_gc_heap_type;
extern "C" uint32_t g_max_generation;
extern "C" MethodTable* g_gc_pFreeObjectMethodTable;
extern "C" uint32_t g_num_processors;

extern VOLATILE(int32_t) g_fSuspensionPending;

extern uint32_t g_yieldProcessorScalingFactor;

::IGCHandleManager*  CreateGCHandleManager();

namespace WKS {
    ::IGCHeapInternal* CreateGCHeap();
    class GCHeap;
    class gc_heap;
    }

#if defined(FEATURE_SVR_GC)
namespace SVR {
    ::IGCHeapInternal* CreateGCHeap();
    class GCHeap;
    class gc_heap;
}
#endif // defined(FEATURE_SVR_GC)

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

struct alloc_context : gc_alloc_context 
{
#ifdef FEATURE_SVR_GC
    inline SVR::GCHeap* get_alloc_heap()
    {
        return static_cast<SVR::GCHeap*>(gc_reserved_1);
    }

    inline void set_alloc_heap(SVR::GCHeap* heap)
    {
        gc_reserved_1 = heap;
    }

    inline SVR::GCHeap* get_home_heap()
    {
        return static_cast<SVR::GCHeap*>(gc_reserved_2);
    }

    inline void set_home_heap(SVR::GCHeap* heap)
    {
        gc_reserved_2 = heap;
    }
#endif // FEATURE_SVR_GC
};

class IGCHeapInternal : public IGCHeap {
public:

    virtual ~IGCHeapInternal() {}

private:
    virtual Object* AllocAlign8Common (void* hp, alloc_context* acontext, size_t size, uint32_t flags) = 0;
public:
    virtual int GetNumberOfHeaps () = 0; 
    virtual int GetHomeHeapNumber () = 0;
    virtual size_t GetPromotedBytes(int heap_index) = 0;

    unsigned GetMaxGeneration()
    {
        return max_generation;
    }

    bool IsValidSegmentSize(size_t cbSize)
    {
        //Must be aligned on a Mb and greater than 4Mb
        return (((cbSize & (1024*1024-1)) ==0) && (cbSize >> 22));
    }

    bool IsValidGen0MaxSize(size_t cbSize)
    {
        return (cbSize >= 64*1024);
    }

    BOOL IsLargeObject(MethodTable *mt)
    {
        return mt->GetBaseSize() >= LARGE_OBJECT_SIZE;
    }

protected: 
public:
#if defined(FEATURE_BASICFREEZE) && defined(VERIFY_HEAP)
    // Return TRUE if object lives in frozen segment
    virtual BOOL IsInFrozenSegment (Object * object) = 0;
#endif // defined(FEATURE_BASICFREEZE) && defined(VERIFY_HEAP)
};

// Go through and touch (read) each page straddled by a memory block.
void TouchPages(void * pStart, size_t cb);

#ifdef WRITE_BARRIER_CHECK
void updateGCShadow(Object** ptr, Object* val);
#endif

#ifndef DACCESS_COMPILE
// The single GC heap instance, shared with the VM.
extern IGCHeapInternal* g_theGCHeap;

// The single GC handle manager instance, shared with the VM.
extern IGCHandleManager* g_theGCHandleManager;
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
inline bool IsGCInProgress(bool bConsiderGCStart = false)
{
    return g_theGCHeap != nullptr ? g_theGCHeap->IsGCInProgressHelper(bConsiderGCStart) : false;
}
#endif // DACCESS_COMPILE

inline bool IsServerHeap()
{
#ifdef FEATURE_SVR_GC
    assert(g_gc_heap_type != GC_HEAP_INVALID);
    return g_gc_heap_type == GC_HEAP_SVR;
#else // FEATURE_SVR_GC
    return false;
#endif // FEATURE_SVR_GC
}

#endif // __GC_H
