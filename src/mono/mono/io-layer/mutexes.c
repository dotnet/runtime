/*
 * mutexes.c:  Mutex handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <string.h>
#include <unistd.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/handles-private.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/io-layer/mutex-private.h>

#undef DEBUG

/* This is used to serialise mutex creation when names are given
 */
static mono_mutex_t named_mutex_mutex;

static void mutex_close_shared (gpointer handle);
static void mutex_signal(gpointer handle);
static void mutex_own (gpointer handle);
static gboolean mutex_is_owned (gpointer handle);

struct _WapiHandleOps _wapi_mutex_ops = {
	mutex_close_shared,	/* close_shared */
	NULL,			/* close_private */
	mutex_signal,		/* signal */
	mutex_own,		/* own */
	mutex_is_owned,		/* is_owned */
};

static mono_once_t mutex_ops_once=MONO_ONCE_INIT;

static void mutex_ops_init (void)
{
	int thr_ret;
#if defined(_POSIX_THREAD_PROCESS_SHARED) && _POSIX_THREAD_PROCESS_SHARED != -1
	pthread_mutexattr_t mutex_shared_attr;

	thr_ret = mono_mutexattr_init (&mutex_shared_attr);
	g_assert (thr_ret == 0);

	thr_ret = mono_mutexattr_setpshared (&mutex_shared_attr,
					     PTHREAD_PROCESS_SHARED);
	g_assert (thr_ret == 0);

	thr_ret = mono_mutex_init (&named_mutex_mutex, &mutex_shared_attr);
	g_assert (thr_ret == 0);
#else
	thr_ret = mono_mutex_init (&named_mutex_mutex, NULL);
#endif

	_wapi_handle_register_capabilities (WAPI_HANDLE_MUTEX,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL |
					    WAPI_HANDLE_CAP_OWN);
}

static void mutex_close_shared (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": closing mutex handle %p", handle);
#endif

	if(mutex_handle->sharedns.name!=0) {
		_wapi_handle_scratch_delete (mutex_handle->sharedns.name);
		mutex_handle->sharedns.name=0;
	}
}

static void mutex_signal(gpointer handle)
{
	ReleaseMutex(handle);
}

static void mutex_own (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return;
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": owning mutex handle %p", handle);
#endif

	_wapi_handle_set_signal_state (handle, FALSE, FALSE);
	
	mutex_handle->pid=getpid ();
	mutex_handle->tid=pthread_self ();
	mutex_handle->recursion++;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION
		   ": mutex handle %p locked %d times by %d:%ld", handle,
		   mutex_handle->recursion, mutex_handle->pid,
		   mutex_handle->tid);
#endif
}

static gboolean mutex_is_owned (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return(FALSE);
	}
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION
		  ": testing ownership mutex handle %p", handle);
#endif

	if(mutex_handle->recursion>0 &&
	   mutex_handle->pid==getpid () &&
	   mutex_handle->tid==pthread_self ()) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": mutex handle %p owned by %d:%ld", handle,
			   getpid (), pthread_self ());
#endif

		return(TRUE);
	} else {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": mutex handle %p not owned by %d:%ld, but locked %d times by %d:%ld", handle, getpid (), pthread_self (), mutex_handle->recursion, mutex_handle->pid, mutex_handle->tid);
#endif

		return(FALSE);
	}
}

struct mutex_check_data
{
	pid_t pid;
	pthread_t tid;
};

static gboolean mutex_check (gpointer handle, gpointer user_data)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	struct mutex_check_data *data = (struct mutex_check_data *)user_data;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				  (gpointer *)&mutex_handle, NULL);
	if (ok == FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return(FALSE);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	if (mutex_handle->pid == data->pid &&
	    mutex_handle->tid == data->tid) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Mutex handle %p abandoned!", handle);
#endif

		mutex_handle->recursion = 0;
		mutex_handle->pid = 0;
		mutex_handle->tid = 0;
		
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	/* Return false to keep searching */
	return(FALSE);
}

/* When a thread exits, any mutexes it still holds need to be signalled */
void _wapi_mutex_check_abandoned (pid_t pid, pthread_t tid)
{
	struct mutex_check_data data;

	data.pid = pid;
	data.tid = tid;
	
	_wapi_search_handle (WAPI_HANDLE_MUTEX, mutex_check, &data, NULL,
			     NULL);
}

/**
 * CreateMutex:
 * @security: Ignored for now.
 * @owned: If %TRUE, the mutex is created with the calling thread
 * already owning the mutex.
 * @name:Pointer to a string specifying the name of this mutex, or
 * %NULL.
 *
 * Creates a new mutex handle.  A mutex is signalled when no thread
 * owns it.  A thread acquires ownership of the mutex by waiting for
 * it with WaitForSingleObject() or WaitForMultipleObjects().  A
 * thread relinquishes ownership with ReleaseMutex().
 *
 * A thread that owns a mutex can specify the same mutex in repeated
 * wait function calls without blocking.  The thread must call
 * ReleaseMutex() an equal number of times to release the mutex.
 *
 * Return value: A new handle, or %NULL on error.
 */
gpointer CreateMutex(WapiSecurityAttributes *security G_GNUC_UNUSED, gboolean owned,
			const gunichar2 *name)
{
	struct _WapiHandle_mutex *mutex_handle;
	gpointer handle;
	gboolean ok;
	gchar *utf8_name;
	int thr_ret;
	gpointer ret = NULL;
	
	mono_once (&mutex_ops_once, mutex_ops_init);

	/* w32 seems to guarantee that opening named mutexes can't
	 * race each other
	 */
	pthread_cleanup_push ((void(*)(void *))mono_mutex_unlock_in_cleanup,
			      (void *)&named_mutex_mutex);
	thr_ret = mono_mutex_lock (&named_mutex_mutex);
	g_assert (thr_ret == 0);

	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a mutex
	 * was freshly created
	 */
	SetLastError (ERROR_SUCCESS);
	
	if(name!=NULL) {
		utf8_name=g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	} else {
		utf8_name=NULL;
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Creating mutex (name [%s])",
		   utf8_name==NULL?"<unnamed>":utf8_name);
#endif
	
	if(name!=NULL) {
		handle=_wapi_search_handle_namespace (
			WAPI_HANDLE_MUTEX, utf8_name,
			(gpointer *)&mutex_handle, NULL);
		if(handle==_WAPI_HANDLE_INVALID) {
			/* The name has already been used for a different
			 * object.
			 */
			g_free (utf8_name);
			SetLastError (ERROR_INVALID_HANDLE);
			goto cleanup;
		} else if (handle!=NULL) {
			g_free (utf8_name);
			_wapi_handle_ref (handle);
			ret = handle;

			/* Not an error, but this is how the caller is
			 * informed that the mutex wasn't freshly
			 * created
			 */
			SetLastError (ERROR_ALREADY_EXISTS);
			goto cleanup;
		}
		/* Otherwise fall through to create the mutex. */
	}
	
	handle=_wapi_handle_new (WAPI_HANDLE_MUTEX);
	if(handle==_WAPI_HANDLE_INVALID) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error creating mutex handle");
		if(utf8_name!=NULL) {
			g_free (utf8_name);
		}
		goto cleanup;
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		if(utf8_name!=NULL) {
			g_free (utf8_name);
		}
		
		goto handle_cleanup;
	}
	ret = handle;
	
	if(utf8_name!=NULL) {
		mutex_handle->sharedns.name=_wapi_handle_scratch_store (
			utf8_name, strlen (utf8_name));
	}
	
	if(owned==TRUE) {
		mutex_own (handle);
	} else {
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning mutex handle %p",
		   handle);
#endif

	if(utf8_name!=NULL) {
		g_free (utf8_name);
	}

handle_cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
cleanup:
	thr_ret = mono_mutex_unlock (&named_mutex_mutex);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(ret);
}

/**
 * ReleaseMutex:
 * @handle: The mutex handle.
 *
 * Releases ownership if the mutex handle @handle.
 *
 * Return value: %TRUE on success, %FALSE otherwise.  This function
 * fails if the calling thread does not own the mutex @handle.
 */
gboolean ReleaseMutex(gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	pthread_t tid=pthread_self();
	pid_t pid=getpid ();
	int thr_ret;
	gboolean ret = FALSE;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle, NULL);
	if(ok==FALSE) {
		g_warning (G_GNUC_PRETTY_FUNCTION
			   ": error looking up mutex handle %p", handle);
		return(FALSE);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Releasing mutex handle %p",
		  handle);
#endif

	if(mutex_handle->tid!=tid || mutex_handle->pid!=pid) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": We don't own mutex handle %p (owned by %d:%ld, me %d:%ld)", handle, mutex_handle->pid, mutex_handle->tid, pid, tid);
#endif

		goto cleanup;
	}
	ret = TRUE;
	
	/* OK, we own this mutex */
	mutex_handle->recursion--;
	
	if(mutex_handle->recursion==0) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Unlocking mutex handle %p",
			  handle);
#endif

		mutex_handle->pid=0;
		mutex_handle->tid=0;
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

cleanup:
	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(ret);
}
