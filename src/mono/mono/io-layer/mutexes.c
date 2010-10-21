/*
 * mutexes.c:  Mutex handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Ximian, Inc.
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

static void mutex_signal(gpointer handle);
static gboolean mutex_own (gpointer handle);
static gboolean mutex_is_owned (gpointer handle);

static void namedmutex_signal (gpointer handle);
static gboolean namedmutex_own (gpointer handle);
static gboolean namedmutex_is_owned (gpointer handle);
static void namedmutex_prewait (gpointer handle);

struct _WapiHandleOps _wapi_mutex_ops = {
	NULL,			/* close */
	mutex_signal,		/* signal */
	mutex_own,		/* own */
	mutex_is_owned,		/* is_owned */
	NULL,			/* special_wait */
	NULL			/* prewait */
};

void _wapi_mutex_details (gpointer handle_info)
{
	struct _WapiHandle_mutex *mut = (struct _WapiHandle_mutex *)handle_info;
	
#ifdef PTHREAD_POINTER_ID
	g_print ("own: %5d:%5p, count: %5u", mut->pid, mut->tid,
		 mut->recursion);
#else
	g_print ("own: %5d:%5ld, count: %5u", mut->pid, mut->tid,
		 mut->recursion);
#endif
}

struct _WapiHandleOps _wapi_namedmutex_ops = {
	NULL,			/* close */
	namedmutex_signal,	/* signal */
	namedmutex_own,		/* own */
	namedmutex_is_owned,	/* is_owned */
	NULL,			/* special_wait */
	namedmutex_prewait	/* prewait */
};

static gboolean mutex_release (gpointer handle);
static gboolean namedmutex_release (gpointer handle);

static struct 
{
	gboolean (*release)(gpointer handle);
} mutex_ops[WAPI_HANDLE_COUNT] = {
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{mutex_release},
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{NULL},
	{namedmutex_release},
};

static mono_once_t mutex_ops_once=MONO_ONCE_INIT;

static void mutex_ops_init (void)
{
	_wapi_handle_register_capabilities (WAPI_HANDLE_MUTEX,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL |
					    WAPI_HANDLE_CAP_OWN);
	_wapi_handle_register_capabilities (WAPI_HANDLE_NAMEDMUTEX,
					    WAPI_HANDLE_CAP_WAIT |
					    WAPI_HANDLE_CAP_SIGNAL |
					    WAPI_HANDLE_CAP_OWN);
}

static void mutex_signal(gpointer handle)
{
	ReleaseMutex(handle);
}

static gboolean mutex_own (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				  (gpointer *)&mutex_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up mutex handle %p", __func__,
			   handle);
		return(FALSE);
	}

	_wapi_thread_own_mutex (handle);
	
#ifdef DEBUG
	g_message("%s: owning mutex handle %p", __func__, handle);
#endif

	_wapi_handle_set_signal_state (handle, FALSE, FALSE);
	
	mutex_handle->pid = _wapi_getpid ();
	mutex_handle->tid = pthread_self ();
	mutex_handle->recursion++;

#ifdef DEBUG
	g_message ("%s: mutex handle %p locked %d times by %d:%ld", __func__,
		   handle, mutex_handle->recursion, mutex_handle->pid,
		   mutex_handle->tid);
#endif

	return(TRUE);
}

static gboolean mutex_is_owned (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				(gpointer *)&mutex_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up mutex handle %p", __func__,
			   handle);
		return(FALSE);
	}
	
#ifdef DEBUG
	g_message("%s: testing ownership mutex handle %p", __func__, handle);
#endif

	if (mutex_handle->recursion > 0 &&
	    mutex_handle->pid == _wapi_getpid () &&
	    pthread_equal (mutex_handle->tid, pthread_self ())) {
#ifdef DEBUG
		g_message ("%s: mutex handle %p owned by %d:%ld", __func__,
			   handle, _wapi_getpid (), pthread_self ());
#endif

		return(TRUE);
	} else {
#ifdef DEBUG
		g_message ("%s: mutex handle %p not owned by %d:%ld, but locked %d times by %d:%ld", __func__, handle, _wapi_getpid (), pthread_self (), mutex_handle->recursion, mutex_handle->pid, mutex_handle->tid);
#endif

		return(FALSE);
	}
}

static void namedmutex_signal (gpointer handle)
{
	ReleaseMutex(handle);
}

/* NB, always called with the shared handle lock held */
static gboolean namedmutex_own (gpointer handle)
{
	struct _WapiHandle_namedmutex *namedmutex_handle;
	gboolean ok;
	
#ifdef DEBUG
	g_message ("%s: owning named mutex handle %p", __func__, handle);
#endif
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDMUTEX,
				  (gpointer *)&namedmutex_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named mutex handle %p",
			   __func__, handle);
		return(FALSE);
	}

	_wapi_thread_own_mutex (handle);

	namedmutex_handle->pid = _wapi_getpid ();
	namedmutex_handle->tid = pthread_self ();
	namedmutex_handle->recursion++;

	_wapi_shared_handle_set_signal_state (handle, FALSE);

#ifdef DEBUG
	g_message ("%s: mutex handle %p locked %d times by %d:%ld", __func__,
		   handle, namedmutex_handle->recursion,
		   namedmutex_handle->pid, namedmutex_handle->tid);
#endif
	
	return(TRUE);
}

static gboolean namedmutex_is_owned (gpointer handle)
{
	struct _WapiHandle_namedmutex *namedmutex_handle;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDMUTEX,
				  (gpointer *)&namedmutex_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up mutex handle %p", __func__,
			   handle);
		return(FALSE);
	}
	
#ifdef DEBUG
	g_message ("%s: testing ownership mutex handle %p", __func__, handle);
#endif

	if (namedmutex_handle->recursion > 0 &&
	    namedmutex_handle->pid == _wapi_getpid () &&
	    pthread_equal (namedmutex_handle->tid, pthread_self ())) {
#ifdef DEBUG
		g_message ("%s: mutex handle %p owned by %d:%ld", __func__,
			   handle, _wapi_getpid (), pthread_self ());
#endif

		return(TRUE);
	} else {
#ifdef DEBUG
		g_message ("%s: mutex handle %p not owned by %d:%ld, but locked %d times by %d:%ld", __func__, handle, _wapi_getpid (), pthread_self (), namedmutex_handle->recursion, namedmutex_handle->pid, namedmutex_handle->tid);
#endif

		return(FALSE);
	}
}

/* The shared state is not locked when prewait methods are called */
static void namedmutex_prewait (gpointer handle)
{
	/* If the mutex is not currently owned, do nothing and let the
	 * usual wait carry on.  If it is owned, check that the owner
	 * is still alive; if it isn't we override the previous owner
	 * and assume that process exited abnormally and failed to
	 * clean up.
	 */
	struct _WapiHandle_namedmutex *namedmutex_handle;
	gboolean ok;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDMUTEX,
				  (gpointer *)&namedmutex_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named mutex handle %p",
			   __func__, handle);
		return;
	}
	
#ifdef DEBUG
	g_message ("%s: Checking ownership of named mutex handle %p", __func__,
		   handle);
#endif

	if (namedmutex_handle->recursion == 0) {
#ifdef DEBUG
		g_message ("%s: Named mutex handle %p not owned", __func__,
			   handle);
#endif
	} else if (namedmutex_handle->pid == _wapi_getpid ()) {
#ifdef DEBUG
		g_message ("%s: Named mutex handle %p owned by this process",
			   __func__, handle);
#endif
	} else {
		int thr_ret;
		gpointer proc_handle;
		
#ifdef DEBUG
		g_message ("%s: Named mutex handle %p owned by another process", __func__, handle);
#endif
		proc_handle = OpenProcess (0, 0, namedmutex_handle->pid);
		if (proc_handle == NULL) {
			/* Didn't find the process that this handle
			 * was owned by, overriding it
			 */
#ifdef DEBUG
			g_message ("%s: overriding old owner of named mutex handle %p", __func__, handle);
#endif
			thr_ret = _wapi_handle_lock_shared_handles ();
			g_assert (thr_ret == 0);

			namedmutex_handle->pid = 0;
			namedmutex_handle->tid = 0;
			namedmutex_handle->recursion = 0;

			_wapi_shared_handle_set_signal_state (handle, TRUE);
			_wapi_handle_unlock_shared_handles ();
		} else {
#ifdef DEBUG
			g_message ("%s: Found active pid %d for named mutex handle %p", __func__, namedmutex_handle->pid, handle);
#endif
		}
		if (proc_handle != NULL)
			CloseProcess (proc_handle);
	}
}

static void mutex_abandon (gpointer handle, pid_t pid, pthread_t tid)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				  (gpointer *)&mutex_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up mutex handle %p", __func__,
			   handle);
		return;
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	if (mutex_handle->pid == pid &&
	    pthread_equal (mutex_handle->tid, tid)) {
#ifdef DEBUG
		g_message ("%s: Mutex handle %p abandoned!", __func__, handle);
#endif

		mutex_handle->recursion = 0;
		mutex_handle->pid = 0;
		mutex_handle->tid = 0;
		
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
}

static void namedmutex_abandon (gpointer handle, pid_t pid, pthread_t tid)
{
	struct _WapiHandle_namedmutex *mutex_handle;
	gboolean ok;
	int thr_ret;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDMUTEX,
				  (gpointer *)&mutex_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up named mutex handle %p",
			   __func__, handle);
		return;
	}

	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
	if (mutex_handle->pid == pid &&
	    pthread_equal (mutex_handle->tid, tid)) {
#ifdef DEBUG
		g_message ("%s: Mutex handle %p abandoned!", __func__, handle);
#endif

		mutex_handle->recursion = 0;
		mutex_handle->pid = 0;
		mutex_handle->tid = 0;
		
		_wapi_shared_handle_set_signal_state (handle, TRUE);
	}

	_wapi_handle_unlock_shared_handles ();
}

/* When a thread exits, any mutexes it still holds need to be
 * signalled.  This function must not be called with the shared handle
 * lock held, as namedmutex_abandon () will try to acquire it
 */
void _wapi_mutex_abandon (gpointer data, pid_t pid, pthread_t tid)
{
	WapiHandleType type = _wapi_handle_type (data);

	if (type == WAPI_HANDLE_MUTEX) {
		mutex_abandon (data, pid, tid);
	} else if (type == WAPI_HANDLE_NAMEDMUTEX) {
		namedmutex_abandon (data, pid, tid);
	} else {
		g_assert_not_reached ();
	}
}

static gpointer mutex_create (WapiSecurityAttributes *security G_GNUC_UNUSED,
			      gboolean owned)
{
	struct _WapiHandle_mutex mutex_handle = {0};
	gpointer handle;
	int thr_ret;
	
	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a mutex
	 * was freshly created
	 */
	SetLastError (ERROR_SUCCESS);
	
#ifdef DEBUG
	g_message ("%s: Creating unnamed mutex", __func__);
#endif
	
	handle = _wapi_handle_new (WAPI_HANDLE_MUTEX, &mutex_handle);
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating mutex handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		return(NULL);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
	if(owned==TRUE) {
		mutex_own (handle);
	} else {
		_wapi_handle_set_signal_state (handle, TRUE, FALSE);
	}
	
#ifdef DEBUG
	g_message ("%s: returning mutex handle %p", __func__, handle);
#endif

	thr_ret = _wapi_handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
	pthread_cleanup_pop (0);
	
	return(handle);
}

static gpointer namedmutex_create (WapiSecurityAttributes *security G_GNUC_UNUSED, gboolean owned,
			const gunichar2 *name)
{
	struct _WapiHandle_namedmutex namedmutex_handle = {{{0}}, 0};
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;
	gpointer ret = NULL;
	guint32 namelen;
	gint32 offset;

	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a mutex
	 * was freshly created
	 */
	SetLastError (ERROR_SUCCESS);
	
	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
#ifdef DEBUG
	g_message ("%s: Creating named mutex [%s]", __func__, utf8_name);
#endif
	
	offset = _wapi_search_handle_namespace (WAPI_HANDLE_NAMEDMUTEX,
						utf8_name);
	if (offset == -1) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (offset != 0) {
		/* Not an error, but this is how the caller is
		 * informed that the mutex wasn't freshly created
		 */
		SetLastError (ERROR_ALREADY_EXISTS);
	}
	/* Fall through to create the mutex handle. */

	if (offset == 0) {
		/* A new named mutex, so create both the private and
		 * shared parts
		 */
	
		if (strlen (utf8_name) < MAX_PATH) {
			namelen = strlen (utf8_name);
		} else {
			namelen = MAX_PATH;
		}
	
		memcpy (&namedmutex_handle.sharedns.name, utf8_name, namelen);

		handle = _wapi_handle_new (WAPI_HANDLE_NAMEDMUTEX,
					   &namedmutex_handle);
	} else {
		/* A new reference to an existing named mutex, so just
		 * create the private part
		 */
		handle = _wapi_handle_new_from_offset (WAPI_HANDLE_NAMEDMUTEX,
						       offset, TRUE);
	}
	
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error creating mutex handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		goto cleanup;
	}
	ret = handle;

	if (offset == 0) {
		/* Set the initial state, as this is a completely new
		 * handle
		 */
		thr_ret = _wapi_handle_lock_shared_handles ();
		g_assert (thr_ret == 0);
	
		if (owned == TRUE) {
			namedmutex_own (handle);
		} else {
			_wapi_shared_handle_set_signal_state (handle, TRUE);
		}

		_wapi_handle_unlock_shared_handles ();
	}
	
#ifdef DEBUG
	g_message ("%s: returning mutex handle %p", __func__, handle);
#endif

cleanup:
	g_free (utf8_name);

	_wapi_namespace_unlock (NULL);
	
	return(ret);
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
	mono_once (&mutex_ops_once, mutex_ops_init);

	if (name == NULL) {
		return(mutex_create (security, owned));
	} else {
		return(namedmutex_create (security, owned, name));
	}
}

static gboolean mutex_release (gpointer handle)
{
	struct _WapiHandle_mutex *mutex_handle;
	gboolean ok;
	pthread_t tid = pthread_self ();
	pid_t pid = _wapi_getpid ();
	int thr_ret;
	gboolean ret = FALSE;
	
	ok = _wapi_lookup_handle (handle, WAPI_HANDLE_MUTEX,
				  (gpointer *)&mutex_handle);
	if (ok == FALSE) {
		g_warning ("%s: error looking up mutex handle %p", __func__,
			   handle);
		return(FALSE);
	}

	pthread_cleanup_push ((void(*)(void *))_wapi_handle_unlock_handle,
			      handle);
	thr_ret = _wapi_handle_lock_handle (handle);
	g_assert (thr_ret == 0);
	
#ifdef DEBUG
	g_message("%s: Releasing mutex handle %p", __func__, handle);
#endif

	if (!pthread_equal (mutex_handle->tid, tid) ||
	    mutex_handle->pid != pid) {
#ifdef DEBUG
		g_message("%s: We don't own mutex handle %p (owned by %d:%ld, me %d:%ld)", __func__, handle, mutex_handle->pid, mutex_handle->tid, _wapi_getpid (), tid);
#endif

		goto cleanup;
	}
	ret = TRUE;
	
	/* OK, we own this mutex */
	mutex_handle->recursion--;
	
	if(mutex_handle->recursion==0) {
		_wapi_thread_disown_mutex (handle);

#ifdef DEBUG
		g_message("%s: Unlocking mutex handle %p", __func__, handle);
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

static gboolean namedmutex_release (gpointer handle)
{
	struct _WapiHandle_namedmutex *mutex_handle;
	gboolean ok;
	pthread_t tid = pthread_self ();
	pid_t pid = _wapi_getpid ();
	int thr_ret;
	gboolean ret = FALSE;
	
	ok=_wapi_lookup_handle (handle, WAPI_HANDLE_NAMEDMUTEX,
				(gpointer *)&mutex_handle);
	if(ok==FALSE) {
		g_warning ("%s: error looking up named mutex handle %p",
			   __func__, handle);
		return(FALSE);
	}

	thr_ret = _wapi_handle_lock_shared_handles ();
	g_assert (thr_ret == 0);
	
#ifdef DEBUG
	g_message("%s: Releasing mutex handle %p", __func__, handle);
#endif

	if (!pthread_equal (mutex_handle->tid, tid) ||
	    mutex_handle->pid != pid) {
#ifdef DEBUG
		g_message("%s: We don't own mutex handle %p (owned by %d:%ld, me %d:%ld)", __func__, handle, mutex_handle->pid, mutex_handle->tid, _wapi_getpid (), tid);
#endif

		goto cleanup;
	}
	ret = TRUE;
	
	/* OK, we own this mutex */
	mutex_handle->recursion--;
	
	if(mutex_handle->recursion==0) {
		_wapi_thread_disown_mutex (handle);

#ifdef DEBUG
		g_message("%s: Unlocking mutex handle %p", __func__, handle);
#endif

		mutex_handle->pid=0;
		mutex_handle->tid=0;
		_wapi_shared_handle_set_signal_state (handle, TRUE);
	}

cleanup:
	_wapi_handle_unlock_shared_handles ();
	
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
	WapiHandleType type;

	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	type = _wapi_handle_type (handle);
	
	if (mutex_ops[type].release == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return(FALSE);
	}
	
	return(mutex_ops[type].release (handle));
}

gpointer OpenMutex (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;
	gpointer ret = NULL;
	gint32 offset;

	mono_once (&mutex_ops_once, mutex_ops_init);

	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
#ifdef DEBUG
	g_message ("%s: Opening named mutex [%s]", __func__, utf8_name);
#endif
	
	offset = _wapi_search_handle_namespace (WAPI_HANDLE_NAMEDMUTEX,
						utf8_name);
	if (offset == -1) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (offset == 0) {
		/* This name doesn't exist */
		SetLastError (ERROR_FILE_NOT_FOUND);	/* yes, really */
		goto cleanup;
	}

	/* A new reference to an existing named mutex, so just create
	 * the private part
	 */
	handle = _wapi_handle_new_from_offset (WAPI_HANDLE_NAMEDMUTEX, offset,
					       TRUE);
	
	if (handle == _WAPI_HANDLE_INVALID) {
		g_warning ("%s: error opening named mutex handle", __func__);
		SetLastError (ERROR_GEN_FAILURE);
		goto cleanup;
	}
	ret = handle;

#ifdef DEBUG
	g_message ("%s: returning named mutex handle %p", __func__, handle);
#endif

cleanup:
	g_free (utf8_name);

	_wapi_namespace_unlock (NULL);
	
	return(ret);
}
