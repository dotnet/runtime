// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

wait_full_gc_status gc_heap::full_gc_wait (GCEvent *event, int time_out_ms)
{
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = gc_heap::g_heaps[0];
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    if (hp->fgn_maxgen_percent == 0)
    {
        return wait_full_gc_na;
    }

    uint32_t wait_result = user_thread_wait(event, FALSE, time_out_ms);

    if ((wait_result == WAIT_OBJECT_0) || (wait_result == WAIT_TIMEOUT))
    {
        if (hp->fgn_maxgen_percent == 0)
        {
            return wait_full_gc_cancelled;
        }

        if (wait_result == WAIT_OBJECT_0)
        {
#ifdef BACKGROUND_GC
            if (fgn_last_gc_was_concurrent)
            {
                fgn_last_gc_was_concurrent = FALSE;
                return wait_full_gc_na;
            }
            else
#endif //BACKGROUND_GC
            {
                return wait_full_gc_success;
            }
        }
        else
        {
            return wait_full_gc_timeout;
        }
    }
    else
    {
        return wait_full_gc_failed;
    }
}

void gc_heap::update_end_gc_time_per_heap()
{
    for (int gen_number = 0; gen_number <= settings.condemned_generation; gen_number++)
    {
        dynamic_data* dd = dynamic_data_of (gen_number);

        if (heap_number == 0)
        {
            dprintf (3, ("prev gen%d GC end time: prev start %I64d + prev gc elapsed %Id = %I64d",
                gen_number, dd_previous_time_clock (dd), dd_gc_elapsed_time (dd), (dd_previous_time_clock (dd) + dd_gc_elapsed_time (dd))));
        }

        dd_gc_elapsed_time (dd) = (size_t)(end_gc_time - dd_time_clock (dd));

        if (heap_number == 0)
        {
            dprintf (3, ("updated NGC%d %Id elapsed time to %I64d - %I64d = %I64d", gen_number, dd_gc_clock (dd), end_gc_time, dd_time_clock (dd), dd_gc_elapsed_time (dd)));
        }
    }
}

void gc_heap::update_end_ngc_time()
{
    end_gc_time = GetHighPrecisionTimeStamp();
    last_alloc_reset_suspended_end_time = end_gc_time;

#ifdef HEAP_BALANCE_INSTRUMENTATION
    last_gc_end_time_us = end_gc_time;
    dprintf (HEAP_BALANCE_LOG, ("[GC#%zd-%zd-%zd]", settings.gc_index,
        (last_gc_end_time_us - dd_time_clock (dynamic_data_of (0))),
        dd_time_clock (dynamic_data_of (0))));
#endif //HEAP_BALANCE_INSTRUMENTATION
}

//internal part of gc used by the serial and concurrent version
void gc_heap::gc1()
{
#ifdef BACKGROUND_GC
    assert (settings.concurrent == (uint32_t)(bgc_thread_id.IsCurrentThread()));
#endif //BACKGROUND_GC

    verify_soh_segment_list();

    int n = settings.condemned_generation;

    if (settings.reason == reason_pm_full_gc)
    {
        assert (n == max_generation);
        init_records();

        gen_to_condemn_tuning* local_condemn_reasons = &(get_gc_data_per_heap()->gen_to_condemn_reasons);
        local_condemn_reasons->init();
        local_condemn_reasons->set_gen (gen_initial, n);
        local_condemn_reasons->set_gen (gen_final_per_heap, n);
    }

    update_collection_counts ();

#ifdef BACKGROUND_GC
    bgc_alloc_lock->check();
#endif //BACKGROUND_GC

    free_list_info (max_generation, "beginning");

    vm_heap->GcCondemnedGeneration = settings.condemned_generation;

    assert (g_gc_card_table == card_table);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    assert (g_gc_card_bundle_table == card_bundle_table);
#endif

    {
#ifndef USE_REGIONS
        if (n == max_generation)
        {
            gc_low = lowest_address;
            gc_high = highest_address;
        }
        else
        {
            gc_low = generation_allocation_start (generation_of (n));
            gc_high = heap_segment_reserved (ephemeral_heap_segment);
        }
#endif //USE_REGIONS

#ifdef BACKGROUND_GC
        if (settings.concurrent)
        {
#ifdef TRACE_GC
            time_bgc_last = GetHighPrecisionTimeStamp();
#endif //TRACE_GC

            FIRE_EVENT(BGCBegin);

            concurrent_print_time_delta ("BGC");

            concurrent_print_time_delta ("RW");
            background_mark_phase();
            free_list_info (max_generation, "after mark phase");

            background_sweep();
            free_list_info (max_generation, "after sweep phase");
        }
        else
#endif //BACKGROUND_GC
        {
            mark_phase (n);

            check_gen0_bricks();

            GCScan::GcRuntimeStructuresValid (FALSE);
            plan_phase (n);
            GCScan::GcRuntimeStructuresValid (TRUE);

            check_gen0_bricks();
        }
    }

    //adjust the allocation size from the pinned quantities.
    for (int gen_number = 0; gen_number <= min ((int)max_generation,n+1); gen_number++)
    {
        generation* gn = generation_of (gen_number);
        if (settings.compaction)
        {
            generation_allocation_size (generation_of (gen_number)) += generation_pinned_allocation_compact_size (gn);
        }
        else
        {
            generation_allocation_size (generation_of (gen_number)) += generation_pinned_allocation_sweep_size (gn);
        }
        generation_pinned_allocation_sweep_size (gn) = 0;
        generation_pinned_allocation_compact_size (gn) = 0;
    }

#ifdef BACKGROUND_GC
    if (settings.concurrent)
    {
        dynamic_data* dd = dynamic_data_of (n);
        end_gc_time = GetHighPrecisionTimeStamp();
        size_t time_since_last_gen2 = 0;

#ifdef DYNAMIC_HEAP_COUNT
        if ((heap_number == 0) && (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes))
        {
            time_since_last_gen2 = (size_t)(end_gc_time - (dd_previous_time_clock (dd) + dd_gc_elapsed_time (dd)));
            dprintf (6666, ("BGC %Id end %I64d - (prev gen2 start %I64d + elapsed %Id = %I64d) = time inbewteen gen2 %Id",
                dd_gc_clock (dd), end_gc_time, dd_previous_time_clock (dd), dd_gc_elapsed_time (dd), (dd_previous_time_clock (dd) + dd_gc_elapsed_time (dd)), time_since_last_gen2));
        }
#endif //DYNAMIC_HEAP_COUNT

        dd_gc_elapsed_time (dd) = (size_t)(end_gc_time - dd_time_clock (dd));
#ifdef DYNAMIC_HEAP_COUNT
        if ((heap_number == 0) && (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes))
        {
            dprintf (6666, ("updating BGC %Id elapsed time to %I64d - %I64d = %I64d", dd_gc_clock (dd), end_gc_time, dd_time_clock (dd), dd_gc_elapsed_time (dd)));

            float bgc_percent = (float)dd_gc_elapsed_time (dd) * 100.0f / (float)time_since_last_gen2;
            dynamic_heap_count_data_t::gen2_sample& g2_sample = dynamic_heap_count_data.gen2_samples[dynamic_heap_count_data.gen2_sample_index];
            g2_sample.gc_index = VolatileLoadWithoutBarrier (&(settings.gc_index));
            g2_sample.gc_duration = dd_gc_elapsed_time (dd);
            g2_sample.gc_percent = bgc_percent;
            dprintf (6666, ("gen2 sample %d elapsed %Id * 100 / time inbetween gen2 %Id = %.3f",
                dynamic_heap_count_data.gen2_sample_index, dd_gc_elapsed_time (dd), time_since_last_gen2, bgc_percent));
            dynamic_heap_count_data.gen2_sample_index = (dynamic_heap_count_data.gen2_sample_index + 1) % dynamic_heap_count_data_t::sample_size;
            (dynamic_heap_count_data.current_gen2_samples_count)++;
            gc_index_full_gc_end = dd_gc_clock (dynamic_data_of (0));

            calculate_new_heap_count ();
        }
#endif //DYNAMIC_HEAP_COUNT

#ifdef HEAP_BALANCE_INSTRUMENTATION
        if (heap_number == 0)
        {
            last_gc_end_time_us = end_gc_time;
            dprintf (HEAP_BALANCE_LOG, ("[GC#%zd-%zd-BGC]", settings.gc_index, dd_gc_elapsed_time (dd)));
        }
#endif //HEAP_BALANCE_INSTRUMENTATION

        free_list_info (max_generation, "after computing new dynamic data");

        gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();

        for (int gen_number = 0; gen_number < max_generation; gen_number++)
        {
            dprintf (2, ("end of BGC: gen%d new_alloc: %zd",
                         gen_number, dd_desired_allocation (dynamic_data_of (gen_number))));
            current_gc_data_per_heap->gen_data[gen_number].size_after = generation_size (gen_number);
            current_gc_data_per_heap->gen_data[gen_number].free_list_space_after = generation_free_list_space (generation_of (gen_number));
            current_gc_data_per_heap->gen_data[gen_number].free_obj_space_after = generation_free_obj_space (generation_of (gen_number));
        }
    }
    else
#endif //BACKGROUND_GC
    {
        free_list_info (max_generation, "end");
        for (int gen_number = 0; gen_number <= n; gen_number++)
        {
            compute_new_dynamic_data (gen_number);
        }

        if (n != max_generation)
        {
            for (int gen_number = (n + 1); gen_number < total_generation_count; gen_number++)
            {
                get_gc_data_per_heap()->gen_data[gen_number].size_after = generation_size (gen_number);
                get_gc_data_per_heap()->gen_data[gen_number].free_list_space_after = generation_free_list_space (generation_of (gen_number));
                get_gc_data_per_heap()->gen_data[gen_number].free_obj_space_after = generation_free_obj_space (generation_of (gen_number));
            }
        }

        get_gc_data_per_heap()->maxgen_size_info.running_free_list_efficiency = (uint32_t)(generation_allocator_efficiency_percent (generation_of (max_generation)));

        free_list_info (max_generation, "after computing new dynamic data");
    }

    if (n < max_generation)
    {
        int highest_gen_number =
#ifdef USE_REGIONS
            max_generation;
#else //USE_REGIONS
            1 + n;
#endif //USE_REGIONS

        for (int older_gen_idx = (1 + n); older_gen_idx <= highest_gen_number; older_gen_idx++)
        {
            compute_in (older_gen_idx);

            dynamic_data* dd = dynamic_data_of (older_gen_idx);
            size_t new_fragmentation = generation_free_list_space (generation_of (older_gen_idx)) +
                                       generation_free_obj_space (generation_of (older_gen_idx));

#ifdef BACKGROUND_GC
            if ((older_gen_idx != max_generation) || (current_c_gc_state != c_gc_state_planning))
#endif //BACKGROUND_GC
            {
                if (settings.promotion)
                {
                    dd_fragmentation (dd) = new_fragmentation;
                }
                else
                {
                    //assert (dd_fragmentation (dd) == new_fragmentation);
                }
            }
        }
    }

#ifdef BACKGROUND_GC
    if (!settings.concurrent)
#endif //BACKGROUND_GC
    {
#ifndef FEATURE_NATIVEAOT
        // GCToEEInterface::IsGCThread() always returns false on NativeAOT, but this assert is useful in CoreCLR.
        assert(GCToEEInterface::IsGCThread());
#endif // FEATURE_NATIVEAOT
        adjust_ephemeral_limits();
    }

#if defined(BACKGROUND_GC) && !defined(USE_REGIONS)
    assert (ephemeral_low == generation_allocation_start (generation_of ( max_generation -1)));
    assert (ephemeral_high == heap_segment_reserved (ephemeral_heap_segment));
#endif //BACKGROUND_GC && !USE_REGIONS

    if (fgn_maxgen_percent)
    {
        if (settings.condemned_generation == (max_generation - 1))
        {
            check_for_full_gc (max_generation - 1, 0);
        }
        else if (settings.condemned_generation == max_generation)
        {
            if (full_gc_approach_event_set
#ifdef MULTIPLE_HEAPS
                && (heap_number == 0)
#endif //MULTIPLE_HEAPS
                )
            {
                dprintf (2, ("FGN-GC: setting gen2 end event"));

                full_gc_approach_event.Reset();
#ifdef BACKGROUND_GC
                // By definition WaitForFullGCComplete only succeeds if it's full, *blocking* GC, otherwise need to return N/A
                fgn_last_gc_was_concurrent = settings.concurrent ? TRUE : FALSE;
#endif //BACKGROUND_GC
                full_gc_end_event.Set();
                full_gc_approach_event_set = false;
            }
        }
    }

#ifdef BACKGROUND_GC
    if (!settings.concurrent)
#endif //BACKGROUND_GC
    {
        //decide on the next allocation quantum
        if (alloc_contexts_used >= 1)
        {
            allocation_quantum = Align (min ((size_t)CLR_SIZE,
                                            (size_t)max ((size_t)1024, get_new_allocation (0) / (2 * alloc_contexts_used))),
                                            get_alignment_constant(FALSE));
            dprintf (3, ("New allocation quantum: %zd(0x%zx)", allocation_quantum, allocation_quantum));
        }
    }
#ifdef USE_REGIONS
    if (end_gen0_region_space == uninitialized_end_gen0_region_space)
    {
        end_gen0_region_space = get_gen0_end_space (memory_type_reserved);
    }
#endif //USE_REGIONS

    descr_generations ("END");

    verify_soh_segment_list();

#ifdef BACKGROUND_GC
    if (gc_can_use_concurrent)
    {
        check_bgc_mark_stack_length();
    }
    assert (settings.concurrent == (uint32_t)(bgc_thread_id.IsCurrentThread()));
#endif //BACKGROUND_GC

#if defined(VERIFY_HEAP) || (defined (FEATURE_EVENT_TRACE) && defined(BACKGROUND_GC))
    if (FALSE
#ifdef VERIFY_HEAP
        // Note that right now g_pConfig->GetHeapVerifyLevel always returns the same
        // value. If we ever allow randomly adjusting this as the process runs,
        // we cannot call it this way as joins need to match - we must have the same
        // value for all heaps like we do with bgc_heap_walk_for_etw_p.
        || (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
#endif
#if defined(FEATURE_EVENT_TRACE) && defined(BACKGROUND_GC)
        || (bgc_heap_walk_for_etw_p && settings.concurrent)
#endif
        )
    {
#ifdef BACKGROUND_GC
        bool cooperative_mode = true;

        if (settings.concurrent)
        {
            cooperative_mode = enable_preemptive ();

#ifdef MULTIPLE_HEAPS
            bgc_t_join.join(this, gc_join_suspend_ee_verify);
            if (bgc_t_join.joined())
            {
                bgc_threads_sync_event.Reset();

                dprintf(2, ("Joining BGC threads to suspend EE for verify heap"));
                bgc_t_join.restart();
            }
            if (heap_number == 0)
            {
                // need to take the gc_lock in preparation for verify_heap below
                // *before* we suspend the EE, otherwise we get a deadlock
                enter_gc_lock_for_verify_heap();

                suspend_EE();
                bgc_threads_sync_event.Set();
            }
            else
            {
                bgc_threads_sync_event.Wait(INFINITE, FALSE);
                dprintf (2, ("bgc_threads_sync_event is signalled"));
            }
#else //MULTIPLE_HEAPS
            // need to take the gc_lock in preparation for verify_heap below
            // *before* we suspend the EE, otherwise we get a deadlock
            enter_gc_lock_for_verify_heap();

            suspend_EE();
#endif //MULTIPLE_HEAPS

            //fix the allocation area so verify_heap can proceed.
            fix_allocation_contexts (FALSE);
        }

        assert (settings.concurrent == (uint32_t)(bgc_thread_id.IsCurrentThread()));
#ifdef FEATURE_EVENT_TRACE
        if (bgc_heap_walk_for_etw_p && settings.concurrent)
        {
            GCToEEInterface::DiagWalkBGCSurvivors(__this);

#ifdef MULTIPLE_HEAPS
            bgc_t_join.join(this, gc_join_after_profiler_heap_walk);
            if (bgc_t_join.joined())
            {
                bgc_t_join.restart();
            }
#endif // MULTIPLE_HEAPS
        }
#endif // FEATURE_EVENT_TRACE
#endif //BACKGROUND_GC

#ifdef VERIFY_HEAP
        if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
            verify_heap (FALSE);
#endif // VERIFY_HEAP

#ifdef BACKGROUND_GC
        if (settings.concurrent)
        {
            repair_allocation_contexts (TRUE);

#ifdef MULTIPLE_HEAPS
            bgc_t_join.join(this, gc_join_restart_ee_verify);
            if (bgc_t_join.joined())
            {
                bgc_threads_sync_event.Reset();

                dprintf(2, ("Joining BGC threads to restart EE after verify heap"));
                bgc_t_join.restart();
            }
            if (heap_number == 0)
            {
                restart_EE();
                leave_gc_lock_for_verify_heap();
                bgc_threads_sync_event.Set();
            }
            else
            {
                bgc_threads_sync_event.Wait(INFINITE, FALSE);
                dprintf (2, ("bgc_threads_sync_event is signalled"));
            }
#else //MULTIPLE_HEAPS

            restart_EE();
            leave_gc_lock_for_verify_heap();
#endif //MULTIPLE_HEAPS

            disable_preemptive (cooperative_mode);
        }
#endif //BACKGROUND_GC
    }
#endif //VERIFY_HEAP || (FEATURE_EVENT_TRACE && BACKGROUND_GC)

#ifdef MULTIPLE_HEAPS
    if (!settings.concurrent)
    {
        gc_t_join.join(this, gc_join_done);
        if (gc_t_join.joined ())
        {
            gc_heap::internal_gc_done = false;

            //equalize the new desired size of the generations
            int limit = settings.condemned_generation;
            if (limit == max_generation)
            {
                limit = total_generation_count-1;
            }

            for (int gen = 0; gen <= limit; gen++)
            {
                size_t total_desired = 0;
                size_t total_already_consumed = 0;

                for (int i = 0; i < gc_heap::n_heaps; i++)
                {
                    gc_heap* hp = gc_heap::g_heaps[i];
                    dynamic_data* dd = hp->dynamic_data_of (gen);
                    size_t temp_total_desired = total_desired + dd_desired_allocation (dd);
                    if (temp_total_desired < total_desired)
                    {
                        // we overflowed.
                        total_desired = (size_t)MAX_PTR;
                        break;
                    }
                    total_desired = temp_total_desired;
                    // for gen 1 and gen 2, there may have been some incoming size
                    // already accounted for
                    assert ((ptrdiff_t)dd_desired_allocation (dd) >= dd_new_allocation (dd));
                    size_t already_consumed = dd_desired_allocation (dd) - dd_new_allocation (dd);
                    size_t temp_total_already_consumed = total_already_consumed + already_consumed;

                    // we should never have an overflow here as the consumed size should always fit in a size_t
                    assert (temp_total_already_consumed >= total_already_consumed);
                    total_already_consumed = temp_total_already_consumed;
                }

                size_t desired_per_heap = Align (total_desired/gc_heap::n_heaps, get_alignment_constant (gen <= max_generation));

                size_t already_consumed_per_heap = total_already_consumed / gc_heap::n_heaps;

                if (gen == 0)
                {
                    // to avoid spikes in mem usage due to short terms fluctuations in survivorship,
                    // apply some smoothing.
                    size_t desired_per_heap_before_smoothing = desired_per_heap;
                    desired_per_heap = exponential_smoothing (gen, dd_collection_count (dynamic_data_of(gen)), desired_per_heap);
                    size_t desired_per_heap_after_smoothing = desired_per_heap;

                    if (!heap_hard_limit
#ifdef DYNAMIC_HEAP_COUNT
                        && (dynamic_adaptation_mode != dynamic_adaptation_to_application_sizes)
#endif //DYNAMIC_HEAP_COUNT
                        )
                    {
                        // if desired_per_heap is close to min_gc_size, trim it
                        // down to min_gc_size to stay in the cache
                        gc_heap* hp = gc_heap::g_heaps[0];
                        dynamic_data* dd = hp->dynamic_data_of (gen);
                        size_t min_gc_size = dd_min_size(dd);
                        // if min GC size larger than true on die cache, then don't bother
                        // limiting the desired size
                        if ((min_gc_size <= GCToOSInterface::GetCacheSizePerLogicalCpu(TRUE)) &&
                            desired_per_heap <= 2*min_gc_size)
                        {
                            desired_per_heap = min_gc_size;
                        }
                    }
#ifdef HOST_64BIT
                    size_t desired_per_heap_before_trim = desired_per_heap;
                    desired_per_heap = joined_youngest_desired (desired_per_heap);

                    dprintf (6666, ("final gen0 bcs: total desired: %Id (%.3fmb/heap), before smooth %zd -> after smooth %zd -> after joined %zd",
                        total_desired, ((double)(total_desired / n_heaps)/ 1000.0 / 1000.0),
                        desired_per_heap_before_smoothing, desired_per_heap_after_smoothing, desired_per_heap));
#endif // HOST_64BIT
                    gc_data_global.final_youngest_desired = desired_per_heap;
                }
#if 1 //subsumed by the linear allocation model
                if (gen >= uoh_start_generation)
                {
                    // to avoid spikes in mem usage due to short terms fluctuations in survivorship,
                    // apply some smoothing.
                    desired_per_heap = exponential_smoothing (gen, dd_collection_count (dynamic_data_of (max_generation)), desired_per_heap);
                }
#endif //0
                for (int i = 0; i < gc_heap::n_heaps; i++)
                {
                    gc_heap* hp = gc_heap::g_heaps[i];
                    dynamic_data* dd = hp->dynamic_data_of (gen);
                    dd_desired_allocation (dd) = desired_per_heap;
                    dd_gc_new_allocation (dd) = desired_per_heap;
#ifdef USE_REGIONS
                    // we may have had some incoming objects during this GC -
                    // adjust the consumed budget for these
                    dd_new_allocation (dd) = desired_per_heap - already_consumed_per_heap;
#else //USE_REGIONS
                    // for segments, we want to keep the .NET 6.0 behavior where we did not adjust
                    dd_new_allocation (dd) = desired_per_heap;
#endif //USE_REGIONS

                    if (gen == 0)
                    {
                        hp->fgn_last_alloc = desired_per_heap;
                    }
                }
            }

#ifdef FEATURE_LOH_COMPACTION
            BOOL all_heaps_compacted_p = TRUE;
#endif //FEATURE_LOH_COMPACTION
            int max_gen0_must_clear_bricks = 0;
            for (int i = 0; i < gc_heap::n_heaps; i++)
            {
                gc_heap* hp = gc_heap::g_heaps[i];
                hp->rearrange_uoh_segments();
#ifdef FEATURE_LOH_COMPACTION
                all_heaps_compacted_p &= hp->loh_compacted_p;
#endif //FEATURE_LOH_COMPACTION
                // compute max of gen0_must_clear_bricks over all heaps
                max_gen0_must_clear_bricks = max(max_gen0_must_clear_bricks, hp->gen0_must_clear_bricks);
            }
            verify_committed_bytes_per_heap ();

#ifdef USE_REGIONS
            initGCShadow();
            verify_region_to_generation_map ();
            compute_gc_and_ephemeral_range (settings.condemned_generation, true);
            stomp_write_barrier_ephemeral (ephemeral_low, ephemeral_high,
                                           map_region_to_generation_skewed, (uint8_t)min_segment_size_shr);
#endif //USE_REGIONS

#ifdef FEATURE_LOH_COMPACTION
            check_loh_compact_mode (all_heaps_compacted_p);
#endif //FEATURE_LOH_COMPACTION

            // if max_gen0_must_clear_bricks > 0, distribute to all heaps -
            // if one heap encountered an interior pointer during this GC,
            // the next GC might see one on another heap
            if (max_gen0_must_clear_bricks > 0)
            {
                for (int i = 0; i < gc_heap::n_heaps; i++)
                {
                    gc_heap* hp = gc_heap::g_heaps[i];
                    hp->gen0_must_clear_bricks = max_gen0_must_clear_bricks;
                }
            }

#ifdef DYNAMIC_HEAP_COUNT
            if (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
            {
                update_total_soh_stable_size();

                if ((settings.condemned_generation == max_generation) && trigger_bgc_for_rethreading_p)
                {
                    trigger_bgc_for_rethreading_p = false;
                }

                process_datas_sample();
            }
#endif //DYNAMIC_HEAP_COUNT

            for (int i = 0; i < gc_heap::n_heaps; i++)
            {
                gc_heap* hp = gc_heap::g_heaps[i];
                hp->decommit_ephemeral_segment_pages();
                hp->descr_generations ("END");
            }

            fire_pevents();

#ifdef USE_REGIONS
            distribute_free_regions();
            age_free_regions ("END");
#endif //USE_REGIONS

            update_end_ngc_time();
            pm_full_gc_init_or_clear();

            gc_t_join.restart();
        }

        update_end_gc_time_per_heap();
        add_to_history_per_heap();
        alloc_context_count = 0;
        heap_select::mark_heap (heap_number);
    }
#else //MULTIPLE_HEAPS
    gc_data_global.final_youngest_desired =
        dd_desired_allocation (dynamic_data_of (0));

#ifdef FEATURE_LOH_COMPACTION
    check_loh_compact_mode (loh_compacted_p);
#endif //FEATURE_LOH_COMPACTION

#ifndef USE_REGIONS
    decommit_ephemeral_segment_pages();
#endif

    fire_pevents();

    if (!(settings.concurrent))
    {
        rearrange_uoh_segments();
        verify_committed_bytes_per_heap ();
#ifdef USE_REGIONS
        initGCShadow();
        verify_region_to_generation_map ();
        compute_gc_and_ephemeral_range (settings.condemned_generation, true);
        stomp_write_barrier_ephemeral (ephemeral_low, ephemeral_high,
                                        map_region_to_generation_skewed, (uint8_t)min_segment_size_shr);
        distribute_free_regions();
        age_free_regions ("END");
#endif //USE_REGIONS

        update_end_ngc_time();
        update_end_gc_time_per_heap();
        add_to_history_per_heap();
        do_post_gc();
    }

    pm_full_gc_init_or_clear();

#ifdef BACKGROUND_GC
    recover_bgc_settings();
#endif //BACKGROUND_GC
#endif //MULTIPLE_HEAPS
#ifdef USE_REGIONS
    if (!(settings.concurrent) && (settings.condemned_generation == max_generation))
    {
        last_gc_before_oom = FALSE;
    }
#endif //USE_REGIONS
}

//update counters
void gc_heap::update_collection_counts ()
{
    dynamic_data* dd0 = dynamic_data_of (0);
    dd_gc_clock (dd0) += 1;

    uint64_t now = GetHighPrecisionTimeStamp();

    for (int i = 0; i <= settings.condemned_generation;i++)
    {
        dynamic_data* dd = dynamic_data_of (i);
        dd_collection_count (dd)++;
        //this is needed by the linear allocation model
        if (i == max_generation)
        {
            dd_collection_count (dynamic_data_of (loh_generation))++;
            dd_collection_count(dynamic_data_of(poh_generation))++;
        }

        dd_gc_clock (dd) = dd_gc_clock (dd0);
        dd_previous_time_clock (dd) = dd_time_clock (dd);
        dd_time_clock (dd) = now;
    }
}

void gc_heap::pm_full_gc_init_or_clear()
{
    // This means the next GC will be a full blocking GC and we need to init.
    if (settings.condemned_generation == (max_generation - 1))
    {
        if (pm_trigger_full_gc)
        {
#ifdef MULTIPLE_HEAPS
            do_post_gc();
#endif //MULTIPLE_HEAPS
            dprintf (GTC_LOG, ("init for PM triggered full GC"));
            uint32_t saved_entry_memory_load = settings.entry_memory_load;
            settings.init_mechanisms();
            settings.reason = reason_pm_full_gc;
            settings.condemned_generation = max_generation;
            settings.entry_memory_load = saved_entry_memory_load;
            // Can't assert this since we only check at the end of gen2 GCs,
            // during gen1 the memory load could have already dropped.
            // Although arguably we should just turn off PM then...
            //assert (settings.entry_memory_load >= high_memory_load_th);
            assert (settings.entry_memory_load > 0);
            settings.gc_index = settings.gc_index + 1;
            do_pre_gc();
        }
    }
    // This means we are in the progress of a full blocking GC triggered by
    // this PM mode.
    else if (settings.reason == reason_pm_full_gc)
    {
        assert (settings.condemned_generation == max_generation);
        assert (pm_trigger_full_gc);
        pm_trigger_full_gc = false;

        dprintf (GTC_LOG, ("PM triggered full GC done"));
    }
}

void gc_heap::garbage_collect_pm_full_gc()
{
    assert (settings.condemned_generation == max_generation);
    assert (settings.reason == reason_pm_full_gc);
    assert (!settings.concurrent);
    gc1();
}

void gc_heap::garbage_collect (int n)
{
    gc_pause_mode saved_settings_pause_mode = settings.pause_mode;

    //reset the number of alloc contexts
    alloc_contexts_used = 0;

    fix_allocation_contexts (TRUE);
#ifdef MULTIPLE_HEAPS
#ifdef JOIN_STATS
    gc_t_join.start_ts(this);
#endif //JOIN_STATS
    check_gen0_bricks();
    clear_gen0_bricks();
#endif //MULTIPLE_HEAPS

    if ((settings.pause_mode == pause_no_gc) && current_no_gc_region_info.minimal_gc_p)
    {
#ifdef MULTIPLE_HEAPS
        gc_t_join.join(this, gc_join_minimal_gc);
        if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
        {
#ifndef USE_REGIONS
#ifdef MULTIPLE_HEAPS
            // this is serialized because we need to get a segment
            for (int i = 0; i < n_heaps; i++)
            {
                if (!(g_heaps[i]->expand_soh_with_minimal_gc()))
                    current_no_gc_region_info.start_status = start_no_gc_no_memory;
            }
#else
            if (!expand_soh_with_minimal_gc())
                current_no_gc_region_info.start_status = start_no_gc_no_memory;
#endif //MULTIPLE_HEAPS
#endif //!USE_REGIONS

            update_collection_counts_for_no_gc();

#ifdef MULTIPLE_HEAPS
            gc_start_event.Reset();
            gc_t_join.restart();
#endif //MULTIPLE_HEAPS
        }

        goto done;
    }

    init_records();

    settings.reason = gc_trigger_reason;
    num_pinned_objects = 0;

#ifdef STRESS_HEAP
    if (settings.reason == reason_gcstress)
    {
        settings.reason = reason_induced;
        settings.stress_induced = TRUE;
    }
#endif // STRESS_HEAP

#ifdef MULTIPLE_HEAPS
#ifdef STRESS_DYNAMIC_HEAP_COUNT
    Interlocked::Increment (&heaps_in_this_gc);
#endif //STRESS_DYNAMIC_HEAP_COUNT
    //align all heaps on the max generation to condemn
    dprintf (3, ("Joining for max generation to condemn"));
    condemned_generation_num = generation_to_condemn (n,
                                                      &blocking_collection,
                                                      &elevation_requested,
                                                      FALSE);
    gc_t_join.join(this, gc_join_generation_determined);
    if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef FEATURE_BASICFREEZE
        seg_table->delete_old_slots();
#endif //FEATURE_BASICFREEZE

#ifndef USE_REGIONS
        copy_brick_card_table_on_growth ();
#endif //!USE_REGIONS

#ifdef MULTIPLE_HEAPS
#ifdef STRESS_DYNAMIC_HEAP_COUNT
        dprintf (9999, ("%d heaps, join sees %d, actually joined %d, %d idle threads (%d)",
            n_heaps, gc_t_join.get_num_threads (), heaps_in_this_gc,
            VolatileLoadWithoutBarrier(&dynamic_heap_count_data.idle_thread_count), (n_max_heaps - n_heaps)));
        if (heaps_in_this_gc != n_heaps)
        {
            dprintf (9999, ("should have %d heaps but actually have %d!!", n_heaps, heaps_in_this_gc));
            GCToOSInterface::DebugBreak ();
        }

        heaps_in_this_gc = 0;
#endif //STRESS_DYNAMIC_HEAP_COUNT

        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
            hp->delay_free_segments();
        }
#else //MULTIPLE_HEAPS
        delay_free_segments();
#endif //MULTIPLE_HEAPS

        BOOL should_evaluate_elevation = TRUE;
        BOOL should_do_blocking_collection = FALSE;

#ifdef MULTIPLE_HEAPS
        int gen_max = condemned_generation_num;
        for (int i = 0; i < n_heaps; i++)
        {
            if (gen_max < g_heaps[i]->condemned_generation_num)
                gen_max = g_heaps[i]->condemned_generation_num;
            if (should_evaluate_elevation && !(g_heaps[i]->elevation_requested))
                should_evaluate_elevation = FALSE;
            if ((!should_do_blocking_collection) && (g_heaps[i]->blocking_collection))
                should_do_blocking_collection = TRUE;
        }

        settings.condemned_generation = gen_max;
#else //MULTIPLE_HEAPS
        settings.condemned_generation = generation_to_condemn (n,
                                                            &blocking_collection,
                                                            &elevation_requested,
                                                            FALSE);
        should_evaluate_elevation = elevation_requested;
        should_do_blocking_collection = blocking_collection;
#endif //MULTIPLE_HEAPS

        settings.condemned_generation = joined_generation_to_condemn (
                                            should_evaluate_elevation,
                                            n,
                                            settings.condemned_generation,
                                            &should_do_blocking_collection
                                            STRESS_HEAP_ARG(n)
                                            );

        STRESS_LOG1(LF_GCROOTS|LF_GC|LF_GCALLOC, LL_INFO10,
                "condemned generation num: %d\n", settings.condemned_generation);

        record_gcs_during_no_gc();

        if (settings.condemned_generation > 1)
            settings.promotion = TRUE;

#ifdef HEAP_ANALYZE
        // At this point we've decided what generation is condemned
        // See if we've been requested to analyze survivors after the mark phase
        if (GCToEEInterface::AnalyzeSurvivorsRequested(settings.condemned_generation))
        {
            heap_analyze_enabled = TRUE;
        }
#endif // HEAP_ANALYZE

        GCToEEInterface::DiagGCStart(settings.condemned_generation, is_induced (settings.reason));

#ifdef BACKGROUND_GC
        if ((settings.condemned_generation == max_generation) &&
            (should_do_blocking_collection == FALSE) &&
            gc_can_use_concurrent &&
            !temp_disable_concurrent_p &&
            ((settings.pause_mode == pause_interactive) || (settings.pause_mode == pause_sustained_low_latency)))
        {
            keep_bgc_threads_p = TRUE;
            c_write (settings.concurrent, TRUE);
            memset (&bgc_data_global, 0, sizeof(bgc_data_global));
            memcpy (&bgc_data_global, &gc_data_global, sizeof(gc_data_global));
        }
#endif //BACKGROUND_GC

        settings.gc_index = (uint32_t)dd_collection_count (dynamic_data_of (0)) + 1;

#ifdef MULTIPLE_HEAPS
        hb_log_balance_activities();
        hb_log_new_allocation();
#endif //MULTIPLE_HEAPS

        // Call the EE for start of GC work
        GCToEEInterface::GcStartWork (settings.condemned_generation,
                                max_generation);

        // TODO: we could fire an ETW event to say this GC as a concurrent GC but later on due to not being able to
        // create threads or whatever, this could be a non concurrent GC. Maybe for concurrent GC we should fire
        // it in do_background_gc and if it failed to be a CGC we fire it in gc1... in other words, this should be
        // fired in gc1.
        do_pre_gc();

#ifdef MULTIPLE_HEAPS
        dprintf (9999, ("in GC, resetting gc_start"));
        gc_start_event.Reset();
        dprintf(3, ("Starting all gc threads for gc"));
        gc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    descr_generations ("BEGIN");
#if defined(TRACE_GC) && defined(USE_REGIONS)
    if (heap_number == 0)
    {
#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap *hp = g_heaps[i];
#else //MULTIPLE_HEAPS
        {
            gc_heap* hp = pGenGCHeap;
            const int i = 0;
#endif //MULTIPLE_HEAPS
            if (settings.condemned_generation == max_generation)
            {
                // print all kinds of free regions
                region_free_list::print(hp->free_regions, i, "BEGIN");
            }
            else
            {
                // print only basic free regions
                hp->free_regions[basic_free_region].print (i, "BEGIN");
            }
        }
    }
#endif // TRACE_GC && USE_REGIONS

#ifdef VERIFY_HEAP
    if ((GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC) &&
       !(GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_POST_GC_ONLY))
    {
        verify_heap (TRUE);
    }
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_BARRIERCHECK)
        checkGCWriteBarrier();
#endif // VERIFY_HEAP

#ifdef BACKGROUND_GC
    if (settings.concurrent)
    {
        // We need to save the settings because we'll need to restore it after each FGC.
        assert (settings.condemned_generation == max_generation);
        settings.compaction = FALSE;
        saved_bgc_settings = settings;

#ifdef MULTIPLE_HEAPS
        if (heap_number == 0)
        {
#ifdef DYNAMIC_HEAP_COUNT
            size_t current_gc_index = VolatileLoadWithoutBarrier (&settings.gc_index);
            if (!bgc_init_gc_index)
            {
                assert (!bgc_init_n_heaps);
                bgc_init_gc_index = current_gc_index;
                bgc_init_n_heaps = (short)n_heaps;
            }
            size_t saved_bgc_th_count_created = bgc_th_count_created;
            size_t saved_bgc_th_count_created_th_existed = bgc_th_count_created_th_existed;
            size_t saved_bgc_th_count_creation_failed = bgc_th_count_creation_failed;
#endif //DYNAMIC_HEAP_COUNT

            // This is the count of threads that GCToEEInterface::CreateThread reported successful for.
            int total_bgc_threads_running = 0;
            for (int i = 0; i < n_heaps; i++)
            {
                gc_heap* hp = g_heaps[i];
                if (prepare_bgc_thread (hp))
                {
                    assert (hp->bgc_thread_running);
                    if (!hp->bgc_thread_running)
                    {
                        dprintf (6666, ("h%d prepare succeeded but running is still false!", i));
                        GCToOSInterface::DebugBreak();
                    }
                    total_bgc_threads_running++;
                }
                else
                {
                    break;
                }
            }

#ifdef DYNAMIC_HEAP_COUNT
            // Even if we don't do a BGC, we need to record how many threads were successfully created because those will
            // be running.
            total_bgc_threads = max (total_bgc_threads, total_bgc_threads_running);

            if (total_bgc_threads_running != n_heaps)
            {
                dprintf (6666, ("wanted to have %d BGC threads but only have %d", n_heaps, total_bgc_threads_running));
            }

            add_to_bgc_th_creation_history (current_gc_index,
                (bgc_th_count_created - saved_bgc_th_count_created),
                (bgc_th_count_created_th_existed - saved_bgc_th_count_created_th_existed),
                (bgc_th_count_creation_failed - saved_bgc_th_count_creation_failed));
#endif //DYNAMIC_HEAP_COUNT

            dprintf (2, ("setting bgc_threads_sync_event"));
            bgc_threads_sync_event.Set();
        }
        else
        {
            bgc_threads_sync_event.Wait(INFINITE, FALSE);
            dprintf (2, ("bgc_threads_sync_event is signalled"));
        }
#else
        prepare_bgc_thread(0);
#endif //MULTIPLE_HEAPS

#ifdef MULTIPLE_HEAPS
        gc_t_join.join(this, gc_join_start_bgc);
        if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
        {
            do_concurrent_p = TRUE;
            do_ephemeral_gc_p = FALSE;
#ifdef MULTIPLE_HEAPS
            dprintf(2, ("Joined to perform a background GC"));

            for (int i = 0; i < n_heaps; i++)
            {
                gc_heap* hp = g_heaps[i];

                if (!(hp->bgc_thread_running))
                {
                    assert (!(hp->bgc_thread));
                }

                // In theory we could be in a situation where bgc_thread_running is false but bgc_thread is non NULL. We don't
                // support this scenario so don't do a BGC.
                if (!(hp->bgc_thread_running && hp->bgc_thread && hp->commit_mark_array_bgc_init()))
                {
                    do_concurrent_p = FALSE;
                    break;
                }
                else
                {
                    hp->background_saved_lowest_address = hp->lowest_address;
                    hp->background_saved_highest_address = hp->highest_address;
                }
            }
#else
            do_concurrent_p = (bgc_thread_running && commit_mark_array_bgc_init());
            if (do_concurrent_p)
            {
                background_saved_lowest_address = lowest_address;
                background_saved_highest_address = highest_address;
            }
#endif //MULTIPLE_HEAPS

#ifdef DYNAMIC_HEAP_COUNT
            dprintf (6666, ("last BGC saw %d heaps and %d total threads, currently %d heaps and %d total threads, %s BGC",
                last_bgc_n_heaps, last_total_bgc_threads, n_heaps, total_bgc_threads, (do_concurrent_p ? "doing" : "not doing")));
#endif //DYNAMIC_HEAP_COUNT

            if (do_concurrent_p)
            {
#ifdef DYNAMIC_HEAP_COUNT
                int diff = n_heaps - last_bgc_n_heaps;
                if (diff > 0)
                {
                    int saved_idle_bgc_thread_count = dynamic_heap_count_data.idle_bgc_thread_count;
                    int max_idle_event_count = min (n_heaps, last_total_bgc_threads);
                    int idle_events_to_set = max_idle_event_count - last_bgc_n_heaps;
                    if (idle_events_to_set > 0)
                    {
                        Interlocked::ExchangeAdd (&dynamic_heap_count_data.idle_bgc_thread_count, -idle_events_to_set);
                        dprintf (6666, ("%d BGC threads exist, setting %d idle events for h%d-h%d, total idle %d -> %d",
                            total_bgc_threads, idle_events_to_set, last_bgc_n_heaps, (last_bgc_n_heaps + idle_events_to_set - 1),
                            saved_idle_bgc_thread_count, VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_bgc_thread_count)));
                        for (int heap_idx = last_bgc_n_heaps; heap_idx < max_idle_event_count; heap_idx++)
                        {
                            g_heaps[heap_idx]->bgc_idle_thread_event.Set();
                        }
                    }
                }

                last_bgc_n_heaps = n_heaps;
                last_total_bgc_threads = total_bgc_threads;
#endif //DYNAMIC_HEAP_COUNT

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
                SoftwareWriteWatch::EnableForGCHeap();
#endif //FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifdef MULTIPLE_HEAPS
                for (int i = 0; i < n_heaps; i++)
                    g_heaps[i]->current_bgc_state = bgc_initialized;
#else
                current_bgc_state = bgc_initialized;
#endif //MULTIPLE_HEAPS

                int gen = check_for_ephemeral_alloc();
                // always do a gen1 GC before we start BGC.
                dont_restart_ee_p = TRUE;
                if (gen == -1)
                {
                    // If we decide to not do a GC before the BGC we need to
                    // restore the gen0 alloc context.
#ifdef MULTIPLE_HEAPS
                    for (int i = 0; i < n_heaps; i++)
                    {
                        generation_allocation_pointer (g_heaps[i]->generation_of (0)) =  0;
                        generation_allocation_limit (g_heaps[i]->generation_of (0)) = 0;
                    }
#else
                    generation_allocation_pointer (youngest_generation) =  0;
                    generation_allocation_limit (youngest_generation) = 0;
#endif //MULTIPLE_HEAPS
                }
                else
                {
                    do_ephemeral_gc_p = TRUE;

                    settings.init_mechanisms();
                    settings.condemned_generation = gen;

#ifdef DYNAMIC_HEAP_COUNT
                    if (trigger_bgc_for_rethreading_p)
                    {
                        settings.condemned_generation = 0;
                    }
#endif //DYNAMIC_HEAP_COUNT

                    settings.gc_index = (size_t)dd_collection_count (dynamic_data_of (0)) + 2;
                    do_pre_gc();

                    // TODO BACKGROUND_GC need to add the profiling stuff here.
                    dprintf (GTC_LOG, ("doing gen%d before doing a bgc", gen));
                }

                //clear the cards so they don't bleed in gen 1 during collection
                // shouldn't this always be done at the beginning of any GC?
                //clear_card_for_addresses (
                //    generation_allocation_start (generation_of (0)),
                //    heap_segment_allocated (ephemeral_heap_segment));

                if (!do_ephemeral_gc_p)
                {
                    do_background_gc();
                }
            }
            else
            {
                settings.compaction = TRUE;
                c_write (settings.concurrent, FALSE);
            }

#ifdef MULTIPLE_HEAPS
            gc_t_join.restart();
#endif //MULTIPLE_HEAPS
        }

        if (do_concurrent_p)
        {
            // At this point we are sure we'll be starting a BGC, so save its per heap data here.
            // global data is only calculated at the end of the GC so we don't need to worry about
            // FGCs overwriting it.
            memset (&bgc_data_per_heap, 0, sizeof (bgc_data_per_heap));
            memcpy (&bgc_data_per_heap, &gc_data_per_heap, sizeof(gc_data_per_heap));

            if (do_ephemeral_gc_p)
            {
                dprintf (2, ("GC threads running, doing gen%d GC", settings.condemned_generation));

                gen_to_condemn_reasons.init();
                gen_to_condemn_reasons.set_condition (gen_before_bgc);
                gc_data_per_heap.gen_to_condemn_reasons.init (&gen_to_condemn_reasons);
                gc1();
#ifdef MULTIPLE_HEAPS
                gc_t_join.join(this, gc_join_bgc_after_ephemeral);
                if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
                {
#ifdef MULTIPLE_HEAPS
                    do_post_gc();
#endif //MULTIPLE_HEAPS
                    settings = saved_bgc_settings;
                    assert (settings.concurrent);

                    do_background_gc();

#ifdef MULTIPLE_HEAPS
                    gc_t_join.restart();
#endif //MULTIPLE_HEAPS
                }
            }
        }
        else
        {
            dprintf (2, ("couldn't create BGC threads, reverting to doing a blocking GC"));
            gc1();
        }
    }
    else
#endif //BACKGROUND_GC
    {
        gc1();
    }
#ifndef MULTIPLE_HEAPS
    allocation_running_time = GCToOSInterface::GetLowPrecisionTimeStamp();
    allocation_running_amount = dd_new_allocation (dynamic_data_of (0));
    fgn_last_alloc = dd_new_allocation (dynamic_data_of (0));
#endif //MULTIPLE_HEAPS

done:
    if (saved_settings_pause_mode == pause_no_gc)
        allocate_for_no_gc_after_gc();
}

#ifdef USE_REGIONS
// recompute ephemeral range - it may have become too large because of temporary allocation
// and deallocation of regions
void gc_heap::compute_gc_and_ephemeral_range (int condemned_gen_number, bool end_of_gc_p)
{
    ephemeral_low = MAX_PTR;
    ephemeral_high = nullptr;
    gc_low = MAX_PTR;
    gc_high = nullptr;
    if (condemned_gen_number >= soh_gen2 || end_of_gc_p)
    {
        gc_low = g_gc_lowest_address;
        gc_high = g_gc_highest_address;
    }
    if (end_of_gc_p)
    {
#if 1
        // simple and safe value
        ephemeral_low = g_gc_lowest_address;
#else
        // conservative value - should still avoid changing
        // ephemeral bounds in the write barrier while app is running
        // scan our address space for a region that is either free
        // or in an ephemeral generation
        uint8_t* addr = g_gc_lowest_address;
        while (true)
        {
            heap_segment* region = get_region_info (addr);
            if (is_free_region (region))
                break;
            if (heap_segment_gen_num (region) <= soh_gen1)
                break;
            addr += ((size_t)1) << min_segment_size_shr;
        }
        ephemeral_low = addr;
#endif
        ephemeral_high = g_gc_highest_address;
    }
    else
    {
        for (int gen_number = soh_gen0; gen_number <= soh_gen1; gen_number++)
        {
#ifdef MULTIPLE_HEAPS
            for (int i = 0; i < n_heaps; i++)
            {
                gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
            {
                gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
                generation *gen = hp->generation_of (gen_number);
                for (heap_segment *region = generation_start_segment (gen); region != nullptr; region = heap_segment_next (region))
                {
                    ephemeral_low = min ((uint8_t*)ephemeral_low, get_region_start (region));
                    ephemeral_high = max ((uint8_t*)ephemeral_high, heap_segment_reserved (region));
                    if (gen_number <= condemned_gen_number)
                    {
                        gc_low = min (gc_low, get_region_start (region));
                        gc_high = max (gc_high, heap_segment_reserved (region));
                    }
                }
            }
        }
    }
    dprintf (2, ("ephemeral_low = %p, ephemeral_high = %p, gc_low = %p, gc_high = %p", (uint8_t*)ephemeral_low, (uint8_t*)ephemeral_high, gc_low, gc_high));
}

#endif //USE_REGIONS

void gc_heap::do_pre_gc()
{
    STRESS_LOG_GC_STACK;

#ifdef STRESS_LOG
    STRESS_LOG_GC_START(VolatileLoad(&settings.gc_index),
                        (uint32_t)settings.condemned_generation,
                        (uint32_t)settings.reason);
#endif // STRESS_LOG

#ifdef MULTIPLE_HEAPS
    gc_heap* hp = g_heaps[0];
#else
    gc_heap* hp = 0;
#endif //MULTIPLE_HEAPS

#ifdef BACKGROUND_GC
    settings.b_state = hp->current_bgc_state;
    if (settings.concurrent)
    {
        last_bgc_info_index = !last_bgc_info_index;
        last_bgc_info[last_bgc_info_index].index = settings.gc_index;
    }
#endif //BACKGROUND_GC

#ifdef TRACE_GC
    size_t total_allocated_since_last_gc[total_oh_count];
    get_total_allocated_since_last_gc (total_allocated_since_last_gc);
    bool compatibleWithStressLog = true;
#ifdef SIMPLE_DPRINTF
    compatibleWithStressLog = false;
#endif //SIMPLE_DPRINTF
    bgc_state b_state = bgc_not_in_process;
#ifdef BACKGROUND_GC
    b_state = settings.b_state;
#endif //BACKGROUND_GC

    size_t heap_size_before = get_total_heap_size();
    uint64_t start_gc_time = GetHighPrecisionTimeStamp();
    uint64_t elapsed_since_last_gc_us = start_gc_time - last_alloc_reset_suspended_end_time;
    max_peak_heap_size = max (max_peak_heap_size, heap_size_before);

    dprintf (6666, (ThreadStressLog::gcDetailedStartMsg(compatibleWithStressLog),
        VolatileLoad(&settings.gc_index),
        dd_collection_count (hp->dynamic_data_of (0)),
        settings.condemned_generation,
        (elapsed_since_last_gc_us / 1000.0),
        total_allocated_since_last_gc[gc_oh_num::soh],
        (dd_desired_allocation (hp->dynamic_data_of (0)) * n_heaps),
        dd_desired_allocation (hp->dynamic_data_of (0)),
        (elapsed_since_last_gc_us ? (total_allocated_since_last_gc[gc_oh_num::soh] / 1000.0 / elapsed_since_last_gc_us) : 0),
        total_allocated_since_last_gc[gc_oh_num::loh],
        (elapsed_since_last_gc_us ? (total_allocated_since_last_gc[gc_oh_num::loh] / 1000.0 / elapsed_since_last_gc_us) : 0),
        total_allocated_since_last_gc[gc_oh_num::poh],
        (elapsed_since_last_gc_us ? (total_allocated_since_last_gc[gc_oh_num::poh] / 1000.0 / elapsed_since_last_gc_us) : 0),
        get_str_gc_type(),
        b_state,
        n_heaps
        SIMPLE_DPRINTF_ARG(heap_size_before / 1000.0 / 1000.0)
        SIMPLE_DPRINTF_ARG(max_peak_heap_size / 1000.0 / 1000.0)));

    if (heap_hard_limit)
    {
        size_t total_heap_committed = get_total_committed_size();
        size_t total_heap_committed_recorded = current_total_committed - current_total_committed_bookkeeping;
        dprintf (1, ("(%d)GC commit BEG #%zd: %zd (recorded: %zd = %zd-%zd)",
            settings.condemned_generation,
            (size_t)settings.gc_index, total_heap_committed, total_heap_committed_recorded,
            current_total_committed, current_total_committed_bookkeeping));
    }
#endif //TRACE_GC

    GCHeap::UpdatePreGCCounters();
    fire_committed_usage_event();

#if defined(__linux__)
    GCToEEInterface::UpdateGCEventStatus(static_cast<int>(GCEventStatus::GetEnabledLevel(GCEventProvider_Default)),
                                         static_cast<int>(GCEventStatus::GetEnabledKeywords(GCEventProvider_Default)),
                                         static_cast<int>(GCEventStatus::GetEnabledLevel(GCEventProvider_Private)),
                                         static_cast<int>(GCEventStatus::GetEnabledKeywords(GCEventProvider_Private)));
#endif // __linux__

    if (settings.concurrent)
    {
#ifdef BACKGROUND_GC
        full_gc_counts[gc_type_background]++;
#endif // BACKGROUND_GC
    }
    else
    {
        if (settings.condemned_generation == max_generation)
        {
            full_gc_counts[gc_type_blocking]++;
        }
        else
        {
#ifdef BACKGROUND_GC
            if (settings.background_p)
            {
                ephemeral_fgc_counts[settings.condemned_generation]++;
            }
#endif //BACKGROUND_GC
        }
    }
}

void gc_heap::do_post_gc()
{
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = g_heaps[0];
#else
    gc_heap* hp = 0;
#endif //MULTIPLE_HEAPS

    GCToEEInterface::GcDone(settings.condemned_generation);

    GCToEEInterface::DiagGCEnd(VolatileLoad(&settings.gc_index),
                         (uint32_t)settings.condemned_generation,
                         (uint32_t)settings.reason,
                         !!settings.concurrent);

    add_to_history();

    uint32_t current_memory_load = 0;

#ifdef BGC_SERVO_TUNING
    if (bgc_tuning::enable_fl_tuning)
    {
        uint64_t current_available_physical = 0;
        size_t gen2_physical_size = 0;
        size_t gen3_physical_size = 0;
        ptrdiff_t gen2_virtual_fl_size = 0;
        ptrdiff_t gen3_virtual_fl_size = 0;
        ptrdiff_t vfl_from_kp = 0;
        ptrdiff_t vfl_from_ki = 0;

        gen2_physical_size = get_total_generation_size (max_generation);
        gen3_physical_size = get_total_generation_size (loh_generation);

        get_memory_info (&current_memory_load, &current_available_physical);
        if ((settings.condemned_generation == max_generation) && !settings.concurrent)
        {
            double gen2_size_ratio = (double)gen2_physical_size / ((double)gen2_physical_size + (double)gen3_physical_size);

            double total_virtual_fl_size = bgc_tuning::calculate_ml_tuning (current_available_physical, true, &vfl_from_kp, &vfl_from_ki);
            gen2_virtual_fl_size = (ptrdiff_t)(total_virtual_fl_size * gen2_size_ratio);
            gen3_virtual_fl_size = (ptrdiff_t)(total_virtual_fl_size * (1.0 - gen2_size_ratio));

#ifdef SIMPLE_DPRINTF
            dprintf (BGC_TUNING_LOG, ("BTL: ml: %d (g: %d)(%s), a: %zd (g: %zd, elg: %zd+%zd=%zd, %zd+%zd=%zd), vfl: %zd=%zd+%zd(NGC2)",
                current_memory_load, bgc_tuning::memory_load_goal,
                ((current_available_physical > bgc_tuning::available_memory_goal) ? "above" : "below"),
                current_available_physical, bgc_tuning::available_memory_goal,
                gen2_physical_size, gen2_virtual_fl_size, (gen2_physical_size + gen2_virtual_fl_size),
                gen3_physical_size, gen3_virtual_fl_size, (gen3_physical_size + gen3_virtual_fl_size),
                (ptrdiff_t)total_virtual_fl_size, vfl_from_kp, vfl_from_ki));
#endif //SIMPLE_DPRINTF
        }

        check_and_adjust_bgc_tuning (max_generation, gen2_physical_size, gen2_virtual_fl_size);
        check_and_adjust_bgc_tuning (loh_generation, gen3_physical_size, gen3_virtual_fl_size);
    }
#endif //BGC_SERVO_TUNING

    dprintf (6666, (ThreadStressLog::gcDetailedEndMsg(),
        VolatileLoad (&settings.gc_index),
        dd_collection_count (hp->dynamic_data_of (0)),
        (get_total_heap_size() / 1000.0 / 1000.0),
        settings.condemned_generation,
        get_str_gc_type(),
        (settings.compaction ? "C" : "S"),
        (settings.promotion ? "P" : "S"),
        settings.entry_memory_load,
        current_memory_load));

#if defined(TRACE_GC) && defined(SIMPLE_DPRINTF)
    flush_gc_log (false);
#endif //TRACE_GC && SIMPLE_DPRINTF

    // Now record the gc info.
    last_recorded_gc_info* last_gc_info = 0;
#ifdef BACKGROUND_GC
    if (settings.concurrent)
    {
        last_gc_info = &last_bgc_info[last_bgc_info_index];
        assert (last_gc_info->index == settings.gc_index);
    }
    else
#endif //BACKGROUND_GC
    {
        last_gc_info = ((settings.condemned_generation == max_generation) ?
                        &last_full_blocking_gc_info : &last_ephemeral_gc_info);
        last_gc_info->index = settings.gc_index;
    }
    size_t total_heap_committed = get_total_committed_size();
    last_gc_info->total_committed = total_heap_committed;
    last_gc_info->promoted = get_total_promoted();
    last_gc_info->pinned_objects = get_total_pinned_objects();
    last_gc_info->finalize_promoted_objects = GCHeap::GetFinalizablePromotedCount();

    if (!settings.concurrent)
    {
        // If it's a normal blocking GC with its own SuspendEE, we simply get the elapsed time recoreded
        // and add the time between SuspendEE start and GC start.
        dynamic_data* dd = hp->dynamic_data_of (settings.condemned_generation);
        uint64_t gc_start_ts = dd_time_clock (dd);
        size_t pause_duration = (size_t)(end_gc_time - dd_time_clock (dd));

#ifdef BACKGROUND_GC
        if ((hp->current_bgc_state != bgc_initialized) && (settings.reason != reason_pm_full_gc))
        {
            pause_duration += (size_t)(gc_start_ts - suspended_start_time);
        }
#endif //BACKGROUND_GC

        last_gc_info->pause_durations[0] = pause_duration;
        total_suspended_time += pause_duration;
        last_gc_info->pause_durations[1] = 0;
    }

    uint64_t total_process_time = end_gc_time - process_start_time;
    last_gc_info->pause_percentage = (float)(total_process_time ?
        ((double)total_suspended_time / (double)total_process_time * 100.0) : 0);

    update_recorded_gen_data (last_gc_info);
    last_gc_info->heap_size = get_total_heap_size();
    last_gc_info->fragmentation = get_total_fragmentation();
    if (settings.exit_memory_load != 0)
        last_gc_info->memory_load = settings.exit_memory_load;
    else if (settings.entry_memory_load != 0)
        last_gc_info->memory_load = settings.entry_memory_load;
    last_gc_info->condemned_generation = (uint8_t)settings.condemned_generation;
    last_gc_info->compaction = settings.compaction;
    last_gc_info->concurrent = settings.concurrent;

#ifdef BACKGROUND_GC
    is_last_recorded_bgc = settings.concurrent;
#endif //BACKGROUND_GC

#ifdef TRACE_GC
    if (heap_hard_limit)
    {
        size_t total_heap_committed_recorded = current_total_committed - current_total_committed_bookkeeping;
        dprintf (1, ("(%d)GC commit END #%zd: %zd (recorded: %zd=%zd-%zd), heap %zd, frag: %zd",
            settings.condemned_generation,
            (size_t)settings.gc_index, total_heap_committed, total_heap_committed_recorded,
            current_total_committed, current_total_committed_bookkeeping,
            last_gc_info->heap_size, last_gc_info->fragmentation));
    }
#endif //TRACE_GC

    // Note we only do this at the end of full blocking GCs because we do not want
    // to turn on this provisional mode during the middle of a BGC.
    if ((settings.condemned_generation == max_generation) && (!settings.concurrent))
    {
        if (pm_stress_on)
        {
            size_t full_compacting_gc_count = full_gc_counts[gc_type_compacting];
            if (provisional_mode_triggered)
            {
                uint64_t r = gc_rand::get_rand(10);
                if ((full_compacting_gc_count - provisional_triggered_gc_count) >= r)
                {
                    provisional_mode_triggered = false;
                    provisional_off_gc_count = full_compacting_gc_count;
                    dprintf (GTC_LOG, ("%zd NGC2s when turned on, %zd NGCs since(%zd)",
                        provisional_triggered_gc_count, (full_compacting_gc_count - provisional_triggered_gc_count),
                        num_provisional_triggered));
                }
            }
            else
            {
                uint64_t r = gc_rand::get_rand(5);
                if ((full_compacting_gc_count - provisional_off_gc_count) >= r)
                {
                    provisional_mode_triggered = true;
                    provisional_triggered_gc_count = full_compacting_gc_count;
                    num_provisional_triggered++;
                    dprintf (GTC_LOG, ("%zd NGC2s when turned off, %zd NGCs since(%zd)",
                        provisional_off_gc_count, (full_compacting_gc_count - provisional_off_gc_count),
                        num_provisional_triggered));
                }
            }
        }
        else
        {
            if (provisional_mode_triggered)
            {
                if ((settings.entry_memory_load < high_memory_load_th) ||
                    !is_pm_ratio_exceeded())
                {
                    dprintf (GTC_LOG, ("turning off PM"));
                    provisional_mode_triggered = false;
                }
            }
            else if ((settings.entry_memory_load >= high_memory_load_th) && is_pm_ratio_exceeded())
            {
                dprintf (GTC_LOG, ("highmem && highfrag - turning on PM"));
                provisional_mode_triggered = true;
                num_provisional_triggered++;
            }
        }
    }

    if (!settings.concurrent)
    {
        fire_committed_usage_event ();
    }
    GCHeap::UpdatePostGCCounters();

    // We need to reinitialize the number of pinned objects because it's used in the GCHeapStats
    // event fired in GCHeap::UpdatePostGCCounters. For BGC, we will get that event following an
    // FGC's GCHeapStats and we wouldn't want that FGC's info to carry over to the BGC.
    reinit_pinned_objects();

#ifdef STRESS_LOG
    STRESS_LOG_GC_END(VolatileLoad(&settings.gc_index),
                      (uint32_t)settings.condemned_generation,
                      (uint32_t)settings.reason);
#endif // STRESS_LOG

#ifdef GC_CONFIG_DRIVEN
    if (!settings.concurrent)
    {
        if (settings.compaction)
            (compact_or_sweep_gcs[0])++;
        else
            (compact_or_sweep_gcs[1])++;
    }

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
        g_heaps[i]->record_interesting_info_per_heap();
#else
    record_interesting_info_per_heap();
#endif //MULTIPLE_HEAPS

    record_global_mechanisms();
#endif //GC_CONFIG_DRIVEN

    if (mark_list_overflow)
    {
        grow_mark_list();
        mark_list_overflow = false;
    }
}
