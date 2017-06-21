/**
 * \file
 * Simple always promote nursery.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-layout-stats.h"
#include "mono/sgen/sgen-client.h"
#include "mono/sgen/sgen-workers.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/mono-proclib.h"

static inline GCObject*
alloc_for_promotion (GCVTable vtable, GCObject *obj, size_t objsize, gboolean has_references)
{
	total_promoted_size += objsize;
	return major_collector.alloc_object (vtable, objsize, has_references);
}

static inline GCObject*
alloc_for_promotion_par (GCVTable vtable, GCObject *obj, size_t objsize, gboolean has_references)
{
	/*
	 * FIXME
	 * Note that the stat is not precise. total_promoted_size incrementing is not atomic and
	 * even in that case, the same object might be promoted simultaneously by different workers
	 * leading to one of the allocated major object to be discarded.
	 */
	total_promoted_size += objsize;
	return major_collector.alloc_object_par (vtable, objsize, has_references);
}

static SgenFragment*
build_fragments_get_exclude_head (void)
{
	return NULL;
}

static void
build_fragments_release_exclude_head (void)
{
}

static void
build_fragments_finish (SgenFragmentAllocator *allocator)
{
}

static void
prepare_to_space (char *to_space_bitmap, size_t space_bitmap_size)
{
}

static void
clear_fragments (void)
{	
}

static void
init_nursery (SgenFragmentAllocator *allocator, char *start, char *end)
{
	char *nursery_limit = sgen_nursery_start + sgen_nursery_size;

	if (start < nursery_limit && end > nursery_limit) {
		sgen_fragment_allocator_add (allocator, start, nursery_limit);
		sgen_fragment_allocator_add (allocator, nursery_limit, end);
	} else {
		sgen_fragment_allocator_add (allocator, start, end);
	}
}


/******************************************Copy/Scan functins ************************************************/

#define collector_pin_object(obj, queue) sgen_pin_object (obj, queue);
#define COLLECTOR_SERIAL_ALLOC_FOR_PROMOTION alloc_for_promotion
#define COLLECTOR_PARALLEL_ALLOC_FOR_PROMOTION alloc_for_promotion_par

#define COPY_OR_MARK_PARALLEL
#include "sgen-copy-object.h"

#define SGEN_SIMPLE_NURSERY

#include "sgen-minor-copy-object.h"
#include "sgen-minor-scan-object.h"

static void
fill_serial_ops (SgenObjectOperations *ops)
{
	ops->copy_or_mark_object = SERIAL_COPY_OBJECT;
	FILL_MINOR_COLLECTOR_SCAN_OBJECT (ops);
}

#define SGEN_SIMPLE_PAR_NURSERY

#include "sgen-minor-copy-object.h"
#include "sgen-minor-scan-object.h"

static void
fill_parallel_ops (SgenObjectOperations *ops)
{
	ops->copy_or_mark_object = SERIAL_COPY_OBJECT;
	FILL_MINOR_COLLECTOR_SCAN_OBJECT (ops);
}

#undef SGEN_SIMPLE_PAR_NURSERY
#define SGEN_CONCURRENT_MAJOR

#include "sgen-minor-copy-object.h"
#include "sgen-minor-scan-object.h"

static void
fill_serial_with_concurrent_major_ops (SgenObjectOperations *ops)
{
	ops->copy_or_mark_object = SERIAL_COPY_OBJECT;
	FILL_MINOR_COLLECTOR_SCAN_OBJECT (ops);
}

#define SGEN_SIMPLE_PAR_NURSERY

#include "sgen-minor-copy-object.h"
#include "sgen-minor-scan-object.h"

static void
fill_parallel_with_concurrent_major_ops (SgenObjectOperations *ops)
{
	ops->copy_or_mark_object = SERIAL_COPY_OBJECT;
	FILL_MINOR_COLLECTOR_SCAN_OBJECT (ops);
}

void
sgen_simple_nursery_init (SgenMinorCollector *collector, gboolean parallel)
{
	if (mono_cpu_count () <= 1)
		parallel = FALSE;

	collector->is_split = FALSE;
	collector->is_parallel = parallel;

	collector->alloc_for_promotion = alloc_for_promotion;
	collector->alloc_for_promotion_par = alloc_for_promotion_par;

	collector->prepare_to_space = prepare_to_space;
	collector->clear_fragments = clear_fragments;
	collector->build_fragments_get_exclude_head = build_fragments_get_exclude_head;
	collector->build_fragments_release_exclude_head = build_fragments_release_exclude_head;
	collector->build_fragments_finish = build_fragments_finish;
	collector->init_nursery = init_nursery;

	fill_serial_ops (&collector->serial_ops);
	fill_serial_with_concurrent_major_ops (&collector->serial_ops_with_concurrent_major);
	fill_parallel_ops (&collector->parallel_ops);
	fill_parallel_with_concurrent_major_ops (&collector->parallel_ops_with_concurrent_major);

	/*
	 * The nursery worker context is created first so it will have priority over
	 * concurrent mark and concurrent sweep.
	 */
	if (parallel)
		sgen_workers_create_context (GENERATION_NURSERY, mono_cpu_count ());
}


#endif
