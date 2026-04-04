// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// #Overview
//
// GC automatically manages memory allocated by managed code.
// The design doc for GC can be found at docs/design/coreclr/botr/garbage-collection.md
//
// This file includes both the code for GC and the allocator. The most common
// case for a GC to be triggered is from the allocator code. See
// code:#try_allocate_more_space where it calls GarbageCollectGeneration.
//
// Entry points for the allocator are GCHeap::Alloc* which are called by the
// allocation helpers in gcscan.cpp
//

#include "common.h"
#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"
#include "gcdesc.h"
#include "softwarewritewatch.h"
#include "handletable.h"
#include "handletable.inl"
#include "gcenv.inl"
#include "gceventstatus.h"
#include <minipal/memorybarrierprocesswide.h>

// If FEATURE_INTERPRETER is set, always enable the GC side of FEATURE_CONSERVATIVE_GC
#ifdef FEATURE_INTERPRETER
#ifndef FEATURE_CONSERVATIVE_GC
#define FEATURE_CONSERVATIVE_GC
#endif
#endif // FEATURE_INTERPRETER

#ifdef __INTELLISENSE__
#if defined(FEATURE_SVR_GC)

#define SERVER_GC 1

#else // defined(FEATURE_SVR_GC)

#ifdef SERVER_GC
#undef SERVER_GC
#endif

#endif // defined(FEATURE_SVR_GC)
#endif // __INTELLISENSE__

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
#include "vxsort/do_vxsort.h"
#define USE_VXSORT
#else
#define USE_INTROSORT
#endif // TARGET_AMD64 || TARGET_ARM64
#include "introsort.h"

#ifdef SERVER_GC
namespace SVR {
#else // SERVER_GC
namespace WKS {
#endif // SERVER_GC

#include "gcimpl.h"
#include "gcpriv.h"

#ifdef DACCESS_COMPILE
#error this source file should not be compiled with DACCESS_COMPILE!
#endif //DACCESS_COMPILE

// We just needed a simple random number generator for testing.
class gc_rand
{
public:
    static uint64_t x;

    static uint64_t get_rand()
    {
        x = (314159269*x+278281) & 0x7FFFFFFF;
        return x;
    }

    // obtain random number in the range 0 .. r-1
    static uint64_t get_rand(uint64_t r)
    {
        // require r >= 0
        uint64_t x = (uint64_t)((get_rand() * r) >> 31);
        return x;
    }
};

uint64_t gc_rand::x = 0;

#if defined(BACKGROUND_GC) && defined(FEATURE_EVENT_TRACE)
BOOL bgc_heap_walk_for_etw_p = FALSE;
#endif //BACKGROUND_GC && FEATURE_EVENT_TRACE

#define MAX_PTR ((uint8_t*)(~(ptrdiff_t)0))
#define commit_min_th (16*OS_PAGE_SIZE)

#define MIN_SOH_CROSS_GEN_REFS (400)
#define MIN_LOH_CROSS_GEN_REFS (800)

#ifdef SERVER_GC
#define partial_size_th 100
#define num_partial_refs 64
#else //SERVER_GC
#define partial_size_th 100
#define num_partial_refs 32
#endif //SERVER_GC

#define demotion_plug_len_th (6*1024*1024)

#ifdef USE_REGIONS
// If the survived / region_size is 90+%, we don't compact this region.
#define sip_surv_ratio_th (90)
// If the survived due to cards from old generations / region_size is 90+%,
// we don't compact this region, also we immediately promote it to gen2.
#define sip_old_card_surv_ratio_th (90)
#endif //USE_REGIONS

#ifdef HOST_64BIT
#define MARK_STACK_INITIAL_LENGTH 1024
#else
#define MARK_STACK_INITIAL_LENGTH 128
#endif // HOST_64BIT

#define LOH_PIN_QUEUE_LENGTH 100
#define LOH_PIN_DECAY 10

#define UOH_ALLOCATION_RETRY_MAX_COUNT 2

#define MAX_YP_SPIN_COUNT_UNIT 32768

uint32_t yp_spin_count_unit = 0;
uint32_t original_spin_count_unit = 0;
size_t loh_size_threshold = LARGE_OBJECT_SIZE;

#ifdef GC_CONFIG_DRIVEN
int compact_ratio = 0;
#endif //GC_CONFIG_DRIVEN

#ifdef FEATURE_SVR_GC
bool g_built_with_svr_gc = true;
#else
bool g_built_with_svr_gc = false;
#endif // FEATURE_SVR_GC

#if defined(BUILDENV_DEBUG)
uint8_t g_build_variant = 0;
#elif defined(BUILDENV_CHECKED)
uint8_t g_build_variant = 1;
#else
uint8_t g_build_variant = 2;
#endif //BUILDENV_DEBUG

VOLATILE(int32_t) g_no_gc_lock = -1;

#ifdef TRACE_GC
const char * const allocation_state_str[] = {
    "start",
    "can_allocate",
    "cant_allocate",
    "retry_allocate",
    "try_fit",
    "try_fit_new_seg",
    "try_fit_after_cg",
    "try_fit_after_bgc",
    "try_free_full_seg_in_bgc",
    "try_free_after_bgc",
    "try_seg_end",
    "acquire_seg",
    "acquire_seg_after_cg",
    "acquire_seg_after_bgc",
    "check_and_wait_for_bgc",
    "trigger_full_compact_gc",
    "trigger_ephemeral_gc",
    "trigger_2nd_ephemeral_gc",
    "check_retry_seg"
};

const char * const msl_take_state_str[] = {
    "get_large_seg",
    "bgc_loh_sweep",
    "wait_bgc",
    "block_gc",
    "clr_mem",
    "clr_large_mem",
    "t_eph_gc",
    "t_full_gc",
    "alloc_small",
    "alloc_large",
    "alloc_small_cant",
    "alloc_large_cant",
    "try_alloc",
    "try_budget"
};
#endif //TRACE_GC


// Keep this in sync with the definition of gc_reason
#if (defined(DT_LOG) || defined(TRACE_GC))
static const char* const str_gc_reasons[] =
{
    "alloc_soh",
    "induced",
    "lowmem",
    "empty",
    "alloc_loh",
    "oos_soh",
    "oos_loh",
    "induced_noforce",
    "gcstress",
    "induced_lowmem",
    "induced_compacting",
    "lowmemory_host",
    "pm_full_gc",
    "lowmemory_host_blocking"
};

static const char* const str_gc_pause_modes[] =
{
    "batch",
    "interactive",
    "low_latency",
    "sustained_low_latency",
    "no_gc"
};

static const char* const str_root_kinds[] = {
    "Stack",
    "FinalizeQueue",
    "Handles",
    "OlderGen",
    "SizedRef",
    "Overflow",
    "DependentHandles",
    "NewFQ",
    "Steal",
    "BGC"
};
#endif //DT_LOG || TRACE_GC

inline
BOOL is_induced (gc_reason reason)
{
    return ((reason == reason_induced) ||
            (reason == reason_induced_noforce) ||
            (reason == reason_lowmemory) ||
            (reason == reason_lowmemory_blocking) ||
            (reason == reason_induced_compacting) ||
            (reason == reason_induced_aggressive) ||
            (reason == reason_lowmemory_host) ||
            (reason == reason_lowmemory_host_blocking));
}


gc_oh_num gen_to_oh(int gen)
{
    switch (gen)
    {
        case soh_gen0:
            return gc_oh_num::soh;
        case soh_gen1:
            return gc_oh_num::soh;
        case soh_gen2:
            return gc_oh_num::soh;
        case loh_generation:
            return gc_oh_num::loh;
        case poh_generation:
            return gc_oh_num::poh;
        default:
            assert(false);
            return gc_oh_num::unknown;
    }
}

uint64_t qpf;
double qpf_ms;
double qpf_us;

uint64_t RawGetHighPrecisionTimeStamp()
{
    return (uint64_t)GCToOSInterface::QueryPerformanceCounter();
}

#ifdef BGC_SERVO_TUNING
bool gc_heap::bgc_tuning::enable_fl_tuning = false;
uint32_t gc_heap::bgc_tuning::memory_load_goal = 0;
uint32_t gc_heap::bgc_tuning::memory_load_goal_slack = 0;
uint64_t gc_heap::bgc_tuning::available_memory_goal = 0;
bool gc_heap::bgc_tuning::panic_activated_p = false;
double gc_heap::bgc_tuning::accu_error_panic = 0.0;
double gc_heap::bgc_tuning::above_goal_kp = 0.0;
double gc_heap::bgc_tuning::above_goal_ki = 0.0;
bool gc_heap::bgc_tuning::enable_kd = false;
bool gc_heap::bgc_tuning::enable_ki = false;
bool gc_heap::bgc_tuning::enable_smooth = false;
bool gc_heap::bgc_tuning::enable_tbh = false;
bool gc_heap::bgc_tuning::enable_ff = false;
bool gc_heap::bgc_tuning::enable_gradual_d = false;
double gc_heap::bgc_tuning::above_goal_kd = 0.0;
double gc_heap::bgc_tuning::above_goal_ff = 0.0;
double gc_heap::bgc_tuning::num_gen1s_smooth_factor = 0.0;
double gc_heap::bgc_tuning::ml_kp = 0.0;
double gc_heap::bgc_tuning::ml_ki = 0.0;
double gc_heap::bgc_tuning::accu_error = 0.0;

bool gc_heap::bgc_tuning::fl_tuning_triggered = false;

size_t gc_heap::bgc_tuning::num_bgcs_since_tuning_trigger = 0;

bool gc_heap::bgc_tuning::next_bgc_p = false;

size_t gc_heap::bgc_tuning::gen1_index_last_bgc_end;
size_t gc_heap::bgc_tuning::gen1_index_last_bgc_start;
size_t gc_heap::bgc_tuning::gen1_index_last_bgc_sweep;
size_t gc_heap::bgc_tuning::actual_num_gen1s_to_trigger;

gc_heap::bgc_tuning::tuning_calculation gc_heap::bgc_tuning::gen_calc[2];
gc_heap::bgc_tuning::tuning_stats gc_heap::bgc_tuning::gen_stats[2];
gc_heap::bgc_tuning::bgc_size_data gc_heap::bgc_tuning::current_bgc_end_data[2];

size_t gc_heap::bgc_tuning::last_stepping_bgc_count = 0;
uint32_t gc_heap::bgc_tuning::last_stepping_mem_load = 0;
uint32_t gc_heap::bgc_tuning::stepping_interval = 0;
bool gc_heap::bgc_tuning::use_stepping_trigger_p = true;
double gc_heap::bgc_tuning::gen2_ratio_correction = 0.0;
double gc_heap::bgc_tuning::ratio_correction_step = 0.0;

int gc_heap::saved_bgc_tuning_reason = -1;
#endif //BGC_SERVO_TUNING

inline
size_t round_up_power2 (size_t size)
{
    // Get the 0-based index of the most-significant bit in size-1.
    // If the call failed (because size-1 is zero), size must be 1,
    // so return 1 (because 1 rounds up to itself).
    DWORD highest_set_bit_index;
    if (0 ==
#ifdef HOST_64BIT
        BitScanReverse64(
#else
        BitScanReverse(
#endif
            &highest_set_bit_index, size - 1)) { return 1; }

    // The size == 0 case (which would have overflowed to SIZE_MAX when decremented)
    // is handled below by relying on the fact that highest_set_bit_index is the maximum value
    // (31 or 63, depending on sizeof(size_t)) and left-shifting a value >= 2 by that
    // number of bits shifts in zeros from the right, resulting in an output of zero.
    return static_cast<size_t>(2) << highest_set_bit_index;
}

inline
size_t round_down_power2 (size_t size)
{
    // Get the 0-based index of the most-significant bit in size.
    // If the call failed, size must be zero so return zero.
    DWORD highest_set_bit_index;
    if (0 ==
#ifdef HOST_64BIT
        BitScanReverse64(
#else
        BitScanReverse(
#endif
            &highest_set_bit_index, size)) { return 0; }

    // Left-shift 1 by highest_set_bit_index to get back a value containing only
    // the most-significant set bit of size, i.e. size rounded down
    // to the next power-of-two value.
    return static_cast<size_t>(1) << highest_set_bit_index;
}

// Get the 0-based index of the most-significant bit in the value.
// Returns -1 if the input value is zero (i.e. has no set bits).
inline
int index_of_highest_set_bit (size_t value)
{
    // Get the 0-based index of the most-significant bit in the value.
    // If the call failed (because value is zero), return -1.
    DWORD highest_set_bit_index;
    return (0 ==
#ifdef HOST_64BIT
        BitScanReverse64(
#else
        BitScanReverse(
#endif
            &highest_set_bit_index, value)) ? -1 : static_cast<int>(highest_set_bit_index);
}


inline
float mb (size_t num)
{
    return (float)((float)num / 1000.0 / 1000.0);
}

inline
size_t gib (size_t num)
{
    return (num / 1024 / 1024 / 1024);
}

#ifdef BACKGROUND_GC
uint32_t bgc_alloc_spin_count = 140;
uint32_t bgc_alloc_spin = 2;

// The following 2 ratios dictate how UOH allocations that happen during a BGC should be handled. Because
// UOH is not collected till the very end of a BGC, by default we don't want to allow UOH to grow too large
// during a BGC. So if we only increase the size by 10%, we will allow to allocate normally. But if it's
// too much (ie, > bgc_uoh_inc_ratio_alloc_wait), we will make the allocation wait till the BGC is done.
//
// This means threads that allocate heavily on UOH may be paused during a BGC. If you're willing to accept
// larger UOH sizes in exchange for fewer pauses, you can use the UOHWaitBGCSizeIncPercent config to increase
// the wait ratio. Likewise, set it to use a smaller ratio if you observe that UOH grows too large during
// BGCs.
float bgc_uoh_inc_ratio_alloc_normal = 0.1f;
// This ratio is 2x for regions because regions could start with a much smaller size since a lot of
// memory could be in the free pool.
#ifdef USE_REGIONS
float bgc_uoh_inc_ratio_alloc_wait = 2.0f;
#else
float bgc_uoh_inc_ratio_alloc_wait = 1.0f;
#endif //USE_REGIONS

inline
void c_write (uint32_t& place, uint32_t value)
{
    Interlocked::Exchange (&place, value);
}

// If every heap's gen2 or gen3 size is less than this threshold we will do a blocking GC.
const size_t bgc_min_per_heap = 4*1024*1024;

int gc_heap::gchist_index = 0;
gc_mechanisms_store gc_heap::gchist[max_history_count];

#ifndef MULTIPLE_HEAPS
VOLATILE(bgc_state) gc_heap::current_bgc_state = bgc_not_in_process;
int gc_heap::gchist_index_per_heap = 0;
gc_heap::gc_history gc_heap::gchist_per_heap[max_history_count];
#endif //MULTIPLE_HEAPS
#endif //BACKGROUND_GC

#ifdef GC_CONFIG_DRIVEN

BOOL   gc_config_log_on = FALSE;
FILE* gc_config_log = NULL;

// we keep this much in a buffer and only flush when the buffer is full
#define gc_config_log_buffer_size (1*1024) // TEMP
uint8_t* gc_config_log_buffer = 0;
size_t gc_config_log_buffer_offset = 0;

// For config since we log so little we keep the whole history. Also it's only
// ever logged by one thread so no need to synchronize.
void log_va_msg_config(const char *fmt, va_list args)
{
    const int BUFFERSIZE = 256;
    static char rgchBuffer[BUFFERSIZE];
    char *  pBuffer  = &rgchBuffer[0];

    pBuffer[0] = '\n';
    int buffer_start = 1;
    int msg_len = _vsnprintf_s (&pBuffer[buffer_start], BUFFERSIZE - buffer_start, _TRUNCATE, fmt, args );
    assert (msg_len != -1);
    msg_len += buffer_start;

    if ((gc_config_log_buffer_offset + msg_len) > gc_config_log_buffer_size)
    {
        fwrite(gc_config_log_buffer, gc_config_log_buffer_offset, 1, gc_config_log);
        fflush(gc_config_log);
        gc_config_log_buffer_offset = 0;
    }

    memcpy (gc_config_log_buffer + gc_config_log_buffer_offset, pBuffer, msg_len);
    gc_config_log_buffer_offset += msg_len;
}

void GCLogConfig (const char *fmt, ... )
{
    if (gc_config_log_on && (gc_config_log != NULL))
    {
        va_list     args;
        va_start( args, fmt );
        log_va_msg_config (fmt, args);
    }
}
#endif // GC_CONFIG_DRIVEN

#ifdef SYNCHRONIZATION_STATS
// Number of GCs have we done since we last logged.
static unsigned int         gc_count_during_log;
 // In ms. This is how often we print out stats.
static const unsigned int   log_interval = 5000;
// Time (in ms) when we start a new log interval.
static uint64_t             log_start_tick;
static unsigned int         gc_lock_contended;
static int64_t              log_start_hires;
// Cycles accumulated in SuspendEE during log_interval.
static uint64_t             suspend_ee_during_log;
// Cycles accumulated in RestartEE during log_interval.
static uint64_t             restart_ee_during_log;
static uint64_t             gc_during_log;
#endif //SYNCHRONIZATION_STATS


void
process_sync_log_stats()
{
#ifdef SYNCHRONIZATION_STATS

    uint64_t log_elapsed = GCToOSInterface::GetLowPrecisionTimeStamp() - log_start_tick;

    if (log_elapsed > log_interval)
    {
        uint64_t total = GCToOSInterface::QueryPerformanceCounter() - log_start_hires;
        // Print out the cycles we spent on average in each suspend and restart.
        printf("\n_________________________________________________________________________________\n"
            "Past %d(s): #%3d GCs; Total gc_lock contended: %8u; GC: %12u\n"
            "SuspendEE: %8u; RestartEE: %8u GC %.3f%%\n",
            log_interval / 1000,
            gc_count_during_log,
            gc_lock_contended,
            (unsigned int)(gc_during_log / gc_count_during_log),
            (unsigned int)(suspend_ee_during_log / gc_count_during_log),
            (unsigned int)(restart_ee_during_log / gc_count_during_log),
            (double)(100.0f * gc_during_log / total));
        gc_heap::print_sync_stats(gc_count_during_log);

        gc_count_during_log = 0;
    }
#endif //SYNCHRONIZATION_STATS
}

#ifdef MULTIPLE_HEAPS
uint32_t g_num_active_processors = 0;

// Note that when a join is no longer used we still keep the values here because
// tooling already recognized them as having the meaning they were assigned originally.
// It doesn't break tooling if we stop using them but does if we assign a new meaning
// to them.
enum gc_join_stage
{
    gc_join_init_cpu_mapping = 0,
    gc_join_done = 1,
    gc_join_generation_determined = 2,
    gc_join_begin_mark_phase = 3,
    gc_join_scan_dependent_handles = 4,
    gc_join_rescan_dependent_handles = 5,
    gc_join_scan_sizedref_done = 6,
    gc_join_null_dead_short_weak = 7,
    gc_join_scan_finalization = 8,
    gc_join_null_dead_long_weak = 9,
    gc_join_null_dead_syncblk = 10,
    gc_join_decide_on_compaction = 11,
    gc_join_rearrange_segs_compaction = 12,
    gc_join_adjust_handle_age_compact = 13,
    gc_join_adjust_handle_age_sweep = 14,
    gc_join_begin_relocate_phase = 15,
    gc_join_relocate_phase_done = 16,
    gc_join_verify_objects_done = 17,
    gc_join_start_bgc = 18,
    gc_join_restart_ee = 19,
    gc_join_concurrent_overflow = 20,
    gc_join_suspend_ee = 21,
    gc_join_bgc_after_ephemeral = 22,
    gc_join_allow_fgc = 23,
    gc_join_bgc_sweep = 24,
    gc_join_suspend_ee_verify = 25,
    gc_join_restart_ee_verify = 26,
    gc_join_set_state_free = 27,
    gc_r_join_update_card_bundle = 28,
    gc_join_after_absorb = 29,
    gc_join_verify_copy_table = 30,
    gc_join_after_reset = 31,
    gc_join_after_ephemeral_sweep = 32,
    gc_join_after_profiler_heap_walk = 33,
    gc_join_minimal_gc = 34,
    gc_join_after_commit_soh_no_gc = 35,
    gc_join_expand_loh_no_gc = 36,
    gc_join_final_no_gc = 37,
    // No longer in use but do not remove, see comments for this enum.
    gc_join_disable_software_write_watch = 38,
    gc_join_merge_temp_fl = 39,
    gc_join_bridge_processing = 40,
    gc_join_max = 41
};

enum gc_join_flavor
{
    join_flavor_server_gc = 0,
    join_flavor_bgc = 1
};

#define first_thread_arrived 2
#pragma warning(push)
#pragma warning(disable:4324) // don't complain if DECLSPEC_ALIGN actually pads
struct DECLSPEC_ALIGN(HS_CACHE_LINE_SIZE) join_structure
{
    // Shared non volatile keep on separate line to prevent eviction
    int n_threads;

    // Keep polling/wait structures on separate line write once per join
    DECLSPEC_ALIGN(HS_CACHE_LINE_SIZE)
    GCEvent joined_event[3]; // the last event in the array is only used for first_thread_arrived.
    Volatile<int> lock_color;
    VOLATILE(BOOL) wait_done;
    VOLATILE(BOOL) joined_p;

    // Keep volatile counted locks on separate cache line write many per join
    DECLSPEC_ALIGN(HS_CACHE_LINE_SIZE)
    VOLATILE(int) join_lock;
    VOLATILE(int) r_join_lock;

};
#pragma warning(pop)

enum join_type
{
    type_last_join = 0,
    type_join = 1,
    type_restart = 2,
    type_first_r_join = 3,
    type_r_join = 4
};

enum join_time
{
    time_start = 0,
    time_end = 1
};

enum join_heap_index
{
    join_heap_restart = 100,
    join_heap_r_restart = 200
};

class t_join
{
    join_structure join_struct;

    int id;
    gc_join_flavor flavor;

#ifdef JOIN_STATS
    uint64_t start[MAX_SUPPORTED_CPUS], end[MAX_SUPPORTED_CPUS], start_seq;
    // remember join id and last thread to arrive so restart can use these
    int thd;
    // we want to print statistics every 10 seconds - this is to remember the start of the 10 sec interval
    uint64_t start_tick;
    // counters for joins, in 1000's of clock cycles
    uint64_t elapsed_total[gc_join_max], wake_total[gc_join_max], seq_loss_total[gc_join_max], par_loss_total[gc_join_max], in_join_total[gc_join_max];
#endif //JOIN_STATS

public:
    BOOL init (int n_th, gc_join_flavor f)
    {
        dprintf (JOIN_LOG, ("Initializing join structure"));
        join_struct.n_threads = n_th;
        join_struct.lock_color = 0;
        for (int i = 0; i < 3; i++)
        {
            if (!join_struct.joined_event[i].IsValid())
            {
                join_struct.joined_p = FALSE;
                dprintf (JOIN_LOG, ("Creating join event %d", i));
                // TODO - changing this to a non OS event
                // because this is also used by BGC threads which are
                // managed threads and WaitEx does not allow you to wait
                // for an OS event on a managed thread.
                // But we are not sure if this plays well in the hosting
                // environment.
                //join_struct.joined_event[i].CreateOSManualEventNoThrow(FALSE);
                if (!join_struct.joined_event[i].CreateManualEventNoThrow(FALSE))
                    return FALSE;
            }
        }
        join_struct.join_lock = join_struct.n_threads;
        join_struct.r_join_lock = join_struct.n_threads;
        join_struct.wait_done = FALSE;
        flavor = f;

#ifdef JOIN_STATS
        start_tick = GCToOSInterface::GetLowPrecisionTimeStamp();
#endif //JOIN_STATS

        return TRUE;
    }

    void update_n_threads(int n_th)
    {
        join_struct.n_threads = n_th;
        join_struct.join_lock = n_th;
        join_struct.r_join_lock = n_th;
    }

    int get_num_threads()
    {
        return join_struct.n_threads;
    }

    // This is for instrumentation only.
    int get_join_lock()
    {
        return VolatileLoadWithoutBarrier (&join_struct.join_lock);
    }

    void destroy ()
    {
        dprintf (JOIN_LOG, ("Destroying join structure"));
        for (int i = 0; i < 3; i++)
        {
            if (join_struct.joined_event[i].IsValid())
                join_struct.joined_event[i].CloseEvent();
        }
    }

    inline void fire_event (int heap, join_time time, join_type type, int join_id)
    {
        FIRE_EVENT(GCJoin_V2, heap, time, type, join_id);
    }

    void join (gc_heap* gch, int join_id)
    {
#ifdef JOIN_STATS
        // parallel execution ends here
        end[gch->heap_number] = get_ts();
#endif //JOIN_STATS

        assert (!join_struct.joined_p);
        int color = join_struct.lock_color.LoadWithoutBarrier();

        if (Interlocked::Decrement(&join_struct.join_lock) != 0)
        {
            dprintf (JOIN_LOG, ("join%d(%d): Join() Waiting...join_lock is now %d",
                flavor, join_id, (int32_t)(join_struct.join_lock)));

            fire_event (gch->heap_number, time_start, type_join, join_id);

            //busy wait around the color
            if (color == join_struct.lock_color.LoadWithoutBarrier())
            {
respin:
                int spin_count = 128 * yp_spin_count_unit;
                for (int j = 0; j < spin_count; j++)
                {
                    if (color != join_struct.lock_color.LoadWithoutBarrier())
                    {
                        break;
                    }
                    YieldProcessor();           // indicate to the processor that we are spinning
                }

                // we've spun, and if color still hasn't changed, fall into hard wait
                if (color == join_struct.lock_color.LoadWithoutBarrier())
                {
                    dprintf (JOIN_LOG, ("join%d(%d): Join() hard wait on reset event %d, join_lock is now %d",
                        flavor, join_id, color, (int32_t)(join_struct.join_lock)));

                    uint32_t dwJoinWait = join_struct.joined_event[color].Wait(INFINITE, FALSE);

                    if (dwJoinWait != WAIT_OBJECT_0)
                    {
                        STRESS_LOG1 (LF_GC, LL_FATALERROR, "joined event wait failed with code: %zx", dwJoinWait);
                        FATAL_GC_ERROR ();
                    }
                }

                // avoid race due to the thread about to reset the event (occasionally) being preempted before ResetEvent()
                if (color == join_struct.lock_color.LoadWithoutBarrier())
                {
                    dprintf (9999, ("---h%d %d j%d %d - respin!!! (c:%d-%d)",
                        gch->heap_number, join_id, join_struct.n_threads, color, join_struct.lock_color.LoadWithoutBarrier()));
                    goto respin;
                }

                dprintf (JOIN_LOG, ("join%d(%d): Join() done, join_lock is %d",
                    flavor, join_id, (int32_t)(join_struct.join_lock)));
            }

            fire_event (gch->heap_number, time_end, type_join, join_id);

#ifdef JOIN_STATS
            // parallel execution starts here
            start[gch->heap_number] = get_ts();
            Interlocked::ExchangeAdd(&in_join_total[join_id], (start[gch->heap_number] - end[gch->heap_number]));
#endif //JOIN_STATS
        }
        else
        {
            fire_event (gch->heap_number, time_start, type_last_join, join_id);

            join_struct.joined_p = TRUE;
            dprintf (JOIN_LOG, ("join%d(%d): Last thread to complete the join, setting id", flavor, join_id));
            join_struct.joined_event[!color].Reset();
            id = join_id;
#ifdef JOIN_STATS
            // remember the join id, the last thread arriving, the start of the sequential phase,
            // and keep track of the cycles spent waiting in the join
            thd = gch->heap_number;
            start_seq = get_ts();
            Interlocked::ExchangeAdd(&in_join_total[join_id], (start_seq - end[gch->heap_number]));
#endif //JOIN_STATS
        }
    }

    // Reverse join - first thread gets here does the work; other threads will only proceed
    // after the work is done.
    // Note that you cannot call this twice in a row on the same thread. Plus there's no
    // need to call it twice in row - you should just merge the work.
    BOOL r_join (gc_heap* gch, int join_id)
    {

        if (join_struct.n_threads == 1)
        {
            return TRUE;
        }

        if (Interlocked::CompareExchange(&join_struct.r_join_lock, 0, join_struct.n_threads) == 0)
        {
            fire_event (gch->heap_number, time_start, type_join, join_id);

            dprintf (JOIN_LOG, ("r_join() Waiting..."));

            //busy wait around the color
respin:
            int spin_count = 256 * yp_spin_count_unit;
            for (int j = 0; j < spin_count; j++)
            {
                if (join_struct.wait_done)
                {
                    break;
                }
                YieldProcessor();           // indicate to the processor that we are spinning
            }

            // we've spun, and if color still hasn't changed, fall into hard wait
            if (!join_struct.wait_done)
            {
                dprintf (JOIN_LOG, ("Join() hard wait on reset event %d", first_thread_arrived));
                uint32_t dwJoinWait = join_struct.joined_event[first_thread_arrived].Wait(INFINITE, FALSE);
                if (dwJoinWait != WAIT_OBJECT_0)
                {
                    STRESS_LOG1 (LF_GC, LL_FATALERROR, "joined event wait failed with code: %zx", dwJoinWait);
                    FATAL_GC_ERROR ();
                }
            }

            // avoid race due to the thread about to reset the event (occasionally) being preempted before ResetEvent()
            if (!join_struct.wait_done)
            {
                goto respin;
            }

            dprintf (JOIN_LOG, ("r_join() done"));

            fire_event (gch->heap_number, time_end, type_join, join_id);

            return FALSE;
        }
        else
        {
            fire_event (gch->heap_number, time_start, type_first_r_join, join_id);
            return TRUE;
        }
    }

#ifdef JOIN_STATS
    uint64_t get_ts()
    {
        return GCToOSInterface::QueryPerformanceCounter();
    }

    void start_ts (gc_heap* gch)
    {
        // parallel execution ends here
        start[gch->heap_number] = get_ts();
    }
#endif //JOIN_STATS

    void restart()
    {
#ifdef JOIN_STATS
        uint64_t elapsed_seq = get_ts() - start_seq;
        uint64_t max = 0, sum = 0, wake = 0;
        uint64_t min_ts = start[0];
        for (int i = 1; i < join_struct.n_threads; i++)
        {
            if(min_ts > start[i]) min_ts = start[i];
        }

        for (int i = 0; i < join_struct.n_threads; i++)
        {
            uint64_t wake_delay = start[i] - min_ts;
            uint64_t elapsed = end[i] - start[i];
            if (max < elapsed)
                max = elapsed;
            sum += elapsed;
            wake += wake_delay;
        }
        uint64_t seq_loss = (join_struct.n_threads - 1)*elapsed_seq;
        uint64_t par_loss = join_struct.n_threads*max - sum;
        double efficiency = 0.0;
        if (max > 0)
            efficiency = sum*100.0/(join_struct.n_threads*max);

        const double ts_scale = 1e-6;

        // enable this printf to get statistics on each individual join as it occurs
        //printf("join #%3d  seq_loss = %5g   par_loss = %5g  efficiency = %3.0f%%\n", join_id, ts_scale*seq_loss, ts_scale*par_loss, efficiency);

        elapsed_total[id] += sum;
        wake_total[id] += wake;
        seq_loss_total[id] += seq_loss;
        par_loss_total[id] += par_loss;

        // every 10 seconds, print a summary of the time spent in each type of join
        if (GCToOSInterface::GetLowPrecisionTimeStamp() - start_tick > 10*1000)
        {
            printf("**** summary *****\n");
            for (int i = 0; i < 16; i++)
            {
                printf("join #%3d  elapsed_total = %8g wake_loss = %8g seq_loss = %8g  par_loss = %8g  in_join_total = %8g\n",
                   i,
                   ts_scale*elapsed_total[i],
                   ts_scale*wake_total[i],
                   ts_scale*seq_loss_total[i],
                   ts_scale*par_loss_total[i],
                   ts_scale*in_join_total[i]);
                elapsed_total[i] = wake_total[i] = seq_loss_total[i] = par_loss_total[i] = in_join_total[i] = 0;
            }
            start_tick = GCToOSInterface::GetLowPrecisionTimeStamp();
        }
#endif //JOIN_STATS

        fire_event (join_heap_restart, time_start, type_restart, -1);
        assert (join_struct.joined_p);
        join_struct.joined_p = FALSE;
        join_struct.join_lock = join_struct.n_threads;
        dprintf (JOIN_LOG, ("join%d(%d): Restarting from join: join_lock is %d", flavor, id, (int32_t)(join_struct.join_lock)));
        int color = join_struct.lock_color.LoadWithoutBarrier();
        join_struct.lock_color = !color;
        join_struct.joined_event[color].Set();

        fire_event (join_heap_restart, time_end, type_restart, -1);

#ifdef JOIN_STATS
        start[thd] = get_ts();
#endif //JOIN_STATS
    }

    BOOL joined()
    {
        dprintf (JOIN_LOG, ("join%d(%d): joined, join_lock is %d", flavor, id, (int32_t)(join_struct.join_lock)));
        return join_struct.joined_p;
    }

    void r_restart()
    {
        if (join_struct.n_threads != 1)
        {
            fire_event (join_heap_r_restart, time_start, type_restart, -1);
            join_struct.wait_done = TRUE;
            join_struct.joined_event[first_thread_arrived].Set();
            fire_event (join_heap_r_restart, time_end, type_restart, -1);
        }
    }

    void r_init()
    {
        if (join_struct.n_threads != 1)
        {
            join_struct.r_join_lock = join_struct.n_threads;
            join_struct.wait_done = FALSE;
            join_struct.joined_event[first_thread_arrived].Reset();
        }
    }
};

t_join gc_t_join;

#ifdef BACKGROUND_GC
t_join bgc_t_join;
#endif //BACKGROUND_GC

#endif //MULTIPLE_HEAPS

#define spin_and_switch(count_to_spin, expr) \
{ \
    for (int j = 0; j < count_to_spin; j++) \
    { \
        if (expr) \
        { \
            break;\
        } \
        YieldProcessor(); \
    } \
    if (!(expr)) \
    { \
        GCToOSInterface::YieldThread(0); \
    } \
}

#define spin_and_wait(count_to_spin, expr) \
{ \
    while (!expr) \
    { \
        for (int j = 0; j < count_to_spin; j++) \
        { \
            if (expr) \
            { \
                break; \
            } \
                YieldProcessor (); \
        } \
        if (!(expr)) \
        { \
            GCToOSInterface::YieldThread (0); \
        } \
    } \
}

#ifdef BACKGROUND_GC

#define max_pending_allocs 64

class exclusive_sync
{
    VOLATILE(uint8_t*) rwp_object;
    VOLATILE(int32_t) needs_checking;

    int spin_count;

    uint8_t cache_separator[HS_CACHE_LINE_SIZE - (sizeof (spin_count) + sizeof (needs_checking) + sizeof (rwp_object))];

    // TODO - perhaps each object should be on its own cache line...
    VOLATILE(uint8_t*) alloc_objects[max_pending_allocs];

    int find_free_index ()
    {
        for (int i = 0; i < max_pending_allocs; i++)
        {
            if (alloc_objects [i] == (uint8_t*)0)
            {
                return i;
            }
        }

        return -1;
    }

public:
    void init()
    {
        spin_count = 32 * (g_num_processors - 1);
        rwp_object = 0;
        needs_checking = 0;
        for (int i = 0; i < max_pending_allocs; i++)
        {
            alloc_objects [i] = (uint8_t*)0;
        }
    }

    void check()
    {
        for (int i = 0; i < max_pending_allocs; i++)
        {
            if (alloc_objects [i] != (uint8_t*)0)
            {
                FATAL_GC_ERROR();
            }
        }
    }

    void bgc_mark_set (uint8_t* obj)
    {
        dprintf (3, ("cm: probing %p", obj));
retry:
        if (Interlocked::CompareExchange(&needs_checking, 1, 0) == 0)
        {
            // If we spend too much time spending all the allocs,
            // consider adding a high water mark and scan up
            // to that; we'll need to interlock in done when
            // we update the high watermark.
            for (int i = 0; i < max_pending_allocs; i++)
            {
                if (obj == alloc_objects[i])
                {
                    needs_checking = 0;
                    dprintf (3, ("cm: will spin"));
                    spin_and_switch (spin_count, (obj != alloc_objects[i]));
                    goto retry;
                }
            }

            rwp_object = obj;
            needs_checking = 0;
            dprintf (3, ("cm: set %p", obj));
            return;
        }
        else
        {
            spin_and_switch (spin_count, (needs_checking == 0));
            goto retry;
        }
    }

    int uoh_alloc_set (uint8_t* obj)
    {
        if (!gc_heap::cm_in_progress)
        {
            return -1;
        }

retry:
        dprintf (3, ("uoh alloc: probing %p", obj));

        if (Interlocked::CompareExchange(&needs_checking, 1, 0) == 0)
        {
            if (obj == rwp_object)
            {
                needs_checking = 0;
                spin_and_switch (spin_count, (obj != rwp_object));
                goto retry;
            }
            else
            {
                int cookie = find_free_index();

                if (cookie != -1)
                {
                    alloc_objects[cookie] = obj;
                    needs_checking = 0;
                    //if (cookie >= 4)
                    //{
                    //    GCToOSInterface::DebugBreak();
                    //}

                    dprintf (3, ("uoh alloc: set %p at %d", obj, cookie));
                    return cookie;
                }
                else
                {
                    needs_checking = 0;
                    dprintf (3, ("uoh alloc: setting %p will spin to acquire a free index", obj));
                    spin_and_switch (spin_count, (find_free_index () != -1));
                    goto retry;
                }
            }
        }
        else
        {
            dprintf (3, ("uoh alloc: will spin on checking %p", obj));
            spin_and_switch (spin_count, (needs_checking == 0));
            goto retry;
        }
    }

    void bgc_mark_done ()
    {
        dprintf (3, ("cm: release lock on %p", (uint8_t *)rwp_object));
        rwp_object = 0;
    }

    void uoh_alloc_done_with_index (int index)
    {
        dprintf (3, ("uoh alloc: release lock on %p based on %d", (uint8_t *)alloc_objects[index], index));
        assert ((index >= 0) && (index < max_pending_allocs));
        alloc_objects[index] = (uint8_t*)0;
    }

    void uoh_alloc_done (uint8_t* obj)
    {
        if (!gc_heap::cm_in_progress)
        {
            return;
        }

        for (int i = 0; i < max_pending_allocs; i++)
        {
            if (alloc_objects [i] == obj)
            {
                uoh_alloc_done_with_index(i);
                return;
            }
        }
        dprintf (3, ("uoh alloc: could not release lock on %p", obj));
    }
};

#endif //BACKGROUND_GC

void reset_memory (uint8_t* o, size_t sizeo);

#ifdef WRITE_WATCH

#ifndef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
static bool virtual_alloc_hardware_write_watch = false;
#endif // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

static bool hardware_write_watch_capability = false;


inline bool can_use_hardware_write_watch()
{
    return hardware_write_watch_capability;
}


inline bool can_use_write_watch_for_card_table()
{
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    return true;
#else
    return can_use_hardware_write_watch();
#endif
}

#else //WRITE_WATCH
#define mem_reserve (MEM_RESERVE)
#endif //WRITE_WATCH

void WaitLongerNoInstru (int i)
{
    // every 8th attempt:
    bool bToggleGC = GCToEEInterface::EnablePreemptiveGC();

    // if we're waiting for gc to finish, we should block immediately
    if (g_fSuspensionPending == 0)
    {
        if  (g_num_processors > 1)
        {
            YieldProcessor();           // indicate to the processor that we are spinning
            if  (i & 0x01f)
                GCToOSInterface::YieldThread (0);
            else
                GCToOSInterface::Sleep (5);
        }
        else
            GCToOSInterface::Sleep (5);
    }

    // If CLR is hosted, a thread may reach here while it is in preemptive GC mode,
    // or it has no Thread object, in order to force a task to yield, or to triger a GC.
    // It is important that the thread is going to wait for GC.  Otherwise the thread
    // is in a tight loop.  If the thread has high priority, the perf is going to be very BAD.
    if (bToggleGC)
    {
#ifdef _DEBUG
        // In debug builds, all enter_spin_lock operations go through this code.  If a GC has
        // started, it is important to block until the GC thread calls set_gc_done (since it is
        // guaranteed to have cleared g_TrapReturningThreads by this point).  This avoids livelock
        // conditions which can otherwise occur if threads are allowed to spin in this function
        // (and therefore starve the GC thread) between the point when the GC thread sets the
        // WaitForGC event and the point when the GC thread clears g_TrapReturningThreads.
        if (gc_heap::gc_started)
        {
            gc_heap::wait_for_gc_done();
        }
#endif // _DEBUG
        GCToEEInterface::DisablePreemptiveGC();
    }
    else if (g_fSuspensionPending > 0)
    {
        g_theGCHeap->WaitUntilGCComplete();
    }
}

inline
static void safe_switch_to_thread()
{
    bool cooperative_mode = gc_heap::enable_preemptive();

    GCToOSInterface::YieldThread(0);

    gc_heap::disable_preemptive(cooperative_mode);
}

#define check_msl_status(msg, size) if (msl_status == msl_retry_different_heap) \
    { \
        dprintf (5555, ("h%d RETRY %s(%Id)", heap_number, msg, size)); \
        return a_state_retry_allocate; \
    }

static const int32_t lock_free = -1;
static const int32_t lock_taken = 0;
static const int32_t lock_decommissioned = 1;


// If our heap got decommissioned, we need to try an existing heap.
//inline
bool gc_heap::should_move_heap (GCSpinLock* msl)
{
#ifdef MULTIPLE_HEAPS
    if (msl->lock == lock_decommissioned)
    {
        dprintf (5555, ("heap#%d got decommissioned! need to retry", heap_number));
    }
    return (msl->lock == lock_decommissioned);
#else //MULTIPLE_HEAPS
    return false;
#endif //MULTIPLE_HEAPS
}

// All the places where we could be stopped because there was a suspension should call should_move_heap to check if we need to return
// so we can try another heap or we can continue the allocation on the same heap.
enter_msl_status gc_heap::enter_spin_lock_msl_helper (GCSpinLock* msl)
{
    do
    {
#ifdef DYNAMIC_HEAP_COUNT
        uint64_t start = GetHighPrecisionTimeStamp();
#endif //DYNAMIC_HEAP_COUNT

        unsigned int i = 0;
        while (VolatileLoad (&msl->lock) != lock_free)
        {
            if (should_move_heap (msl))
            {
                return msl_retry_different_heap;
            }
            if ((++i & 7) && !IsGCInProgress ())
            {
                if (g_num_processors > 1)
                {
#ifndef MULTIPLE_HEAPS
                    int spin_count = 32 * yp_spin_count_unit;
#else //!MULTIPLE_HEAPS
                    int spin_count = yp_spin_count_unit;
#endif //!MULTIPLE_HEAPS
                    for (int j = 0; j < spin_count; j++)
                    {
                        if (VolatileLoad (&msl->lock) == lock_free || IsGCInProgress ())
                            break;
                        // give the HT neighbor a chance to run
                        YieldProcessor ();
                    }
                    if (VolatileLoad (&msl->lock) != lock_free && !IsGCInProgress ())
                    {
#ifdef DYNAMIC_HEAP_COUNT
                        start -= GetHighPrecisionTimeStamp();
#endif //DYNAMIC_HEAP_COUNT
                        safe_switch_to_thread ();
#ifdef DYNAMIC_HEAP_COUNT
                        start += GetHighPrecisionTimeStamp();
#endif //DYNAMIC_HEAP_COUNT
                    }
                }
                else
                {
                    safe_switch_to_thread ();
                }
            }
            else
            {
#ifdef DYNAMIC_HEAP_COUNT
                start -= GetHighPrecisionTimeStamp();
#endif //DYNAMIC_HEAP_COUNT
                WaitLongerNoInstru (i);
#ifdef DYNAMIC_HEAP_COUNT
                start += GetHighPrecisionTimeStamp();
#endif //DYNAMIC_HEAP_COUNT
            }
        }
#ifdef DYNAMIC_HEAP_COUNT
        uint64_t end = GetHighPrecisionTimeStamp();
        Interlocked::ExchangeAdd64 (&msl->msl_wait_time, end - start);
        dprintf (3, ("h%d wait for msl lock wait time %zd, total wait time: %zd", heap_number, (end - start), msl->msl_wait_time));
#endif //DYNAMIC_HEAP_COUNT
    }
    while (Interlocked::CompareExchange (&msl->lock, lock_taken, lock_free) != lock_free);

    return msl_entered;
}

inline
enter_msl_status gc_heap::enter_spin_lock_msl (GCSpinLock* msl)
{
    if (Interlocked::CompareExchange (&msl->lock, lock_taken, lock_free) == lock_free)
        return msl_entered;

    return enter_spin_lock_msl_helper (msl);
}

//
// We need the following methods to have volatile arguments, so that they can accept
// raw pointers in addition to the results of the & operator on Volatile<T>.
// this will never be used for the more_space_lock_xxx, which is why
// "lock_decommissioned" cannot happen.
inline
static void enter_spin_lock_noinstru (RAW_KEYWORD(volatile) int32_t* lock)
{
retry:

    if (Interlocked::CompareExchange(lock, lock_taken, lock_free) != lock_free)
    {
        unsigned int i = 0;
        while (VolatileLoad(lock) != lock_free)
        {
            // will never be used for more_space_lock_xxx
            assert (VolatileLoad(lock) != lock_decommissioned);
            if ((++i & 7) && !IsGCInProgress())
            {
                if  (g_num_processors > 1)
                {
#ifndef MULTIPLE_HEAPS
                    int spin_count = 32 * yp_spin_count_unit;
#else //!MULTIPLE_HEAPS
                    int spin_count = yp_spin_count_unit;
#endif //!MULTIPLE_HEAPS
                    for (int j = 0; j < spin_count; j++)
                    {
                        if  (VolatileLoad(lock) == lock_free || IsGCInProgress())
                            break;
                        YieldProcessor();           // indicate to the processor that we are spinning
                    }
                    if  (VolatileLoad(lock) != lock_free && !IsGCInProgress())
                    {
                        safe_switch_to_thread();
                    }
                }
                else
                {
                    safe_switch_to_thread();
                }
            }
            else
            {
                WaitLongerNoInstru(i);
            }
        }
        goto retry;
    }
}

inline
static BOOL try_enter_spin_lock_noinstru(RAW_KEYWORD(volatile) int32_t* lock)
{
    return (Interlocked::CompareExchange(&*lock, lock_taken, lock_free) == lock_free);
}

inline
static void leave_spin_lock_noinstru (RAW_KEYWORD(volatile) int32_t* lock)
{
    VolatileStore<int32_t>((int32_t*)lock, lock_free);
}

#ifdef _DEBUG

inline
static void enter_spin_lock (GCSpinLock *pSpinLock)
{
    enter_spin_lock_noinstru (&pSpinLock->lock);
    assert (pSpinLock->holding_thread == (Thread*)-1);
    pSpinLock->holding_thread = GCToEEInterface::GetThread();
}

inline
static BOOL try_enter_spin_lock(GCSpinLock *pSpinLock)
{
    BOOL ret = try_enter_spin_lock_noinstru(&pSpinLock->lock);
    if (ret)
        pSpinLock->holding_thread = GCToEEInterface::GetThread();
    return ret;
}

inline
static void leave_spin_lock(GCSpinLock *pSpinLock)
{
    bool gc_thread_p = GCToEEInterface::WasCurrentThreadCreatedByGC();
    pSpinLock->released_by_gc_p = gc_thread_p;
    pSpinLock->holding_thread = (Thread*) -1;
    if (pSpinLock->lock != lock_free)
        leave_spin_lock_noinstru(&pSpinLock->lock);
}

#define ASSERT_HOLDING_SPIN_LOCK(pSpinLock) \
    _ASSERTE((pSpinLock)->holding_thread == GCToEEInterface::GetThread());

#define ASSERT_NOT_HOLDING_SPIN_LOCK(pSpinLock) \
    _ASSERTE((pSpinLock)->holding_thread != GCToEEInterface::GetThread());

#else //_DEBUG

//In the concurrent version, the Enable/DisablePreemptiveGC is optional because
//the gc thread call WaitLonger.
void WaitLonger (int i
#ifdef SYNCHRONIZATION_STATS
    , GCSpinLock* spin_lock
#endif //SYNCHRONIZATION_STATS
    )
{
#ifdef SYNCHRONIZATION_STATS
    (spin_lock->num_wait_longer)++;
#endif //SYNCHRONIZATION_STATS

    // every 8th attempt:
    bool bToggleGC = GCToEEInterface::EnablePreemptiveGC();
    assert (bToggleGC);

    // if we're waiting for gc to finish, we should block immediately
    if (!gc_heap::gc_started)
    {
#ifdef SYNCHRONIZATION_STATS
        (spin_lock->num_switch_thread_w)++;
#endif //SYNCHRONIZATION_STATS
        if  (g_num_processors > 1)
        {
            YieldProcessor();           // indicate to the processor that we are spinning
            if  (i & 0x01f)
                GCToOSInterface::YieldThread (0);
            else
                GCToOSInterface::Sleep (5);
        }
        else
            GCToOSInterface::Sleep (5);
    }

    // If CLR is hosted, a thread may reach here while it is in preemptive GC mode,
    // or it has no Thread object, in order to force a task to yield, or to triger a GC.
    // It is important that the thread is going to wait for GC.  Otherwise the thread
    // is in a tight loop.  If the thread has high priority, the perf is going to be very BAD.
    if (gc_heap::gc_started)
    {
        gc_heap::wait_for_gc_done();
    }

    if (bToggleGC)
    {
#ifdef SYNCHRONIZATION_STATS
        (spin_lock->num_disable_preemptive_w)++;
#endif //SYNCHRONIZATION_STATS
        GCToEEInterface::DisablePreemptiveGC();
    }
}

inline
static void enter_spin_lock (GCSpinLock* spin_lock)
{
retry:

    if (Interlocked::CompareExchange(&spin_lock->lock, lock_taken, lock_free) != lock_free)
    {
        unsigned int i = 0;
        while (spin_lock->lock != lock_free)
        {
            assert (spin_lock->lock != lock_decommissioned);
            if ((++i & 7) && !gc_heap::gc_started)
            {
                if  (g_num_processors > 1)
                {
#ifndef MULTIPLE_HEAPS
                    int spin_count = 32 * yp_spin_count_unit;
#else //!MULTIPLE_HEAPS
                    int spin_count = yp_spin_count_unit;
#endif //!MULTIPLE_HEAPS
                    for (int j = 0; j < spin_count; j++)
                    {
                        if  (spin_lock->lock == lock_free || gc_heap::gc_started)
                            break;
                        YieldProcessor();           // indicate to the processor that we are spinning
                    }
                    if  (spin_lock->lock != lock_free && !gc_heap::gc_started)
                    {
#ifdef SYNCHRONIZATION_STATS
                        (spin_lock->num_switch_thread)++;
#endif //SYNCHRONIZATION_STATS
                        bool cooperative_mode = gc_heap::enable_preemptive ();

                        GCToOSInterface::YieldThread(0);

                        gc_heap::disable_preemptive (cooperative_mode);
                    }
                }
                else
                    GCToOSInterface::YieldThread(0);
            }
            else
            {
                WaitLonger(i
#ifdef SYNCHRONIZATION_STATS
                        , spin_lock
#endif //SYNCHRONIZATION_STATS
                    );
            }
        }
        goto retry;
    }
}

inline
static BOOL try_enter_spin_lock(GCSpinLock* spin_lock)
{
    return (Interlocked::CompareExchange(&spin_lock->lock, lock_taken, lock_free) == lock_free);
}

inline
static void leave_spin_lock (GCSpinLock * spin_lock)
{
    spin_lock->lock = lock_free;
}

#define ASSERT_HOLDING_SPIN_LOCK(pSpinLock)

#endif //_DEBUG

bool gc_heap::enable_preemptive ()
{
    return GCToEEInterface::EnablePreemptiveGC();
}

void gc_heap::disable_preemptive (bool restore_cooperative)
{
    if (restore_cooperative)
    {
        GCToEEInterface::DisablePreemptiveGC();
    }
}

typedef void **  PTR_PTR;
inline
void memclr ( uint8_t* mem, size_t size)
{
    dprintf (3, ("MEMCLR: %p, %zd", mem, size));
    assert ((size & (sizeof(PTR_PTR)-1)) == 0);
    assert (sizeof(PTR_PTR) == DATA_ALIGNMENT);
    memset (mem, 0, size);
}


inline
ptrdiff_t round_down (ptrdiff_t add, int pitch)
{
    return ((add / pitch) * pitch);
}

#if defined(FEATURE_STRUCTALIGN) && defined(RESPECT_LARGE_ALIGNMENT)
// FEATURE_STRUCTALIGN allows the compiler to dictate the alignment,
// i.e, if a larger alignment matters or is beneficial, the compiler
// generated info tells us so.  RESPECT_LARGE_ALIGNMENT is just the
// converse - it's a heuristic for the GC to use a larger alignment.
#error FEATURE_STRUCTALIGN should imply !RESPECT_LARGE_ALIGNMENT
#endif

#if defined(FEATURE_STRUCTALIGN) && defined(FEATURE_LOH_COMPACTION)
#error FEATURE_STRUCTALIGN and FEATURE_LOH_COMPACTION are mutually exclusive
#endif

// Returns true if two pointers have the same large (double than normal) alignment.
inline
BOOL same_large_alignment_p (uint8_t* p1, uint8_t* p2)
{
#ifdef RESPECT_LARGE_ALIGNMENT
    const size_t LARGE_ALIGNMENT_MASK = 2 * DATA_ALIGNMENT - 1;
    return ((((size_t)p1 ^ (size_t)p2) & LARGE_ALIGNMENT_MASK) == 0);
#else
    UNREFERENCED_PARAMETER(p1);
    UNREFERENCED_PARAMETER(p2);
    return TRUE;
#endif // RESPECT_LARGE_ALIGNMENT
}

// Determines the padding size required to fix large alignment during relocation.
inline
size_t switch_alignment_size (BOOL already_padded_p)
{
#ifndef RESPECT_LARGE_ALIGNMENT
    assert (!"Should not be called");
#endif // RESPECT_LARGE_ALIGNMENT

    if (already_padded_p)
        return DATA_ALIGNMENT;
    else
        return Align (min_obj_size) | DATA_ALIGNMENT;
}

#ifdef FEATURE_STRUCTALIGN
void set_node_aligninfo (uint8_t *node, int requiredAlignment, ptrdiff_t pad);
void clear_node_aligninfo (uint8_t *node);
#else // FEATURE_STRUCTALIGN
#define node_realigned(node)    (((plug_and_reloc*)(node))[-1].reloc & 1)
void set_node_realigned (uint8_t* node);
void clear_node_realigned(uint8_t* node);
#endif // FEATURE_STRUCTALIGN

inline
size_t AlignQword (size_t nbytes)
{
#ifdef FEATURE_STRUCTALIGN
    // This function is used to align everything on the large object
    // heap to an 8-byte boundary, to reduce the number of unaligned
    // accesses to (say) arrays of doubles.  With FEATURE_STRUCTALIGN,
    // the compiler dictates the optimal alignment instead of having
    // a heuristic in the GC.
    return Align (nbytes);
#else // FEATURE_STRUCTALIGN
    return (nbytes + 7) & ~7;
#endif // FEATURE_STRUCTALIGN
}

inline
BOOL Aligned (size_t n)
{
    return (n & ALIGNCONST) == 0;
}

#define OBJECT_ALIGNMENT_OFFSET (sizeof(MethodTable *))

#ifdef FEATURE_STRUCTALIGN
#define MAX_STRUCTALIGN OS_PAGE_SIZE
#else // FEATURE_STRUCTALIGN
#define MAX_STRUCTALIGN 0
#endif // FEATURE_STRUCTALIGN

#ifdef FEATURE_STRUCTALIGN
inline
ptrdiff_t AdjustmentForMinPadSize(ptrdiff_t pad, int requiredAlignment)
{
    // The resulting alignpad must be either 0 or at least min_obj_size.
    // Note that by computing the following difference on unsigned types,
    // we can do the range check 0 < alignpad < min_obj_size with a
    // single conditional branch.
    if ((size_t)(pad - DATA_ALIGNMENT) < Align (min_obj_size) - DATA_ALIGNMENT)
    {
        return requiredAlignment;
    }
    return 0;
}


inline
ptrdiff_t ComputeStructAlignPad (uint8_t* plug, int requiredAlignment, size_t alignmentOffset=OBJECT_ALIGNMENT_OFFSET)
{
    return StructAlign (plug, requiredAlignment, alignmentOffset) - plug;
}

BOOL IsStructAligned (uint8_t *ptr, int requiredAlignment)
{
    return StructAlign (ptr, requiredAlignment) == ptr;
}

inline
ptrdiff_t ComputeMaxStructAlignPad (int requiredAlignment)
{
    if (requiredAlignment == DATA_ALIGNMENT)
        return 0;
    // Since a non-zero alignment padding cannot be less than min_obj_size (so we can fit the
    // alignment padding object), the worst-case alignment padding is correspondingly larger
    // than the required alignment.
    return requiredAlignment + Align (min_obj_size) - DATA_ALIGNMENT;
}

inline
ptrdiff_t ComputeMaxStructAlignPadLarge (int requiredAlignment)
{
    if (requiredAlignment <= get_alignment_constant (TRUE)+1)
        return 0;
    // This is the same as ComputeMaxStructAlignPad, except that in addition to leaving space
    // for padding before the actual object, it also leaves space for filling a gap after the
    // actual object.  This is needed on the large object heap, as the outer allocation functions
    // don't operate on an allocation context (which would have left space for the final gap).
    return requiredAlignment + Align (min_obj_size) * 2 - DATA_ALIGNMENT;
}

#else // FEATURE_STRUCTALIGN
#define ComputeMaxStructAlignPad(requiredAlignment) 0
#define ComputeMaxStructAlignPadLarge(requiredAlignment) 0
#endif // FEATURE_STRUCTALIGN

//CLR_SIZE  is the max amount of bytes from gen0 that is set to 0 in one chunk
#ifdef SERVER_GC
#define CLR_SIZE ((size_t)(8*1024+32))
#else //SERVER_GC
#define CLR_SIZE ((size_t)(8*1024+32))
#endif //SERVER_GC

#define END_SPACE_AFTER_GC (loh_size_threshold + MAX_STRUCTALIGN)
// When we fit into the free list we need an extra of a min obj
#define END_SPACE_AFTER_GC_FL (END_SPACE_AFTER_GC + Align (min_obj_size))

#if defined(BACKGROUND_GC) && !defined(USE_REGIONS)
#define SEGMENT_INITIAL_COMMIT (2*OS_PAGE_SIZE)
#else
#define SEGMENT_INITIAL_COMMIT (OS_PAGE_SIZE)
#endif //BACKGROUND_GC && !USE_REGIONS

// This is always power of 2.
#ifdef HOST_64BIT
const size_t min_segment_size_hard_limit = 1024*1024*16;
#else //HOST_64BIT
const size_t min_segment_size_hard_limit = 1024*1024*4;
#endif //HOST_64BIT

#ifndef HOST_64BIT
// Max size of heap hard limit (2^31) to be able to be aligned and rounded up on power of 2 and not overflow
const size_t max_heap_hard_limit = (size_t)2 * (size_t)1024 * (size_t)1024 * (size_t)1024;
#endif //!HOST_64BIT


#ifdef SERVER_GC

#ifdef HOST_64BIT

#define INITIAL_ALLOC ((size_t)((size_t)4*1024*1024*1024))
#define LHEAP_ALLOC   ((size_t)(1024*1024*256))

#else

#define INITIAL_ALLOC ((size_t)(1024*1024*64))
#define LHEAP_ALLOC   ((size_t)(1024*1024*32))

#endif  // HOST_64BIT

#else //SERVER_GC

#ifdef HOST_64BIT

#define INITIAL_ALLOC ((size_t)(1024*1024*256))
#define LHEAP_ALLOC   ((size_t)(1024*1024*128))

#else

#define INITIAL_ALLOC ((size_t)(1024*1024*16))
#define LHEAP_ALLOC   ((size_t)(1024*1024*16))

#endif  // HOST_64BIT

#endif //SERVER_GC

const size_t etw_allocation_tick = 100*1024;

const size_t low_latency_alloc = 256*1024;

const size_t fgn_check_quantum = 2*1024*1024;

#ifdef MH_SC_MARK
const int max_snoop_level = 128;
#endif //MH_SC_MARK

#ifdef CARD_BUNDLE
//threshold of heap size to turn on card bundles.
#define SH_TH_CARD_BUNDLE  (40*1024*1024)
#define MH_TH_CARD_BUNDLE  (180*1024*1024)
#endif //CARD_BUNDLE

// min size to decommit to make the OS call worthwhile
#define MIN_DECOMMIT_SIZE  (100*OS_PAGE_SIZE)

// max size to decommit per millisecond
#define DECOMMIT_SIZE_PER_MILLISECOND (160*1024)

// time in milliseconds between decommit steps
#define DECOMMIT_TIME_STEP_MILLISECONDS (100)

inline
size_t align_on_page (size_t add)
{
    return ((add + OS_PAGE_SIZE - 1) & ~((size_t)OS_PAGE_SIZE - 1));
}

inline
uint8_t* align_on_page (uint8_t* add)
{
    return (uint8_t*)align_on_page ((size_t) add);
}

inline
size_t align_lower_page (size_t add)
{
    return (add & ~((size_t)OS_PAGE_SIZE - 1));
}

inline
uint8_t* align_lower_page (uint8_t* add)
{
    return (uint8_t*)align_lower_page ((size_t)add);
}

inline
size_t align_write_watch_lower_page (size_t add)
{
    return (add & ~(WRITE_WATCH_UNIT_SIZE - 1));
}

inline
uint8_t* align_write_watch_lower_page (uint8_t* add)
{
    return (uint8_t*)align_lower_page ((size_t)add);
}

inline
BOOL power_of_two_p (size_t integer)
{
    return !(integer & (integer-1));
}





#ifdef USE_REGIONS
void region_write_barrier_settings (WriteBarrierParameters* args,
                                    gc_heap::region_info* map_region_to_generation_skewed,
                                    uint8_t region_shr)
{
    switch (GCConfig::GetGCWriteBarrier())
    {
    default:
    case GCConfig::WRITE_BARRIER_DEFAULT:
    case GCConfig::WRITE_BARRIER_REGION_BIT:
        // bitwise region write barrier is the default now
        args->region_to_generation_table = (uint8_t*)map_region_to_generation_skewed;
        args->region_shr = region_shr;
        args->region_use_bitwise_write_barrier = true;
        break;

    case GCConfig::WRITE_BARRIER_REGION_BYTE:
        // bytewise region write barrier
        args->region_to_generation_table = (uint8_t*)map_region_to_generation_skewed;
        args->region_shr = region_shr;
        assert (args->region_use_bitwise_write_barrier == false);
        break;

    case GCConfig::WRITE_BARRIER_SERVER:
        // server write barrier
        // args should have been zero initialized
        assert (args->region_use_bitwise_write_barrier == false);
        assert (args->region_to_generation_table == nullptr);
        assert (args->region_shr == 0);
        break;
    }
}
#endif //USE_REGIONS

void stomp_write_barrier_ephemeral (uint8_t* ephemeral_low, uint8_t* ephemeral_high
#ifdef USE_REGIONS
                                   , gc_heap::region_info* map_region_to_generation_skewed
                                   , uint8_t region_shr
#endif //USE_REGIONS
                                   )
{
#ifndef USE_REGIONS
    initGCShadow();
#endif

    WriteBarrierParameters args = {};
    args.operation = WriteBarrierOp::StompEphemeral;
    args.is_runtime_suspended = true;
    args.ephemeral_low = ephemeral_low;
    args.ephemeral_high = ephemeral_high;
#ifdef USE_REGIONS
    region_write_barrier_settings (&args, map_region_to_generation_skewed, region_shr);
#endif //USE_REGIONS
    GCToEEInterface::StompWriteBarrier(&args);
}

void stomp_write_barrier_initialize(uint8_t* ephemeral_low, uint8_t* ephemeral_high
#ifdef USE_REGIONS
                                   , gc_heap::region_info* map_region_to_generation_skewed
                                   , uint8_t region_shr
#endif //USE_REGIONS
                                   )
{
    WriteBarrierParameters args = {};
    args.operation = WriteBarrierOp::Initialize;
    args.is_runtime_suspended = true;
    args.requires_upper_bounds_check = false;
    args.card_table = g_gc_card_table;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    args.card_bundle_table = g_gc_card_bundle_table;
#endif

    args.lowest_address = g_gc_lowest_address;
    args.highest_address = g_gc_highest_address;
    args.ephemeral_low = ephemeral_low;
    args.ephemeral_high = ephemeral_high;

#ifdef USE_REGIONS
    region_write_barrier_settings (&args, map_region_to_generation_skewed, region_shr);
#endif //USE_REGIONS

    GCToEEInterface::StompWriteBarrier(&args);
}

//extract the low bits [0,low[ of a uint32_t
#define lowbits(wrd, bits) ((wrd) & ((1 << (bits))-1))
//extract the high bits [high, 32] of a uint32_t
#define highbits(wrd, bits) ((wrd) & ~((1 << (bits))-1))

class mark;
class generation;
class heap_segment;
class CObjectHeader;
class dynamic_data;
class l_heap;
class sorted_table;
class c_synchronize;

#ifdef FEATURE_PREMORTEM_FINALIZATION
static
HRESULT AllocateCFinalize(CFinalize **pCFinalize);
#endif // FEATURE_PREMORTEM_FINALIZATION

uint8_t* tree_search (uint8_t* tree, uint8_t* old_address);


#ifdef USE_INTROSORT
#define _sort introsort::sort
#elif defined(USE_VXSORT)
// in this case we have do_vxsort which takes an additional range that
// all items to be sorted are contained in
// so do not #define _sort
#else //USE_INTROSORT
#define _sort qsort1
void qsort1(uint8_t** low, uint8_t** high, unsigned int depth);
#endif //USE_INTROSORT

void* virtual_alloc (size_t size);
void* virtual_alloc (size_t size, bool use_large_pages_p, uint16_t numa_node = NUMA_NODE_UNDEFINED);

/* per heap static initialization */
#if defined(BACKGROUND_GC) && !defined(MULTIPLE_HEAPS)
uint32_t*   gc_heap::mark_array;
#endif //BACKGROUND_GC && !MULTIPLE_HEAPS

uint8_t**   gc_heap::g_mark_list;
uint8_t**   gc_heap::g_mark_list_copy;
size_t      gc_heap::mark_list_size;
size_t      gc_heap::g_mark_list_total_size;
bool        gc_heap::mark_list_overflow;
#ifdef USE_REGIONS
uint8_t***  gc_heap::g_mark_list_piece;
size_t      gc_heap::g_mark_list_piece_size;
size_t      gc_heap::g_mark_list_piece_total_size;
#endif //USE_REGIONS

seg_mapping* seg_mapping_table;

#ifdef FEATURE_BASICFREEZE
sorted_table* gc_heap::seg_table;
#endif //FEATURE_BASICFREEZE

#ifdef MULTIPLE_HEAPS
GCEvent     gc_heap::ee_suspend_event;
size_t      gc_heap::min_gen0_balance_delta = 0;
size_t      gc_heap::min_balance_threshold = 0;
#endif //MULTIPLE_HEAPS

VOLATILE(BOOL) gc_heap::gc_started;

#ifdef MULTIPLE_HEAPS
GCEvent     gc_heap::gc_start_event;
bool        gc_heap::gc_thread_no_affinitize_p = false;
uintptr_t   process_mask = 0;

int         gc_heap::n_heaps;       // current number of heaps
int         gc_heap::n_max_heaps;   // maximum number of heaps

gc_heap**   gc_heap::g_heaps;

#if !defined(USE_REGIONS) || defined(_DEBUG)
size_t*     gc_heap::g_promoted;
#endif //!USE_REGIONS || _DEBUG

#ifdef MH_SC_MARK
int*        gc_heap::g_mark_stack_busy;
#endif //MH_SC_MARK

#ifdef BACKGROUND_GC
size_t*     gc_heap::g_bpromoted;
#endif //BACKGROUND_GC

BOOL        gc_heap::gradual_decommit_in_progress_p = FALSE;
size_t      gc_heap::max_decommit_step_size = 0;
#else  //MULTIPLE_HEAPS

#if !defined(USE_REGIONS) || defined(_DEBUG)
size_t      gc_heap::g_promoted;
#endif //!USE_REGIONS || _DEBUG

#ifdef BACKGROUND_GC
size_t      gc_heap::g_bpromoted;
#endif //BACKGROUND_GC

// this is just to have fewer #ifdefs in code shared between WKS and SVR
// for filling out ScanContext structs
const int n_heaps = 1;

#endif //MULTIPLE_HEAPS

size_t      gc_heap::card_table_element_layout[total_bookkeeping_elements + 1];
uint8_t*    gc_heap::bookkeeping_start = nullptr;
#ifdef USE_REGIONS
uint8_t*    gc_heap::bookkeeping_covered_committed = nullptr;
size_t      gc_heap::bookkeeping_sizes[total_bookkeeping_elements];
#endif //USE_REGIONS

size_t      gc_heap::reserved_memory = 0;
size_t      gc_heap::reserved_memory_limit = 0;
BOOL        gc_heap::g_low_memory_status;

static gc_reason gc_trigger_reason = reason_empty;

gc_latency_level gc_heap::latency_level = latency_level_default;

gc_mechanisms  gc_heap::settings;

gc_history_global gc_heap::gc_data_global;

uint64_t    gc_heap::gc_last_ephemeral_decommit_time = 0;

CLRCriticalSection gc_heap::check_commit_cs;

#ifdef COMMITTED_BYTES_SHADOW
CLRCriticalSection gc_heap::decommit_lock;
#endif //COMMITTED_BYTES_SHADOW

size_t      gc_heap::current_total_committed = 0;

size_t      gc_heap::committed_by_oh[recorded_committed_bucket_counts];

size_t      gc_heap::current_total_committed_bookkeeping = 0;

BOOL        gc_heap::reset_mm_p = TRUE;

#ifdef FEATURE_EVENT_TRACE
bool gc_heap::informational_event_enabled_p = false;

uint64_t*   gc_heap::gc_time_info = 0;

#ifdef BACKGROUND_GC
uint64_t*   gc_heap::bgc_time_info = 0;
#endif //BACKGROUND_GC

size_t      gc_heap::physical_memory_from_config = 0;

size_t      gc_heap::gen0_min_budget_from_config = 0;

size_t      gc_heap::gen0_max_budget_from_config = 0;

int         gc_heap::high_mem_percent_from_config = 0;

bool        gc_heap::use_frozen_segments_p = false;

#ifdef FEATURE_LOH_COMPACTION
gc_heap::etw_loh_compact_info* gc_heap::loh_compact_info;
#endif //FEATURE_LOH_COMPACTION
#endif //FEATURE_EVENT_TRACE

bool        gc_heap::hard_limit_config_p = false;

#if defined(SHORT_PLUGS) && !defined(USE_REGIONS)
double      gc_heap::short_plugs_pad_ratio = 0;
#endif //SHORT_PLUGS && !USE_REGIONS

int         gc_heap::generation_skip_ratio_threshold = 0;
int         gc_heap::conserve_mem_setting = 0;
bool        gc_heap::spin_count_unit_config_p = false;

uint64_t    gc_heap::suspended_start_time = 0;
uint64_t    gc_heap::end_gc_time = 0;
uint64_t    gc_heap::total_suspended_time = 0;
uint64_t    gc_heap::process_start_time = 0;
last_recorded_gc_info gc_heap::last_ephemeral_gc_info;
last_recorded_gc_info gc_heap::last_full_blocking_gc_info;

uint64_t    gc_heap::last_alloc_reset_suspended_end_time = 0;
size_t      gc_heap::max_peak_heap_size = 0;
VOLATILE(size_t) gc_heap::llc_size = 0;

#ifdef BACKGROUND_GC
last_recorded_gc_info gc_heap::last_bgc_info[2];
VOLATILE(bool)        gc_heap::is_last_recorded_bgc = false;
VOLATILE(int)         gc_heap::last_bgc_info_index = 0;
#endif //BACKGROUND_GC

#ifdef DYNAMIC_HEAP_COUNT
size_t      gc_heap::hc_change_cancelled_count_prep = 0;
#ifdef BACKGROUND_GC
int         gc_heap::bgc_th_creation_hist_index = 0;
gc_heap::bgc_thread_creation_history gc_heap::bgc_th_creation_hist[max_bgc_thread_creation_count];
size_t      gc_heap::bgc_th_count_created = 0;
size_t      gc_heap::bgc_th_count_created_th_existed = 0;
size_t      gc_heap::bgc_th_count_creation_failed = 0;
size_t      gc_heap::bgc_init_gc_index = 0;
VOLATILE(short) gc_heap::bgc_init_n_heaps = 0;
size_t      gc_heap::hc_change_cancelled_count_bgc = 0;
#endif //BACKGROUND_GC
#endif //DYNAMIC_HEAP_COUNT

#if defined(HOST_64BIT)
#define MAX_ALLOWED_MEM_LOAD 85

// consider putting this in dynamic data -
// we may want different values for workstation
// and server GC.
#define MIN_YOUNGEST_GEN_DESIRED (16*1024*1024)

size_t      gc_heap::youngest_gen_desired_th;
#endif //HOST_64BIT

uint64_t    gc_heap::mem_one_percent = 0;

uint32_t    gc_heap::high_memory_load_th = 0;

uint32_t    gc_heap::m_high_memory_load_th;

uint32_t    gc_heap::v_high_memory_load_th;

uint32_t    gc_heap::almost_high_memory_load_th;

bool        gc_heap::is_restricted_physical_mem;

uint64_t    gc_heap::total_physical_mem = 0;

uint64_t    gc_heap::entry_available_physical_mem = 0;

size_t      gc_heap::heap_hard_limit = 0;

size_t      gc_heap::heap_hard_limit_oh[total_oh_count];

#ifdef USE_REGIONS

size_t      gc_heap::regions_range = 0;

#endif //USE_REGIONS

bool        affinity_config_specified_p = false;

#ifdef USE_REGIONS
region_allocator global_region_allocator;
uint8_t*(*initial_regions)[total_generation_count][2] = nullptr;
size_t      gc_heap::region_count = 0;

gc_heap::region_info* gc_heap::map_region_to_generation = nullptr;
gc_heap::region_info* gc_heap::map_region_to_generation_skewed = nullptr;

#endif //USE_REGIONS

#ifdef BACKGROUND_GC
GCEvent     gc_heap::bgc_start_event;

gc_mechanisms gc_heap::saved_bgc_settings;

gc_history_global gc_heap::bgc_data_global;

GCEvent     gc_heap::background_gc_done_event;

GCEvent     gc_heap::ee_proceed_event;

bool        gc_heap::gc_can_use_concurrent = false;

bool        gc_heap::temp_disable_concurrent_p = false;

uint32_t    gc_heap::cm_in_progress = FALSE;

BOOL        gc_heap::dont_restart_ee_p = FALSE;

BOOL        gc_heap::keep_bgc_threads_p = FALSE;

GCEvent     gc_heap::bgc_threads_sync_event;

BOOL        gc_heap::do_ephemeral_gc_p = FALSE;

BOOL        gc_heap::do_concurrent_p = FALSE;

size_t      gc_heap::ephemeral_fgc_counts[max_generation];

VOLATILE(c_gc_state) gc_heap::current_c_gc_state = c_gc_state_free;

VOLATILE(BOOL) gc_heap::gc_background_running = FALSE;
#endif //BACKGROUND_GC

#ifdef USE_REGIONS
#ifdef MULTIPLE_HEAPS
uint8_t*    gc_heap::gc_low;
uint8_t*    gc_heap::gc_high;
#endif //MULTIPLE_HEAPS
VOLATILE(uint8_t*)    gc_heap::ephemeral_low;
VOLATILE(uint8_t*)    gc_heap::ephemeral_high;
#endif //USE_REGIONS

#ifndef MULTIPLE_HEAPS
#ifdef SPINLOCK_HISTORY
int         gc_heap::spinlock_info_index = 0;
spinlock_info gc_heap::last_spinlock_info[max_saved_spinlock_info];
allocation_state gc_heap::current_uoh_alloc_state = (allocation_state)-1;
#endif //SPINLOCK_HISTORY

uint32_t    gc_heap::fgn_maxgen_percent = 0;
size_t      gc_heap::fgn_last_alloc = 0;

int         gc_heap::generation_skip_ratio = 100;
#ifdef FEATURE_CARD_MARKING_STEALING
VOLATILE(size_t) gc_heap::n_eph_soh = 0;
VOLATILE(size_t) gc_heap::n_gen_soh = 0;
VOLATILE(size_t) gc_heap::n_eph_loh = 0;
VOLATILE(size_t) gc_heap::n_gen_loh = 0;
#endif //FEATURE_CARD_MARKING_STEALING

uint64_t    gc_heap::loh_alloc_since_cg = 0;

BOOL        gc_heap::elevation_requested = FALSE;

BOOL        gc_heap::last_gc_before_oom = FALSE;

BOOL        gc_heap::sufficient_gen0_space_p = FALSE;

BOOL        gc_heap::decide_promote_gen1_pins_p = TRUE;

#ifdef BACKGROUND_GC
uint8_t*    gc_heap::background_saved_lowest_address = 0;
uint8_t*    gc_heap::background_saved_highest_address = 0;
uint8_t*    gc_heap::next_sweep_obj = 0;
uint8_t*    gc_heap::current_sweep_pos = 0;
#ifdef DOUBLY_LINKED_FL
heap_segment* gc_heap::current_sweep_seg = 0;
#endif //DOUBLY_LINKED_FL
exclusive_sync* gc_heap::bgc_alloc_lock;
#endif //BACKGROUND_GC

oom_history gc_heap::oom_info;

int         gc_heap::oomhist_index_per_heap = 0;

oom_history gc_heap::oomhist_per_heap[max_oom_history_count];

fgm_history gc_heap::fgm_result;

size_t      gc_heap::allocated_since_last_gc[total_oh_count];

#ifndef USE_REGIONS
BOOL        gc_heap::ro_segments_in_range = FALSE;
uint8_t*    gc_heap::ephemeral_low;
uint8_t*    gc_heap::ephemeral_high;
BOOL        gc_heap::ephemeral_promotion;
uint8_t*    gc_heap::saved_ephemeral_plan_start[ephemeral_generation_count];
size_t      gc_heap::saved_ephemeral_plan_start_size[ephemeral_generation_count];
#endif //!USE_REGIONS

uint8_t*    gc_heap::lowest_address;

uint8_t*    gc_heap::highest_address;

short*      gc_heap::brick_table;

uint32_t*   gc_heap::card_table;

#ifdef CARD_BUNDLE
uint32_t*   gc_heap::card_bundle_table;
#endif //CARD_BUNDLE

uint8_t*    gc_heap::gc_low = 0;

uint8_t*    gc_heap::gc_high = 0;

#ifndef USE_REGIONS
uint8_t*    gc_heap::demotion_low;

uint8_t*    gc_heap::demotion_high;

uint8_t*    gc_heap::last_gen1_pin_end;
#endif //!USE_REGIONS

gen_to_condemn_tuning gc_heap::gen_to_condemn_reasons;

size_t      gc_heap::etw_allocation_running_amount[total_oh_count];

uint64_t    gc_heap::total_alloc_bytes_soh = 0;

uint64_t    gc_heap::total_alloc_bytes_uoh = 0;

int         gc_heap::gc_policy = 0;

uint64_t    gc_heap::allocation_running_time;

size_t      gc_heap::allocation_running_amount;

heap_segment* gc_heap::ephemeral_heap_segment = 0;

#ifdef USE_REGIONS
#ifdef STRESS_REGIONS
OBJECTHANDLE* gc_heap::pinning_handles_for_alloc = 0;
int         gc_heap::ph_index_per_heap = 0;
int         gc_heap::pinning_seg_interval = 2;
size_t      gc_heap::num_gen0_regions = 0;
int         gc_heap::sip_seg_interval = 0;
int         gc_heap::sip_seg_maxgen_interval = 0;
size_t      gc_heap::num_condemned_regions = 0;
#endif //STRESS_REGIONS

region_free_list gc_heap::free_regions[count_free_region_kinds];

int         gc_heap::num_regions_freed_in_sweep = 0;

int         gc_heap::regions_per_gen[max_generation + 1];

int         gc_heap::planned_regions_per_gen[max_generation + 1];

int         gc_heap::sip_maxgen_regions_per_gen[max_generation + 1];

heap_segment* gc_heap::reserved_free_regions_sip[max_generation];

int         gc_heap::new_gen0_regions_in_plns = 0;
int         gc_heap::new_regions_in_prr = 0;
int         gc_heap::new_regions_in_threading = 0;

size_t      gc_heap::end_gen0_region_space = 0;

size_t      gc_heap::end_gen0_region_committed_space = 0;

size_t      gc_heap::gen0_pinned_free_space = 0;

bool        gc_heap::gen0_large_chunk_found = false;

size_t*     gc_heap::survived_per_region = nullptr;

size_t*     gc_heap::old_card_survived_per_region = nullptr;
#endif //USE_REGIONS

BOOL        gc_heap::blocking_collection = FALSE;

heap_segment* gc_heap::freeable_uoh_segment = 0;

uint64_t    gc_heap::time_bgc_last = 0;

size_t      gc_heap::mark_stack_tos = 0;

size_t      gc_heap::mark_stack_bos = 0;

size_t      gc_heap::mark_stack_array_length = 0;

mark*       gc_heap::mark_stack_array = 0;

#if defined (_DEBUG) && defined (VERIFY_HEAP)
BOOL        gc_heap::verify_pinned_queue_p = FALSE;
#endif //_DEBUG && VERIFY_HEAP

uint8_t*    gc_heap::oldest_pinned_plug = 0;

size_t      gc_heap::num_pinned_objects = 0;

#ifdef FEATURE_LOH_COMPACTION
size_t      gc_heap::loh_pinned_queue_tos = 0;

size_t      gc_heap::loh_pinned_queue_bos = 0;

size_t      gc_heap::loh_pinned_queue_length = 0;

mark*       gc_heap::loh_pinned_queue = 0;

BOOL        gc_heap::loh_compacted_p = FALSE;
#endif //FEATURE_LOH_COMPACTION

#ifdef BACKGROUND_GC

EEThreadId  gc_heap::bgc_thread_id;

uint8_t*    gc_heap::background_written_addresses [array_size+2];

heap_segment* gc_heap::freeable_soh_segment = 0;

size_t      gc_heap::bgc_overflow_count = 0;

size_t      gc_heap::bgc_begin_uoh_size[uoh_generation_count] = {};
size_t      gc_heap::bgc_uoh_current_size[uoh_generation_count] = {};
size_t      gc_heap::end_uoh_size[uoh_generation_count] = {};

size_t      gc_heap::uoh_a_no_bgc[uoh_generation_count] = {};
size_t      gc_heap::uoh_a_bgc_marking[uoh_generation_count] = {};
size_t      gc_heap::uoh_a_bgc_planning[uoh_generation_count] = {};
#ifdef BGC_SERVO_TUNING
size_t      gc_heap::bgc_maxgen_end_fl_size = 0;
#endif //BGC_SERVO_TUNING

size_t      gc_heap::background_soh_size_end_mark = 0;

size_t      gc_heap::background_soh_alloc_count = 0;

uint8_t**   gc_heap::background_mark_stack_tos = 0;

uint8_t**   gc_heap::background_mark_stack_array = 0;

size_t      gc_heap::background_mark_stack_array_length = 0;

BOOL        gc_heap::processed_eph_overflow_p = FALSE;

#ifdef USE_REGIONS
BOOL        gc_heap::background_overflow_p = FALSE;
#else //USE_REGIONS
uint8_t*    gc_heap::background_min_overflow_address =0;

uint8_t*    gc_heap::background_max_overflow_address =0;

uint8_t*    gc_heap::background_min_soh_overflow_address =0;

uint8_t*    gc_heap::background_max_soh_overflow_address =0;

heap_segment* gc_heap::saved_overflow_ephemeral_seg = 0;

heap_segment* gc_heap::saved_sweep_ephemeral_seg = 0;

uint8_t*    gc_heap::saved_sweep_ephemeral_start = 0;
#endif //USE_REGIONS

Thread*     gc_heap::bgc_thread = 0;

uint8_t**   gc_heap::c_mark_list = 0;

size_t      gc_heap::c_mark_list_length = 0;

size_t      gc_heap::c_mark_list_index = 0;

gc_history_per_heap gc_heap::bgc_data_per_heap;

BOOL    gc_heap::bgc_thread_running;

CLRCriticalSection gc_heap::bgc_threads_timeout_cs;

#endif //BACKGROUND_GC

uint8_t**   gc_heap::mark_list;
uint8_t**   gc_heap::mark_list_index;
uint8_t**   gc_heap::mark_list_end;

#ifdef SNOOP_STATS
snoop_stats_data gc_heap::snoop_stat;
#endif //SNOOP_STATS

uint8_t*    gc_heap::min_overflow_address = MAX_PTR;

uint8_t*    gc_heap::max_overflow_address = 0;

uint8_t*    gc_heap::shigh = 0;

uint8_t*    gc_heap::slow = MAX_PTR;

#ifndef USE_REGIONS
size_t      gc_heap::ordered_free_space_indices[MAX_NUM_BUCKETS];

size_t      gc_heap::saved_ordered_free_space_indices[MAX_NUM_BUCKETS];

size_t      gc_heap::ordered_plug_indices[MAX_NUM_BUCKETS];

size_t      gc_heap::saved_ordered_plug_indices[MAX_NUM_BUCKETS];

BOOL        gc_heap::ordered_plug_indices_init = FALSE;

BOOL        gc_heap::use_bestfit = FALSE;

uint8_t*    gc_heap::bestfit_first_pin = 0;

BOOL        gc_heap::commit_end_of_seg = FALSE;

size_t      gc_heap::max_free_space_items = 0;

size_t      gc_heap::free_space_buckets = 0;

size_t      gc_heap::free_space_items = 0;

int         gc_heap::trimmed_free_space_index = 0;

size_t      gc_heap::total_ephemeral_plugs = 0;

seg_free_spaces* gc_heap::bestfit_seg = 0;

size_t      gc_heap::total_ephemeral_size = 0;
#endif //!USE_REGIONS

#ifdef HEAP_ANALYZE

size_t      gc_heap::internal_root_array_length = initial_internal_roots;

uint8_t**   gc_heap::internal_root_array = 0;

size_t      gc_heap::internal_root_array_index = 0;

BOOL        gc_heap::heap_analyze_success = TRUE;

uint8_t*    gc_heap::current_obj = 0;
size_t      gc_heap::current_obj_size = 0;

#endif //HEAP_ANALYZE

#ifdef GC_CONFIG_DRIVEN
size_t gc_heap::interesting_data_per_gc[max_idp_count];
//size_t gc_heap::interesting_data_per_heap[max_idp_count];
//size_t gc_heap::interesting_mechanisms_per_heap[max_im_count];
#endif //GC_CONFIG_DRIVEN
#endif //MULTIPLE_HEAPS

no_gc_region_info gc_heap::current_no_gc_region_info;
FinalizerWorkItem* gc_heap::finalizer_work;
BOOL gc_heap::proceed_with_gc_p = FALSE;
GCSpinLock gc_heap::gc_lock;

#ifdef FEATURE_JAVAMARSHAL
uint8_t**   gc_heap::global_bridge_list;
size_t      gc_heap::num_global_bridge_objs;
#endif //FEATURE_JAVAMARSHAL

#ifdef BACKGROUND_GC
uint64_t gc_heap::total_uoh_a_last_bgc = 0;
#endif //BACKGROUND_GC

#ifdef USE_REGIONS
region_free_list gc_heap::global_regions_to_decommit[count_free_region_kinds];
region_free_list gc_heap::global_free_huge_regions;
#else //USE_REGIONS
size_t gc_heap::eph_gen_starts_size = 0;
heap_segment* gc_heap::segment_standby_list;
#endif //USE_REGIONS
bool          gc_heap::use_large_pages_p = 0;
#ifdef HEAP_BALANCE_INSTRUMENTATION
size_t        gc_heap::last_gc_end_time_us = 0;
#endif //HEAP_BALANCE_INSTRUMENTATION
#ifdef USE_REGIONS
bool          gc_heap::enable_special_regions_p = false;
#else //USE_REGIONS
size_t        gc_heap::min_segment_size = 0;
size_t        gc_heap::min_uoh_segment_size = 0;
#endif //!USE_REGIONS
size_t        gc_heap::min_segment_size_shr = 0;
size_t        gc_heap::soh_segment_size = 0;
size_t        gc_heap::segment_info_size = 0;

#ifdef GC_CONFIG_DRIVEN
size_t gc_heap::compact_or_sweep_gcs[2];
#endif //GC_CONFIG_DRIVEN

#ifdef FEATURE_LOH_COMPACTION
BOOL                   gc_heap::loh_compaction_always_p = FALSE;
gc_loh_compaction_mode gc_heap::loh_compaction_mode = loh_compaction_default;
#endif //FEATURE_LOH_COMPACTION

GCEvent gc_heap::full_gc_approach_event;

GCEvent gc_heap::full_gc_end_event;

uint32_t gc_heap::fgn_loh_percent = 0;

#ifdef BACKGROUND_GC
BOOL gc_heap::fgn_last_gc_was_concurrent = FALSE;
#endif //BACKGROUND_GC

VOLATILE(bool) gc_heap::full_gc_approach_event_set;

size_t gc_heap::full_gc_counts[gc_type_max];

bool gc_heap::maxgen_size_inc_p = false;

#ifndef USE_REGIONS
BOOL gc_heap::should_expand_in_full_gc = FALSE;
#endif //!USE_REGIONS

#ifdef DYNAMIC_HEAP_COUNT
int gc_heap::dynamic_adaptation_mode = dynamic_adaptation_default;
gc_heap::dynamic_heap_count_data_t SVR::gc_heap::dynamic_heap_count_data;
size_t gc_heap::current_total_soh_stable_size = 0;
uint64_t gc_heap::last_suspended_end_time = 0;
uint64_t gc_heap::change_heap_count_time = 0;
uint64_t gc_heap::total_change_heap_count = 0;
uint64_t gc_heap::total_change_heap_count_time = 0;
size_t gc_heap::gc_index_full_gc_end = 0;
uint64_t gc_heap::before_distribute_free_regions_time = 0;
bool gc_heap::trigger_initial_gen2_p = false;

#ifdef BACKGROUND_GC
bool gc_heap::trigger_bgc_for_rethreading_p = false;
int gc_heap::total_bgc_threads = 0;
int gc_heap::last_bgc_n_heaps = 0;
int gc_heap::last_total_bgc_threads = 0;
#endif //BACKGROUND_GC

#ifdef STRESS_DYNAMIC_HEAP_COUNT
int gc_heap::heaps_in_this_gc = 0;
int gc_heap::bgc_to_ngc2_ratio = 0;
#endif //STRESS_DYNAMIC_HEAP_COUNT
#endif // DYNAMIC_HEAP_COUNT

// Provisional mode related stuff.
bool gc_heap::provisional_mode_triggered = false;
bool gc_heap::pm_trigger_full_gc = false;
size_t gc_heap::provisional_triggered_gc_count = 0;
size_t gc_heap::provisional_off_gc_count = 0;
size_t gc_heap::num_provisional_triggered = 0;
bool   gc_heap::pm_stress_on = false;

#ifdef HEAP_ANALYZE
BOOL        gc_heap::heap_analyze_enabled = FALSE;
#endif //HEAP_ANALYZE

#ifndef MULTIPLE_HEAPS

alloc_list gc_heap::gen2_alloc_list[NUM_GEN2_ALIST - 1];
alloc_list gc_heap::loh_alloc_list [NUM_LOH_ALIST - 1];
alloc_list gc_heap::poh_alloc_list [NUM_POH_ALIST - 1];

#ifdef DOUBLY_LINKED_FL
// size we removed with no undo; only for recording purpose
size_t gc_heap::gen2_removed_no_undo = 0;
size_t gc_heap::saved_pinned_plug_index = INVALID_SAVED_PINNED_PLUG_INDEX;
#endif //DOUBLY_LINKED_FL

#ifdef FEATURE_EVENT_TRACE
etw_bucket_info gc_heap::bucket_info[NUM_GEN2_ALIST];
#endif //FEATURE_EVENT_TRACE

dynamic_data gc_heap::dynamic_data_table [total_generation_count];
gc_history_per_heap gc_heap::gc_data_per_heap;
size_t gc_heap::total_promoted_bytes = 0;
size_t gc_heap::finalization_promoted_bytes = 0;
size_t gc_heap::maxgen_pinned_compact_before_advance = 0;

uint8_t* gc_heap::alloc_allocated = 0;

size_t gc_heap::allocation_quantum = CLR_SIZE;

GCSpinLock gc_heap::more_space_lock_soh;
GCSpinLock gc_heap::more_space_lock_uoh;

#ifdef BACKGROUND_GC
VOLATILE(int32_t) gc_heap::uoh_alloc_thread_count = 0;
#endif //BACKGROUND_GC

#ifdef SYNCHRONIZATION_STATS
unsigned int gc_heap::good_suspension = 0;
unsigned int gc_heap::bad_suspension = 0;
uint64_t     gc_heap::total_msl_acquire = 0;
unsigned int gc_heap::num_msl_acquired = 0;
unsigned int gc_heap::num_high_msl_acquire = 0;
unsigned int gc_heap::num_low_msl_acquire = 0;
#endif //SYNCHRONIZATION_STATS

size_t   gc_heap::alloc_contexts_used = 0;
size_t   gc_heap::soh_allocation_no_gc = 0;
size_t   gc_heap::loh_allocation_no_gc = 0;
bool     gc_heap::no_gc_oom_p = false;
heap_segment* gc_heap::saved_loh_segment_no_gc = 0;

#endif //MULTIPLE_HEAPS

#ifndef MULTIPLE_HEAPS

BOOL        gc_heap::gen0_bricks_cleared = FALSE;

int         gc_heap::gen0_must_clear_bricks = 0;

#ifdef FEATURE_PREMORTEM_FINALIZATION
CFinalize*  gc_heap::finalize_queue = 0;
#endif // FEATURE_PREMORTEM_FINALIZATION

#ifdef FEATURE_CARD_MARKING_STEALING
VOLATILE(uint32_t) gc_heap::card_mark_chunk_index_soh;
VOLATILE(bool) gc_heap::card_mark_done_soh;
VOLATILE(uint32_t) gc_heap::card_mark_chunk_index_loh;
VOLATILE(uint32_t) gc_heap::card_mark_chunk_index_poh;
VOLATILE(bool) gc_heap::card_mark_done_uoh;
#endif // FEATURE_CARD_MARKING_STEALING

generation gc_heap::generation_table [total_generation_count];

size_t     gc_heap::interesting_data_per_heap[max_idp_count];

size_t     gc_heap::compact_reasons_per_heap[max_compact_reasons_count];

size_t     gc_heap::expand_mechanisms_per_heap[max_expand_mechanisms_count];

size_t     gc_heap::interesting_mechanism_bits_per_heap[max_gc_mechanism_bits_count];

mark_queue_t gc_heap::mark_queue;

#ifdef USE_REGIONS
bool gc_heap::special_sweep_p = false;
#endif //USE_REGIONS

int gc_heap::loh_pinned_queue_decay = LOH_PIN_DECAY;

#endif // MULTIPLE_HEAPS

/* end of per heap static initialization */

#ifdef USE_REGIONS
const size_t uninitialized_end_gen0_region_space = (size_t)(-1);
#endif //USE_REGIONS

// budget smoothing
size_t     gc_heap::smoothed_desired_total[total_generation_count];
/* end of static initialization */

// This is for methods that need to iterate through all SOH heap segments/regions.
inline
int get_start_generation_index()
{
#ifdef USE_REGIONS
    return 0;
#else
    return max_generation;
#endif //USE_REGIONS
}

inline
int get_stop_generation_index (int condemned_gen_number)
{
#ifdef USE_REGIONS
    return 0;
#else
    return condemned_gen_number;
#endif //USE_REGIONS
}

void gen_to_condemn_tuning::print (int heap_num)
{
#ifdef DT_LOG
    dprintf (DT_LOG_0, ("condemned reasons (%d %d)", condemn_reasons_gen, condemn_reasons_condition));
    dprintf (DT_LOG_0, ("%s", record_condemn_reasons_gen_header));
    gc_condemn_reason_gen r_gen;
    for (int i = 0; i < gcrg_max; i++)
    {
        r_gen = (gc_condemn_reason_gen)(i);
        str_reasons_gen[i * 2] = get_gen_char (get_gen (r_gen));
    }
    dprintf (DT_LOG_0, ("[%2d]%s", heap_num, str_reasons_gen));

    dprintf (DT_LOG_0, ("%s", record_condemn_reasons_condition_header));
    gc_condemn_reason_condition r_condition;
    for (int i = 0; i < gcrc_max; i++)
    {
        r_condition = (gc_condemn_reason_condition)(i);
        str_reasons_condition[i * 2] = get_condition_char (get_condition (r_condition));
    }

    dprintf (DT_LOG_0, ("[%2d]%s", heap_num, str_reasons_condition));
#else
    UNREFERENCED_PARAMETER(heap_num);
#endif //DT_LOG
}

void gc_generation_data::print (int heap_num, int gen_num)
{
#if defined(SIMPLE_DPRINTF) && defined(DT_LOG)
    dprintf (DT_LOG_0, ("[%2d]gen%d beg %zd fl %zd fo %zd end %zd fl %zd fo %zd in %zd p %zd np %zd alloc %zd",
                heap_num, gen_num,
                size_before,
                free_list_space_before, free_obj_space_before,
                size_after,
                free_list_space_after, free_obj_space_after,
                in, pinned_surv, npinned_surv,
                new_allocation));
#else
    UNREFERENCED_PARAMETER(heap_num);
    UNREFERENCED_PARAMETER(gen_num);
#endif //SIMPLE_DPRINTF && DT_LOG
}

void gc_history_per_heap::set_mechanism (gc_mechanism_per_heap mechanism_per_heap, uint32_t value)
{
    uint32_t* mechanism = &mechanisms[mechanism_per_heap];
    *mechanism = 0;
    *mechanism |= mechanism_mask;
    *mechanism |= (1 << value);

#ifdef DT_LOG
    gc_mechanism_descr* descr = &gc_mechanisms_descr[mechanism_per_heap];
    dprintf (DT_LOG_0, ("setting %s: %s",
            descr->name,
            (descr->descr)[value]));
#endif //DT_LOG
}

void gc_history_per_heap::print()
{
#if defined(SIMPLE_DPRINTF) && defined(DT_LOG)
    for (int i = 0; i < (sizeof (gen_data)/sizeof (gc_generation_data)); i++)
    {
        gen_data[i].print (heap_index, i);
    }

    dprintf (DT_LOG_0, ("fla %zd flr %zd esa %zd ca %zd pa %zd paa %zd, rfle %d, ec %zd",
                    maxgen_size_info.free_list_allocated,
                    maxgen_size_info.free_list_rejected,
                    maxgen_size_info.end_seg_allocated,
                    maxgen_size_info.condemned_allocated,
                    maxgen_size_info.pinned_allocated,
                    maxgen_size_info.pinned_allocated_advance,
                    maxgen_size_info.running_free_list_efficiency,
                    extra_gen0_committed));

    int mechanism = 0;
    gc_mechanism_descr* descr = 0;

    for (int i = 0; i < max_mechanism_per_heap; i++)
    {
        mechanism = get_mechanism ((gc_mechanism_per_heap)i);

        if (mechanism >= 0)
        {
            descr = &gc_mechanisms_descr[(gc_mechanism_per_heap)i];
            dprintf (DT_LOG_0, ("[%2d]%s%s",
                        heap_index,
                        descr->name,
                        (descr->descr)[mechanism]));
        }
    }
#endif //SIMPLE_DPRINTF && DT_LOG
}

void gc_history_global::print()
{
#ifdef DT_LOG
    char str_settings[64];
    memset (str_settings, '|', sizeof (char) * 64);
    str_settings[max_global_mechanisms_count*2] = 0;

    for (int i = 0; i < max_global_mechanisms_count; i++)
    {
        str_settings[i * 2] = (get_mechanism_p ((gc_global_mechanism_p)i) ? 'Y' : 'N');
    }

    dprintf (DT_LOG_0, ("[hp]|c|p|o|d|b|e|"));

    dprintf (DT_LOG_0, ("%4d|%s", num_heaps, str_settings));
    dprintf (DT_LOG_0, ("Condemned gen%d(reason: %s; mode: %s), youngest budget %zd(%d), memload %d",
                        condemned_generation,
                        str_gc_reasons[reason],
                        str_gc_pause_modes[pause_mode],
                        final_youngest_desired,
                        gen0_reduction_count,
                        mem_pressure));
#endif //DT_LOG
}

uint32_t limit_time_to_uint32 (uint64_t time)
{
    time = min (time, (uint64_t)UINT32_MAX);
    return (uint32_t)time;
}

inline BOOL
in_range_for_segment(uint8_t* add, heap_segment* seg)
{
    return ((add >= heap_segment_mem (seg)) && (add < heap_segment_reserved (seg)));
}

#ifdef FEATURE_BASICFREEZE
// The array we allocate is organized as follows:
// 0th element is the address of the last array we allocated.
// starting from the 1st element are the segment addresses, that's
// what buckets() returns.
struct bk
{
    uint8_t* add;
    size_t val;
};

class sorted_table
{
private:
    ptrdiff_t size;
    ptrdiff_t count;
    bk* slots;
    bk* buckets() { return (slots + 1); }
    uint8_t*& last_slot (bk* arr) { return arr[0].add; }
    bk* old_slots;
public:
    static  sorted_table* make_sorted_table ();
    BOOL    insert (uint8_t* add, size_t val);;
    size_t  lookup (uint8_t*& add);
    void    remove (uint8_t* add);
    void    clear ();
    void    delete_sorted_table();
    void    delete_old_slots();
    void    enqueue_old_slot(bk* sl);
    BOOL    ensure_space_for_insert();
};

sorted_table*
sorted_table::make_sorted_table ()
{
    size_t size = 400;

    // allocate one more bk to store the older slot address.
    sorted_table* res = (sorted_table*)new (nothrow) char [sizeof (sorted_table) + (size + 1) * sizeof (bk)];
    if (!res)
        return 0;
    res->size = size;
    res->slots = (bk*)(res + 1);
    res->old_slots = 0;
    res->clear();
    return res;
}

void
sorted_table::delete_sorted_table()
{
    if (slots != (bk*)(this+1))
    {
        delete[] slots;
    }
    delete_old_slots();
}
void
sorted_table::delete_old_slots()
{
    uint8_t* sl = (uint8_t*)old_slots;
    while (sl)
    {
        uint8_t* dsl = sl;
        sl = last_slot ((bk*)sl);
        delete[] dsl;
    }
    old_slots = 0;
}
void
sorted_table::enqueue_old_slot(bk* sl)
{
    last_slot (sl) = (uint8_t*)old_slots;
    old_slots = sl;
}

inline
size_t
sorted_table::lookup (uint8_t*& add)
{
    ptrdiff_t high = (count-1);
    ptrdiff_t low = 0;
    ptrdiff_t ti;
    ptrdiff_t mid;
    bk* buck = buckets();
    while (low <= high)
    {
        mid = ((low + high)/2);
        ti = mid;
        if (buck[ti].add > add)
        {
            if ((ti > 0) && (buck[ti-1].add <= add))
            {
                add = buck[ti-1].add;
                return buck[ti - 1].val;
            }
            high = mid - 1;
        }
        else
        {
            if (buck[ti+1].add > add)
            {
                add = buck[ti].add;
                return buck[ti].val;
            }
            low = mid + 1;
        }
    }
    add = 0;
    return 0;
}

BOOL
sorted_table::ensure_space_for_insert()
{
    if (count == size)
    {
        size = (size * 3)/2;
        assert((size * sizeof (bk)) > 0);
        bk* res = (bk*)new (nothrow) char [(size + 1) * sizeof (bk)];
        assert (res);
        if (!res)
            return FALSE;

        last_slot (res) = 0;
        memcpy (((bk*)res + 1), buckets(), count * sizeof (bk));
        bk* last_old_slots = slots;
        slots = res;
        if (last_old_slots != (bk*)(this + 1))
            enqueue_old_slot (last_old_slots);
    }
    return TRUE;
}

BOOL
sorted_table::insert (uint8_t* add, size_t val)
{
    //grow if no more room
    assert (count < size);

    //insert sorted
    ptrdiff_t high = (count-1);
    ptrdiff_t low = 0;
    ptrdiff_t ti;
    ptrdiff_t mid;
    bk* buck = buckets();
    while (low <= high)
    {
        mid = ((low + high)/2);
        ti = mid;
        if (buck[ti].add > add)
        {
            if ((ti == 0) || (buck[ti-1].add <= add))
            {
                // found insertion point
                for (ptrdiff_t k = count; k > ti;k--)
                {
                    buck [k] = buck [k-1];
                }
                buck[ti].add = add;
                buck[ti].val = val;
                count++;
                return TRUE;
            }
            high = mid - 1;
        }
        else
        {
            if (buck[ti+1].add > add)
            {
                //found the insertion point
                for (ptrdiff_t k = count; k > ti+1;k--)
                {
                    buck [k] = buck [k-1];
                }
                buck[ti+1].add = add;
                buck[ti+1].val = val;
                count++;
                return TRUE;
            }
            low = mid + 1;
        }
    }
    assert (0);
    return TRUE;
}

void
sorted_table::remove (uint8_t* add)
{
    ptrdiff_t high = (count-1);
    ptrdiff_t low = 0;
    ptrdiff_t ti;
    ptrdiff_t mid;
    bk* buck = buckets();
    while (low <= high)
    {
        mid = ((low + high)/2);
        ti = mid;
        if (buck[ti].add > add)
        {
            if (buck[ti-1].add <= add)
            {
                for (ptrdiff_t k = ti; k < count; k++)
                    buck[k-1] = buck[k];
                count--;
                return;
            }
            high = mid - 1;
        }
        else
        {
            if (buck[ti+1].add > add)
            {
                for (ptrdiff_t k = ti+1; k < count; k++)
                    buck[k-1] = buck[k];
                count--;
                return;
            }
            low = mid + 1;
        }
    }
    assert (0);
}

void
sorted_table::clear()
{
    count = 1;
    buckets()[0].add = MAX_PTR;
}
#endif //FEATURE_BASICFREEZE

#ifdef USE_REGIONS
inline
size_t get_skewed_basic_region_index_for_address (uint8_t* address)
{
    assert ((g_gc_lowest_address <= address) && (address <= g_gc_highest_address));
    size_t skewed_basic_region_index = (size_t)address >> gc_heap::min_segment_size_shr;
    return skewed_basic_region_index;
}

inline
size_t get_basic_region_index_for_address (uint8_t* address)
{
    size_t skewed_basic_region_index = get_skewed_basic_region_index_for_address (address);
    return (skewed_basic_region_index - get_skewed_basic_region_index_for_address (g_gc_lowest_address));
}

// Go from a random address to its region info. The random address could be
// in one of the basic regions of a larger region so we need to check for that.
inline
heap_segment* get_region_info_for_address (uint8_t* address)
{
    size_t basic_region_index = (size_t)address >> gc_heap::min_segment_size_shr;
    heap_segment* basic_region_info_entry = (heap_segment*)&seg_mapping_table[basic_region_index];
    ptrdiff_t first_field = (ptrdiff_t)heap_segment_allocated (basic_region_info_entry);
    if (first_field < 0)
    {
        basic_region_index += first_field;
    }

    return ((heap_segment*)(&seg_mapping_table[basic_region_index]));
}

// Go from the physical start of a region to its region info.
inline
heap_segment* get_region_info (uint8_t* region_start)
{
    size_t region_index = (size_t)region_start >> gc_heap::min_segment_size_shr;
    heap_segment* region_info_entry = (heap_segment*)&seg_mapping_table[region_index];
    dprintf (REGIONS_LOG, ("region info for region %p is at %zd, %zx (alloc: %p)",
        region_start, region_index, (size_t)region_info_entry, heap_segment_allocated (region_info_entry)));
    return (heap_segment*)&seg_mapping_table[region_index];
}

// Go from the actual region info to its region start.
inline
uint8_t* get_region_start (heap_segment* region_info)
{
    uint8_t* obj_start = heap_segment_mem (region_info);
    return (obj_start - sizeof (aligned_plug_and_gap));
}

inline
size_t get_region_size (heap_segment* region_info)
{
    return (size_t)(heap_segment_reserved (region_info) - get_region_start (region_info));
}

inline
size_t get_region_committed_size (heap_segment* region)
{
    uint8_t* start = get_region_start (region);
    uint8_t* committed = heap_segment_committed (region);
    return committed - start;
}

inline bool is_free_region (heap_segment* region)
{
    return (heap_segment_allocated (region) == nullptr);
}


#endif //USE_REGIONS

inline
uint8_t* align_on_segment (uint8_t* add)
{
    return (uint8_t*)((size_t)(add + (((size_t)1 << gc_heap::min_segment_size_shr) - 1)) & ~(((size_t)1 << gc_heap::min_segment_size_shr) - 1));
}

inline
uint8_t* align_lower_segment (uint8_t* add)
{
    return (uint8_t*)((size_t)(add) & ~(((size_t)1 << gc_heap::min_segment_size_shr) - 1));
}


#ifdef FEATURE_BASICFREEZE
inline
size_t ro_seg_begin_index (heap_segment* seg)
{
#ifdef USE_REGIONS
    size_t begin_index = (size_t)heap_segment_mem (seg) >> gc_heap::min_segment_size_shr;
#else
    size_t begin_index = (size_t)seg >> gc_heap::min_segment_size_shr;
#endif //USE_REGIONS
    begin_index = max (begin_index, (size_t)g_gc_lowest_address >> gc_heap::min_segment_size_shr);
    return begin_index;
}

inline
size_t ro_seg_end_index (heap_segment* seg)
{
    size_t end_index = (size_t)(heap_segment_reserved (seg) - 1) >> gc_heap::min_segment_size_shr;
    end_index = min (end_index, (size_t)g_gc_highest_address >> gc_heap::min_segment_size_shr);
    return end_index;
}


heap_segment* ro_segment_lookup (uint8_t* o)
{
    uint8_t* ro_seg_start = o;
    heap_segment* seg = (heap_segment*)gc_heap::seg_table->lookup (ro_seg_start);

    if (ro_seg_start && in_range_for_segment (o, seg))
        return seg;
    else
        return 0;
}

#endif //FEATURE_BASICFREEZE

#ifdef MULTIPLE_HEAPS
inline
gc_heap* seg_mapping_table_heap_of_worker (uint8_t* o)
{
    size_t index = (size_t)o >> gc_heap::min_segment_size_shr;
    seg_mapping* entry = &seg_mapping_table[index];

#ifdef USE_REGIONS
    gc_heap* hp = heap_segment_heap ((heap_segment*)entry);
#else
    gc_heap* hp = ((o > entry->boundary) ? entry->h1 : entry->h0);

    dprintf (2, ("checking obj %p, index is %zd, entry: boundary: %p, h0: %p, seg0: %p, h1: %p, seg1: %p",
        o, index, (entry->boundary + 1),
        (uint8_t*)(entry->h0), (uint8_t*)(entry->seg0),
        (uint8_t*)(entry->h1), (uint8_t*)(entry->seg1)));

#ifdef _DEBUG
    heap_segment* seg = ((o > entry->boundary) ? entry->seg1 : entry->seg0);
#ifdef FEATURE_BASICFREEZE
    if ((size_t)seg & ro_in_entry)
        seg = (heap_segment*)((size_t)seg & ~ro_in_entry);
#endif //FEATURE_BASICFREEZE

#ifdef TRACE_GC
    if (seg)
    {
        if (in_range_for_segment (o, seg))
        {
            dprintf (2, ("obj %p belongs to segment %p(-%p)", o, seg, (uint8_t*)heap_segment_allocated (seg)));
        }
        else
        {
            dprintf (2, ("found seg %p(-%p) for obj %p, but it's not on the seg",
                seg, (uint8_t*)heap_segment_allocated (seg), o));
        }
    }
    else
    {
        dprintf (2, ("could not find obj %p in any existing segments", o));
    }
#endif //TRACE_GC
#endif //_DEBUG
#endif //USE_REGIONS
    return hp;
}


#endif //MULTIPLE_HEAPS

// Only returns a valid seg if we can actually find o on the seg.
heap_segment* seg_mapping_table_segment_of (uint8_t* o)
{
#ifdef FEATURE_BASICFREEZE
    if ((o < g_gc_lowest_address) || (o >= g_gc_highest_address))
        return ro_segment_lookup (o);
#endif //FEATURE_BASICFREEZE

    size_t index = (size_t)o >> gc_heap::min_segment_size_shr;
    seg_mapping* entry = &seg_mapping_table[index];

#ifdef USE_REGIONS
    // REGIONS TODO: I think we could simplify this to having the same info for each
    // basic entry in a large region so we can get it right away instead of having to go
    // back some entries.
    ptrdiff_t first_field = (ptrdiff_t)heap_segment_allocated ((heap_segment*)entry);
    if (first_field == 0)
    {
        dprintf (REGIONS_LOG, ("asked for seg for %p, in a freed region mem: %p, committed %p",
            o, heap_segment_mem ((heap_segment*)entry),
            heap_segment_committed ((heap_segment*)entry)));
        return 0;
    }
    // Regions are never going to intersect an ro seg, so this can never be ro_in_entry.
    assert (first_field != 0);
    assert (first_field != ro_in_entry);
    if (first_field < 0)
    {
        index += first_field;
    }
    heap_segment* seg = (heap_segment*)&seg_mapping_table[index];
#else //USE_REGIONS
    dprintf (2, ("checking obj %p, index is %zd, entry: boundary: %p, seg0: %p, seg1: %p",
        o, index, (entry->boundary + 1),
        (uint8_t*)(entry->seg0), (uint8_t*)(entry->seg1)));

    heap_segment* seg = ((o > entry->boundary) ? entry->seg1 : entry->seg0);
#ifdef FEATURE_BASICFREEZE
    if ((size_t)seg & ro_in_entry)
        seg = (heap_segment*)((size_t)seg & ~ro_in_entry);
#endif //FEATURE_BASICFREEZE
#endif //USE_REGIONS

    if (seg)
    {
        if (in_range_for_segment (o, seg))
        {
            dprintf (2, ("obj %p belongs to segment %p(-%p)", o, (uint8_t*)heap_segment_mem(seg), (uint8_t*)heap_segment_reserved(seg)));
        }
        else
        {
            dprintf (2, ("found seg %p(-%p) for obj %p, but it's not on the seg, setting it to 0",
                (uint8_t*)heap_segment_mem(seg), (uint8_t*)heap_segment_reserved(seg), o));
            seg = 0;
        }
    }
    else
    {
        dprintf (2, ("could not find obj %p in any existing segments", o));
    }

#ifdef FEATURE_BASICFREEZE
    // TODO: This was originally written assuming that the seg_mapping_table would always contain entries for ro
    // segments whenever the ro segment falls into the [g_gc_lowest_address,g_gc_highest_address) range.  I.e., it had an
    // extra "&& (size_t)(entry->seg1) & ro_in_entry" expression.  However, at the moment, grow_brick_card_table does
    // not correctly go through the ro segments and add them back to the seg_mapping_table when the [lowest,highest)
    // range changes.  We should probably go ahead and modify grow_brick_card_table and put back the
    // "&& (size_t)(entry->seg1) & ro_in_entry" here.
    if (!seg)
    {
        seg = ro_segment_lookup (o);
        if (seg && !in_range_for_segment (o, seg))
            seg = 0;
    }
#endif //FEATURE_BASICFREEZE

    return seg;
}

size_t gcard_of ( uint8_t*);

#define GC_MARKED       (size_t)0x1
#ifdef DOUBLY_LINKED_FL
// This bit indicates that we'll need to set the bgc mark bit for this object during an FGC.
// We only do this when we decide to compact.
#define BGC_MARKED_BY_FGC (size_t)0x2
#define MAKE_FREE_OBJ_IN_COMPACT (size_t)0x4
#define ALLOWED_SPECIAL_HEADER_BITS (GC_MARKED|BGC_MARKED_BY_FGC|MAKE_FREE_OBJ_IN_COMPACT)
#else //DOUBLY_LINKED_FL
#define ALLOWED_SPECIAL_HEADER_BITS (GC_MARKED)
#endif //!DOUBLY_LINKED_FL

#ifdef HOST_64BIT
#define SPECIAL_HEADER_BITS (0x7)
#else
#define SPECIAL_HEADER_BITS (0x3)
#endif

#define slot(i, j) ((uint8_t**)(i))[(j)+1]

#define free_object_base_size (plug_skew + sizeof(ArrayBase))

#define free_list_slot(x) ((uint8_t**)(x))[2]
#define free_list_undo(x) ((uint8_t**)(x))[-1]
#define UNDO_EMPTY ((uint8_t*)1)

#ifdef DOUBLY_LINKED_FL
#define free_list_prev(x) ((uint8_t**)(x))[3]
#define PREV_EMPTY ((uint8_t*)1)

void check_and_clear_in_free_list (uint8_t* o, size_t size)
{
    if (size >= min_free_list)
    {
        free_list_prev (o) = PREV_EMPTY;
    }
}

#endif //DOUBLY_LINKED_FL

class CObjectHeader : public Object
{
public:

#if defined(FEATURE_NATIVEAOT) || defined(BUILD_AS_STANDALONE)
    // The GC expects the following methods that are provided by the Object class in the CLR but not provided
    // by NativeAOT's version of Object.
    uint32_t GetNumComponents()
    {
        return ((ArrayBase *)this)->GetNumComponents();
    }

    void Validate(BOOL bDeep=TRUE, BOOL bVerifyNextHeader = FALSE, BOOL bVerifySyncBlock = FALSE)
    {
        // declaration of extra parameters just so the call site would need no #ifdefs
        UNREFERENCED_PARAMETER(bVerifyNextHeader);
        UNREFERENCED_PARAMETER(bVerifySyncBlock);

        MethodTable * pMT = GetMethodTable();

        _ASSERTE(pMT->SanityCheck());

        bool noRangeChecks =
            (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_NO_RANGE_CHECKS) == GCConfig::HEAPVERIFY_NO_RANGE_CHECKS;

        BOOL fSmallObjectHeapPtr = FALSE, fLargeObjectHeapPtr = FALSE;
        if (!noRangeChecks)
        {
            fSmallObjectHeapPtr = g_theGCHeap->IsHeapPointer(this, TRUE);
            if (!fSmallObjectHeapPtr)
                fLargeObjectHeapPtr = g_theGCHeap->IsHeapPointer(this);

            _ASSERTE(fSmallObjectHeapPtr || fLargeObjectHeapPtr);
        }

#ifdef FEATURE_STRUCTALIGN
        _ASSERTE(IsStructAligned((uint8_t *)this, GetMethodTable()->GetBaseAlignment()));
#endif // FEATURE_STRUCTALIGN

#if defined(FEATURE_64BIT_ALIGNMENT) && !defined(FEATURE_NATIVEAOT)
        if (pMT->RequiresAlign8())
        {
            _ASSERTE((((size_t)this) & 0x7) == (pMT->IsValueType() ? 4U : 0U));
        }
#endif // FEATURE_64BIT_ALIGNMENT

#ifdef VERIFY_HEAP
        if (bDeep && (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC))
            g_theGCHeap->ValidateObjectMember(this);
#endif
        if (fSmallObjectHeapPtr)
        {
#ifdef FEATURE_BASICFREEZE
            _ASSERTE(!g_theGCHeap->IsLargeObject(this) || g_theGCHeap->IsInFrozenSegment(this));
#else
            _ASSERTE(!g_theGCHeap->IsLargeObject(this));
#endif
        }
    }

    void ValidateHeap(BOOL bDeep)
    {
        Validate(bDeep);
    }

#endif //FEATURE_NATIVEAOT || BUILD_AS_STANDALONE

    /////
    //
    // Header Status Information
    //

    MethodTable    *GetMethodTable() const
    {
        return( (MethodTable *) (((size_t) RawGetMethodTable()) & (~SPECIAL_HEADER_BITS)));
    }

    void SetMarked()
    {
        _ASSERTE(RawGetMethodTable());
        RawSetMethodTable((MethodTable *) (((size_t) RawGetMethodTable()) | GC_MARKED));
    }

    BOOL IsMarked() const
    {
        return !!(((size_t)RawGetMethodTable()) & GC_MARKED);
    }

    void SetPinned()
    {
        assert (!(gc_heap::settings.concurrent));
        GetHeader()->SetGCBit();
    }

    BOOL IsPinned() const
    {
        return !!((((CObjectHeader*)this)->GetHeader()->GetBits()) & BIT_SBLK_GC_RESERVE);
    }

    // Now we set more bits should actually only clear the mark bit
    void ClearMarked()
    {
#ifdef DOUBLY_LINKED_FL
        RawSetMethodTable ((MethodTable *)(((size_t) RawGetMethodTable()) & (~GC_MARKED)));
#else
        RawSetMethodTable (GetMethodTable());
#endif //DOUBLY_LINKED_FL
    }

#ifdef DOUBLY_LINKED_FL
    void SetBGCMarkBit()
    {
        RawSetMethodTable((MethodTable *) (((size_t) RawGetMethodTable()) | BGC_MARKED_BY_FGC));
    }
    BOOL IsBGCMarkBitSet() const
    {
        return !!(((size_t)RawGetMethodTable()) & BGC_MARKED_BY_FGC);
    }
    void ClearBGCMarkBit()
    {
        RawSetMethodTable((MethodTable *)(((size_t) RawGetMethodTable()) & (~BGC_MARKED_BY_FGC)));
    }

    void SetFreeObjInCompactBit()
    {
        RawSetMethodTable((MethodTable *) (((size_t) RawGetMethodTable()) | MAKE_FREE_OBJ_IN_COMPACT));
    }
    BOOL IsFreeObjInCompactBitSet() const
    {
        return !!(((size_t)RawGetMethodTable()) & MAKE_FREE_OBJ_IN_COMPACT);
    }
    void ClearFreeObjInCompactBit()
    {
#ifdef _DEBUG
        // check this looks like an object, but do NOT validate pointers to other objects
        // as these may not be valid yet - we are calling this during compact_phase
        Validate(FALSE);
#endif //_DEBUG
        RawSetMethodTable((MethodTable *)(((size_t) RawGetMethodTable()) & (~MAKE_FREE_OBJ_IN_COMPACT)));
    }
#endif //DOUBLY_LINKED_FL

    size_t ClearSpecialBits()
    {
        size_t special_bits = ((size_t)RawGetMethodTable()) & SPECIAL_HEADER_BITS;
        if (special_bits != 0)
        {
            assert ((special_bits & (~ALLOWED_SPECIAL_HEADER_BITS)) == 0);
            RawSetMethodTable ((MethodTable*)(((size_t)RawGetMethodTable()) & ~(SPECIAL_HEADER_BITS)));
        }
        return special_bits;
    }

    void SetSpecialBits (size_t special_bits)
    {
        assert ((special_bits & (~ALLOWED_SPECIAL_HEADER_BITS)) == 0);
        if (special_bits != 0)
        {
            RawSetMethodTable ((MethodTable*)(((size_t)RawGetMethodTable()) | special_bits));
        }
    }

    CGCDesc *GetSlotMap ()
    {
        assert (GetMethodTable()->ContainsGCPointers());
        return CGCDesc::GetCGCDescFromMT(GetMethodTable());
    }

    void SetFree(size_t size)
    {
        assert (size >= free_object_base_size);

        assert (g_gc_pFreeObjectMethodTable->GetBaseSize() == free_object_base_size);
        assert (g_gc_pFreeObjectMethodTable->RawGetComponentSize() == 1);

        RawSetMethodTable( g_gc_pFreeObjectMethodTable );

        size_t* numComponentsPtr = (size_t*) &((uint8_t*) this)[ArrayBase::GetOffsetOfNumComponents()];
        *numComponentsPtr = size - free_object_base_size;
#ifdef VERIFY_HEAP
        //This introduces a bug in the free list management.
        //((void**) this)[-1] = 0;    // clear the sync block,
        assert (*numComponentsPtr >= 0);
        if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
        {
            memset (((uint8_t*)this)+sizeof(ArrayBase), 0xcc, *numComponentsPtr);
#ifdef DOUBLY_LINKED_FL
            // However, in this case we can't leave the Next field uncleared because no one will clear it
            // so it remains 0xcc and that's not good for verification
            if (*numComponentsPtr > 0)
            {
                free_list_slot (this) = 0;
            }
#endif //DOUBLY_LINKED_FL
        }
#endif //VERIFY_HEAP

#ifdef DOUBLY_LINKED_FL
        // For background GC, we need to distinguish between a free object that's not on the free list
        // and one that is. So we always set its prev to PREV_EMPTY to indicate that it's a free
        // object that's not on the free list. If it should be on the free list, it will be set to the
        // appropriate non zero value.
        check_and_clear_in_free_list ((uint8_t*)this, size);
#endif //DOUBLY_LINKED_FL
    }

    void UnsetFree()
    {
        size_t size = free_object_base_size - plug_skew;

        // since we only need to clear 2 ptr size, we do it manually
        PTR_PTR m = (PTR_PTR) this;
        for (size_t i = 0; i < size / sizeof(PTR_PTR); i++)
            *(m++) = 0;
    }

    BOOL IsFree () const
    {
        return (GetMethodTable() == g_gc_pFreeObjectMethodTable);
    }

#ifdef FEATURE_STRUCTALIGN
    int GetRequiredAlignment () const
    {
        return GetMethodTable()->GetRequiredAlignment();
    }
#endif // FEATURE_STRUCTALIGN

    BOOL ContainsGCPointers() const
    {
        return GetMethodTable()->ContainsGCPointers();
    }

#ifdef COLLECTIBLE_CLASS
    BOOL Collectible() const
    {
        return GetMethodTable()->Collectible();
    }

    FORCEINLINE BOOL ContainsGCPointersOrCollectible() const
    {
        MethodTable *pMethodTable = GetMethodTable();
        return (pMethodTable->ContainsGCPointers() || pMethodTable->Collectible());
    }
#endif //COLLECTIBLE_CLASS

    Object* GetObjectBase() const
    {
        return (Object*) this;
    }
};

#define header(i) ((CObjectHeader*)(i))
#define method_table(o) ((CObjectHeader*)(o))->GetMethodTable()

#ifdef DOUBLY_LINKED_FL
inline
BOOL is_on_free_list (uint8_t* o, size_t size)
{
    if (size >= min_free_list)
    {
        if (header(o)->GetMethodTable() == g_gc_pFreeObjectMethodTable)
        {
            return (free_list_prev (o) != PREV_EMPTY);
        }
    }

    return FALSE;
}

inline
void set_plug_bgc_mark_bit (uint8_t* node)
{
    header(node)->SetBGCMarkBit();
}

inline
BOOL is_plug_bgc_mark_bit_set (uint8_t* node)
{
    return header(node)->IsBGCMarkBitSet();
}

inline
void clear_plug_bgc_mark_bit (uint8_t* node)
{
    header(node)->ClearBGCMarkBit();
}

inline
void set_free_obj_in_compact_bit (uint8_t* node)
{
    header(node)->SetFreeObjInCompactBit();
}

inline
BOOL is_free_obj_in_compact_bit_set (uint8_t* node)
{
    return header(node)->IsFreeObjInCompactBitSet();
}

inline
void clear_free_obj_in_compact_bit (uint8_t* node)
{
    header(node)->ClearFreeObjInCompactBit();
}
#endif //DOUBLY_LINKED_FL

#ifdef SHORT_PLUGS
inline
void set_plug_padded (uint8_t* node)
{
    header(node)->SetMarked();
}
inline
void clear_plug_padded (uint8_t* node)
{
    header(node)->ClearMarked();
}
inline
BOOL is_plug_padded (uint8_t* node)
{
    return header(node)->IsMarked();
}
#else //SHORT_PLUGS
inline void set_plug_padded (uint8_t* node){}
inline void clear_plug_padded (uint8_t* node){}
inline
BOOL is_plug_padded (uint8_t* node){return FALSE;}
#endif //SHORT_PLUGS


inline size_t unused_array_size(uint8_t * p)
{
    assert(((CObjectHeader*)p)->IsFree());

    size_t* numComponentsPtr = (size_t*)(p + ArrayBase::GetOffsetOfNumComponents());
    return free_object_base_size + *numComponentsPtr;
}

inline
heap_segment* heap_segment_non_sip (heap_segment* ns)
{
#ifdef USE_REGIONS
    if ((ns == 0) || !heap_segment_swept_in_plan (ns))
    {
        return ns;
    }
    else
    {
        do
        {
            if (heap_segment_swept_in_plan (ns))
            {
                dprintf (REGIONS_LOG, ("region %p->%p SIP",
                    heap_segment_mem (ns), heap_segment_allocated (ns)));
            }

            ns = heap_segment_next (ns);
        } while ((ns != 0) && heap_segment_swept_in_plan (ns));
        return ns;
    }
#else //USE_REGIONS
    return ns;
#endif //USE_REGIONS
}

inline
heap_segment* heap_segment_next_non_sip (heap_segment* seg)
{
    heap_segment* ns = heap_segment_next (seg);
#ifdef USE_REGIONS
    return heap_segment_non_sip (ns);
#else
    return ns;
#endif //USE_REGIONS
}

heap_segment* heap_segment_rw (heap_segment* ns)
{
    if ((ns == 0) || !heap_segment_read_only_p (ns))
    {
        return ns;
    }
    else
    {
        do
        {
            ns = heap_segment_next (ns);
        } while ((ns != 0) && heap_segment_read_only_p (ns));
        return ns;
    }
}

//returns the next non ro segment.
heap_segment* heap_segment_next_rw (heap_segment* seg)
{
    heap_segment* ns = heap_segment_next (seg);
    return heap_segment_rw (ns);
}

// returns the segment before seg.
heap_segment* heap_segment_prev_rw (heap_segment* begin, heap_segment* seg)
{
    assert (begin != 0);
    heap_segment* prev = begin;
    heap_segment* current = heap_segment_next_rw (begin);

    while (current && current != seg)
    {
        prev = current;
        current = heap_segment_next_rw (current);
    }

    if (current == seg)
    {
        return prev;
    }
    else
    {
        return 0;
    }
}

// returns the segment before seg.


heap_segment* heap_segment_in_range (heap_segment* ns)
{
    if ((ns == 0) || heap_segment_in_range_p (ns))
    {
        return ns;
    }
    else
    {
        do
        {
            ns = heap_segment_next (ns);
        } while ((ns != 0) && !heap_segment_in_range_p (ns));
        return ns;
    }
}

heap_segment* heap_segment_next_in_range (heap_segment* seg)
{
    heap_segment* ns = heap_segment_next (seg);
    return heap_segment_in_range (ns);
}

struct imemory_data
{
    uint8_t* memory_base;
};

struct numa_reserved_block
{
    uint8_t*        memory_base;
    size_t          block_size;

    numa_reserved_block() : memory_base(nullptr), block_size(0) { }
};

struct initial_memory_details
{
    imemory_data *initial_memory;
    imemory_data *initial_normal_heap; // points into initial_memory_array
    imemory_data *initial_large_heap;  // points into initial_memory_array
    imemory_data *initial_pinned_heap; // points into initial_memory_array

    size_t block_size_normal;
    size_t block_size_large;
    size_t block_size_pinned;

    int block_count;                // # of blocks in each
    int current_block_normal;
    int current_block_large;
    int current_block_pinned;

    enum
    {
        ALLATONCE = 1,
        EACH_GENERATION,
        EACH_BLOCK,
        ALLATONCE_SEPARATED_POH,
        EACH_NUMA_NODE
    };

    size_t allocation_pattern;

    size_t block_size(int i)
    {
        switch (i / block_count)
        {
            case 0: return block_size_normal;
            case 1: return block_size_large;
            case 2: return block_size_pinned;
            default: UNREACHABLE();
        }
    };

    void* get_initial_memory (int gen, int h_number)
    {
        switch (gen)
        {
            case soh_gen0:
            case soh_gen1:
            case soh_gen2: return initial_normal_heap[h_number].memory_base;
            case loh_generation: return initial_large_heap[h_number].memory_base;
            case poh_generation: return initial_pinned_heap[h_number].memory_base;
            default: UNREACHABLE();
        }
    };

    size_t get_initial_size (int gen)
    {
        switch (gen)
        {
            case soh_gen0:
            case soh_gen1:
            case soh_gen2: return block_size_normal;
            case loh_generation: return block_size_large;
            case poh_generation: return block_size_pinned;
            default: UNREACHABLE();
        }
    };

    int numa_reserved_block_count;
    numa_reserved_block* numa_reserved_block_table;
};

initial_memory_details memory_details;

heap_segment* make_initial_segment (int gen, int h_number, gc_heap* hp)
{
    void* mem = memory_details.get_initial_memory (gen, h_number);
    size_t size = memory_details.get_initial_size (gen);
    heap_segment* res = gc_heap::make_heap_segment ((uint8_t*)mem, size, hp, gen);

    return res;
}

void* virtual_alloc (size_t size)
{
    return virtual_alloc(size, false);
}

void* virtual_alloc (size_t size, bool use_large_pages_p, uint16_t numa_node)
{
    size_t requested_size = size;

    if ((gc_heap::reserved_memory_limit - gc_heap::reserved_memory) < requested_size)
    {
        gc_heap::reserved_memory_limit = gc_heap::reserved_memory_limit + requested_size;
        if ((gc_heap::reserved_memory_limit - gc_heap::reserved_memory) < requested_size)
        {
            return 0;
        }
    }

    uint32_t flags = VirtualReserveFlags::None;
#ifndef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (virtual_alloc_hardware_write_watch)
    {
        flags = VirtualReserveFlags::WriteWatch;
    }
#endif // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    void* prgmem = use_large_pages_p ?
        GCToOSInterface::VirtualReserveAndCommitLargePages(requested_size, numa_node) :
        GCToOSInterface::VirtualReserve(requested_size, card_size * card_word_width, flags, numa_node);
    void *aligned_mem = prgmem;

    // We don't want (prgmem + size) to be right at the end of the address space
    // because we'd have to worry about that everytime we do (address + size).
    // We also want to make sure that we leave loh_size_threshold at the end
    // so we allocate a small object we don't need to worry about overflow there
    // when we do alloc_ptr+size.
    if (prgmem)
    {
        uint8_t* end_mem = (uint8_t*)prgmem + requested_size;

        if ((end_mem == 0) || ((size_t)(MAX_PTR - end_mem) <= END_SPACE_AFTER_GC))
        {
            GCToOSInterface::VirtualRelease (prgmem, requested_size);
            dprintf (2, ("Virtual Alloc size %zd returned memory right against 4GB [%zx, %zx[ - discarding",
                        requested_size, (size_t)prgmem, (size_t)((uint8_t*)prgmem+requested_size)));
            prgmem = 0;
            aligned_mem = 0;
        }
    }

    if (prgmem)
    {
        gc_heap::reserved_memory += requested_size;
    }

    dprintf (2, ("Virtual Alloc size %zd: [%zx, %zx[",
                 requested_size, (size_t)prgmem, (size_t)((uint8_t*)prgmem+requested_size)));

    return aligned_mem;
}

static size_t get_valid_segment_size (BOOL large_seg=FALSE)
{
    size_t seg_size, initial_seg_size;

    if (!large_seg)
    {
        initial_seg_size = INITIAL_ALLOC;
        seg_size = static_cast<size_t>(GCConfig::GetSegmentSize());
    }
    else
    {
        initial_seg_size = LHEAP_ALLOC;
        seg_size = static_cast<size_t>(GCConfig::GetSegmentSize()) / 2;
    }

#ifdef MULTIPLE_HEAPS
#ifdef HOST_64BIT
    if (!large_seg)
#endif // HOST_64BIT
    {
        if (g_num_processors > 4)
            initial_seg_size /= 2;
        if (g_num_processors > 8)
            initial_seg_size /= 2;
    }
#endif //MULTIPLE_HEAPS

    // if seg_size is small but not 0 (0 is default if config not set)
    // then set the segment to the minimum size
    if (!g_theGCHeap->IsValidSegmentSize(seg_size))
    {
        // if requested size is between 1 byte and 4MB, use min
        if ((seg_size >> 1) && !(seg_size >> 22))
            seg_size = 1024*1024*4;
        else
            seg_size = initial_seg_size;
    }

#ifdef HOST_64BIT
    seg_size = round_up_power2 (seg_size);
#else
    seg_size = round_down_power2 (seg_size);
#endif // HOST_64BIT

    return (seg_size);
}

#ifndef USE_REGIONS
void
gc_heap::compute_new_ephemeral_size()
{
    int eph_gen_max = max_generation - 1 - (settings.promotion ? 1 : 0);
    size_t padding_size = 0;

    for (int i = 0; i <= eph_gen_max; i++)
    {
        dynamic_data* dd = dynamic_data_of (i);
        total_ephemeral_size += (dd_survived_size (dd) - dd_pinned_survived_size (dd));
#ifdef RESPECT_LARGE_ALIGNMENT
        total_ephemeral_size += dd_num_npinned_plugs (dd) * switch_alignment_size (FALSE);
#endif //RESPECT_LARGE_ALIGNMENT
#ifdef FEATURE_STRUCTALIGN
        total_ephemeral_size += dd_num_npinned_plugs (dd) * MAX_STRUCTALIGN;
#endif //FEATURE_STRUCTALIGN

#ifdef SHORT_PLUGS
        padding_size += dd_padding_size (dd);
#endif //SHORT_PLUGS
    }

    total_ephemeral_size += eph_gen_starts_size;

#ifdef RESPECT_LARGE_ALIGNMENT
    size_t planned_ephemeral_size = heap_segment_plan_allocated (ephemeral_heap_segment) -
                                       generation_plan_allocation_start (generation_of (max_generation-1));
    total_ephemeral_size = min (total_ephemeral_size, planned_ephemeral_size);
#endif //RESPECT_LARGE_ALIGNMENT

#ifdef SHORT_PLUGS
    total_ephemeral_size = Align ((size_t)((double)total_ephemeral_size * short_plugs_pad_ratio) + 1);
    total_ephemeral_size += Align (DESIRED_PLUG_LENGTH);
#endif //SHORT_PLUGS

    dprintf (3, ("total ephemeral size is %zx, padding %zx(%zx)",
        total_ephemeral_size,
        padding_size, (total_ephemeral_size - padding_size)));
}

heap_segment*
gc_heap::soh_get_segment_to_expand()
{
    size_t size = soh_segment_size;

    ordered_plug_indices_init = FALSE;
    use_bestfit = FALSE;

    //compute the size of the new ephemeral heap segment.
    compute_new_ephemeral_size();

    if ((settings.pause_mode != pause_low_latency) &&
        (settings.pause_mode != pause_no_gc)
#ifdef BACKGROUND_GC
        && (!gc_heap::background_running_p())
#endif //BACKGROUND_GC
        )
    {
        assert (settings.condemned_generation <= max_generation);
        allocator*  gen_alloc = ((settings.condemned_generation == max_generation) ? nullptr :
                              generation_allocator (generation_of (max_generation)));
        dprintf (2, ("(gen%d)soh_get_segment_to_expand", settings.condemned_generation));

        // try to find one in the gen 2 segment list, search backwards because the first segments
        // tend to be more compact than the later ones.
        heap_segment* fseg = heap_segment_rw (generation_start_segment (generation_of (max_generation)));

        _ASSERTE(fseg != NULL);

#ifdef SEG_REUSE_STATS
        int try_reuse = 0;
#endif //SEG_REUSE_STATS

        heap_segment* seg = ephemeral_heap_segment;
        while ((seg = heap_segment_prev_rw (fseg, seg)) && (seg != fseg))
        {
#ifdef SEG_REUSE_STATS
        try_reuse++;
#endif //SEG_REUSE_STATS

            if (can_expand_into_p (seg, size/3, total_ephemeral_size, gen_alloc))
            {
                get_gc_data_per_heap()->set_mechanism (gc_heap_expand,
                    (use_bestfit ? expand_reuse_bestfit : expand_reuse_normal));
                if (settings.condemned_generation == max_generation)
                {
                    if (use_bestfit)
                    {
                        build_ordered_free_spaces (seg);
                        dprintf (GTC_LOG, ("can use best fit"));
                    }

#ifdef SEG_REUSE_STATS
                    dprintf (SEG_REUSE_LOG_0, ("(gen%d)soh_get_segment_to_expand: found seg #%d to reuse",
                        settings.condemned_generation, try_reuse));
#endif //SEG_REUSE_STATS
                    dprintf (GTC_LOG, ("max_gen: Found existing segment to expand into %zx", (size_t)seg));
                    return seg;
                }
                else
                {
#ifdef SEG_REUSE_STATS
                    dprintf (SEG_REUSE_LOG_0, ("(gen%d)soh_get_segment_to_expand: found seg #%d to reuse - returning",
                        settings.condemned_generation, try_reuse));
#endif //SEG_REUSE_STATS
                    dprintf (GTC_LOG, ("max_gen-1: Found existing segment to expand into %zx", (size_t)seg));

                    // If we return 0 here, the allocator will think since we are short on end
                    // of seg we need to trigger a full compacting GC. So if sustained low latency
                    // is set we should acquire a new seg instead, that way we wouldn't be short.
                    // The real solution, of course, is to actually implement seg reuse in gen1.
                    if (settings.pause_mode != pause_sustained_low_latency)
                    {
                        dprintf (GTC_LOG, ("max_gen-1: SustainedLowLatency is set, acquire a new seg"));
                        get_gc_data_per_heap()->set_mechanism (gc_heap_expand, expand_next_full_gc);
                        return 0;
                    }
                }
            }
        }
    }

    heap_segment* result = get_segment (size, gc_oh_num::soh);

    if(result)
    {
#ifdef BACKGROUND_GC
        if (current_c_gc_state == c_gc_state_planning)
        {
            // When we expand heap during bgc sweep, we set the seg to be swept so
            // we'll always look at cards for objects on the new segment.
            result->flags |= heap_segment_flags_swept;
        }
#endif //BACKGROUND_GC

        FIRE_EVENT(GCCreateSegment_V1, heap_segment_mem(result),
                                  (size_t)(heap_segment_reserved (result) - heap_segment_mem(result)),
                                  gc_etw_segment_small_object_heap);
    }

    get_gc_data_per_heap()->set_mechanism (gc_heap_expand, (result ? expand_new_seg : expand_no_memory));

    if (result == 0)
    {
        dprintf (2, ("h%d: failed to allocate a new segment!", heap_number));
    }
    else
    {
#ifdef MULTIPLE_HEAPS
        heap_segment_heap (result) = this;
#endif //MULTIPLE_HEAPS
    }

    dprintf (GTC_LOG, ("(gen%d)creating new segment %p", settings.condemned_generation, result));
    return result;
}

//returns 0 in case of allocation failure
heap_segment*
gc_heap::get_segment (size_t size, gc_oh_num oh)
{
    assert(oh != gc_oh_num::unknown);
    BOOL uoh_p = (oh == gc_oh_num::loh) || (oh == gc_oh_num::poh);
    if (heap_hard_limit)
        return NULL;

    heap_segment* result = 0;

    if (segment_standby_list != 0)
    {
        result = segment_standby_list;
        heap_segment* last = 0;
        while (result)
        {
            size_t hs = (size_t)(heap_segment_reserved (result) - (uint8_t*)result);
            if ((hs >= size) && ((hs / 2) < size))
            {
                dprintf (2, ("Hoarded segment %zx found", (size_t) result));
                if (last)
                {
                    heap_segment_next (last) = heap_segment_next (result);
                }
                else
                {
                    segment_standby_list = heap_segment_next (result);
                }
                break;
            }
            else
            {
                last = result;
                result = heap_segment_next (result);
            }
        }
    }

    if (result)
    {
        init_heap_segment (result, __this);
#ifdef BACKGROUND_GC
        if (is_bgc_in_progress())
        {
            dprintf (GC_TABLE_LOG, ("hoarded seg %p, mark_array is %p", result, mark_array));
            if (!commit_mark_array_new_seg (__this, result))
            {
                dprintf (GC_TABLE_LOG, ("failed to commit mark array for hoarded seg"));
                // If we can't use it we need to thread it back.
                if (segment_standby_list != 0)
                {
                    heap_segment_next (result) = segment_standby_list;
                    segment_standby_list = result;
                }
                else
                {
                    segment_standby_list = result;
                }

                result = 0;
            }
        }
#endif //BACKGROUND_GC

        if (result)
            seg_mapping_table_add_segment (result, __this);
    }

    if (!result)
    {
        void* mem = virtual_alloc (size);
        if (!mem)
        {
            fgm_result.set_fgm (fgm_reserve_segment, size, uoh_p);
            return 0;
        }

        result = make_heap_segment ((uint8_t*)mem, size, __this, (oh + max_generation));

        if (result)
        {
            uint8_t* start;
            uint8_t* end;
            if (mem < g_gc_lowest_address)
            {
                start =  (uint8_t*)mem;
            }
            else
            {
                start = (uint8_t*)g_gc_lowest_address;
            }

            if (((uint8_t*)mem + size) > g_gc_highest_address)
            {
                end = (uint8_t*)mem + size;
            }
            else
            {
                end = (uint8_t*)g_gc_highest_address;
            }

            if (gc_heap::grow_brick_card_tables (start, end, size, result, __this, uoh_p) != 0)
            {
                // release_segment needs the flags to decrement the proper bucket
                size_t flags = 0;
                if (oh == poh)
                {
                    flags = heap_segment_flags_poh;
                }
                else if (oh == loh)
                {
                    flags = heap_segment_flags_loh;
                }
                result->flags |= flags;
                release_segment (result);
                return 0;
            }
        }
        else
        {
            fgm_result.set_fgm (fgm_commit_segment_beg, SEGMENT_INITIAL_COMMIT, uoh_p);
            virtual_free (mem, size);
        }

        if (result)
        {
            seg_mapping_table_add_segment (result, __this);
        }
    }

#ifdef BACKGROUND_GC
    if (result)
    {
        ::record_changed_seg ((uint8_t*)result, heap_segment_reserved (result),
                            settings.gc_index, current_bgc_state,
                            seg_added);
        bgc_verify_mark_array_cleared (result);
    }
#endif //BACKGROUND_GC

    dprintf (GC_TABLE_LOG, ("h%d: new seg: %p-%p (%zd)", heap_number, result, ((uint8_t*)result + size), size));
    return result;
}

#endif //!USE_REGIONS

#ifdef MULTIPLE_HEAPS
#ifdef HOST_X86
#ifdef _MSC_VER
#pragma warning(disable:4035)
    static ptrdiff_t  get_cycle_count()
    {
        __asm   rdtsc
    }
#pragma warning(default:4035)
#elif defined(__GNUC__)
    static ptrdiff_t  get_cycle_count()
    {
        ptrdiff_t cycles;
        ptrdiff_t cyclesHi;
        __asm__ __volatile__
        ("rdtsc":"=a" (cycles), "=d" (cyclesHi));
        return cycles;
    }
#else //_MSC_VER
#error Unknown compiler
#endif //_MSC_VER
#elif defined(TARGET_AMD64)
#ifdef _MSC_VER
extern "C" uint64_t __rdtsc();
#pragma intrinsic(__rdtsc)
    static ptrdiff_t get_cycle_count()
    {
        return (ptrdiff_t)__rdtsc();
    }
#elif defined(__GNUC__)
    static ptrdiff_t get_cycle_count()
    {
        ptrdiff_t cycles;
        ptrdiff_t cyclesHi;
        __asm__ __volatile__
        ("rdtsc":"=a" (cycles), "=d" (cyclesHi));
        return (cyclesHi << 32) | cycles;
    }
#else // _MSC_VER
    extern "C" ptrdiff_t get_cycle_count(void);
#endif // _MSC_VER
#elif defined(TARGET_LOONGARCH64)
    static ptrdiff_t get_cycle_count()
    {
        ////FIXME: TODO for LOONGARCH64:
        //ptrdiff_t  cycle;
        __asm__ volatile ("break 0 \n");
        return 0;
    }
#else
    static ptrdiff_t get_cycle_count()
    {
        // @ARMTODO, @ARM64TODO, @WASMTODO: cycle counter is not exposed to user mode. For now (until we can show this
        // makes a difference on the configurations on which we'll run) just return 0. This will result in
        // all buffer access times being reported as equal in access_time().
        return 0;
    }
#endif //TARGET_X86

// We may not be on contiguous numa nodes so need to store
// the node index as well.
struct node_heap_count
{
    int node_no;
    int heap_count;
};

class heap_select
{
    heap_select() {}
public:
    static uint8_t* sniff_buffer;
    static unsigned n_sniff_buffers;
    static unsigned cur_sniff_index;

    static uint16_t proc_no_to_heap_no[MAX_SUPPORTED_CPUS];
    static uint16_t heap_no_to_proc_no[MAX_SUPPORTED_CPUS];
    static uint16_t heap_no_to_numa_node[MAX_SUPPORTED_CPUS];
    static uint16_t numa_node_to_heap_map[MAX_SUPPORTED_CPUS+4];

#ifdef HEAP_BALANCE_INSTRUMENTATION
    // Note this is the total numa nodes GC heaps are on. There might be
    // more on the machine if GC threads aren't using all of them.
    static uint16_t total_numa_nodes;
    static node_heap_count heaps_on_node[MAX_SUPPORTED_NODES];
#endif

    static int access_time(uint8_t *sniff_buffer, int heap_number, unsigned sniff_index, unsigned n_sniff_buffers)
    {
        ptrdiff_t start_cycles = get_cycle_count();
        uint8_t sniff = sniff_buffer[(1 + heap_number*n_sniff_buffers + sniff_index)*HS_CACHE_LINE_SIZE];
        assert (sniff == 0);
        ptrdiff_t elapsed_cycles = get_cycle_count() - start_cycles;
        // add sniff here just to defeat the optimizer
        elapsed_cycles += sniff;
        return (int) elapsed_cycles;
    }

public:
    static BOOL init(int n_heaps)
    {
        assert (sniff_buffer == NULL && n_sniff_buffers == 0);
        if (!GCToOSInterface::CanGetCurrentProcessorNumber())
        {
            n_sniff_buffers = n_heaps*2+1;
            size_t n_cache_lines = 1 + n_heaps * n_sniff_buffers + 1;
            size_t sniff_buf_size = n_cache_lines * HS_CACHE_LINE_SIZE;
            if (sniff_buf_size / HS_CACHE_LINE_SIZE != n_cache_lines) // check for overlow
            {
                return FALSE;
            }

            sniff_buffer = new (nothrow) uint8_t[sniff_buf_size];
            if (sniff_buffer == 0)
                return FALSE;
            memset(sniff_buffer, 0, sniff_buf_size*sizeof(uint8_t));
        }

        bool do_numa = GCToOSInterface::CanEnableGCNumaAware();

        // we want to assign heap indices such that there is a contiguous
        // range of heap numbers for each numa node

        // we do this in two passes:
        // 1. gather processor numbers and numa node numbers for all heaps
        // 2. assign heap numbers for each numa node

        // Pass 1: gather processor numbers and numa node numbers
        uint16_t proc_no[MAX_SUPPORTED_CPUS];
        uint16_t node_no[MAX_SUPPORTED_CPUS];
        uint16_t max_node_no = 0;
        uint16_t heap_num;
        for (heap_num = 0; heap_num < n_heaps; heap_num++)
        {
            if (!GCToOSInterface::GetProcessorForHeap (heap_num, &proc_no[heap_num], &node_no[heap_num]))
                break;
            assert(proc_no[heap_num] < MAX_SUPPORTED_CPUS);
            if (!do_numa || node_no[heap_num] == NUMA_NODE_UNDEFINED)
                node_no[heap_num] = 0;
            max_node_no = max(max_node_no, node_no[heap_num]);
        }

        // Pass 2: assign heap numbers by numa node
        int cur_heap_no = 0;
        for (uint16_t cur_node_no = 0; cur_node_no <= max_node_no; cur_node_no++)
        {
            for (int i = 0; i < heap_num; i++)
            {
                if (node_no[i] != cur_node_no)
                    continue;

                // we found a heap on cur_node_no
                heap_no_to_proc_no[cur_heap_no] = proc_no[i];
                heap_no_to_numa_node[cur_heap_no] = cur_node_no;

                cur_heap_no++;
            }
        }

        return TRUE;
    }

    static void init_cpu_mapping(int heap_number)
    {
        if (GCToOSInterface::CanGetCurrentProcessorNumber())
        {
            uint32_t proc_no = GCToOSInterface::GetCurrentProcessorNumber();
            // For a 32-bit process running on a machine with > 64 procs,
            // even though the process can only use up to 32 procs, the processor
            // index can be >= 64; or in the cpu group case, if the process is not running in cpu group #0,
            // the GetCurrentProcessorNumber will return a number that's >= 64.
            proc_no_to_heap_no[proc_no % MAX_SUPPORTED_CPUS] = (uint16_t)heap_number;
        }
    }

    static void mark_heap(int heap_number)
    {
        if (GCToOSInterface::CanGetCurrentProcessorNumber())
            return;

        for (unsigned sniff_index = 0; sniff_index < n_sniff_buffers; sniff_index++)
            sniff_buffer[(1 + heap_number*n_sniff_buffers + sniff_index)*HS_CACHE_LINE_SIZE] &= 1;
    }

    static int select_heap(alloc_context* acontext)
    {
#ifndef TRACE_GC
        UNREFERENCED_PARAMETER(acontext); // only referenced by dprintf
#endif //TRACE_GC

        if (GCToOSInterface::CanGetCurrentProcessorNumber())
        {
            uint32_t proc_no = GCToOSInterface::GetCurrentProcessorNumber();
            // For a 32-bit process running on a machine with > 64 procs,
            // even though the process can only use up to 32 procs, the processor
            // index can be >= 64; or in the cpu group case, if the process is not running in cpu group #0,
            // the GetCurrentProcessorNumber will return a number that's >= 64.
            int adjusted_heap = proc_no_to_heap_no[proc_no % MAX_SUPPORTED_CPUS];
            // with dynamic heap count, need to make sure the value is in range.
            if (adjusted_heap >= gc_heap::n_heaps)
            {
                adjusted_heap %= gc_heap::n_heaps;
            }
            return adjusted_heap;
        }

        unsigned sniff_index = Interlocked::Increment(&cur_sniff_index);
        sniff_index %= n_sniff_buffers;

        int best_heap = 0;
        int best_access_time = 1000*1000*1000;
        int second_best_access_time = best_access_time;

        uint8_t *l_sniff_buffer = sniff_buffer;
        unsigned l_n_sniff_buffers = n_sniff_buffers;
        for (int heap_number = 0; heap_number < gc_heap::n_heaps; heap_number++)
        {
            int this_access_time = access_time(l_sniff_buffer, heap_number, sniff_index, l_n_sniff_buffers);
            if (this_access_time < best_access_time)
            {
                second_best_access_time = best_access_time;
                best_access_time = this_access_time;
                best_heap = heap_number;
            }
            else if (this_access_time < second_best_access_time)
            {
                second_best_access_time = this_access_time;
            }
        }

        if (best_access_time*2 < second_best_access_time)
        {
            sniff_buffer[(1 + best_heap*n_sniff_buffers + sniff_index)*HS_CACHE_LINE_SIZE] &= 1;

            dprintf (3, ("select_heap yields crisp %d for context %p\n", best_heap, (void *)acontext));
        }
        else
        {
            dprintf (3, ("select_heap yields vague %d for context %p\n", best_heap, (void *)acontext ));
        }

        return best_heap;
    }

    static bool can_find_heap_fast()
    {
        return GCToOSInterface::CanGetCurrentProcessorNumber();
    }

    static uint16_t find_proc_no_from_heap_no(int heap_number)
    {
        return heap_no_to_proc_no[heap_number];
    }

    static uint16_t find_numa_node_from_heap_no(int heap_number)
    {
        return heap_no_to_numa_node[heap_number];
    }

    static void init_numa_node_to_heap_map(int nheaps)
    {
        // Called right after GCHeap::Init() for each heap
        // For each NUMA node used by the heaps, the
        // numa_node_to_heap_map[numa_node] is set to the first heap number on that node and
        // numa_node_to_heap_map[numa_node + 1] is set to the first heap number not on that node
        // Set the start of the heap number range for the first NUMA node
        numa_node_to_heap_map[heap_no_to_numa_node[0]] = 0;
#ifdef HEAP_BALANCE_INSTRUMENTATION
        total_numa_nodes = 0;
        memset (heaps_on_node, 0, sizeof (heaps_on_node));
        heaps_on_node[0].node_no = heap_no_to_numa_node[0];
        heaps_on_node[0].heap_count = 1;
#endif //HEAP_BALANCE_INSTRUMENTATION

        for (int i=1; i < nheaps; i++)
        {
            if (heap_no_to_numa_node[i] != heap_no_to_numa_node[i-1])
            {
#ifdef HEAP_BALANCE_INSTRUMENTATION
                total_numa_nodes++;
                heaps_on_node[total_numa_nodes].node_no = heap_no_to_numa_node[i];
#endif

                // Set the end of the heap number range for the previous NUMA node
                numa_node_to_heap_map[heap_no_to_numa_node[i-1] + 1] =
                // Set the start of the heap number range for the current NUMA node
                numa_node_to_heap_map[heap_no_to_numa_node[i]] = (uint16_t)i;
            }
#ifdef HEAP_BALANCE_INSTRUMENTATION
            (heaps_on_node[total_numa_nodes].heap_count)++;
#endif
        }

        // Set the end of the heap range for the last NUMA node
        numa_node_to_heap_map[heap_no_to_numa_node[nheaps-1] + 1] = (uint16_t)nheaps; //mark the end with nheaps

#ifdef HEAP_BALANCE_INSTRUMENTATION
        total_numa_nodes++;
#endif
    }

    static bool get_info_proc (int index, uint16_t* proc_no, uint16_t* node_no, int* start_heap, int* end_heap)
    {
        if (!GCToOSInterface::GetProcessorForHeap ((uint16_t)index, proc_no, node_no))
            return false;

        if (*node_no == NUMA_NODE_UNDEFINED)
            *node_no = 0;

        *start_heap = (int)numa_node_to_heap_map[*node_no];
        *end_heap = (int)(numa_node_to_heap_map[*node_no + 1]);

        return true;
    }

    static void distribute_other_procs (bool distribute_all_p)
    {
        if (affinity_config_specified_p)
            return;

        if (distribute_all_p)
        {
            uint16_t current_heap_no_on_node[MAX_SUPPORTED_CPUS];
            memset (current_heap_no_on_node, 0, sizeof (current_heap_no_on_node));
            uint16_t current_heap_no = 0;

            uint16_t proc_no = 0;
            uint16_t node_no = 0;

            for (int i = gc_heap::n_heaps; i < (int)g_num_active_processors; i++)
            {
                int start_heap, end_heap;
                if (!get_info_proc (i, &proc_no, &node_no, &start_heap, &end_heap))
                    break;

                // This indicates there are heaps on this node
                if ((end_heap - start_heap) > 0)
                {
                    proc_no_to_heap_no[proc_no] = (current_heap_no_on_node[node_no] % (uint16_t)(end_heap - start_heap)) + (uint16_t)start_heap;
                    (current_heap_no_on_node[node_no])++;
                }
                else
                {
                    proc_no_to_heap_no[proc_no] = current_heap_no % gc_heap::n_heaps;
                    (current_heap_no)++;
                }
            }
        }
        else
        {
            // This is for scenarios where GCHeapCount is specified as something like
            // (g_num_active_processors - 2) to allow less randomization to the Server GC threads.
            // In this case we want to assign the right heaps to those procs, ie if they share
            // the same numa node we want to assign local heaps to those procs. Otherwise we
            // let the heap balancing mechanism take over for now.
            uint16_t proc_no = 0;
            uint16_t node_no = 0;
            int current_node_no = -1;
            int current_heap_on_node = -1;

            for (int i = gc_heap::n_heaps; i < (int)g_num_active_processors; i++)
            {
                int start_heap, end_heap;
                if (!get_info_proc (i, &proc_no, &node_no, &start_heap, &end_heap))
                    break;

                if ((end_heap - start_heap) > 0)
                {
                    if (node_no == current_node_no)
                    {
                        // We already iterated through all heaps on this node, don't add more procs to these
                        // heaps.
                        if (current_heap_on_node >= end_heap)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        current_node_no = node_no;
                        current_heap_on_node = start_heap;
                    }

                    proc_no_to_heap_no[proc_no] = (uint16_t)current_heap_on_node;

                    current_heap_on_node++;
                }
            }
        }
    }

    static void get_heap_range_for_heap(int hn, int* start, int* end)
    {
        uint16_t numa_node = heap_no_to_numa_node[hn];
        *start = (int)numa_node_to_heap_map[numa_node];
        *end   = (int)(numa_node_to_heap_map[numa_node+1]);
#ifdef HEAP_BALANCE_INSTRUMENTATION
        dprintf(HEAP_BALANCE_TEMP_LOG, ("TEMPget_heap_range: %d is in numa node %d, start = %d, end = %d", hn, numa_node, *start, *end));
#endif //HEAP_BALANCE_INSTRUMENTATION
    }
};
uint8_t* heap_select::sniff_buffer;
unsigned heap_select::n_sniff_buffers;
unsigned heap_select::cur_sniff_index;
uint16_t heap_select::proc_no_to_heap_no[MAX_SUPPORTED_CPUS];
uint16_t heap_select::heap_no_to_proc_no[MAX_SUPPORTED_CPUS];
uint16_t heap_select::heap_no_to_numa_node[MAX_SUPPORTED_CPUS];
uint16_t heap_select::numa_node_to_heap_map[MAX_SUPPORTED_CPUS+4];
#ifdef HEAP_BALANCE_INSTRUMENTATION
uint16_t  heap_select::total_numa_nodes;
node_heap_count heap_select::heaps_on_node[MAX_SUPPORTED_NODES];
#endif

#ifdef HEAP_BALANCE_INSTRUMENTATION
// This records info we use to look at effect of different strategies
// for heap balancing.
struct heap_balance_info
{
    uint64_t timestamp;
    // This also encodes when we detect the thread runs on
    // different proc during a balance attempt. Sometimes
    // I observe this happens multiple times during one attempt!
    // If this happens, I just record the last proc we observe
    // and set MSB.
    int tid;
    // This records the final alloc_heap for the thread.
    //
    // This also encodes the reason why we needed to set_home_heap
    // in balance_heaps.
    // If we set it because the home heap is not the same as the proc,
    // we set MSB.
    //
    // If we set ideal proc, we set the 2nd MSB.
    int alloc_heap;
    int ideal_proc_no;
};

// This means inbetween each GC we can log at most this many entries per proc.
// This is usually enough. Most of the time we only need to log something every 128k
// of allocations in balance_heaps and gen0 budget is <= 200mb.
#define default_max_hb_heap_balance_info 4096

struct heap_balance_info_proc
{
    int count;
    int index;
    heap_balance_info hb_info[default_max_hb_heap_balance_info];
};

struct heap_balance_info_numa
{
    heap_balance_info_proc* hb_info_procs;
};

uint64_t start_raw_ts = 0;
bool cpu_group_enabled_p = false;
uint32_t procs_per_numa_node = 0;
uint16_t total_numa_nodes_on_machine = 0;
uint32_t procs_per_cpu_group = 0;
uint16_t total_cpu_groups_on_machine = 0;
// Note this is still on one of the numa nodes, so we'll incur a remote access
// no matter what.
heap_balance_info_numa* hb_info_numa_nodes = NULL;

// TODO: This doesn't work for multiple nodes per CPU group yet.
int get_proc_index_numa (int proc_no, int* numa_no)
{
    if (total_numa_nodes_on_machine == 1)
    {
        *numa_no = 0;
        return proc_no;
    }
    else
    {
        if (cpu_group_enabled_p)
        {
            // see vm\gcenv.os.cpp GroupProcNo implementation.
            *numa_no = proc_no >> 6;
            return (proc_no % 64);
        }
        else
        {
            *numa_no = proc_no / procs_per_numa_node;
            return (proc_no % procs_per_numa_node);
        }
    }
}



const int hb_log_buffer_size = 4096;
static char hb_log_buffer[hb_log_buffer_size];
int last_hb_recorded_gc_index = -1;
#endif //HEAP_BALANCE_INSTRUMENTATION

void set_thread_affinity_for_heap (int heap_number, uint16_t proc_no)
{
    if (!GCToOSInterface::SetThreadAffinity (proc_no))
    {
        dprintf (1, ("Failed to set thread affinity for GC thread %d on proc #%d", heap_number, proc_no));
    }
}

#endif //MULTIPLE_HEAPS

class mark
{
public:
    uint8_t* first;
    size_t len;

    // If we want to save space we can have a pool of plug_and_gap's instead of
    // always having 2 allocated for each pinned plug.
    gap_reloc_pair saved_pre_plug;
    // If we decide to not compact, we need to restore the original values.
    gap_reloc_pair saved_pre_plug_reloc;

    gap_reloc_pair saved_post_plug;

    // Supposedly Pinned objects cannot have references but we are seeing some from pinvoke
    // frames. Also if it's an artificially pinned plug created by us, it can certainly
    // have references.
    // We know these cases will be rare so we can optimize this to be only allocated on demand.
    gap_reloc_pair saved_post_plug_reloc;

    // We need to calculate this after we are done with plan phase and before compact
    // phase because compact phase will change the bricks so relocate_address will no
    // longer work.
    uint8_t* saved_pre_plug_info_reloc_start;

    // We need to save this because we will have no way to calculate it, unlike the
    // pre plug info start which is right before this plug.
    uint8_t* saved_post_plug_info_start;

#ifdef SHORT_PLUGS
    uint8_t* allocation_context_start_region;
#endif //SHORT_PLUGS

    // How the bits in these bytes are organized:
    // MSB --> LSB
    // bit to indicate whether it's a short obj | 3 bits for refs in this short obj | 2 unused bits | bit to indicate if it's collectible | last bit
    // last bit indicates if there's pre or post info associated with this plug. If it's not set all other bits will be 0.
    BOOL saved_pre_p;
    BOOL saved_post_p;

#ifdef _DEBUG
    // We are seeing this is getting corrupted for a PP with a NP after.
    // Save it when we first set it and make sure it doesn't change.
    gap_reloc_pair saved_post_plug_debug;
#endif //_DEBUG

    size_t get_max_short_bits()
    {
        return (sizeof (gap_reloc_pair) / sizeof (uint8_t*));
    }

    // pre bits
    size_t get_pre_short_start_bit ()
    {
        return (sizeof (saved_pre_p) * 8 - 1 - (sizeof (gap_reloc_pair) / sizeof (uint8_t*)));
    }

    BOOL pre_short_p()
    {
        return (saved_pre_p & (1 << (sizeof (saved_pre_p) * 8 - 1)));
    }

    void set_pre_short()
    {
        saved_pre_p |= (1 << (sizeof (saved_pre_p) * 8 - 1));
    }

    void set_pre_short_bit (size_t bit)
    {
        saved_pre_p |= 1 << (get_pre_short_start_bit() + bit);
    }

    BOOL pre_short_bit_p (size_t bit)
    {
        return (saved_pre_p & (1 << (get_pre_short_start_bit() + bit)));
    }

#ifdef COLLECTIBLE_CLASS
    void set_pre_short_collectible()
    {
        saved_pre_p |= 2;
    }

    BOOL pre_short_collectible_p()
    {
        return (saved_pre_p & 2);
    }
#endif //COLLECTIBLE_CLASS

    // post bits
    size_t get_post_short_start_bit ()
    {
        return (sizeof (saved_post_p) * 8 - 1 - (sizeof (gap_reloc_pair) / sizeof (uint8_t*)));
    }

    BOOL post_short_p()
    {
        return (saved_post_p & (1 << (sizeof (saved_post_p) * 8 - 1)));
    }

    void set_post_short()
    {
        saved_post_p |= (1 << (sizeof (saved_post_p) * 8 - 1));
    }

    void set_post_short_bit (size_t bit)
    {
        saved_post_p |= 1 << (get_post_short_start_bit() + bit);
    }

    BOOL post_short_bit_p (size_t bit)
    {
        return (saved_post_p & (1 << (get_post_short_start_bit() + bit)));
    }

#ifdef COLLECTIBLE_CLASS
    void set_post_short_collectible()
    {
        saved_post_p |= 2;
    }

    BOOL post_short_collectible_p()
    {
        return (saved_post_p & 2);
    }
#endif //COLLECTIBLE_CLASS

    uint8_t* get_plug_address() { return first; }

    BOOL has_pre_plug_info() { return saved_pre_p; }
    BOOL has_post_plug_info() { return saved_post_p; }

    gap_reloc_pair* get_pre_plug_reloc_info() { return &saved_pre_plug_reloc; }
    gap_reloc_pair* get_post_plug_reloc_info() { return &saved_post_plug_reloc; }
    void set_pre_plug_info_reloc_start (uint8_t* reloc) { saved_pre_plug_info_reloc_start = reloc; }
    uint8_t* get_post_plug_info_start() { return saved_post_plug_info_start; }

    // We need to temporarily recover the shortened plugs for compact phase so we can
    // copy over the whole plug and their related info (mark bits/cards). But we will
    // need to set the artificial gap back so compact phase can keep reading the plug info.
    // We also need to recover the saved info because we'll need to recover it later.
    //
    // So we would call swap_p*_plug_and_saved once to recover the object info; then call
    // it again to recover the artificial gap.
    void swap_pre_plug_and_saved()
    {
        gap_reloc_pair temp;
        memcpy (&temp, (first - sizeof (plug_and_gap)), sizeof (temp));
        memcpy ((first - sizeof (plug_and_gap)), &saved_pre_plug_reloc, sizeof (saved_pre_plug_reloc));
        saved_pre_plug_reloc = temp;
    }

    void swap_post_plug_and_saved()
    {
        gap_reloc_pair temp;
        memcpy (&temp, saved_post_plug_info_start, sizeof (temp));
        memcpy (saved_post_plug_info_start, &saved_post_plug_reloc, sizeof (saved_post_plug_reloc));
        saved_post_plug_reloc = temp;
    }

    void swap_pre_plug_and_saved_for_profiler()
    {
        gap_reloc_pair temp;
        memcpy (&temp, (first - sizeof (plug_and_gap)), sizeof (temp));
        memcpy ((first - sizeof (plug_and_gap)), &saved_pre_plug, sizeof (saved_pre_plug));
        saved_pre_plug = temp;
    }

    void swap_post_plug_and_saved_for_profiler()
    {
        gap_reloc_pair temp;
        memcpy (&temp, saved_post_plug_info_start, sizeof (temp));
        memcpy (saved_post_plug_info_start, &saved_post_plug, sizeof (saved_post_plug));
        saved_post_plug = temp;
    }

    // We should think about whether it's really necessary to have to copy back the pre plug
    // info since it was already copied during compacting plugs. But if a plug doesn't move
    // by >= 3 ptr size (the size of gap_reloc_pair), it means we'd have to recover pre plug info.
    size_t recover_plug_info()
    {
        // We need to calculate the size for sweep case in order to correctly record the
        // free_obj_space - sweep would've made these artificial gaps into free objects and
        // we would need to deduct the size because now we are writing into those free objects.
        size_t recovered_sweep_size = 0;

        if (saved_pre_p)
        {
            if (gc_heap::settings.compaction)
            {
                dprintf (3, ("%p: REC Pre: %p-%p",
                    first,
                    &saved_pre_plug_reloc,
                    saved_pre_plug_info_reloc_start));
                memcpy (saved_pre_plug_info_reloc_start, &saved_pre_plug_reloc, sizeof (saved_pre_plug_reloc));
            }
            else
            {
                dprintf (3, ("%p: REC Pre: %p-%p",
                    first,
                    &saved_pre_plug,
                    (first - sizeof (plug_and_gap))));
                memcpy ((first - sizeof (plug_and_gap)), &saved_pre_plug, sizeof (saved_pre_plug));
                recovered_sweep_size += sizeof (saved_pre_plug);
            }
        }

        if (saved_post_p)
        {
            if (gc_heap::settings.compaction)
            {
                dprintf (3, ("%p: REC Post: %p-%p",
                    first,
                    &saved_post_plug_reloc,
                    saved_post_plug_info_start));
                memcpy (saved_post_plug_info_start, &saved_post_plug_reloc, sizeof (saved_post_plug_reloc));
            }
            else
            {
                dprintf (3, ("%p: REC Post: %p-%p",
                    first,
                    &saved_post_plug,
                    saved_post_plug_info_start));
                memcpy (saved_post_plug_info_start, &saved_post_plug, sizeof (saved_post_plug));
                recovered_sweep_size += sizeof (saved_post_plug);
            }
        }

        return recovered_sweep_size;
    }
};


void gc_mechanisms::init_mechanisms()
{
    condemned_generation = 0;
    promotion = FALSE;//TRUE;
    compaction = TRUE;
#ifdef FEATURE_LOH_COMPACTION
    loh_compaction = gc_heap::loh_compaction_requested();
#else
    loh_compaction = FALSE;
#endif //FEATURE_LOH_COMPACTION
    heap_expansion = FALSE;
    concurrent = FALSE;
    demotion = FALSE;
    elevation_reduced = FALSE;
    found_finalizers = FALSE;
#ifdef BACKGROUND_GC
    background_p = gc_heap::background_running_p() != FALSE;
#endif //BACKGROUND_GC

    entry_memory_load = 0;
    entry_available_physical_mem = 0;
    exit_memory_load = 0;

#ifdef STRESS_HEAP
    stress_induced = FALSE;
#endif // STRESS_HEAP
}

void gc_mechanisms::first_init()
{
    gc_index = 0;
    gen0_reduction_count = 0;
    should_lock_elevation = FALSE;
    elevation_locked_count = 0;
    reason = reason_empty;
#ifdef BACKGROUND_GC
    pause_mode = gc_heap::gc_can_use_concurrent ? pause_interactive : pause_batch;
#ifdef _DEBUG
    int debug_pause_mode = static_cast<int>(GCConfig::GetLatencyMode());
    if (debug_pause_mode >= 0)
    {
        assert (debug_pause_mode <= pause_sustained_low_latency);
        pause_mode = (gc_pause_mode)debug_pause_mode;
    }
#endif //_DEBUG
#else //BACKGROUND_GC
    pause_mode = pause_batch;
#endif //BACKGROUND_GC

    init_mechanisms();
}

void gc_mechanisms::record (gc_history_global* history)
{
#ifdef MULTIPLE_HEAPS
    history->num_heaps = gc_heap::n_heaps;
#else
    history->num_heaps = 1;
#endif //MULTIPLE_HEAPS

    history->condemned_generation = condemned_generation;
    history->gen0_reduction_count = gen0_reduction_count;
    history->reason = reason;
    history->pause_mode = (int)pause_mode;
    history->mem_pressure = entry_memory_load;
    history->global_mechanisms_p = 0;

    // start setting the boolean values.
    if (concurrent)
        history->set_mechanism_p (global_concurrent);

    if (compaction)
        history->set_mechanism_p (global_compaction);

    if (promotion)
        history->set_mechanism_p (global_promotion);

    if (demotion)
        history->set_mechanism_p (global_demotion);

    if (card_bundles)
        history->set_mechanism_p (global_card_bundles);

    if (elevation_reduced)
        history->set_mechanism_p (global_elevation);
}

/**********************************
   called at the beginning of GC to fix the allocated size to
   what is really allocated, or to turn the free area into an unused object
   It needs to be called after all of the other allocation contexts have been
   fixed since it relies on alloc_allocated.
 ********************************/



inline
BOOL grow_mark_stack (mark*& m, size_t& len, size_t init_len)
{
    size_t new_size = max (init_len, 2*len);
    mark* tmp = new (nothrow) mark [new_size];
    if (tmp)
    {
        memcpy (tmp, m, len * sizeof (mark));
        delete[] m;
        m = tmp;
        len = new_size;
        return TRUE;
    }
    else
    {
        dprintf (1, ("Failed to allocate %zd bytes for mark stack", (len * sizeof (mark))));
        return FALSE;
    }
}

inline
uint8_t* pinned_plug (mark* m)
{
   return m->first;
}

inline
size_t& pinned_len (mark* m)
{
    return m->len;
}

inline
void set_new_pin_info (mark* m, uint8_t* pin_free_space_start)
{
    m->len = pinned_plug (m) - pin_free_space_start;
#ifdef SHORT_PLUGS
    m->allocation_context_start_region = pin_free_space_start;
#endif //SHORT_PLUGS
}

#ifdef SHORT_PLUGS
inline
uint8_t*& pin_allocation_context_start_region (mark* m)
{
    return m->allocation_context_start_region;
}

uint8_t* get_plug_start_in_saved (uint8_t* old_loc, mark* pinned_plug_entry)
{
    uint8_t* saved_pre_plug_info = (uint8_t*)(pinned_plug_entry->get_pre_plug_reloc_info());
    uint8_t* plug_start_in_saved = saved_pre_plug_info + (old_loc - (pinned_plug (pinned_plug_entry) - sizeof (plug_and_gap)));
    //dprintf (2, ("detected a very short plug: %zx before PP %zx, pad %zx",
    //    old_loc, pinned_plug (pinned_plug_entry), plug_start_in_saved));
    dprintf (2, ("EP: %p(%p), %p", old_loc, pinned_plug (pinned_plug_entry), plug_start_in_saved));
    return plug_start_in_saved;
}


#endif //SHORT_PLUGS

#ifdef CARD_BUNDLE
// The card bundle keeps track of groups of card words.
static const size_t card_bundle_word_width = 32;

// How do we express the fact that 32 bits (card_word_width) is one uint32_t?
static const size_t card_bundle_size = (size_t)(GC_PAGE_SIZE / (sizeof(uint32_t)*card_bundle_word_width));

inline
size_t card_bundle_word (size_t cardb)
{
    return cardb / card_bundle_word_width;
}

inline
uint32_t card_bundle_bit (size_t cardb)
{
    return (uint32_t)(cardb % card_bundle_word_width);
}

size_t align_cardw_on_bundle (size_t cardw)
{
    return ((size_t)(cardw + card_bundle_size - 1) & ~(card_bundle_size - 1 ));
}

// Get the card bundle representing a card word
size_t cardw_card_bundle (size_t cardw)
{
    return cardw / card_bundle_size;
}

// Get the first card word in a card bundle
size_t card_bundle_cardw (size_t cardb)
{
    return cardb * card_bundle_size;
}


// Takes a pointer to a card bundle table and an address, and returns a pointer that represents
// where a theoretical card bundle table that represents every address (starting from 0) would
// start if the bundle word representing the address were to be located at the pointer passed in.
// The returned 'translated' pointer makes it convenient/fast to calculate where the card bundle
// for a given address is using a simple shift operation on the address.
uint32_t* translate_card_bundle_table (uint32_t* cb, uint8_t* lowest_address)
{
    // The number of bytes of heap memory represented by a card bundle word
    const size_t heap_bytes_for_bundle_word = card_size * card_word_width * card_bundle_size * card_bundle_word_width;

    // Each card bundle word is 32 bits
    return (uint32_t*)((uint8_t*)cb - (((size_t)lowest_address / heap_bytes_for_bundle_word) * sizeof (uint32_t)));
}

#endif // CARD_BUNDLE

#if defined (HOST_64BIT)
#define brick_size ((size_t)4096)
#else
#define brick_size ((size_t)2048)
#endif //HOST_64BIT

inline
uint8_t* align_on_brick (uint8_t* add)
{
    return (uint8_t*)((size_t)(add + brick_size - 1) & ~(brick_size - 1));
}

inline
uint8_t* align_lower_brick (uint8_t* add)
{
    return (uint8_t*)(((size_t)add) & ~(brick_size - 1));
}

size_t size_brick_of (uint8_t* from, uint8_t* end)
{
    assert (((size_t)from & (brick_size-1)) == 0);
    assert (((size_t)end  & (brick_size-1)) == 0);

    return ((end - from) / brick_size) * sizeof (short);
}

inline
uint8_t* align_on_card (uint8_t* add)
{
    return (uint8_t*)((size_t)(add + card_size - 1) & ~(card_size - 1 ));
}
inline
uint8_t* align_on_card_word (uint8_t* add)
{
    return (uint8_t*) ((size_t)(add + (card_size*card_word_width)-1) & ~(card_size*card_word_width - 1));
}

inline
uint8_t* align_lower_card (uint8_t* add)
{
    return (uint8_t*)((size_t)add & ~(card_size-1));
}

// Returns the number of DWORDs in the card table that cover the
// range of addresses [from, end[.
size_t count_card_of (uint8_t* from, uint8_t* end)
{
    return card_word (gcard_of (end - 1)) - card_word (gcard_of (from)) + 1;
}

// Returns the number of bytes to allocate for a card table
// that covers the range of addresses [from, end[.
size_t size_card_of (uint8_t* from, uint8_t* end)
{
    return count_card_of (from, end) * sizeof(uint32_t);
}

// We don't store seg_mapping_table in card_table_info because there's only always one view.
class card_table_info
{
public:
    unsigned    recount;
    size_t      size;
    uint32_t*   next_card_table;

    uint8_t*    lowest_address;
    uint8_t*    highest_address;
    short*      brick_table;

#ifdef CARD_BUNDLE
    uint32_t*   card_bundle_table;
#endif //CARD_BUNDLE

    // mark_array is always at the end of the data structure because we
    // want to be able to make one commit call for everything before it.
#ifdef BACKGROUND_GC
    uint32_t*   mark_array;
#endif //BACKGROUND_GC
};

static_assert(offsetof(dac_card_table_info, size) == offsetof(card_table_info, size), "DAC card_table_info layout mismatch");
static_assert(offsetof(dac_card_table_info, next_card_table) == offsetof(card_table_info, next_card_table), "DAC card_table_info layout mismatch");

//These are accessors on untranslated cardtable
inline
unsigned& card_table_refcount (uint32_t* c_table)
{
    return *(unsigned*)((char*)c_table - sizeof (card_table_info));
}

inline
uint8_t*& card_table_lowest_address (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->lowest_address;
}

uint32_t* translate_card_table (uint32_t* ct)
{
    return (uint32_t*)((uint8_t*)ct - card_word (gcard_of (card_table_lowest_address (ct))) * sizeof(uint32_t));
}

inline
uint8_t*& card_table_highest_address (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->highest_address;
}

inline
short*& card_table_brick_table (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->brick_table;
}

#ifdef CARD_BUNDLE
inline
uint32_t*& card_table_card_bundle_table (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->card_bundle_table;
}
#endif //CARD_BUNDLE

#ifdef BACKGROUND_GC
inline
uint32_t*& card_table_mark_array (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->mark_array;
}

#ifdef HOST_64BIT
#define mark_bit_pitch ((size_t)16)
#else
#define mark_bit_pitch ((size_t)8)
#endif // HOST_64BIT
#define mark_word_width ((size_t)32)
#define mark_word_size (mark_word_width * mark_bit_pitch)

inline
uint8_t* align_on_mark_bit (uint8_t* add)
{
    return (uint8_t*)((size_t)(add + (mark_bit_pitch - 1)) & ~(mark_bit_pitch - 1));
}

inline
uint8_t* align_lower_mark_bit (uint8_t* add)
{
    return (uint8_t*)((size_t)(add) & ~(mark_bit_pitch - 1));
}

inline
BOOL is_aligned_on_mark_word (uint8_t* add)
{
    return ((size_t)add == ((size_t)(add) & ~(mark_word_size - 1)));
}

inline
uint8_t* align_on_mark_word (uint8_t* add)
{
    return (uint8_t*)((size_t)(add + mark_word_size - 1) & ~(mark_word_size - 1));
}

inline
uint8_t* align_lower_mark_word (uint8_t* add)
{
    return (uint8_t*)((size_t)(add) & ~(mark_word_size - 1));
}

inline
size_t mark_bit_of (uint8_t* add)
{
    return ((size_t)add / mark_bit_pitch);
}

inline
unsigned int mark_bit_bit (size_t mark_bit)
{
    return (unsigned int)(mark_bit % mark_word_width);
}

inline
size_t mark_bit_word (size_t mark_bit)
{
    return (mark_bit / mark_word_width);
}

inline
size_t mark_word_of (uint8_t* add)
{
    return ((size_t)add) / mark_word_size;
}

uint8_t* mark_word_address (size_t wd)
{
    return (uint8_t*)(wd*mark_word_size);
}

uint8_t* mark_bit_address (size_t mark_bit)
{
    return (uint8_t*)(mark_bit*mark_bit_pitch);
}

inline
size_t mark_bit_bit_of (uint8_t* add)
{
    return  (((size_t)add / mark_bit_pitch) % mark_word_width);
}

size_t size_mark_array_of (uint8_t* from, uint8_t* end)
{
    assert (((size_t)from & ((mark_word_size)-1)) == 0);
    assert (((size_t)end  & ((mark_word_size)-1)) == 0);
    return sizeof (uint32_t)*(((end - from) / mark_word_size));
}

//In order to eliminate the lowest_address in the mark array
//computations (mark_word_of, etc) mark_array is offset
// according to the lowest_address.
uint32_t* translate_mark_array (uint32_t* ma)
{
    return (uint32_t*)((uint8_t*)ma - size_mark_array_of (0, g_gc_lowest_address));
}

#endif //BACKGROUND_GC

//These work on untranslated card tables
inline
uint32_t*& card_table_next (uint32_t* c_table)
{
    // NOTE:  The dac takes a dependency on card_table_info being right before c_table.
    //        It's 100% ok to change this implementation detail as long as a matching change
    //        is made to DacGCBookkeepingEnumerator::Init in daccess.cpp.
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->next_card_table;
}

inline
size_t& card_table_size (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->size;
}

void own_card_table (uint32_t* c_table)
{
    card_table_refcount (c_table) += 1;
}

void destroy_card_table (uint32_t* c_table);

void delete_next_card_table (uint32_t* c_table)
{
    uint32_t* n_table = card_table_next (c_table);
    if (n_table)
    {
        if (card_table_next (n_table))
        {
            delete_next_card_table (n_table);
        }
        if (card_table_refcount (n_table) == 0)
        {
            destroy_card_table (n_table);
            card_table_next (c_table) = 0;
        }
    }
}

void release_card_table (uint32_t* c_table)
{
    assert (card_table_refcount (c_table) >0);
    card_table_refcount (c_table) -= 1;
    if (card_table_refcount (c_table) == 0)
    {
        delete_next_card_table (c_table);
        if (card_table_next (c_table) == 0)
        {
            destroy_card_table (c_table);
            // sever the link from the parent
            if (&g_gc_card_table[card_word (gcard_of(g_gc_lowest_address))] == c_table)
            {
                g_gc_card_table = 0;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                g_gc_card_bundle_table = 0;
#endif
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
                SoftwareWriteWatch::StaticClose();
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
            }
            else
            {
                uint32_t* p_table = &g_gc_card_table[card_word (gcard_of(g_gc_lowest_address))];
                if (p_table)
                {
                    while (p_table && (card_table_next (p_table) != c_table))
                        p_table = card_table_next (p_table);
                    card_table_next (p_table) = 0;
                }
            }
        }
    }
}

void destroy_card_table (uint32_t* c_table)
{
//  delete (uint32_t*)&card_table_refcount(c_table);

    size_t size = card_table_size(c_table);
    gc_heap::destroy_card_table_helper (c_table);
    GCToOSInterface::VirtualRelease (&card_table_refcount(c_table), size);
    dprintf (2, ("Table Virtual Free : %zx", (size_t)&card_table_refcount(c_table)));
}

uint8_t** make_mark_list (size_t size)
{
    uint8_t** mark_list = new (nothrow) uint8_t* [size];
    return mark_list;
}

#define swap(a,b){uint8_t* t; t = a; a = b; b = t;}


#ifndef USE_INTROSORT
void qsort1( uint8_t* *low, uint8_t* *high, unsigned int depth)
{
    if (((low + 16) >= high) || (depth > 100))
    {
        //insertion sort
        uint8_t **i, **j;
        for (i = low+1; i <= high; i++)
        {
            uint8_t* val = *i;
            for (j=i;j >low && val<*(j-1);j--)
            {
                *j=*(j-1);
            }
            *j=val;
        }
    }
    else
    {
        uint8_t *pivot, **left, **right;

        //sort low middle and high
        if (*(low+((high-low)/2)) < *low)
            swap (*(low+((high-low)/2)), *low);
        if (*high < *low)
            swap (*low, *high);
        if (*high < *(low+((high-low)/2)))
            swap (*(low+((high-low)/2)), *high);

        swap (*(low+((high-low)/2)), *(high-1));
        pivot =  *(high-1);
        left = low; right = high-1;
        while (1) {
            while (*(--right) > pivot);
            while (*(++left)  < pivot);
            if (left < right)
            {
                swap(*left, *right);
            }
            else
                break;
        }
        swap (*left, *(high-1));
        qsort1(low, left-1, depth+1);
        qsort1(left+1, high, depth+1);
    }
}
#endif //USE_INTROSORT

#ifdef USE_VXSORT
static void do_vxsort (uint8_t** item_array, ptrdiff_t item_count, uint8_t* range_low, uint8_t* range_high)
{
    // above this threshold, using AVX2 for sorting will likely pay off
    // despite possible downclocking on some devices
    const ptrdiff_t AVX2_THRESHOLD_SIZE = 8 * 1024;

    // above this threshold, using AVX512F for sorting will likely pay off
    // despite possible downclocking on current devices
    const ptrdiff_t AVX512F_THRESHOLD_SIZE = 128 * 1024;

    // above this threshold, using NEON for sorting will likely pay off
    const ptrdiff_t NEON_THRESHOLD_SIZE = 1024;

    if (item_count <= 1)
        return;

#if defined(TARGET_AMD64)
    if (IsSupportedInstructionSet (InstructionSet::AVX2) && (item_count > AVX2_THRESHOLD_SIZE))
    {
        dprintf(3, ("Sorting mark lists"));

        // use AVX512F only if the list is large enough to pay for downclocking impact
        if (IsSupportedInstructionSet (InstructionSet::AVX512F) && (item_count > AVX512F_THRESHOLD_SIZE))
        {
            do_vxsort_avx512 (item_array, &item_array[item_count - 1], range_low, range_high);
        }
        else
        {
            do_vxsort_avx2 (item_array, &item_array[item_count - 1], range_low, range_high);
        }
    }
#elif defined(TARGET_ARM64)
    if (IsSupportedInstructionSet (InstructionSet::NEON) && (item_count > NEON_THRESHOLD_SIZE))
    {
        dprintf(3, ("Sorting mark lists"));
        do_vxsort_neon (item_array, &item_array[item_count - 1], range_low, range_high);
    }
#endif
    else
    {
        dprintf (3, ("Sorting mark lists"));
        introsort::sort (item_array, &item_array[item_count - 1], 0);
    }
#ifdef _DEBUG
    // check the array is sorted
    for (ptrdiff_t i = 0; i < item_count - 1; i++)
    {
        assert (item_array[i] <= item_array[i + 1]);
    }
    // check that the ends of the array are indeed in range
    // together with the above this implies all elements are in range
    assert ((range_low <= item_array[0]) && (item_array[item_count - 1] <= range_high));
#endif
}
#endif //USE_VXSORT

#ifdef MULTIPLE_HEAPS

#ifdef _DEBUG

#if !defined(_MSC_VER)
#if !defined(__cdecl)
#if defined(__i386__)
#define __cdecl __attribute__((cdecl))
#else
#define __cdecl
#endif
#endif
#endif

#endif // _DEBUG

#else

#ifdef USE_REGIONS


#endif //USE_REGIONS
#endif //MULTIPLE_HEAPS

#ifndef USE_REGIONS
class seg_free_spaces
{
    struct seg_free_space
    {
        BOOL is_plug;
        void* start;
    };

    struct free_space_bucket
    {
        seg_free_space* free_space;
        ptrdiff_t count_add; // Assigned when we first construct the array.
        ptrdiff_t count_fit; // How many items left when we are fitting plugs.
    };

    void move_bucket (int old_power2, int new_power2)
    {
        // PREFAST warning 22015: old_power2 could be negative
        assert (old_power2 >= 0);
        assert (old_power2 >= new_power2);

        if (old_power2 == new_power2)
        {
            return;
        }

        seg_free_space* src_index = free_space_buckets[old_power2].free_space;
        for (int i = old_power2; i > new_power2; i--)
        {
            seg_free_space** dest = &(free_space_buckets[i].free_space);
            (*dest)++;

            seg_free_space* dest_index = free_space_buckets[i - 1].free_space;
            if (i > (new_power2 + 1))
            {
                seg_free_space temp = *src_index;
                *src_index = *dest_index;
                *dest_index = temp;
            }
            src_index = dest_index;
        }

        free_space_buckets[old_power2].count_fit--;
        free_space_buckets[new_power2].count_fit++;
    }

#ifdef _DEBUG

    void dump_free_space (seg_free_space* item)
    {
        uint8_t* addr = 0;
        size_t len = 0;

        if (item->is_plug)
        {
            mark* m = (mark*)(item->start);
            len = pinned_len (m);
            addr = pinned_plug (m) - len;
        }
        else
        {
            heap_segment* seg = (heap_segment*)(item->start);
            addr = heap_segment_plan_allocated (seg);
            len = heap_segment_committed (seg) - addr;
        }

        dprintf (SEG_REUSE_LOG_1, ("[%d]0x%p %zd", heap_num, addr, len));
    }

    void dump()
    {
        seg_free_space* item = NULL;
        int i = 0;

        dprintf (SEG_REUSE_LOG_1, ("[%d]----------------------------------\nnow the free spaces look like:", heap_num));
        for (i = 0; i < (free_space_bucket_count - 1); i++)
        {
            dprintf (SEG_REUSE_LOG_1, ("[%d]Free spaces for 2^%d bucket:", heap_num, (base_power2 + i)));
            dprintf (SEG_REUSE_LOG_1, ("[%d]%s %s", heap_num, "start", "len"));
            item = free_space_buckets[i].free_space;
            while (item < free_space_buckets[i + 1].free_space)
            {
                dump_free_space (item);
                item++;
            }
            dprintf (SEG_REUSE_LOG_1, ("[%d]----------------------------------", heap_num));
        }

        dprintf (SEG_REUSE_LOG_1, ("[%d]Free spaces for 2^%d bucket:", heap_num, (base_power2 + i)));
        dprintf (SEG_REUSE_LOG_1, ("[%d]%s %s", heap_num, "start", "len"));
        item = free_space_buckets[i].free_space;

        while (item <= &seg_free_space_array[free_space_item_count - 1])
        {
            dump_free_space (item);
            item++;
        }
        dprintf (SEG_REUSE_LOG_1, ("[%d]----------------------------------", heap_num));
    }

#endif //_DEBUG

    free_space_bucket* free_space_buckets;
    seg_free_space* seg_free_space_array;
    ptrdiff_t free_space_bucket_count;
    ptrdiff_t free_space_item_count;
    int base_power2;
    int heap_num;
#ifdef _DEBUG
    BOOL has_end_of_seg;
#endif //_DEBUG

public:

    seg_free_spaces (int h_number)
    {
        heap_num = h_number;
    }

    BOOL alloc ()
    {
        size_t total_prealloc_size =
            MAX_NUM_BUCKETS * sizeof (free_space_bucket) +
            MAX_NUM_FREE_SPACES * sizeof (seg_free_space);

        free_space_buckets = (free_space_bucket*) new (nothrow) uint8_t[total_prealloc_size];

        return (!!free_space_buckets);
    }

    // We take the ordered free space array we got from the 1st pass,
    // and feed the portion that we decided to use to this method, ie,
    // the largest item_count free spaces.
    void add_buckets (int base, size_t* ordered_free_spaces, int bucket_count, size_t item_count)
    {
        assert (free_space_buckets);
        assert (item_count <= (size_t)MAX_PTR);

        free_space_bucket_count = bucket_count;
        free_space_item_count = item_count;
        base_power2 = base;
#ifdef _DEBUG
        has_end_of_seg = FALSE;
#endif //_DEBUG

        ptrdiff_t total_item_count = 0;
        ptrdiff_t i = 0;

        seg_free_space_array = (seg_free_space*)(free_space_buckets + free_space_bucket_count);

        for (i = 0; i < (ptrdiff_t)item_count; i++)
        {
            seg_free_space_array[i].start = 0;
            seg_free_space_array[i].is_plug = FALSE;
        }

        for (i = 0; i < bucket_count; i++)
        {
            free_space_buckets[i].count_add = ordered_free_spaces[i];
            free_space_buckets[i].count_fit = ordered_free_spaces[i];
            free_space_buckets[i].free_space = &seg_free_space_array[total_item_count];
            total_item_count += free_space_buckets[i].count_add;
        }

        assert (total_item_count == (ptrdiff_t)item_count);
    }

    // If we are adding a free space before a plug we pass the
    // mark stack position so we can update the length; we could
    // also be adding the free space after the last plug in which
    // case start is the segment which we'll need to update the
    // heap_segment_plan_allocated.
    void add (void* start, BOOL plug_p, BOOL first_p)
    {
        size_t size = (plug_p ?
                       pinned_len ((mark*)start) :
                       (heap_segment_committed ((heap_segment*)start) -
                           heap_segment_plan_allocated ((heap_segment*)start)));

        if (plug_p)
        {
            dprintf (SEG_REUSE_LOG_1, ("[%d]Adding a free space before plug: %zd", heap_num, size));
        }
        else
        {
            dprintf (SEG_REUSE_LOG_1, ("[%d]Adding a free space at end of seg: %zd", heap_num, size));
#ifdef _DEBUG
            has_end_of_seg = TRUE;
#endif //_DEBUG
        }

        if (first_p)
        {
            size_t eph_gen_starts = gc_heap::eph_gen_starts_size;
            size -= eph_gen_starts;
            if (plug_p)
            {
                mark* m = (mark*)(start);
                pinned_len (m) -= eph_gen_starts;
            }
            else
            {
                heap_segment* seg = (heap_segment*)start;
                heap_segment_plan_allocated (seg) += eph_gen_starts;
            }
        }

        int bucket_power2 = index_of_highest_set_bit (size);
        if (bucket_power2 < base_power2)
        {
            return;
        }

        free_space_bucket* bucket = &free_space_buckets[bucket_power2 - base_power2];

        seg_free_space* bucket_free_space = bucket->free_space;
        assert (plug_p || (!plug_p && bucket->count_add));

        if (bucket->count_add == 0)
        {
            dprintf (SEG_REUSE_LOG_1, ("[%d]Already have enough of 2^%d", heap_num, bucket_power2));
            return;
        }

        ptrdiff_t index = bucket->count_add - 1;

        dprintf (SEG_REUSE_LOG_1, ("[%d]Building free spaces: adding %p; len: %zd (2^%d)",
                    heap_num,
                    (plug_p ?
                        (pinned_plug ((mark*)start) - pinned_len ((mark*)start)) :
                        heap_segment_plan_allocated ((heap_segment*)start)),
                    size,
                    bucket_power2));

        if (plug_p)
        {
            bucket_free_space[index].is_plug = TRUE;
        }

        bucket_free_space[index].start = start;
        bucket->count_add--;
    }

#ifdef _DEBUG

    // Do a consistency check after all free spaces are added.
    void check()
    {
        ptrdiff_t i = 0;
        int end_of_seg_count = 0;

        for (i = 0; i < free_space_item_count; i++)
        {
            assert (seg_free_space_array[i].start);
            if (!(seg_free_space_array[i].is_plug))
            {
                end_of_seg_count++;
            }
        }

        if (has_end_of_seg)
        {
            assert (end_of_seg_count == 1);
        }
        else
        {
            assert (end_of_seg_count == 0);
        }

        for (i = 0; i < free_space_bucket_count; i++)
        {
            assert (free_space_buckets[i].count_add == 0);
        }
    }

#endif //_DEBUG

    uint8_t* fit (uint8_t* old_loc,
               size_t plug_size
               REQD_ALIGN_AND_OFFSET_DCL)
    {
        if (old_loc)
        {
#ifdef SHORT_PLUGS
            assert (!is_plug_padded (old_loc));
#endif //SHORT_PLUGS
            assert (!node_realigned (old_loc));
        }

        size_t saved_plug_size = plug_size;

#ifdef FEATURE_STRUCTALIGN
        // BARTOKTODO (4841): this code path is disabled (see can_fit_all_blocks_p) until we take alignment requirements into account
        _ASSERTE(requiredAlignment == DATA_ALIGNMENT && false);
#endif // FEATURE_STRUCTALIGN

        size_t plug_size_to_fit = plug_size;

        // best fit is only done for gen1 to gen2 and we do not pad in gen2.
        // however we must account for requirements of large alignment.
        // which may result in realignment padding.
#ifdef RESPECT_LARGE_ALIGNMENT
        plug_size_to_fit += switch_alignment_size(FALSE);
#endif //RESPECT_LARGE_ALIGNMENT

        int plug_power2 = index_of_highest_set_bit (round_up_power2 (plug_size_to_fit + Align(min_obj_size)));
        ptrdiff_t i;
        uint8_t* new_address = 0;

        if (plug_power2 < base_power2)
        {
            plug_power2 = base_power2;
        }

        int chosen_power2 = plug_power2 - base_power2;
retry:
        for (i = chosen_power2; i < free_space_bucket_count; i++)
        {
            if (free_space_buckets[i].count_fit != 0)
            {
                break;
            }
            chosen_power2++;
        }

        dprintf (SEG_REUSE_LOG_1, ("[%d]Fitting plug len %zd (2^%d) using 2^%d free space",
            heap_num,
            plug_size,
            plug_power2,
            (chosen_power2 + base_power2)));

        assert (i < free_space_bucket_count);

        seg_free_space* bucket_free_space = free_space_buckets[chosen_power2].free_space;
        ptrdiff_t free_space_count = free_space_buckets[chosen_power2].count_fit;
        size_t new_free_space_size = 0;
        BOOL can_fit = FALSE;
        size_t pad = 0;

        for (i = 0; i < free_space_count; i++)
        {
            size_t free_space_size = 0;
            pad = 0;

            if (bucket_free_space[i].is_plug)
            {
                mark* m = (mark*)(bucket_free_space[i].start);
                uint8_t* plug_free_space_start = pinned_plug (m) - pinned_len (m);

                if (!((old_loc == 0) || same_large_alignment_p (old_loc, plug_free_space_start)))
                {
                    pad = switch_alignment_size (FALSE);
                }

                plug_size = saved_plug_size + pad;

                free_space_size = pinned_len (m);
                new_address = pinned_plug (m) - pinned_len (m);

                if (free_space_size >= (plug_size + Align (min_obj_size)) ||
                    free_space_size == plug_size)
                {
                    new_free_space_size = free_space_size - plug_size;
                    pinned_len (m) = new_free_space_size;
#ifdef SIMPLE_DPRINTF
                    dprintf (SEG_REUSE_LOG_0, ("[%d]FP: 0x%p->0x%p(%zx)(%zx), [0x%p (2^%d) -> [0x%p (2^%d)",
                                heap_num,
                                old_loc,
                                new_address,
                                (plug_size - pad),
                                pad,
                                pinned_plug (m),
                                index_of_highest_set_bit (free_space_size),
                                (pinned_plug (m) - pinned_len (m)),
                                index_of_highest_set_bit (new_free_space_size)));
#endif //SIMPLE_DPRINTF

                    if (pad != 0)
                    {
                        set_node_realigned (old_loc);
                    }

                    can_fit = TRUE;
                }
            }
            else
            {
                heap_segment* seg = (heap_segment*)(bucket_free_space[i].start);
                free_space_size = heap_segment_committed (seg) - heap_segment_plan_allocated (seg);

                if (!((old_loc == 0) || same_large_alignment_p (old_loc, heap_segment_plan_allocated (seg))))
                {
                    pad = switch_alignment_size (FALSE);
                }

                plug_size = saved_plug_size + pad;

                if (free_space_size >= (plug_size + Align (min_obj_size)) ||
                    free_space_size == plug_size)
                {
                    new_address = heap_segment_plan_allocated (seg);
                    new_free_space_size = free_space_size - plug_size;
                    heap_segment_plan_allocated (seg) = new_address + plug_size;
#ifdef SIMPLE_DPRINTF
                    dprintf (SEG_REUSE_LOG_0, ("[%d]FS: 0x%p-> 0x%p(%zd) (2^%d) -> 0x%p (2^%d)",
                                heap_num,
                                old_loc,
                                new_address,
                                (plug_size - pad),
                                index_of_highest_set_bit (free_space_size),
                                heap_segment_plan_allocated (seg),
                                index_of_highest_set_bit (new_free_space_size)));
#endif //SIMPLE_DPRINTF

                    if (pad != 0)
                        set_node_realigned (old_loc);

                    can_fit = TRUE;
                }
            }

            if (can_fit)
            {
                break;
            }
        }

        if (!can_fit)
        {
            assert (chosen_power2 == 0);
            chosen_power2 = 1;
            goto retry;
        }

        new_address += pad;
        assert ((chosen_power2 && (i == 0)) ||
                ((!chosen_power2) && (i < free_space_count)));

        int new_bucket_power2 = index_of_highest_set_bit (new_free_space_size);

        if (new_bucket_power2 < base_power2)
        {
            new_bucket_power2 = base_power2;
        }

        move_bucket (chosen_power2, new_bucket_power2 - base_power2);

        //dump();

        return new_address;
    }

    void cleanup ()
    {
        if (free_space_buckets)
        {
            delete [] free_space_buckets;
        }
        if (seg_free_space_array)
        {
            delete [] seg_free_space_array;
        }
    }
};
#endif //!USE_REGIONS

#define marked(i) header(i)->IsMarked()
#define set_marked(i) header(i)->SetMarked()
#define clear_marked(i) header(i)->ClearMarked()
#define pinned(i) header(i)->IsPinned()
#define set_pinned(i) header(i)->SetPinned()
#define clear_pinned(i) header(i)->GetHeader()->ClrGCBit();

inline size_t my_get_size (Object* ob)
{
    MethodTable* mT = header(ob)->GetMethodTable();

    return (mT->GetBaseSize() +
            (mT->HasComponentSize() ?
             ((size_t)((CObjectHeader*)ob)->GetNumComponents() * mT->RawGetComponentSize()) : 0));
}

#define size(i) my_get_size (header(i))

#define contain_pointers(i) header(i)->ContainsGCPointers()
#ifdef COLLECTIBLE_CLASS
#define contain_pointers_or_collectible(i) header(i)->ContainsGCPointersOrCollectible()

#define get_class_object(i) GCToEEInterface::GetLoaderAllocatorObjectForGC((Object *)i)
#define is_collectible(i) method_table(i)->Collectible()
#else //COLLECTIBLE_CLASS
#define contain_pointers_or_collectible(i) header(i)->ContainsGCPointers()
#endif //COLLECTIBLE_CLASS

#ifdef USE_REGIONS


static GCSpinLock write_barrier_spin_lock;

#endif //USE_REGIONS

#ifdef WRITE_WATCH
uint8_t* g_addresses [array_size+2]; // to get around the bug in GetWriteWatch

#ifdef BACKGROUND_GC
const size_t ww_reset_quantum = 128*1024*1024;

#endif //BACKGROUND_GC
#endif //WRITE_WATCH

#ifdef BACKGROUND_GC
void gc_heap::restart_vm()
{
    //assert (generation_allocation_pointer (youngest_generation) == 0);
    dprintf (3, ("Restarting EE"));
    STRESS_LOG0(LF_GC, LL_INFO10000, "Concurrent GC: Restarting EE\n");
    ee_proceed_event.Set();
}


#endif //BACKGROUND_GC


void
gc_heap::suspend_EE ()
{
    dprintf (2, ("suspend_EE"));
    GCToEEInterface::SuspendEE (SUSPEND_FOR_GC_PREP);
}

void
gc_heap::restart_EE ()
{
    dprintf (2, ("restart_EE"));
    GCToEEInterface::RestartEE (FALSE);
}

//Initializes PER_HEAP_ISOLATED data members.
int
gc_heap::init_semi_shared()
{
    int ret = 0;

#ifdef BGC_SERVO_TUNING
    uint32_t current_memory_load = 0;
    uint32_t sweep_flr_goal = 0;
    uint32_t sweep_flr_goal_loh = 0;
#endif //BGC_SERVO_TUNING

#ifndef USE_REGIONS
    // This is used for heap expansion - it's to fix exactly the start for gen 0
    // through (max_generation-1). When we expand the heap we allocate all these
    // gen starts at the beginning of the new ephemeral seg.
    eph_gen_starts_size = (Align (min_obj_size)) * max_generation;
#endif //!USE_REGIONS

#ifdef MULTIPLE_HEAPS
    mark_list_size = min ((size_t)100*1024, max ((size_t)8192, soh_segment_size/(2*10*32)));
#ifdef DYNAMIC_HEAP_COUNT
    if (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
    {
        // we'll actually start with one heap in this case
        g_mark_list_total_size = mark_list_size;
    }
    else
#endif //DYNAMIC_HEAP_COUNT
    {
        g_mark_list_total_size = mark_list_size*n_heaps;
    }
    g_mark_list = make_mark_list (g_mark_list_total_size);

    min_balance_threshold = alloc_quantum_balance_units * CLR_SIZE * 2;
    g_mark_list_copy = make_mark_list (g_mark_list_total_size);
    if (!g_mark_list_copy)
    {
        goto cleanup;
    }
#else //MULTIPLE_HEAPS

    mark_list_size = min((size_t)100*1024, max ((size_t)8192, soh_segment_size/(64*32)));
    g_mark_list_total_size = mark_list_size;
    g_mark_list = make_mark_list (mark_list_size);

#endif //MULTIPLE_HEAPS

    dprintf (3, ("mark_list_size: %zd", mark_list_size));

    if (!g_mark_list)
    {
        goto cleanup;
    }

#ifdef MULTIPLE_HEAPS
    // gradual decommit: set size to some reasonable value per time interval
    max_decommit_step_size = ((DECOMMIT_SIZE_PER_MILLISECOND * DECOMMIT_TIME_STEP_MILLISECONDS) / n_heaps);

    // but do at least MIN_DECOMMIT_SIZE per step to make the OS call worthwhile
    max_decommit_step_size = max (max_decommit_step_size, MIN_DECOMMIT_SIZE);
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_BASICFREEZE
    seg_table = sorted_table::make_sorted_table();

    if (!seg_table)
        goto cleanup;
#endif //FEATURE_BASICFREEZE

#ifndef USE_REGIONS
    segment_standby_list = 0;
#endif //USE_REGIONS

    if (!full_gc_approach_event.CreateManualEventNoThrow(FALSE))
    {
        goto cleanup;
    }
    if (!full_gc_end_event.CreateManualEventNoThrow(FALSE))
    {
        goto cleanup;
    }

    fgn_loh_percent = 0;
    full_gc_approach_event_set = false;

    memset (full_gc_counts, 0, sizeof (full_gc_counts));

#ifndef USE_REGIONS
    should_expand_in_full_gc = FALSE;
#endif //!USE_REGIONS


#ifdef FEATURE_LOH_COMPACTION
    loh_compaction_always_p = GCConfig::GetLOHCompactionMode() != 0;
    loh_compaction_mode = loh_compaction_default;
#endif //FEATURE_LOH_COMPACTION

#ifdef BGC_SERVO_TUNING
    memset (bgc_tuning::gen_calc, 0, sizeof (bgc_tuning::gen_calc));
    memset (bgc_tuning::gen_stats, 0, sizeof (bgc_tuning::gen_stats));
    memset (bgc_tuning::current_bgc_end_data, 0, sizeof (bgc_tuning::current_bgc_end_data));

    // for the outer loop - the ML (memory load) loop
    bgc_tuning::enable_fl_tuning = (GCConfig::GetBGCFLTuningEnabled() != 0);
    bgc_tuning::memory_load_goal = (uint32_t)GCConfig::GetBGCMemGoal();
    bgc_tuning::memory_load_goal_slack = (uint32_t)GCConfig::GetBGCMemGoalSlack();
    bgc_tuning::ml_kp = (double)GCConfig::GetBGCMLkp() / 1000.0;
    bgc_tuning::ml_ki = (double)GCConfig::GetBGCMLki() / 1000.0;
    bgc_tuning::ratio_correction_step = (double)GCConfig::GetBGCG2RatioStep() / 100.0;

    // for the inner loop - the alloc loop which calculates the allocated bytes in gen2 before
    // triggering the next BGC.
    bgc_tuning::above_goal_kp = (double)GCConfig::GetBGCFLkp() / 1000000.0;
    bgc_tuning::enable_ki = (GCConfig::GetBGCFLEnableKi() != 0);
    bgc_tuning::above_goal_ki = (double)GCConfig::GetBGCFLki() / 1000000.0;
    bgc_tuning::enable_kd = (GCConfig::GetBGCFLEnableKd() != 0);
    bgc_tuning::above_goal_kd = (double)GCConfig::GetBGCFLkd() / 100.0;
    bgc_tuning::enable_smooth = (GCConfig::GetBGCFLEnableSmooth() != 0);
    bgc_tuning::num_gen1s_smooth_factor = (double)GCConfig::GetBGCFLSmoothFactor() / 100.0;
    bgc_tuning::enable_tbh = (GCConfig::GetBGCFLEnableTBH() != 0);
    bgc_tuning::enable_ff = (GCConfig::GetBGCFLEnableFF() != 0);
    bgc_tuning::above_goal_ff = (double)GCConfig::GetBGCFLff() / 100.0;
    bgc_tuning::enable_gradual_d = (GCConfig::GetBGCFLGradualD() != 0);
    sweep_flr_goal = (uint32_t)GCConfig::GetBGCFLSweepGoal();
    sweep_flr_goal_loh = (uint32_t)GCConfig::GetBGCFLSweepGoalLOH();

    bgc_tuning::gen_calc[0].sweep_flr_goal = ((sweep_flr_goal == 0) ? 20.0 : (double)sweep_flr_goal);
    bgc_tuning::gen_calc[1].sweep_flr_goal = ((sweep_flr_goal_loh == 0) ? 20.0 : (double)sweep_flr_goal_loh);

    bgc_tuning::available_memory_goal = (uint64_t)((double)gc_heap::total_physical_mem * (double)(100 - bgc_tuning::memory_load_goal) / 100);
    get_memory_info (&current_memory_load);

    dprintf (BGC_TUNING_LOG, ("BTL tuning %s!!!",
        (bgc_tuning::enable_fl_tuning ? "enabled" : "disabled")));

#ifdef SIMPLE_DPRINTF
    dprintf (BGC_TUNING_LOG, ("BTL tuning parameters: mem goal: %d%%(%zd), +/-%d%%, gen2 correction factor: %.2f, sweep flr goal: %d%%, smooth factor: %.3f(%s), TBH: %s, FF: %.3f(%s), ml: kp %.5f, ki %.10f",
        bgc_tuning::memory_load_goal,
        bgc_tuning::available_memory_goal,
        bgc_tuning::memory_load_goal_slack,
        bgc_tuning::ratio_correction_step,
        (int)bgc_tuning::gen_calc[0].sweep_flr_goal,
        bgc_tuning::num_gen1s_smooth_factor,
        (bgc_tuning::enable_smooth ? "enabled" : "disabled"),
        (bgc_tuning::enable_tbh ? "enabled" : "disabled"),
        bgc_tuning::above_goal_ff,
        (bgc_tuning::enable_ff ? "enabled" : "disabled"),
        bgc_tuning::ml_kp,
        bgc_tuning::ml_ki));

    dprintf (BGC_TUNING_LOG, ("BTL tuning parameters: kp: %.5f, ki: %.5f (%s), kd: %.3f (kd-%s, gd-%s), ff: %.3f",
        bgc_tuning::above_goal_kp,
        bgc_tuning::above_goal_ki,
        (bgc_tuning::enable_ki ? "enabled" : "disabled"),
        bgc_tuning::above_goal_kd,
        (bgc_tuning::enable_kd ? "enabled" : "disabled"),
        (bgc_tuning::enable_gradual_d ? "enabled" : "disabled"),
        bgc_tuning::above_goal_ff));
#endif //SIMPLE_DPRINTF

    if (bgc_tuning::enable_fl_tuning && (current_memory_load < bgc_tuning::memory_load_goal))
    {
        uint32_t distance_to_goal = bgc_tuning::memory_load_goal - current_memory_load;
        bgc_tuning::stepping_interval = max (distance_to_goal / 10, 1u);
        bgc_tuning::last_stepping_mem_load = current_memory_load;
        bgc_tuning::last_stepping_bgc_count = 0;
        dprintf (BGC_TUNING_LOG, ("current ml: %d, %d to goal, interval: %d",
            current_memory_load, distance_to_goal, bgc_tuning::stepping_interval));
    }
    else
    {
        dprintf (BGC_TUNING_LOG, ("current ml: %d, >= goal: %d, disable stepping",
            current_memory_load, bgc_tuning::memory_load_goal));
        bgc_tuning::use_stepping_trigger_p = false;
    }
#endif //BGC_SERVO_TUNING

#ifdef BACKGROUND_GC
    memset (ephemeral_fgc_counts, 0, sizeof (ephemeral_fgc_counts));
    bgc_alloc_spin_count = static_cast<uint32_t>(GCConfig::GetBGCSpinCount());
    bgc_alloc_spin = static_cast<uint32_t>(GCConfig::GetBGCSpin());

    {
        int number_bgc_threads = get_num_heaps();
        if (!create_bgc_threads_support (number_bgc_threads))
        {
            goto cleanup;
        }
    }
#endif //BACKGROUND_GC

    memset (&current_no_gc_region_info, 0, sizeof (current_no_gc_region_info));

#ifdef GC_CONFIG_DRIVEN
    compact_or_sweep_gcs[0] = 0;
    compact_or_sweep_gcs[1] = 0;
#endif //GC_CONFIG_DRIVEN

#if defined(SHORT_PLUGS) && !defined(USE_REGIONS)
    short_plugs_pad_ratio = (double)DESIRED_PLUG_LENGTH / (double)(DESIRED_PLUG_LENGTH - Align (min_obj_size));
#endif //SHORT_PLUGS && !USE_REGIONS

    generation_skip_ratio_threshold = (int)GCConfig::GetGCLowSkipRatio();

#ifdef FEATURE_EVENT_TRACE
    gc_time_info = new (nothrow) uint64_t[max_compact_time_type];
    if (!gc_time_info)
    {
        goto cleanup;
    }
#ifdef BACKGROUND_GC
    bgc_time_info = new (nothrow) uint64_t[max_bgc_time_type];
    if (!bgc_time_info)
    {
        goto cleanup;
    }
#endif //BACKGROUND_GC

#ifdef FEATURE_LOH_COMPACTION
    loh_compact_info = new (nothrow) etw_loh_compact_info [get_num_heaps()];
    if (!loh_compact_info)
    {
        goto cleanup;
    }
#endif //FEATURE_LOH_COMPACTION
#endif //FEATURE_EVENT_TRACE

    reset_mm_p = TRUE;

    ret = 1;

cleanup:

    if (!ret)
    {
        if (full_gc_approach_event.IsValid())
        {
            full_gc_approach_event.CloseEvent();
        }
        if (full_gc_end_event.IsValid())
        {
            full_gc_end_event.CloseEvent();
        }
    }

    return ret;
}

uint32_t
gc_heap::wait_for_gc_done(int32_t timeOut)
{
    bool cooperative_mode = enable_preemptive ();

    uint32_t dwWaitResult = NOERROR;

    gc_heap* wait_heap = NULL;
    while (gc_heap::gc_started)
    {
#ifdef MULTIPLE_HEAPS
        wait_heap = g_heaps[heap_select::select_heap(NULL)];
        dprintf(2, ("waiting for the gc_done_event on heap %d", wait_heap->heap_number));
#endif // MULTIPLE_HEAPS

        dwWaitResult = wait_heap->gc_done_event.Wait(timeOut, FALSE);
    }
    disable_preemptive (cooperative_mode);

    return dwWaitResult;
}

void
gc_heap::set_gc_done()
{
    enter_gc_done_event_lock();
    if (!gc_done_event_set)
    {
        gc_done_event_set = true;
        dprintf (2, ("heap %d: setting gc_done_event", heap_number));
        gc_done_event.Set();
    }
    exit_gc_done_event_lock();
}

void
gc_heap::reset_gc_done()
{
    enter_gc_done_event_lock();
    if (gc_done_event_set)
    {
        gc_done_event_set = false;
        dprintf (2, ("heap %d: resetting gc_done_event", heap_number));
        gc_done_event.Reset();
    }
    exit_gc_done_event_lock();
}

void
gc_heap::enter_gc_done_event_lock()
{
    uint32_t dwSwitchCount = 0;
retry:

    if (Interlocked::CompareExchange(&gc_done_event_lock, 0, -1) >= 0)
    {
        while (gc_done_event_lock >= 0)
        {
            if  (g_num_processors > 1)
            {
                int spin_count = yp_spin_count_unit;
                for (int j = 0; j < spin_count; j++)
                {
                    if  (gc_done_event_lock < 0)
                        break;
                    YieldProcessor();           // indicate to the processor that we are spinning
                }
                if  (gc_done_event_lock >= 0)
                    GCToOSInterface::YieldThread(++dwSwitchCount);
            }
            else
                GCToOSInterface::YieldThread(++dwSwitchCount);
        }
        goto retry;
    }
}

void
gc_heap::exit_gc_done_event_lock()
{
    gc_done_event_lock = -1;
}

#ifndef MULTIPLE_HEAPS

#ifdef RECORD_LOH_STATE
int gc_heap::loh_state_index = 0;
gc_heap::loh_state_info gc_heap::last_loh_states[max_saved_loh_states];
#endif //RECORD_LOH_STATE

VOLATILE(int32_t) gc_heap::gc_done_event_lock;
VOLATILE(bool) gc_heap::gc_done_event_set;
GCEvent gc_heap::gc_done_event;
#endif //!MULTIPLE_HEAPS
VOLATILE(bool) gc_heap::internal_gc_done;

int
gc_heap::init_gc_heap (int h_number)
{
#ifdef MULTIPLE_HEAPS
#ifdef _DEBUG
    memset (committed_by_oh_per_heap, 0, sizeof (committed_by_oh_per_heap));
#endif //_DEBUG

    g_heaps [h_number] = this;

    time_bgc_last = 0;

#ifdef SPINLOCK_HISTORY
    spinlock_info_index = 0;
    memset (last_spinlock_info, 0, sizeof(last_spinlock_info));
#endif //SPINLOCK_HISTORY

    // initialize per heap members.
#ifndef USE_REGIONS
    ephemeral_low = (uint8_t*)1;

    ephemeral_high = MAX_PTR;
#endif //!USE_REGIONS

    gc_low = 0;

    gc_high = 0;

    ephemeral_heap_segment = 0;

    oomhist_index_per_heap = 0;

    freeable_uoh_segment = 0;

    condemned_generation_num = 0;

    blocking_collection = FALSE;

    generation_skip_ratio = 100;

#ifdef FEATURE_CARD_MARKING_STEALING
    n_eph_soh = 0;
    n_gen_soh = 0;
    n_eph_loh = 0;
    n_gen_loh = 0;
#endif //FEATURE_CARD_MARKING_STEALING
    mark_stack_tos = 0;

    mark_stack_bos = 0;

    mark_stack_array_length = 0;

    mark_stack_array = 0;

#if defined (_DEBUG) && defined (VERIFY_HEAP)
    verify_pinned_queue_p = FALSE;
#endif // _DEBUG && VERIFY_HEAP

#ifdef FEATURE_LOH_COMPACTION
    loh_pinned_queue_tos = 0;

    loh_pinned_queue_bos = 0;

    loh_pinned_queue_length = 0;

    loh_pinned_queue_decay = LOH_PIN_DECAY;

    loh_pinned_queue = 0;
#endif //FEATURE_LOH_COMPACTION

    min_overflow_address = MAX_PTR;

    max_overflow_address = 0;

    gen0_bricks_cleared = FALSE;

    gen0_must_clear_bricks = 0;

    allocation_quantum = CLR_SIZE;

    more_space_lock_soh = gc_lock;

    more_space_lock_uoh = gc_lock;

    loh_alloc_since_cg = 0;

#ifndef USE_REGIONS
    new_heap_segment = NULL;

    ro_segments_in_range = FALSE;
#endif //!USE_REGIONS

    gen0_allocated_after_gc_p = false;

#ifdef RECORD_LOH_STATE
    loh_state_index = 0;
#endif //RECORD_LOH_STATE

#ifdef USE_REGIONS
    new_gen0_regions_in_plns = 0;
    new_regions_in_prr = 0;
    new_regions_in_threading = 0;

    special_sweep_p = false;
#endif //USE_REGIONS

#endif //MULTIPLE_HEAPS

#ifdef MULTIPLE_HEAPS
    if (h_number > n_heaps)
    {
        assert (!"Number of heaps exceeded");
        return 0;
    }

    heap_number = h_number;
#endif //MULTIPLE_HEAPS

    memset (etw_allocation_running_amount, 0, sizeof (etw_allocation_running_amount));
    memset (allocated_since_last_gc, 0, sizeof (allocated_since_last_gc));
    memset (&oom_info, 0, sizeof (oom_info));
    memset (&fgm_result, 0, sizeof (fgm_result));
    memset (oomhist_per_heap, 0, sizeof (oomhist_per_heap));
    if (!gc_done_event.CreateManualEventNoThrow(FALSE))
    {
        return 0;
    }
    gc_done_event_lock = -1;
    gc_done_event_set = false;

#ifdef DYNAMIC_HEAP_COUNT
    hchist_index_per_heap = 0;
    memset (hchist_per_heap, 0, sizeof (hchist_per_heap));

#ifdef BACKGROUND_GC
    bgc_hchist_index_per_heap = 0;
    memset (bgc_hchist_per_heap, 0, sizeof (bgc_hchist_per_heap));
#endif //BACKGROUND_GC

    if (h_number != 0)
    {
        if (!gc_idle_thread_event.CreateAutoEventNoThrow (FALSE))
        {
            return 0;
        }

#ifdef BACKGROUND_GC
        if (!bgc_idle_thread_event.CreateAutoEventNoThrow (FALSE))
        {
            return 0;
        }
#endif //BACKGROUND_GC

        dprintf (9999, ("creating idle events for h%d", h_number));
    }
#endif //DYNAMIC_HEAP_COUNT

    if (!init_dynamic_data())
    {
        return 0;
    }

    uint32_t* ct = &g_gc_card_table [card_word (card_of (g_gc_lowest_address))];
    own_card_table (ct);
    card_table = translate_card_table (ct);

    brick_table = card_table_brick_table (ct);
    highest_address = card_table_highest_address (ct);
    lowest_address = card_table_lowest_address (ct);

#ifdef CARD_BUNDLE
    card_bundle_table = translate_card_bundle_table (card_table_card_bundle_table (ct), g_gc_lowest_address);
    assert (&card_bundle_table [card_bundle_word (cardw_card_bundle (card_word (card_of (g_gc_lowest_address))))] ==
            card_table_card_bundle_table (ct));
#endif //CARD_BUNDLE

#ifdef BACKGROUND_GC
    background_saved_highest_address = nullptr;
    background_saved_lowest_address = nullptr;
    if (gc_can_use_concurrent)
        mark_array = translate_mark_array (card_table_mark_array (&g_gc_card_table[card_word (card_of (g_gc_lowest_address))]));
    else
        mark_array = NULL;
#endif //BACKGROUND_GC

#ifdef USE_REGIONS
#ifdef STRESS_REGIONS
    // Handle table APIs expect coop so we temporarily switch to coop.
    disable_preemptive (true);
    pinning_handles_for_alloc = new (nothrow) (OBJECTHANDLE[PINNING_HANDLE_INITIAL_LENGTH]);

    for (int i = 0; i < PINNING_HANDLE_INITIAL_LENGTH; i++)
    {
        pinning_handles_for_alloc[i] = g_gcGlobalHandleStore->CreateHandleOfType (0, HNDTYPE_PINNED);
    }
    enable_preemptive();
    ph_index_per_heap = 0;
    pinning_seg_interval = 2;
    num_gen0_regions = 0;
    sip_seg_interval = 2;
    sip_seg_maxgen_interval = 3;
    num_condemned_regions = 0;
#endif //STRESS_REGIONS
    end_gen0_region_space = 0;
    end_gen0_region_committed_space = 0;
    gen0_pinned_free_space = 0;
    gen0_large_chunk_found = false;
    // REGIONS PERF TODO: we should really allocate the POH regions together just so that
    // they wouldn't prevent us from coalescing free regions to form a large virtual address
    // range.
    if (!initial_make_soh_regions (__this) ||
        !initial_make_uoh_regions (loh_generation, __this) ||
        !initial_make_uoh_regions (poh_generation, __this))
    {
        return 0;
    }

#else //USE_REGIONS

    heap_segment* seg = make_initial_segment (soh_gen0, h_number, __this);
    if (!seg)
        return 0;

    FIRE_EVENT(GCCreateSegment_V1, heap_segment_mem(seg),
                              (size_t)(heap_segment_reserved (seg) - heap_segment_mem(seg)),
                              gc_etw_segment_small_object_heap);

    seg_mapping_table_add_segment (seg, __this);
#ifdef MULTIPLE_HEAPS
    assert (heap_segment_heap (seg) == __this);
#endif //MULTIPLE_HEAPS

    uint8_t*  start = heap_segment_mem (seg);

    for (int i = max_generation; i >= 0; i--)
    {
        make_generation (i, seg, start);
        start += Align (min_obj_size);
    }

    heap_segment_allocated (seg) = start;
    alloc_allocated = start;
    heap_segment_used (seg) = start - plug_skew;
    ephemeral_heap_segment = seg;

    // Create segments for the large and pinned generations
    heap_segment* lseg = make_initial_segment(loh_generation, h_number, __this);
    if (!lseg)
        return 0;

    lseg->flags |= heap_segment_flags_loh;

    FIRE_EVENT(GCCreateSegment_V1, heap_segment_mem(lseg),
                              (size_t)(heap_segment_reserved (lseg) - heap_segment_mem(lseg)),
                              gc_etw_segment_large_object_heap);

    heap_segment* pseg = make_initial_segment (poh_generation, h_number, __this);
    if (!pseg)
        return 0;

    pseg->flags |= heap_segment_flags_poh;

    FIRE_EVENT(GCCreateSegment_V1, heap_segment_mem(pseg),
                              (size_t)(heap_segment_reserved (pseg) - heap_segment_mem(pseg)),
                              gc_etw_segment_pinned_object_heap);

    seg_mapping_table_add_segment (lseg, __this);
    seg_mapping_table_add_segment (pseg, __this);

    make_generation (loh_generation, lseg, heap_segment_mem (lseg));
    make_generation (poh_generation, pseg, heap_segment_mem (pseg));

    heap_segment_allocated (lseg) = heap_segment_mem (lseg) + Align (min_obj_size, get_alignment_constant (FALSE));
    heap_segment_used (lseg) = heap_segment_allocated (lseg) - plug_skew;

    heap_segment_allocated (pseg) = heap_segment_mem (pseg) + Align (min_obj_size, get_alignment_constant (FALSE));
    heap_segment_used (pseg) = heap_segment_allocated (pseg) - plug_skew;

    for (int gen_num = 0; gen_num < total_generation_count; gen_num++)
    {
        generation*  gen = generation_of (gen_num);
        make_unused_array (generation_allocation_start (gen), Align (min_obj_size));
    }

#ifdef MULTIPLE_HEAPS
    assert (heap_segment_heap (lseg) == __this);
    assert (heap_segment_heap (pseg) == __this);
#endif //MULTIPLE_HEAPS
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
    //initialize the alloc context heap
    generation_alloc_context (generation_of (soh_gen0))->set_alloc_heap(vm_heap);
    generation_alloc_context (generation_of (loh_generation))->set_alloc_heap(vm_heap);
    generation_alloc_context (generation_of (poh_generation))->set_alloc_heap(vm_heap);

#endif //MULTIPLE_HEAPS

    generation_of (max_generation)->free_list_allocator = allocator(NUM_GEN2_ALIST, BASE_GEN2_ALIST_BITS, gen2_alloc_list, max_generation);
    generation_of (loh_generation)->free_list_allocator = allocator(NUM_LOH_ALIST, BASE_LOH_ALIST_BITS, loh_alloc_list);
    generation_of (poh_generation)->free_list_allocator = allocator(NUM_POH_ALIST, BASE_POH_ALIST_BITS, poh_alloc_list);

    total_alloc_bytes_soh = 0;
    total_alloc_bytes_uoh = 0;

    //needs to be done after the dynamic data has been initialized
#ifdef MULTIPLE_HEAPS
#ifdef STRESS_DYNAMIC_HEAP_COUNT
    uoh_msl_before_gc_p = false;
#endif //STRESS_DYNAMIC_HEAP_COUNT
#else //MULTIPLE_HEAPS
    allocation_running_amount = dd_min_size (dynamic_data_of (0));
#endif //!MULTIPLE_HEAPS

    fgn_maxgen_percent = 0;
    fgn_last_alloc = dd_min_size (dynamic_data_of (0));

    mark* arr = new (nothrow) (mark [MARK_STACK_INITIAL_LENGTH]);
    if (!arr)
        return 0;

    make_mark_stack(arr);

#ifdef BACKGROUND_GC
    for (int i = uoh_start_generation; i < total_generation_count; i++)
    {
        uoh_a_no_bgc[i - uoh_start_generation] = 0;
        uoh_a_bgc_marking[i - uoh_start_generation] = 0;
        uoh_a_bgc_planning[i - uoh_start_generation] = 0;
    }
#ifdef BGC_SERVO_TUNING
    bgc_maxgen_end_fl_size = 0;
#endif //BGC_SERVO_TUNING
    freeable_soh_segment = 0;
    gchist_index_per_heap = 0;
    if (gc_can_use_concurrent)
    {
        uint8_t** b_arr = new (nothrow) (uint8_t * [MARK_STACK_INITIAL_LENGTH]);
        if (!b_arr)
            return 0;

        make_background_mark_stack(b_arr);
    }
#endif //BACKGROUND_GC

#ifndef USE_REGIONS
    ephemeral_low = generation_allocation_start(generation_of(max_generation - 1));
    ephemeral_high = heap_segment_reserved(ephemeral_heap_segment);
#endif //!USE_REGIONS

    if (heap_number == 0)
    {
        stomp_write_barrier_initialize(
#if defined(USE_REGIONS)
            ephemeral_low, ephemeral_high,
            map_region_to_generation_skewed, (uint8_t)min_segment_size_shr
#elif defined(MULTIPLE_HEAPS)
            reinterpret_cast<uint8_t*>(1), reinterpret_cast<uint8_t*>(~0)
#else
            ephemeral_low, ephemeral_high
#endif //MULTIPLE_HEAPS || USE_REGIONS
        );
    }

#ifdef MULTIPLE_HEAPS
    if (!create_gc_thread ())
        return 0;

#endif //MULTIPLE_HEAPS

#ifdef FEATURE_PREMORTEM_FINALIZATION
    HRESULT hr = AllocateCFinalize(&finalize_queue);
    if (FAILED(hr))
        return 0;
#endif // FEATURE_PREMORTEM_FINALIZATION

#ifdef USE_REGIONS
#ifdef MULTIPLE_HEAPS
    min_fl_list = 0;
    num_fl_items_rethreaded_stage2 = 0;
    free_list_space_per_heap = nullptr;
#endif //MULTIPLE_HEAPS
#else //USE_REGIONS
    max_free_space_items = MAX_NUM_FREE_SPACES;

    bestfit_seg = new (nothrow) seg_free_spaces (heap_number);

    if (!bestfit_seg)
    {
        return 0;
    }

    if (!bestfit_seg->alloc())
    {
        return 0;
    }
#endif //USE_REGIONS

    last_gc_before_oom = FALSE;

    sufficient_gen0_space_p = FALSE;

#ifdef MULTIPLE_HEAPS

#ifdef HEAP_ANALYZE

    heap_analyze_success = TRUE;

    internal_root_array  = 0;

    internal_root_array_index = 0;

    internal_root_array_length = initial_internal_roots;

    current_obj          = 0;

    current_obj_size     = 0;

#endif //HEAP_ANALYZE

#endif // MULTIPLE_HEAPS

#ifdef BACKGROUND_GC
    bgc_thread_id.Clear();

    if (!create_bgc_thread_support())
    {
        return 0;
    }

    bgc_alloc_lock = new (nothrow) exclusive_sync;
    if (!bgc_alloc_lock)
    {
        return 0;
    }

    bgc_alloc_lock->init();
    bgc_thread_running = 0;
    bgc_thread = 0;
    bgc_threads_timeout_cs.Initialize();
    current_bgc_state = bgc_not_in_process;
    background_soh_alloc_count = 0;
    bgc_overflow_count = 0;
    for (int i = uoh_start_generation; i < total_generation_count; i++)
    {
        end_uoh_size[i - uoh_start_generation] = dd_min_size (dynamic_data_of (i));
    }

    current_sweep_pos = 0;
#ifdef DOUBLY_LINKED_FL
    current_sweep_seg = 0;
#endif //DOUBLY_LINKED_FL

#endif //BACKGROUND_GC

#ifdef GC_CONFIG_DRIVEN
    memset(interesting_data_per_heap, 0, sizeof (interesting_data_per_heap));
    memset(compact_reasons_per_heap, 0, sizeof (compact_reasons_per_heap));
    memset(expand_mechanisms_per_heap, 0, sizeof (expand_mechanisms_per_heap));
    memset(interesting_mechanism_bits_per_heap, 0, sizeof (interesting_mechanism_bits_per_heap));
#endif //GC_CONFIG_DRIVEN

    return 1;
}

void
gc_heap::destroy_semi_shared()
{
//TODO: will need to move this to per heap
//#ifdef BACKGROUND_GC
//    if (c_mark_list)
//        delete c_mark_list;
//#endif //BACKGROUND_GC

    if (g_mark_list)
        delete[] g_mark_list;

#ifdef FEATURE_BASICFREEZE
    //destroy the segment map
    seg_table->delete_sorted_table();
    delete[] (char*)seg_table;
#endif //FEATURE_BASICFREEZE
}

void
gc_heap::self_destroy()
{
#ifdef BACKGROUND_GC
    kill_gc_thread();
#endif //BACKGROUND_GC

    if (gc_done_event.IsValid())
    {
        gc_done_event.CloseEvent();
    }

    // destroy every segment
    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        heap_segment* seg = heap_segment_rw (generation_start_segment (generation_of (i)));
        _ASSERTE(seg != NULL);

        while (seg)
        {
            heap_segment* next_seg = heap_segment_next_rw (seg);
            delete_heap_segment (seg);
            seg = next_seg;
        }
    }

    // get rid of the card table
    release_card_table (card_table);

    // destroy the mark stack
    delete[] mark_stack_array;

#ifdef FEATURE_PREMORTEM_FINALIZATION
    if (finalize_queue)
        delete finalize_queue;
#endif // FEATURE_PREMORTEM_FINALIZATION
}

void
gc_heap::destroy_gc_heap(gc_heap* heap)
{
    heap->self_destroy();
    delete heap;
}

enum {
CORINFO_EXCEPTION_GC = 0xE0004743 // 'GC'
};


#define mark_stack_empty_p() (mark_stack_base == mark_stack_tos)

#ifdef USE_REGIONS
#ifdef DYNAMIC_HEAP_COUNT

// check that the fields of a decommissioned heap have their expected values,
// i.e. were not inadvertently modified
#define DECOMMISSIONED_VALUE 0xdec0dec0dec0dec0
static const size_t DECOMMISSIONED_SIZE_T = DECOMMISSIONED_VALUE;
static const ptrdiff_t DECOMMISSIONED_PTRDIFF_T = (ptrdiff_t)DECOMMISSIONED_VALUE;
static const ptrdiff_t DECOMMISSIONED_UINT64_T = (uint64_t)DECOMMISSIONED_VALUE;
static uint8_t* const DECOMMISSIONED_UINT8_T_P = (uint8_t*)DECOMMISSIONED_VALUE;
static uint8_t** const DECOMMISSIONED_UINT8_T_PP = (uint8_t**)DECOMMISSIONED_VALUE;
static PTR_heap_segment const DECOMMISSIONED_REGION_P = (PTR_heap_segment)DECOMMISSIONED_VALUE;
static mark* const DECOMMISSIONED_MARK_P = (mark*)DECOMMISSIONED_VALUE;
static const BOOL DECOMMISSIONED_BOOL = 0xdec0dec0;
static const BOOL DECOMMISSIONED_INT = (int)0xdec0dec0;
static const float DECOMMISSIONED_FLOAT = (float)DECOMMISSIONED_VALUE;

static const ptrdiff_t UNINITIALIZED_VALUE  = 0xbaadbaadbaadbaad;


float log_with_base (float x, float base)
{
    assert (x > base);

    return (float)(log(x) / log(base));
}

float mean (float* arr, int size)
{
    float sum = 0.0;

    for (int i = 0; i < size; i++)
    {
        sum += arr[i];
    }
    return (sum / size);
}

// Change it to a desired number if you want to print.
int max_times_to_print_tcp = 0;

// Return the slope, and the average values in the avg arg.
float gc_heap::dynamic_heap_count_data_t::slope (float* y, int n, float* avg)
{
    assert (n > 0);

    if (n == 1)
    {
        dprintf (6666, ("only 1 tcp: %.3f, no slope", y[0]));
        *avg = y[0];
        return 0.0;
    }

    int sum_x = 0;

    for (int i = 0; i < n; i++)
    {
        sum_x += i;

        if (max_times_to_print_tcp >= 0)
        {
            dprintf (6666, ("%.3f, ", y[i]));
        }
    }

    float avg_x = (float)sum_x / n;
    float avg_y = mean (y, n);
    *avg = avg_y;

    float numerator = 0.0;
    float denominator = 0.0;

    for (int i = 0; i < n; ++i)
    {
        numerator += ((float)i - avg_x) * (y[i] - avg_y);
        denominator += ((float)i - avg_x) * (i - avg_x);
    }

    max_times_to_print_tcp--;

    return (numerator / denominator);
}

#endif //DYNAMIC_HEAP_COUNT
#endif //USE_REGIONS


#ifdef MULTIPLE_HEAPS

#ifdef GC_CONFIG_DRIVEN
#define m_boundary(o) {if (mark_list_index <= mark_list_end) {*mark_list_index = o;mark_list_index++;} else {mark_list_index++;}}
#else //GC_CONFIG_DRIVEN
#define m_boundary(o) {if (mark_list_index <= mark_list_end) {*mark_list_index = o;mark_list_index++;}}
#endif //GC_CONFIG_DRIVEN

#define m_boundary_fullgc(o) {}

#else //MULTIPLE_HEAPS

#ifdef GC_CONFIG_DRIVEN
#define m_boundary(o) {if (mark_list_index <= mark_list_end) {*mark_list_index = o;mark_list_index++;} else {mark_list_index++;} if (slow > o) slow = o; if (shigh < o) shigh = o;}
#else
#define m_boundary(o) {if (mark_list_index <= mark_list_end) {*mark_list_index = o;mark_list_index++;}if (slow > o) slow = o; if (shigh < o) shigh = o;}
#endif //GC_CONFIG_DRIVEN

#define m_boundary_fullgc(o) {if (slow > o) slow = o; if (shigh < o) shigh = o;}

#endif //MULTIPLE_HEAPS

#ifdef USE_REGIONS
inline bool is_in_heap_range (uint8_t* o)
{
#ifdef FEATURE_BASICFREEZE
    // we may have frozen objects in read only segments
    // outside of the reserved address range of the gc heap
    assert (((g_gc_lowest_address <= o) && (o < g_gc_highest_address)) ||
        (o == nullptr) || (ro_segment_lookup (o) != nullptr));
    return ((g_gc_lowest_address <= o) && (o < g_gc_highest_address));
#else //FEATURE_BASICFREEZE
    // without frozen objects, every non-null pointer must be
    // within the heap
    assert ((o == nullptr) || (g_gc_lowest_address <= o) && (o < g_gc_highest_address));
    return (o != nullptr);
#endif //FEATURE_BASICFREEZE
}

#endif //USE_REGIONS

#define new_start() {if (ppstop <= start) {break;} else {parm = start}}
#define ignore_start 0
#define use_start 1

#define go_through_object(mt,o,size,parm,start,start_useful,limit,exp)      \
{                                                                           \
    CGCDesc* map = CGCDesc::GetCGCDescFromMT((MethodTable*)(mt));           \
    CGCDescSeries* cur = map->GetHighestSeries();                           \
    ptrdiff_t cnt = (ptrdiff_t) map->GetNumSeries();                        \
                                                                            \
    if (cnt >= 0)                                                           \
    {                                                                       \
        CGCDescSeries* last = map->GetLowestSeries();                       \
        uint8_t** parm = 0;                                                 \
        do                                                                  \
        {                                                                   \
            assert (parm <= (uint8_t**)((o) + cur->GetSeriesOffset()));     \
            parm = (uint8_t**)((o) + cur->GetSeriesOffset());               \
            uint8_t** ppstop =                                              \
                (uint8_t**)((uint8_t*)parm + cur->GetSeriesSize() + (size));\
            if (!start_useful || (uint8_t*)ppstop > (start))                \
            {                                                               \
                if (start_useful && (uint8_t*)parm < (start)) parm = (uint8_t**)(start);\
                while (parm < ppstop)                                       \
                {                                                           \
                   {exp}                                                    \
                   parm++;                                                  \
                }                                                           \
            }                                                               \
            cur--;                                                          \
                                                                            \
        } while (cur >= last);                                              \
    }                                                                       \
    else                                                                    \
    {                                                                       \
        /* Handle the repeating case - array of valuetypes */               \
        uint8_t** parm = (uint8_t**)((o) + cur->startoffset);               \
        if (start_useful && start > (uint8_t*)parm)                         \
        {                                                                   \
            ptrdiff_t cs = mt->RawGetComponentSize();                         \
            parm = (uint8_t**)((uint8_t*)parm + (((start) - (uint8_t*)parm)/cs)*cs); \
        }                                                                   \
        while ((uint8_t*)parm < ((o)+(size)-plug_skew))                     \
        {                                                                   \
            for (ptrdiff_t __i = 0; __i > cnt; __i--)                         \
            {                                                               \
                HALF_SIZE_T skip =  (cur->val_serie + __i)->skip;           \
                HALF_SIZE_T nptrs = (cur->val_serie + __i)->nptrs;          \
                uint8_t** ppstop = parm + nptrs;                            \
                if (!start_useful || (uint8_t*)ppstop > (start))            \
                {                                                           \
                    if (start_useful && (uint8_t*)parm < (start)) parm = (uint8_t**)(start);      \
                    do                                                      \
                    {                                                       \
                       {exp}                                                \
                       parm++;                                              \
                    } while (parm < ppstop);                                \
                }                                                           \
                parm = (uint8_t**)((uint8_t*)ppstop + skip);                \
            }                                                               \
        }                                                                   \
    }                                                                       \
}

#define go_through_object_nostart(mt,o,size,parm,exp) {go_through_object(mt,o,size,parm,o,ignore_start,(o + size),exp); }

// 1 thing to note about this macro:
// 1) you can use *parm safely but in general you don't want to use parm
// because for the collectible types it's not an address on the managed heap.
#ifndef COLLECTIBLE_CLASS
#define go_through_object_cl(mt,o,size,parm,exp)                            \
{                                                                           \
    if (header(o)->ContainsGCPointers())                                      \
    {                                                                       \
        go_through_object_nostart(mt,o,size,parm,exp);                      \
    }                                                                       \
}
#else //COLLECTIBLE_CLASS
#define go_through_object_cl(mt,o,size,parm,exp)                            \
{                                                                           \
    if (header(o)->Collectible())                                           \
    {                                                                       \
        uint8_t* class_obj = get_class_object (o);                             \
        uint8_t** parm = &class_obj;                                           \
        do {exp} while (false);                                             \
    }                                                                       \
    if (header(o)->ContainsGCPointers())                                      \
    {                                                                       \
        go_through_object_nostart(mt,o,size,parm,exp);                      \
    }                                                                       \
}
#endif //COLLECTIBLE_CLASS

// enable on processors known to have a useful prefetch instruction
#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM64) || defined(TARGET_RISCV64)
#define PREFETCH
#endif

#ifdef PREFETCH
inline void Prefetch(void* addr)
{
#ifdef TARGET_WINDOWS

#if defined(TARGET_AMD64) || defined(TARGET_X86)

#ifndef _MM_HINT_T0
#define _MM_HINT_T0 1
#endif
    _mm_prefetch((const char*)addr, _MM_HINT_T0);
#elif defined(TARGET_ARM64)
    __prefetch((const char*)addr);
#endif //defined(TARGET_AMD64) || defined(TARGET_X86)

#elif defined(TARGET_UNIX)
    __builtin_prefetch(addr);
#else //!(TARGET_WINDOWS || TARGET_UNIX)
    UNREFERENCED_PARAMETER(addr);
#endif //TARGET_WINDOWS
}
#else //PREFETCH
inline void Prefetch (void* addr)
{
    UNREFERENCED_PARAMETER(addr);
}
#endif //PREFETCH

#define stolen 2
#define partial 1
#define partial_object 3

inline
BOOL stolen_p (uint8_t* r)
{
    return (((size_t)r&2) && !((size_t)r&1));
}
inline
BOOL ready_p (uint8_t* r)
{
    return ((size_t)r != 1);
}
inline
BOOL partial_p (uint8_t* r)
{
    return (((size_t)r&1) && !((size_t)r&2));
}
inline
BOOL straight_ref_p (uint8_t* r)
{
    return (!stolen_p (r) && !partial_p (r));
}
inline
BOOL partial_object_p (uint8_t* r)
{
    return (((size_t)r & partial_object) == partial_object);
}



#ifdef MULTIPLE_HEAPS

static VOLATILE(BOOL) s_fUnpromotedHandles = FALSE;
static VOLATILE(BOOL) s_fUnscannedPromotions = FALSE;
static VOLATILE(BOOL) s_fScanRequired;
#else //MULTIPLE_HEAPS
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_STRUCTALIGN
//
// The word with left child, right child, and align info is laid out as follows:
//
//      |   upper short word   |   lower short word   |
//      |<------------> <----->|<------------> <----->|
//      |  left child   info hi| right child   info lo|
// x86: |    10 bits     6 bits|   10 bits      6 bits|
//
// where left/right child are signed values and concat(info hi, info lo) is unsigned.
//
// The "align info" encodes two numbers: the required alignment (a power of two)
// and the misalignment (the number of machine words the destination address needs
// to be adjusted by to provide alignment - so this number is always smaller than
// the required alignment).  Thus, the two can be represented as the "logical or"
// of the two numbers.  Note that the actual pad is computed from the misalignment
// by adding the alignment iff the misalignment is non-zero and less than min_obj_size.
//

// The number of bits in a brick.
#if defined (TARGET_AMD64)
#define brick_bits (12)
#else
#define brick_bits (11)
#endif //TARGET_AMD64
static_assert(brick_size == (1 << brick_bits));

// The number of bits needed to represent the offset to a child node.
// "brick_bits + 1" allows us to represent a signed offset within a brick.
#define child_bits (brick_bits + 1 - LOG2_PTRSIZE)

// The number of bits in each of the pad hi, pad lo fields.
#define pad_bits (sizeof(short) * 8 - child_bits)

#define child_from_short(w) (((signed short)(w) / (1 << (pad_bits - LOG2_PTRSIZE))) & ~((1 << LOG2_PTRSIZE) - 1))
#define pad_mask ((1 << pad_bits) - 1)
#define pad_from_short(w) ((size_t)(w) & pad_mask)
#else // FEATURE_STRUCTALIGN
#define child_from_short(w) (w)
#endif // FEATURE_STRUCTALIGN

inline
short node_left_child(uint8_t* node)
{
    return child_from_short(((plug_and_pair*)node)[-1].m_pair.left);
}

inline
void set_node_left_child(uint8_t* node, ptrdiff_t val)
{
    assert (val > -(ptrdiff_t)brick_size);
    assert (val < (ptrdiff_t)brick_size);
    assert (Aligned (val));
#ifdef FEATURE_STRUCTALIGN
    size_t pad = pad_from_short(((plug_and_pair*)node)[-1].m_pair.left);
    ((plug_and_pair*)node)[-1].m_pair.left = ((short)val << (pad_bits - LOG2_PTRSIZE)) | (short)pad;
#else // FEATURE_STRUCTALIGN
    ((plug_and_pair*)node)[-1].m_pair.left = (short)val;
#endif // FEATURE_STRUCTALIGN
    assert (node_left_child (node) == val);
}

inline
short node_right_child(uint8_t* node)
{
    return child_from_short(((plug_and_pair*)node)[-1].m_pair.right);
}

inline
void set_node_right_child(uint8_t* node, ptrdiff_t val)
{
    assert (val > -(ptrdiff_t)brick_size);
    assert (val < (ptrdiff_t)brick_size);
    assert (Aligned (val));
#ifdef FEATURE_STRUCTALIGN
    size_t pad = pad_from_short(((plug_and_pair*)node)[-1].m_pair.right);
    ((plug_and_pair*)node)[-1].m_pair.right = ((short)val << (pad_bits - LOG2_PTRSIZE)) | (short)pad;
#else // FEATURE_STRUCTALIGN
    ((plug_and_pair*)node)[-1].m_pair.right = (short)val;
#endif // FEATURE_STRUCTALIGN
    assert (node_right_child (node) == val);
}

#ifdef FEATURE_STRUCTALIGN
void node_aligninfo (uint8_t* node, int& requiredAlignment, ptrdiff_t& pad)
{
    // Extract the single-number aligninfo from the fields.
    short left = ((plug_and_pair*)node)[-1].m_pair.left;
    short right = ((plug_and_pair*)node)[-1].m_pair.right;
    ptrdiff_t pad_shifted = (pad_from_short(left) << pad_bits) | pad_from_short(right);
    ptrdiff_t aligninfo = pad_shifted * DATA_ALIGNMENT;

    // Replicate the topmost bit into all lower bits.
    ptrdiff_t x = aligninfo;
    x |= x >> 8;
    x |= x >> 4;
    x |= x >> 2;
    x |= x >> 1;

    // Clear all bits but the highest.
    requiredAlignment = (int)(x ^ (x >> 1));
    pad = aligninfo - requiredAlignment;
    pad += AdjustmentForMinPadSize(pad, requiredAlignment);
}

inline
ptrdiff_t node_alignpad (uint8_t* node)
{
    int requiredAlignment;
    ptrdiff_t alignpad;
    node_aligninfo (node, requiredAlignment, alignpad);
    return alignpad;
}

void clear_node_aligninfo (uint8_t* node)
{
    ((plug_and_pair*)node)[-1].m_pair.left &= ~0 << pad_bits;
    ((plug_and_pair*)node)[-1].m_pair.right &= ~0 << pad_bits;
}

void set_node_aligninfo (uint8_t* node, int requiredAlignment, ptrdiff_t pad)
{
    // Encode the alignment requirement and alignment offset as a single number
    // as described above.
    ptrdiff_t aligninfo = (size_t)requiredAlignment + (pad & (requiredAlignment-1));
    assert (Aligned (aligninfo));
    ptrdiff_t aligninfo_shifted = aligninfo / DATA_ALIGNMENT;
    assert (aligninfo_shifted < (1 << (pad_bits + pad_bits)));

    ptrdiff_t hi = aligninfo_shifted >> pad_bits;
    assert (pad_from_short(((plug_and_gap*)node)[-1].m_pair.left) == 0);
    ((plug_and_pair*)node)[-1].m_pair.left |= hi;

    ptrdiff_t lo = aligninfo_shifted & pad_mask;
    assert (pad_from_short(((plug_and_gap*)node)[-1].m_pair.right) == 0);
    ((plug_and_pair*)node)[-1].m_pair.right |= lo;

#ifdef _DEBUG
    int requiredAlignment2;
    ptrdiff_t pad2;
    node_aligninfo (node, requiredAlignment2, pad2);
    assert (requiredAlignment == requiredAlignment2);
    assert (pad == pad2);
#endif // _DEBUG
}
#endif // FEATURE_STRUCTALIGN

inline
void loh_set_node_relocation_distance(uint8_t* node, ptrdiff_t val)
{
    ptrdiff_t* place = &(((loh_obj_and_pad*)node)[-1].reloc);
    *place = val;
}

inline
ptrdiff_t loh_node_relocation_distance(uint8_t* node)
{
    return (((loh_obj_and_pad*)node)[-1].reloc);
}

inline
ptrdiff_t node_relocation_distance (uint8_t* node)
{
    return (((plug_and_reloc*)(node))[-1].reloc & ~3);
}

inline
void set_node_relocation_distance(uint8_t* node, ptrdiff_t val)
{
    assert (val == (val & ~3));
    ptrdiff_t* place = &(((plug_and_reloc*)node)[-1].reloc);
    //clear the left bit and the relocation field
    *place &= 1;
    *place |= val;
}

#define node_left_p(node) (((plug_and_reloc*)(node))[-1].reloc & 2)

#define set_node_left(node) ((plug_and_reloc*)(node))[-1].reloc |= 2;

#ifndef FEATURE_STRUCTALIGN
void set_node_realigned(uint8_t* node)
{
    ((plug_and_reloc*)(node))[-1].reloc |= 1;
}

void clear_node_realigned(uint8_t* node)
{
#ifdef RESPECT_LARGE_ALIGNMENT
    ((plug_and_reloc*)(node))[-1].reloc &= ~1;
#else
    UNREFERENCED_PARAMETER(node);
#endif //RESPECT_LARGE_ALIGNMENT
}
#endif // FEATURE_STRUCTALIGN

inline
size_t  node_gap_size (uint8_t* node)
{
    return ((plug_and_gap *)node)[-1].gap;
}

void set_gap_size (uint8_t* node, size_t size)
{
    assert (Aligned (size));

    // clear the 2 uint32_t used by the node.
    ((plug_and_gap *)node)[-1].reloc = 0;
    ((plug_and_gap *)node)[-1].lr =0;
    ((plug_and_gap *)node)[-1].gap = size;

    assert ((size == 0 )||(size >= sizeof(plug_and_reloc)));

}

/*****************************
Called after compact phase to fix all generation gaps
********************************/
#ifdef USE_REGIONS
void heap_segment::thread_free_obj (uint8_t* obj, size_t s)
{
    //dprintf (REGIONS_LOG, ("threading SIP free obj %zx-%zx(%zd)", obj, (obj + s), s));
    if (s >= min_free_list)
    {
        free_list_slot (obj) = 0;

        if (free_list_head)
        {
            assert (free_list_tail);
            free_list_slot (free_list_tail) = obj;
        }
        else
        {
            free_list_head = obj;
        }

        free_list_tail = obj;

        free_list_size += s;
    }
    else
    {
        free_obj_size += s;
    }
}

#endif //USE_REGIONS

inline
uint8_t* tree_search (uint8_t* tree, uint8_t* old_address)
{
    uint8_t* candidate = 0;
    int cn;
    while (1)
    {
        if (tree < old_address)
        {
            if ((cn = node_right_child (tree)) != 0)
            {
                assert (candidate < tree);
                candidate = tree;
                tree = tree + cn;
                Prefetch (&((plug_and_pair*)tree)[-1].m_pair.left);
                continue;
            }
            else
                break;
        }
        else if (tree > old_address)
        {
            if ((cn = node_left_child (tree)) != 0)
            {
                tree = tree + cn;
                Prefetch (&((plug_and_pair*)tree)[-1].m_pair.left);
                continue;
            }
            else
                break;
        } else
            break;
    }
    if (tree <= old_address)
        return tree;
    else if (candidate)
        return candidate;
    else
        return tree;
}


#ifdef MULTIPLE_HEAPS

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4702) // C4702: unreachable code: gc_thread_function may not return
#endif //_MSC_VER
void gc_heap::gc_thread_stub (void* arg)
{
    gc_heap* heap = (gc_heap*)arg;
    if (!gc_thread_no_affinitize_p)
    {
        // We are about to set affinity for GC threads. It is a good place to set up NUMA and
        // CPU groups because the process mask, processor number, and group number are all
        // readily available.
        set_thread_affinity_for_heap (heap->heap_number, heap_select::find_proc_no_from_heap_no (heap->heap_number));
    }

    // server GC threads run at a higher priority than normal.
    GCToOSInterface::BoostThreadPriority();
    void* tmp = _alloca (256*heap->heap_number);
    heap->gc_thread_function();
}
#ifdef _MSC_VER
#pragma warning(pop)
#endif //_MSC_VER

#endif //MULTIPLE_HEAPS

#ifdef BACKGROUND_GC
#ifdef MULTIPLE_HEAPS
void
gc_heap::bgc_suspend_EE ()
{
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap::g_heaps[i]->reset_gc_done();
    }
    gc_started = TRUE;
    dprintf (2, ("bgc_suspend_EE"));
    GCToEEInterface::SuspendEE(SUSPEND_FOR_GC_PREP);

    gc_started = FALSE;
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap::g_heaps[i]->set_gc_done();
    }
}
#else
void
gc_heap::bgc_suspend_EE ()
{
    reset_gc_done();
    gc_started = TRUE;
    dprintf (2, ("bgc_suspend_EE"));
    GCToEEInterface::SuspendEE(SUSPEND_FOR_GC_PREP);
    gc_started = FALSE;
    set_gc_done();
}
#endif //MULTIPLE_HEAPS

#ifdef BGC_SERVO_TUNING

#endif //BGC_SERVO_TUNING
#endif //BACKGROUND_GC

//because of heap expansion, computing end is complicated.
uint8_t* compute_next_end (heap_segment* seg, uint8_t* low)
{
    if ((low >=  heap_segment_mem (seg)) &&
        (low < heap_segment_allocated (seg)))
        return low;
    else
        return heap_segment_allocated (seg);
}


#ifdef FEATURE_CARD_MARKING_STEALING
bool card_marking_enumerator::move_next(heap_segment* seg, uint8_t*& low, uint8_t*& high)
{
    if (segment == nullptr)
        return false;

    uint32_t chunk_index = old_chunk_index;
    old_chunk_index = INVALID_CHUNK_INDEX;
    if (chunk_index == INVALID_CHUNK_INDEX)
        chunk_index = Interlocked::Increment((volatile int32_t *)chunk_index_counter);

    while (true)
    {
        uint32_t chunk_index_within_seg = chunk_index - segment_start_chunk_index;

        uint8_t* start = heap_segment_mem(segment);
        uint8_t* end = compute_next_end(segment, gc_low);

        uint8_t* aligned_start = (uint8_t*)((size_t)start & ~(CARD_MARKING_STEALING_GRANULARITY - 1));
        size_t seg_size = end - aligned_start;
        uint32_t chunk_count_within_seg = (uint32_t)((seg_size + (CARD_MARKING_STEALING_GRANULARITY - 1)) / CARD_MARKING_STEALING_GRANULARITY);
        if (chunk_index_within_seg < chunk_count_within_seg)
        {
            if (seg == segment)
            {
                low = (chunk_index_within_seg == 0) ? start : (aligned_start + (size_t)chunk_index_within_seg * CARD_MARKING_STEALING_GRANULARITY);
                high = (chunk_index_within_seg + 1 == chunk_count_within_seg) ? end : (aligned_start + (size_t)(chunk_index_within_seg + 1) * CARD_MARKING_STEALING_GRANULARITY);
                chunk_high = high;

                dprintf (3, ("cme:mn ci: %u, low: %p, high: %p", chunk_index, low, high));

                return true;
            }
            else
            {
                // we found the correct segment, but it's not the segment our caller is in

                // our caller should still be in one of the previous segments
#ifdef _DEBUG
                for (heap_segment* cur_seg = seg; cur_seg != segment; cur_seg = heap_segment_next_in_range(cur_seg))
                {
                    assert(cur_seg);
                }
#endif //_DEBUG

                // keep the chunk index for later
                old_chunk_index = chunk_index;

                dprintf (3, ("cme:mn oci: %u, seg mismatch seg: %p, segment: %p", old_chunk_index, heap_segment_mem (segment), heap_segment_mem (seg)));

                return false;
            }
        }

        segment = heap_segment_next_in_range(segment);
        segment_start_chunk_index += chunk_count_within_seg;
        if (segment == nullptr)
        {
            // keep the chunk index for later
            old_chunk_index = chunk_index;

            dprintf (3, ("cme:mn oci: %u no more segments", old_chunk_index));

            return false;
        }
    }
}

#endif // FEATURE_CARD_MARKING_STEALING

//-----------------------------------------------------------------------------
//
//                                  VM Specific support
//
//-----------------------------------------------------------------------------

//Static member variables.
VOLATILE(BOOL)    GCHeap::GcInProgress            = FALSE;
GCEvent           *GCHeap::WaitForGCEvent         = NULL;
unsigned          GCHeap::GcCondemnedGeneration   = 0;
size_t            GCHeap::totalSurvivedSize       = 0;
#ifdef FEATURE_PREMORTEM_FINALIZATION
CFinalize*        GCHeap::m_Finalize              = 0;
BOOL              GCHeap::GcCollectClasses        = FALSE;
VOLATILE(int32_t) GCHeap::m_GCFLock               = 0;

#ifndef FEATURE_NATIVEAOT // NativeAOT forces relocation a different way
#ifdef STRESS_HEAP
#ifndef MULTIPLE_HEAPS
OBJECTHANDLE      GCHeap::m_StressObjs[NUM_HEAP_STRESS_OBJS];
int               GCHeap::m_CurStressObj          = 0;
#endif // !MULTIPLE_HEAPS
#endif // STRESS_HEAP
#endif // FEATURE_NATIVEAOT

#endif //FEATURE_PREMORTEM_FINALIZATION

#ifdef VERIFY_HEAP
void
gc_heap::verify_free_lists ()
{
    for (int gen_num = 0; gen_num < total_generation_count; gen_num++)
    {
        dprintf (3, ("Verifying free list for gen:%d", gen_num));
        allocator* gen_alloc = generation_allocator (generation_of (gen_num));
        size_t sz = gen_alloc->first_bucket_size();
        bool verify_undo_slot = (gen_num != 0) && (gen_num <= max_generation) && !gen_alloc->discard_if_no_fit_p();

        for (unsigned int a_l_number = 0; a_l_number < gen_alloc->number_of_buckets(); a_l_number++)
        {
            uint8_t* free_list = gen_alloc->alloc_list_head_of (a_l_number);
            uint8_t* prev = 0;
            while (free_list)
            {
                if (!((CObjectHeader*)free_list)->IsFree())
                {
                    dprintf (1, ("Verifiying Heap: curr free list item %zx isn't a free object",
                                 (size_t)free_list));
                    FATAL_GC_ERROR();
                }
                if (((a_l_number < (gen_alloc->number_of_buckets()-1))&& (unused_array_size (free_list) >= sz))
                    || ((a_l_number != 0) && (unused_array_size (free_list) < sz/2)))
                {
                    dprintf (1, ("Verifiying Heap: curr free list item %zx isn't in the right bucket",
                                 (size_t)free_list));
                    FATAL_GC_ERROR();
                }
                if (verify_undo_slot && (free_list_undo (free_list) != UNDO_EMPTY))
                {
                    dprintf (1, ("Verifiying Heap: curr free list item %zx has non empty undo slot",
                                 (size_t)free_list));
                    FATAL_GC_ERROR();
                }
                if ((gen_num <= max_generation) && (object_gennum (free_list)!= gen_num))
                {
                    dprintf (1, ("Verifiying Heap: curr free list item %zx is in the wrong generation free list",
                                 (size_t)free_list));
                    FATAL_GC_ERROR();
                }

#ifdef DOUBLY_LINKED_FL
                uint8_t* prev_free_item = free_list_prev (free_list);
                if (gen_num == max_generation)
                {
                    if (prev_free_item != prev)
                    {
                        dprintf (1, ("%p prev should be: %p, actual: %p", free_list, prev_free_item, prev));
                        FATAL_GC_ERROR();
                    }
                }
#endif //DOUBLY_LINKED_FL

#if defined(USE_REGIONS) && defined(MULTIPLE_HEAPS)
                heap_segment* region = region_of (free_list);
                if ((region->heap != this) && ((gen_num != max_generation) || (!trigger_bgc_for_rethreading_p)))
                {
                    // The logic in change_heap_count depends on the coming BGC (or blocking gen 2) to rebuild the gen 2 free list.
                    // In that case, before the rebuild happens, the gen2 free list is expected to contain free list items that do not belong to the right heap.
                    dprintf (1, ("curr free item %p should be on heap %d, but actually is on heap %d: %d", free_list, this->heap_number, region->heap->heap_number));
                    FATAL_GC_ERROR();
                }
#endif //USE_REGIONS && MULTIPLE_HEAPS
                prev = free_list;
                free_list = free_list_slot (free_list);
            }
            //verify the sanity of the tail
            uint8_t* tail = gen_alloc->alloc_list_tail_of (a_l_number);
            if (!((tail == 0) || (tail == prev)))
            {
                dprintf (1, ("Verifying Heap: tail of free list is not correct, tail %p, prev %p", tail, prev));
                FATAL_GC_ERROR();
            }
            if (tail == 0)
            {
                uint8_t* head = gen_alloc->alloc_list_head_of (a_l_number);
                if ((head != 0) && (free_list_slot (head) != 0))
                {
                    dprintf (1, ("Verifying Heap: head of free list is not correct, head %p -> %p",
                        head, free_list_slot (head)));
                    FATAL_GC_ERROR();
                }
            }

            sz *=2;
        }
    }
}


#endif  //VERIFY_HEAP


// Wait until a garbage collection is complete
// returns NOERROR if wait was OK, other error code if failure.
// WARNING: This will not undo the must complete state. If you are
// in a must complete when you call this, you'd better know what you're
// doing.

#ifdef FEATURE_PREMORTEM_FINALIZATION
static
HRESULT AllocateCFinalize(CFinalize **pCFinalize)
{
    *pCFinalize = new (nothrow) CFinalize();
    if (*pCFinalize == NULL || !(*pCFinalize)->Initialize())
        return E_OUTOFMEMORY;

    return S_OK;
}
#endif // FEATURE_PREMORTEM_FINALIZATION

#ifndef FEATURE_NATIVEAOT // NativeAOT forces relocation a different way
#ifdef STRESS_HEAP

void StressHeapDummy ();

#endif // STRESS_HEAP
#endif // !FEATURE_NATIVEAOT

#ifdef FEATURE_PREMORTEM_FINALIZATION
#define REGISTER_FOR_FINALIZATION(_object, _size) \
    hp->finalize_queue->RegisterForFinalization (0, (_object), (_size))
#else // FEATURE_PREMORTEM_FINALIZATION
#define REGISTER_FOR_FINALIZATION(_object, _size) true
#endif // FEATURE_PREMORTEM_FINALIZATION

#define CHECK_ALLOC_AND_POSSIBLY_REGISTER_FOR_FINALIZATION(_object, _size, _register) do {  \
    if ((_object) == NULL || ((_register) && !REGISTER_FOR_FINALIZATION(_object, _size)))   \
    {                                                                                       \
        STRESS_LOG_OOM_STACK(_size);                                                        \
        return NULL;                                                                        \
    }                                                                                       \
} while (false)

#if defined(WRITE_BARRIER_CHECK) && !defined (SERVER_GC)
// This code is designed to catch the failure to update the write barrier
// The way it works is to copy the whole heap right after every GC.  The write
// barrier code has been modified so that it updates the shadow as well as the
// real GC heap.  Before doing the next GC, we walk the heap, looking for pointers
// that were updated in the real heap, but not the shadow.  A mismatch indicates
// an error.  The offending code can be found by breaking after the correct GC,
// and then placing a data breakpoint on the Heap location that was updated without
// going through the write barrier.

// Called at process shutdown
void deleteGCShadow()
{
    if (g_GCShadow != 0)
        GCToOSInterface::VirtualRelease (g_GCShadow, g_GCShadowEnd - g_GCShadow);
    g_GCShadow = 0;
    g_GCShadowEnd = 0;
}

// Called at startup and right after a GC, get a snapshot of the GC Heap
void initGCShadow()
{
    if (!(GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_BARRIERCHECK))
        return;

    uint8_t* highest = nullptr;

#ifdef USE_REGIONS
    highest = global_region_allocator.get_left_used_unsafe();
#else
    highest = g_gc_highest_address;
#endif

    size_t len = g_gc_highest_address - g_gc_lowest_address;
    size_t commit_len = highest - g_gc_lowest_address;
    if (len > (size_t)(g_GCShadowEnd - g_GCShadow))
    {
        deleteGCShadow();
        g_GCShadowEnd = g_GCShadow = (uint8_t *)GCToOSInterface::VirtualReserve (len, 0, VirtualReserveFlags::None);
        if (g_GCShadow == NULL || !GCToOSInterface::VirtualCommit (g_GCShadow, commit_len))
        {
            _ASSERTE(!"Not enough memory to run HeapVerify level 2");
            // If after the assert we decide to allow the program to continue
            // running we need to be in a state that will not trigger any
            // additional AVs while we fail to allocate a shadow segment, i.e.
            // ensure calls to updateGCShadow() checkGCWriteBarrier() don't AV
            deleteGCShadow();
            return;
        }

        g_GCShadowEnd += commit_len;
    }

    // save the value of g_gc_lowest_address at this time.  If this value changes before
    // the next call to checkGCWriteBarrier() it means we extended the heap (with a
    // large object segment most probably), and the whole shadow segment is inconsistent.
    g_shadow_lowest_address = g_gc_lowest_address;

    //****** Copy the whole GC heap ******
    //
    // NOTE: This is the one situation where the combination of heap_segment_rw(gen_start_segment())
    // can produce a NULL result.  This is because the initialization has not completed.
    //
    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        generation* gen = gc_heap::generation_of (i);
        heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

        ptrdiff_t delta = g_GCShadow - g_gc_lowest_address;
        while (seg)
        {
            // Copy the segment
            uint8_t* start = heap_segment_mem (seg);
            uint8_t* end = heap_segment_allocated (seg);
            memcpy (start + delta, start, end - start);
            seg = heap_segment_next_rw (seg);
        }
    }
}

#define INVALIDGCVALUE (void*)((size_t)0xcccccccd)

// test to see if 'ptr' was only updated via the write barrier.
inline void testGCShadow(Object** ptr)
{
    Object** shadow = (Object**) &g_GCShadow[((uint8_t*) ptr - g_gc_lowest_address)];
    if (*ptr != 0 && (uint8_t*) shadow < g_GCShadowEnd && *ptr != *shadow)
    {
        // If you get this assertion, someone updated a GC pointer in the heap without
        // using the write barrier.  To find out who, check the value of
        // dd_collection_count (dynamic_data_of (0)). Also
        // note the value of 'ptr'.  Rerun the App that the previous GC just occurred.
        // Then put a data breakpoint for the value of 'ptr'  Then check every write
        // to pointer between the two GCs.  The last one is not using the write barrier.

        // If the memory of interest does not exist at system startup,
        // you need to set the data breakpoint right after the memory gets committed
        // Set a breakpoint at the end of grow_heap_segment, and put the value of 'ptr'
        // in the memory window.  run until the memory gets mapped. Then you can set
        // your breakpoint

        // Note a recent change, we've identified race conditions when updating the gc shadow.
        // Throughout the runtime, code will update an address in the gc heap, then erect the
        // write barrier, which calls updateGCShadow. With an app that pounds one heap location
        // from multiple threads, you can hit this assert even though all involved are using the
        // write barrier properly. Thusly, we detect the race and set this location to INVALIDGCVALUE.
        // TODO: the code in jithelp.asm doesn't call updateGCShadow, and hasn't been
        // TODO: fixed to detect the race. We've only seen this race from VolatileWritePtr,
        // TODO: so elect not to fix jithelp.asm at this time. It should be done if we start hitting
        // TODO: erroneous asserts in here.
        if(*shadow!=INVALIDGCVALUE)
        {
#ifdef FEATURE_BASICFREEZE
            // Write barriers for stores of references to frozen objects may be optimized away.
            if (!g_theGCHeap->IsInFrozenSegment (*ptr))
#endif // FEATURE_BASICFREEZE
            {
                _ASSERTE(!"Pointer updated without using write barrier");
            }
        }
        /*
        else
        {
             printf("saw a INVALIDGCVALUE. (just to let you know)\n");
        }
        */
    }
}

void testGCShadowHelper (uint8_t* x)
{
    size_t s = size (x);
    if (contain_pointers (x))
    {
        go_through_object_nostart (method_table(x), x, s, oo,
                           { testGCShadow((Object**) oo); });
    }
}

// Walk the whole heap, looking for pointers that were not updated with the write barrier.
void checkGCWriteBarrier()
{
    // g_shadow_lowest_address != g_gc_lowest_address means the GC heap was extended by a segment
    // and the GC shadow segment did not track that change!
    if (g_GCShadowEnd <= g_GCShadow || g_shadow_lowest_address != g_gc_lowest_address)
    {
        // No shadow heap, nothing to check.
        return;
    }

    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        int alignment = get_alignment_constant(i <= max_generation);
        {
            generation* gen = gc_heap::generation_of (i);
            heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

            _ASSERTE(seg != NULL);

            while(seg)
            {
                uint8_t* x = heap_segment_mem (seg);
                while (x < heap_segment_allocated (seg))
                {
                    size_t s = size (x);
                    testGCShadowHelper (x);
                    x = x + Align (s, alignment);
                }
                seg = heap_segment_next_rw (seg);
            }
        }
    }
}
#endif //WRITE_BARRIER_CHECK && !SERVER_GC

#ifdef GC_DESCRIPTOR
extern "C"
{
    struct ContractDescriptor;
    extern ContractDescriptor GCContractDescriptorWKS;
#if FEATURE_SVR_GC
    extern ContractDescriptor GCContractDescriptorSVR;
#endif // FEATURE_SVR_GC
}
#endif // GC_DESCRIPTOR

void PopulateDacVars(GcDacVars *gcDacVars)
{
    bool v2 = gcDacVars->minor_version_number >= 2;
    bool v4 = gcDacVars->minor_version_number >= 4;
    bool v6 = gcDacVars->minor_version_number >= 6;
    bool v8 = gcDacVars->minor_version_number >= 8;

#define DEFINE_FIELD(field_name, field_type) offsetof(CLASS_NAME, field_name),
#define DEFINE_DPTR_FIELD(field_name, field_type) offsetof(CLASS_NAME, field_name),
#define DEFINE_ARRAY_FIELD(field_name, field_type, array_length) offsetof(CLASS_NAME, field_name),
#define DEFINE_MISSING_FIELD(field_name) -1,

#ifdef MULTIPLE_HEAPS
    static int gc_heap_field_offsets[] = {
#define CLASS_NAME gc_heap
#include "dac_gcheap_fields.h"
#undef CLASS_NAME
    };
#endif //MULTIPLE_HEAPS
    static int generation_field_offsets[] = {

#define CLASS_NAME generation
#include "dac_generation_fields.h"
#undef CLASS_NAME

#undef DEFINE_MISSING_FIELD
#undef DEFINE_ARRAY_FIELD
#undef DEFINE_DPTR_FIELD
#undef DEFINE_FIELD
    };

    assert(gcDacVars != nullptr);
    // Note: These version numbers do not need to be checked in the .Net dac/SOS because
    // we always match the compiled dac and GC to the version used.  NativeAOT's SOS may
    // work differently than .Net SOS.  When making breaking changes here you may need to
    // find NativeAOT's equivalent of SOS_BREAKING_CHANGE_VERSION and increment it.
    gcDacVars->major_version_number = 2;
    gcDacVars->minor_version_number = 8;
    if (v2)
    {
        gcDacVars->total_bookkeeping_elements = total_bookkeeping_elements;
        gcDacVars->card_table_info_size = sizeof(card_table_info);
    }

    g_build_variant = 0;
#ifdef USE_REGIONS
    g_build_variant |= build_variant_use_region;
    if (v2)
    {
        gcDacVars->count_free_region_kinds = count_free_region_kinds;
        gcDacVars->global_regions_to_decommit = reinterpret_cast<dac_region_free_list**>(&gc_heap::global_regions_to_decommit);
        gcDacVars->global_free_huge_regions = reinterpret_cast<dac_region_free_list**>(&gc_heap::global_free_huge_regions);
    }
#endif //USE_REGIONS
#ifdef BACKGROUND_GC
    g_build_variant |= build_variant_background_gc;
#endif //BACKGROUND_GC
#ifdef DYNAMIC_HEAP_COUNT
    g_build_variant |= build_variant_dynamic_heap_count;
#endif //DYNAMIC_HEAP_COUNT
    gcDacVars->built_with_svr = &g_built_with_svr_gc;
    gcDacVars->build_variant = &g_build_variant;
    gcDacVars->gc_structures_invalid_cnt = const_cast<int32_t*>(&GCScan::m_GcStructuresInvalidCnt);
    gcDacVars->generation_size = sizeof(generation);
    gcDacVars->total_generation_count = total_generation_count;
    gcDacVars->max_gen = &g_max_generation;
#ifdef BACKGROUND_GC
    gcDacVars->current_c_gc_state = const_cast<c_gc_state*>(&gc_heap::current_c_gc_state);
#else //BACKGROUND_GC
    gcDacVars->current_c_gc_state = 0;
#endif //BACKGROUND_GC
#ifndef MULTIPLE_HEAPS
    gcDacVars->ephemeral_heap_segment = reinterpret_cast<dac_heap_segment**>(&gc_heap::ephemeral_heap_segment);
#ifdef USE_REGIONS
    if (v2)
    {
        gcDacVars->free_regions = reinterpret_cast<dac_region_free_list**>(&gc_heap::free_regions);
    }
#endif
#ifdef BACKGROUND_GC
    gcDacVars->mark_array = &gc_heap::mark_array;
    gcDacVars->background_saved_lowest_address = &gc_heap::background_saved_lowest_address;
    gcDacVars->background_saved_highest_address = &gc_heap::background_saved_highest_address;
    if (v2)
    {
        gcDacVars->freeable_soh_segment = reinterpret_cast<dac_heap_segment**>(&gc_heap::freeable_soh_segment);
        gcDacVars->freeable_uoh_segment = reinterpret_cast<dac_heap_segment**>(&gc_heap::freeable_uoh_segment);
    }
    gcDacVars->next_sweep_obj = &gc_heap::next_sweep_obj;
#ifdef USE_REGIONS
    gcDacVars->saved_sweep_ephemeral_seg = 0;
    gcDacVars->saved_sweep_ephemeral_start = 0;
#else
    gcDacVars->saved_sweep_ephemeral_seg = reinterpret_cast<dac_heap_segment**>(&gc_heap::saved_sweep_ephemeral_seg);
    gcDacVars->saved_sweep_ephemeral_start = &gc_heap::saved_sweep_ephemeral_start;
#endif //USE_REGIONS
#else //BACKGROUND_GC
    gcDacVars->mark_array = 0;
    gcDacVars->background_saved_lowest_address = 0;
    gcDacVars->background_saved_highest_address = 0;
    if (v2)
    {
        gcDacVars->freeable_soh_segment = 0;
        gcDacVars->freeable_uoh_segment = 0;
    }
    gcDacVars->next_sweep_obj = 0;
    gcDacVars->saved_sweep_ephemeral_seg = 0;
    gcDacVars->saved_sweep_ephemeral_start = 0;
#endif //BACKGROUND_GC
    gcDacVars->alloc_allocated = &gc_heap::alloc_allocated;
    gcDacVars->oom_info = &gc_heap::oom_info;
    gcDacVars->finalize_queue = reinterpret_cast<dac_finalize_queue**>(&gc_heap::finalize_queue);
    gcDacVars->generation_table = reinterpret_cast<unused_generation**>(&gc_heap::generation_table);
#ifdef GC_CONFIG_DRIVEN
    gcDacVars->gc_global_mechanisms = reinterpret_cast<size_t**>(&gc_global_mechanisms);
    gcDacVars->interesting_data_per_heap = reinterpret_cast<size_t**>(&gc_heap::interesting_data_per_heap);
    gcDacVars->compact_reasons_per_heap = reinterpret_cast<size_t**>(&gc_heap::compact_reasons_per_heap);
    gcDacVars->expand_mechanisms_per_heap = reinterpret_cast<size_t**>(&gc_heap::expand_mechanisms_per_heap);
    gcDacVars->interesting_mechanism_bits_per_heap = reinterpret_cast<size_t**>(&gc_heap::interesting_mechanism_bits_per_heap);
#endif // GC_CONFIG_DRIVEN
#ifdef HEAP_ANALYZE
    gcDacVars->internal_root_array = &gc_heap::internal_root_array;
    gcDacVars->internal_root_array_index = &gc_heap::internal_root_array_index;
    gcDacVars->heap_analyze_success = &gc_heap::heap_analyze_success;
#endif // HEAP_ANALYZE
#else
    gcDacVars->n_heaps = &gc_heap::n_heaps;
    gcDacVars->g_heaps = reinterpret_cast<unused_gc_heap***>(&gc_heap::g_heaps);
    gcDacVars->gc_heap_field_offsets = reinterpret_cast<int**>(&gc_heap_field_offsets);
#endif // MULTIPLE_HEAPS
    gcDacVars->generation_field_offsets = reinterpret_cast<int**>(&generation_field_offsets);
    if (v2)
    {
        gcDacVars->bookkeeping_start = &gc_heap::bookkeeping_start;
    }
    if (v4)
    {
#ifdef DYNAMIC_HEAP_COUNT
        gcDacVars->dynamic_adaptation_mode = &gc_heap::dynamic_adaptation_mode;
#else
        gcDacVars->dynamic_adaptation_mode = nullptr;
#endif //DYNAMIC_HEAP_COUNT
    }
    if (v6)
    {
#ifdef GC_DESCRIPTOR
#ifdef MULTIPLE_HEAPS
        gcDacVars->gc_descriptor = (void*)&GCContractDescriptorSVR;
#else // MULTIPLE_HEAPS
        gcDacVars->gc_descriptor = (void*)&GCContractDescriptorWKS;
#endif // MULTIPLE_HEAPS
#endif // GC_DESCRIPTOR
    }
    if (v8)
    {
#ifdef MULTIPLE_HEAPS
        gcDacVars->g_totalCpuCount = &::g_totalCpuCount;
#endif // MULTIPLE_HEAPS
    }
}

inline
BOOL gc_heap::ephemeral_pointer_p (uint8_t* o)
{
#ifdef USE_REGIONS
    int gen_num = object_gennum ((uint8_t*)o);
    assert (gen_num >= 0);
    return (gen_num < max_generation);
#else
    return ((o >= ephemeral_low) && (o < ephemeral_high));
#endif //USE_REGIONS
}

// This needs to check the range that's covered by bookkeeping because find_object will
// need to look at the brick table.
inline
bool gc_heap::is_in_find_object_range (uint8_t* o)
{
    if (o == nullptr)
    {
        return false;
    }
#if defined(USE_REGIONS) && defined(FEATURE_CONSERVATIVE_GC)
    return ((o >= g_gc_lowest_address) && (o < bookkeeping_covered_committed));
#else //USE_REGIONS && FEATURE_CONSERVATIVE_GC
    if ((o >= g_gc_lowest_address) && (o < g_gc_highest_address))
    {
#ifdef USE_REGIONS
        assert ((o >= g_gc_lowest_address) && (o < bookkeeping_covered_committed));
#endif //USE_REGIONS
        return true;
    }
    else
    {
        return false;
    }
#endif //USE_REGIONS && FEATURE_CONSERVATIVE_GC
}

#ifdef USE_REGIONS

// This assumes o is guaranteed to be in a region.
inline
bool gc_heap::is_in_condemned_gc (uint8_t* o)
{
    assert ((o >= g_gc_lowest_address) && (o < g_gc_highest_address));

    int condemned_gen = settings.condemned_generation;
    if (condemned_gen < max_generation)
    {
        int gen = get_region_gen_num (o);
        if (gen > condemned_gen)
        {
            return false;
        }
    }

    return true;
}

#endif //USE_REGIONS


#if defined (_MSC_VER) && defined (TARGET_X86)
#pragma optimize("y", on)        // Small critical routines, don't put in EBP frame
#endif //_MSC_VER && TARGET_X86

// return the generation number of an object.
// It is assumed that the object is valid.
// Note that this will return max_generation for UOH objects
int gc_heap::object_gennum (uint8_t* o)
{
#ifdef USE_REGIONS
    return get_region_gen_num (o);
#else
    if (in_range_for_segment (o, ephemeral_heap_segment) &&
        (o >= generation_allocation_start (generation_of (max_generation - 1))))
    {
        // in an ephemeral generation.
        for ( int i = 0; i < max_generation-1; i++)
        {
            if ((o >= generation_allocation_start (generation_of (i))))
                return i;
        }
        return max_generation-1;
    }
    else
    {
        return max_generation;
    }
#endif //USE_REGIONS
}

int gc_heap::object_gennum_plan (uint8_t* o)
{
#ifdef USE_REGIONS
    return get_region_plan_gen_num (o);
#else
    if (in_range_for_segment (o, ephemeral_heap_segment))
    {
        for (int i = 0; i < ephemeral_generation_count; i++)
        {
            uint8_t* plan_start = generation_plan_allocation_start (generation_of (i));
            if (plan_start && (o >= plan_start))
            {
                return i;
            }
        }
    }
    return max_generation;
#endif //USE_REGIONS
}

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("", on)        // Go back to command line default optimizations
#endif //_MSC_VER && TARGET_X86

heap_segment* gc_heap::find_segment (uint8_t* interior, BOOL small_segment_only_p)
{
    heap_segment* seg = seg_mapping_table_segment_of (interior);
    if (seg)
    {
        if (small_segment_only_p && heap_segment_uoh_p (seg))
            return 0;
    }
    return seg;
}

#ifdef MULTIPLE_HEAPS
gc_heap* seg_mapping_table_heap_of (uint8_t* o)
{
    if ((o < g_gc_lowest_address) || (o >= g_gc_highest_address))
        return 0;

    return seg_mapping_table_heap_of_worker (o);
}
#endif //MULTIPLE_HEAPS

#ifdef MULTIPLE_HEAPS
gc_heap* seg_mapping_table_heap_of_gc (uint8_t* o)
{
#ifdef FEATURE_BASICFREEZE
    if ((o < g_gc_lowest_address) || (o >= g_gc_highest_address))
        return 0;
#endif //FEATURE_BASICFREEZE

    return seg_mapping_table_heap_of_worker (o);
}
#endif //MULTIPLE_HEAPS

#if !defined(_DEBUG) && !defined(__GNUC__)
inline // This causes link errors if global optimization is off
#endif //!_DEBUG && !__GNUC__
gc_heap* gc_heap::heap_of (uint8_t* o)
{
#ifdef MULTIPLE_HEAPS
    if (o == 0)
        return g_heaps [0];
    gc_heap* hp = seg_mapping_table_heap_of (o);
    return (hp ? hp : g_heaps[0]);
#else //MULTIPLE_HEAPS
    UNREFERENCED_PARAMETER(o);
    return __this;
#endif //MULTIPLE_HEAPS
}

inline
gc_heap* gc_heap::heap_of_gc (uint8_t* o)
{
#ifdef MULTIPLE_HEAPS
    if (o == 0)
        return g_heaps [0];
    gc_heap* hp = seg_mapping_table_heap_of_gc (o);
    return (hp ? hp : g_heaps[0]);
#else //MULTIPLE_HEAPS
    UNREFERENCED_PARAMETER(o);
    return __this;
#endif //MULTIPLE_HEAPS
}

// will find all heap objects (large and small)
//
// Callers of this method need to guarantee the interior pointer is within the heap range.
//
// If you need it to be stricter, eg if you only want to find an object in ephemeral range,
// you should make sure interior is within that range before calling this method.
uint8_t* gc_heap::find_object (uint8_t* interior)
{
    assert (interior != 0);

    if (!gen0_bricks_cleared)
    {
#ifdef MULTIPLE_HEAPS
        assert (!"Should have already been done in server GC");
#endif //MULTIPLE_HEAPS
        clear_gen0_bricks();
    }
    //indicate that in the future this needs to be done during allocation
    gen0_must_clear_bricks = FFIND_DECAY;

    int brick_entry = get_brick_entry(brick_of (interior));
    if (brick_entry == 0)
    {
        // this is a pointer to a UOH object
        heap_segment* seg = find_segment (interior, FALSE);
        if (seg)
        {
#ifdef FEATURE_CONSERVATIVE_GC
            if (interior >= heap_segment_allocated(seg))
                return 0;
#endif
            // If interior falls within the first free object at the beginning of a generation,
            // we don't have brick entry for it, and we may incorrectly treat it as on large object heap.
            int align_const = get_alignment_constant (heap_segment_read_only_p (seg)
#ifdef FEATURE_CONSERVATIVE_GC
                                                       || (GCConfig::GetConservativeGC() && !heap_segment_uoh_p (seg))
#endif
                                                      );
            assert (interior < heap_segment_allocated (seg));

            uint8_t* o = heap_segment_mem (seg);
            while (o < heap_segment_allocated (seg))
            {
                uint8_t* next_o = o + Align (size (o), align_const);
                assert (next_o > o);
                if ((o <= interior) && (interior < next_o))
                    return o;
                o = next_o;
            }
            return 0;
        }
        else
        {
            return 0;
        }
    }
    else
    {
        heap_segment* seg = find_segment (interior, TRUE);
        if (seg)
        {
#ifdef FEATURE_CONSERVATIVE_GC
            if (interior >= heap_segment_allocated (seg))
                return 0;
#else
            assert (interior < heap_segment_allocated (seg));
#endif
            uint8_t* o = find_first_object (interior, heap_segment_mem (seg));
            return o;
        }
        else
            return 0;
    }
}

size_t gc_heap::get_generation_start_size (int gen_number)
{
#ifdef USE_REGIONS
    return 0;
#else
    return Align (size (generation_allocation_start (generation_of (gen_number))),
                  get_alignment_constant (gen_number <= max_generation));
#endif //!USE_REGIONS
}

inline
int gc_heap::get_num_heaps()
{
#ifdef MULTIPLE_HEAPS
    return n_heaps;
#else
    return 1;
#endif //MULTIPLE_HEAPS
}


void stomp_write_barrier_resize(bool is_runtime_suspended, bool requires_upper_bounds_check)
{
    WriteBarrierParameters args = {};
    args.operation = WriteBarrierOp::StompResize;
    args.is_runtime_suspended = is_runtime_suspended;
    args.requires_upper_bounds_check = requires_upper_bounds_check;

    args.card_table = g_gc_card_table;
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    args.card_bundle_table = g_gc_card_bundle_table;
#endif

    args.lowest_address = g_gc_lowest_address;
    args.highest_address = g_gc_highest_address;

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (SoftwareWriteWatch::IsEnabledForGCHeap())
    {
        args.write_watch_table = g_gc_sw_ww_table;
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    GCToEEInterface::StompWriteBarrier(&args);
}

// Category-specific gc_heap method files
#include "region_allocator.cpp"
#include "region_free_list.cpp"
#include "finalization.cpp"
#include "interface.cpp"
#include "allocation.cpp"
#include "mark_phase.cpp"
#include "plan_phase.cpp"
#include "relocate_compact.cpp"
#include "sweep.cpp"
#include "background.cpp"
#include "regions_segments.cpp"
#include "card_table.cpp"
#include "memory.cpp"
#include "diagnostics.cpp"
#include "dynamic_tuning.cpp"
#include "no_gc.cpp"
#include "dynamic_heap_count.cpp"
#include "init.cpp"
#include "collect.cpp"

}
