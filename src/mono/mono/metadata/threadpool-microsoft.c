/*
 * threadpool-microsoft.c: Microsoft threadpool runtime support
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
#include <complex.h>
#include <math.h>

#include <glib.h>

#include "class-internals.h"
#include "exception.h"
#include "object.h"
#include "object-internals.h"
#include "threadpool-microsoft.h"
#include "threadpool-internals.h"
#include "utils/atomic.h"
#include "utils/mono-compiler.h"
#include "utils/mono-proclib.h"
#include "utils/mono-threads.h"
#include "utils/mono-time.h"
#include "utils/mono-rand.h"

#define SMALL_STACK (sizeof (gpointer) * 32 * 1024)

#define CPU_USAGE_LOW 80
#define CPU_USAGE_HIGH 95

#define MONITOR_INTERVAL 100 // ms

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

/* Keep in sync with System.Threading.NativeOverlapped */
struct _MonoNativeOverlapped {
	gpointer internal_low;
	gpointer internal_high;
	gint offset_low;
	gint offset_high;
	gpointer event_handle;
};

/* Keep in sync with System.Threading.RegisteredWaitHandleSafe */
typedef struct _MonoRegisteredWaitHandleSafe MonoRegisteredWaitHandleSafe;
struct _MonoRegisteredWaitHandleSafe {
	MonoObject object;
	gpointer registered_wait_handle;
	MonoWaitHandle *internal_wait_object;
	gboolean release_needed; // init: false
	volatile gint32 lock; // initt: 0
};

/* Keep in sync with System.Threading.RegisteredWaitHandle */
struct _MonoRegisteredWaitHandle {
	MonoObject object;
	MonoRegisteredWaitHandleSafe *internal_registered_wait;
};

/* Keep in sync with the System.MonoAsyncCall class which provides GC tracking */
typedef struct _MonoAsyncCall MonoAsyncCall;
struct _MonoAsyncCall {
	MonoObject object;
	MonoMethodMessage *msg;
	MonoMethod *cb_method;
	MonoDelegate *cb_target;
	MonoObject *state;
	MonoObject *res;
	MonoArray *out_args;
};

/* Keep in sync with System.Threading.RuntimeWorkItem */
struct _MonoRuntimeWorkItem {
	MonoObject object;
	MonoAsyncResult *ares;
};

typedef union _ThreadPoolCounter ThreadPoolCounter;
union _ThreadPoolCounter {
	struct {
		gint16 max_working; /* determined by heuristic */
		gint16 active; /* working or waiting on thread_work_sem; warm threads */
		gint16 working;
		gint16 parked;
	} _;
	gint64 as_gint64;
};

typedef struct _ThreadPoolDomain ThreadPoolDomain;
struct _ThreadPoolDomain {
	MonoDomain *domain;
	gint32 outstanding_request;
};

typedef struct _ThreadPoolThread ThreadPoolThread;
struct _ThreadPoolThread {
	mono_cond_t cond;
};

typedef struct _ThreadPoolHillClimbing ThreadPoolHillClimbing;
struct _ThreadPoolHillClimbing {
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
};

typedef struct _ThreadPool ThreadPool;
struct _ThreadPool {
	ThreadPoolCounter counters;

	GPtrArray *domains; // ThreadPoolDomain* []
	mono_mutex_t domains_lock;

	GPtrArray *working_threads; // MonoInternalThread* []
	mono_mutex_t working_threads_lock;

	GPtrArray *parked_threads; // ThreadPoolThread* []
	mono_mutex_t parked_threads_lock;

	gint32 heuristic_completions;
	guint32 heuristic_sample_start;
	guint32 heuristic_last_dequeue; // ms
	guint32 heuristic_last_adjustment; // ms
	guint32 heuristic_adjustment_interval; // ms
	ThreadPoolHillClimbing heuristic_hill_climbing;
	mono_mutex_t heuristic_lock;

	gint32 limit_worker_min;
	gint32 limit_worker_max;
	gint32 limit_io_min;
	gint32 limit_io_max;

	MonoCpuUsageState *cpu_usage_state;
	gint32 cpu_usage;

	/* suspended by the debugger */
	gboolean suspended;
};

typedef enum _ThreadPoolHeuristicStateTransition ThreadPoolHeuristicStateTransition;
enum _ThreadPoolHeuristicStateTransition {
	TRANSITION_WARMUP,
	TRANSITION_INITIALIZING,
	TRANSITION_RANDOM_MOVE,
	TRANSITION_CLIMBING_MOVE,
	TRANSITION_CHANGE_POINT,
	TRANSITION_STABILIZING,
	TRANSITION_STARVATION,
	TRANSITION_THREAD_TIMED_OUT,
	TRANSITION_UNDEFINED,
};

enum {
	STATUS_NOT_INITIALIZED,
	STATUS_INITIALIZING,
	STATUS_INITIALIZED,
	STATUS_CLEANING_UP,
	STATUS_CLEANED_UP,
};

enum {
	MONITOR_STATUS_REQUESTED,
	MONITOR_STATUS_WAITING_FOR_REQUEST,
	MONITOR_STATUS_NOT_RUNNING,
};

static gint32 status = STATUS_NOT_INITIALIZED;
static gint32 monitor_status = MONITOR_STATUS_NOT_RUNNING;

static ThreadPool* threadpool;

#define TP_COUNTER_CHECK(counter) \
	do { \
		g_assert (counter._.max_working > 0); \
		g_assert (counter._.active >= 0); \
	} while (0)

#define TP_COUNTER_READ() ((ThreadPoolCounter) InterlockedRead64 (&threadpool->counters.as_gint64))

#define TP_COUNTER_ATOMIC(var,block) \
	do { \
		ThreadPoolCounter __old; \
		do { \
			g_assert (threadpool); \
			(var) = __old = TP_COUNTER_READ (); \
			{ block; } \
			TP_COUNTER_CHECK (var); \
		} while (InterlockedCompareExchange64 (&threadpool->counters.as_gint64, (var).as_gint64, __old.as_gint64) != __old.as_gint64); \
	} while (0)

#define TP_COUNTER_TRY_ATOMIC(res,var,block) \
	do { \
		ThreadPoolCounter __old; \
		do { \
			g_assert (threadpool); \
			(var) = __old = TP_COUNTER_READ (); \
			(res) = FALSE; \
			{ block; } \
			TP_COUNTER_CHECK (var); \
			(res) = InterlockedCompareExchange64 (&threadpool->counters.as_gint64, (var).as_gint64, __old.as_gint64) == __old.as_gint64; \
		} while (0); \
	} while (0)

static gpointer
tp_rand_create (void)
{
	mono_rand_open ();
	return mono_rand_init (NULL, 0);
}

static guint32
tp_rand_next (gpointer *handle, guint32 min, guint32 max)
{
	guint32 val;
	if (!mono_rand_try_get_uint32 (handle, &val, min, max)) {
		// FIXME handle error
		g_assert_not_reached ();
	}
	return val;
}

static void
tp_rand_free (gpointer handle)
{
	mono_rand_close (handle);
}

static void
tp_ensure_initialized (gboolean *enable_worker_tracking)
{
	ThreadPoolHillClimbing *hc;
	const char *threads_per_cpu_env;
	gint threads_per_cpu;
	gint threads_count;

	if (enable_worker_tracking) {
		// TODO implement some kind of switch to have the possibily to use it
		*enable_worker_tracking = FALSE;
	}

	if (status >= STATUS_INITIALIZED)
		return;
	if (status == STATUS_INITIALIZING || InterlockedCompareExchange (&status, STATUS_INITIALIZING, STATUS_NOT_INITIALIZED) != STATUS_NOT_INITIALIZED) {
		while (status == STATUS_INITIALIZING)
			mono_thread_info_yield ();
		g_assert (status >= STATUS_INITIALIZED);
		return;
	}

	g_assert (!threadpool);
	threadpool = g_new0 (ThreadPool, 1);
	g_assert (threadpool);

	threadpool->domains = g_ptr_array_new ();
	mono_mutex_init_recursive (&threadpool->domains_lock);

	threadpool->parked_threads = g_ptr_array_new ();
	mono_mutex_init (&threadpool->parked_threads_lock);

	threadpool->working_threads = g_ptr_array_new ();
	mono_mutex_init (&threadpool->working_threads_lock);

	threadpool->heuristic_adjustment_interval = 10;
	mono_mutex_init (&threadpool->heuristic_lock);

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
	hc->random_interval_generator = tp_rand_create ();
	hc->current_sample_interval = tp_rand_next (&hc->random_interval_generator, hc->sample_interval_low, hc->sample_interval_high);

	if (!(threads_per_cpu_env = g_getenv ("MONO_THREADS_PER_CPU")))
		threads_per_cpu = 1;
	else
		threads_per_cpu = CLAMP (atoi (threads_per_cpu_env), 1, 50);

	threads_count = mono_cpu_count () * threads_per_cpu;

	threadpool->limit_worker_min = threadpool->limit_io_min = threads_count;
	threadpool->limit_worker_max = threadpool->limit_io_max = threads_count * 100;

	threadpool->counters._.max_working = threadpool->limit_worker_min;

	threadpool->cpu_usage_state = g_new0 (MonoCpuUsageState, 1);

	threadpool->suspended = FALSE;

	status = STATUS_INITIALIZED;
}

static void
tp_ensure_cleanedup (void)
{
	if (status == STATUS_NOT_INITIALIZED && InterlockedCompareExchange (&status, STATUS_CLEANED_UP, STATUS_NOT_INITIALIZED) == STATUS_NOT_INITIALIZED)
		return;
	if (status == STATUS_INITIALIZING) {
		while (status == STATUS_INITIALIZING)
			mono_thread_info_yield ();
	}
	if (status == STATUS_CLEANED_UP)
		return;
	if (status == STATUS_CLEANING_UP || InterlockedCompareExchange (&status, STATUS_CLEANING_UP, STATUS_INITIALIZED) != STATUS_INITIALIZED) {
		while (status == STATUS_CLEANING_UP)
			mono_thread_info_yield ();
		g_assert (status == STATUS_CLEANED_UP);
		return;
	}

	/* we make the assumption along the code that we are
	 * cleaning up only if the runtime is shutting down */
	g_assert (mono_runtime_is_shutting_down ());

	/* Unpark all worker threads */
	mono_mutex_lock (&threadpool->parked_threads_lock);
	for (;;) {
		guint i;
		ThreadPoolCounter counter = TP_COUNTER_READ ();
		if (counter._.active == 0 && counter._.parked == 0)
			break;
		for (i = 0; i < threadpool->parked_threads->len; ++i) {
			ThreadPoolThread *tpthread = g_ptr_array_index (threadpool->parked_threads, i);
			mono_cond_signal (&tpthread->cond);
		}
		mono_mutex_unlock (&threadpool->parked_threads_lock);
		usleep (1000);
		mono_mutex_lock (&threadpool->parked_threads_lock);
	}
	mono_mutex_unlock (&threadpool->parked_threads_lock);

	while (monitor_status != MONITOR_STATUS_NOT_RUNNING)
		usleep (1000);

	g_ptr_array_free (threadpool->domains, TRUE);
	mono_mutex_destroy (&threadpool->domains_lock);

	g_ptr_array_free (threadpool->parked_threads, TRUE);
	mono_mutex_destroy (&threadpool->parked_threads_lock);

	g_ptr_array_free (threadpool->working_threads, TRUE);
	mono_mutex_destroy (&threadpool->working_threads_lock);

	mono_mutex_destroy (&threadpool->heuristic_lock);
	g_free (threadpool->heuristic_hill_climbing.samples);
	g_free (threadpool->heuristic_hill_climbing.thread_counts);
	tp_rand_free (threadpool->heuristic_hill_climbing.random_interval_generator);

	g_free (threadpool->cpu_usage_state);

	g_assert (threadpool);
	g_free (threadpool);
	threadpool = NULL;
	g_assert (!threadpool);

	status = STATUS_CLEANED_UP;
}

static void
tp_queue_work_item (MonoRuntimeWorkItem *rwi)
{
	static MonoClass *threadpool_class = NULL;
	static MonoMethod *unsafe_queue_custom_work_item_method = NULL;
	MonoObject *exc = NULL;
	MonoBoolean f;
	gpointer args [2];

	g_assert (rwi);

	if (!threadpool_class)
		threadpool_class = mono_class_from_name (mono_defaults.corlib, "System.Threading", "ThreadPool");
	g_assert (threadpool_class);

	if (!unsafe_queue_custom_work_item_method)
		unsafe_queue_custom_work_item_method = mono_class_get_method_from_name (threadpool_class, "UnsafeQueueCustomWorkItem", 2);
	g_assert (unsafe_queue_custom_work_item_method);

	f = FALSE;

	args [0] = (gpointer) rwi;
	args [1] = (gpointer) mono_value_box (mono_domain_get (), mono_defaults.boolean_class, &f);

	mono_runtime_invoke (unsafe_queue_custom_work_item_method, rwi, args, &exc);
	if (exc)
		mono_raise_exception ((MonoException*) exc);
}

static void
tp_domain_add (ThreadPoolDomain *tpdomain)
{
	guint i, len;

	g_assert (tpdomain);

	mono_mutex_lock (&threadpool->domains_lock);
	len = threadpool->domains->len;
	for (i = 0; i < len; ++i) {
		if (g_ptr_array_index (threadpool->domains, i) == tpdomain)
			break;
	}
	if (i == len)
		g_ptr_array_add (threadpool->domains, tpdomain);
	mono_mutex_unlock (&threadpool->domains_lock);
}

static gboolean
tp_domain_remove (ThreadPoolDomain *tpdomain)
{
	gboolean res;

	g_assert (tpdomain);

	mono_mutex_lock (&threadpool->domains_lock);
	res = g_ptr_array_remove (threadpool->domains, tpdomain);
	mono_mutex_unlock (&threadpool->domains_lock);

	return res;
}

static ThreadPoolDomain *
tp_domain_get_or_create (MonoDomain *domain)
{
	ThreadPoolDomain *tpdomain = NULL;
	guint i;

	g_assert (domain);

	mono_mutex_lock (&threadpool->domains_lock);
	for (i = 0; i < threadpool->domains->len; ++i) {
		ThreadPoolDomain *tmp = g_ptr_array_index (threadpool->domains, i);
		if (tmp->domain == domain) {
			tpdomain = tmp;
			break;
		}
	}
	if (!tpdomain) {
		tpdomain = g_new0 (ThreadPoolDomain, 1);
		tpdomain->domain = domain;
		tp_domain_add (tpdomain);
	}
	mono_mutex_unlock (&threadpool->domains_lock);
	return tpdomain;
}

static gboolean
tp_domain_any_has_request ()
{
	gboolean res = FALSE;
	guint i;

	mono_mutex_lock (&threadpool->domains_lock);
	for (i = 0; i < threadpool->domains->len; ++i) {
		ThreadPoolDomain *tmp = g_ptr_array_index (threadpool->domains, i);
		if (tmp->outstanding_request > 0) {
			res = TRUE;
			break;
		}
	}
	mono_mutex_unlock (&threadpool->domains_lock);
	return res;
}

static ThreadPoolDomain *
tp_domain_get_next (ThreadPoolDomain *current)
{
	ThreadPoolDomain *tpdomain = NULL;
	guint len;

	mono_mutex_lock (&threadpool->domains_lock);
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
			ThreadPoolDomain *tmp = g_ptr_array_index (threadpool->domains, i % len);
			if (tmp->outstanding_request > 0) {
				tpdomain = tmp;
				tpdomain->outstanding_request --;
				g_assert (tpdomain->outstanding_request >= 0);
				break;
			}
		}
	}
	mono_mutex_unlock (&threadpool->domains_lock);
	return tpdomain;
}

static void
tp_worker_park (void)
{
	ThreadPoolThread tpthread;
	mono_cond_init (&tpthread.cond, NULL);

	mono_mutex_lock (&threadpool->parked_threads_lock);
	g_ptr_array_add (threadpool->parked_threads, &tpthread);
	mono_cond_wait (&tpthread.cond, &threadpool->parked_threads_lock);
	g_ptr_array_remove (threadpool->parked_threads, &tpthread);
	mono_mutex_unlock (&threadpool->parked_threads_lock);

	mono_cond_destroy (&tpthread.cond);
}

static gboolean
tp_worker_try_unpark (void)
{
	gboolean res = FALSE;
	guint len;

	mono_mutex_lock (&threadpool->parked_threads_lock);
	len = threadpool->parked_threads->len;
	if (len > 0) {
		ThreadPoolThread *tpthread = g_ptr_array_index (threadpool->parked_threads, len - 1);
		mono_cond_signal (&tpthread->cond);
		res = TRUE;
	}
	mono_mutex_unlock (&threadpool->parked_threads_lock);
	return res;
}

static void
tp_worker_thread (gpointer data)
{
	static MonoClass *threadpool_wait_callback_class = NULL;
	static MonoMethod *perform_wait_callback_method = NULL;
	MonoInternalThread *thread;
	ThreadPoolDomain *tpdomain;
	ThreadPoolCounter counter;
	gboolean retire = FALSE;

	g_assert (status >= STATUS_INITIALIZED);

	tpdomain = data;
	g_assert (tpdomain);
	g_assert (tpdomain->domain);

	if (mono_runtime_is_shutting_down () || mono_domain_is_unloading (tpdomain->domain)) {
		TP_COUNTER_ATOMIC (counter, { counter._.active --; });
		return;
	}

	if (!threadpool_wait_callback_class)
		threadpool_wait_callback_class = mono_class_from_name (mono_defaults.corlib, "System.Threading.Microsoft", "_ThreadPoolWaitCallback");
	g_assert (threadpool_wait_callback_class);

	if (!perform_wait_callback_method)
		perform_wait_callback_method = mono_class_get_method_from_name (threadpool_wait_callback_class, "PerformWaitCallback", 0);
	g_assert (perform_wait_callback_method);

	g_assert (threadpool);

	thread = mono_thread_internal_current ();
	g_assert (thread);

	mono_mutex_lock (&threadpool->domains_lock);

	do {
		guint i, c;

		g_assert (tpdomain);
		g_assert (tpdomain->domain);

		tpdomain->domain->threadpool_jobs ++;

		mono_mutex_unlock (&threadpool->domains_lock);

		mono_mutex_lock (&threadpool->working_threads_lock);
		g_ptr_array_add (threadpool->working_threads, thread);
		mono_mutex_unlock (&threadpool->working_threads_lock);

		TP_COUNTER_ATOMIC (counter, { counter._.working ++; });

		mono_thread_push_appdomain_ref (tpdomain->domain);
		if (mono_domain_set (tpdomain->domain, FALSE)) {
			MonoObject *exc = NULL;
			MonoObject *res = mono_runtime_invoke (perform_wait_callback_method, NULL, NULL, &exc);
			if (exc)
				mono_internal_thread_unhandled_exception (exc);
			else if (res && *(MonoBoolean*) mono_object_unbox (res) == FALSE)
				retire = TRUE;

			mono_thread_clr_state (thread , ~ThreadState_Background);
			if (!mono_thread_test_state (thread , ThreadState_Background))
				ves_icall_System_Threading_Thread_SetState (thread, ThreadState_Background);
		}
		mono_thread_pop_appdomain_ref ();

		TP_COUNTER_ATOMIC (counter, { counter._.working --; });

		mono_mutex_lock (&threadpool->working_threads_lock);
		g_ptr_array_remove_fast (threadpool->working_threads, thread);
		mono_mutex_unlock (&threadpool->working_threads_lock);

		mono_mutex_lock (&threadpool->domains_lock);

		tpdomain->domain->threadpool_jobs --;
		g_assert (tpdomain->domain->threadpool_jobs >= 0);

		if (tpdomain->domain->threadpool_jobs == 0 && mono_domain_is_unloading (tpdomain->domain)) {
			gboolean removed = tp_domain_remove (tpdomain);
			g_assert (removed);
			if (tpdomain->domain->cleanup_semaphore)
				ReleaseSemaphore (tpdomain->domain->cleanup_semaphore, 1, NULL);
			g_free (tpdomain);
			tpdomain = NULL;
		}

		for (i = 0, c = 5; i < c; ++i) {
			if (mono_runtime_is_shutting_down ())
				break;

			if (!retire) {
				tpdomain = tp_domain_get_next (tpdomain);
				if (tpdomain)
					break;
			}

			if (i < c - 1) {
				gboolean park = TRUE;

				TP_COUNTER_ATOMIC (counter, {
					if (counter._.active <= counter._.max_working) {
						park = FALSE;
						break;
					}
					counter._.active --;
					counter._.parked ++;
				});

				if (park) {
					mono_mutex_unlock (&threadpool->domains_lock);
					tp_worker_park ();
					mono_mutex_lock (&threadpool->domains_lock);

					TP_COUNTER_ATOMIC (counter, {
						counter._.active ++;
						counter._.parked --;
					});
				}
			}

			retire = FALSE;
		}
	} while (tpdomain && !mono_runtime_is_shutting_down ());

	mono_mutex_unlock (&threadpool->domains_lock);

	TP_COUNTER_ATOMIC (counter, { counter._.active --; });
}

static gboolean
tp_worker_try_create (ThreadPoolDomain *tpdomain)
{
	g_assert (tpdomain);
	g_assert (tpdomain->domain);

	return mono_thread_create_internal (tpdomain->domain, tp_worker_thread, tpdomain, TRUE, 0) != NULL;
}

static void tp_monitor_ensure_running (void);

static gboolean
tp_worker_request (MonoDomain *domain)
{
	ThreadPoolDomain *tpdomain;
	ThreadPoolCounter counter;

	g_assert (domain);
	g_assert (threadpool);

	if (mono_runtime_is_shutting_down () || mono_domain_is_unloading (domain))
		return FALSE;

	mono_mutex_lock (&threadpool->domains_lock);
	tpdomain = tp_domain_get_or_create (domain);
	g_assert (tpdomain);
	tpdomain->outstanding_request ++;
	mono_mutex_unlock (&threadpool->domains_lock);

	if (threadpool->suspended)
		return FALSE;

	tp_monitor_ensure_running ();

	if (tp_worker_try_unpark ())
		return TRUE;

	TP_COUNTER_ATOMIC (counter, {
		if (counter._.active >= counter._.max_working)
			return FALSE;
		counter._.active ++;
	});

	if (tp_worker_try_create (tpdomain))
		return TRUE;

	TP_COUNTER_ATOMIC (counter, { counter._.active --; });
	return FALSE;
}

static gboolean
tp_monitor_should_keep_running (void)
{
	g_assert (monitor_status == MONITOR_STATUS_WAITING_FOR_REQUEST || monitor_status == MONITOR_STATUS_REQUESTED);

	if (InterlockedExchange (&monitor_status, MONITOR_STATUS_WAITING_FOR_REQUEST) == MONITOR_STATUS_WAITING_FOR_REQUEST) {
		if (mono_runtime_is_shutting_down () || !tp_domain_any_has_request ()) {
			if (InterlockedExchange (&monitor_status, MONITOR_STATUS_NOT_RUNNING) == MONITOR_STATUS_WAITING_FOR_REQUEST)
				return FALSE;
		}
	}

	g_assert (monitor_status == MONITOR_STATUS_WAITING_FOR_REQUEST || monitor_status == MONITOR_STATUS_REQUESTED);

	return TRUE;
}

static gboolean
tp_monitor_sufficient_delay_since_last_dequeue (void)
{
	guint32 threshold;

	g_assert (threadpool);

	if (threadpool->cpu_usage < CPU_USAGE_LOW) {
		threshold = MONITOR_INTERVAL;
	} else {
		ThreadPoolCounter counter = TP_COUNTER_READ ();
		threshold = counter._.max_working * MONITOR_INTERVAL * 2;
	}

	return mono_msec_ticks () >= threadpool->heuristic_last_dequeue + threshold;
}

static void tp_hill_climbing_force_change (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition);

static void
tp_monitor_thread (void)
{
	MonoInternalThread *current_thread = mono_thread_internal_current ();
	guint i;

	mono_cpu_usage (threadpool->cpu_usage_state);

	do {
		MonoInternalThread *thread;
		gboolean all_waitsleepjoin = TRUE;
		gint32 interval_left = MONITOR_INTERVAL;
		gint32 awake = 0; /* number of spurious awakes we tolerate before doing a round of rebalancing */

		g_assert (monitor_status != MONITOR_STATUS_NOT_RUNNING);

		do {
			guint32 ts;

			if (mono_runtime_is_shutting_down ())
				break;

			ts = mono_msec_ticks ();
			if (SleepEx (interval_left, TRUE) == 0)
				break;
			interval_left -= mono_msec_ticks () - ts;

			if ((current_thread->state & (ThreadState_StopRequested | ThreadState_SuspendRequested)) != 0)
				mono_thread_interruption_checkpoint ();
		} while (interval_left > 0 && ++awake < 10);

		if (threadpool->suspended)
			continue;

		if (mono_runtime_is_shutting_down () || !tp_domain_any_has_request ())
			continue;

		mono_mutex_lock (&threadpool->working_threads_lock);
		for (i = 0; i < threadpool->working_threads->len; ++i) {
			thread = g_ptr_array_index (threadpool->working_threads, i);
			if ((thread->state & ThreadState_WaitSleepJoin) == 0) {
				all_waitsleepjoin = FALSE;
				break;
			}
		}
		mono_mutex_unlock (&threadpool->working_threads_lock);

		if (all_waitsleepjoin) {
			ThreadPoolCounter counter;
			TP_COUNTER_ATOMIC (counter, { counter._.max_working ++; });
			tp_hill_climbing_force_change (counter._.max_working, TRANSITION_STARVATION);
		}

		threadpool->cpu_usage = mono_cpu_usage (threadpool->cpu_usage_state);

		if (tp_monitor_sufficient_delay_since_last_dequeue ()) {
			for (i = 0; i < 5; ++i) {
				ThreadPoolDomain *tpdomain;
				ThreadPoolCounter counter;
				gboolean success;

				if (mono_runtime_is_shutting_down ())
					break;

				if (tp_worker_try_unpark ())
					break;

				TP_COUNTER_TRY_ATOMIC (success, counter, {
					if (counter._.active >= counter._.max_working)
						break;
					counter._.active ++;
				});

				if (!success)
					continue;

				tpdomain = tp_domain_get_next (NULL);
				if (tpdomain && tp_worker_try_create (tpdomain))
					break;

				TP_COUNTER_ATOMIC (counter, { counter._.active --; });
			}
		}
	} while (tp_monitor_should_keep_running ());
}

static void
tp_monitor_ensure_running (void)
{
	for (;;) {
		switch (monitor_status) {
		case MONITOR_STATUS_REQUESTED:
			return;
		case MONITOR_STATUS_WAITING_FOR_REQUEST:
			InterlockedCompareExchange (&monitor_status, MONITOR_STATUS_REQUESTED, MONITOR_STATUS_WAITING_FOR_REQUEST);
			break;
		case MONITOR_STATUS_NOT_RUNNING:
			if (InterlockedCompareExchange (&monitor_status, MONITOR_STATUS_REQUESTED, MONITOR_STATUS_NOT_RUNNING) == MONITOR_STATUS_NOT_RUNNING) {
				if (!mono_thread_create_internal (mono_get_root_domain (), tp_monitor_thread, NULL, TRUE, SMALL_STACK))
					monitor_status = MONITOR_STATUS_NOT_RUNNING;
				return;
			}
			break;
		default: g_assert_not_reached ();
		}
	}
}

static void
tp_hill_climbing_change_thread_count (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition)
{
	ThreadPoolHillClimbing *hc;

	g_assert (threadpool);

	hc = &threadpool->heuristic_hill_climbing;

	hc->last_thread_count = new_thread_count;
	hc->current_sample_interval = tp_rand_next (&hc->random_interval_generator, hc->sample_interval_low, hc->sample_interval_high);
	hc->elapsed_since_last_change = 0;
	hc->completions_since_last_change = 0;
}

static void
tp_hill_climbing_force_change (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition)
{
	ThreadPoolHillClimbing *hc;

	g_assert (threadpool);

	hc = &threadpool->heuristic_hill_climbing;

	if (new_thread_count != hc->last_thread_count) {
		hc->current_control_setting += new_thread_count - hc->last_thread_count;
		tp_hill_climbing_change_thread_count (new_thread_count, transition);
	}
}

static double complex
tp_hill_climbing_get_wave_component (gdouble *samples, guint sample_count, gdouble period)
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

	return ((q1 - q2 * cosine) + (q2 * sine) * I) / ((gdouble) sample_count);
}

static gint16
tp_hill_climbing_update (gint16 current_thread_count, guint32 sample_duration, gint32 completions, guint32 *adjustment_interval)
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
	double complex thread_wave_component;
	double complex throughput_wave_component;
	double complex ratio;

	g_assert (threadpool);
	g_assert (adjustment_interval);

	hc = &threadpool->heuristic_hill_climbing;

	/* If someone changed the thread count without telling us, update our records accordingly. */
	if (current_thread_count != hc->last_thread_count)
		tp_hill_climbing_force_change (current_thread_count, TRANSITION_INITIALIZING);

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
	thread_wave_component = 0;
	throughput_wave_component = 0;
	throughput_error_estimate = 0;
	ratio = 0;
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
			throughput_wave_component = tp_hill_climbing_get_wave_component (hc->samples, sample_count, hc->wave_period) / average_throughput;
			throughput_error_estimate = cabs (tp_hill_climbing_get_wave_component (hc->samples, sample_count, adjacent_period_1) / average_throughput);

			if (adjacent_period_2 <= sample_count) {
				throughput_error_estimate = MAX (throughput_error_estimate, cabs (tp_hill_climbing_get_wave_component (
					hc->samples, sample_count, adjacent_period_2) / average_throughput));
			}

			/* Do the same for the thread counts, so we have something to compare to. We don't
			 * measure thread count noise, because there is none; these are exact measurements. */
			thread_wave_component = tp_hill_climbing_get_wave_component (hc->thread_counts, sample_count, hc->wave_period) / average_thread_count;

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
				ratio = (throughput_wave_component - (hc->target_throughput_ratio * thread_wave_component)) / thread_wave_component;
				transition = TRANSITION_CLIMBING_MOVE;
			} else {
				ratio = 0;
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
		tp_hill_climbing_change_thread_count (new_thread_count, transition);

	if (creal (ratio) < 0.0 && new_thread_count == threadpool->limit_worker_min)
		*adjustment_interval = (gint)(0.5 + hc->current_sample_interval * (10.0 * MAX (-1.0 * creal (ratio), 1.0)));
	else
		*adjustment_interval = hc->current_sample_interval;

	return new_thread_count;
}

static void
tp_heuristic_notify_work_completed (void)
{
	g_assert (threadpool);

	InterlockedIncrement (&threadpool->heuristic_completions);
	threadpool->heuristic_last_dequeue = mono_msec_ticks ();
}

static gboolean
tp_heuristic_should_adjust ()
{
	g_assert (threadpool);

	if (threadpool->heuristic_last_dequeue > threadpool->heuristic_last_adjustment + threadpool->heuristic_adjustment_interval) {
		ThreadPoolCounter counter = TP_COUNTER_READ ();
		if (counter._.active <= counter._.max_working)
			return TRUE;
	}

	return FALSE;
}

static void
tp_heuristic_adjust ()
{
	g_assert (threadpool);

	if (mono_mutex_trylock (&threadpool->heuristic_lock) == 0) {
		gint32 completions = InterlockedExchange (&threadpool->heuristic_completions, 0);
		guint32 sample_end = mono_msec_ticks ();
		guint32 sample_duration = sample_end - threadpool->heuristic_sample_start;

		if (sample_duration >= threadpool->heuristic_adjustment_interval / 2) {
			ThreadPoolCounter counter;
			gint16 new_thread_count;

			counter = TP_COUNTER_READ ();
			new_thread_count = tp_hill_climbing_update (counter._.max_working, sample_duration, completions, &threadpool->heuristic_adjustment_interval);

			TP_COUNTER_ATOMIC (counter, { counter._.max_working = new_thread_count; });

			if (new_thread_count > counter._.max_working)
				tp_worker_request (mono_domain_get ());

			threadpool->heuristic_sample_start = sample_end;
			threadpool->heuristic_last_adjustment = mono_msec_ticks ();
		}

		mono_mutex_unlock (&threadpool->heuristic_lock);
	}
}

void
mono_thread_pool_ms_cleanup (void)
{
	tp_ensure_cleanedup ();
}

MonoAsyncResult *
mono_thread_pool_ms_add (MonoObject *target, MonoMethodMessage *msg, MonoDelegate *async_callback, MonoObject *state)
{
	static MonoClass *async_call_klass = NULL;
	static MonoClass *runtime_work_item_class = NULL;
	MonoDomain *domain;
	MonoAsyncResult *ares;
	MonoAsyncCall *ac;
	MonoRuntimeWorkItem *rwi;

	if (!async_call_klass)
		async_call_klass = mono_class_from_name (mono_defaults.corlib, "System", "MonoAsyncCall");
	g_assert (async_call_klass);

	if (!runtime_work_item_class)
		runtime_work_item_class = mono_class_from_name (mono_defaults.corlib, "System.Threading", "MonoRuntimeWorkItem");
	g_assert (runtime_work_item_class);

	tp_ensure_initialized (NULL);

	domain = mono_domain_get ();

	ac = (MonoAsyncCall*) mono_object_new (domain, async_call_klass);
	MONO_OBJECT_SETREF (ac, msg, msg);
	MONO_OBJECT_SETREF (ac, state, state);

	if (async_callback) {
		MONO_OBJECT_SETREF (ac, cb_method, mono_get_delegate_invoke (((MonoObject*) async_callback)->vtable->klass));
		MONO_OBJECT_SETREF (ac, cb_target, async_callback);
	}

	ares = mono_async_result_new (domain, NULL, ac->state, NULL, (MonoObject*) ac);
	MONO_OBJECT_SETREF (ares, async_delegate, target);

	rwi = (MonoRuntimeWorkItem*) mono_object_new (domain, runtime_work_item_class);
	MONO_OBJECT_SETREF (rwi, ares, ares);

	tp_queue_work_item (rwi);

	return ares;
}

MonoObject *
mono_thread_pool_ms_finish (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc)
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

	MONO_OBJECT_SETREF (ares, endinvoke_called, 1);

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
		WaitForSingleObjectEx (wait_event, INFINITE, TRUE);
	}

	ac = (MonoAsyncCall*) ares->object_data;
	g_assert (ac);

	*exc = ac->msg->exc; /* FIXME: GC add write barrier */
	*out_args = ac->out_args;
	return ac->res;
}

gboolean
mono_thread_pool_ms_remove_domain_jobs (MonoDomain *domain, int timeout)
{
	gboolean res = TRUE;
	guint32 start, now;
	gpointer sem;

	sem = domain->cleanup_semaphore = CreateSemaphore (NULL, 0, 1, NULL);
	/*
	 * The memory barrier here is required to have global ordering between assigning to cleanup_semaphone
	 * and reading threadpool_jobs. Otherwise this thread could read a stale version of threadpool_jobs
	 * and wait forever.
	 */
	mono_memory_write_barrier ();

	if (timeout != -1 && domain->threadpool_jobs)
		start = mono_msec_ticks ();
	while (domain->threadpool_jobs) {
		WaitForSingleObject (sem, timeout);
		if (timeout != -1) {
			now = mono_msec_ticks ();
			if (now - start > timeout) {
				res = FALSE;
				break;
			}
			timeout -= now - start;
		}
	}

	domain->cleanup_semaphore = NULL;
	CloseHandle (sem);
	return res;
}

void
mono_thread_pool_ms_remove_socket (int sock)
{
	// FIXME
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
}

void
mono_thread_pool_ms_suspend (void)
{
	threadpool->suspended = TRUE;
}

void
mono_thread_pool_ms_resume (void)
{
	threadpool->suspended = FALSE;
}

void
ves_icall_System_Threading_MonoRuntimeWorkItem_ExecuteWorkItem (MonoRuntimeWorkItem *rwi)
{
	MonoAsyncResult *ares;
	MonoAsyncCall *ac;
	MonoObject *res;
	MonoObject *exc = NULL;
	MonoArray *out_args = NULL;
	gpointer wait_event = NULL;
	MonoInternalThread *thread = mono_thread_internal_current ();

	g_assert (rwi);
	ares = rwi->ares;
	g_assert (ares);

	if (!ares->execution_context) {
		MONO_OBJECT_SETREF (ares, original_context, NULL);
	} else {
		/* use captured ExecutionContext (if available) */
		MONO_OBJECT_SETREF (ares, original_context, mono_thread_get_execution_context ());
		mono_thread_set_execution_context (ares->execution_context);
	}

	ac = (MonoAsyncCall*) ares->object_data;

	if (!ac) {
		g_assert (ares->async_delegate);
		/* The debugger needs this */
		thread->async_invoke_method = ((MonoDelegate*) ares->async_delegate)->method;
		res = mono_runtime_delegate_invoke (ares->async_delegate, (gpointer*) &ares->async_state, &exc);
		thread->async_invoke_method = NULL;
	} else {
		MONO_OBJECT_SETREF (ac, msg->exc, NULL);
		res = mono_message_invoke (ares->async_delegate, ac->msg, &exc, &out_args);
		MONO_OBJECT_SETREF (ac, res, res);
		MONO_OBJECT_SETREF (ac, msg->exc, exc);
		MONO_OBJECT_SETREF (ac, out_args, out_args);

		mono_monitor_enter ((MonoObject*) ares);
		MONO_OBJECT_SETREF (ares, completed, 1);
		if (ares->handle)
			wait_event = mono_wait_handle_get_handle ((MonoWaitHandle*) ares->handle);
		mono_monitor_exit ((MonoObject*) ares);

		if (wait_event != NULL)
			SetEvent (wait_event);

		if (!ac->cb_method) {
			exc = NULL;
		} else {
			thread->async_invoke_method = ac->cb_method;
			mono_runtime_invoke (ac->cb_method, ac->cb_target, (gpointer*) &ares, &exc);
			thread->async_invoke_method = NULL;
		}
	}

	/* restore original thread execution context if flow isn't suppressed, i.e. non null */
	if (ares->original_context) {
		mono_thread_set_execution_context (ares->original_context);
		MONO_OBJECT_SETREF (ares, original_context, NULL);
	}

	if (exc)
		mono_raise_exception ((MonoException*) exc);
}

void
ves_icall_System_Threading_Microsoft_ThreadPool_GetAvailableThreadsNative (gint *worker_threads, gint *completion_port_threads)
{
	if (!worker_threads || !completion_port_threads)
		return;

	tp_ensure_initialized (NULL);

	*worker_threads = threadpool->limit_worker_max;
	*completion_port_threads = threadpool->limit_io_max;
}

void
ves_icall_System_Threading_Microsoft_ThreadPool_GetMinThreadsNative (gint *worker_threads, gint *completion_port_threads)
{
	if (!worker_threads || !completion_port_threads)
		return;

	tp_ensure_initialized (NULL);

	*worker_threads = threadpool->limit_worker_min;
	*completion_port_threads = threadpool->limit_io_min;
}

void
ves_icall_System_Threading_Microsoft_ThreadPool_GetMaxThreadsNative (gint *worker_threads, gint *completion_port_threads)
{
	if (!worker_threads || !completion_port_threads)
		return;

	tp_ensure_initialized (NULL);

	*worker_threads = threadpool->limit_worker_max;
	*completion_port_threads = threadpool->limit_io_max;
}

gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_SetMinThreadsNative (gint worker_threads, gint completion_port_threads)
{
	tp_ensure_initialized (NULL);

	if (worker_threads <= 0 || worker_threads > threadpool->limit_worker_max)
		return FALSE;
	if (completion_port_threads <= 0 || completion_port_threads > threadpool->limit_io_max)
		return FALSE;

	threadpool->limit_worker_max = worker_threads;
	threadpool->limit_io_max = completion_port_threads;

	return TRUE;
}

gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_SetMaxThreadsNative (gint worker_threads, gint completion_port_threads)
{
	gint cpu_count = mono_cpu_count ();

	tp_ensure_initialized (NULL);

	if (worker_threads < threadpool->limit_worker_min || worker_threads < cpu_count)
		return FALSE;
	if (completion_port_threads < threadpool->limit_io_min || completion_port_threads < cpu_count)
		return FALSE;

	threadpool->limit_worker_max = worker_threads;
	threadpool->limit_io_max = completion_port_threads;

	return TRUE;
}

void
ves_icall_System_Threading_Microsoft_ThreadPool_InitializeVMTp (gboolean *enable_worker_tracking)
{
	tp_ensure_initialized (enable_worker_tracking);
}

gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_NotifyWorkItemComplete (void)
{
	ThreadPoolCounter counter;

	if (mono_domain_is_unloading (mono_domain_get ()) || mono_runtime_is_shutting_down ())
		return FALSE;

	tp_heuristic_notify_work_completed ();

	if (tp_heuristic_should_adjust ())
		tp_heuristic_adjust ();

	counter = TP_COUNTER_READ ();
	return counter._.active <= counter._.max_working;
}

void
ves_icall_System_Threading_Microsoft_ThreadPool_NotifyWorkItemProgressNative (void)
{
	tp_heuristic_notify_work_completed ();

	if (tp_heuristic_should_adjust ())
		tp_heuristic_adjust ();
}

void
ves_icall_System_Threading_Microsoft_ThreadPool_ReportThreadStatus (gboolean is_working)
{
	// TODO
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
}

gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_RequestWorkerThread (void)
{
	return tp_worker_request (mono_domain_get ());
}

gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_PostQueuedCompletionStatus (MonoNativeOverlapped *native_overlapped)
{
	/* This copy the behavior of the current Mono implementation */
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
	return FALSE;
}

gpointer
ves_icall_System_Threading_Microsoft_ThreadPool_RegisterWaitForSingleObjectNative (MonoWaitHandle *wait_handle, MonoObject *state, guint timeout_internal, gboolean execute_only_once,
	MonoRegisteredWaitHandle *registered_wait_handle, gint stack_mark, gboolean compress_stack)
{
	// FIXME
	mono_raise_exception (mono_get_exception_not_implemented (NULL));
	return NULL;
}

gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_BindIOCompletionCallbackNative (gpointer file_handle)
{
	/* This copy the behavior of the current Mono implementation */
	return TRUE;
}
