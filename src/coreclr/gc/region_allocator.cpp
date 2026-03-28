// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifdef USE_REGIONS
bool region_allocator::init (uint8_t* start, uint8_t* end, size_t alignment, uint8_t** lowest, uint8_t** highest)
{
    uint8_t* actual_start = start;
    region_alignment = alignment;
    large_region_alignment = LARGE_REGION_FACTOR * alignment;
    global_region_start = (uint8_t*)align_region_up ((size_t)actual_start);
    uint8_t* actual_end = end;
    global_region_end = (uint8_t*)align_region_down ((size_t)actual_end);
    global_region_left_used = global_region_start;
    global_region_right_used = global_region_end;
    num_left_used_free_units = 0;
    num_right_used_free_units = 0;

    // Note: I am allocating a map that covers the whole reserved range.
    // We can optimize it to only cover the current heap range.
    size_t total_num_units = (global_region_end - global_region_start) / region_alignment;
    total_free_units = (uint32_t)total_num_units;

    uint32_t* unit_map = new (nothrow) uint32_t[total_num_units];
    if (unit_map)
    {
        memset (unit_map, 0, sizeof (uint32_t) * total_num_units);
        region_map_left_start = unit_map;
        region_map_left_end = region_map_left_start;

        region_map_right_start = unit_map + total_num_units;
        region_map_right_end = region_map_right_start;

        dprintf (REGIONS_LOG, ("start: %zx, end: %zx, total %zdmb(alignment: %zdmb), map units %zd",
            (size_t)start, (size_t)end,
            (size_t)((end - start) / 1024 / 1024),
            (alignment / 1024 / 1024),
            total_num_units));

        *lowest = global_region_start;
        *highest = global_region_end;
    }
    else
    {
        log_init_error_to_host ("global region allocator failed to allocate %zd bytes during init", (total_num_units * sizeof (uint32_t)));
    }

    return (unit_map != 0);
}

inline
uint8_t* region_allocator::region_address_of (uint32_t* map_index)
{
    return (global_region_start + ((map_index - region_map_left_start) * region_alignment));
}

inline
uint32_t* region_allocator::region_map_index_of (uint8_t* address)
{
    return (region_map_left_start + ((address - global_region_start) / region_alignment));
}

void region_allocator::make_busy_block (uint32_t* index_start, uint32_t num_units)
{
#ifdef _DEBUG
    dprintf (REGIONS_LOG, ("MBB[B: %zd] %d->%d", (size_t)num_units, (int)(index_start - region_map_left_start), (int)(index_start - region_map_left_start + num_units)));
#endif //_DEBUG
    ASSERT_HOLDING_SPIN_LOCK (&region_allocator_lock);
    uint32_t* index_end = index_start + (num_units - 1);
    *index_start = *index_end = num_units;
}

void region_allocator::make_free_block (uint32_t* index_start, uint32_t num_units)
{
#ifdef _DEBUG
    dprintf (REGIONS_LOG, ("MFB[F: %zd] %d->%d", (size_t)num_units, (int)(index_start - region_map_left_start), (int)(index_start - region_map_left_start + num_units)));
#endif //_DEBUG
    ASSERT_HOLDING_SPIN_LOCK (&region_allocator_lock);
    uint32_t* index_end = index_start + (num_units - 1);
    *index_start = *index_end = region_alloc_free_bit | num_units;
}

void region_allocator::print_map (const char* msg)
{
    ASSERT_HOLDING_SPIN_LOCK (&region_allocator_lock);
#ifdef _DEBUG
    const char* heap_type = "UH";
    dprintf (REGIONS_LOG, ("[%s]-----printing----%s", heap_type, msg));

    uint32_t* current_index = region_map_left_start;
    uint32_t* end_index = region_map_left_end;
    uint32_t  count_free_units = 0;

    for (int i = 0; i < 2; i++)
    {
        while (current_index < end_index)
        {
            uint32_t current_val = *current_index;
            uint32_t current_num_units = get_num_units (current_val);
            bool free_p = is_unit_memory_free (current_val);

            dprintf (REGIONS_LOG, ("[%s][%s: %zd]%d->%d", heap_type, (free_p ? "F" : "B"), (size_t)current_num_units,
                (int)(current_index - region_map_left_start),
                (int)(current_index - region_map_left_start + current_num_units)));

            if (free_p)
            {
                count_free_units += current_num_units;
            }

            current_index += current_num_units;
        }
        current_index = region_map_right_start;
        end_index = region_map_right_end;
        if (i == 0)
        {
            assert (count_free_units == num_left_used_free_units);
        }
        else
        {
            assert (count_free_units == num_left_used_free_units + num_right_used_free_units);
        }
    }

    count_free_units += (uint32_t)(region_map_right_start - region_map_left_end);
    assert(count_free_units == total_free_units);

    uint32_t total_regions = (uint32_t)((global_region_end - global_region_start) / region_alignment);

    dprintf (REGIONS_LOG, ("[%s]-----end printing----[%d total, left used %zd (free: %d), right used %zd (free: %d)]\n", heap_type, total_regions,
        (region_map_left_end - region_map_left_start), num_left_used_free_units, (region_map_right_end - region_map_right_start), num_right_used_free_units));
#endif //_DEBUG
}

uint8_t* region_allocator::allocate_end (uint32_t num_units, allocate_direction direction)
{
    uint8_t* alloc = NULL;

    ASSERT_HOLDING_SPIN_LOCK (&region_allocator_lock);

    if (global_region_left_used < global_region_right_used)
    {
        size_t end_remaining = global_region_right_used - global_region_left_used;

        if ((end_remaining / region_alignment) >= num_units)
        {
            if (direction == allocate_forward)
            {
                make_busy_block (region_map_left_end, num_units);
                region_map_left_end += num_units;
                alloc = global_region_left_used;
                global_region_left_used += num_units * region_alignment;
            }
            else
            {
                assert(direction == allocate_backward);
                region_map_right_start -= num_units;
                make_busy_block (region_map_right_start, num_units);
                global_region_right_used -= num_units * region_alignment;
                alloc = global_region_right_used;
            }
        }
    }

    return alloc;
}

void region_allocator::enter_spin_lock()
{
    while (true)
    {
        if (Interlocked::CompareExchange(&region_allocator_lock.lock, 0, -1) < 0)
            break;

        while (region_allocator_lock.lock >= 0)
        {
            YieldProcessor();           // indicate to the processor that we are spinning
        }
    }
#ifdef _DEBUG
    region_allocator_lock.holding_thread = GCToEEInterface::GetThread();
#endif //_DEBUG
}

void region_allocator::leave_spin_lock()
{
#ifdef _DEBUG
    region_allocator_lock.holding_thread = (Thread*)-1;
#endif //_DEBUG
    region_allocator_lock.lock = -1;
}

uint8_t* region_allocator::allocate (uint32_t num_units, allocate_direction direction, region_allocator_callback_fn fn)
{
    enter_spin_lock();

    uint32_t* current_index;
    uint32_t* end_index;
    if (direction == allocate_forward)
    {
        current_index = region_map_left_start;
        end_index = region_map_left_end;
    }
    else
    {
        assert(direction == allocate_backward);
        current_index = region_map_right_end;
        end_index = region_map_right_start;
    }

    dprintf (REGIONS_LOG, ("searching %d->%d", (int)(current_index - region_map_left_start), (int)(end_index - region_map_left_start)));

    print_map ("before alloc");

    if (((direction == allocate_forward) && (num_left_used_free_units >= num_units)) ||
        ((direction == allocate_backward) && (num_right_used_free_units >= num_units)))
    {
        while (((direction == allocate_forward) && (current_index < end_index)) ||
            ((direction == allocate_backward) && (current_index > end_index)))
        {
            uint32_t current_val = *(current_index - ((direction == allocate_backward) ? 1 : 0));
            uint32_t current_num_units = get_num_units (current_val);
            bool free_p = is_unit_memory_free (current_val);
            dprintf (REGIONS_LOG, ("ALLOC[%s: %zd]%d->%d", (free_p ? "F" : "B"), (size_t)current_num_units,
                (int)(current_index - region_map_left_start), (int)(current_index + current_num_units - region_map_left_start)));

            if (free_p)
            {
                if (current_num_units >= num_units)
                {
                    dprintf (REGIONS_LOG, ("found %zd contiguous free units(%d->%d), sufficient",
                        (size_t)current_num_units,
                        (int)(current_index - region_map_left_start),
                        (int)(current_index - region_map_left_start + current_num_units)));

                    if (direction == allocate_forward)
                    {
                        assert (num_left_used_free_units >= num_units);
                        num_left_used_free_units -= num_units;
                    }
                    else
                    {
                        assert (direction == allocate_backward);
                        assert (num_right_used_free_units >= num_units);
                        num_right_used_free_units -= num_units;
                    }

                    uint32_t* busy_block;
                    uint32_t* free_block;
                    if (direction == 1)
                    {
                        busy_block = current_index;
                        free_block = current_index + num_units;
                    }
                    else
                    {
                        busy_block = current_index - num_units;
                        free_block = current_index - current_num_units;
                    }

                    make_busy_block (busy_block, num_units);
                    if ((current_num_units - num_units) > 0)
                    {
                        make_free_block (free_block, (current_num_units - num_units));
                    }

                    total_free_units -= num_units;
                    print_map ("alloc: found in free");

                    leave_spin_lock();

                    return region_address_of (busy_block);
                }
            }

            if (direction == allocate_forward)
            {
                current_index += current_num_units;
            }
            else
            {
                current_index -= current_num_units;
            }
        }
    }

    uint8_t* alloc = allocate_end (num_units, direction);

    if (alloc)
    {
        total_free_units -= num_units;
        if (fn != nullptr)
        {
            if (!fn (global_region_left_used))
            {
                delete_region_impl (alloc);
                alloc = nullptr;
            }
        }
        if (alloc)
        {
            print_map ("alloc: found at the end");
        }
    }
    else
    {
        dprintf (REGIONS_LOG, ("couldn't find memory at the end! only %zd bytes left", (global_region_right_used - global_region_left_used)));
    }

    leave_spin_lock();

    return alloc;
}

bool region_allocator::allocate_region (int gen_num, size_t size, uint8_t** start, uint8_t** end, allocate_direction direction, region_allocator_callback_fn fn)
{
    size_t alignment = region_alignment;
    size_t alloc_size = align_region_up (size);

    uint32_t num_units = (uint32_t)(alloc_size / alignment);
    bool ret = false;
    uint8_t* alloc = NULL;
    dprintf (REGIONS_LOG, ("----GET %u-----", num_units));

    alloc = allocate (num_units, direction, fn);
    *start = alloc;
    *end = alloc + alloc_size;
    ret = (alloc != NULL);

    gc_etw_segment_type segment_type;

    if (gen_num == loh_generation)
    {
        segment_type = gc_etw_segment_large_object_heap;
    }
    else if (gen_num == poh_generation)
    {
        segment_type = gc_etw_segment_pinned_object_heap;
    }
    else
    {
        segment_type = gc_etw_segment_small_object_heap;
    }

    FIRE_EVENT(GCCreateSegment_V1, (alloc + sizeof (aligned_plug_and_gap)),
                                  size - sizeof (aligned_plug_and_gap),
                                  segment_type);

    return ret;
}

bool region_allocator::allocate_basic_region (int gen_num, uint8_t** start, uint8_t** end, region_allocator_callback_fn fn)
{
    return allocate_region (gen_num, region_alignment, start, end, allocate_forward, fn);
}

// Large regions are 8x basic region sizes by default. If you need a larger region than that,
// call allocate_region with the size.
bool region_allocator::allocate_large_region (int gen_num, uint8_t** start, uint8_t** end, allocate_direction direction, size_t size, region_allocator_callback_fn fn)
{
    if (size == 0)
        size = large_region_alignment;
    else
    {
        // round up size to a multiple of large_region_alignment
        // for the below computation to work, large_region_alignment must be a power of 2
        assert (round_up_power2(large_region_alignment) == large_region_alignment);
        size = (size + (large_region_alignment - 1)) & ~(large_region_alignment - 1);
    }
    return allocate_region (gen_num, size, start, end, direction, fn);
}

// Whenever a region is deleted, it is expected that the memory and the mark array
// of the region is decommitted already.
void region_allocator::delete_region (uint8_t* region_start)
{
    enter_spin_lock();
    delete_region_impl (region_start);
    leave_spin_lock();
}

void region_allocator::delete_region_impl (uint8_t* region_start)
{
    ASSERT_HOLDING_SPIN_LOCK (&region_allocator_lock);
    assert (is_region_aligned (region_start));

    print_map ("before delete");

    uint32_t* current_index = region_map_index_of (region_start);
    uint32_t current_val = *current_index;
    assert (!is_unit_memory_free (current_val));

    dprintf (REGIONS_LOG, ("----DEL %d (%u units)-----", (*current_index - *region_map_left_start), current_val));
    uint32_t* region_end_index = current_index + current_val;
    uint8_t* region_end = region_address_of (region_end_index);

    int free_block_size = current_val;
    uint32_t* free_index = current_index;

    if (free_index <= region_map_left_end)
    {
        num_left_used_free_units += free_block_size;
    }
    else
    {
        assert (free_index >= region_map_right_start);
        num_right_used_free_units += free_block_size;
    }

    if ((current_index != region_map_left_start) && (current_index != region_map_right_start))
    {
        uint32_t previous_val = *(current_index - 1);
        if (is_unit_memory_free(previous_val))
        {
            uint32_t previous_size = get_num_units (previous_val);
            free_index -= previous_size;
            free_block_size += previous_size;
        }
    }
    if ((region_end != global_region_left_used) && (region_end != global_region_end))
    {
        uint32_t next_val = *region_end_index;
        if (is_unit_memory_free(next_val))
        {
            uint32_t next_size = get_num_units (next_val);
            free_block_size += next_size;
            region_end += next_size;
        }
    }
    if (region_end == global_region_left_used)
    {
        num_left_used_free_units -= free_block_size;
        region_map_left_end = free_index;
        dprintf (REGIONS_LOG, ("adjust global left used from %p to %p",
            global_region_left_used, region_address_of (free_index)));
        global_region_left_used = region_address_of (free_index);
    }
    else if (region_start == global_region_right_used)
    {
        num_right_used_free_units -= free_block_size;
        region_map_right_start = free_index + free_block_size;
        dprintf (REGIONS_LOG, ("adjust global right used from %p to %p",
            global_region_right_used, region_address_of (free_index + free_block_size)));
        global_region_right_used = region_address_of (free_index + free_block_size);
    }
    else
    {
        make_free_block (free_index, free_block_size);
    }

    total_free_units += current_val;

    print_map ("after delete");
}

void region_allocator::move_highest_free_regions (int64_t n, bool small_region_p, region_free_list to_free_list[count_free_region_kinds])
{
    assert (n > 0);

    uint32_t* current_index = region_map_left_end - 1;
    uint32_t* lowest_index = region_map_left_start;

    while (current_index >= lowest_index)
    {
        uint32_t current_val = *current_index;
        uint32_t current_num_units = get_num_units (current_val);
        bool free_p = is_unit_memory_free (current_val);
        if (!free_p && ((current_num_units == 1) == small_region_p))
        {
            uint32_t* index = current_index - (current_num_units - 1);
            heap_segment* region = get_region_info (region_address_of (index));
            if (is_free_region (region) && !region_free_list::is_on_free_list (region, to_free_list))
            {
                if (n >= current_num_units)
                {
                    n -= current_num_units;

                    region_free_list::unlink_region (region);

                    region_free_list::add_region (region, to_free_list);
                }
                else
                {
                    break;
                }
            }
        }
        current_index -= current_num_units;
    }
}
#endif //USE_REGIONS
