/**
 * \file
 * Object allocation routines + managed allocators
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *  Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2005-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2011 Xamarin, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * ######################################################################
 * ########  Object allocation
 * ######################################################################
 * This section of code deals with allocating memory for objects.
 * There are several ways:
 * *) allocate large objects
 * *) allocate normal objects
 * *) fast lock-free allocation
 * *) allocation of pinned objects
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-client.h"
#include "mono/utils/mono-memory-model.h"

#define ALIGN_UP		SGEN_ALIGN_UP
#define ALLOC_ALIGN		SGEN_ALLOC_ALIGN
#define MAX_SMALL_OBJ_SIZE	SGEN_MAX_SMALL_OBJ_SIZE

#ifdef HEAVY_STATISTICS
static guint64 stat_objects_alloced = 0;
static guint64 stat_bytes_alloced = 0;
static guint64 stat_bytes_alloced_los = 0;

#endif

/*
 * Allocation is done from a Thread Local Allocation Buffer (TLAB). TLABs are allocated
 * from nursery fragments.
 * tlab_next is the pointer to the space inside the TLAB where the next object will 
 * be allocated.
 * tlab_temp_end is the pointer to the end of the temporary space reserved for
 * the allocation: it allows us to set the scan starts at reasonable intervals.
 * tlab_real_end points to the end of the TLAB.
 */

#define TLAB_START	(__thread_info__->tlab_start)
#define TLAB_NEXT	(__thread_info__->tlab_next)
#define TLAB_TEMP_END	(__thread_info__->tlab_temp_end)
#define TLAB_REAL_END	(__thread_info__->tlab_real_end)

static GCObject*
alloc_degraded (GCVTable vtable, size_t size, gboolean for_mature)
{
	GCObject *p;

	if (!for_mature) {
		sgen_client_degraded_allocation ();
		SGEN_ATOMIC_ADD_P (degraded_mode, size);
		sgen_ensure_free_space (size, GENERATION_OLD);
	} else {
		if (sgen_need_major_collection (size))
			sgen_perform_collection (size, GENERATION_OLD, "mature allocation failure", !for_mature, TRUE);
	}


	p = major_collector.alloc_degraded (vtable, size);

	if (!for_mature)
		binary_protocol_alloc_degraded (p, vtable, size, sgen_client_get_provenance ());

	return p;
}

static void
zero_tlab_if_necessary (void *p, size_t size)
{
	if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION || nursery_clear_policy == CLEAR_AT_TLAB_CREATION_DEBUG) {
		memset (p, 0, size);
	} else {
		/*
		 * This function is called for all allocations in
		 * TLABs.  TLABs originate from fragments, which are
		 * initialized to be faux arrays.  The remainder of
		 * the fragments are zeroed out at initialization for
		 * CLEAR_AT_GC, so here we just need to make sure that
		 * the array header is zeroed.  Since we don't know
		 * whether we're called for the start of a fragment or
		 * for somewhere in between, we zero in any case, just
		 * to make sure.
		 */
		sgen_client_zero_array_fill_header (p, size);
	}
}

/*
 * Provide a variant that takes just the vtable for small fixed-size objects.
 * The aligned size is already computed and stored in vt->gc_descr.
 * Note: every SGEN_SCAN_START_SIZE or so we are given the chance to do some special
 * processing. We can keep track of where objects start, for example,
 * so when we scan the thread stacks for pinned objects, we can start
 * a search for the pinned object in SGEN_SCAN_START_SIZE chunks.
 */
GCObject*
sgen_alloc_obj_nolock (GCVTable vtable, size_t size)
{
	/* FIXME: handle OOM */
	void **p;
	char *new_next;
	size_t real_size = size;
	TLAB_ACCESS_INIT;
	
	CANARIFY_SIZE(size);

	HEAVY_STAT (++stat_objects_alloced);
	if (real_size <= SGEN_MAX_SMALL_OBJ_SIZE)
		HEAVY_STAT (stat_bytes_alloced += size);
	else
		HEAVY_STAT (stat_bytes_alloced_los += size);

	size = ALIGN_UP (size);

	SGEN_ASSERT (6, sgen_vtable_get_descriptor (vtable), "VTable without descriptor");

	if (G_UNLIKELY (has_per_allocation_action)) {
		static int alloc_count;
		int current_alloc = mono_atomic_inc_i32 (&alloc_count);

		if (collect_before_allocs) {
			if (((current_alloc % collect_before_allocs) == 0) && nursery_section) {
				sgen_perform_collection (0, GENERATION_NURSERY, "collect-before-alloc-triggered", TRUE, TRUE);
				if (!degraded_mode && sgen_can_alloc_size (size) && real_size <= SGEN_MAX_SMALL_OBJ_SIZE) {
					// FIXME:
					g_assert_not_reached ();
				}
			}
		} else if (verify_before_allocs) {
			if ((current_alloc % verify_before_allocs) == 0)
				sgen_check_whole_heap_stw ();
		}
	}

	/*
	 * We must already have the lock here instead of after the
	 * fast path because we might be interrupted in the fast path
	 * (after confirming that new_next < TLAB_TEMP_END) by the GC,
	 * and we'll end up allocating an object in a fragment which
	 * no longer belongs to us.
	 *
	 * The managed allocator does not do this, but it's treated
	 * specially by the world-stopping code.
	 */

	if (real_size > SGEN_MAX_SMALL_OBJ_SIZE) {
		p = (void **)sgen_los_alloc_large_inner (vtable, ALIGN_UP (real_size));
	} else {
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;
		TLAB_NEXT = new_next;

		if (G_LIKELY (new_next < TLAB_TEMP_END)) {
			/* Fast path */

			CANARIFY_ALLOC(p,real_size);
			SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
			binary_protocol_alloc (p , vtable, size, sgen_client_get_provenance ());
			g_assert (*p == NULL);
			mono_atomic_store_seq (p, vtable);

			return (GCObject*)p;
		}

		/* Slow path */

		/* there are two cases: the object is too big or we run out of space in the TLAB */
		/* we also reach here when the thread does its first allocation after a minor 
		 * collection, since the tlab_ variables are initialized to NULL.
		 * there can be another case (from ORP), if we cooperate with the runtime a bit:
		 * objects that need finalizers can have the high bit set in their size
		 * so the above check fails and we can readily add the object to the queue.
		 * This avoids taking again the GC lock when registering, but this is moot when
		 * doing thread-local allocation, so it may not be a good idea.
		 */
		if (TLAB_NEXT >= TLAB_REAL_END) {
			int available_in_tlab;
			/* 
			 * Run out of space in the TLAB. When this happens, some amount of space
			 * remains in the TLAB, but not enough to satisfy the current allocation
			 * request. Currently, we retire the TLAB in all cases, later we could
			 * keep it if the remaining space is above a treshold, and satisfy the
			 * allocation directly from the nursery.
			 */
			TLAB_NEXT -= size;
			/* when running in degraded mode, we continue allocing that way
			 * for a while, to decrease the number of useless nursery collections.
			 */
			if (degraded_mode && degraded_mode < sgen_nursery_size)
				return alloc_degraded (vtable, size, FALSE);

			available_in_tlab = (int)(TLAB_REAL_END - TLAB_NEXT);//We'll never have tlabs > 2Gb
			if (size > tlab_size || available_in_tlab > SGEN_MAX_NURSERY_WASTE) {
				/* Allocate directly from the nursery */
				p = (void **)sgen_nursery_alloc (size);
				if (!p) {
					/*
					 * We couldn't allocate from the nursery, so we try
					 * collecting.  Even after the collection, we might
					 * still not have enough memory to allocate the
					 * object.  The reason will most likely be that we've
					 * run out of memory, but there is the theoretical
					 * possibility that other threads might have consumed
					 * the freed up memory ahead of us.
					 *
					 * What we do in this case is allocate degraded, i.e.,
					 * from the major heap.
					 *
					 * Ideally we'd like to detect the case of other
					 * threads allocating ahead of us and loop (if we
					 * always loop we will loop endlessly in the case of
					 * OOM).
					 */
					sgen_ensure_free_space (real_size, GENERATION_NURSERY);
					if (!degraded_mode)
						p = (void **)sgen_nursery_alloc (size);
				}
				if (!p)
					return alloc_degraded (vtable, size, TRUE);

				zero_tlab_if_necessary (p, size);
			} else {
				size_t alloc_size = 0;
				if (TLAB_START)
					SGEN_LOG (3, "Retire TLAB: %p-%p [%ld]", TLAB_START, TLAB_REAL_END, (long)(TLAB_REAL_END - TLAB_NEXT - size));
				sgen_nursery_retire_region (p, available_in_tlab);

				p = (void **)sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
				if (!p) {
					/* See comment above in similar case. */
					sgen_ensure_free_space (tlab_size, GENERATION_NURSERY);
					if (!degraded_mode)
						p = (void **)sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
				}
				if (!p)
					return alloc_degraded (vtable, size, TRUE);

				/* Allocate a new TLAB from the current nursery fragment */
				TLAB_START = (char*)p;
				TLAB_NEXT = TLAB_START;
				TLAB_REAL_END = TLAB_START + alloc_size;
				TLAB_TEMP_END = TLAB_START + MIN (SGEN_SCAN_START_SIZE, alloc_size);

				zero_tlab_if_necessary (TLAB_START, alloc_size);

				/* Allocate from the TLAB */
				p = (void **)TLAB_NEXT;
				TLAB_NEXT += size;
				sgen_set_nursery_scan_start ((char*)p);
			}
		} else {
			/* Reached tlab_temp_end */

			/* record the scan start so we can find pinned objects more easily */
			sgen_set_nursery_scan_start ((char*)p);
			/* we just bump tlab_temp_end as well */
			TLAB_TEMP_END = MIN (TLAB_REAL_END, TLAB_NEXT + SGEN_SCAN_START_SIZE);
			SGEN_LOG (5, "Expanding local alloc: %p-%p", TLAB_NEXT, TLAB_TEMP_END);
		}
		CANARIFY_ALLOC(p,real_size);
	}

	if (G_LIKELY (p)) {
		SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
		binary_protocol_alloc (p, vtable, size, sgen_client_get_provenance ());
		mono_atomic_store_seq (p, vtable);
	}

	return (GCObject*)p;
}

GCObject*
sgen_try_alloc_obj_nolock (GCVTable vtable, size_t size)
{
	void **p;
	char *new_next;
	size_t real_size = size;
	TLAB_ACCESS_INIT;

	CANARIFY_SIZE(size);

	size = ALIGN_UP (size);
	SGEN_ASSERT (9, real_size >= SGEN_CLIENT_MINIMUM_OBJECT_SIZE, "Object too small");

	SGEN_ASSERT (6, sgen_vtable_get_descriptor (vtable), "VTable without descriptor");

	if (real_size > SGEN_MAX_SMALL_OBJ_SIZE)
		return NULL;

	if (G_UNLIKELY (size > tlab_size)) {
		/* Allocate directly from the nursery */
		p = (void **)sgen_nursery_alloc (size);
		if (!p)
			return NULL;
		sgen_set_nursery_scan_start ((char*)p);

		/*FIXME we should use weak memory ops here. Should help specially on x86. */
		zero_tlab_if_necessary (p, size);
	} else {
		int available_in_tlab;
		char *real_end;
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;

		real_end = TLAB_REAL_END;
		available_in_tlab = (int)(real_end - (char*)p);//We'll never have tlabs > 2Gb

		if (G_LIKELY (new_next < real_end)) {
			TLAB_NEXT = new_next;

			/* Second case, we overflowed temp end */
			if (G_UNLIKELY (new_next >= TLAB_TEMP_END)) {
				sgen_set_nursery_scan_start (new_next);
				/* we just bump tlab_temp_end as well */
				TLAB_TEMP_END = MIN (TLAB_REAL_END, TLAB_NEXT + SGEN_SCAN_START_SIZE);
				SGEN_LOG (5, "Expanding local alloc: %p-%p", TLAB_NEXT, TLAB_TEMP_END);
			}
		} else if (available_in_tlab > SGEN_MAX_NURSERY_WASTE) {
			/* Allocate directly from the nursery */
			p = (void **)sgen_nursery_alloc (size);
			if (!p)
				return NULL;

			zero_tlab_if_necessary (p, size);
		} else {
			size_t alloc_size = 0;

			sgen_nursery_retire_region (p, available_in_tlab);
			new_next = (char *)sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
			p = (void**)new_next;
			if (!p)
				return NULL;

			TLAB_START = (char*)new_next;
			TLAB_NEXT = new_next + size;
			TLAB_REAL_END = new_next + alloc_size;
			TLAB_TEMP_END = new_next + MIN (SGEN_SCAN_START_SIZE, alloc_size);
			sgen_set_nursery_scan_start ((char*)p);

			zero_tlab_if_necessary (new_next, alloc_size);
		}
	}

	HEAVY_STAT (++stat_objects_alloced);
	HEAVY_STAT (stat_bytes_alloced += size);

	CANARIFY_ALLOC(p,real_size);
	SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
	binary_protocol_alloc (p, vtable, size, sgen_client_get_provenance ());
	g_assert (*p == NULL); /* FIXME disable this in non debug builds */

	mono_atomic_store_seq (p, vtable);

	return (GCObject*)p;
}

GCObject*
sgen_alloc_obj (GCVTable vtable, size_t size)
{
	GCObject *res;
	TLAB_ACCESS_INIT;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;

	if (G_UNLIKELY (has_per_allocation_action)) {
		static int alloc_count;
		int current_alloc = mono_atomic_inc_i32 (&alloc_count);

		if (verify_before_allocs) {
			if ((current_alloc % verify_before_allocs) == 0) {
				LOCK_GC;
				sgen_check_whole_heap_stw ();
				UNLOCK_GC;
			}
		}
		if (collect_before_allocs) {
			if (((current_alloc % collect_before_allocs) == 0) && nursery_section) {
				LOCK_GC;
				sgen_perform_collection (0, GENERATION_NURSERY, "collect-before-alloc-triggered", TRUE, TRUE);
				UNLOCK_GC;
			}
		}
	}

	ENTER_CRITICAL_REGION;
	res = sgen_try_alloc_obj_nolock (vtable, size);
	if (res) {
		EXIT_CRITICAL_REGION;
		return res;
	}
	EXIT_CRITICAL_REGION;

	LOCK_GC;
	res = sgen_alloc_obj_nolock (vtable, size);
	UNLOCK_GC;
	return res;
}

/*
 * To be used for interned strings and possibly MonoThread, reflection handles.
 * We may want to explicitly free these objects.
 */
GCObject*
sgen_alloc_obj_pinned (GCVTable vtable, size_t size)
{
	GCObject *p;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;
	size = ALIGN_UP (size);

	LOCK_GC;

	if (size > SGEN_MAX_SMALL_OBJ_SIZE) {
		/* large objects are always pinned anyway */
		p = (GCObject *)sgen_los_alloc_large_inner (vtable, size);
	} else {
		SGEN_ASSERT (9, sgen_client_vtable_is_inited (vtable), "class %s:%s is not initialized", sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable));
		p = major_collector.alloc_small_pinned_obj (vtable, size, SGEN_VTABLE_HAS_REFERENCES (vtable));
	}
	if (G_LIKELY (p)) {
		SGEN_LOG (6, "Allocated pinned object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
		binary_protocol_alloc_pinned (p, vtable, size, sgen_client_get_provenance ());
	}
	UNLOCK_GC;
	return p;
}

GCObject*
sgen_alloc_obj_mature (GCVTable vtable, size_t size)
{
	GCObject *res;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;
	size = ALIGN_UP (size);

	LOCK_GC;
	res = alloc_degraded (vtable, size, TRUE);
	UNLOCK_GC;

	return res;
}

/*
 * Clear the thread local TLAB variables for all threads.
 */
void
sgen_clear_tlabs (void)
{
	FOREACH_THREAD (info) {
		/* A new TLAB will be allocated when the thread does its first allocation */
		info->tlab_start = NULL;
		info->tlab_next = NULL;
		info->tlab_temp_end = NULL;
		info->tlab_real_end = NULL;
	} FOREACH_THREAD_END
}

void
sgen_init_allocator (void)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("# objects allocated", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_objects_alloced);
	mono_counters_register ("bytes allocated", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_bytes_alloced);
	mono_counters_register ("bytes allocated in LOS", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_bytes_alloced_los);
#endif
}

#endif /*HAVE_SGEN_GC*/
