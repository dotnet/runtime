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

#include "metadata/profiler-private.h"

#include "metadata/sgen-gc.h"
#include "metadata/sgen-protocol.h"
#include "utils/mono-memory-model.h"

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
check was done by checking if a forwarding pointer is installed, but now an object
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
-We apply the same promotion policy to all objects, finalizable ones should age longer in the nursery;
-We apply the same promotion policy to all stages of a collection, maybe we should promote more aggressively
objects from non-stack roots, specially those found in the remembered set;
-Fix our major collection trigger to happen before we do a minor GC and collect the nursery only once.
-Make the serial fragment allocator fast path inlineable
-Make aging threshold be based on survival rates and survivor occupancy;
-Change promotion barrier to be size and not address based;
-Pre allocate memory for young ages to make sure that on overflow only the older suffer;
-Get rid of par_alloc_buffer_refill_mutex so to the parallel collection of the nursery doesn't suck;
*/

/*FIXME Move this to a separate header. */
#define _toi(ptr) ((size_t)ptr)
#define make_ptr_mask(bits) ((1 << bits) - 1)
#define align_down(ptr, bits) ((void*)(_toi(ptr) & ~make_ptr_mask (bits)))
#define align_up(ptr, bits) ((void*) ((_toi(ptr) + make_ptr_mask (bits)) & ~make_ptr_mask (bits)))

/*
Even though the effective max age is 255, aging that much doesn't make sense.
It might even make sense to use nimbles for age recording.
*/
#define MAX_AGE 15

/*
 * Each age has its allocation buffer.  Whenever an object is to be
 * aged we try to fit it into its new age's allocation buffer.  If
 * that is not possible we get new space from the fragment allocator
 * and set the allocation buffer to that space (minus the space
 * required for the object).
 */

typedef struct {
	char *next;
	char *end;
} AgeAllocationBuffer;

/* Limits the ammount of memory the mutator can have. */
static char *promotion_barrier;

/*
Promotion age and alloc ratio are the two nursery knobs to control
how much effort we want to spend on young objects.

Allocation ratio should be the inverse of the expected survivor rate.
The more objects surviver, the smaller the alloc ratio much be so we can
age all objects.

Promote age depends on how much effort we want to spend aging objects before
we promote them to the old generation. If addional ages don't somewhat improve
mortality, it's better avoid as they increase the cost of minor collections.

*/


/*
If we're evacuating an object with this age or more, promote it.
Age is the number of surviving collections of an object.
*/
static int promote_age = 2;

/*
Initial ratio of allocation and survivor spaces.
This should be read as the fraction of the whole nursery dedicated
for the allocator space.
*/
static float alloc_ratio = 60.f/100.f;


static char *region_age;
static int region_age_size;
static AgeAllocationBuffer age_alloc_buffers [MAX_AGE];

/* The collector allocs from here. */
static SgenFragmentAllocator collector_allocator;

static LOCK_DECLARE (par_alloc_buffer_refill_mutex);

static inline int
get_object_age (char *object)
{
	int idx = (object - sgen_nursery_start) >> SGEN_TO_SPACE_GRANULE_BITS;
	return region_age [idx];
}

static inline void
set_object_age (char *object, int age)
{
	int idx = (object - sgen_nursery_start) >> SGEN_TO_SPACE_GRANULE_BITS;
	region_age [idx] = age;
}

static void
set_age_in_range (char *start, char *end, int age)
{
	char *region_start;
	int region_idx, length;
	region_idx = (start - sgen_nursery_start) >> SGEN_TO_SPACE_GRANULE_BITS;
	region_start = &region_age [region_idx];
	length = (end - start) >> SGEN_TO_SPACE_GRANULE_BITS;
	memset (region_start, age, length);
}

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

	for (;start < end; start += SGEN_TO_SPACE_GRANULE_IN_BYTES)
		mark_bit (space_bitmap, start);
}

/*
 * This splits the fragments at the point of the promotion barrier.
 * Two allocator are actually involved here: The mutator allocator and
 * the collector allocator.  This function is called with the
 * collector, but it's a copy of the mutator allocator and contains
 * all the fragments in the nursery.  The fragments below the
 * promotion barrier are left with the mutator allocator and the ones
 * above are put into the collector allocator.
 */
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
				set_age_in_range (list->fragment_start, list->fragment_end, 0);

				allocator->region_head = allocator->alloc_head = res;
				return;
			} else {
				if (prev)
					prev->next = prev->next_in_order = NULL;
				allocator->region_head = allocator->alloc_head = list;
				return;
			}
		}
		set_age_in_range (list->fragment_start, list->fragment_end, 0);
		prev = list;
		list = list->next;
	}
	allocator->region_head = allocator->alloc_head = NULL;
}

/******************************************Minor Collector API ************************************************/

#define AGE_ALLOC_BUFFER_MIN_SIZE SGEN_TO_SPACE_GRANULE_IN_BYTES
#define AGE_ALLOC_BUFFER_DESIRED_SIZE (SGEN_TO_SPACE_GRANULE_IN_BYTES * 8)

static char*
alloc_for_promotion_slow_path (int age, size_t objsize)
{
	char *p;
	size_t allocated_size;
	size_t aligned_objsize = (size_t)align_up (objsize, SGEN_TO_SPACE_GRANULE_BITS);

	p = sgen_fragment_allocator_serial_range_alloc (
		&collector_allocator,
		MAX (aligned_objsize, AGE_ALLOC_BUFFER_DESIRED_SIZE),
		MAX (aligned_objsize, AGE_ALLOC_BUFFER_MIN_SIZE),
		&allocated_size);
	if (p) {
		set_age_in_range (p, p + allocated_size, age);
		sgen_clear_range (age_alloc_buffers [age].next, age_alloc_buffers [age].end);
		age_alloc_buffers [age].next = p + objsize;
		age_alloc_buffers [age].end = p + allocated_size;
	}
	return p;
}

static inline char*
alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	char *p = NULL;
	int age;

	age = get_object_age (obj);
	if (age >= promote_age)
		return major_collector.alloc_object (objsize, has_references);

	/* Promote! */
	++age;

	p = age_alloc_buffers [age].next;
	if (G_LIKELY (p + objsize <= age_alloc_buffers [age].end)) {
        age_alloc_buffers [age].next += objsize;
	} else {
		p = alloc_for_promotion_slow_path (age, objsize);
		if (!p)
			p = major_collector.alloc_object (objsize, has_references);
	}

	return p;
}

static char*
par_alloc_for_promotion_slow_path (int age, size_t objsize)
{
	char *p;
	size_t allocated_size;
	size_t aligned_objsize = (size_t)align_up (objsize, SGEN_TO_SPACE_GRANULE_BITS);

	mono_mutex_lock (&par_alloc_buffer_refill_mutex);

restart:
	p = age_alloc_buffers [age].next;
	if (G_LIKELY (p + objsize <= age_alloc_buffers [age].end)) {
		if (SGEN_CAS_PTR ((void*)&age_alloc_buffers [age].next, p + objsize, p) != p)
			goto restart;
	} else {
		/* Reclaim remaining space - if we OOMd the nursery nothing to see here. */
		char *end = age_alloc_buffers [age].end;
		if (end) {
			do {
				p = age_alloc_buffers [age].next;
			} while (SGEN_CAS_PTR ((void*)&age_alloc_buffers [age].next, end, p) != p);
				sgen_clear_range (p, end);
		}

		/* By setting end to NULL we make sure no other thread can advance while we're updating.*/
		age_alloc_buffers [age].end = NULL;
		STORE_STORE_FENCE;

		p = sgen_fragment_allocator_par_range_alloc (
			&collector_allocator,
			MAX (aligned_objsize, AGE_ALLOC_BUFFER_DESIRED_SIZE),
			MAX (aligned_objsize, AGE_ALLOC_BUFFER_MIN_SIZE),
			&allocated_size);
		if (p) {
			set_age_in_range (p, p + allocated_size, age);
			age_alloc_buffers [age].next = p + objsize;
			STORE_STORE_FENCE; /* Next must arrive before the new value for next. */
			age_alloc_buffers [age].end = p + allocated_size;
		}
	}

	mono_mutex_unlock (&par_alloc_buffer_refill_mutex);
	return p;
}

static inline char*
par_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	char *p;
	int age;

	age = get_object_age (obj);
	if (age >= promote_age)
		return major_collector.par_alloc_object (objsize, has_references);

restart:
	p = age_alloc_buffers [age].next;

	LOAD_LOAD_FENCE; /* The read of ->next must happen before ->end */

	if (G_LIKELY (p + objsize <= age_alloc_buffers [age].end)) {
		if (SGEN_CAS_PTR ((void*)&age_alloc_buffers [age].next, p + objsize, p) != p)
			goto restart;
	} else {
		p = par_alloc_for_promotion_slow_path (age, objsize);

		/* Have we failed to promote to the nursery, lets just evacuate it to old gen. */
		if (!p)
			p = major_collector.par_alloc_object (objsize, has_references);			
	}

	return p;
}

static char*
minor_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	/*
	We only need to check for a non-nursery object if we're doing a major collection.
	*/
	if (!sgen_ptr_in_nursery (obj))
		return major_collector.alloc_object (objsize, has_references);

	return alloc_for_promotion (obj, objsize, has_references);
}

static char*
minor_par_alloc_for_promotion (char *obj, size_t objsize, gboolean has_references)
{
	/*
	We only need to check for a non-nursery object if we're doing a major collection.
	*/
	if (!sgen_ptr_in_nursery (obj))
		return major_collector.par_alloc_object (objsize, has_references);

	return par_alloc_for_promotion (obj, objsize, has_references);
}

static SgenFragment*
build_fragments_get_exclude_head (void)
{
	int i;
	for (i = 0; i < MAX_AGE; ++i) {
		/*If we OOM'd on the last collection ->end might be null while ->next not.*/
		if (age_alloc_buffers [i].end)
			sgen_clear_range (age_alloc_buffers [i].next, age_alloc_buffers [i].end);
	}

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
	fragment_list_split (&collector_allocator);
}

static void
prepare_to_space (char *to_space_bitmap, int space_bitmap_size)
{
	SgenFragment **previous, *frag;

	memset (to_space_bitmap, 0, space_bitmap_size);
	memset (age_alloc_buffers, 0, sizeof (age_alloc_buffers));

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

		/*
		We need to insert 3 phony objects so the fragments build step can correctly
		walk the nursery.
		*/

		/* Clean the fragment range. */
		sgen_clear_range (start, end);
		/* We need a phony object in between the original fragment start and the effective one. */
		if (start != frag->fragment_next)
			sgen_clear_range (frag->fragment_next, start);
		/* We need an phony object in between the new fragment end and the original fragment end. */
		if (end != frag->fragment_end)
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
	int alloc_quote = (int)((end - start) * alloc_ratio);
	promotion_barrier = align_down (start + alloc_quote, 3);
	sgen_fragment_allocator_add (allocator, start, promotion_barrier);
	sgen_fragment_allocator_add (&collector_allocator, promotion_barrier, end);

	region_age_size = (end - start) >> SGEN_TO_SPACE_GRANULE_BITS;
	region_age = g_malloc0 (region_age_size);
}

static gboolean
handle_gc_param (const char *opt)
{
	if (g_str_has_prefix (opt, "alloc-ratio=")) {
		const char *arg = strchr (opt, '=') + 1;
		int percentage = atoi (arg);
		if (percentage < 1 || percentage > 100) {
			fprintf (stderr, "alloc-ratio must be an integer in the range 1-100.\n");
			exit (1);
		}
		alloc_ratio = (float)percentage / 100.0f;
		return TRUE;
	}

	if (g_str_has_prefix (opt, "promotion-age=")) {
		const char *arg = strchr (opt, '=') + 1;
		promote_age = atoi (arg);
		if (promote_age < 1 || promote_age >= MAX_AGE) {
			fprintf (stderr, "promotion-age must be an integer in the range 1-%d.\n", MAX_AGE - 1);
			exit (1);
		}
		return TRUE;
	}
	return FALSE;
}

static void
print_gc_param_usage (void)
{
	fprintf (stderr,
			""
			"  alloc-ratio=P (where P is a percentage, an integer in 1-100)\n"
			"  promotion-age=P (where P is a number, an integer in 1-%d)\n",
			MAX_AGE - 1
			);
}

/******************************************Copy/Scan functins ************************************************/

#define SGEN_SPLIT_NURSERY

#include "sgen-minor-copy-object.h"
#include "sgen-minor-scan-object.h"


void
sgen_split_nursery_init (SgenMinorCollector *collector)
{
	collector->alloc_for_promotion = minor_alloc_for_promotion;
	collector->par_alloc_for_promotion = minor_par_alloc_for_promotion;

	collector->prepare_to_space = prepare_to_space;
	collector->clear_fragments = clear_fragments;
	collector->build_fragments_get_exclude_head = build_fragments_get_exclude_head;
	collector->build_fragments_release_exclude_head = build_fragments_release_exclude_head;
	collector->build_fragments_finish = build_fragments_finish;
	collector->init_nursery = init_nursery;
	collector->handle_gc_param = handle_gc_param;
	collector->print_gc_param_usage = print_gc_param_usage;

	FILL_MINOR_COLLECTOR_COPY_OBJECT (collector);
	FILL_MINOR_COLLECTOR_SCAN_OBJECT (collector);
	LOCK_INIT (par_alloc_buffer_refill_mutex);
}


#endif
