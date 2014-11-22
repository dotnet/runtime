/*
 * sgen-los.c: Large objects space.
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

#include "config.h"

#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-cardtable.h"
#include "metadata/sgen-memory-governor.h"
#include "utils/mono-mmap.h"
#include "utils/mono-compiler.h"

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

LOSObject *los_object_list = NULL;
mword los_memory_usage = 0;

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

#ifdef LOS_CONSISTENCY_CHECK
static void
los_consistency_check (void)
{
	LOSSection *section;
	LOSObject *obj;
	int i;
	mword memory_usage = 0;

	for (obj = los_object_list; obj; obj = obj->next) {
		char *end = obj->data + obj->size;
		int start_index, num_chunks;

		memory_usage += obj->size;

		if (obj->size > LOS_SECTION_OBJECT_LIMIT)
			continue;

		section = LOS_SECTION_FOR_OBJ (obj);

		g_assert (end <= (char*)section + LOS_SECTION_SIZE);

		start_index = LOS_CHUNK_INDEX (obj, section);
		num_chunks = (obj->size + sizeof (LOSObject) + LOS_CHUNK_SIZE - 1) >> LOS_CHUNK_BITS;
		for (i = start_index; i < start_index + num_chunks; ++i)
			g_assert (!section->free_chunk_map [i]);
	}

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

	g_assert (los_memory_usage == memory_usage);
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
get_los_section_memory (size_t size)
{
	LOSSection *section;
	LOSFreeChunks *free_chunks;
	size_t num_chunks;

	size += LOS_CHUNK_SIZE - 1;
	size &= ~(LOS_CHUNK_SIZE - 1);

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

	if (free_chunks)
		return (LOSObject*)free_chunks;

	if (!sgen_memgov_try_alloc_space (LOS_SECTION_SIZE, SPACE_LOS))
		return NULL;

	section = sgen_alloc_os_memory_aligned (LOS_SECTION_SIZE, LOS_SECTION_SIZE, SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE, NULL);

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

	++los_num_sections;

	goto retry;
}

static void
free_los_section_memory (LOSObject *obj, size_t size)
{
	LOSSection *section = LOS_SECTION_FOR_OBJ (obj);
	size_t num_chunks, i, start_index;

	size += LOS_CHUNK_SIZE - 1;
	size &= ~(LOS_CHUNK_SIZE - 1);

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

	add_free_chunk ((LOSFreeChunks*)obj, size);
}

static int pagesize;

void
sgen_los_free_object (LOSObject *obj)
{
#ifndef LOS_DUMMY
	size_t size = obj->size;
	SGEN_LOG (4, "Freed large object %p, size %lu", obj->data, (unsigned long)obj->size);
	binary_protocol_empty (obj->data, obj->size);

	los_memory_usage -= size;
	los_num_objects--;

#ifdef USE_MALLOC
	free (obj);
#else
	if (size > LOS_SECTION_OBJECT_LIMIT) {
		if (!pagesize)
			pagesize = mono_pagesize ();
		size += sizeof (LOSObject);
		size += pagesize - 1;
		size &= ~(pagesize - 1);
		sgen_free_os_memory (obj, size, SGEN_ALLOC_HEAP);
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
sgen_los_alloc_large_inner (MonoVTable *vtable, size_t size)
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
	sgen_ensure_free_space (size);

#ifdef USE_MALLOC
	obj = malloc (size + sizeof (LOSObject));
	memset (obj, 0, size + sizeof (LOSObject));
#else
	if (size > LOS_SECTION_OBJECT_LIMIT) {
		size_t alloc_size = size;
		if (!pagesize)
			pagesize = mono_pagesize ();
		alloc_size += sizeof (LOSObject);
		alloc_size += pagesize - 1;
		alloc_size &= ~(pagesize - 1);
		if (sgen_memgov_try_alloc_space (alloc_size, SPACE_LOS)) {
			obj = sgen_alloc_os_memory (alloc_size, SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE, NULL);
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
	obj->next = los_object_list;
	los_object_list = obj;
	los_memory_usage += size;
	los_num_objects++;
	SGEN_LOG (4, "Allocated large object %p, vtable: %p (%s), size: %zd", obj->data, vtable, vtable->klass->name, size);
	binary_protocol_alloc (obj->data, vtable, size);

#ifdef LOS_CONSISTENCY_CHECK
	los_consistency_check ();
#endif

	return obj->data;
}

void
sgen_los_sweep (void)
{
	LOSSection *section, *prev;
	int i;
	int num_sections = 0;

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
			sgen_free_os_memory (section, LOS_SECTION_SIZE, SGEN_ALLOC_HEAP);
			sgen_memgov_release_space (LOS_SECTION_SIZE, SPACE_LOS);
			section = next;
			--los_num_sections;
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
	g_print ("LOS sections: %d  objects: %d  usage: %d\n", num_sections, los_num_objects, los_memory_usage);
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

	*start = NULL;
	for (obj = los_object_list; obj; obj = obj->next) {
		char *end = obj->data + obj->size;

		if (ptr >= obj->data && ptr < end) {
			*start = obj->data;
			return TRUE;
		}
	}
	return FALSE;
}

void
sgen_los_iterate_objects (IterateObjectCallbackFunc cb, void *user_data)
{
	LOSObject *obj;

	for (obj = los_object_list; obj; obj = obj->next)
		cb (obj->data, obj->size, user_data);
}

gboolean
sgen_los_is_valid_object (char *object)
{
	LOSObject *obj;

	for (obj = los_object_list; obj; obj = obj->next) {
		if (obj->data == object)
			return TRUE;
	}
	return FALSE;
}

gboolean
mono_sgen_los_describe_pointer (char *ptr)
{
	LOSObject *obj;

	for (obj = los_object_list; obj; obj = obj->next) {
		MonoVTable *vtable;
		const char *los_kind;
		mword size;
		gboolean pinned;

		if (obj->data > ptr || obj->data + obj->size <= ptr)
			continue;

		size = sgen_los_object_size (obj);
		pinned = sgen_los_object_is_pinned (obj->data);

		if (size > LOS_SECTION_OBJECT_LIMIT)
			los_kind = "huge-los-ptr";
		else
			los_kind = "los-ptr";

		vtable = (MonoVTable*)SGEN_LOAD_VTABLE (obj->data);

		if (obj->data == ptr) {
			SGEN_LOG (0, "%s (size %d pin %d)\n", los_kind, (int)size, pinned ? 1 : 0);
		} else {
			SGEN_LOG (0, "%s (interior-ptr offset %td size %d pin %d)",
					  los_kind, ptr - obj->data, (int)size, pinned ? 1 : 0);
		}

		return TRUE;
	}
	return FALSE;
}

void
sgen_los_iterate_live_block_ranges (sgen_cardtable_block_callback callback)
{
	LOSObject *obj;
	for (obj = los_object_list; obj; obj = obj->next) {
		MonoVTable *vt = (MonoVTable*)SGEN_LOAD_VTABLE (obj->data);
		if (SGEN_VTABLE_HAS_REFERENCES (vt))
			callback ((mword)obj->data, (mword)obj->size);
	}
}

void
sgen_los_scan_card_table (gboolean mod_union, SgenGrayQueue *queue)
{
	LOSObject *obj;

	for (obj = los_object_list; obj; obj = obj->next) {
		guint8 *cards;

		if (!SGEN_OBJECT_HAS_REFERENCES (obj->data))
			continue;

		if (mod_union) {
			cards = obj->cardtable_mod_union;
			g_assert (cards);
		} else {
			cards = NULL;
		}

		sgen_cardtable_scan_object (obj->data, obj->size, cards, mod_union, queue);
	}
}

void
sgen_los_count_cards (long long *num_total_cards, long long *num_marked_cards)
{
	LOSObject *obj;
	long long total_cards = 0;
	long long marked_cards = 0;

	for (obj = los_object_list; obj; obj = obj->next) {
		int i;
		guint8 *cards = sgen_card_table_get_card_scan_address ((mword) obj->data);
		guint8 *cards_end = sgen_card_table_get_card_scan_address ((mword) obj->data + obj->size - 1);
		mword num_cards = (cards_end - cards) + 1;

		if (!SGEN_OBJECT_HAS_REFERENCES (obj->data))
			continue;

		total_cards += num_cards;
		for (i = 0; i < num_cards; ++i) {
			if (cards [i])
				++marked_cards;
		}
	}

	*num_total_cards = total_cards;
	*num_marked_cards = marked_cards;
}

void
sgen_los_update_cardtable_mod_union (void)
{
	LOSObject *obj;

	for (obj = los_object_list; obj; obj = obj->next) {
		if (!SGEN_OBJECT_HAS_REFERENCES (obj->data))
			continue;
		obj->cardtable_mod_union = sgen_card_table_update_mod_union (obj->cardtable_mod_union,
				obj->data, obj->size, NULL);
	}
}

mword
sgen_los_object_size (LOSObject *obj)
{
	return obj->size & ~1L;
}

LOSObject*
sgen_los_header_for_object (char *data)
{
#if _MSC_VER
	return (LOSObject*)(data - (int)(&(((LOSObject*)0)->data)));
#else
	return (LOSObject*)(data - sizeof (LOSObject));
#endif
}

void
sgen_los_pin_object (char *data)
{
	LOSObject *obj = sgen_los_header_for_object (data);
	obj->size = obj->size | 1;
	binary_protocol_pin (data, (gpointer)SGEN_LOAD_VTABLE (data), sgen_safe_object_get_size ((MonoObject*)data));
}

void
sgen_los_unpin_object (char *data)
{
	LOSObject *obj = sgen_los_header_for_object (data);
	obj->size = sgen_los_object_size (obj);
}

gboolean
sgen_los_object_is_pinned (char *data)
{
	LOSObject *obj = sgen_los_header_for_object (data);
	return obj->size & 1;
}

#endif /* HAVE_SGEN_GC */
