/**
 * \file
 * Runtime support for managed Mutex on Unix
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32mutex.h"

#include <pthread.h>

#include "w32error.h"
#include "w32handle-namespace.h"
#include "mono/metadata/object-internals.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-threads.h"
#include "mono/metadata/w32handle.h"

#define MAX_PATH 260

typedef struct {
	MonoNativeThreadId tid;
	guint32 recursion;
	gboolean abandoned;
} MonoW32HandleMutex;

struct MonoW32HandleNamedMutex {
	MonoW32HandleMutex m;
	MonoW32HandleNamespace sharedns;
};

static void
thread_own_mutex (MonoInternalThread *internal, gpointer handle)
{
	mono_w32handle_ref (handle);

	/* if we are not on the current thread, there is a
	 * race condition when allocating internal->owned_mutexes */
	g_assert (mono_thread_internal_is_current (internal));

	if (!internal->owned_mutexes)
		internal->owned_mutexes = g_ptr_array_new ();

	g_ptr_array_add (internal->owned_mutexes, handle);
}

static void
thread_disown_mutex (MonoInternalThread *internal, gpointer handle)
{
	gboolean removed;

	g_assert (mono_thread_internal_is_current (internal));

	g_assert (internal->owned_mutexes);
	removed = g_ptr_array_remove (internal->owned_mutexes, handle);
	g_assert (removed);

	mono_w32handle_unref (handle);
}

static gboolean
mutex_handle_own (gpointer handle, MonoW32HandleType type, gboolean *abandoned)
{
	MonoW32HandleMutex *mutex_handle;

	*abandoned = FALSE;

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p", __func__, mono_w32handle_get_typename (type), handle);
		return FALSE;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: owning %s handle %p, before: [tid: %p, recursion: %d], after: [tid: %p, recursion: %d], abandoned: %s",
		__func__, mono_w32handle_get_typename (type), handle, (gpointer) mutex_handle->tid, mutex_handle->recursion, (gpointer) pthread_self (), mutex_handle->recursion + 1, mutex_handle->abandoned ? "true" : "false");

	if (mutex_handle->recursion != 0) {
		g_assert (pthread_equal (pthread_self (), mutex_handle->tid));
		mutex_handle->recursion++;
	} else {
		mutex_handle->tid = pthread_self ();
		mutex_handle->recursion = 1;

		thread_own_mutex (mono_thread_internal_current (), handle);
	}

	if (mutex_handle->abandoned) {
		mutex_handle->abandoned = FALSE;
		*abandoned = TRUE;
	}

	mono_w32handle_set_signal_state (handle, FALSE, FALSE);

	return TRUE;
}

static gboolean
mutex_handle_is_owned (gpointer handle, MonoW32HandleType type)
{
	MonoW32HandleMutex *mutex_handle;

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p", __func__, mono_w32handle_get_typename (type), handle);
		return FALSE;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: testing ownership %s handle %p",
		__func__, mono_w32handle_get_typename (type), handle);

	if (mutex_handle->recursion > 0 && pthread_equal (mutex_handle->tid, pthread_self ())) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: %s handle %p owned by %p",
			__func__, mono_w32handle_get_typename (type), handle, (gpointer) pthread_self ());
		return TRUE;
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: %s handle %p not owned by %p, tid: %p recursion: %d",
			__func__, mono_w32handle_get_typename (type), handle, (gpointer) pthread_self (), (gpointer) mutex_handle->tid, mutex_handle->recursion);
		return FALSE;
	}
}

static void mutex_signal(gpointer handle)
{
	ves_icall_System_Threading_Mutex_ReleaseMutex_internal (handle);
}

static gboolean mutex_own (gpointer handle, gboolean *abandoned)
{
	return mutex_handle_own (handle, MONO_W32HANDLE_MUTEX, abandoned);
}

static gboolean mutex_is_owned (gpointer handle)
{
	
	return mutex_handle_is_owned (handle, MONO_W32HANDLE_MUTEX);
}

static void namedmutex_signal (gpointer handle)
{
	ves_icall_System_Threading_Mutex_ReleaseMutex_internal (handle);
}

/* NB, always called with the shared handle lock held */
static gboolean namedmutex_own (gpointer handle, gboolean *abandoned)
{
	return mutex_handle_own (handle, MONO_W32HANDLE_NAMEDMUTEX, abandoned);
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
	MonoW32HandleMutex *mutex_handle;

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, mono_w32handle_get_typename (type), handle);
		return;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: pre-waiting %s handle %p, owned? %s",
		__func__, mono_w32handle_get_typename (type), handle, mutex_handle->recursion != 0 ? "true" : "false");
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
	MonoW32HandleMutex *mut = (MonoW32HandleMutex *)data;
	
#ifdef PTHREAD_POINTER_ID
	g_print ("own: %5p, count: %5u", mut->tid, mut->recursion);
#else
	g_print ("own: %5ld, count: %5u", mut->tid, mut->recursion);
#endif
}

static void namedmutex_details (gpointer data)
{
	MonoW32HandleNamedMutex *namedmut = (MonoW32HandleNamedMutex *)data;
	
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
	return sizeof (MonoW32HandleMutex);
}

static const gchar* namedmutex_typename (void)
{
	return "N.Mutex";
}

static gsize namedmutex_typesize (void)
{
	return sizeof (MonoW32HandleNamedMutex);
}

void
mono_w32mutex_init (void)
{
	static MonoW32HandleOps mutex_ops = {
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

	static MonoW32HandleOps namedmutex_ops = {
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

	mono_w32handle_register_ops (MONO_W32HANDLE_MUTEX,      &mutex_ops);
	mono_w32handle_register_ops (MONO_W32HANDLE_NAMEDMUTEX, &namedmutex_ops);

	mono_w32handle_register_capabilities (MONO_W32HANDLE_MUTEX,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL | MONO_W32HANDLE_CAP_OWN));
	mono_w32handle_register_capabilities (MONO_W32HANDLE_NAMEDMUTEX,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL | MONO_W32HANDLE_CAP_OWN));
}

static gpointer mutex_handle_create (MonoW32HandleMutex *mutex_handle, MonoW32HandleType type, gboolean owned)
{
	gpointer handle;
	gboolean abandoned;

	mutex_handle->tid = 0;
	mutex_handle->recursion = 0;
	mutex_handle->abandoned = FALSE;

	handle = mono_w32handle_new (type, mutex_handle);
	if (handle == INVALID_HANDLE_VALUE) {
		g_warning ("%s: error creating %s handle",
			__func__, mono_w32handle_get_typename (type));
		mono_w32error_set_last (ERROR_GEN_FAILURE);
		return NULL;
	}

	mono_w32handle_lock_handle (handle);

	if (owned)
		mutex_handle_own (handle, type, &abandoned);
	else
		mono_w32handle_set_signal_state (handle, TRUE, FALSE);

	mono_w32handle_unlock_handle (handle);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: created %s handle %p",
		__func__, mono_w32handle_get_typename (type), handle);

	return handle;
}

static gpointer mutex_create (gboolean owned)
{
	MonoW32HandleMutex mutex_handle;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle",
		__func__, mono_w32handle_get_typename (MONO_W32HANDLE_MUTEX));
	return mutex_handle_create (&mutex_handle, MONO_W32HANDLE_MUTEX, owned);
}

static gpointer namedmutex_create (gboolean owned, const gchar *utf8_name)
{
	gpointer handle;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: creating %s handle",
		__func__, mono_w32handle_get_typename (MONO_W32HANDLE_NAMEDMUTEX));

	/* w32 seems to guarantee that opening named objects can't race each other */
	mono_w32handle_namespace_lock ();

	glong utf8_len = strlen (utf8_name);

	handle = mono_w32handle_namespace_search_handle (MONO_W32HANDLE_NAMEDMUTEX, utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		handle = NULL;
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
	} else if (handle) {
		/* Not an error, but this is how the caller is informed that the mutex wasn't freshly created */
		mono_w32error_set_last (ERROR_ALREADY_EXISTS);

		/* mono_w32handle_namespace_search_handle already adds a ref to the handle */
	} else {
		/* A new named mutex */
		MonoW32HandleNamedMutex namedmutex_handle;

		size_t len = utf8_len < MAX_PATH ? utf8_len : MAX_PATH;
		memcpy (&namedmutex_handle.sharedns.name [0], utf8_name, len);
		namedmutex_handle.sharedns.name [len] = '\0';

		handle = mutex_handle_create ((MonoW32HandleMutex*) &namedmutex_handle, MONO_W32HANDLE_NAMEDMUTEX, owned);
	}

	mono_w32handle_namespace_unlock ();

	return handle;
}

gpointer
ves_icall_System_Threading_Mutex_CreateMutex_internal (MonoBoolean owned, MonoStringHandle name, MonoBoolean *created, MonoError *error)
{
	gpointer mutex;

	error_init (error);
	*created = TRUE;

	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a mutex
	 * was freshly created */
	mono_w32error_set_last (ERROR_SUCCESS);

	if (MONO_HANDLE_IS_NULL (name)) {
		mutex = mutex_create (owned);
	} else {
		gchar *utf8_name = mono_string_handle_to_utf8 (name, error);
		return_val_if_nok (error, NULL);

		mutex = namedmutex_create (owned, utf8_name);

		if (mono_w32error_get_last () == ERROR_ALREADY_EXISTS)
			*created = FALSE;
		g_free (utf8_name);
	}

	return mutex;
}

MonoBoolean
ves_icall_System_Threading_Mutex_ReleaseMutex_internal (gpointer handle)
{
	MonoW32HandleType type;
	MonoW32HandleMutex *mutex_handle;
	pthread_t tid;
	gboolean ret;

	if (handle == NULL) {
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	switch (type = mono_w32handle_get_type (handle)) {
	case MONO_W32HANDLE_MUTEX:
	case MONO_W32HANDLE_NAMEDMUTEX:
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
		g_warning ("%s: error looking up %s handle %p",
			__func__, mono_w32handle_get_typename (type), handle);
		return FALSE;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: releasing %s handle %p, tid: %p recursion: %d",
		__func__, mono_w32handle_get_typename (type), handle, (gpointer) mutex_handle->tid, mutex_handle->recursion);

	mono_w32handle_lock_handle (handle);

	tid = pthread_self ();

	if (mutex_handle->abandoned) {
		// The Win32 ReleaseMutex() function returns TRUE for abandoned mutexes
		ret = TRUE;
	} else if (!pthread_equal (mutex_handle->tid, tid)) {
		ret = FALSE;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: we don't own %s handle %p (owned by %ld, me %ld)",
			    __func__, mono_w32handle_get_typename (type), handle, (long)mutex_handle->tid, (long)tid);
	} else {
		ret = TRUE;

		/* OK, we own this mutex */
		mutex_handle->recursion--;

		if (mutex_handle->recursion == 0) {
			thread_disown_mutex (mono_thread_internal_current (), handle);

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: unlocking %s handle %p, tid: %p recusion : %d",
				__func__, mono_w32handle_get_typename (type), handle, (gpointer) mutex_handle->tid, mutex_handle->recursion);

			mutex_handle->tid = 0;
			mono_w32handle_set_signal_state (handle, TRUE, FALSE);
		}
	}

	mono_w32handle_unlock_handle (handle);

	return ret;
}

gpointer
ves_icall_System_Threading_Mutex_OpenMutex_internal (MonoString *name, gint32 rights G_GNUC_UNUSED, gint32 *error)
{
	gpointer handle;
	gchar *utf8_name;

	*error = ERROR_SUCCESS;

	/* w32 seems to guarantee that opening named objects can't race each other */
	mono_w32handle_namespace_lock ();

	utf8_name = g_utf16_to_utf8 (mono_string_chars (name), -1, NULL, NULL, NULL);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: Opening named mutex [%s]",
		__func__, utf8_name);

	handle = mono_w32handle_namespace_search_handle (MONO_W32HANDLE_NAMEDMUTEX, utf8_name);
	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		*error = ERROR_INVALID_HANDLE;
		goto cleanup;
	} else if (!handle) {
		/* This name doesn't exist */
		*error = ERROR_FILE_NOT_FOUND;
		goto cleanup;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: returning named mutex handle %p",
		__func__, handle);

cleanup:
	g_free (utf8_name);

	mono_w32handle_namespace_unlock ();

	return handle;
}

void
mono_w32mutex_abandon (void)
{
	MonoInternalThread *internal;

	g_assert (mono_thread_internal_current_is_attached ());

	internal = mono_thread_internal_current ();
	g_assert (internal);

	if (!internal->owned_mutexes)
		return;

	while (internal->owned_mutexes->len) {
		MonoW32HandleType type;
		MonoW32HandleMutex *mutex_handle;
		MonoNativeThreadId tid;
		gpointer handle;

		handle = g_ptr_array_index (internal->owned_mutexes, 0);

		switch (type = mono_w32handle_get_type (handle)) {
		case MONO_W32HANDLE_MUTEX:
		case MONO_W32HANDLE_NAMEDMUTEX:
			break;
		default:
			g_assert_not_reached ();
		}

		if (!mono_w32handle_lookup (handle, type, (gpointer *)&mutex_handle)) {
			g_error ("%s: error looking up %s handle %p",
				__func__, mono_w32handle_get_typename (type), handle);
		}

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: abandoning %s handle %p",
			__func__, mono_w32handle_get_typename (type), handle);

		tid = MONO_UINT_TO_NATIVE_THREAD_ID (internal->tid);

		if (!pthread_equal (mutex_handle->tid, tid))
			g_error ("%s: trying to release mutex %p acquired by thread %p from thread %p",
				__func__, handle, (gpointer) mutex_handle->tid, (gpointer) tid);

		mono_w32handle_lock_handle (handle);

		mutex_handle->recursion = 0;
		mutex_handle->tid = 0;
		mutex_handle->abandoned = TRUE;

		mono_w32handle_set_signal_state (handle, TRUE, FALSE);

		thread_disown_mutex (internal, handle);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER, "%s: abandoned %s handle %p",
			__func__, mono_w32handle_get_typename (type), handle);

		mono_w32handle_unlock_handle (handle);
	}

	g_ptr_array_free (internal->owned_mutexes, TRUE);
	internal->owned_mutexes = NULL;
}

MonoW32HandleNamespace*
mono_w32mutex_get_namespace (MonoW32HandleNamedMutex *mutex)
{
	return &mutex->sharedns;
}
