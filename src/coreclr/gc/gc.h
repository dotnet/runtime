// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*++

Module Name:

    gc.h

--*/

#ifndef __GC_H
#define __GC_H

#include "gcinterface.h"
#include "env/gcenv.os.h"

#include "gchandletableimpl.h"

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

// These values should be in sync with the GC_REASONs (in eventtrace.h) used for ETW.
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
    reason_pm_full_gc = 12, // provisional mode requested to trigger full GC
    reason_lowmemory_host_blocking = 13,
    reason_bgc_tuning_soh = 14,
    reason_bgc_tuning_loh = 15,
    reason_bgc_stepping = 16,
    reason_induced_aggressive = 17,
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
    gc_etw_segment_read_only_heap = 2,
    gc_etw_segment_pinned_object_heap = 3
};

/* forward declarations */
class CObjectHeader;
class Object;

class IGCHeapInternal;

/* misc defines */
#define LARGE_OBJECT_SIZE ((size_t)(85000))

enum gc_generation_num
{
    // small object heap includes generations [0-2], which are "generations" in the general sense.
    soh_gen0 = 0,
    soh_gen1 = 1,
    soh_gen2 = 2,
    max_generation = soh_gen2,

    // large object heap, technically not a generation, but it is convenient to represent it as such
    loh_generation = 3,

    // pinned heap, a separate generation for the same reasons as loh
    poh_generation = 4,

    uoh_start_generation = loh_generation,

    // number of ephemeral generations
    ephemeral_generation_count = max_generation,

    // number of all generations
    total_generation_count = poh_generation + 1
};

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

enum bgc_state
{
    bgc_not_in_process = 0,
    bgc_initialized,
    bgc_reset_ww,
    bgc_mark_handles,
    bgc_mark_stack,
    bgc_revisit_soh,
    bgc_revisit_uoh,
    bgc_overflow_soh,
    bgc_overflow_uoh,
    bgc_final_marking,
    bgc_sweep_soh,
    bgc_sweep_uoh,
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

    // How the alloc_count field is organized -
    // 
    // high 16-bits are for the handle info, out of which
    // high 10 bits store the cpu index. 
    // low 6 bits store the number of handles allocated so far (before the next reset).
    // 
    // low 16-bits are for the actual alloc_count used by balance_heaps
    inline void init_alloc_count()
    {
        alloc_count &= 0xffff0000;
    }

    inline uint16_t get_alloc_count()
    {
        return (uint16_t)alloc_count;
    }

    inline void inc_alloc_count()
    {
        int high_16_bits = (uint32_t)alloc_count >> 16;
        int low_16_bits = alloc_count & 0xffff;
        // When we overflow we don't start from 0 because we would't want to go through the init logic again
        // in balance_heaps.
        low_16_bits = (low_16_bits == 0xffff) ? 16 : (low_16_bits + 1);

        alloc_count = (high_16_bits << 16) | low_16_bits;
    }

    inline void init_handle_info()
    {
        // Start the handle table index based on the AC value to make it random. There may have been handles
        // already allocated before this and that's fine.
        int cpu_index = ((size_t)this >> 4) % g_num_processors;
        int handle_info = cpu_index << 6;
        alloc_count = handle_info << 16;
    }

    inline void set_handle_info (int handle_info)
    {
        int low_16_bits = alloc_count & 0xffff;
        alloc_count = low_16_bits | (handle_info << 16);
    }
#endif // FEATURE_SVR_GC
};

class IGCHeapInternal : public IGCHeap {
public:
    virtual int GetNumberOfHeaps () PURE_VIRTUAL
    virtual int GetHomeHeapNumber () PURE_VIRTUAL
    virtual size_t GetPromotedBytes(int heap_index) PURE_VIRTUAL

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

#ifndef MAX_LONGPATH
#define MAX_LONGPATH 1024
#endif // MAX_LONGPATH

// #define TRACE_GC
// #define SIMPLE_DPRINTF

#ifdef TRACE_GC
#define MIN_CUSTOM_LOG_LEVEL 7
#define SEG_REUSE_LOG_0 (MIN_CUSTOM_LOG_LEVEL)
#define SEG_REUSE_LOG_1 (MIN_CUSTOM_LOG_LEVEL + 1)
#define DT_LOG_0 (MIN_CUSTOM_LOG_LEVEL + 2)
#define BGC_TUNING_LOG (MIN_CUSTOM_LOG_LEVEL + 3)
#define GTC_LOG (MIN_CUSTOM_LOG_LEVEL + 4)
#define GC_TABLE_LOG (MIN_CUSTOM_LOG_LEVEL + 5)
#define JOIN_LOG (MIN_CUSTOM_LOG_LEVEL + 6)
#define SPINLOCK_LOG (MIN_CUSTOM_LOG_LEVEL + 7)
#define SNOOP_LOG (MIN_CUSTOM_LOG_LEVEL + 8)
#define REGIONS_LOG (MIN_CUSTOM_LOG_LEVEL + 9)

// NOTE! This is for HEAP_BALANCE_INSTRUMENTATION
// This particular one is special and needs to be well formatted because we
// do post processing on it with tools\GCLogParser. If you need to add some
// detail to help with investigation that's not 't processed by tooling
// prefix it with TEMP so that line will be written to the results as is in
// the result. I have some already logged with HEAP_BALANCE_TEMP_LOG.
#define HEAP_BALANCE_LOG (MIN_CUSTOM_LOG_LEVEL + 10)
#define HEAP_BALANCE_TEMP_LOG (MIN_CUSTOM_LOG_LEVEL + 11)

#ifdef SIMPLE_DPRINTF

HRESULT initialize_log_file();
void flush_gc_log (bool);
void GCLog (const char *fmt, ... );
#define dprintf(l,x) {if ((l == 1) || (l == GTC_LOG)) {GCLog x;}}
#else //SIMPLE_DPRINTF
#ifdef HOST_64BIT
#define dprintf(l,x) STRESS_LOG_VA(l,x);
//#define dprintf(l,x) {if ((l <= 2) || (l == 6666)) {STRESS_LOG_VA(l,x);}}
#else //HOST_64BIT
#error Logging dprintf to stress log on 32 bits platforms is not supported.
#endif //HOST_64BIT
#endif //SIMPLE_DPRINTF

#else //TRACE_GC
#define dprintf(l,x)
#endif //TRACE_GC

#if defined(TRACE_GC) || defined(GC_CONFIG_DRIVEN)
FILE* CreateLogFile(const GCConfigStringHolder& temp_logfile_name, bool is_config);
#endif //TRACE_GC || GC_CONFIG_DRIVEN

#endif // __GC_H
