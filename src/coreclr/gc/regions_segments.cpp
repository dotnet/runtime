// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

size_t size_seg_mapping_table_of (uint8_t* from, uint8_t* end)
{
    from = align_lower_segment (from);
    end = align_on_segment (end);
    dprintf (1, ("from: %p, end: %p, size: %zx", from, end,
        sizeof (seg_mapping)*(((size_t)(end - from) >> gc_heap::min_segment_size_shr))));
    return (sizeof (seg_mapping)*((size_t)(end - from) >> gc_heap::min_segment_size_shr));
}

size_t size_region_to_generation_table_of (uint8_t* from, uint8_t* end)
{
    dprintf (1, ("from: %p, end: %p, size: %zx", from, end,
        sizeof (uint8_t)*(((size_t)(end - from) >> gc_heap::min_segment_size_shr))));
    return sizeof (uint8_t)*((size_t)(end - from) >> gc_heap::min_segment_size_shr);
}

inline
size_t seg_mapping_word_of (uint8_t* add)
{
    return (size_t)add >> gc_heap::min_segment_size_shr;
}

#ifdef FEATURE_BASICFREEZE
void seg_mapping_table_add_ro_segment (heap_segment* seg)
{
    if ((heap_segment_reserved (seg) <= g_gc_lowest_address) || (heap_segment_mem (seg) >= g_gc_highest_address))
        return;

    for (size_t entry_index = ro_seg_begin_index (seg); entry_index <= ro_seg_end_index (seg); entry_index++)
    {
#ifdef USE_REGIONS
        heap_segment* region = (heap_segment*)&seg_mapping_table[entry_index];
        heap_segment_allocated (region) = (uint8_t*)ro_in_entry;
#else
        seg_mapping_table[entry_index].seg1 = (heap_segment*)((size_t)seg_mapping_table[entry_index].seg1 | ro_in_entry);
#endif //USE_REGIONS
    }
}

void seg_mapping_table_remove_ro_segment (heap_segment* seg)
{
    UNREFERENCED_PARAMETER(seg);
}

#endif //FEATURE_BASICFREEZE
#ifndef USE_REGIONS
void gc_heap::seg_mapping_table_add_segment (heap_segment* seg, gc_heap* hp)
{
    size_t seg_end = (size_t)(heap_segment_reserved (seg) - 1);
    size_t begin_index = (size_t)seg >> gc_heap::min_segment_size_shr;
    seg_mapping* begin_entry = &seg_mapping_table[begin_index];
    size_t end_index = seg_end >> gc_heap::min_segment_size_shr;
    seg_mapping* end_entry = &seg_mapping_table[end_index];

    dprintf (2, ("adding seg %p(%zd)-%p(%zd)",
        seg, begin_index, heap_segment_reserved (seg), end_index));

    dprintf (2, ("before add: begin entry%zd: boundary: %p; end entry: %zd: boundary: %p",
        begin_index, (seg_mapping_table[begin_index].boundary + 1),
        end_index, (seg_mapping_table[end_index].boundary + 1)));

#ifdef MULTIPLE_HEAPS
#ifdef SIMPLE_DPRINTF
    dprintf (2, ("begin %zd: h0: %p(%d), h1: %p(%d); end %zd: h0: %p(%d), h1: %p(%d)",
        begin_index, (uint8_t*)(begin_entry->h0), (begin_entry->h0 ? begin_entry->h0->heap_number : -1),
        (uint8_t*)(begin_entry->h1), (begin_entry->h1 ? begin_entry->h1->heap_number : -1),
        end_index, (uint8_t*)(end_entry->h0), (end_entry->h0 ? end_entry->h0->heap_number : -1),
        (uint8_t*)(end_entry->h1), (end_entry->h1 ? end_entry->h1->heap_number : -1)));
#endif //SIMPLE_DPRINTF
    assert (end_entry->boundary == 0);
    assert (end_entry->h0 == 0);
    end_entry->h0 = hp;
    assert (begin_entry->h1 == 0);
    begin_entry->h1 = hp;
#else
    UNREFERENCED_PARAMETER(hp);
#endif //MULTIPLE_HEAPS

    end_entry->boundary = (uint8_t*)seg_end;

    dprintf (2, ("set entry %zd seg1 and %zd seg0 to %p", begin_index, end_index, seg));
    assert ((begin_entry->seg1 == 0) || ((size_t)(begin_entry->seg1) == ro_in_entry));
    begin_entry->seg1 = (heap_segment*)((size_t)(begin_entry->seg1) | (size_t)seg);
    end_entry->seg0 = seg;

    // for every entry inbetween we need to set its heap too.
    for (size_t entry_index = (begin_index + 1); entry_index <= (end_index - 1); entry_index++)
    {
        assert (seg_mapping_table[entry_index].boundary == 0);
#ifdef MULTIPLE_HEAPS
        assert (seg_mapping_table[entry_index].h0 == 0);
        seg_mapping_table[entry_index].h1 = hp;
#endif //MULTIPLE_HEAPS
        seg_mapping_table[entry_index].seg1 = seg;
    }

    dprintf (2, ("after add: begin entry%zd: boundary: %p; end entry: %zd: boundary: %p",
        begin_index, (seg_mapping_table[begin_index].boundary + 1),
        end_index, (seg_mapping_table[end_index].boundary + 1)));
#if defined(MULTIPLE_HEAPS) && defined(SIMPLE_DPRINTF)
    dprintf (2, ("begin %zd: h0: %p(%d), h1: %p(%d); end: %zd h0: %p(%d), h1: %p(%d)",
        begin_index, (uint8_t*)(begin_entry->h0), (begin_entry->h0 ? begin_entry->h0->heap_number : -1),
        (uint8_t*)(begin_entry->h1), (begin_entry->h1 ? begin_entry->h1->heap_number : -1),
        end_index, (uint8_t*)(end_entry->h0), (end_entry->h0 ? end_entry->h0->heap_number : -1),
        (uint8_t*)(end_entry->h1), (end_entry->h1 ? end_entry->h1->heap_number : -1)));
#endif //MULTIPLE_HEAPS && SIMPLE_DPRINTF
}

void gc_heap::seg_mapping_table_remove_segment (heap_segment* seg)
{
    size_t seg_end = (size_t)(heap_segment_reserved (seg) - 1);
    size_t begin_index = (size_t)seg >> gc_heap::min_segment_size_shr;
    seg_mapping* begin_entry = &seg_mapping_table[begin_index];
    size_t end_index = seg_end >> gc_heap::min_segment_size_shr;
    seg_mapping* end_entry = &seg_mapping_table[end_index];
    dprintf (2, ("removing seg %p(%zd)-%p(%zd)",
        seg, begin_index, heap_segment_reserved (seg), end_index));

    assert (end_entry->boundary == (uint8_t*)seg_end);
    end_entry->boundary = 0;

#ifdef MULTIPLE_HEAPS
    gc_heap* hp = heap_segment_heap (seg);
    assert (end_entry->h0 == hp);
    end_entry->h0 = 0;
    assert (begin_entry->h1 == hp);
    begin_entry->h1 = 0;
#endif //MULTIPLE_HEAPS

    assert (begin_entry->seg1 != 0);
    begin_entry->seg1 = (heap_segment*)((size_t)(begin_entry->seg1) & ro_in_entry);
    end_entry->seg0 = 0;

    // for every entry inbetween we need to reset its heap too.
    for (size_t entry_index = (begin_index + 1); entry_index <= (end_index - 1); entry_index++)
    {
        assert (seg_mapping_table[entry_index].boundary == 0);
#ifdef MULTIPLE_HEAPS
        assert (seg_mapping_table[entry_index].h0 == 0);
        assert (seg_mapping_table[entry_index].h1 == hp);
        seg_mapping_table[entry_index].h1 = 0;
#endif //MULTIPLE_HEAPS
        seg_mapping_table[entry_index].seg1 = 0;
    }

    dprintf (2, ("after remove: begin entry%zd: boundary: %p; end entry: %zd: boundary: %p",
        begin_index, (seg_mapping_table[begin_index].boundary + 1),
        end_index, (seg_mapping_table[end_index].boundary + 1)));
#ifdef MULTIPLE_HEAPS
    dprintf (2, ("begin %zd: h0: %p, h1: %p; end: %zd h0: %p, h1: %p",
        begin_index, (uint8_t*)(begin_entry->h0), (uint8_t*)(begin_entry->h1),
        end_index, (uint8_t*)(end_entry->h0), (uint8_t*)(end_entry->h1)));
#endif //MULTIPLE_HEAPS
}

#endif //!USE_REGIONS

BOOL gc_heap::reserve_initial_memory (size_t normal_size, size_t large_size, size_t pinned_size,
                                      int num_heaps, bool use_large_pages_p, bool separated_poh_p, uint16_t* heap_no_to_numa_node)
{
    BOOL reserve_success = FALSE;

    // should only be called once
    assert (memory_details.initial_memory == 0);

    // soh + loh + poh segments * num_heaps
    memory_details.initial_memory = new (nothrow) imemory_data[num_heaps * (total_generation_count - ephemeral_generation_count)];
    if (memory_details.initial_memory == 0)
    {
        dprintf (2, ("failed to reserve %zd bytes for imemory_data",
            num_heaps * (total_generation_count - ephemeral_generation_count) * sizeof (imemory_data)));
        return FALSE;
    }

    memory_details.initial_normal_heap = memory_details.initial_memory;
    memory_details.initial_large_heap = memory_details.initial_normal_heap + num_heaps;
    memory_details.initial_pinned_heap = memory_details.initial_large_heap + num_heaps;
    memory_details.block_size_normal = normal_size;
    memory_details.block_size_large = large_size;
    memory_details.block_size_pinned = pinned_size;

    memory_details.block_count = num_heaps;

    memory_details.current_block_normal = 0;
    memory_details.current_block_large = 0;
    memory_details.current_block_pinned = 0;

    g_gc_lowest_address = MAX_PTR;
    g_gc_highest_address = 0;

    if (((size_t)MAX_PTR - large_size) < normal_size)
    {
        // we are already overflowing with just one heap.
        dprintf (2, ("0x%zx + 0x%zx already overflow", normal_size, large_size));
        return FALSE;
    }

    if (((size_t)MAX_PTR / memory_details.block_count) < (normal_size + large_size + pinned_size))
    {
        dprintf (2, ("(0x%zx + 0x%zx)*0x%x overflow", normal_size, large_size, memory_details.block_count));
        return FALSE;
    }

    // figure out number of NUMA nodes and allocate additional table for NUMA local reservation
    memory_details.numa_reserved_block_count = 0;
    memory_details.numa_reserved_block_table = nullptr;
    int numa_node_count = 0;
    if (heap_no_to_numa_node != nullptr)
    {
        uint16_t highest_numa_node = 0;

        // figure out the highest NUMA node
        for (int heap_no = 0; heap_no < num_heaps; heap_no++)
        {
            uint16_t heap_numa_node = heap_no_to_numa_node[heap_no];
            highest_numa_node = max (highest_numa_node, heap_numa_node);
        }

        assert (highest_numa_node < MAX_SUPPORTED_HEAPS);

        numa_node_count = highest_numa_node + 1;
        memory_details.numa_reserved_block_count = numa_node_count * (1 + separated_poh_p);
        memory_details.numa_reserved_block_table = new (nothrow) numa_reserved_block[memory_details.numa_reserved_block_count];
        if (memory_details.numa_reserved_block_table == nullptr)
        {
            // we couldn't get the memory - continue as if doing the non-NUMA case
            dprintf(2, ("failed to reserve %zd bytes for numa_reserved_block data", memory_details.numa_reserved_block_count * sizeof(numa_reserved_block)));
            memory_details.numa_reserved_block_count = 0;
        }
    }

    if (memory_details.numa_reserved_block_table != nullptr)
    {
        // figure out how much to reserve on each NUMA node
        // note this can be very different between NUMA nodes, depending on
        // which processors our heaps are associated with
        size_t merged_pinned_size = separated_poh_p ? 0 : pinned_size;
        for (int heap_no = 0; heap_no < num_heaps; heap_no++)
        {
            uint16_t heap_numa_node = heap_no_to_numa_node[heap_no];

            numa_reserved_block * block = &memory_details.numa_reserved_block_table[heap_numa_node];

            // add the size required for this heap
            block->block_size += normal_size + large_size + merged_pinned_size;

            if (separated_poh_p)
            {
                numa_reserved_block* pinned_block = &memory_details.numa_reserved_block_table[numa_node_count + heap_numa_node];

                // add the pinned size required for this heap
                pinned_block->block_size += pinned_size;
            }
        }

        // reserve the appropriate size on each NUMA node
        bool failure = false;
        for (int block_index = 0; block_index < memory_details.numa_reserved_block_count; block_index++)
        {
            numa_reserved_block * block = &memory_details.numa_reserved_block_table[block_index];

            if (block->block_size == 0)
                continue;

            int numa_node = block_index % numa_node_count;
            bool pinned_block = block_index >= numa_node_count;
            block->memory_base = (uint8_t*)virtual_alloc (block->block_size, use_large_pages_p && !pinned_block, (uint16_t)numa_node);
            if (block->memory_base == nullptr)
            {
                dprintf(2, ("failed to reserve %zd bytes for on NUMA node %u", block->block_size, numa_node));
                failure = true;
                break;
            }
            else
            {
                g_gc_lowest_address = min(g_gc_lowest_address, block->memory_base);
                g_gc_highest_address = max(g_gc_highest_address, block->memory_base + block->block_size);
            }
        }

        if (failure)
        {
            // if we had any failures, undo the work done so far
            // we will instead use one of the other allocation patterns
            // we could try to use what we did succeed to reserve, but that gets complicated
            for (int block_index = 0; block_index < memory_details.numa_reserved_block_count; block_index++)
            {
                numa_reserved_block * block = &memory_details.numa_reserved_block_table[block_index];

                if (block->memory_base != nullptr)
                {
                    virtual_free(block->memory_base, block->block_size);
                    block->memory_base = nullptr;
                }
            }
            delete [] memory_details.numa_reserved_block_table;
            memory_details.numa_reserved_block_table = nullptr;
            memory_details.numa_reserved_block_count = 0;
        }
        else
        {
            // for each NUMA node, give out the memory to its heaps
            for (uint16_t numa_node = 0; numa_node < numa_node_count; numa_node++)
            {
                numa_reserved_block * block = &memory_details.numa_reserved_block_table[numa_node];

                numa_reserved_block* pinned_block = separated_poh_p ?
                    &memory_details.numa_reserved_block_table[numa_node_count + numa_node] : nullptr;

                // if the block's size is 0, there can be no heaps on this NUMA node
                if (block->block_size == 0)
                {
                    assert((pinned_block == nullptr) || (pinned_block->block_size == 0));
                    continue;
                }

                uint8_t* memory_base = block->memory_base;
                uint8_t* pinned_memory_base = ((pinned_block == nullptr) ? nullptr : pinned_block->memory_base);
                for (int heap_no = 0; heap_no < num_heaps; heap_no++)
                {
                    uint16_t heap_numa_node = heap_no_to_numa_node[heap_no];

                    if (heap_numa_node != numa_node)
                    {
                        // this heap is on another NUMA node
                        continue;
                    }

                    memory_details.initial_normal_heap[heap_no].memory_base = memory_base;
                    memory_base += normal_size;

                    memory_details.initial_large_heap[heap_no].memory_base = memory_base;
                    memory_base += large_size;

                    if (separated_poh_p)
                    {
                        memory_details.initial_pinned_heap[heap_no].memory_base = pinned_memory_base;
                        pinned_memory_base += pinned_size;
                    }
                    else
                    {
                        memory_details.initial_pinned_heap[heap_no].memory_base = memory_base;
                        memory_base += pinned_size;
                    }
                }
                // sanity check - we should be at the end of the memory block for this NUMA node
                assert (memory_base == block->memory_base + block->block_size);
                assert ((pinned_block == nullptr) || (pinned_memory_base == pinned_block->memory_base + pinned_block->block_size));
            }
            memory_details.allocation_pattern = initial_memory_details::EACH_NUMA_NODE;
            reserve_success = TRUE;
        }
    }

    if (!reserve_success)
    {
        size_t temp_pinned_size = (separated_poh_p ? 0 : pinned_size);
        size_t separate_pinned_size = memory_details.block_count * pinned_size;
        size_t requestedMemory = memory_details.block_count * (normal_size + large_size + temp_pinned_size);

        uint8_t* allatonce_block = (uint8_t*)virtual_alloc(requestedMemory, use_large_pages_p);
        uint8_t* separated_poh_block = nullptr;
        if (allatonce_block && separated_poh_p)
        {
            separated_poh_block = (uint8_t*)virtual_alloc(separate_pinned_size, false);
            if (!separated_poh_block)
            {
                virtual_free(allatonce_block, requestedMemory);
                allatonce_block = nullptr;
            }
        }
        if (allatonce_block)
        {
            if (separated_poh_p)
            {
                g_gc_lowest_address = min(allatonce_block, separated_poh_block);
                g_gc_highest_address = max((allatonce_block + requestedMemory),
                    (separated_poh_block + separate_pinned_size));
                memory_details.allocation_pattern = initial_memory_details::ALLATONCE_SEPARATED_POH;
            }
            else
            {
                g_gc_lowest_address = allatonce_block;
                g_gc_highest_address = allatonce_block + requestedMemory;
                memory_details.allocation_pattern = initial_memory_details::ALLATONCE;
            }

            for (int i = 0; i < memory_details.block_count; i++)
            {
                memory_details.initial_normal_heap[i].memory_base = allatonce_block +
                    (i * normal_size);
                memory_details.initial_large_heap[i].memory_base = allatonce_block +
                    (memory_details.block_count * normal_size) + (i * large_size);
                if (separated_poh_p)
                {
                    memory_details.initial_pinned_heap[i].memory_base = separated_poh_block +
                        (i * pinned_size);
                }
                else
                {
                    memory_details.initial_pinned_heap[i].memory_base = allatonce_block +
                        (memory_details.block_count * (normal_size + large_size)) + (i * pinned_size);
                }
            }
            reserve_success = TRUE;
        }
        else
        {
            // try to allocate 3 blocks
            uint8_t* b1 = (uint8_t*)virtual_alloc(memory_details.block_count * normal_size, use_large_pages_p);
            uint8_t* b2 = (uint8_t*)virtual_alloc(memory_details.block_count * large_size, use_large_pages_p);
            uint8_t* b3 = (uint8_t*)virtual_alloc(memory_details.block_count * pinned_size, use_large_pages_p && !separated_poh_p);

            if (b1 && b2 && b3)
            {
                memory_details.allocation_pattern = initial_memory_details::EACH_GENERATION;
                g_gc_lowest_address = min(b1, min(b2, b3));
                g_gc_highest_address = max(b1 + memory_details.block_count * normal_size,
                    max(b2 + memory_details.block_count * large_size,
                        b3 + memory_details.block_count * pinned_size));

                for (int i = 0; i < memory_details.block_count; i++)
                {
                    memory_details.initial_normal_heap[i].memory_base = b1 + (i * normal_size);
                    memory_details.initial_large_heap[i].memory_base = b2 + (i * large_size);
                    memory_details.initial_pinned_heap[i].memory_base = b3 + (i * pinned_size);
                }

                reserve_success = TRUE;
            }
            else
            {
                // allocation failed, we'll go on to try allocating each block.
                // We could preserve the b1 alloc, but code complexity increases
                if (b1)
                    virtual_free(b1, memory_details.block_count * normal_size);
                if (b2)
                    virtual_free(b2, memory_details.block_count * large_size);
                if (b3)
                    virtual_free(b3, memory_details.block_count * pinned_size);
            }

            if ((b2 == NULL) && (memory_details.block_count > 1))
            {
                memory_details.allocation_pattern = initial_memory_details::EACH_BLOCK;

                imemory_data* current_block = memory_details.initial_memory;
                for (int i = 0; i < (memory_details.block_count * (total_generation_count - ephemeral_generation_count)); i++, current_block++)
                {
                    size_t block_size = memory_details.block_size(i);
                    uint16_t numa_node = NUMA_NODE_UNDEFINED;
                    if (heap_no_to_numa_node != nullptr)
                    {
                        int heap_no = i % memory_details.block_count;
                        numa_node = heap_no_to_numa_node[heap_no];
                    }
                    current_block->memory_base =
                        (uint8_t*)virtual_alloc(block_size, use_large_pages_p, numa_node);
                    if (current_block->memory_base == 0)
                    {
                        // Free the blocks that we've allocated so far
                        current_block = memory_details.initial_memory;
                        for (int j = 0; j < i; j++, current_block++) {
                            if (current_block->memory_base != 0) {
                                block_size = memory_details.block_size(i);
                                virtual_free(current_block->memory_base, block_size);
                            }
                        }
                        reserve_success = FALSE;
                        break;
                    }
                    else
                    {
                        if (current_block->memory_base < g_gc_lowest_address)
                            g_gc_lowest_address = current_block->memory_base;
                        if (((uint8_t*)current_block->memory_base + block_size) > g_gc_highest_address)
                            g_gc_highest_address = (current_block->memory_base + block_size);
                    }
                    reserve_success = TRUE;
                }
            }
        }
    }

    if (reserve_success && separated_poh_p)
    {
        for (int heap_no = 0; (reserve_success && (heap_no < num_heaps)); heap_no++)
        {
            if (!GCToOSInterface::VirtualCommit(memory_details.initial_pinned_heap[heap_no].memory_base, pinned_size))
            {
                reserve_success = FALSE;
            }
        }
    }

    return reserve_success;
}

void gc_heap::destroy_initial_memory()
{
    if (memory_details.initial_memory != NULL)
    {
        switch (memory_details.allocation_pattern)
        {
            case initial_memory_details::ALLATONCE:
                virtual_free (memory_details.initial_memory[0].memory_base,
                    memory_details.block_count*(memory_details.block_size_normal +
                    memory_details.block_size_large + memory_details.block_size_pinned));
                break;

            case initial_memory_details::ALLATONCE_SEPARATED_POH:
                virtual_free(memory_details.initial_memory[0].memory_base,
                    memory_details.block_count * (memory_details.block_size_normal +
                        memory_details.block_size_large));
                virtual_free(memory_details.initial_pinned_heap[0].memory_base,
                    memory_details.block_count * (memory_details.block_size_pinned));
                break;

            case initial_memory_details::EACH_GENERATION:
                virtual_free (memory_details.initial_normal_heap[0].memory_base,
                    memory_details.block_count*memory_details.block_size_normal);

                virtual_free (memory_details.initial_large_heap[0].memory_base,
                    memory_details.block_count*memory_details.block_size_large);

                virtual_free (memory_details.initial_pinned_heap[0].memory_base,
                    memory_details.block_count*memory_details.block_size_pinned);
                break;

            case initial_memory_details::EACH_BLOCK:
            {
                imemory_data* current_block = memory_details.initial_memory;
                int total_block_count = memory_details.block_count *
                    (total_generation_count - ephemeral_generation_count);
                for (int i = 0; i < total_block_count; i++, current_block++)
                {
                    size_t block_size = memory_details.block_size (i);
                    if (current_block->memory_base != NULL)
                    {
                        virtual_free (current_block->memory_base, block_size);
                    }
                }
                break;
            }
            case initial_memory_details::EACH_NUMA_NODE:
                for (int block_index = 0; block_index < memory_details.numa_reserved_block_count; block_index++)
                {
                    numa_reserved_block * block = &memory_details.numa_reserved_block_table[block_index];

                    if (block->memory_base != nullptr)
                    {
                        virtual_free (block->memory_base, block->block_size);
                    }
                }
                delete [] memory_details.numa_reserved_block_table;
                break;

            default:
                assert (!"unexpected allocation_pattern");
                break;
        }

        delete [] memory_details.initial_memory;
        memory_details.initial_memory = NULL;
        memory_details.initial_normal_heap = NULL;
        memory_details.initial_large_heap = NULL;
        memory_details.initial_pinned_heap = NULL;
    }
}

#ifndef USE_REGIONS
void gc_heap::release_segment (heap_segment* sg)
{
    ptrdiff_t delta = 0;
    FIRE_EVENT(GCFreeSegment_V1, heap_segment_mem(sg));
    size_t reserved_size = (uint8_t*)heap_segment_reserved (sg) - (uint8_t*)sg;
    reduce_committed_bytes (
        sg,
        ((uint8_t*)heap_segment_committed (sg) - (uint8_t*)sg),
        (int) heap_segment_oh (sg)
#ifdef MULTIPLE_HEAPS
        , heap_segment_heap (sg)->heap_number
#else
        , -1
#endif
        , true
        );
    virtual_free (sg, reserved_size, sg);
}

BOOL gc_heap::set_ro_segment_in_range (heap_segment* seg)
{
    seg->flags |= heap_segment_flags_inrange;
    ro_segments_in_range = TRUE;
    return TRUE;
}

#endif //!USE_REGIONS

heap_segment* gc_heap::get_segment_for_uoh (int gen_number, size_t size
#ifdef MULTIPLE_HEAPS
                                           , gc_heap* hp
#endif //MULTIPLE_HEAPS
                                           )
{
#ifndef MULTIPLE_HEAPS
    gc_heap* hp = 0;
#endif //MULTIPLE_HEAPS

#ifdef USE_REGIONS
    heap_segment* res = hp->get_new_region (gen_number, size);
#else //USE_REGIONS
    gc_oh_num oh = gen_to_oh (gen_number);
    heap_segment* res = hp->get_segment (size, oh);
#endif //USE_REGIONS

    if (res != 0)
    {
#ifdef MULTIPLE_HEAPS
        heap_segment_heap (res) = hp;
#endif //MULTIPLE_HEAPS

        size_t flags = (gen_number == poh_generation) ?
            heap_segment_flags_poh :
            heap_segment_flags_loh;

#ifdef USE_REGIONS
        // in the regions case, flags are set by get_new_region
        assert ((res->flags & (heap_segment_flags_loh | heap_segment_flags_poh)) == flags);
#else //USE_REGIONS
        res->flags |= flags;

        FIRE_EVENT(GCCreateSegment_V1,
            heap_segment_mem(res),
            (size_t)(heap_segment_reserved (res) - heap_segment_mem(res)),
            (gen_number == poh_generation) ?
                gc_etw_segment_pinned_object_heap :
                gc_etw_segment_large_object_heap);

#ifdef MULTIPLE_HEAPS
        hp->thread_uoh_segment (gen_number, res);
#else
        thread_uoh_segment (gen_number, res);
#endif //MULTIPLE_HEAPS
#endif //USE_REGIONS
        GCToEEInterface::DiagAddNewRegion(
                            gen_number,
                            heap_segment_mem (res),
                            heap_segment_allocated (res),
                            heap_segment_reserved (res)
                        );
    }

    return res;
}

void gc_heap::thread_uoh_segment (int gen_number, heap_segment* new_seg)
{
    heap_segment* seg = generation_allocation_segment (generation_of (gen_number));

    while (heap_segment_next_rw (seg))
        seg = heap_segment_next_rw (seg);

    heap_segment_next (seg) = new_seg;
}

#ifdef FEATURE_BASICFREEZE
// Note that we always insert at the head of the max_generation segment list.
BOOL gc_heap::insert_ro_segment (heap_segment* seg)
{
#ifdef FEATURE_EVENT_TRACE
    if (!use_frozen_segments_p)
        use_frozen_segments_p = true;
#endif //FEATURE_EVENT_TRACE

    enter_spin_lock (&gc_heap::gc_lock);

    if (!gc_heap::seg_table->ensure_space_for_insert ()
#ifdef BACKGROUND_GC
        || (is_bgc_in_progress() && !commit_mark_array_new_seg(__this, seg))
#endif //BACKGROUND_GC
        )
    {
        leave_spin_lock(&gc_heap::gc_lock);
        return FALSE;
    }

    generation* gen2 = generation_of (max_generation);
    heap_segment* oldhead = generation_start_segment (gen2);
    heap_segment_next (seg) = oldhead;
    generation_start_segment (gen2) = seg;

#ifdef USE_REGIONS
    dprintf (REGIONS_LOG, ("setting gen2 start seg to %zx(%p)->%p",
        (size_t)seg, heap_segment_mem (seg), heap_segment_mem (oldhead)));

    if (generation_tail_ro_region (gen2) == 0)
    {
        dprintf (REGIONS_LOG, ("setting gen2 tail ro -> %p", heap_segment_mem (seg)));
        generation_tail_ro_region (gen2) = seg;
    }
#endif //USE_REGIONS

    seg_table->insert (heap_segment_mem(seg), (size_t)seg);

    seg_mapping_table_add_ro_segment (seg);

#ifdef USE_REGIONS
    // For regions ro segments are always out of range.
    assert (!((heap_segment_reserved (seg) > lowest_address) &&
        (heap_segment_mem (seg) < highest_address)));
#else
    if ((heap_segment_reserved (seg) > lowest_address) &&
        (heap_segment_mem (seg) < highest_address))
    {
        set_ro_segment_in_range (seg);
    }
#endif //USE_REGIONS

    FIRE_EVENT(GCCreateSegment_V1, heap_segment_mem(seg), (size_t)(heap_segment_reserved (seg) - heap_segment_mem(seg)), gc_etw_segment_read_only_heap);

    leave_spin_lock (&gc_heap::gc_lock);
    return TRUE;
}

void gc_heap::update_ro_segment (heap_segment* seg, uint8_t* allocated, uint8_t* committed)
{
    enter_spin_lock (&gc_heap::gc_lock);

    assert (heap_segment_read_only_p (seg));
    assert (allocated <= committed);
    assert (committed <= heap_segment_reserved (seg));
    heap_segment_allocated (seg) = allocated;
    heap_segment_committed (seg) = committed;

    leave_spin_lock (&gc_heap::gc_lock);
}

// No one is calling this function right now. If this is getting called we need
// to take care of decommitting the mark array for it - we will need to remember
// which portion of the mark array was committed and only decommit that.
void gc_heap::remove_ro_segment (heap_segment* seg)
{
    //clear the mark bits so a new segment allocated in its place will have a clear mark bits
#ifdef BACKGROUND_GC
    if (gc_can_use_concurrent)
    {
        if ((seg->flags & heap_segment_flags_ma_committed) || (seg->flags & heap_segment_flags_ma_pcommitted))
        {
            seg_clear_mark_array_bits_soh (seg);
        }
    }
#endif //BACKGROUND_GC

    enter_spin_lock (&gc_heap::gc_lock);

    seg_table->remove (heap_segment_mem (seg));
    seg_mapping_table_remove_ro_segment (seg);

    // Locate segment (and previous segment) in the list.
    generation* gen2 = generation_of (max_generation);

#ifdef USE_REGIONS
    if (generation_tail_ro_region (gen2) == seg)
    {
        generation_tail_ro_region (gen2) = 0;
    }
#endif //USE_REGIONS

    heap_segment* curr_seg = generation_start_segment (gen2);
    heap_segment* prev_seg = NULL;

    while (curr_seg && curr_seg != seg)
    {
        prev_seg = curr_seg;
        curr_seg = heap_segment_next (curr_seg);
    }
    assert (curr_seg == seg);

    // Patch previous segment (or list head if there is none) to skip the removed segment.
    if (prev_seg)
        heap_segment_next (prev_seg) = heap_segment_next (curr_seg);
    else
        generation_start_segment (gen2) = heap_segment_next (curr_seg);

    leave_spin_lock (&gc_heap::gc_lock);
}

#endif //FEATURE_BASICFREEZE
#ifdef USE_REGIONS
void get_initial_region(int gen, int hn, uint8_t** region_start, uint8_t** region_end)
{
    *region_start = initial_regions[hn][gen][0];
    *region_end = initial_regions[hn][gen][1];
}

bool gc_heap::initial_make_soh_regions (gc_heap* hp)
{
    uint8_t* region_start;
    uint8_t* region_end;
    uint32_t hn = 0;
#ifdef MULTIPLE_HEAPS
    hn = hp->heap_number;
#endif //MULTIPLE_HEAPS

    for (int i = max_generation; i >= 0; i--)
    {
        get_initial_region(i, hn, &region_start, &region_end);

        size_t region_size = region_end - region_start;

        heap_segment* current_region = make_heap_segment (region_start, region_size, hp, i);
        if (current_region == nullptr)
        {
            return false;
        }
        uint8_t* gen_start = heap_segment_mem (current_region);
        make_generation (i, current_region, gen_start);

        if (i == 0)
        {
            ephemeral_heap_segment = current_region;
            alloc_allocated = heap_segment_allocated (current_region);
        }
    }

    for (int i = max_generation; i >= 0; i--)
    {
        dprintf (REGIONS_LOG, ("h%d gen%d alloc seg is %p, start seg is %p (%p-%p)",
            heap_number, i, generation_allocation_segment (generation_of (i)),
            generation_start_segment (generation_of (i)),
            heap_segment_mem (generation_start_segment (generation_of (i))),
            heap_segment_allocated (generation_start_segment (generation_of (i)))));
    }

    return true;
}

bool gc_heap::initial_make_uoh_regions (int gen, gc_heap* hp)
{
    uint8_t* region_start;
    uint8_t* region_end;
    uint32_t hn = 0;
#ifdef MULTIPLE_HEAPS
    hn = hp->heap_number;
#endif //MULTIPLE_HEAPS

    get_initial_region(gen, hn, &region_start, &region_end);

    size_t region_size = region_end - region_start;
    heap_segment* uoh_region = make_heap_segment (region_start, region_size, hp, gen);
    if (uoh_region == nullptr)
    {
        return false;
    }
    uoh_region->flags |=
        (gen == loh_generation) ? heap_segment_flags_loh : heap_segment_flags_poh;
    uint8_t* gen_start = heap_segment_mem (uoh_region);
    make_generation (gen, uoh_region, gen_start);
    return true;
}

void gc_heap::clear_region_info (heap_segment* region)
{
    if (!heap_segment_uoh_p (region))
    {
        //cleanup the brick table back to the empty value
        clear_brick_table (heap_segment_mem (region), heap_segment_reserved (region));
    }

    clear_card_for_addresses (get_region_start (region), heap_segment_reserved (region));

#ifdef BACKGROUND_GC
    ::record_changed_seg ((uint8_t*)region, heap_segment_reserved (region),
                        settings.gc_index, current_bgc_state,
                        seg_deleted);

    bgc_verify_mark_array_cleared (region);
#endif //BACKGROUND_GC
}

// Note that returning a region to free does not decommit.
void gc_heap::return_free_region (heap_segment* region)
{
    gc_oh_num oh = heap_segment_oh (region);
    dprintf(3, ("commit-accounting:  from %d to free [%p, %p) for heap %d", oh, get_region_start (region), heap_segment_committed (region), heap_number));
    {
        size_t committed = heap_segment_committed (region) - get_region_start (region);
        if (committed > 0)
        {
            check_commit_cs.Enter();
            assert (committed_by_oh[oh] >= committed);
            committed_by_oh[oh] -= committed;
            committed_by_oh[recorded_committed_free_bucket] += committed;
#if defined(MULTIPLE_HEAPS) && defined(_DEBUG)
            assert (committed_by_oh_per_heap[oh] >= committed);
            committed_by_oh_per_heap[oh] -= committed;
#endif // MULTIPLE_HEAPS && _DEBUG
            check_commit_cs.Leave();
        }
    }
    clear_region_info (region);

    region_free_list::add_region_descending (region, free_regions);

    uint8_t* region_start = get_region_start (region);
    uint8_t* region_end = heap_segment_reserved (region);

    int num_basic_regions = (int)((region_end - region_start) >> min_segment_size_shr);
    dprintf (REGIONS_LOG, ("RETURNING region %p (%d basic regions) to free",
        heap_segment_mem (region), num_basic_regions));
    for (int i = 0; i < num_basic_regions; i++)
    {
        uint8_t* basic_region_start = region_start + ((size_t)i << min_segment_size_shr);
        heap_segment* basic_region = get_region_info (basic_region_start);
        heap_segment_allocated (basic_region) = 0;
#ifdef MULTIPLE_HEAPS
        heap_segment_heap (basic_region) = 0;
#endif //MULTIPLE_HEAPS

        // I'm intentionally not resetting gen_num/plan_gen_num which will show us
        // which gen/plan gen this region was and that's useful for debugging.
    }
}

// USE_REGIONS TODO: SOH should be able to get a large region and split it up into basic regions
// if needed.
// USE_REGIONS TODO: In Server GC we should allow to get a free region from another heap.
heap_segment* gc_heap::get_free_region (int gen_number, size_t size)
{
    heap_segment* region = 0;

    if (gen_number <= max_generation)
    {
        assert (size == 0);
        region = free_regions[basic_free_region].unlink_region_front();
    }
    else
    {
        const size_t LARGE_REGION_SIZE = global_region_allocator.get_large_region_alignment();

        assert (size >= LARGE_REGION_SIZE);
        if (size == LARGE_REGION_SIZE)
        {
            // get it from the local list of large free regions if possible
            region = free_regions[large_free_region].unlink_region_front();
        }
        else
        {
            // get it from the local list of huge free regions if possible
            region = free_regions[huge_free_region].unlink_smallest_region (size);
            if (region == nullptr)
            {
                if (settings.pause_mode == pause_no_gc)
                {
                    // In case of no-gc-region, the gc lock is being held by the thread
                    // triggering the GC.
                    assert (gc_lock.holding_thread != (Thread*)-1);
                }
                else
                {
                    ASSERT_HOLDING_SPIN_LOCK(&gc_lock);
                }

                // get it from the global list of huge free regions
                region = global_free_huge_regions.unlink_smallest_region (size);
            }
        }
    }

    if (region)
    {
        uint8_t* region_start = get_region_start (region);
        uint8_t* region_end = heap_segment_reserved (region);
        init_heap_segment (region, __this, region_start,
                           (region_end - region_start),
                           gen_number, true);

        gc_oh_num oh = gen_to_oh (gen_number);
        dprintf(3, ("commit-accounting:  from free to %d [%p, %p) for heap %d", oh, get_region_start (region), heap_segment_committed (region), heap_number));
        {
            size_t committed = heap_segment_committed (region) - get_region_start (region);
            if (committed > 0)
            {
                check_commit_cs.Enter();
                committed_by_oh[oh] += committed;
                assert (committed_by_oh[recorded_committed_free_bucket] >= committed);
                committed_by_oh[recorded_committed_free_bucket] -= committed;
#if defined(MULTIPLE_HEAPS) && defined(_DEBUG)
                committed_by_oh_per_heap[oh] += committed;
#endif // MULTIPLE_HEAPS && _DEBUG
                check_commit_cs.Leave();
            }
        }

        dprintf (REGIONS_LOG, ("h%d GFR get region %zx (%p-%p) for gen%d",
            heap_number, (size_t)region,
            region_start, region_end,
            gen_number));

        // Something is wrong if a free region is already filled
        assert (heap_segment_allocated(region) == heap_segment_mem (region));
    }
    else
    {
        region = allocate_new_region (__this, gen_number, (gen_number > max_generation), size);
    }

    if (region)
    {
        if (!init_table_for_region (gen_number, region))
        {
            region = 0;
        }
    }

    return region;
}

// Note that this gets the basic region index for obj. If the obj is in a large region,
// this region may not be the start of it.
heap_segment* gc_heap::region_of (uint8_t* obj)
{
    size_t index = (size_t)obj >> gc_heap::min_segment_size_shr;
    seg_mapping* entry = &seg_mapping_table[index];

    return (heap_segment*)entry;
}

heap_segment* gc_heap::get_region_at_index (size_t index)
{
    index += (size_t)g_gc_lowest_address >> gc_heap::min_segment_size_shr;
    return (heap_segment*)(&seg_mapping_table[index]);
}

// For debugging purposes to check that a region looks sane and
// do some logging. This was useful to sprinkle in various places
// where we were threading regions.
void gc_heap::check_seg_gen_num (heap_segment* seg)
{
#ifdef _DEBUG
    uint8_t* mem = heap_segment_mem (seg);

    if ((mem < g_gc_lowest_address) || (mem >= g_gc_highest_address))
    {
        GCToOSInterface::DebugBreak();
    }

    int alloc_seg_gen_num = get_region_gen_num (mem);
    int alloc_seg_plan_gen_num = get_region_plan_gen_num (mem);
    dprintf (3, ("seg %p->%p, num %d, %d",
        seg, mem, alloc_seg_gen_num, alloc_seg_plan_gen_num));
#endif //_DEBUG
}

int gc_heap::get_region_gen_num (heap_segment* region)
{
    return heap_segment_gen_num (region);
}

int gc_heap::get_region_gen_num (uint8_t* obj)
{
    size_t skewed_basic_region_index = get_skewed_basic_region_index_for_address (obj);
    int gen_num = map_region_to_generation_skewed[skewed_basic_region_index] & gc_heap::RI_GEN_MASK;
    assert ((soh_gen0 <= gen_num) && (gen_num <= soh_gen2));
    assert (gen_num == heap_segment_gen_num (region_of (obj)));
    return gen_num;
}

int gc_heap::get_region_plan_gen_num (uint8_t* obj)
{
    size_t skewed_basic_region_index = get_skewed_basic_region_index_for_address (obj);
    int plan_gen_num = map_region_to_generation_skewed[skewed_basic_region_index] >> gc_heap::RI_PLAN_GEN_SHR;
    assert ((soh_gen0 <= plan_gen_num) && (plan_gen_num <= soh_gen2));
    assert (plan_gen_num == heap_segment_plan_gen_num (region_of (obj)));
    return plan_gen_num;
}

bool gc_heap::is_region_demoted (uint8_t* obj)
{
    size_t skewed_basic_region_index = get_skewed_basic_region_index_for_address (obj);
    bool demoted_p = (map_region_to_generation_skewed[skewed_basic_region_index] & gc_heap::RI_DEMOTED) != 0;
    assert (demoted_p == heap_segment_demoted_p (region_of (obj)));
    return demoted_p;
}

inline
void gc_heap::set_region_gen_num (heap_segment* region, int gen_num)
{
    assert (gen_num < (1 << (sizeof (uint8_t) * 8)));
    assert (gen_num >= 0);
    heap_segment_gen_num (region) = (uint8_t)gen_num;

    uint8_t* region_start = get_region_start (region);
    uint8_t* region_end = heap_segment_reserved (region);

    size_t region_index_start = get_basic_region_index_for_address (region_start);
    size_t region_index_end = get_basic_region_index_for_address (region_end);
    region_info entry = (region_info)((gen_num << RI_PLAN_GEN_SHR) | gen_num);
    for (size_t region_index = region_index_start; region_index < region_index_end; region_index++)
    {
        assert (gen_num <= max_generation);
        map_region_to_generation[region_index] = entry;
    }
    if (gen_num <= soh_gen1)
    {
        if ((region_start < ephemeral_low) || (ephemeral_high < region_end))
        {
            while (true)
            {
                if (Interlocked::CompareExchange(&write_barrier_spin_lock.lock, 0, -1) < 0)
                    break;

                if ((ephemeral_low <= region_start) && (region_end <= ephemeral_high))
                    return;

                while (write_barrier_spin_lock.lock >= 0)
                {
                    YieldProcessor();           // indicate to the processor that we are spinning
                }
            }
#ifdef _DEBUG
            write_barrier_spin_lock.holding_thread = GCToEEInterface::GetThread();
#endif //_DEBUG

            if ((region_start < ephemeral_low) || (ephemeral_high < region_end))
            {
                uint8_t* new_ephemeral_low = min (region_start, (uint8_t*)ephemeral_low);
                uint8_t* new_ephemeral_high = max (region_end, (uint8_t*)ephemeral_high);

                dprintf (REGIONS_LOG, ("about to set ephemeral_low = %p ephemeral_high = %p", new_ephemeral_low, new_ephemeral_high));

                stomp_write_barrier_ephemeral (new_ephemeral_low, new_ephemeral_high,
                                               map_region_to_generation_skewed, (uint8_t)min_segment_size_shr);

                // we should only *decrease* ephemeral_low and only *increase* ephemeral_high
                if (ephemeral_low < new_ephemeral_low)
                    GCToOSInterface::DebugBreak ();
                if (new_ephemeral_high < ephemeral_high)
                    GCToOSInterface::DebugBreak ();

                // only set the globals *after* we have updated the write barrier
                ephemeral_low = new_ephemeral_low;
                ephemeral_high = new_ephemeral_high;

                dprintf (REGIONS_LOG, ("set ephemeral_low = %p ephemeral_high = %p", new_ephemeral_low, new_ephemeral_high));
            }
            else
            {
                dprintf (REGIONS_LOG, ("leaving lock - no need to update ephemeral range [%p,%p[ for region [%p,%p]", (uint8_t*)ephemeral_low, (uint8_t*)ephemeral_high, region_start, region_end));
            }
#ifdef _DEBUG
            write_barrier_spin_lock.holding_thread = (Thread*)-1;
#endif //_DEBUG
            write_barrier_spin_lock.lock = -1;
        }
        else
        {
            dprintf (REGIONS_LOG, ("no need to update ephemeral range [%p,%p[ for region [%p,%p]", (uint8_t*)ephemeral_low, (uint8_t*)ephemeral_high, region_start, region_end));
        }
    }
}

inline
void gc_heap::set_region_plan_gen_num (heap_segment* region, int plan_gen_num, bool replace_p)
{
    int gen_num = heap_segment_gen_num (region);
    int supposed_plan_gen_num = get_plan_gen_num (gen_num);
    dprintf (REGIONS_LOG, ("h%d setting plan gen on %p->%p(was gen%d) to %d(should be: %d) %s",
        heap_number, region,
        heap_segment_mem (region),
        gen_num, plan_gen_num,
        supposed_plan_gen_num,
        ((plan_gen_num < supposed_plan_gen_num) ? "DEMOTED" : "ND")));
    region_info region_info_bits_to_set = (region_info)(plan_gen_num << RI_PLAN_GEN_SHR);
    if ((plan_gen_num < supposed_plan_gen_num) && (heap_segment_pinned_survived (region) != 0))
    {
        if (!settings.demotion)
        {
            settings.demotion = TRUE;
        }
        get_gc_data_per_heap()->set_mechanism_bit (gc_demotion_bit);
        region->flags |= heap_segment_flags_demoted;
        region_info_bits_to_set = (region_info)(region_info_bits_to_set | RI_DEMOTED);
    }
    else
    {
        region->flags &= ~heap_segment_flags_demoted;
    }

    // If replace_p is true, it means we need to move a region from its original planned gen to this new gen.
    if (replace_p)
    {
        int original_plan_gen_num = heap_segment_plan_gen_num (region);
        planned_regions_per_gen[original_plan_gen_num]--;
    }

    planned_regions_per_gen[plan_gen_num]++;
    dprintf (REGIONS_LOG, ("h%d g%d %zx(%zx) -> g%d (total %d region planned in g%d)",
        heap_number, heap_segment_gen_num (region), (size_t)region, heap_segment_mem (region), plan_gen_num, planned_regions_per_gen[plan_gen_num], plan_gen_num));

    heap_segment_plan_gen_num (region) = plan_gen_num;

    uint8_t* region_start = get_region_start (region);
    uint8_t* region_end = heap_segment_reserved (region);

    size_t region_index_start = get_basic_region_index_for_address (region_start);
    size_t region_index_end = get_basic_region_index_for_address (region_end);
    for (size_t region_index = region_index_start; region_index < region_index_end; region_index++)
    {
        assert (plan_gen_num <= max_generation);
        map_region_to_generation[region_index] = (region_info)(region_info_bits_to_set | (map_region_to_generation[region_index] & ~(RI_PLAN_GEN_MASK|RI_DEMOTED)));
    }
}

inline
void gc_heap::set_region_plan_gen_num_sip (heap_segment* region, int plan_gen_num)
{
    if (!heap_segment_swept_in_plan (region))
    {
        set_region_plan_gen_num (region, plan_gen_num);
    }
}

void gc_heap::set_region_sweep_in_plan (heap_segment*region)
{
    heap_segment_swept_in_plan (region) = true;

    // this should be a basic region
    assert (get_region_size (region) == global_region_allocator.get_region_alignment());

    uint8_t* region_start = get_region_start (region);
    size_t region_index = get_basic_region_index_for_address (region_start);
    map_region_to_generation[region_index] = (region_info)(map_region_to_generation[region_index] | RI_SIP);
}

void gc_heap::clear_region_sweep_in_plan (heap_segment*region)
{
    heap_segment_swept_in_plan (region) = false;

    // this should be a basic region
    assert (get_region_size (region) == global_region_allocator.get_region_alignment());

    uint8_t* region_start = get_region_start (region);
    size_t region_index = get_basic_region_index_for_address (region_start);
    map_region_to_generation[region_index] = (region_info)(map_region_to_generation[region_index] & ~RI_SIP);
}

void gc_heap::clear_region_demoted (heap_segment* region)
{
    region->flags &= ~heap_segment_flags_demoted;

    // this should be a basic region
    assert (get_region_size (region) == global_region_allocator.get_region_alignment());

    uint8_t* region_start = get_region_start (region);
    size_t region_index = get_basic_region_index_for_address (region_start);
    map_region_to_generation[region_index] = (region_info)(map_region_to_generation[region_index] & ~RI_DEMOTED);
}

#endif //USE_REGIONS

int gc_heap::get_plan_gen_num (int gen_number)
{
    return ((settings.promotion) ? min ((gen_number + 1), (int)max_generation) : gen_number);
}

uint8_t* gc_heap::get_uoh_start_object (heap_segment* region, generation* gen)
{
#ifdef USE_REGIONS
    uint8_t* o = heap_segment_mem (region);
#else
    uint8_t* o = generation_allocation_start (gen);
    assert(((CObjectHeader*)o)->IsFree());
    size_t s = Align (size (o), get_alignment_constant (FALSE));
    assert (s == AlignQword (min_obj_size));
    //Skip the generation gap object
    o += s;
#endif //USE_REGIONS
    return o;
}

uint8_t* gc_heap::get_soh_start_object (heap_segment* region, generation* gen)
{
#ifdef USE_REGIONS
    uint8_t* o             = heap_segment_mem (region);
#else
    uint8_t* o             = generation_allocation_start (gen);
#endif //USE_REGIONS
    return o;
}

size_t gc_heap::get_soh_start_obj_len (uint8_t* start_obj)
{
#ifdef USE_REGIONS
    return 0;
#else
    return Align (size (start_obj));
#endif //USE_REGIONS
}

heap_segment* gc_heap::make_heap_segment (uint8_t* new_pages, size_t size, gc_heap* hp, int gen_num)
{
    gc_oh_num oh = gen_to_oh (gen_num);
    size_t initial_commit = use_large_pages_p ? size : SEGMENT_INITIAL_COMMIT;
    int h_number =
#ifdef MULTIPLE_HEAPS
        hp->heap_number;
#else
        0;
#endif //MULTIPLE_HEAPS

    if (!virtual_commit (new_pages, initial_commit, oh, h_number))
    {
        log_init_error_to_host ("Committing %zd bytes for a region failed", initial_commit);
        return 0;
    }

#ifdef USE_REGIONS
    dprintf (REGIONS_LOG, ("Making region %p->%p(%zdmb)",
        new_pages, (new_pages + size), (size / 1024 / 1024)));
    heap_segment* new_segment = get_region_info (new_pages);
    uint8_t* start = new_pages + sizeof (aligned_plug_and_gap);
#else
    heap_segment* new_segment = (heap_segment*)new_pages;
    uint8_t* start = new_pages + segment_info_size;
#endif //USE_REGIONS
    heap_segment_mem (new_segment) = start;
    heap_segment_used (new_segment) = start;
    heap_segment_reserved (new_segment) = new_pages + size;
    heap_segment_committed (new_segment) = new_pages + initial_commit;

    init_heap_segment (new_segment, hp
#ifdef USE_REGIONS
                       , new_pages, size, gen_num
#endif //USE_REGIONS
                       );
    dprintf (2, ("Creating heap segment %zx", (size_t)new_segment));

    return new_segment;
}

void gc_heap::init_heap_segment (heap_segment* seg, gc_heap* hp
#ifdef USE_REGIONS
                                 , uint8_t* start, size_t size, int gen_num, bool existing_region_p
#endif //USE_REGIONS
    )
{
#ifndef USE_REGIONS
    bool existing_region_p = false;
#endif //!USE_REGIONS
#ifdef BACKGROUND_GC
    seg->flags = existing_region_p ? (seg->flags & heap_segment_flags_ma_committed) : 0;
#else
    seg->flags = 0;
#endif
    heap_segment_next (seg) = 0;
    heap_segment_plan_allocated (seg) = heap_segment_mem (seg);
    heap_segment_allocated (seg) = heap_segment_mem (seg);
    heap_segment_saved_allocated (seg) = heap_segment_mem (seg);
#if !defined(USE_REGIONS) || defined(MULTIPLE_HEAPS)
    heap_segment_decommit_target (seg) = heap_segment_reserved (seg);
#endif //!USE_REGIONS || MULTIPLE_HEAPS
#ifdef BACKGROUND_GC
    heap_segment_background_allocated (seg) = 0;
    heap_segment_saved_bg_allocated (seg) = 0;
#endif //BACKGROUND_GC

#ifdef MULTIPLE_HEAPS
    heap_segment_heap (seg) = hp;
#endif //MULTIPLE_HEAPS

#ifdef USE_REGIONS
    int gen_num_for_region = min (gen_num, (int)max_generation);
    set_region_gen_num (seg, gen_num_for_region);
    heap_segment_plan_gen_num (seg) = gen_num_for_region;
    heap_segment_swept_in_plan (seg) = false;
    int num_basic_regions = (int)(size >> min_segment_size_shr);
    size_t basic_region_size = (size_t)1 << min_segment_size_shr;
    dprintf (REGIONS_LOG, ("this region contains %d basic regions", num_basic_regions));
    if (num_basic_regions > 1)
    {
        for (int i = 1; i < num_basic_regions; i++)
        {
            uint8_t* basic_region_start = start + (i * basic_region_size);
            heap_segment* basic_region = get_region_info (basic_region_start);
            heap_segment_allocated (basic_region) = (uint8_t*)(ptrdiff_t)-i;
            dprintf (REGIONS_LOG, ("Initing basic region %p->%p(%zdmb) alloc to %p",
                basic_region_start, (basic_region_start + basic_region_size),
                (size_t)(basic_region_size / 1024 / 1024),
                heap_segment_allocated (basic_region)));

            heap_segment_gen_num (basic_region) = (uint8_t)gen_num_for_region;
            heap_segment_plan_gen_num (basic_region) = gen_num_for_region;

#ifdef MULTIPLE_HEAPS
            heap_segment_heap (basic_region) = hp;
#endif //MULTIPLE_HEAPS
        }
    }
#endif //USE_REGIONS
}

//Releases the segment to the OS.
// this is always called on one thread only so calling seg_table->remove is fine.
void gc_heap::delete_heap_segment (heap_segment* seg, BOOL consider_hoarding)
{
    if (!heap_segment_uoh_p (seg))
    {
        //cleanup the brick table back to the empty value
        clear_brick_table (heap_segment_mem (seg), heap_segment_reserved (seg));
    }

#ifdef USE_REGIONS
    return_free_region (seg);
#else // USE_REGIONS
    if (consider_hoarding)
    {
        assert ((heap_segment_mem (seg) - (uint8_t*)seg) <= ptrdiff_t(2*OS_PAGE_SIZE));
        size_t ss = (size_t) (heap_segment_reserved (seg) - (uint8_t*)seg);
        //Don't keep the big ones.
        if (ss <= INITIAL_ALLOC)
        {
            dprintf (2, ("Hoarding segment %zx", (size_t)seg));
#ifdef BACKGROUND_GC
            // We don't need to clear the decommitted flag because when this segment is used
            // for a new segment the flags will be cleared.
            if (!heap_segment_decommitted_p (seg))
#endif //BACKGROUND_GC
            {
                decommit_heap_segment (seg);
            }

            seg_mapping_table_remove_segment (seg);

            heap_segment_next (seg) = segment_standby_list;
            segment_standby_list = seg;
            seg = 0;
        }
    }

    if (seg != 0)
    {
        dprintf (2, ("h%d: del seg: [%zx, %zx[",
                     heap_number, (size_t)seg,
                     (size_t)(heap_segment_reserved (seg))));

#ifdef BACKGROUND_GC
        ::record_changed_seg ((uint8_t*)seg, heap_segment_reserved (seg),
                            settings.gc_index, current_bgc_state,
                            seg_deleted);
        bgc_verify_mark_array_cleared (seg);

        decommit_mark_array_by_seg (seg);
#endif //BACKGROUND_GC

        seg_mapping_table_remove_segment (seg);
        release_segment (seg);
    }
#endif //USE_REGIONS
}

//resets the pages beyond allocates size so they won't be swapped out and back in
void gc_heap::reset_heap_segment_pages (heap_segment* seg)
{
    size_t page_start = align_on_page ((size_t)heap_segment_allocated (seg));
    size_t size = (size_t)heap_segment_committed (seg) - page_start;
    if (size != 0)
        GCToOSInterface::VirtualReset((void*)page_start, size, false /* unlock */);
}

void gc_heap::decommit_heap_segment_pages (heap_segment* seg,
                                           size_t extra_space)
{
    if (use_large_pages_p)
        return;

    uint8_t*  page_start = align_on_page (heap_segment_allocated(seg));
    assert (heap_segment_committed (seg) >= page_start);

    size_t size = heap_segment_committed (seg) - page_start;
    extra_space = align_on_page (extra_space);
    if (size >= max ((extra_space + 2*OS_PAGE_SIZE), MIN_DECOMMIT_SIZE))
    {
        page_start += max(extra_space, 32*OS_PAGE_SIZE);
        decommit_heap_segment_pages_worker (seg, page_start);
    }
}

size_t gc_heap::decommit_heap_segment_pages_worker (heap_segment* seg,
                                                    uint8_t* new_committed)
{
    assert (!use_large_pages_p);
    uint8_t* page_start = align_on_page (new_committed);
    ptrdiff_t size = heap_segment_committed (seg) - page_start;
    if (size > 0)
    {
        bool decommit_succeeded_p = virtual_decommit (page_start, (size_t)size, heap_segment_oh (seg), heap_number);
        if (decommit_succeeded_p)
        {
            dprintf (3, ("Decommitting heap segment [%zx, %zx[(%zd)",
                (size_t)page_start,
                (size_t)(page_start + size),
                size));
            heap_segment_committed (seg) = page_start;
            if (heap_segment_used (seg) > heap_segment_committed (seg))
            {
                heap_segment_used (seg) = heap_segment_committed (seg);
            }
        }
        else
        {
            dprintf (3, ("Decommitting heap segment failed"));
        }
    }
    return size;
}

//decommit all pages except one or 2
void gc_heap::decommit_heap_segment (heap_segment* seg)
{
#ifdef USE_REGIONS
    if (!dt_high_memory_load_p())
    {
        return;
    }
#endif

    uint8_t*  page_start = align_on_page (heap_segment_mem (seg));

    dprintf (3, ("Decommitting heap segment %zx(%p)", (size_t)seg, heap_segment_mem (seg)));

#if defined(BACKGROUND_GC) && !defined(USE_REGIONS)
    page_start += OS_PAGE_SIZE;
#endif //BACKGROUND_GC && !USE_REGIONS

    assert (heap_segment_committed (seg) >= page_start);
    size_t size = heap_segment_committed (seg) - page_start;
    bool decommit_succeeded_p = virtual_decommit (page_start, size, heap_segment_oh (seg), heap_number);

    if (decommit_succeeded_p)
    {
        //re-init the segment object
        heap_segment_committed (seg) = page_start;
        if (heap_segment_used (seg) > heap_segment_committed (seg))
        {
            heap_segment_used (seg) = heap_segment_committed (seg);
        }
    }
}

#ifdef BACKGROUND_GC
void gc_heap::rearrange_small_heap_segments()
{
    heap_segment* seg = freeable_soh_segment;
    while (seg)
    {
        heap_segment* next_seg = heap_segment_next (seg);
        // TODO: we need to consider hoarding here.
        delete_heap_segment (seg, FALSE);
        seg = next_seg;
    }
    freeable_soh_segment = 0;
}

#endif //BACKGROUND_GC

void gc_heap::rearrange_uoh_segments()
{
    dprintf (2, ("deleting empty large segments"));
    heap_segment* seg = freeable_uoh_segment;
    while (seg)
    {
        heap_segment* next_seg = heap_segment_next (seg);
        delete_heap_segment (seg, GCConfig::GetRetainVM());
        seg = next_seg;
    }
    freeable_uoh_segment = 0;
}

void gc_heap::delay_free_segments()
{
    rearrange_uoh_segments();
#ifdef BACKGROUND_GC
    background_delay_delete_uoh_segments();
    if (!gc_heap::background_running_p())
        rearrange_small_heap_segments();
#endif //BACKGROUND_GC
}

#ifndef USE_REGIONS
void gc_heap::rearrange_heap_segments(BOOL compacting)
{
    heap_segment* seg =
        generation_start_segment (generation_of (max_generation));

    heap_segment* prev_seg = 0;
    heap_segment* next_seg = 0;
    while (seg)
    {
        next_seg = heap_segment_next (seg);

        //link ephemeral segment when expanding
        if ((next_seg == 0) && (seg != ephemeral_heap_segment))
        {
            seg->next = ephemeral_heap_segment;
            next_seg = heap_segment_next (seg);
        }

        //re-used expanded heap segment
        if ((seg == ephemeral_heap_segment) && next_seg)
        {
            heap_segment_next (prev_seg) = next_seg;
            heap_segment_next (seg) = 0;
        }
        else
        {
            uint8_t* end_segment = (compacting ?
                                 heap_segment_plan_allocated (seg) :
                                 heap_segment_allocated (seg));
            // check if the segment was reached by allocation
            if ((end_segment == heap_segment_mem (seg))&&
                !heap_segment_read_only_p (seg))
            {
                //if not, unthread and delete
                assert (prev_seg);
                assert (seg != ephemeral_heap_segment);
                heap_segment_next (prev_seg) = next_seg;
                delete_heap_segment (seg, GCConfig::GetRetainVM());

                dprintf (2, ("Deleting heap segment %zx", (size_t)seg));
            }
            else
            {
                if (!heap_segment_read_only_p (seg))
                {
                    if (compacting)
                    {
                        heap_segment_allocated (seg) =
                            heap_segment_plan_allocated (seg);
                    }

                    // reset the pages between allocated and committed.
                    if (seg != ephemeral_heap_segment)
                    {
                        decommit_heap_segment_pages (seg, 0);
                    }
                }
                prev_seg = seg;
            }
        }

        seg = next_seg;
    }
}

#endif //!USE_REGIONS
#if defined(USE_REGIONS)
// trim down the list of regions pointed at by src down to target_count, moving the extra ones to dest
static void trim_region_list (region_free_list* dest, region_free_list* src, size_t target_count)
{
    while (src->get_num_free_regions() > target_count)
    {
        heap_segment* region = src->unlink_region_front();
        dest->add_region_front (region);
    }
}

// add regions from src to dest, trying to grow the size of dest to target_count
static int64_t grow_region_list (region_free_list* dest, region_free_list* src, size_t target_count)
{
    int64_t added_count = 0;
    while (dest->get_num_free_regions() < target_count)
    {
        if (src->get_num_free_regions() == 0)
            break;

        added_count++;

        heap_segment* region = src->unlink_region_front();
        dest->add_region_front (region);
    }
    return added_count;
}

void gc_heap::age_free_regions (const char* msg)
{
    // If we are doing an ephemeral GC as a precursor to a BGC, then we will age all of the region
    // kinds during the ephemeral GC and skip the call to age_free_regions during the BGC itself.
    bool age_all_region_kinds = (settings.condemned_generation == max_generation);

    if (!age_all_region_kinds)
    {
#ifdef MULTIPLE_HEAPS
        gc_heap* hp = g_heaps[0];
#else //MULTIPLE_HEAPS
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        age_all_region_kinds = (hp->current_bgc_state == bgc_initialized);
    }

    if (age_all_region_kinds)
    {
        global_free_huge_regions.age_free_regions();
    }

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
        const int i = 0;
#endif //MULTIPLE_HEAPS

        if (age_all_region_kinds)
        {
            // age and print all kinds of free regions
            region_free_list::age_free_regions (hp->free_regions);
            region_free_list::print (hp->free_regions, i, msg);
        }
        else
        {
            // age and print only basic free regions
            hp->free_regions[basic_free_region].age_free_regions();
            hp->free_regions[basic_free_region].print (i, msg);
        }
    }
}

// distribute_free_regions is called during all blocking GCs and in the start of the BGC mark phase
// unless we already called it during an ephemeral GC right before the BGC.
//
// Free regions are stored on the following permanent lists:
// - global_regions_to_decommit
// - global_free_huge_regions
// - (per-heap) free_regions
// and the following lists that are local to distribute_free_regions:
// - aged_regions
// - surplus_regions
//
// For reason_induced_aggressive GCs, we decommit all regions.  Therefore, the below description is
// for other GC types.
//
// distribute_free_regions steps:
//
// 1. Process region ages
//    a. Move all huge regions from free_regions to global_free_huge_regions.
//       (The intention is that free_regions shouldn't contain any huge regions outside of the period
//       where a GC reclaims them and distribute_free_regions moves them to global_free_huge_regions,
//       though perhaps BGC can leave them there.  Future work could verify and assert this.)
//    b. Move any basic region in global_regions_to_decommit (which means we intended to decommit them
//       but haven't done so yet) to surplus_regions
//    c. Move all huge regions that are past the age threshold from global_free_huge_regions to aged_regions
//    d. Move all basic/large regions that are past the age threshold from free_regions to aged_regions
// 2. Move all regions from aged_regions to global_regions_to_decommit.  Note that the intention is to
//    combine this with move_highest_free_regions in a future change, which is why we don't just do this
//    in steps 1c/1d.
// 3. Compute the required per-heap budgets for SOH (basic regions) and the balance.  The budget for LOH
//    (large) is zero as we are using an entirely age-based approach.
//        balance = (number of free regions) - budget
// 4. Decide if we are going to distribute or decommit a nonzero balance.  To distribute, we adjust the
//    per-heap budgets, so after this step the LOH (large) budgets can be positive.
//    a. A negative balance (deficit) for SOH (basic) will be distributed it means we expect to use
//       more memory than we have on the free lists.  A negative balance for LOH (large) isn't possible
//       for LOH since the budgets start at zero.
//    b. For SOH (basic), we will decommit surplus regions unless we are in a foreground GC during BGC.
//    c. For LOH (large), we will distribute surplus regions since we are using an entirely age-based
//       approach.  However, if we are in a high-memory-usage scenario, we will decommit.  In this case,
//       we will also decommit the huge regions in global_free_huge_regions.  Note that they were not
//       originally included in the balance because they are kept in a global list.  Only basic/large
//       regions are kept in per-heap lists where they can be distributed.
// 5. Implement the distribute-or-decommit strategy.  To distribute, we simply move regions across heaps,
//    using surplus_regions as a holding space.  To decommit, for server GC we generally leave them on the
//    global_regions_to_decommit list and decommit them over time.  However, in high-memory-usage scenarios,
//    we will immediately decommit some or all of these regions.  For workstation GC, we decommit a limited
//    amount and move the rest back to the (one) heap's free_list.
void gc_heap::distribute_free_regions()
{
#ifdef MULTIPLE_HEAPS
    BOOL joined_last_gc_before_oom = FALSE;
    for (int i = 0; i < n_heaps; i++)
    {
        if (g_heaps[i]->last_gc_before_oom)
        {
            joined_last_gc_before_oom = TRUE;
            break;
        }
    }
#else
    BOOL joined_last_gc_before_oom = last_gc_before_oom;
#endif //MULTIPLE_HEAPS
    if (settings.reason == reason_induced_aggressive)
    {
        global_regions_to_decommit[huge_free_region].transfer_regions (&global_free_huge_regions);

#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
        {
            gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
            for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
            {
                global_regions_to_decommit[kind].transfer_regions (&hp->free_regions[kind]);
            }
        }
        while (decommit_step(DECOMMIT_TIME_STEP_MILLISECONDS))
        {
        }
#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
            int hn = i;
#else //MULTIPLE_HEAPS
        {
            gc_heap* hp = pGenGCHeap;
            int hn  = 0;
#endif //MULTIPLE_HEAPS
            for (int i = 0; i < total_generation_count; i++)
            {
                generation* generation = hp->generation_of (i);
                heap_segment* region = heap_segment_rw (generation_start_segment (generation));
                while (region != nullptr)
                {
                    uint8_t* aligned_allocated = align_on_page (heap_segment_allocated (region));
                    size_t end_space = heap_segment_committed (region) - aligned_allocated;
                    if (end_space > 0)
                    {
                        virtual_decommit (aligned_allocated, end_space, gen_to_oh (i), hn);
                        heap_segment_committed (region) = aligned_allocated;
                        heap_segment_used (region) = min (heap_segment_used (region), heap_segment_committed (region));
                        assert (heap_segment_committed (region) > heap_segment_mem (region));
                    }
                    region = heap_segment_next_rw (region);
                }
            }
        }

        return;
    }

    // first step: accumulate the number of free regions and the budget over all heaps
    //
    // The initial budget will only be calculated for basic free regions.  For large regions, the initial budget
    // is zero, and distribute-vs-decommit will be determined entirely by region ages and whether we are in a
    // high memory usage scenario.  Distributing a surplus/deficit of regions can change the budgets that are used.
    size_t total_num_free_regions[count_distributed_free_region_kinds] = { 0, 0 };
    size_t total_budget_in_region_units[count_distributed_free_region_kinds] = { 0, 0 };

    size_t heap_budget_in_region_units[count_distributed_free_region_kinds][MAX_SUPPORTED_HEAPS] = {};
    size_t min_heap_budget_in_region_units[count_distributed_free_region_kinds][MAX_SUPPORTED_HEAPS] = {};
    region_free_list aged_regions[count_free_region_kinds];
    region_free_list surplus_regions[count_distributed_free_region_kinds];

    // we may still have regions left on the regions_to_decommit list -
    // use these to fill the budget as well
    surplus_regions[basic_free_region].transfer_regions (&global_regions_to_decommit[basic_free_region]);

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        global_free_huge_regions.transfer_regions (&hp->free_regions[huge_free_region]);
    }

    move_all_aged_regions(total_num_free_regions, aged_regions, joined_last_gc_before_oom);
    // For now, we just decommit right away, but eventually these will be used in move_highest_free_regions
    move_regions_to_decommit(aged_regions);

    size_t total_basic_free_regions = total_num_free_regions[basic_free_region] + surplus_regions[basic_free_region].get_num_free_regions();
    total_budget_in_region_units[basic_free_region] = compute_basic_region_budgets(heap_budget_in_region_units[basic_free_region], min_heap_budget_in_region_units[basic_free_region], total_basic_free_regions);

    bool aggressive_decommit_large_p = joined_last_gc_before_oom || dt_high_memory_load_p() || near_heap_hard_limit_p();

    int region_factor[count_distributed_free_region_kinds] = { 1, LARGE_REGION_FACTOR };

#ifndef MULTIPLE_HEAPS
    // just to reduce the number of #ifdefs in the code below
    const int n_heaps = 1;
#endif //!MULTIPLE_HEAPS

    for (int kind = basic_free_region; kind < count_distributed_free_region_kinds; kind++)
    {
        dprintf(REGIONS_LOG, ("%zd %s free regions, %zd regions budget, %zd regions on surplus list",
            total_num_free_regions[kind],
            free_region_kind_name[kind],
            total_budget_in_region_units[kind],
            surplus_regions[kind].get_num_free_regions()));

        // check if the free regions exceed the budget
        // if so, put the highest free regions on the decommit list
        total_num_free_regions[kind] += surplus_regions[kind].get_num_free_regions();

        ptrdiff_t balance_to_distribute = total_num_free_regions[kind] - total_budget_in_region_units[kind];

        if (distribute_surplus_p(balance_to_distribute, kind, aggressive_decommit_large_p))
        {
#ifdef MULTIPLE_HEAPS
            // we may have a deficit or - for large regions or if background GC is going on - a surplus.
            // adjust the budget per heap accordingly
            if (balance_to_distribute != 0)
            {
                dprintf (REGIONS_LOG, ("distributing the %zd %s regions deficit", -balance_to_distribute, free_region_kind_name[kind]));

                ptrdiff_t curr_balance = 0;
                ptrdiff_t rem_balance = 0;
                for (int i = 0; i < n_heaps; i++)
                {
                    curr_balance += balance_to_distribute;
                    ptrdiff_t adjustment_per_heap = curr_balance / n_heaps;
                    curr_balance -= adjustment_per_heap * n_heaps;
                    ptrdiff_t new_budget = (ptrdiff_t)heap_budget_in_region_units[kind][i] + adjustment_per_heap;
                    ptrdiff_t min_budget = (ptrdiff_t)min_heap_budget_in_region_units[kind][i];
                    dprintf (REGIONS_LOG, ("adjusting the budget for heap %d from %zd %s regions by %zd to %zd",
                        i,
                        heap_budget_in_region_units[kind][i],
                        free_region_kind_name[kind],
                        adjustment_per_heap,
                        max (min_budget, new_budget)));
                    heap_budget_in_region_units[kind][i] = max (min_budget, new_budget);
                    rem_balance += new_budget - heap_budget_in_region_units[kind][i];
                }
                assert (rem_balance <= 0);
                dprintf (REGIONS_LOG, ("remaining balance: %zd %s regions", rem_balance, free_region_kind_name[kind]));

                // if we have a left over deficit, distribute that to the heaps that still have more than the minimum
                while (rem_balance < 0)
                {
                    for (int i = 0; i < n_heaps; i++)
                    {
                        size_t min_budget = min_heap_budget_in_region_units[kind][i];
                        if (heap_budget_in_region_units[kind][i] > min_budget)
                        {
                            dprintf (REGIONS_LOG, ("adjusting the budget for heap %d from %zd %s regions by %d to %zd",
                                i,
                                heap_budget_in_region_units[kind][i],
                                free_region_kind_name[kind],
                                -1,
                                heap_budget_in_region_units[kind][i] - 1));

                            heap_budget_in_region_units[kind][i] -= 1;
                            rem_balance += 1;
                            if (rem_balance == 0)
                                break;
                        }
                    }
                }
            }
#endif //MULTIPLE_HEAPS
        }
        else
        {
            assert (balance_to_distribute >= 0);

            ptrdiff_t balance_to_decommit = balance_to_distribute;
            if (kind == large_free_region)
            {
                // huge regions aren't part of balance_to_distribute because they are kept in a global list
                // and therefore can't be distributed across heaps
                balance_to_decommit += global_free_huge_regions.get_size_free_regions() / global_region_allocator.get_large_region_alignment();
            }

            dprintf(REGIONS_LOG, ("distributing the %zd %s regions, removing %zd regions",
                total_budget_in_region_units[kind],
                free_region_kind_name[kind],
                balance_to_decommit));

            if (balance_to_decommit > 0)
            {
                // remember how many regions we had on the decommit list already due to aging
                size_t num_regions_to_decommit_before = global_regions_to_decommit[kind].get_num_free_regions();

                // put the highest regions on the decommit list
                global_region_allocator.move_highest_free_regions (balance_to_decommit * region_factor[kind],
                                                                   kind == basic_free_region,
                                                                   global_regions_to_decommit);

                dprintf (REGIONS_LOG, ("Moved %zd %s regions to decommit list",
                         global_regions_to_decommit[kind].get_num_free_regions(), free_region_kind_name[kind]));

                if (kind == basic_free_region)
                {
                    // we should now have 'balance' regions more on the decommit list
                    assert (global_regions_to_decommit[kind].get_num_free_regions() ==
                            num_regions_to_decommit_before + (size_t)balance_to_decommit);
                }
                else
                {
                    dprintf (REGIONS_LOG, ("Moved %zd %s regions to decommit list",
                        global_regions_to_decommit[huge_free_region].get_num_free_regions(), free_region_kind_name[huge_free_region]));

                    // cannot assert we moved any regions because there may be a single huge region with more than we want to decommit
                }
            }
        }
    }

    for (int kind = basic_free_region; kind < count_distributed_free_region_kinds; kind++)
    {
#ifdef MULTIPLE_HEAPS
        // now go through all the heaps and remove any free regions above the target count
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];

            if (hp->free_regions[kind].get_num_free_regions() > heap_budget_in_region_units[kind][i])
            {
                dprintf (REGIONS_LOG, ("removing %zd %s regions from heap %d with %zd regions, budget is %zd",
                    hp->free_regions[kind].get_num_free_regions() - heap_budget_in_region_units[kind][i],
                    free_region_kind_name[kind],
                    i,
                    hp->free_regions[kind].get_num_free_regions(),
                    heap_budget_in_region_units[kind][i]));

                trim_region_list (&surplus_regions[kind], &hp->free_regions[kind], heap_budget_in_region_units[kind][i]);
            }
        }
        // finally go through all the heaps and distribute any surplus regions to heaps having too few free regions
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
        {
            gc_heap* hp = pGenGCHeap;
            const int i = 0;
#endif //MULTIPLE_HEAPS

            // second pass: fill all the regions having less than budget
            if (hp->free_regions[kind].get_num_free_regions() < heap_budget_in_region_units[kind][i])
            {
                int64_t num_added_regions = grow_region_list (&hp->free_regions[kind], &surplus_regions[kind], heap_budget_in_region_units[kind][i]);
                dprintf (REGIONS_LOG, ("added %zd %s regions to heap %d - now has %zd, budget is %zd",
                    (size_t)num_added_regions,
                    free_region_kind_name[kind],
                    i,
                    hp->free_regions[kind].get_num_free_regions(),
                    heap_budget_in_region_units[kind][i]));
            }
            hp->free_regions[kind].sort_by_committed_and_age();
        }

        if (surplus_regions[kind].get_num_free_regions() > 0)
        {
            assert (!"should have exhausted the surplus_regions");
            global_regions_to_decommit[kind].transfer_regions (&surplus_regions[kind]);
        }
    }

    decide_on_decommit_strategy(aggressive_decommit_large_p);
}

void gc_heap::move_all_aged_regions(size_t total_num_free_regions[count_distributed_free_region_kinds], region_free_list aged_regions[count_free_region_kinds], bool joined_last_gc_before_oom)
{
    move_aged_regions(aged_regions, global_free_huge_regions, huge_free_region, joined_last_gc_before_oom);

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        for (int kind = basic_free_region; kind < count_distributed_free_region_kinds; kind++)
        {
            move_aged_regions(aged_regions, hp->free_regions[kind], static_cast<free_region_kind>(kind), joined_last_gc_before_oom);
            total_num_free_regions[kind] += hp->free_regions[kind].get_num_free_regions();
        }
    }
}

void gc_heap::move_aged_regions(region_free_list dest[count_free_region_kinds], region_free_list& src, free_region_kind kind, bool joined_last_gc_before_oom)
{
    heap_segment* next_region = nullptr;
    for (heap_segment* region = src.get_first_free_region(); region != nullptr; region = next_region)
    {
        next_region = heap_segment_next (region);
        // when we are about to get OOM, we'd like to discount the free regions that just have the initial page commit as they are not useful
        if (aged_region_p(region, kind) ||
            ((get_region_committed_size (region) == GC_PAGE_SIZE) && joined_last_gc_before_oom))
        {
            region_free_list::unlink_region (region);
            region_free_list::add_region (region, dest);
        }
    }
}

bool gc_heap::aged_region_p(heap_segment* region, free_region_kind kind)
{
#ifndef MULTIPLE_HEAPS
    const int n_heaps = 1;
#endif

    int age_in_free_to_decommit;
    switch (kind)
    {
        case basic_free_region:
            age_in_free_to_decommit = max(AGE_IN_FREE_TO_DECOMMIT_BASIC, n_heaps);
            break;
        case large_free_region:
            age_in_free_to_decommit = AGE_IN_FREE_TO_DECOMMIT_LARGE;
            break;
        case huge_free_region:
            age_in_free_to_decommit = AGE_IN_FREE_TO_DECOMMIT_HUGE;
            break;
        default:
            assert(!"unexpected kind");
            age_in_free_to_decommit = 0;
    }

    age_in_free_to_decommit = min (age_in_free_to_decommit, MAX_AGE_IN_FREE);
    return (heap_segment_age_in_free (region) >= age_in_free_to_decommit);
}

void gc_heap::move_regions_to_decommit(region_free_list regions[count_free_region_kinds])
{
    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        dprintf (1, ("moved %2zd %s regions (%8zd) to decommit based on time",
            regions[kind].get_num_free_regions(), free_region_kind_name[kind], regions[kind].get_size_committed_in_free()));
    }
    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        heap_segment* next_region = nullptr;
        for (heap_segment* region = regions[kind].get_first_free_region(); region != nullptr; region = next_region)
        {
            next_region = heap_segment_next (region);
            dprintf (REGIONS_LOG, ("region %p age %2d, decommit",
                heap_segment_mem (region), heap_segment_age_in_free (region)));
            region_free_list::unlink_region (region);
            region_free_list::add_region (region, global_regions_to_decommit);
        }
    }
    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        assert(regions[kind].get_num_free_regions() == 0);
    }
}

size_t gc_heap::compute_basic_region_budgets(
    size_t heap_basic_budget_in_region_units[MAX_SUPPORTED_HEAPS],
    size_t min_heap_basic_budget_in_region_units[MAX_SUPPORTED_HEAPS],
    size_t total_basic_free_regions)
{
    const size_t region_size = global_region_allocator.get_region_alignment();
    size_t total_budget_in_region_units = 0;

    for (int gen = soh_gen0; gen <= max_generation; gen++)
    {
        if (total_budget_in_region_units >= total_basic_free_regions)
        {
            // don't accumulate budget from higher soh generations if we cannot cover lower ones
            dprintf (REGIONS_LOG, ("out of free regions - skipping gen %d budget = %zd >= avail %zd",
                gen,
                total_budget_in_region_units,
                total_basic_free_regions));
            break;
        }

#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
#else //MULTIPLE_HEAPS
        {
            gc_heap* hp = pGenGCHeap;
            // just to reduce the number of #ifdefs in the code below
            const int i = 0;
#endif //MULTIPLE_HEAPS
            ptrdiff_t budget_gen = max (hp->estimate_gen_growth (gen), (ptrdiff_t)0);
            size_t budget_gen_in_region_units = (budget_gen + (region_size - 1)) / region_size;
            dprintf (REGIONS_LOG, ("h%2d gen %d has an estimated growth of %zd bytes (%zd regions)", i, gen, budget_gen, budget_gen_in_region_units));

            // preserve the budget for the previous generation - we should not go below that
            min_heap_basic_budget_in_region_units[i] = heap_basic_budget_in_region_units[i];

            heap_basic_budget_in_region_units[i] += budget_gen_in_region_units;
            total_budget_in_region_units += budget_gen_in_region_units;
        }
    }

    return total_budget_in_region_units;
}

#endif
#ifdef USE_REGIONS
#ifdef MULTIPLE_HEAPS
void gc_heap::set_heap_for_contained_basic_regions (heap_segment* region, gc_heap* hp)
{
    uint8_t* region_start = get_region_start (region);
    uint8_t* region_end = heap_segment_reserved (region);

    int num_basic_regions = (int)((region_end - region_start) >> min_segment_size_shr);
    for (int i = 0; i < num_basic_regions; i++)
    {
        uint8_t* basic_region_start = region_start + ((size_t)i << min_segment_size_shr);
        heap_segment* basic_region = get_region_info (basic_region_start);
        heap_segment_heap (basic_region) = hp;
    }
}

heap_segment* gc_heap::unlink_first_rw_region (int gen_idx)
{
    generation* gen = generation_of (gen_idx);
    heap_segment* prev_region = generation_tail_ro_region (gen);
    heap_segment* region = nullptr;
    if (prev_region)
    {
        assert (heap_segment_read_only_p (prev_region));
        region = heap_segment_next (prev_region);
        assert (region != nullptr);
        // don't remove the last region in the generation
        if (heap_segment_next (region) == nullptr)
        {
            assert (region == generation_tail_region (gen));
            return nullptr;
        }
        heap_segment_next (prev_region) = heap_segment_next (region);
    }
    else
    {
        region = generation_start_segment (gen);
        assert (region != nullptr);
        // don't remove the last region in the generation
        if (heap_segment_next (region) == nullptr)
        {
            assert (region == generation_tail_region (gen));
            return nullptr;
        }
        generation_start_segment (gen) = heap_segment_next (region);
    }
    assert (region != generation_tail_region (gen));
    assert (!heap_segment_read_only_p (region));
    dprintf (REGIONS_LOG, ("unlink_first_rw_region on heap: %d gen: %d region: %p", heap_number, gen_idx, heap_segment_mem (region)));

    int oh = heap_segment_oh (region);
    dprintf(3, ("commit-accounting:  from %d to temp [%p, %p) for heap %d", oh, get_region_start (region), heap_segment_committed (region), this->heap_number));
#ifdef _DEBUG
    size_t committed = heap_segment_committed (region) - get_region_start (region);
    if (committed > 0)
    {
        assert (this->committed_by_oh_per_heap[oh] >= committed);
        this->committed_by_oh_per_heap[oh] -= committed;
    }
#endif //_DEBUG

    set_heap_for_contained_basic_regions (region, nullptr);

    return region;
}

void gc_heap::thread_rw_region_front (int gen_idx, heap_segment* region)
{
    generation* gen = generation_of (gen_idx);
    assert (!heap_segment_read_only_p (region));
    heap_segment* prev_region = generation_tail_ro_region (gen);
    if (prev_region)
    {
        heap_segment_next (region) = heap_segment_next (prev_region);
        heap_segment_next (prev_region) = region;
    }
    else
    {
        heap_segment_next (region) = generation_start_segment (gen);
        generation_start_segment (gen) = region;
    }
    if (heap_segment_next (region) == nullptr)
    {
        generation_tail_region (gen) = region;
    }
    dprintf (REGIONS_LOG, ("thread_rw_region_front on heap: %d gen: %d region: %p", heap_number, gen_idx, heap_segment_mem (region)));

    int oh = heap_segment_oh (region);
    dprintf(3, ("commit-accounting:  from temp to %d [%p, %p) for heap %d", oh, get_region_start (region), heap_segment_committed (region), this->heap_number));
#ifdef _DEBUG
    size_t committed = heap_segment_committed (region) - get_region_start (region);
    assert (heap_segment_heap (region) == nullptr);
    this->committed_by_oh_per_heap[oh] += committed;
#endif //_DEBUG

    set_heap_for_contained_basic_regions (region, this);
}

#endif //MULTIPLE_HEAPS

heap_segment* gc_heap::allocate_new_region (gc_heap* hp, int gen_num, bool uoh_p, size_t size)
{
    uint8_t* start = 0;
    uint8_t* end = 0;

    // size parameter should be non-zero only for large regions
    assert (uoh_p || size == 0);

    // REGIONS TODO: allocate POH regions on the right
    bool allocated_p = (uoh_p ?
        global_region_allocator.allocate_large_region (gen_num, &start, &end, allocate_forward, size, on_used_changed) :
        global_region_allocator.allocate_basic_region (gen_num, &start, &end, on_used_changed));

    if (!allocated_p)
    {
        return 0;
    }

    heap_segment* res = make_heap_segment (start, (end - start), hp, gen_num);

    dprintf (REGIONS_LOG, ("got a new region %zx %p->%p", (size_t)res, start, end));

    if (res == nullptr)
    {
        global_region_allocator.delete_region (start);
    }

    return res;
}

#endif //USE_REGIONS
#ifdef BACKGROUND_GC
void gc_heap::generation_delete_heap_segment (generation* gen,
                                              heap_segment* seg,
                                              heap_segment* prev_seg,
                                              heap_segment* next_seg)
{
    dprintf (3, ("bgc sweep: deleting seg %zx(%p), next %zx(%p), prev %zx(%p)",
        (size_t)seg, heap_segment_mem (seg),
        (size_t)next_seg, (next_seg ? heap_segment_mem (next_seg) : 0),
        (size_t)prev_seg, (prev_seg ? heap_segment_mem (prev_seg) : 0)));
    if (gen->gen_num > max_generation)
    {
        dprintf (3, ("Preparing empty large segment %zx for deletion", (size_t)seg));

        // We cannot thread segs in here onto freeable_uoh_segment because
        // grow_brick_card_tables could be committing mark array which needs to read
        // the seg list. So we delay it till next time we suspend EE.
        seg->flags |= heap_segment_flags_uoh_delete;
        // Since we will be decommitting the seg, we need to prevent heap verification
        // to verify this segment.
        heap_segment_allocated (seg) = heap_segment_mem (seg);
    }
    else
    {
        assert (seg != ephemeral_heap_segment);

#ifdef DOUBLY_LINKED_FL
        // For doubly linked list we go forward for SOH
        heap_segment_next (prev_seg) = next_seg;
#else //DOUBLY_LINKED_FL
        heap_segment_next (next_seg) = prev_seg;
#endif //DOUBLY_LINKED_FL

        dprintf (3, ("Preparing empty small segment %zx for deletion", (size_t)seg));
        heap_segment_next (seg) = freeable_soh_segment;
        freeable_soh_segment = seg;

#ifdef USE_REGIONS
#ifdef DOUBLY_LINKED_FL
        heap_segment* next_region = next_seg;
        heap_segment* prev_region = prev_seg;
#else //DOUBLY_LINKED_FL
        heap_segment* next_region = prev_seg;
        heap_segment* prev_region = next_seg;
#endif //DOUBLY_LINKED_FL

        update_start_tail_regions (gen, seg, prev_region, next_region);
#endif //USE_REGIONS
    }

    decommit_heap_segment (seg);
    seg->flags |= heap_segment_flags_decommitted;

    set_mem_verify (heap_segment_allocated (seg) - plug_skew, heap_segment_used (seg), 0xbb);
}

#endif //BACKGROUND_GC
