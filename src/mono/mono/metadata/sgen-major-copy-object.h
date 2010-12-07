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
extern long long stat_copy_object_called_nursery;
extern long long stat_objects_copied_nursery;

extern long long stat_nursery_copy_object_failed_from_space;
extern long long stat_nursery_copy_object_failed_forwarded;
extern long long stat_nursery_copy_object_failed_pinned;

/*
 * This function can be used even if the vtable of obj is not valid
 * anymore, which is the case in the parallel collector.
 */
static void
par_copy_object_no_checks (char *destination, MonoVTable *vt, void *obj, mword objsize, SgenGrayQueue *queue)
{
	static const void *copy_labels [] = { &&LAB_0, &&LAB_1, &&LAB_2, &&LAB_3, &&LAB_4, &&LAB_5, &&LAB_6, &&LAB_7, &&LAB_8 };

	DEBUG (9, g_assert (vt->klass->inited));
	DEBUG (9, fprintf (gc_debug_file, " (to %p, %s size: %lu)\n", destination, ((MonoObject*)obj)->vtable->klass->name, (unsigned long)objsize));
	binary_protocol_copy (obj, destination, vt, objsize);

	*(MonoVTable**)destination = vt;
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
		memcpy (destination + sizeof (mword), (char*)obj + sizeof (mword), objsize - sizeof (mword));
	}
	/* adjust array->bounds */
	DEBUG (9, g_assert (vt->gc_descr));
	if (G_UNLIKELY (vt->rank && ((MonoArray*)obj)->bounds)) {
		MonoArray *array = (MonoArray*)destination;
		array->bounds = (MonoArrayBounds*)((char*)destination + ((char*)((MonoArray*)obj)->bounds - (char*)obj));
		DEBUG (9, fprintf (gc_debug_file, "Array instance %p: size: %lu, rank: %d, length: %lu\n", array, (unsigned long)objsize, vt->rank, (unsigned long)mono_array_length (array)));
	}
	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES))
		mono_sgen_register_moved_object (obj, destination);
	obj = destination;
	if (queue) {
		DEBUG (9, fprintf (gc_debug_file, "Enqueuing gray object %p (%s)\n", obj, mono_sgen_safe_name (obj)));
		GRAY_OBJECT_ENQUEUE (queue, obj);
	}
}

static void*
copy_object_no_checks (void *obj, SgenGrayQueue *queue)
{
	MonoVTable *vt = ((MonoObject*)obj)->vtable;
	gboolean has_references = SGEN_VTABLE_HAS_REFERENCES (vt);
	mword objsize = SGEN_ALIGN_UP (mono_sgen_par_object_get_size (vt, (MonoObject*)obj));
	char *destination = major_alloc_object (objsize, has_references);

	if (G_UNLIKELY (!destination)) {
		mono_sgen_pin_object (obj, queue);
		return obj;
	}

	par_copy_object_no_checks (destination, vt, obj, objsize, has_references ? queue : NULL);

	/* set the forwarding pointer */
	SGEN_FORWARD_OBJECT (obj, destination);

	return destination;
}

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

static void
copy_object (void **obj_slot, SgenGrayQueue *queue)
{
	char *forwarded;
	char *obj = *obj_slot;

	DEBUG (9, g_assert (current_collection_generation == GENERATION_NURSERY));

	HEAVY_STAT (++stat_copy_object_called_nursery);

	if (!ptr_in_nursery (obj)) {
		HEAVY_STAT (++stat_nursery_copy_object_failed_from_space);
		return;
	}

	DEBUG (9, fprintf (gc_debug_file, "Precise copy of %p from %p", obj, obj_slot));

	/*
	 * Before we can copy the object we must make sure that we are
	 * allowed to, i.e. that the object not pinned or not already
	 * forwarded.
	 */

	if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
		DEBUG (9, g_assert (((MonoVTable*)SGEN_LOAD_VTABLE(obj))->gc_descr));
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

	HEAVY_STAT (++stat_objects_copied_nursery);

	*obj_slot = copy_object_no_checks (obj, queue);
}

#define FILL_COLLECTOR_COPY_OBJECT(collector)	do {			\
		(collector)->copy_object = copy_object;			\
	} while (0)
