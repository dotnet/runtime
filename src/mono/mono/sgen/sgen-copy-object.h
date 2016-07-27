/*
 * sgen-copy-object.h: This is where objects are copied.
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
 * Copies an object and enqueues it if a queue is given.
 *
 * This function can be used even if the vtable of obj is not valid
 * anymore, which is the case in the parallel collector.
 */
static MONO_ALWAYS_INLINE void
par_copy_object_no_checks (char *destination, GCVTable vt, void *obj, mword objsize, SgenGrayQueue *queue)
{
	sgen_client_pre_copy_checks (destination, vt, obj, objsize);
	binary_protocol_copy (obj, destination, vt, objsize);

	/* FIXME: assumes object layout */
	memcpy ((char*)destination + sizeof (mword), (char*)obj + sizeof (mword), objsize - sizeof (mword));

	/* adjust array->bounds */
	SGEN_ASSERT (9, sgen_vtable_get_descriptor (vt), "vtable %p has no gc descriptor", vt);

	sgen_client_update_copied_object (destination, vt, obj, objsize);
	obj = destination;
	if (queue) {
		SGEN_LOG (9, "Enqueuing gray object %p (%s)", obj, sgen_client_vtable_get_name (vt));
		GRAY_OBJECT_ENQUEUE (queue, (GCObject *)obj, sgen_vtable_get_descriptor (vt));
	}
}

/*
 * This can return OBJ itself on OOM.
 */
static MONO_NEVER_INLINE GCObject *
copy_object_no_checks (GCObject *obj, SgenGrayQueue *queue)
{
	GCVTable vt = SGEN_LOAD_VTABLE_UNCHECKED (obj);
	gboolean has_references = SGEN_VTABLE_HAS_REFERENCES (vt);
	mword objsize = SGEN_ALIGN_UP (sgen_client_par_object_get_size (vt, obj));
	/* FIXME: Does this not mark the newly allocated object? */
	void *destination = COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION (vt, obj, objsize, has_references);

	if (G_UNLIKELY (!destination)) {
		/* FIXME: Is this path ever tested? */
		collector_pin_object (obj, queue);
		sgen_set_pinned_from_failed_allocation (objsize);
		return obj;
	}

	if (!has_references)
		queue = NULL;

	par_copy_object_no_checks ((char *)destination, vt, obj, objsize, queue);
	/* FIXME: mark mod union cards if necessary */

	/* set the forwarding pointer */
	SGEN_FORWARD_OBJECT (obj, destination);

	return (GCObject *)destination;
}

#undef COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION
#undef collector_pin_object
