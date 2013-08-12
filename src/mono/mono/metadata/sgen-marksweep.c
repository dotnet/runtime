/*
 * sgen-marksweep.c: The Mark & Sweep major collector.
 *
 * Author:
 * 	Mark Probst <mark.probst@gmail.com>
 *
 * Copyright 2009-2010 Novell, Inc.
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

#include <math.h>
#include <errno.h>

#include "utils/mono-counters.h"
#include "utils/mono-semaphore.h"
#include "utils/mono-time.h"
#include "metadata/object-internals.h"
#include "metadata/profiler-private.h"

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-cardtable.h"
#include "metadata/sgen-memory-governor.h"
#include "metadata/sgen-layout-stats.h"
#include "metadata/gc-internal.h"

#if !defined(SGEN_PARALLEL_MARK) && !defined(FIXED_HEAP)
#define SGEN_HAVE_CONCURRENT_MARK
#endif

#define MS_BLOCK_SIZE	(16*1024)
#define MS_BLOCK_SIZE_SHIFT	14
#define MAJOR_SECTION_SIZE	MS_BLOCK_SIZE
#define CARDS_PER_BLOCK (MS_BLOCK_SIZE / CARD_SIZE_IN_BYTES)

#ifdef FIXED_HEAP
#define MS_DEFAULT_HEAP_NUM_BLOCKS	(32 * 1024) /* 512 MB */
#endif

/*
 * Don't allocate single blocks, but alloc a contingent of this many
 * blocks in one swoop.  This must be a power of two.
 */
#define MS_BLOCK_ALLOC_NUM	32

/*
 * Number of bytes before the first object in a block.  At the start
 * of a block is the MSBlockHeader, then opional padding, then come
 * the objects, so this must be >= sizeof (MSBlockHeader).
 */
#ifdef FIXED_HEAP
#define MS_BLOCK_SKIP	0
#else
#define MS_BLOCK_SKIP	16
#endif

#define MS_BLOCK_FREE	(MS_BLOCK_SIZE - MS_BLOCK_SKIP)

#define MS_NUM_MARK_WORDS	((MS_BLOCK_SIZE / SGEN_ALLOC_ALIGN + sizeof (mword) * 8 - 1) / (sizeof (mword) * 8))

#if SGEN_MAX_SMALL_OBJ_SIZE > MS_BLOCK_FREE / 2
#error MAX_SMALL_OBJ_SIZE must be at most MS_BLOCK_FREE / 2
#endif

typedef struct _MSBlockInfo MSBlockInfo;
struct _MSBlockInfo {
	int obj_size;
	int obj_size_index;
	int pin_queue_num_entries;
	unsigned int pinned : 1;
	unsigned int has_references : 1;
	unsigned int has_pinned : 1;	/* means cannot evacuate */
	unsigned int is_to_space : 1;
	unsigned int swept : 1;
#ifdef FIXED_HEAP
	unsigned int used : 1;
	unsigned int zeroed : 1;
#endif
	MSBlockInfo *next;
	char *block;
	void **free_list;
	MSBlockInfo *next_free;
	void **pin_queue_start;
#ifdef SGEN_HAVE_CONCURRENT_MARK
	guint8 *cardtable_mod_union;
#endif
	mword mark_words [MS_NUM_MARK_WORDS];
};

#ifdef FIXED_HEAP
static mword ms_heap_num_blocks = MS_DEFAULT_HEAP_NUM_BLOCKS;

static char *ms_heap_start;
static char *ms_heap_end;

#define MS_PTR_IN_SMALL_MAJOR_HEAP(p)	((char*)(p) >= ms_heap_start && (char*)(p) < ms_heap_end)

/* array of all all block infos in the system */
static MSBlockInfo *block_infos;
#endif

#define MS_BLOCK_OBJ(b,i)		((b)->block + MS_BLOCK_SKIP + (b)->obj_size * (i))
#define MS_BLOCK_OBJ_FOR_SIZE(b,i,obj_size)		((b)->block + MS_BLOCK_SKIP + (obj_size) * (i))
#define MS_BLOCK_DATA_FOR_OBJ(o)	((char*)((mword)(o) & ~(mword)(MS_BLOCK_SIZE - 1)))

#ifdef FIXED_HEAP
#define MS_BLOCK_FOR_OBJ(o)		(&block_infos [(mword)((char*)(o) - ms_heap_start) >> MS_BLOCK_SIZE_SHIFT])
#else
typedef struct {
	MSBlockInfo *info;
} MSBlockHeader;

#define MS_BLOCK_FOR_OBJ(o)		(((MSBlockHeader*)MS_BLOCK_DATA_FOR_OBJ ((o)))->info)
#endif

#define MS_BLOCK_OBJ_INDEX(o,b)	(((char*)(o) - ((b)->block + MS_BLOCK_SKIP)) / (b)->obj_size)

#define MS_CALC_MARK_BIT(w,b,o) 	do {				\
		int i = ((char*)(o) - MS_BLOCK_DATA_FOR_OBJ ((o))) >> SGEN_ALLOC_ALIGN_BITS; \
		if (sizeof (mword) == 4) {				\
			(w) = i >> 5;					\
			(b) = i & 31;					\
		} else {						\
			(w) = i >> 6;					\
			(b) = i & 63;					\
		}							\
	} while (0)

#define MS_MARK_BIT(bl,w,b)	((bl)->mark_words [(w)] & (1L << (b)))
#define MS_SET_MARK_BIT(bl,w,b)	((bl)->mark_words [(w)] |= (1L << (b)))
#define MS_PAR_SET_MARK_BIT(was_marked,bl,w,b)	do {			\
		mword __old = (bl)->mark_words [(w)];			\
		mword __bitmask = 1L << (b);				\
		if (__old & __bitmask) {				\
			was_marked = TRUE;				\
			break;						\
		}							\
		if (SGEN_CAS_PTR ((gpointer*)&(bl)->mark_words [(w)],	\
						(gpointer)(__old | __bitmask), \
						(gpointer)__old) ==	\
				(gpointer)__old) {			\
			was_marked = FALSE;				\
			break;						\
		}							\
	} while (1)

#define MS_OBJ_ALLOCED(o,b)	(*(void**)(o) && (*(char**)(o) < (b)->block || *(char**)(o) >= (b)->block + MS_BLOCK_SIZE))

#define MS_BLOCK_OBJ_SIZE_FACTOR	(sqrt (2.0))

/*
 * This way we can lookup block object size indexes for sizes up to
 * 256 bytes with a single load.
 */
#define MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES	32

static int *block_obj_sizes;
static int num_block_obj_sizes;
static int fast_block_obj_size_indexes [MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES];

#define MS_BLOCK_FLAG_PINNED	1
#define MS_BLOCK_FLAG_REFS	2

#define MS_BLOCK_TYPE_MAX	4

#ifdef SGEN_PARALLEL_MARK
static LOCK_DECLARE (ms_block_list_mutex);
#define LOCK_MS_BLOCK_LIST mono_mutex_lock (&ms_block_list_mutex)
#define UNLOCK_MS_BLOCK_LIST mono_mutex_unlock (&ms_block_list_mutex)
#endif

static gboolean *evacuate_block_obj_sizes;
static float evacuation_threshold = 0.666;
#ifdef SGEN_HAVE_CONCURRENT_MARK
static float concurrent_evacuation_threshold = 0.666;
static gboolean want_evacuation = FALSE;
#endif

static gboolean lazy_sweep = TRUE;
static gboolean have_swept;

#ifdef SGEN_HAVE_CONCURRENT_MARK
static gboolean concurrent_mark;
#endif

/* all allocated blocks in the system */
static MSBlockInfo *all_blocks;

#ifdef FIXED_HEAP
/* non-allocated block free-list */
static MSBlockInfo *empty_blocks = NULL;
#else
/* non-allocated block free-list */
static void *empty_blocks = NULL;
static int num_empty_blocks = 0;
#endif

#define FOREACH_BLOCK(bl)	for ((bl) = all_blocks; (bl); (bl) = (bl)->next) {
#define END_FOREACH_BLOCK	}

static int num_major_sections = 0;
/* one free block list for each block object size */
static MSBlockInfo **free_block_lists [MS_BLOCK_TYPE_MAX];

#ifdef SGEN_PARALLEL_MARK
#ifdef HAVE_KW_THREAD
static __thread MSBlockInfo ***workers_free_block_lists;
#else
static MonoNativeTlsKey workers_free_block_lists_key;
#endif
#endif

static long long stat_major_blocks_alloced = 0;
static long long stat_major_blocks_freed = 0;
static long long stat_major_blocks_lazy_swept = 0;
static long long stat_major_objects_evacuated = 0;

#if SIZEOF_VOID_P != 8
static long long stat_major_blocks_freed_ideal = 0;
static long long stat_major_blocks_freed_less_ideal = 0;
static long long stat_major_blocks_freed_individual = 0;
static long long stat_major_blocks_alloced_less_ideal = 0;
#endif

#ifdef SGEN_COUNT_NUMBER_OF_MAJOR_OBJECTS_MARKED
static long long num_major_objects_marked = 0;
#define INC_NUM_MAJOR_OBJECTS_MARKED()	(++num_major_objects_marked)
#else
#define INC_NUM_MAJOR_OBJECTS_MARKED()
#endif

static void
sweep_block (MSBlockInfo *block, gboolean during_major_collection);

static int
ms_find_block_obj_size_index (int size)
{
	int i;
	SGEN_ASSERT (9, size <= SGEN_MAX_SMALL_OBJ_SIZE, "size %d is bigger than max small object size %d", size, SGEN_MAX_SMALL_OBJ_SIZE);
	for (i = 0; i < num_block_obj_sizes; ++i)
		if (block_obj_sizes [i] >= size)
			return i;
	g_error ("no object of size %d\n", size);
}

#define FREE_BLOCKS_FROM(lists,p,r)	(lists [((p) ? MS_BLOCK_FLAG_PINNED : 0) | ((r) ? MS_BLOCK_FLAG_REFS : 0)])
#define FREE_BLOCKS(p,r)		(FREE_BLOCKS_FROM (free_block_lists, (p), (r)))
#ifdef SGEN_PARALLEL_MARK
#ifdef HAVE_KW_THREAD
#define FREE_BLOCKS_LOCAL(p,r)		(FREE_BLOCKS_FROM (workers_free_block_lists, (p), (r)))
#else
#define FREE_BLOCKS_LOCAL(p,r)		(FREE_BLOCKS_FROM (((MSBlockInfo***)(mono_native_tls_get_value (workers_free_block_lists_key))), (p), (r)))
#endif
#else
//#define FREE_BLOCKS_LOCAL(p,r)		(FREE_BLOCKS_FROM (free_block_lists, (p), (r)))
#endif

#define MS_BLOCK_OBJ_SIZE_INDEX(s)				\
	(((s)+7)>>3 < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES ?	\
	 fast_block_obj_size_indexes [((s)+7)>>3] :		\
	 ms_find_block_obj_size_index ((s)))

#ifdef FIXED_HEAP
static void*
major_alloc_heap (mword nursery_size, mword nursery_align, int the_nursery_bits)
{
	char *nursery_start;
	mword major_heap_size = ms_heap_num_blocks * MS_BLOCK_SIZE;
	mword alloc_size = nursery_size + major_heap_size;
	mword i;

	g_assert (ms_heap_num_blocks > 0);
	g_assert (nursery_size % MS_BLOCK_SIZE == 0);
	if (nursery_align)
		g_assert (nursery_align % MS_BLOCK_SIZE == 0);

	nursery_start = sgen_alloc_os_memory_aligned (alloc_size, nursery_align ? nursery_align : MS_BLOCK_SIZE, SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE, "heap");
	ms_heap_start = nursery_start + nursery_size;
	ms_heap_end = ms_heap_start + major_heap_size;

	block_infos = sgen_alloc_internal_dynamic (sizeof (MSBlockInfo) * ms_heap_num_blocks, INTERNAL_MEM_MS_BLOCK_INFO, TRUE);

	for (i = 0; i < ms_heap_num_blocks; ++i) {
		block_infos [i].block = ms_heap_start + i * MS_BLOCK_SIZE;
		if (i < ms_heap_num_blocks - 1)
			block_infos [i].next_free = &block_infos [i + 1];
		else
			block_infos [i].next_free = NULL;
		block_infos [i].zeroed = TRUE;
	}

	empty_blocks = &block_infos [0];

	return nursery_start;
}
#else
static void*
major_alloc_heap (mword nursery_size, mword nursery_align, int the_nursery_bits)
{
	char *start;
	if (nursery_align)
		start = sgen_alloc_os_memory_aligned (nursery_size, nursery_align, SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE, "nursery");
	else
		start = sgen_alloc_os_memory (nursery_size, SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE, "nursery");

	return start;
}
#endif

static void
update_heap_boundaries_for_block (MSBlockInfo *block)
{
	sgen_update_heap_boundaries ((mword)block->block, (mword)block->block + MS_BLOCK_SIZE);
}

#ifdef FIXED_HEAP
static MSBlockInfo*
ms_get_empty_block (void)
{
	MSBlockInfo *block;

	g_assert (empty_blocks);

	do {
		block = empty_blocks;
	} while (SGEN_CAS_PTR ((gpointer*)&empty_blocks, block->next_free, block) != block);

	block->used = TRUE;

	if (!block->zeroed)
		memset (block->block, 0, MS_BLOCK_SIZE);

	return block;
}

static void
ms_free_block (MSBlockInfo *block)
{
	block->next_free = empty_blocks;
	empty_blocks = block;
	block->used = FALSE;
	block->zeroed = FALSE;
	sgen_memgov_release_space (MS_BLOCK_SIZE, SPACE_MAJOR);
}
#else
static void*
ms_get_empty_block (void)
{
	char *p;
	int i;
	void *block, *empty, *next;

 retry:
	if (!empty_blocks) {
		/*
		 * We try allocating MS_BLOCK_ALLOC_NUM blocks first.  If that's
		 * unsuccessful, we halve the number of blocks and try again, until we're at
		 * 1.  If that doesn't work, either, we assert.
		 */
		int alloc_num = MS_BLOCK_ALLOC_NUM;
		for (;;) {
			p = sgen_alloc_os_memory_aligned (MS_BLOCK_SIZE * alloc_num, MS_BLOCK_SIZE, SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE,
					alloc_num == 1 ? "major heap section" : NULL);
			if (p)
				break;
			alloc_num >>= 1;
		}

		for (i = 0; i < alloc_num; ++i) {
			block = p;
			/*
			 * We do the free list update one after the
			 * other so that other threads can use the new
			 * blocks as quickly as possible.
			 */
			do {
				empty = empty_blocks;
				*(void**)block = empty;
			} while (SGEN_CAS_PTR ((gpointer*)&empty_blocks, block, empty) != empty);
			p += MS_BLOCK_SIZE;
		}

		SGEN_ATOMIC_ADD (num_empty_blocks, alloc_num);

		stat_major_blocks_alloced += alloc_num;
#if SIZEOF_VOID_P != 8
		if (alloc_num != MS_BLOCK_ALLOC_NUM)
			stat_major_blocks_alloced_less_ideal += alloc_num;
#endif
	}

	do {
		empty = empty_blocks;
		if (!empty)
			goto retry;
		block = empty;
		next = *(void**)block;
	} while (SGEN_CAS_PTR (&empty_blocks, next, empty) != empty);

	SGEN_ATOMIC_ADD (num_empty_blocks, -1);

	*(void**)block = NULL;

	g_assert (!((mword)block & (MS_BLOCK_SIZE - 1)));

	return block;
}

static void
ms_free_block (void *block)
{
	void *empty;

	sgen_memgov_release_space (MS_BLOCK_SIZE, SPACE_MAJOR);
	memset (block, 0, MS_BLOCK_SIZE);

	do {
		empty = empty_blocks;
		*(void**)block = empty;
	} while (SGEN_CAS_PTR (&empty_blocks, block, empty) != empty);

	SGEN_ATOMIC_ADD (num_empty_blocks, 1);
}
#endif

//#define MARKSWEEP_CONSISTENCY_CHECK

#ifdef MARKSWEEP_CONSISTENCY_CHECK
static void
check_block_free_list (MSBlockInfo *block, int size, gboolean pinned)
{
	MSBlockInfo *b;

	for (; block; block = block->next_free) {
		g_assert (block->obj_size == size);
		g_assert ((pinned && block->pinned) || (!pinned && !block->pinned));

		/* blocks in the free lists must have at least
		   one free slot */
		if (block->swept)
			g_assert (block->free_list);

#ifdef FIXED_HEAP
		/* the block must not be in the empty_blocks list */
		for (b = empty_blocks; b; b = b->next_free)
			g_assert (b != block);
#endif
		/* the block must be in the all_blocks list */
		for (b = all_blocks; b; b = b->next) {
			if (b == block)
				break;
		}
		g_assert (b == block);
	}
}

static void
check_empty_blocks (void)
{
#ifndef FIXED_HEAP
	void *p;
	int i = 0;
	for (p = empty_blocks; p; p = *(void**)p)
		++i;
	g_assert (i == num_empty_blocks);
#endif
}

static void
consistency_check (void)
{
	MSBlockInfo *block;
	int i;

	/* check all blocks */
	FOREACH_BLOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;
		int num_free = 0;
		void **free;

#ifndef FIXED_HEAP
		/* check block header */
		g_assert (((MSBlockHeader*)block->block)->info == block);
#endif

		/* count number of free slots */
		for (i = 0; i < count; ++i) {
			void **obj = (void**) MS_BLOCK_OBJ (block, i);
			if (!MS_OBJ_ALLOCED (obj, block))
				++num_free;
		}

		/* check free list */
		for (free = block->free_list; free; free = (void**)*free) {
			g_assert (MS_BLOCK_FOR_OBJ (free) == block);
			--num_free;
		}
		g_assert (num_free == 0);

		/* check all mark words are zero */
		if (block->swept) {
			for (i = 0; i < MS_NUM_MARK_WORDS; ++i)
				g_assert (block->mark_words [i] == 0);
		}
	} END_FOREACH_BLOCK;

	/* check free blocks */
	for (i = 0; i < num_block_obj_sizes; ++i) {
		int j;
		for (j = 0; j < MS_BLOCK_TYPE_MAX; ++j)
			check_block_free_list (free_block_lists [j][i], block_obj_sizes [i], j & MS_BLOCK_FLAG_PINNED);
	}

	check_empty_blocks ();
}
#endif

static gboolean
ms_alloc_block (int size_index, gboolean pinned, gboolean has_references)
{
	int size = block_obj_sizes [size_index];
	int count = MS_BLOCK_FREE / size;
	MSBlockInfo *info;
#ifdef SGEN_PARALLEL_MARK
	MSBlockInfo *next;
#endif
#ifndef FIXED_HEAP
	MSBlockHeader *header;
#endif
	MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, has_references);
	char *obj_start;
	int i;

	if (!sgen_memgov_try_alloc_space (MS_BLOCK_SIZE, SPACE_MAJOR))
		return FALSE;

#ifdef FIXED_HEAP
	info = ms_get_empty_block ();
#else
	info = sgen_alloc_internal (INTERNAL_MEM_MS_BLOCK_INFO);
#endif

	SGEN_ASSERT (9, count >= 2, "block with %d objects, it must hold at least 2", count);

	info->obj_size = size;
	info->obj_size_index = size_index;
	info->pinned = pinned;
	info->has_references = has_references;
	info->has_pinned = pinned;
	info->is_to_space = (sgen_get_current_collection_generation () == GENERATION_OLD); /*FIXME WHY??? */
	info->swept = 1;
#ifndef FIXED_HEAP
	info->block = ms_get_empty_block ();

	header = (MSBlockHeader*) info->block;
	header->info = info;
#endif
#ifdef SGEN_HAVE_CONCURRENT_MARK
	info->cardtable_mod_union = NULL;
#endif

	update_heap_boundaries_for_block (info);

	/* build free list */
	obj_start = info->block + MS_BLOCK_SKIP;
	info->free_list = (void**)obj_start;
	/* we're skipping the last one - it must be nulled */
	for (i = 0; i < count - 1; ++i) {
		char *next_obj_start = obj_start + size;
		*(void**)obj_start = next_obj_start;
		obj_start = next_obj_start;
	}
	/* the last one */
	*(void**)obj_start = NULL;

#ifdef SGEN_PARALLEL_MARK
	do {
		next = info->next_free = free_blocks [size_index];
	} while (SGEN_CAS_PTR ((void**)&free_blocks [size_index], info, next) != next);

	do {
		next = info->next = all_blocks;
	} while (SGEN_CAS_PTR ((void**)&all_blocks, info, next) != next);
#else
	info->next_free = free_blocks [size_index];
	free_blocks [size_index] = info;

	info->next = all_blocks;
	all_blocks = info;
#endif

	++num_major_sections;
	return TRUE;
}

static gboolean
obj_is_from_pinned_alloc (char *ptr)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		if (ptr >= block->block && ptr <= block->block + MS_BLOCK_SIZE)
			return block->pinned;
	} END_FOREACH_BLOCK;
	return FALSE;
}

static void*
unlink_slot_from_free_list_uncontested (MSBlockInfo **free_blocks, int size_index)
{
	MSBlockInfo *block;
	void *obj;

	block = free_blocks [size_index];
	SGEN_ASSERT (9, block, "no free block to unlink from free_blocks %p size_index %d", free_blocks, size_index);

	if (G_UNLIKELY (!block->swept)) {
		stat_major_blocks_lazy_swept ++;
		sweep_block (block, FALSE);
	}

	obj = block->free_list;
	SGEN_ASSERT (9, obj, "block %p in free list had no available object to alloc from", block);

	block->free_list = *(void**)obj;
	if (!block->free_list) {
		free_blocks [size_index] = block->next_free;
		block->next_free = NULL;
	}

	return obj;
}

#ifdef SGEN_PARALLEL_MARK
static gboolean
try_remove_block_from_free_list (MSBlockInfo *block, MSBlockInfo **free_blocks, int size_index)
{
	/*
	 * No more free slots in the block, so try to free the block.
	 * Don't try again if we don't succeed - another thread will
	 * already have done it.
	 */
	MSBlockInfo *next_block = block->next_free;
	if (SGEN_CAS_PTR ((void**)&free_blocks [size_index], next_block, block) == block) {
		/*
		void *old = SGEN_CAS_PTR ((void**)&block->next_free, NULL, next_block);
		g_assert (old == next_block);
		*/
		block->next_free = NULL;
		return TRUE;
	}
	return FALSE;
}

static void*
alloc_obj_par (MonoVTable *vtable, int size, gboolean pinned, gboolean has_references)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo **free_blocks_local = FREE_BLOCKS_LOCAL (pinned, has_references);
	MSBlockInfo *block;
	void *obj;

#ifdef SGEN_HAVE_CONCURRENT_MARK
	if (concurrent_mark)
		g_assert_not_reached ();
#endif

	SGEN_ASSERT (9, current_collection_generation == GENERATION_OLD, "old gen parallel allocator called from a %d collection", current_collection_generation);

	if (free_blocks_local [size_index]) {
	get_slot:
		obj = unlink_slot_from_free_list_uncontested (free_blocks_local, size_index);
	} else {
		MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, has_references);

	get_block:
		block = free_blocks [size_index];
		if (block) {
			if (!try_remove_block_from_free_list (block, free_blocks, size_index))
				goto get_block;

			g_assert (block->next_free == NULL);
			g_assert (block->free_list);
			block->next_free = free_blocks_local [size_index];
			free_blocks_local [size_index] = block;

			goto get_slot;
		} else {
			gboolean success;

			LOCK_MS_BLOCK_LIST;
			success = ms_alloc_block (size_index, pinned, has_references);
			UNLOCK_MS_BLOCK_LIST;

			if (G_UNLIKELY (!success))
				return NULL;

			goto get_block;
		}
	}

	*(MonoVTable**)obj = vtable;

	return obj;
}

static void*
major_par_alloc_object (MonoVTable *vtable, int size, gboolean has_references)
{
	return alloc_obj_par (vtable, size, FALSE, has_references);
}
#endif

static void*
alloc_obj (MonoVTable *vtable, int size, gboolean pinned, gboolean has_references)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, has_references);
	void *obj;

#ifdef SGEN_PARALLEL_MARK
	SGEN_ASSERT (9, current_collection_generation == GENERATION_OLD, "old gen parallel allocator called from a %d collection", current_collection_generation);

#endif

	if (!free_blocks [size_index]) {
		if (G_UNLIKELY (!ms_alloc_block (size_index, pinned, has_references)))
			return NULL;
	}

	obj = unlink_slot_from_free_list_uncontested (free_blocks, size_index);

	*(MonoVTable**)obj = vtable;

	return obj;
}

static void*
major_alloc_object (MonoVTable *vtable, int size, gboolean has_references)
{
	return alloc_obj (vtable, size, FALSE, has_references);
}

/*
 * We're not freeing the block if it's empty.  We leave that work for
 * the next major collection.
 *
 * This is just called from the domain clearing code, which runs in a
 * single thread and has the GC lock, so we don't need an extra lock.
 */
static void
free_object (char *obj, size_t size, gboolean pinned)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
	int word, bit;

	if (!block->swept)
		sweep_block (block, FALSE);
	SGEN_ASSERT (9, (pinned && block->pinned) || (!pinned && !block->pinned), "free-object pinning mixup object %p pinned %d block %p pinned %d", obj, pinned, block, block->pinned);
	SGEN_ASSERT (9, MS_OBJ_ALLOCED (obj, block), "object %p is already free", obj);
	MS_CALC_MARK_BIT (word, bit, obj);
	SGEN_ASSERT (9, !MS_MARK_BIT (block, word, bit), "object %p has mark bit set");
	if (!block->free_list) {
		MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, block->has_references);
		int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
		SGEN_ASSERT (9, !block->next_free, "block %p doesn't have a free-list of object but belongs to a free-list of blocks");
		block->next_free = free_blocks [size_index];
		free_blocks [size_index] = block;
	}
	memset (obj, 0, size);
	*(void**)obj = block->free_list;
	block->free_list = (void**)obj;
}

static void
major_free_non_pinned_object (char *obj, size_t size)
{
	free_object (obj, size, FALSE);
}

/* size is a multiple of SGEN_ALLOC_ALIGN */
static void*
major_alloc_small_pinned_obj (MonoVTable *vtable, size_t size, gboolean has_references)
{
	void *res;

	res = alloc_obj (vtable, size, TRUE, has_references);
	 /*If we failed to alloc memory, we better try releasing memory
	  *as pinned alloc is requested by the runtime.
	  */
	 if (!res) {
		sgen_perform_collection (0, GENERATION_OLD, "pinned alloc failure", TRUE);
		res = alloc_obj (vtable, size, TRUE, has_references);
	 }
	 return res;
}

static void
free_pinned_object (char *obj, size_t size)
{
	free_object (obj, size, TRUE);
}

/*
 * size is already rounded up and we hold the GC lock.
 */
static void*
major_alloc_degraded (MonoVTable *vtable, size_t size)
{
	void *obj;
	int old_num_sections;

	old_num_sections = num_major_sections;

	obj = alloc_obj (vtable, size, FALSE, SGEN_VTABLE_HAS_REFERENCES (vtable));
	if (G_LIKELY (obj)) {
		HEAVY_STAT (++stat_objects_alloced_degraded);
		HEAVY_STAT (stat_bytes_alloced_degraded += size);
		g_assert (num_major_sections >= old_num_sections);
		sgen_register_major_sections_alloced (num_major_sections - old_num_sections);
	}
	return obj;
}

#define MAJOR_OBJ_IS_IN_TO_SPACE(obj)	FALSE

/*
 * obj is some object.  If it's not in the major heap (i.e. if it's in
 * the nursery or LOS), return FALSE.  Otherwise return whether it's
 * been marked or copied.
 */
static gboolean
major_is_object_live (char *obj)
{
	MSBlockInfo *block;
	int word, bit;
#ifndef FIXED_HEAP
	mword objsize;
#endif

	if (sgen_ptr_in_nursery (obj))
		return FALSE;

#ifdef FIXED_HEAP
	/* LOS */
	if (!MS_PTR_IN_SMALL_MAJOR_HEAP (obj))
		return FALSE;
#else
	objsize = SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)obj));

	/* LOS */
	if (objsize > SGEN_MAX_SMALL_OBJ_SIZE)
		return FALSE;
#endif

	/* now we know it's in a major block */
	block = MS_BLOCK_FOR_OBJ (obj);
	SGEN_ASSERT (9, !block->pinned, "block %p is pinned, BTW why is this bad?");
	MS_CALC_MARK_BIT (word, bit, obj);
	return MS_MARK_BIT (block, word, bit) ? TRUE : FALSE;
}

static gboolean
major_ptr_is_in_non_pinned_space (char *ptr, char **start)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		if (ptr >= block->block && ptr <= block->block + MS_BLOCK_SIZE) {
			int count = MS_BLOCK_FREE / block->obj_size;
			int i;

			*start = NULL;
			for (i = 0; i <= count; ++i) {
				if (ptr >= MS_BLOCK_OBJ (block, i) && ptr < MS_BLOCK_OBJ (block, i + 1)) {
					*start = MS_BLOCK_OBJ (block, i);
					break;
				}
			}
			return !block->pinned;
		}
	} END_FOREACH_BLOCK;
	return FALSE;
}

static void
major_iterate_objects (gboolean non_pinned, gboolean pinned, IterateObjectCallbackFunc callback, void *data)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;
		int i;

		if (block->pinned && !pinned)
			continue;
		if (!block->pinned && !non_pinned)
			continue;
		if (lazy_sweep)
			sweep_block (block, FALSE);

		for (i = 0; i < count; ++i) {
			void **obj = (void**) MS_BLOCK_OBJ (block, i);
			if (MS_OBJ_ALLOCED (obj, block))
				callback ((char*)obj, block->obj_size, data);
		}
	} END_FOREACH_BLOCK;
}

static gboolean
major_is_valid_object (char *object)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		int idx;
		char *obj;

		if ((block->block > object) || ((block->block + MS_BLOCK_SIZE) <= object))
			continue;

		idx = MS_BLOCK_OBJ_INDEX (object, block);
		obj = (char*)MS_BLOCK_OBJ (block, idx);
		if (obj != object)
			return FALSE;
		return MS_OBJ_ALLOCED (obj, block);
	} END_FOREACH_BLOCK;

	return FALSE;
}


static gboolean
major_describe_pointer (char *ptr)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		int idx;
		char *obj;
		gboolean live;
		MonoVTable *vtable;
		int w, b;
		gboolean marked;

		if ((block->block > ptr) || ((block->block + MS_BLOCK_SIZE) <= ptr))
			continue;

		SGEN_LOG (0, "major-ptr (block %p sz %d pin %d ref %d)\n",
			block->block, block->obj_size, block->pinned, block->has_references);

		idx = MS_BLOCK_OBJ_INDEX (ptr, block);
		obj = (char*)MS_BLOCK_OBJ (block, idx);
		live = MS_OBJ_ALLOCED (obj, block);
		vtable = live ? (MonoVTable*)SGEN_LOAD_VTABLE (obj) : NULL;

		MS_CALC_MARK_BIT (w, b, obj);
		marked = MS_MARK_BIT (block, w, b);

		if (obj == ptr) {
			SGEN_LOG (0, "\t(");
			if (live)
				SGEN_LOG (0, "object");
			else
				SGEN_LOG (0, "dead-object");
		} else {
			if (live)
				SGEN_LOG (0, "interior-ptr offset %td", ptr - obj);
			else
				SGEN_LOG (0, "dead-interior-ptr offset %td", ptr - obj);
		}

		SGEN_LOG (0, " marked %d)\n", marked ? 1 : 0);

		return TRUE;
	} END_FOREACH_BLOCK;

	return FALSE;
}

static void
major_check_scan_starts (void)
{
}

static void
major_dump_heap (FILE *heap_dump_file)
{
	MSBlockInfo *block;
	int *slots_available = alloca (sizeof (int) * num_block_obj_sizes);
	int *slots_used = alloca (sizeof (int) * num_block_obj_sizes);
	int i;

	for (i = 0; i < num_block_obj_sizes; ++i)
		slots_available [i] = slots_used [i] = 0;

	FOREACH_BLOCK (block) {
		int index = ms_find_block_obj_size_index (block->obj_size);
		int count = MS_BLOCK_FREE / block->obj_size;

		slots_available [index] += count;
		for (i = 0; i < count; ++i) {
			if (MS_OBJ_ALLOCED (MS_BLOCK_OBJ (block, i), block))
				++slots_used [index];
		}
	} END_FOREACH_BLOCK;

	fprintf (heap_dump_file, "<occupancies>\n");
	for (i = 0; i < num_block_obj_sizes; ++i) {
		fprintf (heap_dump_file, "<occupancy size=\"%d\" available=\"%d\" used=\"%d\" />\n",
				block_obj_sizes [i], slots_available [i], slots_used [i]);
	}
	fprintf (heap_dump_file, "</occupancies>\n");

	FOREACH_BLOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;
		int i;
		int start = -1;

		fprintf (heap_dump_file, "<section type=\"%s\" size=\"%zu\">\n", "old", (size_t)MS_BLOCK_FREE);

		for (i = 0; i <= count; ++i) {
			if ((i < count) && MS_OBJ_ALLOCED (MS_BLOCK_OBJ (block, i), block)) {
				if (start < 0)
					start = i;
			} else {
				if (start >= 0) {
					sgen_dump_occupied (MS_BLOCK_OBJ (block, start), MS_BLOCK_OBJ (block, i), block->block);
					start = -1;
				}
			}
		}

		fprintf (heap_dump_file, "</section>\n");
	} END_FOREACH_BLOCK;
}

#define LOAD_VTABLE	SGEN_LOAD_VTABLE

#define MS_MARK_OBJECT_AND_ENQUEUE_CHECKED(obj,block,queue) do {	\
		int __word, __bit;					\
		MS_CALC_MARK_BIT (__word, __bit, (obj));		\
		if (!MS_MARK_BIT ((block), __word, __bit) && MS_OBJ_ALLOCED ((obj), (block))) { \
			MS_SET_MARK_BIT ((block), __word, __bit);	\
			if ((block)->has_references)			\
				GRAY_OBJECT_ENQUEUE ((queue), (obj));	\
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), sgen_safe_object_get_size ((MonoObject*)(obj))); \
			INC_NUM_MAJOR_OBJECTS_MARKED ();		\
		}							\
	} while (0)
#define MS_MARK_OBJECT_AND_ENQUEUE(obj,block,queue) do {		\
		int __word, __bit;					\
		MS_CALC_MARK_BIT (__word, __bit, (obj));		\
		SGEN_ASSERT (9, MS_OBJ_ALLOCED ((obj), (block)), "object %p not allocated", obj);	\
		if (!MS_MARK_BIT ((block), __word, __bit)) {		\
			MS_SET_MARK_BIT ((block), __word, __bit);	\
			if ((block)->has_references)			\
				GRAY_OBJECT_ENQUEUE ((queue), (obj));	\
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), sgen_safe_object_get_size ((MonoObject*)(obj))); \
			INC_NUM_MAJOR_OBJECTS_MARKED ();		\
		}							\
	} while (0)
#define MS_PAR_MARK_OBJECT_AND_ENQUEUE(obj,block,queue) do {		\
		int __word, __bit;					\
		gboolean __was_marked;					\
		SGEN_ASSERT (9, MS_OBJ_ALLOCED ((obj), (block)), "object %p not allocated", obj);	\
		MS_CALC_MARK_BIT (__word, __bit, (obj));		\
		MS_PAR_SET_MARK_BIT (__was_marked, (block), __word, __bit); \
		if (!__was_marked) {					\
			if ((block)->has_references)			\
				GRAY_OBJECT_ENQUEUE ((queue), (obj));	\
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), sgen_safe_object_get_size ((MonoObject*)(obj))); \
			INC_NUM_MAJOR_OBJECTS_MARKED ();		\
		}							\
	} while (0)

static void
pin_major_object (char *obj, SgenGrayQueue *queue)
{
	MSBlockInfo *block;

#ifdef SGEN_HAVE_CONCURRENT_MARK
	if (concurrent_mark)
		g_assert_not_reached ();
#endif

	block = MS_BLOCK_FOR_OBJ (obj);
	block->has_pinned = TRUE;
	MS_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
}

#include "sgen-major-copy-object.h"

#ifdef SGEN_PARALLEL_MARK
static void
major_copy_or_mark_object (void **ptr, void *obj, SgenGrayQueue *queue)
{
	mword objsize;
	MSBlockInfo *block;
	MonoVTable *vt;

	HEAVY_STAT (++stat_copy_object_called_major);

	SGEN_ASSERT (9, obj, "null object from pointer %p", ptr);
	SGEN_ASSERT (9, current_collection_generation == GENERATION_OLD, "old gen parallel allocator called from a %d collection", current_collection_generation);

	if (sgen_ptr_in_nursery (obj)) {
		int word, bit;
		gboolean has_references;
		void *destination;
		mword vtable_word = *(mword*)obj;
		vt = (MonoVTable*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);

		if (vtable_word & SGEN_FORWARDED_BIT) {
			*ptr = (void*)vt;
			return;
		}

		if (vtable_word & SGEN_PINNED_BIT)
			return;

		/* An object in the nursery To Space has already been copied and grayed. Nothing to do. */
		if (sgen_nursery_is_to_space (obj))
			return;

		HEAVY_STAT (++stat_objects_copied_major);

	do_copy_object:
		objsize = SGEN_ALIGN_UP (sgen_par_object_get_size (vt, (MonoObject*)obj));
		has_references = SGEN_VTABLE_HAS_REFERENCES (vt);

		destination = sgen_minor_collector.par_alloc_for_promotion (vt, obj, objsize, has_references);
		if (G_UNLIKELY (!destination)) {
			if (!sgen_ptr_in_nursery (obj)) {
				int size_index;
				block = MS_BLOCK_FOR_OBJ (obj);
				size_index = block->obj_size_index;
				evacuate_block_obj_sizes [size_index] = FALSE;
			}

			sgen_parallel_pin_or_update (ptr, obj, vt, queue);
			sgen_set_pinned_from_failed_allocation (objsize);
			return;
		}

		if (SGEN_CAS_PTR (obj, (void*)((mword)destination | SGEN_FORWARDED_BIT), vt) == vt) {
			gboolean was_marked;

			par_copy_object_no_checks (destination, vt, obj, objsize, has_references ? queue : NULL);
			obj = destination;
			*ptr = obj;

			/*
			 * FIXME: If we make major_alloc_object() give
			 * us the block info, too, we won't have to
			 * re-fetch it here.
			 *
			 * FIXME (2): We should rework this to avoid all those nursery checks.
			 */
			/*
			 * For the split nursery allocator the object
			 * might still be in the nursery despite
			 * having being promoted, in which case we
			 * can't mark it.
			 */
			if (!sgen_ptr_in_nursery (obj)) {
				block = MS_BLOCK_FOR_OBJ (obj);
				MS_CALC_MARK_BIT (word, bit, obj);
				SGEN_ASSERT (9, !MS_MARK_BIT (block, word, bit), "object %p already marked", obj);
				MS_PAR_SET_MARK_BIT (was_marked, block, word, bit);
			}
		} else {
			/*
			 * FIXME: We have allocated destination, but
			 * we cannot use it.  Give it back to the
			 * allocator.
			 */
			*(void**)destination = NULL;

			vtable_word = *(mword*)obj;
			g_assert (vtable_word & SGEN_FORWARDED_BIT);

			obj = (void*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);

			*ptr = obj;

			HEAVY_STAT (++stat_slots_allocated_in_vain);
		}
	} else {
#ifdef FIXED_HEAP
		if (MS_PTR_IN_SMALL_MAJOR_HEAP (obj))
#else
		mword vtable_word = *(mword*)obj;
		vt = (MonoVTable*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);

		/* see comment in the non-parallel version below */
		if (vtable_word & SGEN_FORWARDED_BIT) {
			*ptr = (void*)vt;
			return;
		}
		objsize = SGEN_ALIGN_UP (sgen_par_object_get_size (vt, (MonoObject*)obj));

		if (objsize <= SGEN_MAX_SMALL_OBJ_SIZE)
#endif
		{
			int size_index;

			block = MS_BLOCK_FOR_OBJ (obj);
			size_index = block->obj_size_index;

			if (!block->has_pinned && evacuate_block_obj_sizes [size_index]) {
				if (block->is_to_space)
					return;

#ifdef FIXED_HEAP
				{
					mword vtable_word = *(mword*)obj;
					vt = (MonoVTable*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);

					if (vtable_word & SGEN_FORWARDED_BIT) {
						*ptr = (void*)vt;
						return;
					}
				}
#endif

				HEAVY_STAT (++stat_major_objects_evacuated);
				goto do_copy_object;
			}

			MS_PAR_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
		} else {
			LOSObject *bigobj = sgen_los_header_for_object (obj);
			mword size_word = bigobj->size;
#ifdef FIXED_HEAP
			mword vtable_word = *(mword*)obj;
			vt = (MonoVTable*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);
#endif
			if (size_word & 1)
				return;
			binary_protocol_pin (obj, vt, sgen_safe_object_get_size ((MonoObject*)obj));
			if (SGEN_CAS_PTR ((void*)&bigobj->size, (void*)(size_word | 1), (void*)size_word) == (void*)size_word) {
				if (SGEN_VTABLE_HAS_REFERENCES (vt))
					GRAY_OBJECT_ENQUEUE (queue, obj);
			} else {
				g_assert (sgen_los_object_is_pinned (obj));
			}
		}
	}
}
#else
#ifdef SGEN_HAVE_CONCURRENT_MARK
static void
major_copy_or_mark_object_concurrent (void **ptr, void *obj, SgenGrayQueue *queue)
{
	g_assert (!SGEN_OBJECT_IS_FORWARDED (obj));

	if (!sgen_ptr_in_nursery (obj)) {
#ifdef FIXED_HEAP
		if (MS_PTR_IN_SMALL_MAJOR_HEAP (obj))
#else
		mword objsize;

		objsize = SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)obj));

		if (objsize <= SGEN_MAX_SMALL_OBJ_SIZE)
#endif
		{
			MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
			MS_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
		} else {
			if (sgen_los_object_is_pinned (obj))
				return;

#ifdef ENABLE_DTRACE
			if (G_UNLIKELY (MONO_GC_OBJ_PINNED_ENABLED ())) {
				MonoVTable *vt = (MonoVTable*)SGEN_LOAD_VTABLE (obj);
				MONO_GC_OBJ_PINNED ((mword)obj, sgen_safe_object_get_size (obj), vt->klass->name_space, vt->klass->name, GENERATION_OLD);
			}
#endif

			sgen_los_pin_object (obj);
			/* FIXME: only enqueue if object has references */
			GRAY_OBJECT_ENQUEUE (queue, obj);
			INC_NUM_MAJOR_OBJECTS_MARKED ();
		}
	}
}
#endif

static void
major_copy_or_mark_object (void **ptr, void *obj, SgenGrayQueue *queue)
{
	MSBlockInfo *block;

	HEAVY_STAT (++stat_copy_object_called_major);

	SGEN_ASSERT (9, obj, "null object from pointer %p", ptr);
	SGEN_ASSERT (9, current_collection_generation == GENERATION_OLD, "old gen parallel allocator called from a %d collection", current_collection_generation);

	if (sgen_ptr_in_nursery (obj)) {
		int word, bit;
		char *forwarded, *old_obj;

		if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
			*ptr = forwarded;
			return;
		}
		if (SGEN_OBJECT_IS_PINNED (obj))
			return;

		/* An object in the nursery To Space has already been copied and grayed. Nothing to do. */
		if (sgen_nursery_is_to_space (obj))
			return;

		HEAVY_STAT (++stat_objects_copied_major);

	do_copy_object:
		old_obj = obj;
		obj = copy_object_no_checks (obj, queue);
		if (G_UNLIKELY (old_obj == obj)) {
			/*If we fail to evacuate an object we just stop doing it for a given block size as all other will surely fail too.*/
			if (!sgen_ptr_in_nursery (obj)) {
				int size_index;
				block = MS_BLOCK_FOR_OBJ (obj);
				size_index = block->obj_size_index;
				evacuate_block_obj_sizes [size_index] = FALSE;
				MS_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
			}
			return;
		}
		*ptr = obj;

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
		if (!sgen_ptr_in_nursery (obj)) {
			block = MS_BLOCK_FOR_OBJ (obj);
			MS_CALC_MARK_BIT (word, bit, obj);
			SGEN_ASSERT (9, !MS_MARK_BIT (block, word, bit), "object %p already marked", obj);
			MS_SET_MARK_BIT (block, word, bit);
			binary_protocol_mark (obj, (gpointer)LOAD_VTABLE (obj), sgen_safe_object_get_size ((MonoObject*)obj));
		}
	} else {
		char *forwarded;
#ifdef FIXED_HEAP
		if (MS_PTR_IN_SMALL_MAJOR_HEAP (obj))
#else
		mword objsize;

		/*
		 * If we have don't have a fixed heap we cannot know
		 * whether an object is in the LOS or in the small
		 * object major heap without checking its size.  To do
		 * that, however, we need to know that we actually
		 * have a valid object, not a forwarding pointer, so
		 * we have to do this check first.
		 */
		if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
			*ptr = forwarded;
			return;
		}

		objsize = SGEN_ALIGN_UP (sgen_safe_object_get_size ((MonoObject*)obj));

		if (objsize <= SGEN_MAX_SMALL_OBJ_SIZE)
#endif
		{
			int size_index;
			gboolean evacuate;

			block = MS_BLOCK_FOR_OBJ (obj);
			size_index = block->obj_size_index;
			evacuate = evacuate_block_obj_sizes [size_index];

#ifdef FIXED_HEAP
			/*
			 * We could also check for !block->has_pinned
			 * here, but it would only make an uncommon case
			 * faster, namely objects that are in blocks
			 * whose slot sizes are evacuated but which have
			 * pinned objects.
			 */
			if (evacuate && (forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
				*ptr = forwarded;
				return;
			}
#endif

			if (evacuate && !block->has_pinned) {
				g_assert (!SGEN_OBJECT_IS_PINNED (obj));
				if (block->is_to_space)
					return;
				HEAVY_STAT (++stat_major_objects_evacuated);
				goto do_copy_object;
			} else {
				MS_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
			}
		} else {
			if (sgen_los_object_is_pinned (obj))
				return;
			binary_protocol_pin (obj, (gpointer)SGEN_LOAD_VTABLE (obj), sgen_safe_object_get_size ((MonoObject*)obj));

#ifdef ENABLE_DTRACE
			if (G_UNLIKELY (MONO_GC_OBJ_PINNED_ENABLED ())) {
				MonoVTable *vt = (MonoVTable*)SGEN_LOAD_VTABLE (obj);
				MONO_GC_OBJ_PINNED ((mword)obj, sgen_safe_object_get_size (obj), vt->klass->name_space, vt->klass->name, GENERATION_OLD);
			}
#endif

			sgen_los_pin_object (obj);
			/* FIXME: only enqueue if object has references */
			GRAY_OBJECT_ENQUEUE (queue, obj);
		}
	}
}
#endif

static void
major_copy_or_mark_object_canonical (void **ptr, SgenGrayQueue *queue)
{
	major_copy_or_mark_object (ptr, *ptr, queue);
}

#ifdef SGEN_HAVE_CONCURRENT_MARK
static void
major_copy_or_mark_object_concurrent_canonical (void **ptr, SgenGrayQueue *queue)
{
	major_copy_or_mark_object_concurrent (ptr, *ptr, queue);
}

static long long
major_get_and_reset_num_major_objects_marked (void)
{
#ifdef SGEN_COUNT_NUMBER_OF_MAJOR_OBJECTS_MARKED
	long long num = num_major_objects_marked;
	num_major_objects_marked = 0;
	return num;
#else
	return 0;
#endif
}
#endif

#include "sgen-major-scan-object.h"

#ifdef SGEN_HAVE_CONCURRENT_MARK
#define SCAN_FOR_CONCURRENT_MARK
#include "sgen-major-scan-object.h"
#undef SCAN_FOR_CONCURRENT_MARK
#endif

static void
mark_pinned_objects_in_block (MSBlockInfo *block, SgenGrayQueue *queue)
{
	int i;
	int last_index = -1;

	if (!block->pin_queue_num_entries)
		return;

	block->has_pinned = TRUE;

	for (i = 0; i < block->pin_queue_num_entries; ++i) {
		int index = MS_BLOCK_OBJ_INDEX (block->pin_queue_start [i], block);
		SGEN_ASSERT (9, index >= 0 && index < MS_BLOCK_FREE / block->obj_size, "invalid object %p index %d max-index %d", block->pin_queue_start [i], index, MS_BLOCK_FREE / block->obj_size);
		if (index == last_index)
			continue;
		MS_MARK_OBJECT_AND_ENQUEUE_CHECKED (MS_BLOCK_OBJ (block, index), block, queue);
		last_index = index;
	}
}

static inline void
sweep_block_for_size (MSBlockInfo *block, int count, int obj_size)
{
	int obj_index;

	for (obj_index = 0; obj_index < count; ++obj_index) {
		int word, bit;
		void *obj = MS_BLOCK_OBJ_FOR_SIZE (block, obj_index, obj_size);

		MS_CALC_MARK_BIT (word, bit, obj);
		if (MS_MARK_BIT (block, word, bit)) {
			SGEN_ASSERT (9, MS_OBJ_ALLOCED (obj, block), "object %p not allocated", obj);
		} else {
			/* an unmarked object */
			if (MS_OBJ_ALLOCED (obj, block)) {
				/*
				 * FIXME: Merge consecutive
				 * slots for lower reporting
				 * overhead.  Maybe memset
				 * will also benefit?
				 */
				binary_protocol_empty (obj, obj_size);
				MONO_GC_MAJOR_SWEPT ((mword)obj, obj_size);
				memset (obj, 0, obj_size);
			}
			*(void**)obj = block->free_list;
			block->free_list = obj;
		}
	}
}

/*
 * sweep_block:
 *
 *   Traverse BLOCK, freeing and zeroing unused objects.
 */
static void
sweep_block (MSBlockInfo *block, gboolean during_major_collection)
{
	int count;

	if (!during_major_collection)
		g_assert (!sgen_concurrent_collection_in_progress ());

	if (block->swept)
		return;

	count = MS_BLOCK_FREE / block->obj_size;

	block->free_list = NULL;

	/* Use inline instances specialized to constant sizes, this allows the compiler to replace the memset calls with inline code */
	// FIXME: Add more sizes
	switch (block->obj_size) {
	case 16:
		sweep_block_for_size (block, count, 16);
		break;
	default:
		sweep_block_for_size (block, count, block->obj_size);
		break;
	}

	/* reset mark bits */
	memset (block->mark_words, 0, sizeof (mword) * MS_NUM_MARK_WORDS);

	/*
	 * FIXME: reverse free list so that it's in address
	 * order
	 */

	block->swept = 1;
}

static inline int
bitcount (mword d)
{
#if SIZEOF_VOID_P == 8
	/* http://www.jjj.de/bitwizardry/bitwizardrypage.html */
	d -=  (d>>1) & 0x5555555555555555;
	d  = ((d>>2) & 0x3333333333333333) + (d & 0x3333333333333333);
	d  = ((d>>4) + d) & 0x0f0f0f0f0f0f0f0f;
	d *= 0x0101010101010101;
	return d >> 56;
#else
	/* http://aggregate.org/MAGIC/ */
	d -= ((d >> 1) & 0x55555555);
	d = (((d >> 2) & 0x33333333) + (d & 0x33333333));
	d = (((d >> 4) + d) & 0x0f0f0f0f);
	d += (d >> 8);
	d += (d >> 16);
	return (d & 0x0000003f);
#endif
}

static void
ms_sweep (void)
{
	int i;
	MSBlockInfo **iter;

	/* statistics for evacuation */
	int *slots_available = alloca (sizeof (int) * num_block_obj_sizes);
	int *slots_used = alloca (sizeof (int) * num_block_obj_sizes);
	int *num_blocks = alloca (sizeof (int) * num_block_obj_sizes);

#ifdef SGEN_HAVE_CONCURRENT_MARK
	mword total_evacuate_heap = 0;
	mword total_evacuate_saved = 0;
#endif

	for (i = 0; i < num_block_obj_sizes; ++i)
		slots_available [i] = slots_used [i] = num_blocks [i] = 0;

	/* clear all the free lists */
	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i) {
		MSBlockInfo **free_blocks = free_block_lists [i];
		int j;
		for (j = 0; j < num_block_obj_sizes; ++j)
			free_blocks [j] = NULL;
	}

	/* traverse all blocks, free and zero unmarked objects */
	iter = &all_blocks;
	while (*iter) {
		MSBlockInfo *block = *iter;
		int count;
		gboolean have_live = FALSE;
		gboolean has_pinned;
		gboolean have_free = FALSE;
		int obj_size_index;
		int nused = 0;

		obj_size_index = block->obj_size_index;

		has_pinned = block->has_pinned;
		block->has_pinned = block->pinned;

		block->is_to_space = FALSE;
		block->swept = 0;

		count = MS_BLOCK_FREE / block->obj_size;

#ifdef SGEN_HAVE_CONCURRENT_MARK
		if (block->cardtable_mod_union) {
			sgen_free_internal_dynamic (block->cardtable_mod_union, CARDS_PER_BLOCK, INTERNAL_MEM_CARDTABLE_MOD_UNION);
			block->cardtable_mod_union = NULL;
		}
#endif

		/* Count marked objects in the block */
		for (i = 0; i < MS_NUM_MARK_WORDS; ++i) {
			nused += bitcount (block->mark_words [i]);
		}
		if (nused) {
			have_live = TRUE;
		}
		if (nused < count)
			have_free = TRUE;

		if (!lazy_sweep)
			sweep_block (block, TRUE);

		if (have_live) {
			if (!has_pinned) {
				++num_blocks [obj_size_index];
				slots_used [obj_size_index] += nused;
				slots_available [obj_size_index] += count;
			}

			iter = &block->next;

			/*
			 * If there are free slots in the block, add
			 * the block to the corresponding free list.
			 */
			if (have_free) {
				MSBlockInfo **free_blocks = FREE_BLOCKS (block->pinned, block->has_references);
				int index = MS_BLOCK_OBJ_SIZE_INDEX (block->obj_size);
				block->next_free = free_blocks [index];
				free_blocks [index] = block;
			}

			update_heap_boundaries_for_block (block);
		} else {
			/*
			 * Blocks without live objects are removed from the
			 * block list and freed.
			 */
			*iter = block->next;

#ifdef FIXED_HEAP
			ms_free_block (block);
#else
			ms_free_block (block->block);

			sgen_free_internal (block, INTERNAL_MEM_MS_BLOCK_INFO);
#endif

			--num_major_sections;
		}
	}

	for (i = 0; i < num_block_obj_sizes; ++i) {
		float usage = (float)slots_used [i] / (float)slots_available [i];
		if (num_blocks [i] > 5 && usage < evacuation_threshold) {
			evacuate_block_obj_sizes [i] = TRUE;
			/*
			g_print ("slot size %d - %d of %d used\n",
					block_obj_sizes [i], slots_used [i], slots_available [i]);
			*/
		} else {
			evacuate_block_obj_sizes [i] = FALSE;
		}
#ifdef SGEN_HAVE_CONCURRENT_MARK
		{
			mword total_bytes = block_obj_sizes [i] * slots_available [i];
			total_evacuate_heap += total_bytes;
			if (evacuate_block_obj_sizes [i])
				total_evacuate_saved += total_bytes - block_obj_sizes [i] * slots_used [i];
		}
#endif
	}

#ifdef SGEN_HAVE_CONCURRENT_MARK
	want_evacuation = (float)total_evacuate_saved / (float)total_evacuate_heap > (1 - concurrent_evacuation_threshold);
#endif

	have_swept = TRUE;
}

static void
major_sweep (void)
{
	ms_sweep ();
}

static int count_pinned_ref;
static int count_pinned_nonref;
static int count_nonpinned_ref;
static int count_nonpinned_nonref;

static void
count_nonpinned_callback (char *obj, size_t size, void *data)
{
	MonoVTable *vtable = (MonoVTable*)LOAD_VTABLE (obj);

	if (vtable->klass->has_references)
		++count_nonpinned_ref;
	else
		++count_nonpinned_nonref;
}

static void
count_pinned_callback (char *obj, size_t size, void *data)
{
	MonoVTable *vtable = (MonoVTable*)LOAD_VTABLE (obj);

	if (vtable->klass->has_references)
		++count_pinned_ref;
	else
		++count_pinned_nonref;
}

static G_GNUC_UNUSED void
count_ref_nonref_objs (void)
{
	int total;

	count_pinned_ref = 0;
	count_pinned_nonref = 0;
	count_nonpinned_ref = 0;
	count_nonpinned_nonref = 0;

	major_iterate_objects (TRUE, FALSE, count_nonpinned_callback, NULL);
	major_iterate_objects (FALSE, TRUE, count_pinned_callback, NULL);

	total = count_pinned_nonref + count_nonpinned_nonref + count_pinned_ref + count_nonpinned_ref;

	g_print ("ref: %d pinned %d non-pinned   non-ref: %d pinned %d non-pinned  --  %.1f\n",
			count_pinned_ref, count_nonpinned_ref,
			count_pinned_nonref, count_nonpinned_nonref,
			(count_pinned_nonref + count_nonpinned_nonref) * 100.0 / total);
}

static int
ms_calculate_block_obj_sizes (double factor, int *arr)
{
	double target_size = sizeof (MonoObject);
	int num_sizes = 0;
	int last_size = 0;

	do {
		int target_count = ceil (MS_BLOCK_FREE / target_size);
		int size = MIN ((MS_BLOCK_FREE / target_count) & ~(SGEN_ALLOC_ALIGN - 1), SGEN_MAX_SMALL_OBJ_SIZE);

		if (size != last_size) {
			if (arr)
				arr [num_sizes] = size;
			++num_sizes;
			last_size = size;
		}

		target_size *= factor;
	} while (last_size < SGEN_MAX_SMALL_OBJ_SIZE);

	return num_sizes;
}

/* only valid during minor collections */
static int old_num_major_sections;

static void
major_start_nursery_collection (void)
{
#ifdef MARKSWEEP_CONSISTENCY_CHECK
	consistency_check ();
#endif

	old_num_major_sections = num_major_sections;
}

static void
major_finish_nursery_collection (void)
{
#ifdef MARKSWEEP_CONSISTENCY_CHECK
	consistency_check ();
#endif
	sgen_register_major_sections_alloced (num_major_sections - old_num_major_sections);
}

static void
major_start_major_collection (void)
{
	int i;

	/* clear the free lists */
	for (i = 0; i < num_block_obj_sizes; ++i) {
		if (!evacuate_block_obj_sizes [i])
			continue;

		free_block_lists [0][i] = NULL;
		free_block_lists [MS_BLOCK_FLAG_REFS][i] = NULL;
	}

	// Sweep all unswept blocks
	if (lazy_sweep) {
		MSBlockInfo **iter;

		MONO_GC_SWEEP_BEGIN (GENERATION_OLD, TRUE);

		iter = &all_blocks;
		while (*iter) {
			MSBlockInfo *block = *iter;

			sweep_block (block, TRUE);

			iter = &block->next;
		}

		MONO_GC_SWEEP_END (GENERATION_OLD, TRUE);
	}
}

static void
major_finish_major_collection (void)
{
}

#if !defined(FIXED_HEAP) && SIZEOF_VOID_P != 8
static int
compare_pointers (const void *va, const void *vb) {
	char *a = *(char**)va, *b = *(char**)vb;
	if (a < b)
		return -1;
	if (a > b)
		return 1;
	return 0;
}
#endif

static void
major_have_computer_minor_collection_allowance (void)
{
#ifndef FIXED_HEAP
	int section_reserve = sgen_get_minor_collection_allowance () / MS_BLOCK_SIZE;

	g_assert (have_swept);

#if SIZEOF_VOID_P != 8
	{
		int i, num_empty_blocks_orig, num_blocks, arr_length;
		void *block;
		void **empty_block_arr;
		void **rebuild_next;

#ifdef TARGET_WIN32
		/*
		 * sgen_free_os_memory () asserts in mono_vfree () because windows doesn't like freeing the middle of
		 * a VirtualAlloc ()-ed block.
		 */
		return;
#endif

		if (num_empty_blocks <= section_reserve)
			return;
		SGEN_ASSERT (0, num_empty_blocks > 0, "section reserve can't be negative");

		num_empty_blocks_orig = num_empty_blocks;
		empty_block_arr = (void**)sgen_alloc_internal_dynamic (sizeof (void*) * num_empty_blocks_orig,
				INTERNAL_MEM_MS_BLOCK_INFO_SORT, FALSE);
		if (!empty_block_arr)
			goto fallback;

		i = 0;
		for (block = empty_blocks; block; block = *(void**)block)
			empty_block_arr [i++] = block;
		SGEN_ASSERT (0, i == num_empty_blocks, "empty block count wrong");

		qsort (empty_block_arr, num_empty_blocks, sizeof (void*), compare_pointers);

		/*
		 * We iterate over the free blocks, trying to find MS_BLOCK_ALLOC_NUM
		 * contiguous ones.  If we do, we free them.  If that's not enough to get to
		 * section_reserve, we halve the number of contiguous blocks we're looking
		 * for and have another go, until we're done with looking for pairs of
		 * blocks, at which point we give up and go to the fallback.
		 */
		arr_length = num_empty_blocks_orig;
		num_blocks = MS_BLOCK_ALLOC_NUM;
		while (num_empty_blocks > section_reserve && num_blocks > 1) {
			int first = -1;
			int dest = 0;

			dest = 0;
			for (i = 0; i < arr_length; ++i) {
				int d = dest;
				void *block = empty_block_arr [i];
				SGEN_ASSERT (0, block, "we're not shifting correctly");
				if (i != dest) {
					empty_block_arr [dest] = block;
					/*
					 * This is not strictly necessary, but we're
					 * cautious.
					 */
					empty_block_arr [i] = NULL;
				}
				++dest;

				if (first < 0) {
					first = d;
					continue;
				}

				SGEN_ASSERT (0, first >= 0 && d > first, "algorithm is wrong");

				if ((char*)block != ((char*)empty_block_arr [d-1]) + MS_BLOCK_SIZE) {
					first = d;
					continue;
				}

				if (d + 1 - first == num_blocks) {
					/*
					 * We found num_blocks contiguous blocks.  Free them
					 * and null their array entries.  As an optimization
					 * we could, instead of nulling the entries, shift
					 * the following entries over to the left, while
					 * we're iterating.
					 */
					int j;
					sgen_free_os_memory (empty_block_arr [first], MS_BLOCK_SIZE * num_blocks, SGEN_ALLOC_HEAP);
					for (j = first; j <= d; ++j)
						empty_block_arr [j] = NULL;
					dest = first;
					first = -1;

					num_empty_blocks -= num_blocks;

					stat_major_blocks_freed += num_blocks;
					if (num_blocks == MS_BLOCK_ALLOC_NUM)
						stat_major_blocks_freed_ideal += num_blocks;
					else
						stat_major_blocks_freed_less_ideal += num_blocks;

				}
			}

			SGEN_ASSERT (0, dest <= i && dest <= arr_length, "array length is off");
			arr_length = dest;
			SGEN_ASSERT (0, arr_length == num_empty_blocks, "array length is off");

			num_blocks >>= 1;
		}

		/* rebuild empty_blocks free list */
		rebuild_next = (void**)&empty_blocks;
		for (i = 0; i < arr_length; ++i) {
			void *block = empty_block_arr [i];
			SGEN_ASSERT (0, block, "we're missing blocks");
			*rebuild_next = block;
			rebuild_next = (void**)block;
		}
		*rebuild_next = NULL;

		/* free array */
		sgen_free_internal_dynamic (empty_block_arr, sizeof (void*) * num_empty_blocks_orig, INTERNAL_MEM_MS_BLOCK_INFO_SORT);
	}

	SGEN_ASSERT (0, num_empty_blocks >= 0, "we freed more blocks than we had in the first place?");

 fallback:
	/*
	 * This is our threshold.  If there's not more empty than used blocks, we won't
	 * release uncontiguous blocks, in fear of fragmenting the address space.
	 */
	if (num_empty_blocks <= num_major_sections)
		return;
#endif

	while (num_empty_blocks > section_reserve) {
		void *next = *(void**)empty_blocks;
		sgen_free_os_memory (empty_blocks, MS_BLOCK_SIZE, SGEN_ALLOC_HEAP);
		empty_blocks = next;
		/*
		 * Needs not be atomic because this is running
		 * single-threaded.
		 */
		--num_empty_blocks;

		++stat_major_blocks_freed;
#if SIZEOF_VOID_P != 8
		++stat_major_blocks_freed_individual;
#endif
	}
#endif
}

static void
major_find_pin_queue_start_ends (SgenGrayQueue *queue)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		block->pin_queue_start = sgen_find_optimized_pin_queue_area (block->block + MS_BLOCK_SKIP, block->block + MS_BLOCK_SIZE,
				&block->pin_queue_num_entries);
	} END_FOREACH_BLOCK;
}

static void
major_pin_objects (SgenGrayQueue *queue)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		mark_pinned_objects_in_block (block, queue);
	} END_FOREACH_BLOCK;
}

static void
major_init_to_space (void)
{
}

static void
major_report_pinned_memory_usage (void)
{
	g_assert_not_reached ();
}

static gint64
major_get_used_size (void)
{
	gint64 size = 0;
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;
		void **iter;
		size += count * block->obj_size;
		for (iter = block->free_list; iter; iter = (void**)*iter)
			size -= block->obj_size;
	} END_FOREACH_BLOCK;

	return size;
}

static int
get_num_major_sections (void)
{
	return num_major_sections;
}

static gboolean
major_handle_gc_param (const char *opt)
{
#ifdef FIXED_HEAP
	if (g_str_has_prefix (opt, "major-heap-size=")) {
		const char *arg = strchr (opt, '=') + 1;
		glong size;
		if (!mono_gc_parse_environment_string_extract_number (arg, &size))
			return FALSE;
		ms_heap_num_blocks = (size + MS_BLOCK_SIZE - 1) / MS_BLOCK_SIZE;
		g_assert (ms_heap_num_blocks > 0);
		return TRUE;
	} else
#endif
	if (g_str_has_prefix (opt, "evacuation-threshold=")) {
		const char *arg = strchr (opt, '=') + 1;
		int percentage = atoi (arg);
		if (percentage < 0 || percentage > 100) {
			fprintf (stderr, "evacuation-threshold must be an integer in the range 0-100.\n");
			exit (1);
		}
		evacuation_threshold = (float)percentage / 100.0;
		return TRUE;
	} else if (!strcmp (opt, "lazy-sweep")) {
		lazy_sweep = TRUE;
		return TRUE;
	} else if (!strcmp (opt, "no-lazy-sweep")) {
		lazy_sweep = FALSE;
		return TRUE;
	}

	return FALSE;
}

static void
major_print_gc_param_usage (void)
{
	fprintf (stderr,
			""
#ifdef FIXED_HEAP
			"  major-heap-size=N (where N is an integer, possibly with a k, m or a g suffix)\n"
#endif
			"  evacuation-threshold=P (where P is a percentage, an integer in 0-100)\n"
			"  (no-)lazy-sweep\n"
			);
}

static void
major_iterate_live_block_ranges (sgen_cardtable_block_callback callback)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		if (block->has_references)
			callback ((mword)block->block, MS_BLOCK_SIZE);
	} END_FOREACH_BLOCK;
}

#ifdef HEAVY_STATISTICS
extern long long marked_cards;
extern long long scanned_cards;
extern long long scanned_objects;
extern long long remarked_cards;
#endif

#define CARD_WORDS_PER_BLOCK (CARDS_PER_BLOCK / SIZEOF_VOID_P)
/*
 * MS blocks are 16K aligned.
 * Cardtables are 4K aligned, at least.
 * This means that the cardtable of a given block is 32 bytes aligned.
 */
static guint8*
initial_skip_card (guint8 *card_data)
{
	mword *cards = (mword*)card_data;
	mword card;
	int i;
	for (i = 0; i < CARD_WORDS_PER_BLOCK; ++i) {
		card = cards [i];
		if (card)
			break;
	}

	if (i == CARD_WORDS_PER_BLOCK)
		return card_data + CARDS_PER_BLOCK;

#if defined(__i386__) && defined(__GNUC__)
	return card_data + i * 4 +  (__builtin_ffs (card) - 1) / 8;
#elif defined(__x86_64__) && defined(__GNUC__)
	return card_data + i * 8 +  (__builtin_ffsll (card) - 1) / 8;
#elif defined(__s390x__) && defined(__GNUC__)
	return card_data + i * 8 +  (__builtin_ffsll (GUINT64_TO_LE(card)) - 1) / 8;
#else
	for (i = i * SIZEOF_VOID_P; i < CARDS_PER_BLOCK; ++i) {
		if (card_data [i])
			return &card_data [i];
	}
	return card_data;
#endif
}


static G_GNUC_UNUSED guint8*
skip_card (guint8 *card_data, guint8 *card_data_end)
{
	while (card_data < card_data_end && !*card_data)
		++card_data;
	return card_data;
}

#define MS_BLOCK_OBJ_INDEX_FAST(o,b,os)	(((char*)(o) - ((b) + MS_BLOCK_SKIP)) / (os))
#define MS_BLOCK_OBJ_FAST(b,os,i)			((b) + MS_BLOCK_SKIP + (os) * (i))
#define MS_OBJ_ALLOCED_FAST(o,b)		(*(void**)(o) && (*(char**)(o) < (b) || *(char**)(o) >= (b) + MS_BLOCK_SIZE))

static void
major_scan_card_table (gboolean mod_union, SgenGrayQueue *queue)
{
	MSBlockInfo *block;
	ScanObjectFunc scan_func = sgen_get_current_object_ops ()->scan_object;

#ifdef SGEN_HAVE_CONCURRENT_MARK
	if (!concurrent_mark)
		g_assert (!mod_union);
#else
	g_assert (!mod_union);
#endif

	FOREACH_BLOCK (block) {
		int block_obj_size;
		char *block_start;

		if (!block->has_references)
			continue;

		block_obj_size = block->obj_size;
		block_start = block->block;

		if (block_obj_size >= CARD_SIZE_IN_BYTES) {
			guint8 *cards;
#ifndef SGEN_HAVE_OVERLAPPING_CARDS
			guint8 cards_data [CARDS_PER_BLOCK];
#endif
			char *obj, *end, *base;

			if (mod_union) {
#ifdef SGEN_HAVE_CONCURRENT_MARK
				cards = block->cardtable_mod_union;
				/*
				 * This happens when the nursery
				 * collection that precedes finishing
				 * the concurrent collection allocates
				 * new major blocks.
				 */
				if (!cards)
					continue;
#endif
			} else {
			/*We can avoid the extra copy since the remark cardtable was cleaned before */
#ifdef SGEN_HAVE_OVERLAPPING_CARDS
				cards = sgen_card_table_get_card_scan_address ((mword)block_start);
#else
				cards = cards_data;
				if (!sgen_card_table_get_card_data (cards_data, (mword)block_start, CARDS_PER_BLOCK))
					continue;
#endif
			}

			obj = (char*)MS_BLOCK_OBJ_FAST (block_start, block_obj_size, 0);
			end = block_start + MS_BLOCK_SIZE;
			base = sgen_card_table_align_pointer (obj);

			while (obj < end) {
				int card_offset;

				if (!block->swept)
					sweep_block (block, FALSE);

				if (!MS_OBJ_ALLOCED_FAST (obj, block_start))
					goto next_large;

				if (mod_union) {
					/* FIXME: do this more efficiently */
					int w, b;
					MS_CALC_MARK_BIT (w, b, obj);
					if (!MS_MARK_BIT (block, w, b))
						goto next_large;
				}

				card_offset = (obj - base) >> CARD_BITS;
				sgen_cardtable_scan_object (obj, block_obj_size, cards + card_offset, mod_union, queue);

			next_large:
				obj += block_obj_size;
			}
		} else {
			guint8 *card_data, *card_base;
			guint8 *card_data_end;

			/*
			 * This is safe in face of card aliasing for the following reason:
			 *
			 * Major blocks are 16k aligned, or 32 cards aligned.
			 * Cards aliasing happens in powers of two, so as long as major blocks are aligned to their
			 * sizes, they won't overflow the cardtable overlap modulus.
			 */
			if (mod_union) {
#ifdef SGEN_HAVE_CONCURRENT_MARK
				card_data = card_base = block->cardtable_mod_union;
				/*
				 * This happens when the nursery
				 * collection that precedes finishing
				 * the concurrent collection allocates
				 * new major blocks.
				 */
				if (!card_data)
					continue;
#else
				g_assert_not_reached ();
				card_data = NULL;
#endif
			} else {
				card_data = card_base = sgen_card_table_get_card_scan_address ((mword)block_start);
			}
			card_data_end = card_data + CARDS_PER_BLOCK;

			for (card_data = initial_skip_card (card_data); card_data < card_data_end; ++card_data) { //card_data = skip_card (card_data + 1, card_data_end)) {
				int index;
				int idx = card_data - card_base;
				char *start = (char*)(block_start + idx * CARD_SIZE_IN_BYTES);
				char *end = start + CARD_SIZE_IN_BYTES;
				char *first_obj, *obj;

				HEAVY_STAT (++scanned_cards);

				if (!*card_data)
					continue;

				if (!block->swept)
					sweep_block (block, FALSE);

				HEAVY_STAT (++marked_cards);

				sgen_card_table_prepare_card_for_scanning (card_data);

				if (idx == 0)
					index = 0;
				else
					index = MS_BLOCK_OBJ_INDEX_FAST (start, block_start, block_obj_size);

				obj = first_obj = (char*)MS_BLOCK_OBJ_FAST (block_start, block_obj_size, index);
				while (obj < end) {
					if (!MS_OBJ_ALLOCED_FAST (obj, block_start))
						goto next_small;

					if (mod_union) {
						/* FIXME: do this more efficiently */
						int w, b;
						MS_CALC_MARK_BIT (w, b, obj);
						if (!MS_MARK_BIT (block, w, b))
							goto next_small;
					}

					HEAVY_STAT (++scanned_objects);
					scan_func (obj, queue);
				next_small:
					obj += block_obj_size;
				}
				HEAVY_STAT (if (*card_data) ++remarked_cards);
				binary_protocol_card_scan (first_obj, obj - first_obj);
			}
		}
	} END_FOREACH_BLOCK;
}

#ifdef SGEN_HAVE_CONCURRENT_MARK
static void
update_cardtable_mod_union (void)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		size_t num_cards;

		block->cardtable_mod_union = sgen_card_table_update_mod_union (block->cardtable_mod_union,
				block->block, MS_BLOCK_SIZE, &num_cards);

		SGEN_ASSERT (0, num_cards == CARDS_PER_BLOCK, "Number of cards calculation is wrong");
	} END_FOREACH_BLOCK;
}

static guint8*
major_get_cardtable_mod_union_for_object (char *obj)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
	return &block->cardtable_mod_union [(obj - (char*)sgen_card_table_align_pointer (block->block)) >> CARD_BITS];
}
#endif

static void
alloc_free_block_lists (MSBlockInfo ***lists)
{
	int i;
	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i)
		lists [i] = sgen_alloc_internal_dynamic (sizeof (MSBlockInfo*) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);
}

#ifdef SGEN_PARALLEL_MARK
static void*
major_alloc_worker_data (void)
{
	/* FIXME: free this when the workers come down */
	MSBlockInfo ***lists = malloc (sizeof (MSBlockInfo**) * MS_BLOCK_TYPE_MAX);
	alloc_free_block_lists (lists);
	return lists;
}

static void
major_init_worker_thread (void *data)
{
	MSBlockInfo ***lists = data;
	int i;

	g_assert (lists && lists != free_block_lists);
	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i) {
		int j;
		for (j = 0; j < num_block_obj_sizes; ++j)
			g_assert (!lists [i][j]);
	}

#ifdef HAVE_KW_THREAD
	workers_free_block_lists = data;
#else
	mono_native_tls_set_value (workers_free_block_lists_key, data);
#endif
}

static void
major_reset_worker_data (void *data)
{
	MSBlockInfo ***lists = data;
	int i;
	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i) {
		int j;
		for (j = 0; j < num_block_obj_sizes; ++j)
			lists [i][j] = NULL;
	}
}
#endif

#undef pthread_create

static void
post_param_init (SgenMajorCollector *collector)
{
	collector->sweeps_lazily = lazy_sweep;
}

#ifdef SGEN_HAVE_CONCURRENT_MARK
static void
sgen_marksweep_init_internal (SgenMajorCollector *collector, gboolean is_concurrent)
#else // SGEN_HAVE_CONCURRENT_MARK
#ifdef SGEN_PARALLEL_MARK
#ifdef FIXED_HEAP
void
sgen_marksweep_fixed_par_init (SgenMajorCollector *collector)
#else // FIXED_HEAP
void
sgen_marksweep_par_init (SgenMajorCollector *collector)
#endif // FIXED_HEAP
#else // SGEN_PARALLEL_MARK
#ifdef FIXED_HEAP
void
sgen_marksweep_fixed_init (SgenMajorCollector *collector)
#else // FIXED_HEAP
#error unknown configuration
#endif // FIXED_HEAP
#endif // SGEN_PARALLEL_MARK
#endif // SGEN_HAVE_CONCURRENT_MARK
{
	int i;

#ifndef FIXED_HEAP
	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_MS_BLOCK_INFO, sizeof (MSBlockInfo));
#endif

	num_block_obj_sizes = ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, NULL);
	block_obj_sizes = sgen_alloc_internal_dynamic (sizeof (int) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);
	ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, block_obj_sizes);

	evacuate_block_obj_sizes = sgen_alloc_internal_dynamic (sizeof (gboolean) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);
	for (i = 0; i < num_block_obj_sizes; ++i)
		evacuate_block_obj_sizes [i] = FALSE;

	/*
	{
		int i;
		g_print ("block object sizes:\n");
		for (i = 0; i < num_block_obj_sizes; ++i)
			g_print ("%d\n", block_obj_sizes [i]);
	}
	*/

	alloc_free_block_lists (free_block_lists);

	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES; ++i)
		fast_block_obj_size_indexes [i] = ms_find_block_obj_size_index (i * 8);
	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES * 8; ++i)
		g_assert (MS_BLOCK_OBJ_SIZE_INDEX (i) == ms_find_block_obj_size_index (i));

#ifdef SGEN_PARALLEL_MARK
	LOCK_INIT (ms_block_list_mutex);
#endif

	mono_counters_register ("# major blocks allocated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_alloced);
	mono_counters_register ("# major blocks freed", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_freed);
	mono_counters_register ("# major blocks lazy swept", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_lazy_swept);
	mono_counters_register ("# major objects evacuated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_objects_evacuated);
#if SIZEOF_VOID_P != 8
	mono_counters_register ("# major blocks freed ideally", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_freed_ideal);
	mono_counters_register ("# major blocks freed less ideally", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_freed_less_ideal);
	mono_counters_register ("# major blocks freed individually", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_freed_individual);
	mono_counters_register ("# major blocks allocated less ideally", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_alloced_less_ideal);
#endif

#ifdef SGEN_PARALLEL_MARK
#ifndef HAVE_KW_THREAD
	mono_native_tls_alloc (&workers_free_block_lists_key, NULL);
#endif
#endif

	collector->section_size = MAJOR_SECTION_SIZE;
#ifdef SGEN_PARALLEL_MARK
	collector->is_parallel = TRUE;
	collector->alloc_worker_data = major_alloc_worker_data;
	collector->init_worker_thread = major_init_worker_thread;
	collector->reset_worker_data = major_reset_worker_data;
#else
	collector->is_parallel = FALSE;
#endif
#ifdef SGEN_HAVE_CONCURRENT_MARK
	concurrent_mark = is_concurrent;
	if (is_concurrent) {
		collector->is_concurrent = TRUE;
		collector->want_synchronous_collection = &want_evacuation;
		collector->get_and_reset_num_major_objects_marked = major_get_and_reset_num_major_objects_marked;
	} else
#endif
	{
		collector->is_concurrent = FALSE;
		collector->want_synchronous_collection = NULL;
	}
	collector->supports_cardtable = TRUE;

	collector->have_swept = &have_swept;

	collector->alloc_heap = major_alloc_heap;
	collector->is_object_live = major_is_object_live;
	collector->alloc_small_pinned_obj = major_alloc_small_pinned_obj;
	collector->alloc_degraded = major_alloc_degraded;

	collector->alloc_object = major_alloc_object;
#ifdef SGEN_PARALLEL_MARK
	collector->par_alloc_object = major_par_alloc_object;
#endif
	collector->free_pinned_object = free_pinned_object;
	collector->iterate_objects = major_iterate_objects;
	collector->free_non_pinned_object = major_free_non_pinned_object;
	collector->find_pin_queue_start_ends = major_find_pin_queue_start_ends;
	collector->pin_objects = major_pin_objects;
	collector->pin_major_object = pin_major_object;
	collector->scan_card_table = major_scan_card_table;
	collector->iterate_live_block_ranges = (void*)(void*) major_iterate_live_block_ranges;
#ifdef SGEN_HAVE_CONCURRENT_MARK
	if (is_concurrent) {
		collector->update_cardtable_mod_union = update_cardtable_mod_union;
		collector->get_cardtable_mod_union_for_object = major_get_cardtable_mod_union_for_object;
	}
#endif
	collector->init_to_space = major_init_to_space;
	collector->sweep = major_sweep;
	collector->check_scan_starts = major_check_scan_starts;
	collector->dump_heap = major_dump_heap;
	collector->get_used_size = major_get_used_size;
	collector->start_nursery_collection = major_start_nursery_collection;
	collector->finish_nursery_collection = major_finish_nursery_collection;
	collector->start_major_collection = major_start_major_collection;
	collector->finish_major_collection = major_finish_major_collection;
	collector->have_computed_minor_collection_allowance = major_have_computer_minor_collection_allowance;
	collector->ptr_is_in_non_pinned_space = major_ptr_is_in_non_pinned_space;
	collector->obj_is_from_pinned_alloc = obj_is_from_pinned_alloc;
	collector->report_pinned_memory_usage = major_report_pinned_memory_usage;
	collector->get_num_major_sections = get_num_major_sections;
	collector->handle_gc_param = major_handle_gc_param;
	collector->print_gc_param_usage = major_print_gc_param_usage;
	collector->post_param_init = post_param_init;
	collector->is_valid_object = major_is_valid_object;
	collector->describe_pointer = major_describe_pointer;

	collector->major_ops.copy_or_mark_object = major_copy_or_mark_object_canonical;
	collector->major_ops.scan_object = major_scan_object;
#ifdef SGEN_HAVE_CONCURRENT_MARK
	if (is_concurrent) {
		collector->major_concurrent_ops.copy_or_mark_object = major_copy_or_mark_object_concurrent_canonical;
		collector->major_concurrent_ops.scan_object = major_scan_object_concurrent;
		collector->major_concurrent_ops.scan_vtype = major_scan_vtype_concurrent;
	}
#endif

	/*cardtable requires major pages to be 8 cards aligned*/
	g_assert ((MS_BLOCK_SIZE % (8 * CARD_SIZE_IN_BYTES)) == 0);
}

#ifdef SGEN_HAVE_CONCURRENT_MARK
void
sgen_marksweep_init (SgenMajorCollector *collector)
{
	sgen_marksweep_init_internal (collector, FALSE);
}

void
sgen_marksweep_conc_init (SgenMajorCollector *collector)
{
	sgen_marksweep_init_internal (collector, TRUE);
}
#endif

#endif
