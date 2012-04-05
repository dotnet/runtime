/*
 * sgen-splliy-nursery.c: 3-space based nursery collector.
 *
 * Author:
 *	Rodrigo Kumpera Kumpera <kumpera@gmail.com>
 *
 * SGen is licensed under the terms of the MIT X11 license
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011-2012 Xamarin Inc (http://www.xamarin.com)
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

#include "metadata/sgen-gc.h"

/*
The nursery is logically divided into 3 spaces: Allocator space and two Survivor spaces.

Objects are born (allocated by the mutator) in the Allocator Space.

The Survivor spaces are divided in a copying collector style From and To spaces.
The hole of each space switch on each collection.

On each collection we process objects from the nursery this way:
Objects from the Allocator Space are evacuated into the To Space.
Objects from the Survivor From Space are evacuated into the old generation.


The nursery is physically divided in two parts, set by the promotion barrier.

The Allocator Space takes the botton part of the nursery.

The Survivor spaces are intermingled in the top part of the nursery. It's done
this way since the required size for the To Space depends on the survivor rate
of objects from the Allocator Space. 

During a collection when the object scan function see a nursery object it must
determine if the object needs to be evacuated or left in place. Originally, this
check was done by checking is a forwarding pointer is installed, but now an object
can be in the To Space, it won't have a forwarding pointer and it must be left in place.

In order to solve that we classify nursery memory been either in the From Space or in
the To Space. Since the Allocator Space has the same behavior as the Survivor From Space
they are unified for this purpoise - a bit confusing at first.

This from/to classification is done on a larger granule than object to make the check efficient
and, due to that, we must make sure that all fragemnts used to allocate memory from the To Space
are naturally aligned in both ends to that granule to avoid wronly classifying a From Space object.

TODO:
-The promotion barrier is statically defined to 50% of the nursery, it should be dinamically adjusted based
on survival rates;
-Objects are aged just one collection, we need to implement multiple cycle aging;
-We apply the same promotion policy to all objects, finalizable ones should age longer in the nursery;
-We apply the same promotion policy to all stages of a collection, maybe we should promote more aggressively
objects from non-stack roots, specially those found in the remembered set;
-Make the new behavior runtime selectable;
-Make the new behavior have a low overhead when disabled;
-Make all new exported functions inlineable in other modules;
-Create specialized copy & scan functions for nursery collections;
-Decide if this is the right place for this code;
-Fix our major collection trigger to happen before we do a minor GC and collect the nursery only once.
*/

/*FIXME Move this to a separate header. */
#define _toi(ptr) ((size_t)ptr)
#define make_ptr_mask(bits) ((1 << bits) - 1)
#define align_down(ptr, bits) ((void*)(_toi(ptr) & ~make_ptr_mask (bits)))
#define align_up(ptr, bits) ((void*) ((_toi(ptr) + make_ptr_mask (bits)) & ~make_ptr_mask (bits)))

/* Limits the ammount of memory the mutator can have. */
static char *promotion_barrier;

/* The collector allocs from here. */
static SgenFragmentAllocator collector_allocator;


static inline void
mark_bit (char *space_bitmap, char *pos)
{
	int idx = (pos - sgen_nursery_start) >> SGEN_TO_SPACE_GRANULE_BITS;
	int byte = idx / 8;
	int bit = idx & 0x7;

	g_assert (byte < sgen_space_bitmap_size);
	space_bitmap [byte] |= 1 << bit;
}

static void
mark_bits_in_range (char *space_bitmap, char *start, char *end)
{
	start = align_down (start, SGEN_TO_SPACE_GRANULE_BITS);
	end = align_up (end, SGEN_TO_SPACE_GRANULE_BITS);

	for (;start <= end; start += SGEN_TO_SPACE_GRANULE_IN_BYTES)
		mark_bit (space_bitmap, start);
}

static void
fragment_list_split (SgenFragmentAllocator *allocator)
{
	SgenFragment *prev = NULL, *list = allocator->region_head;

	while (list) {
		if (list->fragment_end > promotion_barrier) {
			if (list->fragment_start < promotion_barrier) {
				SgenFragment *res = sgen_fragment_allocator_alloc ();

				res->fragment_start = promotion_barrier;
				res->fragment_next = promotion_barrier;
				res->fragment_end = list->fragment_end;
				res->next = list->next;
				res->next_in_order = list->next_in_order;
				g_assert (res->fragment_end > res->fragment_start);

				list->fragment_end = promotion_barrier;
				list->next = list->next_in_order = NULL;

				allocator->region_head = allocator->alloc_head = res;
				return;
			} else {
				if (prev)
					prev->next = prev->next_in_order = NULL;
				allocator->region_head = allocator->alloc_head = list;
				return;
			}
		}
		prev = list;
		list = list->next;
	}
	allocator->region_head = allocator->alloc_head = NULL;
}

/******************************************Minor Collector API ************************************************/
static char*
alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	char *p;

	if (objsize > SGEN_MAX_SMALL_OBJ_SIZE)
		g_error ("asked to allocate object size %d\n", objsize);

	/*This one will be internally promoted. */
	if (obj >= sgen_nursery_start && obj < promotion_barrier) {
		p = sgen_fragment_allocator_serial_alloc (&collector_allocator, objsize);

		/* Have we failed to promote to the nursery, lets just evacuate it to old gen. */
		if (!p)
			p = major_collector.alloc_object (objsize, has_references);
	} else {
		p = major_collector.alloc_object (objsize, has_references);
	}

	return p;
}

static char*
par_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	char *p;

	/*This one will be internally promoted. */
	if (obj >= sgen_nursery_start && obj < promotion_barrier) {
		p = sgen_fragment_allocator_par_alloc (&collector_allocator, objsize);

		/* Have we failed to promote to the nursery, lets just evacuate it to old gen. */
		if (!p)
			p = major_collector.par_alloc_object (objsize, has_references);			
	} else {
		p = major_collector.par_alloc_object (objsize, has_references);
	}

	return p;
}

static SgenFragment*
build_fragments_get_exclude_head (void)
{
	return collector_allocator.region_head;
}

static void
build_fragments_release_exclude_head (void)
{
	sgen_fragment_allocator_release (&collector_allocator);
}

static void
build_fragments_finish (SgenFragmentAllocator *allocator)
{
	/* We split the fragment list based on the promotion barrier. */
	collector_allocator = *allocator;
	fragment_list_split (allocator);
}

static void
prepare_to_space (char *to_space_bitmap, int space_bitmap_size)
{
	SgenFragment **previous, *frag;

	memset (to_space_bitmap, 0, space_bitmap_size);

	previous = &collector_allocator.alloc_head;

	for (frag = *previous; frag; frag = *previous) {
		char *start = align_up (frag->fragment_next, SGEN_TO_SPACE_GRANULE_BITS);
		char *end = align_down (frag->fragment_end, SGEN_TO_SPACE_GRANULE_BITS);

		/* Fragment is too small to be usable. */
		if ((end - start) < SGEN_MAX_NURSERY_WASTE) {
			sgen_clear_range (frag->fragment_next, frag->fragment_end);
			frag->fragment_next = frag->fragment_end = frag->fragment_start;
			*previous = frag->next;
			continue;
		}

		sgen_clear_range (frag->fragment_next, start);
		sgen_clear_range (end, frag->fragment_end);

		frag->fragment_start = frag->fragment_next = start;
		frag->fragment_end = end;
		mark_bits_in_range (to_space_bitmap, start, end);
		previous = &frag->next;
	}
}

static void
clear_fragments (void)
{
	sgen_clear_allocator_fragments (&collector_allocator);
}

static void
init_nursery (SgenFragmentAllocator *allocator, char *start, char *end)
{
	char *middle = start + (end - start) / 2;
	sgen_fragment_allocator_add (allocator, start, middle);
	sgen_fragment_allocator_add (allocator, middle, end);

	promotion_barrier = middle;
}

void
sgen_split_nursery_init (SgenMinorCollector *collector)
{
	collector->alloc_for_promotion = alloc_for_promotion;
	collector->par_alloc_for_promotion = par_alloc_for_promotion;

	collector->prepare_to_space = prepare_to_space;
	collector->clear_fragments = clear_fragments;
	collector->build_fragments_get_exclude_head = build_fragments_get_exclude_head;
	collector->build_fragments_release_exclude_head = build_fragments_release_exclude_head;
	collector->build_fragments_finish = build_fragments_finish;
	collector->init_nursery = init_nursery;
}


#endif