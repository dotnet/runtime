/*
 * sgen-internal.c: Internal lock-free memory allocator.
 *
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

#include "utils/mono-counters.h"
#include "metadata/sgen-gc.h"
#include "utils/lock-free-alloc.h"
#include "metadata/sgen-memory-governor.h"

/* keep each size a multiple of ALLOC_ALIGN */
static const int allocator_sizes [] = {
	   8,   16,   24,   32,   40,   48,   64,   80,
	  96,  128,  160,  192,  224,  248,  320,  384,
	 448,  528,  584,  680,  816, 1088, 1360, 2040,
	2336, 2728, 3272, 4088, 5456, 8184 };

#define NUM_ALLOCATORS	(sizeof (allocator_sizes) / sizeof (int))

static MonoLockFreeAllocSizeClass size_classes [NUM_ALLOCATORS];
static MonoLockFreeAllocator allocators [NUM_ALLOCATORS];

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

	slot = index_for_size (size);
	g_assert (slot >= 0);

	if (fixed_type_allocator_indexes [type] == -1)
		fixed_type_allocator_indexes [type] = slot;
	else
		g_assert (fixed_type_allocator_indexes [type] == slot);
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
	case INTERNAL_MEM_FINALIZE_READY_ENTRY: return "finalize-ready-entry";
	case INTERNAL_MEM_DISLINK_TABLE: return "dislink-table";
	case INTERNAL_MEM_DISLINK: return "dislink";
	case INTERNAL_MEM_ROOTS_TABLE: return "roots-table";
	case INTERNAL_MEM_ROOT_RECORD: return "root-record";
	case INTERNAL_MEM_STATISTICS: return "statistics";
	case INTERNAL_MEM_STAT_PINNED_CLASS: return "pinned-class";
	case INTERNAL_MEM_STAT_REMSET_CLASS: return "remset-class";
	case INTERNAL_MEM_GRAY_QUEUE: return "gray-queue";
	case INTERNAL_MEM_MS_TABLES: return "marksweep-tables";
	case INTERNAL_MEM_MS_BLOCK_INFO: return "marksweep-block-info";
	case INTERNAL_MEM_MS_BLOCK_INFO_SORT: return "marksweep-block-info-sort";
	case INTERNAL_MEM_EPHEMERON_LINK: return "ephemeron-link";
	case INTERNAL_MEM_WORKER_DATA: return "worker-data";
	case INTERNAL_MEM_WORKER_JOB_DATA: return "worker-job-data";
	case INTERNAL_MEM_BRIDGE_DATA: return "bridge-data";
	case INTERNAL_MEM_BRIDGE_HASH_TABLE: return "bridge-hash-table";
	case INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY: return "bridge-hash-table-entry";
	case INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE: return "bridge-alive-hash-table";
	case INTERNAL_MEM_BRIDGE_ALIVE_HASH_TABLE_ENTRY: return "bridge-alive-hash-table-entry";
	case INTERNAL_MEM_JOB_QUEUE_ENTRY: return "job-queue-entry";
	case INTERNAL_MEM_TOGGLEREF_DATA: return "toggleref-data";
	case INTERNAL_MEM_CARDTABLE_MOD_UNION: return "cardtable-mod-union";
	default:
		g_assert_not_reached ();
	}
}

void*
sgen_alloc_internal_dynamic (size_t size, int type, gboolean assert_on_failure)
{
	int index;
	void *p;

	if (size > allocator_sizes [NUM_ALLOCATORS - 1]) {
		p = sgen_alloc_os_memory (size, SGEN_ALLOC_INTERNAL | SGEN_ALLOC_ACTIVATE, NULL);
		if (!p)
			sgen_assert_memory_alloc (NULL, size, description_for_type (type));
	} else {
		index = index_for_size (size);

		p = mono_lock_free_alloc (&allocators [index]);
		if (!p)
			sgen_assert_memory_alloc (NULL, size, description_for_type (type));
		memset (p, 0, size);
	}

	MONO_GC_INTERNAL_ALLOC ((mword)p, size, type);
	return p;
}

void
sgen_free_internal_dynamic (void *addr, size_t size, int type)
{
	if (!addr)
		return;

	if (size > allocator_sizes [NUM_ALLOCATORS - 1])
		sgen_free_os_memory (addr, size, SGEN_ALLOC_INTERNAL);
	else
		mono_lock_free_free (addr);

	MONO_GC_INTERNAL_DEALLOC ((mword)addr, size, type);
}

void*
sgen_alloc_internal (int type)
{
	int index = fixed_type_allocator_indexes [type];
	int size = allocator_sizes [index];
	void *p;
	g_assert (index >= 0 && index < NUM_ALLOCATORS);
	p = mono_lock_free_alloc (&allocators [index]);
	memset (p, 0, size);

	MONO_GC_INTERNAL_ALLOC ((mword)p, size, type);

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

	mono_lock_free_free (addr);

	if (MONO_GC_INTERNAL_DEALLOC_ENABLED ()) {
		int size G_GNUC_UNUSED = allocator_sizes [index];
		MONO_GC_INTERNAL_DEALLOC ((mword)addr, size, type);
	}
}

void
sgen_dump_internal_mem_usage (FILE *heap_dump_file)
{
	/*
	int i;

	fprintf (heap_dump_file, "<other-mem-usage type=\"large-internal\" size=\"%lld\"/>\n", large_internal_bytes_alloced);
	fprintf (heap_dump_file, "<other-mem-usage type=\"pinned-chunks\" size=\"%lld\"/>\n", pinned_chunk_bytes_alloced);
	for (i = 0; i < INTERNAL_MEM_MAX; ++i) {
		fprintf (heap_dump_file, "<other-mem-usage type=\"%s\" size=\"%ld\"/>\n",
				description_for_type (i), unmanaged_allocator.small_internal_mem_bytes [i]);
	}
	*/
}

void
sgen_report_internal_mem_usage (void)
{
	/* FIXME: implement */
	printf ("not implemented yet\n");
}

void
sgen_init_internal_allocator (void)
{
	int i;

	for (i = 0; i < INTERNAL_MEM_MAX; ++i)
		fixed_type_allocator_indexes [i] = -1;

	for (i = 0; i < NUM_ALLOCATORS; ++i) {
		mono_lock_free_allocator_init_size_class (&size_classes [i], allocator_sizes [i]);
		mono_lock_free_allocator_init_allocator (&allocators [i], &size_classes [i]);
	}
}

#endif
