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
extern long long stat_copy_object_called_nursery;
extern long long stat_objects_copied_nursery;

extern long long stat_nursery_copy_object_failed_from_space;
extern long long stat_nursery_copy_object_failed_forwarded;
extern long long stat_nursery_copy_object_failed_pinned;

extern long long stat_slots_allocated_in_vain;

/*
 * This function can be used even if the vtable of obj is not valid
 * anymore, which is the case in the parallel collector.
 */
static inline void
par_copy_object_no_checks (char *destination, MonoVTable *vt, void *obj, mword objsize, SgenGrayQueue *queue)
{
#ifdef __GNUC__
	static const void *copy_labels [] = { &&LAB_0, &&LAB_1, &&LAB_2, &&LAB_3, &&LAB_4, &&LAB_5, &&LAB_6, &&LAB_7, &&LAB_8 };
#endif

	SGEN_ASSERT (9, vt->klass->inited, "vtable %p for class %s:%s was not initialized", vt, vt->klass->name_space, vt->klass->name);
	SGEN_LOG (9, " (to %p, %s size: %lu)", destination, ((MonoObject*)obj)->vtable->klass->name, (unsigned long)objsize);
	binary_protocol_copy (obj, destination, vt, objsize);

	if (G_UNLIKELY (MONO_GC_OBJ_MOVED_ENABLED ())) {
		int dest_gen = sgen_ptr_in_nursery (destination) ? GENERATION_NURSERY : GENERATION_OLD;
		int src_gen = sgen_ptr_in_nursery (obj) ? GENERATION_NURSERY : GENERATION_OLD;
		MONO_GC_OBJ_MOVED ((mword)destination, (mword)obj, dest_gen, src_gen, objsize, vt->klass->name_space, vt->klass->name);
	}

#ifdef __GNUC__
	if (objsize <= sizeof (gpointer) * 8) {
		mword *dest = (mword*)destination;
		goto *copy_labels [objsize / sizeof (gpointer)];
	LAB_8:
		(dest) [7] = ((mword*)obj) [7];
	LAB_7:
		(dest) [6] = ((mword*)obj) [6];
	LAB_6:
		(dest) [5] = ((mword*)obj) [5];
	LAB_5:
		(dest) [4] = ((mword*)obj) [4];
	LAB_4:
		(dest) [3] = ((mword*)obj) [3];
	LAB_3:
		(dest) [2] = ((mword*)obj) [2];
	LAB_2:
		(dest) [1] = ((mword*)obj) [1];
	LAB_1:
		;
	LAB_0:
		;
	} else {
		/*can't trust memcpy doing word copies */
		mono_gc_memmove (destination + sizeof (mword), (char*)obj + sizeof (mword), objsize - sizeof (mword));
	}
#else
		mono_gc_memmove (destination + sizeof (mword), (char*)obj + sizeof (mword), objsize - sizeof (mword));
#endif
	/* adjust array->bounds */
	SGEN_ASSERT (9, vt->gc_descr, "vtable %p for class %s:%s has no gc descriptor", vt, vt->klass->name_space, vt->klass->name);

	if (G_UNLIKELY (vt->rank && ((MonoArray*)obj)->bounds)) {
		MonoArray *array = (MonoArray*)destination;
		array->bounds = (MonoArrayBounds*)((char*)destination + ((char*)((MonoArray*)obj)->bounds - (char*)obj));
		SGEN_LOG (9, "Array instance %p: size: %lu, rank: %d, length: %lu", array, (unsigned long)objsize, vt->rank, (unsigned long)mono_array_length (array));
	}
	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES))
		sgen_register_moved_object (obj, destination);
	obj = destination;
	if (queue) {
		SGEN_LOG (9, "Enqueuing gray object %p (%s)", obj, sgen_safe_name (obj));
		GRAY_OBJECT_ENQUEUE (queue, obj);
	}
}

/*
 * This can return OBJ itself on OOM.
 */
#ifdef _MSC_VER
static __declspec(noinline) void*
#else
static G_GNUC_UNUSED void* __attribute__((noinline))
#endif
copy_object_no_checks (void *obj, SgenGrayQueue *queue)
{
	MonoVTable *vt = ((MonoObject*)obj)->vtable;
	gboolean has_references = SGEN_VTABLE_HAS_REFERENCES (vt);
	mword objsize = SGEN_ALIGN_UP (sgen_par_object_get_size (vt, (MonoObject*)obj));
	char *destination = COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION (obj, objsize, has_references);

	if (G_UNLIKELY (!destination)) {
		collector_pin_object (obj, queue);
		sgen_set_pinned_from_failed_allocation (objsize);
		return obj;
	}

	*(MonoVTable**)destination = vt;
	par_copy_object_no_checks (destination, vt, obj, objsize, has_references ? queue : NULL);

	/* set the forwarding pointer */
	SGEN_FORWARD_OBJECT (obj, destination);

	return destination;
}
