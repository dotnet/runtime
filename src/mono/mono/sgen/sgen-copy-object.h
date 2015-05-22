/*
 * sgen-copy-object.h: This is where objects are copied.
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
		GRAY_OBJECT_ENQUEUE (queue, obj, sgen_vtable_get_descriptor (vt));
	}
}

/*
 * This can return OBJ itself on OOM.
 */
static MONO_NEVER_INLINE void*
copy_object_no_checks (void *obj, SgenGrayQueue *queue)
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

	par_copy_object_no_checks (destination, vt, obj, objsize, queue);
	/* FIXME: mark mod union cards if necessary */

	/* set the forwarding pointer */
	SGEN_FORWARD_OBJECT (obj, destination);

	return destination;
}
