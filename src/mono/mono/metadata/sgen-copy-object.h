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

	DEBUG (9, g_assert (vt->klass->inited));
	DEBUG (9, fprintf (gc_debug_file, " (to %p, %s size: %lu)\n", destination, ((MonoObject*)obj)->vtable->klass->name, (unsigned long)objsize));
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
	DEBUG (9, g_assert (vt->gc_descr));
	if (G_UNLIKELY (vt->rank && ((MonoArray*)obj)->bounds)) {
		MonoArray *array = (MonoArray*)destination;
		array->bounds = (MonoArrayBounds*)((char*)destination + ((char*)((MonoArray*)obj)->bounds - (char*)obj));
		DEBUG (9, fprintf (gc_debug_file, "Array instance %p: size: %lu, rank: %d, length: %lu\n", array, (unsigned long)objsize, vt->rank, (unsigned long)mono_array_length (array)));
	}
	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES))
		sgen_register_moved_object (obj, destination);
	obj = destination;
	if (queue) {
		DEBUG (9, fprintf (gc_debug_file, "Enqueuing gray object %p (%s)\n", obj, sgen_safe_name (obj)));
		GRAY_OBJECT_ENQUEUE (queue, obj);
	}
}

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
	char *destination = collector_serial_alloc_for_promotion (obj, objsize, has_references);

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

#ifdef GENERATE_COPY_FUNCTIONS

extern long long stat_nursery_copy_object_failed_to_space; /* from sgen-gc.c */

#if defined(SGEN_SIMPLE_NURSERY)
#define serial_copy_object simple_nursery_serial_copy_object
#define parallel_copy_object simple_nursery_parallel_copy_object

#elif defined (SGEN_SPLIT_NURSERY)

#define serial_copy_object split_nursery_serial_copy_object
#define parallel_copy_object split_nursery_parallel_copy_object

#else
#error "Please define GC_CONF_NAME"
#endif

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
serial_copy_object (void **obj_slot, SgenGrayQueue *queue) 
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
parallel_copy_object (void **obj_slot, SgenGrayQueue *queue)
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

	destination = collector_parallel_alloc_for_promotion (obj, objsize, has_references);

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

#endif
