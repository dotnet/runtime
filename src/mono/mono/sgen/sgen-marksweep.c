/**
 * \file
 * The Mark & Sweep major collector.
 *
 * Author:
 * 	Mark Probst <mark.probst@gmail.com>
 *
 * Copyright 2009-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#ifdef HAVE_SGEN_GC

#include <math.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-cardtable.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-layout-stats.h"
#include "mono/sgen/sgen-pointer-queue.h"
#include "mono/sgen/sgen-array-list.h"
#include "mono/sgen/sgen-pinning.h"
#include "mono/sgen/sgen-workers.h"
#include "mono/sgen/sgen-thread-pool.h"
#include "mono/sgen/sgen-client.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/mono-proclib.h"

static int ms_block_size;

/*
 * Blocks must be at least this size, meaning that if we detect a
 * page size lower than this, we'll use this instead.
 */
#define MS_BLOCK_SIZE_MIN (1024 * 16)

#define CARDS_PER_BLOCK (ms_block_size / CARD_SIZE_IN_BYTES)

/*
 * Don't allocate single blocks, but alloc a contingent of this many
 * blocks in one swoop.  This must be a power of two.
 */
#define MS_BLOCK_ALLOC_NUM	32

#define MS_NUM_MARK_WORDS	((ms_block_size / SGEN_ALLOC_ALIGN + sizeof (guint32) * 8 - 1) / (sizeof (guint32) * 8))

/*
 * Use this instead of sizeof (MSBlockInfo) since the mark_words
 * array size depends on page size at runtime.
 */
#define SIZEOF_MS_BLOCK_INFO (sizeof (MSBlockInfo) + sizeof (guint32) * (MS_NUM_MARK_WORDS - MONO_ZERO_LEN_ARRAY))

/*
 * Number of bytes before the first object in a block.  At the start
 * of a block is the MSBlockHeader, then opional padding, then come
 * the objects, so this must be >= SIZEOF_MS_BLOCK_INFO.
 */
#define MS_BLOCK_SKIP	((SIZEOF_MS_BLOCK_INFO + 15) & ~15)

#define MS_BLOCK_FREE	(ms_block_size - MS_BLOCK_SKIP)

/*
 * Blocks progress from one state to the next:
 *
 * SWEPT           The block is fully swept.  It might or might not be in
 *                 a free list.
 *
 * MARKING         The block might or might not contain live objects.  If
 *                 we're in between an initial collection pause and the
 *                 finishing pause, the block might or might not be in a
 *                 free list.
 *
 * CHECKING        The sweep thread is investigating the block to determine
 *                 whether or not it contains live objects.  The block is
 *                 not in a free list.
 *
 * NEED_SWEEPING   The block contains live objects but has not yet been
 *                 swept.  It also contains free slots.  It is in a block
 *                 free list.
 *
 * SWEEPING        The block is being swept.  It might be in a free list.
 */

enum {
	BLOCK_STATE_SWEPT,
	BLOCK_STATE_MARKING,
	BLOCK_STATE_CHECKING,
	BLOCK_STATE_NEED_SWEEPING,
	BLOCK_STATE_SWEEPING
};

typedef struct _MSBlockInfo MSBlockInfo;
struct _MSBlockInfo {
	guint16 obj_size;
	/*
	 * FIXME: Do we even need this? It's only used during sweep and might be worth
	 * recalculating to save the space.
	 */
	guint16 obj_size_index;
	/* FIXME: Reduce this - it only needs a byte. */
	volatile gint32 state;
	gint16 nused;
	unsigned int pinned : 1;
	unsigned int has_references : 1;
	unsigned int has_pinned : 1;	/* means cannot evacuate */
	unsigned int is_to_space : 1;
	void ** volatile free_list;
	MSBlockInfo * volatile next_free;
	guint8 * volatile cardtable_mod_union;
	guint32 mark_words [MONO_ZERO_LEN_ARRAY];
};

#define MS_BLOCK_FOR_BLOCK_INFO(b)	((char*)(b))

#define MS_BLOCK_OBJ(b,i)		((GCObject *)(MS_BLOCK_FOR_BLOCK_INFO(b) + MS_BLOCK_SKIP + (b)->obj_size * (i)))
#define MS_BLOCK_OBJ_FOR_SIZE(b,i,obj_size)		(MS_BLOCK_FOR_BLOCK_INFO(b) + MS_BLOCK_SKIP + (obj_size) * (i))
#define MS_BLOCK_DATA_FOR_OBJ(o)	((char*)((mword)(o) & ~(mword)(ms_block_size - 1)))

typedef struct {
	MSBlockInfo info;
} MSBlockHeader;

#define MS_BLOCK_FOR_OBJ(o)		(&((MSBlockHeader*)MS_BLOCK_DATA_FOR_OBJ ((o)))->info)

/* object index will always be small */
#define MS_BLOCK_OBJ_INDEX(o,b)	((int)(((char*)(o) - (MS_BLOCK_FOR_BLOCK_INFO(b) + MS_BLOCK_SKIP)) / (b)->obj_size))

//casting to int is fine since blocks are 32k
#define MS_CALC_MARK_BIT(w,b,o) 	do {				\
		int i = ((int)((char*)(o) - MS_BLOCK_DATA_FOR_OBJ ((o)))) >> SGEN_ALLOC_ALIGN_BITS; \
		(w) = i >> 5;						\
		(b) = i & 31;						\
	} while (0)

#define MS_MARK_BIT(bl,w,b)	((bl)->mark_words [(w)] & (ONE_P << (b)))
#define MS_SET_MARK_BIT(bl,w,b)	((bl)->mark_words [(w)] |= (ONE_P << (b)))
#define MS_SET_MARK_BIT_PAR(bl,w,b,first)	do {			\
		guint32 tmp_mark_word = (bl)->mark_words [(w)];		\
		guint32 old_mark_word;					\
		first = FALSE;						\
		while (!(tmp_mark_word & (ONE_P << (b)))) {		\
			old_mark_word = tmp_mark_word;			\
			tmp_mark_word = mono_atomic_cas_i32 ((volatile gint32*)&(bl)->mark_words [w], old_mark_word | (ONE_P << (b)), old_mark_word); \
			if (tmp_mark_word == old_mark_word) {		\
				first = TRUE;				\
				break;					\
			}						\
		}							\
	} while (0)


#define MS_OBJ_ALLOCED(o,b)	(*(void**)(o) && (*(char**)(o) < MS_BLOCK_FOR_BLOCK_INFO (b) || *(char**)(o) >= MS_BLOCK_FOR_BLOCK_INFO (b) + ms_block_size))

#define MS_BLOCK_OBJ_SIZE_FACTOR	(pow (2.0, 1.0 / 3))

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

static gboolean *evacuate_block_obj_sizes;
static float evacuation_threshold = 0.666f;

static gboolean lazy_sweep = TRUE;

enum {
	SWEEP_STATE_SWEPT,
	SWEEP_STATE_NEED_SWEEPING,
	SWEEP_STATE_SWEEPING,
	SWEEP_STATE_SWEEPING_AND_ITERATING,
	SWEEP_STATE_COMPACTING
};

typedef enum {
	SGEN_SWEEP_SERIAL = FALSE,
	SGEN_SWEEP_CONCURRENT = TRUE,
} SgenSweepMode;

static volatile int sweep_state = SWEEP_STATE_SWEPT;

static gboolean concurrent_mark;
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
static gboolean concurrent_sweep = DEFAULT_SWEEP_MODE;
#else
static const gboolean concurrent_sweep = SGEN_SWEEP_SERIAL;
#endif

static int sweep_pool_context = -1;

#define BLOCK_IS_TAGGED_HAS_REFERENCES(bl)	SGEN_POINTER_IS_TAGGED_1 ((bl))
#define BLOCK_TAG_HAS_REFERENCES(bl)		SGEN_POINTER_TAG_1 ((bl))

#define BLOCK_IS_TAGGED_CHECKING(bl)		SGEN_POINTER_IS_TAGGED_2 ((bl))
#define BLOCK_TAG_CHECKING(bl)			SGEN_POINTER_TAG_2 ((bl))

#define BLOCK_UNTAG(bl)				((MSBlockInfo *)SGEN_POINTER_UNTAG_12 ((bl)))

#define BLOCK_TAG(bl)				((bl)->has_references ? BLOCK_TAG_HAS_REFERENCES ((bl)) : (bl))

/* all allocated blocks in the system */
static SgenArrayList allocated_blocks = SGEN_ARRAY_LIST_INIT (NULL, sgen_array_list_default_is_slot_set, sgen_array_list_default_cas_setter, INTERNAL_MEM_PIN_QUEUE);

/* non-allocated block free-list */
static void *empty_blocks = NULL;
static size_t num_empty_blocks = 0;
static gboolean compact_blocks = FALSE;

/*
 * We can iterate the block list also while sweep is in progress but we
 * need to account for blocks that will be checked for sweeping and even
 * freed in the process.
 */
#define FOREACH_BLOCK_NO_LOCK(bl) {					\
	volatile gpointer *slot;						\
	SGEN_ARRAY_LIST_FOREACH_SLOT (&allocated_blocks, slot) {	\
		(bl) = BLOCK_UNTAG (*slot);				\
		if (!(bl))						\
			continue;
#define FOREACH_BLOCK_HAS_REFERENCES_NO_LOCK(bl,hr) {			\
	volatile gpointer *slot;						\
	SGEN_ARRAY_LIST_FOREACH_SLOT (&allocated_blocks, slot) {	\
		(bl) = (MSBlockInfo *) (*slot);			\
		if (!(bl))						\
			continue;					\
		(hr) = BLOCK_IS_TAGGED_HAS_REFERENCES ((bl));		\
		(bl) = BLOCK_UNTAG ((bl));
#define END_FOREACH_BLOCK_NO_LOCK	} SGEN_ARRAY_LIST_END_FOREACH_SLOT; }

#define FOREACH_BLOCK_RANGE_HAS_REFERENCES_NO_LOCK(bl,begin,end,index,hr) {	\
	volatile gpointer *slot;					\
	SGEN_ARRAY_LIST_FOREACH_SLOT_RANGE (&allocated_blocks, begin, end, slot, index) { \
		(bl) = (MSBlockInfo *) (*slot);				\
		if (!(bl))						\
			continue;					\
		(hr) = BLOCK_IS_TAGGED_HAS_REFERENCES ((bl));		\
		(bl) = BLOCK_UNTAG ((bl));
#define END_FOREACH_BLOCK_RANGE_NO_LOCK	} SGEN_ARRAY_LIST_END_FOREACH_SLOT_RANGE; }

static volatile size_t num_major_sections = 0;
/*
 * One free block list for each block object size.  We add and remove blocks from these
 * lists lock-free via CAS.
 *
 * Blocks accessed/removed from `free_block_lists`:
 *   from the mutator (with GC lock held)
 *   in nursery collections
 *   in non-concurrent major collections
 *   in the finishing pause of concurrent major collections (whole list is cleared)
 *
 * Blocks added to `free_block_lists`:
 *   in the sweeping thread
 *   during nursery collections
 *   from domain clearing (with the world stopped and no sweeping happening)
 *
 * The only item of those that doesn't require the GC lock is the sweep thread.  The sweep
 * thread only ever adds blocks to the free list, so the ABA problem can't occur.
 */
static MSBlockInfo * volatile *free_block_lists [MS_BLOCK_TYPE_MAX];
static MonoNativeTlsKey worker_block_free_list_key;

static guint64 stat_major_blocks_alloced = 0;
static guint64 stat_major_blocks_freed = 0;
static guint64 stat_major_blocks_lazy_swept = 0;

static guint64 stat_major_blocks_freed_ideal = 0;
static guint64 stat_major_blocks_freed_less_ideal = 0;
static guint64 stat_major_blocks_freed_individual = 0;
static guint64 stat_major_blocks_alloced_less_ideal = 0;

#ifdef SGEN_COUNT_NUMBER_OF_MAJOR_OBJECTS_MARKED
static guint64 num_major_objects_marked = 0;
#define INC_NUM_MAJOR_OBJECTS_MARKED()	(++num_major_objects_marked)
#else
#define INC_NUM_MAJOR_OBJECTS_MARKED()
#endif

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
static mono_mutex_t scanned_objects_list_lock;
static SgenPointerQueue scanned_objects_list;

static void
add_scanned_object (void *ptr)
{
	if (!sgen_binary_protocol_is_enabled ())
		return;

	mono_os_mutex_lock (&scanned_objects_list_lock);
	sgen_pointer_queue_add (&scanned_objects_list, ptr);
	mono_os_mutex_unlock (&scanned_objects_list_lock);
}
#endif

static gboolean sweep_block (MSBlockInfo *block);

static int
ms_find_block_obj_size_index (size_t size)
{
	int i;
	SGEN_ASSERT (9, size <= SGEN_MAX_SMALL_OBJ_SIZE, "size %" G_GSIZE_FORMAT "d is bigger than max small object size %d", size, SGEN_MAX_SMALL_OBJ_SIZE);
	for (i = 0; i < num_block_obj_sizes; ++i)
		if (block_obj_sizes [i] >= size)
			return i;
	g_error ("no object of size %" G_GSIZE_FORMAT "d\n", size);
	return -1;
}

#define FREE_BLOCKS_FROM(lists,p,r)	(lists [((p) ? MS_BLOCK_FLAG_PINNED : 0) | ((r) ? MS_BLOCK_FLAG_REFS : 0)])
#define FREE_BLOCKS(p,r)		(FREE_BLOCKS_FROM (free_block_lists, (p), (r)))
#define FREE_BLOCKS_LOCAL(p,r)		(FREE_BLOCKS_FROM (((MSBlockInfo***)mono_native_tls_get_value (worker_block_free_list_key)), (p), (r)))

#define MS_BLOCK_OBJ_SIZE_INDEX(s)				\
	(((s)+7)>>3 < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES ?	\
	 fast_block_obj_size_indexes [((s)+7)>>3] :		\
	 ms_find_block_obj_size_index ((s)))

static void*
major_alloc_heap (mword nursery_size, mword nursery_align)
{
	char *start;
	if (nursery_align)
		start = (char *)sgen_alloc_os_memory_aligned (nursery_size, nursery_align, (SgenAllocFlags)(SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE), "nursery", MONO_MEM_ACCOUNT_SGEN_NURSERY);
	else
		start = (char *)sgen_alloc_os_memory (nursery_size, (SgenAllocFlags)(SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE), "nursery", MONO_MEM_ACCOUNT_SGEN_NURSERY);

	return start;
}

static void
update_heap_boundaries_for_block (MSBlockInfo *block)
{
	sgen_update_heap_boundaries ((mword)MS_BLOCK_FOR_BLOCK_INFO (block), (mword)MS_BLOCK_FOR_BLOCK_INFO (block) + ms_block_size);
}

/*
 * Thread safe
 */
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
			p = (char *)sgen_alloc_os_memory_aligned (ms_block_size * alloc_num, ms_block_size,
				(SgenAllocFlags)(SGEN_ALLOC_HEAP | SGEN_ALLOC_ACTIVATE),
				alloc_num == 1 ? "major heap section" : NULL, MONO_MEM_ACCOUNT_SGEN_MARKSWEEP);
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
			p += ms_block_size;
		}

		SGEN_ATOMIC_ADD_P (num_empty_blocks, alloc_num);

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

	SGEN_ATOMIC_ADD_P (num_empty_blocks, -1);

	*(void**)block = NULL;

	g_assert (!((mword)block & (ms_block_size - 1)));

	return block;
}

/*
 * This doesn't actually free a block immediately, but enqueues it into the `empty_blocks`
 * list, where it will either be freed later on, or reused in nursery collections.
 */
static void
ms_free_block (MSBlockInfo *info)
{
	void *empty;
	char *block = MS_BLOCK_FOR_BLOCK_INFO (info);

	sgen_memgov_release_space (ms_block_size, SPACE_MAJOR);
	if (info->cardtable_mod_union)
		sgen_card_table_free_mod_union (info->cardtable_mod_union, block, ms_block_size);
	memset (block, 0, ms_block_size);

	do {
		empty = empty_blocks;
		*(void**)block = empty;
	} while (SGEN_CAS_PTR (&empty_blocks, block, empty) != empty);

	SGEN_ATOMIC_ADD_P (num_empty_blocks, 1);

	sgen_binary_protocol_block_free (block, ms_block_size);
}

static gboolean
sweep_in_progress (void)
{
	int state = sweep_state;
	return state == SWEEP_STATE_SWEEPING ||
		state == SWEEP_STATE_SWEEPING_AND_ITERATING ||
		state == SWEEP_STATE_COMPACTING;
}

static gboolean
block_is_swept_or_marking (MSBlockInfo *block)
{
	gint32 state = block->state;
	return state == BLOCK_STATE_SWEPT || state == BLOCK_STATE_MARKING;
}

//#define MARKSWEEP_CONSISTENCY_CHECK

#ifdef MARKSWEEP_CONSISTENCY_CHECK
static void
check_block_free_list (MSBlockInfo *block, int size, gboolean pinned)
{
	SGEN_ASSERT (0, !sweep_in_progress (), "Can't examine allocated blocks during sweep");
	for (; block; block = block->next_free) {
		SGEN_ASSERT (0, block->state != BLOCK_STATE_CHECKING, "Can't have a block we're checking in a free list.");
		g_assert (block->obj_size == size);
		g_assert ((pinned && block->pinned) || (!pinned && !block->pinned));

		/* blocks in the free lists must have at least
		   one free slot */
		g_assert (block->free_list);

		/* the block must be in the allocated_blocks array */
		g_assert (sgen_array_list_find (&allocated_blocks, BLOCK_TAG (block)) != (guint32)-1);
	}
}

static void
check_empty_blocks (void)
{
	void *p;
	size_t i = 0;
	for (p = empty_blocks; p; p = *(void**)p)
		++i;
	g_assert (i == num_empty_blocks);
}

static void
consistency_check (void)
{
	MSBlockInfo *block;
	int i;

	/* check all blocks */
	FOREACH_BLOCK_NO_LOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;
		int num_free = 0;
		void **free;

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
		if (!sgen_get_concurrent_collection_in_progress () && block_is_swept_or_marking (block)) {
			for (i = 0; i < MS_NUM_MARK_WORDS; ++i)
				g_assert (block->mark_words [i] == 0);
		}
	} END_FOREACH_BLOCK_NO_LOCK;

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
add_free_block (MSBlockInfo * volatile *free_blocks, int size_index, MSBlockInfo *block)
{
	MSBlockInfo *old;
	do {
		block->next_free = old = free_blocks [size_index];
	} while (SGEN_CAS_PTR ((volatile gpointer *)&free_blocks [size_index], block, old) != old);
}

static void major_finish_sweep_checking (void);

static gboolean
ms_alloc_block (int size_index, gboolean pinned, gboolean has_references)
{
	int size = block_obj_sizes [size_index];
	int count = MS_BLOCK_FREE / size;
	MSBlockInfo *info;
	MSBlockInfo * volatile * free_blocks = FREE_BLOCKS (pinned, has_references);
	char *obj_start;
	int i;

	if (!sgen_memgov_try_alloc_space (ms_block_size, SPACE_MAJOR))
		return FALSE;

	info = (MSBlockInfo*)ms_get_empty_block ();

	SGEN_ASSERT (9, count >= 2, "block with %d objects, it must hold at least 2", count);

	info->obj_size = size;
	info->obj_size_index = size_index;
	info->pinned = pinned;
	info->has_references = has_references;
	info->has_pinned = pinned;
	/*
	 * Blocks that are to-space are not evacuated from.  During an major collection
	 * blocks are allocated for two reasons: evacuating objects from the nursery and
	 * evacuating them from major blocks marked for evacuation.  In both cases we don't
	 * want further evacuation. We also don't want to evacuate objects allocated during
	 * the concurrent mark since it would add pointless stress on the finishing pause.
	 */
	info->is_to_space = (sgen_get_current_collection_generation () == GENERATION_OLD) || sgen_get_concurrent_collection_in_progress ();
	info->state = info->is_to_space ? BLOCK_STATE_MARKING : BLOCK_STATE_SWEPT;
	SGEN_ASSERT (6, !sweep_in_progress () || info->state == BLOCK_STATE_SWEPT, "How do we add a new block to be swept while sweeping?");
	info->cardtable_mod_union = NULL;

	update_heap_boundaries_for_block (info);

	sgen_binary_protocol_block_alloc (info, ms_block_size);

	/* build free list */
	obj_start = MS_BLOCK_FOR_BLOCK_INFO (info) + MS_BLOCK_SKIP;
	info->free_list = (void**)obj_start;
	/* we're skipping the last one - it must be nulled */
	for (i = 0; i < count - 1; ++i) {
		char *next_obj_start = obj_start + size;
		*(void**)obj_start = next_obj_start;
		obj_start = next_obj_start;
	}
	/* the last one */
	*(void**)obj_start = NULL;

	add_free_block (free_blocks, size_index, info);

	sgen_array_list_add (&allocated_blocks, BLOCK_TAG (info), 0, FALSE);

	SGEN_ATOMIC_ADD_P (num_major_sections, 1);
	return TRUE;
}

static gboolean
ptr_is_in_major_block (char *ptr, char **start, gboolean *pinned)
{
	MSBlockInfo *block;

	FOREACH_BLOCK_NO_LOCK (block) {
		if (ptr >= MS_BLOCK_FOR_BLOCK_INFO (block) && ptr <= MS_BLOCK_FOR_BLOCK_INFO (block) + ms_block_size) {
			int count = MS_BLOCK_FREE / block->obj_size;
			int i;

			if (start)
				*start = NULL;
			for (i = 0; i <= count; ++i) {
				if (ptr >= (char*)MS_BLOCK_OBJ (block, i) && ptr < (char*)MS_BLOCK_OBJ (block, i + 1)) {
					if (start)
						*start = (char *)MS_BLOCK_OBJ (block, i);
					break;
				}
			}
			if (pinned)
				*pinned = block->pinned;
			return TRUE;
		}
	} END_FOREACH_BLOCK_NO_LOCK;
	return FALSE;
}

static gboolean
ptr_is_from_pinned_alloc (char *ptr)
{
	gboolean pinned;
	if (ptr_is_in_major_block (ptr, NULL, &pinned))
		return pinned;
	return FALSE;
}

static void
ensure_can_access_block_free_list (MSBlockInfo *block)
{
 retry:
	for (;;) {
		switch (block->state) {
		case BLOCK_STATE_SWEPT:
		case BLOCK_STATE_MARKING:
			return;
		case BLOCK_STATE_CHECKING:
			SGEN_ASSERT (0, FALSE, "How did we get a block that's being checked from a free list?");
			break;
		case BLOCK_STATE_NEED_SWEEPING:
			if (sweep_block (block))
				++stat_major_blocks_lazy_swept;
			break;
		case BLOCK_STATE_SWEEPING:
			/* FIXME: do this more elegantly */
			g_usleep (100);
			goto retry;
		default:
			SGEN_ASSERT (0, FALSE, "Illegal block state");
			break;
		}
	}
}

static void*
unlink_slot_from_free_list_uncontested (MSBlockInfo * volatile *free_blocks, int size_index)
{
	MSBlockInfo *block, *next_free_block;
	void *obj, *next_free_slot;

 retry:
	block = free_blocks [size_index];
	SGEN_ASSERT (9, block, "no free block to unlink from free_blocks %p size_index %d", free_blocks, size_index);

	ensure_can_access_block_free_list (block);

	obj = block->free_list;
	SGEN_ASSERT (6, obj, "block %p in free list had no available object to alloc from", block);

	next_free_slot = *(void**)obj;
	if (next_free_slot) {
		block->free_list = (gpointer *)next_free_slot;
		return obj;
	}

	next_free_block = block->next_free;
	if (SGEN_CAS_PTR ((volatile gpointer *)&free_blocks [size_index], next_free_block, block) != block)
		goto retry;

	block->free_list = NULL;
	block->next_free = NULL;

	return obj;
}

static GCObject*
alloc_obj (GCVTable vtable, size_t size, gboolean pinned, gboolean has_references)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo * volatile * free_blocks = FREE_BLOCKS (pinned, has_references);
	void *obj;

	if (!free_blocks [size_index]) {
		if (G_UNLIKELY (!ms_alloc_block (size_index, pinned, has_references)))
			return NULL;
	}

	obj = unlink_slot_from_free_list_uncontested (free_blocks, size_index);

	/* FIXME: assumes object layout */
	*(GCVTable*)obj = vtable;

	sgen_total_allocated_major += block_obj_sizes [size_index];

	return (GCObject *)obj;
}

static GCObject*
major_alloc_object (GCVTable vtable, size_t size, gboolean has_references)
{
	return alloc_obj (vtable, size, FALSE, has_references);
}

/*
 * This can only be called by sgen workers. While this is called we assume
 * that no other thread is accessing the block free lists. The world should
 * be stopped and the gc thread should be waiting for workers to finish.
 */
static GCObject*
major_alloc_object_par (GCVTable vtable, size_t size, gboolean has_references)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo * volatile * free_blocks = FREE_BLOCKS (FALSE, has_references);
	MSBlockInfo **free_blocks_local = FREE_BLOCKS_LOCAL (FALSE, has_references);
	void *obj;

	if (free_blocks_local [size_index]) {
get_slot:
		obj = unlink_slot_from_free_list_uncontested (free_blocks_local, size_index);
	} else {
		MSBlockInfo *block;
get_block:
		block = free_blocks [size_index];
		if (!block) {
			if (G_UNLIKELY (!ms_alloc_block (size_index, FALSE, has_references)))
				return NULL;
			goto get_block;
		} else {
			MSBlockInfo *next_free = block->next_free;
			/*
			 * Once a block is removed from the main list, it cannot return on the list until
			 * all the workers are finished and sweep is starting. This means we don't need
			 * to account for ABA problems.
			 */
			if (SGEN_CAS_PTR ((volatile gpointer *)&free_blocks [size_index], next_free, block) != block)
				goto get_block;
			block->next_free = free_blocks_local [size_index];
			free_blocks_local [size_index] = block;

			goto get_slot;
		}
	}

	/* FIXME: assumes object layout */
	*(GCVTable*)obj = vtable;

	/* FIXME is it worth CAS-ing here */
	sgen_total_allocated_major += block_obj_sizes [size_index];

	return (GCObject *)obj;
}

/*
 * We're not freeing the block if it's empty.  We leave that work for
 * the next major collection.
 *
 * This is just called from the domain clearing code, which runs in a
 * single thread and has the GC lock, so we don't need an extra lock.
 */
static void
free_object (GCObject *obj, size_t size, gboolean pinned)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
	int word, bit;
	gboolean in_free_list;

	SGEN_ASSERT (9, sweep_state == SWEEP_STATE_SWEPT, "Should have waited for sweep to free objects.");

	ensure_can_access_block_free_list (block);
	SGEN_ASSERT (9, (pinned && block->pinned) || (!pinned && !block->pinned), "free-object pinning mixup object %p pinned %d block %p pinned %d", obj, pinned, block, block->pinned);
	SGEN_ASSERT (9, MS_OBJ_ALLOCED (obj, block), "object %p is already free", obj);
	MS_CALC_MARK_BIT (word, bit, obj);
	SGEN_ASSERT (9, !MS_MARK_BIT (block, word, bit), "object %p has mark bit set", obj);

	memset (obj, 0, size);

	in_free_list = !!block->free_list;
	*(void**)obj = block->free_list;
	block->free_list = (void**)obj;

	if (!in_free_list) {
		MSBlockInfo * volatile *free_blocks = FREE_BLOCKS (pinned, block->has_references);
		int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
		SGEN_ASSERT (9, !block->next_free, "block %p doesn't have a free-list of object but belongs to a free-list of blocks", block);
		add_free_block (free_blocks, size_index, block);
	}
}

static void
major_free_non_pinned_object (GCObject *obj, size_t size)
{
	free_object (obj, size, FALSE);
}

/* size is a multiple of SGEN_ALLOC_ALIGN */
static GCObject*
major_alloc_small_pinned_obj (GCVTable vtable, size_t size, gboolean has_references)
{
	void *res;

	res = alloc_obj (vtable, size, TRUE, has_references);
	 /*If we failed to alloc memory, we better try releasing memory
	  *as pinned alloc is requested by the runtime.
	  */
	 if (!res) {
		sgen_perform_collection (0, GENERATION_OLD, "pinned alloc failure", TRUE, TRUE);
		res = alloc_obj (vtable, size, TRUE, has_references);
	 }
	 return (GCObject *)res;
}

static void
free_pinned_object (GCObject *obj, size_t size)
{
	free_object (obj, size, TRUE);
}

/*
 * size is already rounded up and we hold the GC lock.
 */
static GCObject*
major_alloc_degraded (GCVTable vtable, size_t size)
{
	GCObject *obj;

	obj = alloc_obj (vtable, size, FALSE, SGEN_VTABLE_HAS_REFERENCES (vtable));
	if (G_LIKELY (obj)) {
		HEAVY_STAT (++stat_objects_alloced_degraded);
		HEAVY_STAT (stat_bytes_alloced_degraded += size);
	}
	return obj;
}

/*
 * obj is some object.  If it's not in the major heap (i.e. if it's in
 * the nursery or LOS), return FALSE.  Otherwise return whether it's
 * been marked or copied.
 */
static gboolean
major_is_object_live (GCObject *obj)
{
	MSBlockInfo *block;
	int word, bit;
	mword objsize;

	if (sgen_ptr_in_nursery (obj))
		return FALSE;

	objsize = SGEN_ALIGN_UP (sgen_safe_object_get_size (obj));

	/* LOS */
	if (objsize > SGEN_MAX_SMALL_OBJ_SIZE)
		return FALSE;

	/* now we know it's in a major block */
	block = MS_BLOCK_FOR_OBJ (obj);
	SGEN_ASSERT (9, !block->pinned, "block %p is pinned, BTW why is this bad?", block);
	MS_CALC_MARK_BIT (word, bit, obj);
	return MS_MARK_BIT (block, word, bit) ? TRUE : FALSE;
}

static gboolean
major_ptr_is_in_non_pinned_space (char *ptr, char **start)
{
	gboolean pinned;
	if (ptr_is_in_major_block (ptr, start, &pinned))
		return !pinned;
	return FALSE;
}

static gboolean
try_set_sweep_state (int new_, int expected)
{
	int old = SGEN_CAS (&sweep_state, new_, expected);
	return old == expected;
}

static void
set_sweep_state (int new_, int expected)
{
	gboolean success = try_set_sweep_state (new_, expected);
	SGEN_ASSERT (0, success, "Could not set sweep state.");
}

static gboolean ensure_block_is_checked_for_sweeping (guint32 block_index, gboolean wait, gboolean *have_checked);

static SgenThreadPoolJob * volatile sweep_job;
static SgenThreadPoolJob * volatile sweep_blocks_job;

static void
major_finish_sweep_checking (void)
{
	if (!concurrent_sweep)
		return;

	guint32 block_index;
	SgenThreadPoolJob *job;

 retry:
	switch (sweep_state) {
	case SWEEP_STATE_SWEPT:
	case SWEEP_STATE_NEED_SWEEPING:
		return;
	case SWEEP_STATE_SWEEPING:
		if (try_set_sweep_state (SWEEP_STATE_SWEEPING_AND_ITERATING, SWEEP_STATE_SWEEPING))
			break;
		goto retry;
	case SWEEP_STATE_SWEEPING_AND_ITERATING:
		SGEN_ASSERT (0, FALSE, "Is there another minor collection running?");
		goto retry;
	case SWEEP_STATE_COMPACTING:
		goto wait;
	default:
		SGEN_ASSERT (0, FALSE, "Invalid sweep state.");
		break;
	}

	/*
	 * We're running with the world stopped and the only other thread doing work is the
	 * sweep thread, which doesn't add blocks to the array, so we can safely access
	 * `next_slot`.
	 */
	for (block_index = 0; block_index < allocated_blocks.next_slot; ++block_index)
		ensure_block_is_checked_for_sweeping (block_index, FALSE, NULL);

	set_sweep_state (SWEEP_STATE_SWEEPING, SWEEP_STATE_SWEEPING_AND_ITERATING);

 wait:
	job = sweep_job;
	if (job)
		sgen_thread_pool_job_wait (sweep_pool_context, job);
	SGEN_ASSERT (0, !sweep_job, "Why did the sweep job not null itself?");
	SGEN_ASSERT (0, sweep_state == SWEEP_STATE_SWEPT, "How is the sweep job done but we're not swept?");
}

static void
major_iterate_objects (IterateObjectsFlags flags, IterateObjectCallbackFunc callback, void *data)
{
	gboolean sweep = flags & ITERATE_OBJECTS_SWEEP;
	gboolean non_pinned = flags & ITERATE_OBJECTS_NON_PINNED;
	gboolean pinned = flags & ITERATE_OBJECTS_PINNED;
	MSBlockInfo *block;

	/* No actual sweeping will take place if we are in the middle of a major collection. */
	major_finish_sweep_checking ();
	FOREACH_BLOCK_NO_LOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;
		int i;

		if (block->pinned && !pinned)
			continue;
		if (!block->pinned && !non_pinned)
			continue;
		if (sweep && lazy_sweep && !block_is_swept_or_marking (block)) {
			sweep_block (block);
			SGEN_ASSERT (6, block->state == BLOCK_STATE_SWEPT, "Block must be swept after sweeping");
		}

		for (i = 0; i < count; ++i) {
			void **obj = (void**) MS_BLOCK_OBJ (block, i);
			if (MS_OBJ_ALLOCED (obj, block))
				callback ((GCObject*)obj, block->obj_size, data);
		}
	} END_FOREACH_BLOCK_NO_LOCK;
}

static gboolean
major_is_valid_object (char *object)
{
	MSBlockInfo *block;

	FOREACH_BLOCK_NO_LOCK (block) {
		int idx;
		char *obj;

		if ((MS_BLOCK_FOR_BLOCK_INFO (block) > object) || ((MS_BLOCK_FOR_BLOCK_INFO (block) + ms_block_size) <= object))
			continue;

		idx = MS_BLOCK_OBJ_INDEX (object, block);
		obj = (char*)MS_BLOCK_OBJ (block, idx);
		if (obj != object)
			return FALSE;
		return MS_OBJ_ALLOCED (obj, block);
	} END_FOREACH_BLOCK_NO_LOCK;

	return FALSE;
}


static GCVTable
major_describe_pointer (char *ptr)
{
	MSBlockInfo *block;

	FOREACH_BLOCK_NO_LOCK (block) {
		int idx;
		char *obj;
		gboolean live;
		GCVTable vtable;
		int w, b;
		gboolean marked;

		if ((MS_BLOCK_FOR_BLOCK_INFO (block) > ptr) || ((MS_BLOCK_FOR_BLOCK_INFO (block) + ms_block_size) <= ptr))
			continue;

		SGEN_LOG (0, "major-ptr (block %p sz %d pin %d ref %d)\n",
			MS_BLOCK_FOR_BLOCK_INFO (block), block->obj_size, block->pinned, block->has_references);

		idx = MS_BLOCK_OBJ_INDEX (ptr, block);
		obj = (char*)MS_BLOCK_OBJ (block, idx);
		live = MS_OBJ_ALLOCED (obj, block);
		vtable = live ? SGEN_LOAD_VTABLE ((GCObject*)obj) : NULL;

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
				SGEN_LOG (0, "interior-ptr offset %" G_GSIZE_FORMAT "d", ptr - obj);
			else
				SGEN_LOG (0, "dead-interior-ptr offset %" G_GSIZE_FORMAT "d", ptr - obj);
		}

		SGEN_LOG (0, " marked %d)\n", marked ? 1 : 0);

		return vtable;
	} END_FOREACH_BLOCK_NO_LOCK;

	return NULL;
}

static void
major_check_scan_starts (void)
{
}

static void
major_dump_heap (FILE *heap_dump_file)
{
	MSBlockInfo *block;
	int *slots_available = g_newa (int, num_block_obj_sizes);
	int *slots_used = g_newa (int, num_block_obj_sizes);

	for (int i = 0; i < num_block_obj_sizes; ++i)
		slots_available [i] = slots_used [i] = 0;

	FOREACH_BLOCK_NO_LOCK (block) {
		int index = ms_find_block_obj_size_index (block->obj_size);
		int count = MS_BLOCK_FREE / block->obj_size;

		slots_available [index] += count;
		for (int i = 0; i < count; ++i) {
			if (MS_OBJ_ALLOCED (MS_BLOCK_OBJ (block, i), block))
				++slots_used [index];
		}
	} END_FOREACH_BLOCK_NO_LOCK;

	fprintf (heap_dump_file, "<occupancies>\n");
	for (int i = 0; i < num_block_obj_sizes; ++i) {
		fprintf (heap_dump_file, "<occupancy size=\"%d\" available=\"%d\" used=\"%d\" />\n",
				block_obj_sizes [i], slots_available [i], slots_used [i]);
	}
	fprintf (heap_dump_file, "</occupancies>\n");

	FOREACH_BLOCK_NO_LOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;;
		int start = -1;

		fprintf (heap_dump_file, "<section type=\"%s\" size=\"%" G_GSIZE_FORMAT "u\">\n", "old", (size_t)MS_BLOCK_FREE);

		for (int i = 0; i <= count; ++i) {
			if ((i < count) && MS_OBJ_ALLOCED (MS_BLOCK_OBJ (block, i), block)) {
				if (start < 0)
					start = i;
			} else {
				if (start >= 0) {
					sgen_dump_occupied ((char *)MS_BLOCK_OBJ (block, start), (char *)MS_BLOCK_OBJ (block, i), MS_BLOCK_FOR_BLOCK_INFO (block));
					start = -1;
				}
			}
		}

		fprintf (heap_dump_file, "</section>\n");
	} END_FOREACH_BLOCK_NO_LOCK;
}

static guint8*
get_cardtable_mod_union_for_block (MSBlockInfo *block, gboolean allocate)
{
	guint8 *mod_union = block->cardtable_mod_union;
	guint8 *other;
	if (mod_union)
		return mod_union;
	else if (!allocate)
		return NULL;
	mod_union = sgen_card_table_alloc_mod_union (MS_BLOCK_FOR_BLOCK_INFO (block), ms_block_size);
	other = (guint8 *)SGEN_CAS_PTR ((gpointer*)&block->cardtable_mod_union, mod_union, NULL);
	if (!other) {
		SGEN_ASSERT (0, block->cardtable_mod_union == mod_union, "Why did CAS not replace?");
		return mod_union;
	}
	sgen_card_table_free_mod_union (mod_union, MS_BLOCK_FOR_BLOCK_INFO (block), ms_block_size);
	return other;
}

static guint8*
major_get_cardtable_mod_union_for_reference (char *ptr)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (ptr);
	size_t offset = sgen_card_table_get_card_offset (ptr, (char*)sgen_card_table_align_pointer (MS_BLOCK_FOR_BLOCK_INFO (block)));
	guint8 *mod_union = get_cardtable_mod_union_for_block (block, TRUE);
	SGEN_ASSERT (0, mod_union, "FIXME: optionally allocate the mod union if it's not here and CAS it in.");
	return &mod_union [offset];
}

/*
 * Mark the mod-union card for `ptr`, which must be a reference within the object `obj`.
 */
static void
mark_mod_union_card (GCObject *obj, void **ptr, GCObject *value_obj)
{
	int type = sgen_obj_get_descriptor (obj) & DESC_TYPE_MASK;
	if (sgen_safe_object_is_small (obj, type)) {
		guint8 *card_byte = major_get_cardtable_mod_union_for_reference ((char*)ptr);
		SGEN_ASSERT (0, MS_BLOCK_FOR_OBJ (obj) == MS_BLOCK_FOR_OBJ (ptr), "How can an object and a reference inside it not be in the same block?");
		*card_byte = 1;
	} else {
		sgen_los_mark_mod_union_card (obj, ptr);
	}
	sgen_binary_protocol_mod_union_remset (obj, ptr, value_obj, SGEN_LOAD_VTABLE (value_obj));
}

static gboolean
major_block_is_evacuating (MSBlockInfo *block)
{
	if (evacuate_block_obj_sizes [block->obj_size_index] &&
			!block->has_pinned &&
			!block->is_to_space)
		return TRUE;
	return FALSE;
}

#define MS_MARK_OBJECT_AND_ENQUEUE(obj,desc,block,queue) do {		\
		int __word, __bit;					\
		MS_CALC_MARK_BIT (__word, __bit, (obj));		\
		SGEN_ASSERT (9, MS_OBJ_ALLOCED ((obj), (block)), "object %p not allocated", obj); \
		if (!MS_MARK_BIT ((block), __word, __bit)) {		\
			MS_SET_MARK_BIT ((block), __word, __bit);	\
			if (sgen_gc_descr_has_references (desc))			\
				GRAY_OBJECT_ENQUEUE_SERIAL ((queue), (obj), (desc)); \
			sgen_binary_protocol_mark ((obj), (gpointer)SGEN_LOAD_VTABLE ((obj)), sgen_safe_object_get_size ((obj))); \
			INC_NUM_MAJOR_OBJECTS_MARKED ();		\
		}							\
	} while (0)
#define MS_MARK_OBJECT_AND_ENQUEUE_PAR(obj,desc,block,queue) do {	\
		int __word, __bit;					\
		gboolean first;						\
		MS_CALC_MARK_BIT (__word, __bit, (obj));		\
		SGEN_ASSERT (9, MS_OBJ_ALLOCED ((obj), (block)), "object %p not allocated", obj); \
		MS_SET_MARK_BIT_PAR ((block), __word, __bit, first);	\
		if (first) {						\
			if (sgen_gc_descr_has_references (desc))	\
				GRAY_OBJECT_ENQUEUE_PARALLEL ((queue), (obj), (desc)); \
			sgen_binary_protocol_mark ((obj), (gpointer)SGEN_LOAD_VTABLE ((obj)), sgen_safe_object_get_size ((obj))); \
			INC_NUM_MAJOR_OBJECTS_MARKED ();		\
		}							\
	} while (0)



static void
pin_major_object (GCObject *obj, SgenGrayQueue *queue)
{
	MSBlockInfo *block;

	if (concurrent_mark)
		g_assert_not_reached ();

	block = MS_BLOCK_FOR_OBJ (obj);
	block->has_pinned = TRUE;
	MS_MARK_OBJECT_AND_ENQUEUE (obj, sgen_obj_get_descriptor (obj), block, queue);
}

#define COPY_OR_MARK_PARALLEL
#include "sgen-major-copy-object.h"

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

#define PREFETCH_CARDS		1	/* BOOL FASTENABLE */
#if !PREFETCH_CARDS
#undef PREFETCH_CARDS
#endif

/* gcc 4.2.1 from xcode4 crashes on sgen_card_table_get_card_address () when this is enabled */
#if defined(HOST_DARWIN)
#if MONO_GNUC_VERSION <= 40300
#undef PREFETCH_CARDS
#endif
#endif

#ifdef HEAVY_STATISTICS
static guint64 stat_optimized_copy;
static guint64 stat_optimized_copy_nursery;
static guint64 stat_optimized_copy_nursery_forwarded;
static guint64 stat_optimized_copy_nursery_pinned;
static guint64 stat_optimized_copy_major;
static guint64 stat_optimized_copy_major_small_fast;
static guint64 stat_optimized_copy_major_small_slow;
static guint64 stat_optimized_copy_major_large;
static guint64 stat_optimized_copy_major_forwarded;
static guint64 stat_optimized_copy_major_small_evacuate;
static guint64 stat_optimized_major_scan;
static guint64 stat_optimized_major_scan_no_refs;

static guint64 stat_drain_prefetch_fills;
static guint64 stat_drain_prefetch_fill_failures;
static guint64 stat_drain_loops;
#endif

#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_no_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_no_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_no_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

#define COPY_OR_MARK_PARALLEL
#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_par_no_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_par_no_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_par_no_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

#define COPY_OR_MARK_WITH_EVACUATION
#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_with_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_with_evacuation
#define SCAN_VTYPE_FUNCTION_NAME	major_scan_vtype_with_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_with_evacuation
#define SCAN_PTR_FIELD_FUNCTION_NAME	major_scan_ptr_field_with_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

#define COPY_OR_MARK_PARALLEL
#define COPY_OR_MARK_WITH_EVACUATION
#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_par_with_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_par_with_evacuation
#define SCAN_VTYPE_FUNCTION_NAME	major_scan_vtype_par_with_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_par_with_evacuation
#define SCAN_PTR_FIELD_FUNCTION_NAME	major_scan_ptr_field_par_with_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

#define COPY_OR_MARK_CONCURRENT
#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_concurrent_no_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_concurrent_no_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_concurrent_no_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

#define COPY_OR_MARK_PARALLEL
#define COPY_OR_MARK_CONCURRENT
#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_concurrent_par_no_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_concurrent_par_no_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_concurrent_par_no_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

#define COPY_OR_MARK_CONCURRENT_WITH_EVACUATION
#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_concurrent_with_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_concurrent_with_evacuation
#define SCAN_VTYPE_FUNCTION_NAME	major_scan_vtype_concurrent_with_evacuation
#define SCAN_PTR_FIELD_FUNCTION_NAME	major_scan_ptr_field_concurrent_with_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_concurrent_with_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

#define COPY_OR_MARK_PARALLEL
#define COPY_OR_MARK_CONCURRENT_WITH_EVACUATION
#define COPY_OR_MARK_FUNCTION_NAME	major_copy_or_mark_object_concurrent_par_with_evacuation
#define SCAN_OBJECT_FUNCTION_NAME	major_scan_object_concurrent_par_with_evacuation
#define SCAN_VTYPE_FUNCTION_NAME	major_scan_vtype_concurrent_par_with_evacuation
#define SCAN_PTR_FIELD_FUNCTION_NAME	major_scan_ptr_field_concurrent_par_with_evacuation
#define DRAIN_GRAY_STACK_FUNCTION_NAME	drain_gray_stack_concurrent_par_with_evacuation
#include "sgen-marksweep-drain-gray-stack.h"

static gboolean
major_is_evacuating (void)
{
	int i;
	for (i = 0; i < num_block_obj_sizes; ++i) {
		if (evacuate_block_obj_sizes [i]) {
			return TRUE;
		}
	}

	return FALSE;
}

static gboolean
drain_gray_stack (SgenGrayQueue *queue)
{
	if (major_is_evacuating ())
		return drain_gray_stack_with_evacuation (queue);
	else
		return drain_gray_stack_no_evacuation (queue);
}

static gboolean
drain_gray_stack_par (SgenGrayQueue *queue)
{
	if (major_is_evacuating ())
		return drain_gray_stack_par_with_evacuation (queue);
	else
		return drain_gray_stack_par_no_evacuation (queue);
}

static gboolean
drain_gray_stack_concurrent (SgenGrayQueue *queue)
{
	if (major_is_evacuating ())
		return drain_gray_stack_concurrent_with_evacuation (queue);
	else
		return drain_gray_stack_concurrent_no_evacuation (queue);
}

static gboolean
drain_gray_stack_concurrent_par (SgenGrayQueue *queue)
{
	if (major_is_evacuating ())
		return drain_gray_stack_concurrent_par_with_evacuation (queue);
	else
		return drain_gray_stack_concurrent_par_no_evacuation (queue);
}

static void
major_copy_or_mark_object_canonical (GCObject **ptr, SgenGrayQueue *queue)
{
	major_copy_or_mark_object_with_evacuation (ptr, *ptr, queue);
}

static void
major_copy_or_mark_object_concurrent_canonical (GCObject **ptr, SgenGrayQueue *queue)
{
	major_copy_or_mark_object_concurrent_with_evacuation (ptr, *ptr, queue);
}

static void
major_copy_or_mark_object_concurrent_par_canonical (GCObject **ptr, SgenGrayQueue *queue)
{
	major_copy_or_mark_object_concurrent_par_with_evacuation (ptr, *ptr, queue);
}

static void
major_copy_or_mark_object_concurrent_finish_canonical (GCObject **ptr, SgenGrayQueue *queue)
{
	major_copy_or_mark_object_with_evacuation (ptr, *ptr, queue);
}

static void
major_copy_or_mark_object_concurrent_par_finish_canonical (GCObject **ptr, SgenGrayQueue *queue)
{
	major_copy_or_mark_object_par_with_evacuation (ptr, *ptr, queue);
}

static void
mark_pinned_objects_in_block (MSBlockInfo *block, size_t first_entry, size_t last_entry, SgenGrayQueue *queue)
{
	void **entry, **end;
	int last_index = -1;

	if (first_entry == last_entry)
		return;

	entry = sgen_pinning_get_entry (first_entry);
	end = sgen_pinning_get_entry (last_entry);

	for (; entry < end; ++entry) {
		int index = MS_BLOCK_OBJ_INDEX (*entry, block);
		GCObject *obj;
		SGEN_ASSERT (9, index >= 0 && index < MS_BLOCK_FREE / block->obj_size, "invalid object %p index %d max-index %d", *entry, index, (int)(MS_BLOCK_FREE / block->obj_size));
		if (index == last_index)
			continue;
		obj = MS_BLOCK_OBJ (block, index);
		if (!MS_OBJ_ALLOCED (obj, block))
			continue;
		MS_MARK_OBJECT_AND_ENQUEUE (obj, sgen_obj_get_descriptor (obj), block, queue);
		sgen_pin_stats_register_object (obj, GENERATION_OLD);
		sgen_client_pinned_major_heap_object (obj);
		last_index = index;
	}

	/*
	 * There might have been potential pinning "pointers" into this block, but none of
	 * them pointed to occupied slots, in which case we don't have to pin the block.
	 */
	if (last_index >= 0)
		block->has_pinned = TRUE;
}

static void
sweep_block_for_size (MSBlockInfo *block, int count, int obj_size)
{
	int obj_index;
	void *obj = MS_BLOCK_OBJ_FOR_SIZE (block, 0, obj_size);

	for (obj_index = 0; obj_index < count; ++obj_index, obj = (void*)((mword)obj + obj_size)) {
		int word, bit;

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
				sgen_binary_protocol_empty (obj, obj_size);
				memset (obj, 0, obj_size);
			}
			*(void**)obj = block->free_list;
			block->free_list = (void **)obj;
		}
	}
}

static gboolean
try_set_block_state (MSBlockInfo *block, gint32 new_state, gint32 expected_state)
{
	gint32 old_state = SGEN_CAS (&block->state, new_state, expected_state);
	gboolean success = old_state == expected_state;
	if (success)
		sgen_binary_protocol_block_set_state (block, ms_block_size, old_state, new_state);
	return success;
}

static void
set_block_state (MSBlockInfo *block, gint32 new_state, gint32 expected_state)
{
	SGEN_ASSERT (6, block->state == expected_state, "Block state incorrect before set");
	block->state = new_state;
	sgen_binary_protocol_block_set_state (block, ms_block_size, expected_state, new_state);
}

/*
 * If `block` needs sweeping, sweep it and return TRUE.  Otherwise return FALSE.
 *
 * Sweeping means iterating through the block's slots and building the free-list from the
 * unmarked ones.  They will also be zeroed.  The mark bits will be reset.
 */
static gboolean
sweep_block (MSBlockInfo *block)
{
	int count;
	void *reversed = NULL;

 retry:
	switch (block->state) {
	case BLOCK_STATE_SWEPT:
		return FALSE;
	case BLOCK_STATE_MARKING:
	case BLOCK_STATE_CHECKING:
		SGEN_ASSERT (0, FALSE, "How did we get to sweep a block that's being marked or being checked?");
		goto retry;
	case BLOCK_STATE_SWEEPING:
		/* FIXME: Do this more elegantly */
		g_usleep (100);
		goto retry;
	case BLOCK_STATE_NEED_SWEEPING:
		if (!try_set_block_state (block, BLOCK_STATE_SWEEPING, BLOCK_STATE_NEED_SWEEPING))
			goto retry;
		break;
	default:
		SGEN_ASSERT (0, FALSE, "Illegal block state");
	}

	SGEN_ASSERT (6, block->state == BLOCK_STATE_SWEEPING, "How did we get here without setting state to sweeping?");

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
	memset (block->mark_words, 0, sizeof (guint32) * MS_NUM_MARK_WORDS);

	/* Reverse free list so that it's in address order */
	reversed = NULL;
	while (block->free_list) {
		void *next = *(void**)block->free_list;
		*(void**)block->free_list = reversed;
		reversed = block->free_list;
		block->free_list = (void **)next;
	}
	block->free_list = (void **)reversed;

	mono_memory_write_barrier ();

	set_block_state (block, BLOCK_STATE_SWEPT, BLOCK_STATE_SWEEPING);

	return TRUE;
}

static int
bitcount (mword d)
{
	int count = 0;

#if defined (__GNUC__) && !defined (HOST_WIN32)
// The builtins do work on Win32, but can cause a not worthwhile runtime dependency.
// See https://github.com/mono/mono/pull/14248.
	if (sizeof (mword) == 8)
		count += __builtin_popcountll (d);
	else
		count += __builtin_popcount (d);
#else
	while (d) {
		count ++;
		d &= (d - 1);
	}
#endif
	return count;
}

/* statistics for evacuation */
static size_t *sweep_slots_available;
static size_t *sweep_slots_used;
static size_t *sweep_num_blocks;

static volatile size_t num_major_sections_before_sweep;
static volatile size_t num_major_sections_freed_in_sweep;

static void
sgen_worker_clear_free_block_lists (WorkerData *worker)
{
	int i, j;

	if (!worker->free_block_lists)
		return;

	for (i = 0; i < MS_BLOCK_TYPE_MAX; i++) {
		for (j = 0; j < num_block_obj_sizes; j++) {
			((MSBlockInfo***) worker->free_block_lists) [i][j] = NULL;
		}
	}
}

static void
sgen_worker_clear_free_block_lists_evac (WorkerData *worker)
{
	int i, j;

	if (!worker->free_block_lists)
		return;

	for (i = 0; i < MS_BLOCK_TYPE_MAX; i++) {
		for (j = 0; j < num_block_obj_sizes; j++) {
			if (((MSBlockInfo***) worker->free_block_lists) [i][j])
				SGEN_ASSERT (0, !((MSBlockInfo***) worker->free_block_lists) [i][j]->next_free, "Why do we have linked free blocks on the workers");

			if (evacuate_block_obj_sizes [j])
				((MSBlockInfo***) worker->free_block_lists) [i][j] = NULL;
		}
	}
}

static void
sweep_start (void)
{
	int i;

	for (i = 0; i < num_block_obj_sizes; ++i)
		sweep_slots_available [i] = sweep_slots_used [i] = sweep_num_blocks [i] = 0;

	/* clear all the free lists */
	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i) {
		MSBlockInfo * volatile *free_blocks = free_block_lists [i];
		int j;
		for (j = 0; j < num_block_obj_sizes; ++j)
			free_blocks [j] = NULL;
	}

	sgen_workers_foreach (GENERATION_NURSERY, sgen_worker_clear_free_block_lists);
	sgen_workers_foreach (GENERATION_OLD, sgen_worker_clear_free_block_lists);

	compact_blocks = TRUE;
}

static void sweep_finish (void);

/*
 * If `wait` is TRUE and the block is currently being checked, this function will wait until
 * the checking has finished.
 *
 * Returns whether the block is still there.  If `wait` is FALSE, the return value will not
 * be correct, i.e. must not be used.
 */
static gboolean
ensure_block_is_checked_for_sweeping (guint32 block_index, gboolean wait, gboolean *have_checked)
{
	int count;
	gboolean have_live = FALSE;
	gboolean have_free = FALSE;
	int nused = 0;
	int block_state;
	int i;
	void *tagged_block;
	MSBlockInfo *block;
	volatile gpointer *block_slot = sgen_array_list_get_slot (&allocated_blocks, block_index);

	SGEN_ASSERT (6, sweep_in_progress (), "Why do we call this function if there's no sweep in progress?");

	if (have_checked)
		*have_checked = FALSE;

 retry:
	tagged_block = *(void * volatile *)block_slot;
	if (!tagged_block)
		return FALSE;

	if (BLOCK_IS_TAGGED_CHECKING (tagged_block)) {
		if (!wait)
			return FALSE;
		/* FIXME: do this more elegantly */
		g_usleep (100);
		goto retry;
	}

	if (SGEN_CAS_PTR (block_slot, BLOCK_TAG_CHECKING (tagged_block), tagged_block) != tagged_block)
		goto retry;

	block = BLOCK_UNTAG (tagged_block);
	block_state = block->state;

	if (!sweep_in_progress ()) {
		SGEN_ASSERT (6, block_state != BLOCK_STATE_SWEEPING && block_state != BLOCK_STATE_CHECKING, "Invalid block state.");
		if (!lazy_sweep)
			SGEN_ASSERT (6, block_state != BLOCK_STATE_NEED_SWEEPING, "Invalid block state.");
	}

	switch (block_state) {
	case BLOCK_STATE_SWEPT:
	case BLOCK_STATE_NEED_SWEEPING:
	case BLOCK_STATE_SWEEPING:
		goto done;
	case BLOCK_STATE_MARKING:
		break;
	case BLOCK_STATE_CHECKING:
		SGEN_ASSERT (0, FALSE, "We set the CHECKING bit - how can the stage be CHECKING?");
		goto done;
	default:
		SGEN_ASSERT (0, FALSE, "Illegal block state");
		break;
	}

	SGEN_ASSERT (6, block->state == BLOCK_STATE_MARKING, "When we sweep all blocks must start out marking.");
	set_block_state (block, BLOCK_STATE_CHECKING, BLOCK_STATE_MARKING);

	if (have_checked)
		*have_checked = TRUE;

	block->has_pinned = block->pinned;

	block->is_to_space = FALSE;

	count = MS_BLOCK_FREE / block->obj_size;

	if (block->cardtable_mod_union)
		memset (block->cardtable_mod_union, 0, CARDS_PER_BLOCK);

	/* Count marked objects in the block */
	for (i = 0; i < MS_NUM_MARK_WORDS; ++i)
		nused += bitcount (block->mark_words [i]);

	block->nused = nused;
	if (nused)
		have_live = TRUE;
	if (nused < count)
		have_free = TRUE;

	if (have_live) {
		int obj_size_index = block->obj_size_index;
		gboolean has_pinned = block->has_pinned;

		set_block_state (block, BLOCK_STATE_NEED_SWEEPING, BLOCK_STATE_CHECKING);

		/*
		 * FIXME: Go straight to SWEPT if there are no free slots.  We need
		 * to set the free slot list to NULL, though, and maybe update some
		 * statistics.
		 */
		if (!lazy_sweep)
			sweep_block (block);

		if (!has_pinned) {
			++sweep_num_blocks [obj_size_index];
			sweep_slots_used [obj_size_index] += nused;
			sweep_slots_available [obj_size_index] += count;
		}

		/*
		 * If there are free slots in the block, add
		 * the block to the corresponding free list.
		 */
		if (have_free) {
			MSBlockInfo * volatile *free_blocks = FREE_BLOCKS (block->pinned, block->has_references);

			if (!lazy_sweep)
				SGEN_ASSERT (6, block->free_list, "How do we not have a free list when there are free slots?");

			add_free_block (free_blocks, obj_size_index, block);
		}

		/* FIXME: Do we need the heap boundaries while we do nursery collections? */
		update_heap_boundaries_for_block (block);
	} else {
		/*
		 * Blocks without live objects are removed from the
		 * block list and freed.
		 */
		SGEN_ASSERT (6, block_index < allocated_blocks.next_slot, "How did the number of blocks shrink?");
		SGEN_ASSERT (6, *block_slot == BLOCK_TAG_CHECKING (tagged_block), "How did the block move?");

		sgen_binary_protocol_empty (MS_BLOCK_OBJ (block, 0), (char*)MS_BLOCK_OBJ (block, count) - (char*)MS_BLOCK_OBJ (block, 0));
		ms_free_block (block);

		SGEN_ATOMIC_ADD_P (num_major_sections, -1);
		SGEN_ATOMIC_ADD_P (num_major_sections_freed_in_sweep, 1);

		tagged_block = NULL;
	}

 done:
	/*
	 * Once the block is written back without the checking bit other threads are
	 * free to access it. Make sure the block state is visible before we write it
	 * back.
	 */
	mono_memory_write_barrier ();
	*block_slot = tagged_block;
	return !!tagged_block;
}

static void
sweep_blocks_job_func (void *thread_data_untyped, SgenThreadPoolJob *job)
{
	volatile gpointer *slot;
	MSBlockInfo *bl;

	SGEN_ARRAY_LIST_FOREACH_SLOT (&allocated_blocks, slot) {
		bl = BLOCK_UNTAG (*slot);
		if (bl)
			sweep_block (bl);
	} SGEN_ARRAY_LIST_END_FOREACH_SLOT;

	mono_memory_write_barrier ();

	sweep_blocks_job = NULL;
}

static void
sweep_job_func (void *thread_data_untyped, SgenThreadPoolJob *job)
{
	guint32 block_index;
	guint32 num_blocks = (guint32)num_major_sections_before_sweep;

	SGEN_ASSERT (0, sweep_in_progress (), "Sweep thread called with wrong state");
	SGEN_ASSERT (0, num_blocks <= allocated_blocks.next_slot, "How did we lose blocks?");

	/*
	 * We traverse the block array from high to low.  Nursery collections will have to
	 * cooperate with the sweep thread to finish sweeping, and they will traverse from
	 * low to high, to avoid constantly colliding on the same blocks.
	 */
	for (block_index = allocated_blocks.next_slot; block_index-- > 0;) {
		ensure_block_is_checked_for_sweeping (block_index, TRUE, NULL);
	}

	while (!try_set_sweep_state (SWEEP_STATE_COMPACTING, SWEEP_STATE_SWEEPING)) {
		/*
		 * The main GC thread is currently iterating over the block array to help us
		 * finish the sweep.  We have already finished, but we don't want to mess up
		 * that iteration, so we just wait for it.
		 */
		g_usleep (100);
	}

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
MONO_DISABLE_WARNING(4189) /* local variable is initialized but not referenced */
	if (SGEN_MAX_ASSERT_LEVEL >= 6) {
		for (block_index = num_blocks; block_index < allocated_blocks.next_slot; ++block_index) {
			MSBlockInfo *block = BLOCK_UNTAG (*sgen_array_list_get_slot (&allocated_blocks, block_index));
			SGEN_ASSERT (6, block && block->state == BLOCK_STATE_SWEPT, "How did a new block to be swept get added while swept?");
		}
	}
MONO_RESTORE_WARNING
MONO_RESTORE_WARNING

	/*
	 * Concurrently sweep all the blocks to reduce workload during minor
	 * pauses where we need certain blocks to be swept. At the start of
	 * the next major we need all blocks to be swept anyway.
	 */
	if (concurrent_sweep && lazy_sweep) {
		sweep_blocks_job = sgen_thread_pool_job_alloc ("sweep_blocks", sweep_blocks_job_func, sizeof (SgenThreadPoolJob));
		sgen_thread_pool_job_enqueue (sweep_pool_context, sweep_blocks_job);
	}

	sweep_finish ();

	sweep_job = NULL;
}

static void
sweep_finish (void)
{
	mword used_slots_size = 0;
	int i;

	for (i = 0; i < num_block_obj_sizes; ++i) {
		float usage = (float)sweep_slots_used [i] / (float)sweep_slots_available [i];
		if (sweep_num_blocks [i] > 5 && usage < evacuation_threshold) {
			evacuate_block_obj_sizes [i] = TRUE;
			/*
			g_print ("slot size %d - %d of %d used\n",
					block_obj_sizes [i], slots_used [i], slots_available [i]);
			*/
		} else {
			evacuate_block_obj_sizes [i] = FALSE;
		}

		used_slots_size += sweep_slots_used [i] * block_obj_sizes [i];
	}

	sgen_memgov_major_post_sweep (used_slots_size);

	set_sweep_state (SWEEP_STATE_SWEPT, SWEEP_STATE_COMPACTING);
	if (concurrent_sweep)
		sgen_binary_protocol_concurrent_sweep_end (sgen_timestamp ());
}

static void
major_sweep (void)
{
	set_sweep_state (SWEEP_STATE_SWEEPING, SWEEP_STATE_NEED_SWEEPING);

	sweep_start ();

	num_major_sections_before_sweep = num_major_sections;
	num_major_sections_freed_in_sweep = 0;

	SGEN_ASSERT (0, !sweep_job, "We haven't finished the last sweep?");
	if (concurrent_sweep) {
		sweep_job = sgen_thread_pool_job_alloc ("sweep", sweep_job_func, sizeof (SgenThreadPoolJob));
		sgen_thread_pool_job_enqueue (sweep_pool_context, sweep_job);
	} else {
		sweep_job_func (NULL, NULL);
	}
}

static gboolean
major_have_swept (void)
{
	return sweep_state == SWEEP_STATE_SWEPT;
}

static int count_pinned_ref;
static int count_pinned_nonref;
static int count_nonpinned_ref;
static int count_nonpinned_nonref;

static void
count_nonpinned_callback (GCObject *obj, size_t size, void *data)
{
	GCVTable vtable = SGEN_LOAD_VTABLE (obj);

	if (SGEN_VTABLE_HAS_REFERENCES (vtable))
		++count_nonpinned_ref;
	else
		++count_nonpinned_nonref;
}

static void
count_pinned_callback (GCObject *obj, size_t size, void *data)
{
	GCVTable vtable = SGEN_LOAD_VTABLE (obj);

	if (SGEN_VTABLE_HAS_REFERENCES (vtable))
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

	major_iterate_objects (ITERATE_OBJECTS_SWEEP_NON_PINNED, count_nonpinned_callback, NULL);
	major_iterate_objects (ITERATE_OBJECTS_SWEEP_PINNED, count_pinned_callback, NULL);

	total = count_pinned_nonref + count_nonpinned_nonref + count_pinned_ref + count_nonpinned_ref;

	g_print ("ref: %d pinned %d non-pinned   non-ref: %d pinned %d non-pinned  --  %.1f\n",
			count_pinned_ref, count_nonpinned_ref,
			count_pinned_nonref, count_nonpinned_nonref,
			(count_pinned_nonref + count_nonpinned_nonref) * 100.0 / total);
}

static int
ms_calculate_block_obj_sizes (double factor, int *arr)
{
	double target_size;
	int num_sizes = 0;
	int last_size = 0;

	/*
	 * Have every possible slot size starting with the minimal
	 * object size up to and including four times that size.  Then
	 * proceed by increasing geometrically with the given factor.
	 */

	for (int size = SGEN_CLIENT_MINIMUM_OBJECT_SIZE; size <= 4 * SGEN_CLIENT_MINIMUM_OBJECT_SIZE; size += SGEN_ALLOC_ALIGN) {
		if (arr)
			arr [num_sizes] = size;
		++num_sizes;
		last_size = size;
	}
	target_size = (double)last_size;

	do {
		int target_count = (int)floor (MS_BLOCK_FREE / target_size);
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
static mword old_num_major_sections;

static void
major_start_nursery_collection (void)
{
#ifdef MARKSWEEP_CONSISTENCY_CHECK
	consistency_check ();
#endif

	old_num_major_sections = num_major_sections;

	/* Compact the block list if it hasn't been compacted in a while and nobody is using it */
	if (compact_blocks && !sweep_in_progress () && !sweep_blocks_job && !sgen_get_concurrent_collection_in_progress ()) {
		/*
		 * We support null elements in the array but do regular compaction to avoid
		 * excessive traversal of the array and to facilitate splitting into well
		 * balanced sections for parallel modes. We compact as soon as possible after
		 * sweep.
		 */
		sgen_array_list_remove_nulls (&allocated_blocks);
		compact_blocks = FALSE;
	}
}

static void
major_finish_nursery_collection (void)
{
#ifdef MARKSWEEP_CONSISTENCY_CHECK
	consistency_check ();
#endif
}

static int
block_usage_comparer (const void *bl1, const void *bl2)
{
	const gint16 nused1 = (*(MSBlockInfo**)bl1)->nused;
	const gint16 nused2 = (*(MSBlockInfo**)bl2)->nused;

	return nused2 - nused1;
}

static void
sgen_evacuation_freelist_blocks (MSBlockInfo * volatile *block_list, int size_index)
{
	MSBlockInfo **evacuated_blocks;
	size_t index = 0, count, num_blocks = 0, num_used = 0;
	MSBlockInfo *info;
	MSBlockInfo * volatile *prev;

	for (info = *block_list; info != NULL; info = info->next_free) {
		num_blocks++;
		num_used += info->nused;
	}

	/*
	 * We have a set of blocks in the freelist which will be evacuated. Instead
	 * of evacuating all of the blocks into new ones, we traverse the freelist
	 * sorting it by the number of occupied slots, evacuating the objects from
	 * blocks with fewer used slots into fuller blocks.
	 *
	 * The number of used slots is set at the end of the previous sweep. Since
	 * we sequentially unlink slots from blocks, except for the head of the
	 * freelist, for blocks on the freelist, the number of used slots is the same
	 * as at the end of the previous sweep.
	 */
	evacuated_blocks = (MSBlockInfo**)sgen_alloc_internal_dynamic (sizeof (MSBlockInfo*) * num_blocks, INTERNAL_MEM_TEMPORARY, TRUE);

	for (info = *block_list; info != NULL; info = info->next_free) {
		evacuated_blocks [index++] = info;
	}

	SGEN_ASSERT (0, num_blocks == index, "Why did the freelist change ?");

	sgen_qsort (evacuated_blocks, num_blocks, sizeof (gpointer), block_usage_comparer);

	/*
	 * Form a new freelist with the fullest blocks. These blocks will also be
	 * marked as to_space so we don't evacuate from them.
	 */
	count = MS_BLOCK_FREE / block_obj_sizes [size_index];
	prev = block_list;
	for (index = 0; index < (num_used + count - 1) / count; index++) {
		SGEN_ASSERT (0, index < num_blocks, "Why do we need more blocks for compaction than we already had ?");
		info = evacuated_blocks [index];
		info->is_to_space = TRUE;
		*prev = info;
		prev = &info->next_free;
	}
	*prev = NULL;

	sgen_free_internal_dynamic (evacuated_blocks, sizeof (MSBlockInfo*) * num_blocks, INTERNAL_MEM_TEMPORARY);
}

static void
major_start_major_collection (void)
{
	MSBlockInfo *block;
	int i;

	major_finish_sweep_checking ();

	/*
	 * Clear the free lists for block sizes where we do evacuation.  For those block
	 * sizes we will have to allocate new blocks.
	 */
	for (i = 0; i < num_block_obj_sizes; ++i) {
		if (!evacuate_block_obj_sizes [i])
			continue;

		sgen_binary_protocol_evacuating_blocks (block_obj_sizes [i]);

		sgen_evacuation_freelist_blocks (&free_block_lists [0][i], i);
		sgen_evacuation_freelist_blocks (&free_block_lists [MS_BLOCK_FLAG_REFS][i], i);
	}

	/* We expect workers to have very few blocks on the freelist, just evacuate them */
	sgen_workers_foreach (GENERATION_NURSERY, sgen_worker_clear_free_block_lists_evac);
	sgen_workers_foreach (GENERATION_OLD, sgen_worker_clear_free_block_lists_evac);

	if (lazy_sweep && concurrent_sweep) {
		/*
		 * sweep_blocks_job is created before sweep_finish, which we wait for above
		 * (major_finish_sweep_checking). After the end of sweep, if we don't have
		 * sweep_blocks_job set, it means that it has already been run.
		 */
		SgenThreadPoolJob *job = sweep_blocks_job;
		if (job)
			sgen_thread_pool_job_wait (sweep_pool_context, job);
	}

	if (lazy_sweep && !concurrent_sweep)
		sgen_binary_protocol_sweep_begin (GENERATION_OLD, TRUE);
	/* Sweep all unswept blocks and set them to MARKING */
	FOREACH_BLOCK_NO_LOCK (block) {
		if (lazy_sweep && !concurrent_sweep)
			sweep_block (block);
		SGEN_ASSERT (0, block->state == BLOCK_STATE_SWEPT, "All blocks must be swept when we're pinning.");
		set_block_state (block, BLOCK_STATE_MARKING, BLOCK_STATE_SWEPT);
		/*
		 * Swept blocks that have a null free_list are full. Evacuation is not
		 * effective on these blocks since we expect them to have high usage anyway,
		 * given that the survival rate for majors is relatively high.
		 */
		if (evacuate_block_obj_sizes [block->obj_size_index] && !block->free_list)
			block->is_to_space = TRUE;
	} END_FOREACH_BLOCK_NO_LOCK;
	if (lazy_sweep && !concurrent_sweep)
		sgen_binary_protocol_sweep_end (GENERATION_OLD, TRUE);

	set_sweep_state (SWEEP_STATE_NEED_SWEEPING, SWEEP_STATE_SWEPT);
}

static void
major_finish_major_collection (ScannedObjectCounts *counts)
{
#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	if (sgen_binary_protocol_is_enabled ()) {
		counts->num_scanned_objects = scanned_objects_list.next_slot;

		sgen_pointer_queue_sort_uniq (&scanned_objects_list);
		counts->num_unique_scanned_objects = scanned_objects_list.next_slot;

		sgen_pointer_queue_clear (&scanned_objects_list);
	}
#endif
}

static int
compare_pointers (const void *va, const void *vb) {
	char *a = *(char**)va, *b = *(char**)vb;
	if (a < b)
		return -1;
	if (a > b)
		return 1;
	return 0;
}

/*
 * This is called with sweep completed and the world stopped.
 */
static void
major_free_swept_blocks (size_t section_reserve)
{
	SGEN_ASSERT (0, sweep_state == SWEEP_STATE_SWEPT, "Sweeping must have finished before freeing blocks");

#if defined(HOST_WIN32) || defined(HOST_ORBIS) || defined (HOST_WASM)
		/*
		 * sgen_free_os_memory () asserts in mono_vfree () because windows doesn't like freeing the middle of
		 * a VirtualAlloc ()-ed block.
		 */
		return;
#endif

	{
		size_t i, num_empty_blocks_orig, num_blocks, arr_length;
		void *block;
		void **empty_block_arr;
		void **rebuild_next;

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

		sgen_qsort (empty_block_arr, num_empty_blocks, sizeof (void*), compare_pointers);

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
				block = empty_block_arr [i];
				SGEN_ASSERT (6, block, "we're not shifting correctly");
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

				SGEN_ASSERT (6, first >= 0 && d > first, "algorithm is wrong");

				if ((char*)block != ((char*)empty_block_arr [d-1]) + ms_block_size) {
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
					sgen_free_os_memory (empty_block_arr [first], ms_block_size * num_blocks, SGEN_ALLOC_HEAP, MONO_MEM_ACCOUNT_SGEN_MARKSWEEP);
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

			SGEN_ASSERT (6, dest <= i && dest <= arr_length, "array length is off");
			arr_length = dest;
			SGEN_ASSERT (6, arr_length == num_empty_blocks, "array length is off");

			num_blocks >>= 1;
		}

		/* rebuild empty_blocks free list */
		rebuild_next = (void**)&empty_blocks;
		for (i = 0; i < arr_length; ++i) {
			block = empty_block_arr [i];
			SGEN_ASSERT (6, block, "we're missing blocks");
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

	while (num_empty_blocks > section_reserve) {
		void *next = *(void**)empty_blocks;
		sgen_free_os_memory (empty_blocks, ms_block_size, SGEN_ALLOC_HEAP, MONO_MEM_ACCOUNT_SGEN_MARKSWEEP);
		empty_blocks = next;
		/*
		 * Needs not be atomic because this is running
		 * single-threaded.
		 */
		--num_empty_blocks;

		++stat_major_blocks_freed;
		++stat_major_blocks_freed_individual;
	}
}

static void
major_pin_objects (SgenGrayQueue *queue)
{
	MSBlockInfo *block;

	FOREACH_BLOCK_NO_LOCK (block) {
		size_t first_entry, last_entry;
		SGEN_ASSERT (6, block_is_swept_or_marking (block), "All blocks must be swept when we're pinning.");
		sgen_find_optimized_pin_queue_area (MS_BLOCK_FOR_BLOCK_INFO (block) + MS_BLOCK_SKIP, MS_BLOCK_FOR_BLOCK_INFO (block) + ms_block_size,
				&first_entry, &last_entry);
		mark_pinned_objects_in_block (block, first_entry, last_entry, queue);
	} END_FOREACH_BLOCK_NO_LOCK;
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

	/*
	 * We're holding the GC lock, but the sweep thread might be running.  Make sure it's
	 * finished, then we can iterate over the block array.
	 */
	major_finish_sweep_checking ();

	FOREACH_BLOCK_NO_LOCK (block) {
		int count = MS_BLOCK_FREE / block->obj_size;
		void **iter;
		size += count * block->obj_size;
		for (iter = block->free_list; iter; iter = (void**)*iter)
			size -= block->obj_size;
	} END_FOREACH_BLOCK_NO_LOCK;

	return size;
}

/* FIXME: return number of bytes, not of sections */
static size_t
get_num_major_sections (void)
{
	return num_major_sections;
}

/*
 * Returns the number of bytes in blocks that were present when the last sweep was
 * initiated, and were not freed during the sweep.  They are the basis for calculating the
 * allowance.
 */
static size_t
get_bytes_survived_last_sweep (void)
{
	SGEN_ASSERT (0, sweep_state == SWEEP_STATE_SWEPT, "Can only query unswept sections after sweep");
	return (num_major_sections_before_sweep - num_major_sections_freed_in_sweep) * ms_block_size;
}

static gboolean
major_handle_gc_param (const char *opt)
{
	if (g_str_has_prefix (opt, "evacuation-threshold=")) {
		const char *arg = strchr (opt, '=') + 1;
		int percentage = atoi (arg);
		if (percentage < 0 || percentage > 100) {
			fprintf (stderr, "evacuation-threshold must be an integer in the range 0-100.\n");
			exit (1);
		}
		evacuation_threshold = (float)percentage / 100.0f;
		return TRUE;
	} else if (!strcmp (opt, "lazy-sweep")) {
		lazy_sweep = TRUE;
		return TRUE;
	} else if (!strcmp (opt, "no-lazy-sweep")) {
		lazy_sweep = FALSE;
		return TRUE;
	} else if (!strcmp (opt, "concurrent-sweep")) {
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
		concurrent_sweep = TRUE;
#else
		g_error ("Sgen was built with concurrent collector disabled");
#endif
		return TRUE;
	} else if (!strcmp (opt, "no-concurrent-sweep")) {
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
		concurrent_sweep = FALSE;
#endif
		return TRUE;
	}

	return FALSE;
}

static void
major_print_gc_param_usage (void)
{
	fprintf (stderr,
			""
			"  evacuation-threshold=P (where P is a percentage, an integer in 0-100)\n"
			"  (no-)lazy-sweep\n"
			"  (no-)concurrent-sweep\n"
			);
}

static void
get_block_range_for_job (int job_index, int job_split_count, int block_count, int *start, int *end)
{
	/*
	* The last_block's index is at least (num_major_sections - 1) since we
	* can have nulls in the allocated_blocks list. The last worker will
	* scan the left-overs of the list. We expect few null entries in the
	* allocated_blocks list, therefore using num_major_sections for computing
	* block_count shouldn't affect work distribution.
	*/
	*start = block_count * job_index;
	if (job_index == job_split_count - 1)
		*end = allocated_blocks.next_slot;
	else
		*end = block_count * (job_index + 1);
}

/*
 * This callback is used to clear cards, move cards to the shadow table and do counting.
 */
static void
major_iterate_block_ranges (sgen_cardtable_block_callback callback)
{
	MSBlockInfo *block;
	gboolean has_references;

	FOREACH_BLOCK_HAS_REFERENCES_NO_LOCK (block, has_references) {
		if (has_references)
			callback ((mword)MS_BLOCK_FOR_BLOCK_INFO (block), ms_block_size);
	} END_FOREACH_BLOCK_NO_LOCK;
}

static void
major_iterate_block_ranges_in_parallel (sgen_cardtable_block_callback callback, int job_index, int job_split_count, int block_count)
{
	MSBlockInfo *block;
	gboolean has_references;
	int first_block, last_block, index;

	get_block_range_for_job (job_index, job_split_count, block_count, &first_block, &last_block);

	FOREACH_BLOCK_RANGE_HAS_REFERENCES_NO_LOCK (block, first_block, last_block, index, has_references) {
		if (has_references)
			callback ((mword)MS_BLOCK_FOR_BLOCK_INFO (block), ms_block_size);
	} END_FOREACH_BLOCK_RANGE_NO_LOCK;
}

static void
major_iterate_live_block_ranges (sgen_cardtable_block_callback callback)
{
	MSBlockInfo *block;
	gboolean has_references;

	major_finish_sweep_checking ();
	FOREACH_BLOCK_HAS_REFERENCES_NO_LOCK (block, has_references) {
		if (has_references)
			callback ((mword)MS_BLOCK_FOR_BLOCK_INFO (block), ms_block_size);
	} END_FOREACH_BLOCK_NO_LOCK;
}

#ifdef HEAVY_STATISTICS
extern guint64 marked_cards;
extern guint64 scanned_cards;
extern guint64 scanned_objects;
extern guint64 remarked_cards;
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
	mword card = 0;
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

#define MS_BLOCK_OBJ_INDEX_FAST(o,b,os)	(((char*)(o) - ((b) + MS_BLOCK_SKIP)) / (os))
#define MS_BLOCK_OBJ_FAST(b,os,i)			((b) + MS_BLOCK_SKIP + (os) * (i))
#define MS_OBJ_ALLOCED_FAST(o,b)		(*(void**)(o) && (*(char**)(o) < (b) || *(char**)(o) >= (b) + ms_block_size))

static void
scan_card_table_for_block (MSBlockInfo *block, CardTableScanType scan_type, ScanCopyContext ctx)
{
	SgenGrayQueue *queue = ctx.queue;
	ScanObjectFunc scan_func = ctx.ops->scan_object;
	/*
	 * FIXME: On systems with very large pages, we allocate fairly large
	 * arrays on the stack here. This shouldn't be a problem once block
	 * size is no longer required to be a multiple of the system page size.
	 */
#ifndef SGEN_HAVE_OVERLAPPING_CARDS
	guint8 *cards_copy = g_newa (guint8, CARDS_PER_BLOCK);
#endif
	guint8 *cards_preclean = g_newa (guint8, CARDS_PER_BLOCK);
	gboolean small_objects;
	int block_obj_size;
	char *block_start;
	guint8 *card_data, *card_base;
	guint8 *card_data_end;
	char *scan_front = NULL;

	/* The concurrent mark doesn't enter evacuating blocks */
	if (scan_type == CARDTABLE_SCAN_MOD_UNION_PRECLEAN && major_block_is_evacuating (block))
		return;

	block_obj_size = block->obj_size;
	small_objects = block_obj_size < CARD_SIZE_IN_BYTES;

	block_start = MS_BLOCK_FOR_BLOCK_INFO (block);

	/*
	 * This is safe in face of card aliasing for the following reason:
	 *
	 * Major blocks are 16k aligned, or 32 cards aligned.
	 * Cards aliasing happens in powers of two, so as long as major blocks are aligned to their
	 * sizes, they won't overflow the cardtable overlap modulus.
	 */
	if (scan_type & CARDTABLE_SCAN_MOD_UNION) {
		card_data = card_base = block->cardtable_mod_union;
		/*
		 * This happens when the nursery collection that precedes finishing
		 * the concurrent collection allocates new major blocks.
		 */
		if (!card_data)
			return;

		if (scan_type == CARDTABLE_SCAN_MOD_UNION_PRECLEAN) {
			sgen_card_table_preclean_mod_union (card_data, cards_preclean, CARDS_PER_BLOCK);
			card_data = card_base = cards_preclean;
		}
	} else {
#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		card_data = card_base = sgen_card_table_get_card_scan_address ((mword)block_start);
#else
		if (!sgen_card_table_get_card_data (cards_copy, (mword)block_start, CARDS_PER_BLOCK))
			return;
		card_data = card_base = cards_copy;
#endif
	}
	card_data_end = card_data + CARDS_PER_BLOCK;

	card_data += MS_BLOCK_SKIP >> CARD_BITS;

	card_data = initial_skip_card (card_data);
	while (card_data < card_data_end) {
		size_t card_index, first_object_index;
		char *start;
		char *end;
		char *first_obj, *obj;

		HEAVY_STAT (++scanned_cards);

		if (!*card_data) {
			++card_data;
			continue;
		}

		card_index = card_data - card_base;
		start = (char*)(block_start + card_index * CARD_SIZE_IN_BYTES);
		end = start + CARD_SIZE_IN_BYTES;

		if (!block_is_swept_or_marking (block))
			sweep_block (block);

		HEAVY_STAT (++marked_cards);

		if (small_objects)
			sgen_card_table_prepare_card_for_scanning (card_data);

		/*
		 * If the card we're looking at starts at or in the block header, we
		 * must start at the first object in the block, without calculating
		 * the index of the object we're hypothetically starting at, because
		 * it would be negative.
		 */
		if (card_index <= (MS_BLOCK_SKIP >> CARD_BITS))
			first_object_index = 0;
		else
			first_object_index = MS_BLOCK_OBJ_INDEX_FAST (start, block_start, block_obj_size);

		obj = first_obj = (char*)MS_BLOCK_OBJ_FAST (block_start, block_obj_size, first_object_index);

		sgen_binary_protocol_card_scan (first_obj, end - first_obj);

		while (obj < end) {
			if (obj < scan_front || !MS_OBJ_ALLOCED_FAST (obj, block_start))
				goto next_object;

			if (scan_type & CARDTABLE_SCAN_MOD_UNION) {
				/* FIXME: do this more efficiently */
				int w, b;
				MS_CALC_MARK_BIT (w, b, obj);
				if (!MS_MARK_BIT (block, w, b))
					goto next_object;
			}

			GCObject *object;
			object = (GCObject*)obj;

			if (small_objects) {
				HEAVY_STAT (++scanned_objects);
				scan_func (object, sgen_obj_get_descriptor_safe (object), queue);
			} else {
				size_t offset = sgen_card_table_get_card_offset (obj, block_start);
				sgen_cardtable_scan_object (object, block_obj_size, card_base + offset, ctx);
			}
		next_object:
			obj += block_obj_size;
			g_assert (scan_front <= obj);
			scan_front = obj;
		}

		HEAVY_STAT (if (*card_data) ++remarked_cards);

		if (small_objects)
			++card_data;
		else
			card_data = card_base + sgen_card_table_get_card_offset (obj, block_start);
	}
}

static void
major_scan_card_table (CardTableScanType scan_type, ScanCopyContext ctx, int job_index, int job_split_count, int block_count)
{
	MSBlockInfo *block;
	gboolean has_references, was_sweeping, skip_scan;
	int first_block, last_block, index;

	get_block_range_for_job (job_index, job_split_count, block_count, &first_block, &last_block);

	if (!concurrent_mark)
		g_assert (scan_type == CARDTABLE_SCAN_GLOBAL);

	if (scan_type != CARDTABLE_SCAN_GLOBAL)
		SGEN_ASSERT (0, !sweep_in_progress (), "Sweep should be finished when we scan mod union card table");
	was_sweeping = sweep_in_progress ();

	sgen_binary_protocol_major_card_table_scan_start (sgen_timestamp (), scan_type & CARDTABLE_SCAN_MOD_UNION);
	FOREACH_BLOCK_RANGE_HAS_REFERENCES_NO_LOCK (block, first_block, last_block, index, has_references) {
#ifdef PREFETCH_CARDS
MONO_DISABLE_WARNING(4189) /* local variable is initialized but not referenced */
		int prefetch_index = index + 6;
		if (prefetch_index < allocated_blocks.next_slot) {
			MSBlockInfo *prefetch_block = BLOCK_UNTAG (*sgen_array_list_get_slot (&allocated_blocks, prefetch_index));
			PREFETCH_READ (prefetch_block);
			if (scan_type == CARDTABLE_SCAN_GLOBAL) {
				guint8 *prefetch_cards = sgen_card_table_get_card_scan_address ((mword)MS_BLOCK_FOR_BLOCK_INFO (prefetch_block));
				PREFETCH_WRITE (prefetch_cards);
				PREFETCH_WRITE (prefetch_cards + 32);
			}
		}
MONO_RESTORE_WARNING
#endif
		if (!has_references)
			continue;
		skip_scan = FALSE;

		if (scan_type == CARDTABLE_SCAN_GLOBAL) {
			gpointer *card_start = (gpointer*) sgen_card_table_get_card_scan_address ((mword)MS_BLOCK_FOR_BLOCK_INFO (block));
			gboolean has_dirty_cards = FALSE;
			int i;
			for (i = 0; i < CARDS_PER_BLOCK / sizeof(gpointer); i++) {
				if (card_start [i]) {
					has_dirty_cards = TRUE;
					break;
				}
			}
			if (!has_dirty_cards) {
				skip_scan = TRUE;
			} else {
				/*
				 * After the start of the concurrent collections, blocks change state
				 * to marking. We should not sweep it in that case. We can't race with
				 * sweep start since we are in a nursery collection. Also avoid CAS-ing
				 */
				if (sweep_in_progress ()) {
					skip_scan = !ensure_block_is_checked_for_sweeping (index, TRUE, NULL);
				} else if (was_sweeping) {
					/* Recheck in case sweep finished after dereferencing the slot */
					skip_scan = *sgen_array_list_get_slot (&allocated_blocks, index) == 0;
				}
			}
		}
		if (!skip_scan)
			scan_card_table_for_block (block, scan_type, ctx);
	} END_FOREACH_BLOCK_RANGE_NO_LOCK;
	sgen_binary_protocol_major_card_table_scan_end (sgen_timestamp (), scan_type & CARDTABLE_SCAN_MOD_UNION);
}

static void
major_count_cards (long long *num_total_cards, long long *num_marked_cards)
{
	MSBlockInfo *block;
	gboolean has_references;
	long long total_cards = 0;
	long long marked_cards = 0;

	if (sweep_in_progress ()) {
		*num_total_cards = -1;
		*num_marked_cards = -1;
		return;
	}

	FOREACH_BLOCK_HAS_REFERENCES_NO_LOCK (block, has_references) {
		guint8 *cards = sgen_card_table_get_card_scan_address ((mword) MS_BLOCK_FOR_BLOCK_INFO (block));
		int i;

		if (!has_references)
			continue;

		total_cards += CARDS_PER_BLOCK;
		for (i = 0; i < CARDS_PER_BLOCK; ++i) {
			if (cards [i])
				++marked_cards;
		}
	} END_FOREACH_BLOCK_NO_LOCK;

	*num_total_cards = total_cards;
	*num_marked_cards = marked_cards;
}

static void
update_cardtable_mod_union (void)
{
	MSBlockInfo *block;

	FOREACH_BLOCK_NO_LOCK (block) {
		gpointer *card_start = (gpointer*) sgen_card_table_get_card_address ((mword)MS_BLOCK_FOR_BLOCK_INFO (block));
		gboolean has_dirty_cards = FALSE;
		int i;
		for (i = 0; i < CARDS_PER_BLOCK / sizeof(gpointer); i++) {
			if (card_start [i]) {
				has_dirty_cards = TRUE;
				break;
			}
		}
		if (has_dirty_cards) {
			size_t num_cards;
			guint8 *mod_union = get_cardtable_mod_union_for_block (block, TRUE);
			sgen_card_table_update_mod_union (mod_union, MS_BLOCK_FOR_BLOCK_INFO (block), ms_block_size, &num_cards);
			SGEN_ASSERT (6, num_cards == CARDS_PER_BLOCK, "Number of cards calculation is wrong");
		}
	} END_FOREACH_BLOCK_NO_LOCK;
}

#undef pthread_create

static void
post_param_init (SgenMajorCollector *collector)
{
	collector->sweeps_lazily = lazy_sweep;
}

/*
 * We are guaranteed to be called by the worker in question.
 * This provides initialization for threads that plan to do
 * parallel object allocation. We need to store these lists
 * in additional data structures so we can traverse them
 * at major/sweep start.
 */
static void
sgen_init_block_free_lists (gpointer *list_p)
{
	int i;
	MSBlockInfo ***worker_free_blocks = (MSBlockInfo ***) mono_native_tls_get_value (worker_block_free_list_key);

	/*
	 * For simplification, a worker thread uses the same free block lists,
	 * regardless of the context it is part of (major/minor).
	 */
	if (worker_free_blocks) {
		*list_p = (gpointer)worker_free_blocks;
		return;
	}

	worker_free_blocks = (MSBlockInfo ***) sgen_alloc_internal_dynamic (sizeof (MSBlockInfo**) * MS_BLOCK_TYPE_MAX, INTERNAL_MEM_MS_TABLES, TRUE);

	for (i = 0; i < MS_BLOCK_TYPE_MAX; i++)
		worker_free_blocks [i] = (MSBlockInfo **) sgen_alloc_internal_dynamic (sizeof (MSBlockInfo*) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);

	*list_p = (gpointer)worker_free_blocks;

	mono_native_tls_set_value (worker_block_free_list_key, worker_free_blocks);
}

static void
sgen_marksweep_init_internal (SgenMajorCollector *collector, gboolean is_concurrent, gboolean is_parallel)
{
	int i;

#ifdef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	g_assert (is_concurrent == FALSE);
	g_assert (is_parallel == FALSE);
#endif

	ms_block_size = mono_pagesize ();

	if (ms_block_size < MS_BLOCK_SIZE_MIN)
		ms_block_size = MS_BLOCK_SIZE_MIN;

	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_MS_BLOCK_INFO, SIZEOF_MS_BLOCK_INFO);

	if (mono_cpu_count () <= 1)
		is_parallel = FALSE;

	num_block_obj_sizes = ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, NULL);
	block_obj_sizes = (int *)sgen_alloc_internal_dynamic (sizeof (int) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);
	ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, block_obj_sizes);

	evacuate_block_obj_sizes = (gboolean *)sgen_alloc_internal_dynamic (sizeof (gboolean) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);
	for (i = 0; i < num_block_obj_sizes; ++i)
		evacuate_block_obj_sizes [i] = FALSE;

	sweep_slots_available = (size_t *)sgen_alloc_internal_dynamic (sizeof (size_t) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);
	sweep_slots_used = (size_t *)sgen_alloc_internal_dynamic (sizeof (size_t) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);
	sweep_num_blocks = (size_t *)sgen_alloc_internal_dynamic (sizeof (size_t) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);

	/*
	{
		int i;
		g_print ("block object sizes:\n");
		for (i = 0; i < num_block_obj_sizes; ++i)
			g_print ("%d\n", block_obj_sizes [i]);
	}
	*/

	for (i = 0; i < MS_BLOCK_TYPE_MAX; ++i)
		free_block_lists [i] = (MSBlockInfo *volatile *)sgen_alloc_internal_dynamic (sizeof (MSBlockInfo*) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES, TRUE);

	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES; ++i)
		fast_block_obj_size_indexes [i] = ms_find_block_obj_size_index (i * 8);
	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES * 8; ++i)
		g_assert (MS_BLOCK_OBJ_SIZE_INDEX (i) == ms_find_block_obj_size_index (i));

	/* We can do this because we always init the minor before the major */
	if (is_parallel || sgen_get_minor_collector ()->is_parallel)
		mono_native_tls_alloc (&worker_block_free_list_key, NULL);

	mono_counters_register ("# major blocks allocated", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_major_blocks_alloced);
	mono_counters_register ("# major blocks freed", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_major_blocks_freed);
	mono_counters_register ("# major blocks lazy swept", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_major_blocks_lazy_swept);
	mono_counters_register ("# major blocks freed ideally", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_major_blocks_freed_ideal);
	mono_counters_register ("# major blocks freed less ideally", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_major_blocks_freed_less_ideal);
	mono_counters_register ("# major blocks freed individually", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_major_blocks_freed_individual);
	mono_counters_register ("# major blocks allocated less ideally", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_major_blocks_alloced_less_ideal);

	collector->section_size = ms_block_size;

	concurrent_mark = is_concurrent;
	collector->is_concurrent = is_concurrent;
	collector->is_parallel = is_parallel;
	collector->get_and_reset_num_major_objects_marked = major_get_and_reset_num_major_objects_marked;
	collector->supports_cardtable = TRUE;

	collector->alloc_heap = major_alloc_heap;
	collector->is_object_live = major_is_object_live;
	collector->alloc_small_pinned_obj = major_alloc_small_pinned_obj;
	collector->alloc_degraded = major_alloc_degraded;

	collector->alloc_object = major_alloc_object;
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	collector->alloc_object_par = major_alloc_object_par;
#endif
	collector->free_pinned_object = free_pinned_object;
	collector->iterate_objects = major_iterate_objects;
	collector->free_non_pinned_object = major_free_non_pinned_object;
	collector->pin_objects = major_pin_objects;
	collector->pin_major_object = pin_major_object;
	collector->scan_card_table = major_scan_card_table;
	collector->iterate_live_block_ranges = major_iterate_live_block_ranges;
	collector->iterate_block_ranges = major_iterate_block_ranges;
	collector->iterate_block_ranges_in_parallel = major_iterate_block_ranges_in_parallel;
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	if (is_concurrent) {
		collector->update_cardtable_mod_union = update_cardtable_mod_union;
		collector->get_cardtable_mod_union_for_reference = major_get_cardtable_mod_union_for_reference;
	}
#endif
	collector->init_to_space = major_init_to_space;
	collector->sweep = major_sweep;
	collector->have_swept = major_have_swept;
	collector->finish_sweeping = major_finish_sweep_checking;
	collector->free_swept_blocks = major_free_swept_blocks;
	collector->check_scan_starts = major_check_scan_starts;
	collector->dump_heap = major_dump_heap;
	collector->get_used_size = major_get_used_size;
	collector->start_nursery_collection = major_start_nursery_collection;
	collector->finish_nursery_collection = major_finish_nursery_collection;
	collector->start_major_collection = major_start_major_collection;
	collector->finish_major_collection = major_finish_major_collection;
	collector->ptr_is_in_non_pinned_space = major_ptr_is_in_non_pinned_space;
	collector->ptr_is_from_pinned_alloc = ptr_is_from_pinned_alloc;
	collector->report_pinned_memory_usage = major_report_pinned_memory_usage;
	collector->get_num_major_sections = get_num_major_sections;
	collector->get_bytes_survived_last_sweep = get_bytes_survived_last_sweep;
	collector->handle_gc_param = major_handle_gc_param;
	collector->print_gc_param_usage = major_print_gc_param_usage;
	collector->post_param_init = post_param_init;
	collector->is_valid_object = major_is_valid_object;
	collector->describe_pointer = major_describe_pointer;
#ifndef DISABLE_SGEN_BINARY_PROTOCOL
	collector->count_cards = major_count_cards;
#endif
	collector->init_block_free_lists = sgen_init_block_free_lists;

	collector->major_ops_serial.copy_or_mark_object = major_copy_or_mark_object_canonical;
	collector->major_ops_serial.scan_object = major_scan_object_with_evacuation;
	collector->major_ops_serial.scan_ptr_field = major_scan_ptr_field_with_evacuation;
	collector->major_ops_serial.drain_gray_stack = drain_gray_stack;
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	if (is_concurrent) {
		collector->major_ops_concurrent_start.copy_or_mark_object = major_copy_or_mark_object_concurrent_canonical;
		collector->major_ops_concurrent_start.scan_object = major_scan_object_concurrent_with_evacuation;
		collector->major_ops_concurrent_start.scan_vtype = major_scan_vtype_concurrent_with_evacuation;
		collector->major_ops_concurrent_start.scan_ptr_field = major_scan_ptr_field_concurrent_with_evacuation;
		collector->major_ops_concurrent_start.drain_gray_stack = drain_gray_stack_concurrent;

		collector->major_ops_concurrent_finish.copy_or_mark_object = major_copy_or_mark_object_concurrent_finish_canonical;
		collector->major_ops_concurrent_finish.scan_object = major_scan_object_with_evacuation;
		collector->major_ops_concurrent_finish.scan_vtype = major_scan_vtype_with_evacuation;
		collector->major_ops_concurrent_finish.scan_ptr_field = major_scan_ptr_field_with_evacuation;
		collector->major_ops_concurrent_finish.drain_gray_stack = drain_gray_stack;

		if (is_parallel) {
			collector->major_ops_conc_par_start.copy_or_mark_object = major_copy_or_mark_object_concurrent_par_canonical;
			collector->major_ops_conc_par_start.scan_object = major_scan_object_concurrent_par_with_evacuation;
			collector->major_ops_conc_par_start.scan_vtype = major_scan_vtype_concurrent_par_with_evacuation;
			collector->major_ops_conc_par_start.scan_ptr_field = major_scan_ptr_field_concurrent_par_with_evacuation;
			collector->major_ops_conc_par_start.drain_gray_stack = drain_gray_stack_concurrent_par;

			collector->major_ops_conc_par_finish.copy_or_mark_object = major_copy_or_mark_object_concurrent_par_finish_canonical;
			collector->major_ops_conc_par_finish.scan_object = major_scan_object_par_with_evacuation;
			collector->major_ops_conc_par_finish.scan_vtype = major_scan_vtype_par_with_evacuation;
			collector->major_ops_conc_par_finish.scan_ptr_field = major_scan_ptr_field_par_with_evacuation;
			collector->major_ops_conc_par_finish.drain_gray_stack = drain_gray_stack_par;
		}
	}
#endif

#ifdef HEAVY_STATISTICS
	mono_counters_register ("Optimized copy", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy);
	mono_counters_register ("Optimized copy nursery", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_nursery);
	mono_counters_register ("Optimized copy nursery forwarded", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_nursery_forwarded);
	mono_counters_register ("Optimized copy nursery pinned", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_nursery_pinned);
	mono_counters_register ("Optimized copy major", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_major);
	mono_counters_register ("Optimized copy major small fast", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_major_small_fast);
	mono_counters_register ("Optimized copy major small slow", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_major_small_slow);
	mono_counters_register ("Optimized copy major small evacuate", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_major_small_evacuate);
	mono_counters_register ("Optimized copy major large", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_copy_major_large);
	mono_counters_register ("Optimized major scan", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_major_scan);
	mono_counters_register ("Optimized major scan no refs", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_optimized_major_scan_no_refs);

	mono_counters_register ("Gray stack drain loops", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_drain_loops);
	mono_counters_register ("Gray stack prefetch fills", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_drain_prefetch_fills);
	mono_counters_register ("Gray stack prefetch failures", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_drain_prefetch_fill_failures);
#endif

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	mono_os_mutex_init (&scanned_objects_list_lock);
#endif

	SGEN_ASSERT (0, SGEN_MAX_SMALL_OBJ_SIZE <= MS_BLOCK_FREE / 2, "MAX_SMALL_OBJ_SIZE must be at most MS_BLOCK_FREE / 2");

	/*cardtable requires major pages to be 8 cards aligned*/
	g_assert ((ms_block_size % (8 * CARD_SIZE_IN_BYTES)) == 0);

#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
	if (is_concurrent && is_parallel)
		sgen_workers_create_context (GENERATION_OLD, mono_cpu_count ());
	else if (is_concurrent)
		sgen_workers_create_context (GENERATION_OLD, 1);

	if (concurrent_sweep)
		sweep_pool_context = sgen_thread_pool_create_context (1, NULL, NULL, NULL, NULL, NULL);
#endif
}

void
sgen_marksweep_init (SgenMajorCollector *collector)
{
	sgen_marksweep_init_internal (collector, FALSE, FALSE);
}

#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
void
sgen_marksweep_conc_init (SgenMajorCollector *collector)
{
	sgen_marksweep_init_internal (collector, TRUE, FALSE);
}

void
sgen_marksweep_conc_par_init (SgenMajorCollector *collector)
{
	sgen_marksweep_init_internal (collector, TRUE, TRUE);
}
#endif


#endif
