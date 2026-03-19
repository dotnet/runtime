// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef USE_REGIONS
#ifdef DYNAMIC_HEAP_COUNT
void gc_heap::check_decommissioned_heap()
{
//  keep the mark stack for the time being
//  assert (mark_stack_array_length             == DECOMMISSIONED_SIZE_T);
//  assert (mark_stack_array                    == DECOMMISSIONED_MARK_P);

    assert (generation_skip_ratio               == DECOMMISSIONED_INT);
    assert (gen0_must_clear_bricks              == DECOMMISSIONED_INT);

    assert (freeable_uoh_segment                == DECOMMISSIONED_REGION_P);

    // TODO: check gen2_alloc_list

#ifdef BACKGROUND_GC
    // keep these fields
    // bgc_thread_id;
    // bgc_thread_running; // gc thread is its main loop
    // bgc_thread;

    // we don't want to hold on to this storage for unused heaps, so zap these fields
    //assert (background_mark_stack_tos           == DECOMMISSIONED_UINT8_T_PP);
    //assert (background_mark_stack_array         == DECOMMISSIONED_UINT8_T_PP);
    //assert (background_mark_stack_array_length  == DECOMMISSIONED_SIZE_T);

    //assert (c_mark_list                         == DECOMMISSIONED_UINT8_T_PP);
    //assert (c_mark_list_length                  == DECOMMISSIONED_SIZE_T);

    assert (freeable_soh_segment                == DECOMMISSIONED_REGION_P);
#endif //BACKGROUND_GC

#ifdef FEATURE_LOH_COMPACTION
    assert (loh_pinned_queue_length             == DECOMMISSIONED_SIZE_T);
    assert (loh_pinned_queue_decay              == DECOMMISSIONED_INT);
    assert (loh_pinned_queue                    == DECOMMISSIONED_MARK_P);
#endif //FEATURE_LOH_COMPACTION

    assert (gen0_bricks_cleared                 == DECOMMISSIONED_BOOL);

    // TODO: check loh_alloc_list
    // TODO: check poh_alloc_list

    assert (alloc_allocated                     == DECOMMISSIONED_UINT8_T_P);
    assert (ephemeral_heap_segment              == DECOMMISSIONED_REGION_P);

    // Keep this field
    // finalize_queue;

#ifdef USE_REGIONS
    // TODO: check free_regions[count_free_region_kinds];
#endif //USE_REGIONS

    assert (more_space_lock_soh.lock            == lock_decommissioned);
    assert (more_space_lock_uoh.lock            == lock_decommissioned);

    assert (soh_allocation_no_gc                == DECOMMISSIONED_SIZE_T);
    assert (loh_allocation_no_gc                == DECOMMISSIONED_SIZE_T);

    for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
    {
        generation* gen = generation_of (gen_idx);

        assert (generation_start_segment                   (gen) == DECOMMISSIONED_REGION_P);
        assert (generation_allocation_segment              (gen) == DECOMMISSIONED_REGION_P);
        assert (generation_tail_region                     (gen) == DECOMMISSIONED_REGION_P);
        assert (generation_tail_ro_region                  (gen) == DECOMMISSIONED_REGION_P);
        assert (generation_allocation_context_start_region (gen) == DECOMMISSIONED_UINT8_T_P);
        assert (generation_free_list_allocated             (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_end_seg_allocated               (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_allocate_end_seg_p              (gen) == DECOMMISSIONED_BOOL);
        assert (generation_condemned_allocated             (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_sweep_allocated                 (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_free_list_space                 (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_free_obj_space                  (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_allocation_size                 (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_pinned_allocation_compact_size  (gen) == DECOMMISSIONED_SIZE_T);
        assert (generation_pinned_allocation_sweep_size    (gen) == DECOMMISSIONED_SIZE_T);
        assert (gen->gen_num                                     == DECOMMISSIONED_INT);

#ifdef DOUBLY_LINKED_FL
        assert (generation_set_bgc_mark_bit_p              (gen) == DECOMMISSIONED_BOOL);
        assert (generation_last_free_list_allocated        (gen) == DECOMMISSIONED_UINT8_T_P);
#endif //DOUBLY_LINKED_FL

        dynamic_data* dd = dynamic_data_of (gen_idx);

        // check if any of the fields have been modified
        assert (dd_new_allocation                  (dd) == DECOMMISSIONED_PTRDIFF_T);
        assert (dd_gc_new_allocation               (dd) == DECOMMISSIONED_PTRDIFF_T);
        assert (dd_surv                     (dd) == (float)DECOMMISSIONED_VALUE);
        assert (dd_desired_allocation              (dd) == DECOMMISSIONED_SIZE_T);

        assert (dd_begin_data_size                 (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_survived_size                   (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_pinned_survived_size            (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_artificial_pinned_survived_size (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_added_pinned_size               (dd) == DECOMMISSIONED_SIZE_T);

#ifdef SHORT_PLUGS
        assert (dd_padding_size                    (dd) == DECOMMISSIONED_SIZE_T);
#endif //SHORT_PLUGS
#if defined (RESPECT_LARGE_ALIGNMENT) || defined (FEATURE_STRUCTALIGN)
        assert (dd_num_npinned_plugs               (dd) == DECOMMISSIONED_SIZE_T);
#endif //RESPECT_LARGE_ALIGNMENT || FEATURE_STRUCTALIGN
        assert (dd_current_size                    (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_collection_count                (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_promoted_size                   (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_freach_previous_promotion       (dd) == DECOMMISSIONED_SIZE_T);

        assert (dd_fragmentation                   (dd) == DECOMMISSIONED_SIZE_T);

        assert (dd_gc_clock                        (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_time_clock                      (dd) == DECOMMISSIONED_SIZE_T);
        assert (dd_previous_time_clock             (dd) == DECOMMISSIONED_SIZE_T);

        assert (dd_gc_elapsed_time                 (dd) == DECOMMISSIONED_SIZE_T);
    }
}

// take a heap out of service, setting its fields to non-sensical value
// to detect inadvertent usage
void gc_heap::decommission_heap()
{
    // avoid race condition where a thread decides to wait on the gc done event just as
    // another thread decides to decommission the heap
    set_gc_done();

//  keep the mark stack for the time being
//  mark_stack_array_length             = DECOMMISSIONED_SIZE_T;
//  mark_stack_array                    = DECOMMISSIONED_MARK_P;

    generation_skip_ratio               = DECOMMISSIONED_INT;
    gen0_must_clear_bricks              = DECOMMISSIONED_INT;

    freeable_uoh_segment                = DECOMMISSIONED_REGION_P;

    memset ((void *)gen2_alloc_list, DECOMMISSIONED_INT, sizeof(gen2_alloc_list[0])*(NUM_GEN2_ALIST - 1));

#ifdef BACKGROUND_GC
    // keep these fields
    // bgc_thread_id;
    // bgc_thread_running; // gc thread is its main loop
    // bgc_thread;

    // We can set these to the decommission value (or wait till they are not used for N GCs before we do that) but if we do we'll
    // need to allocate them in recommission_heap. For now I'm leaving them as they are.
    //background_mark_stack_tos           = DECOMMISSIONED_UINT8_T_PP;
    //background_mark_stack_array         = DECOMMISSIONED_UINT8_T_PP;
    //background_mark_stack_array_length  = DECOMMISSIONED_SIZE_T;

    //c_mark_list                         = DECOMMISSIONED_UINT8_T_PP;
    //c_mark_list_length                  = DECOMMISSIONED_SIZE_T;

    freeable_soh_segment                = DECOMMISSIONED_REGION_P;
#endif //BACKGROUND_GC

#ifdef FEATURE_LOH_COMPACTION
    loh_pinned_queue_length             = DECOMMISSIONED_SIZE_T;
    loh_pinned_queue_decay              = DECOMMISSIONED_INT;
    loh_pinned_queue                    = DECOMMISSIONED_MARK_P;
#endif //FEATURE_LOH_COMPACTION

    gen0_bricks_cleared                 = DECOMMISSIONED_BOOL;

    memset ((void *)loh_alloc_list, DECOMMISSIONED_INT, sizeof(loh_alloc_list));
    memset ((void *)poh_alloc_list, DECOMMISSIONED_INT, sizeof(poh_alloc_list));

    alloc_allocated                     = DECOMMISSIONED_UINT8_T_P;
    ephemeral_heap_segment              = DECOMMISSIONED_REGION_P;

    // Keep this field
    // finalize_queue;

#ifdef USE_REGIONS
    memset ((void *)free_regions, DECOMMISSIONED_INT, sizeof(free_regions));
#endif //USE_REGIONS

    // put the more space locks in the decommissioned state
    assert (more_space_lock_soh.lock    == lock_free);
    more_space_lock_soh.lock            = lock_decommissioned;

    assert (more_space_lock_uoh.lock    == lock_free);
    more_space_lock_uoh.lock            = lock_decommissioned;

    soh_allocation_no_gc                = DECOMMISSIONED_SIZE_T;
    loh_allocation_no_gc                = DECOMMISSIONED_SIZE_T;

    // clear per generation data
    for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
    {
        generation* gen = generation_of (gen_idx);

        // clear the free lists
        generation_allocator (gen)->clear();

        // set some fields in the dynamic data to nonsensical values
        // to catch cases where we inadvertently use or modify them
        memset (generation_alloc_context           (gen),  DECOMMISSIONED_INT, sizeof(alloc_context));

        generation_start_segment                   (gen) = DECOMMISSIONED_REGION_P;
        generation_allocation_segment              (gen) = DECOMMISSIONED_REGION_P;
        generation_allocation_context_start_region (gen) = DECOMMISSIONED_UINT8_T_P;
        generation_tail_region                     (gen) = DECOMMISSIONED_REGION_P;
        generation_tail_ro_region                  (gen) = DECOMMISSIONED_REGION_P;

        generation_free_list_allocated             (gen) = DECOMMISSIONED_SIZE_T;
        generation_end_seg_allocated               (gen) = DECOMMISSIONED_SIZE_T;
        generation_allocate_end_seg_p              (gen) = DECOMMISSIONED_BOOL;
        generation_condemned_allocated             (gen) = DECOMMISSIONED_SIZE_T;
        generation_sweep_allocated                 (gen) = DECOMMISSIONED_SIZE_T;
        generation_free_list_space                 (gen) = DECOMMISSIONED_SIZE_T;
        generation_free_obj_space                  (gen) = DECOMMISSIONED_SIZE_T;
        generation_allocation_size                 (gen) = DECOMMISSIONED_SIZE_T;

        generation_pinned_allocation_compact_size  (gen) = DECOMMISSIONED_SIZE_T;
        generation_pinned_allocation_sweep_size    (gen) = DECOMMISSIONED_SIZE_T;
        gen->gen_num                                     = DECOMMISSIONED_INT;

#ifdef DOUBLY_LINKED_FL
        generation_set_bgc_mark_bit_p              (gen) = DECOMMISSIONED_BOOL;
        generation_last_free_list_allocated        (gen) = DECOMMISSIONED_UINT8_T_P;
#endif //DOUBLY_LINKED_FL

        dynamic_data* dd = dynamic_data_of (gen_idx);

        // set some fields in the dynamic data to nonsensical values
        // to catch cases where we inadvertently use or modify them
        dd_new_allocation                  (dd) = DECOMMISSIONED_SIZE_T;
        dd_gc_new_allocation               (dd) = DECOMMISSIONED_PTRDIFF_T;
        dd_surv                     (dd) = (float)DECOMMISSIONED_VALUE;
        dd_desired_allocation              (dd) = DECOMMISSIONED_SIZE_T;

        dd_begin_data_size                 (dd) = DECOMMISSIONED_SIZE_T;
        dd_survived_size                   (dd) = DECOMMISSIONED_SIZE_T;
        dd_pinned_survived_size            (dd) = DECOMMISSIONED_SIZE_T;
        dd_artificial_pinned_survived_size (dd) = DECOMMISSIONED_SIZE_T;
        dd_added_pinned_size               (dd) = DECOMMISSIONED_SIZE_T;

#ifdef SHORT_PLUGS
        dd_padding_size                    (dd) = DECOMMISSIONED_SIZE_T;
#endif //SHORT_PLUGS
#if defined (RESPECT_LARGE_ALIGNMENT) || defined (FEATURE_STRUCTALIGN)
        dd_num_npinned_plugs               (dd) = DECOMMISSIONED_SIZE_T;
#endif //RESPECT_LARGE_ALIGNMENT || FEATURE_STRUCTALIGN
        dd_current_size                    (dd) = DECOMMISSIONED_SIZE_T;
        dd_collection_count                (dd) = DECOMMISSIONED_SIZE_T;
        dd_promoted_size                   (dd) = DECOMMISSIONED_SIZE_T;
        dd_freach_previous_promotion       (dd) = DECOMMISSIONED_SIZE_T;

        dd_fragmentation                   (dd) = DECOMMISSIONED_SIZE_T;

        dd_gc_clock                        (dd) = DECOMMISSIONED_SIZE_T;
        dd_time_clock                      (dd) = DECOMMISSIONED_SIZE_T;
        dd_previous_time_clock             (dd) = DECOMMISSIONED_SIZE_T;

        dd_gc_elapsed_time                 (dd) = DECOMMISSIONED_SIZE_T;
    }
}

// re-initialize a heap in preparation to putting it back into service
void gc_heap::recommission_heap()
{
    // reinitialize the fields - consider setting the ones initialized
    // by the next GC to UNINITIALIZED_VALUE instead

//  keep the mark stack for the time being
//  mark_stack_array_length             = 0;
//  mark_stack_array                    = nullptr;

    generation_skip_ratio               = 100;
    gen0_must_clear_bricks              = 0;

    freeable_uoh_segment                = nullptr;

    memset ((void *)gen2_alloc_list, 0, sizeof(gen2_alloc_list));

#ifdef BACKGROUND_GC
    // keep these fields
    // bgc_thread_id;
    // bgc_thread_running; // gc thread is its main loop
    // bgc_thread;

    //background_mark_stack_tos           = nullptr;
    //background_mark_stack_array         = nullptr;
    //background_mark_stack_array_length  = 0;

    //c_mark_list                         = nullptr;
    //c_mark_list_length                  = 0;

    freeable_soh_segment                = nullptr;
#endif //BACKGROUND_GC

#ifdef FEATURE_LOH_COMPACTION
    loh_pinned_queue_length             = 0;
    loh_pinned_queue_decay              = 0;
    loh_pinned_queue                    = 0;
#endif //FEATURE_LOH_COMPACTION

    gen0_bricks_cleared                 = FALSE;

    memset ((void *)loh_alloc_list, 0, sizeof(loh_alloc_list));
    memset ((void *)poh_alloc_list, 0, sizeof(poh_alloc_list));

    alloc_allocated                     = 0;
    ephemeral_heap_segment              = nullptr;

    // Keep this field
    // finalize_queue;

    for (int kind = 0; kind < count_free_region_kinds; kind++)
    {
        free_regions[kind].reset();
    }

    // put the more space locks in the free state
    more_space_lock_soh.lock            = lock_free;
    more_space_lock_uoh.lock            = lock_free;

    soh_allocation_no_gc                = 0;
    loh_allocation_no_gc                = 0;

#ifdef BACKGROUND_GC
    // initialize the background GC sync mechanism
    bgc_alloc_lock->init();
#endif //BACKGROUND_GC

    gc_heap* heap0 = g_heaps[0];

    for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
    {
        // clear the free lists for the new heaps
        generation* gen = generation_of (gen_idx);
        generation_allocator (gen)->clear();

        // reinitialize the fields - consider setting the ones initialized
        // by the next GC to UNINITIALIZED_VALUE instead
        memset (generation_alloc_context           (gen), 0, sizeof(alloc_context));

        generation_start_segment                   (gen) = nullptr;
        generation_tail_ro_region                  (gen) = nullptr;
        generation_tail_region                     (gen) = nullptr;
        generation_allocation_segment              (gen) = nullptr;
        generation_allocation_context_start_region (gen) = nullptr;

        generation_free_list_allocated             (gen) = 0;
        generation_end_seg_allocated               (gen) = 0;
        generation_allocate_end_seg_p              (gen) = 0;
        generation_condemned_allocated             (gen) = 0;
        generation_sweep_allocated                 (gen) = 0;
        generation_free_list_space                 (gen) = 0;
        generation_free_obj_space                  (gen) = 0;
        generation_allocation_size                 (gen) = 0;

        generation_pinned_allocation_compact_size  (gen) = 0;
        generation_pinned_allocation_sweep_size    (gen) = 0;
        gen->gen_num                                     = gen_idx;

#ifdef DOUBLY_LINKED_FL
        generation_set_bgc_mark_bit_p              (gen) = FALSE;
        generation_last_free_list_allocated        (gen) = nullptr;
#endif //DOUBLY_LINKED_FL

        dynamic_data* dd = dynamic_data_of (gen_idx);

        dynamic_data* heap0_dd = heap0->dynamic_data_of (gen_idx);

        // copy some fields from heap0

        // this is copied to dd_previous_time_clock at the start of GC
        dd_time_clock     (dd) = dd_time_clock (heap0_dd);

        // this is used at the start of the next gc to update setting.gc_index
        dd_collection_count (dd) = dd_collection_count (heap0_dd);

        // this field is used to estimate the heap size - set it to 0
        // as the data on this heap are accounted for by other heaps
        // until the next gc, where the fields will be re-initialized
        dd_promoted_size                   (dd) = 0;

        // this field is used at the beginning of a GC to decide
        // which generation to condemn - it will be
        // adjusted as free list items are rethreaded onto this heap
        dd_fragmentation                   (dd) = 0;

        // this value will just be incremented, not re-initialized
        dd_gc_clock                        (dd) = dd_gc_clock (heap0_dd);

        // these are used by the allocator, but will be set later
        dd_new_allocation                  (dd) = UNINITIALIZED_VALUE;
        dd_desired_allocation              (dd) = UNINITIALIZED_VALUE;

        // set the fields that are supposed to be set by the next GC to
        // a special value to help in debugging
        dd_gc_new_allocation               (dd) = UNINITIALIZED_VALUE;
        dd_surv                     (dd) = (float)UNINITIALIZED_VALUE;

        dd_begin_data_size                 (dd) = UNINITIALIZED_VALUE;
        dd_survived_size                   (dd) = UNINITIALIZED_VALUE;
        dd_pinned_survived_size            (dd) = UNINITIALIZED_VALUE;
        dd_artificial_pinned_survived_size (dd) = UNINITIALIZED_VALUE;
        dd_added_pinned_size               (dd) = UNINITIALIZED_VALUE;

#ifdef SHORT_PLUGS
        dd_padding_size                    (dd) = UNINITIALIZED_VALUE;
#endif //SHORT_PLUGS
#if defined (RESPECT_LARGE_ALIGNMENT) || defined (FEATURE_STRUCTALIGN)
        dd_num_npinned_plugs               (dd) = UNINITIALIZED_VALUE;
#endif //RESPECT_LARGE_ALIGNMENT || FEATURE_STRUCTALIGN
        dd_current_size                    (dd) = UNINITIALIZED_VALUE;
        dd_freach_previous_promotion       (dd) = UNINITIALIZED_VALUE;

        dd_previous_time_clock             (dd) = UNINITIALIZED_VALUE;

        dd_gc_elapsed_time                 (dd) = UNINITIALIZED_VALUE;
    }

#ifdef SPINLOCK_HISTORY
    spinlock_info_index = 0;
    current_uoh_alloc_state = (allocation_state)-1;
#endif //SPINLOCK_HISTORY

#ifdef RECORD_LOH_STATE
    loh_state_index = 0;
#endif //RECORD_LOH_STATE
}

float median_of_3 (float a, float b, float c)
{
#define compare_and_swap(i, j)          \
        {                               \
            if (i < j)                  \
            {                           \
                float t = i;            \
                          i = j;        \
                              j = t;    \
            }                           \
        }
    compare_and_swap (b, a);
    compare_and_swap (c, a);
    compare_and_swap (c, b);
#undef compare_and_swap
    return b;
}

void gc_heap::calculate_new_heap_count ()
{
    assert (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes);

    dprintf (6666, ("current num of samples %Id (g2: %Id) prev processed %Id (g2: %Id), last full GC happened at index %Id",
        dynamic_heap_count_data.current_samples_count, dynamic_heap_count_data.current_gen2_samples_count,
        dynamic_heap_count_data.processed_samples_count, dynamic_heap_count_data.processed_gen2_samples_count, gc_index_full_gc_end));

    if ((dynamic_heap_count_data.current_samples_count < (dynamic_heap_count_data.processed_samples_count + dynamic_heap_count_data_t::sample_size)) &&
        (dynamic_heap_count_data.current_gen2_samples_count < (dynamic_heap_count_data.processed_gen2_samples_count + dynamic_heap_count_data_t::sample_size)))
    {
        dprintf (6666, ("not enough GCs, skipping"));
        return;
    }

    bool process_eph_samples_p = (dynamic_heap_count_data.current_samples_count >= (dynamic_heap_count_data.processed_samples_count + dynamic_heap_count_data_t::sample_size));
    bool process_gen2_samples_p = (dynamic_heap_count_data.current_gen2_samples_count >= (dynamic_heap_count_data.processed_gen2_samples_count + dynamic_heap_count_data_t::sample_size));

    size_t current_gc_index = VolatileLoadWithoutBarrier (&settings.gc_index);
    float median_gen2_tcp = 0.0f;
    if (dynamic_heap_count_data.current_gen2_samples_count >= (dynamic_heap_count_data.processed_gen2_samples_count + dynamic_heap_count_data_t::sample_size))
    {
        median_gen2_tcp = dynamic_heap_count_data.get_median_gen2_gc_percent ();
    }

    // If there was a blocking gen2 GC, the overhead would be very large and most likely we would not pick it. So we
    // rely on the gen2 sample's overhead calculated above.
    float throughput_cost_percents[dynamic_heap_count_data_t::sample_size];

    if (process_eph_samples_p)
    {
        for (int i = 0; i < dynamic_heap_count_data_t::sample_size; i++)
        {
            dynamic_heap_count_data_t::sample& sample = dynamic_heap_count_data.samples[i];
            assert (sample.elapsed_between_gcs > 0);
            throughput_cost_percents[i] = (sample.elapsed_between_gcs ? (((float)sample.msl_wait_time / n_heaps + sample.gc_pause_time) * 100.0f / (float)sample.elapsed_between_gcs) : 0.0f);
            assert (throughput_cost_percents[i] >= 0.0);
            if (throughput_cost_percents[i] > 100.0)
                throughput_cost_percents[i] = 100.0;
            dprintf (6666, ("sample %d in GC#%Id msl %I64d / %d + pause %I64d / elapsed %I64d = tcp: %.3f, surv %zd, gc speed %zd/ms", i,
                sample.gc_index, sample.msl_wait_time, n_heaps, sample.gc_pause_time, sample.elapsed_between_gcs, throughput_cost_percents[i],
                sample.gc_survived_size, (sample.gc_pause_time ? (sample.gc_survived_size * 1000 / sample.gc_pause_time) : 0)));
        }
    }

    float median_throughput_cost_percent = median_of_3 (throughput_cost_percents[0], throughput_cost_percents[1], throughput_cost_percents[2]);
    float avg_throughput_cost_percent = (float)((throughput_cost_percents[0] + throughput_cost_percents[1] + throughput_cost_percents[2]) / 3.0);

    // One of the reasons for outliers is something temporarily affected GC work. We pick the min tcp if the survival is very stable to avoid counting these outliers.
    float min_tcp = throughput_cost_percents[0];
    size_t min_survived = dynamic_heap_count_data.samples[0].gc_survived_size;
    uint64_t min_pause = dynamic_heap_count_data.samples[0].gc_pause_time;
    for (int i = 1; i < dynamic_heap_count_data_t::sample_size; i++)
    {
        min_tcp = min (throughput_cost_percents[i], min_tcp);
        min_survived = min (dynamic_heap_count_data.samples[i].gc_survived_size, min_survived);
        min_pause = min (dynamic_heap_count_data.samples[i].gc_pause_time, min_pause);
    }

    dprintf (6666, ("checking if samples are stable %Id %Id %Id, min tcp %.3f, min pause %I64d",
        dynamic_heap_count_data.samples[0].gc_survived_size, dynamic_heap_count_data.samples[1].gc_survived_size, dynamic_heap_count_data.samples[2].gc_survived_size,
        min_tcp, min_pause));

    bool survived_stable_p = true;
    if (min_survived > 0)
    {
        for (int i = 0; i < dynamic_heap_count_data_t::sample_size; i++)
        {
            dynamic_heap_count_data_t::sample& sample = dynamic_heap_count_data.samples[i];
            float diff = (float)(sample.gc_survived_size - min_survived) / (float)min_survived;
            dprintf (6666, ("sample %d diff from min is %Id -> %.3f", i, (sample.gc_survived_size - min_survived), diff));
            if (diff >= 0.15)
            {
                survived_stable_p = false;
            }
        }
    }

    if (survived_stable_p)
    {
        dprintf (6666, ("survived is stable, so we pick min tcp %.3f", min_tcp));
        median_throughput_cost_percent = min_tcp;
    }

    dprintf (6666, ("median tcp: %.3f, avg tcp: %.3f, gen2 tcp %.3f(%.3f, %.3f, %.3f)",
        median_throughput_cost_percent, avg_throughput_cost_percent, median_gen2_tcp,
        dynamic_heap_count_data.gen2_samples[0].gc_percent, dynamic_heap_count_data.gen2_samples[1].gc_percent, dynamic_heap_count_data.gen2_samples[2].gc_percent));

    int extra_heaps = (n_max_heaps >= 16) + (n_max_heaps >= 64);
    int actual_n_max_heaps = n_max_heaps - extra_heaps;

#ifdef STRESS_DYNAMIC_HEAP_COUNT
    // quick hack for initial testing
    int new_n_heaps = (int)gc_rand::get_rand (n_max_heaps - 1) + 1;

    // if we are adjusting down, make sure we adjust lower than the lowest uoh msl heap
    if ((new_n_heaps < n_heaps) && (dynamic_heap_count_data.lowest_heap_with_msl_uoh != -1))
    {
        new_n_heaps = min (dynamic_heap_count_data.lowest_heap_with_msl_uoh, new_n_heaps);
        new_n_heaps = max (new_n_heaps, 1);
    }
    dprintf (6666, ("stress %d -> %d", n_heaps, new_n_heaps));
#else //STRESS_DYNAMIC_HEAP_COUNT
    int new_n_heaps = n_heaps;

    float target_tcp = dynamic_heap_count_data.target_tcp;
    float target_gen2_tcp = dynamic_heap_count_data.target_gen2_tcp;

    if (process_eph_samples_p)
    {
        dynamic_heap_count_data.add_to_recorded_tcp (median_throughput_cost_percent);

        float tcp_to_consider = 0.0f;
        int agg_factor = 0;
        size_t total_soh_stable_size = 0;
        int max_heap_count_datas = 0;
        int min_heap_count_datas = 0;
        dynamic_heap_count_data_t::adjust_metric adj_metric = dynamic_heap_count_data_t::adjust_metric::not_adjusted;

        // For diagnostic purpose. need to init these
        dynamic_heap_count_data_t::decide_change_condition change_decision = (dynamic_heap_count_data_t::decide_change_condition)0;
        int recorded_tcp_count = 0;
        float recorded_tcp_slope = 0.0f;
        size_t num_gcs_since_last_change = 0;
        float current_around_target_accumulation = 0.0f;
        dynamic_heap_count_data_t::decide_adjustment_reason adj_reason = (dynamic_heap_count_data_t::decide_adjustment_reason)0;
        int hc_change_freq_factor = 0;
        dynamic_heap_count_data_t::hc_change_freq_reason hc_freq_reason = (dynamic_heap_count_data_t::hc_change_freq_reason)0;

        if (dynamic_heap_count_data.should_change (median_throughput_cost_percent, &tcp_to_consider, current_gc_index,
                                                   &change_decision, &recorded_tcp_count, &recorded_tcp_slope, &num_gcs_since_last_change, &current_around_target_accumulation))
        {
            total_soh_stable_size = get_total_soh_stable_size();
            size_t total_bcd = dynamic_heap_count_data.compute_total_gen0_budget (total_soh_stable_size);
            max_heap_count_datas = (int)(total_bcd / dynamic_heap_count_data.min_gen0_new_allocation);
            min_heap_count_datas = (int)(total_bcd / dynamic_heap_count_data.max_gen0_new_allocation);
            int max_heap_count_growth_step = dynamic_heap_count_data.get_max_growth (n_heaps);
            int max_heap_count_growth_datas = max_heap_count_datas - n_heaps;
            if (max_heap_count_growth_datas < 0)
            {
                max_heap_count_growth_datas = 0;
            }
            int max_heap_count_growth_core = actual_n_max_heaps - n_heaps;
            int max_heap_count_growth = min (max_heap_count_growth_step, min (max_heap_count_growth_datas, max_heap_count_growth_core));

            float distance = tcp_to_consider - target_tcp;

            dprintf (6666, ("median tcp %.3f, recent tcp %.3f - target %.1f = %.3f", median_throughput_cost_percent, tcp_to_consider, target_tcp, distance));

            float diff_pct = distance / target_tcp;
            // Different for above and below target to avoid oscillation.
            float hc_change_factor = (float)((diff_pct > 0.0) ? 1.5 : 3.0);
            float change_float = diff_pct / hc_change_factor * (float)n_heaps;
            float change_float_rounded = (float)round(change_float);
            int change_int = (int)change_float_rounded;
            dprintf (6666, ("diff pct %.3f / %.1f * %d = %d (%.3f), max hc allowed by datas %d | by core %d, max growth per step %d, max growth by datas %d | by core %d",
                diff_pct, hc_change_factor, n_heaps, change_int, ((float)change_int / n_heaps), max_heap_count_datas, actual_n_max_heaps,
                max_heap_count_growth_step, max_heap_count_growth_datas, max_heap_count_growth_core));

            if (change_int > 0)
            {
                // If we do want to grow but the max HC allowed by DATAS is 0, and we haven't done any gen2 GCs yet, we do want to
                // trigger a gen2 right away.
                if (!max_heap_count_growth_datas && !(dynamic_heap_count_data.current_gen2_samples_count))
                {
                    trigger_initial_gen2_p = true;

                    dprintf (6666, ("we want to grow but DATAS is limiting, trigger a gen2 right away"));
#ifdef BACKGROUND_GC
                    if (is_bgc_in_progress())
                    {
                        trigger_initial_gen2_p = false;
                    }
#endif //BACKGROUND_GC
                }

                agg_factor = dynamic_heap_count_data.get_aggressiveness (change_int);
                if (agg_factor > 1)
                {
                    change_int *= agg_factor;
                    dprintf (6666, ("agg factor is %d, change by %d heaps", agg_factor, change_int));
                }
            }

            if (change_int)
            {
                adj_metric = dynamic_heap_count_data.should_change_hc (max_heap_count_datas, min_heap_count_datas,
                                                                       max_heap_count_growth, change_int, current_gc_index,
                                                                       &adj_reason, &hc_change_freq_factor, &hc_freq_reason);

                // If we decide to change budget, we let the next GC calculate the right budget, ie, we delay changing by one GC which is acceptable.
                if (adj_metric != dynamic_heap_count_data_t::adjust_metric::adjust_hc)
                {
                    change_int = 0;
                }

                if (adj_metric != dynamic_heap_count_data_t::adjust_metric::not_adjusted)
                {
                    if (adj_metric == dynamic_heap_count_data_t::adjust_metric::adjust_hc)
                    {
                        new_n_heaps = n_heaps + change_int;
                    }

                    dynamic_heap_count_data.record_adjustment (adj_metric, distance, change_int, current_gc_index);
                }
            }

            // We always need to reset these since we already made decisions based on them.
            dynamic_heap_count_data.reset_accumulation();
            dprintf (6666, ("changing HC or budget %d -> %d at GC#%Id", n_heaps, new_n_heaps, current_gc_index));

            dprintf (6666, ("total max gen %.3fmb, total bcd %.3fmb, diff %% %.3f-> +%d hc (%%%.3f)",
                mb (total_soh_stable_size), mb (total_bcd), diff_pct, change_int, (change_int * 100.0 / n_heaps)));
        }

#ifdef FEATURE_EVENT_TRACE
        GCEventFireSizeAdaptationTuning_V1 (
            (uint16_t)new_n_heaps,
            (uint16_t)max_heap_count_datas,
            (uint16_t)min_heap_count_datas,
            (uint64_t)current_gc_index,
            (uint64_t)total_soh_stable_size,
            (float)median_throughput_cost_percent,
            (float)tcp_to_consider,
            (float)current_around_target_accumulation,
            (uint16_t)recorded_tcp_count,
            (float)recorded_tcp_slope,
            (uint32_t)num_gcs_since_last_change,
            (uint8_t)agg_factor,
            (uint16_t)change_decision,
            (uint16_t)adj_reason,
            (uint16_t)hc_change_freq_factor,
            (uint16_t)hc_freq_reason,
            (uint8_t)adj_metric);
#endif //FEATURE_EVENT_TRACE
    }

    size_t num_gen2s_since_last_change = 0;

    if ((new_n_heaps == n_heaps) && !process_eph_samples_p && process_gen2_samples_p)
    {
        num_gen2s_since_last_change = dynamic_heap_count_data.current_gen2_samples_count - dynamic_heap_count_data.gen2_last_changed_sample_count;
        // If we have already been processing eph samples, we don't need to process gen2.
        if ((dynamic_heap_count_data.current_samples_count / dynamic_heap_count_data.current_gen2_samples_count) < 10)
        {
            int step_up = (n_heaps + 1) / 2;
            int max_growth = max ((n_max_heaps / 4), (1 + (actual_n_max_heaps > 3)));
            step_up = min (step_up, (actual_n_max_heaps - n_heaps));

            int step_down = (n_heaps + 1) / 3;

            // The gen2 samples only serve as a backstop so this is quite crude.
            if (median_gen2_tcp > target_gen2_tcp)
            {
                new_n_heaps += step_up;
                new_n_heaps = min (new_n_heaps, actual_n_max_heaps);
                dprintf (6666, ("[CHP2-0] gen2 tcp: %.3f, inc by %d + %d = %d", median_gen2_tcp, step_up, n_heaps, new_n_heaps));

                if ((new_n_heaps < actual_n_max_heaps) && dynamic_heap_count_data.is_close_to_max (new_n_heaps, actual_n_max_heaps))
                {
                    dprintf (6666, ("[CHP2-1] %d is close to max heaps %d, grow to max", new_n_heaps, actual_n_max_heaps));
                    new_n_heaps = actual_n_max_heaps;
                }
            }
            else if ((median_gen2_tcp < (target_gen2_tcp / 2)) && (num_gen2s_since_last_change > 30))
            {
                new_n_heaps -= step_down;
                dprintf (6666, ("[CHP3-0] last gen2 sample count when changed: %Id, gen2 tcp: %.3f, dec by %d, %d -> %d",
                    dynamic_heap_count_data.gen2_last_changed_sample_count, median_gen2_tcp, step_down, n_heaps, new_n_heaps));
            }

            if (new_n_heaps != n_heaps)
            {
                dynamic_heap_count_data.gen2_last_changed_sample_count = dynamic_heap_count_data.current_gen2_samples_count;
            }
        }
    }

    assert (new_n_heaps >= 1);
    assert (new_n_heaps <= actual_n_max_heaps);

    if (process_eph_samples_p)
    {
        dprintf (6666, ("processed eph samples, updating processed %Id -> %Id", dynamic_heap_count_data.processed_samples_count, dynamic_heap_count_data.current_samples_count));
        dynamic_heap_count_data.processed_samples_count = dynamic_heap_count_data.current_samples_count;
    }

    if (process_gen2_samples_p)
    {
        dynamic_heap_count_data_t::gen2_sample* gen2_samples = dynamic_heap_count_data.gen2_samples;
#ifdef FEATURE_EVENT_TRACE
        GCEventFireSizeAdaptationFullGCTuning_V1 (
            (uint16_t)dynamic_heap_count_data.new_n_heaps,
            (uint64_t)current_gc_index,
            (float)median_gen2_tcp,
            (uint32_t)num_gen2s_since_last_change,
            (uint32_t)(current_gc_index - gen2_samples[0].gc_index),
            (float)gen2_samples[0].gc_percent,
            (uint32_t)(current_gc_index - gen2_samples[1].gc_index),
            (float)gen2_samples[1].gc_percent,
            (uint32_t)(current_gc_index - gen2_samples[2].gc_index),
            (float)gen2_samples[2].gc_percent);
#endif //FEATURE_EVENT_TRACEs

        dprintf (6666, ("processed gen2 samples, updating processed %Id -> %Id", dynamic_heap_count_data.processed_gen2_samples_count, dynamic_heap_count_data.current_gen2_samples_count));
        dynamic_heap_count_data.processed_gen2_samples_count = dynamic_heap_count_data.current_gen2_samples_count;
    }
#endif //STRESS_DYNAMIC_HEAP_COUNT

    if (new_n_heaps != n_heaps)
    {
        dprintf (6666, ("GC#%Id should change! %d->%d (%s)",
            VolatileLoadWithoutBarrier (&settings.gc_index), n_heaps, new_n_heaps, ((n_heaps < new_n_heaps) ? "INC" : "DEC")));
        dynamic_heap_count_data.heap_count_to_change_to = new_n_heaps;
        dynamic_heap_count_data.should_change_heap_count = true;
    }
}

void gc_heap::check_heap_count ()
{
    dynamic_heap_count_data.new_n_heaps = dynamic_heap_count_data.heap_count_to_change_to;

    assert (dynamic_heap_count_data.new_n_heaps != n_heaps);

    if (dynamic_heap_count_data.new_n_heaps != n_heaps)
    {
        dprintf (9999, ("h0 suspending EE in check"));
        // can't have threads allocating while we change the number of heaps
        GCToEEInterface::SuspendEE(SUSPEND_FOR_GC_PREP);
        dprintf (9999, ("h0 suspended EE in check"));

#ifdef BACKGROUND_GC
        if (gc_heap::background_running_p())
        {
            // background GC is running - reset the new heap count
            add_to_hc_history (hc_record_check_cancelled_bgc);
            hc_change_cancelled_count_bgc++;
            dynamic_heap_count_data.new_n_heaps = n_heaps;
            dprintf (6666, ("can't change heap count! BGC in progress"));
        }
#endif //BACKGROUND_GC
    }

    if (dynamic_heap_count_data.new_n_heaps != n_heaps)
    {
        dprintf (6666, ("prep to change from %d to %d at GC#%Id", n_heaps, dynamic_heap_count_data.new_n_heaps, VolatileLoadWithoutBarrier (&settings.gc_index)));
        if (!prepare_to_change_heap_count (dynamic_heap_count_data.new_n_heaps))
        {
            // we don't have sufficient resources - reset the new heap count
            add_to_hc_history (hc_record_check_cancelled_prep);
            hc_change_cancelled_count_prep++;
            dynamic_heap_count_data.new_n_heaps = n_heaps;
        }
    }

    if (dynamic_heap_count_data.new_n_heaps == n_heaps)
    {
        dynamic_heap_count_data.processed_samples_count = dynamic_heap_count_data.current_samples_count;
        dynamic_heap_count_data.processed_gen2_samples_count = dynamic_heap_count_data.current_gen2_samples_count;
        dynamic_heap_count_data.should_change_heap_count = false;

        dprintf (6666, ("heap count stays the same %d, no work to do, set processed sample count to %Id",
            dynamic_heap_count_data.new_n_heaps, dynamic_heap_count_data.current_samples_count));

        GCToEEInterface::RestartEE(TRUE);

        return;
    }

    int new_n_heaps = dynamic_heap_count_data.new_n_heaps;

    assert (!(dynamic_heap_count_data.init_only_p));

    {
        // At this point we are guaranteed to be able to change the heap count to the new one.
        // Change the heap count for joins here because we will need to join new_n_heaps threads together.
        dprintf (9999, ("changing join hp %d->%d", n_heaps, new_n_heaps));
        int max_threads_to_wake = max (n_heaps, new_n_heaps);
        gc_t_join.update_n_threads (max_threads_to_wake);

        // make sure the other gc threads cannot see this as a request to GC
        assert (dynamic_heap_count_data.new_n_heaps != n_heaps);

        if (n_heaps < new_n_heaps)
        {
            int saved_idle_thread_count = dynamic_heap_count_data.idle_thread_count;
            Interlocked::ExchangeAdd (&dynamic_heap_count_data.idle_thread_count, (n_heaps - new_n_heaps));
            dprintf (9999, ("GC thread %d setting idle events for h%d-h%d, total idle %d -> %d", heap_number, n_heaps, (new_n_heaps - 1),
                saved_idle_thread_count, VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_thread_count)));

            for (int heap_idx = n_heaps; heap_idx < new_n_heaps; heap_idx++)
            {
                g_heaps[heap_idx]->gc_idle_thread_event.Set();
            }
        }

        gc_start_event.Set();
    }

    int old_n_heaps = n_heaps;

    change_heap_count (dynamic_heap_count_data.new_n_heaps);

    GCToEEInterface::RestartEE(TRUE);
    dprintf (9999, ("h0 restarted EE"));

    dprintf (6666, ("h0 finished changing, set should change to false!\n"));
    dynamic_heap_count_data.should_change_heap_count = false;
}

bool gc_heap::prepare_to_change_heap_count (int new_n_heaps)
{
    dprintf (9999, ("trying to change heap count %d -> %d", n_heaps, new_n_heaps));

    // use this variable for clarity - n_heaps will change during the transition
    int old_n_heaps = n_heaps;

    // first do some steps that may fail and cause us to give up

    // we'll need temporary memory for the rethreading of the free lists -
    // if we can't allocate what we need, we must give up
    for (int i = 0; i < old_n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];

        if (!hp->prepare_rethread_fl_items())
        {
            return false;
        }
    }

    // move finalizer list items from heaps going out of service to remaining heaps
    // if this step fails, we have to give up
    if (new_n_heaps < old_n_heaps)
    {
        int to_heap_number = 0;
        for (int i = new_n_heaps; i < old_n_heaps; i++)
        {
            gc_heap* from_hp = g_heaps[i];
            gc_heap* to_hp = g_heaps[to_heap_number];

            // we always add the finalizer list items from a heap going out of service
            // to one of the remaining heaps, which we select in round robin fashion
            if (!to_hp->finalize_queue->MergeFinalizationData (from_hp->finalize_queue))
            {
                // failing to merge finalization data from one of the heaps about to go idle
                // means we cannot in fact reduce the number of heaps.
                dprintf (3, ("failed to merge finalization from heap %d into heap %d", i, to_heap_number));
                return false;
            }

            to_heap_number = (to_heap_number + 1) % new_n_heaps;
        }
    }

    // Before we look at whether we have sufficient regions we should return regions that should be deleted to free
    // so we don't lose them when we decommission heaps. We could do this for only heaps that we are about
    // to decomission. But it's better to do this for all heaps because we don't need to worry about adding them to the
    // heaps remain (freeable uoh/soh regions) and we get rid of regions with the heap_segment_flags_uoh_delete flag
    // because background_delay_delete_uoh_segments makes the assumption it can't be the start region.
    for (int i = 0; i < old_n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
        hp->delay_free_segments ();
    }

    // if we want to increase the number of heaps, we have to make sure we can give
    // each heap a region for each generation. If we cannot do that, we have to give up
    ptrdiff_t region_count_in_gen[total_generation_count];
    for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
    {
        region_count_in_gen[gen_idx] = 0;
    }
    if (old_n_heaps < new_n_heaps)
    {
        // count the number of regions in each generation
        for (int i = 0; i < old_n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];

            for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
            {
                generation* gen = hp->generation_of (gen_idx);
                for (heap_segment* region = heap_segment_rw (generation_start_segment (gen));
                     region != nullptr;
                     region = heap_segment_next (region))
                {
                    region_count_in_gen[gen_idx]++;
                }
            }
        }

        // check if we either have enough regions for each generation,
        // or can get enough from the free regions lists, or can allocate enough
        bool success = true;
        for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
        {
            const size_t size = gen_idx > soh_gen2 ? global_region_allocator.get_large_region_alignment() : 0;

            // if we don't have enough regions in this generation to cover all the new heaps,
            // try to find enough free regions
            while (region_count_in_gen[gen_idx] < new_n_heaps)
            {
                int kind = gen_idx > soh_gen2 ? large_free_region : basic_free_region;
                bool found_free_regions = false;
                for (int i = 0; i < old_n_heaps; i++)
                {
                    gc_heap* hp = g_heaps[i];
                    if (hp->free_regions[kind].get_num_free_regions() > 0)
                    {
                        // this heap has free regions - move one back into the generation
                        heap_segment* region = hp->get_new_region (gen_idx, size);
                        assert (region != nullptr);
                        region_count_in_gen[gen_idx]++;
                        found_free_regions = true;
                        if (region_count_in_gen[gen_idx] == new_n_heaps)
                            break;
                    }
                }
                if (!found_free_regions)
                {
                    break;
                }
            }
            while (region_count_in_gen[gen_idx] < new_n_heaps)
            {
                if (g_heaps[0]->get_new_region (gen_idx, size) == nullptr)
                {
                    success = false;
                    break;
                }
                region_count_in_gen[gen_idx]++;
            }
            if (!success)
            {
                // we failed to get enough regions - give up and rely on the next GC
                // to return the extra regions we got from the free list or allocated
                return false;
            }
        }
    }
    return true;
}

bool gc_heap::change_heap_count (int new_n_heaps)
{
    uint64_t start_time = 0;

    dprintf (9999, ("BEG heap%d changing %d->%d", heap_number, n_heaps, new_n_heaps));

    // use this variable for clarity - n_heaps will change during the transition
    int old_n_heaps = n_heaps;
    bool init_only_p = dynamic_heap_count_data.init_only_p;

    {
        gc_t_join.join (this, gc_join_merge_temp_fl);
        if (gc_t_join.joined ())
        {
            // BGC is not running, we can safely change its join's heap count.
#ifdef BACKGROUND_GC
            bgc_t_join.update_n_threads (new_n_heaps);
#endif //BACKGROUND_GC

            dynamic_heap_count_data.init_only_p = false;
            dprintf (9999, ("in change h%d resetting gc_start, update bgc join to %d heaps", heap_number, new_n_heaps));
            gc_start_event.Reset();
            gc_t_join.restart ();
        }
    }

    assert (dynamic_heap_count_data.new_n_heaps != old_n_heaps);

    if (heap_number == 0)
    {
        start_time = GetHighPrecisionTimeStamp ();

        // spread finalization data out to heaps coming into service
        // if this step fails, we can still continue
        int from_heap_number = 0;
        for (int i = old_n_heaps; i < new_n_heaps; i++)
        {
            gc_heap* to_hp = g_heaps[i];
            gc_heap* from_hp = g_heaps[from_heap_number];

            if (!from_hp->finalize_queue->SplitFinalizationData (to_hp->finalize_queue))
            {
                // we can live with this failure - it just means finalization data
                // are still on the old heap, which is correct, but suboptimal
                dprintf (3, ("failed to split finalization data between heaps %d and %d", from_heap_number, i));
            }

            from_heap_number = (from_heap_number + 1) % old_n_heaps;
        }

        // prepare for the switch by fixing the allocation contexts on the old heaps, unify the gen0_bricks_cleared flag,
        // and setting the survived size for the existing regions to their allocated size
        BOOL unified_gen0_bricks_cleared = TRUE;
        for (int i = 0; i < old_n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];

            if (!init_only_p)
            {
                hp->fix_allocation_contexts (TRUE);
            }

            if (unified_gen0_bricks_cleared && (hp->gen0_bricks_cleared == FALSE))
            {
                unified_gen0_bricks_cleared = FALSE;
            }

            for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
            {
                generation* gen = hp->generation_of (gen_idx);
                for (heap_segment* region = heap_segment_rw (generation_start_segment (gen));
                     region != nullptr;
                     region = heap_segment_next (region))
                {
                    // prepare the regions by pretending all their allocated space survives
                    heap_segment_survived (region) = heap_segment_allocated (region) - heap_segment_mem (region);
                }
            }
        }

        // inititalize the new heaps
        if (old_n_heaps < new_n_heaps)
        {
            // initialize the region lists of the new heaps
            for (int i = old_n_heaps; i < new_n_heaps; i++)
            {
                gc_heap* hp = g_heaps[i];

                hp->check_decommissioned_heap();

                hp->recommission_heap();
            }
        }

        if (new_n_heaps < old_n_heaps)
        {
            // move all regions from the heaps about to be retired to another heap < new_n_heaps
            assert (new_n_heaps > 0);

            for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
            {
                for (int i = new_n_heaps; i < old_n_heaps; i++)
                {
                    gc_heap* hp = g_heaps[i];

                    int dest_heap_number = i % new_n_heaps;
                    gc_heap* hpd = g_heaps[dest_heap_number];
                    generation* hpd_gen = hpd->generation_of (gen_idx);

                    generation* gen = hp->generation_of (gen_idx);

                    heap_segment* start_region = generation_start_segment (gen);
                    heap_segment* tail_ro_region = generation_tail_ro_region (gen);
                    heap_segment* tail_region = generation_tail_region (gen);

                    for (heap_segment* region = start_region; region != nullptr; region = heap_segment_next(region))
                    {
                        assert ((hp != nullptr) && (hpd != nullptr) && (hp != hpd));

                        int oh = heap_segment_oh (region);
                        size_t committed = heap_segment_committed (region) - get_region_start (region);
                        if (committed > 0)
                        {
                            dprintf(3, ("commit-accounting:  from %d to %d [%p, %p) for heap %d to heap %d", oh, oh, get_region_start (region), heap_segment_committed (region), i, dest_heap_number));
#ifdef _DEBUG
                            assert (hp->committed_by_oh_per_heap[oh] >= committed);
                            hp->committed_by_oh_per_heap[oh] -= committed;
                            hpd->committed_by_oh_per_heap[oh] += committed;
#endif // _DEBUG
                        }

                        set_heap_for_contained_basic_regions (region, hpd);
                    }
                    if (tail_ro_region != nullptr)
                    {
                        // the first r/w region is the one after tail_ro_region
                        heap_segment* start_rw_region = heap_segment_next (tail_ro_region);

                        heap_segment* hpd_tail_ro_region = generation_tail_ro_region (hpd_gen);
                        if (hpd_tail_ro_region != nullptr)
                        {
                            // insert the list of r/o regions between the r/o and the r/w regions already present
                            heap_segment_next (tail_ro_region) = heap_segment_next (hpd_tail_ro_region);
                            heap_segment_next (hpd_tail_ro_region) = start_region;
                        }
                        else
                        {
                            // put the list of r/o regions before the r/w regions present
                            heap_segment_next (tail_ro_region) = generation_start_segment (hpd_gen);
                            generation_start_segment (hpd_gen) = start_region;
                        }
                        generation_tail_ro_region (hpd_gen) = tail_ro_region;

                        // we took care of our r/o regions, we still have to do the r/w regions
                        start_region = start_rw_region;
                    }
                    // put the r/w regions at the tail of hpd_gen
                    heap_segment* hpd_tail_region = generation_tail_region (hpd_gen);
                    heap_segment_next (hpd_tail_region) = start_region;
                    generation_tail_region (hpd_gen) = tail_region;

                    generation_start_segment (gen) = nullptr;
                    generation_tail_ro_region (gen) = nullptr;
                    generation_tail_region (gen) = nullptr;
                }
            }
        }

        // transfer the free regions from the heaps going idle
        for (int i = new_n_heaps; i < old_n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
            int dest_heap_number = i % new_n_heaps;
            gc_heap* hpd = g_heaps[dest_heap_number];

            for (int kind = 0; kind < count_free_region_kinds; kind++)
            {
                hpd->free_regions[kind].transfer_regions(&hp->free_regions[kind]);
            }
        }
        dprintf (9999, ("h%d changing %d->%d", heap_number, n_heaps, new_n_heaps));
        n_heaps = new_n_heaps;

        // even out the regions over the current number of heaps
        equalize_promoted_bytes (max_generation);

        // establish invariants for the heaps now in operation
        for (int i = 0; i < new_n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];

            hp->gen0_bricks_cleared = unified_gen0_bricks_cleared;

            // establish invariants regarding the ephemeral segment
            generation* gen0 = hp->generation_of (0);
            if ((hp->ephemeral_heap_segment == nullptr) ||
                (heap_segment_heap (hp->ephemeral_heap_segment) != hp))
            {
                hp->ephemeral_heap_segment = heap_segment_rw (generation_start_segment (gen0));
                hp->alloc_allocated = heap_segment_allocated (hp->ephemeral_heap_segment);
            }

            for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
            {
                // establish invariants regarding the allocation segment
                generation* gen = hp->generation_of (gen_idx);
                heap_segment *allocation_region = generation_allocation_segment (gen);
                if ((allocation_region == nullptr) ||
                    (heap_segment_heap (allocation_region) != hp))
                {
                    generation_allocation_segment (gen) = heap_segment_rw (generation_start_segment (gen));
                }

                // we shifted regions around, but we have no way to properly account for the small free spaces
                // it's safest to set this to 0, otherwise size computations in compute_new_dynamic_data
                // may overflow
                generation_free_obj_space (gen) = 0;
            }
        }
    }

    dprintf (3, ("individual heap%d changing %d->%d", heap_number, n_heaps, new_n_heaps));

    if (!init_only_p)
    {
        // join for rethreading the free lists
        gc_t_join.join (this, gc_join_merge_temp_fl);
        if (gc_t_join.joined ())
        {
#ifdef BACKGROUND_GC
            // For now I'm always setting it to true. This should be set based on heuristics like the number of
            // FL items. I'm currently rethreading all generations' FL except gen2's. When the next GC happens,
            // it will be a BGC (unless it's a blocking gen2 which also works). And when BGC sweep starts we will
            // build the gen2 FL from scratch.
            trigger_bgc_for_rethreading_p = true;
#endif //BACKGROUND_GC
            gc_t_join.restart ();
        }

        // rethread the free lists
        for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
        {
            bool do_rethreading = true;

#ifdef BACKGROUND_GC
            if (trigger_bgc_for_rethreading_p && (gen_idx == max_generation))
            {
                do_rethreading = false;
            }
#endif //BACKGROUND_GC

            if (do_rethreading)
            {
                if (heap_number < old_n_heaps)
                {
                    dprintf (3, ("h%d calling per heap work!", heap_number));
                    rethread_fl_items (gen_idx);
                }

                // join for merging the free lists
                gc_t_join.join (this, gc_join_merge_temp_fl);
                if (gc_t_join.joined ())
                {
                    merge_fl_from_other_heaps (gen_idx, new_n_heaps, old_n_heaps);

                    gc_t_join.restart ();
                }
            }
        }

#ifdef BACKGROUND_GC
        // there should be no items in the bgc_alloc_lock
        bgc_alloc_lock->check();
#endif //BACKGROUND_GC
    }

    if (heap_number == 0)
    {
        // compute the total budget per generation over the old heaps
        // and figure out what the new budget per heap is
        ptrdiff_t new_alloc_per_heap[total_generation_count];
        size_t desired_alloc_per_heap[total_generation_count];
        for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
        {
            ptrdiff_t total_new_alloc = 0;
            size_t total_desired_alloc = 0;
            for (int i = 0; i < old_n_heaps; i++)
            {
                gc_heap* hp = g_heaps[i];

                dynamic_data* dd = hp->dynamic_data_of (gen_idx);
                total_new_alloc += dd_new_allocation (dd);
                total_desired_alloc += dd_desired_allocation (dd);
            }
            // distribute the total budget for this generation over all new heaps if we are increasing heap count,
            // but keep the budget per heap if we are decreasing heap count
            int max_n_heaps = max (old_n_heaps, new_n_heaps);
            new_alloc_per_heap[gen_idx] = Align (total_new_alloc / max_n_heaps, get_alignment_constant (gen_idx <= max_generation));
            desired_alloc_per_heap[gen_idx] = Align (total_desired_alloc / max_n_heaps, get_alignment_constant (gen_idx <= max_generation));
            size_t allocated_in_budget = total_desired_alloc - total_new_alloc;
            dprintf (6666, ("g%d: total budget %zd (%zd / heap), left in budget: %zd (%zd / heap), (allocated %Id, %.3f%%), min %zd",
                gen_idx, total_desired_alloc, desired_alloc_per_heap[gen_idx],
                total_new_alloc, new_alloc_per_heap[gen_idx],
                allocated_in_budget, ((double)allocated_in_budget * 100.0 / (double)total_desired_alloc),
                dd_min_size (g_heaps[0]->dynamic_data_of (gen_idx))));
        }

        // distribute the new budget per heap over the new heaps
        // and recompute the current size of the generation
        for (int i = 0; i < new_n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];

            for (int gen_idx = 0; gen_idx < total_generation_count; gen_idx++)
            {
                // distribute the total leftover budget over all heaps.
                dynamic_data* dd = hp->dynamic_data_of (gen_idx);
                dd_new_allocation (dd) = new_alloc_per_heap[gen_idx];
                dd_desired_allocation (dd) = max (desired_alloc_per_heap[gen_idx], dd_min_size (dd));

                // recompute dd_fragmentation and dd_current_size
                generation* gen = hp->generation_of (gen_idx);
                size_t gen_size = hp->generation_size (gen_idx);
                dd_fragmentation (dd) = generation_free_list_space (gen);
                if (gen_idx == max_generation)
                {
                    // Just set it to 0 so it doesn't cause any problems. The next GC which will be a gen2 will update it to the correct value.
                    dd_current_size (dd) = 0;
                }
                else
                {
                    // We cannot assert this for gen2 because we didn't actually rethread gen2 FL.
                    assert (gen_size >= dd_fragmentation (dd));
                    dd_current_size (dd) = gen_size - dd_fragmentation (dd);
                }

                dprintf (3, ("h%d g%d: budget: %zd, left in budget: %zd, generation_size: %zd fragmentation: %zd current_size: %zd",
                    i,
                    gen_idx,
                    desired_alloc_per_heap[gen_idx],
                    new_alloc_per_heap[gen_idx],
                    gen_size,
                    dd_fragmentation (dd),
                    dd_current_size (dd)));
            }
        }

        // put heaps that going idle now into the decommissioned state
        for (int i = n_heaps; i < old_n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];

            hp->decommission_heap();
        }

        if (!init_only_p)
        {
            // make sure no allocation contexts point to idle heaps
            fix_allocation_contexts_heaps();
        }

        dynamic_heap_count_data.last_n_heaps = old_n_heaps;
    }

    // join the last time to change the heap count again if needed.
    if (new_n_heaps < old_n_heaps)
    {
        gc_t_join.join (this, gc_join_merge_temp_fl);
        if (gc_t_join.joined ())
        {
            dprintf (9999, ("now changing the join heap count to the smaller one %d", new_n_heaps));
            gc_t_join.update_n_threads (new_n_heaps);

            gc_t_join.restart ();
        }
    }

    if (heap_number == 0)
    {
        add_to_hc_history (hc_record_change_done);
        change_heap_count_time = GetHighPrecisionTimeStamp() - start_time;
        total_change_heap_count_time += change_heap_count_time;
        total_change_heap_count++;
        dprintf (6666, ("changing HC took %I64dus", change_heap_count_time));
    }

    return true;
}

void gc_heap::get_msl_wait_time (size_t* soh_msl_wait_time, size_t* uoh_msl_wait_time)
{
    assert (dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes);

    *soh_msl_wait_time = 0;
    *uoh_msl_wait_time = 0;

    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];

        soh_msl_wait_time += hp->more_space_lock_soh.msl_wait_time;
        hp->more_space_lock_soh.msl_wait_time = 0;

        uoh_msl_wait_time += hp->more_space_lock_uoh.msl_wait_time;
        hp->more_space_lock_uoh.msl_wait_time = 0;
    }
}

void gc_heap::process_datas_sample()
{
    // We get the time here instead of waiting till we assign end_gc_time because end_gc_time includes distribute_free_regions
    // but we need to get the budget from DATAS before we call distribute_free_regions. distribute_free_regions takes < 1% of
    // the GC pause so it's ok to not count it. The GC elapsed time DATAS records uses this timestamp instead of end_gc_time.
    before_distribute_free_regions_time = GetHighPrecisionTimeStamp();
    dynamic_data* dd0 = g_heaps[0]->dynamic_data_of (0);
    uint64_t gc_pause_time = before_distribute_free_regions_time - dd_time_clock (dd0);

    size_t desired_per_heap = dd_desired_allocation (dd0);
    if (settings.gc_index > 1)
    {
        size_t gc_index = VolatileLoadWithoutBarrier (&settings.gc_index);
        dynamic_heap_count_data_t::sample& sample = dynamic_heap_count_data.samples[dynamic_heap_count_data.sample_index];
        sample.elapsed_between_gcs = before_distribute_free_regions_time - last_suspended_end_time;
        sample.gc_pause_time = gc_pause_time;
        size_t soh_msl_wait_time, uoh_msl_wait_time;
        get_msl_wait_time (&soh_msl_wait_time, &uoh_msl_wait_time);
        sample.msl_wait_time = soh_msl_wait_time + uoh_msl_wait_time;
        sample.gc_index = gc_index;
        // could cache this - we will get it again soon in do_post_gc
        sample.gc_survived_size = get_total_promoted();

        // We check to see if we want to adjust the budget here for DATAS.
        size_t desired_per_heap_datas = desired_per_heap;
        float tcp = (sample.elapsed_between_gcs ?
            (((float)sample.msl_wait_time / n_heaps + sample.gc_pause_time) * 100.0f / (float)sample.elapsed_between_gcs) : 0.0f);
        size_t total_soh_stable_size = get_total_soh_stable_size();
        desired_per_heap_datas = dynamic_heap_count_data.compute_gen0_budget_per_heap (total_soh_stable_size, tcp, desired_per_heap);
        dprintf (6666, ("gen0 new_alloc %Id (%.3fmb), from datas: %Id (%.3fmb)",
            desired_per_heap, mb (desired_per_heap), desired_per_heap_datas, mb (desired_per_heap_datas)));
        dprintf (6666, ("budget DATAS %Id, previous %Id", desired_per_heap_datas, desired_per_heap));

        sample.gen0_budget_per_heap = (int)desired_per_heap_datas;
        if (desired_per_heap_datas != desired_per_heap)
        {
            dprintf (6666, ("adjusted budget for DATAS, assigning to all heaps"));
            assign_new_budget (0, desired_per_heap_datas);
        }

        dprintf (6666, ("sample#%d: %d heaps, this GC end %I64d - last sus end %I64d = %I64d, this GC pause %.3fms, msl wait %I64dus, tcp %.3f, surv %zd, gc speed %.3fmb/ms (%.3fkb/ms/heap)",
            dynamic_heap_count_data.sample_index, n_heaps, before_distribute_free_regions_time, last_suspended_end_time, sample.elapsed_between_gcs,
            (sample.gc_pause_time / 1000.0), sample.msl_wait_time, tcp, sample.gc_survived_size,
            (sample.gc_pause_time ? (sample.gc_survived_size / 1000.0 / sample.gc_pause_time) : 0),
            (sample.gc_pause_time ? ((float)sample.gc_survived_size / sample.gc_pause_time / n_heaps) : 0)));

#ifdef FEATURE_EVENT_TRACE
        GCEventFireSizeAdaptationSample_V1 (
            (uint64_t)gc_index,
            (uint32_t)sample.elapsed_between_gcs,
            (uint32_t)sample.gc_pause_time,
            (uint32_t)soh_msl_wait_time, (uint32_t)uoh_msl_wait_time,
            (uint64_t)total_soh_stable_size, (uint32_t)sample.gen0_budget_per_heap);
#endif //FEATURE_EVENT_TRACE

        dynamic_heap_count_data.sample_index = (dynamic_heap_count_data.sample_index + 1) % dynamic_heap_count_data_t::sample_size;
        (dynamic_heap_count_data.current_samples_count)++;

        if (settings.condemned_generation == max_generation)
        {
            gc_index_full_gc_end = dd_gc_clock (dd0);
            dynamic_heap_count_data_t::gen2_sample& last_g2_sample = dynamic_heap_count_data.get_last_gen2_sample();
            uint64_t prev_gen2_end_time = dd_previous_time_clock (g_heaps[0]->dynamic_data_of (max_generation)) + last_g2_sample.gc_duration;
            size_t elapsed_between_gen2_gcs = before_distribute_free_regions_time - prev_gen2_end_time;
            size_t gen2_elapsed_time = sample.gc_pause_time;
            dynamic_heap_count_data_t::gen2_sample& g2_sample = dynamic_heap_count_data.get_current_gen2_sample();
            g2_sample.gc_index = VolatileLoadWithoutBarrier (&(settings.gc_index));
            g2_sample.gc_duration = gen2_elapsed_time;
            g2_sample.gc_percent = (float)gen2_elapsed_time * 100.0f / elapsed_between_gen2_gcs;
            (dynamic_heap_count_data.current_gen2_samples_count)++;

            dprintf (6666, ("gen2 sample#%d: this GC end %I64d - last gen2 end %I64d = %I64d, GC elapsed %I64d, percent %.3f",
                dynamic_heap_count_data.gen2_sample_index, before_distribute_free_regions_time, prev_gen2_end_time, elapsed_between_gen2_gcs, gen2_elapsed_time, g2_sample.gc_percent));
            dynamic_heap_count_data.gen2_sample_index = (dynamic_heap_count_data.gen2_sample_index + 1) % dynamic_heap_count_data_t::sample_size;
        }

        calculate_new_heap_count ();
    }
    else
    {
        // For DATAS we can't just take the BCS because it's likely very large and that could totally make the max heap size larger. We just take the
        // min budget.
        size_t min_desired = dd_min_size (dd0);
        if (min_desired != desired_per_heap)
        {
            dprintf (6666, ("use the min budget for DATAS, assigning to all heaps"));
            assign_new_budget (0, min_desired);
        }
    }

    last_suspended_end_time = before_distribute_free_regions_time;
}

void gc_heap::add_to_hc_history_worker (hc_history* hist, int* current_index, hc_record_stage stage, const char* msg)
{
    dprintf (6666, ("h%d ADDING %s HC hist to entry #%d, stage %d, gc index %Id, last %d, n %d, new %d",
        heap_number, msg, *current_index, (int)stage, VolatileLoadWithoutBarrier (&settings.gc_index),
        dynamic_heap_count_data.last_n_heaps, n_heaps, dynamic_heap_count_data.new_n_heaps));
    hc_history* current_hist = &hist[*current_index];
    current_hist->gc_index = VolatileLoadWithoutBarrier (&settings.gc_index);
    current_hist->stage = (short)stage;
    current_hist->last_n_heaps = (short)dynamic_heap_count_data.last_n_heaps;
    current_hist->n_heaps = (short)n_heaps;
    current_hist->new_n_heaps = (short)dynamic_heap_count_data.new_n_heaps;
    current_hist->idle_thread_count = (short)dynamic_heap_count_data.idle_thread_count;
    current_hist->gc_t_join_n_threads = (short)gc_t_join.get_num_threads();
    current_hist->gc_t_join_join_lock = (short)gc_t_join.get_join_lock();
    current_hist->gc_t_join_joined_p = (bool)gc_t_join.joined();
#ifdef BACKGROUND_GC
    current_hist->bgc_t_join_n_threads = (short)bgc_t_join.get_num_threads();
    current_hist->bgc_t_join_join_lock = (short)bgc_t_join.get_join_lock();
    current_hist->bgc_t_join_joined_p = (bool)bgc_t_join.joined();
    current_hist->concurrent_p = (bool)settings.concurrent;
    current_hist->bgc_thread_running = (bool)bgc_thread_running;
    int bgc_thread_os_id = 0;
    if (bgc_thread)
    {
        bgc_thread_os_id = (int) GCToEEInterface::GetThreadOSThreadId(bgc_thread);
    }
    current_hist->bgc_thread_os_id = bgc_thread_os_id;
#endif //BACKGROUND_GC

    *current_index  = (*current_index + 1) % max_hc_history_count;
}

void gc_heap::add_to_hc_history (hc_record_stage stage)
{
    add_to_hc_history_worker (hchist_per_heap, &hchist_index_per_heap, stage, "GC");
}

void gc_heap::add_to_bgc_hc_history (hc_record_stage stage)
{
    add_to_hc_history_worker (bgc_hchist_per_heap, &bgc_hchist_index_per_heap, stage, "BGC");
}

#endif //DYNAMIC_HEAP_COUNT
#endif //USE_REGIONS
