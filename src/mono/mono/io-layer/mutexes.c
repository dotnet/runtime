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
#include <mono/io-layer/mutex-private.h>
#include <mono/io-layer/io-trace.h>
#include <mono/utils/mono-once.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/w32handle.h>

static void mutex_signal(gpointer handle);
static gboolean mutex_own (gpointer handle);
static gboolean mutex_is_owned (gpointer handle);
static void mutex_prewait (gpointer handle);
static void mutex_details (gpointer data);
static const gchar* mutex_typename (void);
static gsize mutex_typesize (void);

static void namedmutex_signal (gpointer handle);
static gboolean namedmutex_own (gpointer handle);
static gboolean namedmutex_is_owned (gpointer handle);
static void namedmutex_prewait (gpointer handle);
static void namedmutex_details (gpointer data);
static const gchar* namedmutex_typename (void);
static gsize namedmutex_typesize (void);

static MonoW32HandleOps _wapi_mutex_ops = {
	NULL,			/* close */
	mutex_signal,		/* signal */
	mutex_own,		/* own */
	mutex_is_owned,		/* is_owned */
	NULL,			/* special_wait */
	mutex_prewait,			/* prewait */
	mutex_details,	/* details */
	mutex_typename,	/* typename */
	mutex_typesize,	/* typesize */
};

static MonoW32HandleOps _wapi_namedmutex_ops = {
	NULL,			/* close */
	namedmutex_signal,	/* signal */
	namedmutex_own,		/* own */
	namedmutex_is_owned,	/* is_owned */
	NULL,			/* special_wait */
	namedmutex_prewait,	/* prewait */
	namedmutex_details,	/* details */
	namedmutex_typename,	/* typename */
	namedmutex_typesize,	/* typesize */
};

void
_wapi_mutex_init (void)
{
	mono_w32handle_register_ops (MONO_W32HANDLE_MUTEX,      &_wapi_mutex_ops);
	mono_w32handle_register_ops (MONO_W32HANDLE_NAMEDMUTEX, &_wapi_namedmutex_ops);

	mono_w32handle_register_capabilities (MONO_W32HANDLE_MUTEX,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL | MONO_W32HANDLE_CAP_OWN));
	mono_w32handle_register_capabilities (MONO_W32HANDLE_NAMEDMUTEX,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL | MONO_W32HANDLE_CAP_OWN));
}

static const char* mutex_handle_type_to_string (MonoW32HandleType type)
{
	switch (type) {
	case MONO_W32HANDLE_MUTEX: return "mutex";
	case MONO_W32HANDLE_NAMEDMUTEX: return "named mutex";
	default:
		g_assert_not_reached ();
	}
}

static gboolean
mutex_handle_own (gpointer handle, MonoW32HandleType type)
{
	struct _WapiHandle_mutex *mutex_handle;

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p", __func__, mutex_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: owning %s handle %p, tid %p, recursion %u",
		__func__, mutex_handle_type_to_string (type), handle, (gpointer) mutex_handle->tid, mutex_handle->recursion);

	mono_thread_info_own_mutex (mono_thread_info_current (), handle);

	mutex_handle->tid = pthread_self ();
	mutex_handle->recursion++;

	mono_w32handle_set_signal_state (handle, FALSE, FALSE);

	return TRUE;
}

static gboolean
mutex_handle_is_owned (gpointer handle, MonoW32HandleType type)
{
	struct _WapiHandle_mutex *mutex_handle;

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p", __func__, mutex_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: testing ownership %s handle %p",
		__func__, mutex_handle_type_to_string (type), handle);

	if (mutex_handle->recursion > 0 && pthread_equal (mutex_handle->tid, pthread_self ())) {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: %s handle %p owned by %p",
			__func__, mutex_handle_type_to_string (type), handle, (gpointer) pthread_self ());
		return TRUE;
	} else {
		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: %s handle %p not owned by %p, but locked %d times by %p",
			__func__, mutex_handle_type_to_string (type), handle, (gpointer) pthread_self (), mutex_handle->recursion, (gpointer) mutex_handle->tid);
		return FALSE;
	}
}

static void mutex_signal(gpointer handle)
{
	ReleaseMutex(handle);
}

static gboolean mutex_own (gpointer handle)
{
	return mutex_handle_own (handle, MONO_W32HANDLE_MUTEX);
}

static gboolean mutex_is_owned (gpointer handle)
{
	
	return mutex_handle_is_owned (handle, MONO_W32HANDLE_MUTEX);
}

static void namedmutex_signal (gpointer handle)
{
	ReleaseMutex(handle);
}

/* NB, always called with the shared handle lock held */
static gboolean namedmutex_own (gpointer handle)
{
	return mutex_handle_own (handle, MONO_W32HANDLE_NAMEDMUTEX);
}

static gboolean namedmutex_is_owned (gpointer handle)
{
	return mutex_handle_is_owned (handle, MONO_W32HANDLE_NAMEDMUTEX);
}

static void mutex_handle_prewait (gpointer handle, MonoW32HandleType type)
{
	/* If the mutex is not currently owned, do nothing and let the
	 * usual wait carry on.  If it is owned, check that the owner
	 * is still alive; if it isn't we override the previous owner
	 * and assume that process exited abnormally and failed to
	 * clean up.
	 */
	struct _WapiHandle_mutex *mutex_handle;

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, mutex_handle_type_to_string (type), handle);
		return;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: pre-waiting %s handle %p, owned? %s",
		__func__, mutex_handle_type_to_string (type), handle, mutex_handle->recursion != 0 ? "true" : "false");
}

/* The shared state is not locked when prewait methods are called */
static void mutex_prewait (gpointer handle)
{
	mutex_handle_prewait (handle, MONO_W32HANDLE_MUTEX);
}

/* The shared state is not locked when prewait methods are called */
static void namedmutex_prewait (gpointer handle)
{
	mutex_handle_prewait (handle, MONO_W32HANDLE_NAMEDMUTEX);
}

static void mutex_details (gpointer data)
{
	struct _WapiHandle_mutex *mut = (struct _WapiHandle_mutex *)data;
	
#ifdef PTHREAD_POINTER_ID
	g_print ("own: %5p, count: %5u", mut->tid, mut->recursion);
#else
	g_print ("own: %5ld, count: %5u", mut->tid, mut->recursion);
#endif
}

static void namedmutex_details (gpointer data)
{
	struct _WapiHandle_namedmutex *namedmut = (struct _WapiHandle_namedmutex *)data;
	
#ifdef PTHREAD_POINTER_ID
	g_print ("own: %5p, count: %5u, name: \"%s\"",
		namedmut->m.tid, namedmut->m.recursion, namedmut->sharedns.name);
#else
	g_print ("own: %5ld, count: %5u, name: \"%s\"",
		namedmut->m.tid, namedmut->m.recursion, namedmut->sharedns.name);
#endif
}

static const gchar* mutex_typename (void)
{
	return "Mutex";
}

static gsize mutex_typesize (void)
{
	return sizeof (struct _WapiHandle_mutex);
}

static const gchar* namedmutex_typename (void)
{
	return "N.Mutex";
}

static gsize namedmutex_typesize (void)
{
	return sizeof (struct _WapiHandle_namedmutex);
}

/* When a thread exits, any mutexes it still holds need to be signalled. */
void wapi_mutex_abandon (gpointer handle, pid_t pid, pthread_t tid)
{
	MonoW32HandleType type;
	struct _WapiHandle_mutex *mutex_handle;
	int thr_ret;

	switch (type = mono_w32handle_get_type (handle)) {
	case MONO_W32HANDLE_MUTEX:
	case MONO_W32HANDLE_NAMEDMUTEX:
		break;
	default:
		g_assert_not_reached ();
	}

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, mutex_handle_type_to_string (type), handle);
		return;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: abandon %s handle %p",
		__func__, mutex_handle_type_to_string (type), handle);

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (pthread_equal (mutex_handle->tid, tid)) {
		mutex_handle->recursion = 0;
		mutex_handle->tid = 0;

		mono_w32handle_set_signal_state (handle, TRUE, FALSE);

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: abandoned %s handle %p",
			__func__, mutex_handle_type_to_string (type), handle);
	}

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);
}

static gpointer mutex_handle_create (struct _WapiHandle_mutex *mutex_handle, MonoW32HandleType type, gboolean owned)
{
	gpointer handle;
	int thr_ret;

	mutex_handle->tid = 0;
	mutex_handle->recursion = 0;

	handle = mono_w32handle_new (type, mutex_handle);
	if (handle == INVALID_HANDLE_VALUE) {
		g_warning ("%s: error creating %s handle",
			__func__, mutex_handle_type_to_string (type));
		SetLastError (ERROR_GEN_FAILURE);
		return NULL;
	}

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	if (owned)
		mutex_handle_own (handle, type);
	else
		mono_w32handle_set_signal_state (handle, TRUE, FALSE);

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: created %s handle %p",
		__func__, mutex_handle_type_to_string (type), handle);

	return handle;
}

static gpointer mutex_create (gboolean owned)
{
	struct _WapiHandle_mutex mutex_handle;
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle",
		__func__, mutex_handle_type_to_string (MONO_W32HANDLE_MUTEX));
	return mutex_handle_create (&mutex_handle, MONO_W32HANDLE_MUTEX, owned);
}

static gpointer namedmutex_create (gboolean owned, const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle",
		__func__, mutex_handle_type_to_string (MONO_W32HANDLE_NAMEDMUTEX));

	/* w32 seems to guarantee that opening named objects can't race each other */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);

	handle = _wapi_search_handle_namespace (MONO_W32HANDLE_NAMEDMUTEX, utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		handle = NULL;
		SetLastError (ERROR_INVALID_HANDLE);
	} else if (handle) {
		/* Not an error, but this is how the caller is informed that the mutex wasn't freshly created */
		SetLastError (ERROR_ALREADY_EXISTS);

		/* this is used as creating a new handle */
		mono_w32handle_ref (handle);
	} else {
		/* A new named mutex */
		struct _WapiHandle_namedmutex namedmutex_handle;

		strncpy (&namedmutex_handle.sharedns.name [0], utf8_name, MAX_PATH);
		namedmutex_handle.sharedns.name [MAX_PATH] = '\0';

		handle = mutex_handle_create ((struct _WapiHandle_mutex*) &namedmutex_handle, MONO_W32HANDLE_NAMEDMUTEX, owned);
	}

	g_free (utf8_name);

	thr_ret = _wapi_namespace_unlock (NULL);
	g_assert (thr_ret == 0);

	return handle;
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
gpointer CreateMutex(WapiSecurityAttributes *security G_GNUC_UNUSED, gboolean owned, const gunichar2 *name)
{
	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a mutex
	 * was freshly created */
	SetLastError (ERROR_SUCCESS);

	return name ? namedmutex_create (owned, name) : mutex_create (owned);
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
	MonoW32HandleType type;
	struct _WapiHandle_mutex *mutex_handle;
	pthread_t tid;
	int thr_ret;
	gboolean ret;

	if (handle == NULL) {
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (type = mono_w32handle_get_type (handle)) {
	case MONO_W32HANDLE_MUTEX:
	case MONO_W32HANDLE_NAMEDMUTEX:
		break;
	default:
		SetLastError (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, mutex_handle_type_to_string (type), handle);
		return FALSE;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: releasing %s handle %p",
		__func__, mutex_handle_type_to_string (type), handle);

	thr_ret = mono_w32handle_lock_handle (handle);
	g_assert (thr_ret == 0);

	tid = pthread_self ();

	if (!pthread_equal (mutex_handle->tid, tid)) {
		ret = FALSE;

		MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: we don't own %s handle %p (owned by %ld, me %ld)",
			__func__, mutex_handle_type_to_string (type), handle, mutex_handle->tid, tid);
	} else {
		ret = TRUE;

		/* OK, we own this mutex */
		mutex_handle->recursion--;

		if (mutex_handle->recursion == 0) {
			mono_thread_info_disown_mutex (mono_thread_info_current (), handle);

			MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: unlocking %s handle %p",
				__func__, mutex_handle_type_to_string (type), handle);

			mutex_handle->tid = 0;
			mono_w32handle_set_signal_state (handle, TRUE, FALSE);
		}
	}

	thr_ret = mono_w32handle_unlock_handle (handle);
	g_assert (thr_ret == 0);

	return ret;
}

gpointer OpenMutex (guint32 access G_GNUC_UNUSED, gboolean inherit G_GNUC_UNUSED, const gunichar2 *name)
{
	gpointer handle;
	gchar *utf8_name;
	int thr_ret;

	/* w32 seems to guarantee that opening named objects can't
	 * race each other
	 */
	thr_ret = _wapi_namespace_lock ();
	g_assert (thr_ret == 0);

	utf8_name = g_utf16_to_utf8 (name, -1, NULL, NULL, NULL);
	
	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Opening named mutex [%s]", __func__, utf8_name);
	
	handle = _wapi_search_handle_namespace (MONO_W32HANDLE_NAMEDMUTEX,
						utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different
		 * object.
		 */
		SetLastError (ERROR_INVALID_HANDLE);
		goto cleanup;
	} else if (!handle) {
		/* This name doesn't exist */
		SetLastError (ERROR_FILE_NOT_FOUND);	/* yes, really */
		goto cleanup;
	}

	MONO_TRACE (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: returning named mutex handle %p", __func__, handle);

cleanup:
	g_free (utf8_name);

	_wapi_namespace_unlock (NULL);
	
	return handle;
}
