/**
 * \file
 * Internal lock-free memory allocator.
 *
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/utils/lock-free-alloc.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-client.h"

/*
 * When allocating sgen memory we choose the allocator with the smallest slot size
 * that can fit our requested size. These slots are allocated within a block that
 * can contain at least 2 slots of the specific size.
 *
 * Currently, slots from 8 to 2044/2040 are allocated inside 4096 sized blocks,
 * 2728 to 4092/4088 inside 8192 sized blocks, and higher inside 16384 sized
 * blocks. We also need to make sure the slots are pointer size aligned so we
 * don't allocate unaligned memory.
 *
 * The computation of these sizes spawns from two basic rules :
 * 	- if we use slots of size s1 that fit n times in a block, it is illogical
 * to use another slot of size s2 which also fits the same n times in a block.
 *	- if we use slots of size s1 that fit n times in a block, there is no
 * s2 > s1 that can fit n times in the block. That would mean we are wasting memory
 * when allocating size S where s1 < S <= s2.
 */
#if SIZEOF_VOID_P == 4
static const int allocator_sizes [] = {
	   8,   16,   24,   32,   40,   48,   64,   80,
	  96,  124,  160,  192,  224,  252,  292,  340,
	 408,  452,  508,  584,  680,  816, 1020,
	1364, 2044, 2728, 4092, 5460, 8188 };
#else
static const int allocator_sizes [] = {
	   8,   16,   24,   32,   40,   48,   64,   80,
	  96,  128,  160,  192,  224,  248,  288,  336,
	 368,  448,  504,  584,  680,  816, 1016,
	1360, 2040, 2728, 4088, 5456, 8184 };
#endif

#define NUM_ALLOCATORS	(sizeof (allocator_sizes) / sizeof (int))

static int allocator_block_sizes [NUM_ALLOCATORS];

static MonoLockFreeAllocSizeClass size_classes [NUM_ALLOCATORS];
static MonoLockFreeAllocator allocators [NUM_ALLOCATORS];

#ifdef HEAVY_STATISTICS
static int allocator_sizes_stats [NUM_ALLOCATORS];
#endif

static size_t
block_size (size_t slot_size)
{
	static int pagesize = -1;

	int size;
	size_t aligned_slot_size = SGEN_ALIGN_UP_TO (slot_size, SIZEOF_VOID_P);

	if (pagesize == -1)
		pagesize = mono_pagesize ();

	for (size = pagesize; size < LOCK_FREE_ALLOC_SB_MAX_SIZE; size <<= 1) {
		if (aligned_slot_size * 2 <= LOCK_FREE_ALLOC_SB_USABLE_SIZE (size))
			return size;
	}
	return LOCK_FREE_ALLOC_SB_MAX_SIZE;
}

/*
 * Find the allocator index for memory chunks that can contain @size
 * objects.
 */
static int
index_for_size (size_t size)
{
	int slot;
	/* do a binary search or lookup table later. */
	for (slot = 0; slot < NUM_ALLOCATORS; ++slot) {
		if (allocator_sizes [slot] >= size)
			return slot;
	}
	g_assert_not_reached ();
	return -1;
}

/*
 * Allocator indexes for the fixed INTERNAL_MEM_XXX types.  -1 if that
 * type is dynamic.
 */
static int fixed_type_allocator_indexes [INTERNAL_MEM_MAX];

void
sgen_register_fixed_internal_mem_type (int type, size_t size)
{
	int slot;

	g_assert (type >= 0 && type < INTERNAL_MEM_MAX);
	g_assert (size <= allocator_sizes [NUM_ALLOCATORS - 1]);

	slot = index_for_size (size);
	g_assert (slot >= 0);

	if (fixed_type_allocator_indexes [type] == -1)
		fixed_type_allocator_indexes [type] = slot;
	else {
		if (fixed_type_allocator_indexes [type] != slot)
			g_error ("Invalid double registration of type %d old slot %d new slot %d", type, fixed_type_allocator_indexes [type], slot);
	}
}

static const char*
description_for_type (int type)
{
	switch (type) {
	case INTERNAL_MEM_PIN_QUEUE: return "pin-queue";
	case INTERNAL_MEM_FRAGMENT: return "fragment";
	case INTERNAL_MEM_SECTION: return "section";
	case INTERNAL_MEM_SCAN_STARTS: return "scan-starts";
	case INTERNAL_MEM_FIN_TABLE: return "fin-table";
	case INTERNAL_MEM_FINALIZE_ENTRY: return "finalize-entry";
	case INTERNAL_MEM_FINALIZE_READY: return "finalize-ready";
	case INTERNAL_MEM_DISLINK_TABLE: return "dislink-table";
	case INTERNAL_MEM_DISLINK: return "dislink";
	case INTERNAL_MEM_ROOTS_TABLE: return "roots-table";
	case INTERNAL_MEM_ROOT_RECORD: return "root-record";
	case INTERNAL_MEM_STATISTICS: return "statistics";
	case INTERNAL_MEM_STAT_PINNED_CLASS: return "pinned-class";
	case INTERNAL_MEM_STAT_REMSET_CLASS: return "remset-class";
	case INTERNAL_MEM_STAT_GCHANDLE_CLASS: return "gchandle-class";
	case INTERNAL_MEM_GRAY_QUEUE: return "gray-queue";
	case INTERNAL_MEM_MS_TABLES: return "marksweep-tables";
	case INTERNAL_MEM_MS_BLOCK_INFO: return "marksweep-block-info";
	case INTERNAL_MEM_MS_BLOCK_INFO_SORT: return "marksweep-block-info-sort";
	case INTERNAL_MEM_WORKER_DATA: return "worker-data";
	case INTERNAL_MEM_THREAD_POOL_JOB: return "thread-pool-job";
	case INTERNAL_MEM_BRIDGE_DATA: return "bridge-data";
	case INTERNAL_MEM_OLD_BRIDGE_HASH_TABLE: return "old-bridge-hash-table";
	case INTERNAL_MEM_OLD_BRIDGE_HASH_TABLE_ENTRY: return "old-bridge-hash-table-entry";
	case INTERNAL_MEM_BRIDGE_HASH_TABLE: return "bridge-hash-table";
	case INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY: return "bridge-hash-table-entry";
	case INTERNAL_MEM_TARJAN_BRIDGE_HASH_TABLE: return "tarjan-bridge-hash-table";
	case INTERNAL_MEM_TARJAN_BRIDGE_HASH_TABLE_ENTRY: return "tarjan-bridge-hash-table-entry";
	case INTERNAL_MEM_TARJAN_OBJ_BUCKET: return "tarjan-bridge-object-buckets";
	case INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE: return "bridge-alive-hash-table";
	case INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE_ENTRY: return "bridge-alive-hash-table-entry";
	case INTERNAL_MEM_BRIDGE_DEBUG: return "bridge-debug";
	case INTERNAL_MEM_TOGGLEREF_DATA: return "toggleref-data";
	case INTERNAL_MEM_CARDTABLE_MOD_UNION: return "cardtable-mod-union";
	case INTERNAL_MEM_BINARY_PROTOCOL: return "binary-protocol";
	case INTERNAL_MEM_TEMPORARY: return "temporary";
	case INTERNAL_MEM_LOG_ENTRY: return "log-entry";
	case INTERNAL_MEM_COMPLEX_DESCRIPTORS: return "complex-descriptors";
	default: {
		const char *description = sgen_client_description_for_internal_mem_type (type);
		SGEN_ASSERT (0, description, "Unknown internal mem type");
		return description;
	}
	}
}

void*
sgen_alloc_internal_dynamic (size_t size, int type, gboolean assert_on_failure)
{
	int index;
	void *p;

	if (size > allocator_sizes [NUM_ALLOCATORS - 1]) {
		p = sgen_alloc_os_memory (size, (SgenAllocFlags)(SGEN_ALLOC_INTERNAL | SGEN_ALLOC_ACTIVATE), NULL, MONO_MEM_ACCOUNT_SGEN_INTERNAL);
		if (!p)
			sgen_assert_memory_alloc (NULL, size, description_for_type (type));
	} else {
		index = index_for_size (size);

#ifdef HEAVY_STATISTICS
		++ allocator_sizes_stats [index];
#endif

		p = mono_lock_free_alloc (&allocators [index]);
		if (!p)
			sgen_assert_memory_alloc (NULL, size, description_for_type (type));
		memset (p, 0, size);
	}

	SGEN_ASSERT (0, !(((mword)p) & (sizeof(gpointer) - 1)), "Why do we allocate unaligned addresses ?");
	return p;
}

void
sgen_free_internal_dynamic (void *addr, size_t size, int type)
{
	if (!addr)
		return;

	if (size > allocator_sizes [NUM_ALLOCATORS - 1])
		sgen_free_os_memory (addr, size, SGEN_ALLOC_INTERNAL, MONO_MEM_ACCOUNT_SGEN_INTERNAL);
	else
		mono_lock_free_free (addr, block_size (size));
}

void*
sgen_alloc_internal (int type)
{
	int index, size;
	void *p;

	index = fixed_type_allocator_indexes [type];
	g_assert (index >= 0 && index < NUM_ALLOCATORS);

#ifdef HEAVY_STATISTICS
	++ allocator_sizes_stats [index];
#endif

	size = allocator_sizes [index];

	p = mono_lock_free_alloc (&allocators [index]);
	memset (p, 0, size);

	SGEN_ASSERT (0, !(((mword)p) & (sizeof(gpointer) - 1)), "Why do we allocate unaligned addresses ?");

	return p;
}

void
sgen_free_internal (void *addr, int type)
{
	int index;

	if (!addr)
		return;

	index = fixed_type_allocator_indexes [type];
	g_assert (index >= 0 && index < NUM_ALLOCATORS);

	mono_lock_free_free (addr, allocator_block_sizes [index]);
}

void
sgen_dump_internal_mem_usage (FILE *heap_dump_file)
{
	/*
	int i;

	fprintf (heap_dump_file, "<other-mem-usage type=\"large-internal\" size=\"%" PRIx64 "\"/>\n", large_internal_bytes_alloced);
	fprintf (heap_dump_file, "<other-mem-usage type=\"pinned-chunks\" size=\"%" PRIx64 "\"/>\n", pinned_chunk_bytes_alloced);
	for (i = 0; i < INTERNAL_MEM_MAX; ++i) {
		fprintf (heap_dump_file, "<other-mem-usage type=\"%s\" size=\"%ld\"/>\n",
				description_for_type (i), unmanaged_allocator.small_internal_mem_bytes [i]);
	}
	*/
}

void
sgen_report_internal_mem_usage (void)
{
#ifdef HEAVY_STATISTICS
	int i;
	printf ("size -> # allocations\n");
	for (i = 0; i < NUM_ALLOCATORS; ++i)
		printf ("%d -> %d\n", allocator_sizes [i], allocator_sizes_stats [i]);
#endif
}

void
sgen_init_internal_allocator (void)
{
	int i, size;

	for (i = 0; i < INTERNAL_MEM_MAX; ++i)
		fixed_type_allocator_indexes [i] = -1;

	for (i = 0; i < NUM_ALLOCATORS; ++i) {
		allocator_block_sizes [i] = block_size (allocator_sizes [i]);
		mono_lock_free_allocator_init_size_class (&size_classes [i], allocator_sizes [i], allocator_block_sizes [i]);
		mono_lock_free_allocator_init_allocator (&allocators [i], &size_classes [i], MONO_MEM_ACCOUNT_SGEN_INTERNAL);
	}

	for (size = mono_pagesize (); size <= LOCK_FREE_ALLOC_SB_MAX_SIZE; size <<= 1) {
		int max_size = (LOCK_FREE_ALLOC_SB_USABLE_SIZE (size) / 2) & ~(SIZEOF_VOID_P - 1);
		/*
		 * we assert that allocator_sizes contains the biggest possible object size
		 * per block which has to be an aligned address.
		 * (4K => 2040, 8k => 4088, 16k => 8184 on 64bits),
		 * so that we do not get different block sizes for sizes that should go to the same one
		 */
		g_assert (allocator_sizes [index_for_size (max_size)] == max_size);
		g_assert (block_size (max_size) == size);
		if (size < LOCK_FREE_ALLOC_SB_MAX_SIZE)
			g_assert (block_size (max_size + 1) == size << 1);
	}
}

#endif
