/*
 * sgen-memory-governor.c: When to schedule collections based on
 * memory usage.
 *
 * Author:
 * 	Rodrigo Kumpera (rkumpera@novell.com)
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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

#include <stdlib.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-thread-pool.h"
#include "mono/sgen/sgen-client.h"

#define MIN_MINOR_COLLECTION_ALLOWANCE	((mword)(DEFAULT_NURSERY_SIZE * default_allowance_nursery_size_ratio))

/*Heap limits and allocation knobs*/
static mword max_heap_size = ((mword)0)- ((mword)1);
static mword soft_heap_limit = ((mword)0) - ((mword)1);

static double default_allowance_nursery_size_ratio = SGEN_DEFAULT_ALLOWANCE_NURSERY_SIZE_RATIO;
static double save_target_ratio = SGEN_DEFAULT_SAVE_TARGET_RATIO;

/**/
static mword allocated_heap;
static mword total_alloc = 0;
static mword total_alloc_max = 0;

/* GC triggers. */

static gboolean debug_print_allowance = FALSE;


/* use this to tune when to do a major/minor collection */
static mword major_collection_trigger_size;

static mword major_pre_sweep_heap_size;
static mword major_start_heap_size;

static mword last_major_num_sections = 0;
static mword last_los_memory_usage = 0;

static gboolean need_calculate_minor_collection_allowance;

/* The size of the LOS after the last major collection, after sweeping. */
static mword last_collection_los_memory_usage = 0;

static mword sgen_memgov_available_free_space (void);


/* GC trigger heuristics. */

static void
sgen_memgov_calculate_minor_collection_allowance (void)
{
	size_t new_major, new_heap_size, allowance_target, allowance;
	size_t decrease;

	if (!need_calculate_minor_collection_allowance)
		return;

	SGEN_ASSERT (0, major_collector.have_swept (), "Can only calculate allowance if heap is swept");

	new_major = major_collector.get_bytes_survived_last_sweep ();
	new_heap_size = new_major + last_collection_los_memory_usage;

	/*
	 * We allow the heap to grow by one third its current size before we start the next
	 * major collection.
	 */
	allowance_target = new_heap_size * SGEN_DEFAULT_ALLOWANCE_HEAP_SIZE_RATIO;

	allowance = MAX (allowance_target, MIN_MINOR_COLLECTION_ALLOWANCE);

	/*
	 * For the concurrent collector, we decrease the allowance relative to the memory
	 * growth during the M&S phase, survival rate of the collection and the allowance
	 * ratio.
	 */
	decrease = (major_pre_sweep_heap_size - major_start_heap_size) * ((float)new_heap_size / major_pre_sweep_heap_size) * (SGEN_DEFAULT_ALLOWANCE_HEAP_SIZE_RATIO + 1);
	if (decrease > allowance)
		decrease = allowance;
	allowance -= decrease;

	if (new_heap_size + allowance > soft_heap_limit) {
		if (new_heap_size > soft_heap_limit)
			allowance = MIN_MINOR_COLLECTION_ALLOWANCE;
		else
			allowance = MAX (soft_heap_limit - new_heap_size, MIN_MINOR_COLLECTION_ALLOWANCE);
	}

	/* FIXME: Why is this here? */
	if (major_collector.free_swept_blocks)
		major_collector.free_swept_blocks (allowance);

	major_collection_trigger_size = new_heap_size + allowance;

	need_calculate_minor_collection_allowance = FALSE;

	if (debug_print_allowance) {
		SGEN_LOG (0, "Surviving sweep: %ld bytes (%ld major, %ld LOS)", (long)new_heap_size, (long)new_major, (long)last_collection_los_memory_usage);
		SGEN_LOG (0, "Allowance: %ld bytes", (long)allowance);
		SGEN_LOG (0, "Trigger size: %ld bytes", (long)major_collection_trigger_size);
	}
}

static inline size_t
get_heap_size (void)
{
	return major_collector.get_num_major_sections () * major_collector.section_size + los_memory_usage;
}

gboolean
sgen_need_major_collection (mword space_needed)
{
	size_t heap_size;

	if (sgen_concurrent_collection_in_progress ()) {
		heap_size = get_heap_size ();

		if (heap_size <= major_collection_trigger_size)
			return FALSE; 

		/*
		 * The more the heap grows, the more we need to decrease the allowance above,
		 * in order to have similar trigger sizes as the synchronous collector.
		 * If the heap grows so much that we would need to have a negative allowance,
		 * we force the finishing of the collection, to avoid increased memory usage.
		 */
		if ((heap_size - major_start_heap_size) > major_start_heap_size * SGEN_DEFAULT_ALLOWANCE_HEAP_SIZE_RATIO)
			return TRUE;
		return FALSE;
	}

	/* FIXME: This is a cop-out.  We should have some way of figuring this out. */
	if (!major_collector.have_swept ())
		return FALSE;

	if (space_needed > sgen_memgov_available_free_space ())
		return TRUE;

	sgen_memgov_calculate_minor_collection_allowance ();

	heap_size = get_heap_size ();

	return heap_size > major_collection_trigger_size;
}

void
sgen_memgov_minor_collection_start (void)
{
}

void
sgen_memgov_minor_collection_end (void)
{
}

void
sgen_memgov_major_pre_sweep (void)
{
	if (sgen_concurrent_collection_in_progress ()) {
		major_pre_sweep_heap_size = get_heap_size ();
	} else {
		/* We decrease the allowance only in the concurrent case */
		major_pre_sweep_heap_size = major_start_heap_size;
	}
}

void
sgen_memgov_major_collection_start (void)
{
	need_calculate_minor_collection_allowance = TRUE;
	major_start_heap_size = get_heap_size ();

	if (debug_print_allowance) {
		SGEN_LOG (0, "Starting collection with heap size %ld bytes", (long)major_start_heap_size);
	}
}

void
sgen_memgov_major_collection_end (gboolean forced)
{
	last_collection_los_memory_usage = los_memory_usage;

	if (forced) {
		sgen_get_major_collector ()->finish_sweeping ();
		sgen_memgov_calculate_minor_collection_allowance ();
	}
}

void
sgen_memgov_collection_start (int generation)
{
}

void
sgen_memgov_collection_end (int generation, GGTimingInfo* info, int info_count)
{
	int i;
	for (i = 0; i < info_count; ++i) {
		if (info[i].generation != -1)
			sgen_client_log_timing (&info [i], last_major_num_sections, last_los_memory_usage);
	}
	last_major_num_sections = major_collector.get_num_major_sections ();
}

/*
Global GC memory tracking.
This tracks the total usage of memory by the GC. This includes
managed and unmanaged memory.
*/

static unsigned long
prot_flags_for_activate (int activate)
{
	unsigned long prot_flags = activate? MONO_MMAP_READ|MONO_MMAP_WRITE: MONO_MMAP_NONE;
	return prot_flags | MONO_MMAP_PRIVATE | MONO_MMAP_ANON;
}

void
sgen_assert_memory_alloc (void *ptr, size_t requested_size, const char *assert_description)
{
	if (ptr || !assert_description)
		return;
	fprintf (stderr, "Error: Garbage collector could not allocate %zu bytes of memory for %s.\n", requested_size, assert_description);
	exit (1);
}

/*
 * Allocate a big chunk of memory from the OS (usually 64KB to several megabytes).
 * This must not require any lock.
 */
void*
sgen_alloc_os_memory (size_t size, SgenAllocFlags flags, const char *assert_description)
{
	void *ptr;

	g_assert (!(flags & ~(SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE)));

	ptr = mono_valloc (0, size, prot_flags_for_activate (flags & SGEN_ALLOC_ACTIVATE));
	sgen_assert_memory_alloc (ptr, size, assert_description);
	if (ptr) {
		SGEN_ATOMIC_ADD_P (total_alloc, size);
		total_alloc_max = MAX (total_alloc_max, total_alloc);
	}
	return ptr;
}

/* size must be a power of 2 */
void*
sgen_alloc_os_memory_aligned (size_t size, mword alignment, SgenAllocFlags flags, const char *assert_description)
{
	void *ptr;

	g_assert (!(flags & ~(SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE)));

	ptr = mono_valloc_aligned (size, alignment, prot_flags_for_activate (flags & SGEN_ALLOC_ACTIVATE));
	sgen_assert_memory_alloc (ptr, size, assert_description);
	if (ptr) {
		SGEN_ATOMIC_ADD_P (total_alloc, size);
		total_alloc_max = MAX (total_alloc_max, total_alloc);
	}
	return ptr;
}

/*
 * Free the memory returned by sgen_alloc_os_memory (), returning it to the OS.
 */
void
sgen_free_os_memory (void *addr, size_t size, SgenAllocFlags flags)
{
	g_assert (!(flags & ~SGEN_ALLOC_HEAP));

	mono_vfree (addr, size);
	SGEN_ATOMIC_ADD_P (total_alloc, -(gssize)size);
	total_alloc_max = MAX (total_alloc_max, total_alloc);
}

size_t
sgen_gc_get_total_heap_allocation (void)
{
	return total_alloc;
}


/*
Heap Sizing limits.
This limit the max size of the heap. It takes into account
only memory actively in use to hold heap objects and not
for other parts of the GC.
 */
static mword
sgen_memgov_available_free_space (void)
{
	return max_heap_size - MIN (allocated_heap, max_heap_size);
}

void
sgen_memgov_release_space (mword size, int space)
{
	SGEN_ATOMIC_ADD_P (allocated_heap, -(gssize)size);
}

gboolean
sgen_memgov_try_alloc_space (mword size, int space)
{
	if (sgen_memgov_available_free_space () < size) {
		SGEN_ASSERT (4, !sgen_thread_pool_is_thread_pool_thread (mono_native_thread_id_get ()), "Memory shouldn't run out in worker thread");
		return FALSE;
	}

	SGEN_ATOMIC_ADD_P (allocated_heap, size);
	sgen_client_total_allocated_heap_changed (allocated_heap);
	return TRUE;
}

void
sgen_memgov_init (size_t max_heap, size_t soft_limit, gboolean debug_allowance, double allowance_ratio, double save_target)
{
	if (soft_limit)
		soft_heap_limit = soft_limit;

	debug_print_allowance = debug_allowance;
	major_collection_trigger_size = MIN_MINOR_COLLECTION_ALLOWANCE;

	mono_counters_register ("Memgov alloc", MONO_COUNTER_GC | MONO_COUNTER_WORD | MONO_COUNTER_BYTES | MONO_COUNTER_VARIABLE, &total_alloc);
	mono_counters_register ("Memgov max alloc", MONO_COUNTER_GC | MONO_COUNTER_WORD | MONO_COUNTER_BYTES | MONO_COUNTER_MONOTONIC, &total_alloc_max);

	if (max_heap == 0)
		return;

	if (max_heap < soft_limit) {
		sgen_env_var_error (MONO_GC_PARAMS_NAME, "Setting to minimum.", "`max-heap-size` must be at least as large as `soft-heap-limit`.");
		max_heap = soft_limit;
	}

	if (max_heap < sgen_nursery_size * 4) {
		sgen_env_var_error (MONO_GC_PARAMS_NAME, "Setting to minimum.", "`max-heap-size` must be at least 4 times as large as `nursery size`.");
		max_heap = sgen_nursery_size * 4;
	}
	max_heap_size = max_heap - sgen_nursery_size;

	if (allowance_ratio)
		default_allowance_nursery_size_ratio = allowance_ratio;

	if (save_target)
		save_target_ratio = save_target;
}

#endif
