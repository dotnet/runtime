/*
 * sgen-gchandles.c: SGen GC handles.
 *
 * Copyright (C) 2015 Xamarin Inc
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

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-client.h"
#include "mono/utils/mono-membar.h"

#ifdef HEAVY_STATISTICS
static volatile guint32 stat_gc_handles_allocated = 0;
static volatile guint32 stat_gc_handles_max_allocated = 0;
#endif

#define BUCKETS (32 - MONO_GC_HANDLE_TYPE_SHIFT)
#define MIN_BUCKET_BITS (5)
#define MIN_BUCKET_SIZE (1 << MIN_BUCKET_BITS)

/*
 * A table of GC handle data, implementing a simple lock-free bitmap allocator.
 *
 * 'entries' is an array of pointers to buckets of increasing size. The first
 * bucket has size 'MIN_BUCKET_SIZE', and each bucket is twice the size of the
 * previous, i.e.:
 *
 *           |-------|-- MIN_BUCKET_SIZE
 *    [0] -> xxxxxxxx
 *    [1] -> xxxxxxxxxxxxxxxx
 *    [2] -> xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
 *    ...
 *
 * The size of the spine, 'BUCKETS', is chosen so that the maximum number of
 * entries is no less than the maximum index value of a GC handle.
 *
 * Each entry in a bucket is a pointer with two tag bits: if
 * 'GC_HANDLE_OCCUPIED' returns true for a slot, then the slot is occupied; if
 * so, then 'GC_HANDLE_VALID' gives whether the entry refers to a valid (1) or
 * NULL (0) object reference. If the reference is valid, then the pointer is an
 * object pointer. If the reference is NULL, and 'GC_HANDLE_TYPE_IS_WEAK' is
 * true for 'type', then the pointer is a metadata pointer--this allows us to
 * retrieve the domain ID of an expired weak reference in Mono.
 *
 * Finally, 'slot_hint' denotes the position of the last allocation, so that the
 * whole array needn't be searched on every allocation.
 */

typedef struct {
	volatile gpointer *volatile entries [BUCKETS];
	volatile guint32 capacity;
	volatile guint32 slot_hint;
	volatile guint32 max_index;
	guint8 type;
} HandleData;

static inline guint
bucket_size (guint index)
{
	return 1 << (index + MIN_BUCKET_BITS);
}

/* Computes floor(log2(index + MIN_BUCKET_SIZE)) - 1, giving the index
 * of the bucket containing a slot.
 */
static inline guint
index_bucket (guint index)
{
#ifdef __GNUC__
	return CHAR_BIT * sizeof (index) - __builtin_clz (index + MIN_BUCKET_SIZE) - 1 - MIN_BUCKET_BITS;
#else
	guint count = 0;
	index += MIN_BUCKET_SIZE;
	while (index) {
		++count;
		index >>= 1;
	}
	return count - 1 - MIN_BUCKET_BITS;
#endif
}

static inline void
bucketize (guint index, guint *bucket, guint *offset)
{
	*bucket = index_bucket (index);
	*offset = index - bucket_size (*bucket) + MIN_BUCKET_SIZE;
}

static void
protocol_gchandle_update (int handle_type, gpointer link, gpointer old_value, gpointer new_value)
{
	gboolean old = MONO_GC_HANDLE_IS_OBJECT_POINTER (old_value);
	gboolean new_ = MONO_GC_HANDLE_IS_OBJECT_POINTER (new_value);
	gboolean track = handle_type == HANDLE_WEAK_TRACK;

	if (!MONO_GC_HANDLE_TYPE_IS_WEAK (handle_type))
		return;

	if (!old && new_)
		binary_protocol_dislink_add (link, MONO_GC_REVEAL_POINTER (new_value, TRUE), track);
	else if (old && !new_)
		binary_protocol_dislink_remove (link, track);
	else if (old && new_ && old_value != new_value)
		binary_protocol_dislink_update (link, MONO_GC_REVEAL_POINTER (new_value, TRUE), track);
}

/* Returns the new value in the slot, or NULL if the CAS failed. */
static inline gpointer
try_set_slot (volatile gpointer *slot, GCObject *obj, gpointer old, GCHandleType type)
{
	gpointer new_;
	if (obj)
		new_ = MONO_GC_HANDLE_OBJECT_POINTER (obj, GC_HANDLE_TYPE_IS_WEAK (type));
	else
		new_ = MONO_GC_HANDLE_METADATA_POINTER (sgen_client_default_metadata (), GC_HANDLE_TYPE_IS_WEAK (type));
	SGEN_ASSERT (0, new_, "Why is the occupied bit not set?");
	if (InterlockedCompareExchangePointer (slot, new_, old) == old) {
		protocol_gchandle_update (type, (gpointer)slot, old, new_);
		return new_;
	}
	return NULL;
}

/* Try to claim a slot by setting its occupied bit. */
static inline gboolean
try_occupy_slot (HandleData *handles, guint bucket, guint offset, GCObject *obj, gboolean track)
{
	volatile gpointer *link_addr = &(handles->entries [bucket] [offset]);
	if (MONO_GC_HANDLE_OCCUPIED (*link_addr))
		return FALSE;
	return try_set_slot (link_addr, obj, NULL, (GCHandleType)handles->type) != NULL;
}

static HandleData gc_handles [] = {
	{ { NULL }, 0, 0, 0, (HANDLE_WEAK) },
	{ { NULL }, 0, 0, 0, (HANDLE_WEAK_TRACK) },
	{ { NULL }, 0, 0, 0, (HANDLE_NORMAL) },
	{ { NULL }, 0, 0, 0, (HANDLE_PINNED) }
};

static HandleData *
gc_handles_for_type (GCHandleType type)
{
	return type < HANDLE_TYPE_MAX ? &gc_handles [type] : NULL;
}

/* This assumes that the world is stopped. */
void
sgen_mark_normal_gc_handles (void *addr, SgenUserMarkFunc mark_func, void *gc_data)
{
	HandleData *handles = gc_handles_for_type (HANDLE_NORMAL);
	size_t bucket, offset;
	const guint max_bucket = index_bucket (handles->capacity);
	guint32 index = 0;
	const guint32 max_index = handles->max_index;
	for (bucket = 0; bucket < max_bucket; ++bucket) {
		volatile gpointer *entries = handles->entries [bucket];
		for (offset = 0; offset < bucket_size (bucket); ++offset, ++index) {
			volatile gpointer *entry;
			gpointer hidden, revealed;
			/* No need to iterate beyond the largest index ever allocated. */
			if (index > max_index)
				return;
			entry = &entries [offset];
			hidden = *entry;
			revealed = MONO_GC_REVEAL_POINTER (hidden, FALSE);
			if (!MONO_GC_HANDLE_IS_OBJECT_POINTER (hidden))
				continue;
			mark_func ((MonoObject **)&revealed, gc_data);
			g_assert (revealed);
			*entry = MONO_GC_HANDLE_OBJECT_POINTER (revealed, FALSE);
		}
	}
}

static guint
handle_data_find_unset (HandleData *handles, guint32 begin, guint32 end)
{
	guint index;
	gint delta = begin < end ? +1 : -1;
	for (index = begin; index < end; index += delta) {
		guint bucket, offset;
		volatile gpointer *entries;
		bucketize (index, &bucket, &offset);
		entries = handles->entries [bucket];
		g_assert (entries);
		if (!MONO_GC_HANDLE_OCCUPIED (entries [offset]))
			return index;
	}
	return -1;
}

/* Adds a bucket if necessary and possible. */
static void
handle_data_grow (HandleData *handles, guint32 old_capacity)
{
	const guint new_bucket = index_bucket (old_capacity);
	const guint32 growth = bucket_size (new_bucket);
	const guint32 new_capacity = old_capacity + growth;
	gpointer *entries;
	const size_t new_bucket_size = sizeof (**handles->entries) * growth;
	if (handles->capacity >= new_capacity)
		return;
	entries = (gpointer *)g_malloc0 (new_bucket_size);
	if (handles->type == HANDLE_PINNED)
		sgen_register_root ((char *)entries, new_bucket_size, SGEN_DESCRIPTOR_NULL, ROOT_TYPE_PINNED, MONO_ROOT_SOURCE_GC_HANDLE, "pinned gc handles");
	/* The zeroing of the newly allocated bucket must be complete before storing
	 * the new bucket pointer.
	 */
	mono_memory_write_barrier ();
	if (InterlockedCompareExchangePointer ((volatile gpointer *)&handles->entries [new_bucket], entries, NULL) == NULL) {
		/* It must not be the case that we succeeded in setting the bucket
		 * pointer, while someone else succeeded in changing the capacity.
		 */
		if (InterlockedCompareExchange ((volatile gint32 *)&handles->capacity, new_capacity, old_capacity) != old_capacity)
			g_assert_not_reached ();
		handles->slot_hint = old_capacity;
		return;
	}
	/* Someone beat us to the allocation. */
	if (handles->type == HANDLE_PINNED)
		sgen_deregister_root ((char *)entries);
	g_free (entries);
}

static guint32
alloc_handle (HandleData *handles, GCObject *obj, gboolean track)
{
	guint index;
	guint32 res;
	guint bucket, offset;
	guint32 capacity;
	guint32 slot_hint;
	guint32 max_index;
	if (!handles->capacity)
		handle_data_grow (handles, 0);
retry:
	capacity = handles->capacity;
	slot_hint = handles->slot_hint;
	index = handle_data_find_unset (handles, slot_hint, capacity);
	if (index == -1)
		index = handle_data_find_unset (handles, 0, slot_hint);
	if (index == -1) {
		handle_data_grow (handles, capacity);
		goto retry;
	}
	handles->slot_hint = index;

	/*
	 * If a GC happens shortly after a new bucket is allocated, the entire
	 * bucket could be scanned even though it's mostly empty. To avoid this,
	 * we track the maximum index seen so far, so that we can skip the empty
	 * slots.
	 *
	 * Note that we update `max_index` before we even try occupying the
	 * slot.  If we did it the other way around and a GC happened in
	 * between, the GC wouldn't know that the slot was occupied.  This is
	 * not a huge deal since `obj` is on the stack and thus pinned anyway,
	 * but hopefully some day it won't be anymore.
	 */
	do {
		max_index = handles->max_index;
		if (index <= max_index)
			break;
	} while (InterlockedCompareExchange ((volatile gint32 *)&handles->max_index, index, max_index) != max_index);

	bucketize (index, &bucket, &offset);
	if (!try_occupy_slot (handles, bucket, offset, obj, track))
		goto retry;
#ifdef HEAVY_STATISTICS
	InterlockedIncrement ((volatile gint32 *)&stat_gc_handles_allocated);
	if (stat_gc_handles_allocated > stat_gc_handles_max_allocated)
		stat_gc_handles_max_allocated = stat_gc_handles_allocated;
#endif
	/* Ensure that a GC handle cannot be given to another thread without the slot having been set. */
	mono_memory_write_barrier ();
	res = MONO_GC_HANDLE (index, handles->type);
	sgen_client_gchandle_created (handles->type, obj, res);
	return res;
}

static gboolean
object_older_than (GCObject *object, int generation)
{
	return generation == GENERATION_NURSERY && !sgen_ptr_in_nursery (object);
}

/*
 * Maps a function over all GC handles.
 * This assumes that the world is stopped!
 */
void
sgen_gchandle_iterate (GCHandleType handle_type, int max_generation, SgenGCHandleIterateCallback callback, gpointer user)
{
	HandleData *handle_data = gc_handles_for_type (handle_type);
	size_t bucket, offset;
	guint max_bucket = index_bucket (handle_data->capacity);
	guint32 index = 0;
	guint32 max_index = handle_data->max_index;
	/* If a new bucket has been allocated, but the capacity has not yet been
	 * increased, nothing can yet have been allocated in the bucket because the
	 * world is stopped, so we shouldn't miss any handles during iteration.
	 */
	for (bucket = 0; bucket < max_bucket; ++bucket) {
		volatile gpointer *entries = handle_data->entries [bucket];
		for (offset = 0; offset < bucket_size (bucket); ++offset, ++index) {
			gpointer hidden;
			gpointer result;
			/* Table must contain no garbage pointers. */
			gboolean occupied;
			/* No need to iterate beyond the largest index ever allocated. */
			if (index > max_index)
					return;
			hidden = entries [offset];
			occupied = MONO_GC_HANDLE_OCCUPIED (hidden);
			g_assert (hidden ? occupied : !occupied);
			if (!occupied)
				continue;
			result = callback (hidden, handle_type, max_generation, user);
			if (result)
				SGEN_ASSERT (0, MONO_GC_HANDLE_OCCUPIED (result), "Why did the callback return an unoccupied entry?");
			else
				HEAVY_STAT (InterlockedDecrement ((volatile gint32 *)&stat_gc_handles_allocated));
			protocol_gchandle_update (handle_type, (gpointer)&entries [offset], hidden, result);
			entries [offset] = result;
		}
	}
}

/**
 * mono_gchandle_new:
 * @obj: managed object to get a handle for
 * @pinned: whether the object should be pinned
 *
 * This returns a handle that wraps the object, this is used to keep a
 * reference to a managed object from the unmanaged world and preventing the
 * object from being disposed.
 * 
 * If @pinned is false the address of the object can not be obtained, if it is
 * true the address of the object can be obtained.  This will also pin the
 * object so it will not be possible by a moving garbage collector to move the
 * object. 
 * 
 * Returns: a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new (GCObject *obj, gboolean pinned)
{
	return alloc_handle (gc_handles_for_type (pinned ? HANDLE_PINNED : HANDLE_NORMAL), obj, FALSE);
}

/**
 * mono_gchandle_new_weakref:
 * @obj: managed object to get a handle for
 * @track_resurrection: Determines how long to track the object, if this is set to TRUE, the object is tracked after finalization, if FALSE, the object is only tracked up until the point of finalization.
 *
 * This returns a weak handle that wraps the object, this is used to
 * keep a reference to a managed object from the unmanaged world.
 * Unlike the mono_gchandle_new the object can be reclaimed by the
 * garbage collector.  In this case the value of the GCHandle will be
 * set to zero.
 * 
 * If @track_resurrection is TRUE the object will be tracked through
 * finalization and if the object is resurrected during the execution
 * of the finalizer, then the returned weakref will continue to hold
 * a reference to the object.   If @track_resurrection is FALSE, then
 * the weak reference's target will become NULL as soon as the object
 * is passed on to the finalizer.
 * 
 * Returns: a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new_weakref (GCObject *obj, gboolean track_resurrection)
{
	return alloc_handle (gc_handles_for_type (track_resurrection ? HANDLE_WEAK_TRACK : HANDLE_WEAK), obj, track_resurrection);
}

static GCObject *
link_get (volatile gpointer *link_addr, gboolean is_weak)
{
	void *volatile *link_addr_volatile;
	void *ptr;
	GCObject *obj;
retry:
	link_addr_volatile = link_addr;
	ptr = (void*)*link_addr_volatile;
	/*
	 * At this point we have a hidden pointer.  If the GC runs
	 * here, it will not recognize the hidden pointer as a
	 * reference, and if the object behind it is not referenced
	 * elsewhere, it will be freed.  Once the world is restarted
	 * we reveal the pointer, giving us a pointer to a freed
	 * object.  To make sure we don't return it, we load the
	 * hidden pointer again.  If it's still the same, we can be
	 * sure the object reference is valid.
	 */
	if (ptr && MONO_GC_HANDLE_IS_OBJECT_POINTER (ptr))
		obj = (GCObject *)MONO_GC_REVEAL_POINTER (ptr, is_weak);
	else
		return NULL;

	/* Note [dummy use]:
	 *
	 * If a GC happens here, obj needs to be on the stack or in a
	 * register, so we need to prevent this from being reordered
	 * wrt the check.
	 */
	sgen_dummy_use (obj);
	mono_memory_barrier ();

	if (is_weak)
		sgen_client_ensure_weak_gchandles_accessible ();

	if ((void*)*link_addr_volatile != ptr)
		goto retry;

	return obj;
}

/**
 * mono_gchandle_get_target:
 * @gchandle: a GCHandle's handle.
 *
 * The handle was previously created by calling `mono_gchandle_new` or
 * `mono_gchandle_new_weakref`. 
 *
 * Returns a pointer to the `MonoObject*` represented by the handle or
 * NULL for a collected object if using a weakref handle.
 */
GCObject*
mono_gchandle_get_target (guint32 gchandle)
{
	guint index = MONO_GC_HANDLE_SLOT (gchandle);
	GCHandleType type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	/* Invalid handles are possible; accessing one should produce NULL. (#34276) */
	if (!handles)
		return NULL;
	guint bucket, offset;
	g_assert (index < handles->capacity);
	bucketize (index, &bucket, &offset);
	return link_get (&handles->entries [bucket] [offset], MONO_GC_HANDLE_TYPE_IS_WEAK (type));
}

void
sgen_gchandle_set_target (guint32 gchandle, GCObject *obj)
{
	guint index = MONO_GC_HANDLE_SLOT (gchandle);
	GCHandleType type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	if (!handles)
		return;
	guint bucket, offset;
	gpointer slot;

	g_assert (index < handles->capacity);
	bucketize (index, &bucket, &offset);

	do {
		slot = handles->entries [bucket] [offset];
		SGEN_ASSERT (0, MONO_GC_HANDLE_OCCUPIED (slot), "Why are we setting the target on an unoccupied slot?");
	} while (!try_set_slot (&handles->entries [bucket] [offset], obj, slot, (GCHandleType)handles->type));
}

static gpointer
mono_gchandle_slot_metadata (volatile gpointer *slot_addr, gboolean is_weak)
{
	gpointer slot;
	gpointer metadata;
retry:
	slot = *slot_addr;
	if (!MONO_GC_HANDLE_OCCUPIED (slot))
		return NULL;
	if (MONO_GC_HANDLE_IS_OBJECT_POINTER (slot)) {
		GCObject *obj = (GCObject *)MONO_GC_REVEAL_POINTER (slot, is_weak);
		/* See note [dummy use]. */
		sgen_dummy_use (obj);
		/*
		 * FIXME: The compiler could technically not carry a reference to obj around
		 * at this point and recompute it later, in which case we would still use
		 * it.
		 */
		if (*slot_addr != slot)
			goto retry;
		return sgen_client_metadata_for_object (obj);
	}
	metadata = MONO_GC_REVEAL_POINTER (slot, is_weak);
	/* See note [dummy use]. */
	sgen_dummy_use (metadata);
	if (*slot_addr != slot)
		goto retry;
	return metadata;
}

gpointer
sgen_gchandle_get_metadata (guint32 gchandle)
{
	guint index = MONO_GC_HANDLE_SLOT (gchandle);
	GCHandleType type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	if (!handles)
		return NULL;
	guint bucket, offset;
	if (index >= handles->capacity)
		return NULL;
	bucketize (index, &bucket, &offset);
	return mono_gchandle_slot_metadata (&handles->entries [bucket] [offset], MONO_GC_HANDLE_TYPE_IS_WEAK (type));
}

/**
 * mono_gchandle_free:
 * @gchandle: a GCHandle's handle.
 *
 * Frees the @gchandle handle.  If there are no outstanding
 * references, the garbage collector can reclaim the memory of the
 * object wrapped. 
 */
void
mono_gchandle_free (guint32 gchandle)
{
	guint index = MONO_GC_HANDLE_SLOT (gchandle);
	GCHandleType type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	if (!handles)
		return;
	guint bucket, offset;
	gpointer slot;
	bucketize (index, &bucket, &offset);
	slot = handles->entries [bucket] [offset];
	if (index < handles->capacity && MONO_GC_HANDLE_OCCUPIED (slot)) {
		handles->entries [bucket] [offset] = NULL;
		protocol_gchandle_update (handles->type, (gpointer)&handles->entries [bucket] [offset], slot, NULL);
		HEAVY_STAT (InterlockedDecrement ((volatile gint32 *)&stat_gc_handles_allocated));
	} else {
		/* print a warning? */
	}
	sgen_client_gchandle_destroyed (handles->type, gchandle);
}

/*
 * Returns whether to remove the link from its hash.
 */
static gpointer
null_link_if_necessary (gpointer hidden, GCHandleType handle_type, int max_generation, gpointer user)
{
	const gboolean is_weak = GC_HANDLE_TYPE_IS_WEAK (handle_type);
	ScanCopyContext *ctx = (ScanCopyContext *)user;
	GCObject *obj;
	GCObject *copy;

	if (!MONO_GC_HANDLE_VALID (hidden))
		return hidden;

	obj = (GCObject *)MONO_GC_REVEAL_POINTER (hidden, MONO_GC_HANDLE_TYPE_IS_WEAK (handle_type));
	SGEN_ASSERT (0, obj, "Why is the hidden pointer NULL?");

	if (object_older_than (obj, max_generation))
		return hidden;

	if (major_collector.is_object_live (obj))
		return hidden;

	/* Clear link if object is ready for finalization. This check may be redundant wrt is_object_live(). */
	if (sgen_gc_is_object_ready_for_finalization (obj))
		return MONO_GC_HANDLE_METADATA_POINTER (sgen_client_metadata_for_object (obj), is_weak);

	copy = obj;
	ctx->ops->copy_or_mark_object (&copy, ctx->queue);
	SGEN_ASSERT (0, copy, "Why couldn't we copy the object?");
	/* Update link if object was moved. */
	return MONO_GC_HANDLE_OBJECT_POINTER (copy, is_weak);
}

/* LOCKING: requires that the GC lock is held */
void
sgen_null_link_in_range (int generation, ScanCopyContext ctx, gboolean track)
{
	sgen_gchandle_iterate (track ? HANDLE_WEAK_TRACK : HANDLE_WEAK, generation, null_link_if_necessary, &ctx);
}

typedef struct {
	SgenObjectPredicateFunc predicate;
	gpointer data;
} WeakLinkAlivePredicateClosure;

static gpointer
null_link_if (gpointer hidden, GCHandleType handle_type, int max_generation, gpointer user)
{
	WeakLinkAlivePredicateClosure *closure = (WeakLinkAlivePredicateClosure *)user;
	GCObject *obj;

	if (!MONO_GC_HANDLE_VALID (hidden))
		return hidden;

	obj = (GCObject *)MONO_GC_REVEAL_POINTER (hidden, MONO_GC_HANDLE_TYPE_IS_WEAK (handle_type));
	SGEN_ASSERT (0, obj, "Why is the hidden pointer NULL?");

	if (object_older_than (obj, max_generation))
		return hidden;

	if (closure->predicate (obj, closure->data))
		return NULL;

	return hidden;
}

/* LOCKING: requires that the GC lock is held */
void
sgen_null_links_if (SgenObjectPredicateFunc predicate, void *data, int generation, gboolean track)
{
	WeakLinkAlivePredicateClosure closure = { predicate, data };
	sgen_gchandle_iterate (track ? HANDLE_WEAK_TRACK : HANDLE_WEAK, generation, null_link_if, &closure);
}

void
sgen_init_gchandles (void)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("GC handles allocated", MONO_COUNTER_GC | MONO_COUNTER_UINT, (void *)&stat_gc_handles_allocated);
	mono_counters_register ("max GC handles allocated", MONO_COUNTER_GC | MONO_COUNTER_UINT, (void *)&stat_gc_handles_max_allocated);
#endif
}

#endif
