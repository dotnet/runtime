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
#include "metadata/gc-internal.h"

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
	int obj_size_index;
	int pin_queue_num_entries;
	unsigned int pinned : 1;
	unsigned int has_references : 1;
	unsigned int has_pinned : 1;	/* means cannot evacuate */
	unsigned int is_to_space : 1;
#ifdef FIXED_HEAP
	unsigned int used : 1;
	unsigned int zeroed : 1;
#endif
	MSBlockInfo *next;
	char *block;
	void **free_list;
	MSBlockInfo *next_free;
	void **pin_queue_start;
	mword mark_words [MS_NUM_MARK_WORDS];
};

#ifdef FIXED_HEAP
static int ms_heap_num_blocks = MS_DEFAULT_HEAP_NUM_BLOCKS;

static char *ms_heap_start;
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
#define LOCK_MS_BLOCK_LIST mono_mutex_lock (&ms_block_list_mutex)
#define UNLOCK_MS_BLOCK_LIST mono_mutex_unlock (&ms_block_list_mutex)
#endif

static gboolean *evacuate_block_obj_sizes;
static float evacuation_threshold = 0.666;

static gboolean concurrent_sweep = FALSE;
static gboolean have_swept;

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
static long long stat_major_objects_evacuated = 0;
static long long stat_time_wait_for_sweep = 0;

static gboolean ms_sweep_in_progress = FALSE;
static MonoNativeThreadId ms_sweep_thread;
static MonoSemType ms_sweep_cmd_semaphore;
static MonoSemType ms_sweep_done_semaphore;

static void
ms_signal_sweep_command (void)
{
	if (!concurrent_sweep)
		return;

	g_assert (!ms_sweep_in_progress);
	ms_sweep_in_progress = TRUE;
	MONO_SEM_POST (&ms_sweep_cmd_semaphore);
}

static void
ms_signal_sweep_done (void)
{
	if (!concurrent_sweep)
		return;

	MONO_SEM_POST (&ms_sweep_done_semaphore);
}

static void
ms_wait_for_sweep_done (void)
{
	SGEN_TV_DECLARE (atv);
	SGEN_TV_DECLARE (btv);
	int result;

	if (!concurrent_sweep)
		return;

	if (!ms_sweep_in_progress)
		return;

	SGEN_TV_GETTIME (atv);
	while ((result = MONO_SEM_WAIT (&ms_sweep_done_semaphore)) != 0) {
		if (errno != EINTR)
			g_error ("MONO_SEM_WAIT");
	}
	SGEN_TV_GETTIME (btv);
	stat_time_wait_for_sweep += SGEN_TV_ELAPSED (atv, btv);

	g_assert (ms_sweep_in_progress);
	ms_sweep_in_progress = FALSE;
}

static int
ms_find_block_obj_size_index (int size)
{
	int i;
	DEBUG (9, g_assert (size <= SGEN_MAX_SMALL_OBJ_SIZE));
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
	int i;

	g_assert (ms_heap_num_blocks > 0);
	g_assert (nursery_size % MS_BLOCK_SIZE == 0);
	if (nursery_align)
		g_assert (nursery_align % MS_BLOCK_SIZE == 0);

	nursery_start = sgen_alloc_os_memory_aligned (alloc_size, nursery_align ? nursery_align : MS_BLOCK_SIZE, TRUE);
	ms_heap_start = nursery_start + nursery_size;
	ms_heap_end = ms_heap_start + major_heap_size;

	block_infos = sgen_alloc_internal_dynamic (sizeof (MSBlockInfo) * ms_heap_num_blocks, INTERNAL_MEM_MS_BLOCK_INFO);

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
		start = sgen_alloc_os_memory_aligned (nursery_size, nursery_align, TRUE);
	else
		start = sgen_alloc_os_memory (nursery_size, TRUE);

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
		p = sgen_alloc_os_memory_aligned (MS_BLOCK_SIZE * MS_BLOCK_ALLOC_NUM, MS_BLOCK_SIZE, TRUE);

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
			} while (SGEN_CAS_PTR ((gpointer*)&empty_blocks, block, empty) != empty);
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

	DEBUG (9, g_assert (count >= 2));

	info->obj_size = size;
	info->obj_size_index = size_index;
	info->pinned = pinned;
	info->has_references = has_references;
	info->has_pinned = pinned;
	info->is_to_space = (sgen_get_current_collection_generation () == GENERATION_OLD); /*FIXME WHY??? */
#ifndef FIXED_HEAP
	info->block = ms_get_empty_block ();

	header = (MSBlockHeader*) info->block;
	header->info = info;
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
	DEBUG (9, g_assert (block));

	obj = block->free_list;
	DEBUG (9, g_assert (obj));

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
alloc_obj_par (int size, gboolean pinned, gboolean has_references)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo **free_blocks_local = FREE_BLOCKS_LOCAL (pinned, has_references);
	MSBlockInfo *block;
	void *obj;

	DEBUG (9, g_assert (!ms_sweep_in_progress));
	DEBUG (9, g_assert (current_collection_generation == GENERATION_OLD));

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

	/*
	 * FIXME: This should not be necessary because it'll be
	 * overwritten by the vtable immediately.
	 */
	*(void**)obj = NULL;

	return obj;
}

static void*
major_par_alloc_object (int size, gboolean has_references)
{
	return alloc_obj_par (size, FALSE, has_references);
}
#endif

static void*
alloc_obj (int size, gboolean pinned, gboolean has_references)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo **free_blocks = FREE_BLOCKS (pinned, has_references);
	void *obj;

#ifdef SGEN_PARALLEL_MARK
	DEBUG (9, g_assert (current_collection_generation != GENERATION_OLD));
#endif

	DEBUG (9, g_assert (!ms_sweep_in_progress));

	if (!free_blocks [size_index]) {
		if (G_UNLIKELY (!ms_alloc_block (size_index, pinned, has_references)))
			return NULL;
	}

	obj = unlink_slot_from_free_list_uncontested (free_blocks, size_index);

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
	void *res;

	ms_wait_for_sweep_done ();

	res = alloc_obj (size, TRUE, has_references);
	 /*If we failed to alloc memory, we better try releasing memory
	  *as pinned alloc is requested by the runtime.
	  */
	 if (!res) {
		sgen_perform_collection (0, GENERATION_OLD, "pinned alloc failure");
		res = alloc_obj (size, TRUE, has_references);
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

	ms_wait_for_sweep_done ();

	old_num_sections = num_major_sections;

	obj = alloc_obj (size, FALSE, SGEN_VTABLE_HAS_REFERENCES (vtable));
	if (G_LIKELY (obj)) {
		*(MonoVTable**)obj = vtable;
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
	DEBUG (9, g_assert (!block->pinned));
	MS_CALC_MARK_BIT (word, bit, obj);
	return MS_MARK_BIT (block, word, bit) ? TRUE : FALSE;
}

static gboolean
major_ptr_is_in_non_pinned_space (char *ptr)
{
	MSBlockInfo *block;

	FOREACH_BLOCK (block) {
		if (ptr >= block->block && ptr <= block->block + MS_BLOCK_SIZE)
			return !block->pinned;
	} END_FOREACH_BLOCK;
	return FALSE;
}

static void
major_iterate_objects (gboolean non_pinned, gboolean pinned, IterateObjectCallbackFunc callback, void *data)
{
	MSBlockInfo *block;

	ms_wait_for_sweep_done ();

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

static gboolean
major_is_valid_object (char *object)
{
	MSBlockInfo *block;

	ms_wait_for_sweep_done ();
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

		if ((block->block > ptr) || ((block->block + MS_BLOCK_SIZE) <= ptr))
			continue;

		fprintf (gc_debug_file, "major-ptr (block %p sz %d pin %d ref %d) ",
			block->block, block->obj_size, block->pinned, block->has_references);

		idx = MS_BLOCK_OBJ_INDEX (ptr, block);
		obj = (char*)MS_BLOCK_OBJ (block, idx);
		live = MS_OBJ_ALLOCED (obj, block);
		vtable = live ? (MonoVTable*)SGEN_LOAD_VTABLE (obj) : NULL;
		
		if (obj == ptr) {
			if (live)
				fprintf (gc_debug_file, "(object %s.%s)", vtable->klass->name_space, vtable->klass->name);
			else
				fprintf (gc_debug_file, "(dead-object)");
		} else {
			if (live)
				fprintf (gc_debug_file, "(interior-ptr offset %td of %p %s.%s)",
					ptr - obj,
					obj, vtable->klass->name_space, vtable->klass->name);
			else
				fprintf (gc_debug_file, "(dead-interior-ptr to %td to %p)",
					ptr - obj, obj);
		}

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
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), sgen_safe_object_get_size ((MonoObject*)(obj))); \
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
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), sgen_safe_object_get_size ((MonoObject*)(obj))); \
		}							\
	} while (0)

static void
pin_major_object (char *obj, SgenGrayQueue *queue)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
	block->has_pinned = TRUE;
	MS_MARK_OBJECT_AND_ENQUEUE (obj, block, queue);
}

#include "sgen-major-copy-object.h"

#ifdef SGEN_PARALLEL_MARK
static void
major_copy_or_mark_object (void **ptr, SgenGrayQueue *queue)
{
	void *obj = *ptr;
	mword objsize;
	MSBlockInfo *block;
	MonoVTable *vt;

	HEAVY_STAT (++stat_copy_object_called_major);

	DEBUG (9, g_assert (obj));
	DEBUG (9, g_assert (current_collection_generation == GENERATION_OLD));

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

		destination = sgen_minor_collector.par_alloc_for_promotion (obj, objsize, has_references);
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

		/*
		 * We do this before the CAS because we want to make
		 * sure that if another thread sees the destination
		 * pointer the VTable is already in place.  Not doing
		 * this can crash binary protocols.
		 */
		*(MonoVTable**)destination = vt;

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
				DEBUG (9, g_assert (!MS_MARK_BIT (block, word, bit)));
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
#ifdef FIXED_HEAP
			mword vtable_word = *(mword*)obj;
			vt = (MonoVTable*)(vtable_word & ~SGEN_VTABLE_BITS_MASK);
#endif

			if (vtable_word & SGEN_PINNED_BIT)
				return;
			binary_protocol_pin (obj, vt, sgen_safe_object_get_size ((MonoObject*)obj));
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
	MSBlockInfo *block;

	HEAVY_STAT (++stat_copy_object_called_major);

	DEBUG (9, g_assert (obj));
	DEBUG (9, g_assert (current_collection_generation == GENERATION_OLD));

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
			DEBUG (9, g_assert (!MS_MARK_BIT (block, word, bit)));
			MS_SET_MARK_BIT (block, word, bit);
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
			if (SGEN_OBJECT_IS_PINNED (obj))
				return;
			binary_protocol_pin (obj, (gpointer)SGEN_LOAD_VTABLE (obj), sgen_safe_object_get_size ((MonoObject*)obj));
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

	if (!block->pin_queue_num_entries)
		return;

	block->has_pinned = TRUE;

	for (i = 0; i < block->pin_queue_num_entries; ++i) {
		int index = MS_BLOCK_OBJ_INDEX (block->pin_queue_start [i], block);
		DEBUG (9, g_assert (index >= 0 && index < MS_BLOCK_FREE / block->obj_size));
		if (index == last_index)
			continue;
		MS_MARK_OBJECT_AND_ENQUEUE_CHECKED (MS_BLOCK_OBJ (block, index), block, queue);
		last_index = index;
	}
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
		int obj_index;
		int obj_size_index;

		obj_size_index = block->obj_size_index;

		has_pinned = block->has_pinned;
		block->has_pinned = block->pinned;

		block->is_to_space = FALSE;

		count = MS_BLOCK_FREE / block->obj_size;
		block->free_list = NULL;

		for (obj_index = 0; obj_index < count; ++obj_index) {
			int word, bit;
			void *obj = MS_BLOCK_OBJ (block, obj_index);

			MS_CALC_MARK_BIT (word, bit, obj);
			if (MS_MARK_BIT (block, word, bit)) {
				DEBUG (9, g_assert (MS_OBJ_ALLOCED (obj, block)));
				have_live = TRUE;
				if (!has_pinned)
					++slots_used [obj_size_index];
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
			if (!has_pinned) {
				++num_blocks [obj_size_index];
				slots_available [obj_size_index] += count;
			}

			iter = &block->next;

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
	}

	have_swept = TRUE;
}

static mono_native_thread_return_t
ms_sweep_thread_func (void *dummy)
{
	g_assert (concurrent_sweep);

	for (;;) {
		int result;

		while ((result = MONO_SEM_WAIT (&ms_sweep_cmd_semaphore)) != 0) {
			if (errno != EINTR)
				g_error ("MONO_SEM_WAIT FAILED with %d errno %d (%s)", result, errno, strerror (errno));
		}

		ms_sweep ();

		ms_signal_sweep_done ();
	}

	return NULL;
}

static void
major_sweep (void)
{
	if (concurrent_sweep) {
		g_assert (ms_sweep_thread);
		ms_signal_sweep_command ();
	} else {
		ms_sweep ();
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
	ms_wait_for_sweep_done ();

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

	ms_wait_for_sweep_done ();

	/* clear the free lists */
	for (i = 0; i < num_block_obj_sizes; ++i) {
		if (!evacuate_block_obj_sizes [i])
			continue;

		free_block_lists [0][i] = NULL;
		free_block_lists [MS_BLOCK_FLAG_REFS][i] = NULL;
	}
}

static void
major_finish_major_collection (void)
{
}

static void
major_have_computer_minor_collection_allowance (void)
{
#ifndef FIXED_HEAP
	int section_reserve = sgen_get_minor_collection_allowance () / MS_BLOCK_SIZE;

	g_assert (have_swept);
	ms_wait_for_sweep_done ();
	g_assert (!ms_sweep_in_progress);

	/*
	 * FIXME: We don't free blocks on 32 bit platforms because it
	 * can lead to address space fragmentation, since we're
	 * allocating blocks in larger contingents.
	 */
	if (sizeof (mword) < 8)
		return;

	while (num_empty_blocks > section_reserve) {
		void *next = *(void**)empty_blocks;
		sgen_free_os_memory (empty_blocks, MS_BLOCK_SIZE);
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
	} else if (!strcmp (opt, "concurrent-sweep")) {
		concurrent_sweep = TRUE;
		return TRUE;
	} else if (!strcmp (opt, "no-concurrent-sweep")) {
		concurrent_sweep = FALSE;
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
			"  (no-)concurrent-sweep\n"
			);
}

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
major_scan_card_table (SgenGrayQueue *queue)
{
	MSBlockInfo *block;
	ScanObjectFunc scan_func = sgen_get_current_object_ops ()->scan_object;

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

			/*We can avoid the extra copy since the remark cardtable was cleaned before */
#ifdef SGEN_HAVE_OVERLAPPING_CARDS
			cards = sgen_card_table_get_card_scan_address ((mword)block_start);
#else
			cards = cards_data;
			if (!sgen_card_table_get_card_data (cards_data, (mword)block_start, CARDS_PER_BLOCK))
				continue;
#endif

			obj = (char*)MS_BLOCK_OBJ_FAST (block_start, block_obj_size, 0);
			end = block_start + MS_BLOCK_SIZE;
			base = sgen_card_table_align_pointer (obj);

			while (obj < end) {
				if (MS_OBJ_ALLOCED_FAST (obj, block_start)) {
					int card_offset = (obj - base) >> CARD_BITS;
					sgen_cardtable_scan_object (obj, block_obj_size, cards + card_offset, queue);
				}
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
			card_data = card_base = sgen_card_table_get_card_scan_address ((mword)block_start);
			card_data_end = card_data + CARDS_PER_BLOCK;

			for (card_data = initial_skip_card (card_data); card_data < card_data_end; ++card_data) { //card_data = skip_card (card_data + 1, card_data_end)) {
				int index;
				int idx = card_data - card_base;
				char *start = (char*)(block_start + idx * CARD_SIZE_IN_BYTES);
				char *end = start + CARD_SIZE_IN_BYTES;
				char *obj;

				HEAVY_STAT (++scanned_cards);

				if (!*card_data)
					continue;

				HEAVY_STAT (++marked_cards);

				sgen_card_table_prepare_card_for_scanning (card_data);

				if (idx == 0)
					index = 0;
				else
					index = MS_BLOCK_OBJ_INDEX_FAST (start, block_start, block_obj_size);

				obj = (char*)MS_BLOCK_OBJ_FAST (block_start, block_obj_size, index);
				while (obj < end) {
					if (MS_OBJ_ALLOCED_FAST (obj, block_start)) {
						HEAVY_STAT (++scanned_objects);
						scan_func (obj, queue);
					}
					obj += block_obj_size;
				}
				HEAVY_STAT (if (*card_data) ++remarked_cards);
			}
		}
	} END_FOREACH_BLOCK;
}
#endif

static gboolean
major_is_worker_thread (MonoNativeThreadId thread)
{
	if (concurrent_sweep)
		return thread == ms_sweep_thread;
	else
		return FALSE;
}

static void
alloc_free_block_lists (MSBlockInfo ***lists)
{
	int i;
	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i)
		lists [i] = sgen_alloc_internal_dynamic (sizeof (MSBlockInfo*) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);
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
post_param_init (void)
{
	if (concurrent_sweep) {
		if (!mono_native_thread_create (&ms_sweep_thread, ms_sweep_thread_func, NULL)) {
			fprintf (stderr, "Error: Could not create sweep thread.\n");
			exit (1);
		}
	}
}

void
#ifdef SGEN_PARALLEL_MARK
#ifdef FIXED_HEAP
sgen_marksweep_fixed_par_init
#else
sgen_marksweep_par_init
#endif
#else
#ifdef FIXED_HEAP
sgen_marksweep_fixed_init
#else
sgen_marksweep_init
#endif
#endif
	(SgenMajorCollector *collector)
{
	int i;

#ifndef FIXED_HEAP
	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_MS_BLOCK_INFO, sizeof (MSBlockInfo));
#endif

	num_block_obj_sizes = ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, NULL);
	block_obj_sizes = sgen_alloc_internal_dynamic (sizeof (int) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);
	ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, block_obj_sizes);

	evacuate_block_obj_sizes = sgen_alloc_internal_dynamic (sizeof (gboolean) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);
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
	mono_counters_register ("# major objects evacuated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_objects_evacuated);
	mono_counters_register ("Wait for sweep time", MONO_COUNTER_GC | MONO_COUNTER_TIME_INTERVAL, &stat_time_wait_for_sweep);
#ifdef SGEN_PARALLEL_MARK
#ifndef HAVE_KW_THREAD
	mono_native_tls_alloc (&workers_free_block_lists_key, NULL);
#endif
#endif

	/*
	 * FIXME: These are superfluous if concurrent sweep is
	 * disabled.  We might want to create them lazily.
	 */
	MONO_SEM_INIT (&ms_sweep_cmd_semaphore, 0);
	MONO_SEM_INIT (&ms_sweep_done_semaphore, 0);

	collector->section_size = MAJOR_SECTION_SIZE;
#ifdef SGEN_PARALLEL_MARK
	collector->is_parallel = TRUE;
	collector->alloc_worker_data = major_alloc_worker_data;
	collector->init_worker_thread = major_init_worker_thread;
	collector->reset_worker_data = major_reset_worker_data;
#else
	collector->is_parallel = FALSE;
#endif
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
#ifdef SGEN_HAVE_CARDTABLE
	collector->scan_card_table = major_scan_card_table;
	collector->iterate_live_block_ranges = (void*)(void*) major_iterate_live_block_ranges;
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
	collector->is_worker_thread = major_is_worker_thread;
	collector->post_param_init = post_param_init;
	collector->is_valid_object = major_is_valid_object;
	collector->describe_pointer = major_describe_pointer;

	collector->major_ops.copy_or_mark_object = major_copy_or_mark_object;
	collector->major_ops.scan_object = major_scan_object;

#ifdef SGEN_HAVE_CARDTABLE
	/*cardtable requires major pages to be 8 cards aligned*/
	g_assert ((MS_BLOCK_SIZE % (8 * CARD_SIZE_IN_BYTES)) == 0);
#endif
}

#endif
