// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

bool gc_heap::virtual_alloc_commit_for_heap (void* addr, size_t size, int h_number)
{
#ifdef MULTIPLE_HEAPS
    if (GCToOSInterface::CanEnableGCNumaAware())
    {
        uint16_t numa_node = heap_select::find_numa_node_from_heap_no(h_number);
        if (GCToOSInterface::VirtualCommit (addr, size, numa_node))
            return true;
    }
#else //MULTIPLE_HEAPS
    UNREFERENCED_PARAMETER(h_number);
#endif //MULTIPLE_HEAPS

    //numa aware not enabled, or call failed --> fallback to VirtualCommit()
    return GCToOSInterface::VirtualCommit(addr, size);
}

bool gc_heap::virtual_commit (void* address, size_t size, int bucket, int h_number, bool* hard_limit_exceeded_p)
{
    /**
     * Here are all the possible cases for the commits:
     *
     * Case 1: This is for a particular generation - the bucket will be one of the gc_oh_num != unknown, and the h_number will be the right heap
     * Case 2: This is for bookkeeping - the bucket will be recorded_committed_bookkeeping_bucket, and the h_number will be -1
     *
     * Note  : We never commit into free directly, so bucket != recorded_committed_free_bucket
     */

    assert(0 <= bucket && bucket < recorded_committed_bucket_counts);
    assert(bucket < total_oh_count || h_number == -1);
#ifdef USE_REGIONS
    assert(bucket != recorded_committed_free_bucket);
#endif //USE_REGIONS

    dprintf(3, ("commit-accounting:  commit in %d [%p, %p) for heap %d", bucket, address, ((uint8_t*)address + size), h_number));
    bool should_count =
#ifdef USE_REGIONS
        true;
#else
        (bucket != recorded_committed_ignored_bucket);
#endif //USE_REGIONS

    if (should_count)
    {
        check_commit_cs.Enter();
        bool exceeded_p = false;

        if (heap_hard_limit_oh[soh] != 0)
        {
            if ((bucket < total_oh_count) && (committed_by_oh[bucket] + size) > heap_hard_limit_oh[bucket])
            {
                exceeded_p = true;
            }
        }
        else
        {
            size_t base = current_total_committed;
            size_t limit = heap_hard_limit;

            if ((base + size) > limit)
            {
                dprintf (2, ("%zd + %zd = %zd > limit %zd ", base, size, (base + size), limit));
                exceeded_p = true;
            }
        }

        if (!heap_hard_limit) {
            exceeded_p = false;
        }

        if (!exceeded_p)
        {
#if defined(MULTIPLE_HEAPS) && defined(_DEBUG)
            if ((h_number != -1) && (bucket < total_oh_count))
            {
                g_heaps[h_number]->committed_by_oh_per_heap[bucket] += size;
            }
#endif // MULTIPLE_HEAPS && _DEBUG
            committed_by_oh[bucket] += size;
            current_total_committed += size;
            if (h_number < 0)
                current_total_committed_bookkeeping += size;
        }

        check_commit_cs.Leave();

        if (hard_limit_exceeded_p)
            *hard_limit_exceeded_p = exceeded_p;

        if (exceeded_p)
        {
            dprintf (1, ("can't commit %zx for %zd bytes > HARD LIMIT %zd", (size_t)address, size, heap_hard_limit));
            return false;
        }
    }

    // If it's a valid heap number it means it's commiting for memory on the GC heap.
    // In addition if large pages is enabled, we set commit_succeeded_p to true because memory is already committed.
    bool commit_succeeded_p = ((h_number >= 0) ? (use_large_pages_p ? true :
                              virtual_alloc_commit_for_heap (address, size, h_number)) :
                              GCToOSInterface::VirtualCommit(address, size));

    if (!commit_succeeded_p && should_count)
    {
        check_commit_cs.Enter();
        committed_by_oh[bucket] -= size;
#if defined(MULTIPLE_HEAPS) && defined(_DEBUG)
        if ((h_number != -1) && (bucket < total_oh_count))
        {
            assert (g_heaps[h_number]->committed_by_oh_per_heap[bucket] >= size);
            g_heaps[h_number]->committed_by_oh_per_heap[bucket] -= size;
        }
#endif // MULTIPLE_HEAPS && _DEBUG
        dprintf (1, ("commit failed, updating %zd to %zd",
                current_total_committed, (current_total_committed - size)));
        current_total_committed -= size;
        if (h_number < 0)
        {
            assert (current_total_committed_bookkeeping >= size);
            current_total_committed_bookkeeping -= size;
        }

        check_commit_cs.Leave();
    }
    return commit_succeeded_p;
}

void gc_heap::reduce_committed_bytes (void* address, size_t size, int bucket, int h_number, bool decommit_succeeded_p)
{
    assert(0 <= bucket && bucket < recorded_committed_bucket_counts);
    assert(bucket < total_oh_count || h_number == -1);

    dprintf(3, ("commit-accounting:  decommit in %d [%p, %p) for heap %d", bucket, address, ((uint8_t*)address + size), h_number));

#ifndef USE_REGIONS
    if (bucket != recorded_committed_ignored_bucket)
#endif
    if (decommit_succeeded_p)
    {
        check_commit_cs.Enter();
        assert (committed_by_oh[bucket] >= size);
        committed_by_oh[bucket] -= size;
#if defined(MULTIPLE_HEAPS) && defined(_DEBUG)
        if ((h_number != -1) && (bucket < total_oh_count))
        {
            assert (g_heaps[h_number]->committed_by_oh_per_heap[bucket] >= size);
            g_heaps[h_number]->committed_by_oh_per_heap[bucket] -= size;
        }
#endif // MULTIPLE_HEAPS && _DEBUG
        assert (current_total_committed >= size);
        current_total_committed -= size;
        if (bucket == recorded_committed_bookkeeping_bucket)
        {
            assert (current_total_committed_bookkeeping >= size);
            current_total_committed_bookkeeping -= size;
        }
        check_commit_cs.Leave();
    }
}

bool gc_heap::virtual_decommit (void* address, size_t size, int bucket, int h_number)
{
    /**
     * Here are all possible cases for the decommits:
     *
     * Case 1: This is for a particular generation - the bucket will be one of the gc_oh_num != unknown, and the h_number will be the right heap
     * Case 2: This is for bookkeeping - the bucket will be recorded_committed_bookkeeping_bucket, and the h_number will be -1
     * Case 3: This is for free - the bucket will be recorded_committed_free_bucket, and the h_number will be -1
     */

    bool decommit_succeeded_p = ((bucket != recorded_committed_bookkeeping_bucket) && use_large_pages_p) ? true : GCToOSInterface::VirtualDecommit (address, size);

    reduce_committed_bytes (address, size, bucket, h_number, decommit_succeeded_p);

    return decommit_succeeded_p;
}

void gc_heap::virtual_free (void* add, size_t allocated_size, heap_segment* sg)
{
    bool release_succeeded_p = GCToOSInterface::VirtualRelease (add, allocated_size);
    if (release_succeeded_p)
    {
        reserved_memory -= allocated_size;
        dprintf (2, ("Virtual Free size %zd: [%zx, %zx[",
                    allocated_size, (size_t)add, (size_t)((uint8_t*)add + allocated_size)));
    }
}



#if !defined(USE_REGIONS) || defined(MULTIPLE_HEAPS)

// For regions this really just sets the decommit target for ephemeral tail regions so this should really be done in
// distribute_free_regions where we are calling estimate_gen_growth.
void gc_heap::decommit_ephemeral_segment_pages()
{
    if (settings.concurrent || use_large_pages_p || (settings.pause_mode == pause_no_gc))
    {
        return;
    }

#if defined(MULTIPLE_HEAPS) && defined(USE_REGIONS)
    for (int gen_number = soh_gen0; gen_number <= soh_gen1; gen_number++)
    {
        generation *gen = generation_of (gen_number);
        heap_segment* tail_region = generation_tail_region (gen);
        uint8_t* previous_decommit_target = heap_segment_decommit_target (tail_region);

        // reset the decommit targets to make sure we don't decommit inadvertently
        for (heap_segment* region = generation_start_segment_rw (gen); region != nullptr; region = heap_segment_next (region))
        {
            heap_segment_decommit_target (region) = heap_segment_reserved (region);
        }

        ptrdiff_t budget_gen = estimate_gen_growth (gen_number) + loh_size_threshold;

        if (budget_gen >= 0)
        {
            // we need more than the regions we have - nothing to decommit
            continue;
        }

        // we may have too much committed - let's see if we can decommit in the tail region
        ptrdiff_t tail_region_size = heap_segment_reserved (tail_region) - heap_segment_mem (tail_region);
        ptrdiff_t unneeded_tail_size = min (-budget_gen, tail_region_size);
        uint8_t *decommit_target = heap_segment_reserved (tail_region) - unneeded_tail_size;
        decommit_target = max (decommit_target, heap_segment_allocated (tail_region));

        heap_segment_decommit_target (tail_region) = get_smoothed_decommit_target (previous_decommit_target, decommit_target, tail_region);
    }
#elif !defined(USE_REGIONS)
    dynamic_data* dd0 = dynamic_data_of (0);

    ptrdiff_t desired_allocation = dd_new_allocation (dd0) +
                                   max (estimate_gen_growth (soh_gen1), (ptrdiff_t)0) +
                                   loh_size_threshold;

    size_t slack_space =
#ifdef HOST_64BIT
                max(min(min(soh_segment_size/32, dd_max_size (dd0)), (generation_size (max_generation) / 10)), (size_t)desired_allocation);
#else
                desired_allocation;
#endif // HOST_64BIT

    uint8_t* decommit_target = heap_segment_allocated (ephemeral_heap_segment) + slack_space;
    uint8_t* previous_decommit_target = heap_segment_decommit_target (ephemeral_heap_segment);
    heap_segment_decommit_target (ephemeral_heap_segment) = get_smoothed_decommit_target (previous_decommit_target, decommit_target, ephemeral_heap_segment);

#if defined(MULTIPLE_HEAPS) && defined(_DEBUG)
    // these are only for checking against logic errors
    ephemeral_heap_segment->saved_committed = heap_segment_committed (ephemeral_heap_segment);
    ephemeral_heap_segment->saved_desired_allocation = dd_desired_allocation (dd0);
#endif //MULTIPLE_HEAPS && _DEBUG

#ifndef MULTIPLE_HEAPS
    // we want to limit the amount of decommit we do per time to indirectly
    // limit the amount of time spent in recommit and page faults
    size_t ephemeral_elapsed = (size_t)((dd_time_clock (dd0) - gc_last_ephemeral_decommit_time) / 1000);
    gc_last_ephemeral_decommit_time = dd_time_clock (dd0);

    // this is the amount we were planning to decommit
    ptrdiff_t decommit_size = heap_segment_committed (ephemeral_heap_segment) - decommit_target;

    // we do a max of DECOMMIT_SIZE_PER_MILLISECOND per millisecond of elapsed time since the last GC
    // we limit the elapsed time to 10 seconds to avoid spending too much time decommitting
    ptrdiff_t max_decommit_size = min (ephemeral_elapsed, (size_t)(10*1000)) * DECOMMIT_SIZE_PER_MILLISECOND;
    decommit_size = min (decommit_size, max_decommit_size);

    slack_space = heap_segment_committed (ephemeral_heap_segment) - heap_segment_allocated (ephemeral_heap_segment) - decommit_size;
    decommit_heap_segment_pages (ephemeral_heap_segment, slack_space);
#endif // !MULTIPLE_HEAPS

    gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();
    current_gc_data_per_heap->extra_gen0_committed = heap_segment_committed (ephemeral_heap_segment) - heap_segment_allocated (ephemeral_heap_segment);
#endif //MULTIPLE_HEAPS && USE_REGIONS
}

#endif //!defined(USE_REGIONS) || defined(MULTIPLE_HEAPS)

#if defined(MULTIPLE_HEAPS) || defined(USE_REGIONS)

// return true if we actually decommitted anything
bool gc_heap::decommit_step (uint64_t step_milliseconds)
{
    if (settings.pause_mode == pause_no_gc)
    {
        // don't decommit at all if we have entered a no gc region
        return false;
    }

    size_t decommit_size = 0;

#ifdef USE_REGIONS
    const size_t max_decommit_step_size = DECOMMIT_SIZE_PER_MILLISECOND * step_milliseconds;
    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        dprintf (REGIONS_LOG, ("decommit_step %d, regions_to_decommit = %zd",
            kind, global_regions_to_decommit[kind].get_num_free_regions()));
        while (global_regions_to_decommit[kind].get_num_free_regions() > 0)
        {
            heap_segment* region = global_regions_to_decommit[kind].unlink_region_front();
            size_t size = decommit_region (region, recorded_committed_free_bucket, -1);
            decommit_size += size;
            if (decommit_size >= max_decommit_step_size)
            {
                return true;
            }
        }
    }
    if (use_large_pages_p)
    {
        return (decommit_size != 0);
    }
#endif //USE_REGIONS
#ifdef MULTIPLE_HEAPS
    // should never get here for large pages because decommit_ephemeral_segment_pages
    // will not do anything if use_large_pages_p is true
    assert(!use_large_pages_p);

    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        decommit_size += hp->decommit_ephemeral_segment_pages_step ();
    }
#endif //MULTIPLE_HEAPS
    return (decommit_size != 0);
}

#endif //defined(MULTIPLE_HEAPS) || defined(USE_REGIONS)

#ifdef USE_REGIONS

size_t gc_heap::decommit_region (heap_segment* region, int bucket, int h_number)
{
    FIRE_EVENT(GCFreeSegment_V1, heap_segment_mem (region));
    uint8_t* page_start = align_lower_page (get_region_start (region));
    uint8_t* decommit_end = heap_segment_committed (region);
    size_t decommit_size = decommit_end - page_start;
    bool decommit_succeeded_p = virtual_decommit (page_start, decommit_size, bucket, h_number);
    bool require_clearing_memory_p = !decommit_succeeded_p || use_large_pages_p;
    dprintf (REGIONS_LOG, ("decommitted region %p(%p-%p) (%zu bytes) - success: %d",
        region,
        page_start,
        decommit_end,
        decommit_size,
        decommit_succeeded_p));
    if (require_clearing_memory_p)
    {
        uint8_t* clear_end = use_large_pages_p ? heap_segment_used (region) : heap_segment_committed (region);
        size_t clear_size = clear_end - page_start;
        memclr (page_start, clear_size);
        heap_segment_used (region) = heap_segment_mem (region);
        dprintf(REGIONS_LOG, ("cleared region %p(%p-%p) (%zu bytes)",
            region,
            page_start,
            clear_end,
            clear_size));
    }
    else
    {
        heap_segment_committed (region) = heap_segment_mem (region);
    }

#ifdef BACKGROUND_GC
    // Under USE_REGIONS, mark array is never partially committed. So we are only checking for this
    // flag here.
    if ((region->flags & heap_segment_flags_ma_committed) != 0)
    {
#ifdef MULTIPLE_HEAPS
        // In return_free_region, we set heap_segment_heap (region) to nullptr so we cannot use it here.
        // but since all heaps share the same mark array we simply pick the 0th heap to use. 
        gc_heap* hp = g_heaps [0];
#else
        gc_heap* hp = pGenGCHeap;
#endif
        hp->decommit_mark_array_by_seg (region);
        region->flags &= ~(heap_segment_flags_ma_committed);
    }
#endif //BACKGROUND_GC

    if (use_large_pages_p)
    {
        assert (heap_segment_used (region) == heap_segment_mem (region));
    }
    else
    {
        assert (heap_segment_committed (region) == heap_segment_mem (region));
    }
#ifdef BACKGROUND_GC
    assert ((region->flags & heap_segment_flags_ma_committed) == 0);
#endif //BACKGROUND_GC

    global_region_allocator.delete_region (get_region_start (region));

    return decommit_size;
}

#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS

// return the decommitted size
size_t gc_heap::decommit_ephemeral_segment_pages_step ()
{
    size_t size = 0;
#ifdef USE_REGIONS
    for (int gen_number = soh_gen0; gen_number <= soh_gen1; gen_number++)
    {
        generation* gen = generation_of (gen_number);
        heap_segment* seg = generation_tail_region (gen);
#else // USE_REGIONS
    {
        heap_segment* seg = ephemeral_heap_segment;
        // we rely on desired allocation not being changed outside of GC
        assert (seg->saved_desired_allocation == dd_desired_allocation (dynamic_data_of (0)));
#endif // USE_REGIONS

        uint8_t* decommit_target = heap_segment_decommit_target (seg);
        size_t EXTRA_SPACE = 2 * OS_PAGE_SIZE;
        decommit_target += EXTRA_SPACE;
        uint8_t* committed = heap_segment_committed (seg);
        uint8_t* allocated = (seg == ephemeral_heap_segment) ? alloc_allocated : heap_segment_allocated (seg);
        if ((allocated <= decommit_target) && (decommit_target < committed))
        {
#ifdef USE_REGIONS
            if (gen_number == soh_gen0)
            {
                // for gen 0, sync with the allocator by taking the more space lock
                // and re-read the variables
                //
                // we call try_enter_spin_lock here instead of enter_spin_lock because
                // calling enter_spin_lock from this thread can deadlock at the start
                // of a GC - if gc_started is already true, we call wait_for_gc_done(),
                // but we are on GC thread 0, so GC cannot make progress
                if (!try_enter_spin_lock (&more_space_lock_soh))
                {
                    continue;
                }
                add_saved_spinlock_info (false, me_acquire, mt_decommit_step, msl_entered);
                seg = generation_tail_region (gen);
#ifndef STRESS_DECOMMIT
                decommit_target = heap_segment_decommit_target (seg);
                decommit_target += EXTRA_SPACE;
#endif
                committed = heap_segment_committed (seg);
                allocated = (seg == ephemeral_heap_segment) ? alloc_allocated : heap_segment_allocated (seg);
            }
            if ((allocated <= decommit_target) && (decommit_target < committed))
#else // USE_REGIONS
            // we rely on other threads not messing with committed if we are about to trim it down
            assert (seg->saved_committed == heap_segment_committed (seg));
#endif // USE_REGIONS
            {
                // how much would we need to decommit to get to decommit_target in one step?
                size_t full_decommit_size = (committed - decommit_target);

                // don't do more than max_decommit_step_size per step
                size_t decommit_size = min (max_decommit_step_size, full_decommit_size);

                // figure out where the new committed should be
                uint8_t* new_committed = (committed - decommit_size);
                size += decommit_heap_segment_pages_worker (seg, new_committed);

#if defined(_DEBUG) && !defined(USE_REGIONS)
                seg->saved_committed = committed - size;
#endif //_DEBUG && !USE_REGIONS
            }
#ifdef USE_REGIONS
            if (gen_number == soh_gen0)
            {
                // for gen 0, we took the more space lock - leave it again
                add_saved_spinlock_info (false, me_release, mt_decommit_step, msl_entered);
                leave_spin_lock (&more_space_lock_soh);
            }
#endif // USE_REGIONS
        }
    }
    return size;
}

#endif //MULTIPLE_HEAPS
