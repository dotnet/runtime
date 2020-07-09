/**
 * \file
 * Monitor locking functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright 2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/method-builder.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/w32event.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/atomic.h>
#include <mono/utils/w32api.h>
#include <mono/utils/mono-os-wait.h>
#include "external-only.h"
#include "icall-decl.h"

/*
 * Pull the list of opcodes
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

/*#define LOCK_DEBUG(a) do { a; } while (0)*/
#define LOCK_DEBUG(a)

/*
 * The monitor implementation here is based on
 * http://www.usenix.org/events/jvm01/full_papers/dice/dice.pdf and
 * http://www.research.ibm.com/people/d/dfb/papers/Bacon98Thin.ps
 *
 * The Dice paper describes a technique for saving lock record space
 * by returning records to a free list when they become unused.  That
 * sounds like unnecessary complexity to me, though if it becomes
 * clear that unused lock records are taking up lots of space or we
 * need to shave more time off by avoiding a malloc then we can always
 * implement the free list idea later.  The timeout parameter to
 * try_enter voids some of the assumptions about the reference count
 * field in Dice's implementation too.  In his version, the thread
 * attempting to lock a contended object will block until it succeeds,
 * so the reference count will never be decremented while an object is
 * locked.
 *
 * Bacon's thin locks have a fast path that doesn't need a lock record
 * for the common case of locking an unlocked or shallow-nested
 * object.
 */


typedef struct _MonitorArray MonitorArray;

struct _MonitorArray {
	MonitorArray *next;
	int num_monitors;
	MonoThreadsSync monitors [MONO_ZERO_LEN_ARRAY];
};

#define mono_monitor_allocator_lock() mono_os_mutex_lock (&monitor_mutex)
#define mono_monitor_allocator_unlock() mono_os_mutex_unlock (&monitor_mutex)
static mono_mutex_t monitor_mutex;
static MonoThreadsSync *monitor_freelist;
static MonitorArray *monitor_allocated;
static int array_size = 16;

static MonoBoolean
mono_monitor_try_enter_loop_if_interrupted (MonoObject *obj, guint32 ms,
	MonoBoolean allow_interruption, MonoBoolean *lockTaken, MonoError* error);

/* MonoThreadsSync status helpers */

static guint32
mon_status_get_owner (guint32 status)
{
	return status & OWNER_MASK;
}

static guint32
mon_status_set_owner (guint32 status, guint32 owner)
{
	return (status & ENTRY_COUNT_MASK) | owner;
}

static gint32
mon_status_get_entry_count (guint32 status)
{
	gint32 entry_count = (gint32)((status & ENTRY_COUNT_MASK) >> ENTRY_COUNT_SHIFT);
	gint32 zero = (gint32)(((guint32)ENTRY_COUNT_ZERO) >> ENTRY_COUNT_SHIFT);
	return entry_count - zero;
}

static guint32
mon_status_init_entry_count (guint32 status)
{
	return (status & OWNER_MASK) | ENTRY_COUNT_ZERO;
}

static guint32
mon_status_add_entry_count (guint32 status, int val)
{
	if (val > 0)
		return status + (val << ENTRY_COUNT_SHIFT);
	else
		return status - ((-val) << ENTRY_COUNT_SHIFT);
}

static gboolean
mon_status_have_waiters (guint32 status)
{
	return status & ENTRY_COUNT_WAITERS;
}

/* LockWord helpers */

static MonoThreadsSync*
lock_word_get_inflated_lock (LockWord lw)
{
	lw.lock_word &= (~LOCK_WORD_STATUS_MASK);
	return lw.sync;
}

static gboolean
lock_word_is_inflated (LockWord lw)
{
	return lw.lock_word & LOCK_WORD_INFLATED;
}

static gboolean
lock_word_has_hash (LockWord lw)
{
	return lw.lock_word & LOCK_WORD_HAS_HASH;
}

static LockWord
lock_word_set_has_hash (LockWord lw)
{
	LockWord nlw;
	nlw.lock_word = lw.lock_word | LOCK_WORD_HAS_HASH;
	return nlw;
}

static gboolean
lock_word_is_free (LockWord lw)
{
	return !lw.lock_word;
}

static gboolean
lock_word_is_flat (LockWord lw)
{
	/* Return whether the lock is flat or free */
	return (lw.lock_word & LOCK_WORD_STATUS_MASK) == LOCK_WORD_FLAT;
}

static gint32
lock_word_get_hash (LockWord lw)
{
	return (gint32) (lw.lock_word >> LOCK_WORD_HASH_SHIFT);
}

static gint32
lock_word_get_nest (LockWord lw)
{
	if (lock_word_is_free (lw))
		return 0;
	/* Inword nest count starts from 0 */
	return ((lw.lock_word & LOCK_WORD_NEST_MASK) >> LOCK_WORD_NEST_SHIFT) + 1;
}

static gboolean
lock_word_is_nested (LockWord lw)
{
	return lw.lock_word & LOCK_WORD_NEST_MASK;
}

static gboolean
lock_word_is_max_nest (LockWord lw)
{
	return (lw.lock_word & LOCK_WORD_NEST_MASK) == LOCK_WORD_NEST_MASK;
}

static LockWord
lock_word_increment_nest (LockWord lw)
{
	lw.lock_word += 1 << LOCK_WORD_NEST_SHIFT;
	return lw;
}

static LockWord
lock_word_decrement_nest (LockWord lw)
{
	lw.lock_word -= 1 << LOCK_WORD_NEST_SHIFT;
	return lw;
}

static gint32
lock_word_get_owner (LockWord lw)
{
	return lw.lock_word >> LOCK_WORD_OWNER_SHIFT;
}

static LockWord
lock_word_new_thin_hash (gint32 hash)
{
	LockWord lw;
	lw.lock_word = (guint32)hash;
	lw.lock_word = (lw.lock_word << LOCK_WORD_HASH_SHIFT) | LOCK_WORD_HAS_HASH;
	return lw;
}

static LockWord
lock_word_new_inflated (MonoThreadsSync *mon)
{
	LockWord lw;
	lw.sync = mon;
	lw.lock_word |= LOCK_WORD_INFLATED;
	return lw;
}

static LockWord
lock_word_new_flat (gint32 owner)
{
	LockWord lw;
	lw.lock_word = owner;
	lw.lock_word <<= LOCK_WORD_OWNER_SHIFT;
	return lw;
}

void
mono_monitor_init (void)
{
	mono_os_mutex_init_recursive (&monitor_mutex);
}
 
void
mono_monitor_cleanup (void)
{
	MonoThreadsSync *mon;
	/* MonitorArray *marray, *next = NULL; */

	/*mono_os_mutex_destroy (&monitor_mutex);*/

	/* The monitors on the freelist don't have weak links - mark them */
	for (mon = monitor_freelist; mon; mon = (MonoThreadsSync *)mon->data)
		mon->wait_list = (GSList *)-1;

	/*
	 * FIXME: This still crashes with sgen (async_read.exe)
	 *
	 * In mini_cleanup() we first call mono_runtime_cleanup(), which calls
	 * mono_monitor_cleanup(), which is supposed to free all monitor memory.
	 *
	 * Later in mini_cleanup(), we call mono_domain_free(), which calls
	 * mono_gc_clear_domain(), which frees all weak links associated with objects.
	 * Those weak links reside in the monitor structures, which we've freed earlier.
	 *
	 * Unless we fix this dependency in the shutdown sequence this code has to remain
	 * disabled, or at least the call to g_free().
	 */
	/*
	for (marray = monitor_allocated; marray; marray = next) {
		int i;

		for (i = 0; i < marray->num_monitors; ++i) {
			mon = &marray->monitors [i];
			if (mon->wait_list != (gpointer)-1)
				mono_gc_weak_link_remove (&mon->data);
		}

		next = marray->next;
		g_free (marray);
	}
	*/
}

static int
monitor_is_on_freelist (MonoThreadsSync *mon)
{
	MonitorArray *marray;
	for (marray = monitor_allocated; marray; marray = marray->next) {
		if (mon >= marray->monitors && mon < &marray->monitors [marray->num_monitors])
			return TRUE;
	}
	return FALSE;
}

/**
 * mono_locks_dump:
 * \param include_untaken Whether to list unheld inflated locks.
 * Print a report on stdout of the managed locks currently held by
 * threads. If \p include_untaken is specified, list also inflated locks
 * which are unheld.
 * This is supposed to be used in debuggers like gdb.
 */
void
mono_locks_dump (gboolean include_untaken)
{
	int i;
	int used = 0, on_freelist = 0, to_recycle = 0, total = 0, num_arrays = 0;
	MonoThreadsSync *mon;
	MonitorArray *marray;
	for (mon = monitor_freelist; mon; mon = (MonoThreadsSync *)mon->data)
		on_freelist++;
	for (marray = monitor_allocated; marray; marray = marray->next) {
		total += marray->num_monitors;
		num_arrays++;
		for (i = 0; i < marray->num_monitors; ++i) {
			mon = &marray->monitors [i];
			if (mon->data == NULL) {
				if (i < marray->num_monitors - 1)
					to_recycle++;
			} else {
				if (!monitor_is_on_freelist ((MonoThreadsSync *)mon->data)) {
					MonoObject *holder = (MonoObject *)mono_gchandle_get_target_internal ((MonoGCHandle)mon->data);
					if (mon_status_get_owner (mon->status)) {
						g_print ("Lock %p in object %p held by thread %d, nest level: %d\n",
							mon, holder, mon_status_get_owner (mon->status), mon->nest);
						if (mon->entry_cond)
							g_print ("\tWaiting on condvar %p: %d\n", mon->entry_cond, mon_status_get_entry_count (mon->status));
					} else if (include_untaken) {
						g_print ("Lock %p in object %p untaken\n", mon, holder);
					}
					used++;
				}
			}
		}
	}
	g_print ("Total locks (in %d array(s)): %d, used: %d, on freelist: %d, to recycle: %d\n",
		num_arrays, total, used, on_freelist, to_recycle);
}

/* LOCKING: this is called with monitor_mutex held */
static void 
mon_finalize (MonoThreadsSync *mon)
{
	LOCK_DEBUG (g_message ("%s: Finalizing sync %p", __func__, mon));

	if (mon->entry_cond != NULL) {
		mono_coop_cond_destroy (mon->entry_cond);
		g_free (mon->entry_cond);
		mon->entry_cond = NULL;
	}
	if (mon->entry_mutex != NULL) {
		mono_coop_mutex_destroy (mon->entry_mutex);
		g_free (mon->entry_mutex);
		mon->entry_mutex = NULL;
	}
	/* If this isn't empty then something is seriously broken - it
	 * means a thread is still waiting on the object that owned
	 * this lock, but the object has been finalized.
	 */
	g_assert (mon->wait_list == NULL);

	/* owner and nest are set in mon_new, no need to zero them out */

	mon->data = monitor_freelist;
	monitor_freelist = mon;
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_dec_i32 (&mono_perfcounters->gc_sync_blocks);
#endif
}

/* LOCKING: this is called with monitor_mutex held */
static MonoThreadsSync *
mon_new (gsize id)
{
	MonoThreadsSync *new_;

	if (!monitor_freelist) {
		MonitorArray *marray;
		int i;
		/* see if any sync block has been collected */
		new_ = NULL;
		for (marray = monitor_allocated; marray; marray = marray->next) {
			for (i = 0; i < marray->num_monitors; ++i) {
				if (mono_gchandle_get_target_internal ((MonoGCHandle)marray->monitors [i].data) == NULL) {
					new_ = &marray->monitors [i];
					if (new_->wait_list) {
						/* Orphaned events left by aborted threads */
						while (new_->wait_list) {
							LOCK_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%d): Closing orphaned event %d", mono_thread_info_get_small_id (), new_->wait_list->data));
							mono_w32event_close (new_->wait_list->data);
							new_->wait_list = g_slist_remove (new_->wait_list, new_->wait_list->data);
						}
					}
					mono_gchandle_free_internal ((MonoGCHandle)new_->data);
					new_->data = monitor_freelist;
					monitor_freelist = new_;
				}
			}
			/* small perf tweak to avoid scanning all the blocks */
			if (new_)
				break;
		}
		/* need to allocate a new array of monitors */
		if (!monitor_freelist) {
			MonitorArray *last;
			LOCK_DEBUG (g_message ("%s: allocating more monitors: %d", __func__, array_size));
			marray = (MonitorArray *)g_malloc0 (MONO_SIZEOF_MONO_ARRAY + array_size * sizeof (MonoThreadsSync));
			marray->num_monitors = array_size;
			array_size *= 2;
			/* link into the freelist */
			for (i = 0; i < marray->num_monitors - 1; ++i) {
				marray->monitors [i].data = &marray->monitors [i + 1];
			}
			marray->monitors [i].data = NULL; /* the last one */
			monitor_freelist = &marray->monitors [0];
			/* we happend the marray instead of prepending so that
			 * the collecting loop above will need to scan smaller arrays first
			 */
			if (!monitor_allocated) {
				monitor_allocated = marray;
			} else {
				last = monitor_allocated;
				while (last->next)
					last = last->next;
				last->next = marray;
			}
		}
	}

	new_ = monitor_freelist;
	monitor_freelist = (MonoThreadsSync *)new_->data;

	new_->status = mon_status_set_owner (0, id);
	new_->status = mon_status_init_entry_count (new_->status);
	new_->nest = 1;
	new_->data = NULL;
	
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->gc_sync_blocks);
#endif
	return new_;
}

static MonoThreadsSync*
alloc_mon (MonoObject *obj, gint32 id)
{
	MonoThreadsSync *mon;

	mono_monitor_allocator_lock ();
	mon = mon_new (id);
	mon->data = mono_gchandle_new_weakref_internal (obj, TRUE);
	mono_monitor_allocator_unlock ();

	return mon;
}

static void
discard_mon (MonoThreadsSync *mon)
{
	mono_monitor_allocator_lock ();
	mono_gchandle_free_internal ((MonoGCHandle)mon->data);
	mon_finalize (mon);
	mono_monitor_allocator_unlock ();
}

static void
mono_monitor_inflate_owned (MonoObject *obj, int id)
{
	MonoThreadsSync *mon;
	LockWord nlw, old_lw, tmp_lw;
	guint32 nest;

	old_lw.sync = obj->synchronisation;
	LOCK_DEBUG (g_message ("%s: (%d) Inflating owned lock object %p; LW = %p", __func__, id, obj, old_lw.sync));

	if (lock_word_is_inflated (old_lw)) {
		/* Someone else inflated the lock in the meantime */
		return;
	}

	mon = alloc_mon (obj, id);

	nest = lock_word_get_nest (old_lw);
	mon->nest = nest;

	nlw = lock_word_new_inflated (mon);

	mono_memory_write_barrier ();
	tmp_lw.sync = (MonoThreadsSync *)mono_atomic_cas_ptr ((gpointer*)&obj->synchronisation, nlw.sync, old_lw.sync);
	if (tmp_lw.sync != old_lw.sync) {
		/* Someone else inflated the lock in the meantime */
		discard_mon (mon);
	}
}

static void
mono_monitor_inflate (MonoObject *obj)
{
	MonoThreadsSync *mon;
	LockWord nlw, old_lw;

	LOCK_DEBUG (g_message ("%s: (%d) Inflating lock object %p; LW = %p", __func__, mono_thread_info_get_small_id (), obj, obj->synchronisation));

	mon = alloc_mon (obj, 0);

	nlw = lock_word_new_inflated (mon);

	old_lw.sync = obj->synchronisation;

	for (;;) {
		LockWord tmp_lw;

		if (lock_word_is_inflated (old_lw)) {
			break;
		}
#ifdef HAVE_MOVING_COLLECTOR
		 else if (lock_word_has_hash (old_lw)) {
			mon->hash_code = lock_word_get_hash (old_lw);
			mon->status = mon_status_set_owner (mon->status, 0);
			nlw = lock_word_set_has_hash (nlw);
		}
#endif
		else if (lock_word_is_free (old_lw)) {
			mon->status = mon_status_set_owner (mon->status, 0);
			mon->nest = 1;
		} else {
			/* Lock is flat */
			mon->status = mon_status_set_owner (mon->status, lock_word_get_owner (old_lw));
			mon->nest = lock_word_get_nest (old_lw);
		}
		mono_memory_write_barrier ();
		tmp_lw.sync = (MonoThreadsSync *)mono_atomic_cas_ptr ((gpointer*)&obj->synchronisation, nlw.sync, old_lw.sync);
		if (tmp_lw.sync == old_lw.sync) {
			/* Successfully inflated the lock */
			return;
		}

		old_lw.sync = tmp_lw.sync;
	}

	/* Someone else inflated the lock before us */
	discard_mon (mon);
}

#define MONO_OBJECT_ALIGNMENT_SHIFT	3

int
mono_object_hash_internal (MonoObject* obj)
{
#ifdef HAVE_MOVING_COLLECTOR

	LockWord lw;
	unsigned int hash;
	if (!obj)
		return 0;
	lw.sync = obj->synchronisation;

	LOCK_DEBUG (g_message("%s: (%d) Get hash for object %p; LW = %p", __func__, mono_thread_info_get_small_id (), obj, obj->synchronisation));

	if (lock_word_has_hash (lw)) {
		if (lock_word_is_inflated (lw)) {
			return lock_word_get_inflated_lock (lw)->hash_code;
		} else {
			return lock_word_get_hash (lw);
		}
	}
	/*
	 * while we are inside this function, the GC will keep this object pinned,
	 * since we are in the unmanaged stack. Thanks to this and to the hash
	 * function that depends only on the address, we can ignore the races if
	 * another thread computes the hash at the same time, because it'll end up
	 * with the same value.
	 */
	hash = (GPOINTER_TO_UINT (obj) >> MONO_OBJECT_ALIGNMENT_SHIFT) * 2654435761u;
#if SIZEOF_VOID_P == 4
	/* clear the top bits as they can be discarded */
	hash &= ~(LOCK_WORD_STATUS_MASK << (32 - LOCK_WORD_STATUS_BITS));
#endif
	if (lock_word_is_free (lw)) {
		LockWord old_lw;
		lw = lock_word_new_thin_hash (hash);

		old_lw.sync = (MonoThreadsSync *)mono_atomic_cas_ptr ((gpointer*)&obj->synchronisation, lw.sync, NULL);
		if (old_lw.sync == NULL) {
			return hash;
		}

		if (lock_word_has_hash (old_lw)) {
			/* Done by somebody else */
			return hash;
		}
			
		mono_monitor_inflate (obj);
		lw.sync = obj->synchronisation;
	} else if (lock_word_is_flat (lw)) {
		int const id = mono_thread_info_get_small_id ();
		if (lock_word_get_owner (lw) == id)
			mono_monitor_inflate_owned (obj, id);
		else
			mono_monitor_inflate (obj);
		lw.sync = obj->synchronisation;
	}

	/* At this point, the lock is inflated */
	lock_word_get_inflated_lock (lw)->hash_code = hash;
	lw = lock_word_set_has_hash (lw);
	mono_memory_write_barrier ();
	obj->synchronisation = lw.sync;
	return hash;

#else

/*
 * Wang's address-based hash function:
 *   http://www.concentric.net/~Ttwang/tech/addrhash.htm
 */
	return (GPOINTER_TO_UINT (obj) >> MONO_OBJECT_ALIGNMENT_SHIFT) * 2654435761u;
#endif

}

int
mono_object_hash_icall (MonoObjectHandle obj, MonoError* error)
{
	return mono_object_hash_internal (MONO_HANDLE_RAW (obj));
}

/*
 * mono_object_hash:
 * @obj: an object
 *
 * Calculate a hash code for @obj that is constant while @obj is alive.
 */
int
mono_object_hash (MonoObject* obj)
{
	// FIXME slow?
	MONO_EXTERNAL_ONLY (int, mono_object_hash_internal (obj));
}

static gboolean
mono_monitor_ensure_owned (LockWord lw, guint32 id)
{
	if (lock_word_is_flat (lw)) {
		if (lock_word_get_owner (lw) == id)
			return TRUE;
	} else if (lock_word_is_inflated (lw)) {
		if (mon_status_get_owner (lock_word_get_inflated_lock (lw)->status) == id)
			return TRUE;
	}

	ERROR_DECL (error);
	mono_error_set_synchronization_lock (error, "Object synchronization method was called from an unsynchronized block of code.");
	mono_error_set_pending_exception (error);
	return FALSE;
}

/*
 * When this function is called it has already been established that the
 * current thread owns the monitor.
 */
static void
mono_monitor_exit_inflated (MonoObject *obj)
{
	LockWord lw;
	MonoThreadsSync *mon;
	guint32 nest;

	lw.sync = obj->synchronisation;
	mon = lock_word_get_inflated_lock (lw);

	nest = mon->nest - 1;
	if (nest == 0) {
		guint32 new_status, old_status, tmp_status;

		old_status = mon->status;

		for (;;) {
			new_status = mon_status_set_owner (old_status, 0);
			tmp_status = mono_atomic_cas_i32 ((gint32*)&mon->status, new_status, old_status);
			if (tmp_status == old_status) {
				if (mon_status_have_waiters (old_status)) {
					mono_coop_mutex_lock (mon->entry_mutex);
					mono_coop_cond_signal (mon->entry_cond);
					mono_coop_mutex_unlock (mon->entry_mutex);
				}
				break;
			}
			old_status = tmp_status;
		}
		LOCK_DEBUG (g_message ("%s: (%d) Object %p is now unlocked", __func__, mono_thread_info_get_small_id (), obj));
	
		/* object is now unlocked, leave nest==1 so we don't
		 * need to set it when the lock is reacquired
		 */
	} else {
		LOCK_DEBUG (g_message ("%s: (%d) Object %p is now locked %d times", __func__, mono_thread_info_get_small_id (), obj, nest));
		mon->nest = nest;
	}
}

/*
 * When this function is called it has already been established that the
 * current thread owns the monitor.
 */
static void
mono_monitor_exit_flat (MonoObject *obj, LockWord old_lw)
{
	LockWord new_lw, tmp_lw;
	if (G_UNLIKELY (lock_word_is_nested (old_lw)))
		new_lw = lock_word_decrement_nest (old_lw);
	else
		new_lw.lock_word = 0;

	tmp_lw.sync = (MonoThreadsSync *)mono_atomic_cas_ptr ((gpointer*)&obj->synchronisation, new_lw.sync, old_lw.sync);
	if (old_lw.sync != tmp_lw.sync) {
		/* Someone inflated the lock in the meantime */
		mono_monitor_exit_inflated (obj);
	}

	LOCK_DEBUG (g_message ("%s: (%d) Object %p is now locked %d times; LW = %p", __func__, mono_thread_info_get_small_id (), obj, lock_word_get_nest (new_lw), obj->synchronisation));
}

static gboolean
mon_add_entry_count (MonoThreadsSync *mon, int val)
{
	guint32 old_status, tmp_status, new_status;

	old_status = mon->status;
	for (;;) {
		/* The lock is free, we should retry */
		if (val > 0 && mon_status_get_owner (old_status) == 0)
			return FALSE;
		new_status = mon_status_add_entry_count (old_status, val);
		tmp_status = mono_atomic_cas_i32 ((gint32*)&mon->status, new_status, old_status);
		if (tmp_status == old_status) {
			break;
		}
		old_status = tmp_status;
	}

	return TRUE;
}

static void
mon_init_cond_var (MonoThreadsSync *mon)
{
	if (mon->entry_mutex == NULL) {
		/* Create the mutex */
		MonoCoopMutex *mutex = g_new0 (MonoCoopMutex, 1);
		mono_coop_mutex_init (mutex);
		if (mono_atomic_cas_ptr ((gpointer*)&mon->entry_mutex, mutex, NULL) != NULL) {
			/* Someone else just put a handle here */
			mono_coop_mutex_destroy (mutex);
			g_free (mutex);
		}
	}

	if (mon->entry_cond == NULL) {
		/* Create the condition variable */
		MonoCoopCond *cond = g_new0 (MonoCoopCond, 1);
		mono_coop_cond_init (cond);
		if (mono_atomic_cas_ptr ((gpointer*)&mon->entry_cond, cond, NULL) != NULL) {
			/* Someone else just put a handle here */
			mono_coop_cond_destroy (cond);
			g_free (cond);
		}
	}
}

static void
signal_monitor (gpointer mon_untyped)
{
	MonoThreadsSync *mon = (MonoThreadsSync*) mon_untyped;
	mono_coop_mutex_lock (mon->entry_mutex);
	mono_coop_cond_broadcast (mon->entry_cond);
	mono_coop_mutex_unlock (mon->entry_mutex);
}

#ifdef ENABLE_NETCORE
static gint64 thread_contentions; /* for Monitor.LockContentionCount, otherwise mono_perfcounters struct is used */
#endif

/* If allow_interruption==TRUE, the method will be interrupted if abort or suspend
 * is requested. In this case it returns -1.
 */ 
static gint32
mono_monitor_try_enter_inflated (MonoObject *obj, guint32 ms, gboolean allow_interruption, guint32 id)
{
	LockWord lw;
	MonoThreadsSync *mon;
	gint64 then = 0, now, delta;
	guint32 waitms;
	guint32 new_status, old_status, tmp_status;
	MonoInternalThread *thread;
	gboolean interrupted, timedout;

	LOCK_DEBUG (g_message("%s: (%d) Trying to lock object %p (%d ms)", __func__, id, obj, ms));

	if (G_UNLIKELY (!obj)) {
		ERROR_DECL (error);
		mono_error_set_argument_null (error, "obj", "");
		mono_error_set_pending_exception (error);
		return FALSE;
	}

	lw.sync = obj->synchronisation;
	mon = lock_word_get_inflated_lock (lw);
retry:
	/* This case differs from Dice's case 3 because we don't
	 * deflate locks or cache unused lock records
	 */
	old_status = mon->status;
	if (G_LIKELY (mon_status_get_owner (old_status) == 0)) {
		/* Try to install our ID in the owner field, nest
		* should have been left at 1 by the previous unlock
		* operation
		*/
		new_status = mon_status_set_owner (old_status, id);
		tmp_status = mono_atomic_cas_i32 ((gint32*)&mon->status, new_status, old_status);
		if (G_LIKELY (tmp_status == old_status)) {
			/* Success */
			g_assert (mon->nest == 1);
			return 1;
		} else {
			/* Trumped again! */
			goto retry;
		}
	}

	/* If the object is currently locked by this thread... */
	if (mon_status_get_owner (old_status) == id) {
		mon->nest++;
		return 1;
	}

	/* The object must be locked by someone else... */
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->thread_contentions);
#else
#ifdef ENABLE_NETCORE
	mono_atomic_inc_i64 (&thread_contentions);
#endif
#endif

	/* If ms is 0 we don't block, but just fail straight away */
	if (ms == 0) {
		LOCK_DEBUG (g_message ("%s: (%d) timed out, returning FALSE", __func__, id));
		return 0;
	}

	MONO_PROFILER_RAISE (monitor_contention, (obj));

	/* The slow path begins here. */

	/* Make sure the sync primitives are created */
	mon_init_cond_var (mon);
retry_contended:
	/* a small amount of duplicated code, but it allows us to insert the profiler
	 * callbacks without impacting the fast path: from here on we don't need to go back to the
	 * retry label, but to retry_contended. At this point mon is already installed in the object
	 * header.
	 */
	/* This case differs from Dice's case 3 because we don't
	 * deflate locks or cache unused lock records
	 */
	old_status = mon->status;
	if (G_LIKELY (mon_status_get_owner (old_status) == 0)) {
		/* Try to install our ID in the owner field, nest
		* should have been left at 1 by the previous unlock
		* operation
		*/
		new_status = mon_status_set_owner (old_status, id);
		tmp_status = mono_atomic_cas_i32 ((gint32*)&mon->status, new_status, old_status);
		if (G_LIKELY (tmp_status == old_status)) {
			/* Success */
			g_assert (mon->nest == 1);
			MONO_PROFILER_RAISE (monitor_acquired, (obj));
			return 1;
		}
	}

	/*
	 * We need to register ourselves as waiting.
	 *
	 * If we set the entry count, we are guaranteed to be notified when the monitor
	 * is released. Because we have the entry_mutex lock taken during this time and
	 * wait on the condvar, we are guaranteed not to miss the wakeup.
	 *
	 * If the owner of the monitor releases the lock and there are no registered
	 * waiters at that point, a new entering thread is guaranteed to check first
	 * that the monitor is free.
	 */
	mono_coop_mutex_lock (mon->entry_mutex);
	if (!mon_add_entry_count (mon, 1)) {
		mono_coop_mutex_unlock (mon->entry_mutex);
		goto retry_contended;
	}

	if (ms != MONO_INFINITE_WAIT) {
		then = mono_msec_ticks ();
	}
	waitms = ms;
	
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->thread_queue_len);
	mono_atomic_inc_i32 (&mono_perfcounters->thread_queue_max);
#endif
	thread = mono_thread_internal_current ();

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);
	mono_thread_info_install_interrupt (signal_monitor, mon, &interrupted);
	if (!interrupted) {
		timedout = FALSE;
		if (ms == MONO_INFINITE_WAIT) {
			mono_coop_cond_wait (mon->entry_cond, mon->entry_mutex);
		} else {
			if (mono_coop_cond_timedwait (mon->entry_cond, mon->entry_mutex, waitms) == -1)
				timedout = TRUE;
		}
		mono_thread_info_uninstall_interrupt (&interrupted);
	}
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	mon_add_entry_count (mon, -1);
	mono_coop_mutex_unlock (mon->entry_mutex);

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_dec_i32 (&mono_perfcounters->thread_queue_len);
#endif

	if (timedout || (interrupted && allow_interruption)) {
		/* we're done */
	} else {
		/* 
		 * We have to obey a stop/suspend request even if 
		 * allow_interruption is FALSE to avoid hangs at shutdown.
		 * FIXME Handle abort protected blocks
		 */
		if ((interrupted && !mono_thread_test_state (mono_thread_internal_current (), ThreadState_SuspendRequested | ThreadState_AbortRequested)) || !interrupted) {
			/* We were interrupted (and allow_interruption is FALSE) or we were signaled */
			if (ms != MONO_INFINITE_WAIT) {
				now = mono_msec_ticks ();

				/* it should not overflow before ~30k years */
				g_assert (now >= then);

				delta = now - then;
				if (delta >= ms) {
					ms = 0;
				} else {
					ms -= delta;
				}
			}
			/* retry from the top */
			goto retry_contended;
		}
	}

	MONO_PROFILER_RAISE (monitor_failed, (obj));

	if (interrupted) {
		LOCK_DEBUG (g_message ("%s: (%d) interrupted waiting, returning -1", __func__, id));
		return -1;
	} else if (timedout) {
		LOCK_DEBUG (g_message ("%s: (%d) timed out waiting, returning FALSE", __func__, id));
		return 0;
	} else {
		g_assert_not_reached ();
		return 0;
	}
}

/*
 * If allow_interruption == TRUE, the method will be interrupted if abort or suspend
 * is requested. In this case it returns -1.
 */
static gint32
mono_monitor_try_enter_internal (MonoObject *obj, guint32 ms, gboolean allow_interruption)
{
	LockWord lw;
	int const id = mono_thread_info_get_small_id ();

	LOCK_DEBUG (g_message("%s: (%d) Trying to lock object %p (%d ms)", __func__, id, obj, ms));

	lw.sync = obj->synchronisation;

	if (G_LIKELY (lock_word_is_free (lw))) {
		LockWord nlw = lock_word_new_flat (id);
		if (mono_atomic_cas_ptr ((gpointer*)&obj->synchronisation, nlw.sync, NULL) == NULL) {
			return 1;
		} else {
			/* Someone acquired it in the meantime or put a hash */
			mono_monitor_inflate (obj);
			return mono_monitor_try_enter_inflated (obj, ms, allow_interruption, id);
		}
	} else if (lock_word_is_inflated (lw)) {
		return mono_monitor_try_enter_inflated (obj, ms, allow_interruption, id);
	} else if (lock_word_is_flat (lw)) {
		if (lock_word_get_owner (lw) == id) {
			if (lock_word_is_max_nest (lw)) {
				mono_monitor_inflate_owned (obj, id);
				return mono_monitor_try_enter_inflated (obj, ms, allow_interruption, id);
			} else {
				LockWord nlw, old_lw;
				nlw = lock_word_increment_nest (lw);
				old_lw.sync = (MonoThreadsSync *)mono_atomic_cas_ptr ((gpointer*)&obj->synchronisation, nlw.sync, lw.sync);
				if (old_lw.sync != lw.sync) {
					/* Someone else inflated it in the meantime */
					g_assert (lock_word_is_inflated (old_lw));
					return mono_monitor_try_enter_inflated (obj, ms, allow_interruption, id);
				}
				return 1;
			}
		} else {
			mono_monitor_inflate (obj);
			return mono_monitor_try_enter_inflated (obj, ms, allow_interruption, id);
		}
	} else if (lock_word_has_hash (lw)) {
		mono_monitor_inflate (obj);
		return mono_monitor_try_enter_inflated (obj, ms, allow_interruption, id);
	}

	g_assert_not_reached ();
	return -1;
}

/* This is an icall */
MonoBoolean
mono_monitor_enter_internal (MonoObject *obj)
{
	const int timeout_milliseconds = MONO_INFINITE_WAIT;
	const gboolean allow_interruption = TRUE;
	MonoError * const error = NULL;
	MonoBoolean lock_taken;

	return mono_monitor_try_enter_loop_if_interrupted (obj, timeout_milliseconds, allow_interruption, &lock_taken, error);
}

/**
 * mono_monitor_enter:
 */
gboolean
mono_monitor_enter (MonoObject *obj)
{
	// FIXME slow?
	MONO_EXTERNAL_ONLY (gboolean, mono_monitor_enter_internal (obj));
}

/* Called from JITted code so we return guint32 instead of gboolean */
guint32
mono_monitor_enter_fast (MonoObject *obj)
{
	if (G_UNLIKELY (!obj)) {
		/* don't set pending exn on the fast path, just return
		 * FALSE and let the slow path take care of it. */
		return FALSE;
	}
	return mono_monitor_try_enter_internal (obj, 0, FALSE) == 1;
}

/**
 * mono_monitor_try_enter:
 */
gboolean
mono_monitor_try_enter (MonoObject *obj, guint32 ms)
{
	if (G_UNLIKELY (!obj)) {
		ERROR_DECL (error);
		mono_error_set_argument_null (error, "obj", "");
		mono_error_set_pending_exception (error);
		return FALSE;
	}
	MONO_EXTERNAL_ONLY (gboolean, mono_monitor_try_enter_internal (obj, ms, FALSE) == 1);
}

void
mono_monitor_exit_internal (MonoObject *obj)
{
	LockWord lw;
	
	LOCK_DEBUG (g_message ("%s: (%d) Unlocking %p", __func__, mono_thread_info_get_small_id (), obj));

	if (G_UNLIKELY (!obj)) {
		ERROR_DECL (error);
		mono_error_set_argument_null (error, "obj", "");
		mono_error_set_pending_exception (error);
		return;
	}

	lw.sync = obj->synchronisation;

	if (!mono_monitor_ensure_owned (lw, mono_thread_info_get_small_id ()))
		return;

	if (G_UNLIKELY (lock_word_is_inflated (lw)))
		mono_monitor_exit_inflated (obj);
	else
		mono_monitor_exit_flat (obj, lw);
}

void
mono_monitor_exit_icall (MonoObjectHandle obj, MonoError* error)
{
	mono_monitor_exit_internal (MONO_HANDLE_RAW (obj));
}

/**
 * mono_monitor_exit:
 */
void
mono_monitor_exit (MonoObject *obj)
{
	MONO_EXTERNAL_ONLY_VOID (mono_monitor_exit_internal (obj));
}

MonoGCHandle
mono_monitor_get_object_monitor_gchandle (MonoObject *object)
{
	LockWord lw;

	lw.sync = object->synchronisation;

	if (lock_word_is_inflated (lw)) {
		MonoThreadsSync *mon = lock_word_get_inflated_lock (lw);
		return (MonoGCHandle)mon->data;
	}
	return NULL;
}

/*
 * mono_monitor_threads_sync_member_offset:
 * @status_offset: returns size and offset of the "status" member
 * @nest_offset: returns size and offset of the "nest" member
 *
 * Returns the offsets and sizes of two members of the
 * MonoThreadsSync struct.  The Monitor ASM fastpaths need this.
 */
void
mono_monitor_threads_sync_members_offset (int *status_offset, int *nest_offset)
{
	MonoThreadsSync ts;

#define ENCODE_OFF_SIZE(o,s)	(((o) << 8) | ((s) & 0xff))

	*status_offset = ENCODE_OFF_SIZE (MONO_STRUCT_OFFSET (MonoThreadsSync, status), sizeof (ts.status));
	*nest_offset = ENCODE_OFF_SIZE (MONO_STRUCT_OFFSET (MonoThreadsSync, nest), sizeof (ts.nest));
}

static MonoBoolean
mono_monitor_try_enter_loop_if_interrupted (MonoObject *obj, guint32 ms,
	MonoBoolean allow_interruption, MonoBoolean *lockTaken, MonoError* error)
{
	// Return value and lockTaken are equivalent, except, to preserve prior behavior,
	// *lockTaken is not always written to, i.e. in the error paths.
	//
	// Some callers have lockTaken and only use it, some only have the return value.

	if (G_UNLIKELY (!obj)) {
		if (error) {
			mono_error_set_argument_null (error, "obj", "");
		} else {
			ERROR_DECL (error);
			mono_error_set_argument_null (error, "obj", "");
			mono_error_set_pending_exception (error);
		}
		return FALSE;
	}

	gint32 res;

	/*
	 * An inquisitive mind could ask what's the deal with this loop.
	 * It exists to deal with interrupting a monitor enter that happened within an abort-protected block, like a .cctor.
	 *
	 * The thread will be set with a pending abort and the wait might even be interrupted. Either way, once we call mono_thread_interruption_checkpoint,
	 * it will return NULL meaning we can't be aborted right now. Once that happens we switch to non-alertable.
	 */
	do {
		res = mono_monitor_try_enter_internal (obj, ms, allow_interruption);
		if (res == -1) {
			// The wait was interrupted and the monitor was not acquired.
			MonoException *exc;
			HANDLE_FUNCTION_ENTER ();
			exc = mono_thread_interruption_checkpoint ();
			if (exc) {
				MONO_HANDLE_NEW (MonoException, exc);
				if (error)
					mono_error_set_exception_instance (error, exc);
				else
					mono_set_pending_exception (exc);
			}
			HANDLE_FUNCTION_RETURN ();
			if (exc)
				return FALSE;
			// The interrupt was a false positive. Ignore it from now on.
			// This feels like a hack.
			// threads.c should give us less confusing directions.
			allow_interruption = FALSE;
		}
	} while (res == -1);

	/*It's safe to do it from here since interruption would happen only on the wrapper.*/
	*lockTaken = res == 1;
	return res;
}

#ifdef ENABLE_NETCORE
void
ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var (MonoObjectHandle obj, guint32 ms, MonoBoolean allow_interruption, MonoBoolean* lockTaken, MonoError* error)
{
	mono_monitor_try_enter_loop_if_interrupted (MONO_HANDLE_RAW (obj), ms, allow_interruption, lockTaken, error);
}
#else
void
ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var (MonoObjectHandle obj, guint32 ms, MonoBoolean* lockTaken, MonoError* error)
{
	mono_monitor_try_enter_loop_if_interrupted (MONO_HANDLE_RAW (obj), ms, TRUE, lockTaken, error);
}
#endif

/**
 * mono_monitor_enter_v4:
 */
void
mono_monitor_enter_v4 (MonoObject *obj, char *lock_taken)
{
	g_static_assert (sizeof (MonoBoolean) == 1);
	mono_monitor_enter_v4_internal (obj, (MonoBoolean*)lock_taken);
}

/* Called from JITted code */
void
mono_monitor_enter_v4_internal (MonoObject *obj, MonoBoolean *lock_taken)
{
	if (*lock_taken == 1) {
		ERROR_DECL (error);
		mono_error_set_argument (error, "lockTaken", "lockTaken is already true");
		mono_error_set_pending_exception (error);
		return;
	}
	mono_monitor_try_enter_loop_if_interrupted (obj, MONO_INFINITE_WAIT, FALSE, lock_taken, NULL);
}

/*
 * mono_monitor_enter_v4_fast:
 *
 *   Same as mono_monitor_enter_v4, but return immediately if the
 * monitor cannot be acquired.
 * Returns TRUE if the lock was acquired, FALSE otherwise.
 * Called from JITted code so we return guint32 instead of gboolean.
 */
guint32
mono_monitor_enter_v4_fast (MonoObject *obj, MonoBoolean *lock_taken)
{
	if (*lock_taken == 1 || G_UNLIKELY (!obj))
		return FALSE;

	gboolean const res = mono_monitor_try_enter_internal (obj, 0, TRUE) == 1;
	*lock_taken = (MonoBoolean)res;
	return (guint32)res;
}

MonoBoolean
ves_icall_System_Threading_Monitor_Monitor_test_owner (MonoObjectHandle obj_handle, MonoError* error)
{
	MonoObject* const obj = MONO_HANDLE_RAW (obj_handle);

	LockWord lw;

	LOCK_DEBUG (g_message ("%s: Testing if %p is owned by thread %d", __func__, obj, mono_thread_info_get_small_id()));

	lw.sync = obj->synchronisation;

	if (lock_word_is_flat (lw)) {
		return lock_word_get_owner (lw) == mono_thread_info_get_small_id ();
	} else if (lock_word_is_inflated (lw)) {
		return mon_status_get_owner (lock_word_get_inflated_lock (lw)->status) == mono_thread_info_get_small_id ();
	}
	
	return FALSE;
}

MonoBoolean
ves_icall_System_Threading_Monitor_Monitor_test_synchronised (MonoObjectHandle obj_handle, MonoError* error)
{
	MonoObject* const obj = MONO_HANDLE_RAW (obj_handle);

	LockWord lw;

	LOCK_DEBUG (g_message("%s: (%d) Testing if %p is owned by any thread", __func__, mono_thread_info_get_small_id (), obj));

	lw.sync = obj->synchronisation;

	if (lock_word_is_flat (lw)) {
		return !lock_word_is_free (lw);
	} else if (lock_word_is_inflated (lw)) {
		return mon_status_get_owner (lock_word_get_inflated_lock (lw)->status) != 0;
	}

	return FALSE;
}

/* All wait list manipulation in the pulse, pulseall and wait
 * functions happens while the monitor lock is held, so we don't need
 * any extra struct locking
 */

static void
mono_monitor_pulse (MonoObject *obj, const char *func, gboolean all)
{
	int const id = mono_thread_info_get_small_id ();
	LockWord lw;
	MonoThreadsSync *mon;

	LOCK_DEBUG (g_message ("%s: (%d) Pulsing %p", func, id, obj));
	
	lw.sync = obj->synchronisation;

	if (!mono_monitor_ensure_owned (lw, id))
		return;

	if (!lock_word_is_inflated (lw)) {
		/* No threads waiting. A wait would have inflated the lock */
		return;
	}

	mon = lock_word_get_inflated_lock (lw);

	LOCK_DEBUG (g_message ("%s: (%d) %d threads waiting", func, id, g_slist_length (mon->wait_list)));

	do {
		if (mon->wait_list != NULL) {
			LOCK_DEBUG (g_message ("%s: (%d) signalling and dequeuing handle %p", func, id, mon->wait_list->data));

			mono_w32event_set (mon->wait_list->data);
			mon->wait_list = g_slist_remove (mon->wait_list, mon->wait_list->data);
		}
	} while (all && mon->wait_list);
}

void
ves_icall_System_Threading_Monitor_Monitor_pulse (MonoObjectHandle obj, MonoError* error)
{
	mono_monitor_pulse (MONO_HANDLE_RAW (obj), __func__, FALSE);
}

void
ves_icall_System_Threading_Monitor_Monitor_pulse_all (MonoObjectHandle obj, MonoError* error)
{
	mono_monitor_pulse (MONO_HANDLE_RAW (obj), __func__, TRUE);
}

static MonoBoolean
mono_monitor_wait (MonoObjectHandle obj_handle, guint32 ms, MonoBoolean allow_interruption, MonoError* error)
{
	MonoObject* const obj = MONO_HANDLE_RAW (obj_handle);

	LockWord lw;
	MonoThreadsSync *mon;
	HANDLE event;
	guint32 nest;
	MonoW32HandleWaitRet ret;
	gboolean success = FALSE;
	gint32 regain;
	MonoInternalThread *thread = mono_thread_internal_current ();
	int const id = mono_thread_info_get_small_id ();

	LOCK_DEBUG (g_message ("%s: (%d) Trying to wait for %p with timeout %dms", __func__, id, obj, ms));

	lw.sync = obj->synchronisation;

	if (!mono_monitor_ensure_owned (lw, id))
		return FALSE;

	if (!lock_word_is_inflated (lw)) {
		mono_monitor_inflate_owned (obj, id);
		lw.sync = obj->synchronisation;
	}

	mon = lock_word_get_inflated_lock (lw);

	/* Do this WaitSleepJoin check before creating the event handle */
	if (mono_thread_current_check_pending_interrupt ())
		return FALSE;
	
	event = mono_w32event_create (FALSE, FALSE);
	if (event == NULL) {
		ERROR_DECL (error);
		mono_error_set_synchronization_lock (error, "Failed to set up wait event");
		mono_error_set_pending_exception (error);
		return FALSE;
	}

#ifdef DISABLE_THREADS
	if (ms == MONO_INFINITE_WAIT) {
		mono_error_set_platform_not_supported (error, "Cannot wait on monitors on this runtime.");
		return FALSE;
	}
#endif
	
	LOCK_DEBUG (g_message ("%s: (%d) queuing handle %p", __func__, id, event));

	/* This looks superfluous */
	if (allow_interruption && mono_thread_current_check_pending_interrupt ()) {
		mono_w32event_close (event);
		return FALSE;
	}
	
	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);

	mon->wait_list = g_slist_append (mon->wait_list, event);
	
	/* Save the nest count, and release the lock */
	nest = mon->nest;
	mon->nest = 1;
	mono_memory_write_barrier ();
	mono_monitor_exit_inflated (obj);

	LOCK_DEBUG (g_message ("%s: (%d) Unlocked %p lock %p", __func__, id, obj, mon));

	/* There's no race between unlocking mon and waiting for the
	 * event, because auto reset events are sticky, and this event
	 * is private to this thread.  Therefore even if the event was
	 * signalled before we wait, we still succeed.
	 */
	ret = mono_w32handle_wait_one (event, ms, TRUE);

	/* Reset the thread state fairly early, so we don't have to worry
	 * about the monitor error checking
	 */
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);

	/* Regain the lock with the previous nest count */
	do {
		regain = mono_monitor_try_enter_inflated (obj, MONO_INFINITE_WAIT, allow_interruption, id);
		/* We must regain the lock before handling interruption requests */
	} while (regain == -1);

	g_assert (regain == 1);

	mon->nest = nest;

	LOCK_DEBUG (g_message ("%s: (%d) Regained %p lock %p", __func__, id, obj, mon));

	if (ret == MONO_W32HANDLE_WAIT_RET_TIMEOUT) {
		/* Poll the event again, just in case it was signalled
		 * while we were trying to regain the monitor lock
		 */
		ret = mono_w32handle_wait_one (event, 0, FALSE);
	}

	/* Pulse will have popped our event from the queue if it signalled
	 * us, so we only do it here if the wait timed out.
	 *
	 * This avoids a race condition where the thread holding the
	 * lock can Pulse several times before the WaitForSingleObject
	 * returns.  If we popped the queue here then this event might
	 * be signalled more than once, thereby starving another
	 * thread.
	 */
	
	if (ret == MONO_W32HANDLE_WAIT_RET_SUCCESS_0) {
		LOCK_DEBUG (g_message ("%s: (%d) Success", __func__, id));
		success = TRUE;
	} else {
		LOCK_DEBUG (g_message ("%s: (%d) Wait failed, dequeuing handle %p", __func__, id, event));
		/* No pulse, so we have to remove ourself from the
		 * wait queue
		 */
		mon->wait_list = g_slist_remove (mon->wait_list, event);
	}
	mono_w32event_close (event);
	
	return success;
}

#ifdef ENABLE_NETCORE
MonoBoolean
ves_icall_System_Threading_Monitor_Monitor_wait (MonoObjectHandle obj_handle, guint32 ms, MonoBoolean allow_interruption, MonoError* error)
{
	return mono_monitor_wait (obj_handle, ms, allow_interruption, error);
}
#else
MonoBoolean
ves_icall_System_Threading_Monitor_Monitor_wait (MonoObjectHandle obj_handle, guint32 ms, MonoError* error)
{
	return mono_monitor_wait (obj_handle, ms, TRUE, error);
}
#endif
void
ves_icall_System_Threading_Monitor_Monitor_Enter (MonoObjectHandle obj, MonoError* error)
{
	mono_monitor_enter_internal (MONO_HANDLE_RAW (obj));
}

#ifdef ENABLE_NETCORE
gint64
ves_icall_System_Threading_Monitor_Monitor_LockContentionCount (void)
{
#ifndef DISABLE_PERFCOUNTERS
	return mono_perfcounters->thread_contentions;
#else
	return thread_contentions;
#endif
}
#endif
