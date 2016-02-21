/*
 * threadpool-ms.c: Microsoft threadpool runtime support
 *
 * Author:
 *	Ludovic Henry (ludovic.henry@xamarin.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
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

#include <stdlib.h>
#define _USE_MATH_DEFINES // needed by MSVC to define math constants
#include <math.h>
#include <config.h>
#include <glib.h>

#include <mono/metadata/class-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/threadpool-ms.h>
#include <mono/metadata/threadpool-ms-io.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-complex.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-rand.h>

#define CPU_USAGE_LOW 80
#define CPU_USAGE_HIGH 95

#define MONITOR_INTERVAL 500 // ms
#define MONITOR_MINIMAL_LIFETIME 60 * 1000 // ms

#define WORKER_CREATION_MAX_PER_SEC 10

/* The exponent to apply to the gain. 1.0 means to use linear gain,
 * higher values will enhance large moves and damp small ones.
 * default: 2.0 */
#define HILL_CLIMBING_GAIN_EXPONENT 2.0

/* The 'cost' of a thread. 0 means drive for increased throughput regardless
 * of thread count, higher values bias more against higher thread counts.
 * default: 0.15 */
#define HILL_CLIMBING_BIAS 0.15

#define HILL_CLIMBING_WAVE_PERIOD 4
#define HILL_CLIMBING_MAX_WAVE_MAGNITUDE 20
#define HILL_CLIMBING_WAVE_MAGNITUDE_MULTIPLIER 1.0
#define HILL_CLIMBING_WAVE_HISTORY_SIZE 8
#define HILL_CLIMBING_TARGET_SIGNAL_TO_NOISE_RATIO 3.0
#define HILL_CLIMBING_MAX_CHANGE_PER_SECOND 4
#define HILL_CLIMBING_MAX_CHANGE_PER_SAMPLE 20
#define HILL_CLIMBING_SAMPLE_INTERVAL_LOW 10
#define HILL_CLIMBING_SAMPLE_INTERVAL_HIGH 200
#define HILL_CLIMBING_ERROR_SMOOTHING_FACTOR 0.01
#define HILL_CLIMBING_MAX_SAMPLE_ERROR_PERCENT 0.15

typedef union {
	struct {
		gint16 max_working; /* determined by heuristic */
		gint16 active; /* executing worker_thread */
		gint16 working; /* actively executing worker_thread, not parked */
		gint16 parked; /* parked */
	} _;
	gint64 as_gint64;
} ThreadPoolCounter;

typedef struct {
	MonoDomain *domain;
	gint32 outstanding_request;
} ThreadPoolDomain;

typedef MonoInternalThread ThreadPoolWorkingThread;

typedef struct {
	gint32 wave_period;
	gint32 samples_to_measure;
	gdouble target_throughput_ratio;
	gdouble target_signal_to_noise_ratio;
	gdouble max_change_per_second;
	gdouble max_change_per_sample;
	gint32 max_thread_wave_magnitude;
	gint32 sample_interval_low;
	gdouble thread_magnitude_multiplier;
	gint32 sample_interval_high;
	gdouble throughput_error_smoothing_factor;
	gdouble gain_exponent;
	gdouble max_sample_error;

	gdouble current_control_setting;
	gint64 total_samples;
	gint16 last_thread_count;
	gdouble elapsed_since_last_change;
	gdouble completions_since_last_change;

	gdouble average_throughput_noise;

	gdouble *samples;
	gdouble *thread_counts;

	guint32 current_sample_interval;
	gpointer random_interval_generator;

	gint32 accumulated_completion_count;
	gdouble accumulated_sample_duration;
} ThreadPoolHillClimbing;

typedef struct {
	ThreadPoolCounter counters;

	GPtrArray *domains; // ThreadPoolDomain* []
	MonoCoopMutex domains_lock;

	GPtrArray *working_threads; // ThreadPoolWorkingThread* []
	gint32 parked_threads_count;
	MonoCoopCond parked_threads_cond;
	MonoCoopMutex active_threads_lock; /* protect access to working_threads and parked_threads */

	guint32 worker_creation_current_second;
	guint32 worker_creation_current_count;
	MonoCoopMutex worker_creation_lock;

	gint32 heuristic_completions;
	guint32 heuristic_sample_start;
	guint32 heuristic_last_dequeue; // ms
	guint32 heuristic_last_adjustment; // ms
	guint32 heuristic_adjustment_interval; // ms
	ThreadPoolHillClimbing heuristic_hill_climbing;
	MonoCoopMutex heuristic_lock;

	gint32 limit_worker_min;
	gint32 limit_worker_max;
	gint32 limit_io_min;
	gint32 limit_io_max;

	MonoCpuUsageState *cpu_usage_state;
	gint32 cpu_usage;

	/* suspended by the debugger */
	gboolean suspended;
} ThreadPool;

typedef enum {
	TRANSITION_WARMUP,
	TRANSITION_INITIALIZING,
	TRANSITION_RANDOM_MOVE,
	TRANSITION_CLIMBING_MOVE,
	TRANSITION_CHANGE_POINT,
	TRANSITION_STABILIZING,
	TRANSITION_STARVATION,
	TRANSITION_THREAD_TIMED_OUT,
	TRANSITION_UNDEFINED,
} ThreadPoolHeuristicStateTransition;

static mono_lazy_init_t status = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

enum {
	MONITOR_STATUS_REQUESTED,
	MONITOR_STATUS_WAITING_FOR_REQUEST,
	MONITOR_STATUS_NOT_RUNNING,
};

static gint32 monitor_status = MONITOR_STATUS_NOT_RUNNING;

static ThreadPool* threadpool;

#define COUNTER_CHECK(counter) \
	do { \
		g_assert (counter._.max_working > 0); \
		g_assert (counter._.working >= 0); \
		g_assert (counter._.active >= 0); \
	} while (0)

#define COUNTER_READ() (InterlockedRead64 (&threadpool->counters.as_gint64))

#define COUNTER_ATOMIC(var,block) \
	do { \
		ThreadPoolCounter __old; \
		do { \
			g_assert (threadpool); \
			__old.as_gint64 = COUNTER_READ (); \
			(var) = __old; \
			{ block; } \
			COUNTER_CHECK (var); \
		} while (InterlockedCompareExchange64 (&threadpool->counters.as_gint64, (var).as_gint64, __old.as_gint64) != __old.as_gint64); \
	} while (0)

#define COUNTER_TRY_ATOMIC(res,var,block) \
	do { \
		ThreadPoolCounter __old; \
		do { \
			g_assert (threadpool); \
			__old.as_gint64 = COUNTER_READ (); \
			(var) = __old; \
			(res) = FALSE; \
			{ block; } \
			COUNTER_CHECK (var); \
			(res) = InterlockedCompareExchange64 (&threadpool->counters.as_gint64, (var).as_gint64, __old.as_gint64) == __old.as_gint64; \
		} while (0); \
	} while (0)

static gpointer
rand_create (void)
{
	mono_rand_open ();
	return mono_rand_init (NULL, 0);
}

static guint32
rand_next (gpointer *handle, guint32 min, guint32 max)
{
	guint32 val;
	if (!mono_rand_try_get_uint32 (handle, &val, min, max)) {
		// FIXME handle error
		g_assert_not_reached ();
	}
	return val;
}

static void
rand_free (gpointer handle)
{
	mono_rand_close (handle);
}

static void
initialize (void)
{
	ThreadPoolHillClimbing *hc;
	const char *threads_per_cpu_env;
	gint threads_per_cpu;
	gint threads_count;

	g_assert (!threadpool);
	threadpool = g_new0 (ThreadPool, 1);
	g_assert (threadpool);

	threadpool->domains = g_ptr_array_new ();
	mono_coop_mutex_init (&threadpool->domains_lock);

	threadpool->parked_threads_count = 0;
	mono_coop_cond_init (&threadpool->parked_threads_cond);
	threadpool->working_threads = g_ptr_array_new ();
	mono_coop_mutex_init (&threadpool->active_threads_lock);

	threadpool->worker_creation_current_second = -1;
	mono_coop_mutex_init (&threadpool->worker_creation_lock);

	threadpool->heuristic_adjustment_interval = 10;
	mono_coop_mutex_init (&threadpool->heuristic_lock);

	mono_rand_open ();

	hc = &threadpool->heuristic_hill_climbing;

	hc->wave_period = HILL_CLIMBING_WAVE_PERIOD;
	hc->max_thread_wave_magnitude = HILL_CLIMBING_MAX_WAVE_MAGNITUDE;
	hc->thread_magnitude_multiplier = (gdouble) HILL_CLIMBING_WAVE_MAGNITUDE_MULTIPLIER;
	hc->samples_to_measure = hc->wave_period * HILL_CLIMBING_WAVE_HISTORY_SIZE;
	hc->target_throughput_ratio = (gdouble) HILL_CLIMBING_BIAS;
	hc->target_signal_to_noise_ratio = (gdouble) HILL_CLIMBING_TARGET_SIGNAL_TO_NOISE_RATIO;
	hc->max_change_per_second = (gdouble) HILL_CLIMBING_MAX_CHANGE_PER_SECOND;
	hc->max_change_per_sample = (gdouble) HILL_CLIMBING_MAX_CHANGE_PER_SAMPLE;
	hc->sample_interval_low = HILL_CLIMBING_SAMPLE_INTERVAL_LOW;
	hc->sample_interval_high = HILL_CLIMBING_SAMPLE_INTERVAL_HIGH;
	hc->throughput_error_smoothing_factor = (gdouble) HILL_CLIMBING_ERROR_SMOOTHING_FACTOR;
	hc->gain_exponent = (gdouble) HILL_CLIMBING_GAIN_EXPONENT;
	hc->max_sample_error = (gdouble) HILL_CLIMBING_MAX_SAMPLE_ERROR_PERCENT;
	hc->current_control_setting = 0;
	hc->total_samples = 0;
	hc->last_thread_count = 0;
	hc->average_throughput_noise = 0;
	hc->elapsed_since_last_change = 0;
	hc->accumulated_completion_count = 0;
	hc->accumulated_sample_duration = 0;
	hc->samples = g_new0 (gdouble, hc->samples_to_measure);
	hc->thread_counts = g_new0 (gdouble, hc->samples_to_measure);
	hc->random_interval_generator = rand_create ();
	hc->current_sample_interval = rand_next (&hc->random_interval_generator, hc->sample_interval_low, hc->sample_interval_high);

	if (!(threads_per_cpu_env = g_getenv ("MONO_THREADS_PER_CPU")))
		threads_per_cpu = 1;
	else
		threads_per_cpu = CLAMP (atoi (threads_per_cpu_env), 1, 50);

	threads_count = mono_cpu_count () * threads_per_cpu;

	threadpool->limit_worker_min = threadpool->limit_io_min = threads_count;

#if defined (PLATFORM_ANDROID) || defined (HOST_IOS)
	threadpool->limit_worker_max = threadpool->limit_io_max = CLAMP (threads_count * 100, MIN (threads_count, 200), MAX (threads_count, 200));
#else
	threadpool->limit_worker_max = threadpool->limit_io_max = threads_count * 100;
#endif

	threadpool->counters._.max_working = threadpool->limit_worker_min;

	threadpool->cpu_usage_state = g_new0 (MonoCpuUsageState, 1);

	threadpool->suspended = FALSE;
}

static void worker_kill (ThreadPoolWorkingThread *thread);

static void
cleanup (void)
{
	guint i;

	/* we make the assumption along the code that we are
	 * cleaning up only if the runtime is shutting down */
	g_assert (mono_runtime_is_shutting_down ());

	while (monitor_status != MONITOR_STATUS_NOT_RUNNING)
		mono_thread_info_sleep (1, NULL);

	mono_coop_mutex_lock (&threadpool->active_threads_lock);

	/* stop all threadpool->working_threads */
	for (i = 0; i < threadpool->working_threads->len; ++i)
		worker_kill ((ThreadPoolWorkingThread*) g_ptr_array_index (threadpool->working_threads, i));

	/* unpark all threadpool->parked_threads */
	mono_coop_cond_broadcast (&threadpool->parked_threads_cond);

	mono_coop_mutex_unlock (&threadpool->active_threads_lock);
}

void
mono_threadpool_ms_enqueue_work_item (MonoDomain *domain, MonoObject *work_item)
{
	static MonoClass *threadpool_class = NULL;
	static MonoMethod *unsafe_queue_custom_work_item_method = NULL;
	MonoError error;
	MonoDomain *current_domain;
	MonoBoolean f;
	gpointer args [2];

	g_assert (work_item);

	if (!threadpool_class)
		threadpool_class = mono_class_load_from_name (mono_defaults.corlib, "System.Threading", "ThreadPool");

	if (!unsafe_queue_custom_work_item_method)
		unsafe_queue_custom_work_item_method = mono_class_get_method_from_name (threadpool_class, "UnsafeQueueCustomWorkItem", 2);
	g_assert (unsafe_queue_custom_work_item_method);

	f = FALSE;

	args [0] = (gpointer) work_item;
	args [1] = (gpointer) &f;

	current_domain = mono_domain_get ();
	if (current_domain == domain) {
		mono_runtime_invoke_checked (unsafe_queue_custom_work_item_method, NULL, args, &error);
		mono_error_raise_exception (&error); /* FIXME don't raise here */
	} else {
		mono_thread_push_appdomain_ref (domain);
		if (mono_domain_set (domain, FALSE)) {
			mono_runtime_invoke_checked (unsafe_queue_custom_work_item_method, NULL, args, &error);
			mono_error_raise_exception (&error); /* FIXME don't raise here */
			mono_domain_set (current_domain, TRUE);
		}
		mono_thread_pop_appdomain_ref ();
	}
}

/* LOCKING: threadpool->domains_lock must be held */
static void
domain_add (ThreadPoolDomain *tpdomain)
{
	guint i, len;

	g_assert (tpdomain);

	len = threadpool->domains->len;
	for (i = 0; i < len; ++i) {
		if (g_ptr_array_index (threadpool->domains, i) == tpdomain)
			break;
	}

	if (i == len)
		g_ptr_array_add (threadpool->domains, tpdomain);
}

/* LOCKING: threadpool->domains_lock must be held */
static gboolean
domain_remove (ThreadPoolDomain *tpdomain)
{
	g_assert (tpdomain);
	return g_ptr_array_remove (threadpool->domains, tpdomain);
}

/* LOCKING: threadpool->domains_lock must be held */
static ThreadPoolDomain *
domain_get (MonoDomain *domain, gboolean create)
{
	ThreadPoolDomain *tpdomain = NULL;
	guint i;

	g_assert (domain);

	for (i = 0; i < threadpool->domains->len; ++i) {
		tpdomain = (ThreadPoolDomain *)g_ptr_array_index (threadpool->domains, i);
		if (tpdomain->domain == domain)
			return tpdomain;
	}

	if (create) {
		tpdomain = g_new0 (ThreadPoolDomain, 1);
		tpdomain->domain = domain;
		domain_add (tpdomain);
	}

	return tpdomain;
}

static void
domain_free (ThreadPoolDomain *tpdomain)
{
	g_free (tpdomain);
}

/* LOCKING: threadpool->domains_lock must be held */
static gboolean
domain_any_has_request (void)
{
	guint i;

	for (i = 0; i < threadpool->domains->len; ++i) {
		ThreadPoolDomain *tmp = (ThreadPoolDomain *)g_ptr_array_index (threadpool->domains, i);
		if (tmp->outstanding_request > 0)
			return TRUE;
	}

	return FALSE;
}

/* LOCKING: threadpool->domains_lock must be held */
static ThreadPoolDomain *
domain_get_next (ThreadPoolDomain *current)
{
	ThreadPoolDomain *tpdomain = NULL;
	guint len;

	len = threadpool->domains->len;
	if (len > 0) {
		guint i, current_idx = -1;
		if (current) {
			for (i = 0; i < len; ++i) {
				if (current == g_ptr_array_index (threadpool->domains, i)) {
					current_idx = i;
					break;
				}
			}
			g_assert (current_idx >= 0);
		}
		for (i = current_idx + 1; i < len + current_idx + 1; ++i) {
			ThreadPoolDomain *tmp = (ThreadPoolDomain *)g_ptr_array_index (threadpool->domains, i % len);
			if (tmp->outstanding_request > 0) {
				tpdomain = tmp;
				break;
			}
		}
	}

	return tpdomain;
}

static void
worker_wait_interrupt (gpointer data)
{
	mono_coop_mutex_lock (&threadpool->active_threads_lock);
	mono_coop_cond_signal (&threadpool->parked_threads_cond);
	mono_coop_mutex_unlock (&threadpool->active_threads_lock);
}

/* return TRUE if timeout, FALSE otherwise (worker unpark or interrupt) */
static gboolean
worker_park (void)
{
	gboolean timeout = FALSE;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] current worker parking", mono_native_thread_id_get ());

	mono_gc_set_skip_thread (TRUE);

	mono_coop_mutex_lock (&threadpool->active_threads_lock);

	if (!mono_runtime_is_shutting_down ()) {
		static gpointer rand_handle = NULL;
		MonoInternalThread *thread_internal;
		gboolean interrupted = FALSE;

		if (!rand_handle)
			rand_handle = rand_create ();
		g_assert (rand_handle);

		thread_internal = mono_thread_internal_current ();
		g_assert (thread_internal);

		threadpool->parked_threads_count += 1;
		g_ptr_array_remove_fast (threadpool->working_threads, thread_internal);

		mono_thread_info_install_interrupt (worker_wait_interrupt, NULL, &interrupted);
		if (interrupted)
			goto done;

		if (mono_coop_cond_timedwait (&threadpool->parked_threads_cond, &threadpool->active_threads_lock, rand_next ((void **)rand_handle, 5 * 1000, 60 * 1000)) != 0)
			timeout = TRUE;

		mono_thread_info_uninstall_interrupt (&interrupted);

done:
		g_ptr_array_add (threadpool->working_threads, thread_internal);
		threadpool->parked_threads_count -= 1;
	}

	mono_coop_mutex_unlock (&threadpool->active_threads_lock);

	mono_gc_set_skip_thread (FALSE);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] current worker unparking, timeout? %s", mono_native_thread_id_get (), timeout ? "yes" : "no");

	return timeout;
}

static gboolean
worker_try_unpark (void)
{
	gboolean res = FALSE;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try unpark worker", mono_native_thread_id_get ());

	mono_coop_mutex_lock (&threadpool->active_threads_lock);
	if (threadpool->parked_threads_count > 0) {
		mono_coop_cond_signal (&threadpool->parked_threads_cond);
		res = TRUE;
	}
	mono_coop_mutex_unlock (&threadpool->active_threads_lock);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try unpark worker, success? %s", mono_native_thread_id_get (), res ? "yes" : "no");

	return res;
}

static void
worker_kill (ThreadPoolWorkingThread *thread)
{
	if (thread == mono_thread_internal_current ())
		return;

	mono_thread_internal_stop ((MonoInternalThread*) thread);
}

static void
worker_thread (gpointer data)
{
	MonoError error;
	MonoInternalThread *thread;
	ThreadPoolDomain *tpdomain, *previous_tpdomain;
	ThreadPoolCounter counter;
	gboolean retire = FALSE;

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_THREADPOOL, "[%p] worker starting", mono_native_thread_id_get ());

	g_assert (threadpool);

	thread = mono_thread_internal_current ();
	g_assert (thread);

	mono_thread_set_name_internal (thread, mono_string_new (mono_domain_get (), "Threadpool worker"), FALSE);

	mono_coop_mutex_lock (&threadpool->active_threads_lock);
	g_ptr_array_add (threadpool->working_threads, thread);
	mono_coop_mutex_unlock (&threadpool->active_threads_lock);

	previous_tpdomain = NULL;

	mono_coop_mutex_lock (&threadpool->domains_lock);

	while (!mono_runtime_is_shutting_down ()) {
		tpdomain = NULL;

		if ((thread->state & (ThreadState_StopRequested | ThreadState_SuspendRequested)) != 0) {
			mono_coop_mutex_unlock (&threadpool->domains_lock);
			mono_thread_interruption_checkpoint ();
			mono_coop_mutex_lock (&threadpool->domains_lock);
		}

		if (retire || !(tpdomain = domain_get_next (previous_tpdomain))) {
			gboolean timeout;

			COUNTER_ATOMIC (counter, {
				counter._.working --;
				counter._.parked ++;
			});

			mono_coop_mutex_unlock (&threadpool->domains_lock);
			timeout = worker_park ();
			mono_coop_mutex_lock (&threadpool->domains_lock);

			COUNTER_ATOMIC (counter, {
				counter._.working ++;
				counter._.parked --;
			});

			if (timeout)
				break;

			if (retire)
				retire = FALSE;

			continue;
		}

		tpdomain->outstanding_request --;
		g_assert (tpdomain->outstanding_request >= 0);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker running in domain %p",
			mono_native_thread_id_get (), tpdomain->domain, tpdomain->outstanding_request);

		g_assert (tpdomain->domain);
		g_assert (tpdomain->domain->threadpool_jobs >= 0);
		tpdomain->domain->threadpool_jobs ++;

		mono_coop_mutex_unlock (&threadpool->domains_lock);

		mono_thread_push_appdomain_ref (tpdomain->domain);
		if (mono_domain_set (tpdomain->domain, FALSE)) {
			MonoObject *exc = NULL, *res;

			res = mono_runtime_try_invoke (mono_defaults.threadpool_perform_wait_callback_method, NULL, NULL, &exc, &error);
			if (exc || !mono_error_ok(&error)) {
				if (exc == NULL)
					exc = (MonoObject *) mono_error_convert_to_exception (&error);
				else
					mono_error_cleanup (&error);
				mono_thread_internal_unhandled_exception (exc);
			} else if (res && *(MonoBoolean*) mono_object_unbox (res) == FALSE)
				retire = TRUE;

			mono_thread_clr_state (thread, (MonoThreadState)~ThreadState_Background);
			if (!mono_thread_test_state (thread , ThreadState_Background))
				ves_icall_System_Threading_Thread_SetState (thread, ThreadState_Background);

			mono_domain_set (mono_get_root_domain (), TRUE);
		}
		mono_thread_pop_appdomain_ref ();

		mono_coop_mutex_lock (&threadpool->domains_lock);

		tpdomain->domain->threadpool_jobs --;
		g_assert (tpdomain->domain->threadpool_jobs >= 0);

		if (tpdomain->domain->threadpool_jobs == 0 && mono_domain_is_unloading (tpdomain->domain)) {
			gboolean removed = domain_remove (tpdomain);
			g_assert (removed);
			if (tpdomain->domain->cleanup_semaphore)
				ReleaseSemaphore (tpdomain->domain->cleanup_semaphore, 1, NULL);
			domain_free (tpdomain);
			tpdomain = NULL;
		}

		previous_tpdomain = tpdomain;
	}

	mono_coop_mutex_unlock (&threadpool->domains_lock);

	mono_coop_mutex_lock (&threadpool->active_threads_lock);
	g_ptr_array_remove_fast (threadpool->working_threads, thread);
	mono_coop_mutex_unlock (&threadpool->active_threads_lock);

	COUNTER_ATOMIC (counter, {
		counter._.working--;
		counter._.active --;
	});

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_THREADPOOL, "[%p] worker finishing", mono_native_thread_id_get ());
}

static gboolean
worker_try_create (void)
{
	ThreadPoolCounter counter;
	MonoInternalThread *thread;
	gint32 now;

	mono_coop_mutex_lock (&threadpool->worker_creation_lock);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker", mono_native_thread_id_get ());

	if ((now = mono_100ns_ticks () / 10 / 1000 / 1000) == 0) {
		g_warning ("failed to get 100ns ticks");
	} else {
		if (threadpool->worker_creation_current_second != now) {
			threadpool->worker_creation_current_second = now;
			threadpool->worker_creation_current_count = 0;
		} else {
			g_assert (threadpool->worker_creation_current_count <= WORKER_CREATION_MAX_PER_SEC);
			if (threadpool->worker_creation_current_count == WORKER_CREATION_MAX_PER_SEC) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, failed: maximum number of worker created per second reached, current count = %d",
					mono_native_thread_id_get (), threadpool->worker_creation_current_count);
				mono_coop_mutex_unlock (&threadpool->worker_creation_lock);
				return FALSE;
			}
		}
	}

	COUNTER_ATOMIC (counter, {
		if (counter._.working >= counter._.max_working) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, failed: maximum number of working threads reached",
				mono_native_thread_id_get ());
			mono_coop_mutex_unlock (&threadpool->worker_creation_lock);
			return FALSE;
		}
		counter._.working ++;
		counter._.active ++;
	});

	if ((thread = mono_thread_create_internal (mono_get_root_domain (), worker_thread, NULL, TRUE, 0)) != NULL) {
		threadpool->worker_creation_current_count += 1;

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, created %p, now = %d count = %d", mono_native_thread_id_get (), thread->tid, now, threadpool->worker_creation_current_count);
		mono_coop_mutex_unlock (&threadpool->worker_creation_lock);
		return TRUE;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, failed: could not create thread", mono_native_thread_id_get ());

	COUNTER_ATOMIC (counter, {
		counter._.working --;
		counter._.active --;
	});

	mono_coop_mutex_unlock (&threadpool->worker_creation_lock);
	return FALSE;
}

static void monitor_ensure_running (void);

static gboolean
worker_request (MonoDomain *domain)
{
	ThreadPoolDomain *tpdomain;

	g_assert (domain);
	g_assert (threadpool);

	if (mono_runtime_is_shutting_down ())
		return FALSE;

	mono_coop_mutex_lock (&threadpool->domains_lock);

	/* synchronize check with worker_thread */
	if (mono_domain_is_unloading (domain)) {
		mono_coop_mutex_unlock (&threadpool->domains_lock);
		return FALSE;
	}

	tpdomain = domain_get (domain, TRUE);
	g_assert (tpdomain);
	tpdomain->outstanding_request ++;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] request worker, domain = %p, outstanding_request = %d",
		mono_native_thread_id_get (), tpdomain->domain, tpdomain->outstanding_request);

	mono_coop_mutex_unlock (&threadpool->domains_lock);

	if (threadpool->suspended)
		return FALSE;

	monitor_ensure_running ();

	if (worker_try_unpark ()) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] request worker, unparked", mono_native_thread_id_get ());
		return TRUE;
	}

	if (worker_try_create ()) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] request worker, created", mono_native_thread_id_get ());
		return TRUE;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] request worker, failed", mono_native_thread_id_get ());
	return FALSE;
}

static gboolean
monitor_should_keep_running (void)
{
	static gint64 last_should_keep_running = -1;

	g_assert (monitor_status == MONITOR_STATUS_WAITING_FOR_REQUEST || monitor_status == MONITOR_STATUS_REQUESTED);

	if (InterlockedExchange (&monitor_status, MONITOR_STATUS_WAITING_FOR_REQUEST) == MONITOR_STATUS_WAITING_FOR_REQUEST) {
		gboolean should_keep_running = TRUE, force_should_keep_running = FALSE;

		if (mono_runtime_is_shutting_down ()) {
			should_keep_running = FALSE;
		} else {
			mono_coop_mutex_lock (&threadpool->domains_lock);
			if (!domain_any_has_request ())
				should_keep_running = FALSE;
			mono_coop_mutex_unlock (&threadpool->domains_lock);

			if (!should_keep_running) {
				if (last_should_keep_running == -1 || mono_100ns_ticks () - last_should_keep_running < MONITOR_MINIMAL_LIFETIME * 1000 * 10) {
					should_keep_running = force_should_keep_running = TRUE;
				}
			}
		}

		if (should_keep_running) {
			if (last_should_keep_running == -1 || !force_should_keep_running)
				last_should_keep_running = mono_100ns_ticks ();
		} else {
			last_should_keep_running = -1;
			if (InterlockedCompareExchange (&monitor_status, MONITOR_STATUS_NOT_RUNNING, MONITOR_STATUS_WAITING_FOR_REQUEST) == MONITOR_STATUS_WAITING_FOR_REQUEST)
				return FALSE;
		}
	}

	g_assert (monitor_status == MONITOR_STATUS_WAITING_FOR_REQUEST || monitor_status == MONITOR_STATUS_REQUESTED);

	return TRUE;
}

static gboolean
monitor_sufficient_delay_since_last_dequeue (void)
{
	guint32 threshold;

	g_assert (threadpool);

	if (threadpool->cpu_usage < CPU_USAGE_LOW) {
		threshold = MONITOR_INTERVAL;
	} else {
		ThreadPoolCounter counter;
		counter.as_gint64 = COUNTER_READ();
		threshold = counter._.max_working * MONITOR_INTERVAL * 2;
	}

	return mono_msec_ticks () >= threadpool->heuristic_last_dequeue + threshold;
}

static void hill_climbing_force_change (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition);

static void
monitor_thread (void)
{
	MonoInternalThread *current_thread = mono_thread_internal_current ();
	guint i;

	mono_cpu_usage (threadpool->cpu_usage_state);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, started", mono_native_thread_id_get ());

	do {
		ThreadPoolCounter counter;
		gboolean limit_worker_max_reached;
		gint32 interval_left = MONITOR_INTERVAL;
		gint32 awake = 0; /* number of spurious awakes we tolerate before doing a round of rebalancing */

		g_assert (monitor_status != MONITOR_STATUS_NOT_RUNNING);

		mono_gc_set_skip_thread (TRUE);

		do {
			guint32 ts;
			gboolean alerted = FALSE;

			if (mono_runtime_is_shutting_down ())
				break;

			ts = mono_msec_ticks ();
			if (mono_thread_info_sleep (interval_left, &alerted) == 0)
				break;
			interval_left -= mono_msec_ticks () - ts;

			mono_gc_set_skip_thread (FALSE);
			if ((current_thread->state & (ThreadState_StopRequested | ThreadState_SuspendRequested)) != 0)
				mono_thread_interruption_checkpoint ();
			mono_gc_set_skip_thread (TRUE);
		} while (interval_left > 0 && ++awake < 10);

		mono_gc_set_skip_thread (FALSE);

		if (threadpool->suspended)
			continue;

		if (mono_runtime_is_shutting_down ())
			continue;

		mono_coop_mutex_lock (&threadpool->domains_lock);
		if (!domain_any_has_request ()) {
			mono_coop_mutex_unlock (&threadpool->domains_lock);
			continue;
		}
		mono_coop_mutex_unlock (&threadpool->domains_lock);

		threadpool->cpu_usage = mono_cpu_usage (threadpool->cpu_usage_state);

		if (!monitor_sufficient_delay_since_last_dequeue ())
			continue;

		limit_worker_max_reached = FALSE;

		COUNTER_ATOMIC (counter, {
			if (counter._.max_working >= threadpool->limit_worker_max) {
				limit_worker_max_reached = TRUE;
				break;
			}
			counter._.max_working ++;
		});

		if (limit_worker_max_reached)
			continue;

		hill_climbing_force_change (counter._.max_working, TRANSITION_STARVATION);

		for (i = 0; i < 5; ++i) {
			if (mono_runtime_is_shutting_down ())
				break;

			if (worker_try_unpark ()) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, unparked", mono_native_thread_id_get ());
				break;
			}

			if (worker_try_create ()) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, created", mono_native_thread_id_get ());
				break;
			}
		}
	} while (monitor_should_keep_running ());

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, finished", mono_native_thread_id_get ());
}

static void
monitor_ensure_running (void)
{
	for (;;) {
		switch (monitor_status) {
		case MONITOR_STATUS_REQUESTED:
			return;
		case MONITOR_STATUS_WAITING_FOR_REQUEST:
			InterlockedCompareExchange (&monitor_status, MONITOR_STATUS_REQUESTED, MONITOR_STATUS_WAITING_FOR_REQUEST);
			break;
		case MONITOR_STATUS_NOT_RUNNING:
			if (mono_runtime_is_shutting_down ())
				return;
			if (InterlockedCompareExchange (&monitor_status, MONITOR_STATUS_REQUESTED, MONITOR_STATUS_NOT_RUNNING) == MONITOR_STATUS_NOT_RUNNING) {
				if (!mono_thread_create_internal (mono_get_root_domain (), monitor_thread, NULL, TRUE, SMALL_STACK))
					monitor_status = MONITOR_STATUS_NOT_RUNNING;
				return;
			}
			break;
		default: g_assert_not_reached ();
		}
	}
}

static void
hill_climbing_change_thread_count (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition)
{
	ThreadPoolHillClimbing *hc;

	g_assert (threadpool);

	hc = &threadpool->heuristic_hill_climbing;

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_THREADPOOL, "[%p] hill climbing, change max number of threads %d", mono_native_thread_id_get (), new_thread_count);

	hc->last_thread_count = new_thread_count;
	hc->current_sample_interval = rand_next (&hc->random_interval_generator, hc->sample_interval_low, hc->sample_interval_high);
	hc->elapsed_since_last_change = 0;
	hc->completions_since_last_change = 0;
}

static void
hill_climbing_force_change (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition)
{
	ThreadPoolHillClimbing *hc;

	g_assert (threadpool);

	hc = &threadpool->heuristic_hill_climbing;

	if (new_thread_count != hc->last_thread_count) {
		hc->current_control_setting += new_thread_count - hc->last_thread_count;
		hill_climbing_change_thread_count (new_thread_count, transition);
	}
}

static double_complex
hill_climbing_get_wave_component (gdouble *samples, guint sample_count, gdouble period)
{
	ThreadPoolHillClimbing *hc;
	gdouble w, cosine, sine, coeff, q0, q1, q2;
	guint i;

	g_assert (threadpool);
	g_assert (sample_count >= period);
	g_assert (period >= 2);

	hc = &threadpool->heuristic_hill_climbing;

	w = 2.0 * M_PI / period;
	cosine = cos (w);
	sine = sin (w);
	coeff = 2.0 * cosine;
	q0 = q1 = q2 = 0;

	for (i = 0; i < sample_count; ++i) {
		q0 = coeff * q1 - q2 + samples [(hc->total_samples - sample_count + i) % hc->samples_to_measure];
		q2 = q1;
		q1 = q0;
	}

	return mono_double_complex_scalar_div (mono_double_complex_make (q1 - q2 * cosine, (q2 * sine)), ((gdouble)sample_count));
}

static gint16
hill_climbing_update (gint16 current_thread_count, guint32 sample_duration, gint32 completions, guint32 *adjustment_interval)
{
	ThreadPoolHillClimbing *hc;
	ThreadPoolHeuristicStateTransition transition;
	gdouble throughput;
	gdouble throughput_error_estimate;
	gdouble confidence;
	gdouble move;
	gdouble gain;
	gint sample_index;
	gint sample_count;
	gint new_thread_wave_magnitude;
	gint new_thread_count;
	double_complex thread_wave_component;
	double_complex throughput_wave_component;
	double_complex ratio;

	g_assert (threadpool);
	g_assert (adjustment_interval);

	hc = &threadpool->heuristic_hill_climbing;

	/* If someone changed the thread count without telling us, update our records accordingly. */
	if (current_thread_count != hc->last_thread_count)
		hill_climbing_force_change (current_thread_count, TRANSITION_INITIALIZING);

	/* Update the cumulative stats for this thread count */
	hc->elapsed_since_last_change += sample_duration;
	hc->completions_since_last_change += completions;

	/* Add in any data we've already collected about this sample */
	sample_duration += hc->accumulated_sample_duration;
	completions += hc->accumulated_completion_count;

	/* We need to make sure we're collecting reasonably accurate data. Since we're just counting the end
	 * of each work item, we are goinng to be missing some data about what really happened during the
	 * sample interval. The count produced by each thread includes an initial work item that may have
	 * started well before the start of the interval, and each thread may have been running some new
	 * work item for some time before the end of the interval, which did not yet get counted. So
	 * our count is going to be off by +/- threadCount workitems.
	 *
	 * The exception is that the thread that reported to us last time definitely wasn't running any work
	 * at that time, and the thread that's reporting now definitely isn't running a work item now. So
	 * we really only need to consider threadCount-1 threads.
	 *
	 * Thus the percent error in our count is +/- (threadCount-1)/numCompletions.
	 *
	 * We cannot rely on the frequency-domain analysis we'll be doing later to filter out this error, because
	 * of the way it accumulates over time. If this sample is off by, say, 33% in the negative direction,
	 * then the next one likely will be too. The one after that will include the sum of the completions
	 * we missed in the previous samples, and so will be 33% positive. So every three samples we'll have
	 * two "low" samples and one "high" sample. This will appear as periodic variation right in the frequency
	 * range we're targeting, which will not be filtered by the frequency-domain translation. */
	if (hc->total_samples > 0 && ((current_thread_count - 1.0) / completions) >= hc->max_sample_error) {
		/* Not accurate enough yet. Let's accumulate the data so
		 * far, and tell the ThreadPool to collect a little more. */
		hc->accumulated_sample_duration = sample_duration;
		hc->accumulated_completion_count = completions;
		*adjustment_interval = 10;
		return current_thread_count;
	}

	/* We've got enouugh data for our sample; reset our accumulators for next time. */
	hc->accumulated_sample_duration = 0;
	hc->accumulated_completion_count = 0;

	/* Add the current thread count and throughput sample to our history. */
	throughput = ((gdouble) completions) / sample_duration;

	sample_index = hc->total_samples % hc->samples_to_measure;
	hc->samples [sample_index] = throughput;
	hc->thread_counts [sample_index] = current_thread_count;
	hc->total_samples ++;

	/* Set up defaults for our metrics. */
	thread_wave_component = mono_double_complex_make(0, 0);
	throughput_wave_component = mono_double_complex_make(0, 0);
	throughput_error_estimate = 0;
	ratio = mono_double_complex_make(0, 0);
	confidence = 0;

	transition = TRANSITION_WARMUP;

	/* How many samples will we use? It must be at least the three wave periods we're looking for, and it must also
	 * be a whole multiple of the primary wave's period; otherwise the frequency we're looking for will fall between
	 * two frequency bands in the Fourier analysis, and we won't be able to measure it accurately. */
	sample_count = ((gint) MIN (hc->total_samples - 1, hc->samples_to_measure) / hc->wave_period) * hc->wave_period;

	if (sample_count > hc->wave_period) {
		guint i;
		gdouble average_throughput;
		gdouble average_thread_count;
		gdouble sample_sum = 0;
		gdouble thread_sum = 0;

		/* Average the throughput and thread count samples, so we can scale the wave magnitudes later. */
		for (i = 0; i < sample_count; ++i) {
			guint j = (hc->total_samples - sample_count + i) % hc->samples_to_measure;
			sample_sum += hc->samples [j];
			thread_sum += hc->thread_counts [j];
		}

		average_throughput = sample_sum / sample_count;
		average_thread_count = thread_sum / sample_count;

		if (average_throughput > 0 && average_thread_count > 0) {
			gdouble noise_for_confidence, adjacent_period_1, adjacent_period_2;

			/* Calculate the periods of the adjacent frequency bands we'll be using to
			 * measure noise levels. We want the two adjacent Fourier frequency bands. */
			adjacent_period_1 = sample_count / (((gdouble) sample_count) / ((gdouble) hc->wave_period) + 1);
			adjacent_period_2 = sample_count / (((gdouble) sample_count) / ((gdouble) hc->wave_period) - 1);

			/* Get the the three different frequency components of the throughput (scaled by average
			 * throughput). Our "error" estimate (the amount of noise that might be present in the
			 * frequency band we're really interested in) is the average of the adjacent bands. */
			throughput_wave_component = mono_double_complex_scalar_div (hill_climbing_get_wave_component (hc->samples, sample_count, hc->wave_period), average_throughput);
			throughput_error_estimate = cabs (mono_double_complex_scalar_div (hill_climbing_get_wave_component (hc->samples, sample_count, adjacent_period_1), average_throughput));

			if (adjacent_period_2 <= sample_count) {
				throughput_error_estimate = MAX (throughput_error_estimate, cabs (mono_double_complex_scalar_div (hill_climbing_get_wave_component (
					hc->samples, sample_count, adjacent_period_2), average_throughput)));
			}

			/* Do the same for the thread counts, so we have something to compare to. We don't
			 * measure thread count noise, because there is none; these are exact measurements. */
			thread_wave_component = mono_double_complex_scalar_div (hill_climbing_get_wave_component (hc->thread_counts, sample_count, hc->wave_period), average_thread_count);

			/* Update our moving average of the throughput noise. We'll use this
			 * later as feedback to determine the new size of the thread wave. */
			if (hc->average_throughput_noise == 0) {
				hc->average_throughput_noise = throughput_error_estimate;
			} else {
				hc->average_throughput_noise = (hc->throughput_error_smoothing_factor * throughput_error_estimate)
					+ ((1.0 + hc->throughput_error_smoothing_factor) * hc->average_throughput_noise);
			}

			if (cabs (thread_wave_component) > 0) {
				/* Adjust the throughput wave so it's centered around the target wave,
				 * and then calculate the adjusted throughput/thread ratio. */
				ratio = mono_double_complex_div (mono_double_complex_sub (throughput_wave_component, mono_double_complex_scalar_mul(thread_wave_component, hc->target_throughput_ratio)), thread_wave_component);
				transition = TRANSITION_CLIMBING_MOVE;
			} else {
				ratio = mono_double_complex_make (0, 0);
				transition = TRANSITION_STABILIZING;
			}

			noise_for_confidence = MAX (hc->average_throughput_noise, throughput_error_estimate);
			if (noise_for_confidence > 0) {
				confidence = cabs (thread_wave_component) / noise_for_confidence / hc->target_signal_to_noise_ratio;
			} else {
				/* there is no noise! */
				confidence = 1.0;
			}
		}
	}

	/* We use just the real part of the complex ratio we just calculated. If the throughput signal
	 * is exactly in phase with the thread signal, this will be the same as taking the magnitude of
	 * the complex move and moving that far up. If they're 180 degrees out of phase, we'll move
	 * backward (because this indicates that our changes are having the opposite of the intended effect).
	 * If they're 90 degrees out of phase, we won't move at all, because we can't tell wether we're
	 * having a negative or positive effect on throughput. */
	move = creal (ratio);
	move = CLAMP (move, -1.0, 1.0);

	/* Apply our confidence multiplier. */
	move *= CLAMP (confidence, -1.0, 1.0);

	/* Now apply non-linear gain, such that values around zero are attenuated, while higher values
	 * are enhanced. This allows us to move quickly if we're far away from the target, but more slowly
	* if we're getting close, giving us rapid ramp-up without wild oscillations around the target. */
	gain = hc->max_change_per_second * sample_duration;
	move = pow (fabs (move), hc->gain_exponent) * (move >= 0.0 ? 1 : -1) * gain;
	move = MIN (move, hc->max_change_per_sample);

	/* If the result was positive, and CPU is > 95%, refuse the move. */
	if (move > 0.0 && threadpool->cpu_usage > CPU_USAGE_HIGH)
		move = 0.0;

	/* Apply the move to our control setting. */
	hc->current_control_setting += move;

	/* Calculate the new thread wave magnitude, which is based on the moving average we've been keeping of the
	 * throughput error.  This average starts at zero, so we'll start with a nice safe little wave at first. */
	new_thread_wave_magnitude = (gint)(0.5 + (hc->current_control_setting * hc->average_throughput_noise
		* hc->target_signal_to_noise_ratio * hc->thread_magnitude_multiplier * 2.0));
	new_thread_wave_magnitude = CLAMP (new_thread_wave_magnitude, 1, hc->max_thread_wave_magnitude);

	/* Make sure our control setting is within the ThreadPool's limits. */
	hc->current_control_setting = CLAMP (hc->current_control_setting, threadpool->limit_worker_min, threadpool->limit_worker_max - new_thread_wave_magnitude);

	/* Calculate the new thread count (control setting + square wave). */
	new_thread_count = (gint)(hc->current_control_setting + new_thread_wave_magnitude * ((hc->total_samples / (hc->wave_period / 2)) % 2));

	/* Make sure the new thread count doesn't exceed the ThreadPool's limits. */
	new_thread_count = CLAMP (new_thread_count, threadpool->limit_worker_min, threadpool->limit_worker_max);

	if (new_thread_count != current_thread_count)
		hill_climbing_change_thread_count (new_thread_count, transition);

	if (creal (ratio) < 0.0 && new_thread_count == threadpool->limit_worker_min)
		*adjustment_interval = (gint)(0.5 + hc->current_sample_interval * (10.0 * MAX (-1.0 * creal (ratio), 1.0)));
	else
		*adjustment_interval = hc->current_sample_interval;

	return new_thread_count;
}

static void
heuristic_notify_work_completed (void)
{
	g_assert (threadpool);

	InterlockedIncrement (&threadpool->heuristic_completions);
	threadpool->heuristic_last_dequeue = mono_msec_ticks ();
}

static gboolean
heuristic_should_adjust (void)
{
	g_assert (threadpool);

	if (threadpool->heuristic_last_dequeue > threadpool->heuristic_last_adjustment + threadpool->heuristic_adjustment_interval) {
		ThreadPoolCounter counter;
		counter.as_gint64 = COUNTER_READ();
		if (counter._.working <= counter._.max_working)
			return TRUE;
	}

	return FALSE;
}

static void
heuristic_adjust (void)
{
	g_assert (threadpool);

	if (mono_coop_mutex_trylock (&threadpool->heuristic_lock) == 0) {
		gint32 completions = InterlockedExchange (&threadpool->heuristic_completions, 0);
		guint32 sample_end = mono_msec_ticks ();
		guint32 sample_duration = sample_end - threadpool->heuristic_sample_start;

		if (sample_duration >= threadpool->heuristic_adjustment_interval / 2) {
			ThreadPoolCounter counter;
			gint16 new_thread_count;

			counter.as_gint64 = COUNTER_READ ();
			new_thread_count = hill_climbing_update (counter._.max_working, sample_duration, completions, &threadpool->heuristic_adjustment_interval);

			COUNTER_ATOMIC (counter, { counter._.max_working = new_thread_count; });

			if (new_thread_count > counter._.max_working)
				worker_request (mono_domain_get ());

			threadpool->heuristic_sample_start = sample_end;
			threadpool->heuristic_last_adjustment = mono_msec_ticks ();
		}

		mono_coop_mutex_unlock (&threadpool->heuristic_lock);
	}
}

void
mono_threadpool_ms_cleanup (void)
{
	#ifndef DISABLE_SOCKETS
		mono_threadpool_ms_io_cleanup ();
	#endif
	mono_lazy_cleanup (&status, cleanup);
}

MonoAsyncResult *
mono_threadpool_ms_begin_invoke (MonoDomain *domain, MonoObject *target, MonoMethod *method, gpointer *params)
{
	static MonoClass *async_call_klass = NULL;
	MonoError error;
	MonoMethodMessage *message;
	MonoAsyncResult *async_result;
	MonoAsyncCall *async_call;
	MonoDelegate *async_callback = NULL;
	MonoObject *state = NULL;

	if (!async_call_klass)
		async_call_klass = mono_class_load_from_name (mono_defaults.corlib, "System", "MonoAsyncCall");

	mono_lazy_initialize (&status, initialize);

	message = mono_method_call_message_new (method, params, mono_get_delegate_invoke (method->klass), (params != NULL) ? (&async_callback) : NULL, (params != NULL) ? (&state) : NULL);

	async_call = (MonoAsyncCall*) mono_object_new_checked (domain, async_call_klass, &error);
	mono_error_raise_exception (&error); /* FIXME don't raise here */

	MONO_OBJECT_SETREF (async_call, msg, message);
	MONO_OBJECT_SETREF (async_call, state, state);

	if (async_callback) {
		MONO_OBJECT_SETREF (async_call, cb_method, mono_get_delegate_invoke (((MonoObject*) async_callback)->vtable->klass));
		MONO_OBJECT_SETREF (async_call, cb_target, async_callback);
	}

	async_result = mono_async_result_new (domain, NULL, async_call->state, NULL, (MonoObject*) async_call);
	MONO_OBJECT_SETREF (async_result, async_delegate, target);

	mono_threadpool_ms_enqueue_work_item (domain, (MonoObject*) async_result);

	return async_result;
}

MonoObject *
mono_threadpool_ms_end_invoke (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc)
{
	MonoAsyncCall *ac;

	g_assert (exc);
	g_assert (out_args);

	*exc = NULL;
	*out_args = NULL;

	/* check if already finished */
	mono_monitor_enter ((MonoObject*) ares);

	if (ares->endinvoke_called) {
		*exc = (MonoObject*) mono_get_exception_invalid_operation (NULL);
		mono_monitor_exit ((MonoObject*) ares);
		return NULL;
	}

	ares->endinvoke_called = 1;

	/* wait until we are really finished */
	if (ares->completed) {
		mono_monitor_exit ((MonoObject *) ares);
	} else {
		gpointer wait_event;
		if (ares->handle) {
			wait_event = mono_wait_handle_get_handle ((MonoWaitHandle*) ares->handle);
		} else {
			wait_event = CreateEvent (NULL, TRUE, FALSE, NULL);
			g_assert(wait_event);
			MONO_OBJECT_SETREF (ares, handle, (MonoObject*) mono_wait_handle_new (mono_object_domain (ares), wait_event));
		}
		mono_monitor_exit ((MonoObject*) ares);
		MONO_PREPARE_BLOCKING;
		WaitForSingleObjectEx (wait_event, INFINITE, TRUE);
		MONO_FINISH_BLOCKING;
	}

	ac = (MonoAsyncCall*) ares->object_data;
	g_assert (ac);

	*exc = ac->msg->exc; /* FIXME: GC add write barrier */
	*out_args = ac->out_args;
	return ac->res;
}

gboolean
mono_threadpool_ms_remove_domain_jobs (MonoDomain *domain, int timeout)
{
	gboolean res = TRUE;
	guint32 start;
	gpointer sem;

	g_assert (domain);
	g_assert (timeout >= -1);

	g_assert (mono_domain_is_unloading (domain));

	if (timeout != -1)
		start = mono_msec_ticks ();

#ifndef DISABLE_SOCKETS
	mono_threadpool_ms_io_remove_domain_jobs (domain);
	if (timeout != -1) {
		timeout -= mono_msec_ticks () - start;
		if (timeout < 0)
			return FALSE;
	}
#endif

	/*
	 * There might be some threads out that could be about to execute stuff from the given domain.
	 * We avoid that by setting up a semaphore to be pulsed by the thread that reaches zero.
	 */
	sem = domain->cleanup_semaphore = CreateSemaphore (NULL, 0, 1, NULL);

	/*
	 * The memory barrier here is required to have global ordering between assigning to cleanup_semaphone
	 * and reading threadpool_jobs. Otherwise this thread could read a stale version of threadpool_jobs
	 * and wait forever.
	 */
	mono_memory_write_barrier ();

	while (domain->threadpool_jobs) {
		MONO_PREPARE_BLOCKING;
		WaitForSingleObject (sem, timeout);
		MONO_FINISH_BLOCKING;
		if (timeout != -1) {
			timeout -= mono_msec_ticks () - start;
			if (timeout <= 0) {
				res = FALSE;
				break;
			}
		}
	}

	domain->cleanup_semaphore = NULL;
	CloseHandle (sem);

	return res;
}

void
mono_threadpool_ms_suspend (void)
{
	if (threadpool)
		threadpool->suspended = TRUE;
}

void
mono_threadpool_ms_resume (void)
{
	if (threadpool)
		threadpool->suspended = FALSE;
}

void
ves_icall_System_Threading_ThreadPool_GetAvailableThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads)
{
	ThreadPoolCounter counter;

	if (!worker_threads || !completion_port_threads)
		return;

	mono_lazy_initialize (&status, initialize);

	counter.as_gint64 = COUNTER_READ ();

	*worker_threads = MAX (0, threadpool->limit_worker_max - counter._.active);
	*completion_port_threads = threadpool->limit_io_max;
}

void
ves_icall_System_Threading_ThreadPool_GetMinThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads)
{
	if (!worker_threads || !completion_port_threads)
		return;

	mono_lazy_initialize (&status, initialize);

	*worker_threads = threadpool->limit_worker_min;
	*completion_port_threads = threadpool->limit_io_min;
}

void
ves_icall_System_Threading_ThreadPool_GetMaxThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads)
{
	if (!worker_threads || !completion_port_threads)
		return;

	mono_lazy_initialize (&status, initialize);

	*worker_threads = threadpool->limit_worker_max;
	*completion_port_threads = threadpool->limit_io_max;
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMinThreadsNative (gint32 worker_threads, gint32 completion_port_threads)
{
	mono_lazy_initialize (&status, initialize);

	if (worker_threads <= 0 || worker_threads > threadpool->limit_worker_max)
		return FALSE;
	if (completion_port_threads <= 0 || completion_port_threads > threadpool->limit_io_max)
		return FALSE;

	threadpool->limit_worker_min = worker_threads;
	threadpool->limit_io_min = completion_port_threads;

	return TRUE;
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMaxThreadsNative (gint32 worker_threads, gint32 completion_port_threads)
{
	gint cpu_count = mono_cpu_count ();

	mono_lazy_initialize (&status, initialize);

	if (worker_threads < threadpool->limit_worker_min || worker_threads < cpu_count)
		return FALSE;
	if (completion_port_threads < threadpool->limit_io_min || completion_port_threads < cpu_count)
		return FALSE;

	threadpool->limit_worker_max = worker_threads;
	threadpool->limit_io_max = completion_port_threads;

	return TRUE;
}

void
ves_icall_System_Threading_ThreadPool_InitializeVMTp (MonoBoolean *enable_worker_tracking)
{
	if (enable_worker_tracking) {
		// TODO implement some kind of switch to have the possibily to use it
		*enable_worker_tracking = FALSE;
	}

	mono_lazy_initialize (&status, initialize);
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_NotifyWorkItemComplete (void)
{
	ThreadPoolCounter counter;

	if (mono_domain_is_unloading (mono_domain_get ()) || mono_runtime_is_shutting_down ())
		return FALSE;

	heuristic_notify_work_completed ();

	if (heuristic_should_adjust ())
		heuristic_adjust ();

	counter.as_gint64 = COUNTER_READ ();
	return counter._.working <= counter._.max_working;
}

void
ves_icall_System_Threading_ThreadPool_NotifyWorkItemProgressNative (void)
{
	heuristic_notify_work_completed ();

	if (heuristic_should_adjust ())
		heuristic_adjust ();
}

void
ves_icall_System_Threading_ThreadPool_ReportThreadStatus (MonoBoolean is_working)
{
	// TODO
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
}

MonoBoolean
ves_icall_System_Threading_ThreadPool_RequestWorkerThread (void)
{
	return worker_request (mono_domain_get ());
}

MonoBoolean G_GNUC_UNUSED
ves_icall_System_Threading_ThreadPool_PostQueuedCompletionStatus (MonoNativeOverlapped *native_overlapped)
{
	/* This copy the behavior of the current Mono implementation */
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
	return FALSE;
}

MonoBoolean G_GNUC_UNUSED
ves_icall_System_Threading_ThreadPool_BindIOCompletionCallbackNative (gpointer file_handle)
{
	/* This copy the behavior of the current Mono implementation */
	return TRUE;
}

MonoBoolean G_GNUC_UNUSED
ves_icall_System_Threading_ThreadPool_IsThreadPoolHosted (void)
{
	return FALSE;
}
