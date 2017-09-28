/**
 * \file
 * Tunable parameters and debugging switches.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGENCONF_H__
#define __MONO_SGENCONF_H__

#include <glib.h>

/*Basic defines and static tunables */

#if SIZEOF_VOID_P == 4
typedef guint32 mword;
#else
typedef guint64 mword;
#endif

typedef mword SgenDescriptor;
#define SGEN_DESCRIPTOR_NULL	0

/*
 * Turning on heavy statistics will turn off the managed allocator and
 * the managed write barrier.
 */
// #define HEAVY_STATISTICS

#ifdef HEAVY_STATISTICS
#define HEAVY_STAT(x)	x
#else
#define HEAVY_STAT(x)
#endif

/*
 * The binary protocol enables logging a lot of the GC ativity in a way that is not very
 * intrusive and produces a compact file that can be searched using a custom tool.  This
 * option enables very fine-grained binary protocol events, which will make the GC a tiny
 * bit less efficient even if no binary protocol file is generated.
 */
//#define SGEN_HEAVY_BINARY_PROTOCOL

/*
 * This extends the heavy binary protocol to record the provenance of an object
 * for every allocation.
 */
//#define SGEN_OBJECT_PROVENANCE

/*
 * This enables checks whenever objects are enqueued in gray queues.
 * Right now the only check done is that we never enqueue nursery
 * pointers in the concurrent collector.
 */
//#define SGEN_CHECK_GRAY_OBJECT_ENQUEUE

/*
 * This keeps track of where a gray object queue section is and
 * whether it is where it should be.
 */
//#define SGEN_CHECK_GRAY_OBJECT_SECTIONS

/*
 * Enable this to check every reference update for null references and whether the update is
 * made in a worker thread.  In only a few cases do we potentially update references by
 * writing nulls, so we assert in all the cases where it's not allowed.  The concurrent
 * collector's worker thread is not allowed to update references at all, so we also assert
 * that we're not in the worker thread.
 */
//#define SGEN_CHECK_UPDATE_REFERENCE

/*
 * Define this and use the "xdomain-checks" MONO_GC_DEBUG option to
 * have cross-domain checks in the write barrier.
 */
//#define XDOMAIN_CHECKS_IN_WBARRIER

/*
 * Define this to get number of objects marked information in the
 * concurrent GC DTrace probes.  Has a small performance impact, so
 * it's disabled by default.
 */
//#define SGEN_COUNT_NUMBER_OF_MAJOR_OBJECTS_MARKED

/*
 * Object layout statistics gather a histogram of reference locations
 * over all scanned objects.  We use this information to improve GC
 * descriptors to speed up scanning.  This does not provide any
 * troubleshooting assistance (unless you are troubled in highly
 * unusual ways) and makes scanning slower.
 */
//#define SGEN_OBJECT_LAYOUT_STATISTICS

#ifndef SGEN_HEAVY_BINARY_PROTOCOL
#ifndef HEAVY_STATISTICS
#define MANAGED_ALLOCATION
#ifndef XDOMAIN_CHECKS_IN_WBARRIER
#define MANAGED_WBARRIER
#endif
#endif
#endif

#if defined (HOST_WASM)
#define DEFAULT_MAJOR SGEN_MAJOR_SERIAL
#define DEFAULT_SWEEP_MODE SGEN_SWEEP_SERIAL
#elif defined(HAVE_CONC_GC_AS_DEFAULT)
/* Use concurrent major on deskstop platforms */
#define DEFAULT_MAJOR SGEN_MAJOR_CONCURRENT
#define DEFAULT_SWEEP_MODE SGEN_SWEEP_CONCURRENT
#else
#define DEFAULT_MAJOR SGEN_MAJOR_SERIAL
#define DEFAULT_SWEEP_MODE SGEN_SWEEP_CONCURRENT
#endif



/*
 * Maximum level of debug to enable on this build.
 * Making this a constant enables us to put logging in a lot of places and
 * not pay its cost on release builds.
 */
#define SGEN_MAX_DEBUG_LEVEL 2

/*
 * Maximum level of asserts to enable on this build.
 * FIXME replace all magic numbers with defines.
 */
#define SGEN_MAX_ASSERT_LEVEL 5


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
 * Objects bigger then this go into the large object space.  This size has a few
 * constraints.  At least two of them must fit into a major heap block.  It must also play
 * well with the run length GC descriptor, which encodes the object size.
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
 * How much more we allow the heap to grow before triggering another major collection
 */
#define SGEN_DEFAULT_ALLOWANCE_HEAP_SIZE_RATIO 0.33

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

/*
 * Configurable cementing parameters.
 *
 * If there are too many pinned nursery objects with many references
 * from the major heap, the hash table size must be increased.
 *
 * The threshold is the number of references from the major heap to a
 * pinned nursery object which triggers cementing: if there are more
 * than that number of references, the pinned object is cemented until
 * the next major collection.
 */
#define SGEN_CEMENT_HASH_SHIFT	6
#define SGEN_CEMENT_HASH_SIZE	(1 << SGEN_CEMENT_HASH_SHIFT)
#define SGEN_CEMENT_HASH(hv)	(((hv) ^ ((hv) >> SGEN_CEMENT_HASH_SHIFT)) & (SGEN_CEMENT_HASH_SIZE - 1))
#define SGEN_CEMENT_THRESHOLD	1000

/*
 * Default values for the nursery size
 */
#define SGEN_DEFAULT_NURSERY_MIN_SIZE	(1 << 19)
#define SGEN_DEFAULT_NURSERY_SIZE	(1 << 22)
#define SGEN_DEFAULT_NURSERY_MAX_SIZE	(1 << 25)

/*
 * We are trying to keep pauses lower than this (ms). We use it for dynamic nursery
 * sizing heuristics. We are keeping leeway in order to be prepared for work-load
 * variations.
 */
#define SGEN_DEFAULT_MAX_PAUSE_TIME 30
#define SGEN_DEFAULT_MAX_PAUSE_MARGIN 0.66f


#define SGEN_PAUSE_MODE_MAX_PAUSE_MARGIN 0.5f

/*
 * In practice, for nurseries smaller than this, the parallel minor tends to be
 * ineffective, even leading to regressions. Avoid using it for smaller nurseries.
 */
#define SGEN_PARALLEL_MINOR_MIN_NURSERY_SIZE (1 << 24)

#endif
