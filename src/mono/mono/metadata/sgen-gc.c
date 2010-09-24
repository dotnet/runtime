/*
 * sgen-gc.c: Simple generational GC.
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2005-2010 Novell, Inc (http://www.novell.com)
 *
 * Thread start/stop adapted from Boehm's GC:
 * Copyright (c) 1994 by Xerox Corporation.  All rights reserved.
 * Copyright (c) 1996 by Silicon Graphics.  All rights reserved.
 * Copyright (c) 1998 by Fergus Henderson.  All rights reserved.
 * Copyright (c) 2000-2004 by Hewlett-Packard Company.  All rights reserved.
 *
 * THIS MATERIAL IS PROVIDED AS IS, WITH ABSOLUTELY NO WARRANTY EXPRESSED
 * OR IMPLIED.  ANY USE IS AT YOUR OWN RISK.
 *
 * Permission is hereby granted to use or copy this program
 * for any purpose,  provided the above notices are retained on all copies.
 * Permission to modify the code and to distribute modified code is granted,
 * provided the above notices are retained, and a notice that the code was
 * modified is included with the above copyright notice.
 *
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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
 *
 *
 * Important: allocation provides always zeroed memory, having to do
 * a memset after allocation is deadly for performance.
 * Memory usage at startup is currently as follows:
 * 64 KB pinned space
 * 64 KB internal space
 * size of nursery
 * We should provide a small memory config with half the sizes
 *
 * We currently try to make as few mono assumptions as possible:
 * 1) 2-word header with no GC pointers in it (first vtable, second to store the
 *    forwarding ptr)
 * 2) gc descriptor is the second word in the vtable (first word in the class)
 * 3) 8 byte alignment is the minimum and enough (not true for special structures (SIMD), FIXME)
 * 4) there is a function to get an object's size and the number of
 *    elements in an array.
 * 5) we know the special way bounds are allocated for complex arrays
 * 6) we know about proxies and how to treat them when domains are unloaded
 *
 * Always try to keep stack usage to a minimum: no recursive behaviour
 * and no large stack allocs.
 *
 * General description.
 * Objects are initially allocated in a nursery using a fast bump-pointer technique.
 * When the nursery is full we start a nursery collection: this is performed with a
 * copying GC.
 * When the old generation is full we start a copying GC of the old generation as well:
 * this will be changed to mark&sweep with copying when fragmentation becomes to severe
 * in the future.  Maybe we'll even do both during the same collection like IMMIX.
 *
 * The things that complicate this description are:
 * *) pinned objects: we can't move them so we need to keep track of them
 * *) no precise info of the thread stacks and registers: we need to be able to
 *    quickly find the objects that may be referenced conservatively and pin them
 *    (this makes the first issues more important)
 * *) large objects are too expensive to be dealt with using copying GC: we handle them
 *    with mark/sweep during major collections
 * *) some objects need to not move even if they are small (interned strings, Type handles):
 *    we use mark/sweep for them, too: they are not allocated in the nursery, but inside
 *    PinnedChunks regions
 */

/*
 * TODO:

 *) we could have a function pointer in MonoClass to implement
  customized write barriers for value types

 *) investigate the stuff needed to advance a thread to a GC-safe
  point (single-stepping, read from unmapped memory etc) and implement it.
  This would enable us to inline allocations and write barriers, for example,
  or at least parts of them, like the write barrier checks.
  We may need this also for handling precise info on stacks, even simple things
  as having uninitialized data on the stack and having to wait for the prolog
  to zero it. Not an issue for the last frame that we scan conservatively.
  We could always not trust the value in the slots anyway.

 *) modify the jit to save info about references in stack locations:
  this can be done just for locals as a start, so that at least
  part of the stack is handled precisely.

 *) test/fix endianess issues

 *) Implement a card table as the write barrier instead of remembered
    sets?  Card tables are not easy to implement with our current
    memory layout.  We have several different kinds of major heap
    objects: Small objects in regular blocks, small objects in pinned
    chunks and LOS objects.  If we just have a pointer we have no way
    to tell which kind of object it points into, therefore we cannot
    know where its card table is.  The least we have to do to make
    this happen is to get rid of write barriers for indirect stores.
    (See next item)

 *) Get rid of write barriers for indirect stores.  We can do this by
    telling the GC to wbarrier-register an object once we do an ldloca
    or ldelema on it, and to unregister it once it's not used anymore
    (it can only travel downwards on the stack).  The problem with
    unregistering is that it needs to happen eventually no matter
    what, even if exceptions are thrown, the thread aborts, etc.
    Rodrigo suggested that we could do only the registering part and
    let the collector find out (pessimistically) when it's safe to
    unregister, namely when the stack pointer of the thread that
    registered the object is higher than it was when the registering
    happened.  This might make for a good first implementation to get
    some data on performance.

 *) Some sort of blacklist support?  Blacklists is a concept from the
    Boehm GC: if during a conservative scan we find pointers to an
    area which we might use as heap, we mark that area as unusable, so
    pointer retention by random pinning pointers is reduced.

 *) experiment with max small object size (very small right now - 2kb,
    because it's tied to the max freelist size)

  *) add an option to mmap the whole heap in one chunk: it makes for many
     simplifications in the checks (put the nursery at the top and just use a single
     check for inclusion/exclusion): the issue this has is that on 32 bit systems it's
     not flexible (too much of the address space may be used by default or we can't
     increase the heap as needed) and we'd need a race-free mechanism to return memory
     back to the system (mprotect(PROT_NONE) will still keep the memory allocated if it
     was written to, munmap is needed, but the following mmap may not find the same segment
     free...)

 *) memzero the major fragments after restarting the world and optionally a smaller
    chunk at a time

 *) investigate having fragment zeroing threads

 *) separate locks for finalization and other minor stuff to reduce
    lock contention

 *) try a different copying order to improve memory locality

 *) a thread abort after a store but before the write barrier will
    prevent the write barrier from executing

 *) specialized dynamically generated markers/copiers

 *) Dynamically adjust TLAB size to the number of threads.  If we have
    too many threads that do allocation, we might need smaller TLABs,
    and we might get better performance with larger TLABs if we only
    have a handful of threads.  We could sum up the space left in all
    assigned TLABs and if that's more than some percentage of the
    nursery size, reduce the TLAB size.

 *) Explore placing unreachable objects on unused nursery memory.
	Instead of memset'ng a region to zero, place an int[] covering it.
	A good place to start is add_nursery_frag. The tricky thing here is
	placing those objects atomically outside of a collection.


 */
#include "config.h"
#ifdef HAVE_SGEN_GC

#include <unistd.h>
#include <stdio.h>
#include <string.h>
#include <semaphore.h>
#include <signal.h>
#include <errno.h>
#include <assert.h>
#ifdef __MACH__
#undef _XOPEN_SOURCE
#endif
#include <pthread.h>
#ifdef __MACH__
#define _XOPEN_SOURCE
#endif
#include "metadata/metadata-internals.h"
#include "metadata/class-internals.h"
#include "metadata/gc-internal.h"
#include "metadata/object-internals.h"
#include "metadata/threads.h"
#include "metadata/sgen-gc.h"
#include "metadata/sgen-cardtable.h"
#include "metadata/sgen-archdep.h"
#include "metadata/mono-gc.h"
#include "metadata/method-builder.h"
#include "metadata/profiler-private.h"
#include "metadata/monitor.h"
#include "metadata/threadpool-internals.h"
#include "metadata/mempool-internals.h"
#include "metadata/marshal.h"
#include "utils/mono-mmap.h"
#include "utils/mono-time.h"
#include "utils/mono-semaphore.h"
#include "utils/mono-counters.h"
#include "utils/mono-proclib.h"

#include <mono/utils/memcheck.h>

#if defined(__MACH__)
#include "utils/mach-support.h"
#endif

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LAST
};

#undef OPDEF

#undef pthread_create
#undef pthread_join
#undef pthread_detach

/*
 * ######################################################################
 * ########  Types and constants used by the GC.
 * ######################################################################
 */

static int gc_initialized = 0;
/* If set, do a minor collection before every allocation */
static gboolean collect_before_allocs = FALSE;
/* If set, do a heap consistency check before each minor collection */
static gboolean consistency_check_at_minor_collection = FALSE;
/* If set, check that there are no references to the domain left at domain unload */
static gboolean xdomain_checks = FALSE;
/* If not null, dump the heap after each collection into this file */
static FILE *heap_dump_file = NULL;
/* If set, mark stacks conservatively, even if precise marking is possible */
static gboolean conservative_stack_mark = TRUE;
/* If set, do a plausibility check on the scan_starts before and after
   each collection */
static gboolean do_scan_starts_check = FALSE;

#ifdef HEAVY_STATISTICS
static long long stat_objects_alloced = 0;
static long long stat_bytes_alloced = 0;
long long stat_objects_alloced_degraded = 0;
long long stat_bytes_alloced_degraded = 0;
static long long stat_bytes_alloced_los = 0;

long long stat_copy_object_called_nursery = 0;
long long stat_objects_copied_nursery = 0;
long long stat_copy_object_called_major = 0;
long long stat_objects_copied_major = 0;

long long stat_scan_object_called_nursery = 0;
long long stat_scan_object_called_major = 0;

long long stat_nursery_copy_object_failed_from_space = 0;
long long stat_nursery_copy_object_failed_forwarded = 0;
long long stat_nursery_copy_object_failed_pinned = 0;

static long long stat_store_remsets = 0;
static long long stat_store_remsets_unique = 0;
static long long stat_saved_remsets_1 = 0;
static long long stat_saved_remsets_2 = 0;
static long long stat_local_remsets_processed = 0;
static long long stat_global_remsets_added = 0;
static long long stat_global_remsets_readded = 0;
static long long stat_global_remsets_processed = 0;
static long long stat_global_remsets_discarded = 0;

static long long stat_wasted_fragments_used = 0;
static long long stat_wasted_fragments_bytes = 0;

static int stat_wbarrier_set_field = 0;
static int stat_wbarrier_set_arrayref = 0;
static int stat_wbarrier_arrayref_copy = 0;
static int stat_wbarrier_generic_store = 0;
static int stat_wbarrier_generic_store_remset = 0;
static int stat_wbarrier_set_root = 0;
static int stat_wbarrier_value_copy = 0;
static int stat_wbarrier_object_copy = 0;
#endif

static long long time_minor_pre_collection_fragment_clear = 0;
static long long time_minor_pinning = 0;
static long long time_minor_scan_remsets = 0;
static long long time_minor_scan_card_table = 0;
static long long time_minor_scan_pinned = 0;
static long long time_minor_scan_registered_roots = 0;
static long long time_minor_scan_thread_data = 0;
static long long time_minor_finish_gray_stack = 0;
static long long time_minor_fragment_creation = 0;

static long long time_major_pre_collection_fragment_clear = 0;
static long long time_major_pinning = 0;
static long long time_major_scan_pinned = 0;
static long long time_major_scan_registered_roots = 0;
static long long time_major_scan_thread_data = 0;
static long long time_major_scan_alloc_pinned = 0;
static long long time_major_scan_finalized = 0;
static long long time_major_scan_big_objects = 0;
static long long time_major_finish_gray_stack = 0;
static long long time_major_free_bigobjs = 0;
static long long time_major_los_sweep = 0;
static long long time_major_sweep = 0;
static long long time_major_fragment_creation = 0;

#define DEBUG(level,a) do {if (G_UNLIKELY ((level) <= SGEN_MAX_DEBUG_LEVEL && (level) <= gc_debug_level)) a;} while (0)

static int gc_debug_level = 0;
static FILE* gc_debug_file;

/*
void
mono_gc_flush_info (void)
{
	fflush (gc_debug_file);
}
*/

/*
 * Define this to allow the user to change the nursery size by
 * specifying its value in the MONO_GC_PARAMS environmental
 * variable. See mono_gc_base_init for details.
 */
#define USER_CONFIG 1

#define TV_DECLARE(name) gint64 name
#define TV_GETTIME(tv) tv = mono_100ns_ticks ()
#define TV_ELAPSED(start,end) (int)((end-start) / 10)
#define TV_ELAPSED_MS(start,end) ((TV_ELAPSED((start),(end)) + 500) / 1000)

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

/* The method used to clear the nursery */
/* Clearing at nursery collections is the safest, but has bad interactions with caches.
 * Clearing at TLAB creation is much faster, but more complex and it might expose hard
 * to find bugs.
 */
typedef enum {
	CLEAR_AT_GC,
	CLEAR_AT_TLAB_CREATION
} NurseryClearPolicy;

static NurseryClearPolicy nursery_clear_policy = CLEAR_AT_TLAB_CREATION;

/*
 * The young generation is divided into fragments. This is because
 * we can hand one fragments to a thread for lock-less fast alloc and
 * because the young generation ends up fragmented anyway by pinned objects.
 * Once a collection is done, a list of fragments is created. When doing
 * thread local alloc we use smallish nurseries so we allow new threads to
 * allocate memory from gen0 without triggering a collection. Threads that
 * are found to allocate lots of memory are given bigger fragments. This
 * should make the finalizer thread use little nursery memory after a while.
 * We should start assigning threads very small fragments: if there are many
 * threads the nursery will be full of reserved space that the threads may not
 * use at all, slowing down allocation speed.
 * Thread local allocation is done from areas of memory Hotspot calls Thread Local 
 * Allocation Buffers (TLABs).
 */
typedef struct _Fragment Fragment;

struct _Fragment {
	Fragment *next;
	char *fragment_start;
	char *fragment_limit; /* the current soft limit for allocation */
	char *fragment_end;
};

/* the runtime can register areas of memory as roots: we keep two lists of roots,
 * a pinned root set for conservatively scanned roots and a normal one for
 * precisely scanned roots (currently implemented as a single list).
 */
typedef struct _RootRecord RootRecord;
struct _RootRecord {
	RootRecord *next;
	char *start_root;
	char *end_root;
	mword root_desc;
};

/*
 * We're never actually using the first element.  It's always set to
 * NULL to simplify the elimination of consecutive duplicate
 * entries.
 */
#define STORE_REMSET_BUFFER_SIZE	1024

typedef struct _GenericStoreRememberedSet GenericStoreRememberedSet;
struct _GenericStoreRememberedSet {
	GenericStoreRememberedSet *next;
	/* We need one entry less because the first entry of store
	   remset buffers is always a dummy and we don't copy it. */
	gpointer data [STORE_REMSET_BUFFER_SIZE - 1];
};

/* we have 4 possible values in the low 2 bits */
enum {
	REMSET_LOCATION, /* just a pointer to the exact location */
	REMSET_RANGE,    /* range of pointer fields */
	REMSET_OBJECT,   /* mark all the object for scanning */
	REMSET_VTYPE,    /* a valuetype array described by a gc descriptor and a count */
	REMSET_TYPE_MASK = 0x3
};

#ifdef HAVE_KW_THREAD
static __thread RememberedSet *remembered_set MONO_TLS_FAST;
#endif
static pthread_key_t remembered_set_key;
static RememberedSet *global_remset;
static RememberedSet *freed_thread_remsets;
static GenericStoreRememberedSet *generic_store_remsets = NULL;

/*A two slots cache for recently inserted remsets */
static gpointer global_remset_cache [2];

/* FIXME: later choose a size that takes into account the RememberedSet struct
 * and doesn't waste any alloc paddin space.
 */
#define DEFAULT_REMSET_SIZE 1024
static RememberedSet* alloc_remset (int size, gpointer id);

#define object_is_forwarded	SGEN_OBJECT_IS_FORWARDED
#define object_is_pinned	SGEN_OBJECT_IS_PINNED
#define pin_object		SGEN_PIN_OBJECT
#define unpin_object		SGEN_UNPIN_OBJECT

#define ptr_in_nursery(p)	(SGEN_PTR_IN_NURSERY ((p), DEFAULT_NURSERY_BITS, nursery_start, nursery_real_end))

#define LOAD_VTABLE	SGEN_LOAD_VTABLE

static const char*
safe_name (void* obj)
{
	MonoVTable *vt = (MonoVTable*)LOAD_VTABLE (obj);
	return vt->klass->name;
}

#define safe_object_get_size	mono_sgen_safe_object_get_size

/*
 * ######################################################################
 * ########  Global data.
 * ######################################################################
 */
static LOCK_DECLARE (gc_mutex);
static int gc_disabled = 0;
static int num_minor_gcs = 0;
static int num_major_gcs = 0;

static gboolean use_cardtable;

#ifdef USER_CONFIG

/* good sizes are 512KB-1MB: larger ones increase a lot memzeroing time */
#define DEFAULT_NURSERY_SIZE (default_nursery_size)
static int default_nursery_size = (1 << 22);
#ifdef SGEN_ALIGN_NURSERY
/* The number of trailing 0 bits in DEFAULT_NURSERY_SIZE */
#define DEFAULT_NURSERY_BITS (default_nursery_bits)
static int default_nursery_bits = 22;
#endif

#else

#define DEFAULT_NURSERY_SIZE (4*1024*1024)
#ifdef SGEN_ALIGN_NURSERY
#define DEFAULT_NURSERY_BITS 22
#endif

#endif

#ifndef SGEN_ALIGN_NURSERY
#define DEFAULT_NURSERY_BITS -1
#endif

#define MIN_MINOR_COLLECTION_ALLOWANCE	(DEFAULT_NURSERY_SIZE * 4)

#define SCAN_START_SIZE	SGEN_SCAN_START_SIZE

/* the minimum size of a fragment that we consider useful for allocation */
#define FRAGMENT_MIN_SIZE (512)

static mword pagesize = 4096;
static mword nursery_size;
static int degraded_mode = 0;

static mword total_alloc = 0;
/* use this to tune when to do a major/minor collection */
static mword memory_pressure = 0;
static mword minor_collection_allowance;
static int minor_collection_sections_alloced = 0;

static GCMemSection *nursery_section = NULL;
static mword lowest_heap_address = ~(mword)0;
static mword highest_heap_address = 0;

static LOCK_DECLARE (interruption_mutex);
static LOCK_DECLARE (global_remset_mutex);

#define LOCK_GLOBAL_REMSET pthread_mutex_lock (&global_remset_mutex)
#define UNLOCK_GLOBAL_REMSET pthread_mutex_unlock (&global_remset_mutex)

typedef struct _FinalizeEntry FinalizeEntry;
struct _FinalizeEntry {
	FinalizeEntry *next;
	void *object;
};

typedef struct _FinalizeEntryHashTable FinalizeEntryHashTable;
struct _FinalizeEntryHashTable {
	FinalizeEntry **table;
	mword size;
	int num_registered;
};

typedef struct _DisappearingLink DisappearingLink;
struct _DisappearingLink {
	DisappearingLink *next;
	void **link;
};

typedef struct _DisappearingLinkHashTable DisappearingLinkHashTable;
struct _DisappearingLinkHashTable {
	DisappearingLink **table;
	mword size;
	int num_links;
};

typedef struct _EphemeronLinkNode EphemeronLinkNode;

struct _EphemeronLinkNode {
	EphemeronLinkNode *next;
	char *array;
};

typedef struct {
       void *key;
       void *value;
} Ephemeron;

enum {
	GENERATION_NURSERY,
	GENERATION_OLD,
	GENERATION_MAX
};

int current_collection_generation = -1;

/*
 * The link pointer is hidden by negating each bit.  We use the lowest
 * bit of the link (before negation) to store whether it needs
 * resurrection tracking.
 */
#define HIDE_POINTER(p,t)	((gpointer)(~((gulong)(p)|((t)?1:0))))
#define REVEAL_POINTER(p)	((gpointer)((~(gulong)(p))&~3L))

#define DISLINK_OBJECT(d)	(REVEAL_POINTER (*(d)->link))
#define DISLINK_TRACK(d)	((~(gulong)(*(d)->link)) & 1)

/*
 * The finalizable hash has the object as the key, the 
 * disappearing_link hash, has the link address as key.
 */
static FinalizeEntryHashTable minor_finalizable_hash;
static FinalizeEntryHashTable major_finalizable_hash;
/* objects that are ready to be finalized */
static FinalizeEntry *fin_ready_list = NULL;
static FinalizeEntry *critical_fin_list = NULL;

static DisappearingLinkHashTable minor_disappearing_link_hash;
static DisappearingLinkHashTable major_disappearing_link_hash;

static EphemeronLinkNode *ephemeron_list;

static int num_ready_finalizers = 0;
static int no_finalize = 0;

enum {
	ROOT_TYPE_NORMAL = 0, /* "normal" roots */
	ROOT_TYPE_PINNED = 1, /* roots without a GC descriptor */
	ROOT_TYPE_WBARRIER = 2, /* roots with a write barrier */
	ROOT_TYPE_NUM
};

/* registered roots: the key to the hash is the root start address */
/* 
 * Different kinds of roots are kept separate to speed up pin_from_roots () for example.
 */
static RootRecord **roots_hash [ROOT_TYPE_NUM] = { NULL, NULL };
static int roots_hash_size [ROOT_TYPE_NUM] = { 0, 0, 0 };
static mword roots_size = 0; /* amount of memory in the root set */
static int num_roots_entries [ROOT_TYPE_NUM] = { 0, 0, 0 };

/* 
 * The current allocation cursors
 * We allocate objects in the nursery.
 * The nursery is the area between nursery_start and nursery_real_end.
 * Allocation is done from a Thread Local Allocation Buffer (TLAB). TLABs are allocated
 * from nursery fragments.
 * tlab_next is the pointer to the space inside the TLAB where the next object will 
 * be allocated.
 * tlab_temp_end is the pointer to the end of the temporary space reserved for
 * the allocation: it allows us to set the scan starts at reasonable intervals.
 * tlab_real_end points to the end of the TLAB.
 * nursery_frag_real_end points to the end of the currently used nursery fragment.
 * nursery_first_pinned_start points to the start of the first pinned object in the nursery
 * nursery_last_pinned_end points to the end of the last pinned object in the nursery
 * At the next allocation, the area of the nursery where objects can be present is
 * between MIN(nursery_first_pinned_start, first_fragment_start) and
 * MAX(nursery_last_pinned_end, nursery_frag_real_end)
 */
static char *nursery_start = NULL;

#ifdef HAVE_KW_THREAD
#define TLAB_ACCESS_INIT
#define TLAB_START	tlab_start
#define TLAB_NEXT	tlab_next
#define TLAB_TEMP_END	tlab_temp_end
#define TLAB_REAL_END	tlab_real_end
#define REMEMBERED_SET	remembered_set
#define STORE_REMSET_BUFFER	store_remset_buffer
#define STORE_REMSET_BUFFER_INDEX	store_remset_buffer_index
#define IN_CRITICAL_REGION thread_info->in_critical_region
#else
static pthread_key_t thread_info_key;
#define TLAB_ACCESS_INIT	SgenThreadInfo *__thread_info__ = pthread_getspecific (thread_info_key)
#define TLAB_START	(__thread_info__->tlab_start)
#define TLAB_NEXT	(__thread_info__->tlab_next)
#define TLAB_TEMP_END	(__thread_info__->tlab_temp_end)
#define TLAB_REAL_END	(__thread_info__->tlab_real_end)
#define REMEMBERED_SET	(__thread_info__->remset)
#define STORE_REMSET_BUFFER	(__thread_info__->store_remset_buffer)
#define STORE_REMSET_BUFFER_INDEX	(__thread_info__->store_remset_buffer_index)
#define IN_CRITICAL_REGION (__thread_info__->in_critical_region)
#endif

/* we use the memory barrier only to prevent compiler reordering (a memory constraint may be enough) */
#define ENTER_CRITICAL_REGION do {IN_CRITICAL_REGION = 1;mono_memory_barrier ();} while (0)
#define EXIT_CRITICAL_REGION  do {IN_CRITICAL_REGION = 0;mono_memory_barrier ();} while (0)

/*
 * FIXME: What is faster, a TLS variable pointing to a structure, or separate TLS 
 * variables for next+temp_end ?
 */
#ifdef HAVE_KW_THREAD
static __thread SgenThreadInfo *thread_info;
static __thread char *tlab_start;
static __thread char *tlab_next;
static __thread char *tlab_temp_end;
static __thread char *tlab_real_end;
static __thread gpointer *store_remset_buffer;
static __thread long store_remset_buffer_index;
/* Used by the managed allocator/wbarrier */
static __thread char **tlab_next_addr;
static __thread char *stack_end;
static __thread long *store_remset_buffer_index_addr;
#endif
static char *nursery_next = NULL;
static char *nursery_frag_real_end = NULL;
static char *nursery_real_end = NULL;
static char *nursery_last_pinned_end = NULL;

/* The size of a TLAB */
/* The bigger the value, the less often we have to go to the slow path to allocate a new 
 * one, but the more space is wasted by threads not allocating much memory.
 * FIXME: Tune this.
 * FIXME: Make this self-tuning for each thread.
 */
static guint32 tlab_size = (1024 * 4);

/*How much space is tolerable to be wasted from the current fragment when allocating a new TLAB*/
#define MAX_NURSERY_TLAB_WASTE 512

/* fragments that are free and ready to be used for allocation */
static Fragment *nursery_fragments = NULL;
/* freeelist of fragment structures */
static Fragment *fragment_freelist = NULL;

#define MAX_SMALL_OBJ_SIZE	SGEN_MAX_SMALL_OBJ_SIZE

/* Functions supplied by the runtime to be called by the GC */
static MonoGCCallbacks gc_callbacks;

#define ALLOC_ALIGN		SGEN_ALLOC_ALIGN
#define ALLOC_ALIGN_BITS	SGEN_ALLOC_ALIGN_BITS

#define ALIGN_UP		SGEN_ALIGN_UP

#define MOVED_OBJECTS_NUM 64
static void *moved_objects [MOVED_OBJECTS_NUM];
static int moved_objects_idx = 0;

/*
 * ######################################################################
 * ########  Macros and function declarations.
 * ######################################################################
 */

#define ADDR_IN_HEAP_BOUNDARIES(addr) ((p) >= lowest_heap_address && (p) < highest_heap_address)

inline static void*
align_pointer (void *ptr)
{
	mword p = (mword)ptr;
	p += sizeof (gpointer) - 1;
	p &= ~ (sizeof (gpointer) - 1);
	return (void*)p;
}

typedef SgenGrayQueue GrayQueue;

typedef void (*CopyOrMarkObjectFunc) (void**, GrayQueue*);
typedef char* (*ScanObjectFunc) (char*, GrayQueue*);

/* forward declarations */
static int stop_world (int generation);
static int restart_world (int generation);
static void scan_thread_data (void *start_nursery, void *end_nursery, gboolean precise);
static void scan_from_remsets (void *start_nursery, void *end_nursery, GrayQueue *queue);
static void scan_from_registered_roots (CopyOrMarkObjectFunc copy_func, char *addr_start, char *addr_end, int root_type, GrayQueue *queue);
static void scan_finalizer_entries (CopyOrMarkObjectFunc copy_func, FinalizeEntry *list, GrayQueue *queue);
static void find_pinning_ref_from_thread (char *obj, size_t size);
static void update_current_thread_stack (void *start);
static void finalize_in_range (CopyOrMarkObjectFunc copy_func, char *start, char *end, int generation, GrayQueue *queue);
static void add_or_remove_disappearing_link (MonoObject *obj, void **link, gboolean track, int generation);
static void null_link_in_range (CopyOrMarkObjectFunc copy_func, char *start, char *end, int generation, GrayQueue *queue);
static void null_links_for_domain (MonoDomain *domain, int generation);
static gboolean search_fragment_for_size (size_t size);
static int search_fragment_for_size_range (size_t desired_size, size_t minimum_size);
static void clear_nursery_fragments (char *next);
static void pin_from_roots (void *start_nursery, void *end_nursery);
static int pin_objects_from_addresses (GCMemSection *section, void **start, void **end, void *start_nursery, void *end_nursery, GrayQueue *queue);
static void optimize_pin_queue (int start_slot);
static void clear_remsets (void);
static void clear_tlabs (void);
static void sort_addresses (void **array, int size);
static void drain_gray_stack (GrayQueue *queue);
static void finish_gray_stack (char *start_addr, char *end_addr, int generation, GrayQueue *queue);
static gboolean need_major_collection (void);
static void major_collection (const char *reason);

static void mono_gc_register_disappearing_link (MonoObject *obj, void **link, gboolean track);

void describe_ptr (char *ptr);
void check_object (char *start);

static void check_consistency (void);
static void check_major_refs (void);
static void check_scan_starts (void);
static void check_for_xdomain_refs (void);
static void dump_heap (const char *type, int num, const char *reason);

void mono_gc_scan_for_specific_ref (MonoObject *key);

static void init_stats (void);

static int mark_ephemerons_in_range (CopyOrMarkObjectFunc copy_func, char *start, char *end, GrayQueue *queue);
static void clear_unreachable_ephemerons (CopyOrMarkObjectFunc copy_func, char *start, char *end, GrayQueue *queue);
static void null_ephemerons_for_domain (MonoDomain *domain);

SgenMajorCollector major_collector;

#include "sgen-protocol.c"
#include "sgen-pinning.c"
#include "sgen-pinning-stats.c"
#include "sgen-gray.c"
#include "sgen-workers.c"
#include "sgen-los.c"
#include "sgen-cardtable.c"

/* Root bitmap descriptors are simpler: the lower three bits describe the type
 * and we either have 30/62 bitmap bits or nibble-based run-length,
 * or a complex descriptor, or a user defined marker function.
 */
enum {
	ROOT_DESC_CONSERVATIVE, /* 0, so matches NULL value */
	ROOT_DESC_BITMAP,
	ROOT_DESC_RUN_LEN, 
	ROOT_DESC_COMPLEX,
	ROOT_DESC_USER,
	ROOT_DESC_TYPE_MASK = 0x7,
	ROOT_DESC_TYPE_SHIFT = 3,
};

#define MAKE_ROOT_DESC(type,val) ((type) | ((val) << ROOT_DESC_TYPE_SHIFT))

#define MAX_USER_DESCRIPTORS 16

static gsize* complex_descriptors = NULL;
static int complex_descriptors_size = 0;
static int complex_descriptors_next = 0;
static MonoGCRootMarkFunc user_descriptors [MAX_USER_DESCRIPTORS];
static int user_descriptors_next = 0;

static int
alloc_complex_descriptor (gsize *bitmap, int numbits)
{
	int nwords, res, i;

	numbits = ALIGN_TO (numbits, GC_BITS_PER_WORD);
	nwords = numbits / GC_BITS_PER_WORD + 1;

	LOCK_GC;
	res = complex_descriptors_next;
	/* linear search, so we don't have duplicates with domain load/unload
	 * this should not be performance critical or we'd have bigger issues
	 * (the number and size of complex descriptors should be small).
	 */
	for (i = 0; i < complex_descriptors_next; ) {
		if (complex_descriptors [i] == nwords) {
			int j, found = TRUE;
			for (j = 0; j < nwords - 1; ++j) {
				if (complex_descriptors [i + 1 + j] != bitmap [j]) {
					found = FALSE;
					break;
				}
			}
			if (found) {
				UNLOCK_GC;
				return i;
			}
		}
		i += complex_descriptors [i];
	}
	if (complex_descriptors_next + nwords > complex_descriptors_size) {
		int new_size = complex_descriptors_size * 2 + nwords;
		complex_descriptors = g_realloc (complex_descriptors, new_size * sizeof (gsize));
		complex_descriptors_size = new_size;
	}
	DEBUG (6, fprintf (gc_debug_file, "Complex descriptor %d, size: %d (total desc memory: %d)\n", res, nwords, complex_descriptors_size));
	complex_descriptors_next += nwords;
	complex_descriptors [res] = nwords;
	for (i = 0; i < nwords - 1; ++i) {
		complex_descriptors [res + 1 + i] = bitmap [i];
		DEBUG (6, fprintf (gc_debug_file, "\tvalue: %p\n", (void*)complex_descriptors [res + 1 + i]));
	}
	UNLOCK_GC;
	return res;
}

gsize*
mono_sgen_get_complex_descriptor (GCVTable *vt)
{
	return complex_descriptors + (vt->desc >> LOW_TYPE_BITS);
}

/*
 * Descriptor builders.
 */
void*
mono_gc_make_descr_for_string (gsize *bitmap, int numbits)
{
	return (void*) DESC_TYPE_RUN_LENGTH;
}

void*
mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	mword desc = 0;
	size_t stored_size = obj_size;
	for (i = 0; i < numbits; ++i) {
		if (bitmap [i / GC_BITS_PER_WORD] & ((gsize)1 << (i % GC_BITS_PER_WORD))) {
			if (first_set < 0)
				first_set = i;
			last_set = i;
			num_set++;
		}
	}
	/*
	 * We don't encode the size of types that don't contain
	 * references because they might not be aligned, i.e. the
	 * bottom two bits might be set, which would clash with the
	 * bits we need to encode the descriptor type.  Since we don't
	 * use the encoded size to skip objects, other than for
	 * processing remsets, in which case only the positions of
	 * references are relevant, this is not a problem.
	 */
	if (first_set < 0)
		return (void*)DESC_TYPE_RUN_LENGTH;
	g_assert (!(stored_size & 0x3));
	if (stored_size <= MAX_SMALL_OBJ_SIZE) {
		/* check run-length encoding first: one byte offset, one byte number of pointers
		 * on 64 bit archs, we can have 3 runs, just one on 32.
		 * It may be better to use nibbles.
		 */
		if (first_set < 0) {
			desc = DESC_TYPE_RUN_LENGTH | (stored_size << 1);
			DEBUG (6, fprintf (gc_debug_file, "Ptrfree descriptor %p, size: %zd\n", (void*)desc, stored_size));
			return (void*) desc;
		} else if (first_set < 256 && num_set < 256 && (first_set + num_set == last_set + 1)) {
			desc = DESC_TYPE_RUN_LENGTH | (stored_size << 1) | (first_set << 16) | (num_set << 24);
			DEBUG (6, fprintf (gc_debug_file, "Runlen descriptor %p, size: %zd, first set: %d, num set: %d\n", (void*)desc, stored_size, first_set, num_set));
			return (void*) desc;
		}
		/* we know the 2-word header is ptr-free */
		if (last_set < SMALL_BITMAP_SIZE + OBJECT_HEADER_WORDS) {
			desc = DESC_TYPE_SMALL_BITMAP | (stored_size << 1) | ((*bitmap >> OBJECT_HEADER_WORDS) << SMALL_BITMAP_SHIFT);
			DEBUG (6, fprintf (gc_debug_file, "Smallbitmap descriptor %p, size: %zd, last set: %d\n", (void*)desc, stored_size, last_set));
			return (void*) desc;
		}
	}
	/* we know the 2-word header is ptr-free */
	if (last_set < LARGE_BITMAP_SIZE + OBJECT_HEADER_WORDS) {
		desc = DESC_TYPE_LARGE_BITMAP | ((*bitmap >> OBJECT_HEADER_WORDS) << LOW_TYPE_BITS);
		DEBUG (6, fprintf (gc_debug_file, "Largebitmap descriptor %p, size: %zd, last set: %d\n", (void*)desc, stored_size, last_set));
		return (void*) desc;
	}
	/* it's a complex object ... */
	desc = DESC_TYPE_COMPLEX | (alloc_complex_descriptor (bitmap, last_set + 1) << LOW_TYPE_BITS);
	return (void*) desc;
}

/* If the array holds references, numbits == 1 and the first bit is set in elem_bitmap */
void*
mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	mword desc = vector? DESC_TYPE_VECTOR: DESC_TYPE_ARRAY;
	for (i = 0; i < numbits; ++i) {
		if (elem_bitmap [i / GC_BITS_PER_WORD] & ((gsize)1 << (i % GC_BITS_PER_WORD))) {
			if (first_set < 0)
				first_set = i;
			last_set = i;
			num_set++;
		}
	}
	/* See comment at the definition of DESC_TYPE_RUN_LENGTH. */
	if (first_set < 0)
		return (void*)DESC_TYPE_RUN_LENGTH;
	if (elem_size <= MAX_ELEMENT_SIZE) {
		desc |= elem_size << VECTOR_ELSIZE_SHIFT;
		if (!num_set) {
			return (void*)(desc | VECTOR_SUBTYPE_PTRFREE);
		}
		/* Note: we also handle structs with just ref fields */
		if (num_set * sizeof (gpointer) == elem_size) {
			return (void*)(desc | VECTOR_SUBTYPE_REFS | ((gssize)(-1) << 16));
		}
		/* FIXME: try run-len first */
		/* Note: we can't skip the object header here, because it's not present */
		if (last_set <= SMALL_BITMAP_SIZE) {
			return (void*)(desc | VECTOR_SUBTYPE_BITMAP | (*elem_bitmap << 16));
		}
	}
	/* it's am array of complex structs ... */
	desc = DESC_TYPE_COMPLEX_ARR;
	desc |= alloc_complex_descriptor (elem_bitmap, last_set + 1) << LOW_TYPE_BITS;
	return (void*) desc;
}

/* Return the bitmap encoded by a descriptor */
gsize*
mono_gc_get_bitmap_for_descr (void *descr, int *numbits)
{
	mword d = (mword)descr;
	gsize *bitmap;

	switch (d & 0x7) {
	case DESC_TYPE_RUN_LENGTH: {		
		int first_set = (d >> 16) & 0xff;
		int num_set = (d >> 24) & 0xff;
		int i;

		bitmap = g_new0 (gsize, (first_set + num_set + 7) / 8);

		for (i = first_set; i < first_set + num_set; ++i)
			bitmap [i / GC_BITS_PER_WORD] |= ((gsize)1 << (i % GC_BITS_PER_WORD));

		*numbits = first_set + num_set;

		return bitmap;
	}
	case DESC_TYPE_SMALL_BITMAP:
		bitmap = g_new0 (gsize, 1);

		bitmap [0] = (d >> SMALL_BITMAP_SHIFT) << OBJECT_HEADER_WORDS;

	    *numbits = GC_BITS_PER_WORD;
		
		return bitmap;
	default:
		g_assert_not_reached ();
	}
}

static gboolean
is_xdomain_ref_allowed (gpointer *ptr, char *obj, MonoDomain *domain)
{
	MonoObject *o = (MonoObject*)(obj);
	MonoObject *ref = (MonoObject*)*(ptr);
	int offset = (char*)(ptr) - (char*)o;

	if (o->vtable->klass == mono_defaults.thread_class && offset == G_STRUCT_OFFSET (MonoThread, internal_thread))
		return TRUE;
	if (o->vtable->klass == mono_defaults.internal_thread_class && offset == G_STRUCT_OFFSET (MonoInternalThread, current_appcontext))
		return TRUE;
	if (mono_class_has_parent (o->vtable->klass, mono_defaults.real_proxy_class) &&
			offset == G_STRUCT_OFFSET (MonoRealProxy, unwrapped_server))
		return TRUE;
	/* Thread.cached_culture_info */
	if (!strcmp (ref->vtable->klass->name_space, "System.Globalization") &&
			!strcmp (ref->vtable->klass->name, "CultureInfo") &&
			!strcmp(o->vtable->klass->name_space, "System") &&
			!strcmp(o->vtable->klass->name, "Object[]"))
		return TRUE;
	/*
	 *  at System.IO.MemoryStream.InternalConstructor (byte[],int,int,bool,bool) [0x0004d] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.IO/MemoryStream.cs:121
	 * at System.IO.MemoryStream..ctor (byte[]) [0x00017] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.IO/MemoryStream.cs:81
	 * at (wrapper remoting-invoke-with-check) System.IO.MemoryStream..ctor (byte[]) <IL 0x00020, 0xffffffff>
	 * at System.Runtime.Remoting.Messaging.CADMethodCallMessage.GetArguments () [0x0000d] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.Runtime.Remoting.Messaging/CADMessages.cs:327
	 * at System.Runtime.Remoting.Messaging.MethodCall..ctor (System.Runtime.Remoting.Messaging.CADMethodCallMessage) [0x00017] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.Runtime.Remoting.Messaging/MethodCall.cs:87
	 * at System.AppDomain.ProcessMessageInDomain (byte[],System.Runtime.Remoting.Messaging.CADMethodCallMessage,byte[]&,System.Runtime.Remoting.Messaging.CADMethodReturnMessage&) [0x00018] in /home/schani/Work/novell/trunk/mcs/class/corlib/System/AppDomain.cs:1213
	 * at (wrapper remoting-invoke-with-check) System.AppDomain.ProcessMessageInDomain (byte[],System.Runtime.Remoting.Messaging.CADMethodCallMessage,byte[]&,System.Runtime.Remoting.Messaging.CADMethodReturnMessage&) <IL 0x0003d, 0xffffffff>
	 * at System.Runtime.Remoting.Channels.CrossAppDomainSink.ProcessMessageInDomain (byte[],System.Runtime.Remoting.Messaging.CADMethodCallMessage) [0x00008] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.Runtime.Remoting.Channels/CrossAppDomainChannel.cs:198
	 * at (wrapper runtime-invoke) object.runtime_invoke_CrossAppDomainSink/ProcessMessageRes_object_object (object,intptr,intptr,intptr) <IL 0x0004c, 0xffffffff>
	 */
	if (!strcmp (ref->vtable->klass->name_space, "System") &&
			!strcmp (ref->vtable->klass->name, "Byte[]") &&
			!strcmp (o->vtable->klass->name_space, "System.IO") &&
			!strcmp (o->vtable->klass->name, "MemoryStream"))
		return TRUE;
	/* append_job() in threadpool.c */
	if (!strcmp (ref->vtable->klass->name_space, "System.Runtime.Remoting.Messaging") &&
			!strcmp (ref->vtable->klass->name, "AsyncResult") &&
			!strcmp (o->vtable->klass->name_space, "System") &&
			!strcmp (o->vtable->klass->name, "Object[]") &&
			mono_thread_pool_is_queue_array ((MonoArray*) o))
		return TRUE;
	return FALSE;
}

static void
check_reference_for_xdomain (gpointer *ptr, char *obj, MonoDomain *domain)
{
	MonoObject *o = (MonoObject*)(obj);
	MonoObject *ref = (MonoObject*)*(ptr);
	int offset = (char*)(ptr) - (char*)o;
	MonoClass *class;
	MonoClassField *field;
	char *str;

	if (!ref || ref->vtable->domain == domain)
		return;
	if (is_xdomain_ref_allowed (ptr, obj, domain))
		return;

	field = NULL;
	for (class = o->vtable->klass; class; class = class->parent) {
		int i;

		for (i = 0; i < class->field.count; ++i) {
			if (class->fields[i].offset == offset) {
				field = &class->fields[i];
				break;
			}
		}
		if (field)
			break;
	}

	if (ref->vtable->klass == mono_defaults.string_class)
		str = mono_string_to_utf8 ((MonoString*)ref);
	else
		str = NULL;
	g_print ("xdomain reference in %p (%s.%s) at offset %d (%s) to %p (%s.%s) (%s)  -  pointed to by:\n",
			o, o->vtable->klass->name_space, o->vtable->klass->name,
			offset, field ? field->name : "",
			ref, ref->vtable->klass->name_space, ref->vtable->klass->name, str ? str : "");
	mono_gc_scan_for_specific_ref (o);
	if (str)
		g_free (str);
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	check_reference_for_xdomain ((ptr), (obj), domain)

static void
scan_object_for_xdomain_refs (char *start, mword size, void *data)
{
	MonoDomain *domain = ((MonoObject*)start)->vtable->domain;

	#include "sgen-scan-object.h"
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj) do {		\
	if ((MonoObject*)*(ptr) == key) {	\
	g_print ("found ref to %p in object %p (%s) at offset %td\n",	\
			key, (obj), safe_name ((obj)), ((char*)(ptr) - (char*)(obj))); \
	}								\
	} while (0)

static void
scan_object_for_specific_ref (char *start, MonoObject *key)
{
	#include "sgen-scan-object.h"
}

void
mono_sgen_scan_area_with_callback (char *start, char *end, IterateObjectCallbackFunc callback, void *data)
{
	while (start < end) {
		size_t size;
		if (!*(void**)start) {
			start += sizeof (void*); /* should be ALLOC_ALIGN, really */
			continue;
		}

		size = ALIGN_UP (safe_object_get_size ((MonoObject*) start));

		callback (start, size, data);

		start += size;
	}
}

static void
scan_object_for_specific_ref_callback (char *obj, size_t size, MonoObject *key)
{
	scan_object_for_specific_ref (obj, key);
}

static void
check_root_obj_specific_ref (RootRecord *root, MonoObject *key, MonoObject *obj)
{
	if (key != obj)
		return;
	g_print ("found ref to %p in root record %p\n", key, root);
}

static MonoObject *check_key = NULL;
static RootRecord *check_root = NULL;

static void
check_root_obj_specific_ref_from_marker (void **obj)
{
	check_root_obj_specific_ref (check_root, check_key, *obj);
}

static void
scan_roots_for_specific_ref (MonoObject *key, int root_type)
{
	int i;
	RootRecord *root;
	check_key = key;
	for (i = 0; i < roots_hash_size [root_type]; ++i) {
		for (root = roots_hash [root_type][i]; root; root = root->next) {
			void **start_root = (void**)root->start_root;
			mword desc = root->root_desc;

			check_root = root;

			switch (desc & ROOT_DESC_TYPE_MASK) {
			case ROOT_DESC_BITMAP:
				desc >>= ROOT_DESC_TYPE_SHIFT;
				while (desc) {
					if (desc & 1)
						check_root_obj_specific_ref (root, key, *start_root);
					desc >>= 1;
					start_root++;
				}
				return;
			case ROOT_DESC_COMPLEX: {
				gsize *bitmap_data = complex_descriptors + (desc >> ROOT_DESC_TYPE_SHIFT);
				int bwords = (*bitmap_data) - 1;
				void **start_run = start_root;
				bitmap_data++;
				while (bwords-- > 0) {
					gsize bmap = *bitmap_data++;
					void **objptr = start_run;
					while (bmap) {
						if (bmap & 1)
							check_root_obj_specific_ref (root, key, *objptr);
						bmap >>= 1;
						++objptr;
					}
					start_run += GC_BITS_PER_WORD;
				}
				break;
			}
			case ROOT_DESC_USER: {
				MonoGCRootMarkFunc marker = user_descriptors [desc >> ROOT_DESC_TYPE_SHIFT];
				marker (start_root, check_root_obj_specific_ref_from_marker);
				break;
			}
			case ROOT_DESC_RUN_LEN:
				g_assert_not_reached ();
			default:
				g_assert_not_reached ();
			}
		}
	}
	check_key = NULL;
	check_root = NULL;
}

void
mono_gc_scan_for_specific_ref (MonoObject *key)
{
	LOSObject *bigobj;
	RootRecord *root;
	int i;

	mono_sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)scan_object_for_specific_ref_callback, key);

	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)scan_object_for_specific_ref_callback, key);

	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		scan_object_for_specific_ref (bigobj->data, key);

	scan_roots_for_specific_ref (key, ROOT_TYPE_NORMAL);
	scan_roots_for_specific_ref (key, ROOT_TYPE_WBARRIER);

	for (i = 0; i < roots_hash_size [ROOT_TYPE_PINNED]; ++i) {
		for (root = roots_hash [ROOT_TYPE_PINNED][i]; root; root = root->next) {
			void **ptr = (void**)root->start_root;

			while (ptr < (void**)root->end_root) {
				check_root_obj_specific_ref (root, *ptr, key);
				++ptr;
			}
		}
	}
}

/* Clear all remaining nursery fragments */
static void
clear_nursery_fragments (char *next)
{
	Fragment *frag;
	if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION) {
		g_assert (next <= nursery_frag_real_end);
		DEBUG (4, fprintf (gc_debug_file, "Clear nursery frag %p-%p\n", next, nursery_frag_real_end));
		memset (next, 0, nursery_frag_real_end - next);
		for (frag = nursery_fragments; frag; frag = frag->next) {
			DEBUG (4, fprintf (gc_debug_file, "Clear nursery frag %p-%p\n", frag->fragment_start, frag->fragment_end));
			memset (frag->fragment_start, 0, frag->fragment_end - frag->fragment_start);
		}
	}
}

static gboolean
need_remove_object_for_domain (char *start, MonoDomain *domain)
{
	if (mono_object_domain (start) == domain) {
		DEBUG (4, fprintf (gc_debug_file, "Need to cleanup object %p\n", start));
		binary_protocol_cleanup (start, (gpointer)LOAD_VTABLE (start), safe_object_get_size ((MonoObject*)start));
		return TRUE;
	}
	return FALSE;
}

static void
process_object_for_domain_clearing (char *start, MonoDomain *domain)
{
	GCVTable *vt = (GCVTable*)LOAD_VTABLE (start);
	if (vt->klass == mono_defaults.internal_thread_class)
		g_assert (mono_object_domain (start) == mono_get_root_domain ());
	/* The object could be a proxy for an object in the domain
	   we're deleting. */
	if (mono_class_has_parent (vt->klass, mono_defaults.real_proxy_class)) {
		MonoObject *server = ((MonoRealProxy*)start)->unwrapped_server;

		/* The server could already have been zeroed out, so
		   we need to check for that, too. */
		if (server && (!LOAD_VTABLE (server) || mono_object_domain (server) == domain)) {
			DEBUG (4, fprintf (gc_debug_file, "Cleaning up remote pointer in %p to object %p\n",
					start, server));
			((MonoRealProxy*)start)->unwrapped_server = NULL;
		}
	}
}

static MonoDomain *check_domain = NULL;

static void
check_obj_not_in_domain (void **o)
{
	g_assert (((MonoObject*)(*o))->vtable->domain != check_domain);
}

static void
scan_for_registered_roots_in_domain (MonoDomain *domain, int root_type)
{
	int i;
	RootRecord *root;
	check_domain = domain;
	for (i = 0; i < roots_hash_size [root_type]; ++i) {
		for (root = roots_hash [root_type][i]; root; root = root->next) {
			void **start_root = (void**)root->start_root;
			mword desc = root->root_desc;

			/* The MonoDomain struct is allowed to hold
			   references to objects in its own domain. */
			if (start_root == (void**)domain)
				continue;

			switch (desc & ROOT_DESC_TYPE_MASK) {
			case ROOT_DESC_BITMAP:
				desc >>= ROOT_DESC_TYPE_SHIFT;
				while (desc) {
					if ((desc & 1) && *start_root)
						check_obj_not_in_domain (*start_root);
					desc >>= 1;
					start_root++;
				}
				break;
			case ROOT_DESC_COMPLEX: {
				gsize *bitmap_data = complex_descriptors + (desc >> ROOT_DESC_TYPE_SHIFT);
				int bwords = (*bitmap_data) - 1;
				void **start_run = start_root;
				bitmap_data++;
				while (bwords-- > 0) {
					gsize bmap = *bitmap_data++;
					void **objptr = start_run;
					while (bmap) {
						if ((bmap & 1) && *objptr)
							check_obj_not_in_domain (*objptr);
						bmap >>= 1;
						++objptr;
					}
					start_run += GC_BITS_PER_WORD;
				}
				break;
			}
			case ROOT_DESC_USER: {
				MonoGCRootMarkFunc marker = user_descriptors [desc >> ROOT_DESC_TYPE_SHIFT];
				marker (start_root, check_obj_not_in_domain);
				break;
			}
			case ROOT_DESC_RUN_LEN:
				g_assert_not_reached ();
			default:
				g_assert_not_reached ();
			}
		}
	}
	check_domain = NULL;
}

static void
check_for_xdomain_refs (void)
{
	LOSObject *bigobj;

	mono_sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)scan_object_for_xdomain_refs, NULL);

	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)scan_object_for_xdomain_refs, NULL);

	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		scan_object_for_xdomain_refs (bigobj->data, bigobj->size, NULL);
}

static gboolean
clear_domain_process_object (char *obj, MonoDomain *domain)
{
	gboolean remove;

	process_object_for_domain_clearing (obj, domain);
	remove = need_remove_object_for_domain (obj, domain);

	if (remove && ((MonoObject*)obj)->synchronisation) {
		void **dislink = mono_monitor_get_object_monitor_weak_link ((MonoObject*)obj);
		if (dislink)
			mono_gc_register_disappearing_link (NULL, dislink, FALSE);
	}

	return remove;
}

static void
clear_domain_process_minor_object_callback (char *obj, size_t size, MonoDomain *domain)
{
	if (clear_domain_process_object (obj, domain))
		memset (obj, 0, size);
}

static void
clear_domain_process_major_object_callback (char *obj, size_t size, MonoDomain *domain)
{
	clear_domain_process_object (obj, domain);
}

static void
clear_domain_free_major_non_pinned_object_callback (char *obj, size_t size, MonoDomain *domain)
{
	if (need_remove_object_for_domain (obj, domain))
		major_collector.free_non_pinned_object (obj, size);
}

static void
clear_domain_free_major_pinned_object_callback (char *obj, size_t size, MonoDomain *domain)
{
	if (need_remove_object_for_domain (obj, domain))
		major_collector.free_pinned_object (obj, size);
}

/*
 * When appdomains are unloaded we can easily remove objects that have finalizers,
 * but all the others could still be present in random places on the heap.
 * We need a sweep to get rid of them even though it's going to be costly
 * with big heaps.
 * The reason we need to remove them is because we access the vtable and class
 * structures to know the object size and the reference bitmap: once the domain is
 * unloaded the point to random memory.
 */
void
mono_gc_clear_domain (MonoDomain * domain)
{
	LOSObject *bigobj, *prev;
	int i;

	LOCK_GC;

	clear_nursery_fragments (nursery_next);

	if (xdomain_checks && domain != mono_get_root_domain ()) {
		scan_for_registered_roots_in_domain (domain, ROOT_TYPE_NORMAL);
		scan_for_registered_roots_in_domain (domain, ROOT_TYPE_WBARRIER);
		check_for_xdomain_refs ();
	}

	mono_sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)clear_domain_process_minor_object_callback, domain);

	/*Ephemerons and dislinks must be processed before LOS since they might end up pointing
	to memory returned to the OS.*/
	null_ephemerons_for_domain (domain);

	for (i = GENERATION_NURSERY; i < GENERATION_MAX; ++i)
		null_links_for_domain (domain, i);

	/* We need two passes over major and large objects because
	   freeing such objects might give their memory back to the OS
	   (in the case of large objects) or obliterate its vtable
	   (pinned objects with major-copying or pinned and non-pinned
	   objects with major-mark&sweep), but we might need to
	   dereference a pointer from an object to another object if
	   the first object is a proxy. */
	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)clear_domain_process_major_object_callback, domain);
	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		clear_domain_process_object (bigobj->data, domain);

	prev = NULL;
	for (bigobj = los_object_list; bigobj;) {
		if (need_remove_object_for_domain (bigobj->data, domain)) {
			LOSObject *to_free = bigobj;
			if (prev)
				prev->next = bigobj->next;
			else
				los_object_list = bigobj->next;
			bigobj = bigobj->next;
			DEBUG (4, fprintf (gc_debug_file, "Freeing large object %p\n",
					bigobj->data));
			free_large_object (to_free);
			continue;
		}
		prev = bigobj;
		bigobj = bigobj->next;
	}
	major_collector.iterate_objects (TRUE, FALSE, (IterateObjectCallbackFunc)clear_domain_free_major_non_pinned_object_callback, domain);
	major_collector.iterate_objects (FALSE, TRUE, (IterateObjectCallbackFunc)clear_domain_free_major_pinned_object_callback, domain);

	UNLOCK_GC;
}

static void
global_remset_cache_clear (void)
{
	memset (global_remset_cache, 0, sizeof (global_remset_cache));
}

/*
 * Tries to check if a given remset location was already added to the global remset.
 * It can
 *
 * A 2 entry, LRU cache of recently saw location remsets.
 *
 * It's hand-coded instead of done using loops to reduce the number of memory references on cache hit.
 *
 * Returns TRUE is the element was added..
 */
static gboolean
global_remset_location_was_not_added (gpointer ptr)
{

	gpointer first = global_remset_cache [0], second;
	if (first == ptr) {
		HEAVY_STAT (++stat_global_remsets_discarded);
		return FALSE;
	}

	second = global_remset_cache [1];

	if (second == ptr) {
		/*Move the second to the front*/
		global_remset_cache [0] = second;
		global_remset_cache [1] = first;

		HEAVY_STAT (++stat_global_remsets_discarded);
		return FALSE;
	}

	global_remset_cache [0] = second;
	global_remset_cache [1] = ptr;
	return TRUE;
}

/*
 * mono_sgen_add_to_global_remset:
 *
 *   The global remset contains locations which point into newspace after
 * a minor collection. This can happen if the objects they point to are pinned.
 *
 * LOCKING: If called from a parallel collector, the global remset
 * lock must be held.  For serial collectors that is not necessary.
 */
void
mono_sgen_add_to_global_remset (gpointer ptr)
{
	RememberedSet *rs;
	gboolean lock;

	if (use_cardtable) {
		sgen_card_table_mark_address ((mword)ptr);
		return;
	}

	g_assert (!ptr_in_nursery (ptr) && ptr_in_nursery (*(gpointer*)ptr));

	lock = (current_collection_generation == GENERATION_OLD && major_collector.is_parallel);
	if (lock)
		LOCK_GLOBAL_REMSET;

	if (!global_remset_location_was_not_added (ptr))
		goto done;

	DEBUG (8, fprintf (gc_debug_file, "Adding global remset for %p\n", ptr));
	binary_protocol_global_remset (ptr, *(gpointer*)ptr, (gpointer)LOAD_VTABLE (*(gpointer*)ptr));

	HEAVY_STAT (++stat_global_remsets_added);

	/* 
	 * FIXME: If an object remains pinned, we need to add it at every minor collection.
	 * To avoid uncontrolled growth of the global remset, only add each pointer once.
	 */
	if (global_remset->store_next + 3 < global_remset->end_set) {
		*(global_remset->store_next++) = (mword)ptr;
		goto done;
	}
	rs = alloc_remset (global_remset->end_set - global_remset->data, NULL);
	rs->next = global_remset;
	global_remset = rs;
	*(global_remset->store_next++) = (mword)ptr;

	{
		int global_rs_size = 0;

		for (rs = global_remset; rs; rs = rs->next) {
			global_rs_size += rs->store_next - rs->data;
		}
		DEBUG (4, fprintf (gc_debug_file, "Global remset now has size %d\n", global_rs_size));
	}

 done:
	if (lock)
		UNLOCK_GLOBAL_REMSET;
}

/*
 * drain_gray_stack:
 *
 *   Scan objects in the gray stack until the stack is empty. This should be called
 * frequently after each object is copied, to achieve better locality and cache
 * usage.
 */
static void
drain_gray_stack (GrayQueue *queue)
{
	char *obj;

	if (current_collection_generation == GENERATION_NURSERY) {
		for (;;) {
			GRAY_OBJECT_DEQUEUE (queue, obj);
			if (!obj)
				break;
			DEBUG (9, fprintf (gc_debug_file, "Precise gray object scan %p (%s)\n", obj, safe_name (obj)));
			major_collector.minor_scan_object (obj, queue);
		}
	} else {
		if (major_collector.is_parallel && queue == &workers_distribute_gray_queue)
			return;

		for (;;) {
			GRAY_OBJECT_DEQUEUE (queue, obj);
			if (!obj)
				break;
			DEBUG (9, fprintf (gc_debug_file, "Precise gray object scan %p (%s)\n", obj, safe_name (obj)));
			major_collector.major_scan_object (obj, queue);
		}
	}
}

/*
 * Addresses from start to end are already sorted. This function finds
 * the object header for each address and pins the object. The
 * addresses must be inside the passed section.  The (start of the)
 * address array is overwritten with the addresses of the actually
 * pinned objects.  Return the number of pinned objects.
 */
static int
pin_objects_from_addresses (GCMemSection *section, void **start, void **end, void *start_nursery, void *end_nursery, GrayQueue *queue)
{
	void *last = NULL;
	int count = 0;
	void *search_start;
	void *last_obj = NULL;
	size_t last_obj_size = 0;
	void *addr;
	int idx;
	void **definitely_pinned = start;
	while (start < end) {
		addr = *start;
		/* the range check should be reduntant */
		if (addr != last && addr >= start_nursery && addr < end_nursery) {
			DEBUG (5, fprintf (gc_debug_file, "Considering pinning addr %p\n", addr));
			/* multiple pointers to the same object */
			if (addr >= last_obj && (char*)addr < (char*)last_obj + last_obj_size) {
				start++;
				continue;
			}
			idx = ((char*)addr - (char*)section->data) / SCAN_START_SIZE;
			g_assert (idx < section->num_scan_start);
			search_start = (void*)section->scan_starts [idx];
			if (!search_start || search_start > addr) {
				while (idx) {
					--idx;
					search_start = section->scan_starts [idx];
					if (search_start && search_start <= addr)
						break;
				}
				if (!search_start || search_start > addr)
					search_start = start_nursery;
			}
			if (search_start < last_obj)
				search_start = (char*)last_obj + last_obj_size;
			/* now addr should be in an object a short distance from search_start
			 * Note that search_start must point to zeroed mem or point to an object.
			 */
			do {
				if (!*(void**)search_start) {
					search_start = (void*)ALIGN_UP ((mword)search_start + sizeof (gpointer));
					continue;
				}
				last_obj = search_start;
				last_obj_size = ALIGN_UP (safe_object_get_size ((MonoObject*)search_start));
				DEBUG (8, fprintf (gc_debug_file, "Pinned try match %p (%s), size %zd\n", last_obj, safe_name (last_obj), last_obj_size));
				if (addr >= search_start && (char*)addr < (char*)last_obj + last_obj_size) {
					DEBUG (4, fprintf (gc_debug_file, "Pinned object %p, vtable %p (%s), count %d\n", search_start, *(void**)search_start, safe_name (search_start), count));
					binary_protocol_pin (search_start, (gpointer)LOAD_VTABLE (search_start), safe_object_get_size (search_start));
					pin_object (search_start);
					GRAY_OBJECT_ENQUEUE (queue, search_start);
					if (heap_dump_file)
						mono_sgen_pin_stats_register_object (search_start, last_obj_size);
					definitely_pinned [count] = search_start;
					count++;
					break;
				}
				/* skip to the next object */
				search_start = (void*)((char*)search_start + last_obj_size);
			} while (search_start <= addr);
			/* we either pinned the correct object or we ignored the addr because
			 * it points to unused zeroed memory.
			 */
			last = addr;
		}
		start++;
	}
	//printf ("effective pinned: %d (at the end: %d)\n", count, (char*)end_nursery - (char*)last);
	return count;
}

void
mono_sgen_pin_objects_in_section (GCMemSection *section, GrayQueue *queue)
{
	int num_entries = section->pin_queue_num_entries;
	if (num_entries) {
		void **start = section->pin_queue_start;
		int reduced_to;
		reduced_to = pin_objects_from_addresses (section, start, start + num_entries,
				section->data, section->next_data, queue);
		section->pin_queue_num_entries = reduced_to;
		if (!reduced_to)
			section->pin_queue_start = NULL;
	}
}

/* Sort the addresses in array in increasing order.
 * Done using a by-the book heap sort. Which has decent and stable performance, is pretty cache efficient.
 */
static void
sort_addresses (void **array, int size)
{
	int i;
	void *tmp;

	for (i = 1; i < size; ++i) {
		int child = i;
		while (child > 0) {
			int parent = (child - 1) / 2;

			if (array [parent] >= array [child])
				break;

			tmp = array [parent];
			array [parent] = array [child];
			array [child] = tmp;

			child = parent;
		}
	}

	for (i = size - 1; i > 0; --i) {
		int end, root;
		tmp = array [i];
		array [i] = array [0];
		array [0] = tmp;

		end = i - 1;
		root = 0;

		while (root * 2 + 1 <= end) {
			int child = root * 2 + 1;

			if (child < end && array [child] < array [child + 1])
				++child;
			if (array [root] >= array [child])
				break;

			tmp = array [root];
			array [root] = array [child];
			array [child] = tmp;

			root = child;
		}
	}
}

static G_GNUC_UNUSED void
print_nursery_gaps (void* start_nursery, void *end_nursery)
{
	int i;
	gpointer first = start_nursery;
	gpointer next;
	for (i = 0; i < next_pin_slot; ++i) {
		next = pin_queue [i];
		fprintf (gc_debug_file, "Nursery range: %p-%p, size: %td\n", first, next, (char*)next-(char*)first);
		first = next;
	}
	next = end_nursery;
	fprintf (gc_debug_file, "Nursery range: %p-%p, size: %td\n", first, next, (char*)next-(char*)first);
}

/* reduce the info in the pin queue, removing duplicate pointers and sorting them */
static void
optimize_pin_queue (int start_slot)
{
	void **start, **cur, **end;
	/* sort and uniq pin_queue: we just sort and we let the rest discard multiple values */
	/* it may be better to keep ranges of pinned memory instead of individually pinning objects */
	DEBUG (5, fprintf (gc_debug_file, "Sorting pin queue, size: %d\n", next_pin_slot));
	if ((next_pin_slot - start_slot) > 1)
		sort_addresses (pin_queue + start_slot, next_pin_slot - start_slot);
	start = cur = pin_queue + start_slot;
	end = pin_queue + next_pin_slot;
	while (cur < end) {
		*start = *cur++;
		while (*start == *cur && cur < end)
			cur++;
		start++;
	};
	next_pin_slot = start - pin_queue;
	DEBUG (5, fprintf (gc_debug_file, "Pin queue reduced to size: %d\n", next_pin_slot));
	//DEBUG (6, print_nursery_gaps (start_nursery, end_nursery));
	
}

/* 
 * Scan the memory between start and end and queue values which could be pointers
 * to the area between start_nursery and end_nursery for later consideration.
 * Typically used for thread stacks.
 */
static void
conservatively_pin_objects_from (void **start, void **end, void *start_nursery, void *end_nursery, int pin_type)
{
	int count = 0;
	while (start < end) {
		if (*start >= start_nursery && *start < end_nursery) {
			/*
			 * *start can point to the middle of an object
			 * note: should we handle pointing at the end of an object?
			 * pinning in C# code disallows pointing at the end of an object
			 * but there is some small chance that an optimizing C compiler
			 * may keep the only reference to an object by pointing
			 * at the end of it. We ignore this small chance for now.
			 * Pointers to the end of an object are indistinguishable
			 * from pointers to the start of the next object in memory
			 * so if we allow that we'd need to pin two objects...
			 * We queue the pointer in an array, the
			 * array will then be sorted and uniqued. This way
			 * we can coalesce several pinning pointers and it should
			 * be faster since we'd do a memory scan with increasing
			 * addresses. Note: we can align the address to the allocation
			 * alignment, so the unique process is more effective.
			 */
			mword addr = (mword)*start;
			addr &= ~(ALLOC_ALIGN - 1);
			if (addr >= (mword)start_nursery && addr < (mword)end_nursery)
				pin_stage_ptr ((void*)addr);
			if (heap_dump_file)
				pin_stats_register_address ((char*)addr, pin_type);
			DEBUG (6, if (count) fprintf (gc_debug_file, "Pinning address %p\n", (void*)addr));
			count++;
		}
		start++;
	}
	DEBUG (7, if (count) fprintf (gc_debug_file, "found %d potential pinned heap pointers\n", count));
}

/*
 * Debugging function: find in the conservative roots where @obj is being pinned.
 */
static G_GNUC_UNUSED void
find_pinning_reference (char *obj, size_t size)
{
	RootRecord *root;
	int i;
	char *endobj = obj + size;
	for (i = 0; i < roots_hash_size [0]; ++i) {
		for (root = roots_hash [0][i]; root; root = root->next) {
			/* if desc is non-null it has precise info */
			if (!root->root_desc) {
				char ** start = (char**)root->start_root;
				while (start < (char**)root->end_root) {
					if (*start >= obj && *start < endobj) {
						DEBUG (0, fprintf (gc_debug_file, "Object %p referenced in pinned roots %p-%p (at %p in record %p)\n", obj, root->start_root, root->end_root, start, root));
					}
					start++;
				}
			}
		}
	}
	find_pinning_ref_from_thread (obj, size);
}

/*
 * The first thing we do in a collection is to identify pinned objects.
 * This function considers all the areas of memory that need to be
 * conservatively scanned.
 */
static void
pin_from_roots (void *start_nursery, void *end_nursery)
{
	RootRecord *root;
	int i;
	DEBUG (2, fprintf (gc_debug_file, "Scanning pinned roots (%d bytes, %d/%d entries)\n", (int)roots_size, num_roots_entries [ROOT_TYPE_NORMAL], num_roots_entries [ROOT_TYPE_PINNED]));
	/* objects pinned from the API are inside these roots */
	for (i = 0; i < roots_hash_size [ROOT_TYPE_PINNED]; ++i) {
		for (root = roots_hash [ROOT_TYPE_PINNED][i]; root; root = root->next) {
			DEBUG (6, fprintf (gc_debug_file, "Pinned roots %p-%p\n", root->start_root, root->end_root));
			conservatively_pin_objects_from ((void**)root->start_root, (void**)root->end_root, start_nursery, end_nursery, PIN_TYPE_OTHER);
		}
	}
	/* now deal with the thread stacks
	 * in the future we should be able to conservatively scan only:
	 * *) the cpu registers
	 * *) the unmanaged stack frames
	 * *) the _last_ managed stack frame
	 * *) pointers slots in managed frames
	 */
	scan_thread_data (start_nursery, end_nursery, FALSE);

	evacuate_pin_staging_area ();
}

static CopyOrMarkObjectFunc user_copy_or_mark_func;
static GrayQueue *user_copy_or_mark_queue;

static void
single_arg_user_copy_or_mark (void **obj)
{
	user_copy_or_mark_func (obj, user_copy_or_mark_queue);
}

/*
 * The memory area from start_root to end_root contains pointers to objects.
 * Their position is precisely described by @desc (this means that the pointer
 * can be either NULL or the pointer to the start of an object).
 * This functions copies them to to_space updates them.
 *
 * This function is not thread-safe!
 */
static void
precisely_scan_objects_from (CopyOrMarkObjectFunc copy_func, void** start_root, void** end_root, char* n_start, char *n_end, mword desc, GrayQueue *queue)
{
	switch (desc & ROOT_DESC_TYPE_MASK) {
	case ROOT_DESC_BITMAP:
		desc >>= ROOT_DESC_TYPE_SHIFT;
		while (desc) {
			if ((desc & 1) && *start_root) {
				copy_func (start_root, queue);
				DEBUG (9, fprintf (gc_debug_file, "Overwrote root at %p with %p\n", start_root, *start_root));
				drain_gray_stack (queue);
			}
			desc >>= 1;
			start_root++;
		}
		return;
	case ROOT_DESC_COMPLEX: {
		gsize *bitmap_data = complex_descriptors + (desc >> ROOT_DESC_TYPE_SHIFT);
		int bwords = (*bitmap_data) - 1;
		void **start_run = start_root;
		bitmap_data++;
		while (bwords-- > 0) {
			gsize bmap = *bitmap_data++;
			void **objptr = start_run;
			while (bmap) {
				if ((bmap & 1) && *objptr) {
					copy_func (objptr, queue);
					DEBUG (9, fprintf (gc_debug_file, "Overwrote root at %p with %p\n", objptr, *objptr));
					drain_gray_stack (queue);
				}
				bmap >>= 1;
				++objptr;
			}
			start_run += GC_BITS_PER_WORD;
		}
		break;
	}
	case ROOT_DESC_USER: {
		MonoGCRootMarkFunc marker = user_descriptors [desc >> ROOT_DESC_TYPE_SHIFT];
		user_copy_or_mark_func = copy_func;
		user_copy_or_mark_queue = queue;
		marker (start_root, single_arg_user_copy_or_mark);
		user_copy_or_mark_func = NULL;
		user_copy_or_mark_queue = NULL;
		break;
	}
	case ROOT_DESC_RUN_LEN:
		g_assert_not_reached ();
	default:
		g_assert_not_reached ();
	}
}

void
mono_sgen_update_heap_boundaries (mword low, mword high)
{
	mword old;

	do {
		old = lowest_heap_address;
		if (low >= old)
			break;
	} while (SGEN_CAS_PTR ((gpointer*)&lowest_heap_address, (gpointer)low, (gpointer)old) != (gpointer)old);

	do {
		old = highest_heap_address;
		if (high <= old)
			break;
	} while (SGEN_CAS_PTR ((gpointer*)&highest_heap_address, (gpointer)high, (gpointer)old) != (gpointer)old);
}

static Fragment*
alloc_fragment (void)
{
	Fragment *frag = fragment_freelist;
	if (frag) {
		fragment_freelist = frag->next;
		frag->next = NULL;
		return frag;
	}
	frag = mono_sgen_alloc_internal (INTERNAL_MEM_FRAGMENT);
	frag->next = NULL;
	return frag;
}

/* size must be a power of 2 */
void*
mono_sgen_alloc_os_memory_aligned (mword size, mword alignment, gboolean activate)
{
	/* Allocate twice the memory to be able to put the block on an aligned address */
	char *mem = mono_sgen_alloc_os_memory (size + alignment, activate);
	char *aligned;

	g_assert (mem);

	aligned = (char*)((mword)(mem + (alignment - 1)) & ~(alignment - 1));
	g_assert (aligned >= mem && aligned + size <= mem + size + alignment && !((mword)aligned & (alignment - 1)));

	if (aligned > mem)
		mono_sgen_free_os_memory (mem, aligned - mem);
	if (aligned + size < mem + size + alignment)
		mono_sgen_free_os_memory (aligned + size, (mem + size + alignment) - (aligned + size));

	return aligned;
}

/*
 * Allocate and setup the data structures needed to be able to allocate objects
 * in the nursery. The nursery is stored in nursery_section.
 */
static void
alloc_nursery (void)
{
	GCMemSection *section;
	char *data;
	int scan_starts;
	Fragment *frag;
	int alloc_size;

	if (nursery_section)
		return;
	DEBUG (2, fprintf (gc_debug_file, "Allocating nursery size: %lu\n", (unsigned long)nursery_size));
	/* later we will alloc a larger area for the nursery but only activate
	 * what we need. The rest will be used as expansion if we have too many pinned
	 * objects in the existing nursery.
	 */
	/* FIXME: handle OOM */
	section = mono_sgen_alloc_internal (INTERNAL_MEM_SECTION);

	g_assert (nursery_size == DEFAULT_NURSERY_SIZE);
	alloc_size = nursery_size;
#ifdef SGEN_ALIGN_NURSERY
	data = major_collector.alloc_heap (alloc_size, alloc_size, DEFAULT_NURSERY_BITS);
#else
	data = major_collector.alloc_heap (alloc_size, 0, DEFAULT_NURSERY_BITS);
#endif
	nursery_start = data;
	nursery_real_end = nursery_start + nursery_size;
	mono_sgen_update_heap_boundaries ((mword)nursery_start, (mword)nursery_real_end);
	nursery_next = nursery_start;
	DEBUG (4, fprintf (gc_debug_file, "Expanding nursery size (%p-%p): %lu, total: %lu\n", data, data + alloc_size, (unsigned long)nursery_size, (unsigned long)total_alloc));
	section->data = section->next_data = data;
	section->size = alloc_size;
	section->end_data = nursery_real_end;
	scan_starts = (alloc_size + SCAN_START_SIZE - 1) / SCAN_START_SIZE;
	section->scan_starts = mono_sgen_alloc_internal_dynamic (sizeof (char*) * scan_starts, INTERNAL_MEM_SCAN_STARTS);
	section->num_scan_start = scan_starts;
	section->block.role = MEMORY_ROLE_GEN0;
	section->block.next = NULL;

	nursery_section = section;

	/* Setup the single first large fragment */
	frag = alloc_fragment ();
	frag->fragment_start = nursery_start;
	frag->fragment_limit = nursery_start;
	frag->fragment_end = nursery_real_end;
	nursery_frag_real_end = nursery_real_end;
	/* FIXME: frag here is lost */
}

void*
mono_gc_get_nursery (int *shift_bits, size_t *size)
{
	*size = nursery_size;
#ifdef SGEN_ALIGN_NURSERY
	*shift_bits = DEFAULT_NURSERY_BITS;
#else
	*shift_bits = -1;
#endif
	return nursery_start;
}

static void
scan_finalizer_entries (CopyOrMarkObjectFunc copy_func, FinalizeEntry *list, GrayQueue *queue)
{
	FinalizeEntry *fin;

	for (fin = list; fin; fin = fin->next) {
		if (!fin->object)
			continue;
		DEBUG (5, fprintf (gc_debug_file, "Scan of fin ready object: %p (%s)\n", fin->object, safe_name (fin->object)));
		copy_func (&fin->object, queue);
	}
}

static mword fragment_total = 0;
/*
 * We found a fragment of free memory in the nursery: memzero it and if
 * it is big enough, add it to the list of fragments that can be used for
 * allocation.
 */
static void
add_nursery_frag (size_t frag_size, char* frag_start, char* frag_end)
{
	Fragment *fragment;
	DEBUG (4, fprintf (gc_debug_file, "Found empty fragment: %p-%p, size: %zd\n", frag_start, frag_end, frag_size));
	binary_protocol_empty (frag_start, frag_size);
	/* Not worth dealing with smaller fragments: need to tune */
	if (frag_size >= FRAGMENT_MIN_SIZE) {
		/* memsetting just the first chunk start is bound to provide better cache locality */
		if (nursery_clear_policy == CLEAR_AT_GC)
			memset (frag_start, 0, frag_size);

		fragment = alloc_fragment ();
		fragment->fragment_start = frag_start;
		fragment->fragment_limit = frag_start;
		fragment->fragment_end = frag_end;
		fragment->next = nursery_fragments;
		nursery_fragments = fragment;
		fragment_total += frag_size;
	} else {
		/* Clear unused fragments, pinning depends on this */
		/*TODO place an int[] here instead of the memset if size justify it*/
		memset (frag_start, 0, frag_size);
	}
}

static const char*
generation_name (int generation)
{
	switch (generation) {
	case GENERATION_NURSERY: return "nursery";
	case GENERATION_OLD: return "old";
	default: g_assert_not_reached ();
	}
}

static DisappearingLinkHashTable*
get_dislink_hash_table (int generation)
{
	switch (generation) {
	case GENERATION_NURSERY: return &minor_disappearing_link_hash;
	case GENERATION_OLD: return &major_disappearing_link_hash;
	default: g_assert_not_reached ();
	}
}

static FinalizeEntryHashTable*
get_finalize_entry_hash_table (int generation)
{
	switch (generation) {
	case GENERATION_NURSERY: return &minor_finalizable_hash;
	case GENERATION_OLD: return &major_finalizable_hash;
	default: g_assert_not_reached ();
	}
}

static void
finish_gray_stack (char *start_addr, char *end_addr, int generation, GrayQueue *queue)
{
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	int fin_ready;
	int ephemeron_rounds = 0;
	CopyOrMarkObjectFunc copy_func = current_collection_generation == GENERATION_NURSERY ? major_collector.copy_object : major_collector.copy_or_mark_object;

	/*
	 * We copied all the reachable objects. Now it's the time to copy
	 * the objects that were not referenced by the roots, but by the copied objects.
	 * we built a stack of objects pointed to by gray_start: they are
	 * additional roots and we may add more items as we go.
	 * We loop until gray_start == gray_objects which means no more objects have
	 * been added. Note this is iterative: no recursion is involved.
	 * We need to walk the LO list as well in search of marked big objects
	 * (use a flag since this is needed only on major collections). We need to loop
	 * here as well, so keep a counter of marked LO (increasing it in copy_object).
	 *   To achieve better cache locality and cache usage, we drain the gray stack 
	 * frequently, after each object is copied, and just finish the work here.
	 */
	drain_gray_stack (queue);
	TV_GETTIME (atv);
	DEBUG (2, fprintf (gc_debug_file, "%s generation done\n", generation_name (generation)));
	/* walk the finalization queue and move also the objects that need to be
	 * finalized: use the finalized objects as new roots so the objects they depend
	 * on are also not reclaimed. As with the roots above, only objects in the nursery
	 * are marked/copied.
	 * We need a loop here, since objects ready for finalizers may reference other objects
	 * that are fin-ready. Speedup with a flag?
	 */
	do {
		/*
		 * Walk the ephemeron tables marking all values with reachable keys. This must be completely done
		 * before processing finalizable objects to avoid finalizing reachable values.
		 *
		 * It must be done inside the finalizaters loop since objects must not be removed from CWT tables
		 * while they are been finalized.
		 */
		int done_with_ephemerons = 0;
		do {
			done_with_ephemerons = mark_ephemerons_in_range (copy_func, start_addr, end_addr, queue);
			drain_gray_stack (queue);
			++ephemeron_rounds;
		} while (!done_with_ephemerons);

		fin_ready = num_ready_finalizers;
		finalize_in_range (copy_func, start_addr, end_addr, generation, queue);
		if (generation == GENERATION_OLD)
			finalize_in_range (copy_func, nursery_start, nursery_real_end, GENERATION_NURSERY, queue);

		/* drain the new stack that might have been created */
		DEBUG (6, fprintf (gc_debug_file, "Precise scan of gray area post fin\n"));
		drain_gray_stack (queue);
	} while (fin_ready != num_ready_finalizers);

	/*
	 * Clear ephemeron pairs with unreachable keys.
	 * We pass the copy func so we can figure out if an array was promoted or not.
	 */
	clear_unreachable_ephemerons (copy_func, start_addr, end_addr, queue);

	TV_GETTIME (btv);
	DEBUG (2, fprintf (gc_debug_file, "Finalize queue handling scan for %s generation: %d usecs %d ephemeron roundss\n", generation_name (generation), TV_ELAPSED (atv, btv), ephemeron_rounds));

	/*
	 * handle disappearing links
	 * Note we do this after checking the finalization queue because if an object
	 * survives (at least long enough to be finalized) we don't clear the link.
	 * This also deals with a possible issue with the monitor reclamation: with the Boehm
	 * GC a finalized object my lose the monitor because it is cleared before the finalizer is
	 * called.
	 */
	g_assert (gray_object_queue_is_empty (queue));
	for (;;) {
		null_link_in_range (copy_func, start_addr, end_addr, generation, queue);
		if (generation == GENERATION_OLD)
			null_link_in_range (copy_func, start_addr, end_addr, GENERATION_NURSERY, queue);
		if (gray_object_queue_is_empty (queue))
			break;
		drain_gray_stack (queue);
	}

	g_assert (gray_object_queue_is_empty (queue));
}

void
mono_sgen_check_section_scan_starts (GCMemSection *section)
{
	int i;
	for (i = 0; i < section->num_scan_start; ++i) {
		if (section->scan_starts [i]) {
			guint size = safe_object_get_size ((MonoObject*) section->scan_starts [i]);
			g_assert (size >= sizeof (MonoObject) && size <= MAX_SMALL_OBJ_SIZE);
		}
	}
}

static void
check_scan_starts (void)
{
	if (!do_scan_starts_check)
		return;
	mono_sgen_check_section_scan_starts (nursery_section);
	major_collector.check_scan_starts ();
}

static int last_num_pinned = 0;

static void
build_nursery_fragments (void **start, int num_entries)
{
	char *frag_start, *frag_end;
	size_t frag_size;
	int i;

	while (nursery_fragments) {
		Fragment *next = nursery_fragments->next;
		nursery_fragments->next = fragment_freelist;
		fragment_freelist = nursery_fragments;
		nursery_fragments = next;
	}
	frag_start = nursery_start;
	fragment_total = 0;
	/* clear scan starts */
	memset (nursery_section->scan_starts, 0, nursery_section->num_scan_start * sizeof (gpointer));
	for (i = 0; i < num_entries; ++i) {
		frag_end = start [i];
		/* remove the pin bit from pinned objects */
		unpin_object (frag_end);
		nursery_section->scan_starts [((char*)frag_end - (char*)nursery_section->data)/SCAN_START_SIZE] = frag_end;
		frag_size = frag_end - frag_start;
		if (frag_size)
			add_nursery_frag (frag_size, frag_start, frag_end);
		frag_size = ALIGN_UP (safe_object_get_size ((MonoObject*)start [i]));
		frag_start = (char*)start [i] + frag_size;
	}
	nursery_last_pinned_end = frag_start;
	frag_end = nursery_real_end;
	frag_size = frag_end - frag_start;
	if (frag_size)
		add_nursery_frag (frag_size, frag_start, frag_end);
	if (!nursery_fragments) {
		DEBUG (1, fprintf (gc_debug_file, "Nursery fully pinned (%d)\n", num_entries));
		for (i = 0; i < num_entries; ++i) {
			DEBUG (3, fprintf (gc_debug_file, "Bastard pinning obj %p (%s), size: %d\n", start [i], safe_name (start [i]), safe_object_get_size (start [i])));
		}
		degraded_mode = 1;
	}

	nursery_next = nursery_frag_real_end = NULL;

	/* Clear TLABs for all threads */
	clear_tlabs ();
}

static void
scan_from_registered_roots (CopyOrMarkObjectFunc copy_func, char *addr_start, char *addr_end, int root_type, GrayQueue *queue)
{
	int i;
	RootRecord *root;
	for (i = 0; i < roots_hash_size [root_type]; ++i) {
		for (root = roots_hash [root_type][i]; root; root = root->next) {
			DEBUG (6, fprintf (gc_debug_file, "Precise root scan %p-%p (desc: %p)\n", root->start_root, root->end_root, (void*)root->root_desc));
			precisely_scan_objects_from (copy_func, (void**)root->start_root, (void**)root->end_root, addr_start, addr_end, root->root_desc, queue);
		}
	}
}

void
mono_sgen_dump_occupied (char *start, char *end, char *section_start)
{
	fprintf (heap_dump_file, "<occupied offset=\"%td\" size=\"%td\"/>\n", start - section_start, end - start);
}

void
mono_sgen_dump_section (GCMemSection *section, const char *type)
{
	char *start = section->data;
	char *end = section->data + section->size;
	char *occ_start = NULL;
	GCVTable *vt;
	char *old_start = NULL;	/* just for debugging */

	fprintf (heap_dump_file, "<section type=\"%s\" size=\"%lu\">\n", type, (unsigned long)section->size);

	while (start < end) {
		guint size;
		MonoClass *class;

		if (!*(void**)start) {
			if (occ_start) {
				mono_sgen_dump_occupied (occ_start, start, section->data);
				occ_start = NULL;
			}
			start += sizeof (void*); /* should be ALLOC_ALIGN, really */
			continue;
		}
		g_assert (start < section->next_data);

		if (!occ_start)
			occ_start = start;

		vt = (GCVTable*)LOAD_VTABLE (start);
		class = vt->klass;

		size = ALIGN_UP (safe_object_get_size ((MonoObject*) start));

		/*
		fprintf (heap_dump_file, "<object offset=\"%d\" class=\"%s.%s\" size=\"%d\"/>\n",
				start - section->data,
				vt->klass->name_space, vt->klass->name,
				size);
		*/

		old_start = start;
		start += size;
	}
	if (occ_start)
		mono_sgen_dump_occupied (occ_start, start, section->data);

	fprintf (heap_dump_file, "</section>\n");
}

static void
dump_object (MonoObject *obj, gboolean dump_location)
{
	static char class_name [1024];

	MonoClass *class = mono_object_class (obj);
	int i, j;

	/*
	 * Python's XML parser is too stupid to parse angle brackets
	 * in strings, so we just ignore them;
	 */
	i = j = 0;
	while (class->name [i] && j < sizeof (class_name) - 1) {
		if (!strchr ("<>\"", class->name [i]))
			class_name [j++] = class->name [i];
		++i;
	}
	g_assert (j < sizeof (class_name));
	class_name [j] = 0;

	fprintf (heap_dump_file, "<object class=\"%s.%s\" size=\"%d\"",
			class->name_space, class_name,
			safe_object_get_size (obj));
	if (dump_location) {
		const char *location;
		if (ptr_in_nursery (obj))
			location = "nursery";
		else if (safe_object_get_size (obj) <= MAX_SMALL_OBJ_SIZE)
			location = "major";
		else
			location = "LOS";
		fprintf (heap_dump_file, " location=\"%s\"", location);
	}
	fprintf (heap_dump_file, "/>\n");
}

static void
dump_heap (const char *type, int num, const char *reason)
{
	ObjectList *list;
	LOSObject *bigobj;

	fprintf (heap_dump_file, "<collection type=\"%s\" num=\"%d\"", type, num);
	if (reason)
		fprintf (heap_dump_file, " reason=\"%s\"", reason);
	fprintf (heap_dump_file, ">\n");
	fprintf (heap_dump_file, "<other-mem-usage type=\"mempools\" size=\"%ld\"/>\n", mono_mempool_get_bytes_allocated ());
	mono_sgen_dump_internal_mem_usage (heap_dump_file);
	fprintf (heap_dump_file, "<pinned type=\"stack\" bytes=\"%zu\"/>\n", pinned_byte_counts [PIN_TYPE_STACK]);
	/* fprintf (heap_dump_file, "<pinned type=\"static-data\" bytes=\"%d\"/>\n", pinned_byte_counts [PIN_TYPE_STATIC_DATA]); */
	fprintf (heap_dump_file, "<pinned type=\"other\" bytes=\"%zu\"/>\n", pinned_byte_counts [PIN_TYPE_OTHER]);

	fprintf (heap_dump_file, "<pinned-objects>\n");
	for (list = pinned_objects; list; list = list->next)
		dump_object (list->obj, TRUE);
	fprintf (heap_dump_file, "</pinned-objects>\n");

	mono_sgen_dump_section (nursery_section, "nursery");

	major_collector.dump_heap (heap_dump_file);

	fprintf (heap_dump_file, "<los>\n");
	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		dump_object ((MonoObject*)bigobj->data, FALSE);
	fprintf (heap_dump_file, "</los>\n");

	fprintf (heap_dump_file, "</collection>\n");
}

void
mono_sgen_register_moved_object (void *obj, void *destination)
{
	g_assert (mono_profiler_events & MONO_PROFILE_GC_MOVES);

	/* FIXME: handle this for parallel collector */
	g_assert (!major_collector.is_parallel);

	if (moved_objects_idx == MOVED_OBJECTS_NUM) {
		mono_profiler_gc_moves (moved_objects, moved_objects_idx);
		moved_objects_idx = 0;
	}
	moved_objects [moved_objects_idx++] = obj;
	moved_objects [moved_objects_idx++] = destination;
}

static void
init_stats (void)
{
	static gboolean inited = FALSE;

	if (inited)
		return;

	mono_counters_register ("Minor fragment clear", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_pre_collection_fragment_clear);
	mono_counters_register ("Minor pinning", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_pinning);
	mono_counters_register ("Minor scan remsets", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_scan_remsets);
	mono_counters_register ("Minor scan cardtables", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_scan_card_table);
	mono_counters_register ("Minor scan pinned", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_scan_pinned);
	mono_counters_register ("Minor scan registered roots", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_scan_registered_roots);
	mono_counters_register ("Minor scan thread data", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_scan_thread_data);
	mono_counters_register ("Minor finish gray stack", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_finish_gray_stack);
	mono_counters_register ("Minor fragment creation", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_minor_fragment_creation);

	mono_counters_register ("Major fragment clear", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_pre_collection_fragment_clear);
	mono_counters_register ("Major pinning", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_pinning);
	mono_counters_register ("Major scan pinned", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_scan_pinned);
	mono_counters_register ("Major scan registered roots", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_scan_registered_roots);
	mono_counters_register ("Major scan thread data", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_scan_thread_data);
	mono_counters_register ("Major scan alloc_pinned", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_scan_alloc_pinned);
	mono_counters_register ("Major scan finalized", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_scan_finalized);
	mono_counters_register ("Major scan big objects", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_scan_big_objects);
	mono_counters_register ("Major finish gray stack", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_finish_gray_stack);
	mono_counters_register ("Major free big objects", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_free_bigobjs);
	mono_counters_register ("Major LOS sweep", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_los_sweep);
	mono_counters_register ("Major sweep", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_sweep);
	mono_counters_register ("Major fragment creation", MONO_COUNTER_GC | MONO_COUNTER_LONG, &time_major_fragment_creation);


#ifdef HEAVY_STATISTICS
	mono_counters_register ("WBarrier set field", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_set_field);
	mono_counters_register ("WBarrier set arrayref", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_set_arrayref);
	mono_counters_register ("WBarrier arrayref copy", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_arrayref_copy);
	mono_counters_register ("WBarrier generic store called", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_generic_store);
	mono_counters_register ("WBarrier generic store stored", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_generic_store_remset);
	mono_counters_register ("WBarrier set root", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_set_root);
	mono_counters_register ("WBarrier value copy", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_value_copy);
	mono_counters_register ("WBarrier object copy", MONO_COUNTER_GC | MONO_COUNTER_INT, &stat_wbarrier_object_copy);

	mono_counters_register ("# objects allocated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_objects_alloced);
	mono_counters_register ("bytes allocated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_bytes_alloced);
	mono_counters_register ("# objects allocated degraded", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_objects_alloced_degraded);
	mono_counters_register ("bytes allocated degraded", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_bytes_alloced_degraded);
	mono_counters_register ("bytes allocated in LOS", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_bytes_alloced_los);

	mono_counters_register ("# copy_object() called (nursery)", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_copy_object_called_nursery);
	mono_counters_register ("# objects copied (nursery)", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_objects_copied_nursery);
	mono_counters_register ("# copy_object() called (major)", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_copy_object_called_major);
	mono_counters_register ("# objects copied (major)", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_objects_copied_major);

	mono_counters_register ("# scan_object() called (nursery)", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_scan_object_called_nursery);
	mono_counters_register ("# scan_object() called (major)", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_scan_object_called_major);

	mono_counters_register ("# nursery copy_object() failed from space", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_nursery_copy_object_failed_from_space);
	mono_counters_register ("# nursery copy_object() failed forwarded", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_nursery_copy_object_failed_forwarded);
	mono_counters_register ("# nursery copy_object() failed pinned", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_nursery_copy_object_failed_pinned);

	mono_counters_register ("# wasted fragments used", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_wasted_fragments_used);
	mono_counters_register ("bytes in wasted fragments", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_wasted_fragments_bytes);

	mono_counters_register ("Store remsets", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_store_remsets);
	mono_counters_register ("Unique store remsets", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_store_remsets_unique);
	mono_counters_register ("Saved remsets 1", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_saved_remsets_1);
	mono_counters_register ("Saved remsets 2", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_saved_remsets_2);
	mono_counters_register ("Non-global remsets processed", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_local_remsets_processed);
	mono_counters_register ("Global remsets added", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_added);
	mono_counters_register ("Global remsets re-added", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_readded);
	mono_counters_register ("Global remsets processed", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_processed);
	mono_counters_register ("Global remsets discarded", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_global_remsets_discarded);
#endif

	inited = TRUE;
}

static gboolean
need_major_collection (void)
{
	mword los_alloced = los_memory_usage - MIN (last_los_memory_usage, los_memory_usage);
	return minor_collection_sections_alloced * major_collector.section_size + los_alloced > minor_collection_allowance;
}

/*
 * Collect objects in the nursery.  Returns whether to trigger a major
 * collection.
 */
static gboolean
collect_nursery (size_t requested_size)
{
	size_t max_garbage_amount;
	char *orig_nursery_next;
	TV_DECLARE (all_atv);
	TV_DECLARE (all_btv);
	TV_DECLARE (atv);
	TV_DECLARE (btv);

	mono_perfcounters->gc_collections0++;

	current_collection_generation = GENERATION_NURSERY;

	binary_protocol_collection (GENERATION_NURSERY);
	check_scan_starts ();

	degraded_mode = 0;
	orig_nursery_next = nursery_next;
	nursery_next = MAX (nursery_next, nursery_last_pinned_end);
	/* FIXME: optimize later to use the higher address where an object can be present */
	nursery_next = MAX (nursery_next, nursery_real_end);

	DEBUG (1, fprintf (gc_debug_file, "Start nursery collection %d %p-%p, size: %d\n", num_minor_gcs, nursery_start, nursery_next, (int)(nursery_next - nursery_start)));
	max_garbage_amount = nursery_next - nursery_start;
	g_assert (nursery_section->size >= max_garbage_amount);

	/* world must be stopped already */
	TV_GETTIME (all_atv);
	atv = all_atv;

	/* Pinning depends on this */
	clear_nursery_fragments (orig_nursery_next);

	TV_GETTIME (btv);
	time_minor_pre_collection_fragment_clear += TV_ELAPSED_MS (atv, btv);

	if (xdomain_checks)
		check_for_xdomain_refs ();

	nursery_section->next_data = nursery_next;

	major_collector.start_nursery_collection ();

	gray_object_queue_init (&gray_queue, mono_sgen_get_unmanaged_allocator ());

	num_minor_gcs++;
	mono_stats.minor_gc_count ++;

	global_remset_cache_clear ();

	/* pin from pinned handles */
	init_pinning ();
	mono_profiler_gc_event (MONO_GC_EVENT_MARK_START, 0);
	pin_from_roots (nursery_start, nursery_next);
	/* identify pinned objects */
	optimize_pin_queue (0);
	next_pin_slot = pin_objects_from_addresses (nursery_section, pin_queue, pin_queue + next_pin_slot, nursery_start, nursery_next, &gray_queue);
	nursery_section->pin_queue_start = pin_queue;
	nursery_section->pin_queue_num_entries = next_pin_slot;
	TV_GETTIME (atv);
	time_minor_pinning += TV_ELAPSED_MS (btv, atv);
	DEBUG (2, fprintf (gc_debug_file, "Finding pinned pointers: %d in %d usecs\n", next_pin_slot, TV_ELAPSED (btv, atv)));
	DEBUG (4, fprintf (gc_debug_file, "Start scan with %d pinned objects\n", next_pin_slot));

	if (consistency_check_at_minor_collection)
		check_consistency ();

	/* 
	 * walk all the roots and copy the young objects to the old generation,
	 * starting from to_space
	 */

	scan_from_remsets (nursery_start, nursery_next, &gray_queue);
	/* we don't have complete write barrier yet, so we scan all the old generation sections */
	TV_GETTIME (btv);
	time_minor_scan_remsets += TV_ELAPSED_MS (atv, btv);
	DEBUG (2, fprintf (gc_debug_file, "Old generation scan: %d usecs\n", TV_ELAPSED (atv, btv)));

	if (use_cardtable) {
		atv = btv;
		card_tables_collect_starts (TRUE);
		scan_from_card_tables (nursery_start, nursery_next, &gray_queue);
		TV_GETTIME (btv);
		time_minor_scan_card_table += TV_ELAPSED_MS (atv, btv);
	}

	drain_gray_stack (&gray_queue);

	TV_GETTIME (atv);
	time_minor_scan_pinned += TV_ELAPSED_MS (btv, atv);
	/* registered roots, this includes static fields */
	scan_from_registered_roots (major_collector.copy_object, nursery_start, nursery_next, ROOT_TYPE_NORMAL, &gray_queue);
	scan_from_registered_roots (major_collector.copy_object, nursery_start, nursery_next, ROOT_TYPE_WBARRIER, &gray_queue);
	TV_GETTIME (btv);
	time_minor_scan_registered_roots += TV_ELAPSED_MS (atv, btv);
	/* thread data */
	scan_thread_data (nursery_start, nursery_next, TRUE);
	TV_GETTIME (atv);
	time_minor_scan_thread_data += TV_ELAPSED_MS (btv, atv);
	btv = atv;

	finish_gray_stack (nursery_start, nursery_next, GENERATION_NURSERY, &gray_queue);
	TV_GETTIME (atv);
	time_minor_finish_gray_stack += TV_ELAPSED_MS (btv, atv);
	mono_profiler_gc_event (MONO_GC_EVENT_MARK_END, 0);

	/* walk the pin_queue, build up the fragment list of free memory, unmark
	 * pinned objects as we go, memzero() the empty fragments so they are ready for the
	 * next allocations.
	 */
	mono_profiler_gc_event (MONO_GC_EVENT_RECLAIM_START, 0);
	build_nursery_fragments (pin_queue, next_pin_slot);
	mono_profiler_gc_event (MONO_GC_EVENT_RECLAIM_END, 0);
	TV_GETTIME (btv);
	time_minor_fragment_creation += TV_ELAPSED_MS (atv, btv);
	DEBUG (2, fprintf (gc_debug_file, "Fragment creation: %d usecs, %lu bytes available\n", TV_ELAPSED (atv, btv), (unsigned long)fragment_total));

	if (consistency_check_at_minor_collection)
		check_major_refs ();

	major_collector.finish_nursery_collection ();

	TV_GETTIME (all_btv);
	mono_stats.minor_gc_time_usecs += TV_ELAPSED (all_atv, all_btv);

	if (heap_dump_file)
		dump_heap ("minor", num_minor_gcs - 1, NULL);

	/* prepare the pin queue for the next collection */
	last_num_pinned = next_pin_slot;
	next_pin_slot = 0;
	if (fin_ready_list || critical_fin_list) {
		DEBUG (4, fprintf (gc_debug_file, "Finalizer-thread wakeup: ready %d\n", num_ready_finalizers));
		mono_gc_finalize_notify ();
	}
	pin_stats_reset ();

	g_assert (gray_object_queue_is_empty (&gray_queue));

	if (use_cardtable)
		card_tables_collect_starts (FALSE);

	check_scan_starts ();

	binary_protocol_flush_buffers (FALSE);

	current_collection_generation = -1;

	return need_major_collection ();
}

static void
major_do_collection (const char *reason)
{
	LOSObject *bigobj, *prevbo;
	TV_DECLARE (all_atv);
	TV_DECLARE (all_btv);
	TV_DECLARE (atv);
	TV_DECLARE (btv);
	/* FIXME: only use these values for the precise scan
	 * note that to_space pointers should be excluded anyway...
	 */
	char *heap_start = NULL;
	char *heap_end = (char*)-1;
	int old_num_major_sections = major_collector.get_num_major_sections ();
	int num_major_sections, num_major_sections_saved, save_target, allowance_target;
	mword los_memory_saved, los_memory_alloced, old_los_memory_usage;

	mono_perfcounters->gc_collections1++;

	/*
	 * A domain could have been freed, resulting in
	 * los_memory_usage being less than last_los_memory_usage.
	 */
	los_memory_alloced = los_memory_usage - MIN (last_los_memory_usage, los_memory_usage);
	old_los_memory_usage = los_memory_usage;

	//count_ref_nonref_objs ();
	//consistency_check ();

	binary_protocol_collection (GENERATION_OLD);
	check_scan_starts ();
	gray_object_queue_init (&gray_queue, mono_sgen_get_unmanaged_allocator ());
	if (major_collector.is_parallel)
		gray_object_queue_init (&workers_distribute_gray_queue, mono_sgen_get_unmanaged_allocator ());

	degraded_mode = 0;
	DEBUG (1, fprintf (gc_debug_file, "Start major collection %d\n", num_major_gcs));
	num_major_gcs++;
	mono_stats.major_gc_count ++;

	/* world must be stopped already */
	TV_GETTIME (all_atv);
	atv = all_atv;

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
	global_remset_cache_clear ();
	if (use_cardtable)
		card_table_clear ();

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
	mono_sgen_find_section_pin_queue_start_end (nursery_section);
	major_collector.find_pin_queue_start_ends (WORKERS_DISTRIBUTE_GRAY_QUEUE);
	/* identify possible pointers to the insize of large objects */
	DEBUG (6, fprintf (gc_debug_file, "Pinning from large objects\n"));
	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next) {
		int dummy;
		if (mono_sgen_find_optimized_pin_queue_area (bigobj->data, (char*)bigobj->data + bigobj->size, &dummy)) {
			pin_object (bigobj->data);
			/* FIXME: only enqueue if object has references */
			GRAY_OBJECT_ENQUEUE (WORKERS_DISTRIBUTE_GRAY_QUEUE, bigobj->data);
			if (heap_dump_file)
				mono_sgen_pin_stats_register_object ((char*) bigobj->data, safe_object_get_size ((MonoObject*) bigobj->data));
			DEBUG (6, fprintf (gc_debug_file, "Marked large object %p (%s) size: %lu from roots\n", bigobj->data, safe_name (bigobj->data), (unsigned long)bigobj->size));
		}
	}
	/* second pass for the sections */
	mono_sgen_pin_objects_in_section (nursery_section, WORKERS_DISTRIBUTE_GRAY_QUEUE);
	major_collector.pin_objects (WORKERS_DISTRIBUTE_GRAY_QUEUE);

	TV_GETTIME (btv);
	time_major_pinning += TV_ELAPSED_MS (atv, btv);
	DEBUG (2, fprintf (gc_debug_file, "Finding pinned pointers: %d in %d usecs\n", next_pin_slot, TV_ELAPSED (atv, btv)));
	DEBUG (4, fprintf (gc_debug_file, "Start scan with %d pinned objects\n", next_pin_slot));

	major_collector.init_to_space ();

	workers_start_all_workers (1);

	TV_GETTIME (atv);
	time_major_scan_pinned += TV_ELAPSED_MS (btv, atv);

	/* registered roots, this includes static fields */
	scan_from_registered_roots (major_collector.copy_or_mark_object, heap_start, heap_end, ROOT_TYPE_NORMAL, WORKERS_DISTRIBUTE_GRAY_QUEUE);
	scan_from_registered_roots (major_collector.copy_or_mark_object, heap_start, heap_end, ROOT_TYPE_WBARRIER, WORKERS_DISTRIBUTE_GRAY_QUEUE);
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
	scan_finalizer_entries (major_collector.copy_or_mark_object, fin_ready_list, WORKERS_DISTRIBUTE_GRAY_QUEUE);
	scan_finalizer_entries (major_collector.copy_or_mark_object, critical_fin_list, WORKERS_DISTRIBUTE_GRAY_QUEUE);
	TV_GETTIME (atv);
	time_major_scan_finalized += TV_ELAPSED_MS (btv, atv);
	DEBUG (2, fprintf (gc_debug_file, "Root scan: %d usecs\n", TV_ELAPSED (btv, atv)));

	TV_GETTIME (btv);
	time_major_scan_big_objects += TV_ELAPSED_MS (atv, btv);

	if (major_collector.is_parallel) {
		while (!gray_object_queue_is_empty (WORKERS_DISTRIBUTE_GRAY_QUEUE)) {
			workers_distribute_gray_queue_sections ();
			usleep (2000);
		}
	}
	workers_change_num_working (-1);
	workers_join ();

	if (major_collector.is_parallel)
		g_assert (gray_object_queue_is_empty (&gray_queue));

	/* all the objects in the heap */
	finish_gray_stack (heap_start, heap_end, GENERATION_OLD, &gray_queue);
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

	TV_GETTIME (btv);
	time_major_free_bigobjs += TV_ELAPSED_MS (atv, btv);

	los_sweep ();

	TV_GETTIME (atv);
	time_major_los_sweep += TV_ELAPSED_MS (btv, atv);

	major_collector.sweep ();

	TV_GETTIME (btv);
	time_major_sweep += TV_ELAPSED_MS (atv, btv);

	/* walk the pin_queue, build up the fragment list of free memory, unmark
	 * pinned objects as we go, memzero() the empty fragments so they are ready for the
	 * next allocations.
	 */
	build_nursery_fragments (nursery_section->pin_queue_start, nursery_section->pin_queue_num_entries);

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

	g_assert (gray_object_queue_is_empty (&gray_queue));

	num_major_sections = major_collector.get_num_major_sections ();

	num_major_sections_saved = MAX (old_num_major_sections - num_major_sections, 0);
	los_memory_saved = MAX (old_los_memory_usage - los_memory_usage, 1);

	save_target = ((num_major_sections * major_collector.section_size) + los_memory_saved) / 2;
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
	allowance_target = (mword)((double)save_target * (double)(minor_collection_sections_alloced * major_collector.section_size + los_memory_alloced) / (double)(num_major_sections_saved * major_collector.section_size + los_memory_saved));

	minor_collection_allowance = MAX (MIN (allowance_target, num_major_sections * major_collector.section_size + los_memory_usage), MIN_MINOR_COLLECTION_ALLOWANCE);

	minor_collection_sections_alloced = 0;
	last_los_memory_usage = los_memory_usage;

	major_collector.finish_major_collection ();

	check_scan_starts ();

	binary_protocol_flush_buffers (FALSE);

	//consistency_check ();
}

static void
major_collection (const char *reason)
{
	if (g_getenv ("MONO_GC_NO_MAJOR")) {
		collect_nursery (0);
		return;
	}

	current_collection_generation = GENERATION_OLD;
	major_do_collection (reason);
	current_collection_generation = -1;
}

/*
 * When deciding if it's better to collect or to expand, keep track
 * of how much garbage was reclaimed with the last collection: if it's too
 * little, expand.
 * This is called when we could not allocate a small object.
 */
static void __attribute__((noinline))
minor_collect_or_expand_inner (size_t size)
{
	int do_minor_collection = 1;

	g_assert (nursery_section);
	if (do_minor_collection) {
		mono_profiler_gc_event (MONO_GC_EVENT_START, 0);
		stop_world (0);
		if (collect_nursery (size)) {
			mono_profiler_gc_event (MONO_GC_EVENT_START, 1);
			major_collection ("minor overflow");
			/* keep events symmetric */
			mono_profiler_gc_event (MONO_GC_EVENT_END, 1);
		}
		DEBUG (2, fprintf (gc_debug_file, "Heap size: %lu, LOS size: %lu\n", (unsigned long)total_alloc, (unsigned long)los_memory_usage));
		restart_world (0);
		/* this also sets the proper pointers for the next allocation */
		if (!search_fragment_for_size (size)) {
			int i;
			/* TypeBuilder and MonoMethod are killing mcs with fragmentation */
			DEBUG (1, fprintf (gc_debug_file, "nursery collection didn't find enough room for %zd alloc (%d pinned)\n", size, last_num_pinned));
			for (i = 0; i < last_num_pinned; ++i) {
				DEBUG (3, fprintf (gc_debug_file, "Bastard pinning obj %p (%s), size: %d\n", pin_queue [i], safe_name (pin_queue [i]), safe_object_get_size (pin_queue [i])));
			}
			degraded_mode = 1;
		}
		mono_profiler_gc_event (MONO_GC_EVENT_END, 0);
	}
	//report_internal_mem_usage ();
}

/*
 * ######################################################################
 * ########  Memory allocation from the OS
 * ######################################################################
 * This section of code deals with getting memory from the OS and
 * allocating memory for GC-internal data structures.
 * Internal memory can be handled with a freelist for small objects.
 */

/*
 * Debug reporting.
 */
G_GNUC_UNUSED static void
report_internal_mem_usage (void)
{
	printf ("Internal memory usage:\n");
	mono_sgen_report_internal_mem_usage ();
	printf ("Pinned memory usage:\n");
	major_collector.report_pinned_memory_usage ();
}

/*
 * Allocate a big chunk of memory from the OS (usually 64KB to several megabytes).
 * This must not require any lock.
 */
void*
mono_sgen_alloc_os_memory (size_t size, int activate)
{
	void *ptr;
	unsigned long prot_flags = activate? MONO_MMAP_READ|MONO_MMAP_WRITE: MONO_MMAP_NONE;

	prot_flags |= MONO_MMAP_PRIVATE | MONO_MMAP_ANON;
	size += pagesize - 1;
	size &= ~(pagesize - 1);
	ptr = mono_valloc (0, size, prot_flags);
	/* FIXME: CAS */
	total_alloc += size;
	return ptr;
}

/*
 * Free the memory returned by mono_sgen_alloc_os_memory (), returning it to the OS.
 */
void
mono_sgen_free_os_memory (void *addr, size_t size)
{
	mono_vfree (addr, size);

	size += pagesize - 1;
	size &= ~(pagesize - 1);
	/* FIXME: CAS */
	total_alloc -= size;
}

/*
 * ######################################################################
 * ########  Object allocation
 * ######################################################################
 * This section of code deals with allocating memory for objects.
 * There are several ways:
 * *) allocate large objects
 * *) allocate normal objects
 * *) fast lock-free allocation
 * *) allocation of pinned objects
 */

static void
setup_fragment (Fragment *frag, Fragment *prev, size_t size)
{
	/* remove from the list */
	if (prev)
		prev->next = frag->next;
	else
		nursery_fragments = frag->next;
	nursery_next = frag->fragment_start;
	nursery_frag_real_end = frag->fragment_end;

	DEBUG (4, fprintf (gc_debug_file, "Using nursery fragment %p-%p, size: %td (req: %zd)\n", nursery_next, nursery_frag_real_end, nursery_frag_real_end - nursery_next, size));
	frag->next = fragment_freelist;
	fragment_freelist = frag;
}

/* check if we have a suitable fragment in nursery_fragments to be able to allocate
 * an object of size @size
 * Return FALSE if not found (which means we need a collection)
 */
static gboolean
search_fragment_for_size (size_t size)
{
	Fragment *frag, *prev;
	DEBUG (4, fprintf (gc_debug_file, "Searching nursery fragment %p, size: %zd\n", nursery_frag_real_end, size));

	if (nursery_frag_real_end > nursery_next && nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
		/* Clear the remaining space, pinning depends on this */
		memset (nursery_next, 0, nursery_frag_real_end - nursery_next);

	prev = NULL;
	for (frag = nursery_fragments; frag; frag = frag->next) {
		if (size <= (frag->fragment_end - frag->fragment_start)) {
			setup_fragment (frag, prev, size);
			return TRUE;
		}
		prev = frag;
	}
	return FALSE;
}

/*
 * Same as search_fragment_for_size but if search for @desired_size fails, try to satisfy @minimum_size.
 * This improves nursery usage.
 */
static int
search_fragment_for_size_range (size_t desired_size, size_t minimum_size)
{
	Fragment *frag, *prev, *min_prev;
	DEBUG (4, fprintf (gc_debug_file, "Searching nursery fragment %p, desired size: %zd minimum size %zd\n", nursery_frag_real_end, desired_size, minimum_size));

	if (nursery_frag_real_end > nursery_next && nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
		/* Clear the remaining space, pinning depends on this */
		memset (nursery_next, 0, nursery_frag_real_end - nursery_next);

	min_prev = GINT_TO_POINTER (-1);
	prev = NULL;

	for (frag = nursery_fragments; frag; frag = frag->next) {
		int frag_size = frag->fragment_end - frag->fragment_start;
		if (desired_size <= frag_size) {
			setup_fragment (frag, prev, desired_size);
			return desired_size;
		}
		if (minimum_size <= frag_size)
			min_prev = prev;

		prev = frag;
	}

	if (min_prev != GINT_TO_POINTER (-1)) {
		int frag_size;
		if (min_prev)
			frag = min_prev->next;
		else
			frag = nursery_fragments;

		frag_size = frag->fragment_end - frag->fragment_start;
		HEAVY_STAT (++stat_wasted_fragments_used);
		HEAVY_STAT (stat_wasted_fragments_bytes += frag_size);

		setup_fragment (frag, min_prev, minimum_size);
		return frag_size;
	}

	return 0;
}

static void*
alloc_degraded (MonoVTable *vtable, size_t size)
{
	if (need_major_collection ()) {
		mono_profiler_gc_event (MONO_GC_EVENT_START, 1);
		stop_world (1);
		major_collection ("degraded overflow");
		restart_world (1);
		mono_profiler_gc_event (MONO_GC_EVENT_END, 1);
	}

	degraded_mode += size;
	return major_collector.alloc_degraded (vtable, size);
}

/*
 * Provide a variant that takes just the vtable for small fixed-size objects.
 * The aligned size is already computed and stored in vt->gc_descr.
 * Note: every SCAN_START_SIZE or so we are given the chance to do some special
 * processing. We can keep track of where objects start, for example,
 * so when we scan the thread stacks for pinned objects, we can start
 * a search for the pinned object in SCAN_START_SIZE chunks.
 */
static void*
mono_gc_alloc_obj_nolock (MonoVTable *vtable, size_t size)
{
	/* FIXME: handle OOM */
	void **p;
	char *new_next;
	TLAB_ACCESS_INIT;

	HEAVY_STAT (++stat_objects_alloced);
	if (size <= MAX_SMALL_OBJ_SIZE)
		HEAVY_STAT (stat_bytes_alloced += size);
	else
		HEAVY_STAT (stat_bytes_alloced_los += size);

	size = ALIGN_UP (size);

	g_assert (vtable->gc_descr);

	if (G_UNLIKELY (collect_before_allocs)) {
		if (nursery_section) {
			mono_profiler_gc_event (MONO_GC_EVENT_START, 0);
			stop_world (0);
			collect_nursery (0);
			restart_world (0);
			mono_profiler_gc_event (MONO_GC_EVENT_END, 0);
			if (!degraded_mode && !search_fragment_for_size (size)) {
				// FIXME:
				g_assert_not_reached ();
			}
		}
	}

	/*
	 * We must already have the lock here instead of after the
	 * fast path because we might be interrupted in the fast path
	 * (after confirming that new_next < TLAB_TEMP_END) by the GC,
	 * and we'll end up allocating an object in a fragment which
	 * no longer belongs to us.
	 *
	 * The managed allocator does not do this, but it's treated
	 * specially by the world-stopping code.
	 */

	if (size > MAX_SMALL_OBJ_SIZE) {
		p = alloc_large_inner (vtable, size);
	} else {
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;
		TLAB_NEXT = new_next;

		if (G_LIKELY (new_next < TLAB_TEMP_END)) {
			/* Fast path */

			/* 
			 * FIXME: We might need a memory barrier here so the change to tlab_next is 
			 * visible before the vtable store.
			 */

			DEBUG (6, fprintf (gc_debug_file, "Allocated object %p, vtable: %p (%s), size: %zd\n", p, vtable, vtable->klass->name, size));
			binary_protocol_alloc (p , vtable, size);
			g_assert (*p == NULL);
			*p = vtable;

			g_assert (TLAB_NEXT == new_next);

			return p;
		}

		/* Slow path */

		/* there are two cases: the object is too big or we run out of space in the TLAB */
		/* we also reach here when the thread does its first allocation after a minor 
		 * collection, since the tlab_ variables are initialized to NULL.
		 * there can be another case (from ORP), if we cooperate with the runtime a bit:
		 * objects that need finalizers can have the high bit set in their size
		 * so the above check fails and we can readily add the object to the queue.
		 * This avoids taking again the GC lock when registering, but this is moot when
		 * doing thread-local allocation, so it may not be a good idea.
		 */
		g_assert (TLAB_NEXT == new_next);
		if (TLAB_NEXT >= TLAB_REAL_END) {
			/* 
			 * Run out of space in the TLAB. When this happens, some amount of space
			 * remains in the TLAB, but not enough to satisfy the current allocation
			 * request. Currently, we retire the TLAB in all cases, later we could
			 * keep it if the remaining space is above a treshold, and satisfy the
			 * allocation directly from the nursery.
			 */
			TLAB_NEXT -= size;
			/* when running in degraded mode, we continue allocing that way
			 * for a while, to decrease the number of useless nursery collections.
			 */
			if (degraded_mode && degraded_mode < DEFAULT_NURSERY_SIZE) {
				p = alloc_degraded (vtable, size);
				binary_protocol_alloc_degraded (p, vtable, size);
				return p;
			}

			/*FIXME This codepath is current deadcode since tlab_size > MAX_SMALL_OBJ_SIZE*/
			if (size > tlab_size) {
				/* Allocate directly from the nursery */
				if (nursery_next + size >= nursery_frag_real_end) {
					if (!search_fragment_for_size (size)) {
						minor_collect_or_expand_inner (size);
						if (degraded_mode) {
							p = alloc_degraded (vtable, size);
							binary_protocol_alloc_degraded (p, vtable, size);
							return p;
						}
					}
				}

				p = (void*)nursery_next;
				nursery_next += size;
				if (nursery_next > nursery_frag_real_end) {
					// no space left
					g_assert (0);
				}

				if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
					memset (p, 0, size);
			} else {
				int alloc_size = tlab_size;
				int available_in_nursery = nursery_frag_real_end - nursery_next;
				if (TLAB_START)
					DEBUG (3, fprintf (gc_debug_file, "Retire TLAB: %p-%p [%ld]\n", TLAB_START, TLAB_REAL_END, (long)(TLAB_REAL_END - TLAB_NEXT - size)));

				if (alloc_size >= available_in_nursery) {
					if (available_in_nursery > MAX_NURSERY_TLAB_WASTE && available_in_nursery > size) {
						alloc_size = available_in_nursery;
					} else {
						alloc_size = search_fragment_for_size_range (tlab_size, size);
						if (!alloc_size) {
							alloc_size = tlab_size;
							minor_collect_or_expand_inner (tlab_size);
							if (degraded_mode) {
								p = alloc_degraded (vtable, size);
								binary_protocol_alloc_degraded (p, vtable, size);
								return p;
							}
						}
					}
				}

				/* Allocate a new TLAB from the current nursery fragment */
				TLAB_START = nursery_next;
				nursery_next += alloc_size;
				TLAB_NEXT = TLAB_START;
				TLAB_REAL_END = TLAB_START + alloc_size;
				TLAB_TEMP_END = TLAB_START + MIN (SCAN_START_SIZE, alloc_size);

				if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION)
					memset (TLAB_START, 0, alloc_size);

				/* Allocate from the TLAB */
				p = (void*)TLAB_NEXT;
				TLAB_NEXT += size;
				g_assert (TLAB_NEXT <= TLAB_REAL_END);

				nursery_section->scan_starts [((char*)p - (char*)nursery_section->data)/SCAN_START_SIZE] = (char*)p;
			}
		} else {
			/* Reached tlab_temp_end */

			/* record the scan start so we can find pinned objects more easily */
			nursery_section->scan_starts [((char*)p - (char*)nursery_section->data)/SCAN_START_SIZE] = (char*)p;
			/* we just bump tlab_temp_end as well */
			TLAB_TEMP_END = MIN (TLAB_REAL_END, TLAB_NEXT + SCAN_START_SIZE);
			DEBUG (5, fprintf (gc_debug_file, "Expanding local alloc: %p-%p\n", TLAB_NEXT, TLAB_TEMP_END));
		}
	}

	DEBUG (6, fprintf (gc_debug_file, "Allocated object %p, vtable: %p (%s), size: %zd\n", p, vtable, vtable->klass->name, size));
	binary_protocol_alloc (p, vtable, size);
	*p = vtable;

	return p;
}

static void*
mono_gc_try_alloc_obj_nolock (MonoVTable *vtable, size_t size)
{
	void **p;
	char *new_next;
	TLAB_ACCESS_INIT;

	size = ALIGN_UP (size);

	g_assert (vtable->gc_descr);
	if (size <= MAX_SMALL_OBJ_SIZE) {
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;
		TLAB_NEXT = new_next;

		if (G_LIKELY (new_next < TLAB_TEMP_END)) {
			/* Fast path */

			/* 
			 * FIXME: We might need a memory barrier here so the change to tlab_next is 
			 * visible before the vtable store.
			 */

			HEAVY_STAT (++stat_objects_alloced);
			HEAVY_STAT (stat_bytes_alloced += size);

			DEBUG (6, fprintf (gc_debug_file, "Allocated object %p, vtable: %p (%s), size: %zd\n", p, vtable, vtable->klass->name, size));
			binary_protocol_alloc (p, vtable, size);
			g_assert (*p == NULL);
			*p = vtable;

			g_assert (TLAB_NEXT == new_next);

			return p;
		}
	}
	return NULL;
}

void*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size)
{
	void *res;
#ifndef DISABLE_CRITICAL_REGION
	TLAB_ACCESS_INIT;
	ENTER_CRITICAL_REGION;
	res = mono_gc_try_alloc_obj_nolock (vtable, size);
	if (res) {
		EXIT_CRITICAL_REGION;
		return res;
	}
	EXIT_CRITICAL_REGION;
#endif
	LOCK_GC;
	res = mono_gc_alloc_obj_nolock (vtable, size);
	UNLOCK_GC;
	return res;
}

void*
mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length)
{
	MonoArray *arr;
#ifndef DISABLE_CRITICAL_REGION
	TLAB_ACCESS_INIT;
	ENTER_CRITICAL_REGION;
	arr = mono_gc_try_alloc_obj_nolock (vtable, size);
	if (arr) {
		arr->max_length = max_length;
		EXIT_CRITICAL_REGION;
		return arr;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	arr = mono_gc_alloc_obj_nolock (vtable, size);
	arr->max_length = max_length;

	UNLOCK_GC;

	return arr;
}

void*
mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size)
{
	MonoArray *arr;
	MonoArrayBounds *bounds;

	LOCK_GC;

	arr = mono_gc_alloc_obj_nolock (vtable, size);
	arr->max_length = max_length;

	bounds = (MonoArrayBounds*)((char*)arr + size - bounds_size);
	arr->bounds = bounds;

	UNLOCK_GC;

	return arr;
}

void*
mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len)
{
	MonoString *str;
#ifndef DISABLE_CRITICAL_REGION
	TLAB_ACCESS_INIT;
	ENTER_CRITICAL_REGION;
	str = mono_gc_try_alloc_obj_nolock (vtable, size);
	if (str) {
		str->length = len;
		EXIT_CRITICAL_REGION;
		return str;
	}
	EXIT_CRITICAL_REGION;
#endif

	LOCK_GC;

	str = mono_gc_alloc_obj_nolock (vtable, size);
	str->length = len;

	UNLOCK_GC;

	return str;
}

/*
 * To be used for interned strings and possibly MonoThread, reflection handles.
 * We may want to explicitly free these objects.
 */
void*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size)
{
	/* FIXME: handle OOM */
	void **p;
	size = ALIGN_UP (size);
	LOCK_GC;
	if (size > MAX_SMALL_OBJ_SIZE) {
		/* large objects are always pinned anyway */
		p = alloc_large_inner (vtable, size);
	} else {
		DEBUG (9, g_assert (vtable->klass->inited));
		p = major_collector.alloc_small_pinned_obj (size, vtable->klass->has_references);
	}
	DEBUG (6, fprintf (gc_debug_file, "Allocated pinned object %p, vtable: %p (%s), size: %zd\n", p, vtable, vtable->klass->name, size));
	binary_protocol_alloc_pinned (p, vtable, size);
	*p = vtable;
	UNLOCK_GC;
	return p;
}

/*
 * ######################################################################
 * ########  Finalization support
 * ######################################################################
 */

/*
 * this is valid for the nursery: if the object has been forwarded it means it's
 * still refrenced from a root. If it is pinned it's still alive as well.
 * Return TRUE if @obj is ready to be finalized.
 */
#define object_is_fin_ready(obj) (!object_is_pinned (obj) && !object_is_forwarded (obj))

static gboolean
is_critical_finalizer (FinalizeEntry *entry)
{
	MonoObject *obj;
	MonoClass *class;

	if (!mono_defaults.critical_finalizer_object)
		return FALSE;

	obj = entry->object;
	class = ((MonoVTable*)LOAD_VTABLE (obj))->klass;

	return mono_class_has_parent (class, mono_defaults.critical_finalizer_object);
}

static void
queue_finalization_entry (FinalizeEntry *entry) {
	if (is_critical_finalizer (entry)) {
		entry->next = critical_fin_list;
		critical_fin_list = entry;
	} else {
		entry->next = fin_ready_list;
		fin_ready_list = entry;
	}
}

/* LOCKING: requires that the GC lock is held */
static void
rehash_fin_table (FinalizeEntryHashTable *hash_table)
{
	FinalizeEntry **finalizable_hash = hash_table->table;
	mword finalizable_hash_size = hash_table->size;
	int i;
	unsigned int hash;
	FinalizeEntry **new_hash;
	FinalizeEntry *entry, *next;
	int new_size = g_spaced_primes_closest (hash_table->num_registered);

	new_hash = mono_sgen_alloc_internal_dynamic (new_size * sizeof (FinalizeEntry*), INTERNAL_MEM_FIN_TABLE);
	for (i = 0; i < finalizable_hash_size; ++i) {
		for (entry = finalizable_hash [i]; entry; entry = next) {
			hash = mono_object_hash (entry->object) % new_size;
			next = entry->next;
			entry->next = new_hash [hash];
			new_hash [hash] = entry;
		}
	}
	mono_sgen_free_internal_dynamic (finalizable_hash, finalizable_hash_size * sizeof (FinalizeEntry*), INTERNAL_MEM_FIN_TABLE);
	hash_table->table = new_hash;
	hash_table->size = new_size;
}

/* LOCKING: requires that the GC lock is held */
static void
rehash_fin_table_if_necessary (FinalizeEntryHashTable *hash_table)
{
	if (hash_table->num_registered >= hash_table->size * 2)
		rehash_fin_table (hash_table);
}

/* LOCKING: requires that the GC lock is held */
static void
finalize_in_range (CopyOrMarkObjectFunc copy_func, char *start, char *end, int generation, GrayQueue *queue)
{
	FinalizeEntryHashTable *hash_table = get_finalize_entry_hash_table (generation);
	FinalizeEntry *entry, *prev;
	int i;
	FinalizeEntry **finalizable_hash = hash_table->table;
	mword finalizable_hash_size = hash_table->size;

	if (no_finalize)
		return;
	for (i = 0; i < finalizable_hash_size; ++i) {
		prev = NULL;
		for (entry = finalizable_hash [i]; entry;) {
			if ((char*)entry->object >= start && (char*)entry->object < end && !major_collector.is_object_live (entry->object)) {
				gboolean is_fin_ready = object_is_fin_ready (entry->object);
				char *copy = entry->object;
				copy_func ((void**)&copy, queue);
				if (is_fin_ready) {
					char *from;
					FinalizeEntry *next;
					/* remove and put in fin_ready_list */
					if (prev)
						prev->next = entry->next;
					else
						finalizable_hash [i] = entry->next;
					next = entry->next;
					num_ready_finalizers++;
					hash_table->num_registered--;
					queue_finalization_entry (entry);
					/* Make it survive */
					from = entry->object;
					entry->object = copy;
					DEBUG (5, fprintf (gc_debug_file, "Queueing object for finalization: %p (%s) (was at %p) (%d/%d)\n", entry->object, safe_name (entry->object), from, num_ready_finalizers, hash_table->num_registered));
					entry = next;
					continue;
				} else {
					char *from = entry->object;
					if (hash_table == &minor_finalizable_hash && !ptr_in_nursery (copy)) {
						FinalizeEntry *next = entry->next;
						unsigned int major_hash;
						/* remove from the list */
						if (prev)
							prev->next = entry->next;
						else
							finalizable_hash [i] = entry->next;
						hash_table->num_registered--;

						entry->object = copy;

						/* insert it into the major hash */
						rehash_fin_table_if_necessary (&major_finalizable_hash);
						major_hash = mono_object_hash ((MonoObject*) copy) %
							major_finalizable_hash.size;
						entry->next = major_finalizable_hash.table [major_hash];
						major_finalizable_hash.table [major_hash] = entry;
						major_finalizable_hash.num_registered++;

						DEBUG (5, fprintf (gc_debug_file, "Promoting finalization of object %p (%s) (was at %p) to major table\n", copy, safe_name (copy), from));

						entry = next;
						continue;
					} else {
						/* update pointer */
						DEBUG (5, fprintf (gc_debug_file, "Updating object for finalization: %p (%s) (was at %p)\n", entry->object, safe_name (entry->object), from));
						entry->object = copy;
					}
				}
			}
			prev = entry;
			entry = entry->next;
		}
	}
}

static int
object_is_reachable (char *object, char *start, char *end)
{
	/*This happens for non nursery objects during minor collections. We just treat all objects as alive.*/
	if (object < start || object >= end)
		return TRUE;
	return !object_is_fin_ready (object) || major_collector.is_object_live (object);
}

/* LOCKING: requires that the GC lock is held */
static void
null_ephemerons_for_domain (MonoDomain *domain)
{
	EphemeronLinkNode *current = ephemeron_list, *prev = NULL;

	while (current) {
		MonoObject *object = (MonoObject*)current->array;

		if (object && !object->vtable) {
			EphemeronLinkNode *tmp = current;

			if (prev)
				prev->next = current->next;
			else
				ephemeron_list = current->next;

			current = current->next;
			mono_sgen_free_internal (tmp, INTERNAL_MEM_EPHEMERON_LINK);
		} else {
			prev = current;
			current = current->next;
		}
	}
}

/* LOCKING: requires that the GC lock is held */
static void
clear_unreachable_ephemerons (CopyOrMarkObjectFunc copy_func, char *start, char *end, GrayQueue *queue)
{
	int was_in_nursery, was_promoted;
	EphemeronLinkNode *current = ephemeron_list, *prev = NULL;
	MonoArray *array;
	Ephemeron *cur, *array_end;
	char *tombstone;

	while (current) {
		char *object = current->array;

		if (!object_is_reachable (object, start, end)) {
			EphemeronLinkNode *tmp = current;

			DEBUG (5, fprintf (gc_debug_file, "Dead Ephemeron array at %p\n", object));

			if (prev)
				prev->next = current->next;
			else
				ephemeron_list = current->next;

			current = current->next;
			mono_sgen_free_internal (tmp, INTERNAL_MEM_EPHEMERON_LINK);

			continue;
		}

		was_in_nursery = ptr_in_nursery (object);
		copy_func ((void**)&object, queue);
		current->array = object;

		/*The array was promoted, add global remsets for key/values left behind in nursery.*/
		was_promoted = was_in_nursery && !ptr_in_nursery (object);

		DEBUG (5, fprintf (gc_debug_file, "Clearing unreachable entries for ephemeron array at %p\n", object));

		array = (MonoArray*)object;
		cur = mono_array_addr (array, Ephemeron, 0);
		array_end = cur + mono_array_length_fast (array);
		tombstone = (char*)((MonoVTable*)LOAD_VTABLE (object))->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			char *key = (char*)cur->key;

			if (!key || key == tombstone)
				continue;

			DEBUG (5, fprintf (gc_debug_file, "[%td] key %p (%s) value %p (%s)\n", cur - mono_array_addr (array, Ephemeron, 0),
				key, object_is_reachable (key, start, end) ? "reachable" : "unreachable",
				cur->value, cur->value && object_is_reachable (cur->value, start, end) ? "reachable" : "unreachable"));

			if (!object_is_reachable (key, start, end)) {
				cur->key = tombstone;
				cur->value = NULL;
				continue;
			}

			if (was_promoted) {
				if (ptr_in_nursery (key)) {/*key was not promoted*/
					DEBUG (5, fprintf (gc_debug_file, "\tAdded remset to key %p\n", key));
					mono_sgen_add_to_global_remset (&cur->key);
				}
				if (ptr_in_nursery (cur->value)) {/*value was not promoted*/
					DEBUG (5, fprintf (gc_debug_file, "\tAdded remset to value %p\n", cur->value));
					mono_sgen_add_to_global_remset (&cur->value);
				}
			}
		}
		prev = current;
		current = current->next;
	}
}

/* LOCKING: requires that the GC lock is held */
static int
mark_ephemerons_in_range (CopyOrMarkObjectFunc copy_func, char *start, char *end, GrayQueue *queue)
{
	int nothing_marked = 1;
	EphemeronLinkNode *current = ephemeron_list;
	MonoArray *array;
	Ephemeron *cur, *array_end;
	char *tombstone;

	for (current = ephemeron_list; current; current = current->next) {
		char *object = current->array;
		DEBUG (5, fprintf (gc_debug_file, "Ephemeron array at %p\n", object));

		/*We ignore arrays in old gen during minor collections since all objects are promoted by the remset machinery.*/
		if (object < start || object >= end)
			continue;

		/*It has to be alive*/
		if (!object_is_reachable (object, start, end)) {
			DEBUG (5, fprintf (gc_debug_file, "\tnot reachable\n"));
			continue;
		}

		copy_func ((void**)&object, queue);

		array = (MonoArray*)object;
		cur = mono_array_addr (array, Ephemeron, 0);
		array_end = cur + mono_array_length_fast (array);
		tombstone = (char*)((MonoVTable*)LOAD_VTABLE (object))->domain->ephemeron_tombstone;

		for (; cur < array_end; ++cur) {
			char *key = cur->key;

			if (!key || key == tombstone)
				continue;

			DEBUG (5, fprintf (gc_debug_file, "[%td] key %p (%s) value %p (%s)\n", cur - mono_array_addr (array, Ephemeron, 0),
				key, object_is_reachable (key, start, end) ? "reachable" : "unreachable",
				cur->value, cur->value && object_is_reachable (cur->value, start, end) ? "reachable" : "unreachable"));

			if (object_is_reachable (key, start, end)) {
				char *value = cur->value;

				copy_func ((void**)&cur->key, queue);
				if (value) {
					if (!object_is_reachable (value, start, end))
						nothing_marked = 0;
					copy_func ((void**)&cur->value, queue);
				}
			}
		}
	}

	DEBUG (5, fprintf (gc_debug_file, "Ephemeron run finished. Is it done %d\n", nothing_marked));
	return nothing_marked;
}

/* LOCKING: requires that the GC lock is held */
static void
null_link_in_range (CopyOrMarkObjectFunc copy_func, char *start, char *end, int generation, GrayQueue *queue)
{
	DisappearingLinkHashTable *hash = get_dislink_hash_table (generation);
	DisappearingLink **disappearing_link_hash = hash->table;
	int disappearing_link_hash_size = hash->size;
	DisappearingLink *entry, *prev;
	int i;
	if (!hash->num_links)
		return;
	for (i = 0; i < disappearing_link_hash_size; ++i) {
		prev = NULL;
		for (entry = disappearing_link_hash [i]; entry;) {
			char *object = DISLINK_OBJECT (entry);
			if (object >= start && object < end && !major_collector.is_object_live (object)) {
				gboolean track = DISLINK_TRACK (entry);
				if (!track && object_is_fin_ready (object)) {
					void **p = entry->link;
					DisappearingLink *old;
					*p = NULL;
					/* remove from list */
					if (prev)
						prev->next = entry->next;
					else
						disappearing_link_hash [i] = entry->next;
					DEBUG (5, fprintf (gc_debug_file, "Dislink nullified at %p to GCed object %p\n", p, object));
					old = entry->next;
					mono_sgen_free_internal (entry, INTERNAL_MEM_DISLINK);
					entry = old;
					hash->num_links--;
					continue;
				} else {
					char *copy = object;
					copy_func ((void**)&copy, queue);

					/* Update pointer if it's moved.  If the object
					 * has been moved out of the nursery, we need to
					 * remove the link from the minor hash table to
					 * the major one.
					 *
					 * FIXME: what if an object is moved earlier?
					 */

					if (hash == &minor_disappearing_link_hash && !ptr_in_nursery (copy)) {
						void **link = entry->link;
						DisappearingLink *old;
						/* remove from list */
						if (prev)
							prev->next = entry->next;
						else
							disappearing_link_hash [i] = entry->next;
						old = entry->next;
						mono_sgen_free_internal (entry, INTERNAL_MEM_DISLINK);
						entry = old;
						hash->num_links--;

						add_or_remove_disappearing_link ((MonoObject*)copy, link,
							track, GENERATION_OLD);

						DEBUG (5, fprintf (gc_debug_file, "Upgraded dislink at %p to major because object %p moved to %p\n", link, object, copy));

						continue;
					} else {
						/* We set the track resurrection bit to
						 * FALSE if the object is to be finalized
						 * so that the object can be collected in
						 * the next cycle (i.e. after it was
						 * finalized).
						 */
						*entry->link = HIDE_POINTER (copy,
							object_is_fin_ready (object) ? FALSE : track);
						DEBUG (5, fprintf (gc_debug_file, "Updated dislink at %p to %p\n", entry->link, DISLINK_OBJECT (entry)));
					}
				}
			}
			prev = entry;
			entry = entry->next;
		}
	}
}

/* LOCKING: requires that the GC lock is held */
static void
null_links_for_domain (MonoDomain *domain, int generation)
{
	DisappearingLinkHashTable *hash = get_dislink_hash_table (generation);
	DisappearingLink **disappearing_link_hash = hash->table;
	int disappearing_link_hash_size = hash->size;
	DisappearingLink *entry, *prev;
	int i;
	for (i = 0; i < disappearing_link_hash_size; ++i) {
		prev = NULL;
		for (entry = disappearing_link_hash [i]; entry; ) {
			char *object = DISLINK_OBJECT (entry);
			if (object && !((MonoObject*)object)->vtable) {
				DisappearingLink *next = entry->next;

				if (prev)
					prev->next = next;
				else
					disappearing_link_hash [i] = next;

				if (*(entry->link)) {
					*(entry->link) = NULL;
					g_warning ("Disappearing link %p not freed", entry->link);
				} else {
					mono_sgen_free_internal (entry, INTERNAL_MEM_DISLINK);
				}

				entry = next;
				continue;
			}
			prev = entry;
			entry = entry->next;
		}
	}
}

/* LOCKING: requires that the GC lock is held */
static int
finalizers_for_domain (MonoDomain *domain, MonoObject **out_array, int out_size,
	FinalizeEntryHashTable *hash_table)
{
	FinalizeEntry **finalizable_hash = hash_table->table;
	mword finalizable_hash_size = hash_table->size;
	FinalizeEntry *entry, *prev;
	int i, count;

	if (no_finalize || !out_size || !out_array)
		return 0;
	count = 0;
	for (i = 0; i < finalizable_hash_size; ++i) {
		prev = NULL;
		for (entry = finalizable_hash [i]; entry;) {
			if (mono_object_domain (entry->object) == domain) {
				FinalizeEntry *next;
				/* remove and put in out_array */
				if (prev)
					prev->next = entry->next;
				else
					finalizable_hash [i] = entry->next;
				next = entry->next;
				hash_table->num_registered--;
				out_array [count ++] = entry->object;
				DEBUG (5, fprintf (gc_debug_file, "Collecting object for finalization: %p (%s) (%d/%d)\n", entry->object, safe_name (entry->object), num_ready_finalizers, hash_table->num_registered));
				entry = next;
				if (count == out_size)
					return count;
				continue;
			}
			prev = entry;
			entry = entry->next;
		}
	}
	return count;
}

/**
 * mono_gc_finalizers_for_domain:
 * @domain: the unloading appdomain
 * @out_array: output array
 * @out_size: size of output array
 *
 * Store inside @out_array up to @out_size objects that belong to the unloading
 * appdomain @domain. Returns the number of stored items. Can be called repeteadly
 * until it returns 0.
 * The items are removed from the finalizer data structure, so the caller is supposed
 * to finalize them.
 * @out_array should be on the stack to allow the GC to know the objects are still alive.
 */
int
mono_gc_finalizers_for_domain (MonoDomain *domain, MonoObject **out_array, int out_size)
{
	int result;

	LOCK_GC;
	result = finalizers_for_domain (domain, out_array, out_size, &minor_finalizable_hash);
	if (result < out_size) {
		result += finalizers_for_domain (domain, out_array + result, out_size - result,
			&major_finalizable_hash);
	}
	UNLOCK_GC;

	return result;
}

static void
register_for_finalization (MonoObject *obj, void *user_data, int generation)
{
	FinalizeEntryHashTable *hash_table = get_finalize_entry_hash_table (generation);
	FinalizeEntry **finalizable_hash;
	mword finalizable_hash_size;
	FinalizeEntry *entry, *prev;
	unsigned int hash;
	if (no_finalize)
		return;
	g_assert (user_data == NULL || user_data == mono_gc_run_finalize);
	hash = mono_object_hash (obj);
	LOCK_GC;
	rehash_fin_table_if_necessary (hash_table);
	finalizable_hash = hash_table->table;
	finalizable_hash_size = hash_table->size;
	hash %= finalizable_hash_size;
	prev = NULL;
	for (entry = finalizable_hash [hash]; entry; entry = entry->next) {
		if (entry->object == obj) {
			if (!user_data) {
				/* remove from the list */
				if (prev)
					prev->next = entry->next;
				else
					finalizable_hash [hash] = entry->next;
				hash_table->num_registered--;
				DEBUG (5, fprintf (gc_debug_file, "Removed finalizer %p for object: %p (%s) (%d)\n", entry, obj, obj->vtable->klass->name, hash_table->num_registered));
				mono_sgen_free_internal (entry, INTERNAL_MEM_FINALIZE_ENTRY);
			}
			UNLOCK_GC;
			return;
		}
		prev = entry;
	}
	if (!user_data) {
		/* request to deregister, but already out of the list */
		UNLOCK_GC;
		return;
	}
	entry = mono_sgen_alloc_internal (INTERNAL_MEM_FINALIZE_ENTRY);
	entry->object = obj;
	entry->next = finalizable_hash [hash];
	finalizable_hash [hash] = entry;
	hash_table->num_registered++;
	DEBUG (5, fprintf (gc_debug_file, "Added finalizer %p for object: %p (%s) (%d) to %s table\n", entry, obj, obj->vtable->klass->name, hash_table->num_registered, generation_name (generation)));
	UNLOCK_GC;
}

void
mono_gc_register_for_finalization (MonoObject *obj, void *user_data)
{
	if (ptr_in_nursery (obj))
		register_for_finalization (obj, user_data, GENERATION_NURSERY);
	else
		register_for_finalization (obj, user_data, GENERATION_OLD);
}

static void
rehash_dislink (DisappearingLinkHashTable *hash_table)
{
	DisappearingLink **disappearing_link_hash = hash_table->table;
	int disappearing_link_hash_size = hash_table->size;
	int i;
	unsigned int hash;
	DisappearingLink **new_hash;
	DisappearingLink *entry, *next;
	int new_size = g_spaced_primes_closest (hash_table->num_links);

	new_hash = mono_sgen_alloc_internal_dynamic (new_size * sizeof (DisappearingLink*), INTERNAL_MEM_DISLINK_TABLE);
	for (i = 0; i < disappearing_link_hash_size; ++i) {
		for (entry = disappearing_link_hash [i]; entry; entry = next) {
			hash = mono_aligned_addr_hash (entry->link) % new_size;
			next = entry->next;
			entry->next = new_hash [hash];
			new_hash [hash] = entry;
		}
	}
	mono_sgen_free_internal_dynamic (disappearing_link_hash,
			disappearing_link_hash_size * sizeof (DisappearingLink*), INTERNAL_MEM_DISLINK_TABLE);
	hash_table->table = new_hash;
	hash_table->size = new_size;
}

/* LOCKING: assumes the GC lock is held */
static void
add_or_remove_disappearing_link (MonoObject *obj, void **link, gboolean track, int generation)
{
	DisappearingLinkHashTable *hash_table = get_dislink_hash_table (generation);
	DisappearingLink *entry, *prev;
	unsigned int hash;
	DisappearingLink **disappearing_link_hash = hash_table->table;
	int disappearing_link_hash_size = hash_table->size;

	if (hash_table->num_links >= disappearing_link_hash_size * 2) {
		rehash_dislink (hash_table);
		disappearing_link_hash = hash_table->table;
		disappearing_link_hash_size = hash_table->size;
	}
	/* FIXME: add check that link is not in the heap */
	hash = mono_aligned_addr_hash (link) % disappearing_link_hash_size;
	entry = disappearing_link_hash [hash];
	prev = NULL;
	for (; entry; entry = entry->next) {
		/* link already added */
		if (link == entry->link) {
			/* NULL obj means remove */
			if (obj == NULL) {
				if (prev)
					prev->next = entry->next;
				else
					disappearing_link_hash [hash] = entry->next;
				hash_table->num_links--;
				DEBUG (5, fprintf (gc_debug_file, "Removed dislink %p (%d) from %s table\n", entry, hash_table->num_links, generation_name (generation)));
				mono_sgen_free_internal (entry, INTERNAL_MEM_DISLINK);
				*link = NULL;
			} else {
				*link = HIDE_POINTER (obj, track); /* we allow the change of object */
			}
			return;
		}
		prev = entry;
	}
	if (obj == NULL)
		return;
	entry = mono_sgen_alloc_internal (INTERNAL_MEM_DISLINK);
	*link = HIDE_POINTER (obj, track);
	entry->link = link;
	entry->next = disappearing_link_hash [hash];
	disappearing_link_hash [hash] = entry;
	hash_table->num_links++;
	DEBUG (5, fprintf (gc_debug_file, "Added dislink %p for object: %p (%s) at %p to %s table\n", entry, obj, obj->vtable->klass->name, link, generation_name (generation)));
}

/* LOCKING: assumes the GC lock is held */
static void
mono_gc_register_disappearing_link (MonoObject *obj, void **link, gboolean track)
{
	add_or_remove_disappearing_link (NULL, link, FALSE, GENERATION_NURSERY);
	add_or_remove_disappearing_link (NULL, link, FALSE, GENERATION_OLD);
	if (obj) {
		if (ptr_in_nursery (obj))
			add_or_remove_disappearing_link (obj, link, track, GENERATION_NURSERY);
		else
			add_or_remove_disappearing_link (obj, link, track, GENERATION_OLD);
	}
}

int
mono_gc_invoke_finalizers (void)
{
	FinalizeEntry *entry = NULL;
	gboolean entry_is_critical = FALSE;
	int count = 0;
	void *obj;
	/* FIXME: batch to reduce lock contention */
	while (fin_ready_list || critical_fin_list) {
		LOCK_GC;

		if (entry) {
			FinalizeEntry **list = entry_is_critical ? &critical_fin_list : &fin_ready_list;

			/* We have finalized entry in the last
			   interation, now we need to remove it from
			   the list. */
			if (*list == entry)
				*list = entry->next;
			else {
				FinalizeEntry *e = *list;
				while (e->next != entry)
					e = e->next;
				e->next = entry->next;
			}
			mono_sgen_free_internal (entry, INTERNAL_MEM_FINALIZE_ENTRY);
			entry = NULL;
		}

		/* Now look for the first non-null entry. */
		for (entry = fin_ready_list; entry && !entry->object; entry = entry->next)
			;
		if (entry) {
			entry_is_critical = FALSE;
		} else {
			entry_is_critical = TRUE;
			for (entry = critical_fin_list; entry && !entry->object; entry = entry->next)
				;
		}

		if (entry) {
			g_assert (entry->object);
			num_ready_finalizers--;
			obj = entry->object;
			entry->object = NULL;
			DEBUG (7, fprintf (gc_debug_file, "Finalizing object %p (%s)\n", obj, safe_name (obj)));
		}

		UNLOCK_GC;

		if (!entry)
			break;

		g_assert (entry->object == NULL);
		count++;
		/* the object is on the stack so it is pinned */
		/*g_print ("Calling finalizer for object: %p (%s)\n", entry->object, safe_name (entry->object));*/
		mono_gc_run_finalize (obj, NULL);
	}
	g_assert (!entry);
	return count;
}

gboolean
mono_gc_pending_finalizers (void)
{
	return fin_ready_list || critical_fin_list;
}

/* Negative value to remove */
void
mono_gc_add_memory_pressure (gint64 value)
{
	/* FIXME: Use interlocked functions */
	LOCK_GC;
	memory_pressure += value;
	UNLOCK_GC;
}

void
mono_sgen_register_major_sections_alloced (int num_sections)
{
	minor_collection_sections_alloced += num_sections;
}

mword
mono_sgen_get_minor_collection_allowance (void)
{
	return minor_collection_allowance;
}

/*
 * ######################################################################
 * ########  registered roots support
 * ######################################################################
 */

static void
rehash_roots (gboolean pinned)
{
	int i;
	unsigned int hash;
	RootRecord **new_hash;
	RootRecord *entry, *next;
	int new_size;

	new_size = g_spaced_primes_closest (num_roots_entries [pinned]);
	new_hash = mono_sgen_alloc_internal_dynamic (new_size * sizeof (RootRecord*), INTERNAL_MEM_ROOTS_TABLE);
	for (i = 0; i < roots_hash_size [pinned]; ++i) {
		for (entry = roots_hash [pinned][i]; entry; entry = next) {
			hash = mono_aligned_addr_hash (entry->start_root) % new_size;
			next = entry->next;
			entry->next = new_hash [hash];
			new_hash [hash] = entry;
		}
	}
	mono_sgen_free_internal_dynamic (roots_hash [pinned], roots_hash_size [pinned] * sizeof (RootRecord*), INTERNAL_MEM_ROOTS_TABLE);
	roots_hash [pinned] = new_hash;
	roots_hash_size [pinned] = new_size;
}

static RootRecord*
find_root (int root_type, char *start, guint32 addr_hash)
{
	RootRecord *new_root;

	guint32 hash = addr_hash % roots_hash_size [root_type];
	for (new_root = roots_hash [root_type][hash]; new_root; new_root = new_root->next) {
		/* we allow changing the size and the descriptor (for thread statics etc) */
		if (new_root->start_root == start) {
			return new_root;
		}
	}

	return NULL;
}

/*
 * We do not coalesce roots.
 */
static int
mono_gc_register_root_inner (char *start, size_t size, void *descr, int root_type)
{
	RootRecord *new_root;
	unsigned int hash, addr_hash = mono_aligned_addr_hash (start);
	int i;
	LOCK_GC;
	for (i = 0; i < ROOT_TYPE_NUM; ++i) {
		if (num_roots_entries [i] >= roots_hash_size [i] * 2)
			rehash_roots (i);
	}
	for (i = 0; i < ROOT_TYPE_NUM; ++i) {
		new_root = find_root (i, start, addr_hash);
		/* we allow changing the size and the descriptor (for thread statics etc) */
		if (new_root) {
			size_t old_size = new_root->end_root - new_root->start_root;
			new_root->end_root = new_root->start_root + size;
			g_assert (((new_root->root_desc != 0) && (descr != NULL)) ||
					  ((new_root->root_desc == 0) && (descr == NULL)));
			new_root->root_desc = (mword)descr;
			roots_size += size;
			roots_size -= old_size;
			UNLOCK_GC;
			return TRUE;
		}
	}
	new_root = mono_sgen_alloc_internal (INTERNAL_MEM_ROOT_RECORD);
	if (new_root) {
		new_root->start_root = start;
		new_root->end_root = new_root->start_root + size;
		new_root->root_desc = (mword)descr;
		roots_size += size;
		hash = addr_hash % roots_hash_size [root_type];
		num_roots_entries [root_type]++;
		new_root->next = roots_hash [root_type] [hash];
		roots_hash [root_type][hash] = new_root;
		DEBUG (3, fprintf (gc_debug_file, "Added root %p for range: %p-%p, descr: %p  (%d/%d bytes)\n", new_root, new_root->start_root, new_root->end_root, descr, (int)size, (int)roots_size));
	} else {
		UNLOCK_GC;
		return FALSE;
	}
	UNLOCK_GC;
	return TRUE;
}

int
mono_gc_register_root (char *start, size_t size, void *descr)
{
	return mono_gc_register_root_inner (start, size, descr, descr ? ROOT_TYPE_NORMAL : ROOT_TYPE_PINNED);
}

int
mono_gc_register_root_wbarrier (char *start, size_t size, void *descr)
{
	return mono_gc_register_root_inner (start, size, descr, ROOT_TYPE_WBARRIER);
}

void
mono_gc_deregister_root (char* addr)
{
	RootRecord *tmp, *prev;
	unsigned int hash, addr_hash = mono_aligned_addr_hash (addr);
	int root_type;

	LOCK_GC;
	for (root_type = 0; root_type < ROOT_TYPE_NUM; ++root_type) {
		hash = addr_hash % roots_hash_size [root_type];
		tmp = roots_hash [root_type][hash];
		prev = NULL;
		while (tmp) {
			if (tmp->start_root == (char*)addr) {
				if (prev)
					prev->next = tmp->next;
				else
					roots_hash [root_type][hash] = tmp->next;
				roots_size -= (tmp->end_root - tmp->start_root);
				num_roots_entries [root_type]--;
				DEBUG (3, fprintf (gc_debug_file, "Removed root %p for range: %p-%p\n", tmp, tmp->start_root, tmp->end_root));
				mono_sgen_free_internal (tmp, INTERNAL_MEM_ROOT_RECORD);
				break;
			}
			prev = tmp;
			tmp = tmp->next;
		}
	}
	UNLOCK_GC;
}

/*
 * ######################################################################
 * ########  Thread handling (stop/start code)
 * ######################################################################
 */

/* FIXME: handle large/small config */
#define HASH_PTHREAD_T(id) (((unsigned int)(id) >> 4) * 2654435761u)

static SgenThreadInfo* thread_table [THREAD_HASH_SIZE];

#if USE_SIGNAL_BASED_START_STOP_WORLD

static MonoSemType suspend_ack_semaphore;
static MonoSemType *suspend_ack_semaphore_ptr;
static unsigned int global_stop_count = 0;

static sigset_t suspend_signal_mask;
static mword cur_thread_regs [ARCH_NUM_REGS] = {0};

/* LOCKING: assumes the GC lock is held */
SgenThreadInfo**
mono_sgen_get_thread_table (void)
{
	return thread_table;
}

SgenThreadInfo*
mono_sgen_thread_info_lookup (ARCH_THREAD_TYPE id)
{
	unsigned int hash = HASH_PTHREAD_T (id) % THREAD_HASH_SIZE;
	SgenThreadInfo *info;

	info = thread_table [hash];
	while (info && !ARCH_THREAD_EQUALS (info->id, id)) {
		info = info->next;
	}
	return info;
}

static void
update_current_thread_stack (void *start)
{
	void *ptr = cur_thread_regs;
	SgenThreadInfo *info = mono_sgen_thread_info_lookup (ARCH_GET_THREAD ());
	
	info->stack_start = align_pointer (&ptr);
	g_assert (info->stack_start >= info->stack_start_limit && info->stack_start < info->stack_end);
	ARCH_STORE_REGS (ptr);
	info->stopped_regs = ptr;
	if (gc_callbacks.thread_suspend_func)
		gc_callbacks.thread_suspend_func (info->runtime_data, NULL);
}

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

static gboolean
is_ip_in_managed_allocator (MonoDomain *domain, gpointer ip);

void
mono_sgen_wait_for_suspend_ack (int count)
{
	int i, result;

	for (i = 0; i < count; ++i) {
		while ((result = MONO_SEM_WAIT (suspend_ack_semaphore_ptr)) != 0) {
			if (errno != EINTR) {
				g_error ("sem_wait ()");
			}
		}
	}
}

static int
restart_threads_until_none_in_managed_allocator (void)
{
	SgenThreadInfo *info;
	int i, result, num_threads_died = 0;
	int sleep_duration = -1;

	for (;;) {
		int restart_count = 0, restarted_count = 0;
		/* restart all threads that stopped in the
		   allocator */
		for (i = 0; i < THREAD_HASH_SIZE; ++i) {
			for (info = thread_table [i]; info; info = info->next) {
				if (info->skip)
					continue;
				if (!info->stack_start || info->in_critical_region ||
						is_ip_in_managed_allocator (info->stopped_domain, info->stopped_ip)) {
					binary_protocol_thread_restart ((gpointer)info->id);
#if defined(__MACH__) && MONO_MACH_ARCH_SUPPORTED
					result = thread_resume (pthread_mach_thread_np (info->id));
#else
					result = pthread_kill (info->id, restart_signal_num);
#endif
					if (result == 0) {
						++restart_count;
					} else {
						info->skip = 1;
					}
				} else {
					/* we set the stopped_ip to
					   NULL for threads which
					   we're not restarting so
					   that we can easily identify
					   the others */
					info->stopped_ip = NULL;
					info->stopped_domain = NULL;
				}
			}
		}
		/* if no threads were restarted, we're done */
		if (restart_count == 0)
			break;

#if defined(__MACH__) && MONO_MACH_ARCH_SUPPORTED
		/* mach thread_resume is synchronous so we dont need to wait for them */
#else
		/* wait for the threads to signal their restart */
		mono_sgen_wait_for_suspend_ack (restart_count);
#endif

		if (sleep_duration < 0) {
			sched_yield ();
			sleep_duration = 0;
		} else {
			g_usleep (sleep_duration);
			sleep_duration += 10;
		}

		/* stop them again */
		for (i = 0; i < THREAD_HASH_SIZE; ++i) {
			for (info = thread_table [i]; info; info = info->next) {
				if (info->skip || info->stopped_ip == NULL)
					continue;
#if defined(__MACH__) && MONO_MACH_ARCH_SUPPORTED
				result = thread_suspend (pthread_mach_thread_np (info->id));
#else
				result = pthread_kill (info->id, suspend_signal_num);
#endif
				if (result == 0) {
					++restarted_count;
				} else {
					info->skip = 1;
				}
			}
		}
		/* some threads might have died */
		num_threads_died += restart_count - restarted_count;
#if defined(__MACH__) && MONO_MACH_ARCH_SUPPORTED
		/* mach thread_resume is synchronous so we dont need to wait for them */
#else
		/* wait for the threads to signal their suspension
		   again */
		mono_sgen_wait_for_suspend_ack (restart_count);
#endif
	}

	return num_threads_died;
}

/* LOCKING: assumes the GC lock is held (by the stopping thread) */
static void
suspend_handler (int sig, siginfo_t *siginfo, void *context)
{
	SgenThreadInfo *info;
	pthread_t id;
	int stop_count;
	int old_errno = errno;
	gpointer regs [ARCH_NUM_REGS];
	gpointer stack_start;

	id = pthread_self ();
	info = mono_sgen_thread_info_lookup (id);
	info->stopped_domain = mono_domain_get ();
	info->stopped_ip = (gpointer) ARCH_SIGCTX_IP (context);
	stop_count = global_stop_count;
	/* duplicate signal */
	if (0 && info->stop_count == stop_count) {
		errno = old_errno;
		return;
	}
#ifdef HAVE_KW_THREAD
	/* update the remset info in the thread data structure */
	info->remset = remembered_set;
#endif
	stack_start = (char*) ARCH_SIGCTX_SP (context) - REDZONE_SIZE;
	/* If stack_start is not within the limits, then don't set it
	   in info and we will be restarted. */
	if (stack_start >= info->stack_start_limit && info->stack_start <= info->stack_end) {
		info->stack_start = stack_start;

		ARCH_COPY_SIGCTX_REGS (regs, context);
		info->stopped_regs = regs;
	} else {
		g_assert (!info->stack_start);
	}

	/* Notify the JIT */
	if (gc_callbacks.thread_suspend_func)
		gc_callbacks.thread_suspend_func (info->runtime_data, context);

	DEBUG (4, fprintf (gc_debug_file, "Posting suspend_ack_semaphore for suspend from %p %p\n", info, (gpointer)ARCH_GET_THREAD ()));
	/* notify the waiting thread */
	MONO_SEM_POST (suspend_ack_semaphore_ptr);
	info->stop_count = stop_count;

	/* wait until we receive the restart signal */
	do {
		info->signal = 0;
		sigsuspend (&suspend_signal_mask);
	} while (info->signal != restart_signal_num);

	DEBUG (4, fprintf (gc_debug_file, "Posting suspend_ack_semaphore for resume from %p %p\n", info, (gpointer)ARCH_GET_THREAD ()));
	/* notify the waiting thread */
	MONO_SEM_POST (suspend_ack_semaphore_ptr);

	errno = old_errno;
}

static void
restart_handler (int sig)
{
	SgenThreadInfo *info;
	int old_errno = errno;

	info = mono_sgen_thread_info_lookup (pthread_self ());
	info->signal = restart_signal_num;
	DEBUG (4, fprintf (gc_debug_file, "Restart handler in %p %p\n", info, (gpointer)ARCH_GET_THREAD ()));

	errno = old_errno;
}

static void
acquire_gc_locks (void)
{
	LOCK_INTERRUPTION;
}

static void
release_gc_locks (void)
{
	UNLOCK_INTERRUPTION;
}

static TV_DECLARE (stop_world_time);
static unsigned long max_pause_usec = 0;

/* LOCKING: assumes the GC lock is held */
static int
stop_world (int generation)
{
	int count;

	mono_profiler_gc_event (MONO_GC_EVENT_PRE_STOP_WORLD, generation);
	acquire_gc_locks ();

	update_current_thread_stack (&count);

	global_stop_count++;
	DEBUG (3, fprintf (gc_debug_file, "stopping world n %d from %p %p\n", global_stop_count, mono_sgen_thread_info_lookup (ARCH_GET_THREAD ()), (gpointer)ARCH_GET_THREAD ()));
	TV_GETTIME (stop_world_time);
	count = mono_sgen_thread_handshake (suspend_signal_num);
	count -= restart_threads_until_none_in_managed_allocator ();
	g_assert (count >= 0);
	DEBUG (3, fprintf (gc_debug_file, "world stopped %d thread(s)\n", count));
	mono_profiler_gc_event (MONO_GC_EVENT_POST_STOP_WORLD, generation);
	return count;
}

/* LOCKING: assumes the GC lock is held */
static int
restart_world (int generation)
{
	int count, i;
	SgenThreadInfo *info;
	TV_DECLARE (end_sw);
	unsigned long usec;

	/* notify the profiler of the leftovers */
	if (G_UNLIKELY (mono_profiler_events & MONO_PROFILE_GC_MOVES)) {
		if (moved_objects_idx) {
			mono_profiler_gc_moves (moved_objects, moved_objects_idx);
			moved_objects_idx = 0;
		}
	}
	mono_profiler_gc_event (MONO_GC_EVENT_PRE_START_WORLD, generation);
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			info->stack_start = NULL;
			info->stopped_regs = NULL;
		}
	}

	release_gc_locks ();

	count = mono_sgen_thread_handshake (restart_signal_num);
	TV_GETTIME (end_sw);
	usec = TV_ELAPSED (stop_world_time, end_sw);
	max_pause_usec = MAX (usec, max_pause_usec);
	DEBUG (2, fprintf (gc_debug_file, "restarted %d thread(s) (pause time: %d usec, max: %d)\n", count, (int)usec, (int)max_pause_usec));
	mono_profiler_gc_event (MONO_GC_EVENT_POST_START_WORLD, generation);
	return count;
}

#endif /* USE_SIGNAL_BASED_START_STOP_WORLD */

void
mono_gc_set_gc_callbacks (MonoGCCallbacks *callbacks)
{
	gc_callbacks = *callbacks;
}

MonoGCCallbacks *
mono_gc_get_gc_callbacks ()
{
	return &gc_callbacks;
}

/* Variables holding start/end nursery so it won't have to be passed at every call */
static void *scan_area_arg_start, *scan_area_arg_end;

void
mono_gc_conservatively_scan_area (void *start, void *end)
{
	conservatively_pin_objects_from (start, end, scan_area_arg_start, scan_area_arg_end, PIN_TYPE_STACK);
}

void*
mono_gc_scan_object (void *obj)
{
	g_assert_not_reached ();
	if (current_collection_generation == GENERATION_NURSERY)
		major_collector.copy_object (&obj, &gray_queue);
	else
		major_collector.copy_or_mark_object (&obj, &gray_queue);
	return obj;
}

/*
 * Mark from thread stacks and registers.
 */
static void
scan_thread_data (void *start_nursery, void *end_nursery, gboolean precise)
{
	int i;
	SgenThreadInfo *info;

	scan_area_arg_start = start_nursery;
	scan_area_arg_end = end_nursery;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			if (info->skip) {
				DEBUG (3, fprintf (gc_debug_file, "Skipping dead thread %p, range: %p-%p, size: %td\n", info, info->stack_start, info->stack_end, (char*)info->stack_end - (char*)info->stack_start));
				continue;
			}
			DEBUG (3, fprintf (gc_debug_file, "Scanning thread %p, range: %p-%p, size: %td, pinned=%d\n", info, info->stack_start, info->stack_end, (char*)info->stack_end - (char*)info->stack_start, next_pin_slot));
			if (gc_callbacks.thread_mark_func && !conservative_stack_mark)
				gc_callbacks.thread_mark_func (info->runtime_data, info->stack_start, info->stack_end, precise);
			else if (!precise)
				conservatively_pin_objects_from (info->stack_start, info->stack_end, start_nursery, end_nursery, PIN_TYPE_STACK);

			if (!precise)
				conservatively_pin_objects_from (info->stopped_regs, info->stopped_regs + ARCH_NUM_REGS,
						start_nursery, end_nursery, PIN_TYPE_STACK);
		}
	}
}

static void
find_pinning_ref_from_thread (char *obj, size_t size)
{
	int i;
	SgenThreadInfo *info;
	char *endobj = obj + size;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			char **start = (char**)info->stack_start;
			if (info->skip)
				continue;
			while (start < (char**)info->stack_end) {
				if (*start >= obj && *start < endobj) {
					DEBUG (0, fprintf (gc_debug_file, "Object %p referenced in thread %p (id %p) at %p, stack: %p-%p\n", obj, info, (gpointer)info->id, start, info->stack_start, info->stack_end));
				}
				start++;
			}

			/* FIXME: check info->stopped_regs */
		}
	}
}

static gboolean
ptr_on_stack (void *ptr)
{
	gpointer stack_start = &stack_start;
	SgenThreadInfo *info = mono_sgen_thread_info_lookup (ARCH_GET_THREAD ());

	if (ptr >= stack_start && ptr < (gpointer)info->stack_end)
		return TRUE;
	return FALSE;
}

static mword*
handle_remset (mword *p, void *start_nursery, void *end_nursery, gboolean global, GrayQueue *queue)
{
	void **ptr;
	mword count;
	mword desc;

	if (global)
		HEAVY_STAT (++stat_global_remsets_processed);
	else
		HEAVY_STAT (++stat_local_remsets_processed);

	/* FIXME: exclude stack locations */
	switch ((*p) & REMSET_TYPE_MASK) {
	case REMSET_LOCATION:
		ptr = (void**)(*p);
		//__builtin_prefetch (ptr);
		if (((void*)ptr < start_nursery || (void*)ptr >= end_nursery)) {
			gpointer old = *ptr;
			major_collector.copy_object (ptr, queue);
			DEBUG (9, fprintf (gc_debug_file, "Overwrote remset at %p with %p\n", ptr, *ptr));
			if (old)
				binary_protocol_ptr_update (ptr, old, *ptr, (gpointer)LOAD_VTABLE (*ptr), safe_object_get_size (*ptr));
			if (!global && *ptr >= start_nursery && *ptr < end_nursery) {
				/*
				 * If the object is pinned, each reference to it from nonpinned objects
				 * becomes part of the global remset, which can grow very large.
				 */
				DEBUG (9, fprintf (gc_debug_file, "Add to global remset because of pinning %p (%p %s)\n", ptr, *ptr, safe_name (*ptr)));
				mono_sgen_add_to_global_remset (ptr);
			}
		} else {
			DEBUG (9, fprintf (gc_debug_file, "Skipping remset at %p holding %p\n", ptr, *ptr));
		}
		return p + 1;
	case REMSET_RANGE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery))
			return p + 2;
		count = p [1];
		while (count-- > 0) {
			major_collector.copy_object (ptr, queue);
			DEBUG (9, fprintf (gc_debug_file, "Overwrote remset at %p with %p (count: %d)\n", ptr, *ptr, (int)count));
			if (!global && *ptr >= start_nursery && *ptr < end_nursery)
				mono_sgen_add_to_global_remset (ptr);
			++ptr;
		}
		return p + 2;
	case REMSET_OBJECT:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery))
			return p + 1;
		major_collector.minor_scan_object ((char*)ptr, queue);
		return p + 1;
	case REMSET_VTYPE: {
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		if (((void*)ptr >= start_nursery && (void*)ptr < end_nursery))
			return p + 3;
		desc = p [1];
		count = p [2];
		while (count-- > 0)
			ptr = (void**) major_collector.minor_scan_vtype ((char*)ptr, desc, start_nursery, end_nursery, queue);
		return p + 3;
	}
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

#ifdef HEAVY_STATISTICS
static mword*
collect_store_remsets (RememberedSet *remset, mword *bumper)
{
	mword *p = remset->data;
	mword last = 0;
	mword last1 = 0;
	mword last2 = 0;

	while (p < remset->store_next) {
		switch ((*p) & REMSET_TYPE_MASK) {
		case REMSET_LOCATION:
			*bumper++ = *p;
			if (*p == last)
				++stat_saved_remsets_1;
			last = *p;
			if (*p == last1 || *p == last2) {
				++stat_saved_remsets_2;
			} else {
				last2 = last1;
				last1 = *p;
			}
			p += 1;
			break;
		case REMSET_RANGE:
			p += 2;
			break;
		case REMSET_OBJECT:
			p += 1;
			break;
		case REMSET_VTYPE:
			p += 3;
			break;
		default:
			g_assert_not_reached ();
		}
	}

	return bumper;
}

static void
remset_stats (void)
{
	RememberedSet *remset;
	int size = 0;
	SgenThreadInfo *info;
	int i;
	mword *addresses, *bumper, *p, *r;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			for (remset = info->remset; remset; remset = remset->next)
				size += remset->store_next - remset->data;
		}
	}
	for (remset = freed_thread_remsets; remset; remset = remset->next)
		size += remset->store_next - remset->data;
	for (remset = global_remset; remset; remset = remset->next)
		size += remset->store_next - remset->data;

	bumper = addresses = mono_sgen_alloc_internal_dynamic (sizeof (mword) * size, INTERNAL_MEM_STATISTICS);

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			for (remset = info->remset; remset; remset = remset->next)
				bumper = collect_store_remsets (remset, bumper);
		}
	}
	for (remset = global_remset; remset; remset = remset->next)
		bumper = collect_store_remsets (remset, bumper);
	for (remset = freed_thread_remsets; remset; remset = remset->next)
		bumper = collect_store_remsets (remset, bumper);

	g_assert (bumper <= addresses + size);

	stat_store_remsets += bumper - addresses;

	sort_addresses ((void**)addresses, bumper - addresses);
	p = addresses;
	r = addresses + 1;
	while (r < bumper) {
		if (*r != *p)
			*++p = *r;
		++r;
	}

	stat_store_remsets_unique += p - addresses;

	mono_sgen_free_internal_dynamic (addresses, sizeof (mword) * size, INTERNAL_MEM_STATISTICS);
}
#endif

static void
clear_thread_store_remset_buffer (SgenThreadInfo *info)
{
	*info->store_remset_buffer_index_addr = 0;
	memset (*info->store_remset_buffer_addr, 0, sizeof (gpointer) * STORE_REMSET_BUFFER_SIZE);
}

static size_t
remset_byte_size (RememberedSet *remset)
{
	return sizeof (RememberedSet) + (remset->end_set - remset->data) * sizeof (gpointer);
}

static void
scan_from_remsets (void *start_nursery, void *end_nursery, GrayQueue *queue)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset;
	GenericStoreRememberedSet *store_remset;
	mword *p, *next_p, *store_pos;

#ifdef HEAVY_STATISTICS
	remset_stats ();
#endif

	/* the global one */
	for (remset = global_remset; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning global remset range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		store_pos = remset->data;
		for (p = remset->data; p < remset->store_next; p = next_p) {
			void **ptr = (void**)p [0];

			/*Ignore previously processed remset.*/
			if (!global_remset_location_was_not_added (ptr)) {
				next_p = p + 1;
				continue;
			}

			next_p = handle_remset (p, start_nursery, end_nursery, TRUE, queue);

			/* 
			 * Clear global remsets of locations which no longer point to the 
			 * nursery. Otherwise, they could grow indefinitely between major 
			 * collections.
			 *
			 * Since all global remsets are location remsets, we don't need to unmask the pointer.
			 */
			if (ptr_in_nursery (*ptr)) {
				*store_pos ++ = p [0];
				HEAVY_STAT (++stat_global_remsets_readded);
			}
		}

		/* Truncate the remset */
		remset->store_next = store_pos;
	}

	/* the generic store ones */
	store_remset = generic_store_remsets;
	while (store_remset) {
		GenericStoreRememberedSet *next = store_remset->next;

		for (i = 0; i < STORE_REMSET_BUFFER_SIZE - 1; ++i) {
			gpointer addr = store_remset->data [i];
			if (addr)
				handle_remset ((mword*)&addr, start_nursery, end_nursery, FALSE, queue);
		}

		mono_sgen_free_internal (store_remset, INTERNAL_MEM_STORE_REMSET);

		store_remset = next;
	}
	generic_store_remsets = NULL;

	/* the per-thread ones */
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			RememberedSet *next;
			int j;
			for (remset = info->remset; remset; remset = next) {
				DEBUG (4, fprintf (gc_debug_file, "Scanning remset for thread %p, range: %p-%p, size: %td\n", info, remset->data, remset->store_next, remset->store_next - remset->data));
				for (p = remset->data; p < remset->store_next;)
					p = handle_remset (p, start_nursery, end_nursery, FALSE, queue);
				remset->store_next = remset->data;
				next = remset->next;
				remset->next = NULL;
				if (remset != info->remset) {
					DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
					mono_sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
				}
			}
			for (j = 0; j < *info->store_remset_buffer_index_addr; ++j)
				handle_remset ((mword*)*info->store_remset_buffer_addr + j + 1, start_nursery, end_nursery, FALSE, queue);
			clear_thread_store_remset_buffer (info);
		}
	}

	/* the freed thread ones */
	while (freed_thread_remsets) {
		RememberedSet *next;
		remset = freed_thread_remsets;
		DEBUG (4, fprintf (gc_debug_file, "Scanning remset for freed thread, range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;)
			p = handle_remset (p, start_nursery, end_nursery, FALSE, queue);
		next = remset->next;
		DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
		mono_sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
		freed_thread_remsets = next;
	}
}

/*
 * Clear the info in the remembered sets: we're doing a major collection, so
 * the per-thread ones are not needed and the global ones will be reconstructed
 * during the copy.
 */
static void
clear_remsets (void)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset, *next;

	/* the global list */
	for (remset = global_remset; remset; remset = next) {
		remset->store_next = remset->data;
		next = remset->next;
		remset->next = NULL;
		if (remset != global_remset) {
			DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
			mono_sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
		}
	}
	/* the generic store ones */
	while (generic_store_remsets) {
		GenericStoreRememberedSet *gs_next = generic_store_remsets->next;
		mono_sgen_free_internal (generic_store_remsets, INTERNAL_MEM_STORE_REMSET);
		generic_store_remsets = gs_next;
	}
	/* the per-thread ones */
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			for (remset = info->remset; remset; remset = next) {
				remset->store_next = remset->data;
				next = remset->next;
				remset->next = NULL;
				if (remset != info->remset) {
					DEBUG (3, fprintf (gc_debug_file, "Freed remset at %p\n", remset->data));
					mono_sgen_free_internal_dynamic (remset, remset_byte_size (remset), INTERNAL_MEM_REMSET);
				}
			}
			clear_thread_store_remset_buffer (info);
		}
	}

	/* the freed thread ones */
	while (freed_thread_remsets) {
		next = freed_thread_remsets->next;
		DEBUG (4, fprintf (gc_debug_file, "Freed remset at %p\n", freed_thread_remsets->data));
		mono_sgen_free_internal_dynamic (freed_thread_remsets, remset_byte_size (freed_thread_remsets), INTERNAL_MEM_REMSET);
		freed_thread_remsets = next;
	}
}

/*
 * Clear the thread local TLAB variables for all threads.
 */
static void
clear_tlabs (void)
{
	SgenThreadInfo *info;
	int i;

	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			/* A new TLAB will be allocated when the thread does its first allocation */
			*info->tlab_start_addr = NULL;
			*info->tlab_next_addr = NULL;
			*info->tlab_temp_end_addr = NULL;
			*info->tlab_real_end_addr = NULL;
		}
	}
}

/* LOCKING: assumes the GC lock is held */
static SgenThreadInfo*
gc_register_current_thread (void *addr)
{
	int hash;
	SgenThreadInfo* info = malloc (sizeof (SgenThreadInfo));
#ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = info;
#endif

	if (!info)
		return NULL;

	memset (info, 0, sizeof (SgenThreadInfo));
#ifndef HAVE_KW_THREAD
	info->tlab_start = info->tlab_next = info->tlab_temp_end = info->tlab_real_end = NULL;

	g_assert (!pthread_getspecific (thread_info_key));
	pthread_setspecific (thread_info_key, info);
#else
	thread_info = info;
#endif

	info->id = ARCH_GET_THREAD ();
	info->stop_count = -1;
	info->skip = 0;
	info->signal = 0;
	info->stack_start = NULL;
	info->tlab_start_addr = &TLAB_START;
	info->tlab_next_addr = &TLAB_NEXT;
	info->tlab_temp_end_addr = &TLAB_TEMP_END;
	info->tlab_real_end_addr = &TLAB_REAL_END;
	info->store_remset_buffer_addr = &STORE_REMSET_BUFFER;
	info->store_remset_buffer_index_addr = &STORE_REMSET_BUFFER_INDEX;
	info->stopped_ip = NULL;
	info->stopped_domain = NULL;
	info->stopped_regs = NULL;

	binary_protocol_thread_register ((gpointer)info->id);

#ifdef HAVE_KW_THREAD
	tlab_next_addr = &tlab_next;
	store_remset_buffer_index_addr = &store_remset_buffer_index;
#endif

	/* try to get it with attributes first */
#if defined(HAVE_PTHREAD_GETATTR_NP) && defined(HAVE_PTHREAD_ATTR_GETSTACK)
	{
		size_t size;
		void *sstart;
		pthread_attr_t attr;
		pthread_getattr_np (pthread_self (), &attr);
		pthread_attr_getstack (&attr, &sstart, &size);
		info->stack_start_limit = sstart;
		info->stack_end = (char*)sstart + size;
		pthread_attr_destroy (&attr);
	}
#elif defined(HAVE_PTHREAD_GET_STACKSIZE_NP) && defined(HAVE_PTHREAD_GET_STACKADDR_NP)
		 info->stack_end = (char*)pthread_get_stackaddr_np (pthread_self ());
		 info->stack_start_limit = (char*)info->stack_end - pthread_get_stacksize_np (pthread_self ());
#else
	{
		/* FIXME: we assume the stack grows down */
		gsize stack_bottom = (gsize)addr;
		stack_bottom += 4095;
		stack_bottom &= ~4095;
		info->stack_end = (char*)stack_bottom;
	}
#endif

#ifdef HAVE_KW_THREAD
	stack_end = info->stack_end;
#endif

	/* hash into the table */
	hash = HASH_PTHREAD_T (info->id) % THREAD_HASH_SIZE;
	info->next = thread_table [hash];
	thread_table [hash] = info;

	info->remset = alloc_remset (DEFAULT_REMSET_SIZE, info);
	pthread_setspecific (remembered_set_key, info->remset);
#ifdef HAVE_KW_THREAD
	remembered_set = info->remset;
#endif

	STORE_REMSET_BUFFER = mono_sgen_alloc_internal (INTERNAL_MEM_STORE_REMSET);
	STORE_REMSET_BUFFER_INDEX = 0;

	DEBUG (3, fprintf (gc_debug_file, "registered thread %p (%p) (hash: %d)\n", info, (gpointer)info->id, hash));

	if (gc_callbacks.thread_attach_func)
		info->runtime_data = gc_callbacks.thread_attach_func ();

	return info;
}

static void
add_generic_store_remset_from_buffer (gpointer *buffer)
{
	GenericStoreRememberedSet *remset = mono_sgen_alloc_internal (INTERNAL_MEM_STORE_REMSET);
	memcpy (remset->data, buffer + 1, sizeof (gpointer) * (STORE_REMSET_BUFFER_SIZE - 1));
	remset->next = generic_store_remsets;
	generic_store_remsets = remset;
}

static void
unregister_current_thread (void)
{
	int hash;
	SgenThreadInfo *prev = NULL;
	SgenThreadInfo *p;
	RememberedSet *rset;
	ARCH_THREAD_TYPE id = ARCH_GET_THREAD ();

	binary_protocol_thread_unregister ((gpointer)id);

	hash = HASH_PTHREAD_T (id) % THREAD_HASH_SIZE;
	p = thread_table [hash];
	assert (p);
	DEBUG (3, fprintf (gc_debug_file, "unregister thread %p (%p)\n", p, (gpointer)p->id));
	while (!ARCH_THREAD_EQUALS (p->id, id)) {
		prev = p;
		p = p->next;
	}
	if (prev == NULL) {
		thread_table [hash] = p->next;
	} else {
		prev->next = p->next;
	}
	if (p->remset) {
		if (freed_thread_remsets) {
			for (rset = p->remset; rset->next; rset = rset->next)
				;
			rset->next = freed_thread_remsets;
			freed_thread_remsets = p->remset;
		} else {
			freed_thread_remsets = p->remset;
		}
	}
	if (*p->store_remset_buffer_index_addr)
		add_generic_store_remset_from_buffer (*p->store_remset_buffer_addr);
	mono_sgen_free_internal (*p->store_remset_buffer_addr, INTERNAL_MEM_STORE_REMSET);
	free (p);
}

static void
unregister_thread (void *k)
{
	g_assert (!mono_domain_get ());
	LOCK_GC;
	unregister_current_thread ();
	UNLOCK_GC;
}

gboolean
mono_gc_register_thread (void *baseptr)
{
	SgenThreadInfo *info;

	LOCK_GC;
	init_stats ();
	info = mono_sgen_thread_info_lookup (ARCH_GET_THREAD ());
	if (info == NULL)
		info = gc_register_current_thread (baseptr);
	UNLOCK_GC;
	return info != NULL;
}

#if USE_PTHREAD_INTERCEPT

typedef struct {
	void *(*start_routine) (void *);
	void *arg;
	int flags;
	MonoSemType registered;
} SgenThreadStartInfo;

static void*
gc_start_thread (void *arg)
{
	SgenThreadStartInfo *start_info = arg;
	SgenThreadInfo* info;
	void *t_arg = start_info->arg;
	void *(*start_func) (void*) = start_info->start_routine;
	void *result;
	int post_result;

	LOCK_GC;
	info = gc_register_current_thread (&result);
	UNLOCK_GC;
	post_result = MONO_SEM_POST (&(start_info->registered));
	g_assert (!post_result);
	result = start_func (t_arg);
	g_assert (!mono_domain_get ());
	/*
	 * this is done by the pthread key dtor
	LOCK_GC;
	unregister_current_thread ();
	UNLOCK_GC;
	*/

	return result;
}

int
mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg)
{
	SgenThreadStartInfo *start_info;
	int result;

	start_info = malloc (sizeof (SgenThreadStartInfo));
	if (!start_info)
		return ENOMEM;
	MONO_SEM_INIT (&(start_info->registered), 0);
	start_info->arg = arg;
	start_info->start_routine = start_routine;

	result = pthread_create (new_thread, attr, gc_start_thread, start_info);
	if (result == 0) {
		while (MONO_SEM_WAIT (&(start_info->registered)) != 0) {
			/*if (EINTR != errno) ABORT("sem_wait failed"); */
		}
	}
	MONO_SEM_DESTROY (&(start_info->registered));
	free (start_info);
	return result;
}

int
mono_gc_pthread_join (pthread_t thread, void **retval)
{
	return pthread_join (thread, retval);
}

int
mono_gc_pthread_detach (pthread_t thread)
{
	return pthread_detach (thread);
}

#endif /* USE_PTHREAD_INTERCEPT */

/*
 * ######################################################################
 * ########  Write barriers
 * ######################################################################
 */

/*
 * This causes the compile to extend the liveness of 'v' till the call to dummy_use
 */
static void
dummy_use (gpointer v) {
	__asm__ volatile ("" : "=r"(v) : "r"(v));
}


static RememberedSet*
alloc_remset (int size, gpointer id) {
	RememberedSet* res = mono_sgen_alloc_internal_dynamic (sizeof (RememberedSet) + (size * sizeof (gpointer)), INTERNAL_MEM_REMSET);
	res->store_next = res->data;
	res->end_set = res->data + size;
	res->next = NULL;
	DEBUG (4, fprintf (gc_debug_file, "Allocated remset size %d at %p for %p\n", size, res->data, id));
	return res;
}

/*
 * Note: the write barriers first do the needed GC work and then do the actual store:
 * this way the value is visible to the conservative GC scan after the write barrier
 * itself. If a GC interrupts the barrier in the middle, value will be kept alive by
 * the conservative scan, otherwise by the remembered set scan.
 */
void
mono_gc_wbarrier_set_field (MonoObject *obj, gpointer field_ptr, MonoObject* value)
{
	HEAVY_STAT (++stat_wbarrier_set_field);
	if (ptr_in_nursery (field_ptr)) {
		*(void**)field_ptr = value;
		return;
	}
	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p\n", field_ptr));
	if (use_cardtable) {
		*(void**)field_ptr = value;
		if (ptr_in_nursery (value))
			sgen_card_table_mark_address ((mword)field_ptr);
		dummy_use (value);
	} else {
		RememberedSet *rs;
		TLAB_ACCESS_INIT;

		LOCK_GC;
		rs = REMEMBERED_SET;
		if (rs->store_next < rs->end_set) {
			*(rs->store_next++) = (mword)field_ptr;
			*(void**)field_ptr = value;
			UNLOCK_GC;
			return;
		}
		rs = alloc_remset (rs->end_set - rs->data, (void*)1);
		rs->next = REMEMBERED_SET;
		REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
		mono_sgen_thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
#endif
		*(rs->store_next++) = (mword)field_ptr;
		*(void**)field_ptr = value;
		UNLOCK_GC;
	}
}

void
mono_gc_wbarrier_set_arrayref (MonoArray *arr, gpointer slot_ptr, MonoObject* value)
{
	HEAVY_STAT (++stat_wbarrier_set_arrayref);
	if (ptr_in_nursery (slot_ptr)) {
		*(void**)slot_ptr = value;
		return;
	}
	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p\n", slot_ptr));
	if (use_cardtable) {
		*(void**)slot_ptr = value;
		if (ptr_in_nursery (value))
			sgen_card_table_mark_address ((mword)slot_ptr);
		dummy_use (value);
	} else {
		RememberedSet *rs;
		TLAB_ACCESS_INIT;

		LOCK_GC;
		rs = REMEMBERED_SET;
		if (rs->store_next < rs->end_set) {
			*(rs->store_next++) = (mword)slot_ptr;
			*(void**)slot_ptr = value;
			UNLOCK_GC;
			return;
		}
		rs = alloc_remset (rs->end_set - rs->data, (void*)1);
		rs->next = REMEMBERED_SET;
		REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
		mono_sgen_thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
#endif
		*(rs->store_next++) = (mword)slot_ptr;
		*(void**)slot_ptr = value;
		UNLOCK_GC;
	}
}

void
mono_gc_wbarrier_arrayref_copy (gpointer dest_ptr, gpointer src_ptr, int count)
{
	HEAVY_STAT (++stat_wbarrier_arrayref_copy);
	/*This check can be done without taking a lock since dest_ptr array is pinned*/
	if (ptr_in_nursery (dest_ptr) || count <= 0) {
		memmove (dest_ptr, src_ptr, count * sizeof (gpointer));
		return;
	}

	if (use_cardtable) {
		gpointer *dest = dest_ptr;
		gpointer *src = src_ptr;

		/*overlapping that required backward copying*/
		if (src < dest && (src + count) > dest) {
			gpointer *start = dest;
			dest += count - 1;
			src += count - 1;

			for (; dest >= start; --src, --dest) {
				gpointer value = *src;
				*dest = value;
				if (ptr_in_nursery (value))
					sgen_card_table_mark_address ((mword)dest);
				dummy_use (value);
			}
		} else {
			gpointer *end = dest + count;
			for (; dest < end; ++src, ++dest) {
				gpointer value = *src;
				*dest = value;
				if (ptr_in_nursery (value))
					sgen_card_table_mark_address ((mword)dest);
				dummy_use (value);
			}
		}
	} else {
		RememberedSet *rs;
		TLAB_ACCESS_INIT;
		LOCK_GC;
		memmove (dest_ptr, src_ptr, count * sizeof (gpointer));

		rs = REMEMBERED_SET;
		DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p, %d\n", dest_ptr, count));
		if (rs->store_next + 1 < rs->end_set) {
			*(rs->store_next++) = (mword)dest_ptr | REMSET_RANGE;
			*(rs->store_next++) = count;
			UNLOCK_GC;
			return;
		}
		rs = alloc_remset (rs->end_set - rs->data, (void*)1);
		rs->next = REMEMBERED_SET;
		REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
		mono_sgen_thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
#endif
		*(rs->store_next++) = (mword)dest_ptr | REMSET_RANGE;
		*(rs->store_next++) = count;

		UNLOCK_GC;
	}
}

static char *found_obj;

static void
find_object_for_ptr_callback (char *obj, size_t size, char *ptr)
{
	if (ptr >= obj && ptr < obj + size) {
		g_assert (!found_obj);
		found_obj = obj;
	}
}

/* for use in the debugger */
char* find_object_for_ptr (char *ptr);
char*
find_object_for_ptr (char *ptr)
{
	LOSObject *bigobj;

	if (ptr >= nursery_section->data && ptr < nursery_section->end_data) {
		found_obj = NULL;
		mono_sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
				(IterateObjectCallbackFunc)find_object_for_ptr_callback, ptr);
		if (found_obj)
			return found_obj;
	}

	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next) {
		if (ptr >= bigobj->data && ptr < bigobj->data + bigobj->size)
			return bigobj->data;
	}

	/*
	 * Very inefficient, but this is debugging code, supposed to
	 * be called from gdb, so we don't care.
	 */
	found_obj = NULL;
	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)find_object_for_ptr_callback, ptr);
	return found_obj;
}

static void
evacuate_remset_buffer (void)
{
	gpointer *buffer;
	TLAB_ACCESS_INIT;

	buffer = STORE_REMSET_BUFFER;

	add_generic_store_remset_from_buffer (buffer);
	memset (buffer, 0, sizeof (gpointer) * STORE_REMSET_BUFFER_SIZE);

	STORE_REMSET_BUFFER_INDEX = 0;
}

void
mono_gc_wbarrier_generic_nostore (gpointer ptr)
{
	gpointer *buffer;
	int index;
	TLAB_ACCESS_INIT;

	HEAVY_STAT (++stat_wbarrier_generic_store);

#ifdef XDOMAIN_CHECKS_IN_WBARRIER
	/* FIXME: ptr_in_heap must be called with the GC lock held */
	if (xdomain_checks && *(MonoObject**)ptr && ptr_in_heap (ptr)) {
		char *start = find_object_for_ptr (ptr);
		MonoObject *value = *(MonoObject**)ptr;
		LOCK_GC;
		g_assert (start);
		if (start) {
			MonoObject *obj = (MonoObject*)start;
			if (obj->vtable->domain != value->vtable->domain)
				g_assert (is_xdomain_ref_allowed (ptr, start, obj->vtable->domain));
		}
		UNLOCK_GC;
	}
#endif

	if (*(gpointer*)ptr)
		binary_protocol_wbarrier (ptr, *(gpointer*)ptr, (gpointer)LOAD_VTABLE (*(gpointer*)ptr));

	if (ptr_in_nursery (ptr) || ptr_on_stack (ptr) || !ptr_in_nursery (*(gpointer*)ptr)) {
		DEBUG (8, fprintf (gc_debug_file, "Skipping remset at %p\n", ptr));
		return;
	}

	if (use_cardtable) {
		if (ptr_in_nursery(*(gpointer*)ptr))
			sgen_card_table_mark_address ((mword)ptr);
		return;
	}

	LOCK_GC;

	buffer = STORE_REMSET_BUFFER;
	index = STORE_REMSET_BUFFER_INDEX;
	/* This simple optimization eliminates a sizable portion of
	   entries.  Comparing it to the last but one entry as well
	   doesn't eliminate significantly more entries. */
	if (buffer [index] == ptr) {
		UNLOCK_GC;
		return;
	}

	DEBUG (8, fprintf (gc_debug_file, "Adding remset at %p\n", ptr));
	HEAVY_STAT (++stat_wbarrier_generic_store_remset);

	++index;
	if (index >= STORE_REMSET_BUFFER_SIZE) {
		evacuate_remset_buffer ();
		index = STORE_REMSET_BUFFER_INDEX;
		g_assert (index == 0);
		++index;
	}
	buffer [index] = ptr;
	STORE_REMSET_BUFFER_INDEX = index;

	UNLOCK_GC;
}

void
mono_gc_wbarrier_generic_store (gpointer ptr, MonoObject* value)
{
	DEBUG (8, fprintf (gc_debug_file, "Wbarrier store at %p to %p (%s)\n", ptr, value, value ? safe_name (value) : "null"));
	*(void**)ptr = value;
	if (ptr_in_nursery (value))
		mono_gc_wbarrier_generic_nostore (ptr);
	dummy_use (value);
}

void mono_gc_wbarrier_value_copy_bitmap (gpointer _dest, gpointer _src, int size, unsigned bitmap)
{
	mword *dest = _dest;
	mword *src = _src;

	while (size) {
		if (bitmap & 0x1)
			mono_gc_wbarrier_generic_store (dest, (MonoObject*)*src);
		else
			*dest = *src;
		++src;
		++dest;
		size -= SIZEOF_VOID_P;
		bitmap >>= 1;
	}
}


void
mono_gc_wbarrier_value_copy (gpointer dest, gpointer src, int count, MonoClass *klass)
{
	RememberedSet *rs;
	size_t size = count * mono_class_value_size (klass, NULL);
	TLAB_ACCESS_INIT;
	HEAVY_STAT (++stat_wbarrier_value_copy);
	g_assert (klass->valuetype);
	LOCK_GC;
	memmove (dest, src, size);
	if (use_cardtable) {
		sgen_card_table_mark_range ((mword)dest, size);
	} else {
		rs = REMEMBERED_SET;
		if (ptr_in_nursery (dest) || ptr_on_stack (dest) || !klass->has_references) {
			UNLOCK_GC;
			return;
		}
		g_assert (klass->gc_descr_inited);
		DEBUG (8, fprintf (gc_debug_file, "Adding value remset at %p, count %d, descr %p for class %s (%p)\n", dest, count, klass->gc_descr, klass->name, klass));

		if (rs->store_next + 3 < rs->end_set) {
			*(rs->store_next++) = (mword)dest | REMSET_VTYPE;
			*(rs->store_next++) = (mword)klass->gc_descr;
			*(rs->store_next++) = (mword)count;
			UNLOCK_GC;
			return;
		}
		rs = alloc_remset (rs->end_set - rs->data, (void*)1);
		rs->next = REMEMBERED_SET;
		REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
		mono_sgen_thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
#endif
		*(rs->store_next++) = (mword)dest | REMSET_VTYPE;
		*(rs->store_next++) = (mword)klass->gc_descr;
		*(rs->store_next++) = (mword)count;
	}
	UNLOCK_GC;
}

/**
 * mono_gc_wbarrier_object_copy:
 *
 * Write barrier to call when obj is the result of a clone or copy of an object.
 */
void
mono_gc_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	RememberedSet *rs;
	int size;

	TLAB_ACCESS_INIT;
	HEAVY_STAT (++stat_wbarrier_object_copy);
	rs = REMEMBERED_SET;
	DEBUG (6, fprintf (gc_debug_file, "Adding object remset for %p\n", obj));
	size = mono_object_class (obj)->instance_size;
	LOCK_GC;
	/* do not copy the sync state */
	memcpy ((char*)obj + sizeof (MonoObject), (char*)src + sizeof (MonoObject),
			size - sizeof (MonoObject));
	if (ptr_in_nursery (obj) || ptr_on_stack (obj)) {
		UNLOCK_GC;
		return;
	}
	if (rs->store_next < rs->end_set) {
		*(rs->store_next++) = (mword)obj | REMSET_OBJECT;
		UNLOCK_GC;
		return;
	}
	rs = alloc_remset (rs->end_set - rs->data, (void*)1);
	rs->next = REMEMBERED_SET;
	REMEMBERED_SET = rs;
#ifdef HAVE_KW_THREAD
	mono_sgen_thread_info_lookup (ARCH_GET_THREAD ())->remset = rs;
#endif
	*(rs->store_next++) = (mword)obj | REMSET_OBJECT;
	UNLOCK_GC;
}

/*
 * ######################################################################
 * ########  Collector debugging
 * ######################################################################
 */

const char*descriptor_types [] = {
	"run_length",
	"small_bitmap",
	"string",
	"complex",
	"vector",
	"array",
	"large_bitmap",
	"complex_arr"
};

void
describe_ptr (char *ptr)
{
	MonoVTable *vtable;
	mword desc;
	int type;

	if (ptr_in_nursery (ptr)) {
		printf ("Pointer inside nursery.\n");
	} else {
		if (major_collector.ptr_is_in_non_pinned_space (ptr)) {
			printf ("Pointer inside oldspace.\n");
		} else if (major_collector.obj_is_from_pinned_alloc (ptr)) {
			printf ("Pointer is inside a pinned chunk.\n");
		} else {
			printf ("Pointer unknown.\n");
			return;
		}
	}

	if (object_is_pinned (ptr))
		printf ("Object is pinned.\n");

	if (object_is_forwarded (ptr))
		printf ("Object is forwared.\n");

	// FIXME: Handle pointers to the inside of objects
	vtable = (MonoVTable*)LOAD_VTABLE (ptr);

	printf ("VTable: %p\n", vtable);
	if (vtable == NULL) {
		printf ("VTable is invalid (empty).\n");
		return;
	}
	if (ptr_in_nursery (vtable)) {
		printf ("VTable is invalid (points inside nursery).\n");
		return;
	}
	printf ("Class: %s\n", vtable->klass->name);

	desc = ((GCVTable*)vtable)->desc;
	printf ("Descriptor: %lx\n", (long)desc);

	type = desc & 0x7;
	printf ("Descriptor type: %d (%s)\n", type, descriptor_types [type]);
}

static mword*
find_in_remset_loc (mword *p, char *addr, gboolean *found)
{
	void **ptr;
	mword count, desc;
	size_t skip_size;

	switch ((*p) & REMSET_TYPE_MASK) {
	case REMSET_LOCATION:
		if (*p == (mword)addr)
			*found = TRUE;
		return p + 1;
	case REMSET_RANGE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		count = p [1];
		if ((void**)addr >= ptr && (void**)addr < ptr + count)
			*found = TRUE;
		return p + 2;
	case REMSET_OBJECT:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		count = safe_object_get_size ((MonoObject*)ptr); 
		count = ALIGN_UP (count);
		count /= sizeof (mword);
		if ((void**)addr >= ptr && (void**)addr < ptr + count)
			*found = TRUE;
		return p + 1;
	case REMSET_VTYPE:
		ptr = (void**)(*p & ~REMSET_TYPE_MASK);
		desc = p [1];
		count = p [2];

		switch (desc & 0x7) {
		case DESC_TYPE_RUN_LENGTH:
			OBJ_RUN_LEN_SIZE (skip_size, desc, ptr);
			break;
		case DESC_TYPE_SMALL_BITMAP:
			OBJ_BITMAP_SIZE (skip_size, desc, start);
			break;
		default:
			// FIXME:
			g_assert_not_reached ();
		}

		/* The descriptor includes the size of MonoObject */
		skip_size -= sizeof (MonoObject);
		skip_size *= count;
		if ((void**)addr >= ptr && (void**)addr < ptr + (skip_size / sizeof (gpointer)))
			*found = TRUE;

		return p + 3;
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

/*
 * Return whenever ADDR occurs in the remembered sets
 */
static gboolean
find_in_remsets (char *addr)
{
	int i;
	SgenThreadInfo *info;
	RememberedSet *remset;
	GenericStoreRememberedSet *store_remset;
	mword *p;
	gboolean found = FALSE;

	/* the global one */
	for (remset = global_remset; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning global remset range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;) {
			p = find_in_remset_loc (p, addr, &found);
			if (found)
				return TRUE;
		}
	}

	/* the generic store ones */
	for (store_remset = generic_store_remsets; store_remset; store_remset = store_remset->next) {
		for (i = 0; i < STORE_REMSET_BUFFER_SIZE - 1; ++i) {
			if (store_remset->data [i] == addr)
				return TRUE;
		}
	}

	/* the per-thread ones */
	for (i = 0; i < THREAD_HASH_SIZE; ++i) {
		for (info = thread_table [i]; info; info = info->next) {
			int j;
			for (remset = info->remset; remset; remset = remset->next) {
				DEBUG (4, fprintf (gc_debug_file, "Scanning remset for thread %p, range: %p-%p, size: %td\n", info, remset->data, remset->store_next, remset->store_next - remset->data));
				for (p = remset->data; p < remset->store_next;) {
					p = find_in_remset_loc (p, addr, &found);
					if (found)
						return TRUE;
				}
			}
			for (j = 0; j < *info->store_remset_buffer_index_addr; ++j) {
				if ((*info->store_remset_buffer_addr) [j + 1] == addr)
					return TRUE;
			}
		}
	}

	/* the freed thread ones */
	for (remset = freed_thread_remsets; remset; remset = remset->next) {
		DEBUG (4, fprintf (gc_debug_file, "Scanning remset for freed thread, range: %p-%p, size: %td\n", remset->data, remset->store_next, remset->store_next - remset->data));
		for (p = remset->data; p < remset->store_next;) {
			p = find_in_remset_loc (p, addr, &found);
			if (found)
				return TRUE;
		}
	}

	return FALSE;
}

static gboolean missing_remsets;

/*
 * We let a missing remset slide if the target object is pinned,
 * because the store might have happened but the remset not yet added,
 * but in that case the target must be pinned.  We might theoretically
 * miss some missing remsets this way, but it's very unlikely.
 */
#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(ptr) && (char*)*(ptr) >= nursery_start && (char*)*(ptr) < nursery_next) {	\
		if (!find_in_remsets ((char*)(ptr)) && (!use_cardtable || !sgen_card_table_address_is_marked ((mword)ptr))) { \
                fprintf (gc_debug_file, "Oldspace->newspace reference %p at offset %td in object %p (%s.%s) not found in remsets.\n", *(ptr), (char*)(ptr) - (char*)(obj), (obj), ((MonoObject*)(obj))->vtable->klass->name_space, ((MonoObject*)(obj))->vtable->klass->name); \
		binary_protocol_missing_remset ((obj), (gpointer)LOAD_VTABLE ((obj)), (char*)(ptr) - (char*)(obj), *(ptr), (gpointer)LOAD_VTABLE(*(ptr)), object_is_pinned (*(ptr))); \
		if (!object_is_pinned (*(ptr)))				\
			missing_remsets = TRUE;				\
            } \
        } \
	} while (0)

/*
 * Check that each object reference which points into the nursery can
 * be found in the remembered sets.
 */
static void
check_consistency_callback (char *start, size_t size, void *dummy)
{
	GCVTable *vt = (GCVTable*)LOAD_VTABLE (start);
	DEBUG (8, fprintf (gc_debug_file, "Scanning object %p, vtable: %p (%s)\n", start, vt, vt->klass->name));

#define SCAN_OBJECT_ACTION
#include "sgen-scan-object.h"
}

/*
 * Perform consistency check of the heap.
 *
 * Assumes the world is stopped.
 */
static void
check_consistency (void)
{
	LOSObject *bigobj;

	// Need to add more checks

	missing_remsets = FALSE;

	DEBUG (1, fprintf (gc_debug_file, "Begin heap consistency check...\n"));

	// Check that oldspace->newspace pointers are registered with the collector
	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)check_consistency_callback, NULL);

	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		check_consistency_callback (bigobj->data, bigobj->size, NULL);

	DEBUG (1, fprintf (gc_debug_file, "Heap consistency check done.\n"));

#ifdef SGEN_BINARY_PROTOCOL
	if (!binary_protocol_file)
#endif
		g_assert (!missing_remsets);
}


#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		if (*(ptr) && !LOAD_VTABLE (*(ptr)))						\
			g_error ("Could not load vtable for obj %p slot %d (size %d)", obj, (char*)ptr - (char*)obj, safe_object_get_size ((MonoObject*)obj));		\
	} while (0)

static void
check_major_refs_callback (char *start, size_t size, void *dummy)
{
#define SCAN_OBJECT_ACTION
#include "sgen-scan-object.h"
}

static void
check_major_refs (void)
{
	LOSObject *bigobj;

	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)check_major_refs_callback, NULL);

	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		check_major_refs_callback (bigobj->data, bigobj->size, NULL);
}

/* Check that the reference is valid */
#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(ptr)) {	\
			g_assert (safe_name (*(ptr)) != NULL);	\
		}	\
	} while (0)

/*
 * check_object:
 *
 *   Perform consistency check on an object. Currently we only check that the
 * reference fields are valid.
 */
void
check_object (char *start)
{
	if (!start)
		return;

#include "sgen-scan-object.h"
}

/*
 * ######################################################################
 * ########  Other mono public interface functions.
 * ######################################################################
 */

void
mono_gc_collect (int generation)
{
	LOCK_GC;
	if (generation > 1)
		generation = 1;
	mono_profiler_gc_event (MONO_GC_EVENT_START, generation);
	stop_world (generation);
	if (generation == 0) {
		collect_nursery (0);
	} else {
		major_collection ("user request");
	}
	restart_world (generation);
	mono_profiler_gc_event (MONO_GC_EVENT_END, generation);
	UNLOCK_GC;
}

int
mono_gc_max_generation (void)
{
	return 1;
}

int
mono_gc_collection_count (int generation)
{
	if (generation == 0)
		return num_minor_gcs;
	return num_major_gcs;
}

int64_t
mono_gc_get_used_size (void)
{
	gint64 tot = 0;
	LOCK_GC;
	tot = los_memory_usage;
	tot += nursery_section->next_data - nursery_section->data;
	tot += major_collector.get_used_size ();
	/* FIXME: account for pinned objects */
	UNLOCK_GC;
	return tot;
}

int64_t
mono_gc_get_heap_size (void)
{
	return total_alloc;
}

void
mono_gc_disable (void)
{
	LOCK_GC;
	gc_disabled++;
	UNLOCK_GC;
}

void
mono_gc_enable (void)
{
	LOCK_GC;
	gc_disabled--;
	UNLOCK_GC;
}

int
mono_gc_get_los_limit (void)
{
	return MAX_SMALL_OBJ_SIZE;
}

gboolean
mono_object_is_alive (MonoObject* o)
{
	return TRUE;
}

int
mono_gc_get_generation (MonoObject *obj)
{
	if (ptr_in_nursery (obj))
		return 0;
	return 1;
}

void
mono_gc_enable_events (void)
{
}

void
mono_gc_weak_link_add (void **link_addr, MonoObject *obj, gboolean track)
{
	LOCK_GC;
	mono_gc_register_disappearing_link (obj, link_addr, track);
	UNLOCK_GC;
}

void
mono_gc_weak_link_remove (void **link_addr)
{
	LOCK_GC;
	mono_gc_register_disappearing_link (NULL, link_addr, FALSE);
	UNLOCK_GC;
}

MonoObject*
mono_gc_weak_link_get (void **link_addr)
{
	if (!*link_addr)
		return NULL;
	return (MonoObject*) REVEAL_POINTER (*link_addr);
}

gboolean
mono_gc_ephemeron_array_add (MonoObject *obj)
{
	EphemeronLinkNode *node;

	LOCK_GC;

	node = mono_sgen_alloc_internal (INTERNAL_MEM_EPHEMERON_LINK);
	if (!node) {
		UNLOCK_GC;
		return FALSE;
	}
	node->array = (char*)obj;
	node->next = ephemeron_list;
	ephemeron_list = node;

	DEBUG (5, fprintf (gc_debug_file, "Registered ephemeron array %p\n", obj));

	UNLOCK_GC;
	return TRUE;
}

void*
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	if (numbits == 0) {
		return (void*)MAKE_ROOT_DESC (ROOT_DESC_BITMAP, 0);
	} else if (numbits < ((sizeof (*bitmap) * 8) - ROOT_DESC_TYPE_SHIFT)) {
		return (void*)MAKE_ROOT_DESC (ROOT_DESC_BITMAP, bitmap [0]);
	} else {
		mword complex = alloc_complex_descriptor (bitmap, numbits);
		return (void*)MAKE_ROOT_DESC (ROOT_DESC_COMPLEX, complex);
	}
}

 static void *all_ref_root_descrs [32];

void*
mono_gc_make_root_descr_all_refs (int numbits)
{
	gsize *gc_bitmap;
	void *descr;

	if (numbits < 32 && all_ref_root_descrs [numbits])
		return all_ref_root_descrs [numbits];

	gc_bitmap = g_malloc0 (ALIGN_TO (numbits, 8) + 1);
	memset (gc_bitmap, 0xff, numbits / 8);
	if (numbits % 8)
		gc_bitmap [numbits / 8] = (1 << (numbits % 8)) - 1;
	descr = mono_gc_make_descr_from_bitmap (gc_bitmap, numbits);
	g_free (gc_bitmap);

	if (numbits < 32)
		all_ref_root_descrs [numbits] = descr;

	return descr;
}

void*
mono_gc_make_root_descr_user (MonoGCRootMarkFunc marker)
{
	void *descr;

	g_assert (user_descriptors_next < MAX_USER_DESCRIPTORS);
	descr = (void*)MAKE_ROOT_DESC (ROOT_DESC_USER, (mword)user_descriptors_next);
	user_descriptors [user_descriptors_next ++] = marker;

	return descr;
}

void*
mono_gc_alloc_fixed (size_t size, void *descr)
{
	/* FIXME: do a single allocation */
	void *res = calloc (1, size);
	if (!res)
		return NULL;
	if (!mono_gc_register_root (res, size, descr)) {
		free (res);
		res = NULL;
	}
	return res;
}

void
mono_gc_free_fixed (void* addr)
{
	mono_gc_deregister_root (addr);
	free (addr);
}

void*
mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data)
{
	void *result;
	LOCK_INTERRUPTION;
	result = func (data);
	UNLOCK_INTERRUPTION;
	return result;
}

gboolean
mono_gc_is_gc_thread (void)
{
	gboolean result;
	LOCK_GC;
        result = mono_sgen_thread_info_lookup (ARCH_GET_THREAD ()) != NULL;
	UNLOCK_GC;
	return result;
}

/* Tries to extract a number from the passed string, taking in to account m, k
 * and g suffixes */
gboolean
mono_sgen_parse_environment_string_extract_number (const char *str, glong *out)
{
	char *endptr;
	int len = strlen (str), shift = 0;
	glong val;
	gboolean is_suffix = FALSE;
	char suffix;

	switch (str [len - 1]) {
		case 'g':
		case 'G':
			shift += 10;
		case 'm':
		case 'M':
			shift += 10;
		case 'k':
		case 'K':
			shift += 10;
			is_suffix = TRUE;
			suffix = str [len - 1];
			break;
	}

	errno = 0;
	val = strtol (str, &endptr, 10);

	if ((errno == ERANGE && (val == LONG_MAX || val == LONG_MIN))
			|| (errno != 0 && val == 0) || (endptr == str))
		return FALSE;

	if (is_suffix) {
		if (*(endptr + 1)) /* Invalid string. */
			return FALSE;
		val <<= shift;
	}

	*out = val;
	return TRUE;
}

void
mono_gc_base_init (void)
{
	char *env;
	char **opts, **ptr;
	char *major_collector_opt = NULL;
	struct sigaction sinfo;

	/* the gc_initialized guard seems to imply this method is
	   idempotent, but LOCK_INIT(gc_mutex) might not be.  It's
	   defined in sgen-gc.h as nothing, so there's no danger at
	   present. */
	LOCK_INIT (gc_mutex);
	LOCK_GC;
	if (gc_initialized) {
		UNLOCK_GC;
		return;
	}
	pagesize = mono_pagesize ();
	gc_debug_file = stderr;

	LOCK_INIT (interruption_mutex);
	LOCK_INIT (global_remset_mutex);

	if ((env = getenv ("MONO_GC_PARAMS"))) {
		opts = g_strsplit (env, ",", -1);
		for (ptr = opts; *ptr; ++ptr) {
			char *opt = *ptr;
			if (g_str_has_prefix (opt, "major=")) {
				opt = strchr (opt, '=') + 1;
				major_collector_opt = g_strdup (opt);
			}
		}
	} else {
		opts = NULL;
	}

	init_stats ();
	mono_sgen_init_internal_allocator ();

	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_FRAGMENT, sizeof (Fragment));
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_SECTION, SGEN_SIZEOF_GC_MEM_SECTION);
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_FINALIZE_ENTRY, sizeof (FinalizeEntry));
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_DISLINK, sizeof (DisappearingLink));
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_ROOT_RECORD, sizeof (RootRecord));
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_GRAY_QUEUE, sizeof (GrayQueueSection));
	g_assert (sizeof (GenericStoreRememberedSet) == sizeof (gpointer) * STORE_REMSET_BUFFER_SIZE);
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_STORE_REMSET, sizeof (GenericStoreRememberedSet));
	mono_sgen_register_fixed_internal_mem_type (INTERNAL_MEM_EPHEMERON_LINK, sizeof (EphemeronLinkNode));

	if (!major_collector_opt || !strcmp (major_collector_opt, "marksweep")) {
		mono_sgen_marksweep_init (&major_collector);
	} else if (!major_collector_opt || !strcmp (major_collector_opt, "marksweep-fixed")) {
		mono_sgen_marksweep_fixed_init (&major_collector);
	} else if (!major_collector_opt || !strcmp (major_collector_opt, "marksweep-par")) {
		mono_sgen_marksweep_par_init (&major_collector);
		workers_init (mono_cpu_count ());
	} else if (!major_collector_opt || !strcmp (major_collector_opt, "marksweep-fixed-par")) {
		mono_sgen_marksweep_fixed_par_init (&major_collector);
		workers_init (mono_cpu_count ());
	} else if (!strcmp (major_collector_opt, "copying")) {
		mono_sgen_copying_init (&major_collector);
	} else {
		fprintf (stderr, "Unknown major collector `%s'.\n", major_collector_opt);
		exit (1);
	}

#ifdef SGEN_HAVE_CARDTABLE
	use_cardtable = major_collector.supports_cardtable;
#else
	use_cardtable = FALSE;
#endif

	if (opts) {
		for (ptr = opts; *ptr; ++ptr) {
			char *opt = *ptr;
			if (g_str_has_prefix (opt, "major="))
				continue;
			if (g_str_has_prefix (opt, "wbarrier=")) {
				opt = strchr (opt, '=') + 1;
				if (strcmp (opt, "remset") == 0) {
					use_cardtable = FALSE;
				} else if (strcmp (opt, "cardtable") == 0) {
					if (!use_cardtable) {
						if (major_collector.supports_cardtable)
							fprintf (stderr, "The cardtable write barrier is not supported on this platform.\n");
						else
							fprintf (stderr, "The major collector does not support the cardtable write barrier.\n");
						exit (1);
					}
				}
				continue;
			}
#ifdef USER_CONFIG
			if (g_str_has_prefix (opt, "nursery-size=")) {
				long val;
				opt = strchr (opt, '=') + 1;
				if (*opt && mono_sgen_parse_environment_string_extract_number (opt, &val)) {
					default_nursery_size = val;
#ifdef SGEN_ALIGN_NURSERY
					if ((val & (val - 1))) {
						fprintf (stderr, "The nursery size must be a power of two.\n");
						exit (1);
					}

					default_nursery_bits = 0;
					while (1 << (++ default_nursery_bits) != default_nursery_size)
						;
#endif
				} else {
					fprintf (stderr, "nursery-size must be an integer.\n");
					exit (1);
				}
				continue;
			}
#endif
			if (!(major_collector.handle_gc_param && major_collector.handle_gc_param (opt))) {
				fprintf (stderr, "MONO_GC_PARAMS must be a comma-delimited list of one or more of the following:\n");
				fprintf (stderr, "  nursery-size=N (where N is an integer, possibly with a k, m or a g suffix)\n");
				fprintf (stderr, "  major=COLLECTOR (where COLLECTOR is `marksweep', `marksweep-par' or `copying')\n");
				fprintf (stderr, "  wbarrier=WBARRIER (where WBARRIER is `remset' or `cardtable')\n");
				if (major_collector.print_gc_param_usage)
					major_collector.print_gc_param_usage ();
				exit (1);
			}
		}
		g_strfreev (opts);
	}

	if (major_collector_opt)
		g_free (major_collector_opt);

	nursery_size = DEFAULT_NURSERY_SIZE;
	minor_collection_allowance = MIN_MINOR_COLLECTION_ALLOWANCE;

	alloc_nursery ();

	if ((env = getenv ("MONO_GC_DEBUG"))) {
		opts = g_strsplit (env, ",", -1);
		for (ptr = opts; ptr && *ptr; ptr ++) {
			char *opt = *ptr;
			if (opt [0] >= '0' && opt [0] <= '9') {
				gc_debug_level = atoi (opt);
				opt++;
				if (opt [0] == ':')
					opt++;
				if (opt [0]) {
					char *rf = g_strdup_printf ("%s.%d", opt, getpid ());
					gc_debug_file = fopen (rf, "wb");
					if (!gc_debug_file)
						gc_debug_file = stderr;
					g_free (rf);
				}
			} else if (!strcmp (opt, "collect-before-allocs")) {
				collect_before_allocs = TRUE;
			} else if (!strcmp (opt, "check-at-minor-collections")) {
				consistency_check_at_minor_collection = TRUE;
				nursery_clear_policy = CLEAR_AT_GC;
			} else if (!strcmp (opt, "xdomain-checks")) {
				xdomain_checks = TRUE;
			} else if (!strcmp (opt, "clear-at-gc")) {
				nursery_clear_policy = CLEAR_AT_GC;
			} else if (!strcmp (opt, "conservative-stack-mark")) {
				conservative_stack_mark = TRUE;
			} else if (!strcmp (opt, "check-scan-starts")) {
				do_scan_starts_check = TRUE;
			} else if (g_str_has_prefix (opt, "heap-dump=")) {
				char *filename = strchr (opt, '=') + 1;
				nursery_clear_policy = CLEAR_AT_GC;
				heap_dump_file = fopen (filename, "w");
				if (heap_dump_file)
					fprintf (heap_dump_file, "<sgen-dump>\n");
#ifdef SGEN_BINARY_PROTOCOL
			} else if (g_str_has_prefix (opt, "binary-protocol=")) {
				char *filename = strchr (opt, '=') + 1;
				binary_protocol_file = fopen (filename, "w");
#endif
			} else {
				fprintf (stderr, "Invalid format for the MONO_GC_DEBUG env variable: '%s'\n", env);
				fprintf (stderr, "The format is: MONO_GC_DEBUG=[l[:filename]|<option>]+ where l is a debug level 0-9.\n");
				fprintf (stderr, "Valid options are: collect-before-allocs, check-at-minor-collections, xdomain-checks, clear-at-gc.\n");
				exit (1);
			}
		}
		g_strfreev (opts);
	}

	suspend_ack_semaphore_ptr = &suspend_ack_semaphore;
	MONO_SEM_INIT (&suspend_ack_semaphore, 0);

	sigfillset (&sinfo.sa_mask);
	sinfo.sa_flags = SA_RESTART | SA_SIGINFO;
	sinfo.sa_sigaction = suspend_handler;
	if (sigaction (suspend_signal_num, &sinfo, NULL) != 0) {
		g_error ("failed sigaction");
	}

	sinfo.sa_handler = restart_handler;
	if (sigaction (restart_signal_num, &sinfo, NULL) != 0) {
		g_error ("failed sigaction");
	}

	sigfillset (&suspend_signal_mask);
	sigdelset (&suspend_signal_mask, restart_signal_num);

	global_remset = alloc_remset (1024, NULL);
	global_remset->next = NULL;

	pthread_key_create (&remembered_set_key, unregister_thread);

#ifndef HAVE_KW_THREAD
	pthread_key_create (&thread_info_key, NULL);
#endif

	if (use_cardtable)
		card_table_init ();

	gc_initialized = TRUE;
	UNLOCK_GC;
	mono_gc_register_thread (&sinfo);
}

int
mono_gc_get_suspend_signal (void)
{
	return suspend_signal_num;
}

enum {
	ATYPE_NORMAL,
	ATYPE_VECTOR,
	ATYPE_SMALL,
	ATYPE_NUM
};

#ifdef HAVE_KW_THREAD
#define EMIT_TLS_ACCESS(mb,dummy,offset)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), (offset));		\
	} while (0)
#else
#define EMIT_TLS_ACCESS(mb,member,dummy)	do {	\
	mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX);	\
	mono_mb_emit_byte ((mb), CEE_MONO_TLS);		\
	mono_mb_emit_i4 ((mb), thread_info_key);	\
	mono_mb_emit_icon ((mb), G_STRUCT_OFFSET (SgenThreadInfo, member));	\
	mono_mb_emit_byte ((mb), CEE_ADD);		\
	mono_mb_emit_byte ((mb), CEE_LDIND_I);		\
	} while (0)
#endif

#ifdef MANAGED_ALLOCATION
/* FIXME: Do this in the JIT, where specialized allocation sequences can be created
 * for each class. This is currently not easy to do, as it is hard to generate basic 
 * blocks + branches, but it is easy with the linear IL codebase.
 *
 * For this to work we'd need to solve the TLAB race, first.  Now we
 * require the allocator to be in a few known methods to make sure
 * that they are executed atomically via the restart mechanism.
 */
static MonoMethod*
create_allocator (int atype)
{
	int p_var, size_var;
	guint32 slowpath_branch, max_size_branch;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoMethodSignature *csig;
	static gboolean registered = FALSE;
	int tlab_next_addr_var, new_next_var;
	int num_params, i;
	const char *name = NULL;
	AllocatorWrapperInfo *info;

#ifdef HAVE_KW_THREAD
	int tlab_next_addr_offset = -1;
	int tlab_temp_end_offset = -1;

	MONO_THREAD_VAR_OFFSET (tlab_next_addr, tlab_next_addr_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	g_assert (tlab_next_addr_offset != -1);
	g_assert (tlab_temp_end_offset != -1);
#endif

	if (!registered) {
		mono_register_jit_icall (mono_gc_alloc_obj, "mono_gc_alloc_obj", mono_create_icall_signature ("object ptr int"), FALSE);
		mono_register_jit_icall (mono_gc_alloc_vector, "mono_gc_alloc_vector", mono_create_icall_signature ("object ptr int int"), FALSE);
		registered = TRUE;
	}

	if (atype == ATYPE_SMALL) {
		num_params = 1;
		name = "AllocSmall";
	} else if (atype == ATYPE_NORMAL) {
		num_params = 1;
		name = "Alloc";
	} else if (atype == ATYPE_VECTOR) {
		num_params = 2;
		name = "AllocVector";
	} else {
		g_assert_not_reached ();
	}

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, num_params);
	csig->ret = &mono_defaults.object_class->byval_arg;
	for (i = 0; i < num_params; ++i)
		csig->params [i] = &mono_defaults.int_class->byval_arg;

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ALLOC);
	size_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	if (atype == ATYPE_NORMAL || atype == ATYPE_SMALL) {
		/* size = vtable->klass->instance_size; */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoClass, instance_size));
		mono_mb_emit_byte (mb, CEE_ADD);
		/* FIXME: assert instance_size stays a 4 byte integer */
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_stloc (mb, size_var);
	} else if (atype == ATYPE_VECTOR) {
		MonoExceptionClause *clause;
		int pos, pos_leave;
		MonoClass *oom_exc_class;
		MonoMethod *ctor;

		/* n > 	MONO_ARRAY_MAX_INDEX -> OverflowException */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, MONO_ARRAY_MAX_INDEX);
		pos = mono_mb_emit_short_branch (mb, CEE_BLE_UN_S);
		mono_mb_emit_exception (mb, "OverflowException", NULL);
		mono_mb_patch_short_branch (mb, pos);

		clause = mono_image_alloc0 (mono_defaults.corlib, sizeof (MonoExceptionClause));
		clause->try_offset = mono_mb_get_label (mb);

		/* vtable->klass->sizes.element_size */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoClass, sizes.element_size));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_U4);

		/* * n */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, CEE_MUL_OVF_UN);
		/* + sizeof (MonoArray) */
		mono_mb_emit_icon (mb, sizeof (MonoArray));
		mono_mb_emit_byte (mb, CEE_ADD_OVF_UN);
		mono_mb_emit_stloc (mb, size_var);

		pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

		/* catch */
		clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
		clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
		clause->data.catch_class = mono_class_from_name (mono_defaults.corlib,
				"System", "OverflowException");
		g_assert (clause->data.catch_class);
		clause->handler_offset = mono_mb_get_label (mb);

		oom_exc_class = mono_class_from_name (mono_defaults.corlib,
				"System", "OutOfMemoryException");
		g_assert (oom_exc_class);
		ctor = mono_class_get_method_from_name (oom_exc_class, ".ctor", 0);
		g_assert (ctor);

		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
		mono_mb_emit_byte (mb, CEE_THROW);

		clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;
		mono_mb_set_clauses (mb, 1, clause);
		mono_mb_patch_branch (mb, pos_leave);
		/* end catch */
	} else {
		g_assert_not_reached ();
	}

	/* size += ALLOC_ALIGN - 1; */
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_icon (mb, ALLOC_ALIGN - 1);
	mono_mb_emit_byte (mb, CEE_ADD);
	/* size &= ~(ALLOC_ALIGN - 1); */
	mono_mb_emit_icon (mb, ~(ALLOC_ALIGN - 1));
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_stloc (mb, size_var);

	/* if (size > MAX_SMALL_OBJ_SIZE) goto slowpath */
	if (atype != ATYPE_SMALL) {
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_icon (mb, MAX_SMALL_OBJ_SIZE);
		max_size_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BGT_S);
	}

	/*
	 * We need to modify tlab_next, but the JIT only supports reading, so we read
	 * another tls var holding its address instead.
	 */

	/* tlab_next_addr (local) = tlab_next_addr (TLS var) */
	tlab_next_addr_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	EMIT_TLS_ACCESS (mb, tlab_next_addr, tlab_next_addr_offset);
	mono_mb_emit_stloc (mb, tlab_next_addr_var);

	/* p = (void**)tlab_next; */
	p_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, p_var);
	
	/* new_next = (char*)p + size; */
	new_next_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_stloc (mb, new_next_var);

	/* tlab_next = new_next */
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_ldloc (mb, new_next_var);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/* if (G_LIKELY (new_next < tlab_temp_end)) */
	mono_mb_emit_ldloc (mb, new_next_var);
	EMIT_TLS_ACCESS (mb, tlab_temp_end, tlab_temp_end_offset);
	slowpath_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BLT_UN_S);

	/* Slowpath */
	if (atype != ATYPE_SMALL)
		mono_mb_patch_short_branch (mb, max_size_branch);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	/* FIXME: mono_gc_alloc_obj takes a 'size_t' as an argument, not an int32 */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, size_var);
	if (atype == ATYPE_NORMAL || atype == ATYPE_SMALL) {
		mono_mb_emit_icall (mb, mono_gc_alloc_obj);
	} else if (atype == ATYPE_VECTOR) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_gc_alloc_vector);
	} else {
		g_assert_not_reached ();
	}
	mono_mb_emit_byte (mb, CEE_RET);

	/* Fastpath */
	mono_mb_patch_short_branch (mb, slowpath_branch);

	/* FIXME: Memory barrier */

	/* *p = vtable; */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	if (atype == ATYPE_VECTOR) {
		/* arr->max_length = max_length; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoArray, max_length));
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, CEE_STIND_I);
	}

	/* return p */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, csig, 8);
	mono_mb_free (mb);
	mono_method_get_header (res)->init_locals = FALSE;

	info = mono_image_alloc0 (mono_defaults.corlib, sizeof (AllocatorWrapperInfo));
	info->gc_name = "sgen";
	info->alloc_type = atype;
	mono_marshal_set_wrapper_info (res, info);

	return res;
}
#endif

const char *
mono_gc_get_gc_name (void)
{
	return "sgen";
}

static MonoMethod* alloc_method_cache [ATYPE_NUM];
static MonoMethod *write_barrier_method;

static gboolean
is_ip_in_managed_allocator (MonoDomain *domain, gpointer ip)
{
	MonoJitInfo *ji;
	MonoMethod *method;
	int i;

	if (!ip || !domain)
		return FALSE;
	ji = mono_jit_info_table_find (domain, ip);
	if (!ji)
		return FALSE;
	method = ji->method;

	if (method == write_barrier_method)
		return TRUE;
	for (i = 0; i < ATYPE_NUM; ++i)
		if (method == alloc_method_cache [i])
			return TRUE;
	return FALSE;
}

/*
 * Generate an allocator method implementing the fast path of mono_gc_alloc_obj ().
 * The signature of the called method is:
 * 	object allocate (MonoVTable *vtable)
 */
MonoMethod*
mono_gc_get_managed_allocator (MonoVTable *vtable, gboolean for_box)
{
#ifdef MANAGED_ALLOCATION
	MonoClass *klass = vtable->klass;

#ifdef HAVE_KW_THREAD
	int tlab_next_offset = -1;
	int tlab_temp_end_offset = -1;
	MONO_THREAD_VAR_OFFSET (tlab_next, tlab_next_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	if (tlab_next_offset == -1 || tlab_temp_end_offset == -1)
		return NULL;
#endif

	if (!mono_runtime_has_tls_get ())
		return NULL;
	if (klass->instance_size > tlab_size)
		return NULL;
	if (klass->has_finalize || klass->marshalbyref || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return NULL;
	if (klass->rank)
		return NULL;
	if (klass->byval_arg.type == MONO_TYPE_STRING)
		return NULL;
	if (collect_before_allocs)
		return NULL;

	if (ALIGN_TO (klass->instance_size, ALLOC_ALIGN) < MAX_SMALL_OBJ_SIZE)
		return mono_gc_get_managed_allocator_by_type (ATYPE_SMALL);
	else
		return mono_gc_get_managed_allocator_by_type (ATYPE_NORMAL);
#else
	return NULL;
#endif
}

MonoMethod*
mono_gc_get_managed_array_allocator (MonoVTable *vtable, int rank)
{
#ifdef MANAGED_ALLOCATION
	MonoClass *klass = vtable->klass;

#ifdef HAVE_KW_THREAD
	int tlab_next_offset = -1;
	int tlab_temp_end_offset = -1;
	MONO_THREAD_VAR_OFFSET (tlab_next, tlab_next_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	if (tlab_next_offset == -1 || tlab_temp_end_offset == -1)
		return NULL;
#endif

	if (rank != 1)
		return NULL;
	if (!mono_runtime_has_tls_get ())
		return NULL;
	if (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS)
		return NULL;
	if (collect_before_allocs)
		return NULL;
	g_assert (!klass->has_finalize && !klass->marshalbyref);

	return mono_gc_get_managed_allocator_by_type (ATYPE_VECTOR);
#else
	return NULL;
#endif
}

MonoMethod*
mono_gc_get_managed_allocator_by_type (int atype)
{
#ifdef MANAGED_ALLOCATION
	MonoMethod *res;

	if (!mono_runtime_has_tls_get ())
		return NULL;

	mono_loader_lock ();
	res = alloc_method_cache [atype];
	if (!res)
		res = alloc_method_cache [atype] = create_allocator (atype);
	mono_loader_unlock ();
	return res;
#else
	return NULL;
#endif
}

guint32
mono_gc_get_managed_allocator_types (void)
{
	return ATYPE_NUM;
}


MonoMethod*
mono_gc_get_write_barrier (void)
{
	MonoMethod *res;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
#ifdef MANAGED_WBARRIER
	int label_no_wb_1, label_no_wb_2, label_no_wb_3, label_no_wb_4, label_need_wb, label_slow_path;
#ifndef SGEN_ALIGN_NURSERY
	int label_continue_1, label_continue_2, label_no_wb_5;
	int dereferenced_var;
#endif
	int buffer_var, buffer_index_var, dummy_var;

#ifdef HAVE_KW_THREAD
	int stack_end_offset = -1, store_remset_buffer_offset = -1;
	int store_remset_buffer_index_offset = -1, store_remset_buffer_index_addr_offset = -1;

	MONO_THREAD_VAR_OFFSET (stack_end, stack_end_offset);
	g_assert (stack_end_offset != -1);
	MONO_THREAD_VAR_OFFSET (store_remset_buffer, store_remset_buffer_offset);
	g_assert (store_remset_buffer_offset != -1);
	MONO_THREAD_VAR_OFFSET (store_remset_buffer_index, store_remset_buffer_index_offset);
	g_assert (store_remset_buffer_index_offset != -1);
	MONO_THREAD_VAR_OFFSET (store_remset_buffer_index_addr, store_remset_buffer_index_addr_offset);
	g_assert (store_remset_buffer_index_addr_offset != -1);
#endif
#endif

	g_assert (!use_cardtable);

	// FIXME: Maybe create a separate version for ctors (the branch would be
	// correctly predicted more times)
	if (write_barrier_method)
		return write_barrier_method;

	/* Create the IL version of mono_gc_barrier_generic_store () */
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	sig->ret = &mono_defaults.void_class->byval_arg;
	sig->params [0] = &mono_defaults.int_class->byval_arg;

	mb = mono_mb_new (mono_defaults.object_class, "wbarrier", MONO_WRAPPER_WRITE_BARRIER);

#ifdef MANAGED_WBARRIER
	if (mono_runtime_has_tls_get ()) {
#ifdef SGEN_ALIGN_NURSERY
		// if (ptr_in_nursery (ptr)) return;
		/*
		 * Masking out the bits might be faster, but we would have to use 64 bit
		 * immediates, which might be slower.
		 */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, DEFAULT_NURSERY_BITS);
		mono_mb_emit_byte (mb, CEE_SHR_UN);
		mono_mb_emit_icon (mb, (mword)nursery_start >> DEFAULT_NURSERY_BITS);
		label_no_wb_1 = mono_mb_emit_branch (mb, CEE_BEQ);

		// if (!ptr_in_nursery (*ptr)) return;
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, DEFAULT_NURSERY_BITS);
		mono_mb_emit_byte (mb, CEE_SHR_UN);
		mono_mb_emit_icon (mb, (mword)nursery_start >> DEFAULT_NURSERY_BITS);
		label_no_wb_2 = mono_mb_emit_branch (mb, CEE_BNE_UN);
#else

		// if (ptr < (nursery_start)) goto continue;
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ptr (mb, (gpointer) nursery_start);
		label_continue_1 = mono_mb_emit_branch (mb, CEE_BLT);

		// if (ptr >= nursery_real_end)) goto continue;
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ptr (mb, (gpointer) nursery_real_end);
		label_continue_2 = mono_mb_emit_branch (mb, CEE_BGE);

		// Otherwise return
		label_no_wb_1 = mono_mb_emit_branch (mb, CEE_BR);

		// continue:
		mono_mb_patch_branch (mb, label_continue_1);
		mono_mb_patch_branch (mb, label_continue_2);

		// Dereference and store in local var
		dereferenced_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, dereferenced_var);

		// if (*ptr < nursery_start) return;
		mono_mb_emit_ldloc (mb, dereferenced_var);
		mono_mb_emit_ptr (mb, (gpointer) nursery_start);
		label_no_wb_2 = mono_mb_emit_branch (mb, CEE_BLT);

		// if (*ptr >= nursery_end) return;
		mono_mb_emit_ldloc (mb, dereferenced_var);
		mono_mb_emit_ptr (mb, (gpointer) nursery_real_end);
		label_no_wb_5 = mono_mb_emit_branch (mb, CEE_BGE);

#endif 
		// if (ptr >= stack_end) goto need_wb;
		mono_mb_emit_ldarg (mb, 0);
		EMIT_TLS_ACCESS (mb, stack_end, stack_end_offset);
		label_need_wb = mono_mb_emit_branch (mb, CEE_BGE_UN);

		// if (ptr >= stack_start) return;
		dummy_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldloc_addr (mb, dummy_var);
		label_no_wb_3 = mono_mb_emit_branch (mb, CEE_BGE_UN);

		// need_wb:
		mono_mb_patch_branch (mb, label_need_wb);

		// buffer = STORE_REMSET_BUFFER;
		buffer_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		EMIT_TLS_ACCESS (mb, store_remset_buffer, store_remset_buffer_offset);
		mono_mb_emit_stloc (mb, buffer_var);

		// buffer_index = STORE_REMSET_BUFFER_INDEX;
		buffer_index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		EMIT_TLS_ACCESS (mb, store_remset_buffer_index, store_remset_buffer_index_offset);
		mono_mb_emit_stloc (mb, buffer_index_var);

		// if (buffer [buffer_index] == ptr) return;
		mono_mb_emit_ldloc (mb, buffer_var);
		mono_mb_emit_ldloc (mb, buffer_index_var);
		g_assert (sizeof (gpointer) == 4 || sizeof (gpointer) == 8);
		mono_mb_emit_icon (mb, sizeof (gpointer) == 4 ? 2 : 3);
		mono_mb_emit_byte (mb, CEE_SHL);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ldarg (mb, 0);
		label_no_wb_4 = mono_mb_emit_branch (mb, CEE_BEQ);

		// ++buffer_index;
		mono_mb_emit_ldloc (mb, buffer_index_var);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, buffer_index_var);

		// if (buffer_index >= STORE_REMSET_BUFFER_SIZE) goto slow_path;
		mono_mb_emit_ldloc (mb, buffer_index_var);
		mono_mb_emit_icon (mb, STORE_REMSET_BUFFER_SIZE);
		label_slow_path = mono_mb_emit_branch (mb, CEE_BGE);

		// buffer [buffer_index] = ptr;
		mono_mb_emit_ldloc (mb, buffer_var);
		mono_mb_emit_ldloc (mb, buffer_index_var);
		g_assert (sizeof (gpointer) == 4 || sizeof (gpointer) == 8);
		mono_mb_emit_icon (mb, sizeof (gpointer) == 4 ? 2 : 3);
		mono_mb_emit_byte (mb, CEE_SHL);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_byte (mb, CEE_STIND_I);

		// STORE_REMSET_BUFFER_INDEX = buffer_index;
		EMIT_TLS_ACCESS (mb, store_remset_buffer_index_addr, store_remset_buffer_index_addr_offset);
		mono_mb_emit_ldloc (mb, buffer_index_var);
		mono_mb_emit_byte (mb, CEE_STIND_I);

		// return;
		mono_mb_patch_branch (mb, label_no_wb_1);
		mono_mb_patch_branch (mb, label_no_wb_2);
		mono_mb_patch_branch (mb, label_no_wb_3);
		mono_mb_patch_branch (mb, label_no_wb_4);
#ifndef SGEN_ALIGN_NURSERY
		mono_mb_patch_branch (mb, label_no_wb_5);
#endif
		mono_mb_emit_byte (mb, CEE_RET);

		// slow path
		mono_mb_patch_branch (mb, label_slow_path);
	}
#endif

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icall (mb, mono_gc_wbarrier_generic_nostore);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, sig, 16);
	mono_mb_free (mb);

	mono_loader_lock ();
	if (write_barrier_method) {
		/* Already created */
		mono_free_method (res);
	} else {
		/* double-checked locking */
		mono_memory_barrier ();
		write_barrier_method = res;
	}
	mono_loader_unlock ();

	return write_barrier_method;
}

char*
mono_gc_get_description (void)
{
	return g_strdup ("sgen");
}

void
mono_gc_set_desktop_mode (void)
{
}

gboolean
mono_gc_is_moving (void)
{
	return TRUE;
}

gboolean
mono_gc_is_disabled (void)
{
	return FALSE;
}

void
mono_sgen_debug_printf (int level, const char *format, ...)
{
	va_list ap;

	if (level > gc_debug_level)
		return;

	va_start (ap, format);
	vfprintf (gc_debug_file, format, ap);
	va_end (ap);
}

#endif /* HAVE_SGEN_GC */
