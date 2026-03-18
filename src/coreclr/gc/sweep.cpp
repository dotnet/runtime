// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_BASICFREEZE

inline
void gc_heap::seg_clear_mark_bits (heap_segment* seg)
{
    uint8_t* o = heap_segment_mem (seg);
    while (o < heap_segment_allocated (seg))
    {
        if (marked (o))
        {
            clear_marked (o);
        }
        o = o + Align (size (o));
    }
}

void gc_heap::sweep_ro_segments()
{
#ifndef USE_REGIONS
    if ((settings.condemned_generation == max_generation) && ro_segments_in_range)
    {
        heap_segment* seg = generation_start_segment (generation_of (max_generation));;

        while (seg)
        {
            if (!heap_segment_read_only_p (seg))
                break;

            if (heap_segment_in_range_p (seg))
            {
#ifdef BACKGROUND_GC
                if (settings.concurrent)
                {
                    seg_clear_mark_array_bits_soh (seg);
                }
                else
#endif //BACKGROUND_GC
                {
                    seg_clear_mark_bits (seg);
                }
            }
            seg = heap_segment_next (seg);
        }
    }
#endif //!USE_REGIONS
}

#endif //FEATURE_BASICFREEZE

void gc_heap::make_free_lists (int condemned_gen_number)
{
    //Promotion has to happen in sweep case.
    assert (settings.promotion);

    make_free_args args = {};
    int stop_gen_idx = get_stop_generation_index (condemned_gen_number);
    for (int i = condemned_gen_number; i >= stop_gen_idx; i--)
    {
        generation* condemned_gen = generation_of (i);
        heap_segment* current_heap_segment = get_start_segment (condemned_gen);

#ifdef USE_REGIONS
    if (!current_heap_segment)
        continue;
#endif //USE_REGIONS

        uint8_t* start_address = get_soh_start_object (current_heap_segment, condemned_gen);
        size_t current_brick = brick_of (start_address);

        _ASSERTE(current_heap_segment != NULL);

        uint8_t* end_address = heap_segment_allocated (current_heap_segment);
        size_t  end_brick = brick_of (end_address - 1);

        int current_gen_num = i;
#ifdef USE_REGIONS
        args.free_list_gen_number = (special_sweep_p ? current_gen_num : get_plan_gen_num (current_gen_num));
#else
        args.free_list_gen_number = get_plan_gen_num (current_gen_num);
#endif //USE_REGIONS
        args.free_list_gen = generation_of (args.free_list_gen_number);
        args.highest_plug = 0;

#ifdef USE_REGIONS
        dprintf (REGIONS_LOG, ("starting at gen%d %p -> %p", i, start_address, end_address));
#else
        args.current_gen_limit = (((current_gen_num == max_generation)) ?
                                  MAX_PTR :
                                  (generation_limit (args.free_list_gen_number)));
#endif //USE_REGIONS

#ifndef USE_REGIONS
        if ((start_address >= end_address) && (condemned_gen_number < max_generation))
        {
            break;
        }
#endif //!USE_REGIONS

        while (1)
        {
            if ((current_brick > end_brick))
            {
#ifndef USE_REGIONS
                if (args.current_gen_limit == MAX_PTR)
                {
                    //We had an empty segment
                    //need to allocate the generation start
                    generation* gen = generation_of (max_generation);

                    heap_segment* start_seg = heap_segment_rw (generation_start_segment (gen));

                    _ASSERTE(start_seg != NULL);

                    uint8_t* gap = heap_segment_mem (start_seg);

                    generation_allocation_start (gen) = gap;
                    heap_segment_allocated (start_seg) = gap + Align (min_obj_size);
                    make_unused_array (gap, Align (min_obj_size));
                    reset_allocation_pointers (gen, gap);
                    dprintf (3, ("Start segment empty, fixing generation start of %d to: %zx",
                                max_generation, (size_t)gap));
                    args.current_gen_limit = generation_limit (args.free_list_gen_number);
                }
#endif //!USE_REGIONS

                if (heap_segment_next_non_sip (current_heap_segment))
                {
                    current_heap_segment = heap_segment_next_non_sip (current_heap_segment);
                }
                else
                {
                    break;
                }

                current_brick = brick_of (heap_segment_mem (current_heap_segment));
                end_brick = brick_of (heap_segment_allocated (current_heap_segment)-1);
                continue;
            }
            {
                int brick_entry =  brick_table [ current_brick ];
                if ((brick_entry >= 0))
                {
                    make_free_list_in_brick (brick_address (current_brick) + brick_entry-1, &args);
                    dprintf(3,("Fixing brick entry %zx to %zx",
                            current_brick, (size_t)args.highest_plug));
                    set_brick (current_brick,
                            (args.highest_plug - brick_address (current_brick)));
                }
                else
                {
                    if ((brick_entry > -32768))
                    {
#ifdef _DEBUG
                        ptrdiff_t offset = brick_of (args.highest_plug) - current_brick;
                        if ((brick_entry != -32767) && (! ((offset == brick_entry))))
                        {
                            assert ((brick_entry == -1));
                        }
#endif //_DEBUG
                        //init to -1 for faster find_first_object
                        set_brick (current_brick, -1);
                    }
                }
            }
            current_brick++;
        }
    }

    {
#ifdef USE_REGIONS
        check_seg_gen_num (generation_allocation_segment (generation_of (max_generation)));

        thread_final_regions (false);

        generation* gen_gen0 = generation_of (0);
        ephemeral_heap_segment = generation_start_segment (gen_gen0);
        alloc_allocated = heap_segment_allocated (ephemeral_heap_segment);
#else //USE_REGIONS
        int bottom_gen = 0;
        args.free_list_gen_number--;
        while (args.free_list_gen_number >= bottom_gen)
        {
            uint8_t*  gap = 0;
            generation* gen2 = generation_of (args.free_list_gen_number);
            gap = allocate_at_end (Align(min_obj_size));
            generation_allocation_start (gen2) = gap;
            reset_allocation_pointers (gen2, gap);
            dprintf(3,("Fixing generation start of %d to: %zx",
                       args.free_list_gen_number, (size_t)gap));
            _ASSERTE(gap != NULL);
            make_unused_array (gap, Align (min_obj_size));

            args.free_list_gen_number--;
        }

        //reset the allocated size
        uint8_t* start2 = generation_allocation_start (youngest_generation);
        alloc_allocated = start2 + Align (size (start2));
#endif //USE_REGIONS
    }
}

void gc_heap::make_free_list_in_brick (uint8_t* tree, make_free_args* args)
{
    assert ((tree != NULL));
    {
        int  right_node = node_right_child (tree);
        int left_node = node_left_child (tree);
        args->highest_plug = 0;
        if (! (0 == tree))
        {
            if (! (0 == left_node))
            {
                make_free_list_in_brick (tree + left_node, args);
            }
            {
                uint8_t*  plug = tree;
                size_t  gap_size = node_gap_size (tree);
                uint8_t*  gap = (plug - gap_size);
                args->highest_plug = tree;
                dprintf (3,("plug: %p (highest p: %p), free %zx len %zd in %d",
                        plug, args->highest_plug, (size_t)gap, gap_size, args->free_list_gen_number));
#ifdef SHORT_PLUGS
                if (is_plug_padded (plug))
                {
                    dprintf (3, ("%p padded", plug));
                    clear_plug_padded (plug);
                }
#endif //SHORT_PLUGS

#ifdef DOUBLY_LINKED_FL
                // These 2 checks should really just be merged into one.
                if (is_plug_bgc_mark_bit_set (plug))
                {
                    dprintf (3333, ("cbgcm: %p", plug));
                    clear_plug_bgc_mark_bit (plug);
                }
                if (is_free_obj_in_compact_bit_set (plug))
                {
                    dprintf (3333, ("cfoc: %p", plug));
                    clear_free_obj_in_compact_bit (plug);
                }
#endif //DOUBLY_LINKED_FL

#ifndef USE_REGIONS
            gen_crossing:
                {
                    if ((args->current_gen_limit == MAX_PTR) ||
                        ((plug >= args->current_gen_limit) &&
                         ephemeral_pointer_p (plug)))
                    {
                        dprintf(3,(" Crossing Generation boundary at %zx",
                               (size_t)args->current_gen_limit));
                        if (!(args->current_gen_limit == MAX_PTR))
                        {
                            args->free_list_gen_number--;
                            args->free_list_gen = generation_of (args->free_list_gen_number);
                        }
                        dprintf(3,( " Fixing generation start of %d to: %zx",
                                args->free_list_gen_number, (size_t)gap));

                        reset_allocation_pointers (args->free_list_gen, gap);
                        args->current_gen_limit = generation_limit (args->free_list_gen_number);

                        if ((gap_size >= (2*Align (min_obj_size))))
                        {
                            dprintf(3,(" Splitting the gap in two %zd left",
                                   gap_size));
                            make_unused_array (gap, Align(min_obj_size));
                            gap_size = (gap_size - Align(min_obj_size));
                            gap = (gap + Align(min_obj_size));
                        }
                        else
                        {
                            make_unused_array (gap, gap_size);
                            gap_size = 0;
                        }
                        goto gen_crossing;
                    }
                }
#endif //!USE_REGIONS

                thread_gap (gap, gap_size, args->free_list_gen);
                add_gen_free (args->free_list_gen->gen_num, gap_size);
            }
            if (! (0 == right_node))
            {
                make_free_list_in_brick (tree + right_node, args);
            }
        }
    }
}

void gc_heap::thread_gap (uint8_t* gap_start, size_t size, generation*  gen)
{
#ifndef USE_REGIONS
    assert (generation_allocation_start (gen));
#endif

    if ((size > 0))
    {
#ifndef USE_REGIONS
        assert ((heap_segment_rw (generation_start_segment (gen)) != ephemeral_heap_segment) ||
                (gap_start > generation_allocation_start (gen)));
#endif //USE_REGIONS

        // The beginning of a segment gap is not aligned
        assert (size >= Align (min_obj_size));
        make_unused_array (gap_start, size,
                          (!settings.concurrent && (gen != youngest_generation)),
                          (gen->gen_num == max_generation));
        dprintf (3, ("fr: [%zx, %zx[", (size_t)gap_start, (size_t)gap_start+size));

        if ((size >= min_free_list))
        {
            generation_free_list_space (gen) += size;
            generation_allocator (gen)->thread_item (gap_start, size);
        }
        else
        {
            generation_free_obj_space (gen) += size;
        }
    }
}

void gc_heap::uoh_thread_gap_front (uint8_t* gap_start, size_t size, generation*  gen)
{
#ifndef USE_REGIONS
    assert (generation_allocation_start (gen));
#endif

    if (size >= min_free_list)
    {
        generation_free_list_space (gen) += size;
        generation_allocator (gen)->thread_item_front (gap_start, size);
    }
}

void gc_heap::make_unused_array (uint8_t* x, size_t size, BOOL clearp, BOOL resetp)
{
    dprintf (3, (ThreadStressLog::gcMakeUnusedArrayMsg(),
        (size_t)x, (size_t)(x+size)));
    assert (size >= Align (min_obj_size));

//#if defined (VERIFY_HEAP) && defined (BACKGROUND_GC)
//    check_batch_mark_array_bits (x, x+size);
//#endif //VERIFY_HEAP && BACKGROUND_GC

    if (resetp)
    {
#ifdef BGC_SERVO_TUNING
        // Don't do this for servo tuning because it makes it even harder to regulate WS.
        if (!(bgc_tuning::enable_fl_tuning && bgc_tuning::fl_tuning_triggered))
#endif //BGC_SERVO_TUNING
        {
            reset_memory (x, size);
        }
    }
    ((CObjectHeader*)x)->SetFree(size);

#ifdef HOST_64BIT

#if BIGENDIAN
#error "This won't work on big endian platforms"
#endif

    size_t size_as_object = (uint32_t)(size - free_object_base_size) + free_object_base_size;

    if (size_as_object < size)
    {
        //
        // If the size is more than 4GB, we need to create multiple objects because of
        // the Array::m_NumComponents is uint32_t and the high 32 bits of unused array
        // size is ignored in regular object size computation.
        //
        uint8_t * tmp = x + size_as_object;
        size_t remaining_size = size - size_as_object;

        while (remaining_size > UINT32_MAX)
        {
            // Make sure that there will be at least Align(min_obj_size) left
            size_t current_size = UINT32_MAX - get_alignment_constant (FALSE)
                - Align (min_obj_size, get_alignment_constant (FALSE));

            ((CObjectHeader*)tmp)->SetFree(current_size);

            remaining_size -= current_size;
            tmp += current_size;
        }

        ((CObjectHeader*)tmp)->SetFree(remaining_size);
    }
#endif

    if (clearp)
        clear_card_for_addresses (x, x + Align(size));
}

// Clear memory set by make_unused_array.
void gc_heap::clear_unused_array (uint8_t* x, size_t size)
{
    // Also clear the sync block
    *(((PTR_PTR)x)-1) = 0;

    ((CObjectHeader*)x)->UnsetFree();

#ifdef HOST_64BIT

#if BIGENDIAN
#error "This won't work on big endian platforms"
#endif

    // The memory could have been cleared in the meantime. We have to mirror the algorithm
    // from make_unused_array since we cannot depend on the object sizes in memory.
    size_t size_as_object = (uint32_t)(size - free_object_base_size) + free_object_base_size;

    if (size_as_object < size)
    {
        uint8_t * tmp = x + size_as_object;
        size_t remaining_size = size - size_as_object;

        while (remaining_size > UINT32_MAX)
        {
            size_t current_size = UINT32_MAX - get_alignment_constant (FALSE)
                - Align (min_obj_size, get_alignment_constant (FALSE));

            ((CObjectHeader*)tmp)->UnsetFree();

            remaining_size -= current_size;
            tmp += current_size;
        }

        ((CObjectHeader*)tmp)->UnsetFree();
    }
#else
    UNREFERENCED_PARAMETER(size);
#endif
}

void gc_heap::reset_memory (uint8_t* o, size_t sizeo)
{
    if (gc_heap::use_large_pages_p)
        return;

    if (sizeo > 128 * 1024)
    {
        // We cannot reset the memory for the useful part of a free object.
        size_t size_to_skip = min_free_list - plug_skew;

        size_t page_start = align_on_page ((size_t)(o + size_to_skip));
        size_t size = align_lower_page ((size_t)o + sizeo - size_to_skip - plug_skew) - page_start;
        // Note we need to compensate for an OS bug here. This bug would cause the MEM_RESET to fail
        // on write watched memory.
        if (reset_mm_p && gc_heap::dt_high_memory_load_p())
        {
#ifdef MULTIPLE_HEAPS
            bool unlock_p = true;
#else
            // We don't do unlock because there could be many processes using workstation GC and it's
            // bad perf to have many threads doing unlock at the same time.
            bool unlock_p = false;
#endif //MULTIPLE_HEAPS

            reset_mm_p = GCToOSInterface::VirtualReset((void*)page_start, size, unlock_p);
        }
    }
}

BOOL gc_heap::uoh_object_marked (uint8_t* o, BOOL clearp)
{
    BOOL m = FALSE;
    // It shouldn't be necessary to do these comparisons because this is only used for blocking
    // GCs and LOH segments cannot be out of range.
    if ((o >= lowest_address) && (o < highest_address))
    {
        if (marked (o))
        {
            if (clearp)
            {
                clear_marked (o);
                if (pinned (o))
                    clear_pinned(o);
            }
            m = TRUE;
        }
        else
            m = FALSE;
    }
    else
        m = TRUE;
    return m;
}

void gc_heap::sweep_uoh_objects (int gen_num)
{
    //this min value is for the sake of the dynamic tuning.
    //so we know that we are not starting even if we have no
    //survivors.
    generation* gen        = generation_of (gen_num);
    heap_segment* start_seg = heap_segment_rw (generation_start_segment (gen));

    _ASSERTE(start_seg != NULL);

    heap_segment* seg      = start_seg;
    heap_segment* prev_seg = 0;
    uint8_t* o             = get_uoh_start_object (seg, gen);

    uint8_t* plug_end         = o;
    uint8_t* plug_start       = o;

    generation_allocator (gen)->clear();
    generation_free_list_space (gen) = 0;
    generation_free_obj_space (gen) = 0;
    generation_free_list_allocated (gen) = 0;

    dprintf (3, ("sweeping uoh objects"));
    dprintf (3, ("seg: %zx, [%zx, %zx[, starting from %p",
                 (size_t)seg,
                 (size_t)heap_segment_mem (seg),
                 (size_t)heap_segment_allocated (seg),
                 o));

    while (1)
    {
        if (o >= heap_segment_allocated (seg))
        {
            heap_segment* next_seg = heap_segment_next (seg);
            //delete the empty segment if not the only one
            // REGIONS TODO: for regions we can get rid of the start_seg. Just need
            // to update start region accordingly.
            if ((plug_end == heap_segment_mem (seg)) &&
                (seg != start_seg) && !heap_segment_read_only_p (seg))
            {
                //prepare for deletion
                dprintf (3, ("Preparing empty large segment %zx", (size_t)seg));
                assert (prev_seg);
                heap_segment_next (prev_seg) = next_seg;
                heap_segment_next (seg) = freeable_uoh_segment;
                freeable_uoh_segment = seg;
#ifdef USE_REGIONS
                update_start_tail_regions (gen, seg, prev_seg, next_seg);
#endif //USE_REGIONS
            }
            else
            {
                if (!heap_segment_read_only_p (seg))
                {
                    dprintf (3, ("Trimming seg to %zx[", (size_t)plug_end));
                    heap_segment_allocated (seg) = plug_end;
                    decommit_heap_segment_pages (seg, 0);
                }
                prev_seg = seg;
            }
            seg = next_seg;
            if (seg == 0)
                break;
            else
            {
                o = heap_segment_mem (seg);
                plug_end = o;
                dprintf (3, ("seg: %zx, [%zx, %zx[", (size_t)seg,
                             (size_t)heap_segment_mem (seg),
                             (size_t)heap_segment_allocated (seg)));
#ifdef USE_REGIONS
                continue;
#endif //USE_REGIONS
            }
        }
        if (uoh_object_marked(o, TRUE))
        {
            plug_start = o;
            //everything between plug_end and plug_start is free
            thread_gap (plug_end, plug_start-plug_end, gen);

            BOOL m = TRUE;
            while (m)
            {
                o = o + AlignQword (size (o));
                if (o >= heap_segment_allocated (seg))
                {
                    break;
                }
                m = uoh_object_marked (o, TRUE);
            }
            plug_end = o;
            dprintf (3, ("plug [%zx, %zx[", (size_t)plug_start, (size_t)plug_end));
        }
        else
        {
            while (o < heap_segment_allocated (seg) && !uoh_object_marked(o, FALSE))
            {
                o = o + AlignQword (size (o));
            }
        }
    }

    generation_allocation_segment (gen) = heap_segment_rw (generation_start_segment (gen));

    _ASSERTE(generation_allocation_segment(gen) != NULL);
}

