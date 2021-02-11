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
#include "icall-decl.h"

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

static gpointer
mono_w32mutex_open (const char* utf8_name, gint32 rights G_GNUC_UNUSED, gint32 *win32error);

static void
thread_own_mutex (MonoInternalThread *internal, gpointer handle, MonoW32Handle *handle_data)
{
	// Thread and InternalThread are pinned/mature.
	// Take advantage of that and do not use handles here.

	/* if we are not on the current thread, there is a
	 * race condition when allocating internal->owned_mutexes */
	g_assert (mono_thread_internal_is_current (internal));

	if (!internal->owned_mutexes)
		internal->owned_mutexes = g_ptr_array_new ();

	g_ptr_array_add (internal->owned_mutexes, mono_w32handle_duplicate (handle_data));
}

static void
thread_disown_mutex (MonoInternalThread *internal, gpointer handle)
{
	// Thread and InternalThread are pinned/mature.
	// Take advantage of that and do not use handles here.
	gboolean removed;

	g_assert (mono_thread_internal_is_current (internal));

	g_assert (internal->owned_mutexes);
	removed = g_ptr_array_remove (internal->owned_mutexes, handle);
	g_assert (removed);

	mono_w32handle_close (handle);
}

static gint32
mutex_handle_signal (MonoW32Handle *handle_data)
{
	MonoW32HandleMutex *mutex_handle;
	pthread_t tid;

	mutex_handle = (MonoW32HandleMutex*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: signalling %s handle %p, tid: %p recursion: %d",
		__func__, mono_w32handle_get_typename (handle_data->type), handle_data, (gpointer) mutex_handle->tid, mutex_handle->recursion);

	tid = pthread_self ();

	if (mutex_handle->abandoned) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: %s handle %p is abandoned",
			__func__, mono_w32handle_get_typename (handle_data->type), handle_data);
	} else if (!pthread_equal (mutex_handle->tid, tid)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: we don't own %s handle %p (owned by %ld, me %ld)",
			__func__, mono_w32handle_get_typename (handle_data->type), handle_data, (long)mutex_handle->tid, (long)tid);
		return MONO_W32HANDLE_WAIT_RET_NOT_OWNED_BY_CALLER;
	} else {
		/* OK, we own this mutex */
		mutex_handle->recursion--;

		if (mutex_handle->recursion == 0) {
			thread_disown_mutex (mono_thread_internal_current (), handle_data);

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: unlocking %s handle %p, tid: %p recusion : %d",
				__func__, mono_w32handle_get_typename (handle_data->type), handle_data, (gpointer) mutex_handle->tid, mutex_handle->recursion);

			mutex_handle->tid = 0;
			mono_w32handle_set_signal_state (handle_data, TRUE, FALSE);
		}
	}
	return MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
}

static gboolean
mutex_handle_own (MonoW32Handle *handle_data, gboolean *abandoned)
{
	MonoW32HandleMutex *mutex_handle;

	*abandoned = FALSE;

	mutex_handle = (MonoW32HandleMutex*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: owning %s handle %p, before: [tid: %p, recursion: %d], after: [tid: %p, recursion: %d], abandoned: %s",
		__func__, mono_w32handle_get_typename (handle_data->type), handle_data, (gpointer) mutex_handle->tid, mutex_handle->recursion, (gpointer) pthread_self (), mutex_handle->recursion + 1, mutex_handle->abandoned ? "true" : "false");

	if (mutex_handle->recursion != 0) {
		g_assert (pthread_equal (pthread_self (), mutex_handle->tid));
		mutex_handle->recursion++;
	} else {
		mutex_handle->tid = pthread_self ();
		mutex_handle->recursion = 1;

		thread_own_mutex (mono_thread_internal_current (), handle_data, handle_data);
	}

	if (mutex_handle->abandoned) {
		mutex_handle->abandoned = FALSE;
		*abandoned = TRUE;
	}

	mono_w32handle_set_signal_state (handle_data, FALSE, FALSE);
	return TRUE;
}

static gboolean
mutex_handle_is_owned (MonoW32Handle *handle_data)
{
	MonoW32HandleMutex *mutex_handle;

	mutex_handle = (MonoW32HandleMutex*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: testing ownership %s handle %p",
		__func__, mono_w32handle_get_typename (handle_data->type), handle_data);

	if (mutex_handle->recursion > 0 && pthread_equal (mutex_handle->tid, pthread_self ())) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: %s handle %p owned by %p",
			__func__, mono_w32handle_get_typename (handle_data->type), handle_data, (gpointer) pthread_self ());
		return TRUE;
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: %s handle %p not owned by %p, tid: %p recursion: %d",
			__func__, mono_w32handle_get_typename (handle_data->type), handle_data, (gpointer) pthread_self (), (gpointer) mutex_handle->tid, mutex_handle->recursion);
		return FALSE;
	}
}

static void mutex_handle_prewait (MonoW32Handle *handle_data)
{
	/* If the mutex is not currently owned, do nothing and let the
	 * usual wait carry on.  If it is owned, check that the owner
	 * is still alive; if it isn't we override the previous owner
	 * and assume that process exited abnormally and failed to
	 * clean up.
	 */
	MonoW32HandleMutex *mutex_handle;

	mutex_handle = (MonoW32HandleMutex*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: pre-waiting %s handle %p, owned? %s",
		__func__, mono_w32handle_get_typename (handle_data->type), handle_data, mutex_handle->recursion != 0 ? "true" : "false");
}

static void mutex_details (MonoW32Handle *handle_data)
{
	MonoW32HandleMutex *mut = (MonoW32HandleMutex *)handle_data->specific;
	
#ifdef PTHREAD_POINTER_ID
	g_print ("own: %5p, count: %5u", mut->tid, mut->recursion);
#else
	g_print ("own: %5ld, count: %5u", mut->tid, mut->recursion);
#endif
}

static void namedmutex_details (MonoW32Handle *handle_data)
{
	MonoW32HandleNamedMutex *namedmut = (MonoW32HandleNamedMutex *)handle_data->specific;
	
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
	static const MonoW32HandleOps mutex_ops = {
		NULL,			/* close */
		mutex_handle_signal,	/* signal */
		mutex_handle_own,	/* own */
		mutex_handle_is_owned,	/* is_owned */
		NULL,			/* special_wait */
		mutex_handle_prewait,			/* prewait */
		mutex_details,	/* details */
		mutex_typename,	/* typename */
		mutex_typesize,	/* typesize */
	};

	static const MonoW32HandleOps namedmutex_ops = {
		NULL,			/* close */
		mutex_handle_signal,	/* signal */
		mutex_handle_own,	/* own */
		mutex_handle_is_owned,	/* is_owned */
		NULL,			/* special_wait */
		mutex_handle_prewait,	/* prewait */
		namedmutex_details,	/* details */
		namedmutex_typename,	/* typename */
		namedmutex_typesize,	/* typesize */
	};

	mono_w32handle_register_ops (MONO_W32TYPE_MUTEX,      &mutex_ops);
	mono_w32handle_register_ops (MONO_W32TYPE_NAMEDMUTEX, &namedmutex_ops);

	mono_w32handle_register_capabilities (MONO_W32TYPE_MUTEX,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL | MONO_W32HANDLE_CAP_OWN));
	mono_w32handle_register_capabilities (MONO_W32TYPE_NAMEDMUTEX,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SIGNAL | MONO_W32HANDLE_CAP_OWN));
}

static gpointer mutex_handle_create (MonoW32HandleMutex *mutex_handle, MonoW32Type type, gboolean owned)
{
	MonoW32Handle *handle_data;
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

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data))
		g_error ("%s: unkown handle %p", __func__, handle);

	if (handle_data->type != type)
		g_error ("%s: unknown mutex handle %p", __func__, handle);

	mono_w32handle_lock (handle_data);

	if (owned)
		mutex_handle_own (handle_data, &abandoned);
	else
		mono_w32handle_set_signal_state (handle_data, TRUE, FALSE);

	mono_w32handle_unlock (handle_data);

	/* Balance mono_w32handle_lookup_and_ref */
	mono_w32handle_unref (handle_data);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: created %s handle %p",
		__func__, mono_w32handle_get_typename (type), handle);

	return handle;
}

static gpointer mutex_create (gboolean owned)
{
	MonoW32HandleMutex mutex_handle;
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: creating %s handle",
		__func__, mono_w32handle_get_typename (MONO_W32TYPE_MUTEX));
	return mutex_handle_create (&mutex_handle, MONO_W32TYPE_MUTEX, owned);
}

static gpointer
namedmutex_create (gboolean owned, const char *utf8_name, gsize utf8_len)
{
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: creating %s handle",
		__func__, mono_w32handle_get_typename (MONO_W32TYPE_NAMEDMUTEX));

	// Opening named objects does not race.
	mono_w32handle_namespace_lock ();

	gpointer handle = mono_w32handle_namespace_search_handle (MONO_W32TYPE_NAMEDMUTEX, utf8_name);

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

		// FIXME Silent truncation.

		size_t len = utf8_len < MAX_PATH ? utf8_len : MAX_PATH;
		memcpy (&namedmutex_handle.sharedns.name [0], utf8_name, len);
		namedmutex_handle.sharedns.name [len] = '\0';

		handle = mutex_handle_create ((MonoW32HandleMutex*) &namedmutex_handle, MONO_W32TYPE_NAMEDMUTEX, owned);
	}

	mono_w32handle_namespace_unlock ();

	return handle;
}

gpointer
ves_icall_System_Threading_Mutex_CreateMutex_icall (MonoBoolean owned, const gunichar2 *name,
	gint32 name_length, MonoBoolean *created, MonoError *error)
{
	gpointer mutex;

	*created = TRUE;

	/* Need to blow away any old errors here, because code tests
	 * for ERROR_ALREADY_EXISTS on success (!) to see if a mutex
	 * was freshly created */
	mono_w32error_set_last (ERROR_SUCCESS);

	if (!name) {
		mutex = mutex_create (owned);
	} else {
		gsize utf8_name_length = 0;
		char *utf8_name = mono_utf16_to_utf8len (name, name_length, &utf8_name_length, error);
		return_val_if_nok (error, NULL);

		mutex = namedmutex_create (owned, utf8_name, utf8_name_length);

		if (mono_w32error_get_last () == ERROR_ALREADY_EXISTS)
			*created = FALSE;
		g_free (utf8_name);
	}

	return mutex;
}

MonoBoolean
ves_icall_System_Threading_Mutex_ReleaseMutex_internal (gpointer handle)
{
	MonoW32Handle *handle_data;
	MonoW32HandleMutex *mutex_handle;
	pthread_t tid;
	gboolean ret;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		g_warning ("%s: unkown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_MUTEX && handle_data->type != MONO_W32TYPE_NAMEDMUTEX) {
		g_warning ("%s: unknown mutex handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	mutex_handle = (MonoW32HandleMutex*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: releasing %s handle %p, tid: %p recursion: %d",
		__func__, mono_w32handle_get_typename (handle_data->type), handle, (gpointer) mutex_handle->tid, mutex_handle->recursion);

	mono_w32handle_lock (handle_data);

	tid = pthread_self ();

	if (mutex_handle->abandoned) {
		// The Win32 ReleaseMutex() function returns TRUE for abandoned mutexes
		ret = TRUE;
	} else if (!pthread_equal (mutex_handle->tid, tid)) {
		ret = FALSE;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: we don't own %s handle %p (owned by %ld, me %ld)",
			    __func__, mono_w32handle_get_typename (handle_data->type), handle, (long)mutex_handle->tid, (long)tid);
	} else {
		ret = TRUE;

		/* OK, we own this mutex */
		mutex_handle->recursion--;

		if (mutex_handle->recursion == 0) {
			thread_disown_mutex (mono_thread_internal_current (), handle);

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: unlocking %s handle %p, tid: %p recusion : %d",
				__func__, mono_w32handle_get_typename (handle_data->type), handle, (gpointer) mutex_handle->tid, mutex_handle->recursion);

			mutex_handle->tid = 0;
			mono_w32handle_set_signal_state (handle_data, TRUE, FALSE);
		}
	}

	mono_w32handle_unlock (handle_data);
	mono_w32handle_unref (handle_data);

	return ret;
}

gpointer
ves_icall_System_Threading_Mutex_OpenMutex_icall (const gunichar2 *name, gint32 name_length, gint32 rights G_GNUC_UNUSED, gint32 *win32error, MonoError *error)
{
	*win32error = ERROR_SUCCESS;
	char *utf8_name = mono_utf16_to_utf8 (name, name_length, error);
	return_val_if_nok (error, NULL);
	gpointer handle = mono_w32mutex_open (utf8_name, rights, win32error);
	g_free (utf8_name);
	return handle;
}

gpointer
mono_w32mutex_open (const char* utf8_name, gint32 rights G_GNUC_UNUSED, gint32 *win32error)
{
	*win32error = ERROR_SUCCESS;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: Opening named mutex [%s]",
		__func__, utf8_name);

	// Opening named objects does not race.
	mono_w32handle_namespace_lock ();

	gpointer handle = mono_w32handle_namespace_search_handle (MONO_W32TYPE_NAMEDMUTEX, utf8_name);

	mono_w32handle_namespace_unlock ();

	if (handle == INVALID_HANDLE_VALUE) {
		/* The name has already been used for a different object. */
		*win32error = ERROR_INVALID_HANDLE;
		return handle;
	} else if (!handle) {
		/* This name doesn't exist */
		*win32error = ERROR_FILE_NOT_FOUND;
		return handle;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: returning named mutex handle %p",
		__func__, handle);

	return handle;
}

void
mono_w32mutex_abandon (MonoInternalThread *internal)
{
	// Thread and InternalThread are pinned/mature.
	// Take advantage of that and do not use handles here.
	g_assert (mono_thread_internal_is_current (internal));

	if (!internal->owned_mutexes)
		return;

	while (internal->owned_mutexes->len) {
		MonoW32Handle *handle_data;
		MonoW32HandleMutex *mutex_handle;
		MonoNativeThreadId tid;
		gpointer handle;

		handle = g_ptr_array_index (internal->owned_mutexes, 0);

		if (!mono_w32handle_lookup_and_ref (handle, &handle_data))
			g_error ("%s: unkown handle %p", __func__, handle);

		if (handle_data->type != MONO_W32TYPE_MUTEX && handle_data->type != MONO_W32TYPE_NAMEDMUTEX)
			g_error ("%s: unkown mutex handle %p", __func__, handle);

		mutex_handle = (MonoW32HandleMutex*) handle_data->specific;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: abandoning %s handle %p",
			__func__, mono_w32handle_get_typename (handle_data->type), handle);

		tid = MONO_UINT_TO_NATIVE_THREAD_ID (internal->tid);

		if (!pthread_equal (mutex_handle->tid, tid))
			g_error ("%s: trying to release mutex %p acquired by thread %p from thread %p",
				__func__, handle, (gpointer) mutex_handle->tid, (gpointer) tid);

		mono_w32handle_lock (handle_data);

		mutex_handle->recursion = 0;
		mutex_handle->tid = 0;
		mutex_handle->abandoned = TRUE;

		mono_w32handle_set_signal_state (handle_data, TRUE, FALSE);

		thread_disown_mutex (internal, handle);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_MUTEX, "%s: abandoned %s handle %p",
			__func__, mono_w32handle_get_typename (handle_data->type), handle);

		mono_w32handle_unlock (handle_data);
		mono_w32handle_unref (handle_data);
	}

	g_ptr_array_free (internal->owned_mutexes, TRUE);
	internal->owned_mutexes = NULL;
}

MonoW32HandleNamespace*
mono_w32mutex_get_namespace (MonoW32HandleNamedMutex *mutex)
{
	return &mutex->sharedns;
}
