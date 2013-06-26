/*
 * sgen-alloc.c: Object allocation routines + managed allocators
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

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-memory-governor.h"
#include "metadata/profiler-private.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "utils/mono-memory-model.h"
#include "utils/mono-counters.h"

#define ALIGN_UP		SGEN_ALIGN_UP
#define ALLOC_ALIGN		SGEN_ALLOC_ALIGN
#define ALLOC_ALIGN_BITS	SGEN_ALLOC_ALIGN_BITS
#define MAX_SMALL_OBJ_SIZE	SGEN_MAX_SMALL_OBJ_SIZE
#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LAST
};

#undef OPDEF

static gboolean use_managed_allocator = TRUE;

#ifdef HEAVY_STATISTICS
static long long stat_objects_alloced = 0;
static long long stat_bytes_alloced = 0;
static long long stat_bytes_alloced_los = 0;

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

/*
 * FIXME: What is faster, a TLS variable pointing to a structure, or separate TLS 
 * variables for next+temp_end ?
 */
#ifdef HAVE_KW_THREAD
static __thread char *tlab_start;
static __thread char *tlab_next;
static __thread char *tlab_temp_end;
static __thread char *tlab_real_end;
/* Used by the managed allocator/wbarrier */
static __thread char **tlab_next_addr;
#endif

#ifdef HAVE_KW_THREAD
#define TLAB_START	tlab_start
#define TLAB_NEXT	tlab_next
#define TLAB_TEMP_END	tlab_temp_end
#define TLAB_REAL_END	tlab_real_end
#else
#define TLAB_START	(__thread_info__->tlab_start)
#define TLAB_NEXT	(__thread_info__->tlab_next)
#define TLAB_TEMP_END	(__thread_info__->tlab_temp_end)
#define TLAB_REAL_END	(__thread_info__->tlab_real_end)
#endif

static void*
alloc_degraded (MonoVTable *vtable, size_t size, gboolean for_mature)
{
	static int last_major_gc_warned = -1;
	static int num_degraded = 0;

	void *p;

	if (!for_mature) {
		if (last_major_gc_warned < stat_major_gcs) {
			++num_degraded;
			if (num_degraded == 1 || num_degraded == 3)
				fprintf (stderr, "Warning: Degraded allocation.  Consider increasing nursery-size if the warning persists.\n");
			else if (num_degraded == 10)
				fprintf (stderr, "Warning: Repeated degraded allocation.  Consider increasing nursery-size.\n");
			last_major_gc_warned = stat_major_gcs;
		}
		InterlockedExchangeAdd (&degraded_mode, size);
		sgen_ensure_free_space (size);
	} else {
		if (sgen_need_major_collection (size))
			sgen_perform_collection (size, GENERATION_OLD, "mature allocation failure", !for_mature);
	}


	p = major_collector.alloc_degraded (vtable, size);

	if (for_mature) {
		MONO_GC_MAJOR_OBJ_ALLOC_MATURE ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
	} else {
		binary_protocol_alloc_degraded (p, vtable, size);
		MONO_GC_MAJOR_OBJ_ALLOC_DEGRADED ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
	}

	return p;
}

/*
 * Provide a variant that takes just the vtable for small fixed-size objects.
 * The aligned size is already computed and stored in vt->gc_descr.
 * Note: every SGEN_SCAN_START_SIZE or so we are given the chance to do some special
 * processing. We can keep track of where objects start, for example,
 * so when we scan the thread stacks for pinned objects, we can start
 * a search for the pinned object in SGEN_SCAN_START_SIZE chunks.
 */
static void*
mono_gc_alloc_obj_nolock (MonoVTable *vtable, size_t size)
{
	/* FIXME: handle OOM */
	void **p;
	char *new_next;
	TLAB_ACCESS_INIT;

	HEAVY_STAT (++stat_objects_alloced);
	if (size <= SGEN_MAX_SMALL_OBJ_SIZE)
		HEAVY_STAT (stat_bytes_alloced += size);
	else
		HEAVY_STAT (stat_bytes_alloced_los += size);

	size = ALIGN_UP (size);

	g_assert (vtable->gc_descr);

	if (G_UNLIKELY (has_per_allocation_action)) {
		static int alloc_count;
		int current_alloc = InterlockedIncrement (&alloc_count);

		if (collect_before_allocs) {
			if (((current_alloc % collect_before_allocs) == 0) && nursery_section) {
				sgen_perform_collection (0, GENERATION_NURSERY, "collect-before-alloc-triggered", TRUE);
				if (!degraded_mode && sgen_can_alloc_size (size) && size <= SGEN_MAX_SMALL_OBJ_SIZE) {
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

	if (size > SGEN_MAX_SMALL_OBJ_SIZE) {
		p = sgen_los_alloc_large_inner (vtable, size);
	} else {
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;
		TLAB_NEXT = new_next;

		if (G_LIKELY (new_next < TLAB_TEMP_END)) {
			/* Fast path */

			/* 
			 * FIXME: We might need a memory barrier here so the change to tlab_next is 
			 * visible before the vtable store.
			 */

			SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, vtable->klass->name, size);
			binary_protocol_alloc (p , vtable, size);
			if (G_UNLIKELY (MONO_GC_NURSERY_OBJ_ALLOC_ENABLED ()))
				MONO_GC_NURSERY_OBJ_ALLOC ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
			g_assert (*p == NULL);
			mono_atomic_store_seq (p, vtable);

			return p;
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
			if (degraded_mode && degraded_mode < DEFAULT_NURSERY_SIZE)
				return alloc_degraded (vtable, size, FALSE);

			available_in_tlab = TLAB_REAL_END - TLAB_NEXT;
			if (size > tlab_size || available_in_tlab > SGEN_MAX_NURSERY_WASTE) {
				/* Allocate directly from the nursery */
				do {
					p = sgen_nursery_alloc (size);
					if (!p) {
						sgen_ensure_free_space (size);
						if (degraded_mode)
							return alloc_degraded (vtable, size, FALSE);
						else
							p = sgen_nursery_alloc (size);
					}
				} while (!p);
				if (!p) {
					// no space left
					g_assert (0);
				}

				if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
					memset (p, 0, size);
				}
			} else {
				size_t alloc_size = 0;
				if (TLAB_START)
					SGEN_LOG (3, "Retire TLAB: %p-%p [%ld]", TLAB_START, TLAB_REAL_END, (long)(TLAB_REAL_END - TLAB_NEXT - size));
				sgen_nursery_retire_region (p, available_in_tlab);

				do {
					p = sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
					if (!p) {
						sgen_ensure_free_space (tlab_size);
						if (degraded_mode)
							return alloc_degraded (vtable, size, FALSE);
						else
							p = sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
					}
				} while (!p);
					
				if (!p) {
					// no space left
					g_assert (0);
				}

				/* Allocate a new TLAB from the current nursery fragment */
				TLAB_START = (char*)p;
				TLAB_NEXT = TLAB_START;
				TLAB_REAL_END = TLAB_START + alloc_size;
				TLAB_TEMP_END = TLAB_START + MIN (SGEN_SCAN_START_SIZE, alloc_size);

				if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
					memset (TLAB_START, 0, alloc_size);
				}

				/* Allocate from the TLAB */
				p = (void*)TLAB_NEXT;
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
	}

	if (G_LIKELY (p)) {
		SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, vtable->klass->name, size);
		binary_protocol_alloc (p, vtable, size);
		if (G_UNLIKELY (MONO_GC_MAJOR_OBJ_ALLOC_LARGE_ENABLED ()|| MONO_GC_NURSERY_OBJ_ALLOC_ENABLED ())) {
			if (size > SGEN_MAX_SMALL_OBJ_SIZE)
				MONO_GC_MAJOR_OBJ_ALLOC_LARGE ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
			else
				MONO_GC_NURSERY_OBJ_ALLOC ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
		}
		mono_atomic_store_seq (p, vtable);
	}

	return p;
}

static void*
mono_gc_try_alloc_obj_nolock (MonoVTable *vtable, size_t size)
{
	void **p;
	char *new_next;
	TLAB_ACCESS_INIT;

	size = ALIGN_UP (size);

	g_assert (vtable->gc_descr);
	if (size > SGEN_MAX_SMALL_OBJ_SIZE)
		return NULL;

	if (G_UNLIKELY (size > tlab_size)) {
		/* Allocate directly from the nursery */
		p = sgen_nursery_alloc (size);
		if (!p)
			return NULL;
		sgen_set_nursery_scan_start ((char*)p);

		/*FIXME we should use weak memory ops here. Should help specially on x86. */
		if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
			memset (p, 0, size);
	} else {
		int available_in_tlab;
		char *real_end;
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;

		real_end = TLAB_REAL_END;
		available_in_tlab = real_end - (char*)p;

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
			p = sgen_nursery_alloc (size);
			if (!p)
				return NULL;

			if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
				memset (p, 0, size);			
		} else {
			size_t alloc_size = 0;

			sgen_nursery_retire_region (p, available_in_tlab);
			new_next = sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
			p = (void**)new_next;
			if (!p)
				return NULL;

			TLAB_START = (char*)new_next;
			TLAB_NEXT = new_next + size;
			TLAB_REAL_END = new_next + alloc_size;
			TLAB_TEMP_END = new_next + MIN (SGEN_SCAN_START_SIZE, alloc_size);
			sgen_set_nursery_scan_start ((char*)p);

			if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
				memset (new_next, 0, alloc_size);

			MONO_GC_NURSERY_TLAB_ALLOC ((mword)new_next, alloc_size);
		}
	}

	HEAVY_STAT (++stat_objects_alloced);
	HEAVY_STAT (stat_bytes_alloced += size);

	SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, vtable->klass->name, size);
	binary_protocol_alloc (p, vtable, size);
	if (G_UNLIKELY (MONO_GC_NURSERY_OBJ_ALLOC_ENABLED ()))
		MONO_GC_NURSERY_OBJ_ALLOC ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
	g_assert (*p == NULL); /* FIXME disable this in non debug builds */

	mono_atomic_store_seq (p, vtable);

	return p;
}

void*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	void *res;
#ifndef DISABLE_CRITICAL_REGION
	TLAB_ACCESS_INIT;
	ENTER_CRITICAL_REGION;
	res = mono_gc_try_alloc_obj_nolock (vtable, size);
	if (res) {
		EXIT_CRITICAL_REGION;
		return res;
	}
	EXIT_CRITICAL_REGION;
#endif
	LOCK_GC;
	res = mono_gc_alloc_obj_nolock (vtable, size);
	UNLOCK_GC;
	if (G_UNLIKELY (!res))
		return mono_gc_out_of_memory (size);
	return res;
}

void*
mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	MonoArray *arr;
#ifndef DISABLE_CRITICAL_REGION
	TLAB_ACCESS_INIT;
	ENTER_CRITICAL_REGION;
	arr = mono_gc_try_alloc_obj_nolock (vtable, size);
	if (arr) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		arr->max_length = max_length;
		EXIT_CRITICAL_REGION;
		return arr;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	arr = mono_gc_alloc_obj_nolock (vtable, size);
	if (G_UNLIKELY (!arr)) {
		UNLOCK_GC;
		return mono_gc_out_of_memory (size);
	}

	arr->max_length = max_length;

	UNLOCK_GC;

	return arr;
}

void*
mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size)
{
	MonoArray *arr;
	MonoArrayBounds *bounds;

#ifndef DISABLE_CRITICAL_REGION
	TLAB_ACCESS_INIT;
	ENTER_CRITICAL_REGION;
	arr = mono_gc_try_alloc_obj_nolock (vtable, size);
	if (arr) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		arr->max_length = max_length;

		bounds = (MonoArrayBounds*)((char*)arr + size - bounds_size);
		arr->bounds = bounds;
		EXIT_CRITICAL_REGION;
		return arr;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	arr = mono_gc_alloc_obj_nolock (vtable, size);
	if (G_UNLIKELY (!arr)) {
		UNLOCK_GC;
		return mono_gc_out_of_memory (size);
	}

	arr->max_length = max_length;

	bounds = (MonoArrayBounds*)((char*)arr + size - bounds_size);
	arr->bounds = bounds;

	UNLOCK_GC;

	return arr;
}

void*
mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len)
{
	MonoString *str;
#ifndef DISABLE_CRITICAL_REGION
	TLAB_ACCESS_INIT;
	ENTER_CRITICAL_REGION;
	str = mono_gc_try_alloc_obj_nolock (vtable, size);
	if (str) {
		/*This doesn't require fencing since EXIT_CRITICAL_REGION already does it for us*/
		str->length = len;
		EXIT_CRITICAL_REGION;
		return str;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	str = mono_gc_alloc_obj_nolock (vtable, size);
	if (G_UNLIKELY (!str)) {
		UNLOCK_GC;
		return mono_gc_out_of_memory (size);
	}

	str->length = len;

	UNLOCK_GC;

	return str;
}

/*
 * To be used for interned strings and possibly MonoThread, reflection handles.
 * We may want to explicitly free these objects.
 */
void*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	void **p;
	size = ALIGN_UP (size);
	LOCK_GC;

	if (size > SGEN_MAX_SMALL_OBJ_SIZE) {
		/* large objects are always pinned anyway */
		p = sgen_los_alloc_large_inner (vtable, size);
	} else {
		SGEN_ASSERT (9, vtable->klass->inited, "class %s:%s is not initialized", vtable->klass->name_space, vtable->klass->name);
		p = major_collector.alloc_small_pinned_obj (vtable, size, SGEN_VTABLE_HAS_REFERENCES (vtable));
	}
	if (G_LIKELY (p)) {
		SGEN_LOG (6, "Allocated pinned object %p, vtable: %p (%s), size: %zd", p, vtable, vtable->klass->name, size);
		if (size > SGEN_MAX_SMALL_OBJ_SIZE)
			MONO_GC_MAJOR_OBJ_ALLOC_LARGE ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
		else
			MONO_GC_MAJOR_OBJ_ALLOC_PINNED ((mword)p, size, vtable->klass->name_space, vtable->klass->name);
		binary_protocol_alloc_pinned (p, vtable, size);
	}
	UNLOCK_GC;
	return p;
}

void*
mono_gc_alloc_mature (MonoVTable *vtable)
{
	void **res;
	size_t size = ALIGN_UP (vtable->klass->instance_size);
	LOCK_GC;
	res = alloc_degraded (vtable, size, TRUE);
	UNLOCK_GC;
	if (G_UNLIKELY (vtable->klass->has_finalize))
		mono_object_register_finalizer ((MonoObject*)res);

	return res;
}

void*
mono_gc_alloc_fixed (size_t size, void *descr)
{
	/* FIXME: do a single allocation */
	void *res = calloc (1, size);
	if (!res)
		return NULL;
	if (!mono_gc_register_root (res, size, descr)) {
		free (res);
		res = NULL;
	}
	return res;
}

void
mono_gc_free_fixed (void* addr)
{
	mono_gc_deregister_root (addr);
	free (addr);
}

void
sgen_init_tlab_info (SgenThreadInfo* info)
{
#ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = info;
#endif

	info->tlab_start_addr = &TLAB_START;
	info->tlab_next_addr = &TLAB_NEXT;
	info->tlab_temp_end_addr = &TLAB_TEMP_END;
	info->tlab_real_end_addr = &TLAB_REAL_END;

#ifdef HAVE_KW_THREAD
	tlab_next_addr = &tlab_next;
#endif
}

/*
 * Clear the thread local TLAB variables for all threads.
 */
void
sgen_clear_tlabs (void)
{
	SgenThreadInfo *info;

	FOREACH_THREAD (info) {
		/* A new TLAB will be allocated when the thread does its first allocation */
		*info->tlab_start_addr = NULL;
		*info->tlab_next_addr = NULL;
		*info->tlab_temp_end_addr = NULL;
		*info->tlab_real_end_addr = NULL;
	} END_FOREACH_THREAD
}

static MonoMethod* alloc_method_cache [ATYPE_NUM];

#ifdef MANAGED_ALLOCATION
/* FIXME: Do this in the JIT, where specialized allocation sequences can be created
 * for each class. This is currently not easy to do, as it is hard to generate basic 
 * blocks + branches, but it is easy with the linear IL codebase.
 *
 * For this to work we'd need to solve the TLAB race, first.  Now we
 * require the allocator to be in a few known methods to make sure
 * that they are executed atomically via the restart mechanism.
 */
static MonoMethod*
create_allocator (int atype)
{
	int p_var, size_var;
	guint32 slowpath_branch, max_size_branch;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;
	static gboolean registered = FALSE;
	int tlab_next_addr_var, new_next_var;
	int num_params, i;
	const char *name = NULL;
	AllocatorWrapperInfo *info;

#ifdef HAVE_KW_THREAD
	int tlab_next_addr_offset = -1;
	int tlab_temp_end_offset = -1;

	MONO_THREAD_VAR_OFFSET (tlab_next_addr, tlab_next_addr_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	g_assert (tlab_next_addr_offset != -1);
	g_assert (tlab_temp_end_offset != -1);
#endif

	if (!registered) {
		mono_register_jit_icall (mono_gc_alloc_obj, "mono_gc_alloc_obj", mono_create_icall_signature ("object ptr int"), FALSE);
		mono_register_jit_icall (mono_gc_alloc_vector, "mono_gc_alloc_vector", mono_create_icall_signature ("object ptr int int"), FALSE);
		mono_register_jit_icall (mono_gc_alloc_string, "mono_gc_alloc_string", mono_create_icall_signature ("object ptr int int32"), FALSE);
		registered = TRUE;
	}

	if (atype == ATYPE_SMALL) {
		num_params = 1;
		name = "AllocSmall";
	} else if (atype == ATYPE_NORMAL) {
		num_params = 1;
		name = "Alloc";
	} else if (atype == ATYPE_VECTOR) {
		num_params = 2;
		name = "AllocVector";
	} else if (atype == ATYPE_STRING) {
		num_params = 2;
		name = "AllocString";
	} else {
		g_assert_not_reached ();
	}

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, num_params);
	if (atype == ATYPE_STRING) {
		csig->ret = &mono_defaults.string_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int32_class->byval_arg;
	} else {
		csig->ret = &mono_defaults.object_class->byval_arg;
		for (i = 0; i < num_params; ++i)
			csig->params [i] = &mono_defaults.int_class->byval_arg;
	}

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ALLOC);

#ifndef DISABLE_JIT
	size_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	if (atype == ATYPE_NORMAL || atype == ATYPE_SMALL) {
		/* size = vtable->klass->instance_size; */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoClass, instance_size));
		mono_mb_emit_byte (mb, CEE_ADD);
		/* FIXME: assert instance_size stays a 4 byte integer */
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_stloc (mb, size_var);
	} else if (atype == ATYPE_VECTOR) {
		MonoExceptionClause *clause;
		int pos, pos_leave;
		MonoClass *oom_exc_class;
		MonoMethod *ctor;

		/* n > 	MONO_ARRAY_MAX_INDEX -> OverflowException */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, MONO_ARRAY_MAX_INDEX);
		pos = mono_mb_emit_short_branch (mb, CEE_BLE_UN_S);
		mono_mb_emit_exception (mb, "OverflowException", NULL);
		mono_mb_patch_short_branch (mb, pos);

		clause = mono_image_alloc0 (mono_defaults.corlib, sizeof (MonoExceptionClause));
		clause->try_offset = mono_mb_get_label (mb);

		/* vtable->klass->sizes.element_size */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoClass, sizes.element_size));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_U4);

		/* * n */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, CEE_MUL_OVF_UN);
		/* + sizeof (MonoArray) */
		mono_mb_emit_icon (mb, sizeof (MonoArray));
		mono_mb_emit_byte (mb, CEE_ADD_OVF_UN);
		mono_mb_emit_stloc (mb, size_var);

		pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

		/* catch */
		clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
		clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
		clause->data.catch_class = mono_class_from_name (mono_defaults.corlib,
				"System", "OverflowException");
		g_assert (clause->data.catch_class);
		clause->handler_offset = mono_mb_get_label (mb);

		oom_exc_class = mono_class_from_name (mono_defaults.corlib,
				"System", "OutOfMemoryException");
		g_assert (oom_exc_class);
		ctor = mono_class_get_method_from_name (oom_exc_class, ".ctor", 0);
		g_assert (ctor);

		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
		mono_mb_emit_byte (mb, CEE_THROW);

		clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;
		mono_mb_set_clauses (mb, 1, clause);
		mono_mb_patch_branch (mb, pos_leave);
		/* end catch */
	} else if (atype == ATYPE_STRING) {
		/* a string allocator method takes the args: (vtable, len) */
		/* bytes = (sizeof (MonoString) + ((len + 1) * 2)); */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_SHL);
		//WE manually fold the above + 2 here
		mono_mb_emit_icon (mb, sizeof (MonoString) + 2);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, size_var);
	} else {
		g_assert_not_reached ();
	}

	/* size += ALLOC_ALIGN - 1; */
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_icon (mb, ALLOC_ALIGN - 1);
	mono_mb_emit_byte (mb, CEE_ADD);
	/* size &= ~(ALLOC_ALIGN - 1); */
	mono_mb_emit_icon (mb, ~(ALLOC_ALIGN - 1));
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_stloc (mb, size_var);

	/* if (size > MAX_SMALL_OBJ_SIZE) goto slowpath */
	if (atype != ATYPE_SMALL) {
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_icon (mb, MAX_SMALL_OBJ_SIZE);
		max_size_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BGT_UN_S);
	}

	/*
	 * We need to modify tlab_next, but the JIT only supports reading, so we read
	 * another tls var holding its address instead.
	 */

	/* tlab_next_addr (local) = tlab_next_addr (TLS var) */
	tlab_next_addr_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	EMIT_TLS_ACCESS (mb, tlab_next_addr, tlab_next_addr_offset);
	mono_mb_emit_stloc (mb, tlab_next_addr_var);

	/* p = (void**)tlab_next; */
	p_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, p_var);
	
	/* new_next = (char*)p + size; */
	new_next_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_stloc (mb, new_next_var);

	/* if (G_LIKELY (new_next < tlab_temp_end)) */
	mono_mb_emit_ldloc (mb, new_next_var);
	EMIT_TLS_ACCESS (mb, tlab_temp_end, tlab_temp_end_offset);
	slowpath_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BLT_UN_S);

	/* Slowpath */
	if (atype != ATYPE_SMALL)
		mono_mb_patch_short_branch (mb, max_size_branch);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	/* FIXME: mono_gc_alloc_obj takes a 'size_t' as an argument, not an int32 */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, size_var);
	if (atype == ATYPE_NORMAL || atype == ATYPE_SMALL) {
		mono_mb_emit_icall (mb, mono_gc_alloc_obj);
	} else if (atype == ATYPE_VECTOR) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_gc_alloc_vector);
	} else if (atype == ATYPE_STRING) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_gc_alloc_string);
	} else {
		g_assert_not_reached ();
	}
	mono_mb_emit_byte (mb, CEE_RET);

	/* Fastpath */
	mono_mb_patch_short_branch (mb, slowpath_branch);

	/* FIXME: Memory barrier */

	/* tlab_next = new_next */
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_ldloc (mb, new_next_var);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/*The tlab store must be visible before the the vtable store. This could be replaced with a DDS but doing it with IL would be tricky. */
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_MEMORY_BARRIER, StoreStoreBarrier);

	/* *p = vtable; */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	if (atype == ATYPE_VECTOR) {
		/* arr->max_length = max_length; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoArray, max_length));
		mono_mb_emit_ldarg (mb, 1);
#ifdef MONO_BIG_ARRAYS
		mono_mb_emit_byte (mb, CEE_STIND_I);
#else
		mono_mb_emit_byte (mb, CEE_STIND_I4);
#endif
	} else 	if (atype == ATYPE_STRING) {
		/* need to set length and clear the last char */
		/* s->length = len; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoString, length));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I4);
		/* s->chars [len] = 0; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_icon (mb, 2);
		mono_mb_emit_byte (mb, MONO_CEE_SUB);
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I2);
	}

	/*
	We must make sure both vtable and max_length are globaly visible before returning to managed land.
	*/
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_MEMORY_BARRIER, StoreStoreBarrier);

	/* return p */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	res = mono_mb_create_method (mb, csig, 8);
	mono_mb_free (mb);
	mono_method_get_header (res)->init_locals = FALSE;

	info = mono_image_alloc0 (mono_defaults.corlib, sizeof (AllocatorWrapperInfo));
	info->gc_name = "sgen";
	info->alloc_type = atype;
	mono_marshal_set_wrapper_info (res, info);

	return res;
}
#endif

/*
 * Generate an allocator method implementing the fast path of mono_gc_alloc_obj ().
 * The signature of the called method is:
 * 	object allocate (MonoVTable *vtable)
 */
MonoMethod*
mono_gc_get_managed_allocator (MonoClass *klass, gboolean for_box)
{
#ifdef MANAGED_ALLOCATION

#ifdef HAVE_KW_THREAD
	int tlab_next_offset = -1;
	int tlab_temp_end_offset = -1;
	MONO_THREAD_VAR_OFFSET (tlab_next, tlab_next_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	if (tlab_next_offset == -1 || tlab_temp_end_offset == -1)
		return NULL;
#endif

	if (!mono_runtime_has_tls_get ())
		return NULL;
	if (klass->instance_size > tlab_size)
		return NULL;

	if (klass->has_finalize || mono_class_is_marshalbyref (klass) || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return NULL;
	if (klass->rank)
		return NULL;
	if (klass->byval_arg.type == MONO_TYPE_STRING)
		return mono_gc_get_managed_allocator_by_type (ATYPE_STRING);
	if (collect_before_allocs)
		return NULL;
	/* Generic classes have dynamic field and can go above MAX_SMALL_OBJ_SIZE. */
	if (ALIGN_TO (klass->instance_size, ALLOC_ALIGN) < MAX_SMALL_OBJ_SIZE && !mono_class_is_open_constructed_type (&klass->byval_arg))
		return mono_gc_get_managed_allocator_by_type (ATYPE_SMALL);
	else
		return mono_gc_get_managed_allocator_by_type (ATYPE_NORMAL);
#else
	return NULL;
#endif
}

MonoMethod*
mono_gc_get_managed_array_allocator (MonoClass *klass)
{
#ifdef MANAGED_ALLOCATION
#ifdef HAVE_KW_THREAD
	int tlab_next_offset = -1;
	int tlab_temp_end_offset = -1;
	MONO_THREAD_VAR_OFFSET (tlab_next, tlab_next_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	if (tlab_next_offset == -1 || tlab_temp_end_offset == -1)
		return NULL;
#endif

	if (klass->rank != 1)
		return NULL;
	if (!mono_runtime_has_tls_get ())
		return NULL;
	if (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS)
		return NULL;
	if (has_per_allocation_action)
		return NULL;
	g_assert (!mono_class_has_finalizer (klass) && !mono_class_is_marshalbyref (klass));

	return mono_gc_get_managed_allocator_by_type (ATYPE_VECTOR);
#else
	return NULL;
#endif
}

void
sgen_set_use_managed_allocator (gboolean flag)
{
	use_managed_allocator = flag;
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype)
{
#ifdef MANAGED_ALLOCATION
	MonoMethod *res;

	if (!use_managed_allocator)
		return NULL;

	if (!mono_runtime_has_tls_get ())
		return NULL;

	mono_loader_lock ();
	res = alloc_method_cache [atype];
	if (!res)
		res = alloc_method_cache [atype] = create_allocator (atype);
	mono_loader_unlock ();
	return res;
#else
	return NULL;
#endif
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return ATYPE_NUM;
}

gboolean
sgen_is_managed_allocator (MonoMethod *method)
{
	int i;

	for (i = 0; i < ATYPE_NUM; ++i)
		if (method == alloc_method_cache [i])
			return TRUE;
	return FALSE;
}

gboolean
sgen_has_managed_allocator (void)
{
	int i;

	for (i = 0; i < ATYPE_NUM; ++i)
		if (alloc_method_cache [i])
			return TRUE;
	return FALSE;
}	

#ifdef HEAVY_STATISTICS
void
sgen_alloc_init_heavy_stats (void)
{
	mono_counters_register ("# objects allocated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_objects_alloced);	
	mono_counters_register ("bytes allocated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_bytes_alloced);
	mono_counters_register ("bytes allocated in LOS", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_bytes_alloced_los);
}
#endif

#endif /*HAVE_SGEN_GC*/
