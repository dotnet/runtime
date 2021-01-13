/**
 * \file
 * Large objects space.
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2005-2010 Novell, Inc (http://www.novell.com)
 *
 * Thread start/stop adapted from Boehm's GC:
 * Copyright (c) 1994 by Xerox Corporation.  All rights reserved.
 * Copyright (c) 1996 by Silicon Graphics.  All rights reserved.
 * Copyright (c) 1998 by Fergus Henderson.  All rights reserved.
 * Copyright (c) 2000-2004 by Hewlett-Packard Company.  All rights reserved.
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-cardtable.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-client.h"
#include "mono/sgen/sgen-array-list.h"
#include "mono/sgen/sgen-pinning.h"

#define LOS_SECTION_SIZE	(1024 * 1024)

/*
 * This shouldn't be much smaller or larger than MAX_SMALL_OBJ_SIZE.
 * Must be at least sizeof (LOSSection).
 */
#define LOS_CHUNK_SIZE		4096
#define LOS_CHUNK_BITS		12

/* Largest object that can be allocated in a section. */
#define LOS_SECTION_OBJECT_LIMIT	(LOS_SECTION_SIZE - LOS_CHUNK_SIZE - sizeof (LOSObject))
//#define LOS_SECTION_OBJECT_LIMIT	0
#define LOS_SECTION_NUM_CHUNKS		((LOS_SECTION_SIZE >> LOS_CHUNK_BITS) - 1)

#define LOS_SECTION_FOR_OBJ(obj)	((LOSSection*)((mword)(obj) & ~(mword)(LOS_SECTION_SIZE - 1)))
#define LOS_CHUNK_INDEX(obj,section)	(((char*)(obj) - (char*)(section)) >> LOS_CHUNK_BITS)

#define LOS_NUM_FAST_SIZES		32

typedef struct _LOSFreeChunks LOSFreeChunks;
struct _LOSFreeChunks {
	LOSFreeChunks *next_size;
	size_t size;
};

typedef struct _LOSSection LOSSection;
struct _LOSSection {
	LOSSection *next;
	size_t num_free_chunks;
	unsigned char *free_chunk_map;
};

/* We allow read only access on the array list while sweep is not running */
static SgenArrayList sgen_los_object_array_list = SGEN_ARRAY_LIST_INIT (NULL, sgen_array_list_default_is_slot_set, NULL, INTERNAL_MEM_PIN_QUEUE);
static gboolean compact_los_objects = FALSE;

#define LOS_OBJECT_IS_TAGGED_HAS_REFERENCES(bl) SGEN_POINTER_IS_TAGGED_1 ((bl))
#define LOS_OBJECT_TAG_HAS_REFERENCES(bl) SGEN_POINTER_TAG_1 ((bl))

#define LOS_OBJECT_UNTAG(bl) ((LOSObject *)SGEN_POINTER_UNTAG_1 ((bl)))

#define LOS_OBJECT_TAG(bl) (SGEN_OBJECT_HAS_REFERENCES((bl)->data) ? LOS_OBJECT_TAG_HAS_REFERENCES ((bl)) : (bl))

#define FOREACH_LOS_OBJECT_NO_LOCK(bl) { \
	volatile gpointer *slot; \
	SGEN_ARRAY_LIST_FOREACH_SLOT (&sgen_los_object_array_list, slot) { \
		(bl) = LOS_OBJECT_UNTAG (*slot); \
		if (!(bl)) \
			continue;

#define FREE_CURRENT_LOS_OBJECT_NO_LOCK(bl) \
	*slot = NULL; \
	sgen_los_free_object (bl);

#define END_FOREACH_LOS_OBJECT_NO_LOCK } SGEN_ARRAY_LIST_END_FOREACH_SLOT; }

#define FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK(bl,hr) { \
	volatile gpointer *slot; \
	SGEN_ARRAY_LIST_FOREACH_SLOT (&sgen_los_object_array_list, slot) { \
		(bl) = (LOSObject *) (*slot); \
		if (!(bl)) \
			continue; \
		(hr) = LOS_OBJECT_IS_TAGGED_HAS_REFERENCES ((bl)); \
		(bl) = LOS_OBJECT_UNTAG ((bl));

#define END_FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK } SGEN_ARRAY_LIST_END_FOREACH_SLOT; }

#define FOREACH_LOS_OBJECT_RANGE_NO_LOCK(bl,begin,end,index) { \
	volatile gpointer *slot; \
	SGEN_ARRAY_LIST_FOREACH_SLOT_RANGE (&sgen_los_object_array_list, begin, end, slot, index) { \
		(bl) = LOS_OBJECT_UNTAG (*slot); \
		if (!(bl)) \
			continue;

#define END_FOREACH_LOS_OBJECT_RANGE_NO_LOCK } SGEN_ARRAY_LIST_END_FOREACH_SLOT_RANGE; }

#define FOREACH_LOS_OBJECT_RANGE_HAS_REFERENCES_NO_LOCK(bl,begin,end,index,hr) { \
	volatile gpointer *slot; \
	SGEN_ARRAY_LIST_FOREACH_SLOT_RANGE (&sgen_los_object_array_list, begin, end, slot, index) { \
		(bl) = (LOSObject *) (*slot); \
		if (!(bl)) \
			continue; \
		(hr) = LOS_OBJECT_IS_TAGGED_HAS_REFERENCES ((bl)); \
		(bl) = LOS_OBJECT_UNTAG ((bl));

#define END_FOREACH_LOS_OBJECT_RANGE_HAS_REFERENCES_NO_LOCK } SGEN_ARRAY_LIST_END_FOREACH_SLOT_RANGE; }

/* Memory used by LOS objects */
mword sgen_los_memory_usage = 0;
/* Total memory used by the LOS allocator */
mword sgen_los_memory_usage_total = 0;

static LOSSection *los_sections = NULL;
static LOSFreeChunks *los_fast_free_lists [LOS_NUM_FAST_SIZES]; /* 0 is for larger sizes */
static mword los_num_objects = 0;
static int los_num_sections = 0;

//#define USE_MALLOC
//#define LOS_CONSISTENCY_CHECK
//#define LOS_DUMMY

#ifdef LOS_DUMMY
#define LOS_SEGMENT_SIZE	(4096 * 1024)

static char *los_segment = NULL;
static int los_segment_index = 0;
#endif

mword
sgen_los_object_size (LOSObject *obj)
{
	return obj->size & ~1L;
}

#ifdef LOS_CONSISTENCY_CHECK
static void
los_consistency_check (void)
{
	LOSSection *section;
	LOSObject *obj;
	int i;
	mword memory_usage = 0;

	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		mword obj_size = sgen_los_object_size (obj);
		char *end = obj->data + obj_size;
		int start_index, num_chunks;

		memory_usage += obj_size;

		if (obj_size > LOS_SECTION_OBJECT_LIMIT)
			continue;

		section = LOS_SECTION_FOR_OBJ (obj);

		g_assert (end <= (char*)section + LOS_SECTION_SIZE);

		start_index = LOS_CHUNK_INDEX (obj, section);
		num_chunks = (obj_size + sizeof (LOSObject) + LOS_CHUNK_SIZE - 1) >> LOS_CHUNK_BITS;
		for (i = start_index; i < start_index + num_chunks; ++i)
			g_assert (!section->free_chunk_map [i]);
	} END_FOREACH_LOS_OBJECT_NO_LOCK;

	for (i = 0; i < LOS_NUM_FAST_SIZES; ++i) {
		LOSFreeChunks *size_chunks;
		for (size_chunks = los_fast_free_lists [i]; size_chunks; size_chunks = size_chunks->next_size) {
			LOSSection *section = LOS_SECTION_FOR_OBJ (size_chunks);
			int j, num_chunks, start_index;

			if (i == 0)
				g_assert (size_chunks->size >= LOS_NUM_FAST_SIZES * LOS_CHUNK_SIZE);
			else
				g_assert (size_chunks->size == i * LOS_CHUNK_SIZE);

			num_chunks = size_chunks->size >> LOS_CHUNK_BITS;
			start_index = LOS_CHUNK_INDEX (size_chunks, section);
			for (j = start_index; j < start_index + num_chunks; ++j)
				g_assert (section->free_chunk_map [j]);
		}
	}

	g_assert (sgen_los_memory_usage == memory_usage);
}
#endif

static void
add_free_chunk (LOSFreeChunks *free_chunks, size_t size)
{
	size_t num_chunks = size >> LOS_CHUNK_BITS;

	free_chunks->size = size;

	if (num_chunks >= LOS_NUM_FAST_SIZES)
		num_chunks = 0;
	free_chunks->next_size = los_fast_free_lists [num_chunks];
	los_fast_free_lists [num_chunks] = free_chunks;
}

static LOSFreeChunks*
get_from_size_list (LOSFreeChunks **list, size_t size)
{
	LOSFreeChunks *free_chunks = NULL;
	LOSSection *section;
	size_t i, num_chunks, start_index;


	g_assert ((size & (LOS_CHUNK_SIZE - 1)) == 0);

	while (*list) {
		free_chunks = *list;
		if (free_chunks->size >= size)
			break;
		list = &(*list)->next_size;
	}

	if (!*list)
		return NULL;

	*list = free_chunks->next_size;

	if (free_chunks->size > size)
		add_free_chunk ((LOSFreeChunks*)((char*)free_chunks + size), free_chunks->size - size);

	num_chunks = size >> LOS_CHUNK_BITS;

	section = LOS_SECTION_FOR_OBJ (free_chunks);

	start_index = LOS_CHUNK_INDEX (free_chunks, section);
	for (i = start_index; i < start_index + num_chunks; ++i) {
		g_assert (section->free_chunk_map [i]);
		section->free_chunk_map [i] = 0;
	}

	section->num_free_chunks -= size >> LOS_CHUNK_BITS;
	g_assert (section->num_free_chunks >= 0);

	return free_chunks;
}

static LOSObject*
randomize_los_object_start (gpointer addr, size_t obj_size, size_t alloced_size, size_t addr_alignment)
{
	size_t offset = 0;
	if (alloced_size != obj_size) {
		/*
		 * We want to get a random offset between 0 and (alloced_size - obj_size)
		 * We do a prime multiplication to avoid usage of functions which might not
		 * be thread/signal safe (like rand ()). We subtract 1 to avoid common
		 * power by 2 factors.
		 */
		offset = SGEN_ALIGN_DOWN ((((size_t)addr - 1) * 2654435761u) % (alloced_size - obj_size));
	}
	SGEN_ASSERT (0, (alloced_size - obj_size) < addr_alignment, "Why are we wasting one entire chunk for a los object ?");
	/* Randomize the location within the reserved chunks to improve cache performance */
	return (LOSObject*)((guint8*)addr + offset);

}

static LOSObject*
get_los_section_memory (size_t size)
{
	LOSSection *section;
	LOSFreeChunks *free_chunks;
	size_t num_chunks;
	size_t obj_size = size;

	size = SGEN_ALIGN_UP_TO (size, LOS_CHUNK_SIZE);

	num_chunks = size >> LOS_CHUNK_BITS;

	g_assert (size > 0 && size - sizeof (LOSObject) <= LOS_SECTION_OBJECT_LIMIT);
	g_assert (num_chunks > 0);

 retry:
	if (num_chunks >= LOS_NUM_FAST_SIZES) {
		free_chunks = get_from_size_list (&los_fast_free_lists [0], size);
	} else {
		size_t i;
		for (i = num_chunks; i < LOS_NUM_FAST_SIZES; ++i) {
			free_chunks = get_from_size_list (&los_fast_free_lists [i], size);
			if (free_chunks)
				break;
		}
		if (!free_chunks)
			free_chunks = get_from_size_list (&los_fast_free_lists [0], size);
	}

	if (free_chunks) {
		return randomize_los_object_start (free_chunks, obj_size, size, LOS_CHUNK_SIZE);
	}

	if (!sgen_memgov_try_alloc_space (LOS_SECTION_SIZE, SPACE_LOS)) {
		/*
		 * We failed to allocate a section due to exceeding max_heap_size.
		 * Trigger a major collection and try again.
		 */
		sgen_ensure_free_space (LOS_SECTION_SIZE, GENERATION_OLD);
		if (!sgen_memgov_try_alloc_space (LOS_SECTION_SIZE, SPACE_LOS))
			return NULL;
	}

	section = (LOSSection *)sgen_alloc_os_memory_aligned (LOS_SECTION_SIZE, LOS_SECTION_SIZE, (SgenAllocFlags)(SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE), NULL, MONO_MEM_ACCOUNT_SGEN_LOS);

	if (!section)
		return NULL;

	free_chunks = (LOSFreeChunks*)((char*)section + LOS_CHUNK_SIZE);
	free_chunks->size = LOS_SECTION_SIZE - LOS_CHUNK_SIZE;
	free_chunks->next_size = los_fast_free_lists [0];
	los_fast_free_lists [0] = free_chunks;

	section->num_free_chunks = LOS_SECTION_NUM_CHUNKS;

	section->free_chunk_map = (unsigned char*)section + sizeof (LOSSection);
	g_assert (sizeof (LOSSection) + LOS_SECTION_NUM_CHUNKS + 1 <= LOS_CHUNK_SIZE);
	section->free_chunk_map [0] = 0;
	memset (section->free_chunk_map + 1, 1, LOS_SECTION_NUM_CHUNKS);

	section->next = los_sections;
	los_sections = section;

	sgen_los_memory_usage_total += LOS_SECTION_SIZE;
	++los_num_sections;

	goto retry;
}

static void
free_los_section_memory (LOSObject *obj, size_t size)
{
	LOSSection *section = LOS_SECTION_FOR_OBJ (obj);
	size_t num_chunks, i, start_index;

	size = SGEN_ALIGN_UP_TO (size, LOS_CHUNK_SIZE);

	num_chunks = size >> LOS_CHUNK_BITS;

	g_assert (size > 0 && size - sizeof (LOSObject) <= LOS_SECTION_OBJECT_LIMIT);
	g_assert (num_chunks > 0);

	section->num_free_chunks += num_chunks;
	g_assert (section->num_free_chunks <= LOS_SECTION_NUM_CHUNKS);

	/*
	 * We could free the LOS section here if it's empty, but we
	 * can't unless we also remove its free chunks from the fast
	 * free lists.  Instead, we do it in los_sweep().
	 */

	start_index = LOS_CHUNK_INDEX (obj, section);
	for (i = start_index; i < start_index + num_chunks; ++i) {
		g_assert (!section->free_chunk_map [i]);
		section->free_chunk_map [i] = 1;
	}

	add_free_chunk ((LOSFreeChunks*)SGEN_ALIGN_DOWN_TO ((mword)obj, LOS_CHUNK_SIZE), size);
}

void
sgen_los_free_object (LOSObject *obj)
{
	if (obj->cardtable_mod_union)
		sgen_card_table_free_mod_union (obj->cardtable_mod_union, (char*)obj->data, sgen_los_object_size (obj));

#ifndef LOS_DUMMY
	mword size = sgen_los_object_size (obj);
	SGEN_LOG (4, "Freed large object %p, size %lu", obj->data, (unsigned long)size);
	sgen_binary_protocol_empty (obj->data, size);

	sgen_los_memory_usage -= size;
	los_num_objects--;

#ifdef USE_MALLOC
	g_free (obj);
#else
	if (size > LOS_SECTION_OBJECT_LIMIT) {
		int pagesize = mono_pagesize ();
		size += sizeof (LOSObject);
		size = SGEN_ALIGN_UP_TO (size, pagesize);
		sgen_free_os_memory ((gpointer)SGEN_ALIGN_DOWN_TO ((mword)obj, pagesize), size, SGEN_ALLOC_HEAP, MONO_MEM_ACCOUNT_SGEN_LOS);
		sgen_los_memory_usage_total -= size;
		sgen_memgov_release_space (size, SPACE_LOS);
	} else {
		free_los_section_memory (obj, size + sizeof (LOSObject));
#ifdef LOS_CONSISTENCY_CHECKS
		los_consistency_check ();
#endif
	}
#endif
#endif
}

/*
 * Objects with size >= MAX_SMALL_SIZE are allocated in the large object space.
 * They are currently kept track of with a linked list.
 * They don't move, so there is no need to pin them during collection
 * and we avoid the memcpy overhead.
 */
void*
sgen_los_alloc_large_inner (GCVTable vtable, size_t size)
{
	LOSObject *obj = NULL;
	void **vtslot;

	g_assert (size > SGEN_MAX_SMALL_OBJ_SIZE);
	g_assert ((size & 1) == 0);

	/*
	 * size + sizeof (LOSObject) <= SSIZE_MAX - (mono_pagesize () - 1)
	 *
	 * therefore:
	 *
	 * size <= SSIZE_MAX - (mono_pagesize () - 1) - sizeof (LOSObject)
	 */
	if (size > SSIZE_MAX - (mono_pagesize () - 1) - sizeof (LOSObject))
		return NULL;

#ifdef LOS_DUMMY
	if (!los_segment)
		los_segment = sgen_alloc_os_memory (LOS_SEGMENT_SIZE, SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE, NULL);
	los_segment_index = ALIGN_UP (los_segment_index);

	obj = (LOSObject*)(los_segment + los_segment_index);
	los_segment_index += size + sizeof (LOSObject);
	g_assert (los_segment_index <= LOS_SEGMENT_SIZE);
#else
	sgen_ensure_free_space (size, GENERATION_OLD);

#ifdef USE_MALLOC
	obj = g_malloc (size + sizeof (LOSObject));
	memset (obj, 0, size + sizeof (LOSObject));
#else
	if (size > LOS_SECTION_OBJECT_LIMIT) {
		size_t obj_size = size + sizeof (LOSObject);
		int pagesize = mono_pagesize ();
		size_t alloc_size = SGEN_ALIGN_UP_TO (obj_size, pagesize);
		if (sgen_memgov_try_alloc_space (alloc_size, SPACE_LOS)) {
			obj = (LOSObject *)sgen_alloc_os_memory (alloc_size, (SgenAllocFlags)(SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE), NULL, MONO_MEM_ACCOUNT_SGEN_LOS);
			if (obj) {
				sgen_los_memory_usage_total += alloc_size;
				obj = randomize_los_object_start (obj, obj_size, alloc_size, pagesize);
			}
		}
	} else {
		obj = get_los_section_memory (size + sizeof (LOSObject));
		if (obj)
			memset (obj, 0, size + sizeof (LOSObject));
	}
#endif
#endif
	if (!obj)
		return NULL;
	g_assert (!((mword)obj->data & (SGEN_ALLOC_ALIGN - 1)));
	obj->size = size;
	vtslot = (void**)obj->data;
	*vtslot = vtable;
	sgen_update_heap_boundaries ((mword)obj->data, (mword)obj->data + size);

	/*
	 * We need a memory barrier so we don't expose a LOSObject
	 * that doesn't have its fields initialized.
	 */
	mono_memory_write_barrier ();
	sgen_array_list_add (&sgen_los_object_array_list, LOS_OBJECT_TAG (obj), 0, FALSE);
	sgen_los_memory_usage += size;
	los_num_objects++;
	SGEN_LOG (4, "Allocated large object %p, vtable: %p (%s), size: %" G_GSIZE_FORMAT "d", obj->data, vtable, sgen_client_vtable_get_name (vtable), size);
	sgen_binary_protocol_alloc (obj->data, vtable, size, sgen_client_get_provenance ());

#ifdef LOS_CONSISTENCY_CHECK
	los_consistency_check ();
#endif

	return obj->data;
}

static void sgen_los_unpin_object (GCObject *data);

void
sgen_los_sweep (void)
{
	LOSObject *obj;
	LOSSection *section, *prev;
	int i;
	int num_sections = 0;

	/* sweep the big objects list */
	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		SGEN_ASSERT (0, !SGEN_OBJECT_IS_PINNED (obj->data), "Who pinned a LOS object?");

		if (sgen_los_object_is_pinned (obj->data)) {
			if (obj->cardtable_mod_union) {
				mword obj_size = sgen_los_object_size (obj);
				mword num_cards = sgen_card_table_number_of_cards_in_range ((mword) obj->data, obj_size);
				memset (obj->cardtable_mod_union, 0, num_cards);
			}

			sgen_los_unpin_object (obj->data);
			sgen_update_heap_boundaries ((mword)obj->data, (mword)obj->data + sgen_los_object_size (obj));
		} else {
			FREE_CURRENT_LOS_OBJECT_NO_LOCK (obj);
			compact_los_objects = TRUE;
			continue;
		}
	} END_FOREACH_LOS_OBJECT_NO_LOCK;

	/* Try to compact list, if needed. */
	if (compact_los_objects) {
		sgen_array_list_remove_nulls (&sgen_los_object_array_list);
		compact_los_objects = FALSE;
	}

	/* Try to free memory */
	for (i = 0; i < LOS_NUM_FAST_SIZES; ++i)
		los_fast_free_lists [i] = NULL;

	prev = NULL;
	section = los_sections;
	while (section) {
		if (section->num_free_chunks == LOS_SECTION_NUM_CHUNKS) {
			LOSSection *next = section->next;
			if (prev)
				prev->next = next;
			else
				los_sections = next;
			sgen_free_os_memory (section, LOS_SECTION_SIZE, SGEN_ALLOC_HEAP, MONO_MEM_ACCOUNT_SGEN_LOS);
			sgen_memgov_release_space (LOS_SECTION_SIZE, SPACE_LOS);
			section = next;
			--los_num_sections;
			sgen_los_memory_usage_total -= LOS_SECTION_SIZE;
			continue;
		}

		for (i = 0; i <= LOS_SECTION_NUM_CHUNKS; ++i) {
			if (section->free_chunk_map [i]) {
				int j;
				for (j = i + 1; j <= LOS_SECTION_NUM_CHUNKS && section->free_chunk_map [j]; ++j)
					;
				add_free_chunk ((LOSFreeChunks*)((char*)section + (i << LOS_CHUNK_BITS)), (j - i) << LOS_CHUNK_BITS);
				i = j - 1;
			}
		}

		prev = section;
		section = section->next;

		++num_sections;
	}

#ifdef LOS_CONSISTENCY_CHECK
	los_consistency_check ();
#endif

	/*
	g_print ("LOS sections: %d  objects: %d  usage: %d\n", num_sections, los_num_objects, sgen_los_memory_usage);
	for (i = 0; i < LOS_NUM_FAST_SIZES; ++i) {
		int num_chunks = 0;
		LOSFreeChunks *free_chunks;
		for (free_chunks = los_fast_free_lists [i]; free_chunks; free_chunks = free_chunks->next_size)
			++num_chunks;
		g_print ("  %d: %d\n", i, num_chunks);
	}
	*/

	g_assert (los_num_sections == num_sections);
}

gboolean
sgen_ptr_is_in_los (char *ptr, char **start)
{
	LOSObject *obj;

	if (start)
		*start = NULL;
	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		char *end = (char*)obj->data + sgen_los_object_size (obj);

		if (ptr >= (char*)obj->data && ptr < end) {
			if (start)
				*start = (char*)obj->data;
			return TRUE;
		}
	} END_FOREACH_LOS_OBJECT_NO_LOCK;
	return FALSE;
}

void
sgen_los_iterate_objects (IterateObjectCallbackFunc cb, void *user_data)
{
	LOSObject *obj;

	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		cb (obj->data, sgen_los_object_size (obj), user_data);
	} END_FOREACH_LOS_OBJECT_NO_LOCK;
}

void
sgen_los_iterate_objects_free (IterateObjectResultCallbackFunc cb, void *user_data)
{
	LOSObject *obj;
	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		if (cb (obj->data, sgen_los_object_size (obj), user_data)) {
			SGEN_LOG (4, "Freeing large object %p",obj->data);
			FREE_CURRENT_LOS_OBJECT_NO_LOCK (obj);
			compact_los_objects = TRUE;
			continue;
		}
	} END_FOREACH_LOS_OBJECT_NO_LOCK;
}

gboolean
sgen_los_is_valid_object (char *object)
{
	LOSObject *obj;

	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		if ((char*)obj->data == object)
			return TRUE;
	} END_FOREACH_LOS_OBJECT_NO_LOCK;
	return FALSE;
}

gboolean
mono_sgen_los_describe_pointer (char *ptr)
{
	LOSObject *obj;

	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		const char *los_kind;
		mword size;
		gboolean pinned;

		if ((char*)obj->data > ptr || (char*)obj->data + sgen_los_object_size (obj) <= ptr)
			continue;

		size = sgen_los_object_size (obj);
		pinned = sgen_los_object_is_pinned (obj->data);

		if (size > LOS_SECTION_OBJECT_LIMIT)
			los_kind = "huge-los-ptr";
		else
			los_kind = "los-ptr";

		if ((char*)obj->data == ptr) {
			SGEN_LOG (0, "%s (size %d pin %d)\n", los_kind, (int)size, pinned ? 1 : 0);
		} else {
			SGEN_LOG (0, "%s (interior-ptr offset %" G_GSIZE_FORMAT "d size %d pin %d)",
					los_kind, ptr - (char*)obj->data, (int)size, pinned ? 1 : 0);
		}

		return TRUE;
	} END_FOREACH_LOS_OBJECT_NO_LOCK;
	return FALSE;
}

void
sgen_los_iterate_live_block_ranges (sgen_cardtable_block_callback callback)
{
	LOSObject *obj;
	gboolean has_references;
	FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK (obj, has_references) {
		if (has_references)
			callback ((mword)obj->data, sgen_los_object_size (obj));
	} END_FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK;
}

static void
get_los_object_range_for_job (int job_index, int job_split_count, int *start, int *end)
{
	int object_count = sgen_los_object_array_list.next_slot / job_split_count;

	*start = object_count * job_index;
	if (job_index == job_split_count - 1)
		*end = sgen_los_object_array_list.next_slot;
	else
		*end = object_count * (job_index + 1);
}

void
sgen_los_iterate_live_block_range_jobs (sgen_cardtable_block_callback callback, int job_index, int job_split_count)
{
	LOSObject *obj;
	gboolean has_references;
	int first_object, last_object, index;

	get_los_object_range_for_job (job_index, job_split_count, &first_object, &last_object);

	FOREACH_LOS_OBJECT_RANGE_HAS_REFERENCES_NO_LOCK (obj, first_object, last_object, index, has_references) {
		if (has_references)
			callback ((mword)obj->data, sgen_los_object_size (obj));
	} END_FOREACH_LOS_OBJECT_RANGE_HAS_REFERENCES_NO_LOCK;
}

static guint8*
get_cardtable_mod_union_for_object (LOSObject *obj)
{
	mword size = sgen_los_object_size (obj);
	guint8 *mod_union = obj->cardtable_mod_union;
	guint8 *other;
	if (mod_union)
		return mod_union;
	mod_union = sgen_card_table_alloc_mod_union ((char*)obj->data, size);
	other = (guint8 *)SGEN_CAS_PTR ((gpointer*)&obj->cardtable_mod_union, mod_union, NULL);
	if (!other) {
		SGEN_ASSERT (0, obj->cardtable_mod_union == mod_union, "Why did CAS not replace?");
		return mod_union;
	}
	sgen_card_table_free_mod_union (mod_union, (char*)obj->data, size);
	return other;
}

void
sgen_los_scan_card_table (CardTableScanType scan_type, ScanCopyContext ctx, int job_index, int job_split_count)
{
	LOSObject *obj;
	gboolean has_references;
	int first_object, last_object, index;

	sgen_binary_protocol_los_card_table_scan_start (sgen_timestamp (), scan_type & CARDTABLE_SCAN_MOD_UNION);

	get_los_object_range_for_job (job_index, job_split_count, &first_object, &last_object);

	FOREACH_LOS_OBJECT_RANGE_HAS_REFERENCES_NO_LOCK (obj, first_object, last_object, index, has_references) {
		mword num_cards = 0;
		guint8 *cards;

		if (!has_references)
			continue;

		if (scan_type & CARDTABLE_SCAN_MOD_UNION) {
			if (!sgen_los_object_is_pinned (obj->data))
				continue;

			if (!obj->cardtable_mod_union)
				continue;

			cards = get_cardtable_mod_union_for_object (obj);
			g_assert (cards);
			if (scan_type == CARDTABLE_SCAN_MOD_UNION_PRECLEAN) {
				guint8 *cards_preclean;
				mword obj_size = sgen_los_object_size (obj);
				num_cards = sgen_card_table_number_of_cards_in_range ((mword) obj->data, obj_size);
				cards_preclean = (guint8 *)sgen_alloc_internal_dynamic (num_cards, INTERNAL_MEM_CARDTABLE_MOD_UNION, TRUE);

				sgen_card_table_preclean_mod_union (cards, cards_preclean, num_cards);

				cards = cards_preclean;
			}
		} else {
			cards = NULL;
		}

		sgen_cardtable_scan_object (obj->data, sgen_los_object_size (obj), cards, ctx);

		if (scan_type == CARDTABLE_SCAN_MOD_UNION_PRECLEAN)
			sgen_free_internal_dynamic (cards, num_cards, INTERNAL_MEM_CARDTABLE_MOD_UNION);
	} END_FOREACH_LOS_OBJECT_RANGE_HAS_REFERENCES_NO_LOCK;
	sgen_binary_protocol_los_card_table_scan_end (sgen_timestamp (), scan_type & CARDTABLE_SCAN_MOD_UNION);
}

void
sgen_los_count_cards (long long *num_total_cards, long long *num_marked_cards)
{
	LOSObject *obj;
	gboolean has_references;
	long long total_cards = 0;
	long long marked_cards = 0;

	FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK (obj, has_references) {
		int i;
		guint8 *cards = sgen_card_table_get_card_scan_address ((mword) obj->data);
		guint8 *cards_end = sgen_card_table_get_card_scan_address ((mword) obj->data + sgen_los_object_size (obj) - 1);
		mword num_cards = (cards_end - cards) + 1;

		if (!has_references)
			continue;

		total_cards += num_cards;
		for (i = 0; i < num_cards; ++i) {
			if (cards [i])
				++marked_cards;
		}
	} END_FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK;

	*num_total_cards = total_cards;
	*num_marked_cards = marked_cards;
}

void
sgen_los_update_cardtable_mod_union (void)
{
	LOSObject *obj;
	gboolean has_references;
	FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK (obj, has_references) {
		if (!has_references)
			continue;
		sgen_card_table_update_mod_union (get_cardtable_mod_union_for_object (obj),
				(char*)obj->data, sgen_los_object_size (obj), NULL);
	} END_FOREACH_LOS_OBJECT_HAS_REFERENCES_NO_LOCK;
}

LOSObject*
sgen_los_header_for_object (GCObject *data)
{
	return (LOSObject*)((char*)data - offsetof(LOSObject, data));
}

void
sgen_los_pin_object (GCObject *data)
{
	LOSObject *obj = sgen_los_header_for_object (data);
	obj->size = obj->size | 1;
	sgen_binary_protocol_pin (data, (gpointer)SGEN_LOAD_VTABLE (data), sgen_safe_object_get_size (data));
}

void
sgen_los_pin_objects (SgenGrayQueue *gray_queue, gboolean finish_concurrent_mode)
{
	LOSObject *obj;
	FOREACH_LOS_OBJECT_NO_LOCK (obj) {
		size_t dummy;
		if (sgen_find_optimized_pin_queue_area ((char*)obj->data, (char*)obj->data + sgen_los_object_size (obj), &dummy, &dummy)) {
			sgen_binary_protocol_pin (obj->data, (gpointer)SGEN_LOAD_VTABLE (obj->data), sgen_safe_object_get_size (obj->data));

			if (sgen_los_object_is_pinned (obj->data)) {
				SGEN_ASSERT (0, finish_concurrent_mode == TRUE, "LOS objects can only be pinned here after concurrent marking.");
				continue;
			}
			sgen_los_pin_object (obj->data);
			if (SGEN_OBJECT_HAS_REFERENCES (obj->data))
				GRAY_OBJECT_ENQUEUE_SERIAL (gray_queue, obj->data, sgen_obj_get_descriptor ((GCObject*)obj->data));
			sgen_pin_stats_register_object (obj->data, GENERATION_OLD);
			SGEN_LOG (6, "Marked large object %p (%s) size: %lu from roots", obj->data,
					sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (obj->data)),
					(unsigned long)sgen_los_object_size (obj));

			sgen_client_pinned_los_object (obj->data);
		}
	} END_FOREACH_LOS_OBJECT_NO_LOCK;
}

gboolean
sgen_los_pin_object_par (GCObject *data)
{
	LOSObject *obj = sgen_los_header_for_object (data);
	mword old_size = obj->size;
	if (old_size & 1)
		return FALSE;
#if SIZEOF_VOID_P == 4
	old_size = mono_atomic_cas_i32 ((volatile gint32*)&obj->size, old_size | 1, old_size);
#else
	old_size = mono_atomic_cas_i64 ((volatile gint64*)&obj->size, old_size | 1, old_size);
#endif
	if (old_size & 1)
		return FALSE;
	sgen_binary_protocol_pin (data, (gpointer)SGEN_LOAD_VTABLE (data), sgen_safe_object_get_size (data));
	return TRUE;
}

static void
sgen_los_unpin_object (GCObject *data)
{
	LOSObject *obj = sgen_los_header_for_object (data);
	obj->size = sgen_los_object_size (obj);
}

gboolean
sgen_los_object_is_pinned (GCObject *data)
{
	LOSObject *obj = sgen_los_header_for_object (data);
	return obj->size & 1;
}

void
sgen_los_mark_mod_union_card (GCObject *mono_obj, void **ptr)
{
	LOSObject *obj = sgen_los_header_for_object (mono_obj);
	guint8 *mod_union = get_cardtable_mod_union_for_object (obj);
	/* The LOSObject structure is not represented within the card space */
	size_t offset = sgen_card_table_get_card_offset ((char*)ptr, (char*)sgen_card_table_align_pointer((char*)mono_obj));
	SGEN_ASSERT (0, mod_union, "FIXME: optionally allocate the mod union if it's not here and CAS it in.");
	mod_union [offset] = 1;
}

#endif /* HAVE_SGEN_GC */
