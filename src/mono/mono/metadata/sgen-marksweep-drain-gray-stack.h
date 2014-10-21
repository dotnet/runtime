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
COPY_OR_MARK_FUNCTION_NAME (void **ptr, void *obj, SgenGrayQueue *queue)
{
	MSBlockInfo *block;

#ifdef HEAVY_STATISTICS
	++stat_optimized_copy;
	{
		char *forwarded;
		mword desc;
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
#ifdef SGEN_MARK_ON_ENQUEUE
		int word, bit;
#endif
		char *forwarded, *old_obj;
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
			*ptr = forwarded;
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
		*ptr = obj;

		if (sgen_ptr_in_nursery (obj))
			return TRUE;

#ifdef SGEN_MARK_ON_ENQUEUE
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
		binary_protocol_mark (obj, (gpointer)LOAD_VTABLE (obj), sgen_safe_object_get_size ((MonoObject*)obj));
#endif

		return FALSE;
	} else {
#ifdef SGEN_MARK_ON_ENQUEUE
		mword vtable_word = *(mword*)obj;
		mword desc = sgen_vtable_get_descriptor ((MonoVTable*)vtable_word);
		int type = desc & 7;

		HEAVY_STAT (++stat_optimized_copy_major);

#ifdef COPY_OR_MARK_WITH_EVACUATION
		{
			char *forwarded;
			if ((forwarded = SGEN_VTABLE_IS_FORWARDED (vtable_word))) {
				HEAVY_STAT (++stat_optimized_copy_major_forwarded);
				*ptr = forwarded;
				SGEN_ASSERT (9, !sgen_ptr_in_nursery (forwarded), "Cannot be forwarded to nursery.");
				return FALSE;
			}
		}
#endif

		if (type <= DESC_TYPE_MAX_SMALL_OBJ || SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)obj)) <= SGEN_MAX_SMALL_OBJ_SIZE) {
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
			binary_protocol_pin (obj, (gpointer)SGEN_LOAD_VTABLE (obj), sgen_safe_object_get_size ((MonoObject*)obj));

			sgen_los_pin_object (obj);
			if (SGEN_OBJECT_HAS_REFERENCES (obj))
				GRAY_OBJECT_ENQUEUE (queue, obj, sgen_obj_get_descriptor (obj));
		}
		return FALSE;
#else
		GRAY_OBJECT_ENQUEUE (queue, obj, 0);
#endif
	}
	return FALSE;
}

static void
SCAN_OBJECT_FUNCTION_NAME (char *obj, mword desc, SgenGrayQueue *queue)
{
	int type;

#ifndef SGEN_GRAY_QUEUE_HAVE_DESCRIPTORS
	desc = sgen_obj_get_descriptor_safe (obj);
#endif
	type = desc & 7;

#ifndef SGEN_MARK_ON_ENQUEUE
	HEAVY_STAT (++stat_optimized_major_mark);

	/* Mark object or, if already marked, don't process. */
	if (!sgen_ptr_in_nursery (obj)) {
		if (type <= DESC_TYPE_MAX_SMALL_OBJ || SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)obj)) <= SGEN_MAX_SMALL_OBJ_SIZE) {
			MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
			int __word, __bit;

			HEAVY_STAT (++stat_optimized_major_mark_small);

			MS_CALC_MARK_BIT (__word, __bit, (obj));
			if (MS_MARK_BIT ((block), __word, __bit))
				return;
			MS_SET_MARK_BIT ((block), __word, __bit);
		} else {
			HEAVY_STAT (++stat_optimized_major_mark_large);

			if (sgen_los_object_is_pinned (obj))
				return;
			sgen_los_pin_object (obj);
		}
	}
#endif

#ifdef HEAVY_STATISTICS
	++stat_optimized_major_scan;
	if (!sgen_gc_descr_has_references (desc))
		++stat_optimized_major_scan_no_refs;
#endif

	/* Now scan the object. */

#ifdef DESCRIPTOR_FAST_PATH
	if (type == DESC_TYPE_LARGE_BITMAP) {
		int i;
		void *ptrs [LARGE_BITMAP_SIZE];
		int num_ptrs = 0;

		PREFETCH_WRITE (obj + sizeof (MonoObject));

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
			/* PREFETCH_WRITE ((ptr)); */			\
			ptrs [num_ptrs++] = (ptr);			\
		} while (0)

		OBJ_LARGE_BITMAP_FOREACH_PTR (desc, obj);

		for (i = 0; i < num_ptrs; ++i) {
			void **ptr = ptrs [i];
			void *__old = *ptr;
			binary_protocol_scan_process_reference ((obj), (ptr), __old);
			if (__old) {
				gboolean __still_in_nursery = COPY_OR_MARK_FUNCTION_NAME ((ptr), __old, queue);
				if (G_UNLIKELY (__still_in_nursery && !sgen_ptr_in_nursery ((ptr)) && !SGEN_OBJECT_IS_CEMENTED (*(ptr)))) {
					void *__copy = *(ptr);
					sgen_add_to_global_remset ((ptr), __copy);
				}
			}
		}

#ifdef HEAVY_STATISTICS
		sgen_descriptor_count_scanned_object (desc);
		++stat_optimized_major_scan_fast;
#endif
#ifdef SGEN_HEAVY_BINARY_PROTOCOL
		add_scanned_object (obj);
#endif
	} else
#endif
	{
		char *start = obj;
#ifdef HEAVY_STATISTICS
		++stat_optimized_major_scan_slow;
		sgen_descriptor_count_scanned_object (desc);
#endif
#ifdef SGEN_HEAVY_BINARY_PROTOCOL
		add_scanned_object (start);
#endif


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
}

static gboolean
DRAIN_GRAY_STACK_FUNCTION_NAME (ScanCopyContext ctx)
{
	SgenGrayQueue *queue = ctx.queue;

	SGEN_ASSERT (0, ctx.scan_func == major_scan_object_with_evacuation, "Wrong scan function");

	for (;;) {
		char *obj;
		mword desc;

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
