/*
 * sgen-simple-nursery.c: Simple always promote nursery.
 *
 *
 * SGen is licensed under the terms of the MIT X11 license
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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

#include "metadata/profiler-private.h"

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"

static inline char*
alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	return major_collector.alloc_object (objsize, has_references);
}

static inline char*
par_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	return major_collector.par_alloc_object (objsize, has_references);
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
prepare_to_space (char *to_space_bitmap, int space_bitmap_size)
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

#include "sgen-minor-copy-object.h"
#include "sgen-minor-scan-object.h"

void
sgen_simple_nursery_init (SgenMinorCollector *collector)
{
	collector->alloc_for_promotion = alloc_for_promotion;
	collector->par_alloc_for_promotion = par_alloc_for_promotion;

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
