/*
 * threads.c:  Thread handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#if HAVE_BOEHM_GC
#include <mono/os/gc_wrapper.h>
#include "mono/utils/mono-hash.h"
#endif
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/timed-thread.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/thread-private.h>
#include <mono/io-layer/mono-spinlock.h>

#undef DEBUG
#undef TLS_DEBUG
#undef TLS_PTHREAD_MUTEX


/* Hash threads with tids. I thought of using TLS for this, but that
 * would have to set the data in the new thread, which is more hassle
 */
static mono_once_t thread_hash_once = MONO_ONCE_INIT;
static mono_mutex_t thread_hash_mutex = MONO_MUTEX_INITIALIZER;
static GHashTable *thread_hash=NULL;

#if HAVE_BOEHM_GC
static MonoGHashTable *tls_gc_hash = NULL;
#endif

static void thread_close_private (gpointer handle);
static void thread_own (gpointer handle);

struct _WapiHandleOps _wapi_thread_ops = {
	NULL,				/* close_shared */
	thread_close_private,		/* close_private */
	NULL,				/* signal */
	thread_own,			/* own */
	NULL,				/* is_owned */
};

static mono_once_t thread_ops_once=MONO_ONCE_INIT;

static void thread_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_THREAD,
					    WAPI_HANDLE_CAP_WAIT);
}

static void thread_close_private (gpointer handle)
{
	struct _WapiHandlePrivate_thread *thread_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD, NULL,
				(gpointer *)&thread_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": closing thread handle %p with thread %p id %ld",
		  handle, thread_handle->thread,
		  thread_handle->thread->id);
#endif

	if(thread_handle->thread!=NULL) {
		_wapi_timed_thread_destroy (thread_handle->thread);
	}
}

static void thread_own (gpointer handle)
{
	struct _WapiHandle_thread *thread_handle;
	struct _WapiHandlePrivate_thread *thread_private_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle,
				(gpointer *)&thread_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		return;
	}

	if(thread_private_handle->joined==FALSE) {
		_wapi_timed_thread_join (thread_private_handle->thread, NULL,
					 NULL);
		thread_private_handle->joined=TRUE;
	}
}

static void thread_exit(guint32 exitstatus, gpointer handle)
{
	struct _WapiHandle_thread *thread_handle;
	struct _WapiHandlePrivate_thread *thread_private_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle,
				(gpointer *)&thread_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		return;
	}

	_wapi_handle_lock_handle (handle);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": Recording thread handle %p exit status", handle);
#endif
	
	thread_handle->exitstatus=exitstatus;
	thread_handle->state=THREAD_STATE_EXITED;
	_wapi_handle_set_signal_state (handle, TRUE, TRUE);

	_wapi_handle_unlock_handle (handle);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Recording thread handle %p id %ld status as %d",
		  handle, thread_private_handle->thread->id, exitstatus);
#endif

	/* Remove this thread from the hash */
	mono_mutex_lock(&thread_hash_mutex);
	g_hash_table_remove(thread_hash, &thread_private_handle->thread->id);
	mono_mutex_unlock(&thread_hash_mutex);

	/* The thread is no longer active, so unref it */
	_wapi_handle_unref (handle);
}

static void thread_hash_init(void)
{
	thread_hash=g_hash_table_new(g_int_hash, g_int_equal);
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
 * @tid: If non-NULL, the ID of the new thread is stored here.
 *
 * Creates a new threading handle.
 *
 * Return value: a new handle, or NULL
 */
gpointer CreateThread(WapiSecurityAttributes *security G_GNUC_UNUSED, guint32 stacksize G_GNUC_UNUSED,
		      WapiThreadStart start, gpointer param, guint32 create,
		      guint32 *tid) 
{
	struct _WapiHandle_thread *thread_handle;
	struct _WapiHandlePrivate_thread *thread_private_handle;
	gpointer handle;
	gboolean ok;
	int ret;
	
	mono_once(&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
	if(start==NULL) {
		return(NULL);
	}
	
	handle=_wapi_handle_new (WAPI_HANDLE_THREAD);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating thread handle");
		return(NULL);
	}

	_wapi_handle_lock_handle (handle);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle,
				(gpointer *)&thread_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		_wapi_handle_unlock_handle (handle);
		return(NULL);
	}

	/* Hold a reference while the thread is active, because we use
	 * the handle to store thread exit information
	 */
	_wapi_handle_ref (handle);

	thread_handle->state=THREAD_STATE_START;
	
	/* Lock around the thread create, so that the new thread cant
	 * race us to look up the thread handle in GetCurrentThread()
	 */
	mono_mutex_lock(&thread_hash_mutex);
	
	ret=_wapi_timed_thread_create(&thread_private_handle->thread, NULL,
				      create, start, thread_exit, param,
				      handle);
	if(ret!=0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Thread create error: %s",
			  strerror(ret));
#endif
		mono_mutex_unlock(&thread_hash_mutex);
		_wapi_handle_unlock_handle (handle);
		_wapi_handle_unref (handle);
		
		/* And again, because of the reference we took above */
		_wapi_handle_unref (handle);
		return(NULL);
	}

	g_hash_table_insert(thread_hash, &thread_private_handle->thread->id,
			    handle);
	mono_mutex_unlock(&thread_hash_mutex);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Started thread handle %p thread %p ID %ld", handle,
		  thread_private_handle->thread,
		  thread_private_handle->thread->id);
#endif
	
	if(tid!=NULL) {
#ifdef PTHREAD_POINTER_ID
		*tid=GPOINTER_TO_UINT(thread_private_handle->thread->id);
#else
		*tid=thread_private_handle->thread->id;
#endif
	}

	_wapi_handle_unlock_handle (handle);
	
	return(handle);
}

gpointer OpenThread (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, guint32 tid)
{
	gpointer ret=NULL;
	
	mono_once(&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": looking up thread %d", tid);
#endif

	mono_mutex_lock(&thread_hash_mutex);
	
	ret=g_hash_table_lookup(thread_hash, &tid);
	mono_mutex_unlock(&thread_hash_mutex);
	
	if(ret!=NULL) {
		_wapi_handle_ref (ret);
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning thread handle %p", ret);
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
	struct _WapiHandlePrivate_thread *thread_private_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle,
				(gpointer *)&thread_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		return(FALSE);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Finding exit status for thread handle %p id %ld", handle,
		  thread_private_handle->thread->id);
#endif

	if(exitcode==NULL) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Nowhere to store exit code");
#endif
		return(FALSE);
	}
	
	if(thread_handle->state!=THREAD_STATE_EXITED) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Thread still active (state %d, exited is %d)",
			  thread_handle->state, THREAD_STATE_EXITED);
#endif
		*exitcode=STILL_ACTIVE;
		return(TRUE);
	}
	
	*exitcode=thread_handle->exitstatus;
	
	return(TRUE);
}

/**
 * GetCurrentThreadId:
 *
 * Looks up the thread ID of the current thread.  This ID can be
 * passed to OpenThread() to create a new handle on this thread.
 *
 * Return value: the thread ID.
 */
guint32 GetCurrentThreadId(void)
{
	pthread_t tid=pthread_self();
	
#ifdef PTHREAD_POINTER_ID
	return(GPOINTER_TO_UINT(tid));
#else
	return(tid);
#endif
}

static gpointer thread_attach(guint32 *tid)
{
	struct _WapiHandle_thread *thread_handle;
	struct _WapiHandlePrivate_thread *thread_private_handle;
	gpointer handle;
	gboolean ok;
	int ret;

	mono_once(&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);

	handle=_wapi_handle_new (WAPI_HANDLE_THREAD);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating thread handle");
		return(NULL);
	}

	_wapi_handle_lock_handle (handle);

	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle,
				(gpointer *)&thread_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		_wapi_handle_unlock_handle (handle);
		return(NULL);
	}

	/* Hold a reference while the thread is active, because we use
	 * the handle to store thread exit information
	 */
	_wapi_handle_ref (handle);

	thread_handle->state=THREAD_STATE_START;

	/* Lock around the thread create, so that the new thread cant
	 * race us to look up the thread handle in GetCurrentThread()
	 */
	mono_mutex_lock(&thread_hash_mutex);

	ret=_wapi_timed_thread_attach(&thread_private_handle->thread,
				      thread_exit, handle);
	if(ret!=0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Thread attach error: %s",
			  strerror(ret));
#endif
		mono_mutex_unlock(&thread_hash_mutex);
		_wapi_handle_unlock_handle (handle);
		_wapi_handle_unref (handle);

		/* And again, because of the reference we took above */
		_wapi_handle_unref (handle);
		return(NULL);
	}

	g_hash_table_insert(thread_hash, &thread_private_handle->thread->id,
			    handle);
	mono_mutex_unlock(&thread_hash_mutex);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Attached thread handle %p thread %p ID %ld", handle,
		  thread_private_handle->thread,
		  thread_private_handle->thread->id);
#endif

	if(tid!=NULL) {
#ifdef PTHREAD_POINTER_ID
		*tid=GPOINTER_TO_UINT(thread_private_handle->thread->id);
#else
		*tid=thread_private_handle->thread->id;
#endif
	}

	_wapi_handle_unlock_handle (handle);

	return(handle);
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
	guint32 tid;
	
	mono_once(&thread_hash_once, thread_hash_init);
	mono_once (&thread_ops_once, thread_ops_init);
	
	tid=GetCurrentThreadId();
	
	mono_mutex_lock(&thread_hash_mutex);

	ret=g_hash_table_lookup(thread_hash, &tid);
	mono_mutex_unlock(&thread_hash_mutex);
	
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
	struct _WapiHandlePrivate_thread *thread_private_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_THREAD,
				(gpointer *)&thread_handle,
				(gpointer *)&thread_private_handle);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up thread handle %p", handle);
		return(0xFFFFFFFF);
	}

	/* This is still a kludge that only copes with starting a
	 * thread that was suspended on create, so don't bother with
	 * the suspend count crap yet
	 */
	_wapi_timed_thread_resume (thread_private_handle->thread);
	
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
guint32 SuspendThread(gpointer handle G_GNUC_UNUSED)
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
#ifdef TLS_PTHREAD_MUTEX
static mono_mutex_t TLS_mutex=MONO_MUTEX_INITIALIZER;
#else
static guint32 TLS_spinlock=0;
#endif

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
	
#ifdef TLS_PTHREAD_MUTEX
	mono_mutex_lock(&TLS_mutex);
#else
	MONO_SPIN_LOCK (TLS_spinlock);
#endif
	
	for(i=0; i<TLS_MINIMUM_AVAILABLE; i++) {
		if(TLS_used[i]==FALSE) {
			TLS_used[i]=TRUE;
			pthread_key_create(&TLS_keys[i], NULL);

#ifdef TLS_PTHREAD_MUTEX
			mono_mutex_unlock(&TLS_mutex);
#else
			MONO_SPIN_UNLOCK (TLS_spinlock);
#endif
	
#ifdef TLS_DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION ": returning key %d",
				   i);
#endif
			
			return(i);
		}
	}

#ifdef TLS_PTHREAD_MUTEX
	mono_mutex_unlock(&TLS_mutex);
#else
	MONO_SPIN_UNLOCK (TLS_spinlock);
#endif
	
#ifdef TLS_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": out of indices");
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
#ifdef TLS_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": freeing key %d", idx);
#endif

#ifdef TLS_PTHREAD_MUTEX
	mono_mutex_lock(&TLS_mutex);
#else
	MONO_SPIN_LOCK (TLS_spinlock);
#endif
	
	if(TLS_used[idx]==FALSE) {
#ifdef TLS_PTHREAD_MUTEX
		mono_mutex_unlock(&TLS_mutex);
#else
		MONO_SPIN_UNLOCK (TLS_spinlock);
#endif
		return(FALSE);
	}
	
	TLS_used[idx]=FALSE;
	pthread_key_delete(TLS_keys[idx]);
	
#if HAVE_BOEHM_GC
	mono_g_hash_table_remove (tls_gc_hash, MAKE_GC_ID (idx));
#endif

#ifdef TLS_PTHREAD_MUTEX
	mono_mutex_unlock(&TLS_mutex);
#else
	MONO_SPIN_UNLOCK (TLS_spinlock);
#endif
	
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
	g_message (G_GNUC_PRETTY_FUNCTION ": looking up key %d", idx);
#endif
	
	ret=pthread_getspecific(TLS_keys[idx]);

#ifdef TLS_DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning %p", ret);
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
	g_message (G_GNUC_PRETTY_FUNCTION ": setting key %d to %p", idx,
		   value);
#endif
	
#ifdef TLS_PTHREAD_MUTEX
	mono_mutex_lock(&TLS_mutex);
#else
	MONO_SPIN_LOCK (TLS_spinlock);
#endif
	
	if(TLS_used[idx]==FALSE) {
#ifdef TLS_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": key %d unused", idx);
#endif

#ifdef TLS_PTHREAD_MUTEX
		mono_mutex_unlock(&TLS_mutex);
#else
		MONO_SPIN_UNLOCK (TLS_spinlock);
#endif
		return(FALSE);
	}
	
	ret=pthread_setspecific(TLS_keys[idx], value);
	if(ret!=0) {
#ifdef TLS_DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": pthread_setspecific error: %s", strerror (ret));
#endif

#ifdef TLS_PTHREAD_MUTEX
		mono_mutex_unlock(&TLS_mutex);
#else
		MONO_SPIN_UNLOCK (TLS_spinlock);
#endif
		return(FALSE);
	}
	
#if HAVE_BOEHM_GC
	if (!tls_gc_hash)
		tls_gc_hash = mono_g_hash_table_new(g_direct_hash, g_direct_equal);
	mono_g_hash_table_insert (tls_gc_hash, MAKE_GC_ID (idx), value);
#endif

#ifdef TLS_PTHREAD_MUTEX
	mono_mutex_unlock(&TLS_mutex);
#else
	MONO_SPIN_UNLOCK (TLS_spinlock);
#endif
	
	return(TRUE);
}

/**
 * Sleep:
 * @ms: The time in milliseconds to suspend for
 *
 * Suspends execution of the current thread for @ms milliseconds.  A
 * value of zero causes the thread to relinquish its time slice.  A
 * value of %INFINITE causes an infinite delay.
 */
void Sleep(guint32 ms)
{
	struct timespec req, rem;
	div_t divvy;
	int ret;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Sleeping for %d ms", ms);
#endif

	if(ms==0) {
		sched_yield();
		return;
	}
	
	/* FIXME: check for INFINITE and sleep forever */
	divvy=div((int)ms, 1000);
	
	req.tv_sec=divvy.quot;
	req.tv_nsec=divvy.rem*1000000;
	
again:
	ret=nanosleep(&req, &rem);
	if(ret==-1) {
		/* Sleep interrupted with rem time remaining */
#ifdef DEBUG
		guint32 rems=rem.tv_sec*1000 + rem.tv_nsec/1000000;
		
		g_message(G_GNUC_PRETTY_FUNCTION ": Still got %d ms to go",
			  rems);
#endif
		req=rem;
		goto again;
	}
}

/* FIXME: implement alertable */
void SleepEx(guint32 ms, gboolean alertable)
{
	if(alertable==TRUE) {
		g_warning(G_GNUC_PRETTY_FUNCTION ": alertable not implemented");
	}
	
	Sleep(ms);
}
