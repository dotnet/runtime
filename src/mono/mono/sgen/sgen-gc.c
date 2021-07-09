/**
 * \file
 * Simple generational GC.
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *  Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2005-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 *
 * Thread start/stop adapted from Boehm's GC:
 * Copyright (c) 1994 by Xerox Corporation.  All rights reserved.
 * Copyright (c) 1996 by Silicon Graphics.  All rights reserved.
 * Copyright (c) 1998 by Fergus Henderson.  All rights reserved.
 * Copyright (c) 2000-2004 by Hewlett-Packard Company.  All rights reserved.
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 *
 * Important: allocation provides always zeroed memory, having to do
 * a memset after allocation is deadly for performance.
 * Memory usage at startup is currently as follows:
 * 64 KB pinned space
 * 64 KB internal space
 * size of nursery
 * We should provide a small memory config with half the sizes
 *
 * We currently try to make as few mono assumptions as possible:
 * 1) 2-word header with no GC pointers in it (first vtable, second to store the
 *    forwarding ptr)
 * 2) gc descriptor is the second word in the vtable (first word in the class)
 * 3) 8 byte alignment is the minimum and enough (not true for special structures (SIMD), FIXME)
 * 4) there is a function to get an object's size and the number of
 *    elements in an array.
 * 5) we know the special way bounds are allocated for complex arrays
 * 6) we know about proxies and how to treat them when domains are unloaded
 *
 * Always try to keep stack usage to a minimum: no recursive behaviour
 * and no large stack allocs.
 *
 * General description.
 * Objects are initially allocated in a nursery using a fast bump-pointer technique.
 * When the nursery is full we start a nursery collection: this is performed with a
 * copying GC.
 * When the old generation is full we start a copying GC of the old generation as well:
 * this will be changed to mark&sweep with copying when fragmentation becomes to severe
 * in the future.  Maybe we'll even do both during the same collection like IMMIX.
 *
 * The things that complicate this description are:
 * *) pinned objects: we can't move them so we need to keep track of them
 * *) no precise info of the thread stacks and registers: we need to be able to
 *    quickly find the objects that may be referenced conservatively and pin them
 *    (this makes the first issues more important)
 * *) large objects are too expensive to be dealt with using copying GC: we handle them
 *    with mark/sweep during major collections
 * *) some objects need to not move even if they are small (interned strings, Type handles):
 *    we use mark/sweep for them, too: they are not allocated in the nursery, but inside
 *    PinnedChunks regions
 */

/*
 * TODO:

 *) we could have a function pointer in MonoClass to implement
  customized write barriers for value types

 *) investigate the stuff needed to advance a thread to a GC-safe
  point (single-stepping, read from unmapped memory etc) and implement it.
  This would enable us to inline allocations and write barriers, for example,
  or at least parts of them, like the write barrier checks.
  We may need this also for handling precise info on stacks, even simple things
  as having uninitialized data on the stack and having to wait for the prolog
  to zero it. Not an issue for the last frame that we scan conservatively.
  We could always not trust the value in the slots anyway.

 *) modify the jit to save info about references in stack locations:
  this can be done just for locals as a start, so that at least
  part of the stack is handled precisely.

 *) test/fix endianess issues

 *) Implement a card table as the write barrier instead of remembered
    sets?  Card tables are not easy to implement with our current
    memory layout.  We have several different kinds of major heap
    objects: Small objects in regular blocks, small objects in pinned
    chunks and LOS objects.  If we just have a pointer we have no way
    to tell which kind of object it points into, therefore we cannot
    know where its card table is.  The least we have to do to make
    this happen is to get rid of write barriers for indirect stores.
    (See next item)

 *) Get rid of write barriers for indirect stores.  We can do this by
    telling the GC to wbarrier-register an object once we do an ldloca
    or ldelema on it, and to unregister it once it's not used anymore
    (it can only travel downwards on the stack).  The problem with
    unregistering is that it needs to happen eventually no matter
    what, even if exceptions are thrown, the thread aborts, etc.
    Rodrigo suggested that we could do only the registering part and
    let the collector find out (pessimistically) when it's safe to
    unregister, namely when the stack pointer of the thread that
    registered the object is higher than it was when the registering
    happened.  This might make for a good first implementation to get
    some data on performance.

 *) Some sort of blocklist support?  Blocklists is a concept from the
    Boehm GC: if during a conservative scan we find pointers to an
    area which we might use as heap, we mark that area as unusable, so
    pointer retention by random pinning pointers is reduced.

 *) experiment with max small object size (very small right now - 2kb,
    because it's tied to the max freelist size)

  *) add an option to mmap the whole heap in one chunk: it makes for many
     simplifications in the checks (put the nursery at the top and just use a single
     check for inclusion/exclusion): the issue this has is that on 32 bit systems it's
     not flexible (too much of the address space may be used by default or we can't
     increase the heap as needed) and we'd need a race-free mechanism to return memory
     back to the system (mprotect(PROT_NONE) will still keep the memory allocated if it
     was written to, munmap is needed, but the following mmap may not find the same segment
     free...)

 *) memzero the major fragments after restarting the world and optionally a smaller
    chunk at a time

 *) investigate having fragment zeroing threads

 *) separate locks for finalization and other minor stuff to reduce
    lock contention

 *) try a different copying order to improve memory locality

 *) a thread abort after a store but before the write barrier will
    prevent the write barrier from executing

 *) specialized dynamically generated markers/copiers

 *) Dynamically adjust TLAB size to the number of threads.  If we have
    too many threads that do allocation, we might need smaller TLABs,
    and we might get better performance with larger TLABs if we only
    have a handful of threads.  We could sum up the space left in all
    assigned TLABs and if that's more than some percentage of the
    nursery size, reduce the TLAB size.

 *) Explore placing unreachable objects on unused nursery memory.
	Instead of memset'ng a region to zero, place an int[] covering it.
	A good place to start is add_nursery_frag. The tricky thing here is
	placing those objects atomically outside of a collection.

 *) Allocation should use asymmetric Dekker synchronization:
 	http://blogs.oracle.com/dave/resource/Asymmetric-Dekker-Synchronization.txt
	This should help weak consistency archs.
 */
#include "config.h"
#ifdef HAVE_SGEN_GC

#ifdef __MACH__
#undef _XOPEN_SOURCE
#define _XOPEN_SOURCE
#define _DARWIN_C_SOURCE
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif
#ifdef HAVE_PTHREAD_NP_H
#include <pthread_np.h>
#endif
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <assert.h>
#include <stdlib.h>
#include <glib.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-cardtable.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-hash-table.h"
#include "mono/sgen/sgen-pinning.h"
#include "mono/sgen/sgen-workers.h"
#include "mono/sgen/sgen-client.h"
#include "mono/sgen/sgen-pointer-queue.h"
#include "mono/sgen/gc-internal-agnostic.h"
#include "mono/utils/mono-proclib.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/hazard-pointer.h"

#include <mono/utils/memcheck.h>
#include <mono/utils/mono-mmap-internals.h>
#include <mono/utils/unlocked.h>

#undef pthread_create
#undef pthread_join
#undef pthread_detach

/*
 * ######################################################################
 * ########  Types and constants used by the GC.
 * ######################################################################
 */

/* 0 means not initialized, 1 is initialized, -1 means in progress */
static int gc_initialized = 0;
/* If set, check if we need to do something every X allocations */
gboolean sgen_has_per_allocation_action;
/* If set, do a heap check every X allocation */
guint32 sgen_verify_before_allocs = 0;
/* If set, do a minor collection before every X allocation */
guint32 sgen_collect_before_allocs = 0;
/* If set, do a whole heap check before each collection */
static gboolean whole_heap_check_before_collection = FALSE;
/* If set, do a remset consistency check at various opportunities */
static gboolean remset_consistency_checks = FALSE;
/* If set, do parallel copy/clear of remset */
static gboolean remset_copy_clear_par = FALSE;
/* If set, do a mod union consistency check before each finishing collection pause */
static gboolean mod_union_consistency_check = FALSE;
/* If set, check whether mark bits are consistent after major collections */
static gboolean check_mark_bits_after_major_collection = FALSE;
/* If set, check that all vtables of nursery objects are untagged */
static gboolean check_nursery_objects_untag = FALSE;
/* If set, do a few checks when the concurrent collector is used */
static gboolean do_concurrent_checks = FALSE;
/* If set, do a plausibility check on the scan_starts before and after
   each collection */
static gboolean do_scan_starts_check = FALSE;

static gboolean disable_minor_collections = FALSE;
static gboolean disable_major_collections = FALSE;
static gboolean do_verify_nursery = FALSE;
static gboolean do_dump_nursery_content = FALSE;

#ifndef DISABLE_SGEN_DEBUG_HELPERS
static gboolean enable_nursery_canaries = FALSE;
#else
static const gboolean enable_nursery_canaries = FALSE;
#endif

static gboolean precleaning_enabled = TRUE;
static gboolean dynamic_nursery = FALSE;
static size_t min_nursery_size = 0;
static size_t max_nursery_size = 0;

#ifdef HEAVY_STATISTICS
guint64 stat_objects_alloced_degraded = 0;
guint64 stat_bytes_alloced_degraded = 0;

guint64 stat_copy_object_called_nursery = 0;
guint64 stat_objects_copied_nursery = 0;
guint64 stat_copy_object_called_major = 0;
guint64 stat_objects_copied_major = 0;

guint64 stat_scan_object_called_nursery = 0;
guint64 stat_scan_object_called_major = 0;

guint64 stat_slots_allocated_in_vain;

guint64 stat_nursery_copy_object_failed_from_space = 0;
guint64 stat_nursery_copy_object_failed_forwarded = 0;
guint64 stat_nursery_copy_object_failed_pinned = 0;
guint64 stat_nursery_copy_object_failed_to_space = 0;

static guint64 stat_wbarrier_add_to_global_remset = 0;
static guint64 stat_wbarrier_arrayref_copy = 0;
static guint64 stat_wbarrier_generic_store = 0;
static guint64 stat_wbarrier_generic_store_atomic = 0;
static guint64 stat_wbarrier_set_root = 0;
#endif

static guint64 stat_pinned_objects = 0;

static guint64 time_minor_pre_collection_fragment_clear = 0;
static guint64 time_minor_pinning = 0;
static guint64 time_minor_scan_remsets = 0;
static guint64 time_minor_scan_major_blocks = 0;
static guint64 time_minor_scan_los = 0;
static guint64 time_minor_scan_pinned = 0;
static guint64 time_minor_scan_roots = 0;
static guint64 time_minor_finish_gray_stack = 0;
static guint64 time_minor_fragment_creation = 0;

static guint64 time_major_pre_collection_fragment_clear = 0;
static guint64 time_major_pinning = 0;
static guint64 time_major_scan_pinned = 0;
static guint64 time_major_scan_roots = 0;
static guint64 time_major_scan_mod_union_blocks = 0;
static guint64 time_major_scan_mod_union_los = 0;
static guint64 time_major_finish_gray_stack = 0;
static guint64 time_major_free_bigobjs = 0;
static guint64 time_major_los_sweep = 0;
static guint64 time_major_sweep = 0;
static guint64 time_major_fragment_creation = 0;

static guint64 time_max = 0;
static guint64 time_last = 0;
static guint64 time_since_last = 0;
static guint64 timestamp_last_start = 0;

static int sgen_max_pause_time = SGEN_DEFAULT_MAX_PAUSE_TIME;
static float sgen_max_pause_margin = SGEN_DEFAULT_MAX_PAUSE_MARGIN;

static SGEN_TV_DECLARE (time_major_conc_collection_start);
static SGEN_TV_DECLARE (time_major_conc_collection_end);

int sgen_gc_debug_level = 0;
FILE* sgen_gc_debug_file;
static char* gc_params_options;
static char* gc_debug_options;

/*
void
mono_gc_flush_info (void)
{
	fflush (sgen_gc_debug_file);
}
*/

#define TV_DECLARE SGEN_TV_DECLARE
#define TV_GETTIME SGEN_TV_GETTIME
#define TV_ELAPSED SGEN_TV_ELAPSED

static SGEN_TV_DECLARE (sgen_init_timestamp);

NurseryClearPolicy sgen_nursery_clear_policy = CLEAR_AT_TLAB_CREATION;

#define object_is_forwarded	SGEN_OBJECT_IS_FORWARDED
#define object_is_pinned	SGEN_OBJECT_IS_PINNED
#define pin_object		SGEN_PIN_OBJECT

#define ptr_in_nursery sgen_ptr_in_nursery

#define LOAD_VTABLE	SGEN_LOAD_VTABLE

gboolean
sgen_nursery_canaries_enabled (void)
{
	return enable_nursery_canaries;
}

#define safe_object_get_size	sgen_safe_object_get_size

typedef enum {
	SGEN_MAJOR_DEFAULT,
	SGEN_MAJOR_SERIAL,
	SGEN_MAJOR_CONCURRENT,
	SGEN_MAJOR_CONCURRENT_PARALLEL
} SgenMajor;

typedef enum {
	SGEN_MINOR_DEFAULT,
	SGEN_MINOR_SIMPLE,
	SGEN_MINOR_SIMPLE_PARALLEL,
	SGEN_MINOR_SPLIT
} SgenMinor;

typedef enum {
	SGEN_MODE_NONE,
	SGEN_MODE_BALANCED,
	SGEN_MODE_THROUGHPUT,
	SGEN_MODE_PAUSE
} SgenMode;

/*
 * ######################################################################
 * ########  Global data.
 * ######################################################################
 */
MonoCoopMutex sgen_gc_mutex;

#define SCAN_START_SIZE	SGEN_SCAN_START_SIZE

size_t sgen_degraded_mode = 0;

static mword bytes_pinned_from_failed_allocation = 0;

GCMemSection *sgen_nursery_section = NULL;
static volatile mword lowest_heap_address = ~(mword)0;
static volatile mword highest_heap_address = 0;

MonoCoopMutex sgen_interruption_mutex;

int sgen_current_collection_generation = -1;
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
volatile gboolean sgen_concurrent_collection_in_progress = FALSE;
#endif

/* objects that are ready to be finalized */
static SgenPointerQueue fin_ready_queue = SGEN_POINTER_QUEUE_INIT (INTERNAL_MEM_FINALIZE_READY);
static SgenPointerQueue critical_fin_queue = SGEN_POINTER_QUEUE_INIT (INTERNAL_MEM_FINALIZE_READY);

/* registered roots: the key to the hash is the root start address */
/* 
 * Different kinds of roots are kept separate to speed up pin_from_roots () for example.
 */
SgenHashTable sgen_roots_hash [ROOT_TYPE_NUM] = {
	SGEN_HASH_TABLE_INIT (INTERNAL_MEM_ROOTS_TABLE, INTERNAL_MEM_ROOT_RECORD, sizeof (RootRecord), sgen_aligned_addr_hash, NULL),
	SGEN_HASH_TABLE_INIT (INTERNAL_MEM_ROOTS_TABLE, INTERNAL_MEM_ROOT_RECORD, sizeof (RootRecord), sgen_aligned_addr_hash, NULL),
	SGEN_HASH_TABLE_INIT (INTERNAL_MEM_ROOTS_TABLE, INTERNAL_MEM_ROOT_RECORD, sizeof (RootRecord), sgen_aligned_addr_hash, NULL)
};
static mword roots_size = 0; /* amount of memory in the root set */

/* The size of a TLAB */
/* The bigger the value, the less often we have to go to the slow path to allocate a new 
 * one, but the more space is wasted by threads not allocating much memory.
 * FIXME: Tune this.
 * FIXME: Make this self-tuning for each thread.
 */
guint32 sgen_tlab_size = (1024 * 4);

#define MAX_SMALL_OBJ_SIZE	SGEN_MAX_SMALL_OBJ_SIZE

#define ALLOC_ALIGN		SGEN_ALLOC_ALIGN

#define ALIGN_UP		SGEN_ALIGN_UP

#ifdef SGEN_DEBUG_INTERNAL_ALLOC
MonoNativeThreadId main_gc_thread = NULL;
#endif

/*Object was pinned during the current collection*/
static mword objects_pinned;

/*
 * ######################################################################
 * ########  Macros and function declarations.
 * ######################################################################
 */

/* forward declarations */
static void scan_from_registered_roots (char *addr_start, char *addr_end, int root_type, ScanCopyContext ctx);

static void pin_from_roots (void *start_nursery, void *end_nursery, ScanCopyContext ctx);
static void finish_gray_stack (int generation, ScanCopyContext ctx);

static void job_wbroots_iterate_live_block_ranges (void *worker_data_untyped, SgenThreadPoolJob *job);

SgenMajorCollector sgen_major_collector;
SgenMinorCollector sgen_minor_collector;

static SgenRememberedSet remset;

#ifdef MONO_ATOMIC_USES_LOCK
#include <pthread.h>
static pthread_mutex_t sgen_atomic_spin_lock G_GNUC_UNUSED = PTHREAD_MUTEX_INITIALIZER;

static gint64
mono_sgen_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	gint64 old;
	int ret;

	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock, (void *)&sgen_atomic_spin_lock);
	ret = pthread_mutex_lock(&sgen_atomic_spin_lock);
	g_assert (ret == 0);

	old= *dest;
	if(old==comp) {
		*dest=exch;
	}

	ret = pthread_mutex_unlock(&sgen_atomic_spin_lock);
	g_assert (ret == 0);

	pthread_cleanup_pop (0);

	return(old);
}
#endif

/*
 * The gray queue a worker job must use.  If we're not parallel or
 * concurrent, we use the main gray queue.
 */
static SgenGrayQueue*
sgen_workers_get_job_gray_queue (WorkerData *worker_data, SgenGrayQueue *default_gray_queue)
{
	if (worker_data)
		return &worker_data->private_gray_queue;
	SGEN_ASSERT (0, default_gray_queue, "Why don't we have a default gray queue when we're not running in a worker thread?");
	return default_gray_queue;
}

static void
gray_queue_redirect (SgenGrayQueue *queue)
{
	sgen_workers_take_from_queue (sgen_current_collection_generation, queue);
}

void
sgen_scan_area_with_callback (char *start, char *end, IterateObjectCallbackFunc callback, void *data, gboolean allow_flags, gboolean fail_on_canaries)
{
	while (start < end) {
		size_t size;
		char *obj;

		if (!*(void**)start) {
			start += sizeof (void*); /* should be ALLOC_ALIGN, really */
			continue;
		}

		if (allow_flags) {
			if (!(obj = (char *)SGEN_OBJECT_IS_FORWARDED (start)))
				obj = start;
		} else {
			obj = start;
		}

		if (!sgen_client_object_is_array_fill ((GCObject*)obj)) {
			CHECK_CANARY_FOR_OBJECT ((GCObject*)obj, fail_on_canaries);
			size = ALIGN_UP (safe_object_get_size ((GCObject*)obj));
			callback ((GCObject*)obj, size, data);
			CANARIFY_SIZE (size);
		} else {
			size = ALIGN_UP (safe_object_get_size ((GCObject*)obj));
		}

		start += size;
	}
}

/*
 * sgen_add_to_global_remset:
 *
 *   The global remset contains locations which point into newspace after
 * a minor collection. This can happen if the objects they point to are pinned.
 *
 * LOCKING: If called from a parallel collector, the global remset
 * lock must be held.  For serial collectors that is not necessary.
 */
void
sgen_add_to_global_remset (gpointer ptr, GCObject *obj)
{
	SGEN_ASSERT (5, sgen_ptr_in_nursery (obj), "Target pointer of global remset must be in the nursery");

	HEAVY_STAT (++stat_wbarrier_add_to_global_remset);

	if (!sgen_major_collector.is_concurrent) {
		SGEN_ASSERT (5, sgen_current_collection_generation != -1, "Global remsets can only be added during collections");
	} else {
		if (sgen_current_collection_generation == -1)
			SGEN_ASSERT (5, sgen_get_concurrent_collection_in_progress (), "Global remsets outside of collection pauses can only be added by the concurrent collector");
	}

	if (!object_is_pinned (obj))
		SGEN_ASSERT (5, sgen_minor_collector.is_split || sgen_get_concurrent_collection_in_progress (), "Non-pinned objects can only remain in nursery if it is a split nursery");
	else if (sgen_cement_lookup_or_register (obj))
		return;

	remset.record_pointer (ptr);

	sgen_pin_stats_register_global_remset (obj);

	SGEN_LOG (8, "Adding global remset for %p", ptr);
	sgen_binary_protocol_global_remset (ptr, obj, (gpointer)SGEN_LOAD_VTABLE (obj));
}

/*
 * sgen_drain_gray_stack:
 *
 *   Scan objects in the gray stack until the stack is empty. This should be called
 * frequently after each object is copied, to achieve better locality and cache
 * usage.
 *
 */
gboolean
sgen_drain_gray_stack (ScanCopyContext ctx)
{
	SGEN_ASSERT (0, ctx.ops->drain_gray_stack, "Why do we have a scan/copy context with a missing drain gray stack function?");

	return ctx.ops->drain_gray_stack (ctx.queue);
}

/*
 * Addresses in the pin queue are already sorted. This function finds
 * the object header for each address and pins the object. The
 * addresses must be inside the nursery section.  The (start of the)
 * address array is overwritten with the addresses of the actually
 * pinned objects.  Return the number of pinned objects.
 */
static int
pin_objects_from_nursery_pin_queue (gboolean do_scan_objects, ScanCopyContext ctx)
{
	GCMemSection *section = sgen_nursery_section;
	void **start =  sgen_pinning_get_entry (section->pin_queue_first_entry);
	void **end = sgen_pinning_get_entry (section->pin_queue_last_entry);
	void *start_nursery = section->data;
	void *end_nursery = section->end_data;
	void *last = NULL;
	int count = 0;
	void *search_start;
	void *addr;
	void *pinning_front = start_nursery;
	size_t idx;
	void **definitely_pinned = start;
	ScanObjectFunc scan_func = ctx.ops->scan_object;
	SgenGrayQueue *queue = ctx.queue;

	sgen_nursery_allocator_prepare_for_pinning ();

	while (start < end) {
		GCObject *obj_to_pin = NULL;
		size_t obj_to_pin_size = 0;
		SgenDescriptor desc;

		addr = *start;

		SGEN_ASSERT (0, addr >= start_nursery && addr < end_nursery, "Potential pinning address out of range");
		SGEN_ASSERT (0, addr >= last, "Pin queue not sorted");

		if (addr == last) {
			++start;
			continue;
		}

		SGEN_LOG (5, "Considering pinning addr %p", addr);
		/* We've already processed everything up to pinning_front. */
		if (addr < pinning_front) {
			start++;
			continue;
		}

		/*
		 * Find the closest scan start <= addr.  We might search backward in the
		 * scan_starts array because entries might be NULL.  In the worst case we
		 * start at start_nursery.
		 */
		idx = ((char*)addr - (char*)section->data) / SCAN_START_SIZE;
		SGEN_ASSERT (0, idx < section->num_scan_start, "Scan start index out of range");
		search_start = (void*)section->scan_starts [idx];
		if (!search_start || search_start > addr) {
			while (idx) {
				--idx;
				search_start = section->scan_starts [idx];
				if (search_start && search_start <= addr)
					break;
			}
			if (!search_start || search_start > addr)
				search_start = start_nursery;
		}

		/*
		 * If the pinning front is closer than the scan start we found, start
		 * searching at the front.
		 */
		if (search_start < pinning_front)
			search_start = pinning_front;

		/*
		 * Now addr should be in an object a short distance from search_start.
		 *
		 * search_start must point to zeroed mem or point to an object.
		 */
		do {
			size_t obj_size, canarified_obj_size;

			/* Skip zeros. */
			if (!*(void**)search_start) {
				search_start = (void*)ALIGN_UP ((mword)search_start + sizeof (gpointer));
				/* The loop condition makes sure we don't overrun addr. */
				continue;
			}

			canarified_obj_size = obj_size = ALIGN_UP (safe_object_get_size ((GCObject*)search_start));

			/*
			 * Filler arrays are marked by an invalid sync word.  We don't
			 * consider them for pinning.  They are not delimited by canaries,
			 * either.
			 */
			if (!sgen_client_object_is_array_fill ((GCObject*)search_start)) {
				CHECK_CANARY_FOR_OBJECT (search_start, TRUE);
				CANARIFY_SIZE (canarified_obj_size);

				if (addr >= search_start && (char*)addr < (char*)search_start + obj_size) {
					/* This is the object we're looking for. */
					obj_to_pin = (GCObject*)search_start;
					obj_to_pin_size = canarified_obj_size;
					break;
				}
			}

			/* Skip to the next object */
			search_start = (void*)((char*)search_start + canarified_obj_size);
		} while (search_start <= addr);

		/* We've searched past the address we were looking for. */
		if (!obj_to_pin) {
			pinning_front = search_start;
			goto next_pin_queue_entry;
		}

		/*
		 * We've found an object to pin.  It might still be a dummy array, but we
		 * can advance the pinning front in any case.
		 */
		pinning_front = (char*)obj_to_pin + obj_to_pin_size;

		/*
		 * If this is a dummy array marking the beginning of a nursery
		 * fragment, we don't pin it.
		 */
		if (sgen_client_object_is_array_fill (obj_to_pin))
			goto next_pin_queue_entry;

		/*
		 * Finally - pin the object!
		 */
		desc = sgen_obj_get_descriptor_safe (obj_to_pin);

		if (do_scan_objects) {
			scan_func (obj_to_pin, desc, queue);
		} else {
			SGEN_LOG (4, "Pinned object %p, vtable %p (%s), count %d\n",
					obj_to_pin, *(void**)obj_to_pin, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (obj_to_pin)), count);
			sgen_binary_protocol_pin (obj_to_pin,
					(gpointer)LOAD_VTABLE (obj_to_pin),
					safe_object_get_size (obj_to_pin));

			pin_object (obj_to_pin);
			GRAY_OBJECT_ENQUEUE_SERIAL (queue, obj_to_pin, desc);
			sgen_pin_stats_register_object (obj_to_pin, GENERATION_NURSERY);
			definitely_pinned [count] = obj_to_pin;
			count++;
		}
		if (sgen_concurrent_collection_in_progress)
			sgen_pinning_register_pinned_in_nursery (obj_to_pin);

	next_pin_queue_entry:
		last = addr;
		++start;
	}
	sgen_client_nursery_objects_pinned (definitely_pinned, count);
	stat_pinned_objects += count;
	return count;
}

static void
pin_objects_in_nursery (gboolean do_scan_objects, ScanCopyContext ctx)
{
	size_t reduced_to;

	if (sgen_nursery_section->pin_queue_first_entry == sgen_nursery_section->pin_queue_last_entry)
		return;

	reduced_to = pin_objects_from_nursery_pin_queue (do_scan_objects, ctx);
	sgen_nursery_section->pin_queue_last_entry = sgen_nursery_section->pin_queue_first_entry + reduced_to;
}

/*
 * This function is only ever called (via `collector_pin_object()` in `sgen-copy-object.h`)
 * when we can't promote an object because we're out of memory.
 */
void
sgen_pin_object (GCObject *object, SgenGrayQueue *queue)
{
	SGEN_ASSERT (0, sgen_ptr_in_nursery (object), "We're only supposed to use this for pinning nursery objects when out of memory.");

	/*
	 * All pinned objects are assumed to have been staged, so we need to stage as well.
	 * Also, the count of staged objects shows that "late pinning" happened.
	 */
	sgen_pin_stage_ptr (object);

	SGEN_PIN_OBJECT (object);
	sgen_binary_protocol_pin (object, (gpointer)LOAD_VTABLE (object), safe_object_get_size (object));

	++objects_pinned;
	sgen_pin_stats_register_object (object, GENERATION_NURSERY);

	GRAY_OBJECT_ENQUEUE_SERIAL (queue, object, sgen_obj_get_descriptor_safe (object));
}

/* Sort the addresses in array in increasing order.
 * Done using a by-the book heap sort. Which has decent and stable performance, is pretty cache efficient.
 */
void
sgen_sort_addresses (void **array, size_t size)
{
	size_t i;
	void *tmp;

	for (i = 1; i < size; ++i) {
		size_t child = i;
		while (child > 0) {
			size_t parent = (child - 1) / 2;

			if (array [parent] >= array [child])
				break;

			tmp = array [parent];
			array [parent] = array [child];
			array [child] = tmp;

			child = parent;
		}
	}

	for (i = size - 1; i > 0; --i) {
		size_t end, root;
		tmp = array [i];
		array [i] = array [0];
		array [0] = tmp;

		end = i - 1;
		root = 0;

		while (root * 2 + 1 <= end) {
			size_t child = root * 2 + 1;

			if (child < end && array [child] < array [child + 1])
				++child;
			if (array [root] >= array [child])
				break;

			tmp = array [root];
			array [root] = array [child];
			array [child] = tmp;

			root = child;
		}
	}
}

/* 
 * Scan the memory between start and end and queue values which could be pointers
 * to the area between start_nursery and end_nursery for later consideration.
 * Typically used for thread stacks.
 */
MONO_NO_SANITIZE_ADDRESS
void
sgen_conservatively_pin_objects_from (void **start, void **end, void *start_nursery, void *end_nursery, int pin_type)
{
	int count = 0;

	SGEN_ASSERT (0, ((mword)start & (SIZEOF_VOID_P - 1)) == 0, "Why are we scanning for references in unaligned memory ?");

#if defined(VALGRIND_MAKE_MEM_DEFINED_IF_ADDRESSABLE) && !defined(_WIN64)
	VALGRIND_MAKE_MEM_DEFINED_IF_ADDRESSABLE (start, (char*)end - (char*)start);
#endif

	while (start < end) {
		/*
		 * *start can point to the middle of an object
		 * note: should we handle pointing at the end of an object?
		 * pinning in C# code disallows pointing at the end of an object
		 * but there is some small chance that an optimizing C compiler
		 * may keep the only reference to an object by pointing
		 * at the end of it. We ignore this small chance for now.
		 * Pointers to the end of an object are indistinguishable
		 * from pointers to the start of the next object in memory
		 * so if we allow that we'd need to pin two objects...
		 * We queue the pointer in an array, the
		 * array will then be sorted and uniqued. This way
		 * we can coalesce several pinning pointers and it should
		 * be faster since we'd do a memory scan with increasing
		 * addresses. Note: we can align the address to the allocation
		 * alignment, so the unique process is more effective.
		 */
		mword addr = (mword)*start;
		addr &= ~(ALLOC_ALIGN - 1);
		if (addr >= (mword)start_nursery && addr < (mword)end_nursery) {
			SGEN_LOG (6, "Pinning address %p from %p", (void*)addr, start);
			sgen_pin_stage_ptr ((void*)addr);
			sgen_binary_protocol_pin_stage (start, (void*)addr);
			sgen_pin_stats_register_address ((char*)addr, pin_type);
			count++;
		}
		start++;
	}
	if (count)
		SGEN_LOG (7, "found %d potential pinned heap pointers", count);
}

/*
 * The first thing we do in a collection is to identify pinned objects.
 * This function considers all the areas of memory that need to be
 * conservatively scanned.
 */
static void
pin_from_roots (void *start_nursery, void *end_nursery, ScanCopyContext ctx)
{
	void **start_root;
	RootRecord *root;
	SGEN_LOG (2, "Scanning pinned roots (%d bytes, %d/%d entries)", (int)roots_size, sgen_roots_hash [ROOT_TYPE_NORMAL].num_entries, sgen_roots_hash [ROOT_TYPE_PINNED].num_entries);
	/* objects pinned from the API are inside these roots */
	SGEN_HASH_TABLE_FOREACH (&sgen_roots_hash [ROOT_TYPE_PINNED], void **, start_root, RootRecord *, root) {
		SGEN_LOG (6, "Pinned roots %p-%p", start_root, root->end_root);
		sgen_conservatively_pin_objects_from (start_root, (void**)root->end_root, start_nursery, end_nursery, PIN_TYPE_OTHER);
	} SGEN_HASH_TABLE_FOREACH_END;
	/* now deal with the thread stacks
	 * in the future we should be able to conservatively scan only:
	 * *) the cpu registers
	 * *) the unmanaged stack frames
	 * *) the _last_ managed stack frame
	 * *) pointers slots in managed frames
	 */
	sgen_client_scan_thread_data (start_nursery, end_nursery, FALSE, ctx);
}

static void
single_arg_user_copy_or_mark (GCObject **obj, void *gc_data)
{
	ScanCopyContext *ctx = (ScanCopyContext *)gc_data;
	ctx->ops->copy_or_mark_object (obj, ctx->queue);
}

/*
 * The memory area from start_root to end_root contains pointers to objects.
 * Their position is precisely described by @desc (this means that the pointer
 * can be either NULL or the pointer to the start of an object).
 * This functions copies them to to_space updates them.
 *
 * This function is not thread-safe!
 */
static void
precisely_scan_objects_from (void** start_root, void** end_root, char* n_start, char *n_end, SgenDescriptor desc, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	ScanPtrFieldFunc scan_field_func = ctx.ops->scan_ptr_field;
	SgenGrayQueue *queue = ctx.queue;

	switch (desc & ROOT_DESC_TYPE_MASK) {
	case ROOT_DESC_BITMAP:
		desc >>= ROOT_DESC_TYPE_SHIFT;
		while (desc) {
			if ((desc & 1) && *start_root) {
				copy_func ((GCObject**)start_root, queue);
				SGEN_LOG (9, "Overwrote root at %p with %p", start_root, *start_root);
			}
			desc >>= 1;
			start_root++;
		}
		return;
	case ROOT_DESC_COMPLEX: {
		gsize *bitmap_data = (gsize *)sgen_get_complex_descriptor_bitmap (desc);
		gsize bwords = (*bitmap_data) - 1;
		void **start_run = start_root;
		bitmap_data++;
		while (bwords-- > 0) {
			gsize bmap = *bitmap_data++;
			void **objptr = start_run;
			while (bmap) {
				if ((bmap & 1) && *objptr) {
					copy_func ((GCObject**)objptr, queue);
					SGEN_LOG (9, "Overwrote root at %p with %p", objptr, *objptr);
				}
				bmap >>= 1;
				++objptr;
			}
			start_run += GC_BITS_PER_WORD;
		}
		break;
	}
	case ROOT_DESC_VECTOR: {
		void **p;

		for (p = start_root; p < end_root; p++) {
			if (*p)
				scan_field_func (NULL, (GCObject**)p, queue);
		}
		break;
	}
	case ROOT_DESC_USER: {
		SgenUserRootMarkFunc marker = sgen_get_user_descriptor_func (desc);
		marker (start_root, single_arg_user_copy_or_mark, &ctx);
		break;
	}
	case ROOT_DESC_RUN_LEN:
		g_assert_not_reached ();
	default:
		g_assert_not_reached ();
	}
}

static void
reset_heap_boundaries (void)
{
	lowest_heap_address = ~(mword)0;
	highest_heap_address = 0;
}

void
sgen_update_heap_boundaries (mword low, mword high)
{
	mword old;

	do {
		old = lowest_heap_address;
		if (low >= old)
			break;
	} while (SGEN_CAS_PTR ((gpointer*)&lowest_heap_address, (gpointer)low, (gpointer)old) != (gpointer)old);

	do {
		old = highest_heap_address;
		if (high <= old)
			break;
	} while (SGEN_CAS_PTR ((gpointer*)&highest_heap_address, (gpointer)high, (gpointer)old) != (gpointer)old);
}

/*
 * Allocate and setup the data structures needed to be able to allocate objects
 * in the nursery. The nursery is stored in sgen_nursery_section.
 */
static void
alloc_nursery (gboolean dynamic, size_t min_size, size_t max_size)
{
	char *data;
	size_t scan_starts;

	if (dynamic) {
		if (!min_size)
			min_size = SGEN_DEFAULT_NURSERY_MIN_SIZE;
		if (!max_size)
			max_size = SGEN_DEFAULT_NURSERY_MAX_SIZE;
	} else {
		SGEN_ASSERT (0, min_size == max_size, "We can't have nursery ranges for static configuration.");
		if (!min_size)
			min_size = max_size = SGEN_DEFAULT_NURSERY_SIZE;
	}

	SGEN_ASSERT (0, !sgen_nursery_section, "Why are we allocating the nursery twice?");
	SGEN_LOG (2, "Allocating nursery size: %" G_GSIZE_FORMAT "u, initial %" G_GSIZE_FORMAT "u", max_size, min_size);

	/* FIXME: handle OOM */
	sgen_nursery_section = (GCMemSection *)sgen_alloc_internal (INTERNAL_MEM_SECTION);

	/* If there isn't enough space even for the nursery we should simply abort. */
	g_assert (sgen_memgov_try_alloc_space (max_size, SPACE_NURSERY));

	/*
	 * The nursery section range represents the memory section where objects
	 * can be found. This is used when iterating for objects in the nursery,
	 * pinning etc. sgen_nursery_max_size represents the total allocated space
	 * for the nursery. sgen_nursery_size represents the current size of the
	 * nursery and it is used for allocation limits, heuristics etc. The
	 * nursery section is not always identical to the current nursery size
	 * because it can contain pinned objects from when the nursery was larger.
	 *
	 * sgen_nursery_size <= sgen_nursery_section size <= sgen_nursery_max_size
	 */
	data = (char *)sgen_major_collector.alloc_heap (max_size, max_size);
	sgen_update_heap_boundaries ((mword)data, (mword)(data + max_size));
	sgen_nursery_section->data = data;
	sgen_nursery_section->end_data = data + min_size;
	scan_starts = (max_size + SCAN_START_SIZE - 1) / SCAN_START_SIZE;
	sgen_nursery_section->scan_starts = (char **)sgen_alloc_internal_dynamic (sizeof (char*) * scan_starts, INTERNAL_MEM_SCAN_STARTS, TRUE);
	sgen_nursery_section->num_scan_start = scan_starts;

	sgen_nursery_allocator_set_nursery_bounds (data, min_size, max_size);
}

FILE *
mono_gc_get_logfile (void)
{
	return sgen_gc_debug_file;
}

void
mono_gc_params_set (const char* options)
{
	if (gc_params_options)
		g_free (gc_params_options);

	gc_params_options = g_strdup (options);
}

void
mono_gc_debug_set (const char* options)
{
	if (gc_debug_options)
		g_free (gc_debug_options);

	gc_debug_options = g_strdup (options);
}

static void
scan_finalizer_entries (SgenPointerQueue *fin_queue, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	SgenGrayQueue *queue = ctx.queue;
	size_t i;

	for (i = 0; i < fin_queue->next_slot; ++i) {
		GCObject *obj = (GCObject *)fin_queue->data [i];
		if (!obj)
			continue;
		SGEN_LOG (5, "Scan of fin ready object: %p (%s)\n", obj, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (obj)));
		copy_func ((GCObject**)&fin_queue->data [i], queue);
	}
}

static const char*
generation_name (int generation)
{
	switch (generation) {
	case GENERATION_NURSERY: return "nursery";
	case GENERATION_OLD: return "old";
	default: g_assert_not_reached ();
	}
}

const char*
sgen_generation_name (int generation)
{
	return generation_name (generation);
}

static void
finish_gray_stack (int generation, ScanCopyContext ctx)
{
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	int done_with_ephemerons, ephemeron_rounds = 0;
	char *start_addr = generation == GENERATION_NURSERY ? sgen_get_nursery_start () : NULL;
	char *end_addr = generation == GENERATION_NURSERY ? sgen_get_nursery_end () : (char*)-1;
	SgenGrayQueue *queue = ctx.queue;

	sgen_binary_protocol_finish_gray_stack_start (sgen_timestamp (), generation);
	/*
	 * We copied all the reachable objects. Now it's the time to copy
	 * the objects that were not referenced by the roots, but by the copied objects.
	 * we built a stack of objects pointed to by gray_start: they are
	 * additional roots and we may add more items as we go.
	 * We loop until gray_start == gray_objects which means no more objects have
	 * been added. Note this is iterative: no recursion is involved.
	 * We need to walk the LO list as well in search of marked big objects
	 * (use a flag since this is needed only on major collections). We need to loop
	 * here as well, so keep a counter of marked LO (increasing it in copy_object).
	 *   To achieve better cache locality and cache usage, we drain the gray stack 
	 * frequently, after each object is copied, and just finish the work here.
	 */
	sgen_drain_gray_stack (ctx);
	TV_GETTIME (atv);
	SGEN_LOG (2, "%s generation done", generation_name (generation));

	/*
	Reset bridge data, we might have lingering data from a previous collection if this is a major
	collection trigged by minor overflow.

	We must reset the gathered bridges since their original block might be evacuated due to major
	fragmentation in the meanwhile and the bridge code should not have to deal with that.
	*/
	if (sgen_client_bridge_need_processing ())
		sgen_client_bridge_reset_data ();

	/*
	 * Mark all strong toggleref objects. This must be done before we walk ephemerons or finalizers
	 * to ensure they see the full set of live objects.
	 */
	sgen_client_mark_togglerefs (start_addr, end_addr, ctx);

	/*
	 * Walk the ephemeron tables marking all values with reachable keys. This must be completely done
	 * before processing finalizable objects and non-tracking weak links to avoid finalizing/clearing
	 * objects that are in fact reachable.
	 */
	done_with_ephemerons = 0;
	do {
		done_with_ephemerons = sgen_client_mark_ephemerons (ctx);
		sgen_drain_gray_stack (ctx);
		++ephemeron_rounds;
	} while (!done_with_ephemerons);

	if (sgen_client_bridge_need_processing ()) {
		/*Make sure the gray stack is empty before we process bridge objects so we get liveness right*/
		sgen_drain_gray_stack (ctx);
		sgen_collect_bridge_objects (generation, ctx);
		if (generation == GENERATION_OLD)
			sgen_collect_bridge_objects (GENERATION_NURSERY, ctx);

		/*
		Do the first bridge step here, as the collector liveness state will become useless after that.

		An important optimization is to only proccess the possibly dead part of the object graph and skip
		over all live objects as we transitively know everything they point must be alive too.

		The above invariant is completely wrong if we let the gray queue be drained and mark/copy everything.

		This has the unfortunate side effect of making overflow collections perform the first step twice, but
		given we now have heuristics that perform major GC in anticipation of minor overflows this should not
		be a big deal.
		*/
		sgen_client_bridge_processing_stw_step ();
	}

	/*
	Make sure we drain the gray stack before processing disappearing links and finalizers.
	If we don't make sure it is empty we might wrongly see a live object as dead.
	*/
	sgen_drain_gray_stack (ctx);

	/*
	We must clear weak links that don't track resurrection before processing object ready for
	finalization so they can be cleared before that.
	*/
	sgen_null_link_in_range (generation, ctx, FALSE);
	if (generation == GENERATION_OLD)
		sgen_null_link_in_range (GENERATION_NURSERY, ctx, FALSE);


	/* walk the finalization queue and move also the objects that need to be
	 * finalized: use the finalized objects as new roots so the objects they depend
	 * on are also not reclaimed. As with the roots above, only objects in the nursery
	 * are marked/copied.
	 */
	sgen_finalize_in_range (generation, ctx);
	if (generation == GENERATION_OLD)
		sgen_finalize_in_range (GENERATION_NURSERY, ctx);
	/* drain the new stack that might have been created */
	SGEN_LOG (6, "Precise scan of gray area post fin");
	sgen_drain_gray_stack (ctx);

	/*
	 * This must be done again after processing finalizable objects since CWL slots are cleared only after the key is finalized.
	 */
	done_with_ephemerons = 0;
	do {
		done_with_ephemerons = sgen_client_mark_ephemerons (ctx);
		sgen_drain_gray_stack (ctx);
		++ephemeron_rounds;
	} while (!done_with_ephemerons);

	sgen_client_clear_unreachable_ephemerons (ctx);

	/*
	 * We clear togglerefs only after all possible chances of revival are done. 
	 * This is semantically more inline with what users expect and it allows for
	 * user finalizers to correctly interact with TR objects.
	*/
	sgen_client_clear_togglerefs (start_addr, end_addr, ctx);

	TV_GETTIME (btv);
	SGEN_LOG (2, "Finalize queue handling scan for %s generation: %" PRId64 " usecs %d ephemeron rounds", generation_name (generation), (gint64)(TV_ELAPSED (atv, btv) / 10), ephemeron_rounds);

	/*
	 * handle disappearing links
	 * Note we do this after checking the finalization queue because if an object
	 * survives (at least long enough to be finalized) we don't clear the link.
	 * This also deals with a possible issue with the monitor reclamation: with the Boehm
	 * GC a finalized object my lose the monitor because it is cleared before the finalizer is
	 * called.
	 */
	g_assert (sgen_gray_object_queue_is_empty (queue));
	for (;;) {
		sgen_null_link_in_range (generation, ctx, TRUE);
		if (generation == GENERATION_OLD)
			sgen_null_link_in_range (GENERATION_NURSERY, ctx, TRUE);
		if (sgen_gray_object_queue_is_empty (queue))
			break;
		sgen_drain_gray_stack (ctx);
	}

	g_assert (sgen_gray_object_queue_is_empty (queue));

	sgen_binary_protocol_finish_gray_stack_end (sgen_timestamp (), generation);
}

void
sgen_check_section_scan_starts (GCMemSection *section)
{
	size_t i;
	for (i = 0; i < section->num_scan_start; ++i) {
		if (section->scan_starts [i]) {
			mword size = safe_object_get_size ((GCObject*) section->scan_starts [i]);
			SGEN_ASSERT (0, size >= SGEN_CLIENT_MINIMUM_OBJECT_SIZE && size <= MAX_SMALL_OBJ_SIZE, "Weird object size at scan starts.");
		}
	}
}

static void
check_scan_starts (void)
{
	if (!do_scan_starts_check)
		return;
	sgen_check_section_scan_starts (sgen_nursery_section);
	sgen_major_collector.check_scan_starts ();
}

static void
scan_from_registered_roots (char *addr_start, char *addr_end, int root_type, ScanCopyContext ctx)
{
	void **start_root;
	RootRecord *root;
	SGEN_HASH_TABLE_FOREACH (&sgen_roots_hash [root_type], void **, start_root, RootRecord *, root) {
		SGEN_LOG (6, "Precise root scan %p-%p (desc: %p)", start_root, root->end_root, (void*)(uintptr_t)root->root_desc);
		precisely_scan_objects_from (start_root, (void**)root->end_root, addr_start, addr_end, root->root_desc, ctx);
	} SGEN_HASH_TABLE_FOREACH_END;
}

static void
init_stats (void)
{
	static gboolean inited = FALSE;

	if (inited)
		return;

	mono_counters_register ("Collection max time",  MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME | MONO_COUNTER_MONOTONIC, &time_max);

	mono_counters_register ("Minor fragment clear", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_pre_collection_fragment_clear);
	mono_counters_register ("Minor pinning", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_pinning);
	mono_counters_register ("Minor scan remembered set", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_scan_remsets);
	mono_counters_register ("Minor scan major blocks", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_scan_major_blocks);
	mono_counters_register ("Minor scan los", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_scan_los);
	mono_counters_register ("Minor scan pinned", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_scan_pinned);
	mono_counters_register ("Minor scan roots", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_scan_roots);
	mono_counters_register ("Minor fragment creation", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_minor_fragment_creation);

	mono_counters_register ("Major fragment clear", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_pre_collection_fragment_clear);
	mono_counters_register ("Major pinning", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_pinning);
	mono_counters_register ("Major scan pinned", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_scan_pinned);
	mono_counters_register ("Major scan roots", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_scan_roots);
	mono_counters_register ("Major scan mod union blocks", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_scan_mod_union_blocks);
	mono_counters_register ("Major scan mod union los", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_scan_mod_union_los);
	mono_counters_register ("Major finish gray stack", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_finish_gray_stack);
	mono_counters_register ("Major free big objects", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_free_bigobjs);
	mono_counters_register ("Major LOS sweep", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_los_sweep);
	mono_counters_register ("Major sweep", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_sweep);
	mono_counters_register ("Major fragment creation", MONO_COUNTER_GC | MONO_COUNTER_ULONG | MONO_COUNTER_TIME, &time_major_fragment_creation);

	mono_counters_register ("Number of pinned objects", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_pinned_objects);

#ifdef HEAVY_STATISTICS
	mono_counters_register ("WBarrier remember pointer", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_add_to_global_remset);
	mono_counters_register ("WBarrier arrayref copy", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_arrayref_copy);
	mono_counters_register ("WBarrier generic store called", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_generic_store);
	mono_counters_register ("WBarrier generic atomic store called", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_generic_store_atomic);
	mono_counters_register ("WBarrier set root", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_wbarrier_set_root);

	mono_counters_register ("# objects allocated degraded", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_objects_alloced_degraded);
	mono_counters_register ("bytes allocated degraded", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_bytes_alloced_degraded);

	mono_counters_register ("# copy_object() called (nursery)", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copy_object_called_nursery);
	mono_counters_register ("# objects copied (nursery)", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_objects_copied_nursery);
	mono_counters_register ("# copy_object() called (major)", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copy_object_called_major);
	mono_counters_register ("# objects copied (major)", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_objects_copied_major);

	mono_counters_register ("# scan_object() called (nursery)", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scan_object_called_nursery);
	mono_counters_register ("# scan_object() called (major)", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scan_object_called_major);

	mono_counters_register ("Slots allocated in vain", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_slots_allocated_in_vain);

	mono_counters_register ("# nursery copy_object() failed from space", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_nursery_copy_object_failed_from_space);
	mono_counters_register ("# nursery copy_object() failed forwarded", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_nursery_copy_object_failed_forwarded);
	mono_counters_register ("# nursery copy_object() failed pinned", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_nursery_copy_object_failed_pinned);
	mono_counters_register ("# nursery copy_object() failed to space", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_nursery_copy_object_failed_to_space);

	sgen_nursery_allocator_init_heavy_stats ();
#endif

	inited = TRUE;
}


static void
reset_pinned_from_failed_allocation (void)
{
	bytes_pinned_from_failed_allocation = 0;
}

void
sgen_set_pinned_from_failed_allocation (mword objsize)
{
	bytes_pinned_from_failed_allocation += objsize;
}

#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
gboolean
sgen_collection_is_concurrent (void)
{
	switch (sgen_current_collection_generation) {
	case GENERATION_NURSERY:
		return FALSE;
	case GENERATION_OLD:
		return sgen_concurrent_collection_in_progress;
	default:
		g_error ("Invalid current generation %d", sgen_current_collection_generation);
	}
	return FALSE;
}

gboolean
sgen_get_concurrent_collection_in_progress (void)
{
	return sgen_concurrent_collection_in_progress;
}
#endif

typedef struct {
	SgenThreadPoolJob job;
	SgenObjectOperations *ops;
	SgenGrayQueue *gc_thread_gray_queue;
} ScanJob;

typedef struct {
	ScanJob scan_job;
	int job_index, job_split_count;
	int data;
} ParallelScanJob;

typedef struct {
	SgenThreadPoolJob job;
	int job_index, job_split_count;
	int data;
	sgen_cardtable_block_callback callback;
} ParallelIterateBlockRangesJob;

static ScanCopyContext
scan_copy_context_for_scan_job (void *worker_data_untyped, ScanJob *job)
{
	WorkerData *worker_data = (WorkerData *)worker_data_untyped;

	if (!job->ops) {
		/*
		 * For jobs enqueued on workers we set the ops at job runtime in order
		 * to be able to profit from on the fly optimized object ops or other
		 * object ops changes, like forced concurrent finish.
		 */
		SGEN_ASSERT (0, sgen_workers_is_worker_thread (mono_native_thread_id_get ()), "We need a context for the scan job");
		job->ops = sgen_workers_get_idle_func_object_ops (worker_data);
	}

	return CONTEXT_FROM_OBJECT_OPERATIONS (job->ops, sgen_workers_get_job_gray_queue (worker_data, job->gc_thread_gray_queue));
}

typedef struct {
	ScanJob scan_job;
	char *heap_start;
	char *heap_end;
	int root_type;
} ScanFromRegisteredRootsJob;

static void
job_scan_from_registered_roots (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ScanFromRegisteredRootsJob *job_data = (ScanFromRegisteredRootsJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, &job_data->scan_job);

	scan_from_registered_roots (job_data->heap_start, job_data->heap_end, job_data->root_type, ctx);
}

typedef struct {
	ScanJob scan_job;
	char *heap_start;
	char *heap_end;
} ScanThreadDataJob;

static void
job_scan_thread_data (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ScanThreadDataJob *job_data = (ScanThreadDataJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, &job_data->scan_job);

	sgen_client_scan_thread_data (job_data->heap_start, job_data->heap_end, TRUE, ctx);
}

typedef struct {
	ScanJob scan_job;
	SgenPointerQueue *queue;
} ScanFinalizerEntriesJob;

static void
job_scan_finalizer_entries (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ScanFinalizerEntriesJob *job_data = (ScanFinalizerEntriesJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, &job_data->scan_job);

	scan_finalizer_entries (job_data->queue, ctx);
}

static void
job_scan_wbroots (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ScanJob *job_data = (ScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, job_data);

	sgen_wbroots_scan_card_table (ctx);
}

static void
job_scan_major_card_table (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);
	ParallelScanJob *job_data = (ParallelScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, (ScanJob*)job_data);

	SGEN_TV_GETTIME (atv);
	sgen_major_collector.scan_card_table (CARDTABLE_SCAN_GLOBAL, ctx, job_data->job_index, job_data->job_split_count, job_data->data);
	SGEN_TV_GETTIME (btv);

	gint64 elapsed_time = SGEN_TV_ELAPSED (atv, btv);
	SGEN_ATOMIC_ADD_I64 (time_minor_scan_major_blocks, elapsed_time);

	if (worker_data_untyped)
		((WorkerData*)worker_data_untyped)->major_scan_time += elapsed_time;
}

static void
job_major_collector_iterate_block_ranges (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ParallelIterateBlockRangesJob *job_data = (ParallelIterateBlockRangesJob*)job;
	sgen_major_collector.iterate_block_ranges_in_parallel (job_data->callback, job_data->job_index, job_data->job_split_count, job_data->data);
}

static void
job_scan_los_card_table (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);
	ParallelScanJob *job_data = (ParallelScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, (ScanJob*)job_data);

	SGEN_TV_GETTIME (atv);
	sgen_los_scan_card_table (CARDTABLE_SCAN_GLOBAL, ctx, job_data->job_index, job_data->job_split_count);
	SGEN_TV_GETTIME (btv);

	gint64 elapsed_time = SGEN_TV_ELAPSED (atv, btv);
	SGEN_ATOMIC_ADD_I64 (time_minor_scan_los, elapsed_time);

	if (worker_data_untyped)
		((WorkerData*)worker_data_untyped)->los_scan_time += elapsed_time;
}

static void
job_los_iterate_live_block_ranges (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ParallelIterateBlockRangesJob *job_data = (ParallelIterateBlockRangesJob*)job;
	sgen_los_iterate_live_block_range_jobs (job_data->callback, job_data->job_index, job_data->job_split_count);
}

static void
job_scan_major_mod_union_card_table (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);
	ParallelScanJob *job_data = (ParallelScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, (ScanJob*)job_data);

	g_assert (sgen_concurrent_collection_in_progress);
	SGEN_TV_GETTIME (atv);
	sgen_major_collector.scan_card_table (CARDTABLE_SCAN_MOD_UNION, ctx, job_data->job_index, job_data->job_split_count, job_data->data);
	SGEN_TV_GETTIME (btv);

	gint64 elapsed_time = SGEN_TV_ELAPSED (atv, btv);
	SGEN_ATOMIC_ADD_I64 (time_minor_scan_los, time_major_scan_mod_union_blocks);

	if (worker_data_untyped)
		((WorkerData*)worker_data_untyped)->major_scan_time += elapsed_time;
}

static void
job_scan_los_mod_union_card_table (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);
	ParallelScanJob *job_data = (ParallelScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, (ScanJob*)job_data);

	g_assert (sgen_concurrent_collection_in_progress);
	SGEN_TV_GETTIME (atv);
	sgen_los_scan_card_table (CARDTABLE_SCAN_MOD_UNION, ctx, job_data->job_index, job_data->job_split_count);
	SGEN_TV_GETTIME (btv);

	gint64 elapsed_time = SGEN_TV_ELAPSED (atv, btv);
	SGEN_ATOMIC_ADD_I64 (time_minor_scan_los, time_major_scan_mod_union_los);

	if (worker_data_untyped)
		((WorkerData*)worker_data_untyped)->los_scan_time += elapsed_time;
}

static void
job_major_mod_union_preclean (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);
	ParallelScanJob *job_data = (ParallelScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, (ScanJob*)job_data);

	g_assert (sgen_concurrent_collection_in_progress);
	SGEN_TV_GETTIME (atv);
	sgen_major_collector.scan_card_table (CARDTABLE_SCAN_MOD_UNION_PRECLEAN, ctx, job_data->job_index, job_data->job_split_count, job_data->data);
	SGEN_TV_GETTIME (btv);

	g_assert (worker_data_untyped);
	((WorkerData*)worker_data_untyped)->major_scan_time += SGEN_TV_ELAPSED (atv, btv);
}

static void
job_los_mod_union_preclean (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);
	ParallelScanJob *job_data = (ParallelScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, (ScanJob*)job_data);

	g_assert (sgen_concurrent_collection_in_progress);
	SGEN_TV_GETTIME (atv);
	sgen_los_scan_card_table (CARDTABLE_SCAN_MOD_UNION_PRECLEAN, ctx, job_data->job_index, job_data->job_split_count);
	SGEN_TV_GETTIME (btv);

	g_assert (worker_data_untyped);
	((WorkerData*)worker_data_untyped)->los_scan_time += SGEN_TV_ELAPSED (atv, btv);
}

static void
job_scan_last_pinned (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ScanJob *job_data = (ScanJob*)job;
	ScanCopyContext ctx = scan_copy_context_for_scan_job (worker_data_untyped, job_data);

	g_assert (sgen_concurrent_collection_in_progress);

	sgen_scan_pin_queue_objects (ctx);
}

static void
workers_finish_callback (void)
{
	ParallelScanJob *psj;
	ScanJob *sj;
	size_t num_major_sections = sgen_major_collector.get_num_major_sections ();
	int split_count = sgen_workers_get_job_split_count (GENERATION_OLD);
	int i;
	/* Mod union preclean jobs */
	for (i = 0; i < split_count; i++) {
		psj = (ParallelScanJob*)sgen_thread_pool_job_alloc ("preclean major mod union cardtable", job_major_mod_union_preclean, sizeof (ParallelScanJob));
		psj->scan_job.gc_thread_gray_queue = NULL;
		psj->job_index = i;
		psj->job_split_count = split_count;
		psj->data = num_major_sections / split_count;
		sgen_workers_enqueue_job (GENERATION_OLD, &psj->scan_job.job, TRUE);
	}

	for (i = 0; i < split_count; i++) {
		psj = (ParallelScanJob*)sgen_thread_pool_job_alloc ("preclean los mod union cardtable", job_los_mod_union_preclean, sizeof (ParallelScanJob));
		psj->scan_job.gc_thread_gray_queue = NULL;
		psj->job_index = i;
		psj->job_split_count = split_count;
		sgen_workers_enqueue_job (GENERATION_OLD, &psj->scan_job.job, TRUE);
	}

	sj = (ScanJob*)sgen_thread_pool_job_alloc ("scan last pinned", job_scan_last_pinned, sizeof (ScanJob));
	sj->gc_thread_gray_queue = NULL;
	sgen_workers_enqueue_job (GENERATION_OLD, &sj->job, TRUE);
}

static void
init_gray_queue (SgenGrayQueue *gc_thread_gray_queue)
{
	sgen_gray_object_queue_init (gc_thread_gray_queue, NULL, TRUE);
}

static void
enqueue_scan_remembered_set_jobs (SgenGrayQueue *gc_thread_gray_queue, SgenObjectOperations *ops, gboolean is_parallel)
{
	int i, split_count = sgen_workers_get_job_split_count (GENERATION_NURSERY);
	size_t num_major_sections = sgen_major_collector.get_num_major_sections ();
	ScanJob *sj;

	sj = (ScanJob*)sgen_thread_pool_job_alloc ("scan wbroots", job_scan_wbroots, sizeof (ScanJob));
	sj->ops = ops;
	sj->gc_thread_gray_queue = gc_thread_gray_queue;
	sgen_workers_enqueue_deferred_job (GENERATION_NURSERY, &sj->job, is_parallel);

	for (i = 0; i < split_count; i++) {
		ParallelScanJob *psj;
		psj = (ParallelScanJob*)sgen_thread_pool_job_alloc ("scan major remsets", job_scan_major_card_table, sizeof (ParallelScanJob));
		psj->scan_job.ops = ops;
		psj->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
		psj->job_index = i;
		psj->job_split_count = split_count;
		psj->data = num_major_sections / split_count;
		sgen_workers_enqueue_deferred_job (GENERATION_NURSERY, &psj->scan_job.job, is_parallel);

		psj = (ParallelScanJob*)sgen_thread_pool_job_alloc ("scan LOS remsets", job_scan_los_card_table, sizeof (ParallelScanJob));
		psj->scan_job.ops = ops;
		psj->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
		psj->job_index = i;
		psj->job_split_count = split_count;
		sgen_workers_enqueue_deferred_job (GENERATION_NURSERY, &psj->scan_job.job, is_parallel);
	}
}

void
sgen_iterate_all_block_ranges (sgen_cardtable_block_callback callback, gboolean is_parallel)
{
	int i, split_count = sgen_workers_get_job_split_count (GENERATION_NURSERY);
	size_t num_major_sections = sgen_major_collector.get_num_major_sections ();
	ParallelIterateBlockRangesJob *pjob;

	pjob = (ParallelIterateBlockRangesJob*)sgen_thread_pool_job_alloc ("iterate wbroots block ranges", job_wbroots_iterate_live_block_ranges, sizeof (ParallelIterateBlockRangesJob));
	pjob->job_index = 0;
	pjob->job_split_count = split_count;
	pjob->callback = callback;
	sgen_workers_enqueue_deferred_job (GENERATION_NURSERY, &pjob->job, is_parallel);

	for (i = 0; i < split_count; i++) {
		pjob = (ParallelIterateBlockRangesJob*)sgen_thread_pool_job_alloc ("iterate major block ranges", job_major_collector_iterate_block_ranges, sizeof (ParallelIterateBlockRangesJob));
		pjob->job_index = i;
		pjob->job_split_count = split_count;
		pjob->data = num_major_sections / split_count;
		pjob->callback = callback;
		sgen_workers_enqueue_deferred_job (GENERATION_NURSERY, &pjob->job, is_parallel);

		pjob = (ParallelIterateBlockRangesJob*)sgen_thread_pool_job_alloc ("iterate LOS block ranges", job_los_iterate_live_block_ranges, sizeof (ParallelIterateBlockRangesJob));
		pjob->job_index = i;
		pjob->job_split_count = split_count;
		pjob->callback = callback;
		sgen_workers_enqueue_deferred_job (GENERATION_NURSERY, &pjob->job, is_parallel);
	}

	sgen_workers_flush_deferred_jobs (GENERATION_NURSERY, is_parallel);

	if (is_parallel) {
		sgen_workers_start_all_workers (GENERATION_NURSERY, NULL, NULL, NULL);
		sgen_workers_join (GENERATION_NURSERY);
	}
}

static void
enqueue_scan_from_roots_jobs (SgenGrayQueue *gc_thread_gray_queue, char *heap_start, char *heap_end, SgenObjectOperations *ops, gboolean is_parallel)
{
	ScanFromRegisteredRootsJob *scrrj;
	ScanThreadDataJob *stdj;
	ScanFinalizerEntriesJob *sfej;

	/* registered roots, this includes static fields */

	scrrj = (ScanFromRegisteredRootsJob*)sgen_thread_pool_job_alloc ("scan from registered roots normal", job_scan_from_registered_roots, sizeof (ScanFromRegisteredRootsJob));
	scrrj->scan_job.ops = ops;
	scrrj->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
	scrrj->heap_start = heap_start;
	scrrj->heap_end = heap_end;
	scrrj->root_type = ROOT_TYPE_NORMAL;
	sgen_workers_enqueue_deferred_job (sgen_current_collection_generation, &scrrj->scan_job.job, is_parallel);

	if (sgen_current_collection_generation == GENERATION_OLD) {
		/* During minors we scan the cardtable for these roots instead */
		scrrj = (ScanFromRegisteredRootsJob*)sgen_thread_pool_job_alloc ("scan from registered roots wbarrier", job_scan_from_registered_roots, sizeof (ScanFromRegisteredRootsJob));
		scrrj->scan_job.ops = ops;
		scrrj->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
		scrrj->heap_start = heap_start;
		scrrj->heap_end = heap_end;
		scrrj->root_type = ROOT_TYPE_WBARRIER;
		sgen_workers_enqueue_deferred_job (sgen_current_collection_generation, &scrrj->scan_job.job, is_parallel);
	}

	/* Threads */

	stdj = (ScanThreadDataJob*)sgen_thread_pool_job_alloc ("scan thread data", job_scan_thread_data, sizeof (ScanThreadDataJob));
	stdj->scan_job.ops = ops;
	stdj->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
	stdj->heap_start = heap_start;
	stdj->heap_end = heap_end;
	sgen_workers_enqueue_deferred_job (sgen_current_collection_generation, &stdj->scan_job.job, is_parallel);

	/* Scan the list of objects ready for finalization. */

	sfej = (ScanFinalizerEntriesJob*)sgen_thread_pool_job_alloc ("scan finalizer entries", job_scan_finalizer_entries, sizeof (ScanFinalizerEntriesJob));
	sfej->scan_job.ops = ops;
	sfej->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
	sfej->queue = &fin_ready_queue;
	sgen_workers_enqueue_deferred_job (sgen_current_collection_generation, &sfej->scan_job.job, is_parallel);

	sfej = (ScanFinalizerEntriesJob*)sgen_thread_pool_job_alloc ("scan critical finalizer entries", job_scan_finalizer_entries, sizeof (ScanFinalizerEntriesJob));
	sfej->scan_job.ops = ops;
	sfej->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
	sfej->queue = &critical_fin_queue;
	sgen_workers_enqueue_deferred_job (sgen_current_collection_generation, &sfej->scan_job.job, is_parallel);
}

/*
 * Perform a nursery collection.
 *
 * Return whether any objects were late-pinned due to being out of memory.
 */
static gboolean
collect_nursery (const char *reason, gboolean is_overflow)
{
	gboolean needs_major, is_parallel = FALSE;
	mword fragment_total;
	SgenGrayQueue gc_thread_gray_queue;
	SgenObjectOperations *object_ops_nopar, *object_ops_par = NULL;
	ScanCopyContext ctx;
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	SGEN_TV_DECLARE (last_minor_collection_start_tv);
	SGEN_TV_DECLARE (last_minor_collection_end_tv);
	guint64 major_scan_start = time_minor_scan_major_blocks;
	guint64 los_scan_start = time_minor_scan_los;
	guint64 finish_gray_start = time_minor_finish_gray_stack;

	if (disable_minor_collections)
		return TRUE;

	TV_GETTIME (last_minor_collection_start_tv);
	atv = last_minor_collection_start_tv;

	sgen_binary_protocol_collection_begin (mono_atomic_load_i32 (&mono_gc_stats.minor_gc_count), GENERATION_NURSERY);

	object_ops_nopar = sgen_get_concurrent_collection_in_progress ()
				? &sgen_minor_collector.serial_ops_with_concurrent_major
				: &sgen_minor_collector.serial_ops;
	if (sgen_minor_collector.is_parallel && sgen_nursery_size >= SGEN_PARALLEL_MINOR_MIN_NURSERY_SIZE) {
		object_ops_par = sgen_get_concurrent_collection_in_progress ()
					? &sgen_minor_collector.parallel_ops_with_concurrent_major
					: &sgen_minor_collector.parallel_ops;
		is_parallel = TRUE;
	}

	if (do_verify_nursery || do_dump_nursery_content)
		sgen_debug_verify_nursery (do_dump_nursery_content);

	sgen_current_collection_generation = GENERATION_NURSERY;

	SGEN_ASSERT (0, !sgen_collection_is_concurrent (), "Why is the nursery collection concurrent?");

	reset_pinned_from_failed_allocation ();

	check_scan_starts ();

	sgen_nursery_alloc_prepare_for_minor ();

	sgen_degraded_mode = 0;
	objects_pinned = 0;

	SGEN_LOG (1, "Start nursery collection %" G_GINT32_FORMAT " %p-%p, size: %d", mono_atomic_load_i32 (&mono_gc_stats.minor_gc_count), sgen_nursery_section->data, sgen_nursery_section->end_data, (int)(sgen_nursery_section->end_data - sgen_nursery_section->data));

	/* world must be stopped already */
	TV_GETTIME (btv);
	time_minor_pre_collection_fragment_clear += TV_ELAPSED (atv, btv);

	sgen_client_pre_collection_checks ();

	sgen_major_collector.start_nursery_collection ();

	sgen_memgov_minor_collection_start ();

	init_gray_queue (&gc_thread_gray_queue);
	ctx = CONTEXT_FROM_OBJECT_OPERATIONS (object_ops_nopar, &gc_thread_gray_queue);

	mono_atomic_inc_i32 (&mono_gc_stats.minor_gc_count);

	sgen_process_fin_stage_entries ();

	/* pin from pinned handles */
	sgen_init_pinning ();
	if (sgen_concurrent_collection_in_progress)
		sgen_init_pinning_for_conc ();
	sgen_client_binary_protocol_mark_start (GENERATION_NURSERY);
	pin_from_roots (sgen_nursery_section->data, sgen_nursery_section->end_data, ctx);
	/* pin cemented objects */
	sgen_pin_cemented_objects ();
	/* identify pinned objects */
	sgen_optimize_pin_queue ();
	sgen_pinning_setup_section (sgen_nursery_section);

	pin_objects_in_nursery (FALSE, ctx);
	sgen_pinning_trim_queue_to_section (sgen_nursery_section);
	if (sgen_concurrent_collection_in_progress)
		sgen_finish_pinning_for_conc ();

	if (remset_consistency_checks)
		sgen_check_remset_consistency ();

	if (whole_heap_check_before_collection) {
		sgen_clear_nursery_fragments ();
		sgen_check_whole_heap (FALSE);
	}

	TV_GETTIME (atv);
	time_minor_pinning += TV_ELAPSED (btv, atv);
	SGEN_LOG (2, "Finding pinned pointers: %" G_GSIZE_FORMAT "d in %" PRId64 " usecs", sgen_get_pinned_count (), (gint64)(TV_ELAPSED (btv, atv) / 10));
	SGEN_LOG (4, "Start scan with %" G_GSIZE_FORMAT "d pinned objects", sgen_get_pinned_count ());
	sgen_client_pinning_end ();

	remset.start_scan_remsets (remset_copy_clear_par);
	TV_GETTIME (btv);

	SGEN_LOG (2, "Minor scan copy/clear remsets: %lld usecs", (long long)(TV_ELAPSED (atv, btv) / 10));

	TV_GETTIME (atv);
	enqueue_scan_remembered_set_jobs (&gc_thread_gray_queue, is_parallel ? NULL : object_ops_nopar, is_parallel);
	TV_GETTIME (btv);

	if (!is_parallel) {
		time_minor_scan_remsets += TV_ELAPSED (atv, btv);
		SGEN_LOG (2, "Old generation scan: %" PRId64 " usecs", (gint64)(TV_ELAPSED (atv, btv) / 10));
	}

	sgen_pin_stats_report ();
	sgen_gchandle_stats_report ();

	TV_GETTIME (atv);
	time_minor_scan_pinned += TV_ELAPSED (btv, atv);

	enqueue_scan_from_roots_jobs (&gc_thread_gray_queue, sgen_nursery_section->data, sgen_nursery_section->end_data, is_parallel ? NULL : object_ops_nopar, is_parallel);

	sgen_workers_flush_deferred_jobs (GENERATION_NURSERY, is_parallel);

	if (is_parallel) {
		gray_queue_redirect (&gc_thread_gray_queue);
		sgen_workers_start_all_workers (GENERATION_NURSERY, object_ops_nopar, object_ops_par, NULL);
		sgen_workers_join (GENERATION_NURSERY);
	}

	TV_GETTIME (btv);
	if (!is_parallel) {
		time_minor_scan_roots += TV_ELAPSED (atv, btv);

		SGEN_LOG (2, "Minor scan roots: %lld usecs",
			(long long)(TV_ELAPSED (atv, btv) / 10));
	} else {
		SGEN_LOG (2, "Minor scan remsets + roots: %lld usecs",
			(long long)(TV_ELAPSED (atv, btv) / 10));

		SGEN_LOG (2, "Minor scan remsets: accumulated major scan=%lld usecs, accumulated los scan=%lld usecs, workers=%d",
			(long long)((time_minor_scan_major_blocks - major_scan_start) / 10),
			(long long)((time_minor_scan_los - los_scan_start) / 10),
			sgen_workers_get_active_worker_count (GENERATION_NURSERY));
	}

	finish_gray_stack (GENERATION_NURSERY, ctx);

	TV_GETTIME (atv);
	time_minor_finish_gray_stack += TV_ELAPSED (btv, atv);
	sgen_client_binary_protocol_mark_end (GENERATION_NURSERY);

	if (objects_pinned) {
		sgen_optimize_pin_queue ();
		sgen_pinning_setup_section (sgen_nursery_section);
	}

	/*
	 * This is the latest point at which we can do this check, because
	 * sgen_build_nursery_fragments() unpins nursery objects again.
	 */
	if (remset_consistency_checks)
		sgen_check_remset_consistency ();


	if (sgen_max_pause_time) {
		int duration;

		TV_GETTIME (btv);
		duration = (int)(TV_ELAPSED (last_minor_collection_start_tv, btv) / 10000);
		if (duration > (sgen_max_pause_time * sgen_max_pause_margin))
			sgen_resize_nursery (TRUE);
		else
			sgen_resize_nursery (FALSE);
	} else {
			sgen_resize_nursery (FALSE);
	}

	/*
	 * This is used by the profiler to report GC roots.
	 * Invariants: Heap's finished, no more moves left, objects still pinned in nursery.
	 */
	sgen_client_collecting_minor_report_roots (&fin_ready_queue, &critical_fin_queue);

	/* walk the pin_queue, build up the fragment list of free memory, unmark
	 * pinned objects as we go, memzero() the empty fragments so they are ready for the
	 * next allocations.
	 */
	sgen_client_binary_protocol_reclaim_start (GENERATION_NURSERY);
	fragment_total = sgen_build_nursery_fragments (sgen_nursery_section);
	if (!fragment_total)
		sgen_degraded_mode = 1;

	/* Clear TLABs for all threads */
	sgen_clear_tlabs ();

	sgen_client_binary_protocol_reclaim_end (GENERATION_NURSERY);
	TV_GETTIME (btv);
	time_minor_fragment_creation += TV_ELAPSED (atv, btv);
	SGEN_LOG (2, "Fragment creation: %" PRId64 " usecs, %lu bytes available", (gint64)TV_ELAPSED (atv, btv), (unsigned long)fragment_total);

	if (remset_consistency_checks)
		sgen_check_major_refs ();

	sgen_major_collector.finish_nursery_collection ();

	TV_GETTIME (last_minor_collection_end_tv);
	UnlockedAdd64 (&mono_gc_stats.minor_gc_time, TV_ELAPSED (last_minor_collection_start_tv, last_minor_collection_end_tv));

	sgen_debug_dump_heap ("minor", mono_atomic_load_i32 (&mono_gc_stats.minor_gc_count) - 1, NULL);

	/* prepare the pin queue for the next collection */
	sgen_finish_pinning ();
	if (sgen_have_pending_finalizers ()) {
		SGEN_LOG (4, "Finalizer-thread wakeup");
		sgen_client_finalize_notify ();
	}
	sgen_pin_stats_reset ();
	/* clear cemented hash */
	sgen_cement_clear_below_threshold ();

	sgen_gray_object_queue_dispose (&gc_thread_gray_queue);

	check_scan_starts ();

	sgen_binary_protocol_flush_buffers (FALSE);

	sgen_memgov_minor_collection_end (reason, is_overflow);

	/*objects are late pinned because of lack of memory, so a major is a good call*/
	needs_major = objects_pinned > 0;
	sgen_current_collection_generation = -1;
	objects_pinned = 0;

	if (is_parallel)
		sgen_binary_protocol_collection_end_stats (0, 0, time_minor_finish_gray_stack - finish_gray_start);
	else
		sgen_binary_protocol_collection_end_stats (
			time_minor_scan_major_blocks - major_scan_start,
			time_minor_scan_los - los_scan_start,
			time_minor_finish_gray_stack - finish_gray_start);

	sgen_binary_protocol_collection_end (mono_atomic_load_i32 (&mono_gc_stats.minor_gc_count) - 1, GENERATION_NURSERY, 0, 0);

	if (check_nursery_objects_untag)
		sgen_check_nursery_objects_untag ();

	return needs_major;
}

typedef enum {
	COPY_OR_MARK_FROM_ROOTS_SERIAL,
	COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT,
	COPY_OR_MARK_FROM_ROOTS_FINISH_CONCURRENT
} CopyOrMarkFromRootsMode;

static void
major_copy_or_mark_from_roots (SgenGrayQueue *gc_thread_gray_queue, size_t *old_next_pin_slot, CopyOrMarkFromRootsMode mode, SgenObjectOperations *object_ops_nopar, SgenObjectOperations *object_ops_par)
{
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	/* FIXME: only use these values for the precise scan
	 * note that to_space pointers should be excluded anyway...
	 */
	char *heap_start = NULL;
	char *heap_end = (char*)-1;
	ScanCopyContext ctx = CONTEXT_FROM_OBJECT_OPERATIONS (object_ops_nopar, gc_thread_gray_queue);
	gboolean concurrent = mode != COPY_OR_MARK_FROM_ROOTS_SERIAL;

	SGEN_ASSERT (0, !!concurrent == !!sgen_concurrent_collection_in_progress, "We've been called with the wrong mode.");

	if (mode == COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT) {
		/*This cleans up unused fragments */
		sgen_nursery_allocator_prepare_for_pinning ();

		if (do_concurrent_checks)
			sgen_debug_check_nursery_is_clean ();
	} else {
		/* The concurrent collector doesn't touch the nursery. */
		sgen_nursery_alloc_prepare_for_major ();
	}

	TV_GETTIME (atv);

	/* Pinning depends on this */
	sgen_clear_nursery_fragments ();

	if (whole_heap_check_before_collection)
		sgen_check_whole_heap (TRUE);

	TV_GETTIME (btv);
	time_major_pre_collection_fragment_clear += TV_ELAPSED (atv, btv);

	objects_pinned = 0;

	sgen_client_pre_collection_checks ();

	if (mode != COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT) {
		/* Remsets are not useful for a major collection */
		remset.clear_cards ();
	}

	sgen_process_fin_stage_entries ();

	TV_GETTIME (atv);
	sgen_init_pinning ();
	if (mode == COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT)
		sgen_init_pinning_for_conc ();
	SGEN_LOG (6, "Collecting pinned addresses");
	pin_from_roots ((void*)lowest_heap_address, (void*)highest_heap_address, ctx);
	if (mode == COPY_OR_MARK_FROM_ROOTS_FINISH_CONCURRENT) {
		/* Pin cemented objects that were forced */
		sgen_pin_cemented_objects ();
	}
	sgen_optimize_pin_queue ();
	if (mode == COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT) {
		/*
		 * Cemented objects that are in the pinned list will be marked. When
		 * marking concurrently we won't mark mod-union cards for these objects.
		 * Instead they will remain cemented until the next major collection,
		 * when we will recheck if they are still pinned in the roots.
		 */
		sgen_cement_force_pinned ();
	}

	/*
	 * pin_queue now contains all candidate pointers, sorted and
	 * uniqued.  We must do two passes now to figure out which
	 * objects are pinned.
	 *
	 * The first is to find within the pin_queue the area for each
	 * section.  This requires that the pin_queue be sorted.  We
	 * also process the LOS objects and pinned chunks here.
	 *
	 * The second, destructive, pass is to reduce the section
	 * areas to pointers to the actually pinned objects.
	 */
	SGEN_LOG (6, "Pinning from sections");
	/* first pass for the sections */
	sgen_find_section_pin_queue_start_end (sgen_nursery_section);
	/* identify possible pointers to the insize of large objects */
	SGEN_LOG (6, "Pinning from large objects");
	sgen_los_pin_objects (gc_thread_gray_queue, mode == COPY_OR_MARK_FROM_ROOTS_FINISH_CONCURRENT);

	pin_objects_in_nursery (mode == COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT, ctx);

	sgen_major_collector.pin_objects (gc_thread_gray_queue);
	if (old_next_pin_slot)
		*old_next_pin_slot = sgen_get_pinned_count ();

	TV_GETTIME (btv);
	time_major_pinning += TV_ELAPSED (atv, btv);
	SGEN_LOG (2, "Finding pinned pointers: %" G_GSIZE_FORMAT "d in %" PRId64 " usecs", sgen_get_pinned_count (), (gint64)(TV_ELAPSED (atv, btv) / 10));
	SGEN_LOG (4, "Start scan with %" G_GSIZE_FORMAT "d pinned objects", sgen_get_pinned_count ());
	sgen_client_pinning_end ();

	if (mode == COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT)
		sgen_finish_pinning_for_conc ();

	sgen_major_collector.init_to_space ();

	SGEN_ASSERT (0, sgen_workers_all_done (), "Why are the workers not done when we start or finish a major collection?");
	if (mode == COPY_OR_MARK_FROM_ROOTS_FINISH_CONCURRENT) {
		if (object_ops_par != NULL)
			sgen_workers_set_num_active_workers (GENERATION_OLD, 0);
		if (object_ops_par == NULL && sgen_workers_have_idle_work (GENERATION_OLD)) {
			/*
			 * We force the finish of the worker with the new object ops context
			 * which can also do copying. We need to have finished pinning. On the
			 * parallel collector, there is no need to drain the private queues
			 * here, since we can do it as part of the finishing work, achieving
			 * better work distribution.
			 */
			sgen_workers_start_all_workers (GENERATION_OLD, object_ops_nopar, object_ops_par, NULL);

			sgen_workers_join (GENERATION_OLD);
		}
	}

#ifdef SGEN_DEBUG_INTERNAL_ALLOC
	main_gc_thread = mono_native_thread_self ();
#endif

	TV_GETTIME (atv);
	time_major_scan_pinned += TV_ELAPSED (btv, atv);

	enqueue_scan_from_roots_jobs (gc_thread_gray_queue, heap_start, heap_end, object_ops_nopar, FALSE);

	TV_GETTIME (btv);
	time_major_scan_roots += TV_ELAPSED (atv, btv);

	/*
	 * We start the concurrent worker after pinning and after we scanned the roots
	 * in order to make sure that the worker does not finish before handling all
	 * the roots.
	 */
	if (mode == COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT) {
		sgen_workers_set_num_active_workers (GENERATION_OLD, 1);
		gray_queue_redirect (gc_thread_gray_queue);
		if (precleaning_enabled) {
			sgen_workers_start_all_workers (GENERATION_OLD, object_ops_nopar, object_ops_par, workers_finish_callback);
		} else {
			sgen_workers_start_all_workers (GENERATION_OLD, object_ops_nopar, object_ops_par, NULL);
		}
	}

	if (mode == COPY_OR_MARK_FROM_ROOTS_FINISH_CONCURRENT) {
		int i, split_count = sgen_workers_get_job_split_count (GENERATION_OLD);
		size_t num_major_sections = sgen_major_collector.get_num_major_sections ();
		gboolean parallel = object_ops_par != NULL;

		/* If we're not parallel we finish the collection on the gc thread */
		if (parallel)
			gray_queue_redirect (gc_thread_gray_queue);

		/* Mod union card table */
		for (i = 0; i < split_count; i++) {
			ParallelScanJob *psj;

			psj = (ParallelScanJob*)sgen_thread_pool_job_alloc ("scan mod union cardtable", job_scan_major_mod_union_card_table, sizeof (ParallelScanJob));
			psj->scan_job.ops = parallel ? NULL : object_ops_nopar;
			psj->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
			psj->job_index = i;
			psj->job_split_count = split_count;
			psj->data = num_major_sections / split_count;
			sgen_workers_enqueue_job (GENERATION_OLD, &psj->scan_job.job, parallel);

			psj = (ParallelScanJob*)sgen_thread_pool_job_alloc ("scan LOS mod union cardtable", job_scan_los_mod_union_card_table, sizeof (ParallelScanJob));
			psj->scan_job.ops = parallel ? NULL : object_ops_nopar;
			psj->scan_job.gc_thread_gray_queue = gc_thread_gray_queue;
			psj->job_index = i;
			psj->job_split_count = split_count;
			sgen_workers_enqueue_job (GENERATION_OLD, &psj->scan_job.job, parallel);
		}

		if (parallel) {
			/*
			 * If we enqueue a job while workers are running we need to sgen_workers_ensure_awake
			 * in order to make sure that we are running the idle func and draining all worker
			 * gray queues. The operation of starting workers implies this, so we start them after
			 * in order to avoid doing this operation twice. The workers will drain the main gray
			 * stack that contained roots and pinned objects and also scan the mod union card
			 * table.
			 */
			sgen_workers_start_all_workers (GENERATION_OLD, object_ops_nopar, object_ops_par, NULL);
			sgen_workers_join (GENERATION_OLD);
		}
	}

	sgen_pin_stats_report ();

	if (mode == COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT) {
		sgen_finish_pinning ();

		sgen_pin_stats_reset ();

		if (do_concurrent_checks)
			sgen_debug_check_nursery_is_clean ();
	}
}

static void
major_start_collection (SgenGrayQueue *gc_thread_gray_queue, const char *reason, gboolean concurrent, size_t *old_next_pin_slot)
{
	SgenObjectOperations *object_ops_nopar, *object_ops_par = NULL;

#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	if (concurrent) {
		g_assert (sgen_major_collector.is_concurrent);
		sgen_concurrent_collection_in_progress = TRUE;
	}
#endif

	sgen_binary_protocol_collection_begin (mono_atomic_load_i32 (&mono_gc_stats.major_gc_count), GENERATION_OLD);

	sgen_current_collection_generation = GENERATION_OLD;

	sgen_workers_assert_gray_queue_is_empty (GENERATION_OLD);

	if (!concurrent)
		sgen_cement_reset ();

	if (concurrent) {
		object_ops_nopar = &sgen_major_collector.major_ops_concurrent_start;
		if (sgen_major_collector.is_parallel)
			object_ops_par = &sgen_major_collector.major_ops_conc_par_start;

	} else {
		object_ops_nopar = &sgen_major_collector.major_ops_serial;
	}

	reset_pinned_from_failed_allocation ();

	sgen_memgov_major_collection_start (concurrent, reason);

	//count_ref_nonref_objs ();
	//consistency_check ();

	check_scan_starts ();

	sgen_degraded_mode = 0;
	SGEN_LOG (1, "Start major collection %" G_GINT32_FORMAT, mono_atomic_load_i32 (&mono_gc_stats.major_gc_count));
	mono_atomic_inc_i32 (&mono_gc_stats.major_gc_count);

	if (sgen_major_collector.start_major_collection)
		sgen_major_collector.start_major_collection ();

	major_copy_or_mark_from_roots (gc_thread_gray_queue, old_next_pin_slot, concurrent ? COPY_OR_MARK_FROM_ROOTS_START_CONCURRENT : COPY_OR_MARK_FROM_ROOTS_SERIAL, object_ops_nopar, object_ops_par);
}

static void
major_finish_collection (SgenGrayQueue *gc_thread_gray_queue, const char *reason, gboolean is_overflow, size_t old_next_pin_slot, gboolean forced)
{
	ScannedObjectCounts counts;
	SgenObjectOperations *object_ops_nopar;
	mword fragment_total;
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	guint64 major_scan_start = time_major_scan_mod_union_blocks;
	guint64 los_scan_start = time_major_scan_mod_union_los;
	guint64 finish_gray_start = time_major_finish_gray_stack;

	if (sgen_concurrent_collection_in_progress) {
		SgenObjectOperations *object_ops_par = NULL;

		object_ops_nopar = &sgen_major_collector.major_ops_concurrent_finish;
		if (sgen_major_collector.is_parallel)
			object_ops_par = &sgen_major_collector.major_ops_conc_par_finish;

		major_copy_or_mark_from_roots (gc_thread_gray_queue, NULL, COPY_OR_MARK_FROM_ROOTS_FINISH_CONCURRENT, object_ops_nopar, object_ops_par);

#ifdef SGEN_DEBUG_INTERNAL_ALLOC
		main_gc_thread = NULL;
#endif
	} else {
		object_ops_nopar = &sgen_major_collector.major_ops_serial;
	}

	sgen_workers_assert_gray_queue_is_empty (GENERATION_OLD);

	TV_GETTIME (btv);
	finish_gray_stack (GENERATION_OLD, CONTEXT_FROM_OBJECT_OPERATIONS (object_ops_nopar, gc_thread_gray_queue));
	TV_GETTIME (atv);
	time_major_finish_gray_stack += TV_ELAPSED (btv, atv);

	SGEN_ASSERT (0, sgen_workers_all_done (), "Can't have workers working after joining");

	if (objects_pinned) {
		g_assert (!sgen_concurrent_collection_in_progress);

		/*
		 * This is slow, but we just OOM'd.
		 *
		 * See comment at `sgen_pin_queue_clear_discarded_entries` for how the pin
		 * queue is laid out at this point.
		 */
		sgen_pin_queue_clear_discarded_entries (sgen_nursery_section, old_next_pin_slot);
		/*
		 * We need to reestablish all pinned nursery objects in the pin queue
		 * because they're needed for fragment creation.  Unpinning happens by
		 * walking the whole queue, so it's not necessary to reestablish where major
		 * heap block pins are - all we care is that they're still in there
		 * somewhere.
		 */
		sgen_optimize_pin_queue ();
		sgen_find_section_pin_queue_start_end (sgen_nursery_section);
		objects_pinned = 0;
	}

	reset_heap_boundaries ();
	sgen_update_heap_boundaries ((mword)sgen_get_nursery_start (), (mword)sgen_get_nursery_end ());

	/*
	 * We collect the roots before unpinning objects in the nursery since we need to have
	 * object liveness information for ephemeron root reporting.
	 */
	sgen_client_collecting_major_report_roots (&fin_ready_queue, &critical_fin_queue);

	/* walk the pin_queue, build up the fragment list of free memory, unmark
	 * pinned objects as we go, memzero() the empty fragments so they are ready for the
	 * next allocations.
	 */
	fragment_total = sgen_build_nursery_fragments (sgen_nursery_section);
	if (!fragment_total)
		sgen_degraded_mode = 1;
	SGEN_LOG (4, "Free space in nursery after major %ld", (long)fragment_total);

	if (do_concurrent_checks && sgen_concurrent_collection_in_progress)
		sgen_debug_check_nursery_is_clean ();
	if (check_nursery_objects_untag)
		sgen_check_nursery_objects_untag ();

	/* prepare the pin queue for the next collection */
	sgen_finish_pinning ();

	/* Clear TLABs for all threads */
	sgen_clear_tlabs ();

	sgen_pin_stats_reset ();

	sgen_cement_clear_below_threshold ();

	if (check_mark_bits_after_major_collection)
		sgen_check_heap_marked (sgen_concurrent_collection_in_progress);

	TV_GETTIME (btv);
	time_major_fragment_creation += TV_ELAPSED (atv, btv);

	sgen_binary_protocol_sweep_begin (GENERATION_OLD, !sgen_major_collector.sweeps_lazily);
	sgen_memgov_major_pre_sweep ();

	TV_GETTIME (atv);
	time_major_free_bigobjs += TV_ELAPSED (btv, atv);

	sgen_los_sweep ();

	TV_GETTIME (btv);
	time_major_los_sweep += TV_ELAPSED (atv, btv);

	sgen_major_collector.sweep ();

	sgen_binary_protocol_sweep_end (GENERATION_OLD, !sgen_major_collector.sweeps_lazily);

	TV_GETTIME (atv);
	time_major_sweep += TV_ELAPSED (btv, atv);

	sgen_debug_dump_heap ("major", mono_atomic_load_i32 (&mono_gc_stats.major_gc_count) - 1, reason);

	if (sgen_have_pending_finalizers ()) {
		SGEN_LOG (4, "Finalizer-thread wakeup");
		sgen_client_finalize_notify ();
	}

	sgen_memgov_major_collection_end (forced, sgen_concurrent_collection_in_progress, reason, is_overflow);
	sgen_current_collection_generation = -1;

	memset (&counts, 0, sizeof (ScannedObjectCounts));
	sgen_major_collector.finish_major_collection (&counts);

	sgen_workers_assert_gray_queue_is_empty (GENERATION_OLD);

	SGEN_ASSERT (0, sgen_workers_all_done (), "Can't have workers working after major collection has finished");

	check_scan_starts ();

	sgen_binary_protocol_flush_buffers (FALSE);

	//consistency_check ();
	if (sgen_major_collector.is_parallel)
                sgen_binary_protocol_collection_end_stats (0, 0, time_major_finish_gray_stack - finish_gray_start);
        else
                sgen_binary_protocol_collection_end_stats (
                        time_major_scan_mod_union_blocks - major_scan_start,
                        time_major_scan_mod_union_los - los_scan_start,
                        time_major_finish_gray_stack - finish_gray_start);

	sgen_binary_protocol_collection_end (mono_atomic_load_i32 (&mono_gc_stats.major_gc_count) - 1, GENERATION_OLD, counts.num_scanned_objects, counts.num_unique_scanned_objects);

#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	if (sgen_concurrent_collection_in_progress)
		sgen_concurrent_collection_in_progress = FALSE;
#endif
}

static gboolean
major_do_collection (const char *reason, gboolean is_overflow, gboolean forced)
{
	TV_DECLARE (time_start);
	TV_DECLARE (time_end);
	size_t old_next_pin_slot;
	SgenGrayQueue gc_thread_gray_queue;

	if (disable_major_collections)
		return FALSE;

	if (sgen_major_collector.get_and_reset_num_major_objects_marked) {
		long long num_marked = sgen_major_collector.get_and_reset_num_major_objects_marked ();
		g_assert (!num_marked);
	}

	/* world must be stopped already */
	TV_GETTIME (time_start);

	init_gray_queue (&gc_thread_gray_queue);
	major_start_collection (&gc_thread_gray_queue, reason, FALSE, &old_next_pin_slot);
	major_finish_collection (&gc_thread_gray_queue, reason, is_overflow, old_next_pin_slot, forced);
	sgen_gray_object_queue_dispose (&gc_thread_gray_queue);

	TV_GETTIME (time_end);
	UnlockedAdd64 (&mono_gc_stats.major_gc_time, TV_ELAPSED (time_start, time_end));

	/* FIXME: also report this to the user, preferably in gc-end. */
	if (sgen_major_collector.get_and_reset_num_major_objects_marked)
		sgen_major_collector.get_and_reset_num_major_objects_marked ();

	return bytes_pinned_from_failed_allocation > 0;
}

static void
major_start_concurrent_collection (const char *reason)
{
	TV_DECLARE (time_start);
	TV_DECLARE (time_end);
	long long num_objects_marked;
	SgenGrayQueue gc_thread_gray_queue;

	if (disable_major_collections)
		return;

	TV_GETTIME (time_start);
	SGEN_TV_GETTIME (time_major_conc_collection_start);

	num_objects_marked = sgen_major_collector.get_and_reset_num_major_objects_marked ();
	g_assert (num_objects_marked == 0);

	sgen_binary_protocol_concurrent_start ();

	init_gray_queue (&gc_thread_gray_queue);
	// FIXME: store reason and pass it when finishing
	major_start_collection (&gc_thread_gray_queue, reason, TRUE, NULL);
	sgen_gray_object_queue_dispose (&gc_thread_gray_queue);

	num_objects_marked = sgen_major_collector.get_and_reset_num_major_objects_marked ();

	TV_GETTIME (time_end);
	UnlockedAdd64 (&mono_gc_stats.major_gc_time, TV_ELAPSED (time_start, time_end));

	sgen_current_collection_generation = -1;
}

/*
 * Returns whether the major collection has finished.
 */
static gboolean
major_should_finish_concurrent_collection (void)
{
	return sgen_workers_all_done ();
}

static void
major_update_concurrent_collection (void)
{
	TV_DECLARE (total_start);
	TV_DECLARE (total_end);

	TV_GETTIME (total_start);

	sgen_binary_protocol_concurrent_update ();

	sgen_major_collector.update_cardtable_mod_union ();
	sgen_los_update_cardtable_mod_union ();

	TV_GETTIME (total_end);
	UnlockedAdd64 (&mono_gc_stats.major_gc_time, TV_ELAPSED (total_start, total_end));
}

static void
major_finish_concurrent_collection (gboolean forced)
{
	SgenGrayQueue gc_thread_gray_queue;
	TV_DECLARE (total_start);
	TV_DECLARE (total_end);

	TV_GETTIME (total_start);

	sgen_binary_protocol_concurrent_finish ();

	/*
	 * We need to stop all workers since we're updating the cardtable below.
	 * The workers will be resumed with a finishing pause context to avoid
	 * additional cardtable and object scanning.
	 */
	sgen_workers_stop_all_workers (GENERATION_OLD);

	SGEN_TV_GETTIME (time_major_conc_collection_end);
	UnlockedAdd64 (&mono_gc_stats.major_gc_time_concurrent, SGEN_TV_ELAPSED (time_major_conc_collection_start, time_major_conc_collection_end));

	sgen_major_collector.update_cardtable_mod_union ();
	sgen_los_update_cardtable_mod_union ();

	if (mod_union_consistency_check)
		sgen_check_mod_union_consistency ();

	sgen_current_collection_generation = GENERATION_OLD;
	sgen_cement_reset ();
	init_gray_queue (&gc_thread_gray_queue);
	major_finish_collection (&gc_thread_gray_queue, "finishing", FALSE, -1, forced);
	sgen_gray_object_queue_dispose (&gc_thread_gray_queue);

	TV_GETTIME (total_end);
	UnlockedAdd64 (&mono_gc_stats.major_gc_time, TV_ELAPSED (total_start, total_end));

	sgen_current_collection_generation = -1;
}

/*
 * Ensure an allocation request for @size will succeed by freeing enough memory.
 *
 * LOCKING: The GC lock MUST be held.
 */
void
sgen_ensure_free_space (size_t size, int generation)
{
	int generation_to_collect = -1;
	const char *reason = NULL;
	gboolean forced = FALSE;

	if (generation == GENERATION_OLD) {
		if (sgen_need_major_collection (size, &forced)) {
			reason = "LOS overflow";
			generation_to_collect = GENERATION_OLD;
		}
	} else {
		if (sgen_degraded_mode) {
			if (sgen_need_major_collection (size, &forced)) {
				reason = "Degraded mode overflow";
				generation_to_collect = GENERATION_OLD;
			}
		} else if (sgen_need_major_collection (size, &forced)) {
			reason = sgen_concurrent_collection_in_progress ? "Forced finish concurrent collection" : "Minor allowance";
			generation_to_collect = GENERATION_OLD;
		} else {
			generation_to_collect = GENERATION_NURSERY;
			reason = "Nursery full";                        
		}
	}

	if (generation_to_collect == -1) {
		if (sgen_concurrent_collection_in_progress && sgen_workers_all_done ()) {
			generation_to_collect = GENERATION_OLD;
			reason = "Finish concurrent collection";
		}
	}

	if (generation_to_collect == -1)
		return;
	sgen_perform_collection (size, generation_to_collect, reason, forced, TRUE);
}

/*
 * LOCKING: Assumes the GC lock is held.
 */
static void
sgen_perform_collection_inner (size_t requested_size, int generation_to_collect, const char *reason, gboolean forced_serial, gboolean stw)
{
	TV_DECLARE (gc_total_start);
	TV_DECLARE (gc_total_end);
	int overflow_generation_to_collect = -1;
	int oldest_generation_collected = generation_to_collect;
	const char *overflow_reason = NULL;
	gboolean finish_concurrent = sgen_concurrent_collection_in_progress && (major_should_finish_concurrent_collection () || generation_to_collect == GENERATION_OLD);

	sgen_binary_protocol_collection_requested (generation_to_collect, requested_size, forced_serial ? 1 : 0);

	SGEN_ASSERT (0, generation_to_collect == GENERATION_NURSERY || generation_to_collect == GENERATION_OLD, "What generation is this?");

	if (stw)
		sgen_stop_world (generation_to_collect, forced_serial || !sgen_major_collector.is_concurrent);
	else
		SGEN_ASSERT (0, sgen_is_world_stopped (), "We can only collect if the world is stopped");
		

	TV_GETTIME (gc_total_start);

	// FIXME: extract overflow reason
	// FIXME: minor overflow for concurrent case
	if (generation_to_collect == GENERATION_NURSERY && !finish_concurrent) {
		if (sgen_concurrent_collection_in_progress)
			major_update_concurrent_collection ();

		if (collect_nursery (reason, FALSE) && !sgen_concurrent_collection_in_progress) {
			overflow_generation_to_collect = GENERATION_OLD;
			overflow_reason = "Minor overflow";
		}
	} else if (finish_concurrent) {
		major_finish_concurrent_collection (forced_serial);
		oldest_generation_collected = GENERATION_OLD;
		if (forced_serial && generation_to_collect == GENERATION_OLD)
			major_do_collection (reason, FALSE, TRUE);
	} else {
		SGEN_ASSERT (0, generation_to_collect == GENERATION_OLD, "We should have handled nursery collections above");
		if (sgen_major_collector.is_concurrent && !forced_serial) {
			collect_nursery ("Concurrent start", FALSE);
			major_start_concurrent_collection (reason);
			oldest_generation_collected = GENERATION_NURSERY;
		} else if (major_do_collection (reason, FALSE, forced_serial)) {
			overflow_generation_to_collect = GENERATION_NURSERY;
			overflow_reason = "Excessive pinning";
		}
	}

	if (overflow_generation_to_collect != -1) {
		SGEN_ASSERT (0, !sgen_concurrent_collection_in_progress, "We don't yet support overflow collections with the concurrent collector");

		/*
		 * We need to do an overflow collection, either because we ran out of memory
		 * or the nursery is fully pinned.
		 */

		if (overflow_generation_to_collect == GENERATION_NURSERY)
			collect_nursery (overflow_reason, TRUE);
		else
			major_do_collection (overflow_reason, TRUE, forced_serial);

		oldest_generation_collected = MAX (oldest_generation_collected, overflow_generation_to_collect);
	}

	SGEN_LOG (2, "Heap size: %lu, LOS size: %lu", (unsigned long)sgen_gc_get_total_heap_allocation (), (unsigned long)sgen_los_memory_usage);

	/* this also sets the proper pointers for the next allocation */
	if (generation_to_collect == GENERATION_NURSERY && !sgen_can_alloc_size (requested_size)) {
		/* TypeBuilder and MonoMethod are killing mcs with fragmentation */
		SGEN_LOG (1, "nursery collection didn't find enough room for %" G_GSIZE_FORMAT "d alloc (%" G_GSIZE_FORMAT "d pinned)", requested_size, sgen_get_pinned_count ());
		sgen_dump_pin_queue ();
		sgen_degraded_mode = 1;
	}

	TV_GETTIME (gc_total_end);

	time_last = TV_ELAPSED (gc_total_start, gc_total_end);

	// Long running timers start/stop on different threads might not be correct an all platforms
	// using mono_100ns_ticks, use mono_100ns_datetime for long running timers.
	gint64 timestamp_last_end = mono_100ns_datetime ();

	// Elapsed time since ending last GC.
	time_since_last = TV_ELAPSED (timestamp_last_start, timestamp_last_end);

	timestamp_last_start = timestamp_last_end;

	time_max = MAX (time_max, time_last);

	if (stw)
		sgen_restart_world (oldest_generation_collected, forced_serial || !sgen_major_collector.is_concurrent);
}

#ifdef HOST_WASM

typedef struct {
	size_t requested_size;
	int generation_to_collect;
	const char *reason;
} SgenGcRequest;

static SgenGcRequest gc_request;

#include <emscripten.h>

static void
gc_pump_callback (void)
{
	sgen_perform_collection_inner (gc_request.requested_size, gc_request.generation_to_collect, gc_request.reason, TRUE, TRUE);
	gc_request.generation_to_collect = 0;
}
#endif

#ifdef HOST_WASM
extern gboolean mono_wasm_enable_gc;
#endif

void
sgen_perform_collection (size_t requested_size, int generation_to_collect, const char *reason, gboolean forced_serial, gboolean stw)
{
#ifdef HOST_WASM
	if (!mono_wasm_enable_gc) {
		g_assert (stw); //can't handle non-stw mode (IE, domain unload)
		//we ignore forced_serial

		//There's a window for racing where we're executing other bg jobs before the GC, they trigger a GC request and it overrides this one.
		//I belive this case to be benign as it will, in the worst case, upgrade a minor to a major collection.
		if (gc_request.generation_to_collect <= generation_to_collect) {
			gc_request.requested_size = requested_size;
			gc_request.generation_to_collect = generation_to_collect;
			gc_request.reason = reason;
			sgen_client_schedule_background_job (gc_pump_callback);
		}

		sgen_degraded_mode = 1; //enable degraded mode so allocation can continue
		return;
	}
#endif

	sgen_perform_collection_inner (requested_size, generation_to_collect, reason, forced_serial, stw);
}
/*
 * ######################################################################
 * ########  Memory allocation from the OS
 * ######################################################################
 * This section of code deals with getting memory from the OS and
 * allocating memory for GC-internal data structures.
 * Internal memory can be handled with a freelist for small objects.
 */

/*
 * Debug reporting.
 */
G_GNUC_UNUSED static void
report_internal_mem_usage (void)
{
	printf ("Internal memory usage:\n");
	sgen_report_internal_mem_usage ();
	printf ("Pinned memory usage:\n");
	sgen_major_collector.report_pinned_memory_usage ();
}

/*
 * ######################################################################
 * ########  Finalization support
 * ######################################################################
 */

/*
 * This function returns true if @object is either alive and belongs to the
 * current collection - major collections are full heap, so old gen objects
 * are never alive during a minor collection.
 */
static int
sgen_is_object_alive_and_on_current_collection (GCObject *object)
{
	if (ptr_in_nursery (object))
		return sgen_nursery_is_object_alive (object);

	if (sgen_current_collection_generation == GENERATION_NURSERY)
		return FALSE;

	return sgen_major_is_object_alive (object);
}


gboolean
sgen_gc_is_object_ready_for_finalization (GCObject *object)
{
	return !sgen_is_object_alive (object);
}

void
sgen_queue_finalization_entry (GCObject *obj)
{
	gboolean critical = sgen_client_object_has_critical_finalizer (obj);

	sgen_pointer_queue_add (critical ? &critical_fin_queue : &fin_ready_queue, obj);

	sgen_client_object_queued_for_finalization (obj);
}

gboolean
sgen_object_is_live (GCObject *obj)
{
	return sgen_is_object_alive_and_on_current_collection (obj);
}

/*
 * `System.GC.WaitForPendingFinalizers` first checks `sgen_have_pending_finalizers()` to
 * determine whether it can exit quickly.  The latter must therefore only return FALSE if
 * all finalizers have really finished running.
 *
 * `sgen_gc_invoke_finalizers()` first dequeues a finalizable object, and then finalizes it.
 * This means that just checking whether the queues are empty leaves the possibility that an
 * object might have been dequeued but not yet finalized.  That's why we need the additional
 * flag `pending_unqueued_finalizer`.
 */

static volatile gboolean pending_unqueued_finalizer = FALSE;
volatile gboolean sgen_suspend_finalizers = FALSE;

void
sgen_set_suspend_finalizers (void)
{
	sgen_suspend_finalizers = TRUE;
}

int
sgen_gc_invoke_finalizers (void)
{
	int count = 0;

	g_assert (!pending_unqueued_finalizer);

	/* FIXME: batch to reduce lock contention */
	while (sgen_have_pending_finalizers ()) {
		GCObject *obj;

		LOCK_GC;

		/*
		 * We need to set `pending_unqueued_finalizer` before dequeing the
		 * finalizable object.
		 */
		if (!sgen_pointer_queue_is_empty (&fin_ready_queue)) {
			pending_unqueued_finalizer = TRUE;
			mono_memory_write_barrier ();
			obj = (GCObject *)sgen_pointer_queue_pop (&fin_ready_queue);
		} else if (!sgen_pointer_queue_is_empty (&critical_fin_queue)) {
			pending_unqueued_finalizer = TRUE;
			mono_memory_write_barrier ();
			obj = (GCObject *)sgen_pointer_queue_pop (&critical_fin_queue);
		} else {
			obj = NULL;
		}

		if (obj)
			SGEN_LOG (7, "Finalizing object %p (%s)", obj, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (obj)));

		UNLOCK_GC;

		if (!obj)
			break;

		count++;
		/* the object is on the stack so it is pinned */
		/*g_print ("Calling finalizer for object: %p (%s)\n", obj, sgen_client_object_safe_name (obj));*/
		sgen_client_run_finalize (obj);
	}

	if (pending_unqueued_finalizer) {
		mono_memory_write_barrier ();
		pending_unqueued_finalizer = FALSE;
	}

	return count;
}

gboolean
sgen_have_pending_finalizers (void)
{
	if (sgen_suspend_finalizers)
		return FALSE;
	return pending_unqueued_finalizer || !sgen_pointer_queue_is_empty (&fin_ready_queue) || !sgen_pointer_queue_is_empty (&critical_fin_queue);
}

/*
 * ######################################################################
 * ########  registered roots support
 * ######################################################################
 */

/*
 * We do not coalesce roots.
 */
int
sgen_register_root (char *start, size_t size, SgenDescriptor descr, int root_type, MonoGCRootSource source, void *key, const char *msg)
{
	RootRecord new_root;
	int i;

	sgen_client_root_registered (start, size, source, key, msg);

	LOCK_GC;
	for (i = 0; i < ROOT_TYPE_NUM; ++i) {
		RootRecord *root = (RootRecord *)sgen_hash_table_lookup (&sgen_roots_hash [i], start);
		/* we allow changing the size and the descriptor (for thread statics etc) */
		if (root) {
			size_t old_size = root->end_root - start;
			root->end_root = start + size;
			SGEN_ASSERT (0, !!root->root_desc == !!descr, "Can't change whether a root is precise or conservative.");
			SGEN_ASSERT (0, root->source == source, "Can't change a root's source identifier.");
			SGEN_ASSERT (0, !!root->msg == !!msg, "Can't change a root's message.");
			root->root_desc = descr;
			roots_size += size;
			roots_size -= old_size;
			UNLOCK_GC;
			return TRUE;
		}
	}

	new_root.end_root = start + size;
	new_root.root_desc = descr;
	new_root.source = source;
	new_root.msg = msg;

	sgen_hash_table_replace (&sgen_roots_hash [root_type], start, &new_root, NULL);
	roots_size += size;

	SGEN_LOG (3, "Added root for range: %p-%p, descr: %" PRIx64 "  (%d/%d bytes)", start, new_root.end_root, (gint64)descr, (int)size, (int)roots_size);

	UNLOCK_GC;
	return TRUE;
}

void
sgen_deregister_root (char* addr)
{
	int root_type;
	RootRecord root;

	sgen_client_root_deregistered (addr);

	LOCK_GC;
	for (root_type = 0; root_type < ROOT_TYPE_NUM; ++root_type) {
		if (sgen_hash_table_remove (&sgen_roots_hash [root_type], addr, &root))
			roots_size -= (root.end_root - addr);
	}
	UNLOCK_GC;
}

void
sgen_wbroots_iterate_live_block_ranges (sgen_cardtable_block_callback cb)
{
	void **start_root;
	RootRecord *root;
	SGEN_HASH_TABLE_FOREACH (&sgen_roots_hash [ROOT_TYPE_WBARRIER], void **, start_root, RootRecord *, root) {
		cb ((mword)start_root, (mword)root->end_root - (mword)start_root);
	} SGEN_HASH_TABLE_FOREACH_END;
}

static void
job_wbroots_iterate_live_block_ranges (void *worker_data_untyped, SgenThreadPoolJob *job)
{
	ParallelIterateBlockRangesJob *job_data = (ParallelIterateBlockRangesJob*)job;

	// Currently we only iterate live wbroots block ranges on one job.
	if (job_data->job_index == 0)
		sgen_wbroots_iterate_live_block_ranges (job_data->callback);
}

/* Root equivalent of sgen_client_cardtable_scan_object */
static void
sgen_wbroot_scan_card_table (void** start_root, mword size,  ScanCopyContext ctx)
{
	ScanPtrFieldFunc scan_field_func = ctx.ops->scan_ptr_field;
	guint8 *card_data = sgen_card_table_get_card_scan_address ((mword)start_root);
	guint8 *card_base = card_data;
	mword card_count = sgen_card_table_number_of_cards_in_range ((mword)start_root, size);
	guint8 *card_data_end = card_data + card_count;
	mword extra_idx = 0;
	char *obj_start = (char*)sgen_card_table_align_pointer (start_root);
	char *obj_end = (char*)start_root + size;
#ifdef SGEN_HAVE_OVERLAPPING_CARDS
	guint8 *overflow_scan_end = NULL;
#endif

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
	/*Check for overflow and if so, setup to scan in two steps*/
	if (card_data_end >= SGEN_SHADOW_CARDTABLE_END) {
		overflow_scan_end = sgen_shadow_cardtable + (card_data_end - SGEN_SHADOW_CARDTABLE_END);
		card_data_end = SGEN_SHADOW_CARDTABLE_END;
	}

LOOP_HEAD:
#endif

	card_data = sgen_find_next_card (card_data, card_data_end);

	for (; card_data < card_data_end; card_data = sgen_find_next_card (card_data + 1, card_data_end)) {
		size_t idx = (card_data - card_base) + extra_idx;
		char *start = (char*)(obj_start + idx * CARD_SIZE_IN_BYTES);
		char *card_end = start + CARD_SIZE_IN_BYTES;
		char *elem = start, *first_elem = start;

		/*
		 * Don't clean first and last card on 32bit systems since they
		 * may also be part from other roots.
		 */
		if (card_data != card_base && card_data != (card_data_end - 1))
			sgen_card_table_prepare_card_for_scanning (card_data);

		card_end = MIN (card_end, obj_end);

		if (elem < (char*)start_root)
			first_elem = elem = (char*)start_root;

		for (; elem < card_end; elem += SIZEOF_VOID_P) {
			if (*(GCObject**)elem)
				scan_field_func (NULL, (GCObject**)elem, ctx.queue);
		}

		sgen_binary_protocol_card_scan (first_elem, elem - first_elem);
	}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
	if (overflow_scan_end) {
		extra_idx = card_data - card_base;
		card_base = card_data = sgen_shadow_cardtable;
		card_data_end = overflow_scan_end;
		overflow_scan_end = NULL;
		goto LOOP_HEAD;
	}
#endif
}

void
sgen_wbroots_scan_card_table (ScanCopyContext ctx)
{
	void **start_root;
	RootRecord *root;

	SGEN_HASH_TABLE_FOREACH (&sgen_roots_hash [ROOT_TYPE_WBARRIER], void **, start_root, RootRecord *, root) {
		SGEN_ASSERT (0, (root->root_desc & ROOT_DESC_TYPE_MASK) == ROOT_DESC_VECTOR, "Unsupported root type");

		sgen_wbroot_scan_card_table (start_root, (mword)root->end_root - (mword)start_root, ctx);
	} SGEN_HASH_TABLE_FOREACH_END;
}

/*
 * ######################################################################
 * ########  Thread handling (stop/start code)
 * ######################################################################
 */

int
sgen_get_current_collection_generation (void)
{
	return sgen_current_collection_generation;
}

void*
sgen_thread_attach (SgenThreadInfo* info)
{
	info->tlab_start = info->tlab_next = info->tlab_temp_end = info->tlab_real_end = NULL;

	sgen_client_thread_attach (info);

	return info;
}

void
sgen_thread_detach_with_lock (SgenThreadInfo *p)
{
	sgen_client_thread_detach_with_lock (p);
}

/*
 * ######################################################################
 * ########  Write barriers
 * ######################################################################
 */

/*
 * Note: the write barriers first do the needed GC work and then do the actual store:
 * this way the value is visible to the conservative GC scan after the write barrier
 * itself. If a GC interrupts the barrier in the middle, value will be kept alive by
 * the conservative scan, otherwise by the remembered set scan.
 */

/**
 * mono_gc_wbarrier_arrayref_copy_internal:
 */
void
mono_gc_wbarrier_arrayref_copy_internal (gpointer dest_ptr, gconstpointer src_ptr, int count)
{
	HEAVY_STAT (++stat_wbarrier_arrayref_copy);
	/*This check can be done without taking a lock since dest_ptr array is pinned*/
	if (ptr_in_nursery (dest_ptr) || count <= 0) {
		mono_gc_memmove_aligned (dest_ptr, src_ptr, count * sizeof (gpointer));
		return;
	}

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	if (sgen_binary_protocol_is_heavy_enabled ()) {
		int i;
		for (i = 0; i < count; ++i) {
			gpointer dest = (gpointer*)dest_ptr + i;
			gpointer obj = *((gpointer*)src_ptr + i);
			if (obj)
				sgen_binary_protocol_wbarrier (dest, obj, (gpointer)LOAD_VTABLE (obj));
		}
	}
#endif

	remset.wbarrier_arrayref_copy (dest_ptr, src_ptr, count);
}

/**
 * mono_gc_wbarrier_generic_nostore_internal:
 */
void
mono_gc_wbarrier_generic_nostore_internal (gpointer ptr)
{
	gpointer obj;

	HEAVY_STAT (++stat_wbarrier_generic_store);

	sgen_client_wbarrier_generic_nostore_check (ptr);

	obj = *(gpointer*)ptr;
	if (obj)
		sgen_binary_protocol_wbarrier (ptr, obj, (gpointer)LOAD_VTABLE (obj));

	/*
	 * We need to record old->old pointer locations for the
	 * concurrent collector.
	 */
	if (!ptr_in_nursery (obj) && !sgen_concurrent_collection_in_progress) {
		SGEN_LOG (8, "Skipping remset at %p", ptr);
		return;
	}

	SGEN_LOG (8, "Adding remset at %p", ptr);

	remset.wbarrier_generic_nostore (ptr);
}

/**
 * mono_gc_wbarrier_generic_store_internal:
 */
void
mono_gc_wbarrier_generic_store_internal (void volatile* ptr, GCObject* value)
{
	SGEN_LOG (8, "Wbarrier store at %p to %p (%s)", ptr, value, value ? sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (value)) : "null");
	SGEN_UPDATE_REFERENCE_ALLOW_NULL ((void*)ptr, value); // FIXME volatile
	if (ptr_in_nursery (value) || sgen_concurrent_collection_in_progress)
		mono_gc_wbarrier_generic_nostore_internal ((void*)ptr); // FIXME volatile
	sgen_dummy_use (value);
}

/**
 * mono_gc_wbarrier_generic_store_atomic_internal:
 * Same as \c mono_gc_wbarrier_generic_store but performs the store
 * as an atomic operation with release semantics.
 */
void
mono_gc_wbarrier_generic_store_atomic_internal (gpointer ptr, GCObject *value)
{
	HEAVY_STAT (++stat_wbarrier_generic_store_atomic);

	SGEN_LOG (8, "Wbarrier atomic store at %p to %p (%s)", ptr, value, value ? sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (value)) : "null");

	mono_atomic_store_ptr ((volatile gpointer *)ptr, value);

	if (ptr_in_nursery (value) || sgen_concurrent_collection_in_progress)
		mono_gc_wbarrier_generic_nostore_internal (ptr);

	sgen_dummy_use (value);
}

void
sgen_wbarrier_range_copy (gpointer _dest, gconstpointer _src, int size)
{
	remset.wbarrier_range_copy (_dest,_src, size);
}

/*
 * ######################################################################
 * ########  Other mono public interface functions.
 * ######################################################################
 */

void
sgen_gc_collect (int generation)
{
	gboolean forced;

	LOCK_GC;
	if (generation > 1)
		generation = 1;
	sgen_perform_collection (0, generation, "user request", TRUE, TRUE);
	/* Make sure we don't exceed heap size allowance by promoting */
	if (generation == GENERATION_NURSERY && sgen_need_major_collection (0, &forced))
		sgen_perform_collection (0, GENERATION_OLD, "Minor allowance", forced, TRUE);
	UNLOCK_GC;
}

int
sgen_gc_collection_count (int generation)
{
	return mono_atomic_load_i32 (generation == GENERATION_NURSERY ? &mono_gc_stats.minor_gc_count : &mono_gc_stats.major_gc_count);
}

size_t
sgen_gc_get_used_size (void)
{
	gint64 tot = 0;
	LOCK_GC;
	tot = sgen_los_memory_usage;
	tot += sgen_nursery_section->end_data - sgen_nursery_section->data;
	tot += sgen_major_collector.get_used_size ();
	/* FIXME: account for pinned objects */
	UNLOCK_GC;
	return tot;
}

void sgen_gc_get_gctimeinfo (
	guint64 *time_last_gc_100ns,
	guint64 *time_since_last_gc_100ns,
	guint64 *time_max_gc_100ns)
{
	*time_last_gc_100ns = time_last;
	*time_since_last_gc_100ns = time_since_last;
	*time_max_gc_100ns = time_max;
}

void
sgen_env_var_error (const char *env_var, const char *fallback, const char *description_format, ...)
{
	va_list ap;

	va_start (ap, description_format);

	fprintf (stderr, "Warning: In environment variable `%s': ", env_var);
	vfprintf (stderr, description_format, ap);
	if (fallback)
		fprintf (stderr, " - %s", fallback);
	fprintf (stderr, "\n");

	va_end (ap);
}

static gboolean
parse_double_in_interval (const char *env_var, const char *opt_name, const char *opt, double min, double max, double *result)
{
	char *endptr;
	double val = strtod (opt, &endptr);
	if (endptr == opt) {
		sgen_env_var_error (env_var, "Using default value.", "`%s` must be a number.", opt_name);
		return FALSE;
	}
	else if (val < min || val > max) {
		sgen_env_var_error (env_var, "Using default value.", "`%s` must be between %.2f - %.2f.", opt_name, min, max);
		return FALSE;
	}
	*result = val;
	return TRUE;
}

static SgenMinor
parse_sgen_minor (const char *opt)
{
	if (!opt)
		return SGEN_MINOR_DEFAULT;

	if (!strcmp (opt, "simple")) {
		return SGEN_MINOR_SIMPLE;
	} else if (!strcmp (opt, "simple-par")) {
		return SGEN_MINOR_SIMPLE_PARALLEL;
	} else if (!strcmp (opt, "split")) {
		return SGEN_MINOR_SPLIT;
	} else {
		sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default instead.", "Unknown minor collector `%s'.", opt);
		return SGEN_MINOR_DEFAULT;
	}
}

static SgenMajor
parse_sgen_major (const char *opt)
{
	if (!opt)
		return SGEN_MAJOR_DEFAULT;

	if (!strcmp (opt, "marksweep")) {
		return SGEN_MAJOR_SERIAL;
	} else if (!strcmp (opt, "marksweep-conc")) {
		return SGEN_MAJOR_CONCURRENT;
	} else if (!strcmp (opt, "marksweep-conc-par")) {
		return SGEN_MAJOR_CONCURRENT_PARALLEL;
	} else {
		sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default instead.", "Unknown major collector `%s'.", opt);
		return SGEN_MAJOR_DEFAULT;
	}

}

static SgenMode
parse_sgen_mode (const char *opt)
{
	if (!opt)
		return SGEN_MODE_NONE;

	if (!strcmp (opt, "balanced")) {
		return SGEN_MODE_BALANCED;
	} else if (!strcmp (opt, "throughput")) {
		return SGEN_MODE_THROUGHPUT;
	} else if (!strcmp (opt, "pause") || g_str_has_prefix (opt, "pause:")) {
		return SGEN_MODE_PAUSE;
	} else {
		sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default configurations.", "Unknown mode `%s'.", opt);
		return SGEN_MODE_NONE;
	}
}

static void
init_sgen_minor (SgenMinor minor)
{
	switch (minor) {
	case SGEN_MINOR_DEFAULT:
	case SGEN_MINOR_SIMPLE:
		sgen_simple_nursery_init (&sgen_minor_collector, FALSE);
		break;
	case SGEN_MINOR_SIMPLE_PARALLEL:
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
		sgen_simple_nursery_init (&sgen_minor_collector, TRUE);
#else
		g_error ("Sgen was build with concurrent collector disabled");
#endif
		break;
	case SGEN_MINOR_SPLIT:
#ifndef DISABLE_SGEN_SPLIT_NURSERY
		sgen_split_nursery_init (&sgen_minor_collector);
#else
		g_error ("Sgenw as build with split nursery disabled");
#endif
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
init_sgen_major (SgenMajor major)
{
	if (major == SGEN_MAJOR_DEFAULT)
		major = DEFAULT_MAJOR;

	switch (major) {
	case SGEN_MAJOR_SERIAL:
		sgen_marksweep_init (&sgen_major_collector);
		break;
#ifdef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	case SGEN_MAJOR_CONCURRENT:
	case SGEN_MAJOR_CONCURRENT_PARALLEL:
		g_error ("Sgen was build with the concurent collector disabled");
#else
	case SGEN_MAJOR_CONCURRENT:
		sgen_marksweep_conc_init (&sgen_major_collector);
		break;
	case SGEN_MAJOR_CONCURRENT_PARALLEL:
		sgen_marksweep_conc_par_init (&sgen_major_collector);
		break;
#endif
	default:
		g_assert_not_reached ();
	}
}

/*
 * If sgen mode is set, major/minor configuration is fixed. The other gc_params
 * are parsed and processed after major/minor initialization, so it can potentially
 * override some knobs set by the sgen mode. We can consider locking out additional
 * configurations when gc_modes are used.
 */
static void
init_sgen_mode (SgenMode mode)
{
	SgenMinor minor = SGEN_MINOR_DEFAULT;
	SgenMajor major = SGEN_MAJOR_DEFAULT;

	switch (mode) {
	case SGEN_MODE_BALANCED:
		/*
		 * Use a dynamic parallel nursery with a major concurrent collector.
		 * This uses the default values for max pause time and nursery size.
		 */
		minor = SGEN_MINOR_SIMPLE;
		major = SGEN_MAJOR_CONCURRENT;
		dynamic_nursery = TRUE;
		break;
	case SGEN_MODE_THROUGHPUT:
		/*
		 * Use concurrent major to let the mutator do more work. Use a larger
		 * nursery, without pause time constraints, in order to collect more
		 * objects in parallel and avoid repetitive collection tasks (pinning,
		 * root scanning etc)
		 */
		minor = SGEN_MINOR_SIMPLE_PARALLEL;
		major = SGEN_MAJOR_CONCURRENT;
		dynamic_nursery = TRUE;
		sgen_max_pause_time = 0;
		break;
	case SGEN_MODE_PAUSE:
		/*
		 * Use concurrent major and dynamic nursery with a more
		 * aggressive shrinking relative to pause times.
		 */
		minor = SGEN_MINOR_SIMPLE_PARALLEL;
		major = SGEN_MAJOR_CONCURRENT;
		dynamic_nursery = TRUE;
		sgen_max_pause_margin = SGEN_PAUSE_MODE_MAX_PAUSE_MARGIN;
		break;
	default:
		g_assert_not_reached ();
	}

	init_sgen_minor (minor);
	init_sgen_major (major);
}

void
sgen_gc_init (void)
{
	char *env;
	char **opts, **ptr;
	SgenMajor sgen_major = SGEN_MAJOR_DEFAULT;
	SgenMinor sgen_minor = SGEN_MINOR_DEFAULT;
	SgenMode sgen_mode = SGEN_MODE_NONE;
	char *params_opts = NULL;
	char *debug_opts = NULL;
	size_t max_heap = 0;
	size_t soft_limit = 0;
	int result;
	gboolean debug_print_allowance = FALSE;
	double allowance_ratio = 0, save_target = 0;
	gboolean cement_enabled = TRUE;

	do {
		result = mono_atomic_cas_i32 (&gc_initialized, -1, 0);
		switch (result) {
		case 1:
			/* already inited */
			return;
		case -1:
			/* being inited by another thread */
			mono_thread_info_usleep (1000);
			break;
		case 0:
			/* we will init it */
			break;
		default:
			g_assert_not_reached ();
		}
	} while (result != 0);

	SGEN_TV_GETTIME (sgen_init_timestamp);

#ifdef SGEN_WITHOUT_MONO
	mono_thread_smr_init ();
#endif

	mono_coop_mutex_init (&sgen_gc_mutex);

	sgen_gc_debug_file = stderr;

	mono_coop_mutex_init (&sgen_interruption_mutex);

	if ((env = g_getenv (MONO_GC_PARAMS_NAME)) || gc_params_options) {
		params_opts = g_strdup_printf ("%s,%s", gc_params_options ? gc_params_options : "", env ? env : "");
		g_free (env);
	}

	if (params_opts) {
		opts = g_strsplit (params_opts, ",", -1);
		for (ptr = opts; *ptr; ++ptr) {
			char *opt = *ptr;
			if (g_str_has_prefix (opt, "major=")) {
				opt = strchr (opt, '=') + 1;
				sgen_major = parse_sgen_major (opt);
			} else if (g_str_has_prefix (opt, "minor=")) {
				opt = strchr (opt, '=') + 1;
				sgen_minor = parse_sgen_minor (opt);
			} else if (g_str_has_prefix (opt, "mode=")) {
				opt = strchr (opt, '=') + 1;
				sgen_mode = parse_sgen_mode (opt);
			}
		}
	} else {
		opts = NULL;
	}

	init_stats ();
	sgen_init_internal_allocator ();
	sgen_init_nursery_allocator ();
	sgen_init_fin_weak_hash ();
	sgen_init_hash_table ();
	sgen_init_descriptors ();
	sgen_init_gray_queues ();
	sgen_init_allocator ();
	sgen_init_gchandles ();

	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_SECTION, SGEN_SIZEOF_GC_MEM_SECTION);
	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_GRAY_QUEUE, sizeof (GrayQueueSection));

	sgen_client_init ();

	if (sgen_mode != SGEN_MODE_NONE) {
		if (sgen_minor != SGEN_MINOR_DEFAULT || sgen_major != SGEN_MAJOR_DEFAULT)
			sgen_env_var_error (MONO_GC_PARAMS_NAME, "Ignoring major/minor configuration", "Major/minor configurations cannot be used with sgen modes");
		init_sgen_mode (sgen_mode);
	} else {
		init_sgen_minor (sgen_minor);
		init_sgen_major (sgen_major);
	}

	if (opts) {
		gboolean usage_printed = FALSE;

		for (ptr = opts; *ptr; ++ptr) {
			char *opt = *ptr;
			if (!strcmp (opt, ""))
				continue;
			if (g_str_has_prefix (opt, "major="))
				continue;
			if (g_str_has_prefix (opt, "minor="))
				continue;
			if (g_str_has_prefix (opt, "mode=")) {
				if (g_str_has_prefix (opt, "mode=pause:")) {
					char *str_pause = strchr (opt, ':') + 1;
					int pause = atoi (str_pause);
					if (pause)
						sgen_max_pause_time = pause;
					else
						sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default", "Invalid maximum pause time for `pause` sgen mode");
				}
				continue;
			}
			if (g_str_has_prefix (opt, "max-heap-size=")) {
				size_t page_size = mono_pagesize ();
				size_t max_heap_candidate = 0;
				opt = strchr (opt, '=') + 1;
				if (*opt && mono_gc_parse_environment_string_extract_number (opt, &max_heap_candidate)) {
					max_heap = (max_heap_candidate + page_size - 1) & ~(size_t)(page_size - 1);
					if (max_heap != max_heap_candidate)
						sgen_env_var_error (MONO_GC_PARAMS_NAME, "Rounding up.", "`max-heap-size` size must be a multiple of %d.", page_size);
				} else {
					sgen_env_var_error (MONO_GC_PARAMS_NAME, NULL, "`max-heap-size` must be an integer.");
				}
				continue;
			}
			if (g_str_has_prefix (opt, "soft-heap-limit=")) {
				opt = strchr (opt, '=') + 1;
				if (*opt && mono_gc_parse_environment_string_extract_number (opt, &soft_limit)) {
					if (soft_limit <= 0) {
						sgen_env_var_error (MONO_GC_PARAMS_NAME, NULL, "`soft-heap-limit` must be positive.");
						soft_limit = 0;
					}
				} else {
					sgen_env_var_error (MONO_GC_PARAMS_NAME, NULL, "`soft-heap-limit` must be an integer.");
				}
				continue;
			}
			if (g_str_has_prefix (opt, "nursery-size=")) {
				size_t val;
				opt = strchr (opt, '=') + 1;
				if (*opt && mono_gc_parse_environment_string_extract_number (opt, &val)) {
					if ((val & (val - 1))) {
						sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default value.", "`nursery-size` must be a power of two.");
						continue;
					}

					if (val < SGEN_MAX_NURSERY_WASTE) {
						sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default value.",
								"`nursery-size` must be at least %d bytes.", SGEN_MAX_NURSERY_WASTE);
						continue;
					}
#ifdef SGEN_MAX_NURSERY_SIZE
					if (val > SGEN_MAX_NURSERY_SIZE) {
						sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default value.",
								"`nursery-size` must be smaller than %" PRId64 " bytes.", SGEN_MAX_NURSERY_SIZE);
						continue;
					}
#endif
					min_nursery_size = max_nursery_size = val;
					dynamic_nursery = FALSE;
				} else {
					sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default value.", "`nursery-size` must be an integer.");
					continue;
				}
				continue;
			}
			if (g_str_has_prefix (opt, "save-target-ratio=")) {
				double val;
				opt = strchr (opt, '=') + 1;
				if (parse_double_in_interval (MONO_GC_PARAMS_NAME, "save-target-ratio", opt,
						SGEN_MIN_SAVE_TARGET_RATIO, SGEN_MAX_SAVE_TARGET_RATIO, &val)) {
					save_target = val;
				}
				continue;
			}
			if (g_str_has_prefix (opt, "default-allowance-ratio=")) {
				double val;
				opt = strchr (opt, '=') + 1;
				if (parse_double_in_interval (MONO_GC_PARAMS_NAME, "default-allowance-ratio", opt,
						SGEN_MIN_ALLOWANCE_NURSERY_SIZE_RATIO, SGEN_MAX_ALLOWANCE_NURSERY_SIZE_RATIO, &val)) {
					allowance_ratio = val;
				}
				continue;
			}

			if (!strcmp (opt, "cementing")) {
				cement_enabled = TRUE;
				continue;
			}
			if (!strcmp (opt, "no-cementing")) {
				cement_enabled = FALSE;
				continue;
			}

			if (!strcmp (opt, "precleaning")) {
				precleaning_enabled = TRUE;
				continue;
			}
			if (!strcmp (opt, "no-precleaning")) {
				precleaning_enabled = FALSE;
				continue;
			}

			if (!strcmp (opt, "dynamic-nursery")) {
				if (sgen_minor_collector.is_split)
					sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default value.",
							"dynamic-nursery not supported with split-nursery.");
				else
					dynamic_nursery = TRUE;
				continue;
			}
			if (!strcmp (opt, "no-dynamic-nursery")) {
				dynamic_nursery = FALSE;
				continue;
			}

			if (!strcmp (opt, "remset-copy-clear-par")) {
				if (!sgen_minor_collector.is_parallel)
					sgen_env_var_error (MONO_GC_PARAMS_NAME, "Using default value.",
							"parallel remset copy clear only supported with minor=simple-par.");
				else
					remset_copy_clear_par = TRUE;
				continue;
			}

			if (sgen_major_collector.handle_gc_param && sgen_major_collector.handle_gc_param (opt))
				continue;

			if (sgen_minor_collector.handle_gc_param && sgen_minor_collector.handle_gc_param (opt))
				continue;

			if (sgen_client_handle_gc_param (opt))
				continue;

			sgen_env_var_error (MONO_GC_PARAMS_NAME, "Ignoring.", "Unknown option `%s`.", opt);

			if (usage_printed)
				continue;

			fprintf (stderr, "\n%s must be a comma-delimited list of one or more of the following:\n", MONO_GC_PARAMS_NAME);
			fprintf (stderr, "  max-heap-size=N (where N is an integer, possibly with a k, m or a g suffix)\n");
			fprintf (stderr, "  soft-heap-limit=n (where N is an integer, possibly with a k, m or a g suffix)\n");
			fprintf (stderr, "  mode=MODE (where MODE is 'balanced', 'throughput' or 'pause[:N]' and N is maximum pause in milliseconds)\n");
			fprintf (stderr, "  nursery-size=N (where N is an integer, possibly with a k, m or a g suffix)\n");
			fprintf (stderr, "  major=COLLECTOR (where COLLECTOR is `marksweep', `marksweep-conc', `marksweep-par')\n");
			fprintf (stderr, "  minor=COLLECTOR (where COLLECTOR is `simple' or `split')\n");
			fprintf (stderr, "  wbarrier=WBARRIER (where WBARRIER is `remset' or `cardtable')\n");
			fprintf (stderr, "  [no-]cementing\n");
			fprintf (stderr, "  [no-]dynamic-nursery\n");
			fprintf (stderr, "  remset-copy-clear-par\n");
			if (sgen_major_collector.print_gc_param_usage)
				sgen_major_collector.print_gc_param_usage ();
			if (sgen_minor_collector.print_gc_param_usage)
				sgen_minor_collector.print_gc_param_usage ();
			sgen_client_print_gc_params_usage ();
			fprintf (stderr, " Experimental options:\n");
			fprintf (stderr, "  save-target-ratio=R (where R must be between %.2f - %.2f).\n", SGEN_MIN_SAVE_TARGET_RATIO, SGEN_MAX_SAVE_TARGET_RATIO);
			fprintf (stderr, "  default-allowance-ratio=R (where R must be between %.2f - %.2f).\n", SGEN_MIN_ALLOWANCE_NURSERY_SIZE_RATIO, SGEN_MAX_ALLOWANCE_NURSERY_SIZE_RATIO);
			fprintf (stderr, "\n");

			usage_printed = TRUE;
		}
		g_strfreev (opts);
	}

	if (params_opts)
		g_free (params_opts);

	alloc_nursery (dynamic_nursery, min_nursery_size, max_nursery_size);

	sgen_pinning_init ();
	sgen_cement_init (cement_enabled);

	if ((env = g_getenv (MONO_GC_DEBUG_NAME)) || gc_debug_options) {
		debug_opts = g_strdup_printf ("%s,%s", gc_debug_options ? gc_debug_options  : "", env ? env : "");
		g_free (env);
	}

	if (debug_opts) {
		gboolean usage_printed = FALSE;

		opts = g_strsplit (debug_opts, ",", -1);
		for (ptr = opts; ptr && *ptr; ptr ++) {
			char *opt = *ptr;
			if (!strcmp (opt, ""))
				continue;
			if (opt [0] >= '0' && opt [0] <= '9') {
				sgen_gc_debug_level = atoi (opt);
				opt++;
				if (opt [0] == ':')
					opt++;
				if (opt [0]) {
					char *rf = g_strdup_printf ("%s.%d", opt, mono_process_current_pid ());
					sgen_gc_debug_file = fopen (rf, "wb");
					if (!sgen_gc_debug_file)
						sgen_gc_debug_file = stderr;
					g_free (rf);
				}
			} else if (!strcmp (opt, "print-allowance")) {
				debug_print_allowance = TRUE;
			} else if (!strcmp (opt, "print-pinning")) {
				sgen_pin_stats_enable ();
			} else if (!strcmp (opt, "print-gchandles")) {
				sgen_gchandle_stats_enable ();
			} else if (!strcmp (opt, "verify-before-allocs")) {
				sgen_verify_before_allocs = 1;
				sgen_has_per_allocation_action = TRUE;
			} else if (g_str_has_prefix (opt, "max-valloc-size=")) {
				size_t max_valloc_size;
				char *arg = strchr (opt, '=') + 1;
				if (*opt && mono_gc_parse_environment_string_extract_number (arg, &max_valloc_size)) {
					mono_valloc_set_limit (max_valloc_size);
				} else {
					sgen_env_var_error (MONO_GC_DEBUG_NAME, NULL, "`max-valloc-size` must be an integer.");
				}
				continue;
			} else if (g_str_has_prefix (opt, "verify-before-allocs=")) {
				char *arg = strchr (opt, '=') + 1;
				sgen_verify_before_allocs = atoi (arg);
				sgen_has_per_allocation_action = TRUE;
			} else if (!strcmp (opt, "collect-before-allocs")) {
				sgen_collect_before_allocs = 1;
				sgen_has_per_allocation_action = TRUE;
			} else if (g_str_has_prefix (opt, "collect-before-allocs=")) {
				char *arg = strchr (opt, '=') + 1;
				sgen_has_per_allocation_action = TRUE;
				sgen_collect_before_allocs = atoi (arg);
			} else if (!strcmp (opt, "verify-before-collections")) {
				whole_heap_check_before_collection = TRUE;
			} else if (!strcmp (opt, "check-remset-consistency")) {
				remset_consistency_checks = TRUE;
				sgen_nursery_clear_policy = CLEAR_AT_GC;
			} else if (!strcmp (opt, "mod-union-consistency-check")) {
				if (!sgen_major_collector.is_concurrent) {
					sgen_env_var_error (MONO_GC_DEBUG_NAME, "Ignoring.", "`mod-union-consistency-check` only works with concurrent major collector.");
					continue;
				}
				mod_union_consistency_check = TRUE;
			} else if (!strcmp (opt, "check-mark-bits")) {
				check_mark_bits_after_major_collection = TRUE;
			} else if (!strcmp (opt, "check-nursery-untag")) {
				check_nursery_objects_untag = TRUE;
			} else if (!strcmp (opt, "clear-at-gc")) {
				sgen_nursery_clear_policy = CLEAR_AT_GC;
			} else if (!strcmp (opt, "clear-nursery-at-gc")) {
				sgen_nursery_clear_policy = CLEAR_AT_GC;
			} else if (!strcmp (opt, "clear-at-tlab-creation")) {
				sgen_nursery_clear_policy = CLEAR_AT_TLAB_CREATION;
			} else if (!strcmp (opt, "debug-clear-at-tlab-creation")) {
				sgen_nursery_clear_policy = CLEAR_AT_TLAB_CREATION_DEBUG;
			} else if (!strcmp (opt, "check-scan-starts")) {
				do_scan_starts_check = TRUE;
			} else if (!strcmp (opt, "verify-nursery-at-minor-gc")) {
				do_verify_nursery = TRUE;
			} else if (!strcmp (opt, "check-concurrent")) {
				if (!sgen_major_collector.is_concurrent) {
					sgen_env_var_error (MONO_GC_DEBUG_NAME, "Ignoring.", "`check-concurrent` only works with concurrent major collectors.");
					continue;
				}
				sgen_nursery_clear_policy = CLEAR_AT_GC;
				do_concurrent_checks = TRUE;
			} else if (!strcmp (opt, "dump-nursery-at-minor-gc")) {
				do_dump_nursery_content = TRUE;
			} else if (!strcmp (opt, "disable-minor")) {
				disable_minor_collections = TRUE;
			} else if (!strcmp (opt, "disable-major")) {
				disable_major_collections = TRUE;
			} else if (g_str_has_prefix (opt, "heap-dump=")) {
				char *filename = strchr (opt, '=') + 1;
				sgen_nursery_clear_policy = CLEAR_AT_GC;
				sgen_debug_enable_heap_dump (filename);
			} else if (g_str_has_prefix (opt, "binary-protocol=")) {
				char *filename = strchr (opt, '=') + 1;
				char *colon = strrchr (filename, ':');
				size_t limit = 0;
				if (colon) {
					if (!mono_gc_parse_environment_string_extract_number (colon + 1, &limit)) {
						sgen_env_var_error (MONO_GC_DEBUG_NAME, "Ignoring limit.", "Binary protocol file size limit must be an integer.");
						limit = -1;
					}
					*colon = '\0';
				}
				sgen_binary_protocol_init (filename, (gint64)limit);
			} else if (!strcmp (opt, "nursery-canaries")) {
				do_verify_nursery = TRUE;
#ifndef DISABLE_SGEN_DEBUG_HELPERS
				enable_nursery_canaries = TRUE;
#else
				g_error ("Sgen was built with canaries disabled");
#endif
				/* If aot code is used, allocation from there won't expect the layout with canaries enabled */
				sgen_set_use_managed_allocator (FALSE);
			} else if (!strcmp (opt, "coop-no-stack-scan")) {
				sgen_disable_native_stack_scan ();
			} else if (!sgen_client_handle_gc_debug (opt)) {
				sgen_env_var_error (MONO_GC_DEBUG_NAME, "Ignoring.", "Unknown option `%s`.", opt);

				if (usage_printed)
					continue;

				fprintf (stderr, "\n%s must be of the format [<l>[:<filename>]|<option>]+ where <l> is a debug level 0-9.\n", MONO_GC_DEBUG_NAME);
				fprintf (stderr, "Valid <option>s are:\n");
				fprintf (stderr, "  collect-before-allocs[=<n>]\n");
				fprintf (stderr, "  verify-before-allocs[=<n>]\n");
				fprintf (stderr, "  max-valloc-size=N (where N is an integer, possibly with a k, m or a g suffix)\n");
				fprintf (stderr, "  check-remset-consistency\n");
				fprintf (stderr, "  check-mark-bits\n");
				fprintf (stderr, "  check-nursery-untag\n");
				fprintf (stderr, "  verify-before-collections\n");
				fprintf (stderr, "  verify-nursery-at-minor-gc\n");
				fprintf (stderr, "  dump-nursery-at-minor-gc\n");
				fprintf (stderr, "  disable-minor\n");
				fprintf (stderr, "  disable-major\n");
				fprintf (stderr, "  check-concurrent\n");
				fprintf (stderr, "  clear-[nursery-]at-gc\n");
				fprintf (stderr, "  clear-at-tlab-creation\n");
				fprintf (stderr, "  debug-clear-at-tlab-creation\n");
				fprintf (stderr, "  check-scan-starts\n");
				fprintf (stderr, "  print-allowance\n");
				fprintf (stderr, "  print-pinning\n");
				fprintf (stderr, "  print-gchandles\n");
				fprintf (stderr, "  coop-no-stack-scan\n");
				fprintf (stderr, "  heap-dump=<filename>\n");
				fprintf (stderr, "  binary-protocol=<filename>[:<file-size-limit>]\n");
				fprintf (stderr, "  nursery-canaries\n");
				sgen_client_print_gc_debug_usage ();
				fprintf (stderr, "\n");

				usage_printed = TRUE;
			}
		}
		g_strfreev (opts);
	}

	if (debug_opts)
		g_free (debug_opts);

	if (check_mark_bits_after_major_collection)
		sgen_nursery_clear_policy = CLEAR_AT_GC;

	if (sgen_major_collector.post_param_init)
		sgen_major_collector.post_param_init (&sgen_major_collector);

	sgen_thread_pool_start ();

	sgen_memgov_init (max_heap, soft_limit, debug_print_allowance, allowance_ratio, save_target);

	memset (&remset, 0, sizeof (remset));

	sgen_card_table_init (&remset);

	sgen_register_root (NULL, 0, sgen_make_user_root_descriptor (sgen_mark_normal_gc_handles), ROOT_TYPE_NORMAL, MONO_ROOT_SOURCE_GC_HANDLE, NULL, "GC Handles (SGen, Normal)");

	gc_initialized = 1;

	sgen_init_bridge ();
}

gboolean
sgen_gc_initialized ()
{
	return gc_initialized > 0;
}

NurseryClearPolicy
sgen_get_nursery_clear_policy (void)
{
	return sgen_nursery_clear_policy;
}

void
sgen_gc_lock (void)
{
	mono_coop_mutex_lock (&sgen_gc_mutex);
}

void
sgen_gc_unlock (void)
{
	mono_coop_mutex_unlock (&sgen_gc_mutex);
}

void
sgen_major_collector_iterate_live_block_ranges (sgen_cardtable_block_callback callback)
{
	sgen_major_collector.iterate_live_block_ranges (callback);
}

void
sgen_major_collector_iterate_block_ranges (sgen_cardtable_block_callback callback)
{
	sgen_major_collector.iterate_block_ranges (callback);
}

SgenMajorCollector*
sgen_get_major_collector (void)
{
	return &sgen_major_collector;
}

SgenMinorCollector*
sgen_get_minor_collector (void)
{
	return &sgen_minor_collector;
}

SgenRememberedSet*
sgen_get_remset (void)
{
	return &remset;
}

static void
count_cards (long long *major_total, long long *major_marked, long long *los_total, long long *los_marked)
{
	sgen_get_major_collector ()->count_cards (major_total, major_marked);
	sgen_los_count_cards (los_total, los_marked);
}

static gboolean world_is_stopped = FALSE;

/* LOCKING: assumes the GC lock is held */
void
sgen_stop_world (int generation, gboolean serial_collection)
{
	long long major_total = -1, major_marked = -1, los_total = -1, los_marked = -1;

	SGEN_ASSERT (0, !world_is_stopped, "Why are we stopping a stopped world?");

	sgen_binary_protocol_world_stopping (generation, sgen_timestamp (), (gpointer) (gsize) mono_native_thread_id_get ());

	sgen_client_stop_world (generation, serial_collection);

	world_is_stopped = TRUE;

	if (sgen_binary_protocol_is_heavy_enabled ())
		count_cards (&major_total, &major_marked, &los_total, &los_marked);
	sgen_binary_protocol_world_stopped (generation, sgen_timestamp (), major_total, major_marked, los_total, los_marked);
}

/* LOCKING: assumes the GC lock is held */
void
sgen_restart_world (int generation, gboolean serial_collection)
{
	long long major_total = -1, major_marked = -1, los_total = -1, los_marked = -1;
	gint64 stw_time;

	SGEN_ASSERT (0, world_is_stopped, "Why are we restarting a running world?");

	if (sgen_binary_protocol_is_heavy_enabled ())
		count_cards (&major_total, &major_marked, &los_total, &los_marked);
	sgen_binary_protocol_world_restarting (generation, sgen_timestamp (), major_total, major_marked, los_total, los_marked);

	world_is_stopped = FALSE;

	sgen_client_restart_world (generation, serial_collection, &stw_time);

	sgen_binary_protocol_world_restarted (generation, sgen_timestamp ());

	if (sgen_client_bridge_need_processing ())
		sgen_client_bridge_processing_finish (generation);

	sgen_memgov_collection_end (generation, stw_time);
}

gboolean
sgen_is_world_stopped (void)
{
	return world_is_stopped;
}

void
sgen_check_whole_heap_stw (void)
{
	sgen_stop_world (0, FALSE);
	sgen_clear_nursery_fragments ();
	sgen_check_whole_heap (TRUE);
	sgen_restart_world (0, FALSE);
}

gint64
sgen_timestamp (void)
{
	SGEN_TV_DECLARE (timestamp);
	SGEN_TV_GETTIME (timestamp);
	return SGEN_TV_ELAPSED (sgen_init_timestamp, timestamp);
}

void
sgen_check_canary_for_object (gpointer addr)
{
	if (sgen_nursery_canaries_enabled ()) {
		guint size = sgen_safe_object_get_size_unaligned ((GCObject *) (addr));
		char* canary_ptr = (char*) (addr) + size;
		if (!CANARY_VALID(canary_ptr)) {
			char *window_start, *window_end;
			window_start = (char*)(addr) - 128;
			if (!sgen_ptr_in_nursery (window_start))
				window_start = sgen_get_nursery_start ();
			window_end = (char*)(addr) + 128;
			if (!sgen_ptr_in_nursery (window_end))
				window_end = sgen_get_nursery_end ();
			fprintf (stderr, "\nCANARY ERROR - Type:%s Size:%d Address:%p Data:\n", sgen_client_vtable_get_name (SGEN_LOAD_VTABLE ((addr))), size,  (char*) addr);
			fwrite (addr, sizeof (char), size, stderr);
			fprintf (stderr, "\nCanary zone (next 12 chars):\n");
			fwrite (canary_ptr, sizeof (char), 12, stderr);
			fprintf (stderr, "\nOriginal canary string:\n");
			fwrite (CANARY_STRING, sizeof (char), 8, stderr);
			for (int x = -8; x <= 8; x++) {
				if (canary_ptr + x < (char*) addr)
					continue;
				if (CANARY_VALID(canary_ptr +x))
					fprintf (stderr, "\nCANARY ERROR - canary found at offset %d\n", x);
			}
			fprintf (stderr, "\nSurrounding nursery (%p - %p):\n", window_start, window_end);
			fwrite (window_start, sizeof (char), window_end - window_start, stderr);
		}
	}
}

#endif /* HAVE_SGEN_GC */
