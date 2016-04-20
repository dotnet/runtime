/*
 * sgen-gchandles.c: SGen GC handles.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-client.h"
#include "mono/sgen/sgen-array-list.h"
#include "mono/utils/mono-membar.h"

#ifdef HEAVY_STATISTICS
static volatile guint32 stat_gc_handles_allocated = 0;
static volatile guint32 stat_gc_handles_max_allocated = 0;
#endif

/*
 * A table of GC handle data, implementing a simple lock-free bitmap allocator.
 *
 * Each entry in a bucket is a pointer with two tag bits: if
 * 'GC_HANDLE_OCCUPIED' returns true for a slot, then the slot is occupied; if
 * so, then 'GC_HANDLE_VALID' gives whether the entry refers to a valid (1) or
 * NULL (0) object reference. If the reference is valid, then the pointer is an
 * object pointer. If the reference is NULL, and 'GC_HANDLE_TYPE_IS_WEAK' is
 * true for 'type', then the pointer is a metadata pointer--this allows us to
 * retrieve the domain ID of an expired weak reference in Mono.
 */

typedef struct {
	SgenArrayList entries_array;
	guint8 type;
} HandleData;

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

static inline gboolean
is_slot_set (volatile gpointer *slot)
{
	gpointer entry = *slot;
	if (MONO_GC_HANDLE_OCCUPIED (entry))
		return TRUE;
	return FALSE;
}

/* Try to claim a slot by setting its occupied bit. */
static inline gboolean
try_occupy_slot (volatile gpointer *slot, gpointer obj, int data)
{
	if (is_slot_set (slot))
		return FALSE;
	return try_set_slot (slot, (GCObject *)obj, NULL, (GCHandleType)data) != NULL;
}

static void
bucket_alloc_callback (gpointer *bucket, guint32 new_bucket_size, gboolean alloc)
{
	if (alloc)
		sgen_register_root ((char *)bucket, new_bucket_size, SGEN_DESCRIPTOR_NULL, ROOT_TYPE_PINNED, MONO_ROOT_SOURCE_GC_HANDLE, "pinned gc handles");
	else
		sgen_deregister_root ((char *)bucket);
}

static HandleData gc_handles [] = {
	{ SGEN_ARRAY_LIST_INIT (NULL, is_slot_set, try_occupy_slot, -1), (HANDLE_WEAK) },
	{ SGEN_ARRAY_LIST_INIT (NULL, is_slot_set, try_occupy_slot, -1), (HANDLE_WEAK_TRACK) },
	{ SGEN_ARRAY_LIST_INIT (NULL, is_slot_set, try_occupy_slot, -1), (HANDLE_NORMAL) },
	{ SGEN_ARRAY_LIST_INIT (bucket_alloc_callback, is_slot_set, try_occupy_slot, -1), (HANDLE_PINNED) }
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
	SgenArrayList *array = &handles->entries_array;
	volatile gpointer *slot;
	gpointer hidden, revealed;

	SGEN_ARRAY_LIST_FOREACH_SLOT (array, slot) {
		hidden = *slot;
		revealed = MONO_GC_REVEAL_POINTER (hidden, FALSE);
		if (!MONO_GC_HANDLE_IS_OBJECT_POINTER (hidden))
			continue;
		mark_func ((MonoObject **)&revealed, gc_data);
		g_assert (revealed);
		*slot = MONO_GC_HANDLE_OBJECT_POINTER (revealed, FALSE);
	} SGEN_ARRAY_LIST_END_FOREACH_SLOT;
}


static guint32
alloc_handle (HandleData *handles, GCObject *obj, gboolean track)
{
	guint32 res, index;
	SgenArrayList *array = &handles->entries_array;

	/*
	 * If a GC happens shortly after a new bucket is allocated, the entire
	 * bucket could be scanned even though it's mostly empty. To avoid this,
	 * we track the maximum index seen so far, so that we can skip the empty
	 * slots.
	 *
	 * Note that we update `next_slot` before we even try occupying the
	 * slot.  If we did it the other way around and a GC happened in
	 * between, the GC wouldn't know that the slot was occupied.  This is
	 * not a huge deal since `obj` is on the stack and thus pinned anyway,
	 * but hopefully some day it won't be anymore.
	 */
	index = sgen_array_list_add (array, obj, handles->type, TRUE);
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
	SgenArrayList *array = &handle_data->entries_array;
	gpointer hidden, result, occupied;
	volatile gpointer *slot;

	/* If a new bucket has been allocated, but the capacity has not yet been
	 * increased, nothing can yet have been allocated in the bucket because the
	 * world is stopped, so we shouldn't miss any handles during iteration.
	 */
	SGEN_ARRAY_LIST_FOREACH_SLOT (array, slot) {
		hidden = *slot;
		occupied = (gpointer) MONO_GC_HANDLE_OCCUPIED (hidden);
		g_assert (hidden ? !!occupied : !occupied);
		if (!occupied)
			continue;
		result = callback (hidden, handle_type, max_generation, user);
		if (result)
			SGEN_ASSERT (0, MONO_GC_HANDLE_OCCUPIED (result), "Why did the callback return an unoccupied entry?");
		else
			HEAVY_STAT (InterlockedDecrement ((volatile gint32 *)&stat_gc_handles_allocated));
		protocol_gchandle_update (handle_type, (gpointer)slot, hidden, result);
		*slot = result;
	} SGEN_ARRAY_LIST_END_FOREACH_SLOT;
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
	return link_get (sgen_array_list_get_slot (&handles->entries_array, index), MONO_GC_HANDLE_TYPE_IS_WEAK (type));
}

void
sgen_gchandle_set_target (guint32 gchandle, GCObject *obj)
{
	guint32 index = MONO_GC_HANDLE_SLOT (gchandle);
	GCHandleType type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	volatile gpointer *slot;
	gpointer entry;

	if (!handles)
		return;

	slot = sgen_array_list_get_slot (&handles->entries_array, index);

	do {
		entry = *slot;
		SGEN_ASSERT (0, MONO_GC_HANDLE_OCCUPIED (entry), "Why are we setting the target on an unoccupied slot?");
	} while (!try_set_slot (slot, obj, entry, (GCHandleType)handles->type));
}

static gpointer
mono_gchandle_slot_metadata (volatile gpointer *slot, gboolean is_weak)
{
	gpointer entry;
	gpointer metadata;
retry:
	entry = *slot;
	if (!MONO_GC_HANDLE_OCCUPIED (entry))
		return NULL;
	if (MONO_GC_HANDLE_IS_OBJECT_POINTER (entry)) {
		GCObject *obj = (GCObject *)MONO_GC_REVEAL_POINTER (entry, is_weak);
		/* See note [dummy use]. */
		sgen_dummy_use (obj);
		/*
		 * FIXME: The compiler could technically not carry a reference to obj around
		 * at this point and recompute it later, in which case we would still use
		 * it.
		 */
		if (*slot != entry)
			goto retry;
		return sgen_client_metadata_for_object (obj);
	}
	metadata = MONO_GC_REVEAL_POINTER (entry, is_weak);
	/* See note [dummy use]. */
	sgen_dummy_use (metadata);
	if (*slot != entry)
		goto retry;
	return metadata;
}

gpointer
sgen_gchandle_get_metadata (guint32 gchandle)
{
	guint32 index = MONO_GC_HANDLE_SLOT (gchandle);
	GCHandleType type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	volatile gpointer *slot;

	if (!handles)
		return NULL;
	if (index >= handles->entries_array.capacity)
		return NULL;

	slot = sgen_array_list_get_slot (&handles->entries_array, index);

	return mono_gchandle_slot_metadata (slot, MONO_GC_HANDLE_TYPE_IS_WEAK (type));
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
	guint32 index = MONO_GC_HANDLE_SLOT (gchandle);
	GCHandleType type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = gc_handles_for_type (type);
	volatile gpointer *slot;
	gpointer entry;
	if (!handles)
		return;

	slot = sgen_array_list_get_slot (&handles->entries_array, index);
	entry = *slot;

	if (index < handles->entries_array.capacity && MONO_GC_HANDLE_OCCUPIED (entry)) {
		*slot = NULL;
		protocol_gchandle_update (handles->type, (gpointer)slot, entry, NULL);
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
		return MONO_GC_HANDLE_METADATA_POINTER (sgen_client_default_metadata (), GC_HANDLE_TYPE_IS_WEAK (handle_type));

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
