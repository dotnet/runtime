// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

inline
BOOL is_induced_blocking (gc_reason reason)
{
    return ((reason == reason_induced) ||
            (reason == reason_lowmemory_blocking) ||
            (reason == reason_induced_compacting) ||
            (reason == reason_induced_aggressive) ||
            (reason == reason_lowmemory_host_blocking));
}

inline
int relative_index_power2_plug (size_t power2)
{
    int index = index_of_highest_set_bit (power2);
    assert (index <= MAX_INDEX_POWER2);

    return ((index < MIN_INDEX_POWER2) ? 0 : (index - MIN_INDEX_POWER2));
}

inline
int relative_index_power2_free_space (size_t power2)
{
    int index = index_of_highest_set_bit (power2);
    assert (index <= MAX_INDEX_POWER2);

    return ((index < MIN_INDEX_POWER2) ? -1 : (index - MIN_INDEX_POWER2));
}

inline
BOOL oddp (size_t integer)
{
    return (integer & 1) != 0;
}

// we only ever use this for WORDs.
size_t logcount (size_t word)
{
    //counts the number of high bits in a 16 bit word.
    assert (word < 0x10000);
    size_t count;
    count = (word & 0x5555) + ( (word >> 1 ) & 0x5555);
    count = (count & 0x3333) + ( (count >> 2) & 0x3333);
    count = (count & 0x0F0F) + ( (count >> 4) & 0x0F0F);
    count = (count & 0x00FF) + ( (count >> 8) & 0x00FF);
    return count;
}

#ifdef SHORT_PLUGS
inline
void clear_padding_in_expand (uint8_t* old_loc,
                              BOOL set_padding_on_saved_p,
                              mark* pinned_plug_entry)
{
    if (set_padding_on_saved_p)
    {
        clear_plug_padded (get_plug_start_in_saved (old_loc, pinned_plug_entry));
    }
    else
    {
        clear_plug_padded (old_loc);
    }
}

#endif //SHORT_PLUGS

void verify_qsort_array (uint8_t* *low, uint8_t* *high)
{
    uint8_t **i = 0;

    for (i = low+1; i <= high; i++)
    {
        if (*i < *(i-1))
        {
            FATAL_GC_ERROR();
        }
    }
}

int gc_heap::joined_generation_to_condemn (BOOL should_evaluate_elevation,
                                           int initial_gen,
                                           int current_gen,
                                           BOOL* blocking_collection_p
                                           STRESS_HEAP_ARG(int n_original))
{
    gc_data_global.gen_to_condemn_reasons.init();
#ifdef BGC_SERVO_TUNING
    if (settings.entry_memory_load == 0)
    {
        uint32_t current_memory_load = 0;
        uint64_t current_available_physical = 0;
        get_memory_info (&current_memory_load, &current_available_physical);

        settings.entry_memory_load = current_memory_load;
        settings.entry_available_physical_mem = current_available_physical;
    }
#endif //BGC_SERVO_TUNING

    int n = current_gen;
#ifdef MULTIPLE_HEAPS
    BOOL joined_last_gc_before_oom = FALSE;
    for (int i = 0; i < n_heaps; i++)
    {
        if (g_heaps[i]->last_gc_before_oom)
        {
            dprintf (GTC_LOG, ("h%d is setting blocking to TRUE", i));
            joined_last_gc_before_oom = TRUE;
            break;
        }
    }
#else
    BOOL joined_last_gc_before_oom = last_gc_before_oom;
#endif //MULTIPLE_HEAPS

    if (joined_last_gc_before_oom && settings.pause_mode != pause_low_latency)
    {
        assert (*blocking_collection_p);
    }

    if (should_evaluate_elevation && (n == max_generation))
    {
        dprintf (GTC_LOG, ("lock: %d(%d)",
            (settings.should_lock_elevation ? 1 : 0),
            settings.elevation_locked_count));

        if (settings.should_lock_elevation)
        {
            settings.elevation_locked_count++;
            if (settings.elevation_locked_count == 6)
            {
                settings.elevation_locked_count = 0;
            }
            else
            {
                n = max_generation - 1;
                gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_avoid_unproductive);
                settings.elevation_reduced = TRUE;
            }
        }
        else
        {
            settings.elevation_locked_count = 0;
        }
    }
    else
    {
        settings.should_lock_elevation = FALSE;
        settings.elevation_locked_count = 0;
    }

    if (provisional_mode_triggered && (n == max_generation))
    {
        // There are a few cases where we should not reduce the generation.
        if ((initial_gen == max_generation) || (settings.reason == reason_alloc_loh))
        {
            // If we are doing a full GC in the provisional mode, we always
            // make it blocking because we don't want to get into a situation
            // where foreground GCs are asking for a compacting full GC right away
            // and not getting it.
            dprintf (GTC_LOG, ("full GC induced, not reducing gen"));
            if (initial_gen == max_generation)
            {
                gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_pm_induced_fullgc_p);
            }
            else
            {
                gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_pm_alloc_loh);
            }
            *blocking_collection_p = TRUE;
        }
        else if (
#ifndef USE_REGIONS
                 should_expand_in_full_gc ||
#endif //!USE_REGIONS
                 joined_last_gc_before_oom)
        {
            dprintf (GTC_LOG, ("need full blocking GCs to expand heap or avoid OOM, not reducing gen"));
            assert (*blocking_collection_p);
        }
        else
        {
            dprintf (GTC_LOG, ("reducing gen in PM: %d->%d->%d", initial_gen, n, (max_generation - 1)));
            gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_gen1_in_pm);
            n = max_generation - 1;
        }
    }

#ifndef USE_REGIONS
    if (should_expand_in_full_gc)
    {
        should_expand_in_full_gc = FALSE;
    }
#endif //!USE_REGIONS

    if (heap_hard_limit)
    {
        // If we have already consumed 90% of the limit, we should check to see if we should compact LOH.
        // TODO: should unify this with gen2.
        dprintf (GTC_LOG, ("committed %zd is %d%% of limit %zd",
            current_total_committed, (int)((float)current_total_committed * 100.0 / (float)heap_hard_limit),
            heap_hard_limit));

        bool full_compact_gc_p = false;

        if (joined_last_gc_before_oom)
        {
            gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_limit_before_oom);
            full_compact_gc_p = true;
        }
        else if (((uint64_t)current_total_committed * (uint64_t)10) >= ((uint64_t)heap_hard_limit * (uint64_t)9))
        {
            size_t loh_frag = get_total_gen_fragmentation (loh_generation);

            // If the LOH frag is >= 1/8 it's worth compacting it
            if (loh_frag >= heap_hard_limit / 8)
            {
                dprintf (GTC_LOG, ("loh frag: %zd > 1/8 of limit %zd", loh_frag, (heap_hard_limit / 8)));
                gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_limit_loh_frag);
                full_compact_gc_p = true;
            }
            else
            {
                // If there's not much fragmentation but it looks like it'll be productive to
                // collect LOH, do that.
                size_t est_loh_reclaim = get_total_gen_estimated_reclaim (loh_generation);
                if (est_loh_reclaim >= heap_hard_limit / 8)
                {
                    gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_limit_loh_reclaim);
                    full_compact_gc_p = true;
                }
                dprintf (GTC_LOG, ("loh est reclaim: %zd, 1/8 of limit %zd", est_loh_reclaim, (heap_hard_limit / 8)));
            }
        }

        if (full_compact_gc_p)
        {
            n = max_generation;
            *blocking_collection_p = TRUE;
            settings.loh_compaction = TRUE;
            dprintf (GTC_LOG, ("compacting LOH due to hard limit"));
        }
    }

    if ((conserve_mem_setting != 0) && (n == max_generation))
    {
        float frag_limit = 1.0f - conserve_mem_setting / 10.0f;

        size_t loh_size = get_total_gen_size (loh_generation);
        size_t gen2_size = get_total_gen_size (max_generation);
        float loh_frag_ratio = 0.0f;
        float combined_frag_ratio = 0.0f;
        if (loh_size != 0)
        {
            size_t loh_frag  = get_total_gen_fragmentation (loh_generation);
            size_t gen2_frag = get_total_gen_fragmentation (max_generation);
            loh_frag_ratio = (float)loh_frag / (float)loh_size;
            combined_frag_ratio = (float)(gen2_frag + loh_frag) / (float)(gen2_size + loh_size);
        }
        if (combined_frag_ratio > frag_limit)
        {
            dprintf (GTC_LOG, ("combined frag: %f > limit %f, loh frag: %f", combined_frag_ratio, frag_limit, loh_frag_ratio));
            gc_data_global.gen_to_condemn_reasons.set_condition (gen_max_high_frag_p);

            n = max_generation;
            *blocking_collection_p = TRUE;
            if (loh_frag_ratio > frag_limit)
            {
                settings.loh_compaction = TRUE;

                dprintf (GTC_LOG, ("compacting LOH due to GCConserveMem setting"));
            }
        }
    }

    if (settings.reason == reason_induced_aggressive)
    {
        gc_data_global.gen_to_condemn_reasons.set_condition (gen_joined_aggressive);
        settings.loh_compaction = TRUE;
    }

#ifdef BGC_SERVO_TUNING
    if (bgc_tuning::should_trigger_ngc2())
    {
        gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_servo_ngc);
        n = max_generation;
        *blocking_collection_p = TRUE;
    }

    if ((n < max_generation) && !gc_heap::background_running_p() &&
        bgc_tuning::stepping_trigger (settings.entry_memory_load, get_current_gc_index (max_generation)))
    {
        gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_servo_initial);
        n = max_generation;
        saved_bgc_tuning_reason = reason_bgc_stepping;
    }

    if ((n < max_generation) && bgc_tuning::should_trigger_bgc())
    {
        gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_servo_bgc);
        n = max_generation;
    }

    if (n == (max_generation - 1))
    {
        if (bgc_tuning::should_delay_alloc (max_generation))
        {
            gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_servo_postpone);
            n -= 1;
        }
    }
#endif //BGC_SERVO_TUNING

    if ((n == max_generation) && (*blocking_collection_p == FALSE))
    {
        // If we are doing a gen2 we should reset elevation regardless and let the gen2
        // decide if we should lock again or in the bgc case by design we will not retract
        // gen1 start.
        settings.should_lock_elevation = FALSE;
        settings.elevation_locked_count = 0;
        dprintf (GTC_LOG, ("doing bgc, reset elevation"));
    }

#ifdef STRESS_HEAP
#ifdef BACKGROUND_GC
    // We can only do Concurrent GC Stress if the caller did not explicitly ask for all
    // generations to be collected,
    //
    // [LOCALGC TODO] STRESS_HEAP is not defined for a standalone GC so there are multiple
    // things that need to be fixed in this code block.
    if (n_original != max_generation &&
        g_pConfig->GetGCStressLevel() && gc_can_use_concurrent)
    {
#ifndef FEATURE_NATIVEAOT
        if (*blocking_collection_p)
        {
            // We call StressHeap() a lot for Concurrent GC Stress. However,
            // if we can not do a concurrent collection, no need to stress anymore.
            // @TODO: Enable stress when the memory pressure goes down again
            GCStressPolicy::GlobalDisable();
        }
        else
#endif // !FEATURE_NATIVEAOT
        {
            gc_data_global.gen_to_condemn_reasons.set_condition(gen_joined_stress);
            n = max_generation;
        }
    }
#endif //BACKGROUND_GC
#endif //STRESS_HEAP

#ifdef BACKGROUND_GC
#ifdef DYNAMIC_HEAP_COUNT
    if (trigger_bgc_for_rethreading_p)
    {
        if (background_running_p())
        {
            // trigger_bgc_for_rethreading_p being true indicates we did not change gen2 FL items when we changed HC.
            // So some heaps could have no FL at all which means if we did a gen1 GC during this BGC we would increase
            // gen2 size. We chose to prioritize not increasing gen2 size so we disallow gen1 GCs.
            if (n != 0)
            {
                n = 0;
            }
        }
        else
        {
            dprintf (6666, ("was going to be g%d %s GC, HC change request this GC to be a BGC unless it's an NGC2",
                n, (*blocking_collection_p ? "blocking" : "non blocking")));

            // If we already decided to do a blocking gen2 which would also achieve the purpose of building up a new
            // gen2 FL, let it happen; otherwise we want to trigger a BGC.
            if (!((n == max_generation) && *blocking_collection_p))
            {
                n = max_generation;

#ifdef STRESS_DYNAMIC_HEAP_COUNT
                if (bgc_to_ngc2_ratio)
                {
                    int r = (int)gc_rand::get_rand ((bgc_to_ngc2_ratio + 1) * 10);
                    dprintf (6666, ("%d - making this full GC %s", r, ((r < 10) ? "NGC2" : "BGC")));
                    if (r < 10)
                    {
                        *blocking_collection_p = TRUE;
                    }
                }
#endif //STRESS_DYNAMIC_HEAP_COUNT
            }
        }
    }
    else
#endif //DYNAMIC_HEAP_COUNT
    if ((n == max_generation) && background_running_p())
    {
        n = max_generation - 1;
        dprintf (GTC_LOG, ("bgc in progress - 1 instead of 2"));
    }
#endif //BACKGROUND_GC

#ifdef DYNAMIC_HEAP_COUNT
    if (trigger_initial_gen2_p)
    {
#ifdef BACKGROUND_GC
        assert (!trigger_bgc_for_rethreading_p);
        assert (!background_running_p());
#endif //BACKGROUND_GC

        if (n != max_generation)
        {
            n = max_generation;
            *blocking_collection_p = FALSE;

            dprintf (6666, ("doing the 1st gen2 GC requested by DATAS"));
        }

        trigger_initial_gen2_p = false;
    }
#endif //DYNAMIC_HEAP_COUNT

    return n;
}

inline
size_t get_survived_size (gc_history_per_heap* hist)
{
    size_t surv_size = 0;
    gc_generation_data* gen_data;

    for (int gen_number = 0; gen_number < total_generation_count; gen_number++)
    {
        gen_data = &(hist->gen_data[gen_number]);
        surv_size += (gen_data->size_after -
                      gen_data->free_list_space_after -
                      gen_data->free_obj_space_after);
    }

    return surv_size;
}

size_t gc_heap::get_total_survived_size()
{
    size_t total_surv_size = 0;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        gc_history_per_heap* current_gc_data_per_heap = hp->get_gc_data_per_heap();
        total_surv_size += get_survived_size (current_gc_data_per_heap);
    }
#else
    gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();
    total_surv_size = get_survived_size (current_gc_data_per_heap);
#endif //MULTIPLE_HEAPS
    return total_surv_size;
}

void gc_heap::get_total_allocated_since_last_gc (size_t* oh_allocated)
{
    memset (oh_allocated, 0, (total_oh_count * sizeof (size_t)));
    size_t total_allocated_size = 0;

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        for (int oh_idx = 0; oh_idx < total_oh_count; oh_idx++)
        {
            oh_allocated[oh_idx] += hp->allocated_since_last_gc[oh_idx];
            hp->allocated_since_last_gc[oh_idx] = 0;
        }
    }
}

// Gets what's allocated on both SOH, LOH, etc that hasn't been collected.
size_t gc_heap::get_current_allocated()
{
    dynamic_data* dd = dynamic_data_of (0);
    size_t current_alloc = dd_desired_allocation (dd) - dd_new_allocation (dd);
    for (int i = uoh_start_generation; i < total_generation_count; i++)
    {
        dynamic_data* dd = dynamic_data_of (i);
        current_alloc += dd_desired_allocation (dd) - dd_new_allocation (dd);
    }
    return current_alloc;
}

size_t gc_heap::get_total_allocated()
{
    size_t total_current_allocated = 0;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        total_current_allocated += hp->get_current_allocated();
    }
#else
    total_current_allocated = get_current_allocated();
#endif //MULTIPLE_HEAPS
    return total_current_allocated;
}

size_t gc_heap::get_total_promoted()
{
    size_t total_promoted_size = 0;
    int highest_gen = ((settings.condemned_generation == max_generation) ?
                       (total_generation_count - 1) : settings.condemned_generation);
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        for (int gen_number = 0; gen_number <= highest_gen; gen_number++)
        {
            total_promoted_size += dd_promoted_size (hp->dynamic_data_of (gen_number));
        }
    }
    return total_promoted_size;
}

#ifdef BGC_SERVO_TUNING
size_t gc_heap::get_total_generation_size (int gen_number)
{
    size_t total_generation_size = 0;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        total_generation_size += hp->generation_size (gen_number);
    }
    return total_generation_size;
}

// gets all that's allocated into the gen. This is only used for gen2/3
// for servo tuning.
size_t gc_heap::get_total_servo_alloc (int gen_number)
{
    size_t total_alloc = 0;

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        generation* gen = hp->generation_of (gen_number);
        total_alloc += generation_free_list_allocated (gen);
        total_alloc += generation_end_seg_allocated (gen);
        total_alloc += generation_condemned_allocated (gen);
        total_alloc += generation_sweep_allocated (gen);
    }

    return total_alloc;
}

size_t gc_heap::get_total_bgc_promoted()
{
    size_t total_bgc_promoted = 0;
#ifdef MULTIPLE_HEAPS
    int num_heaps = gc_heap::n_heaps;
#else //MULTIPLE_HEAPS
    int num_heaps = 1;
#endif //MULTIPLE_HEAPS

    for (int i = 0; i < num_heaps; i++)
    {
        total_bgc_promoted += bpromoted_bytes (i);
    }
    return total_bgc_promoted;
}

// This is called after compute_new_dynamic_data is called, at which point
// dd_current_size is calculated.
size_t gc_heap::get_total_surv_size (int gen_number)
{
    size_t total_surv_size = 0;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        total_surv_size += dd_current_size (hp->dynamic_data_of (gen_number));
    }
    return total_surv_size;
}

size_t gc_heap::get_total_begin_data_size (int gen_number)
{
    size_t total_begin_data_size = 0;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        total_begin_data_size += dd_begin_data_size (hp->dynamic_data_of (gen_number));
    }
    return total_begin_data_size;
}

size_t gc_heap::get_total_generation_fl_size (int gen_number)
{
    size_t total_generation_fl_size = 0;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        total_generation_fl_size += generation_free_list_space (hp->generation_of (gen_number));
    }
    return total_generation_fl_size;
}

size_t gc_heap::get_current_gc_index (int gen_number)
{
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = gc_heap::g_heaps[0];
    return dd_collection_count (hp->dynamic_data_of (gen_number));
#else
    return dd_collection_count (dynamic_data_of (gen_number));
#endif //MULTIPLE_HEAPS
}

#endif //BGC_SERVO_TUNING

size_t gc_heap::current_generation_size (int gen_number)
{
    dynamic_data* dd = dynamic_data_of (gen_number);
    size_t gen_size = (dd_current_size (dd) + dd_desired_allocation (dd)
                        - dd_new_allocation (dd));

    return gen_size;
}

#ifdef USE_REGIONS
// We may need a new empty region while doing a GC so try to get one now, if we don't have any
// reserve in the free region list.
bool gc_heap::try_get_new_free_region()
{
    heap_segment* region = 0;
    if (free_regions[basic_free_region].get_num_free_regions() > 0)
    {
        dprintf (REGIONS_LOG, ("h%d has %zd free regions %p", heap_number, free_regions[basic_free_region].get_num_free_regions(),
            heap_segment_mem (free_regions[basic_free_region].get_first_free_region())));
        return true;
    }
    else
    {
        region = allocate_new_region (__this, 0, false);
        if (region)
        {
            if (init_table_for_region (0, region))
            {
                return_free_region (region);
                dprintf (REGIONS_LOG, ("h%d got a new empty region %p", heap_number, region));
            }
            else
            {
                region = 0;
            }
        }
    }

    return (region != 0);
}

bool gc_heap::init_table_for_region (int gen_number, heap_segment* region)
{
#ifdef BACKGROUND_GC
    dprintf (GC_TABLE_LOG, ("new seg %Ix, mark_array is %Ix",
        heap_segment_mem (region), mark_array));
    if (((region->flags & heap_segment_flags_ma_committed) == 0) &&
        !commit_mark_array_new_seg (__this, region))
    {
        dprintf (GC_TABLE_LOG, ("failed to commit mark array for the new region %Ix-%Ix",
            get_region_start (region), heap_segment_reserved (region)));

        // We don't have memory to commit the mark array so we cannot use the new region.
        decommit_region (region, gen_to_oh (gen_number), heap_number);
        return false;
    }
    if ((region->flags & heap_segment_flags_ma_committed) != 0)
    {
        bgc_verify_mark_array_cleared (region, true);
    }
#endif //BACKGROUND_GC

    if (gen_number <= max_generation)
    {
        size_t first_brick = brick_of (heap_segment_mem (region));
        set_brick (first_brick, -1);
    }
    else
    {
        assert (brick_table[brick_of (heap_segment_mem (region))] == 0);
    }

    return true;
}

#endif //USE_REGIONS

// The following 2 methods Use integer division to prevent potential floating point exception.
// FPE may occur if we use floating point division because of speculative execution.
//
// Return the percentage of efficiency (between 0 and 100) of the allocator.
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
            return 0;
        return (size_t)((100 * free_list_allocated) / (free_list_allocated + free_obj_space));
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

/*
    This is called by when we are actually doing a GC, or when we are just checking whether
    we would do a full blocking GC, in which case check_only_p is TRUE.

    The difference between calling this with check_only_p TRUE and FALSE is that when it's
    TRUE:
            settings.reason is ignored
            budgets are not checked (since they are checked before this is called)
            it doesn't change anything non local like generation_skip_ratio
*/
int gc_heap::generation_to_condemn (int n_initial,
                                    BOOL* blocking_collection_p,
                                    BOOL* elevation_requested_p,
                                    BOOL check_only_p)
{
    gc_mechanisms temp_settings = settings;
    gen_to_condemn_tuning temp_condemn_reasons;
    gc_mechanisms* local_settings = (check_only_p ? &temp_settings : &settings);
    gen_to_condemn_tuning* local_condemn_reasons = (check_only_p ? &temp_condemn_reasons : &gen_to_condemn_reasons);
    if (!check_only_p)
    {
        if ((local_settings->reason == reason_oos_soh) || (local_settings->reason == reason_oos_loh))
        {
            assert (n_initial >= 1);
        }

        assert (settings.reason != reason_empty);
    }

    local_condemn_reasons->init();

    int n = n_initial;
    int n_alloc = n;
    if (heap_number == 0)
    {
        dprintf (6666, ("init: %d(%d)", n_initial, settings.reason));
    }
    int i = 0;
    int temp_gen = 0;
    BOOL low_memory_detected = g_low_memory_status;
    uint32_t memory_load = 0;
    uint64_t available_physical = 0;
    uint64_t available_page_file = 0;
    BOOL check_memory = FALSE;
    BOOL high_fragmentation  = FALSE;
    BOOL v_high_memory_load  = FALSE;
    BOOL high_memory_load    = FALSE;
    BOOL low_ephemeral_space = FALSE;
    BOOL evaluate_elevation  = TRUE;
    *elevation_requested_p   = FALSE;
    *blocking_collection_p   = FALSE;

    BOOL check_max_gen_alloc = TRUE;

#ifdef STRESS_HEAP
    int orig_gen = n;
#endif //STRESS_HEAP

    if (!check_only_p)
    {
        dd_fragmentation (dynamic_data_of (0)) =
            generation_free_list_space (youngest_generation) +
            generation_free_obj_space (youngest_generation);

        for (int i = uoh_start_generation; i < total_generation_count; i++)
        {
            dd_fragmentation (dynamic_data_of (i)) =
                generation_free_list_space (generation_of (i)) +
                generation_free_obj_space (generation_of (i));
        }

        //save new_allocation
        for (i = 0; i < total_generation_count; i++)
        {
            dynamic_data* dd = dynamic_data_of (i);
            if ((dd_new_allocation (dd) < 0) && (i >= 2))
            {
                dprintf (6666, ("h%d: g%d: l: %zd (%zd)",
                    heap_number, i,
                    dd_new_allocation (dd),
                    dd_desired_allocation (dd)));
            }
            dd_gc_new_allocation (dd) = dd_new_allocation (dd);
        }

        local_condemn_reasons->set_gen (gen_initial, n);
        temp_gen = n;

#ifdef BACKGROUND_GC
        if (gc_heap::background_running_p()
#ifdef BGC_SERVO_TUNING
            || bgc_tuning::fl_tuning_triggered
            || (bgc_tuning::enable_fl_tuning && bgc_tuning::use_stepping_trigger_p)
#endif //BGC_SERVO_TUNING
            )
        {
            check_max_gen_alloc = FALSE;
        }
#endif //BACKGROUND_GC

        if (check_max_gen_alloc)
        {
            //figure out if UOH objects need to be collected.
            for (int i = uoh_start_generation; i < total_generation_count; i++)
            {
                if (get_new_allocation (i) <= 0)
                {
                    n = max_generation;
                    local_condemn_reasons->set_gen (gen_alloc_budget, n);
                    dprintf (BGC_TUNING_LOG, ("BTL[GTC]: trigger based on gen%d b: %zd",
                             (i),
                             get_new_allocation (i)));
                    break;
                }
            }
        }

        //figure out which generation ran out of allocation
        for (i = n+1; i <= (check_max_gen_alloc ? max_generation : (max_generation - 1)); i++)
        {
            if (get_new_allocation (i) <= 0)
            {
                n = i;
                if (n == max_generation)
                {
                    dprintf (BGC_TUNING_LOG, ("BTL[GTC]: trigger based on gen2 b: %zd",
                            get_new_allocation (max_generation)));
                }
            }
            else
                break;
        }
    }

    if (n > temp_gen)
    {
        local_condemn_reasons->set_gen (gen_alloc_budget, n);
    }

    if (n > 0)
    {
        dprintf (6666, ("h%d: g%d budget", heap_number, ((get_new_allocation (loh_generation) <= 0) ? 3 : n)));
    }

    n_alloc = n;

#if defined(BACKGROUND_GC) && !defined(MULTIPLE_HEAPS)
    //time based tuning
    // if enough time has elapsed since the last gc
    // and the number of gc is too low (1/10 of lower gen) then collect
    // This should also be enabled if we have memory concerns
    int n_time_max = max_generation;

    if (!check_only_p)
    {
        if (!check_max_gen_alloc)
        {
            n_time_max = max_generation - 1;
        }
    }

    if ((local_settings->pause_mode == pause_interactive) ||
        (local_settings->pause_mode == pause_sustained_low_latency))
    {
        dynamic_data* dd0 = dynamic_data_of (0);
        uint64_t now = GetHighPrecisionTimeStamp();
        temp_gen = n;
        for (i = (temp_gen+1); i <= n_time_max; i++)
        {
            dynamic_data* dd = dynamic_data_of (i);
            if ((now > dd_time_clock(dd) + dd_time_clock_interval(dd)) &&
                (dd_gc_clock (dd0) > (dd_gc_clock (dd) + dd_gc_clock_interval(dd))) &&
                ((n < max_generation) || ((dd_current_size (dd) < dd_max_size (dd0)))))
            {
                n = min (i, n_time_max);
                dprintf (GTC_LOG, ("time %d", n));
            }
        }
        if (n > temp_gen)
        {
            local_condemn_reasons->set_gen (gen_time_tuning, n);
            if (n == max_generation)
            {
                dprintf (BGC_TUNING_LOG, ("BTL[GTC]: trigger based on time"));
            }
        }
    }

    if (n != n_alloc)
    {
        dprintf (GTC_LOG, ("Condemning %d based on time tuning and fragmentation", n));
    }
#endif //BACKGROUND_GC && !MULTIPLE_HEAPS

    if (n < (max_generation - 1))
    {
        dprintf (6666, ("h%d: skip %d", heap_number, generation_skip_ratio));

        if (dt_low_card_table_efficiency_p (tuning_deciding_condemned_gen))
        {
            n = max (n, max_generation - 1);
            local_settings->promotion = TRUE;
            dprintf (2, ("h%d: skip %d, c %d",
                        heap_number, generation_skip_ratio, n));
            local_condemn_reasons->set_condition (gen_low_card_p);
        }
    }

    if (!check_only_p)
    {
        generation_skip_ratio = 100;
    }

    if (dt_low_ephemeral_space_p (check_only_p ?
                                  tuning_deciding_full_gc :
                                  tuning_deciding_condemned_gen))
    {
        low_ephemeral_space = TRUE;

        n = max (n, max_generation - 1);
        local_condemn_reasons->set_condition (gen_low_ephemeral_p);
        dprintf (GTC_LOG, ("h%d: low eph", heap_number));

        if (!provisional_mode_triggered)
        {
#ifdef BACKGROUND_GC
            if (!gc_can_use_concurrent || (generation_free_list_space (generation_of (max_generation)) == 0))
#endif //BACKGROUND_GC
            {
                //It is better to defragment first if we are running out of space for
                //the ephemeral generation but we have enough fragmentation to make up for it
                //in the non ephemeral generation. Essentially we are trading a gen2 for
                // having to expand heap in ephemeral collections.
                if (dt_high_frag_p (tuning_deciding_condemned_gen,
                                    max_generation - 1,
                                    TRUE))
                {
                    high_fragmentation = TRUE;
                    local_condemn_reasons->set_condition (gen_max_high_frag_e_p);
                    dprintf (6666, ("heap%d: gen1 frag", heap_number));
                }
            }
        }
    }

#ifdef USE_REGIONS
    if (!check_only_p)
    {
        if (!try_get_new_free_region())
        {
            dprintf (GTC_LOG, ("can't get an empty region -> full compacting"));
            last_gc_before_oom = TRUE;
        }
    }
#endif //USE_REGIONS

    //figure out which ephemeral generation is too fragmented
    temp_gen = n;
    for (i = n+1; i < max_generation; i++)
    {
        if (dt_high_frag_p (tuning_deciding_condemned_gen, i))
        {
            dprintf (6666, ("h%d g%d too frag", heap_number, i));
            n = i;
        }
        else
            break;
    }

    if (low_ephemeral_space)
    {
        //enable promotion
        local_settings->promotion = TRUE;
    }

    if (n > temp_gen)
    {
        local_condemn_reasons->set_condition (gen_eph_high_frag_p);
    }

    if (!check_only_p)
    {
        if (settings.pause_mode == pause_low_latency)
        {
            if (!is_induced (settings.reason))
            {
                n = min (n, max_generation - 1);
                dprintf (GTC_LOG, ("low latency mode is enabled, condemning %d", n));
                evaluate_elevation = FALSE;
                goto exit;
            }
        }
    }

    // It's hard to catch when we get to the point that the memory load is so high
    // we get an induced GC from the finalizer thread so we are checking the memory load
    // for every gen0 GC.
    check_memory = (check_only_p ?
                    (n >= 0) :
                    ((n >= 1) || low_memory_detected));

    if (check_memory)
    {
        //find out if we are short on memory
        get_memory_info (&memory_load, &available_physical, &available_page_file);
        if (heap_number == 0)
        {
            dprintf (GTC_LOG, ("ml: %d", memory_load));
        }

#ifdef USE_REGIONS
        // For regions we want to take the VA range into consideration as well.
        uint32_t va_memory_load = global_region_allocator.get_va_memory_load();
        if (heap_number == 0)
        {
            dprintf (GTC_LOG, ("h%d ML %d, va ML %d", heap_number, memory_load, va_memory_load));
        }
        memory_load = max (memory_load, va_memory_load);
#endif //USE_REGIONS

        // Need to get it early enough for all heaps to use.
        local_settings->entry_available_physical_mem = available_physical;
        local_settings->entry_memory_load = memory_load;

        // @TODO: Force compaction more often under GCSTRESS
        if (memory_load >= high_memory_load_th || low_memory_detected)
        {
#ifdef SIMPLE_DPRINTF
            // stress log can't handle any parameter that's bigger than a void*.
            if (heap_number == 0)
            {
                dprintf (GTC_LOG, ("tp: %zd, ap: %zd", total_physical_mem, available_physical));
            }
#endif //SIMPLE_DPRINTF

            high_memory_load = TRUE;

            if (memory_load >= v_high_memory_load_th || low_memory_detected)
            {
                // TODO: Perhaps in 64-bit we should be estimating gen1's fragmentation as well since
                // gen1/gen0 may take a lot more memory than gen2.
                if (!high_fragmentation)
                {
                    high_fragmentation = dt_estimate_reclaim_space_p (tuning_deciding_condemned_gen, max_generation);
                }
                v_high_memory_load = TRUE;
            }
            else
            {
                if (!high_fragmentation)
                {
                    high_fragmentation = dt_estimate_high_frag_p (tuning_deciding_condemned_gen, max_generation, available_physical);
                }
            }

            if (high_fragmentation)
            {
                dprintf (6666, ("h%d high frag true!! mem load %d", heap_number, memory_load));

                if (high_memory_load)
                {
                    local_condemn_reasons->set_condition (gen_max_high_frag_m_p);
                }
                else if (v_high_memory_load)
                {
                    local_condemn_reasons->set_condition (gen_max_high_frag_vm_p);
                }
            }
        }
    }

    dprintf (GTC_LOG, ("h%d: le: %d, hm: %d, vm: %d, f: %d",
                 heap_number, low_ephemeral_space, high_memory_load, v_high_memory_load,
                 high_fragmentation));

#ifndef USE_REGIONS
    if (should_expand_in_full_gc)
    {
        dprintf (GTC_LOG, ("h%d: expand_in_full - BLOCK", heap_number));
        *blocking_collection_p = TRUE;
        evaluate_elevation = FALSE;
        n = max_generation;
        local_condemn_reasons->set_condition (gen_expand_fullgc_p);
    }
#endif //!USE_REGIONS

    if (last_gc_before_oom)
    {
        dprintf (GTC_LOG, ("h%d: alloc full - BLOCK", heap_number));
        n = max_generation;
        *blocking_collection_p = TRUE;

        if ((local_settings->reason == reason_oos_loh) ||
            (local_settings->reason == reason_alloc_loh))
        {
            evaluate_elevation = FALSE;
        }

        local_condemn_reasons->set_condition (gen_before_oom);
    }

    if (!check_only_p)
    {
        if (is_induced_blocking (settings.reason) &&
            n_initial == max_generation
            IN_STRESS_HEAP( && !settings.stress_induced ))
        {
            if (heap_number == 0)
            {
                dprintf (GTC_LOG, ("induced - BLOCK"));
            }

            *blocking_collection_p = TRUE;
            local_condemn_reasons->set_condition (gen_induced_fullgc_p);
            evaluate_elevation = FALSE;
        }

        if (settings.reason == reason_induced_noforce)
        {
            local_condemn_reasons->set_condition (gen_induced_noforce_p);
            evaluate_elevation = FALSE;
        }
    }

    if (!provisional_mode_triggered && evaluate_elevation && (low_ephemeral_space || high_memory_load || v_high_memory_load))
    {
        *elevation_requested_p = TRUE;
#ifdef HOST_64BIT
        // if we are in high memory load and have consumed 10% of the gen2 budget, do a gen2 now.
        if (high_memory_load || v_high_memory_load)
        {
            dynamic_data* dd_max = dynamic_data_of (max_generation);
            if (((float)dd_new_allocation (dd_max) / (float)dd_desired_allocation (dd_max)) < 0.9)
            {
                dprintf (GTC_LOG, ("%zd left in gen2 alloc (%zd)",
                    dd_new_allocation (dd_max), dd_desired_allocation (dd_max)));
                n = max_generation;
                local_condemn_reasons->set_condition (gen_almost_max_alloc);
            }
        }

        if (n <= max_generation)
#endif // HOST_64BIT
        {
            if (high_fragmentation)
            {
                //elevate to max_generation
                n = max_generation;
                dprintf (GTC_LOG, ("h%d: f full", heap_number));

#ifdef BACKGROUND_GC
                if (high_memory_load || v_high_memory_load)
                {
                    // For background GC we want to do blocking collections more eagerly because we don't
                    // want to get into the situation where the memory load becomes high while we are in
                    // a background GC and we'd have to wait for the background GC to finish to start
                    // a blocking collection (right now the implementation doesn't handle converting
                    // a background GC to a blocking collection midway.
                    dprintf (GTC_LOG, ("h%d: bgc - BLOCK", heap_number));
                    *blocking_collection_p = TRUE;
                }
#else
                if (v_high_memory_load)
                {
                    dprintf (GTC_LOG, ("h%d: - BLOCK", heap_number));
                    *blocking_collection_p = TRUE;
                }
#endif //BACKGROUND_GC
            }
            else
            {
                n = max (n, max_generation - 1);
                dprintf (GTC_LOG, ("h%d: nf c %d", heap_number, n));
            }
        }
    }

    if (!provisional_mode_triggered && (n == (max_generation - 1)) && (n_alloc < (max_generation -1)))
    {
#ifdef BGC_SERVO_TUNING
        if (!bgc_tuning::enable_fl_tuning)
#endif //BGC_SERVO_TUNING
        {
            dprintf (GTC_LOG, ("h%d: budget %d, check 2",
                        heap_number, n_alloc));
            if (get_new_allocation (max_generation) <= 0)
            {
                dprintf (GTC_LOG, ("h%d: budget alloc", heap_number));
                n = max_generation;
                local_condemn_reasons->set_condition (gen_max_gen1);
            }
        }
    }

    //figure out if max_generation is too fragmented -> blocking collection
    if (!provisional_mode_triggered
#ifdef BGC_SERVO_TUNING
        && !bgc_tuning::enable_fl_tuning
#endif //BGC_SERVO_TUNING
        && (n == max_generation))
    {
        if (dt_high_frag_p (tuning_deciding_condemned_gen, n))
        {
            dprintf (6666, ("h%d: g%d too frag", heap_number, n));
            local_condemn_reasons->set_condition (gen_max_high_frag_p);
            if (local_settings->pause_mode != pause_sustained_low_latency)
            {
                *blocking_collection_p = TRUE;
            }
        }
    }

#ifdef BACKGROUND_GC
    if ((n == max_generation) && !(*blocking_collection_p))
    {
        if (heap_number == 0)
        {
            BOOL bgc_heap_too_small = TRUE;
            size_t gen2size = 0;
            size_t gen3size = 0;
#ifdef MULTIPLE_HEAPS
            for (int i = 0; i < n_heaps; i++)
            {
                if (((g_heaps[i]->current_generation_size (max_generation)) > bgc_min_per_heap) ||
                    ((g_heaps[i]->current_generation_size (loh_generation)) > bgc_min_per_heap) ||
                    ((g_heaps[i]->current_generation_size (poh_generation)) > bgc_min_per_heap))
                {
                    bgc_heap_too_small = FALSE;
                    break;
                }
            }
#else //MULTIPLE_HEAPS
            if ((current_generation_size (max_generation) > bgc_min_per_heap) ||
                (current_generation_size (loh_generation) > bgc_min_per_heap) ||
                (current_generation_size (poh_generation) > bgc_min_per_heap))
            {
                bgc_heap_too_small = FALSE;
            }
#endif //MULTIPLE_HEAPS

            if (bgc_heap_too_small)
            {
                dprintf (GTC_LOG, ("gen2 and gen3 too small"));

#ifdef STRESS_HEAP
                // do not turn stress-induced collections into blocking GCs
                if (!settings.stress_induced)
#endif //STRESS_HEAP
                {
                    *blocking_collection_p = TRUE;
                }

                local_condemn_reasons->set_condition (gen_gen2_too_small);
            }
        }
    }
#endif //BACKGROUND_GC

exit:
    if (!check_only_p)
    {
#ifdef STRESS_HEAP
#ifdef BACKGROUND_GC
        // We can only do Concurrent GC Stress if the caller did not explicitly ask for all
        // generations to be collected,

        if (orig_gen != max_generation &&
            g_pConfig->GetGCStressLevel() && gc_can_use_concurrent)
        {
            *elevation_requested_p = FALSE;
        }
#endif //BACKGROUND_GC
#endif //STRESS_HEAP

        if (check_memory)
        {
            fgm_result.available_pagefile_mb = (size_t)(available_page_file / (1024 * 1024));
        }

        local_condemn_reasons->set_gen (gen_final_per_heap, n);
        get_gc_data_per_heap()->gen_to_condemn_reasons.init (local_condemn_reasons);

#ifdef DT_LOG
        local_condemn_reasons->print (heap_number);
#endif //DT_LOG

        if ((local_settings->reason == reason_oos_soh) ||
            (local_settings->reason == reason_oos_loh))
        {
            assert (n >= 1);
        }
    }

    return n;
}

inline
size_t gc_heap::min_reclaim_fragmentation_threshold (uint32_t num_heaps)
{
    // if the memory load is higher, the threshold we'd want to collect gets lower.
    size_t min_mem_based_on_available =
        (500 - (settings.entry_memory_load - high_memory_load_th) * 40) * 1024 * 1024 / num_heaps;

    size_t ten_percent_size = (size_t)((float)generation_size (max_generation) * 0.10);
    uint64_t three_percent_mem = mem_one_percent * 3 / num_heaps;

#ifdef SIMPLE_DPRINTF
    dprintf (GTC_LOG, ("min av: %zd, 10%% gen2: %zd, 3%% mem: %zd",
        min_mem_based_on_available, ten_percent_size, three_percent_mem));
#endif //SIMPLE_DPRINTF
    return (size_t)(min ((uint64_t)min_mem_based_on_available, min ((uint64_t)ten_percent_size, three_percent_mem)));
}

inline
uint64_t gc_heap::min_high_fragmentation_threshold(uint64_t available_mem, uint32_t num_heaps)
{
    return min (available_mem, (uint64_t)(256*1024*1024)) / num_heaps;
}

void gc_heap::free_list_info (int gen_num, const char* msg)
{
#if defined (BACKGROUND_GC) && defined (TRACE_GC)
    dprintf (3, ("h%d: %s", heap_number, msg));
    for (int i = 0; i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        if ((generation_allocation_size (gen) == 0) &&
            (generation_free_list_space (gen) == 0) &&
            (generation_free_obj_space (gen) == 0))
        {
            // don't print if everything is 0.
        }
        else
        {
            dprintf (3, ("h%d: g%d: a-%zd, fl-%zd, fo-%zd",
                heap_number, i,
                generation_allocation_size (gen),
                generation_free_list_space (gen),
                generation_free_obj_space (gen)));
        }
    }
#else
    UNREFERENCED_PARAMETER(gen_num);
    UNREFERENCED_PARAMETER(msg);
#endif // BACKGROUND_GC && TRACE_GC
}

#ifdef DYNAMIC_HEAP_COUNT
void gc_heap::assign_new_budget (int gen_number, size_t desired_per_heap)
{
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        dynamic_data* dd = hp->dynamic_data_of (gen_number);
        dd_desired_allocation (dd) = desired_per_heap;
        dd_gc_new_allocation (dd) = desired_per_heap;
        dd_new_allocation (dd) = desired_per_heap;
        if (gen_number == 0)
        {
            hp->fgn_last_alloc = desired_per_heap;
        }
    }

    gc_data_global.final_youngest_desired = desired_per_heap;
}

bool gc_heap::prepare_rethread_fl_items()
{
    if (!min_fl_list)
    {
        min_fl_list = new (nothrow) min_fl_list_info [MAX_BUCKET_COUNT * n_max_heaps];
        if (min_fl_list == nullptr)
            return false;
    }
    if (!free_list_space_per_heap)
    {
        free_list_space_per_heap = new (nothrow) size_t[n_max_heaps];
        if (free_list_space_per_heap == nullptr)
            return false;
    }
    return true;
}

void gc_heap::rethread_fl_items(int gen_idx)
{
    uint32_t min_fl_list_size = sizeof (min_fl_list_info) * (MAX_BUCKET_COUNT * n_max_heaps);
    memset (min_fl_list, 0, min_fl_list_size);
    memset (free_list_space_per_heap, 0, sizeof(free_list_space_per_heap[0])*n_max_heaps);

    size_t num_fl_items = 0;
    size_t num_fl_items_rethreaded = 0;

    allocator* gen_allocator = generation_allocator (generation_of (gen_idx));
    gen_allocator->rethread_items (&num_fl_items, &num_fl_items_rethreaded, this, min_fl_list, free_list_space_per_heap, n_heaps);

    num_fl_items_rethreaded_stage2 = num_fl_items_rethreaded;
}

void gc_heap::merge_fl_from_other_heaps (int gen_idx, int to_n_heaps, int from_n_heaps)
{
#ifdef _DEBUG
    uint64_t start_us = GetHighPrecisionTimeStamp ();

    size_t total_num_fl_items_rethreaded_stage2 = 0;

    for (int hn = 0; hn < to_n_heaps; hn++)
    {
        gc_heap* hp = g_heaps[hn];

        total_num_fl_items_rethreaded_stage2 += hp->num_fl_items_rethreaded_stage2;

        min_fl_list_info* current_heap_min_fl_list = hp->min_fl_list;
        allocator* gen_allocator = generation_allocator (hp->generation_of (gen_idx));
        int num_buckets = gen_allocator->number_of_buckets();

        for (int i = 0; i < num_buckets; i++)
        {
            // Get to the bucket for this fl
            min_fl_list_info* current_bucket_min_fl_list = current_heap_min_fl_list + (i * to_n_heaps);
            for (int other_hn = 0; other_hn < from_n_heaps; other_hn++)
            {
                min_fl_list_info* min_fl_other_heap = &current_bucket_min_fl_list[other_hn];
                if (min_fl_other_heap->head)
                {
                    if (other_hn == hn)
                    {
                        dprintf (8888, ("h%d has fl items for itself on the temp list?!", hn));
                        GCToOSInterface::DebugBreak ();
                    }
                }
            }
        }
    }

    uint64_t elapsed = GetHighPrecisionTimeStamp () - start_us;

    dprintf (8888, ("rethreaded %Id items, merging took %I64dus (%I64dms)",
        total_num_fl_items_rethreaded_stage2, elapsed, (elapsed / 1000)));
#endif //_DEBUG

    for (int hn = 0; hn < to_n_heaps; hn++)
    {
        gc_heap* hp = g_heaps[hn];
        generation* gen = hp->generation_of (gen_idx);
        dynamic_data* dd = hp->dynamic_data_of (gen_idx);
        allocator* gen_allocator = generation_allocator (gen);
        gen_allocator->merge_items (hp, to_n_heaps, from_n_heaps);

        size_t free_list_space_decrease = 0;
        if (hn < from_n_heaps)
        {
            // we don't keep track of the size of the items staying on the same heap
            assert (hp->free_list_space_per_heap[hn] == 0);

            for (int to_hn = 0; to_hn < to_n_heaps; to_hn++)
            {
                free_list_space_decrease += hp->free_list_space_per_heap[to_hn];
            }
        }
        dprintf (8888, ("heap %d gen %d %zd total free list space, %zd moved to other heaps",
            hn,
            gen_idx,
            generation_free_list_space (gen),
            free_list_space_decrease));

        assert (free_list_space_decrease <= generation_free_list_space (gen));
        generation_free_list_space (gen) -= free_list_space_decrease;

        // TODO - I'm seeing for gen2 this is free_list_space_decrease can be a bit larger than frag.
        // Need to fix this later.
        if (gen_idx != max_generation)
        {
            assert (free_list_space_decrease <= dd_fragmentation (dd));
        }

        size_t free_list_space_increase = 0;
        for (int from_hn = 0; from_hn < from_n_heaps; from_hn++)
        {
            gc_heap* from_hp = g_heaps[from_hn];

            free_list_space_increase += from_hp->free_list_space_per_heap[hn];
        }
        dprintf (8888, ("heap %d gen %d %zd free list space moved from other heaps", hn, gen_idx, free_list_space_increase));
        generation_free_list_space (gen) += free_list_space_increase;
    }

#ifdef _DEBUG
    // verification to make sure we have the same # of fl items total
    size_t total_fl_items_count = 0;
    size_t total_fl_items_for_oh_count = 0;

    for (int hn = 0; hn < to_n_heaps; hn++)
    {
        gc_heap* hp = g_heaps[hn];
        allocator* gen_allocator = generation_allocator (hp->generation_of (gen_idx));
        size_t fl_items_count = 0;
        size_t fl_items_for_oh_count = 0;
        gen_allocator->count_items (hp, &fl_items_count, &fl_items_for_oh_count);
        total_fl_items_count += fl_items_count;
        total_fl_items_for_oh_count += fl_items_for_oh_count;
    }

    dprintf (8888, ("total %Id fl items, %Id are for other heaps",
        total_fl_items_count, total_fl_items_for_oh_count));

    if (total_fl_items_for_oh_count)
    {
        GCToOSInterface::DebugBreak ();
    }
#endif //_DEBUG
}

#endif //DYNAMIC_HEAP_COUNT

uint8_t* gc_heap::insert_node (uint8_t* new_node, size_t sequence_number,
                   uint8_t* tree, uint8_t* last_node)
{
    dprintf (3, ("IN: %zx(%zx), T: %zx(%zx), L: %zx(%zx) [%zx]",
                 (size_t)new_node, brick_of(new_node),
                 (size_t)tree, brick_of(tree),
                 (size_t)last_node, brick_of(last_node),
                 sequence_number));
    if (power_of_two_p (sequence_number))
    {
        set_node_left_child (new_node, (tree - new_node));
        dprintf (3, ("NT: %zx, LC->%zx", (size_t)new_node, (tree - new_node)));
        tree = new_node;
    }
    else
    {
        if (oddp (sequence_number))
        {
            set_node_right_child (last_node, (new_node - last_node));
            dprintf (3, ("%p RC->%zx", last_node, (new_node - last_node)));
        }
        else
        {
            uint8_t*  earlier_node = tree;
            size_t imax = logcount(sequence_number) - 2;
            for (size_t i = 0; i != imax; i++)
            {
                earlier_node = earlier_node + node_right_child (earlier_node);
            }
            int tmp_offset = node_right_child (earlier_node);
            assert (tmp_offset); // should never be empty
            set_node_left_child (new_node, ((earlier_node + tmp_offset ) - new_node));
            set_node_right_child (earlier_node, (new_node - earlier_node));

            dprintf (3, ("%p LC->%zx, %p RC->%zx",
                new_node, ((earlier_node + tmp_offset ) - new_node),
                earlier_node, (new_node - earlier_node)));
        }
    }
    return tree;
}

size_t gc_heap::update_brick_table (uint8_t* tree, size_t current_brick,
                                    uint8_t* x, uint8_t* plug_end)
{
    dprintf (3, ("tree: %p, current b: %zx, x: %p, plug_end: %p",
        tree, current_brick, x, plug_end));

    if (tree != NULL)
    {
        dprintf (3, ("b- %zx->%zx pointing to tree %p",
            current_brick, (size_t)(tree - brick_address (current_brick)), tree));
        set_brick (current_brick, (tree - brick_address (current_brick)));
    }
    else
    {
        dprintf (3, ("b- %zx->-1", current_brick));
        set_brick (current_brick, -1);
    }
    size_t  b = 1 + current_brick;
    ptrdiff_t  offset = 0;
    size_t last_br = brick_of (plug_end-1);
    current_brick = brick_of (x-1);
    dprintf (3, ("ubt: %zx->%zx]->%zx]", b, last_br, current_brick));
    while (b <= current_brick)
    {
        if (b <= last_br)
        {
            set_brick (b, --offset);
        }
        else
        {
            set_brick (b,-1);
        }
        b++;
    }
    return brick_of (x);
}

#ifndef USE_REGIONS
void gc_heap::plan_generation_start (generation* gen, generation* consing_gen, uint8_t* next_plug_to_allocate)
{
#ifdef HOST_64BIT
    // We should never demote big plugs to gen0.
    if (gen == youngest_generation)
    {
        heap_segment* seg = ephemeral_heap_segment;
        size_t mark_stack_large_bos = mark_stack_bos;
        size_t large_plug_pos = 0;
        while (mark_stack_large_bos < mark_stack_tos)
        {
            if (mark_stack_array[mark_stack_large_bos].len > demotion_plug_len_th)
            {
                while (mark_stack_bos <= mark_stack_large_bos)
                {
                    size_t entry = deque_pinned_plug();
                    size_t len = pinned_len (pinned_plug_of (entry));
                    uint8_t* plug = pinned_plug (pinned_plug_of(entry));
                    if (len > demotion_plug_len_th)
                    {
                        dprintf (2, ("ps(%d): S %p (%zd)(%p)", gen->gen_num, plug, len, (plug+len)));
                    }
                    pinned_len (pinned_plug_of (entry)) = plug - generation_allocation_pointer (consing_gen);
                    assert(mark_stack_array[entry].len == 0 ||
                            mark_stack_array[entry].len >= Align(min_obj_size));
                    generation_allocation_pointer (consing_gen) = plug + len;
                    generation_allocation_limit (consing_gen) = heap_segment_plan_allocated (seg);
                    set_allocator_next_pin (consing_gen);
                }
            }

            mark_stack_large_bos++;
        }
    }
#endif // HOST_64BIT

    generation_plan_allocation_start (gen) =
        allocate_in_condemned_generations (consing_gen, Align (min_obj_size), -1);
    generation_plan_allocation_start_size (gen) = Align (min_obj_size);
    size_t allocation_left = (size_t)(generation_allocation_limit (consing_gen) - generation_allocation_pointer (consing_gen));
    if (next_plug_to_allocate)
    {
        size_t dist_to_next_plug = (size_t)(next_plug_to_allocate - generation_allocation_pointer (consing_gen));
        if (allocation_left > dist_to_next_plug)
        {
            allocation_left = dist_to_next_plug;
        }
    }
    if (allocation_left < Align (min_obj_size))
    {
        generation_plan_allocation_start_size (gen) += allocation_left;
        generation_allocation_pointer (consing_gen) += allocation_left;
    }

    dprintf (2, ("plan alloc gen%d(%p) start at %zx (ptr: %p, limit: %p, next: %p)", gen->gen_num,
        generation_plan_allocation_start (gen),
        generation_plan_allocation_start_size (gen),
        generation_allocation_pointer (consing_gen), generation_allocation_limit (consing_gen),
        next_plug_to_allocate));
}

void gc_heap::realloc_plan_generation_start (generation* gen, generation* consing_gen)
{
    BOOL adjacentp = FALSE;

    generation_plan_allocation_start (gen) =
        allocate_in_expanded_heap (consing_gen, Align(min_obj_size), adjacentp, 0,
#ifdef SHORT_PLUGS
                                   FALSE, NULL,
#endif //SHORT_PLUGS
                                   FALSE, -1 REQD_ALIGN_AND_OFFSET_ARG);

    generation_plan_allocation_start_size (gen) = Align (min_obj_size);
    size_t allocation_left = (size_t)(generation_allocation_limit (consing_gen) - generation_allocation_pointer (consing_gen));
    if ((allocation_left < Align (min_obj_size)) &&
         (generation_allocation_limit (consing_gen)!=heap_segment_plan_allocated (generation_allocation_segment (consing_gen))))
    {
        generation_plan_allocation_start_size (gen) += allocation_left;
        generation_allocation_pointer (consing_gen) += allocation_left;
    }

    dprintf (2, ("plan re-alloc gen%d start at %p (ptr: %p, limit: %p)", gen->gen_num,
        generation_plan_allocation_start (consing_gen),
        generation_allocation_pointer (consing_gen),
        generation_allocation_limit (consing_gen)));
}

void gc_heap::plan_generation_starts (generation*& consing_gen)
{
    //make sure that every generation has a planned allocation start
    int  gen_number = settings.condemned_generation;
    while (gen_number >= 0)
    {
        if (gen_number < max_generation)
        {
            consing_gen = ensure_ephemeral_heap_segment (consing_gen);
        }
        generation* gen = generation_of (gen_number);
        if (0 == generation_plan_allocation_start (gen))
        {
            plan_generation_start (gen, consing_gen, 0);
            assert (generation_plan_allocation_start (gen));
        }
        gen_number--;
    }
    // now we know the planned allocation size
    heap_segment_plan_allocated (ephemeral_heap_segment) =
        generation_allocation_pointer (consing_gen);
}

void gc_heap::advance_pins_for_demotion (generation* gen)
{
    uint8_t* original_youngest_start = generation_allocation_start (youngest_generation);
    heap_segment* seg = ephemeral_heap_segment;

    if ((!(pinned_plug_que_empty_p())))
    {
        size_t gen1_pinned_promoted = generation_pinned_allocation_compact_size (generation_of (max_generation));
        size_t gen1_pins_left = dd_pinned_survived_size (dynamic_data_of (max_generation - 1)) - gen1_pinned_promoted;
        size_t total_space_to_skip = last_gen1_pin_end - generation_allocation_pointer (gen);
        float pin_frag_ratio = (float)gen1_pins_left / (float)total_space_to_skip;
        float pin_surv_ratio = (float)gen1_pins_left / (float)(dd_survived_size (dynamic_data_of (max_generation - 1)));
        if ((pin_frag_ratio > 0.15) && (pin_surv_ratio > 0.30))
        {
            while (!pinned_plug_que_empty_p() &&
                    (pinned_plug (oldest_pin()) < original_youngest_start))
            {
                size_t entry = deque_pinned_plug();
                size_t len = pinned_len (pinned_plug_of (entry));
                uint8_t* plug = pinned_plug (pinned_plug_of(entry));
                pinned_len (pinned_plug_of (entry)) = plug - generation_allocation_pointer (gen);
                assert(mark_stack_array[entry].len == 0 ||
                        mark_stack_array[entry].len >= Align(min_obj_size));
                generation_allocation_pointer (gen) = plug + len;
                generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                set_allocator_next_pin (gen);

                //Add the size of the pinned plug to the right pinned allocations
                //find out which gen this pinned plug came from
                int frgn = object_gennum (plug);
                if ((frgn != (int)max_generation) && settings.promotion)
                {
                    int togn = object_gennum_plan (plug);
                    generation_pinned_allocation_sweep_size ((generation_of (frgn +1))) += len;
                    if (frgn < togn)
                    {
                        generation_pinned_allocation_compact_size (generation_of (togn)) += len;
                    }
                }

                dprintf (2, ("skipping gap %zu, pin %p (%zd)",
                    pinned_len (pinned_plug_of (entry)), plug, len));
            }
        }
        dprintf (2, ("ad_p_d: PL: %zd, SL: %zd, pfr: %d, psr: %d",
            gen1_pins_left, total_space_to_skip, (int)(pin_frag_ratio*100), (int)(pin_surv_ratio*100)));
    }
}

void gc_heap::process_ephemeral_boundaries (uint8_t* x,
                                            int& active_new_gen_number,
                                            int& active_old_gen_number,
                                            generation*& consing_gen,
                                            BOOL& allocate_in_condemned)
{
retry:
    if ((active_old_gen_number > 0) &&
        (x >= generation_allocation_start (generation_of (active_old_gen_number - 1))))
    {
        dprintf (2, ("crossing gen%d, x is %p", active_old_gen_number - 1, x));

        if (!pinned_plug_que_empty_p())
        {
            dprintf (2, ("oldest pin: %p(%zd)",
                pinned_plug (oldest_pin()),
                (x - pinned_plug (oldest_pin()))));
        }

        if (active_old_gen_number <= (settings.promotion ? (max_generation - 1) : max_generation))
        {
            active_new_gen_number--;
        }

        active_old_gen_number--;
        assert ((!settings.promotion) || (active_new_gen_number>0));

        if (active_new_gen_number == (max_generation - 1))
        {
#ifdef FREE_USAGE_STATS
            if (settings.condemned_generation == max_generation)
            {
                // We need to do this before we skip the rest of the pinned plugs.
                generation* gen_2 = generation_of (max_generation);
                generation* gen_1 = generation_of (max_generation - 1);

                size_t total_num_pinned_free_spaces_left = 0;

                // We are about to allocate gen1, check to see how efficient fitting in gen2 pinned free spaces is.
                for (int j = 0; j < NUM_GEN_POWER2; j++)
                {
                    dprintf (1, ("[h%d][#%zd]2^%d: current: %zd, S: 2: %zd, 1: %zd(%zd)",
                        heap_number,
                        settings.gc_index,
                        (j + 10),
                        gen_2->gen_current_pinned_free_spaces[j],
                        gen_2->gen_plugs[j], gen_1->gen_plugs[j],
                        (gen_2->gen_plugs[j] + gen_1->gen_plugs[j])));

                    total_num_pinned_free_spaces_left += gen_2->gen_current_pinned_free_spaces[j];
                }

                float pinned_free_list_efficiency = 0;
                size_t total_pinned_free_space = generation_allocated_in_pinned_free (gen_2) + generation_pinned_free_obj_space (gen_2);
                if (total_pinned_free_space != 0)
                {
                    pinned_free_list_efficiency = (float)(generation_allocated_in_pinned_free (gen_2)) / (float)total_pinned_free_space;
                }

                dprintf (1, ("[h%d] gen2 allocated %zd bytes with %zd bytes pinned free spaces (effi: %d%%), %zd (%zd) left",
                            heap_number,
                            generation_allocated_in_pinned_free (gen_2),
                            total_pinned_free_space,
                            (int)(pinned_free_list_efficiency * 100),
                            generation_pinned_free_obj_space (gen_2),
                            total_num_pinned_free_spaces_left));
            }
#endif //FREE_USAGE_STATS

            //Go past all of the pinned plugs for this generation.
            while (!pinned_plug_que_empty_p() &&
                   (!in_range_for_segment ((pinned_plug (oldest_pin())), ephemeral_heap_segment)))
            {
                size_t  entry = deque_pinned_plug();
                mark*  m = pinned_plug_of (entry);
                uint8_t*  plug = pinned_plug (m);
                size_t  len = pinned_len (m);
                // detect pinned block in different segment (later) than
                // allocation segment, skip those until the oldest pin is in the ephemeral seg.
                // adjust the allocation segment along the way (at the end it will
                // be the ephemeral segment.
                heap_segment* nseg = heap_segment_in_range (generation_allocation_segment (consing_gen));

                _ASSERTE(nseg != NULL);

                while (!((plug >= generation_allocation_pointer (consing_gen))&&
                        (plug < heap_segment_allocated (nseg))))
                {
                    //adjust the end of the segment to be the end of the plug
                    assert (generation_allocation_pointer (consing_gen)>=
                            heap_segment_mem (nseg));
                    assert (generation_allocation_pointer (consing_gen)<=
                            heap_segment_committed (nseg));

                    heap_segment_plan_allocated (nseg) =
                        generation_allocation_pointer (consing_gen);
                    //switch allocation segment
                    nseg = heap_segment_next_rw (nseg);
                    generation_allocation_segment (consing_gen) = nseg;
                    //reset the allocation pointer and limits
                    generation_allocation_pointer (consing_gen) =
                        heap_segment_mem (nseg);
                }
                set_new_pin_info (m, generation_allocation_pointer (consing_gen));
                assert(pinned_len(m) == 0 || pinned_len(m) >= Align(min_obj_size));
                generation_allocation_pointer (consing_gen) = plug + len;
                generation_allocation_limit (consing_gen) =
                    generation_allocation_pointer (consing_gen);
            }
            allocate_in_condemned = TRUE;
            consing_gen = ensure_ephemeral_heap_segment (consing_gen);
        }

        if (active_new_gen_number != max_generation)
        {
            if (active_new_gen_number == (max_generation - 1))
            {
                maxgen_pinned_compact_before_advance = generation_pinned_allocation_compact_size (generation_of (max_generation));
                if (!demote_gen1_p)
                    advance_pins_for_demotion (consing_gen);
            }

            plan_generation_start (generation_of (active_new_gen_number), consing_gen, x);

            dprintf (2, ("process eph: allocated gen%d start at %p",
                active_new_gen_number,
                generation_plan_allocation_start (generation_of (active_new_gen_number))));

            if ((demotion_low == MAX_PTR) && !pinned_plug_que_empty_p())
            {
                uint8_t* pplug = pinned_plug (oldest_pin());
                if (object_gennum (pplug) > 0)
                {
                    demotion_low = pplug;
                    dprintf (3, ("process eph: dlow->%p", demotion_low));
                }
            }

            assert (generation_plan_allocation_start (generation_of (active_new_gen_number)));
        }

        goto retry;
    }
}

#endif //!USE_REGIONS
#ifdef FEATURE_LOH_COMPACTION
inline
BOOL gc_heap::loh_pinned_plug_que_empty_p()
{
    return (loh_pinned_queue_bos == loh_pinned_queue_tos);
}

void gc_heap::loh_set_allocator_next_pin()
{
    if (!(loh_pinned_plug_que_empty_p()))
    {
        mark*  oldest_entry = loh_oldest_pin();
        uint8_t* plug = pinned_plug (oldest_entry);
        generation* gen = large_object_generation;
        if ((plug >= generation_allocation_pointer (gen)) &&
            (plug <  generation_allocation_limit (gen)))
        {
            generation_allocation_limit (gen) = pinned_plug (oldest_entry);
        }
        else
            assert (!((plug < generation_allocation_pointer (gen)) &&
                      (plug >= heap_segment_mem (generation_allocation_segment (gen)))));
    }
}

size_t gc_heap::loh_deque_pinned_plug ()
{
    size_t m = loh_pinned_queue_bos;
    loh_pinned_queue_bos++;
    return m;
}

inline
mark* gc_heap::loh_pinned_plug_of (size_t bos)
{
    return &loh_pinned_queue[bos];
}

inline
mark* gc_heap::loh_oldest_pin()
{
    return loh_pinned_plug_of (loh_pinned_queue_bos);
}

// If we can't grow the queue, then don't compact.
BOOL gc_heap::loh_enque_pinned_plug (uint8_t* plug, size_t len)
{
    assert(len >= Align(min_obj_size, get_alignment_constant (FALSE)));

    if (loh_pinned_queue_length <= loh_pinned_queue_tos)
    {
        if (!grow_mark_stack (loh_pinned_queue, loh_pinned_queue_length, LOH_PIN_QUEUE_LENGTH))
        {
            return FALSE;
        }
    }
    dprintf (3, (" P: %p(%zd)", plug, len));
    mark& m = loh_pinned_queue[loh_pinned_queue_tos];
    m.first = plug;
    m.len = len;
    loh_pinned_queue_tos++;
    loh_set_allocator_next_pin();
    return TRUE;
}

inline
BOOL gc_heap::loh_size_fit_p (size_t size, uint8_t* alloc_pointer, uint8_t* alloc_limit, bool end_p)
{
    dprintf (1235, ("trying to fit %zd(%zd) between %p and %p (%zd)",
        size,
        (2* AlignQword (loh_padding_obj_size) +  size),
        alloc_pointer,
        alloc_limit,
        (alloc_limit - alloc_pointer)));

    // If it's at the end, we don't need to allocate the tail padding
    size_t pad = 1 + (end_p ? 0 : 1);
    pad *= AlignQword (loh_padding_obj_size);

    return ((alloc_pointer + pad + size) <= alloc_limit);
}

uint8_t* gc_heap::loh_allocate_in_condemned (size_t size)
{
    generation* gen = large_object_generation;
    dprintf (1235, ("E: p:%p, l:%p, s: %zd",
        generation_allocation_pointer (gen),
        generation_allocation_limit (gen),
        size));

retry:
    {
        heap_segment* seg = generation_allocation_segment (gen);
        if (!(loh_size_fit_p (size, generation_allocation_pointer (gen), generation_allocation_limit (gen),
                              (generation_allocation_limit (gen) == heap_segment_plan_allocated (seg)))))
        {
            if ((!(loh_pinned_plug_que_empty_p()) &&
                 (generation_allocation_limit (gen) ==
                  pinned_plug (loh_oldest_pin()))))
            {
                mark* m = loh_pinned_plug_of (loh_deque_pinned_plug());
                size_t len = pinned_len (m);
                uint8_t* plug = pinned_plug (m);
                dprintf (1235, ("AIC: %p->%p(%zd)", generation_allocation_pointer (gen), plug, plug - generation_allocation_pointer (gen)));
                pinned_len (m) = plug - generation_allocation_pointer (gen);
                generation_allocation_pointer (gen) = plug + len;

                generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                loh_set_allocator_next_pin();
                dprintf (1235, ("s: p: %p, l: %p (%zd)",
                    generation_allocation_pointer (gen),
                    generation_allocation_limit (gen),
                    (generation_allocation_limit (gen) - generation_allocation_pointer (gen))));

                goto retry;
            }

            if (generation_allocation_limit (gen) != heap_segment_plan_allocated (seg))
            {
                generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                dprintf (1235, ("l->pa(%p)", generation_allocation_limit (gen)));
            }
            else
            {
                if (heap_segment_plan_allocated (seg) != heap_segment_committed (seg))
                {
                    heap_segment_plan_allocated (seg) = heap_segment_committed (seg);
                    generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                    dprintf (1235, ("l->c(%p)", generation_allocation_limit (gen)));
                }
                else
                {
                    if (loh_size_fit_p (size, generation_allocation_pointer (gen), heap_segment_reserved (seg), true) &&
                        (grow_heap_segment (seg, (generation_allocation_pointer (gen) + size + AlignQword (loh_padding_obj_size)))))
                    {
                        dprintf (1235, ("growing seg from %p to %p\n", heap_segment_committed (seg),
                                         (generation_allocation_pointer (gen) + size)));

                        heap_segment_plan_allocated (seg) = heap_segment_committed (seg);
                        generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);

                        dprintf (1235, ("g: p: %p, l: %p (%zd)",
                            generation_allocation_pointer (gen),
                            generation_allocation_limit (gen),
                            (generation_allocation_limit (gen) - generation_allocation_pointer (gen))));
                    }
                    else
                    {
                        heap_segment* next_seg = heap_segment_next (seg);
                        assert (generation_allocation_pointer (gen)>=
                                heap_segment_mem (seg));
                        // Verify that all pinned plugs for this segment are consumed
                        if (!loh_pinned_plug_que_empty_p() &&
                            ((pinned_plug (loh_oldest_pin()) <
                              heap_segment_allocated (seg)) &&
                             (pinned_plug (loh_oldest_pin()) >=
                              generation_allocation_pointer (gen))))
                        {
                            LOG((LF_GC, LL_INFO10, "remaining pinned plug %zx while leaving segment on allocation",
                                         pinned_plug (loh_oldest_pin())));
                            dprintf (1, ("queue empty: %d", loh_pinned_plug_que_empty_p()));
                            FATAL_GC_ERROR();
                        }
                        assert (generation_allocation_pointer (gen)>=
                                heap_segment_mem (seg));
                        assert (generation_allocation_pointer (gen)<=
                                heap_segment_committed (seg));
                        heap_segment_plan_allocated (seg) = generation_allocation_pointer (gen);

                        if (next_seg)
                        {
                            // for LOH do we want to try starting from the first LOH every time though?
                            generation_allocation_segment (gen) = next_seg;
                            generation_allocation_pointer (gen) = heap_segment_mem (next_seg);
                            generation_allocation_limit (gen) = generation_allocation_pointer (gen);

                            dprintf (1235, ("n: p: %p, l: %p (%zd)",
                                generation_allocation_pointer (gen),
                                generation_allocation_limit (gen),
                                (generation_allocation_limit (gen) - generation_allocation_pointer (gen))));
                        }
                        else
                        {
                            dprintf (1, ("We ran out of space compacting, shouldn't happen"));
                            FATAL_GC_ERROR();
                        }
                    }
                }
            }
            loh_set_allocator_next_pin();

            dprintf (1235, ("r: p: %p, l: %p (%zd)",
                generation_allocation_pointer (gen),
                generation_allocation_limit (gen),
                (generation_allocation_limit (gen) - generation_allocation_pointer (gen))));

            goto retry;
        }
    }

    {
        assert (generation_allocation_pointer (gen)>=
                heap_segment_mem (generation_allocation_segment (gen)));
        uint8_t* result = generation_allocation_pointer (gen);
        size_t loh_pad = AlignQword (loh_padding_obj_size);

        generation_allocation_pointer (gen) += size + loh_pad;
        assert (generation_allocation_pointer (gen) <= generation_allocation_limit (gen));

        dprintf (1235, ("p: %p, l: %p (%zd)",
            generation_allocation_pointer (gen),
            generation_allocation_limit (gen),
            (generation_allocation_limit (gen) - generation_allocation_pointer (gen))));

        assert (result + loh_pad);
        return result + loh_pad;
    }
}

BOOL gc_heap::loh_compaction_requested()
{
    // If hard limit is specified GC will automatically decide if LOH needs to be compacted.
    return (loh_compaction_always_p || (loh_compaction_mode != loh_compaction_default));
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

BOOL gc_heap::plan_loh()
{
#ifdef FEATURE_EVENT_TRACE
    uint64_t start_time = 0, end_time;
    if (informational_event_enabled_p)
    {
        memset (loh_compact_info, 0, (sizeof (etw_loh_compact_info) * get_num_heaps()));
        start_time = GetHighPrecisionTimeStamp();
    }
#endif //FEATURE_EVENT_TRACE

    if (!loh_pinned_queue)
    {
        loh_pinned_queue = new (nothrow) (mark [LOH_PIN_QUEUE_LENGTH]);
        if (!loh_pinned_queue)
        {
            dprintf (1, ("Cannot allocate the LOH pinned queue (%zd bytes), no compaction",
                         LOH_PIN_QUEUE_LENGTH * sizeof (mark)));
            return FALSE;
        }

        loh_pinned_queue_length = LOH_PIN_QUEUE_LENGTH;
    }

    loh_pinned_queue_decay = LOH_PIN_DECAY;

    loh_pinned_queue_tos = 0;
    loh_pinned_queue_bos = 0;

    generation* gen        = large_object_generation;
    heap_segment* start_seg = heap_segment_rw (generation_start_segment (gen));
    _ASSERTE(start_seg != NULL);
    heap_segment* seg      = start_seg;
    uint8_t* o             = get_uoh_start_object (seg, gen);

    dprintf (1235, ("before GC LOH size: %zd, free list: %zd, free obj: %zd\n",
        generation_size (loh_generation),
        generation_free_list_space (gen),
        generation_free_obj_space (gen)));

    while (seg)
    {
        heap_segment_plan_allocated (seg) = heap_segment_mem (seg);
        seg = heap_segment_next (seg);
    }

    seg = start_seg;

    // We don't need to ever realloc gen3 start so don't touch it.
    heap_segment_plan_allocated (seg) = o;
    generation_allocation_pointer (gen) = o;
    generation_allocation_limit (gen) = generation_allocation_pointer (gen);
    generation_allocation_segment (gen) = start_seg;

    uint8_t* free_space_start = o;
    uint8_t* free_space_end = o;
    uint8_t* new_address = 0;

    while (1)
    {
        if (o >= heap_segment_allocated (seg))
        {
            seg = heap_segment_next (seg);
            if (seg == 0)
            {
                break;
            }

            o = heap_segment_mem (seg);
        }

        if (marked (o))
        {
            free_space_end = o;
            size_t size = AlignQword (size (o));
            dprintf (1235, ("%p(%zd) M", o, size));

            if (pinned (o))
            {
                // We don't clear the pinned bit yet so we can check in
                // compact phase how big a free object we should allocate
                // in front of the pinned object. We use the reloc address
                // field to store this.
                if (!loh_enque_pinned_plug (o, size))
                {
                    return FALSE;
                }
                new_address = o;
            }
            else
            {
                new_address = loh_allocate_in_condemned (size);
            }

            loh_set_node_relocation_distance (o, (new_address - o));
            dprintf (1235, ("lobj %p-%p -> %p-%p (%zd)", o, (o + size), new_address, (new_address + size), (new_address - o)));

            o = o + size;
            free_space_start = o;
            if (o < heap_segment_allocated (seg))
            {
                assert (!marked (o));
            }
        }
        else
        {
            while (o < heap_segment_allocated (seg) && !marked (o))
            {
                dprintf (1235, ("%p(%zd) F (%d)", o, AlignQword (size (o)), ((method_table (o) == g_gc_pFreeObjectMethodTable) ? 1 : 0)));
                o = o + AlignQword (size (o));
            }
        }
    }

    while (!loh_pinned_plug_que_empty_p())
    {
        mark* m = loh_pinned_plug_of (loh_deque_pinned_plug());
        size_t len = pinned_len (m);
        uint8_t* plug = pinned_plug (m);

        // detect pinned block in different segment (later) than
        // allocation segment
        heap_segment* nseg = heap_segment_rw (generation_allocation_segment (gen));

        while ((plug < generation_allocation_pointer (gen)) ||
               (plug >= heap_segment_allocated (nseg)))
        {
            assert ((plug < heap_segment_mem (nseg)) ||
                    (plug > heap_segment_reserved (nseg)));
            //adjust the end of the segment to be the end of the plug
            assert (generation_allocation_pointer (gen)>=
                    heap_segment_mem (nseg));
            assert (generation_allocation_pointer (gen)<=
                    heap_segment_committed (nseg));

            heap_segment_plan_allocated (nseg) =
                generation_allocation_pointer (gen);
            //switch allocation segment
            nseg = heap_segment_next_rw (nseg);
            generation_allocation_segment (gen) = nseg;
            //reset the allocation pointer and limits
            generation_allocation_pointer (gen) =
                heap_segment_mem (nseg);
        }

        dprintf (1235, ("SP: %p->%p(%zd)", generation_allocation_pointer (gen), plug, plug - generation_allocation_pointer (gen)));
        pinned_len (m) = plug - generation_allocation_pointer (gen);
        generation_allocation_pointer (gen) = plug + len;
    }

    heap_segment_plan_allocated (generation_allocation_segment (gen)) = generation_allocation_pointer (gen);
    generation_allocation_pointer (gen) = 0;
    generation_allocation_limit (gen) = 0;

#ifdef FEATURE_EVENT_TRACE
    if (informational_event_enabled_p)
    {
        end_time = GetHighPrecisionTimeStamp();
        loh_compact_info[heap_number].time_plan = limit_time_to_uint32 (end_time - start_time);
    }
#endif //FEATURE_EVENT_TRACE

    return TRUE;
}

#endif //FEATURE_LOH_COMPACTION

void gc_heap::convert_to_pinned_plug (BOOL& last_npinned_plug_p,
                                      BOOL& last_pinned_plug_p,
                                      BOOL& pinned_plug_p,
                                      size_t ps,
                                      size_t& artificial_pinned_size)
{
    last_npinned_plug_p = FALSE;
    last_pinned_plug_p = TRUE;
    pinned_plug_p = TRUE;
    artificial_pinned_size = ps;
}

// Because we have the artificial pinning, we can't guarantee that pinned and npinned
// plugs are always interleaved.
void gc_heap::store_plug_gap_info (uint8_t* plug_start,
                                   uint8_t* plug_end,
                                   BOOL& last_npinned_plug_p,
                                   BOOL& last_pinned_plug_p,
                                   uint8_t*& last_pinned_plug,
                                   BOOL& pinned_plug_p,
                                   uint8_t* last_object_in_last_plug,
                                   BOOL& merge_with_last_pin_p,
                                   // this is only for verification purpose
                                   size_t last_plug_len)
{
    UNREFERENCED_PARAMETER(last_plug_len);

    if (!last_npinned_plug_p && !last_pinned_plug_p)
    {
        //dprintf (3, ("last full plug end: %zx, full plug start: %zx", plug_end, plug_start));
        dprintf (3, ("Free: %zx", (plug_start - plug_end)));
        assert ((plug_start == plug_end) || ((size_t)(plug_start - plug_end) >= Align (min_obj_size)));
        set_gap_size (plug_start, plug_start - plug_end);
    }

    if (pinned (plug_start))
    {
        BOOL save_pre_plug_info_p = FALSE;

        if (last_npinned_plug_p || last_pinned_plug_p)
        {
            //if (last_plug_len == Align (min_obj_size))
            //{
            //    dprintf (3, ("debugging only - last npinned plug is min, check to see if it's correct"));
            //    GCToOSInterface::DebugBreak();
            //}
            save_pre_plug_info_p = TRUE;
        }

        pinned_plug_p = TRUE;
        last_npinned_plug_p = FALSE;

        if (last_pinned_plug_p)
        {
            dprintf (3, ("last plug %p was also pinned, should merge", last_pinned_plug));
            merge_with_last_pin_p = TRUE;
        }
        else
        {
            last_pinned_plug_p = TRUE;
            last_pinned_plug = plug_start;

            enque_pinned_plug (last_pinned_plug, save_pre_plug_info_p, last_object_in_last_plug);

            if (save_pre_plug_info_p)
            {
#ifdef DOUBLY_LINKED_FL
                if (last_object_in_last_plug == generation_last_free_list_allocated(generation_of(max_generation)))
                {
                    saved_pinned_plug_index = mark_stack_tos;
                }
#endif //DOUBLY_LINKED_FL
                set_gap_size (plug_start, sizeof (gap_reloc_pair));
            }
        }
    }
    else
    {
        if (last_pinned_plug_p)
        {
            //if (Align (last_plug_len) < min_pre_pin_obj_size)
            //{
            //    dprintf (3, ("debugging only - last pinned plug is min, check to see if it's correct"));
            //    GCToOSInterface::DebugBreak();
            //}

            save_post_plug_info (last_pinned_plug, last_object_in_last_plug, plug_start);
            set_gap_size (plug_start, sizeof (gap_reloc_pair));

            verify_pins_with_post_plug_info("after saving post plug info");
        }
        last_npinned_plug_p = TRUE;
        last_pinned_plug_p = FALSE;
    }
}

void gc_heap::record_interesting_data_point (interesting_data_point idp)
{
#ifdef GC_CONFIG_DRIVEN
    (interesting_data_per_gc[idp])++;
#else
    UNREFERENCED_PARAMETER(idp);
#endif //GC_CONFIG_DRIVEN
}

#ifdef USE_REGIONS
void gc_heap::skip_pins_in_alloc_region (generation* consing_gen, int plan_gen_num)
{
    heap_segment* alloc_region = generation_allocation_segment (consing_gen);
    while (!pinned_plug_que_empty_p())
    {
        uint8_t* oldest_plug = pinned_plug (oldest_pin());

        if ((oldest_plug >= generation_allocation_pointer (consing_gen)) &&
            (oldest_plug < heap_segment_allocated (alloc_region)))
        {
            mark* m =       pinned_plug_of (deque_pinned_plug());
            uint8_t* plug = pinned_plug (m);
            size_t len =    pinned_len (m);

            set_new_pin_info (m, generation_allocation_pointer (consing_gen));
            dprintf (REGIONS_LOG, ("pin %p b: %zx->%zx", plug, brick_of (plug),
                (size_t)(brick_table[brick_of (plug)])));

            generation_allocation_pointer (consing_gen) = plug + len;
        }
        else
        {
            // Exit when we detect the first pin that's not on the alloc seg anymore.
            break;
        }
    }

    dprintf (REGIONS_LOG, ("finished with alloc region %p, (%s) plan gen -> %d",
        heap_segment_mem (alloc_region),
        (heap_segment_swept_in_plan (alloc_region) ? "SIP" : "non SIP"),
        (heap_segment_swept_in_plan (alloc_region) ?
            heap_segment_plan_gen_num (alloc_region) : plan_gen_num)));
    set_region_plan_gen_num_sip (alloc_region, plan_gen_num);
    heap_segment_plan_allocated (alloc_region) = generation_allocation_pointer (consing_gen);
}

void gc_heap::decide_on_demotion_pin_surv (heap_segment* region, int* no_pinned_surv_region_count)
{
    int new_gen_num = 0;
    int pinned_surv = heap_segment_pinned_survived (region);

    if (pinned_surv == 0)
    {
        (*no_pinned_surv_region_count)++;
        dprintf (REGIONS_LOG, ("region %Ix will be empty", heap_segment_mem (region)));
    }

    // If this region doesn't have much pinned surv left, we demote it; otherwise the region
    // will be promoted like normal.
    size_t basic_region_size = (size_t)1 << min_segment_size_shr;
    int pinned_ratio = (int)(((double)pinned_surv * 100.0) / (double)basic_region_size);
    dprintf (REGIONS_LOG, ("h%d g%d region %Ix(%Ix) ps: %d (%d) (%s)", heap_number,
        heap_segment_gen_num (region), (size_t)region, heap_segment_mem (region), pinned_surv, pinned_ratio,
        ((pinned_ratio >= demotion_pinned_ratio_th) ? "ND" : "D")));

    if (pinned_ratio >= demotion_pinned_ratio_th)
    {
        if (settings.promotion)
        {
            new_gen_num = get_plan_gen_num (heap_segment_gen_num (region));
        }
    }

    set_region_plan_gen_num (region, new_gen_num);
}

// If the next plan gen number is different, since different generations cannot share the same
// region, we need to get a new alloc region and skip all remaining pins in the alloc region if
// any.
void gc_heap::process_last_np_surv_region (generation* consing_gen,
                                           int current_plan_gen_num,
                                           int next_plan_gen_num)
{
    heap_segment* alloc_region = generation_allocation_segment (consing_gen);
    //assert (in_range_for_segment (generation_allocation_pointer (consing_gen), alloc_region));
    // I'm not using in_range_for_segment here because alloc pointer/limit can be exactly the same
    // as reserved. size_fit_p in allocate_in_condemned_generations can be used to fit the exact
    // size of a plug at the end of the segment which makes alloc pointer/limit both reserved
    // on exit of that method.
    uint8_t* consing_gen_alloc_ptr = generation_allocation_pointer (consing_gen);
    assert ((consing_gen_alloc_ptr >= heap_segment_mem (alloc_region)) &&
            (consing_gen_alloc_ptr <= heap_segment_reserved (alloc_region)));

    dprintf (REGIONS_LOG, ("h%d PLN: (%s) plan gen%d->%d, consing alloc region: %p, ptr: %p (%Id) (consing gen: %d)",
        heap_number, (settings.promotion ? "promotion" : "no promotion"), current_plan_gen_num, next_plan_gen_num,
        heap_segment_mem (alloc_region),
        generation_allocation_pointer (consing_gen),
        (generation_allocation_pointer (consing_gen) - heap_segment_mem (alloc_region)),
        consing_gen->gen_num));

    if (current_plan_gen_num != next_plan_gen_num)
    {
        // If we haven't needed to consume this alloc region at all, we can use it to allocate the new
        // gen.
        if (generation_allocation_pointer (consing_gen) == heap_segment_mem (alloc_region))
        {
            dprintf (REGIONS_LOG, ("h%d alloc region %p unused, using it to plan %d",
                heap_number, heap_segment_mem (alloc_region), next_plan_gen_num));
            return;
        }

        // skip all the pins in this region since we cannot use it to plan the next gen.
        skip_pins_in_alloc_region (consing_gen, current_plan_gen_num);

        heap_segment* next_region = heap_segment_next_non_sip (alloc_region);

        if (!next_region)
        {
            int gen_num = heap_segment_gen_num (alloc_region);
            if (gen_num > 0)
            {
                next_region = generation_start_segment (generation_of (gen_num - 1));
                dprintf (REGIONS_LOG, ("h%d consing switching to next gen%d seg %p",
                    heap_number, heap_segment_gen_num (next_region), heap_segment_mem (next_region)));
            }
            else
            {
                if (settings.promotion)
                {
                    assert (next_plan_gen_num == 0);
                    next_region = get_new_region (0);
                    if (next_region)
                    {
                        dprintf (REGIONS_LOG, ("h%d getting a new region for gen0 plan start seg to %p",
                            heap_number, heap_segment_mem (next_region)));

                        regions_per_gen[0]++;
                        new_gen0_regions_in_plns++;
                    }
                    else
                    {
                        dprintf (REGIONS_LOG, ("h%d couldn't get a region to plan gen0, special sweep on",
                            heap_number));
                        special_sweep_p = true;
                    }
                }
                else
                {
                    assert (!"ran out of regions for non promotion case??");
                }
            }
        }
        else
        {
            dprintf (REGIONS_LOG, ("h%d consing switching to next seg %p in gen%d to alloc in",
                heap_number, heap_segment_mem (next_region), heap_segment_gen_num (next_region)));
        }

        if (next_region)
        {
            init_alloc_info (consing_gen, next_region);

            dprintf (REGIONS_LOG, ("h%d consing(%d) alloc seg: %p(%p, %p), ptr: %p, planning gen%d",
                heap_number, consing_gen->gen_num,
                heap_segment_mem (generation_allocation_segment (consing_gen)),
                heap_segment_allocated (generation_allocation_segment (consing_gen)),
                heap_segment_plan_allocated (generation_allocation_segment (consing_gen)),
                generation_allocation_pointer (consing_gen), next_plan_gen_num));
        }
        else
        {
            assert (special_sweep_p);
        }
    }
}

void gc_heap::process_remaining_regions (int current_plan_gen_num, generation* consing_gen)
{
    assert ((current_plan_gen_num == 0) || (!settings.promotion && (current_plan_gen_num == -1)));

    if (special_sweep_p)
    {
        assert (pinned_plug_que_empty_p());
    }

    dprintf (REGIONS_LOG, ("h%d PRR: (%s) plan %d: consing alloc seg: %p, ptr: %p",
        heap_number, (settings.promotion ? "promotion" : "no promotion"), current_plan_gen_num,
        heap_segment_mem (generation_allocation_segment (consing_gen)),
        generation_allocation_pointer (consing_gen)));

    if (current_plan_gen_num == -1)
    {
        assert (!settings.promotion);
        current_plan_gen_num = 0;

        // For the non promotion case we need to take care of the alloc region we are on right
        // now if there's already planned allocations in it. We cannot let it go through
        // decide_on_demotion_pin_surv which is only concerned with pinned surv.
        heap_segment* alloc_region = generation_allocation_segment (consing_gen);
        if (generation_allocation_pointer (consing_gen) > heap_segment_mem (alloc_region))
        {
            skip_pins_in_alloc_region (consing_gen, current_plan_gen_num);
            heap_segment* next_region = heap_segment_next_non_sip (alloc_region);

            if ((next_region == 0) && (heap_segment_gen_num (alloc_region) > 0))
            {
                next_region = generation_start_segment (generation_of (heap_segment_gen_num (alloc_region) - 1));
            }

            if (next_region)
            {
                init_alloc_info (consing_gen, next_region);
            }
            else
            {
                assert (pinned_plug_que_empty_p ());
                if (!pinned_plug_que_empty_p ())
                {
                    dprintf (REGIONS_LOG, ("we still have a pin at %Ix but no more regions!?", pinned_plug (oldest_pin ())));
                    GCToOSInterface::DebugBreak ();
                }

                // Instead of checking for this condition we just set the alloc region to 0 so it's easier to check
                // later.
                generation_allocation_segment (consing_gen) = 0;
                generation_allocation_pointer (consing_gen) = 0;
                generation_allocation_limit (consing_gen) = 0;
            }
        }
    }

    // What has been planned doesn't change at this point. So at this point we know exactly which generation still doesn't
    // have any regions planned and this method is responsible to attempt to plan at least one region in each of those gens.
    // So we look at each of the remaining regions (that are non SIP, since SIP regions have already been planned) and decide
    // which generation it should be planned in. We used the following rules to decide -
    //
    // + if the pinned surv of a region is >= demotion_pinned_ratio_th (this will be dynamically tuned based on memory load),
    //   it will be promoted to its normal planned generation unconditionally.
    //
    // + if the pinned surv is < demotion_pinned_ratio_th, we will always demote it to gen0. We will record how many regions
    //   have no survival at all - those will be empty and can be used to plan any non gen0 generation if needed.
    //
    //   Note! We could actually promote a region with non zero pinned survivors to whichever generation we'd like (eg, we could
    //   promote a gen0 region to gen2). However it means we'd need to set cards on those objects because we will not have a chance
    //   later. The benefit of doing this is small in general as when we get into this method, it's very rare we don't already
    //   have planned regions in higher generations. So I don't think it's worth the complexicity for now. We may consider it
    //   for the future.
    //
    // + if after we are done walking the remaining regions, we still haven't successfully planned all the needed generations,
    //   we check to see if we have enough in the regions that will be empty (note that we call set_region_plan_gen_num on
    //   these regions which means they are planned in gen0. So we need to make sure at least gen0 has 1 region). If so
    //   thread_final_regions will naturally get one from there so we don't need to call set_region_plan_gen_num to replace the
    //   plan gen num.
    //
    // + if we don't have enough in regions that will be empty, we'll need to ask for new regions and if we can't, we fall back
    //   to the special sweep mode.
    //
    dprintf (REGIONS_LOG, ("h%d regions in g2: %d, g1: %d, g0: %d, before processing remaining regions",
        heap_number, planned_regions_per_gen[2], planned_regions_per_gen[1], planned_regions_per_gen[0]));

    dprintf (REGIONS_LOG, ("h%d g2: surv %Id(p: %Id, %.2f%%), g1: surv %Id(p: %Id, %.2f%%), g0: surv %Id(p: %Id, %.2f%%)",
        heap_number,
        dd_survived_size (dynamic_data_of (2)), dd_pinned_survived_size (dynamic_data_of (2)),
        (dd_survived_size (dynamic_data_of (2)) ? ((double)dd_pinned_survived_size (dynamic_data_of (2)) * 100.0 / (double)dd_survived_size (dynamic_data_of (2))) : 0),
        dd_survived_size (dynamic_data_of (1)), dd_pinned_survived_size (dynamic_data_of (1)),
        (dd_survived_size (dynamic_data_of (2)) ? ((double)dd_pinned_survived_size (dynamic_data_of (1)) * 100.0 / (double)dd_survived_size (dynamic_data_of (1))) : 0),
        dd_survived_size (dynamic_data_of (0)), dd_pinned_survived_size (dynamic_data_of (0)),
        (dd_survived_size (dynamic_data_of (2)) ? ((double)dd_pinned_survived_size (dynamic_data_of (0)) * 100.0 / (double)dd_survived_size (dynamic_data_of (0))) : 0)));

    int to_be_empty_regions = 0;

    while (!pinned_plug_que_empty_p())
    {
        uint8_t* oldest_plug = pinned_plug (oldest_pin());

        // detect pinned block in segments without pins
        heap_segment* nseg = heap_segment_rw (generation_allocation_segment (consing_gen));
        dprintf (3, ("h%d oldest pin: %p, consing alloc %p, ptr %p, limit %p",
            heap_number, oldest_plug, heap_segment_mem (nseg),
            generation_allocation_pointer (consing_gen),
            generation_allocation_limit (consing_gen)));

        while ((oldest_plug < generation_allocation_pointer (consing_gen)) ||
               (oldest_plug >= heap_segment_allocated (nseg)))
        {
            assert ((oldest_plug < heap_segment_mem (nseg)) ||
                    (oldest_plug > heap_segment_reserved (nseg)));
            assert (generation_allocation_pointer (consing_gen)>=
                    heap_segment_mem (nseg));
            assert (generation_allocation_pointer (consing_gen)<=
                    heap_segment_committed (nseg));

            dprintf (3, ("h%d PRR: in loop, seg %p pa %p -> alloc ptr %p, plan gen %d->%d",
                heap_number, heap_segment_mem (nseg),
                heap_segment_plan_allocated (nseg),
                generation_allocation_pointer (consing_gen),
                heap_segment_plan_gen_num (nseg),
                current_plan_gen_num));

            assert (!heap_segment_swept_in_plan (nseg));

            heap_segment_plan_allocated (nseg) = generation_allocation_pointer (consing_gen);
            decide_on_demotion_pin_surv (nseg, &to_be_empty_regions);

            heap_segment* next_seg = heap_segment_next_non_sip (nseg);

            if ((next_seg == 0) && (heap_segment_gen_num (nseg) > 0))
            {
                next_seg = generation_start_segment (generation_of (heap_segment_gen_num (nseg) - 1));
                dprintf (3, ("h%d PRR: switching to next gen%d start %zx",
                    heap_number, heap_segment_gen_num (next_seg), (size_t)next_seg));
            }

            assert (next_seg != 0);
            nseg = next_seg;

            generation_allocation_segment (consing_gen) = nseg;
            generation_allocation_pointer (consing_gen) = heap_segment_mem (nseg);
        }

        mark* m = pinned_plug_of (deque_pinned_plug());
        uint8_t* plug = pinned_plug (m);
        size_t len = pinned_len (m);

        set_new_pin_info (m, generation_allocation_pointer (consing_gen));
        size_t free_size = pinned_len (m);
        update_planned_gen0_free_space (free_size, plug);
        dprintf (2, ("h%d plug %p-%p(%zu), free space before %p-%p(%zu)",
            heap_number, plug, (plug + len), len,
            generation_allocation_pointer (consing_gen), plug, free_size));

        generation_allocation_pointer (consing_gen) = plug + len;
        generation_allocation_limit (consing_gen) =
            generation_allocation_pointer (consing_gen);
    }

    heap_segment* current_region = generation_allocation_segment (consing_gen);

    if (special_sweep_p)
    {
        assert ((current_region == 0) || (heap_segment_next_rw (current_region) == 0));
        return;
    }

    dprintf (REGIONS_LOG, ("after going through the rest of regions - regions in g2: %d, g1: %d, g0: %d, to be empty %d now",
        planned_regions_per_gen[2], planned_regions_per_gen[1], planned_regions_per_gen[0], to_be_empty_regions));

    // We may not have gone through the while loop above so we could get an alloc region that's SIP (which normally would be
    // filtered out by get_next_alloc_seg in allocate_in_condemned_generations. But we are not allocating in condemned anymore
    // so make sure we skip if it's SIP.
    current_region = heap_segment_non_sip (current_region);
    dprintf (REGIONS_LOG, ("now current region is %p", (current_region ? heap_segment_mem (current_region) : 0)));

    if (current_region)
    {
        decide_on_demotion_pin_surv (current_region, &to_be_empty_regions);

        if (!heap_segment_swept_in_plan (current_region))
        {
            heap_segment_plan_allocated (current_region) = generation_allocation_pointer (consing_gen);
            dprintf (REGIONS_LOG, ("h%d setting alloc seg %p plan alloc to %p",
                heap_number, heap_segment_mem (current_region),
                heap_segment_plan_allocated (current_region)));
        }

        dprintf (REGIONS_LOG, ("before going through the rest of empty regions - regions in g2: %d, g1: %d, g0: %d, to be empty %d now",
            planned_regions_per_gen[2], planned_regions_per_gen[1], planned_regions_per_gen[0], to_be_empty_regions));

        heap_segment* region_no_pins = heap_segment_next (current_region);
        int region_no_pins_gen_num = heap_segment_gen_num (current_region);

        do
        {
            region_no_pins = heap_segment_non_sip (region_no_pins);

            if (region_no_pins)
            {
                set_region_plan_gen_num (region_no_pins, current_plan_gen_num);
                to_be_empty_regions++;

                heap_segment_plan_allocated (region_no_pins) = heap_segment_mem (region_no_pins);
                dprintf (REGIONS_LOG, ("h%d setting empty seg %p(no pins) plan gen to 0, plan alloc to %p",
                    heap_number, heap_segment_mem (region_no_pins),
                    heap_segment_plan_allocated (region_no_pins)));

                region_no_pins = heap_segment_next (region_no_pins);
            }

            if (!region_no_pins)
            {
                if (region_no_pins_gen_num > 0)
                {
                    region_no_pins_gen_num--;
                    region_no_pins = generation_start_segment (generation_of (region_no_pins_gen_num));
                }
                else
                    break;
            }
        } while (region_no_pins);
    }

    if (to_be_empty_regions)
    {
        if (planned_regions_per_gen[0] == 0)
        {
            dprintf (REGIONS_LOG, ("we didn't seem to find any gen to plan gen0 yet we have empty regions?!"));
        }
        assert (planned_regions_per_gen[0]);
    }

    int saved_planned_regions_per_gen[max_generation + 1];
    memcpy (saved_planned_regions_per_gen, planned_regions_per_gen, sizeof (saved_planned_regions_per_gen));

    // Because all the "to be empty regions" were planned in gen0, we should substract them if we want to repurpose them.
    assert (saved_planned_regions_per_gen[0] >= to_be_empty_regions);
    saved_planned_regions_per_gen[0] -= to_be_empty_regions;

    int plan_regions_needed = 0;
    for (int gen_idx = settings.condemned_generation; gen_idx >= 0; gen_idx--)
    {
        if (saved_planned_regions_per_gen[gen_idx] == 0)
        {
            dprintf (REGIONS_LOG, ("g%d has 0 planned regions!!!", gen_idx));
            plan_regions_needed++;
        }
    }

    dprintf (REGIONS_LOG, ("we still need %d regions, %d will be empty", plan_regions_needed, to_be_empty_regions));
    if (plan_regions_needed > to_be_empty_regions)
    {
        dprintf (REGIONS_LOG, ("h%d %d regions will be empty but we still need %d regions!!", heap_number, to_be_empty_regions, plan_regions_needed));

        plan_regions_needed -= to_be_empty_regions;

        while (plan_regions_needed && get_new_region (0))
        {
            new_regions_in_prr++;
            plan_regions_needed--;
        }

        if (plan_regions_needed > 0)
        {
            dprintf (REGIONS_LOG, ("h%d %d regions short for having at least one region per gen, special sweep on",
                heap_number));
            special_sweep_p = true;
        }
    }

#ifdef _DEBUG
    {
        dprintf (REGIONS_LOG, ("regions in g2: %d[%d], g1: %d[%d], g0: %d[%d]",
            planned_regions_per_gen[2], regions_per_gen[2],
            planned_regions_per_gen[1], regions_per_gen[1],
            planned_regions_per_gen[0], regions_per_gen[0]));

        int total_regions = 0;
        int total_planned_regions = 0;
        for (int i = max_generation; i >= 0; i--)
        {
            total_regions += regions_per_gen[i];
            total_planned_regions += planned_regions_per_gen[i];
        }

        if (total_regions != total_planned_regions)
        {
            dprintf (REGIONS_LOG, ("planned %d regions, saw %d total",
                total_planned_regions, total_regions));
        }
    }
#endif //_DEBUG
}

void gc_heap::save_current_survived()
{
    if (!survived_per_region) return;

    size_t region_info_to_copy = region_count * sizeof (size_t);
    memcpy (old_card_survived_per_region, survived_per_region, region_info_to_copy);

#ifdef _DEBUG
    for (size_t region_index = 0; region_index < region_count; region_index++)
    {
        if (survived_per_region[region_index] != 0)
        {
            dprintf (REGIONS_LOG, ("region#[%3zd]: %zd", region_index, survived_per_region[region_index]));
        }
    }

    dprintf (REGIONS_LOG, ("global reported %zd", promoted_bytes (heap_number)));
#endif //_DEBUG
}

void gc_heap::update_old_card_survived()
{
    if (!survived_per_region) return;

    for (size_t region_index = 0; region_index < region_count; region_index++)
    {
        old_card_survived_per_region[region_index] = survived_per_region[region_index] -
                                                     old_card_survived_per_region[region_index];
        if (survived_per_region[region_index] != 0)
        {
            dprintf (REGIONS_LOG, ("region#[%3zd]: %zd (card: %zd)",
                region_index, survived_per_region[region_index], old_card_survived_per_region[region_index]));
        }
    }
}

void gc_heap::update_planned_gen0_free_space (size_t free_size, uint8_t* plug)
{
    gen0_pinned_free_space += free_size;
    if (!gen0_large_chunk_found)
    {
        gen0_large_chunk_found = (free_size >= END_SPACE_AFTER_GC_FL);
        if (gen0_large_chunk_found)
        {
            dprintf (3, ("h%d found large pin free space: %zd at %p",
                heap_number, free_size, plug));
        }
    }
}

// REGIONS TODO: I wrote this in the same spirit as ephemeral_gen_fit_p but we really should
// take committed into consideration instead of reserved. We could also avoid going through
// the regions again and do this update in plan phase.
void gc_heap::get_gen0_end_plan_space()
{
    end_gen0_region_space = 0;
    for (int gen_idx = settings.condemned_generation; gen_idx >= 0; gen_idx--)
    {
        generation* gen = generation_of (gen_idx);
        heap_segment* region = heap_segment_rw (generation_start_segment (gen));
        while (region)
        {
            if (heap_segment_plan_gen_num (region) == 0)
            {
                size_t end_plan_space = heap_segment_reserved (region) - heap_segment_plan_allocated (region);
                if (!gen0_large_chunk_found)
                {
                    gen0_large_chunk_found = (end_plan_space >= END_SPACE_AFTER_GC_FL);

                    if (gen0_large_chunk_found)
                    {
                        dprintf (REGIONS_LOG, ("h%d found large end space: %zd in region %p",
                            heap_number, end_plan_space, heap_segment_mem (region)));
                    }
                }

                dprintf (REGIONS_LOG, ("h%d found end space: %zd in region %p, total %zd->%zd",
                    heap_number, end_plan_space, heap_segment_mem (region), end_gen0_region_space,
                    (end_gen0_region_space + end_plan_space)));
                end_gen0_region_space += end_plan_space;
            }

            region = heap_segment_next (region);
        }
    }
}

size_t gc_heap::get_gen0_end_space(memory_type type)
{
    size_t end_space = 0;
    heap_segment* seg = generation_start_segment (generation_of (0));

    while (seg)
    {
        // TODO -
        // This method can also be called concurrently by full GC notification but
        // there's no synchronization between checking for ephemeral_heap_segment and
        // getting alloc_allocated so for now we just always use heap_segment_allocated.
        //uint8_t* allocated = ((seg == ephemeral_heap_segment) ?
        //                      alloc_allocated : heap_segment_allocated (seg));
        uint8_t* allocated = heap_segment_allocated (seg);
        uint8_t* end = (type == memory_type_reserved) ? heap_segment_reserved (seg) : heap_segment_committed (seg);

        end_space += end - allocated;
        dprintf (REGIONS_LOG, ("h%d gen0 seg %p, end %p-%p=%zx, end_space->%zd",
            heap_number, heap_segment_mem (seg),
            end, allocated,
            (end - allocated),
            end_space));

        seg = heap_segment_next (seg);
    }

    return end_space;
}

#endif //USE_REGIONS

inline
uint8_t* gc_heap::find_next_marked (uint8_t* x, uint8_t* end,
                                    BOOL use_mark_list,
                                    uint8_t**& mark_list_next,
                                    uint8_t** mark_list_index)
{
    if (use_mark_list)
    {
        uint8_t* old_x = x;
        while ((mark_list_next < mark_list_index) &&
            (*mark_list_next <= x))
        {
            mark_list_next++;
        }
        x = end;
        if ((mark_list_next < mark_list_index)
#ifdef MULTIPLE_HEAPS
            && (*mark_list_next < end) //for multiple segments
#endif //MULTIPLE_HEAPS
            )
        x = *mark_list_next;
#ifdef BACKGROUND_GC
        if (current_c_gc_state == c_gc_state_marking)
        {
            assert(gc_heap::background_running_p());
            bgc_clear_batch_mark_array_bits (old_x, x);
        }
#endif //BACKGROUND_GC
    }
    else
    {
        uint8_t* xl = x;
#ifdef BACKGROUND_GC
        if (current_c_gc_state == c_gc_state_marking)
        {
            assert (gc_heap::background_running_p());
            while ((xl < end) && !marked (xl))
            {
                dprintf (4, ("-%zx-", (size_t)xl));
                assert ((size (xl) > 0));
                background_object_marked (xl, TRUE);
                xl = xl + Align (size (xl));
                Prefetch (xl);
            }
        }
        else
#endif //BACKGROUND_GC
        {
            while ((xl < end) && !marked (xl))
            {
                dprintf (4, ("-%zx-", (size_t)xl));
                assert ((size (xl) > 0));
                xl = xl + Align (size (xl));
                Prefetch (xl);
            }
        }
        assert (xl <= end);
        x = xl;
    }

    return x;
}

#ifdef FEATURE_EVENT_TRACE
void gc_heap::init_bucket_info()
{
    memset (bucket_info, 0, sizeof (bucket_info));
}

void gc_heap::add_plug_in_condemned_info (generation* gen, size_t plug_size)
{
    uint32_t bucket_index = generation_allocator (gen)->first_suitable_bucket (plug_size);
    (bucket_info[bucket_index].count)++;
    bucket_info[bucket_index].size += plug_size;
}

#endif //FEATURE_EVENT_TRACE

inline void save_allocated(heap_segment* seg)
{
#ifndef MULTIPLE_HEAPS
    if (!heap_segment_saved_allocated(seg))
#endif // !MULTIPLE_HEAPS
    {
        heap_segment_saved_allocated (seg) = heap_segment_allocated (seg);
    }
}

void gc_heap::plan_phase (int condemned_gen_number)
{
    size_t old_gen2_allocated = 0;
    size_t old_gen2_size = 0;

    if (condemned_gen_number == (max_generation - 1))
    {
        old_gen2_allocated = generation_free_list_allocated (generation_of (max_generation));
        old_gen2_size = generation_size (max_generation);
    }

    assert (settings.concurrent == FALSE);

    dprintf (2,(ThreadStressLog::gcStartPlanMsg(), heap_number,
                condemned_gen_number, settings.promotion ? 1 : 0));

    generation*  condemned_gen1 = generation_of (condemned_gen_number);

    BOOL use_mark_list = FALSE;
#ifdef GC_CONFIG_DRIVEN
    dprintf (3, ("total number of marked objects: %zd (%zd)",
                 (mark_list_index - &mark_list[0]), (mark_list_end - &mark_list[0])));

    if (mark_list_index >= (mark_list_end + 1))
    {
        mark_list_index = mark_list_end + 1;
#ifndef MULTIPLE_HEAPS // in Server GC, we check for mark list overflow in sort_mark_list
        mark_list_overflow = true;
#endif
    }
#else //GC_CONFIG_DRIVEN
    dprintf (3, ("mark_list length: %zd",
                 (mark_list_index - &mark_list[0])));
#endif //GC_CONFIG_DRIVEN

    if ((condemned_gen_number < max_generation) &&
        (mark_list_index <= mark_list_end))
    {
#ifndef MULTIPLE_HEAPS
#ifdef USE_VXSORT
        do_vxsort (mark_list, mark_list_index - mark_list, slow, shigh);
#else //USE_VXSORT
        _sort (&mark_list[0], mark_list_index - 1, 0);
#endif //USE_VXSORT

        dprintf (3, ("using mark list at GC #%zd", (size_t)settings.gc_index));
        //verify_qsort_array (&mark_list[0], mark_list_index-1);
#endif //!MULTIPLE_HEAPS
        use_mark_list = TRUE;
        get_gc_data_per_heap()->set_mechanism_bit(gc_mark_list_bit);
    }
    else
    {
        dprintf (3, ("mark_list not used"));
    }

#ifdef FEATURE_BASICFREEZE
    sweep_ro_segments();
#endif //FEATURE_BASICFREEZE

#ifndef MULTIPLE_HEAPS
    int condemned_gen_index = get_stop_generation_index (condemned_gen_number);
    for (; condemned_gen_index <= condemned_gen_number; condemned_gen_index++)
    {
        generation* current_gen = generation_of (condemned_gen_index);
        if (shigh != (uint8_t*)0)
        {
            heap_segment* seg = heap_segment_rw (generation_start_segment (current_gen));
            _ASSERTE(seg != NULL);

            heap_segment* fseg = seg;
            do
            {
                heap_segment_saved_allocated(seg) = 0;
                if (in_range_for_segment (slow, seg))
                {
                    uint8_t* start_unmarked = 0;
#ifdef USE_REGIONS
                    start_unmarked = heap_segment_mem (seg);
#else //USE_REGIONS
                    if (seg == fseg)
                    {
                        uint8_t* o = generation_allocation_start (current_gen);
                        o += get_soh_start_obj_len (o);
                        if (slow > o)
                        {
                            start_unmarked = o;
                            assert ((slow - o) >= (int)Align (min_obj_size));
                        }
                    }
                    else
                    {
                        assert (condemned_gen_number == max_generation);
                        start_unmarked = heap_segment_mem (seg);
                    }
#endif //USE_REGIONS

                    if (start_unmarked)
                    {
                        size_t unmarked_size = slow - start_unmarked;

                        if (unmarked_size > 0)
                        {
#ifdef BACKGROUND_GC
                            if (current_c_gc_state == c_gc_state_marking)
                            {
                                bgc_clear_batch_mark_array_bits (start_unmarked, slow);
                            }
#endif //BACKGROUND_GC
                            make_unused_array (start_unmarked, unmarked_size);
                        }
                    }
                }
                if (in_range_for_segment (shigh, seg))
                {
#ifdef BACKGROUND_GC
                    if (current_c_gc_state == c_gc_state_marking)
                    {
                        bgc_clear_batch_mark_array_bits ((shigh + Align (size (shigh))), heap_segment_allocated (seg));
                    }
#endif //BACKGROUND_GC
                    save_allocated(seg);
                    heap_segment_allocated (seg) = shigh + Align (size (shigh));
                }
                // test if the segment is in the range of [slow, shigh]
                if (!((heap_segment_reserved (seg) >= slow) &&
                    (heap_segment_mem (seg) <= shigh)))
                {
#ifdef BACKGROUND_GC
                    if (current_c_gc_state == c_gc_state_marking)
                    {
#ifdef USE_REGIONS
                        bgc_clear_batch_mark_array_bits (heap_segment_mem (seg), heap_segment_allocated (seg));
#else //USE_REGIONS
                        // This cannot happen with segments as we'd only be on the ephemeral segment if BGC is in
                        // progress and it's guaranteed shigh/slow would be in range of the ephemeral segment.
                        assert (!"cannot happen with segments");
#endif //USE_REGIONS
                    }
#endif //BACKGROUND_GC
                    save_allocated(seg);
                    // shorten it to minimum
                    heap_segment_allocated (seg) =  heap_segment_mem (seg);
                }
                seg = heap_segment_next_rw (seg);
            } while (seg);
        }
        else
        {
            heap_segment* seg = heap_segment_rw (generation_start_segment (current_gen));

            _ASSERTE(seg != NULL);

            heap_segment* sseg = seg;
            do
            {
                heap_segment_saved_allocated(seg) = 0;
                uint8_t* start_unmarked = heap_segment_mem (seg);
#ifndef USE_REGIONS
                // shorten it to minimum
                if (seg == sseg)
                {
                    // no survivors make all generations look empty
                    uint8_t* o = generation_allocation_start (current_gen);
                    o += get_soh_start_obj_len (o);
                    start_unmarked = o;
                }
#endif //!USE_REGIONS

#ifdef BACKGROUND_GC
                if (current_c_gc_state == c_gc_state_marking)
                {
                    bgc_clear_batch_mark_array_bits (start_unmarked, heap_segment_allocated (seg));
                }
#endif //BACKGROUND_GC
                save_allocated(seg);
                heap_segment_allocated (seg) = start_unmarked;

                seg = heap_segment_next_rw (seg);
            } while (seg);
        }
    }
#endif //MULTIPLE_HEAPS

    heap_segment*  seg1 = heap_segment_rw (generation_start_segment (condemned_gen1));

    _ASSERTE(seg1 != NULL);

    uint8_t*  end = heap_segment_allocated (seg1);
    uint8_t*  first_condemned_address = get_soh_start_object (seg1, condemned_gen1);
    uint8_t*  x = first_condemned_address;

#ifdef USE_REGIONS
    memset (regions_per_gen, 0, sizeof (regions_per_gen));
    memset (planned_regions_per_gen, 0, sizeof (planned_regions_per_gen));
    memset (sip_maxgen_regions_per_gen, 0, sizeof (sip_maxgen_regions_per_gen));
    memset (reserved_free_regions_sip, 0, sizeof (reserved_free_regions_sip));
    int pinned_survived_region = 0;
    uint8_t** mark_list_index = nullptr;
    uint8_t** mark_list_next = nullptr;
    if (use_mark_list)
        mark_list_next = get_region_mark_list (use_mark_list, x, end, &mark_list_index);
#else // USE_REGIONS
    assert (!marked (x));
    uint8_t** mark_list_next = &mark_list[0];
#endif //USE_REGIONS
    uint8_t*  plug_end = x;
    uint8_t*  tree = 0;
    size_t  sequence_number = 0;
    uint8_t*  last_node = 0;
    size_t  current_brick = brick_of (x);
    BOOL  allocate_in_condemned = ((condemned_gen_number == max_generation)||
                                   (settings.promotion == FALSE));
    int  active_old_gen_number = condemned_gen_number;
    int  active_new_gen_number = (allocate_in_condemned ? condemned_gen_number:
                                  (1 + condemned_gen_number));

    generation*  older_gen = 0;
    generation* consing_gen = condemned_gen1;
    alloc_list  r_free_list [MAX_SOH_BUCKET_COUNT];

    size_t r_free_list_space = 0;
    size_t r_free_obj_space = 0;
    size_t r_older_gen_free_list_allocated = 0;
    size_t r_older_gen_condemned_allocated = 0;
    size_t r_older_gen_end_seg_allocated = 0;
    uint8_t*  r_allocation_pointer = 0;
    uint8_t*  r_allocation_limit = 0;
    uint8_t* r_allocation_start_region = 0;
    heap_segment*  r_allocation_segment = 0;
#ifdef FREE_USAGE_STATS
    size_t r_older_gen_free_space[NUM_GEN_POWER2];
#endif //FREE_USAGE_STATS

    if ((condemned_gen_number < max_generation))
    {
        older_gen = generation_of (min ((int)max_generation, 1 + condemned_gen_number));
        generation_allocator (older_gen)->copy_to_alloc_list (r_free_list);

        r_free_list_space = generation_free_list_space (older_gen);
        r_free_obj_space = generation_free_obj_space (older_gen);
#ifdef FREE_USAGE_STATS
        memcpy (r_older_gen_free_space, older_gen->gen_free_spaces, sizeof (r_older_gen_free_space));
#endif //FREE_USAGE_STATS
        generation_allocate_end_seg_p (older_gen) = FALSE;

#ifdef DOUBLY_LINKED_FL
        if (older_gen->gen_num == max_generation)
        {
            generation_set_bgc_mark_bit_p (older_gen) = FALSE;
            generation_last_free_list_allocated (older_gen) = 0;
        }
#endif //DOUBLY_LINKED_FL

        r_older_gen_free_list_allocated = generation_free_list_allocated (older_gen);
        r_older_gen_condemned_allocated = generation_condemned_allocated (older_gen);
        r_older_gen_end_seg_allocated = generation_end_seg_allocated (older_gen);
        r_allocation_limit = generation_allocation_limit (older_gen);
        r_allocation_pointer = generation_allocation_pointer (older_gen);
        r_allocation_start_region = generation_allocation_context_start_region (older_gen);
        r_allocation_segment = generation_allocation_segment (older_gen);

#ifdef USE_REGIONS
        if (older_gen->gen_num == max_generation)
        {
            check_seg_gen_num (r_allocation_segment);
        }
#endif //USE_REGIONS

        heap_segment* start_seg = heap_segment_rw (generation_start_segment (older_gen));

        _ASSERTE(start_seg != NULL);

#ifdef USE_REGIONS
        heap_segment* skip_seg = 0;

        assert (generation_allocation_pointer (older_gen) == 0);
        assert (generation_allocation_limit (older_gen) == 0);
#else //USE_REGIONS
        heap_segment* skip_seg = ephemeral_heap_segment;
        if (start_seg != ephemeral_heap_segment)
        {
            assert (condemned_gen_number == (max_generation - 1));
        }
#endif //USE_REGIONS
        if (start_seg != skip_seg)
        {
            while (start_seg && (start_seg != skip_seg))
            {
                assert (heap_segment_allocated (start_seg) >=
                        heap_segment_mem (start_seg));
                assert (heap_segment_allocated (start_seg) <=
                        heap_segment_reserved (start_seg));
                heap_segment_plan_allocated (start_seg) =
                    heap_segment_allocated (start_seg);
                start_seg = heap_segment_next_rw (start_seg);
            }
        }
    }

    //reset all of the segment's plan_allocated
    {
        int condemned_gen_index1 = get_stop_generation_index (condemned_gen_number);
        for (; condemned_gen_index1 <= condemned_gen_number; condemned_gen_index1++)
        {
            generation* current_gen = generation_of (condemned_gen_index1);
            heap_segment*  seg2 = heap_segment_rw (generation_start_segment (current_gen));
            _ASSERTE(seg2 != NULL);

            while (seg2)
            {
#ifdef USE_REGIONS
                regions_per_gen[condemned_gen_index1]++;
                dprintf (REGIONS_LOG, ("h%d PS: gen%d %p-%p (%d, surv: %d), %d regions",
                    heap_number, condemned_gen_index1,
                    heap_segment_mem (seg2), heap_segment_allocated (seg2),
                    (heap_segment_allocated (seg2) - heap_segment_mem (seg2)),
                    (int)heap_segment_survived (seg2), regions_per_gen[condemned_gen_index1]));
#endif //USE_REGIONS

                heap_segment_plan_allocated (seg2) =
                    heap_segment_mem (seg2);
                seg2 = heap_segment_next_rw (seg2);
            }
        }
    }

    int  condemned_gn = condemned_gen_number;

    int bottom_gen = 0;
    init_free_and_plug();

    while (condemned_gn >= bottom_gen)
    {
        generation*  condemned_gen2 = generation_of (condemned_gn);
        generation_allocator (condemned_gen2)->clear();
        generation_free_list_space (condemned_gen2) = 0;
        generation_free_obj_space (condemned_gen2) = 0;
        generation_allocation_size (condemned_gen2) = 0;
        generation_condemned_allocated (condemned_gen2) = 0;
        generation_sweep_allocated (condemned_gen2) = 0;
        generation_free_list_allocated(condemned_gen2) = 0;
        generation_end_seg_allocated (condemned_gen2) = 0;
        generation_pinned_allocation_sweep_size (condemned_gen2) = 0;
        generation_pinned_allocation_compact_size (condemned_gen2) = 0;
#ifdef FREE_USAGE_STATS
        generation_pinned_free_obj_space (condemned_gen2) = 0;
        generation_allocated_in_pinned_free (condemned_gen2) = 0;
        generation_allocated_since_last_pin (condemned_gen2) = 0;
#endif //FREE_USAGE_STATS

#ifndef USE_REGIONS
        generation_plan_allocation_start (condemned_gen2) = 0;
#endif //!USE_REGIONS
        generation_allocation_segment (condemned_gen2) =
            heap_segment_rw (generation_start_segment (condemned_gen2));

        _ASSERTE(generation_allocation_segment(condemned_gen2) != NULL);

#ifdef USE_REGIONS
        generation_allocation_pointer (condemned_gen2) =
            heap_segment_mem (generation_allocation_segment (condemned_gen2));
#else //USE_REGIONS
        if (generation_start_segment (condemned_gen2) != ephemeral_heap_segment)
        {
            generation_allocation_pointer (condemned_gen2) =
                heap_segment_mem (generation_allocation_segment (condemned_gen2));
        }
        else
        {
            generation_allocation_pointer (condemned_gen2) = generation_allocation_start (condemned_gen2);
        }
#endif //USE_REGIONS
        generation_allocation_limit (condemned_gen2) = generation_allocation_pointer (condemned_gen2);
        generation_allocation_context_start_region (condemned_gen2) = generation_allocation_pointer (condemned_gen2);

        condemned_gn--;
    }

    BOOL allocate_first_generation_start = FALSE;

    if (allocate_in_condemned)
    {
        allocate_first_generation_start = TRUE;
    }

    dprintf(3,( " From %zx to %zx", (size_t)x, (size_t)end));

#ifdef USE_REGIONS
    if (should_sweep_in_plan (seg1))
    {
        sweep_region_in_plan (seg1, use_mark_list, mark_list_next, mark_list_index);
        x = end;
    }
#else
    demotion_low = MAX_PTR;
    demotion_high = heap_segment_allocated (ephemeral_heap_segment);
    // If we are doing a gen1 only because of cards, it means we should not demote any pinned plugs
    // from gen1. They should get promoted to gen2.
    demote_gen1_p = !(settings.promotion &&
        (settings.condemned_generation == (max_generation - 1)) &&
        gen_to_condemn_reasons.is_only_condition(gen_low_card_p));

    total_ephemeral_size = 0;
#endif //!USE_REGIONS

    print_free_and_plug ("BP");

#ifndef USE_REGIONS
    for (int gen_idx = 0; gen_idx <= max_generation; gen_idx++)
    {
        generation* temp_gen = generation_of (gen_idx);

        dprintf (2, ("gen%d start %p, plan start %p",
            gen_idx,
            generation_allocation_start (temp_gen),
            generation_plan_allocation_start (temp_gen)));
    }
#endif //!USE_REGIONS

#ifdef FEATURE_EVENT_TRACE
    // When verbose level is enabled we want to record some info about gen2 FL usage during gen1 GCs.
    // We record the bucket info for the largest FL items and plugs that we have to allocate in condemned.
    bool record_fl_info_p = (EVENT_ENABLED (GCFitBucketInfo) && (condemned_gen_number == (max_generation - 1)));
    size_t recorded_fl_info_size = 0;
    if (record_fl_info_p)
        init_bucket_info();
    bool fire_pinned_plug_events_p = EVENT_ENABLED(PinPlugAtGCTime);
#endif //FEATURE_EVENT_TRACE

    size_t last_plug_len = 0;

#ifdef DOUBLY_LINKED_FL
    gen2_removed_no_undo = 0;
    saved_pinned_plug_index = INVALID_SAVED_PINNED_PLUG_INDEX;
#endif //DOUBLY_LINKED_FL

    while (1)
    {
        if (x >= end)
        {
            if (!use_mark_list)
            {
                assert (x == end);
            }

#ifdef USE_REGIONS
            if (heap_segment_swept_in_plan (seg1))
            {
                assert (heap_segment_gen_num (seg1) == active_old_gen_number);
                dynamic_data* dd_active_old = dynamic_data_of (active_old_gen_number);
                dd_survived_size (dd_active_old) += heap_segment_survived (seg1);
                dprintf (REGIONS_LOG, ("region %p-%p SIP",
                    heap_segment_mem (seg1), heap_segment_allocated (seg1)));
            }
            else
#endif //USE_REGIONS
            {
                assert (heap_segment_allocated (seg1) == end);
                save_allocated(seg1);
                heap_segment_allocated (seg1) = plug_end;
                current_brick = update_brick_table (tree, current_brick, x, plug_end);
                dprintf (REGIONS_LOG, ("region %p-%p(%p) non SIP",
                    heap_segment_mem (seg1), heap_segment_allocated (seg1),
                    heap_segment_plan_allocated (seg1)));
                dprintf (3, ("end of seg: new tree, sequence# 0"));
                sequence_number = 0;
                tree = 0;
            }

#ifdef USE_REGIONS
            heap_segment_pinned_survived (seg1) = pinned_survived_region;
            dprintf (REGIONS_LOG, ("h%d setting seg %p pin surv: %d",
                heap_number, heap_segment_mem (seg1), pinned_survived_region));
            pinned_survived_region = 0;
            if (heap_segment_mem (seg1) == heap_segment_allocated (seg1))
            {
                num_regions_freed_in_sweep++;
            }
#endif //USE_REGIONS

            if (heap_segment_next_rw (seg1))
            {
                seg1 = heap_segment_next_rw (seg1);
                end = heap_segment_allocated (seg1);
                plug_end = x = heap_segment_mem (seg1);
                current_brick = brick_of (x);
#ifdef USE_REGIONS
                if (use_mark_list)
                    mark_list_next = get_region_mark_list (use_mark_list, x, end, &mark_list_index);

                if (should_sweep_in_plan (seg1))
                {
                    sweep_region_in_plan (seg1, use_mark_list, mark_list_next, mark_list_index);
                    x = end;
                }
#endif //USE_REGIONS
                dprintf(3,( " From %zx to %zx", (size_t)x, (size_t)end));
                continue;
            }
            else
            {
#ifdef USE_REGIONS
                // We have a few task here when we ran out of regions to go through for the
                // active_old_gen_number -
                //
                // + decide on which pins to skip
                // + set the planned gen for the regions we process here
                // + set the consing gen's alloc ptr/limit
                // + decide on the new active_old_gen_number (which is just the current one - 1)
                // + decide on the new active_new_gen_number (which depends on settings.promotion)
                //
                // Important differences between process_last_np_surv_region and process_ephemeral_boundaries
                // - it's guaranteed we would ask to allocate gen1 start for promotion and gen0
                //   start for non promotion case.
                // - consing_gen is never changed. In fact we really don't need consing_gen, we just
                //   need the alloc ptr/limit pair and the alloc seg.
                //   TODO : should just get rid of consing_gen.
                // These make things more regular and easier to keep track of.
                //
                // Also I'm doing everything here instead of having to have separate code to go
                // through the left over pins after the main loop in plan phase.
                int saved_active_new_gen_number = active_new_gen_number;
                BOOL saved_allocate_in_condemned = allocate_in_condemned;

                dprintf (REGIONS_LOG, ("h%d finished planning gen%d regions into gen%d, alloc_in_condemned: %d",
                    heap_number, active_old_gen_number, active_new_gen_number, allocate_in_condemned));

                if (active_old_gen_number <= (settings.promotion ? (max_generation - 1) : max_generation))
                {
                    dprintf (REGIONS_LOG, ("h%d active old: %d, new: %d->%d, allocate_in_condemned %d->1",
                        heap_number, active_old_gen_number,
                        active_new_gen_number, (active_new_gen_number - 1),
                        allocate_in_condemned));
                    active_new_gen_number--;
                    allocate_in_condemned = TRUE;
                }

                if (active_new_gen_number >= 0)
                {
                    process_last_np_surv_region (consing_gen, saved_active_new_gen_number, active_new_gen_number);
                }

                if (active_old_gen_number == 0)
                {
                    // We need to process the pins on the remaining regions if any.
                    process_remaining_regions (active_new_gen_number, consing_gen);
                    break;
                }
                else
                {
                    active_old_gen_number--;

                    seg1 = heap_segment_rw (generation_start_segment (generation_of (active_old_gen_number)));
                    end = heap_segment_allocated (seg1);
                    plug_end = x = heap_segment_mem (seg1);
                    current_brick = brick_of (x);

                    if (use_mark_list)
                        mark_list_next = get_region_mark_list (use_mark_list, x, end, &mark_list_index);

                    if (should_sweep_in_plan (seg1))
                    {
                        sweep_region_in_plan (seg1, use_mark_list, mark_list_next, mark_list_index);
                        x = end;
                    }

                    dprintf (REGIONS_LOG,("h%d switching to gen%d start region %p, %p-%p",
                        heap_number, active_old_gen_number, heap_segment_mem (seg1), x, end));
                    continue;
                }
#else //USE_REGIONS
                break;
#endif //USE_REGIONS
            }
        }

        BOOL last_npinned_plug_p = FALSE;
        BOOL last_pinned_plug_p = FALSE;

        // last_pinned_plug is the beginning of the last pinned plug. If we merge a plug into a pinned
        // plug we do not change the value of last_pinned_plug. This happens with artificially pinned plugs -
        // it can be merged with a previous pinned plug and a pinned plug after it can be merged with it.
        uint8_t* last_pinned_plug = 0;
        size_t num_pinned_plugs_in_plug = 0;

        uint8_t* last_object_in_plug = 0;

        while ((x < end) && marked (x))
        {
            uint8_t*  plug_start = x;
            uint8_t*  saved_plug_end = plug_end;
            BOOL   pinned_plug_p = FALSE;
            BOOL   npin_before_pin_p = FALSE;
            BOOL   saved_last_npinned_plug_p = last_npinned_plug_p;
            uint8_t*  saved_last_object_in_plug = last_object_in_plug;
            BOOL   merge_with_last_pin_p = FALSE;

            size_t added_pinning_size = 0;
            size_t artificial_pinned_size = 0;

            store_plug_gap_info (plug_start, plug_end, last_npinned_plug_p, last_pinned_plug_p,
                                 last_pinned_plug, pinned_plug_p, last_object_in_plug,
                                 merge_with_last_pin_p, last_plug_len);

#ifdef FEATURE_STRUCTALIGN
            int requiredAlignment = ((CObjectHeader*)plug_start)->GetRequiredAlignment();
            size_t alignmentOffset = OBJECT_ALIGNMENT_OFFSET;
#endif // FEATURE_STRUCTALIGN

            {
                uint8_t* xl = x;
                while ((xl < end) && marked (xl) && (pinned (xl) == pinned_plug_p))
                {
                    assert (xl < end);
                    if (pinned(xl))
                    {
                        clear_pinned (xl);
                    }
#ifdef FEATURE_STRUCTALIGN
                    else
                    {
                        int obj_requiredAlignment = ((CObjectHeader*)xl)->GetRequiredAlignment();
                        if (obj_requiredAlignment > requiredAlignment)
                        {
                            requiredAlignment = obj_requiredAlignment;
                            alignmentOffset = xl - plug_start + OBJECT_ALIGNMENT_OFFSET;
                        }
                    }
#endif // FEATURE_STRUCTALIGN

                    clear_marked (xl);

                    dprintf(4, ("+%zx+", (size_t)xl));
                    assert ((size (xl) > 0));
                    assert ((size (xl) <= loh_size_threshold));

                    last_object_in_plug = xl;

                    xl = xl + Align (size (xl));
                    Prefetch (xl);
                }

                BOOL next_object_marked_p = ((xl < end) && marked (xl));

                if (pinned_plug_p)
                {
                    // If it is pinned we need to extend to the next marked object as we can't use part of
                    // a pinned object to make the artificial gap (unless the last 3 ptr sized words are all
                    // references but for now I am just using the next non pinned object for that).
                    if (next_object_marked_p)
                    {
                        clear_marked (xl);
                        last_object_in_plug = xl;
                        size_t extra_size = Align (size (xl));
                        xl = xl + extra_size;
                        added_pinning_size = extra_size;
                    }
                }
                else
                {
                    if (next_object_marked_p)
                        npin_before_pin_p = TRUE;
                }

                assert (xl <= end);
                x = xl;
            }
            dprintf (3, ( "%zx[", (size_t)plug_start));
            plug_end = x;
            size_t ps = plug_end - plug_start;
            last_plug_len = ps;
            dprintf (3, ( "%zx[(%zx)", (size_t)x, ps));
            uint8_t*  new_address = 0;

            if (!pinned_plug_p)
            {
                if (allocate_in_condemned &&
                    (settings.condemned_generation == max_generation) &&
                    (ps > OS_PAGE_SIZE))
                {
                    ptrdiff_t reloc = plug_start - generation_allocation_pointer (consing_gen);
                    //reloc should >=0 except when we relocate
                    //across segments and the dest seg is higher then the src

                    if ((ps > (8*OS_PAGE_SIZE)) &&
                        (reloc > 0) &&
                        ((size_t)reloc < (ps/16)))
                    {
                        dprintf (3, ("Pinning %zx; reloc would have been: %zx",
                                     (size_t)plug_start, reloc));
                        // The last plug couldn't have been a npinned plug or it would have
                        // included this plug.
                        assert (!saved_last_npinned_plug_p);

                        if (last_pinned_plug)
                        {
                            dprintf (3, ("artificially pinned plug merged with last pinned plug"));
                            merge_with_last_pin_p = TRUE;
                        }
                        else
                        {
                            enque_pinned_plug (plug_start, FALSE, 0);
                            last_pinned_plug = plug_start;
                        }

                        convert_to_pinned_plug (last_npinned_plug_p, last_pinned_plug_p, pinned_plug_p,
                                                ps, artificial_pinned_size);
                    }
                }
            }

#ifndef USE_REGIONS
            if (allocate_first_generation_start)
            {
                allocate_first_generation_start = FALSE;
                plan_generation_start (condemned_gen1, consing_gen, plug_start);
                assert (generation_plan_allocation_start (condemned_gen1));
            }

            if (seg1 == ephemeral_heap_segment)
            {
                process_ephemeral_boundaries (plug_start, active_new_gen_number,
                                              active_old_gen_number,
                                              consing_gen,
                                              allocate_in_condemned);
            }
#endif //!USE_REGIONS

            dprintf (3, ("adding %zd to gen%d surv", ps, active_old_gen_number));

            dynamic_data* dd_active_old = dynamic_data_of (active_old_gen_number);
            dd_survived_size (dd_active_old) += ps;

            BOOL convert_to_pinned_p = FALSE;
            BOOL allocated_in_older_p = FALSE;

            if (!pinned_plug_p)
            {
#if defined (RESPECT_LARGE_ALIGNMENT) || defined (FEATURE_STRUCTALIGN)
                dd_num_npinned_plugs (dd_active_old)++;
#endif //RESPECT_LARGE_ALIGNMENT || FEATURE_STRUCTALIGN

                add_gen_plug (active_old_gen_number, ps);

                if (allocate_in_condemned)
                {
                    verify_pins_with_post_plug_info("before aic");

                    new_address =
                        allocate_in_condemned_generations (consing_gen,
                                                           ps,
                                                           active_old_gen_number,
#ifdef SHORT_PLUGS
                                                           &convert_to_pinned_p,
                                                           (npin_before_pin_p ? plug_end : 0),
                                                           seg1,
#endif //SHORT_PLUGS
                                                           plug_start REQD_ALIGN_AND_OFFSET_ARG);
                    verify_pins_with_post_plug_info("after aic");
                }
                else
                {
                    new_address = allocate_in_older_generation (older_gen, ps, active_old_gen_number, plug_start REQD_ALIGN_AND_OFFSET_ARG);

                    if (new_address != 0)
                    {
                        allocated_in_older_p = TRUE;
                        if (settings.condemned_generation == (max_generation - 1))
                        {
                            dprintf (3, (" NA: %p-%p -> %zx, %zx (%zx)",
                                plug_start, plug_end,
                                (size_t)new_address, (size_t)new_address + (plug_end - plug_start),
                                (size_t)(plug_end - plug_start)));
                        }
                    }
                    else
                    {
                        if (generation_allocator(older_gen)->discard_if_no_fit_p())
                        {
                            allocate_in_condemned = TRUE;
                        }

                        new_address = allocate_in_condemned_generations (consing_gen, ps, active_old_gen_number,
#ifdef SHORT_PLUGS
                                                                         &convert_to_pinned_p,
                                                                         (npin_before_pin_p ? plug_end : 0),
                                                                         seg1,
#endif //SHORT_PLUGS
                                                                         plug_start REQD_ALIGN_AND_OFFSET_ARG);
                    }
                }

#ifdef FEATURE_EVENT_TRACE
                if (record_fl_info_p && !allocated_in_older_p)
                {
                    add_plug_in_condemned_info (older_gen, ps);
                    recorded_fl_info_size += ps;
                }
#endif //FEATURE_EVENT_TRACE

                if (convert_to_pinned_p)
                {
                    assert (last_npinned_plug_p != FALSE);
                    assert (last_pinned_plug_p == FALSE);
                    convert_to_pinned_plug (last_npinned_plug_p, last_pinned_plug_p, pinned_plug_p,
                                            ps, artificial_pinned_size);
                    enque_pinned_plug (plug_start, FALSE, 0);
                    last_pinned_plug = plug_start;
                }
                else
                {
                    if (!new_address)
                    {
                        //verify that we are at then end of the ephemeral segment
                        assert (generation_allocation_segment (consing_gen) ==
                                ephemeral_heap_segment);
                        //verify that we are near the end
                        assert ((generation_allocation_pointer (consing_gen) + Align (ps)) <
                                heap_segment_allocated (ephemeral_heap_segment));
                        assert ((generation_allocation_pointer (consing_gen) + Align (ps)) >
                                (heap_segment_allocated (ephemeral_heap_segment) + Align (min_obj_size)));
                    }
                    else
                    {
                        dprintf (3, (ThreadStressLog::gcPlanPlugMsg(),
                            (size_t)(node_gap_size (plug_start)),
                            plug_start, plug_end, (size_t)new_address, (size_t)(plug_start - new_address),
                                (size_t)new_address + ps, ps,
                                (is_plug_padded (plug_start) ? 1 : 0), x,
                                (allocated_in_older_p ? "O" : "C")));

#ifdef SHORT_PLUGS
                        if (is_plug_padded (plug_start))
                        {
                            dprintf (3, ("%p was padded", plug_start));
                            dd_padding_size (dd_active_old) += Align (min_obj_size);
                        }
#endif //SHORT_PLUGS
                    }
                }
            }

            if (pinned_plug_p)
            {
#ifdef FEATURE_EVENT_TRACE
                if (fire_pinned_plug_events_p)
                {
                    FIRE_EVENT(PinPlugAtGCTime, plug_start, plug_end,
                               (merge_with_last_pin_p ? 0 : (uint8_t*)node_gap_size (plug_start)));
                }
#endif //FEATURE_EVENT_TRACE

                if (merge_with_last_pin_p)
                {
                    merge_with_last_pinned_plug (last_pinned_plug, ps);
                }
                else
                {
                    assert (last_pinned_plug == plug_start);
                    set_pinned_info (plug_start, ps, consing_gen);
                }

                new_address = plug_start;

                dprintf (3, (ThreadStressLog::gcPlanPinnedPlugMsg(),
                            (size_t)(node_gap_size (plug_start)), (size_t)plug_start,
                            (size_t)plug_end, ps,
                            (merge_with_last_pin_p ? 1 : 0)));

                dprintf (3, ("adding %zd to gen%d pinned surv", plug_end - plug_start, active_old_gen_number));

                size_t pinned_plug_size = plug_end - plug_start;
#ifdef USE_REGIONS
                pinned_survived_region += (int)pinned_plug_size;
#endif //USE_REGIONS

                dd_pinned_survived_size (dd_active_old) += pinned_plug_size;
                dd_added_pinned_size (dd_active_old) += added_pinning_size;
                dd_artificial_pinned_survived_size (dd_active_old) += artificial_pinned_size;

#ifndef USE_REGIONS
                if (!demote_gen1_p && (active_old_gen_number == (max_generation - 1)))
                {
                    last_gen1_pin_end = plug_end;
                }
#endif //!USE_REGIONS
            }

#ifdef _DEBUG
            // detect forward allocation in the same segment
            assert (!((new_address > plug_start) &&
                (new_address < heap_segment_reserved (seg1))));
#endif //_DEBUG

            if (!merge_with_last_pin_p)
            {
                if (current_brick != brick_of (plug_start))
                {
                    current_brick = update_brick_table (tree, current_brick, plug_start, saved_plug_end);
                    sequence_number = 0;
                    tree = 0;
                }

                set_node_relocation_distance (plug_start, (new_address - plug_start));
                if (last_node && (node_relocation_distance (last_node) ==
                                  (node_relocation_distance (plug_start) +
                                   (ptrdiff_t)node_gap_size (plug_start))))
                {
                    //dprintf(3,( " Lb"));
                    dprintf (3, ("%p Lb", plug_start));
                    set_node_left (plug_start);
                }
                if (0 == sequence_number)
                {
                    dprintf (2, ("sn: 0, tree is set to %p", plug_start));
                    tree = plug_start;
                }

                verify_pins_with_post_plug_info("before insert node");

                tree = insert_node (plug_start, ++sequence_number, tree, last_node);
                dprintf (3, ("tree is %p (b: %zx) after insert_node(lc: %p, rc: %p)",
                    tree, brick_of (tree),
                    (tree + node_left_child (tree)), (tree + node_right_child (tree))));
                last_node = plug_start;

#ifdef _DEBUG
                // If we detect if the last plug is pinned plug right before us, we should save this gap info
                if (!pinned_plug_p)
                {
                    if (mark_stack_tos > 0)
                    {
                        mark& m = mark_stack_array[mark_stack_tos - 1];
                        if (m.has_post_plug_info())
                        {
                            uint8_t* post_plug_info_start = m.saved_post_plug_info_start;
                            size_t* current_plug_gap_start = (size_t*)(plug_start - sizeof (plug_and_gap));
                            if ((uint8_t*)current_plug_gap_start == post_plug_info_start)
                            {
                                dprintf (3, ("Ginfo: %zx, %zx, %zx",
                                    *current_plug_gap_start, *(current_plug_gap_start + 1),
                                    *(current_plug_gap_start + 2)));
                                memcpy (&(m.saved_post_plug_debug), current_plug_gap_start, sizeof (gap_reloc_pair));
                            }
                        }
                    }
                }
#endif //_DEBUG

                verify_pins_with_post_plug_info("after insert node");
            }
        }

        if (num_pinned_plugs_in_plug > 1)
        {
            dprintf (3, ("more than %zd pinned plugs in this plug", num_pinned_plugs_in_plug));
        }

        x = find_next_marked (x, end, use_mark_list, mark_list_next, mark_list_index);
    }

#ifndef USE_REGIONS
    while (!pinned_plug_que_empty_p())
    {
        if (settings.promotion)
        {
            uint8_t* pplug = pinned_plug (oldest_pin());
            if (in_range_for_segment (pplug, ephemeral_heap_segment))
            {
                consing_gen = ensure_ephemeral_heap_segment (consing_gen);
                //allocate all of the generation gaps
                while (active_new_gen_number > 0)
                {
                    active_new_gen_number--;

                    if (active_new_gen_number == (max_generation - 1))
                    {
                        maxgen_pinned_compact_before_advance = generation_pinned_allocation_compact_size (generation_of (max_generation));
                        if (!demote_gen1_p)
                            advance_pins_for_demotion (consing_gen);
                    }

                    generation* gen = generation_of (active_new_gen_number);
                    plan_generation_start (gen, consing_gen, 0);

                    if (demotion_low == MAX_PTR)
                    {
                        demotion_low = pplug;
                        dprintf (3, ("end plan: dlow->%p", demotion_low));
                    }

                    dprintf (2, ("(%d)gen%d plan start: %zx",
                                  heap_number, active_new_gen_number, (size_t)generation_plan_allocation_start (gen)));
                    assert (generation_plan_allocation_start (gen));
                }
            }
        }

        if (pinned_plug_que_empty_p())
            break;

        size_t  entry = deque_pinned_plug();
        mark*  m = pinned_plug_of (entry);
        uint8_t*  plug = pinned_plug (m);
        size_t  len = pinned_len (m);

        // detect pinned block in different segment (later) than
        // allocation segment
        heap_segment* nseg = heap_segment_rw (generation_allocation_segment (consing_gen));

        while ((plug < generation_allocation_pointer (consing_gen)) ||
               (plug >= heap_segment_allocated (nseg)))
        {
            assert ((plug < heap_segment_mem (nseg)) ||
                    (plug > heap_segment_reserved (nseg)));
            //adjust the end of the segment to be the end of the plug
            assert (generation_allocation_pointer (consing_gen)>=
                    heap_segment_mem (nseg));
            assert (generation_allocation_pointer (consing_gen)<=
                    heap_segment_committed (nseg));

            heap_segment_plan_allocated (nseg) =
                generation_allocation_pointer (consing_gen);
            //switch allocation segment
            nseg = heap_segment_next_rw (nseg);
            generation_allocation_segment (consing_gen) = nseg;
            //reset the allocation pointer and limits
            generation_allocation_pointer (consing_gen) =
                heap_segment_mem (nseg);
        }

        set_new_pin_info (m, generation_allocation_pointer (consing_gen));
        dprintf (2, ("pin %p b: %zx->%zx", plug, brick_of (plug),
            (size_t)(brick_table[brick_of (plug)])));

        generation_allocation_pointer (consing_gen) = plug + len;
        generation_allocation_limit (consing_gen) =
            generation_allocation_pointer (consing_gen);
        //Add the size of the pinned plug to the right pinned allocations
        //find out which gen this pinned plug came from
        int frgn = object_gennum (plug);
        if ((frgn != (int)max_generation) && settings.promotion)
        {
            generation_pinned_allocation_sweep_size ((generation_of (frgn +1))) += len;
        }
    }

    plan_generation_starts (consing_gen);
#endif //!USE_REGIONS

    descr_generations ("AP");

    print_free_and_plug ("AP");

    {
#ifdef SIMPLE_DPRINTF
        for (int gen_idx = 0; gen_idx <= max_generation; gen_idx++)
        {
            generation* temp_gen = generation_of (gen_idx);
            dynamic_data* temp_dd = dynamic_data_of (gen_idx);

            int added_pinning_ratio = 0;
            int artificial_pinned_ratio = 0;

            if (dd_pinned_survived_size (temp_dd) != 0)
            {
                added_pinning_ratio = (int)((float)dd_added_pinned_size (temp_dd) * 100 / (float)dd_pinned_survived_size (temp_dd));
                artificial_pinned_ratio = (int)((float)dd_artificial_pinned_survived_size (temp_dd) * 100 / (float)dd_pinned_survived_size (temp_dd));
            }

            size_t padding_size =
#ifdef SHORT_PLUGS
                dd_padding_size (temp_dd);
#else
                0;
#endif //SHORT_PLUGS
            dprintf (2, ("gen%d: NON PIN alloc: %zd, pin com: %zd, sweep: %zd, surv: %zd, pinsurv: %zd(%d%% added, %d%% art), np surv: %zd, pad: %zd",
                gen_idx,
                generation_allocation_size (temp_gen),
                generation_pinned_allocation_compact_size (temp_gen),
                generation_pinned_allocation_sweep_size (temp_gen),
                dd_survived_size (temp_dd),
                dd_pinned_survived_size (temp_dd),
                added_pinning_ratio,
                artificial_pinned_ratio,
                (dd_survived_size (temp_dd) - dd_pinned_survived_size (temp_dd)),
                padding_size));

#ifndef USE_REGIONS
            dprintf (1, ("gen%d: %p, %p(%zd)",
                gen_idx,
                generation_allocation_start (temp_gen),
                generation_plan_allocation_start (temp_gen),
                (size_t)(generation_plan_allocation_start (temp_gen) - generation_allocation_start (temp_gen))));
#endif //USE_REGIONS
        }
#endif //SIMPLE_DPRINTF
    }

    if (settings.condemned_generation == (max_generation - 1 ))
    {
        generation* older_gen = generation_of (settings.condemned_generation + 1);
        size_t rejected_free_space = generation_free_obj_space (older_gen) - r_free_obj_space;
        size_t free_list_allocated = generation_free_list_allocated (older_gen) - r_older_gen_free_list_allocated;
        size_t end_seg_allocated = generation_end_seg_allocated (older_gen) - r_older_gen_end_seg_allocated;
        size_t condemned_allocated = generation_condemned_allocated (older_gen) - r_older_gen_condemned_allocated;

        size_t growth = end_seg_allocated + condemned_allocated;

        if (growth > 0)
        {
            dprintf (1, ("gen2 grew %zd (end seg alloc: %zd, condemned alloc: %zd",
                         growth, end_seg_allocated, condemned_allocated));

            maxgen_size_inc_p = true;
        }
        else
        {
            dprintf (1, ("gen2 didn't grow (end seg alloc: %zd, , condemned alloc: %zd, gen1 c alloc: %zd",
                         end_seg_allocated, condemned_allocated,
                         generation_condemned_allocated (generation_of (max_generation - 1))));
        }

        dprintf (2, ("older gen's free alloc: %zd->%zd, seg alloc: %zd->%zd, condemned alloc: %zd->%zd",
                    r_older_gen_free_list_allocated, generation_free_list_allocated (older_gen),
                    r_older_gen_end_seg_allocated, generation_end_seg_allocated (older_gen),
                    r_older_gen_condemned_allocated, generation_condemned_allocated (older_gen)));

        dprintf (2, ("this GC did %zd free list alloc(%zd bytes free space rejected)",
            free_list_allocated, rejected_free_space));

        maxgen_size_increase* maxgen_size_info = &(get_gc_data_per_heap()->maxgen_size_info);
        maxgen_size_info->free_list_allocated = free_list_allocated;
        maxgen_size_info->free_list_rejected = rejected_free_space;
        maxgen_size_info->end_seg_allocated = end_seg_allocated;
        maxgen_size_info->condemned_allocated = condemned_allocated;
        maxgen_size_info->pinned_allocated = maxgen_pinned_compact_before_advance;
        maxgen_size_info->pinned_allocated_advance = generation_pinned_allocation_compact_size (generation_of (max_generation)) - maxgen_pinned_compact_before_advance;

#ifdef FREE_USAGE_STATS
        int free_list_efficiency = 0;
        if ((free_list_allocated + rejected_free_space) != 0)
            free_list_efficiency = (int)(((float) (free_list_allocated) / (float)(free_list_allocated + rejected_free_space)) * (float)100);

        size_t running_free_list_efficiency = generation_allocator_efficiency_percent(older_gen);

        dprintf (1, ("gen%d free list alloc effi: %d%%, current effi: %zu%%",
                    older_gen->gen_num,
                    free_list_efficiency, running_free_list_efficiency));

        dprintf (1, ("gen2 free list change"));
        for (int j = 0; j < NUM_GEN_POWER2; j++)
        {
            dprintf (1, ("[h%d][#%zd]: 2^%d: F: %zd->%zd(%zd), P: %zd",
                heap_number,
                settings.gc_index,
                (j + 10), r_older_gen_free_space[j], older_gen->gen_free_spaces[j],
                (ptrdiff_t)(r_older_gen_free_space[j] - older_gen->gen_free_spaces[j]),
                (generation_of(max_generation - 1))->gen_plugs[j]));
        }
#endif //FREE_USAGE_STATS
    }

    size_t fragmentation =
        generation_fragmentation (generation_of (condemned_gen_number),
                                  consing_gen,
                                  heap_segment_allocated (ephemeral_heap_segment));

    dprintf (2,("Fragmentation: %zd", fragmentation));
    dprintf (2,("---- End of Plan phase ----"));

    // We may update write barrier code.  We assume here EE has been suspended if we are on a GC thread.
    assert(IsGCInProgress());

    BOOL should_expand = FALSE;
    BOOL should_compact= FALSE;

#ifndef USE_REGIONS
    ephemeral_promotion = FALSE;
#endif //!USE_REGIONS

#ifdef HOST_64BIT
    if ((!settings.concurrent) &&
#ifdef USE_REGIONS
        !special_sweep_p &&
#endif //USE_REGIONS
        !provisional_mode_triggered &&
        ((condemned_gen_number < max_generation) &&
         ((settings.gen0_reduction_count > 0) || (settings.entry_memory_load >= 95))))
    {
        dprintf (GTC_LOG, ("gen0 reduction count is %d, condemning %d, mem load %d",
                     settings.gen0_reduction_count,
                     condemned_gen_number,
                     settings.entry_memory_load));
        should_compact = TRUE;

        get_gc_data_per_heap()->set_mechanism (gc_heap_compact,
            ((settings.gen0_reduction_count > 0) ? compact_fragmented_gen0 : compact_high_mem_load));

#ifndef USE_REGIONS
        if ((condemned_gen_number >= (max_generation - 1)) &&
            dt_low_ephemeral_space_p (tuning_deciding_expansion))
        {
            dprintf (GTC_LOG, ("Not enough space for all ephemeral generations with compaction"));
            should_expand = TRUE;
        }
#endif //!USE_REGIONS
    }
    else
#endif // HOST_64BIT
    {
        should_compact = decide_on_compacting (condemned_gen_number, fragmentation, should_expand);
    }

    if (condemned_gen_number == max_generation)
    {
#ifdef FEATURE_LOH_COMPACTION
        if (settings.loh_compaction)
        {
            should_compact = TRUE;
            get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_loh_forced);
        }
        else
#endif //FEATURE_LOH_COMPACTION
        {
            GCToEEInterface::DiagWalkUOHSurvivors(__this, loh_generation);
            sweep_uoh_objects (loh_generation);
        }

        GCToEEInterface::DiagWalkUOHSurvivors(__this, poh_generation);
        sweep_uoh_objects (poh_generation);
    }
    else
    {
        settings.loh_compaction = FALSE;
    }

#ifdef MULTIPLE_HEAPS
#ifndef USE_REGIONS
    new_heap_segment = NULL;
#endif //!USE_REGIONS

    if (should_compact && should_expand)
        gc_policy = policy_expand;
    else if (should_compact)
        gc_policy = policy_compact;
    else
        gc_policy = policy_sweep;

    //vote for result of should_compact
    dprintf (3, ("Joining for compaction decision"));
    gc_t_join.join(this, gc_join_decide_on_compaction);
    if (gc_t_join.joined())
    {
#ifndef USE_REGIONS
        //safe place to delete large heap segments
        if (condemned_gen_number == max_generation)
        {
            for (int i = 0; i < n_heaps; i++)
            {
                g_heaps [i]->rearrange_uoh_segments ();
            }
        }
#endif //!USE_REGIONS
        if (maxgen_size_inc_p && provisional_mode_triggered
#ifdef BACKGROUND_GC
            && !is_bgc_in_progress()
#endif //BACKGROUND_GC
            )
        {
            pm_trigger_full_gc = true;
            dprintf (GTC_LOG, ("in PM: maxgen size inc, doing a sweeping gen1 and trigger NGC2"));
        }
        else
        {
#ifdef USE_REGIONS
            bool joined_special_sweep_p = false;
#else
            settings.demotion = FALSE;
#endif //USE_REGIONS
            int pol_max = policy_sweep;
#ifdef GC_CONFIG_DRIVEN
            BOOL is_compaction_mandatory = FALSE;
#endif //GC_CONFIG_DRIVEN

            int i;
            for (i = 0; i < n_heaps; i++)
            {
                if (pol_max < g_heaps[i]->gc_policy)
                    pol_max = policy_compact;
#ifdef USE_REGIONS
                joined_special_sweep_p |= g_heaps[i]->special_sweep_p;
#else
                // set the demotion flag is any of the heap has demotion
                if (g_heaps[i]->demotion_high >= g_heaps[i]->demotion_low)
                {
                    (g_heaps[i]->get_gc_data_per_heap())->set_mechanism_bit (gc_demotion_bit);
                    settings.demotion = TRUE;
                }
#endif //USE_REGIONS

#ifdef GC_CONFIG_DRIVEN
                if (!is_compaction_mandatory)
                {
                    int compact_reason = (g_heaps[i]->get_gc_data_per_heap())->get_mechanism (gc_heap_compact);
                    if (compact_reason >= 0)
                    {
                        if (gc_heap_compact_reason_mandatory_p[compact_reason])
                            is_compaction_mandatory = TRUE;
                    }
                }
#endif //GC_CONFIG_DRIVEN
            }

#ifdef GC_CONFIG_DRIVEN
            if (!is_compaction_mandatory)
            {
                // If compaction is not mandatory we can feel free to change it to a sweeping GC.
                // Note that we may want to change this to only checking every so often instead of every single GC.
                if (should_do_sweeping_gc (pol_max >= policy_compact))
                {
                    pol_max = policy_sweep;
                }
                else
                {
                    if (pol_max == policy_sweep)
                        pol_max = policy_compact;
                }
            }
#endif //GC_CONFIG_DRIVEN

            for (i = 0; i < n_heaps; i++)
            {
#ifdef USE_REGIONS
                g_heaps[i]->special_sweep_p = joined_special_sweep_p;
                if (joined_special_sweep_p)
                {
                    g_heaps[i]->gc_policy = policy_sweep;
                }
                else
#endif //USE_REGIONS
                if (pol_max > g_heaps[i]->gc_policy)
                    g_heaps[i]->gc_policy = pol_max;
#ifndef USE_REGIONS
                //get the segment while we are serialized
                if (g_heaps[i]->gc_policy == policy_expand)
                {
                    g_heaps[i]->new_heap_segment =
                        g_heaps[i]->soh_get_segment_to_expand();
                    if (!g_heaps[i]->new_heap_segment)
                    {
                        set_expand_in_full_gc (condemned_gen_number);
                        //we are out of memory, cancel the expansion
                        g_heaps[i]->gc_policy = policy_compact;
                    }
                }
#endif //!USE_REGIONS
            }

            BOOL is_full_compacting_gc = FALSE;

            if ((gc_policy >= policy_compact) && (condemned_gen_number == max_generation))
            {
                full_gc_counts[gc_type_compacting]++;
                is_full_compacting_gc = TRUE;
            }

            for (i = 0; i < n_heaps; i++)
            {
#ifndef USE_REGIONS
                if (g_gc_card_table!= g_heaps[i]->card_table)
                {
                    g_heaps[i]->copy_brick_card_table();
                }
#endif //!USE_REGIONS
                if (is_full_compacting_gc)
                {
                    g_heaps[i]->loh_alloc_since_cg = 0;
                }
            }
        }

#ifdef FEATURE_EVENT_TRACE
        if (informational_event_enabled_p)
        {
            gc_time_info[time_sweep] = GetHighPrecisionTimeStamp();
            gc_time_info[time_plan] = gc_time_info[time_sweep] - gc_time_info[time_plan];
        }
#endif //FEATURE_EVENT_TRACE

        dprintf(3, ("Starting all gc threads after compaction decision"));
        gc_t_join.restart();
    }

    should_compact = (gc_policy >= policy_compact);
    should_expand  = (gc_policy >= policy_expand);

#else //MULTIPLE_HEAPS
#ifndef USE_REGIONS
    //safe place to delete large heap segments
    if (condemned_gen_number == max_generation)
    {
        rearrange_uoh_segments ();
    }
#endif //!USE_REGIONS
    if (maxgen_size_inc_p && provisional_mode_triggered
#ifdef BACKGROUND_GC
        && !is_bgc_in_progress()
#endif //BACKGROUND_GC
        )
    {
        pm_trigger_full_gc = true;
        dprintf (GTC_LOG, ("in PM: maxgen size inc, doing a sweeping gen1 and trigger NGC2"));
    }
    else
    {
#ifndef USE_REGIONS
        // for regions it was already set when we set plan_gen_num for regions.
        settings.demotion = ((demotion_high >= demotion_low) ? TRUE : FALSE);
        if (settings.demotion)
            get_gc_data_per_heap()->set_mechanism_bit (gc_demotion_bit);
#endif //!USE_REGIONS

#ifdef GC_CONFIG_DRIVEN
        BOOL is_compaction_mandatory = FALSE;
        int compact_reason = get_gc_data_per_heap()->get_mechanism (gc_heap_compact);
        if (compact_reason >= 0)
            is_compaction_mandatory = gc_heap_compact_reason_mandatory_p[compact_reason];

        if (!is_compaction_mandatory)
        {
            if (should_do_sweeping_gc (should_compact))
                should_compact = FALSE;
            else
                should_compact = TRUE;
        }
#endif //GC_CONFIG_DRIVEN

        if (should_compact && (condemned_gen_number == max_generation))
        {
            full_gc_counts[gc_type_compacting]++;
            loh_alloc_since_cg = 0;
        }
    }

#ifdef FEATURE_EVENT_TRACE
    if (informational_event_enabled_p)
    {
        gc_time_info[time_sweep] = GetHighPrecisionTimeStamp();
        gc_time_info[time_plan] = gc_time_info[time_sweep] - gc_time_info[time_plan];
    }
#endif //FEATURE_EVENT_TRACE

#ifdef USE_REGIONS
    if (special_sweep_p)
    {
        should_compact = FALSE;
    }
#endif //!USE_REGIONS
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_LOH_COMPACTION
    loh_compacted_p = FALSE;
#endif //FEATURE_LOH_COMPACTION

    if (condemned_gen_number == max_generation)
    {
#ifdef FEATURE_LOH_COMPACTION
        if (settings.loh_compaction)
        {
            if (should_compact && plan_loh())
            {
                loh_compacted_p = TRUE;
            }
            else
            {
                GCToEEInterface::DiagWalkUOHSurvivors(__this, loh_generation);
                sweep_uoh_objects (loh_generation);
            }
        }
        else
        {
            if (loh_pinned_queue)
            {
                loh_pinned_queue_decay--;

                if (!loh_pinned_queue_decay)
                {
                    delete[] loh_pinned_queue;
                    loh_pinned_queue = 0;
                }
            }
        }
#endif //FEATURE_LOH_COMPACTION
    }

    if (!pm_trigger_full_gc && pm_stress_on && provisional_mode_triggered)
    {
        if ((settings.condemned_generation == (max_generation - 1)) &&
            ((settings.gc_index % 5) == 0)
#ifdef BACKGROUND_GC
            && !is_bgc_in_progress()
#endif //BACKGROUND_GC
            )
        {
            pm_trigger_full_gc = true;
        }
    }

    if (settings.condemned_generation == (max_generation - 1))
    {
        if (provisional_mode_triggered)
        {
            if (should_expand)
            {
                should_expand = FALSE;
                dprintf (GTC_LOG, ("h%d in PM cannot expand", heap_number));
            }
        }

        if (pm_trigger_full_gc)
        {
            should_compact = FALSE;
            dprintf (GTC_LOG, ("h%d PM doing sweeping", heap_number));
        }
    }

    if (should_compact)
    {
        dprintf (2,( "**** Doing Compacting GC ****"));

#if defined(USE_REGIONS) && defined(BACKGROUND_GC)
        if (should_update_end_mark_size())
        {
            background_soh_size_end_mark += generation_end_seg_allocated (older_gen) -
                                            r_older_gen_end_seg_allocated;
        }
#endif //USE_REGIONS && BACKGROUND_GC

#ifndef USE_REGIONS
        if (should_expand)
        {
#ifndef MULTIPLE_HEAPS
            heap_segment* new_heap_segment = soh_get_segment_to_expand();
#endif //!MULTIPLE_HEAPS
            if (new_heap_segment)
            {
                consing_gen = expand_heap(condemned_gen_number,
                                          consing_gen,
                                          new_heap_segment);
            }

            // If we couldn't get a new segment, or we were able to
            // reserve one but no space to commit, we couldn't
            // expand heap.
            if (ephemeral_heap_segment != new_heap_segment)
            {
                set_expand_in_full_gc (condemned_gen_number);
                should_expand = FALSE;
            }
        }
#endif //!USE_REGIONS

        generation_allocation_limit (condemned_gen1) =
            generation_allocation_pointer (condemned_gen1);
        if ((condemned_gen_number < max_generation))
        {
            generation_allocator (older_gen)->commit_alloc_list_changes();

            // Fix the allocation area of the older generation
            fix_older_allocation_area (older_gen);

#ifdef FEATURE_EVENT_TRACE
            if (record_fl_info_p)
            {
                // For plugs allocated in condemned we kept track of each one but only fire the
                // event for buckets with non zero items.
                uint16_t non_zero_buckets = 0;
                for (uint16_t bucket_index = 0; bucket_index < NUM_GEN2_ALIST; bucket_index++)
                {
                    if (bucket_info[bucket_index].count != 0)
                    {
                        if (bucket_index != non_zero_buckets)
                        {
                            bucket_info[non_zero_buckets].set (bucket_index,
                                                            bucket_info[bucket_index].count,
                                                            bucket_info[bucket_index].size);
                        }
                        else
                        {
                            bucket_info[bucket_index].index = bucket_index;
                        }
                        non_zero_buckets++;
                    }
                }

                if (non_zero_buckets)
                {
                    FIRE_EVENT(GCFitBucketInfo,
                            (uint16_t)etw_bucket_kind::plugs_in_condemned,
                            recorded_fl_info_size,
                            non_zero_buckets,
                            (uint32_t)(sizeof (etw_bucket_info)),
                            (void *)bucket_info);
                    init_bucket_info();
                }

                // We want to get an idea of the sizes of free items in the top 25% of the free list
                // for gen2 (to be accurate - we stop as soon as the size we count exceeds 25%. This
                // is just so that if we have a really big free item we will still count that one).
                // The idea is we want to see if they all in a few big ones or many smaller ones?
                // To limit the amount of time we spend counting, we stop till we have counted the
                // top percentage, or exceeded max_etw_item_count items.
                size_t max_size_to_count = generation_free_list_space (older_gen) / 4;
                non_zero_buckets =
                    generation_allocator (older_gen)->count_largest_items (bucket_info,
                                                                        max_size_to_count,
                                                                        max_etw_item_count,
                                                                        &recorded_fl_info_size);
                if (non_zero_buckets)
                {
                    FIRE_EVENT(GCFitBucketInfo,
                            (uint16_t)etw_bucket_kind::largest_fl_items,
                            recorded_fl_info_size,
                            non_zero_buckets,
                            (uint32_t)(sizeof (etw_bucket_info)),
                            (void *)bucket_info);
                }
            }
#endif //FEATURE_EVENT_TRACE
        }
#ifndef USE_REGIONS
        assert (generation_allocation_segment (consing_gen) ==
                ephemeral_heap_segment);
#endif //!USE_REGIONS

        GCToEEInterface::DiagWalkSurvivors(__this, true);

        relocate_phase (condemned_gen_number, first_condemned_address);
        compact_phase (condemned_gen_number, first_condemned_address,
                       (!settings.demotion && settings.promotion));
        fix_generation_bounds (condemned_gen_number, consing_gen);
        assert (generation_allocation_limit (youngest_generation) ==
                generation_allocation_pointer (youngest_generation));

#ifndef USE_REGIONS
        if (condemned_gen_number >= (max_generation -1))
        {
#ifdef MULTIPLE_HEAPS
            gc_t_join.join(this, gc_join_rearrange_segs_compaction);
            if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
            {
#ifdef MULTIPLE_HEAPS
                for (int i = 0; i < n_heaps; i++)
                {
                    g_heaps [i]->rearrange_heap_segments (TRUE);
                }
#else //MULTIPLE_HEAPS
                rearrange_heap_segments (TRUE);
#endif //MULTIPLE_HEAPS

#ifdef MULTIPLE_HEAPS
                gc_t_join.restart();
#endif //MULTIPLE_HEAPS
            }

            if (should_expand)
            {
                //fix the start_segment for the ephemeral generations
                for (int i = 0; i < max_generation; i++)
                {
                    generation* gen = generation_of (i);
                    generation_start_segment (gen) = ephemeral_heap_segment;
                    generation_allocation_segment (gen) = ephemeral_heap_segment;
                }
            }
        }
#endif //!USE_REGIONS

        {
#ifdef USE_REGIONS
            end_gen0_region_committed_space = get_gen0_end_space (memory_type_committed);
            dprintf(REGIONS_LOG, ("h%d computed the end_gen0_region_committed_space value to be %zd", heap_number, end_gen0_region_committed_space));
#endif //USE_REGIONS
#ifdef MULTIPLE_HEAPS
            dprintf(3, ("Joining after end of compaction"));
            gc_t_join.join(this, gc_join_adjust_handle_age_compact);
            if (gc_t_join.joined())
            {
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_EVENT_TRACE
                if (informational_event_enabled_p)
                {
                    uint64_t current_time = GetHighPrecisionTimeStamp();
                    gc_time_info[time_compact] = current_time - gc_time_info[time_compact];
                }
#endif //FEATURE_EVENT_TRACE

#ifdef _DEBUG
                verify_committed_bytes ();
#endif // _DEBUG

#ifdef MULTIPLE_HEAPS
                //join all threads to make sure they are synchronized
                dprintf(3, ("Restarting after Promotion granted"));
                gc_t_join.restart();
            }
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_PREMORTEM_FINALIZATION
            finalize_queue->UpdatePromotedGenerations (condemned_gen_number,
                                                       (!settings.demotion && settings.promotion));
#endif // FEATURE_PREMORTEM_FINALIZATION

            ScanContext sc;
            sc.thread_number = heap_number;
            sc.thread_count = n_heaps;
            sc.promotion = FALSE;
            sc.concurrent = FALSE;
            // new generations bounds are set can call this guy
            if (settings.promotion && !settings.demotion)
            {
                dprintf (2, ("Promoting EE roots for gen %d",
                             condemned_gen_number));
                GCScan::GcPromotionsGranted(condemned_gen_number, max_generation, &sc);
            }
            else if (settings.demotion)
            {
                dprintf (2, ("Demoting EE roots for gen %d",
                             condemned_gen_number));
                GCScan::GcDemote (condemned_gen_number, max_generation, &sc);
            }
        }

        {
            reset_pinned_queue_bos();
#ifndef USE_REGIONS
            unsigned int  gen_number = (unsigned int)min ((int)max_generation, 1 + condemned_gen_number);
            generation*  gen = generation_of (gen_number);
            uint8_t*  low = generation_allocation_start (generation_of (gen_number-1));
            uint8_t*  high =  heap_segment_allocated (ephemeral_heap_segment);
#endif //!USE_REGIONS

            while (!pinned_plug_que_empty_p())
            {
                mark*  m = pinned_plug_of (deque_pinned_plug());
                size_t len = pinned_len (m);
                uint8_t*  arr = (pinned_plug (m) - len);
                dprintf(3,("free [%zx %zx[ pin",
                            (size_t)arr, (size_t)arr + len));
                if (len != 0)
                {
                    assert (len >= Align (min_obj_size));
                    make_unused_array (arr, len);
                    // fix fully contained bricks + first one
                    // if the array goes beyond the first brick
                    size_t start_brick = brick_of (arr);
                    size_t end_brick = brick_of (arr + len);
                    if (end_brick != start_brick)
                    {
                        dprintf (3,
                                    ("Fixing bricks [%zx, %zx[ to point to unused array %zx",
                                    start_brick, end_brick, (size_t)arr));
                        set_brick (start_brick,
                                    arr - brick_address (start_brick));
                        size_t brick = start_brick+1;
                        while (brick < end_brick)
                        {
                            set_brick (brick, start_brick - brick);
                            brick++;
                        }
                    }

#ifdef USE_REGIONS
                    int gen_number = object_gennum_plan (arr);
                    generation* gen = generation_of (gen_number);
#else
                    //when we take an old segment to make the new
                    //ephemeral segment. we can have a bunch of
                    //pinned plugs out of order going to the new ephemeral seg
                    //and then the next plugs go back to max_generation
                    if ((heap_segment_mem (ephemeral_heap_segment) <= arr) &&
                        (heap_segment_reserved (ephemeral_heap_segment) > arr))
                    {
                        while ((low <= arr) && (high > arr))
                        {
                            gen_number--;
                            assert ((gen_number >= 1) || (demotion_low != MAX_PTR) ||
                                    settings.demotion || !settings.promotion);
                            dprintf (3, ("new free list generation %d", gen_number));

                            gen = generation_of (gen_number);
                            if (gen_number >= 1)
                                low = generation_allocation_start (generation_of (gen_number-1));
                            else
                                low = high;
                        }
                    }
                    else
                    {
                        dprintf (3, ("new free list generation %d", max_generation));
                        gen_number = max_generation;
                        gen = generation_of (gen_number);
                    }
#endif //USE_REGIONS

                    dprintf(3,("h%d threading %p (%zd) before pin in gen %d",
                        heap_number, arr, len, gen_number));
                    thread_gap (arr, len, gen);
                    add_gen_free (gen_number, len);
                }
            }
        }

        clear_gen1_cards();
    }
    else
    {
        //force promotion for sweep
        settings.promotion = TRUE;
        settings.compaction = FALSE;

#ifdef USE_REGIONS
        // This should be set for segs too actually. We should always reset demotion
        // if we sweep.
        settings.demotion = FALSE;
#endif //USE_REGIONS

        ScanContext sc;
        sc.thread_number = heap_number;
        sc.thread_count = n_heaps;
        sc.promotion = FALSE;
        sc.concurrent = FALSE;

        dprintf (2, ("**** Doing Mark and Sweep GC****"));

        if ((condemned_gen_number < max_generation))
        {
#ifdef FREE_USAGE_STATS
            memcpy (older_gen->gen_free_spaces, r_older_gen_free_space, sizeof (r_older_gen_free_space));
#endif //FREE_USAGE_STATS
            generation_allocator (older_gen)->copy_from_alloc_list (r_free_list);
            generation_free_list_space (older_gen) = r_free_list_space;
            generation_free_obj_space (older_gen) = r_free_obj_space;

#ifdef DOUBLY_LINKED_FL
            if (condemned_gen_number == (max_generation - 1))
            {
                dprintf (2, ("[h%d] no undo, FL %zd-%zd -> %zd, FO %zd+%zd=%zd",
                    heap_number,
                    generation_free_list_space (older_gen), gen2_removed_no_undo,
                    (generation_free_list_space (older_gen) - gen2_removed_no_undo),
                    generation_free_obj_space (older_gen), gen2_removed_no_undo,
                    (generation_free_obj_space (older_gen) + gen2_removed_no_undo)));

                generation_free_list_space (older_gen) -= gen2_removed_no_undo;
                generation_free_obj_space (older_gen) += gen2_removed_no_undo;
            }
#endif //DOUBLY_LINKED_FL

            generation_free_list_allocated (older_gen) = r_older_gen_free_list_allocated;
            generation_end_seg_allocated (older_gen) = r_older_gen_end_seg_allocated;
            generation_condemned_allocated (older_gen) = r_older_gen_condemned_allocated;
            generation_sweep_allocated (older_gen) += dd_survived_size (dynamic_data_of (condemned_gen_number));
            generation_allocation_limit (older_gen) = r_allocation_limit;
            generation_allocation_pointer (older_gen) = r_allocation_pointer;
            generation_allocation_context_start_region (older_gen) = r_allocation_start_region;
            generation_allocation_segment (older_gen) = r_allocation_segment;
#ifdef USE_REGIONS
            if (older_gen->gen_num == max_generation)
            {
                check_seg_gen_num (r_allocation_segment);
            }
#endif //USE_REGIONS
        }

        if ((condemned_gen_number < max_generation))
        {
            // Fix the allocation area of the older generation
            fix_older_allocation_area (older_gen);
        }

        GCToEEInterface::DiagWalkSurvivors(__this, false);

        make_free_lists (condemned_gen_number);
        size_t total_recovered_sweep_size = recover_saved_pinned_info();
        if (total_recovered_sweep_size > 0)
        {
            generation_free_obj_space (generation_of (max_generation)) -= total_recovered_sweep_size;
            dprintf (2, ("h%d: deduct %zd for pin, fo->%zd",
                heap_number, total_recovered_sweep_size,
                generation_free_obj_space (generation_of (max_generation))));
        }

#ifdef USE_REGIONS
        end_gen0_region_committed_space = get_gen0_end_space (memory_type_committed);
        dprintf(REGIONS_LOG, ("h%d computed the end_gen0_region_committed_space value to be %zd", heap_number, end_gen0_region_committed_space));
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
        dprintf(3, ("Joining after end of sweep"));
        gc_t_join.join(this, gc_join_adjust_handle_age_sweep);
        if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
        {
#ifdef FEATURE_EVENT_TRACE
            if (informational_event_enabled_p)
            {
                uint64_t current_time = GetHighPrecisionTimeStamp();
                gc_time_info[time_sweep] = current_time - gc_time_info[time_sweep];
            }
#endif //FEATURE_EVENT_TRACE

#ifdef USE_REGIONS
            if (!special_sweep_p)
#endif //USE_REGIONS
            {
                GCScan::GcPromotionsGranted(condemned_gen_number,
                                                max_generation, &sc);
            }

#ifndef USE_REGIONS
            if (condemned_gen_number >= (max_generation -1))
            {
#ifdef MULTIPLE_HEAPS
                for (int i = 0; i < n_heaps; i++)
                {
                    g_heaps[i]->rearrange_heap_segments(FALSE);
                }
#else
                rearrange_heap_segments(FALSE);
#endif //MULTIPLE_HEAPS
            }
#endif //!USE_REGIONS

#ifdef USE_REGIONS
            verify_region_to_generation_map ();
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
            //join all threads to make sure they are synchronized
            dprintf(3, ("Restarting after Promotion granted"));
            gc_t_join.restart();
#endif //MULTIPLE_HEAPS
        }

#ifdef FEATURE_PREMORTEM_FINALIZATION
#ifdef USE_REGIONS
        if (!special_sweep_p)
#endif //USE_REGIONS
        {
            finalize_queue->UpdatePromotedGenerations (condemned_gen_number, TRUE);
        }
#endif // FEATURE_PREMORTEM_FINALIZATION

#ifdef USE_REGIONS
        if (!special_sweep_p)
#endif //USE_REGIONS
        {
            clear_gen1_cards();
        }
    }

    //verify_partial();
}

void gc_heap::fix_generation_bounds (int condemned_gen_number,
                                     generation* consing_gen)
{
#ifndef _DEBUG
    UNREFERENCED_PARAMETER(consing_gen);
#endif //_DEBUG

    int gen_number = condemned_gen_number;
    dprintf (2, ("---- thread regions gen%d GC ----", gen_number));

#ifdef USE_REGIONS
    // For ephemeral GCs, we handle up till the generation_allocation_segment as that's the last one we
    // changed in the older gen.
    if (settings.promotion && (condemned_gen_number < max_generation))
    {
        int older_gen_number = condemned_gen_number + 1;
        generation* older_gen = generation_of (older_gen_number);
        heap_segment* last_alloc_region = generation_allocation_segment (older_gen);

        dprintf (REGIONS_LOG, ("fix till we see alloc region which is %p", heap_segment_mem (last_alloc_region)));

        heap_segment* region = heap_segment_rw (generation_start_segment (older_gen));
        while (region)
        {
            heap_segment_allocated (region) = heap_segment_plan_allocated (region);
            if (region == last_alloc_region)
                break;
            region = heap_segment_next (region);
        }
    }

    thread_final_regions (true);

    ephemeral_heap_segment = generation_start_segment (generation_of (0));
    alloc_allocated = heap_segment_allocated (ephemeral_heap_segment);
#else //USE_REGIONS
    assert (generation_allocation_segment (consing_gen) ==
            ephemeral_heap_segment);

    int bottom_gen = 0;

    while (gen_number >= bottom_gen)
    {
        generation*  gen = generation_of (gen_number);
        dprintf(3,("Fixing generation pointers for %d", gen_number));
        if ((gen_number < max_generation) && ephemeral_promotion)
        {
            size_t saved_eph_start_size = saved_ephemeral_plan_start_size[gen_number];

            make_unused_array (saved_ephemeral_plan_start[gen_number],
                               saved_eph_start_size);
            generation_free_obj_space (generation_of (max_generation)) += saved_eph_start_size;
            dprintf (2, ("[h%d] EP %p(%zd)", heap_number, saved_ephemeral_plan_start[gen_number],
                saved_ephemeral_plan_start_size[gen_number]));
        }
        reset_allocation_pointers (gen, generation_plan_allocation_start (gen));
        make_unused_array (generation_allocation_start (gen), generation_plan_allocation_start_size (gen));
        dprintf(3,(" start %zx", (size_t)generation_allocation_start (gen)));
        gen_number--;
    }
#ifdef MULTIPLE_HEAPS
    if (ephemeral_promotion)
    {
        //we are creating a generation fault. set the cards.
        // and we are only doing this for multiple heaps because in the single heap scenario the
        // new ephemeral generations will be empty and there'll be no need to set cards for the
        // old ephemeral generations that got promoted into max_generation.
        ptrdiff_t delta = 0;
        heap_segment* old_ephemeral_seg = seg_mapping_table_segment_of (saved_ephemeral_plan_start[max_generation-1]);

        assert (in_range_for_segment (saved_ephemeral_plan_start[max_generation-1], old_ephemeral_seg));
        size_t end_card = card_of (align_on_card (heap_segment_plan_allocated (old_ephemeral_seg)));
        size_t card = card_of (saved_ephemeral_plan_start[max_generation-1]);
        while (card != end_card)
        {
            set_card (card);
            card++;
        }
    }
#endif //MULTIPLE_HEAPS

#ifdef BACKGROUND_GC
    if (should_update_end_mark_size())
    {
        background_soh_size_end_mark = generation_size (max_generation);
    }
#endif //BACKGROUND_GC
#endif //!USE_REGIONS

    {
        alloc_allocated = heap_segment_plan_allocated(ephemeral_heap_segment);
        //reset the allocated size
#ifdef _DEBUG
        uint8_t* start = get_soh_start_object (ephemeral_heap_segment, youngest_generation);
        if (settings.promotion && !settings.demotion)
        {
            assert ((start + get_soh_start_obj_len (start)) ==
                    heap_segment_plan_allocated(ephemeral_heap_segment));
        }
#endif //_DEBUG
        heap_segment_allocated(ephemeral_heap_segment)=
            heap_segment_plan_allocated(ephemeral_heap_segment);
    }
}

#ifndef USE_REGIONS
uint8_t* gc_heap::generation_limit (int gen_number)
{
    if (settings.promotion)
    {
        if (gen_number <= 1)
            return heap_segment_reserved (ephemeral_heap_segment);
        else
            return generation_allocation_start (generation_of ((gen_number - 2)));
    }
    else
    {
        if (gen_number <= 0)
            return heap_segment_reserved (ephemeral_heap_segment);
        else
            return generation_allocation_start (generation_of ((gen_number - 1)));
    }
}

#endif //!USE_REGIONS

BOOL gc_heap::ensure_gap_allocation (int condemned_gen_number)
{
#ifndef USE_REGIONS
    uint8_t* start = heap_segment_allocated (ephemeral_heap_segment);
    size_t size = Align (min_obj_size)*(condemned_gen_number+1);
    assert ((start + size) <=
            heap_segment_reserved (ephemeral_heap_segment));
    if ((start + size) >
        heap_segment_committed (ephemeral_heap_segment))
    {
        if (!grow_heap_segment (ephemeral_heap_segment, start + size))
        {
            return FALSE;
        }
    }
#endif //USE_REGIONS
    return TRUE;
}

uint8_t* gc_heap::allocate_at_end (size_t size)
{
    uint8_t* start = heap_segment_allocated (ephemeral_heap_segment);
    size = Align (size);
    uint8_t* result = start;
    // only called to allocate a min obj so can't overflow here.
    assert ((start + size) <=
            heap_segment_reserved (ephemeral_heap_segment));
    //ensure_gap_allocation took care of it
    assert ((start + size) <=
            heap_segment_committed (ephemeral_heap_segment));
    heap_segment_allocated (ephemeral_heap_segment) += size;
    return result;
}

#ifdef USE_REGIONS
// Find the first non empty region and also does the following in the process -
// + decommit end of region if it's not a gen0 region;
// + set the region gen_num to the new one;
//
// For empty regions, we always return empty regions to free. Note that I'm returning
// gen0 empty regions as well, however, returning a region to free does not decommit.
//
// If this is called for a compacting GC, we know we always take the planned generation
// on the region (and set the new allocated); else this is called for sweep in which case
// it's more complicated -
//
// + if we are in the special sweep mode, we don't change the old gen number at all
// + if we are not in special sweep we need to promote all regions, including the SIP ones
//   because we make the assumption that this is the case for sweep for handles.
heap_segment* gc_heap::find_first_valid_region (heap_segment* region, bool compact_p, int* num_returned_regions)
{
    check_seg_gen_num (generation_allocation_segment (generation_of (max_generation)));

    dprintf (REGIONS_LOG, ("  FFVR region %zx(%p), gen%d",
        (size_t)region, (region ? heap_segment_mem (region) : 0),
        (region ? heap_segment_gen_num (region) : 0)));

    if (!region)
        return 0;

    heap_segment* current_region = region;

    do
    {
        int gen_num = heap_segment_gen_num (current_region);
        int plan_gen_num = -1;
        if (compact_p)
        {
            assert (settings.compaction);
            plan_gen_num = heap_segment_plan_gen_num (current_region);
            dprintf (REGIONS_LOG, ("  gen%d->%d", gen_num, plan_gen_num));
        }
        else
        {
            plan_gen_num = (special_sweep_p ? gen_num : get_plan_gen_num (gen_num));
            dprintf (REGIONS_LOG, ("  gen%d->%d, special_sweep_p %d, swept_in_plan %d",
                gen_num, plan_gen_num, (int)special_sweep_p,
                (int)heap_segment_swept_in_plan (current_region)));
        }

        uint8_t* allocated = (compact_p ?
                              heap_segment_plan_allocated (current_region) :
                              heap_segment_allocated (current_region));
        if (heap_segment_mem (current_region) == allocated)
        {
            heap_segment* region_to_delete = current_region;
            current_region = heap_segment_next (current_region);
            return_free_region (region_to_delete);
            (*num_returned_regions)++;

            dprintf (REGIONS_LOG, ("  h%d gen%d return region %p to free, current->%p(%p)",
                heap_number, gen_num, heap_segment_mem (region_to_delete),
                current_region, (current_region ? heap_segment_mem (current_region) : 0)));
            if (!current_region)
                return 0;
        }
        else
        {
            if (compact_p)
            {
                dprintf (REGIONS_LOG, ("  gen%d setting region %p alloc %p to plan %p",
                    gen_num, heap_segment_mem (current_region),
                    heap_segment_allocated (current_region),
                    heap_segment_plan_allocated (current_region)));

                if (heap_segment_swept_in_plan (current_region))
                {
                    assert (heap_segment_allocated (current_region) ==
                            heap_segment_plan_allocated (current_region));
                }
                else
                {
                    heap_segment_allocated (current_region) = heap_segment_plan_allocated (current_region);
                }
            }
            else
            {
                // Set this so we keep plan gen and gen the same.
                set_region_plan_gen_num (current_region, plan_gen_num);
            }

            if (gen_num >= soh_gen2)
            {
                dprintf (REGIONS_LOG, ("  gen%d decommit end of region %p(%p)",
                    gen_num, current_region, heap_segment_mem (current_region)));
                decommit_heap_segment_pages (current_region, 0);
            }

            dprintf (REGIONS_LOG, ("  set region %p(%p) gen num to %d",
                current_region, heap_segment_mem (current_region), plan_gen_num));
            set_region_gen_num (current_region, plan_gen_num);
            break;
        }
    } while (current_region);

    assert (current_region);

    if (heap_segment_swept_in_plan (current_region))
    {
        int gen_num = heap_segment_gen_num (current_region);
        dprintf (REGIONS_LOG, ("threading SIP region %p surv %zd onto gen%d",
            heap_segment_mem (current_region), heap_segment_survived (current_region), gen_num));

        generation* gen = generation_of (gen_num);
        generation_allocator (gen)->thread_sip_fl (current_region);
        generation_free_list_space (gen) += heap_segment_free_list_size (current_region);
        generation_free_obj_space (gen) += heap_segment_free_obj_size (current_region);
    }

    // Take this opportunity to make sure all the regions left with flags only for this GC are reset.
    clear_region_sweep_in_plan (current_region);
    clear_region_demoted (current_region);

    return current_region;
}

void gc_heap::thread_final_regions (bool compact_p)
{
    int num_returned_regions = 0;
    int num_new_regions = 0;

    for (int i = 0; i < max_generation; i++)
    {
        if (reserved_free_regions_sip[i])
        {
            return_free_region (reserved_free_regions_sip[i]);
        }
    }

    int condemned_gen_number = settings.condemned_generation;
    generation_region_info generation_final_regions[max_generation + 1];
    memset (generation_final_regions, 0, sizeof (generation_final_regions));

    // Step 1: we initialize all the regions for generations we are not condemning with their
    // current head and tail as we know these regions will for sure exist.
    for (int gen_idx = max_generation; gen_idx > condemned_gen_number; gen_idx--)
    {
        generation* gen = generation_of (gen_idx);
        // Note this needs to be the first rw region as we will not be changing any ro regions and
        // we will work on thread rw regions here.
        generation_final_regions[gen_idx].head = heap_segment_rw (generation_start_segment (gen));
        generation_final_regions[gen_idx].tail = generation_tail_region (gen);
    }

#ifdef BACKGROUND_GC
    heap_segment* max_gen_tail_region = 0;
    if (should_update_end_mark_size())
    {
        max_gen_tail_region = generation_final_regions[max_generation].tail;
    }
#endif //BACKGROUND_GC

    // Step 2: for each region in the condemned generations, we thread it onto its planned generation
    // in our generation_final_regions array.
    for (int gen_idx = condemned_gen_number; gen_idx >= 0; gen_idx--)
    {
        heap_segment* current_region = heap_segment_rw (generation_start_segment (generation_of (gen_idx)));
        dprintf (REGIONS_LOG, ("gen%d start from %p", gen_idx, heap_segment_mem (current_region)));

        while ((current_region = find_first_valid_region (current_region, compact_p, &num_returned_regions)))
        {
            assert (!compact_p ||
                    (heap_segment_plan_gen_num (current_region) == heap_segment_gen_num (current_region)));
            int new_gen_num = heap_segment_plan_gen_num (current_region);
            generation* new_gen = generation_of (new_gen_num);
            heap_segment* next_region = heap_segment_next (current_region);
            if (generation_final_regions[new_gen_num].head)
            {
                assert (generation_final_regions[new_gen_num].tail);
                // The new gen already exists, just thread this region onto it.
                dprintf (REGIONS_LOG, ("gen%d exists, tail region %p next -> %p",
                    new_gen_num, heap_segment_mem (generation_final_regions[new_gen_num].tail),
                    heap_segment_mem (current_region)));
                heap_segment_next (generation_final_regions[new_gen_num].tail) = current_region;
                generation_final_regions[new_gen_num].tail = current_region;
            }
            else
            {
                generation_final_regions[new_gen_num].head = current_region;
                generation_final_regions[new_gen_num].tail = current_region;
            }

            current_region = next_region;
        }
    }

    // Step 3: all the tail regions' next needs to be set to 0.
    for (int gen_idx = 0; gen_idx <= max_generation; gen_idx++)
    {
        generation* gen = generation_of (gen_idx);
        if (generation_final_regions[gen_idx].tail)
        {
            heap_segment_next (generation_final_regions[gen_idx].tail) = 0;
            //if (heap_segment_next (generation_final_regions[gen_idx].tail) != 0)
            //{
            //    dprintf (REGIONS_LOG, ("tail->next is %zx",
            //        heap_segment_next (generation_final_regions[gen_idx].tail)));
            //    GCToOSInterface::DebugBreak();
            //}
        }
    }

#ifdef BACKGROUND_GC
    if (max_gen_tail_region)
    {
        max_gen_tail_region = heap_segment_next (max_gen_tail_region);

        while (max_gen_tail_region)
        {
            background_soh_size_end_mark += heap_segment_allocated (max_gen_tail_region) -
                                            heap_segment_mem (max_gen_tail_region);

            max_gen_tail_region = heap_segment_next (max_gen_tail_region);
        }
    }
#endif //BACKGROUND_GC

    // Step 4: if a generation doesn't have any regions, we need to get a new one for it;
    // otherwise we just set the head region as the start region for that generation.
    for (int gen_idx = 0; gen_idx <= max_generation; gen_idx++)
    {
        bool condemned_p = (gen_idx <= condemned_gen_number);
        assert (condemned_p || generation_final_regions[gen_idx].head);

        generation* gen = generation_of (gen_idx);
        heap_segment* start_region = 0;

        if (generation_final_regions[gen_idx].head)
        {
            if (condemned_p)
            {
                start_region = generation_final_regions[gen_idx].head;
                thread_start_region (gen, start_region);
            }
            generation_tail_region (gen) = generation_final_regions[gen_idx].tail;
            dprintf (REGIONS_LOG, ("setting gen%d start %p, tail %p",
                gen_idx,
                heap_segment_mem (heap_segment_rw (generation_start_segment (gen))),
                heap_segment_mem (generation_tail_region (gen))));
        }
        else
        {
            start_region = get_free_region (gen_idx);
            assert (start_region);
            num_new_regions++;
            thread_start_region (gen, start_region);
            dprintf (REGIONS_LOG, ("creating new gen%d at %p", gen_idx, heap_segment_mem (start_region)));
        }

        if (condemned_p)
        {
            uint8_t* gen_start = heap_segment_mem (start_region);
            reset_allocation_pointers (gen, gen_start);
        }
    }

    int net_added_regions = num_new_regions - num_returned_regions;
    dprintf (REGIONS_LOG, ("TFR: added %d, returned %d, net %d", num_new_regions, num_returned_regions, net_added_regions));

    // TODO: For sweeping GCs by design we will need to get a new region for gen0 unless we are doing a special sweep.
    // This means we need to know when we decided to sweep that we can get a new region (if needed). If we can't, we
    // need to turn special sweep on.
    if ((settings.compaction || special_sweep_p) && (net_added_regions > 0))
    {
        new_regions_in_threading += net_added_regions;

        assert (!"we shouldn't be getting new regions in TFR!");
    }

    verify_regions (true, false);
}

void gc_heap::thread_start_region (generation* gen, heap_segment* region)
{
    heap_segment* prev_region = generation_tail_ro_region (gen);

    if (prev_region)
    {
        heap_segment_next (prev_region) = region;
        dprintf (REGIONS_LOG,("gen%d tail ro %zx(%p) next -> %zx(%p)",
            gen->gen_num, (size_t)prev_region, heap_segment_mem (prev_region),
            (size_t)region, heap_segment_mem (region)));
    }
    else
    {
        generation_start_segment (gen) = region;
        dprintf (REGIONS_LOG, ("start region of gen%d -> %zx(%p)", gen->gen_num,
            (size_t)region, heap_segment_mem (region)));
    }

    dprintf (REGIONS_LOG, ("tail region of gen%d -> %zx(%p)", gen->gen_num,
        (size_t)region, heap_segment_mem (region)));
    generation_tail_region (gen) = region;
}

heap_segment* gc_heap::get_new_region (int gen_number, size_t size)
{
    heap_segment* new_region = get_free_region (gen_number, size);

    if (new_region)
    {
        switch (gen_number)
        {
        default:
            assert ((new_region->flags & (heap_segment_flags_loh | heap_segment_flags_poh)) == 0);
            break;

        case    loh_generation:
            new_region->flags |= heap_segment_flags_loh;
            break;

        case    poh_generation:
            new_region->flags |= heap_segment_flags_poh;
            break;
        }

        generation* gen = generation_of (gen_number);
        heap_segment_next (generation_tail_region (gen)) = new_region;
        generation_tail_region (gen) = new_region;

        verify_regions (gen_number, false, settings.concurrent);
    }

    return new_region;
}

void gc_heap::update_start_tail_regions(generation* gen,
                                         heap_segment* region_to_delete,
                                         heap_segment* prev_region,
                                         heap_segment* next_region)
{
    if (region_to_delete == heap_segment_rw (generation_start_segment (gen)))
    {
        assert (!prev_region);
        heap_segment* tail_ro_region = generation_tail_ro_region (gen);

        if (tail_ro_region)
        {
            heap_segment_next (tail_ro_region) = next_region;
            dprintf (REGIONS_LOG, ("gen%d tail ro %zx(%p) next updated to %zx(%p)",
                gen->gen_num, (size_t)tail_ro_region, heap_segment_mem (tail_ro_region),
                (size_t)next_region, heap_segment_mem (next_region)));
        }
        else
        {
            generation_start_segment (gen) = next_region;
            dprintf (REGIONS_LOG, ("start region of gen%d updated to %zx(%p)", gen->gen_num,
                (size_t)next_region, heap_segment_mem (next_region)));
        }
    }

    if (region_to_delete == generation_tail_region (gen))
    {
        assert (!next_region);
        generation_tail_region (gen) = prev_region;
        dprintf (REGIONS_LOG, ("tail region of gen%d updated to %zx(%p)", gen->gen_num,
            (size_t)prev_region, heap_segment_mem (prev_region)));
    }

    verify_regions (false, settings.concurrent);
}

// There's one complication with deciding whether we can make a region SIP or not - if the plan_gen_num of
// a generation is not maxgen, and if we want to make every region in that generation maxgen, we need to
// make sure we can get a new region for this generation so we can guarantee each generation has at least
// one region. If we can't get a new region, we need to make sure we leave at least one region in that gen
// to guarantee our invariant.
//
// This new region we get needs to be temporarily recorded instead of being on the free_regions list because
// we can't use it for other purposes.
inline
bool gc_heap::should_sweep_in_plan (heap_segment* region)
{
    if (!enable_special_regions_p)
    {
        return false;
    }

    if (settings.reason == reason_induced_aggressive)
    {
        return false;
    }
    bool sip_p = false;
    int gen_num = get_region_gen_num (region);
    int new_gen_num = get_plan_gen_num (gen_num);
    heap_segment_swept_in_plan (region) = false;

    dprintf (REGIONS_LOG, ("checking if region %p should be SIP", heap_segment_mem (region)));

#ifdef STRESS_REGIONS
    // Only do this for testing or it would keep too much swept.
    if (0)
    {
        num_condemned_regions++;
        if ((num_condemned_regions % sip_seg_interval) == 0)
        {
            set_region_plan_gen_num (region, new_gen_num);
            sip_p = true;
        }

        if ((num_condemned_regions % sip_seg_maxgen_interval) == 0)
        {
            set_region_plan_gen_num (region, max_generation);
            sip_maxgen_regions_per_gen[gen_num]++;
            sip_p = true;
        }
    }
    else
#endif //STRESS_REGIONS
    {
        size_t basic_region_size = (size_t)1 << min_segment_size_shr;
        assert (heap_segment_gen_num (region) == heap_segment_plan_gen_num (region));

        uint8_t surv_ratio = (uint8_t)(((double)heap_segment_survived (region) * 100.0) / (double)basic_region_size);
        dprintf (2222, ("SSIP: region %p surv %hu / %zd = %d%%(%d)",
            heap_segment_mem (region),
            heap_segment_survived (region),
            basic_region_size,
            surv_ratio, sip_surv_ratio_th));
        if (surv_ratio >= sip_surv_ratio_th)
        {
            set_region_plan_gen_num (region, new_gen_num);
            sip_p = true;
        }

        if (settings.promotion && (new_gen_num < max_generation))
        {
            int old_card_surv_ratio =
                (int)(((double)heap_segment_old_card_survived (region) * 100.0) / (double)basic_region_size);
            dprintf (2222, ("SSIP: region %p old card surv %d / %zd = %d%%(%d)",
                heap_segment_mem (region),
                heap_segment_old_card_survived (region),
                basic_region_size,
                old_card_surv_ratio, sip_surv_ratio_th));
            if (old_card_surv_ratio >= sip_old_card_surv_ratio_th)
            {
                set_region_plan_gen_num (region, max_generation, true);
                sip_maxgen_regions_per_gen[gen_num]++;
                sip_p = true;
            }
        }
    }

    if (sip_p)
    {
        if ((new_gen_num < max_generation) &&
            (sip_maxgen_regions_per_gen[gen_num] == regions_per_gen[gen_num]))
        {
            assert (get_region_gen_num (region) == 0);
            assert (new_gen_num < max_generation);

            heap_segment* reserved_free_region = get_free_region (gen_num);
            if (reserved_free_region)
            {
                dprintf (REGIONS_LOG, ("all regions in gen%d -> SIP 2, get a new region for it %p",
                    gen_num, heap_segment_mem (reserved_free_region)));
                reserved_free_regions_sip[gen_num] = reserved_free_region;
            }
            else
            {
                // If we cannot get another region, simply revert our decision.
                sip_maxgen_regions_per_gen[gen_num]--;
                set_region_plan_gen_num (region, new_gen_num, true);
            }
        }
    }

    dprintf (REGIONS_LOG, ("region %p %s SIP", heap_segment_mem (region),
        (sip_p ? "is" : "is not")));
    return sip_p;
}

// For a region that we sweep in plan, we need to do the following -
//
// + set the swept_in_plan_p for this region.
// + update allocated for this region.
// + build bricks.
// + build free objects. We keep a list of them which will then be threaded onto the appropriate generation's
//   free list. This can be optimized, both gen0 and gen2 GCs are easy to handle - need to see how easy it is
//   to handle gen1 GCs as the commit/repair there is complicated.
//
// in plan_phase we also need to make sure to not call update_brick_table when handling end of this region,
// and the plan gen num is set accordingly.
void gc_heap::sweep_region_in_plan (heap_segment* region,
                                    BOOL use_mark_list,
                                    uint8_t**& mark_list_next,
                                    uint8_t** mark_list_index)
{
    set_region_sweep_in_plan (region);

    region->init_free_list();

    uint8_t* x = heap_segment_mem (region);
    uint8_t* last_marked_obj_start = 0;
    uint8_t* last_marked_obj_end = 0;
    uint8_t* end = heap_segment_allocated (region);
    dprintf (2222, ("h%d region %p->%p SIP, gen %d->%d, %s mark list(%p->%p, %p->%p)",
        heap_number, x, end, heap_segment_gen_num (region), heap_segment_plan_gen_num (region),
        (use_mark_list ? "using" : "not using"),
        (uint8_t*)mark_list_next, (mark_list_next ? *mark_list_next : 0),
        (uint8_t*)mark_list_index, (mark_list_index ? *mark_list_index : 0)));

#ifdef _DEBUG
    size_t survived = 0;
    uint8_t* saved_last_unmarked_obj_start = 0;
    uint8_t* saved_last_unmarked_obj_end = 0;
    size_t saved_obj_brick = 0;
    size_t saved_next_obj_brick = 0;
#endif //_DEBUG

    while (x < end)
    {
        uint8_t* obj = x;
        size_t obj_brick = (size_t)obj / brick_size;
        uint8_t* next_obj = 0;
        if (marked (obj))
        {
            if (pinned(obj))
            {
                clear_pinned (obj);
            }
            clear_marked (obj);

            size_t s = size (obj);
            next_obj = obj + Align (s);
            last_marked_obj_start = obj;
            last_marked_obj_end = next_obj;
#ifdef _DEBUG
            survived += s;
#endif //_DEBUG
            dprintf (4444, ("M: %p-%p(%zd)", obj, next_obj, s));
        }
        else
        {
            next_obj = find_next_marked (x, end, use_mark_list, mark_list_next, mark_list_index);

#ifdef _DEBUG
            saved_last_unmarked_obj_start = obj;
            saved_last_unmarked_obj_end = next_obj;
#endif //_DEBUG

            if ((next_obj > obj) && (next_obj != end))
            {
                size_t free_obj_size = next_obj - obj;
                make_unused_array (obj, free_obj_size);
                region->thread_free_obj (obj, free_obj_size);
                dprintf (4444, ("UM threading: %p-%p(%zd)", obj, next_obj, (next_obj - obj)));
            }
        }

        size_t next_obj_brick = (size_t)next_obj / brick_size;

#ifdef _DEBUG
        saved_obj_brick = obj_brick;
        saved_next_obj_brick = next_obj_brick;
#endif //_DEBUG

        if (next_obj_brick != obj_brick)
        {
            fix_brick_to_highest (obj, next_obj);
        }

        x = next_obj;
    }

    if (last_marked_obj_start)
    {
        // We only need to make sure we fix the brick the last marked object's end is in.
        // Note this brick could have been fixed already.
        size_t last_marked_obj_start_b = brick_of (last_marked_obj_start);
        size_t last_marked_obj_end_b = brick_of (last_marked_obj_end - 1);
        dprintf (REGIONS_LOG, ("last live obj %p(%p)-%p, fixing its brick(s) %zx-%zx",
            last_marked_obj_start, method_table (last_marked_obj_start), last_marked_obj_end,
            last_marked_obj_start_b, last_marked_obj_end_b));

        if (last_marked_obj_start_b == last_marked_obj_end_b)
        {
            set_brick (last_marked_obj_start_b,
                    (last_marked_obj_start - brick_address (last_marked_obj_start_b)));
        }
        else
        {
            set_brick (last_marked_obj_end_b,
                    (last_marked_obj_start_b - last_marked_obj_end_b));
        }
    }
    else
    {
        last_marked_obj_end = heap_segment_mem (region);
    }

#ifdef _DEBUG
    size_t region_index = get_basic_region_index_for_address (heap_segment_mem (region));
    dprintf (REGIONS_LOG, ("region #%zd %p survived %zd, %s recorded %d",
        region_index, heap_segment_mem (region), survived,
        ((survived == heap_segment_survived (region)) ? "same as" : "diff from"),
        heap_segment_survived (region)));
#ifdef MULTIPLE_HEAPS
    assert (survived <= heap_segment_survived (region));
#else
    assert (survived == heap_segment_survived (region));
#endif //MULTIPLE_HEAPS
#endif //_DEBUG

    assert (last_marked_obj_end);
    save_allocated(region);
    heap_segment_allocated (region) = last_marked_obj_end;
    heap_segment_plan_allocated (region) = heap_segment_allocated (region);

    int plan_gen_num = heap_segment_plan_gen_num (region);
    if (plan_gen_num < heap_segment_gen_num (region))
    {
        generation_allocation_size (generation_of (plan_gen_num)) += heap_segment_survived (region);
        dprintf (REGIONS_LOG, ("sip: g%d alloc size is now %zd", plan_gen_num,
            generation_allocation_size (generation_of (plan_gen_num))));
    }
}

inline
void gc_heap::check_demotion_helper_sip (uint8_t** pval, int parent_gen_num, uint8_t* parent_loc)
{
    uint8_t* child_object = *pval;
    if (!is_in_heap_range (child_object))
        return;
    assert (child_object != nullptr);
    int child_object_plan_gen = get_region_plan_gen_num (child_object);

    if (child_object_plan_gen < parent_gen_num)
    {
        set_card (card_of (parent_loc));
    }

    dprintf (3, ("SCS %d, %d", child_object_plan_gen, parent_gen_num));
}

#endif //USE_REGIONS
#ifndef USE_REGIONS
#ifdef SEG_REUSE_STATS
size_t gc_heap::dump_buckets (size_t* ordered_indices, int count, size_t* total_size)
{
    size_t total_items = 0;
    *total_size = 0;
    for (int i = 0; i < count; i++)
    {
        total_items += ordered_indices[i];
        *total_size += ordered_indices[i] << (MIN_INDEX_POWER2 + i);
        dprintf (SEG_REUSE_LOG_0, ("[%d]%4d 2^%2d", heap_number, ordered_indices[i], (MIN_INDEX_POWER2 + i)));
    }
    dprintf (SEG_REUSE_LOG_0, ("[%d]Total %d items, total size is 0x%zx", heap_number, total_items, *total_size));
    return total_items;
}

#endif //SEG_REUSE_STATS

void gc_heap::count_plug (size_t last_plug_size, uint8_t*& last_plug)
{
    // detect pinned plugs
    if (!pinned_plug_que_empty_p() && (last_plug == pinned_plug (oldest_pin())))
    {
        deque_pinned_plug();
        update_oldest_pinned_plug();
        dprintf (3, ("deque pin,now oldest pin is %p", pinned_plug (oldest_pin())));
    }
    else
    {
        size_t plug_size = last_plug_size + Align(min_obj_size);
        BOOL is_padded = FALSE;

#ifdef SHORT_PLUGS
        plug_size += Align (min_obj_size);
        is_padded = TRUE;
#endif //SHORT_PLUGS

#ifdef RESPECT_LARGE_ALIGNMENT
        plug_size += switch_alignment_size (is_padded);
#endif //RESPECT_LARGE_ALIGNMENT

        total_ephemeral_plugs += plug_size;
        size_t plug_size_power2 = round_up_power2 (plug_size);
        ordered_plug_indices[relative_index_power2_plug (plug_size_power2)]++;
        dprintf (SEG_REUSE_LOG_1, ("[%d]count_plug: adding 0x%p - %zd (2^%d) to ordered plug array",
            heap_number,
            last_plug,
            plug_size,
            (relative_index_power2_plug (plug_size_power2) + MIN_INDEX_POWER2)));
    }
}

void gc_heap::count_plugs_in_brick (uint8_t* tree, uint8_t*& last_plug)
{
    assert ((tree != NULL));
    if (node_left_child (tree))
    {
        count_plugs_in_brick (tree + node_left_child (tree), last_plug);
    }

    if (last_plug != 0)
    {
        uint8_t*  plug = tree;
        size_t gap_size = node_gap_size (plug);
        uint8_t*   gap = (plug - gap_size);
        uint8_t*  last_plug_end = gap;
        size_t  last_plug_size = (last_plug_end - last_plug);
        dprintf (3, ("tree: %p, last plug: %p, gap size: %zx, gap: %p, last plug size: %zx",
            tree, last_plug, gap_size, gap, last_plug_size));

        if (tree == oldest_pinned_plug)
        {
            dprintf (3, ("tree %p is pinned, last plug is %p, size is %zx",
                tree, last_plug, last_plug_size));
            mark* m = oldest_pin();
            if (m->has_pre_plug_info())
            {
                last_plug_size += sizeof (gap_reloc_pair);
                dprintf (3, ("pin %p has pre plug, adjusting plug size to %zx", tree, last_plug_size));
            }
        }
        // Can't assert here - if it's a pinned plug it can be less.
        //assert (last_plug_size >= Align (min_obj_size));

        count_plug (last_plug_size, last_plug);
    }

    last_plug = tree;

    if (node_right_child (tree))
    {
        count_plugs_in_brick (tree + node_right_child (tree), last_plug);
    }
}

void gc_heap::build_ordered_plug_indices ()
{
    memset (ordered_plug_indices, 0, sizeof(ordered_plug_indices));
    memset (saved_ordered_plug_indices, 0, sizeof(saved_ordered_plug_indices));

    uint8_t*  start_address = generation_limit (max_generation);
    uint8_t* end_address = heap_segment_allocated (ephemeral_heap_segment);
    size_t  current_brick = brick_of (start_address);
    size_t  end_brick = brick_of (end_address - 1);
    uint8_t* last_plug = 0;

    //Look for the right pinned plug to start from.
    reset_pinned_queue_bos();
    while (!pinned_plug_que_empty_p())
    {
        mark* m = oldest_pin();
        if ((m->first >= start_address) && (m->first < end_address))
        {
            dprintf (3, ("found a pin %p between %p and %p", m->first, start_address, end_address));

            break;
        }
        else
            deque_pinned_plug();
    }

    update_oldest_pinned_plug();

    while (current_brick <= end_brick)
    {
        int brick_entry =  brick_table [ current_brick ];
        if (brick_entry >= 0)
        {
            count_plugs_in_brick (brick_address (current_brick) + brick_entry -1, last_plug);
        }

        current_brick++;
    }

    if (last_plug !=0)
    {
        count_plug (end_address - last_plug, last_plug);
    }

    // we need to make sure that after fitting all the existing plugs, we
    // have big enough free space left to guarantee that the next allocation
    // will succeed.
    size_t extra_size = END_SPACE_AFTER_GC_FL;
    total_ephemeral_plugs += extra_size;
    dprintf (SEG_REUSE_LOG_0, ("Making sure we can fit a large object after fitting all plugs"));
    ordered_plug_indices[relative_index_power2_plug (round_up_power2 (extra_size))]++;

    memcpy (saved_ordered_plug_indices, ordered_plug_indices, sizeof(ordered_plug_indices));

#ifdef SEG_REUSE_STATS
    dprintf (SEG_REUSE_LOG_0, ("Plugs:"));
    size_t total_plug_power2 = 0;
    dump_buckets (ordered_plug_indices, MAX_NUM_BUCKETS, &total_plug_power2);
    dprintf (SEG_REUSE_LOG_0, ("plugs: 0x%zx (rounded up to 0x%zx (%d%%))",
                total_ephemeral_plugs,
                total_plug_power2,
                (total_ephemeral_plugs ?
                    (total_plug_power2 * 100 / total_ephemeral_plugs) :
                    0)));
    dprintf (SEG_REUSE_LOG_0, ("-------------------"));
#endif // SEG_REUSE_STATS
}

void gc_heap::init_ordered_free_space_indices ()
{
    memset (ordered_free_space_indices, 0, sizeof(ordered_free_space_indices));
    memset (saved_ordered_free_space_indices, 0, sizeof(saved_ordered_free_space_indices));
}

void gc_heap::trim_free_spaces_indices ()
{
    trimmed_free_space_index = -1;
    size_t max_count = max_free_space_items - 1;
    size_t count = 0;
    int i = 0;
    for (i = (MAX_NUM_BUCKETS - 1); i >= 0; i--)
    {
        count += ordered_free_space_indices[i];

        if (count >= max_count)
        {
            break;
        }
    }

    ptrdiff_t extra_free_space_items = count - max_count;

    if (extra_free_space_items > 0)
    {
        ordered_free_space_indices[i] -= extra_free_space_items;
        free_space_items = max_count;
        trimmed_free_space_index = i;
    }
    else
    {
        free_space_items = count;
    }

    if (i == -1)
    {
        i = 0;
    }

    free_space_buckets = MAX_NUM_BUCKETS - i;

    for (--i; i >= 0; i--)
    {
        ordered_free_space_indices[i] = 0;
    }

    memcpy (saved_ordered_free_space_indices,
            ordered_free_space_indices,
            sizeof(ordered_free_space_indices));
}

// We fit as many plugs as we can and update the number of plugs left and the number
// of free spaces left.
BOOL gc_heap::can_fit_in_spaces_p (size_t* ordered_blocks, int small_index, size_t* ordered_spaces, int big_index)
{
    assert (small_index <= big_index);
    assert (big_index < MAX_NUM_BUCKETS);

    size_t small_blocks = ordered_blocks[small_index];

    if (small_blocks == 0)
    {
        return TRUE;
    }

    size_t big_spaces = ordered_spaces[big_index];

    if (big_spaces == 0)
    {
        return FALSE;
    }

    dprintf (SEG_REUSE_LOG_1, ("[%d]Fitting %zu 2^%d plugs into %zu 2^%d free spaces",
        heap_number,
        small_blocks, (small_index + MIN_INDEX_POWER2),
        big_spaces, (big_index + MIN_INDEX_POWER2)));

    size_t big_to_small = big_spaces << (big_index - small_index);

    ptrdiff_t extra_small_spaces = big_to_small - small_blocks;
    dprintf (SEG_REUSE_LOG_1, ("[%d]%zu 2^%d spaces can fit %zu 2^%d blocks",
        heap_number,
        big_spaces, (big_index + MIN_INDEX_POWER2), big_to_small, (small_index + MIN_INDEX_POWER2)));
    BOOL can_fit = (extra_small_spaces >= 0);

    if (can_fit)
    {
        dprintf (SEG_REUSE_LOG_1, ("[%d]Can fit with %zd 2^%d extras blocks",
            heap_number,
            extra_small_spaces, (small_index + MIN_INDEX_POWER2)));
    }

    int i = 0;

    dprintf (SEG_REUSE_LOG_1, ("[%d]Setting # of 2^%d spaces to 0", heap_number, (big_index + MIN_INDEX_POWER2)));
    ordered_spaces[big_index] = 0;
    if (extra_small_spaces > 0)
    {
        dprintf (SEG_REUSE_LOG_1, ("[%d]Setting # of 2^%d blocks to 0", heap_number, (small_index + MIN_INDEX_POWER2)));
        ordered_blocks[small_index] = 0;
        for (i = small_index; i < big_index; i++)
        {
            if (extra_small_spaces & 1)
            {
                dprintf (SEG_REUSE_LOG_1, ("[%d]Increasing # of 2^%d spaces from %zu to %zu",
                    heap_number,
                    (i + MIN_INDEX_POWER2), ordered_spaces[i], (ordered_spaces[i] + 1)));
                ordered_spaces[i] += 1;
            }
            extra_small_spaces >>= 1;
        }

        dprintf (SEG_REUSE_LOG_1, ("[%d]Finally increasing # of 2^%d spaces from %zu to %zu",
            heap_number,
            (i + MIN_INDEX_POWER2), ordered_spaces[i], (ordered_spaces[i] + extra_small_spaces)));
        ordered_spaces[i] += extra_small_spaces;
    }
    else
    {
        dprintf (SEG_REUSE_LOG_1, ("[%d]Decreasing # of 2^%d blocks from %zu to %zu",
            heap_number,
            (small_index + MIN_INDEX_POWER2),
            ordered_blocks[small_index],
            (ordered_blocks[small_index] - big_to_small)));
        ordered_blocks[small_index] -= big_to_small;
    }

#ifdef SEG_REUSE_STATS
    size_t temp;
    dprintf (SEG_REUSE_LOG_1, ("[%d]Plugs became:", heap_number));
    dump_buckets (ordered_blocks, MAX_NUM_BUCKETS, &temp);

    dprintf (SEG_REUSE_LOG_1, ("[%d]Free spaces became:", heap_number));
    dump_buckets (ordered_spaces, MAX_NUM_BUCKETS, &temp);
#endif //SEG_REUSE_STATS

    return can_fit;
}

// space_index gets updated to the biggest available space index.
BOOL gc_heap::can_fit_blocks_p (size_t* ordered_blocks, int block_index, size_t* ordered_spaces, int* space_index)
{
    assert (*space_index >= block_index);

    while (!can_fit_in_spaces_p (ordered_blocks, block_index, ordered_spaces, *space_index))
    {
        (*space_index)--;
        if (*space_index < block_index)
        {
            return FALSE;
        }
    }

    return TRUE;
}

BOOL gc_heap::can_fit_all_blocks_p (size_t* ordered_blocks, size_t* ordered_spaces, int count)
{
#ifdef FEATURE_STRUCTALIGN
    // BARTOKTODO (4841): reenable when can_fit_in_spaces_p takes alignment requirements into account
    return FALSE;
#endif // FEATURE_STRUCTALIGN
    int space_index = count - 1;
    for (int block_index = (count - 1); block_index >= 0; block_index--)
    {
        if (!can_fit_blocks_p (ordered_blocks, block_index, ordered_spaces, &space_index))
        {
            return FALSE;
        }
    }

    return TRUE;
}

void gc_heap::build_ordered_free_spaces (heap_segment* seg)
{
    assert (bestfit_seg);

    //bestfit_seg->add_buckets (MAX_NUM_BUCKETS - free_space_buckets + MIN_INDEX_POWER2,
    //                    ordered_free_space_indices + (MAX_NUM_BUCKETS - free_space_buckets),
    //                    free_space_buckets,
    //                    free_space_items);

    bestfit_seg->add_buckets (MIN_INDEX_POWER2,
                        ordered_free_space_indices,
                        MAX_NUM_BUCKETS,
                        free_space_items);

    assert (settings.condemned_generation == max_generation);

    uint8_t* first_address = heap_segment_mem (seg);
    uint8_t* end_address   = heap_segment_reserved (seg);
    //look through the pinned plugs for relevant ones.
    //Look for the right pinned plug to start from.
    reset_pinned_queue_bos();
    mark* m = 0;

    // See comment in can_expand_into_p why we need this size.
    size_t eph_gen_starts = eph_gen_starts_size + Align (min_obj_size);
    BOOL has_fit_gen_starts = FALSE;

    while (!pinned_plug_que_empty_p())
    {
        m = oldest_pin();
        if ((pinned_plug (m) >= first_address) &&
            (pinned_plug (m) < end_address) &&
            (pinned_len (m) >= eph_gen_starts))
        {

            assert ((pinned_plug (m) - pinned_len (m)) == bestfit_first_pin);
            break;
        }
        else
        {
            deque_pinned_plug();
        }
    }

    if (!pinned_plug_que_empty_p())
    {
        bestfit_seg->add ((void*)m, TRUE, TRUE);
        deque_pinned_plug();
        m = oldest_pin();
        has_fit_gen_starts = TRUE;
    }

    while (!pinned_plug_que_empty_p() &&
            ((pinned_plug (m) >= first_address) && (pinned_plug (m) < end_address)))
    {
        bestfit_seg->add ((void*)m, TRUE, FALSE);
        deque_pinned_plug();
        m = oldest_pin();
    }

    if (commit_end_of_seg)
    {
        if (!has_fit_gen_starts)
        {
            assert (bestfit_first_pin == heap_segment_plan_allocated (seg));
        }
        bestfit_seg->add ((void*)seg, FALSE, (!has_fit_gen_starts));
    }

#ifdef _DEBUG
    bestfit_seg->check();
#endif //_DEBUG
}

BOOL gc_heap::try_best_fit (BOOL end_of_segment_p)
{
    if (!end_of_segment_p)
    {
        trim_free_spaces_indices ();
    }

    BOOL can_bestfit = can_fit_all_blocks_p (ordered_plug_indices,
                                             ordered_free_space_indices,
                                             MAX_NUM_BUCKETS);

    return can_bestfit;
}

BOOL gc_heap::best_fit (size_t free_space,
                        size_t largest_free_space,
                        size_t additional_space,
                        BOOL* use_additional_space)
{
    dprintf (SEG_REUSE_LOG_0, ("gen%d: trying best fit mechanism", settings.condemned_generation));

    assert (!additional_space || (additional_space && use_additional_space));
    if (use_additional_space)
    {
        *use_additional_space = FALSE;
    }

    if (ordered_plug_indices_init == FALSE)
    {
        total_ephemeral_plugs = 0;
        build_ordered_plug_indices();
        ordered_plug_indices_init = TRUE;
    }
    else
    {
        memcpy (ordered_plug_indices, saved_ordered_plug_indices, sizeof(ordered_plug_indices));
    }

    if (total_ephemeral_plugs == END_SPACE_AFTER_GC_FL)
    {
        dprintf (SEG_REUSE_LOG_0, ("No ephemeral plugs to realloc, done"));
        size_t empty_eph = (END_SPACE_AFTER_GC_FL + (Align (min_obj_size)) * (max_generation + 1));
        BOOL can_fit_empty_eph = (largest_free_space >= empty_eph);
        if (!can_fit_empty_eph)
        {
            can_fit_empty_eph = (additional_space >= empty_eph);

            if (can_fit_empty_eph)
            {
                *use_additional_space = TRUE;
            }
        }

        return can_fit_empty_eph;
    }

    if ((total_ephemeral_plugs + approximate_new_allocation()) >= (free_space + additional_space))
    {
        dprintf (SEG_REUSE_LOG_0, ("We won't have enough free space left in this segment after fitting, done"));
        return FALSE;
    }

    if ((free_space + additional_space) == 0)
    {
        dprintf (SEG_REUSE_LOG_0, ("No free space in this segment, done"));
        return FALSE;
    }

#ifdef SEG_REUSE_STATS
    dprintf (SEG_REUSE_LOG_0, ("Free spaces:"));
    size_t total_free_space_power2 = 0;
    size_t total_free_space_items =
        dump_buckets (ordered_free_space_indices,
                      MAX_NUM_BUCKETS,
                      &total_free_space_power2);
    dprintf (SEG_REUSE_LOG_0, ("currently max free spaces is %zd", max_free_space_items));

    dprintf (SEG_REUSE_LOG_0, ("Ephemeral plugs: 0x%zx, free space: 0x%zx (rounded down to 0x%zx (%zd%%)), additional free_space: 0x%zx",
                total_ephemeral_plugs,
                free_space,
                total_free_space_power2,
                (free_space ? (total_free_space_power2 * 100 / free_space) : 0),
                additional_space));

    size_t saved_all_free_space_indices[MAX_NUM_BUCKETS];
    memcpy (saved_all_free_space_indices,
            ordered_free_space_indices,
            sizeof(saved_all_free_space_indices));

#endif // SEG_REUSE_STATS

    if (total_ephemeral_plugs > (free_space + additional_space))
    {
        return FALSE;
    }

    use_bestfit = try_best_fit(FALSE);

    if (!use_bestfit && additional_space)
    {
        int relative_free_space_index = relative_index_power2_free_space (round_down_power2 (additional_space));

        if (relative_free_space_index != -1)
        {
            int relative_plug_index = 0;
            size_t plugs_to_fit = 0;

            for (relative_plug_index = (MAX_NUM_BUCKETS - 1); relative_plug_index >= 0; relative_plug_index--)
            {
                plugs_to_fit = ordered_plug_indices[relative_plug_index];
                if (plugs_to_fit != 0)
                {
                    break;
                }
            }

            if ((relative_plug_index > relative_free_space_index) ||
                ((relative_plug_index == relative_free_space_index) &&
                (plugs_to_fit > 1)))
            {
#ifdef SEG_REUSE_STATS
                dprintf (SEG_REUSE_LOG_0, ("additional space is 2^%d but we stopped at %d 2^%d plug(s)",
                            (relative_free_space_index + MIN_INDEX_POWER2),
                            plugs_to_fit,
                            (relative_plug_index + MIN_INDEX_POWER2)));
#endif // SEG_REUSE_STATS
                goto adjust;
            }

            dprintf (SEG_REUSE_LOG_0, ("Adding end of segment (2^%d)", (relative_free_space_index + MIN_INDEX_POWER2)));
            ordered_free_space_indices[relative_free_space_index]++;
            use_bestfit = try_best_fit(TRUE);
            if (use_bestfit)
            {
                free_space_items++;
                // Since we might've trimmed away some of the free spaces we had, we should see
                // if we really need to use end of seg space - if it's the same or smaller than
                // the largest space we trimmed we can just add that one back instead of
                // using end of seg.
                if (relative_free_space_index > trimmed_free_space_index)
                {
                    *use_additional_space = TRUE;
                }
                else
                {
                    // If the addition space is <= than the last trimmed space, we
                    // should just use that last trimmed space instead.
                    saved_ordered_free_space_indices[trimmed_free_space_index]++;
                }
            }
        }
    }

adjust:

    if (!use_bestfit)
    {
        dprintf (SEG_REUSE_LOG_0, ("couldn't fit..."));

#ifdef SEG_REUSE_STATS
        size_t saved_max = max_free_space_items;
        BOOL temp_bestfit = FALSE;

        dprintf (SEG_REUSE_LOG_0, ("----Starting experiment process----"));
        dprintf (SEG_REUSE_LOG_0, ("----Couldn't fit with max free items %zd", max_free_space_items));

        // TODO: need to take the end of segment into consideration.
        while (max_free_space_items <= total_free_space_items)
        {
            max_free_space_items += max_free_space_items / 2;
            dprintf (SEG_REUSE_LOG_0, ("----Temporarily increasing max free spaces to %zd", max_free_space_items));
            memcpy (ordered_free_space_indices,
                    saved_all_free_space_indices,
                    sizeof(ordered_free_space_indices));
            if (try_best_fit(FALSE))
            {
                temp_bestfit = TRUE;
                break;
            }
        }

        if (temp_bestfit)
        {
            dprintf (SEG_REUSE_LOG_0, ("----With %zd max free spaces we could fit", max_free_space_items));
        }
        else
        {
            dprintf (SEG_REUSE_LOG_0, ("----Tried all free spaces and still couldn't fit, lost too much space"));
        }

        dprintf (SEG_REUSE_LOG_0, ("----Restoring max free spaces to %zd", saved_max));
        max_free_space_items = saved_max;
#endif // SEG_REUSE_STATS
        if (free_space_items)
        {
            max_free_space_items = min ((size_t)MAX_NUM_FREE_SPACES, free_space_items * 2);
            max_free_space_items = max (max_free_space_items, (size_t)MIN_NUM_FREE_SPACES);
        }
        else
        {
            max_free_space_items = MAX_NUM_FREE_SPACES;
        }
    }

    dprintf (SEG_REUSE_LOG_0, ("Adjusted number of max free spaces to %zd", max_free_space_items));
    dprintf (SEG_REUSE_LOG_0, ("------End of best fitting process------\n"));

    return use_bestfit;
}

BOOL gc_heap::process_free_space (heap_segment* seg,
                                  size_t free_space,
                                  size_t min_free_size,
                                  size_t min_cont_size,
                                  size_t* total_free_space,
                                  size_t* largest_free_space)
{
    *total_free_space += free_space;
    *largest_free_space = max (*largest_free_space, free_space);

#ifdef SIMPLE_DPRINTF
    dprintf (SEG_REUSE_LOG_1, ("free space len: %zx, total free space: %zx, largest free space: %zx",
                free_space, *total_free_space, *largest_free_space));
#endif //SIMPLE_DPRINTF

    if ((*total_free_space >= min_free_size) && (*largest_free_space >= min_cont_size))
    {
#ifdef SIMPLE_DPRINTF
        dprintf (SEG_REUSE_LOG_0, ("(gen%d)total free: %zx(min: %zx), largest free: %zx(min: %zx). Found segment %zx to reuse without bestfit",
            settings.condemned_generation,
            *total_free_space, min_free_size, *largest_free_space, min_cont_size,
            (size_t)seg));
#else
        UNREFERENCED_PARAMETER(seg);
#endif //SIMPLE_DPRINTF
        return TRUE;
    }

    int free_space_index = relative_index_power2_free_space (round_down_power2 (free_space));
    if (free_space_index != -1)
    {
        ordered_free_space_indices[free_space_index]++;
    }
    return FALSE;
}

BOOL gc_heap::can_expand_into_p (heap_segment* seg, size_t min_free_size, size_t min_cont_size,
                                 allocator* gen_allocator)
{
    min_cont_size += END_SPACE_AFTER_GC;
    use_bestfit = FALSE;
    commit_end_of_seg = FALSE;
    bestfit_first_pin = 0;
    uint8_t* first_address = heap_segment_mem (seg);
    uint8_t* end_address   = heap_segment_reserved (seg);
    size_t end_extra_space = end_space_after_gc();

    if ((heap_segment_reserved (seg) - end_extra_space) <= heap_segment_plan_allocated (seg))
    {
        dprintf (SEG_REUSE_LOG_0, ("can_expand_into_p: can't use segment [%p %p, has less than %zu bytes at the end",
                                   first_address, end_address, end_extra_space));
        return FALSE;
    }

    end_address -= end_extra_space;

    dprintf (SEG_REUSE_LOG_0, ("can_expand_into_p(gen%d): min free: %zx, min continuous: %zx",
        settings.condemned_generation, min_free_size, min_cont_size));
    size_t eph_gen_starts = eph_gen_starts_size;

    if (settings.condemned_generation == max_generation)
    {
        size_t free_space = 0;
        size_t largest_free_space = free_space;
        dprintf (SEG_REUSE_LOG_0, ("can_expand_into_p: gen2: testing segment [%p %p", first_address, end_address));
        //Look through the pinned plugs for relevant ones and Look for the right pinned plug to start from.
        //We are going to allocate the generation starts in the 1st free space,
        //so start from the first free space that's big enough for gen starts and a min object size.
        // If we see a free space that is >= gen starts but < gen starts + min obj size we just don't use it -
        // we could use it by allocating the last generation start a bit bigger but
        // the complexity isn't worth the effort (those plugs are from gen2
        // already anyway).
        reset_pinned_queue_bos();
        mark* m = 0;
        BOOL has_fit_gen_starts = FALSE;

        init_ordered_free_space_indices ();
        while (!pinned_plug_que_empty_p())
        {
            m = oldest_pin();
            if ((pinned_plug (m) >= first_address) &&
                (pinned_plug (m) < end_address) &&
                (pinned_len (m) >= (eph_gen_starts + Align (min_obj_size))))
            {
                break;
            }
            else
            {
                deque_pinned_plug();
            }
        }

        if (!pinned_plug_que_empty_p())
        {
            bestfit_first_pin = pinned_plug (m) - pinned_len (m);

            if (process_free_space (seg,
                                    pinned_len (m) - eph_gen_starts,
                                    min_free_size, min_cont_size,
                                    &free_space, &largest_free_space))
            {
                return TRUE;
            }

            deque_pinned_plug();
            m = oldest_pin();
            has_fit_gen_starts = TRUE;
        }

        dprintf (3, ("first pin is %p", pinned_plug (m)));

        //tally up free space
        while (!pinned_plug_que_empty_p() &&
               ((pinned_plug (m) >= first_address) && (pinned_plug (m) < end_address)))
        {
            dprintf (3, ("looking at pin %p", pinned_plug (m)));
            if (process_free_space (seg,
                                    pinned_len (m),
                                    min_free_size, min_cont_size,
                                    &free_space, &largest_free_space))
            {
                return TRUE;
            }

            deque_pinned_plug();
            m = oldest_pin();
        }

        //try to find space at the end of the segment.
        size_t end_space = (end_address - heap_segment_plan_allocated (seg));
        size_t additional_space = ((min_free_size > free_space) ? (min_free_size - free_space) : 0);
        dprintf (SEG_REUSE_LOG_0, ("end space: %zx; additional: %zx", end_space, additional_space));
        if (end_space >= additional_space)
        {
            BOOL can_fit = TRUE;
            commit_end_of_seg = TRUE;

            if (largest_free_space < min_cont_size)
            {
                if (end_space >= min_cont_size)
                {
                    additional_space = max (min_cont_size, additional_space);
                    dprintf (SEG_REUSE_LOG_0, ("(gen2)Found segment %p to reuse without bestfit, with committing end of seg for eph",
                        seg));
                }
                else
                {
                    if (settings.concurrent)
                    {
                        can_fit = FALSE;
                        commit_end_of_seg = FALSE;
                    }
                    else
                    {
                        size_t additional_space_bestfit = additional_space;
                        if (!has_fit_gen_starts)
                        {
                            if (additional_space_bestfit < (eph_gen_starts + Align (min_obj_size)))
                            {
                                dprintf (SEG_REUSE_LOG_0, ("(gen2)Couldn't fit, gen starts not allocated yet and end space is too small: %zd",
                                        additional_space_bestfit));
                                return FALSE;
                            }

                            bestfit_first_pin = heap_segment_plan_allocated (seg);
                            additional_space_bestfit -= eph_gen_starts;
                        }

                        can_fit = best_fit (free_space,
                                            largest_free_space,
                                            additional_space_bestfit,
                                            &commit_end_of_seg);

                        if (can_fit)
                        {
                            dprintf (SEG_REUSE_LOG_0, ("(gen2)Found segment %p to reuse with bestfit, %s committing end of seg",
                                seg, (commit_end_of_seg ? "with" : "without")));
                        }
                        else
                        {
                            dprintf (SEG_REUSE_LOG_0, ("(gen2)Couldn't fit, total free space is %zx", (free_space + end_space)));
                        }
                    }
                }
            }
            else
            {
                dprintf (SEG_REUSE_LOG_0, ("(gen2)Found segment %p to reuse without bestfit, with committing end of seg", seg));
            }

            assert (additional_space <= end_space);
            if (commit_end_of_seg)
            {
                if (!grow_heap_segment (seg, heap_segment_plan_allocated (seg) + additional_space))
                {
                    dprintf (2, ("Couldn't commit end of segment?!"));
                    use_bestfit = FALSE;

                    return FALSE;
                }

                if (use_bestfit)
                {
                    // We increase the index here because growing heap segment could create a discrepency with
                    // the additional space we used (could be bigger).
                    size_t free_space_end_of_seg =
                        heap_segment_committed (seg) - heap_segment_plan_allocated (seg);
                    int relative_free_space_index = relative_index_power2_free_space (round_down_power2 (free_space_end_of_seg));
                    saved_ordered_free_space_indices[relative_free_space_index]++;
                }
            }

            if (use_bestfit)
            {
                memcpy (ordered_free_space_indices,
                        saved_ordered_free_space_indices,
                        sizeof(ordered_free_space_indices));
                max_free_space_items = max ((size_t)MIN_NUM_FREE_SPACES, free_space_items * 3 / 2);
                max_free_space_items = min ((size_t)MAX_NUM_FREE_SPACES, max_free_space_items);
                dprintf (SEG_REUSE_LOG_0, ("could fit! %zd free spaces, %zd max", free_space_items, max_free_space_items));
            }

            return can_fit;
        }

        dprintf (SEG_REUSE_LOG_0, ("(gen2)Couldn't fit, total free space is %zx", (free_space + end_space)));
        return FALSE;
    }
    else
    {
        assert (settings.condemned_generation == (max_generation-1));
        size_t free_space = (end_address - heap_segment_plan_allocated (seg));
        size_t largest_free_space = free_space;
        dprintf (SEG_REUSE_LOG_0, ("can_expand_into_p: gen1: testing segment [%p %p", first_address, end_address));
        //find the first free list in range of the current segment
        uint8_t* free_list = 0;
        unsigned int a_l_idx = gen_allocator->first_suitable_bucket(eph_gen_starts);
        for (; a_l_idx < gen_allocator->number_of_buckets(); a_l_idx++)
        {
            free_list = gen_allocator->alloc_list_head_of (a_l_idx);
            while (free_list)
            {
                if ((free_list >= first_address) &&
                    (free_list < end_address) &&
                    (unused_array_size (free_list) >= eph_gen_starts))
                {
                    goto next;
                }
                else
                {
                    free_list = free_list_slot (free_list);
                }
            }
        }
next:
        if (free_list)
        {
            init_ordered_free_space_indices ();
            if (process_free_space (seg,
                                    unused_array_size (free_list) - eph_gen_starts + Align (min_obj_size),
                                    min_free_size, min_cont_size,
                                    &free_space, &largest_free_space))
            {
                return TRUE;
            }

            free_list = free_list_slot (free_list);
        }
        else
        {
            dprintf (SEG_REUSE_LOG_0, ("(gen1)Couldn't fit, no free list"));
            return FALSE;
        }

       //tally up free space
        while (1)
        {
            while (free_list)
            {
                if ((free_list >= first_address) && (free_list < end_address) &&
                    process_free_space (seg,
                                        unused_array_size (free_list),
                                        min_free_size, min_cont_size,
                                        &free_space, &largest_free_space))
                {
                    return TRUE;
                }

                free_list = free_list_slot (free_list);
            }
            a_l_idx++;
            if (a_l_idx < gen_allocator->number_of_buckets())
            {
                free_list = gen_allocator->alloc_list_head_of (a_l_idx);
            }
            else
                break;
        }

        dprintf (SEG_REUSE_LOG_0, ("(gen1)Couldn't fit, total free space is %zx", free_space));
        return FALSE;

        /*
        BOOL can_fit = best_fit (free_space, 0, NULL);
        if (can_fit)
        {
            dprintf (SEG_REUSE_LOG_0, ("(gen1)Found segment %zx to reuse with bestfit", seg));
        }
        else
        {
            dprintf (SEG_REUSE_LOG_0, ("(gen1)Couldn't fit, total free space is %zx", free_space));
        }

        return can_fit;
        */
    }
}

void gc_heap::realloc_plug (size_t last_plug_size, uint8_t*& last_plug,
                            generation* gen, uint8_t* start_address,
                            unsigned int& active_new_gen_number,
                            uint8_t*& last_pinned_gap, BOOL& leftp,
                            BOOL shortened_p
#ifdef SHORT_PLUGS
                            , mark* pinned_plug_entry
#endif //SHORT_PLUGS
                            )
{
    // detect generation boundaries
    // make sure that active_new_gen_number is not the youngest generation.
    // because the generation_limit wouldn't return the right thing in this case.
    if (!use_bestfit)
    {
        if ((active_new_gen_number > 1) &&
            (last_plug >= generation_limit (active_new_gen_number)))
        {
            assert (last_plug >= start_address);
            active_new_gen_number--;
            realloc_plan_generation_start (generation_of (active_new_gen_number), gen);
            assert (generation_plan_allocation_start (generation_of (active_new_gen_number)));
            leftp = FALSE;
        }
    }

    // detect pinned plugs
    if (!pinned_plug_que_empty_p() && (last_plug == pinned_plug (oldest_pin())))
    {
        size_t  entry = deque_pinned_plug();
        mark*  m = pinned_plug_of (entry);

        size_t saved_pinned_len = pinned_len(m);
        pinned_len(m) = last_plug - last_pinned_gap;
        //dprintf (3,("Adjusting pinned gap: [%zx, %zx[", (size_t)last_pinned_gap, (size_t)last_plug));

        if (m->has_post_plug_info())
        {
            last_plug_size += sizeof (gap_reloc_pair);
            dprintf (3, ("ra pinned %p was shortened, adjusting plug size to %zx", last_plug, last_plug_size))
        }

        last_pinned_gap = last_plug + last_plug_size;
        dprintf (3, ("ra found pin %p, len: %zx->%zx, last_p: %p, last_p_size: %zx",
            pinned_plug (m), saved_pinned_len, pinned_len (m), last_plug, last_plug_size));
        leftp = FALSE;

        //we are creating a generation fault. set the cards.
        {
            size_t end_card = card_of (align_on_card (last_plug + last_plug_size));
            size_t card = card_of (last_plug);
            while (card != end_card)
            {
                set_card (card);
                card++;
            }
        }
    }
    else if (last_plug >= start_address)
    {
#ifdef FEATURE_STRUCTALIGN
        int requiredAlignment;
        ptrdiff_t pad;
        node_aligninfo (last_plug, requiredAlignment, pad);

        // from how we previously aligned the plug's destination address,
        // compute the actual alignment offset.
        uint8_t* reloc_plug = last_plug + node_relocation_distance (last_plug);
        ptrdiff_t alignmentOffset = ComputeStructAlignPad(reloc_plug, requiredAlignment, 0);
        if (!alignmentOffset)
        {
            // allocate_in_expanded_heap doesn't expect alignmentOffset to be zero.
            alignmentOffset = requiredAlignment;
        }

        //clear the alignment info because we are reallocating
        clear_node_aligninfo (last_plug);
#else // FEATURE_STRUCTALIGN
        //clear the realignment flag because we are reallocating
        clear_node_realigned (last_plug);
#endif // FEATURE_STRUCTALIGN
        BOOL adjacentp = FALSE;
        BOOL set_padding_on_saved_p = FALSE;

        if (shortened_p)
        {
            last_plug_size += sizeof (gap_reloc_pair);

#ifdef SHORT_PLUGS
            assert (pinned_plug_entry != NULL);
            if (last_plug_size <= sizeof (plug_and_gap))
            {
                set_padding_on_saved_p = TRUE;
            }
#endif //SHORT_PLUGS

            dprintf (3, ("ra plug %p was shortened, adjusting plug size to %zx", last_plug, last_plug_size))
        }

#ifdef SHORT_PLUGS
        clear_padding_in_expand (last_plug, set_padding_on_saved_p, pinned_plug_entry);
#endif //SHORT_PLUGS

        uint8_t* new_address = allocate_in_expanded_heap(gen, last_plug_size, adjacentp, last_plug,
#ifdef SHORT_PLUGS
                                     set_padding_on_saved_p,
                                     pinned_plug_entry,
#endif //SHORT_PLUGS
                                     TRUE, active_new_gen_number REQD_ALIGN_AND_OFFSET_ARG);

        dprintf (3, ("ra NA: [%p, %p[: %zx", new_address, (new_address + last_plug_size), last_plug_size));
        assert (new_address);
        set_node_relocation_distance (last_plug, new_address - last_plug);
#ifdef FEATURE_STRUCTALIGN
        if (leftp && node_alignpad (last_plug) == 0)
#else // FEATURE_STRUCTALIGN
        if (leftp && !node_realigned (last_plug))
#endif // FEATURE_STRUCTALIGN
        {
            // TODO - temporarily disable L optimization because of a bug in it.
            //set_node_left (last_plug);
        }
        dprintf (3,(" Re-allocating %zx->%zx len %zd", (size_t)last_plug, (size_t)new_address, last_plug_size));
        leftp = adjacentp;
    }
}

void gc_heap::realloc_in_brick (uint8_t* tree, uint8_t*& last_plug,
                                uint8_t* start_address,
                                generation* gen,
                                unsigned int& active_new_gen_number,
                                uint8_t*& last_pinned_gap, BOOL& leftp)
{
    assert (tree != NULL);
    int   left_node = node_left_child (tree);
    int   right_node = node_right_child (tree);

    dprintf (3, ("ra: tree: %p, last_pin_gap: %p, last_p: %p, L: %d, R: %d",
        tree, last_pinned_gap, last_plug, left_node, right_node));

    if (left_node)
    {
        dprintf (3, ("LN: realloc %p(%p)", (tree + left_node), last_plug));
        realloc_in_brick ((tree + left_node), last_plug, start_address,
                          gen, active_new_gen_number, last_pinned_gap,
                          leftp);
    }

    if (last_plug != 0)
    {
        uint8_t*  plug = tree;

        BOOL has_pre_plug_info_p = FALSE;
        BOOL has_post_plug_info_p = FALSE;
        mark* pinned_plug_entry = get_next_pinned_entry (tree,
                                                         &has_pre_plug_info_p,
                                                         &has_post_plug_info_p,
                                                         FALSE);

        // We only care about the pre plug info 'cause that's what decides if the last plug is shortened.
        // The pinned plugs are handled in realloc_plug.
        size_t gap_size = node_gap_size (plug);
        uint8_t*   gap = (plug - gap_size);
        uint8_t*  last_plug_end = gap;
        size_t  last_plug_size = (last_plug_end - last_plug);
        // Cannot assert this - a plug could be less than that due to the shortened ones.
        //assert (last_plug_size >= Align (min_obj_size));
        dprintf (3, ("ra: plug %p, gap size: %zd, last_pin_gap: %p, last_p: %p, last_p_end: %p, shortened: %d",
            plug, gap_size, last_pinned_gap, last_plug, last_plug_end, (has_pre_plug_info_p ? 1 : 0)));
        realloc_plug (last_plug_size, last_plug, gen, start_address,
                      active_new_gen_number, last_pinned_gap,
                      leftp, has_pre_plug_info_p
#ifdef SHORT_PLUGS
                      , pinned_plug_entry
#endif //SHORT_PLUGS
                      );
    }

    last_plug = tree;

    if (right_node)
    {
        dprintf (3, ("RN: realloc %p(%p)", (tree + right_node), last_plug));
        realloc_in_brick ((tree + right_node), last_plug, start_address,
                          gen, active_new_gen_number, last_pinned_gap,
                          leftp);
    }
}

void
gc_heap::realloc_plugs (generation* consing_gen, heap_segment* seg,
                        uint8_t* start_address, uint8_t* end_address,
                        unsigned active_new_gen_number)
{
    dprintf (3, ("--- Reallocing ---"));

    if (use_bestfit)
    {
        //make sure that every generation has a planned allocation start
        int  gen_number = max_generation - 1;
        while (gen_number >= 0)
        {
            generation* gen = generation_of (gen_number);
            if (0 == generation_plan_allocation_start (gen))
            {
                generation_plan_allocation_start (gen) =
                    bestfit_first_pin + (max_generation - gen_number - 1) * Align (min_obj_size);
                generation_plan_allocation_start_size (gen) = Align (min_obj_size);
                assert (generation_plan_allocation_start (gen));
            }
            gen_number--;
        }
    }

    uint8_t* first_address = start_address;
    //Look for the right pinned plug to start from.
    reset_pinned_queue_bos();
    uint8_t* planned_ephemeral_seg_end = heap_segment_plan_allocated (seg);
    while (!pinned_plug_que_empty_p())
    {
        mark* m = oldest_pin();
        if ((pinned_plug (m) >= planned_ephemeral_seg_end) && (pinned_plug (m) < end_address))
        {
            if (pinned_plug (m) < first_address)
            {
                first_address = pinned_plug (m);
            }
            break;
        }
        else
            deque_pinned_plug();
    }

    size_t  current_brick = brick_of (first_address);
    size_t  end_brick = brick_of (end_address-1);
    uint8_t*  last_plug = 0;

    uint8_t* last_pinned_gap = heap_segment_plan_allocated (seg);
    BOOL leftp = FALSE;

    dprintf (3, ("start addr: %p, first addr: %p, current oldest pin: %p",
        start_address, first_address, pinned_plug (oldest_pin())));

    while (current_brick <= end_brick)
    {
        int   brick_entry =  brick_table [ current_brick ];
        if (brick_entry >= 0)
        {
            realloc_in_brick ((brick_address (current_brick) + brick_entry - 1),
                              last_plug, start_address, consing_gen,
                              active_new_gen_number, last_pinned_gap,
                              leftp);
        }
        current_brick++;
    }

    if (last_plug != 0)
    {
        realloc_plug (end_address - last_plug, last_plug, consing_gen,
                      start_address,
                      active_new_gen_number, last_pinned_gap,
                      leftp, FALSE
#ifdef SHORT_PLUGS
                      , NULL
#endif //SHORT_PLUGS
                      );
    }

    //Fix the old segment allocated size
    assert (last_pinned_gap >= heap_segment_mem (seg));
    assert (last_pinned_gap <= heap_segment_committed (seg));
    heap_segment_plan_allocated (seg) = last_pinned_gap;
}

void gc_heap::set_expand_in_full_gc (int condemned_gen_number)
{
    if (!should_expand_in_full_gc)
    {
        if ((condemned_gen_number != max_generation) &&
            (settings.pause_mode != pause_low_latency) &&
            (settings.pause_mode != pause_sustained_low_latency))
        {
            should_expand_in_full_gc = TRUE;
        }
    }
}

void gc_heap::save_ephemeral_generation_starts()
{
    for (int ephemeral_generation = 0; ephemeral_generation < max_generation; ephemeral_generation++)
    {
        saved_ephemeral_plan_start[ephemeral_generation] =
            generation_plan_allocation_start (generation_of (ephemeral_generation));
        saved_ephemeral_plan_start_size[ephemeral_generation] =
            generation_plan_allocation_start_size (generation_of (ephemeral_generation));
    }
}

generation* gc_heap::expand_heap (int condemned_generation,
                                  generation* consing_gen,
                                  heap_segment* new_heap_segment)
{
#ifndef _DEBUG
    UNREFERENCED_PARAMETER(condemned_generation);
#endif //!_DEBUG
    assert (condemned_generation >= (max_generation -1));
    unsigned int active_new_gen_number = max_generation; //Set one too high to get generation gap
    uint8_t*  start_address = generation_limit (max_generation);
    uint8_t*  end_address = heap_segment_allocated (ephemeral_heap_segment);
    BOOL should_promote_ephemeral = FALSE;
    ptrdiff_t eph_size = total_ephemeral_size;
#ifdef BACKGROUND_GC
    dprintf(2,("%s: ---- Heap Expansion ----", get_str_gc_type()));
#endif //BACKGROUND_GC
    settings.heap_expansion = TRUE;

    //reset the elevation state for next time.
    dprintf (2, ("Elevation: elevation = el_none"));
    if (settings.should_lock_elevation && !expand_reused_seg_p())
        settings.should_lock_elevation = FALSE;

    heap_segment* new_seg = new_heap_segment;

    if (!new_seg)
        return consing_gen;

    //copy the card and brick tables
    if (g_gc_card_table!= card_table)
        copy_brick_card_table();

    BOOL new_segment_p = (heap_segment_next (new_seg) == 0);
    dprintf (2, ("new_segment_p %zx", (size_t)new_segment_p));

    assert (generation_plan_allocation_start (generation_of (max_generation-1)));
    assert (generation_plan_allocation_start (generation_of (max_generation-1)) >=
            heap_segment_mem (ephemeral_heap_segment));
    assert (generation_plan_allocation_start (generation_of (max_generation-1)) <=
            heap_segment_committed (ephemeral_heap_segment));

    assert (generation_plan_allocation_start (youngest_generation));
    assert (generation_plan_allocation_start (youngest_generation) <
            heap_segment_plan_allocated (ephemeral_heap_segment));

    if (settings.pause_mode == pause_no_gc)
    {
        // We don't reuse for no gc, so the size used on the new eph seg is eph_size.
        if ((size_t)(heap_segment_reserved (new_seg) - heap_segment_mem (new_seg)) < (eph_size + soh_allocation_no_gc))
            should_promote_ephemeral = TRUE;
    }
    else
    {
        if (!use_bestfit)
        {
            should_promote_ephemeral = dt_low_ephemeral_space_p (tuning_deciding_promote_ephemeral);
        }
    }

    if (should_promote_ephemeral)
    {
        ephemeral_promotion = TRUE;
        get_gc_data_per_heap()->set_mechanism (gc_heap_expand, expand_new_seg_ep);
        dprintf (2, ("promoting ephemeral"));
        save_ephemeral_generation_starts();

        // We also need to adjust free_obj_space (due to padding) here because now young gens' free_obj_space will
        // belong to gen2.
        generation* max_gen = generation_of (max_generation);
        for (int i = 1; i < max_generation; i++)
        {
            generation_free_obj_space (max_gen) +=
                generation_free_obj_space (generation_of (i));
            dprintf (2, ("[h%d] maxgen freeobj + %zd=%zd",
                heap_number, generation_free_obj_space (generation_of (i)),
                generation_free_obj_space (max_gen)));
        }

        // TODO: This is actually insufficient - if BACKGROUND_GC is not defined we'd need to commit more
        // in order to accommodate eph gen starts. Also in the no_gc we should make sure used
        // is updated correctly.
        heap_segment_used (new_seg) = heap_segment_committed (new_seg);
    }
    else
    {
        // commit the new ephemeral segment all at once if it is a new one.
        if ((eph_size > 0) && new_segment_p)
        {
#ifdef FEATURE_STRUCTALIGN
            // The destination may require a larger alignment padding than the source.
            // Assume the worst possible alignment padding.
            eph_size += ComputeStructAlignPad(heap_segment_mem (new_seg), MAX_STRUCTALIGN, OBJECT_ALIGNMENT_OFFSET);
#endif // FEATURE_STRUCTALIGN
#ifdef RESPECT_LARGE_ALIGNMENT
            //Since the generation start can be larger than min_obj_size
            //The alignment could be switched.
            eph_size += switch_alignment_size(FALSE);
#endif //RESPECT_LARGE_ALIGNMENT
            //Since the generation start can be larger than min_obj_size
            //Compare the alignment of the first object in gen1
            if (grow_heap_segment (new_seg, heap_segment_mem (new_seg) + eph_size) == 0)
            {
                fgm_result.set_fgm (fgm_commit_eph_segment, eph_size, FALSE);
                return consing_gen;
            }
            heap_segment_used (new_seg) = heap_segment_committed (new_seg);
        }

        //Fix the end of the old ephemeral heap segment
        heap_segment_plan_allocated (ephemeral_heap_segment) =
            generation_plan_allocation_start (generation_of (max_generation-1));

        dprintf (3, ("Old ephemeral allocated set to %zx",
                    (size_t)heap_segment_plan_allocated (ephemeral_heap_segment)));
    }

    if (new_segment_p)
    {
        // TODO - Is this really necessary? We should think about it.
        //initialize the first brick
        size_t first_brick = brick_of (heap_segment_mem (new_seg));
        set_brick (first_brick,
                heap_segment_mem (new_seg) - brick_address (first_brick));
    }

    //From this point on, we cannot run out of memory

    //reset the allocation of the consing generation back to the end of the
    //old ephemeral segment
    generation_allocation_limit (consing_gen) =
        heap_segment_plan_allocated (ephemeral_heap_segment);
    generation_allocation_pointer (consing_gen) = generation_allocation_limit (consing_gen);
    generation_allocation_segment (consing_gen) = ephemeral_heap_segment;

    //clear the generation gap for all of the ephemeral generations
    {
        int generation_num = max_generation-1;
        while (generation_num >= 0)
        {
            generation* gen = generation_of (generation_num);
            generation_plan_allocation_start (gen) = 0;
            generation_num--;
        }
    }

    heap_segment* old_seg = ephemeral_heap_segment;
    ephemeral_heap_segment = new_seg;

    //Note: the ephemeral segment shouldn't be threaded onto the segment chain
    //because the relocation and compact phases shouldn't see it

    // set the generation members used by allocate_in_expanded_heap
    // and switch to ephemeral generation
    consing_gen = ensure_ephemeral_heap_segment (consing_gen);

    if (!should_promote_ephemeral)
    {
        realloc_plugs (consing_gen, old_seg, start_address, end_address,
                    active_new_gen_number);
    }

    if (!use_bestfit)
    {
        repair_allocation_in_expanded_heap (consing_gen);
    }

    // assert that the generation gap for all of the ephemeral generations were allocated.
#ifdef _DEBUG
    {
        int generation_num = max_generation-1;
        while (generation_num >= 0)
        {
            generation* gen = generation_of (generation_num);
            assert (generation_plan_allocation_start (gen));
            generation_num--;
        }
    }
#endif // _DEBUG

    if (!new_segment_p)
    {
        dprintf (2, ("Demoting ephemeral segment"));
        //demote the entire segment.
        settings.demotion = TRUE;
        get_gc_data_per_heap()->set_mechanism_bit (gc_demotion_bit);
        demotion_low = heap_segment_mem (ephemeral_heap_segment);
        demotion_high = heap_segment_reserved (ephemeral_heap_segment);
    }
    else
    {
        demotion_low = MAX_PTR;
        demotion_high = 0;
#ifndef MULTIPLE_HEAPS
        settings.demotion = FALSE;
        get_gc_data_per_heap()->clear_mechanism_bit (gc_demotion_bit);
#endif //!MULTIPLE_HEAPS
    }

    if (!should_promote_ephemeral && new_segment_p)
    {
        assert ((ptrdiff_t)total_ephemeral_size <= eph_size);
    }

    if (heap_segment_mem (old_seg) == heap_segment_plan_allocated (old_seg))
    {
        // This is to catch when we accidently delete a segment that has pins.
        verify_no_pins (heap_segment_mem (old_seg), heap_segment_reserved (old_seg));
    }

    verify_no_pins (heap_segment_plan_allocated (old_seg), heap_segment_reserved(old_seg));

    dprintf(2,("---- End of Heap Expansion ----"));
    return consing_gen;
}

#endif //!USE_REGIONS

BOOL gc_heap::expand_reused_seg_p()
{
#ifdef USE_REGIONS
    return FALSE;
#else
    BOOL reused_seg = FALSE;
    int heap_expand_mechanism = gc_data_per_heap.get_mechanism (gc_heap_expand);
    if ((heap_expand_mechanism == expand_reuse_bestfit) ||
        (heap_expand_mechanism == expand_reuse_normal))
    {
        reused_seg = TRUE;
    }

    return reused_seg;
#endif //USE_REGIONS
}

void gc_heap::verify_no_pins (uint8_t* start, uint8_t* end)
{
#ifdef VERIFY_HEAP
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
    {
        BOOL contains_pinned_plugs = FALSE;
        size_t mi = 0;
        mark* m = 0;
        while (mi != mark_stack_tos)
        {
            m = pinned_plug_of (mi);
            if ((pinned_plug (m) >= start) && (pinned_plug (m) < end))
            {
                contains_pinned_plugs = TRUE;
                break;
            }
            else
                mi++;
        }

        if (contains_pinned_plugs)
        {
            FATAL_GC_ERROR();
        }
    }
#endif //VERIFY_HEAP
}

// REGIONS TODO: this can be merged with generation_size.
//returns the planned size of a generation (including free list element)
size_t gc_heap::generation_plan_size (int gen_number)
{
#ifdef USE_REGIONS
    size_t result = 0;
    heap_segment* seg = heap_segment_rw (generation_start_segment (generation_of (gen_number)));
    while (seg)
    {
        uint8_t* end = heap_segment_plan_allocated (seg);
        result += end - heap_segment_mem (seg);
        dprintf (REGIONS_LOG, ("h%d size + %zd (%p - %p) -> %zd",
            heap_number, (end - heap_segment_mem (seg)),
            heap_segment_mem (seg), end, result));
        seg = heap_segment_next (seg);
    }
    return result;
#else //USE_REGIONS
    if (0 == gen_number)
        return (size_t)max((heap_segment_plan_allocated (ephemeral_heap_segment) -
                    generation_plan_allocation_start (generation_of (gen_number))),
                   (ptrdiff_t)Align (min_obj_size));
    else
    {
        generation* gen = generation_of (gen_number);
        if (heap_segment_rw (generation_start_segment (gen)) == ephemeral_heap_segment)
            return (generation_plan_allocation_start (generation_of (gen_number - 1)) -
                    generation_plan_allocation_start (generation_of (gen_number)));
        else
        {
            size_t gensize = 0;
            heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

            _ASSERTE(seg != NULL);

            while (seg && (seg != ephemeral_heap_segment))
            {
                gensize += heap_segment_plan_allocated (seg) -
                           heap_segment_mem (seg);
                seg = heap_segment_next_rw (seg);
            }
            if (seg)
            {
                gensize += (generation_plan_allocation_start (generation_of (gen_number - 1)) -
                            heap_segment_mem (ephemeral_heap_segment));
            }
            return gensize;
        }
    }
#endif //USE_REGIONS
}

//returns the size of a generation (including free list element)
size_t gc_heap::generation_size (int gen_number)
{
#ifdef USE_REGIONS
    size_t result = 0;
    heap_segment* seg = heap_segment_rw (generation_start_segment (generation_of (gen_number)));
    while (seg)
    {
        uint8_t* end = heap_segment_allocated (seg);
        result += end - heap_segment_mem (seg);
        dprintf (2, ("h%d size + %zd (%p - %p) -> %zd",
            heap_number, (end - heap_segment_mem (seg)),
            heap_segment_mem (seg), end, result));
        seg = heap_segment_next (seg);
    }
    return result;
#else //USE_REGIONS
    if (0 == gen_number)
        return (size_t)max((heap_segment_allocated (ephemeral_heap_segment) -
                    generation_allocation_start (generation_of (gen_number))),
                   (ptrdiff_t)Align (min_obj_size));
    else
    {
        generation* gen = generation_of (gen_number);
        if (heap_segment_rw (generation_start_segment (gen)) == ephemeral_heap_segment)
            return (generation_allocation_start (generation_of (gen_number - 1)) -
                    generation_allocation_start (generation_of (gen_number)));
        else
        {
            size_t gensize = 0;
            heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

            _ASSERTE(seg != NULL);

            while (seg && (seg != ephemeral_heap_segment))
            {
                gensize += heap_segment_allocated (seg) -
                           heap_segment_mem (seg);
                seg = heap_segment_next_rw (seg);
            }
            if (seg)
            {
                gensize += (generation_allocation_start (generation_of (gen_number - 1)) -
                            heap_segment_mem (ephemeral_heap_segment));
            }

            return gensize;
        }
    }
#endif //USE_REGIONS
}

size_t  gc_heap::compute_in (int gen_number)
{
    assert (gen_number != 0);
    dynamic_data* dd = dynamic_data_of (gen_number);

    size_t in = generation_allocation_size (generation_of (gen_number));

#ifndef USE_REGIONS
    if (gen_number == max_generation && ephemeral_promotion)
    {
        in = 0;
        for (int i = 0; i <= max_generation; i++)
        {
            dynamic_data* dd = dynamic_data_of (i);
            in += dd_survived_size (dd);
            if (i != max_generation)
            {
                generation_condemned_allocated (generation_of (gen_number)) += dd_survived_size (dd);
            }
        }
    }
#endif //!USE_REGIONS

    dd_gc_new_allocation (dd) -= in;
    dd_new_allocation (dd) = dd_gc_new_allocation (dd);

    gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();
    gc_generation_data* gen_data = &(current_gc_data_per_heap->gen_data[gen_number]);
    gen_data->in = in;

    generation_allocation_size (generation_of (gen_number)) = 0;
    return in;
}

//This is meant to be called by decide_on_compacting.
size_t gc_heap::generation_fragmentation (generation* gen,
                                          generation* consing_gen,
                                          uint8_t* end)
{
    ptrdiff_t frag = 0;

#ifdef USE_REGIONS
    for (int gen_num = 0; gen_num <= gen->gen_num; gen_num++)
    {
        generation* gen = generation_of (gen_num);
        heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
        while (seg)
        {
            frag += (heap_segment_saved_allocated (seg) -
                 heap_segment_plan_allocated (seg));

            dprintf (3, ("h%d g%d adding seg plan frag: %p-%p=%zd -> %zd",
                heap_number, gen_num,
                heap_segment_saved_allocated (seg),
                heap_segment_plan_allocated (seg),
                (heap_segment_saved_allocated (seg) - heap_segment_plan_allocated (seg)),
                frag));

            seg = heap_segment_next_rw (seg);
        }
    }
#else //USE_REGIONS
    uint8_t* alloc = generation_allocation_pointer (consing_gen);
    // If the allocation pointer has reached the ephemeral segment
    // fine, otherwise the whole ephemeral segment is considered
    // fragmentation
    if (in_range_for_segment (alloc, ephemeral_heap_segment))
    {
        if (alloc <= heap_segment_allocated(ephemeral_heap_segment))
            frag = end - alloc;
        else
        {
            // case when no survivors, allocated set to beginning
            frag = 0;
        }
        dprintf (3, ("ephemeral frag: %zd", frag));
    }
    else
        frag = (heap_segment_allocated (ephemeral_heap_segment) -
                heap_segment_mem (ephemeral_heap_segment));
    heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

    _ASSERTE(seg != NULL);

    while (seg != ephemeral_heap_segment)
    {
        frag += (heap_segment_allocated (seg) -
                 heap_segment_plan_allocated (seg));
        dprintf (3, ("seg: %zx, frag: %zd", (size_t)seg,
                     (heap_segment_allocated (seg) -
                      heap_segment_plan_allocated (seg))));

        seg = heap_segment_next_rw (seg);
        assert (seg);
    }
#endif //USE_REGIONS

    dprintf (3, ("frag: %zd discounting pinned plugs", frag));
    //add the length of the dequeued plug free space
    size_t bos = 0;
    while (bos < mark_stack_bos)
    {
        frag += (pinned_len (pinned_plug_of (bos)));
        dprintf (3, ("adding pinned len %zd to frag ->%zd",
            pinned_len (pinned_plug_of (bos)), frag));
        bos++;
    }

    return frag;
}

// for SOH this returns the total sizes of the generation and its
// younger generation(s).
// for LOH this returns just LOH size.
size_t gc_heap::generation_sizes (generation* gen, bool use_saved_p)
{
    size_t result = 0;

#ifdef USE_REGIONS
    int gen_num = gen->gen_num;
    int start_gen_index = ((gen_num > max_generation) ? gen_num : 0);
    for (int i = start_gen_index; i <= gen_num; i++)
    {
        heap_segment* seg = heap_segment_in_range (generation_start_segment (generation_of (i)));
        while (seg)
        {
            uint8_t* end = (use_saved_p ?
                heap_segment_saved_allocated (seg) : heap_segment_allocated (seg));
            result += end - heap_segment_mem (seg);
            dprintf (3, ("h%d gen%d size + %zd (%p - %p) -> %zd",
                heap_number, i, (end - heap_segment_mem (seg)),
                heap_segment_mem (seg), end, result));
            seg = heap_segment_next (seg);
        }
    }
#else //USE_REGIONS
    if (generation_start_segment (gen ) == ephemeral_heap_segment)
        result = (heap_segment_allocated (ephemeral_heap_segment) -
                  generation_allocation_start (gen));
    else
    {
        heap_segment* seg = heap_segment_in_range (generation_start_segment (gen));

        _ASSERTE(seg != NULL);

        while (seg)
        {
            result += (heap_segment_allocated (seg) -
                       heap_segment_mem (seg));
            seg = heap_segment_next_in_range (seg);
        }
    }
#endif //USE_REGIONS

    return result;
}

#ifdef USE_REGIONS
bool gc_heap::decide_on_compaction_space()
{
    size_t gen0size = approximate_new_allocation();

    dprintf (REGIONS_LOG, ("gen0size: %zd, free: %zd",
        gen0size, (num_regions_freed_in_sweep * ((size_t)1 << min_segment_size_shr))));
    // If we don't compact, would we have enough space?
    if (sufficient_space_regions ((num_regions_freed_in_sweep * ((size_t)1 << min_segment_size_shr)),
                                  gen0size))
    {
        dprintf (REGIONS_LOG, ("it is sufficient!"));
        return false;
    }

    // If we do compact, would we have enough space?
    get_gen0_end_plan_space();

    if (!gen0_large_chunk_found)
    {
        gen0_large_chunk_found = (free_regions[basic_free_region].get_num_free_regions() > 0);
    }

    dprintf (REGIONS_LOG, ("gen0_pinned_free_space: %zd, end_gen0_region_space: %zd, gen0size: %zd",
            gen0_pinned_free_space, end_gen0_region_space, gen0size));

    if (sufficient_space_regions ((gen0_pinned_free_space + end_gen0_region_space), gen0size) &&
        gen0_large_chunk_found)
    {
        sufficient_gen0_space_p = TRUE;
    }

    return true;
}

#endif //USE_REGIONS

size_t gc_heap::estimated_reclaim (int gen_number)
{
    dynamic_data* dd = dynamic_data_of (gen_number);
    size_t gen_allocated = (dd_desired_allocation (dd) - dd_new_allocation (dd));
    size_t gen_total_size = gen_allocated + dd_current_size (dd);
    size_t est_gen_surv = (size_t)((float) (gen_total_size) * dd_surv (dd));
    size_t est_gen_free = gen_total_size - est_gen_surv + dd_fragmentation (dd);

    dprintf (GTC_LOG, ("h%d gen%d total size: %zd, est dead space: %zd (s: %d, allocated: %zd), frag: %zd",
                heap_number, gen_number,
                gen_total_size,
                est_gen_free,
                (int)(dd_surv (dd) * 100),
                gen_allocated,
                dd_fragmentation (dd)));

    return est_gen_free;
}

bool gc_heap::is_full_compacting_gc_productive()
{
#ifdef USE_REGIONS
    // If we needed to grow gen2 by extending either the end of its tail region
    // or having to acquire more regions for gen2, then we view this as unproductive.
    //
    // Note that when we freely choose which region to demote and promote, this calculation
    // will need to change.
    heap_segment* gen1_start_region = generation_start_segment (generation_of (max_generation - 1));
    if (heap_segment_plan_gen_num (gen1_start_region) == max_generation)
    {
        dprintf (REGIONS_LOG, ("gen1 start region %p is now part of gen2, unproductive",
            heap_segment_mem (gen1_start_region)));
        return false;
    }
    else
    {
        heap_segment* gen2_tail_region = generation_tail_region (generation_of (max_generation));
        if (heap_segment_plan_allocated (gen2_tail_region) >= heap_segment_allocated (gen2_tail_region))
        {
            dprintf (REGIONS_LOG, ("last gen2 region extended %p->%p, unproductive",
                heap_segment_allocated (gen2_tail_region), heap_segment_plan_allocated (gen2_tail_region)));

            return false;
        }
    }

    return true;
#else //USE_REGIONS
    if (generation_plan_allocation_start (generation_of (max_generation - 1)) >=
        generation_allocation_start (generation_of (max_generation - 1)))
    {
        dprintf (1, ("gen1 start %p->%p, gen2 size %zd->%zd, lock elevation",
                generation_allocation_start (generation_of (max_generation - 1)),
                generation_plan_allocation_start (generation_of (max_generation - 1)),
                    generation_size (max_generation),
                    generation_plan_size (max_generation)));
        return false;
    }
    else
        return true;
#endif //USE_REGIONS
}

BOOL gc_heap::decide_on_compacting (int condemned_gen_number,
                                    size_t fragmentation,
                                    BOOL& should_expand)
{
    BOOL should_compact = FALSE;
    should_expand = FALSE;
    generation*   gen = generation_of (condemned_gen_number);
    dynamic_data* dd = dynamic_data_of (condemned_gen_number);
    size_t gen_sizes     = generation_sizes(gen, true);
    float  fragmentation_burden = ( ((0 == fragmentation) || (0 == gen_sizes)) ? (0.0f) :
                                    (float (fragmentation) / gen_sizes) );

    dprintf (GTC_LOG, ("h%d g%d fragmentation: %zd (%d%%), gen_sizes: %zd",
        heap_number, settings.condemned_generation,
        fragmentation, (int)(fragmentation_burden * 100.0),
        gen_sizes));

#ifdef USE_REGIONS
    if (special_sweep_p)
    {
        return FALSE;
    }
#endif //USE_REGIONS

#if defined(STRESS_HEAP) && !defined(FEATURE_NATIVEAOT)
    // for GC stress runs we need compaction
    if (GCStress<cfg_any>::IsEnabled() && !settings.concurrent)
        should_compact = TRUE;
#endif //defined(STRESS_HEAP) && !defined(FEATURE_NATIVEAOT)

    if (GCConfig::GetForceCompact())
        should_compact = TRUE;

    if ((condemned_gen_number == max_generation) && last_gc_before_oom)
    {
        should_compact = TRUE;
#ifndef USE_REGIONS
        last_gc_before_oom = FALSE;
#endif //!USE_REGIONS
        get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_last_gc);
    }

    if (settings.reason == reason_induced_compacting)
    {
        dprintf (2, ("induced compacting GC"));
        should_compact = TRUE;
        get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_induced_compacting);
    }

    if (settings.reason == reason_induced_aggressive)
    {
        dprintf (2, ("aggressive compacting GC"));
        should_compact = TRUE;
        get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_aggressive_compacting);
    }

    if (settings.reason == reason_pm_full_gc)
    {
        assert (condemned_gen_number == max_generation);
        if (heap_number == 0)
        {
            dprintf (GTC_LOG, ("PM doing compacting full GC after a gen1"));
        }
        should_compact = TRUE;
    }

    dprintf (2, ("Fragmentation: %zu Fragmentation burden %d%%",
                fragmentation, (int) (100*fragmentation_burden)));

    if (provisional_mode_triggered && (condemned_gen_number == (max_generation - 1)))
    {
        dprintf (GTC_LOG, ("gen1 in PM always compact"));
        should_compact = TRUE;
    }

#ifdef USE_REGIONS
    if (!should_compact)
    {
        should_compact = !!decide_on_compaction_space();
    }
#else //USE_REGIONS
    if (!should_compact)
    {
        if (dt_low_ephemeral_space_p (tuning_deciding_compaction))
        {
            dprintf(GTC_LOG, ("compacting due to low ephemeral"));
            should_compact = TRUE;
            get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_low_ephemeral);
        }
    }

    if (should_compact)
    {
        if ((condemned_gen_number >= (max_generation - 1)))
        {
            if (dt_low_ephemeral_space_p (tuning_deciding_expansion))
            {
                dprintf (GTC_LOG,("Not enough space for all ephemeral generations with compaction"));
                should_expand = TRUE;
            }
        }
    }
#endif //USE_REGIONS

#ifdef HOST_64BIT
    BOOL high_memory = FALSE;
#endif // HOST_64BIT

    if (!should_compact)
    {
        // We are not putting this in dt_high_frag_p because it's not exactly
        // high fragmentation - it's just enough planned fragmentation for us to
        // want to compact. Also the "fragmentation" we are talking about here
        // is different from anywhere else.
        dprintf (REGIONS_LOG, ("frag: %zd, fragmentation_burden: %.3f",
            fragmentation, fragmentation_burden));
        BOOL frag_exceeded = ((fragmentation >= dd_fragmentation_limit (dd)) &&
                                (fragmentation_burden >= dd_fragmentation_burden_limit (dd)));

        if (frag_exceeded)
        {
#ifdef BACKGROUND_GC
            // do not force compaction if this was a stress-induced GC
            IN_STRESS_HEAP(if (!settings.stress_induced))
            {
#endif // BACKGROUND_GC
            assert (settings.concurrent == FALSE);
            should_compact = TRUE;
            get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_high_frag);
#ifdef BACKGROUND_GC
            }
#endif // BACKGROUND_GC
        }

#ifdef HOST_64BIT
        // check for high memory situation
        if(!should_compact)
        {
            uint32_t num_heaps = 1;
#ifdef MULTIPLE_HEAPS
            num_heaps = gc_heap::n_heaps;
#endif // MULTIPLE_HEAPS

            ptrdiff_t reclaim_space = generation_size(max_generation) - generation_plan_size(max_generation);

            if((settings.entry_memory_load >= high_memory_load_th) && (settings.entry_memory_load < v_high_memory_load_th))
            {
                if(reclaim_space > (int64_t)(min_high_fragmentation_threshold (entry_available_physical_mem, num_heaps)))
                {
                    dprintf(GTC_LOG,("compacting due to fragmentation in high memory"));
                    should_compact = TRUE;
                    get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_high_mem_frag);
                }
                high_memory = TRUE;
            }
            else if(settings.entry_memory_load >= v_high_memory_load_th)
            {
                if(reclaim_space > (ptrdiff_t)(min_reclaim_fragmentation_threshold (num_heaps)))
                {
                    dprintf(GTC_LOG,("compacting due to fragmentation in very high memory"));
                    should_compact = TRUE;
                    get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_vhigh_mem_frag);
                }
                high_memory = TRUE;
            }
        }
#endif // HOST_64BIT
    }

    // The purpose of calling ensure_gap_allocation here is to make sure
    // that we actually are able to commit the memory to allocate generation
    // starts.
    if ((should_compact == FALSE) &&
        (ensure_gap_allocation (condemned_gen_number) == FALSE))
    {
        should_compact = TRUE;
        get_gc_data_per_heap()->set_mechanism (gc_heap_compact, compact_no_gaps);
    }

    if (settings.condemned_generation == max_generation)
    {
        //check the progress
        if (
#ifdef HOST_64BIT
            (high_memory && !should_compact) ||
#endif // HOST_64BIT
            !is_full_compacting_gc_productive())
        {
            //no progress -> lock
            settings.should_lock_elevation = TRUE;
        }
    }

    if (settings.pause_mode == pause_no_gc)
    {
        should_compact = TRUE;
        if ((size_t)(heap_segment_reserved (ephemeral_heap_segment) - heap_segment_plan_allocated (ephemeral_heap_segment))
            < soh_allocation_no_gc)
        {
            should_expand = TRUE;
        }
    }

    dprintf (2, ("will %s(%s)", (should_compact ? "compact" : "sweep"), (should_expand ? "ex" : "")));
    return should_compact;
}

size_t align_lower_good_size_allocation (size_t size)
{
    return (size/64)*64;
}

size_t gc_heap::approximate_new_allocation()
{
    dynamic_data* dd0 = dynamic_data_of (0);
    return max (2*dd_min_size (dd0), ((dd_desired_allocation (dd0)*2)/3));
}

bool gc_heap::check_against_hard_limit (size_t space_required)
{
    bool can_fit = TRUE;

    // If hard limit is specified, and if we attributed all that's left in commit to the ephemeral seg
    // so we treat that as segment end, do we have enough space.
    if (heap_hard_limit)
    {
        size_t left_in_commit = heap_hard_limit - current_total_committed;
        int num_heaps = get_num_heaps();
        left_in_commit /= num_heaps;
        if (left_in_commit < space_required)
        {
            can_fit = FALSE;
        }

        dprintf (2, ("h%d end seg %zd, but only %zd left in HARD LIMIT commit, required: %zd %s on eph",
            heap_number, space_required,
            left_in_commit, space_required,
            (can_fit ? "ok" : "short")));
    }

    return can_fit;
}

#ifdef USE_REGIONS
bool gc_heap::sufficient_space_regions_for_allocation (size_t end_space, size_t end_space_required)
{
    // REGIONS PERF TODO: we can repurpose large regions here too, if needed.
    size_t free_regions_space = (free_regions[basic_free_region].get_num_free_regions() * ((size_t)1 << min_segment_size_shr)) +
                                global_region_allocator.get_free();
    size_t total_alloc_space = end_space + free_regions_space;
    dprintf (REGIONS_LOG, ("h%d required %zd, end %zd + free %zd=%zd",
        heap_number, end_space_required, end_space, free_regions_space, total_alloc_space));
    size_t total_commit_space = end_gen0_region_committed_space + free_regions[basic_free_region].get_size_committed_in_free();
    if (total_alloc_space > end_space_required)
    {
        if (end_space_required > total_commit_space)
        {
            return check_against_hard_limit (end_space_required - total_commit_space);
        }
        else
        {
            return true;
        }
    }
    else
    {
        return false;
    }
}

bool gc_heap::sufficient_space_regions (size_t end_space, size_t end_space_required)
{
    // REGIONS PERF TODO: we can repurpose large regions here too, if needed.
    // REGIONS PERF TODO: for callsites other than allocation, we should also take commit into account
    size_t free_regions_space = (free_regions[basic_free_region].get_num_free_regions() * ((size_t)1 << min_segment_size_shr)) +
                                global_region_allocator.get_free();
    size_t total_alloc_space = end_space + free_regions_space;
    dprintf (REGIONS_LOG, ("h%d required %zd, end %zd + free %zd=%zd",
        heap_number, end_space_required, end_space, free_regions_space, total_alloc_space));
    if (total_alloc_space > end_space_required)
    {
        return check_against_hard_limit (end_space_required);
    }
    else
    {
        return false;
    }
}

#else //USE_REGIONS
BOOL gc_heap::sufficient_space_end_seg (uint8_t* start, uint8_t* committed, uint8_t* reserved, size_t end_space_required)
{
    BOOL can_fit = FALSE;
    size_t committed_space = (size_t)(committed - start);
    size_t end_seg_space = (size_t)(reserved - start);
    if (committed_space > end_space_required)
    {
        return true;
    }
    else if (end_seg_space > end_space_required)
    {
        return check_against_hard_limit (end_space_required - committed_space);
    }
    else
        return false;
}

#endif //USE_REGIONS

// After we did a GC we expect to have at least this
// much space at the end of the segment to satisfy
// a reasonable amount of allocation requests.
size_t gc_heap::end_space_after_gc()
{
    return max ((dd_min_size (dynamic_data_of (0))/2), (END_SPACE_AFTER_GC_FL));
}

BOOL gc_heap::ephemeral_gen_fit_p (gc_tuning_point tp)
{
    uint8_t* start = 0;

#ifdef USE_REGIONS
    assert ((tp == tuning_deciding_condemned_gen) || (tp == tuning_deciding_full_gc));
#else//USE_REGIONS
    if ((tp == tuning_deciding_condemned_gen) ||
        (tp == tuning_deciding_compaction))
    {
        start = (settings.concurrent ? alloc_allocated : heap_segment_allocated (ephemeral_heap_segment));
        if (settings.concurrent)
        {
            dprintf (2, ("%zd left at the end of ephemeral segment (alloc_allocated)",
                (size_t)(heap_segment_reserved (ephemeral_heap_segment) - alloc_allocated)));
        }
        else
        {
            dprintf (2, ("%zd left at the end of ephemeral segment (allocated)",
                (size_t)(heap_segment_reserved (ephemeral_heap_segment) - heap_segment_allocated (ephemeral_heap_segment))));
        }
    }
    else if (tp == tuning_deciding_expansion)
    {
        start = heap_segment_plan_allocated (ephemeral_heap_segment);
        dprintf (2, ("%zd left at the end of ephemeral segment based on plan",
            (size_t)(heap_segment_reserved (ephemeral_heap_segment) - start)));
    }
    else
    {
        assert (tp == tuning_deciding_full_gc);
        dprintf (2, ("FGC: %zd left at the end of ephemeral segment (alloc_allocated)",
            (size_t)(heap_segment_reserved (ephemeral_heap_segment) - alloc_allocated)));
        start = alloc_allocated;
    }

    if (start == 0) // empty ephemeral generations
    {
        assert (tp == tuning_deciding_expansion);
        // if there are no survivors in the ephemeral segment,
        // this should be the beginning of ephemeral segment.
        start = generation_allocation_pointer (generation_of (max_generation));
        assert (start == heap_segment_mem (ephemeral_heap_segment));
    }

    if (tp == tuning_deciding_expansion)
    {
        assert (settings.condemned_generation >= (max_generation-1));
        size_t gen0size = approximate_new_allocation();
        size_t eph_size = gen0size;
        size_t gen_min_sizes = 0;

        for (int j = 1; j <= max_generation-1; j++)
        {
            gen_min_sizes += 2*dd_min_size (dynamic_data_of(j));
        }

        eph_size += gen_min_sizes;

        dprintf (3, ("h%d deciding on expansion, need %zd (gen0: %zd, 2*min: %zd)",
            heap_number, gen0size, gen_min_sizes, eph_size));

        // We must find room for one large object and enough room for gen0size
        if ((size_t)(heap_segment_reserved (ephemeral_heap_segment) - start) > eph_size)
        {
            dprintf (3, ("Enough room before end of segment"));
            return TRUE;
        }
        else
        {
            size_t room = align_lower_good_size_allocation
                (heap_segment_reserved (ephemeral_heap_segment) - start);
            size_t end_seg = room;

            //look at the plug free space
            size_t largest_alloc = END_SPACE_AFTER_GC_FL;
            bool large_chunk_found = FALSE;
            size_t bos = 0;
            uint8_t* gen0start = generation_plan_allocation_start (youngest_generation);
            dprintf (3, ("ephemeral_gen_fit_p: gen0 plan start: %zx", (size_t)gen0start));
            if (gen0start == 0)
                return FALSE;
            dprintf (3, ("ephemeral_gen_fit_p: room before free list search %zd, needed: %zd",
                         room, gen0size));
            while ((bos < mark_stack_bos) &&
                   !((room >= gen0size) && large_chunk_found))
            {
                uint8_t* plug = pinned_plug (pinned_plug_of (bos));
                if (in_range_for_segment (plug, ephemeral_heap_segment))
                {
                    if (plug >= gen0start)
                    {
                        size_t chunk = align_lower_good_size_allocation (pinned_len (pinned_plug_of (bos)));
                        room += chunk;
                        if (!large_chunk_found)
                        {
                            large_chunk_found = (chunk >= largest_alloc);
                        }
                        dprintf (3, ("ephemeral_gen_fit_p: room now %zd, large chunk: %d",
                                     room, large_chunk_found));
                    }
                }
                bos++;
            }

            if (room >= gen0size)
            {
                if (large_chunk_found)
                {
                    sufficient_gen0_space_p = TRUE;

                    dprintf (3, ("Enough room"));
                    return TRUE;
                }
                else
                {
                    // now we need to find largest_alloc at the end of the segment.
                    if (end_seg >= end_space_after_gc())
                    {
                        dprintf (3, ("Enough room (may need end of seg)"));
                        return TRUE;
                    }
                }
            }

            dprintf (3, ("Not enough room"));
                return FALSE;
        }
    }
    else
#endif //USE_REGIONS
    {
        size_t end_space = 0;
        dynamic_data* dd = dynamic_data_of (0);
        if ((tp == tuning_deciding_condemned_gen) ||
            (tp == tuning_deciding_full_gc))
        {
            end_space = max (2*dd_min_size (dd), end_space_after_gc());
        }
        else
        {
            assert (tp == tuning_deciding_compaction);
            end_space = approximate_new_allocation();
        }

#ifdef USE_REGIONS
        size_t gen0_end_space = get_gen0_end_space (memory_type_reserved);
        BOOL can_fit = sufficient_space_regions (gen0_end_space, end_space);
#else //USE_REGIONS
        BOOL can_fit = sufficient_space_end_seg (start, heap_segment_committed (ephemeral_heap_segment), heap_segment_reserved (ephemeral_heap_segment), end_space);
#endif //USE_REGIONS
        return can_fit;
    }
}

#ifdef GC_CONFIG_DRIVEN
BOOL gc_heap::should_do_sweeping_gc (BOOL compact_p)
{
    if (!compact_ratio)
        return (!compact_p);

    size_t compact_count = compact_or_sweep_gcs[0];
    size_t sweep_count = compact_or_sweep_gcs[1];

    size_t total_count = compact_count + sweep_count;
    BOOL should_compact = compact_p;
    if (total_count > 3)
    {
        if (compact_p)
        {
            int temp_ratio = (int)((compact_count + 1) * 100 / (total_count + 1));
            if (temp_ratio > compact_ratio)
            {
                // cprintf (("compact would be: %d, total_count: %d, ratio would be %d%% > target\n",
                //     (compact_count + 1), (total_count + 1), temp_ratio));
                should_compact = FALSE;
            }
        }
        else
        {
            int temp_ratio = (int)((sweep_count + 1) * 100 / (total_count + 1));
            if (temp_ratio > (100 - compact_ratio))
            {
                // cprintf (("sweep would be: %d, total_count: %d, ratio would be %d%% > target\n",
                //     (sweep_count + 1), (total_count + 1), temp_ratio));
                should_compact = TRUE;
            }
        }
    }

    return !should_compact;
}

#endif //GC_CONFIG_DRIVEN
