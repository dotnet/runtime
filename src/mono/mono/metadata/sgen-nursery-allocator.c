/*
 * sgen-managed-allocator.c: Simple generational GC.
 *
 *
 * Copyright 2009-2010 Novell, Inc.
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

/*
 * The young generation is divided into fragments. This is because
 * we can hand one fragments to a thread for lock-less fast alloc and
 * because the young generation ends up fragmented anyway by pinned objects.
 * Once a collection is done, a list of fragments is created. When doing
 * thread local alloc we use smallish nurseries so we allow new threads to
 * allocate memory from gen0 without triggering a collection. Threads that
 * are found to allocate lots of memory are given bigger fragments. This
 * should make the finalizer thread use little nursery memory after a while.
 * We should start assigning threads very small fragments: if there are many
 * threads the nursery will be full of reserved space that the threads may not
 * use at all, slowing down allocation speed.
 * Thread local allocation is done from areas of memory Hotspot calls Thread Local 
 * Allocation Buffers (TLABs).
 */

typedef struct _Fragment Fragment;

struct _Fragment {
	Fragment *next;
	char *fragment_start;
	char *fragment_limit; /* the current soft limit for allocation */
	char *fragment_end;
};

/* the minimum size of a fragment that we consider useful for allocation */
#define FRAGMENT_MIN_SIZE (512)

/* fragments that are free and ready to be used for allocation */
static Fragment *nursery_fragments = NULL;
/* freeelist of fragment structures */
static Fragment *fragment_freelist = NULL;

#ifdef HEAVY_STATISTICS

static long long stat_wasted_fragments_used = 0;
static long long stat_wasted_fragments_bytes = 0;

#endif

static Fragment*
alloc_fragment (void)
{
	Fragment *frag = fragment_freelist;
	if (frag) {
		fragment_freelist = frag->next;
		frag->next = NULL;
		return frag;
	}
	frag = mono_sgen_alloc_internal (INTERNAL_MEM_FRAGMENT);
	frag->next = NULL;
	return frag;
}

static void
add_fragment (char *start, char *end)
{
	Fragment *fragment;

	fragment = alloc_fragment ();
	fragment->fragment_start = start;
	fragment->fragment_limit = start;
	fragment->fragment_end = end;
	fragment->next = nursery_fragments;
	nursery_fragments = fragment;
}



static void
setup_fragment (Fragment *frag, Fragment *prev, size_t size)
{
	/* remove from the list */
	if (prev)
		prev->next = frag->next;
	else
		nursery_fragments = frag->next;
	nursery_next = frag->fragment_start;
	nursery_frag_real_end = frag->fragment_end;

	DEBUG (4, fprintf (gc_debug_file, "Using nursery fragment %p-%p, size: %td (req: %zd)\n", nursery_next, nursery_frag_real_end, nursery_frag_real_end - nursery_next, size));
	frag->next = fragment_freelist;
	fragment_freelist = frag;
}

void
mono_sgen_clear_current_nursery_fragment (char *next)
{
	if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
		g_assert (next <= nursery_frag_real_end);
		DEBUG (4, fprintf (gc_debug_file, "Clear nursery frag %p-%p\n", next, nursery_frag_real_end));
		memset (next, 0, nursery_frag_real_end - next);
	}
}

/* Clear all remaining nursery fragments */
void
mono_sgen_clear_nursery_fragments (char *next)
{
	Fragment *frag;
	if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
		clear_current_nursery_fragment (next);
		for (frag = nursery_fragments; frag; frag = frag->next) {
			DEBUG (4, fprintf (gc_debug_file, "Clear nursery frag %p-%p\n", frag->fragment_start, frag->fragment_end));
			memset (frag->fragment_start, 0, frag->fragment_end - frag->fragment_start);
		}
	}
}

void
mono_sgen_nursery_allocator_prepare_for_pinning (void)
{
	Fragment *frag;

	sgen_nursery_allocator_prepare_for_pinning ();
	/*
	 * The code below starts the search from an entry in scan_starts, which might point into a nursery
	 * fragment containing random data. Clearing the nursery fragments takes a lot of time, and searching
	 * though them too, so lay arrays at each location inside a fragment where a search can start:
	 * - scan_locations[i]
	 * - start_nursery
	 * - the start of each fragment (the last_obj + last_obj case)
	 * The third encompasses the first two, since scan_locations [i] can't point inside a nursery fragment.
	 */
	for (frag = nursery_fragments; frag; frag = frag->next) {
		MonoArray *o;

		g_assert (frag->fragment_end - frag->fragment_start >= sizeof (MonoArray));
		o = (MonoArray*)frag->fragment_start;
		memset (o, 0, sizeof (MonoArray));
		g_assert (array_fill_vtable);
		o->obj.vtable = array_fill_vtable;
		/* Mark this as not a real object */
		o->obj.synchronisation = GINT_TO_POINTER (-1);
		o->max_length = (frag->fragment_end - frag->fragment_start) - sizeof (MonoArray);
		g_assert (frag->fragment_start + safe_object_get_size ((MonoObject*)o) == frag->fragment_end);
	}
}

static mword fragment_total = 0;
/*
 * We found a fragment of free memory in the nursery: memzero it and if
 * it is big enough, add it to the list of fragments that can be used for
 * allocation.
 */
static void
add_nursery_frag (size_t frag_size, char* frag_start, char* frag_end)
{
	DEBUG (4, fprintf (gc_debug_file, "Found empty fragment: %p-%p, size: %zd\n", frag_start, frag_end, frag_size));
	binary_protocol_empty (frag_start, frag_size);
	/* Not worth dealing with smaller fragments: need to tune */
	if (frag_size >= FRAGMENT_MIN_SIZE) {
		/* memsetting just the first chunk start is bound to provide better cache locality */
		if (nursery_clear_policy == CLEAR_AT_GC)
			memset (frag_start, 0, frag_size);

		add_fragment (frag_start, frag_end);
		fragment_total += frag_size;
	} else {
		/* Clear unused fragments, pinning depends on this */
		/*TODO place an int[] here instead of the memset if size justify it*/
		memset (frag_start, 0, frag_size);
	}
}


mword
mono_sgen_build_nursery_fragments (void **start, int num_entries)
{
	char *frag_start, *frag_end;
	size_t frag_size;
	int i;

	while (nursery_fragments) {
		Fragment *next = nursery_fragments->next;
		nursery_fragments->next = fragment_freelist;
		fragment_freelist = nursery_fragments;
		nursery_fragments = next;
	}
	frag_start = nursery_start;
	fragment_total = 0;
	/* clear scan starts */
	memset (nursery_section->scan_starts, 0, nursery_section->num_scan_start * sizeof (gpointer));
	for (i = 0; i < num_entries; ++i) {
		frag_end = start [i];
		/* remove the pin bit from pinned objects */
		unpin_object (frag_end);
		nursery_section->scan_starts [((char*)frag_end - (char*)nursery_section->data)/SCAN_START_SIZE] = frag_end;
		frag_size = frag_end - frag_start;
		if (frag_size)
			add_nursery_frag (frag_size, frag_start, frag_end);
		frag_size = ALIGN_UP (safe_object_get_size ((MonoObject*)start [i]));
		frag_start = (char*)start [i] + frag_size;
	}
	nursery_last_pinned_end = frag_start;
	frag_end = nursery_end;
	frag_size = frag_end - frag_start;
	if (frag_size)
		add_nursery_frag (frag_size, frag_start, frag_end);
	if (!nursery_fragments) {
		DEBUG (1, fprintf (gc_debug_file, "Nursery fully pinned (%d)\n", num_entries));
		for (i = 0; i < num_entries; ++i) {
			DEBUG (3, fprintf (gc_debug_file, "Bastard pinning obj %p (%s), size: %d\n", start [i], safe_name (start [i]), safe_object_get_size (start [i])));
		}
		degraded_mode = 1;
	}

	nursery_next = nursery_frag_real_end = NULL;
	return fragment_total;
}

/*** Nursery memory allocation ***/

/*
 * Allocate a new nursery fragment able to hold an object of size @size.
 * nursery_next and nursery_frag_real_end are set to the boundaries of the fragment.
 * Return TRUE if found, FALSE otherwise.
 */
gboolean
mono_sgen_alloc_fragment_for_size (size_t size)
{
	Fragment *frag, *prev;
	DEBUG (4, fprintf (gc_debug_file, "Searching nursery fragment %p, size: %zd\n", nursery_frag_real_end, size));

	if (nursery_frag_real_end > nursery_next && nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
		/* Clear the remaining space, pinning depends on this */
		memset (nursery_next, 0, nursery_frag_real_end - nursery_next);
	}

	prev = NULL;
	for (frag = nursery_fragments; frag; frag = frag->next) {
		if (size <= (frag->fragment_end - frag->fragment_start)) {
			setup_fragment (frag, prev, size);
			return TRUE;
		}
		prev = frag;
	}
	return FALSE;
}

/*
 * Same as alloc_fragment_for_size but if search for @desired_size fails, try to satisfy @minimum_size.
 * This improves nursery usage.
 */
int
mono_sgen_alloc_fragment_for_size_range (size_t desired_size, size_t minimum_size)
{
	Fragment *frag, *prev, *min_prev;
	DEBUG (4, fprintf (gc_debug_file, "Searching nursery fragment %p, desired size: %zd minimum size %zd\n", nursery_frag_real_end, desired_size, minimum_size));

	if (nursery_frag_real_end > nursery_next && nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
		/* Clear the remaining space, pinning depends on this */
		memset (nursery_next, 0, nursery_frag_real_end - nursery_next);
	}

	min_prev = GINT_TO_POINTER (-1);
	prev = NULL;

	for (frag = nursery_fragments; frag; frag = frag->next) {
		int frag_size = frag->fragment_end - frag->fragment_start;
		if (desired_size <= frag_size) {
			setup_fragment (frag, prev, desired_size);
			return desired_size;
		}
		if (minimum_size <= frag_size)
			min_prev = prev;

		prev = frag;
	}

	if (min_prev != GINT_TO_POINTER (-1)) {
		int frag_size;
		if (min_prev)
			frag = min_prev->next;
		else
			frag = nursery_fragments;

		frag_size = frag->fragment_end - frag->fragment_start;
		HEAVY_STAT (++stat_wasted_fragments_used);
		HEAVY_STAT (stat_wasted_fragments_bytes += frag_size);

		setup_fragment (frag, min_prev, minimum_size);
		return frag_size;
	}

	return 0;
}

/*** Initialization ***/

#ifdef HEAVY_STATISTICS

void
mono_sgen_nursery_allocator_init_heavy_stats (void)
{
	mono_counters_register ("# wasted fragments used", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_wasted_fragments_used);
	mono_counters_register ("bytes in wasted fragments", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_wasted_fragments_bytes);
}

#endif

void
mono_sgen_init_nursery_allocator (void)
{
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_FRAGMENT, sizeof (Fragment));
}

void
mono_sgen_nursery_allocator_set_nursery_bounds (char *nursery_start, char *nursery_end)
{
	/* Setup the single first large fragment */
	add_fragment (nursery_start, nursery_end);	
}
