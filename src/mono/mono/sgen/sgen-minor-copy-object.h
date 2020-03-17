/**
 * \file
 * Copy functions for nursery collections.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#undef SERIAL_COPY_OBJECT
#undef SERIAL_COPY_OBJECT_FROM_OBJ

#if defined(SGEN_SIMPLE_NURSERY)

#ifdef SGEN_SIMPLE_PAR_NURSERY

#ifdef SGEN_CONCURRENT_MAJOR
#define SERIAL_COPY_OBJECT simple_par_nursery_with_concurrent_major_copy_object
#define SERIAL_COPY_OBJECT_FROM_OBJ simple_par_nursery_with_concurrent_major_copy_object_from_obj
#else
#define SERIAL_COPY_OBJECT simple_par_nursery_copy_object
#define SERIAL_COPY_OBJECT_FROM_OBJ simple_par_nursery_copy_object_from_obj
#endif

#else

#ifdef SGEN_CONCURRENT_MAJOR
#define SERIAL_COPY_OBJECT simple_nursery_serial_with_concurrent_major_copy_object
#define SERIAL_COPY_OBJECT_FROM_OBJ simple_nursery_serial_with_concurrent_major_copy_object_from_obj
#else
#define SERIAL_COPY_OBJECT simple_nursery_serial_copy_object
#define SERIAL_COPY_OBJECT_FROM_OBJ simple_nursery_serial_copy_object_from_obj
#endif

#endif

#elif defined (SGEN_SPLIT_NURSERY)

#ifdef SGEN_CONCURRENT_MAJOR
#define SERIAL_COPY_OBJECT split_nursery_serial_with_concurrent_major_copy_object
#define SERIAL_COPY_OBJECT_FROM_OBJ split_nursery_serial_with_concurrent_major_copy_object_from_obj
#else
#define SERIAL_COPY_OBJECT split_nursery_serial_copy_object
#define SERIAL_COPY_OBJECT_FROM_OBJ split_nursery_serial_copy_object_from_obj
#endif

#else
#error "No nursery configuration specified"
#endif


extern guint64 stat_nursery_copy_object_failed_to_space; /* from sgen-gc.c */

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

static MONO_ALWAYS_INLINE void
SERIAL_COPY_OBJECT (GCObject **obj_slot, SgenGrayQueue *queue) 
{
	GCObject *forwarded;
	GCObject *copy;
	GCObject *obj = *obj_slot;

	SGEN_ASSERT (9, sgen_current_collection_generation == GENERATION_NURSERY, "calling minor-serial-copy from a %d generation collection", sgen_current_collection_generation);

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
		SGEN_ASSERT (9, sgen_obj_get_descriptor (forwarded),  "forwarded object %p has no gc descriptor", forwarded);
		SGEN_LOG (9, " (already forwarded to %p)", forwarded);
		HEAVY_STAT (++stat_nursery_copy_object_failed_forwarded);
		SGEN_UPDATE_REFERENCE (obj_slot, forwarded);
		return;
	}
	if (G_UNLIKELY (SGEN_OBJECT_IS_PINNED (obj))) {
		SGEN_ASSERT (9, sgen_vtable_get_descriptor (SGEN_LOAD_VTABLE(obj)), "pinned object %p has no gc descriptor", obj);
		SGEN_LOG (9, " (pinned, no change)");
		HEAVY_STAT (++stat_nursery_copy_object_failed_pinned);
		return;
	}

#ifndef SGEN_SIMPLE_NURSERY
	if (sgen_nursery_is_to_space (obj)) {
		SGEN_ASSERT (9, sgen_vtable_get_descriptor (SGEN_LOAD_VTABLE(obj)), "to space object %p has no gc descriptor", obj);
		SGEN_LOG (9, " (tospace, no change)");
		HEAVY_STAT (++stat_nursery_copy_object_failed_to_space);		
		return;
	}
#endif

	HEAVY_STAT (++stat_objects_copied_nursery);

#ifdef SGEN_SIMPLE_PAR_NURSERY
	copy = copy_object_no_checks_par (obj, queue);
#else
	copy = copy_object_no_checks (obj, queue);
#endif
	SGEN_UPDATE_REFERENCE (obj_slot, copy);
}

/*
 * SERIAL_COPY_OBJECT_FROM_OBJ:
 *
 *   Similar to SERIAL_COPY_OBJECT, but assumes that OBJ_SLOT is part of an object, so it handles global remsets as well.
 */
static MONO_ALWAYS_INLINE void
SERIAL_COPY_OBJECT_FROM_OBJ (GCObject **obj_slot, SgenGrayQueue *queue)
{
	GCObject *forwarded;
	GCObject *obj = *obj_slot;
	GCObject *copy;

	SGEN_ASSERT (9, sgen_current_collection_generation == GENERATION_NURSERY, "calling minor-serial-copy-from-obj from a %d generation collection", sgen_current_collection_generation);

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
		SGEN_ASSERT (9, sgen_obj_get_descriptor (forwarded),  "forwarded object %p has no gc descriptor", forwarded);
		SGEN_LOG (9, " (already forwarded to %p)", forwarded);
		HEAVY_STAT (++stat_nursery_copy_object_failed_forwarded);
#ifdef SGEN_CONCURRENT_MAJOR
		/* See comment on STORE_STORE_FENCE below. */
		STORE_STORE_FENCE;
#endif
		SGEN_UPDATE_REFERENCE (obj_slot, forwarded);
#ifndef SGEN_SIMPLE_NURSERY
		if (G_UNLIKELY (sgen_ptr_in_nursery (forwarded) && !sgen_ptr_in_nursery (obj_slot) && !SGEN_OBJECT_IS_CEMENTED (forwarded)))
			sgen_add_to_global_remset (obj_slot, forwarded);
#endif
		return;
	}
	if (G_UNLIKELY (SGEN_OBJECT_IS_PINNED (obj))) {
		SGEN_ASSERT (9, sgen_vtable_get_descriptor (SGEN_LOAD_VTABLE(obj)), "pinned object %p has no gc descriptor", obj);
		SGEN_LOG (9, " (pinned, no change)");
		HEAVY_STAT (++stat_nursery_copy_object_failed_pinned);
		if (!sgen_ptr_in_nursery (obj_slot) && !SGEN_OBJECT_IS_CEMENTED (obj))
			sgen_add_to_global_remset (obj_slot, obj);
		return;
	}

#ifndef SGEN_SIMPLE_NURSERY
	if (sgen_nursery_is_to_space (obj)) {
		/* FIXME: all of these could just use `sgen_obj_get_descriptor_safe()` */
		SGEN_ASSERT (9, sgen_vtable_get_descriptor (SGEN_LOAD_VTABLE(obj)), "to space object %p has no gc descriptor", obj);
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
		if (!sgen_ptr_in_nursery (obj_slot) && !SGEN_OBJECT_IS_CEMENTED (obj))
			sgen_add_to_global_remset (obj_slot, obj);

		return;
	}
#endif

	HEAVY_STAT (++stat_objects_copied_nursery);

#ifdef SGEN_SIMPLE_PAR_NURSERY
	copy = copy_object_no_checks_par (obj, queue);
#else
	copy = copy_object_no_checks (obj, queue);
#endif
#ifdef SGEN_CONCURRENT_MAJOR
	/*
	 * If an object is evacuated to the major heap and a reference to it, from the major
	 * heap, updated, the concurrent major collector might follow that reference and
	 * scan the new major object.  To make sure the object contents are seen by the
	 * major collector we need this write barrier, so that the reference is seen after
	 * the object.
	 */
	STORE_STORE_FENCE;
#endif
	SGEN_UPDATE_REFERENCE (obj_slot, copy);
#ifndef SGEN_SIMPLE_NURSERY
	if (G_UNLIKELY (sgen_ptr_in_nursery (copy) && !sgen_ptr_in_nursery (obj_slot) && !SGEN_OBJECT_IS_CEMENTED (copy)))
		sgen_add_to_global_remset (obj_slot, copy);
#else
	/* copy_object_no_checks () can return obj on OOM */
	if (G_UNLIKELY (obj == copy)) {
		if (G_UNLIKELY (sgen_ptr_in_nursery (copy) && !sgen_ptr_in_nursery (obj_slot) && !SGEN_OBJECT_IS_CEMENTED (copy)))
			sgen_add_to_global_remset (obj_slot, copy);
	}
#endif
}
