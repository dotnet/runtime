// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

inline
size_t clear_special_bits (uint8_t* node)
{
    return header(node)->ClearSpecialBits();
}

inline
void set_special_bits (uint8_t* node, size_t special_bits)
{
    header(node)->SetSpecialBits (special_bits);
}

void gc_heap::reset_pinned_queue()
{
    mark_stack_tos = 0;
    mark_stack_bos = 0;
}

void gc_heap::reset_pinned_queue_bos()
{
    mark_stack_bos = 0;
}

// last_pinned_plug is only for asserting purpose.
void gc_heap::merge_with_last_pinned_plug (uint8_t* last_pinned_plug, size_t plug_size)
{
    if (last_pinned_plug)
    {
        mark& last_m = mark_stack_array[mark_stack_tos - 1];
        assert (last_pinned_plug == last_m.first);
        if (last_m.saved_post_p)
        {
            last_m.saved_post_p = FALSE;
            dprintf (3, ("setting last plug %p post to false", last_m.first));
            // We need to recover what the gap has overwritten.
            memcpy ((last_m.first + last_m.len - sizeof (plug_and_gap)), &(last_m.saved_post_plug), sizeof (gap_reloc_pair));
        }
        last_m.len += plug_size;
        dprintf (3, ("recovered the last part of plug %p, setting its plug size to %zx", last_m.first, last_m.len));
    }
}

void gc_heap::set_allocator_next_pin (generation* gen)
{
    dprintf (3, ("SANP: gen%d, ptr; %p, limit: %p", gen->gen_num, generation_allocation_pointer (gen), generation_allocation_limit (gen)));
    if (!(pinned_plug_que_empty_p()))
    {
        mark*  oldest_entry = oldest_pin();
        uint8_t* plug = pinned_plug (oldest_entry);
        if ((plug >= generation_allocation_pointer (gen)) &&
            (plug <  generation_allocation_limit (gen)))
        {
#ifdef USE_REGIONS
            assert (region_of (generation_allocation_pointer (gen)) ==
                    region_of (generation_allocation_limit (gen) - 1));
#endif //USE_REGIONS
            generation_allocation_limit (gen) = pinned_plug (oldest_entry);
            dprintf (3, ("SANP: get next pin free space in gen%d for alloc: %p->%p(%zd)",
                gen->gen_num,
                generation_allocation_pointer (gen), generation_allocation_limit (gen),
                (generation_allocation_limit (gen) - generation_allocation_pointer (gen))));
        }
        else
            assert (!((plug < generation_allocation_pointer (gen)) &&
                      (plug >= heap_segment_mem (generation_allocation_segment (gen)))));
    }
}

// After we set the info, we increase tos.
void gc_heap::set_pinned_info (uint8_t* last_pinned_plug, size_t plug_len, generation* gen)
{
#ifndef _DEBUG
    UNREFERENCED_PARAMETER(last_pinned_plug);
#endif //_DEBUG

    mark& m = mark_stack_array[mark_stack_tos];
    assert (m.first == last_pinned_plug);

    m.len = plug_len;
    mark_stack_tos++;
    assert (gen != 0);
    // Why are we checking here? gen is never 0.
    if (gen != 0)
    {
        set_allocator_next_pin (gen);
    }
}

size_t gc_heap::deque_pinned_plug ()
{
    size_t m = mark_stack_bos;
    dprintf (3, ("deque: %zd->%p", mark_stack_bos, pinned_plug (pinned_plug_of (m))));
    mark_stack_bos++;
    return m;
}

inline
mark* gc_heap::pinned_plug_of (size_t bos)
{
    return &mark_stack_array [ bos ];
}

inline
mark* gc_heap::oldest_pin ()
{
    return pinned_plug_of (mark_stack_bos);
}

inline
BOOL gc_heap::pinned_plug_que_empty_p ()
{
    return (mark_stack_bos == mark_stack_tos);
}

inline
mark* gc_heap::before_oldest_pin()
{
    if (mark_stack_bos >= 1)
        return pinned_plug_of (mark_stack_bos-1);
    else
        return 0;
}

#ifdef MH_SC_MARK
inline
int& gc_heap::mark_stack_busy()
{
    return  g_mark_stack_busy [(heap_number+2)*HS_CACHE_LINE_SIZE/sizeof(int)];
}

#endif //MH_SC_MARK

void gc_heap::make_mark_stack (mark* arr)
{
    reset_pinned_queue();
    mark_stack_array = arr;
    mark_stack_array_length = MARK_STACK_INITIAL_LENGTH;
#ifdef MH_SC_MARK
    mark_stack_busy() = 0;
#endif //MH_SC_MARK
}

#ifdef BACKGROUND_GC
inline
size_t& gc_heap::bpromoted_bytes(int thread)
{
#ifdef MULTIPLE_HEAPS
    return g_bpromoted [thread*16];
#else //MULTIPLE_HEAPS
    UNREFERENCED_PARAMETER(thread);
    return g_bpromoted;
#endif //MULTIPLE_HEAPS
}

void gc_heap::make_background_mark_stack (uint8_t** arr)
{
    background_mark_stack_array = arr;
    background_mark_stack_array_length = MARK_STACK_INITIAL_LENGTH;
    background_mark_stack_tos = arr;
}

void gc_heap::make_c_mark_list (uint8_t** arr)
{
    c_mark_list = arr;
    c_mark_list_index = 0;
    c_mark_list_length = 1 + (OS_PAGE_SIZE / MIN_OBJECT_SIZE);
}

inline
unsigned int gc_heap::mark_array_marked(uint8_t* add)
{
    return mark_array [mark_word_of (add)] & (1 << mark_bit_bit_of (add));
}

inline
BOOL gc_heap::is_mark_bit_set (uint8_t* add)
{
    return (mark_array [mark_word_of (add)] & (1 << mark_bit_bit_of (add)));
}

inline
void gc_heap::mark_array_set_marked (uint8_t* add)
{
    size_t index = mark_word_of (add);
    uint32_t val = (1 << mark_bit_bit_of (add));
#ifdef MULTIPLE_HEAPS
    Interlocked::Or (&(mark_array [index]), val);
#else
    mark_array [index] |= val;
#endif
}

inline
void gc_heap::mark_array_clear_marked (uint8_t* add)
{
    mark_array [mark_word_of (add)] &= ~(1 << mark_bit_bit_of (add));
}

#ifdef FEATURE_BASICFREEZE
// end must be page aligned addresses.
void gc_heap::clear_mark_array (uint8_t* from, uint8_t* end)
{
    assert (gc_can_use_concurrent);
    assert (end == align_on_mark_word (end));

    uint8_t* current_lowest_address = background_saved_lowest_address;
    uint8_t* current_highest_address = background_saved_highest_address;

    //there is a possibility of the addresses to be
    //outside of the covered range because of a newly allocated
    //large object segment
    if ((end <= current_highest_address) && (from >= current_lowest_address))
    {
        size_t beg_word = mark_word_of (align_on_mark_word (from));
        //align end word to make sure to cover the address
        size_t end_word = mark_word_of (align_on_mark_word (end));
        dprintf (3, ("Calling clearing mark array [%zx, %zx[ for addresses [%zx, %zx[",
                     (size_t)mark_word_address (beg_word),
                     (size_t)mark_word_address (end_word),
                     (size_t)from, (size_t)end));

        uint8_t* op = from;
        while (op < mark_word_address (beg_word))
        {
            mark_array_clear_marked (op);
            op += mark_bit_pitch;
        }

        memset (&mark_array[beg_word], 0, (end_word - beg_word)*sizeof (uint32_t));

#ifdef _DEBUG
        //Beware, it is assumed that the mark array word straddling
        //start has been cleared before
        //verify that the array is empty.
        size_t  markw = mark_word_of (align_on_mark_word (from));
        size_t  markw_end = mark_word_of (align_on_mark_word (end));
        while (markw < markw_end)
        {
            assert (!(mark_array [markw]));
            markw++;
        }
        uint8_t* p = mark_word_address (markw_end);
        while (p < end)
        {
            assert (!(mark_array_marked (p)));
            p++;
        }
#endif //_DEBUG
    }
}

#endif //FEATURE_BASICFREEZE
#endif //BACKGROUND_GC
#ifdef MULTIPLE_HEAPS
static size_t target_mark_count_for_heap (size_t total_mark_count, int heap_count, int heap_number)
{
    // compute the average (rounded down)
    size_t average_mark_count = total_mark_count / heap_count;

    // compute the remainder
    size_t remaining_mark_count = total_mark_count - (average_mark_count * heap_count);

    // compute the target count for this heap - last heap has the remainder
    if (heap_number == (heap_count - 1))
        return (average_mark_count + remaining_mark_count);
    else
        return average_mark_count;
}

NOINLINE
uint8_t** gc_heap::equalize_mark_lists (size_t total_mark_list_size)
{
    size_t local_mark_count[MAX_SUPPORTED_HEAPS];
    size_t total_mark_count = 0;

    // compute mark count per heap into a local array
    // compute the total
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
        size_t mark_count = hp->mark_list_index - hp->mark_list;
        local_mark_count[i] = mark_count;
        total_mark_count += mark_count;
    }

    // this should agree with our input parameter
    assert(total_mark_count == total_mark_list_size);

    // compute the target count for this heap
    size_t this_target_mark_count = target_mark_count_for_heap (total_mark_count, n_heaps, heap_number);

    // if our heap has sufficient entries, we can exit early
    if (local_mark_count[heap_number] >= this_target_mark_count)
        return (mark_list + this_target_mark_count);

    // In the following, we try to fill the deficit in heap "deficit_heap_index" with
    // surplus from "surplus_heap_index".
    // If there is no deficit or surplus (anymore), the indices are advanced.
    int surplus_heap_index = 0;
    for (int deficit_heap_index = 0; deficit_heap_index <= heap_number; deficit_heap_index++)
    {
        // compute the target count for this heap - last heap has the remainder
        size_t deficit_target_mark_count = target_mark_count_for_heap (total_mark_count, n_heaps, deficit_heap_index);

        // if this heap has the target or larger count, skip it
        if (local_mark_count[deficit_heap_index] >= deficit_target_mark_count)
            continue;

        // while this heap is lower than average, fill it up
        while ((surplus_heap_index < n_heaps) && (local_mark_count[deficit_heap_index] < deficit_target_mark_count))
        {
            size_t deficit = deficit_target_mark_count - local_mark_count[deficit_heap_index];

            size_t surplus_target_mark_count = target_mark_count_for_heap(total_mark_count, n_heaps, surplus_heap_index);

            if (local_mark_count[surplus_heap_index] > surplus_target_mark_count)
            {
                size_t surplus = local_mark_count[surplus_heap_index] - surplus_target_mark_count;
                size_t amount_to_transfer = min(deficit, surplus);
                local_mark_count[surplus_heap_index] -= amount_to_transfer;
                if (deficit_heap_index == heap_number)
                {
                    // copy amount_to_transfer mark list items
                    memcpy(&g_heaps[deficit_heap_index]->mark_list[local_mark_count[deficit_heap_index]],
                           &g_heaps[surplus_heap_index]->mark_list[local_mark_count[surplus_heap_index]],
                           (amount_to_transfer*sizeof(mark_list[0])));
                }
                local_mark_count[deficit_heap_index] += amount_to_transfer;
            }
            else
            {
                surplus_heap_index++;
            }
        }
    }
    return (mark_list + local_mark_count[heap_number]);
}

NOINLINE
size_t gc_heap::sort_mark_list()
{
    if ((settings.condemned_generation >= max_generation)
#ifdef USE_REGIONS
      || (g_mark_list_piece == nullptr)
#endif //USE_REGIONS
        )
    {
        // fake a mark list overflow so merge_mark_lists knows to quit early
        mark_list_index = mark_list_end + 1;
        return 0;
    }

    // if this heap had a mark list overflow, we don't do anything
    if (mark_list_index > mark_list_end)
    {
        dprintf (2, ("h%d sort_mark_list overflow", heap_number));
        mark_list_overflow = true;
        return 0;
    }

    // if any other heap had a mark list overflow, we fake one too,
    // so we don't use an incomplete mark list by mistake
    for (int i = 0; i < n_heaps; i++)
    {
        if (g_heaps[i]->mark_list_index > g_heaps[i]->mark_list_end)
        {
            mark_list_index = mark_list_end + 1;
            dprintf (2, ("h%d sort_mark_list: detected overflow on heap %d", heap_number, i));
            return 0;
        }
    }

    // compute total mark list size and total ephemeral size
    size_t total_mark_list_size = 0;
    size_t total_ephemeral_size = 0;
    uint8_t* low = (uint8_t*)~0;
    uint8_t* high = 0;
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];
        total_mark_list_size += (hp->mark_list_index - hp->mark_list);
#ifdef USE_REGIONS
        // iterate through the ephemeral regions to get a tighter bound
        for (int gen_num = settings.condemned_generation; gen_num >= 0; gen_num--)
        {
            generation* gen = hp->generation_of (gen_num);
            for (heap_segment* seg = generation_start_segment (gen); seg != nullptr; seg = heap_segment_next (seg))
            {
                size_t ephemeral_size = heap_segment_allocated (seg) - heap_segment_mem (seg);
                total_ephemeral_size += ephemeral_size;
                low = min (low, heap_segment_mem (seg));
                high = max (high, heap_segment_allocated (seg));
            }
        }
#else //USE_REGIONS
        size_t ephemeral_size = heap_segment_allocated (hp->ephemeral_heap_segment) - hp->gc_low;
        total_ephemeral_size += ephemeral_size;
        low = min (low, hp->gc_low);
        high = max (high, heap_segment_allocated (hp->ephemeral_heap_segment));
#endif //USE_REGIONS
    }

    // give up if the mark list size is unreasonably large
    if (total_mark_list_size > (total_ephemeral_size / 256))
    {
        mark_list_index = mark_list_end + 1;
        // let's not count this as a mark list overflow
        dprintf (2, ("h%d total mark list %zd is too large > (%zd / 256), don't use",
            heap_number, total_mark_list_size, total_ephemeral_size));
        mark_list_overflow = false;
        return 0;
    }

    uint8_t **local_mark_list_index = equalize_mark_lists (total_mark_list_size);

#ifdef USE_VXSORT
    ptrdiff_t item_count = local_mark_list_index - mark_list;
//#define WRITE_SORT_DATA
#if defined(_DEBUG) || defined(WRITE_SORT_DATA)
        // in debug, make a copy of the mark list
        // for checking and debugging purposes
    uint8_t** mark_list_copy = &g_mark_list_copy[heap_number * mark_list_size];
    uint8_t** mark_list_copy_index = &mark_list_copy[item_count];
    for (ptrdiff_t i = 0; i < item_count; i++)
    {
        uint8_t* item = mark_list[i];
        assert ((low <= item) && (item < high));
        mark_list_copy[i] = item;
    }
#endif // _DEBUG || WRITE_SORT_DATA

    do_vxsort (mark_list, item_count, low, high);

#ifdef WRITE_SORT_DATA
    char file_name[256];
    sprintf_s (file_name, ARRAY_SIZE(file_name), "sort_data_gc%d_heap%d", settings.gc_index, heap_number);

    FILE* f;
    errno_t err = fopen_s (&f, file_name, "wb");

    if (err == 0)
    {
        size_t magic = 'SDAT';
        if (fwrite (&magic, sizeof(magic), 1, f) != 1)
            dprintf (3, ("fwrite failed\n"));
        if (fwrite (&elapsed_cycles, sizeof(elapsed_cycles), 1, f) != 1)
            dprintf (3, ("fwrite failed\n"));
        if (fwrite (&low, sizeof(low), 1, f) != 1)
            dprintf (3, ("fwrite failed\n"));
        if (fwrite (&item_count, sizeof(item_count), 1, f) != 1)
            dprintf (3, ("fwrite failed\n"));
        if (fwrite (mark_list_copy, sizeof(mark_list_copy[0]), item_count, f) != item_count)
            dprintf (3, ("fwrite failed\n"));
        if (fwrite (&magic, sizeof(magic), 1, f) != 1)
            dprintf (3, ("fwrite failed\n"));
        if (fclose (f) != 0)
            dprintf (3, ("fclose failed\n"));
    }
#endif

#ifdef _DEBUG
    // in debug, sort the copy as well using the proven sort, so we can check we got the right result
    if (mark_list_copy_index > mark_list_copy)
    {
        introsort::sort (mark_list_copy, mark_list_copy_index - 1, 0);
    }
    for (ptrdiff_t i = 0; i < item_count; i++)
    {
        uint8_t* item = mark_list[i];
        assert (mark_list_copy[i] == item);
    }
#endif //_DEBUG

#else //USE_VXSORT
    dprintf (3, ("Sorting mark lists"));
    if (local_mark_list_index > mark_list)
    {
        introsort::sort (mark_list, local_mark_list_index - 1, 0);
    }
#endif //USE_VXSORT

    uint8_t** x = mark_list;

#ifdef USE_REGIONS
    // first set the pieces for all regions to empty
    assert (g_mark_list_piece_size >= region_count);
    assert (g_mark_list_piece_total_size >= region_count*n_heaps);
    for (size_t region_index = 0; region_index < region_count; region_index++)
    {
        mark_list_piece_start[region_index] = NULL;
        mark_list_piece_end[region_index] = NULL;
    }

    // predicate means: x is still within the mark list, and within the bounds of this region
#define predicate(x) (((x) < local_mark_list_index) && (*(x) < region_limit))

    while (x < local_mark_list_index)
    {
        heap_segment* region = get_region_info_for_address (*x);

        // sanity check - the object on the mark list should be within the region
        assert ((heap_segment_mem (region) <= *x) && (*x < heap_segment_allocated (region)));

        size_t region_index = get_basic_region_index_for_address (heap_segment_mem (region));
        uint8_t* region_limit = heap_segment_allocated (region);

        // Due to GC holes, x can point to something in a region that already got freed. And that region's
        // allocated would be 0 and cause an infinite loop which is much harder to handle on production than
        // simply throwing an exception.
        if (region_limit == 0)
        {
            FATAL_GC_ERROR();
        }

        uint8_t*** mark_list_piece_start_ptr = &mark_list_piece_start[region_index];
        uint8_t*** mark_list_piece_end_ptr = &mark_list_piece_end[region_index];
#else // USE_REGIONS

// predicate means: x is still within the mark list, and within the bounds of this heap
#define predicate(x) (((x) < local_mark_list_index) && (*(x) < heap->ephemeral_high))

    // first set the pieces for all heaps to empty
    int heap_num;
    for (heap_num = 0; heap_num < n_heaps; heap_num++)
    {
        mark_list_piece_start[heap_num] = NULL;
        mark_list_piece_end[heap_num] = NULL;
    }

    heap_num = -1;
    while (x < local_mark_list_index)
    {
        gc_heap* heap;
        // find the heap x points into - searching cyclically from the last heap,
        // because in many cases the right heap is the next one or comes soon after
#ifdef _DEBUG
        int last_heap_num = heap_num;
#endif //_DEBUG
        do
        {
            heap_num++;
            if (heap_num >= n_heaps)
                heap_num = 0;
            assert(heap_num != last_heap_num); // we should always find the heap - infinite loop if not!
            heap = g_heaps[heap_num];
        }
        while (!(*x >= heap->ephemeral_low && *x < heap->ephemeral_high));

        uint8_t*** mark_list_piece_start_ptr = &mark_list_piece_start[heap_num];
        uint8_t*** mark_list_piece_end_ptr = &mark_list_piece_end[heap_num];
#endif // USE_REGIONS

        // x is the start of the mark list piece for this heap/region
        *mark_list_piece_start_ptr = x;

        // to find the end of the mark list piece for this heap/region, find the first x
        // that has !predicate(x), i.e. that is either not in this heap, or beyond the end of the list
        if (predicate(x))
        {
            // let's see if we get lucky and the whole rest belongs to this piece
            if (predicate(local_mark_list_index -1))
            {
                x = local_mark_list_index;
                *mark_list_piece_end_ptr = x;
                break;
            }

            // we play a variant of binary search to find the point sooner.
            // the first loop advances by increasing steps until the predicate turns false.
            // then we retreat the last step, and the second loop advances by decreasing steps, keeping the predicate true.
            unsigned inc = 1;
            do
            {
                inc *= 2;
                uint8_t** temp_x = x;
                x += inc;
                if (temp_x > x)
                {
                    break;
                }
            }
            while (predicate(x));
            // we know that only the last step was wrong, so we undo it
            x -= inc;
            do
            {
                // loop invariant - predicate holds at x, but not x + inc
                assert (predicate(x) && !(((x + inc) > x) && predicate(x + inc)));
                inc /= 2;
                if (((x + inc) > x) && predicate(x + inc))
                {
                    x += inc;
                }
            }
            while (inc > 1);
            // the termination condition and the loop invariant together imply this:
            assert(predicate(x) && !predicate(x + inc) && (inc == 1));
            // so the spot we're looking for is one further
            x += 1;
        }
        *mark_list_piece_end_ptr = x;
    }

#undef predicate

    return total_mark_list_size;
}

void gc_heap::append_to_mark_list (uint8_t **start, uint8_t **end)
{
    size_t slots_needed = end - start;
    size_t slots_available = mark_list_end + 1 - mark_list_index;
    size_t slots_to_copy = min(slots_needed, slots_available);
    memcpy(mark_list_index, start, slots_to_copy*sizeof(*start));
    mark_list_index += slots_to_copy;
    dprintf (3, ("h%d: appended %zd slots to mark_list\n", heap_number, slots_to_copy));
}

#ifdef _DEBUG
static int __cdecl cmp_mark_list_item (const void* vkey, const void* vdatum)
{
    uint8_t** key = (uint8_t**)vkey;
    uint8_t** datum = (uint8_t**)vdatum;
    if (*key < *datum)
        return -1;
    else if (*key > *datum)
        return 1;
    else
        return 0;
}

#endif //_DEBUG

#ifdef USE_REGIONS
uint8_t** gc_heap::get_region_mark_list (BOOL& use_mark_list, uint8_t* start, uint8_t* end, uint8_t*** mark_list_end_ptr)
{
    size_t region_number = get_basic_region_index_for_address (start);
    size_t source_number = region_number;
#else //USE_REGIONS
void gc_heap::merge_mark_lists (size_t total_mark_list_size)
{
    // in case of mark list overflow, don't bother
    if (total_mark_list_size == 0)
    {
        return;
    }

#ifdef _DEBUG
    // if we had more than the average number of mark list items,
    // make sure these got copied to another heap, i.e. didn't get lost
    size_t this_mark_list_size = target_mark_count_for_heap (total_mark_list_size, n_heaps, heap_number);
    for (uint8_t** p = mark_list + this_mark_list_size; p < mark_list_index; p++)
    {
        uint8_t* item = *p;
        uint8_t** found_slot = nullptr;
        for (int i = 0; i < n_heaps; i++)
        {
            uint8_t** heap_mark_list = &g_mark_list[i * mark_list_size];
            size_t heap_mark_list_size = target_mark_count_for_heap (total_mark_list_size, n_heaps, i);
            found_slot = (uint8_t**)bsearch (&item, heap_mark_list, heap_mark_list_size, sizeof(item), cmp_mark_list_item);
            if (found_slot != nullptr)
                break;
        }
        assert ((found_slot != nullptr) && (*found_slot == item));
    }
#endif

    dprintf(3, ("merge_mark_lists: heap_number = %d  starts out with %zd entries",
        heap_number, (mark_list_index - mark_list)));

    int source_number = (size_t)heap_number;
#endif //USE_REGIONS

    uint8_t** source[MAX_SUPPORTED_HEAPS];
    uint8_t** source_end[MAX_SUPPORTED_HEAPS];
    int source_heap[MAX_SUPPORTED_HEAPS];
    int source_count = 0;

    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* heap = g_heaps[i];
        if (heap->mark_list_piece_start[source_number] < heap->mark_list_piece_end[source_number])
        {
            source[source_count] = heap->mark_list_piece_start[source_number];
            source_end[source_count] = heap->mark_list_piece_end[source_number];
            source_heap[source_count] = i;
            if (source_count < MAX_SUPPORTED_HEAPS)
                source_count++;
        }
    }

    dprintf(3, ("source_number = %zd  has %d sources\n", (size_t)source_number, source_count));

#if defined(_DEBUG) || defined(TRACE_GC)
    for (int j = 0; j < source_count; j++)
    {
        dprintf(3, ("source_number = %zd  ", (size_t)source_number));
        dprintf(3, (" source from heap %zd = %zx .. %zx (%zd entries)",
            (size_t)(source_heap[j]), (size_t)(source[j][0]),
            (size_t)(source_end[j][-1]), (size_t)(source_end[j] - source[j])));
       // the sources should all be sorted
        for (uint8_t **x = source[j]; x < source_end[j] - 1; x++)
        {
            if (x[0] > x[1])
            {
                dprintf(3, ("oops, mark_list from source %d for heap %zd isn't sorted\n", j,  (size_t)source_number));
                assert (0);
            }
        }
    }
#endif //_DEBUG || TRACE_GC

    mark_list = &g_mark_list_copy [heap_number*mark_list_size];
    mark_list_index = mark_list;
    mark_list_end = &mark_list [mark_list_size-1];
    int piece_count = 0;
    if (source_count == 0)
    {
        ; // nothing to do
    }
    else if (source_count == 1)
    {
        mark_list = source[0];
        mark_list_index = source_end[0];
        mark_list_end = mark_list_index;
        piece_count++;
    }
    else
    {
        while (source_count > 1)
        {
            // find the lowest and second lowest value in the sources we're merging from
            int lowest_source = 0;
            uint8_t *lowest = *source[0];
            uint8_t *second_lowest = *source[1];
            for (int i = 1; i < source_count; i++)
            {
                if (lowest > *source[i])
                {
                    second_lowest = lowest;
                    lowest = *source[i];
                    lowest_source = i;
                }
                else if (second_lowest > *source[i])
                {
                    second_lowest = *source[i];
                }
            }

            // find the point in the lowest source where it either runs out or is not <= second_lowest anymore
            // let's first try to get lucky and see if the whole source is <= second_lowest -- this is actually quite common
            uint8_t **x;
            if (source_end[lowest_source][-1] <= second_lowest)
                x = source_end[lowest_source];
            else
            {
                // use linear search to find the end -- could also use binary search as in sort_mark_list,
                // but saw no improvement doing that
                for (x = source[lowest_source]; x < source_end[lowest_source] && *x <= second_lowest; x++)
                    ;
            }

            // blast this piece to the mark list
            append_to_mark_list(source[lowest_source], x);
#ifdef USE_REGIONS
            if (mark_list_index > mark_list_end)
            {
                use_mark_list = false;
                return nullptr;
            }
#endif //USE_REGIONS
            piece_count++;

            source[lowest_source] = x;

            // check whether this source is now exhausted
            if (x >= source_end[lowest_source])
            {
                // if it's not the source with the highest index, copy the source with the highest index
                // over it so the non-empty sources are always at the beginning
                if (lowest_source < source_count-1)
                {
                    source[lowest_source] = source[source_count-1];
                    source_end[lowest_source] = source_end[source_count-1];
                }
                source_count--;
            }
        }
        // we're left with just one source that we copy
        append_to_mark_list(source[0], source_end[0]);
#ifdef USE_REGIONS
        if (mark_list_index > mark_list_end)
        {
            use_mark_list = false;
            return nullptr;
        }
#endif //USE_REGIONS
        piece_count++;
    }

#if defined(_DEBUG) || defined(TRACE_GC)
    // the final mark list must be sorted
    for (uint8_t **x = mark_list; x < mark_list_index - 1; x++)
    {
        if (x[0] > x[1])
        {
            dprintf(3, ("oops, mark_list for heap %d isn't sorted at the end of merge_mark_lists", heap_number));
            assert (0);
        }
    }
#endif //_DEBUG || TRACE_GC

#ifdef USE_REGIONS
    *mark_list_end_ptr = mark_list_index;
    return mark_list;
#endif // USE_REGIONS
}

#else //MULTIPLE_HEAPS
#ifdef USE_REGIONS
// a variant of binary search that doesn't look for an exact match,
// but finds the first element >= e
static uint8_t** binary_search (uint8_t** left, uint8_t** right, uint8_t* e)
{
    if (left == right)
        return left;
    assert (left < right);
    uint8_t** a = left;
    size_t l = 0;
    size_t r = (size_t)(right - left);
    while ((r - l) >= 2)
    {
        size_t m = l + (r - l) / 2;

        // loop condition says that r - l is at least 2
        // so l, m, r are all different
        assert ((l < m) && (m < r));

        if (a[m] < e)
        {
            l = m;
        }
        else
        {
            r = m;
        }
    }
    if (a[l] < e)
        return a + l + 1;
    else
        return a + l;
}

uint8_t** gc_heap::get_region_mark_list (BOOL& use_mark_list, uint8_t* start, uint8_t* end, uint8_t*** mark_list_end_ptr)
{
    // do a binary search over the sorted marked list to find start and end of the
    // mark list for this region
    *mark_list_end_ptr = binary_search (mark_list, mark_list_index, end);
    return binary_search (mark_list, *mark_list_end_ptr, start);
}

#endif //USE_REGIONS
#endif //MULTIPLE_HEAPS

void gc_heap::grow_mark_list ()
{
    // with vectorized sorting, we can use bigger mark lists
    bool use_big_lists = false;
#if defined(USE_VXSORT) && defined(TARGET_AMD64)
    use_big_lists = IsSupportedInstructionSet (InstructionSet::AVX2);
#elif defined(USE_VXSORT) && defined(TARGET_ARM64)
    use_big_lists = IsSupportedInstructionSet (InstructionSet::NEON);
#endif //USE_VXSORT

#ifdef MULTIPLE_HEAPS
    const size_t MAX_MARK_LIST_SIZE = use_big_lists ? (1000 * 1024) : (200 * 1024);
#else //MULTIPLE_HEAPS
    const size_t MAX_MARK_LIST_SIZE = use_big_lists ? (32 * 1024) : (16 * 1024);
#endif //MULTIPLE_HEAPS

    size_t new_mark_list_size = min (mark_list_size * 2, MAX_MARK_LIST_SIZE);
    size_t new_mark_list_total_size = new_mark_list_size*n_heaps;
    if (new_mark_list_total_size == g_mark_list_total_size)
        return;

#ifdef MULTIPLE_HEAPS
    uint8_t** new_mark_list = make_mark_list (new_mark_list_total_size);
    uint8_t** new_mark_list_copy = make_mark_list (new_mark_list_total_size);

    if ((new_mark_list != nullptr) && (new_mark_list_copy != nullptr))
    {
        delete[] g_mark_list;
        g_mark_list = new_mark_list;
        delete[] g_mark_list_copy;
        g_mark_list_copy = new_mark_list_copy;
        mark_list_size = new_mark_list_size;
        g_mark_list_total_size = new_mark_list_total_size;
    }
    else
    {
        delete[] new_mark_list;
        delete[] new_mark_list_copy;
    }

#else //MULTIPLE_HEAPS
    uint8_t** new_mark_list = make_mark_list (new_mark_list_size);
    if (new_mark_list != nullptr)
    {
        delete[] mark_list;
        g_mark_list = new_mark_list;
        mark_list_size = new_mark_list_size;
        g_mark_list_total_size = new_mark_list_size;
    }
#endif //MULTIPLE_HEAPS
}

#ifdef BACKGROUND_GC
#ifdef FEATURE_BASICFREEZE
inline
void gc_heap::seg_clear_mark_array_bits_soh (heap_segment* seg)
{
    uint8_t* range_beg = 0;
    uint8_t* range_end = 0;
    if (bgc_mark_array_range (seg, FALSE, &range_beg, &range_end))
    {
        clear_mark_array (range_beg, align_on_mark_word (range_end));
    }
}

inline
void gc_heap::seg_set_mark_array_bits_soh (heap_segment* seg)
{
    uint8_t* range_beg = 0;
    uint8_t* range_end = 0;
    if (bgc_mark_array_range (seg, FALSE, &range_beg, &range_end))
    {
        size_t beg_word = mark_word_of (align_on_mark_word (range_beg));
        size_t end_word = mark_word_of (align_on_mark_word (range_end));

        uint8_t* op = range_beg;
        while (op < mark_word_address (beg_word))
        {
            mark_array_set_marked (op);
            op += mark_bit_pitch;
        }

        memset (&mark_array[beg_word], 0xFF, (end_word - beg_word)*sizeof (uint32_t));
    }
}

#endif //FEATURE_BASICFREEZE

void gc_heap::bgc_clear_batch_mark_array_bits (uint8_t* start, uint8_t* end)
{
    if ((start < background_saved_highest_address) &&
        (end > background_saved_lowest_address))
    {
        start = max (start, background_saved_lowest_address);
        end = min (end, background_saved_highest_address);

        size_t start_mark_bit = mark_bit_of (start);
        size_t end_mark_bit = mark_bit_of (end);
        unsigned int startbit = mark_bit_bit (start_mark_bit);
        unsigned int endbit = mark_bit_bit (end_mark_bit);
        size_t startwrd = mark_bit_word (start_mark_bit);
        size_t endwrd = mark_bit_word (end_mark_bit);

        dprintf (3, ("Clearing all mark array bits between [%zx:%zx-[%zx:%zx",
            (size_t)start, (size_t)start_mark_bit,
            (size_t)end, (size_t)end_mark_bit));

        unsigned int firstwrd = lowbits (~0, startbit);
        unsigned int lastwrd = highbits (~0, endbit);

        if (startwrd == endwrd)
        {
            if (startbit != endbit)
            {
                unsigned int wrd = firstwrd | lastwrd;
                mark_array[startwrd] &= wrd;
            }
            else
            {
                assert (start == end);
            }
            return;
        }

        // clear the first mark word.
        if (startbit)
        {
            mark_array[startwrd] &= firstwrd;
            startwrd++;
        }

        for (size_t wrdtmp = startwrd; wrdtmp < endwrd; wrdtmp++)
        {
            mark_array[wrdtmp] = 0;
        }

        // clear the last mark word.
        if (endbit)
        {
            mark_array[endwrd] &= lastwrd;
        }
    }
}

#endif //BACKGROUND_GC

inline
BOOL gc_heap::is_mark_set (uint8_t* o)
{
    return marked (o);
}

inline
size_t gc_heap::get_promoted_bytes()
{
#ifdef USE_REGIONS
    if (!survived_per_region)
    {
        dprintf (REGIONS_LOG, ("no space to store promoted bytes"));
        return 0;
    }

    dprintf (3, ("h%d getting surv", heap_number));
    size_t promoted = 0;
    for (size_t i = 0; i < region_count; i++)
    {
        if (survived_per_region[i] > 0)
        {
            heap_segment* region = get_region_at_index (i);
            dprintf (REGIONS_LOG, ("h%d region[%zd] %p(g%d)(%s) surv: %zd(%p)",
                heap_number, i,
                heap_segment_mem (region),
                heap_segment_gen_num (region),
                (heap_segment_loh_p (region) ? "LOH" : (heap_segment_poh_p (region) ? "POH" :"SOH")),
                survived_per_region[i],
                &survived_per_region[i]));

            promoted += survived_per_region[i];
        }
    }

#ifdef _DEBUG
    dprintf (REGIONS_LOG, ("h%d global recorded %zd, regions recorded %zd",
        heap_number, promoted_bytes (heap_number), promoted));
    assert (promoted_bytes (heap_number) == promoted);
#endif //_DEBUG

    return promoted;

#else //USE_REGIONS

#ifdef MULTIPLE_HEAPS
    return g_promoted [heap_number*16];
#else //MULTIPLE_HEAPS
    return g_promoted;
#endif //MULTIPLE_HEAPS
#endif //USE_REGIONS
}

#ifdef USE_REGIONS
void gc_heap::sync_promoted_bytes()
{
    int condemned_gen_number = settings.condemned_generation;
    int highest_gen_number = ((condemned_gen_number == max_generation) ?
                              (total_generation_count - 1) : settings.condemned_generation);
    int stop_gen_idx = get_stop_generation_index (condemned_gen_number);

#ifdef MULTIPLE_HEAPS
// We gather all the promoted bytes for a region recorded by all threads into that region's survived
// for plan phase. sore_mark_list will be called shortly and will start using the same storage that
// the GC threads used to record promoted bytes.
    for (int i = 0; i < n_heaps; i++)
    {
        gc_heap* hp = g_heaps[i];

#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        for (int gen_idx = highest_gen_number; gen_idx >= stop_gen_idx; gen_idx--)
        {
            generation* condemned_gen = hp->generation_of (gen_idx);
            heap_segment* current_region = heap_segment_rw (generation_start_segment (condemned_gen));

            while (current_region)
            {
                size_t region_index = get_basic_region_index_for_address (heap_segment_mem (current_region));

#ifdef MULTIPLE_HEAPS
                size_t total_surv = 0;
                size_t total_old_card_surv = 0;

                for (int hp_idx = 0; hp_idx < n_heaps; hp_idx++)
                {
                    total_surv += g_heaps[hp_idx]->survived_per_region[region_index];
                    total_old_card_surv += g_heaps[hp_idx]->old_card_survived_per_region[region_index];
                }

                heap_segment_survived (current_region) = total_surv;
                heap_segment_old_card_survived (current_region) = (int)total_old_card_surv;
#else
                heap_segment_survived (current_region) = survived_per_region[region_index];
                heap_segment_old_card_survived (current_region) =
                    (int)(old_card_survived_per_region[region_index]);
#endif //MULTIPLE_HEAPS

                dprintf (REGIONS_LOG, ("region #%zd %p surv %zd, old card surv %d",
                    region_index,
                    heap_segment_mem (current_region),
                    heap_segment_survived (current_region),
                    heap_segment_old_card_survived (current_region)));

                current_region = heap_segment_next (current_region);
            }
        }
    }
}

void gc_heap::equalize_promoted_bytes(int condemned_gen_number)
{
#ifdef MULTIPLE_HEAPS
    // algorithm to roughly balance promoted bytes across heaps by moving regions between heaps
    // goal is just to balance roughly, while keeping computational complexity low
    // hope is to achieve better work balancing in relocate and compact phases
    // this is also used when the heap count changes to balance regions between heaps
    int highest_gen_number = ((condemned_gen_number == max_generation) ?
        (total_generation_count - 1) : condemned_gen_number);
    int stop_gen_idx = get_stop_generation_index (condemned_gen_number);

    for (int gen_idx = highest_gen_number; gen_idx >= stop_gen_idx; gen_idx--)
    {
        // step 1:
        //  compute total promoted bytes per gen
        size_t total_surv = 0;
        size_t max_surv_per_heap = 0;
        size_t surv_per_heap[MAX_SUPPORTED_HEAPS];
        for (int i = 0; i < n_heaps; i++)
        {
            surv_per_heap[i] = 0;

            gc_heap* hp = g_heaps[i];

            generation* condemned_gen = hp->generation_of (gen_idx);
            heap_segment* current_region = heap_segment_rw (generation_start_segment (condemned_gen));

            while (current_region)
            {
                total_surv += heap_segment_survived (current_region);
                surv_per_heap[i] += heap_segment_survived (current_region);
                current_region = heap_segment_next (current_region);
            }

            max_surv_per_heap = max (max_surv_per_heap, surv_per_heap[i]);

            dprintf (REGIONS_LOG, ("gen: %d heap %d surv: %zd", gen_idx, i, surv_per_heap[i]));
        }
        // compute average promoted bytes per heap and per gen
        // be careful to round up
        size_t avg_surv_per_heap = (total_surv + n_heaps - 1) / n_heaps;

        if (avg_surv_per_heap != 0)
        {
            dprintf (REGIONS_LOG, ("before equalize: gen: %d avg surv: %zd max_surv: %zd imbalance: %zd", gen_idx, avg_surv_per_heap, max_surv_per_heap, max_surv_per_heap*100/avg_surv_per_heap));
        }
        //
        // step 2:
        //   remove regions from surplus heaps until all heaps are <= average
        //   put removed regions into surplus regions
        //
        // step 3:
        //   put regions into size classes by survivorship
        //   put deficit heaps into size classes by deficit
        //
        // step 4:
        //   while (surplus regions is non-empty)
        //     get surplus region from biggest size class
        //     put it into heap from biggest deficit size class
        //     re-insert heap by resulting deficit size class

        heap_segment* surplus_regions = nullptr;
        size_t max_deficit = 0;
        size_t max_survived = 0;

        //  go through all the heaps
        for (int i = 0; i < n_heaps; i++)
        {
            // remove regions from this heap until it has average or less survivorship
            while (surv_per_heap[i] > avg_surv_per_heap)
            {
                heap_segment* region = g_heaps[i]->unlink_first_rw_region (gen_idx);
                if (region == nullptr)
                {
                    break;
                }
                assert (surv_per_heap[i] >= heap_segment_survived (region));
                dprintf (REGIONS_LOG, ("heap: %d surv: %zd - %zd = %zd",
                    i,
                    surv_per_heap[i],
                    heap_segment_survived (region),
                    surv_per_heap[i] - heap_segment_survived (region)));

                surv_per_heap[i] -= heap_segment_survived (region);

                heap_segment_next (region) = surplus_regions;
                surplus_regions = region;

                max_survived = max (max_survived, heap_segment_survived (region));
            }
            if (surv_per_heap[i] < avg_surv_per_heap)
            {
                size_t deficit = avg_surv_per_heap - surv_per_heap[i];
                max_deficit = max (max_deficit, deficit);
            }
        }

        // give heaps without regions a region from the surplus_regions,
        // if none are available, steal a region from another heap
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
            generation* gen = hp->generation_of (gen_idx);
            if (heap_segment_rw (generation_start_segment (gen)) == nullptr)
            {
                heap_segment* start_region = surplus_regions;
                if (start_region != nullptr)
                {
                    surplus_regions = heap_segment_next (start_region);
                }
                else
                {
                    for (int j = 0; j < n_heaps; j++)
                    {
                        start_region = g_heaps[j]->unlink_first_rw_region (gen_idx);
                        if (start_region != nullptr)
                        {
                            surv_per_heap[j] -= heap_segment_survived (start_region);
                            size_t deficit = avg_surv_per_heap - surv_per_heap[j];
                            max_deficit = max (max_deficit, deficit);
                            break;
                        }
                    }
                }
                assert (start_region);
                dprintf (3, ("making sure heap %d gen %d has at least one region by adding region %zx", start_region));
                heap_segment_next (start_region) = nullptr;

                assert (heap_segment_heap (start_region) == nullptr && hp != nullptr);
                int oh = heap_segment_oh (start_region);
                size_t committed = heap_segment_committed (start_region) - get_region_start (start_region);
                dprintf(3, ("commit-accounting:  from temp to %d [%p, %p) for heap %d", oh, get_region_start (start_region), heap_segment_committed (start_region), hp->heap_number));
#ifdef _DEBUG
                g_heaps[hp->heap_number]->committed_by_oh_per_heap[oh] += committed;
#endif //_DEBUG
                set_heap_for_contained_basic_regions (start_region, hp);
                max_survived = max (max_survived, heap_segment_survived (start_region));
                hp->thread_start_region (gen, start_region);
                surv_per_heap[i] += heap_segment_survived (start_region);
            }
        }

        // we arrange both surplus regions and deficit heaps by size classes
        const int NUM_SIZE_CLASSES = 16;
        heap_segment* surplus_regions_by_size_class[NUM_SIZE_CLASSES];
        memset (surplus_regions_by_size_class, 0, sizeof(surplus_regions_by_size_class));
        double survived_scale_factor = ((double)NUM_SIZE_CLASSES) / (max_survived + 1);

        heap_segment* next_region;
        for (heap_segment* region = surplus_regions; region != nullptr; region = next_region)
        {
            size_t size_class = (size_t)(heap_segment_survived (region)*survived_scale_factor);
            assert ((0 <= size_class) && (size_class < NUM_SIZE_CLASSES));
            next_region = heap_segment_next (region);
            heap_segment_next (region) = surplus_regions_by_size_class[size_class];
            surplus_regions_by_size_class[size_class] = region;
        }

        int next_heap_in_size_class[MAX_SUPPORTED_HEAPS];
        int heaps_by_deficit_size_class[NUM_SIZE_CLASSES];
        for (int i = 0; i < NUM_SIZE_CLASSES; i++)
        {
            heaps_by_deficit_size_class[i] = -1;
        }
        double deficit_scale_factor = ((double)NUM_SIZE_CLASSES) / (max_deficit + 1);

        for (int i = 0; i < n_heaps; i++)
        {
            if (avg_surv_per_heap > surv_per_heap[i])
            {
                size_t deficit = avg_surv_per_heap - surv_per_heap[i];
                int size_class = (int)(deficit*deficit_scale_factor);
                assert ((0 <= size_class) && (size_class < NUM_SIZE_CLASSES));
                next_heap_in_size_class[i] = heaps_by_deficit_size_class[size_class];
                heaps_by_deficit_size_class[size_class] = i;
            }
        }

        int region_size_class = NUM_SIZE_CLASSES - 1;
        int heap_size_class = NUM_SIZE_CLASSES - 1;
        while (region_size_class >= 0)
        {
            // obtain a region from the biggest size class
            heap_segment* region = surplus_regions_by_size_class[region_size_class];
            if (region == nullptr)
            {
                region_size_class--;
                continue;
            }
            // and a heap from the biggest deficit size class
            int heap_num;
            while (true)
            {
                if (heap_size_class < 0)
                {
                    // put any remaining regions on heap 0
                    // rare case, but there may be some 0 surv size regions
                    heap_num = 0;
                    break;
                }
                heap_num = heaps_by_deficit_size_class[heap_size_class];
                if (heap_num >= 0)
                {
                    break;
                }
                heap_size_class--;
            }

            // now move the region to the heap
            surplus_regions_by_size_class[region_size_class] = heap_segment_next (region);
            g_heaps[heap_num]->thread_rw_region_front (gen_idx, region);

            // adjust survival for this heap
            dprintf (REGIONS_LOG, ("heap: %d surv: %zd + %zd = %zd",
                heap_num,
                surv_per_heap[heap_num],
                heap_segment_survived (region),
                surv_per_heap[heap_num] + heap_segment_survived (region)));

            surv_per_heap[heap_num] += heap_segment_survived (region);

            if (heap_size_class < 0)
            {
                // no need to update size classes for heaps -
                // just work down the remaining regions, if any
                continue;
            }

            // is this heap now average or above?
            if (surv_per_heap[heap_num] >= avg_surv_per_heap)
            {
                // if so, unlink from the current size class
                heaps_by_deficit_size_class[heap_size_class] = next_heap_in_size_class[heap_num];
                continue;
            }

            // otherwise compute the updated deficit
            size_t new_deficit = avg_surv_per_heap - surv_per_heap[heap_num];

            // check if this heap moves to a differenct deficit size class
            int new_heap_size_class = (int)(new_deficit*deficit_scale_factor);
            if (new_heap_size_class != heap_size_class)
            {
                // the new deficit size class should be smaller and in range
                assert (new_heap_size_class < heap_size_class);
                assert ((0 <= new_heap_size_class) && (new_heap_size_class < NUM_SIZE_CLASSES));

                // if so, unlink from the current size class
                heaps_by_deficit_size_class[heap_size_class] = next_heap_in_size_class[heap_num];

                // and link to the new size class
                next_heap_in_size_class[heap_num] = heaps_by_deficit_size_class[new_heap_size_class];
                heaps_by_deficit_size_class[new_heap_size_class] = heap_num;
            }
        }
        // we will generally be left with some heaps with deficits here, but that's ok

        // check we didn't screw up the data structures
        for (int i = 0; i < n_heaps; i++)
        {
            gc_heap* hp = g_heaps[i];
            hp->verify_regions (gen_idx, true, true);
        }
#ifdef TRACE_GC
        max_surv_per_heap = 0;
        for (int i = 0; i < n_heaps; i++)
        {
            max_surv_per_heap = max (max_surv_per_heap, surv_per_heap[i]);
        }
        if (avg_surv_per_heap != 0)
        {
            dprintf (REGIONS_LOG, ("after equalize: gen: %d avg surv: %zd max_surv: %zd imbalance: %zd", gen_idx, avg_surv_per_heap, max_surv_per_heap, max_surv_per_heap*100/avg_surv_per_heap));
        }
#endif // TRACE_GC
    }
#endif //MULTIPLE_HEAPS
}

#endif //USE_REGIONS
#if !defined(USE_REGIONS) || defined(_DEBUG)
inline
void gc_heap::init_promoted_bytes()
{
#ifdef MULTIPLE_HEAPS
    g_promoted [heap_number*16] = 0;
#else //MULTIPLE_HEAPS
    g_promoted = 0;
#endif //MULTIPLE_HEAPS
}

size_t& gc_heap::promoted_bytes (int thread)
{
#ifdef MULTIPLE_HEAPS
    return g_promoted [thread*16];
#else //MULTIPLE_HEAPS
    UNREFERENCED_PARAMETER(thread);
    return g_promoted;
#endif //MULTIPLE_HEAPS
}

#endif

inline
void gc_heap::add_to_promoted_bytes (uint8_t* object, int thread)
{
    size_t obj_size = size (object);
    add_to_promoted_bytes (object, obj_size, thread);
}

inline
void gc_heap::add_to_promoted_bytes (uint8_t* object, size_t obj_size, int thread)
{
    assert (thread == heap_number);

#ifdef USE_REGIONS
    if (survived_per_region)
    {
        survived_per_region[get_basic_region_index_for_address (object)] += obj_size;
    }
#endif //USE_REGIONS

#if !defined(USE_REGIONS) || defined(_DEBUG)
#ifdef MULTIPLE_HEAPS
    g_promoted [heap_number*16] += obj_size;
#else //MULTIPLE_HEAPS
    g_promoted += obj_size;
#endif //MULTIPLE_HEAPS
#endif //!USE_REGIONS || _DEBUG

#ifdef _DEBUG
    // Verify we keep the 2 recordings in sync.
    //get_promoted_bytes();
#endif //_DEBUG
}

inline
BOOL gc_heap::gc_mark1 (uint8_t* o)
{
    BOOL marked = !marked (o);
    set_marked (o);
    dprintf (3, ("*%zx*, newly marked: %d", (size_t)o, marked));
#if defined(USE_REGIONS) && defined(_DEBUG)
    heap_segment* seg = seg_mapping_table_segment_of (o);
    if (o > heap_segment_allocated (seg))
    {
        dprintf (REGIONS_LOG, ("%p is in seg %zx(%p) but beyond alloc %p!!",
            o, (size_t)seg, heap_segment_mem (seg), heap_segment_allocated (seg)));
        GCToOSInterface::DebugBreak();
    }
#endif //USE_REGIONS && _DEBUG
    return marked;
}

#ifdef USE_REGIONS
inline bool gc_heap::is_in_gc_range (uint8_t* o)
{
#ifdef FEATURE_BASICFREEZE
    // we may have frozen objects in read only segments
    // outside of the reserved address range of the gc heap
    assert (((g_gc_lowest_address <= o) && (o < g_gc_highest_address)) ||
        (o == nullptr) || (ro_segment_lookup (o) != nullptr));
#else //FEATURE_BASICFREEZE
    // without frozen objects, every non-null pointer must be
    // within the heap
    assert ((o == nullptr) || (g_gc_lowest_address <= o) && (o < g_gc_highest_address));
#endif //FEATURE_BASICFREEZE
    return ((gc_low <= o) && (o < gc_high));
}

#endif //USE_REGIONS

inline
BOOL gc_heap::gc_mark (uint8_t* o, uint8_t* low, uint8_t* high, int condemned_gen)
{
#ifdef USE_REGIONS
    if ((o >= low) && (o < high))
    {
        if (condemned_gen != max_generation && get_region_gen_num (o) > condemned_gen)
        {
            return FALSE;
        }
        BOOL already_marked = marked (o);
        if (already_marked)
        {
            return FALSE;
        }
        set_marked (o);
        return TRUE;
    }
    return FALSE;
#else //USE_REGIONS
    assert (condemned_gen == -1);

    BOOL marked = FALSE;
    if ((o >= low) && (o < high))
        marked = gc_mark1 (o);
#ifdef MULTIPLE_HEAPS
    else if (o)
    {
        gc_heap* hp = heap_of_gc (o);
        assert (hp);
        if ((o >= hp->gc_low) && (o < hp->gc_high))
            marked = gc_mark1 (o);
    }
#ifdef SNOOP_STATS
    snoop_stat.objects_checked_count++;

    if (marked)
    {
        snoop_stat.objects_marked_count++;
    }
    if (!o)
    {
        snoop_stat.zero_ref_count++;
    }

#endif //SNOOP_STATS
#endif //MULTIPLE_HEAPS
    return marked;
#endif //USE_REGIONS
}

// This starts a plug. But mark_stack_tos isn't increased until set_pinned_info is called.
void gc_heap::enque_pinned_plug (uint8_t* plug,
                                 BOOL save_pre_plug_info_p,
                                 uint8_t* last_object_in_last_plug)
{
    if (mark_stack_array_length <= mark_stack_tos)
    {
        if (!grow_mark_stack (mark_stack_array, mark_stack_array_length, MARK_STACK_INITIAL_LENGTH))
        {
            // we don't want to continue here due to security
            // risks. This happens very rarely and fixing it in the
            // way so that we can continue is a bit involved and will
            // not be done in Dev10.
            GCToEEInterface::HandleFatalError((unsigned int)CORINFO_EXCEPTION_GC);
        }
    }

    dprintf (3, ("enqueuing P #%zd(%p): %p. oldest: %zd, LO: %p, pre: %d",
        mark_stack_tos, &mark_stack_array[mark_stack_tos], plug, mark_stack_bos, last_object_in_last_plug, (save_pre_plug_info_p ? 1 : 0)));
    mark& m = mark_stack_array[mark_stack_tos];
    m.first = plug;
    // Must be set now because if we have a short object we'll need the value of saved_pre_p.
    m.saved_pre_p = save_pre_plug_info_p;

    if (save_pre_plug_info_p)
    {
        // In the case of short plugs or doubly linked free lists, there may be extra bits
        // set in the method table pointer.
        // Clear these bits for the copy saved in saved_pre_plug, but not for the copy
        // saved in saved_pre_plug_reloc.
        // This is because we need these bits for compaction, but not for mark & sweep.
        size_t special_bits = clear_special_bits (last_object_in_last_plug);
        // now copy the bits over
        memcpy (&(m.saved_pre_plug), &(((plug_and_gap*)plug)[-1]), sizeof (gap_reloc_pair));
        // restore the bits in the original
        set_special_bits (last_object_in_last_plug, special_bits);

        memcpy (&(m.saved_pre_plug_reloc), &(((plug_and_gap*)plug)[-1]), sizeof (gap_reloc_pair));

        // If the last object in the last plug is too short, it requires special handling.
        size_t last_obj_size = plug - last_object_in_last_plug;
        if (last_obj_size < min_pre_pin_obj_size)
        {
            record_interesting_data_point (idp_pre_short);
#ifdef SHORT_PLUGS
            if (is_plug_padded (last_object_in_last_plug))
                record_interesting_data_point (idp_pre_short_padded);
#endif //SHORT_PLUGS
            dprintf (3, ("encountered a short object %p right before pinned plug %p!",
                         last_object_in_last_plug, plug));
            // Need to set the short bit regardless of having refs or not because we need to
            // indicate that this object is not walkable.
            m.set_pre_short();

#ifdef COLLECTIBLE_CLASS
            if (is_collectible (last_object_in_last_plug))
            {
                m.set_pre_short_collectible();
            }
#endif //COLLECTIBLE_CLASS

            if (contain_pointers (last_object_in_last_plug))
            {
                dprintf (3, ("short object: %p(%zx)", last_object_in_last_plug, last_obj_size));

                go_through_object_nostart (method_table(last_object_in_last_plug), last_object_in_last_plug, last_obj_size, pval,
                    {
                        size_t gap_offset = (((size_t)pval - (size_t)(plug - sizeof (gap_reloc_pair) - plug_skew))) / sizeof (uint8_t*);
                        dprintf (3, ("member: %p->%p, %zd ptrs from beginning of gap", (uint8_t*)pval, *pval, gap_offset));
                        m.set_pre_short_bit (gap_offset);
                    }
                );
            }
        }
    }

    m.saved_post_p = FALSE;
}

void gc_heap::save_post_plug_info (uint8_t* last_pinned_plug, uint8_t* last_object_in_last_plug, uint8_t* post_plug)
{
#ifndef _DEBUG
    UNREFERENCED_PARAMETER(last_pinned_plug);
#endif //_DEBUG

    mark& m = mark_stack_array[mark_stack_tos - 1];
    assert (last_pinned_plug == m.first);
    m.saved_post_plug_info_start = (uint8_t*)&(((plug_and_gap*)post_plug)[-1]);

    // In the case of short plugs or doubly linked free lists, there may be extra bits
    // set in the method table pointer.
    // Clear these bits for the copy saved in saved_post_plug, but not for the copy
    // saved in saved_post_plug_reloc.
    // This is because we need these bits for compaction, but not for mark & sweep.
    // Note that currently none of these bits will ever be set in the object saved *after*
    // a pinned plug - this object is currently pinned along with the pinned object before it
    size_t special_bits = clear_special_bits (last_object_in_last_plug);
    memcpy (&(m.saved_post_plug), m.saved_post_plug_info_start, sizeof (gap_reloc_pair));
    // restore the bits in the original
    set_special_bits (last_object_in_last_plug, special_bits);

    memcpy (&(m.saved_post_plug_reloc), m.saved_post_plug_info_start, sizeof (gap_reloc_pair));

    // This is important - we need to clear all bits here except the last one.
    m.saved_post_p = TRUE;

#ifdef _DEBUG
    m.saved_post_plug_debug.gap = 1;
#endif //_DEBUG

    dprintf (3, ("PP %p has NP %p right after", last_pinned_plug, post_plug));

    size_t last_obj_size = post_plug - last_object_in_last_plug;
    if (last_obj_size < min_pre_pin_obj_size)
    {
        dprintf (3, ("PP %p last obj %p is too short", last_pinned_plug, last_object_in_last_plug));
        record_interesting_data_point (idp_post_short);
#ifdef SHORT_PLUGS
        if (is_plug_padded (last_object_in_last_plug))
            record_interesting_data_point (idp_post_short_padded);
#endif //SHORT_PLUGS
        m.set_post_short();
#if defined (_DEBUG) && defined (VERIFY_HEAP)
        verify_pinned_queue_p = TRUE;
#endif // _DEBUG && VERIFY_HEAP

#ifdef COLLECTIBLE_CLASS
        if (is_collectible (last_object_in_last_plug))
        {
            m.set_post_short_collectible();
        }
#endif //COLLECTIBLE_CLASS

        if (contain_pointers (last_object_in_last_plug))
        {
            dprintf (3, ("short object: %p(%zx)", last_object_in_last_plug, last_obj_size));

            // TODO: since we won't be able to walk this object in relocation, we still need to
            // take care of collectible assemblies here.
            go_through_object_nostart (method_table(last_object_in_last_plug), last_object_in_last_plug, last_obj_size, pval,
                {
                    size_t gap_offset = (((size_t)pval - (size_t)(post_plug - sizeof (gap_reloc_pair) - plug_skew))) / sizeof (uint8_t*);
                    dprintf (3, ("member: %p->%p, %zd ptrs from beginning of gap", (uint8_t*)pval, *pval, gap_offset));
                    m.set_post_short_bit (gap_offset);
                }
            );
        }
    }
}

#ifdef MH_SC_MARK
inline
VOLATILE(uint8_t*)& gc_heap::ref_mark_stack (gc_heap* hp, int index)
{
    return ((VOLATILE(uint8_t*)*)(hp->mark_stack_array))[index];
}

#endif //MH_SC_MARK

inline
uint8_t* ref_from_slot (uint8_t* r)
{
    return (uint8_t*)((size_t)r & ~(stolen | partial));
}

inline
BOOL ref_p (uint8_t* r)
{
    return (straight_ref_p (r) || partial_object_p (r));
}

mark_queue_t::mark_queue_t()
#ifdef MARK_PHASE_PREFETCH
    : curr_slot_index(0)
#endif //MARK_PHASE_PREFETCH
{
#ifdef MARK_PHASE_PREFETCH
    for (size_t i = 0; i < slot_count; i++)
    {
        slot_table[i] = nullptr;
    }
#endif //MARK_PHASE_PREFETCH
}

// place an object in the mark queue
// returns a *different* object or nullptr
// if a non-null object is returned, that object is newly marked
// object o *must* be in a condemned generation
FORCEINLINE
uint8_t *mark_queue_t::queue_mark(uint8_t *o)
{
#ifdef MARK_PHASE_PREFETCH
    Prefetch (o);

    // while the prefetch is taking effect, park our object in the queue
    // and fetch an object that has been sitting in the queue for a while
    // and where (hopefully) the memory is already in the cache
    size_t slot_index = curr_slot_index;
    uint8_t* old_o = slot_table[slot_index];
    slot_table[slot_index] = o;

    curr_slot_index = (slot_index + 1) % slot_count;
    if (old_o == nullptr)
        return nullptr;
#else //MARK_PHASE_PREFETCH
    uint8_t* old_o = o;
#endif //MARK_PHASE_PREFETCH

    // this causes us to access the method table pointer of the old object
    BOOL already_marked = marked (old_o);
    if (already_marked)
    {
        return nullptr;
    }
    set_marked (old_o);
    return old_o;
}

// place an object in the mark queue
// returns a *different* object or nullptr
// if a non-null object is returned, that object is newly marked
// check first whether the object o is indeed in a condemned generation
FORCEINLINE
uint8_t *mark_queue_t::queue_mark(uint8_t *o, int condemned_gen)
{
#ifdef USE_REGIONS
    if (!is_in_heap_range (o))
    {
        return nullptr;
    }
    if ((condemned_gen != max_generation) && (gc_heap::get_region_gen_num (o) > condemned_gen))
    {
        return nullptr;
    }
    return queue_mark(o);
#else //USE_REGIONS
    assert (condemned_gen == -1);

#ifdef MULTIPLE_HEAPS
    if (o)
    {
        gc_heap* hp = gc_heap::heap_of_gc (o);
        assert (hp);
        if ((o >= hp->gc_low) && (o < hp->gc_high))
            return queue_mark (o);
    }
#else //MULTIPLE_HEAPS
    if ((o >= gc_heap::gc_low) && (o < gc_heap::gc_high))
        return queue_mark (o);
#endif //MULTIPLE_HEAPS
    return nullptr;
#endif //USE_REGIONS
}

// retrieve a newly marked object from the queue
// returns nullptr if there is no such object
uint8_t* mark_queue_t::get_next_marked()
{
#ifdef MARK_PHASE_PREFETCH
    size_t slot_index = curr_slot_index;
    size_t empty_slot_count = 0;
    while (empty_slot_count < slot_count)
    {
        uint8_t* o = slot_table[slot_index];
        slot_table[slot_index] = nullptr;
        slot_index = (slot_index + 1) % slot_count;
        if (o != nullptr)
        {
            BOOL already_marked = marked (o);
            if (!already_marked)
            {
                set_marked (o);
                curr_slot_index = slot_index;
                return o;
            }
        }
        empty_slot_count++;
    }
#endif //MARK_PHASE_PREFETCH
    return nullptr;
}

void mark_queue_t::verify_empty()
{
#ifdef MARK_PHASE_PREFETCH
    for (size_t slot_index = 0; slot_index < slot_count; slot_index++)
    {
        assert(slot_table[slot_index] == nullptr);
    }
#endif //MARK_PHASE_PREFETCH
}

void gc_heap::mark_object_simple1 (uint8_t* oo, uint8_t* start THREAD_NUMBER_DCL)
{
    SERVER_SC_MARK_VOLATILE(uint8_t*)* mark_stack_tos = (SERVER_SC_MARK_VOLATILE(uint8_t*)*)mark_stack_array;
    SERVER_SC_MARK_VOLATILE(uint8_t*)* mark_stack_limit = (SERVER_SC_MARK_VOLATILE(uint8_t*)*)&mark_stack_array[mark_stack_array_length];
    SERVER_SC_MARK_VOLATILE(uint8_t*)* mark_stack_base = mark_stack_tos;

    // If we are doing a full GC we don't use mark list anyway so use m_boundary_fullgc that doesn't
    // update mark list.
    BOOL  full_p = (settings.condemned_generation == max_generation);
    int condemned_gen =
#ifdef USE_REGIONS
        settings.condemned_generation;
#else
        -1;
#endif //USE_REGIONS

    assert ((start >= oo) && (start < oo+size(oo)));

#ifndef MH_SC_MARK
    *mark_stack_tos = oo;
#endif //!MH_SC_MARK

    while (1)
    {
#ifdef MULTIPLE_HEAPS
#else  //MULTIPLE_HEAPS
        const int thread = 0;
#endif //MULTIPLE_HEAPS

        if (oo && ((size_t)oo != 4))
        {
            size_t s = 0;
            if (stolen_p (oo))
            {
                --mark_stack_tos;
                goto next_level;
            }
            else if (!partial_p (oo) && ((s = size (oo)) < (partial_size_th*sizeof (uint8_t*))))
            {
                BOOL overflow_p = FALSE;

                if (mark_stack_tos + (s) /sizeof (uint8_t*) >= (mark_stack_limit  - 1))
                {
                    size_t num_components = ((method_table(oo))->HasComponentSize() ? ((CObjectHeader*)oo)->GetNumComponents() : 0);
                    if (mark_stack_tos + CGCDesc::GetNumPointers(method_table(oo), s, num_components) >= (mark_stack_limit - 1))
                    {
                        overflow_p = TRUE;
                    }
                }

                if (overflow_p == FALSE)
                {
                    dprintf(3,("pushing mark for %zx ", (size_t)oo));

                    go_through_object_cl (method_table(oo), oo, s, ppslot,
                                          {
                                              uint8_t* o = mark_queue.queue_mark(*ppslot, condemned_gen);
                                              if (o != nullptr)
                                              {
                                                  if (full_p)
                                                  {
                                                      m_boundary_fullgc (o);
                                                  }
                                                  else
                                                  {
                                                      m_boundary (o);
                                                  }
                                                  add_to_promoted_bytes (o, thread);
                                                  if (contain_pointers_or_collectible (o))
                                                  {
                                                      *(mark_stack_tos++) = o;
                                                  }
                                              }
                                          }
                        );
                }
                else
                {
                    dprintf(3,("mark stack overflow for object %zx ", (size_t)oo));
                    min_overflow_address = min (min_overflow_address, oo);
                    max_overflow_address = max (max_overflow_address, oo);
                }
            }
            else
            {
                if (partial_p (oo))
                {
                    start = ref_from_slot (oo);
                    oo = ref_from_slot (*(--mark_stack_tos));
                    dprintf (4, ("oo: %zx, start: %zx\n", (size_t)oo, (size_t)start));
                    assert ((oo < start) && (start < (oo + size (oo))));
                }
#ifdef COLLECTIBLE_CLASS
                else
                {
                    // If there's a class object, push it now. We are guaranteed to have the slot since
                    // we just popped one object off.
                    if (is_collectible (oo))
                    {
                        uint8_t* class_obj = get_class_object (oo);
                        if (gc_mark (class_obj, gc_low, gc_high, condemned_gen))
                        {
                            if (full_p)
                            {
                                m_boundary_fullgc (class_obj);
                            }
                            else
                            {
                                m_boundary (class_obj);
                            }

                            add_to_promoted_bytes (class_obj, thread);
                            *(mark_stack_tos++) = class_obj;
                            // The code below expects that the oo is still stored in the stack slot that was
                            // just popped and it "pushes" it back just by incrementing the mark_stack_tos.
                            // But the class_obj has just overwritten that stack slot and so the oo needs to
                            // be stored to the new slot that's pointed to by the mark_stack_tos.
                            *mark_stack_tos = oo;
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

                if (mark_stack_tos + (num_partial_refs + 2)  >= mark_stack_limit)
                {
                    overflow_p = TRUE;
                }
                if (overflow_p == FALSE)
                {
                    dprintf(3,("pushing mark for %zx ", (size_t)oo));

                    //push the object and its current
                    SERVER_SC_MARK_VOLATILE(uint8_t*)* place = ++mark_stack_tos;
                    mark_stack_tos++;
#ifdef MH_SC_MARK
                    *(place-1) = 0;
                    *(place) = (uint8_t*)partial;
#endif //MH_SC_MARK
                    int i = num_partial_refs;
                    uint8_t* ref_to_continue = 0;

                    go_through_object (method_table(oo), oo, s, ppslot,
                                       start, use_start, (oo + s),
                                       {
                                           uint8_t* o = mark_queue.queue_mark(*ppslot, condemned_gen);
                                           if (o != nullptr)
                                           {
                                                if (full_p)
                                                {
                                                    m_boundary_fullgc (o);
                                                }
                                                else
                                                {
                                                    m_boundary (o);
                                                }
                                                add_to_promoted_bytes (o, thread);
                                                if (contain_pointers_or_collectible (o))
                                                {
                                                    *(mark_stack_tos++) = o;
                                                    if (--i == 0)
                                                    {
                                                        ref_to_continue = (uint8_t*)((size_t)(ppslot+1) | partial);
                                                        goto more_to_do;
                                                    }

                                                }
                                           }

                                       }
                        );
                    //we are finished with this object
                    assert (ref_to_continue == 0);
#ifdef MH_SC_MARK
                    assert ((*(place-1)) == (uint8_t*)0);
#else //MH_SC_MARK
                    *(place-1) = 0;
#endif //MH_SC_MARK
                    *place = 0;
                    // shouldn't we decrease tos by 2 here??

more_to_do:
                    if (ref_to_continue)
                    {
                        //update the start
#ifdef MH_SC_MARK
                        assert ((*(place-1)) == (uint8_t*)0);
                        *(place-1) = (uint8_t*)((size_t)oo | partial_object);
                        assert (((*place) == (uint8_t*)1) || ((*place) == (uint8_t*)2));
#endif //MH_SC_MARK
                        *place = ref_to_continue;
                    }
                }
                else
                {
                    dprintf(3,("mark stack overflow for object %zx ", (size_t)oo));
                    min_overflow_address = min (min_overflow_address, oo);
                    max_overflow_address = max (max_overflow_address, oo);
                }
            }
        }
    next_level:
        if (!(mark_stack_empty_p()))
        {
            oo = *(--mark_stack_tos);
            start = oo;
        }
        else
            break;
    }
}

#ifdef MH_SC_MARK
BOOL same_numa_node_p (int hn1, int hn2)
{
    return (heap_select::find_numa_node_from_heap_no (hn1) == heap_select::find_numa_node_from_heap_no (hn2));
}

int find_next_buddy_heap (int this_heap_number, int current_buddy, int n_heaps)
{
    int hn = (current_buddy+1)%n_heaps;
    while (hn != current_buddy)
    {
        if ((this_heap_number != hn) && (same_numa_node_p (this_heap_number, hn)))
            return hn;
        hn = (hn+1)%n_heaps;
    }
    return current_buddy;
}

void
gc_heap::mark_steal()
{
    mark_stack_busy() = 0;
    //clear the mark stack in the snooping range
    for (int i = 0; i < max_snoop_level; i++)
    {
        ((VOLATILE(uint8_t*)*)(mark_stack_array))[i] = 0;
    }

    //pick the next heap as our buddy
    int thpn = find_next_buddy_heap (heap_number, heap_number, n_heaps);

#ifdef SNOOP_STATS
        dprintf (SNOOP_LOG, ("(GC%d)heap%d: start snooping %d", settings.gc_index, heap_number, (heap_number+1)%n_heaps));
        uint64_t begin_tick = GCToOSInterface::GetLowPrecisionTimeStamp();
#endif //SNOOP_STATS

    int idle_loop_count = 0;
    int first_not_ready_level = 0;

    while (1)
    {
        gc_heap* hp = g_heaps [thpn];
        int level = first_not_ready_level;
        first_not_ready_level = 0;

        while (check_next_mark_stack (hp) && (level < (max_snoop_level-1)))
        {
            idle_loop_count = 0;
#ifdef SNOOP_STATS
            snoop_stat.busy_count++;
            dprintf (SNOOP_LOG, ("heap%d: looking at next heap level %d stack contents: %zx",
                                 heap_number, level, (int)((uint8_t**)(hp->mark_stack_array))[level]));
#endif //SNOOP_STATS

            uint8_t* o = ref_mark_stack (hp, level);

            uint8_t* start = o;
            if (ref_p (o))
            {
                mark_stack_busy() = 1;

                BOOL success = TRUE;
                uint8_t* next = (ref_mark_stack (hp, level+1));
                if (ref_p (next))
                {
                    if (((size_t)o > 4) && !partial_object_p (o))
                    {
                        //this is a normal object, not a partial mark tuple
                        //success = (Interlocked::CompareExchangePointer (&ref_mark_stack (hp, level), 0, o)==o);
                        success = (Interlocked::CompareExchangePointer (&ref_mark_stack (hp, level), (uint8_t*)4, o)==o);
#ifdef SNOOP_STATS
                        snoop_stat.interlocked_count++;
                        if (success)
                            snoop_stat.normal_count++;
#endif //SNOOP_STATS
                    }
                    else
                    {
                        //it is a stolen entry, or beginning/ending of a partial mark
                        level++;
#ifdef SNOOP_STATS
                        snoop_stat.stolen_or_pm_count++;
#endif //SNOOP_STATS
                        success = FALSE;
                    }
                }
                else if (stolen_p (next))
                {
                    //ignore the stolen guy and go to the next level
                    success = FALSE;
                    level+=2;
#ifdef SNOOP_STATS
                    snoop_stat.stolen_entry_count++;
#endif //SNOOP_STATS
                }
                else
                {
                    assert (partial_p (next));
                    start = ref_from_slot (next);
                    //re-read the object
                    o = ref_from_slot (ref_mark_stack (hp, level));
                    if (o && start)
                    {
                        //steal the object
                        success = (Interlocked::CompareExchangePointer (&ref_mark_stack (hp, level+1),
                                                                        (uint8_t*)stolen, next) == next);
#ifdef SNOOP_STATS
                        snoop_stat.interlocked_count++;
                        if (success)
                        {
                            snoop_stat.partial_mark_parent_count++;
                        }
#endif //SNOOP_STATS
                    }
                    else
                    {
                        // stack is not ready, or o is completely different from the last time we read from this stack level.
                        // go up 2 levels to steal children or totally unrelated objects.
                        success = FALSE;
                        if (first_not_ready_level == 0)
                        {
                            first_not_ready_level = level;
                        }
                        level+=2;
#ifdef SNOOP_STATS
                        snoop_stat.pm_not_ready_count++;
#endif //SNOOP_STATS
                    }
                }
                if (success)
                {

#ifdef SNOOP_STATS
                    dprintf (SNOOP_LOG, ("heap%d: marking %zx from %d [%d] tl:%dms",
                            heap_number, (size_t)o, (heap_number+1)%n_heaps, level,
                            (GCToOSInterface::GetLowPrecisionTimeStamp()-begin_tick)));
                    uint64_t start_tick = GCToOSInterface::GetLowPrecisionTimeStamp();
#endif //SNOOP_STATS

                    mark_object_simple1 (o, start, heap_number);

#ifdef SNOOP_STATS
                    dprintf (SNOOP_LOG, ("heap%d: done marking %zx from %d [%d] %dms tl:%dms",
                            heap_number, (size_t)o, (heap_number+1)%n_heaps, level,
                            (GCToOSInterface::GetLowPrecisionTimeStamp()-start_tick),(GCToOSInterface::GetLowPrecisionTimeStamp()-begin_tick)));
#endif //SNOOP_STATS

                    mark_stack_busy() = 0;

                    //clear the mark stack in snooping range
                    for (int i = 0; i < max_snoop_level; i++)
                    {
                        if (((uint8_t**)mark_stack_array)[i] != 0)
                        {
                            ((VOLATILE(uint8_t*)*)(mark_stack_array))[i] = 0;
#ifdef SNOOP_STATS
                            snoop_stat.stack_bottom_clear_count++;
#endif //SNOOP_STATS
                        }
                    }

                    level = 0;
                }
                mark_stack_busy() = 0;
            }
            else
            {
                //slot is either partial or stolen
                level++;
            }
        }
        if ((first_not_ready_level != 0) && hp->mark_stack_busy())
        {
            continue;
        }
        if (!hp->mark_stack_busy())
        {
            first_not_ready_level = 0;
            idle_loop_count++;

            if ((idle_loop_count % (6) )==1)
            {
#ifdef SNOOP_STATS
                snoop_stat.switch_to_thread_count++;
#endif //SNOOP_STATS
                GCToOSInterface::Sleep(1);
            }
            int free_count = 1;
#ifdef SNOOP_STATS
            snoop_stat.stack_idle_count++;
            //dprintf (SNOOP_LOG, ("heap%d: counting idle threads", heap_number));
#endif //SNOOP_STATS
            for (int hpn = (heap_number+1)%n_heaps; hpn != heap_number;)
            {
                if (!((g_heaps [hpn])->mark_stack_busy()))
                {
                    free_count++;
#ifdef SNOOP_STATS
                dprintf (SNOOP_LOG, ("heap%d: %d idle", heap_number, free_count));
#endif //SNOOP_STATS
                }
                else if (same_numa_node_p (hpn, heap_number) || ((idle_loop_count%1000))==999)
                {
                    thpn = hpn;
                    break;
                }
                hpn = (hpn+1)%n_heaps;
                YieldProcessor();
            }
            if (free_count == n_heaps)
            {
                break;
            }
        }
    }
}

inline
BOOL gc_heap::check_next_mark_stack (gc_heap* next_heap)
{
#ifdef SNOOP_STATS
    snoop_stat.check_level_count++;
#endif //SNOOP_STATS
    return (next_heap->mark_stack_busy()>=1);
}

#endif //MH_SC_MARK
#ifdef SNOOP_STATS
void gc_heap::print_snoop_stat()
{
    dprintf (1234, ("%4s | %8s | %8s | %8s | %8s | %8s | %8s | %8s",
        "heap", "check", "zero", "mark", "stole", "pstack", "nstack", "nonsk"));
    dprintf (1234, ("%4d | %8d | %8d | %8d | %8d | %8d | %8d | %8d",
        snoop_stat.heap_index,
        snoop_stat.objects_checked_count,
        snoop_stat.zero_ref_count,
        snoop_stat.objects_marked_count,
        snoop_stat.stolen_stack_count,
        snoop_stat.partial_stack_count,
        snoop_stat.normal_stack_count,
        snoop_stat.non_stack_count));
    dprintf (1234, ("%4s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s",
        "heap", "level", "busy", "xchg", "pmparent", "s_pm", "stolen", "nready", "clear"));
    dprintf (1234, ("%4d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d\n",
        snoop_stat.heap_index,
        snoop_stat.check_level_count,
        snoop_stat.busy_count,
        snoop_stat.interlocked_count,
        snoop_stat.partial_mark_parent_count,
        snoop_stat.stolen_or_pm_count,
        snoop_stat.stolen_entry_count,
        snoop_stat.pm_not_ready_count,
        snoop_stat.normal_count,
        snoop_stat.stack_bottom_clear_count));

    printf ("\n%4s | %8s | %8s | %8s | %8s | %8s\n",
        "heap", "check", "zero", "mark", "idle", "switch");
    printf ("%4d | %8d | %8d | %8d | %8d | %8d\n",
        snoop_stat.heap_index,
        snoop_stat.objects_checked_count,
        snoop_stat.zero_ref_count,
        snoop_stat.objects_marked_count,
        snoop_stat.stack_idle_count,
        snoop_stat.switch_to_thread_count);
    printf ("%4s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s\n",
        "heap", "level", "busy", "xchg", "pmparent", "s_pm", "stolen", "nready", "normal", "clear");
    printf ("%4d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d\n",
        snoop_stat.heap_index,
        snoop_stat.check_level_count,
        snoop_stat.busy_count,
        snoop_stat.interlocked_count,
        snoop_stat.partial_mark_parent_count,
        snoop_stat.stolen_or_pm_count,
        snoop_stat.stolen_entry_count,
        snoop_stat.pm_not_ready_count,
        snoop_stat.normal_count,
        snoop_stat.stack_bottom_clear_count);
}

#endif //SNOOP_STATS
#ifdef HEAP_ANALYZE
void
gc_heap::ha_mark_object_simple (uint8_t** po THREAD_NUMBER_DCL)
{
    if (!internal_root_array)
    {
        internal_root_array = new (nothrow) uint8_t* [internal_root_array_length];
        if (!internal_root_array)
        {
            heap_analyze_success = FALSE;
        }
    }

    if (heap_analyze_success && (internal_root_array_length <= internal_root_array_index))
    {
        size_t new_size = 2*internal_root_array_length;

        uint64_t available_physical = 0;
        get_memory_info (NULL, &available_physical);
        if (new_size > (size_t)(available_physical / 10))
        {
            heap_analyze_success = FALSE;
        }
        else
        {
            uint8_t** tmp = new (nothrow) uint8_t* [new_size];
            if (tmp)
            {
                memcpy (tmp, internal_root_array,
                        internal_root_array_length*sizeof (uint8_t*));
                delete[] internal_root_array;
                internal_root_array = tmp;
                internal_root_array_length = new_size;
            }
            else
            {
                heap_analyze_success = FALSE;
            }
        }
    }

    if (heap_analyze_success)
    {
        _ASSERTE(internal_root_array_index < internal_root_array_length);

        uint8_t* ref = (uint8_t*)po;
        if (!current_obj ||
            !((ref >= current_obj) && (ref < (current_obj + current_obj_size))))
        {
            gc_heap* hp = gc_heap::heap_of (ref);
            current_obj = hp->find_object (ref);
            current_obj_size = size (current_obj);

            internal_root_array[internal_root_array_index] = current_obj;
            internal_root_array_index++;
        }
    }

    mark_object_simple (po THREAD_NUMBER_ARG);
}

#endif //HEAP_ANALYZE

//this method assumes that *po is in the [low. high[ range
void
gc_heap::mark_object_simple (uint8_t** po THREAD_NUMBER_DCL)
{
    int condemned_gen =
#ifdef USE_REGIONS
        settings.condemned_generation;
#else
        -1;
#endif //USE_REGIONS

    uint8_t* o = *po;
#ifndef MULTIPLE_HEAPS
    const int thread = 0;
#endif //MULTIPLE_HEAPS
    {
#ifdef SNOOP_STATS
        snoop_stat.objects_checked_count++;
#endif //SNOOP_STATS

        o = mark_queue.queue_mark (o);
        if (o != nullptr)
        {
            m_boundary (o);
            size_t s = size (o);
            add_to_promoted_bytes (o, s, thread);
            {
                go_through_object_cl (method_table(o), o, s, poo,
                                        {
                                            uint8_t* oo = mark_queue.queue_mark(*poo, condemned_gen);
                                            if (oo != nullptr)
                                            {
                                                m_boundary (oo);
                                                add_to_promoted_bytes (oo, thread);
                                                if (contain_pointers_or_collectible (oo))
                                                    mark_object_simple1 (oo, oo THREAD_NUMBER_ARG);
                                            }
                                        }
                    );
            }
        }
    }
}

inline
void gc_heap::mark_object (uint8_t* o THREAD_NUMBER_DCL)
{
#ifdef USE_REGIONS
    if (is_in_gc_range (o) && is_in_condemned_gc (o))
    {
        mark_object_simple (&o THREAD_NUMBER_ARG);
    }
#else //USE_REGIONS
    if ((o >= gc_low) && (o < gc_high))
        mark_object_simple (&o THREAD_NUMBER_ARG);
#ifdef MULTIPLE_HEAPS
    else if (o)
    {
        gc_heap* hp = heap_of (o);
        assert (hp);
        if ((o >= hp->gc_low) && (o < hp->gc_high))
            mark_object_simple (&o THREAD_NUMBER_ARG);
    }
#endif //MULTIPLE_HEAPS
#endif //USE_REGIONS
}

void gc_heap::drain_mark_queue ()
{
    int condemned_gen =
#ifdef USE_REGIONS
        settings.condemned_generation;
#else
        -1;
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
    THREAD_FROM_HEAP;
#else
    const int thread = 0;
#endif //MULTIPLE_HEAPS

    uint8_t* o;
    while ((o = mark_queue.get_next_marked()) != nullptr)
    {
        m_boundary (o);
        size_t s = size (o);
        add_to_promoted_bytes (o, s, thread);
        if (contain_pointers_or_collectible (o))
        {
            go_through_object_cl (method_table(o), o, s, poo,
                                    {
                                        uint8_t* oo = mark_queue.queue_mark(*poo, condemned_gen);
                                        if (oo != nullptr)
                                        {
                                            m_boundary (oo);
                                            add_to_promoted_bytes (oo, thread);
                                            if (contain_pointers_or_collectible (oo))
                                                mark_object_simple1 (oo, oo THREAD_NUMBER_ARG);
                                        }
                                    }
                );
        }
    }
}

inline
void gc_heap::mark_through_object (uint8_t* oo, BOOL mark_class_object_p THREAD_NUMBER_DCL)
{
#ifndef COLLECTIBLE_CLASS
    UNREFERENCED_PARAMETER(mark_class_object_p);
    BOOL to_mark_class_object = FALSE;
#else //COLLECTIBLE_CLASS
    BOOL to_mark_class_object = (mark_class_object_p && (is_collectible(oo)));
#endif //COLLECTIBLE_CLASS
    if (contain_pointers (oo) || to_mark_class_object)
    {
        dprintf(3,( "Marking through %zx", (size_t)oo));
        size_t s = size (oo);

#ifdef COLLECTIBLE_CLASS
        if (to_mark_class_object)
        {
            uint8_t* class_obj = get_class_object (oo);
            mark_object (class_obj THREAD_NUMBER_ARG);
        }
#endif //COLLECTIBLE_CLASS

        if (contain_pointers (oo))
        {
            go_through_object_nostart (method_table(oo), oo, s, po,
                                uint8_t* o = *po;
                                mark_object (o THREAD_NUMBER_ARG);
                                );
        }
    }
}

//returns TRUE is an overflow happened.
BOOL gc_heap::process_mark_overflow(int condemned_gen_number)
{
    size_t last_promoted_bytes = get_promoted_bytes();

    BOOL  overflow_p = FALSE;
recheck:
    drain_mark_queue();
    if ((! (max_overflow_address == 0) ||
         ! (min_overflow_address == MAX_PTR)))
    {
        overflow_p = TRUE;
        // Try to grow the array.
        size_t new_size =
            max ((size_t)MARK_STACK_INITIAL_LENGTH, 2*mark_stack_array_length);

        if ((new_size * sizeof(mark)) > 100*1024)
        {
            size_t new_max_size = (get_total_heap_size() / 10) / sizeof(mark);

            new_size = min(new_max_size, new_size);
        }

        if ((mark_stack_array_length < new_size) &&
            ((new_size - mark_stack_array_length) > (mark_stack_array_length / 2)))
        {
            mark* tmp = new (nothrow) mark [new_size];
            if (tmp)
            {
                delete[] mark_stack_array;
                mark_stack_array = tmp;
                mark_stack_array_length = new_size;
            }
        }

        uint8_t*  min_add = min_overflow_address;
        uint8_t*  max_add = max_overflow_address;
        max_overflow_address = 0;
        min_overflow_address = MAX_PTR;
        process_mark_overflow_internal (condemned_gen_number, min_add, max_add);
        goto recheck;
    }

    size_t current_promoted_bytes = get_promoted_bytes();
    if (current_promoted_bytes != last_promoted_bytes)
        fire_mark_event (ETW::GC_ROOT_OVERFLOW, current_promoted_bytes, last_promoted_bytes);
    return overflow_p;
}

void gc_heap::process_mark_overflow_internal (int condemned_gen_number,
                                              uint8_t* min_add, uint8_t* max_add)
{
#ifdef MULTIPLE_HEAPS
    int thread = heap_number;
#endif //MULTIPLE_HEAPS
    BOOL  full_p = (condemned_gen_number == max_generation);

    dprintf(3,("Processing Mark overflow [%zx %zx]", (size_t)min_add, (size_t)max_add));

    size_t obj_count = 0;

#ifdef MULTIPLE_HEAPS
    for (int hi = 0; hi < n_heaps; hi++)
    {
        gc_heap*  hp = g_heaps [(heap_number + hi) % n_heaps];

#else
    {
        gc_heap*  hp = 0;
#endif //MULTIPLE_HEAPS
        int gen_limit = full_p ? total_generation_count : condemned_gen_number + 1;

        for (int i = get_stop_generation_index (condemned_gen_number); i < gen_limit; i++)
        {
            generation* gen = hp->generation_of (i);
            heap_segment* seg = heap_segment_in_range (generation_start_segment (gen));
            int align_const = get_alignment_constant (i < uoh_start_generation);

            _ASSERTE(seg != NULL);

            while (seg)
            {
                uint8_t*  o = max (heap_segment_mem (seg), min_add);
                uint8_t*  end = heap_segment_allocated (seg);

                while ((o < end) && (o <= max_add))
                {
                    assert ((min_add <= o) && (max_add >= o));
                    dprintf (3, ("considering %zx", (size_t)o));
                    if (marked (o))
                    {
                        mark_through_object (o, TRUE THREAD_NUMBER_ARG);
                        obj_count++;
                    }

                    o = o + Align (size (o), align_const);
                }

                seg = heap_segment_next_in_range (seg);
            }
        }
#ifndef MULTIPLE_HEAPS
        // we should have found at least one object
        assert (obj_count > 0);
#endif //MULTIPLE_HEAPS
    }
}

// Scanning for promotion for dependent handles need special handling. Because the primary holds a strong
// reference to the secondary (when the primary itself is reachable) and this can cause a cascading series of
// promotions (the secondary of one handle is or promotes the primary of another) we might need to perform the
// promotion scan multiple times.
// This helper encapsulates the logic to complete all dependent handle promotions when running a server GC. It
// also has the effect of processing any mark stack overflow.

#ifdef MULTIPLE_HEAPS
// When multiple heaps are enabled we have must utilize a more complex algorithm in order to keep all the GC
// worker threads synchronized. The algorithms are sufficiently divergent that we have different
// implementations based on whether MULTIPLE_HEAPS is defined or not.
//
// Define some static variables used for synchronization in the method below. These should really be defined
// locally but MSVC complains when the VOLATILE macro is expanded into an instantiation of the Volatile class.
//
// A note about the synchronization used within this method. Communication between the worker threads is
// achieved via two shared booleans (defined below). These both act as latches that are transitioned only from
// false -> true by unsynchronized code. They are only read or reset to false by a single thread under the
// protection of a join
void gc_heap::scan_dependent_handles (int condemned_gen_number, ScanContext *sc, BOOL initial_scan_p)
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

        drain_mark_queue();

        // Synchronize all the threads so we can read our state variables safely. The shared variable
        // s_fScanRequired, indicating whether we should scan the tables or terminate the loop, will be set by
        // a single thread inside the join.
        gc_t_join.join(this, gc_join_scan_dependent_handles);
        if (gc_t_join.joined())
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
                // We're terminating the loop. Perform any last operations that require single threaded access.
                if (!initial_scan_p)
                {
                    // On the second invocation we reconcile all mark overflow ranges across the heaps. This can help
                    // load balance if some of the heaps have an abnormally large workload.
                    uint8_t* all_heaps_max = 0;
                    uint8_t* all_heaps_min = MAX_PTR;
                    int i;
                    for (i = 0; i < n_heaps; i++)
                    {
                        if (all_heaps_max < g_heaps[i]->max_overflow_address)
                            all_heaps_max = g_heaps[i]->max_overflow_address;
                        if (all_heaps_min > g_heaps[i]->min_overflow_address)
                            all_heaps_min = g_heaps[i]->min_overflow_address;
                    }
                    for (i = 0; i < n_heaps; i++)
                    {
                        g_heaps[i]->max_overflow_address = all_heaps_max;
                        g_heaps[i]->min_overflow_address = all_heaps_min;
                    }
                }
            }

            dprintf(3, ("Starting all gc thread mark stack overflow processing"));
            gc_t_join.restart();
        }

        // Handle any mark stack overflow: scanning dependent handles relies on all previous object promotions
        // being visible. If there really was an overflow (process_mark_overflow returns true) then set the
        // global flag indicating that at least one object promotion may have occurred (the usual comment
        // about races applies). (Note it's OK to set this flag even if we're about to terminate the loop and
        // exit the method since we unconditionally set this variable on method entry anyway).
        if (process_mark_overflow(condemned_gen_number))
            s_fUnscannedPromotions = TRUE;

        // If we decided that no scan was required we can terminate the loop now.
        if (!s_fScanRequired)
            break;

        // Otherwise we must join with the other workers to ensure that all mark stack overflows have been
        // processed before we start scanning dependent handle tables (if overflows remain while we scan we
        // could miss noting the promotion of some primary objects).
        gc_t_join.join(this, gc_join_rescan_dependent_handles);
        if (gc_t_join.joined())
        {
            dprintf(3, ("Starting all gc thread for dependent handle promotion"));
            gc_t_join.restart();
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
// Non-multiple heap version of scan_dependent_handles: much simpler without the need to keep multiple worker
// threads synchronized.
void gc_heap::scan_dependent_handles (int condemned_gen_number, ScanContext *sc, BOOL initial_scan_p)
{
    UNREFERENCED_PARAMETER(initial_scan_p);

    // Whenever we call this method there may have been preceding object promotions. So set
    // fUnscannedPromotions unconditionally (during further iterations of the scanning loop this will be set
    // based on the how the scanning proceeded).
    bool fUnscannedPromotions = true;

    // Loop until there are either no more dependent handles that can have their secondary promoted or we've
    // managed to perform a scan without promoting anything new.
    while (GCScan::GcDhUnpromotedHandlesExist(sc) && fUnscannedPromotions)
    {
        // On each iteration of the loop start with the assumption that no further objects have been promoted.
        fUnscannedPromotions = false;

        // Handle any mark stack overflow: scanning dependent handles relies on all previous object promotions
        // being visible. If there was an overflow (process_mark_overflow returned true) then additional
        // objects now appear to be promoted and we should set the flag.
        if (process_mark_overflow(condemned_gen_number))
            fUnscannedPromotions = true;

        // mark queue must be empty after process_mark_overflow
        mark_queue.verify_empty();

        // Perform the scan and set the flag if any promotions resulted.
        if (GCScan::GcDhReScan(sc))
            fUnscannedPromotions = true;
    }

    // Process any mark stack overflow that may have resulted from scanning handles (or if we didn't need to
    // scan any handles at all this is the processing of overflows that may have occurred prior to this method
    // invocation).
    process_mark_overflow(condemned_gen_number);
}

#endif //MULTIPLE_HEAPS

BOOL gc_heap::decide_on_promotion_surv (size_t threshold)
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
#else //MULTIPLE_HEAPS
    {
        gc_heap* hp = pGenGCHeap;
        int i = 0;
#endif //MULTIPLE_HEAPS
        dynamic_data* dd = hp->dynamic_data_of (min ((int)(settings.condemned_generation + 1), (int)max_generation));
        size_t older_gen_size = dd_current_size (dd) + (dd_desired_allocation (dd) - dd_new_allocation (dd));

        size_t promoted = hp->total_promoted_bytes;

        dprintf (6666, ("h%d promotion threshold: %zd, promoted bytes: %zd size n+1: %zd -> %s",
            i, threshold, promoted, older_gen_size,
            (((threshold > (older_gen_size)) || (promoted > threshold)) ? "promote" : "don't promote")));

        if ((threshold > (older_gen_size)) || (promoted > threshold))
        {
            return TRUE;
        }
    }

    return FALSE;
}

inline
void gc_heap::fire_mark_event (int root_type, size_t& current_promoted_bytes, size_t& last_promoted_bytes)
{
#ifdef FEATURE_EVENT_TRACE
    if (informational_event_enabled_p)
    {
        current_promoted_bytes = get_promoted_bytes();
        size_t root_promoted = current_promoted_bytes - last_promoted_bytes;
        dprintf (3, ("h%d marked root %s: %zd (%zd - %zd)",
            heap_number, str_root_kinds[root_type], root_promoted,
            current_promoted_bytes, last_promoted_bytes));
        FIRE_EVENT(GCMarkWithType, heap_number, root_type, root_promoted);
        last_promoted_bytes = current_promoted_bytes;
    }
#endif // FEATURE_EVENT_TRACE
}

#ifdef FEATURE_EVENT_TRACE
inline
void gc_heap::record_mark_time (uint64_t& mark_time,
                                uint64_t& current_mark_time,
                                uint64_t& last_mark_time)
{
    if (informational_event_enabled_p)
    {
        current_mark_time = GetHighPrecisionTimeStamp();
        mark_time = limit_time_to_uint32 (current_mark_time - last_mark_time);
        dprintf (3, ("%zd - %zd = %zd",
            current_mark_time, last_mark_time, (current_mark_time - last_mark_time)));
        last_mark_time = current_mark_time;
    }
}

#endif //FEATURE_EVENT_TRACE

void gc_heap::mark_phase (int condemned_gen_number)
{
    assert (settings.concurrent == FALSE);

    ScanContext sc;
    sc.thread_number = heap_number;
    sc.thread_count = n_heaps;
    sc.promotion = TRUE;
    sc.concurrent = FALSE;

    dprintf (2, (ThreadStressLog::gcStartMarkMsg(), heap_number, condemned_gen_number));
    BOOL  full_p = (condemned_gen_number == max_generation);

    int gen_to_init = condemned_gen_number;
    if (condemned_gen_number == max_generation)
    {
        gen_to_init = total_generation_count - 1;
    }

    for (int gen_idx = 0; gen_idx <= gen_to_init; gen_idx++)
    {
        dynamic_data* dd = dynamic_data_of (gen_idx);
        dd_begin_data_size (dd) = generation_size (gen_idx) -
                                   dd_fragmentation (dd) -
#ifdef USE_REGIONS
                                   0;
#else
                                   get_generation_start_size (gen_idx);
#endif //USE_REGIONS
        dprintf (2, ("begin data size for gen%d is %zd", gen_idx, dd_begin_data_size (dd)));
        dd_survived_size (dd) = 0;
        dd_pinned_survived_size (dd) = 0;
        dd_artificial_pinned_survived_size (dd) = 0;
        dd_added_pinned_size (dd) = 0;
#ifdef SHORT_PLUGS
        dd_padding_size (dd) = 0;
#endif //SHORT_PLUGS
#if defined (RESPECT_LARGE_ALIGNMENT) || defined (FEATURE_STRUCTALIGN)
        dd_num_npinned_plugs (dd) = 0;
#endif //RESPECT_LARGE_ALIGNMENT || FEATURE_STRUCTALIGN
    }

    if (gen0_must_clear_bricks > 0)
        gen0_must_clear_bricks--;

    size_t last_promoted_bytes = 0;
    size_t current_promoted_bytes = 0;
#if !defined(USE_REGIONS) || defined(_DEBUG)
    init_promoted_bytes();
#endif //!USE_REGIONS || _DEBUG
    reset_mark_stack();

#ifdef SNOOP_STATS
    memset (&snoop_stat, 0, sizeof(snoop_stat));
    snoop_stat.heap_index = heap_number;
#endif //SNOOP_STATS

#ifdef MH_SC_MARK
    if (full_p)
    {
        //initialize the mark stack
        for (int i = 0; i < max_snoop_level; i++)
        {
            ((uint8_t**)(mark_stack_array))[i] = 0;
        }

        mark_stack_busy() = 1;
    }
#endif //MH_SC_MARK

    static uint32_t num_sizedrefs = 0;

#ifdef MH_SC_MARK
    static BOOL do_mark_steal_p = FALSE;
#endif //MH_SC_MARK

#ifdef FEATURE_CARD_MARKING_STEALING
    reset_card_marking_enumerators();
#endif // FEATURE_CARD_MARKING_STEALING

#ifdef STRESS_REGIONS
    heap_segment* gen0_region = generation_start_segment (generation_of (0));
    while (gen0_region)
    {
        size_t gen0_region_size = heap_segment_allocated (gen0_region) - heap_segment_mem (gen0_region);

        if (gen0_region_size > 0)
        {
            if ((num_gen0_regions % pinning_seg_interval) == 0)
            {
                dprintf (REGIONS_LOG, ("h%d potentially creating pinning in region %zx",
                    heap_number, heap_segment_mem (gen0_region)));

                int align_const = get_alignment_constant (TRUE);
                // Pinning the first and the middle object in the region.
                uint8_t* boundary = heap_segment_mem (gen0_region);
                uint8_t* obj_to_pin = boundary;
                int num_pinned_objs = 0;
                while (obj_to_pin < heap_segment_allocated (gen0_region))
                {
                    if (obj_to_pin >= boundary && !((CObjectHeader*)obj_to_pin)->IsFree())
                    {
                        pin_by_gc (obj_to_pin);
                        num_pinned_objs++;
                        if (num_pinned_objs >= 2)
                            break;
                        boundary += (gen0_region_size / 2) + 1;
                    }
                    obj_to_pin += Align (size (obj_to_pin), align_const);
                }
            }
        }

        num_gen0_regions++;
        gen0_region = heap_segment_next (gen0_region);
    }
#endif //STRESS_REGIONS

#ifdef FEATURE_EVENT_TRACE
    static uint64_t current_mark_time = 0;
    static uint64_t last_mark_time = 0;
#endif //FEATURE_EVENT_TRACE

#ifdef USE_REGIONS
    special_sweep_p = false;
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
    gc_t_join.join(this, gc_join_begin_mark_phase);
    if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
        maxgen_size_inc_p = false;

#ifdef USE_REGIONS
        region_count = global_region_allocator.get_used_region_count();
        grow_mark_list_piece();
        verify_region_to_generation_map();
        compute_gc_and_ephemeral_range (condemned_gen_number, false);
#endif //USE_REGIONS

        GCToEEInterface::BeforeGcScanRoots(condemned_gen_number, /* is_bgc */ false, /* is_concurrent */ false);

#ifdef FEATURE_SIZED_REF_HANDLES
        num_sizedrefs = GCToEEInterface::GetTotalNumSizedRefHandles();
#endif // FEATURE_SIZED_REF_HANDLES

#ifdef FEATURE_EVENT_TRACE
        informational_event_enabled_p = EVENT_ENABLED (GCMarkWithType);
        if (informational_event_enabled_p)
        {
            last_mark_time = GetHighPrecisionTimeStamp();
            // We may not have SizedRefs to mark so init it to 0.
            gc_time_info[time_mark_sizedref] = 0;
        }
#endif //FEATURE_EVENT_TRACE

#ifdef MULTIPLE_HEAPS
#ifdef MH_SC_MARK
        if (full_p)
        {
            size_t total_heap_size = get_total_heap_size();

            if (total_heap_size > (100 * 1024 * 1024))
            {
                do_mark_steal_p = TRUE;
            }
            else
            {
                do_mark_steal_p = FALSE;
            }
        }
        else
        {
            do_mark_steal_p = FALSE;
        }
#endif //MH_SC_MARK

        gc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

    {
        //set up the mark lists from g_mark_list
        assert (g_mark_list);
#ifdef MULTIPLE_HEAPS
        mark_list_size = g_mark_list_total_size / n_heaps;
        mark_list = &g_mark_list [heap_number*mark_list_size];
#else
        mark_list = g_mark_list;
#endif //MULTIPLE_HEAPS
        //dont use the mark list for full gc
        //because multiple segments are more complex to handle and the list
        //is likely to overflow
        if (condemned_gen_number < max_generation)
            mark_list_end = &mark_list [mark_list_size-1];
        else
            mark_list_end = &mark_list [0];
        mark_list_index = &mark_list [0];

#ifdef USE_REGIONS
        if (g_mark_list_piece != nullptr)
        {
#ifdef MULTIPLE_HEAPS
            // two arrays with g_mark_list_piece_size entries per heap
            mark_list_piece_start = &g_mark_list_piece[heap_number * 2 * g_mark_list_piece_size];
            mark_list_piece_end = &mark_list_piece_start[g_mark_list_piece_size];
#endif //MULTIPLE_HEAPS
            survived_per_region = (size_t*)&g_mark_list_piece[heap_number * 2 * g_mark_list_piece_size];
            old_card_survived_per_region = (size_t*)&survived_per_region[g_mark_list_piece_size];
            size_t region_info_to_clear = region_count * sizeof (size_t);
            memset (survived_per_region, 0, region_info_to_clear);
            memset (old_card_survived_per_region, 0, region_info_to_clear);
        }
        else
        {
#ifdef MULTIPLE_HEAPS
            // disable use of mark list altogether
            mark_list_piece_start = nullptr;
            mark_list_piece_end = nullptr;
            mark_list_end = &mark_list[0];
#endif //MULTIPLE_HEAPS
            survived_per_region = nullptr;
            old_card_survived_per_region = nullptr;
        }
#endif // USE_REGIONS && MULTIPLE_HEAPS

#ifndef MULTIPLE_HEAPS
        shigh = (uint8_t*) 0;
        slow  = MAX_PTR;
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_SIZED_REF_HANDLES
        if ((condemned_gen_number == max_generation) && (num_sizedrefs > 0))
        {
            GCScan::GcScanSizedRefs(GCHeap::Promote, condemned_gen_number, max_generation, &sc);
            drain_mark_queue();
            fire_mark_event (ETW::GC_ROOT_SIZEDREF, current_promoted_bytes, last_promoted_bytes);

#ifdef MULTIPLE_HEAPS
            gc_t_join.join(this, gc_join_scan_sizedref_done);
            if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
            {
#ifdef FEATURE_EVENT_TRACE
                record_mark_time (gc_time_info[time_mark_sizedref], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

#ifdef MULTIPLE_HEAPS
                dprintf(3, ("Done with marking all sized refs. Starting all gc thread for marking other strong roots"));
                gc_t_join.restart();
#endif //MULTIPLE_HEAPS
            }
        }
#endif // FEATURE_SIZED_REF_HANDLES

#if defined(FEATURE_BASICFREEZE) && !defined(USE_REGIONS)
        if (ro_segments_in_range)
        {
            dprintf(3,("Marking in range ro segments"));
            mark_ro_segments();
            // Should fire an ETW event here.
        }
#endif //FEATURE_BASICFREEZE && !USE_REGIONS

        dprintf(3,("Marking Roots"));

        GCScan::GcScanRoots(GCHeap::Promote,
                                condemned_gen_number, max_generation,
                                &sc);
        drain_mark_queue();
        fire_mark_event (ETW::GC_ROOT_STACK, current_promoted_bytes, last_promoted_bytes);

#ifdef BACKGROUND_GC
        if (gc_heap::background_running_p())
        {
            scan_background_roots (GCHeap::Promote, heap_number, &sc);
            drain_mark_queue();
            fire_mark_event (ETW::GC_ROOT_BGC, current_promoted_bytes, last_promoted_bytes);
        }
#endif //BACKGROUND_GC

#ifdef FEATURE_PREMORTEM_FINALIZATION
        dprintf(3, ("Marking finalization data"));
        finalize_queue->GcScanRoots(GCHeap::Promote, heap_number, 0);
        drain_mark_queue();
        fire_mark_event (ETW::GC_ROOT_FQ, current_promoted_bytes, last_promoted_bytes);
#endif // FEATURE_PREMORTEM_FINALIZATION

        dprintf(3,("Marking handle table"));
        GCScan::GcScanHandles(GCHeap::Promote,
                                    condemned_gen_number, max_generation,
                                    &sc);
        drain_mark_queue();
        fire_mark_event (ETW::GC_ROOT_HANDLES, current_promoted_bytes, last_promoted_bytes);

        if (!full_p)
        {
#ifdef USE_REGIONS
            save_current_survived();
#endif //USE_REGIONS

#ifdef FEATURE_CARD_MARKING_STEALING
            n_eph_soh = 0;
            n_gen_soh = 0;
            n_eph_loh = 0;
            n_gen_loh = 0;
#endif //FEATURE_CARD_MARKING_STEALING

#ifdef CARD_BUNDLE
#ifdef MULTIPLE_HEAPS
            if (gc_t_join.r_join(this, gc_r_join_update_card_bundle))
            {
#endif //MULTIPLE_HEAPS

#ifndef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                // If we are manually managing card bundles, every write to the card table should already be
                // accounted for in the card bundle table so there's nothing to update here.
                update_card_table_bundle();
#endif
                if (card_bundles_enabled())
                {
                    verify_card_bundles();
                }

#ifdef MULTIPLE_HEAPS
                gc_t_join.r_restart();
            }
#endif //MULTIPLE_HEAPS
#endif //CARD_BUNDLE

            card_fn mark_object_fn = &gc_heap::mark_object_simple;
#ifdef HEAP_ANALYZE
            heap_analyze_success = TRUE;
            if (heap_analyze_enabled)
            {
                internal_root_array_index = 0;
                current_obj = 0;
                current_obj_size = 0;
                mark_object_fn = &gc_heap::ha_mark_object_simple;
            }
#endif //HEAP_ANALYZE

#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
            if (!card_mark_done_soh)
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
            {
                dprintf (3, ("Marking cross generation pointers on heap %d", heap_number));
                mark_through_cards_for_segments(mark_object_fn, FALSE THIS_ARG);
#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
                card_mark_done_soh = true;
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
            }

#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
            if (!card_mark_done_uoh)
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
            {
                dprintf (3, ("Marking cross generation pointers for uoh objects on heap %d", heap_number));
                for (int i = uoh_start_generation; i < total_generation_count; i++)
                {
#ifndef ALLOW_REFERENCES_IN_POH
                    if (i != poh_generation)
#endif //ALLOW_REFERENCES_IN_POH
                        mark_through_cards_for_uoh_objects(mark_object_fn, i, FALSE THIS_ARG);
                }

#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
                card_mark_done_uoh = true;
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING
            }

#if defined(MULTIPLE_HEAPS) && defined(FEATURE_CARD_MARKING_STEALING)
            // check the other heaps cyclically and try to help out where the marking isn't done
            for (int i = 0; i < gc_heap::n_heaps; i++)
            {
                int heap_number_to_look_at = (i + heap_number) % gc_heap::n_heaps;
                gc_heap* hp = gc_heap::g_heaps[heap_number_to_look_at];
                if (!hp->card_mark_done_soh)
                {
                    dprintf(3, ("Marking cross generation pointers on heap %d", hp->heap_number));
                    hp->mark_through_cards_for_segments(mark_object_fn, FALSE THIS_ARG);
                    hp->card_mark_done_soh = true;
                }

                if (!hp->card_mark_done_uoh)
                {
                    dprintf(3, ("Marking cross generation pointers for large objects on heap %d", hp->heap_number));
                    for (int i = uoh_start_generation; i < total_generation_count; i++)
                    {
#ifndef ALLOW_REFERENCES_IN_POH
                        if (i != poh_generation)
#endif //ALLOW_REFERENCES_IN_POH
                            hp->mark_through_cards_for_uoh_objects(mark_object_fn, i, FALSE THIS_ARG);
                    }

                    hp->card_mark_done_uoh = true;
                }
            }
#endif // MULTIPLE_HEAPS && FEATURE_CARD_MARKING_STEALING

            drain_mark_queue();

#ifdef USE_REGIONS
            update_old_card_survived();
#endif //USE_REGIONS

            fire_mark_event (ETW::GC_ROOT_OLDER, current_promoted_bytes, last_promoted_bytes);
        }
    }

#ifdef MH_SC_MARK
    if (do_mark_steal_p)
    {
        mark_steal();
        drain_mark_queue();
        fire_mark_event (ETW::GC_ROOT_STEAL, current_promoted_bytes, last_promoted_bytes);
    }
#endif //MH_SC_MARK

    // Dependent handles need to be scanned with a special algorithm (see the header comment on
    // scan_dependent_handles for more detail). We perform an initial scan without synchronizing with other
    // worker threads or processing any mark stack overflow. This is not guaranteed to complete the operation
    // but in a common case (where there are no dependent handles that are due to be collected) it allows us
    // to optimize away further scans. The call to scan_dependent_handles is what will cycle through more
    // iterations if required and will also perform processing of any mark stack overflow once the dependent
    // handle table has been fully promoted.
    GCScan::GcDhInitialScan(GCHeap::Promote, condemned_gen_number, max_generation, &sc);
    scan_dependent_handles(condemned_gen_number, &sc, true);

    // mark queue must be empty after scan_dependent_handles
    mark_queue.verify_empty();
    fire_mark_event (ETW::GC_ROOT_DH_HANDLES, current_promoted_bytes, last_promoted_bytes);

#ifdef FEATURE_JAVAMARSHAL

#ifdef MULTIPLE_HEAPS
    dprintf(3, ("Joining for short weak handle scan"));
    gc_t_join.join(this, gc_join_bridge_processing);
    if (gc_t_join.joined())
    {
#endif //MULTIPLE_HEAPS
        global_bridge_list = GCScan::GcProcessBridgeObjects (condemned_gen_number, max_generation, &sc, &num_global_bridge_objs);

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
            mark_object_simple (&global_bridge_list[obj_idx] THREAD_NUMBER_ARG);
        }

        drain_mark_queue();
        // using GC_ROOT_DH_HANDLES temporarily. add a new value for GC_ROOT_BRIDGE
        fire_mark_event (ETW::GC_ROOT_DH_HANDLES, current_promoted_bytes, last_promoted_bytes);
    }
#endif //FEATURE_JAVAMARSHAL

#ifdef MULTIPLE_HEAPS
    dprintf(3, ("Joining for short weak handle scan"));
    gc_t_join.join(this, gc_join_null_dead_short_weak);
    if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef FEATURE_EVENT_TRACE
        record_mark_time (gc_time_info[time_mark_roots], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

        uint64_t promoted_bytes_global = 0;
#ifdef HEAP_ANALYZE
        heap_analyze_enabled = FALSE;
#ifdef MULTIPLE_HEAPS
        for (int i = 0; i < n_heaps; i++)
        {
            promoted_bytes_global += g_heaps[i]->get_promoted_bytes();
        }
#else
        promoted_bytes_global = get_promoted_bytes();
#endif //MULTIPLE_HEAPS

        GCToEEInterface::AnalyzeSurvivorsFinished (settings.gc_index, condemned_gen_number, promoted_bytes_global, GCHeap::ReportGenerationBounds);
#endif // HEAP_ANALYZE
        GCToEEInterface::AfterGcScanRoots (condemned_gen_number, max_generation, &sc);

#ifdef MULTIPLE_HEAPS
        if (!full_p)
        {
            // we used r_join and need to reinitialize states for it here.
            gc_t_join.r_init();
        }

        dprintf(3, ("Starting all gc thread for short weak handle scan"));
        gc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

#ifdef FEATURE_CARD_MARKING_STEALING
    reset_card_marking_enumerators();

    if (!full_p)
    {
        int generation_skip_ratio_soh = ((n_eph_soh > MIN_SOH_CROSS_GEN_REFS) ?
                                         (int)(((float)n_gen_soh / (float)n_eph_soh) * 100) : 100);
        int generation_skip_ratio_loh = ((n_eph_loh > MIN_LOH_CROSS_GEN_REFS) ?
                                         (int)(((float)n_gen_loh / (float)n_eph_loh) * 100) : 100);

        generation_skip_ratio = min (generation_skip_ratio_soh, generation_skip_ratio_loh);
#ifdef SIMPLE_DPRINTF
        dprintf (6666, ("h%d skip ratio soh: %d (n_gen_soh: %Id, n_eph_soh: %Id), loh: %d (n_gen_loh: %Id, n_eph_loh: %Id), size 0: %Id-%Id, 1: %Id-%Id, 2: %Id-%Id, 3: %Id-%Id",
            heap_number,
            generation_skip_ratio_soh, VolatileLoadWithoutBarrier (&n_gen_soh), VolatileLoadWithoutBarrier (&n_eph_soh),
            generation_skip_ratio_loh, VolatileLoadWithoutBarrier (&n_gen_loh), VolatileLoadWithoutBarrier (&n_eph_loh),
            generation_size (0), dd_fragmentation (dynamic_data_of (0)),
            generation_size (1), dd_fragmentation (dynamic_data_of (1)),
            generation_size (2), dd_fragmentation (dynamic_data_of (2)),
            generation_size (3), dd_fragmentation (dynamic_data_of (3))));
#endif //SIMPLE_DPRINTF
    }
#endif // FEATURE_CARD_MARKING_STEALING

    // null out the target of short weakref that were not promoted.
    GCScan::GcShortWeakPtrScan (condemned_gen_number, max_generation,&sc);

#ifdef MULTIPLE_HEAPS
    dprintf(3, ("Joining for finalization"));
    gc_t_join.join(this, gc_join_scan_finalization);
    if (gc_t_join.joined())
    {
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_EVENT_TRACE
        record_mark_time (gc_time_info[time_mark_short_weak], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

#ifdef MULTIPLE_HEAPS
        dprintf(3, ("Starting all gc thread for Finalization"));
        gc_t_join.restart();
    }
#endif //MULTIPLE_HEAPS

    //Handle finalization.
    size_t promoted_bytes_live = get_promoted_bytes();

#ifdef FEATURE_PREMORTEM_FINALIZATION
    dprintf (3, ("Finalize marking"));
    finalize_queue->ScanForFinalization (GCHeap::Promote, condemned_gen_number, __this);
    drain_mark_queue();
    fire_mark_event (ETW::GC_ROOT_NEW_FQ, current_promoted_bytes, last_promoted_bytes);
    GCToEEInterface::DiagWalkFReachableObjects(__this);

    // Scan dependent handles again to promote any secondaries associated with primaries that were promoted
    // for finalization. As before scan_dependent_handles will also process any mark stack overflow.
    scan_dependent_handles(condemned_gen_number, &sc, false);

    // mark queue must be empty after scan_dependent_handles
    mark_queue.verify_empty();
    fire_mark_event (ETW::GC_ROOT_DH_HANDLES, current_promoted_bytes, last_promoted_bytes);
#endif //FEATURE_PREMORTEM_FINALIZATION

    total_promoted_bytes = get_promoted_bytes();

#ifdef MULTIPLE_HEAPS
    static VOLATILE(int32_t) syncblock_scan_p;
    dprintf(3, ("Joining for weak pointer deletion"));
    gc_t_join.join(this, gc_join_null_dead_long_weak);
    if (gc_t_join.joined())
    {
        dprintf(3, ("Starting all gc thread for weak pointer deletion"));
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_EVENT_TRACE
        record_mark_time (gc_time_info[time_mark_scan_finalization], current_mark_time, last_mark_time);
#endif //FEATURE_EVENT_TRACE

#ifdef USE_REGIONS
        sync_promoted_bytes();
        equalize_promoted_bytes(settings.condemned_generation);
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
        syncblock_scan_p = 0;
        gc_t_join.restart();
    }
#endif //MULTIPLE_HEAPS

    // null out the target of long weakref that were not promoted.
    GCScan::GcWeakPtrScan (condemned_gen_number, max_generation, &sc);

#ifdef MULTIPLE_HEAPS
    size_t total_mark_list_size = sort_mark_list();
    // first thread to finish sorting will scan the sync syncblk cache
    if ((syncblock_scan_p == 0) && (Interlocked::Increment(&syncblock_scan_p) == 1))
#endif //MULTIPLE_HEAPS
    {
        // scan for deleted entries in the syncblk cache
        GCScan::GcWeakPtrScanBySingleThread(condemned_gen_number, max_generation, &sc);
    }

#ifdef MULTIPLE_HEAPS
    dprintf (3, ("Joining for sync block cache entry scanning"));
    gc_t_join.join(this, gc_join_null_dead_syncblk);
    if (gc_t_join.joined())
#endif //MULTIPLE_HEAPS
    {
#ifdef FEATURE_EVENT_TRACE
        record_mark_time (gc_time_info[time_mark_long_weak], current_mark_time, last_mark_time);
        gc_time_info[time_plan] = last_mark_time;
#endif //FEATURE_EVENT_TRACE

        //decide on promotion
        if (!settings.promotion)
        {
            size_t m = 0;
            for (int n = 0; n <= condemned_gen_number;n++)
            {
#ifdef MULTIPLE_HEAPS
                m +=  (size_t)(dd_min_size (dynamic_data_of (n))*(n+1)*0.1);
#else
                m +=  (size_t)(dd_min_size (dynamic_data_of (n))*(n+1)*0.06);
#endif //MULTIPLE_HEAPS
            }

            settings.promotion = decide_on_promotion_surv (m);
        }

#ifdef MULTIPLE_HEAPS
#ifdef SNOOP_STATS
        if (do_mark_steal_p)
        {
            size_t objects_checked_count = 0;
            size_t zero_ref_count = 0;
            size_t objects_marked_count = 0;
            size_t check_level_count = 0;
            size_t busy_count = 0;
            size_t interlocked_count = 0;
            size_t partial_mark_parent_count = 0;
            size_t stolen_or_pm_count = 0;
            size_t stolen_entry_count = 0;
            size_t pm_not_ready_count = 0;
            size_t normal_count = 0;
            size_t stack_bottom_clear_count = 0;

            for (int i = 0; i < n_heaps; i++)
            {
                gc_heap* hp = g_heaps[i];
                hp->print_snoop_stat();
                objects_checked_count += hp->snoop_stat.objects_checked_count;
                zero_ref_count += hp->snoop_stat.zero_ref_count;
                objects_marked_count += hp->snoop_stat.objects_marked_count;
                check_level_count += hp->snoop_stat.check_level_count;
                busy_count += hp->snoop_stat.busy_count;
                interlocked_count += hp->snoop_stat.interlocked_count;
                partial_mark_parent_count += hp->snoop_stat.partial_mark_parent_count;
                stolen_or_pm_count += hp->snoop_stat.stolen_or_pm_count;
                stolen_entry_count += hp->snoop_stat.stolen_entry_count;
                pm_not_ready_count += hp->snoop_stat.pm_not_ready_count;
                normal_count += hp->snoop_stat.normal_count;
                stack_bottom_clear_count += hp->snoop_stat.stack_bottom_clear_count;
            }

            fflush (stdout);

            printf ("-------total stats-------\n");
            printf ("%8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s | %8s\n",
                "checked", "zero", "marked", "level", "busy", "xchg", "pmparent", "s_pm", "stolen", "nready", "normal", "clear");
            printf ("%8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d | %8d\n",
                objects_checked_count,
                zero_ref_count,
                objects_marked_count,
                check_level_count,
                busy_count,
                interlocked_count,
                partial_mark_parent_count,
                stolen_or_pm_count,
                stolen_entry_count,
                pm_not_ready_count,
                normal_count,
                stack_bottom_clear_count);
        }
#endif //SNOOP_STATS

        dprintf(3, ("Starting all threads for end of mark phase"));
        gc_t_join.restart();
#endif //MULTIPLE_HEAPS
    }

#if defined(MULTIPLE_HEAPS) && !defined(USE_REGIONS)
    merge_mark_lists (total_mark_list_size);
#endif //MULTIPLE_HEAPS && !USE_REGIONS

    finalization_promoted_bytes = total_promoted_bytes - promoted_bytes_live;

    mark_queue.verify_empty();

    dprintf(2,("---- End of mark phase ----"));
}

inline
void gc_heap::pin_object (uint8_t* o, uint8_t** ppObject)
{
    dprintf (3, ("Pinning %zx->%zx", (size_t)ppObject, (size_t)o));
    set_pinned (o);

#ifdef FEATURE_EVENT_TRACE
    if(EVENT_ENABLED(PinObjectAtGCTime))
    {
        fire_etw_pin_object_event(o, ppObject);
    }
#endif // FEATURE_EVENT_TRACE

    num_pinned_objects++;
}

size_t gc_heap::get_total_pinned_objects()
{
#ifdef MULTIPLE_HEAPS
    size_t total_num_pinned_objects = 0;
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        total_num_pinned_objects += hp->num_pinned_objects;
    }
    return total_num_pinned_objects;
#else //MULTIPLE_HEAPS
    return num_pinned_objects;
#endif //MULTIPLE_HEAPS
}

void gc_heap::reinit_pinned_objects()
{
#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap::g_heaps[i]->num_pinned_objects = 0;
    }
#else //MULTIPLE_HEAPS
    num_pinned_objects = 0;
#endif //MULTIPLE_HEAPS
}

void gc_heap::reset_mark_stack ()
{
    reset_pinned_queue();
    max_overflow_address = 0;
    min_overflow_address = MAX_PTR;
}

#ifdef FEATURE_BASICFREEZE
inline
void gc_heap::seg_set_mark_bits (heap_segment* seg)
{
    uint8_t* o = heap_segment_mem (seg);
    while (o < heap_segment_allocated (seg))
    {
        set_marked (o);
        o = o + Align (size(o));
    }
}

// We have to do this for in range ro segments because these objects' life time isn't accurately
// expressed. The expectation is all objects on ro segs are live. So we just artifically mark
// all of them on the in range ro segs.
void gc_heap::mark_ro_segments()
{
#ifndef USE_REGIONS
    if ((settings.condemned_generation == max_generation) && ro_segments_in_range)
    {
        heap_segment* seg = generation_start_segment (generation_of (max_generation));

        while (seg)
        {
            if (!heap_segment_read_only_p (seg))
                break;

            if (heap_segment_in_range_p (seg))
            {
#ifdef BACKGROUND_GC
                if (settings.concurrent)
                {
                    seg_set_mark_array_bits_soh (seg);
                }
                else
#endif //BACKGROUND_GC
                {
                    seg_set_mark_bits (seg);
                }
            }
            seg = heap_segment_next (seg);
        }
    }
#endif //!USE_REGIONS
}

#endif //FEATURE_BASICFREEZE
#ifdef USE_REGIONS
void gc_heap::grow_mark_list_piece()
{
    if (g_mark_list_piece_total_size < region_count * 2 * get_num_heaps())
    {
        delete[] g_mark_list_piece;

        // at least double the size
        size_t alloc_count = max ((g_mark_list_piece_size * 2), region_count);

        // we need two arrays with alloc_count entries per heap
        g_mark_list_piece = new (nothrow) uint8_t * *[alloc_count * 2 * get_num_heaps()];
        if (g_mark_list_piece != nullptr)
        {
            g_mark_list_piece_size = alloc_count;
        }
        else
        {
            g_mark_list_piece_size = 0;
        }
        g_mark_list_piece_total_size = g_mark_list_piece_size * 2 * get_num_heaps();
    }
    // update the size per heap in case the number of heaps has changed,
    // but the total size is still sufficient
    g_mark_list_piece_size = g_mark_list_piece_total_size / (2 * get_num_heaps());
}

#endif //USE_REGIONS

// For regions -
// n_gen means it's pointing into the condemned regions so it's incremented
// if the child object's region is <= condemned_gen.
// cg_pointers_found means it's pointing into a lower generation so it's incremented
// if the child object's region is < current_gen.
inline void
gc_heap::mark_through_cards_helper (uint8_t** poo, size_t& n_gen,
                                    size_t& cg_pointers_found,
                                    card_fn fn, uint8_t* nhigh,
                                    uint8_t* next_boundary,
                                    int condemned_gen,
                                    // generation of the parent object
                                    int current_gen
                                    CARD_MARKING_STEALING_ARG(gc_heap* hpt))
{
#if defined(FEATURE_CARD_MARKING_STEALING) && defined(MULTIPLE_HEAPS)
    int thread = hpt->heap_number;
#else
    THREAD_FROM_HEAP;
#ifdef MULTIPLE_HEAPS
    gc_heap* hpt = this;
#endif //MULTIPLE_HEAPS
#endif //FEATURE_CARD_MARKING_STEALING && MULTIPLE_HEAPS

#ifdef USE_REGIONS
    assert (nhigh == 0);
    assert (next_boundary == 0);
    uint8_t* child_object = *poo;
    if ((child_object < ephemeral_low) || (ephemeral_high <= child_object))
        return;

    int child_object_gen = get_region_gen_num (child_object);
    int saved_child_object_gen = child_object_gen;
    uint8_t* saved_child_object = child_object;

    if (child_object_gen <= condemned_gen)
    {
        n_gen++;
        call_fn(hpt,fn) (poo THREAD_NUMBER_ARG);
    }

    if (fn == &gc_heap::relocate_address)
    {
        child_object_gen = get_region_plan_gen_num (*poo);
    }

    if (child_object_gen < current_gen)
    {
        cg_pointers_found++;
        dprintf (4, ("cg pointer %zx found, %zd so far",
                        (size_t)*poo, cg_pointers_found ));
    }
#else //USE_REGIONS
    assert (condemned_gen == -1);
    if ((gc_low <= *poo) && (gc_high > *poo))
    {
        n_gen++;
        call_fn(hpt,fn) (poo THREAD_NUMBER_ARG);
    }
#ifdef MULTIPLE_HEAPS
    else if (*poo)
    {
        gc_heap* hp = heap_of_gc (*poo);
        if (hp != this)
        {
            if ((hp->gc_low <= *poo) &&
                (hp->gc_high > *poo))
            {
                n_gen++;
                call_fn(hpt,fn) (poo THREAD_NUMBER_ARG);
            }
            if ((fn == &gc_heap::relocate_address) ||
                ((hp->ephemeral_low <= *poo) &&
                 (hp->ephemeral_high > *poo)))
            {
                cg_pointers_found++;
            }
        }
    }
#endif //MULTIPLE_HEAPS
    if ((next_boundary <= *poo) && (nhigh > *poo))
    {
        cg_pointers_found ++;
        dprintf (4, ("cg pointer %zx found, %zd so far",
                     (size_t)*poo, cg_pointers_found ));
    }
#endif //USE_REGIONS
}

void gc_heap::mark_through_cards_for_segments (card_fn fn, BOOL relocating CARD_MARKING_STEALING_ARG(gc_heap* hpt))
{
#ifdef BACKGROUND_GC
#ifdef USE_REGIONS
    dprintf (3, ("current_sweep_pos is %p", current_sweep_pos));
#else
    dprintf (3, ("current_sweep_pos is %p, saved_sweep_ephemeral_seg is %p(%p)",
                 current_sweep_pos, saved_sweep_ephemeral_seg, saved_sweep_ephemeral_start));
#endif //USE_REGIONS
    for (int i = get_start_generation_index(); i < max_generation; i++)
    {
        heap_segment* soh_seg = heap_segment_rw (generation_start_segment (generation_of (i)));
        _ASSERTE(soh_seg != NULL);

        while (soh_seg)
        {
            dprintf (3, ("seg %p, bgc_alloc: %p, alloc: %p",
                soh_seg,
                heap_segment_background_allocated (soh_seg),
                heap_segment_allocated (soh_seg)));

            soh_seg = heap_segment_next_rw (soh_seg);
        }
    }
#endif //BACKGROUND_GC

    size_t end_card = 0;

    generation*   oldest_gen        = generation_of (max_generation);
    int           curr_gen_number   = max_generation;
    // Note - condemned_gen is only needed for regions and the other 2 are
    // only for if USE_REGIONS is not defined, but I need to pass them to a
    // function inside the macro below so just assert they are the unused values.
#ifdef USE_REGIONS
    uint8_t* low = 0;
    uint8_t*      gen_boundary      = 0;
    uint8_t*      next_boundary     = 0;
    int condemned_gen               = settings.condemned_generation;
    uint8_t*      nhigh             = 0;
#else
    uint8_t* low = gc_low;
    uint8_t* high = gc_high;
    uint8_t*      gen_boundary      = generation_allocation_start(generation_of(curr_gen_number - 1));
    uint8_t*      next_boundary     = compute_next_boundary(curr_gen_number, relocating);
    int condemned_gen = -1;
    uint8_t*      nhigh             = (relocating ?
                                       heap_segment_plan_allocated (ephemeral_heap_segment) : high);
#endif //USE_REGIONS
    heap_segment* seg               = heap_segment_rw (generation_start_segment (oldest_gen));
    _ASSERTE(seg != NULL);

    uint8_t*      beg               = get_soh_start_object (seg, oldest_gen);
    uint8_t*      end               = compute_next_end (seg, low);
    uint8_t*      last_object       = beg;

    size_t  cg_pointers_found = 0;

    size_t  card_word_end = (card_of (align_on_card_word (end)) / card_word_width);

    size_t        n_eph             = 0;
    size_t        n_gen             = 0;
    size_t        n_card_set        = 0;

    BOOL          foundp            = FALSE;
    uint8_t*      start_address     = 0;
    uint8_t*      limit             = 0;
    size_t        card              = card_of (beg);
#ifdef BACKGROUND_GC
    BOOL consider_bgc_mark_p        = FALSE;
    BOOL check_current_sweep_p      = FALSE;
    BOOL check_saved_sweep_p        = FALSE;
    should_check_bgc_mark (seg, &consider_bgc_mark_p, &check_current_sweep_p, &check_saved_sweep_p);
#endif //BACKGROUND_GC

    dprintf(3, ("CMs: %zx->%zx", (size_t)beg, (size_t)end));
    size_t total_cards_cleared = 0;

#ifdef FEATURE_CARD_MARKING_STEALING
    card_marking_enumerator card_mark_enumerator (seg, low, (VOLATILE(uint32_t)*)&card_mark_chunk_index_soh);
    card_word_end = 0;
#endif // FEATURE_CARD_MARKING_STEALING

    while (1)
    {
        if (card_of(last_object) > card)
        {
            dprintf (3, ("Found %zd cg pointers", cg_pointers_found));
            if (cg_pointers_found == 0)
            {
                uint8_t* last_object_processed = last_object;
#ifdef FEATURE_CARD_MARKING_STEALING
                last_object_processed = min(limit, last_object);
#endif // FEATURE_CARD_MARKING_STEALING
                dprintf (3, (" Clearing cards [%zx, %zx[ ", (size_t)card_address(card), (size_t)last_object_processed));

                size_t card_last_obj = card_of (last_object_processed);
                clear_cards(card, card_last_obj);

                // We need to be careful of the accounting here because we could be in the situation where there are more set cards between end of
                // last set card batch and last_object_processed. We will be clearing all of them. But we can't count the set cards we haven't
                // discovered yet or we can get a negative number for n_card_set. However, if last_object_processed lands before what end_card
                // corresponds to, we can't count the whole batch because it will be handled by a later clear_cards.
                size_t cards_to_deduct = (card_last_obj < end_card) ? (card_last_obj - card) : (end_card - card);
                n_card_set -= cards_to_deduct;
                total_cards_cleared += cards_to_deduct;
            }

            n_eph += cg_pointers_found;
            cg_pointers_found = 0;
            card = card_of (last_object);
        }

        if (card >= end_card)
        {
#ifdef FEATURE_CARD_MARKING_STEALING
            // find another chunk with some cards set
            foundp = find_next_chunk(card_mark_enumerator, seg, n_card_set, start_address, limit, card, end_card, card_word_end);
#else // FEATURE_CARD_MARKING_STEALING
            foundp = find_card(card_table, card, card_word_end, end_card);
            if (foundp)
            {
                n_card_set += end_card - card;
                start_address = max (beg, card_address (card));
            }
            limit = min (end, card_address (end_card));
#endif // FEATURE_CARD_MARKING_STEALING
        }
        if (!foundp || (last_object >= end) || (card_address (card) >= end))
        {
            if (foundp && (cg_pointers_found == 0))
            {
#ifndef USE_REGIONS
                // in the segment case, need to recompute end_card so we don't clear cards
                // for the next generation
                end_card = card_of (end);
#endif
                dprintf(3,(" Clearing cards [%zx, %zx[ ", (size_t)card_address(card),
                            (size_t)card_address(end_card)));
                clear_cards (card, end_card);
                n_card_set -= (end_card - card);
                total_cards_cleared += (end_card - card);
            }
            n_eph += cg_pointers_found;
            cg_pointers_found = 0;
#ifdef FEATURE_CARD_MARKING_STEALING
            // we have decided to move to the next segment - make sure we exhaust the chunk enumerator for this segment
            card_mark_enumerator.exhaust_segment(seg);
#endif // FEATURE_CARD_MARKING_STEALING

            seg = heap_segment_next_in_range (seg);
#ifdef USE_REGIONS
            if (!seg)
            {
                curr_gen_number--;
                if (curr_gen_number > condemned_gen)
                {
                    // Switch to regions for this generation.
                    seg = generation_start_segment (generation_of (curr_gen_number));
#ifdef FEATURE_CARD_MARKING_STEALING
                    card_mark_enumerator.switch_to_segment(seg);
#endif // FEATURE_CARD_MARKING_STEALING
                    dprintf (REGIONS_LOG, ("h%d switching to gen%d start seg %zx",
                        heap_number, curr_gen_number, (size_t)seg));
                }
            }
#endif //USE_REGIONS

            if (seg)
            {
#ifdef BACKGROUND_GC
                should_check_bgc_mark (seg, &consider_bgc_mark_p, &check_current_sweep_p, &check_saved_sweep_p);
#endif //BACKGROUND_GC
                beg = heap_segment_mem (seg);
#ifdef USE_REGIONS
                end = heap_segment_allocated (seg);
#else
                end = compute_next_end (seg, low);
#endif //USE_REGIONS
#ifdef FEATURE_CARD_MARKING_STEALING
                card_word_end = 0;
#else // FEATURE_CARD_MARKING_STEALING
                card_word_end = card_of (align_on_card_word (end)) / card_word_width;
#endif // FEATURE_CARD_MARKING_STEALING
                card = card_of (beg);
                last_object = beg;
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
            uint8_t* o = last_object;

            o = find_first_object (start_address, last_object);
            // Never visit an object twice.
            assert (o >= last_object);

#ifndef USE_REGIONS
            //dprintf(3,("Considering card %zx start object: %zx, %zx[ boundary: %zx",
            dprintf(3, ("c: %zx, o: %zx, l: %zx[ boundary: %zx",
                   card, (size_t)o, (size_t)limit, (size_t)gen_boundary));
#endif //USE_REGIONS

            while (o < limit)
            {
                assert (Align (size (o)) >= Align (min_obj_size));
                size_t s = size (o);

                // next_o is the next object in the heap walk
                uint8_t* next_o =  o + Align (s);

                // while cont_o is the object we should continue with at the end_object label
                uint8_t* cont_o = next_o;

                Prefetch (next_o);

#ifndef USE_REGIONS
                if ((o >= gen_boundary) &&
                    (seg == ephemeral_heap_segment))
                {
                    dprintf (3, ("switching gen boundary %zx", (size_t)gen_boundary));
                    curr_gen_number--;
                    assert ((curr_gen_number > 0));
                    gen_boundary = generation_allocation_start
                        (generation_of (curr_gen_number - 1));
                    next_boundary = (compute_next_boundary
                                     (curr_gen_number, relocating));
                }
#endif //!USE_REGIONS

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
                                                       condemned_gen, curr_gen_number CARD_MARKING_STEALING_ARG(hpt));
                        }
                    }

                    if (passed_end_card_p)
                    {
                        if (foundp && (card_address (card) < next_o))
                        {
                            goto go_through_refs;
                        }
                        else if (foundp && (start_address < limit))
                        {
                            cont_o = find_first_object (start_address, o);
                            goto end_object;
                        }
                        else
                            goto end_limit;
                    }
                }

go_through_refs:
#endif //COLLECTIBLE_CLASS

                if (contain_pointers (o))
                {
                    dprintf(3,("Going through %zx start_address: %zx", (size_t)o, (size_t)start_address));

                    {
                        dprintf (4, ("normal object path"));
                        go_through_object
                            (method_table(o), o, s, poo,
                             start_address, use_start, (o + s),
                             {
                                 dprintf (4, ("<%zx>:%zx", (size_t)poo, (size_t)*poo));
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
                                         else if (foundp && (start_address < limit))
                                         {
                                             cont_o = find_first_object (start_address, o);
                                             goto end_object;
                                         }
                                         else
                                             goto end_limit;
                                     }
                                 }

                                 mark_through_cards_helper (poo, n_gen,
                                                            cg_pointers_found, fn,
                                                            nhigh, next_boundary,
                                                            condemned_gen, curr_gen_number CARD_MARKING_STEALING_ARG(hpt));
                             }
                            );
                    }
                }

            end_object:
                if (((size_t)next_o / brick_size) != ((size_t) o / brick_size))
                {
                    if (brick_table [brick_of (o)] <0)
                        fix_brick_to_highest (o, next_o);
                }
                o = cont_o;
            }
        end_limit:
            last_object = o;
        }
    }
    // compute the efficiency ratio of the card table
    if (!relocating)
    {
#ifdef FEATURE_CARD_MARKING_STEALING
        Interlocked::ExchangeAddPtr(&n_eph_soh, n_eph);
        Interlocked::ExchangeAddPtr(&n_gen_soh, n_gen);
        dprintf (3, ("h%d marking h%d Msoh: cross: %zd, useful: %zd, cards set: %zd, cards cleared: %zd, ratio: %d",
            hpt->heap_number, heap_number, n_eph, n_gen, n_card_set, total_cards_cleared,
            (n_eph ? (int)(((float)n_gen / (float)n_eph) * 100) : 0)));
        dprintf (3, ("h%d marking h%d Msoh: total cross %zd, useful: %zd, running ratio: %d",
            hpt->heap_number, heap_number, (size_t)n_eph_soh, (size_t)n_gen_soh,
            (n_eph_soh ? (int)(((float)n_gen_soh / (float)n_eph_soh) * 100) : 0)));
#else
        generation_skip_ratio = ((n_eph > MIN_SOH_CROSS_GEN_REFS) ? (int)(((float)n_gen / (float)n_eph) * 100) : 100);
        dprintf (3, ("marking h%d Msoh: cross: %zd, useful: %zd, cards set: %zd, cards cleared: %zd, ratio: %d",
            heap_number, n_eph, n_gen, n_card_set, total_cards_cleared, generation_skip_ratio));
#endif //FEATURE_CARD_MARKING_STEALING
    }
    else
    {
        dprintf (3, ("R: Msoh: cross: %zd, useful: %zd, cards set: %zd, cards cleared: %zd, ratio: %d",
            n_gen, n_eph, n_card_set, total_cards_cleared, generation_skip_ratio));
    }
}
