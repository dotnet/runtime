/**
 * \file
 * Microsoft threadpool runtime support
 *
 * Author:
 *	Ludovic Henry (ludovic.henry@xamarin.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Files:
//  - src/vm/comthreadpool.cpp
//  - src/vm/win32threadpoolcpp
//  - src/vm/threadpoolrequest.cpp
//  - src/vm/hillclimbing.cpp
//
// Ported from C++ to C and adjusted to Mono runtime

//
// NETCORE version, based on threadpool.c with domains support removed
//

#include <config.h>

#ifdef ENABLE_NETCORE

#include <stdlib.h>
#define _USE_MATH_DEFINES // needed by MSVC to define math constants
#include <math.h>

#include <glib.h>

#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/threadpool-worker.h>
#include <mono/metadata/threadpool-io.h>
#include <mono/metadata/w32event.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-complex.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/refcount.h>
#include <mono/utils/mono-os-wait.h>
#include "monitor.h"
#include "icall-decl.h"

// consistency with coreclr https://github.com/dotnet/coreclr/blob/643b09f966e68e06d5f0930755985a01a2a2b096/src/vm/win32threadpool.h#L111
#define MAX_POSSIBLE_THREADS 0x7fff

typedef union {
	struct {
		gint16 starting; /* starting, but not yet in worker_callback */
		gint16 working; /* executing worker_callback */
	} _;
	gint32 as_gint32;
} ThreadPoolCounter;

typedef struct {
	MonoRefCount ref;

	MonoCoopMutex tp_lock;

	ThreadPoolCounter counters;

	/* Number of outstanding jobs */
	gint32 outstanding_request;
	/* Number of currently executing jobs */
	gint32 threadpool_jobs;
	/* Signalled when threadpool_jobs + outstanding_request is 0 */
	/* Protected by tp_lock */
	MonoCoopCond cleanup_cond;

	gint32 limit_io_min;
	gint32 limit_io_max;
} ThreadPool;

static mono_lazy_init_t status = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static ThreadPool threadpool;

#define COUNTER_ATOMIC(var,block) \
	do { \
		ThreadPoolCounter __old; \
		do { \
			(var) = __old = COUNTER_READ (); \
			{ block; } \
			if (!(counter._.starting >= 0)) \
				g_error ("%s: counter._.starting = %d, but should be >= 0", __func__, counter._.starting); \
			if (!(counter._.working >= 0)) \
				g_error ("%s: counter._.working = %d, but should be >= 0", __func__, counter._.working); \
		} while (mono_atomic_cas_i32 (&threadpool.counters.as_gint32, (var).as_gint32, __old.as_gint32) != __old.as_gint32); \
	} while (0)

static inline ThreadPoolCounter
COUNTER_READ (void)
{
	ThreadPoolCounter counter;
	counter.as_gint32 = mono_atomic_load_i32 (&threadpool.counters.as_gint32);
	return counter;
}

static inline void
tp_lock (void)
{
	mono_coop_mutex_lock (&threadpool.tp_lock);
}

static inline void
tp_unlock (void)
{
	mono_coop_mutex_unlock (&threadpool.tp_lock);
}

static void
destroy (gpointer unused)
{
	mono_coop_mutex_destroy (&threadpool.tp_lock);
}

static gsize
set_thread_name (MonoInternalThread *thread)
{
	return mono_thread_set_name_constant_ignore_error (thread, "Thread Pool Worker", MonoSetThreadNameFlag_Reset);
}

static void
worker_callback (void);

static void
initialize (void)
{
	g_assert (sizeof (ThreadPoolCounter) == sizeof (gint32));

	mono_refcount_init (&threadpool, destroy);

	mono_coop_mutex_init (&threadpool.tp_lock);

	threadpool.limit_io_min = mono_cpu_count ();
	threadpool.limit_io_max = CLAMP (threadpool.limit_io_min * 100, MIN (threadpool.limit_io_min, 200), MAX (threadpool.limit_io_min, 200));

	mono_coop_cond_init (&threadpool.cleanup_cond);

	mono_threadpool_worker_init (worker_callback);
}

static void
cleanup (void)
{
	mono_threadpool_worker_cleanup ();

	mono_refcount_dec (&threadpool);
}

gboolean
mono_threadpool_enqueue_work_item (MonoDomain *domain, MonoObject *work_item, MonoError *error)
{
	static MonoClass *threadpool_class = NULL;
	static MonoMethod *unsafe_queue_custom_work_item_method = NULL;
	MonoBoolean f;
	gpointer args [2];

	error_init (error);
	g_assert (work_item);

	g_assert (domain == mono_get_root_domain ());

	if (!threadpool_class)
		threadpool_class = mono_class_load_from_name (mono_defaults.corlib, "System.Threading", "ThreadPool");

	if (!unsafe_queue_custom_work_item_method) {
		unsafe_queue_custom_work_item_method = mono_class_get_method_from_name_checked (threadpool_class, "UnsafeQueueCustomWorkItem", 2, 0, error);
		mono_error_assert_ok (error);
	}
	g_assert (unsafe_queue_custom_work_item_method);

	f = FALSE;

	args [0] = (gpointer) work_item;
	args [1] = (gpointer) &f;

	mono_runtime_invoke_checked (unsafe_queue_custom_work_item_method, NULL, args, error);
	return is_ok (error);
}

static MonoObject*
try_invoke_perform_wait_callback (MonoObject** exc, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoObject * const res = mono_runtime_try_invoke (mono_defaults.threadpool_perform_wait_callback_method, NULL, NULL, exc, error);
	HANDLE_FUNCTION_RETURN_VAL (res);
}

static void
worker_callback (void)
{
	ERROR_DECL (error);
	ThreadPoolCounter counter;
	MonoInternalThread *thread;

	if (!mono_refcount_tryinc (&threadpool))
		return;

	thread = mono_thread_internal_current ();

	COUNTER_ATOMIC (counter, {
		if (!(counter._.working < 32767 /* G_MAXINT16 */))
			g_error ("%s: counter._.working = %d, but should be < 32767", __func__, counter._.working);

		counter._.starting --;
		counter._.working ++;
	});

	if (mono_runtime_is_shutting_down ()) {
		COUNTER_ATOMIC (counter, {
			counter._.working --;
		});

		mono_refcount_dec (&threadpool);
		return;
	}

	/*
	 * This is needed so there is always an lmf frame in the runtime invoke call below,
	 * so ThreadAbortExceptions are caught even if the thread is in native code.
	 */
	mono_defaults.threadpool_perform_wait_callback_method->save_lmf = TRUE;

	tp_lock ();

	gsize name_generation = thread->name.generation;
	/* Set the name if this is the first call to worker_callback on this thread */
	if (name_generation == 0)
	   name_generation = set_thread_name (thread);

	while (!mono_runtime_is_shutting_down ()) {
		gboolean retire = FALSE;

		if (thread->state & (ThreadState_AbortRequested | ThreadState_SuspendRequested)) {
			tp_unlock ();
			if (mono_thread_interruption_checkpoint_bool ()) {
				tp_lock ();
				continue;
			}
			tp_lock ();
		}

		if (threadpool.outstanding_request == 0)
			break;

		threadpool.outstanding_request --;
		g_assert (threadpool.outstanding_request >= 0);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker (outstanding requests %d)",
			GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), threadpool.outstanding_request);

		g_assert (threadpool.threadpool_jobs >= 0);
		threadpool.threadpool_jobs ++;

		tp_unlock ();

		// Any thread can set any other thread name at any time.
		// So this is unavoidably racy.
		// This only partly fights against that -- i.e. not atomic and not a loop.
		// It is reliable against the thread setting its own name, and somewhat
		// reliable against other threads setting this thread's name.
		if (name_generation != thread->name.generation)
			name_generation = set_thread_name (thread);

		mono_thread_clear_and_set_state (thread,
			(MonoThreadState)~ThreadState_Background,
			ThreadState_Background);

		MonoObject *exc = NULL, *res;

		res = try_invoke_perform_wait_callback (&exc, error);
		if (exc || !is_ok (error)) {
			if (exc == NULL)
				exc = (MonoObject *) mono_error_convert_to_exception (error);
			else
				mono_error_cleanup (error);
			mono_thread_internal_unhandled_exception (exc);
		} else if (res && *(MonoBoolean*) mono_object_unbox_internal (res) == FALSE) {
			retire = TRUE;
		}

		/* Reset name after every callback */
		if (name_generation != thread->name.generation)
			name_generation = set_thread_name (thread);

		tp_lock ();

		threadpool.threadpool_jobs --;
		g_assert (threadpool.threadpool_jobs >= 0);

		if (retire)
			break;
	}

	tp_unlock ();

	COUNTER_ATOMIC (counter, {
		counter._.working --;
	});

	mono_refcount_dec (&threadpool);
}

void
mono_threadpool_cleanup (void)
{
#ifndef DISABLE_SOCKETS
	mono_threadpool_io_cleanup ();
#endif
	mono_lazy_cleanup (&status, cleanup);
}

MonoAsyncResult *
mono_threadpool_begin_invoke (MonoDomain *domain, MonoObject *target, MonoMethod *method, gpointer *params, MonoError *error)
{
	static MonoClass *async_call_klass = NULL;
	MonoMethodMessage *message;
	MonoAsyncResult *async_result;
	MonoAsyncCall *async_call;
	MonoDelegate *async_callback = NULL;
	MonoObject *state = NULL;

	if (!async_call_klass)
		async_call_klass = mono_class_load_from_name (mono_defaults.corlib, "System", "MonoAsyncCall");

	error_init (error);

	message = mono_method_call_message_new (method, params, mono_get_delegate_invoke_internal (method->klass), (params != NULL) ? (&async_callback) : NULL, (params != NULL) ? (&state) : NULL, error);
	return_val_if_nok (error, NULL);

	async_call = (MonoAsyncCall*) mono_object_new_checked (domain, async_call_klass, error);
	return_val_if_nok (error, NULL);

	MONO_OBJECT_SETREF_INTERNAL (async_call, msg, message);
	MONO_OBJECT_SETREF_INTERNAL (async_call, state, state);

	if (async_callback) {
		MONO_OBJECT_SETREF_INTERNAL (async_call, cb_method, mono_get_delegate_invoke_internal (((MonoObject*) async_callback)->vtable->klass));
		MONO_OBJECT_SETREF_INTERNAL (async_call, cb_target, async_callback);
	}

	async_result = mono_async_result_new (domain, NULL, async_call->state, NULL, (MonoObject*) async_call, error);
	return_val_if_nok (error, NULL);
	MONO_OBJECT_SETREF_INTERNAL (async_result, async_delegate, target);

	mono_threadpool_enqueue_work_item (domain, (MonoObject*) async_result, error);
	return_val_if_nok (error, NULL);

	return async_result;
}

MonoObject *
mono_threadpool_end_invoke (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc, MonoError *error)
{
	MonoAsyncCall *ac;

	error_init (error);
	g_assert (exc);
	g_assert (out_args);

	*exc = NULL;
	*out_args = NULL;

	/* check if already finished */
	mono_monitor_enter_internal ((MonoObject*) ares);

	if (ares->endinvoke_called) {
		mono_error_set_invalid_operation(error, "Delegate EndInvoke method called more than once");
		mono_monitor_exit_internal ((MonoObject*) ares);
		return NULL;
	}

	ares->endinvoke_called = 1;

	/* wait until we are really finished */
	if (ares->completed) {
		mono_monitor_exit_internal ((MonoObject *) ares);
	} else {
		gpointer wait_event;
		if (ares->handle) {
			wait_event = mono_wait_handle_get_handle ((MonoWaitHandle*) ares->handle);
		} else {
			wait_event = mono_w32event_create (TRUE, FALSE);
			g_assert(wait_event);
			MonoWaitHandle *wait_handle = mono_wait_handle_new (mono_object_domain (ares), wait_event, error);
			if (!is_ok (error)) {
				mono_w32event_close (wait_event);
				return NULL;
			}
			MONO_OBJECT_SETREF_INTERNAL (ares, handle, (MonoObject*) wait_handle);
		}
		mono_monitor_exit_internal ((MonoObject*) ares);
		mono_w32handle_wait_one (wait_event, MONO_INFINITE_WAIT, TRUE);
	}

	ac = (MonoAsyncCall*) ares->object_data;
	g_assert (ac);

	*exc = ac->msg->exc; /* FIXME: GC add write barrier */
	*out_args = ac->out_args;
	return ac->res;
}

void
mono_threadpool_suspend (void)
{
	if (mono_lazy_is_initialized (&status))
		mono_threadpool_worker_set_suspended (TRUE);
}

void
mono_threadpool_resume (void)
{
	if (mono_lazy_is_initialized (&status))
		mono_threadpool_worker_set_suspended (FALSE);
}

void
ves_icall_System_Threading_ThreadPool_GetAvailableThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads, MonoError *error)
{
	ThreadPoolCounter counter;

	if (!worker_threads || !completion_port_threads)
		return;

	if (!mono_lazy_initialize (&status, initialize) || !mono_refcount_tryinc (&threadpool)) {
		*worker_threads = 0;
		*completion_port_threads = 0;
		return;
	}

	counter = COUNTER_READ ();

	*worker_threads = MAX (0, mono_threadpool_worker_get_max () - counter._.working);
	*completion_port_threads = threadpool.limit_io_max;

	mono_refcount_dec (&threadpool);
}

void
ves_icall_System_Threading_ThreadPool_GetMinThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads, MonoError *error)
{
	if (!worker_threads || !completion_port_threads)
		return;

	if (!mono_lazy_initialize (&status, initialize) || !mono_refcount_tryinc (&threadpool)) {
		*worker_threads = 0;
		*completion_port_threads = 0;
		return;
	}

	*worker_threads = mono_threadpool_worker_get_min ();
	*completion_port_threads = threadpool.limit_io_min;

	mono_refcount_dec (&threadpool);
}

void
ves_icall_System_Threading_ThreadPool_GetMaxThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads, MonoError *error)
{
	if (!worker_threads || !completion_port_threads)
		return;

	if (!mono_lazy_initialize (&status, initialize) || !mono_refcount_tryinc (&threadpool)) {
		*worker_threads = 0;
		*completion_port_threads = 0;
		return;
	}

	*worker_threads = mono_threadpool_worker_get_max ();
	*completion_port_threads = threadpool.limit_io_max;

	mono_refcount_dec (&threadpool);
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMinThreadsNative (gint32 worker_threads, gint32 completion_port_threads, MonoError *error)
{
	if (!mono_lazy_initialize (&status, initialize) || !mono_refcount_tryinc (&threadpool))
		return FALSE;

	if (completion_port_threads <= 0 || completion_port_threads > threadpool.limit_io_max)
		return FALSE;

	if (!mono_threadpool_worker_set_min (worker_threads)) {
		mono_refcount_dec (&threadpool);
		return FALSE;
	}

	threadpool.limit_io_min = completion_port_threads;

	mono_refcount_dec (&threadpool);
	return TRUE;
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMaxThreadsNative (gint32 worker_threads, gint32 completion_port_threads, MonoError *error)
{
	if (!mono_lazy_initialize (&status, initialize) || !mono_refcount_tryinc (&threadpool))
		return FALSE;

	worker_threads = MIN (worker_threads, MAX_POSSIBLE_THREADS);
	completion_port_threads = MIN (completion_port_threads, MAX_POSSIBLE_THREADS);

	gint cpu_count = mono_cpu_count ();

	if (completion_port_threads < threadpool.limit_io_min || completion_port_threads < cpu_count)
		return FALSE;

	if (!mono_threadpool_worker_set_max (worker_threads)) {
		mono_refcount_dec (&threadpool);
		return FALSE;
	}

	threadpool.limit_io_max = completion_port_threads;

	mono_refcount_dec (&threadpool);
	return TRUE;
}

gint32
ves_icall_System_Threading_ThreadPool_GetThreadCount (MonoError *error)
{
	return mono_threadpool_worker_get_threads_count ();
}

gint64
ves_icall_System_Threading_ThreadPool_GetCompletedWorkItemCount (MonoError *error)
{
	return mono_threadpool_worker_get_completed_threads_count ();
}

void
ves_icall_System_Threading_ThreadPool_InitializeVMTp (MonoBoolean *enable_worker_tracking, MonoError *error)
{
	if (enable_worker_tracking) {
		// TODO implement some kind of switch to have the possibily to use it
		*enable_worker_tracking = FALSE;
	}

	mono_lazy_initialize (&status, initialize);
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_NotifyWorkItemComplete (MonoError *error)
{
	if (mono_runtime_is_shutting_down ())
		return FALSE;

	return mono_threadpool_worker_notify_completed ();
}

void
ves_icall_System_Threading_ThreadPool_NotifyWorkItemProgressNative (MonoError *error)
{
	mono_threadpool_worker_notify_completed ();
}

void
ves_icall_System_Threading_ThreadPool_NotifyWorkItemQueued (MonoError *error)
// FIXME Move to managed.
{
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i64 (&mono_perfcounters->threadpool_workitems);
#endif
}

void
ves_icall_System_Threading_ThreadPool_ReportThreadStatus (MonoBoolean is_working, MonoError *error)
{
	// TODO
	mono_error_set_not_implemented (error, "");
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_RequestWorkerThread (MonoError *error)
{
	ThreadPoolCounter counter;

	if (!mono_lazy_initialize (&status, initialize) || !mono_refcount_tryinc (&threadpool)) {
		/* threadpool has been destroyed, we are shutting down */
		return FALSE;
	}

	tp_lock ();
	threadpool.outstanding_request ++;
	g_assert (threadpool.outstanding_request >= 1);
	tp_unlock ();

	COUNTER_ATOMIC (counter, {
		if (counter._.starting == 16) {
			mono_refcount_dec (&threadpool);
			return TRUE;
		}

		counter._.starting ++;
	});

	mono_threadpool_worker_request ();

	mono_refcount_dec (&threadpool);
	return TRUE;
}

#endif /* ENABLE_NETCORE */
