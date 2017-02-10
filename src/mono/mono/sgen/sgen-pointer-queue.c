/**
 * \file
 * A pointer queue that can be sorted.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-pointer-queue.h"

void
sgen_pointer_queue_clear (SgenPointerQueue *queue)
{
	queue->next_slot = 0;
}

void
sgen_pointer_queue_init (SgenPointerQueue *queue, int mem_type)
{
	queue->next_slot = 0;
	queue->size = 0;
	queue->data = NULL;
	queue->mem_type = mem_type;
}

static void
realloc_queue (SgenPointerQueue *queue)
{
	size_t new_size = queue->size ? queue->size + queue->size/2 : 1024;
	void **new_data = (void **)sgen_alloc_internal_dynamic (sizeof (void*) * new_size, queue->mem_type, TRUE);

	memcpy (new_data, queue->data, sizeof (void*) * queue->next_slot);
	sgen_free_internal_dynamic (queue->data, sizeof (void*) * queue->size, queue->mem_type);
	queue->data = new_data;
	queue->size = new_size;
	SGEN_LOG (4, "Reallocated pointer queue to size: %lu", new_size);
}

gboolean
sgen_pointer_queue_will_grow (SgenPointerQueue *queue)
{
	return queue->next_slot >= queue->size;
}

void
sgen_pointer_queue_add (SgenPointerQueue *queue, void *ptr)
{
	if (sgen_pointer_queue_will_grow (queue))
		realloc_queue (queue);

	queue->data [queue->next_slot++] = ptr;
}

void*
sgen_pointer_queue_pop (SgenPointerQueue *queue)
{
	g_assert (queue->next_slot);

	return queue->data [--queue->next_slot];
}

size_t
sgen_pointer_queue_search (SgenPointerQueue *queue, void *addr)
{
	size_t first = 0, last = queue->next_slot;
	while (first < last) {
		size_t middle = first + ((last - first) >> 1);
		if (addr <= queue->data [middle])
			last = middle;
		else
			first = middle + 1;
	}
	g_assert (first == last);
	return first;
}

/*
 * Removes all NULL pointers from the queue.
 */
void
sgen_pointer_queue_remove_nulls (SgenPointerQueue *queue)
{
	void **start, **cur, **end;
	start = cur = queue->data;
	end = queue->data + queue->next_slot;
	while (cur < end) {
		if (*cur)
			*start++ = *cur++;
		else
			++cur;
	}
	queue->next_slot = start - queue->data;
}

/*
 * Sorts the pointers in the queue, then removes duplicates.
 */
void
sgen_pointer_queue_sort_uniq (SgenPointerQueue *queue)
{
	void **start, **cur, **end;
	/* sort and uniq pin_queue: we just sort and we let the rest discard multiple values */
	/* it may be better to keep ranges of pinned memory instead of individually pinning objects */
	SGEN_LOG (5, "Sorting pointer queue, size: %lu", queue->next_slot);
	if (queue->next_slot > 1)
		sgen_sort_addresses (queue->data, queue->next_slot);
	start = cur = queue->data;
	end = queue->data + queue->next_slot;
	while (cur < end) {
		*start = *cur++;
		while (cur < end && *start == *cur)
			cur++;
		start++;
	};
	queue->next_slot = start - queue->data;
	SGEN_LOG (5, "Pointer queue reduced to size: %lu", queue->next_slot);
}

/*
 * Does a linear search through the pointer queue to find `ptr`.  Returns the index if
 * found, otherwise (size_t)-1.
 */
size_t
sgen_pointer_queue_find (SgenPointerQueue *queue, void *ptr)
{
	size_t i;
	for (i = 0; i < queue->next_slot; ++i)
		if (queue->data [i] == ptr)
			return i;
	return (size_t)-1;
}

gboolean
sgen_pointer_queue_is_empty (SgenPointerQueue *queue)
{
	return !queue->next_slot;
}

void
sgen_pointer_queue_free (SgenPointerQueue *queue)
{
	sgen_free_internal_dynamic (queue->data, sizeof (void*) * queue->size, queue->mem_type);
}

#endif
