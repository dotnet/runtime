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

#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef ENABLE_NETCORE

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

typedef struct {
	MonoDomain *domain;
	/* Number of outstanding jobs */
	gint32 outstanding_request;
	/* Number of currently executing jobs */
	gint32 threadpool_jobs;
	/* Signalled when threadpool_jobs + outstanding_request is 0 */
	/* Protected by threadpool.domains_lock */
	MonoCoopCond cleanup_cond;
} ThreadPoolDomain;

typedef union {
	struct {
		gint16 starting; /* starting, but not yet in worker_callback */
		gint16 working; /* executing worker_callback */
	} _;
	gint32 as_gint32;
} ThreadPoolCounter;

typedef struct {
	MonoRefCount ref;

	GPtrArray *domains; // ThreadPoolDomain* []
	MonoCoopMutex domains_lock;

	ThreadPoolCounter counters;

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

static ThreadPoolCounter
COUNTER_READ (void)
{
	ThreadPoolCounter counter;
	counter.as_gint32 = mono_atomic_load_i32 (&threadpool.counters.as_gint32);
	return counter;
}

static void
domains_lock (void)
{
	mono_coop_mutex_lock (&threadpool.domains_lock);
}

static void
domains_unlock (void)
{
	mono_coop_mutex_unlock (&threadpool.domains_lock);
}

static void
destroy (gpointer unused)
{
	g_ptr_array_free (threadpool.domains, TRUE);
	mono_coop_mutex_destroy (&threadpool.domains_lock);
}

static void
worker_callback (void);

static void
initialize (void)
{
	g_assert (sizeof (ThreadPoolCounter) == sizeof (gint32));

	mono_refcount_init (&threadpool, destroy);

	threadpool.domains = g_ptr_array_new ();
	mono_coop_mutex_init (&threadpool.domains_lock);

	threadpool.limit_io_min = mono_cpu_count ();
	threadpool.limit_io_max = CLAMP (threadpool.limit_io_min * 100, MIN (threadpool.limit_io_min, 200), MAX (threadpool.limit_io_min, 200));

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
	MonoDomain *current_domain;
	MonoBoolean f;
	gpointer args [2];

	error_init (error);
	g_assert (work_item);

	MONO_STATIC_POINTER_INIT (MonoClass, threadpool_class)

		threadpool_class = mono_class_load_from_name (mono_defaults.corlib, "System.Threading", "ThreadPool");

	MONO_STATIC_POINTER_INIT_END (MonoClass, threadpool_class)

	MONO_STATIC_POINTER_INIT (MonoMethod, unsafe_queue_custom_work_item_method)

		unsafe_queue_custom_work_item_method = mono_class_get_method_from_name_checked (threadpool_class, "UnsafeQueueCustomWorkItem", 2, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, unsafe_queue_custom_work_item_method)

	g_assert (unsafe_queue_custom_work_item_method);

	f = FALSE;

	args [0] = (gpointer) work_item;
	args [1] = (gpointer) &f;

	current_domain = mono_domain_get ();
	if (current_domain == domain) {
		mono_runtime_invoke_checked (unsafe_queue_custom_work_item_method, NULL, args, error);
	} else {
		mono_thread_push_appdomain_ref (domain);
		if (mono_domain_set_fast (domain, FALSE)) {
			mono_runtime_invoke_checked (unsafe_queue_custom_work_item_method, NULL, args, error);
			mono_domain_set_fast (current_domain, TRUE);
		} else {
			// mono_domain_set_fast failing still leads to success.
		}
		mono_thread_pop_appdomain_ref ();
	}
	return is_ok (error);
}

/* LOCKING: domains_lock must be held. */
static ThreadPoolDomain *
tpdomain_create (MonoDomain *domain)
{
	ThreadPoolDomain *tpdomain;

	tpdomain = g_new0 (ThreadPoolDomain, 1);
	tpdomain->domain = domain;
	mono_coop_cond_init (&tpdomain->cleanup_cond);

	g_ptr_array_add (threadpool.domains, tpdomain);

	return tpdomain;
}

/* LOCKING: domains_lock must be held. */
static gboolean
tpdomain_remove (ThreadPoolDomain *tpdomain)
{
	g_assert (tpdomain);
	return g_ptr_array_remove (threadpool.domains, tpdomain);
}

/* LOCKING: domains_lock must be held */
static ThreadPoolDomain *
tpdomain_get (MonoDomain *domain)
{
	gint i;

	g_assert (domain);

	for (i = 0; i < threadpool.domains->len; ++i) {
		ThreadPoolDomain *tpdomain;

		tpdomain = (ThreadPoolDomain *)g_ptr_array_index (threadpool.domains, i);
		if (tpdomain->domain == domain)
			return tpdomain;
	}

	return NULL;
}

static void
tpdomain_free (ThreadPoolDomain *tpdomain)
{
	g_free (tpdomain);
}

/* LOCKING: domains_lock must be held */
static ThreadPoolDomain *
tpdomain_get_next (ThreadPoolDomain *current)
{
	ThreadPoolDomain *tpdomain = NULL;
	gint len;

	len = threadpool.domains->len;
	if (len > 0) {
		gint i, current_idx = -1;
		if (current) {
			for (i = 0; i < len; ++i) {
				if (current == g_ptr_array_index (threadpool.domains, i)) {
					current_idx = i;
					break;
				}
			}
		}
		for (i = current_idx + 1; i < len + current_idx + 1; ++i) {
			ThreadPoolDomain *tmp = (ThreadPoolDomain *)g_ptr_array_index (threadpool.domains, i % len);
			if (tmp->outstanding_request > 0) {
				tpdomain = tmp;
				break;
			}
		}
	}

	return tpdomain;
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
mono_threadpool_set_thread_name (MonoInternalThread *thread)
{
	mono_thread_set_name_constant_ignore_error (
		thread,
		"Thread Pool Worker",
		MonoSetThreadNameFlag_Reset | MonoSetThreadNameFlag_RepeatedlyButOptimized);
}

static void
worker_callback (void)
{
	ThreadPoolDomain *tpdomain, *previous_tpdomain;
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

	/* Set the name if this is the first call to worker_callback on this thread */
	mono_threadpool_set_thread_name (thread);

	domains_lock ();

	previous_tpdomain = NULL;

	while (!mono_runtime_is_shutting_down ()) {
		gboolean retire = FALSE;

		if (thread->state & (ThreadState_AbortRequested | ThreadState_SuspendRequested)) {
			domains_unlock ();
			if (mono_thread_interruption_checkpoint_bool ()) {
				domains_lock ();
				continue;
			}
			domains_lock ();
		}

		tpdomain = tpdomain_get_next (previous_tpdomain);
		if (!tpdomain)
			break;

		tpdomain->outstanding_request --;
		g_assert (tpdomain->outstanding_request >= 0);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker running in domain %p (outstanding requests %d)",
			GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), tpdomain->domain, tpdomain->outstanding_request);

		g_assert (tpdomain->threadpool_jobs >= 0);
		tpdomain->threadpool_jobs ++;

		domains_unlock ();

		mono_threadpool_set_thread_name (thread);

		mono_thread_clear_and_set_state (thread,
			(MonoThreadState)~ThreadState_Background,
			ThreadState_Background);

		mono_thread_push_appdomain_ref (tpdomain->domain);
		if (mono_domain_set_fast (tpdomain->domain, FALSE)) {
			MonoObject *exc = NULL, *res;

			ERROR_DECL (error);

			res = try_invoke_perform_wait_callback (&exc, error);
			if (exc || !is_ok(error)) {
				if (exc == NULL)
					exc = (MonoObject *) mono_error_convert_to_exception (error);
				else
					mono_error_cleanup (error);
				mono_thread_internal_unhandled_exception (exc);
			} else if (res && *(MonoBoolean*) mono_object_unbox_internal (res) == FALSE) {
				retire = TRUE;
			}

			mono_domain_set_fast (mono_get_root_domain (), TRUE);
		}
		mono_thread_pop_appdomain_ref ();

		/* Reset name after every callback */
		mono_threadpool_set_thread_name (thread);

		domains_lock ();

		tpdomain->threadpool_jobs --;
		g_assert (tpdomain->threadpool_jobs >= 0);

		if (tpdomain->outstanding_request + tpdomain->threadpool_jobs == 0 && mono_domain_is_unloading (tpdomain->domain)) {
			gboolean removed;

			removed = tpdomain_remove (tpdomain);
			g_assert (removed);

			mono_coop_cond_signal (&tpdomain->cleanup_cond);
			tpdomain = NULL;
		}

		if (retire)
			break;

		previous_tpdomain = tpdomain;
	}

	domains_unlock ();

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
	MonoMethodMessage *message;
	MonoAsyncResult *async_result;
	MonoAsyncCall *async_call;
	MonoDelegate *async_callback = NULL;
	MonoObject *state = NULL;

	MONO_STATIC_POINTER_INIT (MonoClass, async_call_klass)

		async_call_klass = mono_class_load_from_name (mono_defaults.corlib, "System", "MonoAsyncCall");

	MONO_STATIC_POINTER_INIT_END (MonoClass, async_call_klass)

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

gboolean
mono_threadpool_remove_domain_jobs (MonoDomain *domain, int timeout)
{
	gint64 end = 0;
	ThreadPoolDomain *tpdomain;
	gboolean ret;

	g_assert (domain);
	g_assert (timeout >= -1);

	g_assert (mono_domain_is_unloading (domain));

	if (timeout != -1)
		end = mono_msec_ticks () + timeout;

#ifndef DISABLE_SOCKETS
	mono_threadpool_io_remove_domain_jobs (domain);
	if (timeout != -1) {
		if (mono_msec_ticks () > end)
			return FALSE;
	}
#endif

	/*
	 * Wait for all threads which execute jobs in the domain to exit.
	 * The is_unloading () check in worker_request () ensures that
	 * no new jobs are added after we enter the lock below.
	 */

	if (!mono_lazy_is_initialized (&status))
		return TRUE;

	mono_refcount_inc (&threadpool);

	domains_lock ();

	tpdomain = tpdomain_get (domain);
	if (!tpdomain) {
		domains_unlock ();
		mono_refcount_dec (&threadpool);
		return TRUE;
	}

	ret = TRUE;

	while (tpdomain->outstanding_request + tpdomain->threadpool_jobs > 0) {
		if (timeout == -1) {
			mono_coop_cond_wait (&tpdomain->cleanup_cond, &threadpool.domains_lock);
		} else {
			gint64 now;
			gint res;

			now = mono_msec_ticks();
			if (now > end) {
				ret = FALSE;
				break;
			}

			res = mono_coop_cond_timedwait (&tpdomain->cleanup_cond, &threadpool.domains_lock, end - now);
			if (res != 0) {
				ret = FALSE;
				break;
			}
		}
	}

	/* Remove from the list the worker threads look at */
	tpdomain_remove (tpdomain);

	domains_unlock ();

	mono_coop_cond_destroy (&tpdomain->cleanup_cond);
	tpdomain_free (tpdomain);

	mono_refcount_dec (&threadpool);

	return ret;
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
	if (mono_domain_is_unloading (mono_domain_get ()) || mono_runtime_is_shutting_down ())
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
	MonoDomain *domain;
	ThreadPoolDomain *tpdomain;
	ThreadPoolCounter counter;

	domain = mono_domain_get ();
	if (mono_domain_is_unloading (domain))
		return FALSE;

	if (!mono_lazy_initialize (&status, initialize) || !mono_refcount_tryinc (&threadpool)) {
		/* threadpool has been destroyed, we are shutting down */
		return FALSE;
	}

	domains_lock ();

	tpdomain = tpdomain_get (domain);
	if (!tpdomain) {
		/* synchronize with mono_threadpool_remove_domain_jobs */
		if (mono_domain_is_unloading (domain)) {
			domains_unlock ();
			mono_refcount_dec (&threadpool);
			return FALSE;
		}

		tpdomain = tpdomain_create (domain);
	}

	g_assert (tpdomain);

	tpdomain->outstanding_request ++;
	g_assert (tpdomain->outstanding_request >= 1);

	domains_unlock ();

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

#endif /* !ENABLE_NETCORE */

MONO_EMPTY_SOURCE_FILE (threadpool);
