// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Things we need to manually initialize:
// gen0 min_size - based on cache
// gen0/1 max_size - based on segment size
static static_data static_data_table[latency_level_last - latency_level_first + 1][total_generation_count] =
{
    // latency_level_memory_footprint
    {
        // gen0
        {0, 0, 40000, 0.5f, 9.0f, 20.0f, (1000 * 1000), 1},
        // gen1
        {160*1024, 0, 80000, 0.5f, 2.0f, 7.0f, (10 * 1000 * 1000), 10},
        // gen2
        {256*1024, SSIZE_T_MAX, 200000, 0.25f, 1.2f, 1.8f, (100 * 1000 * 1000), 100},
        // loh
        {3*1024*1024, SSIZE_T_MAX, 0, 0.0f, 1.25f, 4.5f, 0, 0},
        // poh
        {3*1024*1024, SSIZE_T_MAX, 0, 0.0f, 1.25f, 4.5f, 0, 0},
    },

    // latency_level_balanced
    {
        // gen0
        {0, 0, 40000, 0.5f,
#ifdef MULTIPLE_HEAPS
            20.0f, 40.0f,
#else
            9.0f, 20.0f,
#endif //MULTIPLE_HEAPS
            (1000 * 1000), 1},
        // gen1
        {256*1024, 0, 80000, 0.5f, 2.0f, 7.0f, (10 * 1000 * 1000), 10},
        // gen2
        {256*1024, SSIZE_T_MAX, 200000, 0.25f, 1.2f, 1.8f, (100 * 1000 * 1000), 100},
        // loh
        {3*1024*1024, SSIZE_T_MAX, 0, 0.0f, 1.25f, 4.5f, 0, 0},
        // poh
        {3*1024*1024, SSIZE_T_MAX, 0, 0.0f, 1.25f, 4.5f, 0, 0}
    },
};

inline BOOL
gc_heap::dt_low_ephemeral_space_p (gc_tuning_point tp)
{
    BOOL ret = FALSE;

    switch (tp)
    {
        case tuning_deciding_condemned_gen:
#ifndef USE_REGIONS
        case tuning_deciding_compaction:
        case tuning_deciding_expansion:
#endif //USE_REGIONS
        case tuning_deciding_full_gc:
        {
            ret = (!ephemeral_gen_fit_p (tp));
            break;
        }
#ifndef USE_REGIONS
        case tuning_deciding_promote_ephemeral:
        {
            size_t new_gen0size = approximate_new_allocation();
            ptrdiff_t plan_ephemeral_size = total_ephemeral_size;

            dprintf (GTC_LOG, ("h%d: plan eph size is %zd, new gen0 is %zd",
                heap_number, plan_ephemeral_size, new_gen0size));
            // If we were in no_gc_region we could have allocated a larger than normal segment,
            // and the next seg we allocate will be a normal sized seg so if we can't fit the new
            // ephemeral generations there, do an ephemeral promotion.
            ret = ((soh_segment_size - segment_info_size) < (plan_ephemeral_size + new_gen0size));
            break;
        }
#endif //USE_REGIONS
        default:
        {
            assert (!"invalid tuning reason");
            break;
        }
    }

    return ret;
}

BOOL
gc_heap::dt_high_frag_p (gc_tuning_point tp,
                         int gen_number,
                         BOOL elevate_p)
{
    BOOL ret = FALSE;

    switch (tp)
    {
        case tuning_deciding_condemned_gen:
        {
            dynamic_data* dd = dynamic_data_of (gen_number);
            float fragmentation_burden = 0;

            if (elevate_p)
            {
                ret = (dd_fragmentation (dynamic_data_of (max_generation)) >= dd_max_size(dd));
                if (ret)
                {
                    dprintf (6666, ("h%d: frag is %zd, max size is %zd",
                        heap_number, dd_fragmentation (dd), dd_max_size(dd)));
                }
            }
            else
            {
#ifndef MULTIPLE_HEAPS
                if (gen_number == max_generation)
                {
                    size_t maxgen_size = generation_size (max_generation);
                    float frag_ratio = (maxgen_size ? ((float)dd_fragmentation (dynamic_data_of (max_generation)) / (float)maxgen_size) : 0.0f);
                    if (frag_ratio > 0.65)
                    {
                        dprintf (GTC_LOG, ("g2 FR: %d%%", (int)(frag_ratio*100)));
                        return TRUE;
                    }
                }
#endif //!MULTIPLE_HEAPS
                size_t fr = generation_unusable_fragmentation (generation_of (gen_number), heap_number);
                ret = (fr > dd_fragmentation_limit(dd));
                if (ret)
                {
                    size_t gen_size = generation_size (gen_number);
                    fragmentation_burden = (gen_size ? ((float)fr / (float)gen_size) : 0.0f);
                    ret = (fragmentation_burden > dd_v_fragmentation_burden_limit (dd));
                }
                if (ret)
                {
                    dprintf (6666, ("h%d: gen%d, frag is %zd, alloc effi: %zu%%, unusable frag is %zd, ratio is %d",
                        heap_number, gen_number, dd_fragmentation (dd),
                        generation_allocator_efficiency_percent (generation_of (gen_number)),
                        fr, (int)(fragmentation_burden * 100)));
                }
            }
            break;
        }
        default:
            break;
    }

    return ret;
}

inline BOOL
gc_heap::dt_estimate_reclaim_space_p (gc_tuning_point tp, int gen_number)
{
    BOOL ret = FALSE;

    switch (tp)
    {
        case tuning_deciding_condemned_gen:
        {
            if (gen_number == max_generation)
            {
                size_t est_maxgen_free = estimated_reclaim (gen_number);

                uint32_t num_heaps = 1;
#ifdef MULTIPLE_HEAPS
                num_heaps = gc_heap::n_heaps;
#endif //MULTIPLE_HEAPS

                size_t min_frag_th = min_reclaim_fragmentation_threshold (num_heaps);
                dprintf (GTC_LOG, ("h%d, min frag is %zd", heap_number, min_frag_th));
                ret = (est_maxgen_free >= min_frag_th);
            }
            else
            {
                assert (0);
            }
            break;
        }

        default:
            break;
    }

    return ret;
}

// DTREVIEW: Right now we only estimate gen2 fragmentation.
// on 64-bit though we should consider gen1 or even gen0 fragmentation as
// well
inline BOOL
gc_heap::dt_estimate_high_frag_p (gc_tuning_point tp, int gen_number, uint64_t available_mem)
{
    BOOL ret = FALSE;

    switch (tp)
    {
        case tuning_deciding_condemned_gen:
        {
            if (gen_number == max_generation)
            {
                dynamic_data* dd = dynamic_data_of (gen_number);
                float est_frag_ratio = 0;
                if (dd_current_size (dd) == 0)
                {
                    est_frag_ratio = 1;
                }
                else if ((dd_fragmentation (dd) == 0) || (dd_fragmentation (dd) + dd_current_size (dd) == 0))
                {
                    est_frag_ratio = 0;
                }
                else
                {
                    est_frag_ratio = (float)dd_fragmentation (dd) / (float)(dd_fragmentation (dd) + dd_current_size (dd));
                }

                size_t est_frag = (dd_fragmentation (dd) + (size_t)((dd_desired_allocation (dd) - dd_new_allocation (dd)) * est_frag_ratio));
                dprintf (GTC_LOG, ("h%d: gen%d: current_size is %zd, frag is %zd, est_frag_ratio is %d%%, estimated frag is %zd",
                    heap_number,
                    gen_number,
                    dd_current_size (dd),
                    dd_fragmentation (dd),
                    (int)(est_frag_ratio * 100),
                    est_frag));

                uint32_t num_heaps = 1;

#ifdef MULTIPLE_HEAPS
                num_heaps = gc_heap::n_heaps;
#endif //MULTIPLE_HEAPS
                uint64_t min_frag_th = min_high_fragmentation_threshold(available_mem, num_heaps);
                //dprintf (GTC_LOG, ("h%d, min frag is %zd", heap_number, min_frag_th));
                ret = (est_frag >= min_frag_th);
            }
            else
            {
                assert (0);
            }
            break;
        }

        default:
            break;
    }

    return ret;
}

inline BOOL
gc_heap::dt_low_card_table_efficiency_p (gc_tuning_point tp)
{
    BOOL ret = FALSE;

    switch (tp)
    {
    case tuning_deciding_condemned_gen:
    {
        /* promote into max-generation if the card table has too many
        * generation faults besides the n -> 0
        */
        ret = (generation_skip_ratio < generation_skip_ratio_threshold);
        break;
    }

    default:
        break;
    }

    return ret;
}

inline BOOL
gc_heap::dt_high_memory_load_p()
{
    return ((settings.entry_memory_load >= high_memory_load_th) || g_low_memory_status);
}

#if defined(USE_REGIONS)
bool gc_heap::near_heap_hard_limit_p()
{
    if (heap_hard_limit)
    {
        int current_percent_heap_hard_limit = (int)((float)current_total_committed * 100.0 / (float)heap_hard_limit);
        dprintf (REGIONS_LOG, ("committed %zd is %d%% of limit %zd",
            current_total_committed, current_percent_heap_hard_limit, heap_hard_limit));
        if (current_percent_heap_hard_limit >= 90)
        {
            return true;
        }
    }

    return false;
}

bool gc_heap::distribute_surplus_p(ptrdiff_t balance, int kind, bool aggressive_decommit_large_p)
{
    if (balance < 0)
    {
        return true;
    }

    if (kind == basic_free_region)
    {
#ifdef BACKGROUND_GC
        // This is detecting FGCs that run during BGCs. It is not detecting ephemeral GCs that
        // (possibly) run right before a BGC as background_running_p() is not yet true at that point.
        return (background_running_p() && (settings.condemned_generation != max_generation));
#else
        return false;
#endif
    }

    return !aggressive_decommit_large_p;
}

void gc_heap::decide_on_decommit_strategy(bool joined_last_gc_before_oom)
{
#ifdef MULTIPLE_HEAPS
    if (joined_last_gc_before_oom || g_low_memory_status)
    {
        dprintf (REGIONS_LOG, ("low memory - decommitting everything (last_gc_before_oom=%d, g_low_memory_status=%d)", joined_last_gc_before_oom, g_low_memory_status));

        while (decommit_step(DECOMMIT_TIME_STEP_MILLISECONDS))
        {
        }
        return;
    }

    ptrdiff_t size_to_decommit_for_heap_hard_limit = 0;
    if (heap_hard_limit)
    {
        size_to_decommit_for_heap_hard_limit = (ptrdiff_t)(current_total_committed - (heap_hard_limit * (MAX_ALLOWED_MEM_LOAD / 100.0f)));
        size_to_decommit_for_heap_hard_limit = max(size_to_decommit_for_heap_hard_limit, (ptrdiff_t)0);
    }

    // For the various high memory load situations, we're not using the process size at all.  In
    // particular, if we had a large process and smaller processes running in the same container,
    // then we will treat them the same if the container reaches reaches high_memory_load_th.  In
    // the future, we could consider additional complexity to try to reclaim more memory from
    // larger processes than smaller ones.
    ptrdiff_t size_to_decommit_for_physical = 0;
    if (settings.entry_memory_load >= high_memory_load_th)
    {
        size_t entry_used_physical_mem = total_physical_mem - entry_available_physical_mem;
        size_t goal_used_physical_mem = (size_t)(((almost_high_memory_load_th) / 100.0) * total_physical_mem);
        size_to_decommit_for_physical = entry_used_physical_mem - goal_used_physical_mem;
    }

    size_t size_to_decommit = max(size_to_decommit_for_heap_hard_limit, size_to_decommit_for_physical);
    if (size_to_decommit > 0)
    {
        dprintf (REGIONS_LOG, ("low memory - decommitting %zd (for heap_hard_limit: %zd, for physical: %zd)", size_to_decommit, size_to_decommit_for_heap_hard_limit, size_to_decommit_for_physical));

        decommit_step(size_to_decommit / DECOMMIT_SIZE_PER_MILLISECOND);
    }

    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        if (global_regions_to_decommit[kind].get_num_free_regions() != 0)
        {
            gradual_decommit_in_progress_p = TRUE;
            break;
        }
    }
#else //MULTIPLE_HEAPS
    // we want to limit the amount of decommit we do per time to indirectly
    // limit the amount of time spent in recommit and page faults
    // we use the elapsed time since the last GC to arrive at the desired
    // decommit size
    // we limit the elapsed time to 10 seconds to avoid spending too much time decommitting
    // if less than DECOMMIT_TIME_STEP_MILLISECONDS elapsed, we don't decommit -
    // we don't want to decommit fractions of regions here
    dynamic_data* dd0 = dynamic_data_of (0);
    size_t ephemeral_elapsed = (size_t)((dd_time_clock (dd0) - gc_last_ephemeral_decommit_time) / 1000);
    if (ephemeral_elapsed >= DECOMMIT_TIME_STEP_MILLISECONDS)
    {
        gc_last_ephemeral_decommit_time = dd_time_clock (dd0);
        size_t decommit_step_milliseconds = min (ephemeral_elapsed, (size_t)(10*1000));

        decommit_step (decommit_step_milliseconds);
    }
    // transfer any remaining regions on the decommit list back to the free list
    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        if (global_regions_to_decommit[kind].get_num_free_regions() != 0)
        {
            free_regions[kind].transfer_regions (&global_regions_to_decommit[kind]);
        }
    }
#endif //MULTIPLE_HEAPS
}

#endif

size_t gc_heap::exponential_smoothing (int gen, size_t collection_count, size_t desired_per_heap)
{
    // to avoid spikes in mem usage due to short terms fluctuations in survivorship,
    // apply some smoothing.
    size_t smoothing = min((size_t)3, collection_count);

    size_t desired_total = desired_per_heap * n_heaps;
    size_t new_smoothed_desired_total = desired_total / smoothing + ((smoothed_desired_total[gen] / smoothing) * (smoothing - 1));
    smoothed_desired_total[gen] = new_smoothed_desired_total;
    size_t new_smoothed_desired_per_heap = new_smoothed_desired_total / n_heaps;

    // make sure we have at least dd_min_size
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = g_heaps[0];
#else //MULTIPLE_HEAPS
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
    dynamic_data* dd = hp->dynamic_data_of (gen);
    new_smoothed_desired_per_heap = max (new_smoothed_desired_per_heap, dd_min_size (dd));

    // align properly
    new_smoothed_desired_per_heap = Align (new_smoothed_desired_per_heap, get_alignment_constant (gen <= soh_gen2));
    dprintf (2, ("new smoothed_desired_per_heap for gen %d = %zd, desired_per_heap = %zd", gen, new_smoothed_desired_per_heap, desired_per_heap));

    return new_smoothed_desired_per_heap;
}

#ifdef DYNAMIC_HEAP_COUNT
size_t gc_heap::get_total_soh_stable_size()
{
    if (current_total_soh_stable_size)
    {
        return current_total_soh_stable_size;
    }
    else
    {
        size_t total_stable_size = 0;
        for (int i = 0; i < gc_heap::n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
            total_stable_size += hp->generation_size (max_generation - 1) / 2;
        }

        if (!total_stable_size)
        {
            // Setting a temp value before a GC naturally happens (ie, due to allocation).
            total_stable_size = dd_min_size (g_heaps[0]->dynamic_data_of (max_generation - 1));
        }

        return total_stable_size;
    }
}

void gc_heap::update_total_soh_stable_size()
{
    if ((dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes) && (settings.condemned_generation == max_generation))
    {
        current_total_soh_stable_size = 0;
        for (int i = 0; i < gc_heap::n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];

            dynamic_data* dd = hp->dynamic_data_of (max_generation);
            current_total_soh_stable_size += dd_current_size (dd) + dd_desired_allocation (dd);
            dprintf (2, ("current size is %.3fmb, budget %.3fmb, total -> %.3fmb", mb (dd_current_size (dd)), mb (dd_desired_allocation (dd)), mb (current_total_soh_stable_size)));
        }
    }
}

#endif //DYNAMIC_HEAP_COUNT

size_t gc_heap::get_total_heap_size()
{
    size_t total_heap_size = 0;

    // It's correct to start from max_generation for this method because
    // generation_sizes will return all SOH sizes when passed max_generation.
#ifdef MULTIPLE_HEAPS
    int hn = 0;

    for (hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp2 = gc_heap::g_heaps [hn];
        for (int i = max_generation; i < total_generation_count; i++)
        {
            total_heap_size += hp2->generation_sizes (hp2->generation_of (i));
        }
    }
#else
    for (int i = max_generation; i < total_generation_count; i++)
    {
        total_heap_size += generation_sizes (generation_of (i));
    }
#endif //MULTIPLE_HEAPS

    return total_heap_size;
}

size_t gc_heap::get_total_fragmentation()
{
    size_t total_fragmentation = 0;

#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps[hn];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        for (int i = 0; i < total_generation_count; i++)
        {
            generation* gen = hp->generation_of (i);
            total_fragmentation += (generation_free_list_space (gen) + generation_free_obj_space (gen));
        }
    }

    return total_fragmentation;
}

size_t gc_heap::get_total_gen_fragmentation (int gen_number)
{
    size_t total_fragmentation = 0;

#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps[hn];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        generation* gen = hp->generation_of (gen_number);
        total_fragmentation += (generation_free_list_space (gen) + generation_free_obj_space (gen));
    }

    return total_fragmentation;
}

#ifdef USE_REGIONS
int gc_heap::get_total_new_gen0_regions_in_plns ()
{
    int total_new_gen0_regions_in_plns = 0;

#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps[hn];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        total_new_gen0_regions_in_plns += hp->new_gen0_regions_in_plns;
    }

    return total_new_gen0_regions_in_plns;
}

int gc_heap::get_total_new_regions_in_prr ()
{
    int total_new_regions_in_prr = 0;

#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps[hn];
#else //MULTIPLE_HEAPS
        {
            gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
            total_new_regions_in_prr += hp->new_regions_in_prr;
        }

        return total_new_regions_in_prr;
}

int gc_heap::get_total_new_regions_in_threading ()
{
    int total_new_regions_in_threading = 0;

#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps[hn];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        total_new_regions_in_threading += hp->new_regions_in_threading;
    }

    return total_new_regions_in_threading;
}

#endif //USE_REGIONS

size_t gc_heap::get_total_gen_estimated_reclaim (int gen_number)
{
    size_t total_estimated_reclaim = 0;

#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps[hn];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        total_estimated_reclaim += hp->estimated_reclaim (gen_number);
    }

    return total_estimated_reclaim;
}

size_t gc_heap::get_total_gen_size (int gen_number)
{
#ifdef MULTIPLE_HEAPS
    size_t size = 0;
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps[hn];
        size += hp->generation_size (gen_number);
    }
#else
    size_t size = generation_size (gen_number);
#endif //MULTIPLE_HEAPS
    return size;
}

size_t gc_heap::committed_size()
{
    size_t total_committed = 0;

    const size_t kB = 1024;

    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
        size_t gen_committed = 0;
        size_t gen_allocated = 0;

        while (seg)
        {
            uint8_t* start =
#ifdef USE_REGIONS
                get_region_start (seg);
#else
                (uint8_t*)seg;
#endif //USE_REGIONS

            gen_committed += heap_segment_committed (seg) - start;
            gen_allocated += heap_segment_allocated (seg) - start;

            seg = heap_segment_next (seg);
        }
        dprintf (3, ("h%d committed in gen%d %zdkB, allocated %zdkB, committed-allocated %zdkB", heap_number, i, gen_committed/kB, gen_allocated/kB, (gen_committed - gen_allocated)/kB));

        total_committed += gen_committed;
    }

#ifdef USE_REGIONS
    size_t committed_in_free = 0;

    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        committed_in_free += free_regions[kind].get_size_committed_in_free();
    }

    dprintf (3, ("h%d committed in free %zdkB", heap_number, committed_in_free/kB));

    total_committed += committed_in_free;
#endif //USE_REGIONS

    return total_committed;
}

size_t gc_heap::get_total_committed_size()
{
    size_t total_committed = 0;

#ifdef MULTIPLE_HEAPS
    int hn = 0;

    for (hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        total_committed += hp->committed_size();
    }
#else
    total_committed = committed_size();
#endif //MULTIPLE_HEAPS

    return total_committed;
}

size_t gc_heap::uoh_committed_size (int gen_number, size_t* allocated)
{
    generation* gen = generation_of (gen_number);
    heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
    size_t total_committed = 0;
    size_t total_allocated = 0;

    while (seg)
    {
        uint8_t* start =
#ifdef USE_REGIONS
            get_region_start (seg);
#else
            (uint8_t*)seg;
#endif //USE_REGIONS
        total_committed += heap_segment_committed (seg) - start;
        total_allocated += heap_segment_allocated (seg) - start;
        seg = heap_segment_next (seg);
    }

    *allocated = total_allocated;
    return total_committed;
}

void gc_heap::get_memory_info (uint32_t* memory_load,
                               uint64_t* available_physical,
                               uint64_t* available_page_file)
{
    GCToOSInterface::GetMemoryStatus(is_restricted_physical_mem ? total_physical_mem  : 0,  memory_load, available_physical, available_page_file);
}

#ifdef BACKGROUND_GC
#ifdef BGC_SERVO_TUNING
bool gc_heap::bgc_tuning::stepping_trigger (uint32_t current_memory_load, size_t current_gen2_count)
{
    if (!bgc_tuning::enable_fl_tuning)
    {
        return false;
    }

    bool stepping_trigger_p = false;
    if (use_stepping_trigger_p)
    {
        dprintf (BGC_TUNING_LOG, ("current ml: %d, goal: %d",
            current_memory_load, memory_load_goal));
        // We don't go all the way up to mem goal because if we do we could end up with every
        // BGC being triggered by stepping all the way up to goal, and when we actually reach
        // goal we have no time to react 'cause the next BGC could already be over goal.
        if ((current_memory_load <= (memory_load_goal * 2 / 3)) ||
            ((memory_load_goal > current_memory_load) &&
             ((memory_load_goal - current_memory_load) > (stepping_interval * 3))))
        {
            int memory_load_delta = (int)current_memory_load - (int)last_stepping_mem_load;
            if (memory_load_delta >= (int)stepping_interval)
            {
                stepping_trigger_p = (current_gen2_count == last_stepping_bgc_count);
                if (stepping_trigger_p)
                {
                    current_gen2_count++;
                }

                dprintf (BGC_TUNING_LOG, ("current ml: %u - %u = %d (>= %u), gen2 count: %zu->%zu, stepping trigger: %s ",
                    current_memory_load, last_stepping_mem_load, memory_load_delta, stepping_interval,
                    last_stepping_bgc_count, current_gen2_count,
                    (stepping_trigger_p ? "yes" : "no")));
                last_stepping_mem_load = current_memory_load;
                last_stepping_bgc_count = current_gen2_count;
            }
        }
        else
        {
            use_stepping_trigger_p = false;
        }
    }

    return stepping_trigger_p;
}

// Note that I am doing this per heap but as we are in this calculation other
// heaps could increase their fl alloc. We are okay with that inaccurancy.
bool gc_heap::bgc_tuning::should_trigger_bgc_loh()
{
    if (fl_tuning_triggered)
    {
#ifdef MULTIPLE_HEAPS
        gc_heap* hp = g_heaps[0];
#else
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        if (!(gc_heap::background_running_p()))
        {
            size_t current_alloc = get_total_servo_alloc (loh_generation);
            tuning_calculation* current_gen_calc = &gen_calc[loh_generation - max_generation];

            if (current_alloc < current_gen_calc->last_bgc_end_alloc)
            {
                dprintf (BGC_TUNING_LOG, ("BTL: current alloc: %zd, last alloc: %zd?",
                    current_alloc, current_gen_calc->last_bgc_end_alloc));
            }

            bool trigger_p = ((current_alloc - current_gen_calc->last_bgc_end_alloc) >= current_gen_calc->alloc_to_trigger);
            dprintf (2, ("BTL3: LOH a %zd, la: %zd(%zd), %zd",
                    current_alloc, current_gen_calc->last_bgc_end_alloc,
                    (current_alloc - current_gen_calc->last_bgc_end_alloc),
                    current_gen_calc->alloc_to_trigger));

            if (trigger_p)
            {
                dprintf (BGC_TUNING_LOG, ("BTL3: LOH detected (%zd - %zd) >= %zd, TRIGGER",
                        current_alloc, current_gen_calc->last_bgc_end_alloc, current_gen_calc->alloc_to_trigger));
                return true;
            }
        }
    }

    return false;
}

bool gc_heap::bgc_tuning::should_trigger_bgc()
{
    if (!bgc_tuning::enable_fl_tuning || gc_heap::background_running_p())
    {
        return false;
    }

    if (settings.reason == reason_bgc_tuning_loh)
    {
        // TODO: this should be an assert because if the reason was reason_bgc_tuning_loh,
        // we should have already set to condemn max_generation but I'm keeping it
        // for now in case we are reverting it for other reasons.
        bgc_tuning::next_bgc_p = true;
        dprintf (BGC_TUNING_LOG, ("BTL LOH triggered"));
        return true;
    }

    if (!bgc_tuning::next_bgc_p &&
        !fl_tuning_triggered &&
        (gc_heap::settings.entry_memory_load >= (memory_load_goal * 2 / 3)) &&
        (gc_heap::full_gc_counts[gc_type_background] >= 2))
    {
        next_bgc_p = true;

        gen_calc[0].first_alloc_to_trigger = gc_heap::get_total_servo_alloc (max_generation);
        gen_calc[1].first_alloc_to_trigger = gc_heap::get_total_servo_alloc (loh_generation);
        dprintf (BGC_TUNING_LOG, ("BTL[GTC] mem high enough: %d(goal: %d), %zd BGCs done, g2a=%zd, g3a=%zd, trigger FL tuning!",
            gc_heap::settings.entry_memory_load, memory_load_goal,
            gc_heap::full_gc_counts[gc_type_background],
            gen_calc[0].first_alloc_to_trigger,
            gen_calc[1].first_alloc_to_trigger));
    }

    if (bgc_tuning::next_bgc_p)
    {
        dprintf (BGC_TUNING_LOG, ("BTL started FL tuning"));
        return true;
    }

    if (!fl_tuning_triggered)
    {
        return false;
    }

    // If the tuning started, we need to check if we've exceeded the alloc.
    int index = 0;
    bgc_tuning::tuning_calculation* current_gen_calc = 0;

    index = 0;
    current_gen_calc = &bgc_tuning::gen_calc[index];

#ifdef MULTIPLE_HEAPS
    gc_heap* hp = g_heaps[0];
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    size_t current_gen1_index = dd_collection_count (hp->dynamic_data_of (max_generation - 1));
    size_t gen1_so_far = current_gen1_index - gen1_index_last_bgc_end;

    if (current_gen_calc->alloc_to_trigger > 0)
    {
        // We are specifically checking for gen2 here. LOH is covered by should_trigger_bgc_loh.
        size_t current_alloc = get_total_servo_alloc (max_generation);
        if ((current_alloc - current_gen_calc->last_bgc_end_alloc) >= current_gen_calc->alloc_to_trigger)
        {
            dprintf (BGC_TUNING_LOG, ("BTL2: SOH detected (%zd - %zd) >= %zd, TRIGGER",
                    current_alloc, current_gen_calc->last_bgc_end_alloc, current_gen_calc->alloc_to_trigger));
            settings.reason = reason_bgc_tuning_soh;
            return true;
        }
    }

    return false;
}

bool gc_heap::bgc_tuning::should_delay_alloc (int gen_number)
{
    if ((gen_number != max_generation) || !bgc_tuning::enable_fl_tuning)
        return false;

    if (current_c_gc_state == c_gc_state_planning)
    {
        int i = 0;
#ifdef MULTIPLE_HEAPS
        for (; i < gc_heap::n_heaps; i++)
        {
            gc_heap* hp = gc_heap::g_heaps[i];
            size_t current_fl_size = generation_free_list_space (hp->generation_of (max_generation));
            size_t last_bgc_fl_size = hp->bgc_maxgen_end_fl_size;
#else
        {
            size_t current_fl_size = generation_free_list_space (generation_of (max_generation));
            size_t last_bgc_fl_size = bgc_maxgen_end_fl_size;
#endif //MULTIPLE_HEAPS

            if (last_bgc_fl_size)
            {
                float current_flr = (float) current_fl_size / (float)last_bgc_fl_size;
                if (current_flr < 0.4)
                {
                    dprintf (BGC_TUNING_LOG, ("BTL%d h%d last fl %zd, curr fl %zd (%.3f) d1",
                            gen_number, i, last_bgc_fl_size, current_fl_size, current_flr));
                    return true;
                }
            }
        }
    }

    return false;
}

void gc_heap::bgc_tuning::update_bgc_start (int gen_number, size_t num_gen1s_since_end)
{
    int tuning_data_index = gen_number - max_generation;
    tuning_calculation* current_gen_calc = &gen_calc[tuning_data_index];
    tuning_stats* current_gen_stats = &gen_stats[tuning_data_index];

    size_t total_generation_size = get_total_generation_size (gen_number);
    ptrdiff_t current_bgc_fl_size = get_total_generation_fl_size (gen_number);

    double physical_gen_flr = (double)current_bgc_fl_size * 100.0 / (double)total_generation_size;

    ptrdiff_t artificial_additional_fl = 0;

    if (fl_tuning_triggered)
    {
        artificial_additional_fl = ((current_gen_calc->end_gen_size_goal > total_generation_size) ? (current_gen_calc->end_gen_size_goal - total_generation_size) : 0);
        total_generation_size += artificial_additional_fl;
        current_bgc_fl_size += artificial_additional_fl;
    }

    current_gen_calc->current_bgc_start_flr = (double)current_bgc_fl_size * 100.0 / (double)total_generation_size;

    size_t current_alloc = get_total_servo_alloc (gen_number);
    dprintf (BGC_TUNING_LOG, ("BTL%d: st a: %zd, la: %zd",
        gen_number, current_alloc, current_gen_stats->last_alloc));
    current_gen_stats->last_alloc_end_to_start = current_alloc - current_gen_stats->last_alloc;
    current_gen_stats->last_alloc = current_alloc;

    current_gen_calc->actual_alloc_to_trigger = current_alloc - current_gen_calc->last_bgc_end_alloc;

    dprintf (BGC_TUNING_LOG, ("BTL%d: st: %zd g1s (%zd->%zd/gen1) since end, flr: %.3f(afl: %zd, %.3f)",
             gen_number, actual_num_gen1s_to_trigger,
             current_gen_stats->last_alloc_end_to_start,
             (num_gen1s_since_end ? (current_gen_stats->last_alloc_end_to_start / num_gen1s_since_end) : 0),
             current_gen_calc->current_bgc_start_flr, artificial_additional_fl, physical_gen_flr));
}

void gc_heap::bgc_tuning::record_bgc_start()
{
    if (!bgc_tuning::enable_fl_tuning)
        return;

    uint64_t elapsed_time_so_far = GetHighPrecisionTimeStamp() - process_start_time;

    // Note that younger gen's collection count is always updated with older gen's collections.
    // So to calcuate the actual # of gen1 occurred we really should take the # of gen2s into
    // account (and deduct from gen1's collection count). But right now I am using it for stats.
    size_t current_gen1_index = get_current_gc_index (max_generation - 1);

    dprintf (BGC_TUNING_LOG, ("BTL: g2t[st][g1 %zd]: %0.3f minutes",
        current_gen1_index,
        (double)elapsed_time_so_far / (double)1000000 / (double)60));

    actual_num_gen1s_to_trigger = current_gen1_index - gen1_index_last_bgc_end;
    gen1_index_last_bgc_start = current_gen1_index;

    update_bgc_start (max_generation, actual_num_gen1s_to_trigger);
    update_bgc_start (loh_generation, actual_num_gen1s_to_trigger);
}

double convert_range (double lower, double upper, double num, double percentage)
{
    double d = num - lower;
    if (d < 0.0)
        return 0.0;
    else
    {
        d = min ((upper - lower), d);
        return (d * percentage);
    }
}

double calculate_gradual_d (double delta_double, double step)
{
    bool changed_sign = false;
    if (delta_double < 0.0)
    {
        delta_double = -delta_double;
        changed_sign = true;
    }
    double res = 0;
    double current_lower_limit = 0;
    double current_ratio = 1.0;
    // Given a step, we will gradually reduce the weight of the portion
    // in each step.
    // We reduce by *0.6 each time so there will be 3 iterations:
    // 1->0.6->0.36 (next one would be 0.216 and terminate the loop)
    // This will produce a result that's between 0 and 0.098.
    while (current_ratio > 0.22)
    {
        res += convert_range (current_lower_limit, (current_lower_limit + step), delta_double, current_ratio);
        current_lower_limit += step;
        current_ratio *= 0.6;
    }

    if (changed_sign)
        res = -res;

    return res;
}

void gc_heap::bgc_tuning::update_bgc_sweep_start (int gen_number, size_t num_gen1s_since_start)
{
    int tuning_data_index = gen_number - max_generation;
    tuning_calculation* current_gen_calc = &gen_calc[tuning_data_index];
    tuning_stats* current_gen_stats = &gen_stats[tuning_data_index];

    size_t total_generation_size = 0;
    ptrdiff_t current_bgc_fl_size = 0;

    total_generation_size = get_total_generation_size (gen_number);
    current_bgc_fl_size = get_total_generation_fl_size (gen_number);

    double physical_gen_flr = (double)current_bgc_fl_size * 100.0 / (double)total_generation_size;

    ptrdiff_t artificial_additional_fl = 0;
    if (fl_tuning_triggered)
    {
        artificial_additional_fl = ((current_gen_calc->end_gen_size_goal > total_generation_size) ? (current_gen_calc->end_gen_size_goal - total_generation_size) : 0);
        total_generation_size += artificial_additional_fl;
        current_bgc_fl_size += artificial_additional_fl;
    }

    current_gen_calc->current_bgc_sweep_flr = (double)current_bgc_fl_size * 100.0 / (double)total_generation_size;

    size_t current_alloc = get_total_servo_alloc (gen_number);
    dprintf (BGC_TUNING_LOG, ("BTL%d: sw a: %zd, la: %zd",
        gen_number, current_alloc, current_gen_stats->last_alloc));
    current_gen_stats->last_alloc_start_to_sweep = current_alloc - current_gen_stats->last_alloc;
    // We are resetting gen2 alloc at sweep start.
    current_gen_stats->last_alloc = 0;

#ifdef SIMPLE_DPRINTF
    dprintf (BGC_TUNING_LOG, ("BTL%d: sflr: %.3f%%->%.3f%% (%zd->%zd, %zd->%zd) (%zd:%zd-%zd/gen1) since start (afl: %zd, %.3f)",
             gen_number,
             current_gen_calc->last_bgc_flr, current_gen_calc->current_bgc_sweep_flr,
             current_gen_calc->last_bgc_size, total_generation_size,
             current_gen_stats->last_bgc_fl_size, current_bgc_fl_size,
             num_gen1s_since_start, current_gen_stats->last_alloc_start_to_sweep,
             (num_gen1s_since_start? (current_gen_stats->last_alloc_start_to_sweep / num_gen1s_since_start) : 0),
             artificial_additional_fl, physical_gen_flr));
#endif //SIMPLE_DPRINTF
}

void gc_heap::bgc_tuning::record_bgc_sweep_start()
{
    if (!bgc_tuning::enable_fl_tuning)
        return;

    size_t current_gen1_index = get_current_gc_index (max_generation - 1);
    size_t num_gen1s_since_start = current_gen1_index - gen1_index_last_bgc_start;
    gen1_index_last_bgc_sweep = current_gen1_index;

    uint64_t elapsed_time_so_far = GetHighPrecisionTimeStamp() - process_start_time;
    dprintf (BGC_TUNING_LOG, ("BTL: g2t[sw][g1 %zd]: %0.3f minutes",
        current_gen1_index,
        (double)elapsed_time_so_far / (double)1000000 / (double)60));

    update_bgc_sweep_start (max_generation, num_gen1s_since_start);
    update_bgc_sweep_start (loh_generation, num_gen1s_since_start);
}

void gc_heap::bgc_tuning::calculate_tuning (int gen_number, bool use_this_loop_p)
{
    BOOL use_kd_p = enable_kd;
    BOOL use_ki_p = enable_ki;
    BOOL use_smooth_p = enable_smooth;
    BOOL use_tbh_p = enable_tbh;
    BOOL use_ff_p = enable_ff;

    int tuning_data_index = gen_number - max_generation;
    tuning_calculation* current_gen_calc = &gen_calc[tuning_data_index];
    tuning_stats* current_gen_stats = &gen_stats[tuning_data_index];
    bgc_size_data* data = &current_bgc_end_data[tuning_data_index];

    size_t total_generation_size = data->gen_size;
    size_t current_bgc_fl = data->gen_fl_size;

    size_t current_bgc_surv_size = get_total_surv_size (gen_number);
    size_t current_bgc_begin_data_size = get_total_begin_data_size (gen_number);

    // This is usually 0 unless a GC happened where we joined at the end of sweep
    size_t current_alloc = get_total_servo_alloc (gen_number);
    //dprintf (BGC_TUNING_LOG, ("BTL%d: current fl alloc: %zd, last recorded alloc: %zd, last_bgc_end_alloc: %zd",
    dprintf (BGC_TUNING_LOG, ("BTL%d: en a: %zd, la: %zd, lbgca: %zd",
        gen_number, current_alloc, current_gen_stats->last_alloc, current_gen_calc->last_bgc_end_alloc));

    double current_bgc_surv_rate = (current_bgc_begin_data_size == 0) ?
                                    0 : ((double)current_bgc_surv_size * 100.0 / (double)current_bgc_begin_data_size);

    current_gen_stats->last_alloc_sweep_to_end = current_alloc - current_gen_stats->last_alloc;

    size_t gen1_index = get_current_gc_index (max_generation - 1);
    size_t gen2_index = get_current_gc_index (max_generation);

    size_t num_gen1s_since_sweep = gen1_index - gen1_index_last_bgc_sweep;
    size_t num_gen1s_bgc_end = gen1_index - gen1_index_last_bgc_end;

    size_t gen_end_size_goal = current_gen_calc->end_gen_size_goal;
    double gen_sweep_flr_goal = current_gen_calc->sweep_flr_goal;
    size_t last_gen_alloc_to_trigger = current_gen_calc->alloc_to_trigger;
    size_t gen_actual_alloc_to_trigger = current_gen_calc->actual_alloc_to_trigger;
    size_t last_gen_alloc_to_trigger_0 = current_gen_calc->alloc_to_trigger_0;

    double current_end_to_sweep_flr = current_gen_calc->last_bgc_flr - current_gen_calc->current_bgc_sweep_flr;
    bool current_sweep_above_p = (current_gen_calc->current_bgc_sweep_flr > gen_sweep_flr_goal);

#ifdef SIMPLE_DPRINTF
    dprintf (BGC_TUNING_LOG, ("BTL%d: sflr: c %.3f (%s), p %s, palloc: %zd, aalloc %zd(%s)",
        gen_number,
        current_gen_calc->current_bgc_sweep_flr,
        (current_sweep_above_p ? "above" : "below"),
        (current_gen_calc->last_sweep_above_p ? "above" : "below"),
        last_gen_alloc_to_trigger,
        current_gen_calc->actual_alloc_to_trigger,
        (use_this_loop_p ? "this" : "last")));

    dprintf (BGC_TUNING_LOG, ("BTL%d-en[g1: %zd, g2: %zd]: end fl: %zd (%zd: S-%zd, %.3f%%->%.3f%%)",
            gen_number,
            gen1_index, gen2_index, current_bgc_fl,
            total_generation_size, current_bgc_surv_size,
            current_gen_stats->last_bgc_surv_rate, current_bgc_surv_rate));

    dprintf (BGC_TUNING_LOG, ("BTLS%d sflr: %.3f, end-start: %zd(%zd), start-sweep: %zd(%zd), sweep-end: %zd(%zd)",
            gen_number,
            current_gen_calc->current_bgc_sweep_flr,
            (gen1_index_last_bgc_start - gen1_index_last_bgc_end), current_gen_stats->last_alloc_end_to_start,
            (gen1_index_last_bgc_sweep - gen1_index_last_bgc_start), current_gen_stats->last_alloc_start_to_sweep,
            num_gen1s_since_sweep, current_gen_stats->last_alloc_sweep_to_end));
#endif //SIMPLE_DPRINTF

    size_t saved_alloc_to_trigger = 0;

    // during our calculation alloc can be negative so use double here.
    double current_alloc_to_trigger = 0.0;

    if (!fl_tuning_triggered && use_tbh_p)
    {
        current_gen_calc->alloc_to_trigger_0 = current_gen_calc->actual_alloc_to_trigger;
        dprintf (BGC_TUNING_LOG, ("BTL%d[g1: %zd]: not in FL tuning yet, setting alloc_to_trigger_0 to %zd",
                 gen_number,
                 gen1_index, current_gen_calc->alloc_to_trigger_0));
    }

    if (fl_tuning_triggered)
    {
        BOOL tuning_kd_finished_p = FALSE;

        // We shouldn't have an alloc_to_trigger that's > what's consumed before sweep happens.
        double max_alloc_to_trigger = ((double)current_bgc_fl * (100 - gen_sweep_flr_goal) / 100.0);
        double min_alloc_to_trigger = (double)current_bgc_fl * 0.05;

        {
            if (current_gen_calc->current_bgc_sweep_flr < 0.0)
            {
                dprintf (BGC_TUNING_LOG, ("BTL%d: sflr is %.3f!!! < 0, make it 0", gen_number, current_gen_calc->current_bgc_sweep_flr));
                current_gen_calc->current_bgc_sweep_flr = 0.0;
            }

            double adjusted_above_goal_kp = above_goal_kp;
            double above_goal_distance = current_gen_calc->current_bgc_sweep_flr - gen_sweep_flr_goal;
            if (use_ki_p)
            {
                if (current_gen_calc->above_goal_accu_error > max_alloc_to_trigger)
                {
                    dprintf (BGC_TUNING_LOG, ("g%d: ae TB! %.1f->%.1f", gen_number, current_gen_calc->above_goal_accu_error, max_alloc_to_trigger));
                }
                else if (current_gen_calc->above_goal_accu_error < min_alloc_to_trigger)
                {
                    dprintf (BGC_TUNING_LOG, ("g%d: ae TS! %.1f->%.1f", gen_number, current_gen_calc->above_goal_accu_error, min_alloc_to_trigger));
                }

                current_gen_calc->above_goal_accu_error = min (max_alloc_to_trigger, current_gen_calc->above_goal_accu_error);
                current_gen_calc->above_goal_accu_error = max (min_alloc_to_trigger, current_gen_calc->above_goal_accu_error);

                double above_goal_ki_gain = above_goal_ki * above_goal_distance * current_bgc_fl;
                double temp_accu_error = current_gen_calc->above_goal_accu_error + above_goal_ki_gain;
                // anti-windup
                if ((temp_accu_error > min_alloc_to_trigger) &&
                    (temp_accu_error < max_alloc_to_trigger))
                {
                    current_gen_calc->above_goal_accu_error = temp_accu_error;
                }
                else
                {
                    //dprintf (BGC_TUNING_LOG, ("alloc accu err + %.1f=%.1f, exc",
                    dprintf (BGC_TUNING_LOG, ("g%d: aae + %.1f=%.1f, exc", gen_number,
                            above_goal_ki_gain,
                            temp_accu_error));
                }
            }

            // First we do the PI loop.
            {
                saved_alloc_to_trigger = current_gen_calc->alloc_to_trigger;
                current_alloc_to_trigger = adjusted_above_goal_kp * above_goal_distance * current_bgc_fl;
                // la is last alloc_to_trigger, +%zd is the diff between la and the new alloc.
                // laa is the last actual alloc (gen_actual_alloc_to_trigger), +%zd is the diff between la and laa.
                dprintf (BGC_TUNING_LOG, ("BTL%d: sflr %.3f above * %.4f * %zd = %zd bytes in alloc, la: %zd(+%zd), laa: %zd(+%zd)",
                        gen_number,
                        (current_gen_calc->current_bgc_sweep_flr - (double)gen_sweep_flr_goal),
                        adjusted_above_goal_kp,
                        current_bgc_fl,
                        (size_t)current_alloc_to_trigger,
                        saved_alloc_to_trigger,
                        (size_t)(current_alloc_to_trigger - (double)saved_alloc_to_trigger),
                        gen_actual_alloc_to_trigger,
                        (gen_actual_alloc_to_trigger - saved_alloc_to_trigger)));

                if (use_ki_p)
                {
                    current_alloc_to_trigger += current_gen_calc->above_goal_accu_error;
                    dprintf (BGC_TUNING_LOG, ("BTL%d: +accu err %zd=%zd",
                            gen_number,
                            (size_t)(current_gen_calc->above_goal_accu_error),
                            (size_t)current_alloc_to_trigger));
                }
            }

            if (use_tbh_p)
            {
                if (current_gen_calc->last_sweep_above_p != current_sweep_above_p)
                {
                    size_t new_alloc_to_trigger_0 = (last_gen_alloc_to_trigger + last_gen_alloc_to_trigger_0) / 2;
                    dprintf (BGC_TUNING_LOG, ("BTL%d: tbh crossed SP, setting both to %zd", gen_number, new_alloc_to_trigger_0));
                    current_gen_calc->alloc_to_trigger_0 = new_alloc_to_trigger_0;
                    current_gen_calc->alloc_to_trigger = new_alloc_to_trigger_0;
                }

                tuning_kd_finished_p = TRUE;
            }
        }

        if (!tuning_kd_finished_p)
        {
            if (use_kd_p)
            {
                saved_alloc_to_trigger = last_gen_alloc_to_trigger;
                size_t alloc_delta = saved_alloc_to_trigger - gen_actual_alloc_to_trigger;
                double adjust_ratio = (double)alloc_delta / (double)gen_actual_alloc_to_trigger;
                double saved_adjust_ratio = adjust_ratio;
                if (enable_gradual_d)
                {
                    adjust_ratio = calculate_gradual_d (adjust_ratio, above_goal_kd);
                    dprintf (BGC_TUNING_LOG, ("BTL%d: gradual kd - reduced from %.3f to %.3f",
                            gen_number, saved_adjust_ratio, adjust_ratio));
                }
                else
                {
                    double kd = above_goal_kd;
                    double neg_kd = 0 - kd;
                    if (adjust_ratio > kd) adjust_ratio = kd;
                    if (adjust_ratio < neg_kd) adjust_ratio = neg_kd;
                    dprintf (BGC_TUNING_LOG, ("BTL%d: kd - reduced from %.3f to %.3f",
                            gen_number, saved_adjust_ratio, adjust_ratio));
                }

                current_gen_calc->alloc_to_trigger = (size_t)((double)gen_actual_alloc_to_trigger * (1 + adjust_ratio));

                dprintf (BGC_TUNING_LOG, ("BTL%d: kd %.3f, reduced it to %.3f * %zd, adjust %zd->%zd",
                        gen_number, saved_adjust_ratio,
                        adjust_ratio, gen_actual_alloc_to_trigger,
                        saved_alloc_to_trigger, current_gen_calc->alloc_to_trigger));
            }

            if (use_smooth_p && use_this_loop_p)
            {
                saved_alloc_to_trigger = current_gen_calc->alloc_to_trigger;
                size_t gen_smoothed_alloc_to_trigger = current_gen_calc->smoothed_alloc_to_trigger;
                double current_num_gen1s_smooth_factor = (num_gen1s_smooth_factor > (double)num_bgcs_since_tuning_trigger) ?
                                                        (double)num_bgcs_since_tuning_trigger : num_gen1s_smooth_factor;
                current_gen_calc->smoothed_alloc_to_trigger = (size_t)((double)saved_alloc_to_trigger / current_num_gen1s_smooth_factor +
                    ((double)gen_smoothed_alloc_to_trigger / current_num_gen1s_smooth_factor) * (current_num_gen1s_smooth_factor - 1.0));

                dprintf (BGC_TUNING_LOG, ("BTL%d: smoothed %zd / %.3f + %zd / %.3f * %.3f adjust %zd->%zd",
                    gen_number, saved_alloc_to_trigger, current_num_gen1s_smooth_factor,
                    gen_smoothed_alloc_to_trigger, current_num_gen1s_smooth_factor,
                    (current_num_gen1s_smooth_factor - 1.0),
                    saved_alloc_to_trigger, current_gen_calc->smoothed_alloc_to_trigger));
                current_gen_calc->alloc_to_trigger = current_gen_calc->smoothed_alloc_to_trigger;
            }
        }

        if (use_ff_p)
        {
            double next_end_to_sweep_flr = data->gen_flr - gen_sweep_flr_goal;

            if (next_end_to_sweep_flr > 0.0)
            {
                saved_alloc_to_trigger = current_gen_calc->alloc_to_trigger;
                double ff_ratio = next_end_to_sweep_flr / current_end_to_sweep_flr - 1;

                if (use_this_loop_p)
                {
                    // if we adjust down we want ff to be bigger, so the alloc will be even smaller;
                    // if we adjust up want ff to be smaller, so the alloc will also be smaller;
                    // the idea is we want to be slower at increase than decrease
                    double ff_step = above_goal_ff * 0.5;
                    double adjusted_above_goal_ff = above_goal_ff;
                    if (ff_ratio > 0)
                        adjusted_above_goal_ff -= ff_step;
                    else
                        adjusted_above_goal_ff += ff_step;

                    double adjusted_ff_ratio = ff_ratio * adjusted_above_goal_ff;
                    current_gen_calc->alloc_to_trigger = saved_alloc_to_trigger + (size_t)((double)saved_alloc_to_trigger * adjusted_ff_ratio);
                    dprintf (BGC_TUNING_LOG, ("BTL%d: ff (%.3f / %.3f - 1) * %.3f = %.3f adjust %zd->%zd",
                        gen_number, next_end_to_sweep_flr, current_end_to_sweep_flr, adjusted_above_goal_ff, adjusted_ff_ratio,
                        saved_alloc_to_trigger, current_gen_calc->alloc_to_trigger));
                }
            }
        }

        if (use_this_loop_p)
        {
            // apply low/high caps.
            if (current_alloc_to_trigger > max_alloc_to_trigger)
            {
                dprintf (BGC_TUNING_LOG, ("BTL%d: TB! %.1f -> %.1f",
                    gen_number, current_alloc_to_trigger, max_alloc_to_trigger));
                current_alloc_to_trigger = max_alloc_to_trigger;
            }

            if (current_alloc_to_trigger < min_alloc_to_trigger)
            {
                dprintf (BGC_TUNING_LOG, ("BTL%d: TS! %zd -> %zd",
                        gen_number, (ptrdiff_t)current_alloc_to_trigger, (size_t)min_alloc_to_trigger));
                current_alloc_to_trigger = min_alloc_to_trigger;
            }

            current_gen_calc->alloc_to_trigger = (size_t)current_alloc_to_trigger;
        }
        else
        {
            // we can't do the above comparison - we could be in the situation where
            // we haven't done any alloc.
            dprintf (BGC_TUNING_LOG, ("BTL%d: ag, revert %zd->%zd",
                gen_number, current_gen_calc->alloc_to_trigger, last_gen_alloc_to_trigger));
            current_gen_calc->alloc_to_trigger = last_gen_alloc_to_trigger;
        }
    }

    // This is only executed once to get the tuning started.
    if (next_bgc_p)
    {
        size_t first_alloc = (size_t)((double)current_gen_calc->first_alloc_to_trigger * 0.75);
        // The initial conditions can be quite erratic so check to see if the first alloc we set was reasonable - take 5% of the FL
        size_t min_first_alloc = current_bgc_fl / 20;

        current_gen_calc->alloc_to_trigger = max (first_alloc, min_first_alloc);

        dprintf (BGC_TUNING_LOG, ("BTL%d[g1: %zd]: BGC end, trigger FL, set gen%d alloc to max (0.75 of first: %zd, 5%% fl: %zd), actual alloc: %zd",
            gen_number, gen1_index, gen_number,
            first_alloc, min_first_alloc,
            current_gen_calc->actual_alloc_to_trigger));
    }

    dprintf (BGC_TUNING_LOG, ("BTL%d* %zd, %.3f, %.3f, %.3f, %.3f, %.3f, %zd, %zd, %zd, %zd",
                              gen_number,
                              total_generation_size,
                              current_gen_calc->current_bgc_start_flr,
                              current_gen_calc->current_bgc_sweep_flr,
                              current_bgc_end_data[tuning_data_index].gen_flr,
                              current_gen_stats->last_gen_increase_flr,
                              current_bgc_surv_rate,
                              actual_num_gen1s_to_trigger,
                              num_gen1s_bgc_end,
                              gen_actual_alloc_to_trigger,
                              current_gen_calc->alloc_to_trigger));

    gen1_index_last_bgc_end = gen1_index;

    current_gen_calc->last_bgc_size = total_generation_size;
    current_gen_calc->last_bgc_flr = current_bgc_end_data[tuning_data_index].gen_flr;
    current_gen_calc->last_sweep_above_p = current_sweep_above_p;
    current_gen_calc->last_bgc_end_alloc = current_alloc;

    current_gen_stats->last_bgc_physical_size = data->gen_physical_size;
    current_gen_stats->last_alloc_end_to_start = 0;
    current_gen_stats->last_alloc_start_to_sweep = 0;
    current_gen_stats->last_alloc_sweep_to_end = 0;
    current_gen_stats->last_alloc = current_alloc;
    current_gen_stats->last_bgc_fl_size = current_bgc_end_data[tuning_data_index].gen_fl_size;
    current_gen_stats->last_bgc_surv_rate = current_bgc_surv_rate;
    current_gen_stats->last_gen_increase_flr = 0;
}

// Note that in this method for the !use_this_loop_p generation we will adjust
// its sweep_flr accordingly. And the inner loop will not need to know about this.
void gc_heap::bgc_tuning::init_bgc_end_data (int gen_number, bool use_this_loop_p)
{
    int index = gen_number - max_generation;
    bgc_size_data* data = &current_bgc_end_data[index];

    size_t physical_size = get_total_generation_size (gen_number);
    ptrdiff_t physical_fl_size = get_total_generation_fl_size (gen_number);
    data->gen_actual_phys_fl_size = physical_fl_size;

    if (fl_tuning_triggered && !use_this_loop_p)
    {
        tuning_calculation* current_gen_calc = &gen_calc[gen_number - max_generation];

        if (current_gen_calc->actual_alloc_to_trigger > current_gen_calc->alloc_to_trigger)
        {
            dprintf (BGC_TUNING_LOG, ("BTL%d: gen alloc also exceeded %zd (la: %zd), no action",
                gen_number, current_gen_calc->actual_alloc_to_trigger, current_gen_calc->alloc_to_trigger));
        }
        else
        {
            // We will deduct the missing portion from alloc to fl, simulating that we consumed it.
            size_t remaining_alloc = current_gen_calc->alloc_to_trigger -
                                     current_gen_calc->actual_alloc_to_trigger;

            // now re-calc current_bgc_sweep_flr
            // TODO: note that I am assuming the physical size at sweep was <= end_gen_size_goal which
            // not have been the case.
            size_t gen_size = current_gen_calc->end_gen_size_goal;
            double sweep_flr = current_gen_calc->current_bgc_sweep_flr;
            size_t sweep_fl_size = (size_t)((double)gen_size * sweep_flr / 100.0);

            if (sweep_fl_size < remaining_alloc)
            {
                dprintf (BGC_TUNING_LOG, ("BTL%d: sweep fl %zd < remain alloc %zd", gen_number, sweep_fl_size, remaining_alloc));
                // TODO: this is saying that we didn't have enough fl to accommodate the
                // remaining alloc which is suspicious. To set remaining_alloc to
                // something slightly smaller is only so that we could continue with
                // our calculation but this is something we should look into.
                remaining_alloc = sweep_fl_size - (10 * 1024);
            }

            size_t new_sweep_fl_size = sweep_fl_size - remaining_alloc;
            ptrdiff_t signed_new_sweep_fl_size = sweep_fl_size - remaining_alloc;

            double new_current_bgc_sweep_flr = (double)new_sweep_fl_size * 100.0 / (double)gen_size;
            double signed_new_current_bgc_sweep_flr = (double)signed_new_sweep_fl_size * 100.0 / (double)gen_size;

            dprintf (BGC_TUNING_LOG, ("BTL%d: sg: %zd(%zd), sfl: %zd->%zd(%zd)(%.3f->%.3f(%.3f)), la: %zd, aa: %zd",
                gen_number, gen_size, physical_size, sweep_fl_size,
                new_sweep_fl_size, signed_new_sweep_fl_size,
                sweep_flr, new_current_bgc_sweep_flr, signed_new_current_bgc_sweep_flr,
                current_gen_calc->alloc_to_trigger, current_gen_calc->actual_alloc_to_trigger));

            current_gen_calc->actual_alloc_to_trigger = current_gen_calc->alloc_to_trigger;
            current_gen_calc->current_bgc_sweep_flr = new_current_bgc_sweep_flr;

            // TODO: NOTE this is duplicated in calculate_tuning except I am not * 100.0 here.
            size_t current_bgc_surv_size = get_total_surv_size (gen_number);
            size_t current_bgc_begin_data_size = get_total_begin_data_size (gen_number);
            double current_bgc_surv_rate = (current_bgc_begin_data_size == 0) ?
                                            0 : ((double)current_bgc_surv_size / (double)current_bgc_begin_data_size);

            size_t remaining_alloc_surv = (size_t)((double)remaining_alloc * current_bgc_surv_rate);
            physical_fl_size -= remaining_alloc_surv;
            dprintf (BGC_TUNING_LOG, ("BTL%d: asfl %zd-%zd=%zd, flr %.3f->%.3f, %.3f%% s, fl %zd-%zd->%zd",
                gen_number, sweep_fl_size, remaining_alloc, new_sweep_fl_size,
                sweep_flr, current_gen_calc->current_bgc_sweep_flr,
                (current_bgc_surv_rate * 100.0),
                (physical_fl_size + remaining_alloc_surv),
                remaining_alloc_surv, physical_fl_size));
        }
    }

    double physical_gen_flr = (double)physical_fl_size * 100.0 / (double)physical_size;
    data->gen_physical_size = physical_size;
    data->gen_physical_fl_size = physical_fl_size;
    data->gen_physical_flr = physical_gen_flr;
}

void gc_heap::bgc_tuning::calc_end_bgc_fl (int gen_number)
{
    int index = gen_number - max_generation;
    bgc_size_data* data = &current_bgc_end_data[index];

    tuning_calculation* current_gen_calc = &gen_calc[gen_number - max_generation];

    size_t virtual_size = current_gen_calc->end_gen_size_goal;
    size_t physical_size = data->gen_physical_size;
    ptrdiff_t physical_fl_size = data->gen_physical_fl_size;
    ptrdiff_t virtual_fl_size = (ptrdiff_t)virtual_size - (ptrdiff_t)physical_size;
    ptrdiff_t end_gen_fl_size = physical_fl_size + virtual_fl_size;

    if (end_gen_fl_size < 0)
    {
        end_gen_fl_size = 0;
    }

    data->gen_size = virtual_size;
    data->gen_fl_size = end_gen_fl_size;
    data->gen_flr = (double)(data->gen_fl_size) * 100.0 / (double)(data->gen_size);

    dprintf (BGC_TUNING_LOG, ("BTL%d: vfl: %zd, size %zd->%zd, fl %zd->%zd, flr %.3f->%.3f",
        gen_number, virtual_fl_size,
        data->gen_physical_size, data->gen_size,
        data->gen_physical_fl_size, data->gen_fl_size,
        data->gen_physical_flr, data->gen_flr));
}

// reduce_p is for NGC2s. we want to reduce the ki so we don't overshoot.
double gc_heap::bgc_tuning::calculate_ml_tuning (uint64_t current_available_physical, bool reduce_p,
                                                 ptrdiff_t* _vfl_from_kp, ptrdiff_t* _vfl_from_ki)
{
    ptrdiff_t error = (ptrdiff_t)(current_available_physical - available_memory_goal);

    // This is questionable as gen0/1 and other processes are consuming memory
    // too
    size_t gen2_physical_size = current_bgc_end_data[0].gen_physical_size;
    size_t gen3_physical_size = current_bgc_end_data[1].gen_physical_size;

    double max_output = (double)(total_physical_mem - available_memory_goal -
                                 gen2_physical_size - gen3_physical_size);

    double error_ratio = (double)error / (double)total_physical_mem;

    // do we want this to contribute to the integral term?
    bool include_in_i_p = ((error_ratio > 0.005) || (error_ratio < -0.005));

    dprintf (BGC_TUNING_LOG, ("total phy %zd, mem goal: %zd, curr phy: %zd, g2 phy: %zd, g3 phy: %zd",
            (size_t)total_physical_mem, (size_t)available_memory_goal,
            (size_t)current_available_physical,
            gen2_physical_size, gen3_physical_size));
    dprintf (BGC_TUNING_LOG, ("BTL: Max output: %zd, ER %zd / %zd = %.3f, %s",
            (size_t)max_output,
            error, available_memory_goal, error_ratio,
            (include_in_i_p ? "inc" : "exc")));

    if (include_in_i_p)
    {
        double error_ki = ml_ki * (double)error;
        double temp_accu_error = accu_error + error_ki;
        // anti-windup
        if ((temp_accu_error > 0) && (temp_accu_error < max_output))
            accu_error = temp_accu_error;
        else
        {
            //dprintf (BGC_TUNING_LOG, ("ml accu err + %zd=%zd, exc",
            dprintf (BGC_TUNING_LOG, ("mae + %zd=%zd, exc",
                    (size_t)error_ki, (size_t)temp_accu_error));
        }
    }

    if (reduce_p)
    {
        double saved_accu_error = accu_error;
        accu_error = accu_error * 2.0 / 3.0;
        panic_activated_p = false;
        accu_error_panic = 0;
        dprintf (BGC_TUNING_LOG, ("BTL reduced accu ki %zd->%zd", (ptrdiff_t)saved_accu_error, (ptrdiff_t)accu_error));
    }

    if (panic_activated_p)
        accu_error_panic += (double)error;
    else
        accu_error_panic = 0.0;

    double vfl_from_kp = (double)error * ml_kp;
    double total_virtual_fl_size = vfl_from_kp + accu_error;
    // limit output
    if (total_virtual_fl_size < 0)
    {
        dprintf (BGC_TUNING_LOG, ("BTL vfl %zd < 0", (size_t)total_virtual_fl_size));
        total_virtual_fl_size = 0;
    }
    else if (total_virtual_fl_size > max_output)
    {
        dprintf (BGC_TUNING_LOG, ("BTL vfl %zd > max", (size_t)total_virtual_fl_size));
        total_virtual_fl_size = max_output;
    }

    *_vfl_from_kp = (ptrdiff_t)vfl_from_kp;
    *_vfl_from_ki = (ptrdiff_t)accu_error;
    return total_virtual_fl_size;
}

void gc_heap::bgc_tuning::set_total_gen_sizes (bool use_gen2_loop_p, bool use_gen3_loop_p)
{
    size_t gen2_physical_size = current_bgc_end_data[0].gen_physical_size;
    size_t gen3_physical_size = 0;
    ptrdiff_t gen3_virtual_fl_size = 0;
    gen3_physical_size = current_bgc_end_data[1].gen_physical_size;
    double gen2_size_ratio = (double)gen2_physical_size / ((double)gen2_physical_size + (double)gen3_physical_size);

    // We know how far we are from the memory load goal, assuming that the memory is only
    // used by gen2/3 (which is obviously not the case, but that's why we are not setting the
    // memory goal at 90+%. Assign the memory proportionally to them.
    //
    // We use entry memory load info because that seems to be more closedly correlated to what the VMM decides
    // in memory load.
    uint32_t current_memory_load = settings.entry_memory_load;
    uint64_t current_available_physical = settings.entry_available_physical_mem;

    panic_activated_p = (current_memory_load >= (memory_load_goal + memory_load_goal_slack));

    if (panic_activated_p)
    {
        dprintf (BGC_TUNING_LOG, ("BTL: exceeded slack %zd >= (%zd + %zd)",
            (size_t)current_memory_load, (size_t)memory_load_goal,
            (size_t)memory_load_goal_slack));
    }

    ptrdiff_t vfl_from_kp = 0;
    ptrdiff_t vfl_from_ki = 0;
    double total_virtual_fl_size = calculate_ml_tuning (current_available_physical, false, &vfl_from_kp, &vfl_from_ki);

    if (use_gen2_loop_p || use_gen3_loop_p)
    {
        if (use_gen2_loop_p)
        {
            gen2_ratio_correction += ratio_correction_step;
        }
        else
        {
            gen2_ratio_correction -= ratio_correction_step;
        }

        dprintf (BGC_TUNING_LOG, ("BTL: rc: g2 ratio %.3f%% + %d%% = %.3f%%",
            (gen2_size_ratio * 100.0), (int)(gen2_ratio_correction * 100.0), ((gen2_size_ratio + gen2_ratio_correction) * 100.0)));

        gen2_ratio_correction = min (0.99, gen2_ratio_correction);
        gen2_ratio_correction = max (-0.99, gen2_ratio_correction);

        dprintf (BGC_TUNING_LOG, ("BTL: rc again: g2 ratio %.3f%% + %d%% = %.3f%%",
            (gen2_size_ratio * 100.0), (int)(gen2_ratio_correction * 100.0), ((gen2_size_ratio + gen2_ratio_correction) * 100.0)));

        gen2_size_ratio += gen2_ratio_correction;

        if (gen2_size_ratio <= 0.0)
        {
            gen2_size_ratio = 0.01;
            dprintf (BGC_TUNING_LOG, ("BTL: rc: g2 ratio->0.01"));
        }

        if (gen2_size_ratio >= 1.0)
        {
            gen2_size_ratio = 0.99;
            dprintf (BGC_TUNING_LOG, ("BTL: rc: g2 ratio->0.99"));
        }
    }

    ptrdiff_t gen2_virtual_fl_size = (ptrdiff_t)(total_virtual_fl_size * gen2_size_ratio);
    gen3_virtual_fl_size = (ptrdiff_t)(total_virtual_fl_size * (1.0 - gen2_size_ratio));
    if (gen2_virtual_fl_size < 0)
    {
        ptrdiff_t saved_gen2_virtual_fl_size = gen2_virtual_fl_size;
        ptrdiff_t half_gen2_physical_size = (ptrdiff_t)((double)gen2_physical_size * 0.5);
        if (-gen2_virtual_fl_size > half_gen2_physical_size)
        {
            gen2_virtual_fl_size = -half_gen2_physical_size;
        }

        dprintf (BGC_TUNING_LOG, ("BTL2: n_vfl %zd(%zd)->%zd", saved_gen2_virtual_fl_size, half_gen2_physical_size, gen2_virtual_fl_size));
        gen2_virtual_fl_size = 0;
    }

    if (gen3_virtual_fl_size < 0)
    {
        ptrdiff_t saved_gen3_virtual_fl_size = gen3_virtual_fl_size;
        ptrdiff_t half_gen3_physical_size = (ptrdiff_t)((double)gen3_physical_size * 0.5);
        if (-gen3_virtual_fl_size > half_gen3_physical_size)
        {
            gen3_virtual_fl_size = -half_gen3_physical_size;
        }

        dprintf (BGC_TUNING_LOG, ("BTL3: n_vfl %zd(%zd)->%zd", saved_gen3_virtual_fl_size, half_gen3_physical_size, gen3_virtual_fl_size));
        gen3_virtual_fl_size = 0;
    }

    gen_calc[0].end_gen_size_goal = gen2_physical_size + gen2_virtual_fl_size;
    gen_calc[1].end_gen_size_goal = gen3_physical_size + gen3_virtual_fl_size;

    // We calculate the end info here because the ff in fl servo loop is using this.
    calc_end_bgc_fl (max_generation);
    calc_end_bgc_fl (loh_generation);

#ifdef SIMPLE_DPRINTF
    dprintf (BGC_TUNING_LOG, ("BTL: ml: %d (g: %d)(%s), a: %zd (g: %zd, elg: %zd+%zd=%zd, %zd+%zd=%zd, pi=%zd), vfl: %zd=%zd+%zd",
        current_memory_load, memory_load_goal,
        ((current_available_physical > available_memory_goal) ? "above" : "below"),
        current_available_physical, available_memory_goal,
        gen2_physical_size, gen2_virtual_fl_size, gen_calc[0].end_gen_size_goal,
        gen3_physical_size, gen3_virtual_fl_size, gen_calc[1].end_gen_size_goal,
        (ptrdiff_t)accu_error_panic,
        (ptrdiff_t)total_virtual_fl_size, vfl_from_kp, vfl_from_ki));
#endif //SIMPLE_DPRINTF
}

bool gc_heap::bgc_tuning::should_trigger_ngc2()
{
    return panic_activated_p;
}

// This is our outer ml servo loop where we calculate the control for the inner fl servo loop.
void gc_heap::bgc_tuning::convert_to_fl (bool use_gen2_loop_p, bool use_gen3_loop_p)
{
    size_t current_bgc_count = full_gc_counts[gc_type_background];

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        hp->bgc_maxgen_end_fl_size = generation_free_list_space (hp->generation_of (max_generation));
    }
#else
    bgc_maxgen_end_fl_size = generation_free_list_space (generation_of (max_generation));
#endif //MULTIPLE_HEAPS

    init_bgc_end_data (max_generation, use_gen2_loop_p);
    init_bgc_end_data (loh_generation, use_gen3_loop_p);
    set_total_gen_sizes (use_gen2_loop_p, use_gen3_loop_p);

    dprintf (BGC_TUNING_LOG, ("BTL: gen2 %zd, fl %zd(%.3f)->%zd; gen3 %zd, fl %zd(%.3f)->%zd, %zd BGCs",
        current_bgc_end_data[0].gen_size, current_bgc_end_data[0].gen_fl_size,
        current_bgc_end_data[0].gen_flr, gen_calc[0].end_gen_size_goal,
        current_bgc_end_data[1].gen_size, current_bgc_end_data[1].gen_fl_size,
        current_bgc_end_data[1].gen_flr, gen_calc[1].end_gen_size_goal,
        current_bgc_count));
}

void gc_heap::bgc_tuning::record_and_adjust_bgc_end()
{
    if (!bgc_tuning::enable_fl_tuning)
        return;

    uint64_t elapsed_time_so_far = GetHighPrecisionTimeStamp() - process_start_time;
    size_t current_gen1_index = get_current_gc_index (max_generation - 1);
    dprintf (BGC_TUNING_LOG, ("BTL: g2t[en][g1 %zd]: %0.3f minutes",
        current_gen1_index,
        (double)elapsed_time_so_far / (double)1000000 / (double)60));

    if (fl_tuning_triggered)
    {
        num_bgcs_since_tuning_trigger++;
    }

    bool use_gen2_loop_p = (settings.reason == reason_bgc_tuning_soh);
    bool use_gen3_loop_p = (settings.reason == reason_bgc_tuning_loh);
    dprintf (BGC_TUNING_LOG, ("BTL: reason: %d, gen2 loop: %s; gen3 loop: %s, promoted %zd bytes",
        (((settings.reason != reason_bgc_tuning_soh) && (settings.reason != reason_bgc_tuning_loh)) ?
            saved_bgc_tuning_reason : settings.reason),
        (use_gen2_loop_p ? "yes" : "no"),
        (use_gen3_loop_p ? "yes" : "no"),
        get_total_bgc_promoted()));

    convert_to_fl (use_gen2_loop_p, use_gen3_loop_p);

    calculate_tuning (max_generation, true);

    if (total_uoh_a_last_bgc > 0)
    {
        calculate_tuning (loh_generation, true);
    }
    else
    {
        dprintf (BGC_TUNING_LOG, ("BTL: gen3 not allocated"));
    }

    if (next_bgc_p)
    {
        next_bgc_p = false;
        fl_tuning_triggered = true;
        dprintf (BGC_TUNING_LOG, ("BTL: FL tuning ENABLED!!!"));
    }

    saved_bgc_tuning_reason = -1;
}

#endif //BGC_SERVO_TUNING
#endif //BACKGROUND_GC

void gc_heap::set_static_data()
{
    static_data* pause_mode_sdata = static_data_table[latency_level];
    for (int i = 0; i < total_generation_count; i++)
    {
        dynamic_data* dd = dynamic_data_of (i);
        static_data* sdata = &pause_mode_sdata[i];

        dd->sdata = sdata;
        dd->min_size = sdata->min_size;

        dprintf (GTC_LOG, ("PM: %d, gen%d:  min: %zd, max: %zd, fr_l: %zd, fr_b: %d%%",
            settings.pause_mode,i,
            dd->min_size, dd_max_size (dd),
            sdata->fragmentation_limit, (int)(sdata->fragmentation_burden_limit * 100)));
    }
}

// Initialize the values that are not const.
void gc_heap::init_static_data()
{
    size_t gen0_min_size = get_gen0_min_size();

    size_t gen0_max_size = 0;

    size_t gen0_max_size_config = (size_t)GCConfig::GetGCGen0MaxBudget();

    if (gen0_max_size_config)
    {
        gen0_max_size = gen0_max_size_config;

#ifdef FEATURE_EVENT_TRACE
        gen0_max_budget_from_config = gen0_max_size;
#endif //FEATURE_EVENT_TRACE
    }
    else
    {
        gen0_max_size =
#ifdef MULTIPLE_HEAPS
            max ((size_t)6 * 1024 * 1024, min (Align(soh_segment_size / 2), (size_t)200 * 1024 * 1024));
#else //MULTIPLE_HEAPS
            (
#ifdef BACKGROUND_GC
                gc_can_use_concurrent ?
                6 * 1024 * 1024 :
#endif //BACKGROUND_GC
                max ((size_t)6 * 1024 * 1024, min (Align(soh_segment_size / 2), (size_t)200 * 1024 * 1024))
                );
#endif //MULTIPLE_HEAPS

        gen0_max_size = max (gen0_min_size, gen0_max_size);

        if (heap_hard_limit)
        {
            size_t gen0_max_size_seg = soh_segment_size / 4;
            dprintf (GTC_LOG, ("limit gen0 max %zd->%zd", gen0_max_size, gen0_max_size_seg));
            gen0_max_size = min (gen0_max_size, gen0_max_size_seg);
        }
    }

    gen0_max_size = Align (gen0_max_size);
    gen0_min_size = min (gen0_min_size, gen0_max_size);

    GCConfig::SetGCGen0MaxBudget (gen0_max_size);

    // TODO: gen0_max_size has a 200mb cap; gen1_max_size should also have a cap.
    size_t gen1_max_size = (size_t)
#ifdef MULTIPLE_HEAPS
        max ((size_t)6*1024*1024, Align(soh_segment_size/2));
#else //MULTIPLE_HEAPS
        (
#ifdef BACKGROUND_GC
            gc_can_use_concurrent ?
            6*1024*1024 :
#endif //BACKGROUND_GC
            max ((size_t)6*1024*1024, Align(soh_segment_size/2))
        );
#endif //MULTIPLE_HEAPS

#ifndef HOST_64BIT
    if (heap_hard_limit)
    {
        size_t gen1_max_size_seg = soh_segment_size / 2;
        dprintf (GTC_LOG, ("limit gen1 max %zd->%zd", gen1_max_size, gen1_max_size_seg));
        gen1_max_size = min (gen1_max_size, gen1_max_size_seg);
    }
#endif //!HOST_64BIT

    size_t gen1_max_size_config = (size_t)GCConfig::GetGCGen1MaxBudget();

    if (gen1_max_size_config)
    {
        gen1_max_size = min (gen1_max_size, gen1_max_size_config);
    }

    gen1_max_size = Align (gen1_max_size);

    dprintf (GTC_LOG, ("gen0 min: %zd, max: %zd, gen1 max: %zd",
        gen0_min_size, gen0_max_size, gen1_max_size));

    for (int i = latency_level_first; i <= latency_level_last; i++)
    {
        static_data_table[i][0].min_size = gen0_min_size;
        static_data_table[i][0].max_size = gen0_max_size;
        static_data_table[i][1].max_size = gen1_max_size;
    }

#ifdef DYNAMIC_HEAP_COUNT
    if (gc_heap::dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
    {
        gc_heap::dynamic_heap_count_data.min_gen0_new_allocation = gen0_min_size;
        if (gen0_max_size_config)
        {
            gc_heap::dynamic_heap_count_data.max_gen0_new_allocation = gen0_max_size;
        }
    }
#endif //DYNAMIC_HEAP_COUNT
}

bool gc_heap::init_dynamic_data()
{
    uint64_t now_raw_ts = RawGetHighPrecisionTimeStamp ();
#ifdef HEAP_BALANCE_INSTRUMENTATION
    start_raw_ts = now_raw_ts;
#endif //HEAP_BALANCE_INSTRUMENTATION
    uint64_t now = (uint64_t)((double)now_raw_ts * qpf_us);

    set_static_data();

    if (heap_number == 0)
    {
        process_start_time = now;
        smoothed_desired_total[0] = dynamic_data_of (0)->min_size * n_heaps;
#ifdef DYNAMIC_HEAP_COUNT
        last_suspended_end_time = now;
#endif //DYNAMIC_HEAP_COUNT
#ifdef HEAP_BALANCE_INSTRUMENTATION
        last_gc_end_time_us = now;
        dprintf (HEAP_BALANCE_LOG, ("qpf=%zd, start: %zd(%d)", qpf, start_raw_ts, now));
#endif //HEAP_BALANCE_INSTRUMENTATION
    }

    for (int i = 0; i < total_generation_count; i++)
    {
        dynamic_data* dd = dynamic_data_of (i);
        dd->gc_clock = 0;
        dd->time_clock = now;
        dd->previous_time_clock = now;
        dd->current_size = 0;
        dd->promoted_size = 0;
        dd->collection_count = 0;
        dd->new_allocation = dd->min_size;
        dd->gc_new_allocation = dd->new_allocation;
        dd->desired_allocation = dd->new_allocation;
        dd->fragmentation = 0;
    }

    return true;
}

float gc_heap::surv_to_growth (float cst, float limit, float max_limit)
{
    if (cst < ((max_limit - limit ) / (limit * (max_limit-1.0f))))
        return ((limit - limit*cst) / (1.0f - (cst * limit)));
    else
        return max_limit;
}

//if the allocation budget wasn't exhausted, the new budget may be wrong because the survival may
//not be correct (collection happened too soon). Correct with a linear estimation based on the previous
//value of the budget
static size_t linear_allocation_model (float allocation_fraction, size_t new_allocation,
                                       size_t previous_desired_allocation, float time_since_previous_collection_secs)
{
    if ((allocation_fraction < 0.95) && (allocation_fraction > 0.0))
    {
        const float decay_time = 5*60.0f; // previous desired allocation expires over 5 minutes
        float decay_factor = (decay_time <= time_since_previous_collection_secs) ?
                                0 :
                                ((decay_time - time_since_previous_collection_secs) / decay_time);
        float previous_allocation_factor = (1.0f - allocation_fraction) * decay_factor;
        dprintf (2, ("allocation fraction: %d, decay factor: %d, previous allocation factor: %d",
            (int)(allocation_fraction*100.0), (int)(decay_factor*100.0), (int)(previous_allocation_factor*100.0)));
        new_allocation = (size_t)((1.0 - previous_allocation_factor)*new_allocation + previous_allocation_factor * previous_desired_allocation);
    }
    return new_allocation;
}

size_t gc_heap::desired_new_allocation (dynamic_data* dd,
                                        size_t out, int gen_number,
                                        int pass)
{
    gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();

    if (dd_begin_data_size (dd) == 0)
    {
        size_t new_allocation = dd_min_size (dd);
        current_gc_data_per_heap->gen_data[gen_number].new_allocation = new_allocation;
        return new_allocation;
    }
    else
    {
        float     cst;
        size_t    previous_desired_allocation = dd_desired_allocation (dd);
        size_t    current_size = dd_current_size (dd);
        float     max_limit = dd_max_limit (dd);
        float     limit = dd_limit (dd);
        size_t    min_gc_size = dd_min_size (dd);
        float     f = 0;
        size_t    max_size = dd_max_size (dd);
        size_t    new_allocation = 0;
        float     time_since_previous_collection_secs = (dd_time_clock (dd) - dd_previous_time_clock (dd))*1e-6f;
        float allocation_fraction = (float) (dd_desired_allocation (dd) - dd_gc_new_allocation (dd)) / (float) (dd_desired_allocation (dd));

        if (gen_number >= max_generation)
        {
            size_t    new_size = 0;

            cst = min (1.0f, float (out) / float (dd_begin_data_size (dd)));

            f = surv_to_growth (cst, limit, max_limit);
            if (conserve_mem_setting != 0)
            {
                // if this is set, compute a growth factor based on it.
                // example: a setting of 6 means we have a goal of 60% live data
                // this means we allow 40% fragmentation
                // to keep heap size stable, we only use half of that (20%) for new allocation
                // f is (live data + new allocation)/(live data), so would be (60% + 20%) / 60% or 1.33
                float f_conserve = ((10.0f / conserve_mem_setting) - 1) * 0.5f + 1.0f;

                // use the smaller one
                f = min (f, f_conserve);
            }

            size_t max_growth_size = (size_t)(max_size / f);
            if (current_size >= max_growth_size)
            {
                new_size = max_size;
            }
            else
            {
                new_size = (size_t) min (max ( (size_t)(f * current_size), min_gc_size), max_size);
            }

            assert ((new_size >= current_size) || (new_size == max_size));

            if (gen_number == max_generation)
            {
                new_allocation  =  max((new_size - current_size), min_gc_size);

                new_allocation = linear_allocation_model (allocation_fraction, new_allocation,
                                                          dd_desired_allocation (dd), time_since_previous_collection_secs);

                if (
#ifdef BGC_SERVO_TUNING
                    !bgc_tuning::fl_tuning_triggered &&
#endif //BGC_SERVO_TUNING
                    (conserve_mem_setting == 0) &&
                    (dd_fragmentation (dd) > ((size_t)((f-1)*current_size))))
                {
                    //reducing allocation in case of fragmentation
                    size_t new_allocation1 = max (min_gc_size,
                                                  // CAN OVERFLOW
                                                  (size_t)((float)new_allocation * current_size /
                                                           ((float)current_size + 2*dd_fragmentation (dd))));
                    dprintf (2, ("Reducing max_gen allocation due to fragmentation from %zd to %zd",
                                 new_allocation, new_allocation1));
                    new_allocation = new_allocation1;
                }
            }
            else // not a SOH generation
            {
                uint32_t memory_load = 0;
                uint64_t available_physical = 0;
                get_memory_info (&memory_load, &available_physical);
#ifdef TRACE_GC
                if (heap_hard_limit)
                {
                    size_t allocated = 0;
                    size_t committed = uoh_committed_size (gen_number, &allocated);
                    dprintf (2, ("GC#%zd h%d, GMI: UOH budget, UOH commit %zd (obj %zd, frag %zd), total commit: %zd (recorded: %zd)",
                        (size_t)settings.gc_index, heap_number,
                        committed, allocated,
                        dd_fragmentation (dynamic_data_of (gen_number)),
                        get_total_committed_size(), (current_total_committed - current_total_committed_bookkeeping)));
                }
#endif //TRACE_GC
                if (heap_number == 0)
                    settings.exit_memory_load = memory_load;
                if (available_physical > 1024*1024)
                    available_physical -= 1024*1024;

                uint64_t available_free = available_physical + (uint64_t)generation_free_list_space (generation_of (gen_number));
                if (available_free > (uint64_t)MAX_PTR)
                {
                    available_free = (uint64_t)MAX_PTR;
                }

                //try to avoid OOM during large object allocation
                new_allocation = max (min(max((new_size - current_size), dd_desired_allocation (dynamic_data_of (max_generation))),
                                          (size_t)available_free),
                                      max ((current_size/4), min_gc_size));

                new_allocation = linear_allocation_model (allocation_fraction, new_allocation,
                                                          dd_desired_allocation (dd), time_since_previous_collection_secs);

            }
        }
        else
        {
            size_t survivors = out;
            cst = float (survivors) / float (dd_begin_data_size (dd));
            f = surv_to_growth (cst, limit, max_limit);
            new_allocation = (size_t) min (max ((size_t)(f * (survivors)), min_gc_size), max_size);

            new_allocation = linear_allocation_model (allocation_fraction, new_allocation,
                                                      dd_desired_allocation (dd), time_since_previous_collection_secs);

#ifdef DYNAMIC_HEAP_COUNT
            if (dynamic_adaptation_mode != dynamic_adaptation_to_application_sizes)
#endif //DYNAMIC_HEAP_COUNT
            {
                if (gen_number == 0)
                {
                    if (pass == 0)
                    {
                        size_t free_space = generation_free_list_space (generation_of (gen_number));
                        // DTREVIEW - is min_gc_size really a good choice?
                        // on 64-bit this will almost always be true.
                        dprintf (GTC_LOG, ("frag: %zd, min: %zd", free_space, min_gc_size));
                        if (free_space > min_gc_size)
                        {
                            settings.gen0_reduction_count = 2;
                        }
                        else
                        {
                            if (settings.gen0_reduction_count > 0)
                                settings.gen0_reduction_count--;
                        }
                    }
                    if (settings.gen0_reduction_count > 0)
                    {
                        dprintf (2, ("Reducing new allocation based on fragmentation"));
                        new_allocation = min (new_allocation,
                                              max (min_gc_size, (max_size/3)));
                    }
                }
            }
        }

        size_t new_allocation_ret = Align (new_allocation, get_alignment_constant (gen_number <= max_generation));
        int gen_data_index = gen_number;
        gc_generation_data* gen_data = &(current_gc_data_per_heap->gen_data[gen_data_index]);
        gen_data->new_allocation = new_allocation_ret;

        dd_surv (dd) = cst;

        dprintf (2, (ThreadStressLog::gcDesiredNewAllocationMsg(),
                    heap_number, gen_number, out, current_size, (dd_desired_allocation (dd) - dd_gc_new_allocation (dd)),
                    (int)(cst*100), (int)(f*100), current_size + new_allocation, new_allocation));

        return new_allocation_ret;
    }
}

#ifdef HOST_64BIT
inline
size_t gc_heap::trim_youngest_desired (uint32_t memory_load,
                                       size_t total_new_allocation,
                                       size_t total_min_allocation)
{
    if (memory_load < MAX_ALLOWED_MEM_LOAD)
    {
        // If the total of memory load and gen0 budget exceeds
        // our max memory load limit, trim the gen0 budget so the total
        // is the max memory load limit.
        size_t remain_memory_load = (MAX_ALLOWED_MEM_LOAD - memory_load) * mem_one_percent;
        return min (total_new_allocation, remain_memory_load);
    }
    else
    {
        size_t total_max_allocation = max ((size_t)mem_one_percent, total_min_allocation);
        return min (total_new_allocation, total_max_allocation);
    }
}

size_t gc_heap::joined_youngest_desired (size_t new_allocation)
{
    dprintf (2, ("Entry memory load: %d; gen0 new_alloc: %zd", settings.entry_memory_load, new_allocation));

    size_t final_new_allocation = new_allocation;
    if (new_allocation > MIN_YOUNGEST_GEN_DESIRED)
    {
        uint32_t num_heaps = 1;

#ifdef MULTIPLE_HEAPS
        num_heaps = gc_heap::n_heaps;
#endif //MULTIPLE_HEAPS

        size_t total_new_allocation = new_allocation * num_heaps;
        size_t total_min_allocation = (size_t)MIN_YOUNGEST_GEN_DESIRED * num_heaps;

        if ((settings.entry_memory_load >= MAX_ALLOWED_MEM_LOAD) ||
            (total_new_allocation > max (youngest_gen_desired_th, total_min_allocation)))
        {
            uint32_t memory_load = 0;
            get_memory_info (&memory_load);
            settings.exit_memory_load = memory_load;
            dprintf (2, ("Current memory load: %d", memory_load));

            size_t final_total =
                trim_youngest_desired (memory_load, total_new_allocation, total_min_allocation);
            size_t max_new_allocation =
#ifdef MULTIPLE_HEAPS
                                         dd_max_size (g_heaps[0]->dynamic_data_of (0));
#else //MULTIPLE_HEAPS
                                         dd_max_size (dynamic_data_of (0));
#endif //MULTIPLE_HEAPS

            final_new_allocation  = min (Align ((final_total / num_heaps), get_alignment_constant (TRUE)), max_new_allocation);
        }
    }

    if (final_new_allocation < new_allocation)
    {
        settings.gen0_reduction_count = 2;
    }

    return final_new_allocation;
}

#endif //HOST_64BIT

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

void gc_heap::compute_new_dynamic_data (int gen_number)
{
    _ASSERTE(gen_number >= 0);
    _ASSERTE(gen_number <= max_generation);

    dynamic_data* dd = dynamic_data_of (gen_number);
    generation*   gen = generation_of (gen_number);
    size_t        in = (gen_number==0) ? 0 : compute_in (gen_number);

    size_t total_gen_size = generation_size (gen_number);
    //keep track of fragmentation
    dd_fragmentation (dd) = generation_free_list_space (gen) + generation_free_obj_space (gen);

    // We need to reset the condemned alloc for the condemned generation because it will participate in the free list efficiency
    // calculation. And if a generation is condemned, it means all the allocations into this generation during that GC will be
    // condemned and it wouldn't make sense to use this value to calculate the FL efficiency since at this point the FL hasn't
    // been built.
    generation_condemned_allocated (gen) = 0;

    if (settings.concurrent)
    {
        // For BGC we could have non zero values due to gen1 FGCs. We reset all 3 allocs to start anew.
        generation_free_list_allocated (gen) = 0;
        generation_end_seg_allocated (gen) = 0;
    }
    else
    {
        assert (generation_free_list_allocated (gen) == 0);
        assert (generation_end_seg_allocated (gen) == 0);
    }

    // make sure the subtraction below doesn't overflow
    if (dd_fragmentation (dd) <= total_gen_size)
        dd_current_size (dd) = total_gen_size - dd_fragmentation (dd);
    else
        dd_current_size (dd) = 0;

    gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();

    size_t out = dd_survived_size (dd);

    gc_generation_data* gen_data = &(current_gc_data_per_heap->gen_data[gen_number]);
    gen_data->size_after = total_gen_size;
    gen_data->free_list_space_after = generation_free_list_space (gen);
    gen_data->free_obj_space_after = generation_free_obj_space (gen);

    if ((settings.pause_mode == pause_low_latency) && (gen_number <= 1))
    {
        // When we are in the low latency mode, we can still be
        // condemning more than gen1's 'cause of induced GCs.
        dd_desired_allocation (dd) = low_latency_alloc;
        dd_gc_new_allocation (dd) = dd_desired_allocation (dd);
        dd_new_allocation (dd) = dd_gc_new_allocation (dd);
    }
    else
    {
        if (gen_number == 0)
        {
            //compensate for dead finalizable objects promotion.
            //they shouldn't be counted for growth.
            size_t final_promoted = 0;
            final_promoted = min (finalization_promoted_bytes, out);
            // Prefast: this is clear from above but prefast needs to be told explicitly
            _ASSERTE(final_promoted <= out);

            dprintf (2, ("gen: %d final promoted: %zd", gen_number, final_promoted));
            dd_freach_previous_promotion (dd) = final_promoted;
            size_t lower_bound = desired_new_allocation  (dd, out-final_promoted, gen_number, 0);

            if (settings.condemned_generation == 0)
            {
                //there is no noise.
                dd_desired_allocation (dd) = lower_bound;
            }
            else
            {
                size_t higher_bound = desired_new_allocation (dd, out, gen_number, 1);

                // <TODO>This assert was causing AppDomains\unload\test1n\test1nrun.bat to fail</TODO>
                //assert ( lower_bound <= higher_bound);

                //discount the noise. Change the desired allocation
                //only if the previous value is outside of the range.
                if (dd_desired_allocation (dd) < lower_bound)
                {
                    dd_desired_allocation (dd) = lower_bound;
                }
                else if (dd_desired_allocation (dd) > higher_bound)
                {
                    dd_desired_allocation (dd) = higher_bound;
                }
#if defined (HOST_64BIT) && !defined (MULTIPLE_HEAPS)
                dd_desired_allocation (dd) = joined_youngest_desired (dd_desired_allocation (dd));
#endif // HOST_64BIT && !MULTIPLE_HEAPS
                trim_youngest_desired_low_memory();
                dprintf (2, ("final gen0 new_alloc: %zd", dd_desired_allocation (dd)));
            }
        }
        else
        {
            dd_desired_allocation (dd) = desired_new_allocation (dd, out, gen_number, 0);
        }
        dd_gc_new_allocation (dd) = dd_desired_allocation (dd);

#ifdef USE_REGIONS
        // we may have had some incoming objects during this GC -
        // adjust the consumed budget for these
        dd_new_allocation (dd) = dd_gc_new_allocation (dd) - in;
#else //USE_REGIONS
        // for segments, we want to keep the .NET 6.0 behavior where we did not adjust
        dd_new_allocation (dd) = dd_gc_new_allocation (dd);
#endif //USE_REGIONS
    }

    gen_data->pinned_surv = dd_pinned_survived_size (dd);
    gen_data->npinned_surv = dd_survived_size (dd) - dd_pinned_survived_size (dd);

    dd_promoted_size (dd) = out;
    if (gen_number == max_generation)
    {
        for (int i = (gen_number + 1); i < total_generation_count; i++)
        {
            dd = dynamic_data_of (i);
            total_gen_size = generation_size (i);
            generation* gen = generation_of (i);
            dd_fragmentation (dd) = generation_free_list_space (gen) +
                generation_free_obj_space (gen);
            dd_current_size (dd) = total_gen_size - dd_fragmentation (dd);
            dd_survived_size (dd) = dd_current_size (dd);
            in = 0;
            out = dd_current_size (dd);
            dd_desired_allocation (dd) = desired_new_allocation (dd, out, i, 0);
            dd_gc_new_allocation (dd) = Align (dd_desired_allocation (dd),
                get_alignment_constant (FALSE));
            dd_new_allocation (dd) = dd_gc_new_allocation (dd);

            gen_data = &(current_gc_data_per_heap->gen_data[i]);
            gen_data->size_after = total_gen_size;
            gen_data->free_list_space_after = generation_free_list_space (gen);
            gen_data->free_obj_space_after = generation_free_obj_space (gen);
            gen_data->npinned_surv = out;
#ifdef BACKGROUND_GC
            end_uoh_size[i - uoh_start_generation] = total_gen_size;
#endif //BACKGROUND_GC
            dd_promoted_size (dd) = out;
        }
    }
}

void gc_heap::trim_youngest_desired_low_memory()
{
    if (g_low_memory_status)
    {
        size_t committed_mem = committed_size();
        dynamic_data* dd = dynamic_data_of (0);
        size_t current = dd_desired_allocation (dd);
        size_t candidate = max (Align ((committed_mem / 10), get_alignment_constant(FALSE)), dd_min_size (dd));

        dd_desired_allocation (dd) = min (current, candidate);
    }
}

ptrdiff_t gc_heap::estimate_gen_growth (int gen_number)
{
    dynamic_data* dd_gen = dynamic_data_of (gen_number);
    generation *gen = generation_of (gen_number);
    ptrdiff_t new_allocation_gen = dd_new_allocation (dd_gen);
    ptrdiff_t free_list_space_gen = generation_free_list_space (gen);

#ifdef USE_REGIONS
    // in the case of regions, we assume all the space up to reserved gets used before we get a new region for this gen
    ptrdiff_t reserved_not_in_use = 0;
    ptrdiff_t allocated_gen = 0;

    for (heap_segment* region = generation_start_segment_rw (gen); region != nullptr; region = heap_segment_next (region))
    {
        allocated_gen += heap_segment_allocated (region) - heap_segment_mem (region);
        reserved_not_in_use += heap_segment_reserved (region) - heap_segment_allocated (region);
    }

    // compute how much of the allocated space is on the free list
    double free_list_fraction_gen = (allocated_gen == 0) ? 0.0 : (double)(free_list_space_gen) / (double)allocated_gen;

    // estimate amount of usable free space
    // e.g. if 90% of the allocated space is free, assume 90% of these 90% can get used
    // e.g. if 10% of the allocated space is free, assume 10% of these 10% can get used
    ptrdiff_t usable_free_space = (ptrdiff_t)(free_list_fraction_gen * free_list_space_gen);

    ptrdiff_t budget_gen = new_allocation_gen - usable_free_space - reserved_not_in_use;

    dprintf (REGIONS_LOG, ("h%2d gen %d budget %zd allocated: %zd, FL: %zd, reserved_not_in_use %zd budget_gen %zd",
        heap_number, gen_number, new_allocation_gen, allocated_gen, free_list_space_gen, reserved_not_in_use, budget_gen));

#else  //USE_REGIONS
    // estimate how we are going to need in this generation - estimate half the free list space gets used
    ptrdiff_t budget_gen = new_allocation_gen - (free_list_space_gen / 2);
    dprintf (REGIONS_LOG, ("budget for gen %d on heap %d is %zd (new %zd, free %zd)",
        gen_number, heap_number, budget_gen, new_allocation_gen, free_list_space_gen));
#endif //USE_REGIONS

    return budget_gen;
}

#if !defined(USE_REGIONS) || defined(MULTIPLE_HEAPS)
uint8_t* gc_heap::get_smoothed_decommit_target (uint8_t* previous_decommit_target, uint8_t* new_decommit_target, heap_segment* seg)
{
    uint8_t* decommit_target = new_decommit_target;
    if (decommit_target < previous_decommit_target)
    {
        // we used to have a higher target - do exponential smoothing by computing
        // essentially decommit_target = 1/3*decommit_target + 2/3*previous_decommit_target
        // computation below is slightly different to avoid overflow
        ptrdiff_t target_decrease = previous_decommit_target - decommit_target;
        decommit_target += target_decrease * 2 / 3;
    }

#ifdef STRESS_DECOMMIT
    // our decommit logic should work for a random decommit target within tail_region - make sure it does
    decommit_target = heap_segment_mem (seg) + gc_rand::get_rand (heap_segment_reserved (seg) - heap_segment_mem (seg));
#endif //STRESS_DECOMMIT

#ifdef MULTIPLE_HEAPS
    if (decommit_target < heap_segment_committed (seg))
    {
        gradual_decommit_in_progress_p = TRUE;
    }
#endif //MULTIPLE_HEAPS

    int gen_num =
#ifdef USE_REGIONS
        seg->gen_num;
#else
        0;
#endif
    dprintf (3, ("h%2d gen %d allocated: %zdkb committed: %zdkb target: %zdkb",
        heap_number,
        gen_num,
        ((heap_segment_allocated (seg) - heap_segment_mem (seg)) / 1024),
        ((heap_segment_committed (seg) - heap_segment_mem (seg)) / 1024),
        (heap_segment_decommit_target (seg) - heap_segment_mem (seg)) / 1024));

    return decommit_target;
}

#endif
#ifdef BGC_SERVO_TUNING
// virtual_fl_size is only used for NGC2
void gc_heap::check_and_adjust_bgc_tuning (int gen_number, size_t physical_size, ptrdiff_t virtual_fl_size)
{
    // For LOH we need to check more often to catch things like when the size grows too much.
    int min_gen_to_check = ((gen_number == max_generation) ? (max_generation - 1) : 0);

    if (settings.condemned_generation >= min_gen_to_check)
    {
#ifdef MULTIPLE_HEAPS
        gc_heap* hp = g_heaps[0];
#else
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        size_t total_gen_size = physical_size;
        size_t total_generation_fl_size = get_total_generation_fl_size (gen_number);
        double gen_flr = (double)total_generation_fl_size * 100.0 / (double)total_gen_size;
        size_t gen1_index = dd_collection_count (hp->dynamic_data_of (max_generation - 1));
        size_t gen2_index = dd_collection_count (hp->dynamic_data_of (max_generation));

        bgc_tuning::tuning_calculation* current_gen_calc = &bgc_tuning::gen_calc[gen_number - max_generation];
        bgc_tuning::tuning_stats* current_gen_stats = &bgc_tuning::gen_stats[gen_number - max_generation];

        bool gen_size_inc_p = (total_gen_size > current_gen_calc->last_bgc_size);

        if ((settings.condemned_generation >= min_gen_to_check) &&
            (settings.condemned_generation != max_generation))
        {
            if (gen_size_inc_p)
            {
                current_gen_stats->last_gen_increase_flr = gen_flr;
                dprintf (BGC_TUNING_LOG, ("BTLp[g1: %zd, g2: %zd]: gen%d size inc %s %zd->%zd, flr: %.3f",
                        gen1_index, gen2_index, gen_number,
                        (gc_heap::background_running_p() ? "during bgc" : ""),
                        current_gen_stats->last_bgc_physical_size, total_gen_size, gen_flr));
            }

            if (!bgc_tuning::fl_tuning_triggered)
            {
                if (bgc_tuning::enable_fl_tuning)
                {
                    if (!((gc_heap::background_running_p() || (hp->current_bgc_state == bgc_initialized))))
                    {
                        assert (settings.entry_memory_load);

                        // We start when we are 2/3 way there so we don't overshoot.
                        if ((settings.entry_memory_load >= (bgc_tuning::memory_load_goal * 2 / 3)) &&
                            (full_gc_counts[gc_type_background] >= 2))
                        {
                            bgc_tuning::next_bgc_p = true;
                            current_gen_calc->first_alloc_to_trigger = get_total_servo_alloc (gen_number);
                            dprintf (BGC_TUNING_LOG, ("BTL[g1: %zd] mem high enough: %d(goal: %d), gen%d fl alloc: %zd, trigger BGC!",
                                gen1_index, settings.entry_memory_load, bgc_tuning::memory_load_goal,
                                gen_number, current_gen_calc->first_alloc_to_trigger));
                        }
                    }
                }
            }
        }

        if ((settings.condemned_generation == max_generation) && !(settings.concurrent))
        {
            size_t total_survived = get_total_surv_size (gen_number);
            size_t total_begin = get_total_begin_data_size (gen_number);
            double current_gc_surv_rate = (double)total_survived * 100.0 / (double)total_begin;

            // calculate the adjusted gen_flr.
            double total_virtual_size = (double)physical_size + (double)virtual_fl_size;
            double total_fl_size = (double)total_generation_fl_size + (double)virtual_fl_size;
            double new_gen_flr = total_fl_size * 100.0 / total_virtual_size;

            dprintf (BGC_TUNING_LOG, ("BTL%d NGC2 size %zd->%zd, fl %zd(%.3f)->%zd(%.3f)",
                gen_number, physical_size, (size_t)total_virtual_size,
                total_generation_fl_size, gen_flr,
                (size_t)total_fl_size, new_gen_flr));

            dprintf (BGC_TUNING_LOG, ("BTL%d* %zd, %.3f, %.3f, %.3f, %.3f, %.3f, %d, %d, %d, %zd",
                                    gen_number,
                                    (size_t)total_virtual_size,
                                    0.0,
                                    0.0,
                                    new_gen_flr,
                                    current_gen_stats->last_gen_increase_flr,
                                    current_gc_surv_rate,
                                    0,
                                    0,
                                    0,
                                    current_gen_calc->alloc_to_trigger));

            bgc_tuning::gen1_index_last_bgc_end = gen1_index;

            current_gen_calc->last_bgc_size = total_gen_size;
            current_gen_calc->last_bgc_flr = new_gen_flr;
            current_gen_calc->last_sweep_above_p = false;
            current_gen_calc->last_bgc_end_alloc = 0;

            current_gen_stats->last_alloc_end_to_start = 0;
            current_gen_stats->last_alloc_start_to_sweep = 0;
            current_gen_stats->last_alloc_sweep_to_end = 0;
            current_gen_stats->last_bgc_fl_size = total_generation_fl_size;
            current_gen_stats->last_bgc_surv_rate = current_gc_surv_rate;
            current_gen_stats->last_gen_increase_flr = 0;
        }
    }
}

#endif //BGC_SERVO_TUNING
#ifdef BACKGROUND_GC
void gc_heap::get_and_reset_uoh_alloc_info()
{
    total_uoh_a_last_bgc = 0;

    uint64_t total_uoh_a_no_bgc = 0;
    uint64_t total_uoh_a_bgc_marking = 0;
    uint64_t total_uoh_a_bgc_planning = 0;
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        // We need to adjust size_before for UOH allocations that occurred during marking
        // before we lose the values here.
        gc_history_per_heap* current_gc_data_per_heap = hp->get_gc_data_per_heap();
        // loh/poh_a_bgc_planning should be the same as they were when init_records set size_before.
        for (int i = uoh_start_generation; i < total_generation_count; i++)
        {
            current_gc_data_per_heap->gen_data[i].size_before += hp->uoh_a_bgc_marking[i - uoh_start_generation];

            total_uoh_a_no_bgc += hp->uoh_a_no_bgc[i - uoh_start_generation];
            hp->uoh_a_no_bgc[i - uoh_start_generation] = 0;

            total_uoh_a_bgc_marking += hp->uoh_a_bgc_marking[i - uoh_start_generation];
            hp->uoh_a_bgc_marking[i - uoh_start_generation] = 0;

            total_uoh_a_bgc_planning += hp->uoh_a_bgc_planning[i - uoh_start_generation];
            hp->uoh_a_bgc_planning[i - uoh_start_generation] = 0;
        }
    }
    dprintf (2, ("LOH alloc: outside bgc: %zd; bm: %zd; bp: %zd",
        total_uoh_a_no_bgc,
        total_uoh_a_bgc_marking,
        total_uoh_a_bgc_planning));

    total_uoh_a_last_bgc = total_uoh_a_no_bgc + total_uoh_a_bgc_marking + total_uoh_a_bgc_planning;
}

#endif //BACKGROUND_GC

bool gc_heap::is_pm_ratio_exceeded()
{
    size_t maxgen_frag = 0;
    size_t maxgen_size = 0;
    size_t total_heap_size = get_total_heap_size();

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        maxgen_frag += dd_fragmentation (hp->dynamic_data_of (max_generation));
        maxgen_size += hp->generation_size (max_generation);
    }

    double maxgen_ratio = (double)maxgen_size / (double)total_heap_size;
    double maxgen_frag_ratio = (double)maxgen_frag / (double)maxgen_size;
    dprintf (GTC_LOG, ("maxgen %zd(%d%% total heap), frag: %zd (%d%% maxgen)",
        maxgen_size, (int)(maxgen_ratio * 100.0),
        maxgen_frag, (int)(maxgen_frag_ratio * 100.0)));

    bool maxgen_highfrag_p = ((maxgen_ratio > 0.5) && (maxgen_frag_ratio > 0.1));

    // We need to adjust elevation here because if there's enough fragmentation it's not
    // unproductive.
    if (maxgen_highfrag_p)
    {
        settings.should_lock_elevation = FALSE;
        dprintf (GTC_LOG, ("high frag gen2, turn off elevation"));
    }

    return maxgen_highfrag_p;
}

void gc_heap::update_recorded_gen_data (last_recorded_gc_info* gc_info)
{
    memset (gc_info->gen_info, 0, sizeof (gc_info->gen_info));

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        gc_history_per_heap* current_gc_data_per_heap = hp->get_gc_data_per_heap();
        for (int gen_number = 0; gen_number < total_generation_count; gen_number++)
        {
            recorded_generation_info* recorded_info = &(gc_info->gen_info[gen_number]);
            gc_generation_data* data = &(current_gc_data_per_heap->gen_data[gen_number]);
            recorded_info->size_before += data->size_before;
            recorded_info->fragmentation_before += data->free_list_space_before + data->free_obj_space_before;
            recorded_info->size_after += data->size_after;
            recorded_info->fragmentation_after += data->free_list_space_after + data->free_obj_space_after;
        }
    }
}

size_t gc_heap::compute_committed_bytes_per_heap(int oh, size_t& committed_bookkeeping)
{
#ifdef USE_REGIONS
    int start_generation = (oh == 0) ? 0 : oh + max_generation;
#else
    int start_generation = oh + max_generation;
#endif
    int end_generation = oh + max_generation;

    size_t total_committed_per_heap = 0;
    for (int gen = start_generation; gen <= end_generation; gen++)
    {
        accumulate_committed_bytes (generation_start_segment (generation_of (gen)), total_committed_per_heap, committed_bookkeeping);
    }

#ifdef BACKGROUND_GC
    if (oh == soh)
    {
        accumulate_committed_bytes (freeable_soh_segment, total_committed_per_heap, committed_bookkeeping);
    }
    else
#endif //BACKGROUND_GC
    {
        accumulate_committed_bytes (freeable_uoh_segment, total_committed_per_heap, committed_bookkeeping, (gc_oh_num)oh);
    }

    return total_committed_per_heap;
}

void gc_heap::compute_committed_bytes(size_t& total_committed, size_t& committed_decommit, size_t& committed_free,
                                      size_t& committed_bookkeeping, size_t& new_current_total_committed, size_t& new_current_total_committed_bookkeeping,
                                      size_t* new_committed_by_oh)
{
    // Accounting for the bytes committed for the regions
    for (int oh = soh; oh < total_oh_count; oh++)
    {
        size_t total_committed_per_oh = 0;
#ifdef MULTIPLE_HEAPS
        for (int h = 0; h < n_heaps; h++)
        {
            gc_heap* heap = g_heaps[h];
#else
        {
            gc_heap* heap = pGenGCHeap;
#endif //MULTIPLE_HEAPS
            size_t total_committed_per_heap = heap->compute_committed_bytes_per_heap (oh, committed_bookkeeping);
#if defined(MULTIPLE_HEAPS) && defined(_DEBUG)
            heap->committed_by_oh_per_heap_refresh[oh] = total_committed_per_heap;
#endif // MULTIPLE_HEAPS && _DEBUG
            total_committed_per_oh += total_committed_per_heap;
        }
        new_committed_by_oh[oh] = total_committed_per_oh;
        total_committed += total_committed_per_oh;
    }

#ifdef USE_REGIONS
    // Accounting for the bytes committed for the free lists
    size_t committed_old_free = 0;
    committed_free = 0;
#ifdef MULTIPLE_HEAPS
    for (int h = 0; h < n_heaps; h++)
    {
        gc_heap* heap = g_heaps[h];
#else
    {
        gc_heap* heap = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        for (int i = 0; i < count_free_region_kinds; i++)
        {
            heap_segment* seg = heap->free_regions[i].get_first_free_region();
            heap->accumulate_committed_bytes (seg, committed_free, committed_bookkeeping);
        }
    }
    committed_old_free += committed_free;
    committed_decommit = 0;
    for (int i = 0; i < count_free_region_kinds; i++)
    {
        heap_segment* seg = global_regions_to_decommit[i].get_first_free_region();
#ifdef MULTIPLE_HEAPS
        gc_heap* heap = g_heaps[0];
#else
        gc_heap* heap = nullptr;
#endif //MULTIPLE_HEAPS
        heap->accumulate_committed_bytes (seg, committed_decommit, committed_bookkeeping);
    }
    committed_old_free += committed_decommit;
    {
        heap_segment* seg = global_free_huge_regions.get_first_free_region();
#ifdef MULTIPLE_HEAPS
        gc_heap* heap = g_heaps[0];
#else
        gc_heap* heap = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        heap->accumulate_committed_bytes (seg, committed_old_free, committed_bookkeeping);
    }

    new_committed_by_oh[recorded_committed_free_bucket] = committed_old_free;
    total_committed += committed_old_free;

    // Accounting for the bytes committed for the book keeping elements
    uint8_t* commit_begins[total_bookkeeping_elements];
    size_t commit_sizes[total_bookkeeping_elements];
    size_t new_sizes[total_bookkeeping_elements];
    bool get_card_table_commit_layout_result = get_card_table_commit_layout(g_gc_lowest_address, bookkeeping_covered_committed, commit_begins, commit_sizes, new_sizes);
    assert (get_card_table_commit_layout_result);

    for (int i = card_table_element; i <= seg_mapping_table_element; i++)
    {
        // In case background GC is disabled - the software write watch table is still there
        // but with size 0
        assert (commit_sizes[i] >= 0);
        committed_bookkeeping += commit_sizes[i];
    }

    new_current_total_committed_bookkeeping = committed_bookkeeping;
    new_committed_by_oh[recorded_committed_bookkeeping_bucket] = committed_bookkeeping;
#else
    new_committed_by_oh[recorded_committed_ignored_bucket] = committed_free = 0;

    uint32_t* ct = &g_gc_card_table[card_word (gcard_of (g_gc_lowest_address))];
    while (ct)
    {
        uint8_t* lowest = card_table_lowest_address (ct);
        uint8_t* highest = card_table_highest_address (ct);
        get_card_table_element_layout(lowest, highest, card_table_element_layout);
        size_t result = card_table_element_layout[seg_mapping_table_element + 1];
        committed_bookkeeping += result;
        ct = card_table_next (ct);
    }
    // If we don't put the mark array committed in the ignored bucket, calculate the committed memory for mark array here
    new_committed_by_oh[recorded_committed_bookkeeping_bucket] = new_current_total_committed_bookkeeping = committed_bookkeeping;
#endif //USE_REGIONS
    total_committed += committed_bookkeeping;
    new_current_total_committed = total_committed;
}

void gc_heap::accumulate_committed_bytes(heap_segment* seg, size_t& committed_bytes, size_t& mark_array_committed_bytes, gc_oh_num oh)
{
    seg = heap_segment_rw (seg);
    while (seg)
    {
        if ((oh == unknown) || (heap_segment_oh (seg) == oh))
        {
            uint8_t* start;
#ifdef USE_REGIONS
            mark_array_committed_bytes += get_mark_array_size (seg);
            start = get_region_start (seg);
#else
            start = (uint8_t*)seg;
#endif
            committed_bytes += (heap_segment_committed (seg) - start);
        }
        seg = heap_segment_next_rw (seg);
    }
}
