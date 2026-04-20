// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

void gc_heap::update_collection_counts_for_no_gc()
{
    assert (settings.pause_mode == pause_no_gc);

    settings.condemned_generation = max_generation;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
        g_heaps[i]->update_collection_counts();
#else //MULTIPLE_HEAPS
    update_collection_counts();
#endif //MULTIPLE_HEAPS

    full_gc_counts[gc_type_blocking]++;
}

BOOL gc_heap::should_proceed_with_gc()
{
    if (gc_heap::settings.pause_mode == pause_no_gc)
    {
        if (current_no_gc_region_info.started)
        {
            if (current_no_gc_region_info.soh_withheld_budget != 0)
            {
                dprintf(1, ("[no_gc_callback] allocation budget exhausted with withheld, time to trigger callback\n"));
#ifdef MULTIPLE_HEAPS
                for (int i = 0; i < gc_heap::n_heaps; i++)
                {
                    gc_heap* hp = gc_heap::g_heaps [i];
#else
                {
                    gc_heap* hp = pGenGCHeap;
#endif
                    dd_new_allocation (hp->dynamic_data_of (soh_gen0)) += current_no_gc_region_info.soh_withheld_budget;
                    dd_new_allocation (hp->dynamic_data_of (loh_generation)) += current_no_gc_region_info.loh_withheld_budget;
                }
                current_no_gc_region_info.soh_withheld_budget = 0;
                current_no_gc_region_info.loh_withheld_budget = 0;

                // Trigger the callback
                schedule_no_gc_callback (false);
                current_no_gc_region_info.callback = nullptr;
                return FALSE;
            }
            else
            {
                dprintf(1, ("[no_gc_callback] GC triggered while in no_gc mode. Exiting no_gc mode.\n"));
                // The no_gc mode was already in progress yet we triggered another GC,
                // this effectively exits the no_gc mode.
                restore_data_for_no_gc();
                if (current_no_gc_region_info.callback != nullptr)
                {
                    dprintf (1, ("[no_gc_callback] detaching callback on exit"));
                    schedule_no_gc_callback (true);
                }
                memset (&current_no_gc_region_info, 0, sizeof (current_no_gc_region_info));
            }
        }
        else
            return should_proceed_for_no_gc();
    }

    return TRUE;
}

void gc_heap::save_data_for_no_gc()
{
    current_no_gc_region_info.saved_pause_mode = settings.pause_mode;
#ifdef MULTIPLE_HEAPS
    // This is to affect heap balancing.
    for (int i = 0; i < n_heaps; i++)
    {
        current_no_gc_region_info.saved_gen0_min_size = dd_min_size (g_heaps[i]->dynamic_data_of (0));
        dd_min_size (g_heaps[i]->dynamic_data_of (0)) = min_balance_threshold;
        current_no_gc_region_info.saved_gen3_min_size = dd_min_size (g_heaps[i]->dynamic_data_of (loh_generation));
        dd_min_size (g_heaps[i]->dynamic_data_of (loh_generation)) = 0;
    }
#endif //MULTIPLE_HEAPS
}

void gc_heap::restore_data_for_no_gc()
{
    gc_heap::settings.pause_mode = current_no_gc_region_info.saved_pause_mode;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        dd_min_size (g_heaps[i]->dynamic_data_of (0)) = current_no_gc_region_info.saved_gen0_min_size;
        dd_min_size (g_heaps[i]->dynamic_data_of (loh_generation)) = current_no_gc_region_info.saved_gen3_min_size;
    }
#endif //MULTIPLE_HEAPS
}

start_no_gc_region_status gc_heap::prepare_for_no_gc_region (uint64_t total_size,
                                                             BOOL loh_size_known,
                                                             uint64_t loh_size,
                                                             BOOL disallow_full_blocking)
{
    if (current_no_gc_region_info.started)
    {
        return start_no_gc_in_progress;
    }

    start_no_gc_region_status status = start_no_gc_success;

    save_data_for_no_gc();
    settings.pause_mode = pause_no_gc;
    current_no_gc_region_info.start_status = start_no_gc_success;

    uint64_t allocation_no_gc_loh = 0;
    uint64_t allocation_no_gc_soh = 0;
    assert(total_size != 0);
    if (loh_size_known)
    {
        assert(loh_size != 0);
        assert(loh_size <= total_size);
        allocation_no_gc_loh = loh_size;
        allocation_no_gc_soh = total_size - loh_size;
    }
    else
    {
        allocation_no_gc_soh = total_size;
        allocation_no_gc_loh = total_size;
    }

    int soh_align_const = get_alignment_constant (TRUE);
#ifdef USE_REGIONS
    size_t max_soh_allocated = SIZE_T_MAX;
#else
    size_t max_soh_allocated = soh_segment_size - segment_info_size - eph_gen_starts_size;
#endif
    size_t size_per_heap = 0;
    const double scale_factor = 1.05;

    int num_heaps = get_num_heaps();

    uint64_t total_allowed_soh_allocation = (uint64_t)max_soh_allocated * num_heaps;
    // [LOCALGC TODO]
    // In theory, the upper limit here is the physical memory of the machine, not
    // SIZE_T_MAX. This is not true today because total_physical_mem can be
    // larger than SIZE_T_MAX if running in wow64 on a machine with more than
    // 4GB of RAM. Once Local GC code divergence is resolved and code is flowing
    // more freely between branches, it would be good to clean this up to use
    // total_physical_mem instead of SIZE_T_MAX.
    assert(total_allowed_soh_allocation <= SIZE_T_MAX);
    uint64_t total_allowed_loh_allocation = SIZE_T_MAX;
    uint64_t total_allowed_soh_alloc_scaled = allocation_no_gc_soh > 0 ? static_cast<uint64_t>(total_allowed_soh_allocation / scale_factor) : 0;
    uint64_t total_allowed_loh_alloc_scaled = allocation_no_gc_loh > 0 ? static_cast<uint64_t>(total_allowed_loh_allocation / scale_factor) : 0;

    if (allocation_no_gc_soh > total_allowed_soh_alloc_scaled ||
        allocation_no_gc_loh > total_allowed_loh_alloc_scaled)
    {
        status = start_no_gc_too_large;
        goto done;
    }

    if (allocation_no_gc_soh > 0)
    {
        allocation_no_gc_soh = static_cast<uint64_t>(allocation_no_gc_soh * scale_factor);
        allocation_no_gc_soh = min (allocation_no_gc_soh, total_allowed_soh_alloc_scaled);
    }

    if (allocation_no_gc_loh > 0)
    {
        allocation_no_gc_loh = static_cast<uint64_t>(allocation_no_gc_loh * scale_factor);
        allocation_no_gc_loh = min (allocation_no_gc_loh, total_allowed_loh_alloc_scaled);
    }

    if (disallow_full_blocking)
        current_no_gc_region_info.minimal_gc_p = TRUE;

    if (allocation_no_gc_soh != 0)
    {
        current_no_gc_region_info.soh_allocation_size = (size_t)allocation_no_gc_soh;
        size_per_heap = current_no_gc_region_info.soh_allocation_size;
#ifdef MULTIPLE_HEAPS
        size_per_heap /= n_heaps;
        for (int i = 0; i < n_heaps; i++)
        {
            // due to heap balancing we need to allow some room before we even look to balance to another heap.
            g_heaps[i]->soh_allocation_no_gc = min (Align ((size_per_heap + min_balance_threshold), soh_align_const), max_soh_allocated);
        }
#else //MULTIPLE_HEAPS
        soh_allocation_no_gc = min (Align (size_per_heap, soh_align_const), max_soh_allocated);
#endif //MULTIPLE_HEAPS
    }

    if (allocation_no_gc_loh != 0)
    {
        current_no_gc_region_info.loh_allocation_size = (size_t)allocation_no_gc_loh;
        size_per_heap = current_no_gc_region_info.loh_allocation_size;
#ifdef MULTIPLE_HEAPS
        size_per_heap /= n_heaps;
        for (int i = 0; i < n_heaps; i++)
            g_heaps[i]->loh_allocation_no_gc = Align (size_per_heap, get_alignment_constant (FALSE));
#else //MULTIPLE_HEAPS
        loh_allocation_no_gc = Align (size_per_heap, get_alignment_constant (FALSE));
#endif //MULTIPLE_HEAPS
    }

done:
    if (status != start_no_gc_success)
        restore_data_for_no_gc();
    return status;
}

void gc_heap::handle_failure_for_no_gc()
{
    gc_heap::restore_data_for_no_gc();
    // sets current_no_gc_region_info.started to FALSE here.
    memset (&current_no_gc_region_info, 0, sizeof (current_no_gc_region_info));
}

start_no_gc_region_status gc_heap::get_start_no_gc_region_status()
{
    return current_no_gc_region_info.start_status;
}

void gc_heap::record_gcs_during_no_gc()
{
    if (current_no_gc_region_info.started)
    {
        current_no_gc_region_info.num_gcs++;
        if (is_induced (settings.reason))
            current_no_gc_region_info.num_gcs_induced++;
    }
}

BOOL gc_heap::find_loh_free_for_no_gc()
{
    allocator* loh_allocator = generation_allocator (generation_of (loh_generation));
    size_t size = loh_allocation_no_gc;
    for (unsigned int a_l_idx = loh_allocator->first_suitable_bucket(size); a_l_idx < loh_allocator->number_of_buckets(); a_l_idx++)
    {
        uint8_t* free_list = loh_allocator->alloc_list_head_of (a_l_idx);
        while (free_list)
        {
            size_t free_list_size = unused_array_size(free_list);

            if (free_list_size > size)
            {
                dprintf (3, ("free item %zx(%zd) for no gc", (size_t)free_list, free_list_size));
                return TRUE;
            }

            free_list = free_list_slot (free_list);
        }
    }

    return FALSE;
}

BOOL gc_heap::find_loh_space_for_no_gc()
{
    saved_loh_segment_no_gc = 0;

    if (find_loh_free_for_no_gc())
        return TRUE;

    heap_segment* seg = generation_allocation_segment (generation_of (loh_generation));

    while (seg)
    {
        size_t remaining = heap_segment_reserved (seg) - heap_segment_allocated (seg);
        if (remaining >= loh_allocation_no_gc)
        {
            saved_loh_segment_no_gc = seg;
            break;
        }
        seg = heap_segment_next (seg);
    }

    if (!saved_loh_segment_no_gc && current_no_gc_region_info.minimal_gc_p)
    {
        // If no full GC is allowed, we try to get a new seg right away.
        saved_loh_segment_no_gc = get_segment_for_uoh (loh_generation, get_uoh_seg_size (loh_allocation_no_gc)
#ifdef MULTIPLE_HEAPS
                                                      , this
#endif //MULTIPLE_HEAPS
                                                      );
    }

    return (saved_loh_segment_no_gc != 0);
}

BOOL gc_heap::loh_allocated_for_no_gc()
{
    if (!saved_loh_segment_no_gc)
        return FALSE;

    heap_segment* seg = generation_allocation_segment (generation_of (loh_generation));
    do
    {
        if (seg == saved_loh_segment_no_gc)
        {
            return FALSE;
        }
        seg = heap_segment_next (seg);
    } while (seg);

    return TRUE;
}

BOOL gc_heap::commit_loh_for_no_gc (heap_segment* seg)
{
    uint8_t* end_committed = heap_segment_allocated (seg) + loh_allocation_no_gc;
    assert (end_committed <= heap_segment_reserved (seg));
    return (grow_heap_segment (seg, end_committed));
}

void gc_heap::thread_no_gc_loh_segments()
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
        if (hp->loh_allocated_for_no_gc())
        {
            hp->thread_uoh_segment (loh_generation, hp->saved_loh_segment_no_gc);
            hp->saved_loh_segment_no_gc = 0;
        }
    }
#else //MULTIPLE_HEAPS
    if (loh_allocated_for_no_gc())
    {
        thread_uoh_segment (loh_generation, saved_loh_segment_no_gc);
        saved_loh_segment_no_gc = 0;
    }
#endif //MULTIPLE_HEAPS
}

void gc_heap::set_loh_allocations_for_no_gc()
{
    if (current_no_gc_region_info.loh_allocation_size != 0)
    {
        dynamic_data* dd = dynamic_data_of (loh_generation);
        dd_new_allocation (dd) = loh_allocation_no_gc;
        dd_gc_new_allocation (dd) = dd_new_allocation (dd);
    }
}

void gc_heap::set_soh_allocations_for_no_gc()
{
    if (current_no_gc_region_info.soh_allocation_size != 0)
    {
        dynamic_data* dd = dynamic_data_of (0);
        dd_new_allocation (dd) = soh_allocation_no_gc;
        dd_gc_new_allocation (dd) = dd_new_allocation (dd);
#ifdef MULTIPLE_HEAPS
        alloc_context_count = 0;
#endif //MULTIPLE_HEAPS
    }
}

void gc_heap::set_allocations_for_no_gc()
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
        hp->set_loh_allocations_for_no_gc();
        hp->set_soh_allocations_for_no_gc();
    }
#else //MULTIPLE_HEAPS
    set_loh_allocations_for_no_gc();
    set_soh_allocations_for_no_gc();
#endif //MULTIPLE_HEAPS
}

BOOL gc_heap::should_proceed_for_no_gc()
{
    BOOL gc_requested = FALSE;
    BOOL loh_full_gc_requested = FALSE;
    BOOL soh_full_gc_requested = FALSE;
    BOOL no_gc_requested = FALSE;
    BOOL get_new_loh_segments = FALSE;

#ifdef MULTIPLE_HEAPS
    // need to turn off this flag here because of the call to grow_heap_segment below
    gradual_decommit_in_progress_p = FALSE;
#endif //MULTIPLE_HEAPS

    gc_heap* hp = nullptr;
    if (current_no_gc_region_info.soh_allocation_size)
    {
#ifdef USE_REGIONS
#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            hp = g_heaps[i];
#else
        {
            hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
            if (!hp->extend_soh_for_no_gc())
            {
                soh_full_gc_requested = TRUE;
#ifdef MULTIPLE_HEAPS
                break;
#endif //MULTIPLE_HEAPS
            }
        }
#else //USE_REGIONS
#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            hp = g_heaps[i];
#else //MULTIPLE_HEAPS
        {
            hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
            size_t reserved_space = heap_segment_reserved (hp->ephemeral_heap_segment) - hp->alloc_allocated;
            if (reserved_space < hp->soh_allocation_no_gc)
            {
                gc_requested = TRUE;
#ifdef MULTIPLE_HEAPS
                break;
#endif //MULTIPLE_HEAPS
            }
        }
        if (!gc_requested)
        {
#ifdef MULTIPLE_HEAPS
            for (int i = 0; i < n_heaps; i++)
            {
                hp = g_heaps[i];
#else //MULTIPLE_HEAPS
            {
                hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
                if (!(hp->grow_heap_segment (hp->ephemeral_heap_segment, (hp->alloc_allocated + hp->soh_allocation_no_gc))))
                {
                    soh_full_gc_requested = TRUE;
#ifdef MULTIPLE_HEAPS
                    break;
#endif //MULTIPLE_HEAPS
                }
            }
        }
#endif //USE_REGIONS
    }

    if (!current_no_gc_region_info.minimal_gc_p && gc_requested)
    {
        soh_full_gc_requested = TRUE;
    }

    no_gc_requested = !(soh_full_gc_requested || gc_requested);

    if (soh_full_gc_requested && current_no_gc_region_info.minimal_gc_p)
    {
        current_no_gc_region_info.start_status = start_no_gc_no_memory;
        goto done;
    }

    if (!soh_full_gc_requested && current_no_gc_region_info.loh_allocation_size)
    {
        // Check to see if we have enough reserved space.
#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
            if (!hp->find_loh_space_for_no_gc())
            {
                loh_full_gc_requested = TRUE;
                break;
            }
        }
#else //MULTIPLE_HEAPS
        if (!find_loh_space_for_no_gc())
            loh_full_gc_requested = TRUE;
#endif //MULTIPLE_HEAPS

        // Check to see if we have committed space.
        if (!loh_full_gc_requested)
        {
#ifdef MULTIPLE_HEAPS
            for (int i = 0; i < n_heaps; i++)
            {
                gc_heap* hp = g_heaps[i];
                if (hp->saved_loh_segment_no_gc &&!hp->commit_loh_for_no_gc (hp->saved_loh_segment_no_gc))
                {
                    loh_full_gc_requested = TRUE;
                    break;
                }
            }
#else //MULTIPLE_HEAPS
            if (saved_loh_segment_no_gc && !commit_loh_for_no_gc (saved_loh_segment_no_gc))
                loh_full_gc_requested = TRUE;
#endif //MULTIPLE_HEAPS
        }
    }

    if (loh_full_gc_requested || soh_full_gc_requested)
    {
        if (current_no_gc_region_info.minimal_gc_p)
            current_no_gc_region_info.start_status = start_no_gc_no_memory;
    }

    no_gc_requested = !(loh_full_gc_requested || soh_full_gc_requested || gc_requested);

    if (current_no_gc_region_info.start_status == start_no_gc_success)
    {
        if (no_gc_requested)
            set_allocations_for_no_gc();
    }

done:

    if ((current_no_gc_region_info.start_status == start_no_gc_success) && !no_gc_requested)
        return TRUE;
    else
    {
        // We are done with starting the no_gc_region.
        current_no_gc_region_info.started = TRUE;
        return FALSE;
    }
}

end_no_gc_region_status gc_heap::end_no_gc_region()
{
    dprintf (1, ("end no gc called"));

    end_no_gc_region_status status = end_no_gc_success;

    if (!(current_no_gc_region_info.started))
        status = end_no_gc_not_in_progress;
    if (current_no_gc_region_info.num_gcs_induced)
        status = end_no_gc_induced;
    else if (current_no_gc_region_info.num_gcs)
        status = end_no_gc_alloc_exceeded;

    if (settings.pause_mode == pause_no_gc)
    {
        restore_data_for_no_gc();
        if (current_no_gc_region_info.callback != nullptr)
        {
            dprintf (1, ("[no_gc_callback] detaching callback on exit"));
            schedule_no_gc_callback (true);
        }
    }

    // sets current_no_gc_region_info.started to FALSE here.
    memset (&current_no_gc_region_info, 0, sizeof (current_no_gc_region_info));

    return status;
}

void gc_heap::schedule_no_gc_callback (bool abandoned)
{
    // We still want to schedule the work even when the no-gc callback is abandoned
    // so that we can free the memory associated with it.
    current_no_gc_region_info.callback->abandoned = abandoned;

    if (!current_no_gc_region_info.callback->scheduled)
    {
        current_no_gc_region_info.callback->scheduled = true;
        schedule_finalizer_work(current_no_gc_region_info.callback);
    }
}

#ifdef USE_REGIONS
bool gc_heap::extend_soh_for_no_gc()
{
    size_t required = soh_allocation_no_gc;
    heap_segment* region = ephemeral_heap_segment;

    while (true)
    {
        uint8_t* allocated = (region == ephemeral_heap_segment) ?
                             alloc_allocated :
                             heap_segment_allocated (region);
        size_t available = heap_segment_reserved (region) - allocated;
        size_t commit = min (available, required);

        if (grow_heap_segment (region, allocated + commit))
        {
            required -= commit;
            if (required == 0)
            {
                break;
            }

            region = heap_segment_next (region);
            if (region == nullptr)
            {
                region = get_new_region (0);
                if (region == nullptr)
                {
                    break;
                }
                else
                {
                    GCToEEInterface::DiagAddNewRegion(
                            0,
                            heap_segment_mem (region),
                            heap_segment_allocated (region),
                            heap_segment_reserved (region)
                        );
                }
            }
        }
        else
        {
            break;
        }
    }

    return (required == 0);
}

#else //USE_REGIONS
BOOL gc_heap::expand_soh_with_minimal_gc()
{
    if ((size_t)(heap_segment_reserved (ephemeral_heap_segment) - heap_segment_allocated (ephemeral_heap_segment)) >= soh_allocation_no_gc)
        return TRUE;

    heap_segment* new_seg = soh_get_segment_to_expand();
    if (new_seg)
    {
        if (g_gc_card_table != card_table)
            copy_brick_card_table();

        settings.promotion = TRUE;
        settings.demotion = FALSE;
        ephemeral_promotion = TRUE;
        int condemned_gen_number = max_generation - 1;

        int align_const = get_alignment_constant (TRUE);

        for (int i = 0; i <= condemned_gen_number; i++)
        {
            generation* gen = generation_of (i);
            saved_ephemeral_plan_start[i] = generation_allocation_start (gen);
            saved_ephemeral_plan_start_size[i] = Align (size (generation_allocation_start (gen)), align_const);
        }

        // We do need to clear the bricks here as we are converting a bunch of ephemeral objects to gen2
        // and need to make sure that there are no left over bricks from the previous GCs for the space
        // we just used for gen0 allocation. We will need to go through the bricks for these objects for
        // ephemeral GCs later.
        for (size_t b = brick_of (generation_allocation_start (generation_of (0)));
             b < brick_of (align_on_brick (heap_segment_allocated (ephemeral_heap_segment)));
             b++)
        {
            set_brick (b, -1);
        }

        size_t ephemeral_size = (heap_segment_allocated (ephemeral_heap_segment) -
                                generation_allocation_start (generation_of (max_generation - 1)));
        heap_segment_next (ephemeral_heap_segment) = new_seg;
        ephemeral_heap_segment = new_seg;
        uint8_t*  start = heap_segment_mem (ephemeral_heap_segment);

        for (int i = condemned_gen_number; i >= 0; i--)
        {
            size_t gen_start_size = Align (min_obj_size);
            make_generation (i, ephemeral_heap_segment, start);

            generation* gen = generation_of (i);
            generation_plan_allocation_start (gen) = start;
            generation_plan_allocation_start_size (gen) = gen_start_size;
            start += gen_start_size;
        }
        heap_segment_used (ephemeral_heap_segment) = start - plug_skew;
        heap_segment_plan_allocated (ephemeral_heap_segment) = start;

        fix_generation_bounds (condemned_gen_number, generation_of (0));

        dd_gc_new_allocation (dynamic_data_of (max_generation)) -= ephemeral_size;
        dd_new_allocation (dynamic_data_of (max_generation)) = dd_gc_new_allocation (dynamic_data_of (max_generation));

        adjust_ephemeral_limits();
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

#endif //USE_REGIONS

// Only to be done on the thread that calls restart in a join for server GC
// and reset the oom status per heap.
void gc_heap::check_and_set_no_gc_oom()
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
        if (hp->no_gc_oom_p)
        {
            current_no_gc_region_info.start_status = start_no_gc_no_memory;
            hp->no_gc_oom_p = false;
        }
    }
#else
    if (no_gc_oom_p)
    {
        current_no_gc_region_info.start_status = start_no_gc_no_memory;
        no_gc_oom_p = false;
    }
#endif //MULTIPLE_HEAPS
}

void gc_heap::allocate_for_no_gc_after_gc()
{
    if (current_no_gc_region_info.minimal_gc_p)
        repair_allocation_contexts (TRUE);

    no_gc_oom_p = false;

    if (current_no_gc_region_info.start_status != start_no_gc_no_memory)
    {
        if (current_no_gc_region_info.soh_allocation_size != 0)
        {
#ifdef USE_REGIONS
            no_gc_oom_p = !extend_soh_for_no_gc();
#else
            if (((size_t)(heap_segment_reserved (ephemeral_heap_segment) - heap_segment_allocated (ephemeral_heap_segment)) < soh_allocation_no_gc) ||
                (!grow_heap_segment (ephemeral_heap_segment, (heap_segment_allocated (ephemeral_heap_segment) + soh_allocation_no_gc))))
            {
                no_gc_oom_p = true;
            }
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
            gc_t_join.join(this, gc_join_after_commit_soh_no_gc);
            if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
            {
                check_and_set_no_gc_oom();

#ifdef MULTIPLE_HEAPS
                gc_t_join.restart();
#endif //MULTIPLE_HEAPS
            }
        }

        if ((current_no_gc_region_info.start_status == start_no_gc_success) &&
            !(current_no_gc_region_info.minimal_gc_p) &&
            (current_no_gc_region_info.loh_allocation_size != 0))
        {
            gc_policy = policy_compact;
            saved_loh_segment_no_gc = 0;

            if (!find_loh_free_for_no_gc())
            {
                heap_segment* seg = generation_allocation_segment (generation_of (loh_generation));
                BOOL found_seg_p = FALSE;
                while (seg)
                {
                    if ((size_t)(heap_segment_reserved (seg) - heap_segment_allocated (seg)) >= loh_allocation_no_gc)
                    {
                        found_seg_p = TRUE;
                        if (!commit_loh_for_no_gc (seg))
                        {
                            no_gc_oom_p = true;
                            break;
                        }
                    }
                    seg = heap_segment_next (seg);
                }

                if (!found_seg_p)
                    gc_policy = policy_expand;
            }

#ifdef MULTIPLE_HEAPS
            gc_t_join.join(this, gc_join_expand_loh_no_gc);
            if (gc_t_join.joined())
            {
                check_and_set_no_gc_oom();

                if (current_no_gc_region_info.start_status == start_no_gc_success)
                {
                    for (int i = 0; i < n_heaps; i++)
                    {
                        gc_heap* hp = g_heaps[i];
                        if (hp->gc_policy == policy_expand)
                        {
                            hp->saved_loh_segment_no_gc = get_segment_for_uoh (loh_generation, get_uoh_seg_size (loh_allocation_no_gc), hp);
                            if (!(hp->saved_loh_segment_no_gc))
                            {
                                current_no_gc_region_info.start_status = start_no_gc_no_memory;
                                break;
                            }
                        }
                    }
                }

                gc_t_join.restart();
            }
#else //MULTIPLE_HEAPS
            check_and_set_no_gc_oom();

            if ((current_no_gc_region_info.start_status == start_no_gc_success) && (gc_policy == policy_expand))
            {
                saved_loh_segment_no_gc = get_segment_for_uoh (loh_generation, get_uoh_seg_size (loh_allocation_no_gc));
                if (!saved_loh_segment_no_gc)
                    current_no_gc_region_info.start_status = start_no_gc_no_memory;
            }
#endif //MULTIPLE_HEAPS

            if ((current_no_gc_region_info.start_status == start_no_gc_success) && saved_loh_segment_no_gc)
            {
                if (!commit_loh_for_no_gc (saved_loh_segment_no_gc))
                {
                    no_gc_oom_p = true;
                }
            }
        }
    }

#ifdef MULTIPLE_HEAPS
    gc_t_join.join(this, gc_join_final_no_gc);
    if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
        check_and_set_no_gc_oom();

        if (current_no_gc_region_info.start_status == start_no_gc_success)
        {
            set_allocations_for_no_gc();
            current_no_gc_region_info.started = TRUE;
        }

#ifdef MULTIPLE_HEAPS
        gc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }
}

enable_no_gc_region_callback_status gc_heap::enable_no_gc_callback(NoGCRegionCallbackFinalizerWorkItem* callback, uint64_t callback_threshold)
{
    dprintf(1, ("[no_gc_callback] calling enable_no_gc_callback with callback_threshold = %llu\n", callback_threshold));
    enable_no_gc_region_callback_status status = enable_no_gc_region_callback_status::succeed;
    suspend_EE();
    {
        if (!current_no_gc_region_info.started)
        {
            status = enable_no_gc_region_callback_status::not_started;
        }
        else if (current_no_gc_region_info.callback != nullptr)
        {
            status = enable_no_gc_region_callback_status::already_registered;
        }
        else
        {
            uint64_t total_original_soh_budget = 0;
            uint64_t total_original_loh_budget = 0;
#ifdef MULTIPLE_HEAPS
            for (int i = 0; i < gc_heap::n_heaps; i++)
            {
                gc_heap* hp = gc_heap::g_heaps [i];
#else
            {
                gc_heap* hp = pGenGCHeap;
#endif
                total_original_soh_budget += hp->soh_allocation_no_gc;
                total_original_loh_budget += hp->loh_allocation_no_gc;
            }
            uint64_t total_original_budget = total_original_soh_budget + total_original_loh_budget;
            if (total_original_budget >= callback_threshold)
            {
                uint64_t total_withheld = total_original_budget - callback_threshold;

                float soh_ratio = ((float)total_original_soh_budget)/total_original_budget;
                float loh_ratio = ((float)total_original_loh_budget)/total_original_budget;

                size_t soh_withheld_budget = (size_t)(soh_ratio * total_withheld);
                size_t loh_withheld_budget = (size_t)(loh_ratio * total_withheld);

#ifdef MULTIPLE_HEAPS
                soh_withheld_budget = soh_withheld_budget / gc_heap::n_heaps;
                loh_withheld_budget = loh_withheld_budget / gc_heap::n_heaps;
#endif
                soh_withheld_budget = max(soh_withheld_budget, (size_t)1);
                soh_withheld_budget = Align(soh_withheld_budget, get_alignment_constant (TRUE));
                loh_withheld_budget = Align(loh_withheld_budget, get_alignment_constant (FALSE));
#ifdef MULTIPLE_HEAPS
                for (int i = 0; i < gc_heap::n_heaps; i++)
                {
                    gc_heap* hp = gc_heap::g_heaps [i];
#else
                {
                    gc_heap* hp = pGenGCHeap;
#endif
                    if (dd_new_allocation (hp->dynamic_data_of (soh_gen0)) <= (ptrdiff_t)soh_withheld_budget)
                    {
                        dprintf(1, ("[no_gc_callback] failed because of running out of soh budget= %llu\n", soh_withheld_budget));
                        status = insufficient_budget;
                    }
                    if (dd_new_allocation (hp->dynamic_data_of (loh_generation)) <= (ptrdiff_t)loh_withheld_budget)
                    {
                        dprintf(1, ("[no_gc_callback] failed because of running out of loh budget= %llu\n", loh_withheld_budget));
                        status = insufficient_budget;
                    }
                }

                if (status == enable_no_gc_region_callback_status::succeed)
                {
                    dprintf(1, ("[no_gc_callback] enabling succeed\n"));
#ifdef MULTIPLE_HEAPS
                    for (int i = 0; i < gc_heap::n_heaps; i++)
                    {
                        gc_heap* hp = gc_heap::g_heaps [i];
#else
                    {
                        gc_heap* hp = pGenGCHeap;
#endif
                        dd_new_allocation (hp->dynamic_data_of (soh_gen0)) -= soh_withheld_budget;
                        dd_new_allocation (hp->dynamic_data_of (loh_generation)) -= loh_withheld_budget;
                    }
                    current_no_gc_region_info.soh_withheld_budget = soh_withheld_budget;
                    current_no_gc_region_info.loh_withheld_budget = loh_withheld_budget;
                    current_no_gc_region_info.callback = callback;
                }
            }
            else
            {
                status = enable_no_gc_region_callback_status::insufficient_budget;
            }
        }
    }
    restart_EE();

    return status;
}
