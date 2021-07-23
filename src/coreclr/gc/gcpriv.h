// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _DEBUG
#ifdef _MSC_VER
// optimize for speed
#pragma optimize( "t", on )
#endif
#endif

#ifdef __GNUC__
#define inline __attribute__((always_inline)) inline
#else
#define inline __forceinline
#endif // __GNUC__

#include "gc.h"
#include "gcrecord.h"

#ifdef _MSC_VER
#pragma warning(disable:4293)
#pragma warning(disable:4477)
#endif //_MSC_VER

inline void FATAL_GC_ERROR()
{
#ifndef DACCESS_COMPILE
    GCToOSInterface::DebugBreak();
#endif // DACCESS_COMPILE
    _ASSERTE(!"Fatal Error in GC.");
    GCToEEInterface::HandleFatalError((unsigned int)COR_E_EXECUTIONENGINE);
}

#ifdef MULTIPLE_HEAPS
// This turns on instrumentation that collects info for heap balancing.
// Define it and make sure you have HEAP_BALANCE_LOG/HEAP_BALANCE_TEMP_LOG
// level logging enabled *only*.
//#define HEAP_BALANCE_INSTRUMENTATION
#endif //MULTIPLE_HEAPS

#ifdef _MSC_VER
#pragma inline_depth(20)
#endif

/* the following section defines the optional features */

// Regions invariants -
// 
// + each generation consists of 1+ regions.
// + a region is in a contiguous address range; different regions could have
//   gaps inbetween.
// + a region cannot contain more than one generation.
// 
// This means any empty regions can be freely used for any generation. For 
// Server GC we will balance regions between heaps.
// For now enable regions by default for only StandAlone GC builds
#if defined (HOST_64BIT) && defined (BUILD_AS_STANDALONE)
#define USE_REGIONS
#endif //HOST_64BIT && BUILD_AS_STANDALONE

#ifdef USE_REGIONS
// Currently this -
// + creates some pins on our own
// + creates some ro segs
// We can add more mechanisms here.
//#define STRESS_REGIONS
#endif //USE_REGIONS

// FEATURE_STRUCTALIGN was added by Midori. In CLR we are not interested
// in supporting custom alignments on LOH. Currently FEATURE_LOH_COMPACTION
// and FEATURE_STRUCTALIGN are mutually exclusive. It shouldn't be much
// work to make FEATURE_STRUCTALIGN not apply to LOH so they can be both
// turned on.
#define FEATURE_LOH_COMPACTION

#ifdef FEATURE_64BIT_ALIGNMENT
// We need the following feature as part of keeping 64-bit types aligned in the GC heap.
#define RESPECT_LARGE_ALIGNMENT //Preserve double alignment of objects during relocation
#endif //FEATURE_64BIT_ALIGNMENT

#define SHORT_PLUGS //used to keep ephemeral plugs short so they fit better into the oldest generation free items

#ifdef SHORT_PLUGS
#define DESIRED_PLUG_LENGTH (1000)
#endif //SHORT_PLUGS

#define FEATURE_PREMORTEM_FINALIZATION
#define GC_HISTORY

// We need the lower 3 bits in the MT to do our bookkeeping so doubly linked free list is only for 64-bit
#ifdef HOST_64BIT
#define DOUBLY_LINKED_FL
#endif //HOST_64BIT

#ifndef FEATURE_REDHAWK
#define HEAP_ANALYZE
#define COLLECTIBLE_CLASS
#endif // !FEATURE_REDHAWK

#ifdef HEAP_ANALYZE
#define initial_internal_roots        (1024*16)
#endif // HEAP_ANALYZE

#define BACKGROUND_GC   //concurrent background GC (requires WRITE_WATCH)

#ifdef SERVER_GC
#define MH_SC_MARK //scalable marking
//#define SNOOP_STATS //diagnostic
#endif //SERVER_GC

//This is used to mark some type volatile only when the scalable marking is used.
#if defined (SERVER_GC) && defined (MH_SC_MARK)
#define SERVER_SC_MARK_VOLATILE(x) VOLATILE(x)
#else //SERVER_GC&&MH_SC_MARK
#define SERVER_SC_MARK_VOLATILE(x) x
#endif //SERVER_GC&&MH_SC_MARK

//#define MULTIPLE_HEAPS         //Allow multiple heaps for servers

#define CARD_BUNDLE         //enable card bundle feature.(requires WRITE_WATCH)

#define ALLOW_REFERENCES_IN_POH  //Allow POH objects to contain references.

#ifdef BACKGROUND_GC
#define BGC_SERVO_TUNING
#endif //BACKGROUND_GC

#if defined(BACKGROUND_GC) || defined(CARD_BUNDLE) || defined(FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP)
#define WRITE_WATCH     //Write Watch feature
#endif //BACKGROUND_GC || CARD_BUNDLE

#ifdef WRITE_WATCH
#define array_size 100
#endif //WRITE_WATCH

#define FFIND_DECAY  7      //Number of GC for which fast find will be active

#ifndef MAX_LONGPATH
#define MAX_LONGPATH 1024
#endif // MAX_LONGPATH

//#define TRACE_GC
//#define SIMPLE_DPRINTF

//#define JOIN_STATS         //amount of time spent in the join

//#define SYNCHRONIZATION_STATS
//#define SEG_REUSE_STATS

#ifdef SYNCHRONIZATION_STATS
#define BEGIN_TIMING(x) \
    int64_t x##_start; \
    x##_start = GCToOSInterface::QueryPerformanceCounter()

#define END_TIMING(x) \
    int64_t x##_end; \
    x##_end = GCToOSInterface::QueryPerformanceCounter(); \
    x += x##_end - x##_start

#else //SYNCHRONIZATION_STATS
#define BEGIN_TIMING(x)
#define END_TIMING(x)
#endif //SYNCHRONIZATION_STATS

#ifdef GC_CONFIG_DRIVEN
void GCLogConfig (const char *fmt, ... );
#define cprintf(x) {GCLogConfig x;}
#endif //GC_CONFIG_DRIVEN

// For the bestfit algorithm when we relocate ephemeral generations into an
// existing gen2 segment.
// We recorded sizes from 2^6, 2^7, 2^8...up to 2^30 (1GB). So that's 25 sizes total.
#define MIN_INDEX_POWER2 6

#ifdef SERVER_GC

#ifdef HOST_64BIT
#define MAX_INDEX_POWER2 30
#else
#define MAX_INDEX_POWER2 26
#endif  // HOST_64BIT

#else //SERVER_GC

#ifdef HOST_64BIT
#define MAX_INDEX_POWER2 28
#else
#define MAX_INDEX_POWER2 24
#endif  // HOST_64BIT

#endif //SERVER_GC

#define MAX_NUM_BUCKETS (MAX_INDEX_POWER2 - MIN_INDEX_POWER2 + 1)

#define MAX_NUM_FREE_SPACES 200
#define MIN_NUM_FREE_SPACES 5

#ifdef memcpy
#undef memcpy
#endif //memcpy

#ifdef FEATURE_STRUCTALIGN
#define REQD_ALIGN_DCL ,int requiredAlignment
#define REQD_ALIGN_ARG ,requiredAlignment
#define REQD_ALIGN_AND_OFFSET_DCL ,int requiredAlignment,size_t alignmentOffset
#define REQD_ALIGN_AND_OFFSET_DEFAULT_DCL ,int requiredAlignment=DATA_ALIGNMENT,size_t alignmentOffset=0
#define REQD_ALIGN_AND_OFFSET_ARG ,requiredAlignment,alignmentOffset
#else // FEATURE_STRUCTALIGN
#define REQD_ALIGN_DCL
#define REQD_ALIGN_ARG
#define REQD_ALIGN_AND_OFFSET_DCL
#define REQD_ALIGN_AND_OFFSET_DEFAULT_DCL
#define REQD_ALIGN_AND_OFFSET_ARG
#endif // FEATURE_STRUCTALIGN

#ifdef MULTIPLE_HEAPS
#define THREAD_NUMBER_DCL ,int thread
#define THREAD_NUMBER_ARG ,thread
#define THREAD_NUMBER_FROM_CONTEXT int thread = sc->thread_number;
#define THREAD_FROM_HEAP  int thread = heap_number;
#define HEAP_FROM_THREAD  gc_heap* hpt = gc_heap::g_heaps[thread];
#else
#define THREAD_NUMBER_DCL
#define THREAD_NUMBER_ARG
#define THREAD_NUMBER_FROM_CONTEXT
#define THREAD_FROM_HEAP
#define HEAP_FROM_THREAD  gc_heap* hpt = 0;
#endif //MULTIPLE_HEAPS

//These constants are ordered
const int policy_sweep = 0;
const int policy_compact = 1;
const int policy_expand  = 2;

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

#ifndef DACCESS_COMPILE

#ifdef SIMPLE_DPRINTF

void GCLog (const char *fmt, ... );
#define dprintf(l,x) {if ((l == 1) || (l == GTC_LOG)) {GCLog x;}}
#else //SIMPLE_DPRINTF
// Nobody used the logging mechanism that used to be here. If we find ourselves
// wanting to inspect GC logs on unmodified builds, we can use this define here
// to do so.
//#define dprintf(l, x)
#define dprintf(l,x) STRESS_LOG_VA(l,x);

#endif //SIMPLE_DPRINTF

#else //DACCESS_COMPILE
#define dprintf(l,x)
#endif //DACCESS_COMPILE
#else //TRACE_GC
#define dprintf(l,x)
#endif //TRACE_GC

#if !defined(FEATURE_REDHAWK) && !defined(BUILD_AS_STANDALONE)
#undef  assert
#define assert _ASSERTE
#undef  ASSERT
#define ASSERT _ASSERTE
#endif // FEATURE_REDHAWK

struct GCDebugSpinLock {
    VOLATILE(int32_t) lock;                   // -1 if free, 0 if held
#ifdef _DEBUG
    VOLATILE(Thread *) holding_thread;     // -1 if no thread holds the lock.
    VOLATILE(BOOL) released_by_gc_p;       // a GC thread released the lock.
#endif
#if defined (SYNCHRONIZATION_STATS)
    // number of times we went into SwitchToThread in enter_spin_lock.
    unsigned int num_switch_thread;
    // number of times we went into WaitLonger.
    unsigned int num_wait_longer;
    // number of times we went to calling SwitchToThread in WaitLonger.
    unsigned int num_switch_thread_w;
    // number of times we went to calling DisablePreemptiveGC in WaitLonger.
    unsigned int num_disable_preemptive_w;
#endif

    GCDebugSpinLock()
        : lock(-1)
#ifdef _DEBUG
        , holding_thread((Thread*) -1)
#endif
#if defined (SYNCHRONIZATION_STATS)
        , num_switch_thread(0), num_wait_longer(0), num_switch_thread_w(0), num_disable_preemptive_w(0)
#endif
    {
    }

#if defined (SYNCHRONIZATION_STATS)
    void init()
    {
        num_switch_thread = 0;
        num_wait_longer = 0;
        num_switch_thread_w = 0;
        num_disable_preemptive_w = 0;
    }
#endif
};
typedef GCDebugSpinLock GCSpinLock;

class mark;
class heap_segment;
class CObjectHeader;
class sorted_table;
class seg_free_spaces;
class gc_heap;

#ifdef BACKGROUND_GC
class exclusive_sync;
class recursive_gc_sync;
#endif //BACKGROUND_GC

#ifdef MULTIPLE_HEAPS
// This feature hasn't been enabled for regions yet.
//#ifndef USE_REGIONS
// card marking stealing only makes sense in server GC
// but it works and is easier to debug for workstation GC
// so turn it on for server GC, turn on for workstation GC if necessary
#define FEATURE_CARD_MARKING_STEALING
//#endif //!USE_REGIONS
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_CARD_MARKING_STEALING
class card_marking_enumerator;
#define CARD_MARKING_STEALING_ARG(a)    ,a
#define CARD_MARKING_STEALING_ARGS(a,b,c)    ,a,b,c
#else // FEATURE_CARD_MARKING_STEALING
#define CARD_MARKING_STEALING_ARG(a)
#define CARD_MARKING_STEALING_ARGS(a,b,c)
#endif // FEATURE_CARD_MARKING_STEALING

// The following 2 modes are of the same format as in clr\src\bcl\system\runtime\gcsettings.cs
// make sure you change that one if you change this one!
enum gc_pause_mode
{
    pause_batch = 0, //We are not concerned about pause length
    pause_interactive = 1,     //We are running an interactive app
    pause_low_latency = 2,     //short pauses are essential
    //avoid long pauses from blocking full GCs unless running out of memory
    pause_sustained_low_latency = 3,
    pause_no_gc = 4
};

enum gc_loh_compaction_mode
{
    loh_compaction_default = 1, // the default mode, don't compact LOH.
    loh_compaction_once = 2, // only compact once the next time a blocking full GC happens.
    loh_compaction_auto = 4 // GC decides when to compact LOH, to be implemented.
};

enum set_pause_mode_status
{
    set_pause_mode_success = 0,
    set_pause_mode_no_gc = 1 // NoGCRegion is in progress, can't change pause mode.
};

/*
 Latency modes required user to have specific GC knowledge (eg, budget, full blocking GC).
 We are trying to move away from them as it makes a lot more sense for users to tell
 us what's the most important out of the perf aspects that make sense to them.

 In general there are 3 such aspects:

 + memory footprint
 + throughput
 + pause predictibility

 Currently the following levels are supported. We may (and will likely) add more
 in the future.

 +----------+--------------------+---------------------------------------+
 | Level    | Optimization Goals | Latency Charactaristics               |
 +==========+====================+=======================================+
 | 0        | memory footprint   | pauses can be long and more frequent  |
 +----------+--------------------+---------------------------------------+
 | 1        | balanced           | pauses are more predictable and more  |
 |          |                    | frequent. the longest pauses are      |
 |          |                    | shorter than 1.                       |
 +----------+--------------------+---------------------------------------+
*/
enum gc_latency_level
{
    latency_level_first = 0,
    latency_level_memory_footprint = latency_level_first,
    latency_level_balanced = 1,
    latency_level_last = latency_level_balanced,
    latency_level_default = latency_level_balanced
};

enum gc_tuning_point
{
    tuning_deciding_condemned_gen = 0,
    tuning_deciding_full_gc = 1,
    tuning_deciding_compaction = 2,
    tuning_deciding_expansion = 3,
    tuning_deciding_promote_ephemeral = 4,
    tuning_deciding_short_on_seg = 5
};

enum gc_oh_num
{
    soh = 0,
    loh = 1,
    poh = 2,
    none = 3,
    total_oh_count = 4
};

gc_oh_num gen_to_oh (int gen);

#if defined(TRACE_GC) && defined(BACKGROUND_GC)
static const char * const str_bgc_state[] =
{
    "not_in_process",
    "mark_handles",
    "mark_stack",
    "revisit_soh",
    "revisit_loh",
    "overflow_soh",
    "overflow_loh",
    "final_marking",
    "sweep_soh",
    "sweep_loh",
    "plan_phase"
};
#endif // defined(TRACE_GC) && defined(BACKGROUND_GC)

enum allocation_state
{
    a_state_start = 0,
    a_state_can_allocate,
    a_state_cant_allocate,
    // This could be due to having to wait till a GC is done,
    // or having to try a different heap.
    a_state_retry_allocate,
    a_state_try_fit,
    a_state_try_fit_new_seg,
    a_state_try_fit_after_cg,
    a_state_try_fit_after_bgc,
    a_state_try_free_full_seg_in_bgc,
    a_state_try_free_after_bgc,
    a_state_try_seg_end,
    a_state_acquire_seg,
    a_state_acquire_seg_after_cg,
    a_state_acquire_seg_after_bgc,
    a_state_check_and_wait_for_bgc,
    a_state_trigger_full_compact_gc,
    a_state_trigger_ephemeral_gc,
    a_state_trigger_2nd_ephemeral_gc,
    a_state_check_retry_seg,
    a_state_max
};

enum gc_type
{
    gc_type_compacting = 0,
    gc_type_blocking = 1,
#ifdef BACKGROUND_GC
    gc_type_background = 2,
#endif //BACKGROUND_GC
    gc_type_max = 3
};

//encapsulates the mechanism for the current gc
class gc_mechanisms
{
public:
    VOLATILE(size_t) gc_index; // starts from 1 for the first GC, like dd_collection_count
    int condemned_generation;
    BOOL promotion;
    BOOL compaction;
    BOOL loh_compaction;
    BOOL heap_expansion;
    uint32_t concurrent;
    BOOL demotion;
    BOOL card_bundles;
    int  gen0_reduction_count;
    BOOL should_lock_elevation;
    int elevation_locked_count;
    BOOL elevation_reduced;
    BOOL minimal_gc;
    gc_reason reason;
    gc_pause_mode pause_mode;
    BOOL found_finalizers;

#ifdef BACKGROUND_GC
    BOOL background_p;
    bgc_state b_state;
    BOOL allocations_allowed;
#endif //BACKGROUND_GC

#ifdef STRESS_HEAP
    BOOL stress_induced;
#endif // STRESS_HEAP

    // These are opportunistically set
    uint32_t entry_memory_load;
    uint64_t entry_available_physical_mem;
    uint32_t exit_memory_load;

    void init_mechanisms(); //for each GC
    void first_init(); // for the life of the EE

    void record (gc_history_global* history);
};

// This is a compact version of gc_mechanism that we use to save in the history.
class gc_mechanisms_store
{
public:
    size_t gc_index;
    bool promotion;
    bool compaction;
    bool loh_compaction;
    bool heap_expansion;
    bool concurrent;
    bool demotion;
    bool card_bundles;
    bool should_lock_elevation;
    int condemned_generation   : 8;
    int gen0_reduction_count   : 8;
    int elevation_locked_count : 8;
    gc_reason reason           : 8;
    gc_pause_mode pause_mode   : 8;
#ifdef BACKGROUND_GC
    bgc_state b_state          : 8;
#endif //BACKGROUND_GC
    bool found_finalizers;

#ifdef BACKGROUND_GC
    bool background_p;
#endif //BACKGROUND_GC

#ifdef STRESS_HEAP
    bool stress_induced;
#endif // STRESS_HEAP

#ifdef HOST_64BIT
    uint32_t entry_memory_load;
#endif // HOST_64BIT

    void store (gc_mechanisms* gm)
    {
        gc_index                = gm->gc_index;
        condemned_generation    = gm->condemned_generation;
        promotion               = (gm->promotion != 0);
        compaction              = (gm->compaction != 0);
        loh_compaction          = (gm->loh_compaction != 0);
        heap_expansion          = (gm->heap_expansion != 0);
        concurrent              = (gm->concurrent != 0);
        demotion                = (gm->demotion != 0);
        card_bundles            = (gm->card_bundles != 0);
        gen0_reduction_count    = gm->gen0_reduction_count;
        should_lock_elevation   = (gm->should_lock_elevation != 0);
        elevation_locked_count  = gm->elevation_locked_count;
        reason                  = gm->reason;
        pause_mode              = gm->pause_mode;
        found_finalizers        = (gm->found_finalizers != 0);

#ifdef BACKGROUND_GC
        background_p            = (gm->background_p != 0);
        b_state                 = gm->b_state;
#endif //BACKGROUND_GC

#ifdef STRESS_HEAP
        stress_induced          = (gm->stress_induced != 0);
#endif // STRESS_HEAP

#ifdef HOST_64BIT
        entry_memory_load       = gm->entry_memory_load;
#endif // HOST_64BIT
    }
};

typedef DPTR(class heap_segment)               PTR_heap_segment;
typedef DPTR(class gc_heap)                    PTR_gc_heap;
typedef DPTR(PTR_gc_heap)                      PTR_PTR_gc_heap;
#ifdef FEATURE_PREMORTEM_FINALIZATION
typedef DPTR(class CFinalize)                  PTR_CFinalize;
#endif // FEATURE_PREMORTEM_FINALIZATION

//-------------------------------------
//generation free list. It is an array of free lists bucketed by size, starting at sizes lower than (1 << first_bucket_bits)
//and doubling each time. The last bucket (index == num_buckets) is for largest sizes with no limit

#define MAX_SOH_BUCKET_COUNT (13)//Max number of buckets for the SOH generations.
#define MAX_BUCKET_COUNT (20)//Max number of buckets.
class alloc_list
{
#ifdef DOUBLY_LINKED_FL
    uint8_t* added_head;
    uint8_t* added_tail;
#endif //DOUBLY_LINKED_FL

    uint8_t* head;
    uint8_t* tail;

    size_t damage_count;
public:
#ifdef FL_VERIFICATION
    size_t item_count;
#endif //FL_VERIFICATION

#ifdef DOUBLY_LINKED_FL
    uint8_t*& added_alloc_list_head () { return added_head;}
    uint8_t*& added_alloc_list_tail () { return added_tail;}
#endif //DOUBLY_LINKED_FL

    uint8_t*& alloc_list_head () { return head;}
    uint8_t*& alloc_list_tail () { return tail;}
    size_t& alloc_list_damage_count(){ return damage_count; }
    alloc_list()
    {
#ifdef DOUBLY_LINKED_FL
        added_head = 0;
        added_tail = 0;
#endif //DOUBLY_LINKED_FL
        head = 0;
        tail = 0;
        damage_count = 0;
    }
};

#ifdef FEATURE_EVENT_TRACE
struct etw_bucket_info
{
    uint16_t index;
    uint32_t count;
    size_t size;

    etw_bucket_info() {}

    void set (uint16_t _index, uint32_t _count, size_t _size)
    {
        index = _index;
        count = _count;
        size = _size;
    }
};
#endif //FEATURE_EVENT_TRACE

class allocator
{
    int first_bucket_bits;
    unsigned int num_buckets;
    alloc_list first_bucket;
    alloc_list* buckets;
    int gen_number;
    alloc_list& alloc_list_of (unsigned int bn);
    size_t& alloc_list_damage_count_of (unsigned int bn);
    void thread_free_item_end (uint8_t* free_item, uint8_t*& head, uint8_t*& tail, int bn);

public:
    allocator (unsigned int num_b, int fbb, alloc_list* b, int gen=-1);

    allocator()
    {
        num_buckets = 1;
        first_bucket_bits = sizeof(size_t) * 8 - 1;
        // for young gens we just set it to 0 since we don't treat
        // them differently from each other
        gen_number = 0;
    }

    unsigned int number_of_buckets()
    {
        return num_buckets;
    }

    // skip buckets that cannot possibly fit "size" and return the next one
    // there is always such bucket since the last one fits everything
    unsigned int first_suitable_bucket (size_t size)
    {
        // sizes taking first_bucket_bits or less are mapped to bucket 0
        // others are mapped to buckets 0, 1, 2 respectively
        size = (size >> first_bucket_bits) | 1;

        DWORD highest_set_bit_index;
    #ifdef HOST_64BIT
        BitScanReverse64(&highest_set_bit_index, size);
    #else
        BitScanReverse(&highest_set_bit_index, size);
    #endif

        return min ((unsigned int)highest_set_bit_index, (num_buckets - 1));
    }

    size_t first_bucket_size()
    {
        return ((size_t)1 << (first_bucket_bits + 1));
    }

    uint8_t*& alloc_list_head_of (unsigned int bn)
    {
        return alloc_list_of (bn).alloc_list_head();
    }

    uint8_t*& alloc_list_tail_of (unsigned int bn)
    {
        return alloc_list_of (bn).alloc_list_tail();
    }

#ifdef DOUBLY_LINKED_FL
    uint8_t*& added_alloc_list_head_of (unsigned int bn)
    {
        return alloc_list_of (bn).added_alloc_list_head();
    }
    uint8_t*& added_alloc_list_tail_of (unsigned int bn)
    {
        return alloc_list_of (bn).added_alloc_list_tail();
    }
#endif //DOUBLY_LINKED_FL

    void clear();

    BOOL discard_if_no_fit_p()
    {
        return (num_buckets == 1);
    }

    // This is when we know there's nothing to repair because this free
    // list has never gone through plan phase. Right now it's only used
    // by the background ephemeral sweep when we copy the local free list
    // to gen0's free list.
    //
    // We copy head and tail manually (vs together like copy_to_alloc_list)
    // since we need to copy tail first because when we get the free items off
    // of each bucket we check head first. We also need to copy the
    // smaller buckets first so when gen0 allocation needs to thread
    // smaller items back that bucket is guaranteed to have been full
    // copied.
    void copy_with_no_repair (allocator* allocator_to_copy)
    {
        assert (num_buckets == allocator_to_copy->number_of_buckets());
        for (unsigned int i = 0; i < num_buckets; i++)
        {
            alloc_list* al = &(allocator_to_copy->alloc_list_of (i));
            alloc_list_tail_of(i) = al->alloc_list_tail();
            alloc_list_head_of(i) = al->alloc_list_head();
        }
    }

    void unlink_item (unsigned int bn, uint8_t* item, uint8_t* previous_item, BOOL use_undo_p);
    void thread_item (uint8_t* item, size_t size);
    void thread_item_front (uint8_t* itme, size_t size);
#ifdef DOUBLY_LINKED_FL
    int thread_item_front_added (uint8_t* itme, size_t size);
    void unlink_item_no_undo (uint8_t* item, size_t size);
    void unlink_item_no_undo (unsigned int bn, uint8_t* item, size_t size);
    void unlink_item_no_undo_added (unsigned int bn, uint8_t* item, uint8_t* previous_item);
#endif //DOUBLY_LINKED_FL

    void copy_to_alloc_list (alloc_list* toalist);
    void copy_from_alloc_list (alloc_list* fromalist);
    void commit_alloc_list_changes();

#ifdef USE_REGIONS
    void thread_sip_fl (heap_segment* region);
#endif //USE_REGIONS

#ifdef FEATURE_EVENT_TRACE
    uint16_t count_largest_items (etw_bucket_info* bucket_info, 
                                  size_t max_size,
                                  size_t max_item_count,
                                  size_t* recorded_fl_info_size);
#endif //FEATURE_EVENT_TRACE
};

#define NUM_GEN_POWER2 (20)
#define BASE_GEN_SIZE (1*512)

// group the frequently used ones together (need intrumentation on accessors)
class generation
{
public:
    // Don't move these first two fields without adjusting the references
    // from the __asm in jitinterface.cpp.
    alloc_context   allocation_context;
    PTR_heap_segment start_segment;
#ifndef USE_REGIONS
    uint8_t*        allocation_start;
#endif //!USE_REGIONS
    heap_segment*   allocation_segment;
    uint8_t*        allocation_context_start_region;
#ifdef USE_REGIONS
    heap_segment*   tail_region;
    heap_segment*   plan_start_segment;
    // only max_generation could have ro regions; for other generations 
    // this will be 0.
    heap_segment*   tail_ro_region;
#endif //USE_REGIONS
    allocator       free_list_allocator;
    size_t          free_list_allocated;
    size_t          end_seg_allocated;
    BOOL            allocate_end_seg_p;
    size_t          condemned_allocated;
    size_t          sweep_allocated;
    size_t          free_list_space;
    size_t          free_obj_space;
    size_t          allocation_size;
#ifndef USE_REGIONS
    uint8_t*        plan_allocation_start;
    size_t          plan_allocation_start_size;
#endif //!USE_REGIONS

    // this is the pinned plugs that got allocated into this gen.
    size_t          pinned_allocated;
    size_t          pinned_allocation_compact_size;
    size_t          pinned_allocation_sweep_size;
    int             gen_num;

#ifdef DOUBLY_LINKED_FL
    BOOL            set_bgc_mark_bit_p;
    uint8_t*        last_free_list_allocated;
#endif //DOUBLY_LINKED_FL

#ifdef FREE_USAGE_STATS
    size_t          gen_free_spaces[NUM_GEN_POWER2];
    // these are non pinned plugs only
    size_t          gen_plugs[NUM_GEN_POWER2];
    size_t          gen_current_pinned_free_spaces[NUM_GEN_POWER2];
    size_t          pinned_free_obj_space;
    // this is what got allocated into the pinned free spaces.
    size_t          allocated_in_pinned_free;
    size_t          allocated_since_last_pin;
#endif //FREE_USAGE_STATS
};

static_assert(offsetof(dac_generation, allocation_context) == offsetof(generation, allocation_context), "DAC generation offset mismatch");
static_assert(offsetof(dac_generation, start_segment) == offsetof(generation, start_segment), "DAC generation offset mismatch");
#ifndef USE_REGIONS
static_assert(offsetof(dac_generation, allocation_start) == offsetof(generation, allocation_start), "DAC generation offset mismatch");
#endif //!USE_REGIONS

// static data remains the same after it's initialized.
// It's per generation.
// TODO: for gen_time_tuning, we should put the multipliers in static data.
struct static_data
{
    size_t min_size;
    size_t max_size;
    size_t fragmentation_limit;
    float fragmentation_burden_limit;
    float limit;
    float max_limit;
    uint64_t time_clock; // time after which to collect generation, in performance counts (see QueryPerformanceCounter)
    size_t gc_clock; // nubmer of gcs after which to collect generation
};

// The dynamic data fields are grouped into 3 categories:
//
// calculated logical data (like desired_allocation)
// physical data (like fragmentation)
// const data (sdata), initialized at the beginning
class dynamic_data
{
public:
    ptrdiff_t new_allocation;
    ptrdiff_t gc_new_allocation; // new allocation at beginning of gc
    float     surv;
    size_t    desired_allocation;

    // # of bytes taken by objects (ie, not free space) at the beginning
    // of the GC.
    size_t    begin_data_size;
    // # of bytes taken by survived objects after mark.
    size_t    survived_size;
    // # of bytes taken by survived pinned plugs after mark.
    size_t    pinned_survived_size;
    size_t    artificial_pinned_survived_size;
    size_t    added_pinned_size;

#ifdef SHORT_PLUGS
    size_t    padding_size;
#endif //SHORT_PLUGS
#if defined (RESPECT_LARGE_ALIGNMENT) || defined (FEATURE_STRUCTALIGN)
    // # of plugs that are not pinned plugs.
    size_t    num_npinned_plugs;
#endif //RESPECT_LARGE_ALIGNMENT || FEATURE_STRUCTALIGN
    //total object size after a GC, ie, doesn't include fragmentation
    size_t    current_size;
    size_t    collection_count;
    size_t    promoted_size;
    size_t    freach_previous_promotion;
    size_t    fragmentation;    //fragmentation when we don't compact
    size_t    gc_clock;         //gc# when last GC happened
    uint64_t  time_clock;       //time when last gc started
    uint64_t  previous_time_clock; // time when previous gc started
    size_t    gc_elapsed_time;  // Time it took for the gc to complete
    float     gc_speed;         //  speed in bytes/msec for the gc to complete

    size_t    min_size;

    static_data* sdata;
};

struct recorded_generation_info
{
    size_t size_before;
    size_t fragmentation_before;
    size_t size_after;
    size_t fragmentation_after;
};

struct last_recorded_gc_info
{
    VOLATILE(size_t) index;
    size_t total_committed;
    size_t promoted;
    size_t pinned_objects;
    size_t finalize_promoted_objects;
    size_t pause_durations[2];
    float pause_percentage;
    recorded_generation_info gen_info[total_generation_count];
    size_t heap_size;
    size_t fragmentation;
    uint32_t memory_load;
    uint8_t condemned_generation;
    bool compaction;
    bool concurrent;
};

// alignment helpers
//Alignment constant for allocation
#define ALIGNCONST (DATA_ALIGNMENT-1)

inline
size_t Align (size_t nbytes, int alignment=ALIGNCONST)
{
    return (nbytes + alignment) & ~alignment;
}

//return alignment constant for small object heap vs large object heap
inline
int get_alignment_constant (BOOL small_object_p)
{
#ifdef FEATURE_STRUCTALIGN
    // If any objects on the large object heap require 8-byte alignment,
    // the compiler will tell us so.  Let's not guess an alignment here.
    return ALIGNCONST;
#else // FEATURE_STRUCTALIGN
    return small_object_p ? ALIGNCONST : 7;
#endif // FEATURE_STRUCTALIGN
}

struct etw_opt_info
{
    size_t desired_allocation;
    size_t new_allocation;
    int    gen_number;
};

// Note, I am not removing the ones that are no longer used
// because the older versions of the runtime still use them
// and ETW interprets them.
enum alloc_wait_reason
{
    // When we don't care about firing an event for
    // this.
    awr_ignored = -1,

    // when we detect we are in low memory
    awr_low_memory = 0,

    // when we detect the ephemeral segment is too full
    awr_low_ephemeral = 1,

    // we've given out too much budget for gen0.
    awr_gen0_alloc = 2,

    // we've given out too much budget for loh.
    awr_loh_alloc = 3,

    // this event is really obsolete - it's for pre-XP
    // OSs where low mem notification is not supported.
    awr_alloc_loh_low_mem = 4,

    // we ran out of VM spaced to reserve on loh.
    awr_loh_oos = 5,

    // ran out of space when allocating a small object
    awr_gen0_oos_bgc = 6,

    // ran out of space when allocating a large object
    awr_loh_oos_bgc = 7,

    // waiting for BGC to let FGC happen
    awr_fgc_wait_for_bgc = 8,

    // wait for bgc to finish to get loh seg.
    // no longer used with the introduction of loh msl.
    awr_get_loh_seg = 9,

    // we don't allow loh allocation during bgc planning.
    // no longer used with the introduction of loh msl.
    awr_loh_alloc_during_plan = 10,

    // we don't allow too much uoh allocation during bgc.
    awr_uoh_alloc_during_bgc = 11
};

struct alloc_thread_wait_data
{
    int awr;
};

enum msl_take_state
{
    mt_get_large_seg = 0,
    mt_bgc_uoh_sweep,
    mt_wait_bgc,
    mt_block_gc,
    mt_clr_mem,
    mt_clr_large_mem,
    mt_t_eph_gc,
    mt_t_full_gc,
    mt_alloc_small,
    mt_alloc_large,
    mt_alloc_small_cant,
    mt_alloc_large_cant,
    mt_try_alloc,
    mt_try_budget,
    mt_try_servo_budget
};

enum msl_enter_state
{
    me_acquire,
    me_release
};

struct spinlock_info
{
    msl_enter_state enter_state;
    msl_take_state take_state;
    EEThreadId thread_id;
    bool loh_p;
};

#define HS_CACHE_LINE_SIZE 128

#ifdef SNOOP_STATS
struct snoop_stats_data
{
    int heap_index;

    // total number of objects that we called
    // gc_mark on.
    size_t objects_checked_count;
    // total number of time we called gc_mark
    // on a 0 reference.
    size_t zero_ref_count;
    // total objects actually marked.
    size_t objects_marked_count;
    // number of objects written to the mark stack because
    // of mark_stolen.
    size_t stolen_stack_count;
    // number of objects pushed onto the mark stack because
    // of the partial mark code path.
    size_t partial_stack_count;
    // number of objects pushed onto the mark stack because
    // of the non partial mark code path.
    size_t normal_stack_count;
    // number of references marked without mark stack.
    size_t non_stack_count;

    // number of times we detect next heap's mark stack
    // is not busy.
    size_t stack_idle_count;

    // number of times we do switch to thread.
    size_t switch_to_thread_count;

    // number of times we are checking if the next heap's
    // mark stack is busy.
    size_t check_level_count;
    // number of times next stack is busy and level is
    // at the bottom.
    size_t busy_count;
    // how many interlocked exchange operations we did
    size_t interlocked_count;
    // numer of times parent objects stolen
    size_t partial_mark_parent_count;
    // numer of times we look at a normal stolen entry,
    // or the beginning/ending PM pair.
    size_t stolen_or_pm_count;
    // number of times we see 2 for the entry.
    size_t stolen_entry_count;
    // number of times we see a PM entry that's not ready.
    size_t pm_not_ready_count;
    // number of stolen normal marked objects and partial mark children.
    size_t normal_count;
    // number of times the bottom of mark stack was cleared.
    size_t stack_bottom_clear_count;
};
#endif //SNOOP_STATS

struct no_gc_region_info
{
    size_t soh_allocation_size;
    size_t loh_allocation_size;
    size_t started;
    size_t num_gcs;
    size_t num_gcs_induced;
    start_no_gc_region_status start_status;
    gc_pause_mode saved_pause_mode;
    size_t saved_gen0_min_size;
    size_t saved_gen3_min_size;
    BOOL minimal_gc_p;
};

// if you change these, make sure you update them for sos (strike.cpp) as well.
//
// !!!NOTE!!!
// Right now I am only recording data from blocking GCs. When recording from BGC,
// it should have its own copy just like gc_data_per_heap.
// for BGCs we will have a very different set of datapoints to record.
enum interesting_data_point
{
    idp_pre_short = 0,
    idp_post_short = 1,
    idp_merged_pin = 2,
    idp_converted_pin = 3,
    idp_pre_pin = 4,
    idp_post_pin = 5,
    idp_pre_and_post_pin = 6,
    idp_pre_short_padded = 7,
    idp_post_short_padded = 8,
    max_idp_count
};

//class definition of the internal class
class gc_heap
{
    friend class GCHeap;
#ifdef FEATURE_PREMORTEM_FINALIZATION
    friend class CFinalize;
#endif // FEATURE_PREMORTEM_FINALIZATION
    friend struct ::alloc_context;
    friend void ProfScanRootsHelper(Object** object, ScanContext *pSC, uint32_t dwFlags);
    friend void GCProfileWalkHeapWorker(BOOL fProfilerPinned, BOOL fShouldWalkHeapRootsForEtw, BOOL fShouldWalkHeapObjectsForEtw);
    friend class t_join;
    friend class gc_mechanisms;
    friend class seg_free_spaces;

#ifdef BACKGROUND_GC
    friend class exclusive_sync;
    friend class recursive_gc_sync;
#endif //BACKGROUND_GC

#if defined (WRITE_BARRIER_CHECK) && !defined (SERVER_GC)
    friend void checkGCWriteBarrier();
    friend void initGCShadow();
#endif //defined (WRITE_BARRIER_CHECK) && !defined (SERVER_GC)

    friend void PopulateDacVars(GcDacVars *gcDacVars);

#ifdef MULTIPLE_HEAPS
    typedef void (gc_heap::* card_fn) (uint8_t**, int);
#define call_fn(this_arg,fn) (this_arg->*fn)
#define __this this
#else
    typedef void (* card_fn) (uint8_t**);
#define call_fn(this_arg,fn) (*fn)
#define __this (gc_heap*)0
#endif

public:

#ifdef TRACE_GC
    PER_HEAP
    void print_free_list (int gen, heap_segment* seg);
#endif // TRACE_GC

#ifdef SYNCHRONIZATION_STATS

    PER_HEAP_ISOLATED
    void init_sync_stats()
    {
#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < gc_heap::n_heaps; i++)
        {
            gc_heap::g_heaps[i]->init_heap_sync_stats();
        }
#else  //MULTIPLE_HEAPS
        init_heap_sync_stats();
#endif  //MULTIPLE_HEAPS
    }

    PER_HEAP_ISOLATED
    void print_sync_stats(unsigned int gc_count_during_log)
    {
        // bad/good gl acquire is accumulative during the log interval (because the numbers are too small)
        // min/max msl_acquire is the min/max during the log interval, not each GC.
        // Threads is however many allocation threads for the last GC.
        // num of msl acquired, avg_msl, high and low are all for each GC.
        printf("%2s%2s%10s%10s%12s%6s%4s%8s(  st,  wl, stw, dpw)\n",
            "H", "T", "good_sus", "bad_sus", "avg_msl", "high", "low", "num_msl");

#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < gc_heap::n_heaps; i++)
        {
            gc_heap::g_heaps[i]->print_heap_sync_stats(i, gc_count_during_log);
        }
#else  //MULTIPLE_HEAPS
        print_heap_sync_stats(0, gc_count_during_log);
#endif  //MULTIPLE_HEAPS
    }

#endif //SYNCHRONIZATION_STATS

    PER_HEAP
    void verify_soh_segment_list();

#ifdef VERIFY_HEAP
    PER_HEAP
    void verify_free_lists();
    PER_HEAP
    void verify_regions (int gen_number, bool can_verify_gen_num);
    PER_HEAP
    void verify_regions (bool can_verify_gen_num);
    PER_HEAP_ISOLATED
    void enter_gc_lock_for_verify_heap();
    PER_HEAP_ISOLATED
    void leave_gc_lock_for_verify_heap();
    PER_HEAP
    void verify_heap (BOOL begin_gc_p);
    PER_HEAP
    BOOL check_need_card (uint8_t* child_obj, int gen_num_for_cards, 
                          uint8_t* low, uint8_t* high);
#endif //VERIFY_HEAP

    PER_HEAP_ISOLATED
    void fire_per_heap_hist_event (gc_history_per_heap* current_gc_data_per_heap, int heap_num);

    PER_HEAP_ISOLATED
    void fire_pevents();

#ifdef FEATURE_BASICFREEZE
    static void walk_read_only_segment(heap_segment *seg, void *pvContext, object_callback_func pfnMethodTable, object_callback_func pfnObjRef);
#endif

    PER_HEAP_ISOLATED
    int get_plan_gen_num (int gen_number);

    // region is only needed for regions and gen is only needed otherwise for these
    // 2 methods.
    //
    // Skips the generation start object for non regions.
    PER_HEAP
    uint8_t* get_uoh_start_object (heap_segment* region, generation* gen);

    // Does not skip the generation start object for non regions.
    PER_HEAP
    uint8_t* get_soh_start_object (heap_segment* region, generation* gen);

    PER_HEAP
    size_t get_soh_start_obj_len (uint8_t* start_obj);

    PER_HEAP
    void clear_gen1_cards();

#ifdef USE_REGIONS
    PER_HEAP
    bool sufficient_space_regions (size_t end_space, size_t end_space_required);
    PER_HEAP
    bool initial_make_soh_regions (gc_heap* hp);
    PER_HEAP
    bool initial_make_uoh_regions (int gen, gc_heap* hp);
    PER_HEAP
    void return_free_region (heap_segment* region);
    PER_HEAP
    heap_segment* get_free_region (int gen_number, size_t size = 0);
    PER_HEAP
    void clear_region_info (heap_segment* region);
    PER_HEAP_ISOLATED
    heap_segment* region_of (uint8_t* obj);
    PER_HEAP_ISOLATED
    heap_segment* get_region_at_index (size_t index);
    PER_HEAP_ISOLATED
    int get_region_gen_num (heap_segment* region);
    PER_HEAP
    void check_seg_gen_num (heap_segment* seg);
    PER_HEAP_ISOLATED
    int get_region_gen_num (uint8_t* obj);
    PER_HEAP_ISOLATED
    void set_region_gen_num (heap_segment* region, int gen_num);
    PER_HEAP_ISOLATED
    int get_region_plan_gen_num (uint8_t* obj);
    PER_HEAP_ISOLATED
    bool is_region_demoted (uint8_t* obj);
    PER_HEAP
    void set_region_plan_gen_num (heap_segment* region, int plan_gen_num);
    PER_HEAP
    void set_region_plan_gen_num_sip (heap_segment* region, int plan_gen_num);
    PER_HEAP
    void decide_on_demotion_pin_surv (heap_segment* region);
    PER_HEAP
    void skip_pins_in_alloc_region (generation* consing_gen, int plan_gen_num);
    PER_HEAP
    void process_last_np_surv_region (generation* consing_gen,
                                      int current_plan_gen_num,
                                      int next_plan_gen_num);
    PER_HEAP
    void process_remaining_regions (int current_plan_gen_num,
                                    generation* consing_gen);

    PER_HEAP
    void grow_mark_list_piece();
    PER_HEAP
    void save_current_survived();
    PER_HEAP
    void update_old_card_survived();

    // Used as we discover free spaces before pins during plan.
    // the plug arg is only for logging.
    PER_HEAP
    void update_planned_gen0_free_space (size_t free_size, uint8_t* plug);
    // used when deciding on expansion.
    PER_HEAP
    void get_gen0_end_plan_space();
    PER_HEAP
    size_t get_gen0_end_space();
    PER_HEAP
    bool decide_on_compaction_space();
    PER_HEAP
    bool try_get_new_free_region();
    PER_HEAP
    bool init_table_for_region (int gen_number, heap_segment* region);
    PER_HEAP
    heap_segment* find_first_valid_region (heap_segment* region, bool compact_p);
    PER_HEAP
    void thread_final_regions (bool compact_p);
    PER_HEAP
    void thread_start_region (generation* gen, heap_segment* region);
    PER_HEAP
    heap_segment* get_new_region (int gen_number, size_t size = 0);
    // This allocates one from region allocator and commit the mark array if needed.
    PER_HEAP_ISOLATED
    heap_segment* allocate_new_region (gc_heap* hp, int gen_num, bool uoh_p, size_t size = 0);
    // When we delete a region we need to update start and tail region
    // if needed.
    PER_HEAP
    void update_start_tail_regions (generation* gen,
                                    heap_segment* region_to_delete, 
                                    heap_segment* prev_region, 
                                    heap_segment* next_region);
    PER_HEAP
    bool should_sweep_in_plan (heap_segment* region);

    PER_HEAP
    void sweep_region_in_plan (heap_segment* region, 
                               BOOL use_mark_list, 
                               uint8_t**& mark_list_next,
                               uint8_t** mark_list_index);

    PER_HEAP
    void check_demotion_helper_sip (uint8_t** pval, 
                                    int parent_gen_num, 
                                    uint8_t* parent_loc);
    // This relocates the SIP regions and return the next non SIP region.
    PER_HEAP
    heap_segment* relocate_advance_to_non_sip (heap_segment* region);
#ifdef STRESS_REGIONS
    PER_HEAP
    void pin_by_gc (uint8_t* object);
#endif //STRESS_REGIONS
#endif //USE_REGIONS

    static
    heap_segment* make_heap_segment (uint8_t* new_pages,
                                     size_t size,
                                     gc_heap* hp,
                                     int gen_num);

    static
    gc_heap* make_gc_heap(
#if defined (MULTIPLE_HEAPS)
        GCHeap* vm_heap,
        int heap_number
#endif //MULTIPLE_HEAPS
        );

    static
    void destroy_gc_heap(gc_heap* heap);

    static
    HRESULT initialize_gc  (size_t soh_segment_size,
                            size_t loh_segment_size,
                            size_t poh_segment_size
#ifdef MULTIPLE_HEAPS
                            , int number_of_heaps
#endif //MULTIPLE_HEAPS
        );

    static
    void shutdown_gc();

    // If the hard limit is specified, take that into consideration
    // and this means it may modify the # of heaps.
    PER_HEAP_ISOLATED
    size_t get_segment_size_hard_limit (uint32_t* num_heaps, bool should_adjust_num_heaps);

    PER_HEAP_ISOLATED
    bool should_retry_other_heap (int gen_number, size_t size);

    PER_HEAP
    bool check_against_hard_limit (size_t space_required);

    PER_HEAP
    CObjectHeader* allocate (size_t jsize,
                             alloc_context* acontext,
                             uint32_t flags);

#ifdef MULTIPLE_HEAPS
    PER_HEAP_ISOLATED
    void hb_log_new_allocation();

    PER_HEAP_ISOLATED
    void hb_log_balance_activities();

    static
    void balance_heaps (alloc_context* acontext);
    PER_HEAP
    ptrdiff_t get_balance_heaps_uoh_effective_budget (int generation_num);
    static
    gc_heap* balance_heaps_uoh (alloc_context* acontext, size_t size, int generation_num);
    // Unlike balance_heaps_uoh, this may return nullptr if we failed to change heaps.
    static
    gc_heap* balance_heaps_uoh_hard_limit_retry (alloc_context* acontext, size_t size, int generation_num);
    static
    void gc_thread_stub (void* arg);
#endif //MULTIPLE_HEAPS

    // For UOH allocations we only update the alloc_bytes_uoh in allocation
    // context - we don't actually use the ptr/limit from it so I am
    // making this explicit by not passing in the alloc_context.
    // Note: This are instance methods, but the heap instance is only used for
    // lowest_address and highest_address, which are currently the same across all heaps.
    PER_HEAP
    CObjectHeader* allocate_uoh_object (size_t size, uint32_t flags, int gen_num, int64_t& alloc_bytes);

#ifdef FEATURE_STRUCTALIGN
    PER_HEAP
    uint8_t* pad_for_alignment_large (uint8_t* newAlloc, int requiredAlignment, size_t size);
#endif // FEATURE_STRUCTALIGN

    PER_HEAP_ISOLATED
    void do_pre_gc();

    PER_HEAP_ISOLATED
    void do_post_gc();

    PER_HEAP_ISOLATED
    void update_recorded_gen_data (last_recorded_gc_info* gc_info);

    PER_HEAP
    void update_end_gc_time_per_heap();

    PER_HEAP_ISOLATED
    void update_end_ngc_time();

    PER_HEAP
    void add_to_history_per_heap();

    PER_HEAP_ISOLATED
    void add_to_history();

#ifdef BGC_SERVO_TUNING
    PER_HEAP_ISOLATED
    void check_and_adjust_bgc_tuning (int gen_number, size_t physical_size, ptrdiff_t virtual_fl_size);
    PER_HEAP_ISOLATED
    void get_and_reset_loh_alloc_info();
#endif //BGC_SERVO_TUNING

    PER_HEAP
    BOOL expand_soh_with_minimal_gc();

    // EE is always suspended when this method is called.
    // returning FALSE means we actually didn't do a GC. This happens
    // when we figured that we needed to do a BGC.
    PER_HEAP
    void garbage_collect (int n);

    // Since we don't want to waste a join just to do this, I am doing
    // doing this at the last join in gc1.
    PER_HEAP_ISOLATED
    void pm_full_gc_init_or_clear();

    // This does a GC when pm_trigger_full_gc is set
    PER_HEAP
    void garbage_collect_pm_full_gc();

    PER_HEAP_ISOLATED
    bool is_pm_ratio_exceeded();

    PER_HEAP
    void init_records();

    static
    uint32_t* make_card_table (uint8_t* start, uint8_t* end);

    static
    void set_fgm_result (failure_get_memory f, size_t s, BOOL loh_p);

    static
    int grow_brick_card_tables (uint8_t* start,
                                uint8_t* end,
                                size_t size,
                                heap_segment* new_seg,
                                gc_heap* hp,
                                BOOL loh_p);

    PER_HEAP_ISOLATED
    BOOL is_mark_set (uint8_t* o);

#ifdef FEATURE_BASICFREEZE
    PER_HEAP_ISOLATED
    bool frozen_object_p(Object* obj);
#endif // FEATURE_BASICFREEZE

protected:
    PER_HEAP_ISOLATED
    BOOL reserve_initial_memory (size_t normal_size, size_t large_size, size_t pinned_size, int num_heaps, bool use_large_pages_p, bool separated_poh_p, uint16_t* heap_no_to_numa_node);

    PER_HEAP_ISOLATED
    void destroy_initial_memory();

    PER_HEAP_ISOLATED
    void walk_heap (walk_fn fn, void* context, int gen_number, BOOL walk_large_object_heap_p);

    PER_HEAP
    void walk_heap_per_heap (walk_fn fn, void* context, int gen_number, BOOL walk_large_object_heap_p);

    struct walk_relocate_args
    {
        uint8_t* last_plug;
        BOOL is_shortened;
        mark* pinned_plug_entry;
        void* profiling_context;
        record_surv_fn fn;
    };

    PER_HEAP
    void walk_survivors (record_surv_fn fn, void* context, walk_surv_type type);

    PER_HEAP
    void walk_plug (uint8_t* plug, size_t size, BOOL check_last_object_p,
                    walk_relocate_args* args);

    PER_HEAP
    void walk_relocation (void* profiling_context, record_surv_fn fn);

    PER_HEAP
    void walk_relocation_in_brick (uint8_t* tree, walk_relocate_args* args);

    PER_HEAP
    void walk_finalize_queue (fq_walk_fn fn);

#if defined(BACKGROUND_GC) && defined(FEATURE_EVENT_TRACE)
    PER_HEAP
    void walk_survivors_for_bgc (void* profiling_context, record_surv_fn fn);
#endif // defined(BACKGROUND_GC) && defined(FEATURE_EVENT_TRACE)

    // used in blocking GCs after plan phase so this walks the plugs.
    PER_HEAP
    void walk_survivors_relocation (void* profiling_context, record_surv_fn fn);
    PER_HEAP
    void walk_survivors_for_uoh (void* profiling_context, record_surv_fn fn, int gen_number);

    PER_HEAP
    int generation_to_condemn (int n,
                               BOOL* blocking_collection_p,
                               BOOL* elevation_requested_p,
                               BOOL check_only_p);

    PER_HEAP_ISOLATED
    int joined_generation_to_condemn (BOOL should_evaluate_elevation,
                                      int initial_gen,
                                      int current_gen,
                                      BOOL* blocking_collection
                                      STRESS_HEAP_ARG(int n_original));

    PER_HEAP
    size_t min_reclaim_fragmentation_threshold (uint32_t num_heaps);

    PER_HEAP_ISOLATED
    uint64_t min_high_fragmentation_threshold (uint64_t available_mem, uint32_t num_heaps);

    PER_HEAP
    void concurrent_print_time_delta (const char* msg);
    PER_HEAP
    void free_list_info (int gen_num, const char* msg);

    // in svr GC on entry and exit of this method, the GC threads are not
    // synchronized
    PER_HEAP
    void gc1();

    PER_HEAP_ISOLATED
    void save_data_for_no_gc();

    PER_HEAP_ISOLATED
    void restore_data_for_no_gc();

    PER_HEAP_ISOLATED
    void update_collection_counts_for_no_gc();

    PER_HEAP_ISOLATED
    BOOL should_proceed_with_gc();

    PER_HEAP_ISOLATED
    void record_gcs_during_no_gc();

    PER_HEAP
    BOOL find_loh_free_for_no_gc();

    PER_HEAP
    BOOL find_loh_space_for_no_gc();

    PER_HEAP
    BOOL commit_loh_for_no_gc (heap_segment* seg);

    PER_HEAP_ISOLATED
    start_no_gc_region_status prepare_for_no_gc_region (uint64_t total_size,
                                                        BOOL loh_size_known,
                                                        uint64_t loh_size,
                                                        BOOL disallow_full_blocking);

    PER_HEAP
    BOOL loh_allocated_for_no_gc();

    PER_HEAP_ISOLATED
    void release_no_gc_loh_segments();

    PER_HEAP_ISOLATED
    void thread_no_gc_loh_segments();

    PER_HEAP
    void check_and_set_no_gc_oom();

    PER_HEAP
    void allocate_for_no_gc_after_gc();

    PER_HEAP
    void set_loh_allocations_for_no_gc();

    PER_HEAP
    void set_soh_allocations_for_no_gc();

    PER_HEAP
    void prepare_for_no_gc_after_gc();

    PER_HEAP_ISOLATED
    void set_allocations_for_no_gc();

    PER_HEAP_ISOLATED
    BOOL should_proceed_for_no_gc();

    PER_HEAP_ISOLATED
    start_no_gc_region_status get_start_no_gc_region_status();

    PER_HEAP_ISOLATED
    end_no_gc_region_status end_no_gc_region();

    PER_HEAP_ISOLATED
    void handle_failure_for_no_gc();

    PER_HEAP
    void fire_mark_event (int root_type, 
                          size_t& current_promoted_bytes, 
                          size_t& last_promoted_bytes);

    PER_HEAP
    size_t limit_from_size (size_t size, uint32_t flags, size_t room, int gen_number,
                            int align_const);
    PER_HEAP
    allocation_state try_allocate_more_space (alloc_context* acontext, size_t jsize, uint32_t flags,
                                              int alloc_generation_number);
    PER_HEAP_ISOLATED
    BOOL allocate_more_space (alloc_context* acontext, size_t jsize, uint32_t flags,
                              int alloc_generation_number);

    PER_HEAP
    size_t get_full_compact_gc_count();

    PER_HEAP
    BOOL short_on_end_of_seg (heap_segment* seg);

    PER_HEAP
    BOOL a_fit_free_list_p (int gen_number,
                            size_t size,
                            alloc_context* acontext,
                            uint32_t flags,
                            int align_const);

#ifdef BACKGROUND_GC
    PER_HEAP
    void wait_for_background (alloc_wait_reason awr, bool loh_p);

    PER_HEAP
    void wait_for_bgc_high_memory (alloc_wait_reason awr, bool loh_p);

    PER_HEAP
    void bgc_uoh_alloc_clr (uint8_t* alloc_start,
                            size_t size,
                            alloc_context* acontext,
                            uint32_t flags,
                            int gen_number,
                            int align_const,
                            int lock_index,
                            BOOL check_used_p,
                            heap_segment* seg);
#endif //BACKGROUND_GC

#ifdef BACKGROUND_GC
    PER_HEAP
    void bgc_track_uoh_alloc();

    PER_HEAP
    void bgc_untrack_uoh_alloc();

    PER_HEAP
    BOOL bgc_loh_allocate_spin();

    PER_HEAP
    BOOL bgc_poh_allocate_spin();
#endif //BACKGROUND_GC

#define max_saved_spinlock_info 48

#ifdef SPINLOCK_HISTORY
    PER_HEAP
    int spinlock_info_index;

    PER_HEAP
    spinlock_info last_spinlock_info[max_saved_spinlock_info + 8];
#endif //SPINLOCK_HISTORY

    PER_HEAP
    void add_saved_spinlock_info (
            bool loh_p,
            msl_enter_state enter_state,
            msl_take_state take_state);

    PER_HEAP
    void trigger_gc_for_alloc (int gen_number, gc_reason reason,
                               GCSpinLock* spin_lock, bool loh_p,
                               msl_take_state take_state);

    PER_HEAP
    BOOL a_fit_free_list_uoh_p (size_t size,
                                  alloc_context* acontext,
                                  uint32_t flags,
                                  int align_const,
                                  int gen_number);

    PER_HEAP
    BOOL a_fit_segment_end_p (int gen_number,
                              heap_segment* seg,
                              size_t size,
                              alloc_context* acontext,
                              uint32_t flags,
                              int align_const,
                              BOOL* commit_failed_p);
    PER_HEAP
    BOOL uoh_a_fit_segment_end_p (int gen_number,
                                  size_t size,
                                  alloc_context* acontext,
                                  uint32_t flags,
                                  int align_const,
                                  BOOL* commit_failed_p,
                                  oom_reason* oom_r);
    PER_HEAP
    BOOL uoh_get_new_seg (int gen_number,
                          size_t size,
                          BOOL* commit_failed_p,
                          oom_reason* oom_r);

    PER_HEAP_ISOLATED
    size_t get_uoh_seg_size (size_t size);

    PER_HEAP
    BOOL retry_full_compact_gc (size_t size);

    PER_HEAP
    BOOL check_and_wait_for_bgc (alloc_wait_reason awr,
                                 BOOL* did_full_compact_gc,
                                 bool loh_p);

    PER_HEAP
    BOOL trigger_full_compact_gc (gc_reason gr,
                                  oom_reason* oom_r,
                                  bool loh_p);

    PER_HEAP
    BOOL trigger_ephemeral_gc (gc_reason gr);

    PER_HEAP
    BOOL soh_try_fit (int gen_number,
                      size_t size,
                      alloc_context* acontext,
                      uint32_t flags,
                      int align_const,
                      BOOL* commit_failed_p,
                      BOOL* short_seg_end_p);
    PER_HEAP
    BOOL uoh_try_fit (int gen_number,
                      size_t size,
                      alloc_context* acontext,
                      uint32_t flags,
                      int align_const,
                      BOOL* commit_failed_p,
                      oom_reason* oom_r);

    PER_HEAP
    allocation_state allocate_soh (int gen_number,
                                     size_t size,
                                     alloc_context* acontext,
                                     uint32_t flags,
                                     int align_const);

#ifdef RECORD_LOH_STATE
    #define max_saved_loh_states 12
    PER_HEAP
    int loh_state_index;

    struct loh_state_info
    {
        allocation_state alloc_state;
        EEThreadId thread_id;
    };

    PER_HEAP
    loh_state_info last_loh_states[max_saved_loh_states];
    PER_HEAP
    void add_saved_loh_state (allocation_state loh_state_to_save, EEThreadId thread_id);
#endif //RECORD_LOH_STATE
    PER_HEAP
    allocation_state allocate_uoh (int gen_number,
                                     size_t size,
                                     alloc_context* acontext,
                                     uint32_t flags,
                                     int align_const);

    PER_HEAP_ISOLATED
    int init_semi_shared();
    PER_HEAP
    int init_gc_heap (int heap_number);
    PER_HEAP
    void self_destroy();
    PER_HEAP_ISOLATED
    void destroy_semi_shared();
    PER_HEAP
    void repair_allocation_contexts (BOOL repair_p);
    PER_HEAP
    void fix_allocation_contexts (BOOL for_gc_p);
    PER_HEAP
    void fix_youngest_allocation_area();
    PER_HEAP
    void fix_allocation_context (alloc_context* acontext, BOOL for_gc_p,
                                 BOOL record_ac_p);
    PER_HEAP
    void fix_older_allocation_area (generation* older_gen);
    PER_HEAP
    void set_allocation_heap_segment (generation* gen);
    PER_HEAP
    void reset_allocation_pointers (generation* gen, uint8_t* start);
    PER_HEAP
    int object_gennum (uint8_t* o);
    PER_HEAP
    int object_gennum_plan (uint8_t* o);
    PER_HEAP_ISOLATED
    void init_heap_segment (heap_segment* seg, gc_heap* hp
#ifdef USE_REGIONS
                            , uint8_t* start, size_t size, int gen_num
#endif //USE_REGIONS
                           );
    PER_HEAP
    void delete_heap_segment (heap_segment* seg, BOOL consider_hoarding=FALSE);
#ifdef FEATURE_BASICFREEZE
    PER_HEAP
    BOOL insert_ro_segment (heap_segment* seg);
    PER_HEAP
    void remove_ro_segment (heap_segment* seg);
#endif //FEATURE_BASICFREEZE
    PER_HEAP
    BOOL set_ro_segment_in_range (heap_segment* seg);
#ifndef USE_REGIONS
    PER_HEAP
    heap_segment* soh_get_segment_to_expand();
#endif //!USE_REGIONS
    PER_HEAP
    heap_segment* get_segment (size_t size, gc_oh_num oh);
    PER_HEAP_ISOLATED
    void release_segment (heap_segment* sg);
    PER_HEAP_ISOLATED
    void seg_mapping_table_add_segment (heap_segment* seg, gc_heap* hp);
    PER_HEAP_ISOLATED
    void seg_mapping_table_remove_segment (heap_segment* seg);
    PER_HEAP
    heap_segment* get_uoh_segment (int gen_number, size_t size, BOOL* did_full_compact_gc);
    PER_HEAP
    void thread_uoh_segment (int gen_number, heap_segment* new_seg);
    PER_HEAP_ISOLATED
    heap_segment* get_segment_for_uoh (int gen_number, size_t size
#ifdef MULTIPLE_HEAPS
                                      , gc_heap* hp
#endif //MULTIPLE_HEAPS
                                      );
    PER_HEAP
    void reset_heap_segment_pages (heap_segment* seg);
    PER_HEAP
    void decommit_heap_segment_pages (heap_segment* seg, size_t extra_space);
    PER_HEAP
    size_t decommit_ephemeral_segment_pages_step ();
    PER_HEAP
    size_t decommit_heap_segment_pages_worker (heap_segment* seg, uint8_t *new_committed);
    PER_HEAP_ISOLATED
    bool decommit_step ();
    PER_HEAP
    void decommit_heap_segment (heap_segment* seg);
    PER_HEAP_ISOLATED
    bool virtual_alloc_commit_for_heap (void* addr, size_t size, int h_number);
    PER_HEAP_ISOLATED
    bool virtual_commit (void* address, size_t size, gc_oh_num oh, int h_number=-1, bool* hard_limit_exceeded_p=NULL);
    PER_HEAP_ISOLATED
    bool virtual_decommit (void* address, size_t size, gc_oh_num oh, int h_number=-1);
    PER_HEAP_ISOLATED
    void virtual_free (void* add, size_t size, heap_segment* sg=NULL);
    PER_HEAP
    void clear_gen0_bricks();
#ifdef BACKGROUND_GC
    PER_HEAP
    void rearrange_small_heap_segments();
#endif //BACKGROUND_GC
    PER_HEAP
    void rearrange_uoh_segments();
#ifndef USE_REGIONS
    PER_HEAP
    void rearrange_heap_segments(BOOL compacting);
#endif //!USE_REGIONS
    PER_HEAP_ISOLATED
    void reset_write_watch_for_gc_heap(void* base_address, size_t region_size);
    PER_HEAP_ISOLATED
    void get_write_watch_for_gc_heap(bool reset, void *base_address, size_t region_size, void** dirty_pages, uintptr_t* dirty_page_count_ref, bool is_runtime_suspended);

    PER_HEAP
    void switch_one_quantum();
    PER_HEAP
    void reset_ww_by_chunk (uint8_t* start_address, size_t total_reset_size);
    PER_HEAP
    void switch_on_reset (BOOL concurrent_p, size_t* current_total_reset_size, size_t last_reset_size);
    PER_HEAP
    void reset_write_watch (BOOL concurrent_p);
    PER_HEAP
    void adjust_ephemeral_limits();
    PER_HEAP
    void make_generation (int gen_num, heap_segment* seg, uint8_t* start);

#define USE_PADDING_FRONT 1
#define USE_PADDING_TAIL  2

    PER_HEAP
    BOOL size_fit_p (size_t size REQD_ALIGN_AND_OFFSET_DCL, uint8_t* alloc_pointer, uint8_t* alloc_limit,
                     uint8_t* old_loc=0, int use_padding=USE_PADDING_TAIL);
    PER_HEAP
    BOOL a_size_fit_p (size_t size, uint8_t* alloc_pointer, uint8_t* alloc_limit,
                       int align_const);

    PER_HEAP
    void handle_oom (oom_reason reason, size_t alloc_size,
                     uint8_t* allocated, uint8_t* reserved);

    PER_HEAP
    size_t card_of ( uint8_t* object);
    PER_HEAP
    uint8_t* brick_address (size_t brick);
    PER_HEAP
    size_t brick_of (uint8_t* add);
    PER_HEAP
    uint8_t* card_address (size_t card);
    PER_HEAP
    void clear_card (size_t card);
    PER_HEAP
    void set_card (size_t card);
    PER_HEAP
    BOOL  card_set_p (size_t card);
    PER_HEAP
    void card_table_set_bit (uint8_t* location);

#ifdef CARD_BUNDLE
    PER_HEAP
    void update_card_table_bundle();
    PER_HEAP
    void reset_card_table_write_watch();
    PER_HEAP
    void card_bundle_clear(size_t cardb);
    PER_HEAP
    void card_bundle_set (size_t cardb);
    PER_HEAP
    void card_bundles_set (size_t start_cardb, size_t end_cardb);
    PER_HEAP
    void verify_card_bundle_bits_set(size_t first_card_word, size_t last_card_word);
    PER_HEAP
    void verify_card_bundles();
    PER_HEAP
    BOOL card_bundle_set_p (size_t cardb);
    PER_HEAP
    BOOL find_card_dword (size_t& cardw, size_t cardw_end);
    PER_HEAP
    void enable_card_bundles();
    PER_HEAP_ISOLATED
    BOOL card_bundles_enabled();

#endif //CARD_BUNDLE

    PER_HEAP
    BOOL find_card (uint32_t* card_table, size_t& card,
                    size_t card_word_end, size_t& end_card);
    PER_HEAP
    BOOL grow_heap_segment (heap_segment* seg, uint8_t* high_address, bool* hard_limit_exceeded_p=NULL);
    PER_HEAP
    int grow_heap_segment (heap_segment* seg, uint8_t* high_address, uint8_t* old_loc, size_t size, BOOL pad_front_p REQD_ALIGN_AND_OFFSET_DCL);
    PER_HEAP
    void copy_brick_card_range (uint8_t* la, uint32_t* old_card_table,
                                short* old_brick_table,
                                uint8_t* start, uint8_t* end);
    PER_HEAP
    void copy_brick_card_table();
    PER_HEAP
    void clear_brick_table (uint8_t* from, uint8_t* end);
    PER_HEAP
    void set_brick (size_t index, ptrdiff_t val);
    PER_HEAP
    int get_brick_entry (size_t index);
#ifdef BACKGROUND_GC
    PER_HEAP
    unsigned int mark_array_marked (uint8_t* add);
    PER_HEAP
    void mark_array_set_marked (uint8_t* add);
    PER_HEAP
    BOOL is_mark_bit_set (uint8_t* add);
    PER_HEAP
    void gmark_array_set_marked (uint8_t* add);
    PER_HEAP
    void set_mark_array_bit (size_t mark_bit);
    PER_HEAP
    BOOL mark_array_bit_set (size_t mark_bit);
    PER_HEAP
    void mark_array_clear_marked (uint8_t* add);
    PER_HEAP
    void clear_mark_array (uint8_t* from, uint8_t* end, BOOL check_only=TRUE
#ifdef FEATURE_BASICFREEZE
        , BOOL read_only=FALSE
#endif // FEATURE_BASICFREEZE
        );
    PER_HEAP
    void seg_clear_mark_array_bits_soh (heap_segment* seg);
    PER_HEAP
    void clear_batch_mark_array_bits (uint8_t* start, uint8_t* end);
    PER_HEAP
    void bgc_clear_batch_mark_array_bits (uint8_t* start, uint8_t* end);
#ifdef VERIFY_HEAP
    PER_HEAP
    void set_batch_mark_array_bits (uint8_t* start, uint8_t* end);
    PER_HEAP
    void check_batch_mark_array_bits (uint8_t* start, uint8_t* end);
#endif //VERIFY_HEAP
#endif //BACKGROUND_GC

    PER_HEAP
    BOOL uoh_object_marked (uint8_t* o, BOOL clearp);

#ifdef BACKGROUND_GC
    PER_HEAP
    BOOL background_allowed_p();
#endif //BACKGROUND_GC

    PER_HEAP_ISOLATED
    void send_full_gc_notification (int gen_num, BOOL due_to_alloc_p);

    PER_HEAP
    void check_for_full_gc (int gen_num, size_t size);

    PER_HEAP
    void adjust_limit (uint8_t* start, size_t limit_size, generation* gen);
    PER_HEAP
    void adjust_limit_clr (uint8_t* start, size_t limit_size, size_t size,
                           alloc_context* acontext, uint32_t flags, heap_segment* seg,
                           int align_const, int gen_number);
    PER_HEAP
    void  leave_allocation_segment (generation* gen);

    PER_HEAP
    void init_free_and_plug();

    PER_HEAP
    void print_free_and_plug (const char* msg);

    PER_HEAP
    void add_gen_plug (int gen_number, size_t plug_size);

    PER_HEAP_ISOLATED
    int find_bucket (size_t size);

    PER_HEAP
    void add_gen_free (int gen_number, size_t free_size);

    PER_HEAP
    void add_gen_plug_allocated_in_free (int gen_number, size_t plug_size);

    PER_HEAP
    void add_item_to_current_pinned_free (int gen_number, size_t free_size);

    PER_HEAP
    void remove_gen_free (int gen_number, size_t free_size);

    PER_HEAP
    void thread_free_item_front (generation* gen, uint8_t* free_start, size_t free_size);

#ifdef DOUBLY_LINKED_FL
    PER_HEAP
    void thread_item_front_added (generation* gen, uint8_t* free_start, size_t free_size);
#endif //DOUBLY_LINKED_FL

    PER_HEAP
    void make_free_obj (generation* gen, uint8_t* free_start, size_t free_size);

    PER_HEAP
    uint8_t* allocate_in_older_generation (generation* gen, size_t size,
                                        int from_gen_number,
                                        uint8_t* old_loc=0
                                        REQD_ALIGN_AND_OFFSET_DEFAULT_DCL);

    PER_HEAP
    void init_alloc_info (generation* gen, heap_segment* seg);

    PER_HEAP
    heap_segment* get_next_alloc_seg (generation* gen);

#ifndef USE_REGIONS
    PER_HEAP
    generation*  ensure_ephemeral_heap_segment (generation* consing_gen);
#endif //!USE_REGIONS
    PER_HEAP
    uint8_t* allocate_in_condemned_generations (generation* gen,
                                             size_t size,
                                             int from_gen_number,
#ifdef SHORT_PLUGS
                                             BOOL* convert_to_pinned_p=NULL,
                                             uint8_t* next_pinned_plug=0,
                                             heap_segment* current_seg=0,
#endif //SHORT_PLUGS
                                             uint8_t* old_loc=0
                                             REQD_ALIGN_AND_OFFSET_DEFAULT_DCL);
    // Verifies that interior is actually in the range of seg; otherwise
    // returns 0.
    PER_HEAP_ISOLATED
    heap_segment* find_segment (uint8_t* interior, BOOL small_segment_only_p);

    PER_HEAP_ISOLATED
    gc_heap* heap_of (uint8_t* object);

    PER_HEAP_ISOLATED
    gc_heap* heap_of_gc (uint8_t* object);

    PER_HEAP
    size_t get_promoted_bytes();

#ifdef USE_REGIONS
    PER_HEAP_ISOLATED
    void sync_promoted_bytes();
#endif //USE_REGIONS

#if !defined(USE_REGIONS) || defined(_DEBUG)
    PER_HEAP
    void init_promoted_bytes();
    PER_HEAP
    size_t& promoted_bytes (int thread);
#endif //!USE_REGIONS || _DEBUG

    // Thread is only used by segments. It should really be the same as heap_number.
    PER_HEAP
    void add_to_promoted_bytes (uint8_t* object, int thread);

    PER_HEAP
    void add_to_promoted_bytes (uint8_t* object, size_t obj_size, int thread);

    PER_HEAP
    uint8_t* find_object (uint8_t* o);

    PER_HEAP
    dynamic_data* dynamic_data_of (int gen_number);
    PER_HEAP
    ptrdiff_t  get_desired_allocation (int gen_number);
    PER_HEAP
    ptrdiff_t  get_new_allocation (int gen_number);
    PER_HEAP
    ptrdiff_t  get_allocation (int gen_number);
    PER_HEAP
    bool new_allocation_allowed (int gen_number);
#ifdef BACKGROUND_GC
    PER_HEAP_ISOLATED
    void allow_new_allocation (int gen_number);
    PER_HEAP_ISOLATED
    void disallow_new_allocation (int gen_number);
#endif //BACKGROUND_GC
    PER_HEAP
    void reset_pinned_queue();
    PER_HEAP
    void reset_pinned_queue_bos();
    PER_HEAP
    void set_allocator_next_pin (generation* gen);
    PER_HEAP
    void enque_pinned_plug (generation* gen, uint8_t* plug, size_t len);
    PER_HEAP
    void enque_pinned_plug (uint8_t* plug,
                            BOOL save_pre_plug_info_p,
                            uint8_t* last_object_in_last_plug);
    PER_HEAP
    void merge_with_last_pinned_plug (uint8_t* last_pinned_plug, size_t plug_size);
    PER_HEAP
    void set_pinned_info (uint8_t* last_pinned_plug, size_t plug_len, generation* gen);
    PER_HEAP
    void save_post_plug_info (uint8_t* last_pinned_plug, uint8_t* last_object_in_last_plug, uint8_t* post_plug);
    PER_HEAP
    size_t deque_pinned_plug ();
    PER_HEAP
    mark* pinned_plug_of (size_t bos);
    PER_HEAP
    mark* oldest_pin ();
    PER_HEAP
    mark* before_oldest_pin();
    PER_HEAP
    BOOL pinned_plug_que_empty_p ();
    PER_HEAP
    void make_mark_stack (mark* arr);
#ifdef MH_SC_MARK
    PER_HEAP
    int& mark_stack_busy();
    PER_HEAP
    VOLATILE(uint8_t*)& ref_mark_stack (gc_heap* hp, int index);
#endif
#ifdef BACKGROUND_GC
    PER_HEAP_ISOLATED
    size_t&  bpromoted_bytes (int);
    PER_HEAP
    void make_background_mark_stack (uint8_t** arr);
    PER_HEAP
    void make_c_mark_list (uint8_t** arr);
#endif //BACKGROUND_GC
    PER_HEAP
    generation* generation_of (int  n);
    PER_HEAP
    BOOL gc_mark1 (uint8_t* o);
    PER_HEAP
    BOOL gc_mark (uint8_t* o, uint8_t* low, uint8_t* high, int condemned_gen);
    PER_HEAP
    void mark_object (uint8_t* o THREAD_NUMBER_DCL);
#ifdef HEAP_ANALYZE
    PER_HEAP
    void ha_mark_object_simple (uint8_t** o THREAD_NUMBER_DCL);
#endif //HEAP_ANALYZE
    PER_HEAP
    void mark_object_simple (uint8_t** o THREAD_NUMBER_DCL);
    PER_HEAP
    void mark_object_simple1 (uint8_t* o, uint8_t* start THREAD_NUMBER_DCL);

#ifdef MH_SC_MARK
    PER_HEAP
    void mark_steal ();
#endif //MH_SC_MARK

#ifdef BACKGROUND_GC
    PER_HEAP
    BOOL background_marked (uint8_t* o);
    PER_HEAP
    BOOL background_mark1 (uint8_t* o);
    PER_HEAP
    BOOL background_mark (uint8_t* o, uint8_t* low, uint8_t* high);
    PER_HEAP
    uint8_t* background_mark_object (uint8_t* o THREAD_NUMBER_DCL);
    PER_HEAP
    void background_mark_simple (uint8_t* o THREAD_NUMBER_DCL);
    PER_HEAP
    void background_mark_simple1 (uint8_t* o THREAD_NUMBER_DCL);
    PER_HEAP_ISOLATED
    void background_promote (Object**, ScanContext* , uint32_t);
    PER_HEAP
    BOOL background_object_marked (uint8_t* o, BOOL clearp);
    PER_HEAP
    void init_background_gc();
    PER_HEAP
    uint8_t* background_next_end (heap_segment*, BOOL);
    // while we are in LOH sweep we can't modify the segment list
    // there so we mark them as to be deleted and deleted them
    // at the next chance we get.
    PER_HEAP
    void background_delay_delete_uoh_segments();
    PER_HEAP
    void generation_delete_heap_segment (generation*,
                                         heap_segment*, heap_segment*, heap_segment*);
    PER_HEAP
    void set_mem_verify (uint8_t*, uint8_t*, uint8_t);
    PER_HEAP
    void process_background_segment_end (heap_segment*, generation*, uint8_t*,
                                         heap_segment*, BOOL* delete_p,
                                         size_t free_obj_size_last_gap);

    PER_HEAP
    BOOL fgc_should_consider_object (uint8_t* o,
                                     heap_segment* seg,
                                     BOOL consider_bgc_mark_p,
                                     BOOL check_current_sweep_p,
                                     BOOL check_saved_sweep_p);
    PER_HEAP
    void should_check_bgc_mark (heap_segment* seg,
                                BOOL* consider_bgc_mark_p,
                                BOOL* check_current_sweep_p,
                                BOOL* check_saved_sweep_p);

#ifdef DOUBLY_LINKED_FL
    PER_HEAP
    BOOL should_set_bgc_mark_bit (uint8_t* o);
#endif //DOUBLY_LINKED_FL

    PER_HEAP
    void background_ephemeral_sweep();
    PER_HEAP
    void background_sweep ();
    // Check if we should grow the mark stack proactively to avoid mark stack
    // overflow and grow if necessary.
    PER_HEAP
    void check_bgc_mark_stack_length();
    PER_HEAP
    void grow_bgc_mark_stack (size_t new_size);
    PER_HEAP
    uint8_t* background_seg_end (heap_segment* seg, BOOL concurrent_p);
    PER_HEAP
    uint8_t* background_first_overflow (uint8_t* min_add,
                                     heap_segment* seg,
                                     BOOL concurrent_p,
                                     BOOL small_object_p);
    PER_HEAP
    void background_process_mark_overflow_internal (uint8_t* min_add, uint8_t* max_add,
                                                    BOOL concurrent_p);
    PER_HEAP
    BOOL background_process_mark_overflow (BOOL concurrent_p);

    // for foreground GC to get hold of background structures containing refs
    PER_HEAP
    void
    scan_background_roots (promote_func* fn, int hn, ScanContext *pSC);

    PER_HEAP
    BOOL bgc_mark_array_range (heap_segment* seg,
                               BOOL whole_seg_p,
                               uint8_t** range_beg,
                               uint8_t** range_end);
    PER_HEAP
    void bgc_verify_mark_array_cleared (heap_segment* seg);
    PER_HEAP
    void verify_mark_array_cleared();
    PER_HEAP
    void verify_partial();
    PER_HEAP
    void verify_mark_bits_cleared (uint8_t* obj, size_t s);
    PER_HEAP
    void clear_all_mark_array();

#ifdef BGC_SERVO_TUNING

    // Currently BGC servo tuning is an experimental feature.
    class bgc_tuning
    {
    public:
        struct tuning_calculation
        {
            // We use this virtual size that represents the generation
            // size at goal. We calculate the flr based on this.
            size_t end_gen_size_goal;

            // sweep goal is expressed as flr as we want to avoid
            // expanding the gen size.
            double sweep_flr_goal;

            // gen2 size at the end of last bgc.
            size_t last_bgc_size;

            //
            // these need to be double so we don't loose so much accurancy
            // they are *100.0
            //
            // the FL ratio at the start of current bgc sweep.
            double current_bgc_sweep_flr;
            // the FL ratio at the end of last bgc.
            // Only used for FF.
            double last_bgc_flr;
            // the FL ratio last time we started a bgc
            double current_bgc_start_flr;

            double above_goal_accu_error;

            // We will trigger the next BGC if this much
            // alloc has been consumed between the last
            // bgc end and now.
            size_t alloc_to_trigger;
            // actual consumed alloc
            size_t actual_alloc_to_trigger;

            // the alloc between last bgc sweep start and end.
            size_t last_bgc_end_alloc;

            //
            // For smoothing calc
            //
            size_t smoothed_alloc_to_trigger;

            //
            // For TBH
            //
            // last time we checked, were we above sweep flr goal?
            bool last_sweep_above_p;
            size_t alloc_to_trigger_0;

            // This is to get us started. It's set when we observe in a gen1
            // GC when the memory load is high enough and is used to seed the first
            // BGC triggered due to this tuning.
            size_t first_alloc_to_trigger;
        };

        struct tuning_stats
        {
            size_t last_bgc_physical_size;

            size_t last_alloc_end_to_start;
            size_t last_alloc_start_to_sweep;
            size_t last_alloc_sweep_to_end;
            // records the alloc at the last significant point,
            // used to calculate the 3 alloc's above.
            // It's reset at bgc sweep start as that's when we reset
            // all the allocation data (sweep_allocated/condemned_allocated/etc)
            size_t last_alloc;

            // the FL size at the end of last bgc.
            size_t last_bgc_fl_size;

            // last gen2 surv rate
            double last_bgc_surv_rate;

            // the FL ratio last time gen size increased.
            double last_gen_increase_flr;
        };

        // This is just so that I don't need to calculate things multiple
        // times. Only used during bgc end calculations. Everything that
        // needs to be perserved across GCs will be saved in the other 2
        // structs.
        struct bgc_size_data
        {
            size_t gen_size;
            size_t gen_physical_size;
            size_t gen_fl_size;
            // The actual physical fl size, unadjusted
            size_t gen_actual_phys_fl_size;
            // I call this physical_fl but really it's adjusted based on alloc
            // that we haven't consumed because the other generation consumed
            // its alloc and triggered the BGC. See init_bgc_end_data.
            // We don't allow it to go negative.
            ptrdiff_t gen_physical_fl_size;
            double gen_physical_flr;
            double gen_flr;
        };

        static bool enable_fl_tuning;
        // the memory load we aim to maintain.
        static uint32_t memory_load_goal;

        // if we are BGCMemGoalSlack above BGCMemGoal, this is where we
        // panic and start to see if we should do NGC2.
        static uint32_t memory_load_goal_slack;
        // This is calculated based on memory_load_goal.
        static uint64_t available_memory_goal;
        // If we are above (ml goal + slack), we need to panic.
        // Currently we just trigger the next GC as an NGC2, but
        // we do track the accumulated error and could be more
        // sophisticated about triggering NGC2 especially when
        // slack is small. We could say unless we see the error
        // is large enough would we actually trigger an NGC2.
        static bool panic_activated_p;
        static double accu_error_panic;

        static double above_goal_kp;
        static double above_goal_ki;
        static bool enable_ki;
        static bool enable_kd;
        static bool enable_smooth;
        static bool enable_tbh;
        static bool enable_ff;
        static bool enable_gradual_d;
        static double above_goal_kd;
        static double above_goal_ff;
        static double num_gen1s_smooth_factor;

        // for ML servo loop
        static double ml_kp;
        static double ml_ki;

        // for ML loop ki
        static double accu_error;

        // did we start tuning with FL yet?
        static bool fl_tuning_triggered;

        // ==================================================
        // ============what's used in calculation============
        // ==================================================
        //
        // only used in smoothing.
        static size_t num_bgcs_since_tuning_trigger;

        // gen1 GC setting the next GC as a BGC when it observes the
        // memory load is high enough for the first time.
        static bool next_bgc_p;

        // this is organized as:
        // element 0 is for max_generation
        // element 1 is for max_generation+1
        static tuning_calculation gen_calc[2];

        // ======================================================
        // ============what's used to only show stats============
        // ======================================================
        //
        // how many gen1's actually happened before triggering next bgc.
        static size_t actual_num_gen1s_to_trigger;

        static size_t gen1_index_last_bgc_end;
        static size_t gen1_index_last_bgc_start;
        static size_t gen1_index_last_bgc_sweep;

        static tuning_stats gen_stats[2];
        // ============end of stats============

        static bgc_size_data current_bgc_end_data[2];

        static size_t last_stepping_bgc_count;
        static uint32_t last_stepping_mem_load;
        static uint32_t stepping_interval;

        // When we are in the initial stage before fl tuning is triggered.
        static bool use_stepping_trigger_p;

        // the gen2 correction factor is used to put more emphasis
        // on the gen2 when it triggered the BGC.
        // If the BGC was triggered due to gen3, we decrease this
        // factor.
        static double gen2_ratio_correction;
        static double ratio_correction_step;

        // Since we have 2 loops, this BGC was caused by one of them; for the other loop we know
        // we didn't reach the goal so use the output from last time.
        static void calculate_tuning (int gen_number, bool use_this_loop_p);

        static void init_bgc_end_data (int gen_number, bool use_this_loop_p);
        static void calc_end_bgc_fl (int gen_number);

        static void convert_to_fl (bool use_gen2_loop_p, bool use_gen3_loop_p);
        static double calculate_ml_tuning (uint64_t current_available_physical, bool reduce_p, ptrdiff_t* _vfl_from_kp, ptrdiff_t* _vfl_from_ki);

        // This invokes the ml tuning loop and sets the total gen sizes, ie
        // including vfl.
        static void set_total_gen_sizes (bool use_gen2_loop_p, bool use_gen3_loop_p);

        static bool should_trigger_bgc_loh();

        // This is only called when we've already stopped for GC.
        // For LOH we'd be doing this in the alloc path.
        static bool should_trigger_bgc();

        // If we keep being above ml goal, we need to compact.
        static bool should_trigger_ngc2();

        // Only implemented for gen2 now while we are in sweep.
        // Before we could build up enough fl, we delay gen1 consuming
        // gen2 alloc so we don't get into panic.
        // When we maintain the fl instead of building a new one, this
        // can be eliminated.
        static bool should_delay_alloc (int gen_number);

        // When we are under the memory load goal, we'd like to do 10 BGCs
        // before we reach the goal.
        static bool stepping_trigger (uint32_t current_memory_load, size_t current_gen2_count);

        static void update_bgc_start (int gen_number, size_t num_gen1s_since_end);
        // Updates the following:
        // current_bgc_start_flr
        // actual_alloc_to_trigger
        // last_alloc_end_to_start
        // last_alloc
        // actual_num_gen1s_to_trigger
        // gen1_index_last_bgc_start
        static void record_bgc_start();

        static void update_bgc_sweep_start (int gen_number, size_t num_gen1s_since_start);
        // Updates the following:
        // current_bgc_sweep_flr
        // last_alloc_start_to_sweep
        // last_alloc
        // gen1_index_last_bgc_sweep
        static void record_bgc_sweep_start();
        // Updates the rest
        static void record_and_adjust_bgc_end();
    };

    // This tells us why we chose to do a bgc in tuning.
    PER_HEAP_ISOLATED
    int saved_bgc_tuning_reason;
#endif //BGC_SERVO_TUNING

#endif //BACKGROUND_GC

    PER_HEAP
    void mark_through_object (uint8_t* oo, BOOL mark_class_object_p THREAD_NUMBER_DCL);
    PER_HEAP
    BOOL process_mark_overflow (int condemned_gen_number);
    PER_HEAP
    void process_mark_overflow_internal (int condemned_gen_number,
                                         uint8_t* min_address, uint8_t* max_address);

#ifdef SNOOP_STATS
    PER_HEAP
    void print_snoop_stat();
#endif //SNOOP_STATS

#ifdef MH_SC_MARK
    PER_HEAP
    BOOL check_next_mark_stack (gc_heap* next_heap);
#endif //MH_SC_MARK

    PER_HEAP
    void scan_dependent_handles (int condemned_gen_number, ScanContext *sc, BOOL initial_scan_p);

    PER_HEAP
    size_t get_generation_start_size (int gen_number);

    PER_HEAP_ISOLATED
    int get_num_heaps();

    PER_HEAP
    BOOL decide_on_promotion_surv (size_t threshold);

    PER_HEAP
    void mark_phase (int condemned_gen_number, BOOL mark_only_p);

    PER_HEAP
    void pin_object (uint8_t* o, uint8_t** ppObject);

    PER_HEAP_ISOLATED
    size_t get_total_pinned_objects();

    PER_HEAP_ISOLATED
    void reinit_pinned_objects();

    PER_HEAP
    void reset_mark_stack ();
    PER_HEAP
    uint8_t* insert_node (uint8_t* new_node, size_t sequence_number,
                       uint8_t* tree, uint8_t* last_node);
    PER_HEAP
    size_t update_brick_table (uint8_t* tree, size_t current_brick,
                               uint8_t* x, uint8_t* plug_end);

#ifndef USE_REGIONS
    PER_HEAP
    void plan_generation_start (generation* gen, generation* consing_gen, uint8_t* next_plug_to_allocate);

    PER_HEAP
    void realloc_plan_generation_start (generation* gen, generation* consing_gen);

    PER_HEAP
    void plan_generation_starts (generation*& consing_gen);

    PER_HEAP
    void advance_pins_for_demotion (generation* gen);

    PER_HEAP
    void process_ephemeral_boundaries(uint8_t* x, int& active_new_gen_number,
                                      int& active_old_gen_number,
                                      generation*& consing_gen,
                                      BOOL& allocate_in_condemned);
#endif //!USE_REGIONS

    PER_HEAP
    void seg_clear_mark_bits (heap_segment* seg);
    PER_HEAP
    void sweep_ro_segments (heap_segment* start_seg);
    PER_HEAP
    void convert_to_pinned_plug (BOOL& last_npinned_plug_p,
                                 BOOL& last_pinned_plug_p,
                                 BOOL& pinned_plug_p,
                                 size_t ps,
                                 size_t& artificial_pinned_size);
    PER_HEAP
    void store_plug_gap_info (uint8_t* plug_start,
                              uint8_t* plug_end,
                              BOOL& last_npinned_plug_p,
                              BOOL& last_pinned_plug_p,
                              uint8_t*& last_pinned_plug,
                              BOOL& pinned_plug_p,
                              uint8_t* last_object_in_last_plug,
                              BOOL& merge_with_last_pin_p,
                              // this is only for verification purpose
                              size_t last_plug_len);
    PER_HEAP
    void plan_phase (int condemned_gen_number);

    PER_HEAP
    void add_alloc_in_condemned_bucket (size_t plug_size);

    PER_HEAP
    uint8_t* find_next_marked (uint8_t* x, uint8_t* end,
                               BOOL use_mark_list, 
                               uint8_t**& mark_list_next,
                               uint8_t** mark_list_index);


    PER_HEAP
    void record_interesting_data_point (interesting_data_point idp);

#ifdef GC_CONFIG_DRIVEN
    PER_HEAP
    void record_interesting_info_per_heap();
    PER_HEAP_ISOLATED
    void record_global_mechanisms();
    PER_HEAP_ISOLATED
    BOOL should_do_sweeping_gc (BOOL compact_p);
#endif //GC_CONFIG_DRIVEN

#ifdef FEATURE_LOH_COMPACTION
    // plan_loh can allocate memory so it can fail. If it fails, we will
    // fall back to sweeping.
    PER_HEAP
    BOOL plan_loh();

    PER_HEAP
    void compact_loh();

    PER_HEAP
    void relocate_in_loh_compact();

    PER_HEAP
    void walk_relocation_for_loh (void* profiling_context, record_surv_fn fn);

    PER_HEAP
    BOOL loh_enque_pinned_plug (uint8_t* plug, size_t len);

    PER_HEAP
    void loh_set_allocator_next_pin();

    PER_HEAP
    BOOL loh_pinned_plug_que_empty_p();

    PER_HEAP
    size_t loh_deque_pinned_plug();

    PER_HEAP
    mark* loh_pinned_plug_of (size_t bos);

    PER_HEAP
    mark* loh_oldest_pin();

    PER_HEAP
    BOOL loh_size_fit_p (size_t size, uint8_t* alloc_pointer, uint8_t* alloc_limit);

    PER_HEAP
    uint8_t* loh_allocate_in_condemned (size_t size);

    PER_HEAP_ISOLATED
    BOOL loh_object_p (uint8_t* o);

    PER_HEAP_ISOLATED
    BOOL loh_compaction_requested();

    // If the LOH compaction mode is just to compact once,
    // we need to see if we should reset it back to not compact.
    // We would only reset if every heap's LOH was compacted.
    PER_HEAP_ISOLATED
    void check_loh_compact_mode  (BOOL all_heaps_compacted_p);
#endif //FEATURE_LOH_COMPACTION

    PER_HEAP
    void fix_generation_bounds (int condemned_gen_number,
                                generation* consing_gen);
#ifndef USE_REGIONS
    PER_HEAP
    uint8_t* generation_limit (int gen_number);
#endif //!USE_REGIONS

    struct make_free_args
    {
        int free_list_gen_number;
#ifndef USE_REGIONS
        uint8_t* current_gen_limit;
#endif //USE_REGIONS
        generation* free_list_gen;
        uint8_t* highest_plug;
    };
    PER_HEAP
    uint8_t* allocate_at_end (size_t size);
    PER_HEAP
    BOOL ensure_gap_allocation (int condemned_gen_number);
    // make_free_lists is only called by blocking GCs.
    PER_HEAP
    void make_free_lists (int condemned_gen_number);
    PER_HEAP
    void make_free_list_in_brick (uint8_t* tree, make_free_args* args);
    PER_HEAP
    void thread_gap (uint8_t* gap_start, size_t size, generation*  gen);
    PER_HEAP
    void uoh_thread_gap_front (uint8_t* gap_start, size_t size, generation*  gen);
    PER_HEAP
    void make_unused_array (uint8_t* x, size_t size, BOOL clearp=FALSE, BOOL resetp=FALSE);
    PER_HEAP
    void clear_unused_array (uint8_t* x, size_t size);
    PER_HEAP
    void relocate_address (uint8_t** old_address THREAD_NUMBER_DCL);
    struct relocate_args
    {
        uint8_t* last_plug;
        BOOL is_shortened;
        mark* pinned_plug_entry;
    };

    PER_HEAP
    void reloc_survivor_helper (uint8_t** pval);
    PER_HEAP
    void check_class_object_demotion (uint8_t* obj);
    PER_HEAP
    void check_class_object_demotion_internal (uint8_t* obj);

    PER_HEAP
    void check_demotion_helper (uint8_t** pval, uint8_t* parent_obj);

    PER_HEAP
    void relocate_survivor_helper (uint8_t* plug, uint8_t* plug_end);

    PER_HEAP
    void verify_pins_with_post_plug_info (const char* msg);

#ifdef COLLECTIBLE_CLASS
    PER_HEAP
    void unconditional_set_card_collectible (uint8_t* obj);
#endif //COLLECTIBLE_CLASS

    PER_HEAP
    void relocate_shortened_survivor_helper (uint8_t* plug, uint8_t* plug_end, mark* pinned_plug_entry);

    PER_HEAP
    void relocate_obj_helper (uint8_t* x, size_t s);

    PER_HEAP
    void reloc_ref_in_shortened_obj (uint8_t** address_to_set_card, uint8_t** address_to_reloc);

    PER_HEAP
    void relocate_pre_plug_info (mark* pinned_plug_entry);

    PER_HEAP
    void relocate_shortened_obj_helper (uint8_t* x, size_t s, uint8_t* end, mark* pinned_plug_entry, BOOL is_pinned);

    PER_HEAP
    void relocate_survivors_in_plug (uint8_t* plug, uint8_t* plug_end,
                                     BOOL check_last_object_p,
                                     mark* pinned_plug_entry);
    PER_HEAP
    void relocate_survivors_in_brick (uint8_t* tree, relocate_args* args);

    PER_HEAP
    void update_oldest_pinned_plug();

    PER_HEAP
    heap_segment* get_start_segment (generation* gen);

    PER_HEAP
    void relocate_survivors (int condemned_gen_number,
                             uint8_t* first_condemned_address );
    PER_HEAP
    void relocate_phase (int condemned_gen_number,
                         uint8_t* first_condemned_address);

    struct compact_args
    {
        BOOL copy_cards_p;
        uint8_t* last_plug;
        ptrdiff_t last_plug_relocation;
        uint8_t* before_last_plug;
        size_t current_compacted_brick;
        BOOL is_shortened;
        mark* pinned_plug_entry;
        BOOL check_gennum_p;
        int src_gennum;

        void print()
        {
            dprintf (3, ("last plug: %Ix, last plug reloc: %Ix, before last: %Ix, b: %Ix",
                last_plug, last_plug_relocation, before_last_plug, current_compacted_brick));
        }
    };

    PER_HEAP
    void copy_cards_range (uint8_t* dest, uint8_t* src, size_t len, BOOL copy_cards_p);
    PER_HEAP
    void  gcmemcopy (uint8_t* dest, uint8_t* src, size_t len, BOOL copy_cards_p);
    PER_HEAP
    void compact_plug (uint8_t* plug, size_t size, BOOL check_last_object_p, compact_args* args);
    PER_HEAP
    void compact_in_brick (uint8_t* tree, compact_args* args);

    PER_HEAP
    mark* get_next_pinned_entry (uint8_t* tree,
                                 BOOL* has_pre_plug_info_p,
                                 BOOL* has_post_plug_info_p,
                                 BOOL deque_p=TRUE);

    PER_HEAP
    mark* get_oldest_pinned_entry (BOOL* has_pre_plug_info_p, BOOL* has_post_plug_info_p);

    PER_HEAP
    size_t recover_saved_pinned_info();

    PER_HEAP
    void compact_phase (int condemned_gen_number, uint8_t*
                        first_condemned_address, BOOL clear_cards);
    PER_HEAP
    void clear_cards (size_t start_card, size_t end_card);
    PER_HEAP
    void clear_card_for_addresses (uint8_t* start_address, uint8_t* end_address);
    PER_HEAP
    void copy_cards (size_t dst_card, size_t src_card,
                     size_t end_card, BOOL nextp);
    PER_HEAP
    void copy_cards_for_addresses (uint8_t* dest, uint8_t* src, size_t len);

#ifdef BACKGROUND_GC
    PER_HEAP
    void copy_mark_bits (size_t dst_mark_bit, size_t src_mark_bit, size_t end_mark_bit);
    PER_HEAP
    void copy_mark_bits_for_addresses (uint8_t* dest, uint8_t* src, size_t len);
#endif //BACKGROUND_GC

#ifdef USE_REGIONS
    // o is guaranteed to be in the heap range.
    PER_HEAP_ISOLATED
    bool is_in_condemned_gc (uint8_t* o);
    // requires checking if o is in the heap range first.
    PER_HEAP_ISOLATED
    bool is_in_condemned (uint8_t* o);
    PER_HEAP_ISOLATED
    bool should_check_brick_for_reloc (uint8_t* o);
#endif //USE_REGIONS
    PER_HEAP
    BOOL ephemeral_pointer_p (uint8_t* o);
    PER_HEAP
    void fix_brick_to_highest (uint8_t* o, uint8_t* next_o);
    PER_HEAP
    uint8_t* find_first_object (uint8_t* start_address, uint8_t* first_object);
#ifndef USE_REGIONS
    PER_HEAP
    uint8_t* compute_next_boundary (int gen_number, BOOL relocating);
#endif //!USE_REGIONS
    PER_HEAP
    void mark_through_cards_helper (uint8_t** poo, size_t& ngen,
                                    size_t& cg_pointers_found,
                                    card_fn fn, uint8_t* nhigh,
                                    uint8_t* next_boundary,
                                    int condemned_gen,
                                    int current_gen
                                    CARD_MARKING_STEALING_ARG(gc_heap* hpt));
    PER_HEAP
    BOOL card_transition (uint8_t* po, uint8_t* end, size_t card_word_end,
                          size_t& cg_pointers_found,
                          size_t& n_eph, size_t& n_card_set,
                          size_t& card, size_t& end_card,
                          BOOL& foundp, uint8_t*& start_address,
                          uint8_t*& limit, size_t& n_cards_cleared
                          CARD_MARKING_STEALING_ARGS(card_marking_enumerator& card_mark_enumerator, heap_segment* seg, size_t& card_word_end_out));
    PER_HEAP
    void mark_through_cards_for_segments(card_fn fn, BOOL relocating CARD_MARKING_STEALING_ARG(gc_heap* hpt));

#ifndef USE_REGIONS
    PER_HEAP
    void repair_allocation_in_expanded_heap (generation* gen);
    PER_HEAP
    BOOL can_fit_in_spaces_p (size_t* ordered_blocks, int small_index, size_t* ordered_spaces, int big_index);
    PER_HEAP
    BOOL can_fit_blocks_p (size_t* ordered_blocks, int block_index, size_t* ordered_spaces, int* space_index);
    PER_HEAP
    BOOL can_fit_all_blocks_p (size_t* ordered_blocks, size_t* ordered_spaces, int count);
#ifdef SEG_REUSE_STATS
    PER_HEAP
    size_t dump_buckets (size_t* ordered_indices, int count, size_t* total_size);
#endif //SEG_REUSE_STATS
    PER_HEAP
    void build_ordered_free_spaces (heap_segment* seg);
    PER_HEAP
    void count_plug (size_t last_plug_size, uint8_t*& last_plug);
    PER_HEAP
    void count_plugs_in_brick (uint8_t* tree, uint8_t*& last_plug);
    PER_HEAP
    void build_ordered_plug_indices ();
    PER_HEAP
    void init_ordered_free_space_indices ();
    PER_HEAP
    void trim_free_spaces_indices ();
    PER_HEAP
    BOOL try_best_fit (BOOL end_of_segment_p);
    PER_HEAP
    BOOL best_fit (size_t free_space, size_t largest_free_space, size_t additional_space, BOOL* use_additional_space);
    PER_HEAP
    BOOL process_free_space (heap_segment* seg,
                             size_t free_space,
                             size_t min_free_size,
                             size_t min_cont_size,
                             size_t* total_free_space,
                             size_t* largest_free_space);
    PER_HEAP
    void compute_new_ephemeral_size();
    PER_HEAP
    BOOL can_expand_into_p (heap_segment* seg, size_t min_free_size,
                            size_t min_cont_size, allocator* al);
    PER_HEAP
    uint8_t* allocate_in_expanded_heap (generation* gen, size_t size,
                                     BOOL& adjacentp, uint8_t* old_loc,
#ifdef SHORT_PLUGS
                                     BOOL set_padding_on_saved_p,
                                     mark* pinned_plug_entry,
#endif //SHORT_PLUGS
                                     BOOL consider_bestfit, int active_new_gen_number
                                     REQD_ALIGN_AND_OFFSET_DEFAULT_DCL);
    PER_HEAP
    void realloc_plug (size_t last_plug_size, uint8_t*& last_plug,
                       generation* gen, uint8_t* start_address,
                       unsigned int& active_new_gen_number,
                       uint8_t*& last_pinned_gap, BOOL& leftp,
                       BOOL shortened_p
#ifdef SHORT_PLUGS
                       , mark* pinned_plug_entry
#endif //SHORT_PLUGS
                       );
    PER_HEAP
    void realloc_in_brick (uint8_t* tree, uint8_t*& last_plug, uint8_t* start_address,
                           generation* gen,
                           unsigned int& active_new_gen_number,
                           uint8_t*& last_pinned_gap, BOOL& leftp);
    PER_HEAP
    void realloc_plugs (generation* consing_gen, heap_segment* seg,
                        uint8_t* start_address, uint8_t* end_address,
                        unsigned active_new_gen_number);
    PER_HEAP
    void set_expand_in_full_gc (int condemned_gen_number);

    PER_HEAP
    generation* expand_heap (int condemned_generation,
                             generation* consing_gen,
                             heap_segment* new_heap_segment);

    PER_HEAP
    void save_ephemeral_generation_starts();
#endif //!USE_REGIONS

    PER_HEAP
    BOOL expand_reused_seg_p();

    PER_HEAP
    void verify_no_pins (uint8_t* start, uint8_t* end);

    PER_HEAP_ISOLATED
    size_t get_gen0_min_size();

    PER_HEAP
    void set_static_data();

    PER_HEAP_ISOLATED
    void init_static_data();

    PER_HEAP
    bool init_dynamic_data ();
    PER_HEAP
    float surv_to_growth (float cst, float limit, float max_limit);
    PER_HEAP
    size_t desired_new_allocation (dynamic_data* dd, size_t out,
                                   int gen_number, int pass);

    PER_HEAP
    void trim_youngest_desired_low_memory();

    PER_HEAP
    void decommit_ephemeral_segment_pages();

#ifdef HOST_64BIT
    PER_HEAP_ISOLATED
    size_t trim_youngest_desired (uint32_t memory_load,
                                  size_t total_new_allocation,
                                  size_t total_min_allocation);
    PER_HEAP_ISOLATED
    size_t joined_youngest_desired (size_t new_allocation);
#endif // HOST_64BIT
    PER_HEAP_ISOLATED
    size_t get_total_heap_size ();
    PER_HEAP_ISOLATED
    size_t get_total_committed_size();
    PER_HEAP_ISOLATED
    size_t get_total_fragmentation();
    PER_HEAP_ISOLATED
    size_t get_total_gen_fragmentation (int gen_number);
    PER_HEAP_ISOLATED
    size_t get_total_gen_estimated_reclaim (int gen_number);
    PER_HEAP_ISOLATED
    void get_memory_info (uint32_t* memory_load,
                          uint64_t* available_physical=NULL,
                          uint64_t* available_page_file=NULL);
    PER_HEAP
    size_t generation_size (int gen_number);
    PER_HEAP_ISOLATED
    size_t get_total_survived_size();
    PER_HEAP
    bool update_alloc_info (int gen_number, 
                            size_t allocated_size, 
                            size_t* etw_allocation_amount);
    // this also resets allocated_since_last_gc
    PER_HEAP_ISOLATED
    size_t get_total_allocated_since_last_gc();
    PER_HEAP
    size_t get_current_allocated();
    PER_HEAP_ISOLATED
    size_t get_total_allocated();
    PER_HEAP_ISOLATED
    size_t get_total_promoted();
#ifdef BGC_SERVO_TUNING
    PER_HEAP_ISOLATED
    size_t get_total_generation_size (int gen_number);
    PER_HEAP_ISOLATED
    size_t get_total_servo_alloc (int gen_number);
    PER_HEAP_ISOLATED
    size_t get_total_bgc_promoted();
    PER_HEAP_ISOLATED
    size_t get_total_surv_size (int gen_number);
    PER_HEAP_ISOLATED
    size_t get_total_begin_data_size (int gen_number);
    PER_HEAP_ISOLATED
    size_t get_total_generation_fl_size (int gen_number);
    PER_HEAP_ISOLATED
    size_t get_current_gc_index (int gen_number);
#endif //BGC_SERVO_TUNING
    PER_HEAP
    size_t current_generation_size (int gen_number);
    PER_HEAP
    size_t generation_plan_size (int gen_number);
    PER_HEAP
    void  compute_promoted_allocation (int gen_number);
    PER_HEAP
    size_t  compute_in (int gen_number);
    PER_HEAP
    void compute_new_dynamic_data (int gen_number);
    PER_HEAP_ISOLATED
    gc_history_global* get_gc_data_global();
    PER_HEAP
    gc_history_per_heap* get_gc_data_per_heap();
    PER_HEAP
    size_t new_allocation_limit (size_t size, size_t free_size, int gen_number);
    PER_HEAP
    size_t generation_fragmentation (generation* gen,
                                     generation* consing_gen,
                                     uint8_t* end);
    PER_HEAP
    size_t generation_sizes (generation* gen, bool use_saved_p=FALSE);
    PER_HEAP
    size_t committed_size();
    PER_HEAP
    size_t uoh_committed_size (int gen_number, size_t* allocated);
    PER_HEAP
    size_t approximate_new_allocation();
    PER_HEAP
    size_t end_space_after_gc();
    PER_HEAP
    size_t estimated_reclaim (int gen_number);
    PER_HEAP
    bool is_full_compacting_gc_productive();
    PER_HEAP
    BOOL decide_on_compacting (int condemned_gen_number,
                               size_t fragmentation,
                               BOOL& should_expand);
#ifndef USE_REGIONS
    PER_HEAP
    BOOL sufficient_space_end_seg (uint8_t* start, uint8_t* committed, uint8_t* reserved,
                                   size_t end_space_required);
#endif //!USE_REGIONS

    PER_HEAP
    BOOL ephemeral_gen_fit_p (gc_tuning_point tp);
    PER_HEAP
    void sweep_uoh_objects (int gen_num);
    PER_HEAP
    void relocate_in_uoh_objects (int gen_num);
    PER_HEAP
    void mark_through_cards_for_uoh_objects(card_fn fn, int oldest_gen_num, BOOL relocating
                                              CARD_MARKING_STEALING_ARG(gc_heap* hpt));
    PER_HEAP
    void descr_generations (const char* msg);

    PER_HEAP_ISOLATED
    void descr_generations_to_profiler (gen_walk_fn fn, void *context);

    /*------------ Multiple non isolated heaps ----------------*/
#ifdef MULTIPLE_HEAPS
    PER_HEAP_ISOLATED
    BOOL   create_thread_support (int number_of_heaps);
    PER_HEAP_ISOLATED
    void destroy_thread_support ();
    PER_HEAP
    bool create_gc_thread();
    PER_HEAP
    void gc_thread_function();
    PER_HEAP
    size_t sort_mark_list();
    PER_HEAP
    uint8_t** equalize_mark_lists(size_t total_mark_list_size);
    PER_HEAP
    void merge_mark_lists(size_t total_mark_list_size);
    PER_HEAP
    void append_to_mark_list(uint8_t **start, uint8_t **end);
#endif //MULTIPLE_HEAPS

    PER_HEAP_ISOLATED
    void grow_mark_list();

#ifdef USE_REGIONS
    PER_HEAP
    uint8_t** get_region_mark_list (uint8_t* start, uint8_t* end, uint8_t*** mark_list_end);
#endif //USE_REGIONS

#ifdef BACKGROUND_GC

    PER_HEAP
    uint8_t* high_page (heap_segment* seg, BOOL concurrent_p);

    PER_HEAP
    void revisit_written_page (uint8_t* page, uint8_t* end,
                               BOOL concurrent_p, uint8_t*& last_page,
                               uint8_t*& last_object, BOOL large_objects_p,
                               size_t& num_marked_objects);
    PER_HEAP
    void revisit_written_pages (BOOL concurrent_p, BOOL reset_only_p=FALSE);

    PER_HEAP
    void concurrent_scan_dependent_handles (ScanContext *sc);

    PER_HEAP_ISOLATED
    void suspend_EE ();

    PER_HEAP_ISOLATED
    void bgc_suspend_EE ();

    PER_HEAP_ISOLATED
    void restart_EE ();

    PER_HEAP
    void background_scan_dependent_handles (ScanContext *sc);

    PER_HEAP
    void allow_fgc();

    // Restores BGC settings if necessary.
    PER_HEAP_ISOLATED
    void recover_bgc_settings();

    PER_HEAP
    BOOL is_bgc_in_progress();

    PER_HEAP
    void clear_commit_flag();

    PER_HEAP_ISOLATED
    void clear_commit_flag_global();

    PER_HEAP_ISOLATED
    void verify_mark_array_cleared (uint8_t* begin, uint8_t* end, uint32_t* mark_array_addr);

    PER_HEAP_ISOLATED
    uint8_t* get_start_address (heap_segment* seg);

    PER_HEAP_ISOLATED
    BOOL commit_mark_array_by_range (uint8_t* begin,
                                     uint8_t* end,
                                     uint32_t* mark_array_addr);

    PER_HEAP_ISOLATED
    BOOL commit_mark_array_new_seg (gc_heap* hp,
                                    heap_segment* seg,
                                    uint32_t* new_card_table = 0,
                                    uint8_t* new_lowest_address = 0);

    PER_HEAP_ISOLATED
    BOOL commit_mark_array_with_check (heap_segment* seg, uint32_t* mark_array_addr);

    // commit the portion of the mark array that corresponds to
    // this segment (from beginning to reserved).
    // seg and heap_segment_reserved (seg) are guaranteed to be
    // page aligned.
    PER_HEAP_ISOLATED
    BOOL commit_mark_array_by_seg (heap_segment* seg, uint32_t* mark_array_addr);

    // During BGC init, we commit the mark array for all in range
    // segments whose mark array hasn't been committed or fully
    // committed. All rw segments are in range, only ro segments
    // can be partial in range.
    PER_HEAP
    BOOL commit_mark_array_bgc_init();

    PER_HEAP
    BOOL commit_new_mark_array (uint32_t* new_mark_array);

    // We need to commit all segments that intersect with the bgc
    // range. If a segment is only partially in range, we still
    // should commit the mark array for the whole segment as
    // we will set the mark array commit flag for this segment.
    PER_HEAP_ISOLATED
    BOOL commit_new_mark_array_global (uint32_t* new_mark_array);

    // We can't decommit the first and the last page in the mark array
    // if the beginning and ending don't happen to be page aligned.
    PER_HEAP
    void decommit_mark_array_by_seg (heap_segment* seg);

    PER_HEAP
    void background_mark_phase();

    PER_HEAP
    void background_drain_mark_list (int thread);

    PER_HEAP
    void background_grow_c_mark_list();

    PER_HEAP_ISOLATED
    void background_promote_callback(Object** object, ScanContext* sc, uint32_t flags);

    PER_HEAP
    void mark_absorb_new_alloc();

    PER_HEAP
    void restart_vm();

    PER_HEAP
    BOOL prepare_bgc_thread(gc_heap* gh);
    PER_HEAP
    BOOL create_bgc_thread(gc_heap* gh);
    PER_HEAP_ISOLATED
    BOOL create_bgc_threads_support (int number_of_heaps);
    PER_HEAP
    BOOL create_bgc_thread_support();
    PER_HEAP_ISOLATED
    int check_for_ephemeral_alloc();
    PER_HEAP_ISOLATED
    void wait_to_proceed();
    PER_HEAP_ISOLATED
    void fire_alloc_wait_event_begin (alloc_wait_reason awr);
    PER_HEAP_ISOLATED
    void fire_alloc_wait_event_end (alloc_wait_reason awr);
    PER_HEAP
    uint32_t background_gc_wait (alloc_wait_reason awr = awr_ignored, int time_out_ms = INFINITE);
    PER_HEAP_ISOLATED
    BOOL background_running_p() { return gc_background_running; }
    PER_HEAP_ISOLATED
    void start_c_gc();
    PER_HEAP
    void kill_gc_thread();
    PER_HEAP
    void bgc_thread_function();
    PER_HEAP_ISOLATED
    void do_background_gc();
    static
    void bgc_thread_stub (void* arg);
#endif //BACKGROUND_GC

public:

    PER_HEAP_ISOLATED
    VOLATILE(bool) internal_gc_done;

#ifdef BACKGROUND_GC
    PER_HEAP_ISOLATED
    uint32_t cm_in_progress;

    // normally this is FALSE; we set it to TRUE at the end of the gen1 GC
    // we do right before the bgc starts.
    PER_HEAP_ISOLATED
    BOOL     dont_restart_ee_p;

    PER_HEAP_ISOLATED
    GCEvent bgc_start_event;
#endif //BACKGROUND_GC

    // The variables in this block are known to the DAC and must come first
    // in the gc_heap class.

    // Keeps track of the highest address allocated by Alloc
    PER_HEAP
    uint8_t* alloc_allocated;

    // The ephemeral heap segment
    // For regions this is the region we currently allocate in.
    PER_HEAP
    heap_segment* ephemeral_heap_segment;

    // The finalize queue.
    PER_HEAP
    CFinalize* finalize_queue;

    // OOM info.
    PER_HEAP
    oom_history oom_info;

    // Interesting data, recorded per-heap.
    PER_HEAP
    size_t interesting_data_per_heap[max_idp_count];

    PER_HEAP
    size_t compact_reasons_per_heap[max_compact_reasons_count];

    PER_HEAP
    size_t expand_mechanisms_per_heap[max_expand_mechanisms_count];

    PER_HEAP
    size_t interesting_mechanism_bits_per_heap[max_gc_mechanism_bits_count];

    PER_HEAP
    uint8_t** internal_root_array;

    PER_HEAP
    size_t internal_root_array_index;

    PER_HEAP
    BOOL heap_analyze_success;

    // The generation table. Must always be last.
    PER_HEAP
    generation generation_table [total_generation_count];

    // End DAC zone

#ifdef USE_REGIONS
#ifdef STRESS_REGIONS
    // TODO: could consider dynamically grow this.
    // Right now the way it works -
    // For each gen0 region, pin an object somewhere near the beginning and middle.
    // When we run out start replacing objects pinned by the earlier handles.
#define PINNING_HANDLE_INITIAL_LENGTH 128
    PER_HEAP
    OBJECTHANDLE* pinning_handles_for_alloc;
    PER_HEAP
    int ph_index_per_heap;
    PER_HEAP
    int pinning_seg_interval;
    PER_HEAP
    size_t num_gen0_regions;
    PER_HEAP
    int sip_seg_interval;
    PER_HEAP
    int sip_seg_maxgen_interval;
    PER_HEAP
    size_t num_condemned_regions;
#endif //STRESS_REGIONS

    PER_HEAP
    heap_segment* free_regions;

    PER_HEAP
    int num_free_regions;

    PER_HEAP
    int num_free_regions_added;

    PER_HEAP
    int num_free_regions_removed;

    // This is the number of regions we would free up if we sweep.
    // It's used in the decision for compaction so we calculate it in plan.
    PER_HEAP
    int num_regions_freed_in_sweep;

    PER_HEAP
    heap_segment* free_large_regions;

    PER_HEAP
    int num_free_large_regions;

    PER_HEAP
    int num_free_large_regions_added;

    PER_HEAP
    int num_free_large_regions_removed;

    PER_HEAP
    int regions_per_gen[max_generation + 1];

    PER_HEAP
    int sip_maxgen_regions_per_gen[max_generation + 1];

    PER_HEAP
    heap_segment* reserved_free_regions_sip[max_generation];

    PER_HEAP
    int num_sip_regions;

    PER_HEAP
    size_t committed_in_free;

    PER_HEAP
    // After plan we calculate this as the planned end gen0 space;
    // but if we end up sweeping, we recalculate it at the end of
    // sweep.
    size_t end_gen0_region_space;

    // These are updated as we plan and will be used to make compaction
    // decision.
    PER_HEAP
    size_t gen0_pinned_free_space;

    PER_HEAP
    bool gen0_large_chunk_found;

    PER_HEAP_ISOLATED
    size_t regions_range;

    // Each GC thread maintains its own record of survived/survived due to 
    // old gen cards pointing into that region. These allow us to make the 
    // following decisions - 
    // 
    // If a region's survival rate is very high, it's not very useful to
    // compact it, unless we want to free up its virtual address range to
    // form a larger free space so we can accommodate a larger region.
    //
    // During a GC whose plan gen is not gen2, if a region's survival rate 
    // is very high and most of the survival comes from old generations' cards,
    // it would be much better to promote that region directly into gen2 
    // intead of having to go through gen1 then get promoted to gen2.
    //
    // I'm reusing g_mark_list_piece for these since g_mark_list_piece is
    // not used while we are marking. So this means we can only use this up 
    // till sort_mark_list is called.
    // 
    // REGIONS TODO: this means we should treat g_mark_list_piece as part of 
    // the GC bookkeeping data structures and allocate it as such.
    //
    // REGIONS TODO: currently we only make use of SOH's promoted bytes to 
    // make decisions whether we want to compact or sweep a region. We 
    // should also enable this for LOH compaction.
    PER_HEAP
    size_t* survived_per_region;
    PER_HEAP
    size_t* old_card_survived_per_region;
#endif //USE_REGIONS

#define max_oom_history_count 4

    PER_HEAP
    int oomhist_index_per_heap;

    PER_HEAP
    oom_history oomhist_per_heap[max_oom_history_count];

    PER_HEAP
    void add_to_oom_history_per_heap();

    PER_HEAP_ISOLATED
    uint32_t wait_for_gc_done(int32_t timeOut = INFINITE);

    // Returns TRUE if the current thread used to be in cooperative mode
    // before calling this function.
    PER_HEAP_ISOLATED
    bool enable_preemptive ();
    PER_HEAP_ISOLATED
    void disable_preemptive (bool restore_cooperative);

    /* ------------------- per heap members --------------------------*/

    PER_HEAP
#ifndef MULTIPLE_HEAPS
    GCEvent gc_done_event;
#else // MULTIPLE_HEAPS
    GCEvent gc_done_event;
#endif // MULTIPLE_HEAPS

    PER_HEAP
    VOLATILE(int32_t) gc_done_event_lock;

    PER_HEAP
    VOLATILE(bool) gc_done_event_set;

    PER_HEAP
    void set_gc_done();

    PER_HEAP
    void reset_gc_done();

    PER_HEAP
    void enter_gc_done_event_lock();

    PER_HEAP
    void exit_gc_done_event_lock();

#ifndef USE_REGIONS
    PER_HEAP
    uint8_t*  ephemeral_low;      //lowest ephemeral address

    PER_HEAP
    uint8_t*  ephemeral_high;     //highest ephemeral address
#endif //!USE_REGIONS

    PER_HEAP
    uint32_t* card_table;

    PER_HEAP
    short* brick_table;

#ifdef BACKGROUND_GC
    PER_HEAP
    uint32_t* mark_array;
#endif //BACKGROUND_GC

#ifdef CARD_BUNDLE
    PER_HEAP
    uint32_t* card_bundle_table;
#endif //CARD_BUNDLE

#ifdef FEATURE_BASICFREEZE
    PER_HEAP_ISOLATED
    sorted_table* seg_table;
#endif //FEATURE_BASICFREEZE

    PER_HEAP_ISOLATED
    VOLATILE(BOOL) gc_started;

    // The following 2 events are there to support the gen2
    // notification feature which is only enabled if concurrent
    // GC is disabled.
    PER_HEAP_ISOLATED
    GCEvent full_gc_approach_event;

    PER_HEAP_ISOLATED
    GCEvent full_gc_end_event;

    // Full GC Notification percentages.
    PER_HEAP
    uint32_t fgn_maxgen_percent;

    PER_HEAP_ISOLATED
    uint32_t fgn_loh_percent;

    PER_HEAP_ISOLATED
    VOLATILE(bool) full_gc_approach_event_set;

    PER_HEAP_ISOLATED
    bool special_sweep_p;

#ifdef BACKGROUND_GC
    PER_HEAP_ISOLATED
    BOOL fgn_last_gc_was_concurrent;
#endif //BACKGROUND_GC

    PER_HEAP
    size_t fgn_last_alloc;

    static uint32_t user_thread_wait (GCEvent *event, BOOL no_mode_change, int time_out_ms=INFINITE);

    static wait_full_gc_status full_gc_wait (GCEvent *event, int time_out_ms);

#ifndef USE_REGIONS
    PER_HEAP
    uint8_t* demotion_low;

    PER_HEAP
    uint8_t* demotion_high;
#endif //!USE_REGIONS

    PER_HEAP
    BOOL demote_gen1_p;

    PER_HEAP
    uint8_t* last_gen1_pin_end;

    PER_HEAP
    gen_to_condemn_tuning gen_to_condemn_reasons;

    PER_HEAP
    size_t etw_allocation_running_amount[gc_oh_num::total_oh_count - 1];

    PER_HEAP
    uint64_t total_alloc_bytes_soh;

    PER_HEAP
    uint64_t total_alloc_bytes_uoh;

    PER_HEAP
    int gc_policy;  //sweep, compact, expand

#ifdef MULTIPLE_HEAPS
    PER_HEAP_ISOLATED
    bool gc_thread_no_affinitize_p;

    PER_HEAP_ISOLATED
    GCEvent gc_start_event;

    PER_HEAP_ISOLATED
    GCEvent ee_suspend_event;

    PER_HEAP
    heap_segment* new_heap_segment;

    PER_HEAP_ISOLATED
    size_t min_gen0_balance_delta;

#define alloc_quantum_balance_units (16)

    PER_HEAP_ISOLATED
    size_t min_balance_threshold;
#else //MULTIPLE_HEAPS

    PER_HEAP
    size_t allocation_running_time;

    PER_HEAP
    size_t allocation_running_amount;

#endif //MULTIPLE_HEAPS

    PER_HEAP_ISOLATED
    gc_latency_level latency_level;

    PER_HEAP_ISOLATED
    gc_mechanisms settings;

    PER_HEAP_ISOLATED
    gc_history_global gc_data_global;

    PER_HEAP_ISOLATED
    uint64_t gc_last_ephemeral_decommit_time;

#ifdef SHORT_PLUGS
    PER_HEAP_ISOLATED
    double short_plugs_pad_ratio;
#endif //SHORT_PLUGS

    // We record the time GC work is done while EE is suspended.
    // suspended_start_ts is what we get right before we call
    // SuspendEE. We omit the time between GC end and RestartEE
    // because it's very short and by the time we are calling it
    // the settings may have changed and we'd have to do more work
    // to figure out the right GC to record info of.
    //
    // The complications are the GCs triggered without their own
    // SuspendEE, in which case we will record that GC's duration
    // as its pause duration and the rest toward the GC that
    // the SuspendEE was for. The ephemeral GC we might trigger
    // at the beginning of a BGC and the PM triggered full GCs
    // fall into this case.
    PER_HEAP_ISOLATED
    uint64_t suspended_start_time;

    PER_HEAP_ISOLATED
    uint64_t end_gc_time;

    PER_HEAP_ISOLATED
    uint64_t total_suspended_time;

    PER_HEAP_ISOLATED
    uint64_t process_start_time;

    PER_HEAP_ISOLATED
    last_recorded_gc_info last_ephemeral_gc_info;

    PER_HEAP_ISOLATED
    last_recorded_gc_info last_full_blocking_gc_info;

#ifdef BACKGROUND_GC
    // If the user didn't specify which kind of GC info to return, we need
    // to return the last recorded one. There's a complication with BGC as BGC
    // end runs concurrently. If 2 BGCs run back to back, we can't have one
    // update the info while the user thread is reading it (and we'd still like
    // to return the last BGC info otherwise if we only did BGCs we could frequently
    // return nothing). So we maintain 2 of these for BGC and the older one is
    // guaranteed to be consistent.
    PER_HEAP_ISOLATED
    last_recorded_gc_info last_bgc_info[2];
    // This is either 0 or 1.
    PER_HEAP_ISOLATED
    VOLATILE(int) last_bgc_info_index;
    // Since a BGC can finish later than blocking GCs with larger indices,
    // we can't just compare the index recorded in the GC info. We use this
    // to know whether we should be looking for a bgc info or a blocking GC,
    // if the user asks for the latest GC info of any kind.
    // This can only go from false to true concurrently so if it is true,
    // it means the bgc info is ready.
    PER_HEAP_ISOLATED
    VOLATILE(bool) is_last_recorded_bgc;

    PER_HEAP_ISOLATED
    void add_bgc_pause_duration_0();

    PER_HEAP_ISOLATED
    last_recorded_gc_info* get_completed_bgc_info();
#endif //BACKGROUND_GC

#ifdef HOST_64BIT
    PER_HEAP_ISOLATED
        size_t youngest_gen_desired_th;
#endif //HOST_64BIT

    PER_HEAP_ISOLATED
    uint32_t high_memory_load_th;

    PER_HEAP_ISOLATED
    uint32_t m_high_memory_load_th;

    PER_HEAP_ISOLATED
    uint32_t v_high_memory_load_th;

    PER_HEAP_ISOLATED
    bool is_restricted_physical_mem;

    PER_HEAP_ISOLATED
    uint64_t mem_one_percent;

    PER_HEAP_ISOLATED
    uint64_t total_physical_mem;

    PER_HEAP_ISOLATED
    uint64_t entry_available_physical_mem;

    // Hard limit for the heap, only supported on 64-bit.
    //
    // Users can specify a hard limit for the GC heap via GCHeapHardLimit or
    // a percentage of the physical memory this process is allowed to use via
    // GCHeapHardLimitPercent. This is the maximum commit size the GC heap
    // can consume.
    //
    // The way the hard limit is decided is:
    //
    // If the GCHeapHardLimit config is specified that's the value we use;
    // else if the GCHeapHardLimitPercent config is specified we use that
    // value;
    // else if the process is running inside a container with a memory limit,
    // the hard limit is
    // max (20mb, 75% of the memory limit on the container).
    //
    // Due to the different perf charicteristics of containers we make the
    // following policy changes:
    //
    // 1) No longer affinitize Server GC threads by default because we wouldn't
    // want all the containers on the machine to only affinitize to use the
    // first few CPUs (and we don't know which CPUs are already used). You
    // can however override this by specifying the GCHeapAffinitizeMask
    // config which will decide which CPUs the process will affinitize the
    // Server GC threads to.
    //
    // 2) Segment size is determined by limit / number of heaps but has a
    // minimum value of 16mb. This can be changed by specifying the number
    // of heaps via the GCHeapCount config. The minimum size is to avoid
    // the scenario where the hard limit is small but the process can use
    // many procs and we end up with tiny segments which doesn't make sense.
    //
    // 3) LOH compaction occurs automatically if needed.
    //
    // Since we do allow both gen0 and gen3 allocations, and we don't know
    // the distinction (and it's unrealistic to request users to specify
    // this distribution) we reserve memory this way -
    //
    // For SOH we reserve (limit / number of heaps) per heap.
    // For LOH we reserve (limit * 2 / number of heaps) per heap.
    //
    // This means the following -
    //
    // + we never need to acquire new segments. This simplies the perf
    // calculations by a lot.
    //
    // + we now need a different definition of "end of seg" because we
    // need to make sure the total does not exceed the limit.
    //
    // + if we detect that we exceed the commit limit in the allocator we
    // wouldn't want to treat that as a normal commit failure because that
    // would mean we always do full compacting GCs.
    //
    // TODO: some of the logic here applies to the general case as well
    // such as LOH automatic compaction. However it will require more
    //testing to change the general case.
    PER_HEAP_ISOLATED
    size_t heap_hard_limit;

    PER_HEAP_ISOLATED
    size_t heap_hard_limit_oh[total_oh_count - 1];

    PER_HEAP_ISOLATED
    CLRCriticalSection check_commit_cs;

    PER_HEAP_ISOLATED
    size_t current_total_committed;

    PER_HEAP_ISOLATED
    size_t committed_by_oh[total_oh_count];

    // This is what GC uses for its own bookkeeping.
    PER_HEAP_ISOLATED
    size_t current_total_committed_bookkeeping;

    // This is if large pages should be used.
    PER_HEAP_ISOLATED
    bool use_large_pages_p;

#ifdef HEAP_BALANCE_INSTRUMENTATION
    PER_HEAP_ISOLATED
    size_t last_gc_end_time_us;
#endif //HEAP_BALANCE_INSTRUMENTATION

#ifndef USE_REGIONS
    PER_HEAP_ISOLATED
    size_t min_segment_size;

    PER_HEAP_ISOLATED
    size_t min_uoh_segment_size;
#endif //!USE_REGIONS

    // For regions this is for region size.
    PER_HEAP_ISOLATED
    size_t min_segment_size_shr;

    // For SOH we always allocate segments of the same
    // size unless no_gc_region requires larger ones.
    PER_HEAP_ISOLATED
    size_t soh_segment_size;

    PER_HEAP_ISOLATED
    size_t segment_info_size;

    PER_HEAP
    uint8_t* lowest_address;

    PER_HEAP
    uint8_t* highest_address;

    PER_HEAP
    BOOL ephemeral_promotion;
    PER_HEAP
    uint8_t* saved_ephemeral_plan_start[ephemeral_generation_count];
    PER_HEAP
    size_t saved_ephemeral_plan_start_size[ephemeral_generation_count];

protected:
#ifdef MULTIPLE_HEAPS
    PER_HEAP
    GCHeap* vm_heap;
    PER_HEAP
    int heap_number;
    PER_HEAP
    VOLATILE(int) alloc_context_count;
#else //MULTIPLE_HEAPS
#define vm_heap ((GCHeap*) g_theGCHeap)
#define heap_number (0)
#endif //MULTIPLE_HEAPS

    PER_HEAP
    uint64_t time_bgc_last;

//#ifndef USE_REGIONS
    PER_HEAP
    uint8_t*       gc_low; // lowest address being condemned

    PER_HEAP
    uint8_t*       gc_high; // highest address being condemned
//#endif //USE_REGIONS

    PER_HEAP
    size_t      mark_stack_tos;

    PER_HEAP
    size_t      mark_stack_bos;

    PER_HEAP
    size_t      mark_stack_array_length;

    PER_HEAP
    mark*       mark_stack_array;

#if defined (_DEBUG) && defined (VERIFY_HEAP)
    PER_HEAP
    BOOL       verify_pinned_queue_p;
#endif // _DEBUG && VERIFY_HEAP

    PER_HEAP
    uint8_t*    oldest_pinned_plug;

    PER_HEAP
    size_t      num_pinned_objects;

#ifdef FEATURE_LOH_COMPACTION
    PER_HEAP
    size_t      loh_pinned_queue_tos;

    PER_HEAP
    size_t      loh_pinned_queue_bos;

    PER_HEAP
    size_t      loh_pinned_queue_length;

    PER_HEAP_ISOLATED
    int         loh_pinned_queue_decay;

    PER_HEAP
    mark*       loh_pinned_queue;

    // This is for forced LOH compaction via the complus env var
    PER_HEAP_ISOLATED
    BOOL        loh_compaction_always_p;

    // This is set by the user.
    PER_HEAP_ISOLATED
    gc_loh_compaction_mode loh_compaction_mode;

    // We may not compact LOH on every heap if we can't
    // grow the pinned queue. This is to indicate whether
    // this heap's LOH is compacted or not. So even if
    // settings.loh_compaction is TRUE this may not be TRUE.
    PER_HEAP
    BOOL        loh_compacted_p;
#endif //FEATURE_LOH_COMPACTION

#ifdef BACKGROUND_GC

    PER_HEAP
    EEThreadId bgc_thread_id;

#ifdef WRITE_WATCH
    PER_HEAP
    uint8_t* background_written_addresses [array_size+2];
#endif //WRITE_WATCH

    PER_HEAP_ISOLATED
    VOLATILE(c_gc_state) current_c_gc_state;     //tells the large object allocator to
    //mark the object as new since the start of gc.

    PER_HEAP_ISOLATED
    gc_mechanisms saved_bgc_settings;

    PER_HEAP_ISOLATED
    gc_history_global bgc_data_global;

    PER_HEAP_ISOLATED
    VOLATILE(BOOL) gc_background_running;

    PER_HEAP
    gc_history_per_heap bgc_data_per_heap;

    PER_HEAP
    BOOL bgc_thread_running; // gc thread is its main loop

    PER_HEAP_ISOLATED
    BOOL keep_bgc_threads_p;

    // This event is used by BGC threads to do something on
    // one specific thread while other BGC threads have to
    // wait. This is different from a join 'cause you can't
    // specify which thread should be doing some task
    // while other threads have to wait.
    // For example, to make the BGC threads managed threads
    // we need to create them on the thread that called
    // SuspendEE which is heap 0.
    PER_HEAP_ISOLATED
    GCEvent bgc_threads_sync_event;

    PER_HEAP
    Thread* bgc_thread;

    PER_HEAP
    CLRCriticalSection bgc_threads_timeout_cs;

    PER_HEAP_ISOLATED
    GCEvent background_gc_done_event;

    PER_HEAP_ISOLATED
    GCEvent ee_proceed_event;

    PER_HEAP_ISOLATED
    bool gc_can_use_concurrent;

    PER_HEAP_ISOLATED
    bool temp_disable_concurrent_p;

    PER_HEAP_ISOLATED
    BOOL do_ephemeral_gc_p;

    PER_HEAP_ISOLATED
    BOOL do_concurrent_p;

    PER_HEAP
    VOLATILE(bgc_state) current_bgc_state;

    struct gc_history
    {
        size_t gc_index;
        bgc_state current_bgc_state;
        uint32_t gc_time_ms;
        // This is in bytes per ms; consider breaking it
        // into the efficiency per phase.
        size_t gc_efficiency;
#ifndef USE_REGIONS
        uint8_t* eph_low;
        uint8_t* gen0_start;
        uint8_t* eph_high;
#endif //!USE_REGIONS
        uint8_t* bgc_highest;
        uint8_t* bgc_lowest;
        uint8_t* fgc_highest;
        uint8_t* fgc_lowest;
        uint8_t* g_highest;
        uint8_t* g_lowest;
    };

#define max_history_count 64

    PER_HEAP
    int gchist_index_per_heap;

    PER_HEAP
    gc_history gchist_per_heap[max_history_count];

    PER_HEAP_ISOLATED
    int gchist_index;

    PER_HEAP_ISOLATED
    gc_mechanisms_store gchist[max_history_count];

    PER_HEAP
    size_t     bgc_overflow_count;

    PER_HEAP
    size_t     bgc_begin_loh_size;
    PER_HEAP
    size_t     bgc_begin_poh_size;
    PER_HEAP
    size_t     end_loh_size;
    PER_HEAP
    size_t     end_poh_size;

#ifdef BGC_SERVO_TUNING
    PER_HEAP
    uint64_t   loh_a_no_bgc;

    PER_HEAP
    uint64_t   loh_a_bgc_marking;

    PER_HEAP
    uint64_t   loh_a_bgc_planning;

    // Total allocated last BGC's plan + between last and this bgc +
    // this bgc's mark
    PER_HEAP_ISOLATED
    uint64_t   total_loh_a_last_bgc;

    PER_HEAP
    size_t     bgc_maxgen_end_fl_size;
#endif //BGC_SERVO_TUNING

    // We need to throttle the LOH allocations during BGC since we can't
    // collect LOH when BGC is in progress.
    // We allow the LOH heap size to double during a BGC. So for every
    // 10% increase we will have the LOH allocating thread sleep for one more
    // ms. So we are already 30% over the original heap size the thread will
    // sleep for 3ms.
    PER_HEAP
    uint32_t   bgc_alloc_spin_uoh;

    // This includes what we allocate at the end of segment - allocating
    // in free list doesn't increase the heap size.
    PER_HEAP
    size_t     bgc_loh_size_increased;
    PER_HEAP
    size_t     bgc_poh_size_increased;

    PER_HEAP
    size_t     background_soh_alloc_count;

    PER_HEAP
    size_t     background_uoh_alloc_count;

    PER_HEAP
    VOLATILE(int32_t) uoh_alloc_thread_count;

    PER_HEAP
    uint8_t**  background_mark_stack_tos;

    PER_HEAP
    uint8_t**  background_mark_stack_array;

    PER_HEAP
    size_t    background_mark_stack_array_length;

    PER_HEAP
    uint8_t*  background_min_overflow_address;

    PER_HEAP
    uint8_t*  background_max_overflow_address;

    // We can't process the ephemeral range concurrently so we
    // wait till final mark to process it.
    PER_HEAP
    BOOL      processed_eph_overflow_p;

#ifndef USE_REGIONS
    PER_HEAP
    uint8_t*  background_min_soh_overflow_address;

    PER_HEAP
    uint8_t*  background_max_soh_overflow_address;

    PER_HEAP
    heap_segment* saved_overflow_ephemeral_seg;

    PER_HEAP
    heap_segment* saved_sweep_ephemeral_seg;

    PER_HEAP
    uint8_t* saved_sweep_ephemeral_start;
#endif //!USE_REGIONS

    PER_HEAP
    uint8_t* background_saved_lowest_address;

    PER_HEAP
    uint8_t* background_saved_highest_address;

    // This is used for synchronization between the bgc thread
    // for this heap and the user threads allocating on this
    // heap.
    PER_HEAP
    exclusive_sync* bgc_alloc_lock;

#ifdef SNOOP_STATS
    PER_HEAP
    snoop_stats_data snoop_stat;
#endif //SNOOP_STATS


    PER_HEAP
    uint8_t**          c_mark_list;

    PER_HEAP
    size_t          c_mark_list_length;

    PER_HEAP
    size_t          c_mark_list_index;
#endif //BACKGROUND_GC

    PER_HEAP
    uint8_t** mark_list;

    PER_HEAP_ISOLATED
    size_t mark_list_size;

    PER_HEAP_ISOLATED
    bool mark_list_overflow;

    PER_HEAP
    uint8_t** mark_list_end;

    PER_HEAP
    uint8_t** mark_list_index;

    PER_HEAP_ISOLATED
    uint8_t** g_mark_list;
    PER_HEAP_ISOLATED
    uint8_t** g_mark_list_copy;
    PER_HEAP
    uint8_t*** mark_list_piece_start;
    PER_HEAP
    uint8_t*** mark_list_piece_end;
#ifdef USE_REGIONS
    // REGIONS TODO: these are allocated separately but should really be part
    // of GC's book keeping datastructures.
    PER_HEAP_ISOLATED
    size_t g_mark_list_piece_size;
    PER_HEAP_ISOLATED
    uint8_t*** g_mark_list_piece;
#endif //USE_REGIONS

    PER_HEAP
    uint8_t*  min_overflow_address;

    PER_HEAP
    uint8_t*  max_overflow_address;

#ifndef MULTIPLE_HEAPS
    PER_HEAP
    uint8_t*  shigh; //keeps track of the highest marked object

    PER_HEAP
    uint8_t*  slow; //keeps track of the lowest marked object
#endif //MULTIPLE_HEAPS

    PER_HEAP
    size_t allocation_quantum;

    PER_HEAP
    size_t alloc_contexts_used;

    PER_HEAP_ISOLATED
    no_gc_region_info current_no_gc_region_info;

    PER_HEAP
    size_t soh_allocation_no_gc;

    PER_HEAP
    size_t loh_allocation_no_gc;

    PER_HEAP
    bool no_gc_oom_p;

    PER_HEAP
    heap_segment* saved_loh_segment_no_gc;

    PER_HEAP_ISOLATED
    BOOL proceed_with_gc_p;

#ifdef MULTIPLE_HEAPS
    PER_HEAP_ISOLATED
    BOOL gradual_decommit_in_progress_p;

    PER_HEAP_ISOLATED
    size_t max_decommit_step_size;
#endif //MULTIPLE_HEAPS

#define youngest_generation (generation_of (0))
#define large_object_generation (generation_of (loh_generation))
#define pinned_object_generation (generation_of (poh_generation))

    // The more_space_lock and gc_lock is used for 3 purposes:
    //
    // 1) to coordinate threads that exceed their quantum (UP & MP) (more_space_lock_soh)
    // 2) to synchronize allocations of large objects (more_space_lock_uoh)
    // 3) to synchronize the GC itself (gc_lock)
    //
    PER_HEAP_ISOLATED
    GCSpinLock gc_lock; //lock while doing GC

    PER_HEAP
    GCSpinLock more_space_lock_soh; //lock while allocating more space for soh

    PER_HEAP
    GCSpinLock more_space_lock_uoh;

#ifdef SYNCHRONIZATION_STATS

    PER_HEAP
    unsigned int good_suspension;

    PER_HEAP
    unsigned int bad_suspension;

    // Number of times when msl_acquire is > 200 cycles.
    PER_HEAP
    unsigned int num_high_msl_acquire;

    // Number of times when msl_acquire is < 200 cycles.
    PER_HEAP
    unsigned int num_low_msl_acquire;

    // Number of times the more_space_lock is acquired.
    PER_HEAP
    unsigned int num_msl_acquired;

    // Total cycles it takes to acquire the more_space_lock.
    PER_HEAP
    uint64_t total_msl_acquire;

    PER_HEAP
    void init_heap_sync_stats()
    {
        good_suspension = 0;
        bad_suspension = 0;
        num_msl_acquired = 0;
        total_msl_acquire = 0;
        num_high_msl_acquire = 0;
        num_low_msl_acquire = 0;
        more_space_lock.init();
        gc_lock.init();
    }

    PER_HEAP
    void print_heap_sync_stats(unsigned int heap_num, unsigned int gc_count_during_log)
    {
        printf("%2d%2d%10u%10u%12u%6u%4u%8u(%4u,%4u,%4u,%4u)\n",
            heap_num,
            alloc_contexts_used,
            good_suspension,
            bad_suspension,
            (unsigned int)(total_msl_acquire / gc_count_during_log),
            num_high_msl_acquire / gc_count_during_log,
            num_low_msl_acquire / gc_count_during_log,
            num_msl_acquired / gc_count_during_log,
            more_space_lock.num_switch_thread / gc_count_during_log,
            more_space_lock.num_wait_longer / gc_count_during_log,
            more_space_lock.num_switch_thread_w / gc_count_during_log,
            more_space_lock.num_disable_preemptive_w / gc_count_during_log);
    }

#endif //SYNCHRONIZATION_STATS

#define NUM_LOH_ALIST (7)
    // bucket 0 contains sizes less than 64*1024
    // the "BITS" number here is the highest bit in 64*1024 - 1, zero-based as in BitScanReverse.
    // see first_suitable_bucket(size_t size) for details.
#define BASE_LOH_ALIST_BITS (15)
    PER_HEAP
    alloc_list loh_alloc_list[NUM_LOH_ALIST-1];

#define NUM_GEN2_ALIST (12)
#ifdef HOST_64BIT
    // bucket 0 contains sizes less than 256
#define BASE_GEN2_ALIST_BITS (7)
#else
    // bucket 0 contains sizes less than 128
#define BASE_GEN2_ALIST_BITS (6)
#endif // HOST_64BIT
    PER_HEAP
    alloc_list gen2_alloc_list[NUM_GEN2_ALIST-1];

#define NUM_POH_ALIST (19)
    // bucket 0 contains sizes less than 256
#define BASE_POH_ALIST_BITS (7)
    PER_HEAP
    alloc_list poh_alloc_list[NUM_POH_ALIST-1];

#ifdef DOUBLY_LINKED_FL
    // For bucket 0 added list, we don't want to have to go through
    // it to count how many bytes it has so we keep a record here.
    // If we need to sweep in gen1, we discard this added list and
    // need to deduct the size from free_list_space.
    // Note that we should really move this and the free_list_space
    // accounting into the alloc_list class.
    PER_HEAP
    size_t gen2_removed_no_undo;

    PER_HEAP
    size_t saved_pinned_plug_index;
#endif //DOUBLY_LINKED_FL

#ifdef FEATURE_EVENT_TRACE
    PER_HEAP_ISOLATED
    bool informational_event_enabled_p;

    // Time is all in microseconds here. These are times recorded during STW.
    //
    // Note that the goal of this is not to show every single type of roots
    // For that you have the per heap MarkWithType events. This takes advantage
    // of the joins we already have and naturally gets the time between each 
    // join.
    enum etw_gc_time_info
    {
        time_mark_sizedref = 0,
        // Note time_mark_roots does not include scanning sizedref handles.
        time_mark_roots = 1,
        time_mark_short_weak = 2,
        time_mark_scan_finalization = 3,
        time_mark_long_weak = 4,
        max_bgc_time_type = 5,
        time_plan = 5,
        time_relocate = 6,
        time_sweep = 6,
        max_sweep_time_type = 7,
        time_compact = 7,
        max_compact_time_type = 8
    };

    PER_HEAP_ISOLATED
    uint64_t* gc_time_info;

#ifdef BACKGROUND_GC
    PER_HEAP_ISOLATED
    uint64_t* bgc_time_info;
#endif //BACKGROUND_GC

    PER_HEAP_ISOLATED
    void record_mark_time (uint64_t& mark_time, 
                           uint64_t& current_mark_time,
                           uint64_t& last_mark_time);

#define max_etw_item_count 2000

    enum etw_bucket_kind
    {
        largest_fl_items = 0,
        plugs_in_condemned = 1
    };

    // This is for gen2 FL purpose so it would use sizes for gen2 buckets.
    // This event is only to give us a rough idea of the largest gen2 fl
    // items or plugs that we had to allocate in condemned. We only fire
    // these events on verbose level and stop at max_etw_item_count items.
    PER_HEAP
    etw_bucket_info bucket_info[NUM_GEN2_ALIST];
    
    PER_HEAP
    void init_bucket_info();

    PER_HEAP
    void add_plug_in_condemned_info (generation* gen, size_t plug_size);

    PER_HEAP
    void fire_etw_allocation_event (size_t allocation_amount, 
                                    int gen_number, 
                                    uint8_t* object_address,
                                    size_t object_size);

    PER_HEAP
    void fire_etw_pin_object_event (uint8_t* object, uint8_t** ppObject);

    // config stuff
    PER_HEAP_ISOLATED
    size_t physical_memory_from_config;

    PER_HEAP_ISOLATED
    size_t gen0_min_budget_from_config;

    PER_HEAP_ISOLATED
    size_t gen0_max_budget_from_config;

    PER_HEAP_ISOLATED
    int high_mem_percent_from_config;

    PER_HEAP_ISOLATED
    bool use_frozen_segments_p;

    PER_HEAP_ISOLATED
    bool hard_limit_config_p;

#ifdef FEATURE_LOH_COMPACTION
    // This records the LOH compaction info -
    // time it takes to plan, relocate and compact.
    // We want to see how reference rich large objects are so
    // we also record ref info. Survived bytes are already recorded
    // in gc_generation_data of the perheap history event.
    //
    // If we don't end up actually doing LOH compaction because plan
    // failed, the time would all be 0s.
    struct etw_loh_compact_info
    {
        uint32_t time_plan;
        uint32_t time_compact;
        uint32_t time_relocate;
        size_t total_refs;
        size_t zero_refs;
    };

    PER_HEAP_ISOLATED
    etw_loh_compact_info* loh_compact_info;

    PER_HEAP
    void loh_reloc_survivor_helper (uint8_t** pval, 
                                    size_t& total_refs, 
                                    size_t& zero_refs);
#endif //FEATURE_LOH_COMPACTION
#endif //FEATURE_EVENT_TRACE

    PER_HEAP
    dynamic_data dynamic_data_table [total_generation_count];

    PER_HEAP
    gc_history_per_heap gc_data_per_heap;

    PER_HEAP
    size_t total_promoted_bytes;

    PER_HEAP
    size_t finalization_promoted_bytes;

    PER_HEAP
    size_t maxgen_pinned_compact_before_advance;

    // dynamic tuning.
    PER_HEAP
    BOOL dt_low_ephemeral_space_p (gc_tuning_point tp);
    // if elevate_p is FALSE, it means we are determining fragmentation for a generation
    // to see if we should condemn this gen; otherwise it means we are determining if
    // we should elevate to doing max_gen from an ephemeral gen.
    PER_HEAP
    BOOL dt_high_frag_p (gc_tuning_point tp, int gen_number, BOOL elevate_p=FALSE);
    PER_HEAP
    BOOL
    dt_estimate_reclaim_space_p (gc_tuning_point tp, int gen_number);
    PER_HEAP
    BOOL dt_estimate_high_frag_p (gc_tuning_point tp, int gen_number, uint64_t available_mem);
    PER_HEAP
    BOOL dt_low_card_table_efficiency_p (gc_tuning_point tp);

    PER_HEAP
    int generation_skip_ratio;//in %

#ifdef FEATURE_CARD_MARKING_STEALING
    PER_HEAP
    VOLATILE(size_t) n_eph_soh;
    PER_HEAP
    VOLATILE(size_t) n_gen_soh;
    PER_HEAP
    VOLATILE(size_t) n_eph_loh;
    PER_HEAP
    VOLATILE(size_t) n_gen_loh;
#endif //FEATURE_CARD_MARKING_STEALING

    PER_HEAP_ISOLATED
    int generation_skip_ratio_threshold;

    PER_HEAP
    BOOL gen0_bricks_cleared;
    PER_HEAP
    int gen0_must_clear_bricks;

    PER_HEAP_ISOLATED
    bool maxgen_size_inc_p;

    PER_HEAP_ISOLATED
    size_t full_gc_counts[gc_type_max];

    // the # of bytes allocates since the last full compacting GC.
    PER_HEAP
    uint64_t loh_alloc_since_cg;

    PER_HEAP
    BOOL elevation_requested;

    // if this is TRUE, we should always guarantee that we do a
    // full compacting GC before we OOM.
    PER_HEAP
    BOOL last_gc_before_oom;

    PER_HEAP_ISOLATED
    BOOL should_expand_in_full_gc;

    // When we decide if we should expand the heap or not, we are
    // fine NOT to expand if we find enough free space in gen0's free
    // list or end of seg and we check this in decide_on_compacting.
    // This is an expensive check so we just record the fact and not
    // need to check in the allocator again.
    PER_HEAP
    BOOL sufficient_gen0_space_p;

#ifdef MULTIPLE_HEAPS
    PER_HEAP
    bool gen0_allocated_after_gc_p;
#endif //MULTIPLE_HEAPS

    // A provisional mode means we could change our mind in the middle of a GC
    // and want to do a different GC instead.
    //
    // Right now there's only one such case which is in the middle of a gen1
    // GC we want to do a blocking gen2 instead. If/When we have more we should
    // have an enum that tells us which case in this provisional mode
    // we are in.
    //
    // When this mode is triggered, our current (only) condition says
    // we have high fragmentation in gen2 even after we do a compacting
    // full GC which is an indication of heavy pinning in gen2. In this
    // case we never do BGCs, we just do either gen0 or gen1's till a
    // gen1 needs to increase the gen2 size, in which case we finish up
    // the current gen1 as a sweeping GC and immediately do a compacting
    // full GC instead (without restarting EE).
    PER_HEAP_ISOLATED
    bool provisional_mode_triggered;

    PER_HEAP_ISOLATED
    bool pm_trigger_full_gc;

    // For testing only BEG
    // pm_stress_on currently means (since we just have one mode) we
    // randomly turn the mode on; and after a random # of NGC2s we
    // turn it off.
    // NOTE that this means concurrent will be disabled so we can
    // simulate what this mode is supposed to be used.
    PER_HEAP_ISOLATED
    bool pm_stress_on;

    PER_HEAP_ISOLATED
    size_t provisional_triggered_gc_count;

    PER_HEAP_ISOLATED
    size_t provisional_off_gc_count;
    // For testing only END

    PER_HEAP_ISOLATED
    size_t num_provisional_triggered;

    PER_HEAP
    size_t allocated_since_last_gc[gc_oh_num::total_oh_count - 1];

#ifdef BACKGROUND_GC
    PER_HEAP_ISOLATED
    size_t ephemeral_fgc_counts[max_generation];

    PER_HEAP_ISOLATED
    BOOL alloc_wait_event_p;

    PER_HEAP
    uint8_t* next_sweep_obj;

    PER_HEAP
    uint8_t* current_sweep_pos;

#ifdef DOUBLY_LINKED_FL
    PER_HEAP
    heap_segment* current_sweep_seg;
#endif //DOUBLY_LINKED_FL
#endif //BACKGROUND_GC

    PER_HEAP
    fgm_history fgm_result;

    PER_HEAP_ISOLATED
    size_t eph_gen_starts_size;

#ifdef GC_CONFIG_DRIVEN
    // 0 stores compacting GCs;
    // 1 stores sweeping GCs;
    PER_HEAP_ISOLATED
    size_t compact_or_sweep_gcs[2];

    PER_HEAP
    size_t interesting_data_per_gc[max_idp_count];
#endif //GC_CONFIG_DRIVEN

    PER_HEAP
    BOOL        ro_segments_in_range;

#ifdef BACKGROUND_GC
    PER_HEAP
    heap_segment* freeable_soh_segment;
#endif //BACKGROUND_GC

    PER_HEAP
    heap_segment* freeable_uoh_segment;

    PER_HEAP_ISOLATED
    heap_segment* segment_standby_list;

    PER_HEAP
    size_t ordered_free_space_indices[MAX_NUM_BUCKETS];

    PER_HEAP
    size_t saved_ordered_free_space_indices[MAX_NUM_BUCKETS];

    PER_HEAP
    size_t ordered_plug_indices[MAX_NUM_BUCKETS];

    PER_HEAP
    size_t saved_ordered_plug_indices[MAX_NUM_BUCKETS];

    PER_HEAP
    BOOL ordered_plug_indices_init;

    PER_HEAP
    BOOL use_bestfit;

    PER_HEAP
    uint8_t* bestfit_first_pin;

    PER_HEAP
    BOOL commit_end_of_seg;

    PER_HEAP
    size_t max_free_space_items; // dynamically adjusted.

    PER_HEAP
    size_t free_space_buckets;

    PER_HEAP
    size_t free_space_items;

    // -1 means we are using all the free
    // spaces we have (not including
    // end of seg space).
    PER_HEAP
    int trimmed_free_space_index;

    PER_HEAP
    size_t total_ephemeral_plugs;

    PER_HEAP
    seg_free_spaces* bestfit_seg;

    // Note: we know this from the plan phase.
    // total_ephemeral_plugs actually has the same value
    // but while we are calculating its value we also store
    // info on how big the plugs are for best fit which we
    // don't do in plan phase.
    // TODO: get rid of total_ephemeral_plugs.
    PER_HEAP
    size_t total_ephemeral_size;

public:

#ifdef HEAP_ANALYZE

    PER_HEAP_ISOLATED
    BOOL heap_analyze_enabled;

    PER_HEAP
    size_t internal_root_array_length;

    // next two fields are used to optimize the search for the object
    // enclosing the current reference handled by ha_mark_object_simple.
    PER_HEAP
    uint8_t*  current_obj;

    PER_HEAP
    size_t current_obj_size;

#endif //HEAP_ANALYZE

public:

    PER_HEAP
    int         condemned_generation_num;

    PER_HEAP
    BOOL        blocking_collection;

#ifdef MULTIPLE_HEAPS
    static
    int n_heaps;

    static
    gc_heap** g_heaps;

#if !defined(USE_REGIONS) || defined(_DEBUG)
    static
    size_t*   g_promoted;
#endif //!USE_REGIONS || _DEBUG

#ifdef BACKGROUND_GC
    static
    size_t*   g_bpromoted;
#endif //BACKGROUND_GC
#ifdef MH_SC_MARK
    PER_HEAP_ISOLATED
    int*  g_mark_stack_busy;
#endif //MH_SC_MARK
#else
#if !defined(USE_REGIONS) || defined(_DEBUG)
    static
    size_t    g_promoted;
#endif //!USE_REGIONS || _DEBUG
#ifdef BACKGROUND_GC
    static
    size_t    g_bpromoted;
#endif //BACKGROUND_GC
#endif //MULTIPLE_HEAPS

    static
    size_t reserved_memory;
    static
    size_t reserved_memory_limit;
    static
    BOOL      g_low_memory_status;

#ifdef FEATURE_CARD_MARKING_STEALING
    PER_HEAP
    VOLATILE(uint32_t)    card_mark_chunk_index_soh;

    PER_HEAP
    VOLATILE(bool)        card_mark_done_soh;

    PER_HEAP
    VOLATILE(uint32_t)    card_mark_chunk_index_loh;

    PER_HEAP
    VOLATILE(uint32_t)    card_mark_chunk_index_poh;

    PER_HEAP
    VOLATILE(bool)        card_mark_done_uoh;

    PER_HEAP
    void reset_card_marking_enumerators()
    {
        // set chunk index to all 1 bits so that incrementing it yields 0 as the first index
        card_mark_chunk_index_soh = ~0;
        card_mark_done_soh = false;

        card_mark_chunk_index_loh = ~0;
        card_mark_chunk_index_poh = ~0;
        card_mark_done_uoh = false;
    }

    PER_HEAP
    bool find_next_chunk(card_marking_enumerator& card_mark_enumerator, heap_segment* seg,
                         size_t& n_card_set, uint8_t*& start_address, uint8_t*& limit,
                         size_t& card, size_t& end_card, size_t& card_word_end);
#endif //FEATURE_CARD_MARKING_STEALING

protected:
    PER_HEAP
    void update_collection_counts ();
}; // class gc_heap

#define ASSERT_OFFSETS_MATCH(field) \
  static_assert(offsetof(dac_gc_heap, field) == offsetof(gc_heap, field), #field " offset mismatch")

#ifndef USE_REGIONS
#ifdef MULTIPLE_HEAPS
ASSERT_OFFSETS_MATCH(alloc_allocated);
ASSERT_OFFSETS_MATCH(ephemeral_heap_segment);
ASSERT_OFFSETS_MATCH(finalize_queue);
ASSERT_OFFSETS_MATCH(oom_info);
ASSERT_OFFSETS_MATCH(interesting_data_per_heap);
ASSERT_OFFSETS_MATCH(compact_reasons_per_heap);
ASSERT_OFFSETS_MATCH(expand_mechanisms_per_heap);
ASSERT_OFFSETS_MATCH(interesting_mechanism_bits_per_heap);
ASSERT_OFFSETS_MATCH(internal_root_array);
ASSERT_OFFSETS_MATCH(internal_root_array_index);
ASSERT_OFFSETS_MATCH(heap_analyze_success);
ASSERT_OFFSETS_MATCH(generation_table);
#endif // MULTIPLE_HEAPS
#endif //USE_REGIONS

#ifdef FEATURE_PREMORTEM_FINALIZATION
class CFinalize
{
#ifdef DACCESS_COMPILE
    friend class ::ClrDataAccess;
#endif // DACCESS_COMPILE

    friend class CFinalizeStaticAsserts;

private:

    //adjust the count and add a constant to add a segment
    static const int ExtraSegCount = 2;
    static const int FinalizerListSeg = total_generation_count + 1;
    static const int CriticalFinalizerListSeg = total_generation_count;
    //Does not correspond to a segment
    static const int FreeList = total_generation_count + ExtraSegCount;

    PTR_PTR_Object m_FillPointers[total_generation_count + ExtraSegCount];
    PTR_PTR_Object m_Array;
    PTR_PTR_Object m_EndArray;
    size_t   m_PromotedCount;

    VOLATILE(int32_t) lock;
#ifdef _DEBUG
    EEThreadId lockowner_threadid;
#endif // _DEBUG

    BOOL GrowArray();
    void MoveItem (Object** fromIndex,
                   unsigned int fromSeg,
                   unsigned int toSeg);

    inline PTR_PTR_Object& SegQueue (unsigned int Seg)
    {
        return (Seg ? m_FillPointers [Seg-1] : m_Array);
    }
    inline PTR_PTR_Object& SegQueueLimit (unsigned int Seg)
    {
        return m_FillPointers [Seg];
    }

    BOOL IsSegEmpty ( unsigned int i)
    {
        ASSERT ( (int)i < FreeList);
        return (SegQueueLimit(i) == SegQueue (i));

    }

public:
    ~CFinalize();
    bool Initialize();
    void EnterFinalizeLock();
    void LeaveFinalizeLock();
    bool RegisterForFinalization (int gen, Object* obj, size_t size=0);
    Object* GetNextFinalizableObject (BOOL only_non_critical=FALSE);
    BOOL ScanForFinalization (promote_func* fn, int gen,BOOL mark_only_p, gc_heap* hp);
    void RelocateFinalizationData (int gen, gc_heap* hp);
    void WalkFReachableObjects (fq_walk_fn fn);
    void GcScanRoots (promote_func* fn, int hn, ScanContext *pSC);
    void UpdatePromotedGenerations (int gen, BOOL gen_0_empty_p);
    size_t GetPromotedCount();

    //Methods used by the shutdown code to call every finalizer
    size_t GetNumberFinalizableObjects();

    void CheckFinalizerObjects();
};

class CFinalizeStaticAsserts {
    static_assert(dac_finalize_queue::ExtraSegCount == CFinalize::ExtraSegCount, "ExtraSegCount mismatch");
    static_assert(offsetof(dac_finalize_queue, m_FillPointers) == offsetof(CFinalize, m_FillPointers), "CFinalize layout mismatch");
};
#endif // FEATURE_PREMORTEM_FINALIZATION

inline
 size_t& dd_begin_data_size (dynamic_data* inst)
{
  return inst->begin_data_size;
}
inline
 size_t& dd_survived_size (dynamic_data* inst)
{
  return inst->survived_size;
}
#if defined (RESPECT_LARGE_ALIGNMENT) || defined (FEATURE_STRUCTALIGN)
inline
 size_t& dd_num_npinned_plugs(dynamic_data* inst)
{
  return inst->num_npinned_plugs;
}
#endif //RESPECT_LARGE_ALIGNMENT || FEATURE_STRUCTALIGN
inline
size_t& dd_pinned_survived_size (dynamic_data* inst)
{
  return inst->pinned_survived_size;
}
inline
size_t& dd_added_pinned_size (dynamic_data* inst)
{
  return inst->added_pinned_size;
}
inline
size_t& dd_artificial_pinned_survived_size (dynamic_data* inst)
{
  return inst->artificial_pinned_survived_size;
}
#ifdef SHORT_PLUGS
inline
size_t& dd_padding_size (dynamic_data* inst)
{
  return inst->padding_size;
}
#endif //SHORT_PLUGS
inline
 size_t& dd_current_size (dynamic_data* inst)
{
  return inst->current_size;
}
inline
float& dd_surv (dynamic_data* inst)
{
  return inst->surv;
}
inline
size_t& dd_freach_previous_promotion (dynamic_data* inst)
{
  return inst->freach_previous_promotion;
}
inline
size_t& dd_desired_allocation (dynamic_data* inst)
{
  return inst->desired_allocation;
}
inline
size_t& dd_collection_count (dynamic_data* inst)
{
    return inst->collection_count;
}
inline
size_t& dd_promoted_size (dynamic_data* inst)
{
    return inst->promoted_size;
}
inline
float& dd_limit (dynamic_data* inst)
{
  return inst->sdata->limit;
}
inline
float& dd_max_limit (dynamic_data* inst)
{
  return inst->sdata->max_limit;
}
inline
size_t& dd_max_size (dynamic_data* inst)
{
  return inst->sdata->max_size;
}
inline
size_t& dd_min_size (dynamic_data* inst)
{
  return inst->min_size;
}
inline
ptrdiff_t& dd_new_allocation (dynamic_data* inst)
{
  return inst->new_allocation;
}
inline
ptrdiff_t& dd_gc_new_allocation (dynamic_data* inst)
{
  return inst->gc_new_allocation;
}
inline
size_t& dd_fragmentation_limit (dynamic_data* inst)
{
  return inst->sdata->fragmentation_limit;
}
inline
float& dd_fragmentation_burden_limit (dynamic_data* inst)
{
  return inst->sdata->fragmentation_burden_limit;
}
inline
float dd_v_fragmentation_burden_limit (dynamic_data* inst)
{
  return (min (2*dd_fragmentation_burden_limit (inst), 0.75f));
}
inline
size_t& dd_fragmentation (dynamic_data* inst)
{
  return inst->fragmentation;
}
inline
size_t& dd_gc_clock (dynamic_data* inst)
{
  return inst->gc_clock;
}
inline
uint64_t& dd_time_clock (dynamic_data* inst)
{
  return inst->time_clock;
}
inline
uint64_t& dd_previous_time_clock (dynamic_data* inst)
{
    return inst->previous_time_clock;
}


inline
size_t& dd_gc_clock_interval (dynamic_data* inst)
{
  return inst->sdata->gc_clock;
}
inline
uint64_t& dd_time_clock_interval (dynamic_data* inst)
{
  return inst->sdata->time_clock;
}

inline
size_t& dd_gc_elapsed_time (dynamic_data* inst)
{
    return inst->gc_elapsed_time;
}

inline
float& dd_gc_speed (dynamic_data* inst)
{
    return inst->gc_speed;
}

inline
alloc_context* generation_alloc_context (generation* inst)
{
    return &(inst->allocation_context);
}

#ifndef USE_REGIONS
inline
uint8_t*& generation_allocation_start (generation* inst)
{
  return inst->allocation_start;
}
#endif //!USE_REGIONS
inline
uint8_t*& generation_allocation_pointer (generation* inst)
{
  return inst->allocation_context.alloc_ptr;
}
inline
uint8_t*& generation_allocation_limit (generation* inst)
{
  return inst->allocation_context.alloc_limit;
}
inline
allocator* generation_allocator (generation* inst)
{
    return &inst->free_list_allocator;
}

inline
PTR_heap_segment& generation_start_segment (generation* inst)
{
  return inst->start_segment;
}

#ifdef USE_REGIONS
inline
heap_segment*& generation_tail_region (generation* inst)
{
  return inst->tail_region;
}

inline
heap_segment*& generation_tail_ro_region (generation* inst)
{
  return inst->tail_ro_region;
}
#endif //USE_REGIONS

inline
heap_segment*& generation_allocation_segment (generation* inst)
{
  return inst->allocation_segment;
}
#ifndef USE_REGIONS
inline
uint8_t*& generation_plan_allocation_start (generation* inst)
{
  return inst->plan_allocation_start;
}
inline
size_t& generation_plan_allocation_start_size (generation* inst)
{
  return inst->plan_allocation_start_size;
}
#endif //!USE_REGIONS
inline
uint8_t*& generation_allocation_context_start_region (generation* inst)
{
  return inst->allocation_context_start_region;
}
inline
size_t& generation_free_list_space (generation* inst)
{
  return inst->free_list_space;
}
inline
size_t& generation_free_obj_space (generation* inst)
{
  return inst->free_obj_space;
}
inline
size_t& generation_allocation_size (generation* inst)
{
  return inst->allocation_size;
}

inline
size_t& generation_pinned_allocated (generation* inst)
{
    return inst->pinned_allocated;
}
inline
size_t& generation_pinned_allocation_sweep_size (generation* inst)
{
    return inst->pinned_allocation_sweep_size;
}
inline
size_t& generation_pinned_allocation_compact_size (generation* inst)
{
    return inst->pinned_allocation_compact_size;
}
inline
size_t&  generation_free_list_allocated (generation* inst)
{
    return inst->free_list_allocated;
}
inline
size_t&  generation_end_seg_allocated (generation* inst)
{
    return inst->end_seg_allocated;
}
inline
BOOL&  generation_allocate_end_seg_p (generation* inst)
{
    return inst->allocate_end_seg_p;
}
inline
size_t& generation_condemned_allocated (generation* inst)
{
    return inst->condemned_allocated;
}
inline
size_t& generation_sweep_allocated (generation* inst)
{
    return inst->sweep_allocated;
}
#ifdef DOUBLY_LINKED_FL
inline
BOOL&  generation_set_bgc_mark_bit_p (generation* inst)
{
    return inst->set_bgc_mark_bit_p;
}
inline
uint8_t*&  generation_last_free_list_allocated (generation* inst)
{
    return inst->last_free_list_allocated;
}
#endif //DOUBLY_LINKED_FL
#ifdef FREE_USAGE_STATS
inline
size_t& generation_pinned_free_obj_space (generation* inst)
{
    return inst->pinned_free_obj_space;
}
inline
size_t& generation_allocated_in_pinned_free (generation* inst)
{
    return inst->allocated_in_pinned_free;
}
inline
size_t& generation_allocated_since_last_pin (generation* inst)
{
    return inst->allocated_since_last_pin;
}
#endif //FREE_USAGE_STATS

inline
float generation_allocator_efficiency (generation* inst)
{
    if ((generation_free_list_allocated (inst) + generation_free_obj_space (inst)) != 0)
    {
        return ((float) (generation_free_list_allocated (inst)) / (float)(generation_free_list_allocated (inst) + generation_free_obj_space (inst)));
    }
    else
        return 0;
}
inline
size_t generation_unusable_fragmentation (generation* inst)
{
    return (size_t)(generation_free_obj_space (inst) +
                    (1.0f-generation_allocator_efficiency(inst))*generation_free_list_space (inst));
}

#define plug_skew           sizeof(ObjHeader)
// We always use USE_PADDING_TAIL when fitting so items on the free list should be
// twice the min_obj_size.
#define min_free_list       (2*min_obj_size)
#define min_free_item_no_prev  (min_obj_size+sizeof(uint8_t*))
struct plug
{
    uint8_t *  skew[plug_skew / sizeof(uint8_t *)];
};

class pair
{
public:
    short left;
    short right;
};

//Note that these encode the fact that plug_skew is a multiple of uint8_t*.
// Each of new field is prepended to the prior struct.

struct plug_and_pair
{
    pair        m_pair;
    plug        m_plug;
};

struct plug_and_reloc
{
    ptrdiff_t   reloc;
    pair        m_pair;
    plug        m_plug;
};

struct plug_and_gap
{
    ptrdiff_t   gap;
    ptrdiff_t   reloc;
    union
    {
        pair    m_pair;
        int     lr;  //for clearing the entire pair in one instruction
    };
    plug        m_plug;
};

struct gap_reloc_pair
{
    size_t gap;
    size_t   reloc;
    pair        m_pair;
};

#define min_pre_pin_obj_size (sizeof (gap_reloc_pair) + min_obj_size)

struct DECLSPEC_ALIGN(8) aligned_plug_and_gap
{
    plug_and_gap plugandgap;
};

struct loh_obj_and_pad
{
    ptrdiff_t   reloc;
    plug        m_plug;
};

struct loh_padding_obj
{
    uint8_t*    mt;
    size_t      len;
    ptrdiff_t   reloc;
    plug        m_plug;
};
#define loh_padding_obj_size (sizeof(loh_padding_obj))

//flags description
#define heap_segment_flags_readonly     1
#define heap_segment_flags_inrange      2
#define heap_segment_flags_loh          8

#ifdef BACKGROUND_GC
#define heap_segment_flags_swept        16
#define heap_segment_flags_decommitted  32
#define heap_segment_flags_ma_committed 64
// for segments whose mark array is only partially committed.
#define heap_segment_flags_ma_pcommitted 128
#define heap_segment_flags_uoh_delete   256
#endif //BACKGROUND_GC

#define heap_segment_flags_poh          512

#if defined(BACKGROUND_GC) && defined(USE_REGIONS)
// This means this seg needs to be processed by 
// BGC overflow when we process non concurrently.
#define heap_segment_flags_overflow      1024
#endif //BACKGROUND_GC && USE_REGIONS

#ifdef USE_REGIONS
#define heap_segment_flags_demoted       2048

struct generation_region_info
{
    heap_segment* head;
    heap_segment* tail;
};
#endif //USE_REGIONS

//need to be careful to keep enough pad items to fit a relocation node
//padded to QuadWord before the plug_skew

class heap_segment
{
public:
    // For regions allocated is used to indicate whether this is a valid segment
    // or not, ie, if it's 0 it means it's freed; else it's either a valid value
    // or a negative value which means it's in a large region.
    uint8_t*        allocated;
    uint8_t*        committed;
    // For regions This could be obtained from region_allocator as each 
    // busy block knows its size.
    uint8_t*        reserved;
    uint8_t*        used;
    // For regions this is the actual physical start + aligned_plug_and_gap.
    uint8_t*        mem;
    // Currently we are using 12 bits for flags, see "flags description" right above.
    size_t          flags;
    PTR_heap_segment next;
    uint8_t*        background_allocated;
#ifdef MULTIPLE_HEAPS
    gc_heap*        heap;
#ifdef _DEBUG
    uint8_t*        saved_committed;
    size_t          saved_desired_allocation;
#endif // _DEBUG
#endif //MULTIPLE_HEAPS
    uint8_t*        decommit_target;
    uint8_t*        plan_allocated;
    // In the plan phase we change the allocated for a seg but we need this
    // value to correctly calculate how much space we can reclaim in 
    // generation_fragmentation. But it's beneficial to truncate it as it
    // means in the later phases we only need to look up to the new allocated.
    uint8_t*        saved_allocated; 
    uint8_t*        saved_bg_allocated;
#ifdef USE_REGIONS
    // These generation numbers are initialized to -1.
    // For plan_gen_num:
    // for all regions in condemned generations it needs
    // to be re-initialized to -1 when a GC is done. 
    // When setting it we update the demotion decision accordingly.
    uint8_t         gen_num;
    // This says this region was already swept during plan and its bricks
    // were built to indicates objects, ie, not the way plan builds them. 
    // Other phases need to be aware of this so they don't assume bricks 
    // indicate tree nodes.
    //
    // swept_in_plan_p can be folded into gen_num.
    bool            swept_in_plan_p;
    int             plan_gen_num;
    int             survived;
    int             old_card_survived;
    int             pinned_survived;
    // This is currently only used by regions that are swept in plan -
    // we then thread this list onto the generation's free list.
    // We may keep per region free list later which requires more work.
    uint8_t*        free_list_head;
    uint8_t*        free_list_tail;

    // Fields that we need to provide in response to a
    // random address that might land anywhere on the region.
    // - heap
    // - gen_num
    // - plan_gen_num
    // which means if a region consists of multiple basic regions,
    // these will need to be populated for each basic region in the
    // seg mapping table.
    //
    // We can break this up into 2 data structures, one with the 
    // fields per basic region; the other with the rest of the fields.
    //
    // Could consider to have the region itself populated per basic
    // region but so far it doesn't seem necessary so I'll leave it 
    // out.
    void init_free_list()
    {
        free_list_head = 0;
        free_list_tail = 0;
    }

    void thread_free_obj (uint8_t* obj, size_t s);
#else //USE_REGIONS

#ifdef _MSC_VER
// Disable this warning - we intentionally want __declspec(align()) to insert padding for us
#pragma warning(disable:4324)  // structure was padded due to __declspec(align())
#endif
    // REGIONS TODO: we don't need this for regions - to be removed.
    aligned_plug_and_gap padandplug;
#ifdef _MSC_VER
#pragma warning(default:4324)  // structure was padded due to __declspec(align())
#endif
#endif //USE_REGIONS
};

#ifdef USE_REGIONS
// Region management
// 
// We reserve a big space for regions. We could consider including the GC bookkeeping data
// structures in this space too (eg, at the end and only commit the portion that's used to 
// cover the current regions). 
// 
// region_allocator is used to find where to put a region. When it finds a space to allocate
// a region in it will mark is as busy. Note that the actual commit operation for a region
// is not done by region_allocator - it's done as needed when GC actually stores objects there.
// 
// TODO: 
// When GC detects a region only containing dead objects, it does not immediately return this 
// region to region_allocator. It doesn't decommit anything from this region and stores this 
// region per heap as free_regions and free_large_regions respectively for SOH and UOH. When
// the memory pressure is high enough, we decommit an appropriate amount regions in free_regions
// and free_large_regions. These decommitted regions will be returned to region_allocator which
// mark the space as free blocks.
// 
#define LARGE_REGION_FACTOR (8)

#define region_alloc_free_bit (1 << (sizeof (uint32_t) * 8 - 1))

enum allocate_direction
{
    allocate_forward = 1,
    allocate_backward = -1,
};

// The big space we reserve for regions is divided into units of region_alignment.
// 
// SOH regions are all basic regions, meaning their size is the same as alignment. UOH regions 
// are by default 8x as large.
// 
// We use a map to encode info on these units. The map consists of an array of 32-bit uints.
// The encoding is the following:
// 
// If the MSB is not set, it means it's busy (in use); otherwise it means it's free.
//
// The value (without the MSB) indicates how many units to walk till we get to the next
// group of encoded bytes which is called a block.
// 
// For each region we encode the info with a busy block in the map. This block has the 
// same # of uints as the # of units this region occupies. And we store the # in 
// the starting uint. These uints can be converted to bytes since we have multiple units
// for larger regions anyway. I haven't done that since this will need to be changed in
// the near future based on more optimal allocation strategies.
//
// When we allocate, we search forward to find contiguous free units >= num_units
// We do take the opportunity to coalesce free blocks but we do not coalesce busy blocks.
// When we decommit a region, we simply mark its block free. Free blocks are coalesced 
// opportunistically when we need to walk them.
//
// TODO: to accommodate 32-bit processes, we reserve in segment sizes and divide each seg
// into regions. 
class region_allocator
{
private:

    uint8_t* global_region_start;
    uint8_t* global_region_end;
    uint8_t* global_region_left_used;
    uint8_t* global_region_right_used;

    uint32_t total_free_units;

    size_t region_alignment;
    size_t large_region_alignment;

    GCSpinLock region_allocator_lock;

    uint32_t* region_map_left_start;
    uint32_t* region_map_left_end;

    uint32_t* region_map_right_start;
    uint32_t* region_map_right_end;

    uint8_t* region_address_of (uint32_t* map_index);
    uint32_t* region_map_index_of (uint8_t* address);

    uint8_t* allocate (uint32_t num_units, allocate_direction direction);
    uint8_t* allocate_end (uint32_t num_units, allocate_direction direction);

    void enter_spin_lock();
    void leave_spin_lock();

    void make_busy_block (uint32_t* index_start, uint32_t num_units);
    void make_free_block (uint32_t* index_start, uint32_t num_units);

    void print_map (const char* msg);

    size_t align_region_up (size_t size)
    {
        return ((size + (region_alignment - 1)) & ~(region_alignment - 1));
    }

    size_t align_region_down (size_t size)
    {
        return (size & ~(region_alignment - 1));
    }

    size_t is_region_aligned (uint8_t* address)
    {
        return ((size_t)address == ((size_t)address & ~(region_alignment - 1)));
    }

    bool is_unit_memory_free (uint32_t val)
    {
        return !!(val & region_alloc_free_bit);
    }

    uint32_t get_num_units (uint32_t val)
    {
        return (val & ~region_alloc_free_bit);
    }

public:
    bool init (uint8_t* start, uint8_t* end, size_t alignment, uint8_t** lowest, uint8_t** highest);
    bool allocate_region (size_t size, uint8_t** start, uint8_t** end, allocate_direction direction);
    bool allocate_basic_region (uint8_t** start, uint8_t** end);
    bool allocate_large_region (uint8_t** start, uint8_t** end, allocate_direction direction, size_t size = 0);
    void delete_region (uint8_t* start);
    uint32_t get_va_memory_load()
    {
        return (uint32_t)(((global_region_left_used - global_region_start) + ((global_region_end - global_region_right_used)))* 100.0
                          / (global_region_end - global_region_start));
    }
    // Returns the amount of free space + end of unused region space
    size_t get_free() { return (total_free_units * region_alignment) ; }
    size_t get_region_alignment () { return region_alignment; }
    size_t get_large_region_alignment () { return large_region_alignment; }
};
#endif //USE_REGIONS

#define ro_in_entry 0x1

// Note that I am storing both h0 and seg0, even though in Server GC you can get to
// the heap* from the segment info. This is because heap_of needs to be really fast
// and we would not want yet another indirection.
//
// Note on USE_REGIONS - since ro segs will never be in range, we can simply return
// when an address is not between lowest and highest. Each entry in the seg_mapping
// table can indicate one of the following -
//
// - an rw seg
// - part of an rw seg
// - no seg 
//
// If it's part of an rw seg, meaning this is a large region, each basic region would
// store the info for fast access but when we need to get to the actual region info
// we go back to the first basic region.
struct seg_mapping
{
#ifdef USE_REGIONS
    heap_segment region_info;
#else
    // if an address is > boundary it belongs to h1; else h0.
    // since we init h0 and h1 to 0, if we get 0 it means that
    // address doesn't exist on managed segments. And heap_of
    // would just return heap0 which is what it does now.
    uint8_t* boundary;
#ifdef MULTIPLE_HEAPS
    gc_heap* h0;
    gc_heap* h1;
#endif //MULTIPLE_HEAPS
    // You could have an address that's inbetween 2 segments and
    // this would return a seg, the caller then will use
    // in_range_for_segment to determine if it's on that seg.
    heap_segment* seg0; // this is what the seg for h0 is.
    heap_segment* seg1; // this is what the seg for h1 is.
    // Note that when frozen objects are used we mask seg1
    // with 0x1 to indicate that there is a ro segment for
    // this entry.
#endif //USE_REGIONS
};

static_assert(offsetof(dac_heap_segment, allocated) == offsetof(heap_segment, allocated), "DAC heap segment layout mismatch");
static_assert(offsetof(dac_heap_segment, committed) == offsetof(heap_segment, committed), "DAC heap segment layout mismatch");
static_assert(offsetof(dac_heap_segment, reserved) == offsetof(heap_segment, reserved), "DAC heap segment layout mismatch");
static_assert(offsetof(dac_heap_segment, used) == offsetof(heap_segment, used), "DAC heap segment layout mismatch");
static_assert(offsetof(dac_heap_segment, mem) == offsetof(heap_segment, mem), "DAC heap segment layout mismatch");
static_assert(offsetof(dac_heap_segment, flags) == offsetof(heap_segment, flags), "DAC heap segment layout mismatch");
static_assert(offsetof(dac_heap_segment, next) == offsetof(heap_segment, next), "DAC heap segment layout mismatch");
static_assert(offsetof(dac_heap_segment, background_allocated) == offsetof(heap_segment, background_allocated), "DAC heap segment layout mismatch");
#ifdef MULTIPLE_HEAPS
static_assert(offsetof(dac_heap_segment, heap) == offsetof(heap_segment, heap), "DAC heap segment layout mismatch");
#endif // MULTIPLE_HEAPS

inline
uint8_t*& heap_segment_reserved (heap_segment* inst)
{
  return inst->reserved;
}
inline
uint8_t*& heap_segment_committed (heap_segment* inst)
{
  return inst->committed;
}
inline
uint8_t*& heap_segment_decommit_target (heap_segment* inst)
{
    return inst->decommit_target;
}
inline
uint8_t*& heap_segment_used (heap_segment* inst)
{
  return inst->used;
}
inline
uint8_t*& heap_segment_allocated (heap_segment* inst)
{
  return inst->allocated;
}

inline
BOOL heap_segment_read_only_p (heap_segment* inst)
{
    return ((inst->flags & heap_segment_flags_readonly) != 0);
}

inline
BOOL heap_segment_in_range_p (heap_segment* inst)
{
    return (!(inst->flags & heap_segment_flags_readonly) ||
            ((inst->flags & heap_segment_flags_inrange) != 0));
}

inline
BOOL heap_segment_loh_p (heap_segment* inst)
{
    return !!(inst->flags & heap_segment_flags_loh);
}

inline
BOOL heap_segment_poh_p (heap_segment* inst)
{
    return !!(inst->flags & heap_segment_flags_poh);
}

inline
BOOL heap_segment_uoh_p (heap_segment* inst)
{
    return !!(inst->flags & (heap_segment_flags_loh | heap_segment_flags_poh));
}

inline gc_oh_num heap_segment_oh (heap_segment * inst)
{
    if ((inst->flags & heap_segment_flags_loh) != 0)
    {
        return gc_oh_num::loh;
    }
    else if ((inst->flags & heap_segment_flags_poh) != 0)
    {
        return gc_oh_num::poh;
    }
    else
    {
        return gc_oh_num::soh;
    }
}

#ifdef BACKGROUND_GC
#ifdef USE_REGIONS
inline
bool heap_segment_overflow_p (heap_segment* inst)
{
    return ((inst->flags & heap_segment_flags_overflow) != 0);
}
#endif //USE_REGIONS

inline
BOOL heap_segment_decommitted_p (heap_segment * inst)
{
    return !!(inst->flags & heap_segment_flags_decommitted);
}
inline
BOOL heap_segment_swept_p (heap_segment * inst)
{
    return !!(inst->flags & heap_segment_flags_swept);
}
#endif //BACKGROUND_GC

inline
PTR_heap_segment & heap_segment_next (heap_segment* inst)
{
  return inst->next;
}
inline
uint8_t*& heap_segment_mem (heap_segment* inst)
{
  return inst->mem;
}
inline
uint8_t*& heap_segment_plan_allocated (heap_segment* inst)
{
  return inst->plan_allocated;
}
inline
uint8_t*& heap_segment_saved_allocated (heap_segment* inst)
{
  return inst->saved_allocated;
}
#ifdef BACKGROUND_GC
inline
uint8_t*& heap_segment_background_allocated (heap_segment* inst)
{
  return inst->background_allocated;
}
inline
uint8_t*& heap_segment_saved_bg_allocated (heap_segment* inst)
{
  return inst->saved_bg_allocated;
}
#endif //BACKGROUND_GC

#ifdef MULTIPLE_HEAPS
inline
gc_heap*& heap_segment_heap (heap_segment* inst)
{
    return inst->heap;
}
#endif //MULTIPLE_HEAPS

#ifdef USE_REGIONS
inline
uint8_t& heap_segment_gen_num (heap_segment* inst)
{
    return inst->gen_num;
}
inline
bool& heap_segment_swept_in_plan (heap_segment* inst)
{
    return inst->swept_in_plan_p;
}
inline
int& heap_segment_plan_gen_num (heap_segment* inst)
{
    return inst->plan_gen_num;
}
inline
int& heap_segment_survived (heap_segment* inst)
{
    return inst->survived;
}
inline
int& heap_segment_old_card_survived (heap_segment* inst)
{
    return inst->old_card_survived;
}
inline
int& heap_segment_pinned_survived (heap_segment* inst)
{
    return inst->pinned_survived;
}
inline
uint8_t* heap_segment_free_list_head (heap_segment* inst)
{
    return inst->free_list_head;
}
inline
uint8_t* heap_segment_free_list_tail (heap_segment* inst)
{
    return inst->free_list_tail;
}
inline
bool heap_segment_demoted_p (heap_segment* inst)
{
    return ((inst->flags & heap_segment_flags_demoted) != 0);
}
#endif //USE_REGIONS

inline
generation* gc_heap::generation_of (int  n)
{
    assert (((n < total_generation_count) && (n >= 0)));
    return &generation_table [ n ];
}

inline
dynamic_data* gc_heap::dynamic_data_of (int gen_number)
{
    return &dynamic_data_table [ gen_number ];
}

#define GC_PAGE_SIZE 0x1000

#define card_word_width ((size_t)32)

//
// The value of card_size is determined empirically according to the average size of an object
// In the code we also rely on the assumption that one card_table entry (uint32_t) covers an entire os page
//
#if defined (HOST_64BIT)
#define card_size ((size_t)(2*GC_PAGE_SIZE/card_word_width))
#else
#define card_size ((size_t)(GC_PAGE_SIZE/card_word_width))
#endif // HOST_64BIT

inline
size_t card_word (size_t card)
{
    return card / card_word_width;
}

inline
unsigned card_bit (size_t card)
{
    return (unsigned)(card % card_word_width);
}

inline
size_t gcard_of (uint8_t* object)
{
    return (size_t)(object) / card_size;
}
#ifdef FEATURE_CARD_MARKING_STEALING
// make this 8 card bundle bits (2 MB in 64-bit architectures, 1 MB in 32-bit) - should be at least 1 card bundle bit
#define CARD_MARKING_STEALING_GRANULARITY (card_size*card_word_width*card_bundle_size*8)

// REGIONS TODO: this shouldn't need gc_low for regions.
#define THIS_ARG    , __this
class card_marking_enumerator
{
private:
    heap_segment*       segment;
    uint8_t*            gc_low;
    uint32_t            segment_start_chunk_index;
    VOLATILE(uint32_t)* chunk_index_counter;
    uint8_t*            chunk_high;
    uint32_t            old_chunk_index;
    static const uint32_t INVALID_CHUNK_INDEX = ~0u;

public:
    card_marking_enumerator(heap_segment* seg, uint8_t* low, VOLATILE(uint32_t)* counter) :
        segment(seg), gc_low(low), segment_start_chunk_index(0), chunk_index_counter(counter), chunk_high(nullptr), old_chunk_index(INVALID_CHUNK_INDEX)
    {
    }

    // move to the next chunk in this segment - return false if no more chunks in this segment
    bool move_next(heap_segment* seg, uint8_t*& low, uint8_t*& high);

    void exhaust_segment(heap_segment* seg)
    {
        uint8_t* low;
        uint8_t* high;
        // make sure no more chunks in this segment - do this via move_next because we want to keep
        // incrementing the chunk_index_counter rather than updating it via interlocked compare exchange
        while (move_next(seg, low, high))
            ;
    }

#ifdef USE_REGIONS
    void switch_to_segment(heap_segment* seg)
    {
        assert(segment == nullptr);
        segment = seg;
    }
#endif

    uint8_t* get_chunk_high()
    {
        return chunk_high;
    }
};
#else
#define THIS_ARG
#endif // FEATURE_CARD_MARKING_STEALING
