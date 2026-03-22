// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// static

// returns the segment before seg.
heap_segment* heap_segment_prev (heap_segment* begin, heap_segment* seg)
{
    assert (begin != 0);
    heap_segment* prev = begin;
    heap_segment* current = heap_segment_next (begin);

    while (current && current != seg)
    {
        prev = current;
        current = heap_segment_next (current);
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

#ifdef WRITE_WATCH
#ifdef BACKGROUND_GC
void gc_heap::reset_write_watch_for_gc_heap(void* base_address, size_t region_size)
{
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    SoftwareWriteWatch::ClearDirty(base_address, region_size);
#else // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    GCToOSInterface::ResetWriteWatch(base_address, region_size);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

// static
void gc_heap::get_write_watch_for_gc_heap(bool reset, void *base_address, size_t region_size,
                                          void** dirty_pages, uintptr_t* dirty_page_count_ref,
                                          bool is_runtime_suspended)
{
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    SoftwareWriteWatch::GetDirty(base_address, region_size, dirty_pages, dirty_page_count_ref,
                                 reset, is_runtime_suspended);
#else // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    UNREFERENCED_PARAMETER(is_runtime_suspended);
    bool success = GCToOSInterface::GetWriteWatch(reset, base_address, region_size, dirty_pages,
                                                  dirty_page_count_ref);
    assert(success);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

inline
void gc_heap::switch_one_quantum()
{
    enable_preemptive ();
    GCToOSInterface::Sleep (1);
    disable_preemptive (true);
}

void gc_heap::reset_ww_by_chunk (uint8_t* start_address, size_t total_reset_size)
{
    size_t reset_size = 0;
    size_t remaining_reset_size = 0;
    size_t next_reset_size = 0;

    while (reset_size != total_reset_size)
    {
        remaining_reset_size = total_reset_size - reset_size;
        next_reset_size = ((remaining_reset_size >= ww_reset_quantum) ?
            ww_reset_quantum : remaining_reset_size);
        if (next_reset_size)
        {
            reset_write_watch_for_gc_heap(start_address, next_reset_size);
            reset_size += next_reset_size;

            switch_one_quantum();
        }
    }

    assert (reset_size == total_reset_size);
}

// This does a Sleep(1) for every reset ww_reset_quantum bytes of reset
// we do concurrently.
void gc_heap::switch_on_reset (BOOL concurrent_p, size_t* current_total_reset_size, size_t last_reset_size)
{
    if (concurrent_p)
    {
        *current_total_reset_size += last_reset_size;

        dprintf (2, ("reset %zd bytes so far", *current_total_reset_size));

        if (*current_total_reset_size > ww_reset_quantum)
        {
            switch_one_quantum();

            *current_total_reset_size = 0;
        }
    }
}

void gc_heap::reset_write_watch (BOOL concurrent_p)
{
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    // Software write watch currently requires the runtime to be suspended during reset.
    // See SoftwareWriteWatch::ClearDirty().
    assert(!concurrent_p);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    dprintf (2, ("bgc lowest: %p, bgc highest: %p",
        background_saved_lowest_address, background_saved_highest_address));

    size_t reset_size = 0;

    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        heap_segment* seg = heap_segment_rw (generation_start_segment (generation_of (i)));

        while (seg)
        {
            uint8_t* base_address = align_lower_page (heap_segment_mem (seg));
            base_address = max (base_address, background_saved_lowest_address);

            uint8_t* high_address = ((seg == ephemeral_heap_segment) ?
                alloc_allocated : heap_segment_allocated (seg));
            high_address = min (high_address, background_saved_highest_address);

            if (base_address < high_address)
            {
                size_t reset_size = 0;
                size_t region_size = high_address - base_address;
                dprintf (3, ("h%d, gen: %x, ww: [%zx(%zd)", heap_number, i, (size_t)base_address, region_size));
                //reset_ww_by_chunk (base_address, region_size);
                reset_write_watch_for_gc_heap(base_address, region_size);
                switch_on_reset (concurrent_p, &reset_size, region_size);
            }

            seg = heap_segment_next_rw (seg);

            concurrent_print_time_delta (i == max_generation ? "CRWW soh": "CRWW uoh");
        }
    }
}

#endif //BACKGROUND_GC
#endif //WRITE_WATCH
#ifdef BACKGROUND_GC
void gc_heap::init_background_gc ()
{
    //reset the allocation so foreground gc can allocate into older (max_generation) generation
    generation* gen = generation_of (max_generation);
    generation_allocation_pointer (gen)= 0;
    generation_allocation_limit (gen) = 0;
    generation_allocation_segment (gen) = heap_segment_rw (generation_start_segment (gen));

    _ASSERTE(generation_allocation_segment(gen) != NULL);

#ifdef DOUBLY_LINKED_FL
    generation_set_bgc_mark_bit_p (gen) = FALSE;
#endif //DOUBLY_LINKED_FL

#ifndef USE_REGIONS
    //reset the plan allocation for each segment
    for (heap_segment* seg = generation_allocation_segment (gen); seg != ephemeral_heap_segment;
        seg = heap_segment_next_rw (seg))
    {
        heap_segment_plan_allocated (seg) = heap_segment_allocated (seg);
    }
#endif //!USE_REGIONS

    if (heap_number == 0)
    {
        dprintf (2, ("heap%d: bgc lowest: %p, highest: %p",
            heap_number,
            background_saved_lowest_address,
            background_saved_highest_address));
    }
}

#endif //BACKGROUND_GC

inline
void fire_drain_mark_list_event (size_t mark_list_objects)
{
    FIRE_EVENT(BGCDrainMark, mark_list_objects);
}

inline
void fire_revisit_event (size_t dirtied_pages,
                         size_t marked_objects,
                         BOOL large_objects_p)
{
    FIRE_EVENT(BGCRevisit, dirtied_pages, marked_objects, large_objects_p);
}

inline
void fire_overflow_event (uint8_t* overflow_min,
                          uint8_t* overflow_max,
                          size_t marked_objects,
                          int gen_number)
{
    FIRE_EVENT(BGCOverflow_V1, (uint64_t)overflow_min, (uint64_t)overflow_max, marked_objects, gen_number == loh_generation, gen_number);
}

void gc_heap::concurrent_print_time_delta (const char* msg)
{
#ifdef TRACE_GC
    uint64_t current_time = GetHighPrecisionTimeStamp();
    size_t elapsed_time_ms = (size_t)((current_time - time_bgc_last) / 1000);
    time_bgc_last = current_time;

    dprintf (2, ("h%d: %s T %zd ms", heap_number, msg, elapsed_time_ms));
#else
    UNREFERENCED_PARAMETER(msg);
#endif //TRACE_GC
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

#ifdef USE_REGIONS
void gc_heap::set_background_overflow_p (uint8_t* oo)
{
    heap_segment* overflow_region = get_region_info_for_address (oo);
    overflow_region->flags |= heap_segment_flags_overflow;
    dprintf (3,("setting overflow flag for region %p", heap_segment_mem (overflow_region)));
    background_overflow_p = TRUE;
}

#endif //USE_REGIONS

void gc_heap::background_mark_simple1 (uint8_t* oo THREAD_NUMBER_DCL)
{
    uint8_t** mark_stack_limit = &background_mark_stack_array[background_mark_stack_array_length];

    background_mark_stack_tos = background_mark_stack_array;

    while (1)
    {
#ifdef MULTIPLE_HEAPS
#else  //MULTIPLE_HEAPS
        const int thread = 0;
#endif //MULTIPLE_HEAPS
        if (oo)
        {
            size_t s = 0;
            if ((((size_t)oo & 1) == 0) && ((s = size (oo)) < (partial_size_th*sizeof (uint8_t*))))
            {
                BOOL overflow_p = FALSE;

                if (background_mark_stack_tos + (s) /sizeof (uint8_t*) >= (mark_stack_limit - 1))
                {
                    size_t num_components = ((method_table(oo))->HasComponentSize() ? ((CObjectHeader*)oo)->GetNumComponents() : 0);
                    size_t num_pointers = CGCDesc::GetNumPointers(method_table(oo), s, num_components);
                    if (background_mark_stack_tos + num_pointers >= (mark_stack_limit - 1))
                    {
                        dprintf (2, ("h%d: %zd left, obj (mt: %p) %zd ptrs",
                            heap_number,
                            (size_t)(mark_stack_limit - 1 - background_mark_stack_tos),
                            method_table(oo),
                            num_pointers));

                        bgc_overflow_count++;
                        overflow_p = TRUE;
                    }
                }

                if (overflow_p == FALSE)
                {
                    dprintf(3,("pushing mark for %zx ", (size_t)oo));

                    go_through_object_cl (method_table(oo), oo, s, ppslot,
                    {
                        uint8_t* o = *ppslot;
                        Prefetch(o);
                        if (background_mark (o,
                                             background_saved_lowest_address,
                                             background_saved_highest_address))
                        {
                            //m_boundary (o);
                            size_t obj_size = size (o);
                            bpromoted_bytes (thread) += obj_size;
                            if (contain_pointers_or_collectible (o))
                            {
                                *(background_mark_stack_tos++) = o;

                            }
                        }
                    }
                        );
                }
                else
                {
                    dprintf (3,("background mark stack overflow for object %zx ", (size_t)oo));
#ifdef USE_REGIONS
                    set_background_overflow_p (oo);
#else //USE_REGIONS
                    background_min_overflow_address = min (background_min_overflow_address, oo);
                    background_max_overflow_address = max (background_max_overflow_address, oo);
#endif //USE_REGIONS
                }
            }
            else
            {
                uint8_t* start = oo;
                if ((size_t)oo & 1)
                {
                    oo = (uint8_t*)((size_t)oo & ~1);
                    start = *(--background_mark_stack_tos);
                    dprintf (4, ("oo: %zx, start: %zx\n", (size_t)oo, (size_t)start));
                }
#ifdef COLLECTIBLE_CLASS
                else
                {
                    // If there's a class object, push it now. We are guaranteed to have the slot since
                    // we just popped one object off.
                    if (is_collectible (oo))
                    {
                        uint8_t* class_obj = get_class_object (oo);
                        if (background_mark (class_obj,
                                            background_saved_lowest_address,
                                            background_saved_highest_address))
                        {
                            size_t obj_size = size (class_obj);
                            bpromoted_bytes (thread) += obj_size;

                            *(background_mark_stack_tos++) = class_obj;
                        }
                    }

                    if (!contain_pointers (oo))
                    {
                        goto next_level;
                    }
                }
#endif //COLLECTIBLE_CLASS

                s = size (oo);

                BOOL overflow_p = FALSE;

                if (background_mark_stack_tos + (num_partial_refs + 2)  >= mark_stack_limit)
                {
                    size_t num_components = ((method_table(oo))->HasComponentSize() ? ((CObjectHeader*)oo)->GetNumComponents() : 0);
                    size_t num_pointers = CGCDesc::GetNumPointers(method_table(oo), s, num_components);

                    dprintf (2, ("h%d: PM: %zd left, obj %p (mt: %p) start: %p, total: %zd",
                        heap_number,
                        (size_t)(mark_stack_limit - background_mark_stack_tos),
                        oo,
                        method_table(oo),
                        start,
                        num_pointers));

                    bgc_overflow_count++;
                    overflow_p = TRUE;
                }
                if (overflow_p == FALSE)
                {
                    dprintf(3,("pushing mark for %zx ", (size_t)oo));

                    //push the object and its current
                    uint8_t** place = background_mark_stack_tos++;
                    *(place) = start;
                    *(background_mark_stack_tos++) = (uint8_t*)((size_t)oo | 1);

                    int num_pushed_refs = num_partial_refs;
                    int num_processed_refs = num_pushed_refs * 16;

                    go_through_object (method_table(oo), oo, s, ppslot,
                                       start, use_start, (oo + s),
                    {
                        uint8_t* o = *ppslot;
                        Prefetch(o);

                        if (background_mark (o,
                                            background_saved_lowest_address,
                                            background_saved_highest_address))
                        {
                            //m_boundary (o);
                            size_t obj_size = size (o);
                            bpromoted_bytes (thread) += obj_size;
                            if (contain_pointers_or_collectible (o))
                            {
                                *(background_mark_stack_tos++) = o;
                                if (--num_pushed_refs == 0)
                                {
                                    //update the start
                                    *place = (uint8_t*)(ppslot+1);
                                    goto more_to_do;
                                }

                            }
                        }
                        if (--num_processed_refs == 0)
                        {
                            // give foreground GC a chance to run
                            *place = (uint8_t*)(ppslot + 1);
                            goto more_to_do;
                        }

                        }
                        );
                    //we are finished with this object
                    *place = 0;
                    *(place+1) = 0;

                more_to_do:;
                }
                else
                {
                    dprintf (3,("background mark stack overflow for object %zx ", (size_t)oo));
#ifdef USE_REGIONS
                    set_background_overflow_p (oo);
#else //USE_REGIONS
                    background_min_overflow_address = min (background_min_overflow_address, oo);
                    background_max_overflow_address = max (background_max_overflow_address, oo);
#endif //USE_REGIONS
                }
            }
        }

#ifdef COLLECTIBLE_CLASS
next_level:
#endif // COLLECTIBLE_CLASS
        allow_fgc();

        if (!(background_mark_stack_tos == background_mark_stack_array))
        {
            oo = *(--background_mark_stack_tos);
        }
        else
            break;
    }

    assert (background_mark_stack_tos == background_mark_stack_array);


}

//this version is different than the foreground GC because
//it can't keep pointers to the inside of an object
//while calling background_mark_simple1. The object could be moved
//by an intervening foreground gc.
//this method assumes that *po is in the [low. high[ range
void
gc_heap::background_mark_simple (uint8_t* o THREAD_NUMBER_DCL)
{
#ifdef MULTIPLE_HEAPS
#else  //MULTIPLE_HEAPS
    const int thread = 0;
#endif //MULTIPLE_HEAPS
    {
        dprintf (3, ("bmarking %p", o));

        if (background_mark1 (o))
        {
            //m_boundary (o);
            size_t s = size (o);
            bpromoted_bytes (thread) += s;

            if (contain_pointers_or_collectible (o))
            {
                background_mark_simple1 (o THREAD_NUMBER_ARG);
            }
        }
        allow_fgc();
    }
}

inline
uint8_t* gc_heap::background_mark_object (uint8_t* o THREAD_NUMBER_DCL)
{
    if ((o >= background_saved_lowest_address) && (o < background_saved_highest_address))
    {
        background_mark_simple (o THREAD_NUMBER_ARG);
    }
    else
    {
        if (o)
        {
            dprintf (3, ("or-%p", o));
        }
    }
    return o;
}

void gc_heap::background_promote (Object** ppObject, ScanContext* sc, uint32_t flags)
{
    UNREFERENCED_PARAMETER(sc);
    //in order to save space on the array, mark the object,
    //knowing that it will be visited later
    assert (settings.concurrent);

    THREAD_NUMBER_FROM_CONTEXT;
#ifndef MULTIPLE_HEAPS
    const int thread = 0;
#endif //!MULTIPLE_HEAPS

    uint8_t* o = (uint8_t*)*ppObject;

    if (!is_in_find_object_range (o))
    {
        return;
    }

#ifdef DEBUG_DestroyedHandleValue
    // we can race with destroy handle during concurrent scan
    if (o == (uint8_t*)DEBUG_DestroyedHandleValue)
        return;
#endif //DEBUG_DestroyedHandleValue

    HEAP_FROM_THREAD;

    gc_heap* hp = gc_heap::heap_of (o);

    if ((o < hp->background_saved_lowest_address) || (o >= hp->background_saved_highest_address))
    {
        return;
    }

    if (flags & GC_CALL_INTERIOR)
    {
        o = hp->find_object (o);
        if (o == 0)
            return;
    }

#ifdef FEATURE_CONSERVATIVE_GC
    // For conservative GC, a value on stack may point to middle of a free object.
    // In this case, we don't need to promote the pointer.
    if (GCConfig::GetConservativeGC() && ((CObjectHeader*)o)->IsFree())
    {
        return;
    }
#endif //FEATURE_CONSERVATIVE_GC

#ifdef _DEBUG
    ((CObjectHeader*)o)->Validate();
#endif //_DEBUG

    //needs to be called before the marking because it is possible for a foreground
    //gc to take place during the mark and move the object
    STRESS_LOG3(LF_GC|LF_GCROOTS, LL_INFO1000000, "    GCHeap::Promote: Promote GC Root *%p = %p MT = %pT", ppObject, o, o ? ((Object*) o)->GetGCSafeMethodTable() : NULL);

    hpt->background_mark_simple (o THREAD_NUMBER_ARG);
}

//used by the ephemeral collection to scan the local background structures
//containing references.
void
gc_heap::scan_background_roots (promote_func* fn, int hn, ScanContext *pSC)
{
    ScanContext sc;
    if (pSC == 0)
        pSC = &sc;

    pSC->thread_number = hn;
    pSC->thread_count = n_heaps;

    BOOL relocate_p = (fn == &GCHeap::Relocate);

    dprintf (3, ("Scanning background mark list"));

    //scan mark_list
    size_t mark_list_finger = 0;
    while (mark_list_finger < c_mark_list_index)
    {
        uint8_t** o = &c_mark_list [mark_list_finger];
        if (!relocate_p)
        {
            // We may not be able to calculate the size during relocate as POPO
            // may have written over the object.
            size_t s = size (*o);
            assert (Align (s) >= Align (min_obj_size));
            dprintf(3,("background root %zx", (size_t)*o));
        }
        (*fn) ((Object**)o, pSC, 0);
        mark_list_finger++;
    }

    //scan the mark stack
    dprintf (3, ("Scanning background mark stack"));

    uint8_t** finger = background_mark_stack_array;
    while (finger < background_mark_stack_tos)
    {
        if ((finger + 1) < background_mark_stack_tos)
        {
            // We need to check for the partial mark case here.
            uint8_t* parent_obj = *(finger + 1);
            if ((size_t)parent_obj & 1)
            {
                uint8_t* place = *finger;
                size_t place_offset = 0;
                uint8_t* real_parent_obj = (uint8_t*)((size_t)parent_obj & ~1);

                if (relocate_p)
                {
                    *(finger + 1) = real_parent_obj;
                    place_offset = place - real_parent_obj;
                    dprintf(3,("relocating background root %zx", (size_t)real_parent_obj));
                    (*fn) ((Object**)(finger + 1), pSC, 0);
                    real_parent_obj = *(finger + 1);
                    *finger = real_parent_obj + place_offset;
                    *(finger + 1) = (uint8_t*)((size_t)real_parent_obj | 1);
                    dprintf(3,("roots changed to %p, %p", *finger, *(finger + 1)));
                }
                else
                {
                    uint8_t** temp = &real_parent_obj;
                    dprintf(3,("marking background root %zx", (size_t)real_parent_obj));
                    (*fn) ((Object**)temp, pSC, 0);
                }

                finger += 2;
                continue;
            }
        }
        dprintf(3,("background root %zx", (size_t)*finger));
        (*fn) ((Object**)finger, pSC, 0);
        finger++;
    }
}

void gc_heap::grow_bgc_mark_stack (size_t new_size)
{
    if ((background_mark_stack_array_length < new_size) &&
        ((new_size - background_mark_stack_array_length) > (background_mark_stack_array_length / 2)))
    {
        dprintf (2, ("h%d: ov grow to %zd", heap_number, new_size));

        uint8_t** tmp = new (nothrow) uint8_t* [new_size];
        if (tmp)
        {
            delete [] background_mark_stack_array;
            background_mark_stack_array = tmp;
            background_mark_stack_array_length = new_size;
            background_mark_stack_tos = background_mark_stack_array;
        }
    }
}

void gc_heap::check_bgc_mark_stack_length()
{
    if ((settings.condemned_generation < (max_generation - 1)) || gc_heap::background_running_p())
        return;

    size_t total_heap_size = get_total_heap_size();

    if (total_heap_size < ((size_t)4*1024*1024*1024))
        return;

#ifdef MULTIPLE_HEAPS
    int total_heaps = n_heaps;
#else
    int total_heaps = 1;
#endif //MULTIPLE_HEAPS
    size_t size_based_on_heap = total_heap_size / (size_t)(100 * 100 * total_heaps * sizeof (uint8_t*));

    size_t new_size = max (background_mark_stack_array_length, size_based_on_heap);

    grow_bgc_mark_stack (new_size);
}

uint8_t* gc_heap::background_seg_end (heap_segment* seg, BOOL concurrent_p)
{
#ifndef USE_REGIONS
    if (concurrent_p && (seg == saved_overflow_ephemeral_seg))
    {
        // for now we stop at where gen1 started when we started processing
        return background_min_soh_overflow_address;
    }
    else
#endif //!USE_REGIONS
    {
        return heap_segment_allocated (seg);
    }
}

uint8_t* gc_heap::background_first_overflow (uint8_t* min_add,
                                          heap_segment* seg,
                                          BOOL concurrent_p,
                                          BOOL small_object_p)
{
#ifdef USE_REGIONS
        return heap_segment_mem (seg);
#else
    uint8_t* o = 0;

    if (small_object_p)
    {
        if (in_range_for_segment (min_add, seg))
        {
            // min_add was the beginning of gen1 when we did the concurrent
            // overflow. Now we could be in a situation where min_add is
            // actually the same as allocated for that segment (because
            // we expanded heap), in which case we can not call
            // find first on this address or we will AV.
            if (min_add >= heap_segment_allocated (seg))
            {
                return min_add;
            }
            else
            {
                if (concurrent_p &&
                    ((seg == saved_overflow_ephemeral_seg) && (min_add >= background_min_soh_overflow_address)))
                {
                    return background_min_soh_overflow_address;
                }
                else
                {
                    o = find_first_object (min_add, heap_segment_mem (seg));
                    return o;
                }
            }
        }
    }

    o = max (heap_segment_mem (seg), min_add);
    return o;
#endif //USE_REGIONS
}

void gc_heap::background_process_mark_overflow_internal (uint8_t* min_add, uint8_t* max_add,
                                                         BOOL concurrent_p)
{
    if (concurrent_p)
    {
        current_bgc_state = bgc_overflow_soh;
    }

    size_t total_marked_objects = 0;

#ifdef MULTIPLE_HEAPS
    int thread = heap_number;
#endif //MULTIPLE_HEAPS

    int start_gen_idx = get_start_generation_index();
#ifdef USE_REGIONS
    if (concurrent_p)
        start_gen_idx = max_generation;
#endif //USE_REGIONS

    exclusive_sync* loh_alloc_lock = 0;

#ifndef USE_REGIONS
    dprintf (2,("Processing Mark overflow [%zx %zx]", (size_t)min_add, (size_t)max_add));
#endif
#ifdef MULTIPLE_HEAPS
    // We don't have each heap scan all heaps concurrently because we are worried about
    // multiple threads calling things like find_first_object.
    int h_start = (concurrent_p ? heap_number : 0);
    int h_end = (concurrent_p ? (heap_number + 1) : n_heaps);
    for (int hi = h_start; hi < h_end; hi++)
    {
        gc_heap*  hp = (concurrent_p ? this : g_heaps [(heap_number + hi) % n_heaps]);

#else
    {
        gc_heap*  hp = 0;

#endif //MULTIPLE_HEAPS
        BOOL small_object_segments = TRUE;
        loh_alloc_lock = hp->bgc_alloc_lock;

        for (int i = start_gen_idx; i < total_generation_count; i++)
        {
            int align_const = get_alignment_constant (small_object_segments);
            generation* gen = hp->generation_of (i);
            heap_segment* seg = heap_segment_in_range (generation_start_segment (gen));
            _ASSERTE(seg != NULL);

            uint8_t* current_min_add = min_add;
            uint8_t* current_max_add = max_add;

            while (seg)
            {
#ifdef USE_REGIONS
                if (heap_segment_overflow_p (seg))
                {
                    seg->flags &= ~heap_segment_flags_overflow;
                    current_min_add = heap_segment_mem (seg);
                    current_max_add = heap_segment_allocated (seg);
                    dprintf (2,("Processing Mark overflow [%zx %zx]", (size_t)current_min_add, (size_t)current_max_add));
                }
                else
                {
                    current_min_add = current_max_add = 0;
                }
#endif //USE_REGIONS
                uint8_t* o = hp->background_first_overflow (current_min_add, seg, concurrent_p, small_object_segments);

                while ((o < hp->background_seg_end (seg, concurrent_p)) && (o <= current_max_add))
                {
                    dprintf (3, ("considering %zx", (size_t)o));

                    size_t s;

                    if (concurrent_p && !small_object_segments)
                    {
                        loh_alloc_lock->bgc_mark_set (o);

                        if (((CObjectHeader*)o)->IsFree())
                        {
                            s = unused_array_size (o);
                        }
                        else
                        {
                            s = size (o);
                        }
                    }
                    else
                    {
                        s = size (o);
                    }

                    if (background_object_marked (o, FALSE) && contain_pointers_or_collectible (o))
                    {
                        total_marked_objects++;
                        go_through_object_cl (method_table(o), o, s, poo,
                                              uint8_t* oo = *poo;
                                              background_mark_object (oo THREAD_NUMBER_ARG);
                                             );
                    }

                    if (concurrent_p && !small_object_segments)
                    {
                        loh_alloc_lock->bgc_mark_done ();
                    }

                    o = o + Align (s, align_const);

                    if (concurrent_p)
                    {
                        allow_fgc();
                    }
                }

#ifdef USE_REGIONS
                if (current_max_add != 0)
#endif //USE_REGIONS
                {
                    dprintf (2, ("went through overflow objects in segment %p (%d) (so far %zd marked)",
                        heap_segment_mem (seg), (small_object_segments ? 0 : 1), total_marked_objects));
                }
#ifndef USE_REGIONS
                if (concurrent_p && (seg == hp->saved_overflow_ephemeral_seg))
                {
                    break;
                }
#endif //!USE_REGIONS
                seg = heap_segment_next_in_range (seg);
            }

            if (concurrent_p)
            {
                current_bgc_state = bgc_overflow_uoh;
            }

            dprintf (2, ("h%d: SOH: ov-mo: %zd", heap_number, total_marked_objects));
            fire_overflow_event (min_add, max_add, total_marked_objects, i);
            if (i >= soh_gen2)
            {
                concurrent_print_time_delta (concurrent_p ? "Cov SOH" : "Nov SOH");
                small_object_segments = FALSE;
            }

            total_marked_objects = 0;
        }
    }
}

BOOL gc_heap::background_process_mark_overflow (BOOL concurrent_p)
{
    BOOL grow_mark_array_p = TRUE;

    if (concurrent_p)
    {
        assert (!processed_eph_overflow_p);
#ifndef USE_REGIONS
        if ((background_max_overflow_address != 0) &&
            (background_min_overflow_address != MAX_PTR))
        {
            // We have overflow to process but we know we can't process the ephemeral generations
            // now (we actually could process till the current gen1 start but since we are going to
            // make overflow per segment, for now I'll just stop at the saved gen1 start.
            saved_overflow_ephemeral_seg = ephemeral_heap_segment;
            background_max_soh_overflow_address = heap_segment_reserved (saved_overflow_ephemeral_seg);
            background_min_soh_overflow_address = generation_allocation_start (generation_of (max_generation - 1));
        }
#endif //!USE_REGIONS
    }
    else
    {
#ifndef USE_REGIONS
        assert ((saved_overflow_ephemeral_seg == 0) ||
                ((background_max_soh_overflow_address != 0) &&
                 (background_min_soh_overflow_address != MAX_PTR)));
#endif //!USE_REGIONS

        if (!processed_eph_overflow_p)
        {
            // if there was no more overflow we just need to process what we didn't process
            // on the saved ephemeral segment.
#ifdef USE_REGIONS
            if (!background_overflow_p)
#else
            if ((background_max_overflow_address == 0) && (background_min_overflow_address == MAX_PTR))
#endif //USE_REGIONS
            {
                dprintf (2, ("final processing mark overflow - no more overflow since last time"));
                grow_mark_array_p = FALSE;
            }
#ifdef USE_REGIONS
            background_overflow_p = TRUE;
#else
            background_min_overflow_address = min (background_min_overflow_address,
                                                background_min_soh_overflow_address);
            background_max_overflow_address = max (background_max_overflow_address,
                                                background_max_soh_overflow_address);
#endif //!USE_REGIONS
            processed_eph_overflow_p = TRUE;
        }
    }

    BOOL  overflow_p = FALSE;
recheck:
#ifdef USE_REGIONS
    if (background_overflow_p)
#else
    if ((! ((background_max_overflow_address == 0)) ||
         ! ((background_min_overflow_address == MAX_PTR))))
#endif
    {
        overflow_p = TRUE;

        if (grow_mark_array_p)
        {
            // Try to grow the array.
            size_t new_size = max ((size_t)MARK_STACK_INITIAL_LENGTH, 2*background_mark_stack_array_length);

            if ((new_size * sizeof(mark)) > 100*1024)
            {
                size_t new_max_size = (get_total_heap_size() / 10) / sizeof(mark);

                new_size = min(new_max_size, new_size);
            }

            grow_bgc_mark_stack (new_size);
        }
        else
        {
            grow_mark_array_p = TRUE;
        }

#ifdef USE_REGIONS
        uint8_t*  min_add = 0;
        uint8_t*  max_add = 0;
        background_overflow_p = FALSE;
#else
        uint8_t*  min_add = background_min_overflow_address;
        uint8_t*  max_add = background_max_overflow_address;

        background_max_overflow_address = 0;
        background_min_overflow_address = MAX_PTR;
#endif

        background_process_mark_overflow_internal (min_add, max_add, concurrent_p);
        if (!concurrent_p)
        {
            goto recheck;
        }
    }

    return overflow_p;
}

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4702) // C4702: unreachable code: gc_thread_function may not return
#endif //_MSC_VER
void gc_heap::bgc_thread_stub (void* arg)
{
    gc_heap* heap = (gc_heap*)arg;

#ifdef STRESS_DYNAMIC_HEAP_COUNT
    // We should only do this every so often; otherwise we'll never be able to do a BGC
    int r = (int)gc_rand::get_rand (30);
    bool wait_p = (r < 10);

    if (wait_p)
    {
        GCToOSInterface::Sleep (100);
    }
    dprintf (6666, ("h%d %s", heap->heap_number, (wait_p ? "waited" : "did not wait")));
#endif

    heap->bgc_thread = GCToEEInterface::GetThread();
    assert(heap->bgc_thread != nullptr);
    heap->bgc_thread_function();
}
#ifdef _MSC_VER
#pragma warning(pop)
#endif //_MSC_VER

void gc_heap::background_drain_mark_list (int thread)
{
#ifndef MULTIPLE_HEAPS
    UNREFERENCED_PARAMETER(thread);
#endif //!MULTIPLE_HEAPS

    size_t saved_c_mark_list_index = c_mark_list_index;

    if (saved_c_mark_list_index)
    {
        concurrent_print_time_delta ("SML");
    }
    while (c_mark_list_index != 0)
    {
        size_t current_index = c_mark_list_index - 1;
        uint8_t* o = c_mark_list [current_index];
        background_mark_object (o THREAD_NUMBER_ARG);
        c_mark_list_index--;
    }
    if (saved_c_mark_list_index)
    {
        concurrent_print_time_delta ("EML");
    }

    fire_drain_mark_list_event (saved_c_mark_list_index);
}

// The background GC version of scan_dependent_handles (see that method for a more in-depth comment).
#ifdef MULTIPLE_HEAPS
// Since we only scan dependent handles while we are stopped we'll never interfere with FGCs scanning
// them. So we can use the same static variables.
void gc_heap::background_scan_dependent_handles (ScanContext *sc)
{
    // Whenever we call this method there may have been preceding object promotions. So set
    // s_fUnscannedPromotions unconditionally (during further iterations of the scanning loop this will be set
    // based on the how the scanning proceeded).
    s_fUnscannedPromotions = TRUE;

    // We don't know how many times we need to loop yet. In particular we can't base the loop condition on
    // the state of this thread's portion of the dependent handle table. That's because promotions on other
    // threads could cause handle promotions to become necessary here. Even if there are definitely no more
    // promotions possible in this thread's handles, we still have to stay in lock-step with those worker
    // threads that haven't finished yet (each GC worker thread has to join exactly the same number of times
    // as all the others or they'll get out of step).
    while (true)
    {
        // The various worker threads are all currently racing in this code. We need to work out if at least
        // one of them think they have work to do this cycle. Each thread needs to rescan its portion of the
        // dependent handle table when both of the following conditions apply:
        //  1) At least one (arbitrary) object might have been promoted since the last scan (because if this
        //     object happens to correspond to a primary in one of our handles we might potentially have to
        //     promote the associated secondary).
        //  2) The table for this thread has at least one handle with a secondary that isn't promoted yet.
        //
        // The first condition is represented by s_fUnscannedPromotions. This is always non-zero for the first
        // iteration of this loop (see comment above) and in subsequent cycles each thread updates this
        // whenever a mark stack overflow occurs or scanning their dependent handles results in a secondary
        // being promoted. This value is cleared back to zero in a synchronized fashion in the join that
        // follows below. Note that we can't read this outside of the join since on any iteration apart from
        // the first threads will be racing between reading this value and completing their previous
        // iteration's table scan.
        //
        // The second condition is tracked by the dependent handle code itself on a per worker thread basis
        // (and updated by the GcDhReScan() method). We call GcDhUnpromotedHandlesExist() on each thread to
        // determine the local value and collect the results into the s_fUnpromotedHandles variable in what is
        // effectively an OR operation. As per s_fUnscannedPromotions we can't read the final result until
        // we're safely joined.
        if (GCScan::GcDhUnpromotedHandlesExist(sc))
            s_fUnpromotedHandles = TRUE;

        // Synchronize all the threads so we can read our state variables safely. The following shared
        // variable (indicating whether we should scan the tables or terminate the loop) will be set by a
        // single thread inside the join.
        bgc_t_join.join(this, gc_join_scan_dependent_handles);
        if (bgc_t_join.joined())
        {
            // We're synchronized so it's safe to read our shared state variables. We update another shared
            // variable to indicate to all threads whether we'll be scanning for another cycle or terminating
            // the loop. We scan if there has been at least one object promotion since last time and at least
            // one thread has a dependent handle table with a potential handle promotion possible.
            s_fScanRequired = s_fUnscannedPromotions && s_fUnpromotedHandles;

            // Reset our shared state variables (ready to be set again on this scan or with a good initial
            // value for the next call if we're terminating the loop).
            s_fUnscannedPromotions = FALSE;
            s_fUnpromotedHandles = FALSE;

            if (!s_fScanRequired)
            {
#ifdef USE_REGIONS
                BOOL all_heaps_background_overflow_p = FALSE;
#else //USE_REGIONS
                uint8_t* all_heaps_max = 0;
                uint8_t* all_heaps_min = MAX_PTR;
#endif //USE_REGIONS
                int i;
                for (i = 0; i < n_heaps; i++)
                {
#ifdef USE_REGIONS
                    // in the regions case, compute the OR of all the per-heap flags
                    if (g_heaps[i]->background_overflow_p)
                        all_heaps_background_overflow_p = TRUE;
#else //USE_REGIONS
                    if (all_heaps_max < g_heaps[i]->background_max_overflow_address)
                        all_heaps_max = g_heaps[i]->background_max_overflow_address;
                    if (all_heaps_min > g_heaps[i]->background_min_overflow_address)
                        all_heaps_min = g_heaps[i]->background_min_overflow_address;
#endif //USE_REGIONS
                }
                for (i = 0; i < n_heaps; i++)
                {
#ifdef USE_REGIONS
                    g_heaps[i]->background_overflow_p = all_heaps_background_overflow_p;
#else //USE_REGIONS
                    g_heaps[i]->background_max_overflow_address = all_heaps_max;
                    g_heaps[i]->background_min_overflow_address = all_heaps_min;
#endif //USE_REGIONS
                }
            }

            dprintf(2, ("Starting all gc thread mark stack overflow processing"));
            bgc_t_join.restart();
        }

        // Handle any mark stack overflow: scanning dependent handles relies on all previous object promotions
        // being visible. If there really was an overflow (process_mark_overflow returns true) then set the
        // global flag indicating that at least one object promotion may have occurred (the usual comment
        // about races applies). (Note it's OK to set this flag even if we're about to terminate the loop and
        // exit the method since we unconditionally set this variable on method entry anyway).
        if (background_process_mark_overflow (sc->concurrent))
            s_fUnscannedPromotions = TRUE;

        // If we decided that no scan was required we can terminate the loop now.
        if (!s_fScanRequired)
            break;

        // Otherwise we must join with the other workers to ensure that all mark stack overflows have been
        // processed before we start scanning dependent handle tables (if overflows remain while we scan we
        // could miss noting the promotion of some primary objects).
        bgc_t_join.join(this, gc_join_rescan_dependent_handles);
        if (bgc_t_join.joined())
        {
            dprintf(3, ("Starting all gc thread for dependent handle promotion"));
            bgc_t_join.restart();
        }

        // If the portion of the dependent handle table managed by this worker has handles that could still be
        // promoted perform a rescan. If the rescan resulted in at least one promotion note this fact since it
        // could require a rescan of handles on this or other workers.
        if (GCScan::GcDhUnpromotedHandlesExist(sc))
            if (GCScan::GcDhReScan(sc))
                s_fUnscannedPromotions = TRUE;
    }
}

#else //MULTIPLE_HEAPS
void gc_heap::background_scan_dependent_handles (ScanContext *sc)
{
    // Whenever we call this method there may have been preceding object promotions. So set
    // fUnscannedPromotions unconditionally (during further iterations of the scanning loop this will be set
    // based on the how the scanning proceeded).
    bool fUnscannedPromotions = true;

    // Scan dependent handles repeatedly until there are no further promotions that can be made or we made a
    // scan without performing any new promotions.
    while (GCScan::GcDhUnpromotedHandlesExist(sc) && fUnscannedPromotions)
    {
        // On each iteration of the loop start with the assumption that no further objects have been promoted.
        fUnscannedPromotions = false;

        // Handle any mark stack overflow: scanning dependent handles relies on all previous object promotions
        // being visible. If there was an overflow (background_process_mark_overflow returned true) then
        // additional objects now appear to be promoted and we should set the flag.
        if (background_process_mark_overflow (sc->concurrent))
            fUnscannedPromotions = true;

        // Perform the scan and set the flag if any promotions resulted.
        if (GCScan::GcDhReScan (sc))
            fUnscannedPromotions = true;
    }

    // Perform a last processing of any overflowed mark stack.
    background_process_mark_overflow (sc->concurrent);
}

#endif //MULTIPLE_HEAPS

void gc_heap::recover_bgc_settings()
{
    if ((settings.condemned_generation < max_generation) && gc_heap::background_running_p())
    {
        dprintf (2, ("restoring bgc settings"));
        settings = saved_bgc_settings;
        GCHeap::GcCondemnedGeneration = gc_heap::settings.condemned_generation;
    }
}

void gc_heap::allow_fgc()
{
    assert (bgc_thread == GCToEEInterface::GetThread());
    bool bToggleGC = false;

    if (g_fSuspensionPending > 0)
    {
        bToggleGC = GCToEEInterface::EnablePreemptiveGC();
        if (bToggleGC)
        {
            GCToEEInterface::DisablePreemptiveGC();
        }
    }
}

BOOL gc_heap::is_bgc_in_progress()
{
#ifdef MULTIPLE_HEAPS
    // All heaps are changed to/from the bgc_initialized state during the VM suspension at the start of BGC,
    // so checking any heap will work.
    gc_heap* hp = g_heaps[0];
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    return (background_running_p() || (hp->current_bgc_state == bgc_initialized));
}

void gc_heap::clear_commit_flag()
{
    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        heap_segment* seg = heap_segment_in_range (generation_start_segment (gen));
        while (seg)
        {
            if (seg->flags & heap_segment_flags_ma_committed)
            {
                seg->flags &= ~heap_segment_flags_ma_committed;
            }

            if (seg->flags & heap_segment_flags_ma_pcommitted)
            {
                seg->flags &= ~heap_segment_flags_ma_pcommitted;
            }

            seg = heap_segment_next (seg);
        }
    }
}

void gc_heap::clear_commit_flag_global()
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        g_heaps[i]->clear_commit_flag();
    }
#else
    clear_commit_flag();
#endif //MULTIPLE_HEAPS
}

uint8_t* gc_heap::get_start_address (heap_segment* seg)
{
    uint8_t* start =
#ifdef USE_REGIONS
        heap_segment_mem (seg);
#else
        (heap_segment_read_only_p(seg) ? heap_segment_mem (seg) : (uint8_t*)seg);
#endif //USE_REGIONS
    return start;
}

BOOL gc_heap::commit_mark_array_new_seg (gc_heap* hp,
                                         heap_segment* seg,
                                         uint32_t* new_card_table,
                                         uint8_t* new_lowest_address)
{
    uint8_t* start = get_start_address (seg);
    uint8_t* end = heap_segment_reserved (seg);

    uint8_t* lowest = hp->background_saved_lowest_address;
    uint8_t* highest = hp->background_saved_highest_address;

    uint8_t* commit_start = NULL;
    uint8_t* commit_end = NULL;
    size_t commit_flag = 0;

    if ((highest >= start) &&
        (lowest <= end))
    {
        if ((start >= lowest) && (end <= highest))
        {
            dprintf (GC_TABLE_LOG, ("completely in bgc range: seg %p-%p, bgc: %p-%p",
                                    start, end, lowest, highest));
            commit_flag = heap_segment_flags_ma_committed;
        }
        else
        {
            dprintf (GC_TABLE_LOG, ("partially in bgc range: seg %p-%p, bgc: %p-%p",
                                    start, end, lowest, highest));
            commit_flag = heap_segment_flags_ma_pcommitted;
#ifdef USE_REGIONS
            assert (!"Region should not have its mark array partially committed.");
#endif
        }

        commit_start = max (lowest, start);
        commit_end = min (highest, end);

        if (!commit_mark_array_by_range (commit_start, commit_end, hp->mark_array))
        {
            return FALSE;
        }

        if (new_card_table == 0)
        {
            new_card_table = g_gc_card_table;
        }

        if (hp->card_table != new_card_table)
        {
            if (new_lowest_address == 0)
            {
                new_lowest_address = g_gc_lowest_address;
            }

            uint32_t* ct = &new_card_table[card_word (gcard_of (new_lowest_address))];
            uint32_t* ma = (uint32_t*)((uint8_t*)card_table_mark_array (ct) - size_mark_array_of (0, new_lowest_address));

            dprintf (GC_TABLE_LOG, ("table realloc-ed: %p->%p, MA: %p->%p",
                                    hp->card_table, new_card_table,
                                    hp->mark_array, ma));

            if (!commit_mark_array_by_range (commit_start, commit_end, ma))
            {
                return FALSE;
            }
        }

        seg->flags |= commit_flag;
    }

    return TRUE;
}

BOOL gc_heap::commit_mark_array_by_range (uint8_t* begin, uint8_t* end, uint32_t* mark_array_addr)
{
    size_t beg_word = mark_word_of (begin);
    size_t end_word = mark_word_of (align_on_mark_word (end));
    uint8_t* commit_start = align_lower_page ((uint8_t*)&mark_array_addr[beg_word]);
    uint8_t* commit_end = align_on_page ((uint8_t*)&mark_array_addr[end_word]);
    size_t size = (size_t)(commit_end - commit_start);

#ifdef SIMPLE_DPRINTF
    dprintf (GC_TABLE_LOG, ("range: %p->%p mark word: %zx->%zx(%zd), mark array: %p->%p(%zd), commit %p->%p(%zd)",
                            begin, end,
                            beg_word, end_word,
                            (end_word - beg_word) * sizeof (uint32_t),
                            &mark_array_addr[beg_word],
                            &mark_array_addr[end_word],
                            (size_t)(&mark_array_addr[end_word] - &mark_array_addr[beg_word]),
                            commit_start, commit_end,
                            size));
#endif //SIMPLE_DPRINTF

    if (virtual_commit (commit_start, size, recorded_committed_mark_array_bucket))
    {
        // We can only verify the mark array is cleared from begin to end, the first and the last
        // page aren't necessarily all cleared 'cause they could be used by other segments or
        // card bundle.
        verify_mark_array_cleared (begin, end, mark_array_addr);
        return TRUE;
    }
    else
    {
        dprintf (GC_TABLE_LOG, ("failed to commit %zd bytes", (end_word - beg_word) * sizeof (uint32_t)));
        return FALSE;
    }
}

BOOL gc_heap::commit_mark_array_with_check (heap_segment* seg, uint32_t* new_mark_array_addr)
{
    uint8_t* start = get_start_address (seg);
    uint8_t* end = heap_segment_reserved (seg);

#ifdef MULTIPLE_HEAPS
    uint8_t* lowest = heap_segment_heap (seg)->background_saved_lowest_address;
    uint8_t* highest = heap_segment_heap (seg)->background_saved_highest_address;
#else
    uint8_t* lowest = background_saved_lowest_address;
    uint8_t* highest = background_saved_highest_address;
#endif //MULTIPLE_HEAPS

    if ((highest >= start) &&
        (lowest <= end))
    {
        start = max (lowest, start);
        end = min (highest, end);
        if (!commit_mark_array_by_range (start, end, new_mark_array_addr))
        {
            return FALSE;
        }
    }

    return TRUE;
}

BOOL gc_heap::commit_mark_array_by_seg (heap_segment* seg, uint32_t* mark_array_addr)
{
    dprintf (GC_TABLE_LOG, ("seg: %p->%p; MA: %p",
        seg,
        heap_segment_reserved (seg),
        mark_array_addr));
    uint8_t* start = get_start_address (seg);

    return commit_mark_array_by_range (start, heap_segment_reserved (seg), mark_array_addr);
}

BOOL gc_heap::commit_mark_array_bgc_init()
{
    dprintf (GC_TABLE_LOG, ("BGC init commit: lowest: %p, highest: %p, mark_array: %p",
                            lowest_address, highest_address, mark_array));

    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        heap_segment* seg = heap_segment_in_range (generation_start_segment (gen));
        while (seg)
        {
            dprintf (GC_TABLE_LOG, ("h%d gen%d seg: %p(%p-%p), flags: %zd",
                heap_number, i, seg, heap_segment_mem (seg), heap_segment_allocated (seg), seg->flags));

            if (!(seg->flags & heap_segment_flags_ma_committed))
            {
                // For ro segments they could always be only partially in range so we'd
                // be calling this at the beginning of every BGC. We are not making this
                // more efficient right now - ro segments are currently only used by NativeAOT.
                if (heap_segment_read_only_p (seg))
                {
                    if ((heap_segment_mem (seg) >= lowest_address) &&
                        (heap_segment_reserved (seg) <= highest_address))
                    {
                        if (commit_mark_array_by_seg (seg, mark_array))
                        {
                            seg->flags |= heap_segment_flags_ma_committed;
                        }
                        else
                        {
                            return FALSE;
                        }
                    }
                    else
                    {
                        uint8_t* start = max (lowest_address, heap_segment_mem (seg));
                        uint8_t* end = min (highest_address, heap_segment_reserved (seg));
                        if (commit_mark_array_by_range (start, end, mark_array))
                        {
                            seg->flags |= heap_segment_flags_ma_pcommitted;
                        }
                        else
                        {
                            return FALSE;
                        }
                    }
                }
                else
                {
                    // For normal segments they are by design completely in range so just
                    // commit the whole mark array for each seg.
                    if (commit_mark_array_by_seg (seg, mark_array))
                    {
                        if (seg->flags & heap_segment_flags_ma_pcommitted)
                        {
                            seg->flags &= ~heap_segment_flags_ma_pcommitted;
                        }
                        seg->flags |= heap_segment_flags_ma_committed;
                    }
                    else
                    {
                        return FALSE;
                    }
                }
            }

            seg = heap_segment_next (seg);
        }
    }

    return TRUE;
}

// This function doesn't check the commit flag since it's for a new array -
// the mark_array flag for these segments will remain the same.
BOOL gc_heap::commit_new_mark_array (uint32_t* new_mark_array_addr)
{
    dprintf (GC_TABLE_LOG, ("committing existing segs on MA %p", new_mark_array_addr));

    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        heap_segment* seg = heap_segment_in_range (generation_start_segment (gen));
        while (seg)
        {
            if (!commit_mark_array_with_check (seg, new_mark_array_addr))
            {
                return FALSE;
            }

            seg = heap_segment_next (seg);
        }
    }

#if defined(MULTIPLE_HEAPS) && !defined(USE_REGIONS)
    if (new_heap_segment)
    {
        if (!commit_mark_array_with_check (new_heap_segment, new_mark_array_addr))
        {
            return FALSE;
        }
    }
#endif //MULTIPLE_HEAPS && !USE_REGIONS

    return TRUE;
}

BOOL gc_heap::commit_new_mark_array_global (uint32_t* new_mark_array)
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        if (!g_heaps[i]->commit_new_mark_array (new_mark_array))
        {
            return FALSE;
        }
    }
#else
    if (!commit_new_mark_array (new_mark_array))
    {
        return FALSE;
    }
#endif //MULTIPLE_HEAPS

    return TRUE;
}

void gc_heap::decommit_mark_array_by_seg (heap_segment* seg)
{
    // if BGC is disabled (the finalize watchdog does this at shutdown), the mark array could have
    // been set to NULL.
    if (mark_array == NULL)
    {
        return;
    }

    dprintf (GC_TABLE_LOG, ("decommitting seg %p(%zx), MA: %p", seg, seg->flags, mark_array));

    size_t flags = seg->flags;

    if ((flags & heap_segment_flags_ma_committed) ||
        (flags & heap_segment_flags_ma_pcommitted))
    {
        uint8_t* start = get_start_address (seg);
        uint8_t* end = heap_segment_reserved (seg);

        if (flags & heap_segment_flags_ma_pcommitted)
        {
            start = max (lowest_address, start);
            end = min (highest_address, end);
        }

        size_t beg_word = mark_word_of (start);
        size_t end_word = mark_word_of (align_on_mark_word (end));
        uint8_t* decommit_start = align_on_page ((uint8_t*)&mark_array[beg_word]);
        uint8_t* decommit_end = align_lower_page ((uint8_t*)&mark_array[end_word]);
        size_t size = (size_t)(decommit_end - decommit_start);

#ifdef SIMPLE_DPRINTF
        dprintf (GC_TABLE_LOG, ("seg: %p mark word: %zx->%zx(%zd), mark array: %p->%p(%zd), decommit %p->%p(%zd)",
                                seg,
                                beg_word, end_word,
                                (end_word - beg_word) * sizeof (uint32_t),
                                &mark_array[beg_word],
                                &mark_array[end_word],
                                (size_t)(&mark_array[end_word] - &mark_array[beg_word]),
                                decommit_start, decommit_end,
                                size));
#endif //SIMPLE_DPRINTF

        if (decommit_start < decommit_end)
        {
            if (!virtual_decommit (decommit_start, size, recorded_committed_mark_array_bucket))
            {
                dprintf (GC_TABLE_LOG, ("decommit on %p for %zd bytes failed",
                                        decommit_start, size));
                assert (!"decommit failed");
            }
        }

        dprintf (GC_TABLE_LOG, ("decommitted [%zx for address [%p", beg_word, seg));
    }
}

bool gc_heap::should_update_end_mark_size()
{
    return ((settings.condemned_generation == (max_generation - 1)) && (current_c_gc_state == c_gc_state_planning));
}

void gc_heap::background_mark_phase ()
{
    verify_mark_array_cleared();

    ScanContext sc;
    sc.thread_number = heap_number;
    sc.thread_count = n_heaps;
    sc.promotion = TRUE;
    sc.concurrent = FALSE;

    THREAD_FROM_HEAP;
    BOOL cooperative_mode = TRUE;
#ifndef MULTIPLE_HEAPS
    const int thread = heap_number;
#endif //!MULTIPLE_HEAPS

    dprintf(2,("-(GC%zu)BMark-", VolatileLoad(&settings.gc_index)));

    assert (settings.concurrent);

    if (gen0_must_clear_bricks > 0)
        gen0_must_clear_bricks--;

    background_soh_alloc_count = 0;
    bgc_overflow_count = 0;

    bpromoted_bytes (heap_number) = 0;
    static uint32_t num_sizedrefs = 0;

#ifdef USE_REGIONS
    background_overflow_p = FALSE;
#else
    background_min_overflow_address = MAX_PTR;
    background_max_overflow_address = 0;
    background_min_soh_overflow_address = MAX_PTR;
    background_max_soh_overflow_address = 0;
#endif //USE_REGIONS
    processed_eph_overflow_p = FALSE;

    //set up the mark lists from g_mark_list
    assert (g_mark_list);
    mark_list = g_mark_list;
    //dont use the mark list for full gc
    //because multiple segments are more complex to handle and the list
    //is likely to overflow
    mark_list_end = &mark_list [0];
    mark_list_index = &mark_list [0];

    c_mark_list_index = 0;

#ifndef MULTIPLE_HEAPS
    shigh = (uint8_t*) 0;
    slow  = MAX_PTR;
#endif //MULTIPLE_HEAPS

    dprintf(3,("BGC: stack marking"));
    sc.concurrent = TRUE;

    GCScan::GcScanRoots(background_promote_callback,
                            max_generation, max_generation,
                            &sc);

    dprintf(3,("BGC: finalization marking"));
    finalize_queue->GcScanRoots(background_promote_callback, heap_number, 0);

    background_soh_size_end_mark = 0;

    for (int uoh_gen_idx = uoh_start_generation; uoh_gen_idx < total_generation_count; uoh_gen_idx++)
    {
        size_t uoh_size = generation_size (uoh_gen_idx);
        int uoh_idx = uoh_gen_idx - uoh_start_generation;
        bgc_begin_uoh_size[uoh_idx] = uoh_size;
        bgc_uoh_current_size[uoh_idx] = uoh_size;
    }

    dprintf (GTC_LOG, ("BM: h%d: soh: %zd, loh: %zd, poh: %zd",
        heap_number, generation_sizes (generation_of (max_generation)),
        bgc_uoh_current_size[loh_generation - uoh_start_generation], bgc_uoh_current_size[poh_generation - uoh_start_generation]));

    //concurrent_print_time_delta ("copying stack roots");
    concurrent_print_time_delta ("CS");

    FIRE_EVENT(BGC1stNonConEnd);

#ifndef USE_REGIONS
    saved_overflow_ephemeral_seg = 0;
#endif //!USE_REGIONS
    current_bgc_state = bgc_reset_ww;

    // we don't need a join here - just whichever thread that gets here
    // first can change the states and call restart_vm.
    // this is not true - we can't let the EE run when we are scanning stack.
    // since we now allow reset ww to run concurrently and have a join for it,
    // we can do restart ee on the 1st thread that got here. Make sure we handle the
    // sizedref handles correctly.
#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_restart_ee);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef USE_REGIONS
        // There's no need to distribute a second time if we just did an ephemeral GC, and we don't want to
        // age the free regions twice.
        if (!do_ephemeral_gc_p)
        {
            distribute_free_regions ();
            age_free_regions ("BGC");
        }
#endif //USE_REGIONS

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Resetting write watch for software write watch is pretty fast, much faster than for hardware write watch. Reset
        // can be done while the runtime is suspended or after the runtime is restarted, the preference was to reset while
        // the runtime is suspended. The reset for hardware write watch is done after the runtime is restarted below.
        concurrent_print_time_delta ("CRWW begin");

#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            g_heaps[i]->reset_write_watch (FALSE);
        }
#else
        reset_write_watch (FALSE);
#endif //MULTIPLE_HEAPS

        concurrent_print_time_delta ("CRWW");
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifdef FEATURE_SIZED_REF_HANDLES
        num_sizedrefs = GCToEEInterface::GetTotalNumSizedRefHandles();
#endif // FEATURE_SIZED_REF_HANDLES

        // this c_write is not really necessary because restart_vm
        // has an instruction that will flush the cpu cache (interlocked
        // or whatever) but we don't want to rely on that.
        dprintf (GTC_LOG, ("setting cm_in_progress"));
        c_write (cm_in_progress, TRUE);

        assert (dont_restart_ee_p);
        dont_restart_ee_p = FALSE;
        last_alloc_reset_suspended_end_time = GetHighPrecisionTimeStamp();

        restart_vm();
        GCToOSInterface::YieldThread (0);
#ifdef MULTIPLE_HEAPS
        dprintf(3, ("Starting all gc threads for gc"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_after_reset);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
        disable_preemptive (true);

#ifndef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // When software write watch is enabled, resetting write watch is done while the runtime is
        // suspended above. The post-reset call to revisit_written_pages is only necessary for concurrent
        // reset_write_watch, to discard dirtied pages during the concurrent reset.
#ifdef WRITE_WATCH
        concurrent_print_time_delta ("CRWW begin");

#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            g_heaps[i]->reset_write_watch (TRUE);
        }
#else
        reset_write_watch (TRUE);
#endif //MULTIPLE_HEAPS

        concurrent_print_time_delta ("CRWW");
#endif //WRITE_WATCH

#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            g_heaps[i]->revisit_written_pages (TRUE, TRUE);
        }
#else
        revisit_written_pages (TRUE, TRUE);
#endif //MULTIPLE_HEAPS

        concurrent_print_time_delta ("CRW");
#endif // !FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            g_heaps[i]->current_bgc_state = bgc_mark_handles;
        }
#else
        current_bgc_state = bgc_mark_handles;
#endif //MULTIPLE_HEAPS

        current_c_gc_state = c_gc_state_marking;

        enable_preemptive ();

#ifdef MULTIPLE_HEAPS
        dprintf(3, ("Joining BGC threads after resetting writewatch"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    disable_preemptive (true);

#ifdef FEATURE_SIZED_REF_HANDLES
    if (num_sizedrefs > 0)
    {
        GCScan::GcScanSizedRefs(background_promote, max_generation, max_generation, &sc);

        enable_preemptive ();

#ifdef MULTIPLE_HEAPS
        bgc_t_join.join(this, gc_join_scan_sizedref_done);
        if (bgc_t_join.joined())
        {
            dprintf(3, ("Done with marking all sized refs. Starting all bgc thread for marking other strong roots"));
            bgc_t_join.restart();
        }
#endif //MULTIPLE_HEAPS

        disable_preemptive (true);
    }
#endif // FEATURE_SIZED_REF_HANDLES

    dprintf (3,("BGC: handle table marking"));
    GCScan::GcScanHandles(background_promote,
                                max_generation, max_generation,
                                &sc);
    //concurrent_print_time_delta ("concurrent marking handle table");
    concurrent_print_time_delta ("CRH");

    current_bgc_state = bgc_mark_stack;
    dprintf (2,("concurrent draining mark list"));
    background_drain_mark_list (thread);
    //concurrent_print_time_delta ("concurrent marking stack roots");
    concurrent_print_time_delta ("CRS");

    dprintf (2,("concurrent revisiting dirtied pages"));

    // tuning has shown that there are advantages in doing this 2 times
    revisit_written_pages (TRUE);
    revisit_written_pages (TRUE);

    //concurrent_print_time_delta ("concurrent marking dirtied pages on LOH");
    concurrent_print_time_delta ("CRre");

    enable_preemptive ();

#if defined(MULTIPLE_HEAPS)
    bgc_t_join.join(this, gc_join_concurrent_overflow);
    if (bgc_t_join.joined())
    {
#ifdef USE_REGIONS
        BOOL all_heaps_background_overflow_p = FALSE;
#else //USE_REGIONS
        uint8_t* all_heaps_max = 0;
        uint8_t* all_heaps_min = MAX_PTR;
#endif //USE_REGIONS
        int i;
        for (i = 0; i < n_heaps; i++)
        {
#ifdef USE_REGIONS
            // in the regions case, compute the OR of all the per-heap flags
            if (g_heaps[i]->background_overflow_p)
                all_heaps_background_overflow_p = TRUE;
#else //USE_REGIONS
            dprintf (3, ("heap %d overflow max is %p, min is %p",
                i,
                g_heaps[i]->background_max_overflow_address,
                g_heaps[i]->background_min_overflow_address));
            if (all_heaps_max < g_heaps[i]->background_max_overflow_address)
                all_heaps_max = g_heaps[i]->background_max_overflow_address;
            if (all_heaps_min > g_heaps[i]->background_min_overflow_address)
                all_heaps_min = g_heaps[i]->background_min_overflow_address;
#endif //USE_REGIONS
        }
        for (i = 0; i < n_heaps; i++)
        {
#ifdef USE_REGIONS
            g_heaps[i]->background_overflow_p = all_heaps_background_overflow_p;
#else //USE_REGIONS
            g_heaps[i]->background_max_overflow_address = all_heaps_max;
            g_heaps[i]->background_min_overflow_address = all_heaps_min;
#endif //USE_REGIONS
        }
        dprintf(3, ("Starting all bgc threads after updating the overflow info"));
        bgc_t_join.restart();
    }
#endif //MULTIPLE_HEAPS

    disable_preemptive (true);

    dprintf (2, ("before CRov count: %zu", bgc_overflow_count));
    bgc_overflow_count = 0;
    background_process_mark_overflow (TRUE);
    dprintf (2, ("after CRov count: %zu", bgc_overflow_count));
    bgc_overflow_count = 0;
    //concurrent_print_time_delta ("concurrent processing mark overflow");
    concurrent_print_time_delta ("CRov");

    // Stop all threads, crawl all stacks and revisit changed pages.
    FIRE_EVENT(BGC1stConEnd);

    dprintf (2, ("Stopping the EE"));

    enable_preemptive ();

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_suspend_ee);
    if (bgc_t_join.joined())
    {
        bgc_threads_sync_event.Reset();

        dprintf(3, ("Joining BGC threads for non concurrent final marking"));
        bgc_t_join.restart();
    }
#endif //MULTIPLE_HEAPS

    if (heap_number == 0)
    {
        enter_spin_lock (&gc_lock);

        suspended_start_time = GetHighPrecisionTimeStamp();
        bgc_suspend_EE ();
        //suspend_EE ();
        bgc_threads_sync_event.Set();
    }
    else
    {
        bgc_threads_sync_event.Wait(INFINITE, FALSE);
        dprintf (2, ("bgc_threads_sync_event is signalled"));
    }

    assert (settings.concurrent);
    assert (settings.condemned_generation == max_generation);

    dprintf (2, ("clearing cm_in_progress"));
    c_write (cm_in_progress, FALSE);

    bgc_alloc_lock->check();

    current_bgc_state = bgc_final_marking;

    //concurrent_print_time_delta ("concurrent marking ended");
    concurrent_print_time_delta ("CR");

    FIRE_EVENT(BGC2ndNonConBegin);

    mark_absorb_new_alloc();

#ifdef FEATURE_EVENT_TRACE
    static uint64_t current_mark_time = 0;
    static uint64_t last_mark_time = 0;
#endif //FEATURE_EVENT_TRACE

    // We need a join here 'cause find_object would complain if the gen0
    // bricks of another heap haven't been fixed up. So we need to make sure
    // that every heap's gen0 bricks are fixed up before we proceed.
#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_after_absorb);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef BGC_SERVO_TUNING
        bgc_tuning::record_bgc_sweep_start();
#endif //BGC_SERVO_TUNING

        GCToEEInterface::BeforeGcScanRoots(max_generation, /* is_bgc */ true, /* is_concurrent */ false);

#ifdef FEATURE_EVENT_TRACE
        informational_event_enabled_p = EVENT_ENABLED (GCMarkWithType);
        if (informational_event_enabled_p)
            last_mark_time = GetHighPrecisionTimeStamp();
#endif //FEATURE_EVENT_TRACE

#ifdef MULTIPLE_HEAPS
        dprintf(3, ("Joining BGC threads after absorb"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    //reset the flag, indicating that the EE no longer expect concurrent
    //marking
    sc.concurrent = FALSE;

    dprintf (GTC_LOG, ("FM: h%d: soh: %zd, loh: %zd, poh: %zd", heap_number,
        generation_sizes (generation_of (max_generation)),
        bgc_uoh_current_size[loh_generation - uoh_start_generation],
        bgc_uoh_current_size[poh_generation - uoh_start_generation]));

#if defined(FEATURE_BASICFREEZE) && !defined(USE_REGIONS)
    if (ro_segments_in_range)
    {
        dprintf (2, ("nonconcurrent marking in range ro segments"));
        mark_ro_segments();
        //concurrent_print_time_delta ("nonconcurrent marking in range ro segments");
        concurrent_print_time_delta ("NRRO");
    }
#endif //FEATURE_BASICFREEZE && !USE_REGIONS

    dprintf (2, ("nonconcurrent marking stack roots"));
    GCScan::GcScanRoots(background_promote,
                            max_generation, max_generation,
                            &sc);
    //concurrent_print_time_delta ("nonconcurrent marking stack roots");
    concurrent_print_time_delta ("NRS");

    finalize_queue->GcScanRoots(background_promote, heap_number, 0);

    dprintf (2, ("nonconcurrent marking handle table"));
    GCScan::GcScanHandles(background_promote,
                                max_generation, max_generation,
                                &sc);
    //concurrent_print_time_delta ("nonconcurrent marking handle table");
    concurrent_print_time_delta ("NRH");

    dprintf (2,("---- (GC%zu)final going through written pages ----", VolatileLoad(&settings.gc_index)));
    revisit_written_pages (FALSE);
    //concurrent_print_time_delta ("nonconcurrent revisit dirtied pages on LOH");
    concurrent_print_time_delta ("NRre LOH");

    dprintf (2, ("before NR 1st Hov count: %zu", bgc_overflow_count));
    bgc_overflow_count = 0;

    // Dependent handles need to be scanned with a special algorithm (see the header comment on
    // scan_dependent_handles for more detail). We perform an initial scan without processing any mark
    // stack overflow. This is not guaranteed to complete the operation but in a common case (where there
    // are no dependent handles that are due to be collected) it allows us to optimize away further scans.
    // The call to background_scan_dependent_handles is what will cycle through more iterations if
    // required and will also perform processing of any mark stack overflow once the dependent handle
    // table has been fully promoted.
    dprintf (2, ("1st dependent handle scan and process mark overflow"));
    GCScan::GcDhInitialScan(background_promote, max_generation, max_generation, &sc);
    background_scan_dependent_handles (&sc);
    //concurrent_print_time_delta ("1st nonconcurrent dependent handle scan and process mark overflow");
    concurrent_print_time_delta ("NR 1st Hov");

    dprintf (2, ("after NR 1st Hov count: %zu", bgc_overflow_count));
    bgc_overflow_count = 0;

#ifdef FEATURE_JAVAMARSHAL

    // FIXME Any reason this code should be different for BGC ? Otherwise extract it to some common method ?

#ifdef MULTIPLE_HEAPS
    dprintf(3, ("Joining for short weak handle scan"));
    gc_t_join.join(this, gc_join_bridge_processing);
    if (gc_t_join.joined())
    {
#endif //MULTIPLE_HEAPS
        global_bridge_list = GCScan::GcProcessBridgeObjects (max_generation, max_generation, &sc, &num_global_bridge_objs);

#ifdef MULTIPLE_HEAPS
        dprintf (3, ("Starting all gc thread after bridge processing"));
        gc_t_join.restart();
    }
#endif //MULTIPLE_HEAPS

    {
        int thread = heap_number;
        // Each thread will receive an equal chunk of bridge objects, with the last thread
        // handling a few more objects from the remainder.
        size_t count_per_heap = num_global_bridge_objs / n_heaps;
        size_t start_index = thread * count_per_heap;
        size_t end_index = (thread == n_heaps - 1) ? num_global_bridge_objs : (thread + 1) * count_per_heap;

        for (size_t obj_idx = start_index; obj_idx < end_index; obj_idx++)
        {
            background_mark_simple (global_bridge_list[obj_idx] THREAD_NUMBER_ARG);
        }

        drain_mark_queue();
    }
#endif //FEATURE_JAVAMARSHAL

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_null_dead_short_weak);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef FEATURE_EVENT_TRACE
        bgc_time_info[time_mark_sizedref] = 0;
        record_mark_time (bgc_time_info[time_mark_roots], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // The runtime is suspended, take this opportunity to pause tracking written pages to
        // avoid further perf penalty after the runtime is restarted
        SoftwareWriteWatch::DisableForGCHeap();
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        GCToEEInterface::AfterGcScanRoots (max_generation, max_generation, &sc);

#ifdef MULTIPLE_HEAPS
        dprintf(3, ("Joining BGC threads for short weak handle scan"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    // null out the target of short weakref that were not promoted.
    GCScan::GcShortWeakPtrScan(max_generation, max_generation, &sc);

    //concurrent_print_time_delta ("bgc GcShortWeakPtrScan");
    concurrent_print_time_delta ("NR GcShortWeakPtrScan");

    {
#ifdef MULTIPLE_HEAPS
        bgc_t_join.join(this, gc_join_scan_finalization);
        if (bgc_t_join.joined())
        {
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_EVENT_TRACE
            record_mark_time (bgc_time_info[time_mark_short_weak], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

#ifdef MULTIPLE_HEAPS
            dprintf(3, ("Joining BGC threads for finalization"));
            bgc_t_join.restart();
        }
#endif //MULTIPLE_HEAPS

        dprintf(3,("Marking finalization data"));
        //concurrent_print_time_delta ("bgc joined to mark finalization");
        concurrent_print_time_delta ("NRj");
        finalize_queue->ScanForFinalization (background_promote, max_generation, __this);
        concurrent_print_time_delta ("NRF");
    }

    dprintf (2, ("before NR 2nd Hov count: %zu", bgc_overflow_count));
    bgc_overflow_count = 0;

    // Scan dependent handles again to promote any secondaries associated with primaries that were promoted
    // for finalization. As before background_scan_dependent_handles will also process any mark stack
    // overflow.
    dprintf (2, ("2nd dependent handle scan and process mark overflow"));
    background_scan_dependent_handles (&sc);
    //concurrent_print_time_delta ("2nd nonconcurrent dependent handle scan and process mark overflow");
    concurrent_print_time_delta ("NR 2nd Hov");

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_null_dead_long_weak);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {

#ifdef FEATURE_EVENT_TRACE
        record_mark_time (bgc_time_info[time_mark_scan_finalization], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

#ifdef MULTIPLE_HEAPS
        dprintf(2, ("Joining BGC threads for weak pointer deletion"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    // null out the target of long weakref that were not promoted.
    GCScan::GcWeakPtrScan (max_generation, max_generation, &sc);
    concurrent_print_time_delta ("NR GcWeakPtrScan");

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_null_dead_syncblk);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
        dprintf (2, ("calling GcWeakPtrScanBySingleThread"));
        // scan for deleted entries in the syncblk cache
        GCScan::GcWeakPtrScanBySingleThread (max_generation, max_generation, &sc);

#ifdef FEATURE_EVENT_TRACE
        record_mark_time (bgc_time_info[time_mark_long_weak], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

        concurrent_print_time_delta ("NR GcWeakPtrScanBySingleThread");
#ifdef MULTIPLE_HEAPS
        dprintf(2, ("Starting BGC threads for end of background mark phase"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    dprintf (2, ("end of bgc mark: loh: %zu, poh: %zu, soh: %zu",
                 generation_size (loh_generation),
                 generation_size (poh_generation),
                 generation_sizes (generation_of (max_generation))));

    for (int gen_idx = max_generation; gen_idx < total_generation_count; gen_idx++)
    {
        generation* gen = generation_of (gen_idx);
        dynamic_data* dd = dynamic_data_of (gen_idx);
        dd_begin_data_size (dd) = generation_size (gen_idx) -
                                  (generation_free_list_space (gen) + generation_free_obj_space (gen)) -
                                   get_generation_start_size (gen_idx);
        dd_survived_size (dd) = 0;
        dd_pinned_survived_size (dd) = 0;
        dd_artificial_pinned_survived_size (dd) = 0;
        dd_added_pinned_size (dd) = 0;
    }

    for (int i = get_start_generation_index(); i < uoh_start_generation; i++)
    {
        heap_segment* seg = heap_segment_rw (generation_start_segment (generation_of (i)));
        _ASSERTE(seg != NULL);

        while (seg)
        {
            seg->flags &= ~heap_segment_flags_swept;

#ifndef USE_REGIONS
            if (heap_segment_allocated (seg) == heap_segment_mem (seg))
            {
                FATAL_GC_ERROR();
            }

            if (seg == ephemeral_heap_segment)
            {
                heap_segment_background_allocated (seg) = generation_allocation_start (generation_of (max_generation - 1));
            }
            else
#endif //!USE_REGIONS
            {
                heap_segment_background_allocated (seg) = heap_segment_allocated (seg);
            }

            background_soh_size_end_mark += heap_segment_background_allocated (seg) - heap_segment_mem (seg);

            dprintf (3333, ("h%d gen%d seg %zx (%p) background allocated is %p",
                            heap_number, i, (size_t)(seg), heap_segment_mem (seg),
                            heap_segment_background_allocated (seg)));
            seg = heap_segment_next_rw (seg);
        }
    }

    // We need to void alloc contexts here 'cause while background_ephemeral_sweep is running
    // we can't let the user code consume the left over parts in these alloc contexts.
    repair_allocation_contexts (FALSE);

    dprintf (2, ("end of bgc mark: gen2 free list space: %zu, free obj space: %zu",
        generation_free_list_space (generation_of (max_generation)),
        generation_free_obj_space (generation_of (max_generation))));

    dprintf(2,("---- (GC%zu)End of background mark phase ----", VolatileLoad(&settings.gc_index)));
}

inline uint8_t* gc_heap::high_page (heap_segment* seg, BOOL concurrent_p)
{
#ifdef USE_REGIONS
    assert (!concurrent_p || (heap_segment_gen_num (seg) >= max_generation));
#else
    if (concurrent_p)
    {
        uint8_t* end = ((seg == ephemeral_heap_segment) ?
                     generation_allocation_start (generation_of (max_generation - 1)) :
                     heap_segment_allocated (seg));
        return align_lower_page (end);
    }
    else
#endif //USE_REGIONS
    {
        return heap_segment_allocated (seg);
    }
}

void gc_heap::revisit_written_page (uint8_t* page,
                                    uint8_t* end,
                                    BOOL concurrent_p,
                                    uint8_t*& last_page,
                                    uint8_t*& last_object,
                                    BOOL large_objects_p,
                                    size_t& num_marked_objects)
{
    uint8_t*   start_address = page;
    uint8_t*   o             = 0;
    int align_const = get_alignment_constant (!large_objects_p);
    uint8_t* high_address = end;
    uint8_t* current_lowest_address = background_saved_lowest_address;
    uint8_t* current_highest_address = background_saved_highest_address;
    BOOL no_more_loop_p = FALSE;

    THREAD_FROM_HEAP;
#ifndef MULTIPLE_HEAPS
    const int thread = heap_number;
#endif //!MULTIPLE_HEAPS

    if (large_objects_p)
    {
        o = last_object;
    }
    else
    {
        if (((last_page + WRITE_WATCH_UNIT_SIZE) == page)
            || (start_address <= last_object))
        {
            o = last_object;
        }
        else
        {
            o = find_first_object (start_address, last_object);
            // We can visit the same object again, but on a different page.
            assert (o >= last_object);
        }
    }

    dprintf (3,("page %zx start: %zx, %zx[ ",
               (size_t)page, (size_t)o,
               (size_t)(min (high_address, page + WRITE_WATCH_UNIT_SIZE))));

    while (o < (min (high_address, page + WRITE_WATCH_UNIT_SIZE)))
    {
        size_t s;

        if (concurrent_p && large_objects_p)
        {
            bgc_alloc_lock->bgc_mark_set (o);

            if (((CObjectHeader*)o)->IsFree())
            {
                s = unused_array_size (o);
            }
            else
            {
                s = size (o);
            }
        }
        else
        {
            s = size (o);
        }

        dprintf (3,("Considering object %zx(%s)", (size_t)o, (background_object_marked (o, FALSE) ? "bm" : "nbm")));

        assert (Align (s) >= Align (min_obj_size));

        uint8_t* next_o =  o + Align (s, align_const);

        if (next_o >= start_address)
        {
#ifdef MULTIPLE_HEAPS
            if (concurrent_p)
            {
                // We set last_object here for SVR BGC here because SVR BGC has more than
                // one GC thread. When we have more than one GC thread we would run into this
                // situation if we skipped unmarked objects:
                // bgc thread 1 calls GWW, and detect object X not marked so it would skip it
                // for revisit.
                // bgc thread 2 marks X and all its current children.
                // user thread comes along and dirties more (and later) pages in X.
                // bgc thread 1 calls GWW again and gets those later pages but it will not mark anything
                // on them because it had already skipped X. We need to detect that this object is now
                // marked and mark the children on the dirtied pages.
                // In the future if we have less BGC threads than we have heaps we should add
                // the check to the number of BGC threads.
                last_object = o;
            }
#endif //MULTIPLE_HEAPS

            if (contain_pointers (o) &&
                (!((o >= current_lowest_address) && (o < current_highest_address)) ||
                background_marked (o)))
            {
                dprintf (3, ("going through %zx", (size_t)o));
                go_through_object (method_table(o), o, s, poo, start_address, use_start, (o + s),
                                    if ((uint8_t*)poo >= min (high_address, page + WRITE_WATCH_UNIT_SIZE))
                                    {
                                        no_more_loop_p = TRUE;
                                        goto end_limit;
                                    }
                                    uint8_t* oo = VolatileLoadWithoutBarrier(poo);

                                    num_marked_objects++;
                                    background_mark_object (oo THREAD_NUMBER_ARG);
                                );
            }
            else if (concurrent_p &&
                     ((CObjectHeader*)o)->IsFree() &&
                     (next_o > min (high_address, page + WRITE_WATCH_UNIT_SIZE)))
            {
                // We need to not skip the object here because of this corner scenario:
                // A large object was being allocated during BGC mark so we first made it
                // into a free object, then cleared its memory. In this loop we would detect
                // that it's a free object which normally we would skip. But by the next time
                // we call GetWriteWatch we could still be on this object and the object had
                // been made into a valid object and some of its memory was changed. We need
                // to be sure to process those written pages so we can't skip the object just
                // yet.
                //
                // Similarly, when using software write watch, don't advance last_object when
                // the current object is a free object that spans beyond the current page or
                // high_address. Software write watch acquires gc_lock before the concurrent
                // GetWriteWatch() call during revisit_written_pages(). A foreground GC may
                // happen at that point and allocate from this free region, so when
                // revisit_written_pages() continues, it cannot skip now-valid objects in this
                // region.
                no_more_loop_p = TRUE;
                goto end_limit;
            }
        }
end_limit:
        if (concurrent_p && large_objects_p)
        {
            bgc_alloc_lock->bgc_mark_done ();
        }
        if (no_more_loop_p)
        {
            break;
        }
        o = next_o;
    }

#ifdef MULTIPLE_HEAPS
    if (concurrent_p)
    {
        assert (last_object < (min (high_address, page + WRITE_WATCH_UNIT_SIZE)));
    }
    else
#endif //MULTIPLE_HEAPS
    {
        last_object = o;
    }

    dprintf (3,("Last object: %zx", (size_t)last_object));
    last_page = align_write_watch_lower_page (o);

    if (concurrent_p)
    {
        allow_fgc();
    }
}

// When reset_only_p is TRUE, we should only reset pages that are in range
// because we need to consider the segments or part of segments that were
// allocated out of range all live.
void gc_heap::revisit_written_pages (BOOL concurrent_p, BOOL reset_only_p)
{
    if (concurrent_p && !reset_only_p)
    {
        current_bgc_state = bgc_revisit_soh;
    }

    size_t total_dirtied_pages = 0;
    size_t total_marked_objects = 0;

    bool reset_watch_state = !!concurrent_p;
    bool is_runtime_suspended = !concurrent_p;
    BOOL small_object_segments = TRUE;
    int start_gen_idx = get_start_generation_index();
#ifdef USE_REGIONS
    if (concurrent_p && !reset_only_p)
    {
        // We don't go into ephemeral regions during concurrent revisit.
        start_gen_idx = max_generation;
    }
#endif //USE_REGIONS

    for (int i = start_gen_idx; i < total_generation_count; i++)
    {
        heap_segment* seg = heap_segment_rw (generation_start_segment (generation_of (i)));
        _ASSERTE(seg != NULL);

        while (seg)
        {
            uint8_t* base_address = (uint8_t*)heap_segment_mem (seg);
            //we need to truncate to the base of the page because
            //some newly allocated could exist beyond heap_segment_allocated
            //and if we reset the last page write watch status,
            // they wouldn't be guaranteed to be visited -> gc hole.
            uintptr_t bcount = array_size;
            uint8_t* last_page = 0;
            uint8_t* last_object = heap_segment_mem (seg);
            uint8_t* high_address = 0;

            BOOL skip_seg_p = FALSE;

            if (reset_only_p)
            {
                if ((heap_segment_mem (seg) >= background_saved_lowest_address) ||
                    (heap_segment_reserved (seg) <= background_saved_highest_address))
                {
                    dprintf (3, ("h%d: sseg: %p(-%p)", heap_number,
                        heap_segment_mem (seg), heap_segment_reserved (seg)));
                    skip_seg_p = TRUE;
                }
            }

            if (!skip_seg_p)
            {
                dprintf (3, ("looking at seg %zx", (size_t)last_object));

                if (reset_only_p)
                {
                    base_address = max (base_address, background_saved_lowest_address);
                    dprintf (3, ("h%d: reset only starting %p", heap_number, base_address));
                }

                dprintf (3, ("h%d: starting: %p, seg %p-%p", heap_number, base_address,
                    heap_segment_mem (seg), heap_segment_reserved (seg)));


                while (1)
                {
                    if (reset_only_p)
                    {
                        high_address = ((seg == ephemeral_heap_segment) ? alloc_allocated : heap_segment_allocated (seg));
                        high_address = min (high_address, background_saved_highest_address);
                    }
                    else
                    {
                        high_address = high_page (seg, concurrent_p);
                    }

                    if ((base_address < high_address) &&
                        (bcount >= array_size))
                    {
                        ptrdiff_t region_size = high_address - base_address;
                        dprintf (3, ("h%d: gw: [%zx(%zd)", heap_number, (size_t)base_address, (size_t)region_size));

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
                        // When the runtime is not suspended, it's possible for the table to be resized concurrently with the scan
                        // for dirty pages below. Prevent that by synchronizing with grow_brick_card_tables(). When the runtime is
                        // suspended, it's ok to scan for dirty pages concurrently from multiple background GC threads for disjoint
                        // memory regions.
                        if (!is_runtime_suspended)
                        {
                            enter_spin_lock(&gc_lock);
                        }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

                        get_write_watch_for_gc_heap (reset_watch_state, base_address, region_size,
                                                     (void**)background_written_addresses,
                                                     &bcount, is_runtime_suspended);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
                        if (!is_runtime_suspended)
                        {
                            leave_spin_lock(&gc_lock);
                        }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

                        if (bcount != 0)
                        {
                            total_dirtied_pages += bcount;

                            dprintf (3, ("Found %zu pages [%zx, %zx[",
                                            bcount, (size_t)base_address, (size_t)high_address));
                        }

                        if (!reset_only_p)
                        {
                            // refetch the high address in case it has changed while we fetched dirty pages
                            // this is only an issue for the page high_address is on - we may have new
                            // objects after high_address.
                            high_address = high_page (seg, concurrent_p);

                            for (unsigned i = 0; i < bcount; i++)
                            {
                                uint8_t* page = (uint8_t*)background_written_addresses[i];
                                dprintf (3, ("looking at page %d at %zx(h: %zx)", i,
                                    (size_t)page, (size_t)high_address));
                                if (page < high_address)
                                {
                                    //search for marked objects in the page
                                    revisit_written_page (page, high_address, concurrent_p,
                                                          last_page, last_object,
                                                          !small_object_segments,
                                                          total_marked_objects);
                                }
                                else
                                {
                                    dprintf (3, ("page %d at %zx is >= %zx!", i, (size_t)page, (size_t)high_address));
                                    assert (!"page shouldn't have exceeded limit");
                                }
                            }
                        }

                        if (bcount >= array_size){
                            base_address = background_written_addresses [array_size-1] + WRITE_WATCH_UNIT_SIZE;
                            bcount = array_size;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            seg = heap_segment_next_rw (seg);
        }

        if (i == soh_gen2)
        {
            if (!reset_only_p)
            {
                dprintf (GTC_LOG, ("h%d: SOH: dp:%zd; mo: %zd", heap_number, total_dirtied_pages, total_marked_objects));
                fire_revisit_event (total_dirtied_pages, total_marked_objects, FALSE);
                concurrent_print_time_delta (concurrent_p ? "CR SOH" : "NR SOH");
                total_dirtied_pages = 0;
                total_marked_objects = 0;
            }

            if (concurrent_p && !reset_only_p)
            {
                current_bgc_state = bgc_revisit_uoh;
            }

            small_object_segments = FALSE;
            dprintf (3, ("now revisiting large object segments"));
        }
        else
        {
            if (reset_only_p)
            {
                dprintf (GTC_LOG, ("h%d: tdp: %zd", heap_number, total_dirtied_pages));
            }
            else
            {
                dprintf (GTC_LOG, ("h%d: LOH: dp:%zd; mo: %zd", heap_number, total_dirtied_pages, total_marked_objects));
                fire_revisit_event (total_dirtied_pages, total_marked_objects, TRUE);
            }
        }
    }
}

void gc_heap::background_grow_c_mark_list()
{
    assert (c_mark_list_index >= c_mark_list_length);
    BOOL should_drain_p = FALSE;
    THREAD_FROM_HEAP;
#ifndef MULTIPLE_HEAPS
    const int thread = heap_number;
#endif //!MULTIPLE_HEAPS

    dprintf (2, ("stack copy buffer overflow"));
    uint8_t** new_c_mark_list = 0;
    {
        FAULT_NOT_FATAL();
        if (c_mark_list_length >= (SIZE_T_MAX / (2 * sizeof (uint8_t*))))
        {
            should_drain_p = TRUE;
        }
        else
        {
            new_c_mark_list = new (nothrow) uint8_t*[c_mark_list_length*2];
            if (new_c_mark_list == 0)
            {
                should_drain_p = TRUE;
            }
        }
    }
    if (should_drain_p)

    {
        dprintf (2, ("No more memory for the stacks copy, draining.."));
        //drain the list by marking its elements
        background_drain_mark_list (thread);
    }
    else
    {
        assert (new_c_mark_list);
        memcpy (new_c_mark_list, c_mark_list, c_mark_list_length*sizeof(uint8_t*));
        c_mark_list_length = c_mark_list_length*2;
        dprintf (5555, ("h%d replacing mark list at %Ix with %Ix", heap_number, (size_t)c_mark_list, (size_t)new_c_mark_list));
        delete[] c_mark_list;
        c_mark_list = new_c_mark_list;
    }
}

void gc_heap::background_promote_callback (Object** ppObject, ScanContext* sc,
                                  uint32_t flags)
{
    UNREFERENCED_PARAMETER(sc);
    //in order to save space on the array, mark the object,
    //knowing that it will be visited later
    assert (settings.concurrent);

    THREAD_NUMBER_FROM_CONTEXT;
#ifndef MULTIPLE_HEAPS
    const int thread = 0;
#endif //!MULTIPLE_HEAPS

    uint8_t* o = (uint8_t*)*ppObject;

    if (!is_in_find_object_range (o))
    {
        return;
    }

    HEAP_FROM_THREAD;

    gc_heap* hp = gc_heap::heap_of (o);

    if ((o < hp->background_saved_lowest_address) || (o >= hp->background_saved_highest_address))
    {
        return;
    }

    if (flags & GC_CALL_INTERIOR)
    {
        o = hp->find_object (o);
        if (o == 0)
            return;
    }

#ifdef FEATURE_CONSERVATIVE_GC
    // For conservative GC, a value on stack may point to middle of a free object.
    // In this case, we don't need to promote the pointer.
    if (GCConfig::GetConservativeGC() && ((CObjectHeader*)o)->IsFree())
    {
        return;
    }
#endif //FEATURE_CONSERVATIVE_GC

#ifdef _DEBUG
    ((CObjectHeader*)o)->Validate();
#endif //_DEBUG

    dprintf (3, ("Concurrent Background Promote %zx", (size_t)o));
    if (o && (size (o) > loh_size_threshold))
    {
        dprintf (3, ("Brc %zx", (size_t)o));
    }

    if (hpt->c_mark_list_index >= hpt->c_mark_list_length)
    {
        hpt->background_grow_c_mark_list();
    }
    dprintf (3, ("pushing %zx into mark_list", (size_t)o));
    hpt->c_mark_list [hpt->c_mark_list_index++] = o;

    STRESS_LOG3(LF_GC|LF_GCROOTS, LL_INFO1000000, "    GCHeap::Background Promote: Promote GC Root *%p = %p MT = %pT", ppObject, o, o ? ((Object*) o)->GetGCSafeMethodTable() : NULL);
}

void gc_heap::mark_absorb_new_alloc()
{
    fix_allocation_contexts (FALSE);

    gen0_bricks_cleared = FALSE;

    clear_gen0_bricks();
}

#ifdef DYNAMIC_HEAP_COUNT
void gc_heap::add_to_bgc_th_creation_history (size_t gc_index, size_t count_created,
                                              size_t count_created_th_existed, size_t count_creation_failed)
{
    if ((count_created != 0) || (count_created_th_existed != 0) || (count_creation_failed != 0))
    {
        dprintf (6666, ("ADDING to BGC th hist entry%d gc index %Id, created %d, %d th existed, %d failed",
            bgc_th_creation_hist_index, gc_index, count_created, count_created_th_existed, count_creation_failed));

        bgc_thread_creation_history* current_hist = &bgc_th_creation_hist[bgc_th_creation_hist_index];
        current_hist->gc_index = gc_index;
        current_hist->n_heaps = (short)n_heaps;
        current_hist->count_created = (short)count_created;
        current_hist->count_created_th_existed = (short)count_created_th_existed;
        current_hist->count_creation_failed = (short)count_creation_failed;

        bgc_th_creation_hist_index = (bgc_th_creation_hist_index + 1) % max_bgc_thread_creation_count;
    }
}

#endif //DYNAMIC_HEAP_COUNT

// If this returns TRUE, we are saying we expect that thread to be there. However, when that thread is available to work is indeterministic.
// But when we actually start a BGC, naturally we'll need to wait till it gets to the point it can work.
BOOL gc_heap::prepare_bgc_thread(gc_heap* gh)
{
    BOOL success = FALSE;
    BOOL thread_created = FALSE;
    dprintf (2, ("Preparing gc thread"));
    gh->bgc_threads_timeout_cs.Enter();
    if (!(gh->bgc_thread_running))
    {
        dprintf (2, ("GC thread not running"));
        if (gh->bgc_thread == 0)
        {
#ifdef STRESS_DYNAMIC_HEAP_COUNT
            // to stress, we just don't actually try to create the thread to simulate a failure
            int r = (int)gc_rand::get_rand (100);
            bool try_to_create_p = (r > 10);
            BOOL thread_created_p = (try_to_create_p ? create_bgc_thread (gh) : FALSE);
            if (!thread_created_p)
            {
                dprintf (6666, ("h%d we failed to create the thread, %s", gh->heap_number, (try_to_create_p ? "tried" : "didn't try")));
            }
            if (thread_created_p)
#else //STRESS_DYNAMIC_HEAP_COUNT
            if (create_bgc_thread(gh))
#endif //STRESS_DYNAMIC_HEAP_COUNT
            {
                success = TRUE;
                thread_created = TRUE;
#ifdef DYNAMIC_HEAP_COUNT
                bgc_th_count_created++;
#endif //DYNAMIC_HEAP_COUNT
            }
            else
            {
#ifdef DYNAMIC_HEAP_COUNT
                bgc_th_count_creation_failed++;
#endif //DYNAMIC_HEAP_COUNT
            }
        }
        else
        {
#ifdef DYNAMIC_HEAP_COUNT
            // This would be a very unusual scenario where GCToEEInterface::CreateThread told us it failed yet the thread was created.
            bgc_th_count_created_th_existed++;
            dprintf (6666, ("h%d we cannot have a thread that runs yet CreateThread reported it failed to create it", gh->heap_number));
#endif //DYNAMIC_HEAP_COUNT
            assert (!"GCToEEInterface::CreateThread returned FALSE yet the thread was created!");
        }
    }
    else
    {
        dprintf (3, ("GC thread already running"));
        success = TRUE;
    }
    gh->bgc_threads_timeout_cs.Leave();

    if(thread_created)
        FIRE_EVENT(GCCreateConcurrentThread_V1);

    return success;
}

BOOL gc_heap::create_bgc_thread(gc_heap* gh)
{
    assert (background_gc_done_event.IsValid());

    //dprintf (2, ("Creating BGC thread"));

    gh->bgc_thread_running = GCToEEInterface::CreateThread(gh->bgc_thread_stub, gh, true, ".NET BGC");
    return gh->bgc_thread_running;
}

BOOL gc_heap::create_bgc_threads_support (int number_of_heaps)
{
    BOOL ret = FALSE;
    dprintf (3, ("Creating concurrent GC thread for the first time"));
    if (!background_gc_done_event.CreateManualEventNoThrow(TRUE))
    {
        goto cleanup;
    }
    if (!bgc_threads_sync_event.CreateManualEventNoThrow(FALSE))
    {
        goto cleanup;
    }
    if (!ee_proceed_event.CreateAutoEventNoThrow(FALSE))
    {
        goto cleanup;
    }
    if (!bgc_start_event.CreateManualEventNoThrow(FALSE))
    {
        goto cleanup;
    }

#ifdef MULTIPLE_HEAPS
    bgc_t_join.init (number_of_heaps, join_flavor_bgc);
#else
    UNREFERENCED_PARAMETER(number_of_heaps);
#endif //MULTIPLE_HEAPS

    ret = TRUE;

cleanup:

    if (!ret)
    {
        if (background_gc_done_event.IsValid())
        {
            background_gc_done_event.CloseEvent();
        }
        if (bgc_threads_sync_event.IsValid())
        {
            bgc_threads_sync_event.CloseEvent();
        }
        if (ee_proceed_event.IsValid())
        {
            ee_proceed_event.CloseEvent();
        }
        if (bgc_start_event.IsValid())
        {
            bgc_start_event.CloseEvent();
        }
    }

    return ret;
}

BOOL gc_heap::create_bgc_thread_support()
{
    uint8_t** parr;

    //needs to have room for enough smallest objects fitting on a page
    parr = new (nothrow) uint8_t*[1 + OS_PAGE_SIZE / MIN_OBJECT_SIZE];
    if (!parr)
    {
        return FALSE;
    }

    make_c_mark_list (parr);

    return TRUE;
}

int gc_heap::check_for_ephemeral_alloc()
{
    int gen = ((settings.reason == reason_oos_soh) ? (max_generation - 1) : -1);

    if (gen == -1)
    {
#ifdef MULTIPLE_HEAPS
        for (int heap_index = 0; heap_index < n_heaps; heap_index++)
#endif //MULTIPLE_HEAPS
        {
            for (int i = 0; i < max_generation; i++)
            {
#ifdef MULTIPLE_HEAPS
                if (g_heaps[heap_index]->get_new_allocation (i) <= 0)
#else
                if (get_new_allocation (i) <= 0)
#endif //MULTIPLE_HEAPS
                {
                    gen = max (gen, i);
                }
                else
                    break;
            }
        }
    }

    return gen;
}

// Wait for gc to finish sequential part
void gc_heap::wait_to_proceed()
{
    assert (background_gc_done_event.IsValid());
    assert (bgc_start_event.IsValid());

    user_thread_wait(&ee_proceed_event, FALSE);
}

// Start a new concurrent gc
void gc_heap::start_c_gc()
{
    assert (background_gc_done_event.IsValid());
    assert (bgc_start_event.IsValid());

//Need to make sure that the gc thread is in the right place.
    background_gc_done_event.Wait(INFINITE, FALSE);
    background_gc_done_event.Reset();
    bgc_start_event.Set();
}

void gc_heap::do_background_gc()
{
    dprintf (2, ("starting a BGC"));
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        g_heaps[i]->init_background_gc();
    }
#else
    init_background_gc();
#endif //MULTIPLE_HEAPS

#ifdef BGC_SERVO_TUNING
    bgc_tuning::record_bgc_start();
#endif //BGC_SERVO_TUNING

    //start the background gc
    start_c_gc ();

    //wait until we get restarted by the BGC.
    wait_to_proceed();
}

void gc_heap::kill_gc_thread()
{
    //assert (settings.concurrent == FALSE);

    // We are doing a two-stage shutdown now.
    // In the first stage, we do minimum work, and call ExitProcess at the end.
    // In the secodn stage, we have the Loader lock and only one thread is
    // alive.  Hence we do not need to kill gc thread.
    background_gc_done_event.CloseEvent();
    bgc_start_event.CloseEvent();
    bgc_threads_timeout_cs.Destroy();
    bgc_thread = 0;
}

void gc_heap::bgc_thread_function()
{
    assert (background_gc_done_event.IsValid());
    assert (bgc_start_event.IsValid());

    dprintf (3, ("gc_thread thread starting..."));

    BOOL do_exit = FALSE;

    bool cooperative_mode = true;
    bgc_thread_id.SetToCurrentThread();
    dprintf (1, ("bgc_thread_id is set to %x", (uint32_t)GCToOSInterface::GetCurrentThreadIdForLogging()));
    while (1)
    {
        // Wait for work to do...
        dprintf (6666, ("h%d bgc thread: waiting...", heap_number));

        cooperative_mode = enable_preemptive ();
        //current_thread->m_fPreemptiveGCDisabled = 0;

        uint32_t result = bgc_start_event.Wait(
#ifdef _DEBUG
#ifdef MULTIPLE_HEAPS
                                             INFINITE,
#else
                                             2000,
#endif //MULTIPLE_HEAPS
#else //_DEBUG
#ifdef MULTIPLE_HEAPS
                                             INFINITE,
#else
                                             20000,
#endif //MULTIPLE_HEAPS
#endif //_DEBUG
            FALSE);
        dprintf (2, ("gc thread: finished waiting"));

        // not calling disable_preemptive here 'cause we
        // can't wait for GC complete here - RestartEE will be called
        // when we've done the init work.

        if (result == WAIT_TIMEOUT)
        {
            // Should join the bgc threads and terminate all of them
            // at once.
            dprintf (1, ("GC thread timeout"));
            bgc_threads_timeout_cs.Enter();
            if (!keep_bgc_threads_p)
            {
                dprintf (2, ("GC thread exiting"));
                bgc_thread_running = FALSE;
                bgc_thread = 0;
                bgc_thread_id.Clear();
                do_exit = TRUE;
            }
            bgc_threads_timeout_cs.Leave();
            if (do_exit)
                break;
            else
            {
                dprintf (3, ("GC thread needed, not exiting"));
                continue;
            }
        }

#ifdef STRESS_DYNAMIC_HEAP_COUNT
        if (n_heaps <= heap_number)
        {
            uint32_t delay_ms = (uint32_t)gc_rand::get_rand (200);
            GCToOSInterface::Sleep (delay_ms);
        }
#endif //STRESS_DYNAMIC_HEAP_COUNT

        // if we signal the thread with no concurrent work to do -> exit
        if (!settings.concurrent)
        {
            dprintf (6666, ("h%d no concurrent GC needed, exiting", heap_number));

#if defined(TRACE_GC) && defined(SIMPLE_DPRINTF) && defined(STRESS_DYNAMIC_HEAP_COUNT)
            flush_gc_log (true);
            GCToOSInterface::DebugBreak();
#endif
            break;
        }

#ifdef DYNAMIC_HEAP_COUNT
        if (n_heaps <= heap_number)
        {
            Interlocked::Increment (&dynamic_heap_count_data.idle_bgc_thread_count);
            add_to_bgc_hc_history (hc_record_bgc_inactive);

            // this is the case where we have more background GC threads than heaps
            // - wait until we're told to continue...
            dprintf (6666, ("BGC%Id h%d going idle (%d heaps), idle count is now %d",
                VolatileLoadWithoutBarrier (&settings.gc_index), heap_number, n_heaps, VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_bgc_thread_count)));
            bgc_idle_thread_event.Wait(INFINITE, FALSE);
            dprintf (6666, ("BGC%Id h%d woke from idle (%d heaps), idle count is now %d",
                VolatileLoadWithoutBarrier (&settings.gc_index), heap_number, n_heaps, VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_bgc_thread_count)));
            continue;
        }
        else
        {
            if (heap_number == 0)
            {
                const int spin_count = 1024;
                int idle_bgc_thread_count = total_bgc_threads - n_heaps;
                dprintf (6666, ("n_heaps %d, total %d bgc threads, bgc idle should be %d and is %d",
                    n_heaps, total_bgc_threads, idle_bgc_thread_count, VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_bgc_thread_count)));
                if (idle_bgc_thread_count != dynamic_heap_count_data.idle_bgc_thread_count)
                {
                    dprintf (6666, ("current idle is %d, trying to get to %d",
                        VolatileLoadWithoutBarrier (&dynamic_heap_count_data.idle_bgc_thread_count), idle_bgc_thread_count));
                    spin_and_wait (spin_count, (idle_bgc_thread_count == dynamic_heap_count_data.idle_bgc_thread_count));
                }
            }

            add_to_bgc_hc_history (hc_record_bgc_active);
        }
#endif //DYNAMIC_HEAP_COUNT

        if (heap_number == 0)
        {
            gc_background_running = TRUE;
            dprintf (6666, (ThreadStressLog::gcStartBgcThread(), heap_number,
                generation_free_list_space (generation_of (max_generation)),
                generation_free_obj_space (generation_of (max_generation)),
                dd_fragmentation (dynamic_data_of (max_generation))));
        }

        gc1();

#ifndef DOUBLY_LINKED_FL
        current_bgc_state = bgc_not_in_process;
#endif //!DOUBLY_LINKED_FL

        enable_preemptive ();
#ifdef MULTIPLE_HEAPS
        bgc_t_join.join(this, gc_join_done);
        if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
        {
            enter_spin_lock (&gc_lock);
            dprintf (SPINLOCK_LOG, ("bgc Egc"));

            bgc_start_event.Reset();
            do_post_gc();
#ifdef MULTIPLE_HEAPS
            for (int gen = max_generation; gen < total_generation_count; gen++)
            {
                size_t desired_per_heap = 0;
                size_t total_desired = 0;
                gc_heap* hp = 0;
                dynamic_data* dd;
                for (int i = 0; i < n_heaps; i++)
                {
                    hp = g_heaps[i];
                    dd = hp->dynamic_data_of (gen);
                    size_t temp_total_desired = total_desired + dd_desired_allocation (dd);
                    if (temp_total_desired < total_desired)
                    {
                        // we overflowed.
                        total_desired = (size_t)MAX_PTR;
                        break;
                    }
                    total_desired = temp_total_desired;
                }

                desired_per_heap = Align ((total_desired/n_heaps), get_alignment_constant (FALSE));

                if (gen >= loh_generation)
                {
                    desired_per_heap = exponential_smoothing (gen, dd_collection_count (dynamic_data_of (max_generation)), desired_per_heap);
                }

                for (int i = 0; i < n_heaps; i++)
                {
                    hp = gc_heap::g_heaps[i];
                    dd = hp->dynamic_data_of (gen);
                    dd_desired_allocation (dd) = desired_per_heap;
                    dd_gc_new_allocation (dd) = desired_per_heap;
                    dd_new_allocation (dd) = desired_per_heap;
                }
            }

            fire_pevents();
#endif //MULTIPLE_HEAPS

#ifdef DYNAMIC_HEAP_COUNT
            if (trigger_bgc_for_rethreading_p)
            {
                trigger_bgc_for_rethreading_p = false;
            }
#endif //DYNAMIC_HEAP_COUNT

            c_write (settings.concurrent, FALSE);
            gc_background_running = FALSE;
            keep_bgc_threads_p = FALSE;
            background_gc_done_event.Set();

            dprintf (SPINLOCK_LOG, ("bgc Lgc"));
            leave_spin_lock (&gc_lock);
#ifdef MULTIPLE_HEAPS
            dprintf(1, ("End of BGC"));
            bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
        }
        // We can't disable preempt here because there might've been a GC already
        // started and decided to do a BGC and waiting for a BGC thread to restart
        // vm. That GC will be waiting in wait_to_proceed and we are waiting for it
        // to restart the VM so we deadlock.
        //gc_heap::disable_preemptive (true);
    }

    FIRE_EVENT(GCTerminateConcurrentThread_V1);

    dprintf (3, ("bgc_thread thread exiting"));
    return;
}

BOOL gc_heap::background_object_marked (uint8_t* o, BOOL clearp)
{
    BOOL m = FALSE;
    if ((o >= background_saved_lowest_address) && (o < background_saved_highest_address))
    {
        if (mark_array_marked (o))
        {
            if (clearp)
            {
                mark_array_clear_marked (o);
                //dprintf (3, ("mark array bit for object %zx is cleared", o));
                dprintf (3, ("CM: %p", o));
            }
            m = TRUE;
        }
        else
            m = FALSE;
    }
    else
        m = TRUE;

    dprintf (3, ("o %p(%zu) %s", o, size(o), (m ? "was bm" : "was NOT bm")));
    return m;
}

void gc_heap::background_delay_delete_uoh_segments()
{
    for (int i = uoh_start_generation; i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
        heap_segment* prev_seg = 0;

#ifdef USE_REGIONS
        heap_segment* first_remaining_region = 0;
#endif //USE_REGIONS

        while (seg)
        {
            heap_segment* next_seg = heap_segment_next (seg);
            if (seg->flags & heap_segment_flags_uoh_delete)
            {
                dprintf (3, ("deleting %zx-%p-%p", (size_t)seg, heap_segment_allocated (seg), heap_segment_reserved (seg)));
                delete_heap_segment (seg, (GCConfig::GetRetainVM() != 0));
                heap_segment_next (prev_seg) = next_seg;
#ifdef USE_REGIONS
                update_start_tail_regions (gen, seg, prev_seg, next_seg);
#endif //USE_REGIONS
            }
            else
            {
#ifdef USE_REGIONS
                if (!first_remaining_region)
                    first_remaining_region = seg;
#endif //USE_REGIONS
                prev_seg = seg;
            }

            seg = next_seg;
        }

#ifdef USE_REGIONS
        assert (heap_segment_rw (generation_start_segment (gen)) == generation_start_segment (gen));
        if (generation_start_segment (gen) != first_remaining_region)
        {
            dprintf (REGIONS_LOG, ("h%d gen%d start %p -> %p",
                heap_number, gen->gen_num,
                heap_segment_mem (generation_start_segment (gen)),
                heap_segment_mem (first_remaining_region)));
            generation_start_segment (gen) = first_remaining_region;
        }
        if (generation_tail_region (gen) != prev_seg)
        {
            dprintf (REGIONS_LOG, ("h%d gen%d start %p -> %p",
                heap_number, gen->gen_num,
                heap_segment_mem (generation_tail_region (gen)),
                heap_segment_mem (prev_seg)));
            generation_tail_region (gen) = prev_seg;
        }
#endif //USE_REGIONS
    }
}

uint8_t* gc_heap::background_next_end (heap_segment* seg, BOOL uoh_objects_p)
{
    return
        (uoh_objects_p ? heap_segment_allocated (seg) : heap_segment_background_allocated (seg));
}

void gc_heap::process_background_segment_end (heap_segment* seg,
                                              generation* gen,
                                              uint8_t* last_plug_end,
                                              heap_segment* start_seg,
                                              BOOL* delete_p,
                                              size_t free_obj_size_last_gap)
{
    *delete_p = FALSE;
    uint8_t* allocated = heap_segment_allocated (seg);
    uint8_t* background_allocated = heap_segment_background_allocated (seg);
    BOOL uoh_p = heap_segment_uoh_p (seg);

    dprintf (3, ("EoS [%zx, %p[(%p[), last: %p(%zu)",
                (size_t)heap_segment_mem (seg), background_allocated, allocated, last_plug_end, free_obj_size_last_gap));

    if (!uoh_p && (allocated != background_allocated))
    {
        assert (gen->gen_num <= max_generation);

        dprintf (3, ("Make a free object before newly promoted objects [%zx, %p[",
                    (size_t)last_plug_end, background_allocated));

        size_t last_gap = background_allocated - last_plug_end;
        if (last_gap > 0)
        {
            thread_gap (last_plug_end, last_gap, generation_of (max_generation));
            add_gen_free (max_generation, last_gap);

            fix_brick_to_highest (last_plug_end, background_allocated);

            // When we allowed fgc's during going through gaps, we could have erased the brick
            // that corresponds to bgc_allocated 'cause we had to update the brick there,
            // recover it here.
            fix_brick_to_highest (background_allocated, background_allocated);
        }
    }
    else
    {
        // by default, if allocated == background_allocated, it can't
        // be the ephemeral segment.
        if (seg == ephemeral_heap_segment)
        {
            FATAL_GC_ERROR();
        }

#ifndef USE_REGIONS
        if (allocated == heap_segment_mem (seg))
        {
            // this can happen with UOH segments when multiple threads
            // allocate new segments and not all of them were needed to
            // satisfy allocation requests.
            assert (gen->gen_num > max_generation);
        }
#endif //!USE_REGIONS

        if (last_plug_end == heap_segment_mem (seg))
        {
            // REGIONS TODO: start_seg doesn't matter for regions. We can get rid of it too.
            // Just need to update the start segment accordingly in generation_delete_heap_segment.
            // Also this might leave us with no regions at all for gen2 and we should be prepared
            // for that. One approach is to ensure at least one region per generation at the beginning
            // of a GC.
            if (seg != start_seg)
            {
                *delete_p = TRUE;
            }

            dprintf (3, ("h%d seg %p %s be deleted", heap_number,
                        heap_segment_mem (seg), (*delete_p ? "should" : "should not")));

        }
        if (!*delete_p)
        {
            dprintf (3, ("[h%d] seg %zx alloc %p->%zx",
                heap_number, (size_t)seg,
                heap_segment_allocated (seg),
                (size_t)last_plug_end));
            heap_segment_allocated (seg) = last_plug_end;
            set_mem_verify (heap_segment_allocated (seg) - plug_skew, heap_segment_used (seg), 0xbb);

            decommit_heap_segment_pages (seg, 0);
        }
    }

    if (free_obj_size_last_gap)
    {
        generation_free_obj_space (gen) -= free_obj_size_last_gap;
        dprintf (2, ("[h%d] PS: gen2FO-: %zd->%zd",
            heap_number, free_obj_size_last_gap, generation_free_obj_space (gen)));
    }

    dprintf (3, ("verifying seg %p's mark array was completely cleared", seg));
    bgc_verify_mark_array_cleared (seg);
}

inline
BOOL gc_heap::fgc_should_consider_object (uint8_t* o,
                                          heap_segment* seg,
                                          BOOL consider_bgc_mark_p,
                                          BOOL check_current_sweep_p,
                                          BOOL check_saved_sweep_p)
{
#ifdef USE_REGIONS
    assert (!check_saved_sweep_p);
#endif //USE_REGIONS

    // the logic for this function must be kept in sync with the analogous function
    // in ToolBox\SOS\Strike\gc.cpp

    // TRUE means we don't need to check the bgc mark bit
    // FALSE means we do.
    BOOL no_bgc_mark_p = FALSE;

    if (consider_bgc_mark_p)
    {
        if (check_current_sweep_p && (o < current_sweep_pos))
        {
            dprintf (3, ("no bgc mark - o: %p < cs: %p", o, current_sweep_pos));
            no_bgc_mark_p = TRUE;
        }

        if (!no_bgc_mark_p)
        {
#ifndef USE_REGIONS
            if(check_saved_sweep_p && (o >= saved_sweep_ephemeral_start))
            {
                dprintf (3, ("no bgc mark - o: %p >= ss: %p", o, saved_sweep_ephemeral_start));
                no_bgc_mark_p = TRUE;
            }
#endif //!USE_REGIONS
            if (!check_saved_sweep_p)
            {
                uint8_t* background_allocated = heap_segment_background_allocated (seg);

#ifndef USE_REGIONS
                // if this was the saved ephemeral segment, check_saved_sweep_p
                // would've been true.
                assert (heap_segment_background_allocated (seg) != saved_sweep_ephemeral_start);
#endif //!USE_REGIONS

                // background_allocated could be 0 for the new segments acquired during bgc
                // sweep and we still want no_bgc_mark_p to be true.
                if (o >= background_allocated)
                {
                    dprintf (3, ("no bgc mark - o: %p >= ba: %p", o, background_allocated));
                    no_bgc_mark_p = TRUE;
                }
            }
        }
    }
    else
    {
        no_bgc_mark_p = TRUE;
    }

    dprintf (3, ("bgc mark %p: %s (bm: %s)", o, (no_bgc_mark_p ? "no" : "yes"), ((no_bgc_mark_p || background_object_marked (o, FALSE)) ? "yes" : "no")));
    return (no_bgc_mark_p ? TRUE : background_object_marked (o, FALSE));
}

// consider_bgc_mark_p tells you if you need to care about the bgc mark bit at all
// if it's TRUE, check_current_sweep_p tells you if you should consider the
// current sweep position or not.
void gc_heap::should_check_bgc_mark (heap_segment* seg,
                                     BOOL* consider_bgc_mark_p,
                                     BOOL* check_current_sweep_p,
                                     BOOL* check_saved_sweep_p)
{
    // the logic for this function must be kept in sync with the analogous function
    // in ToolBox\SOS\Strike\gc.cpp
    *consider_bgc_mark_p = FALSE;
    *check_current_sweep_p = FALSE;
    *check_saved_sweep_p = FALSE;

    if (current_c_gc_state == c_gc_state_planning)
    {
        // We are doing the current_sweep_pos comparison here because we have yet to
        // turn on the swept flag for the segment but in_range_for_segment will return
        // FALSE if the address is the same as reserved.
        if ((seg->flags & heap_segment_flags_swept) || (current_sweep_pos == heap_segment_reserved (seg)))
        {
            dprintf (3, ("seg %p is already swept by bgc", seg));
        }
        else if (heap_segment_background_allocated (seg) == 0)
        {
            dprintf (3, ("seg %p newly alloc during bgc", seg));
        }
        else
        {
            *consider_bgc_mark_p = TRUE;

            dprintf (3, ("seg %p hasn't been swept by bgc", seg));

#ifndef USE_REGIONS
            if (seg == saved_sweep_ephemeral_seg)
            {
                dprintf (3, ("seg %p is the saved ephemeral seg", seg));
                *check_saved_sweep_p = TRUE;
            }
#endif //!USE_REGIONS

            if (in_range_for_segment (current_sweep_pos, seg))
            {
                dprintf (3, ("current sweep pos is %p and within seg %p",
                              current_sweep_pos, seg));
                *check_current_sweep_p = TRUE;
            }
        }
    }
}

// REGIONS TODO: I'm not releasing any empty ephemeral regions here the gen0 allocator is
// iterating over these regions. We'd want to do the same as what we do with LOH segs/regions.
void gc_heap::background_ephemeral_sweep()
{
    dprintf (3, ("bgc ephemeral sweep"));

    int align_const = get_alignment_constant (TRUE);

#ifndef USE_REGIONS
    saved_sweep_ephemeral_seg = ephemeral_heap_segment;
    saved_sweep_ephemeral_start = generation_allocation_start (generation_of (max_generation - 1));
#endif //!USE_REGIONS

    // Since we don't want to interfere with gen0 allocation while we are threading gen0 free list,
    // we thread onto a list first then publish it when we are done.
    allocator youngest_free_list;
    size_t youngest_free_list_space = 0;
    size_t youngest_free_obj_space = 0;

    youngest_free_list.clear();

    for (int i = 0; i <= (max_generation - 1); i++)
    {
        generation* gen_to_reset = generation_of (i);
        assert (generation_free_list_space (gen_to_reset) == 0);
        // Can only assert free_list_space is 0, not free_obj_space as the allocator could have added
        // something there.
    }

    for (int i = (max_generation - 1); i >= 0; i--)
    {
        generation* current_gen = generation_of (i);
#ifdef USE_REGIONS
        heap_segment* ephemeral_region = heap_segment_rw (generation_start_segment (current_gen));
        while (ephemeral_region)
#endif //USE_REGIONS
        {
#ifdef USE_REGIONS
            uint8_t* o = heap_segment_mem (ephemeral_region);
            uint8_t* end = heap_segment_background_allocated (ephemeral_region);
            dprintf (3, ("bgc eph: gen%d seg %p(%p-%p)",
                heap_segment_gen_num (ephemeral_region),
                heap_segment_mem (ephemeral_region),
                heap_segment_allocated (ephemeral_region),
                heap_segment_background_allocated (ephemeral_region)));
            // This doesn't conflict with the allocator getting a new region in gen0.
            // If the allocator just threaded a region onto the gen0 region list we will
            // read that region and detect that its background allocated is 0.
            if (!end)
            {
                ephemeral_region->flags |= heap_segment_flags_swept;
                ephemeral_region = heap_segment_next (ephemeral_region);
                continue;
            }
#else //USE_REGIONS
            uint8_t* o = generation_allocation_start (current_gen);
            //Skip the generation gap object
            o = o + Align(size (o), align_const);
            uint8_t* end = ((i > 0) ?
                        generation_allocation_start (generation_of (i - 1)) :
                        heap_segment_allocated (ephemeral_heap_segment));
#endif //USE_REGIONS

            uint8_t* plug_end = o;
            uint8_t* plug_start = o;
            BOOL marked_p = FALSE;

            while (o < end)
            {
                marked_p = background_object_marked (o, TRUE);
                if (marked_p)
                {
                    plug_start = o;
                    size_t plug_size = plug_start - plug_end;

                    if (i >= 1)
                    {
                        thread_gap (plug_end, plug_size, current_gen);
                    }
                    else
                    {
                        if (plug_size > 0)
                        {
                            make_unused_array (plug_end, plug_size);
                            if (plug_size >= min_free_list)
                            {
                                youngest_free_list_space += plug_size;
                                youngest_free_list.thread_item (plug_end, plug_size);
                            }
                            else
                            {
                                youngest_free_obj_space += plug_size;
                            }
                        }
                    }

                    fix_brick_to_highest (plug_end, plug_start);
                    fix_brick_to_highest (plug_start, plug_start);

                    BOOL m = TRUE;
                    while (m)
                    {
                        o = o + Align (size (o), align_const);
                        if (o >= end)
                        {
                            break;
                        }

                        m = background_object_marked (o, TRUE);
                    }
                    plug_end = o;
                    dprintf (3, ("bgs: plug [%zx, %zx[", (size_t)plug_start, (size_t)plug_end));
                }
                else
                {
                    while ((o < end) && !background_object_marked (o, FALSE))
                    {
                        o = o + Align (size (o), align_const);
                    }
                }
            }

            if (plug_end != end)
            {
                if (i >= 1)
                {
                    thread_gap (plug_end, end - plug_end, current_gen);
                }
                else
                {
#ifndef USE_REGIONS
                    heap_segment_allocated (ephemeral_heap_segment) = plug_end;
                    heap_segment_saved_bg_allocated (ephemeral_heap_segment) = plug_end;
#endif //!USE_REGIONS
                    make_unused_array (plug_end, (end - plug_end));
                }

                fix_brick_to_highest (plug_end, end);
            }
#ifdef USE_REGIONS
            ephemeral_region->flags |= heap_segment_flags_swept;
            // Setting this to 0 so background_sweep can terminate for SOH.
            heap_segment_background_allocated (ephemeral_region) = 0;
            ephemeral_region = heap_segment_next (ephemeral_region);
#endif //USE_REGIONS
        }
        dd_fragmentation (dynamic_data_of (i)) =
            generation_free_list_space (current_gen) + generation_free_obj_space (current_gen);
    }

    generation* youngest_gen = generation_of (0);
    generation_free_list_space (youngest_gen) = youngest_free_list_space;
    generation_free_obj_space (youngest_gen) = youngest_free_obj_space;
    dd_fragmentation (dynamic_data_of (0)) = youngest_free_list_space + youngest_free_obj_space;
    generation_allocator (youngest_gen)->copy_with_no_repair (&youngest_free_list);
}

void gc_heap::background_sweep()
{
    //concurrent_print_time_delta ("finished with mark and start with sweep");
    concurrent_print_time_delta ("Sw");
    dprintf (2, ("---- (GC%zu)Background Sweep Phase ----", VolatileLoad(&settings.gc_index)));

    bool rebuild_maxgen_fl_p = true;

#ifdef DOUBLY_LINKED_FL
#ifdef DYNAMIC_HEAP_COUNT
    rebuild_maxgen_fl_p = trigger_bgc_for_rethreading_p;
#else
    rebuild_maxgen_fl_p = false;
#endif //DYNAMIC_HEAP_COUNT
#endif //DOUBLY_LINKED_FL

    for (int i = 0; i <= max_generation; i++)
    {
        generation* gen_to_reset = generation_of (i);

        bool clear_fl_p = true;

#ifdef DOUBLY_LINKED_FL
        if (i == max_generation)
        {
            clear_fl_p = rebuild_maxgen_fl_p;

            dprintf (6666, ("h%d: gen2 still has FL: %zd, FO: %zd, clear gen2 FL %s",
                heap_number,
                generation_free_list_space (gen_to_reset),
                generation_free_obj_space (gen_to_reset),
                (clear_fl_p ? "yes" : "no")));
        }
#endif //DOUBLY_LINKED_FL

        if (clear_fl_p)
        {
            if (i == max_generation)
            {
                dprintf (6666, ("clearing g2 FL for h%d!", heap_number));
            }
            generation_allocator (gen_to_reset)->clear();
            generation_free_list_space (gen_to_reset) = 0;
            generation_free_obj_space (gen_to_reset) = 0;
        }

        generation_free_list_allocated (gen_to_reset) = 0;
        generation_end_seg_allocated (gen_to_reset) = 0;
        generation_condemned_allocated (gen_to_reset) = 0;
        generation_sweep_allocated (gen_to_reset) = 0;
        //reset the allocation so foreground gc can allocate into older generation
        generation_allocation_pointer (gen_to_reset)= 0;
        generation_allocation_limit (gen_to_reset) = 0;
        generation_allocation_segment (gen_to_reset) = heap_segment_rw (generation_start_segment (gen_to_reset));
    }

    FIRE_EVENT(BGC2ndNonConEnd);

    uoh_alloc_thread_count = 0;

    init_free_and_plug();

    current_bgc_state = bgc_sweep_soh;
    verify_soh_segment_list();

#ifdef DOUBLY_LINKED_FL
    // set the initial segment and position so that foreground GC knows where BGC is with the sweep
    current_sweep_seg = heap_segment_rw (generation_start_segment (generation_of (max_generation)));
    current_sweep_pos = 0;
#endif //DOUBLY_LINKED_FL

#ifdef FEATURE_BASICFREEZE
    sweep_ro_segments();
#endif //FEATURE_BASICFREEZE

    dprintf (3, ("lh state: planning"));

    // Multiple threads may reach here.  This conditional partially avoids multiple volatile writes.
    if (current_c_gc_state != c_gc_state_planning)
    {
        current_c_gc_state = c_gc_state_planning;
    }

    concurrent_print_time_delta ("Swe");

    for (int i = uoh_start_generation; i < total_generation_count; i++)
    {
        heap_segment* uoh_seg = heap_segment_rw (generation_start_segment (generation_of (i)));
        _ASSERTE(uoh_seg  != NULL);
        while (uoh_seg)
        {
            uoh_seg->flags &= ~heap_segment_flags_swept;
            heap_segment_background_allocated (uoh_seg) = heap_segment_allocated (uoh_seg);
            uoh_seg = heap_segment_next_rw (uoh_seg);
        }
    }

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_restart_ee);
    if (bgc_t_join.joined())
    {
        dprintf(2, ("Starting BGC threads for resuming EE"));
        bgc_t_join.restart();
    }
#endif //MULTIPLE_HEAPS

    if (heap_number == 0)
    {
        get_and_reset_uoh_alloc_info();
        uint64_t suspended_end_ts = GetHighPrecisionTimeStamp();
        last_bgc_info[last_bgc_info_index].pause_durations[1] = (size_t)(suspended_end_ts - suspended_start_time);
        total_suspended_time += last_bgc_info[last_bgc_info_index].pause_durations[1];
        restart_EE ();
    }

    FIRE_EVENT(BGC2ndConBegin);

    background_ephemeral_sweep();

    concurrent_print_time_delta ("Swe eph");

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_after_ephemeral_sweep);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef FEATURE_EVENT_TRACE
        bgc_heap_walk_for_etw_p = GCEventStatus::IsEnabled(GCEventProvider_Default,
                                                           GCEventKeyword_GCHeapSurvivalAndMovement,
                                                           GCEventLevel_Information);
#endif //FEATURE_EVENT_TRACE

        leave_spin_lock (&gc_lock);

#ifdef MULTIPLE_HEAPS
        dprintf(2, ("Starting BGC threads for BGC sweeping"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    disable_preemptive (true);

    dynamic_data* dd     = dynamic_data_of (max_generation);
    const int num_objs   = 256;
    int current_num_objs = 0;

    for (int i = max_generation; i < total_generation_count; i++)
    {
        generation* gen = generation_of (i);
        heap_segment* gen_start_seg = heap_segment_rw (generation_start_segment(gen));
        heap_segment* next_seg = 0;
        heap_segment* prev_seg;
        heap_segment* start_seg;
        int align_const = get_alignment_constant (i == max_generation);

#ifndef DOUBLY_LINKED_FL
        if (i == max_generation)
        {
#ifdef USE_REGIONS
            start_seg = generation_tail_region (gen);
#else
            // start with saved ephemeral segment
            // we are no longer holding gc_lock, so a new ephemeral segment could be added, we want the saved one.
            start_seg = saved_sweep_ephemeral_seg;
#endif //USE_REGIONS
            prev_seg = heap_segment_next(start_seg);
        }
        else
#endif //!DOUBLY_LINKED_FL
        {
            // If we use doubly linked FL we don't need to go backwards as we are maintaining the free list.
            start_seg = gen_start_seg;
            prev_seg = NULL;

            if (i > max_generation)
            {
                // UOH allocations are allowed while sweeping SOH, so
                // we defer clearing UOH free lists until we start sweeping them
                generation_allocator (gen)->clear();
                generation_free_list_space (gen) = 0;
                generation_free_obj_space (gen) = 0;
                generation_free_list_allocated (gen) = 0;
                generation_end_seg_allocated (gen) = 0;
                generation_condemned_allocated (gen) = 0;
                generation_sweep_allocated (gen) = 0;
                generation_allocation_pointer (gen)= 0;
                generation_allocation_limit (gen) = 0;
                generation_allocation_segment (gen) = heap_segment_rw (generation_start_segment (gen));
            }
            else
            {
                dprintf (3333, ("h%d: SOH sweep start on seg %zx: total FL: %zd, FO: %zd",
                    heap_number, (size_t)start_seg,
                    generation_free_list_space (gen),
                    generation_free_obj_space (gen)));
            }
        }

        _ASSERTE(start_seg != NULL);
        heap_segment* seg = start_seg;
        dprintf (2, ("bgs: sweeping gen %d seg %p->%p(%p)", gen->gen_num,
            heap_segment_mem (seg),
            heap_segment_allocated (seg),
            heap_segment_background_allocated (seg)));
        while (seg
#ifdef DOUBLY_LINKED_FL
               // We no longer go backwards in segment list for SOH so we need to bail when we see
               // segments newly allocated during bgc sweep.
               && !((heap_segment_background_allocated (seg) == 0) && (gen != large_object_generation))
#endif //DOUBLY_LINKED_FL
                )
        {
            uint8_t* o = heap_segment_mem (seg);
            if (seg == gen_start_seg)
            {
#ifndef USE_REGIONS
                assert (o == generation_allocation_start (gen));
                assert (method_table (o) == g_gc_pFreeObjectMethodTable);
                o = o + Align (size (o), align_const);
#endif //!USE_REGIONS
            }

            uint8_t* plug_end = o;
            current_sweep_pos = o;
            next_sweep_obj = o;
#ifdef DOUBLY_LINKED_FL
            current_sweep_seg = seg;
#endif //DOUBLY_LINKED_FL

            // This records the total size of free objects (including the ones on and not on FL)
            // in the gap and it gets set to 0 when we encounter a plug. If the last gap we saw
            // on a seg is unmarked, we will process this in process_background_segment_end.
            size_t free_obj_size_last_gap = 0;

            allow_fgc();
            uint8_t* end = background_next_end (seg, (i > max_generation));
            dprintf (3333, ("bgs: seg: %zx, [%zx, %zx[%zx", (size_t)seg,
                            (size_t)heap_segment_mem (seg),
                            (size_t)heap_segment_allocated (seg),
                            (size_t)heap_segment_background_allocated (seg)));

            while (o < end)
            {
                if (background_object_marked (o, TRUE))
                {
                    uint8_t* plug_start = o;
                    if (i > max_generation)
                    {
                        dprintf (2, ("uoh fr: [%p-%p[(%zd)", plug_end, plug_start, plug_start-plug_end));
                    }

                    thread_gap (plug_end, plug_start-plug_end, gen);
                    if (i == max_generation)
                    {
                        add_gen_free (max_generation, plug_start-plug_end);

#ifdef DOUBLY_LINKED_FL
                        if (free_obj_size_last_gap)
                        {
                            generation_free_obj_space (gen) -= free_obj_size_last_gap;
                            dprintf (3333, ("[h%d] LG: gen2FO-: %zd->%zd",
                                heap_number, free_obj_size_last_gap, generation_free_obj_space (gen)));

                            free_obj_size_last_gap = 0;
                        }
#endif //DOUBLY_LINKED_FL

                        fix_brick_to_highest (plug_end, plug_start);
                        // we need to fix the brick for the next plug here 'cause an FGC can
                        // happen and can't read a stale brick.
                        fix_brick_to_highest (plug_start, plug_start);
                    }

                    do
                    {
                        next_sweep_obj = o + Align (size (o), align_const);
                        current_num_objs++;
                        if (current_num_objs >= num_objs)
                        {
                            current_sweep_pos = next_sweep_obj;
                            allow_fgc();
                            current_num_objs = 0;
                        }
                        o = next_sweep_obj;
                    } while ((o < end) && background_object_marked(o, TRUE));

                    plug_end = o;
                    if (i == max_generation)
                    {
                        add_gen_plug (max_generation, plug_end-plug_start);
                        dd_survived_size (dd) += (plug_end - plug_start);
                    }
                    dprintf (3, ("bgs: plug [%zx, %zx[", (size_t)plug_start, (size_t)plug_end));
                }

                while ((o < end) && !background_object_marked (o, FALSE))
                {
                    size_t size_o = Align(size (o), align_const);
                    next_sweep_obj = o + size_o;

#ifdef DOUBLY_LINKED_FL
                    if ((i == max_generation) && !rebuild_maxgen_fl_p)
                    {
                        if (method_table (o) == g_gc_pFreeObjectMethodTable)
                        {
                            free_obj_size_last_gap += size_o;

                            if (is_on_free_list (o, size_o))
                            {
#ifdef MULTIPLE_HEAPS
                                assert (heap_of (o) == this);
#endif //MULTIPLE_HEAPS
                                generation_allocator (gen)->unlink_item_no_undo (o, size_o);
                                generation_free_list_space (gen) -= size_o;
                                assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);
                                generation_free_obj_space (gen) += size_o;

                                dprintf (3333, ("[h%d] gen2F-: %p->%p(%zd) FL: %zd",
                                    heap_number, o, (o + size_o), size_o,
                                    generation_free_list_space (gen)));
                                dprintf (3333, ("h%d: gen2FO+: %p(%zx)->%zd (g: %zd)",
                                    heap_number, o, size_o,
                                    generation_free_obj_space (gen),
                                    free_obj_size_last_gap));
                                remove_gen_free (max_generation, size_o);
                            }
                            else
                            {
                                // this was not on the free list so it was already part of
                                // free_obj_space, so no need to subtract from it. However,
                                // we do need to keep track in this gap's FO space.
                                dprintf (3333, ("h%d: gen2FO: %p(%zd)->%zd (g: %zd)",
                                    heap_number, o, size_o,
                                    generation_free_obj_space (gen), free_obj_size_last_gap));
                            }

                            dprintf (3333, ("h%d: total FO: %p->%p FL: %zd, FO: %zd (g: %zd)",
                                heap_number, plug_end, next_sweep_obj,
                                generation_free_list_space (gen),
                                generation_free_obj_space (gen),
                                free_obj_size_last_gap));
                        }
                    }
#endif //DOUBLY_LINKED_FL

                    current_num_objs++;
                    if (current_num_objs >= num_objs)
                    {
                        current_sweep_pos = plug_end;
                        dprintf (1234, ("f: swept till %p", current_sweep_pos));
                        allow_fgc();
                        current_num_objs = 0;
                    }

                    o = next_sweep_obj;
                }
            }

#ifdef DOUBLY_LINKED_FL
            next_seg = heap_segment_next (seg);
#else //DOUBLY_LINKED_FL
            if (i > max_generation)
            {
                next_seg = heap_segment_next (seg);
            }
            else
            {
                // For SOH segments we go backwards.
                next_seg = heap_segment_prev (gen_start_seg, seg);
            }
#endif //DOUBLY_LINKED_FL

            BOOL delete_p = FALSE;
            if (!heap_segment_read_only_p (seg))
            {
                if (i > max_generation)
                {
                    // we can treat all UOH segments as in the bgc domain
                    // regardless of whether we saw in bgc mark or not
                    // because we don't allow UOH allocations during bgc
                    // sweep anyway - the UOH segments can't change.
                    process_background_segment_end (seg, gen, plug_end,
                                                    start_seg, &delete_p, 0);
                }
                else
                {
                    assert (heap_segment_background_allocated (seg) != 0);
                    process_background_segment_end (seg, gen, plug_end,
                                                    start_seg, &delete_p, free_obj_size_last_gap);

#ifndef USE_REGIONS
                    assert (next_seg || !delete_p);
#endif //!USE_REGIONS
                }
            }

            heap_segment* saved_prev_seg = prev_seg;

            if (delete_p)
            {
                generation_delete_heap_segment (gen, seg, prev_seg, next_seg);
            }
            else
            {
                prev_seg = seg;
                dprintf (2, ("seg %p (%p) has been swept", seg, heap_segment_mem (seg)));
                seg->flags |= heap_segment_flags_swept;
                current_sweep_pos = end;
            }

            verify_soh_segment_list();

#ifdef DOUBLY_LINKED_FL
            while (next_seg && heap_segment_background_allocated (next_seg) == 0)
            {
                dprintf (2, ("[h%d] skip new %p ", heap_number, next_seg));
                next_seg = heap_segment_next (next_seg);
            }
#endif //DOUBLY_LINKED_FL

            dprintf (GTC_LOG, ("seg: %p(%p), next_seg: %p(%p), prev_seg: %p(%p), delete_p %d",
                seg, (seg ? heap_segment_mem (seg) : 0),
                next_seg, (next_seg ? heap_segment_mem (next_seg) : 0),
                saved_prev_seg, (saved_prev_seg ? heap_segment_mem (saved_prev_seg) : 0),
                (delete_p ? 1 : 0)));
            seg = next_seg;
        }

        generation_allocation_segment (gen) = heap_segment_rw (generation_start_segment (gen));
        _ASSERTE(generation_allocation_segment(gen) != NULL);

        if (i == max_generation)
        {
            dprintf (2, ("bgs: sweeping uoh objects"));
            concurrent_print_time_delta ("Swe SOH");
            FIRE_EVENT(BGC1stSweepEnd, 0);

            //block concurrent allocation for UOH objects
            enter_spin_lock (&more_space_lock_uoh);
            add_saved_spinlock_info (true, me_acquire, mt_bgc_uoh_sweep, msl_entered);

            concurrent_print_time_delta ("Swe UOH took msl");

            // We wait till all allocating threads are completely done.
            int spin_count = yp_spin_count_unit;
            while (uoh_alloc_thread_count)
            {
                spin_and_switch (spin_count, (uoh_alloc_thread_count == 0));
            }

            current_bgc_state = bgc_sweep_uoh;
        }
    }

    size_t total_soh_size = generation_sizes (generation_of (max_generation));
    size_t total_loh_size = generation_size (loh_generation);
    size_t total_poh_size = generation_size (poh_generation);

    dprintf (GTC_LOG, ("h%d: S: poh: %zd, loh: %zd, soh: %zd", heap_number, total_poh_size, total_loh_size, total_soh_size));

    dprintf (GTC_LOG, ("end of bgc sweep: gen2 FL: %zd, FO: %zd",
        generation_free_list_space (generation_of (max_generation)),
        generation_free_obj_space (generation_of (max_generation))));

    dprintf (GTC_LOG, ("h%d: end of bgc sweep: loh FL: %zd, FO: %zd",
        heap_number,
        generation_free_list_space (generation_of (loh_generation)),
        generation_free_obj_space (generation_of (loh_generation))));

    dprintf (GTC_LOG, ("h%d: end of bgc sweep: poh FL: %zd, FO: %zd",
        heap_number,
        generation_free_list_space (generation_of (poh_generation)),
        generation_free_obj_space (generation_of (poh_generation))));

    FIRE_EVENT(BGC2ndConEnd);
    concurrent_print_time_delta ("background sweep");

    heap_segment* reset_seg = heap_segment_rw (generation_start_segment (generation_of (max_generation)));
    _ASSERTE(reset_seg != NULL);

    while (reset_seg)
    {
        heap_segment_saved_bg_allocated (reset_seg) = heap_segment_background_allocated (reset_seg);
        heap_segment_background_allocated (reset_seg) = 0;
        reset_seg = heap_segment_next_rw (reset_seg);
    }

    // We calculate dynamic data here because if we wait till we signal the lh event,
    // the allocation thread can change the fragmentation and we may read an intermediate
    // value (which can be greater than the generation size). Plus by that time it won't
    // be accurate.
    compute_new_dynamic_data (max_generation);

    // We also need to adjust size_before for UOH allocations that occurred during sweeping.
    gc_history_per_heap* current_gc_data_per_heap = get_gc_data_per_heap();
    for (int i = uoh_start_generation; i < total_generation_count; i++)
    {
        assert(uoh_a_bgc_marking[i - uoh_start_generation] == 0);
        assert(uoh_a_no_bgc[i - uoh_start_generation] == 0);
        current_gc_data_per_heap->gen_data[i].size_before += uoh_a_bgc_planning[i - uoh_start_generation];
    }

#ifdef DOUBLY_LINKED_FL
    current_bgc_state = bgc_not_in_process;

    // We can have an FGC triggered before we set the global state to free
    // so we need to not have left over current_sweep_seg that point to
    // a segment that might've been deleted at the beginning of an FGC.
    current_sweep_seg = 0;
#endif //DOUBLY_LINKED_FL

    enable_preemptive ();

#ifdef MULTIPLE_HEAPS
    bgc_t_join.join(this, gc_join_set_state_free);
    if (bgc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
        // TODO: We are using this join just to set the state. Should
        // look into eliminating it - check to make sure things that use
        // this state can live with per heap state like should_check_bgc_mark.
        current_c_gc_state = c_gc_state_free;

#ifdef DYNAMIC_HEAP_COUNT
        update_total_soh_stable_size();
#endif //DYNAMIC_HEAP_COUNT

#ifdef BGC_SERVO_TUNING
        if (bgc_tuning::enable_fl_tuning)
        {
            enter_spin_lock (&gc_lock);
            bgc_tuning::record_and_adjust_bgc_end();
            leave_spin_lock (&gc_lock);
        }
#endif //BGC_SERVO_TUNING

#ifdef MULTIPLE_HEAPS
        dprintf(2, ("Starting BGC threads after background sweep phase"));
        bgc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    disable_preemptive (true);

    add_saved_spinlock_info (true, me_release, mt_bgc_uoh_sweep, msl_entered);
    leave_spin_lock (&more_space_lock_uoh);

    //dprintf (GTC_LOG, ("---- (GC%zu)End Background Sweep Phase ----", VolatileLoad(&settings.gc_index)));
    dprintf (GTC_LOG, ("---- (GC%zu)ESw ----", VolatileLoad(&settings.gc_index)));
}

#endif //BACKGROUND_GC

void gc_heap::mark_through_cards_for_uoh_objects (card_fn fn,
                                                  int gen_num,
                                                  BOOL relocating
                                                  CARD_MARKING_STEALING_ARG(gc_heap* hpt))
{
#ifdef USE_REGIONS
    uint8_t*      low               = 0;
#else
    uint8_t*      low               = gc_low;
#endif //USE_REGIONS
    size_t        end_card          = 0;
    generation*   oldest_gen        = generation_of (gen_num);
    heap_segment* seg               = heap_segment_rw (generation_start_segment (oldest_gen));

    _ASSERTE(seg != NULL);

    uint8_t*      beg               = get_uoh_start_object (seg, oldest_gen);
    uint8_t*      end               = heap_segment_allocated (seg);

    size_t  cg_pointers_found = 0;

    size_t  card_word_end = (card_of (align_on_card_word (end)) /
                             card_word_width);

    size_t      n_eph             = 0;
    size_t      n_gen             = 0;
    size_t      n_card_set        = 0;

#ifdef USE_REGIONS
    uint8_t*    next_boundary = 0;
    uint8_t*    nhigh         = 0;
#else
    uint8_t*    next_boundary = (relocating ?
                              generation_plan_allocation_start (generation_of (max_generation -1)) :
                              ephemeral_low);

    uint8_t*    nhigh         = (relocating ?
                              heap_segment_plan_allocated (ephemeral_heap_segment) :
                              ephemeral_high);
#endif //USE_REGIONS
    BOOL          foundp            = FALSE;
    uint8_t*      start_address     = 0;
    uint8_t*      limit             = 0;
    size_t        card              = card_of (beg);
    uint8_t*      o                 = beg;
#ifdef BACKGROUND_GC
    BOOL consider_bgc_mark_p        = FALSE;
    BOOL check_current_sweep_p      = FALSE;
    BOOL check_saved_sweep_p        = FALSE;
    should_check_bgc_mark (seg, &consider_bgc_mark_p, &check_current_sweep_p, &check_saved_sweep_p);
#endif //BACKGROUND_GC

    size_t total_cards_cleared = 0;

#ifdef FEATURE_CARD_MARKING_STEALING
    VOLATILE(uint32_t)* chunk_index = (VOLATILE(uint32_t)*) &(gen_num == loh_generation ?
        card_mark_chunk_index_loh :
        card_mark_chunk_index_poh);

    card_marking_enumerator card_mark_enumerator(seg, low, chunk_index);
    card_word_end = 0;
#endif // FEATURE_CARD_MARKING_STEALING

#ifdef USE_REGIONS
    int condemned_gen = settings.condemned_generation;
#else
    int condemned_gen = -1;
#endif //USE_REGIONS

    //dprintf(3,( "scanning large objects from %zx to %zx", (size_t)beg, (size_t)end));
    dprintf(3, ("CMl: %zx->%zx", (size_t)beg, (size_t)end));
    while (1)
    {
        if ((o < end) && (card_of(o) > card))
        {
            dprintf (3, ("Found %zd cg pointers", cg_pointers_found));
            if (cg_pointers_found == 0)
            {
                uint8_t* last_object_processed = o;
#ifdef FEATURE_CARD_MARKING_STEALING
                last_object_processed = min(limit, o);
#endif // FEATURE_CARD_MARKING_STEALING
                dprintf (3, (" Clearing cards [%zx, %zx[ ", (size_t)card_address(card), (size_t)last_object_processed));
                clear_cards (card, card_of((uint8_t*)last_object_processed));
                total_cards_cleared += (card_of((uint8_t*)last_object_processed) - card);
            }
            n_eph +=cg_pointers_found;
            cg_pointers_found = 0;
            card = card_of ((uint8_t*)o);
        }
        if ((o < end) &&(card >= end_card))
        {
#ifdef FEATURE_CARD_MARKING_STEALING
            // find another chunk with some cards set
            foundp = find_next_chunk(card_mark_enumerator, seg, n_card_set, start_address, limit, card, end_card, card_word_end);
#else // FEATURE_CARD_MARKING_STEALING
            foundp = find_card (card_table, card, card_word_end, end_card);
            if (foundp)
            {
                n_card_set+= end_card - card;
                start_address = max (beg, card_address (card));
            }
            limit = min (end, card_address (end_card));
#endif  // FEATURE_CARD_MARKING_STEALING
        }
        if ((!foundp) || (o >= end) || (card_address (card) >= end))
        {
            if ((foundp) && (cg_pointers_found == 0))
            {
                dprintf(3,(" Clearing cards [%zx, %zx[ ", (size_t)card_address(card),
                           (size_t)card_address(card+1)));
                clear_cards (card, card+1);
                total_cards_cleared += 1;
            }
            n_eph +=cg_pointers_found;
            cg_pointers_found = 0;
#ifdef FEATURE_CARD_MARKING_STEALING
            // we have decided to move to the next segment - make sure we exhaust the chunk enumerator for this segment
            card_mark_enumerator.exhaust_segment(seg);
#endif // FEATURE_CARD_MARKING_STEALING
            if ((seg = heap_segment_next_rw (seg)) != 0)
            {
#ifdef BACKGROUND_GC
                should_check_bgc_mark (seg, &consider_bgc_mark_p, &check_current_sweep_p, &check_saved_sweep_p);
#endif //BACKGROUND_GC
                beg = heap_segment_mem (seg);
                end = compute_next_end (seg, low);
#ifdef FEATURE_CARD_MARKING_STEALING
                card_word_end = 0;
#else // FEATURE_CARD_MARKING_STEALING
                card_word_end = card_of (align_on_card_word (end)) / card_word_width;
#endif // FEATURE_CARD_MARKING_STEALING
                card = card_of (beg);
                o  = beg;
                end_card = 0;
                continue;
            }
            else
            {
                break;
            }
        }

        assert (card_set_p (card));
        {
            dprintf(3,("card %zx: o: %zx, l: %zx[ ",
                       card, (size_t)o, (size_t)limit));

            assert (Align (size (o)) >= Align (min_obj_size));
            size_t s = size (o);
            uint8_t* next_o =  o + AlignQword (s);
            Prefetch (next_o);

            while (o < limit)
            {
                s = size (o);
                assert (Align (s) >= Align (min_obj_size));
                next_o =  o + AlignQword (s);
                Prefetch (next_o);

                dprintf (4, ("|%zx|", (size_t)o));
                if (next_o < start_address)
                {
                    goto end_object;
                }

#ifdef BACKGROUND_GC
                if (!fgc_should_consider_object (o, seg, consider_bgc_mark_p, check_current_sweep_p, check_saved_sweep_p))
                {
                    goto end_object;
                }
#endif //BACKGROUND_GC

#ifdef COLLECTIBLE_CLASS
                if (is_collectible(o))
                {
                    BOOL passed_end_card_p = FALSE;

                    if (card_of (o) > card)
                    {
                        passed_end_card_p = card_transition (o, end, card_word_end,
                            cg_pointers_found,
                            n_eph, n_card_set,
                            card, end_card,
                            foundp, start_address,
                            limit, total_cards_cleared
                            CARD_MARKING_STEALING_ARGS(card_mark_enumerator, seg, card_word_end));
                    }

                    if ((!passed_end_card_p || foundp) && (card_of (o) == card))
                    {
                        // card is valid and it covers the head of the object
                        if (fn == &gc_heap::relocate_address)
                        {
                            cg_pointers_found++;
                        }
                        else
                        {
                            uint8_t* class_obj = get_class_object (o);
                            mark_through_cards_helper (&class_obj, n_gen,
                                                       cg_pointers_found, fn,
                                                       nhigh, next_boundary,
                                                       condemned_gen, max_generation CARD_MARKING_STEALING_ARG(hpt));
                        }
                    }

                    if (passed_end_card_p)
                    {
                        if (foundp && (card_address (card) < next_o))
                        {
                            goto go_through_refs;
                        }
                        else
                        {
                            goto end_object;
                        }
                    }
                }

go_through_refs:
#endif //COLLECTIBLE_CLASS

                if (contain_pointers (o))
                {
                    dprintf(3,("Going through %zx", (size_t)o));

                    go_through_object (method_table(o), o, s, poo,
                                       start_address, use_start, (o + s),
                       {
                           if (card_of ((uint8_t*)poo) > card)
                           {
                                BOOL passed_end_card_p  = card_transition ((uint8_t*)poo, end,
                                        card_word_end,
                                        cg_pointers_found,
                                        n_eph, n_card_set,
                                        card, end_card,
                                        foundp, start_address,
                                        limit, total_cards_cleared
                                        CARD_MARKING_STEALING_ARGS(card_mark_enumerator, seg, card_word_end));

                                if (passed_end_card_p)
                                {
                                    if (foundp && (card_address (card) < next_o))
                                    {
                                        //new_start();
                                        {
                                            if (ppstop <= (uint8_t**)start_address)
                                            {break;}
                                            else if (poo < (uint8_t**)start_address)
                                            {poo = (uint8_t**)start_address;}
                                        }
                                    }
                                    else
                                    {
                                        goto end_object;
                                    }
                                }
                            }

                           mark_through_cards_helper (poo, n_gen,
                                                      cg_pointers_found, fn,
                                                      nhigh, next_boundary,
                                                      condemned_gen, max_generation CARD_MARKING_STEALING_ARG(hpt));
                       }
                        );
                }

            end_object:
                o = next_o;
            }

        }
    }

    // compute the efficiency ratio of the card table
    if (!relocating)
    {
#ifdef FEATURE_CARD_MARKING_STEALING
        Interlocked::ExchangeAddPtr(&n_eph_loh, n_eph);
        Interlocked::ExchangeAddPtr(&n_gen_loh, n_gen);
        dprintf (3, ("h%d marking h%d Mloh: cross: %zd, useful: %zd, cards set: %zd, cards cleared: %zd, ratio: %d",
            hpt->heap_number, heap_number, n_eph, n_gen, n_card_set, total_cards_cleared,
            (n_eph ? (int)(((float)n_gen / (float)n_eph) * 100) : 0)));
        dprintf (3, ("h%d marking h%d Mloh: total cross %zd, useful: %zd, running ratio: %d",
            hpt->heap_number, heap_number, (size_t)n_eph_loh, (size_t)n_gen_loh,
            (n_eph_loh ? (int)(((float)n_gen_loh / (float)n_eph_loh) * 100) : 0)));
#else
        generation_skip_ratio = min (((n_eph > MIN_LOH_CROSS_GEN_REFS) ?
            (int)(((float)n_gen / (float)n_eph) * 100) : 100),
            generation_skip_ratio);
        dprintf (3, ("marking h%d Mloh: cross: %zd, useful: %zd, cards cleared: %zd, cards set: %zd, ratio: %d",
            heap_number, n_eph, n_gen, total_cards_cleared, n_card_set, generation_skip_ratio));
#endif //FEATURE_CARD_MARKING_STEALING
    }
    else
    {
        dprintf (3, ("R: Mloh: cross: %zd, useful: %zd, cards set: %zd, ratio: %d",
             n_eph, n_gen, n_card_set, generation_skip_ratio));
    }
}

#ifdef USE_REGIONS
size_t gc_heap::get_mark_array_size (heap_segment* seg)
{
#ifdef BACKGROUND_GC
    if (seg->flags & heap_segment_flags_ma_committed)
    {
        uint32_t* mark_array_addr = mark_array;
        uint8_t* begin = get_start_address (seg);
        uint8_t* end = heap_segment_reserved (seg);
        size_t beg_word = mark_word_of (begin);
        size_t end_word = mark_word_of (align_on_mark_word (end));
        uint8_t* commit_start = align_lower_page ((uint8_t*)&mark_array_addr[beg_word]);
        uint8_t* commit_end = align_on_page ((uint8_t*)&mark_array_addr[end_word]);
        return (size_t)(commit_end - commit_start);
    }
#endif //BACKGROUND_GC
    return 0;
}

#endif //USE_REGIONS
