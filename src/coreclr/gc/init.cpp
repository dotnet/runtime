// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef WRITE_WATCH
void hardware_write_watch_api_supported()
{
    if (GCToOSInterface::SupportsWriteWatch())
    {
        hardware_write_watch_capability = true;
        dprintf (2, ("WriteWatch supported"));
    }
    else
    {
        dprintf (2,("WriteWatch not supported"));
    }
}

inline bool can_use_write_watch_for_gc_heap()
{
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    return true;
#else // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    return can_use_hardware_write_watch();
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

#endif //WRITE_WATCH

inline
size_t align_on_segment_hard_limit (size_t add)
{
    return ((size_t)(add + (min_segment_size_hard_limit - 1)) & ~(min_segment_size_hard_limit - 1));
}

#ifdef MULTIPLE_HEAPS
// This logs what we recorded in balance_heaps
// The format for this is
//
// [ms since last GC end]
// [cpu index]
// all elements we stored before this GC for this CPU in the format
// timestamp,tid, alloc_heap_no
// repeat this for each CPU
//
// the timestamp here is just the result of calling QPC,
// it's not converted to ms. The conversion will be done when we process
// the log.
void gc_heap::hb_log_balance_activities()
{
#ifdef HEAP_BALANCE_INSTRUMENTATION
    char* log_buffer = hb_log_buffer;

    uint64_t now = GetHighPrecisionTimeStamp();
    size_t time_since_last_gc_ms = (size_t)((now - last_gc_end_time_us) / 1000);
    dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMP%zd - %zd = %zd", now, last_gc_end_time_ms, time_since_last_gc_ms));

    // We want to get the min and the max timestamp for all procs because it helps with our post processing
    // to know how big an array to allocate to display the history inbetween the GCs.
    uint64_t min_timestamp = 0xffffffffffffffff;
    uint64_t max_timestamp = 0;

    for (int numa_node_index = 0; numa_node_index < total_numa_nodes_on_machine; numa_node_index++)
    {
        heap_balance_info_proc* hb_info_procs = hb_info_numa_nodes[numa_node_index].hb_info_procs;
        for (int proc_index = 0; proc_index < (int)procs_per_numa_node; proc_index++)
        {
            heap_balance_info_proc* hb_info_proc = &hb_info_procs[proc_index];
            int total_entries_on_proc = hb_info_proc->index;

            if (total_entries_on_proc > 0)
            {
                min_timestamp = min (min_timestamp, hb_info_proc->hb_info[0].timestamp);
                max_timestamp = max (max_timestamp, hb_info_proc->hb_info[total_entries_on_proc - 1].timestamp);
            }
        }
    }

    dprintf (HEAP_BALANCE_LOG, ("[GCA#%zd %zd-%zd-%zd]",
        settings.gc_index, time_since_last_gc_ms, (min_timestamp - start_raw_ts), (max_timestamp - start_raw_ts)));

    if (last_hb_recorded_gc_index == (int)settings.gc_index)
    {
        GCToOSInterface::DebugBreak ();
    }

    last_hb_recorded_gc_index = (int)settings.gc_index;

    // When we print out the proc index we need to convert it to the actual proc index (this is contiguous).
    // It helps with post processing.
    for (int numa_node_index = 0; numa_node_index < total_numa_nodes_on_machine; numa_node_index++)
    {
        heap_balance_info_proc* hb_info_procs = hb_info_numa_nodes[numa_node_index].hb_info_procs;
        for (int proc_index = 0; proc_index < (int)procs_per_numa_node; proc_index++)
        {
            heap_balance_info_proc* hb_info_proc = &hb_info_procs[proc_index];
            int total_entries_on_proc = hb_info_proc->index;
            if (total_entries_on_proc > 0)
            {
                int total_exec_time_ms =
                    (int)((double)(hb_info_proc->hb_info[total_entries_on_proc - 1].timestamp -
                                   hb_info_proc->hb_info[0].timestamp) * qpf_ms);
                dprintf (HEAP_BALANCE_LOG, ("[p%d]-%d-%dms",
                    (proc_index + numa_node_index * procs_per_numa_node),
                    total_entries_on_proc, total_exec_time_ms));
            }

            for (int i = 0; i < hb_info_proc->index; i++)
            {
                heap_balance_info* hb_info = &hb_info_proc->hb_info[i];
                bool multiple_procs_p = false;
                bool alloc_count_p = true;
                bool set_ideal_p = false;
                int tid = hb_info->tid;
                int alloc_heap = hb_info->alloc_heap;

                if (tid & (1 << (sizeof (tid) * 8 - 1)))
                {
                    multiple_procs_p = true;
                    tid &= ~(1 << (sizeof (tid) * 8 - 1));
                }

                if (alloc_heap & (1 << (sizeof (alloc_heap) * 8 - 1)))
                {
                    alloc_count_p = false;
                    alloc_heap &= ~(1 << (sizeof (alloc_heap) * 8 - 1));
                }

                if (alloc_heap & (1 << (sizeof (alloc_heap) * 8 - 2)))
                {
                    set_ideal_p = true;
                    alloc_heap &= ~(1 << (sizeof (alloc_heap) * 8 - 2));
                }

                // TODO - This assumes ideal proc is in the same cpu group which is not true
                // when we don't have CPU groups.
                int ideal_proc_no = hb_info->ideal_proc_no;
                int ideal_node_no = -1;
                ideal_proc_no = get_proc_index_numa (ideal_proc_no, &ideal_node_no);
                ideal_proc_no = ideal_proc_no + ideal_node_no * procs_per_numa_node;

                dprintf (HEAP_BALANCE_LOG, ("%zd,%d,%d,%d%s%s%s",
                    (hb_info->timestamp - start_raw_ts),
                    tid,
                    ideal_proc_no,
                    (int)alloc_heap,
                    (multiple_procs_p ? "|m" : ""), (!alloc_count_p ? "|p" : ""), (set_ideal_p ? "|i" : "")));
            }
        }
    }

    for (int numa_node_index = 0; numa_node_index < total_numa_nodes_on_machine; numa_node_index++)
    {
        heap_balance_info_proc* hb_info_procs = hb_info_numa_nodes[numa_node_index].hb_info_procs;
        for (int proc_index = 0; proc_index < (int)procs_per_numa_node; proc_index++)
        {
            heap_balance_info_proc* hb_info_proc = &hb_info_procs[proc_index];
            hb_info_proc->index = 0;
        }
    }
#endif //HEAP_BALANCE_INSTRUMENTATION
}

// The format for this is
//
// [GC_alloc_mb]
// h0_new_alloc, h1_new_alloc, ...
//
void gc_heap::hb_log_new_allocation()
{
#ifdef HEAP_BALANCE_INSTRUMENTATION
    char* log_buffer = hb_log_buffer;

    int desired_alloc_mb = (int)(dd_desired_allocation (g_heaps[0]->dynamic_data_of (0)) / 1024 / 1024);

    int buffer_pos = sprintf_s (hb_log_buffer, hb_log_buffer_size, "[GC_alloc_mb]\n");
    for (int numa_node_index = 0; numa_node_index < heap_select::total_numa_nodes; numa_node_index++)
    {
        int node_allocated_mb = 0;

        // I'm printing out the budget here instead of the numa node index so we know how much
        // of the budget we consumed.
        buffer_pos += sprintf_s (hb_log_buffer + buffer_pos, hb_log_buffer_size - buffer_pos, "[N#%3d]",
            //numa_node_index);
            desired_alloc_mb);

        int heaps_on_node = heap_select::heaps_on_node[numa_node_index].heap_count;

        for (int heap_index = 0; heap_index < heaps_on_node; heap_index++)
        {
            int actual_heap_index = heap_index + numa_node_index * heaps_on_node;
            gc_heap* hp = g_heaps[actual_heap_index];
            dynamic_data* dd0 = hp->dynamic_data_of (0);
            int allocated_mb = (int)((dd_desired_allocation (dd0) - dd_new_allocation (dd0)) / 1024 / 1024);
            node_allocated_mb += allocated_mb;
            buffer_pos += sprintf_s (hb_log_buffer + buffer_pos, hb_log_buffer_size - buffer_pos, "%d,",
                allocated_mb);
        }

        dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMPN#%d a %dmb(%dmb)",
            numa_node_index, node_allocated_mb, desired_alloc_mb));

        buffer_pos += sprintf_s (hb_log_buffer + buffer_pos, hb_log_buffer_size - buffer_pos, "\n");
    }

    dprintf (HEAP_BALANCE_LOG, ("%s", hb_log_buffer));
#endif //HEAP_BALANCE_INSTRUMENTATION
}

BOOL gc_heap::create_thread_support (int number_of_heaps)
{
    BOOL ret = FALSE;
    if (!gc_start_event.CreateOSManualEventNoThrow (FALSE))
    {
        goto cleanup;
    }
    if (!ee_suspend_event.CreateOSAutoEventNoThrow (FALSE))
    {
        goto cleanup;
    }
    if (!gc_t_join.init (number_of_heaps, join_flavor_server_gc))
    {
        goto cleanup;
    }

    ret = TRUE;

cleanup:

    if (!ret)
    {
        destroy_thread_support();
    }

    return ret;
}

void gc_heap::destroy_thread_support ()
{
    if (ee_suspend_event.IsValid())
    {
        ee_suspend_event.CloseEvent();
    }
    if (gc_start_event.IsValid())
    {
        gc_start_event.CloseEvent();
    }
}

bool gc_heap::create_gc_thread ()
{
    dprintf (3, ("Creating gc thread\n"));
    return GCToEEInterface::CreateThread(gc_thread_stub, this, false, ".NET Server GC");
}

#ifdef _MSC_VER
#pragma warning(disable:4715) //IA64 xcompiler recognizes that without the 'break;' the while(1) will never end and therefore not return a value for that code path
#endif //_MSC_VER
void gc_heap::gc_thread_function ()
{
    assert (gc_done_event.IsValid());
    assert (gc_start_event.IsValid());
    dprintf (3, ("gc thread started"));

    heap_select::init_cpu_mapping(heap_number);

    while (1)
    {
#ifdef DYNAMIC_HEAP_COUNT
        if (gc_heap::dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
        {
            // Inactive GC threads may observe gc_t_join.joined() being true here.
            // Before the 1st GC happens, h0's GC thread can also observe gc_t_join.joined() being true because it's
            // also inactive as the main thread (that inits the GC) will act as h0 (to call change_heap_count).
            assert (((heap_number == 0) && (VolatileLoadWithoutBarrier (&settings.gc_index) == 0)) ||
                    (n_heaps <= heap_number) ||
                    !gc_t_join.joined());
        }
        else
#endif //DYNAMIC_HEAP_COUNT
        {
            assert (!gc_t_join.joined());
        }

        if (heap_number == 0)
        {
            bool wait_on_time_out_p = gradual_decommit_in_progress_p;
            uint32_t wait_time = DECOMMIT_TIME_STEP_MILLISECONDS;
#ifdef DYNAMIC_HEAP_COUNT
            // background_running_p can only change from false to true during suspension.
            if (
#ifdef BACKGROUND_GC
                !gc_heap::background_running_p () &&
#endif
                dynamic_heap_count_data.should_change_heap_count)
            {
                assert (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes);

                wait_on_time_out_p = true;
                dynamic_heap_count_data_t::sample& sample = dynamic_heap_count_data.samples[dynamic_heap_count_data.sample_index];
                wait_time = min (wait_time, (uint32_t)(sample.elapsed_between_gcs / 1000 / 3));
                wait_time = max (wait_time, 1u);

                dprintf (6666, ("gc#0 thread waiting for %d ms (betwen GCs %I64d)", wait_time, sample.elapsed_between_gcs));
            }
#endif //DYNAMIC_HEAP_COUNT
            uint32_t wait_result = gc_heap::ee_suspend_event.Wait(wait_on_time_out_p ? wait_time : INFINITE, FALSE);
#ifdef DYNAMIC_HEAP_COUNT
            dprintf (9999, ("waiting for ee done res %d (timeout %d, %I64d ms since last suspend end)(should_change_heap_count is %d) (gradual_decommit_in_progress_p %d)",
                wait_result, wait_time, ((GetHighPrecisionTimeStamp() - last_suspended_end_time) / 1000),
                dynamic_heap_count_data.should_change_heap_count, gradual_decommit_in_progress_p));
#endif //DYNAMIC_HEAP_COUNT
            if (wait_result == WAIT_TIMEOUT)
            {
#ifdef DYNAMIC_HEAP_COUNT
                if (dynamic_heap_count_data.should_change_heap_count)
                {
#ifdef BACKGROUND_GC
                    if (!gc_heap::background_running_p ())
#endif //BACKGROUND_GC
                    {
                        dprintf (6666, ("changing heap count due to timeout"));
                        add_to_hc_history (hc_record_before_check_timeout);
                        check_heap_count();
                    }
                }
#endif //DYNAMIC_HEAP_COUNT

                if (gradual_decommit_in_progress_p)
                {
#ifdef COMMITTED_BYTES_SHADOW
                    decommit_lock.Enter ();
#endif //COMMITTED_BYTES_SHADOW
                    gradual_decommit_in_progress_p = decommit_step (DECOMMIT_TIME_STEP_MILLISECONDS);
#ifdef COMMITTED_BYTES_SHADOW
                    decommit_lock.Leave ();
#endif //COMMITTED_BYTES_SHADOW
                }
                continue;
            }

#ifdef DYNAMIC_HEAP_COUNT
            // We might want to consider also doing this when a BGC finishes.
            if (dynamic_heap_count_data.should_change_heap_count)
            {
#ifdef BACKGROUND_GC
                if (!gc_heap::background_running_p ())
#endif //BACKGROUND_GC
                {
                    // this was a request to do a GC so make sure we follow through with one.
                    dprintf (6666, ("changing heap count at a GC start"));
                    add_to_hc_history (hc_record_before_check_gc_start);
                    check_heap_count ();
                }
            }

            // wait till the threads that should have gone idle at least reached the place where they are about to wait on the idle event.
            if ((gc_heap::dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes) &&
                (n_heaps != dynamic_heap_count_data.last_n_heaps))
            {
                int spin_count = 1024;
                int idle_thread_count = n_max_heaps - n_heaps;
                dprintf (9999, ("heap count changed %d->%d, idle should be %d and is %d", dynamic_heap_count_data.last_n_heaps, n_heaps,
                    idle_thread_count, VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_thread_count)));
                if (idle_thread_count != dynamic_heap_count_data.idle_thread_count)
                {
                    spin_and_wait (spin_count, (idle_thread_count == dynamic_heap_count_data.idle_thread_count));
                    dprintf (9999, ("heap count changed %d->%d, now idle is %d", dynamic_heap_count_data.last_n_heaps, n_heaps,
                        VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_thread_count)));
                }

                add_to_hc_history (hc_record_set_last_heaps);

                dynamic_heap_count_data.last_n_heaps = n_heaps;
            }
#endif //DYNAMIC_HEAP_COUNT

            suspended_start_time = GetHighPrecisionTimeStamp();
            BEGIN_TIMING(suspend_ee_during_log);
            dprintf (9999, ("h0 suspending EE in GC!"));
            GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);
            dprintf (9999, ("h0 suspended EE in GC!"));
            END_TIMING(suspend_ee_during_log);

            proceed_with_gc_p = TRUE;

            if (!should_proceed_with_gc())
            {
                update_collection_counts_for_no_gc();
                proceed_with_gc_p = FALSE;
            }
            else
            {
                settings.init_mechanisms();
#ifdef DYNAMIC_HEAP_COUNT
                if (gc_heap::dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
                {
                    // make sure the other gc threads cannot see this as a request to change heap count
                    // see explanation below about the cases when we return from gc_start_event.Wait
                    assert (dynamic_heap_count_data.new_n_heaps == n_heaps);
                }
#endif //DYNAMIC_HEAP_COUNT
                dprintf (9999, ("GC thread %d setting_gc_start_in_gc(h%d)", heap_number, n_heaps));
                gc_start_event.Set();
            }
            dprintf (3, (ThreadStressLog::gcServerThread0StartMsg(), heap_number));
        }
        else
        {
            dprintf (9999, ("GC thread %d waiting_for_gc_start(%d)(gc%Id)", heap_number, n_heaps, VolatileLoadWithoutBarrier(&settings.gc_index)));
            gc_start_event.Wait(INFINITE, FALSE);
#ifdef DYNAMIC_HEAP_COUNT
            dprintf (9999, ("GC thread %d waiting_done_gc_start(%d-%d)(i: %d)(gc%Id)",
                heap_number, n_heaps, dynamic_heap_count_data.new_n_heaps, dynamic_heap_count_data.init_only_p, VolatileLoadWithoutBarrier (&settings.gc_index)));

            if ((gc_heap::dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes) &&
                (dynamic_heap_count_data.new_n_heaps != n_heaps))
            {
                // The reason why we need to do this is -
                // + for threads that were participating, we need them to do work for change_heap_count
                // + for threads that were not participating but will need to participate, we need to make sure they are woken now instead of
                // randomly sometime later.
                int old_n_heaps = n_heaps;
                int new_n_heaps = dynamic_heap_count_data.new_n_heaps;
                int num_threads_to_wake = max (new_n_heaps, old_n_heaps);
                if (heap_number < num_threads_to_wake)
                {
                    dprintf (9999, ("h%d < %d, calling change", heap_number, num_threads_to_wake));
                    change_heap_count (dynamic_heap_count_data.new_n_heaps);
                    if (new_n_heaps < old_n_heaps)
                    {
                        dprintf (9999, ("h%d after change", heap_number));
                        // at the end of change_heap_count we've changed join's heap count to the new one if it's smaller. So we need to make sure
                        // only that many threads will participate in the following GCs.
                        if (heap_number < new_n_heaps)
                        {
                            add_to_hc_history (hc_record_still_active);
                            dprintf (9999, ("h%d < %d participating (dec)", heap_number, new_n_heaps));
                        }
                        else
                        {
                            Interlocked::Increment (&dynamic_heap_count_data.idle_thread_count);
                            add_to_hc_history (hc_record_became_inactive);

                            dprintf (9999, ("GC thread %d wait_on_idle(%d < %d)(gc%Id), total idle %d", heap_number, old_n_heaps, new_n_heaps,
                                VolatileLoadWithoutBarrier (&settings.gc_index), VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_thread_count)));
                            gc_idle_thread_event.Wait (INFINITE, FALSE);
                            dprintf (9999, ("GC thread %d waking_from_idle(%d)(gc%Id) after doing change", heap_number, n_heaps, VolatileLoadWithoutBarrier (&settings.gc_index)));
                        }
                    }
                    else
                    {
                        add_to_hc_history ((heap_number < old_n_heaps) ? hc_record_still_active : hc_record_became_active);
                        dprintf (9999, ("h%d < %d participating (inc)", heap_number, new_n_heaps));
                    }
                }
                else
                {
                    Interlocked::Increment (&dynamic_heap_count_data.idle_thread_count);
                    add_to_hc_history (hc_record_inactive_waiting);
                    dprintf (9999, ("GC thread %d wait_on_idle(< max %d)(gc%Id), total  idle %d", heap_number, num_threads_to_wake,
                        VolatileLoadWithoutBarrier (&settings.gc_index), VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_thread_count)));
                    gc_idle_thread_event.Wait (INFINITE, FALSE);
                    dprintf (9999, ("GC thread %d waking_from_idle(%d)(gc%Id)", heap_number, n_heaps, VolatileLoadWithoutBarrier (&settings.gc_index)));
                }

                continue;
            }
#endif //DYNAMIC_HEAP_COUNT
            dprintf (3, (ThreadStressLog::gcServerThreadNStartMsg(), heap_number));
        }

        assert ((heap_number == 0) || proceed_with_gc_p);

        if (proceed_with_gc_p)
        {
            garbage_collect (GCHeap::GcCondemnedGeneration);

            if (pm_trigger_full_gc)
            {
                garbage_collect_pm_full_gc();
            }
        }

        if (heap_number == 0)
        {
            if (proceed_with_gc_p && (!settings.concurrent))
            {
                do_post_gc();
            }

#ifdef BACKGROUND_GC
            recover_bgc_settings();
#endif //BACKGROUND_GC

#ifdef MULTIPLE_HEAPS
#ifdef STRESS_DYNAMIC_HEAP_COUNT
            dynamic_heap_count_data.lowest_heap_with_msl_uoh = -1;
#endif //STRESS_DYNAMIC_HEAP_COUNT
            for (int i = 0; i < gc_heap::n_heaps; i++)
            {
                gc_heap* hp = gc_heap::g_heaps[i];
                leave_spin_lock(&hp->more_space_lock_soh);

#ifdef STRESS_DYNAMIC_HEAP_COUNT
                if ((dynamic_heap_count_data.lowest_heap_with_msl_uoh == -1) && (hp->uoh_msl_before_gc_p))
                {
                    dynamic_heap_count_data.lowest_heap_with_msl_uoh = i;
                }

                if (hp->uoh_msl_before_gc_p)
                {
                    dprintf (5555, ("h%d uoh msl was taken before GC", i));
                    hp->uoh_msl_before_gc_p = false;
                }
#endif //STRESS_DYNAMIC_HEAP_COUNT
            }
#endif //MULTIPLE_HEAPS

            gc_heap::gc_started = FALSE;

#ifdef BACKGROUND_GC
            gc_heap::add_bgc_pause_duration_0();
#endif //BACKGROUND_GC
            BEGIN_TIMING(restart_ee_during_log);
            GCToEEInterface::RestartEE(TRUE);
            END_TIMING(restart_ee_during_log);
            process_sync_log_stats();

            dprintf (SPINLOCK_LOG, ("GC Lgc"));
            leave_spin_lock (&gc_heap::gc_lock);

            gc_heap::internal_gc_done = true;

            if (proceed_with_gc_p)
                set_gc_done();
            else
            {
                // If we didn't actually do a GC, it means we didn't wait up the other threads,
                // we still need to set the gc_done_event for those threads.
                for (int i = 0; i < gc_heap::n_heaps; i++)
                {
                    gc_heap* hp = gc_heap::g_heaps[i];
                    hp->set_gc_done();
                }
            }

            // check if we should do some decommitting
            if (gradual_decommit_in_progress_p)
            {
#ifdef COMMITTED_BYTES_SHADOW
                decommit_lock.Enter ();
#endif //COMMITTED_BYTES_SHADOW
                gradual_decommit_in_progress_p = decommit_step (DECOMMIT_TIME_STEP_MILLISECONDS);
#ifdef COMMITTED_BYTES_SHADOW
                decommit_lock.Leave ();
#endif //COMMITTED_BYTES_SHADOW
            }
        }
        else
        {
            int spin_count = 32 * (gc_heap::n_heaps - 1);

            // wait until RestartEE has progressed to a stage where we can restart user threads
            while (!gc_heap::internal_gc_done && !GCHeap::SafeToRestartManagedThreads())
            {
                spin_and_switch (spin_count, (gc_heap::internal_gc_done || GCHeap::SafeToRestartManagedThreads()));
            }
            set_gc_done();
        }
    }
}
#ifdef _MSC_VER
#pragma warning(default:4715) //IA64 xcompiler recognizes that without the 'break;' the while(1) will never end and therefore not return a value for that code path
#endif //_MSC_VER

#endif //MULTIPLE_HEAPS

void gc_heap::make_generation (int gen_num, heap_segment* seg, uint8_t* start)
{
    generation* gen = generation_of (gen_num);

    gen->gen_num = gen_num;
#ifndef USE_REGIONS
    gen->allocation_start = start;
    gen->plan_allocation_start = 0;
#endif //USE_REGIONS
    gen->allocation_context.alloc_ptr = 0;
    gen->allocation_context.alloc_limit = 0;
    gen->allocation_context.alloc_bytes = 0;
    gen->allocation_context.alloc_bytes_uoh = 0;
    gen->allocation_context_start_region = 0;
    gen->start_segment = seg;

#ifdef USE_REGIONS
    dprintf (REGIONS_LOG, ("g%d start seg is %zx-%p", gen_num, (size_t)seg, heap_segment_mem (seg)));
    gen->tail_region = seg;
    gen->tail_ro_region = 0;
#endif //USE_REGIONS
    gen->allocation_segment = seg;
    gen->free_list_space = 0;
    gen->free_list_allocated = 0;
    gen->end_seg_allocated = 0;
    gen->condemned_allocated = 0;
    gen->sweep_allocated = 0;
    gen->free_obj_space = 0;
    gen->allocation_size = 0;
    gen->pinned_allocation_sweep_size = 0;
    gen->pinned_allocation_compact_size = 0;
    gen->allocate_end_seg_p = FALSE;
    gen->free_list_allocator.clear();

#ifdef DOUBLY_LINKED_FL
    gen->set_bgc_mark_bit_p = FALSE;
#endif //DOUBLY_LINKED_FL

#ifdef FREE_USAGE_STATS
    memset (gen->gen_free_spaces, 0, sizeof (gen->gen_free_spaces));
    memset (gen->gen_current_pinned_free_spaces, 0, sizeof (gen->gen_current_pinned_free_spaces));
    memset (gen->gen_plugs, 0, sizeof (gen->gen_plugs));
#endif //FREE_USAGE_STATS
}

void gc_heap::adjust_ephemeral_limits ()
{
#ifndef USE_REGIONS
    ephemeral_low = generation_allocation_start (generation_of (max_generation - 1));
    ephemeral_high = heap_segment_reserved (ephemeral_heap_segment);

    dprintf (3, ("new ephemeral low: %zx new ephemeral high: %zx",
        (size_t)ephemeral_low, (size_t)ephemeral_high))

#ifndef MULTIPLE_HEAPS
    // This updates the write barrier helpers with the new info.
    stomp_write_barrier_ephemeral(ephemeral_low, ephemeral_high);
#endif // MULTIPLE_HEAPS
#endif //USE_REGIONS
}

uint32_t adjust_heaps_hard_limit_worker (uint32_t nhp, size_t limit)
{
    if (!limit)
        return nhp;

    size_t aligned_limit =  align_on_segment_hard_limit (limit);
    uint32_t nhp_oh = (uint32_t)(aligned_limit / min_segment_size_hard_limit);
    nhp = min (nhp_oh, nhp);
    return (max (nhp, 1u));
}

uint32_t gc_heap::adjust_heaps_hard_limit (uint32_t nhp)
{
#ifdef MULTIPLE_HEAPS
    if (heap_hard_limit_oh[soh])
    {
        for (int i = 0; i < (total_oh_count - 1); i++)
        {
            nhp = adjust_heaps_hard_limit_worker (nhp, heap_hard_limit_oh[i]);
        }
    }
    else if (heap_hard_limit)
    {
        nhp = adjust_heaps_hard_limit_worker (nhp, heap_hard_limit);
    }
#endif

    return nhp;
}

size_t gc_heap::adjust_segment_size_hard_limit_va (size_t seg_size)
{
    return (use_large_pages_p ?
            align_on_segment_hard_limit (seg_size) :
            round_up_power2 (seg_size));
}

size_t gc_heap::adjust_segment_size_hard_limit (size_t limit, uint32_t nhp)
{
    if (!limit)
    {
        limit = min_segment_size_hard_limit;
    }

    size_t seg_size = align_on_segment_hard_limit (limit) / nhp;
    return adjust_segment_size_hard_limit_va (seg_size);
}

#ifdef USE_REGIONS
bool allocate_initial_regions(int number_of_heaps)
{
    initial_regions = new (nothrow) uint8_t*[number_of_heaps][total_generation_count][2];
    if (initial_regions == nullptr)
    {
        log_init_error_to_host ("allocate_initial_regions failed to allocate %zd bytes", (number_of_heaps * total_generation_count * 2 * sizeof (uint8_t*)));
        return false;
    }
    for (int i = 0; i < number_of_heaps; i++)
    {
        bool succeed = global_region_allocator.allocate_large_region(
            poh_generation,
            &initial_regions[i][poh_generation][0],
            &initial_regions[i][poh_generation][1], allocate_forward, 0, nullptr);
        assert(succeed);
    }
    for (int i = 0; i < number_of_heaps; i++)
    {
        for (int gen_num = max_generation; gen_num >= 0; gen_num--)
        {
            bool succeed = global_region_allocator.allocate_basic_region(
                gen_num,
                &initial_regions[i][gen_num][0],
                &initial_regions[i][gen_num][1], nullptr);
            assert(succeed);
        }
    }
    for (int i = 0; i < number_of_heaps; i++)
    {
        bool succeed = global_region_allocator.allocate_large_region(
            loh_generation,
            &initial_regions[i][loh_generation][0],
            &initial_regions[i][loh_generation][1], allocate_forward, 0, nullptr);
        assert(succeed);
    }
    return true;
}

#endif //USE_REGIONS

HRESULT gc_heap::initialize_gc (size_t soh_segment_size,
                                size_t loh_segment_size,
                                size_t poh_segment_size
#ifdef MULTIPLE_HEAPS
                                ,int number_of_heaps
#endif //MULTIPLE_HEAPS
)
{
#ifdef GC_CONFIG_DRIVEN
    if (GCConfig::GetConfigLogEnabled())
    {
        gc_config_log = CreateLogFile(GCConfig::GetConfigLogFile(), true);

        if (gc_config_log == NULL)
        {
            return E_FAIL;
        }

        gc_config_log_buffer = new (nothrow) uint8_t [gc_config_log_buffer_size];
        if (!gc_config_log_buffer)
        {
            fclose(gc_config_log);
            return E_OUTOFMEMORY;
        }

        compact_ratio = static_cast<int>(GCConfig::GetCompactRatio());

        //         h#  | GC  | gen | C   | EX   | NF  | BF  | ML  | DM  || PreS | PostS | Merge | Conv | Pre | Post | PrPo | PreP | PostP |
        cprintf (("%2s | %6s | %1s | %1s | %2s | %2s | %2s | %2s | %2s || %5s | %5s | %5s | %5s | %5s | %5s | %5s | %5s | %5s |",
                "h#", // heap index
                "GC", // GC index
                "g", // generation
                "C",  // compaction (empty means sweeping), 'M' means it was mandatory, 'W' means it was not
                "EX", // heap expansion
                "NF", // normal fit
                "BF", // best fit (if it indicates neither NF nor BF it means it had to acquire a new seg.
                "ML", // mark list
                "DM", // demotion
                "PreS", // short object before pinned plug
                "PostS", // short object after pinned plug
                "Merge", // merged pinned plugs
                "Conv", // converted to pinned plug
                "Pre", // plug before pinned plug but not after
                "Post", // plug after pinned plug but not before
                "PrPo", // plug both before and after pinned plug
                "PreP", // pre short object padded
                "PostP" // post short object padded
                ));
    }
#endif //GC_CONFIG_DRIVEN

    HRESULT hres = S_OK;

    conserve_mem_setting = (int)GCConfig::GetGCConserveMem();

#ifdef DYNAMIC_HEAP_COUNT
    dynamic_adaptation_mode = (int)GCConfig::GetGCDynamicAdaptationMode();
    if (GCConfig::GetHeapCount() != 0)
    {
        dynamic_adaptation_mode = 0;
    }

    if ((dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes) && (conserve_mem_setting == 0))
        conserve_mem_setting = 5;

#ifdef STRESS_DYNAMIC_HEAP_COUNT
    bgc_to_ngc2_ratio = (int)GCConfig::GetGCDBGCRatio();
    dprintf (1, ("bgc_to_ngc2_ratio is %d", bgc_to_ngc2_ratio));
#endif
#endif //DYNAMIC_HEAP_COUNT

    if (conserve_mem_setting < 0)
        conserve_mem_setting = 0;
    if (conserve_mem_setting > 9)
        conserve_mem_setting = 9;

    dprintf (1, ("conserve_mem_setting = %d", conserve_mem_setting));

#ifdef WRITE_WATCH
    hardware_write_watch_api_supported();
#ifdef BACKGROUND_GC
    if (can_use_write_watch_for_gc_heap() && GCConfig::GetConcurrentGC())
    {
        gc_can_use_concurrent = true;
#ifndef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        virtual_alloc_hardware_write_watch = true;
#endif // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    }
    else
    {
        gc_can_use_concurrent = false;
    }

    GCConfig::SetConcurrentGC(gc_can_use_concurrent);
#else //BACKGROUND_GC
    GCConfig::SetConcurrentGC(false);
#endif //BACKGROUND_GC
#endif //WRITE_WATCH

#ifdef BACKGROUND_GC
#ifdef USE_REGIONS
    int bgc_uoh_inc_percent_alloc_wait = (int)GCConfig::GetUOHWaitBGCSizeIncPercent();
    if (bgc_uoh_inc_percent_alloc_wait != -1)
    {
        bgc_uoh_inc_ratio_alloc_wait = (float)bgc_uoh_inc_percent_alloc_wait / 100.0f;
    }
    else
    {
        bgc_uoh_inc_percent_alloc_wait = (int)(bgc_uoh_inc_ratio_alloc_wait * 100.0f);
    }

    if (bgc_uoh_inc_ratio_alloc_normal > bgc_uoh_inc_ratio_alloc_wait)
    {
        bgc_uoh_inc_ratio_alloc_normal = bgc_uoh_inc_ratio_alloc_wait;
    }
    GCConfig::SetUOHWaitBGCSizeIncPercent (bgc_uoh_inc_percent_alloc_wait);
    dprintf (1, ("UOH allocs during BGC are allowed normally when inc ratio is  < %.3f, will wait when > %.3f",
        bgc_uoh_inc_ratio_alloc_normal, bgc_uoh_inc_ratio_alloc_wait));
#endif

    // leave the first page to contain only segment info
    // because otherwise we could need to revisit the first page frequently in
    // background GC.
    segment_info_size = OS_PAGE_SIZE;
#else
    segment_info_size = Align (sizeof (heap_segment), get_alignment_constant (FALSE));
#endif //BACKGROUND_GC

    reserved_memory = 0;
    size_t initial_heap_size = soh_segment_size + loh_segment_size + poh_segment_size;
    uint16_t* heap_no_to_numa_node = nullptr;
#ifdef MULTIPLE_HEAPS
    reserved_memory_limit = initial_heap_size * number_of_heaps;
    if (!heap_select::init(number_of_heaps))
        return E_OUTOFMEMORY;
    if (GCToOSInterface::CanEnableGCNumaAware())
        heap_no_to_numa_node = heap_select::heap_no_to_numa_node;
#else //MULTIPLE_HEAPS
    reserved_memory_limit = initial_heap_size;
    int number_of_heaps = 1;
#endif //MULTIPLE_HEAPS

    check_commit_cs.Initialize();
#ifdef COMMITTED_BYTES_SHADOW
    decommit_lock.Initialize();
#endif //COMMITTED_BYTES_SHADOW

#ifdef USE_REGIONS
    if (regions_range)
    {
        // REGIONS TODO: we should reserve enough space at the end of what we reserved that's
        // big enough to accommodate if we were to materialize all the GC bookkeeping datastructures.
        // We only need to commit what we use and just need to commit more instead of having to
        // relocate the existing table and then calling copy_brick_card_table.
        // Right now all the non mark array portions are commmitted since I'm calling make_card_table
        // on the whole range. This can be committed as needed.
        size_t reserve_size = regions_range;
        // In large pages emulation mode, use normal reserve (not real large pages) then
        // commit all upfront to simulate the "always committed" property.
        bool use_real_large_pages = use_large_pages_p && !large_pages_emulation_mode_p;
        uint8_t* reserve_range = (uint8_t*)virtual_alloc (reserve_size, use_real_large_pages);
        if (!reserve_range)
        {
            log_init_error_to_host ("Reserving %zd bytes (%zd GiB) for the regions range failed, do you have a virtual memory limit set on this process?",
                reserve_size, gib (reserve_size));
            return E_OUTOFMEMORY;
        }
        if (large_pages_emulation_mode_p)
        {
            if (!GCToOSInterface::VirtualCommit (reserve_range, reserve_size))
            {
                return E_OUTOFMEMORY;
            }
        }

        if (!global_region_allocator.init (reserve_range, (reserve_range + reserve_size),
                                           ((size_t)1 << min_segment_size_shr),
                                           &g_gc_lowest_address, &g_gc_highest_address))
            return E_OUTOFMEMORY;

        if (!allocate_initial_regions(number_of_heaps))
            return E_OUTOFMEMORY;
    }
    else
    {
        assert (!"cannot use regions without specifying the range!!!");
        log_init_error_to_host ("Regions range is 0! unexpected");
        return E_FAIL;
    }
#else //USE_REGIONS
    bool separated_poh_p = use_large_pages_p &&
                           heap_hard_limit_oh[soh] &&
                           (GCConfig::GetGCHeapHardLimitPOH() == 0) &&
                           (GCConfig::GetGCHeapHardLimitPOHPercent() == 0);
    bool use_real_large_pages = use_large_pages_p && !large_pages_emulation_mode_p;
    if (!reserve_initial_memory (soh_segment_size, loh_segment_size, poh_segment_size, number_of_heaps,
                                 use_real_large_pages, separated_poh_p, heap_no_to_numa_node))
        return E_OUTOFMEMORY;
    if (use_large_pages_p)
    {
#ifndef HOST_64BIT
        // Large pages are not supported on 32bit
        assert (false);
#endif //!HOST_64BIT

        if (heap_hard_limit_oh[soh])
        {
            heap_hard_limit_oh[soh] = soh_segment_size * number_of_heaps;
            heap_hard_limit_oh[loh] = loh_segment_size * number_of_heaps;
            heap_hard_limit_oh[poh] = poh_segment_size * number_of_heaps;
            heap_hard_limit = heap_hard_limit_oh[soh] + heap_hard_limit_oh[loh] + heap_hard_limit_oh[poh];
        }
        else
        {
            assert (heap_hard_limit);
            heap_hard_limit = (soh_segment_size + loh_segment_size + poh_segment_size) * number_of_heaps;
        }
    }
#endif //USE_REGIONS

#ifdef CARD_BUNDLE
    //check if we need to turn on card_bundles.
#ifdef MULTIPLE_HEAPS
    // use INT64 arithmetic here because of possible overflow on 32p
    uint64_t th = (uint64_t)MH_TH_CARD_BUNDLE*number_of_heaps;
#else
    // use INT64 arithmetic here because of possible overflow on 32p
    uint64_t th = (uint64_t)SH_TH_CARD_BUNDLE;
#endif //MULTIPLE_HEAPS

    if (can_use_write_watch_for_card_table() && reserved_memory >= th)
    {
        settings.card_bundles = TRUE;
    }
    else
    {
        settings.card_bundles = FALSE;
    }
#endif //CARD_BUNDLE

    settings.first_init();

    int latency_level_from_config = static_cast<int>(GCConfig::GetLatencyLevel());
    if (latency_level_from_config >= latency_level_first && latency_level_from_config <= latency_level_last)
    {
        gc_heap::latency_level = static_cast<gc_latency_level>(latency_level_from_config);
    }

    init_static_data();

    g_gc_card_table = make_card_table (g_gc_lowest_address, g_gc_highest_address);

    if (!g_gc_card_table)
        return E_OUTOFMEMORY;

    gc_started = FALSE;

#ifdef MULTIPLE_HEAPS
    g_heaps = new (nothrow) gc_heap* [number_of_heaps];
    if (!g_heaps)
        return E_OUTOFMEMORY;

#if !defined(USE_REGIONS) || defined(_DEBUG)
    g_promoted = new (nothrow) size_t [number_of_heaps*16];
    if (!g_promoted)
        return E_OUTOFMEMORY;
#endif //!USE_REGIONS || _DEBUG
#ifdef BACKGROUND_GC
    g_bpromoted = new (nothrow) size_t [number_of_heaps*16];
    if (!g_bpromoted)
        return E_OUTOFMEMORY;
#endif

#ifdef MH_SC_MARK
    g_mark_stack_busy = new (nothrow) int[(number_of_heaps+2)*HS_CACHE_LINE_SIZE/sizeof(int)];
    if (!g_mark_stack_busy)
        return E_OUTOFMEMORY;
#endif //MH_SC_MARK

    if (!create_thread_support (number_of_heaps))
        return E_OUTOFMEMORY;

    yp_spin_count_unit = 32 * number_of_heaps;
#else
    yp_spin_count_unit = 32 * g_num_processors;
#endif //MULTIPLE_HEAPS

    // Check if the values are valid for the spin count if provided by the user
    // and if they are, set them as the yp_spin_count_unit and then ignore any updates made in SetYieldProcessorScalingFactor.
    int64_t spin_count_unit_from_config = GCConfig::GetGCSpinCountUnit();
    gc_heap::spin_count_unit_config_p = (spin_count_unit_from_config > 0) && (spin_count_unit_from_config <= MAX_YP_SPIN_COUNT_UNIT);
    if (gc_heap::spin_count_unit_config_p)
    {
        yp_spin_count_unit = static_cast<int32_t>(spin_count_unit_from_config);
    }

    original_spin_count_unit = yp_spin_count_unit;

#if (defined(MULTIPLE_HEAPS) && defined(DYNAMIC_HEAP_COUNT))
    if ((dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes) && (!gc_heap::spin_count_unit_config_p))
    {
        yp_spin_count_unit = 10;
    }
#endif // MULTIPLE_HEAPS && DYNAMIC_HEAP_COUNT

#if defined(__linux__)
    GCToEEInterface::UpdateGCEventStatus(static_cast<int>(GCEventStatus::GetEnabledLevel(GCEventProvider_Default)),
                                         static_cast<int>(GCEventStatus::GetEnabledKeywords(GCEventProvider_Default)),
                                         static_cast<int>(GCEventStatus::GetEnabledLevel(GCEventProvider_Private)),
                                         static_cast<int>(GCEventStatus::GetEnabledKeywords(GCEventProvider_Private)));
#endif // __linux__

#ifdef USE_VXSORT
    InitSupportedInstructionSet ((int32_t)GCConfig::GetGCEnabledInstructionSets());
#endif

    if (!init_semi_shared())
    {
        log_init_error_to_host ("PER_HEAP_ISOLATED data members initialization failed");
        hres = E_FAIL;
    }

    return hres;
}

gc_heap* gc_heap::make_gc_heap (
#ifdef MULTIPLE_HEAPS
                                GCHeap* vm_hp,
                                int heap_number
#endif //MULTIPLE_HEAPS
                                )
{
    gc_heap* res = 0;

#ifdef MULTIPLE_HEAPS
    res = new (nothrow) gc_heap;
    if (!res)
        return 0;

    res->vm_heap = vm_hp;
    res->alloc_context_count = 0;

#ifndef USE_REGIONS
    res->mark_list_piece_start = new (nothrow) uint8_t**[n_heaps];
    if (!res->mark_list_piece_start)
        return 0;

    res->mark_list_piece_end = new (nothrow) uint8_t**[n_heaps + 32]; // +32 is padding to reduce false sharing

    if (!res->mark_list_piece_end)
        return 0;
#endif //!USE_REGIONS

#endif //MULTIPLE_HEAPS

    if (res->init_gc_heap (
#ifdef MULTIPLE_HEAPS
        heap_number
#else  //MULTIPLE_HEAPS
        0
#endif //MULTIPLE_HEAPS
        )==0)
    {
        return 0;
    }

#ifdef MULTIPLE_HEAPS
    return res;
#else
    return (gc_heap*)1;
#endif //MULTIPLE_HEAPS
}

// Destroys resources owned by gc. It is assumed that a last GC has been performed and that
// the finalizer queue has been drained.
void gc_heap::shutdown_gc()
{
    destroy_semi_shared();

#ifdef MULTIPLE_HEAPS
    //delete the heaps array
    delete[] g_heaps;
    destroy_thread_support();
    n_heaps = 0;
#endif //MULTIPLE_HEAPS
    //destroy seg_manager

    destroy_initial_memory();

    GCToOSInterface::Shutdown();
}

void gc_heap::init_records()
{
    // An option is to move this to be after we figure out which gen to condemn so we don't
    // need to clear some generations' data 'cause we know they don't change, but that also means
    // we can't simply call memset here.
    memset (&gc_data_per_heap, 0, sizeof (gc_data_per_heap));
    gc_data_per_heap.heap_index = heap_number;
    if (heap_number == 0)
        memset (&gc_data_global, 0, sizeof (gc_data_global));

#ifdef GC_CONFIG_DRIVEN
    memset (interesting_data_per_gc, 0, sizeof (interesting_data_per_gc));
#endif //GC_CONFIG_DRIVEN
    memset (&fgm_result, 0, sizeof (fgm_result));

    for (int i = 0; i < total_generation_count; i++)
    {
        gc_data_per_heap.gen_data[i].size_before = generation_size (i);
        generation* gen = generation_of (i);
        gc_data_per_heap.gen_data[i].free_list_space_before = generation_free_list_space (gen);
        gc_data_per_heap.gen_data[i].free_obj_space_before = generation_free_obj_space (gen);
    }

#ifdef USE_REGIONS
    end_gen0_region_space = uninitialized_end_gen0_region_space;
    end_gen0_region_committed_space = 0;
    gen0_pinned_free_space = 0;
    gen0_large_chunk_found = false;
    num_regions_freed_in_sweep = 0;
#endif //USE_REGIONS

    sufficient_gen0_space_p = FALSE;

#ifdef MULTIPLE_HEAPS
    gen0_allocated_after_gc_p = false;
#endif //MULTIPLE_HEAPS

#if defined (_DEBUG) && defined (VERIFY_HEAP)
    verify_pinned_queue_p = FALSE;
#endif // _DEBUG && VERIFY_HEAP
}

size_t gc_heap::get_gen0_min_size()
{
    size_t gen0size = static_cast<size_t>(GCConfig::GetGen0Size());
    bool is_config_invalid = ((gen0size == 0) || !g_theGCHeap->IsValidGen0MaxSize(gen0size));
    if (is_config_invalid)
    {
#ifdef SERVER_GC
        // performance data seems to indicate halving the size results
        // in optimal perf.  Ask for adjusted gen0 size.
        gen0size = max(GCToOSInterface::GetCacheSizePerLogicalCpu(FALSE), (size_t)(256*1024));

        // if gen0 size is too large given the available memory, reduce it.
        // Get true cache size, as we don't want to reduce below this.
        size_t trueSize = max(GCToOSInterface::GetCacheSizePerLogicalCpu(TRUE), (size_t)(256*1024));
        dprintf (1, ("cache: %zd-%zd",
            GCToOSInterface::GetCacheSizePerLogicalCpu(FALSE),
            GCToOSInterface::GetCacheSizePerLogicalCpu(TRUE)));

        int n_heaps = gc_heap::n_heaps;
#else //SERVER_GC
        size_t trueSize = GCToOSInterface::GetCacheSizePerLogicalCpu(TRUE);
        gen0size = max((4*trueSize/5),(size_t)(256*1024));
        trueSize = max(trueSize, (size_t)(256*1024));
        int n_heaps = 1;
#endif //SERVER_GC

        llc_size = trueSize;

#ifdef DYNAMIC_HEAP_COUNT
        if (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
        {
            // if we are asked to be stingy with memory, limit gen 0 size
            gen0size = min (gen0size, (size_t)(4*1024*1024));
        }
#endif //DYNAMIC_HEAP_COUNT

        dprintf (1, ("gen0size: %zd * %d = %zd, physical mem: %zd / 6 = %zd",
                gen0size, n_heaps, (gen0size * n_heaps),
                gc_heap::total_physical_mem,
                gc_heap::total_physical_mem / 6));

        // if the total min GC across heaps will exceed 1/6th of available memory,
        // then reduce the min GC size until it either fits or has been reduced to cache size.
        while ((gen0size * n_heaps) > (gc_heap::total_physical_mem / 6))
        {
            gen0size = gen0size / 2;
            if (gen0size <= trueSize)
            {
                gen0size = trueSize;
                break;
            }
        }
    }
#ifdef FEATURE_EVENT_TRACE
    else
    {
        gen0_min_budget_from_config = gen0size;
    }
#endif //FEATURE_EVENT_TRACE

    size_t seg_size = gc_heap::soh_segment_size;
    assert (seg_size);

    // Generation 0 must never be more than 1/2 the segment size.
    if (gen0size >= (seg_size / 2))
        gen0size = seg_size / 2;

    // If the value from config is valid we use it as is without this adjustment.
    if (is_config_invalid)
    {
        if (heap_hard_limit)
        {
            size_t gen0size_seg = seg_size / 8;
            if (gen0size >= gen0size_seg)
            {
                dprintf (1, ("gen0 limited by seg size %zd->%zd", gen0size, gen0size_seg));
                gen0size = gen0size_seg;
            }
        }

        gen0size = gen0size / 8 * 5;
    }

#ifdef STRESS_REGIONS
    // This is just so we can test allocation using more than one region on machines with very
    // small caches.
    gen0size = ((size_t)1 << min_segment_size_shr) * 3;
#endif //STRESS_REGIONS

    gen0size = Align (gen0size);

    return gen0size;
}

bool gc_heap::compute_hard_limit_from_heap_limits()
{
#ifndef HOST_64BIT
    // need to consider overflows:
    if (! ((heap_hard_limit_oh[soh] < max_heap_hard_limit && heap_hard_limit_oh[loh] <= max_heap_hard_limit / 2 && heap_hard_limit_oh[poh] <= max_heap_hard_limit / 2)
           || (heap_hard_limit_oh[soh] <= max_heap_hard_limit / 2 && heap_hard_limit_oh[loh] < max_heap_hard_limit && heap_hard_limit_oh[poh] <= max_heap_hard_limit / 2)
           || (heap_hard_limit_oh[soh] <= max_heap_hard_limit / 2 && heap_hard_limit_oh[loh] <= max_heap_hard_limit / 2 && heap_hard_limit_oh[poh] < max_heap_hard_limit)))
    {
        return false;
    }
#endif //!HOST_64BIT

    heap_hard_limit = heap_hard_limit_oh[soh] + heap_hard_limit_oh[loh] + heap_hard_limit_oh[poh];
    return true;
}

// On 32bit we have next guarantees for limits:
// 1) heap-specific limits:
//   0 <= (heap_hard_limit = heap_hard_limit_oh[soh] + heap_hard_limit_oh[loh] + heap_hard_limit_oh[poh]) < 4Gb
//   a) 0 <= heap_hard_limit_oh[soh] < 2Gb, 0 <= heap_hard_limit_oh[loh] <= 1Gb, 0 <= heap_hard_limit_oh[poh] <= 1Gb
//   b) 0 <= heap_hard_limit_oh[soh] <= 1Gb, 0 <= heap_hard_limit_oh[loh] < 2Gb, 0 <= heap_hard_limit_oh[poh] <= 1Gb
//   c) 0 <= heap_hard_limit_oh[soh] <= 1Gb, 0 <= heap_hard_limit_oh[loh] <= 1Gb, 0 <= heap_hard_limit_oh[poh] < 2Gb
// 2) same limit for all heaps:
//   0 <= heap_hard_limit <= 1Gb
//
// These ranges guarantee that calculation of soh_segment_size, loh_segment_size and poh_segment_size with alignment and round up won't overflow,
// as well as calculation of sum of them (overflow to 0 is allowed, because allocation with 0 size will fail later).
bool gc_heap::compute_hard_limit()
{
    heap_hard_limit_oh[soh] = 0;

    heap_hard_limit = (size_t)GCConfig::GetGCHeapHardLimit();
    heap_hard_limit_oh[soh] = (size_t)GCConfig::GetGCHeapHardLimitSOH();
    heap_hard_limit_oh[loh] = (size_t)GCConfig::GetGCHeapHardLimitLOH();
    heap_hard_limit_oh[poh] = (size_t)GCConfig::GetGCHeapHardLimitPOH();

#ifdef HOST_64BIT
    int64_t large_pages_config = GCConfig::GetGCLargePages();
    use_large_pages_p = (large_pages_config != 0);
    large_pages_emulation_mode_p = (large_pages_config == 2);
#endif //HOST_64BIT

    if (heap_hard_limit_oh[soh] || heap_hard_limit_oh[loh] || heap_hard_limit_oh[poh])
    {
        if (!heap_hard_limit_oh[soh])
        {
            return false;
        }
        if (!heap_hard_limit_oh[loh])
        {
            return false;
        }
        if (!compute_hard_limit_from_heap_limits())
        {
            return false;
        }
    }
    else
    {
        uint32_t percent_of_mem_soh = (uint32_t)GCConfig::GetGCHeapHardLimitSOHPercent();
        uint32_t percent_of_mem_loh = (uint32_t)GCConfig::GetGCHeapHardLimitLOHPercent();
        uint32_t percent_of_mem_poh = (uint32_t)GCConfig::GetGCHeapHardLimitPOHPercent();
        if (percent_of_mem_soh || percent_of_mem_loh || percent_of_mem_poh)
        {
            if ((percent_of_mem_soh <= 0) || (percent_of_mem_soh >= 100))
            {
                return false;
            }
            if ((percent_of_mem_loh <= 0) || (percent_of_mem_loh >= 100))
            {
                return false;
            }
            else if ((percent_of_mem_poh < 0) || (percent_of_mem_poh >= 100))
            {
                return false;
            }
            if ((percent_of_mem_soh + percent_of_mem_loh + percent_of_mem_poh) >= 100)
            {
                return false;
            }
            heap_hard_limit_oh[soh] = (size_t)(total_physical_mem * (uint64_t)percent_of_mem_soh / (uint64_t)100);
            heap_hard_limit_oh[loh] = (size_t)(total_physical_mem * (uint64_t)percent_of_mem_loh / (uint64_t)100);
            heap_hard_limit_oh[poh] = (size_t)(total_physical_mem * (uint64_t)percent_of_mem_poh / (uint64_t)100);

            if (!compute_hard_limit_from_heap_limits())
            {
                return false;
            }
        }
#ifndef HOST_64BIT
        else
        {
            // need to consider overflows
            if (heap_hard_limit > max_heap_hard_limit / 2)
            {
                return false;
            }
        }
#endif //!HOST_64BIT
    }

    if (heap_hard_limit_oh[soh] && (!heap_hard_limit_oh[poh]) && (!use_large_pages_p))
    {
        return false;
    }

    if (!(heap_hard_limit))
    {
        uint32_t percent_of_mem = (uint32_t)GCConfig::GetGCHeapHardLimitPercent();
        if ((percent_of_mem > 0) && (percent_of_mem < 100))
        {
            heap_hard_limit = (size_t)(total_physical_mem * (uint64_t)percent_of_mem / (uint64_t)100);

#ifndef HOST_64BIT
            // need to consider overflows
            if (heap_hard_limit > max_heap_hard_limit / 2)
            {
                return false;
            }
#endif //!HOST_64BIT
        }
    }

    return true;
}

bool gc_heap::compute_memory_settings(bool is_initialization, uint32_t& nhp, uint32_t nhp_from_config, size_t& seg_size_from_config, size_t new_current_total_committed)
{
#ifdef HOST_64BIT
    // If the hard limit is specified, the user is saying even if the process is already
    // running in a container, use this limit for the GC heap.
    if (!hard_limit_config_p)
    {
        if (is_restricted_physical_mem)
        {
            uint64_t physical_mem_for_gc = total_physical_mem * (uint64_t)75 / (uint64_t)100;
#ifndef USE_REGIONS
            // Establishing a heap_hard_limit when we don't already have one requires
            // us to figure out how many bytes are committed for what purposes. This is going
            // to be very tedious for segments and therefore we chose not to support this scenario.
            if (is_initialization)
#endif //USE_REGIONS
            {
                heap_hard_limit = (size_t)max ((uint64_t)(20 * 1024 * 1024), physical_mem_for_gc);
            }
        }
    }
#endif //HOST_64BIT

    if (heap_hard_limit && (heap_hard_limit < new_current_total_committed))
    {
        return false;
    }

#ifdef USE_REGIONS
    {
#else
    // Changing segment size in the hard limit case for segments is not supported
    if (is_initialization)
    {
#endif //USE_REGIONS
        if (heap_hard_limit)
        {
            if (is_initialization && (!nhp_from_config))
            {
                nhp = adjust_heaps_hard_limit (nhp);
            }

            seg_size_from_config = (size_t)GCConfig::GetSegmentSize();
            if (seg_size_from_config)
            {
                seg_size_from_config = use_large_pages_p ? align_on_segment_hard_limit (seg_size_from_config) :
#ifdef HOST_64BIT
                    round_up_power2 (seg_size_from_config);
#else //HOST_64BIT
                    round_down_power2 (seg_size_from_config);
                seg_size_from_config = min (seg_size_from_config, max_heap_hard_limit / 2);
#endif //HOST_64BIT
            }

            // On 32bit we have next guarantees:
            //   0 <= seg_size_from_config <= 1Gb (from max_heap_hard_limit/2)
            // a) heap-specific limits:
            //   0 <= (heap_hard_limit = heap_hard_limit_oh[soh] + heap_hard_limit_oh[loh] + heap_hard_limit_oh[poh]) < 4Gb (from gc_heap::compute_hard_limit_from_heap_limits)
            //   0 <= heap_hard_limit_oh[soh] <= 1Gb or < 2Gb
            //   0 <= soh_segment_size <= 1Gb or <= 2Gb (alignment and round up)
            // b) same limit for all heaps:
            //   0 <= heap_hard_limit <= 1Gb
            //   0 <= soh_segment_size <= 1Gb
            size_t limit_to_check = (heap_hard_limit_oh[soh] ? heap_hard_limit_oh[soh] : heap_hard_limit);
            soh_segment_size = max (adjust_segment_size_hard_limit (limit_to_check, nhp), seg_size_from_config);
        }
        else
        {
            soh_segment_size = get_valid_segment_size();
        }
    }

    mem_one_percent = total_physical_mem / 100;
#ifndef MULTIPLE_HEAPS
    mem_one_percent /= g_num_processors;
#endif //!MULTIPLE_HEAPS

    uint32_t highmem_th_from_config = (uint32_t)GCConfig::GetGCHighMemPercent();
    if (highmem_th_from_config)
    {
        high_memory_load_th = min (99u, highmem_th_from_config);
        v_high_memory_load_th = min (99u, (high_memory_load_th + 7));
#ifdef FEATURE_EVENT_TRACE
        high_mem_percent_from_config = highmem_th_from_config;
#endif //FEATURE_EVENT_TRACE
    }
    else
    {
        // We should only use this if we are in the "many process" mode which really is only applicable
        // to very powerful machines - before that's implemented, temporarily I am only enabling this for 80GB+ memory.
        // For now I am using an estimate to calculate these numbers but this should really be obtained
        // programmatically going forward.
        // I am assuming 47 processes using WKS GC and 3 using SVR GC.
        // I am assuming 3 in part due to the "very high memory load" is 97%.
        int available_mem_th = 10;
        if (total_physical_mem >= ((uint64_t)80 * 1024 * 1024 * 1024))
        {
            int adjusted_available_mem_th = 3 + (int)((float)47 / (float)g_num_processors);
            available_mem_th = min (available_mem_th, adjusted_available_mem_th);
        }

        high_memory_load_th = 100 - available_mem_th;
        v_high_memory_load_th = 97;
    }

    m_high_memory_load_th = min ((high_memory_load_th + 5), v_high_memory_load_th);
    almost_high_memory_load_th = (high_memory_load_th > 5) ? (high_memory_load_th - 5) : 1; // avoid underflow of high_memory_load_th - 5

    GCConfig::SetGCHighMemPercent (high_memory_load_th);

    return true;
}

int gc_heap::refresh_memory_limit()
{
    refresh_memory_limit_status status = refresh_success;

    if (GCConfig::GetGCTotalPhysicalMemory() != 0)
    {
        return (int)status;
    }

    GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);

    uint32_t nhp_from_config = static_cast<uint32_t>(GCConfig::GetHeapCount());
#ifdef MULTIPLE_HEAPS
    uint32_t nhp = n_heaps;
#else
    uint32_t nhp = 1;
#endif //MULTIPLE_HEAPS
    size_t seg_size_from_config;

    bool old_is_restricted_physical_mem = is_restricted_physical_mem;
    uint64_t old_total_physical_mem = total_physical_mem;
    size_t old_heap_hard_limit = heap_hard_limit;
    size_t old_heap_hard_limit_soh = heap_hard_limit_oh[soh];
    size_t old_heap_hard_limit_loh = heap_hard_limit_oh[loh];
    size_t old_heap_hard_limit_poh = heap_hard_limit_oh[poh];
    bool old_hard_limit_config_p = hard_limit_config_p;

    total_physical_mem = GCToOSInterface::GetPhysicalMemoryLimit (&is_restricted_physical_mem);

    bool succeed = true;

#ifdef USE_REGIONS
    GCConfig::RefreshHeapHardLimitSettings();

    if (!compute_hard_limit())
    {
        succeed = false;
        status = refresh_hard_limit_invalid;
    }
    hard_limit_config_p = heap_hard_limit != 0;
#else
    size_t new_current_total_committed = 0;
#endif //USE_REGIONS

    if (succeed && !compute_memory_settings(false, nhp, nhp_from_config, seg_size_from_config, current_total_committed))
    {
        succeed = false;
        status = refresh_hard_limit_too_low;
    }

    if (!succeed)
    {
        is_restricted_physical_mem = old_is_restricted_physical_mem;
        total_physical_mem = old_total_physical_mem;
        heap_hard_limit = old_heap_hard_limit;
        heap_hard_limit_oh[soh] = old_heap_hard_limit_soh;
        heap_hard_limit_oh[loh] = old_heap_hard_limit_loh;
        heap_hard_limit_oh[poh] = old_heap_hard_limit_poh;
        hard_limit_config_p = old_hard_limit_config_p;
    }
#ifdef COMMITTED_BYTES_SHADOW
    else
    {
        decommit_lock.Enter();
        verify_committed_bytes ();
        decommit_lock.Leave();
    }
#endif //COMMITTED_BYTES_SHADOW

    GCToEEInterface::RestartEE(TRUE);

    return (int)status;
}
