#include <math.h>

#define MS_BLOCK_SIZE	(16*1024)

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
#define MS_BLOCK_SKIP	16

#define MS_BLOCK_FREE	(MS_BLOCK_SIZE - MS_BLOCK_SKIP)

#define MS_NUM_MARK_WORDS(c)	(((c) + sizeof (mword) * 8 - 1) / (sizeof (mword) * 8))

#if MAX_SMALL_OBJ_SIZE > MS_BLOCK_FREE / 2
#error MAX_SMALL_OBJ_SIZE must be at most (MS_BLOCK_SIZE - MS_BLOCK_SKIP) / 2
#endif

typedef struct _MSBlockInfo MSBlockInfo;
struct _MSBlockInfo {
	int obj_size;
	gboolean pinned;
	char *block;
	void **free_list;
	MSBlockInfo *next_free;
	MSBlockInfo *next;
	int pin_queue_start;
	int pin_queue_end;
	mword mark_words [1];
};

#define MS_BLOCK_OBJ(b,i)	((b)->block + MS_BLOCK_SKIP + (b)->obj_size * (i))

typedef struct {
	MSBlockInfo *info;
} MSBlockHeader;

#define MS_BLOCK_FOR_OBJ(o)	(((MSBlockHeader*)((mword)(o) & ~(MS_BLOCK_SIZE-1)))->info)

#define MS_BLOCK_OBJ_INDEX(o,b)	(((char*)(o) - ((b)->block + MS_BLOCK_SKIP)) / (b)->obj_size)

#define MS_CALC_MARK_BIT(w,b,i)	do {		\
		if (sizeof (mword) == 4) {	\
			(w) = (i) >> 5;		\
			(b) = (i) & 31;		\
		} else {			\
			(w) = (i) >> 6;		\
			(b) = (i) & 63;		\
		}				\
	} while (0)

#define MS_MARK_BIT(bl,w,b)	((bl)->mark_words [(w)] & (1L << (b)))
#define MS_SET_MARK_BIT(bl,w,b)	((bl)->mark_words [(w)] |= (1L << (b)))

#define MS_OBJ_ALLOCED(o,b)	(*(void**)(o) && (*(char**)(o) < (b)->block || *(char**)(o) >= (b)->block + MS_BLOCK_SIZE))

#define MS_BLOCK_OBJ_SIZE_FACTOR	(sqrt (2.0))

#define MIN_MINOR_COLLECTION_SECTION_ALLOWANCE	(DEFAULT_NURSERY_SIZE * 3 / MS_BLOCK_SIZE)

static int minor_collection_section_allowance;
static int minor_collection_sections_alloced = 0;

/*
 * This way we can lookup block object size indexes for sizes up to
 * 256 bytes with a single load.
 */
#define MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES	32

static int *block_obj_sizes;
static int num_block_obj_sizes;
static int fast_block_obj_size_indexes [MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES];

/* all blocks in the system */
static MSBlockInfo *all_blocks;
static int num_major_sections = 0;
/* one free block list for each block object size */
static MSBlockInfo **free_non_pinned_blocks;
static MSBlockInfo **free_pinned_blocks;

static long stat_major_blocks_alloced = 0;
static long stat_major_blocks_freed = 0;

static int
ms_find_block_obj_size_index (int size)
{
	int i;
	DEBUG (9, g_assert (size <= MAX_SMALL_OBJ_SIZE));
	for (i = 0; i < num_block_obj_sizes; ++i)
		if (block_obj_sizes [i] >= size)
			return i;
	g_assert_not_reached ();
}

#define FREE_BLOCKS(pinned) ((pinned) ? free_pinned_blocks : free_non_pinned_blocks)

#define MS_BLOCK_OBJ_SIZE_INDEX(s)				\
	(((s)+7)>>3 < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES ?	\
	 fast_block_obj_size_indexes [((s)+7)>>3] :		\
	 ms_find_block_obj_size_index ((s)))

void *empty_blocks = NULL;
int num_empty_blocks = 0;

static void*
ms_get_empty_block (void)
{
	char *p;
	int i;
	void *block;

	if (!empty_blocks) {
		g_assert (num_empty_blocks == 0);

		p = get_os_memory_aligned (MS_BLOCK_SIZE * MS_BLOCK_ALLOC_NUM, MS_BLOCK_SIZE, TRUE);

		for (i = 0; i < MS_BLOCK_ALLOC_NUM; ++i) {
			block = p;
			*(void**)block = empty_blocks;
			empty_blocks = block;
			p += MS_BLOCK_SIZE;
		}

		num_empty_blocks += MS_BLOCK_ALLOC_NUM;
	}

	g_assert (empty_blocks);

	block = empty_blocks;
	empty_blocks = *(void**)empty_blocks;

	--num_empty_blocks;

	*(void**)block = NULL;

	g_assert (!((mword)block & (MS_BLOCK_SIZE - 1)));

	return block;
}

static void
ms_free_block (void *block)
{
	memset (block, 0, MS_BLOCK_SIZE);

	*(void**)block = empty_blocks;
	empty_blocks = block;

	++num_empty_blocks;
}

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

		/* the block must be in the all_blocks list */
		for (b = all_blocks; b; b = b->next) {
			if (b == block)
				break;
		}
		g_assert (b == block);
	}
}

static void
consistency_check (void)
{
	MSBlockInfo *block;
	int i;

	/* check all blocks */
	for (block = all_blocks; block; block = block->next) {
		int count = MS_BLOCK_FREE / block->obj_size;
		int num_mark_words = MS_NUM_MARK_WORDS (count);
		int num_free = 0;
		void **free;

		/* check block header */
		g_assert (((MSBlockHeader*)block->block)->info == block);

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
		for (i = 0; i < num_mark_words; ++i)
			g_assert (block->mark_words [i] == 0);
	}

	/* check free blocks */
	for (i = 0; i < num_block_obj_sizes; ++i) {
		check_block_free_list (free_non_pinned_blocks [i], block_obj_sizes [i], FALSE);
		check_block_free_list (free_pinned_blocks [i], block_obj_sizes [i], TRUE);
	}
}

static void
ms_alloc_block (int size_index, gboolean pinned)
{
	int size = block_obj_sizes [size_index];
	int count = MS_BLOCK_FREE / size;
	int num_mark_words = MS_NUM_MARK_WORDS (count);
	int block_info_size = sizeof (MSBlockInfo) + sizeof (mword) * (num_mark_words - 1);
	MSBlockInfo *info = get_internal_mem (block_info_size, INTERNAL_MEM_MS_BLOCK_INFO);
	MSBlockHeader *header;
	MSBlockInfo **free_blocks = FREE_BLOCKS (pinned);
	char *obj_start;
	int i;

	DEBUG (9, g_assert (count >= 2));

	info->obj_size = size;
	info->pinned = pinned;
	info->block = ms_get_empty_block ();

	header = (MSBlockHeader*) info->block;
	header->info = info;

	/* build free list */
	obj_start = info->block + MS_BLOCK_SKIP;
	info->free_list = (void**)obj_start;
	/* we're skipping the last one - it's already NULL */
	for (i = 0; i < count - 1; ++i) {
		char *next_obj_start = obj_start + size;
		*(void**)obj_start = next_obj_start;
		obj_start = next_obj_start;
	}

	info->next_free = free_blocks [size_index];
	free_blocks [size_index] = info;

	info->next = all_blocks;
	all_blocks = info;

	++num_major_sections;

	++stat_major_blocks_alloced;
}

static gboolean
obj_is_from_pinned_alloc (char *obj)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
	return block->pinned;
}

static void*
alloc_obj (int size, gboolean pinned)
{
	int size_index = MS_BLOCK_OBJ_SIZE_INDEX (size);
	MSBlockInfo **free_blocks = FREE_BLOCKS (pinned);
	MSBlockInfo *block;
	void *obj;

	if (!free_blocks [size_index])
		ms_alloc_block (size_index, pinned);

	block = free_blocks [size_index];
	DEBUG (9, g_assert (block));

	obj = block->free_list;
	DEBUG (9, g_assert (obj));

	block->free_list = *(void**)obj;
	if (!block->free_list) {
		free_blocks [size_index] = block->next_free;
		block->next_free = NULL;
	}

	/*
	 * FIXME: This should not be necessary because it'll be
	 * overwritten by the vtable immediately.
	 */
	*(void**)obj = NULL;

	return obj;
}

static void*
ms_alloc_obj (int size)
{
	return alloc_obj (size, FALSE);
}

/* FIXME: inline fast path */
#define MAJOR_GET_COPY_OBJECT_SPACE(dest, size) do {	\
		(dest) = ms_alloc_obj ((size));		\
	} while (0)

/*
 * We're not freeing the block if it's empty.  We leave that work for
 * the next major collection.
 */
static void
free_object (char *obj, size_t size, gboolean pinned)
{
	MSBlockInfo *block = MS_BLOCK_FOR_OBJ (obj);
	int word, bit;
	DEBUG (9, g_assert ((pinned && block->pinned) || (!pinned && !block->pinned)));
	DEBUG (9, g_assert (MS_OBJ_ALLOCED (obj, block)));
	MS_CALC_MARK_BIT (word, bit, MS_BLOCK_OBJ_INDEX (obj, block));
	DEBUG (9, g_assert (!MS_MARK_BIT (block, word, bit)));
	if (!block->free_list) {
		MSBlockInfo **free_blocks = FREE_BLOCKS (pinned);
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

/* size is a multiple of ALLOC_ALIGN */
static void*
major_alloc_small_pinned_obj (size_t size)
{
	return alloc_obj (size, TRUE);
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
alloc_degraded (MonoVTable *vtable, size_t size)
{
	void *obj = alloc_obj (size, FALSE);
	*(MonoVTable**)obj = vtable;
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
	mword objsize;

	if (ptr_in_nursery (obj))
		return FALSE;

	objsize = safe_object_get_size ((MonoObject*)obj);
	objsize += ALLOC_ALIGN - 1;
	objsize &= ~(ALLOC_ALIGN - 1);

	/* LOS */
	if (objsize > MAX_SMALL_OBJ_SIZE)
		return FALSE;

	/* now we know it's in a major block */
	block = MS_BLOCK_FOR_OBJ (obj);
	DEBUG (9, g_assert (!block->pinned));
	MS_CALC_MARK_BIT (word, bit, MS_BLOCK_OBJ_INDEX (obj, block));
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

	for (block = all_blocks; block; block = block->next) {
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
	}
}

#define major_check_scan_starts()

static void
major_dump_non_pinned_sections (void)
{
	g_assert_not_reached ();
}

#define MS_MARK_INDEX_IN_BLOCK_AND_ENQUEUE_CHECKED(obj,block,index) do { \
		int __word, __bit;					\
		MS_CALC_MARK_BIT (__word, __bit, (index));		\
		DEBUG (9, g_assert ((obj) == MS_BLOCK_OBJ ((block), (index)))); \
		if (!MS_MARK_BIT ((block), __word, __bit) && MS_OBJ_ALLOCED ((obj), (block))) { \
			MS_SET_MARK_BIT ((block), __word, __bit);	\
			GRAY_OBJECT_ENQUEUE ((obj));			\
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), safe_object_get_size ((MonoObject*)(obj))); \
		}							\
	} while (0)
#define MS_MARK_INDEX_IN_BLOCK_AND_ENQUEUE(obj,block,index) do {	\
		int __word, __bit;					\
		MS_CALC_MARK_BIT (__word, __bit, (index));		\
		DEBUG (9, g_assert ((obj) == MS_BLOCK_OBJ ((block), (index)))); \
		g_assert (MS_OBJ_ALLOCED ((obj), (block)));		\
		if (!MS_MARK_BIT ((block), __word, __bit)) {		\
			MS_SET_MARK_BIT ((block), __word, __bit);	\
			GRAY_OBJECT_ENQUEUE ((obj));			\
			binary_protocol_mark ((obj), (gpointer)LOAD_VTABLE ((obj)), safe_object_get_size ((MonoObject*)(obj))); \
		}							\
	} while (0)

static void
major_copy_or_mark_object (void **ptr)
{
	void *obj = *ptr;
	mword objsize;
	MSBlockInfo *block;
	int index;
	int count;

	DEBUG (9, g_assert (obj));
	DEBUG (9, g_assert (current_collection_generation == GENERATION_OLD));

	if (ptr_in_nursery (obj)) {
		int word, bit;
		char *forwarded;

		if ((forwarded = object_is_forwarded (obj))) {
			*ptr = forwarded;
			return;
		}
		if (object_is_pinned (obj))
			return;

		obj = copy_object_no_checks (obj);
		*ptr = obj;

		/*
		 * FIXME: See comment for copy_object_no_checks().  If
		 * we have that, we can let the allocation function
		 * give us the block info, too, and we won't have to
		 * re-fetch it.
		 */
		block = MS_BLOCK_FOR_OBJ (obj);
		index = MS_BLOCK_OBJ_INDEX (obj, block);
		DEBUG (9, g_assert (obj == MS_BLOCK_OBJ (block, index)));
		MS_CALC_MARK_BIT (word, bit, (index));
		DEBUG (9, g_assert (!MS_MARK_BIT (block, word, bit)));
		MS_SET_MARK_BIT (block, word, bit);
		return;
	}

	objsize = safe_object_get_size ((MonoObject*)obj);
	objsize += ALLOC_ALIGN - 1;
	objsize &= ~(ALLOC_ALIGN - 1);

	if (objsize > MAX_SMALL_OBJ_SIZE) {
		if (object_is_pinned (obj))
			return;
		binary_protocol_pin (obj, (gpointer)LOAD_VTABLE (obj), safe_object_get_size ((MonoObject*)obj));
		pin_object (obj);
		GRAY_OBJECT_ENQUEUE (obj);
		return;
	}

	block = MS_BLOCK_FOR_OBJ (obj);
	index = MS_BLOCK_OBJ_INDEX (obj, block);
	count = MS_BLOCK_FREE / block->obj_size;
	DEBUG (9, g_assert (index >= 0 && index < count));
	MS_MARK_INDEX_IN_BLOCK_AND_ENQUEUE (obj, block, index);
}

static void
mark_pinned_objects_in_block (MSBlockInfo *block)
{
	int i;
	int last_index = -1;
	int count = MS_BLOCK_FREE / block->obj_size;

	for (i = block->pin_queue_start; i < block->pin_queue_end; ++i) {
		int index = MS_BLOCK_OBJ_INDEX (pin_queue [i], block);
		DEBUG (9, g_assert (index >= 0 && index < count));
		if (index == last_index)
			continue;
		MS_MARK_INDEX_IN_BLOCK_AND_ENQUEUE_CHECKED (MS_BLOCK_OBJ (block, index), block, index);
		last_index = index;
	}
}

static void
major_sweep (void)
{
	MSBlockInfo *empty_blocks = NULL;
	MSBlockInfo **iter;
	int i;

	/* traverse all blocks, free and zero unmarked objects */
	iter = &all_blocks;
	while (*iter) {
		MSBlockInfo *block = *iter;
		int count = MS_BLOCK_FREE / block->obj_size;
		gboolean have_live = FALSE;
		int word_index = 0;
		int obj_index = 0;

		block->free_list = NULL;

		do {
			mword word = block->mark_words [word_index];

			if (word == (mword)-1L) {
				obj_index += 8 * sizeof (mword);
				have_live = TRUE;
			} else {
				int num_bits = MIN (count - obj_index, 8 * sizeof (mword));
				for (i = 0; i < num_bits; ++i) {
					void *obj = MS_BLOCK_OBJ (block, obj_index);
					if (word & 1) {
						DEBUG (9, g_assert (MS_OBJ_ALLOCED (obj, block)));
						have_live = TRUE;
					} else {
						/* an unmarked object */
						void *obj = MS_BLOCK_OBJ (block, obj_index);
						if (MS_OBJ_ALLOCED (obj, block)) {
							binary_protocol_empty (obj, block->obj_size);
							memset (obj, 0, block->obj_size);
						}
						*(void**)obj = block->free_list;
						block->free_list = obj;
					}
					word >>= 1;
					++obj_index;
				}
			}

			/* reset mark bits */
			block->mark_words [word_index] = 0;

			++word_index;
		} while (obj_index < count);

		/*
		 * FIXME: reverse free list so that it's in address
		 * order
		 */

		if (have_live) {
			iter = &block->next;
			continue;
		}

		/*
		 * blocks without live objects are removed from the
		 * block list and marked with obj_size = -1
		 */
		*iter = block->next;
		block->next = empty_blocks;
		empty_blocks = block;

		block->obj_size = -1;
	}

	/* if there are no blocks to be freed, we're done */
	if (!empty_blocks)
		return;

	/* go through all free lists and remove the blocks to be freed */
	for (i = 0; i < num_block_obj_sizes; ++i) {
		int pinned;
		for (pinned = 0; pinned <= 1; ++pinned) {
			MSBlockInfo **free_blocks = FREE_BLOCKS (pinned);
			iter = &(free_blocks [i]);
			while (*iter) {
				MSBlockInfo *block = *iter;
				if (block->obj_size < 0)
					*iter = block->next_free;
				else
					iter = &block->next_free;
			}
		}
	}

	/* now free the blocks */
	while (empty_blocks) {
		MSBlockInfo *next = empty_blocks->next;

		ms_free_block (empty_blocks->block);
		free_internal_mem (empty_blocks, INTERNAL_MEM_MS_BLOCK_INFO);

		empty_blocks = next;

		--num_major_sections;

		++stat_major_blocks_freed;
	}
}

static void
major_do_collection (const char *reason)
{
	LOSObject *bigobj, *prevbo;
	MSBlockInfo *block;
	TV_DECLARE (all_atv);
	TV_DECLARE (all_btv);
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	/* FIXME: only use these values for the precise scan
	 * note that to_space pointers should be excluded anyway...
	 */
	char *heap_start = NULL;
	char *heap_end = (char*)-1;
	int old_num_major_sections = num_major_sections;
	int num_major_sections_saved, save_target, allowance_target;

	//consistency_check ();

	init_stats ();
	binary_protocol_collection (GENERATION_OLD);
	check_scan_starts ();
	gray_object_queue_init ();

	degraded_mode = 0;
	DEBUG (1, fprintf (gc_debug_file, "Start major collection %d\n", num_major_gcs));
	num_major_gcs++;
	mono_stats.major_gc_count ++;

	/* world must be stopped already */
	TV_GETTIME (all_atv);
	TV_GETTIME (atv);

	/* Pinning depends on this */
	clear_nursery_fragments (nursery_next);

	TV_GETTIME (btv);
	time_major_pre_collection_fragment_clear += TV_ELAPSED_MS (atv, btv);

	if (xdomain_checks)
		check_for_xdomain_refs ();

	nursery_section->next_data = nursery_real_end;
	/* we should also coalesce scanning from sections close to each other
	 * and deal with pointers outside of the sections later.
	 */
	/* The remsets are not useful for a major collection */
	clear_remsets ();

	TV_GETTIME (atv);
	init_pinning ();
	DEBUG (6, fprintf (gc_debug_file, "Collecting pinned addresses\n"));
	pin_from_roots ((void*)lowest_heap_address, (void*)highest_heap_address);
	optimize_pin_queue (0);

	/*
	 * pin_queue now contains all candidate pointers, sorted and
	 * uniqued.  We must do two passes now to figure out which
	 * objects are pinned.
	 *
	 * The first is to find within the pin_queue the area for each
	 * section.  This requires that the pin_queue be sorted.  We
	 * also process the LOS objects and pinned chunks here.
	 *
	 * The second, destructive, pass is to reduce the section
	 * areas to pointers to the actually pinned objects.
	 */
	DEBUG (6, fprintf (gc_debug_file, "Pinning from sections\n"));
	/* first pass for the sections */
	find_section_pin_queue_start_end (nursery_section);
	for (block = all_blocks; block; block = block->next) {
		find_optimized_pin_queue_area (block->block + MS_BLOCK_SKIP, block->block + MS_BLOCK_SIZE,
				&block->pin_queue_start, &block->pin_queue_end);
	}
	/* identify possible pointers to the insize of large objects */
	DEBUG (6, fprintf (gc_debug_file, "Pinning from large objects\n"));
	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next) {
		int start, end;
		find_optimized_pin_queue_area (bigobj->data, (char*)bigobj->data + bigobj->size, &start, &end);
		if (start != end) {
			pin_object (bigobj->data);
			GRAY_OBJECT_ENQUEUE (bigobj->data);
			if (heap_dump_file)
				pin_stats_register_object ((char*) bigobj->data, safe_object_get_size ((MonoObject*) bigobj->data));
			DEBUG (6, fprintf (gc_debug_file, "Marked large object %p (%s) size: %zd from roots\n", bigobj->data, safe_name (bigobj->data), bigobj->size));
		}
	}
	/* second pass for the sections */
	pin_objects_in_section (nursery_section);
	for (block = all_blocks; block; block = block->next)
		mark_pinned_objects_in_block (block);

	TV_GETTIME (btv);
	time_major_pinning += TV_ELAPSED_MS (atv, btv);
	DEBUG (2, fprintf (gc_debug_file, "Finding pinned pointers: %d in %d usecs\n", next_pin_slot, TV_ELAPSED (atv, btv)));
	DEBUG (4, fprintf (gc_debug_file, "Start scan with %d pinned objects\n", next_pin_slot));

	drain_gray_stack ();

	TV_GETTIME (atv);
	time_major_scan_pinned += TV_ELAPSED_MS (btv, atv);

	/* registered roots, this includes static fields */
	scan_from_registered_roots (major_copy_or_mark_object, heap_start, heap_end, ROOT_TYPE_NORMAL);
	scan_from_registered_roots (major_copy_or_mark_object, heap_start, heap_end, ROOT_TYPE_WBARRIER);
	TV_GETTIME (btv);
	time_major_scan_registered_roots += TV_ELAPSED_MS (atv, btv);

	/* Threads */
	/* FIXME: This is the wrong place for this, because it does
	   pinning */
	scan_thread_data (heap_start, heap_end, TRUE);
	TV_GETTIME (atv);
	time_major_scan_thread_data += TV_ELAPSED_MS (btv, atv);

	TV_GETTIME (btv);
	time_major_scan_alloc_pinned += TV_ELAPSED_MS (atv, btv);

	/* scan the list of objects ready for finalization */
	scan_finalizer_entries (major_copy_or_mark_object, fin_ready_list);
	scan_finalizer_entries (major_copy_or_mark_object, critical_fin_list);
	TV_GETTIME (atv);
	time_major_scan_finalized += TV_ELAPSED_MS (btv, atv);
	DEBUG (2, fprintf (gc_debug_file, "Root scan: %d usecs\n", TV_ELAPSED (btv, atv)));

	TV_GETTIME (btv);
	time_major_scan_big_objects += TV_ELAPSED_MS (atv, btv);

	/* all the objects in the heap */
	finish_gray_stack (heap_start, heap_end, GENERATION_OLD);
	TV_GETTIME (atv);
	time_major_finish_gray_stack += TV_ELAPSED_MS (btv, atv);

	/* sweep the big objects list */
	prevbo = NULL;
	for (bigobj = los_object_list; bigobj;) {
		if (object_is_pinned (bigobj->data)) {
			unpin_object (bigobj->data);
		} else {
			LOSObject *to_free;
			/* not referenced anywhere, so we can free it */
			if (prevbo)
				prevbo->next = bigobj->next;
			else
				los_object_list = bigobj->next;
			to_free = bigobj;
			bigobj = bigobj->next;
			free_large_object (to_free);
			continue;
		}
		prevbo = bigobj;
		bigobj = bigobj->next;
	}

	major_sweep ();

	TV_GETTIME (btv);
	time_major_sweep += TV_ELAPSED_MS (atv, btv);

	/* walk the pin_queue, build up the fragment list of free memory, unmark
	 * pinned objects as we go, memzero() the empty fragments so they are ready for the
	 * next allocations.
	 */
	build_nursery_fragments (nursery_section->pin_queue_start, nursery_section->pin_queue_end);

	TV_GETTIME (atv);
	time_major_fragment_creation += TV_ELAPSED_MS (btv, atv);

	TV_GETTIME (all_btv);
	mono_stats.major_gc_time_usecs += TV_ELAPSED (all_atv, all_btv);

	if (heap_dump_file)
		dump_heap ("major", num_major_gcs - 1, reason);

	/* prepare the pin queue for the next collection */
	next_pin_slot = 0;
	if (fin_ready_list || critical_fin_list) {
		DEBUG (4, fprintf (gc_debug_file, "Finalizer-thread wakeup: ready %d\n", num_ready_finalizers));
		mono_gc_finalize_notify ();
	}
	pin_stats_reset ();

	g_assert (gray_object_queue_is_empty ());

	commit_stats (GENERATION_OLD);

	num_major_sections_saved = MAX (old_num_major_sections - num_major_sections, 1);

	save_target = num_major_sections / 2;
	/*
	 * We aim to allow the allocation of as many sections as is
	 * necessary to reclaim save_target sections in the next
	 * collection.  We assume the collection pattern won't change.
	 * In the last cycle, we had num_major_sections_saved for
	 * minor_collection_sections_alloced.  Assuming things won't
	 * change, this must be the same ratio as save_target for
	 * allowance_target, i.e.
	 *
	 *    num_major_sections_saved            save_target
	 * --------------------------------- == ----------------
	 * minor_collection_sections_alloced    allowance_target
	 *
	 * hence:
	 */
	allowance_target = save_target * minor_collection_sections_alloced / num_major_sections_saved;

	minor_collection_section_allowance = MAX (MIN (allowance_target, num_major_sections), MIN_MINOR_COLLECTION_SECTION_ALLOWANCE);

	minor_collection_sections_alloced = 0;

	check_scan_starts ();

	//consistency_check ();
}

static int
ms_calculate_block_obj_sizes (double factor, int *arr)
{
	double target_size = sizeof (MonoObject);
	int num_sizes = 0;
	int last_size = 0;

	do {
		int target_count = ceil (MS_BLOCK_FREE / target_size);
		int size = MIN ((MS_BLOCK_FREE / target_count) & ~(ALLOC_ALIGN - 1), MAX_SMALL_OBJ_SIZE);

		if (size != last_size) {
			if (arr)
				arr [num_sizes] = size;
			++num_sizes;
			last_size = size;
		}

		target_size *= factor;
	} while (last_size < MAX_SMALL_OBJ_SIZE);

	return num_sizes;
}

static void
major_init (void)
{
	int i;

	num_block_obj_sizes = ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, NULL);
	block_obj_sizes = get_internal_mem (sizeof (int) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);
	ms_calculate_block_obj_sizes (MS_BLOCK_OBJ_SIZE_FACTOR, block_obj_sizes);

	/*
	{
		int i;
		g_print ("block object sizes:\n");
		for (i = 0; i < num_block_obj_sizes; ++i)
			g_print ("%d\n", block_obj_sizes [i]);
	}
	*/

	free_pinned_blocks = get_internal_mem (sizeof (MSBlockInfo*) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);
	free_non_pinned_blocks = get_internal_mem (sizeof (MSBlockInfo*) * num_block_obj_sizes, INTERNAL_MEM_MS_TABLES);

	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES; ++i)
		fast_block_obj_size_indexes [i] = ms_find_block_obj_size_index (i * 8);
	for (i = 0; i < MS_NUM_FAST_BLOCK_OBJ_SIZE_INDEXES * 8; ++i)
		g_assert (MS_BLOCK_OBJ_SIZE_INDEX (i) == ms_find_block_obj_size_index (i));

	mono_counters_register ("# major blocks allocated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_alloced);
	mono_counters_register ("# major blocks freed", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_major_blocks_freed);

	minor_collection_section_allowance = MIN_MINOR_COLLECTION_SECTION_ALLOWANCE;
}

/* only valid during minor collections */
static int old_num_major_sections;

static void
major_start_nursery_collection (void)
{
	//consistency_check ();

	old_num_major_sections = num_major_sections;
}

static void
major_finish_nursery_collection (void)
{
	int sections_alloced;

	//consistency_check ();

	sections_alloced = num_major_sections - old_num_major_sections;
	minor_collection_sections_alloced += sections_alloced;
}

static gboolean
major_need_major_collection (void)
{
	return minor_collection_sections_alloced > minor_collection_section_allowance;
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

	for (block = all_blocks; block; block = block->next) {
		int count = MS_BLOCK_FREE / block->obj_size;
		void **iter;
		size += count * block->obj_size;
		for (iter = block->free_list; iter; iter = (void**)*iter)
			size -= block->obj_size;
	}

	return size;
}
