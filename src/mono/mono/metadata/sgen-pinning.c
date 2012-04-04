/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
	void **new_pin = sgen_alloc_internal_dynamic (sizeof (void*) * new_size, INTERNAL_MEM_PIN_QUEUE);
	memcpy (new_pin, pin_queue, sizeof (void*) * next_pin_slot);
	sgen_free_internal_dynamic (pin_queue, sizeof (void*) * pin_queue_size, INTERNAL_MEM_PIN_QUEUE);
	pin_queue = new_pin;
	pin_queue_size = new_size;
	DEBUG (4, fprintf (gc_debug_file, "Reallocated pin queue to size: %d\n", new_size));
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
	DEBUG (6, fprintf (gc_debug_file, "Pinning from section %p (%p-%p)\n", section, section->data, section->end_data));
	section->pin_queue_start = sgen_find_optimized_pin_queue_area (section->data, section->end_data, &section->pin_queue_num_entries);
	DEBUG (6, fprintf (gc_debug_file, "Found %d pinning addresses in section %p\n", section->pin_queue_num_entries, section));
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

static G_GNUC_UNUSED void
print_nursery_gaps (void* start_nursery, void *end_nursery)
{
	int i;
	gpointer first = start_nursery;
	gpointer next;
	for (i = 0; i < next_pin_slot; ++i) {
		next = pin_queue [i];
		fprintf (gc_debug_file, "Nursery range: %p-%p, size: %td\n", first, next, (char*)next-(char*)first);
		first = next;
	}
	next = end_nursery;
	fprintf (gc_debug_file, "Nursery range: %p-%p, size: %td\n", first, next, (char*)next-(char*)first);
}

/* reduce the info in the pin queue, removing duplicate pointers and sorting them */
void
sgen_optimize_pin_queue (int start_slot)
{
	void **start, **cur, **end;
	/* sort and uniq pin_queue: we just sort and we let the rest discard multiple values */
	/* it may be better to keep ranges of pinned memory instead of individually pinning objects */
	DEBUG (5, fprintf (gc_debug_file, "Sorting pin queue, size: %d\n", next_pin_slot));
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
	DEBUG (5, fprintf (gc_debug_file, "Pin queue reduced to size: %d\n", next_pin_slot));
	//DEBUG (6, print_nursery_gaps (start_nursery, end_nursery));	
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
		DEBUG (3, fprintf (gc_debug_file, "Bastard pinning obj %p (%s), size: %d\n", pin_queue [i], sgen_safe_name (pin_queue [i]), sgen_safe_object_get_size (pin_queue [i])));
	}	
}

#endif /* HAVE_SGEN_GC */
