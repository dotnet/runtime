/**
 * \file
 * native threadpool worker
 *
 * Author:
 *	Ludovic Henry (ludovic.henry@xamarin.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

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
#include <mono/metadata/threadpool.h>
#include <mono/metadata/threadpool-worker.h>
#include <mono/metadata/threadpool-io.h>
#include <mono/metadata/w32event.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-complex.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-rand.h>
#include <mono/utils/refcount.h>
#include <mono/utils/w32api.h>

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

typedef union {
	struct {
		gint16 max_working; /* determined by heuristic */
		gint16 starting; /* starting, but not yet in worker_thread */
		gint16 working; /* executing worker_thread */
		gint16 parked; /* parked */
	} _;
	gint64 as_gint64;
} ThreadPoolWorkerCounter
#ifdef __GNUC__
__attribute__((aligned(64)))
#endif
;

typedef struct {
	MonoRefCount ref;

	MonoThreadPoolWorkerCallback callback;

	ThreadPoolWorkerCounter counters;

	MonoCoopMutex parked_threads_lock;
	gint32 parked_threads_count;
	MonoCoopCond parked_threads_cond;

	volatile gint32 work_items_count;

	guint32 worker_creation_current_second;
	guint32 worker_creation_current_count;
	MonoCoopMutex worker_creation_lock;

	gint32 heuristic_completions;
	gint64 heuristic_sample_start;
	gint64 heuristic_last_dequeue; // ms
	gint64 heuristic_last_adjustment; // ms
	gint64 heuristic_adjustment_interval; // ms
	ThreadPoolHillClimbing heuristic_hill_climbing;
	MonoCoopMutex heuristic_lock;

	gint32 limit_worker_min;
	gint32 limit_worker_max;

	MonoCpuUsageState *cpu_usage_state;
	gint32 cpu_usage;

	/* suspended by the debugger */
	gboolean suspended;

	gint32 monitor_status;
} ThreadPoolWorker;

enum {
	MONITOR_STATUS_REQUESTED,
	MONITOR_STATUS_WAITING_FOR_REQUEST,
	MONITOR_STATUS_NOT_RUNNING,
};

static ThreadPoolWorker worker;

#define COUNTER_CHECK(counter) \
	do { \
		g_assert (counter._.max_working > 0); \
		g_assert (counter._.starting >= 0); \
		g_assert (counter._.working >= 0); \
	} while (0)

#define COUNTER_ATOMIC(var,block) \
	do { \
		ThreadPoolWorkerCounter __old; \
		do { \
			__old = COUNTER_READ (); \
			(var) = __old; \
			{ block; } \
			COUNTER_CHECK (var); \
		} while (mono_atomic_cas_i64 (&worker.counters.as_gint64, (var).as_gint64, __old.as_gint64) != __old.as_gint64); \
	} while (0)

static inline ThreadPoolWorkerCounter
COUNTER_READ (void)
{
	ThreadPoolWorkerCounter counter;
	counter.as_gint64 = mono_atomic_load_i64 (&worker.counters.as_gint64);
	return counter;
}

static gpointer
rand_create (void)
{
	mono_rand_open ();
	return mono_rand_init (NULL, 0);
}

static guint32
rand_next (gpointer *handle, guint32 min, guint32 max)
{
	MonoError error;
	guint32 val;
	mono_rand_try_get_uint32 (handle, &val, min, max, &error);
	// FIXME handle error
	mono_error_assert_ok (&error);
	return val;
}

static void
destroy (gpointer data)
{
	mono_coop_mutex_destroy (&worker.parked_threads_lock);
	mono_coop_cond_destroy (&worker.parked_threads_cond);

	mono_coop_mutex_destroy (&worker.worker_creation_lock);

	mono_coop_mutex_destroy (&worker.heuristic_lock);

	g_free (worker.cpu_usage_state);
}

void
mono_threadpool_worker_init (MonoThreadPoolWorkerCallback callback)
{
	ThreadPoolHillClimbing *hc;
	const char *threads_per_cpu_env;
	gint threads_per_cpu;
	gint threads_count;

	mono_refcount_init (&worker, destroy);

	worker.callback = callback;

	mono_coop_mutex_init (&worker.parked_threads_lock);
	worker.parked_threads_count = 0;
	mono_coop_cond_init (&worker.parked_threads_cond);

	worker.worker_creation_current_second = -1;
	mono_coop_mutex_init (&worker.worker_creation_lock);

	worker.heuristic_adjustment_interval = 10;
	mono_coop_mutex_init (&worker.heuristic_lock);

	mono_rand_open ();

	hc = &worker.heuristic_hill_climbing;

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

	worker.limit_worker_min = threads_count;

#if defined (HOST_ANDROID) || defined (HOST_IOS)
	worker.limit_worker_max = CLAMP (threads_count * 100, MIN (threads_count, 200), MAX (threads_count, 200));
#else
	worker.limit_worker_max = threads_count * 100;
#endif

	worker.counters._.max_working = worker.limit_worker_min;

	worker.cpu_usage_state = g_new0 (MonoCpuUsageState, 1);

	worker.suspended = FALSE;

	worker.monitor_status = MONITOR_STATUS_NOT_RUNNING;
}

void
mono_threadpool_worker_cleanup (void)
{
	mono_refcount_dec (&worker);
}

static void
work_item_push (void)
{
	gint32 old, new;

	do {
		old = mono_atomic_load_i32 (&worker.work_items_count);
		g_assert (old >= 0);

		new = old + 1;
	} while (mono_atomic_cas_i32 (&worker.work_items_count, new, old) != old);
}

static gboolean
work_item_try_pop (void)
{
	gint32 old, new;

	do {
		old = mono_atomic_load_i32 (&worker.work_items_count);
		g_assert (old >= 0);

		if (old == 0)
			return FALSE;

		new = old - 1;
	} while (mono_atomic_cas_i32 (&worker.work_items_count, new, old) != old);

	return TRUE;
}

static gint32
work_item_count (void)
{
	return mono_atomic_load_i32 (&worker.work_items_count);
}

static void worker_request (void);

void
mono_threadpool_worker_request (void)
{
	if (!mono_refcount_tryinc (&worker))
		return;

	work_item_push ();

	worker_request ();

	mono_refcount_dec (&worker);
}

static void
worker_wait_interrupt (gpointer unused)
{
	/* If the runtime is not shutting down, we are not using this mechanism to wake up a unparked thread, and if the
	 * runtime is shutting down, then we need to wake up ALL the threads.
	 * It might be a bit wasteful, but I witnessed shutdown hang where the main thread would abort and then wait for all
	 * background threads to exit (see mono_thread_manage). This would go wrong because not all threadpool threads would
	 * be unparked. It would end up getting unstucked because of the timeout, but that would delay shutdown by 5-60s. */
	if (!mono_runtime_is_shutting_down ())
		return;

	if (!mono_refcount_tryinc (&worker))
		return;

	mono_coop_mutex_lock (&worker.parked_threads_lock);
	mono_coop_cond_broadcast (&worker.parked_threads_cond);
	mono_coop_mutex_unlock (&worker.parked_threads_lock);

	mono_refcount_dec (&worker);
}

/* return TRUE if timeout, FALSE otherwise (worker unpark or interrupt) */
static gboolean
worker_park (void)
{
	gboolean timeout = FALSE;
	gboolean interrupted = FALSE;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker parking",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

	mono_coop_mutex_lock (&worker.parked_threads_lock);

	if (!mono_runtime_is_shutting_down ()) {
		static gpointer rand_handle = NULL;
		MonoInternalThread *thread;
		ThreadPoolWorkerCounter counter;

		if (!rand_handle)
			rand_handle = rand_create ();
		g_assert (rand_handle);

		thread = mono_thread_internal_current ();
		g_assert (thread);

		COUNTER_ATOMIC (counter, {
			counter._.working --;
			counter._.parked ++;
		});

		worker.parked_threads_count += 1;

		mono_thread_info_install_interrupt (worker_wait_interrupt, NULL, &interrupted);
		if (interrupted)
			goto done;

		if (mono_coop_cond_timedwait (&worker.parked_threads_cond, &worker.parked_threads_lock, rand_next (&rand_handle, 5 * 1000, 60 * 1000)) != 0)
			timeout = TRUE;

		mono_thread_info_uninstall_interrupt (&interrupted);

done:
		worker.parked_threads_count -= 1;

		COUNTER_ATOMIC (counter, {
			counter._.working ++;
			counter._.parked --;
		});
	}

	mono_coop_mutex_unlock (&worker.parked_threads_lock);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker unparking, timeout? %s interrupted? %s",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), timeout ? "yes" : "no", interrupted ? "yes" : "no");

	return timeout;
}

static gboolean
worker_try_unpark (void)
{
	gboolean res = FALSE;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try unpark worker",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

	mono_coop_mutex_lock (&worker.parked_threads_lock);
	if (worker.parked_threads_count > 0) {
		mono_coop_cond_signal (&worker.parked_threads_cond);
		res = TRUE;
	}
	mono_coop_mutex_unlock (&worker.parked_threads_lock);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try unpark worker, success? %s",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), res ? "yes" : "no");

	return res;
}

static gsize WINAPI
worker_thread (gpointer unused)
{
	MonoInternalThread *thread;
	ThreadPoolWorkerCounter counter;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker starting",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

	if (!mono_refcount_tryinc (&worker))
		return 0;

	COUNTER_ATOMIC (counter, {
		counter._.starting --;
		counter._.working ++;
	});

	thread = mono_thread_internal_current ();
	g_assert (thread);

	while (!mono_runtime_is_shutting_down ()) {
		if (mono_thread_interruption_checkpoint ())
			continue;

		if (!work_item_try_pop ()) {
			gboolean timeout;

			timeout = worker_park ();
			if (timeout)
				break;

			continue;
		}

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker executing",
			GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

		worker.callback ();
	}

	COUNTER_ATOMIC (counter, {
		counter._.working --;
	});

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] worker finishing",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

	mono_refcount_dec (&worker);

	return 0;
}

static gboolean
worker_try_create (void)
{
	MonoError error;
	MonoInternalThread *thread;
	gint64 current_ticks;
	gint32 now;
	ThreadPoolWorkerCounter counter;

	if (mono_runtime_is_shutting_down ())
		return FALSE;

	mono_coop_mutex_lock (&worker.worker_creation_lock);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

	current_ticks = mono_100ns_ticks ();
	if (0 == current_ticks) {
		g_warning ("failed to get 100ns ticks");
	} else {
		now = current_ticks / (10 * 1000 * 1000);
		if (worker.worker_creation_current_second != now) {
			worker.worker_creation_current_second = now;
			worker.worker_creation_current_count = 0;
		} else {
			g_assert (worker.worker_creation_current_count <= WORKER_CREATION_MAX_PER_SEC);
			if (worker.worker_creation_current_count == WORKER_CREATION_MAX_PER_SEC) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, failed: maximum number of worker created per second reached, current count = %d",
					GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), worker.worker_creation_current_count);
				mono_coop_mutex_unlock (&worker.worker_creation_lock);
				return FALSE;
			}
		}
	}

	COUNTER_ATOMIC (counter, {
		if (counter._.working >= counter._.max_working) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, failed: maximum number of working threads reached",
				GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));
			mono_coop_mutex_unlock (&worker.worker_creation_lock);
			return FALSE;
		}
		counter._.starting ++;
	});

	thread = mono_thread_create_internal (mono_get_root_domain (), worker_thread, NULL, MONO_THREAD_CREATE_FLAGS_THREADPOOL, &error);
	if (!thread) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, failed: could not create thread due to %s",
			GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), mono_error_get_message (&error));
		mono_error_cleanup (&error);

		COUNTER_ATOMIC (counter, {
			counter._.starting --;
		});

		mono_coop_mutex_unlock (&worker.worker_creation_lock);

		return FALSE;
	}

	worker.worker_creation_current_count += 1;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] try create worker, created %p, now = %d count = %d",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), (gpointer) thread->tid, now, worker.worker_creation_current_count);

	mono_coop_mutex_unlock (&worker.worker_creation_lock);
	return TRUE;
}

static void monitor_ensure_running (void);

static void
worker_request (void)
{
	if (worker.suspended)
		return;

	monitor_ensure_running ();

	if (worker_try_unpark ()) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] request worker, unparked",
			GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));
		return;
	}

	if (worker_try_create ()) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] request worker, created",
			GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));
		return;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] request worker, failed",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));
}

static gboolean
monitor_should_keep_running (void)
{
	static gint64 last_should_keep_running = -1;

	g_assert (worker.monitor_status == MONITOR_STATUS_WAITING_FOR_REQUEST || worker.monitor_status == MONITOR_STATUS_REQUESTED);

	if (mono_atomic_xchg_i32 (&worker.monitor_status, MONITOR_STATUS_WAITING_FOR_REQUEST) == MONITOR_STATUS_WAITING_FOR_REQUEST) {
		gboolean should_keep_running = TRUE, force_should_keep_running = FALSE;

		if (mono_runtime_is_shutting_down ()) {
			should_keep_running = FALSE;
		} else {
			if (work_item_count () == 0)
				should_keep_running = FALSE;

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
			if (mono_atomic_cas_i32 (&worker.monitor_status, MONITOR_STATUS_NOT_RUNNING, MONITOR_STATUS_WAITING_FOR_REQUEST) == MONITOR_STATUS_WAITING_FOR_REQUEST)
				return FALSE;
		}
	}

	g_assert (worker.monitor_status == MONITOR_STATUS_WAITING_FOR_REQUEST || worker.monitor_status == MONITOR_STATUS_REQUESTED);

	return TRUE;
}

static gboolean
monitor_sufficient_delay_since_last_dequeue (void)
{
	gint64 threshold;

	if (worker.cpu_usage < CPU_USAGE_LOW) {
		threshold = MONITOR_INTERVAL;
	} else {
		ThreadPoolWorkerCounter counter;
		counter = COUNTER_READ ();
		threshold = counter._.max_working * MONITOR_INTERVAL * 2;
	}

	return mono_msec_ticks () >= worker.heuristic_last_dequeue + threshold;
}

static void hill_climbing_force_change (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition);

static gsize WINAPI
monitor_thread (gpointer unused)
{
	MonoInternalThread *internal;
	guint i;

	if (!mono_refcount_tryinc (&worker))
		return 0;

	internal = mono_thread_internal_current ();
	g_assert (internal);

	mono_cpu_usage (worker.cpu_usage_state);

	// printf ("monitor_thread: start\n");

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, started",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

	do {
		ThreadPoolWorkerCounter counter;
		gboolean limit_worker_max_reached;
		gint32 interval_left = MONITOR_INTERVAL;
		gint32 awake = 0; /* number of spurious awakes we tolerate before doing a round of rebalancing */

		g_assert (worker.monitor_status != MONITOR_STATUS_NOT_RUNNING);

		// counter = COUNTER_READ ();
		// printf ("monitor_thread: starting = %d working = %d parked = %d max_working = %d\n",
		// 	counter._.starting, counter._.working, counter._.parked, counter._.max_working);

		do {
			gint64 ts;
			gboolean alerted = FALSE;

			if (mono_runtime_is_shutting_down ())
				break;

			ts = mono_msec_ticks ();
			if (mono_thread_info_sleep (interval_left, &alerted) == 0)
				break;
			interval_left -= mono_msec_ticks () - ts;

			mono_thread_interruption_checkpoint ();
		} while (interval_left > 0 && ++awake < 10);

		if (mono_runtime_is_shutting_down ())
			continue;

		if (worker.suspended)
			continue;

		if (work_item_count () == 0)
			continue;

		worker.cpu_usage = mono_cpu_usage (worker.cpu_usage_state);

		if (!monitor_sufficient_delay_since_last_dequeue ())
			continue;

		limit_worker_max_reached = FALSE;

		COUNTER_ATOMIC (counter, {
			if (counter._.max_working >= worker.limit_worker_max) {
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
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, unparked",
					GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));
				break;
			}

			if (worker_try_create ()) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, created",
					GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));
				break;
			}
		}
	} while (monitor_should_keep_running ());

	// printf ("monitor_thread: stop\n");

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] monitor thread, finished",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())));

	mono_refcount_dec (&worker);
	return 0;
}

static void
monitor_ensure_running (void)
{
	MonoError error;
	for (;;) {
		switch (worker.monitor_status) {
		case MONITOR_STATUS_REQUESTED:
			// printf ("monitor_thread: requested\n");
			return;
		case MONITOR_STATUS_WAITING_FOR_REQUEST:
			// printf ("monitor_thread: waiting for request\n");
			mono_atomic_cas_i32 (&worker.monitor_status, MONITOR_STATUS_REQUESTED, MONITOR_STATUS_WAITING_FOR_REQUEST);
			break;
		case MONITOR_STATUS_NOT_RUNNING:
			// printf ("monitor_thread: not running\n");
			if (mono_runtime_is_shutting_down ())
				return;
			if (mono_atomic_cas_i32 (&worker.monitor_status, MONITOR_STATUS_REQUESTED, MONITOR_STATUS_NOT_RUNNING) == MONITOR_STATUS_NOT_RUNNING) {
				// printf ("monitor_thread: creating\n");
				if (!mono_thread_create_internal (mono_get_root_domain (), monitor_thread, NULL, MONO_THREAD_CREATE_FLAGS_THREADPOOL | MONO_THREAD_CREATE_FLAGS_SMALL_STACK, &error)) {
					// printf ("monitor_thread: creating failed\n");
					worker.monitor_status = MONITOR_STATUS_NOT_RUNNING;
					mono_error_cleanup (&error);
					mono_refcount_dec (&worker);
				}
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

	hc = &worker.heuristic_hill_climbing;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_THREADPOOL, "[%p] hill climbing, change max number of threads %d",
		GUINT_TO_POINTER (MONO_NATIVE_THREAD_ID_TO_UINT (mono_native_thread_id_get ())), new_thread_count);

	hc->last_thread_count = new_thread_count;
	hc->current_sample_interval = rand_next (&hc->random_interval_generator, hc->sample_interval_low, hc->sample_interval_high);
	hc->elapsed_since_last_change = 0;
	hc->completions_since_last_change = 0;
}

static void
hill_climbing_force_change (gint16 new_thread_count, ThreadPoolHeuristicStateTransition transition)
{
	ThreadPoolHillClimbing *hc;

	hc = &worker.heuristic_hill_climbing;

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

	g_assert (sample_count >= period);
	g_assert (period >= 2);

	hc = &worker.heuristic_hill_climbing;

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
hill_climbing_update (gint16 current_thread_count, guint32 sample_duration, gint32 completions, gint64 *adjustment_interval)
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

	g_assert (adjustment_interval);

	hc = &worker.heuristic_hill_climbing;

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
		 * far, and tell the ThreadPoolWorker to collect a little more. */
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
	if (move > 0.0 && worker.cpu_usage > CPU_USAGE_HIGH)
		move = 0.0;

	/* Apply the move to our control setting. */
	hc->current_control_setting += move;

	/* Calculate the new thread wave magnitude, which is based on the moving average we've been keeping of the
	 * throughput error.  This average starts at zero, so we'll start with a nice safe little wave at first. */
	new_thread_wave_magnitude = (gint)(0.5 + (hc->current_control_setting * hc->average_throughput_noise
		* hc->target_signal_to_noise_ratio * hc->thread_magnitude_multiplier * 2.0));
	new_thread_wave_magnitude = CLAMP (new_thread_wave_magnitude, 1, hc->max_thread_wave_magnitude);

	/* Make sure our control setting is within the ThreadPoolWorker's limits. */
	hc->current_control_setting = CLAMP (hc->current_control_setting, worker.limit_worker_min, worker.limit_worker_max - new_thread_wave_magnitude);

	/* Calculate the new thread count (control setting + square wave). */
	new_thread_count = (gint)(hc->current_control_setting + new_thread_wave_magnitude * ((hc->total_samples / (hc->wave_period / 2)) % 2));

	/* Make sure the new thread count doesn't exceed the ThreadPoolWorker's limits. */
	new_thread_count = CLAMP (new_thread_count, worker.limit_worker_min, worker.limit_worker_max);

	if (new_thread_count != current_thread_count)
		hill_climbing_change_thread_count (new_thread_count, transition);

	if (creal (ratio) < 0.0 && new_thread_count == worker.limit_worker_min)
		*adjustment_interval = (gint)(0.5 + hc->current_sample_interval * (10.0 * MAX (-1.0 * creal (ratio), 1.0)));
	else
		*adjustment_interval = hc->current_sample_interval;

	return new_thread_count;
}

static gboolean
heuristic_should_adjust (void)
{
	if (worker.heuristic_last_dequeue > worker.heuristic_last_adjustment + worker.heuristic_adjustment_interval) {
		ThreadPoolWorkerCounter counter;
		counter = COUNTER_READ ();
		if (counter._.working <= counter._.max_working)
			return TRUE;
	}

	return FALSE;
}

static void
heuristic_adjust (void)
{
	if (mono_coop_mutex_trylock (&worker.heuristic_lock) == 0) {
		gint32 completions = mono_atomic_xchg_i32 (&worker.heuristic_completions, 0);
		gint64 sample_end = mono_msec_ticks ();
		gint64 sample_duration = sample_end - worker.heuristic_sample_start;

		if (sample_duration >= worker.heuristic_adjustment_interval / 2) {
			ThreadPoolWorkerCounter counter;
			gint16 new_thread_count;

			counter = COUNTER_READ ();
			new_thread_count = hill_climbing_update (counter._.max_working, sample_duration, completions, &worker.heuristic_adjustment_interval);

			COUNTER_ATOMIC (counter, {
				counter._.max_working = new_thread_count;
			});

			if (new_thread_count > counter._.max_working)
				worker_request ();

			worker.heuristic_sample_start = sample_end;
			worker.heuristic_last_adjustment = mono_msec_ticks ();
		}

		mono_coop_mutex_unlock (&worker.heuristic_lock);
	}
}

static void
heuristic_notify_work_completed (void)
{
	mono_atomic_inc_i32 (&worker.heuristic_completions);
	worker.heuristic_last_dequeue = mono_msec_ticks ();

	if (heuristic_should_adjust ())
		heuristic_adjust ();
}

gboolean
mono_threadpool_worker_notify_completed (void)
{
	ThreadPoolWorkerCounter counter;

	heuristic_notify_work_completed ();

	counter = COUNTER_READ ();
	return counter._.working <= counter._.max_working;
}

gint32
mono_threadpool_worker_get_min (void)
{
	gint32 ret;

	if (!mono_refcount_tryinc (&worker))
		return 0;

	ret = worker.limit_worker_min;

	mono_refcount_dec (&worker);
	return ret;
}

gboolean
mono_threadpool_worker_set_min (gint32 value)
{
	if (value <= 0 || value > worker.limit_worker_max)
		return FALSE;

	if (!mono_refcount_tryinc (&worker))
		return FALSE;

	worker.limit_worker_min = value;

	mono_refcount_dec (&worker);
	return TRUE;
}

gint32
mono_threadpool_worker_get_max (void)
{
	gint32 ret;

	if (!mono_refcount_tryinc (&worker))
		return 0;

	ret = worker.limit_worker_max;

	mono_refcount_dec (&worker);
	return ret;
}

gboolean
mono_threadpool_worker_set_max (gint32 value)
{
	gint32 cpu_count;

	cpu_count = mono_cpu_count ();
	if (value < worker.limit_worker_min || value < cpu_count)
		return FALSE;

	if (!mono_refcount_tryinc (&worker))
		return FALSE;

	worker.limit_worker_max = value;

	mono_refcount_dec (&worker);
	return TRUE;
}

void
mono_threadpool_worker_set_suspended (gboolean suspended)
{
	if (!mono_refcount_tryinc (&worker))
		return;

	worker.suspended = suspended;
	if (!suspended)
		worker_request ();

	mono_refcount_dec (&worker);
}
