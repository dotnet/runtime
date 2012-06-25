/*
 * sgen-nursery-allocator.c: Nursery allocation code.
 *
 *
 * Copyright 2009-2010 Novell, Inc.
 *           2011 Rodrigo Kumpera
 * 
 * Copyright 2011 Xamarin Inc  (http://www.xamarin.com)
 *
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
#include "config.h"
#ifdef HAVE_SGEN_GC

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif
#include <stdio.h>
#include <string.h>
#include <signal.h>
#include <errno.h>
#include <assert.h>
#ifdef __MACH__
#undef _XOPEN_SOURCE
#endif
#ifdef __MACH__
#define _XOPEN_SOURCE
#endif

#include "metadata/sgen-gc.h"
#include "metadata/metadata-internals.h"
#include "metadata/class-internals.h"
#include "metadata/gc-internal.h"
#include "metadata/object-internals.h"
#include "metadata/threads.h"
#include "metadata/sgen-cardtable.h"
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-archdep.h"
#include "metadata/sgen-bridge.h"
#include "metadata/mono-gc.h"
#include "metadata/method-builder.h"
#include "metadata/profiler-private.h"
#include "metadata/monitor.h"
#include "metadata/threadpool-internals.h"
#include "metadata/mempool-internals.h"
#include "metadata/marshal.h"
#include "utils/mono-mmap.h"
#include "utils/mono-time.h"
#include "utils/mono-semaphore.h"
#include "utils/mono-counters.h"
#include "utils/mono-proclib.h"
#include "utils/mono-threads.h"

/* Enable it so nursery allocation diagnostic data is collected */
//#define NALLOC_DEBUG 1

/* The mutator allocs from here. */
SgenFragmentAllocator mutator_allocator;

/* freeelist of fragment structures */
static SgenFragment *fragment_freelist = NULL;

/* Allocator cursors */
static char *nursery_last_pinned_end = NULL;

char *sgen_nursery_start;
char *sgen_nursery_end;

#ifdef USER_CONFIG
int sgen_nursery_size = (1 << 22);
#ifdef SGEN_ALIGN_NURSERY
int sgen_nursery_bits = 22;
#endif
#endif

char *sgen_space_bitmap MONO_INTERNAL;
int sgen_space_bitmap_size MONO_INTERNAL;

#ifdef HEAVY_STATISTICS

static gint32 stat_wasted_bytes_trailer = 0;
static gint32 stat_wasted_bytes_small_areas = 0;
static gint32 stat_wasted_bytes_discarded_fragments = 0;
static gint32 stat_nursery_alloc_requests = 0;
static gint32 stat_alloc_iterations = 0;
static gint32 stat_alloc_retries = 0;

static gint32 stat_nursery_alloc_range_requests = 0;
static gint32 stat_alloc_range_iterations = 0;
static gint32 stat_alloc_range_retries = 0;

#endif

/************************************Nursery allocation debugging *********************************************/

#ifdef NALLOC_DEBUG

enum {
	FIXED_ALLOC = 1,
	RANGE_ALLOC,
	PINNING,
	BLOCK_ZEROING,
	CLEAR_NURSERY_FRAGS
};

typedef struct {
	char *address;
	size_t size;
	int reason;
	int seq;
	MonoNativeThreadId tid;
} AllocRecord;

#define ALLOC_RECORD_COUNT 128000


static AllocRecord *alloc_records;
static volatile int next_record;
static volatile int alloc_count;


static const char*
get_reason_name (AllocRecord *rec)
{
	switch (rec->reason) {
	case FIXED_ALLOC: return "fixed-alloc";
	case RANGE_ALLOC: return "range-alloc";
	case PINNING: return "pinning";
	case BLOCK_ZEROING: return "block-zeroing";
	case CLEAR_NURSERY_FRAGS: return "clear-nursery-frag";
	default: return "invalid";
	}
}

static void
reset_alloc_records (void)
{
	next_record = 0;
	alloc_count = 0;
}

static void
add_alloc_record (char *addr, size_t size, int reason)
{
	int idx = InterlockedIncrement (&next_record) - 1;
	alloc_records [idx].address = addr;
	alloc_records [idx].size = size;
	alloc_records [idx].reason = reason;
	alloc_records [idx].seq = idx;
	alloc_records [idx].tid = mono_native_thread_id_get ();
}

static int
comp_alloc_record (const void *_a, const void *_b)
{
	const AllocRecord *a = _a;
	const AllocRecord *b = _b;
	if (a->address == b->address)
		return a->seq - b->seq;
	return a->address - b->address;
}

#define rec_end(REC) ((REC)->address + (REC)->size)

void
dump_alloc_records (void)
{
	int i;
	qsort (alloc_records, next_record, sizeof (AllocRecord), comp_alloc_record);

	printf ("------------------------------------DUMP RECORDS----------------------------\n");
	for (i = 0; i < next_record; ++i) {
		AllocRecord *rec = alloc_records + i;
		printf ("obj [%p, %p] size %zd reason %s seq %d tid %zx\n", rec->address, rec_end (rec), rec->size, get_reason_name (rec), rec->seq, (size_t)rec->tid);
	}
}

void
verify_alloc_records (void)
{
	int i;
	int total = 0;
	int holes = 0;
	int max_hole = 0;
	AllocRecord *prev = NULL;

	qsort (alloc_records, next_record, sizeof (AllocRecord), comp_alloc_record);
	printf ("------------------------------------DUMP RECORDS- %d %d---------------------------\n", next_record, alloc_count);
	for (i = 0; i < next_record; ++i) {
		AllocRecord *rec = alloc_records + i;
		int hole_size = 0;
		total += rec->size;
		if (prev) {
			if (rec_end (prev) > rec->address)
				printf ("WE GOT OVERLAPPING objects %p and %p\n", prev->address, rec->address);
			if ((rec->address - rec_end (prev)) >= 8)
				++holes;
			hole_size = rec->address - rec_end (prev);
			max_hole = MAX (max_hole, hole_size);
		}
		printf ("obj [%p, %p] size %zd hole to prev %d reason %s seq %d tid %zx\n", rec->address, rec_end (rec), rec->size, hole_size, get_reason_name (rec), rec->seq, (size_t)rec->tid);
		prev = rec;
	}
	printf ("SUMMARY total alloc'd %d holes %d max_hole %d\n", total, holes, max_hole);
}

#endif

/*********************************************************************************/


static inline gpointer
mask (gpointer n, uintptr_t bit)
{
	return (gpointer)(((uintptr_t)n) | bit);
}

static inline gpointer
unmask (gpointer p)
{
	return (gpointer)((uintptr_t)p & ~(uintptr_t)0x3);
}

static inline uintptr_t
get_mark (gpointer n)
{
	return (uintptr_t)n & 0x1;
}

/*MUST be called with world stopped*/
SgenFragment*
sgen_fragment_allocator_alloc (void)
{
	SgenFragment *frag = fragment_freelist;
	if (frag) {
		fragment_freelist = frag->next_in_order;
		frag->next = frag->next_in_order = NULL;
		return frag;
	}
	frag = sgen_alloc_internal (INTERNAL_MEM_FRAGMENT);
	frag->next = frag->next_in_order = NULL;
	return frag;
}

void
sgen_fragment_allocator_add (SgenFragmentAllocator *allocator, char *start, char *end)
{
	SgenFragment *fragment;

	fragment = sgen_fragment_allocator_alloc ();
	fragment->fragment_start = start;
	fragment->fragment_next = start;
	fragment->fragment_end = end;
	fragment->next_in_order = fragment->next = unmask (allocator->region_head);

	allocator->region_head = allocator->alloc_head = fragment;
	g_assert (fragment->fragment_end > fragment->fragment_start);
}

void
sgen_fragment_allocator_release (SgenFragmentAllocator *allocator)
{
	SgenFragment *last = allocator->region_head;
	if (!last)
		return;

	/* Find the last fragment in insert order */
	for (; last->next_in_order; last = last->next_in_order) ;

	last->next_in_order = fragment_freelist;
	fragment_freelist = allocator->region_head;
	allocator->alloc_head = allocator->region_head = NULL;
}

static SgenFragment**
find_previous_pointer_fragment (SgenFragmentAllocator *allocator, SgenFragment *frag)
{
	SgenFragment **prev;
	SgenFragment *cur, *next;
#ifdef NALLOC_DEBUG
	int count = 0;
#endif

try_again:
	prev = &allocator->alloc_head;
#ifdef NALLOC_DEBUG
	if (count++ > 5)
		printf ("retry count for fppf is %d\n", count);
#endif

	cur = unmask (*prev);

	while (1) {
		if (cur == NULL)
			return NULL;
		next = cur->next;

		/*
		 * We need to make sure that we dereference prev below
		 * after reading cur->next above, so we need a read
		 * barrier.
		 */
		mono_memory_read_barrier ();

		if (*prev != cur)
			goto try_again;

		if (!get_mark (next)) {
			if (cur == frag)
				return prev;
			prev = &cur->next;
		} else {
			next = unmask (next);
			if (InterlockedCompareExchangePointer ((volatile gpointer*)prev, next, cur) != cur)
				goto try_again;
			/*we must make sure that the next from cur->next happens after*/
			mono_memory_write_barrier ();
		}

		cur = mono_lls_pointer_unmask (next);
	}
	return NULL;
}

static gboolean
claim_remaining_size (SgenFragment *frag, char *alloc_end)
{
	/* All space used, nothing to claim. */
	if (frag->fragment_end <= alloc_end)
		return FALSE;

	/* Try to alloc all the remaining space. */
	return InterlockedCompareExchangePointer ((volatile gpointer*)&frag->fragment_next, frag->fragment_end, alloc_end) == alloc_end;
}

static void*
par_alloc_from_fragment (SgenFragmentAllocator *allocator, SgenFragment *frag, size_t size)
{
	char *p = frag->fragment_next;
	char *end = p + size;

	if (end > frag->fragment_end)
		return NULL;

	/* p = frag->fragment_next must happen before */
	mono_memory_barrier ();

	if (InterlockedCompareExchangePointer ((volatile gpointer*)&frag->fragment_next, end, p) != p)
		return NULL;

	if (frag->fragment_end - end < SGEN_MAX_NURSERY_WASTE) {
		SgenFragment *next, **prev_ptr;
		
		/*
		 * Before we clean the remaining nursery, we must claim the remaining space
		 * as it could end up been used by the range allocator since it can end up
		 * allocating from this dying fragment as it doesn't respect SGEN_MAX_NURSERY_WASTE
		 * when doing second chance allocation.
		 */
		if (sgen_get_nursery_clear_policy () == CLEAR_AT_TLAB_CREATION && claim_remaining_size (frag, end)) {
			sgen_clear_range (end, frag->fragment_end);
			HEAVY_STAT (InterlockedExchangeAdd (&stat_wasted_bytes_trailer, frag->fragment_end - end));
#ifdef NALLOC_DEBUG
			add_alloc_record (end, frag->fragment_end - end, BLOCK_ZEROING);
#endif
		}

		prev_ptr = find_previous_pointer_fragment (allocator, frag);

		/*Use Michaels linked list remove*/

		/*prev_ptr will be null if the fragment was removed concurrently */
		while (prev_ptr) {
			next = frag->next;

			/*already deleted*/
			if (!get_mark (next)) {
				/*frag->next read must happen before the first CAS*/
				mono_memory_write_barrier ();

				/*Fail if the next done is removed concurrently and its CAS wins */
				if (InterlockedCompareExchangePointer ((volatile gpointer*)&frag->next, mask (next, 1), next) != next) {
					continue;
				}
			}

			/* The second CAS must happen after the first CAS or frag->next. */
			mono_memory_write_barrier ();

			/* Fail if the previous node was deleted and its CAS wins */
			if (InterlockedCompareExchangePointer ((volatile gpointer*)prev_ptr, next, frag) != frag) {
				prev_ptr = find_previous_pointer_fragment (allocator, frag);
				continue;
			}
			break;
		}
	}

	return p;
}

static void*
serial_alloc_from_fragment (SgenFragment **previous, SgenFragment *frag, size_t size)
{
	char *p = frag->fragment_next;
	char *end = p + size;

	if (end > frag->fragment_end)
		return NULL;

	frag->fragment_next = end;

	if (frag->fragment_end - end < SGEN_MAX_NURSERY_WASTE) {
		*previous = frag->next;
		
		/* Clear the remaining space, pinning depends on this. FIXME move this to use phony arrays */
		memset (end, 0, frag->fragment_end - end);

		*previous = frag->next;
	}

	return p;
}

void*
sgen_fragment_allocator_par_alloc (SgenFragmentAllocator *allocator, size_t size)
{
	SgenFragment *frag;

#ifdef NALLOC_DEBUG
	InterlockedIncrement (&alloc_count);
#endif

restart:
	for (frag = unmask (allocator->alloc_head); unmask (frag); frag = unmask (frag->next)) {
		HEAVY_STAT (InterlockedIncrement (&stat_alloc_iterations));

		if (size <= (frag->fragment_end - frag->fragment_next)) {
			void *p = par_alloc_from_fragment (allocator, frag, size);
			if (!p) {
				HEAVY_STAT (InterlockedIncrement (&stat_alloc_retries));
				goto restart;
			}
#ifdef NALLOC_DEBUG
			add_alloc_record (p, size, FIXED_ALLOC);
#endif
			return p;
		}
	}
	return NULL;
}

void*
sgen_fragment_allocator_serial_alloc (SgenFragmentAllocator *allocator, size_t size)
{
	SgenFragment *frag;
	SgenFragment **previous;
#ifdef NALLOC_DEBUG
	InterlockedIncrement (&alloc_count);
#endif

	previous = &allocator->alloc_head;

	for (frag = *previous; frag; frag = *previous) {
		char *p = serial_alloc_from_fragment (previous, frag, size);

		HEAVY_STAT (InterlockedIncrement (&stat_alloc_iterations));

		if (p) {
#ifdef NALLOC_DEBUG
			add_alloc_record (p, size, FIXED_ALLOC);
#endif
			return p;
		}
		previous = &frag->next;
	}
	return NULL;
}

void*
sgen_fragment_allocator_serial_range_alloc (SgenFragmentAllocator *allocator, size_t desired_size, size_t minimum_size, size_t *out_alloc_size)
{
	SgenFragment *frag, **previous, *min_frag = NULL, **prev_min_frag = NULL;
	size_t current_minimum = minimum_size;

#ifdef NALLOC_DEBUG
	InterlockedIncrement (&alloc_count);
#endif

	previous = &allocator->alloc_head;

	for (frag = *previous; frag; frag = *previous) {
		size_t frag_size = frag->fragment_end - frag->fragment_next;

		HEAVY_STAT (InterlockedIncrement (&stat_alloc_range_iterations));

		if (desired_size <= frag_size) {
			void *p;
			*out_alloc_size = desired_size;

			p = serial_alloc_from_fragment (previous, frag, desired_size);
#ifdef NALLOC_DEBUG
			add_alloc_record (p, desired_size, RANGE_ALLOC);
#endif
			return p;
		}
		if (current_minimum <= frag_size) {
			min_frag = frag;
			prev_min_frag = previous;
			current_minimum = frag_size;
		}
		previous = &frag->next;
	}

	if (min_frag) {
		void *p;
		size_t frag_size = min_frag->fragment_end - min_frag->fragment_next;
		*out_alloc_size = frag_size;

		p = serial_alloc_from_fragment (prev_min_frag, min_frag, frag_size);

#ifdef NALLOC_DEBUG
		add_alloc_record (p, frag_size, RANGE_ALLOC);
#endif
		return p;
	}

	return NULL;
}

void*
sgen_fragment_allocator_par_range_alloc (SgenFragmentAllocator *allocator, size_t desired_size, size_t minimum_size, size_t *out_alloc_size)
{
	SgenFragment *frag, *min_frag;
	size_t current_minimum;

restart:
	min_frag = NULL;
	current_minimum = minimum_size;

#ifdef NALLOC_DEBUG
	InterlockedIncrement (&alloc_count);
#endif

	for (frag = unmask (allocator->alloc_head); frag; frag = unmask (frag->next)) {
		int frag_size = frag->fragment_end - frag->fragment_next;

		HEAVY_STAT (InterlockedIncrement (&stat_alloc_range_iterations));

		if (desired_size <= frag_size) {
			void *p;
			*out_alloc_size = desired_size;

			p = par_alloc_from_fragment (allocator, frag, desired_size);
			if (!p) {
				HEAVY_STAT (InterlockedIncrement (&stat_alloc_range_retries));
				goto restart;
			}
#ifdef NALLOC_DEBUG
			add_alloc_record (p, desired_size, RANGE_ALLOC);
#endif
			return p;
		}
		if (current_minimum <= frag_size) {
			min_frag = frag;
			current_minimum = frag_size;
		}
	}

	/* The second fragment_next read should be ordered in respect to the first code block */
	mono_memory_barrier ();

	if (min_frag) {
		void *p;
		int frag_size;

		frag_size = min_frag->fragment_end - min_frag->fragment_next;
		if (frag_size < minimum_size)
			goto restart;

		*out_alloc_size = frag_size;

		mono_memory_barrier ();
		p = par_alloc_from_fragment (allocator, min_frag, frag_size);

		/*XXX restarting here is quite dubious given this is already second chance allocation. */
		if (!p) {
			HEAVY_STAT (InterlockedIncrement (&stat_alloc_retries));
			goto restart;
		}
#ifdef NALLOC_DEBUG
		add_alloc_record (p, frag_size, RANGE_ALLOC);
#endif
		return p;
	}

	return NULL;
}

void
sgen_clear_allocator_fragments (SgenFragmentAllocator *allocator)
{
	SgenFragment *frag;

	for (frag = unmask (allocator->alloc_head); frag; frag = unmask (frag->next)) {
		DEBUG (4, fprintf (gc_debug_file, "Clear nursery frag %p-%p\n", frag->fragment_next, frag->fragment_end));
		sgen_clear_range (frag->fragment_next, frag->fragment_end);
#ifdef NALLOC_DEBUG
		add_alloc_record (frag->fragment_next, frag->fragment_end - frag->fragment_next, CLEAR_NURSERY_FRAGS);
#endif
	}	
}

/* Clear all remaining nursery fragments */
void
sgen_clear_nursery_fragments (void)
{
	if (sgen_get_nursery_clear_policy () == CLEAR_AT_TLAB_CREATION) {
		sgen_clear_allocator_fragments (&mutator_allocator);
		sgen_minor_collector.clear_fragments ();
	}
}

/*
 * Mark a given range of memory as invalid.
 *
 * This can be done either by zeroing memory or by placing
 * a phony byte[] array. This keeps the heap forward walkable.
 *
 * This function ignores calls with a zero range, even if
 * both start and end are NULL.
 */
void
sgen_clear_range (char *start, char *end)
{
	MonoArray *o;
	size_t size = end - start;

	if ((start && !end) || (start > end))
		g_error ("Invalid range [%p %p]", start, end);

	if (size < sizeof (MonoArray)) {
		memset (start, 0, size);
		return;
	}

	o = (MonoArray*)start;
	o->obj.vtable = sgen_get_array_fill_vtable ();
	/* Mark this as not a real object */
	o->obj.synchronisation = GINT_TO_POINTER (-1);
	o->bounds = NULL;
	o->max_length = size - sizeof (MonoArray);
	sgen_set_nursery_scan_start (start);
	g_assert (start + sgen_safe_object_get_size ((MonoObject*)o) == end);
}

void
sgen_nursery_allocator_prepare_for_pinning (void)
{
	sgen_clear_allocator_fragments (&mutator_allocator);
	sgen_minor_collector.clear_fragments ();
}

static mword fragment_total = 0;
/*
 * We found a fragment of free memory in the nursery: memzero it and if
 * it is big enough, add it to the list of fragments that can be used for
 * allocation.
 */
static void
add_nursery_frag (SgenFragmentAllocator *allocator, size_t frag_size, char* frag_start, char* frag_end)
{
	DEBUG (4, fprintf (gc_debug_file, "Found empty fragment: %p-%p, size: %zd\n", frag_start, frag_end, frag_size));
	binary_protocol_empty (frag_start, frag_size);
	/* Not worth dealing with smaller fragments: need to tune */
	if (frag_size >= SGEN_MAX_NURSERY_WASTE) {
		/* memsetting just the first chunk start is bound to provide better cache locality */
		if (sgen_get_nursery_clear_policy () == CLEAR_AT_GC)
			memset (frag_start, 0, frag_size);

#ifdef NALLOC_DEBUG
		/* XXX convert this into a flight record entry
		printf ("\tfragment [%p %p] size %zd\n", frag_start, frag_end, frag_size);
		*/
#endif
		sgen_fragment_allocator_add (allocator, frag_start, frag_end);
		fragment_total += frag_size;
	} else {
		/* Clear unused fragments, pinning depends on this */
		sgen_clear_range (frag_start, frag_end);
		HEAVY_STAT (InterlockedExchangeAdd (&stat_wasted_bytes_small_areas, frag_size));
	}
}

static void
fragment_list_reverse (SgenFragmentAllocator *allocator)
{
	SgenFragment *prev = NULL, *list = allocator->region_head;
	while (list) {
		SgenFragment *next = list->next;
		list->next = prev;
		list->next_in_order = prev;
		prev = list;
		list = next;
	}

	allocator->region_head = allocator->alloc_head = prev;
}

mword
sgen_build_nursery_fragments (GCMemSection *nursery_section, void **start, int num_entries)
{
	char *frag_start, *frag_end;
	size_t frag_size;
	int i = 0;
	SgenFragment *frags_ranges;

#ifdef NALLOC_DEBUG
	reset_alloc_records ();
#endif
	/*The mutator fragments are done. We no longer need them. */
	sgen_fragment_allocator_release (&mutator_allocator);

	frag_start = sgen_nursery_start;
	fragment_total = 0;

	/* The current nursery might give us a fragment list to exclude [start, next[*/
	frags_ranges = sgen_minor_collector.build_fragments_get_exclude_head ();

	/* clear scan starts */
	memset (nursery_section->scan_starts, 0, nursery_section->num_scan_start * sizeof (gpointer));

	while (i < num_entries || frags_ranges) {
		char *addr0, *addr1;
		size_t size;
		SgenFragment *last_frag = NULL;

		addr0 = addr1 = sgen_nursery_end;
		if (i < num_entries)
			addr0 = start [i];
		if (frags_ranges)
			addr1 = frags_ranges->fragment_start;

		if (addr0 < addr1) {
			SGEN_UNPIN_OBJECT (addr0);
			sgen_set_nursery_scan_start (addr0);
			frag_end = addr0;
			size = SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)addr0));
			++i;
		} else {
			frag_end = addr1;
			size = frags_ranges->fragment_next - addr1;
			last_frag = frags_ranges;
			frags_ranges = frags_ranges->next_in_order;
		}

		frag_size = frag_end - frag_start;

		if (size == 0)
			continue;

		g_assert (frag_size >= 0);
		g_assert (size > 0);
		if (frag_size && size)
			add_nursery_frag (&mutator_allocator, frag_size, frag_start, frag_end);	

		frag_size = size;
#ifdef NALLOC_DEBUG
		add_alloc_record (start [i], frag_size, PINNING);
#endif
		frag_start = frag_end + frag_size;
	}

	nursery_last_pinned_end = frag_start;
	frag_end = sgen_nursery_end;
	frag_size = frag_end - frag_start;
	if (frag_size)
		add_nursery_frag (&mutator_allocator, frag_size, frag_start, frag_end);

	/* Now it's safe to release the fragments exclude list. */
	sgen_minor_collector.build_fragments_release_exclude_head ();

	/* First we reorder the fragment list to be in ascending address order. This makes H/W prefetchers happier. */
	fragment_list_reverse (&mutator_allocator);

	/*The collector might want to do something with the final nursery fragment list.*/
	sgen_minor_collector.build_fragments_finish (&mutator_allocator);

	if (!unmask (mutator_allocator.alloc_head)) {
		DEBUG (1, fprintf (gc_debug_file, "Nursery fully pinned (%d)\n", num_entries));
		for (i = 0; i < num_entries; ++i) {
			DEBUG (3, fprintf (gc_debug_file, "Bastard pinning obj %p (%s), size: %d\n", start [i], sgen_safe_name (start [i]), sgen_safe_object_get_size (start [i])));
		}
	}
	return fragment_total;
}

char *
sgen_nursery_alloc_get_upper_alloc_bound (void)
{
	/*FIXME we need to calculate the collector upper bound as well, but this must be done in the previous GC. */
	return sgen_nursery_end;
}

/*** Nursery memory allocation ***/
void
sgen_nursery_retire_region (void *address, ptrdiff_t size)
{
	HEAVY_STAT (InterlockedExchangeAdd (&stat_wasted_bytes_discarded_fragments, size));
}

gboolean
sgen_can_alloc_size (size_t size)
{
	SgenFragment *frag;
	size = SGEN_ALIGN_UP (size);

	for (frag = unmask (mutator_allocator.alloc_head); frag; frag = unmask (frag->next)) {
		if ((frag->fragment_end - frag->fragment_next) >= size)
			return TRUE;
	}
	return FALSE;
}

void*
sgen_nursery_alloc (size_t size)
{
	DEBUG (4, fprintf (gc_debug_file, "Searching nursery for size: %zd\n", size));
	size = SGEN_ALIGN_UP (size);

	HEAVY_STAT (InterlockedIncrement (&stat_nursery_alloc_requests));

	return sgen_fragment_allocator_par_alloc (&mutator_allocator, size);
}

void*
sgen_nursery_alloc_range (size_t desired_size, size_t minimum_size, size_t *out_alloc_size)
{
	DEBUG (4, fprintf (gc_debug_file, "Searching for byte range desired size: %zd minimum size %zd\n", desired_size, minimum_size));

	HEAVY_STAT (InterlockedIncrement (&stat_nursery_alloc_range_requests));

	return sgen_fragment_allocator_par_range_alloc (&mutator_allocator, desired_size, minimum_size, out_alloc_size);
}

/*** Initialization ***/

#ifdef HEAVY_STATISTICS

void
sgen_nursery_allocator_init_heavy_stats (void)
{
	mono_counters_register ("bytes wasted trailer fragments", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wasted_bytes_trailer);
	mono_counters_register ("bytes wasted small areas", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wasted_bytes_small_areas);
	mono_counters_register ("bytes wasted discarded fragments", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wasted_bytes_discarded_fragments);

	mono_counters_register ("# nursery alloc requests", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_nursery_alloc_requests);
	mono_counters_register ("# nursery alloc iterations", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_alloc_iterations);
	mono_counters_register ("# nursery alloc retries", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_alloc_retries);

	mono_counters_register ("# nursery alloc range requests", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_nursery_alloc_range_requests);
	mono_counters_register ("# nursery alloc range iterations", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_alloc_range_iterations);
	mono_counters_register ("# nursery alloc range restries", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_alloc_range_retries);
}

#endif

void
sgen_init_nursery_allocator (void)
{
	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_FRAGMENT, sizeof (SgenFragment));
#ifdef NALLOC_DEBUG
	alloc_records = sgen_alloc_os_memory (sizeof (AllocRecord) * ALLOC_RECORD_COUNT, TRUE);
#endif
}

void
sgen_nursery_alloc_prepare_for_minor (void)
{
	sgen_minor_collector.prepare_to_space (sgen_space_bitmap, sgen_space_bitmap_size);
}

void
sgen_nursery_alloc_prepare_for_major (const char *reason)
{
	sgen_minor_collector.prepare_to_space (sgen_space_bitmap, sgen_space_bitmap_size);
}

void
sgen_nursery_allocator_set_nursery_bounds (char *start, char *end)
{
	sgen_nursery_start = start;
	sgen_nursery_end = end;

	sgen_space_bitmap_size = (end - start) / (SGEN_TO_SPACE_GRANULE_IN_BYTES * 8);
	sgen_space_bitmap = g_malloc0 (sgen_space_bitmap_size);

	/* Setup the single first large fragment */
	sgen_minor_collector.init_nursery (&mutator_allocator, start, end);
}

#endif
