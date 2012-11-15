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

/*
 * This file defines copy functions for nursery collections.
 */

#define collector_pin_object(obj, queue) sgen_pin_object (obj, queue);
#define COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION alloc_for_promotion
#define COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION par_alloc_for_promotion

extern long long stat_nursery_copy_object_failed_to_space; /* from sgen-gc.c */

#include "sgen-copy-object.h"

/*
 * This is how the copying happens from the nursery to the old generation.
 * We assume that at this time all the pinned objects have been identified and
 * marked as such.
 * We run scan_object() for each pinned object so that each referenced
 * objects if possible are copied. The new gray objects created can have
 * scan_object() run on them right away, too.
 * Then we run copy_object() for the precisely tracked roots. At this point
 * all the roots are either gray or black. We run scan_object() on the gray
 * objects until no more gray objects are created.
 * At the end of the process we walk again the pinned list and we unmark
 * the pinned flag. As we go we also create the list of free space for use
 * in the next allocation runs.
 *
 * We need to remember objects from the old generation that point to the new one
 * (or just addresses?).
 *
 * copy_object could be made into a macro once debugged (use inline for now).
 */

#ifdef _MSC_VER
static __forceinline void
#else
static inline void __attribute__((always_inline))
#endif
SERIAL_COPY_OBJECT (void **obj_slot, SgenGrayQueue *queue) 
{
	char *forwarded;
	char *obj = *obj_slot;

	DEBUG (9, g_assert (current_collection_generation == GENERATION_NURSERY));

	HEAVY_STAT (++stat_copy_object_called_nursery);

	if (!sgen_ptr_in_nursery (obj)) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_from_space);
		return;
	}

	DEBUG (9, fprintf (gc_debug_file, "Precise copy of %p from %p", obj, obj_slot));

	/*
	 * Before we can copy the object we must make sure that we are
	 * allowed to, i.e. that the object not pinned, not already
	 * forwarded or belongs to the nursery To Space.
	 */

	if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
		DEBUG (9, g_assert ((*(MonoVTable**)SGEN_LOAD_VTABLE(obj))->gc_descr));
		DEBUG (9, fprintf (gc_debug_file, " (already forwarded to %p)\n", forwarded));
		HEAVY_STAT (++stat_nursery_copy_object_failed_forwarded);
		*obj_slot = forwarded;
		return;
	}
	if (SGEN_OBJECT_IS_PINNED (obj)) {
		DEBUG (9, g_assert (((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr));
		DEBUG (9, fprintf (gc_debug_file, " (pinned, no change)\n"));
		HEAVY_STAT (++stat_nursery_copy_object_failed_pinned);
		return;
	}

	if (sgen_nursery_is_to_space (obj)) {
		DEBUG (9, g_assert (((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr));
		DEBUG (9, fprintf (gc_debug_file, " (tospace, no change)\n"));
		HEAVY_STAT (++stat_nursery_copy_object_failed_to_space);		
		return;
	}

	HEAVY_STAT (++stat_objects_copied_nursery);

	*obj_slot = copy_object_no_checks (obj, queue);
}

static void
PARALLEL_COPY_OBJECT (void **obj_slot, SgenGrayQueue *queue)
{
	char *obj = *obj_slot;
	mword vtable_word, objsize;
	MonoVTable *vt;
	void *destination;
	gboolean has_references;

	DEBUG (9, g_assert (current_collection_generation == GENERATION_NURSERY));

	HEAVY_STAT (++stat_copy_object_called_nursery);

	if (!sgen_ptr_in_nursery (obj)) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_from_space);
		return;
	}

	vtable_word = *(mword*)obj;
	vt = (MonoVTable*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);

	/*
	 * Before we can copy the object we must make sure that we are
	 * allowed to, i.e. that the object not pinned, not already
	 * forwarded and not in the nursery To Space.
	 */

	if (vtable_word & SGEN_FORWARDED_BIT) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_forwarded);
		*obj_slot = vt;
		return;
	}
	if (vtable_word & SGEN_PINNED_BIT) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_pinned);
		return;
	}

	if (sgen_nursery_is_to_space (obj)) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_to_space);		
		return;
	}

	HEAVY_STAT (++stat_objects_copied_nursery);

	objsize = SGEN_ALIGN_UP (sgen_par_object_get_size (vt, (MonoObject*)obj));
	has_references = SGEN_VTABLE_HAS_REFERENCES (vt);

	destination = COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION (obj, objsize, has_references);

	if (G_UNLIKELY (!destination)) {
		sgen_parallel_pin_or_update (obj_slot, obj, vt, queue);
		return;
	}

	*(MonoVTable**)destination = vt;

	if (SGEN_CAS_PTR ((void*)obj, (void*)((mword)destination | SGEN_FORWARDED_BIT), vt) == vt) {
		par_copy_object_no_checks (destination, vt, obj, objsize, has_references ? queue : NULL);
		obj = destination;
		*obj_slot = obj;
	} else {
		/* FIXME: unify with code in major_copy_or_mark_object() */

		/* FIXME: Give destination back to the allocator. */
		/*The major collector only needs the first word zeroed and nursery requires all bits to be. */
		if (!sgen_ptr_in_nursery (destination))
			*(void**)destination = NULL;
		else
			memset (destination, 0, objsize);

		vtable_word = *(mword*)obj;
		g_assert (vtable_word & SGEN_FORWARDED_BIT);

		obj = (void*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);

		*obj_slot = obj;

		HEAVY_STAT (++stat_slots_allocated_in_vain);
	}
}

#define FILL_MINOR_COLLECTOR_COPY_OBJECT(collector)	do {			\
		(collector)->serial_ops.copy_or_mark_object = SERIAL_COPY_OBJECT;			\
		(collector)->parallel_ops.copy_or_mark_object = PARALLEL_COPY_OBJECT;	\
	} while (0)
