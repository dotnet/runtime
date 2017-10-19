/**
 * \file
 * Nursery allocation code.
 *
 * Copyright 2009-2010 Novell, Inc.
 *           2011 Rodrigo Kumpera
 * 
 * Copyright 2011 Xamarin Inc  (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
#include <errno.h>
#include <assert.h>
#ifdef __MACH__
#undef _XOPEN_SOURCE
#endif
#ifdef __MACH__
#define _XOPEN_SOURCE
#endif

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-cardtable.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-pinning.h"
#include "mono/sgen/sgen-client.h"
#include "mono/utils/mono-membar.h"

/* Enable it so nursery allocation diagnostic data is collected */
//#define NALLOC_DEBUG 1

/* The mutator allocs from here. */
static SgenFragmentAllocator mutator_allocator;

/* freeelist of fragment structures */
static SgenFragment *fragment_freelist = NULL;

char *sgen_nursery_start;
char *sgen_nursery_end;

/* good sizes are 512KB-1MB: larger ones increase a lot memzeroing time */
size_t sgen_nursery_size;
/*
 * Maximum size that we can resize the nursery to.
 * If sgen_nursery_default_size == sgen_nursery_max_size then we are not
 * dynamically resizing the nursery
 */
size_t sgen_nursery_max_size;
size_t sgen_nursery_min_size;
/* The number of trailing 0 bits in sgen_nursery_max_size */
int sgen_nursery_bits;


char *sgen_space_bitmap;
size_t sgen_space_bitmap_size;

#ifdef HEAVY_STATISTICS

static mword stat_wasted_bytes_trailer = 0;
static mword stat_wasted_bytes_small_areas = 0;
static mword stat_wasted_bytes_discarded_fragments = 0;
static guint64 stat_nursery_alloc_requests = 0;
static guint64 stat_alloc_iterations = 0;
static guint64 stat_alloc_retries = 0;

static guint64 stat_nursery_alloc_range_requests = 0;
static guint64 stat_alloc_range_iterations = 0;
static guint64 stat_alloc_range_retries = 0;

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

void dump_alloc_records (void);
void verify_alloc_records (void);

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
	int idx = mono_atomic_inc_i32 (&next_record) - 1;
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
	sgen_qsort (alloc_records, next_record, sizeof (AllocRecord), comp_alloc_record);

	printf ("------------------------------------DUMP RECORDS----------------------------\n");
	for (i = 0; i < next_record; ++i) {
		AllocRecord *rec = alloc_records + i;
		printf ("obj [%p, %p] size %d reason %s seq %d tid %x\n", rec->address, rec_end (rec), (int)rec->size, get_reason_name (rec), rec->seq, (size_t)rec->tid);
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

	sgen_qsort (alloc_records, next_record, sizeof (AllocRecord), comp_alloc_record);
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
		printf ("obj [%p, %p] size %d hole to prev %d reason %s seq %d tid %zx\n", rec->address, rec_end (rec), (int)rec->size, hole_size, get_reason_name (rec), rec->seq, (size_t)rec->tid);
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
	frag = (SgenFragment *)sgen_alloc_internal (INTERNAL_MEM_FRAGMENT);
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
	fragment->next_in_order = fragment->next = (SgenFragment *)unmask (allocator->region_head);

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

	cur = (SgenFragment *)unmask (*prev);

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
			next = (SgenFragment *)unmask (next);
			if (mono_atomic_cas_ptr ((volatile gpointer*)prev, next, cur) != cur)
				goto try_again;
			/*we must make sure that the next from cur->next happens after*/
			mono_memory_write_barrier ();
		}

		cur = (SgenFragment *)unmask (next);
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
	return mono_atomic_cas_ptr ((volatile gpointer*)&frag->fragment_next, frag->fragment_end, alloc_end) == alloc_end;
}

static void*
par_alloc_from_fragment (SgenFragmentAllocator *allocator, SgenFragment *frag, size_t size)
{
	char *p = frag->fragment_next;
	char *end = p + size;

	if (end > frag->fragment_end || end > (sgen_nursery_start + sgen_nursery_size))
		return NULL;

	/* p = frag->fragment_next must happen before */
	mono_memory_barrier ();

	if (mono_atomic_cas_ptr ((volatile gpointer*)&frag->fragment_next, end, p) != p)
		return NULL;

	if (frag->fragment_end - end < SGEN_MAX_NURSERY_WASTE) {
		SgenFragment *next, **prev_ptr;
		
		/*
		 * Before we clean the remaining nursery, we must claim the remaining space
		 * as it could end up been used by the range allocator since it can end up
		 * allocating from this dying fragment as it doesn't respect SGEN_MAX_NURSERY_WASTE
		 * when doing second chance allocation.
		 */
		if ((sgen_get_nursery_clear_policy () == CLEAR_AT_TLAB_CREATION || sgen_get_nursery_clear_policy () == CLEAR_AT_TLAB_CREATION_DEBUG) && claim_remaining_size (frag, end)) {
			sgen_clear_range (end, frag->fragment_end);
			HEAVY_STAT (stat_wasted_bytes_trailer += frag->fragment_end - end);
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

				/*Fail if the next node is removed concurrently and its CAS wins */
				if (mono_atomic_cas_ptr ((volatile gpointer*)&frag->next, mask (next, 1), next) != next) {
					continue;
				}
			}

			/* The second CAS must happen after the first CAS or frag->next. */
			mono_memory_write_barrier ();

			/* Fail if the previous node was deleted and its CAS wins */
			if (mono_atomic_cas_ptr ((volatile gpointer*)prev_ptr, unmask (next), frag) != frag) {
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
	mono_atomic_inc_i32 (&alloc_count);
#endif

restart:
	for (frag = (SgenFragment *)unmask (allocator->alloc_head); unmask (frag); frag = (SgenFragment *)unmask (frag->next)) {
		size_t frag_size = frag->fragment_end - frag->fragment_next;

		if (frag->fragment_next >= (sgen_nursery_start + sgen_nursery_size))
			continue;

		HEAVY_STAT (++stat_alloc_iterations);

		if (size <= frag_size) {
			void *p = par_alloc_from_fragment (allocator, frag, size);
			if (!p) {
				HEAVY_STAT (++stat_alloc_retries);
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
sgen_fragment_allocator_serial_range_alloc (SgenFragmentAllocator *allocator, size_t desired_size, size_t minimum_size, size_t *out_alloc_size)
{
	SgenFragment *frag, **previous, *min_frag = NULL, **prev_min_frag = NULL;
	size_t current_minimum = minimum_size;

#ifdef NALLOC_DEBUG
	mono_atomic_inc_i32 (&alloc_count);
#endif

	previous = &allocator->alloc_head;

	for (frag = *previous; frag; frag = *previous) {
		size_t frag_size = frag->fragment_end - frag->fragment_next;

		HEAVY_STAT (++stat_alloc_range_iterations);

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
	mono_atomic_inc_i32 (&alloc_count);
#endif

	for (frag = (SgenFragment *)unmask (allocator->alloc_head); frag; frag = (SgenFragment *)unmask (frag->next)) {
		size_t frag_size = frag->fragment_end - frag->fragment_next;

		if (frag->fragment_next >= (sgen_nursery_start + sgen_nursery_size))
			continue;

		HEAVY_STAT (++stat_alloc_range_iterations);

		if (desired_size <= frag_size) {
			void *p;
			*out_alloc_size = desired_size;

			p = par_alloc_from_fragment (allocator, frag, desired_size);
			if (!p) {
				HEAVY_STAT (++stat_alloc_range_retries);
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
		size_t frag_size = min_frag->fragment_end - min_frag->fragment_next;

		if (frag_size < minimum_size)
			goto restart;

		*out_alloc_size = frag_size;

		mono_memory_barrier ();
		p = par_alloc_from_fragment (allocator, min_frag, frag_size);

		/*XXX restarting here is quite dubious given this is already second chance allocation. */
		if (!p) {
			HEAVY_STAT (++stat_alloc_retries);
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

	for (frag = (SgenFragment *)unmask (allocator->alloc_head); frag; frag = (SgenFragment *)unmask (frag->next)) {
		SGEN_LOG (4, "Clear nursery frag %p-%p", frag->fragment_next, frag->fragment_end);
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
	if (sgen_get_nursery_clear_policy () == CLEAR_AT_TLAB_CREATION || sgen_get_nursery_clear_policy () == CLEAR_AT_TLAB_CREATION_DEBUG) {
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
	size_t size = end - start;

	if ((start && !end) || (start > end))
		g_error ("Invalid range [%p %p]", start, end);

	if (sgen_client_array_fill_range (start, size)) {
		sgen_set_nursery_scan_start (start);
		SGEN_ASSERT (0, start + sgen_safe_object_get_size ((GCObject*)start) == end, "Array fill produced wrong size");
	}
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
	SGEN_LOG (4, "Found empty fragment: %p-%p, size: %zd", frag_start, frag_end, frag_size);
	binary_protocol_empty (frag_start, frag_size);
	/* Not worth dealing with smaller fragments: need to tune */
	if (frag_size >= SGEN_MAX_NURSERY_WASTE) {
		/* memsetting just the first chunk start is bound to provide better cache locality */
		if (sgen_get_nursery_clear_policy () == CLEAR_AT_GC)
			memset (frag_start, 0, frag_size);
		else if (sgen_get_nursery_clear_policy () == CLEAR_AT_TLAB_CREATION_DEBUG)
			memset (frag_start, 0xff, frag_size);

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
		HEAVY_STAT (stat_wasted_bytes_small_areas += frag_size);
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

/*
 * We split fragments at the border of the current nursery limit. When we
 * allocate from the nursery we only consider fragments that start in the
 * current nursery section. We build fragments for the entire nursery in
 * order to facilitate scanning it for objects (adding a nursery frag also
 * marks a region in the nursery as being free)
 */
static void
add_nursery_frag_checks (SgenFragmentAllocator *allocator, char *frag_start, char *frag_end)
{
	char *nursery_limit = sgen_nursery_start + sgen_nursery_size;

	if (frag_start < nursery_limit && frag_end > nursery_limit) {
		add_nursery_frag (allocator, nursery_limit - frag_start, frag_start, nursery_limit);
		add_nursery_frag (allocator, frag_end - nursery_limit, nursery_limit, frag_end);
	} else {
		add_nursery_frag (allocator, frag_end - frag_start, frag_start, frag_end);
	}
}

mword
sgen_build_nursery_fragments (GCMemSection *nursery_section, SgenGrayQueue *unpin_queue)
{
	char *frag_start, *frag_end;
	size_t frag_size;
	SgenFragment *frags_ranges;
	void **pin_start, **pin_entry, **pin_end;

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

	pin_start = pin_entry = sgen_pinning_get_entry (nursery_section->pin_queue_first_entry);
	pin_end = sgen_pinning_get_entry (nursery_section->pin_queue_last_entry);

	while (pin_entry < pin_end || frags_ranges) {
		char *addr0, *addr1;
		size_t size;

		addr0 = addr1 = sgen_nursery_end;
		if (pin_entry < pin_end)
			addr0 = (char *)*pin_entry;
		if (frags_ranges)
			addr1 = frags_ranges->fragment_start;

		if (addr0 < addr1) {
			if (unpin_queue)
				GRAY_OBJECT_ENQUEUE_SERIAL (unpin_queue, (GCObject*)addr0, sgen_obj_get_descriptor_safe ((GCObject*)addr0));
			else
				SGEN_UNPIN_OBJECT (addr0);
			size = SGEN_ALIGN_UP (sgen_safe_object_get_size ((GCObject*)addr0));
			CANARIFY_SIZE (size);
			sgen_set_nursery_scan_start (addr0);
			frag_end = addr0;
			++pin_entry;
		} else {
			frag_end = addr1;
			size = frags_ranges->fragment_next - addr1;
			frags_ranges = frags_ranges->next_in_order;
		}

		frag_size = frag_end - frag_start;

		if (size == 0)
			continue;

		g_assert (frag_size >= 0);
		g_assert (size > 0);
		if (frag_size && size)
			add_nursery_frag_checks (&mutator_allocator, frag_start, frag_end);

		frag_size = size;
#ifdef NALLOC_DEBUG
		add_alloc_record (*pin_entry, frag_size, PINNING);
#endif
		frag_start = frag_end + frag_size;
	}

	frag_end = sgen_nursery_end;
	frag_size = frag_end - frag_start;
	if (frag_size)
		add_nursery_frag_checks (&mutator_allocator, frag_start, frag_end);

	/* Now it's safe to release the fragments exclude list. */
	sgen_minor_collector.build_fragments_release_exclude_head ();

	/* First we reorder the fragment list to be in ascending address order. This makes H/W prefetchers happier. */
	fragment_list_reverse (&mutator_allocator);

	/*The collector might want to do something with the final nursery fragment list.*/
	sgen_minor_collector.build_fragments_finish (&mutator_allocator);

	if (!unmask (mutator_allocator.alloc_head)) {
		SGEN_LOG (1, "Nursery fully pinned");
		for (pin_entry = pin_start; pin_entry < pin_end; ++pin_entry) {
			GCObject *p = (GCObject *)*pin_entry;
			SGEN_LOG (3, "Bastard pinning obj %p (%s), size: %zd", p, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (p)), sgen_safe_object_get_size (p));
		}
	}
	return fragment_total;
}

/*** Nursery memory allocation ***/
void
sgen_nursery_retire_region (void *address, ptrdiff_t size)
{
	HEAVY_STAT (stat_wasted_bytes_discarded_fragments += size);
}

gboolean
sgen_can_alloc_size (size_t size)
{
	SgenFragment *frag;

	if (!SGEN_CAN_ALIGN_UP (size))
		return FALSE;

	size = SGEN_ALIGN_UP (size);

	for (frag = (SgenFragment *)unmask (mutator_allocator.alloc_head); frag; frag = (SgenFragment *)unmask (frag->next)) {
		if ((size_t)(frag->fragment_end - frag->fragment_next) >= size)
			return TRUE;
	}
	return FALSE;
}

void*
sgen_nursery_alloc (size_t size)
{
	SGEN_ASSERT (1, size >= (SGEN_CLIENT_MINIMUM_OBJECT_SIZE + CANARY_SIZE) && size <= (SGEN_MAX_SMALL_OBJ_SIZE + CANARY_SIZE), "Invalid nursery object size");

	SGEN_LOG (4, "Searching nursery for size: %zd", size);
	size = SGEN_ALIGN_UP (size);

	HEAVY_STAT (++stat_nursery_alloc_requests);

	return sgen_fragment_allocator_par_alloc (&mutator_allocator, size);
}

void*
sgen_nursery_alloc_range (size_t desired_size, size_t minimum_size, size_t *out_alloc_size)
{
	SGEN_LOG (4, "Searching for byte range desired size: %zd minimum size %zd", desired_size, minimum_size);

	HEAVY_STAT (++stat_nursery_alloc_range_requests);

	return sgen_fragment_allocator_par_range_alloc (&mutator_allocator, desired_size, minimum_size, out_alloc_size);
}

/*** Initialization ***/

#ifdef HEAVY_STATISTICS

void
sgen_nursery_allocator_init_heavy_stats (void)
{
	mono_counters_register ("bytes wasted trailer fragments", MONO_COUNTER_GC | MONO_COUNTER_WORD | MONO_COUNTER_BYTES, &stat_wasted_bytes_trailer);
	mono_counters_register ("bytes wasted small areas", MONO_COUNTER_GC | MONO_COUNTER_WORD | MONO_COUNTER_BYTES, &stat_wasted_bytes_small_areas);
	mono_counters_register ("bytes wasted discarded fragments", MONO_COUNTER_GC | MONO_COUNTER_WORD | MONO_COUNTER_BYTES, &stat_wasted_bytes_discarded_fragments);

	mono_counters_register ("# nursery alloc requests", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_nursery_alloc_requests);
	mono_counters_register ("# nursery alloc iterations", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_alloc_iterations);
	mono_counters_register ("# nursery alloc retries", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_alloc_retries);

	mono_counters_register ("# nursery alloc range requests", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_nursery_alloc_range_requests);
	mono_counters_register ("# nursery alloc range iterations", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_alloc_range_iterations);
	mono_counters_register ("# nursery alloc range restries", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_alloc_range_retries);
}

#endif

void
sgen_init_nursery_allocator (void)
{
	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_FRAGMENT, sizeof (SgenFragment));
#ifdef NALLOC_DEBUG
	alloc_records = sgen_alloc_os_memory (sizeof (AllocRecord) * ALLOC_RECORD_COUNT, SGEN_ALLOC_INTERNAL | SGEN_ALLOC_ACTIVATE, "debugging memory");
#endif
}

void
sgen_nursery_alloc_prepare_for_minor (void)
{
	sgen_minor_collector.prepare_to_space (sgen_space_bitmap, sgen_space_bitmap_size);
}

void
sgen_nursery_alloc_prepare_for_major (void)
{
	sgen_minor_collector.prepare_to_space (sgen_space_bitmap, sgen_space_bitmap_size);
}

void
sgen_nursery_allocator_set_nursery_bounds (char *start, size_t min_size, size_t max_size)
{
	sgen_nursery_start = start;
	sgen_nursery_end = start + max_size;

	sgen_nursery_size = min_size;
	sgen_nursery_min_size = min_size;
	sgen_nursery_max_size = max_size;

	sgen_nursery_bits = 0;
	while (ONE_P << (++ sgen_nursery_bits) != sgen_nursery_max_size)
		;

	/*
	 * This will not divide evenly for tiny nurseries (<4kb), so we make sure to be on
	 * the right side of things and round up.  We could just do a MIN(1,x) instead,
	 * since the nursery size must be a power of 2.
	 */
	sgen_space_bitmap_size = (sgen_nursery_end - sgen_nursery_start + SGEN_TO_SPACE_GRANULE_IN_BYTES * 8 - 1) / (SGEN_TO_SPACE_GRANULE_IN_BYTES * 8);
	sgen_space_bitmap = (char *)g_malloc0 (sgen_space_bitmap_size);

	/* Setup the single first large fragment */
	sgen_minor_collector.init_nursery (&mutator_allocator, sgen_nursery_start, sgen_nursery_end);
}

void
sgen_resize_nursery (gboolean need_shrink)
{
	size_t major_size;

	if (sgen_nursery_min_size == sgen_nursery_max_size)
		return;

	major_size = major_collector.get_num_major_sections () * major_collector.section_size + los_memory_usage;
	/*
	 * We attempt to use a larger nursery size, as long as it doesn't
	 * exceed a certain percentage of the major heap.
	 *
	 * FIXME
	 * Commit memory when expanding and release it when shrinking (which
	 * would only be possible if there aren't any pinned objects in the
	 * section).
	 */
	if ((sgen_nursery_size * 2) < (major_size / SGEN_DEFAULT_ALLOWANCE_NURSERY_SIZE_RATIO) &&
			(sgen_nursery_size * 2) <= sgen_nursery_max_size && !need_shrink) {
		if ((nursery_section->end_data - nursery_section->data) == sgen_nursery_size)
			nursery_section->end_data += sgen_nursery_size;
		sgen_nursery_size *= 2;
	} else if ((sgen_nursery_size > (major_size / SGEN_DEFAULT_ALLOWANCE_NURSERY_SIZE_RATIO) || need_shrink) &&
			(sgen_nursery_size / 2) >= sgen_nursery_min_size) {
		sgen_nursery_size /= 2;
	}
}


#endif
