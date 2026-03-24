// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifdef USE_REGIONS
region_free_list::region_free_list() : num_free_regions (0),
                                       size_free_regions (0),
                                       size_committed_in_free_regions (0),
                                       num_free_regions_added (0),
                                       num_free_regions_removed (0),
                                       head_free_region (nullptr),
                                       tail_free_region (nullptr)
{
}

void region_free_list::verify (bool empty_p)
{
#ifdef _DEBUG
    assert ((num_free_regions == 0) == empty_p);
    assert ((size_free_regions == 0) == empty_p);
    assert ((size_committed_in_free_regions == 0) == empty_p);
    assert ((head_free_region == nullptr) == empty_p);
    assert ((tail_free_region == nullptr) == empty_p);
    assert (num_free_regions == (num_free_regions_added - num_free_regions_removed));

    if (!empty_p)
    {
        assert (heap_segment_next (tail_free_region) == nullptr);
        assert (heap_segment_prev_free_region (head_free_region) == nullptr);

        size_t actual_count = 0;
        heap_segment* last_region = nullptr;
        for (heap_segment* region = head_free_region; region != nullptr; region = heap_segment_next(region))
        {
            last_region = region;
            actual_count++;
        }
        assert (num_free_regions == actual_count);
        assert (last_region == tail_free_region);
        heap_segment* first_region = nullptr;
        for (heap_segment* region = tail_free_region; region != nullptr; region = heap_segment_prev_free_region(region))
        {
            first_region = region;
            actual_count--;
        }
        assert (actual_count == 0);
        assert (head_free_region == first_region);
    }
#endif
}

void region_free_list::reset()
{
    num_free_regions = 0;
    size_free_regions = 0;
    size_committed_in_free_regions = 0;

    head_free_region = nullptr;
    tail_free_region = nullptr;
}

inline
void region_free_list::update_added_region_info (heap_segment* region)
{
    num_free_regions++;
    num_free_regions_added++;

    size_t region_size = get_region_size (region);
    size_free_regions += region_size;

    size_t region_committed_size = get_region_committed_size (region);
    size_committed_in_free_regions += region_committed_size;

    verify (false);
}

void region_free_list::add_region_front (heap_segment* region)
{
    assert (heap_segment_containing_free_list (region) == nullptr);
    heap_segment_containing_free_list(region) = this;
    if (head_free_region != nullptr)
    {
        heap_segment_prev_free_region(head_free_region) = region;
        assert (tail_free_region != nullptr);
    }
    else
    {
        tail_free_region = region;
    }
    heap_segment_next (region) = head_free_region;
    head_free_region = region;
    heap_segment_prev_free_region (region) = nullptr;

    update_added_region_info (region);
}

// This inserts fully committed regions at the head, otherwise it goes backward in the list till
// we find a region whose committed size is >= this region's committed or we reach the head.
void region_free_list::add_region_in_descending_order (heap_segment* region_to_add)
{
    assert (heap_segment_containing_free_list (region_to_add) == nullptr);
    heap_segment_containing_free_list (region_to_add) = this;
    heap_segment_age_in_free (region_to_add) = 0;
    heap_segment* prev_region = nullptr;
    heap_segment* region = nullptr;

    // if the region is fully committed, it's inserted at the front
    if (heap_segment_committed (region_to_add) == heap_segment_reserved (region_to_add))
    {
        region = head_free_region;
    }
    else
    {
        // otherwise we search backwards for a good insertion spot
        // most regions at the front are fully committed and thus boring to search

        size_t region_to_add_committed = get_region_committed_size (region_to_add);

        for (prev_region = tail_free_region; prev_region != nullptr; prev_region = heap_segment_prev_free_region (prev_region))
        {
            size_t prev_region_committed = get_region_committed_size (prev_region);

            if (prev_region_committed >= region_to_add_committed)
            {
                break;
            }
            region = prev_region;
        }
    }

    if (prev_region != nullptr)
    {
        heap_segment_next (prev_region) = region_to_add;
    }
    else
    {
        assert (region == head_free_region);
        head_free_region = region_to_add;
    }

    heap_segment_prev_free_region (region_to_add) = prev_region;
    heap_segment_next (region_to_add) = region;

    if (region != nullptr)
    {
        heap_segment_prev_free_region (region) = region_to_add;
    }
    else
    {
        assert (prev_region == tail_free_region);
        tail_free_region = region_to_add;
    }

    update_added_region_info (region_to_add);
}

heap_segment* region_free_list::unlink_region_front()
{
    heap_segment* region = head_free_region;
    if (region != nullptr)
    {
        assert (heap_segment_containing_free_list (region) == this);
        unlink_region (region);
    }
    return region;
}

void region_free_list::unlink_region (heap_segment* region)
{
    region_free_list* rfl = heap_segment_containing_free_list (region);
    rfl->verify (false);

    heap_segment* prev = heap_segment_prev_free_region (region);
    heap_segment* next = heap_segment_next (region);

    if (prev != nullptr)
    {
        assert (region != rfl->head_free_region);
        assert (heap_segment_next (prev) == region);
        heap_segment_next (prev) = next;
    }
    else
    {
        assert (region == rfl->head_free_region);
        rfl->head_free_region = next;
    }

    if (next != nullptr)
    {
        assert (region != rfl->tail_free_region);
        assert (heap_segment_prev_free_region (next) == region);
        heap_segment_prev_free_region (next) = prev;
    }
    else
    {
        assert (region == rfl->tail_free_region);
        rfl->tail_free_region = prev;
    }
    heap_segment_containing_free_list (region) = nullptr;

    rfl->num_free_regions--;
    rfl->num_free_regions_removed++;

    size_t region_size = get_region_size (region);
    assert (rfl->size_free_regions >= region_size);
    rfl->size_free_regions -= region_size;

    size_t region_committed_size = get_region_committed_size (region);
    assert (rfl->size_committed_in_free_regions >= region_committed_size);
    rfl->size_committed_in_free_regions -= region_committed_size;
}

free_region_kind region_free_list::get_region_kind (heap_segment* region)
{
    const size_t BASIC_REGION_SIZE = global_region_allocator.get_region_alignment();
    const size_t LARGE_REGION_SIZE = global_region_allocator.get_large_region_alignment();
    size_t region_size = get_region_size (region);

    if (region_size == BASIC_REGION_SIZE)
        return basic_free_region;
    else if (region_size == LARGE_REGION_SIZE)
        return large_free_region;
    else
    {
        assert(region_size > LARGE_REGION_SIZE);
        return huge_free_region;
    }
}

heap_segment* region_free_list::unlink_smallest_region (size_t minimum_size)
{
    verify (num_free_regions == 0);

    // look for the smallest region that is large enough
    heap_segment* smallest_region = nullptr;
    size_t smallest_size = (size_t)-1;
    for (heap_segment* region = head_free_region; region != nullptr; region = heap_segment_next (region))
    {
        uint8_t* region_start = get_region_start(region);
        uint8_t* region_end = heap_segment_reserved(region);

        size_t region_size = get_region_size (region);
        const size_t LARGE_REGION_SIZE = global_region_allocator.get_large_region_alignment();
        assert (region_size >= LARGE_REGION_SIZE * 2);
        if (region_size >= minimum_size)
        {
            // found a region that is large enough - see if it's smaller than the smallest so far
            if (smallest_size > region_size)
            {
                smallest_size = region_size;
                smallest_region = region;
            }
            // is the region's size equal to the minimum on this list?
            if (region_size == LARGE_REGION_SIZE * 2)
            {
                // we won't find a smaller one on this list
                assert (region == smallest_region);
                break;
            }
        }
    }

    if (smallest_region != nullptr)
    {
        unlink_region (smallest_region);
        dprintf(REGIONS_LOG, ("get %p-%p-%p",
            heap_segment_mem(smallest_region), heap_segment_committed(smallest_region), heap_segment_used(smallest_region)));
    }

    return smallest_region;
}

void region_free_list::transfer_regions (region_free_list* from)
{
    this->verify (this->num_free_regions == 0);
    from->verify (from->num_free_regions == 0);

    if (from->num_free_regions == 0)
    {
        // the from list is empty
        return;
    }

    if (num_free_regions == 0)
    {
        // this list is empty
        head_free_region = from->head_free_region;
        tail_free_region = from->tail_free_region;
    }
    else
    {
        // both free lists are non-empty
        // attach the from list at the tail
        heap_segment* this_tail = tail_free_region;
        heap_segment* from_head = from->head_free_region;

        heap_segment_next (this_tail) = from_head;
        heap_segment_prev_free_region (from_head) = this_tail;

        tail_free_region = from->tail_free_region;

    }

    for (heap_segment* region = from->head_free_region; region != nullptr; region = heap_segment_next (region))
    {
        heap_segment_containing_free_list (region) = this;
    }

    num_free_regions += from->num_free_regions;
    num_free_regions_added += from->num_free_regions;
    size_free_regions += from->size_free_regions;
    size_committed_in_free_regions += from->size_committed_in_free_regions;

    from->num_free_regions_removed += from->num_free_regions;
    from->reset();

    verify (false);
}

size_t region_free_list::get_num_free_regions()
{
#ifdef _DEBUG
    verify (num_free_regions == 0);
#endif //_DEBUG
    return num_free_regions;
}

void region_free_list::add_region (heap_segment* region, region_free_list to_free_list[count_free_region_kinds])
{
    free_region_kind kind = get_region_kind (region);
    to_free_list[kind].add_region_front (region);
}

void region_free_list::add_region_descending (heap_segment* region, region_free_list to_free_list[count_free_region_kinds])
{
    free_region_kind kind = get_region_kind (region);
    to_free_list[kind].add_region_in_descending_order (region);
}

bool region_free_list::is_on_free_list (heap_segment* region, region_free_list free_list[count_free_region_kinds])
{
    region_free_list* rfl = heap_segment_containing_free_list (region);
    free_region_kind kind = get_region_kind (region);
    return rfl == &free_list[kind];
}

void region_free_list::age_free_regions()
{
    for (heap_segment* region = head_free_region; region != nullptr; region = heap_segment_next (region))
    {
        // only age to 99... that's enough for us to decommit this.
        if (heap_segment_age_in_free (region) < MAX_AGE_IN_FREE)
            heap_segment_age_in_free (region)++;
    }
}

void region_free_list::age_free_regions (region_free_list free_lists[count_free_region_kinds])
{
    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        free_lists[kind].age_free_regions();
    }
}

void region_free_list::print (int hn, const char* msg, int* ages)
{
    dprintf (3, ("h%2d PRINTING-------------------------------", hn));
    for (heap_segment* region = head_free_region; region != nullptr; region = heap_segment_next (region))
    {
        if (ages)
        {
            ages[heap_segment_age_in_free (region)]++;
        }

        dprintf (3, ("[%s] h%2d age %d region %p (%zd)%s",
            msg, hn, (int)heap_segment_age_in_free (region),
            heap_segment_mem (region), get_region_committed_size (region),
            ((heap_segment_committed (region) == heap_segment_reserved (region)) ? "(FC)" : "")));
    }
    dprintf (3, ("h%2d PRINTING END-------------------------------", hn));
}

void region_free_list::print (region_free_list free_lists[count_free_region_kinds], int hn, const char* msg, int* ages)
{
    for (int kind = basic_free_region; kind < count_free_region_kinds; kind++)
    {
        free_lists[kind].print (hn, msg, ages);
    }
}

static int compare_by_committed_and_age (heap_segment* l, heap_segment* r)
{
    size_t l_committed = get_region_committed_size (l);
    size_t r_committed = get_region_committed_size (r);
    if (l_committed > r_committed)
        return -1;
    else if (l_committed < r_committed)
        return 1;
    int l_age = heap_segment_age_in_free (l);
    int r_age = heap_segment_age_in_free (r);
    return (l_age - r_age);
}

static heap_segment* merge_sort_by_committed_and_age (heap_segment *head, size_t count)
{
    if (count <= 1)
        return head;
    size_t half = count / 2;
    heap_segment* mid = nullptr;
    size_t i = 0;
    for (heap_segment *region = head; region != nullptr; region = heap_segment_next (region))
    {
        i++;
        if (i == half)
        {
            mid = heap_segment_next (region);
            heap_segment_next (region) = nullptr;
            break;
        }
    }
    head = merge_sort_by_committed_and_age (head, half);
    mid = merge_sort_by_committed_and_age (mid, count - half);

    heap_segment* new_head;
    if (compare_by_committed_and_age (head, mid) <= 0)
    {
        new_head = head;
        head = heap_segment_next (head);
    }
    else
    {
        new_head = mid;
        mid = heap_segment_next (mid);
    }
    heap_segment* new_tail = new_head;
    while ((head != nullptr) && (mid != nullptr))
    {
        heap_segment* region = nullptr;
        if (compare_by_committed_and_age (head, mid) <= 0)
        {
            region = head;
            head = heap_segment_next (head);
        }
        else
        {
            region = mid;
            mid = heap_segment_next (mid);
        }

        heap_segment_next (new_tail) = region;
        new_tail = region;
    }

    if (head != nullptr)
    {
        assert (mid == nullptr);
        heap_segment_next (new_tail) = head;
    }
    else
    {
        heap_segment_next (new_tail) = mid;
    }
    return new_head;
}

void region_free_list::sort_by_committed_and_age()
{
    if (num_free_regions <= 1)
        return;
    heap_segment* new_head = merge_sort_by_committed_and_age (head_free_region, num_free_regions);

    // need to set head, tail, and all the prev links again
    head_free_region = new_head;
    heap_segment* prev = nullptr;
    for (heap_segment* region = new_head; region != nullptr; region = heap_segment_next (region))
    {
        heap_segment_prev_free_region (region) = prev;
        assert ((prev == nullptr) || (compare_by_committed_and_age (prev, region) <= 0));
        prev = region;
    }
    tail_free_region = prev;
}
#endif //USE_REGIONS
