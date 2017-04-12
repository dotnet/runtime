/**
 * \file
 * Worker threads for parallel and concurrent GC.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-workers.h"
#include "mono/sgen/sgen-thread-pool.h"
#include "mono/utils/mono-membar.h"
#include "mono/sgen/sgen-client.h"

static int workers_num;
static int active_workers_num;
static volatile gboolean started;
static volatile gboolean forced_stop;
static WorkerData *workers_data;
static SgenWorkerCallback worker_init_cb;

static SgenThreadPool pool_inst;
static SgenThreadPool *pool; /* null if we're not using workers */

/*
 * When using multiple workers, we need to have the last worker
 * enqueue the preclean jobs (if there are any). This lock ensures
 * that when the last worker takes it, all the other workers have
 * gracefully finished, so it can restart them.
 */
static mono_mutex_t finished_lock;
static volatile gboolean workers_finished;
static int worker_awakenings;

static SgenSectionGrayQueue workers_distribute_gray_queue;
static gboolean workers_distribute_gray_queue_inited;

/*
 * Allowed transitions:
 *
 * | from \ to          | NOT WORKING | WORKING | WORK ENQUEUED |
 * |--------------------+-------------+---------+---------------+
 * | NOT WORKING        | -           | -       | main / worker |
 * | WORKING            | worker      | -       | main / worker |
 * | WORK ENQUEUED      | -           | worker  | -             |
 *
 * The WORK ENQUEUED state guarantees that the worker thread will inspect the queue again at
 * least once.  Only after looking at the queue will it go back to WORKING, and then,
 * eventually, to NOT WORKING.  After enqueuing work the main thread transitions the state
 * to WORK ENQUEUED.  Signalling the worker thread to wake up is only necessary if the old
 * state was NOT WORKING.
 */

enum {
	STATE_NOT_WORKING,
	STATE_WORKING,
	STATE_WORK_ENQUEUED
};

#define SGEN_WORKER_MIN_SECTIONS_SIGNAL 4

typedef gint32 State;

static SgenObjectOperations * volatile idle_func_object_ops;
static SgenObjectOperations *idle_func_object_ops_par, *idle_func_object_ops_nopar;
/*
 * finished_callback is called only when the workers finish work normally (when they
 * are not forced to finish). The callback is used to enqueue preclean jobs.
 */
static volatile SgenWorkersFinishCallback finish_callback;

static guint64 stat_workers_num_finished;

static gboolean
set_state (WorkerData *data, State old_state, State new_state)
{
	SGEN_ASSERT (0, old_state != new_state, "Why are we transitioning to the same state?");
	if (new_state == STATE_NOT_WORKING)
		SGEN_ASSERT (0, old_state == STATE_WORKING, "We can only transition to NOT WORKING from WORKING");
	else if (new_state == STATE_WORKING)
		SGEN_ASSERT (0, old_state == STATE_WORK_ENQUEUED, "We can only transition to WORKING from WORK ENQUEUED");
	if (new_state == STATE_NOT_WORKING || new_state == STATE_WORKING)
		SGEN_ASSERT (6, sgen_thread_pool_is_thread_pool_thread (pool, mono_native_thread_id_get ()), "Only the worker thread is allowed to transition to NOT_WORKING or WORKING");

	return InterlockedCompareExchange (&data->state, new_state, old_state) == old_state;
}

static gboolean
state_is_working_or_enqueued (State state)
{
	return state == STATE_WORKING || state == STATE_WORK_ENQUEUED;
}

static void
sgen_workers_ensure_awake (void)
{
	int i;
	gboolean need_signal = FALSE;

	/*
	 * All workers are awaken, make sure we reset the parallel context.
	 * We call this function only when starting the workers so nobody is running,
	 * or when the last worker is enqueuing preclean work. In both cases we can't
	 * have a worker working using a nopar context, which means it is safe.
	 */
	idle_func_object_ops = (active_workers_num > 1) ? idle_func_object_ops_par : idle_func_object_ops_nopar;
	workers_finished = FALSE;

	for (i = 0; i < active_workers_num; i++) {
		State old_state;
		gboolean did_set_state;

		do {
			old_state = workers_data [i].state;

			if (old_state == STATE_WORK_ENQUEUED)
				break;

			did_set_state = set_state (&workers_data [i], old_state, STATE_WORK_ENQUEUED);
		} while (!did_set_state);

		if (!state_is_working_or_enqueued (old_state))
			need_signal = TRUE;
	}

	if (need_signal)
		sgen_thread_pool_idle_signal (pool);
}

static void
worker_try_finish (WorkerData *data)
{
	State old_state;
	int i, working = 0;

	++stat_workers_num_finished;

	mono_os_mutex_lock (&finished_lock);

	for (i = 0; i < active_workers_num; i++) {
		if (state_is_working_or_enqueued (workers_data [i].state))
			working++;
	}

	if (working == 1) {
		SgenWorkersFinishCallback callback = finish_callback;
		SGEN_ASSERT (0, idle_func_object_ops == idle_func_object_ops_nopar, "Why are we finishing with parallel context");
		/* We are the last one left. Enqueue preclean job if we have one and awake everybody */
		SGEN_ASSERT (0, data->state != STATE_NOT_WORKING, "How did we get from doing idle work to NOT WORKING without setting it ourselves?");
		if (callback) {
			finish_callback = NULL;
			callback ();
			worker_awakenings = 0;
			/* Make sure each worker has a chance of seeing the enqueued jobs */
			sgen_workers_ensure_awake ();
			SGEN_ASSERT (0, data->state == STATE_WORK_ENQUEUED, "Why did we fail to set our own state to ENQUEUED");
			goto work_available;
		}
	}

	do {
		old_state = data->state;

		SGEN_ASSERT (0, old_state != STATE_NOT_WORKING, "How did we get from doing idle work to NOT WORKING without setting it ourselves?");
		if (old_state == STATE_WORK_ENQUEUED)
			goto work_available;
		SGEN_ASSERT (0, old_state == STATE_WORKING, "What other possibility is there?");
	} while (!set_state (data, old_state, STATE_NOT_WORKING));

	/*
	 * If we are second to last to finish, we set the scan context to the non-parallel
	 * version so we can speed up the last worker. This helps us maintain same level
	 * of performance as non-parallel mode even if we fail to distribute work properly.
	 */
	if (working == 2)
		idle_func_object_ops = idle_func_object_ops_nopar;

	workers_finished = TRUE;
	mono_os_mutex_unlock (&finished_lock);

	binary_protocol_worker_finish (sgen_timestamp (), forced_stop);

	sgen_gray_object_queue_trim_free_list (&data->private_gray_queue);
	return;

work_available:
	mono_os_mutex_unlock (&finished_lock);
}

void
sgen_workers_enqueue_job (SgenThreadPoolJob *job, gboolean enqueue)
{
	if (!enqueue) {
		job->func (NULL, job);
		sgen_thread_pool_job_free (job);
		return;
	}

	sgen_thread_pool_job_enqueue (pool, job);
}

static gboolean
workers_get_work (WorkerData *data)
{
	SgenMajorCollector *major = sgen_get_major_collector ();
	SgenMinorCollector *minor = sgen_get_minor_collector ();
	GrayQueueSection *section;

	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));
	g_assert (major->is_concurrent || minor->is_parallel);

	section = sgen_section_gray_queue_dequeue (&workers_distribute_gray_queue);
	if (section) {
		sgen_gray_object_enqueue_section (&data->private_gray_queue, section, major->is_parallel);
		return TRUE;
	}

	/* Nobody to steal from */
	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));
	return FALSE;
}

static gboolean
workers_steal_work (WorkerData *data)
{
	SgenMajorCollector *major = sgen_get_major_collector ();
	SgenMinorCollector *minor = sgen_get_minor_collector ();
	int generation = sgen_get_current_collection_generation ();
	GrayQueueSection *section = NULL;
	int i, current_worker;

	if ((generation == GENERATION_OLD && !major->is_parallel) ||
			(generation == GENERATION_NURSERY && !minor->is_parallel))
		return FALSE;

	/* If we're parallel, steal from other workers' private gray queues  */
	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));

	current_worker = (int) (data - workers_data);

	for (i = 1; i < active_workers_num && !section; i++) {
		int steal_worker = (current_worker + i) % active_workers_num;
		if (state_is_working_or_enqueued (workers_data [steal_worker].state))
			section = sgen_gray_object_steal_section (&workers_data [steal_worker].private_gray_queue);
	}

	if (section) {
		sgen_gray_object_enqueue_section (&data->private_gray_queue, section, TRUE);
		return TRUE;
	}

	/* Nobody to steal from */
	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));
	return FALSE;
}

static void
concurrent_enqueue_check (GCObject *obj)
{
	g_assert (sgen_concurrent_collection_in_progress ());
	g_assert (!sgen_ptr_in_nursery (obj));
	g_assert (SGEN_LOAD_VTABLE (obj));
}

static void
init_private_gray_queue (WorkerData *data)
{
	sgen_gray_object_queue_init (&data->private_gray_queue,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL,
			FALSE);
}

static void
thread_pool_init_func (void *data_untyped)
{
	WorkerData *data = (WorkerData *)data_untyped;
	SgenMajorCollector *major = sgen_get_major_collector ();
	SgenMinorCollector *minor = sgen_get_minor_collector ();

	sgen_client_thread_register_worker ();

	if (!major->is_concurrent && !minor->is_parallel)
		return;

	init_private_gray_queue (data);

	if (worker_init_cb)
		worker_init_cb (data);
}

static gboolean
continue_idle_func (void *data_untyped)
{
	if (data_untyped) {
		WorkerData *data = (WorkerData *)data_untyped;
		return state_is_working_or_enqueued (data->state);
	} else {
		/* Return if any of the threads is working */
		return !sgen_workers_all_done ();
	}
}

static gboolean
should_work_func (void *data_untyped)
{
	WorkerData *data = (WorkerData*)data_untyped;
	int current_worker = (int) (data - workers_data);

	return started && current_worker < active_workers_num;
}

static void
marker_idle_func (void *data_untyped)
{
	WorkerData *data = (WorkerData *)data_untyped;

	SGEN_ASSERT (0, continue_idle_func (data_untyped), "Why are we called when we're not supposed to work?");

	if (data->state == STATE_WORK_ENQUEUED) {
		set_state (data, STATE_WORK_ENQUEUED, STATE_WORKING);
		SGEN_ASSERT (0, data->state != STATE_NOT_WORKING, "How did we get from WORK ENQUEUED to NOT WORKING?");
	}

	if (!forced_stop && (!sgen_gray_object_queue_is_empty (&data->private_gray_queue) || workers_get_work (data) || workers_steal_work (data))) {
		ScanCopyContext ctx = CONTEXT_FROM_OBJECT_OPERATIONS (idle_func_object_ops, &data->private_gray_queue);

		SGEN_ASSERT (0, !sgen_gray_object_queue_is_empty (&data->private_gray_queue), "How is our gray queue empty if we just got work?");

		sgen_drain_gray_stack (ctx);

		if (data->private_gray_queue.num_sections >= SGEN_WORKER_MIN_SECTIONS_SIGNAL
				&& workers_finished && worker_awakenings < active_workers_num) {
			/* We bound the number of worker awakenings just to be sure */
			worker_awakenings++;
			mono_os_mutex_lock (&finished_lock);
			sgen_workers_ensure_awake ();
			mono_os_mutex_unlock (&finished_lock);
		}
	} else {
		worker_try_finish (data);
	}
}

static void
init_distribute_gray_queue (void)
{
	if (workers_distribute_gray_queue_inited) {
		g_assert (sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue));
		g_assert (workers_distribute_gray_queue.locked);
		return;
	}

	sgen_section_gray_queue_init (&workers_distribute_gray_queue, TRUE,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL);
	workers_distribute_gray_queue_inited = TRUE;
}

void
sgen_workers_init_distribute_gray_queue (void)
{
	SGEN_ASSERT (0, sgen_get_major_collector ()->is_concurrent || sgen_get_minor_collector ()->is_parallel,
			"Why should we init the distribute gray queue if we don't need it?");
	init_distribute_gray_queue ();
}

void
sgen_workers_init (int num_workers, SgenWorkerCallback callback)
{
	int i;
	WorkerData **workers_data_ptrs = (WorkerData**)alloca(num_workers * sizeof(WorkerData*));

	mono_os_mutex_init (&finished_lock);
	//g_print ("initing %d workers\n", num_workers);

	workers_num = num_workers;
	active_workers_num = num_workers;

	workers_data = (WorkerData *)sgen_alloc_internal_dynamic (sizeof (WorkerData) * num_workers, INTERNAL_MEM_WORKER_DATA, TRUE);
	memset (workers_data, 0, sizeof (WorkerData) * num_workers);

	init_distribute_gray_queue ();

	for (i = 0; i < num_workers; ++i)
		workers_data_ptrs [i] = &workers_data [i];

	worker_init_cb = callback;

	pool = &pool_inst;
	sgen_thread_pool_init (pool, num_workers, thread_pool_init_func, marker_idle_func, continue_idle_func, should_work_func, (SgenThreadPoolData**)workers_data_ptrs);

	mono_counters_register ("# workers finished", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_num_finished);
}

void
sgen_workers_shutdown (void)
{
	if (pool)
		sgen_thread_pool_shutdown (pool);
}

void
sgen_workers_stop_all_workers (void)
{
	finish_callback = NULL;
	mono_memory_write_barrier ();
	forced_stop = TRUE;

	sgen_thread_pool_wait_for_all_jobs (pool);
	sgen_thread_pool_idle_wait (pool);
	SGEN_ASSERT (0, sgen_workers_all_done (), "Can only signal enqueue work when in no work state");

	started = FALSE;
}

void
sgen_workers_set_num_active_workers (int num_workers)
{
	if (num_workers) {
		SGEN_ASSERT (0, active_workers_num <= workers_num, "We can't start more workers than we initialized");
		active_workers_num = num_workers;
	} else {
		active_workers_num = workers_num;
	}
}

void
sgen_workers_start_all_workers (SgenObjectOperations *object_ops_nopar, SgenObjectOperations *object_ops_par, SgenWorkersFinishCallback callback)
{
	SGEN_ASSERT (0, !started, "Why are we starting to work without finishing previous cycle");

	idle_func_object_ops_par = object_ops_par;
	idle_func_object_ops_nopar = object_ops_nopar;
	forced_stop = FALSE;
	finish_callback = callback;
	worker_awakenings = 0;
	started = TRUE;
	mono_memory_write_barrier ();

	/*
	 * We expect workers to start finishing only after all of them were awaken.
	 * Otherwise we might think that we have fewer workers and use wrong context.
	 */
	mono_os_mutex_lock (&finished_lock);
	sgen_workers_ensure_awake ();
	mono_os_mutex_unlock (&finished_lock);
}

void
sgen_workers_join (void)
{
	int i;

	sgen_thread_pool_wait_for_all_jobs (pool);
	sgen_thread_pool_idle_wait (pool);
	SGEN_ASSERT (0, sgen_workers_all_done (), "Can only signal enqueue work when in no work state");

	/* At this point all the workers have stopped. */

	SGEN_ASSERT (0, sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue), "Why is there still work left to do?");
	for (i = 0; i < active_workers_num; ++i)
		SGEN_ASSERT (0, sgen_gray_object_queue_is_empty (&workers_data [i].private_gray_queue), "Why is there still work left to do?");

	started = FALSE;
}

/*
 * Can only be called if the workers are stopped.
 * If we're stopped, there are also no pending jobs.
 */
gboolean
sgen_workers_have_idle_work (void)
{
	int i;

	SGEN_ASSERT (0, forced_stop && sgen_workers_all_done (), "Checking for idle work should only happen if the workers are stopped.");

	if (!sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue))
		return TRUE;

	for (i = 0; i < active_workers_num; ++i) {
		if (!sgen_gray_object_queue_is_empty (&workers_data [i].private_gray_queue))
			return TRUE;
	}

	return FALSE;
}

gboolean
sgen_workers_all_done (void)
{
	int i;

	for (i = 0; i < active_workers_num; i++) {
		if (state_is_working_or_enqueued (workers_data [i].state))
			return FALSE;
	}
	return TRUE;
}

/* Must only be used for debugging */
gboolean
sgen_workers_are_working (void)
{
	return !sgen_workers_all_done ();
}

void
sgen_workers_assert_gray_queue_is_empty (void)
{
	SGEN_ASSERT (0, sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue), "Why is the workers gray queue not empty?");
}

void
sgen_workers_take_from_queue (SgenGrayQueue *queue)
{
	sgen_gray_object_spread (queue, sgen_workers_get_job_split_count ());

	for (;;) {
		GrayQueueSection *section = sgen_gray_object_dequeue_section (queue);
		if (!section)
			break;
		sgen_section_gray_queue_enqueue (&workers_distribute_gray_queue, section);
	}

	SGEN_ASSERT (0, !sgen_workers_are_working (), "We should fully populate the distribute gray queue before we start the workers");
}

SgenObjectOperations*
sgen_workers_get_idle_func_object_ops (void)
{
	return (idle_func_object_ops_par) ? idle_func_object_ops_par : idle_func_object_ops_nopar;
}

/*
 * If we have a single worker, splitting into multiple jobs makes no sense. With
 * more than one worker, we split into a larger number of jobs so that, in case
 * the work load is uneven, a worker that finished quickly can take up more jobs
 * than another one.
 */
int
sgen_workers_get_job_split_count (void)
{
	return (active_workers_num > 1) ? active_workers_num * 4 : 1;
}

void
sgen_workers_foreach (SgenWorkerCallback callback)
{
	int i;

	for (i = 0; i < workers_num; i++)
		callback (&workers_data [i]);
}

gboolean
sgen_workers_is_worker_thread (MonoNativeThreadId id)
{
	if (!pool)
		return FALSE;
	return sgen_thread_pool_is_thread_pool_thread (pool, id);
}

#endif
