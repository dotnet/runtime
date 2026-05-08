// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

void memcopy (uint8_t* dmem, uint8_t* smem, size_t size)
{
    const size_t sz4ptr = sizeof(PTR_PTR)*4;
    const size_t sz2ptr = sizeof(PTR_PTR)*2;
    const size_t sz1ptr = sizeof(PTR_PTR)*1;

    assert ((size & (sizeof (PTR_PTR)-1)) == 0);
    assert (sizeof(PTR_PTR) == DATA_ALIGNMENT);

    // copy in groups of four pointer sized things at a time
    if (size >= sz4ptr)
    {
        do
        {
            ((PTR_PTR)dmem)[0] = ((PTR_PTR)smem)[0];
            ((PTR_PTR)dmem)[1] = ((PTR_PTR)smem)[1];
            ((PTR_PTR)dmem)[2] = ((PTR_PTR)smem)[2];
            ((PTR_PTR)dmem)[3] = ((PTR_PTR)smem)[3];
            dmem += sz4ptr;
            smem += sz4ptr;
        }
        while ((size -= sz4ptr) >= sz4ptr);
    }

    // still two pointer sized things or more left to copy?
    if (size & sz2ptr)
    {
        ((PTR_PTR)dmem)[0] = ((PTR_PTR)smem)[0];
        ((PTR_PTR)dmem)[1] = ((PTR_PTR)smem)[1];
        dmem += sz2ptr;
        smem += sz2ptr;
    }

    // still one pointer sized thing left to copy?
    if (size & sz1ptr)
    {
        ((PTR_PTR)dmem)[0] = ((PTR_PTR)smem)[0];
    }
}

#ifdef USE_REGIONS
inline
bool gc_heap::should_check_brick_for_reloc (uint8_t* o)
{
    assert ((o >= g_gc_lowest_address) && (o < g_gc_highest_address));

    size_t skewed_basic_region_index = get_skewed_basic_region_index_for_address (o);

    // return true if the region is not SIP and the generation is <= condemned generation
    return (map_region_to_generation_skewed[skewed_basic_region_index] & (RI_SIP|RI_GEN_MASK)) <= settings.condemned_generation;
}

#endif //USE_REGIONS

#ifdef FEATURE_LOH_COMPACTION
void gc_heap::compact_loh()
{
    assert (loh_compaction_requested() || heap_hard_limit || conserve_mem_setting || (settings.reason == reason_induced_aggressive));

#ifdef FEATURE_EVENT_TRACE
    uint64_t start_time = 0, end_time;
    if (informational_event_enabled_p)
    {
        start_time = GetHighPrecisionTimeStamp();
    }
#endif //FEATURE_EVENT_TRACE

    generation* gen        = large_object_generation;
    heap_segment* start_seg = heap_segment_rw (generation_start_segment (gen));
    _ASSERTE(start_seg != NULL);
    heap_segment* seg      = start_seg;
    heap_segment* prev_seg = 0;
    uint8_t* o             = get_uoh_start_object (seg, gen);

    // We don't need to ever realloc gen3 start so don't touch it.
    uint8_t* free_space_start = o;
    uint8_t* free_space_end = o;
    generation_allocator (gen)->clear();
    generation_free_list_space (gen) = 0;
    generation_free_obj_space (gen) = 0;

    loh_pinned_queue_bos = 0;

    while (1)
    {
        if (o >= heap_segment_allocated (seg))
        {
            heap_segment* next_seg = heap_segment_next (seg);

            // REGIONS TODO: for regions we can get rid of the start_seg. Just need
            // to update start region accordingly.
            if ((heap_segment_plan_allocated (seg) == heap_segment_mem (seg)) &&
                (seg != start_seg) && !heap_segment_read_only_p (seg))
            {
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
                    // We grew the segment to accommodate allocations.
                    if (heap_segment_plan_allocated (seg) > heap_segment_allocated (seg))
                    {
                        if ((heap_segment_plan_allocated (seg) - plug_skew)  > heap_segment_used (seg))
                        {
                            heap_segment_used (seg) = heap_segment_plan_allocated (seg) - plug_skew;
                        }
                    }

                    heap_segment_allocated (seg) = heap_segment_plan_allocated (seg);
                    dprintf (3, ("Trimming seg to %p[", heap_segment_allocated (seg)));
                    decommit_heap_segment_pages (seg, 0);
                    dprintf (1236, ("CLOH: seg: %p, alloc: %p, used: %p, committed: %p",
                        seg,
                        heap_segment_allocated (seg),
                        heap_segment_used (seg),
                        heap_segment_committed (seg)));
                    //heap_segment_used (seg) = heap_segment_allocated (seg) - plug_skew;
                    dprintf (1236, ("CLOH: used is set to %p", heap_segment_used (seg)));
                }
                prev_seg = seg;
            }

            seg = next_seg;
            if (seg == 0)
                break;
            else
            {
                o = heap_segment_mem (seg);
            }
        }

        if (marked (o))
        {
            free_space_end = o;
            size_t size = AlignQword (size (o));

            size_t loh_pad;
            uint8_t* reloc = o;
            clear_marked (o);

            if (pinned (o))
            {
                // We are relying on the fact the pinned objects are always looked at in the same order
                // in plan phase and in compact phase.
                mark* m = loh_pinned_plug_of (loh_deque_pinned_plug());
                uint8_t* plug = pinned_plug (m);
                assert (plug == o);

                loh_pad = pinned_len (m);
                clear_pinned (o);
            }
            else
            {
                loh_pad = AlignQword (loh_padding_obj_size);

                reloc += loh_node_relocation_distance (o);
                gcmemcopy (reloc, o, size, TRUE);
            }

            thread_gap ((reloc - loh_pad), loh_pad, gen);

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
                o = o + AlignQword (size (o));
            }
        }
    }

#ifdef FEATURE_EVENT_TRACE
    if (informational_event_enabled_p)
    {
        end_time = GetHighPrecisionTimeStamp();
        loh_compact_info[heap_number].time_compact = limit_time_to_uint32 (end_time - start_time);
    }
#endif //FEATURE_EVENT_TRACE

    assert (loh_pinned_plug_que_empty_p());

    dprintf (1235, ("after GC LOH size: %zd, free list: %zd, free obj: %zd\n\n",
        generation_size (loh_generation),
        generation_free_list_space (gen),
        generation_free_obj_space (gen)));
}

#ifdef FEATURE_EVENT_TRACE
inline
void gc_heap::loh_reloc_survivor_helper (uint8_t** pval, size_t& total_refs, size_t& zero_refs)
{
    uint8_t* val = *pval;
    if (!val)
        zero_refs++;
    total_refs++;

    reloc_survivor_helper (pval);
}

#endif //FEATURE_EVENT_TRACE

void gc_heap::relocate_in_loh_compact()
{
    generation* gen        = large_object_generation;
    heap_segment* seg      = heap_segment_rw (generation_start_segment (gen));
    uint8_t* o              = get_uoh_start_object (seg, gen);

#ifdef FEATURE_EVENT_TRACE
    size_t total_refs = 0;
    size_t zero_refs = 0;
    uint64_t start_time = 0, end_time;
    if (informational_event_enabled_p)
    {
        start_time = GetHighPrecisionTimeStamp();
    }
#endif //FEATURE_EVENT_TRACE

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
            size_t size = AlignQword (size (o));

            check_class_object_demotion (o);
            if (contain_pointers (o))
            {
#ifdef FEATURE_EVENT_TRACE
                if (informational_event_enabled_p)
                {
                    go_through_object_nostart (method_table (o), o, size(o), pval,
                    {
                        loh_reloc_survivor_helper (pval, total_refs, zero_refs);
                    });
                }
                else
#endif //FEATURE_EVENT_TRACE
                {
                    go_through_object_nostart (method_table (o), o, size(o), pval,
                    {
                        reloc_survivor_helper (pval);
                    });
                }
            }
            o = o + size;
            if (o < heap_segment_allocated (seg))
            {
                assert (!marked (o));
            }
        }
        else
        {
            while (o < heap_segment_allocated (seg) && !marked (o))
            {
                o = o + AlignQword (size (o));
            }
        }
    }

#ifdef FEATURE_EVENT_TRACE
    if (informational_event_enabled_p)
    {
        end_time = GetHighPrecisionTimeStamp();
        loh_compact_info[heap_number].time_relocate = limit_time_to_uint32 (end_time - start_time);
        loh_compact_info[heap_number].total_refs = total_refs;
        loh_compact_info[heap_number].zero_refs = zero_refs;
    }
#endif //FEATURE_EVENT_TRACE

    dprintf (1235, ("after GC LOH size: %zd, free list: %zd, free obj: %zd\n\n",
        generation_size (loh_generation),
        generation_free_list_space (gen),
        generation_free_obj_space (gen)));
}

void gc_heap::walk_relocation_for_loh (void* profiling_context, record_surv_fn fn)
{
    generation* gen        = large_object_generation;
    heap_segment* seg      = heap_segment_rw (generation_start_segment (gen));
    uint8_t* o             = get_uoh_start_object (seg, gen);

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
            size_t size = AlignQword (size (o));

            ptrdiff_t reloc = loh_node_relocation_distance (o);

            STRESS_LOG_PLUG_MOVE(o, (o + size), -reloc);

            fn (o, (o + size), reloc, profiling_context, !!settings.compaction, false);

            o = o + size;
            if (o < heap_segment_allocated (seg))
            {
                assert (!marked (o));
            }
        }
        else
        {
            while (o < heap_segment_allocated (seg) && !marked (o))
            {
                o = o + AlignQword (size (o));
            }
        }
    }
}

BOOL gc_heap::loh_object_p (uint8_t* o)
{
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = gc_heap::g_heaps [0];
    int brick_entry = hp->brick_table[hp->brick_of (o)];
#else //MULTIPLE_HEAPS
    int brick_entry = brick_table[brick_of (o)];
#endif //MULTIPLE_HEAPS

    return (brick_entry == 0);
}

#endif //FEATURE_LOH_COMPACTION
#ifdef USE_REGIONS
heap_segment* gc_heap::relocate_advance_to_non_sip (heap_segment* region)
{
    THREAD_FROM_HEAP;

    heap_segment* current_region = region;
    dprintf (REGIONS_LOG, ("Relocate searching for next non SIP, starting from %p",
        (region ? heap_segment_mem (region) : 0)));

    while (current_region)
    {
        if (heap_segment_swept_in_plan (current_region))
        {
            int gen_num = heap_segment_gen_num (current_region);
            int plan_gen_num = heap_segment_plan_gen_num (current_region);
            bool use_sip_demotion = (plan_gen_num > get_plan_gen_num (gen_num));

            dprintf (REGIONS_LOG, ("region %p is SIP, relocating, gen %d, plan gen: %d(supposed to be %d) %s",
                heap_segment_mem (current_region), gen_num, plan_gen_num, get_plan_gen_num (gen_num),
                (use_sip_demotion ? "Sd" : "d")));
            uint8_t* x = heap_segment_mem (current_region);
            uint8_t* end = heap_segment_allocated (current_region);

            // For SIP regions, we go linearly in the region and relocate each object's references.
            while (x < end)
            {
                size_t s = size (x);
                assert (s > 0);
                uint8_t* next_obj = x + Align (s);
                Prefetch (next_obj);
                if (!(((CObjectHeader*)x)->IsFree()))
                {
                    //relocate_obj_helper (x, s);
                    if (contain_pointers (x))
                    {
                        dprintf (3, ("$%zx$", (size_t)x));

                        go_through_object_nostart (method_table(x), x, s, pval,
                        {
                            uint8_t* child = *pval;
                            //reloc_survivor_helper (pval);
                            relocate_address (pval THREAD_NUMBER_ARG);
                            if (use_sip_demotion)
                                check_demotion_helper_sip (pval, plan_gen_num, (uint8_t*)pval);
                            else
                                check_demotion_helper (pval, (uint8_t*)pval);

                            if (child)
                            {
                                dprintf (4444, ("SIP %p(%p)->%p->%p(%p)",
                                    x, (uint8_t*)pval, child, *pval, method_table (child)));
                            }
                        });
                    }
                    check_class_object_demotion (x);
                }
                x = next_obj;
            }
        }
        else
        {
            int gen_num = heap_segment_gen_num (current_region);
            int plan_gen_num = heap_segment_plan_gen_num (current_region);

            dprintf (REGIONS_LOG, ("region %p is not SIP, relocating, gen %d, plan gen: %d",
                heap_segment_mem (current_region), gen_num, plan_gen_num));
            return current_region;
        }

        current_region = heap_segment_next (current_region);
    }

    return 0;
}

#ifdef STRESS_REGIONS
void gc_heap::pin_by_gc (uint8_t* object)
{
    heap_segment* region = region_of (object);
    HndAssignHandleGC(pinning_handles_for_alloc[ph_index_per_heap], object);
    dprintf (REGIONS_LOG, ("h%d pinning object at %zx on eph seg %zx (ph#%d)",
        heap_number, object, heap_segment_mem (region), ph_index_per_heap));

    ph_index_per_heap++;
    if (ph_index_per_heap == PINNING_HANDLE_INITIAL_LENGTH)
    {
        ph_index_per_heap = 0;
    }
}

#endif //STRESS_REGIONS
#endif //USE_REGIONS

void gc_heap::relocate_address (uint8_t** pold_address THREAD_NUMBER_DCL)
{
    uint8_t* old_address = *pold_address;
#ifdef USE_REGIONS
    if (!is_in_gc_range (old_address) || !should_check_brick_for_reloc (old_address))
    {
        return;
    }
#else //USE_REGIONS
    if (!((old_address >= gc_low) && (old_address < gc_high)))
#ifdef MULTIPLE_HEAPS
    {
        UNREFERENCED_PARAMETER(thread);
        if (old_address == 0)
            return;
        gc_heap* hp = heap_of (old_address);
        if ((hp == this) ||
            !((old_address >= hp->gc_low) && (old_address < hp->gc_high)))
            return;
    }
#else //MULTIPLE_HEAPS
        return ;
#endif //MULTIPLE_HEAPS
#endif //USE_REGIONS
    // delta translates old_address into address_gc (old_address);
    size_t  brick = brick_of (old_address);
    int    brick_entry =  brick_table [ brick ];
    uint8_t*  new_address = old_address;
    if (! ((brick_entry == 0)))
    {
    retry:
        {
            while (brick_entry < 0)
            {
                brick = (brick + brick_entry);
                brick_entry =  brick_table [ brick ];
            }
            uint8_t* old_loc = old_address;

            uint8_t* node = tree_search ((brick_address (brick) + brick_entry-1),
                                      old_loc);
            if ((node <= old_loc))
                new_address = (old_address + node_relocation_distance (node));
            else
            {
                if (node_left_p (node))
                {
                    dprintf(3,(" L: %zx", (size_t)node));
                    new_address = (old_address +
                                   (node_relocation_distance (node) +
                                    node_gap_size (node)));
                }
                else
                {
                    brick = brick - 1;
                    brick_entry =  brick_table [ brick ];
                    goto retry;
                }
            }
        }

        dprintf (4, (ThreadStressLog::gcRelocateReferenceMsg(), pold_address, old_address, new_address));
        *pold_address = new_address;
        return;
    }

#ifdef FEATURE_LOH_COMPACTION
    if (settings.loh_compaction)
    {
        heap_segment* pSegment = seg_mapping_table_segment_of ((uint8_t*)old_address);
#ifdef USE_REGIONS
        // pSegment could be 0 for regions, see comment for is_in_condemned.
        if (!pSegment)
        {
            return;
        }
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
        if (heap_segment_heap (pSegment)->loh_compacted_p)
#else
        if (loh_compacted_p)
#endif
        {
            size_t flags = pSegment->flags;
            if ((flags & heap_segment_flags_loh)
#ifdef FEATURE_BASICFREEZE
                && !(flags & heap_segment_flags_readonly)
#endif
                )
            {
                new_address = old_address + loh_node_relocation_distance (old_address);
                dprintf (4, (ThreadStressLog::gcRelocateReferenceMsg(), pold_address, old_address, new_address));
                *pold_address = new_address;
            }
        }
    }
#endif //FEATURE_LOH_COMPACTION
}

inline void
gc_heap::check_class_object_demotion (uint8_t* obj)
{
#ifdef COLLECTIBLE_CLASS
    if (is_collectible(obj))
    {
        check_class_object_demotion_internal (obj);
    }
#else
    UNREFERENCED_PARAMETER(obj);
#endif //COLLECTIBLE_CLASS
}

#ifdef COLLECTIBLE_CLASS
NOINLINE void
gc_heap::check_class_object_demotion_internal (uint8_t* obj)
{
    if (settings.demotion)
    {
#ifdef MULTIPLE_HEAPS
        // We set the card without checking the demotion range 'cause at this point
        // the handle that points to the loader allocator object may or may not have
        // been relocated by other GC threads.
        set_card (card_of (obj));
#else
        THREAD_FROM_HEAP;
        uint8_t* class_obj = get_class_object (obj);
        dprintf (3, ("%p: got classobj %p", obj, class_obj));
        uint8_t* temp_class_obj = class_obj;
        uint8_t** temp = &temp_class_obj;
        relocate_address (temp THREAD_NUMBER_ARG);

        check_demotion_helper (temp, obj);
#endif //MULTIPLE_HEAPS
    }
}

#endif //COLLECTIBLE_CLASS

inline void
gc_heap::check_demotion_helper (uint8_t** pval, uint8_t* parent_obj)
{
#ifdef USE_REGIONS
    uint8_t* child_object = *pval;
    if (!is_in_heap_range (child_object))
        return;
    int child_object_plan_gen = get_region_plan_gen_num (child_object);
    bool child_obj_demoted_p = is_region_demoted (child_object);

    if (child_obj_demoted_p)
    {
        set_card (card_of (parent_obj));
    }

    dprintf (3, ("SC %d (%s)", child_object_plan_gen, (child_obj_demoted_p ? "D" : "ND")));
#else //USE_REGIONS
    // detect if we are demoting an object
    if ((*pval < demotion_high) &&
        (*pval >= demotion_low))
    {
        dprintf(3, ("setting card %zx:%zx",
                    card_of((uint8_t*)pval),
                    (size_t)pval));

        set_card (card_of (parent_obj));
    }
#ifdef MULTIPLE_HEAPS
    else if (settings.demotion)
    {
        dprintf (4, ("Demotion active, computing heap_of object"));
        gc_heap* hp = heap_of (*pval);
        if ((*pval < hp->demotion_high) &&
            (*pval >= hp->demotion_low))
        {
            dprintf(3, ("setting card %zx:%zx",
                        card_of((uint8_t*)pval),
                        (size_t)pval));

            set_card (card_of (parent_obj));
        }
    }
#endif //MULTIPLE_HEAPS
#endif //USE_REGIONS
}

inline void
gc_heap::reloc_survivor_helper (uint8_t** pval)
{
    THREAD_FROM_HEAP;
    relocate_address (pval THREAD_NUMBER_ARG);

    check_demotion_helper (pval, (uint8_t*)pval);
}

inline void
gc_heap::relocate_obj_helper (uint8_t* x, size_t s)
{
    THREAD_FROM_HEAP;
    if (contain_pointers (x))
    {
        dprintf (3, ("o$%zx$", (size_t)x));

        go_through_object_nostart (method_table(x), x, s, pval,
                            {
                                uint8_t* child = *pval;
                                reloc_survivor_helper (pval);
                                if (child)
                                {
                                    dprintf (3, ("%p->%p->%p", (uint8_t*)pval, child, *pval));
                                }
                            });

    }
    check_class_object_demotion (x);
}

inline
void gc_heap::reloc_ref_in_shortened_obj (uint8_t** address_to_set_card, uint8_t** address_to_reloc)
{
    THREAD_FROM_HEAP;

    uint8_t* old_val = (address_to_reloc ? *address_to_reloc : 0);
    relocate_address (address_to_reloc THREAD_NUMBER_ARG);
    if (address_to_reloc)
    {
        dprintf (3, ("SR %p: %p->%p", (uint8_t*)address_to_reloc, old_val, *address_to_reloc));
    }

    check_demotion_helper (address_to_reloc, (uint8_t*)address_to_set_card);
}

void gc_heap::relocate_pre_plug_info (mark* pinned_plug_entry)
{
    THREAD_FROM_HEAP;
    uint8_t* plug = pinned_plug (pinned_plug_entry);
    uint8_t* pre_plug_start = plug - sizeof (plug_and_gap);
    // Note that we need to add one ptr size here otherwise we may not be able to find the relocated
    // address. Consider this scenario:
    // gen1 start | 3-ptr sized NP | PP
    // 0          | 0x18           | 0x30
    // If we are asking for the reloc address of 0x10 we will AV in relocate_address because
    // the first plug we saw in the brick is 0x18 which means 0x10 will cause us to go back a brick
    // which is 0, and then we'll AV in tree_search when we try to do node_right_child (tree).
    pre_plug_start += sizeof (uint8_t*);
    uint8_t** old_address = &pre_plug_start;

    uint8_t* old_val = (old_address ? *old_address : 0);
    relocate_address (old_address THREAD_NUMBER_ARG);
    if (old_address)
    {
        dprintf (3, ("PreR %p: %p->%p, set reloc: %p",
            (uint8_t*)old_address, old_val, *old_address, (pre_plug_start - sizeof (uint8_t*))));
    }

    pinned_plug_entry->set_pre_plug_info_reloc_start (pre_plug_start - sizeof (uint8_t*));
}

inline
void gc_heap::relocate_shortened_obj_helper (uint8_t* x, size_t s, uint8_t* end, mark* pinned_plug_entry, BOOL is_pinned)
{
    THREAD_FROM_HEAP;
    uint8_t* plug = pinned_plug (pinned_plug_entry);

    if (!is_pinned)
    {
        //// Temporary - we just wanna make sure we are doing things right when padding is needed.
        //if ((x + s) < plug)
        //{
        //    dprintf (3, ("obj %zx needed padding: end %zx is %d bytes from pinned obj %zx",
        //        x, (x + s), (plug- (x + s)), plug));
        //    GCToOSInterface::DebugBreak();
        //}

        relocate_pre_plug_info (pinned_plug_entry);
    }

    verify_pins_with_post_plug_info("after relocate_pre_plug_info");

    uint8_t* saved_plug_info_start = 0;
    uint8_t** saved_info_to_relocate = 0;

    if (is_pinned)
    {
        saved_plug_info_start = (uint8_t*)(pinned_plug_entry->get_post_plug_info_start());
        saved_info_to_relocate = (uint8_t**)(pinned_plug_entry->get_post_plug_reloc_info());
    }
    else
    {
        saved_plug_info_start = (plug - sizeof (plug_and_gap));
        saved_info_to_relocate = (uint8_t**)(pinned_plug_entry->get_pre_plug_reloc_info());
    }

    uint8_t** current_saved_info_to_relocate = 0;
    uint8_t* child = 0;

    dprintf (3, ("x: %p, pp: %p, end: %p", x, plug, end));

    if (contain_pointers (x))
    {
        dprintf (3,("s$%zx$", (size_t)x));

        go_through_object_nostart (method_table(x), x, s, pval,
        {
            dprintf (3, ("obj %p, member: %p->%p", x, (uint8_t*)pval, *pval));

            if ((uint8_t*)pval >= end)
            {
                current_saved_info_to_relocate = saved_info_to_relocate + ((uint8_t*)pval - saved_plug_info_start) / sizeof (uint8_t**);
                child = *current_saved_info_to_relocate;
                reloc_ref_in_shortened_obj (pval, current_saved_info_to_relocate);
                dprintf (3, ("last part: R-%p(saved: %p)->%p ->%p",
                    (uint8_t*)pval, current_saved_info_to_relocate, child, *current_saved_info_to_relocate));
            }
            else
            {
                reloc_survivor_helper (pval);
            }
        });
    }

    check_class_object_demotion (x);
}

void gc_heap::relocate_survivor_helper (uint8_t* plug, uint8_t* plug_end)
{
    uint8_t*  x = plug;
    while (x < plug_end)
    {
        size_t s = size (x);
        uint8_t* next_obj = x + Align (s);
        Prefetch (next_obj);
        relocate_obj_helper (x, s);
        assert (s > 0);
        x = next_obj;
    }
}

// if we expanded, right now we are not handling it as We are not saving the new reloc info.
void gc_heap::verify_pins_with_post_plug_info (const char* msg)
{
#if defined (_DEBUG) && defined (VERIFY_HEAP)
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
    {
        if (!verify_pinned_queue_p)
            return;

        if (settings.heap_expansion)
            return;

        for (size_t i = 0; i < mark_stack_tos; i++)
        {
            mark& m = mark_stack_array[i];

            mark* pinned_plug_entry = pinned_plug_of(i);

            if (pinned_plug_entry->has_post_plug_info() &&
                pinned_plug_entry->post_short_p() &&
                (pinned_plug_entry->saved_post_plug_debug.gap != 1))
            {
                uint8_t* next_obj = pinned_plug_entry->get_post_plug_info_start() + sizeof (plug_and_gap);
                // object after pin
                dprintf (3, ("OFP: %p, G: %zx, R: %zx, LC: %d, RC: %d",
                    next_obj, node_gap_size (next_obj), node_relocation_distance (next_obj),
                    (int)node_left_child (next_obj), (int)node_right_child (next_obj)));

                size_t* post_plug_debug = (size_t*)(&m.saved_post_plug_debug);

                if (node_gap_size (next_obj) != *post_plug_debug)
                {
                    dprintf (1, ("obj: %p gap should be %zx but it is %zx",
                        next_obj, *post_plug_debug, (size_t)(node_gap_size (next_obj))));
                    FATAL_GC_ERROR();
                }
                post_plug_debug++;
                // can't do node_relocation_distance here as it clears the left bit.
                //if (node_relocation_distance (next_obj) != *post_plug_debug)
                if (*((size_t*)(next_obj - 3 * sizeof (size_t))) != *post_plug_debug)
                {
                    dprintf (1, ("obj: %p reloc should be %zx but it is %zx",
                        next_obj, *post_plug_debug, (size_t)(node_relocation_distance (next_obj))));
                    FATAL_GC_ERROR();
                }
                if (node_left_child (next_obj) > 0)
                {
                    dprintf (1, ("obj: %p, vLC: %d\n", next_obj, (int)(node_left_child (next_obj))));
                    FATAL_GC_ERROR();
                }
            }
        }

        dprintf (3, ("%s verified", msg));
    }
#else
    UNREFERENCED_PARAMETER(msg);
#endif // _DEBUG && VERIFY_HEAP
}

void gc_heap::relocate_shortened_survivor_helper (uint8_t* plug, uint8_t* plug_end, mark* pinned_plug_entry)
{
    uint8_t*  x = plug;
    uint8_t* p_plug = pinned_plug (pinned_plug_entry);
    BOOL is_pinned = (plug == p_plug);
    BOOL check_short_obj_p = (is_pinned ? pinned_plug_entry->post_short_p() : pinned_plug_entry->pre_short_p());

    plug_end += sizeof (gap_reloc_pair);

    //dprintf (3, ("%s %p is shortened, and last object %s overwritten", (is_pinned ? "PP" : "NP"), plug, (check_short_obj_p ? "is" : "is not")));
    dprintf (3, ("%s %p-%p short, LO: %s OW", (is_pinned ? "PP" : "NP"), plug, plug_end, (check_short_obj_p ? "is" : "is not")));

    verify_pins_with_post_plug_info("begin reloc short surv");

    while (x < plug_end)
    {
        if (check_short_obj_p && ((DWORD)(plug_end - x) < (DWORD)min_pre_pin_obj_size))
        {
            dprintf (3, ("last obj %p is short", x));

            if (is_pinned)
            {
#ifdef COLLECTIBLE_CLASS
                if (pinned_plug_entry->post_short_collectible_p())
                    unconditional_set_card_collectible (x);
#endif //COLLECTIBLE_CLASS

                // Relocate the saved references based on bits set.
                uint8_t** saved_plug_info_start = (uint8_t**)(pinned_plug_entry->get_post_plug_info_start());
                uint8_t** saved_info_to_relocate = (uint8_t**)(pinned_plug_entry->get_post_plug_reloc_info());
                for (size_t i = 0; i < pinned_plug_entry->get_max_short_bits(); i++)
                {
                    if (pinned_plug_entry->post_short_bit_p (i))
                    {
                        reloc_ref_in_shortened_obj ((saved_plug_info_start + i), (saved_info_to_relocate + i));
                    }
                }
            }
            else
            {
#ifdef COLLECTIBLE_CLASS
                if (pinned_plug_entry->pre_short_collectible_p())
                    unconditional_set_card_collectible (x);
#endif //COLLECTIBLE_CLASS

                relocate_pre_plug_info (pinned_plug_entry);

                // Relocate the saved references based on bits set.
                uint8_t** saved_plug_info_start = (uint8_t**)(p_plug - sizeof (plug_and_gap));
                uint8_t** saved_info_to_relocate = (uint8_t**)(pinned_plug_entry->get_pre_plug_reloc_info());
                for (size_t i = 0; i < pinned_plug_entry->get_max_short_bits(); i++)
                {
                    if (pinned_plug_entry->pre_short_bit_p (i))
                    {
                        reloc_ref_in_shortened_obj ((saved_plug_info_start + i), (saved_info_to_relocate + i));
                    }
                }
            }

            break;
        }

        size_t s = size (x);
        uint8_t* next_obj = x + Align (s);
        Prefetch (next_obj);

        if (next_obj >= plug_end)
        {
            dprintf (3, ("object %p is at the end of the plug %p->%p",
                next_obj, plug, plug_end));

            verify_pins_with_post_plug_info("before reloc short obj");

            relocate_shortened_obj_helper (x, s, (x + Align (s) - sizeof (plug_and_gap)), pinned_plug_entry, is_pinned);
        }
        else
        {
            relocate_obj_helper (x, s);
        }

        assert (s > 0);
        x = next_obj;
    }

    verify_pins_with_post_plug_info("end reloc short surv");
}

void gc_heap::relocate_survivors_in_plug (uint8_t* plug, uint8_t* plug_end,
                                          BOOL check_last_object_p,
                                          mark* pinned_plug_entry)
{
    dprintf (3,("RP: [%zx(%zx->%zx),%zx(%zx->%zx)[",
        (size_t)plug, brick_of (plug), (size_t)brick_table[brick_of (plug)],
        (size_t)plug_end, brick_of (plug_end), (size_t)brick_table[brick_of (plug_end)]));

    if (check_last_object_p)
    {
        relocate_shortened_survivor_helper (plug, plug_end, pinned_plug_entry);
    }
    else
    {
        relocate_survivor_helper (plug, plug_end);
    }
}

void gc_heap::relocate_survivors_in_brick (uint8_t* tree, relocate_args* args)
{
    assert ((tree != NULL));

    dprintf (3, ("tree: %p, args->last_plug: %p, left: %p, right: %p, gap(t): %zx",
        tree, args->last_plug,
        (tree + node_left_child (tree)),
        (tree + node_right_child (tree)),
        node_gap_size (tree)));

    if (node_left_child (tree))
    {
        relocate_survivors_in_brick (tree + node_left_child (tree), args);
    }
    {
        uint8_t*  plug = tree;
        BOOL   has_post_plug_info_p = FALSE;
        BOOL   has_pre_plug_info_p = FALSE;

        if (tree == oldest_pinned_plug)
        {
            args->pinned_plug_entry = get_oldest_pinned_entry (&has_pre_plug_info_p,
                                                               &has_post_plug_info_p);
            assert (tree == pinned_plug (args->pinned_plug_entry));

            dprintf (3, ("tree is the oldest pin: %p", tree));
        }
        if (args->last_plug)
        {
            size_t  gap_size = node_gap_size (tree);
            uint8_t*  gap = (plug - gap_size);
            dprintf (3, ("tree: %p, gap: %p (%zx)", tree, gap, gap_size));
            assert (gap_size >= Align (min_obj_size));
            uint8_t*  last_plug_end = gap;

            BOOL check_last_object_p = (args->is_shortened || has_pre_plug_info_p);

            {
                relocate_survivors_in_plug (args->last_plug, last_plug_end, check_last_object_p, args->pinned_plug_entry);
            }
        }
        else
        {
            assert (!has_pre_plug_info_p);
        }

        args->last_plug = plug;
        args->is_shortened = has_post_plug_info_p;
        if (has_post_plug_info_p)
        {
            dprintf (3, ("setting %p as shortened", plug));
        }
        dprintf (3, ("last_plug: %p(shortened: %d)", plug, (args->is_shortened ? 1 : 0)));
    }
    if (node_right_child (tree))
    {
        relocate_survivors_in_brick (tree + node_right_child (tree), args);
    }
}

inline
void gc_heap::update_oldest_pinned_plug()
{
    oldest_pinned_plug = (pinned_plug_que_empty_p() ? 0 : pinned_plug (oldest_pin()));
}

heap_segment* gc_heap::get_start_segment (generation* gen)
{
    heap_segment* start_heap_segment = heap_segment_rw (generation_start_segment (gen));
#ifdef USE_REGIONS
    heap_segment* current_heap_segment = heap_segment_non_sip (start_heap_segment);
    if (current_heap_segment != start_heap_segment)
    {
        dprintf (REGIONS_LOG, ("h%d skipped gen%d SIP regions, start %p->%p",
            heap_number,
            (current_heap_segment ? heap_segment_gen_num (current_heap_segment) : -1),
            heap_segment_mem (start_heap_segment),
            (current_heap_segment ? heap_segment_mem (current_heap_segment) : 0)));
    }
    start_heap_segment = current_heap_segment;
#endif //USE_REGIONS

    return start_heap_segment;
}

void gc_heap::relocate_survivors (int condemned_gen_number,
                                  uint8_t* first_condemned_address)
{
    reset_pinned_queue_bos();
    update_oldest_pinned_plug();

    int stop_gen_idx = get_stop_generation_index (condemned_gen_number);

#ifndef USE_REGIONS
    assert (first_condemned_address == generation_allocation_start (generation_of (condemned_gen_number)));
#endif //!USE_REGIONS

    for (int i = condemned_gen_number; i >= stop_gen_idx; i--)
    {
        generation* condemned_gen = generation_of (i);
        heap_segment* current_heap_segment = heap_segment_rw (generation_start_segment (condemned_gen));
#ifdef USE_REGIONS
        current_heap_segment = relocate_advance_to_non_sip (current_heap_segment);
        if (!current_heap_segment)
            continue;
#endif //USE_REGIONS
        uint8_t*  start_address = get_soh_start_object (current_heap_segment, condemned_gen);
        size_t  current_brick = brick_of (start_address);

        _ASSERTE(current_heap_segment != NULL);

        uint8_t*  end_address = heap_segment_allocated (current_heap_segment);

        size_t  end_brick = brick_of (end_address - 1);
        relocate_args args;
        args.is_shortened = FALSE;
        args.pinned_plug_entry = 0;
        args.last_plug = 0;

        while (1)
        {
            if (current_brick > end_brick)
            {
                if (args.last_plug)
                {
                    {
                        assert (!(args.is_shortened));
                        relocate_survivors_in_plug (args.last_plug,
                                                    heap_segment_allocated (current_heap_segment),
                                                    args.is_shortened,
                                                    args.pinned_plug_entry);
                    }

                    args.last_plug = 0;
                }

                heap_segment* next_heap_segment = heap_segment_next (current_heap_segment);
                if (next_heap_segment)
                {
#ifdef USE_REGIONS
                    next_heap_segment = relocate_advance_to_non_sip (next_heap_segment);
#endif //USE_REGIONS
                    if (next_heap_segment)
                    {
                        current_heap_segment = next_heap_segment;
                        current_brick = brick_of (heap_segment_mem (current_heap_segment));
                        end_brick = brick_of (heap_segment_allocated (current_heap_segment)-1);
                        continue;
                    }
                    else
                        break;
                }
                else
                {
                    break;
                }
            }
            {
                int brick_entry =  brick_table [ current_brick ];

                if (brick_entry >= 0)
                {
                    relocate_survivors_in_brick (brick_address (current_brick) +
                                                brick_entry -1,
                                                &args);
                }
            }
            current_brick++;
        }
    }
}

void gc_heap::walk_plug (uint8_t* plug, size_t size, BOOL check_last_object_p, walk_relocate_args* args)
{
    if (check_last_object_p)
    {
        size += sizeof (gap_reloc_pair);
        mark* entry = args->pinned_plug_entry;

        if (args->is_shortened)
        {
            assert (entry->has_post_plug_info());
            entry->swap_post_plug_and_saved_for_profiler();
        }
        else
        {
            assert (entry->has_pre_plug_info());
            entry->swap_pre_plug_and_saved_for_profiler();
        }
    }

    ptrdiff_t last_plug_relocation = node_relocation_distance (plug);
    STRESS_LOG_PLUG_MOVE(plug, (plug + size), -last_plug_relocation);
    ptrdiff_t reloc = settings.compaction ? last_plug_relocation : 0;

    (args->fn) (plug, (plug + size), reloc, args->profiling_context, !!settings.compaction, false);

    if (check_last_object_p)
    {
        mark* entry = args->pinned_plug_entry;

        if (args->is_shortened)
        {
            entry->swap_post_plug_and_saved_for_profiler();
        }
        else
        {
            entry->swap_pre_plug_and_saved_for_profiler();
        }
    }
}

void gc_heap::walk_relocation_in_brick (uint8_t* tree, walk_relocate_args* args)
{
    assert ((tree != NULL));
    if (node_left_child (tree))
    {
        walk_relocation_in_brick (tree + node_left_child (tree), args);
    }

    uint8_t*  plug = tree;
    BOOL   has_pre_plug_info_p = FALSE;
    BOOL   has_post_plug_info_p = FALSE;

    if (tree == oldest_pinned_plug)
    {
        args->pinned_plug_entry = get_oldest_pinned_entry (&has_pre_plug_info_p,
                                                           &has_post_plug_info_p);
        assert (tree == pinned_plug (args->pinned_plug_entry));
    }

    if (args->last_plug != 0)
    {
        size_t gap_size = node_gap_size (tree);
        uint8_t*  gap = (plug - gap_size);
        uint8_t*  last_plug_end = gap;
        size_t last_plug_size = (last_plug_end - args->last_plug);
        dprintf (3, ("tree: %p, last_plug: %p, gap: %p(%zx), last_plug_end: %p, size: %zx",
            tree, args->last_plug, gap, gap_size, last_plug_end, last_plug_size));

        BOOL check_last_object_p = (args->is_shortened || has_pre_plug_info_p);
        if (!check_last_object_p)
        {
            assert (last_plug_size >= Align (min_obj_size));
        }

        walk_plug (args->last_plug, last_plug_size, check_last_object_p, args);
    }
    else
    {
        assert (!has_pre_plug_info_p);
    }

    dprintf (3, ("set args last plug to plug: %p", plug));
    args->last_plug = plug;
    args->is_shortened = has_post_plug_info_p;

    if (node_right_child (tree))
    {
        walk_relocation_in_brick (tree + node_right_child (tree), args);
    }
}

void gc_heap::walk_relocation (void* profiling_context, record_surv_fn fn)
{
    int condemned_gen_number = settings.condemned_generation;
    int stop_gen_idx = get_stop_generation_index (condemned_gen_number);

    reset_pinned_queue_bos();
    update_oldest_pinned_plug();

    for (int i = condemned_gen_number; i >= stop_gen_idx; i--)
    {
        generation* condemned_gen = generation_of (i);
        heap_segment*  current_heap_segment = heap_segment_rw (generation_start_segment (condemned_gen));
#ifdef USE_REGIONS
        current_heap_segment = walk_relocation_sip (current_heap_segment, profiling_context, fn);
        if (!current_heap_segment)
            continue;
#endif // USE_REGIONS
        uint8_t*  start_address = get_soh_start_object (current_heap_segment, condemned_gen);
        size_t  current_brick = brick_of (start_address);

        _ASSERTE(current_heap_segment != NULL);
        size_t end_brick = brick_of (heap_segment_allocated (current_heap_segment)-1);
        walk_relocate_args args;
        args.is_shortened = FALSE;
        args.pinned_plug_entry = 0;
        args.last_plug = 0;
        args.profiling_context = profiling_context;
        args.fn = fn;

        while (1)
        {
            if (current_brick > end_brick)
            {
                if (args.last_plug)
                {
                    walk_plug (args.last_plug,
                            (heap_segment_allocated (current_heap_segment) - args.last_plug),
                            args.is_shortened,
                            &args);
                    args.last_plug = 0;
                }
                current_heap_segment = heap_segment_next_rw (current_heap_segment);
#ifdef USE_REGIONS
                current_heap_segment = walk_relocation_sip (current_heap_segment, profiling_context, fn);
#endif // USE_REGIONS
                if (current_heap_segment)
                {
                    current_brick = brick_of (heap_segment_mem (current_heap_segment));
                    end_brick = brick_of (heap_segment_allocated (current_heap_segment)-1);
                    continue;
                }
                else
                {
                    break;
                }
            }
            {
                int brick_entry =  brick_table [ current_brick ];
                if (brick_entry >= 0)
                {
                    walk_relocation_in_brick (brick_address (current_brick) +
                                            brick_entry - 1,
                                            &args);
                }
            }
            current_brick++;
        }
    }
}

#ifdef USE_REGIONS
heap_segment* gc_heap::walk_relocation_sip (heap_segment* current_heap_segment, void* profiling_context, record_surv_fn fn)
{
    while (current_heap_segment && heap_segment_swept_in_plan (current_heap_segment))
    {
        uint8_t* start = heap_segment_mem (current_heap_segment);
        uint8_t* end = heap_segment_allocated (current_heap_segment);
        uint8_t* obj = start;
        uint8_t* plug_start = nullptr;
        while (obj < end)
        {
            if (((CObjectHeader*)obj)->IsFree())
            {
                if (plug_start)
                {
                    fn (plug_start, obj, 0, profiling_context, false, false);
                    plug_start = nullptr;
                }
            }
            else
            {
                if (!plug_start)
                {
                    plug_start = obj;
                }
            }

            obj += Align (size (obj));
        }
        if (plug_start)
        {
            fn (plug_start, end, 0, profiling_context, false, false);
        }
        current_heap_segment = heap_segment_next_rw (current_heap_segment);
    }
    return current_heap_segment;
}

#endif //USE_REGIONS

void gc_heap::walk_survivors (record_surv_fn fn, void* context, walk_surv_type type)
{
    if (type == walk_for_gc)
        walk_survivors_relocation (context, fn);
#if defined(BACKGROUND_GC) && defined(FEATURE_EVENT_TRACE)
    else if (type == walk_for_bgc)
        walk_survivors_for_bgc (context, fn);
#endif //BACKGROUND_GC && FEATURE_EVENT_TRACE
    else
        assert (!"unknown type!");
}

#if defined(BACKGROUND_GC) && defined(FEATURE_EVENT_TRACE)
void gc_heap::walk_survivors_for_bgc (void* profiling_context, record_surv_fn fn)
{
    assert(settings.concurrent);

    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        int align_const = get_alignment_constant (i == max_generation);
        heap_segment* seg = heap_segment_rw (generation_start_segment (generation_of (i)));

        while (seg)
        {
            uint8_t* o = heap_segment_mem (seg);
            uint8_t* end = heap_segment_allocated (seg);

            while (o < end)
            {
                if (method_table(o) == g_gc_pFreeObjectMethodTable)
                {
                    o += Align (size (o), align_const);
                    continue;
                }

                // It's survived. Make a fake plug, starting at o,
                // and send the event

                uint8_t* plug_start = o;

                while (method_table(o) != g_gc_pFreeObjectMethodTable)
                {
                    o += Align (size (o), align_const);
                    if (o >= end)
                    {
                        break;
                    }
                }

                uint8_t* plug_end = o;

                fn (plug_start,
                    plug_end,
                    0,              // Reloc distance == 0 as this is non-compacting
                    profiling_context,
                    false,          // Non-compacting
                    true);          // BGC
            }

            seg = heap_segment_next (seg);
        }
    }
}

#endif

void gc_heap::relocate_phase (int condemned_gen_number,
                              uint8_t* first_condemned_address)
{
    ScanContext sc;
    sc.thread_number = heap_number;
    sc.thread_count = n_heaps;
    sc.promotion = FALSE;
    sc.concurrent = FALSE;

#ifdef MULTIPLE_HEAPS
    //join all threads to make sure they are synchronized
    dprintf(3, ("Joining after end of plan"));
    gc_t_join.join(this, gc_join_begin_relocate_phase);
    if (gc_t_join.joined())
    {
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_EVENT_TRACE
        if (informational_event_enabled_p)
        {
            gc_time_info[time_relocate] = GetHighPrecisionTimeStamp();
        }
#endif //FEATURE_EVENT_TRACE

#ifdef USE_REGIONS
        verify_region_to_generation_map();
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
        //join all threads to make sure they are synchronized
        dprintf(3, ("Restarting for relocation"));
        gc_t_join.restart();
    }
#endif //MULTIPLE_HEAPS

    dprintf (2, (ThreadStressLog::gcStartRelocateMsg(), heap_number));

    dprintf(3,("Relocating roots"));
    GCScan::GcScanRoots(GCHeap::Relocate,
                            condemned_gen_number, max_generation, &sc);

    verify_pins_with_post_plug_info("after reloc stack");

#ifdef BACKGROUND_GC
    if (gc_heap::background_running_p())
    {
        scan_background_roots (GCHeap::Relocate, heap_number, &sc);
    }
#endif //BACKGROUND_GC

#ifdef FEATURE_CARD_MARKING_STEALING
    // for card marking stealing, do the other relocations *before* we scan the older generations
    // this gives us a chance to make up for imbalance in these phases later
    {
        dprintf(3, ("Relocating survivors"));
        relocate_survivors(condemned_gen_number,
            first_condemned_address);
    }

#ifdef FEATURE_PREMORTEM_FINALIZATION
    dprintf(3, ("Relocating finalization data"));
    finalize_queue->RelocateFinalizationData(condemned_gen_number,
        __this);
#endif // FEATURE_PREMORTEM_FINALIZATION

    {
        dprintf(3, ("Relocating handle table"));
        GCScan::GcScanHandles(GCHeap::Relocate,
            condemned_gen_number, max_generation, &sc);
    }
#endif // FEATURE_CARD_MARKING_STEALING

    if (condemned_gen_number != max_generation)
    {
#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
        if (!card_mark_done_soh)
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
        {
            dprintf (3, ("Relocating cross generation pointers on heap %d", heap_number));
            mark_through_cards_for_segments(&gc_heap::relocate_address, TRUE THIS_ARG);
            verify_pins_with_post_plug_info("after reloc cards");
#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
            card_mark_done_soh = true;
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
        }
    }
    if (condemned_gen_number != max_generation)
    {
#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
        if (!card_mark_done_uoh)
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
        {
            dprintf (3, ("Relocating cross generation pointers for uoh objects on heap %d", heap_number));
            for (int i = uoh_start_generation; i < total_generation_count; i++)
            {
#ifndef ALLOW_REFERENCES_IN_POH
                if (i != poh_generation)
#endif //ALLOW_REFERENCES_IN_POH
                    mark_through_cards_for_uoh_objects(&gc_heap::relocate_address, i, TRUE THIS_ARG);
            }

#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
            card_mark_done_uoh = true;
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
        }
    }
    else
    {
#ifdef FEATURE_LOH_COMPACTION
        if (loh_compacted_p)
        {
            assert (settings.condemned_generation == max_generation);
            relocate_in_loh_compact();
        }
        else
#endif //FEATURE_LOH_COMPACTION
        {
            relocate_in_uoh_objects (loh_generation);
        }

#ifdef ALLOW_REFERENCES_IN_POH
        relocate_in_uoh_objects (poh_generation);
#endif
    }
#ifndef FEATURE_CARD_MARKING_STEALING
    // moved this code *before* we scan the older generations via mark_through_cards_xxx
    // this gives us a chance to have mark_through_cards_xxx make up for imbalance in the other relocations
    {
        dprintf(3,("Relocating survivors"));
        relocate_survivors (condemned_gen_number,
                            first_condemned_address);
    }

#ifdef FEATURE_PREMORTEM_FINALIZATION
        dprintf(3,("Relocating finalization data"));
        finalize_queue->RelocateFinalizationData (condemned_gen_number,
                                                       __this);
#endif // FEATURE_PREMORTEM_FINALIZATION

    {
        dprintf(3,("Relocating handle table"));
        GCScan::GcScanHandles(GCHeap::Relocate,
                                  condemned_gen_number, max_generation, &sc);
    }
#endif // !FEATURE_CARD_MARKING_STEALING


#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
    if (condemned_gen_number != max_generation)
    {
        // check the other heaps cyclically and try to help out where the relocation isn't done
        for (int i = 0; i < gc_heap::n_heaps; i++)
        {
            int heap_number_to_look_at = (i + heap_number) % gc_heap::n_heaps;
            gc_heap* hp = gc_heap::g_heaps[heap_number_to_look_at];
            if (!hp->card_mark_done_soh)
            {
                dprintf(3, ("Relocating cross generation pointers on heap %d", hp->heap_number));
                hp->mark_through_cards_for_segments(&gc_heap::relocate_address, TRUE THIS_ARG);
                hp->card_mark_done_soh = true;
            }

            if (!hp->card_mark_done_uoh)
            {
                dprintf(3, ("Relocating cross generation pointers for uoh objects on heap %d", hp->heap_number));
                for (int i = uoh_start_generation; i < total_generation_count; i++)
                {
#ifndef ALLOW_REFERENCES_IN_POH
                    if (i != poh_generation)
#endif //ALLOW_REFERENCES_IN_POH
                        hp->mark_through_cards_for_uoh_objects(&gc_heap::relocate_address, i, TRUE THIS_ARG);
                }
                hp->card_mark_done_uoh = true;
            }
        }
    }
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING

    dprintf(2, (ThreadStressLog::gcEndRelocateMsg(), heap_number));
}

// This compares to see if tree is the current pinned plug and returns info
// for this pinned plug. Also advances the pinned queue if that's the case.
//
// We don't change the values of the plug info if tree is not the same as
// the current pinned plug - the caller is responsible for setting the right
// values to begin with.
//
// POPO TODO: We are keeping this temporarily as this is also used by realloc
// where it passes FALSE to deque_p, change it to use the same optimization
// as relocate. Not as essential since realloc is already a slow path.
mark* gc_heap::get_next_pinned_entry (uint8_t* tree,
                                      BOOL* has_pre_plug_info_p,
                                      BOOL* has_post_plug_info_p,
                                      BOOL deque_p)
{
    if (!pinned_plug_que_empty_p())
    {
        mark* oldest_entry = oldest_pin();
        uint8_t* oldest_plug = pinned_plug (oldest_entry);
        if (tree == oldest_plug)
        {
            *has_pre_plug_info_p =  oldest_entry->has_pre_plug_info();
            *has_post_plug_info_p = oldest_entry->has_post_plug_info();

            if (deque_p)
            {
                deque_pinned_plug();
            }

            dprintf (3, ("found a pinned plug %p, pre: %d, post: %d",
                tree,
                (*has_pre_plug_info_p ? 1 : 0),
                (*has_post_plug_info_p ? 1 : 0)));

            return oldest_entry;
        }
    }

    return NULL;
}

// This also deques the oldest entry and update the oldest plug
mark* gc_heap::get_oldest_pinned_entry (BOOL* has_pre_plug_info_p,
                                        BOOL* has_post_plug_info_p)
{
    mark* oldest_entry = oldest_pin();
    *has_pre_plug_info_p =  oldest_entry->has_pre_plug_info();
    *has_post_plug_info_p = oldest_entry->has_post_plug_info();

    deque_pinned_plug();
    update_oldest_pinned_plug();
    return oldest_entry;
}

inline
void gc_heap::copy_cards_range (uint8_t* dest, uint8_t* src, size_t len, BOOL copy_cards_p)
{
    if (copy_cards_p)
        copy_cards_for_addresses (dest, src, len);
    else
        clear_card_for_addresses (dest, dest + len);
}

// POPO TODO: We should actually just recover the artificially made gaps here..because when we copy
// we always copy the earlier plugs first which means we won't need the gap sizes anymore. This way
// we won't need to individually recover each overwritten part of plugs.
inline
void  gc_heap::gcmemcopy (uint8_t* dest, uint8_t* src, size_t len, BOOL copy_cards_p)
{
    if (dest != src)
    {
#ifdef BACKGROUND_GC
        if (current_c_gc_state == c_gc_state_marking)
        {
            //TODO: should look to see whether we should consider changing this
            // to copy a consecutive region of the mark array instead.
            copy_mark_bits_for_addresses (dest, src, len);
        }
#endif //BACKGROUND_GC

#ifdef DOUBLY_LINKED_FL
        BOOL set_bgc_mark_bits_p = is_plug_bgc_mark_bit_set (src);
        if (set_bgc_mark_bits_p)
        {
            clear_plug_bgc_mark_bit (src);
        }

        BOOL make_free_obj_p = FALSE;
        if (len <= min_free_item_no_prev)
        {
            make_free_obj_p = is_free_obj_in_compact_bit_set (src);

            if (make_free_obj_p)
            {
                clear_free_obj_in_compact_bit (src);
            }
        }
#endif //DOUBLY_LINKED_FL

        //dprintf(3,(" Memcopy [%p->%p, %p->%p[", (size_t)src, (size_t)dest, (size_t)src+len, (size_t)dest+len));
        dprintf(3,(ThreadStressLog::gcMemCopyMsg(), (size_t)src, (size_t)dest, (size_t)src+len, (size_t)dest+len));
        memcopy (dest - plug_skew, src - plug_skew, len);

#ifdef DOUBLY_LINKED_FL
        if (set_bgc_mark_bits_p)
        {
            uint8_t* dest_o = dest;
            uint8_t* dest_end_o = dest + len;
            while (dest_o < dest_end_o)
            {
                uint8_t* next_o = dest_o + Align (size (dest_o));
                background_mark (dest_o, background_saved_lowest_address, background_saved_highest_address);

                dest_o = next_o;
            }
            dprintf (3333, ("[h%d] GM: %p(%zx-%zx)->%p(%zx-%zx)",
                heap_number, dest,
                (size_t)(&mark_array [mark_word_of (dest)]),
                (size_t)(mark_array [mark_word_of (dest)]),
                dest_end_o,
                (size_t)(&mark_array [mark_word_of (dest_o)]),
                (size_t)(mark_array [mark_word_of (dest_o)])));
        }

        if (make_free_obj_p)
        {
            size_t* filler_free_obj_size_location = (size_t*)(dest + min_free_item_no_prev);
            size_t filler_free_obj_size = *filler_free_obj_size_location;
            make_unused_array ((dest + len), filler_free_obj_size);
            dprintf (3333, ("[h%d] smallobj, %p(%zd): %p->%p", heap_number,
                filler_free_obj_size_location, filler_free_obj_size, (dest + len), (dest + len + filler_free_obj_size)));
        }
#endif //DOUBLY_LINKED_FL

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        if (SoftwareWriteWatch::IsEnabledForGCHeap())
        {
            // The ranges [src - plug_kew .. src[ and [src + len - plug_skew .. src + len[ are ObjHeaders, which don't have GC
            // references, and are not relevant for write watch. The latter range actually corresponds to the ObjHeader for the
            // object at (src + len), so it can be ignored anyway.
            SoftwareWriteWatch::SetDirtyRegion(dest, len - plug_skew);
        }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        copy_cards_range (dest, src, len, copy_cards_p);
    }
}

void gc_heap::compact_plug (uint8_t* plug, size_t size, BOOL check_last_object_p, compact_args* args)
{
    args->print();
    uint8_t* reloc_plug = plug + args->last_plug_relocation;

    if (check_last_object_p)
    {
        size += sizeof (gap_reloc_pair);
        mark* entry = args->pinned_plug_entry;

        if (args->is_shortened)
        {
            assert (entry->has_post_plug_info());
            entry->swap_post_plug_and_saved();
        }
        else
        {
            assert (entry->has_pre_plug_info());
            entry->swap_pre_plug_and_saved();
        }
    }

    int  old_brick_entry =  brick_table [brick_of (plug)];

    assert (node_relocation_distance (plug) == args->last_plug_relocation);

#ifdef FEATURE_STRUCTALIGN
    ptrdiff_t alignpad = node_alignpad(plug);
    if (alignpad)
    {
        make_unused_array (reloc_plug - alignpad, alignpad);
        if (brick_of (reloc_plug - alignpad) != brick_of (reloc_plug))
        {
            // The alignment padding is straddling one or more bricks;
            // it has to be the last "object" of its first brick.
            fix_brick_to_highest (reloc_plug - alignpad, reloc_plug);
        }
    }
#else // FEATURE_STRUCTALIGN
    size_t unused_arr_size = 0;
    BOOL  already_padded_p = FALSE;
#ifdef SHORT_PLUGS
    if (is_plug_padded (plug))
    {
        already_padded_p = TRUE;
        clear_plug_padded (plug);
        unused_arr_size = Align (min_obj_size);
    }
#endif //SHORT_PLUGS
    if (node_realigned (plug))
    {
        unused_arr_size += switch_alignment_size (already_padded_p);
    }

    if (unused_arr_size != 0)
    {
        make_unused_array (reloc_plug - unused_arr_size, unused_arr_size);

        if (brick_of (reloc_plug - unused_arr_size) != brick_of (reloc_plug))
        {
            dprintf (3, ("fix B for padding: %zd: %p->%p",
                unused_arr_size, (reloc_plug - unused_arr_size), reloc_plug));
            // The alignment padding is straddling one or more bricks;
            // it has to be the last "object" of its first brick.
            fix_brick_to_highest (reloc_plug - unused_arr_size, reloc_plug);
        }
    }
#endif // FEATURE_STRUCTALIGN

#ifdef SHORT_PLUGS
    if (is_plug_padded (plug))
    {
        make_unused_array (reloc_plug - Align (min_obj_size), Align (min_obj_size));

        if (brick_of (reloc_plug - Align (min_obj_size)) != brick_of (reloc_plug))
        {
            // The alignment padding is straddling one or more bricks;
            // it has to be the last "object" of its first brick.
            fix_brick_to_highest (reloc_plug - Align (min_obj_size), reloc_plug);
        }
    }
#endif //SHORT_PLUGS

    gcmemcopy (reloc_plug, plug, size, args->copy_cards_p);

    if (args->check_gennum_p)
    {
        int src_gennum = args->src_gennum;
        if (src_gennum == -1)
        {
            src_gennum = object_gennum (plug);
        }

        int dest_gennum = object_gennum_plan (reloc_plug);

        if (src_gennum < dest_gennum)
        {
            generation_allocation_size (generation_of (dest_gennum)) += size;
        }
    }

    size_t current_reloc_brick = args->current_compacted_brick;

    if (brick_of (reloc_plug) != current_reloc_brick)
    {
        dprintf (3, ("last reloc B: %zx, current reloc B: %zx",
            current_reloc_brick, brick_of (reloc_plug)));

        if (args->before_last_plug)
        {
            dprintf (3,(" fixing last brick %zx to point to last plug %p(%zx)",
                     current_reloc_brick,
                     args->before_last_plug,
                     (args->before_last_plug - brick_address (current_reloc_brick))));

            {
                set_brick (current_reloc_brick,
                        args->before_last_plug - brick_address (current_reloc_brick));
            }
        }
        current_reloc_brick = brick_of (reloc_plug);
    }
    size_t end_brick = brick_of (reloc_plug + size-1);
    if (end_brick != current_reloc_brick)
    {
        // The plug is straddling one or more bricks
        // It has to be the last plug of its first brick
        dprintf (3,("plug spanning multiple bricks, fixing first brick %zx to %zx(%zx)",
                 current_reloc_brick, (size_t)reloc_plug,
                 (reloc_plug - brick_address (current_reloc_brick))));

        {
            set_brick (current_reloc_brick,
                    reloc_plug - brick_address (current_reloc_brick));
        }
        // update all intervening brick
        size_t brick = current_reloc_brick + 1;
        dprintf (3,("setting intervening bricks %zu->%zu to -1",
            brick, (end_brick - 1)));
        while (brick < end_brick)
        {
            set_brick (brick, -1);
            brick++;
        }
        // code last brick offset as a plug address
        args->before_last_plug = brick_address (end_brick) -1;
        current_reloc_brick = end_brick;
        dprintf (3, ("setting before last to %p, last brick to %zx",
            args->before_last_plug, current_reloc_brick));
    }
    else
    {
        dprintf (3, ("still in the same brick: %zx", end_brick));
        args->before_last_plug = reloc_plug;
    }
    args->current_compacted_brick = current_reloc_brick;

    if (check_last_object_p)
    {
        mark* entry = args->pinned_plug_entry;

        if (args->is_shortened)
        {
            entry->swap_post_plug_and_saved();
        }
        else
        {
            entry->swap_pre_plug_and_saved();
        }
    }
}

void gc_heap::compact_in_brick (uint8_t* tree, compact_args* args)
{
    assert (tree != NULL);
    int   left_node = node_left_child (tree);
    int   right_node = node_right_child (tree);
    ptrdiff_t relocation = node_relocation_distance (tree);

    args->print();

    if (left_node)
    {
        dprintf (3, ("B: L: %d->%p", left_node, (tree + left_node)));
        compact_in_brick ((tree + left_node), args);
    }

    uint8_t*  plug = tree;
    BOOL   has_pre_plug_info_p = FALSE;
    BOOL   has_post_plug_info_p = FALSE;

    if (tree == oldest_pinned_plug)
    {
        args->pinned_plug_entry = get_oldest_pinned_entry (&has_pre_plug_info_p,
                                                           &has_post_plug_info_p);
        assert (tree == pinned_plug (args->pinned_plug_entry));
    }

    if (args->last_plug != 0)
    {
        size_t gap_size = node_gap_size (tree);
        uint8_t*  gap = (plug - gap_size);
        uint8_t*  last_plug_end = gap;
        size_t last_plug_size = (last_plug_end - args->last_plug);
        assert ((last_plug_size & (sizeof(PTR_PTR) - 1)) == 0);
        dprintf (3, ("tree: %p, last_plug: %p, gap: %p(%zx), last_plug_end: %p, size: %zx",
            tree, args->last_plug, gap, gap_size, last_plug_end, last_plug_size));

        BOOL check_last_object_p = (args->is_shortened || has_pre_plug_info_p);
        if (!check_last_object_p)
        {
            assert (last_plug_size >= Align (min_obj_size));
        }

        compact_plug (args->last_plug, last_plug_size, check_last_object_p, args);
    }
    else
    {
        assert (!has_pre_plug_info_p);
    }

    dprintf (3, ("set args last plug to plug: %p, reloc: %zx", plug, relocation));
    args->last_plug = plug;
    args->last_plug_relocation = relocation;
    args->is_shortened = has_post_plug_info_p;

    if (right_node)
    {
        dprintf (3, ("B: R: %d->%p", right_node, (tree + right_node)));
        compact_in_brick ((tree + right_node), args);
    }
}

// This returns the recovered size for gen2 plugs as that's what we need
// mostly - would be nice to make it work for all generations.
size_t gc_heap::recover_saved_pinned_info()
{
    reset_pinned_queue_bos();
    size_t total_recovered_sweep_size = 0;

    while (!(pinned_plug_que_empty_p()))
    {
        mark* oldest_entry = oldest_pin();
        size_t recovered_sweep_size = oldest_entry->recover_plug_info();

        if (recovered_sweep_size > 0)
        {
            uint8_t* plug = pinned_plug (oldest_entry);
            if (object_gennum (plug) == max_generation)
            {
                dprintf (3, ("recovered %p(%zd) from pin", plug, recovered_sweep_size));
                total_recovered_sweep_size += recovered_sweep_size;
            }
        }
#ifdef GC_CONFIG_DRIVEN
        if (oldest_entry->has_pre_plug_info() && oldest_entry->has_post_plug_info())
            record_interesting_data_point (idp_pre_and_post_pin);
        else if (oldest_entry->has_pre_plug_info())
            record_interesting_data_point (idp_pre_pin);
        else if (oldest_entry->has_post_plug_info())
            record_interesting_data_point (idp_post_pin);
#endif //GC_CONFIG_DRIVEN

        deque_pinned_plug();
    }

    return total_recovered_sweep_size;
}

void gc_heap::compact_phase (int condemned_gen_number,
                             uint8_t*  first_condemned_address,
                             BOOL clear_cards)
{
#ifdef MULTIPLE_HEAPS
    dprintf(3, ("Joining after end of relocation"));
    gc_t_join.join(this, gc_join_relocate_phase_done);
    if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef FEATURE_EVENT_TRACE
        if (informational_event_enabled_p)
        {
            gc_time_info[time_compact] = GetHighPrecisionTimeStamp();
            gc_time_info[time_relocate] = gc_time_info[time_compact] - gc_time_info[time_relocate];
        }
#endif //FEATURE_EVENT_TRACE

#ifdef MULTIPLE_HEAPS
        dprintf(3, ("Restarting for compaction"));
        gc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    dprintf (2, (ThreadStressLog::gcStartCompactMsg(), heap_number,
        first_condemned_address, brick_of (first_condemned_address)));

#ifdef FEATURE_LOH_COMPACTION
    if (loh_compacted_p)
    {
        compact_loh();
    }
#endif //FEATURE_LOH_COMPACTION

    reset_pinned_queue_bos();
    update_oldest_pinned_plug();
    BOOL reused_seg = expand_reused_seg_p();
    if (reused_seg)
    {
        for (int i = 1; i <= max_generation; i++)
        {
            generation_allocation_size (generation_of (i)) = 0;
        }
    }

    int stop_gen_idx = get_stop_generation_index (condemned_gen_number);
    for (int i = condemned_gen_number; i >= stop_gen_idx; i--)
    {
        generation* condemned_gen = generation_of (i);
        heap_segment* current_heap_segment = get_start_segment (condemned_gen);
#ifdef USE_REGIONS
        if (!current_heap_segment)
            continue;

        size_t   current_brick = brick_of (heap_segment_mem (current_heap_segment));
#else
        size_t   current_brick = brick_of (first_condemned_address);
#endif //USE_REGIONS

        uint8_t*  end_address = heap_segment_allocated (current_heap_segment);

#ifndef USE_REGIONS
        if ((first_condemned_address >= end_address) && (condemned_gen_number < max_generation))
        {
            return;
        }
#endif //!USE_REGIONS

        size_t  end_brick = brick_of (end_address-1);
        compact_args args;
        args.last_plug = 0;
        args.before_last_plug = 0;
        args.current_compacted_brick = ~((size_t)1);
        args.is_shortened = FALSE;
        args.pinned_plug_entry = 0;
        args.copy_cards_p =  (condemned_gen_number >= 1) || !clear_cards;
        args.check_gennum_p = reused_seg;
        if (args.check_gennum_p)
        {
            args.src_gennum = ((current_heap_segment == ephemeral_heap_segment) ? -1 : 2);
        }
#ifdef USE_REGIONS
        assert (!args.check_gennum_p);
#endif //USE_REGIONS

        while (1)
        {
            if (current_brick > end_brick)
            {
                if (args.last_plug != 0)
                {
                    dprintf (3, ("compacting last plug: %p", args.last_plug))
                    compact_plug (args.last_plug,
                                  (heap_segment_allocated (current_heap_segment) - args.last_plug),
                                  args.is_shortened,
                                  &args);
                }

                heap_segment* next_heap_segment = heap_segment_next_non_sip (current_heap_segment);
                if (next_heap_segment)
                {
                    current_heap_segment = next_heap_segment;
                    current_brick = brick_of (heap_segment_mem (current_heap_segment));
                    end_brick = brick_of (heap_segment_allocated (current_heap_segment)-1);
                    args.last_plug = 0;
                    if (args.check_gennum_p)
                    {
                        args.src_gennum = ((current_heap_segment == ephemeral_heap_segment) ? -1 : 2);
                    }
                    continue;
                }
                else
                {
                    if (args.before_last_plug !=0)
                    {
                        dprintf (3, ("Fixing last brick %zx to point to plug %zx",
                                    args.current_compacted_brick, (size_t)args.before_last_plug));
                        assert (args.current_compacted_brick != ~1u);
                        set_brick (args.current_compacted_brick,
                                   args.before_last_plug - brick_address (args.current_compacted_brick));
                    }
                    break;
                }
            }
            {
                int  brick_entry =  brick_table [ current_brick ];
                dprintf (3, ("B: %zx(%zx)->%p",
                    current_brick, (size_t)brick_entry, (brick_address (current_brick) + brick_entry - 1)));

                if (brick_entry >= 0)
                {
                    compact_in_brick ((brick_address (current_brick) + brick_entry -1),
                                      &args);

                }
            }
            current_brick++;
        }
    }

    recover_saved_pinned_info();

    concurrent_print_time_delta ("compact end");

    dprintf (2, (ThreadStressLog::gcEndCompactMsg(), heap_number));
}

#ifndef USE_REGIONS
uint8_t*
gc_heap::compute_next_boundary (int gen_number,
                                BOOL relocating)
{
    //when relocating, the fault line is the plan start of the younger
    //generation because the generation is promoted.
    if (relocating && (gen_number == (settings.condemned_generation + 1)))
    {
        generation* gen = generation_of (gen_number - 1);
        uint8_t* gen_alloc = generation_plan_allocation_start (gen);
        assert (gen_alloc);
        return gen_alloc;
    }
    else
    {
        assert (gen_number > settings.condemned_generation);
        return generation_allocation_start (generation_of (gen_number - 1 ));
    }
}

#endif //!USE_REGIONS

void gc_heap::walk_survivors_relocation (void* profiling_context, record_surv_fn fn)
{
    // Now walk the portion of memory that is actually being relocated.
    walk_relocation (profiling_context, fn);

#ifdef FEATURE_LOH_COMPACTION
    if (loh_compacted_p)
    {
        walk_relocation_for_loh (profiling_context, fn);
    }
#endif //FEATURE_LOH_COMPACTION
}

void gc_heap::walk_survivors_for_uoh (void* profiling_context, record_surv_fn fn, int gen_number)
{
    generation* gen        = generation_of (gen_number);
    heap_segment* seg      = heap_segment_rw (generation_start_segment (gen));;

    _ASSERTE(seg != NULL);

    uint8_t* o                = get_uoh_start_object (seg, gen);
    uint8_t* plug_end         = o;
    uint8_t* plug_start       = o;

    while (1)
    {
        if (o >= heap_segment_allocated (seg))
        {
            seg = heap_segment_next (seg);
            if (seg == 0)
                break;
            else
                o = heap_segment_mem (seg);
        }
        if (uoh_object_marked(o, FALSE))
        {
            plug_start = o;

            BOOL m = TRUE;
            while (m)
            {
                o = o + AlignQword (size (o));
                if (o >= heap_segment_allocated (seg))
                {
                    break;
                }
                m = uoh_object_marked (o, FALSE);
            }

            plug_end = o;

            fn (plug_start, plug_end, 0, profiling_context, false, false);
        }
        else
        {
            while (o < heap_segment_allocated (seg) && !uoh_object_marked(o, FALSE))
            {
                o = o + AlignQword (size (o));
            }
        }
    }
}

void gc_heap::relocate_in_uoh_objects (int gen_num)
{
    generation* gen = generation_of (gen_num);

    heap_segment* seg = heap_segment_rw (generation_start_segment (gen));

    _ASSERTE(seg != NULL);

    uint8_t* o = get_uoh_start_object (seg, gen);

    while (1)
    {
        if (o >= heap_segment_allocated (seg))
        {
            seg = heap_segment_next_rw (seg);
            if (seg == 0)
                break;
            else
            {
                o = heap_segment_mem (seg);
            }
        }
        while (o < heap_segment_allocated (seg))
        {
            check_class_object_demotion (o);
            if (contain_pointers (o))
            {
                dprintf(3, ("Relocating through uoh object %zx", (size_t)o));
                go_through_object_nostart (method_table (o), o, size(o), pval,
                        {
                            reloc_survivor_helper (pval);
                        });
            }
            o = o + AlignQword (size (o));
        }
    }
}
