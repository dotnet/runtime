/*
 * sgen-pointer-queue.c: A pointer queue that can be sorted.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#include "metadata/sgen-gc.h"
#include "metadata/sgen-pointer-queue.h"

#ifdef HAVE_SGEN_GC

void
sgen_pointer_queue_clear (SgenPointerQueue *queue)
{
	queue->next_slot = 0;
}

static void
realloc_queue (SgenPointerQueue *queue)
{
	size_t new_size = queue->size ? queue->size + queue->size/2 : 1024;
	void **new_data = sgen_alloc_internal_dynamic (sizeof (void*) * new_size, INTERNAL_MEM_PIN_QUEUE, TRUE);
	memcpy (new_data, queue->data, sizeof (void*) * queue->next_slot);
	sgen_free_internal_dynamic (queue->data, sizeof (void*) * queue->size, INTERNAL_MEM_PIN_QUEUE);
	queue->data = new_data;
	queue->size = new_size;
	SGEN_LOG (4, "Reallocated pointer queue to size: %lu", new_size);
}

void
sgen_pointer_queue_add (SgenPointerQueue *queue, void *ptr)
{
	if (queue->next_slot >= queue->size)
		realloc_queue (queue);

	queue->data [queue->next_slot++] = ptr;
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
		while (*start == *cur && cur < end)
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
	for (size_t i = 0; i < queue->next_slot; ++i)
		if (queue->data [i] == ptr)
			return i;
	return (size_t)-1;
}

#endif
