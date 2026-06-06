// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef CARD_BUNDLE

// Clear the specified card bundle
void gc_heap::card_bundle_clear (size_t cardb)
{
    uint32_t bit = (uint32_t)(1 << card_bundle_bit (cardb));
    uint32_t* bundle = &card_bundle_table[card_bundle_word (cardb)];
#ifdef MULTIPLE_HEAPS
    // card bundles may straddle segments and heaps, thus bits may be cleared concurrently
    if ((*bundle & bit) != 0)
    {
        Interlocked::And (bundle, ~bit);
    }
#else
    *bundle &= ~bit;
#endif

    // check for races
    assert ((*bundle & bit) == 0);

    dprintf (2, ("Cleared card bundle %zx [%zx, %zx[", cardb, (size_t)card_bundle_cardw (cardb),
              (size_t)card_bundle_cardw (cardb+1)));
}

inline void set_bundle_bits (uint32_t* bundle, uint32_t bits)
{
#ifdef MULTIPLE_HEAPS
    // card bundles may straddle segments and heaps, thus bits may be set concurrently
    if ((*bundle & bits) != bits)
    {
        Interlocked::Or (bundle, bits);
    }
#else
    *bundle |= bits;
#endif

    // check for races
    assert ((*bundle & bits) == bits);
}

void gc_heap::card_bundle_set (size_t cardb)
{
    uint32_t bits = (1 << card_bundle_bit (cardb));
    set_bundle_bits (&card_bundle_table [card_bundle_word (cardb)], bits);
}

// Set the card bundle bits between start_cardb and end_cardb
void gc_heap::card_bundles_set (size_t start_cardb, size_t end_cardb)
{
    if (start_cardb == end_cardb)
    {
        card_bundle_set(start_cardb);
        return;
    }

    size_t start_word = card_bundle_word (start_cardb);
    size_t end_word = card_bundle_word (end_cardb);

    if (start_word < end_word)
    {
        // Set the partial words
        uint32_t bits = highbits (~0u, card_bundle_bit (start_cardb));
        set_bundle_bits (&card_bundle_table [start_word], bits);

        if (card_bundle_bit (end_cardb))
        {
            bits = lowbits (~0u, card_bundle_bit (end_cardb));
            set_bundle_bits (&card_bundle_table [end_word], bits);
        }

        // Set the full words
        for (size_t i = start_word + 1; i < end_word; i++)
        {
            card_bundle_table [i] = ~0u;
        }
    }
    else
    {
        uint32_t bits = (highbits (~0u, card_bundle_bit (start_cardb)) &
                          lowbits (~0u, card_bundle_bit (end_cardb)));
        set_bundle_bits (&card_bundle_table [start_word], bits);
    }
}

// Indicates whether the specified bundle is set.
BOOL gc_heap::card_bundle_set_p (size_t cardb)
{
    return (card_bundle_table[card_bundle_word(cardb)] & (1 << card_bundle_bit (cardb)));
}

// Returns the size (in bytes) of a card bundle representing the region from 'from' to 'end'
size_t size_card_bundle_of (uint8_t* from, uint8_t* end)
{
    // Number of heap bytes represented by a card bundle word
    size_t cbw_span = card_size * card_word_width * card_bundle_size * card_bundle_word_width;

    // Align the start of the region down
    from = (uint8_t*)((size_t)from & ~(cbw_span - 1));

    // Align the end of the region up
    end = (uint8_t*)((size_t)(end + (cbw_span - 1)) & ~(cbw_span - 1));

    // Make sure they're really aligned
    assert (((size_t)from & (cbw_span - 1)) == 0);
    assert (((size_t)end  & (cbw_span - 1)) == 0);

    return ((end - from) / cbw_span) * sizeof (uint32_t);
}

void gc_heap::enable_card_bundles ()
{
    if (can_use_write_watch_for_card_table() && (!card_bundles_enabled()))
    {
        dprintf (1, ("Enabling card bundles"));

        // We initially set all of the card bundles
        card_bundles_set (cardw_card_bundle (card_word (card_of (lowest_address))),
                          cardw_card_bundle (align_cardw_on_bundle (card_word (card_of (highest_address)))));
        settings.card_bundles = TRUE;
    }
}

BOOL gc_heap::card_bundles_enabled ()
{
    return settings.card_bundles;
}

#endif //CARD_BUNDLE

inline
size_t gc_heap::brick_of (uint8_t* add)
{
    return (size_t)(add - lowest_address) / brick_size;
}

inline
uint8_t* gc_heap::brick_address (size_t brick)
{
    return lowest_address + (brick_size * brick);
}

void gc_heap::clear_brick_table (uint8_t* from, uint8_t* end)
{
    size_t from_brick = brick_of (from);
    size_t end_brick = brick_of (end);
    memset (&brick_table[from_brick], 0, sizeof(brick_table[from_brick])*(end_brick-from_brick));
}

//codes for the brick entries:
//entry == 0 -> not assigned
//entry >0 offset is entry-1
//entry <0 jump back entry bricks
inline
void gc_heap::set_brick (size_t index, ptrdiff_t val)
{
    if (val < -32767)
    {
        val = -32767;
    }
    assert (val < 32767);
    if (val >= 0)
        brick_table [index] = (short)val+1;
    else
        brick_table [index] = (short)val;

    dprintf (3, ("set brick[%zx] to %d\n", index, (short)val));
}

inline
int gc_heap::get_brick_entry (size_t index)
{
#ifdef MULTIPLE_HEAPS
    return VolatileLoadWithoutBarrier(&brick_table [index]);
#else
    return brick_table[index];
#endif
}

inline
uint8_t* gc_heap::card_address (size_t card)
{
    return  (uint8_t*) (card_size * card);
}

inline
size_t gc_heap::card_of ( uint8_t* object)
{
    return (size_t)(object) / card_size;
}

inline
void gc_heap::clear_card (size_t card)
{
    card_table [card_word (card)] =
        (card_table [card_word (card)] & ~(1 << card_bit (card)));
    dprintf (3,("Cleared card %zx [%zx, %zx[", card, (size_t)card_address (card),
              (size_t)card_address (card+1)));
}

inline
void gc_heap::set_card (size_t card)
{
    size_t word = card_word (card);
    card_table[word] = (card_table [word] | (1 << card_bit (card)));

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    // Also set the card bundle that corresponds to the card
    size_t bundle_to_set = cardw_card_bundle(word);

    card_bundle_set(bundle_to_set);

    dprintf (3,("Set card %zx [%zx, %zx[ and bundle %zx", card, (size_t)card_address (card), (size_t)card_address (card+1), bundle_to_set));
#endif
}

inline
BOOL  gc_heap::card_set_p (size_t card)
{
    return ( card_table [ card_word (card) ] & (1 << card_bit (card)));
}

void gc_heap::destroy_card_table_helper (uint32_t* c_table)
{
    uint8_t* lowest = card_table_lowest_address (c_table);
    uint8_t* highest = card_table_highest_address (c_table);
    get_card_table_element_layout(lowest, highest, card_table_element_layout);
    size_t result = card_table_element_layout[seg_mapping_table_element + 1];
    gc_heap::reduce_committed_bytes (&card_table_refcount(c_table), result, recorded_committed_bookkeeping_bucket, -1, true);

    // If we don't put the mark array committed in the ignored bucket, then this is where to account for the decommit of it
}

void gc_heap::get_card_table_element_sizes (uint8_t* start, uint8_t* end, size_t sizes[total_bookkeeping_elements])
{
    memset (sizes, 0, sizeof(size_t) * total_bookkeeping_elements);
    sizes[card_table_element] = size_card_of (start, end);
    sizes[brick_table_element] = size_brick_of (start, end);
#ifdef CARD_BUNDLE
    if (can_use_write_watch_for_card_table())
    {
        sizes[card_bundle_table_element] = size_card_bundle_of (start, end);
    }
#endif //CARD_BUNDLE
#if defined(FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP) && defined (BACKGROUND_GC)
    if (gc_can_use_concurrent)
    {
        sizes[software_write_watch_table_element] = SoftwareWriteWatch::GetTableByteSize(start, end);
    }
#endif //FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP && BACKGROUND_GC
#ifdef USE_REGIONS
    sizes[region_to_generation_table_element] = size_region_to_generation_table_of (start, end);
#endif //USE_REGIONS
    sizes[seg_mapping_table_element] = size_seg_mapping_table_of (start, end);
#ifdef BACKGROUND_GC
    if (gc_can_use_concurrent)
    {
        sizes[mark_array_element] = size_mark_array_of (start, end);
    }
#endif //BACKGROUND_GC
}

void gc_heap::get_card_table_element_layout (uint8_t* start, uint8_t* end, size_t layout[total_bookkeeping_elements + 1])
{
    size_t sizes[total_bookkeeping_elements];
    get_card_table_element_sizes(start, end, sizes);

    const size_t alignment[total_bookkeeping_elements + 1] =
    {
        sizeof (uint32_t), // card_table_element
        sizeof (short),    // brick_table_element
#ifdef CARD_BUNDLE
        sizeof (uint32_t), // card_bundle_table_element
#endif //CARD_BUNDLE
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        sizeof(size_t),    // software_write_watch_table_element
#endif //FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
#ifdef USE_REGIONS
        sizeof (uint8_t),  // region_to_generation_table_element
#endif //USE_REGIONS
        sizeof (uint8_t*), // seg_mapping_table_element
#ifdef BACKGROUND_GC
        // In order to avoid a dependency between commit_mark_array_by_range and this logic, it is easier to make sure
        // pages for mark array never overlaps with pages in the seg mapping table. That way commit_mark_array_by_range
        // will never commit a page that is already committed here for the seg mapping table.
        OS_PAGE_SIZE,      // mark_array_element
#endif //BACKGROUND_GC
        // commit_mark_array_by_range extends the end pointer of the commit to the next page boundary, we better make sure it
        // is reserved
        OS_PAGE_SIZE       // total_bookkeeping_elements
    };

    layout[card_table_element] = ALIGN_UP(sizeof(card_table_info), alignment[card_table_element]);
    for (int element = brick_table_element; element <= total_bookkeeping_elements; element++)
    {
        layout[element] = layout[element - 1] + sizes[element - 1];
        if ((element != total_bookkeeping_elements) && (sizes[element] != 0))
        {
            layout[element] = ALIGN_UP(layout[element], alignment[element]);
        }
    }
}

#ifdef USE_REGIONS
bool gc_heap::on_used_changed (uint8_t* new_used)
{
#if defined(WRITE_BARRIER_CHECK) && !defined (SERVER_GC)
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_BARRIERCHECK)
    {
        size_t shadow_covered = g_GCShadowEnd - g_GCShadow;
        size_t used_heap_range = new_used - g_gc_lowest_address;
        if (used_heap_range > shadow_covered)
        {
            size_t extra = used_heap_range - shadow_covered;
            if (!GCToOSInterface::VirtualCommit (g_GCShadowEnd, extra))
            {
                _ASSERTE(!"Not enough memory to run HeapVerify level 2");
                // If after the assert we decide to allow the program to continue
                // running we need to be in a state that will not trigger any
                // additional AVs while we fail to allocate a shadow segment, i.e.
                // ensure calls to updateGCShadow() checkGCWriteBarrier() don't AV
                deleteGCShadow();
            }
            else
            {
                g_GCShadowEnd += extra;
            }
        }
    }
#endif //WRITE_BARRIER_CHECK && !SERVER_GC

    if (new_used > bookkeeping_covered_committed)
    {
        bool speculative_commit_tried = false;
#ifdef STRESS_REGIONS
        if (gc_rand::get_rand(10) > 3)
        {
            dprintf (REGIONS_LOG, ("skipping speculative commit under stress regions"));
            speculative_commit_tried = true;
        }
#endif
        while (true)
        {
            uint8_t* new_bookkeeping_covered_committed = nullptr;
            if (speculative_commit_tried)
            {
                new_bookkeeping_covered_committed = new_used;
            }
            else
            {
                uint64_t committed_size = (uint64_t)(bookkeeping_covered_committed - g_gc_lowest_address);
                uint64_t total_size = (uint64_t)(g_gc_highest_address - g_gc_lowest_address);
                assert (committed_size <= total_size);
                assert (committed_size < (UINT64_MAX / 2));
                uint64_t new_committed_size = min(committed_size * 2, total_size);
                assert ((UINT64_MAX - new_committed_size) > (uint64_t)g_gc_lowest_address);
                uint8_t* double_commit = g_gc_lowest_address + new_committed_size;
                new_bookkeeping_covered_committed = max(double_commit, new_used);
                dprintf (REGIONS_LOG, ("committed_size                           = %zd", committed_size));
                dprintf (REGIONS_LOG, ("total_size                               = %zd", total_size));
                dprintf (REGIONS_LOG, ("new_committed_size                       = %zd", new_committed_size));
                dprintf (REGIONS_LOG, ("double_commit                            = %p", double_commit));
            }
            dprintf (REGIONS_LOG, ("bookkeeping_covered_committed     = %p", bookkeeping_covered_committed));
            dprintf (REGIONS_LOG, ("new_bookkeeping_covered_committed = %p", new_bookkeeping_covered_committed));

            if (inplace_commit_card_table (bookkeeping_covered_committed, new_bookkeeping_covered_committed))
            {
                bookkeeping_covered_committed = new_bookkeeping_covered_committed;
                break;
            }
            else
            {
                if (new_bookkeeping_covered_committed == new_used)
                {
                    dprintf (REGIONS_LOG, ("The minimal commit for the GC bookkeeping data structure failed, giving up"));
                    return false;
                }
                dprintf (REGIONS_LOG, ("The speculative commit for the GC bookkeeping data structure failed, retry for minimal commit"));
                speculative_commit_tried = true;
            }
        }
    }
    return true;
}

bool gc_heap::get_card_table_commit_layout (uint8_t* from, uint8_t* to,
                    uint8_t* commit_begins[total_bookkeeping_elements],
                    size_t commit_sizes[total_bookkeeping_elements],
                    size_t new_sizes[total_bookkeeping_elements])
{
    uint8_t* start = g_gc_lowest_address;
    uint8_t* end = g_gc_highest_address;

    bool initial_commit = (from == start);
    bool additional_commit = !initial_commit && (to > from);

    if (!initial_commit && !additional_commit)
    {
        return false;
    }
#ifdef _DEBUG
    size_t offsets[total_bookkeeping_elements + 1];
    get_card_table_element_layout(start, end, offsets);

    dprintf (REGIONS_LOG, ("layout"));
    for (int i = card_table_element; i <= total_bookkeeping_elements; i++)
    {
        assert (offsets[i] == card_table_element_layout[i]);
        dprintf (REGIONS_LOG, ("%zd", card_table_element_layout[i]));
    }
#endif //_DEBUG
    get_card_table_element_sizes (start, to, new_sizes);
#ifdef _DEBUG
    dprintf (REGIONS_LOG, ("new_sizes"));
    for (int i = card_table_element; i < total_bookkeeping_elements; i++)
    {
        dprintf (REGIONS_LOG, ("%zd", new_sizes[i]));
    }
    if (additional_commit)
    {
        size_t current_sizes[total_bookkeeping_elements];
        get_card_table_element_sizes (start, from, current_sizes);
        dprintf (REGIONS_LOG, ("old_sizes"));
        for (int i = card_table_element; i < total_bookkeeping_elements; i++)
        {
            assert (current_sizes[i] == bookkeeping_sizes[i]);
            dprintf (REGIONS_LOG, ("%zd", bookkeeping_sizes[i]));
        }
    }
#endif //_DEBUG
    for (int i = card_table_element; i <= seg_mapping_table_element; i++)
    {
        uint8_t* required_begin = nullptr;
        uint8_t* required_end = nullptr;
        uint8_t* commit_begin = nullptr;
        uint8_t* commit_end = nullptr;
        if (initial_commit)
        {
            required_begin = bookkeeping_start + ((i == card_table_element) ? 0 : card_table_element_layout[i]);
            required_end = bookkeeping_start + card_table_element_layout[i] + new_sizes[i];
            commit_begin = align_lower_page(required_begin);
        }
        else
        {
            assert (additional_commit);
            required_begin = bookkeeping_start + card_table_element_layout[i] + bookkeeping_sizes[i];
            required_end = required_begin + new_sizes[i] - bookkeeping_sizes[i];
            commit_begin = align_on_page(required_begin);
        }
        assert (required_begin <= required_end);
        commit_end = align_on_page(required_end);

        commit_end = min (commit_end, align_lower_page(bookkeeping_start + card_table_element_layout[i + 1]));
        commit_begin = min (commit_begin, commit_end);
        assert (commit_begin <= commit_end);

        dprintf (REGIONS_LOG, ("required = [%p, %p), size = %zd", required_begin, required_end, required_end - required_begin));
        dprintf (REGIONS_LOG, ("commit   = [%p, %p), size = %zd", commit_begin, commit_end, commit_end - commit_begin));

        commit_begins[i] = commit_begin;
        commit_sizes[i] = (size_t)(commit_end - commit_begin);
    }
    dprintf (REGIONS_LOG, ("---------------------------------------"));
    return true;
}

bool gc_heap::inplace_commit_card_table (uint8_t* from, uint8_t* to)
{
    dprintf (REGIONS_LOG, ("inplace_commit_card_table(%p, %p), size = %zd", from, to, to - from));

    uint8_t* start = g_gc_lowest_address;
    uint8_t* end = g_gc_highest_address;

    uint8_t* commit_begins[total_bookkeeping_elements];
    size_t commit_sizes[total_bookkeeping_elements];
    size_t new_sizes[total_bookkeeping_elements];

    if (!get_card_table_commit_layout(from, to, commit_begins, commit_sizes, new_sizes))
    {
        return true;
    }
    int failed_commit = -1;
    for (int i = card_table_element; i <= seg_mapping_table_element; i++)
    {
        bool succeed;
        if (commit_sizes[i] > 0)
        {
            succeed = virtual_commit (commit_begins[i], commit_sizes[i], recorded_committed_bookkeeping_bucket);
            if (!succeed)
            {
                log_init_error_to_host ("Committing %zd bytes (%.3f mb) for GC bookkeeping element#%d failed", commit_sizes[i], mb (commit_sizes[i]), i);
                failed_commit = i;
                break;
            }
        }
    }
    if (failed_commit == -1)
    {
        for (int i = card_table_element; i < total_bookkeeping_elements; i++)
        {
            bookkeeping_sizes[i] = new_sizes[i];
        }
    }
    else
    {
        for (int i = card_table_element; i < failed_commit; i++)
        {
            bool succeed;
            if (commit_sizes[i] > 0)
            {
                succeed = virtual_decommit (commit_begins[i], commit_sizes[i], recorded_committed_bookkeeping_bucket);
                assert (succeed);
            }
        }
        return false;
    }
    return true;
}

#endif //USE_REGIONS

uint32_t* gc_heap::make_card_table (uint8_t* start, uint8_t* end)
{
    assert (g_gc_lowest_address == start);
    assert (g_gc_highest_address == end);

    uint32_t virtual_reserve_flags = VirtualReserveFlags::None;
#ifdef CARD_BUNDLE
    if (can_use_write_watch_for_card_table())
    {
#ifndef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // If we're not manually managing the card bundles, we will need to use OS write
        // watch APIs over this region to track changes.
        virtual_reserve_flags |= VirtualReserveFlags::WriteWatch;
#endif
    }
#endif //CARD_BUNDLE

    get_card_table_element_layout(start, end, card_table_element_layout);

    size_t alloc_size = card_table_element_layout[total_bookkeeping_elements];
    uint8_t* mem = (uint8_t*)GCToOSInterface::VirtualReserve (alloc_size, 0, virtual_reserve_flags);
    bookkeeping_start = mem;

    if (!mem)
    {
        log_init_error_to_host ("Reserving %zd bytes (%.3f mb) for GC bookkeeping failed", alloc_size, mb (alloc_size));
        return 0;
    }

    dprintf (2, ("Init - Card table alloc for %zd bytes: [%zx, %zx[",
                 alloc_size, (size_t)mem, (size_t)(mem+alloc_size)));

#ifdef USE_REGIONS
    if (!inplace_commit_card_table (g_gc_lowest_address, global_region_allocator.get_left_used_unsafe()))
    {
        dprintf (1, ("Card table commit failed"));
        GCToOSInterface::VirtualRelease (mem, alloc_size);
        return 0;
    }
    bookkeeping_covered_committed = global_region_allocator.get_left_used_unsafe();
#else
    // in case of background gc, the mark array will be committed separately (per segment).
    size_t commit_size = card_table_element_layout[seg_mapping_table_element + 1];

    if (!virtual_commit (mem, commit_size, recorded_committed_bookkeeping_bucket))
    {
        dprintf (1, ("Card table commit failed"));
        GCToOSInterface::VirtualRelease (mem, alloc_size);
        return 0;
    }
#endif //USE_REGIONS

    // initialize the ref count
    uint32_t* ct = (uint32_t*)(mem + card_table_element_layout[card_table_element]);
    card_table_refcount (ct) = 0;
    card_table_lowest_address (ct) = start;
    card_table_highest_address (ct) = end;
    card_table_brick_table (ct) = (short*)(mem + card_table_element_layout[brick_table_element]);
    card_table_size (ct) = alloc_size;
    card_table_next (ct) = 0;

#ifdef CARD_BUNDLE
    card_table_card_bundle_table (ct) = (uint32_t*)(mem + card_table_element_layout[card_bundle_table_element]);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    g_gc_card_bundle_table = translate_card_bundle_table(card_table_card_bundle_table(ct), g_gc_lowest_address);
#endif
#endif //CARD_BUNDLE

#if defined(FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP) && defined (BACKGROUND_GC)
    if (gc_can_use_concurrent)
    {
        SoftwareWriteWatch::InitializeUntranslatedTable(mem + card_table_element_layout[software_write_watch_table_element], start);
    }
#endif //FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP && BACKGROUND_GC

#ifdef USE_REGIONS
    map_region_to_generation = (region_info*)(mem + card_table_element_layout[region_to_generation_table_element]);
    map_region_to_generation_skewed = map_region_to_generation - size_region_to_generation_table_of (0, g_gc_lowest_address);
#endif //USE_REGIONS

    seg_mapping_table = (seg_mapping*)(mem + card_table_element_layout[seg_mapping_table_element]);
    seg_mapping_table = (seg_mapping*)((uint8_t*)seg_mapping_table -
                                        size_seg_mapping_table_of (0, (align_lower_segment (g_gc_lowest_address))));

#ifdef BACKGROUND_GC
    if (gc_can_use_concurrent)
        card_table_mark_array (ct) = (uint32_t*)(mem + card_table_element_layout[mark_array_element]);
    else
        card_table_mark_array (ct) = NULL;
#endif //BACKGROUND_GC

    return translate_card_table(ct);
}

void gc_heap::set_fgm_result (failure_get_memory f, size_t s, BOOL loh_p)
{
#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        hp->fgm_result.set_fgm (f, s, loh_p);
    }
#else //MULTIPLE_HEAPS
    fgm_result.set_fgm (f, s, loh_p);
#endif //MULTIPLE_HEAPS
}

#ifndef USE_REGIONS
//returns 0 for success, -1 otherwise
// We are doing all the decommitting here because we want to make sure we have
// enough memory to do so - if we do this during copy_brick_card_table and
// and fail to decommit it would make the failure case very complicated to
// handle. This way we can waste some decommit if we call this multiple
// times before the next FGC but it's easier to handle the failure case.
int gc_heap::grow_brick_card_tables (uint8_t* start,
                                     uint8_t* end,
                                     size_t size,
                                     heap_segment* new_seg,
                                     gc_heap* hp,
                                     BOOL uoh_p)
{
    uint8_t* la = g_gc_lowest_address;
    uint8_t* ha = g_gc_highest_address;
    uint8_t* saved_g_lowest_address = min (start, g_gc_lowest_address);
    uint8_t* saved_g_highest_address = max (end, g_gc_highest_address);
    seg_mapping* new_seg_mapping_table = nullptr;
#ifdef BACKGROUND_GC
    // This value is only for logging purpose - it's not necessarily exactly what we
    // would commit for mark array but close enough for diagnostics purpose.
    size_t logging_ma_commit_size = size_mark_array_of (0, (uint8_t*)size);
#endif //BACKGROUND_GC

    // See if the address is already covered
    if ((la != saved_g_lowest_address ) || (ha != saved_g_highest_address))
    {
        {
            //modify the highest address so the span covered
            //is twice the previous one.
            uint8_t* top = (uint8_t*)0 + Align (GCToOSInterface::GetVirtualMemoryMaxAddress());
            // On non-Windows systems, we get only an approximate value that can possibly be
            // slightly lower than the saved_g_highest_address.
            // In such case, we set the top to the saved_g_highest_address so that the
            // card and brick tables always cover the whole new range.
            if (top < saved_g_highest_address)
            {
                top = saved_g_highest_address;
            }
            size_t ps = ha-la;
#ifdef HOST_64BIT
            if (ps > (uint64_t)200*1024*1024*1024)
                ps += (uint64_t)100*1024*1024*1024;
            else
#endif // HOST_64BIT
                ps *= 2;

            if (saved_g_lowest_address < g_gc_lowest_address)
            {
                if (ps > (size_t)g_gc_lowest_address)
                    saved_g_lowest_address = (uint8_t*)(size_t)OS_PAGE_SIZE;
                else
                {
                    assert (((size_t)g_gc_lowest_address - ps) >= OS_PAGE_SIZE);
                    saved_g_lowest_address = min (saved_g_lowest_address, (g_gc_lowest_address - ps));
                }
            }

            if (saved_g_highest_address > g_gc_highest_address)
            {
                saved_g_highest_address = max ((saved_g_lowest_address + ps), saved_g_highest_address);
                if (saved_g_highest_address > top)
                    saved_g_highest_address = top;
            }
        }
        dprintf (GC_TABLE_LOG, ("Growing card table [%zx, %zx[",
                                (size_t)saved_g_lowest_address,
                                (size_t)saved_g_highest_address));

        bool write_barrier_updated = false;
        uint32_t virtual_reserve_flags = VirtualReserveFlags::None;
        uint32_t* saved_g_card_table = g_gc_card_table;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        uint32_t* saved_g_card_bundle_table = g_gc_card_bundle_table;
#endif
        get_card_table_element_layout(saved_g_lowest_address, saved_g_highest_address, card_table_element_layout);
        size_t cb = 0;
        uint32_t* ct = 0;
        uint32_t* translated_ct = 0;

#ifdef CARD_BUNDLE
        if (can_use_write_watch_for_card_table())
        {
            cb = size_card_bundle_of (saved_g_lowest_address, saved_g_highest_address);

#ifndef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            // If we're not manually managing the card bundles, we will need to use OS write
            // watch APIs over this region to track changes.
            virtual_reserve_flags |= VirtualReserveFlags::WriteWatch;
#endif
        }
#endif //CARD_BUNDLE

        size_t alloc_size = card_table_element_layout[total_bookkeeping_elements];
        size_t commit_size = 0;
        uint8_t* mem = (uint8_t*)GCToOSInterface::VirtualReserve (alloc_size, 0, virtual_reserve_flags);

        if (!mem)
        {
            set_fgm_result (fgm_grow_table, alloc_size, uoh_p);
            goto fail;
        }

        dprintf (GC_TABLE_LOG, ("Table alloc for %zd bytes: [%zx, %zx[",
                                 alloc_size, (size_t)mem, (size_t)((uint8_t*)mem+alloc_size)));

        {
            // in case of background gc, the mark array will be committed separately (per segment).
            commit_size = card_table_element_layout[seg_mapping_table_element + 1];

            if (!virtual_commit (mem, commit_size, recorded_committed_bookkeeping_bucket))
            {
                commit_size = 0;
                dprintf (GC_TABLE_LOG, ("Table commit failed"));
                set_fgm_result (fgm_commit_table, commit_size, uoh_p);
                goto fail;
            }

        }

        ct = (uint32_t*)(mem + card_table_element_layout[card_table_element]);
        card_table_refcount (ct) = 0;
        card_table_lowest_address (ct) = saved_g_lowest_address;
        card_table_highest_address (ct) = saved_g_highest_address;
        card_table_next (ct) = &g_gc_card_table[card_word (gcard_of (la))];

        //clear the card table
/*
        memclr ((uint8_t*)ct,
                (((saved_g_highest_address - saved_g_lowest_address)*sizeof (uint32_t) /
                  (card_size * card_word_width))
                 + sizeof (uint32_t)));
*/
        // No initialization needed, will be done in copy_brick_card

        card_table_brick_table (ct) = (short*)(mem + card_table_element_layout[brick_table_element]);

#ifdef CARD_BUNDLE
        card_table_card_bundle_table (ct) = (uint32_t*)(mem + card_table_element_layout[card_bundle_table_element]);
        //set all bundle to look at all of the cards
        memset(card_table_card_bundle_table (ct), 0xFF, cb);
#endif //CARD_BUNDLE

        new_seg_mapping_table = (seg_mapping*)(mem + card_table_element_layout[seg_mapping_table_element]);
        new_seg_mapping_table = (seg_mapping*)((uint8_t*)new_seg_mapping_table -
                                            size_seg_mapping_table_of (0, (align_lower_segment (saved_g_lowest_address))));
        memcpy(&new_seg_mapping_table[seg_mapping_word_of(g_gc_lowest_address)],
            &seg_mapping_table[seg_mapping_word_of(g_gc_lowest_address)],
            size_seg_mapping_table_of(g_gc_lowest_address, g_gc_highest_address));

        // new_seg_mapping_table gets assigned to seg_mapping_table at the bottom of this function,
        // not here. The reason for this is that, if we fail at mark array committing (OOM) and we've
        // already switched seg_mapping_table to point to the new mapping table, we'll decommit it and
        // run into trouble. By not assigning here, we're making sure that we will not change seg_mapping_table
        // if an OOM occurs.

#ifdef BACKGROUND_GC
        if(gc_can_use_concurrent)
            card_table_mark_array (ct) = (uint32_t*)(mem + card_table_element_layout[mark_array_element]);
        else
            card_table_mark_array (ct) = NULL;
#endif //BACKGROUND_GC

        translated_ct = translate_card_table (ct);

#ifdef BACKGROUND_GC
        dprintf (GC_TABLE_LOG, ("card table: %zx(translated: %zx), seg map: %zx, mark array: %zx",
            (size_t)ct, (size_t)translated_ct, (size_t)new_seg_mapping_table, (size_t)card_table_mark_array (ct)));

        if (is_bgc_in_progress())
        {
            dprintf (GC_TABLE_LOG, ("new low: %p, new high: %p, latest mark array is %p(translate: %p)",
                                    saved_g_lowest_address, saved_g_highest_address,
                                    card_table_mark_array (ct),
                                    translate_mark_array (card_table_mark_array (ct))));
            uint32_t* new_mark_array = (uint32_t*)((uint8_t*)card_table_mark_array (ct) - size_mark_array_of (0, saved_g_lowest_address));
            if (!commit_new_mark_array_global (new_mark_array))
            {
                dprintf (GC_TABLE_LOG, ("failed to commit portions in the mark array for existing segments"));
                set_fgm_result (fgm_commit_table, logging_ma_commit_size, uoh_p);
                goto fail;
            }

            if (!commit_mark_array_new_seg (hp, new_seg, translated_ct, saved_g_lowest_address))
            {
                dprintf (GC_TABLE_LOG, ("failed to commit mark array for the new seg"));
                set_fgm_result (fgm_commit_table, logging_ma_commit_size, uoh_p);
                goto fail;
            }
        }
        else
        {
            clear_commit_flag_global();
        }
#endif //BACKGROUND_GC

#if defined(FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP) && defined(BACKGROUND_GC)
        if (gc_can_use_concurrent)
        {
            // The current design of software write watch requires that the runtime is suspended during resize. Suspending
            // on resize is preferred because it is a far less frequent operation than GetWriteWatch() / ResetWriteWatch().
            // Suspending here allows copying dirty state from the old table into the new table, and not have to merge old
            // table info lazily as done for card tables.

            // Either this thread was the thread that did the suspension which means we are suspended; or this is called
            // from a GC thread which means we are in a blocking GC and also suspended.
            bool is_runtime_suspended = GCToEEInterface::IsGCThread();
            if (!is_runtime_suspended)
            {
                // Note on points where the runtime is suspended anywhere in this function. Upon an attempt to suspend the
                // runtime, a different thread may suspend first, causing this thread to block at the point of the suspend call.
                // So, at any suspend point, externally visible state needs to be consistent, as code that depends on that state
                // may run while this thread is blocked. This includes updates to g_gc_card_table, g_gc_lowest_address, and
                // g_gc_highest_address.
                suspend_EE();
            }

            g_gc_card_table = translated_ct;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            g_gc_card_bundle_table = translate_card_bundle_table(card_table_card_bundle_table(ct), saved_g_lowest_address);
#endif

            SoftwareWriteWatch::SetResizedUntranslatedTable(
                mem + card_table_element_layout[software_write_watch_table_element],
                saved_g_lowest_address,
                saved_g_highest_address);

            seg_mapping_table = new_seg_mapping_table;

            // Since the runtime is already suspended, update the write barrier here as well.
            // This passes a bool telling whether we need to switch to the post
            // grow version of the write barrier.  This test tells us if the new
            // segment was allocated at a lower address than the old, requiring
            // that we start doing an upper bounds check in the write barrier.
            g_gc_lowest_address = saved_g_lowest_address;
            g_gc_highest_address = saved_g_highest_address;
            stomp_write_barrier_resize(true, la != saved_g_lowest_address);
            write_barrier_updated = true;

            if (!is_runtime_suspended)
            {
                restart_EE();
            }
        }
        else
#endif //FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP && BACKGROUND_GC
        {
            g_gc_card_table = translated_ct;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            g_gc_card_bundle_table = translate_card_bundle_table(card_table_card_bundle_table(ct), saved_g_lowest_address);
#endif
        }

        if (!write_barrier_updated)
        {
            seg_mapping_table = new_seg_mapping_table;
            minipal_memory_barrier_process_wide();
            g_gc_lowest_address = saved_g_lowest_address;
            g_gc_highest_address = saved_g_highest_address;

            // This passes a bool telling whether we need to switch to the post
            // grow version of the write barrier.  This test tells us if the new
            // segment was allocated at a lower address than the old, requiring
            // that we start doing an upper bounds check in the write barrier.
            // This will also suspend the runtime if the write barrier type needs
            // to be changed, so we are doing this after all global state has
            // been updated. See the comment above suspend_EE() above for more
            // info.
            stomp_write_barrier_resize(GCToEEInterface::IsGCThread(), la != saved_g_lowest_address);
        }

        return 0;

fail:
        if (mem)
        {
            assert(g_gc_card_table == saved_g_card_table);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            assert(g_gc_card_bundle_table  == saved_g_card_bundle_table);
#endif

            if (!GCToOSInterface::VirtualRelease (mem, alloc_size))
            {
                dprintf (GC_TABLE_LOG, ("GCToOSInterface::VirtualRelease failed"));
                assert (!"release failed");
            }
            reduce_committed_bytes (mem, commit_size, recorded_committed_bookkeeping_bucket, -1, true);
        }

        return -1;
    }
    else
    {
#ifdef BACKGROUND_GC
        if (is_bgc_in_progress())
        {
            dprintf (GC_TABLE_LOG, ("in range new seg %p, mark_array is %p", new_seg, hp->mark_array));
            if (!commit_mark_array_new_seg (hp, new_seg))
            {
                dprintf (GC_TABLE_LOG, ("failed to commit mark array for the new seg in range"));
                set_fgm_result (fgm_commit_table, logging_ma_commit_size, uoh_p);
                return -1;
            }
        }
#endif //BACKGROUND_GC
    }

    return 0;
}

//copy all of the arrays managed by the card table for a page aligned range
void gc_heap::copy_brick_card_range (uint8_t* la, uint32_t* old_card_table,
                                     short* old_brick_table,
                                     uint8_t* start, uint8_t* end)
{
    ptrdiff_t brick_offset = brick_of (start) - brick_of (la);
    dprintf (2, ("copying tables for range [%zx %zx[", (size_t)start, (size_t)end));

    // copy brick table
    short* brick_start = &brick_table [brick_of (start)];
    if (old_brick_table)
    {
        // segments are always on page boundaries
        memcpy (brick_start, &old_brick_table[brick_offset],
                size_brick_of (start, end));
    }

    uint32_t* old_ct = &old_card_table[card_word (card_of (la))];

#ifdef BACKGROUND_GC
    if (gc_heap::background_running_p())
    {
        uint32_t* old_mark_array = card_table_mark_array (old_ct);

        // We don't need to go through all the card tables here because
        // we only need to copy from the GC version of the mark array - when we
        // mark (even in allocate_uoh_object) we always use that mark array.
        if ((card_table_highest_address (old_ct) >= start) &&
            (card_table_lowest_address (old_ct) <= end))
        {
            if ((background_saved_highest_address >= start) &&
                (background_saved_lowest_address <= end))
            {
                //copy the mark bits
                // segments are always on page boundaries
                uint8_t* m_start = max (background_saved_lowest_address, start);
                uint8_t* m_end = min (background_saved_highest_address, end);
                memcpy (&mark_array[mark_word_of (m_start)],
                        &old_mark_array[mark_word_of (m_start) - mark_word_of (la)],
                        size_mark_array_of (m_start, m_end));
            }
        }
        else
        {
            //only large segments can be out of range
            assert (old_brick_table == 0);
        }
    }
#endif //BACKGROUND_GC

    // n way merge with all of the card table ever used in between
    uint32_t* ct = card_table_next (&card_table[card_word (card_of(lowest_address))]);

    assert (ct);
    while (card_table_next (old_ct) != ct)
    {
        //copy if old card table contained [start, end[
        if ((card_table_highest_address (ct) >= end) &&
            (card_table_lowest_address (ct) <= start))
        {
            // or the card_tables
            size_t start_word = card_word (card_of (start));

            uint32_t* dest = &card_table[start_word];
            uint32_t* src = &((translate_card_table (ct))[start_word]);
            ptrdiff_t count = count_card_of (start, end);
            for (int x = 0; x < count; x++)
            {
                *dest |= *src;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                if (*src != 0)
                {
                    card_bundle_set(cardw_card_bundle(start_word+x));
                }
#endif

                dest++;
                src++;
            }
        }
        ct = card_table_next (ct);
    }
}

void gc_heap::copy_brick_card_table()
{
    uint32_t* old_card_table = card_table;
    short* old_brick_table = brick_table;

    uint8_t* la = lowest_address;
#ifdef _DEBUG
    uint8_t* ha = highest_address;
    assert (la == card_table_lowest_address (&old_card_table[card_word (card_of (la))]));
    assert (ha == card_table_highest_address (&old_card_table[card_word (card_of (la))]));
#endif //_DEBUG

    /* todo: Need a global lock for this */
    uint32_t* ct = &g_gc_card_table[card_word (gcard_of (g_gc_lowest_address))];
    own_card_table (ct);
    card_table = translate_card_table (ct);
    bookkeeping_start = (uint8_t*)ct - sizeof(card_table_info);
    card_table_size(ct) = card_table_element_layout[total_bookkeeping_elements];
    /* End of global lock */
    highest_address = card_table_highest_address (ct);
    lowest_address = card_table_lowest_address (ct);

    brick_table = card_table_brick_table (ct);

#ifdef BACKGROUND_GC
    if (gc_can_use_concurrent)
    {
        mark_array = translate_mark_array (card_table_mark_array (ct));
        assert (mark_word_of (g_gc_highest_address) ==
            mark_word_of (align_on_mark_word (g_gc_highest_address)));
    }
    else
        mark_array = NULL;
#endif //BACKGROUND_GC

#ifdef CARD_BUNDLE
    card_bundle_table = translate_card_bundle_table (card_table_card_bundle_table (ct), g_gc_lowest_address);

    // Ensure that the word that represents g_gc_lowest_address in the translated table is located at the
    // start of the untranslated table.
    assert (&card_bundle_table [card_bundle_word (cardw_card_bundle (card_word (card_of (g_gc_lowest_address))))] ==
            card_table_card_bundle_table (ct));

    //set the card table if we are in a heap growth scenario
    if (card_bundles_enabled())
    {
        card_bundles_set (cardw_card_bundle (card_word (card_of (lowest_address))),
                          cardw_card_bundle (align_cardw_on_bundle (card_word (card_of (highest_address)))));
    }
    //check if we need to turn on card_bundles.
#ifdef MULTIPLE_HEAPS
    // use INT64 arithmetic here because of possible overflow on 32p
    uint64_t th = (uint64_t)MH_TH_CARD_BUNDLE*gc_heap::n_heaps;
#else
    // use INT64 arithmetic here because of possible overflow on 32p
    uint64_t th = (uint64_t)SH_TH_CARD_BUNDLE;
#endif //MULTIPLE_HEAPS
    if (reserved_memory >= th)
    {
        enable_card_bundles();
    }
#endif //CARD_BUNDLE

    // for each of the segments and heaps, copy the brick table and
    // or the card table
    for (int i = get_start_generation_index(); i < total_generation_count; i++)
    {
        heap_segment* seg = generation_start_segment (generation_of (i));
        while (seg)
        {
            if (heap_segment_read_only_p (seg) && !heap_segment_in_range_p (seg))
            {
                //check if it became in range
                if ((heap_segment_reserved (seg) > lowest_address) &&
                    (heap_segment_mem (seg) < highest_address))
                {
                    set_ro_segment_in_range (seg);
                }
            }
            else
            {
                uint8_t* end = align_on_page (heap_segment_allocated (seg));
                copy_brick_card_range (la, old_card_table,
                    (i < uoh_start_generation) ? old_brick_table : NULL,
                    align_lower_page (heap_segment_mem (seg)),
                    end);
            }
            seg = heap_segment_next (seg);
        }
    }

    release_card_table (&old_card_table[card_word (card_of(la))]);
}

void gc_heap::copy_brick_card_table_on_growth ()
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        if (g_gc_card_table != hp->card_table)
        {
            hp->copy_brick_card_table ();
        }
    }
}

#endif //!USE_REGIONS

void gc_heap::clear_gen1_cards()
{
#if defined(_DEBUG) && !defined(USE_REGIONS)
    for (int x = 0; x <= max_generation; x++)
    {
        assert (generation_allocation_start (generation_of (x)));
    }
#endif //_DEBUG && !USE_REGIONS

    if (!settings.demotion && settings.promotion)
    {
        //clear card for generation 1. generation 0 is empty
#ifdef USE_REGIONS
        heap_segment* region = generation_start_segment (generation_of (1));
        while (region)
        {
            clear_card_for_addresses (get_region_start (region), heap_segment_reserved (region));
            region = heap_segment_next (region);
        }
#else //USE_REGIONS
        clear_card_for_addresses (
            generation_allocation_start (generation_of (1)),
            generation_allocation_start (generation_of (0)));
#endif //USE_REGIONS

#ifdef _DEBUG
        uint8_t* start = get_soh_start_object (ephemeral_heap_segment, youngest_generation);
        assert (heap_segment_allocated (ephemeral_heap_segment) ==
                (start + get_soh_start_obj_len (start)));
#endif //_DEBUG
    }
}

void gc_heap::clear_gen0_bricks()
{
    if (!gen0_bricks_cleared)
    {
        gen0_bricks_cleared = TRUE;
        //initialize brick table for gen 0
#ifdef USE_REGIONS
        heap_segment* gen0_region = generation_start_segment (generation_of (0));
        while (gen0_region)
        {
            uint8_t* clear_start = heap_segment_mem (gen0_region);
#else
        heap_segment* gen0_region = ephemeral_heap_segment;
        uint8_t* clear_start = generation_allocation_start (generation_of (0));
        {
#endif //USE_REGIONS
            for (size_t b = brick_of (clear_start);
                    b < brick_of (align_on_brick
                                (heap_segment_allocated (gen0_region)));
                    b++)
            {
                set_brick (b, -1);
            }

#ifdef USE_REGIONS
            gen0_region = heap_segment_next (gen0_region);
#endif //USE_REGIONS
        }
    }
}

void gc_heap::check_gen0_bricks()
{
//#ifdef _DEBUG
    if (gen0_bricks_cleared)
    {
#ifdef USE_REGIONS
        heap_segment* gen0_region = generation_start_segment (generation_of (0));
        while (gen0_region)
        {
            uint8_t* start = heap_segment_mem (gen0_region);
#else
        heap_segment* gen0_region = ephemeral_heap_segment;
        uint8_t* start = generation_allocation_start (generation_of (0));
        {
#endif //USE_REGIONS
            size_t end_b = brick_of (heap_segment_allocated (gen0_region));
            for (size_t b = brick_of (start); b < end_b; b++)
            {
                assert (brick_table[b] != 0);
                if (brick_table[b] == 0)
                {
                    GCToOSInterface::DebugBreak();
                }
            }

#ifdef USE_REGIONS
            gen0_region = heap_segment_next (gen0_region);
#endif //USE_REGIONS
        }
    }
//#endif //_DEBUG
}

#ifdef WRITE_WATCH
#ifdef CARD_BUNDLE
inline void gc_heap::verify_card_bundle_bits_set(size_t first_card_word, size_t last_card_word)
{
#ifdef _DEBUG
    for (size_t x = cardw_card_bundle (first_card_word); x < cardw_card_bundle (last_card_word); x++)
    {
        if (!card_bundle_set_p (x))
        {
            assert (!"Card bundle not set");
            dprintf (3, ("Card bundle %zx not set", x));
        }
    }
#else
    UNREFERENCED_PARAMETER(first_card_word);
    UNREFERENCED_PARAMETER(last_card_word);
#endif
}

// Verifies that any bundles that are not set represent only cards that are not set.
inline void gc_heap::verify_card_bundles()
{
#ifdef _DEBUG
    size_t lowest_card = card_word (card_of (lowest_address));
#ifdef USE_REGIONS
    size_t highest_card = card_word (card_of (global_region_allocator.get_left_used_unsafe()));
#else
    size_t highest_card = card_word (card_of (highest_address));
#endif
    size_t cardb = cardw_card_bundle (lowest_card);
    size_t end_cardb = cardw_card_bundle (align_cardw_on_bundle (highest_card));

    while (cardb < end_cardb)
    {
        uint32_t* card_word = &card_table[max(card_bundle_cardw (cardb), lowest_card)];
        uint32_t* card_word_end = &card_table[min(card_bundle_cardw (cardb+1), highest_card)];

        if (card_bundle_set_p (cardb) == 0)
        {
            // Verify that no card is set
            while (card_word < card_word_end)
            {
                if (*card_word != 0)
                {
                    dprintf  (3, ("gc: %zd, Card word %zx for address %zx set, card_bundle %zx clear",
                            dd_collection_count (dynamic_data_of (0)),
                            (size_t)(card_word-&card_table[0]),
                            (size_t)(card_address ((size_t)(card_word-&card_table[0]) * card_word_width)),
                            cardb));
                }

                assert((*card_word)==0);
                card_word++;
            }
        }

        cardb++;
    }
#endif
}

// If card bundles are enabled, use write watch to find pages in the card table that have
// been dirtied, and set the corresponding card bundle bits.
void gc_heap::update_card_table_bundle()
{
    if (card_bundles_enabled())
    {
        // The address of the card word containing the card representing the lowest heap address
        uint8_t* base_address = (uint8_t*)(&card_table[card_word (card_of (lowest_address))]);

        // The address of the card word containing the card representing the highest heap address
#ifdef USE_REGIONS
        uint8_t* high_address = (uint8_t*)(&card_table[card_word (card_of (global_region_allocator.get_left_used_unsafe()))]);
#else
        uint8_t* high_address = (uint8_t*)(&card_table[card_word (card_of (highest_address))]);
#endif //USE_REGIONS

        uint8_t* saved_base_address = base_address;
        uintptr_t bcount = array_size;
        size_t saved_region_size = align_on_page (high_address) - saved_base_address;

        do
        {
            size_t region_size = align_on_page (high_address) - base_address;

            dprintf (3,("Probing card table pages [%zx, %zx[",
                (size_t)base_address, (size_t)(base_address + region_size)));
            bool success = GCToOSInterface::GetWriteWatch(false /* resetState */,
                                                          base_address,
                                                          region_size,
                                                          (void**)g_addresses,
                                                          &bcount);
            assert (success && "GetWriteWatch failed!");

            dprintf (3,("Found %zd pages written", bcount));
            for (unsigned i = 0; i < bcount; i++)
            {
                // Offset of the dirty page from the start of the card table (clamped to base_address)
                size_t bcardw = (uint32_t*)(max(g_addresses[i],base_address)) - &card_table[0];

                // Offset of the end of the page from the start of the card table (clamped to high addr)
                size_t ecardw = (uint32_t*)(min(g_addresses[i]+OS_PAGE_SIZE, high_address)) - &card_table[0];
                assert (bcardw >= card_word (card_of (g_gc_lowest_address)));

                // Set the card bundle bits representing the dirty card table page
                card_bundles_set (cardw_card_bundle (bcardw),
                                  cardw_card_bundle (align_cardw_on_bundle (ecardw)));
                dprintf (3,("Set Card bundle [%zx, %zx[",
                    cardw_card_bundle (bcardw), cardw_card_bundle (align_cardw_on_bundle (ecardw))));

                verify_card_bundle_bits_set(bcardw, ecardw);
            }

            if (bcount >= array_size)
            {
                base_address = g_addresses [array_size-1] + OS_PAGE_SIZE;
                bcount = array_size;
            }

        } while ((bcount >= array_size) && (base_address < high_address));

        // Now that we've updated the card bundle bits, reset the write-tracking state.
        GCToOSInterface::ResetWriteWatch (saved_base_address, saved_region_size);
    }
}

#endif //CARD_BUNDLE
#endif //WRITE_WATCH
#ifdef COLLECTIBLE_CLASS
// We don't want to burn another ptr size space for pinned plugs to record this so just
// set the card unconditionally for collectible objects if we are demoting.
inline void
gc_heap::unconditional_set_card_collectible (uint8_t* obj)
{
    if (settings.demotion)
    {
        set_card (card_of (obj));
    }
}

#endif //COLLECTIBLE_CLASS

//Clear the cards [start_card, end_card[
void gc_heap::clear_cards (size_t start_card, size_t end_card)
{
    if (start_card < end_card)
    {
        size_t start_word = card_word (start_card);
        size_t end_word = card_word (end_card);
        if (start_word < end_word)
        {
            // Figure out the bit positions of the cards within their words
            unsigned bits = card_bit (start_card);
            card_table [start_word] &= lowbits (~0, bits);
            for (size_t i = start_word+1; i < end_word; i++)
                card_table [i] = 0;
            bits = card_bit (end_card);
            // Don't write beyond end_card (and possibly uncommitted card table space).
            if (bits != 0)
            {
                card_table [end_word] &= highbits (~0, bits);
            }
        }
        else
        {
            // If the start and end cards are in the same word, just clear the appropriate card
            // bits in that word.
            card_table [start_word] &= (lowbits (~0, card_bit (start_card)) |
                                        highbits (~0, card_bit (end_card)));
        }
#if defined(_DEBUG) && defined(VERIFY_HEAP)
        if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
        {
            size_t  card = start_card;
            while (card < end_card)
            {
                assert (!(card_set_p (card)));
                card++;
            }
        }
#endif //_DEBUG && VERIFY_HEAP
        dprintf (3,("Cleared cards [%zx:%zx, %zx:%zx[",
                  start_card, (size_t)card_address (start_card),
                  end_card, (size_t)card_address (end_card)));
    }
}

void gc_heap::clear_card_for_addresses (uint8_t* start_address, uint8_t* end_address)
{
    size_t   start_card = card_of (align_on_card (start_address));
    size_t   end_card = card_of (align_lower_card (end_address));
    clear_cards (start_card, end_card);
}

// copy [srccard, ...[ to [dst_card, end_card[
// This will set the same bit twice. Can be optimized.
inline
void gc_heap::copy_cards (size_t dst_card,
                          size_t src_card,
                          size_t end_card,
                          BOOL nextp)
{
    // If the range is empty, this function is a no-op - with the subtlety that
    // either of the accesses card_table[srcwrd] or card_table[dstwrd] could be
    // outside the committed region.  To avoid the access, leave early.
    if (!(dst_card < end_card))
        return;

    unsigned int srcbit = card_bit (src_card);
    unsigned int dstbit = card_bit (dst_card);
    size_t srcwrd = card_word (src_card);
    size_t dstwrd = card_word (dst_card);
    unsigned int srctmp = card_table[srcwrd];
    unsigned int dsttmp = card_table[dstwrd];

    for (size_t card = dst_card; card < end_card; card++)
    {
        if (srctmp & (1 << srcbit))
            dsttmp |= 1 << dstbit;
        else
            dsttmp &= ~(1 << dstbit);
        if (!(++srcbit % 32))
        {
            srctmp = card_table[++srcwrd];
            srcbit = 0;
        }

        if (nextp)
        {
            if (srctmp & (1 << srcbit))
                dsttmp |= 1 << dstbit;
        }

        if (!(++dstbit % 32))
        {
            card_table[dstwrd] = dsttmp;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            if (dsttmp != 0)
            {
                card_bundle_set(cardw_card_bundle(dstwrd));
            }
#endif

            dstwrd++;
            dsttmp = card_table[dstwrd];
            dstbit = 0;
        }
    }

    card_table[dstwrd] = dsttmp;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    if (dsttmp != 0)
    {
        card_bundle_set(cardw_card_bundle(dstwrd));
    }
#endif
}

void gc_heap::copy_cards_for_addresses (uint8_t* dest, uint8_t* src, size_t len)
{
    ptrdiff_t relocation_distance = src - dest;
    size_t start_dest_card = card_of (align_on_card (dest));
    size_t end_dest_card = card_of (dest + len - 1);
    size_t dest_card = start_dest_card;
    size_t src_card = card_of (card_address (dest_card)+relocation_distance);
    dprintf (3,("Copying cards [%zx:%zx->%zx:%zx, ",
                 src_card, (size_t)src, dest_card, (size_t)dest));
    dprintf (3,(" %zx->%zx:%zx[",
              (size_t)src+len, end_dest_card, (size_t)dest+len));

    dprintf (3, ("dest: %p, src: %p, len: %zx, reloc: %zx, align_on_card(dest) is %p",
        dest, src, len, relocation_distance, (align_on_card (dest))));

    dprintf (3, ("start_dest_card: %zx (address: %p), end_dest_card: %zx(addr: %p), card_of (dest): %zx",
        start_dest_card, card_address (start_dest_card), end_dest_card, card_address (end_dest_card), card_of (dest)));

    //First card has two boundaries
    if (start_dest_card != card_of (dest))
    {
        if ((card_of (card_address (start_dest_card) + relocation_distance) <= card_of (src + len - 1))&&
            card_set_p (card_of (card_address (start_dest_card) + relocation_distance)))
        {
            dprintf (3, ("card_address (start_dest_card) + reloc is %p, card: %zx(set), src+len-1: %p, card: %zx",
                    (card_address (start_dest_card) + relocation_distance),
                    card_of (card_address (start_dest_card) + relocation_distance),
                    (src + len - 1),
                    card_of (src + len - 1)));

            dprintf (3, ("setting card: %zx", card_of (dest)));
            set_card (card_of (dest));
        }
    }

    if (card_set_p (card_of (src)))
        set_card (card_of (dest));


    copy_cards (dest_card, src_card, end_dest_card,
                ((dest - align_lower_card (dest)) != (src - align_lower_card (src))));

    //Last card has two boundaries.
    if ((card_of (card_address (end_dest_card) + relocation_distance) >= card_of (src)) &&
        card_set_p (card_of (card_address (end_dest_card) + relocation_distance)))
    {
        dprintf (3, ("card_address (end_dest_card) + reloc is %p, card: %zx(set), src: %p, card: %zx",
                (card_address (end_dest_card) + relocation_distance),
                card_of (card_address (end_dest_card) + relocation_distance),
                src,
                card_of (src)));

        dprintf (3, ("setting card: %zx", end_dest_card));
        set_card (end_dest_card);
    }

    if (card_set_p (card_of (src + len - 1)))
        set_card (end_dest_card);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    card_bundles_set(cardw_card_bundle(card_word(card_of(dest))), cardw_card_bundle(align_cardw_on_bundle(card_word(end_dest_card))));
#endif
}

#ifdef BACKGROUND_GC
// this does not need the Interlocked version of mark_array_set_marked.
void gc_heap::copy_mark_bits_for_addresses (uint8_t* dest, uint8_t* src, size_t len)
{
    dprintf (3, ("Copying mark_bits for addresses [%zx->%zx, %zx->%zx[",
                 (size_t)src, (size_t)dest,
                 (size_t)src+len, (size_t)dest+len));

    uint8_t* src_o = src;
    uint8_t* dest_o;
    uint8_t* src_end = src + len;
    int align_const = get_alignment_constant (TRUE);
    ptrdiff_t reloc = dest - src;

    while (src_o < src_end)
    {
        uint8_t*  next_o = src_o + Align (size (src_o), align_const);

        if (background_object_marked (src_o, TRUE))
        {
            dest_o = src_o + reloc;
            background_mark (dest_o,
                             background_saved_lowest_address,
                             background_saved_highest_address);
            dprintf (3, ("bc*%zx*bc, b*%zx*b", (size_t)src_o, (size_t)(dest_o)));
        }

        src_o = next_o;
    }
}

#endif //BACKGROUND_GC

void gc_heap::fix_brick_to_highest (uint8_t* o, uint8_t* next_o)
{
    size_t new_current_brick = brick_of (o);
    set_brick (new_current_brick,
               (o - brick_address (new_current_brick)));
    size_t b = 1 + new_current_brick;
    size_t limit = brick_of (next_o);
    //dprintf(3,(" fixing brick %zx to point to object %zx, till %zx(%zx)",
    dprintf(3,("b:%zx->%zx-%zx",
               new_current_brick, (size_t)o, (size_t)next_o));
    while (b < limit)
    {
        set_brick (b,(new_current_brick - b));
        b++;
    }
}

// start can not be >= heap_segment_allocated for the segment.
uint8_t* gc_heap::find_first_object (uint8_t* start, uint8_t* first_object)
{
    size_t brick = brick_of (start);
    uint8_t* o = 0;
    //last_object == null -> no search shortcut needed
    if ((brick == brick_of (first_object) || (start <= first_object)))
    {
        o = first_object;
    }
    else
    {
        ptrdiff_t  min_brick = (ptrdiff_t)brick_of (first_object);
        ptrdiff_t  prev_brick = (ptrdiff_t)brick - 1;
        int         brick_entry = 0;
        while (1)
        {
            if (prev_brick < min_brick)
            {
                break;
            }
            if ((brick_entry = get_brick_entry(prev_brick)) >= 0)
            {
                break;
            }
            assert (! ((brick_entry == 0)));
            prev_brick = (brick_entry + prev_brick);

        }
        o = ((prev_brick < min_brick) ? first_object :
                      brick_address (prev_brick) + brick_entry - 1);
        assert (o <= start);
    }

    assert (Align (size (o)) >= Align (min_obj_size));
    uint8_t*  next_o = o + Align (size (o));
    size_t curr_cl = (size_t)next_o / brick_size;
    size_t min_cl = (size_t)first_object / brick_size;

#ifdef TRACE_GC
    unsigned int n_o = 1;
#endif //TRACE_GC

    uint8_t* next_b = min (align_lower_brick (next_o) + brick_size, start+1);

    while (next_o <= start)
    {
        do
        {
#ifdef TRACE_GC
            n_o++;
#endif //TRACE_GC
            o = next_o;
            assert (Align (size (o)) >= Align (min_obj_size));
            next_o = o + Align (size (o));
            Prefetch (next_o);
        }while (next_o < next_b);

        if (((size_t)next_o / brick_size) != curr_cl)
        {
            if (curr_cl >= min_cl)
            {
                fix_brick_to_highest (o, next_o);
            }
            curr_cl = (size_t) next_o / brick_size;
        }
        next_b = min (align_lower_brick (next_o) + brick_size, start+1);
    }

    size_t bo = brick_of (o);
    //dprintf (3, ("Looked at %u objects, fixing brick [%zx-[%zx",
    dprintf (3, ("%u o, [%zx-[%zx",
        n_o, bo, brick));
    if (bo < brick)
    {
        set_brick (bo, (o - brick_address(bo)));
        size_t b = 1 + bo;
        int x = -1;
        while (b < brick)
        {
            set_brick (b,x--);
            b++;
        }
    }

    return o;
}

#ifdef CARD_BUNDLE
// Find the first non-zero card word between cardw and cardw_end.
// The index of the word we find is returned in cardw.
BOOL gc_heap::find_card_dword (size_t& cardw, size_t cardw_end)
{
    dprintf (3, ("gc: %zd, find_card_dword cardw: %zx, cardw_end: %zx",
                 dd_collection_count (dynamic_data_of (0)), cardw, cardw_end));

    if (card_bundles_enabled())
    {
        size_t cardb = cardw_card_bundle (cardw);
        size_t end_cardb = cardw_card_bundle (align_cardw_on_bundle (cardw_end));
        while (1)
        {
            // Find a non-zero bundle
            while (cardb < end_cardb)
            {
                uint32_t cbw = card_bundle_table[card_bundle_word(cardb)] >> card_bundle_bit (cardb);
                DWORD bit_index;
                if (BitScanForward (&bit_index, cbw))
                {
                    cardb += bit_index;
                    break;
                }
                else
                {
                    cardb += sizeof(cbw)*8 - card_bundle_bit (cardb);
                }
            }
            if (cardb >= end_cardb)
                return FALSE;

            uint32_t* card_word = &card_table[max(card_bundle_cardw (cardb),cardw)];
            uint32_t* card_word_end = &card_table[min(card_bundle_cardw (cardb+1),cardw_end)];
            while ((card_word < card_word_end) && !(*card_word))
            {
                card_word++;
            }

            if (card_word != card_word_end)
            {
                cardw = (card_word - &card_table[0]);
                return TRUE;
            }
            // explore the beginning of the card bundle so we can possibly clear it
            if (cardw == (card_bundle_cardw (cardb) + 1) && !card_table[cardw-1])
            {
                cardw--;
            }
            // explore the end of the card bundle so we can possibly clear it
            card_word_end = &card_table[card_bundle_cardw (cardb+1)];
            while ((card_word < card_word_end) && !(*card_word))
            {
                card_word++;
            }
            if ((cardw <= card_bundle_cardw (cardb)) &&
                (card_word == card_word_end))
            {
                // a whole bundle was explored and is empty
                dprintf  (3, ("gc: %zd, find_card_dword clear bundle: %zx cardw:[%zx,%zx[",
                        dd_collection_count (dynamic_data_of (0)),
                        cardb, card_bundle_cardw (cardb),
                        card_bundle_cardw (cardb+1)));
                card_bundle_clear (cardb);
            }

            cardb++;
        }
    }
    else
    {
        uint32_t* card_word = &card_table[cardw];
        uint32_t* card_word_end = &card_table [cardw_end];

        while (card_word < card_word_end)
        {
            if ((*card_word) != 0)
            {
                cardw = (card_word - &card_table [0]);
                return TRUE;
            }

            card_word++;
        }
        return FALSE;

    }
}

#endif //CARD_BUNDLE

// Find cards that are set between two points in a card table.
// Parameters
//     card_table    : The card table.
//     card          : [in/out] As input, the card to start searching from.
//                              As output, the first card that's set.
//     card_word_end : The card word at which to stop looking.
//     end_card      : [out] The last card which is set.
BOOL gc_heap::find_card(uint32_t* card_table,
                        size_t&   card,
                        size_t    card_word_end,
                        size_t&   end_card)
{
    uint32_t* last_card_word;
    uint32_t card_word_value;
    uint32_t bit_position;

    if (card_word (card) >= card_word_end)
        return FALSE;

    // Find the first card which is set
    last_card_word = &card_table [card_word (card)];
    bit_position = card_bit (card);
#ifdef CARD_BUNDLE
    // if we have card bundles, consult them before fetching a new card word
    if (bit_position == 0)
    {
        card_word_value = 0;
    }
    else
#endif
    {
        card_word_value = (*last_card_word) >> bit_position;
    }
    if (!card_word_value)
    {
#ifdef CARD_BUNDLE
        // Using the card bundle, go through the remaining card words between here and
        // card_word_end until we find one that is non-zero.
        size_t lcw = card_word(card) + (bit_position != 0);
        if (gc_heap::find_card_dword (lcw, card_word_end) == FALSE)
        {
            return FALSE;
        }
        else
        {
            last_card_word = &card_table [lcw];
            card_word_value = *last_card_word;
        }
        bit_position = 0;
#else //CARD_BUNDLE
        // Go through the remaining card words between here and card_word_end until we find
        // one that is non-zero.
        do
        {
            ++last_card_word;
        }

        while ((last_card_word < &card_table [card_word_end]) && !(*last_card_word));
        if (last_card_word < &card_table [card_word_end])
        {
            card_word_value = *last_card_word;
        }
        else
        {
            // We failed to find any non-zero card words before we got to card_word_end
            return FALSE;
        }
#endif //CARD_BUNDLE
    }

    // Look for the lowest bit set
    if (card_word_value)
    {
        DWORD bit_index;
        uint8_t res = BitScanForward (&bit_index, card_word_value);
        assert (res != 0);
        card_word_value >>= bit_index;
        bit_position += bit_index;
    }

    // card is the card word index * card size + the bit index within the card
    card = (last_card_word - &card_table[0]) * card_word_width + bit_position;

    do
    {
        // Keep going until we get to an un-set card.
        bit_position++;
        card_word_value = card_word_value / 2;

        // If we reach the end of the card word and haven't hit a 0 yet, start going
        // card word by card word until we get to one that's not fully set (0xFFFF...)
        // or we reach card_word_end.
        if ((bit_position == card_word_width) && (last_card_word < &card_table [card_word_end-1]))
        {
            do
            {
                card_word_value = *(++last_card_word);
            } while ((last_card_word < &card_table [card_word_end-1]) &&
                     (card_word_value == ~0u /* (1 << card_word_width)-1 */));
            bit_position = 0;
        }
    } while (card_word_value & 1);

    end_card = (last_card_word - &card_table [0])* card_word_width + bit_position;

    //dprintf (3, ("find_card: [%zx, %zx[ set", card, end_card));
    dprintf (3, ("fc: [%zx, %zx[", card, end_card));
    return TRUE;
}

BOOL gc_heap::card_transition (uint8_t* po, uint8_t* end, size_t card_word_end,
                               size_t& cg_pointers_found,
                               size_t& n_eph, size_t& n_card_set,
                               size_t& card, size_t& end_card,
                               BOOL& foundp, uint8_t*& start_address,
                               uint8_t*& limit, size_t& n_cards_cleared
                               CARD_MARKING_STEALING_ARGS(card_marking_enumerator& card_mark_enumerator, heap_segment* seg, size_t &card_word_end_out))
{
    dprintf (3, ("pointer %zx past card %zx, cg %zd", (size_t)po, (size_t)card, cg_pointers_found));
    BOOL passed_end_card_p = FALSE;
    foundp = FALSE;

    if (cg_pointers_found == 0)
    {
        //dprintf(3,(" Clearing cards [%zx, %zx[ ",
        dprintf(3,(" CC [%zx, %zx[ ",
                (size_t)card_address(card), (size_t)po));
        uint8_t* card_clearing_limit = po;
#ifdef FEATURE_CARD_MARKING_STEALING
        card_clearing_limit = min (limit, po);
#endif // FEATURE_CARD_MARKING_STEALING
        clear_cards (card, card_of (card_clearing_limit));
        n_card_set -= (card_of (card_clearing_limit) - card);
        n_cards_cleared += (card_of (card_clearing_limit) - card);
    }
    n_eph +=cg_pointers_found;
    cg_pointers_found = 0;
    card = card_of (po);
    if (card >= end_card)
    {
        passed_end_card_p = TRUE;
        dprintf (3, ("card %zx exceeding end_card %zx",
                    (size_t)card, (size_t)end_card));
        foundp = find_card (card_table, card, card_word_end, end_card);
        if (foundp)
        {
            n_card_set+= end_card - card;
            start_address = card_address (card);
            dprintf (3, ("NewC: %zx, start: %zx, end: %zx",
                        (size_t)card, (size_t)start_address,
                        (size_t)card_address (end_card)));
        }
        limit = min (end, card_address (end_card));

#ifdef FEATURE_CARD_MARKING_STEALING
        // the card bit @ end_card should not be set
        // if end_card is still shy of the limit set by card_word_end
        assert(!((card_word(end_card) < card_word_end) &&
            card_set_p(end_card)));
        if (!foundp)
        {
            card_word_end_out = 0;
            foundp = find_next_chunk(card_mark_enumerator, seg, n_card_set, start_address, limit, card, end_card, card_word_end_out);
        }
#else
        // the card bit @ end_card should not be set -
        // find_card is supposed to terminate only when it finds a 0 bit
        // or the end of the segment
        assert (!((limit < end) &&
                card_set_p (end_card)));
#endif
    }

    return passed_end_card_p;
}

#ifdef FEATURE_CARD_MARKING_STEALING
bool gc_heap::find_next_chunk(card_marking_enumerator& card_mark_enumerator, heap_segment* seg, size_t& n_card_set,
    uint8_t*& start_address, uint8_t*& limit,
    size_t& card, size_t& end_card, size_t& card_word_end)
{
    while (true)
    {
        if (card_word_end != 0 && find_card(card_table, card, card_word_end, end_card))
        {
            assert(end_card <= card_word_end * card_word_width);
            n_card_set += end_card - card;
            start_address = card_address(card);
            dprintf(3, ("NewC: %zx, start: %zx, end: %zx",
                (size_t)card, (size_t)start_address,
                (size_t)card_address(end_card)));
            limit = min(card_mark_enumerator.get_chunk_high(), card_address(end_card));
            dprintf (3, ("New run of cards on heap %d: [%zx,%zx[", heap_number, (size_t)start_address, (size_t)limit));
            return true;
        }
        // we have exhausted this chunk, get the next one
        uint8_t* chunk_low = nullptr;
        uint8_t* chunk_high = nullptr;
        if (!card_mark_enumerator.move_next(seg, chunk_low, chunk_high))
        {
            dprintf (3, ("No more chunks on heap %d\n", heap_number));
            return false;
        }
        card = max(card, card_of(chunk_low));
        card_word_end = (card_of(align_on_card_word(chunk_high)) / card_word_width);
        dprintf (3, ("Moved to next chunk on heap %d: [%zx,%zx[", heap_number, (size_t)chunk_low, (size_t)chunk_high));
    }
}

#endif //FEATURE_CARD_MARKING_STEALING
