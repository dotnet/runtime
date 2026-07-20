// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef GC_INTERNAL_H
#define GC_INTERNAL_H

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

#define MAX_PTR ((uint8_t*)(~(ptrdiff_t)0))
#define MAX_YP_SPIN_COUNT_UNIT 32768
#define MIN_SOH_CROSS_GEN_REFS (400)
#define MIN_LOH_CROSS_GEN_REFS (800)

#ifdef SERVER_GC
#define partial_size_th 100
#define num_partial_refs 64
#else //SERVER_GC
#define partial_size_th 100
#define num_partial_refs 32
#endif //SERVER_GC

#ifdef HOST_64BIT
#define MARK_STACK_INITIAL_LENGTH 1024
#else
#define MARK_STACK_INITIAL_LENGTH 128
#endif // HOST_64BIT

extern uint32_t yp_spin_count_unit;
extern uint32_t original_spin_count_unit;

#ifdef GC_CONFIG_DRIVEN
extern int compact_ratio;
#define gc_config_log_buffer_size (1*1024) // TEMP
extern FILE* gc_config_log;
extern uint8_t* gc_config_log_buffer;
#endif //GC_CONFIG_DRIVEN

#ifdef WRITE_WATCH
extern uint8_t* g_addresses [array_size+2];
#endif //WRITE_WATCH

#define lowbits(wrd, bits) ((wrd) & ((1 << (bits))-1))
#define highbits(wrd, bits) ((wrd) & ~((1 << (bits))-1))

#if defined(BACKGROUND_GC) && defined(FEATURE_EVENT_TRACE)
extern BOOL bgc_heap_walk_for_etw_p;
#endif //BACKGROUND_GC && FEATURE_EVENT_TRACE

extern const char* const str_root_kinds[];

#ifdef MH_SC_MARK
extern const int max_snoop_level;
#endif //MH_SC_MARK

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

#ifdef MULTIPLE_HEAPS
extern uint32_t g_num_active_processors;

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
    uint64_t start[MAX_SUPPORTED_HEAPS], end[MAX_SUPPORTED_HEAPS], start_seq;
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

extern t_join gc_t_join;
#ifdef BACKGROUND_GC
extern t_join bgc_t_join;
#endif //BACKGROUND_GC
#endif //MULTIPLE_HEAPS

inline
void c_write (uint32_t& place, uint32_t value)
{
    Interlocked::Exchange (&place, value);
}

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

extern float bgc_uoh_inc_ratio_alloc_normal;
extern float bgc_uoh_inc_ratio_alloc_wait;

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

#ifdef FEATURE_STRUCTALIGN
BOOL IsStructAligned (uint8_t *ptr, int requiredAlignment);
#endif // FEATURE_STRUCTALIGN

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

#define free_object_base_size (plug_skew + sizeof(ArrayBase))

#define free_list_slot(x) ((uint8_t**)(x))[2]
#define free_list_undo(x) ((uint8_t**)(x))[-1]
#define UNDO_EMPTY ((uint8_t*)1)

#ifdef DOUBLY_LINKED_FL
#define free_list_prev(x) ((uint8_t**)(x))[3]
#define PREV_EMPTY ((uint8_t*)1)

inline
void check_and_clear_in_free_list (uint8_t* o, size_t size)
{
    if (size >= min_free_list)
    {
        free_list_prev (o) = PREV_EMPTY;
    }
}

#endif //DOUBLY_LINKED_FL

typedef void** PTR_PTR;

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
            _ASSERTE(!g_theGCHeap->IsLargeObject(this) || g_theGCHeap->IsInFrozenSegment(this));
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

inline size_t my_get_size (Object* ob)
{
    MethodTable* mT = header(ob)->GetMethodTable();

    return (mT->GetBaseSize() +
            (mT->HasComponentSize() ?
             ((size_t)((CObjectHeader*)ob)->GetNumComponents() * mT->RawGetComponentSize()) : 0));
}

#define size(i) my_get_size (header(i))
#define marked(i) header(i)->IsMarked()
#define set_marked(i) header(i)->SetMarked()
#define clear_marked(i) header(i)->ClearMarked()
#define pinned(i) header(i)->IsPinned()
#define set_pinned(i) header(i)->SetPinned()
#define clear_pinned(i) header(i)->GetHeader()->ClrGCBit();

inline size_t unused_array_size(uint8_t * p)
{
    assert(((CObjectHeader*)p)->IsFree());

    size_t* numComponentsPtr = (size_t*)(p + ArrayBase::GetOffsetOfNumComponents());
    return free_object_base_size + *numComponentsPtr;
}

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

//CLR_SIZE  is the max amount of bytes from gen0 that is set to 0 in one chunk
#define CLR_SIZE ((size_t)(8*1024+32))

#define DECOMMIT_SIZE_PER_MILLISECOND (160*1024)

#ifndef MULTIPLE_HEAPS
extern const int n_heaps;
#endif //MULTIPLE_HEAPS

#ifdef MULTIPLE_HEAPS
extern bool affinity_config_specified_p;
#if defined(TARGET_AMD64) && !(defined(_MSC_VER) || defined(__GNUC__))
extern "C" ptrdiff_t get_cycle_count(void);
#else
ptrdiff_t get_cycle_count();
#endif

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

    static uint16_t *proc_no_to_heap_no;
    static uint16_t heap_no_to_proc_no[MAX_SUPPORTED_HEAPS];
    static uint16_t heap_no_to_numa_node[MAX_SUPPORTED_HEAPS];
    static uint16_t *numa_node_to_heap_map;

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

        uint32_t maxCpuCount = GCToOSInterface::GetMaxProcessorCount();
        proc_no_to_heap_no = new (nothrow) uint16_t[maxCpuCount];
        if (proc_no_to_heap_no == NULL)
        {
            return FALSE;
        }

        memset(proc_no_to_heap_no, 0, maxCpuCount*sizeof(uint16_t));

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
        uint16_t proc_no[MAX_SUPPORTED_HEAPS];
        uint16_t node_no[MAX_SUPPORTED_HEAPS];
        uint16_t max_node_no = 0;
        int heap_num;
        for (heap_num = 0; heap_num < n_heaps; heap_num++)
        {
            if (!GCToOSInterface::GetProcessorForHeap (static_cast<uint16_t>(heap_num), &proc_no[heap_num], &node_no[heap_num]))
                break;
            assert(proc_no[heap_num] < GCToOSInterface::GetMaxProcessorCount());
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
            proc_no_to_heap_no[proc_no] = (uint16_t)heap_number;
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
            int adjusted_heap = proc_no_to_heap_no[proc_no];
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

    static bool init_numa_node_to_heap_map(int nheaps)
    {
        // Called right after GCHeap::Init() for each heap

        uint32_t maxCpuCount = GCToOSInterface::GetMaxProcessorCount();
        // The upper limit of the numa node numbers is maxCpuCount - 1 since in the worst case each processor could be on a different NUMA node. 
        // We add +1 here to make it easier to calculate the heap number range for the last NUMA node.
        numa_node_to_heap_map = new (nothrow) uint16_t[maxCpuCount + 1];
        if (numa_node_to_heap_map == nullptr)
        {
            return false;
        }

        memset(numa_node_to_heap_map, 0, (maxCpuCount + 1)*sizeof(uint16_t));

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
                assert(heap_no_to_numa_node[i-1] < maxCpuCount);
                assert(heap_no_to_numa_node[i] < maxCpuCount);
                // Set the end of the heap number range for the previous NUMA node
                numa_node_to_heap_map[heap_no_to_numa_node[i-1] + 1] =
                // Set the start of the heap number range for the current NUMA node
                numa_node_to_heap_map[heap_no_to_numa_node[i]] = (uint16_t)i;
            }
#ifdef HEAP_BALANCE_INSTRUMENTATION
            (heaps_on_node[total_numa_nodes].heap_count)++;
#endif
        }

        assert(heap_no_to_numa_node[nheaps-1] < maxCpuCount);
        // Set the end of the heap range for the last NUMA node
        numa_node_to_heap_map[heap_no_to_numa_node[nheaps-1] + 1] = (uint16_t)nheaps; //mark the end with nheaps

#ifdef HEAP_BALANCE_INSTRUMENTATION
        total_numa_nodes++;
#endif
        return true;
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
            uint16_t current_heap_no_on_node[MAX_SUPPORTED_HEAPS];
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

// We don't store seg_mapping_table in card_table_info because there's only always one view.
extern seg_mapping* seg_mapping_table;

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

#ifdef WRITE_WATCH
#ifndef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
extern bool virtual_alloc_hardware_write_watch;
#endif // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
extern bool hardware_write_watch_capability;

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
#endif //WRITE_WATCH

inline
size_t gib (size_t num)
{
    return (num / 1024 / 1024 / 1024);
}

#ifdef HOST_64BIT
#define brick_size ((size_t)4096)
#else
#define brick_size ((size_t)2048)
#endif //HOST_64BIT

inline
uint8_t* align_on_brick (uint8_t* add)
{
    return (uint8_t*)((size_t)(add + brick_size - 1) & ~(brick_size - 1));
}

#ifdef CARD_BUNDLE
//threshold of heap size to turn on card bundles.
#define SH_TH_CARD_BUNDLE  (40*1024*1024)
#define MH_TH_CARD_BUNDLE  (180*1024*1024)
#endif //CARD_BUNDLE

// time in milliseconds between decommit steps
#define DECOMMIT_TIME_STEP_MILLISECONDS (100)

#if defined(HOST_64BIT)
#define MAX_ALLOWED_MEM_LOAD 85
#define MIN_YOUNGEST_GEN_DESIRED (16*1024*1024)
#endif //HOST_64BIT

extern const size_t min_segment_size_hard_limit;
extern const size_t low_latency_alloc;
extern gc_reason gc_trigger_reason;
extern double qpf_us;

uint64_t RawGetHighPrecisionTimeStamp();

#ifdef WRITE_WATCH
#ifdef BACKGROUND_GC
extern const size_t ww_reset_quantum;
#endif //BACKGROUND_GC

inline
size_t align_write_watch_lower_page (size_t add)
{
    return (add & ~(WRITE_WATCH_UNIT_SIZE - 1));
}

inline
uint8_t* align_write_watch_lower_page (uint8_t* add)
{
    return (uint8_t*)((size_t)add & ~((size_t)OS_PAGE_SIZE - 1));
}
#endif //WRITE_WATCH

void stomp_write_barrier_ephemeral (uint8_t* ephemeral_low, uint8_t* ephemeral_high
#ifdef USE_REGIONS
                                     , gc_heap::region_info* map_region_to_generation_skewed
                                    , uint8_t region_shr
#endif //USE_REGIONS
                                    );

void process_sync_log_stats();
void* virtual_alloc (size_t size);
void* virtual_alloc (size_t size, bool use_large_pages_p, uint16_t numa_node = NUMA_NODE_UNDEFINED);
size_t get_valid_segment_size (BOOL large_seg = FALSE);
heap_segment* ro_segment_lookup (uint8_t* o);
heap_segment* heap_segment_rw (heap_segment* ns);
heap_segment* heap_segment_next_rw (heap_segment* seg);
heap_segment* heap_segment_prev_rw (heap_segment* begin, heap_segment* seg);
void enter_spin_lock (GCSpinLock* spin_lock);

inline
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

inline
heap_segment* heap_segment_next_in_range (heap_segment* seg)
{
    heap_segment* ns = heap_segment_next (seg);
    return heap_segment_in_range (ns);
}

inline
BOOL in_range_for_segment (uint8_t* add, heap_segment* seg)
{
    return ((add >= heap_segment_mem (seg)) && (add < heap_segment_reserved (seg)));
}

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

inline
uint8_t* align_lower_segment (uint8_t* add)
{
    return (uint8_t*)((size_t)(add) & ~(((size_t)1 << gc_heap::min_segment_size_shr) - 1));
}

#ifdef CARD_BUNDLE
// The card bundle keeps track of groups of card words.
static const size_t card_bundle_word_width = 32;

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

size_t align_cardw_on_bundle (size_t cardw);
size_t cardw_card_bundle (size_t cardw);
size_t card_bundle_cardw (size_t cardb);
uint32_t* translate_card_bundle_table (uint32_t* cb, uint8_t* lowest_address);
#endif //CARD_BUNDLE

inline
uint8_t* align_lower_brick (uint8_t* add)
{
    return (uint8_t*)(((size_t)add) & ~(brick_size - 1));
}

size_t size_brick_of (uint8_t* from, uint8_t* end);

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

size_t gcard_of (uint8_t*);
size_t count_card_of (uint8_t* from, uint8_t* end);
size_t size_card_of (uint8_t* from, uint8_t* end);
size_t size_seg_mapping_table_of (uint8_t* from, uint8_t* end);
heap_segment* seg_mapping_table_segment_of (uint8_t* o);
#ifdef MULTIPLE_HEAPS
gc_heap* seg_mapping_table_heap_of (uint8_t* o);
#endif //MULTIPLE_HEAPS

inline
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
size_t seg_mapping_word_of (uint8_t* add)
{
    return (size_t)add >> gc_heap::min_segment_size_shr;
}

inline
unsigned& card_table_refcount (uint32_t* c_table)
{
    return *(unsigned*)((char*)c_table - sizeof (card_table_info));
}

uint32_t* translate_card_table (uint32_t* ct);
void own_card_table (uint32_t* c_table);
void release_card_table (uint32_t* c_table);
void stomp_write_barrier_resize(bool is_runtime_suspended, bool requires_upper_bounds_check);

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

size_t size_mark_array_of (uint8_t* from, uint8_t* end);
uint32_t* translate_mark_array (uint32_t* ma);
#endif //BACKGROUND_GC

inline
BOOL grow_mark_stack (mark*& m, size_t& len, size_t init_len)
{
    size_t new_size = max (init_len, 2 * len);
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

enum
{
    CORINFO_EXCEPTION_GC = 0xE0004743 // 'GC'
};

#define mark_stack_empty_p() (mark_stack_base == mark_stack_tos)

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

#define stolen 2
#define partial 1
#define partial_object 3

inline
BOOL stolen_p (uint8_t* r)
{
    return (((size_t)r & 2) && !((size_t)r & 1));
}

inline
BOOL partial_p (uint8_t* r)
{
    return (((size_t)r & 1) && !((size_t)r & 2));
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
extern VOLATILE(BOOL) s_fUnpromotedHandles;
extern VOLATILE(BOOL) s_fUnscannedPromotions;
extern VOLATILE(BOOL) s_fScanRequired;
#endif //MULTIPLE_HEAPS

uint8_t** make_mark_list (size_t size);

#ifdef USE_VXSORT
void do_vxsort (uint8_t** item_array, ptrdiff_t item_count, uint8_t* range_low, uint8_t* range_high);
#endif //USE_VXSORT

uint8_t* compute_next_end (heap_segment* seg, uint8_t* low);

inline
size_t& card_table_size (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->size;
}

#ifdef USE_REGIONS
extern region_allocator global_region_allocator;
extern uint8_t*(*initial_regions)[total_generation_count][2];
extern const size_t uninitialized_end_gen0_region_space;
size_t size_region_to_generation_table_of (uint8_t* from, uint8_t* end);

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

#ifdef DYNAMIC_HEAP_COUNT
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
#endif //DYNAMIC_HEAP_COUNT

inline bool is_in_heap_range (uint8_t* o)
{
    assert (((g_gc_lowest_address <= o) && (o < g_gc_highest_address)) ||
        (o == nullptr) || (ro_segment_lookup (o) != nullptr));
    return ((g_gc_lowest_address <= o) && (o < g_gc_highest_address));
}

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

inline
heap_segment* get_region_info (uint8_t* region_start)
{
    size_t region_index = (size_t)region_start >> gc_heap::min_segment_size_shr;
    heap_segment* region_info_entry = (heap_segment*)&seg_mapping_table[region_index];
    dprintf (REGIONS_LOG, ("region info for region %p is at %zd, %zx (alloc: %p)",
        region_start, region_index, (size_t)region_info_entry, heap_segment_allocated (region_info_entry)));
    return (heap_segment*)&seg_mapping_table[region_index];
}

inline
bool is_free_region (heap_segment* region)
{
    return (heap_segment_allocated (region) == nullptr);
}

inline
void gc_heap::set_region_plan_gen_num (heap_segment* region, int plan_gen_num, bool replace_p)
{
    int gen_num = heap_segment_gen_num (region);
    int supposed_plan_gen_num = get_plan_gen_num (gen_num);
    dprintf (REGIONS_LOG, ("h%d setting plan gen on %p->%p(was gen%d) to %d(should be: %d) %s",
        heap_number, region,
        heap_segment_mem (region),
        gen_num, plan_gen_num,
        supposed_plan_gen_num,
        ((plan_gen_num < supposed_plan_gen_num) ? "DEMOTED" : "ND")));
    region_info region_info_bits_to_set = (region_info)(plan_gen_num << RI_PLAN_GEN_SHR);
    if ((plan_gen_num < supposed_plan_gen_num) && (heap_segment_pinned_survived (region) != 0))
    {
        if (!settings.demotion)
        {
            settings.demotion = TRUE;
        }
        get_gc_data_per_heap()->set_mechanism_bit (gc_demotion_bit);
        region->flags |= heap_segment_flags_demoted;
        region_info_bits_to_set = (region_info)(region_info_bits_to_set | RI_DEMOTED);
    }
    else
    {
        region->flags &= ~heap_segment_flags_demoted;
    }

    if (replace_p)
    {
        int original_plan_gen_num = heap_segment_plan_gen_num (region);
        planned_regions_per_gen[original_plan_gen_num]--;
    }

    planned_regions_per_gen[plan_gen_num]++;
    dprintf (REGIONS_LOG, ("h%d g%d %zx(%zx) -> g%d (total %d region planned in g%d)",
        heap_number, heap_segment_gen_num (region), (size_t)region, heap_segment_mem (region), plan_gen_num, planned_regions_per_gen[plan_gen_num], plan_gen_num));

    heap_segment_plan_gen_num (region) = plan_gen_num;

    uint8_t* region_start = get_region_start (region);
    uint8_t* region_end = heap_segment_reserved (region);

    size_t region_index_start = get_basic_region_index_for_address (region_start);
    size_t region_index_end = get_basic_region_index_for_address (region_end);
    for (size_t region_index = region_index_start; region_index < region_index_end; region_index++)
    {
        assert (plan_gen_num <= max_generation);
        map_region_to_generation[region_index] = (region_info)(region_info_bits_to_set | (map_region_to_generation[region_index] & ~(RI_PLAN_GEN_MASK|RI_DEMOTED)));
    }
}

inline
void gc_heap::set_region_plan_gen_num_sip (heap_segment* region, int plan_gen_num)
{
    if (!heap_segment_swept_in_plan (region))
    {
        set_region_plan_gen_num (region, plan_gen_num);
    }
}
#endif //USE_REGIONS

extern const int32_t lock_free;
extern const int32_t lock_taken;
extern const int32_t lock_decommissioned;

#define demotion_plug_len_th (6*1024*1024)
#define LOH_PIN_QUEUE_LENGTH 100
#define LOH_PIN_DECAY 10

#ifdef USE_REGIONS
#define sip_surv_ratio_th (90)
#define sip_old_card_surv_ratio_th (90)
#endif //USE_REGIONS

static void enter_spin_lock_noinstru (RAW_KEYWORD(volatile) int32_t* lock);

#ifdef _DEBUG
#define ASSERT_HOLDING_SPIN_LOCK(pSpinLock) \
    _ASSERTE((pSpinLock)->holding_thread == GCToEEInterface::GetThread());

inline
BOOL try_enter_spin_lock(GCSpinLock* pSpinLock)
{
    BOOL ret = (Interlocked::CompareExchange(&pSpinLock->lock, 0, -1) == -1);
    if (ret)
    {
        pSpinLock->holding_thread = GCToEEInterface::GetThread();
    }

    return ret;
}
#else // _DEBUG
#define ASSERT_HOLDING_SPIN_LOCK(pSpinLock)

inline
BOOL try_enter_spin_lock(GCSpinLock* spin_lock)
{
    return (Interlocked::CompareExchange(&spin_lock->lock, 0, -1) == -1);
}
#endif // _DEBUG

inline
enter_msl_status gc_heap::enter_spin_lock_msl (GCSpinLock* msl)
{
    if (Interlocked::CompareExchange (&msl->lock, lock_taken, lock_free) == lock_free)
        return msl_entered;

    return enter_spin_lock_msl_helper (msl);
}

#ifdef _DEBUG
inline
void enter_spin_lock (GCSpinLock *pSpinLock)
{
    enter_spin_lock_noinstru (&pSpinLock->lock);
    assert (pSpinLock->holding_thread == (Thread*)-1);
    pSpinLock->holding_thread = GCToEEInterface::GetThread();
}
#else //_DEBUG
inline
void WaitLonger (int i
#ifdef SYNCHRONIZATION_STATS
    , GCSpinLock* spin_lock
#endif //SYNCHRONIZATION_STATS
    )
{
#ifdef SYNCHRONIZATION_STATS
    (spin_lock->num_wait_longer)++;
#endif //SYNCHRONIZATION_STATS

    bool bToggleGC = GCToEEInterface::EnablePreemptiveGC();
    assert (bToggleGC);

    if (!gc_heap::gc_started)
    {
#ifdef SYNCHRONIZATION_STATS
        (spin_lock->num_switch_thread_w)++;
#endif //SYNCHRONIZATION_STATS
        if  (g_num_processors > 1)
        {
            YieldProcessor();
            if  (i & 0x01f)
                GCToOSInterface::YieldThread (0);
            else
                GCToOSInterface::Sleep (5);
        }
        else
            GCToOSInterface::Sleep (5);
    }

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
void enter_spin_lock (GCSpinLock* spin_lock)
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
                        YieldProcessor();
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
#endif //_DEBUG

inline
void leave_spin_lock(GCSpinLock *pSpinLock)
{
#ifdef _DEBUG
    bool gc_thread_p = GCToEEInterface::WasCurrentThreadCreatedByGC();
    pSpinLock->released_by_gc_p = gc_thread_p;
    pSpinLock->holding_thread = (Thread*) -1;
#endif // _DEBUG
    if (pSpinLock->lock != -1)
        VolatileStore<int32_t>((int32_t*)&pSpinLock->lock, -1);
}

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

#define END_SPACE_AFTER_GC (loh_size_threshold + MAX_STRUCTALIGN)
// When we fit into the free list we need an extra of a min obj
#define END_SPACE_AFTER_GC_FL (END_SPACE_AFTER_GC + Align (min_obj_size))

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
    DWORD highest_set_bit_index;
    if (0 ==
#ifdef HOST_64BIT
        BitScanReverse64(
#else
        BitScanReverse(
#endif
            &highest_set_bit_index, size)) { return 0; }

    return static_cast<size_t>(1) << highest_set_bit_index;
}

extern size_t loh_size_threshold;

inline
float mb (size_t num)
{
    return (float)((float)num / 1000.0 / 1000.0);
}

inline
uint32_t limit_time_to_uint32 (uint64_t time)
{
    time = min (time, (uint64_t)UINT32_MAX);
    return (uint32_t)time;
}

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
void memclr (uint8_t* mem, size_t size)
{
    dprintf (3, ("MEMCLR: %p, %zd", mem, size));
    assert ((size & (sizeof(PTR_PTR) - 1)) == 0);
    assert (sizeof(PTR_PTR) == DATA_ALIGNMENT);
    memset (mem, 0, size);
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

#ifdef HOST_64BIT
#define mark_bit_pitch ((size_t)16)
#else
#define mark_bit_pitch ((size_t)8)
#endif //HOST_64BIT
#define mark_word_width ((size_t)32)
#define mark_word_size (mark_word_width * mark_bit_pitch)

inline
uint8_t* align_on_mark_word (uint8_t* add)
{
    return (uint8_t*)((size_t)(add + mark_word_size - 1) & ~(mark_word_size - 1));
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
size_t mark_bit_bit_of (uint8_t* add)
{
    return (((size_t)add / mark_bit_pitch) % mark_word_width);
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

inline
uint8_t* mark_word_address (size_t wd)
{
    return (uint8_t*)(wd * mark_word_size);
}

#ifdef BACKGROUND_GC
inline
size_t& gc_heap::bpromoted_bytes (int thread)
{
#ifdef MULTIPLE_HEAPS
    return g_bpromoted [thread * 16];
#else //MULTIPLE_HEAPS
    UNREFERENCED_PARAMETER(thread);
    return g_bpromoted;
#endif //MULTIPLE_HEAPS
}

inline
unsigned int gc_heap::mark_array_marked (uint8_t* add)
{
    return mark_array [mark_word_of (add)] & (1 << mark_bit_bit_of (add));
}

inline
void gc_heap::mark_array_set_marked (uint8_t* add)
{
    size_t index = mark_word_of (add);
    uint32_t val = (1 << mark_bit_bit_of (add));
#ifdef MULTIPLE_HEAPS
    Interlocked::Or (&(mark_array [index]), val);
#else
    mark_array [index] |= val;
#endif
}

inline
void gc_heap::mark_array_clear_marked (uint8_t* add)
{
    mark_array [mark_word_of (add)] &= ~(1 << mark_bit_bit_of (add));
}

inline
void gc_heap::seg_clear_mark_array_bits_soh (heap_segment* seg)
{
    uint8_t* range_beg = 0;
    uint8_t* range_end = 0;
    if (bgc_mark_array_range (seg, FALSE, &range_beg, &range_end))
    {
        clear_mark_array (range_beg, align_on_mark_word (range_end));
    }
}
#endif //BACKGROUND_GC

inline
BOOL gc_heap::is_mark_set (uint8_t* o)
{
    return marked (o);
}

inline
void gc_heap::pin_object (uint8_t* o, uint8_t** ppObject)
{
    dprintf (3, ("Pinning %zx->%zx", (size_t)ppObject, (size_t)o));
    set_pinned (o);

#ifdef FEATURE_EVENT_TRACE
    if (EVENT_ENABLED(PinObjectAtGCTime))
    {
        fire_etw_pin_object_event (o, ppObject);
    }
#endif // FEATURE_EVENT_TRACE

    num_pinned_objects++;
}

#define contain_pointers(i) header(i)->ContainsGCPointers()
#ifdef COLLECTIBLE_CLASS
#define contain_pointers_or_collectible(i) header(i)->ContainsGCPointersOrCollectible()
#define get_class_object(i) GCToEEInterface::GetLoaderAllocatorObjectForGC((Object *)i)
#define is_collectible(i) method_table(i)->Collectible()
#else //COLLECTIBLE_CLASS
#define contain_pointers_or_collectible(i) header(i)->ContainsGCPointers()
#endif //COLLECTIBLE_CLASS

inline
uint8_t*& card_table_lowest_address (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->lowest_address;
}

inline
uint8_t*& card_table_highest_address (uint32_t* c_table)
{
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->highest_address;
}

inline
uint32_t*& card_table_next (uint32_t* c_table)
{
    // NOTE: The dac takes a dependency on card_table_info being right before c_table.
    return ((card_table_info*)((uint8_t*)c_table - sizeof (card_table_info)))->next_card_table;
}

void destroy_card_table (uint32_t* c_table);

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
            ptrdiff_t cs = mt->RawGetComponentSize();                       \
            parm = (uint8_t**)((uint8_t*)parm + (((start) - (uint8_t*)parm)/cs)*cs); \
        }                                                                   \
        while ((uint8_t*)parm < ((o)+(size)-plug_skew))                     \
        {                                                                   \
            for (ptrdiff_t __i = 0; __i > cnt; __i--)                       \
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

#ifndef COLLECTIBLE_CLASS
#define go_through_object_cl(mt,o,size,parm,exp)                            \
{                                                                           \
    if (header(o)->ContainsGCPointers())                                    \
    {                                                                       \
        go_through_object_nostart(mt,o,size,parm,exp);                      \
    }                                                                       \
}
#else //COLLECTIBLE_CLASS
#define go_through_object_cl(mt,o,size,parm,exp)                            \
{                                                                           \
    if (header(o)->Collectible())                                           \
    {                                                                       \
        uint8_t* class_obj = get_class_object (o);                          \
        uint8_t** parm = &class_obj;                                        \
        do {exp} while (false);                                             \
    }                                                                       \
    if (header(o)->ContainsGCPointers())                                    \
    {                                                                       \
        go_through_object_nostart(mt,o,size,parm,exp);                      \
    }                                                                       \
}
#endif //COLLECTIBLE_CLASS

inline BOOL
gc_heap::dt_high_memory_load_p()
{
    return ((settings.entry_memory_load >= high_memory_load_th) || g_low_memory_status);
}

#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM64) || defined(TARGET_RISCV64)
#ifndef PREFETCH
#define PREFETCH
#endif
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

#ifdef BACKGROUND_GC
inline
void gc_heap::bgc_track_uoh_alloc()
{
    if (current_c_gc_state == c_gc_state_planning)
    {
        Interlocked::Increment (&uoh_alloc_thread_count);
        dprintf (3, ("h%d: inc lc: %d", heap_number, (int32_t)uoh_alloc_thread_count));
    }
}

inline
void gc_heap::bgc_untrack_uoh_alloc()
{
    if (current_c_gc_state == c_gc_state_planning)
    {
        Interlocked::Decrement (&uoh_alloc_thread_count);
        dprintf (3, ("h%d: dec lc: %d", heap_number, (int32_t)uoh_alloc_thread_count));
    }
}

#endif //BACKGROUND_GC

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
size_t gc_heap::generation_allocator_efficiency_percent (generation* inst)
{
#ifdef DYNAMIC_HEAP_COUNT
    if (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
    {
        uint64_t total_plan_allocated = generation_total_plan_allocated (inst);
        uint64_t condemned_allocated = generation_condemned_allocated (inst);
        return ((total_plan_allocated == 0) ? 0 : (100 * (total_plan_allocated - condemned_allocated) / total_plan_allocated));
    }
    else
#endif //DYNAMIC_HEAP_COUNT
    {
        uint64_t free_obj_space = generation_free_obj_space (inst);
        uint64_t free_list_allocated = generation_free_list_allocated (inst);
        if ((free_list_allocated + free_obj_space) == 0)
        {
            return 0;
        }
        else
        {
            return (size_t)((100 * free_list_allocated) / (free_list_allocated + free_obj_space));
        }
    }
}

inline
size_t gc_heap::generation_unusable_fragmentation (generation* inst, int hn)
{
#ifdef DYNAMIC_HEAP_COUNT
    if (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
    {
        uint64_t total_plan_allocated = generation_total_plan_allocated (inst);
        uint64_t condemned_allocated = generation_condemned_allocated (inst);
        uint64_t unusable_frag = 0;
        size_t fo_space = (((ptrdiff_t)generation_free_obj_space (inst) < 0) ? 0 : generation_free_obj_space (inst));

        if (total_plan_allocated != 0)
        {
            unusable_frag = fo_space + (condemned_allocated * generation_free_list_space (inst) / total_plan_allocated);
        }

        dprintf (3, ("h%d g%d FLa: %Id, ESa: %Id, Ca: %Id | FO: %Id, FL %Id, fl effi %.3f, unusable fl is %Id",
            hn, inst->gen_num,
            generation_free_list_allocated (inst), generation_end_seg_allocated (inst), (size_t)condemned_allocated,
            fo_space, generation_free_list_space (inst),
            ((total_plan_allocated == 0) ? 1.0 : ((float)(total_plan_allocated - condemned_allocated) / (float)total_plan_allocated)),
            (size_t)unusable_frag));

        return (size_t)unusable_frag;
    }
    else
#endif //DYNAMIC_HEAP_COUNT
    {
        uint64_t free_obj_space = generation_free_obj_space (inst);
        uint64_t free_list_allocated = generation_free_list_allocated (inst);
        uint64_t free_list_space = generation_free_list_space (inst);
        if ((free_list_allocated + free_obj_space) == 0)
            return 0;
        return (size_t)(free_obj_space + (free_obj_space * free_list_space) / (free_list_allocated + free_obj_space));
    }
}

inline
void gc_heap::check_loh_compact_mode (BOOL all_heaps_compacted_p)
{
    if (settings.loh_compaction && (loh_compaction_mode == loh_compaction_once))
    {
        if (all_heaps_compacted_p)
        {
            // If the compaction mode says to compact once and we are going to compact LOH,
            // we need to revert it back to no compaction.
            loh_compaction_mode = loh_compaction_default;
        }
    }
}

inline
gc_history_global* gc_heap::get_gc_data_global()
{
#ifdef BACKGROUND_GC
    return (settings.concurrent ? &bgc_data_global : &gc_data_global);
#else
    return &gc_data_global;
#endif //BACKGROUND_GC
}

inline
gc_history_per_heap* gc_heap::get_gc_data_per_heap()
{
#ifdef BACKGROUND_GC
    return (settings.concurrent ? &bgc_data_per_heap : &gc_data_per_heap);
#else
    return &gc_data_per_heap;
#endif //BACKGROUND_GC
}

inline
size_t gc_heap::brick_of (uint8_t* add)
{
    return (size_t)(add - lowest_address) / brick_size;
}

inline
uint8_t* gc_heap::brick_address (size_t brick)
{
    return lowest_address + (brick_size * brick);
}

//codes for the brick entries:
//entry == 0 -> not assigned
//entry >0 offset is entry-1
//entry <0 jump back entry bricks
inline
void gc_heap::set_brick (size_t index, ptrdiff_t val)
{
    if (val < -32767)
    {
        val = -32767;
    }
    assert (val < 32767);
    if (val >= 0)
        brick_table [index] = (short)val+1;
    else
        brick_table [index] = (short)val;

    dprintf (3, ("set brick[%zx] to %d\n", index, (short)val));
}

inline
int gc_heap::get_brick_entry (size_t index)
{
#ifdef MULTIPLE_HEAPS
    return VolatileLoadWithoutBarrier(&brick_table [index]);
#else
    return brick_table[index];
#endif
}

inline
uint8_t* gc_heap::card_address (size_t card)
{
    return  (uint8_t*) (card_size * card);
}

inline
size_t gc_heap::card_of ( uint8_t* object)
{
    return (size_t)(object) / card_size;
}

inline
void gc_heap::clear_card (size_t card)
{
    card_table [card_word (card)] =
        (card_table [card_word (card)] & ~(1 << card_bit (card)));
    dprintf (3,("Cleared card %zx [%zx, %zx[", card, (size_t)card_address (card),
              (size_t)card_address (card+1)));
}

inline
void gc_heap::set_card (size_t card)
{
    size_t word = card_word (card);
    card_table[word] = (card_table [word] | (1 << card_bit (card)));

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    // Also set the card bundle that corresponds to the card
    size_t bundle_to_set = cardw_card_bundle(word);

    card_bundle_set(bundle_to_set);

    dprintf (3,("Set card %zx [%zx, %zx[ and bundle %zx", card, (size_t)card_address (card), (size_t)card_address (card+1), bundle_to_set));
#endif
}

inline
BOOL  gc_heap::card_set_p (size_t card)
{
    return ( card_table [ card_word (card) ] & (1 << card_bit (card)));
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

inline
ptrdiff_t gc_heap::get_desired_allocation (int gen_number)
{
    return dd_desired_allocation (dynamic_data_of (gen_number));
}

inline
ptrdiff_t  gc_heap::get_new_allocation (int gen_number)
{
    return dd_new_allocation (dynamic_data_of (gen_number));
}

//return the amount allocated so far in gen_number
inline
ptrdiff_t  gc_heap::get_allocation (int gen_number)
{
    dynamic_data* dd = dynamic_data_of (gen_number);

    return dd_desired_allocation (dd) - dd_new_allocation (dd);
}

#ifdef BACKGROUND_GC
inline
BOOL gc_heap::background_marked (uint8_t* o)
{
    return mark_array_marked (o);
}

inline
BOOL gc_heap::background_mark1 (uint8_t* o)
{
    BOOL to_mark = !mark_array_marked (o);

    dprintf (3, ("b*%zx*b(%d)", (size_t)o, (to_mark ? 1 : 0)));
    if (to_mark)
    {
        mark_array_set_marked (o);
        dprintf (4, ("n*%zx*n", (size_t)o));
        return TRUE;
    }
    else
        return FALSE;
}

// TODO: we could consider filtering out NULL's here instead of going to
// look for it on other heaps
inline
BOOL gc_heap::background_mark (uint8_t* o, uint8_t* low, uint8_t* high)
{
    BOOL marked = FALSE;
    if ((o >= low) && (o < high))
        marked = background_mark1 (o);
#ifdef MULTIPLE_HEAPS
    else if (o)
    {
        gc_heap* hp = heap_of (o);
        assert (hp);
        if ((o >= hp->background_saved_lowest_address) && (o < hp->background_saved_highest_address))
            marked = background_mark1 (o);
    }
#endif //MULTIPLE_HEAPS
    return marked;
}

#endif //BACKGROUND_GC

inline
size_t size_mark_array_of (uint8_t* from, uint8_t* end)
{
    assert (((size_t)from & ((mark_word_size)-1)) == 0);
    assert (((size_t)end  & ((mark_word_size)-1)) == 0);
    return sizeof (uint32_t)*(((end - from) / mark_word_size));
}

inline
mark* gc_heap::pinned_plug_of (size_t bos)
{
    return &mark_stack_array [ bos ];
}

inline
mark* gc_heap::oldest_pin ()
{
    return pinned_plug_of (mark_stack_bos);
}

inline
BOOL gc_heap::pinned_plug_que_empty_p ()
{
    return (mark_stack_bos == mark_stack_tos);
}

#ifdef FEATURE_LOH_COMPACTION
inline
BOOL gc_heap::loh_pinned_plug_que_empty_p()
{
    return (loh_pinned_queue_bos == loh_pinned_queue_tos);
}
#endif // FEATURE_LOH_COMPACTION

inline
mark* gc_heap::loh_pinned_plug_of (size_t bos)
{
    return &loh_pinned_queue[bos];
}

#ifdef USE_REGIONS
inline bool gc_heap::is_in_gc_range (uint8_t* o)
{
    // we may have frozen objects in read only segments
    // outside of the reserved address range of the gc heap
    assert (((g_gc_lowest_address <= o) && (o < g_gc_highest_address)) ||
        (o == nullptr) || (ro_segment_lookup (o) != nullptr));
    return ((gc_low <= o) && (o < gc_high));
}
#endif //USE_REGIONS

#ifdef FEATURE_EVENT_TRACE
inline
void gc_heap::record_mark_time (uint64_t& mark_time,
                                uint64_t& current_mark_time,
                                uint64_t& last_mark_time)
{
    if (informational_event_enabled_p)
    {
        current_mark_time = GetHighPrecisionTimeStamp();
        mark_time = limit_time_to_uint32 (current_mark_time - last_mark_time);
        dprintf (3, ("%zd - %zd = %zd",
            current_mark_time, last_mark_time, (current_mark_time - last_mark_time)));
        last_mark_time = current_mark_time;
    }
}
#endif //FEATURE_EVENT_TRACE

inline
void gc_heap::init_alloc_info (generation* gen, heap_segment* seg)
{
    generation_allocation_segment (gen) = seg;
    generation_allocation_pointer (gen) = heap_segment_mem (seg);
    generation_allocation_limit (gen) = generation_allocation_pointer (gen);
    generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
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

inline
void gc_heap::update_oldest_pinned_plug()
{
    oldest_pinned_plug = (pinned_plug_que_empty_p() ? 0 : pinned_plug (oldest_pin()));
}

#if !defined(_MSC_VER) && !defined(__cdecl)
#if defined(TARGET_X86)
#define __cdecl __attribute__((cdecl))
#else // TARGET_X86
#define __cdecl
#endif // TARGET_X86
#endif // !_MSC_VER && !__cdecl

#ifdef FEATURE_STRUCTALIGN
#if defined (TARGET_AMD64)
#define brick_bits (12)
#else
#define brick_bits (11)
#endif //TARGET_AMD64
static_assert(brick_size == (1 << brick_bits));

#define child_bits (brick_bits + 1 - LOG2_PTRSIZE)
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
short node_right_child(uint8_t* node)
{
    return child_from_short(((plug_and_pair*)node)[-1].m_pair.right);
}

inline
size_t node_gap_size (uint8_t* node)
{
    return ((plug_and_gap*)node)[-1].gap;
}

inline
ptrdiff_t loh_node_relocation_distance(uint8_t* node)
{
    return (((loh_obj_and_pad*)node)[-1].reloc);
}

inline
void loh_set_node_relocation_distance(uint8_t* node, ptrdiff_t val)
{
    ptrdiff_t* place = &(((loh_obj_and_pad*)node)[-1].reloc);
    *place = val;
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
    *place &= 1;
    *place |= val;
}

#define node_left_p(node) (((plug_and_reloc*)(node))[-1].reloc & 2)
#define set_node_left(node) ((plug_and_reloc*)(node))[-1].reloc |= 2

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

inline
void set_gap_size (uint8_t* node, size_t size)
{
    assert (Aligned (size));

    ((plug_and_gap *)node)[-1].reloc = 0;
    ((plug_and_gap *)node)[-1].lr = 0;
    ((plug_and_gap *)node)[-1].gap = size;

    assert ((size == 0) || (size >= sizeof(plug_and_reloc)));
}

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
            {
                break;
            }
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
            {
                break;
            }
        }
        else
        {
            break;
        }
    }

    if (tree <= old_address)
        return tree;
    else if (candidate)
        return candidate;
    else
        return tree;
}

#ifdef DOUBLY_LINKED_FL
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
BOOL is_free_obj_in_compact_bit_set (uint8_t* node)
{
    return header(node)->IsFreeObjInCompactBitSet();
}

inline
void clear_free_obj_in_compact_bit (uint8_t* node)
{
    header(node)->ClearFreeObjInCompactBit();
}

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
#endif //DOUBLY_LINKED_FL

#ifdef SHORT_PLUGS
inline
void clear_plug_padded (uint8_t* node)
{
    header(node)->ClearMarked();
}
#else //SHORT_PLUGS
inline
void clear_plug_padded (uint8_t* node)
{
    UNREFERENCED_PARAMETER(node);
}
#endif //SHORT_PLUGS

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

inline
static void safe_switch_to_thread()
{
    bool cooperative_mode = gc_heap::enable_preemptive();

    GCToOSInterface::YieldThread(0);

    gc_heap::disable_preemptive(cooperative_mode);
}

void WaitLongerNoInstru (int i);

extern const int32_t lock_free;
extern const int32_t lock_taken;
extern const int32_t lock_decommissioned;

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

inline
BOOL power_of_two_p (size_t integer)
{
    return !(integer & (integer-1));
}

#ifdef FEATURE_STRUCTALIGN
void set_node_aligninfo (uint8_t *node, int requiredAlignment, ptrdiff_t pad);
void clear_node_aligninfo (uint8_t *node);
#else // FEATURE_STRUCTALIGN
#define node_realigned(node)    (((plug_and_reloc*)(node))[-1].reloc & 1)
void set_node_realigned (uint8_t* node);
void clear_node_realigned(uint8_t* node);
#endif // FEATURE_STRUCTALIGN

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

inline
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

#ifndef FEATURE_STRUCTALIGN
#define node_realigned(node)    (((plug_and_reloc*)(node))[-1].reloc & 1)
void set_node_realigned (uint8_t* node);
#endif // FEATURE_STRUCTALIGN

#define commit_min_th (16*OS_PAGE_SIZE)
#define UOH_ALLOCATION_RETRY_MAX_COUNT 2

#ifdef TRACE_GC
extern const char* const allocation_state_str[];
#endif //TRACE_GC

extern const size_t etw_allocation_tick;
extern const size_t fgn_check_quantum;

#ifdef BACKGROUND_GC
extern uint32_t bgc_alloc_spin_count;
extern uint32_t bgc_alloc_spin;
#endif //BACKGROUND_GC

#define check_msl_status(msg, size) if (msl_status == msl_retry_different_heap) \
    { \
        dprintf (5555, ("h%d RETRY %s(%Id)", heap_number, msg, size)); \
        return a_state_retry_allocate; \
    }

#ifdef DOUBLY_LINKED_FL
inline
void set_plug_bgc_mark_bit (uint8_t* node)
{
    header(node)->SetBGCMarkBit();
}

inline
void set_free_obj_in_compact_bit (uint8_t* node)
{
    header(node)->SetFreeObjInCompactBit();
}
#endif //DOUBLY_LINKED_FL

#ifdef SHORT_PLUGS
inline
void set_plug_padded (uint8_t* node)
{
    header(node)->SetMarked();
}

inline
BOOL is_plug_padded (uint8_t* node)
{
    return header(node)->IsMarked();
}
#else //SHORT_PLUGS
inline
void set_plug_padded (uint8_t* node)
{
    UNREFERENCED_PARAMETER(node);
}

inline
BOOL is_plug_padded (uint8_t* node)
{
    UNREFERENCED_PARAMETER(node);
    return FALSE;
}
#endif //SHORT_PLUGS

#ifdef SHORT_PLUGS
inline
uint8_t*& pin_allocation_context_start_region (mark* m)
{
    return m->allocation_context_start_region;
}

inline
uint8_t* get_plug_start_in_saved (uint8_t* old_loc, mark* pinned_plug_entry)
{
    uint8_t* saved_pre_plug_info = (uint8_t*)(pinned_plug_entry->get_pre_plug_reloc_info());
    uint8_t* plug_start_in_saved = saved_pre_plug_info + (old_loc - (pinned_plug (pinned_plug_entry) - sizeof (plug_and_gap)));
    dprintf (2, ("EP: %p(%p), %p", old_loc, pinned_plug (pinned_plug_entry), plug_start_in_saved));
    return plug_start_in_saved;
}
#endif //SHORT_PLUGS

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
        ptrdiff_t count_add;
        ptrdiff_t count_fit;
    };

    void move_bucket (int old_power2, int new_power2)
    {
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
        _ASSERTE(requiredAlignment == DATA_ALIGNMENT && false);
#endif // FEATURE_STRUCTALIGN

        size_t plug_size_to_fit = plug_size;

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

extern uint64_t qpf;
extern double qpf_ms;
extern double qpf_us;

heap_segment* ro_segment_lookup (uint8_t* o);

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

extern initial_memory_details memory_details;

#if defined(BACKGROUND_GC) && !defined(USE_REGIONS)
#define SEGMENT_INITIAL_COMMIT (2*OS_PAGE_SIZE)
#else
#define SEGMENT_INITIAL_COMMIT (OS_PAGE_SIZE)
#endif //BACKGROUND_GC && !USE_REGIONS

// min size to decommit to make the OS call worthwhile
#define MIN_DECOMMIT_SIZE  (100*OS_PAGE_SIZE)

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

#ifdef USE_REGIONS
extern GCSpinLock write_barrier_spin_lock;

inline
void gc_heap::set_region_gen_num (heap_segment* region, int gen_num)
{
    assert (gen_num < (1 << (sizeof (uint8_t) * 8)));
    assert (gen_num >= 0);
    heap_segment_gen_num (region) = (uint8_t)gen_num;

    uint8_t* region_start = get_region_start (region);
    uint8_t* region_end = heap_segment_reserved (region);

    size_t region_index_start = get_basic_region_index_for_address (region_start);
    size_t region_index_end = get_basic_region_index_for_address (region_end);
    region_info entry = (region_info)((gen_num << RI_PLAN_GEN_SHR) | gen_num);
    for (size_t region_index = region_index_start; region_index < region_index_end; region_index++)
    {
        assert (gen_num <= max_generation);
        map_region_to_generation[region_index] = entry;
    }
    if (gen_num <= soh_gen1)
    {
        if ((region_start < ephemeral_low) || (ephemeral_high < region_end))
        {
            while (true)
            {
                if (Interlocked::CompareExchange(&write_barrier_spin_lock.lock, 0, -1) < 0)
                    break;

                if ((ephemeral_low <= region_start) && (region_end <= ephemeral_high))
                    return;

                while (write_barrier_spin_lock.lock >= 0)
                {
                    YieldProcessor();           // indicate to the processor that we are spinning
                }
            }
#ifdef _DEBUG
            write_barrier_spin_lock.holding_thread = GCToEEInterface::GetThread();
#endif //_DEBUG

            if ((region_start < ephemeral_low) || (ephemeral_high < region_end))
            {
                uint8_t* new_ephemeral_low = min (region_start, (uint8_t*)ephemeral_low);
                uint8_t* new_ephemeral_high = max (region_end, (uint8_t*)ephemeral_high);

                dprintf (REGIONS_LOG, ("about to set ephemeral_low = %p ephemeral_high = %p", new_ephemeral_low, new_ephemeral_high));

                stomp_write_barrier_ephemeral (new_ephemeral_low, new_ephemeral_high,
                                               map_region_to_generation_skewed, (uint8_t)min_segment_size_shr);

                // we should only *decrease* ephemeral_low and only *increase* ephemeral_high
                if (ephemeral_low < new_ephemeral_low)
                    GCToOSInterface::DebugBreak ();
                if (new_ephemeral_high < ephemeral_high)
                    GCToOSInterface::DebugBreak ();

                // only set the globals *after* we have updated the write barrier
                ephemeral_low = new_ephemeral_low;
                ephemeral_high = new_ephemeral_high;

                dprintf (REGIONS_LOG, ("set ephemeral_low = %p ephemeral_high = %p", new_ephemeral_low, new_ephemeral_high));
            }
            else
            {
                dprintf (REGIONS_LOG, ("leaving lock - no need to update ephemeral range [%p,%p[ for region [%p,%p]", (uint8_t*)ephemeral_low, (uint8_t*)ephemeral_high, region_start, region_end));
            }
#ifdef _DEBUG
            write_barrier_spin_lock.holding_thread = (Thread*)-1;
#endif //_DEBUG
            write_barrier_spin_lock.lock = -1;
        }
        else
        {
            dprintf (REGIONS_LOG, ("no need to update ephemeral range [%p,%p[ for region [%p,%p]", (uint8_t*)ephemeral_low, (uint8_t*)ephemeral_high, region_start, region_end));
        }
    }
}

#endif //USE_REGIONS

} // namespace WKS/SVR

#endif // GC_INTERNAL_H
