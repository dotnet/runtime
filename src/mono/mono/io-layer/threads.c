/*
 * threads.c:  Thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Ximian, Inc.
 */

#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <string.h>
#include <mono/os/gc_wrapper.h>
#include <pthread.h>
#include <signal.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <unistd.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/mono-spinlock.h>
#include <mono/io-layer/mutex-private.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#undef DEBUG
#undef TLS_DEBUG

/* Hash threads with tids. I thought of using TLS for this, but that
 * would have to set the data in the new thread, which is more hassle
 */
static mono_once_t thread_hash_once = MONO_ONCE_INIT;
static pthread_key_t thread_hash_key;

/* This key is used with attached threads and a destructor to signal
 * when attached threads exit, as they don't have the thread_exit()
 * infrastructure
 */
static pthread_key_t thread_attached_key;

struct _WapiHandleOps _wapi_thread_ops = {
	NULL,				/* close */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
	NULL,				/* special_wait */
	NULL				/* prewait */
};

static mono_once_t thread_ops_once=MONO_ONCE_INIT;

static void thread_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_THREAD,
					    WAPI_HANDLE_CAP_WAIT);
}

void _wapi_thread_cleanup (void)
{
	int ret;
	
	ret = pthread_key_delete (thread_hash_key);
	g_assert (ret == 0);
	
	ret = pthread_key_delete (thread_attached_key);
	g_assert (ret == 0);
}

/* Called by thread_exit(), but maybe indirectly by
 * mono_thread_manage() via mono_thread_signal_self() too
 */
static void _wapi_thread_abandon_mutexes (gpointer handle)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	int i;
	pid_t pid = _wapi_getpid ();
	pthread_t tid = pthread_self ();
	
#ifdef DEBUG
	g_message ("%s: Thread %p abandoning held mutexes", __func__, handle);
#endif

	if (handle == NULL) {
		handle = _wapi_thread_handle_from_id (pthread_self ());
		if (handle == NULL) {
			/* Something gone badly wrong... */
			return;
		}
	}
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return;
	}
	
	if (!pthread_equal (thread_handle->id, tid)) {
		return;
	}
	
	for (i = 0; i < thread_handle->owned_mutexes->len; i++) {
		gpointer mutex = g_ptr_array_index (thread_handle->owned_mutexes, i);
		
		_wapi_mutex_abandon (mutex, pid, tid);
		_wapi_thread_disown_mutex (mutex);
	}
}

void _wapi_thread_set_termination_details (gpointer handle,
					   guint32 exitstatus)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	int thr_ret;
	
	if (_wapi_handle_issignalled (handle) ||
	    _wapi_handle_type (handle) == WAPI_HANDLE_UNUSED) {
		/* We must have already deliberately finished with
		 * this thread, so don't do any more now
		 */
		return;
	}

#ifdef DEBUG
	g_message ("%s: Thread %p terminating", __func__, handle);
#endif

	_wapi_thread_abandon_mutexes (handle);
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);

		return;
	}
	
	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	thread_handle->exitstatus = exitstatus;
	thread_handle->state = THREAD_STATE_EXITED;
	MONO_SEM_DESTROY (&thread_handle->suspend_sem);
	g_ptr_array_free (thread_handle->owned_mutexes, TRUE);

	_wapi_handle_set_signal_state (handle, TRUE, TRUE);

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
#ifdef DEBUG
	g_message("%s: Recording thread handle %p id %ld status as %d",
		  __func__, handle, thread_handle->id, exitstatus);
#endif
	
	/* The thread is no longer active, so unref it */
	_wapi_handle_unref (handle);
}

void _wapi_thread_signal_self (guint32 exitstatus)
{
	gpointer handle;
	
	handle = _wapi_thread_handle_from_id (pthread_self ());
	if (handle == NULL) {
		/* Something gone badly wrong... */
		return;
	}
	
	_wapi_thread_set_termination_details (handle, exitstatus);
}

/* Called by the thread creation code as a thread is finishing up, and
 * by ExitThread()
*/
static void thread_exit (guint32 exitstatus, gpointer handle) G_GNUC_NORETURN;
static void thread_exit (guint32 exitstatus, gpointer handle)
{
	_wapi_thread_set_termination_details (handle, exitstatus);
	
	/* Call pthread_exit() to call destructors and really exit the
	 * thread
	 */
	pthread_exit (NULL);
}

static void thread_attached_exit (gpointer handle)
{
	/* Drop the extra reference we take in thread_attach, now this
	 * thread is dead
	 */
	
	_wapi_thread_set_termination_details (handle, 0);
}

static void thread_hash_init(void)
{
	int thr_ret;
	
	thr_ret = pthread_key_create (&thread_hash_key, NULL);
	g_assert (thr_ret == 0);

	thr_ret = pthread_key_create (&thread_attached_key,
				      thread_attached_exit);
	g_assert (thr_ret == 0);
}

static void _wapi_thread_suspend (struct _WapiHandle_thread *thread)
{
	g_assert (pthread_equal (thread->id, pthread_self ()));
	
	while (MONO_SEM_WAIT (&thread->suspend_sem) != 0 &&
	       errno == EINTR);
}

static void _wapi_thread_resume (struct _WapiHandle_thread *thread)
{
	MONO_SEM_POST (&thread->suspend_sem);
}

static void *thread_start_routine (gpointer args) G_GNUC_NORETURN;
static void *thread_start_routine (gpointer args)
{
	struct _WapiHandle_thread *thread = (struct _WapiHandle_thread *)args;
	int thr_ret;
	
	thr_ret = pthread_detach (pthread_self ());
	g_assert (thr_ret == 0);

	thr_ret = pthread_setspecific (thread_hash_key,
				       (void *)thread->handle);
	if (thr_ret != 0) {
		/* This is only supposed to happen when Mono is
		   shutting down.  We cannot assert on it, though,
		   because we must not depend on metadata, which is
		   where the shutdown code is.

		   This is a race condition which arises because
		   pthreads don't allow creation of suspended threads.
		   Once Mono is set to shut down no new thread is
		   allowed to start, even though threads may still be
		   created.  We emulate suspended threads in this
		   function by calling _wapi_thread_suspend() below.

		   So it can happen that even though Mono is already
		   shutting down we still end up here, and at this
		   point the thread_hash_key might already be
		   destroyed. */
		pthread_exit (NULL);
	}

	thread->id = pthread_self();

#ifdef DEBUG
	g_message ("%s: started thread id %ld", __func__, thread->id);
#endif

	if (thread->create_flags & CREATE_SUSPENDED) {
		_wapi_thread_suspend (thread);
	}
	
	thread_exit (thread->start_routine (thread->start_arg),
		     thread->handle);

#ifndef __GNUC__
	/* Even though we tell gcc that this function doesn't return,
	 * other compilers won't see that.
	 */
	return(NULL);
#endif
}

/**
 * CreateThread:
 * @security: Ignored for now.
 * @stacksize: the size in bytes of the new thread's stack. Use 0 to
 * default to the normal stack size. (Ignored for now).
 * @start: The function that the new thread should start with
 * @param: The parameter to give to @start.
 * @create: If 0, the new thread is ready to run immediately.  If
 * %CREATE_SUSPENDED, the new thread will be in the suspended state,
 * requiring a ResumeThread() call to continue running.
 * @tid: If non-NULL, the ID of the new thread is stored here.  NB
 * this is defined as a DWORD (ie 32bit) in the MS API, but we need to
 * cope with 64 bit IDs for s390x and amd64.
 *
 * Creates a new threading handle.
 *
 * Return value: a new handle, or NULL
 */
gpointer CreateThread(WapiSecurityAttributes *security G_GNUC_UNUSED, guint32 stacksize,
		      WapiThreadStart start, gpointer param, guint32 create,
		      gsize *tid) 
{
	struct _WapiHandle_thread thread_handle = {0}, *thread_handle_p;
	pthread_attr_t attr;
	gpointer handle;
	gboolean ok;
	int ret;
	int thr_ret;
	int i, unrefs = 0;
	gpointer ct_ret = NULL;
	
	mono_once (&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
	if (start == NULL) {
		return(NULL);
	}

	thread_handle.state = THREAD_STATE_START;
	thread_handle.owned_mutexes = g_ptr_array_new ();
	thread_handle.create_flags = create;
	thread_handle.start_routine = start;
	thread_handle.start_arg = param;
	
	handle = _wapi_handle_new (WAPI_HANDLE_THREAD, &thread_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating thread handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		
		return (NULL);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle_p);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		SetLastError (ERROR_GEN_FAILURE);
		
		goto cleanup;
	}

	/* Hold a reference while the thread is active, because we use
	 * the handle to store thread exit information
	 */
	_wapi_handle_ref (handle);
	
	/* Set a 2M stack size.  This is the default on Linux, but BSD
	 * needs it.  (The original bug report from Martin Dvorak <md@9ll.cz>
	 * set the size to 2M-4k.  I don't know why it's short by 4k, so
	 * I'm leaving it as 2M until I'm told differently.)
	 */
	thr_ret = pthread_attr_init(&attr);
	g_assert (thr_ret == 0);
	
	/* defaults of 2Mb for 32bits and 4Mb for 64bits */
	/* temporarily changed to use 1 MB: this allows more threads
	 * to be used, as well as using less virtual memory and so
	 * more is available for the GC heap.
	 */
	if (stacksize == 0){
#if HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND) {
			stacksize = 1 << 20;
		} else {
			stacksize = (SIZEOF_VOID_P / 4) * 1024 * 1024;
		}
#else
		stacksize = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#endif
	}

#ifdef HAVE_PTHREAD_ATTR_SETSTACKSIZE
	thr_ret = pthread_attr_setstacksize(&attr, stacksize);
	g_assert (thr_ret == 0);
#endif

	MONO_SEM_INIT (&thread_handle_p->suspend_sem, 0);
	thread_handle_p->handle = handle;
	
	ret = pthread_create (&thread_handle_p->id, &attr,
			      thread_start_routine, (void *)thread_handle_p);
	if (ret != 0) {
#ifdef DEBUG
		g_message ("%s: Thread create error: %s", __func__,
			   strerror(ret));
#endif

		/* Two, because of the reference we took above */
		unrefs = 2;
		
		goto cleanup;
	}
	ct_ret = handle;
	
#ifdef DEBUG
	g_message("%s: Started thread handle %p ID %ld", __func__, handle,
		  thread_handle_p->id);
#endif
	
	if (tid != NULL) {
#ifdef PTHREAD_POINTER_ID
		/* Don't use GPOINTER_TO_UINT here, it can't cope with
		 * sizeof(void *) > sizeof(uint) when a cast to uint
		 * would overflow
		 */
		*tid = (gsize)(thread_handle_p->id);
#else
		*tid = thread_handle_p->id;
#endif
	}

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	/* Must not call _wapi_handle_unref() with the shared handles
	 * already locked
	 */
	for (i = 0; i < unrefs; i++) {
		_wapi_handle_unref (handle);
	}
	
	return(ct_ret);
}

/* The only time this function is called when tid != pthread_self ()
 * is from OpenThread (), so we can fast-path most cases by just
 * looking up the handle in TLS.  OpenThread () must cope with a NULL
 * return and do a handle search in that case.
 */
gpointer _wapi_thread_handle_from_id (pthread_t tid)
{
	gpointer ret;

	if (pthread_equal (tid, pthread_self ()) &&
	    (ret = pthread_getspecific (thread_hash_key)) != NULL) {
		/* We know the handle */

#ifdef DEBUG
		g_message ("%s: Returning %p for self thread %ld from TLS",
			   __func__, ret, tid);
#endif
		
		return(ret);
	}
	
#ifdef DEBUG
	g_message ("%s: Returning NULL for unknown or non-self thread %ld",
		   __func__, tid);
#endif
		

	return(NULL);
}

static gboolean find_thread_by_id (gpointer handle, gpointer user_data)
{
	pthread_t tid = (pthread_t)user_data;
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	
	/* Ignore threads that have already exited (ie they are signalled) */
	if (_wapi_handle_issignalled (handle) == FALSE) {
		ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
					  (gpointer *)&thread_handle);
		if (ok == FALSE) {
			/* It's possible that the handle has vanished
			 * during the _wapi_search_handle before it
			 * gets here, so don't spam the console with
			 * warnings.
			 */
			return(FALSE);
		}
		
#ifdef DEBUG
		g_message ("%s: looking at thread %ld from process %d", __func__, thread_handle->id, 0);
#endif

		if (pthread_equal (thread_handle->id, tid)) {
#ifdef DEBUG
			g_message ("%s: found the thread we are looking for",
				   __func__);
#endif
			return(TRUE);
		}
	}
	
#ifdef DEBUG
	g_message ("%s: not found %ld, returning FALSE", __func__, tid);
#endif
	
	return(FALSE);
}

/* NB tid is 32bit in MS API, but we need 64bit on amd64 and s390x
 * (and probably others)
 */
gpointer OpenThread (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, gsize tid)
{
	gpointer ret=NULL;
	
	mono_once (&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
#ifdef DEBUG
	g_message ("%s: looking up thread %"G_GSIZE_FORMAT, __func__, tid);
#endif

	ret = _wapi_thread_handle_from_id ((pthread_t)tid);
	if (ret == NULL) {
		/* We need to search for this thread */
		ret = _wapi_search_handle (WAPI_HANDLE_THREAD, find_thread_by_id, (gpointer)tid, NULL, FALSE/*TRUE*/);	/* FIXME: have a proper look at this, me might not need to set search_shared = TRUE */
	} else {
		/* if _wapi_search_handle() returns a found handle, it
		 * refs it itself
		 */
		_wapi_handle_ref (ret);
	}
	
#ifdef DEBUG
	g_message ("%s: returning thread handle %p", __func__, ret);
#endif
	
	return(ret);
}

/**
 * ExitThread:
 * @exitcode: Sets the thread's exit code, which can be read from
 * another thread with GetExitCodeThread().
 *
 * Terminates the calling thread.  A thread can also exit by returning
 * from its start function. When the last thread in a process
 * terminates, the process itself terminates.
 */
void ExitThread(guint32 exitcode)
{
	gpointer thread = _wapi_thread_handle_from_id (pthread_self ());
	
	if (thread != NULL) {
		thread_exit(exitcode, thread);
	} else {
		/* Just blow this thread away */
		pthread_exit (NULL);
	}
}

/**
 * GetExitCodeThread:
 * @handle: The thread handle to query
 * @exitcode: The thread @handle exit code is stored here
 *
 * Finds the exit code of @handle, and stores it in @exitcode.  If the
 * thread @handle is still running, the value stored is %STILL_ACTIVE.
 *
 * Return value: %TRUE, or %FALSE on error.
 */
gboolean GetExitCodeThread(gpointer handle, guint32 *exitcode)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return (FALSE);
	}
	
#ifdef DEBUG
	g_message ("%s: Finding exit status for thread handle %p id %ld",
		   __func__, handle, thread_handle->id);
#endif

	if (exitcode == NULL) {
#ifdef DEBUG
		g_message ("%s: Nowhere to store exit code", __func__);
#endif
		return(FALSE);
	}
	
	if (thread_handle->state != THREAD_STATE_EXITED) {
#ifdef DEBUG
		g_message ("%s: Thread still active (state %d, exited is %d)",
			   __func__, thread_handle->state,
			   THREAD_STATE_EXITED);
#endif
		*exitcode = STILL_ACTIVE;
		return(TRUE);
	}
	
	*exitcode = thread_handle->exitstatus;
	
	return(TRUE);
}

/**
 * GetCurrentThreadId:
 *
 * Looks up the thread ID of the current thread.  This ID can be
 * passed to OpenThread() to create a new handle on this thread.
 *
 * Return value: the thread ID.  NB this is defined as DWORD (ie 32
 * bit) in the MS API, but we need to cope with 64 bit IDs for s390x
 * and amd64.  This doesn't really break the API, it just embraces and
 * extends it on 64bit platforms :)
 */
gsize GetCurrentThreadId(void)
{
	pthread_t tid = pthread_self();
	
#ifdef PTHREAD_POINTER_ID
	/* Don't use GPOINTER_TO_UINT here, it can't cope with
	 * sizeof(void *) > sizeof(uint) when a cast to uint would
	 * overflow
	 */
	return((gsize)tid);
#else
	return(tid);
#endif
}

static gpointer thread_attach(gsize *tid)
{
	struct _WapiHandle_thread thread_handle = {0}, *thread_handle_p;
	gpointer handle;
	gboolean ok;
	int thr_ret;
	
	mono_once (&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);

	thread_handle.state = THREAD_STATE_START;
	thread_handle.owned_mutexes = g_ptr_array_new ();

	handle = _wapi_handle_new (WAPI_HANDLE_THREAD, &thread_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating thread handle", __func__);
		
		SetLastError (ERROR_GEN_FAILURE);
		return (NULL);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle_p);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		
		SetLastError (ERROR_GEN_FAILURE);
		goto cleanup;
	}

	/* Hold a reference while the thread is active, because we use
	 * the handle to store thread exit information
	 */
	_wapi_handle_ref (handle);

	/* suspend_sem is not used for attached threads, but
	 * thread_exit() might try to destroy it
	 */
	MONO_SEM_INIT (&thread_handle_p->suspend_sem, 0);
	thread_handle_p->handle = handle;
	thread_handle_p->id = pthread_self ();

	thr_ret = pthread_setspecific (thread_hash_key, (void *)handle);
	g_assert (thr_ret == 0);

	thr_ret = pthread_setspecific (thread_attached_key, (void *)handle);
	g_assert (thr_ret == 0);
	
#ifdef DEBUG
	g_message("%s: Attached thread handle %p ID %ld", __func__, handle,
		  thread_handle_p->id);
#endif

	if (tid != NULL) {
#ifdef PTHREAD_POINTER_ID
		/* Don't use GPOINTER_TO_UINT here, it can't cope with
		 * sizeof(void *) > sizeof(uint) when a cast to uint
		 * would overflow
		 */
		*tid = (gsize)(thread_handle_p->id);
#else
		*tid = thread_handle_p->id;
#endif
	}

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(handle);
}

gpointer _wapi_thread_duplicate ()
{
	gpointer ret = NULL;
	
	mono_once (&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
	ret = _wapi_thread_handle_from_id (pthread_self ());
	if (!ret) {
		ret = thread_attach (NULL);
	} else {
		_wapi_handle_ref (ret);
	}
	
	return(ret);
}

/**
 * GetCurrentThread:
 *
 * Looks up the handle associated with the current thread.  Under
 * Windows this is a pseudohandle, and must be duplicated with
 * DuplicateHandle() for some operations.
 *
 * Return value: The current thread handle, or %NULL on failure.
 * (Unknown whether Windows has a possible failure here.  It may be
 * necessary to implement the pseudohandle-constant behaviour).
 */
gpointer GetCurrentThread(void)
{
	mono_once(&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
	return(_WAPI_THREAD_CURRENT);
}

/**
 * ResumeThread:
 * @handle: the thread handle to resume
 *
 * Decrements the suspend count of thread @handle. A thread can only
 * run if its suspend count is zero.
 *
 * Return value: the previous suspend count, or 0xFFFFFFFF on error.
 */
guint32 ResumeThread(gpointer handle)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		
		return (0xFFFFFFFF);
	}

	/* This is still a kludge that only copes with starting a
	 * thread that was suspended on create, so don't bother with
	 * the suspend count crap yet
	 */
	_wapi_thread_resume (thread_handle);
	return(0xFFFFFFFF);
}

/**
 * SuspendThread:
 * @handle: the thread handle to suspend
 *
 * Increments the suspend count of thread @handle. A thread can only
 * run if its suspend count is zero.
 *
 * Return value: the previous suspend count, or 0xFFFFFFFF on error.
 */
guint32 SuspendThread(gpointer handle)
{
	return(0xFFFFFFFF);
}

/*
 * We assume here that TLS_MINIMUM_AVAILABLE is less than
 * PTHREAD_KEYS_MAX, allowing enough overhead for a few TLS keys for
 * library usage.
 *
 * Currently TLS_MINIMUM_AVAILABLE is 64 and _POSIX_THREAD_KEYS_MAX
 * (the minimum value for PTHREAD_KEYS_MAX) is 128, so we should be
 * fine.
 */

static pthread_key_t TLS_keys[TLS_MINIMUM_AVAILABLE];
static gboolean TLS_used[TLS_MINIMUM_AVAILABLE]={FALSE};
static guint32 TLS_spinlock=0;

guint32
mono_pthread_key_for_tls (guint32 idx)
{
	return (guint32)TLS_keys [idx];
}

/**
 * TlsAlloc:
 *
 * Allocates a Thread Local Storage (TLS) index.  Any thread in the
 * same process can use this index to store and retrieve values that
 * are local to that thread.
 *
 * Return value: The index value, or %TLS_OUT_OF_INDEXES if no index
 * is available.
 */
guint32 TlsAlloc(void)
{
	guint32 i;
	int thr_ret;
	
	MONO_SPIN_LOCK (TLS_spinlock);
	
	for(i=0; i<TLS_MINIMUM_AVAILABLE; i++) {
		if(TLS_used[i]==FALSE) {
			TLS_used[i]=TRUE;
			thr_ret = pthread_key_create(&TLS_keys[i], NULL);
			g_assert (thr_ret == 0);

			MONO_SPIN_UNLOCK (TLS_spinlock);
	
#ifdef TLS_DEBUG
			g_message ("%s: returning key %d", __func__, i);
#endif
			
			return(i);
		}
	}

	MONO_SPIN_UNLOCK (TLS_spinlock);
	
#ifdef TLS_DEBUG
	g_message ("%s: out of indices", __func__);
#endif
			
	
	return(TLS_OUT_OF_INDEXES);
}

#define MAKE_GC_ID(idx) (GUINT_TO_POINTER((idx)|(GetCurrentThreadId()<<8)))

/**
 * TlsFree:
 * @idx: The TLS index to free
 *
 * Releases a TLS index, making it available for reuse.  This call
 * will delete any TLS data stored under index @idx in all threads.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean TlsFree(guint32 idx)
{
	int thr_ret;
	
#ifdef TLS_DEBUG
	g_message ("%s: freeing key %d", __func__, idx);
#endif

	MONO_SPIN_LOCK (TLS_spinlock);
	
	if(TLS_used[idx]==FALSE) {
		MONO_SPIN_UNLOCK (TLS_spinlock);

		return(FALSE);
	}
	
	TLS_used[idx]=FALSE;
	thr_ret = pthread_key_delete(TLS_keys[idx]);
	g_assert (thr_ret == 0);
	
	MONO_SPIN_UNLOCK (TLS_spinlock);
	
	return(TRUE);
}

/**
 * TlsGetValue:
 * @idx: The TLS index to retrieve
 *
 * Retrieves the TLS data stored under index @idx.
 *
 * Return value: The value stored in the TLS index @idx in the current
 * thread, or %NULL on error.  As %NULL can be a valid return value,
 * in this case GetLastError() returns %ERROR_SUCCESS.
 */
gpointer TlsGetValue(guint32 idx)
{
	gpointer ret;
	
#ifdef TLS_DEBUG
	g_message ("%s: looking up key %d", __func__, idx);
#endif
	
	ret=pthread_getspecific(TLS_keys[idx]);

#ifdef TLS_DEBUG
	g_message ("%s: returning %p", __func__, ret);
#endif
	
	return(ret);
}

/**
 * TlsSetValue:
 * @idx: The TLS index to store
 * @value: The value to store under index @idx
 *
 * Stores @value at TLS index @idx.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean TlsSetValue(guint32 idx, gpointer value)
{
	int ret;

#ifdef TLS_DEBUG
	g_message ("%s: setting key %d to %p", __func__, idx, value);
#endif
	
	MONO_SPIN_LOCK (TLS_spinlock);
	
	if(TLS_used[idx]==FALSE) {
#ifdef TLS_DEBUG
		g_message ("%s: key %d unused", __func__, idx);
#endif

		MONO_SPIN_UNLOCK (TLS_spinlock);

		return(FALSE);
	}
	
	ret=pthread_setspecific(TLS_keys[idx], value);
	if(ret!=0) {
#ifdef TLS_DEBUG
		g_message ("%s: pthread_setspecific error: %s", __func__,
			   strerror (ret));
#endif

		MONO_SPIN_UNLOCK (TLS_spinlock);

		return(FALSE);
	}
	
	MONO_SPIN_UNLOCK (TLS_spinlock);
	
	return(TRUE);
}

/**
 * SleepEx:
 * @ms: The time in milliseconds to suspend for
 * @alertable: if TRUE, the wait can be interrupted by an APC call
 *
 * Suspends execution of the current thread for @ms milliseconds.  A
 * value of zero causes the thread to relinquish its time slice.  A
 * value of %INFINITE causes an infinite delay.
 */
guint32 SleepEx(guint32 ms, gboolean alertable)
{
	struct timespec req, rem;
	int ms_quot, ms_rem;
	int ret;
	gpointer current_thread = NULL;
	
#ifdef DEBUG
	g_message("%s: Sleeping for %d ms", __func__, ms);
#endif

	if (alertable) {
		current_thread = _wapi_thread_handle_from_id (pthread_self ());
		if (current_thread == NULL) {
			SetLastError (ERROR_INVALID_HANDLE);
			return(WAIT_FAILED);
		}
		
		if (_wapi_thread_apc_pending (current_thread)) {
			_wapi_thread_dispatch_apc_queue (current_thread);
			return WAIT_IO_COMPLETION;
		}
	}
	
	if(ms==0) {
		sched_yield();
		return 0;
	}
	
	/* FIXME: check for INFINITE and sleep forever */
	ms_quot = ms / 1000;
	ms_rem = ms % 1000;
	
	req.tv_sec=ms_quot;
	req.tv_nsec=ms_rem*1000000;
	
again:
	ret=nanosleep(&req, &rem);

	if (alertable && _wapi_thread_apc_pending (current_thread)) {
		_wapi_thread_dispatch_apc_queue (current_thread);
		return WAIT_IO_COMPLETION;
	}
	
	if(ret==-1) {
		/* Sleep interrupted with rem time remaining */
#ifdef DEBUG
		guint32 rems=rem.tv_sec*1000 + rem.tv_nsec/1000000;
		
		g_message("%s: Still got %d ms to go", __func__, rems);
#endif
		req=rem;
		goto again;
	}

	return 0;
}

void Sleep(guint32 ms)
{
	SleepEx(ms, FALSE);
}

gboolean _wapi_thread_cur_apc_pending (void)
{
	gpointer thread = _wapi_thread_handle_from_id (pthread_self ());
	
	if (thread == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(_wapi_thread_apc_pending (thread));
}

gboolean _wapi_thread_apc_pending (gpointer handle)
{
	struct _WapiHandle_thread *thread;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread);
	if (ok == FALSE) {
#ifdef DEBUG
		/* This might happen at process shutdown, as all
		 * thread handles are forcibly closed.  If a thread
		 * still has an alertable wait the final
		 * _wapi_thread_apc_pending check will probably fail
		 * to find the handle
		 */
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
#endif
		return (FALSE);
	}
	
	return(thread->has_apc);
}

gboolean _wapi_thread_dispatch_apc_queue (gpointer handle)
{
	/* We don't support calling APC functions */
	struct _WapiHandle_thread *thread;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread);
	g_assert (ok);

	thread->has_apc = FALSE;

	return(TRUE);
}

/*
 * In this implementation, APC_CALLBACK is ignored, HANDLE can only refer to the current
 * thread, and the only effect this function has that if called from a signal handler,
 * and the thread was waiting when receiving the signal, the wait will be broken after
 * the signal handler returns.
 * These limitations are not a problem as the runtime only uses this functionality.
 * This function is async-signal-safe.
 */
guint32 QueueUserAPC (WapiApcProc apc_callback, gpointer handle, 
		      gpointer param)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return (0);
	}

	g_assert (thread_handle->id == GetCurrentThreadId ());

	/* No locking/memory barriers are needed here */
	thread_handle->has_apc = TRUE;

	return(1);
}

void _wapi_thread_own_mutex (gpointer mutex)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	gpointer thread;
	
	thread = _wapi_thread_handle_from_id (pthread_self ());
	if (thread == NULL) {
		g_warning ("%s: error looking up thread by ID", __func__);
		return;
	}

	ok = _wapi_lookup_handle (thread, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   thread);
		return;
	}

	_wapi_handle_ref (mutex);
	
	g_ptr_array_add (thread_handle->owned_mutexes, mutex);
}

void _wapi_thread_disown_mutex (gpointer mutex)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	gpointer thread;

	thread = _wapi_thread_handle_from_id (pthread_self ());
	if (thread == NULL) {
		g_warning ("%s: error looking up thread by ID", __func__);
		return;
	}

	ok = _wapi_lookup_handle (thread, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   thread);
		return;
	}

	_wapi_handle_unref (mutex);
	
	g_ptr_array_remove (thread_handle->owned_mutexes, mutex);
}
