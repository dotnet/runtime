/*
 * monitor.c:  Monitor locking functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright 2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threads.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/method-builder.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/atomic.h>

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

#define mono_monitor_allocator_lock() mono_mutex_lock (&monitor_mutex)
#define mono_monitor_allocator_unlock() mono_mutex_unlock (&monitor_mutex)
static mono_mutex_t monitor_mutex;
static MonoThreadsSync *monitor_freelist;
static MonitorArray *monitor_allocated;
static int array_size = 16;

/* MonoThreadsSync status helpers */

static inline guint32
mon_status_get_owner (guint32 status)
{
	return status & OWNER_MASK;
}

static inline guint32
mon_status_set_owner (guint32 status, guint32 owner)
{
	return (status & ENTRY_COUNT_MASK) | owner;
}

static inline gint32
mon_status_get_entry_count (guint32 status)
{
	gint32 entry_count = (gint32)((status & ENTRY_COUNT_MASK) >> ENTRY_COUNT_SHIFT);
	gint32 zero = (gint32)(((guint32)ENTRY_COUNT_ZERO) >> ENTRY_COUNT_SHIFT);
	return entry_count - zero;
}

static inline guint32
mon_status_init_entry_count (guint32 status)
{
	return (status & OWNER_MASK) | ENTRY_COUNT_ZERO;
}

static inline guint32
mon_status_increment_entry_count (guint32 status)
{
	return status + (1 << ENTRY_COUNT_SHIFT);
}

static inline guint32
mon_status_decrement_entry_count (guint32 status)
{
	return status - (1 << ENTRY_COUNT_SHIFT);
}

static inline gboolean
mon_status_have_waiters (guint32 status)
{
	return status & ENTRY_COUNT_WAITERS;
}

/* LockWord helpers */

static inline MonoThreadsSync*
lock_word_get_inflated_lock (LockWord lw)
{
	lw.lock_word &= (~LOCK_WORD_STATUS_MASK);
	return lw.sync;
}

static inline gboolean
lock_word_is_inflated (LockWord lw)
{
	return lw.lock_word & LOCK_WORD_INFLATED;
}

static inline gboolean
lock_word_has_hash (LockWord lw)
{
	return lw.lock_word & LOCK_WORD_HAS_HASH;
}

static inline LockWord
lock_word_set_has_hash (LockWord lw)
{
	LockWord nlw;
	nlw.lock_word = lw.lock_word | LOCK_WORD_HAS_HASH;
	return nlw;
}

static inline gboolean
lock_word_is_free (LockWord lw)
{
	return !lw.lock_word;
}

static inline gboolean
lock_word_is_flat (LockWord lw)
{
	/* Return whether the lock is flat or free */
	return (lw.lock_word & LOCK_WORD_STATUS_MASK) == LOCK_WORD_FLAT;
}

static inline gint32
lock_word_get_hash (LockWord lw)
{
	return (gint32) (lw.lock_word >> LOCK_WORD_HASH_SHIFT);
}

static inline gint32
lock_word_get_nest (LockWord lw)
{
	if (lock_word_is_free (lw))
		return 0;
	/* Inword nest count starts from 0 */
	return ((lw.lock_word & LOCK_WORD_NEST_MASK) >> LOCK_WORD_NEST_SHIFT) + 1;
}

static inline gboolean
lock_word_is_nested (LockWord lw)
{
	return lw.lock_word & LOCK_WORD_NEST_MASK;
}

static inline gboolean
lock_word_is_max_nest (LockWord lw)
{
	return (lw.lock_word & LOCK_WORD_NEST_MASK) == LOCK_WORD_NEST_MASK;
}

static inline LockWord
lock_word_increment_nest (LockWord lw)
{
	lw.lock_word += 1 << LOCK_WORD_NEST_SHIFT;
	return lw;
}

static inline LockWord
lock_word_decrement_nest (LockWord lw)
{
	lw.lock_word -= 1 << LOCK_WORD_NEST_SHIFT;
	return lw;
}

static inline gint32
lock_word_get_owner (LockWord lw)
{
	return lw.lock_word >> LOCK_WORD_OWNER_SHIFT;
}

static inline LockWord
lock_word_new_thin_hash (gint32 hash)
{
	LockWord lw;
	lw.lock_word = (guint32)hash;
	lw.lock_word = (lw.lock_word << LOCK_WORD_HASH_SHIFT) | LOCK_WORD_HAS_HASH;
	return lw;
}

static inline LockWord
lock_word_new_inflated (MonoThreadsSync *mon)
{
	LockWord lw;
	lw.sync = mon;
	lw.lock_word |= LOCK_WORD_INFLATED;
	return lw;
}

static inline LockWord
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
	mono_mutex_init_recursive (&monitor_mutex);
}
 
void
mono_monitor_cleanup (void)
{
	MonoThreadsSync *mon;
	/* MonitorArray *marray, *next = NULL; */

	/*mono_mutex_destroy (&monitor_mutex);*/

	/* The monitors on the freelist don't have weak links - mark them */
	for (mon = monitor_freelist; mon; mon = mon->data)
		mon->wait_list = (gpointer)-1;

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
 * @include_untaken:
 *
 * Print a report on stdout of the managed locks currently held by
 * threads. If @include_untaken is specified, list also inflated locks
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
	for (mon = monitor_freelist; mon; mon = mon->data)
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
				if (!monitor_is_on_freelist (mon->data)) {
					MonoObject *holder = mono_gc_weak_link_get (&mon->data);
					if (mon_status_get_owner (mon->status)) {
						g_print ("Lock %p in object %p held by thread %d, nest level: %d\n",
							mon, holder, mon_status_get_owner (mon->status), mon->nest);
						if (mon->entry_sem)
							g_print ("\tWaiting on semaphore %p: %d\n", mon->entry_sem, mon_status_get_entry_count (mon->status));
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

	if (mon->entry_sem != NULL) {
		CloseHandle (mon->entry_sem);
		mon->entry_sem = NULL;
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
	mono_perfcounters->gc_sync_blocks--;
#endif
}

/* LOCKING: this is called with monitor_mutex held */
static MonoThreadsSync *
mon_new (gsize id)
{
	MonoThreadsSync *new;

	if (!monitor_freelist) {
		MonitorArray *marray;
		int i;
		/* see if any sync block has been collected */
		new = NULL;
		for (marray = monitor_allocated; marray; marray = marray->next) {
			for (i = 0; i < marray->num_monitors; ++i) {
				if (marray->monitors [i].data == NULL) {
					new = &marray->monitors [i];
					if (new->wait_list) {
						/* Orphaned events left by aborted threads */
						while (new->wait_list) {
							LOCK_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%d): Closing orphaned event %d", mono_thread_info_get_small_id (), new->wait_list->data));
							CloseHandle (new->wait_list->data);
							new->wait_list = g_slist_remove (new->wait_list, new->wait_list->data);
						}
					}
					mono_gc_weak_link_remove (&new->data, TRUE);
					new->data = monitor_freelist;
					monitor_freelist = new;
				}
			}
			/* small perf tweak to avoid scanning all the blocks */
			if (new)
				break;
		}
		/* need to allocate a new array of monitors */
		if (!monitor_freelist) {
			MonitorArray *last;
			LOCK_DEBUG (g_message ("%s: allocating more monitors: %d", __func__, array_size));
			marray = g_malloc0 (sizeof (MonoArray) + array_size * sizeof (MonoThreadsSync));
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

	new = monitor_freelist;
	monitor_freelist = new->data;

	new->status = mon_status_set_owner (0, id);
	new->status = mon_status_init_entry_count (new->status);
	new->nest = 1;
	new->data = NULL;
	
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_sync_blocks++;
#endif
	return new;
}

static MonoThreadsSync*
alloc_mon (MonoObject *obj, gint32 id)
{
	MonoThreadsSync *mon;

	mono_monitor_allocator_lock ();
	mon = mon_new (id);
	mono_gc_weak_link_add (&mon->data, obj, TRUE);
	mono_monitor_allocator_unlock ();

	return mon;
}


static void
discard_mon (MonoThreadsSync *mon)
{
	mono_monitor_allocator_lock ();
	mono_gc_weak_link_remove (&mon->data, TRUE);
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
	tmp_lw.sync = InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, nlw.sync, old_lw.sync);
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
		tmp_lw.sync = InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, nlw.sync, old_lw.sync);
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

/*
 * mono_object_hash:
 * @obj: an object
 *
 * Calculate a hash code for @obj that is constant while @obj is alive.
 */
int
mono_object_hash (MonoObject* obj)
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

		old_lw.sync = InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, lw.sync, NULL);
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
		int id = mono_thread_info_get_small_id ();
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

static void
mono_monitor_ensure_owned (LockWord lw, guint32 id)
{
	if (lock_word_is_flat (lw)) {
		if (lock_word_get_owner (lw) == id)
			return;
	} else if (lock_word_is_inflated (lw)) {
		if (mon_status_get_owner (lock_word_get_inflated_lock (lw)->status) == id)
			return;
	}

	mono_set_pending_exception (mono_get_exception_synchronization_lock ("Object synchronization method was called from an unsynchronized block of code."));
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

		/*
		 * Release lock and do the wakeup stuff. It's possible that
		 * the last blocking thread gave up waiting just before we
		 * release the semaphore resulting in a negative entry count
		 * and a futile wakeup next time there's contention for this
		 * object.
		 */
		for (;;) {
			gboolean have_waiters = mon_status_have_waiters (old_status);
	
			new_status = mon_status_set_owner (old_status, 0);
			if (have_waiters)
				new_status = mon_status_decrement_entry_count (new_status);
			tmp_status = InterlockedCompareExchange ((gint32*)&mon->status, new_status, old_status);
			if (tmp_status == old_status) {
				if (have_waiters)
					ReleaseSemaphore (mon->entry_sem, 1, NULL);
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

	tmp_lw.sync = InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, new_lw.sync, old_lw.sync);
	if (old_lw.sync != tmp_lw.sync) {
		/* Someone inflated the lock in the meantime */
		mono_monitor_exit_inflated (obj);
	}

	LOCK_DEBUG (g_message ("%s: (%d) Object %p is now locked %d times; LW = %p", __func__, mono_thread_info_get_small_id (), obj, lock_word_get_nest (new_lw), obj->synchronisation));
}

static void
mon_decrement_entry_count (MonoThreadsSync *mon)
{
	guint32 old_status, tmp_status, new_status;

	/* Decrement entry count */
	old_status = mon->status;
	for (;;) {
		new_status = mon_status_decrement_entry_count (old_status);
		tmp_status = InterlockedCompareExchange ((gint32*)&mon->status, new_status, old_status);
		if (tmp_status == old_status) {
			break;
		}
		old_status = tmp_status;
	}
}

/* If allow_interruption==TRUE, the method will be interrumped if abort or suspend
 * is requested. In this case it returns -1.
 */ 
static inline gint32 
mono_monitor_try_enter_inflated (MonoObject *obj, guint32 ms, gboolean allow_interruption, guint32 id)
{
	LockWord lw;
	MonoThreadsSync *mon;
	HANDLE sem;
	guint32 then = 0, now, delta;
	guint32 waitms;
	guint32 ret;
	guint32 new_status, old_status, tmp_status;
	MonoInternalThread *thread;
	gboolean interrupted = FALSE;

	LOCK_DEBUG (g_message("%s: (%d) Trying to lock object %p (%d ms)", __func__, id, obj, ms));

	if (G_UNLIKELY (!obj)) {
		mono_set_pending_exception (mono_get_exception_argument_null ("obj"));
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
		tmp_status = InterlockedCompareExchange ((gint32*)&mon->status, new_status, old_status);
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
	mono_perfcounters->thread_contentions++;
#endif

	/* If ms is 0 we don't block, but just fail straight away */
	if (ms == 0) {
		LOCK_DEBUG (g_message ("%s: (%d) timed out, returning FALSE", __func__, id));
		return 0;
	}

	mono_profiler_monitor_event (obj, MONO_PROFILER_MONITOR_CONTENTION);

	/* The slow path begins here. */
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
		tmp_status = InterlockedCompareExchange ((gint32*)&mon->status, new_status, old_status);
		if (G_LIKELY (tmp_status == old_status)) {
			/* Success */
			g_assert (mon->nest == 1);
			mono_profiler_monitor_event (obj, MONO_PROFILER_MONITOR_DONE);
			return 1;
		}
	}

	/* If the object is currently locked by this thread... */
	if (mon_status_get_owner (old_status) == id) {
		mon->nest++;
		mono_profiler_monitor_event (obj, MONO_PROFILER_MONITOR_DONE);
		return 1;
	}

	/* We need to make sure there's a semaphore handle (creating it if
	 * necessary), and block on it
	 */
	if (mon->entry_sem == NULL) {
		/* Create the semaphore */
		sem = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
		g_assert (sem != NULL);
		if (InterlockedCompareExchangePointer ((gpointer*)&mon->entry_sem, sem, NULL) != NULL) {
			/* Someone else just put a handle here */
			CloseHandle (sem);
		}
	}

	/*
	 * We need to register ourselves as waiting if it is the first time we are waiting,
	 * of if we were signaled and failed to acquire the lock.
	 */
	if (!interrupted) {
		old_status = mon->status;
		for (;;) {
			if (mon_status_get_owner (old_status) == 0)
				goto retry_contended;
			new_status = mon_status_increment_entry_count (old_status);
			tmp_status = InterlockedCompareExchange ((gint32*)&mon->status, new_status, old_status);
			if (tmp_status == old_status) {
				break;
			}
			old_status = tmp_status;
		}
	}

	if (ms != INFINITE) {
		then = mono_msec_ticks ();
	}
	waitms = ms;
	
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->thread_queue_len++;
	mono_perfcounters->thread_queue_max++;
#endif
	thread = mono_thread_internal_current ();

	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);

	/*
	 * We pass TRUE instead of allow_interruption since we have to check for the
	 * StopRequested case below.
	 */
	MONO_PREPARE_BLOCKING;
	ret = WaitForSingleObjectEx (mon->entry_sem, waitms, TRUE);
	MONO_FINISH_BLOCKING;

	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);
	
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->thread_queue_len--;
#endif

	if (ret == WAIT_IO_COMPLETION && !allow_interruption) {
		interrupted = TRUE;
		/* 
		 * We have to obey a stop/suspend request even if 
		 * allow_interruption is FALSE to avoid hangs at shutdown.
		 */
		if (!mono_thread_test_state (mono_thread_internal_current (), (ThreadState_StopRequested|ThreadState_SuspendRequested))) {
			if (ms != INFINITE) {
				now = mono_msec_ticks ();
				if (now < then) {
					LOCK_DEBUG (g_message ("%s: wrapped around! now=0x%x then=0x%x", __func__, now, then));

					now += (0xffffffff - then);
					then = 0;

					LOCK_DEBUG (g_message ("%s: wrap rejig: now=0x%x then=0x%x delta=0x%x", __func__, now, then, now-then));
				}

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
	} else if (ret == WAIT_OBJECT_0) {
		interrupted = FALSE;
		/* retry from the top */
		goto retry_contended;
	} else if (ret == WAIT_TIMEOUT) {
		/* we're done */
	}

	/* Timed out or interrupted */
	mon_decrement_entry_count (mon);

	mono_profiler_monitor_event (obj, MONO_PROFILER_MONITOR_FAIL);

	if (ret == WAIT_IO_COMPLETION) {
		LOCK_DEBUG (g_message ("%s: (%d) interrupted waiting, returning -1", __func__, id));
		return -1;
	} else if (ret == WAIT_TIMEOUT) {
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
	int id = mono_thread_info_get_small_id ();

	LOCK_DEBUG (g_message("%s: (%d) Trying to lock object %p (%d ms)", __func__, id, obj, ms));

	if (G_UNLIKELY (!obj)) {
		mono_set_pending_exception (mono_get_exception_argument_null ("obj"));
		return FALSE;
	}

	lw.sync = obj->synchronisation;

	if (G_LIKELY (lock_word_is_free (lw))) {
		LockWord nlw = lock_word_new_flat (id);
		if (InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, nlw.sync, NULL) == NULL) {
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
				old_lw.sync = InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, nlw.sync, lw.sync);
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

gboolean 
mono_monitor_enter (MonoObject *obj)
{
	return mono_monitor_try_enter_internal (obj, INFINITE, FALSE) == 1;
}

gboolean 
mono_monitor_try_enter (MonoObject *obj, guint32 ms)
{
	return mono_monitor_try_enter_internal (obj, ms, FALSE) == 1;
}

void
mono_monitor_exit (MonoObject *obj)
{
	LockWord lw;
	
	LOCK_DEBUG (g_message ("%s: (%d) Unlocking %p", __func__, mono_thread_info_get_small_id (), obj));

	if (G_UNLIKELY (!obj)) {
		mono_set_pending_exception (mono_get_exception_argument_null ("obj"));
		return;
	}

	lw.sync = obj->synchronisation;

	mono_monitor_ensure_owned (lw, mono_thread_info_get_small_id ());

	if (G_UNLIKELY (lock_word_is_inflated (lw)))
		mono_monitor_exit_inflated (obj);
	else
		mono_monitor_exit_flat (obj, lw);
}

void**
mono_monitor_get_object_monitor_weak_link (MonoObject *object)
{
	LockWord lw;

	lw.sync = object->synchronisation;

	if (lock_word_is_inflated (lw)) {
		MonoThreadsSync *mon = lock_word_get_inflated_lock (lw);
		if (mon->data)
			return &mon->data;
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

gboolean 
ves_icall_System_Threading_Monitor_Monitor_try_enter (MonoObject *obj, guint32 ms)
{
	gint32 res;

	do {
		res = mono_monitor_try_enter_internal (obj, ms, TRUE);
		if (res == -1)
			mono_thread_interruption_checkpoint ();
	} while (res == -1);
	
	return res == 1;
}

void
ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var (MonoObject *obj, guint32 ms, char *lockTaken)
{
	gint32 res;
	do {
		res = mono_monitor_try_enter_internal (obj, ms, TRUE);
		/*This means we got interrupted during the wait and didn't got the monitor.*/
		if (res == -1)
			mono_thread_interruption_checkpoint ();
	} while (res == -1);
	/*It's safe to do it from here since interruption would happen only on the wrapper.*/
	*lockTaken = res == 1;
}

void
mono_monitor_enter_v4 (MonoObject *obj, char *lock_taken)
{

	if (*lock_taken == 1) {
		mono_set_pending_exception (mono_get_exception_argument ("lockTaken", "lockTaken is already true"));
		return;
	}

	ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var (obj, INFINITE, lock_taken);
}

gboolean 
ves_icall_System_Threading_Monitor_Monitor_test_owner (MonoObject *obj)
{
	LockWord lw;

	LOCK_DEBUG (g_message ("%s: Testing if %p is owned by thread %d", __func__, obj, mono_thread_info_get_small_id()));

	lw.sync = obj->synchronisation;

	if (lock_word_is_flat (lw)) {
		return lock_word_get_owner (lw) == mono_thread_info_get_small_id ();
	} else if (lock_word_is_inflated (lw)) {
		return mon_status_get_owner (lock_word_get_inflated_lock (lw)->status) == mono_thread_info_get_small_id ();
	}
	
	return(FALSE);
}

gboolean 
ves_icall_System_Threading_Monitor_Monitor_test_synchronised (MonoObject *obj)
{
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

void
ves_icall_System_Threading_Monitor_Monitor_pulse (MonoObject *obj)
{
	int id;
	LockWord lw;
	MonoThreadsSync *mon;

	LOCK_DEBUG (g_message ("%s: (%d) Pulsing %p", __func__, mono_thread_info_get_small_id (), obj));
	
	id = mono_thread_info_get_small_id ();
	lw.sync = obj->synchronisation;

	mono_monitor_ensure_owned (lw, id);

	if (!lock_word_is_inflated (lw)) {
		/* No threads waiting. A wait would have inflated the lock */
		return;
	}

	mon = lock_word_get_inflated_lock (lw);

	LOCK_DEBUG (g_message ("%s: (%d) %d threads waiting", __func__, mono_thread_info_get_small_id (), g_slist_length (mon->wait_list)));

	if (mon->wait_list != NULL) {
		LOCK_DEBUG (g_message ("%s: (%d) signalling and dequeuing handle %p", __func__, mono_thread_info_get_small_id (), mon->wait_list->data));
	
		SetEvent (mon->wait_list->data);
		mon->wait_list = g_slist_remove (mon->wait_list, mon->wait_list->data);
	}
}

void
ves_icall_System_Threading_Monitor_Monitor_pulse_all (MonoObject *obj)
{
	int id;
	LockWord lw;
	MonoThreadsSync *mon;
	
	LOCK_DEBUG (g_message("%s: (%d) Pulsing all %p", __func__, mono_thread_info_get_small_id (), obj));

	id = mono_thread_info_get_small_id ();
	lw.sync = obj->synchronisation;

	mono_monitor_ensure_owned (lw, id);

	if (!lock_word_is_inflated (lw)) {
		/* No threads waiting. A wait would have inflated the lock */
		return;
	}

	mon = lock_word_get_inflated_lock (lw);

	LOCK_DEBUG (g_message ("%s: (%d) %d threads waiting", __func__, mono_thread_info_get_small_id (), g_slist_length (mon->wait_list)));

	while (mon->wait_list != NULL) {
		LOCK_DEBUG (g_message ("%s: (%d) signalling and dequeuing handle %p", __func__, mono_thread_info_get_small_id (), mon->wait_list->data));
	
		SetEvent (mon->wait_list->data);
		mon->wait_list = g_slist_remove (mon->wait_list, mon->wait_list->data);
	}
}

gboolean
ves_icall_System_Threading_Monitor_Monitor_wait (MonoObject *obj, guint32 ms)
{
	LockWord lw;
	MonoThreadsSync *mon;
	HANDLE event;
	guint32 nest;
	guint32 ret;
	gboolean success = FALSE;
	gint32 regain;
	MonoInternalThread *thread = mono_thread_internal_current ();
	int id = mono_thread_info_get_small_id ();

	LOCK_DEBUG (g_message ("%s: (%d) Trying to wait for %p with timeout %dms", __func__, mono_thread_info_get_small_id (), obj, ms));

	lw.sync = obj->synchronisation;

	mono_monitor_ensure_owned (lw, id);

	if (!lock_word_is_inflated (lw)) {
		mono_monitor_inflate_owned (obj, id);
		lw.sync = obj->synchronisation;
	}

	mon = lock_word_get_inflated_lock (lw);

	/* Do this WaitSleepJoin check before creating the event handle */
	mono_thread_current_check_pending_interrupt ();
	
	event = CreateEvent (NULL, FALSE, FALSE, NULL);
	if (event == NULL) {
		mono_set_pending_exception (mono_get_exception_synchronization_lock ("Failed to set up wait event"));
		return FALSE;
	}
	
	LOCK_DEBUG (g_message ("%s: (%d) queuing handle %p", __func__, mono_thread_info_get_small_id (), event));

	mono_thread_current_check_pending_interrupt ();
	
	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);

	mon->wait_list = g_slist_append (mon->wait_list, event);
	
	/* Save the nest count, and release the lock */
	nest = mon->nest;
	mon->nest = 1;
	mono_memory_write_barrier ();
	mono_monitor_exit_inflated (obj);

	LOCK_DEBUG (g_message ("%s: (%d) Unlocked %p lock %p", __func__, mono_thread_info_get_small_id (), obj, mon));

	/* There's no race between unlocking mon and waiting for the
	 * event, because auto reset events are sticky, and this event
	 * is private to this thread.  Therefore even if the event was
	 * signalled before we wait, we still succeed.
	 */
	MONO_PREPARE_BLOCKING;
	ret = WaitForSingleObjectEx (event, ms, TRUE);
	MONO_FINISH_BLOCKING;

	/* Reset the thread state fairly early, so we don't have to worry
	 * about the monitor error checking
	 */
	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);
	
	if (mono_thread_interruption_requested ()) {
		/* 
		 * Can't remove the event from wait_list, since the monitor is not locked by
		 * us. So leave it there, mon_new () will delete it when the mon structure
		 * is placed on the free list.
		 * FIXME: The caller expects to hold the lock after the wait returns, but it
		 * doesn't happen in this case:
		 * http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=97268
		 */
		return FALSE;
	}

	/* Regain the lock with the previous nest count */
	do {
		regain = mono_monitor_try_enter_inflated (obj, INFINITE, TRUE, id);
		if (regain == -1) 
			mono_thread_interruption_checkpoint ();
	} while (regain == -1);

	if (regain == 0) {
		/* Something went wrong, so throw a
		 * SynchronizationLockException
		 */
		CloseHandle (event);
		mono_set_pending_exception (mono_get_exception_synchronization_lock ("Failed to regain lock"));
		return FALSE;
	}

	mon->nest = nest;

	LOCK_DEBUG (g_message ("%s: (%d) Regained %p lock %p", __func__, mono_thread_info_get_small_id (), obj, mon));

	if (ret == WAIT_TIMEOUT) {
		/* Poll the event again, just in case it was signalled
		 * while we were trying to regain the monitor lock
		 */
		MONO_PREPARE_BLOCKING;
		ret = WaitForSingleObjectEx (event, 0, FALSE);
		MONO_FINISH_BLOCKING;
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
	
	if (ret == WAIT_OBJECT_0) {
		LOCK_DEBUG (g_message ("%s: (%d) Success", __func__, mono_thread_info_get_small_id ()));
		success = TRUE;
	} else {
		LOCK_DEBUG (g_message ("%s: (%d) Wait failed, dequeuing handle %p", __func__, mono_thread_info_get_small_id (), event));
		/* No pulse, so we have to remove ourself from the
		 * wait queue
		 */
		mon->wait_list = g_slist_remove (mon->wait_list, event);
	}
	CloseHandle (event);
	
	return success;
}

