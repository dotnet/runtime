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

#ifndef HOST_WIN32
static GENERATE_GET_CLASS_WITH_CACHE (wait_subsystem, "System.Threading", "WaitSubsystem");

void
mono_w32mutex_abandon (MonoInternalThread *internal)
{
	ERROR_DECL (error);

	MONO_STATIC_POINTER_INIT (MonoMethod, thread_exiting)

		MonoClass *wait_subsystem_class = mono_class_get_wait_subsystem_class ();
		g_assert (wait_subsystem_class);
		thread_exiting = mono_class_get_method_from_name_checked (wait_subsystem_class, "OnThreadExiting", -1, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, thread_exiting)

	g_assert (thread_exiting);

	if (mono_runtime_get_no_exec ())
		return;

	HANDLE_FUNCTION_ENTER ();

	ERROR_DECL (error);

	gpointer args [1];
	args [0] = internal;
	mono_runtime_try_invoke_handle (thread_exiting, NULL_HANDLE, args, error);

	mono_error_cleanup (error);

	HANDLE_FUNCTION_RETURN ();
}
#endif

MonoW32HandleNamespace*
mono_w32mutex_get_namespace (MonoW32HandleNamedMutex *mutex)
{
	return &mutex->sharedns;
}
