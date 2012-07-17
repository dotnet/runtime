/*
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
#ifndef __MONO_SGENCONF_H__
#define __MONO_SGENCONF_H__

/*Basic defines and static tunables */

#if SIZEOF_VOID_P == 4
typedef guint32 mword;
#define MWORD_MAX_VALUE ((uint32_t) 0xffffffff)
#else
typedef guint64 mword;
#define MWORD_MAX_VALUE (G_MAXUINT64)
#endif


/*
 * Turning on heavy statistics will turn off the managed allocator and
 * the managed write barrier.
 */
//#define HEAVY_STATISTICS

/*
 * Define this to allow the user to change the nursery size by
 * specifying its value in the MONO_GC_PARAMS environmental
 * variable. See mono_gc_base_init for details.
 */
#define USER_CONFIG 1


/*
 * If this is set, the nursery is aligned to an address aligned to its size, ie.
 * a 1MB nursery will be aligned to an address divisible by 1MB. This allows us to
 * speed up ptr_in_nursery () checks which are very frequent. This requires the
 * nursery size to be a compile time constant.
 */
#define SGEN_ALIGN_NURSERY 1

/*
 * The binary protocol enables logging a lot of the GC ativity in a way that is not very
 * intrusive and produce a compact file that can be searched using a custom tool.
 *
 */
//#define SGEN_BINARY_PROTOCOL

/*
 * Define this and use the "xdomain-checks" MONO_GC_DEBUG option to
 * have cross-domain checks in the write barrier.
 */
//#define XDOMAIN_CHECKS_IN_WBARRIER

#ifndef SGEN_BINARY_PROTOCOL
#ifndef HEAVY_STATISTICS
#define MANAGED_ALLOCATION
#ifndef XDOMAIN_CHECKS_IN_WBARRIER
#define MANAGED_WBARRIER
#endif
#endif
#endif

/*
 * Maximum level of debug to enable on this build.
 * Making this a static variable enables us to put logging in a lot of places.
 * FIXME decouple logging from assertions
 */
#define SGEN_MAX_DEBUG_LEVEL 2


#define GC_BITS_PER_WORD (sizeof (mword) * 8)

/*Size of the section used by the copying GC. */
#define SGEN_SIZEOF_GC_MEM_SECTION	((sizeof (GCMemSection) + 7) & ~7)

/*
 * to quickly find the head of an object pinned by a conservative
 * address we keep track of the objects allocated for each
 * SGEN_SCAN_START_SIZE memory chunk in the nursery or other memory
 * sections. Larger values have less memory overhead and bigger
 * runtime cost. 4-8 KB are reasonable values.
 */
#define SGEN_SCAN_START_SIZE (4096*2)

/*
 * Objects bigger then this go into the large object space.  This size
 * has a few constraints.  It must fit into the major heap, which in
 * the case of the copying collector means that it must fit into a
 * pinned chunk.  It must also play well with the GC descriptors, some
 * of which (DESC_TYPE_RUN_LENGTH, DESC_TYPE_SMALL_BITMAP) encode the
 * object size.
 */
#define SGEN_MAX_SMALL_OBJ_SIZE 8000

/*
 * This is the maximum ammount of memory we're willing to waste in order to speed up allocation.
 * Wastage comes in thre forms:
 *
 * -when building the nursery fragment list, small regions are discarded;
 * -when allocating memory from a fragment if it ends up below the threshold, we remove it from the fragment list; and
 * -when allocating a new tlab, we discard the remaining space of the old one
 *
 * Increasing this value speeds up allocation but will cause more frequent nursery collections as less space will be used.
 * Descreasing this value will cause allocation to be slower since we'll have to cycle thru more fragments.
 * 512 annedoctally keeps wastage under control and doesn't impact allocation performance too much. 
*/
#define SGEN_MAX_NURSERY_WASTE 512


/* This is also the MAJOR_SECTION_SIZE for the copying major
   collector */
#define SGEN_PINNED_CHUNK_SIZE	(128 * 1024)

/*
 * Number of entries of a sequential store buffer.
 * This number represents how frequently we'll have to alloc
 * a new buffer, so it's a tradeoff of potential wasted space and
 * increased performance.
 * 
 * The current value of 1024 was probably selected because it fits a x86 page.
 * There's no history on the why's of this value besides this.
 */
#define DEFAULT_REMSET_SIZE 1024


/*
 * Minimum allowance for nursery allocations, as a multiple of the size of nursery.
 *
 * We allow at least this much allocation to happen to the major heap from multiple
 * minor collections before triggering a major collection.
 *
 * Bigger values increases throughput by allowing more garbage to sit in the major heap.
 * Smaller values leads to better memory effiency but more frequent major collections.
 */
#define SGEN_DEFAULT_ALLOWANCE_NURSERY_SIZE_RATIO 4.0

#define SGEN_MIN_ALLOWANCE_NURSERY_SIZE_RATIO 1.0
#define SGEN_MAX_ALLOWANCE_NURSERY_SIZE_RATIO 10.0

/*
 * Default ratio of memory we want to release in a major collection in relation to the the current heap size.
 *
 * A major collection target is to free a given amount of memory. This amount is a ratio of the major heap size.
 *
 * Values above 0.5 cause the heap to agressively grow when it's small and waste memory when it's big.
 * Lower values will produce more reasonable sized heaps when it's small, but will be suboptimal at large
 * sizes as they will use a small fraction only.
 *
 */
#define SGEN_DEFAULT_SAVE_TARGET_RATIO 0.5

#define SGEN_MIN_SAVE_TARGET_RATIO 0.1
#define SGEN_MAX_SAVE_TARGET_RATIO 2.0

#endif
