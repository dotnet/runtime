/*
 * sgen-fin-weak-hash.c: Finalizers and weak links.
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *  Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2005-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2011 Xamarin, Inc.
 * Copyright (C) 2012 Xamarin Inc
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

#include "metadata/sgen-gc.h"
#include "metadata/sgen-gray.h"
#include "metadata/sgen-protocol.h"
#include "utils/dtrace.h"

#define ptr_in_nursery sgen_ptr_in_nursery

typedef SgenGrayQueue GrayQueue;

int num_ready_finalizers = 0;
static int no_finalize = 0;

#define DISLINK_OBJECT(l)	(REVEAL_POINTER (*(void**)(l)))
#define DISLINK_TRACK(l)	((~(gulong)(*(void**)(l))) & 1)

/*
 * The finalizable hash has the object as the key, the 
 * disappearing_link hash, has the link address as key.
 *
 * Copyright 2011 Xamarin Inc.
 */

#define TAG_MASK ((mword)0x1)

static inline MonoObject*
tagged_object_get_object (MonoObject *object)
{
	return (MonoObject*)(((mword)object) & ~TAG_MASK);
}

static inline int
tagged_object_get_tag (MonoObject *object)
{
	return ((mword)object) & TAG_MASK;
}

static inline MonoObject*
tagged_object_apply (void *object, int tag_bits)
{
       return (MonoObject*)((mword)object | (mword)tag_bits);
}

static int
tagged_object_hash (MonoObject *o)
{
	return mono_object_hash (tagged_object_get_object (o));
}

static gboolean
tagged_object_equals (MonoObject *a, MonoObject *b)
{
	return tagged_object_get_object (a) == tagged_object_get_object (b);
}

static SgenHashTable minor_finalizable_hash = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_FIN_TABLE, INTERNAL_MEM_FINALIZE_ENTRY, 0, (GHashFunc)tagged_object_hash, (GEqualFunc)tagged_object_equals);
static SgenHashTable major_finalizable_hash = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_FIN_TABLE, INTERNAL_MEM_FINALIZE_ENTRY, 0, (GHashFunc)tagged_object_hash, (GEqualFunc)tagged_object_equals);

static SgenHashTable*
get_finalize_entry_hash_table (int generation)
{
	switch (generation) {
	case GENERATION_NURSERY: return &minor_finalizable_hash;
	case GENERATION_OLD: return &major_finalizable_hash;
	default: g_assert_not_reached ();
	}
}

#define BRIDGE_OBJECT_MARKED 0x1

/* LOCKING: requires that the GC lock is held */
void
sgen_mark_bridge_object (MonoObject *obj)
{
	SgenHashTable *hash_table = get_finalize_entry_hash_table (ptr_in_nursery (obj) ? GENERATION_NURSERY : GENERATION_OLD);

	sgen_hash_table_set_key (hash_table, obj, tagged_object_apply (obj, BRIDGE_OBJECT_MARKED));
}

/* LOCKING: requires that the GC lock is held */
void
sgen_collect_bridge_objects (int generation, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.copy_func;
	GrayQueue *queue = ctx.queue;
	SgenHashTable *hash_table = get_finalize_entry_hash_table (generation);
	MonoObject *object;
	gpointer dummy;
	char *copy;

	if (no_finalize)
		return;

	SGEN_HASH_TABLE_FOREACH (hash_table, object, dummy) {
		int tag = tagged_object_get_tag (object);
		object = tagged_object_get_object (object);

		/* Bridge code told us to ignore this one */
		if (tag == BRIDGE_OBJECT_MARKED)
			continue;

		/* Object is a bridge object and major heap says it's dead  */
		if (major_collector.is_object_live ((char*)object))
			continue;

		/* Nursery says the object is dead. */
		if (!sgen_gc_is_object_ready_for_finalization (object))
			continue;

		if (!sgen_is_bridge_object (object))
			continue;

		copy = (char*)object;
		copy_func ((void**)&copy, queue);

		sgen_bridge_register_finalized_object ((MonoObject*)copy);
		
		if (hash_table == &minor_finalizable_hash && !ptr_in_nursery (copy)) {
			/* remove from the list */
			SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

			/* insert it into the major hash */
			sgen_hash_table_replace (&major_finalizable_hash, tagged_object_apply (copy, tag), NULL, NULL);

			SGEN_LOG (5, "Promoting finalization of object %p (%s) (was at %p) to major table", copy, sgen_safe_name (copy), object);

			continue;
		} else {
			/* update pointer */
			SGEN_LOG (5, "Updating object for finalization: %p (%s) (was at %p)", copy, sgen_safe_name (copy), object);
			SGEN_HASH_TABLE_FOREACH_SET_KEY (tagged_object_apply (copy, tag));
		}
	} SGEN_HASH_TABLE_FOREACH_END;
}


/* LOCKING: requires that the GC lock is held */
void
sgen_finalize_in_range (int generation, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.copy_func;
	GrayQueue *queue = ctx.queue;
	SgenHashTable *hash_table = get_finalize_entry_hash_table (generation);
	MonoObject *object;
	gpointer dummy;

	if (no_finalize)
		return;
	SGEN_HASH_TABLE_FOREACH (hash_table, object, dummy) {
		int tag = tagged_object_get_tag (object);
		object = tagged_object_get_object (object);
		if (!major_collector.is_object_live ((char*)object)) {
			gboolean is_fin_ready = sgen_gc_is_object_ready_for_finalization (object);
			MonoObject *copy = object;
			copy_func ((void**)&copy, queue);
			if (is_fin_ready) {
				/* remove and put in fin_ready_list */
				SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
				num_ready_finalizers++;
				sgen_queue_finalization_entry (copy);
				/* Make it survive */
				SGEN_LOG (5, "Queueing object for finalization: %p (%s) (was at %p) (%d/%d)", copy, sgen_safe_name (copy), object, num_ready_finalizers, sgen_hash_table_num_entries (hash_table));
				continue;
			} else {
				if (hash_table == &minor_finalizable_hash && !ptr_in_nursery (copy)) {
					/* remove from the list */
					SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

					/* insert it into the major hash */
					sgen_hash_table_replace (&major_finalizable_hash, tagged_object_apply (copy, tag), NULL, NULL);

					SGEN_LOG (5, "Promoting finalization of object %p (%s) (was at %p) to major table", copy, sgen_safe_name (copy), object);

					continue;
				} else {
					/* update pointer */
					SGEN_LOG (5, "Updating object for finalization: %p (%s) (was at %p)", copy, sgen_safe_name (copy), object);
					SGEN_HASH_TABLE_FOREACH_SET_KEY (tagged_object_apply (copy, tag));
				}
			}
		}
	} SGEN_HASH_TABLE_FOREACH_END;
}

/* LOCKING: requires that the GC lock is held */
static void
register_for_finalization (MonoObject *obj, void *user_data, int generation)
{
	SgenHashTable *hash_table = get_finalize_entry_hash_table (generation);

	if (no_finalize)
		return;

	g_assert (user_data == NULL || user_data == mono_gc_run_finalize);

	if (user_data) {
		if (sgen_hash_table_replace (hash_table, obj, NULL, NULL))
			SGEN_LOG (5, "Added finalizer for object: %p (%s) (%d) to %s table", obj, obj->vtable->klass->name, hash_table->num_entries, sgen_generation_name (generation));
	} else {
		if (sgen_hash_table_remove (hash_table, obj, NULL))
			SGEN_LOG (5, "Removed finalizer for object: %p (%s) (%d)", obj, obj->vtable->klass->name, hash_table->num_entries);
	}
}

#define STAGE_ENTRY_FREE	0
#define STAGE_ENTRY_BUSY	1
#define STAGE_ENTRY_USED	2

typedef struct {
	gint32 state;
	MonoObject *obj;
	void *user_data;
} StageEntry;

#define NUM_FIN_STAGE_ENTRIES	1024

static volatile gint32 next_fin_stage_entry = 0;
static StageEntry fin_stage_entries [NUM_FIN_STAGE_ENTRIES];

/* LOCKING: requires that the GC lock is held */
static void
process_stage_entries (int num_entries, volatile gint32 *next_entry, StageEntry *entries, void (*process_func) (MonoObject*, void*))
{
	int i;
	int num_registered = 0;
	int num_busy = 0;

	for (i = 0; i < num_entries; ++i) {
		gint32 state = entries [i].state;

		if (state == STAGE_ENTRY_BUSY)
			++num_busy;

		if (state != STAGE_ENTRY_USED ||
				InterlockedCompareExchange (&entries [i].state, STAGE_ENTRY_BUSY, STAGE_ENTRY_USED) != STAGE_ENTRY_USED) {
			continue;
		}

		process_func (entries [i].obj, entries [i].user_data);

		entries [i].obj = NULL;
		entries [i].user_data = NULL;

		mono_memory_write_barrier ();

		entries [i].state = STAGE_ENTRY_FREE;

		++num_registered;
	}

	*next_entry = 0;

	/* g_print ("stage busy %d reg %d\n", num_busy, num_registered); */
}

static gboolean
add_stage_entry (int num_entries, volatile gint32 *next_entry, StageEntry *entries, MonoObject *obj, void *user_data)
{
	gint32 index;

	do {
		do {
			index = *next_entry;
			if (index >= num_entries)
				return FALSE;
		} while (InterlockedCompareExchange (next_entry, index + 1, index) != index);

		/*
		 * We don't need a write barrier here.  *next_entry is just a
		 * help for finding an index, its value is irrelevant for
		 * correctness.
		 */
	} while (entries [index].state != STAGE_ENTRY_FREE ||
			InterlockedCompareExchange (&entries [index].state, STAGE_ENTRY_BUSY, STAGE_ENTRY_FREE) != STAGE_ENTRY_FREE);

	entries [index].obj = obj;
	entries [index].user_data = user_data;

	mono_memory_write_barrier ();

	entries [index].state = STAGE_ENTRY_USED;

	return TRUE;
}

/* LOCKING: requires that the GC lock is held */
static void
process_fin_stage_entry (MonoObject *obj, void *user_data)
{
	if (ptr_in_nursery (obj))
		register_for_finalization (obj, user_data, GENERATION_NURSERY);
	else
		register_for_finalization (obj, user_data, GENERATION_OLD);
}

/* LOCKING: requires that the GC lock is held */
void
sgen_process_fin_stage_entries (void)
{
	process_stage_entries (NUM_FIN_STAGE_ENTRIES, &next_fin_stage_entry, fin_stage_entries, process_fin_stage_entry);
}

void
mono_gc_register_for_finalization (MonoObject *obj, void *user_data)
{
	while (!add_stage_entry (NUM_FIN_STAGE_ENTRIES, &next_fin_stage_entry, fin_stage_entries, obj, user_data)) {
		LOCK_GC;
		sgen_process_fin_stage_entries ();
		UNLOCK_GC;
	}
}

/* LOCKING: requires that the GC lock is held */
static int
finalizers_for_domain (MonoDomain *domain, MonoObject **out_array, int out_size,
	SgenHashTable *hash_table)
{
	MonoObject *object;
	gpointer dummy;
	int count;

	if (no_finalize || !out_size || !out_array)
		return 0;
	count = 0;
	SGEN_HASH_TABLE_FOREACH (hash_table, object, dummy) {
		object = tagged_object_get_object (object);

		if (mono_object_domain (object) == domain) {
			/* remove and put in out_array */
			SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
			out_array [count ++] = object;
			SGEN_LOG (5, "Collecting object for finalization: %p (%s) (%d/%d)", object, sgen_safe_name (object), num_ready_finalizers, sgen_hash_table_num_entries (hash_table));
			if (count == out_size)
				return count;
			continue;
		}
	} SGEN_HASH_TABLE_FOREACH_END;
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
	sgen_process_fin_stage_entries ();
	result = finalizers_for_domain (domain, out_array, out_size, &minor_finalizable_hash);
	if (result < out_size) {
		result += finalizers_for_domain (domain, out_array + result, out_size - result,
			&major_finalizable_hash);
	}
	UNLOCK_GC;

	return result;
}

static SgenHashTable minor_disappearing_link_hash = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_DISLINK_TABLE, INTERNAL_MEM_DISLINK, 0, mono_aligned_addr_hash, NULL);
static SgenHashTable major_disappearing_link_hash = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_DISLINK_TABLE, INTERNAL_MEM_DISLINK, 0, mono_aligned_addr_hash, NULL);

static SgenHashTable*
get_dislink_hash_table (int generation)
{
	switch (generation) {
	case GENERATION_NURSERY: return &minor_disappearing_link_hash;
	case GENERATION_OLD: return &major_disappearing_link_hash;
	default: g_assert_not_reached ();
	}
}

/* LOCKING: assumes the GC lock is held */
static void
add_or_remove_disappearing_link (MonoObject *obj, void **link, int generation)
{
	SgenHashTable *hash_table = get_dislink_hash_table (generation);

	if (!obj) {
		if (sgen_hash_table_remove (hash_table, link, NULL)) {
			SGEN_LOG (5, "Removed dislink %p (%d) from %s table",
					link, hash_table->num_entries, sgen_generation_name (generation));
		}
		return;
	}

	sgen_hash_table_replace (hash_table, link, NULL, NULL);
	SGEN_LOG (5, "Added dislink for object: %p (%s) at %p to %s table",
			obj, obj->vtable->klass->name, link, sgen_generation_name (generation));
}

/* LOCKING: requires that the GC lock is held */
void
sgen_null_link_in_range (int generation, gboolean before_finalization, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.copy_func;
	GrayQueue *queue = ctx.queue;
	void **link;
	gpointer dummy;
	SgenHashTable *hash = get_dislink_hash_table (generation);

	SGEN_HASH_TABLE_FOREACH (hash, link, dummy) {
		char *object;
		gboolean track;

		/*
		We null a weak link before unregistering it, so it's possible that a thread is
		suspended right in between setting the content to null and staging the unregister.

		The rest of this code cannot handle null links as DISLINK_OBJECT (NULL) produces an invalid address.

		We should simply skip the entry as the staged removal will take place during the next GC.
		*/
		if (!*link) {
			SGEN_LOG (5, "Dislink %p was externally nullified", link);
			continue;
		}

		track = DISLINK_TRACK (link);
		/*
		 * Tracked references are processed after
		 * finalization handling whereas standard weak
		 * references are processed before.  If an
		 * object is still not marked after finalization
		 * handling it means that it either doesn't have
		 * a finalizer or the finalizer has already run,
		 * so we must null a tracking reference.
		 */
		if (track != before_finalization) {
			object = DISLINK_OBJECT (link);
			/*
			We should guard against a null object been hidden. This can sometimes happen.
			*/
			if (!object) {
				SGEN_LOG (5, "Dislink %p with a hidden null object", link);
				continue;
			}

			if (!major_collector.is_object_live (object)) {
				if (sgen_gc_is_object_ready_for_finalization (object)) {
					*link = NULL;
					binary_protocol_dislink_update (link, NULL, 0);
					SGEN_LOG (5, "Dislink nullified at %p to GCed object %p", link, object);
					SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
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
						SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

						g_assert (copy);
						*link = HIDE_POINTER (copy, track);
						add_or_remove_disappearing_link ((MonoObject*)copy, link, GENERATION_OLD);
						binary_protocol_dislink_update (link, copy, track);

						SGEN_LOG (5, "Upgraded dislink at %p to major because object %p moved to %p", link, object, copy);

						continue;
					} else {
						*link = HIDE_POINTER (copy, track);
						binary_protocol_dislink_update (link, copy, track);
						SGEN_LOG (5, "Updated dislink at %p to %p", link, DISLINK_OBJECT (link));
					}
				}
			}
		}
	} SGEN_HASH_TABLE_FOREACH_END;
}

/* LOCKING: requires that the GC lock is held */
void
sgen_null_links_for_domain (MonoDomain *domain, int generation)
{
	void **link;
	gpointer dummy;
	SgenHashTable *hash = get_dislink_hash_table (generation);
	SGEN_HASH_TABLE_FOREACH (hash, link, dummy) {
		char *object = DISLINK_OBJECT (link);
		if (*link && object && !((MonoObject*)object)->vtable) {
			gboolean free = TRUE;

			if (*link) {
				*link = NULL;
				binary_protocol_dislink_update (link, NULL, 0);
				free = FALSE;
				/*
				 * This can happen if finalizers are not ran, i.e. Environment.Exit ()
				 * is called from finalizer like in finalizer-abort.cs.
				 */
				SGEN_LOG (5, "Disappearing link %p not freed", link);
			}

			SGEN_HASH_TABLE_FOREACH_REMOVE (free);

			continue;
		}
	} SGEN_HASH_TABLE_FOREACH_END;
}

/* LOCKING: requires that the GC lock is held */
void
sgen_null_links_with_predicate (int generation, WeakLinkAlivePredicateFunc predicate, void *data)
{
	void **link;
	gpointer dummy;
	SgenHashTable *hash = get_dislink_hash_table (generation);
	SGEN_HASH_TABLE_FOREACH (hash, link, dummy) {
		char *object = DISLINK_OBJECT (link);
		mono_bool is_alive;

		if (!*link)
			continue;
		is_alive = predicate ((MonoObject*)object, data);

		if (!is_alive) {
			*link = NULL;
			binary_protocol_dislink_update (link, NULL, 0);
			SGEN_LOG (5, "Dislink nullified by predicate at %p to GCed object %p", link, object);
			SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
			continue;
		}
	} SGEN_HASH_TABLE_FOREACH_END;
}

void
sgen_remove_finalizers_for_domain (MonoDomain *domain, int generation)
{
	SgenHashTable *hash_table = get_finalize_entry_hash_table (generation);
	MonoObject *object;
	gpointer dummy;

	SGEN_HASH_TABLE_FOREACH (hash_table, object, dummy) {
		object = tagged_object_get_object (object);

		if (mono_object_domain (object) == domain) {
			SGEN_LOG (5, "Unregistering finalizer for object: %p (%s)", object, sgen_safe_name (object));

			SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
			continue;
		}
	} SGEN_HASH_TABLE_FOREACH_END;	
}

/* LOCKING: requires that the GC lock is held */
static void
process_dislink_stage_entry (MonoObject *obj, void *_link)
{
	void **link = _link;

	add_or_remove_disappearing_link (NULL, link, GENERATION_NURSERY);
	add_or_remove_disappearing_link (NULL, link, GENERATION_OLD);
	if (obj) {
		if (ptr_in_nursery (obj))
			add_or_remove_disappearing_link (obj, link, GENERATION_NURSERY);
		else
			add_or_remove_disappearing_link (obj, link, GENERATION_OLD);
	}
}

#define NUM_DISLINK_STAGE_ENTRIES	1024

static volatile gint32 next_dislink_stage_entry = 0;
static StageEntry dislink_stage_entries [NUM_DISLINK_STAGE_ENTRIES];

/* LOCKING: requires that the GC lock is held */
void
sgen_process_dislink_stage_entries (void)
{
	process_stage_entries (NUM_DISLINK_STAGE_ENTRIES, &next_dislink_stage_entry, dislink_stage_entries, process_dislink_stage_entry);
}

void
sgen_register_disappearing_link (MonoObject *obj, void **link, gboolean track, gboolean in_gc)
{

#ifdef ENABLE_DTRACE
	if (MONO_GC_WEAK_UPDATE_ENABLED ()) {
		MonoVTable *vt = obj ? (MonoVTable*)SGEN_LOAD_VTABLE (obj) : NULL;
		MONO_GC_WEAK_UPDATE ((mword)link,
				*link ? (mword)DISLINK_OBJECT (link) : (mword)0,
				(mword)obj,
				obj ? (mword)sgen_safe_object_get_size (obj) : (mword)0,
				obj ? vt->klass->name_space : NULL,
				obj ? vt->klass->name : NULL,
				track ? 1 : 0);
	}
#endif

	if (obj)
		*link = HIDE_POINTER (obj, track);
	else
		*link = NULL;

	binary_protocol_dislink_update (link, obj, track);

#if 1
	if (in_gc) {
		process_dislink_stage_entry (obj, link);
	} else {
		while (!add_stage_entry (NUM_DISLINK_STAGE_ENTRIES, &next_dislink_stage_entry, dislink_stage_entries, obj, link)) {
			LOCK_GC;
			sgen_process_dislink_stage_entries ();
			UNLOCK_GC;
		}
	}
#else
	if (!in_gc)
		LOCK_GC;
	process_dislink_stage_entry (obj, link);
	if (!in_gc)
		UNLOCK_GC;
#endif
}

#endif /* HAVE_SGEN_GC */
