// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


allocator::allocator (unsigned int num_b, int fbb, alloc_list* b, int gen)
{
    assert (num_b < MAX_BUCKET_COUNT);
    num_buckets = num_b;
    first_bucket_bits = fbb;
    buckets = b;
    gen_number = gen;
}

alloc_list& allocator::alloc_list_of (unsigned int bn)
{
    assert (bn < num_buckets);
    if (bn == 0)
        return first_bucket;
    else
        return buckets [bn-1];
}

size_t& allocator::alloc_list_damage_count_of (unsigned int bn)
{
    assert (bn < num_buckets);
    if (bn == 0)
        return first_bucket.alloc_list_damage_count();
    else
        return buckets [bn-1].alloc_list_damage_count();
}

void allocator::unlink_item (unsigned int bn, uint8_t* item, uint8_t* prev_item, BOOL use_undo_p)
{
    alloc_list* al = &alloc_list_of (bn);
    uint8_t* next_item = free_list_slot(item);

#ifdef DOUBLY_LINKED_FL
    // if repair_list is TRUE yet use_undo_p is FALSE, it means we do need to make sure
    // this item does not look like it's on the free list as we will not have a chance to
    // do that later.
    BOOL repair_list = !discard_if_no_fit_p ();
#endif //DOUBLY_LINKED_FL

    if (prev_item)
    {
        if (use_undo_p && (free_list_undo (prev_item) == UNDO_EMPTY))
        {
            assert (item == free_list_slot (prev_item));
            free_list_undo (prev_item) = item;
            alloc_list_damage_count_of (bn)++;
        }

        free_list_slot (prev_item) = next_item;
    }
    else
    {
        al->alloc_list_head() = next_item;
    }
    if (al->alloc_list_tail() == item)
    {
        al->alloc_list_tail() = prev_item;
    }

#ifdef DOUBLY_LINKED_FL
    if (repair_list)
    {
        if (!use_undo_p)
        {
            free_list_prev (item) = PREV_EMPTY;
        }
    }

    if (gen_number == max_generation)
    {
        dprintf (3, ("[g%2d, b%2d]UL: %p->%p->%p (h: %p, t: %p)",
            gen_number, bn, free_list_prev (item), item, free_list_slot (item),
            al->alloc_list_head(), al->alloc_list_tail()));
        dprintf (3, ("[g%2d, b%2d]UL: exit, h->N: %p, h->P: %p, t->N: %p, t->P: %p",
            gen_number, bn,
            (al->alloc_list_head() ? free_list_slot (al->alloc_list_head()) : 0),
            (al->alloc_list_head() ? free_list_prev (al->alloc_list_head()) : 0),
            (al->alloc_list_tail() ? free_list_slot (al->alloc_list_tail()) : 0),
            (al->alloc_list_tail() ? free_list_prev (al->alloc_list_tail()) : 0)));
    }
#endif //DOUBLY_LINKED_FL

    if (al->alloc_list_head() == 0)
    {
        assert (al->alloc_list_tail() == 0);
    }
}

#ifdef DOUBLY_LINKED_FL
void allocator::unlink_item_no_undo (unsigned int bn, uint8_t* item)
{
    alloc_list* al = &alloc_list_of (bn);

    uint8_t* next_item = free_list_slot (item);
    uint8_t* prev_item = free_list_prev (item);

#ifdef FL_VERIFICATION
    {
        uint8_t* start = al->alloc_list_head();
        BOOL found_p = FALSE;
        while (start)
        {
            if (start == item)
            {
                found_p = TRUE;
                break;
            }

            start = free_list_slot (start);
        }

        if (!found_p)
        {
            dprintf (1, ("could not find %p in b%d!!!", item, a_l_number));
            FATAL_GC_ERROR();
        }
    }
#endif //FL_VERIFICATION

    if (prev_item)
    {
        free_list_slot (prev_item) = next_item;
    }
    else
    {
        al->alloc_list_head() = next_item;
    }

    if (next_item)
    {
        free_list_prev (next_item) = prev_item;
    }

    if (al->alloc_list_tail() == item)
    {
        al->alloc_list_tail() = prev_item;
    }

    free_list_prev (item) = PREV_EMPTY;

    if (gen_number == max_generation)
    {
        dprintf (3333, ("[g%2d, b%2d]ULN: %p->%p->%p (h: %p, t: %p)",
            gen_number, bn, free_list_prev (item), item, free_list_slot (item),
            al->alloc_list_head(), al->alloc_list_tail()));
        dprintf (3333, ("[g%2d, b%2d]ULN: exit: h->N: %p, h->P: %p, t->N: %p, t->P: %p",
            gen_number, bn,
            (al->alloc_list_head() ? free_list_slot (al->alloc_list_head()) : 0),
            (al->alloc_list_head() ? free_list_prev (al->alloc_list_head()) : 0),
            (al->alloc_list_tail() ? free_list_slot (al->alloc_list_tail()) : 0),
            (al->alloc_list_tail() ? free_list_prev (al->alloc_list_tail()) : 0)));
    }
}

void allocator::unlink_item_no_undo (uint8_t* item, size_t size)
{
    unsigned int bn = first_suitable_bucket (size);
    unlink_item_no_undo (bn, item);
}

void allocator::unlink_item_no_undo_added (unsigned int bn, uint8_t* item, uint8_t* previous_item)
{
    alloc_list* al = &alloc_list_of (bn);

    uint8_t* next_item = free_list_slot (item);
    uint8_t* prev_item = free_list_prev (item);

    assert (prev_item == previous_item);

    if (prev_item)
    {
        free_list_slot (prev_item) = next_item;
    }
    else
    {
        al->added_alloc_list_head() = next_item;
    }

    if (next_item)
    {
        free_list_prev (next_item) = prev_item;
    }

    if (al->added_alloc_list_tail() == item)
    {
        al->added_alloc_list_tail() = prev_item;
    }

    free_list_prev (item) = PREV_EMPTY;

    if (gen_number == max_generation)
    {
        dprintf (3333, ("[g%2d, b%2d]ULNA: %p->%p->%p (h: %p, t: %p)",
            gen_number, bn, free_list_prev (item), item, free_list_slot (item),
            al->added_alloc_list_head(), al->added_alloc_list_tail()));
        dprintf (3333, ("[g%2d, b%2d]ULNA: exit: h->N: %p, h->P: %p, t->N: %p, t->P: %p",
            gen_number, bn,
            (al->added_alloc_list_head() ? free_list_slot (al->added_alloc_list_head()) : 0),
            (al->added_alloc_list_head() ? free_list_prev (al->added_alloc_list_head()) : 0),
            (al->added_alloc_list_tail() ? free_list_slot (al->added_alloc_list_tail()) : 0),
            (al->added_alloc_list_tail() ? free_list_prev (al->added_alloc_list_tail()) : 0)));
    }
}

int allocator::thread_item_front_added (uint8_t* item, size_t size)
{
    unsigned int a_l_number = first_suitable_bucket (size);
    alloc_list* al = &alloc_list_of (a_l_number);

    free_list_slot (item) = al->added_alloc_list_head();
    free_list_prev (item) = 0;
    // this list's UNDO is not useful.
    free_list_undo (item) = UNDO_EMPTY;

    if (al->added_alloc_list_head() != 0)
    {
        free_list_prev (al->added_alloc_list_head()) = item;
    }

    al->added_alloc_list_head() = item;

    if (al->added_alloc_list_tail() == 0)
    {
        al->added_alloc_list_tail() = item;
    }

    if (gen_number == max_generation)
    {
        dprintf (3333, ("[g%2d, b%2d]TFFA: exit: %p->%p->%p (h: %p, t: %p)",
            gen_number, a_l_number,
            free_list_prev (item), item, free_list_slot (item),
            al->added_alloc_list_head(), al->added_alloc_list_tail()));
        dprintf (3333, ("[g%2d, b%2d]TFFA: h->N: %p, h->P: %p, t->N: %p, t->P: %p",
            gen_number, a_l_number,
            (al->added_alloc_list_head() ? free_list_slot (al->added_alloc_list_head()) : 0),
            (al->added_alloc_list_head() ? free_list_prev (al->added_alloc_list_head()) : 0),
            (al->added_alloc_list_tail() ? free_list_slot (al->added_alloc_list_tail()) : 0),
            (al->added_alloc_list_tail() ? free_list_prev (al->added_alloc_list_tail()) : 0)));
    }

    return a_l_number;
}
#endif //DOUBLY_LINKED_FL

#ifdef DYNAMIC_HEAP_COUNT
// This counts the total fl items, and print out the ones whose heap != this_hp
void allocator::count_items (gc_heap* this_hp, size_t* fl_items_count, size_t* fl_items_for_oh_count)
{
    uint64_t start_us = GetHighPrecisionTimeStamp();
    uint64_t end_us = 0;

    int align_const = get_alignment_constant (gen_number == max_generation);
    size_t num_fl_items = 0;
    // items whose heap != this_hp
    size_t num_fl_items_for_oh = 0;

    for (unsigned int i = 0; i < num_buckets; i++)
    {
        uint8_t* free_item = alloc_list_head_of (i);
        while (free_item)
        {
            assert (((CObjectHeader*)free_item)->IsFree());

            num_fl_items++;
            // Get the heap its region belongs to see if we need to put it back.
            heap_segment* region = gc_heap::region_of (free_item);
            dprintf (3, ("b#%2d FL %Ix region %Ix heap %d -> %d",
                i, free_item, (size_t)region, this_hp->heap_number, region->heap->heap_number));
            if (region->heap != this_hp)
            {
                num_fl_items_for_oh++;
            }

            free_item = free_list_slot (free_item);
        }
    }

    end_us = GetHighPrecisionTimeStamp();
    dprintf (3, ("total - %Id items out of %Id items are from a different heap in %I64d us",
        num_fl_items_for_oh, num_fl_items, (end_us - start_us)));

    *fl_items_count = num_fl_items;
    *fl_items_for_oh_count = num_fl_items_for_oh;
}

#ifdef DOUBLY_LINKED_FL
void min_fl_list_info::thread_item (uint8_t* item)
{
    free_list_slot (item) = 0;
    free_list_undo (item) = UNDO_EMPTY;
    assert (item != head);

    free_list_prev (item) = tail;

    if (head == 0)
    {
        head = item;
    }
    else
    {
        assert ((free_list_slot(head) != 0) || (tail == head));
        assert (item != tail);
        assert (free_list_slot(tail) == 0);

        free_list_slot (tail) = item;
    }

    tail = item;
}
#endif //DOUBLY_LINKED_FL

void min_fl_list_info::thread_item_no_prev (uint8_t* item)
{
    free_list_slot (item) = 0;
    free_list_undo (item) = UNDO_EMPTY;
    assert (item != head);

    if (head == 0)
    {
        head = item;
    }
    else
    {
        assert ((free_list_slot(head) != 0) || (tail == head));
        assert (item != tail);
        assert (free_list_slot(tail) == 0);

        free_list_slot (tail) = item;
    }

    tail = item;
}

// the min_fl_list array is arranged as chunks of n_heaps min_fl_list_info, the 1st chunk corresponds to the 1st bucket,
// and so on.
void allocator::rethread_items (size_t* num_total_fl_items, size_t* num_total_fl_items_rethreaded, gc_heap* current_heap,
                                min_fl_list_info* min_fl_list, size_t *free_list_space_per_heap, int num_heaps)
{
    uint64_t start_us = GetHighPrecisionTimeStamp();
    uint64_t end_us = 0;

    int align_const = get_alignment_constant (gen_number == max_generation);
    size_t num_fl_items = 0;
    size_t num_fl_items_rethreaded = 0;

    assert (num_buckets <= MAX_BUCKET_COUNT);

    for (unsigned int i = 0; i < num_buckets; i++)
    {
        // Get to the portion that corresponds to beginning of this bucket. We will be filling in entries for heaps
        // we can find FL items for.
        min_fl_list_info* current_bucket_min_fl_list = min_fl_list + (i * num_heaps);

        uint8_t* free_item = alloc_list_head_of (i);
        uint8_t* prev_item = nullptr;
        while (free_item)
        {
            assert (((CObjectHeader*)free_item)->IsFree());

            num_fl_items++;
            // Get the heap its region belongs to see if we need to put it back.
            heap_segment* region = gc_heap::region_of (free_item);
            dprintf (3, ("b#%2d FL %Ix region %Ix heap %d -> %d",
                i, free_item, (size_t)region, current_heap->heap_number, region->heap->heap_number));
            // need to keep track of heap and only check if it's not from our heap!!
            if (region->heap != current_heap)
            {
                num_fl_items_rethreaded++;

                size_t size_o = Align(size (free_item), align_const);
                uint8_t* next_item = free_list_slot (free_item);

                int hn = region->heap->heap_number;
#ifdef DOUBLY_LINKED_FL
                if (is_doubly_linked_p())
                {
                    unlink_item_no_undo (free_item, size_o);
                    current_bucket_min_fl_list[hn].thread_item (free_item);
                }
                else
#endif //DOUBLY_LINKED_FL
                {
                    unlink_item (i, free_item, prev_item, FALSE);
                    current_bucket_min_fl_list[hn].thread_item_no_prev (free_item);
                }
                free_list_space_per_heap[hn] += size_o;

                free_item = next_item;
            }
            else
            {
                prev_item = free_item;
                free_item = free_list_slot (free_item);
            }
        }
    }

    end_us = GetHighPrecisionTimeStamp();
    dprintf (8888, ("h%d total %Id items rethreaded out of %Id items in %I64d us (%I64dms)",
        current_heap->heap_number, num_fl_items_rethreaded, num_fl_items, (end_us - start_us), ((end_us - start_us) / 1000)));

    (*num_total_fl_items) += num_fl_items;
    (*num_total_fl_items_rethreaded) += num_fl_items_rethreaded;
}

// merge buckets from min_fl_list to their corresponding buckets to this FL.
void allocator::merge_items (gc_heap* current_heap, int to_num_heaps, int from_num_heaps)
{
    int this_hn = current_heap->heap_number;

    for (unsigned int i = 0; i < num_buckets; i++)
    {
        alloc_list* al = &alloc_list_of (i);
        uint8_t*& head = al->alloc_list_head ();
        uint8_t*& tail = al->alloc_list_tail ();

        for (int other_hn = 0; other_hn < from_num_heaps; other_hn++)
        {
            min_fl_list_info* current_bucket_min_fl_list = gc_heap::g_heaps[other_hn]->min_fl_list + (i * to_num_heaps);

            // get the fl corresponding to the heap we want to merge it onto.
            min_fl_list_info* current_heap_bucket_min_fl_list = &current_bucket_min_fl_list[this_hn];

            uint8_t* head_other_heap = current_heap_bucket_min_fl_list->head;

            if (head_other_heap)
            {
#ifdef DOUBLY_LINKED_FL
                if (is_doubly_linked_p())
                {
                    free_list_prev (head_other_heap) = tail;
                }
#endif //DOUBLY_LINKED_FL

                uint8_t* saved_head = head;
                uint8_t* saved_tail = tail;

                if (head)
                {
                    free_list_slot (tail) = head_other_heap;
                }
                else
                {
                    head = head_other_heap;
                }

                tail = current_heap_bucket_min_fl_list->tail;
            }
        }
    }
}
#endif //DYNAMIC_HEAP_COUNT

void allocator::clear()
{
    for (unsigned int i = 0; i < num_buckets; i++)
    {
        alloc_list_head_of (i) = 0;
        alloc_list_tail_of (i) = 0;
    }
}

//always thread to the end.
void allocator::thread_item (uint8_t* item, size_t size)
{
    unsigned int a_l_number = first_suitable_bucket (size);
    alloc_list* al = &alloc_list_of (a_l_number);
    uint8_t*& head = al->alloc_list_head();
    uint8_t*& tail = al->alloc_list_tail();

    if (al->alloc_list_head() == 0)
    {
        assert (al->alloc_list_tail() == 0);
    }

    free_list_slot (item) = 0;
    free_list_undo (item) = UNDO_EMPTY;
    assert (item != head);

#ifdef DOUBLY_LINKED_FL
    if (gen_number == max_generation)
    {
        free_list_prev (item) = tail;
    }
#endif //DOUBLY_LINKED_FL

    if (head == 0)
    {
        head = item;
    }
    else
    {
        assert ((free_list_slot(head) != 0) || (tail == head));
        assert (item != tail);
        assert (free_list_slot(tail) == 0);

        free_list_slot (tail) = item;
    }

    tail = item;

#ifdef DOUBLY_LINKED_FL
    if (gen_number == max_generation)
    {
        dprintf (3333, ("[g%2d, b%2d]TFE: %p->%p->%p (h: %p, t: %p)",
            gen_number, a_l_number,
            free_list_prev (item), item, free_list_slot (item),
            al->alloc_list_head(), al->alloc_list_tail()));
        dprintf (3333, ("[g%2d, b%2d]TFE: exit: h->N: %p, h->P: %p, t->N: %p, t->P: %p",
            gen_number, a_l_number,
            (al->alloc_list_head() ? free_list_slot (al->alloc_list_head()) : 0),
            (al->alloc_list_head() ? free_list_prev (al->alloc_list_head()) : 0),
            (al->alloc_list_tail() ? free_list_slot (al->alloc_list_tail()) : 0),
            (al->alloc_list_tail() ? free_list_prev (al->alloc_list_tail()) : 0)));
    }
#endif //DOUBLY_LINKED_FL
}

void allocator::thread_item_front (uint8_t* item, size_t size)
{
    unsigned int a_l_number = first_suitable_bucket (size);
    alloc_list* al = &alloc_list_of (a_l_number);

    if (al->alloc_list_head() == 0)
    {
        assert (al->alloc_list_tail() == 0);
    }

    free_list_slot (item) = al->alloc_list_head();
    free_list_undo (item) = UNDO_EMPTY;

    if (al->alloc_list_tail() == 0)
    {
        assert (al->alloc_list_head() == 0);
        al->alloc_list_tail() = al->alloc_list_head();
    }

#ifdef DOUBLY_LINKED_FL
    if (gen_number == max_generation)
    {
        if (al->alloc_list_head() != 0)
        {
            free_list_prev (al->alloc_list_head()) = item;
        }
    }
#endif //DOUBLY_LINKED_FL

    al->alloc_list_head() = item;
    if (al->alloc_list_tail() == 0)
    {
        al->alloc_list_tail() = item;
    }

#ifdef DOUBLY_LINKED_FL
    if (gen_number == max_generation)
    {
        free_list_prev (item) = 0;

        dprintf (3333, ("[g%2d, b%2d]TFF: exit: %p->%p->%p (h: %p, t: %p)",
            gen_number, a_l_number,
            free_list_prev (item), item, free_list_slot (item),
            al->alloc_list_head(), al->alloc_list_tail()));
        dprintf (3333, ("[g%2d, b%2d]TFF: h->N: %p, h->P: %p, t->N: %p, t->P: %p",
            gen_number, a_l_number,
            (al->alloc_list_head() ? free_list_slot (al->alloc_list_head()) : 0),
            (al->alloc_list_head() ? free_list_prev (al->alloc_list_head()) : 0),
            (al->alloc_list_tail() ? free_list_slot (al->alloc_list_tail()) : 0),
            (al->alloc_list_tail() ? free_list_prev (al->alloc_list_tail()) : 0)));
    }
#endif //DOUBLY_LINKED_FL
}

void allocator::copy_to_alloc_list (alloc_list* toalist)
{
    for (unsigned int i = 0; i < num_buckets; i++)
    {
        toalist [i] = alloc_list_of (i);
#ifdef FL_VERIFICATION
        size_t damage_count = alloc_list_damage_count_of (i);
        // We are only calling this method to copy to an empty list
        // so damage count is always 0
        assert (damage_count == 0);

        uint8_t* free_item = alloc_list_head_of (i);
        size_t count = 0;
        while (free_item)
        {
            count++;
            free_item = free_list_slot (free_item);
        }

        toalist[i].item_count = count;
#endif //FL_VERIFICATION
    }
}

void allocator::copy_from_alloc_list (alloc_list* fromalist)
{
    BOOL repair_list = !discard_if_no_fit_p ();
#ifdef DOUBLY_LINKED_FL
    BOOL bgc_repair_p = FALSE;
    if (gen_number == max_generation)
    {
        bgc_repair_p = TRUE;

        if (alloc_list_damage_count_of (0) != 0)
        {
            GCToOSInterface::DebugBreak();
        }

        uint8_t* b0_head = alloc_list_head_of (0);
        if (b0_head)
        {
            free_list_prev (b0_head) = 0;
        }

        added_alloc_list_head_of (0) = 0;
        added_alloc_list_tail_of (0) = 0;
    }

    unsigned int start_index = (bgc_repair_p ? 1 : 0);
#else
    unsigned int start_index = 0;

#endif //DOUBLY_LINKED_FL

    for (unsigned int i = start_index; i < num_buckets; i++)
    {
        size_t count = alloc_list_damage_count_of (i);

        alloc_list_of (i) = fromalist [i];
        assert (alloc_list_damage_count_of (i) == 0);

        if (repair_list)
        {
            //repair the list
            //new items may have been added during the plan phase
            //items may have been unlinked.
            uint8_t* free_item = alloc_list_head_of (i);

            while (free_item && count)
            {
                assert (((CObjectHeader*)free_item)->IsFree());
                if ((free_list_undo (free_item) != UNDO_EMPTY))
                {
                    count--;

                    free_list_slot (free_item) = free_list_undo (free_item);
                    free_list_undo (free_item) = UNDO_EMPTY;
                }

                free_item = free_list_slot (free_item);
            }

#ifdef DOUBLY_LINKED_FL
            if (bgc_repair_p)
            {
                added_alloc_list_head_of (i) = 0;
                added_alloc_list_tail_of (i) = 0;
            }
#endif //DOUBLY_LINKED_FL

#ifdef FL_VERIFICATION
            free_item = alloc_list_head_of (i);
            size_t item_count = 0;
            while (free_item)
            {
                item_count++;
                free_item = free_list_slot (free_item);
            }

            assert (item_count == alloc_list_of (i).item_count);
#endif //FL_VERIFICATION
        }

#ifdef DEBUG
        uint8_t* tail_item = alloc_list_tail_of (i);
        assert ((tail_item == 0) || (free_list_slot (tail_item) == 0));
#endif
    }
}

void allocator::commit_alloc_list_changes()
{
    BOOL repair_list = !discard_if_no_fit_p ();
#ifdef DOUBLY_LINKED_FL
    BOOL bgc_repair_p = FALSE;
    if (gen_number == max_generation)
    {
        bgc_repair_p = TRUE;
    }
#endif //DOUBLY_LINKED_FL

    if (repair_list)
    {
        for (unsigned int i = 0; i < num_buckets; i++)
        {
            //remove the undo info from list.
            uint8_t* free_item = alloc_list_head_of (i);

#ifdef DOUBLY_LINKED_FL
            if (bgc_repair_p)
            {
                dprintf (3, ("C[b%2d] ENTRY: h: %p t: %p", i,
                    alloc_list_head_of (i), alloc_list_tail_of (i)));
            }

            if (free_item && bgc_repair_p)
            {
                if (free_list_prev (free_item) != 0)
                    free_list_prev (free_item) = 0;
            }
#endif //DOUBLY_LINKED_FL

            size_t count = alloc_list_damage_count_of (i);

            while (free_item && count)
            {
                assert (((CObjectHeader*)free_item)->IsFree());

                if (free_list_undo (free_item) != UNDO_EMPTY)
                {
                    free_list_undo (free_item) = UNDO_EMPTY;

#ifdef DOUBLY_LINKED_FL
                    if (bgc_repair_p)
                    {
                        uint8_t* next_item = free_list_slot (free_item);
                        if (next_item && (free_list_prev (next_item) != free_item))
                            free_list_prev (next_item) = free_item;
                    }
#endif //DOUBLY_LINKED_FL

                    count--;
                }

                free_item = free_list_slot (free_item);
            }

            alloc_list_damage_count_of (i) = 0;

#ifdef DOUBLY_LINKED_FL
            if (bgc_repair_p)
            {
                uint8_t* head = alloc_list_head_of (i);
                uint8_t* tail_added = added_alloc_list_tail_of (i);

                if (tail_added)
                {
                    assert (free_list_slot (tail_added) == 0);

                    if (head)
                    {
                        free_list_slot (tail_added) = head;
                        free_list_prev (head) = tail_added;
                    }
                }

                uint8_t* head_added = added_alloc_list_head_of (i);

                if (head_added)
                {
                    alloc_list_head_of (i) = head_added;
                    uint8_t* final_head = alloc_list_head_of (i);

                    if (alloc_list_tail_of (i) == 0)
                    {
                        alloc_list_tail_of (i) = tail_added;
                    }
                }

                added_alloc_list_head_of (i) = 0;
                added_alloc_list_tail_of (i) = 0;
            }
#endif //DOUBLY_LINKED_FL
        }
    }
}

#ifdef USE_REGIONS
void allocator::thread_sip_fl (heap_segment* region)
{
    uint8_t* region_fl_head = region->free_list_head;
    uint8_t* region_fl_tail = region->free_list_tail;

    if (!region_fl_head)
    {
        assert (!region_fl_tail);
        assert (region->free_list_size == 0);
        return;
    }

    if (num_buckets == 1)
    {
        dprintf (REGIONS_LOG, ("threading gen%d region %p onto gen%d FL",
            heap_segment_gen_num (region), heap_segment_mem (region), gen_number));
        alloc_list* al = &alloc_list_of (0);
        uint8_t*& head = al->alloc_list_head();
        uint8_t*& tail = al->alloc_list_tail();

        if (tail == 0)
        {
            assert (head == 0);
            head = region_fl_head;
        }
        else
        {
            free_list_slot (tail) = region_fl_head;
        }

        tail = region_fl_tail;
    }
    else
    {
        dprintf (REGIONS_LOG, ("threading gen%d region %p onto gen%d bucketed FL",
            heap_segment_gen_num (region), heap_segment_mem (region), gen_number));
        // If we have a bucketed free list we'd need to go through the region's free list.
        uint8_t* region_fl_item = region_fl_head;
        size_t total_free_size = 0;
        while (region_fl_item)
        {
            uint8_t* next_fl_item = free_list_slot (region_fl_item);
            size_t size_item = size (region_fl_item);
            thread_item (region_fl_item, size_item);
            total_free_size += size_item;
            region_fl_item = next_fl_item;
        }
        assert (total_free_size == region->free_list_size);
    }
}
#endif //USE_REGIONS

#ifdef FEATURE_EVENT_TRACE
uint16_t allocator::count_largest_items (etw_bucket_info* bucket_info,
                                         size_t max_size,
                                         size_t max_item_count,
                                         size_t* recorded_fl_info_size)
{
    assert (gen_number == max_generation);

    size_t size_counted_total = 0;
    size_t items_counted_total = 0;
    uint16_t bucket_info_index = 0;
    for (int i = (num_buckets - 1); i >= 0; i--)
    {
        uint32_t items_counted = 0;
        size_t size_counted = 0;
        uint8_t* free_item = alloc_list_head_of ((unsigned int)i);
        while (free_item)
        {
            assert (((CObjectHeader*)free_item)->IsFree());

            size_t free_item_size = Align (size (free_item));
            size_counted_total += free_item_size;
            size_counted += free_item_size;
            items_counted_total++;
            items_counted++;

            if ((size_counted_total > max_size) || (items_counted > max_item_count))
            {
                bucket_info[bucket_info_index++].set ((uint16_t)i, items_counted, size_counted);
                *recorded_fl_info_size = size_counted_total;
                return bucket_info_index;
            }

            free_item = free_list_slot (free_item);
        }

        if (items_counted)
        {
            bucket_info[bucket_info_index++].set ((uint16_t)i, items_counted, size_counted);
        }
    }

    *recorded_fl_info_size = size_counted_total;
    return bucket_info_index;
}
#endif //FEATURE_EVENT_TRACE

#ifdef FEATURE_STRUCTALIGN
inline
uint8_t* StructAlign (uint8_t* origPtr, int requiredAlignment, ptrdiff_t alignmentOffset=OBJECT_ALIGNMENT_OFFSET)
{
    // required alignment must be a power of two
    _ASSERTE(((size_t)origPtr & ALIGNCONST) == 0);
    _ASSERTE(((requiredAlignment - 1) & requiredAlignment) == 0);
    _ASSERTE(requiredAlignment >= sizeof(void *));
    _ASSERTE(requiredAlignment <= MAX_STRUCTALIGN);

    // When this method is invoked for individual objects (i.e., alignmentOffset
    // is just the size of the PostHeader), what needs to be aligned when
    // we're done is the pointer to the payload of the object (which means
    // the actual resulting object pointer is typically not aligned).

    uint8_t* result = (uint8_t*)Align ((size_t)origPtr + alignmentOffset, requiredAlignment-1) - alignmentOffset;
    ptrdiff_t alignpad = result - origPtr;

    return result + AdjustmentForMinPadSize (alignpad, requiredAlignment);
}

uint8_t* gc_heap::pad_for_alignment (uint8_t* newAlloc, int requiredAlignment, size_t size, alloc_context* acontext)
{
    uint8_t* alignedPtr = StructAlign (newAlloc, requiredAlignment);
    if (alignedPtr != newAlloc) {
        make_unused_array (newAlloc, alignedPtr - newAlloc);
    }
    acontext->alloc_ptr = alignedPtr + Align (size);
    return alignedPtr;
}

uint8_t* gc_heap::pad_for_alignment_large (uint8_t* newAlloc, int requiredAlignment, size_t size)
{
    uint8_t* alignedPtr = StructAlign (newAlloc, requiredAlignment);
    if (alignedPtr != newAlloc) {
        make_unused_array (newAlloc, alignedPtr - newAlloc);
    }
    if (alignedPtr < newAlloc + ComputeMaxStructAlignPadLarge (requiredAlignment)) {
        make_unused_array (alignedPtr + AlignQword (size), newAlloc + ComputeMaxStructAlignPadLarge (requiredAlignment) - alignedPtr);
    }
    return alignedPtr;
}

#endif //FEATURE_STRUCTALIGN
#ifdef DOUBLY_LINKED_FL
// This is used when we need to clear the prev bit for a free object we made because we know
// it's not actually a free obj (it's just a temporary thing during allocation).
void clear_prev_bit (uint8_t* o, size_t size)
{
    if (size >= min_free_list)
    {
        free_list_prev (o) = 0;
    }
}

#endif //DOUBLY_LINKED_FL

heap_segment*
gc_heap::get_uoh_segment (int gen_number, size_t size, BOOL* did_full_compact_gc, enter_msl_status* msl_status)
{
    *did_full_compact_gc = FALSE;
    size_t last_full_compact_gc_count = get_full_compact_gc_count();

    //access to get_segment needs to be serialized
    add_saved_spinlock_info (true, me_release, mt_get_large_seg, msl_entered);
    leave_spin_lock (&more_space_lock_uoh);
    enter_spin_lock (&gc_heap::gc_lock);
    dprintf (SPINLOCK_LOG, ("[%d]Seg: Egc", heap_number));
    // if a GC happened between here and before we ask for a segment in
    // get_uoh_segment, we need to count that GC.
    size_t current_full_compact_gc_count = get_full_compact_gc_count();

    if (current_full_compact_gc_count > last_full_compact_gc_count)
    {
        *did_full_compact_gc = TRUE;
    }

    if (should_move_heap (&more_space_lock_uoh))
    {
        *msl_status = msl_retry_different_heap;
        leave_spin_lock (&gc_heap::gc_lock);
        return NULL;
    }

    heap_segment* res = get_segment_for_uoh (gen_number, size
#ifdef MULTIPLE_HEAPS
                                            , this
#endif //MULTIPLE_HEAPS
                                            );

    dprintf (SPINLOCK_LOG, ("[%d]Seg: A Lgc", heap_number));
    leave_spin_lock (&gc_heap::gc_lock);
    *msl_status = enter_spin_lock_msl (&more_space_lock_uoh);
    if (*msl_status == msl_retry_different_heap)
        return NULL;

    add_saved_spinlock_info (true, me_acquire, mt_get_large_seg, *msl_status);

    return res;
}

#ifdef MULTIPLE_HEAPS
#ifdef HEAP_BALANCE_INSTRUMENTATION
// We could consider optimizing it so we don't need to get the tid
// everytime but it's not very expensive to get.
void add_to_hb_numa (
    int proc_no,
    int ideal_proc_no,
    int alloc_heap,
    bool multiple_procs_p,
    bool alloc_count_p,
    bool set_ideal_p)
{
    int tid = (int)GCToOSInterface::GetCurrentThreadIdForLogging ();
    uint64_t timestamp = RawGetHighPrecisionTimeStamp ();

    int saved_proc_no = proc_no;
    int numa_no = -1;
    proc_no = get_proc_index_numa (proc_no, &numa_no);

    heap_balance_info_numa* hb_info_numa_node = &hb_info_numa_nodes[numa_no];

    heap_balance_info_proc* hb_info_proc = &(hb_info_numa_node->hb_info_procs[proc_no]);
    int index = hb_info_proc->index;
    int count = hb_info_proc->count;

    if (index == count)
    {
        // Too much info inbetween GCs. This can happen if the thread is scheduled on a different
        // processor very often so it caused us to log many entries due to that reason. You could
        // increase default_max_hb_heap_balance_info but this usually indicates a problem that
        // should be investigated.
        dprintf (HEAP_BALANCE_LOG, ("too much info between GCs, already logged %d entries", index));
        GCToOSInterface::DebugBreak ();
    }
    heap_balance_info* hb_info = &(hb_info_proc->hb_info[index]);

    dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMP[p%3d->%3d(i:%3d), N%d] #%4d: %zd, tid %d, ah: %d, m: %d, p: %d, i: %d",
        saved_proc_no, proc_no, ideal_proc_no, numa_no, index,
        (timestamp - start_raw_ts) / 1000, tid, alloc_heap, (int)multiple_procs_p, (int)(!alloc_count_p), (int)set_ideal_p));

    if (multiple_procs_p)
    {
        tid |= (1 << (sizeof (tid) * 8 - 1));
    }

    if (!alloc_count_p)
    {
        alloc_heap |= (1 << (sizeof (alloc_heap) * 8 - 1));
    }

    if (set_ideal_p)
    {
        alloc_heap |= (1 << (sizeof (alloc_heap) * 8 - 2));
    }

    hb_info->timestamp = timestamp;
    hb_info->tid = tid;
    hb_info->alloc_heap = alloc_heap;
    hb_info->ideal_proc_no = ideal_proc_no;
    (hb_info_proc->index)++;
}

#endif //HEAP_BALANCE_INSTRUMENTATION
#endif //MULTIPLE_HEAPS

//for_gc_p indicates that the work is being done for GC,
//as opposed to concurrent heap verification
void gc_heap::fix_youngest_allocation_area()
{
    // The gen 0 alloc context is never used for allocation in the allocator path. It's
    // still used in the allocation path during GCs.
    assert (generation_allocation_pointer (youngest_generation) == nullptr);
    assert (generation_allocation_limit (youngest_generation) == nullptr);
    heap_segment_allocated (ephemeral_heap_segment) = alloc_allocated;
    assert (heap_segment_mem (ephemeral_heap_segment) <= heap_segment_allocated (ephemeral_heap_segment));
    assert (heap_segment_allocated (ephemeral_heap_segment) <= heap_segment_reserved (ephemeral_heap_segment));
}

//for_gc_p indicates that the work is being done for GC,
//as opposed to concurrent heap verification
void gc_heap::fix_allocation_context (alloc_context* acontext, BOOL for_gc_p,
                                      BOOL record_ac_p)
{
    dprintf (3, ("Fixing allocation context %zx: ptr: %zx, limit: %zx",
                 (size_t)acontext,
                 (size_t)acontext->alloc_ptr, (size_t)acontext->alloc_limit));

    if (acontext->alloc_ptr == 0)
    {
        return;
    }
    int align_const = get_alignment_constant (TRUE);
#ifdef USE_REGIONS
    bool is_ephemeral_heap_segment = in_range_for_segment (acontext->alloc_limit, ephemeral_heap_segment);
#else // USE_REGIONS
    bool is_ephemeral_heap_segment = true;
#endif // USE_REGIONS
    if ((!is_ephemeral_heap_segment) || ((size_t)(alloc_allocated - acontext->alloc_limit) > Align (min_obj_size, align_const)) ||
        !for_gc_p)
    {
        uint8_t*  point = acontext->alloc_ptr;
        size_t  size = (acontext->alloc_limit - acontext->alloc_ptr);
        // the allocation area was from the free list
        // it was shortened by Align (min_obj_size) to make room for
        // at least the shortest unused object
        size += Align (min_obj_size, align_const);
        assert ((size >= Align (min_obj_size)));

        dprintf(3,("Making unused area [%zx, %zx[", (size_t)point,
                    (size_t)point + size ));
        make_unused_array (point, size);

        if (for_gc_p)
        {
            generation_free_obj_space (generation_of (0)) += size;
            if (record_ac_p)
                alloc_contexts_used ++;
        }
    }
    else if (for_gc_p)
    {
        assert (is_ephemeral_heap_segment);
        alloc_allocated = acontext->alloc_ptr;
        assert (heap_segment_allocated (ephemeral_heap_segment) <=
                heap_segment_committed (ephemeral_heap_segment));
        if (record_ac_p)
            alloc_contexts_used ++;
    }

    if (for_gc_p)
    {
        // We need to update the alloc_bytes to reflect the portion that we have not used
        acontext->alloc_bytes -= (acontext->alloc_limit - acontext->alloc_ptr);
        total_alloc_bytes_soh -= (acontext->alloc_limit - acontext->alloc_ptr);

        acontext->alloc_ptr = 0;
        acontext->alloc_limit = acontext->alloc_ptr;
    }
}

//used by the heap verification for concurrent gc.
//it nulls out the words set by fix_allocation_context for heap_verification
void repair_allocation (gc_alloc_context* acontext, void*)
{
    uint8_t*  point = acontext->alloc_ptr;

    if (point != 0)
    {
        dprintf (3, ("Clearing [%zx, %zx[", (size_t)acontext->alloc_ptr,
                     (size_t)acontext->alloc_limit+Align(min_obj_size)));
        memclr (acontext->alloc_ptr - plug_skew,
                (acontext->alloc_limit - acontext->alloc_ptr)+Align (min_obj_size));
    }
}

void void_allocation (gc_alloc_context* acontext, void*)
{
    uint8_t*  point = acontext->alloc_ptr;

    if (point != 0)
    {
        dprintf (3, ("Void [%zx, %zx[", (size_t)acontext->alloc_ptr,
                     (size_t)acontext->alloc_limit+Align(min_obj_size)));
        acontext->alloc_ptr = 0;
        acontext->alloc_limit = acontext->alloc_ptr;
    }
}

void gc_heap::repair_allocation_contexts (BOOL repair_p)
{
    GCToEEInterface::GcEnumAllocContexts (repair_p ? repair_allocation : void_allocation, NULL);
}

struct fix_alloc_context_args
{
    BOOL for_gc_p;
    void* heap;
};
void fix_alloc_context (gc_alloc_context* acontext, void* param)
{
    fix_alloc_context_args* args = (fix_alloc_context_args*)param;
    g_theGCHeap->FixAllocContext(acontext, (void*)(size_t)(args->for_gc_p), args->heap);
}

void gc_heap::fix_allocation_contexts (BOOL for_gc_p)
{
    fix_alloc_context_args args;
    args.for_gc_p = for_gc_p;
    args.heap = __this;

    GCToEEInterface::GcEnumAllocContexts(fix_alloc_context, &args);
    fix_youngest_allocation_area();
}

void gc_heap::fix_older_allocation_area (generation* older_gen)
{
    heap_segment* older_gen_seg = generation_allocation_segment (older_gen);
    if (generation_allocation_limit (older_gen) !=
        heap_segment_plan_allocated (older_gen_seg))
    {
        uint8_t*  point = generation_allocation_pointer (older_gen);

        size_t  size = (generation_allocation_limit (older_gen) - generation_allocation_pointer (older_gen));
        if (size != 0)
        {
            assert ((size >= Align (min_obj_size)));
            dprintf(3,("Making unused area [%zx, %zx[", (size_t)point, (size_t)point+size));
            make_unused_array (point, size);
            if (size >= min_free_list)
            {
                generation_allocator (older_gen)->thread_item_front (point, size);
                add_gen_free (older_gen->gen_num, size);
                generation_free_list_space (older_gen) += size;
            }
            else
            {
                generation_free_obj_space (older_gen) += size;
            }
        }
    }
    else
    {
        assert (older_gen_seg != ephemeral_heap_segment);
        heap_segment_plan_allocated (older_gen_seg) =
            generation_allocation_pointer (older_gen);
        generation_allocation_limit (older_gen) =
            generation_allocation_pointer (older_gen);
    }

    generation_allocation_pointer (older_gen) = 0;
    generation_allocation_limit (older_gen) = 0;
}

#ifdef MULTIPLE_HEAPS
// make sure this allocation context does not point to idle heaps
void gc_heap::fix_allocation_context_heaps (gc_alloc_context* gc_context, void*)
{
    alloc_context* acontext = (alloc_context*)gc_context;
    GCHeap* pHomeHeap = acontext->get_home_heap ();
    int home_hp_num = pHomeHeap ? pHomeHeap->pGenGCHeap->heap_number : 0;
    if (home_hp_num >= gc_heap::n_heaps)
    {
        home_hp_num %= gc_heap::n_heaps;
        acontext->set_home_heap (GCHeap::GetHeap (home_hp_num));
    }
    GCHeap* pAllocHeap = acontext->get_alloc_heap ();
    int alloc_hp_num = pAllocHeap ? pAllocHeap->pGenGCHeap->heap_number : 0;
    if (alloc_hp_num >= gc_heap::n_heaps)
    {
        alloc_hp_num %= gc_heap::n_heaps;
        acontext->set_alloc_heap (GCHeap::GetHeap (alloc_hp_num));
        gc_heap* hp = acontext->get_alloc_heap ()->pGenGCHeap;
        hp->alloc_context_count = hp->alloc_context_count + 1;
    }
}

// make sure no allocation contexts point to idle heaps
void gc_heap::fix_allocation_contexts_heaps()
{
    GCToEEInterface::GcEnumAllocContexts (fix_allocation_context_heaps, nullptr);
}

#endif //MULTIPLE_HEAPS

void gc_heap::set_allocation_heap_segment (generation* gen)
{
#ifdef USE_REGIONS
    heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
    dprintf (REGIONS_LOG, ("set gen%d alloc seg to start seg %p", gen->gen_num, heap_segment_mem (seg)));
#else
    uint8_t* p = generation_allocation_start (gen);
    assert (p);
    heap_segment* seg = generation_allocation_segment (gen);
    if (in_range_for_segment (p, seg))
        return;

    // try ephemeral heap segment in case of heap expansion
    seg = ephemeral_heap_segment;
    if (!in_range_for_segment (p, seg))
    {
        seg = heap_segment_rw (generation_start_segment (gen));

        _ASSERTE(seg != NULL);

        while (!in_range_for_segment (p, seg))
        {
            seg = heap_segment_next_rw (seg);
            _ASSERTE(seg != NULL);
        }
    }
#endif //USE_REGIONS

    generation_allocation_segment (gen) = seg;
}

void gc_heap::reset_allocation_pointers (generation* gen, uint8_t* start)
{
    assert (start);
    assert (Align ((size_t)start) == (size_t)start);
#ifndef USE_REGIONS
    generation_allocation_start (gen) = start;
#endif //!USE_REGIONS
    generation_allocation_pointer (gen) =  0;//start + Align (min_obj_size);
    generation_allocation_limit (gen) = 0;//generation_allocation_pointer (gen);
    set_allocation_heap_segment (gen);
}

bool gc_heap::new_allocation_allowed (int gen_number)
{
    if (dd_new_allocation (dynamic_data_of (gen_number)) < 0)
    {
        return FALSE;
    }
#ifndef MULTIPLE_HEAPS
    else if ((settings.pause_mode != pause_no_gc) && (gen_number == 0))
    {
        dynamic_data* dd0 = dynamic_data_of (0);
        dprintf (3, ("evaluating, running amount %zd - new %zd = %zd",
            allocation_running_amount, dd_new_allocation (dd0),
            (allocation_running_amount - dd_new_allocation (dd0))));
        if ((allocation_running_amount - dd_new_allocation (dd0)) >
            dd_min_size (dd0))
        {
            uint64_t ctime = GCToOSInterface::GetLowPrecisionTimeStamp();
            if ((ctime - allocation_running_time) > 1000)
            {
                dprintf (2, (">1s since last gen0 gc"));
                return FALSE;
            }
            else
            {
                allocation_running_amount = dd_new_allocation (dd0);
            }
        }
    }
#endif //MULTIPLE_HEAPS
    return TRUE;
}

inline
ptrdiff_t gc_heap::get_desired_allocation (int gen_number)
{
    return dd_desired_allocation (dynamic_data_of (gen_number));
}

inline
ptrdiff_t  gc_heap::get_new_allocation (int gen_number)
{
    return dd_new_allocation (dynamic_data_of (gen_number));
}

//return the amount allocated so far in gen_number
inline
ptrdiff_t  gc_heap::get_allocation (int gen_number)
{
    dynamic_data* dd = dynamic_data_of (gen_number);

    return dd_desired_allocation (dd) - dd_new_allocation (dd);
}

#ifdef SHORT_PLUGS
inline
void set_padding_in_expand (uint8_t* old_loc,
                            BOOL set_padding_on_saved_p,
                            mark* pinned_plug_entry)
{
    if (set_padding_on_saved_p)
    {
        set_plug_padded (get_plug_start_in_saved (old_loc, pinned_plug_entry));
    }
    else
    {
        set_plug_padded (old_loc);
    }
}

#endif //SHORT_PLUGS

inline
BOOL gc_heap::size_fit_p (size_t size REQD_ALIGN_AND_OFFSET_DCL, uint8_t* alloc_pointer, uint8_t* alloc_limit,
                          uint8_t* old_loc, int use_padding)
{
    BOOL already_padded = FALSE;
#ifdef SHORT_PLUGS
    if ((old_loc != 0) && (use_padding & USE_PADDING_FRONT))
    {
        alloc_pointer = alloc_pointer + Align (min_obj_size);
        already_padded = TRUE;
    }
#endif //SHORT_PLUGS

    if (!((old_loc == 0) || same_large_alignment_p (old_loc, alloc_pointer)))
        size = size + switch_alignment_size (already_padded);

#ifdef FEATURE_STRUCTALIGN
    alloc_pointer = StructAlign(alloc_pointer, requiredAlignment, alignmentOffset);
#endif // FEATURE_STRUCTALIGN

    // in allocate_in_condemned_generation we can have this when we
    // set the alloc_limit to plan_allocated which could be less than
    // alloc_ptr
    if (alloc_limit < alloc_pointer)
    {
        return FALSE;
    }

    if (old_loc != 0)
    {
        return (((size_t)(alloc_limit - alloc_pointer) >= (size + ((use_padding & USE_PADDING_TAIL)? Align(min_obj_size) : 0)))
#ifdef SHORT_PLUGS
                ||((!(use_padding & USE_PADDING_FRONT)) && ((alloc_pointer + size) == alloc_limit))
#else //SHORT_PLUGS
                ||((alloc_pointer + size) == alloc_limit)
#endif //SHORT_PLUGS
            );
    }
    else
    {
        assert (size == Align (min_obj_size));
        return ((size_t)(alloc_limit - alloc_pointer) >= size);
    }
}

inline
BOOL gc_heap::a_size_fit_p (size_t size, uint8_t* alloc_pointer, uint8_t* alloc_limit,
                            int align_const)
{
    // We could have run into cases where this is true when alloc_allocated is the
    // the same as the seg committed.
    if (alloc_limit < alloc_pointer)
    {
        return FALSE;
    }

    return ((size_t)(alloc_limit - alloc_pointer) >= (size + Align(min_obj_size, align_const)));
}

// Grow by committing more pages
BOOL gc_heap::grow_heap_segment (heap_segment* seg, uint8_t* high_address, bool* hard_limit_exceeded_p)
{
    assert (high_address <= heap_segment_reserved (seg));

    if (hard_limit_exceeded_p)
        *hard_limit_exceeded_p = false;

    //return 0 if we are at the end of the segment.
    if (align_on_page (high_address) > heap_segment_reserved (seg))
        return FALSE;

    if (high_address <= heap_segment_committed (seg))
        return TRUE;

    size_t c_size = align_on_page ((size_t)(high_address - heap_segment_committed (seg)));
    c_size = max (c_size, commit_min_th);
    c_size = min (c_size, (size_t)(heap_segment_reserved (seg) - heap_segment_committed (seg)));

    if (c_size == 0)
        return FALSE;

    STRESS_LOG2(LF_GC, LL_INFO10000,
                "Growing heap_segment: %zx high address: %zx\n",
                (size_t)seg, (size_t)high_address);

    bool ret = virtual_commit (heap_segment_committed (seg), c_size, heap_segment_oh (seg), heap_number, hard_limit_exceeded_p);
    if (ret)
    {
        heap_segment_committed (seg) += c_size;

        STRESS_LOG1(LF_GC, LL_INFO10000, "New commit: %zx\n",
                    (size_t)heap_segment_committed (seg));

        assert (heap_segment_committed (seg) <= heap_segment_reserved (seg));
        assert (high_address <= heap_segment_committed (seg));

#if defined(MULTIPLE_HEAPS) && !defined(USE_REGIONS)
        // we should never increase committed beyond decommit target when gradual
        // decommit is in progress - if we do, this means commit and decommit are
        // going on at the same time.
        assert (!gradual_decommit_in_progress_p ||
                (seg != ephemeral_heap_segment) ||
                (heap_segment_committed (seg) <= heap_segment_decommit_target (seg)));
#endif //MULTIPLE_HEAPS && !USE_REGIONS
    }

    return !!ret;
}

inline
int gc_heap::grow_heap_segment (heap_segment* seg, uint8_t* allocated, uint8_t* old_loc, size_t size,
                                BOOL pad_front_p  REQD_ALIGN_AND_OFFSET_DCL)
{
    BOOL already_padded = FALSE;
#ifdef SHORT_PLUGS
    if ((old_loc != 0) && pad_front_p)
    {
        allocated = allocated + Align (min_obj_size);
        already_padded = TRUE;
    }
#endif //SHORT_PLUGS

    if (!((old_loc == 0) || same_large_alignment_p (old_loc, allocated)))
        size += switch_alignment_size (already_padded);

#ifdef FEATURE_STRUCTALIGN
    size_t pad = ComputeStructAlignPad(allocated, requiredAlignment, alignmentOffset);
    return grow_heap_segment (seg, allocated + pad + size);
#else // FEATURE_STRUCTALIGN
    return grow_heap_segment (seg, allocated + size);
#endif // FEATURE_STRUCTALIGN
}

// thread this object to the front of gen's free list and update stats.
void gc_heap::thread_free_item_front (generation* gen, uint8_t* free_start, size_t free_size)
{
    make_unused_array (free_start, free_size);
    generation_free_list_space (gen) += free_size;
    generation_allocator(gen)->thread_item_front (free_start, free_size);
    add_gen_free (gen->gen_num, free_size);

    if (gen->gen_num == max_generation)
    {
        dprintf (2, ("AO h%d: gen2F+: %p(%zd)->%zd, FO: %zd",
            heap_number, free_start, free_size,
            generation_free_list_space (gen), generation_free_obj_space (gen)));
    }
}

#ifdef DOUBLY_LINKED_FL
void gc_heap::thread_item_front_added (generation* gen, uint8_t* free_start, size_t free_size)
{
    make_unused_array (free_start, free_size);
    generation_free_list_space (gen) += free_size;
    int bucket_index = generation_allocator(gen)->thread_item_front_added (free_start, free_size);

    if (gen->gen_num == max_generation)
    {
        dprintf (2, ("AO [h%d] gen2FL+: %p(%zd)->%zd",
            heap_number, free_start, free_size, generation_free_list_space (gen)));
    }

    add_gen_free (gen->gen_num, free_size);
}

#endif //DOUBLY_LINKED_FL

// this is for free objects that are not on the free list; also update stats.
void gc_heap::make_free_obj (generation* gen, uint8_t* free_start, size_t free_size)
{
    make_unused_array (free_start, free_size);
    generation_free_obj_space (gen) += free_size;

    if (gen->gen_num == max_generation)
    {
        dprintf (2, ("AO [h%d] gen2FO+: %p(%zd)->%zd",
            heap_number, free_start, free_size, generation_free_obj_space (gen)));
    }
}

//used only in older generation allocation (i.e during gc).
void gc_heap::adjust_limit (uint8_t* start, size_t limit_size, generation* gen)
{
    dprintf (3, ("gc Expanding segment allocation"));
    heap_segment* seg = generation_allocation_segment (gen);
    if ((generation_allocation_limit (gen) != start) || (start != heap_segment_plan_allocated (seg)))
    {
        if (generation_allocation_limit (gen) == heap_segment_plan_allocated (seg))
        {
            assert (generation_allocation_pointer (gen) >= heap_segment_mem (seg));
            assert (generation_allocation_pointer (gen) <= heap_segment_committed (seg));
            heap_segment_plan_allocated (generation_allocation_segment (gen)) = generation_allocation_pointer (gen);
        }
        else
        {
            uint8_t*  hole = generation_allocation_pointer (gen);
            size_t  size = (generation_allocation_limit (gen) - generation_allocation_pointer (gen));

            if (size != 0)
            {
                dprintf (3, ("filling up hole: %p, size %zx", hole, size));
                size_t allocated_size = generation_allocation_pointer (gen) - generation_allocation_context_start_region (gen);
#ifdef DOUBLY_LINKED_FL
                if (gen->gen_num == max_generation)
                {
                    // For BGC since we need to thread the max_gen's free list as a doubly linked list we need to
                    // preserve 5 ptr-sized words: SB | MT | Len | Next | Prev
                    // This means we cannot simply make a filler free object right after what's allocated in this
                    // alloc context if that's < 5-ptr sized.
                    //
                    if (allocated_size <= min_free_item_no_prev)
                    {
                        // We can't make the free object just yet. Need to record the size.
                        size_t* filler_free_obj_size_location = (size_t*)(generation_allocation_context_start_region (gen) +
                                                                          min_free_item_no_prev);
                        size_t filler_free_obj_size = 0;
                        if (size >= (Align (min_free_list) + Align (min_obj_size)))
                        {

                            filler_free_obj_size = Align (min_obj_size);
                            size_t fl_size = size - filler_free_obj_size;
                            thread_item_front_added (gen, (hole + filler_free_obj_size), fl_size);
                        }
                        else
                        {
                            filler_free_obj_size = size;
                        }

                        generation_free_obj_space (gen) += filler_free_obj_size;
                        *filler_free_obj_size_location = filler_free_obj_size;
                        uint8_t* old_loc = generation_last_free_list_allocated (gen);

                        // check if old_loc happens to be in a saved plug_and_gap with a pinned plug after it
                        uint8_t* saved_plug_and_gap = nullptr;
                        if (saved_pinned_plug_index != INVALID_SAVED_PINNED_PLUG_INDEX)
                        {
                            saved_plug_and_gap = pinned_plug (pinned_plug_of (saved_pinned_plug_index)) - sizeof(plug_and_gap);

                            dprintf (3333, ("[h%d] sppi: %zd mtos: %zd old_loc: %p pp: %p(%zd) offs: %zd",
                                heap_number,
                                saved_pinned_plug_index,
                                mark_stack_tos,
                                old_loc,
                                pinned_plug (pinned_plug_of (saved_pinned_plug_index)),
                                pinned_len (pinned_plug_of (saved_pinned_plug_index)),
                                old_loc - saved_plug_and_gap));
                        }
                        size_t offset = old_loc - saved_plug_and_gap;
                        if (offset < sizeof(gap_reloc_pair))
                        {
                            // the object at old_loc must be at least min_obj_size
                            assert (offset <= sizeof(plug_and_gap) - min_obj_size);

                            // if so, set the bit in the saved info instead
                            set_free_obj_in_compact_bit ((uint8_t*)(&pinned_plug_of (saved_pinned_plug_index)->saved_pre_plug_reloc) + offset);
                        }
                        else
                        {
#ifdef _DEBUG
                            // check this looks like an object
                            header(old_loc)->Validate();
#endif //_DEBUG
                            set_free_obj_in_compact_bit (old_loc);
                        }

                        dprintf (3333, ("[h%d] ac: %p->%p((%zd < %zd), Pset %p s->%zd", heap_number,
                            generation_allocation_context_start_region (gen), generation_allocation_pointer (gen),
                            allocated_size, min_free_item_no_prev, filler_free_obj_size_location, filler_free_obj_size));
                    }
                    else
                    {
                        if (size >= Align (min_free_list))
                        {
                            thread_item_front_added (gen, hole, size);
                        }
                        else
                        {
                            make_free_obj (gen, hole, size);
                        }
                    }
                }
                else
#endif //DOUBLY_LINKED_FL
                {
                    // TODO: this should be written the same way as the above, ie, it should check
                    // allocated_size first, but it doesn't need to do MAKE_FREE_OBJ_IN_COMPACT
                    // related things.
                    if (size >= Align (min_free_list))
                    {
                        if (allocated_size < min_free_item_no_prev)
                        {
                            if (size >= (Align (min_free_list) + Align (min_obj_size)))
                            {
                                //split hole into min obj + threadable free item
                                make_free_obj (gen, hole, min_obj_size);
                                thread_free_item_front (gen, (hole + Align (min_obj_size)),
                                    (size - Align (min_obj_size)));
                            }
                            else
                            {
                                dprintf (3, ("allocated size too small, can't put back rest on free list %zx",
                                    allocated_size));
                                make_free_obj (gen, hole, size);
                            }
                        }
                        else
                        {
                            dprintf (3, ("threading hole in front of free list"));
                            thread_free_item_front (gen, hole, size);
                        }
                    }
                    else
                    {
                        make_free_obj (gen, hole, size);
                    }
                }
            }
        }
        generation_allocation_pointer (gen) = start;
        generation_allocation_context_start_region (gen) = start;
    }
    generation_allocation_limit (gen) = (start + limit_size);
}

void verify_mem_cleared (uint8_t* start, size_t size)
{
    if (!Aligned (size))
    {
        FATAL_GC_ERROR();
    }

    PTR_PTR curr_ptr = (PTR_PTR) start;
    for (size_t i = 0; i < size / sizeof(PTR_PTR); i++)
    {
        if (*(curr_ptr++) != 0)
        {
            FATAL_GC_ERROR();
        }
    }
}

#if defined (VERIFY_HEAP) && defined (BACKGROUND_GC)
// makes sure that the mark array bits between start and end are 0.
void gc_heap::check_batch_mark_array_bits (uint8_t* start, uint8_t* end)
{
    size_t start_mark_bit = mark_bit_of (start);
    size_t end_mark_bit = mark_bit_of (end);
    unsigned int startbit = mark_bit_bit (start_mark_bit);
    unsigned int endbit = mark_bit_bit (end_mark_bit);
    size_t startwrd = mark_bit_word (start_mark_bit);
    size_t endwrd = mark_bit_word (end_mark_bit);

    //dprintf (3, ("Setting all mark array bits between [%zx:%zx-[%zx:%zx",
    //    (size_t)start, (size_t)start_mark_bit,
    //    (size_t)end, (size_t)end_mark_bit));

    unsigned int firstwrd = ~(lowbits (~0, startbit));
    unsigned int lastwrd = ~(highbits (~0, endbit));

    if (startwrd == endwrd)
    {
        unsigned int wrd = firstwrd & lastwrd;
        if (mark_array[startwrd] & wrd)
        {
            dprintf  (1, ("The %x portion of mark bits at 0x%zx:0x%x(addr: 0x%p) were not cleared",
                            wrd, startwrd,
                            mark_array [startwrd], mark_word_address (startwrd)));
            FATAL_GC_ERROR();
        }
        return;
    }

    // set the first mark word.
    if (startbit)
    {
        if (mark_array[startwrd] & firstwrd)
        {
            dprintf  (1, ("The %x portion of mark bits at 0x%zx:0x%x(addr: 0x%p) were not cleared",
                            firstwrd, startwrd,
                            mark_array [startwrd], mark_word_address (startwrd)));
            FATAL_GC_ERROR();
        }

        startwrd++;
    }

    for (size_t wrdtmp = startwrd; wrdtmp < endwrd; wrdtmp++)
    {
        if (mark_array[wrdtmp])
        {
            dprintf  (1, ("The mark bits at 0x%zx:0x%x(addr: 0x%p) were not cleared",
                            wrdtmp,
                            mark_array [wrdtmp], mark_word_address (wrdtmp)));
            FATAL_GC_ERROR();
        }
    }

    // set the last mark word.
    if (endbit)
    {
        if (mark_array[endwrd] & lastwrd)
        {
            dprintf  (1, ("The %x portion of mark bits at 0x%x:0x%x(addr: 0x%p) were not cleared",
                            lastwrd, lastwrd,
                            mark_array [lastwrd], mark_word_address (lastwrd)));
            FATAL_GC_ERROR();
        }
    }
}

#endif

void gc_heap::adjust_limit_clr (uint8_t* start, size_t limit_size, size_t size,
                                alloc_context* acontext, uint32_t flags,
                                heap_segment* seg, int align_const, int gen_number)
{
    bool uoh_p = (gen_number > 0);
    GCSpinLock* msl = uoh_p ? &more_space_lock_uoh : &more_space_lock_soh;
    uint64_t& total_alloc_bytes = uoh_p ? total_alloc_bytes_uoh : total_alloc_bytes_soh;

    size_t aligned_min_obj_size = Align(min_obj_size, align_const);

#ifdef USE_REGIONS
    if (seg)
    {
        assert (heap_segment_used (seg) <= heap_segment_committed (seg));
    }
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
    if (gen_number == 0)
    {
        if (!gen0_allocated_after_gc_p)
        {
            gen0_allocated_after_gc_p = true;
        }
    }
#endif //MULTIPLE_HEAPS

    dprintf (3, ("Expanding segment allocation [%zx, %zx[", (size_t)start,
               (size_t)start + limit_size - aligned_min_obj_size));

    if ((acontext->alloc_limit != start) &&
        (acontext->alloc_limit + aligned_min_obj_size)!= start)
    {
        uint8_t*  hole = acontext->alloc_ptr;
        if (hole != 0)
        {
            size_t  ac_size = (acontext->alloc_limit - acontext->alloc_ptr);
            dprintf (3, ("filling up hole [%zx, %zx[", (size_t)hole, (size_t)hole + ac_size + aligned_min_obj_size));
            // when we are finishing an allocation from a free list
            // we know that the free area was Align(min_obj_size) larger
            acontext->alloc_bytes -= ac_size;
            total_alloc_bytes -= ac_size;
            size_t free_obj_size = ac_size + aligned_min_obj_size;
            make_unused_array (hole, free_obj_size);
            generation_free_obj_space (generation_of (gen_number)) += free_obj_size;
        }
        acontext->alloc_ptr = start;
    }
    else
    {
        if (gen_number == 0)
        {
#ifdef USE_REGIONS
            if (acontext->alloc_ptr == 0)
            {
                acontext->alloc_ptr = start;
            }
            else
#endif //USE_REGIONS
            {
                size_t pad_size = aligned_min_obj_size;
                dprintf (3, ("contiguous ac: making min obj gap %p->%p(%zd)",
                    acontext->alloc_ptr, (acontext->alloc_ptr + pad_size), pad_size));
                make_unused_array (acontext->alloc_ptr, pad_size);
                acontext->alloc_ptr += pad_size;
            }
        }
    }
    acontext->alloc_limit = (start + limit_size - aligned_min_obj_size);
    size_t added_bytes = limit_size - ((gen_number <= max_generation) ? aligned_min_obj_size : 0);
    acontext->alloc_bytes += added_bytes;
    total_alloc_bytes     += added_bytes;

    size_t etw_allocation_amount = 0;
    bool fire_event_p = update_alloc_info (gen_number, added_bytes, &etw_allocation_amount);

    uint8_t* saved_used = 0;

    if (seg)
    {
        saved_used = heap_segment_used (seg);
    }

    if (seg == ephemeral_heap_segment)
    {
        //Sometimes the allocated size is advanced without clearing the
        //memory. Let's catch up here
        if (heap_segment_used (seg) < (alloc_allocated - plug_skew))
        {
            heap_segment_used (seg) = alloc_allocated - plug_skew;
            assert (heap_segment_mem (seg) <= heap_segment_used (seg));
            assert (heap_segment_used (seg) <= heap_segment_reserved (seg));
        }
    }
#ifdef BACKGROUND_GC
    else if (seg)
    {
        uint8_t* old_allocated = heap_segment_allocated (seg) - plug_skew - limit_size;
#ifdef FEATURE_LOH_COMPACTION
        if (gen_number == loh_generation)
        {
            old_allocated -= Align (loh_padding_obj_size, align_const);
        }
#endif //FEATURE_LOH_COMPACTION

        assert (heap_segment_used (seg) >= old_allocated);
    }
#endif //BACKGROUND_GC

    // we are going to clear a right-edge exclusive span [clear_start, clear_limit)
    // but will adjust for cases when object is ok to stay dirty or the space has not seen any use yet
    // NB: the size and limit_size include syncblock, which is to the -1 of the object start
    //     that effectively shifts the allocation by `plug_skew`
    uint8_t* clear_start = start - plug_skew;
    uint8_t* clear_limit = start + limit_size - plug_skew;

    if (flags & GC_ALLOC_ZEROING_OPTIONAL)
    {
        uint8_t* obj_start = acontext->alloc_ptr;
        assert(start >= obj_start);
        uint8_t* obj_end = obj_start + size - plug_skew;
        assert(obj_end >= clear_start);

        // if clearing at the object start, clear the syncblock.
        if(obj_start == start)
        {
            *(PTR_PTR)clear_start = 0;
        }
        // skip the rest of the object
        dprintf(3, ("zeroing optional: skipping object at %p->%p(%zd)",
            clear_start, obj_end, obj_end - clear_start));
        clear_start = obj_end;
    }

    // fetch the ephemeral_heap_segment *before* we release the msl
    // - ephemeral_heap_segment may change due to other threads allocating
    heap_segment* gen0_segment = ephemeral_heap_segment;

#ifdef BACKGROUND_GC
    {
        if (uoh_p && gc_heap::background_running_p())
        {
            uint8_t* obj = acontext->alloc_ptr;
            uint8_t* result = obj;
            uint8_t* current_lowest_address = background_saved_lowest_address;
            uint8_t* current_highest_address = background_saved_highest_address;

            if (current_c_gc_state == c_gc_state_planning)
            {
                dprintf (3, ("Concurrent allocation of a large object %zx",
                            (size_t)obj));
                //mark the new block specially so we know it is a new object
                if ((result < current_highest_address) && (result >= current_lowest_address))
                {
#ifdef DOUBLY_LINKED_FL
                    heap_segment* seg = seg_mapping_table_segment_of (result);
                    // if bgc_allocated is 0 it means it was allocated during bgc sweep,
                    // and since sweep does not look at this seg we cannot set the mark array bit.
                    uint8_t* background_allocated = heap_segment_background_allocated(seg);
                    if (background_allocated != 0)
#endif //DOUBLY_LINKED_FL
                    {
                        dprintf(3, ("Setting mark bit at address %zx",
                            (size_t)(&mark_array[mark_word_of(result)])));

                        mark_array_set_marked(result);
                    }
                }
            }
        }
    }
#endif //BACKGROUND_GC

    // check if space to clear is all dirty from prior use or only partially
    if ((seg == 0) || (clear_limit <= heap_segment_used (seg)))
    {
        add_saved_spinlock_info (uoh_p, me_release, mt_clr_mem, msl_entered);
        leave_spin_lock (msl);

        if (clear_start < clear_limit)
        {
            dprintf(3, ("clearing memory at %p for %zd bytes", clear_start, clear_limit - clear_start));
            memclr(clear_start, clear_limit - clear_start);
        }
    }
    else
    {
        // we only need to clear [clear_start, used) and only if clear_start < used
        uint8_t* used = heap_segment_used (seg);
        heap_segment_used (seg) = clear_limit;

        add_saved_spinlock_info (uoh_p, me_release, mt_clr_mem, msl_entered);
        leave_spin_lock (msl);

        if (clear_start < used)
        {
            if (used != saved_used)
            {
                FATAL_GC_ERROR();
            }

            dprintf (2, ("clearing memory before used at %p for %zd bytes", clear_start, used - clear_start));
            memclr (clear_start, used - clear_start);
        }
    }

#ifdef FEATURE_EVENT_TRACE
    if (fire_event_p)
    {
        fire_etw_allocation_event (etw_allocation_amount, gen_number, acontext->alloc_ptr, size);
    }
#endif //FEATURE_EVENT_TRACE

    //this portion can be done after we release the lock
    if (seg == gen0_segment ||
       ((seg == nullptr) && (gen_number == 0) && (limit_size >= CLR_SIZE / 2)))
    {
        if (gen0_must_clear_bricks > 0)
        {
            //set the brick table to speed up find_object
            size_t b = brick_of (acontext->alloc_ptr);
            set_brick (b, acontext->alloc_ptr - brick_address (b));
            b++;
            dprintf (3, ("Allocation Clearing bricks [%zx, %zx[",
                         b, brick_of (align_on_brick (start + limit_size))));
            volatile short* x = &brick_table [b];
            short* end_x = &brick_table [brick_of (align_on_brick (start + limit_size))];

            for (;x < end_x;x++)
                *x = -1;
        }
        else
        {
            gen0_bricks_cleared = FALSE;
        }
    }

    // verifying the memory is completely cleared.
    //if (!(flags & GC_ALLOC_ZEROING_OPTIONAL))
    //{
    //    verify_mem_cleared(start - plug_skew, limit_size);
    //}
}

size_t gc_heap::new_allocation_limit (size_t size, size_t physical_limit, int gen_number)
{
    dynamic_data* dd = dynamic_data_of (gen_number);
    ptrdiff_t new_alloc = dd_new_allocation (dd);
    assert (new_alloc == (ptrdiff_t)Align (new_alloc, get_alignment_constant (gen_number < uoh_start_generation)));

    ptrdiff_t logical_limit = max (new_alloc, (ptrdiff_t)size);
    size_t limit = min (logical_limit, (ptrdiff_t)physical_limit);
    assert (limit == Align (limit, get_alignment_constant (gen_number <= max_generation)));

    return limit;
}

size_t gc_heap::limit_from_size (size_t size, uint32_t flags, size_t physical_limit, int gen_number,
                                 int align_const)
{
    size_t padded_size = size + Align (min_obj_size, align_const);
    // for LOH this is not true...we could select a physical_limit that's exactly the same
    // as size.
    assert ((gen_number != 0) || (physical_limit >= padded_size));

    // For SOH if the size asked for is very small, we want to allocate more than just what's asked for if possible.
    // Unless we were told not to clean, then we will not force it.
    size_t min_size_to_allocate = ((gen_number == 0 && !(flags & GC_ALLOC_ZEROING_OPTIONAL)) ? allocation_quantum : 0);

    size_t desired_size_to_allocate  = max (padded_size, min_size_to_allocate);
    size_t new_physical_limit = min (physical_limit, desired_size_to_allocate);

    size_t new_limit = new_allocation_limit (padded_size,
                                             new_physical_limit,
                                             gen_number);
    assert (new_limit >= (size + Align (min_obj_size, align_const)));
    dprintf (3, ("h%d requested to allocate %zd bytes, actual size is %zd, phy limit: %zd",
        heap_number, size, new_limit, physical_limit));
    return new_limit;
}

void gc_heap::add_to_oom_history_per_heap()
{
    oom_history* current_hist = &oomhist_per_heap[oomhist_index_per_heap];
    memcpy (current_hist, &oom_info, sizeof (oom_info));
    oomhist_index_per_heap++;
    if (oomhist_index_per_heap == max_oom_history_count)
    {
        oomhist_index_per_heap = 0;
    }
}

void gc_heap::handle_oom (oom_reason reason, size_t alloc_size,
                          uint8_t* allocated, uint8_t* reserved)
{
    if (reason == oom_budget)
    {
        alloc_size = dd_min_size (dynamic_data_of (0)) / 2;
    }

    if ((reason == oom_budget) && ((!fgm_result.loh_p) && (fgm_result.fgm != fgm_no_failure)))
    {
        // This means during the last GC we needed to reserve and/or commit more memory
        // but we couldn't. We proceeded with the GC and ended up not having enough
        // memory at the end. This is a legitimate OOM situtation. Otherwise we
        // probably made a mistake and didn't expand the heap when we should have.
        reason = oom_low_mem;
    }

    oom_info.reason = reason;
    oom_info.allocated = allocated;
    oom_info.reserved = reserved;
    oom_info.alloc_size = alloc_size;
    oom_info.gc_index = settings.gc_index;
    oom_info.fgm = fgm_result.fgm;
    oom_info.size = fgm_result.size;
    oom_info.available_pagefile_mb = fgm_result.available_pagefile_mb;
    oom_info.loh_p = fgm_result.loh_p;

    add_to_oom_history_per_heap();
    fgm_result.fgm = fgm_no_failure;

    // Break early - before the more_space_lock is release so no other threads
    // could have allocated on the same heap when OOM happened.
    if (GCConfig::GetBreakOnOOM())
    {
        GCToOSInterface::DebugBreak();
    }
}

#ifdef BACKGROUND_GC
BOOL gc_heap::background_allowed_p()
{
    return ( gc_can_use_concurrent && ((settings.pause_mode == pause_interactive) || (settings.pause_mode == pause_sustained_low_latency)) );
}

#endif //BACKGROUND_GC

void gc_heap::check_for_full_gc (int gen_num, size_t size)
{
    BOOL should_notify = FALSE;
    // if we detect full gc because of the allocation budget specified this is TRUE;
    // it's FALSE if it's due to other factors.
    BOOL alloc_factor = TRUE;
    int n_initial = gen_num;
    BOOL local_blocking_collection = FALSE;
    BOOL local_elevation_requested = FALSE;
    int new_alloc_remain_percent = 0;

    if (full_gc_approach_event_set)
    {
        return;
    }

    if (gen_num < max_generation)
    {
        gen_num = max_generation;
    }

    dynamic_data* dd_full = dynamic_data_of (gen_num);
    ptrdiff_t new_alloc_remain = 0;
    uint32_t pct = (gen_num >= uoh_start_generation) ? fgn_loh_percent : fgn_maxgen_percent;

    for (int gen_index = 0; gen_index < total_generation_count; gen_index++)
    {
        dprintf (2, ("FGN: h#%d: gen%d: %zd(%zd)",
                     heap_number, gen_index,
                     dd_new_allocation (dynamic_data_of (gen_index)),
                     dd_desired_allocation (dynamic_data_of (gen_index))));
    }

    // For small object allocations we only check every fgn_check_quantum bytes.
    if (n_initial == 0)
    {
        dprintf (2, ("FGN: gen0 last recorded alloc: %zd", fgn_last_alloc));
        dynamic_data* dd_0 = dynamic_data_of (n_initial);
        if (((fgn_last_alloc - dd_new_allocation (dd_0)) < fgn_check_quantum) &&
            (dd_new_allocation (dd_0) >= 0))
        {
            return;
        }
        else
        {
            fgn_last_alloc = dd_new_allocation (dd_0);
            dprintf (2, ("FGN: gen0 last recorded alloc is now: %zd", fgn_last_alloc));
        }

        // We don't consider the size that came from soh 'cause it doesn't contribute to the
        // gen2 budget.
        size = 0;
    }

    int n = 0;
    for (int i = 1; i <= max_generation; i++)
    {
            if (get_new_allocation (i) <= 0)
            {
                n = i;
            }
            else
                break;
    }

    dprintf (2, ("FGN: h#%d: gen%d budget exceeded", heap_number, n));
    if (gen_num == max_generation)
    {
        // If it's small object heap we should first see if we will even be looking at gen2 budget
        // in the next GC or not. If not we should go directly to checking other factors.
        if (n < (max_generation - 1))
        {
            goto check_other_factors;
        }
    }

    new_alloc_remain = dd_new_allocation (dd_full) - size;

    new_alloc_remain_percent = (int)(((float)(new_alloc_remain) / (float)dd_desired_allocation (dd_full)) * 100);

    dprintf (2, ("FGN: alloc threshold for gen%d is %d%%, current threshold is %d%%",
                 gen_num, pct, new_alloc_remain_percent));

    if (new_alloc_remain_percent <= (int)pct)
    {
#ifdef BACKGROUND_GC
        // If background GC is enabled, we still want to check whether this will
        // be a blocking GC or not because we only want to notify when it's a
        // blocking full GC.
        if (background_allowed_p())
        {
            goto check_other_factors;
        }
#endif //BACKGROUND_GC

        should_notify = TRUE;
        goto done;
    }

check_other_factors:

    dprintf (2, ("FGC: checking other factors"));
    n = generation_to_condemn (n,
                               &local_blocking_collection,
                               &local_elevation_requested,
                               TRUE);

    if (local_elevation_requested && (n == max_generation))
    {
        if (settings.should_lock_elevation)
        {
            int local_elevation_locked_count = settings.elevation_locked_count + 1;
            if (local_elevation_locked_count != 6)
            {
                dprintf (2, ("FGN: lock count is %d - Condemning max_generation-1",
                    local_elevation_locked_count));
                n = max_generation - 1;
            }
        }
    }

    dprintf (2, ("FGN: we estimate gen%d will be collected", n));

#ifdef BACKGROUND_GC
    // When background GC is enabled it decreases the accuracy of our predictability -
    // by the time the GC happens, we may not be under BGC anymore. If we try to
    // predict often enough it should be ok.
    if ((n == max_generation) &&
        (gc_heap::background_running_p()))
    {
        n = max_generation - 1;
        dprintf (2, ("FGN: bgc - 1 instead of 2"));
    }

    if ((n == max_generation) && !local_blocking_collection)
    {
        if (!background_allowed_p())
        {
            local_blocking_collection = TRUE;
        }
    }
#endif //BACKGROUND_GC

    dprintf (2, ("FGN: we estimate gen%d will be collected: %s",
                       n,
                       (local_blocking_collection ? "blocking" : "background")));

    if ((n == max_generation) && local_blocking_collection)
    {
        alloc_factor = FALSE;
        should_notify = TRUE;
        goto done;
    }

done:

    if (should_notify)
    {
        dprintf (2, ("FGN: gen%d detecting full GC approaching(%s) (GC#%zd) (%d%% left in gen%d)",
                     n_initial,
                     (alloc_factor ? "alloc" : "other"),
                     dd_collection_count (dynamic_data_of (0)),
                     new_alloc_remain_percent,
                     gen_num));

        send_full_gc_notification (n_initial, alloc_factor);
    }
}

void gc_heap::send_full_gc_notification (int gen_num, BOOL due_to_alloc_p)
{
    if (!full_gc_approach_event_set)
    {
        assert (full_gc_approach_event.IsValid());
        FIRE_EVENT(GCFullNotify_V1, gen_num, due_to_alloc_p);

        full_gc_end_event.Reset();
        full_gc_approach_event.Set();
        full_gc_approach_event_set = true;
    }
}

size_t gc_heap::get_full_compact_gc_count()
{
    return full_gc_counts[gc_type_compacting];
}

// DTREVIEW - we should check this in dt_low_ephemeral_space_p
// as well.
inline
BOOL gc_heap::short_on_end_of_seg (heap_segment* seg)
{
    uint8_t* allocated = heap_segment_allocated (seg);

#ifdef USE_REGIONS
    assert (end_gen0_region_space != uninitialized_end_gen0_region_space);
    BOOL sufficient_p = sufficient_space_regions_for_allocation (end_gen0_region_space, end_space_after_gc());
#else
    BOOL sufficient_p = sufficient_space_end_seg (allocated,
                                                  heap_segment_committed (seg),
                                                  heap_segment_reserved (seg),
                                                  end_space_after_gc());
#endif //USE_REGIONS
    if (!sufficient_p)
    {
        if (sufficient_gen0_space_p)
        {
            dprintf (GTC_LOG, ("gen0 has enough free space"));
        }

        sufficient_p = sufficient_gen0_space_p;
    }

    return !sufficient_p;
}

inline
BOOL gc_heap::a_fit_free_list_p (int gen_number,
                                 size_t size,
                                 alloc_context* acontext,
                                 uint32_t flags,
                                 int align_const)
{
    BOOL can_fit = FALSE;
    generation* gen = generation_of (gen_number);
    allocator* gen_allocator = generation_allocator (gen);

    for (unsigned int a_l_idx = gen_allocator->first_suitable_bucket(size); a_l_idx < gen_allocator->number_of_buckets(); a_l_idx++)
    {
        uint8_t* free_list = gen_allocator->alloc_list_head_of (a_l_idx);
        uint8_t* prev_free_item = 0;

        while (free_list != 0)
        {
            dprintf (3, ("considering free list %zx", (size_t)free_list));
            size_t free_list_size = unused_array_size (free_list);
            if ((size + Align (min_obj_size, align_const)) <= free_list_size)
            {
                dprintf (3, ("Found adequate unused area: [%zx, size: %zd",
                                (size_t)free_list, free_list_size));

                gen_allocator->unlink_item (a_l_idx, free_list, prev_free_item, FALSE);
                // We ask for more Align (min_obj_size)
                // to make sure that we can insert a free object
                // in adjust_limit will set the limit lower
                size_t limit = limit_from_size (size, flags, free_list_size, gen_number, align_const);
                dd_new_allocation (dynamic_data_of (gen_number)) -= limit;

                uint8_t*  remain = (free_list + limit);
                size_t remain_size = (free_list_size - limit);
                if (remain_size >= Align(min_free_list, align_const))
                {
                    make_unused_array (remain, remain_size);
                    gen_allocator->thread_item_front (remain, remain_size);
                    assert (remain_size >= Align (min_obj_size, align_const));
                }
                else
                {
                    //absorb the entire free list
                    limit += remain_size;
                }
                generation_free_list_space (gen) -= limit;
                assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);

                adjust_limit_clr (free_list, limit, size, acontext, flags, 0, align_const, gen_number);

                can_fit = TRUE;
                goto end;
            }
            else if (gen_allocator->discard_if_no_fit_p())
            {
                assert (prev_free_item == 0);
                dprintf (3, ("couldn't use this free area, discarding"));
                generation_free_obj_space (gen) += free_list_size;

                gen_allocator->unlink_item (a_l_idx, free_list, prev_free_item, FALSE);
                generation_free_list_space (gen) -= free_list_size;
                assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);
            }
            else
            {
                prev_free_item = free_list;
            }
            free_list = free_list_slot (free_list);
        }
    }
end:
    return can_fit;
}

#ifdef BACKGROUND_GC
void gc_heap::bgc_uoh_alloc_clr (uint8_t* alloc_start,
                                 size_t size,
                                 alloc_context* acontext,
                                 uint32_t flags,
                                 int gen_number,
                                 int align_const,
                                 int lock_index,
                                 BOOL check_used_p,
                                 heap_segment* seg)
{
    make_unused_array (alloc_start, size);
#ifdef DOUBLY_LINKED_FL
    clear_prev_bit (alloc_start, size);
#endif //DOUBLY_LINKED_FL

    size_t size_of_array_base = sizeof(ArrayBase);

    bgc_alloc_lock->uoh_alloc_done_with_index (lock_index);

    // clear memory while not holding the lock.
    size_t size_to_skip = size_of_array_base;
    size_t size_to_clear = size - size_to_skip - plug_skew;
    size_t saved_size_to_clear = size_to_clear;
    if (check_used_p)
    {
        uint8_t* end = alloc_start + size - plug_skew;
        uint8_t* used = heap_segment_used (seg);
        if (used < end)
        {
            if ((alloc_start + size_to_skip) < used)
            {
                size_to_clear = used - (alloc_start + size_to_skip);
            }
            else
            {
                size_to_clear = 0;
            }
            dprintf (2, ("bgc uoh: setting used to %p", end));
            heap_segment_used (seg) = end;
        }

        dprintf (2, ("bgc uoh: used: %p, alloc: %p, end of alloc: %p, clear %zd bytes",
                     used, alloc_start, end, size_to_clear));
    }
    else
    {
        dprintf (2, ("bgc uoh: [%p-[%p(%zd)", alloc_start, alloc_start+size, size));
    }

#ifdef VERIFY_HEAP
    // since we filled in 0xcc for free object when we verify heap,
    // we need to make sure we clear those bytes.
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_GC)
    {
        if (size_to_clear < saved_size_to_clear)
        {
            size_to_clear = saved_size_to_clear;
        }
    }
#endif //VERIFY_HEAP

    size_t allocated_size = size - Align (min_obj_size, align_const);
    total_alloc_bytes_uoh += allocated_size;
    size_t etw_allocation_amount = 0;
    bool fire_event_p = update_alloc_info (gen_number, allocated_size, &etw_allocation_amount);

    dprintf (SPINLOCK_LOG, ("[%d]Lmsl to clear uoh obj", heap_number));
    add_saved_spinlock_info (true, me_release, mt_clr_large_mem, msl_entered);
    leave_spin_lock (&more_space_lock_uoh);

#ifdef FEATURE_EVENT_TRACE
    if (fire_event_p)
    {
        fire_etw_allocation_event (etw_allocation_amount, gen_number, alloc_start, size);
    }
#endif //FEATURE_EVENT_TRACE

    ((void**) alloc_start)[-1] = 0;     //clear the sync block
    if (!(flags & GC_ALLOC_ZEROING_OPTIONAL))
    {
        memclr(alloc_start + size_to_skip, size_to_clear);
    }

#ifdef MULTIPLE_HEAPS
    assert (heap_of (alloc_start) == this);
#endif // MULTIPLE_HEAPS

    bgc_alloc_lock->uoh_alloc_set (alloc_start);

    acontext->alloc_ptr = alloc_start;
    acontext->alloc_limit = (alloc_start + size - Align (min_obj_size, align_const));

    // need to clear the rest of the object before we hand it out.
    clear_unused_array(alloc_start, size);
}

#endif //BACKGROUND_GC

BOOL gc_heap::a_fit_free_list_uoh_p (size_t size,
                                       alloc_context* acontext,
                                       uint32_t flags,
                                       int align_const,
                                       int gen_number)
{
    BOOL can_fit = FALSE;
    generation* gen = generation_of (gen_number);
    allocator* allocator = generation_allocator (gen);

#ifdef FEATURE_LOH_COMPACTION
    size_t loh_pad = (gen_number == loh_generation) ? Align (loh_padding_obj_size, align_const) : 0;
#endif //FEATURE_LOH_COMPACTION

#ifdef BACKGROUND_GC
    int cookie = -1;
#endif //BACKGROUND_GC

    for (unsigned int a_l_idx = allocator->first_suitable_bucket(size); a_l_idx < allocator->number_of_buckets(); a_l_idx++)
    {
        uint8_t* free_list = allocator->alloc_list_head_of (a_l_idx);
        uint8_t* prev_free_item = 0;
        while (free_list != 0)
        {
            dprintf (3, ("considering free list %zx", (size_t)free_list));

            size_t free_list_size = unused_array_size(free_list);

            ptrdiff_t diff = free_list_size - size;

#ifdef FEATURE_LOH_COMPACTION
            diff -= loh_pad;
#endif //FEATURE_LOH_COMPACTION

            // must fit exactly or leave formattable space
            if ((diff == 0) || (diff >= (ptrdiff_t)Align (min_obj_size, align_const)))
            {
#ifdef BACKGROUND_GC
#ifdef MULTIPLE_HEAPS
                assert (heap_of (free_list) == this);
#endif // MULTIPLE_HEAPS

                cookie = bgc_alloc_lock->uoh_alloc_set (free_list);
                bgc_track_uoh_alloc();
#endif //BACKGROUND_GC

                allocator->unlink_item (a_l_idx, free_list, prev_free_item, FALSE);
                remove_gen_free (gen_number, free_list_size);

                // Subtract min obj size because limit_from_size adds it. Not needed for LOH
                size_t limit = limit_from_size (size - Align(min_obj_size, align_const), flags, free_list_size,
                                                gen_number, align_const);
                dd_new_allocation (dynamic_data_of (gen_number)) -= limit;

                size_t saved_free_list_size = free_list_size;
#ifdef FEATURE_LOH_COMPACTION
                if (loh_pad)
                {
                    make_unused_array (free_list, loh_pad);
                    generation_free_obj_space (gen) += loh_pad;
                    limit -= loh_pad;
                    free_list += loh_pad;
                    free_list_size -= loh_pad;
                }
#endif //FEATURE_LOH_COMPACTION

                uint8_t*  remain = (free_list + limit);
                size_t remain_size = (free_list_size - limit);
                if (remain_size != 0)
                {
                    assert (remain_size >= Align (min_obj_size, align_const));
                    make_unused_array (remain, remain_size);
                }
                if (remain_size >= Align(min_free_list, align_const))
                {
                    uoh_thread_gap_front (remain, remain_size, gen);
                    add_gen_free (gen_number, remain_size);
                    assert (remain_size >= Align (min_obj_size, align_const));
                }
                else
                {
                    generation_free_obj_space (gen) += remain_size;
                }
                generation_free_list_space (gen) -= saved_free_list_size;
                assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);
                generation_free_list_allocated (gen) += limit;

                dprintf (3, ("found fit on loh at %p", free_list));
#ifdef BACKGROUND_GC
                if (cookie != -1)
                {
                    bgc_uoh_alloc_clr (free_list, limit, acontext, flags, gen_number, align_const, cookie, FALSE, 0);
                }
                else
#endif //BACKGROUND_GC
                {
                    adjust_limit_clr (free_list, limit, size, acontext, flags, 0, align_const, gen_number);
                }

                //fix the limit to compensate for adjust_limit_clr making it too short
                acontext->alloc_limit += Align (min_obj_size, align_const);
                can_fit = TRUE;
                goto exit;
            }
            prev_free_item = free_list;
            free_list = free_list_slot (free_list);
        }
    }
exit:
    return can_fit;
}

BOOL gc_heap::a_fit_segment_end_p (int gen_number,
                                   heap_segment* seg,
                                   size_t size,
                                   alloc_context* acontext,
                                   uint32_t flags,
                                   int align_const,
                                   BOOL* commit_failed_p)
{
    *commit_failed_p = FALSE;
    size_t limit = 0;
    bool hard_limit_short_seg_end_p = false;
#ifdef BACKGROUND_GC
    int cookie = -1;
#endif //BACKGROUND_GC

    uint8_t*& allocated = ((gen_number == 0) ?
                                    alloc_allocated :
                                    heap_segment_allocated(seg));

    size_t pad = Align (min_obj_size, align_const);

#ifdef FEATURE_LOH_COMPACTION
    size_t loh_pad = Align (loh_padding_obj_size, align_const);
    if (gen_number == loh_generation)
    {
        pad += loh_pad;
    }
#endif //FEATURE_LOH_COMPACTION

    uint8_t* end = heap_segment_committed (seg) - pad;

    if (a_size_fit_p (size, allocated, end, align_const))
    {
        limit = limit_from_size (size,
                                 flags,
                                 (end - allocated),
                                 gen_number, align_const);
        goto found_fit;
    }

    end = heap_segment_reserved (seg) - pad;

    if ((heap_segment_reserved (seg) != heap_segment_committed (seg)) && (a_size_fit_p (size, allocated, end, align_const)))
    {
        limit = limit_from_size (size,
                                 flags,
                                 (end - allocated),
                                 gen_number, align_const);

        if (grow_heap_segment (seg, (allocated + limit), &hard_limit_short_seg_end_p))
        {
            goto found_fit;
        }

        else
        {
#ifdef USE_REGIONS
            *commit_failed_p = TRUE;
#else
            if (!hard_limit_short_seg_end_p)
            {
                dprintf (2, ("can't grow segment, doing a full gc"));
                *commit_failed_p = TRUE;
            }
            else
            {
                assert (heap_hard_limit);
            }
#endif // USE_REGIONS
        }
    }

    goto found_no_fit;

found_fit:
    dd_new_allocation (dynamic_data_of (gen_number)) -= limit;

#ifdef BACKGROUND_GC
    if (gen_number != 0)
    {
#ifdef MULTIPLE_HEAPS
        assert (heap_of (allocated) == this);
#endif // MULTIPLE_HEAPS

        cookie = bgc_alloc_lock->uoh_alloc_set (allocated);
        bgc_track_uoh_alloc();
    }
#endif //BACKGROUND_GC

#ifdef FEATURE_LOH_COMPACTION
    if (gen_number == loh_generation)
    {
        make_unused_array (allocated, loh_pad);
        generation_free_obj_space (generation_of (gen_number)) += loh_pad;
        allocated += loh_pad;
        limit -= loh_pad;
    }
#endif //FEATURE_LOH_COMPACTION

#if defined (VERIFY_HEAP) && defined (_DEBUG)
    // we are responsible for cleaning the syncblock and we will do it later
    // as a part of cleanup routine and when not holding the heap lock.
    // However, once we move "allocated" forward and if another thread initiate verification of
    // the previous object, it may consider the syncblock in the "next" eligible for validation.
    // (see also: object.cpp/Object::ValidateInner)
    // Make sure it will see cleaned up state to prevent triggering occasional verification failures.
    // And make sure the write happens before updating "allocated"
    ((void**)allocated)[-1] = 0;    // clear the sync block
    VOLATILE_MEMORY_BARRIER();
#endif //VERIFY_HEAP && _DEBUG

    uint8_t* old_alloc;
    old_alloc = allocated;
    dprintf (3, ("found fit at end of seg: %p", old_alloc));

#ifdef BACKGROUND_GC
    if (cookie != -1)
    {
        bgc_record_uoh_end_seg_allocation (gen_number, limit);
        allocated += limit;
        bgc_uoh_alloc_clr (old_alloc, limit, acontext, flags, gen_number, align_const, cookie, TRUE, seg);
    }
    else
#endif //BACKGROUND_GC
    {
        // In a contiguous AC case with GC_ALLOC_ZEROING_OPTIONAL, deduct unspent space from the limit to
        // clear only what is necessary.
        if ((flags & GC_ALLOC_ZEROING_OPTIONAL) &&
            ((allocated == acontext->alloc_limit) ||
             (allocated == (acontext->alloc_limit + Align (min_obj_size, align_const)))))
        {
            assert(gen_number == 0);
            assert(allocated > acontext->alloc_ptr);

            size_t extra = allocated - acontext->alloc_ptr;
            limit -= extra;

            // Since we are not consuming all the memory we already deducted from the budget,
            // we should put the extra back.
            dynamic_data* dd = dynamic_data_of (0);
            dd_new_allocation (dd) += extra;

            // add space for an AC continuity divider
            limit += Align(min_obj_size, align_const);
        }

#ifdef BACKGROUND_GC
        bgc_record_uoh_end_seg_allocation (gen_number, limit);
#endif

        allocated += limit;
        adjust_limit_clr (old_alloc, limit, size, acontext, flags, seg, align_const, gen_number);
    }

    return TRUE;

found_no_fit:

    return FALSE;
}

BOOL gc_heap::uoh_a_fit_segment_end_p (int gen_number,
                                       size_t size,
                                       alloc_context* acontext,
                                       uint32_t flags,
                                       int align_const,
                                       BOOL* commit_failed_p,
                                       oom_reason* oom_r)
{
    *commit_failed_p = FALSE;

    generation* gen = generation_of (gen_number);
    heap_segment* seg = generation_allocation_segment (gen);
    BOOL can_allocate_p = FALSE;

    while (seg)
    {
#ifdef BACKGROUND_GC
        if (seg->flags & heap_segment_flags_uoh_delete)
        {
            dprintf (3, ("h%d skipping seg %zx to be deleted", heap_number, (size_t)seg));
        }
        else
#endif //BACKGROUND_GC
        {
            if (a_fit_segment_end_p (gen_number, seg, (size - Align (min_obj_size, align_const)),
                                        acontext, flags, align_const, commit_failed_p))
            {
                acontext->alloc_limit += Align (min_obj_size, align_const);
                can_allocate_p = TRUE;
                break;
            }

            if (*commit_failed_p)
            {
                *oom_r = oom_cant_commit;
                break;
            }
        }

        seg = heap_segment_next_rw (seg);
    }

    if (can_allocate_p)
    {
        generation_end_seg_allocated (gen) += size;
    }

    return can_allocate_p;
}

#ifdef BACKGROUND_GC
inline
enter_msl_status gc_heap::wait_for_background (alloc_wait_reason awr, bool loh_p)
{
    GCSpinLock* msl = loh_p ? &more_space_lock_uoh : &more_space_lock_soh;
    enter_msl_status msl_status = msl_entered;

    dprintf (2, ("BGC is already in progress, waiting for it to finish"));
    add_saved_spinlock_info (loh_p, me_release, mt_wait_bgc, msl_status);
    leave_spin_lock (msl);
    background_gc_wait (awr);
    msl_status = enter_spin_lock_msl (msl);
    add_saved_spinlock_info (loh_p, me_acquire, mt_wait_bgc, msl_status);

    return msl_status;
}

bool gc_heap::wait_for_bgc_high_memory (alloc_wait_reason awr, bool loh_p, enter_msl_status* msl_status)
{
    bool wait_p = false;
    if (gc_heap::background_running_p())
    {
        uint32_t memory_load;
        get_memory_info (&memory_load);
        if (memory_load >= m_high_memory_load_th)
        {
            wait_p = true;
            dprintf (GTC_LOG, ("high mem - wait for BGC to finish, wait reason: %d", awr));
            *msl_status = wait_for_background (awr, loh_p);
        }
    }

    return wait_p;
}

#endif //BACKGROUND_GC

// We request to trigger an ephemeral GC but we may get a full compacting GC.
// return TRUE if that's the case.
BOOL gc_heap::trigger_ephemeral_gc (gc_reason gr, enter_msl_status* msl_status)
{
#ifdef BACKGROUND_GC
    wait_for_bgc_high_memory (awr_loh_oos_bgc, false, msl_status);
    if (*msl_status == msl_retry_different_heap) return FALSE;
#endif //BACKGROUND_GC

    BOOL did_full_compact_gc = FALSE;

    dprintf (1, ("h%d triggering a gen1 GC", heap_number));
    size_t last_full_compact_gc_count = get_full_compact_gc_count();
    vm_heap->GarbageCollectGeneration(max_generation - 1, gr);

#ifdef MULTIPLE_HEAPS
    *msl_status = enter_spin_lock_msl (&more_space_lock_soh);
    if (*msl_status == msl_retry_different_heap) return FALSE;
    add_saved_spinlock_info (false, me_acquire, mt_t_eph_gc, *msl_status);
#endif //MULTIPLE_HEAPS

    size_t current_full_compact_gc_count = get_full_compact_gc_count();

    if (current_full_compact_gc_count > last_full_compact_gc_count)
    {
        dprintf (2, ("attempted to trigger an ephemeral GC and got a full compacting GC"));
        did_full_compact_gc = TRUE;
    }

    return did_full_compact_gc;
}

BOOL gc_heap::soh_try_fit (int gen_number,
                           size_t size,
                           alloc_context* acontext,
                           uint32_t flags,
                           int align_const,
                           BOOL* commit_failed_p,
                           BOOL* short_seg_end_p)
{
    BOOL can_allocate = TRUE;
    if (short_seg_end_p)
    {
        *short_seg_end_p = FALSE;
    }

    can_allocate = a_fit_free_list_p (gen_number, size, acontext, flags, align_const);
    if (!can_allocate)
    {
        if (short_seg_end_p)
        {
            *short_seg_end_p = short_on_end_of_seg (ephemeral_heap_segment);
        }
        // If the caller doesn't care, we always try to fit at the end of seg;
        // otherwise we would only try if we are actually not short at end of seg.
        if (!short_seg_end_p || !(*short_seg_end_p))
        {
#ifdef USE_REGIONS
            while (ephemeral_heap_segment)
#endif //USE_REGIONS
            {
                can_allocate = a_fit_segment_end_p (gen_number, ephemeral_heap_segment, size,
                                                    acontext, flags, align_const, commit_failed_p);
#ifdef USE_REGIONS
                if (can_allocate)
                {
                    break;
                }

                dprintf (REGIONS_LOG, ("h%d fixing region %p end to alloc ptr: %p, alloc_allocated %p",
                    heap_number, heap_segment_mem (ephemeral_heap_segment), acontext->alloc_ptr,
                    alloc_allocated));

                fix_allocation_context (acontext, TRUE, FALSE);
                fix_youngest_allocation_area();

                heap_segment* next_seg = heap_segment_next (ephemeral_heap_segment);
                bool new_seg = false;

                if (!next_seg)
                {
                    assert (ephemeral_heap_segment == generation_tail_region (generation_of (gen_number)));
                    next_seg = get_new_region (gen_number);
                    new_seg = true;
                }

                if (next_seg)
                {
                    dprintf (REGIONS_LOG, ("eph seg %p -> next %p",
                        heap_segment_mem (ephemeral_heap_segment), heap_segment_mem (next_seg)));
                    ephemeral_heap_segment = next_seg;
                    if (new_seg)
                    {
                        GCToEEInterface::DiagAddNewRegion(
                            heap_segment_gen_num (next_seg),
                            heap_segment_mem (next_seg),
                            heap_segment_allocated (next_seg),
                            heap_segment_reserved (next_seg)
                        );
                    }
                }
                else
                {
                    *commit_failed_p = TRUE;
                    dprintf (REGIONS_LOG, ("couldn't get a new ephemeral region"));
                    return FALSE;
                }

                alloc_allocated = heap_segment_allocated (ephemeral_heap_segment);
                dprintf (REGIONS_LOG, ("h%d alloc_allocated is now %p", heap_number, alloc_allocated));
#endif //USE_REGIONS
            }
        }
    }

    return can_allocate;
}

allocation_state gc_heap::allocate_soh (int gen_number,
                                          size_t size,
                                          alloc_context* acontext,
                                          uint32_t flags,
                                          int align_const)
{
    enter_msl_status msl_status = msl_entered;

#if defined (BACKGROUND_GC) && !defined (MULTIPLE_HEAPS)
    if (gc_heap::background_running_p())
    {
        background_soh_alloc_count++;
        if ((background_soh_alloc_count % bgc_alloc_spin_count) == 0)
        {
            add_saved_spinlock_info (false, me_release, mt_alloc_small, msl_status);
            leave_spin_lock (&more_space_lock_soh);
            bool cooperative_mode = enable_preemptive();
            GCToOSInterface::Sleep (bgc_alloc_spin);
            disable_preemptive (cooperative_mode);

            msl_status = enter_spin_lock_msl (&more_space_lock_soh);
            if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;

            add_saved_spinlock_info (false, me_acquire, mt_alloc_small, msl_status);
        }
        else
        {
            //GCToOSInterface::YieldThread (0);
        }
    }
#endif //BACKGROUND_GC && !MULTIPLE_HEAPS

    gc_reason gr = reason_oos_soh;
    oom_reason oom_r = oom_no_failure;

    // No variable values should be "carried over" from one state to the other.
    // That's why there are local variable for each state

    allocation_state soh_alloc_state = a_state_start;

    // If we can get a new seg it means allocation will succeed.
    while (1)
    {
        dprintf (3, ("[h%d]soh state is %s", heap_number, allocation_state_str[soh_alloc_state]));

        switch (soh_alloc_state)
        {
            case a_state_can_allocate:
            case a_state_cant_allocate:
            {
                goto exit;
            }
            case a_state_start:
            {
                soh_alloc_state = a_state_try_fit;
                break;
            }
            case a_state_try_fit:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;

                can_use_existing_p = soh_try_fit (gen_number, size, acontext, flags,
                                                  align_const, &commit_failed_p,
                                                  NULL);
                soh_alloc_state = (can_use_existing_p ?
                                        a_state_can_allocate :
                                        (commit_failed_p ?
                                            a_state_trigger_full_compact_gc :
                                            a_state_trigger_ephemeral_gc));
                break;
            }
            case a_state_try_fit_after_bgc:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;
                BOOL short_seg_end_p = FALSE;

                can_use_existing_p = soh_try_fit (gen_number, size, acontext, flags,
                                                  align_const, &commit_failed_p,
                                                  &short_seg_end_p);
                soh_alloc_state = (can_use_existing_p ?
                                        a_state_can_allocate :
                                        (short_seg_end_p ?
                                            a_state_trigger_2nd_ephemeral_gc :
                                            a_state_trigger_full_compact_gc));
                break;
            }
            case a_state_try_fit_after_cg:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;
                BOOL short_seg_end_p = FALSE;

                can_use_existing_p = soh_try_fit (gen_number, size, acontext, flags,
                                                  align_const, &commit_failed_p,
                                                  &short_seg_end_p);

                if (can_use_existing_p)
                {
                    soh_alloc_state = a_state_can_allocate;
                }
#ifdef MULTIPLE_HEAPS
                else if (gen0_allocated_after_gc_p)
                {
                    // some other threads already grabbed the more space lock and allocated
                    // so we should attempt an ephemeral GC again.
                    soh_alloc_state = a_state_trigger_ephemeral_gc;
                }
#endif //MULTIPLE_HEAPS
                else if (short_seg_end_p)
                {
                    soh_alloc_state = a_state_cant_allocate;
                    oom_r = oom_budget;
                }
                else
                {
                    assert (commit_failed_p || heap_hard_limit);
                    soh_alloc_state = a_state_cant_allocate;
                    oom_r = oom_cant_commit;
                }
                break;
            }
            case a_state_check_and_wait_for_bgc:
            {
                BOOL bgc_in_progress_p = FALSE;
                BOOL did_full_compacting_gc = FALSE;

                bgc_in_progress_p = check_and_wait_for_bgc (awr_gen0_oos_bgc, &did_full_compacting_gc, false, &msl_status);
                if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;

                soh_alloc_state = (did_full_compacting_gc ?
                                        a_state_try_fit_after_cg :
                                        a_state_try_fit_after_bgc);
                break;
            }
            case a_state_trigger_ephemeral_gc:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;
                BOOL short_seg_end_p = FALSE;
                BOOL bgc_in_progress_p = FALSE;
                BOOL did_full_compacting_gc = FALSE;

                did_full_compacting_gc = trigger_ephemeral_gc (gr, &msl_status);
                if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;

                if (did_full_compacting_gc)
                {
                    soh_alloc_state = a_state_try_fit_after_cg;
                }
                else
                {
                    can_use_existing_p = soh_try_fit (gen_number, size, acontext, flags,
                                                      align_const, &commit_failed_p,
                                                      &short_seg_end_p);
#ifdef BACKGROUND_GC
                    bgc_in_progress_p = gc_heap::background_running_p();
#endif //BACKGROUND_GC

                    if (can_use_existing_p)
                    {
                        soh_alloc_state = a_state_can_allocate;
                    }
                    else
                    {
                        if (short_seg_end_p)
                        {
#ifndef USE_REGIONS
                            if (should_expand_in_full_gc)
                            {
                                dprintf (2, ("gen1 GC wanted to expand!"));
                                soh_alloc_state = a_state_trigger_full_compact_gc;
                            }
                            else
#endif //!USE_REGIONS
                            {
                                soh_alloc_state = (bgc_in_progress_p ?
                                                        a_state_check_and_wait_for_bgc :
                                                        a_state_trigger_full_compact_gc);
                            }
                        }
                        else if (commit_failed_p)
                        {
                            soh_alloc_state = a_state_trigger_full_compact_gc;
                        }
                        else
                        {
#ifdef MULTIPLE_HEAPS
                            // some other threads already grabbed the more space lock and allocated
                            // so we should attempt an ephemeral GC again.
                            assert (gen0_allocated_after_gc_p);
                            soh_alloc_state = a_state_trigger_ephemeral_gc;
#else //MULTIPLE_HEAPS
                            assert (!"shouldn't get here");
#endif //MULTIPLE_HEAPS
                        }
                    }
                }
                break;
            }
            case a_state_trigger_2nd_ephemeral_gc:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;
                BOOL short_seg_end_p = FALSE;
                BOOL did_full_compacting_gc = FALSE;

                did_full_compacting_gc = trigger_ephemeral_gc (gr, &msl_status);
                if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;

                if (did_full_compacting_gc)
                {
                    soh_alloc_state = a_state_try_fit_after_cg;
                }
                else
                {
                    can_use_existing_p = soh_try_fit (gen_number, size, acontext, flags,
                                                      align_const, &commit_failed_p,
                                                      &short_seg_end_p);
                    if (short_seg_end_p || commit_failed_p)
                    {
                        soh_alloc_state = a_state_trigger_full_compact_gc;
                    }
                    else
                    {
                        assert (can_use_existing_p);
                        soh_alloc_state = a_state_can_allocate;
                    }
                }
                break;
            }
            case a_state_trigger_full_compact_gc:
            {
                if (fgn_maxgen_percent)
                {
                    dprintf (2, ("FGN: SOH doing last GC before we throw OOM"));
                    send_full_gc_notification (max_generation, FALSE);
                }

                BOOL got_full_compacting_gc = FALSE;

                got_full_compacting_gc = trigger_full_compact_gc (gr, &oom_r, false, &msl_status);
                if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;

                soh_alloc_state = (got_full_compacting_gc ? a_state_try_fit_after_cg : a_state_cant_allocate);
                break;
            }
            default:
            {
                assert (!"Invalid state!");
                break;
            }
        }
    }

exit:
    if (soh_alloc_state == a_state_cant_allocate)
    {
        assert (oom_r != oom_no_failure);
        handle_oom (oom_r,
                    size,
                    heap_segment_allocated (ephemeral_heap_segment),
                    heap_segment_reserved (ephemeral_heap_segment));

        add_saved_spinlock_info (false, me_release, mt_alloc_small_cant, msl_entered);
        leave_spin_lock (&more_space_lock_soh);
    }

    assert ((soh_alloc_state == a_state_can_allocate) ||
            (soh_alloc_state == a_state_cant_allocate) ||
            (soh_alloc_state == a_state_retry_allocate));

    return soh_alloc_state;
}

#ifdef BACKGROUND_GC
inline
void gc_heap::bgc_track_uoh_alloc()
{
    if (current_c_gc_state == c_gc_state_planning)
    {
        Interlocked::Increment (&uoh_alloc_thread_count);
        dprintf (3, ("h%d: inc lc: %d", heap_number, (int32_t)uoh_alloc_thread_count));
    }
}

inline
void gc_heap::bgc_untrack_uoh_alloc()
{
    if (current_c_gc_state == c_gc_state_planning)
    {
        Interlocked::Decrement (&uoh_alloc_thread_count);
        dprintf (3, ("h%d: dec lc: %d", heap_number, (int32_t)uoh_alloc_thread_count));
    }
}

#endif //BACKGROUND_GC

size_t gc_heap::get_uoh_seg_size (size_t size)
{
    size_t default_seg_size =
#ifdef USE_REGIONS
        global_region_allocator.get_large_region_alignment();
#else
        min_uoh_segment_size;
#endif //USE_REGIONS
    size_t align_size =  default_seg_size;
    int align_const = get_alignment_constant (FALSE);
    size_t large_seg_size = align_on_page (
        max (default_seg_size,
            ((size + 2 * Align(min_obj_size, align_const) + OS_PAGE_SIZE +
            align_size) / align_size * align_size)));
    return large_seg_size;
}

BOOL gc_heap::uoh_get_new_seg (int gen_number,
                               size_t size,
                               BOOL* did_full_compact_gc,
                               oom_reason* oom_r,
                               enter_msl_status* msl_status)
{
    *did_full_compact_gc = FALSE;

    size_t seg_size = get_uoh_seg_size (size);

    heap_segment* new_seg = get_uoh_segment (gen_number, seg_size, did_full_compact_gc, msl_status);
    if (*msl_status == msl_retry_different_heap) return FALSE;

    if (new_seg && (gen_number == loh_generation))
    {
        loh_alloc_since_cg += seg_size;
    }
    else
    {
        *oom_r = oom_loh;
    }

    return (new_seg != 0);
}

// PERF TODO: this is too aggressive; and in hard limit we should
// count the actual allocated bytes instead of only updating it during
// getting a new seg.
BOOL gc_heap::retry_full_compact_gc (size_t size)
{
    size_t seg_size = get_uoh_seg_size (size);

    if (loh_alloc_since_cg >= (2 * (uint64_t)seg_size))
    {
        return TRUE;
    }

#ifdef MULTIPLE_HEAPS
    uint64_t total_alloc_size = 0;
    for (int i = 0; i < n_heaps; i++)
    {
        total_alloc_size += g_heaps[i]->loh_alloc_since_cg;
    }

    if (total_alloc_size >= (2 * (uint64_t)seg_size))
    {
        return TRUE;
    }
#endif //MULTIPLE_HEAPS

    return FALSE;
}

BOOL gc_heap::check_and_wait_for_bgc (alloc_wait_reason awr,
                                      BOOL* did_full_compact_gc,
                                      bool loh_p,
                                      enter_msl_status* msl_status)
{
    BOOL bgc_in_progress = FALSE;
    *did_full_compact_gc = FALSE;
#ifdef BACKGROUND_GC
    if (gc_heap::background_running_p())
    {
        bgc_in_progress = TRUE;
        size_t last_full_compact_gc_count = get_full_compact_gc_count();
        *msl_status = wait_for_background (awr, loh_p);
        size_t current_full_compact_gc_count = get_full_compact_gc_count();
        if (current_full_compact_gc_count > last_full_compact_gc_count)
        {
            *did_full_compact_gc = TRUE;
        }
    }
#endif //BACKGROUND_GC

    return bgc_in_progress;
}

BOOL gc_heap::uoh_try_fit (int gen_number,
                           size_t size,
                           alloc_context* acontext,
                           uint32_t flags,
                           int align_const,
                           BOOL* commit_failed_p,
                           oom_reason* oom_r)
{
    BOOL can_allocate = TRUE;

    if (!a_fit_free_list_uoh_p (size, acontext, flags, align_const, gen_number))
    {
        can_allocate = uoh_a_fit_segment_end_p (gen_number, size,
                                                acontext, flags, align_const,
                                                commit_failed_p, oom_r);

    }

    return can_allocate;
}

BOOL gc_heap::trigger_full_compact_gc (gc_reason gr,
                                       oom_reason* oom_r,
                                       bool loh_p,
                                       enter_msl_status* msl_status)
{
    BOOL did_full_compact_gc = FALSE;

    size_t last_full_compact_gc_count = get_full_compact_gc_count();

    // Set this so the next GC will be a full compacting GC.
    if (!last_gc_before_oom)
    {
        last_gc_before_oom = TRUE;
    }

#ifdef BACKGROUND_GC
    if (gc_heap::background_running_p())
    {
        *msl_status = wait_for_background (((gr == reason_oos_soh) ? awr_gen0_oos_bgc : awr_loh_oos_bgc), loh_p);
        dprintf (2, ("waited for BGC - done"));
        if (*msl_status == msl_retry_different_heap) return FALSE;
    }
#endif //BACKGROUND_GC

    GCSpinLock* msl = loh_p ? &more_space_lock_uoh : &more_space_lock_soh;
    size_t current_full_compact_gc_count = get_full_compact_gc_count();
    if (current_full_compact_gc_count > last_full_compact_gc_count)
    {
        dprintf (3, ("a full compacting GC triggered while waiting for BGC (%zd->%zd)", last_full_compact_gc_count, current_full_compact_gc_count));
        assert (current_full_compact_gc_count > last_full_compact_gc_count);
        did_full_compact_gc = TRUE;
        goto exit;
    }

    dprintf (3, ("h%d full GC", heap_number));

    *msl_status = trigger_gc_for_alloc (max_generation, gr, msl, loh_p, mt_t_full_gc);

    current_full_compact_gc_count = get_full_compact_gc_count();

    if (current_full_compact_gc_count == last_full_compact_gc_count)
    {
        dprintf (2, ("attempted to trigger a full compacting GC but didn't get it"));
        // We requested a full GC but didn't get because of the elevation logic
        // which means we should fail.
        *oom_r = oom_unproductive_full_gc;
    }
    else
    {
        dprintf (3, ("h%d: T full compacting GC (%zd->%zd)",
            heap_number,
            last_full_compact_gc_count,
            current_full_compact_gc_count));

        assert (current_full_compact_gc_count > last_full_compact_gc_count);
        did_full_compact_gc = TRUE;
    }

exit:
    return did_full_compact_gc;
}

#ifdef RECORD_LOH_STATE
void gc_heap::add_saved_loh_state (allocation_state loh_state_to_save, EEThreadId thread_id)
{
    // When the state is can_allocate we already have released the more
    // space lock. So we are not logging states here since this code
    // is not thread safe.
    if (loh_state_to_save != a_state_can_allocate)
    {
        last_loh_states[loh_state_index].alloc_state = loh_state_to_save;
        last_loh_states[loh_state_index].gc_index = VolatileLoadWithoutBarrier (&settings.gc_index);
        last_loh_states[loh_state_index].thread_id = thread_id;
        loh_state_index++;

        if (loh_state_index == max_saved_loh_states)
        {
            loh_state_index = 0;
        }

        assert (loh_state_index < max_saved_loh_states);
    }
}

#endif //RECORD_LOH_STATE

bool gc_heap::should_retry_other_heap (int gen_number, size_t size)
{
#ifdef MULTIPLE_HEAPS
    if (heap_hard_limit)
    {
        size_t min_size = dd_min_size (g_heaps[0]->dynamic_data_of (gen_number));
        size_t slack_space = max (commit_min_th, min_size);
        bool retry_p = ((current_total_committed + size) < (heap_hard_limit - slack_space));
        dprintf (1, ("%zd - %zd - total committed %zd - size %zd = %zd, %s",
            heap_hard_limit, slack_space, current_total_committed, size,
            (heap_hard_limit - slack_space - current_total_committed - size),
            (retry_p ? "retry" : "no retry")));
        return retry_p;
    }
    else
#endif //MULTIPLE_HEAPS
    {
        return false;
    }
}

#ifdef BACKGROUND_GC
uoh_allocation_action gc_heap::get_bgc_allocate_action (int gen_number)
{
    int uoh_idx = gen_number - uoh_start_generation;

    // We always allocate normally if the total size is small enough.
    if (bgc_uoh_current_size[uoh_idx] < (dd_min_size (dynamic_data_of (gen_number)) * 10))
    {
        return uoh_alloc_normal;
    }

#ifndef USE_REGIONS
    // This is legacy behavior for segments - segments' sizes are usually very stable. But for regions we could
    // have released a bunch of regions into the free pool during the last gen2 GC so checking the last UOH size
    // doesn't make sense.
    if (bgc_begin_uoh_size[uoh_idx] >= (2 * end_uoh_size[uoh_idx]))
    {
        dprintf (3, ("h%d alloc-ed too much before bgc started, last end %Id, this start %Id, wait",
            heap_number, end_uoh_size[uoh_idx], bgc_begin_uoh_size[uoh_idx]));
        return uoh_alloc_wait;
    }
#endif //USE_REGIONS

    size_t size_increased = bgc_uoh_current_size[uoh_idx] - bgc_begin_uoh_size[uoh_idx];
    float size_increased_ratio = (float)size_increased / (float)bgc_begin_uoh_size[uoh_idx];

    if (size_increased_ratio < bgc_uoh_inc_ratio_alloc_normal)
    {
        return uoh_alloc_normal;
    }
    else if (size_increased_ratio > bgc_uoh_inc_ratio_alloc_wait)
    {
        return uoh_alloc_wait;
    }
    else
    {
        return uoh_alloc_yield;
    }
}

void gc_heap::bgc_record_uoh_allocation(int gen_number, size_t size)
{
    assert((gen_number >= uoh_start_generation) && (gen_number < total_generation_count));

    int uoh_idx = gen_number - uoh_start_generation;

    if (gc_heap::background_running_p())
    {
        if (current_c_gc_state == c_gc_state_planning)
        {
            uoh_a_bgc_planning[uoh_idx] += size;
        }
        else
        {
            uoh_a_bgc_marking[uoh_idx] += size;
        }
    }
    else
    {
        uoh_a_no_bgc[uoh_idx] += size;
    }
}

void gc_heap::bgc_record_uoh_end_seg_allocation (int gen_number, size_t size)
{
    if ((gen_number >= uoh_start_generation) && gc_heap::background_running_p())
    {
        int uoh_idx = gen_number - uoh_start_generation;
        bgc_uoh_current_size[uoh_idx] += size;

#ifdef SIMPLE_DPRINTF
        dynamic_data* dd_uoh = dynamic_data_of (gen_number);
        size_t gen_size = generation_size (gen_number);
        dprintf (3, ("h%d g%d size is now %Id (inc-ed %Id), size is %Id (gen size is %Id), budget %.3fmb, new alloc %.3fmb",
            heap_number, gen_number, bgc_uoh_current_size[uoh_idx],
            (bgc_uoh_current_size[uoh_idx] - bgc_begin_uoh_size[uoh_idx]), size, gen_size,
            mb (dd_desired_allocation (dd_uoh)), (dd_new_allocation (dd_uoh) / 1000.0 / 1000.0)));
#endif //SIMPLE_DPRINTF
    }
}

#endif //BACKGROUND_GC

allocation_state gc_heap::allocate_uoh (int gen_number,
                                          size_t size,
                                          alloc_context* acontext,
                                          uint32_t flags,
                                          int align_const)
{
    enter_msl_status msl_status = msl_entered;

    // No variable values should be "carried over" from one state to the other.
    // That's why there are local variable for each state
    allocation_state uoh_alloc_state = a_state_start;

#ifdef SPINLOCK_HISTORY
    current_uoh_alloc_state = uoh_alloc_state;
#endif //SPINLOCK_HISTORY

#ifdef RECORD_LOH_STATE
    EEThreadId current_thread_id;
    current_thread_id.SetToCurrentThread ();
#endif //RECORD_LOH_STATE

#ifdef BACKGROUND_GC
    bgc_record_uoh_allocation(gen_number, size);

    if (gc_heap::background_running_p())
    {
        uoh_allocation_action action = get_bgc_allocate_action (gen_number);

        if (action == uoh_alloc_yield)
        {
            add_saved_spinlock_info (true, me_release, mt_alloc_large, msl_status);
            leave_spin_lock (&more_space_lock_uoh);
            bool cooperative_mode = enable_preemptive();
            GCToOSInterface::YieldThread (0);
            disable_preemptive (cooperative_mode);

            msl_status = enter_spin_lock_msl (&more_space_lock_uoh);
            if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;

            add_saved_spinlock_info (true, me_acquire, mt_alloc_large, msl_status);
            dprintf (SPINLOCK_LOG, ("[%d]spin Emsl uoh", heap_number));
        }
        else if (action == uoh_alloc_wait)
        {
            dynamic_data* dd_uoh = dynamic_data_of (loh_generation);
            dprintf (3, ("h%d WAIT loh begin %.3fmb, current size recorded is %.3fmb(begin+%.3fmb), budget %.3fmb, new alloc %.3fmb (alloc-ed %.3fmb)",
                heap_number, mb (bgc_begin_uoh_size[0]), mb (bgc_uoh_current_size[0]),
                mb (bgc_uoh_current_size[0] - bgc_begin_uoh_size[0]),
                mb (dd_desired_allocation (dd_uoh)), (dd_new_allocation (dd_uoh) / 1000.0 / 1000.0),
                mb (dd_desired_allocation (dd_uoh) - dd_new_allocation (dd_uoh))));

            msl_status = wait_for_background (awr_uoh_alloc_during_bgc, true);
            check_msl_status ("uoh a_state_acquire_seg", size);
        }
    }
#endif //BACKGROUND_GC

    gc_reason gr = reason_oos_loh;
    generation* gen = generation_of (gen_number);
    oom_reason oom_r = oom_no_failure;
    size_t current_full_compact_gc_count = 0;

    // If we can get a new seg it means allocation will succeed.
    while (1)
    {
        dprintf (3, ("[h%d]loh state is %s", heap_number, allocation_state_str[uoh_alloc_state]));

#ifdef SPINLOCK_HISTORY
        current_uoh_alloc_state = uoh_alloc_state;
#endif //SPINLOCK_HISTORY

#ifdef RECORD_LOH_STATE
        current_uoh_alloc_state = uoh_alloc_state;
        add_saved_loh_state (uoh_alloc_state, current_thread_id);
#endif //RECORD_LOH_STATE
        switch (uoh_alloc_state)
        {
            case a_state_can_allocate:
            case a_state_cant_allocate:
            {
                goto exit;
            }
            case a_state_start:
            {
                uoh_alloc_state = a_state_try_fit;
                break;
            }
            case a_state_try_fit:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;

                can_use_existing_p = uoh_try_fit (gen_number, size, acontext, flags,
                                                  align_const, &commit_failed_p, &oom_r);
                uoh_alloc_state = (can_use_existing_p ?
                                        a_state_can_allocate :
                                        (commit_failed_p ?
                                            a_state_trigger_full_compact_gc :
                                            a_state_acquire_seg));
                assert ((uoh_alloc_state == a_state_can_allocate) == (acontext->alloc_ptr != 0));
                break;
            }
            case a_state_try_fit_new_seg:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;

                can_use_existing_p = uoh_try_fit (gen_number, size, acontext, flags,
                                                  align_const, &commit_failed_p, &oom_r);
                // Even after we got a new seg it doesn't necessarily mean we can allocate,
                // another LOH allocating thread could have beat us to acquire the msl so
                // we need to try again.
                uoh_alloc_state = (can_use_existing_p ? a_state_can_allocate : a_state_try_fit);
                assert ((uoh_alloc_state == a_state_can_allocate) == (acontext->alloc_ptr != 0));
                break;
            }
            case a_state_try_fit_after_cg:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;

                can_use_existing_p = uoh_try_fit (gen_number, size, acontext, flags,
                                                  align_const, &commit_failed_p, &oom_r);
                // If we failed to commit, we bail right away 'cause we already did a
                // full compacting GC.
                uoh_alloc_state = (can_use_existing_p ?
                                        a_state_can_allocate :
                                        (commit_failed_p ?
                                            a_state_cant_allocate :
                                            a_state_acquire_seg_after_cg));
                assert ((uoh_alloc_state == a_state_can_allocate) == (acontext->alloc_ptr != 0));
                break;
            }
            case a_state_try_fit_after_bgc:
            {
                BOOL commit_failed_p = FALSE;
                BOOL can_use_existing_p = FALSE;

                can_use_existing_p = uoh_try_fit (gen_number, size, acontext, flags,
                                                  align_const, &commit_failed_p, &oom_r);
                uoh_alloc_state = (can_use_existing_p ?
                                        a_state_can_allocate :
                                        (commit_failed_p ?
                                            a_state_trigger_full_compact_gc :
                                            a_state_acquire_seg_after_bgc));
                assert ((uoh_alloc_state == a_state_can_allocate) == (acontext->alloc_ptr != 0));
                break;
            }
            case a_state_acquire_seg:
            {
                BOOL can_get_new_seg_p = FALSE;
                BOOL did_full_compacting_gc = FALSE;

                current_full_compact_gc_count = get_full_compact_gc_count();

                can_get_new_seg_p = uoh_get_new_seg (gen_number, size, &did_full_compacting_gc, &oom_r, &msl_status);
                check_msl_status ("uoh a_state_acquire_seg", size);

                uoh_alloc_state = (can_get_new_seg_p ?
                                        a_state_try_fit_new_seg :
                                        (did_full_compacting_gc ?
                                            a_state_check_retry_seg :
                                            a_state_check_and_wait_for_bgc));
                break;
            }
            case a_state_acquire_seg_after_cg:
            {
                BOOL can_get_new_seg_p = FALSE;
                BOOL did_full_compacting_gc = FALSE;

                current_full_compact_gc_count = get_full_compact_gc_count();

                can_get_new_seg_p = uoh_get_new_seg (gen_number, size, &did_full_compacting_gc, &oom_r, &msl_status);
                check_msl_status ("uoh a_state_acquire_seg_after_cg", size);

                // Since we release the msl before we try to allocate a seg, other
                // threads could have allocated a bunch of segments before us so
                // we might need to retry.
                uoh_alloc_state = (can_get_new_seg_p ?
                                        a_state_try_fit_after_cg :
                                        a_state_check_retry_seg);
                break;
            }
            case a_state_acquire_seg_after_bgc:
            {
                BOOL can_get_new_seg_p = FALSE;
                BOOL did_full_compacting_gc = FALSE;

                current_full_compact_gc_count = get_full_compact_gc_count();

                can_get_new_seg_p = uoh_get_new_seg (gen_number, size, &did_full_compacting_gc, &oom_r, &msl_status);
                check_msl_status ("uoh a_state_acquire_seg_after_bgc", size);

                uoh_alloc_state = (can_get_new_seg_p ?
                                        a_state_try_fit_new_seg :
                                        (did_full_compacting_gc ?
                                            a_state_check_retry_seg :
                                            a_state_trigger_full_compact_gc));
                assert ((uoh_alloc_state != a_state_cant_allocate) || (oom_r != oom_no_failure));
                break;
            }
            case a_state_check_and_wait_for_bgc:
            {
                BOOL bgc_in_progress_p = FALSE;
                BOOL did_full_compacting_gc = FALSE;

                bgc_in_progress_p = check_and_wait_for_bgc (awr_loh_oos_bgc, &did_full_compacting_gc, true, &msl_status);
                check_msl_status ("uoh a_state_check_and_wait_for_bgc", size);

                uoh_alloc_state = (!bgc_in_progress_p ?
                                        a_state_trigger_full_compact_gc :
                                        (did_full_compacting_gc ?
                                            a_state_try_fit_after_cg :
                                            a_state_try_fit_after_bgc));
                break;
            }
            case a_state_trigger_full_compact_gc:
            {
                if (fgn_maxgen_percent)
                {
                    dprintf (2, ("FGN: LOH doing last GC before we throw OOM"));
                    send_full_gc_notification (max_generation, FALSE);
                }

                BOOL got_full_compacting_gc = FALSE;

                got_full_compacting_gc = trigger_full_compact_gc (gr, &oom_r, true, &msl_status);
                check_msl_status ("uoh a_state_trigger_full_compact_gc", size);

                uoh_alloc_state = (got_full_compacting_gc ? a_state_try_fit_after_cg : a_state_cant_allocate);
                assert ((uoh_alloc_state != a_state_cant_allocate) || (oom_r != oom_no_failure));
                break;
            }
            case a_state_check_retry_seg:
            {
                BOOL should_retry_gc = retry_full_compact_gc (size);
                BOOL should_retry_get_seg = FALSE;
                if (!should_retry_gc)
                {
                    size_t last_full_compact_gc_count = current_full_compact_gc_count;
                    current_full_compact_gc_count = get_full_compact_gc_count();
                    if (current_full_compact_gc_count > last_full_compact_gc_count)
                    {
                        should_retry_get_seg = TRUE;
                    }
                }

                uoh_alloc_state = (should_retry_gc ?
                                        a_state_trigger_full_compact_gc :
                                        (should_retry_get_seg ?
                                            a_state_try_fit_after_cg :
                                            a_state_cant_allocate));
                assert ((uoh_alloc_state != a_state_cant_allocate) || (oom_r != oom_no_failure));
                break;
            }
            default:
            {
                assert (!"Invalid state!");
                break;
            }
        }
    }

exit:
    if (uoh_alloc_state == a_state_cant_allocate)
    {
        assert (oom_r != oom_no_failure);

        if ((oom_r != oom_cant_commit) && should_retry_other_heap (gen_number, size))
        {
            uoh_alloc_state = a_state_retry_allocate;
        }
        else
        {
            handle_oom (oom_r,
                        size,
                        0,
                        0);
        }
        add_saved_spinlock_info (true, me_release, mt_alloc_large_cant, msl_entered);
        leave_spin_lock (&more_space_lock_uoh);
    }

    assert ((uoh_alloc_state == a_state_can_allocate) ||
            (uoh_alloc_state == a_state_cant_allocate) ||
            (uoh_alloc_state == a_state_retry_allocate));
    return uoh_alloc_state;
}

// BGC's final mark phase will acquire the msl, so release it here and re-acquire.
enter_msl_status gc_heap::trigger_gc_for_alloc (int gen_number, gc_reason gr,
                                    GCSpinLock* msl, bool loh_p,
                                    msl_take_state take_state)
{
    enter_msl_status msl_status = msl_entered;

#ifdef BACKGROUND_GC
    if (loh_p)
    {
#ifdef MULTIPLE_HEAPS
#ifdef STRESS_DYNAMIC_HEAP_COUNT
        uoh_msl_before_gc_p = true;
#endif //STRESS_DYNAMIC_HEAP_COUNT
        dprintf (5555, ("h%d uoh alloc before GC", heap_number));
#endif //MULTIPLE_HEAPS
        add_saved_spinlock_info (loh_p, me_release, take_state, msl_status);
        leave_spin_lock (msl);
    }
#endif //BACKGROUND_GC

#ifdef MULTIPLE_HEAPS
    if (!loh_p)
    {
        add_saved_spinlock_info (loh_p, me_release, take_state, msl_status);
        leave_spin_lock (msl);
    }
#endif //MULTIPLE_HEAPS

    vm_heap->GarbageCollectGeneration (gen_number, gr);

#ifdef MULTIPLE_HEAPS
    if (!loh_p)
    {
        msl_status = enter_spin_lock_msl (msl);
        add_saved_spinlock_info (loh_p, me_acquire, take_state, msl_status);
    }
#endif //MULTIPLE_HEAPS

#ifdef BACKGROUND_GC
    if (loh_p)
    {
        msl_status = enter_spin_lock_msl (msl);
        add_saved_spinlock_info (loh_p, me_acquire, take_state, msl_status);
    }
#endif //BACKGROUND_GC

    return msl_status;
}

inline
bool gc_heap::update_alloc_info (int gen_number, size_t allocated_size, size_t* etw_allocation_amount)
{
    bool exceeded_p = false;
    int oh_index = gen_to_oh (gen_number);
    allocated_since_last_gc[oh_index] += allocated_size;

    size_t& etw_allocated = etw_allocation_running_amount[oh_index];
    etw_allocated += allocated_size;
    if (etw_allocated > etw_allocation_tick)
    {
        *etw_allocation_amount = etw_allocated;
        exceeded_p = true;
        etw_allocated = 0;
    }

    return exceeded_p;
}

allocation_state gc_heap::try_allocate_more_space (alloc_context* acontext, size_t size,
                                    uint32_t flags, int gen_number)
{
    enter_msl_status msl_status = msl_entered;

    if (gc_heap::gc_started)
    {
        wait_for_gc_done();
        //dprintf (5555, ("h%d TAMS g%d %Id returning a_state_retry_allocate!", heap_number, gen_number, size));

        return a_state_retry_allocate;
    }

    bool loh_p = (gen_number > 0);
    GCSpinLock* msl = loh_p ? &more_space_lock_uoh : &more_space_lock_soh;

#ifdef SYNCHRONIZATION_STATS
    int64_t msl_acquire_start = GCToOSInterface::QueryPerformanceCounter();
#endif //SYNCHRONIZATION_STATS

    msl_status = enter_spin_lock_msl (msl);
    check_msl_status ("TAMS", size);
    //if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;
    add_saved_spinlock_info (loh_p, me_acquire, mt_try_alloc, msl_status);
    dprintf (SPINLOCK_LOG, ("[%d]Emsl for alloc", heap_number));
#ifdef SYNCHRONIZATION_STATS
    int64_t msl_acquire = GCToOSInterface::QueryPerformanceCounter() - msl_acquire_start;
    total_msl_acquire += msl_acquire;
    num_msl_acquired++;
    if (msl_acquire > 200)
    {
        num_high_msl_acquire++;
    }
    else
    {
        num_low_msl_acquire++;
    }
#endif //SYNCHRONIZATION_STATS

    dprintf (3, ("requested to allocate %zd bytes on gen%d", size, gen_number));

    int align_const = get_alignment_constant (gen_number <= max_generation);

    if (fgn_maxgen_percent)
    {
        check_for_full_gc (gen_number, size);
    }

#ifdef BGC_SERVO_TUNING
    if ((gen_number != 0) && bgc_tuning::should_trigger_bgc_loh())
    {
        msl_status = trigger_gc_for_alloc (max_generation, reason_bgc_tuning_loh, msl, loh_p, mt_try_servo_budget);
        if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;
    }
    else
#endif //BGC_SERVO_TUNING
    {
        bool trigger_on_budget_loh_p =
#ifdef BGC_SERVO_TUNING
            !bgc_tuning::enable_fl_tuning;
#else
            true;
#endif //BGC_SERVO_TUNING

        bool check_budget_p = true;
        if (gen_number != 0)
        {
            check_budget_p = trigger_on_budget_loh_p;
        }

        if (check_budget_p && !(new_allocation_allowed (gen_number)))
        {
            if (fgn_maxgen_percent && (gen_number == 0))
            {
                // We only check gen0 every so often, so take this opportunity to check again.
                check_for_full_gc (gen_number, size);
            }

#ifdef BACKGROUND_GC
            bool recheck_p = wait_for_bgc_high_memory (awr_gen0_alloc, loh_p, &msl_status);
            if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;
#endif //BACKGROUND_GC

#ifdef SYNCHRONIZATION_STATS
            bad_suspension++;
#endif //SYNCHRONIZATION_STATS
            dprintf (2, ("h%d running out of budget on gen%d, gc", heap_number, gen_number));

#ifdef BACKGROUND_GC
            bool trigger_gc_p = true;
            if (recheck_p)
                trigger_gc_p = !(new_allocation_allowed (gen_number));

            if (trigger_gc_p)
#endif //BACKGROUND_GC
            {
                if (!settings.concurrent || (gen_number == 0))
                {
                    msl_status = trigger_gc_for_alloc (0, ((gen_number == 0) ? reason_alloc_soh : reason_alloc_loh),
                                                       msl, loh_p, mt_try_budget);
                    if (msl_status == msl_retry_different_heap) return a_state_retry_allocate;
                }
            }
        }
    }

    allocation_state can_allocate = ((gen_number == 0) ?
        allocate_soh (gen_number, size, acontext, flags, align_const) :
        allocate_uoh (gen_number, size, acontext, flags, align_const));

    return can_allocate;
}

#ifdef MULTIPLE_HEAPS
void gc_heap::balance_heaps (alloc_context* acontext)
{
    if (acontext->get_alloc_count() < 4)
    {
        if (acontext->get_alloc_count() == 0)
        {
            int home_hp_num = heap_select::select_heap (acontext);
            acontext->set_home_heap (GCHeap::GetHeap (home_hp_num));
            gc_heap* hp = acontext->get_home_heap ()->pGenGCHeap;
            acontext->set_alloc_heap (acontext->get_home_heap ());
            hp->alloc_context_count = hp->alloc_context_count + 1;

#ifdef HEAP_BALANCE_INSTRUMENTATION
            uint16_t ideal_proc_no = 0;
            GCToOSInterface::GetCurrentThreadIdealProc (&ideal_proc_no);

            uint32_t proc_no = GCToOSInterface::GetCurrentProcessorNumber ();

            add_to_hb_numa (proc_no, ideal_proc_no,
                home_hp_num, false, true, false);

            dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMPafter GC: 1st alloc on p%3d, h%d, ip: %d",
                proc_no, home_hp_num, ideal_proc_no));
#endif //HEAP_BALANCE_INSTRUMENTATION
        }
    }
    else
    {
        BOOL set_home_heap = FALSE;
        gc_heap* home_hp = NULL;
        int proc_hp_num = 0;

#ifdef HEAP_BALANCE_INSTRUMENTATION
        bool alloc_count_p = true;
        bool multiple_procs_p = false;
        bool set_ideal_p = false;
        uint32_t proc_no = GCToOSInterface::GetCurrentProcessorNumber ();
        uint32_t last_proc_no = proc_no;
#endif //HEAP_BALANCE_INSTRUMENTATION

        if (heap_select::can_find_heap_fast ())
        {
            assert (acontext->get_home_heap () != NULL);
            home_hp = acontext->get_home_heap ()->pGenGCHeap;
            proc_hp_num = heap_select::select_heap (acontext);

            if (home_hp != gc_heap::g_heaps[proc_hp_num])
            {
#ifdef HEAP_BALANCE_INSTRUMENTATION
                alloc_count_p = false;
#endif //HEAP_BALANCE_INSTRUMENTATION
                set_home_heap = TRUE;
            }
            else if ((acontext->get_alloc_count() & 15) == 0)
                set_home_heap = TRUE;
        }
        else
        {
            if ((acontext->get_alloc_count() & 3) == 0)
                set_home_heap = TRUE;
        }

        if (set_home_heap)
        {
            /*
                        // Since we are balancing up to MAX_SUPPORTED_HEAPS, no need for this.
                        if (n_heaps > MAX_SUPPORTED_HEAPS)
                        {
                            // on machines with many processors cache affinity is really king, so don't even try
                            // to balance on these.
                            acontext->home_heap = GCHeap::GetHeap( heap_select::select_heap(acontext));
                            acontext->alloc_heap = acontext->home_heap;
                        }
                        else
            */
            {
                gc_heap* org_hp = acontext->get_alloc_heap ()->pGenGCHeap;
                int org_hp_num = org_hp->heap_number;
                int final_alloc_hp_num = org_hp_num;

                dynamic_data* dd = org_hp->dynamic_data_of (0);
                ptrdiff_t org_size = dd_new_allocation (dd);
                ptrdiff_t total_size = (ptrdiff_t)dd_desired_allocation (dd);

#ifdef HEAP_BALANCE_INSTRUMENTATION
                dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMP[p%3d] ph h%3d, hh: %3d, ah: %3d (%dmb-%dmb), ac: %5d(%s)",
                    proc_no, proc_hp_num, home_hp->heap_number,
                    org_hp_num, (total_size / 1024 / 1024), (org_size / 1024 / 1024),
                    acontext->get_alloc_count(),
                    ((proc_hp_num == home_hp->heap_number) ? "AC" : "H")));
#endif //HEAP_BALANCE_INSTRUMENTATION

                int org_alloc_context_count;
                int max_alloc_context_count;
                gc_heap* max_hp;
                int max_hp_num = 0;
                ptrdiff_t max_size;
                size_t local_delta = max (((size_t)org_size >> 6), min_gen0_balance_delta);
                size_t delta = local_delta;

                if (((size_t)org_size + 2 * delta) >= (size_t)total_size)
                {
                    acontext->inc_alloc_count();
                    return;
                }

#ifdef HEAP_BALANCE_INSTRUMENTATION
                proc_no = GCToOSInterface::GetCurrentProcessorNumber ();
                if (proc_no != last_proc_no)
                {
                    dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMPSP: %d->%d", last_proc_no, proc_no));
                    multiple_procs_p = true;
                    last_proc_no = proc_no;
                }

                int new_home_hp_num = heap_select::proc_no_to_heap_no[proc_no];
#else
                int new_home_hp_num = heap_select::select_heap(acontext);
#endif //HEAP_BALANCE_INSTRUMENTATION
                gc_heap* new_home_hp = gc_heap::g_heaps[new_home_hp_num];
                acontext->set_home_heap (new_home_hp->vm_heap);

                int start, end, finish;
                heap_select::get_heap_range_for_heap (new_home_hp_num, &start, &end);
                finish = start + n_heaps;

                do
                {
                    max_hp = org_hp;
                    max_hp_num = org_hp_num;
                    max_size = org_size + delta;
                    org_alloc_context_count = org_hp->alloc_context_count;
                    max_alloc_context_count = org_alloc_context_count;
                    if (org_hp == new_home_hp)
                        max_size = max_size + delta;

                    if (max_alloc_context_count > 1)
                        max_size /= max_alloc_context_count;

                    // check if the new home heap has more space
                    if (org_hp != new_home_hp)
                    {
                        dd = new_home_hp->dynamic_data_of(0);
                        ptrdiff_t size = dd_new_allocation(dd);

                        // favor new home heap over org heap
                        size += delta * 2;

                        int new_home_hp_alloc_context_count = new_home_hp->alloc_context_count;
                        if (new_home_hp_alloc_context_count > 0)
                            size /= (new_home_hp_alloc_context_count + 1);

                        if (size > max_size)
                        {
#ifdef HEAP_BALANCE_INSTRUMENTATION
                            dprintf(HEAP_BALANCE_TEMP_LOG, ("TEMPorg h%d(%dmb), m h%d(%dmb)",
                                org_hp_num, (max_size / 1024 / 1024),
                                new_home_hp_num, (size / 1024 / 1024)));
#endif //HEAP_BALANCE_INSTRUMENTATION

                            max_hp = new_home_hp;
                            max_size = size;
                            max_hp_num = new_home_hp_num;
                            max_alloc_context_count = new_home_hp_alloc_context_count;
                        }
                    }

                    // consider heaps both inside our local NUMA node,
                    // and outside, but with different thresholds
                    enum
                    {
                        LOCAL_NUMA_NODE,
                        REMOTE_NUMA_NODE
                    };

                    for (int pass = LOCAL_NUMA_NODE; pass <= REMOTE_NUMA_NODE; pass++)
                    {
                        int count = end - start;
                        int max_tries = min(count, 4);

                        // we will consider max_tries consecutive (in a circular sense)
                        // other heaps from a semi random starting point

                        // alloc_count often increases by multiples of 16 (due to logic at top of routine),
                        // and we want to advance the starting point by 4 between successive calls,
                        // therefore the shift right by 2 bits
                        int heap_num = start + ((acontext->get_alloc_count() >> 2) + new_home_hp_num) % count;

#ifdef HEAP_BALANCE_INSTRUMENTATION
                        dprintf(HEAP_BALANCE_TEMP_LOG, ("TEMP starting at h%d (home_heap_num = %d, alloc_count = %d)", heap_num, new_home_hp_num, acontext->get_alloc_count()));
#endif //HEAP_BALANCE_INSTRUMENTATION

                        for (int tries = max_tries; --tries >= 0; heap_num++)
                        {
                            // wrap around if we hit the end of our range
                            if (heap_num >= end)
                                heap_num -= count;
                            // wrap around if we hit the end of the heap numbers
                            while (heap_num >= n_heaps)
                                heap_num -= n_heaps;

                            assert (heap_num < n_heaps);
                            gc_heap* hp = gc_heap::g_heaps[heap_num];
                            dd = hp->dynamic_data_of(0);
                            ptrdiff_t size = dd_new_allocation(dd);

#ifdef HEAP_BALANCE_INSTRUMENTATION
                            dprintf(HEAP_BALANCE_TEMP_LOG, ("TEMP looking at h%d(%dmb)",
                                heap_num, (size / 1024 / 1024)));
#endif //HEAP_BALANCE_INSTRUMENTATION
                            // if the size is not bigger than what we already have,
                            // give up immediately, as it can't be a winner...
                            // this is a micro-optimization to avoid fetching the
                            // alloc_context_count and possibly dividing by it
                            if (size <= max_size)
                                continue;

                            int hp_alloc_context_count = hp->alloc_context_count;

                            if (hp_alloc_context_count > 0)
                            {
                                size /= (hp_alloc_context_count + 1);
                            }

                            if (size > max_size)
                            {
#ifdef HEAP_BALANCE_INSTRUMENTATION
                                dprintf(HEAP_BALANCE_TEMP_LOG, ("TEMPorg h%d(%dmb), m h%d(%dmb)",
                                    org_hp_num, (max_size / 1024 / 1024),
                                    hp->heap_number, (size / 1024 / 1024)));
#endif //HEAP_BALANCE_INSTRUMENTATION

                                max_hp = hp;
                                max_size = size;
                                max_hp_num = max_hp->heap_number;
                                max_alloc_context_count = hp_alloc_context_count;
                            }
                        }

                        if ((max_hp == org_hp) && (end < finish))
                        {
                            start = end; end = finish;
                            delta = local_delta * 2; // Make it twice as hard to balance to remote nodes on NUMA.
                        }
                        else
                        {
                            // we already found a better heap, or there are no remote NUMA nodes
                            break;
                        }
                    }
                }
                while (org_alloc_context_count != org_hp->alloc_context_count ||
                       max_alloc_context_count != max_hp->alloc_context_count);

#ifdef HEAP_BALANCE_INSTRUMENTATION
                uint16_t ideal_proc_no_before_set_ideal = 0;
                GCToOSInterface::GetCurrentThreadIdealProc (&ideal_proc_no_before_set_ideal);
#endif //HEAP_BALANCE_INSTRUMENTATION

                if (max_hp != org_hp)
                {
                    final_alloc_hp_num = max_hp->heap_number;

                    // update the alloc_context_count for the original and new heaps.
                    // NOTE: at this time the alloc_context_count for these heaps could have changed due to racing threads,
                    //       but we will update the counts based on what we observed, without trying to re-check or 
                    //       synchronize, as this is just a heuristic to improve our balancing, and doesn't need to 
                    //       be perfectly accurate.
                    org_hp->alloc_context_count = org_hp->alloc_context_count - 1;
                    max_hp->alloc_context_count = max_hp->alloc_context_count + 1;

                    acontext->set_alloc_heap (GCHeap::GetHeap (final_alloc_hp_num));
                    if (!gc_thread_no_affinitize_p)
                    {
                        uint16_t src_proc_no = heap_select::find_proc_no_from_heap_no (org_hp->heap_number);
                        uint16_t dst_proc_no = heap_select::find_proc_no_from_heap_no (max_hp->heap_number);

                        dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMPSW! h%d(p%d)->h%d(p%d)",
                            org_hp_num, src_proc_no, final_alloc_hp_num, dst_proc_no));

#ifdef HEAP_BALANCE_INSTRUMENTATION
                        int current_proc_no_before_set_ideal = GCToOSInterface::GetCurrentProcessorNumber ();
                        if ((uint16_t)current_proc_no_before_set_ideal != last_proc_no)
                        {
                            dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMPSPa: %d->%d", last_proc_no, current_proc_no_before_set_ideal));
                            multiple_procs_p = true;
                        }
#endif //HEAP_BALANCE_INSTRUMENTATION

                        if (!GCToOSInterface::SetCurrentThreadIdealAffinity (src_proc_no, dst_proc_no))
                        {
                            dprintf (HEAP_BALANCE_TEMP_LOG, ("TEMPFailed to set the ideal processor for heap %d %d->%d",
                                org_hp->heap_number, (int)src_proc_no, (int)dst_proc_no));
                        }
#ifdef HEAP_BALANCE_INSTRUMENTATION
                        else
                        {
                            set_ideal_p = true;
                        }
#endif //HEAP_BALANCE_INSTRUMENTATION
                    }
                }

#ifdef HEAP_BALANCE_INSTRUMENTATION
                add_to_hb_numa (proc_no, ideal_proc_no_before_set_ideal,
                    final_alloc_hp_num, multiple_procs_p, alloc_count_p, set_ideal_p);
#endif //HEAP_BALANCE_INSTRUMENTATION
            }
        }
    }
    acontext->inc_alloc_count();
}

ptrdiff_t gc_heap::get_balance_heaps_uoh_effective_budget (int generation_num)
{
#ifndef USE_REGIONS
    if (heap_hard_limit)
    {
        const ptrdiff_t free_list_space = generation_free_list_space (generation_of (generation_num));
        heap_segment* seg = generation_start_segment (generation_of (generation_num));
        assert (heap_segment_next (seg) == nullptr);
        const ptrdiff_t allocated = heap_segment_allocated (seg) - seg->mem;
        // We could calculate the actual end_of_seg_space by taking reserved - allocated,
        // but all heaps have the same reserved memory and this value is only used for comparison.
        return free_list_space - allocated;
    }
    else
#endif // !USE_REGIONS
    {
        return dd_new_allocation (dynamic_data_of (generation_num));
    }
}

gc_heap* gc_heap::balance_heaps_uoh (alloc_context* acontext, size_t alloc_size, int generation_num)
{
    const int home_hp_num = heap_select::select_heap(acontext);
    dprintf (3, ("[h%d] LA: %zd", home_hp_num, alloc_size));
    gc_heap* home_hp = GCHeap::GetHeap(home_hp_num)->pGenGCHeap;
    dynamic_data* dd = home_hp->dynamic_data_of (generation_num);
    const ptrdiff_t home_hp_size = home_hp->get_balance_heaps_uoh_effective_budget (generation_num);

    size_t delta = dd_min_size (dd) / 2;
    int start, end;
    heap_select::get_heap_range_for_heap(home_hp_num, &start, &end);
    const int finish = start + n_heaps;

try_again:
    gc_heap* max_hp = home_hp;
    ptrdiff_t max_size = home_hp_size + delta;

    dprintf (3, ("home hp: %d, max size: %zd",
        home_hp_num,
        max_size));

    for (int i = start; i < end; i++)
    {
        gc_heap* hp = GCHeap::GetHeap(i%n_heaps)->pGenGCHeap;
        const ptrdiff_t size = hp->get_balance_heaps_uoh_effective_budget (generation_num);

        dprintf (3, ("hp: %d, size: %zd", hp->heap_number, size));
        if (size > max_size)
        {
            max_hp = hp;
            max_size = size;
            dprintf (3, ("max hp: %d, max size: %zd",
                max_hp->heap_number,
                max_size));
        }
    }

    if ((max_hp == home_hp) && (end < finish))
    {
        start = end; end = finish;
        delta = dd_min_size (dd) * 3 / 2; // Make it harder to balance to remote nodes on NUMA.
        goto try_again;
    }

    if (max_hp != home_hp)
    {
        dprintf (3, ("uoh: %d(%zd)->%d(%zd)",
            home_hp->heap_number, dd_new_allocation (home_hp->dynamic_data_of (generation_num)),
            max_hp->heap_number, dd_new_allocation (max_hp->dynamic_data_of (generation_num))));
    }

    return max_hp;
}

gc_heap* gc_heap::balance_heaps_uoh_hard_limit_retry (alloc_context* acontext, size_t alloc_size, int generation_num)
{
    assert (heap_hard_limit);
#ifdef USE_REGIONS
    return balance_heaps_uoh (acontext, alloc_size, generation_num);
#else //USE_REGIONS
    const int home_heap = heap_select::select_heap(acontext);
    dprintf (3, ("[h%d] balance_heaps_loh_hard_limit_retry alloc_size: %zd", home_heap, alloc_size));
    int start, end;
    heap_select::get_heap_range_for_heap (home_heap, &start, &end);
    const int finish = start + n_heaps;

    gc_heap* max_hp = nullptr;
    size_t max_end_of_seg_space = alloc_size; // Must be more than this much, or return NULL

try_again:
    {
        for (int i = start; i < end; i++)
        {
            gc_heap* hp = GCHeap::GetHeap (i%n_heaps)->pGenGCHeap;
            heap_segment* seg = generation_start_segment (hp->generation_of (generation_num));
            // With a hard limit, there is only one segment.
            assert (heap_segment_next (seg) == nullptr);
            const size_t end_of_seg_space = heap_segment_reserved (seg) - heap_segment_allocated (seg);
            if (end_of_seg_space >= max_end_of_seg_space)
            {
                dprintf (3, ("Switching heaps in hard_limit_retry! To: [h%d], New end_of_seg_space: %zd", hp->heap_number, end_of_seg_space));
                max_end_of_seg_space = end_of_seg_space;
                max_hp = hp;
            }
        }
    }

    // Only switch to a remote NUMA node if we didn't find space on this one.
    if ((max_hp == nullptr) && (end < finish))
    {
        start = end; end = finish;
        goto try_again;
    }

    return max_hp;
#endif //USE_REGIONS
}

#endif //MULTIPLE_HEAPS

BOOL gc_heap::allocate_more_space(alloc_context* acontext, size_t size,
                                   uint32_t flags, int alloc_generation_number)
{
    allocation_state status = a_state_start;
    int retry_count = 0;

    gc_heap* saved_alloc_heap = 0;

    do
    {
#ifdef MULTIPLE_HEAPS
        if (alloc_generation_number == 0)
        {
            balance_heaps (acontext);
            status = acontext->get_alloc_heap ()->pGenGCHeap->try_allocate_more_space (acontext, size, flags, alloc_generation_number);
        }
        else
        {
            uint64_t start_us = GetHighPrecisionTimeStamp ();

            gc_heap* alloc_heap;
            if (heap_hard_limit && (status == a_state_retry_allocate))
            {
                alloc_heap = balance_heaps_uoh_hard_limit_retry (acontext, size, alloc_generation_number);
                if (alloc_heap == nullptr || (retry_count++ == UOH_ALLOCATION_RETRY_MAX_COUNT))
                {
                    return false;
                }
            }
            else
            {
                alloc_heap = balance_heaps_uoh (acontext, size, alloc_generation_number);
                dprintf (3, ("uoh alloc %Id on h%d", size, alloc_heap->heap_number));
                saved_alloc_heap = alloc_heap;
            }

            bool alloced_on_retry = (status == a_state_retry_allocate);

            status = alloc_heap->try_allocate_more_space (acontext, size, flags, alloc_generation_number);
            dprintf (3, ("UOH h%d %Id returned from TAMS, s %d", alloc_heap->heap_number, size, status));

            uint64_t end_us = GetHighPrecisionTimeStamp ();

            if (status == a_state_retry_allocate)
            {
                // This records that we had to retry due to decommissioned heaps or GC in progress
                dprintf (5555, ("UOH h%d alloc %Id retry!", alloc_heap->heap_number, size));
            }
            else
            {
                if (alloced_on_retry)
                {
                    dprintf (5555, ("UOH h%d allocated %Id on retry (%I64dus)", alloc_heap->heap_number, size, (end_us - start_us)));
                }
            }
        }
#else
        status = try_allocate_more_space (acontext, size, flags, alloc_generation_number);
#endif //MULTIPLE_HEAPS
    }
    while (status == a_state_retry_allocate);

    return (status == a_state_can_allocate);
}

inline
CObjectHeader* gc_heap::allocate (size_t jsize, alloc_context* acontext, uint32_t flags)
{
    size_t size = Align (jsize);
    assert (size >= Align (min_obj_size));
    {
    retry:
        uint8_t*  result = acontext->alloc_ptr;
        acontext->alloc_ptr+=size;
        if (acontext->alloc_ptr <= acontext->alloc_limit)
        {
            CObjectHeader* obj = (CObjectHeader*)result;
            assert (obj != 0);
            return obj;
        }
        else
        {
            acontext->alloc_ptr -= size;

#ifdef _MSC_VER
#pragma inline_depth(0)
#endif //_MSC_VER

            if (! allocate_more_space (acontext, size, flags, 0))
                return 0;

#ifdef _MSC_VER
#pragma inline_depth(20)
#endif //_MSC_VER

            goto retry;
        }
    }
}

void  gc_heap::leave_allocation_segment (generation* gen)
{
    adjust_limit (0, 0, gen);
}

void gc_heap::init_free_and_plug()
{
#ifdef FREE_USAGE_STATS
    int i = (settings.concurrent ? max_generation : 0);

    for (; i <= settings.condemned_generation; i++)
    {
        generation* gen = generation_of (i);
#ifdef DOUBLY_LINKED_FL
        print_free_and_plug ("BGC");
#else
        memset (gen->gen_free_spaces, 0, sizeof (gen->gen_free_spaces));
#endif //DOUBLY_LINKED_FL
        memset (gen->gen_plugs, 0, sizeof (gen->gen_plugs));
        memset (gen->gen_current_pinned_free_spaces, 0, sizeof (gen->gen_current_pinned_free_spaces));
    }

    if (settings.condemned_generation != max_generation)
    {
        for (int i = (settings.condemned_generation + 1); i <= max_generation; i++)
        {
            generation* gen = generation_of (i);
            memset (gen->gen_plugs, 0, sizeof (gen->gen_plugs));
        }
    }
#endif //FREE_USAGE_STATS
}

void gc_heap::print_free_and_plug (const char* msg)
{
#ifdef FREE_USAGE_STATS
    int older_gen = ((settings.condemned_generation == max_generation) ? max_generation : (settings.condemned_generation + 1));
    for (int i = 0; i <= older_gen; i++)
    {
        generation* gen = generation_of (i);
        for (int j = 0; j < NUM_GEN_POWER2; j++)
        {
            if ((gen->gen_free_spaces[j] != 0) || (gen->gen_plugs[j] != 0))
            {
                dprintf (2, ("[%s][h%d][%s#%d]gen%d: 2^%d: F: %zd, P: %zd",
                    msg,
                    heap_number,
                    (settings.concurrent ? "BGC" : "GC"),
                    settings.gc_index,
                    i,
                    (j + 9), gen->gen_free_spaces[j], gen->gen_plugs[j]));
            }
        }
    }
#else
    UNREFERENCED_PARAMETER(msg);
#endif //FREE_USAGE_STATS
}

// replace with allocator::first_suitable_bucket
int gc_heap::find_bucket (size_t size)
{
    size_t sz = BASE_GEN_SIZE;
    int i = 0;

    for (; i < (NUM_GEN_POWER2 - 1); i++)
    {
        if (size < sz)
        {
            break;
        }
        sz = sz * 2;
    }

    return i;
}

void gc_heap::add_gen_plug (int gen_number, size_t plug_size)
{
#ifdef FREE_USAGE_STATS
    dprintf (3, ("adding plug size %zd to gen%d", plug_size, gen_number));
    generation* gen = generation_of (gen_number);
    size_t sz = BASE_GEN_SIZE;
    int i = find_bucket (plug_size);

    (gen->gen_plugs[i])++;
#else
    UNREFERENCED_PARAMETER(gen_number);
    UNREFERENCED_PARAMETER(plug_size);
#endif //FREE_USAGE_STATS
}

void gc_heap::add_item_to_current_pinned_free (int gen_number, size_t free_size)
{
#ifdef FREE_USAGE_STATS
    generation* gen = generation_of (gen_number);
    size_t sz = BASE_GEN_SIZE;
    int i = find_bucket (free_size);

    (gen->gen_current_pinned_free_spaces[i])++;
    generation_pinned_free_obj_space (gen) += free_size;
    dprintf (3, ("left pin free %zd(2^%d) to gen%d, total %zd bytes (%zd)",
        free_size, (i + 10), gen_number,
        generation_pinned_free_obj_space (gen),
        gen->gen_current_pinned_free_spaces[i]));
#else
    UNREFERENCED_PARAMETER(gen_number);
    UNREFERENCED_PARAMETER(free_size);
#endif //FREE_USAGE_STATS
}

// This is only for items large enough to be on the FL
// Ideally we should keep track of smaller ones too but for now
// it's easier to make the accounting right
void gc_heap::add_gen_free (int gen_number, size_t free_size)
{
#ifdef FREE_USAGE_STATS
    dprintf (3, ("adding free size %zd to gen%d", free_size, gen_number));
    if (free_size < min_free_list)
        return;

    generation* gen = generation_of (gen_number);
    size_t sz = BASE_GEN_SIZE;
    int i = find_bucket (free_size);

    (gen->gen_free_spaces[i])++;
    if (gen_number == max_generation)
    {
        dprintf (3, ("Mb b%d: f+ %zd (%zd)",
            i, free_size, gen->gen_free_spaces[i]));
    }
#else
    UNREFERENCED_PARAMETER(gen_number);
    UNREFERENCED_PARAMETER(free_size);
#endif //FREE_USAGE_STATS
}

void gc_heap::remove_gen_free (int gen_number, size_t free_size)
{
#ifdef FREE_USAGE_STATS
    dprintf (3, ("removing free %zd from gen%d", free_size, gen_number));
    if (free_size < min_free_list)
        return;

    generation* gen = generation_of (gen_number);
    size_t sz = BASE_GEN_SIZE;
    int i = find_bucket (free_size);

    (gen->gen_free_spaces[i])--;
    if (gen_number == max_generation)
    {
        dprintf (3, ("Mb b%d: f- %zd (%zd)",
            i, free_size, gen->gen_free_spaces[i]));
    }
#else
    UNREFERENCED_PARAMETER(gen_number);
    UNREFERENCED_PARAMETER(free_size);
#endif //FREE_USAGE_STATS
}

#ifdef DOUBLY_LINKED_FL
// This is only called on free spaces.
BOOL gc_heap::should_set_bgc_mark_bit (uint8_t* o)
{
    if (!current_sweep_seg)
    {
        assert (current_bgc_state == bgc_not_in_process);
        return FALSE;
    }

    // This is cheaper so I am doing this comparison first before having to get the seg for o.
    if (in_range_for_segment (o, current_sweep_seg))
    {
        // The current sweep seg could have free spaces beyond its background_allocated so we need
        // to check for that.
        if ((o >= current_sweep_pos) && (o < heap_segment_background_allocated (current_sweep_seg)))
        {
#ifndef USE_REGIONS
            if (current_sweep_seg == saved_sweep_ephemeral_seg)
            {
                return (o < saved_sweep_ephemeral_start);
            }
            else
#endif //!USE_REGIONS
            {
                return TRUE;
            }
        }
        else
            return FALSE;
    }
    else
    {
        // We can have segments outside the BGC range that were allocated during mark - and we
        // wouldn't have committed the mark array for them and their background_allocated would be
        // non-zero. Don't set mark bits for those.
        // The ones allocated during BGC sweep would have their background_allocated as 0.
        if ((o >= background_saved_lowest_address) && (o < background_saved_highest_address))
        {
            heap_segment* seg = seg_mapping_table_segment_of (o);
            // if bgc_allocated is 0 it means it was allocated during bgc sweep,
            // and everything on it should be considered live.
            uint8_t* background_allocated = heap_segment_background_allocated (seg);
            if (background_allocated == 0)
                return FALSE;
            // During BGC sweep gen1 GCs could add some free spaces in gen2.
            // If we use those, we should not set the mark bits on them.
            // They could either be a newly allocated seg which is covered by the
            // above case; or they are on a seg that's seen but beyond what BGC mark
            // saw.
            else if (o >= background_allocated)
                return FALSE;
            else
                return (!heap_segment_swept_p (seg));
        }
        else
            return FALSE;
    }
}

#endif //DOUBLY_LINKED_FL

uint8_t* gc_heap::allocate_in_older_generation (generation* gen, size_t size,
                                                int from_gen_number,
                                                uint8_t* old_loc REQD_ALIGN_AND_OFFSET_DCL)
{
    size = Align (size);
    assert (size >= Align (min_obj_size));
    assert (from_gen_number < max_generation);
    assert (from_gen_number >= 0);
    assert (generation_of (from_gen_number + 1) == gen);

#ifdef DOUBLY_LINKED_FL
    BOOL consider_bgc_mark_p        = FALSE;
    BOOL check_current_sweep_p      = FALSE;
    BOOL check_saved_sweep_p        = FALSE;
    BOOL try_added_list_p       = (gen->gen_num == max_generation);
    BOOL record_free_list_allocated_p = ((gen->gen_num == max_generation) &&
                                         (current_c_gc_state == c_gc_state_planning));
#endif //DOUBLY_LINKED_FL

    allocator* gen_allocator = generation_allocator (gen);
    BOOL discard_p = gen_allocator->discard_if_no_fit_p ();
#ifdef SHORT_PLUGS
    int pad_in_front = ((old_loc != 0) && ((from_gen_number+1) != max_generation)) ? USE_PADDING_FRONT : 0;
#else //SHORT_PLUGS
    int pad_in_front = 0;
#endif //SHORT_PLUGS

    size_t real_size = size + Align (min_obj_size);
    if (pad_in_front)
        real_size += Align (min_obj_size);

#ifdef RESPECT_LARGE_ALIGNMENT
    real_size += switch_alignment_size (pad_in_front);
#endif //RESPECT_LARGE_ALIGNMENT

    if (! (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, generation_allocation_pointer (gen),
                       generation_allocation_limit (gen), old_loc, USE_PADDING_TAIL | pad_in_front)))
    {
        for (unsigned int a_l_idx = gen_allocator->first_suitable_bucket(real_size * 2);
             a_l_idx < gen_allocator->number_of_buckets(); a_l_idx++)
        {
            uint8_t* free_list = 0;
            uint8_t* prev_free_item = 0;

            BOOL use_undo_p = !discard_p;

#ifdef DOUBLY_LINKED_FL
            if (a_l_idx == 0)
            {
                use_undo_p = FALSE;
            }

            if (try_added_list_p)
            {
                free_list = gen_allocator->added_alloc_list_head_of (a_l_idx);
                while (free_list != 0)
                {
                    dprintf (3, ("considering free list in added list%zx", (size_t)free_list));

                    size_t free_list_size = unused_array_size (free_list);

                    if (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, free_list, (free_list + free_list_size),
                                    old_loc, USE_PADDING_TAIL | pad_in_front))
                    {
                        dprintf (4, ("F:%zx-%zd",
                                    (size_t)free_list, free_list_size));

                        gen_allocator->unlink_item_no_undo_added (a_l_idx, free_list, prev_free_item);
                        generation_free_list_space (gen) -= free_list_size;
                        assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);

                        remove_gen_free (gen->gen_num, free_list_size);

                        if (record_free_list_allocated_p)
                        {
                            generation_set_bgc_mark_bit_p (gen) = should_set_bgc_mark_bit (free_list);
                            dprintf (3333, ("SFA: %p->%p(%d)", free_list, (free_list + free_list_size),
                                (generation_set_bgc_mark_bit_p (gen) ? 1 : 0)));
                        }
                        adjust_limit (free_list, free_list_size, gen);
                        generation_allocate_end_seg_p (gen) = FALSE;

                        goto finished;
                    }
                    // We do first fit on bucket 0 because we are not guaranteed to find a fit there.
                    else if (a_l_idx == 0)
                    {
                        dprintf (3, ("couldn't use this free area, discarding"));
                        generation_free_obj_space (gen) += free_list_size;

                        gen_allocator->unlink_item_no_undo_added (a_l_idx, free_list, prev_free_item);
                        generation_free_list_space (gen) -= free_list_size;
                        assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);

                        remove_gen_free (gen->gen_num, free_list_size);
                    }
                    else
                    {
                        prev_free_item = free_list;
                    }
                    free_list = free_list_slot (free_list);
                }
            }
#endif //DOUBLY_LINKED_FL

            free_list = gen_allocator->alloc_list_head_of (a_l_idx);
            prev_free_item = 0;

            while (free_list != 0)
            {
                dprintf (3, ("considering free list %zx", (size_t)free_list));

                size_t free_list_size = unused_array_size (free_list);

                if (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, free_list, (free_list + free_list_size),
                                old_loc, USE_PADDING_TAIL | pad_in_front))
                {
                    dprintf (4, ("F:%zx-%zd",
                                    (size_t)free_list, free_list_size));

                    gen_allocator->unlink_item (a_l_idx, free_list, prev_free_item, use_undo_p);
                    generation_free_list_space (gen) -= free_list_size;
                    assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);
                    remove_gen_free (gen->gen_num, free_list_size);

#ifdef DOUBLY_LINKED_FL
                    if (!discard_p && !use_undo_p)
                    {
                        gen2_removed_no_undo += free_list_size;
                        dprintf (3, ("h%d: remove with no undo %zd = %zd",
                            heap_number, free_list_size, gen2_removed_no_undo));
                    }

                    if (record_free_list_allocated_p)
                    {
                        generation_set_bgc_mark_bit_p (gen) = should_set_bgc_mark_bit (free_list);
                        dprintf (3333, ("SF: %p(%d)", free_list, (generation_set_bgc_mark_bit_p (gen) ? 1 : 0)));
                    }
#endif //DOUBLY_LINKED_FL

                    adjust_limit (free_list, free_list_size, gen);
                    generation_allocate_end_seg_p (gen) = FALSE;
                    goto finished;
                }
                // We do first fit on bucket 0 because we are not guaranteed to find a fit there.
                else if (discard_p || (a_l_idx == 0))
                {
                    dprintf (3, ("couldn't use this free area, discarding"));
                    generation_free_obj_space (gen) += free_list_size;

                    gen_allocator->unlink_item (a_l_idx, free_list, prev_free_item, FALSE);
                    generation_free_list_space (gen) -= free_list_size;
                    assert ((ptrdiff_t)generation_free_list_space (gen) >= 0);
                    remove_gen_free (gen->gen_num, free_list_size);

#ifdef DOUBLY_LINKED_FL
                    if (!discard_p)
                    {
                        gen2_removed_no_undo += free_list_size;
                        dprintf (3, ("h%d: b0 remove with no undo %zd = %zd",
                            heap_number, free_list_size, gen2_removed_no_undo));
                    }
#endif //DOUBLY_LINKED_FL
                }
                else
                {
                    prev_free_item = free_list;
                }
                free_list = free_list_slot (free_list);
            }
        }
#ifdef USE_REGIONS
        // We don't want to always go back to the first region since there might be many.
        heap_segment* seg = generation_allocation_segment (gen);
        dprintf (3, ("end of seg, starting from alloc seg %p", heap_segment_mem (seg)));
        assert (seg != ephemeral_heap_segment);
        while (true)
#else
        //go back to the beginning of the segment list
        heap_segment* seg = heap_segment_rw (generation_start_segment (gen));
        if (seg != generation_allocation_segment (gen))
        {
            leave_allocation_segment (gen);
            generation_allocation_segment (gen) = seg;
        }
        while (seg != ephemeral_heap_segment)
#endif //USE_REGIONS
        {
            if (size_fit_p(size REQD_ALIGN_AND_OFFSET_ARG, heap_segment_plan_allocated (seg),
                           heap_segment_committed (seg), old_loc, USE_PADDING_TAIL | pad_in_front))
            {
                adjust_limit (heap_segment_plan_allocated (seg),
                              (heap_segment_committed (seg) - heap_segment_plan_allocated (seg)),
                              gen);
                generation_allocate_end_seg_p (gen) = TRUE;
                heap_segment_plan_allocated (seg) =
                    heap_segment_committed (seg);
                dprintf (3, ("seg %p is used for end of seg alloc", heap_segment_mem (seg)));
                goto finished;
            }
            else
            {
                if (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, heap_segment_plan_allocated (seg),
                                heap_segment_reserved (seg), old_loc, USE_PADDING_TAIL | pad_in_front) &&
                    grow_heap_segment (seg, heap_segment_plan_allocated (seg), old_loc, size, pad_in_front REQD_ALIGN_AND_OFFSET_ARG))
                {
                    adjust_limit (heap_segment_plan_allocated (seg),
                                  (heap_segment_committed (seg) - heap_segment_plan_allocated (seg)),
                                  gen);
                    generation_allocate_end_seg_p (gen) = TRUE;
                    heap_segment_plan_allocated (seg) =
                        heap_segment_committed (seg);
                    dprintf (3, ("seg %p is used for end of seg alloc after grow, %p",
                        heap_segment_mem (seg), heap_segment_committed (seg)));

                    goto finished;
                }
                else
                {
                    leave_allocation_segment (gen);
                    heap_segment*   next_seg = heap_segment_next_rw (seg);

#ifdef USE_REGIONS
                    assert (next_seg != ephemeral_heap_segment);
#endif //USE_REGIONS

                    if (next_seg)
                    {
                        generation_allocation_segment (gen) = next_seg;
                        generation_allocation_pointer (gen) = heap_segment_mem (next_seg);
                        generation_allocation_limit (gen) = generation_allocation_pointer (gen);
                        dprintf (3, ("alloc region advanced to %p", heap_segment_mem (next_seg)));
                    }
                    else
                    {
                        size = 0;
                        goto finished;
                    }
                }
            }
            seg = generation_allocation_segment (gen);
        }
        //No need to fix the last region. Will be done later
        size = 0;
        goto finished;
    }

finished:
    if (0 == size)
    {
        return 0;
    }
    else
    {
        uint8_t*  result = generation_allocation_pointer (gen);
        size_t pad = 0;

#ifdef SHORT_PLUGS
        if ((pad_in_front & USE_PADDING_FRONT) &&
            (((generation_allocation_pointer (gen) - generation_allocation_context_start_region (gen))==0) ||
             ((generation_allocation_pointer (gen) - generation_allocation_context_start_region (gen))>=DESIRED_PLUG_LENGTH)))
        {
            pad = Align (min_obj_size);
            set_plug_padded (old_loc);
        }
#endif //SHORT_PLUGS

#ifdef FEATURE_STRUCTALIGN
        _ASSERTE(!old_loc || alignmentOffset != 0);
        _ASSERTE(old_loc || requiredAlignment == DATA_ALIGNMENT);
        if (old_loc != 0)
        {
            size_t pad1 = ComputeStructAlignPad(result+pad, requiredAlignment, alignmentOffset);
            set_node_aligninfo (old_loc, requiredAlignment, pad1);
            pad += pad1;
        }
#else // FEATURE_STRUCTALIGN
        if (!((old_loc == 0) || same_large_alignment_p (old_loc, result+pad)))
        {
            pad += switch_alignment_size (pad != 0);
            set_node_realigned (old_loc);
            dprintf (3, ("Allocation realignment old_loc: %zx, new_loc:%zx",
                         (size_t)old_loc, (size_t)(result+pad)));
            assert (same_large_alignment_p (result + pad, old_loc));
        }
#endif // FEATURE_STRUCTALIGN
        dprintf (3, ("Allocate %zd bytes", size));

        if ((old_loc == 0) || (pad != 0))
        {
            //allocating a non plug or a gap, so reset the start region
            generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
        }

        generation_allocation_pointer (gen) += size + pad;
        assert (generation_allocation_pointer (gen) <= generation_allocation_limit (gen));

        generation_free_obj_space (gen) += pad;

        if (generation_allocate_end_seg_p (gen))
        {
            generation_end_seg_allocated (gen) += size;
        }
        else
        {
#ifdef DOUBLY_LINKED_FL
            if (generation_set_bgc_mark_bit_p (gen))
            {
                dprintf (2, ("IOM: %p(->%p(%zd) (%zx-%zx)", old_loc, result, pad,
                        (size_t)(&mark_array [mark_word_of (result)]),
                        (size_t)(mark_array [mark_word_of (result)])));

                set_plug_bgc_mark_bit (old_loc);
            }

            generation_last_free_list_allocated (gen) = old_loc;
#endif //DOUBLY_LINKED_FL

            generation_free_list_allocated (gen) += size;
        }
        generation_allocation_size (gen) += size;

        dprintf (3, ("aio: ptr: %p, limit: %p, sr: %p",
            generation_allocation_pointer (gen), generation_allocation_limit (gen),
            generation_allocation_context_start_region (gen)));

        return (result + pad);
    }
}

#ifndef USE_REGIONS
void gc_heap::repair_allocation_in_expanded_heap (generation* consing_gen)
{
    //make sure that every generation has a planned allocation start
    int  gen_number = max_generation - 1;
    while (gen_number>= 0)
    {
        generation* gen = generation_of (gen_number);
        if (0 == generation_plan_allocation_start (gen))
        {
            realloc_plan_generation_start (gen, consing_gen);

            assert (generation_plan_allocation_start (gen));
        }
        gen_number--;
    }

    // now we know the planned allocation size
    size_t  size = (generation_allocation_limit (consing_gen) - generation_allocation_pointer (consing_gen));
    heap_segment* seg = generation_allocation_segment (consing_gen);
    if (generation_allocation_limit (consing_gen) == heap_segment_plan_allocated (seg))
    {
        if (size != 0)
        {
            heap_segment_plan_allocated (seg) = generation_allocation_pointer (consing_gen);
        }
    }
    else
    {
        assert (settings.condemned_generation == max_generation);
        uint8_t* first_address = generation_allocation_limit (consing_gen);
        //look through the pinned plugs for relevant ones.
        //Look for the right pinned plug to start from.
        size_t mi = 0;
        mark* m = 0;
        while (mi != mark_stack_tos)
        {
            m = pinned_plug_of (mi);
            if ((pinned_plug (m) == first_address))
                break;
            else
                mi++;
        }
        assert (mi != mark_stack_tos);
        pinned_len (m) = size;
    }
}

//tododefrag optimize for new segment (plan_allocated == mem)
uint8_t* gc_heap::allocate_in_expanded_heap (generation* gen,
                                          size_t size,
                                          BOOL& adjacentp,
                                          uint8_t* old_loc,
#ifdef SHORT_PLUGS
                                          BOOL set_padding_on_saved_p,
                                          mark* pinned_plug_entry,
#endif //SHORT_PLUGS
                                          BOOL consider_bestfit,
                                          int active_new_gen_number
                                          REQD_ALIGN_AND_OFFSET_DCL)
{
    dprintf (3, ("aie: P: %p, size: %zx", old_loc, size));

    size = Align (size);
    assert (size >= Align (min_obj_size));
#ifdef SHORT_PLUGS
    int pad_in_front = ((old_loc != 0) && (active_new_gen_number != max_generation)) ? USE_PADDING_FRONT : 0;
#else //SHORT_PLUGS
    int pad_in_front = 0;
#endif //SHORT_PLUGS

    if (consider_bestfit && use_bestfit)
    {
        assert (bestfit_seg);
        dprintf (SEG_REUSE_LOG_1, ("reallocating 0x%p in expanded heap, size: %zd",
                    old_loc, size));
        return bestfit_seg->fit (old_loc,
                                 size REQD_ALIGN_AND_OFFSET_ARG);
    }

    heap_segment* seg = generation_allocation_segment (gen);

    if (! (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, generation_allocation_pointer (gen),
                       generation_allocation_limit (gen), old_loc,
                       ((generation_allocation_limit (gen) !=
                          heap_segment_plan_allocated (seg))? USE_PADDING_TAIL : 0) | pad_in_front)))
    {
        dprintf (3, ("aie: can't fit: ptr: %p, limit: %p", generation_allocation_pointer (gen),
            generation_allocation_limit (gen)));

        adjacentp = FALSE;
        uint8_t* first_address = (generation_allocation_limit (gen) ?
                               generation_allocation_limit (gen) :
                               heap_segment_mem (seg));
        assert (in_range_for_segment (first_address, seg));

        uint8_t* end_address   = heap_segment_reserved (seg);

        dprintf (3, ("aie: first_addr: %p, gen alloc limit: %p, end_address: %p",
            first_address, generation_allocation_limit (gen), end_address));

        size_t mi = 0;
        mark* m = 0;

        if (heap_segment_allocated (seg) != heap_segment_mem (seg))
        {
            assert (settings.condemned_generation == max_generation);
            //look through the pinned plugs for relevant ones.
            //Look for the right pinned plug to start from.
            while (mi != mark_stack_tos)
            {
                m = pinned_plug_of (mi);
                if ((pinned_plug (m) >= first_address) && (pinned_plug (m) < end_address))
                {
                    dprintf (3, ("aie: found pin: %p", pinned_plug (m)));
                    break;
                }
                else
                    mi++;
            }
            if (mi != mark_stack_tos)
            {
                //fix old free list.
                size_t  hsize = (generation_allocation_limit (gen) - generation_allocation_pointer (gen));
                {
                    dprintf(3,("gc filling up hole"));
                    ptrdiff_t mi1 = (ptrdiff_t)mi;
                    while ((mi1 >= 0) &&
                           (pinned_plug (pinned_plug_of(mi1)) != generation_allocation_limit (gen)))
                    {
                        dprintf (3, ("aie: checking pin %p", pinned_plug (pinned_plug_of(mi1))));
                        mi1--;
                    }
                    if (mi1 >= 0)
                    {
                        size_t saved_pinned_len = pinned_len (pinned_plug_of(mi1));
                        pinned_len (pinned_plug_of(mi1)) = hsize;
                        dprintf (3, ("changing %p len %zx->%zx",
                            pinned_plug (pinned_plug_of(mi1)),
                            saved_pinned_len, pinned_len (pinned_plug_of(mi1))));
                    }
                }
            }
        }
        else
        {
            assert (generation_allocation_limit (gen) ==
                    generation_allocation_pointer (gen));
            mi = mark_stack_tos;
        }

        while ((mi != mark_stack_tos) && in_range_for_segment (pinned_plug (m), seg))
        {
            size_t len = pinned_len (m);
            uint8_t*  free_list = (pinned_plug (m) - len);
            dprintf (3, ("aie: testing free item: %p->%p(%zx)",
                free_list, (free_list + len), len));
            if (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, free_list, (free_list + len), old_loc, USE_PADDING_TAIL | pad_in_front))
            {
                dprintf (3, ("aie: Found adequate unused area: %zx, size: %zd",
                            (size_t)free_list, len));
                {
                    generation_allocation_pointer (gen) = free_list;
                    generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
                    generation_allocation_limit (gen) = (free_list + len);
                }
                goto allocate_in_free;
            }
            mi++;
            m = pinned_plug_of (mi);
        }

        //switch to the end of the segment.
        generation_allocation_pointer (gen) = heap_segment_plan_allocated (seg);
        generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
        heap_segment_plan_allocated (seg) = heap_segment_committed (seg);
        generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
        dprintf (3, ("aie: switching to end of seg: %p->%p(%zx)",
            generation_allocation_pointer (gen), generation_allocation_limit (gen),
            (generation_allocation_limit (gen) - generation_allocation_pointer (gen))));

        if (!size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, generation_allocation_pointer (gen),
                         generation_allocation_limit (gen), old_loc, USE_PADDING_TAIL | pad_in_front))
        {
            dprintf (3, ("aie: ptr: %p, limit: %p, can't alloc", generation_allocation_pointer (gen),
                generation_allocation_limit (gen)));
            assert (!"Can't allocate if no free space");
            return 0;
        }
    }
    else
    {
        adjacentp = TRUE;
    }

allocate_in_free:
    {
        uint8_t*  result = generation_allocation_pointer (gen);
        size_t pad = 0;

#ifdef SHORT_PLUGS
        if ((pad_in_front & USE_PADDING_FRONT) &&
            (((generation_allocation_pointer (gen) - generation_allocation_context_start_region (gen))==0) ||
             ((generation_allocation_pointer (gen) - generation_allocation_context_start_region (gen))>=DESIRED_PLUG_LENGTH)))

        {
            pad = Align (min_obj_size);
            set_padding_in_expand (old_loc, set_padding_on_saved_p, pinned_plug_entry);
        }
#endif //SHORT_PLUGS

#ifdef FEATURE_STRUCTALIGN
        _ASSERTE(!old_loc || alignmentOffset != 0);
        _ASSERTE(old_loc || requiredAlignment == DATA_ALIGNMENT);
        if (old_loc != 0)
        {
            size_t pad1 = ComputeStructAlignPad(result+pad, requiredAlignment, alignmentOffset);
            set_node_aligninfo (old_loc, requiredAlignment, pad1);
            pad += pad1;
            adjacentp = FALSE;
        }
#else // FEATURE_STRUCTALIGN
        if (!((old_loc == 0) || same_large_alignment_p (old_loc, result+pad)))
        {
            pad += switch_alignment_size (pad != 0);
            set_node_realigned (old_loc);
            dprintf (3, ("Allocation realignment old_loc: %zx, new_loc:%zx",
                         (size_t)old_loc, (size_t)(result+pad)));
            assert (same_large_alignment_p (result + pad, old_loc));
            adjacentp = FALSE;
        }
#endif // FEATURE_STRUCTALIGN

        if ((old_loc == 0) || (pad != 0))
        {
            //allocating a non plug or a gap, so reset the start region
            generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
        }

        generation_allocation_pointer (gen) += size + pad;
        assert (generation_allocation_pointer (gen) <= generation_allocation_limit (gen));
        dprintf (3, ("Allocated in expanded heap %zx:%zd", (size_t)(result+pad), size));

        dprintf (3, ("aie: ptr: %p, limit: %p, sr: %p",
            generation_allocation_pointer (gen), generation_allocation_limit (gen),
            generation_allocation_context_start_region (gen)));

        return result + pad;
    }
}

generation*  gc_heap::ensure_ephemeral_heap_segment (generation* consing_gen)
{
    heap_segment* seg = generation_allocation_segment (consing_gen);
    if (seg != ephemeral_heap_segment)
    {
        assert (generation_allocation_pointer (consing_gen)>= heap_segment_mem (seg));
        assert (generation_allocation_pointer (consing_gen)<= heap_segment_committed (seg));

        //fix the allocated size of the segment.
        heap_segment_plan_allocated (seg) = generation_allocation_pointer (consing_gen);

        generation* new_consing_gen = generation_of (max_generation - 1);
        generation_allocation_pointer (new_consing_gen) =
                heap_segment_mem (ephemeral_heap_segment);
        generation_allocation_limit (new_consing_gen) =
            generation_allocation_pointer (new_consing_gen);
        generation_allocation_context_start_region (new_consing_gen) =
            generation_allocation_pointer (new_consing_gen);
        generation_allocation_segment (new_consing_gen) = ephemeral_heap_segment;

        return new_consing_gen;
    }
    else
        return consing_gen;
}

#endif //!USE_REGIONS

inline
void gc_heap::init_alloc_info (generation* gen, heap_segment* seg)
{
    generation_allocation_segment (gen) = seg;
    generation_allocation_pointer (gen) = heap_segment_mem (seg);
    generation_allocation_limit (gen) = generation_allocation_pointer (gen);
    generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
}

inline
heap_segment* gc_heap::get_next_alloc_seg (generation* gen)
{
#ifdef USE_REGIONS
    heap_segment* saved_region = generation_allocation_segment (gen);
    int gen_num = heap_segment_gen_num (saved_region);

    heap_segment* region = saved_region;

    while (1)
    {
        region = heap_segment_non_sip (region);

        if (region)
        {
            break;
        }
        else
        {
            if (gen_num > 0)
            {
                gen_num--;
                region = generation_start_segment (generation_of (gen_num));
                dprintf (REGIONS_LOG, ("h%d next alloc region: switching to next gen%d start %zx(%p)",
                    heap_number, heap_segment_gen_num (region), (size_t)region,
                    heap_segment_mem (region)));
            }
            else
            {
                assert (!"ran out regions when getting the next alloc seg!");
            }
        }
    }

    if (region != saved_region)
    {
        dprintf (REGIONS_LOG, ("init allocate region for gen%d to %p(%d)",
            gen->gen_num, heap_segment_mem (region), heap_segment_gen_num (region)));
        init_alloc_info (gen, region);
    }

    return region;
#else
    return generation_allocation_segment (gen);
#endif //USE_REGIONS
}

bool gc_heap::decide_on_gen1_pin_promotion (float pin_frag_ratio, float pin_surv_ratio)
{
    return ((pin_frag_ratio > 0.15) && (pin_surv_ratio > 0.30));
}

// Add the size of the pinned plug to the higher generation's pinned allocations.
void gc_heap::attribute_pin_higher_gen_alloc (
#ifdef USE_REGIONS
                                              heap_segment* seg, int to_gen_number,
#endif
                                              uint8_t* plug, size_t len)
{
    //find out which gen this pinned plug came from
    int frgn = object_gennum (plug);
    if ((frgn != (int)max_generation) && settings.promotion)
    {
        generation_pinned_allocation_sweep_size (generation_of (frgn + 1)) += len;

#ifdef USE_REGIONS
        // With regions it's a bit more complicated since we only set the plan_gen_num
        // of a region after we've planned it. This means if the pinning plug is in
        // the same seg we are planning, we haven't set its plan_gen_num yet. So we
        // need to check for that first.
        int togn = (in_range_for_segment (plug, seg) ? to_gen_number : object_gennum_plan (plug));
#else
        int togn = object_gennum_plan (plug);
#endif //USE_REGIONS
        if (frgn < togn)
        {
            generation_pinned_allocation_compact_size (generation_of (togn)) += len;
        }
    }
}

#ifdef USE_REGIONS
void gc_heap::attribute_pin_higher_gen_alloc (int frgn, int togn, size_t len)
{
    if ((frgn != (int)max_generation) && settings.promotion)
    {
        generation_pinned_allocation_sweep_size (generation_of (frgn + 1)) += len;

        if (frgn < togn)
        {
            generation_pinned_allocation_compact_size (generation_of (togn)) += len;
        }
    }
}
#endif //USE_REGIONS

uint8_t* gc_heap::allocate_in_condemned_generations (generation* gen,
                                                  size_t size,
                                                  int from_gen_number,
#ifdef SHORT_PLUGS
                                                  BOOL* convert_to_pinned_p,
                                                  uint8_t* next_pinned_plug,
                                                  heap_segment* current_seg,
#endif //SHORT_PLUGS
                                                  uint8_t* old_loc
                                                  REQD_ALIGN_AND_OFFSET_DCL)
{
#ifndef USE_REGIONS
    // Make sure that the youngest generation gap hasn't been allocated
    if (settings.promotion)
    {
        assert (generation_plan_allocation_start (youngest_generation) == 0);
    }
#endif //!USE_REGIONS

    size = Align (size);
    assert (size >= Align (min_obj_size));
    int to_gen_number = from_gen_number;
    if (from_gen_number != (int)max_generation)
    {
        to_gen_number = from_gen_number + (settings.promotion ? 1 : 0);
    }

    dprintf (3, ("aic gen%d: s: %zd, ac: %p-%p", gen->gen_num, size,
            generation_allocation_pointer (gen), generation_allocation_limit (gen)));

#ifdef SHORT_PLUGS
    int pad_in_front = ((old_loc != 0) && (to_gen_number != max_generation)) ? USE_PADDING_FRONT : 0;

    // A near-region-sized plug can't fit with front padding even in an empty region, so skip the padding.
    // This is safe because front padding only exists to protect short plugs (shorter than sizeof(plug_and_gap))
    // from being overwritten by the plug_and_gap header during compaction — a plug this large is in no such danger.
    if ((pad_in_front & USE_PADDING_FRONT) &&
        (size + Align (min_obj_size) >
        ((size_t)1 << min_segment_size_shr) - sizeof (aligned_plug_and_gap)))
    {
        pad_in_front = 0;
    }
#else //SHORT_PLUGS
    int pad_in_front = 0;
#endif //SHORT_PLUGS

    if ((from_gen_number != -1) && (from_gen_number != (int)max_generation) && settings.promotion)
    {
        generation_condemned_allocated (generation_of (from_gen_number + (settings.promotion ? 1 : 0))) += size;
        generation_allocation_size (generation_of (from_gen_number + (settings.promotion ? 1 : 0))) += size;
    }
retry:
    {
        heap_segment* seg = get_next_alloc_seg (gen);
        if (! (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, generation_allocation_pointer (gen),
                           generation_allocation_limit (gen), old_loc,
                           ((generation_allocation_limit (gen) != heap_segment_plan_allocated (seg))?USE_PADDING_TAIL:0)|pad_in_front)))
        {
            if ((! (pinned_plug_que_empty_p()) &&
                 (generation_allocation_limit (gen) ==
                  pinned_plug (oldest_pin()))))
            {
                size_t entry = deque_pinned_plug();
                mark* pinned_plug_entry = pinned_plug_of (entry);
                size_t len = pinned_len (pinned_plug_entry);
                uint8_t* plug = pinned_plug (pinned_plug_entry);
                set_new_pin_info (pinned_plug_entry, generation_allocation_pointer (gen));

#ifdef USE_REGIONS
                if (to_gen_number == 0)
                {
                    update_planned_gen0_free_space (pinned_len (pinned_plug_entry), plug);
                    dprintf (REGIONS_LOG, ("aic: not promotion, gen0 added free space %zd at %p",
                                    pinned_len (pinned_plug_entry), plug));
                }
#endif //USE_REGIONS

#ifdef FREE_USAGE_STATS
                generation_allocated_in_pinned_free (gen) += generation_allocated_since_last_pin (gen);
                dprintf (3, ("allocated %zd so far within pin %zx, total->%zd",
                    generation_allocated_since_last_pin (gen),
                    plug,
                    generation_allocated_in_pinned_free (gen)));
                generation_allocated_since_last_pin (gen) = 0;

                add_item_to_current_pinned_free (gen->gen_num, pinned_len (pinned_plug_of (entry)));
#endif //FREE_USAGE_STATS

                dprintf (3, ("mark stack bos: %zd, tos: %zd, aic: p %p len: %zx->%zx",
                    mark_stack_bos, mark_stack_tos, plug, len, pinned_len (pinned_plug_of (entry))));

                assert(mark_stack_array[entry].len == 0 ||
                       mark_stack_array[entry].len >= Align(min_obj_size));
                generation_allocation_pointer (gen) = plug + len;
                generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
                generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                set_allocator_next_pin (gen);
                attribute_pin_higher_gen_alloc (
#ifdef USE_REGIONS
                                                seg, to_gen_number,
#endif
                                                plug, len);

                goto retry;
            }

            if (generation_allocation_limit (gen) != heap_segment_plan_allocated (seg))
            {
                generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                dprintf (3, ("changed limit to plan alloc: %p", generation_allocation_limit (gen)));
            }
            else
            {
                if (heap_segment_plan_allocated (seg) != heap_segment_committed (seg))
                {
                    heap_segment_plan_allocated (seg) = heap_segment_committed (seg);
                    generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                    dprintf (3, ("changed limit to commit: %p", generation_allocation_limit (gen)));
                }
                else
                {
#if !defined(RESPECT_LARGE_ALIGNMENT) && !defined(USE_REGIONS)
                    assert (gen != youngest_generation);
#endif //!RESPECT_LARGE_ALIGNMENT && !USE_REGIONS

                    if (size_fit_p (size REQD_ALIGN_AND_OFFSET_ARG, generation_allocation_pointer (gen),
                                    heap_segment_reserved (seg), old_loc, USE_PADDING_TAIL | pad_in_front) &&
                        (grow_heap_segment (seg, generation_allocation_pointer (gen), old_loc,
                                            size, pad_in_front REQD_ALIGN_AND_OFFSET_ARG)))
                    {
                        dprintf (3, ("Expanded segment allocation by committing more memory"));
                        heap_segment_plan_allocated (seg) = heap_segment_committed (seg);
                        generation_allocation_limit (gen) = heap_segment_plan_allocated (seg);
                    }
                    else
                    {
                        heap_segment*   next_seg = heap_segment_next (seg);
                        dprintf (REGIONS_LOG, ("aic next: %p(%p,%p) -> %p(%p,%p)",
                            heap_segment_mem (seg), heap_segment_allocated (seg), heap_segment_plan_allocated (seg),
                            (next_seg ? heap_segment_mem (next_seg) : 0),
                            (next_seg ? heap_segment_allocated (next_seg) : 0),
                            (next_seg ? heap_segment_plan_allocated (next_seg) : 0)));
                        assert (generation_allocation_pointer (gen)>=
                                heap_segment_mem (seg));
                        // Verify that all pinned plugs for this segment are consumed
                        if (!pinned_plug_que_empty_p() &&
                            ((pinned_plug (oldest_pin()) < heap_segment_allocated (seg)) &&
                             (pinned_plug (oldest_pin()) >= generation_allocation_pointer (gen))))
                        {
                            LOG((LF_GC, LL_INFO10, "remaining pinned plug %zx while leaving segment on allocation",
                                         pinned_plug (oldest_pin())));
                            FATAL_GC_ERROR();
                        }
                        assert (generation_allocation_pointer (gen)>=
                                heap_segment_mem (seg));
                        assert (generation_allocation_pointer (gen)<=
                                heap_segment_committed (seg));
                        heap_segment_plan_allocated (seg) = generation_allocation_pointer (gen);

#ifdef USE_REGIONS
                        set_region_plan_gen_num (seg, to_gen_number);
                        if ((next_seg == 0) && (heap_segment_gen_num (seg) > 0))
                        {
                            // We need to switch to a younger gen's segments so the allocate seg will be in
                            // sync with the pins.
                            next_seg = generation_start_segment (generation_of (heap_segment_gen_num (seg) - 1));
                            dprintf (REGIONS_LOG, ("h%d aic: switching to next gen%d start %zx(%p)",
                                heap_number, heap_segment_gen_num (next_seg), (size_t)next_seg,
                                heap_segment_mem (next_seg)));
                        }
#endif //USE_REGIONS

                        if (next_seg)
                        {
                            init_alloc_info (gen, next_seg);
                        }
                        else
                        {
#ifdef USE_REGIONS
                            assert (!"should not happen for regions!");
#else
                            return 0; //should only happen during allocation of generation 0 gap
                            // in that case we are going to grow the heap anyway
#endif //USE_REGIONS
                        }
                    }
                }
            }
            set_allocator_next_pin (gen);

            goto retry;
        }
    }

    {
        assert (generation_allocation_pointer (gen)>=
                heap_segment_mem (generation_allocation_segment (gen)));
        uint8_t* result = generation_allocation_pointer (gen);
        size_t pad = 0;
#ifdef SHORT_PLUGS
        if ((pad_in_front & USE_PADDING_FRONT) &&
            (((generation_allocation_pointer (gen) - generation_allocation_context_start_region (gen))==0) ||
             ((generation_allocation_pointer (gen) - generation_allocation_context_start_region (gen))>=DESIRED_PLUG_LENGTH)))
        {
            ptrdiff_t dist = old_loc - result;
            if (dist == 0)
            {
                dprintf (3, ("old alloc: %p, same as new alloc, not padding", old_loc));
                pad = 0;
            }
            else
            {
                if ((dist > 0) && (dist < (ptrdiff_t)Align (min_obj_size)))
                {
                    dprintf (1, ("old alloc: %p, only %zd bytes > new alloc! Shouldn't happen", old_loc, dist));
                    FATAL_GC_ERROR();
                }

                pad = Align (min_obj_size);
                set_plug_padded (old_loc);
            }
        }
#endif //SHORT_PLUGS
#ifdef FEATURE_STRUCTALIGN
        _ASSERTE(!old_loc || alignmentOffset != 0);
        _ASSERTE(old_loc || requiredAlignment == DATA_ALIGNMENT);
        if ((old_loc != 0))
        {
            size_t pad1 = ComputeStructAlignPad(result+pad, requiredAlignment, alignmentOffset);
            set_node_aligninfo (old_loc, requiredAlignment, pad1);
            pad += pad1;
        }
#else // FEATURE_STRUCTALIGN
        if (!((old_loc == 0) || same_large_alignment_p (old_loc, result+pad)))
        {
            pad += switch_alignment_size (pad != 0);
            set_node_realigned(old_loc);
            dprintf (3, ("Allocation realignment old_loc: %zx, new_loc:%zx",
                         (size_t)old_loc, (size_t)(result+pad)));
            assert (same_large_alignment_p (result + pad, old_loc));
        }
#endif // FEATURE_STRUCTALIGN

#ifdef SHORT_PLUGS
        if ((next_pinned_plug != 0) && (pad != 0) && (generation_allocation_segment (gen) == current_seg))
        {
            assert (old_loc != 0);
            ptrdiff_t dist_to_next_pin = (ptrdiff_t)(next_pinned_plug - (generation_allocation_pointer (gen) + size + pad));
            assert (dist_to_next_pin >= 0);

            if ((dist_to_next_pin >= 0) && (dist_to_next_pin < (ptrdiff_t)Align (min_obj_size)))
            {
                dprintf (3, ("%p->(%p,%p),%p(%zx)(%zx),NP->PP",
                    old_loc,
                    generation_allocation_pointer (gen),
                    generation_allocation_limit (gen),
                    next_pinned_plug,
                    size,
                    dist_to_next_pin));
                clear_plug_padded (old_loc);
                pad = 0;
                *convert_to_pinned_p = TRUE;
                record_interesting_data_point (idp_converted_pin);

                return 0;
            }
        }
#endif //SHORT_PLUGS

        if ((old_loc == 0) || (pad != 0))
        {
            //allocating a non plug or a gap, so reset the start region
            generation_allocation_context_start_region (gen) = generation_allocation_pointer (gen);
        }

        generation_allocation_pointer (gen) += size + pad;
        assert (generation_allocation_pointer (gen) <= generation_allocation_limit (gen));

        if ((pad > 0) && (to_gen_number >= 0))
        {
            generation_free_obj_space (generation_of (to_gen_number)) += pad;
        }

#ifdef FREE_USAGE_STATS
        generation_allocated_since_last_pin (gen) += size;
#endif //FREE_USAGE_STATS

        dprintf (3, ("aic: old: %p ptr: %p, limit: %p, sr: %p, res: %p, pad: %zd",
            old_loc,
            generation_allocation_pointer (gen), generation_allocation_limit (gen),
            generation_allocation_context_start_region (gen),
            result, (size_t)pad));

        assert (result + pad);
        return result + pad;
    }
}

CObjectHeader* gc_heap::allocate_uoh_object (size_t jsize, uint32_t flags, int gen_number, int64_t& alloc_bytes)
{
    alloc_context acontext;
    acontext.init();

#if HOST_64BIT
    size_t maxObjectSize = (INT64_MAX - 7 - Align(min_obj_size));
#else
    size_t maxObjectSize = (INT32_MAX - 7 - Align(min_obj_size));
#endif

    if (jsize >= maxObjectSize)
    {
        if (GCConfig::GetBreakOnOOM())
        {
            GCToOSInterface::DebugBreak();
        }
        return NULL;
    }

    size_t size = AlignQword (jsize);
    int align_const = get_alignment_constant (FALSE);
    size_t pad = 0;
#ifdef FEATURE_LOH_COMPACTION
    if (gen_number == loh_generation)
    {
        pad = Align (loh_padding_obj_size, align_const);
    }
#endif //FEATURE_LOH_COMPACTION

    assert (size >= Align (min_obj_size, align_const));
#ifdef _MSC_VER
#pragma inline_depth(0)
#endif //_MSC_VER
    if (! allocate_more_space (&acontext, (size + pad), flags, gen_number))
    {
        return 0;
    }

#ifdef _MSC_VER
#pragma inline_depth(20)
#endif //_MSC_VER

#ifdef FEATURE_LOH_COMPACTION
    // The GC allocator made a free object already in this alloc context and
    // adjusted the alloc_ptr accordingly.
#endif //FEATURE_LOH_COMPACTION

    uint8_t*  result = acontext.alloc_ptr;

    assert ((size_t)(acontext.alloc_limit - acontext.alloc_ptr) == size);
    alloc_bytes += size;

    CObjectHeader* obj = (CObjectHeader*)result;

    assert (obj != 0);
    assert ((size_t)obj == Align ((size_t)obj, align_const));

    return obj;
}
