#include <config.h>
#if HAVE_BOEHM_GC
#include <gc/gc.h>
#include "mono/utils/mono-hash.h"
#endif
#include <glib.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>

#include "mono/io-layer/wapi.h"
#include "wapi-private.h"
#include "timed-thread.h"
#include "wait-private.h"
#include "handles-private.h"
#include "misc-private.h"

#include "mono-mutex.h"

#undef DEBUG

typedef enum {
	THREAD_STATE_START,
	THREAD_STATE_EXITED,
} WapiThreadState;

struct _WapiHandle_thread
{
	WapiHandle handle;
	WapiThreadState state;
	TimedThread *thread;
	guint32 exitstatus;
};

static mono_mutex_t thread_signal_mutex = MONO_MUTEX_INITIALIZER;
static pthread_cond_t thread_signal_cond = PTHREAD_COND_INITIALIZER;

/* Hash threads with tids. I thought of using TLS for this, but that
 * would have to set the data in the new thread, which is more hassle
 */
static pthread_once_t thread_hash_once = PTHREAD_ONCE_INIT;
static mono_mutex_t thread_hash_mutex = MONO_MUTEX_INITIALIZER;
static GHashTable *thread_hash=NULL;

#if HAVE_BOEHM_GC
static MonoGHashTable *tls_gc_hash = NULL;
#endif

static void thread_close(WapiHandle *handle);
static gboolean thread_wait(WapiHandle *handle, WapiHandle *signal,
			    guint32 ms);
static guint32 thread_wait_multiple(gpointer data);

static struct _WapiHandleOps thread_ops = {
	thread_close,			/* close */
	NULL,				/* getfiletype */
	NULL,				/* readfile */
	NULL,				/* writefile */
	NULL,				/* flushfile */
	NULL,				/* seek */
	NULL,				/* setendoffile */
	NULL,				/* getfilesize */
	NULL,				/* getfiletime */
	NULL,				/* setfiletime */
	thread_wait,			/* wait */
	thread_wait_multiple,		/* wait_multiple */
	NULL,				/* signal */
};

static void thread_close(WapiHandle *handle)
{
	struct _WapiHandle_thread *thread_handle=(struct _WapiHandle_thread *)handle;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": closing thread handle %p with thread %p id %ld",
		  thread_handle, thread_handle->thread,
		  thread_handle->thread->id);
#endif

	mono_mutex_destroy (&thread_handle->thread->join_mutex);
	g_free(thread_handle->thread);
}

static gboolean thread_wait(WapiHandle *handle, WapiHandle *signal, guint32 ms)
{
	struct _WapiHandle_thread *thread_handle=(struct _WapiHandle_thread *)handle;
	int ret;
	
	/* A thread can never become unsignalled after it was
	 * signalled, so we can signal this handle now without
	 * worrying about lost wakeups
	 */
	if(signal!=NULL) {
		signal->ops->signal(signal);
	}
	
	if(handle->signalled==TRUE) {
		/* Already signalled, so return straight away */
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": thread handle %p already signalled, returning now", handle);
#endif

		return(TRUE);
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": waiting for %d ms for thread handle %p with id %ld", ms,
		  thread_handle, thread_handle->thread->id);
#endif

	if(ms==INFINITE) {
		ret=_wapi_timed_thread_join(thread_handle->thread, NULL, NULL);
	} else {
		struct timespec timeout;

		_wapi_calc_timeout(&timeout, ms);
	
		ret=_wapi_timed_thread_join(thread_handle->thread, &timeout,
					    NULL);
	}
	
	if(ret==0) {
		/* Thread joined */
		return(TRUE);
	} else {
		/* ret might be ETIMEDOUT for timeout, or other for error */
		return(FALSE);
	}
}

static guint32 thread_wait_multiple(gpointer data)
{
	WaitQueueItem *item=(WaitQueueItem *)data;
	int ret;
	guint32 numhandles, count;
	struct timespec timeout;
	
	numhandles=item->handles[WAPI_HANDLE_THREAD]->len;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": waiting on %d thread handles for %d ms", numhandles,
		  item->timeout);
#endif

	/* First, check if any of the handles are already
	 * signalled. If waitall is specified we only return if all
	 * handles have been signalled.
	 */
	count=_wapi_handle_count_signalled(item, WAPI_HANDLE_THREAD);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Preliminary check found %d handles signalled", count);
#endif

	if((item->waitall==TRUE && count==numhandles) || 
	   (item->waitall==FALSE && count>0)) {
		goto success;
	}
	
	/* OK, we need to wait for some */
	if(item->timeout!=INFINITE) {
		_wapi_calc_timeout(&timeout, item->timeout);
	}
	
	/* We can restart from here without resetting the timeout,
	 * because it is calculated from absolute time, not an offset
	 */
again:
	mono_mutex_lock(&thread_signal_mutex);
	if(item->timeout==INFINITE) {
		ret=mono_cond_wait(&thread_signal_cond,
				      &thread_signal_mutex);
	} else {
		ret=mono_cond_timedwait(&thread_signal_cond,
					   &thread_signal_mutex,
					   &timeout);
	}
	mono_mutex_unlock(&thread_signal_mutex);

	if(ret==ETIMEDOUT) {
		/* Check signalled state here, just in case a thread
		 * exited between the first check and the cond wait.
		 * We return the number of signalled handles, which
		 * may be fewer than the total.
		 */
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Wait timed out");
#endif

		count=_wapi_handle_count_signalled(item, WAPI_HANDLE_THREAD);
		goto success;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Thread exited, checking status");
#endif

	/* Another thread exited, so see if it was one we are
	 * interested in
	 */
	count=_wapi_handle_count_signalled(item, WAPI_HANDLE_THREAD);

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Check after thread exit found %d handles signalled",
		  count);
#endif

	if((item->waitall==TRUE && count==numhandles) ||
	   (item->waitall==FALSE && count>0)) {
		goto success;
	}

	/* Either we have waitall set with more handles to wait for,
	 * or the thread that exited wasn't interesting to us
	 */
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Waiting a bit longer");
#endif

	goto again;

success:
	item->waited[WAPI_HANDLE_THREAD]=TRUE;
	item->waitcount[WAPI_HANDLE_THREAD]=count;
	
	return(count);
}

static void thread_exit(guint32 exitstatus, gpointer userdata)
{
	struct _WapiHandle_thread *thread_handle=(struct _WapiHandle_thread *)userdata;

	thread_handle->exitstatus=exitstatus;
	thread_handle->state=THREAD_STATE_EXITED;
	thread_handle->handle.signalled=TRUE;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Recording thread handle %p id %ld status as %d",
		  thread_handle, thread_handle->thread->id, exitstatus);
#endif

	/* Remove this thread from the hash */
	mono_mutex_lock(&thread_hash_mutex);
	g_hash_table_remove(thread_hash, &thread_handle->thread->id);
	mono_mutex_unlock(&thread_hash_mutex);
	
	/* Signal any thread waiting on thread exit */
	mono_mutex_lock(&thread_signal_mutex);
	pthread_cond_broadcast(&thread_signal_cond);
	mono_mutex_unlock(&thread_signal_mutex);
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
WapiHandle *CreateThread(WapiSecurityAttributes *security G_GNUC_UNUSED, guint32 stacksize G_GNUC_UNUSED,
			 WapiThreadStart start, gpointer param, guint32 create G_GNUC_UNUSED,
			 guint32 *tid) 
{
	struct _WapiHandle_thread *thread_handle;
	WapiHandle *handle;
	int ret;
	
	pthread_once(&thread_hash_once, thread_hash_init);
	
	if(start==NULL) {
		return(NULL);
	}
	
	thread_handle=(struct _WapiHandle_thread *)g_new0(struct _WapiHandle_thread, 1);

	handle=(WapiHandle *)thread_handle;
	_WAPI_HANDLE_INIT(handle, WAPI_HANDLE_THREAD, thread_ops);

	thread_handle->state=THREAD_STATE_START;
	
	/* Lock around the thread create, so that the new thread cant
	 * race us to look up the thread handle in GetCurrentThread()
	 */
	mono_mutex_lock(&thread_hash_mutex);
	
	ret=_wapi_timed_thread_create(&thread_handle->thread, NULL, start,
				      thread_exit, param, thread_handle);
	if(ret!=0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Thread create error: %s",
			  strerror(ret));
#endif
		mono_mutex_unlock(&thread_hash_mutex);
		g_free(thread_handle);
		return(NULL);
	}

	g_hash_table_insert(thread_hash, &thread_handle->thread->id,
			    thread_handle);
	mono_mutex_unlock(&thread_hash_mutex);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Started thread handle %p thread %p ID %ld", thread_handle,
		  thread_handle->thread, thread_handle->thread->id);
#endif
	
	if(tid!=NULL) {
		*tid=thread_handle->thread->id;
	}
	
	return(handle);
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
gboolean GetExitCodeThread(WapiHandle *handle, guint32 *exitcode)
{
	struct _WapiHandle_thread *thread_handle=(struct _WapiHandle_thread *)handle;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": Finding exit status for thread handle %p id %ld", handle,
		  thread_handle->thread->id);
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
	
	return(tid);
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
WapiHandle *GetCurrentThread(void)
{
	WapiHandle *ret=NULL;
	guint32 tid;
	
	tid=GetCurrentThreadId();
	
	mono_mutex_lock(&thread_hash_mutex);

	ret=g_hash_table_lookup(thread_hash, &tid);
	
	mono_mutex_unlock(&thread_hash_mutex);
	
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
guint32 ResumeThread(WapiHandle *handle G_GNUC_UNUSED)
{
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
guint32 SuspendThread(WapiHandle *handle G_GNUC_UNUSED)
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
static mono_mutex_t TLS_mutex=MONO_MUTEX_INITIALIZER;

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
	
	mono_mutex_lock(&TLS_mutex);
	
	for(i=0; i<TLS_MINIMUM_AVAILABLE; i++) {
		if(TLS_used[i]==FALSE) {
			TLS_used[i]=TRUE;
			pthread_key_create(&TLS_keys[i], NULL);

			mono_mutex_unlock(&TLS_mutex);
			
			return(i);
		}
	}

	mono_mutex_unlock(&TLS_mutex);
	
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
	mono_mutex_lock(&TLS_mutex);
	
	if(TLS_used[idx]==FALSE) {
		mono_mutex_unlock(&TLS_mutex);
		return(FALSE);
	}
	
	TLS_used[idx]=FALSE;
	pthread_key_delete(TLS_keys[idx]);
	
#if HAVE_BOEHM_GC
	mono_g_hash_table_remove (tls_gc_hash, MAKE_GC_ID (idx));
#endif
	mono_mutex_unlock(&TLS_mutex);
	
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
	
	mono_mutex_lock(&TLS_mutex);
	
	if(TLS_used[idx]==FALSE) {
		mono_mutex_unlock(&TLS_mutex);
		return(NULL);
	}
	
	ret=pthread_getspecific(TLS_keys[idx]);
	
	mono_mutex_unlock(&TLS_mutex);
	
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
	
	mono_mutex_lock(&TLS_mutex);
	
	if(TLS_used[idx]==FALSE) {
		mono_mutex_unlock(&TLS_mutex);
		return(FALSE);
	}
	
	ret=pthread_setspecific(TLS_keys[idx], value);
	if(ret!=0) {
		mono_mutex_unlock(&TLS_mutex);
		return(FALSE);
	}
	
#if HAVE_BOEHM_GC
	if (!tls_gc_hash)
		tls_gc_hash = mono_g_hash_table_new(g_direct_hash, g_direct_equal);
	mono_g_hash_table_insert (tls_gc_hash, MAKE_GC_ID (idx), value);
#endif
	mono_mutex_unlock(&TLS_mutex);
	
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
