/*
 * sgen-pinning.c: The pin queue.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
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

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-pinning.h"

static void** pin_queue;
static int pin_queue_size = 0;
static int next_pin_slot = 0;
static int last_num_pinned = 0;

#define PIN_HASH_SIZE 1024
static void *pin_hash_filter [PIN_HASH_SIZE];

void
sgen_init_pinning (void)
{
	memset (pin_hash_filter, 0, sizeof (pin_hash_filter));
}

void
sgen_finish_pinning (void)
{
	last_num_pinned = next_pin_slot;
	next_pin_slot = 0;
}

static void
realloc_pin_queue (void)
{
	int new_size = pin_queue_size? pin_queue_size + pin_queue_size/2: 1024;
	void **new_pin = sgen_alloc_internal_dynamic (sizeof (void*) * new_size, INTERNAL_MEM_PIN_QUEUE, TRUE);
	memcpy (new_pin, pin_queue, sizeof (void*) * next_pin_slot);
	sgen_free_internal_dynamic (pin_queue, sizeof (void*) * pin_queue_size, INTERNAL_MEM_PIN_QUEUE);
	pin_queue = new_pin;
	pin_queue_size = new_size;
	SGEN_LOG (4, "Reallocated pin queue to size: %d", new_size);
}

void
sgen_pin_stage_ptr (void *ptr)
{
	/*very simple multiplicative hash function, tons better than simple and'ng */ 
	int hash_idx = ((mword)ptr * 1737350767) & (PIN_HASH_SIZE - 1);
	if (pin_hash_filter [hash_idx] == ptr)
		return;

	pin_hash_filter [hash_idx] = ptr;

	if (next_pin_slot >= pin_queue_size)
		realloc_pin_queue ();

	pin_queue [next_pin_slot++] = ptr;
}

static int
optimized_pin_queue_search (void *addr)
{
	int first = 0, last = next_pin_slot;
	while (first < last) {
		int middle = first + ((last - first) >> 1);
		if (addr <= pin_queue [middle])
			last = middle;
		else
			first = middle + 1;
	}
	g_assert (first == last);
	return first;
}

void**
sgen_find_optimized_pin_queue_area (void *start, void *end, int *num)
{
	int first, last;
	first = optimized_pin_queue_search (start);
	last = optimized_pin_queue_search (end);
	*num = last - first;
	if (first == last)
		return NULL;
	return pin_queue + first;
}

void
sgen_find_section_pin_queue_start_end (GCMemSection *section)
{
	SGEN_LOG (6, "Pinning from section %p (%p-%p)", section, section->data, section->end_data);
	section->pin_queue_start = sgen_find_optimized_pin_queue_area (section->data, section->end_data, &section->pin_queue_num_entries);
	SGEN_LOG (6, "Found %d pinning addresses in section %p", section->pin_queue_num_entries, section);
}

/*This will setup the given section for the while pin queue. */
void
sgen_pinning_setup_section (GCMemSection *section)
{
	section->pin_queue_start = pin_queue;
	section->pin_queue_num_entries = next_pin_slot;
}

void
sgen_pinning_trim_queue_to_section (GCMemSection *section)
{
	next_pin_slot = section->pin_queue_num_entries;
}

void
sgen_pin_queue_clear_discarded_entries (GCMemSection *section, int max_pin_slot)
{
	void **start = section->pin_queue_start + section->pin_queue_num_entries;
	void **end = pin_queue + max_pin_slot;
	void *addr;

	if (!start)
		return;

	for (; start < end; ++start) {
		addr = *start;
		if ((char*)addr < section->data || (char*)addr > section->end_data)
			break;
		*start = NULL;
	}
}

/* reduce the info in the pin queue, removing duplicate pointers and sorting them */
void
sgen_optimize_pin_queue (int start_slot)
{
	void **start, **cur, **end;
	/* sort and uniq pin_queue: we just sort and we let the rest discard multiple values */
	/* it may be better to keep ranges of pinned memory instead of individually pinning objects */
	SGEN_LOG (5, "Sorting pin queue, size: %d", next_pin_slot);
	if ((next_pin_slot - start_slot) > 1)
		sgen_sort_addresses (pin_queue + start_slot, next_pin_slot - start_slot);
	start = cur = pin_queue + start_slot;
	end = pin_queue + next_pin_slot;
	while (cur < end) {
		*start = *cur++;
		while (*start == *cur && cur < end)
			cur++;
		start++;
	};
	next_pin_slot = start - pin_queue;
	SGEN_LOG (5, "Pin queue reduced to size: %d", next_pin_slot);
}

int
sgen_get_pinned_count (void)
{
	return next_pin_slot;
}

void
sgen_dump_pin_queue (void)
{
	int i;

	for (i = 0; i < last_num_pinned; ++i) {
		SGEN_LOG (3, "Bastard pinning obj %p (%s), size: %d", pin_queue [i], sgen_safe_name (pin_queue [i]), sgen_safe_object_get_size (pin_queue [i]));
	}	
}

#endif /* HAVE_SGEN_GC */
