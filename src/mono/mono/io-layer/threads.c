/*
 * threads.c:  Thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <signal.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <unistd.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/timed-thread.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/mono-spinlock.h>
#include <mono/io-layer/mutex-private.h>

#if HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#undef DEBUG
#undef TLS_DEBUG


/* Hash threads with tids. I thought of using TLS for this, but that
 * would have to set the data in the new thread, which is more hassle
 */
static mono_once_t thread_hash_once = MONO_ONCE_INIT;
static mono_mutex_t thread_hash_mutex = MONO_MUTEX_INITIALIZER;
static GHashTable *thread_hash=NULL;

static void thread_close (gpointer handle, gpointer data);
static gboolean thread_own (gpointer handle);

struct _WapiHandleOps _wapi_thread_ops = {
	thread_close,			/* close */
	NULL,				/* signal */
	thread_own,			/* own */
	NULL,				/* is_owned */
};

static mono_once_t thread_ops_once=MONO_ONCE_INIT;

#ifdef WITH_INCLUDED_LIBGC
static void gc_init (void);
#endif

static void thread_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_THREAD,
					    WAPI_HANDLE_CAP_WAIT);

#ifdef WITH_INCLUDED_LIBGC
	gc_init ();
#endif
}

static void thread_close (gpointer handle, gpointer data)
{
	struct _WapiHandle_thread *thread_handle = (struct _WapiHandle_thread *)data;

#ifdef DEBUG
	g_message ("%s: closing thread handle %p", __func__, handle);
#endif

	g_ptr_array_free (thread_handle->owned_mutexes, TRUE);
}

static gboolean thread_own (gpointer handle)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	
#ifdef DEBUG
	g_message ("%s: owning thread handle %p", __func__, handle);
#endif

	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return(FALSE);
	}
	
	if (thread_handle->joined == FALSE) {
		_wapi_timed_thread_join (thread_handle->thread, NULL, NULL);
		thread_handle->joined = TRUE;
	}

	return(TRUE);
}

static void thread_exit(guint32 exitstatus, gpointer handle)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	int thr_ret;
	int i;
	
	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return;
	}
	
	for (i = 0; i < thread_handle->owned_mutexes->len; i++) {
		_wapi_mutex_abandon (g_ptr_array_index (thread_handle->owned_mutexes, i), getpid (), thread_handle->thread->id);
	}

#ifdef DEBUG
	g_message ("%s: Recording thread handle %p exit status", __func__,
		   handle);
#endif
	
	thread_handle->exitstatus = exitstatus;
	thread_handle->state = THREAD_STATE_EXITED;

	_wapi_handle_set_signal_state (handle, TRUE, TRUE);

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
#ifdef DEBUG
	g_message("%s: Recording thread handle %p id %ld status as %d",
		  __func__, handle, thread_handle->thread->id, exitstatus);
#endif

	/* Remove this thread from the hash */
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&thread_hash_mutex);
	thr_ret = mono_mutex_lock(&thread_hash_mutex);
	g_assert (thr_ret == 0);
	
	g_hash_table_remove (thread_hash, (gpointer)(thread_handle->thread->id));

	thr_ret = mono_mutex_unlock(&thread_hash_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

	/* The thread is no longer active, so unref it */
	_wapi_handle_unref (handle);
}

static void thread_hash_init(void)
{
	thread_hash = g_hash_table_new (NULL, NULL);
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
	thread_handle.owner_pid = getpid ();
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
	
	/* Lock around the thread create, so that the new thread cant
	 * race us to look up the thread handle in GetCurrentThread()
	 */
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&thread_hash_mutex);
	thr_ret = mono_mutex_lock(&thread_hash_mutex);
	g_assert (thr_ret == 0);
	
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

	ret = _wapi_timed_thread_create (&thread_handle_p->thread, &attr,
					 create, start, thread_exit, param,
					 handle);
	if (ret != 0) {
#ifdef DEBUG
		g_message ("%s: Thread create error: %s", __func__,
			   strerror(ret));
#endif

		/* Two, because of the reference we took above */
		unrefs = 2;
		
		goto thread_hash_cleanup;
	}
	ct_ret = handle;
	
	g_hash_table_insert (thread_hash,
			     (gpointer)(thread_handle_p->thread->id),
			     handle);
	
#ifdef DEBUG
	g_message("%s: Started thread handle %p thread %p ID %ld", __func__,
		  handle, thread_handle_p->thread,
		  thread_handle_p->thread->id);
#endif
	
	if (tid != NULL) {
#ifdef PTHREAD_POINTER_ID
		/* Don't use GPOINTER_TO_UINT here, it can't cope with
		 * sizeof(void *) > sizeof(uint) when a cast to uint
		 * would overflow
		 */
		*tid = (gsize)(thread_handle_p->thread->id);
#else
		*tid = thread_handle_p->thread->id;
#endif
	}

thread_hash_cleanup:
	thr_ret = mono_mutex_unlock (&thread_hash_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	/* Must not call _wapi_handle_unref() with the handle already
	 * locked
	 */
	for (i = 0; i < unrefs; i++) {
		_wapi_handle_unref (handle);
	}
	
	return(ct_ret);
}

gpointer _wapi_thread_handle_from_id (pthread_t tid)
{
	gpointer ret=NULL;
	int thr_ret;

	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&thread_hash_mutex);
	thr_ret = mono_mutex_lock(&thread_hash_mutex);
	g_assert (thr_ret == 0);
	
	ret = g_hash_table_lookup (thread_hash, (gpointer)(tid));

	thr_ret = mono_mutex_unlock(&thread_hash_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

	return(ret);
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
	if(ret!=NULL) {
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
	_wapi_timed_thread_exit(exitcode);
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
		   __func__, handle, thread_handle->thread->id);
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
	int ret;
	int thr_ret;
	int i, unrefs = 0;
	gpointer ta_ret = NULL;
	
	mono_once (&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);

	thread_handle.state = THREAD_STATE_START;
	thread_handle.owner_pid = getpid ();
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
	
	/* Lock around the thread create, so that the new thread cant
	 * race us to look up the thread handle in GetCurrentThread()
	 */
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&thread_hash_mutex);
	thr_ret = mono_mutex_lock(&thread_hash_mutex);
	g_assert (thr_ret == 0);

	ret = _wapi_timed_thread_attach (&thread_handle_p->thread, thread_exit,
					 handle);
	if (ret != 0) {
#ifdef DEBUG
		g_message ("%s: Thread attach error: %s", __func__,
			   strerror(ret));
#endif

		/* Two, because of the reference we took above */
		unrefs = 2;
		
		goto thread_hash_cleanup;
	}
	ta_ret = handle;
	
	g_hash_table_insert (thread_hash,
			     (gpointer)(thread_handle_p->thread->id),
			     handle);

#ifdef DEBUG
	g_message("%s: Attached thread handle %p thread %p ID %ld", __func__,
		  handle, thread_handle_p->thread,
		  thread_handle_p->thread->id);
#endif

	if (tid != NULL) {
#ifdef PTHREAD_POINTER_ID
		/* Don't use GPOINTER_TO_UINT here, it can't cope with
		 * sizeof(void *) > sizeof(uint) when a cast to uint
		 * would overflow
		 */
		*tid = (gsize)(thread_handle_p->thread->id);
#else
		*tid = thread_handle_p->thread->id;
#endif
	}

thread_hash_cleanup:
	thr_ret = mono_mutex_unlock (&thread_hash_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	/* Must not call _wapi_handle_unref() with the handle already
	 * locked
	 */
	for (i = 0; i < unrefs; i++) {
		_wapi_handle_unref (handle);
	}
	
	return(ta_ret);
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
	gpointer ret=NULL;
	gsize tid;
	
	mono_once(&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
	tid = GetCurrentThreadId();
	
	ret = _wapi_thread_handle_from_id ((pthread_t)tid);
	if (!ret) {
		ret = thread_attach (NULL);
	}

	return(ret);
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
	
	if (thread_handle->thread == NULL) {
		return(0xFFFFFFFF);
	}

#ifdef WITH_INCLUDED_LIBGC
	if (thread_handle->thread->suspend_count <= 1)
		_wapi_timed_thread_resume (thread_handle->thread);
	
	return (--thread_handle->thread->suspend_count));
#else
	/* This is still a kludge that only copes with starting a
	 * thread that was suspended on create, so don't bother with
	 * the suspend count crap yet
	 */
	_wapi_timed_thread_resume (thread_handle->thread);
	return(0xFFFFFFFF);
#endif
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
#ifdef WITH_INCLUDED_LIBGC
	struct _WapiHandle_thread *thread_handle;
	gpointer current;
	gboolean ok;

	current = GetCurrentThread ();
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return (0xFFFFFFFF);
	}
	
	if (thread_handle->thread == NULL) {
		return(0xFFFFFFFF);
	}

	if (!thread_handle->thread->suspend_count) {
		if (handle == current)
			_wapi_timed_thread_suspend (thread_handle->thread);
		else {
			pthread_kill (thread_handle->thread->id, SIGPWR);
			while (MONO_SEM_WAIT (&thread_handle->thread->suspended_sem) != 0) {
				if (errno != EINTR) {
					return(0xFFFFFFFF);
				}
			}
		}
	}

	return (thread_handle->thread->suspend_count++);
#else
	return(0xFFFFFFFF);
#endif
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
		current_thread = GetCurrentThread ();
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
	
	_wapi_timed_thread_queue_apc (thread_handle->thread, apc_callback,
				      param);
	return(1);
}

gboolean _wapi_thread_cur_apc_pending (void)
{
	return(_wapi_thread_apc_pending (GetCurrentThread ()));
}

gboolean _wapi_thread_apc_pending (gpointer handle)
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
	
	return(_wapi_timed_thread_apc_pending (thread_handle->thread));
}

gboolean _wapi_thread_dispatch_apc_queue (gpointer handle)
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
	
	_wapi_timed_thread_dispatch_apc_queue (thread_handle->thread);
	return(1);
}

void _wapi_thread_own_mutex (pthread_t tid, gpointer mutex)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	gpointer thread;

	thread = _wapi_thread_handle_from_id (tid);
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

void _wapi_thread_disown_mutex (pthread_t tid, gpointer mutex)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	gpointer thread;

	thread = _wapi_thread_handle_from_id (tid);
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



#ifdef WITH_INCLUDED_LIBGC

static void GC_suspend_handler (int sig)
{
	struct _WapiHandle_thread *thread_handle;
	gpointer handle;
	gboolean ok;

	handle = GetCurrentThread ();
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return;
	}
	
	thread_handle->thread->stack_ptr = &ok;
	MONO_SEM_POST (&thread_handle->thread->suspended_sem);

	_wapi_timed_thread_suspend (thread_handle->thread);

	thread_handle->thread->stack_ptr = NULL;
}

static void gc_init (void)
{
	struct sigaction act;

	act.sa_handler = GC_suspend_handler;
	g_assert (sigaction (SIGPWR, &act, NULL) == 0);
}

void mono_wapi_push_thread_stack (gpointer handle, gpointer stack_ptr)
{
	struct _WapiHandle_thread *thread_handle;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				  (gpointer *)&thread_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up thread handle %p", __func__,
			   handle);
		return;
	}
	
	GC_push_all_stack (thread_handle->thread->stack_ptr, stack_ptr);
}

#endif /* WITH_INCLUDED_LIBGC */
