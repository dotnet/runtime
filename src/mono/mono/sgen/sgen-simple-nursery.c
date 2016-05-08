/*
 * sgen-simple-nursery.c: Simple always promote nursery.
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

static inline GCObject*
alloc_for_promotion (GCVTable vtable, GCObject *obj, size_t objsize, gboolean has_references)
{
	total_promoted_size += objsize;
	return major_collector.alloc_object (vtable, objsize, has_references);
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
	sgen_fragment_allocator_add (allocator, start, end);
}


/******************************************Copy/Scan functins ************************************************/

#define SGEN_SIMPLE_NURSERY

#define SERIAL_COPY_OBJECT simple_nursery_serial_copy_object
#define SERIAL_COPY_OBJECT_FROM_OBJ simple_nursery_serial_copy_object_from_obj

#include "sgen-minor-copy-object.h"
#include "sgen-minor-scan-object.h"

void
sgen_simple_nursery_init (SgenMinorCollector *collector)
{
	collector->is_split = FALSE;

	collector->alloc_for_promotion = alloc_for_promotion;

	collector->prepare_to_space = prepare_to_space;
	collector->clear_fragments = clear_fragments;
	collector->build_fragments_get_exclude_head = build_fragments_get_exclude_head;
	collector->build_fragments_release_exclude_head = build_fragments_release_exclude_head;
	collector->build_fragments_finish = build_fragments_finish;
	collector->init_nursery = init_nursery;

	FILL_MINOR_COLLECTOR_COPY_OBJECT (collector);
	FILL_MINOR_COLLECTOR_SCAN_OBJECT (collector);
}


#endif
