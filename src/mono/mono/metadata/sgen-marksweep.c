/*
 * sgen-marksweep.c: Simple generational GC.
 *
 * Author:
 * 	Mark Probst <mark.probst@gmail.com>
 *
 * Copyright 2009-2010 Novell, Inc.
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

#ifdef HAVE_SGEN_GC

#include <math.h>

#include "utils/mono-counters.h"
#include "metadata/object-internals.h"
#include "metadata/profiler-private.h"

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"

#define DEBUG(l,x)

#define MS_BLOCK_SIZE	(16*1024)
#define MS_BLOCK_SIZE_SHIFT	14
#define MAJOR_SECTION_SIZE	MS_BLOCK_SIZE
#define CARDS_PER_BLOCK (MS_BLOCK_SIZE / CARD_SIZE_IN_BYTES)

#ifdef FIXED_HEAP
#define MS_DEFAULT_HEAP_NUM_BLOCKS	(32 * 1024) /* 512 MB */
#endif

/*
 * Don't allocate single blocks, but alloc a contingent of this many
 * blocks in one swoop.
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
	gboolean pinned;
	gboolean has_references;
#ifdef FIXED_HEAP
	gboolean used;
#else
	MSBlockInfo *next;
#endif
	char *block;
	void **free_list;
	MSBlockInfo *next_free;
	void **pin_queue_start;
	int pin_queue_num_entries;
	mword mark_words [MS_NUM_MARK_WORDS];
};

#ifdef FIXED_HEAP
static int ms_heap_num_blocks = MS_DEFAULT_HEAP_NUM_BLOCKS;

#define ms_heap_start	nursery_end
static char *ms_heap_end;

#define MS_PTR_IN_SMALL_MAJOR_HEAP(p)	((char*)(p) >= ms_heap_start && (char*)(p) < ms_heap_end)

/* array of all all block infos in the system */
static MSBlockInfo *block_infos;
#endif

#define MS_BLOCK_OBJ(b,i)		((b)->block + MS_BLOCK_SKIP + (b)->obj_size * (i))
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
#define LOCK_MS_BLOCK_LIST pthread_mutex_lock (&ms_block_list_mutex)
#define UNLOCK_MS_BLOCK_LIST pthread_mutex_unlock (&ms_block_list_mutex)
#else
#define LOCK_MS_BLOCK_LIST
#define UNLOCK_MS_BLOCK_LIST
#endif

/* we get this at init */
static int nursery_bits;
static char *nursery_start;
static char *nursery_end;

#define ptr_in_nursery(p)	(SGEN_PTR_IN_NURSERY ((p), nursery_bits, nursery_start, nursery_end))

#ifdef FIXED_HEAP
/* non-allocated block free-list */
static MSBlockInfo *empty_blocks = NULL;
#else
/* non-allocated block free-list */
static void *empty_blocks = NULL;
/* all allocated blocks in the system */
static MSBlockInfo *all_blocks;
static int num_empty_blocks = 0;
#endif

#ifdef FIXED_HEAP
#define FOREACH_BLOCK(bl)	{					\
		int __block_i;						\
		for (__block_i = 0; __block_i < ms_heap_num_blocks; ++__block_i) { \
			(bl) = &block_infos [__block_i];		\
			if (!(bl)->used) continue;
#define END_FOREACH_BLOCK	}}
#else
#define FOREACH_BLOCK(bl)	for ((bl) = all_blocks; (bl); (bl) = (bl)->next) {
#define END_FOREACH_BLOCK	}
#endif

static int num_major_sections = 0;
/* one free block list for each block object size */
static MSBlockInfo **free_block_lists [MS_BLOCK_TYPE_MAX];

static long long stat_major_blocks_alloced = 0;
static long long stat_major_blocks_freed = 0;

static int
ms_find_block_obj_size_index (int size)
{
	int i;
	DEBUG (9, g_assert (size <= SGEN_MAX_SMALL_OBJ_SIZE));
	for (i = 0; i < num_block_obj_sizes; ++i)
		if (block_obj_sizes [i] >= size)
			return i;
	g_assert_not_reached ();
}

#define FREE_BLOCKS(p,r) (free_block_lists [((p) ? MS_BLOCK_FLAG_PINNED : 0) | ((r) ? MS_BLOCK_FLAG_REFS : 0)])

#define MS_BLOCK_OBJ_SIZE_INDEX(s)				\
	(((s)+7)>>3 < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES ?	\
	 fast_block_obj_size_indexes [((s)+7)>>3] :		\
	 ms_find_block_obj_size_index ((s)))

#ifdef FIXED_HEAP
static void*
major_alloc_heap (mword nursery_size, mword nursery_align, int the_nursery_bits)
{
	char *heap_start;
	mword major_heap_size = ms_heap_num_blocks * MS_BLOCK_SIZE;
	mword alloc_size = nursery_size + major_heap_size;
	int i;

	g_assert (ms_heap_num_blocks > 0);
	g_assert (nursery_size % MS_BLOCK_SIZE == 0);
	if (nursery_align)
		g_assert (nursery_align % MS_BLOCK_SIZE == 0);

	nursery_start = mono_sgen_alloc_os_memory_aligned (alloc_size, nursery_align ? nursery_align : MS_BLOCK_SIZE, TRUE);
	nursery_end = heap_start = nursery_start + nursery_size;
	nursery_bits = the_nursery_bits;

	ms_heap_end = heap_start + major_heap_size;

	block_infos = mono_sgen_alloc_internal_dynamic (sizeof (MSBlockInfo) * ms_heap_num_blocks, INTERNAL_MEM_MS_BLOCK_INFO);

	for (i = 0; i < ms_heap_num_blocks; ++i) {
		block_infos [i].block = heap_start + i * MS_BLOCK_SIZE;
		if (i < ms_heap_num_blocks - 1)
			block_infos [i].next_free = &block_infos [i + 1];
		else
			block_infos [i].next_free = NULL;
	}

	empty_blocks = &block_infos [0];

	return nursery_start;
}
#else
static void*
major_alloc_heap (mword nursery_size, mword nursery_align, int the_nursery_bits)
{
	if (nursery_align)
		nursery_start = mono_sgen_alloc_os_memory_aligned (nursery_size, nursery_align, TRUE);
	else
		nursery_start = mono_sgen_alloc_os_memory (nursery_size, TRUE);

	nursery_end = nursery_start + nursery_size;
	nursery_bits = the_nursery_bits;

	return nursery_start;
}
#endif

#ifdef FIXED_HEAP
static MSBlockInfo*
ms_get_empty_block (void)
{
	MSBlockInfo *block;

	g_assert (empty_blocks);

	block = empty_blocks;
	empty_blocks = empty_blocks->next_free;

	block->used = TRUE;

	mono_sgen_update_heap_boundaries ((mword)block->block, (mword)block->block + MS_BLOCK_SIZE);

	return block;
}

static void
ms_free_block (MSBlockInfo *block)
{
	block->next_free = empty_blocks;
	empty_blocks = block;
	block->used = FALSE;
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
		p = mono_sgen_alloc_os_memory_aligned (MS_BLOCK_SIZE * MS_BLOCK_ALLOC_NUM, MS_BLOCK_SIZE, TRUE);

		for (i = 0; i < MS_BLOCK_ALLOC_NUM; ++i) {
			block = p;
			/*
			 * We do the free list update one after the
			 * other so that other threads can use the new
			 * blocks as quickly as possible.
			 */
			do {
				empty = empty_blocks;
				*(void**)block = empty;
			} while (SGEN_CAS_PTR (&empty_blocks, block, empty) != empty);
			p += MS_BLOCK_SIZE;
		}

		SGEN_ATOMIC_ADD (num_empty_blocks, MS_BLOCK_ALLOC_NUM);

		stat_major_blocks_alloced += MS_BLOCK_ALLOC_NUM;
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

	mono_sgen_update_heap_boundaries ((mword)block, (mword)block + MS_BLOCK_SIZE);

	return block;
}

static void
ms_free_block (void *block)
{
	void *empty;

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
		g_assert (block->free_list);

#ifdef FIXED_HEAP
		/* the block must not be in the empty_blocks list */
		for (b = empty_blocks; b; b = b->next_free)
			g_assert (b != block);
#else
		/* the block must be in the all_blocks list */
		for (b = all_blocks; b; b = b->next) {
			if (b == block)
				break;
		}
		g_assert (b == block);
#endif
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
		for (i = 0; i < MS_NUM_MARK_WORDS; ++i)
			g_assert (block->mark_words [i] == 0);
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

static void
ms_alloc_block (int size_index, gboolean pinned, gboolean has_references)
{
	int size = block_obj_sizes [size_index];
	int count = MS_BLOCK_FREE / size;
#ifdef FIXED_HEAP
	MSBlockInfo *info = ms_get_empty_block ();
#else
	MSBlockInfo *info = mono_sgen_alloc_internal (INTERNAL_MEM_MS_BLOCK_INFO);
	MSBlockHeader *header;
#endif
	MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, has_references);
	char *obj_start;
	int i;

	DEBUG (9, g_assert (count >= 2));

	info->obj_size = size;
	info->pinned = pinned;
	info->has_references = has_references;
#ifndef FIXED_HEAP
	info->block = ms_get_empty_block ();

	header = (MSBlockHeader*) info->block;
	header->info = info;
#endif

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

	info->next_free = free_blocks [size_index];
	free_blocks [size_index] = info;

#ifndef FIXED_HEAP
	info->next = all_blocks;
	all_blocks = info;
#endif

	++num_major_sections;
}

static gboolean
obj_is_from_pinned_alloc (char *obj)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
	return block->pinned;
}

static void*
alloc_obj (int size, gboolean pinned, gboolean has_references)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, has_references);
	MSBlockInfo *block;
	void *obj;

	/* FIXME: try to do this without locking */

	LOCK_MS_BLOCK_LIST;

	if (!free_blocks [size_index])
		ms_alloc_block (size_index, pinned, has_references);

	block = free_blocks [size_index];
	DEBUG (9, g_assert (block));

	obj = block->free_list;
	DEBUG (9, g_assert (obj));

	block->free_list = *(void**)obj;
	if (!block->free_list) {
		free_blocks [size_index] = block->next_free;
		block->next_free = NULL;
	}

	UNLOCK_MS_BLOCK_LIST;

	/*
	 * FIXME: This should not be necessary because it'll be
	 * overwritten by the vtable immediately.
	 */
	*(void**)obj = NULL;

	return obj;
}

static void*
major_alloc_object (int size, gboolean has_references)
{
	return alloc_obj (size, FALSE, has_references);
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
	DEBUG (9, g_assert ((pinned && block->pinned) || (!pinned && !block->pinned)));
	DEBUG (9, g_assert (MS_OBJ_ALLOCED (obj, block)));
	MS_CALC_MARK_BIT (word, bit, obj);
	DEBUG (9, g_assert (!MS_MARK_BIT (block, word, bit)));
	if (!block->free_list) {
		MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, block->has_references);
		int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
		DEBUG (9, g_assert (!block->next_free));
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
major_alloc_small_pinned_obj (size_t size, gboolean has_references)
{
	return alloc_obj (size, TRUE, has_references);
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
	int old_num_sections = num_major_sections;
	obj = alloc_obj (size, FALSE, vtable->klass->has_references);
	*(MonoVTable**)obj = vtable;
	HEAVY_STAT (++stat_objects_alloced_degraded);
	HEAVY_STAT (stat_bytes_alloced_degraded += size);
	g_assert (num_major_sections >= old_num_sections);
	mono_sgen_register_major_sections_alloced (num_major_sections - old_num_sections);
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

	if (ptr_in_nursery (obj))
		return FALSE;

#ifdef FIXED_HEAP
	/* LOS */
	if (!MS_PTR_IN_SMALL_MAJOR_HEAP (obj))
		return FALSE;
#else
	objsize = SGEN_ALIGN_UP (mono_sgen_safe_object_get_size ((MonoObject*)obj));

	/* LOS */
	if (objsize > SGEN_MAX_SMALL_OBJ_SIZE)
		return FALSE;
#endif

	/* now we know it's in a major block */
	block = MS_BLOCK_FOR_OBJ (obj);
	DEBUG (9, g_assert (!block->pinned));
	MS_CALC_MARK_BIT (word, bit, obj);
	return MS_MARK_BIT (block, word, bit) ? TRUE : FALSE;
}

static gboolean
major_ptr_is_in_non_pinned_space (char *ptr)
{
	g_assert_not_reached ();
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

		for (i = 0; i < count; ++i) {
			void **obj = (void**) MS_BLOCK_OBJ (block, i);
			if (MS_OBJ_ALLOCED (obj, block))
				callback ((char*)obj, block->obj_size, data);
		}
	} END_FOREACH_BLOCK;
}

static void
major_check_scan_starts (void)
{
}

static void
major_dump_heap (FILE *heap_dump_file)
{
	MSBlockInfo *block;

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
					mono_sgen_dump_occupied (MS_BLOCK_OBJ (block, start), MS_BLOCK_OBJ (block, i), block->block);
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
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), mono_sgen_safe_object_get_size ((MonoObject*)(obj))); \
		}							\
	} while (0)
#define MS_MARK_OBJECT_AND_ENQUEUE(obj,block,queue) do {		\
		int __word, __bit;					\
		MS_CALC_MARK_BIT (__word, __bit, (obj));		\
		DEBUG (9, g_assert (MS_OBJ_ALLOCED ((obj), (block))));	\
		if (!MS_MARK_BIT ((block), __word, __bit)) {		\
			MS_SET_MARK_BIT ((block), __word, __bit);	\
			if ((block)->has_references)			\
				GRAY_OBJECT_ENQUEUE ((queue), (obj));	\
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), mono_sgen_safe_object_get_size ((MonoObject*)(obj))); \
		}							\
	} while (0)
#define MS_PAR_MARK_OBJECT_AND_ENQUEUE(obj,block,queue) do {		\
		int __word, __bit;					\
		gboolean __was_marked;					\
		DEBUG (9, g_assert (MS_OBJ_ALLOCED ((obj), (block))));	\
		MS_CALC_MARK_BIT (__word, __bit, (obj));		\
		MS_PAR_SET_MARK_BIT (__was_marked, (block), __word, __bit); \
		if (!__was_marked) {					\
			if ((block)->has_references)			\
				GRAY_OBJECT_ENQUEUE ((queue), (obj));	\
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), mono_sgen_safe_object_get_size ((MonoObject*)(obj))); \
		}							\
	} while (0)

#include "sgen-major-copy-object.h"

#ifdef SGEN_PARALLEL_MARK
static void
major_copy_or_mark_object (void **ptr, SgenGrayQueue *queue)
{
	void *obj = *ptr;
	mword vtable_word = *(mword*)obj;
	MonoVTable *vt = (MonoVTable*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);
	mword objsize;
	MSBlockInfo *block;

	HEAVY_STAT (++stat_copy_object_called_major);

	DEBUG (9, g_assert (obj));
	DEBUG (9, g_assert (current_collection_generation == GENERATION_OLD));

	if (ptr_in_nursery (obj)) {
		int word, bit;
		gboolean has_references;
		void *destination;

		if (vtable_word & SGEN_FORWARDED_BIT) {
			*ptr = (void*)vt;
			return;
		}

		if (vtable_word & SGEN_PINNED_BIT)
			return;

		HEAVY_STAT (++stat_objects_copied_major);

		objsize = SGEN_ALIGN_UP (mono_sgen_par_object_get_size (vt, (MonoObject*)obj));
		has_references = SGEN_VTABLE_HAS_REFERENCES (vt);

		destination = major_alloc_object (objsize, has_references);

		if (SGEN_CAS_PTR (obj, (void*)((mword)destination | SGEN_FORWARDED_BIT), vt) == vt) {
			gboolean was_marked;

			par_copy_object_no_checks (destination, vt, obj, objsize, has_references ? queue : NULL);
			obj = destination;
			*ptr = obj;

			/*
			 * FIXME: If we make major_alloc_object() give
			 * us the block info, too, we won't have to
			 * re-fetch it here.
			 */
			block = MS_BLOCK_FOR_OBJ (obj);
			MS_CALC_MARK_BIT (word, bit, obj);
			DEBUG (9, g_assert (!MS_MARK_BIT (block, word, bit)));
			MS_PAR_SET_MARK_BIT (was_marked, block, word, bit);
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
		}
	} else {
#ifdef FIXED_HEAP
		if (MS_PTR_IN_SMALL_MAJOR_HEAP (obj))
#else
		objsize = SGEN_ALIGN_UP (mono_sgen_par_object_get_size (vt, (MonoObject*)obj));

		if (objsize <= SGEN_MAX_SMALL_OBJ_SIZE)
#endif
		{
			block = MS_BLOCK_FOR_OBJ (obj);
			MS_PAR_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
		} else {
			if (vtable_word & SGEN_PINNED_BIT)
				return;
			binary_protocol_pin (obj, vt, mono_sgen_safe_object_get_size ((MonoObject*)obj));
			if (SGEN_CAS_PTR (obj, (void*)(vtable_word | SGEN_PINNED_BIT), (void*)vtable_word) == (void*)vtable_word) {
				if (SGEN_VTABLE_HAS_REFERENCES (vt))
					GRAY_OBJECT_ENQUEUE (queue, obj);
			} else {
				g_assert (SGEN_OBJECT_IS_PINNED (obj));
			}
		}
	}
}
#else
static void
major_copy_or_mark_object (void **ptr, SgenGrayQueue *queue)
{
	void *obj = *ptr;
	mword objsize;
	MSBlockInfo *block;

	HEAVY_STAT (++stat_copy_object_called_major);

	DEBUG (9, g_assert (obj));
	DEBUG (9, g_assert (current_collection_generation == GENERATION_OLD));

	if (ptr_in_nursery (obj)) {
		int word, bit;
		char *forwarded;

		if ((forwarded = SGEN_OBJECT_IS_FORWARDED (obj))) {
			*ptr = forwarded;
			return;
		}
		if (SGEN_OBJECT_IS_PINNED (obj))
			return;

		HEAVY_STAT (++stat_objects_copied_major);

		obj = copy_object_no_checks (obj, queue);
		*ptr = obj;

		/*
		 * FIXME: See comment for copy_object_no_checks().  If
		 * we have that, we can let the allocation function
		 * give us the block info, too, and we won't have to
		 * re-fetch it.
		 */
		block = MS_BLOCK_FOR_OBJ (obj);
		MS_CALC_MARK_BIT (word, bit, obj);
		DEBUG (9, g_assert (!MS_MARK_BIT (block, word, bit)));
		MS_SET_MARK_BIT (block, word, bit);
	} else {
#ifdef FIXED_HEAP
		if (MS_PTR_IN_SMALL_MAJOR_HEAP (obj))
#else
		objsize = SGEN_ALIGN_UP (mono_sgen_safe_object_get_size ((MonoObject*)obj));

		if (objsize <= SGEN_MAX_SMALL_OBJ_SIZE)
#endif
		{
			block = MS_BLOCK_FOR_OBJ (obj);
			MS_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
		} else {
			if (SGEN_OBJECT_IS_PINNED (obj))
				return;
			binary_protocol_pin (obj, (gpointer)SGEN_LOAD_VTABLE (obj), mono_sgen_safe_object_get_size ((MonoObject*)obj));
			SGEN_PIN_OBJECT (obj);
			/* FIXME: only enqueue if object has references */
			GRAY_OBJECT_ENQUEUE (queue, obj);
		}
	}
}
#endif

#include "sgen-major-scan-object.h"

static void
mark_pinned_objects_in_block (MSBlockInfo *block, SgenGrayQueue *queue)
{
	int i;
	int last_index = -1;
	int count = MS_BLOCK_FREE / block->obj_size;

	for (i = 0; i < block->pin_queue_num_entries; ++i) {
		int index = MS_BLOCK_OBJ_INDEX (block->pin_queue_start [i], block);
		DEBUG (9, g_assert (index >= 0 && index < count));
		if (index == last_index)
			continue;
		MS_MARK_OBJECT_AND_ENQUEUE_CHECKED (MS_BLOCK_OBJ (block, index), block, queue);
		last_index = index;
	}
}

static void
major_sweep (void)
{
	int i;
#ifdef FIXED_HEAP
	int j;
#else
	MSBlockInfo **iter;
#endif

	/* clear all the free lists */
	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i) {
		MSBlockInfo **free_blocks = free_block_lists [i];
		int j;
		for (j = 0; j < num_block_obj_sizes; ++j)
			free_blocks [j] = NULL;
	}

	/* traverse all blocks, free and zero unmarked objects */
#ifdef FIXED_HEAP
	for (j = 0; j < ms_heap_num_blocks; ++j) {
		MSBlockInfo *block = &block_infos [j];
#else
	iter = &all_blocks;
	while (*iter) {
		MSBlockInfo *block = *iter;
#endif
		int count;
		gboolean have_live = FALSE;
		int obj_index;

#ifdef FIXED_HEAP
		if (!block->used)
			continue;
#endif

		count = MS_BLOCK_FREE / block->obj_size;
		block->free_list = NULL;

		for (obj_index = 0; obj_index < count; ++obj_index) {
			int word, bit;
			void *obj = MS_BLOCK_OBJ (block, obj_index);

			MS_CALC_MARK_BIT (word, bit, obj);
			if (MS_MARK_BIT (block, word, bit)) {
				DEBUG (9, g_assert (MS_OBJ_ALLOCED (obj, block)));
				have_live = TRUE;
			} else {
				/* an unmarked object */
				if (MS_OBJ_ALLOCED (obj, block)) {
					binary_protocol_empty (obj, block->obj_size);
					memset (obj, 0, block->obj_size);
				}
				*(void**)obj = block->free_list;
				block->free_list = obj;
			}
		}

		/* reset mark bits */
		memset (block->mark_words, 0, sizeof (mword) * MS_NUM_MARK_WORDS);

		/*
		 * FIXME: reverse free list so that it's in address
		 * order
		 */

		if (have_live) {
#ifndef FIXED_HEAP
			iter = &block->next;
#endif

			/*
			 * If there are free slots in the block, add
			 * the block to the corresponding free list.
			 */
			if (block->free_list) {
				MSBlockInfo **free_blocks = FREE_BLOCKS (block->pinned, block->has_references);
				int index = MS_BLOCK_OBJ_SIZE_INDEX (block->obj_size);
				block->next_free = free_blocks [index];
				free_blocks [index] = block;
			}
		} else {
			/*
			 * Blocks without live objects are removed from the
			 * block list and freed.
			 */
#ifdef FIXED_HEAP
			ms_free_block (block);
#else
			*iter = block->next;

			ms_free_block (block->block);
			mono_sgen_free_internal (block, INTERNAL_MEM_MS_BLOCK_INFO);
#endif

			--num_major_sections;
		}
	}
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

static void __attribute__ ((unused))
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
	mono_sgen_register_major_sections_alloced (num_major_sections - old_num_major_sections);
}

static void
major_finish_major_collection (void)
{
#ifndef FIXED_HEAP
	int section_reserve = mono_sgen_get_minor_collection_allowance () / MS_BLOCK_SIZE;

	/*
	 * FIXME: We don't free blocks on 32 bit platforms because it
	 * can lead to address space fragmentation, since we're
	 * allocating blocks in larger contingents.
	 */
	if (sizeof (mword) < 8)
		return;

	while (num_empty_blocks > section_reserve) {
		void *next = *(void**)empty_blocks;
		mono_sgen_free_os_memory (empty_blocks, MS_BLOCK_SIZE);
		empty_blocks = next;
		/*
		 * Needs not be atomic because this is running
		 * single-threaded.
		 */
		--num_empty_blocks;

		++stat_major_blocks_freed;
	}
#endif
}

static void
major_find_pin_queue_start_ends (SgenGrayQueue *queue)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		block->pin_queue_start = mono_sgen_find_optimized_pin_queue_area (block->block + MS_BLOCK_SKIP, block->block + MS_BLOCK_SIZE,
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

#ifdef FIXED_HEAP
static gboolean
major_handle_gc_param (const char *opt)
{
	if (g_str_has_prefix (opt, "major-heap-size=")) {
		const char *arg = strchr (opt, '=') + 1;
		glong size;
		if (!mono_sgen_parse_environment_string_extract_number (arg, &size))
			return FALSE;
		ms_heap_num_blocks = (size + MS_BLOCK_SIZE - 1) / MS_BLOCK_SIZE;
		g_assert (ms_heap_num_blocks > 0);
		return TRUE;
	}

	return FALSE;
}

static void
major_print_gc_param_usage (void)
{
	fprintf (stderr, "  major-heap-size=N (where N is an integer, possibly with a k, m or a g suffix)\n");
}
#endif

#ifdef SGEN_HAVE_CARDTABLE
static void
major_iterate_live_block_ranges (sgen_cardtable_block_callback callback)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		if (block->has_references)
			callback ((mword)block->block, MS_BLOCK_SIZE);
	} END_FOREACH_BLOCK;
}

static void
major_scan_card_table (SgenGrayQueue *queue)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		int i;
		int block_obj_size = block->obj_size;
		char *start = block->block;

		if (!block->has_references)
			continue;

		for (i = 0; i < CARDS_PER_BLOCK; ++i, start += CARD_SIZE_IN_BYTES) {
			int index;
			char *obj, *end;

			if (!sgen_card_table_card_begin_scanning ((mword)start))
				continue;

			end = start + CARD_SIZE_IN_BYTES;
			if (i == 0)
				index = 0;
			else
				index = MS_BLOCK_OBJ_INDEX (start, block);

			obj = (char*)MS_BLOCK_OBJ (block, index);
			while (obj < end) {
				if (MS_OBJ_ALLOCED (obj, block))
					minor_scan_object (obj, queue);
				obj += block_obj_size;
			}
		}
	} END_FOREACH_BLOCK;
}
#endif

void
#ifdef SGEN_PARALLEL_MARK
#ifdef FIXED_HEAP
mono_sgen_marksweep_fixed_par_init
#else
mono_sgen_marksweep_par_init
#endif
#else
#ifdef FIXED_HEAP
mono_sgen_marksweep_fixed_init
#else
mono_sgen_marksweep_init
#endif
#endif
	(SgenMajorCollector *collector)
{
	int i;

#ifndef FIXED_HEAP
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_MS_BLOCK_INFO, sizeof (MSBlockInfo));
#endif

	num_block_obj_sizes = ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, NULL);
	block_obj_sizes = mono_sgen_alloc_internal_dynamic (sizeof (int) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);
	ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, block_obj_sizes);

	/*
	{
		int i;
		g_print ("block object sizes:\n");
		for (i = 0; i < num_block_obj_sizes; ++i)
			g_print ("%d\n", block_obj_sizes [i]);
	}
	*/

	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i)
		free_block_lists [i] = mono_sgen_alloc_internal_dynamic (sizeof (MSBlockInfo*) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);

	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES; ++i)
		fast_block_obj_size_indexes [i] = ms_find_block_obj_size_index (i * 8);
	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES * 8; ++i)
		g_assert (MS_BLOCK_OBJ_SIZE_INDEX (i) == ms_find_block_obj_size_index (i));

	LOCK_INIT (ms_block_list_mutex);

	mono_counters_register ("# major blocks allocated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_alloced);
	mono_counters_register ("# major blocks freed", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_freed);

	collector->section_size = MAJOR_SECTION_SIZE;
#ifdef SGEN_PARALLEL_MARK
	collector->is_parallel = TRUE;
#else
	collector->is_parallel = FALSE;
#endif
	collector->supports_cardtable = !collector->is_parallel;

	collector->alloc_heap = major_alloc_heap;
	collector->is_object_live = major_is_object_live;
	collector->alloc_small_pinned_obj = major_alloc_small_pinned_obj;
	collector->alloc_degraded = major_alloc_degraded;
	collector->copy_or_mark_object = major_copy_or_mark_object;
	collector->alloc_object = major_alloc_object;
	collector->free_pinned_object = free_pinned_object;
	collector->iterate_objects = major_iterate_objects;
	collector->free_non_pinned_object = major_free_non_pinned_object;
	collector->find_pin_queue_start_ends = major_find_pin_queue_start_ends;
	collector->pin_objects = major_pin_objects;
#ifdef SGEN_HAVE_CARDTABLE
	collector->scan_card_table = major_scan_card_table;
	collector->iterate_live_block_ranges = major_iterate_live_block_ranges;
#endif
	collector->init_to_space = major_init_to_space;
	collector->sweep = major_sweep;
	collector->check_scan_starts = major_check_scan_starts;
	collector->dump_heap = major_dump_heap;
	collector->get_used_size = major_get_used_size;
	collector->start_nursery_collection = major_start_nursery_collection;
	collector->finish_nursery_collection = major_finish_nursery_collection;
	collector->finish_major_collection = major_finish_major_collection;
	collector->ptr_is_in_non_pinned_space = major_ptr_is_in_non_pinned_space;
	collector->obj_is_from_pinned_alloc = obj_is_from_pinned_alloc;
	collector->report_pinned_memory_usage = major_report_pinned_memory_usage;
	collector->get_num_major_sections = get_num_major_sections;
#ifdef FIXED_HEAP
	collector->handle_gc_param = major_handle_gc_param;
	collector->print_gc_param_usage = major_print_gc_param_usage;
#else
	collector->handle_gc_param = NULL;
	collector->print_gc_param_usage = NULL;
#endif

	FILL_COLLECTOR_COPY_OBJECT (collector);
	FILL_COLLECTOR_SCAN_OBJECT (collector);
}

#endif
