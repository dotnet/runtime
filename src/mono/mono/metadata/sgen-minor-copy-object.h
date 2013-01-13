/*
 * sgen-minor-copy-object.h: Copy functions for nursery collections.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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

	SGEN_ASSERT (9, current_collection_generation == GENERATION_NURSERY, "calling minor-serial-copy from a %d generation collection", current_collection_generation);

	HEAVY_STAT (++stat_copy_object_called_nursery);

	if (!sgen_ptr_in_nursery (obj)) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_from_space);
		return;
	}

	SGEN_LOG (9, "Precise copy of %p from %p", obj, obj_slot);

	/*
	 * Before we can copy the object we must make sure that we are
	 * allowed to, i.e. that the object not pinned, not already
	 * forwarded or belongs to the nursery To Space.
	 */

	if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
		SGEN_ASSERT (9, (*(MonoVTable**)SGEN_LOAD_VTABLE (obj))->gc_descr,  "forwarded object %p has no gc descriptor", forwarded);
		SGEN_LOG (9, " (already forwarded to %p)", forwarded);
		HEAVY_STAT (++stat_nursery_copy_object_failed_forwarded);
		*obj_slot = forwarded;
		return;
	}
	if (G_UNLIKELY (SGEN_OBJECT_IS_PINNED (obj))) {
		SGEN_ASSERT (9, ((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr, "pinned object %p has no gc descriptor", obj);
		SGEN_LOG (9, " (pinned, no change)");
		HEAVY_STAT (++stat_nursery_copy_object_failed_pinned);
		return;
	}

#ifndef SGEN_SIMPLE_NURSERY
	if (sgen_nursery_is_to_space (obj)) {
		SGEN_ASSERT (9, ((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr, "to space object %p has no gc descriptor", obj);
		SGEN_LOG (9, " (tospace, no change)");
		HEAVY_STAT (++stat_nursery_copy_object_failed_to_space);		
		return;
	}
#endif

	HEAVY_STAT (++stat_objects_copied_nursery);

	*obj_slot = copy_object_no_checks (obj, queue);
}

/*
 * SERIAL_COPY_OBJECT_FROM_OBJ:
 *
 *   Similar to SERIAL_COPY_OBJECT, but assumes that OBJ_SLOT is part of an object, so it handles global remsets as well.
 */
#ifdef _MSC_VER
static __forceinline void
#else
static inline void __attribute__((always_inline))
#endif
SERIAL_COPY_OBJECT_FROM_OBJ (void **obj_slot, SgenGrayQueue *queue) 
{
	char *forwarded;
	char *obj = *obj_slot;
	void *copy;

	SGEN_ASSERT (9, current_collection_generation == GENERATION_NURSERY, "calling minor-serial-copy-from-obj from a %d generation collection", current_collection_generation);

	HEAVY_STAT (++stat_copy_object_called_nursery);

	if (!sgen_ptr_in_nursery (obj)) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_from_space);
		return;
	}

	SGEN_LOG (9, "Precise copy of %p from %p", obj, obj_slot);

	/*
	 * Before we can copy the object we must make sure that we are
	 * allowed to, i.e. that the object not pinned, not already
	 * forwarded or belongs to the nursery To Space.
	 */

	if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
		SGEN_ASSERT (9, (*(MonoVTable**)SGEN_LOAD_VTABLE (obj))->gc_descr,  "forwarded object %p has no gc descriptor", forwarded);
		SGEN_LOG (9, " (already forwarded to %p)", forwarded);
		HEAVY_STAT (++stat_nursery_copy_object_failed_forwarded);
		*obj_slot = forwarded;
#ifndef SGEN_SIMPLE_NURSERY
		if (G_UNLIKELY (sgen_ptr_in_nursery (forwarded) && !sgen_ptr_in_nursery (obj_slot)))
			sgen_add_to_global_remset (obj_slot, forwarded);
#endif
		return;
	}
	if (G_UNLIKELY (SGEN_OBJECT_IS_PINNED (obj))) {
		SGEN_ASSERT (9, ((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr, "pinned object %p has no gc descriptor", obj);
		SGEN_LOG (9, " (pinned, no change)");
		HEAVY_STAT (++stat_nursery_copy_object_failed_pinned);
		if (!sgen_ptr_in_nursery (obj_slot))
			sgen_add_to_global_remset (obj_slot, obj);
		return;
	}

#ifndef SGEN_SIMPLE_NURSERY
	if (sgen_nursery_is_to_space (obj)) {
		SGEN_ASSERT (9, ((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr, "to space object %p has no gc descriptor", obj);
		SGEN_LOG (9, " (tospace, no change)");
		HEAVY_STAT (++stat_nursery_copy_object_failed_to_space);		

		/*
		 * FIXME:
		 *
		 * The card table scanning code sometimes clears cards
		 * that have just been set for a global remset.  In
		 * the split nursery the following situation can
		 * occur:
		 *
		 * Let's say object A starts in card C but continues
		 * into C+1.  Within A, at offset O there's a
		 * reference to a new nursery object X.  A+O is in
		 * card C+1.  Now card C is scanned, and as part of
		 * it, object A.  The reference at A+O is processed by
		 * copying X into nursery to-space at Y.  Since it's
		 * still in the nursery, a global remset must be added
		 * for A+O, so card C+1 is marked.  Now, however, card
		 * C+1 is scanned, which means that it's cleared
		 * first.  This wouldn't be terribly bad if reference
		 * A+O were re-scanned and the global remset re-added,
		 * but since the reference points to to-space, that
		 * doesn't happen, and C+1 remains cleared: the remset
		 * is lost.
		 *
		 * There's at least two ways to fix this.  The easy
		 * one is to re-add the remset on the re-scan.  This
		 * is that - the following two lines of code.
		 *
		 * The proper solution appears to be to first make a
		 * copy of the cards before scanning a block, then to
		 * clear all the cards and scan from the copy, so no
		 * remsets will be overwritten.  Scanning objects at
		 * most once would be the icing on the cake.
		 */
		if (!sgen_ptr_in_nursery (obj_slot))
			sgen_add_to_global_remset (obj_slot, obj);

		return;
	}
#endif

	HEAVY_STAT (++stat_objects_copied_nursery);

	copy = copy_object_no_checks (obj, queue);
	*obj_slot = copy;
#ifndef SGEN_SIMPLE_NURSERY
	if (G_UNLIKELY (sgen_ptr_in_nursery (copy) && !sgen_ptr_in_nursery (obj_slot)))
		sgen_add_to_global_remset (obj_slot, copy);
#else
	/* copy_object_no_checks () can return obj on OOM */
	if (G_UNLIKELY (obj == copy)) {
		if (G_UNLIKELY (sgen_ptr_in_nursery (copy) && !sgen_ptr_in_nursery (obj_slot)))
			sgen_add_to_global_remset (obj_slot, copy);
	}
#endif
}

static void
PARALLEL_COPY_OBJECT (void **obj_slot, SgenGrayQueue *queue)
{
	char *obj = *obj_slot;
	mword vtable_word, objsize;
	MonoVTable *vt;
	void *destination;
	gboolean has_references;

	SGEN_ASSERT (9, current_collection_generation == GENERATION_NURSERY, "calling minor-par-copy from a %d generation collection", current_collection_generation);

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

	destination = COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION (vt, obj, objsize, has_references);

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
