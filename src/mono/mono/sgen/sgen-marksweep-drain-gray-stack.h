/*
 * sgen-marksweep-drain-gray-stack.h: The copy/mark and gray stack
 *     draining functions of the M&S major collector.
 *
 * Copyright (C) 2014 Xamarin Inc
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
 * COPY_OR_MARK_FUNCTION_NAME must be defined to be the function name of the copy/mark
 * function.
 *
 * SCAN_OBJECT_FUNCTION_NAME must be defined to be the function name of the object scanning
 * function.
 *
 * DRAIN_GRAY_STACK_FUNCTION_NAME must be defined to be the function name of the gray stack
 * draining function.
 *
 * Define COPY_OR_MARK_WITH_EVACUATION to support evacuation.
 */

/* Returns whether the object is still in the nursery. */
static inline MONO_ALWAYS_INLINE gboolean
COPY_OR_MARK_FUNCTION_NAME (GCObject **ptr, GCObject *obj, SgenGrayQueue *queue)
{
	MSBlockInfo *block;

#ifdef HEAVY_STATISTICS
	++stat_optimized_copy;
	{
		char *forwarded;
		SgenDescriptor desc;
		if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj)))
			desc = sgen_obj_get_descriptor_safe (forwarded);
		else
			desc = sgen_obj_get_descriptor_safe (obj);

		sgen_descriptor_count_copied_object (desc);
	}
#endif

	SGEN_ASSERT (9, obj, "null object from pointer %p", ptr);
	SGEN_ASSERT (9, current_collection_generation == GENERATION_OLD, "old gen parallel allocator called from a %d collection", current_collection_generation);

	if (sgen_ptr_in_nursery (obj)) {
		int word, bit;
		GCObject *forwarded, *old_obj;
		mword vtable_word = *(mword*)obj;

		HEAVY_STAT (++stat_optimized_copy_nursery);

#if SGEN_MAX_DEBUG_LEVEL >= 9
		if (sgen_nursery_is_to_space (obj))
			SGEN_ASSERT (9, !SGEN_VTABLE_IS_PINNED (vtable_word) && !SGEN_VTABLE_IS_FORWARDED (vtable_word), "To-space object can't be pinned or forwarded.");
#endif

		if (SGEN_VTABLE_IS_PINNED (vtable_word)) {
			SGEN_ASSERT (9, !SGEN_VTABLE_IS_FORWARDED (vtable_word), "Cannot be both pinned and forwarded.");
			HEAVY_STAT (++stat_optimized_copy_nursery_pinned);
			return TRUE;
		}
		if ((forwarded = SGEN_VTABLE_IS_FORWARDED (vtable_word))) {
			HEAVY_STAT (++stat_optimized_copy_nursery_forwarded);
			SGEN_UPDATE_REFERENCE (ptr, forwarded);
			return sgen_ptr_in_nursery (forwarded);
		}

		/* An object in the nursery To Space has already been copied and grayed. Nothing to do. */
		if (sgen_nursery_is_to_space (obj))
			return TRUE;

#ifdef COPY_OR_MARK_WITH_EVACUATION
	do_copy_object:
#endif
		old_obj = obj;
		obj = copy_object_no_checks (obj, queue);
		if (G_UNLIKELY (old_obj == obj)) {
			/*
			 * If we fail to evacuate an object we just stop doing it for a
			 * given block size as all other will surely fail too.
			 */
			/* FIXME: test this case somehow. */
			if (!sgen_ptr_in_nursery (obj)) {
				int size_index;
				block = MS_BLOCK_FOR_OBJ (obj);
				size_index = block->obj_size_index;
				evacuate_block_obj_sizes [size_index] = FALSE;
				MS_MARK_OBJECT_AND_ENQUEUE (obj, sgen_obj_get_descriptor (obj), block, queue);
				return FALSE;
			}
			return TRUE;
		}
		HEAVY_STAT (++stat_objects_copied_major);
		SGEN_UPDATE_REFERENCE (ptr, obj);

		if (sgen_ptr_in_nursery (obj))
			return TRUE;

		/*
		 * FIXME: See comment for copy_object_no_checks().  If
		 * we have that, we can let the allocation function
		 * give us the block info, too, and we won't have to
		 * re-fetch it.
		 *
		 * FIXME (2): We should rework this to avoid all those nursery checks.
		 */
		/*
		 * For the split nursery allocator the object might
		 * still be in the nursery despite having being
		 * promoted, in which case we can't mark it.
		 */
		block = MS_BLOCK_FOR_OBJ (obj);
		MS_CALC_MARK_BIT (word, bit, obj);
		SGEN_ASSERT (9, !MS_MARK_BIT (block, word, bit), "object %p already marked", obj);
		MS_SET_MARK_BIT (block, word, bit);
		binary_protocol_mark (obj, (gpointer)LOAD_VTABLE (obj), sgen_safe_object_get_size (obj));

		return FALSE;
	} else {
		mword vtable_word = *(mword*)obj;
		SgenDescriptor desc;
		int type;

		HEAVY_STAT (++stat_optimized_copy_major);

#ifdef COPY_OR_MARK_WITH_EVACUATION
		{
			GCObject *forwarded;
			if ((forwarded = SGEN_VTABLE_IS_FORWARDED (vtable_word))) {
				HEAVY_STAT (++stat_optimized_copy_major_forwarded);
				SGEN_UPDATE_REFERENCE (ptr, forwarded);
				SGEN_ASSERT (9, !sgen_ptr_in_nursery (forwarded), "Cannot be forwarded to nursery.");
				return FALSE;
			}
		}
#endif

		SGEN_ASSERT (9, !SGEN_VTABLE_IS_PINNED (vtable_word), "Pinned object in non-pinned block?");

		desc = sgen_vtable_get_descriptor ((GCVTable)vtable_word);
		type = desc & DESC_TYPE_MASK;

		if (sgen_safe_object_is_small (obj, type)) {
#ifdef HEAVY_STATISTICS
			if (type <= DESC_TYPE_MAX_SMALL_OBJ)
				++stat_optimized_copy_major_small_fast;
			else
				++stat_optimized_copy_major_small_slow;
#endif

			block = MS_BLOCK_FOR_OBJ (obj);

#ifdef COPY_OR_MARK_WITH_EVACUATION
			{
				int size_index = block->obj_size_index;

				if (evacuate_block_obj_sizes [size_index] && !block->has_pinned) {
					HEAVY_STAT (++stat_optimized_copy_major_small_evacuate);
					if (block->is_to_space)
						return FALSE;
					goto do_copy_object;
				}
			}
#endif

			MS_MARK_OBJECT_AND_ENQUEUE (obj, desc, block, queue);
		} else {
			HEAVY_STAT (++stat_optimized_copy_major_large);

			if (sgen_los_object_is_pinned (obj))
				return FALSE;
			binary_protocol_pin (obj, (gpointer)SGEN_LOAD_VTABLE (obj), sgen_safe_object_get_size (obj));

			sgen_los_pin_object (obj);
			if (SGEN_OBJECT_HAS_REFERENCES (obj))
				GRAY_OBJECT_ENQUEUE (queue, obj, sgen_obj_get_descriptor (obj));
		}
		return FALSE;
	}
	SGEN_ASSERT (0, FALSE, "How is this happening?");
	return FALSE;
}

static void
SCAN_OBJECT_FUNCTION_NAME (GCObject *obj, SgenDescriptor desc, SgenGrayQueue *queue)
{
	char *start = (char*)obj;

#ifdef HEAVY_STATISTICS
	++stat_optimized_major_scan;
	if (!sgen_gc_descr_has_references (desc))
		++stat_optimized_major_scan_no_refs;
	sgen_descriptor_count_scanned_object (desc);
#endif
#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	add_scanned_object (start);
#endif

	/* Now scan the object. */

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		void *__old = *(ptr);					\
		binary_protocol_scan_process_reference ((obj), (ptr), __old); \
		if (__old) {						\
			gboolean __still_in_nursery = COPY_OR_MARK_FUNCTION_NAME ((ptr), __old, queue); \
			if (G_UNLIKELY (__still_in_nursery && !sgen_ptr_in_nursery ((ptr)) && !SGEN_OBJECT_IS_CEMENTED (*(ptr)))) { \
				void *__copy = *(ptr);			\
				sgen_add_to_global_remset ((ptr), __copy); \
			}						\
		}							\
	} while (0)

#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"
}

static gboolean
DRAIN_GRAY_STACK_FUNCTION_NAME (ScanCopyContext ctx)
{
	SgenGrayQueue *queue = ctx.queue;

	SGEN_ASSERT (0, ctx.ops->scan_object == major_scan_object_with_evacuation, "Wrong scan function");

	for (;;) {
		GCObject *obj;
		SgenDescriptor desc;

		HEAVY_STAT (++stat_drain_loops);

		GRAY_OBJECT_DEQUEUE (queue, &obj, &desc);
		if (!obj)
			return TRUE;

		SCAN_OBJECT_FUNCTION_NAME (obj, desc, ctx.queue);
	}
}

#undef COPY_OR_MARK_FUNCTION_NAME
#undef COPY_OR_MARK_WITH_EVACUATION
#undef SCAN_OBJECT_FUNCTION_NAME
#undef DRAIN_GRAY_STACK_FUNCTION_NAME
