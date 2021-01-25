/**
 * \file
 * SGen GC handles.
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


typedef struct {
        size_t num_handles [HANDLE_TYPE_MAX];
} GCHandleClassEntry;

static gboolean do_gchandle_stats = FALSE;

static SgenHashTable gchandle_class_hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_STATISTICS, INTERNAL_MEM_STAT_GCHANDLE_CLASS, sizeof (GCHandleClassEntry), g_str_hash, g_str_equal);

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
		sgen_binary_protocol_dislink_add (link, MONO_GC_REVEAL_POINTER (new_value, TRUE), track);
	else if (old && !new_)
		sgen_binary_protocol_dislink_remove (link, track);
	else if (old && new_ && old_value != new_value)
		sgen_binary_protocol_dislink_update (link, MONO_GC_REVEAL_POINTER (new_value, TRUE), track);
}

/* Returns the new value in the slot, or NULL if the CAS failed. */
static gpointer
try_set_slot (volatile gpointer *slot, GCObject *obj, gpointer old, GCHandleType type)
{
	gpointer new_;
	if (obj)
		new_ = MONO_GC_HANDLE_OBJECT_POINTER (obj, GC_HANDLE_TYPE_IS_WEAK (type));
	else
		new_ = MONO_GC_HANDLE_METADATA_POINTER (sgen_client_default_metadata (), GC_HANDLE_TYPE_IS_WEAK (type));
	SGEN_ASSERT (0, new_, "Why is the occupied bit not set?");
	if (mono_atomic_cas_ptr (slot, new_, old) == old) {
		protocol_gchandle_update (type, (gpointer)slot, old, new_);
		return new_;
	}
	return NULL;
}

static gboolean
is_slot_set (volatile gpointer *slot)
{
	gpointer entry = *slot;
	if (MONO_GC_HANDLE_OCCUPIED (entry))
		return TRUE;
	return FALSE;
}

/* Try to claim a slot by setting its occupied bit. */
static gboolean
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
		sgen_register_root ((char *)bucket, new_bucket_size, SGEN_DESCRIPTOR_NULL, ROOT_TYPE_PINNED, MONO_ROOT_SOURCE_GC_HANDLE, NULL, "GC Handle Bucket (SGen, Pinned)");
	else
		sgen_deregister_root ((char *)bucket);
}

static void
bucket_alloc_report_root (gpointer *bucket, guint32 new_bucket_size, gboolean alloc)
{
	if (alloc)
		sgen_client_root_registered ((char *)bucket, new_bucket_size, MONO_ROOT_SOURCE_GC_HANDLE, NULL, "GC Handle Bucket (SGen, Normal)");
	else
		sgen_client_root_deregistered ((char *)bucket);
}

static HandleData gc_handles [] = {
	{ SGEN_ARRAY_LIST_INIT (NULL, is_slot_set, try_occupy_slot, -1), (HANDLE_WEAK) },
	{ SGEN_ARRAY_LIST_INIT (NULL, is_slot_set, try_occupy_slot, -1), (HANDLE_WEAK_TRACK) },
	{ SGEN_ARRAY_LIST_INIT (bucket_alloc_report_root, is_slot_set, try_occupy_slot, -1), (HANDLE_NORMAL) },
	{ SGEN_ARRAY_LIST_INIT (bucket_alloc_callback, is_slot_set, try_occupy_slot, -1), (HANDLE_PINNED) },
	{ SGEN_ARRAY_LIST_INIT (NULL, is_slot_set, try_occupy_slot, -1), (HANDLE_WEAK_FIELDS) },
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

void
sgen_gc_handles_report_roots (SgenUserReportRootFunc report_func, void *gc_data)
{
	HandleData *handles = gc_handles_for_type (HANDLE_NORMAL);
	SgenArrayList *array = &handles->entries_array;
	volatile gpointer *slot;
	gpointer hidden, revealed;

	SGEN_ARRAY_LIST_FOREACH_SLOT (array, slot) {
		hidden = *slot;
		revealed = MONO_GC_REVEAL_POINTER (hidden, FALSE);

		if (MONO_GC_HANDLE_IS_OBJECT_POINTER (hidden))
			report_func ((void*)slot, (GCObject*)revealed, gc_data);
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
	mono_atomic_inc_i32 ((volatile gint32 *)&stat_gc_handles_allocated);
	if (stat_gc_handles_allocated > stat_gc_handles_max_allocated)
		stat_gc_handles_max_allocated = stat_gc_handles_allocated;
#endif
	/* Ensure that a GC handle cannot be given to another thread without the slot having been set. */
	mono_memory_write_barrier ();
	res = MONO_GC_HANDLE (index, handles->type);
	sgen_client_gchandle_created ((GCHandleType)handles->type, obj, res);
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
			HEAVY_STAT (mono_atomic_dec_i32 ((volatile gint32 *)&stat_gc_handles_allocated));
		protocol_gchandle_update (handle_type, (gpointer)slot, hidden, result);
		*slot = result;
	} SGEN_ARRAY_LIST_END_FOREACH_SLOT;
}

guint32
sgen_gchandle_new (GCObject *obj, gboolean pinned)
{
	return alloc_handle (gc_handles_for_type (pinned ? HANDLE_PINNED : HANDLE_NORMAL), obj, FALSE);
}

guint32
sgen_gchandle_new_weakref (GCObject *obj, gboolean track_resurrection)
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

GCObject*
sgen_gchandle_get_target (guint32 gchandle)
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

void
sgen_gchandle_free (guint32 gchandle)
{
	if (!gchandle)
		return;

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
		HEAVY_STAT (mono_atomic_dec_i32 ((volatile gint32 *)&stat_gc_handles_allocated));
	} else {
		/* print a warning? */
	}
	sgen_client_gchandle_destroyed ((GCHandleType)handles->type, gchandle);
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

	if (sgen_major_collector.is_object_live (obj))
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

static gpointer
scan_for_weak (gpointer hidden, GCHandleType handle_type, int max_generation, gpointer user)
{
	const gboolean is_weak = GC_HANDLE_TYPE_IS_WEAK (handle_type);
	ScanCopyContext *ctx = (ScanCopyContext *)user;

	if (!MONO_GC_HANDLE_VALID (hidden))
		return hidden;

	GCObject *obj = (GCObject *)MONO_GC_REVEAL_POINTER (hidden, is_weak);

	/* If the object is dead we free the gc handle */
	if (!sgen_is_object_alive_for_current_gen (obj))
		return NULL;

	/* Relocate it */
	ctx->ops->copy_or_mark_object (&obj, ctx->queue);

	int nbits;
	gsize *weak_bitmap = sgen_client_get_weak_bitmap (SGEN_LOAD_VTABLE (obj), &nbits);
	for (int i = 0; i < nbits; ++i) {
		if (weak_bitmap [i / (sizeof (gsize) * 8)] & ((gsize)1 << (i % (sizeof (gsize) * 8)))) {
			GCObject **addr = (GCObject **)((char*)obj + (i * sizeof (gpointer)));
			GCObject *field = *addr;

			/* if the object in the weak field is alive, we relocate it */
			if (field && sgen_is_object_alive_for_current_gen (field))
				ctx->ops->copy_or_mark_object (addr, ctx->queue);
			else
				*addr = NULL;
	   }
	}

	/* Update link if object was moved. */
	return MONO_GC_HANDLE_OBJECT_POINTER (obj, is_weak);
}

/* LOCKING: requires that the GC lock is held */
void
sgen_null_link_in_range (int generation, ScanCopyContext ctx, gboolean track)
{
	sgen_gchandle_iterate (track ? HANDLE_WEAK_TRACK : HANDLE_WEAK, generation, null_link_if_necessary, &ctx);

	//we're always called for gen zero. !track means short ref
	if (generation == 0 && !track)
		sgen_gchandle_iterate (HANDLE_WEAK_FIELDS, generation, scan_for_weak, &ctx);
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
sgen_register_obj_with_weak_fields (GCObject *obj)
{
	//
	// We use a gc handle to be able to do some processing for these objects at every gc
	//
	alloc_handle (gc_handles_for_type (HANDLE_WEAK_FIELDS), obj, FALSE);
}

#ifndef DISABLE_SGEN_DEBUG_HELPERS
void
sgen_gchandle_stats_enable (void)
{
	do_gchandle_stats = TRUE;
}

static void
sgen_gchandle_stats_register_vtable (GCVTable vtable, int handle_type)
{
	GCHandleClassEntry *entry;

	char *name = g_strdup_printf ("%s.%s", sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable));
	entry = (GCHandleClassEntry*) sgen_hash_table_lookup (&gchandle_class_hash_table, name);

	if (entry) {
		g_free (name);
	} else {
		// Create the entry for this class and get the address of it
		GCHandleClassEntry empty_entry;
		memset (&empty_entry, 0, sizeof (GCHandleClassEntry));
		sgen_hash_table_replace (&gchandle_class_hash_table, name, &empty_entry, NULL);
		entry = (GCHandleClassEntry*) sgen_hash_table_lookup (&gchandle_class_hash_table, name);
	}

	entry->num_handles [handle_type]++;
}

static void
sgen_gchandle_stats_count (void)
{
	int i;

	sgen_hash_table_clean (&gchandle_class_hash_table);

	for (i = HANDLE_TYPE_MIN; i < HANDLE_TYPE_MAX; i++) {
		HandleData *handles = gc_handles_for_type ((GCHandleType)i);
		SgenArrayList *array = &handles->entries_array;
		volatile gpointer *slot;
		gpointer hidden, revealed;

		SGEN_ARRAY_LIST_FOREACH_SLOT (array, slot) {
			hidden = *slot;
			revealed = MONO_GC_REVEAL_POINTER (hidden, MONO_GC_HANDLE_TYPE_IS_WEAK (i));

			if (MONO_GC_HANDLE_IS_OBJECT_POINTER (hidden))
				sgen_gchandle_stats_register_vtable (SGEN_LOAD_VTABLE (revealed), i);
		} SGEN_ARRAY_LIST_END_FOREACH_SLOT;
	}
}

void
sgen_gchandle_stats_report (void)
{
	char *name;
	GCHandleClassEntry *gchandle_entry;

	if (!do_gchandle_stats)
		return;

	sgen_gchandle_stats_count ();

	mono_gc_printf (sgen_gc_debug_file, "\n%-60s  %10s  %10s  %10s\n", "Class", "Normal", "Weak", "Pinned");
	SGEN_HASH_TABLE_FOREACH (&gchandle_class_hash_table, char *, name, GCHandleClassEntry *, gchandle_entry) {
		mono_gc_printf (sgen_gc_debug_file, "%-60s", name);
		mono_gc_printf (sgen_gc_debug_file, "  %10ld", (long)gchandle_entry->num_handles [HANDLE_NORMAL]);
		size_t weak_handles = gchandle_entry->num_handles [HANDLE_WEAK] +
				gchandle_entry->num_handles [HANDLE_WEAK_TRACK] +
				gchandle_entry->num_handles [HANDLE_WEAK_FIELDS];
		mono_gc_printf (sgen_gc_debug_file, "  %10ld", (long)weak_handles);
		mono_gc_printf (sgen_gc_debug_file, "  %10ld", (long)gchandle_entry->num_handles [HANDLE_PINNED]);
		mono_gc_printf (sgen_gc_debug_file, "\n");
	} SGEN_HASH_TABLE_FOREACH_END;
}
#endif

void
sgen_init_gchandles (void)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("GC handles allocated", MONO_COUNTER_GC | MONO_COUNTER_UINT, (void *)&stat_gc_handles_allocated);
	mono_counters_register ("max GC handles allocated", MONO_COUNTER_GC | MONO_COUNTER_UINT, (void *)&stat_gc_handles_max_allocated);
#endif
}

#endif
