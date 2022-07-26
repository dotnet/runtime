/**
 * \file
 * The copy/mark and gray stack draining functions of the M&S major collector.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
		GCObject *forwarded;
		SgenDescriptor desc;
		if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj)))
			desc = sgen_obj_get_descriptor_safe (forwarded);
		else
			desc = sgen_obj_get_descriptor_safe (obj);

		sgen_descriptor_count_copied_object (desc);
	}
#endif

	SGEN_ASSERT (9, obj, "null object from pointer %p", ptr);
#if !defined(COPY_OR_MARK_CONCURRENT) && !defined(COPY_OR_MARK_CONCURRENT_WITH_EVACUATION)
	SGEN_ASSERT (9, sgen_current_collection_generation == GENERATION_OLD, "old gen parallel allocator called from a %d collection", sgen_current_collection_generation);
#endif

	if (sgen_ptr_in_nursery (obj)) {
#if !defined(COPY_OR_MARK_CONCURRENT) && !defined(COPY_OR_MARK_CONCURRENT_WITH_EVACUATION)
		int word, bit;
		gboolean first;
		GCObject *forwarded, *old_obj;
		mword vtable_word;
		vtable_word = *(mword*)obj;

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
		if ((forwarded = (GCObject *)SGEN_VTABLE_IS_FORWARDED (vtable_word))) {
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
#ifdef COPY_OR_MARK_PARALLEL
		obj = copy_object_no_checks_par (obj, queue);
#else
		obj = copy_object_no_checks (obj, queue);
#endif
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
#ifdef COPY_OR_MARK_PARALLEL
		MS_SET_MARK_BIT_PAR (block, word, bit, first);
#else
		MS_SET_MARK_BIT (block, word, bit);
		first = TRUE;
#endif
		if (first)
			sgen_binary_protocol_mark (obj, (gpointer)SGEN_LOAD_VTABLE (obj), sgen_safe_object_get_size (obj));

		return FALSE;
#endif
	} else {
		mword vtable_word = *(mword*)obj;
		SgenDescriptor desc;
		int type;

		HEAVY_STAT (++stat_optimized_copy_major);

#ifdef COPY_OR_MARK_WITH_EVACUATION
		{
			GCObject *forwarded;
			if ((forwarded = (GCObject *)SGEN_VTABLE_IS_FORWARDED (vtable_word))) {
				HEAVY_STAT (++stat_optimized_copy_major_forwarded);
				SGEN_UPDATE_REFERENCE (ptr, forwarded);
				SGEN_ASSERT (9, !sgen_ptr_in_nursery (forwarded), "Cannot be forwarded to nursery.");
				return FALSE;
			}
		}
#endif

		SGEN_ASSERT (9, !SGEN_VTABLE_IS_PINNED (vtable_word), "Pinned object in non-pinned block?");

		/* We untag the vtable for concurrent M&S, in case bridge is running and it tagged it */
		desc = sgen_vtable_get_descriptor ((GCVTable)SGEN_POINTER_UNTAG_VTABLE (vtable_word));
		type = desc & DESC_TYPE_MASK;

		if (sgen_safe_object_is_small (obj, type)) {
#ifdef HEAVY_STATISTICS
			if (type <= DESC_TYPE_MAX_SMALL_OBJ)
				++stat_optimized_copy_major_small_fast;
			else
				++stat_optimized_copy_major_small_slow;
#endif

			block = MS_BLOCK_FOR_OBJ (obj);

#ifdef COPY_OR_MARK_CONCURRENT_WITH_EVACUATION
			if (G_UNLIKELY (major_block_is_evacuating (block))) {
				/*
				 * We don't copy within the concurrent phase. These objects will
				 * be handled below in the finishing pause, by scanning the mod-union
				 * card table.
				 */
				return FALSE;
			}
#endif

#ifdef COPY_OR_MARK_WITH_EVACUATION
			if (major_block_is_evacuating (block)) {
				HEAVY_STAT (++stat_optimized_copy_major_small_evacuate);
				goto do_copy_object;
			}
#endif

#ifdef COPY_OR_MARK_PARALLEL
			MS_MARK_OBJECT_AND_ENQUEUE_PAR (obj, desc, block, queue);
#else
			MS_MARK_OBJECT_AND_ENQUEUE (obj, desc, block, queue);
#endif
		} else {
			gboolean first = TRUE;
			HEAVY_STAT (++stat_optimized_copy_major_large);
#ifdef COPY_OR_MARK_PARALLEL
			first = sgen_los_pin_object_par (obj);
#else
			if (sgen_los_object_is_pinned (obj))
				first = FALSE;
			else
				sgen_los_pin_object (obj);
#endif

			if (first) {
				sgen_binary_protocol_pin (obj, (gpointer)SGEN_LOAD_VTABLE (obj), sgen_safe_object_get_size (obj));
				if (SGEN_OBJECT_HAS_REFERENCES (obj))
#ifdef COPY_OR_MARK_PARALLEL
					GRAY_OBJECT_ENQUEUE_PARALLEL (queue, obj, desc);
#else
					GRAY_OBJECT_ENQUEUE_SERIAL (queue, obj, desc);
#endif
			}
		}
		return FALSE;
	}

	return TRUE;
}

static void
SCAN_OBJECT_FUNCTION_NAME (GCObject *full_object, SgenDescriptor desc, SgenGrayQueue *queue)
{
	char *start = (char*)full_object;

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
#if defined(COPY_OR_MARK_CONCURRENT_WITH_EVACUATION)
#define HANDLE_PTR(ptr,obj)	do {					\
		GCObject *__old = *(ptr);				\
		sgen_binary_protocol_scan_process_reference ((full_object), (ptr), __old); \
		if (__old && !sgen_ptr_in_nursery (__old)) {            \
			if (G_UNLIKELY (full_object && !sgen_ptr_in_nursery (ptr) && \
					sgen_safe_object_is_small (__old, sgen_obj_get_descriptor (__old) & DESC_TYPE_MASK) && \
					major_block_is_evacuating (MS_BLOCK_FOR_OBJ (__old)))) { \
				mark_mod_union_card ((full_object), (void**)(ptr), __old); \
			} else {					\
				PREFETCH_READ (__old);			\
				COPY_OR_MARK_FUNCTION_NAME ((ptr), __old, queue); \
			}						\
		} else {                                                \
			if (G_UNLIKELY (full_object && sgen_ptr_in_nursery (__old) && !sgen_ptr_in_nursery ((ptr)) && !sgen_cement_is_forced (__old))) \
				mark_mod_union_card ((full_object), (void**)(ptr), __old); \
			}						\
		} while (0)
#elif defined(COPY_OR_MARK_CONCURRENT)
#define HANDLE_PTR(ptr,obj)	do {					\
		GCObject *__old = *(ptr);				\
		sgen_binary_protocol_scan_process_reference ((full_object), (ptr), __old); \
		if (__old && !sgen_ptr_in_nursery (__old)) {            \
			PREFETCH_READ (__old);			\
			COPY_OR_MARK_FUNCTION_NAME ((ptr), __old, queue); \
		} else {                                                \
			if (G_UNLIKELY (full_object && sgen_ptr_in_nursery (__old) && !sgen_ptr_in_nursery ((ptr)) && !sgen_cement_is_forced (__old))) \
				mark_mod_union_card ((full_object), (void**)(ptr), __old); \
			}						\
		} while (0)
#else
#define HANDLE_PTR(ptr,obj)	do {					\
		GCObject *__old = *(ptr);					\
		sgen_binary_protocol_scan_process_reference ((full_object), (ptr), __old); \
		if (__old) {						\
			gboolean __still_in_nursery = COPY_OR_MARK_FUNCTION_NAME ((ptr), __old, queue); \
			if (G_UNLIKELY (__still_in_nursery && !sgen_ptr_in_nursery ((ptr)) && !SGEN_OBJECT_IS_CEMENTED (*(ptr)))) { \
				GCObject *__copy = *(ptr);			\
				sgen_add_to_global_remset ((ptr), __copy); \
			}						\
		}							\
	} while (0)
#endif

#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"
}

#ifdef SCAN_VTYPE_FUNCTION_NAME
static void
SCAN_VTYPE_FUNCTION_NAME (GCObject *full_object, char *start, SgenDescriptor desc, SgenGrayQueue *queue BINARY_PROTOCOL_ARG (size_t size))
{
	SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP;

#ifdef HEAVY_STATISTICS
	/* FIXME: We're half scanning this object.  How do we account for that? */
	//add_scanned_object (start);
#endif

	/* The descriptors include info about the object header as well */
	start -= SGEN_CLIENT_OBJECT_HEADER_SIZE;

	/* We use the same HANDLE_PTR from the obj scan function */
#define SCAN_OBJECT_NOVTABLE
#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"

	SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP;
}
#endif

#ifdef SCAN_PTR_FIELD_FUNCTION_NAME
static void
SCAN_PTR_FIELD_FUNCTION_NAME (GCObject *full_object, GCObject **ptr, SgenGrayQueue *queue)
{
	/*
	 * full_object is NULL if we scan unmanaged memory. This means we can't mark
	 * mod unions for it, so these types of roots currently don't have support
	 * for the concurrent collector (aka they need to be scanned as normal roots
	 * both in the start and finishing pause)
	 */
	HANDLE_PTR (ptr, NULL);
}
#endif

static gboolean
DRAIN_GRAY_STACK_FUNCTION_NAME (SgenGrayQueue *queue)
{
#if defined(COPY_OR_MARK_CONCURRENT) || defined(COPY_OR_MARK_CONCURRENT_WITH_EVACUATION) || defined(COPY_OR_MARK_PARALLEL)
	int i;
	for (i = 0; i < 32; i++) {
#else
	for (;;) {
#endif
		GCObject *obj;
		SgenDescriptor desc;

		HEAVY_STAT (++stat_drain_loops);

#if defined(COPY_OR_MARK_PARALLEL)
		GRAY_OBJECT_DEQUEUE_PARALLEL (queue, &obj, &desc);
#else
		GRAY_OBJECT_DEQUEUE_SERIAL (queue, &obj, &desc);
#endif
		if (!obj)
			return TRUE;

		SCAN_OBJECT_FUNCTION_NAME (obj, desc, queue);
	}
	return FALSE;
}

#undef COPY_OR_MARK_PARALLEL
#undef COPY_OR_MARK_FUNCTION_NAME
#undef COPY_OR_MARK_WITH_EVACUATION
#undef COPY_OR_MARK_CONCURRENT
#undef COPY_OR_MARK_CONCURRENT_WITH_EVACUATION
#undef SCAN_OBJECT_FUNCTION_NAME
#undef SCAN_VTYPE_FUNCTION_NAME
#undef SCAN_PTR_FIELD_FUNCTION_NAME
#undef DRAIN_GRAY_STACK_FUNCTION_NAME
