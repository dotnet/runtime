/*
 * monitor.c:  Monitor locking functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/monitor.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/exception.h>
#include <mono/io-layer/io-layer.h>

#include <mono/os/gc_wrapper.h>

#undef THREAD_LOCK_DEBUG

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


static void mon_finalize (void *o, void *unused)
{
	MonoThreadsSync *mon=(MonoThreadsSync *)o;
	
#ifdef THREAD_LOCK_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Finalizing sync %p", mon);
#endif

	if(mon->entry_sem!=NULL) {
		CloseHandle (mon->entry_sem);
	}
	/* If this isn't empty then something is seriously broken - it
	 * means a thread is still waiting on the object that owned
	 * this lock, but the object has been finalized.
	 */
	g_assert (mon->wait_list==NULL);
}

static MonoThreadsSync *mon_new(guint32 id)
{
	MonoThreadsSync *new;
	
#if HAVE_BOEHM_GC
	new=(MonoThreadsSync *)GC_MALLOC (sizeof(MonoThreadsSync));
	GC_REGISTER_FINALIZER (new, mon_finalize, NULL, NULL, NULL);
#else
	/* This should be freed when the object that owns it is
	 * deleted
	 */
	new=(MonoThreadsSync *)g_new0 (MonoThreadsSync, 1);
#endif
	new->owner=id;
	new->nest=1;
	
	return(new);
}

gboolean mono_monitor_try_enter (MonoObject *obj, guint32 ms)
{
	MonoThreadsSync *mon;
	guint32 id=GetCurrentThreadId ();
	HANDLE sem;
	guint32 then, now, delta;
	guint32 waitms;
	guint32 ret;
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": (%d) Trying to lock object %p (%d ms)", id, obj, ms);
#endif

retry:
	mon=obj->synchronisation;

	/* If the object has never been locked... */
	if(mon==NULL) {
		mon=mon_new(id);
		if(InterlockedCompareExchangePointer ((gpointer*)&obj->synchronisation, mon, NULL)==NULL) {
			/* Successfully locked */
			return(TRUE);
		} else {
			/* Another thread got in first, so try again.
			 * GC will take care of the monitor record
			 */
#ifndef HAVE_BOEHM_GC
			mon_finalize (mon, NULL);
#endif
			goto retry;
		}
	}

	/* If the object is currently locked by this thread... */
	if(mon->owner==id) {
		mon->nest++;
		return(TRUE);
	}

	/* If the object has previously been locked but isn't now... */

	/* This case differs from Dice's case 3 because we don't
	 * deflate locks or cache unused lock records
	 */
	if(mon->owner==0) {
		/* Try to install our ID in the owner field, nest
		 * should have been left at 1 by the previous unlock
		 * operation
		 */
		if(InterlockedCompareExchange (&mon->owner, id, 0)==0) {
			/* Success */
			g_assert (mon->nest==1);
			return(TRUE);
		} else {
			/* Trumped again! */
			goto retry;
		}
	}

	/* The object must be locked by someone else... */

	/* If ms is 0 we don't block, but just fail straight away */
	if(ms==0) {
#ifdef THREAD_LOCK_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": (%d) timed out, returning FALSE", id);
#endif

		return(FALSE);
	}

	/* The slow path begins here.  We need to make sure theres a
	 * semaphore handle (creating it if necessary), and block on
	 * it
	 */
	if(mon->entry_sem==NULL) {
		/* Create the semaphore */
		sem=CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
		if(InterlockedCompareExchangePointer ((gpointer*)&mon->entry_sem, sem, NULL)!=NULL) {
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
	if(ms!=INFINITE) {
		then=GetTickCount ();
		if(ms<100) {
			waitms=ms;
		} else {
			waitms=100;
		}
	} else {
		waitms=100;
	}
	
	InterlockedIncrement (&mon->entry_count);
	ret=WaitForSingleObject (mon->entry_sem, waitms);
	InterlockedDecrement (&mon->entry_count);
	
	if(ms!=INFINITE) {
		now=GetTickCount ();
		
		if(now<then) {
			/* The counter must have wrapped around */
#ifdef THREAD_LOCK_DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": wrapped around! now=0x%x then=0x%x",
				   now, then);
#endif
			
			now+=(0xffffffff - then);
			then=0;

#ifdef THREAD_LOCK_DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": wrap rejig: now=0x%x then=0x%x delta=0x%x", now, then, now-then);
#endif
		}
		
		delta=now-then;
		if(delta >= ms) {
			ms=0;
		} else {
			ms-=delta;
		}

		if(ret==WAIT_TIMEOUT && ms>0) {
			/* More time left */
			goto retry;
		}
	} else {
		if(ret==WAIT_TIMEOUT) {
			/* Infinite wait, so just try again */
			goto retry;
		}
	}
	
	if(ret==WAIT_OBJECT_0) {
		/* retry from the top */
		goto retry;
	}
	
	/* We must have timed out */
#ifdef THREAD_LOCK_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": (%d) timed out waiting, returning FALSE", id);
#endif

	return(FALSE);
}

void mono_monitor_exit (MonoObject *obj)
{
	MonoThreadsSync *mon;
	guint32 nest;
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Unlocking %p",
		  GetCurrentThreadId (), obj);
#endif

	mon=obj->synchronisation;

	if(mon==NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
		return;
	}
	if(mon->owner!=GetCurrentThreadId ()) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked by this thread"));
		return;
	}
	
	nest=mon->nest-1;
	if(nest==0) {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": (%d) Object %p is now unlocked",
			  GetCurrentThreadId (), obj);
#endif
	
		/* object is now unlocked, leave nest==1 so we don't
		 * need to set it when the lock is reacquired
		 */
		mon->owner=0;

		/* Do the wakeup stuff.  It's possible that the last
		 * blocking thread gave up waiting just before we
		 * release the semaphore resulting in a futile wakeup
		 * next time there's contention for this object, but
		 * it means we don't have to waste time locking the
		 * struct.
		 */
		if(mon->entry_count>0) {
			ReleaseSemaphore (mon->entry_sem, 1, NULL);
		}
	} else {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": (%d) Object %p is now locked %d times",
			  GetCurrentThreadId (), obj,
			  nest);
#endif
		mon->nest=nest;
	}
}

gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj,
							      guint32 ms)
{
	MONO_ARCH_SAVE_REGS;

	return(mono_monitor_try_enter (obj, ms));
}

void ves_icall_System_Threading_Monitor_Monitor_exit(MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	return(mono_monitor_exit (obj));
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj)
{
	MonoThreadsSync *mon;
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Testing if %p is owned by thread %d", obj,
		  GetCurrentThreadId());
#endif
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		return(FALSE);
	}
	
	if(mon->owner==GetCurrentThreadId ()) {
		return(TRUE);
	}
	
	return(FALSE);
}

gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj)
{
	MonoThreadsSync *mon;
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": (%d) Testing if %p is owned by any thread",
		  GetCurrentThreadId (), obj);
#endif
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		return(FALSE);
	}
	
	if(mon->owner!=0) {
		return(TRUE);
	}
	
	return(FALSE);
}

/* All wait list manipulation in the pulse, pulseall and wait
 * functions happens while the monitor lock is held, so we don't need
 * any extra struct locking
 */

void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj)
{
	MonoThreadsSync *mon;
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Pulsing %p",
		  GetCurrentThreadId (), obj);
#endif
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
		return;
	}
	if(mon->owner!=GetCurrentThreadId ()) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked by this thread"));
		return;
	}

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) %d threads waiting",
		  GetCurrentThreadId (), g_slist_length (mon->wait_list));
#endif
	
	if(mon->wait_list!=NULL) {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": (%d) signalling and dequeuing handle %p",
			  GetCurrentThreadId (), mon->wait_list->data);
#endif
	
		SetEvent (mon->wait_list->data);
		mon->wait_list=g_slist_remove (mon->wait_list,
					       mon->wait_list->data);
	}
}

void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj)
{
	MonoThreadsSync *mon;
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Pulsing all %p",
		  GetCurrentThreadId (), obj);
#endif
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
		return;
	}
	if(mon->owner!=GetCurrentThreadId ()) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked by this thread"));
		return;
	}

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) %d threads waiting",
		  GetCurrentThreadId (), g_slist_length (mon->wait_list));
#endif
	
	while(mon->wait_list!=NULL) {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": (%d) signalling and dequeuing handle %p",
			  GetCurrentThreadId (), mon->wait_list->data);
#endif
	
		SetEvent (mon->wait_list->data);
		mon->wait_list=g_slist_remove (mon->wait_list,
					       mon->wait_list->data);
	}
}

gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj,
							 guint32 ms)
{
	MonoThreadsSync *mon;
	HANDLE event;
	guint32 nest;
	guint32 ret;
	gboolean success=FALSE, regain;
	
	MONO_ARCH_SAVE_REGS;

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": (%d) Trying to wait for %p with timeout %dms",
		  GetCurrentThreadId (), obj, ms);
#endif
	
	mon=obj->synchronisation;
	if(mon==NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked"));
		return(FALSE);
	}
	if(mon->owner!=GetCurrentThreadId ()) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Not locked by this thread"));
		return(FALSE);
	}
	
	event=CreateEvent (NULL, FALSE, FALSE, NULL);
	if(event==NULL) {
		mono_raise_exception (mono_get_exception_synchronization_lock ("Failed to set up wait event"));
		return(FALSE);
	}
	
#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) queuing handle %p",
		  GetCurrentThreadId (), event);
#endif

	mon->wait_list=g_slist_append (mon->wait_list, event);
	
	/* Save the nest count, and release the lock */
	nest=mon->nest;
	mon->nest=1;
	mono_monitor_exit (obj);

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Unlocked %p lock %p",
		  GetCurrentThreadId (), obj, mon);
#endif

	/* There's no race between unlocking mon and waiting for the
	 * event, because auto reset events are sticky, and this event
	 * is private to this thread.  Therefore even if the event was
	 * signalled before we wait, we still succeed.
	 */
	ret=WaitForSingleObject (event, ms);
	
	/* Regain the lock with the previous nest count */
	regain=mono_monitor_try_enter (obj, INFINITE);
	if(regain==FALSE) {
		/* Something went wrong, so throw a
		 * SynchronizationLockException
		 */
		CloseHandle (event);
		mono_raise_exception (mono_get_exception_synchronization_lock ("Failed to regain lock"));
		return(FALSE);
	}

	mon->nest=nest;

#ifdef THREAD_LOCK_DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Regained %p lock %p",
		  GetCurrentThreadId (), obj, mon);
#endif

	if(ret==WAIT_TIMEOUT) {
		/* Poll the event again, just in case it was signalled
		 * while we were trying to regain the monitor lock
		 */
		ret=WaitForSingleObject (event, 0);
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
	
	if(ret==WAIT_OBJECT_0) {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Success",
			  GetCurrentThreadId ());
#endif
		success=TRUE;
	} else {
#ifdef THREAD_LOCK_DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) Wait failed",
			  GetCurrentThreadId ());
		g_message(G_GNUC_PRETTY_FUNCTION ": (%d) dequeuing handle %p",
			  GetCurrentThreadId (), event);
#endif
		/* No pulse, so we have to remove ourself from the
		 * wait queue
		 */
		mon->wait_list=g_slist_remove (mon->wait_list, event);
	}
	CloseHandle (event);
	
	return(success);
}

