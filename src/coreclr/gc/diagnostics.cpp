// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

void gc_heap::add_to_history_per_heap()
{
#if defined(GC_HISTORY) && defined(BACKGROUND_GC)
    gc_history* current_hist = &gchist_per_heap[gchist_index_per_heap];
    current_hist->gc_index = settings.gc_index;
    current_hist->current_bgc_state = current_bgc_state;
    size_t elapsed = dd_gc_elapsed_time (dynamic_data_of (0));
    current_hist->gc_time_ms = (uint32_t)(elapsed / 1000);
    current_hist->gc_efficiency = (elapsed ? (total_promoted_bytes / elapsed) : total_promoted_bytes);
#ifndef USE_REGIONS
    current_hist->eph_low = generation_allocation_start (generation_of (max_generation - 1));
    current_hist->gen0_start = generation_allocation_start (generation_of (0));
    current_hist->eph_high = heap_segment_allocated (ephemeral_heap_segment);
#endif //!USE_REGIONS
#ifdef BACKGROUND_GC
    current_hist->bgc_lowest = background_saved_lowest_address;
    current_hist->bgc_highest = background_saved_highest_address;
#endif //BACKGROUND_GC
    current_hist->fgc_lowest = lowest_address;
    current_hist->fgc_highest = highest_address;
    current_hist->g_lowest = g_gc_lowest_address;
    current_hist->g_highest = g_gc_highest_address;

    gchist_index_per_heap++;
    if (gchist_index_per_heap == max_history_count)
    {
        gchist_index_per_heap = 0;
    }
#endif //GC_HISTORY && BACKGROUND_GC
}

void gc_heap::add_to_history()
{
#if defined(GC_HISTORY) && defined(BACKGROUND_GC)
    gc_mechanisms_store* current_settings = &gchist[gchist_index];
    current_settings->store (&settings);

    gchist_index++;
    if (gchist_index == max_history_count)
    {
        gchist_index = 0;
    }
#endif //GC_HISTORY && BACKGROUND_GC
}

void gc_heap::fire_per_heap_hist_event (gc_history_per_heap* current_gc_data_per_heap, int heap_num)
{
    maxgen_size_increase* maxgen_size_info = &(current_gc_data_per_heap->maxgen_size_info);
    FIRE_EVENT(GCPerHeapHistory_V3,
               (void *)(maxgen_size_info->free_list_allocated),
               (void *)(maxgen_size_info->free_list_rejected),
               (void *)(maxgen_size_info->end_seg_allocated),
               (void *)(maxgen_size_info->condemned_allocated),
               (void *)(maxgen_size_info->pinned_allocated),
               (void *)(maxgen_size_info->pinned_allocated_advance),
               maxgen_size_info->running_free_list_efficiency,
               current_gc_data_per_heap->gen_to_condemn_reasons.get_reasons0(),
               current_gc_data_per_heap->gen_to_condemn_reasons.get_reasons1(),
               current_gc_data_per_heap->mechanisms[gc_heap_compact],
               current_gc_data_per_heap->mechanisms[gc_heap_expand],
               current_gc_data_per_heap->heap_index,
               (void *)(current_gc_data_per_heap->extra_gen0_committed),
               total_generation_count,
               (uint32_t)(sizeof (gc_generation_data)),
               (void *)&(current_gc_data_per_heap->gen_data[0]));

    current_gc_data_per_heap->print();
    current_gc_data_per_heap->gen_to_condemn_reasons.print (heap_num);
}

void gc_heap::fire_pevents()
{
    gc_history_global* current_gc_data_global = get_gc_data_global();

    settings.record (current_gc_data_global);
    current_gc_data_global->print();

#ifdef FEATURE_EVENT_TRACE
    if (!informational_event_enabled_p) return;

    uint32_t count_time_info = (settings.concurrent ? max_bgc_time_type :
                                (settings.compaction ? max_compact_time_type : max_sweep_time_type));

#ifdef BACKGROUND_GC
    uint64_t* time_info = (settings.concurrent ? bgc_time_info : gc_time_info);
#else
    uint64_t* time_info = gc_time_info;
#endif //BACKGROUND_GC
    // We don't want to have to fire the time info as 64-bit integers as there's no need to
    // so compress them down to 32-bit ones.
    uint32_t* time_info_32 = (uint32_t*)time_info;
    for (uint32_t i = 0; i < count_time_info; i++)
    {
        time_info_32[i] = limit_time_to_uint32 (time_info[i]);
    }

    FIRE_EVENT(GCGlobalHeapHistory_V4,
               current_gc_data_global->final_youngest_desired,
               current_gc_data_global->num_heaps,
               current_gc_data_global->condemned_generation,
               current_gc_data_global->gen0_reduction_count,
               current_gc_data_global->reason,
               current_gc_data_global->global_mechanisms_p,
               current_gc_data_global->pause_mode,
               current_gc_data_global->mem_pressure,
               current_gc_data_global->gen_to_condemn_reasons.get_reasons0(),
               current_gc_data_global->gen_to_condemn_reasons.get_reasons1(),
               count_time_info,
               (uint32_t)(sizeof (uint32_t)),
               (void*)time_info_32);

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        gc_history_per_heap* current_gc_data_per_heap = hp->get_gc_data_per_heap();
        fire_per_heap_hist_event (current_gc_data_per_heap, hp->heap_number);
    }
#else
    gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();
    fire_per_heap_hist_event (current_gc_data_per_heap, heap_number);
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_LOH_COMPACTION
    if (!settings.concurrent && settings.loh_compaction)
    {
        // Not every heap will compact LOH, the ones that didn't will just have 0s
        // in its info.
        FIRE_EVENT(GCLOHCompact,
                   (uint16_t)get_num_heaps(),
                   (uint32_t)(sizeof (etw_loh_compact_info)),
                   (void *)loh_compact_info);
    }
#endif //FEATURE_LOH_COMPACTION
#endif //FEATURE_EVENT_TRACE
}

// This fires the amount of total committed in use, in free and on the decommit list.
// It's fired on entry and exit of each blocking GC and on entry of each BGC (not firing this on exit of a GC
// because EE is not suspended then. On entry it's fired after the GCStart event, on exit it's fire before the GCStop event.
void gc_heap::fire_committed_usage_event()
{
#ifdef FEATURE_EVENT_TRACE
    if (!EVENT_ENABLED (GCMarkWithType)) return;

    size_t total_committed = 0;
    size_t committed_decommit = 0;
    size_t committed_free = 0;
    size_t committed_bookkeeping = 0;
    size_t new_current_total_committed;
    size_t new_current_total_committed_bookkeeping;
    size_t new_committed_by_oh[recorded_committed_bucket_counts];
    compute_committed_bytes(total_committed, committed_decommit, committed_free,
                            committed_bookkeeping, new_current_total_committed, new_current_total_committed_bookkeeping,
                            new_committed_by_oh);

    size_t total_committed_in_use = new_committed_by_oh[soh] + new_committed_by_oh[loh] + new_committed_by_oh[poh];
#ifdef USE_REGIONS
    size_t total_committed_in_global_decommit = committed_decommit;
    size_t total_committed_in_free = committed_free;
    size_t total_committed_in_global_free = new_committed_by_oh[recorded_committed_free_bucket] - total_committed_in_free - total_committed_in_global_decommit;
#else
    assert (committed_decommit == 0);
    assert (committed_free == 0);
    size_t total_committed_in_global_decommit = 0;
    size_t total_committed_in_free = 0;
    size_t total_committed_in_global_free = 0;
    // For segments, bookkeeping committed does not include mark array
#endif //USE_REGIONS
    size_t total_bookkeeping_committed = committed_bookkeeping;

    GCEventFireCommittedUsage_V1 (
        (uint64_t)total_committed_in_use,
        (uint64_t)total_committed_in_global_decommit,
        (uint64_t)total_committed_in_free,
        (uint64_t)total_committed_in_global_free,
        (uint64_t)total_bookkeeping_committed
    );
#endif //FEATURE_EVENT_TRACE
}

#ifdef BACKGROUND_GC
inline
void fire_alloc_wait_event (alloc_wait_reason awr, BOOL begin_p)
{
    if (awr != awr_ignored)
    {
        if (begin_p)
        {
            FIRE_EVENT(BGCAllocWaitBegin, awr);
        }
        else
        {
            FIRE_EVENT(BGCAllocWaitEnd, awr);
        }
    }
}

void gc_heap::fire_alloc_wait_event_begin (alloc_wait_reason awr)
{
    fire_alloc_wait_event (awr, TRUE);
}

void gc_heap::fire_alloc_wait_event_end (alloc_wait_reason awr)
{
    fire_alloc_wait_event (awr, FALSE);
}

#endif //BACKGROUND_GC

void gc_heap::add_saved_spinlock_info (
            bool loh_p,
            msl_enter_state enter_state,
            msl_take_state take_state,
            enter_msl_status msl_status)
{
#ifdef SPINLOCK_HISTORY
    if (!loh_p || (msl_status == msl_retry_different_heap))
    {
        return;
    }

    spinlock_info* current = &last_spinlock_info[spinlock_info_index];

    current->enter_state = enter_state;
    current->take_state = take_state;
    current->current_uoh_alloc_state = current_uoh_alloc_state;
    current->thread_id.SetToCurrentThread();
    current->loh_p = loh_p;
    dprintf (SPINLOCK_LOG, ("[%d]%s %s %s",
        heap_number,
        (loh_p ? "loh" : "soh"),
        ((enter_state == me_acquire) ? "E" : "L"),
        msl_take_state_str[take_state]));

    spinlock_info_index++;

    assert (spinlock_info_index <= max_saved_spinlock_info);

    if (spinlock_info_index >= max_saved_spinlock_info)
    {
        spinlock_info_index = 0;
    }
#else
    UNREFERENCED_PARAMETER(enter_state);
    UNREFERENCED_PARAMETER(take_state);
#endif //SPINLOCK_HISTORY
}

#ifdef USE_REGIONS
void gc_heap::verify_region_to_generation_map()
{
#ifdef _DEBUG
    uint8_t* local_ephemeral_low = MAX_PTR;
    uint8_t* local_ephemeral_high = nullptr;
    for (int gen_number = soh_gen0; gen_number < total_generation_count; gen_number++)
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
                if (heap_segment_read_only_p (region))
                {
                    // the region to generation map doesn't cover read only segments
                    continue;
                }
                size_t region_index_start = get_basic_region_index_for_address (get_region_start (region));
                size_t region_index_end = get_basic_region_index_for_address (heap_segment_reserved (region));
                int gen_num = min (gen_number, (int)soh_gen2);
                assert (gen_num == heap_segment_gen_num (region));
                int plan_gen_num = heap_segment_plan_gen_num (region);
                bool is_demoted = (region->flags & heap_segment_flags_demoted) != 0;
                bool is_sweep_in_plan = heap_segment_swept_in_plan (region);
                for (size_t region_index = region_index_start; region_index < region_index_end; region_index++)
                {
                    region_info region_info_bits = map_region_to_generation[region_index];
                    assert ((region_info_bits & RI_GEN_MASK) == gen_num);
                    assert ((region_info_bits >> RI_PLAN_GEN_SHR) == plan_gen_num);
                    assert (((region_info_bits & RI_SIP) != 0) == is_sweep_in_plan);
                    assert (((region_info_bits & RI_DEMOTED) != 0) == is_demoted);
                }
            }
        }
    }
#endif //_DEBUG
}

#endif //USE_REGIONS
#ifdef BACKGROUND_GC
void gc_heap::verify_mark_array_cleared (uint8_t* begin, uint8_t* end, uint32_t* mark_array_addr)
{
#ifdef _DEBUG
    size_t  markw = mark_word_of (begin);
    size_t  markw_end = mark_word_of (end);

    while (markw < markw_end)
    {
        if (mark_array_addr[markw])
        {
            uint8_t* addr = mark_word_address (markw);
#ifdef USE_REGIONS
            heap_segment* region = region_of (addr);
            dprintf (1, ("The mark bits at 0x%zx:0x%x(addr: 0x%p, r: %zx(%p)) were not cleared",
                            markw, mark_array_addr[markw], addr,
                            (size_t)region, heap_segment_mem (region)));
#else
            dprintf (1, ("The mark bits at 0x%zx:0x%x(addr: 0x%p) were not cleared",
                            markw, mark_array_addr[markw], addr));
#endif //USE_REGIONS
            FATAL_GC_ERROR();
        }
        markw++;
    }
#else // _DEBUG
    UNREFERENCED_PARAMETER(begin);
    UNREFERENCED_PARAMETER(end);
    UNREFERENCED_PARAMETER(mark_array_addr);
#endif //_DEBUG
}

void gc_heap::set_mem_verify (uint8_t* start, uint8_t* end, uint8_t b)
{
#ifdef VERIFY_HEAP
    if (end > start)
    {
        if ((GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC) &&
           !(GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_NO_MEM_FILL))
        {
            dprintf (3, ("setting mem to %c [%p, [%p", b, start, end));
            memset (start, b, (end - start));
        }
    }
#endif //VERIFY_HEAP
}

#endif //BACKGROUND_GC

void gc_heap::descr_generations_to_profiler (gen_walk_fn fn, void *context)
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = NULL;
#endif //MULTIPLE_HEAPS

        for (int curr_gen_number = total_generation_count-1; curr_gen_number >= 0; curr_gen_number--)
        {
            generation* gen = hp->generation_of (curr_gen_number);
            heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
#ifdef USE_REGIONS
            while (seg)
            {
                fn(context, curr_gen_number, heap_segment_mem (seg),
                                              heap_segment_allocated (seg),
                                              heap_segment_reserved (seg));

                seg = heap_segment_next_rw (seg);
            }
#else
            while (seg && (seg != hp->ephemeral_heap_segment))
            {
                assert (curr_gen_number > 0);

                // report bounds from heap_segment_mem (seg) to
                // heap_segment_allocated (seg);
                // for generation # curr_gen_number
                // for heap # heap_no
                fn(context, curr_gen_number, heap_segment_mem (seg),
                                              heap_segment_allocated (seg),
                                              (curr_gen_number > max_generation) ?
                                                heap_segment_reserved (seg) : heap_segment_allocated (seg));

                seg = heap_segment_next_rw (seg);
            }

            if (seg)
            {
                assert (seg == hp->ephemeral_heap_segment);
                assert (curr_gen_number <= max_generation);

                if (curr_gen_number == max_generation)
                {
                    if (heap_segment_mem (seg) < generation_allocation_start (hp->generation_of (max_generation-1)))
                    {
                        // report bounds from heap_segment_mem (seg) to
                        // generation_allocation_start (generation_of (max_generation-1))
                        // for heap # heap_number
                        fn(context, curr_gen_number, heap_segment_mem (seg),
                                                      generation_allocation_start (hp->generation_of (max_generation-1)),
                                                      generation_allocation_start (hp->generation_of (max_generation-1)) );
                    }
                }
                else if (curr_gen_number != 0)
                {
                    //report bounds from generation_allocation_start (generation_of (curr_gen_number))
                    // to generation_allocation_start (generation_of (curr_gen_number-1))
                    // for heap # heap_number
                    fn(context, curr_gen_number, generation_allocation_start (hp->generation_of (curr_gen_number)),
                                                  generation_allocation_start (hp->generation_of (curr_gen_number-1)),
                                                  generation_allocation_start (hp->generation_of (curr_gen_number-1)));
                }
                else
                {
                    //report bounds from generation_allocation_start (generation_of (curr_gen_number))
                    // to heap_segment_allocated (ephemeral_heap_segment);
                    // for heap # heap_number
                    fn(context, curr_gen_number, generation_allocation_start (hp->generation_of (curr_gen_number)),
                                                  heap_segment_allocated (hp->ephemeral_heap_segment),
                                                  heap_segment_reserved (hp->ephemeral_heap_segment) );
                }
            }
#endif //USE_REGIONS
        }
    }
}

#ifdef TRACE_GC
// Note that when logging is on it can take a long time to go through the free items.
void gc_heap::print_free_list (int gen, heap_segment* seg)
{
    UNREFERENCED_PARAMETER(gen);
    UNREFERENCED_PARAMETER(seg);
/*
    if (settings.concurrent == FALSE)
    {
        uint8_t* seg_start = heap_segment_mem (seg);
        uint8_t* seg_end = heap_segment_allocated (seg);

        dprintf (3, ("Free list in seg %zx:", seg_start));

        size_t total_free_item = 0;

        allocator* gen_allocator = generation_allocator (generation_of (gen));
        for (unsigned int b = 0; b < gen_allocator->number_of_buckets(); b++)
        {
            uint8_t* fo = gen_allocator->alloc_list_head_of (b);
            while (fo)
            {
                if (fo >= seg_start && fo < seg_end)
                {
                    total_free_item++;

                    size_t free_item_len = size(fo);

                    dprintf (3, ("[%zx, %zx[:%zd",
                                 (size_t)fo,
                                 (size_t)(fo + free_item_len),
                                 free_item_len));
                }

                fo = free_list_slot (fo);
            }
        }

        dprintf (3, ("total %zd free items", total_free_item));
    }
*/
}

#endif //TRACE_GC

void gc_heap::descr_generations (const char* msg)
{
#ifndef TRACE_GC
    UNREFERENCED_PARAMETER(msg);
#endif //!TRACE_GC

#ifdef STRESS_LOG
    if (StressLog::StressLogOn(LF_GC, LL_INFO1000))
    {
        gc_heap* hp = 0;
#ifdef MULTIPLE_HEAPS
        hp= this;
#endif //MULTIPLE_HEAPS

        STRESS_LOG1(LF_GC, LL_INFO1000, "GC Heap %p\n", hp);
        for (int n = max_generation; n >= 0; --n)
        {
#ifndef USE_REGIONS
            STRESS_LOG4(LF_GC, LL_INFO1000, "    Generation %d [%p, %p] cur = %p\n",
                    n,
                    generation_allocation_start(generation_of(n)),
                    generation_allocation_limit(generation_of(n)),
                    generation_allocation_pointer(generation_of(n)));
#endif //USE_REGIONS

            heap_segment* seg = generation_start_segment(generation_of(n));
            while (seg)
            {
                STRESS_LOG4(LF_GC, LL_INFO1000, "        Segment mem %p alloc = %p used %p committed %p\n",
                        heap_segment_mem(seg),
                        heap_segment_allocated(seg),
                        heap_segment_used(seg),
                        heap_segment_committed(seg));
                seg = heap_segment_next(seg);
            }
        }
    }
#endif  // STRESS_LOG

#ifdef TRACE_GC
    dprintf (2, ("lowest_address: %zx highest_address: %zx",
             (size_t) lowest_address, (size_t) highest_address));
#ifdef BACKGROUND_GC
    dprintf (2, ("bgc lowest_address: %zx bgc highest_address: %zx",
             (size_t) background_saved_lowest_address, (size_t) background_saved_highest_address));
#endif //BACKGROUND_GC

    if (heap_number == 0)
    {
#ifdef USE_REGIONS
        size_t alloc_size = get_total_heap_size () / 1024 / 1024;
        size_t commit_size = get_total_committed_size () / 1024 / 1024;
        size_t frag_size = get_total_fragmentation () / 1024 / 1024;
        int total_new_gen0_regions_in_plns = get_total_new_gen0_regions_in_plns ();
        int total_new_regions_in_prr = get_total_new_regions_in_prr ();
        int total_new_regions_in_threading = get_total_new_regions_in_threading ();
        uint64_t elapsed_time_so_far = GetHighPrecisionTimeStamp () - process_start_time;

        size_t idx = VolatileLoadWithoutBarrier (&settings.gc_index);

        dprintf (REGIONS_LOG, ("[%s] GC#%5Id [%s] heap %Idmb (F: %Idmb %d%%) commit size: %Idmb, %0.3f min, %d,%d new in plan, %d in threading",
            msg, idx, (settings.promotion ? "PM" : "NPM"), alloc_size, frag_size,
            (int)((double)frag_size * 100.0 / (double)alloc_size),
            commit_size,
            (double)elapsed_time_so_far / (double)1000000 / (double)60,
            total_new_gen0_regions_in_plns, total_new_regions_in_prr, total_new_regions_in_threading));

        size_t total_gen_size_mb[loh_generation + 1] = { 0, 0, 0, 0 };
        size_t total_gen_fragmentation_mb[loh_generation + 1] = { 0, 0, 0, 0 };
        for (int i = 0; i < (loh_generation + 1); i++)
        {
            total_gen_size_mb[i] = get_total_generation_size (i) / 1024 / 1024;
            total_gen_fragmentation_mb[i] = get_total_gen_fragmentation (i) / 1024 / 1024;
        }

        int bgcs = VolatileLoadWithoutBarrier (&current_bgc_state);
#ifdef SIMPLE_DPRINTF
        dprintf (REGIONS_LOG, ("[%s] GC#%Id (bgcs: %d, %s) g0: %Idmb (f: %Idmb %d%%), g1: %Idmb (f: %Idmb %d%%), g2: %Idmb (f: %Idmb %d%%), g3: %Idmb (f: %Idmb %d%%)",
            msg, idx, bgcs, str_bgc_state[bgcs],
            total_gen_size_mb[0], total_gen_fragmentation_mb[0], (total_gen_size_mb[0] ? (int)((double)total_gen_fragmentation_mb[0] * 100.0 / (double)total_gen_size_mb[0]) : 0),
            total_gen_size_mb[1], total_gen_fragmentation_mb[1], (total_gen_size_mb[1] ? (int)((double)total_gen_fragmentation_mb[1] * 100.0 / (double)total_gen_size_mb[1]) : 0),
            total_gen_size_mb[2], total_gen_fragmentation_mb[2], (total_gen_size_mb[2] ? (int)((double)total_gen_fragmentation_mb[2] * 100.0 / (double)total_gen_size_mb[2]) : 0),
            total_gen_size_mb[3], total_gen_fragmentation_mb[3], (total_gen_size_mb[3] ? (int)((double)total_gen_fragmentation_mb[3] * 100.0 / (double)total_gen_size_mb[3]) : 0)));
#endif //SIMPLE_DPRINTF
        // print every 20 GCs so it's easy to see if we are making progress.
        if ((idx % 20) == 0)
        {
            dprintf (1, ("[%5s] GC#%5Id total heap size: %Idmb (F: %Idmb %d%%) commit size: %Idmb, %0.3f min, %d,%d new in plan, %d in threading\n",
                msg, idx, alloc_size, frag_size,
                (int)((double)frag_size * 100.0 / (double)alloc_size),
                commit_size,
                (double)elapsed_time_so_far / (double)1000000 / (double)60,
                total_new_gen0_regions_in_plns, total_new_regions_in_prr, total_new_regions_in_threading));
        }
#endif //USE_REGIONS
    }

    for (int curr_gen_number = total_generation_count - 1; curr_gen_number >= 0; curr_gen_number--)
    {
        size_t total_gen_size = generation_size (curr_gen_number);
#ifdef SIMPLE_DPRINTF
        dprintf (GTC_LOG, ("[%s][g%d]gen %d:, size: %zd, frag: %zd(L: %zd, O: %zd), f: %d%% %s %s %s",
                      msg,
                      settings.condemned_generation,
                      curr_gen_number,
                      total_gen_size,
                      dd_fragmentation (dynamic_data_of (curr_gen_number)),
                      generation_free_list_space (generation_of (curr_gen_number)),
                      generation_free_obj_space (generation_of (curr_gen_number)),
                      (total_gen_size ?
                        (int)(((double)dd_fragmentation (dynamic_data_of (curr_gen_number)) / (double)total_gen_size) * 100) :
                        0),
                      (settings.compaction ? "(compact)" : "(sweep)"),
                      (settings.heap_expansion ? "(EX)" : " "),
                      (settings.promotion ? "Promotion" : "NoPromotion")));
#else
        dprintf (2, ( "Generation %d: generation size: %zd, fragmentation: %zd",
                      curr_gen_number,
                      total_gen_size,
                      dd_fragmentation (dynamic_data_of (curr_gen_number))));
#endif //SIMPLE_DPRINTF

        generation* gen = generation_of (curr_gen_number);
        heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
#ifdef USE_REGIONS
        dprintf (GTC_LOG, ("g%d: start seg: %p alloc seg: %p, tail region: %p",
            curr_gen_number,
            heap_segment_mem (seg),
            (generation_allocation_segment (gen) ? heap_segment_mem (generation_allocation_segment (gen)) : 0),
            heap_segment_mem (generation_tail_region (gen))));
        while (seg)
        {
            dprintf (GTC_LOG, ("g%d: (%d:p %d) [%zx %zx(sa: %zx, pa: %zx)[-%zx[ (%zd) (%zd)",
                               curr_gen_number,
                               heap_segment_gen_num (seg),
                               heap_segment_plan_gen_num (seg),
                               (size_t)heap_segment_mem (seg),
                               (size_t)heap_segment_allocated (seg),
                               (size_t)heap_segment_saved_allocated (seg),
                               (size_t)heap_segment_plan_allocated (seg),
                               (size_t)heap_segment_committed (seg),
                               (size_t)(heap_segment_allocated (seg) - heap_segment_mem (seg)),
                               (size_t)(heap_segment_committed (seg) - heap_segment_allocated (seg))));
            print_free_list (curr_gen_number, seg);
            seg = heap_segment_next (seg);
        }
#else
        while (seg && (seg != ephemeral_heap_segment))
        {
            dprintf (GTC_LOG, ("g%d: [%zx %zx[-%zx[ (%zd) (%zd)",
                        curr_gen_number,
                        (size_t)heap_segment_mem (seg),
                        (size_t)heap_segment_allocated (seg),
                        (size_t)heap_segment_committed (seg),
                        (size_t)(heap_segment_allocated (seg) - heap_segment_mem (seg)),
                        (size_t)(heap_segment_committed (seg) - heap_segment_allocated (seg))));
            print_free_list (curr_gen_number, seg);
            seg = heap_segment_next (seg);
        }
        if (seg && (seg != generation_start_segment (gen)))
        {
            dprintf (GTC_LOG, ("g%d: [%zx %zx[",
                         curr_gen_number,
                         (size_t)heap_segment_mem (seg),
                         (size_t)generation_allocation_start (generation_of (curr_gen_number-1))));
            print_free_list (curr_gen_number, seg);

        }
        else if (seg)
        {
            dprintf (GTC_LOG, ("g%d: [%zx %zx[",
                         curr_gen_number,
                         (size_t)generation_allocation_start (generation_of (curr_gen_number)),
                         (size_t)(((curr_gen_number == 0)) ?
                                  (heap_segment_allocated
                                   (generation_start_segment
                                    (generation_of (curr_gen_number)))) :
                                  (generation_allocation_start
                                   (generation_of (curr_gen_number - 1))))
                         ));
            print_free_list (curr_gen_number, seg);
        }
#endif //USE_REGIONS
    }

#endif //TRACE_GC
}

#ifdef BACKGROUND_GC
BOOL gc_heap::bgc_mark_array_range (heap_segment* seg,
                                    BOOL whole_seg_p,
                                    uint8_t** range_beg,
                                    uint8_t** range_end)
{
    uint8_t* seg_start = heap_segment_mem (seg);
    uint8_t* seg_end = (whole_seg_p ? heap_segment_reserved (seg) : align_on_mark_word (heap_segment_allocated (seg)));

    if ((seg_start < background_saved_highest_address) &&
        (seg_end > background_saved_lowest_address))
    {
        *range_beg = max (seg_start, background_saved_lowest_address);
        *range_end = min (seg_end, background_saved_highest_address);
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

void gc_heap::bgc_verify_mark_array_cleared (heap_segment* seg, bool always_verify_p)
{
#ifdef _DEBUG
    if (gc_heap::background_running_p() || always_verify_p)
    {
        uint8_t* range_beg = 0;
        uint8_t* range_end = 0;

        if (bgc_mark_array_range (seg, TRUE, &range_beg, &range_end) || always_verify_p)
        {
            if (always_verify_p)
            {
                range_beg = heap_segment_mem (seg);
                range_end = heap_segment_reserved (seg);
            }
            size_t  markw = mark_word_of (range_beg);
            size_t  markw_end = mark_word_of (range_end);
            while (markw < markw_end)
            {
                if (mark_array [markw])
                {
                    dprintf (1, ("The mark bits at 0x%zx:0x%u(addr: 0x%p) were not cleared",
                                    markw, mark_array [markw], mark_word_address (markw)));
                    FATAL_GC_ERROR();
                }
                markw++;
            }
            uint8_t* p = mark_word_address (markw_end);
            while (p < range_end)
            {
                assert (!(mark_array_marked (p)));
                p++;
            }
        }
    }
#endif //_DEBUG
}

void gc_heap::verify_mark_bits_cleared (uint8_t* obj, size_t s)
{
#ifdef VERIFY_HEAP
    size_t start_mark_bit = mark_bit_of (obj) + 1;
    size_t end_mark_bit = mark_bit_of (obj + s);
    unsigned int startbit = mark_bit_bit (start_mark_bit);
    unsigned int endbit = mark_bit_bit (end_mark_bit);
    size_t startwrd = mark_bit_word (start_mark_bit);
    size_t endwrd = mark_bit_word (end_mark_bit);
    unsigned int result = 0;

    unsigned int firstwrd = ~(lowbits (~0, startbit));
    unsigned int lastwrd = ~(highbits (~0, endbit));

    if (startwrd == endwrd)
    {
        unsigned int wrd = firstwrd & lastwrd;
        result = mark_array[startwrd] & wrd;
        if (result)
        {
            FATAL_GC_ERROR();
        }
        return;
    }

    // verify the first mark word is cleared.
    if (startbit)
    {
        result = mark_array[startwrd] & firstwrd;
        if (result)
        {
            FATAL_GC_ERROR();
        }
        startwrd++;
    }

    for (size_t wrdtmp = startwrd; wrdtmp < endwrd; wrdtmp++)
    {
        result = mark_array[wrdtmp];
        if (result)
        {
            FATAL_GC_ERROR();
        }
    }

    // set the last mark word.
    if (endbit)
    {
        result = mark_array[endwrd] & lastwrd;
        if (result)
        {
            FATAL_GC_ERROR();
        }
    }
#endif //VERIFY_HEAP
}

void gc_heap::verify_mark_array_cleared()
{
#ifdef VERIFY_HEAP
    if (gc_heap::background_running_p() &&
        (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC))
    {
        for (int i = get_start_generation_index(); i < total_generation_count; i++)
        {
            generation* gen = generation_of (i);
            heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

            while (seg)
            {
                bgc_verify_mark_array_cleared (seg);
                seg = heap_segment_next_rw (seg);
            }
        }
    }
#endif //VERIFY_HEAP
}

#endif //BACKGROUND_GC

// This function is called to make sure we don't mess up the segment list
// in SOH. It's called by:
// 1) begin and end of ephemeral GCs
// 2) during bgc sweep when we switch segments.
void gc_heap::verify_soh_segment_list()
{
#ifdef VERIFY_HEAP
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
    {
        for (int i = get_start_generation_index(); i <= max_generation; i++)
        {
            generation* gen = generation_of (i);
            heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
            heap_segment* last_seg = 0;
            while (seg)
            {
                last_seg = seg;
                seg = heap_segment_next_rw (seg);
            }
#ifdef USE_REGIONS
            if (last_seg != generation_tail_region (gen))
#else
            if (last_seg != ephemeral_heap_segment)
#endif //USE_REGIONS
            {
                FATAL_GC_ERROR();
            }
        }
    }
#endif //VERIFY_HEAP
}

// This function can be called at any foreground GCs or blocking GCs. For background GCs,
// it can be called at the end of the final marking; and at any point during background
// sweep.
// NOTE - to be able to call this function during background sweep, we need to temporarily
// NOT clear the mark array bits as we go.
#ifdef BACKGROUND_GC
void gc_heap::verify_partial()
{
    // Different ways to fail.
    BOOL mark_missed_p = FALSE;
    BOOL bad_ref_p = FALSE;
    BOOL free_ref_p = FALSE;

    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        int align_const = get_alignment_constant (i == max_generation);
        heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

        while (seg)
        {
            uint8_t* o = heap_segment_mem (seg);
            uint8_t* end = heap_segment_allocated (seg);

            while (o < end)
            {
                size_t s = size (o);

                BOOL marked_p = background_object_marked (o, FALSE);

                if (marked_p)
                {
                    go_through_object_cl (method_table (o), o, s, oo,
                        {
                            if (*oo)
                            {
                                //dprintf (3, ("VOM: verifying member %zx in obj %zx", (size_t)*oo, o));
                                MethodTable *pMT = method_table (*oo);

                                if (pMT == g_gc_pFreeObjectMethodTable)
                                {
                                    free_ref_p = TRUE;
                                    FATAL_GC_ERROR();
                                }

                                if (!pMT->SanityCheck())
                                {
                                    bad_ref_p = TRUE;
                                    dprintf (1, ("Bad member of %zx %zx",
                                                (size_t)oo, (size_t)*oo));
                                    FATAL_GC_ERROR();
                                }

                                if (current_bgc_state == bgc_final_marking)
                                {
                                    if (marked_p && !background_object_marked (*oo, FALSE))
                                    {
                                        mark_missed_p = TRUE;
                                        FATAL_GC_ERROR();
                                    }
                                }
                            }
                        }
                    );
                }

                o = o + Align(s, align_const);
            }
            seg = heap_segment_next_rw (seg);
        }
    }
}

#endif //BACKGROUND_GC
#ifdef VERIFY_HEAP
void gc_heap::verify_committed_bytes_per_heap()
{
    size_t committed_bookkeeping = 0; // unused
    for (int oh = soh; oh < total_oh_count; oh++)
    {
#ifdef MULTIPLE_HEAPS
        assert (committed_by_oh_per_heap[oh] == compute_committed_bytes_per_heap (oh, committed_bookkeeping));
#else
        assert (committed_by_oh[oh] == compute_committed_bytes_per_heap (oh, committed_bookkeeping));
#endif //MULTIPLE_HEAPS
    }
}

void gc_heap::verify_committed_bytes()
{
    size_t total_committed = 0;
    size_t committed_decommit; // unused
    size_t committed_free; // unused
    size_t committed_bookkeeping = 0;
    size_t new_current_total_committed;
    size_t new_current_total_committed_bookkeeping;
    size_t new_committed_by_oh[recorded_committed_bucket_counts];
    compute_committed_bytes(total_committed, committed_decommit, committed_free,
                            committed_bookkeeping, new_current_total_committed, new_current_total_committed_bookkeeping,
                            new_committed_by_oh);
#ifdef MULTIPLE_HEAPS
    for (int h = 0; h < n_heaps; h++)
    {
        for (int oh = soh; oh < total_oh_count; oh++)
        {
            assert (g_heaps[h]->committed_by_oh_per_heap[oh] == g_heaps[h]->committed_by_oh_per_heap_refresh[oh]);
        }
    }
    for (int i = 0; i < recorded_committed_bucket_counts; i++)
    {
        assert (new_committed_by_oh[i] == committed_by_oh[i]);
    }
#endif //MULTIPLE_HEAPS
    assert (new_current_total_committed_bookkeeping == current_total_committed_bookkeeping);
    assert (new_current_total_committed == current_total_committed);
}

#ifdef USE_REGIONS
void gc_heap::verify_regions (int gen_number, bool can_verify_gen_num, bool can_verify_tail)
{
#ifdef _DEBUG
    // For the given generation, verify that
    //
    // 1) it has at least one region.
    // 2) the tail region is the same as the last region if we following the list of regions
    // in that generation.
    // 3) no region is pointing to itself.
    // 4) if we can verify gen num, each region's gen_num and plan_gen_num are the same and
    // they are the right generation.
    generation* gen = generation_of (gen_number);
    int num_regions_in_gen = 0;
    heap_segment* seg_in_gen = heap_segment_rw (generation_start_segment (gen));
    heap_segment* prev_region_in_gen = 0;
    heap_segment* tail_region = generation_tail_region (gen);

    while (seg_in_gen)
    {
        if (can_verify_gen_num)
        {
            if (heap_segment_gen_num (seg_in_gen) != min (gen_number, (int)max_generation))
            {
                dprintf (REGIONS_LOG, ("h%d gen%d region %p(%p) gen is %d!",
                    heap_number, gen_number, seg_in_gen, heap_segment_mem (seg_in_gen),
                    heap_segment_gen_num (seg_in_gen)));
                FATAL_GC_ERROR();
            }
            if (heap_segment_gen_num (seg_in_gen) != heap_segment_plan_gen_num (seg_in_gen))
            {
                dprintf (REGIONS_LOG, ("h%d gen%d region %p(%p) gen is %d but plan gen is %d!!",
                    heap_number, gen_number, seg_in_gen, heap_segment_mem (seg_in_gen),
                    heap_segment_gen_num (seg_in_gen), heap_segment_plan_gen_num (seg_in_gen)));
                FATAL_GC_ERROR();
            }
        }

        if (heap_segment_allocated (seg_in_gen) > heap_segment_reserved (seg_in_gen))
        {
            dprintf (REGIONS_LOG, ("h%d gen%d region %p alloc %p > reserved %p!!",
                heap_number, gen_number, heap_segment_mem (seg_in_gen),
                heap_segment_allocated (seg_in_gen), heap_segment_reserved (seg_in_gen)));
            FATAL_GC_ERROR();
        }

        prev_region_in_gen = seg_in_gen;
        num_regions_in_gen++;
        heap_segment* next_region = heap_segment_next (seg_in_gen);
        if (seg_in_gen == next_region)
        {
            dprintf (REGIONS_LOG, ("h%d gen%d region %p(%p) pointing to itself!!",
                heap_number, gen_number, seg_in_gen, heap_segment_mem (seg_in_gen)));
            FATAL_GC_ERROR();
        }
        seg_in_gen = next_region;
    }

    if (num_regions_in_gen == 0)
    {
        dprintf (REGIONS_LOG, ("h%d gen%d has no regions!!", heap_number, gen_number));
        FATAL_GC_ERROR();
    }

    if (can_verify_tail && (tail_region != prev_region_in_gen))
    {
        dprintf (REGIONS_LOG, ("h%d gen%d tail region is %p(%p), diff from last region %p(%p)!!",
            heap_number, gen_number,
            tail_region, heap_segment_mem (tail_region),
            prev_region_in_gen, heap_segment_mem (prev_region_in_gen)));
        FATAL_GC_ERROR();
    }
#endif // _DEBUG
}

inline bool is_user_alloc_gen (int gen_number)
{
    return ((gen_number == soh_gen0) || (gen_number == loh_generation) || (gen_number == poh_generation));
}

void gc_heap::verify_regions (bool can_verify_gen_num, bool concurrent_p)
{
#ifdef _DEBUG
    for (int i = 0; i < total_generation_count; i++)
    {
        bool can_verify_tail = (concurrent_p ? !is_user_alloc_gen (i) : true);
        verify_regions (i, can_verify_gen_num, can_verify_tail);

        if (can_verify_gen_num &&
            can_verify_tail &&
            (i >= max_generation))
        {
            verify_committed_bytes_per_heap ();
        }
    }
#endif // _DEBUG
}

#endif //USE_REGIONS

BOOL gc_heap::check_need_card (uint8_t* child_obj, int gen_num_for_cards,
                               uint8_t* low, uint8_t* high)
{
#ifdef USE_REGIONS
    return (is_in_heap_range (child_obj) && (get_region_gen_num (child_obj) < gen_num_for_cards));
#else
    return ((child_obj < high) && (child_obj >= low));
#endif //USE_REGIONS
}

void gc_heap::enter_gc_lock_for_verify_heap()
{
#ifdef VERIFY_HEAP
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
    {
        enter_spin_lock (&gc_heap::gc_lock);
        dprintf (SPINLOCK_LOG, ("enter gc_lock for verify_heap"));
    }
#endif // VERIFY_HEAP
}

void gc_heap::leave_gc_lock_for_verify_heap()
{
#ifdef VERIFY_HEAP
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
    {
        dprintf (SPINLOCK_LOG, ("leave gc_lock taken for verify_heap"));
        leave_spin_lock (&gc_heap::gc_lock);
    }
#endif // VERIFY_HEAP
}

void gc_heap::verify_heap (BOOL begin_gc_p)
{
    int heap_verify_level = static_cast<int>(GCConfig::GetHeapVerifyLevel());

#ifdef MULTIPLE_HEAPS
    t_join* current_join = &gc_t_join;
#ifdef BACKGROUND_GC
    if (settings.concurrent && (bgc_thread_id.IsCurrentThread()))
    {
        // We always call verify_heap on entry of GC on the SVR GC threads.
        current_join = &bgc_t_join;
    }
#endif //BACKGROUND_GC
#endif //MULTIPLE_HEAPS

#ifndef TRACE_GC
    UNREFERENCED_PARAMETER(begin_gc_p);
#endif //!TRACE_GC

#ifdef BACKGROUND_GC
    dprintf (2,("[%s]GC#%zu(%s): Verifying heap - begin",
        (begin_gc_p ? "BEG" : "END"),
        VolatileLoad(&settings.gc_index),
        get_str_gc_type()));
#else
    dprintf (2,("[%s]GC#%zu: Verifying heap - begin",
                (begin_gc_p ? "BEG" : "END"), VolatileLoad(&settings.gc_index)));
#endif //BACKGROUND_GC

#ifndef MULTIPLE_HEAPS
#ifndef USE_REGIONS
    if ((ephemeral_low != generation_allocation_start (generation_of (max_generation - 1))) ||
        (ephemeral_high != heap_segment_reserved (ephemeral_heap_segment)))
    {
        FATAL_GC_ERROR();
    }
#endif //!USE_REGIONS
#endif //MULTIPLE_HEAPS

#ifdef BACKGROUND_GC
    //don't touch the memory because the program is allocating from it.
    if (!settings.concurrent)
#endif //BACKGROUND_GC
    {
        if (!(heap_verify_level & GCConfig::HEAPVERIFY_NO_MEM_FILL))
        {
            // 0xaa the unused portions of segments.
            for (int i = get_start_generation_index(); i < total_generation_count; i++)
            {
                generation* gen1 = generation_of (i);
                heap_segment* seg1 = heap_segment_rw (generation_start_segment (gen1));

                while (seg1)
                {
                    uint8_t* clear_start = heap_segment_allocated (seg1) - plug_skew;
                    if (heap_segment_used (seg1) > clear_start)
                    {
                        dprintf (3, ("setting end of seg %p: [%p-[%p to 0xaa",
                            heap_segment_mem (seg1),
                            clear_start ,
                            heap_segment_used (seg1)));
                        memset (heap_segment_allocated (seg1) - plug_skew, 0xaa,
                            (heap_segment_used (seg1) - clear_start));
                    }
                    seg1 = heap_segment_next_rw (seg1);
                }
            }
        }
    }

#ifndef USE_REGIONS
#ifdef MULTIPLE_HEAPS
    current_join->join(this, gc_join_verify_copy_table);
    if (current_join->joined())
#endif //MULTIPLE_HEAPS
    {
        // in concurrent GC, new segment could be allocated when GC is working so the card brick table might not be updated at this point
        copy_brick_card_table_on_growth ();

#ifdef MULTIPLE_HEAPS
        current_join->restart();
#endif //MULTIPLE_HEAPS
    }
#endif //!USE_REGIONS

    //verify that the generation structures makes sense
    {
#ifdef _DEBUG
#ifdef USE_REGIONS
        verify_regions (true, settings.concurrent);
#else //USE_REGIONS
        generation* gen = generation_of (max_generation);

        assert (generation_allocation_start (gen) ==
                heap_segment_mem (heap_segment_rw (generation_start_segment (gen))));
        int gen_num = max_generation-1;
        generation* prev_gen = gen;
        while (gen_num >= 0)
        {
            gen = generation_of (gen_num);
            assert (generation_allocation_segment (gen) == ephemeral_heap_segment);
            assert (generation_allocation_start (gen) >= heap_segment_mem (ephemeral_heap_segment));
            assert (generation_allocation_start (gen) < heap_segment_allocated (ephemeral_heap_segment));

            if (generation_start_segment (prev_gen ) ==
                generation_start_segment (gen))
            {
                assert (generation_allocation_start (prev_gen) <
                        generation_allocation_start (gen));
            }
            prev_gen = gen;
            gen_num--;
        }
#endif //USE_REGIONS
#endif //_DEBUG
    }

    size_t          total_objects_verified = 0;
    size_t          total_objects_verified_deep = 0;

    BOOL            bCurrentBrickInvalid = FALSE;
    size_t          last_valid_brick = 0;
    size_t          curr_brick = 0;
    size_t          prev_brick = (size_t)-1;
    int             gen_num_for_cards = 0;
#ifdef USE_REGIONS
    int             gen_num_to_stop = 0;
    uint8_t*        e_high = 0;
    uint8_t*        next_boundary = 0;
#else //USE_REGIONS
    // For no regions the gen number is separately reduced when we detect the ephemeral seg.
    int gen_num_to_stop = max_generation;
    uint8_t*        e_high = ephemeral_high;
    uint8_t*        next_boundary = generation_allocation_start (generation_of (max_generation - 1));
    uint8_t*        begin_youngest = generation_allocation_start(generation_of(0));
#endif //!USE_REGIONS

    // go through all generations starting with the highest
    for (int curr_gen_num = total_generation_count - 1; curr_gen_num >= gen_num_to_stop; curr_gen_num--)
    {
        int             align_const = get_alignment_constant (curr_gen_num == max_generation);
        BOOL            large_brick_p = (curr_gen_num > max_generation);
#ifdef USE_REGIONS
        gen_num_for_cards = ((curr_gen_num >= max_generation) ? max_generation : curr_gen_num);
#endif //USE_REGIONS
        heap_segment*   seg = heap_segment_in_range (generation_start_segment (generation_of (curr_gen_num) ));

        while (seg)
        {
            uint8_t*        curr_object = heap_segment_mem (seg);
            uint8_t*        prev_object = 0;

            bool verify_bricks_p = true;
#ifdef USE_REGIONS
            if (heap_segment_read_only_p(seg))
            {
                dprintf(1, ("seg %zx is ro! Shouldn't happen with regions", (size_t)seg));
                FATAL_GC_ERROR();
            }
            if (heap_segment_gen_num (seg) != heap_segment_plan_gen_num (seg))
            {
                dprintf (1, ("Seg %p, gen num is %d, plan gen num is %d",
                    heap_segment_mem (seg), heap_segment_gen_num (seg), heap_segment_plan_gen_num (seg)));
                FATAL_GC_ERROR();
            }
#else //USE_REGIONS
            if (heap_segment_read_only_p(seg))
            {
                size_t current_brick = brick_of(max(heap_segment_mem(seg), lowest_address));
                size_t end_brick = brick_of(min(heap_segment_reserved(seg), highest_address) - 1);
                while (current_brick <= end_brick)
                {
                    if (brick_table[current_brick] != 0)
                    {
                        dprintf(1, ("Verifying Heap: %zx brick of a frozen segment is not zeroed", current_brick));
                        FATAL_GC_ERROR();
                    }
                    current_brick++;
                }
                verify_bricks_p = false;
            }
#endif //USE_REGIONS

#ifdef BACKGROUND_GC
            BOOL consider_bgc_mark_p    = FALSE;
            BOOL check_current_sweep_p  = FALSE;
            BOOL check_saved_sweep_p    = FALSE;
            should_check_bgc_mark (seg, &consider_bgc_mark_p, &check_current_sweep_p, &check_saved_sweep_p);
#endif //BACKGROUND_GC

            while (curr_object < heap_segment_allocated (seg))
            {
                if (is_mark_set (curr_object))
                {
                    dprintf (1, ("curr_object: %zx is marked!",(size_t)curr_object));
                    FATAL_GC_ERROR();
                }

                size_t s = size (curr_object);
                dprintf (3, ("o: %zx, s: %zu", (size_t)curr_object, s));
                if (s == 0)
                {
                    dprintf (1, ("Verifying Heap: size of current object %p == 0", curr_object));
                    FATAL_GC_ERROR();
                }

#ifndef USE_REGIONS
                // handle generation boundaries within ephemeral segment
                if (seg == ephemeral_heap_segment)
                {
                    if ((curr_gen_num > 0) && (curr_object >= next_boundary))
                    {
                        curr_gen_num--;
                        if (curr_gen_num > 0)
                        {
                            next_boundary = generation_allocation_start (generation_of (curr_gen_num - 1));
                        }
                    }
                }
#endif //!USE_REGIONS

#ifdef USE_REGIONS
                if (verify_bricks_p && curr_gen_num != 0)
#else
                // If object is not in the youngest generation, then lets
                // verify that the brick table is correct....
                if (verify_bricks_p && ((seg != ephemeral_heap_segment) ||
                     (brick_of(curr_object) < brick_of(begin_youngest))))
#endif //USE_REGIONS
                {
                    curr_brick = brick_of(curr_object);

                    // Brick Table Verification...
                    //
                    // On brick transition
                    //     if brick is negative
                    //          verify that brick indirects to previous valid brick
                    //     else
                    //          set current brick invalid flag to be flipped if we
                    //          encounter an object at the correct place
                    //
                    if (curr_brick != prev_brick)
                    {
                        // If the last brick we were examining had positive
                        // entry but we never found the matching object, then
                        // we have a problem
                        // If prev_brick was the last one of the segment
                        // it's ok for it to be invalid because it is never looked at
                        if (bCurrentBrickInvalid &&
                            (curr_brick != brick_of (heap_segment_mem (seg))) &&
                            !heap_segment_read_only_p (seg))
                        {
                            dprintf (1, ("curr brick %zx invalid", curr_brick));
                            FATAL_GC_ERROR();
                        }

                        if (large_brick_p)
                        {
                            //large objects verify the table only if they are in
                            //range.
                            if ((heap_segment_reserved (seg) <= highest_address) &&
                                (heap_segment_mem (seg) >= lowest_address) &&
                                brick_table [curr_brick] != 0)
                            {
                                dprintf (1, ("curr_brick %zx for large object %zx is set to %zx",
                                    curr_brick, (size_t)curr_object, (size_t)brick_table[curr_brick]));
                                FATAL_GC_ERROR();
                            }
                            else
                            {
                                bCurrentBrickInvalid = FALSE;
                            }
                        }
                        else
                        {
                            // If the current brick contains a negative value make sure
                            // that the indirection terminates at the last  valid brick
                            if (brick_table [curr_brick] <= 0)
                            {
                                if (brick_table [curr_brick] == 0)
                                {
                                    dprintf(1, ("curr_brick %zx for object %zx set to 0",
                                            curr_brick, (size_t)curr_object));
                                    FATAL_GC_ERROR();
                                }
                                ptrdiff_t i = curr_brick;
                                while ((i >= ((ptrdiff_t) brick_of (heap_segment_mem (seg)))) &&
                                       (brick_table[i] < 0))
                                {
                                    i = i + brick_table[i];
                                }
                                if (i <  ((ptrdiff_t)(brick_of (heap_segment_mem (seg))) - 1))
                                {
                                    dprintf (1, ("ptrdiff i: %zx < brick_of (heap_segment_mem (seg)):%zx - 1. curr_brick: %zx",
                                            i, brick_of (heap_segment_mem (seg)),
                                            curr_brick));
                                    FATAL_GC_ERROR();
                                }
                                bCurrentBrickInvalid = FALSE;
                            }
                            else if (!heap_segment_read_only_p (seg))
                            {
                                bCurrentBrickInvalid = TRUE;
                            }
                        }
                    }

                    if (bCurrentBrickInvalid)
                    {
                        if (curr_object == (brick_address(curr_brick) + brick_table[curr_brick] - 1))
                        {
                            bCurrentBrickInvalid = FALSE;
                            last_valid_brick = curr_brick;
                        }
                    }
                }

                if (*((uint8_t**)curr_object) != (uint8_t *) g_gc_pFreeObjectMethodTable)
                {
#ifdef FEATURE_LOH_COMPACTION
                    if ((curr_gen_num == loh_generation) && (prev_object != 0))
                    {
                        assert (method_table (prev_object) == g_gc_pFreeObjectMethodTable);
                    }
#endif //FEATURE_LOH_COMPACTION

                    total_objects_verified++;

                    BOOL can_verify_deep = TRUE;
#ifdef BACKGROUND_GC
                    can_verify_deep = fgc_should_consider_object (curr_object, seg, consider_bgc_mark_p, check_current_sweep_p, check_saved_sweep_p);
#endif //BACKGROUND_GC

                    BOOL deep_verify_obj = can_verify_deep;
                    if ((heap_verify_level & GCConfig::HEAPVERIFY_DEEP_ON_COMPACT) && !settings.compaction)
                        deep_verify_obj = FALSE;

                    ((CObjectHeader*)curr_object)->ValidateHeap(deep_verify_obj);

                    if (can_verify_deep)
                    {
                        if (curr_gen_num > 0)
                        {
                            BOOL need_card_p = FALSE;
                            if (contain_pointers_or_collectible (curr_object))
                            {
                                dprintf (4, ("curr_object: %zx", (size_t)curr_object));
                                size_t crd = card_of (curr_object);
                                BOOL found_card_p = card_set_p (crd);

#ifdef COLLECTIBLE_CLASS
                                if (is_collectible(curr_object))
                                {
                                    uint8_t* class_obj = get_class_object (curr_object);
                                    if (check_need_card (class_obj, gen_num_for_cards, next_boundary, e_high))
                                    {
                                        if (!found_card_p)
                                        {
                                            dprintf (1, ("Card not set, curr_object = [%zx:%zx pointing to class object %p",
                                                        card_of (curr_object), (size_t)curr_object, class_obj));
                                            FATAL_GC_ERROR();
                                        }
                                    }
                                }
#endif //COLLECTIBLE_CLASS

                                if (contain_pointers(curr_object))
                                {
                                    go_through_object_nostart
                                        (method_table(curr_object), curr_object, s, oo,
                                        {
                                            if (crd != card_of ((uint8_t*)oo))
                                            {
                                                crd = card_of ((uint8_t*)oo);
                                                found_card_p = card_set_p (crd);
                                                need_card_p = FALSE;
                                            }
                                            if (*oo && check_need_card (*oo, gen_num_for_cards, next_boundary, e_high))
                                            {
                                                need_card_p = TRUE;
                                            }

                                            if (need_card_p && !found_card_p)
                                            {
                                                dprintf (1, ("(in loop) Card not set, curr_object = [%zx:%zx, %zx:%zx[",
                                                            card_of (curr_object), (size_t)curr_object,
                                                            card_of (curr_object+Align(s, align_const)),
                                                            (size_t)(curr_object+Align(s, align_const))));
                                                FATAL_GC_ERROR();
                                            }
                                        }
                                            );
                                }
                                if (need_card_p && !found_card_p)
                                {
                                    dprintf (1, ("Card not set, curr_object = [%zx:%zx, %zx:%zx[",
                                        card_of (curr_object), (size_t)curr_object,
                                        card_of (curr_object + Align(s, align_const)),
                                        (size_t)(curr_object + Align(s, align_const))));
                                    FATAL_GC_ERROR();
                                }
                            }
                        }
                        total_objects_verified_deep++;
                    }
                }

                prev_object = curr_object;
                prev_brick = curr_brick;
                curr_object = curr_object + Align(s, align_const);
                if (curr_object < prev_object)
                {
                    dprintf (1, ("overflow because of a bad object size: %p size %zx", prev_object, s));
                    FATAL_GC_ERROR();
                }
            }

            if (curr_object > heap_segment_allocated(seg))
            {
                dprintf (1, ("Verifiying Heap: curr_object: %zx > heap_segment_allocated (seg: %zx) %p",
                        (size_t)curr_object, (size_t)seg, heap_segment_allocated (seg)));
                FATAL_GC_ERROR();
            }

            seg = heap_segment_next_in_range (seg);
        }
    }

#ifdef BACKGROUND_GC
    dprintf (2, ("(%s)(%s)(%s) total_objects_verified is %zd, total_objects_verified_deep is %zd",
                 get_str_gc_type(),
                 (begin_gc_p ? "BEG" : "END"),
                 ((current_c_gc_state == c_gc_state_planning) ? "in plan" : "not in plan"),
                 total_objects_verified, total_objects_verified_deep));
    if (current_c_gc_state != c_gc_state_planning)
    {
        assert (total_objects_verified == total_objects_verified_deep);
    }
#endif //BACKGROUND_GC

    verify_free_lists();

#ifdef FEATURE_PREMORTEM_FINALIZATION
    finalize_queue->CheckFinalizerObjects();
#endif // FEATURE_PREMORTEM_FINALIZATION

    {
        // to be consistent with handle table APIs pass a ScanContext*
        // to provide the heap number.  the SC isn't complete though so
        // limit its scope to handle table verification.
        ScanContext sc;
        sc.thread_number = heap_number;
        sc.thread_count = n_heaps;
        GCScan::VerifyHandleTable(max_generation, max_generation, &sc);
    }

#ifdef MULTIPLE_HEAPS
    current_join->join(this, gc_join_verify_objects_done);
    if (current_join->joined())
#endif //MULTIPLE_HEAPS
    {
        GCToEEInterface::VerifySyncTableEntry();
#ifdef MULTIPLE_HEAPS
#ifdef USE_REGIONS
        // check that the heaps not in use have not been inadvertently written to
        for (int hn = n_heaps; hn < n_max_heaps; hn++)
        {
            gc_heap* hp = g_heaps[hn];
            hp->check_decommissioned_heap();
        }
#endif //USE_REGIONS

        current_join->restart();
#endif //MULTIPLE_HEAPS
    }

#ifdef BACKGROUND_GC
    if (settings.concurrent)
    {
        verify_mark_array_cleared();
    }
    dprintf (2,("GC%zu(%s): Verifying heap - end",
        VolatileLoad(&settings.gc_index),
        get_str_gc_type()));
#else
    dprintf (2,("GC#d: Verifying heap - end", VolatileLoad(&settings.gc_index)));
#endif //BACKGROUND_GC
}

#endif //VERIFY_HEAP
#ifdef BACKGROUND_GC
void gc_heap::add_bgc_pause_duration_0()
{
    if (settings.concurrent)
    {
        uint64_t suspended_end_ts = GetHighPrecisionTimeStamp();
        size_t pause_duration = (size_t)(suspended_end_ts - suspended_start_time);
        last_recorded_gc_info* last_gc_info = &(last_bgc_info[last_bgc_info_index]);
        last_gc_info->pause_durations[0] = pause_duration;
        if (last_gc_info->index < last_ephemeral_gc_info.index)
        {
            last_gc_info->pause_durations[0] -= last_ephemeral_gc_info.pause_durations[0];
        }

        total_suspended_time += last_gc_info->pause_durations[0];
    }
}

last_recorded_gc_info* gc_heap::get_completed_bgc_info()
{
    int completed_bgc_index = gc_heap::background_running_p() ?
        (int)(!(gc_heap::last_bgc_info_index)) : (int)gc_heap::last_bgc_info_index;
    return &gc_heap::last_bgc_info[completed_bgc_index];
}

#endif //BACKGROUND_GC

const char* gc_heap::get_str_gc_type()
{
#ifdef BACKGROUND_GC
    return (settings.concurrent ? "BGC" : (gc_heap::background_running_p () ? "FGC" : "NGC"));
#else // BACKGROUND_GC
    return "NGC";
#endif // BACKGROUND_GC
}

#ifdef GC_CONFIG_DRIVEN
void gc_heap::record_interesting_info_per_heap()
{
    // datapoints are always from the last blocking GC so don't record again
    // for BGCs.
    if (!(settings.concurrent))
    {
        for (int i = 0; i < max_idp_count; i++)
        {
            interesting_data_per_heap[i] += interesting_data_per_gc[i];
        }
    }

    int compact_reason = get_gc_data_per_heap()->get_mechanism (gc_heap_compact);
    if (compact_reason >= 0)
        (compact_reasons_per_heap[compact_reason])++;
    int expand_mechanism = get_gc_data_per_heap()->get_mechanism (gc_heap_expand);
    if (expand_mechanism >= 0)
        (expand_mechanisms_per_heap[expand_mechanism])++;

    for (int i = 0; i < max_gc_mechanism_bits_count; i++)
    {
        if (get_gc_data_per_heap()->is_mechanism_bit_set ((gc_mechanism_bit_per_heap)i))
            (interesting_mechanism_bits_per_heap[i])++;
    }

    //         h#  | GC  | gen | C   | EX  | NF  | BF  | ML  | DM  || PreS | PostS | Merge | Conv | Pre | Post | PrPo | PreP | PostP |
    cprintf (("%2d | %6d | %1d | %1s | %2s | %2s | %2s | %2s | %2s || %5Id | %5Id | %5Id | %5Id | %5Id | %5Id | %5Id | %5Id | %5Id |",
            heap_number,
            (size_t)settings.gc_index,
            settings.condemned_generation,
            // TEMP - I am just doing this for wks GC 'cause I wanna see the pattern of doing C/S GCs.
            (settings.compaction ? (((compact_reason >= 0) && gc_heap_compact_reason_mandatory_p[compact_reason]) ? "M" : "W") : ""), // compaction
            ((expand_mechanism >= 0)? "X" : ""), // EX
            ((expand_mechanism == expand_reuse_normal) ? "X" : ""), // NF
            ((expand_mechanism == expand_reuse_bestfit) ? "X" : ""), // BF
            (get_gc_data_per_heap()->is_mechanism_bit_set (gc_mark_list_bit) ? "X" : ""), // ML
            (get_gc_data_per_heap()->is_mechanism_bit_set (gc_demotion_bit) ? "X" : ""), // DM
            interesting_data_per_gc[idp_pre_short],
            interesting_data_per_gc[idp_post_short],
            interesting_data_per_gc[idp_merged_pin],
            interesting_data_per_gc[idp_converted_pin],
            interesting_data_per_gc[idp_pre_pin],
            interesting_data_per_gc[idp_post_pin],
            interesting_data_per_gc[idp_pre_and_post_pin],
            interesting_data_per_gc[idp_pre_short_padded],
            interesting_data_per_gc[idp_post_short_padded]));
}

void gc_heap::record_global_mechanisms()
{
    for (int i = 0; i < max_global_mechanisms_count; i++)
    {
        if (gc_data_global.get_mechanism_p ((gc_global_mechanism_p)i))
        {
            ::record_global_mechanism (i);
        }
    }
}

#endif //GC_CONFIG_DRIVEN

//------------------------------------------------------------------------------
//
//                      End of VM specific support
//
//------------------------------------------------------------------------------
void gc_heap::walk_heap_per_heap (walk_fn fn, void* context, int gen_number, BOOL walk_large_object_heap_p)
{
    generation* gen = gc_heap::generation_of (gen_number);
    heap_segment*    seg = generation_start_segment (gen);
    uint8_t* x = ((gen_number == max_generation) ? heap_segment_mem (seg) : get_soh_start_object (seg, gen));
    uint8_t*       end = heap_segment_allocated (seg);
    int align_const = get_alignment_constant (TRUE);
    BOOL walk_pinned_object_heap = walk_large_object_heap_p;

    while (1)
    {
        if (x >= end)
        {
            if ((seg = heap_segment_next (seg)) != 0)
            {
                x = heap_segment_mem (seg);
                end = heap_segment_allocated (seg);
                continue;
            }
#ifdef USE_REGIONS
            else if (gen_number > 0)
            {
                // advance to next lower generation
                gen_number--;
                gen = gc_heap::generation_of (gen_number);
                seg = generation_start_segment (gen);

                x = heap_segment_mem (seg);
                end = heap_segment_allocated (seg);
                continue;
            }
#endif // USE_REGIONS
            else
            {
                if (walk_large_object_heap_p)
                {
                    walk_large_object_heap_p = FALSE;
                    seg = generation_start_segment (large_object_generation);
                }
                else if (walk_pinned_object_heap)
                {
                    walk_pinned_object_heap = FALSE;
                    seg = generation_start_segment (pinned_object_generation);
                }
                else
                {
                    break;
                }

                align_const = get_alignment_constant (FALSE);
                x = heap_segment_mem (seg);
                end = heap_segment_allocated (seg);
                continue;
            }
        }

        size_t s = size (x);
        CObjectHeader* o = (CObjectHeader*)x;

        if (!o->IsFree())

        {
            _ASSERTE(((size_t)o & 0x3) == 0); // Last two bits should never be set at this point

            if (!fn (o->GetObjectBase(), context))
                return;
        }
        x = x + Align (s, align_const);
    }
}

void gc_heap::walk_heap (walk_fn fn, void* context, int gen_number, BOOL walk_large_object_heap_p)
{
#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];

        hp->walk_heap_per_heap (fn, context, gen_number, walk_large_object_heap_p);
    }
#else
    walk_heap_per_heap(fn, context, gen_number, walk_large_object_heap_p);
#endif //MULTIPLE_HEAPS
}

#ifdef FEATURE_BASICFREEZE
void gc_heap::walk_read_only_segment(heap_segment *seg, void *pvContext, object_callback_func pfnMethodTable, object_callback_func pfnObjRef)
{
    uint8_t *o = heap_segment_mem(seg);

    int alignment = get_alignment_constant(TRUE);

    while (o < heap_segment_allocated(seg))
    {
        pfnMethodTable(pvContext, o);

        if (contain_pointers (o))
        {
            go_through_object_nostart (method_table (o), o, size(o), oo,
                   {
                       if (*oo)
                           pfnObjRef(pvContext, oo);
                   }
            );
        }

        o += Align(size(o), alignment);
    }
}

#endif //FEATURE_BASICFREEZE
