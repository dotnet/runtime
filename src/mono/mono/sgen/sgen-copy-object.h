/**
 * \file
 * This is where objects are copied.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * Defines
 *
 *     GCObject* copy_object_no_checks (GCObject *obj, SgenGrayQueue *queue)
 *
 * which allocates new space for `obj`, copies it there, forwards `obj` to its new location,
 * and enqueues the copy into `queue`.
 *
 * To be defined by the includer:
 *
 *     COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION(vt, obj, objsize, has_refs)
 *
 * Allocates space for promoting object `obj`, with size `objsize`, and initizializes the
 * vtable with `vt`.  `has_refs` indicates whether the object contains references.
 *
 *     collector_pin_object(obj, queue)
 *
 * Called when no space for `obj` could be allocated.  It must pin `obj` and enqueue it into
 * `queue` for scanning.
 */

extern guint64 stat_copy_object_called_nursery;
extern guint64 stat_objects_copied_nursery;

extern guint64 stat_nursery_copy_object_failed_from_space;
extern guint64 stat_nursery_copy_object_failed_forwarded;
extern guint64 stat_nursery_copy_object_failed_pinned;

extern guint64 stat_slots_allocated_in_vain;

/*
 * This function can be used even if the vtable of obj is not valid
 * anymore, which is the case in the parallel collector.
 */
static MONO_ALWAYS_INLINE void
par_copy_object_no_checks (char *destination, GCVTable vt, void *obj, mword objsize)
{
	sgen_client_pre_copy_checks (destination, vt, obj, objsize);
	sgen_binary_protocol_copy (obj, destination, vt, objsize);

	/* FIXME: assumes object layout */
	memcpy ((char*)destination + sizeof (mword), (char*)obj + sizeof (mword), objsize - sizeof (mword));

	/* adjust array->bounds */
	SGEN_ASSERT (9, sgen_vtable_get_descriptor (vt), "vtable %p has no gc descriptor", vt);

	sgen_client_update_copied_object (destination, vt, obj, objsize);
}

/*
 * Copies an object and enqueues it if a queue is given.
 * This can return OBJ itself on OOM.
 */
static MONO_NEVER_INLINE GCObject *
copy_object_no_checks (GCObject *obj, SgenGrayQueue *queue)
{
	GCVTable vt = SGEN_LOAD_VTABLE_UNCHECKED (obj);
	gboolean has_references = SGEN_VTABLE_HAS_REFERENCES (vt);
	mword objsize = SGEN_ALIGN_UP (sgen_client_par_object_get_size (vt, obj));
	void *destination = COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION (vt, obj, objsize, has_references);

	if (G_UNLIKELY (!destination)) {
		/* FIXME: Is this path ever tested? */
		collector_pin_object (obj, queue);
		sgen_set_pinned_from_failed_allocation (objsize);
		return obj;
	}

	par_copy_object_no_checks ((char *)destination, vt, obj, objsize);

	/* set the forwarding pointer */
	SGEN_FORWARD_OBJECT (obj, destination);

	if (has_references) {
		SGEN_LOG (9, "Enqueuing gray object %p (%s)", destination, sgen_client_vtable_get_name (vt));
		GRAY_OBJECT_ENQUEUE_SERIAL (queue, (GCObject *)destination, sgen_vtable_get_descriptor (vt));
	}

	return (GCObject *)destination;
}

#if defined(COPY_OR_MARK_PARALLEL)
static MONO_NEVER_INLINE GCObject *
copy_object_no_checks_par (GCObject *obj, SgenGrayQueue *queue)
{
	mword vtable_word = *(mword*)obj;
	GCObject *destination;

	destination = (GCObject*) SGEN_VTABLE_IS_FORWARDED (vtable_word);

	if (!destination) {
		GCVTable vt = (GCVTable) vtable_word;
		GCObject *final_destination;
		/*
		 * At this point we know vt is not tagged and we shouldn't access the vtable through obj
		 * since it could get copied at any time by another thread.
		 */
		gboolean has_references = SGEN_VTABLE_HAS_REFERENCES (vt);
		mword objsize = SGEN_ALIGN_UP (sgen_client_par_object_get_size (vt, obj));
		destination = COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION (vt, obj, objsize, has_references);

		par_copy_object_no_checks ((char*)destination, vt, obj, objsize);

		/* FIXME we might need a membar here so other threads see the vtable before we forward */

		/* set the forwarding pointer */
		SGEN_FORWARD_OBJECT_PAR (obj, destination, final_destination);

		if (destination == final_destination) {
			/* In a racing case, only the worker that allocated the object enqueues it */
			if (has_references) {
				SGEN_LOG (9, "Enqueuing gray object %p (%s)", destination, sgen_client_vtable_get_name (vt));
				GRAY_OBJECT_ENQUEUE_PARALLEL (queue, (GCObject *)destination, sgen_vtable_get_descriptor (vt));
			}
		} else {
			/*
			 * Unlikely case. Clear the allocated object so it doesn't confuse nursery
			 * card table scanning, since it can contain old invalid refs.
			 * FIXME make sure it is not a problem if another threads scans it while we clear
			 */
			mono_gc_bzero_aligned (destination, objsize);
			destination = final_destination;
		}
	}

	return destination;
}
#endif

#undef COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION
#undef COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION
#undef collector_pin_object
#undef COPY_OR_MARK_PARALLEL
