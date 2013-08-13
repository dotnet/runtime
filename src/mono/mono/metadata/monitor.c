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
 * object, but the technique relies on encoding the thread ID in 15
 * bits (to avoid too much per-object space overhead.)  Unfortunately
 * I don't think it's possible to reliably encode a pthread_t into 15
 * bits. (The JVM implementation used seems to have a 15-bit
 * per-thread identifier available.)
 *
 * This implementation then combines Dice's basic lock model with
 * Bacon's simplification of keeping a lock record for the lifetime of
 * an object.
 */

struct _MonoThreadsSync
{
	gsize owner;			/* thread ID */
	guint32 nest;
#ifdef HAVE_MOVING_COLLECTOR
	gint32 hash_code;
#endif
	volatile gint32 entry_count;
	HANDLE entry_sem;
	GSList *wait_list;
	void *data;
};

typedef struct _MonitorArray MonitorArray;

struct _MonitorArray {
	MonitorArray *next;
	int num_monitors;
	MonoThreadsSync monitors [MONO_ZERO_LEN_ARRAY];
};

#define mono_monitor_allocator_lock() EnterCriticalSection (&monitor_mutex)
#define mono_monitor_allocator_unlock() LeaveCriticalSection (&monitor_mutex)
static CRITICAL_SECTION monitor_mutex;
static MonoThreadsSync *monitor_freelist;
static MonitorArray *monitor_allocated;
static int array_size = 16;

#ifdef HAVE_KW_THREAD
static __thread gsize tls_pthread_self MONO_TLS_FAST;
#endif

#ifndef HOST_WIN32
#ifdef HAVE_KW_THREAD
#define GetCurrentThreadId() tls_pthread_self
#else
/* 
 * The usual problem: we can't replace GetCurrentThreadId () with a macro because
 * it is in a public header.
 */
#define GetCurrentThreadId() ((gsize)pthread_self ())
#endif
#endif

void
mono_monitor_init (void)
{
	InitializeCriticalSection (&monitor_mutex);
}
 
void
mono_monitor_cleanup (void)
{
	MonoThreadsSync *mon;
	/* MonitorArray *marray, *next = NULL; */

	/*DeleteCriticalSection (&monitor_mutex);*/

	/* The monitors on the freelist don't have weak links - mark them */
	for (mon = monitor_freelist; mon; mon = mon->data)
		mon->wait_list = (gpointer)-1;

	/* FIXME: This still crashes with sgen (async_read.exe) */
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

/*
 * mono_monitor_init_tls:
 *
 *   Setup TLS variables used by the monitor code for the current thread.
 */
void
mono_monitor_init_tls (void)
{
#if !defined(HOST_WIN32) && defined(HAVE_KW_THREAD)
	tls_pthread_self = (gsize) pthread_self ();
#endif
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
					if (mon->owner) {
						g_print ("Lock %p in object %p held by thread %p, nest level: %d\n",
							mon, holder, (void*)mon->owner, mon->nest);
						if (mon->entry_sem)
							g_print ("\tWaiting on semaphore %p: %d\n", mon->entry_sem, mon->entry_count);
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

	mon->entry_count = 0;
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
							LOCK_DEBUG (g_message (G_GNUC_PRETTY_FUNCTION ": (%d): Closing orphaned event %d", GetCurrentThreadId (), new->wait_list->data));
							CloseHandle (new->wait_list->data);
							new->wait_list = g_slist_remove (new->wait_list, new->wait_list->data);
						}
					}
					mono_gc_weak_link_remove (&new->data, FALSE);
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

	new->owner = id;
	new->nest = 1;
	new->data = NULL;
	
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->gc_sync_blocks++;
#endif
	return new;
}

/*
 * Format of the lock word:
 * thinhash | fathash | data
 *
 * thinhash is the lower bit: if set data is the shifted hashcode of the object.
 * fathash is another bit: if set the hash code is stored in the MonoThreadsSync
 *   struct pointed to by data
 * if neither bit is set and data is non-NULL, data is a MonoThreadsSync
 */
typedef union {
	gsize lock_word;
	MonoThreadsSync *sync;
} LockWord;

enum {
	LOCK_WORD_THIN_HASH = 1,
	LOCK_WORD_FAT_HASH = 1 << 1,
	LOCK_WORD_BITS_MASK = 0x3,
	LOCK_WORD_HASH_SHIFT = 2
};

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
	if (lw.lock_word & LOCK_WORD_THIN_HASH) {
		/*g_print ("fast thin hash %d for obj %p store\n", (unsigned int)lw.lock_word >> LOCK_WORD_HASH_SHIFT, obj);*/
		return (unsigned int)lw.lock_word >> LOCK_WORD_HASH_SHIFT;
	}
	if (lw.lock_word & LOCK_WORD_FAT_HASH) {
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		/*g_print ("fast fat hash %d for obj %p store\n", lw.sync->hash_code, obj);*/
		return lw.sync->hash_code;
	}
	/*
	 * while we are inside this function, the GC will keep this object pinned,
	 * since we are in the unmanaged stack. Thanks to this and to the hash
	 * function that depends only on the address, we can ignore the races if
	 * another thread computes the hash at the same time, because it'll end up
	 * with the same value.
	 */
	hash = (GPOINTER_TO_UINT (obj) >> MONO_OBJECT_ALIGNMENT_SHIFT) * 2654435761u;
	/* clear the top bits as they can be discarded */
	hash &= ~(LOCK_WORD_BITS_MASK << 30);
	/* no hash flags were set, so it must be a MonoThreadsSync pointer if not NULL */
	if (lw.sync) {
		lw.sync->hash_code = hash;
		/*g_print ("storing hash code %d for obj %p in sync %p\n", hash, obj, lw.sync);*/
		lw.lock_word |= LOCK_WORD_FAT_HASH;
		/* this is safe since we don't deflate locks */
		obj->synchronisation = lw.sync;
	} else {
		/*g_print ("storing thin hash code %d for obj %p\n", hash, obj);*/
		lw.lock_word = LOCK_WORD_THIN_HASH | (hash << LOCK_WORD_HASH_SHIFT);
		if (InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, lw.sync, NULL) == NULL)
			return hash;
		/*g_print ("failed store\n");*/
		/* someone set the hash flag or someone inflated the object */
		lw.sync = obj->synchronisation;
		if (lw.lock_word & LOCK_WORD_THIN_HASH)
			return hash;
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		lw.sync->hash_code = hash;
		lw.lock_word |= LOCK_WORD_FAT_HASH;
		/* this is safe since we don't deflate locks */
		obj->synchronisation = lw.sync;
	}
	return hash;
#else
/*
 * Wang's address-based hash function:
 *   http://www.concentric.net/~Ttwang/tech/addrhash.htm
 */
	return (GPOINTER_TO_UINT (obj) >> MONO_OBJECT_ALIGNMENT_SHIFT) * 2654435761u;
#endif
}

/* If allow_interruption==TRUE, the method will be interrumped if abort or suspend
 * is requested. In this case it returns -1.
 */ 
static inline gint32 
mono_monitor_try_enter_internal (MonoObject *obj, guint32 ms, gboolean allow_interruption)
{
	MonoThreadsSync *mon;
	gsize id = GetCurrentThreadId ();
	HANDLE sem;
	guint32 then = 0, now, delta;
	guint32 waitms;
	guint32 ret;
	MonoInternalThread *thread;

	LOCK_DEBUG (g_message("%s: (%d) Trying to lock object %p (%d ms)", __func__, id, obj, ms));

	if (G_UNLIKELY (!obj)) {
		mono_raise_exception (mono_get_exception_argument_null ("obj"));
		return FALSE;
	}

retry:
	mon = obj->synchronisation;

	/* If the object has never been locked... */
	if (G_UNLIKELY (mon == NULL)) {
		mono_monitor_allocator_lock ();
		mon = mon_new (id);
		if (InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, mon, NULL) == NULL) {
			mono_gc_weak_link_add (&mon->data, obj, FALSE);
			mono_monitor_allocator_unlock ();
			/* Successfully locked */
			return 1;
		} else {
#ifdef HAVE_MOVING_COLLECTOR
			LockWord lw;
			lw.sync = obj->synchronisation;
			if (lw.lock_word & LOCK_WORD_THIN_HASH) {
				MonoThreadsSync *oldlw = lw.sync;
				/* move the already calculated hash */
				mon->hash_code = lw.lock_word >> LOCK_WORD_HASH_SHIFT;
				lw.sync = mon;
				lw.lock_word |= LOCK_WORD_FAT_HASH;
				if (InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, lw.sync, oldlw) == oldlw) {
					mono_gc_weak_link_add (&mon->data, obj, FALSE);
					mono_monitor_allocator_unlock ();
					/* Successfully locked */
					return 1;
				} else {
					mon_finalize (mon);
					mono_monitor_allocator_unlock ();
					goto retry;
				}
			} else if (lw.lock_word & LOCK_WORD_FAT_HASH) {
				mon_finalize (mon);
				mono_monitor_allocator_unlock ();
				/* get the old lock without the fat hash bit */
				lw.lock_word &= ~LOCK_WORD_BITS_MASK;
				mon = lw.sync;
			} else {
				mon_finalize (mon);
				mono_monitor_allocator_unlock ();
				mon = obj->synchronisation;
			}
#else
			mon_finalize (mon);
			mono_monitor_allocator_unlock ();
			mon = obj->synchronisation;
#endif
		}
	} else {
#ifdef HAVE_MOVING_COLLECTOR
		LockWord lw;
		lw.sync = mon;
		if (lw.lock_word & LOCK_WORD_THIN_HASH) {
			MonoThreadsSync *oldlw = lw.sync;
			mono_monitor_allocator_lock ();
			mon = mon_new (id);
			/* move the already calculated hash */
			mon->hash_code = lw.lock_word >> LOCK_WORD_HASH_SHIFT;
			lw.sync = mon;
			lw.lock_word |= LOCK_WORD_FAT_HASH;
			if (InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, lw.sync, oldlw) == oldlw) {
				mono_gc_weak_link_add (&mon->data, obj, TRUE);
				mono_monitor_allocator_unlock ();
				/* Successfully locked */
				return 1;
			} else {
				mon_finalize (mon);
				mono_monitor_allocator_unlock ();
				goto retry;
			}
		}
#endif
	}

#ifdef HAVE_MOVING_COLLECTOR
	{
		LockWord lw;
		lw.sync = mon;
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		mon = lw.sync;
	}
#endif

	/* If the object has previously been locked but isn't now... */

	/* This case differs from Dice's case 3 because we don't
	 * deflate locks or cache unused lock records
	 */
	if (G_LIKELY (mon->owner == 0)) {
		/* Try to install our ID in the owner field, nest
		 * should have been left at 1 by the previous unlock
		 * operation
		 */
		if (G_LIKELY (InterlockedCompareExchangePointer ((gpointer *)&mon->owner, (gpointer)id, 0) == 0)) {
			/* Success */
			g_assert (mon->nest == 1);
			return 1;
		} else {
			/* Trumped again! */
			goto retry;
		}
	}

	/* If the object is currently locked by this thread... */
	if (mon->owner == id) {
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
	if (G_LIKELY (mon->owner == 0)) {
		/* Try to install our ID in the owner field, nest
		* should have been left at 1 by the previous unlock
		* operation
		*/
		if (G_LIKELY (InterlockedCompareExchangePointer ((gpointer *)&mon->owner, (gpointer)id, 0) == 0)) {
			/* Success */
			g_assert (mon->nest == 1);
			mono_profiler_monitor_event (obj, MONO_PROFILER_MONITOR_DONE);
			return 1;
		}
	}

	/* If the object is currently locked by this thread... */
	if (mon->owner == id) {
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
	
	/* If we need to time out, record a timestamp and adjust ms,
	 * because WaitForSingleObject doesn't tell us how long it
	 * waited for.
	 *
	 * Don't block forever here, because theres a chance the owner
	 * thread released the lock while we were creating the
	 * semaphore: we would not get the wakeup.  Using the event
	 * handle technique from pulse/wait would involve locking the
	 * lock struct and therefore slowing down the fast path.
	 */
	if (ms != INFINITE) {
		then = mono_msec_ticks ();
		if (ms < 100) {
			waitms = ms;
		} else {
			waitms = 100;
		}
	} else {
		waitms = 100;
	}
	
	InterlockedIncrement (&mon->entry_count);

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
	ret = WaitForSingleObjectEx (mon->entry_sem, waitms, TRUE);

	mono_thread_clr_state (thread, ThreadState_WaitSleepJoin);
	
	InterlockedDecrement (&mon->entry_count);
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->thread_queue_len--;
#endif

	if (ms != INFINITE) {
		now = mono_msec_ticks ();
		
		if (now < then) {
			/* The counter must have wrapped around */
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

		if ((ret == WAIT_TIMEOUT || (ret == WAIT_IO_COMPLETION && !allow_interruption)) && ms > 0) {
			/* More time left */
			goto retry_contended;
		}
	} else {
		if (ret == WAIT_TIMEOUT || (ret == WAIT_IO_COMPLETION && !allow_interruption)) {
			if (ret == WAIT_IO_COMPLETION && (mono_thread_test_state (mono_thread_internal_current (), (ThreadState_StopRequested|ThreadState_SuspendRequested)))) {
				/* 
				 * We have to obey a stop/suspend request even if 
				 * allow_interruption is FALSE to avoid hangs at shutdown.
				 */
				mono_profiler_monitor_event (obj, MONO_PROFILER_MONITOR_FAIL);
				return -1;
			}
			/* Infinite wait, so just try again */
			goto retry_contended;
		}
	}
	
	if (ret == WAIT_OBJECT_0) {
		/* retry from the top */
		goto retry_contended;
	}

	/* We must have timed out */
	LOCK_DEBUG (g_message ("%s: (%d) timed out waiting, returning FALSE", __func__, id));

	mono_profiler_monitor_event (obj, MONO_PROFILER_MONITOR_FAIL);

	if (ret == WAIT_IO_COMPLETION)
		return -1;
	else 
		return 0;
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
	MonoThreadsSync *mon;
	guint32 nest;
	
	LOCK_DEBUG (g_message ("%s: (%d) Unlocking %p", __func__, GetCurrentThreadId (), obj));

	if (G_UNLIKELY (!obj)) {
		mono_raise_exception (mono_get_exception_argument_null ("obj"));
		return;
	}

	mon = obj->synchronisation;

#ifdef HAVE_MOVING_COLLECTOR
	{
		LockWord lw;
		lw.sync = mon;
		if (lw.lock_word & LOCK_WORD_THIN_HASH)
			return;
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		mon = lw.sync;
	}
#endif
	if (G_UNLIKELY (mon == NULL)) {
		/* No one ever used Enter. Just ignore the Exit request as MS does */
		return;
	}
	if (G_UNLIKELY (mon->owner != GetCurrentThreadId ())) {
		return;
	}
	
	nest = mon->nest - 1;
	if (nest == 0) {
		LOCK_DEBUG (g_message ("%s: (%d) Object %p is now unlocked", __func__, GetCurrentThreadId (), obj));
	
		/* object is now unlocked, leave nest==1 so we don't
		 * need to set it when the lock is reacquired
		 */
		mon->owner = 0;

		/* Do the wakeup stuff.  It's possible that the last
		 * blocking thread gave up waiting just before we
		 * release the semaphore resulting in a futile wakeup
		 * next time there's contention for this object, but
		 * it means we don't have to waste time locking the
		 * struct.
		 */
		if (mon->entry_count > 0) {
			ReleaseSemaphore (mon->entry_sem, 1, NULL);
		}
	} else {
		LOCK_DEBUG (g_message ("%s: (%d) Object %p is now locked %d times", __func__, GetCurrentThreadId (), obj, nest));
		mon->nest = nest;
	}
}

void**
mono_monitor_get_object_monitor_weak_link (MonoObject *object)
{
	LockWord lw;
	MonoThreadsSync *sync = NULL;

	lw.sync = object->synchronisation;
	if (lw.lock_word & LOCK_WORD_FAT_HASH) {
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		sync = lw.sync;
	} else if (!(lw.lock_word & LOCK_WORD_THIN_HASH)) {
		sync = lw.sync;
	}

	if (sync && sync->data)
		return &sync->data;
	return NULL;
}

#ifndef DISABLE_JIT

static void
emit_obj_syncp_check (MonoMethodBuilder *mb, int syncp_loc, int *obj_null_branch, int *true_locktaken_branch, int *syncp_true_false_branch,
	int *thin_hash_branch, gboolean branch_on_true)
{
	/*
	  ldarg		0							obj
	  brfalse.s	obj_null
	*/

	mono_mb_emit_byte (mb, CEE_LDARG_0);
	*obj_null_branch = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

	/*
	  ldarg.1
	  ldind.i1
	  brtrue.s true_locktaken
	*/
	if (true_locktaken_branch) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_LDIND_I1);
		*true_locktaken_branch = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);
	}

	/*
	  ldarg		0							obj
	  conv.i								objp
	  ldc.i4	G_STRUCT_OFFSET(MonoObject, synchronisation)		objp off
	  add									&syncp
	  ldind.i								syncp
	  stloc		syncp
	  ldloc		syncp							syncp
	  brtrue/false.s	syncp_true_false
	*/

	mono_mb_emit_byte (mb, CEE_LDARG_0);
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoObject, synchronisation));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, syncp_loc);

	
	if (mono_gc_is_moving ()) {
		/*check for a thin hash*/
		mono_mb_emit_ldloc (mb, syncp_loc);
		mono_mb_emit_icon (mb, 0x01);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_byte (mb, CEE_AND);
		*thin_hash_branch = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

		/*clear gc bits*/
		mono_mb_emit_ldloc (mb, syncp_loc);
		mono_mb_emit_icon (mb, ~0x3);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_byte (mb, CEE_AND);
		mono_mb_emit_stloc (mb, syncp_loc);
	} else {
		*thin_hash_branch = 0;
	}

	mono_mb_emit_ldloc (mb, syncp_loc);
	*syncp_true_false_branch = mono_mb_emit_short_branch (mb, branch_on_true ? CEE_BRTRUE_S : CEE_BRFALSE_S);
}

#endif

static MonoMethod* monitor_il_fastpaths[3];

gboolean
mono_monitor_is_il_fastpath_wrapper (MonoMethod *method)
{
	int i;
	for (i = 0; i < 3; ++i) {
		if (monitor_il_fastpaths [i] == method)
			return TRUE;
	}
	return FALSE;
}

enum {
	FASTPATH_ENTER,
	FASTPATH_ENTERV4,
	FASTPATH_EXIT
};


static MonoMethod*
register_fastpath (MonoMethod *method, int idx)
{
	mono_memory_barrier ();
	monitor_il_fastpaths [idx] = method;
	return method;
}

static MonoMethod*
mono_monitor_get_fast_enter_method (MonoMethod *monitor_enter_method)
{
	MonoMethodBuilder *mb;
	MonoMethod *res;
	static MonoMethod *compare_exchange_method;
	int obj_null_branch, true_locktaken_branch = 0, syncp_null_branch, has_owner_branch, other_owner_branch, tid_branch, thin_hash_branch;
	int tid_loc, syncp_loc, owner_loc;
	int thread_tls_offset;
	gboolean is_v4 = mono_method_signature (monitor_enter_method)->param_count == 2;
	int fast_path_idx = is_v4 ? FASTPATH_ENTERV4 : FASTPATH_ENTER;
	WrapperInfo *info;

	/* The !is_v4 version is not used/tested */
	g_assert (is_v4);

	thread_tls_offset = mono_thread_get_tls_offset ();
	if (thread_tls_offset == -1)
		return NULL;

	if (monitor_il_fastpaths [fast_path_idx])
		return monitor_il_fastpaths [fast_path_idx];

	if (!compare_exchange_method) {
		MonoMethodDesc *desc;
		MonoClass *class;

		desc = mono_method_desc_new ("Interlocked:CompareExchange(intptr&,intptr,intptr)", FALSE);
		class = mono_class_from_name (mono_defaults.corlib, "System.Threading", "Interlocked");
		compare_exchange_method = mono_method_desc_search_in_class (desc, class);
		mono_method_desc_free (desc);

		if (!compare_exchange_method)
			return NULL;
	}

	mb = mono_mb_new (mono_defaults.monitor_class, is_v4 ? "FastMonitorEnterV4" : "FastMonitorEnter", MONO_WRAPPER_UNKNOWN);

	mb->method->slot = -1;
	mb->method->flags = METHOD_ATTRIBUTE_PUBLIC | METHOD_ATTRIBUTE_STATIC |
		METHOD_ATTRIBUTE_HIDE_BY_SIG | METHOD_ATTRIBUTE_FINAL;

#ifndef DISABLE_JIT
	tid_loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	syncp_loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	owner_loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	emit_obj_syncp_check (mb, syncp_loc, &obj_null_branch, is_v4 ? &true_locktaken_branch : NULL, &syncp_null_branch, &thin_hash_branch, FALSE);

	/*
	  mono. tls	thread_tls_offset					threadp
	  ldc.i4	G_STRUCT_OFFSET(MonoThread, tid)			threadp off
	  add									&tid
	  ldind.i								tid
	  stloc		tid
	  ldloc		syncp							syncp
	  ldc.i4	G_STRUCT_OFFSET(MonoThreadsSync, owner)			syncp off
	  add									&owner
	  ldind.i								owner
	  stloc		owner
	  ldloc		owner							owner
	  brtrue.s	tid
	*/

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_TLS);
	mono_mb_emit_i4 (mb, thread_tls_offset);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoInternalThread, tid));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, tid_loc);
	mono_mb_emit_ldloc (mb, syncp_loc);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoThreadsSync, owner));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, owner_loc);
	mono_mb_emit_ldloc (mb, owner_loc);
	tid_branch = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

	/*
	  ldloc		syncp							syncp
	  ldc.i4	G_STRUCT_OFFSET(MonoThreadsSync, owner)			syncp off
	  add									&owner
	  ldloc		tid							&owner tid
	  ldc.i4	0							&owner tid 0
	  call		System.Threading.Interlocked.CompareExchange		oldowner
	  brtrue.s	has_owner
	  ret
	*/

	mono_mb_emit_ldloc (mb, syncp_loc);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoThreadsSync, owner));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_ldloc (mb, tid_loc);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_managed_call (mb, compare_exchange_method, NULL);
	has_owner_branch = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

	if (is_v4) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
	}
	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 tid:
	  ldloc		owner							owner
	  ldloc		tid							owner tid
	  brne.s	other_owner
	  ldloc		syncp							syncp
	  ldc.i4	G_STRUCT_OFFSET(MonoThreadsSync, nest)			syncp off
	  add									&nest
	  dup									&nest &nest
	  ldind.i4								&nest nest
	  ldc.i4	1							&nest nest 1
	  add									&nest nest+
	  stind.i4
	  ret
	*/

	mono_mb_patch_short_branch (mb, tid_branch);
	mono_mb_emit_ldloc (mb, owner_loc);
	mono_mb_emit_ldloc (mb, tid_loc);
	other_owner_branch = mono_mb_emit_short_branch (mb, CEE_BNE_UN_S);
	mono_mb_emit_ldloc (mb, syncp_loc);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoThreadsSync, nest));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_byte (mb, CEE_LDC_I4_1);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_STIND_I4);

	if (is_v4) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 obj_null, syncp_null, has_owner, other_owner:
	  ldarg		0							obj
	  call		System.Threading.Monitor.Enter
	  ret
	*/

	if (thin_hash_branch)
		mono_mb_patch_short_branch (mb, thin_hash_branch);
	mono_mb_patch_short_branch (mb, obj_null_branch);
	mono_mb_patch_short_branch (mb, syncp_null_branch);
	mono_mb_patch_short_branch (mb, has_owner_branch);
	mono_mb_patch_short_branch (mb, other_owner_branch);
	if (true_locktaken_branch)
		mono_mb_patch_short_branch (mb, true_locktaken_branch);
	mono_mb_emit_byte (mb, CEE_LDARG_0);
	if (is_v4)
		mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_managed_call (mb, monitor_enter_method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	res = register_fastpath (mono_mb_create_method (mb, mono_signature_no_pinvoke (monitor_enter_method), 5), fast_path_idx);

	info = mono_image_alloc0 (mono_defaults.corlib, sizeof (WrapperInfo));
	info->subtype = is_v4 ? WRAPPER_SUBTYPE_FAST_MONITOR_ENTER_V4 : WRAPPER_SUBTYPE_FAST_MONITOR_ENTER;
	mono_marshal_set_wrapper_info (res, info);

	mono_mb_free (mb);
	return res;
}

static MonoMethod*
mono_monitor_get_fast_exit_method (MonoMethod *monitor_exit_method)
{
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int obj_null_branch, has_waiting_branch, has_syncp_branch, owned_branch, nested_branch, thin_hash_branch;
	int thread_tls_offset;
	int syncp_loc;
	WrapperInfo *info;

	thread_tls_offset = mono_thread_get_tls_offset ();
	if (thread_tls_offset == -1)
		return NULL;

	if (monitor_il_fastpaths [FASTPATH_EXIT])
		return monitor_il_fastpaths [FASTPATH_EXIT];

	mb = mono_mb_new (mono_defaults.monitor_class, "FastMonitorExit", MONO_WRAPPER_UNKNOWN);

	mb->method->slot = -1;
	mb->method->flags = METHOD_ATTRIBUTE_PUBLIC | METHOD_ATTRIBUTE_STATIC |
		METHOD_ATTRIBUTE_HIDE_BY_SIG | METHOD_ATTRIBUTE_FINAL;

#ifndef DISABLE_JIT
	syncp_loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	emit_obj_syncp_check (mb, syncp_loc, &obj_null_branch, NULL, &has_syncp_branch, &thin_hash_branch, TRUE);

	/*
	  ret
	*/

	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 has_syncp:
	  ldloc		syncp							syncp
	  ldc.i4	G_STRUCT_OFFSET(MonoThreadsSync, owner)			syncp off
	  add									&owner
	  ldind.i								owner
	  mono. tls	thread_tls_offset					owner threadp
	  ldc.i4	G_STRUCT_OFFSET(MonoThread, tid)			owner threadp off
	  add									owner &tid
	  ldind.i								owner tid
	  beq.s		owned
	*/

	mono_mb_patch_short_branch (mb, has_syncp_branch);
	mono_mb_emit_ldloc (mb, syncp_loc);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoThreadsSync, owner));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_TLS);
	mono_mb_emit_i4 (mb, thread_tls_offset);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoInternalThread, tid));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	owned_branch = mono_mb_emit_short_branch (mb, CEE_BEQ_S);

	/*
	  ret
	*/

	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 owned:
	  ldloc		syncp							syncp
	  ldc.i4	G_STRUCT_OFFSET(MonoThreadsSync, nest)			syncp off
	  add									&nest
	  dup									&nest &nest
	  ldind.i4								&nest nest
	  dup									&nest nest nest
	  ldc.i4	1							&nest nest nest 1
	  bgt.un.s	nested							&nest nest
	*/

	mono_mb_patch_short_branch (mb, owned_branch);
	mono_mb_emit_ldloc (mb, syncp_loc);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoThreadsSync, nest));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_byte (mb, CEE_LDC_I4_1);
	nested_branch = mono_mb_emit_short_branch (mb, CEE_BGT_UN_S);

	/*
	  pop									&nest
	  pop
	  ldloc		syncp							syncp
	  ldc.i4	G_STRUCT_OFFSET(MonoThreadsSync, entry_count)		syncp off
	  add									&count
	  ldind.i4								count
	  brtrue.s	has_waiting
	*/

	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_ldloc (mb, syncp_loc);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoThreadsSync, entry_count));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	has_waiting_branch = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

	/*
	  ldloc		syncp							syncp
	  ldc.i4	G_STRUCT_OFFSET(MonoThreadsSync, owner)			syncp off
	  add									&owner
	  ldnull								&owner 0
	  stind.i
	  ret
	*/

	mono_mb_emit_ldloc (mb, syncp_loc);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoThreadsSync, owner));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_I);
	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 nested:
	  ldc.i4	1							&nest nest 1
	  sub									&nest nest-
	  stind.i4
	  ret
	*/

	mono_mb_patch_short_branch (mb, nested_branch);
	mono_mb_emit_byte (mb, CEE_LDC_I4_1);
	mono_mb_emit_byte (mb, CEE_SUB);
	mono_mb_emit_byte (mb, CEE_STIND_I4);
	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 obj_null, has_waiting:
	  ldarg		0							obj
	  call		System.Threading.Monitor.Exit
	  ret
	 */

	if (thin_hash_branch)
		mono_mb_patch_short_branch (mb, thin_hash_branch);
	mono_mb_patch_short_branch (mb, obj_null_branch);
	mono_mb_patch_short_branch (mb, has_waiting_branch);
	mono_mb_emit_byte (mb, CEE_LDARG_0);
	mono_mb_emit_managed_call (mb, monitor_exit_method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	res = register_fastpath (mono_mb_create_method (mb, mono_signature_no_pinvoke (monitor_exit_method), 5), FASTPATH_EXIT);
	mono_mb_free (mb);

	info = mono_image_alloc0 (mono_defaults.corlib, sizeof (WrapperInfo));
	info->subtype = WRAPPER_SUBTYPE_FAST_MONITOR_EXIT;
	mono_marshal_set_wrapper_info (res, info);

	return res;
}

MonoMethod*
mono_monitor_get_fast_path (MonoMethod *enter_or_exit)
{
	if (strcmp (enter_or_exit->name, "Enter") == 0)
		return mono_monitor_get_fast_enter_method (enter_or_exit);
	if (strcmp (enter_or_exit->name, "Exit") == 0)
		return mono_monitor_get_fast_exit_method (enter_or_exit);
	g_assert_not_reached ();
	return NULL;
}

/*
 * mono_monitor_threads_sync_member_offset:
 * @owner_offset: returns size and offset of the "owner" member
 * @nest_offset: returns size and offset of the "nest" member
 * @entry_count_offset: returns size and offset of the "entry_count" member
 *
 * Returns the offsets and sizes of three members of the
 * MonoThreadsSync struct.  The Monitor ASM fastpaths need this.
 */
void
mono_monitor_threads_sync_members_offset (int *owner_offset, int *nest_offset, int *entry_count_offset)
{
	MonoThreadsSync ts;

#define ENCODE_OFF_SIZE(o,s)	(((o) << 8) | ((s) & 0xff))

	*owner_offset = ENCODE_OFF_SIZE (G_STRUCT_OFFSET (MonoThreadsSync, owner), sizeof (ts.owner));
	*nest_offset = ENCODE_OFF_SIZE (G_STRUCT_OFFSET (MonoThreadsSync, nest), sizeof (ts.nest));
	*entry_count_offset = ENCODE_OFF_SIZE (G_STRUCT_OFFSET (MonoThreadsSync, entry_count), sizeof (ts.entry_count));
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

gboolean 
ves_icall_System_Threading_Monitor_Monitor_test_owner (MonoObject *obj)
{
	MonoThreadsSync *mon;
	
	LOCK_DEBUG (g_message ("%s: Testing if %p is owned by thread %d", __func__, obj, GetCurrentThreadId()));

	mon = obj->synchronisation;
#ifdef HAVE_MOVING_COLLECTOR
	{
		LockWord lw;
		lw.sync = mon;
		if (lw.lock_word & LOCK_WORD_THIN_HASH)
			return FALSE;
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		mon = lw.sync;
	}
#endif
	if (mon == NULL) {
		return FALSE;
	}
	
	if(mon->owner==GetCurrentThreadId ()) {
		return(TRUE);
	}
	
	return(FALSE);
}

gboolean 
ves_icall_System_Threading_Monitor_Monitor_test_synchronised (MonoObject *obj)
{
	MonoThreadsSync *mon;

	LOCK_DEBUG (g_message("%s: (%d) Testing if %p is owned by any thread", __func__, GetCurrentThreadId (), obj));
	
	mon = obj->synchronisation;
#ifdef HAVE_MOVING_COLLECTOR
	{
		LockWord lw;
		lw.sync = mon;
		if (lw.lock_word & LOCK_WORD_THIN_HASH)
			return FALSE;
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		mon = lw.sync;
	}
#endif
	if (mon == NULL) {
		return FALSE;
	}
	
	if (mon->owner != 0) {
		return TRUE;
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
	MonoThreadsSync *mon;
	
	LOCK_DEBUG (g_message ("%s: (%d) Pulsing %p", __func__, GetCurrentThreadId (), obj));
	
	mon = obj->synchronisation;
#ifdef HAVE_MOVING_COLLECTOR
	{
		LockWord lw;
		lw.sync = mon;
		if (lw.lock_word & LOCK_WORD_THIN_HASH) {
			mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
			return;
		}
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		mon = lw.sync;
	}
#endif
	if (mon == NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
		return;
	}
	if (mon->owner != GetCurrentThreadId ()) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked by this thread"));
		return;
	}

	LOCK_DEBUG (g_message ("%s: (%d) %d threads waiting", __func__, GetCurrentThreadId (), g_slist_length (mon->wait_list)));
	
	if (mon->wait_list != NULL) {
		LOCK_DEBUG (g_message ("%s: (%d) signalling and dequeuing handle %p", __func__, GetCurrentThreadId (), mon->wait_list->data));
	
		SetEvent (mon->wait_list->data);
		mon->wait_list = g_slist_remove (mon->wait_list, mon->wait_list->data);
	}
}

void
ves_icall_System_Threading_Monitor_Monitor_pulse_all (MonoObject *obj)
{
	MonoThreadsSync *mon;
	
	LOCK_DEBUG (g_message("%s: (%d) Pulsing all %p", __func__, GetCurrentThreadId (), obj));

	mon = obj->synchronisation;
#ifdef HAVE_MOVING_COLLECTOR
	{
		LockWord lw;
		lw.sync = mon;
		if (lw.lock_word & LOCK_WORD_THIN_HASH) {
			mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
			return;
		}
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		mon = lw.sync;
	}
#endif
	if (mon == NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
		return;
	}
	if (mon->owner != GetCurrentThreadId ()) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked by this thread"));
		return;
	}

	LOCK_DEBUG (g_message ("%s: (%d) %d threads waiting", __func__, GetCurrentThreadId (), g_slist_length (mon->wait_list)));

	while (mon->wait_list != NULL) {
		LOCK_DEBUG (g_message ("%s: (%d) signalling and dequeuing handle %p", __func__, GetCurrentThreadId (), mon->wait_list->data));
	
		SetEvent (mon->wait_list->data);
		mon->wait_list = g_slist_remove (mon->wait_list, mon->wait_list->data);
	}
}

gboolean
ves_icall_System_Threading_Monitor_Monitor_wait (MonoObject *obj, guint32 ms)
{
	MonoThreadsSync *mon;
	HANDLE event;
	guint32 nest;
	guint32 ret;
	gboolean success = FALSE;
	gint32 regain;
	MonoInternalThread *thread = mono_thread_internal_current ();

	LOCK_DEBUG (g_message ("%s: (%d) Trying to wait for %p with timeout %dms", __func__, GetCurrentThreadId (), obj, ms));
	
	mon = obj->synchronisation;
#ifdef HAVE_MOVING_COLLECTOR
	{
		LockWord lw;
		lw.sync = mon;
		if (lw.lock_word & LOCK_WORD_THIN_HASH) {
			mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
			return FALSE;
		}
		lw.lock_word &= ~LOCK_WORD_BITS_MASK;
		mon = lw.sync;
	}
#endif
	if (mon == NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
		return FALSE;
	}
	if (mon->owner != GetCurrentThreadId ()) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked by this thread"));
		return FALSE;
	}

	/* Do this WaitSleepJoin check before creating the event handle */
	mono_thread_current_check_pending_interrupt ();
	
	event = CreateEvent (NULL, FALSE, FALSE, NULL);
	if (event == NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Failed to set up wait event"));
		return FALSE;
	}
	
	LOCK_DEBUG (g_message ("%s: (%d) queuing handle %p", __func__, GetCurrentThreadId (), event));

	mono_thread_current_check_pending_interrupt ();
	
	mono_thread_set_state (thread, ThreadState_WaitSleepJoin);

	mon->wait_list = g_slist_append (mon->wait_list, event);
	
	/* Save the nest count, and release the lock */
	nest = mon->nest;
	mon->nest = 1;
	mono_monitor_exit (obj);

	LOCK_DEBUG (g_message ("%s: (%d) Unlocked %p lock %p", __func__, GetCurrentThreadId (), obj, mon));

	/* There's no race between unlocking mon and waiting for the
	 * event, because auto reset events are sticky, and this event
	 * is private to this thread.  Therefore even if the event was
	 * signalled before we wait, we still succeed.
	 */
	ret = WaitForSingleObjectEx (event, ms, TRUE);

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
		regain = mono_monitor_try_enter_internal (obj, INFINITE, TRUE);
		if (regain == -1) 
			mono_thread_interruption_checkpoint ();
	} while (regain == -1);

	if (regain == 0) {
		/* Something went wrong, so throw a
		 * SynchronizationLockException
		 */
		CloseHandle (event);
		mono_raise_exception (mono_get_exception_synchronization_lock ("Failed to regain lock"));
		return FALSE;
	}

	mon->nest = nest;

	LOCK_DEBUG (g_message ("%s: (%d) Regained %p lock %p", __func__, GetCurrentThreadId (), obj, mon));

	if (ret == WAIT_TIMEOUT) {
		/* Poll the event again, just in case it was signalled
		 * while we were trying to regain the monitor lock
		 */
		ret = WaitForSingleObjectEx (event, 0, FALSE);
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
		LOCK_DEBUG (g_message ("%s: (%d) Success", __func__, GetCurrentThreadId ()));
		success = TRUE;
	} else {
		LOCK_DEBUG (g_message ("%s: (%d) Wait failed, dequeuing handle %p", __func__, GetCurrentThreadId (), event));
		/* No pulse, so we have to remove ourself from the
		 * wait queue
		 */
		mon->wait_list = g_slist_remove (mon->wait_list, event);
	}
	CloseHandle (event);
	
	return success;
}

