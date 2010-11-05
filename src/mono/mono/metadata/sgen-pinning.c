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
#define PIN_STAGING_AREA_SIZE	1024

static void* pin_staging_area [PIN_STAGING_AREA_SIZE];
static int pin_staging_area_index;

static void** pin_queue;
static int pin_queue_size = 0;
static int next_pin_slot = 0;

static void
init_pinning (void)
{
	pin_staging_area_index = 0;
}

static void
realloc_pin_queue (void)
{
	int new_size = pin_queue_size? pin_queue_size + pin_queue_size/2: 1024;
	void **new_pin = mono_sgen_alloc_internal_dynamic (sizeof (void*) * new_size, INTERNAL_MEM_PIN_QUEUE);
	memcpy (new_pin, pin_queue, sizeof (void*) * next_pin_slot);
	mono_sgen_free_internal_dynamic (pin_queue, sizeof (void*) * pin_queue_size, INTERNAL_MEM_PIN_QUEUE);
	pin_queue = new_pin;
	pin_queue_size = new_size;
	DEBUG (4, fprintf (gc_debug_file, "Reallocated pin queue to size: %d\n", new_size));
}

static void
evacuate_pin_staging_area (void)
{
	int i;

	g_assert (pin_staging_area_index >= 0 && pin_staging_area_index <= PIN_STAGING_AREA_SIZE);

	if (pin_staging_area_index == 0)
		return;

	/*
	 * The pinning addresses might come from undefined memory, this is normal. Since they
	 * are used in lots of functions, we make the memory defined here instead of having
	 * to add a supression for those functions.
	 */
	VALGRIND_MAKE_MEM_DEFINED (pin_staging_area, pin_staging_area_index * sizeof (void*));

	sort_addresses (pin_staging_area, pin_staging_area_index);

	while (next_pin_slot + pin_staging_area_index > pin_queue_size)
		realloc_pin_queue ();

	pin_queue [next_pin_slot++] = pin_staging_area [0];
	for (i = 1; i < pin_staging_area_index; ++i) {
		void *p = pin_staging_area [i];
		if (p != pin_queue [next_pin_slot - 1])
			pin_queue [next_pin_slot++] = p;
	}

	g_assert (next_pin_slot <= pin_queue_size);

	pin_staging_area_index = 0;
}

static void
pin_stage_ptr (void *ptr)
{
	if (pin_staging_area_index >= PIN_STAGING_AREA_SIZE)
		evacuate_pin_staging_area ();

	pin_staging_area [pin_staging_area_index++] = ptr;
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
mono_sgen_find_optimized_pin_queue_area (void *start, void *end, int *num)
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
mono_sgen_find_section_pin_queue_start_end (GCMemSection *section)
{
	DEBUG (6, fprintf (gc_debug_file, "Pinning from section %p (%p-%p)\n", section, section->data, section->end_data));
	section->pin_queue_start = mono_sgen_find_optimized_pin_queue_area (section->data, section->end_data, &section->pin_queue_num_entries);
	DEBUG (6, fprintf (gc_debug_file, "Found %d pinning addresses in section %p\n", section->pin_queue_num_entries, section));
}

static void
mono_sgen_pin_queue_clear_discarded_entries (GCMemSection *section, int max_pin_slot)
{
	void **start = section->pin_queue_start + section->pin_queue_num_entries;
	void **end = pin_queue + max_pin_slot;
	void *addr;

	for (; start < end; ++start) {
		addr = *start;
		if ((char*)addr < section->data || (char*)addr > section->end_data)
			break;
		*start = NULL;
	}
}
