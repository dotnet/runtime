/**
 * \file
 * Finalizers and weak links.
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
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-gray.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-pointer-queue.h"
#include "mono/sgen/sgen-client.h"
#include "mono/sgen/gc-internal-agnostic.h"
#include "mono/utils/mono-membar.h"
#include "mono/utils/atomic.h"
#include "mono/utils/unlocked.h"

#define ptr_in_nursery sgen_ptr_in_nursery

typedef SgenGrayQueue GrayQueue;

static int no_finalize = 0;

/*
 * The finalizable hash has the object as the key, the
 * disappearing_link hash, has the link address as key.
 *
 * Copyright 2011 Xamarin Inc.
 */

#define TAG_MASK ((mword)0x1)

static GCObject*
tagged_object_get_object (GCObject *object)
{
	return (GCObject*)(((mword)object) & ~TAG_MASK);
}

static int
tagged_object_get_tag (GCObject *object)
{
	return ((mword)object) & TAG_MASK;
}

static GCObject*
tagged_object_apply (void *object, int tag_bits)
{
       return (GCObject*)((mword)object | (mword)tag_bits);
}

static int
tagged_object_hash (GCObject *o)
{
	return sgen_aligned_addr_hash (tagged_object_get_object (o));
}

static gboolean
tagged_object_equals (GCObject *a, GCObject *b)
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
sgen_mark_bridge_object (GCObject *obj)
{
	SgenHashTable *hash_table = get_finalize_entry_hash_table (ptr_in_nursery (obj) ? GENERATION_NURSERY : GENERATION_OLD);

	sgen_hash_table_set_key (hash_table, obj, tagged_object_apply (obj, BRIDGE_OBJECT_MARKED));
}

/* LOCKING: requires that the GC lock is held */
void
sgen_collect_bridge_objects (int generation, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	GrayQueue *queue = ctx.queue;
	SgenHashTable *hash_table = get_finalize_entry_hash_table (generation);
	GCObject *object;
	gpointer dummy G_GNUC_UNUSED;
	GCObject *copy;
	SgenPointerQueue moved_fin_objects;

	sgen_pointer_queue_init (&moved_fin_objects, INTERNAL_MEM_TEMPORARY);

	if (no_finalize)
		return;

	SGEN_HASH_TABLE_FOREACH (hash_table, GCObject *, object, gpointer, dummy) {
		int tag = tagged_object_get_tag (object);
		object = tagged_object_get_object (object);

		/* Bridge code told us to ignore this one */
		if (tag == BRIDGE_OBJECT_MARKED)
			continue;

		/* Object is a bridge object and major heap says it's dead  */
		if (sgen_major_collector.is_object_live (object))
			continue;

		/* Nursery says the object is dead. */
		if (!sgen_gc_is_object_ready_for_finalization (object))
			continue;

		if (!sgen_client_bridge_is_bridge_object (object))
			continue;

		copy = object;
		copy_func (&copy, queue);

		sgen_client_bridge_register_finalized_object (copy);

		if (hash_table == &minor_finalizable_hash && !ptr_in_nursery (copy)) {
			/* remove from the list */
			SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

			/* insert it into the major hash */
			sgen_hash_table_replace (&major_finalizable_hash, tagged_object_apply (copy, tag), NULL, NULL);

			SGEN_LOG (5, "Promoting finalization of object %p (%s) (was at %p) to major table", copy, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (copy)), object);

			continue;
		} else if (copy != object) {
			/* update pointer */
			SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

			/* register for reinsertion */
			sgen_pointer_queue_add (&moved_fin_objects, tagged_object_apply (copy, tag));

			SGEN_LOG (5, "Updating object for finalization: %p (%s) (was at %p)", copy, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (copy)), object);

			continue;
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	while (!sgen_pointer_queue_is_empty (&moved_fin_objects)) {
		sgen_hash_table_replace (hash_table, sgen_pointer_queue_pop (&moved_fin_objects), NULL, NULL);
	}

	sgen_pointer_queue_free (&moved_fin_objects);
}


/* LOCKING: requires that the GC lock is held */
void
sgen_finalize_in_range (int generation, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	GrayQueue *queue = ctx.queue;
	SgenHashTable *hash_table = get_finalize_entry_hash_table (generation);
	GCObject *object;
	gpointer dummy G_GNUC_UNUSED;
	SgenPointerQueue moved_fin_objects;

	sgen_pointer_queue_init (&moved_fin_objects, INTERNAL_MEM_TEMPORARY);

	if (no_finalize)
		return;
	SGEN_HASH_TABLE_FOREACH (hash_table, GCObject *, object, gpointer, dummy) {
		int tag = tagged_object_get_tag (object);
		object = tagged_object_get_object (object);
		if (!sgen_major_collector.is_object_live (object)) {
			gboolean is_fin_ready = sgen_gc_is_object_ready_for_finalization (object);
			if (is_fin_ready && sgen_client_object_finalize_eagerly (object)) {
				/* just remove an eagerly finalized object */
				SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

				SGEN_LOG (5, "Eagerly finalized object: %p (%s) (was at %p)", object, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (object)), object);
				continue;
			}
			GCObject *copy = object;
			copy_func (&copy, queue);
			if (is_fin_ready) {
				/* remove and put in fin_ready_list */
				SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
				sgen_queue_finalization_entry (copy);
				/* Make it survive */
				SGEN_LOG (5, "Queueing object for finalization: %p (%s) (was at %p) (%d)", copy, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (copy)), object, sgen_hash_table_num_entries (hash_table));
				continue;
			} else {
				if (hash_table == &minor_finalizable_hash && !ptr_in_nursery (copy)) {
					/* remove from the list */
					SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

					/* insert it into the major hash */
					sgen_hash_table_replace (&major_finalizable_hash, tagged_object_apply (copy, tag), NULL, NULL);

					SGEN_LOG (5, "Promoting finalization of object %p (%s) (was at %p) to major table", copy, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (copy)), object);

					continue;
				} else if (copy != object) {
					/* update pointer */
					SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);

					/* register for reinsertion */
					sgen_pointer_queue_add (&moved_fin_objects, tagged_object_apply (copy, tag));

					SGEN_LOG (5, "Updating object for finalization: %p (%s) (was at %p)", copy, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (copy)), object);

					continue;
				}
			}
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	while (!sgen_pointer_queue_is_empty (&moved_fin_objects)) {
		sgen_hash_table_replace (hash_table, sgen_pointer_queue_pop (&moved_fin_objects), NULL, NULL);
	}

	sgen_pointer_queue_free (&moved_fin_objects);
}

/* LOCKING: requires that the GC lock is held */
static MONO_PERMIT (need (sgen_gc_locked)) void
register_for_finalization (GCObject *obj, void *user_data, int generation)
{
	SgenHashTable *hash_table = get_finalize_entry_hash_table (generation);

	if (no_finalize)
		return;

	if (user_data) {
		if (sgen_hash_table_replace (hash_table, obj, NULL, NULL)) {
			GCVTable vt = SGEN_LOAD_VTABLE_UNCHECKED (obj);
			SGEN_LOG (5, "Added finalizer for object: %p (%s) (%d) to %s table", obj, sgen_client_vtable_get_name (vt), hash_table->num_entries, sgen_generation_name (generation));
		}
	} else {
		if (sgen_hash_table_remove (hash_table, obj, NULL)) {
			GCVTable vt = SGEN_LOAD_VTABLE_UNCHECKED (obj);
			SGEN_LOG (5, "Removed finalizer for object: %p (%s) (%d)", obj, sgen_client_vtable_get_name (vt), hash_table->num_entries);
		}
	}
}

/*
 * We're using (mostly) non-locking staging queues for finalizers and weak links to speed
 * up registering them.  Otherwise we'd have to take the GC lock.
 *
 * The queues are arrays of `StageEntry`, plus a `next_entry` index.  Threads add entries to
 * the queue via `add_stage_entry()` in a linear fashion until it fills up, in which case
 * `process_stage_entries()` is called to drain it.  A garbage collection will also drain
 * the queues via the same function.  That implies that `add_stage_entry()`, since it
 * doesn't take a lock, must be able to run concurrently with `process_stage_entries()`,
 * though it doesn't have to make progress while the queue is drained.  In fact, once it
 * detects that the queue is being drained, it blocks until the draining is done.
 *
 * The protocol must guarantee that entries in the queue are causally ordered, otherwise two
 * entries for the same location might get switched, resulting in the earlier one being
 * committed and the later one ignored.
 *
 * `next_entry` is the index of the next entry to be filled, or `-1` if the queue is
 * currently being drained.  Each entry has a state:
 *
 * `STAGE_ENTRY_FREE`: The entry is free.  Its data fields must be `NULL`.
 *
 * `STAGE_ENTRY_BUSY`: The entry is currently being filled in.
 *
 * `STAGE_ENTRY_USED`: The entry is completely filled in and must be processed in the next
 * draining round.
 *
 * `STAGE_ENTRY_INVALID`: The entry was busy during queue draining and therefore
 * invalidated.  Entries that are `BUSY` can obviously not be processed during a drain, but
 * we can't leave them in place because new entries might be inserted before them, including
 * from the same thread, violating causality.  An alternative would be not to reset
 * `next_entry` to `0` after a drain, but to the index of the last `BUSY` entry plus one,
 * but that can potentially waste the whole queue.
 *
 * State transitions:
 *
 * | from    | to      | filler? | drainer? |
 * +---------+---------+---------+----------+
 * | FREE    | BUSY    | X       |          |
 * | BUSY    | FREE    | X       |          |
 * | BUSY    | USED    | X       |          |
 * | BUSY    | INVALID |         | X        |
 * | USED    | FREE    |         | X        |
 * | INVALID | FREE    | X       |          |
 *
 * `next_entry` can be incremented either by the filler thread that set the corresponding
 * entry to `BUSY`, or by another filler thread that's trying to get a `FREE` slot.  If that
 * other thread wasn't allowed to increment, it would block on the first filler thread.
 *
 * An entry's state, once it's set from `FREE` to `BUSY` by a filler thread, can only be
 * changed by that same thread or by the drained.  The drainer can only set a `BUSY` thread
 * to `INVALID`, so it needs to be set to `FREE` again by the original filler thread.
 */

#define STAGE_ENTRY_FREE	0
#define STAGE_ENTRY_BUSY	1
#define STAGE_ENTRY_USED	2
#define STAGE_ENTRY_INVALID	3

typedef struct {
	volatile gint32 state;
	GCObject *obj;
	void *user_data;
} StageEntry;

#define NUM_FIN_STAGE_ENTRIES	1024

static volatile gint32 next_fin_stage_entry = 0;
static StageEntry fin_stage_entries [NUM_FIN_STAGE_ENTRIES];

/*
 * This is used to lock the stage when processing is forced, i.e. when it's triggered by a
 * garbage collection.  In that case, the world is already stopped and there's only one
 * thread operating on the queue.
 */
static void
lock_stage_for_processing (volatile gint32 *next_entry)
{
	*next_entry = -1;
}

/*
 * When processing is triggered by an overflow, we don't want to take the GC lock
 * immediately, and then set `next_index` to `-1`, because another thread might have drained
 * the queue in the mean time.  Instead, we make sure the overflow is still there, we
 * atomically set `next_index`, and only once that happened do we take the GC lock.
 */
static gboolean
try_lock_stage_for_processing (int num_entries, volatile gint32 *next_entry)
{
	gint32 old = *next_entry;
	if (old < num_entries)
		return FALSE;
	return mono_atomic_cas_i32 (next_entry, -1, old) == old;
}

/* LOCKING: requires that the GC lock is held */
static MONO_PERMIT (need (sgen_gc_locked)) void
process_stage_entries (int num_entries, volatile gint32 *next_entry, StageEntry *entries, void (*process_func) (GCObject*, void*, int))
{
	int i;

	/*
	 * This can happen if after setting `next_index` to `-1` in
	 * `try_lock_stage_for_processing()`, a GC was triggered, which then drained the
	 * queue and reset `next_entry`.
	 *
	 * We have the GC lock now, so if it's still `-1`, we can't be interrupted by a GC.
	 */
	if (*next_entry != -1)
		return;

	for (i = 0; i < num_entries; ++i) {
		gint32 state;

	retry:
		state = entries [i].state;

		switch (state) {
		case STAGE_ENTRY_FREE:
		case STAGE_ENTRY_INVALID:
			continue;
		case STAGE_ENTRY_BUSY:
			/* BUSY -> INVALID */
			/*
			 * This must be done atomically, because the filler thread can set
			 * the entry to `USED`, in which case we must process it, so we must
			 * detect that eventuality.
			 */
			if (mono_atomic_cas_i32 (&entries [i].state, STAGE_ENTRY_INVALID, STAGE_ENTRY_BUSY) != STAGE_ENTRY_BUSY)
				goto retry;
			continue;
		case STAGE_ENTRY_USED:
			break;
		default:
			SGEN_ASSERT (0, FALSE, "Invalid stage entry state");
			break;
		}

		/* state is USED */

		process_func (entries [i].obj, entries [i].user_data, i);

		entries [i].obj = NULL;
		entries [i].user_data = NULL;

		mono_memory_write_barrier ();

		/* USED -> FREE */
		/*
		 * This transition only happens here, so we don't have to do it atomically.
		 */
		entries [i].state = STAGE_ENTRY_FREE;
	}

	mono_memory_write_barrier ();

	*next_entry = 0;
}

#ifdef HEAVY_STATISTICS
static gint64 stat_success = 0;
static gint64 stat_overflow_abort = 0;
static gint64 stat_wait_for_processing = 0;
static gint64 stat_increment_other_thread = 0;
static gint64 stat_index_decremented = 0;
static gint64 stat_entry_invalidated = 0;
#endif

static int
add_stage_entry (int num_entries, volatile gint32 *next_entry, StageEntry *entries, GCObject *obj, SGenFinalizationProc user_data)
{
	gint32 index, new_next_entry, old_next_entry;
	gint32 previous_state;

 retry:
	for (;;) {
		index = UnlockedRead (next_entry);
		if (index >= num_entries) {
			HEAVY_STAT (UnlockedIncrement64 (&stat_overflow_abort));
			return -1;
		}
		if (index < 0) {
			/*
			 * Backed-off waiting is way more efficient than even using a
			 * dedicated lock for this.
			 */
			while ((index = UnlockedRead (next_entry)) < 0) {
				/*
				 * This seems like a good value.  Determined by timing
				 * sgen-weakref-stress.exe.
				 */
				mono_thread_info_usleep (200);
				HEAVY_STAT (UnlockedIncrement64 (&stat_wait_for_processing));
			}
			continue;
		}
		/* FREE -> BUSY */
		if (UnlockedRead (&entries [index].state) != STAGE_ENTRY_FREE ||
				mono_atomic_cas_i32 (&entries [index].state, STAGE_ENTRY_BUSY, STAGE_ENTRY_FREE) != STAGE_ENTRY_FREE) {
			/*
			 * If we can't get the entry it must be because another thread got
			 * it first.  We don't want to wait for that thread to increment
			 * `next_entry`, so we try to do it ourselves.  Whether we succeed
			 * or not, we start over.
			 */
			if (UnlockedRead (next_entry) == index) {
				mono_atomic_cas_i32 (next_entry, index + 1, index);
				//g_print ("tried increment for other thread\n");
				HEAVY_STAT (UnlockedIncrement64 (&stat_increment_other_thread));
			}
			continue;
		}
		/* state is BUSY now */
		mono_memory_write_barrier ();
		/*
		 * Incrementing `next_entry` must happen after setting the state to `BUSY`.
		 * If it were the other way around, it would be possible that after a filler
		 * incremented the index, other threads fill up the queue, the queue is
		 * drained, the original filler finally fills in the slot, but `next_entry`
		 * ends up at the start of the queue, and new entries are written in the
		 * queue in front of, not behind, the original filler's entry.
		 *
		 * We don't actually require that the CAS succeeds, but we do require that
		 * the value of `next_entry` is not lower than our index.  Since the drainer
		 * sets it to `-1`, that also takes care of the case that the drainer is
		 * currently running.
		 */
		old_next_entry = mono_atomic_cas_i32 (next_entry, index + 1, index);
		if (old_next_entry < index) {
			/* BUSY -> FREE */
			/* INVALID -> FREE */
			/*
			 * The state might still be `BUSY`, or the drainer could have set it
			 * to `INVALID`.  In either case, there's no point in CASing.  Set
			 * it to `FREE` and start over.
			 */
			UnlockedWrite (&entries [index].state, STAGE_ENTRY_FREE);
			HEAVY_STAT (UnlockedIncrement64 (&stat_index_decremented));
			continue;
		}
		break;
	}

	SGEN_ASSERT (0, index >= 0 && index < num_entries, "Invalid index");

	UnlockedWritePointer ((volatile gpointer*)&entries [index].obj, obj);
	UnlockedWritePointer (&entries [index].user_data, (gpointer)user_data);

	mono_memory_write_barrier ();

	new_next_entry = UnlockedRead (next_entry);
	mono_memory_read_barrier ();
	/* BUSY -> USED */
	/*
	 * A `BUSY` entry will either still be `BUSY` or the drainer will have set it to
	 * `INVALID`.  In the former case, we set it to `USED` and we're finished.  In the
	 * latter case, we reset it to `FREE` and start over.
	 */
	previous_state = mono_atomic_cas_i32 (&entries [index].state, STAGE_ENTRY_USED, STAGE_ENTRY_BUSY);
	if (previous_state == STAGE_ENTRY_BUSY) {
		SGEN_ASSERT (0, new_next_entry >= index || new_next_entry < 0, "Invalid next entry index - as long as we're busy, other thread can only increment or invalidate it");
		HEAVY_STAT (UnlockedIncrement64 (&stat_success));
		return index;
	}

	SGEN_ASSERT (0, previous_state == STAGE_ENTRY_INVALID, "Invalid state transition - other thread can only make busy state invalid");
	UnlockedWritePointer ((volatile gpointer*)&entries [index].obj, NULL);
	UnlockedWritePointer (&entries [index].user_data, NULL);
	mono_memory_write_barrier ();
	/* INVALID -> FREE */
	UnlockedWrite (&entries [index].state, STAGE_ENTRY_FREE);

	HEAVY_STAT (UnlockedIncrement64 (&stat_entry_invalidated));

	goto retry;
}

/* LOCKING: requires that the GC lock is held */
static MONO_PERMIT (need (sgen_gc_locked)) void
process_fin_stage_entry (GCObject *obj, void *user_data, int index)
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
	lock_stage_for_processing (&next_fin_stage_entry);
	process_stage_entries (NUM_FIN_STAGE_ENTRIES, &next_fin_stage_entry, fin_stage_entries, process_fin_stage_entry);
}

void
sgen_object_register_for_finalization (GCObject *obj, SGenFinalizationProc user_data)
{
	while (add_stage_entry (NUM_FIN_STAGE_ENTRIES, &next_fin_stage_entry, fin_stage_entries, obj, user_data) == -1) {
		if (try_lock_stage_for_processing (NUM_FIN_STAGE_ENTRIES, &next_fin_stage_entry)) {
			LOCK_GC;
			process_stage_entries (NUM_FIN_STAGE_ENTRIES, &next_fin_stage_entry, fin_stage_entries, process_fin_stage_entry);
			UNLOCK_GC;
		}
	}
}

/* LOCKING: requires that the GC lock is held */
static MONO_PERMIT (need (sgen_gc_locked)) void
finalize_all (SgenHashTable *hash_table)
{
	GCObject *object;
	gpointer dummy G_GNUC_UNUSED;

	if (no_finalize)
		return;
	SGEN_HASH_TABLE_FOREACH (hash_table, GCObject *, object, gpointer, dummy) {
		object = tagged_object_get_object (object);

		/* remove and put in out_array */
		SGEN_HASH_TABLE_FOREACH_REMOVE (TRUE);
		sgen_queue_finalization_entry (object);
		SGEN_LOG (5, "Enqueuing object for finalization: %p (%s) (%d)", object, sgen_client_vtable_get_name (SGEN_LOAD_VTABLE (object)), sgen_hash_table_num_entries (hash_table));
	} SGEN_HASH_TABLE_FOREACH_END;
}

/**
 * sgen_gather_finalizers_if:
 * @predicate: predicate function
 * @user_data: predicate function data argument
 * @out_array: output array
 * @out_size: size of output array
 *
 * Store inside @out_array up to @out_size objects that match @predicate. Returns the number
 * of stored items. Can be called repeteadly until it returns 0.
 *
 * The items are removed from the finalizer data structure, so the caller is supposed
 * to finalize them.
 *
 * @out_array me be on the stack, or registered as a root, to allow the GC to know the
 * objects are still alive.
 */
void
sgen_finalize_all ()
{
	LOCK_GC;
	sgen_process_fin_stage_entries ();
	finalize_all (&minor_finalizable_hash);
	finalize_all (&major_finalizable_hash);
	UNLOCK_GC;
}

void
sgen_init_fin_weak_hash (void)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("FinWeak Successes", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_success);
	mono_counters_register ("FinWeak Overflow aborts", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_overflow_abort);
	mono_counters_register ("FinWeak Wait for processing", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_wait_for_processing);
	mono_counters_register ("FinWeak Increment other thread", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_increment_other_thread);
	mono_counters_register ("FinWeak Index decremented", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_index_decremented);
	mono_counters_register ("FinWeak Entry invalidated", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_entry_invalidated);
#endif
}

#endif /* HAVE_SGEN_GC */
