/*
 * sgen-workers.c: Worker threads for parallel and concurrent GC.
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
static volatile gboolean forced_stop;
static WorkerData *workers_data;
static SgenWorkerCallback worker_init_cb;

/*
 * When using multiple workers, we need to have the last worker
 * enqueue the preclean jobs (if there are any). This lock ensures
 * that when the last worker takes it, all the other workers have
 * gracefully finished, so it can restart them.
 */
static mono_mutex_t finished_lock;

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

typedef gint32 State;

static SgenObjectOperations * volatile idle_func_object_ops;
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
		SGEN_ASSERT (6, sgen_thread_pool_is_thread_pool_thread (mono_native_thread_id_get ()), "Only the worker thread is allowed to transition to NOT_WORKING or WORKING");

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

	for (i = 0; i < workers_num; i++) {
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
		sgen_thread_pool_idle_signal ();
}

static void
worker_try_finish (WorkerData *data)
{
	State old_state;
	int i, working = 0;

	++stat_workers_num_finished;

	mono_os_mutex_lock (&finished_lock);

	for (i = 0; i < workers_num; i++) {
		if (state_is_working_or_enqueued (workers_data [i].state))
			working++;
	}

	if (working == 1) {
		SgenWorkersFinishCallback callback = finish_callback;
		/* We are the last one left. Enqueue preclean job if we have one and awake everybody */
		SGEN_ASSERT (0, data->state != STATE_NOT_WORKING, "How did we get from doing idle work to NOT WORKING without setting it ourselves?");
		if (callback) {
			finish_callback = NULL;
			callback ();
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

	sgen_thread_pool_job_enqueue (job);
}

void
sgen_workers_wait_for_jobs_finished (void)
{
	sgen_thread_pool_wait_for_all_jobs ();
	/*
	 * If the idle task was never triggered or it finished before the last job did and
	 * then didn't get triggered again, we might end up in the situation of having
	 * something in the gray queue yet the idle task not working.  The easiest way to
	 * make sure this doesn't stay that way is to just trigger it again after all jobs
	 * have finished.
	 */
	sgen_workers_ensure_awake ();
}

static gboolean
workers_get_work (WorkerData *data)
{
	SgenMajorCollector *major;

	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));

	/* If we're concurrent, steal from the workers distribute gray queue. */
	major = sgen_get_major_collector ();
	if (major->is_concurrent) {
		GrayQueueSection *section = sgen_section_gray_queue_dequeue (&workers_distribute_gray_queue);
		if (section) {
			sgen_gray_object_enqueue_section (&data->private_gray_queue, section);
			return TRUE;
		}
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

	sgen_client_thread_register_worker ();

	if (!major->is_concurrent)
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

static void
marker_idle_func (void *data_untyped)
{
	WorkerData *data = (WorkerData *)data_untyped;

	SGEN_ASSERT (0, continue_idle_func (data_untyped), "Why are we called when we're not supposed to work?");
	SGEN_ASSERT (0, sgen_concurrent_collection_in_progress (), "The worker should only mark in concurrent collections.");

	if (data->state == STATE_WORK_ENQUEUED) {
		set_state (data, STATE_WORK_ENQUEUED, STATE_WORKING);
		SGEN_ASSERT (0, data->state != STATE_NOT_WORKING, "How did we get from WORK ENQUEUED to NOT WORKING?");
	}

	if (!forced_stop && (!sgen_gray_object_queue_is_empty (&data->private_gray_queue) || workers_get_work (data))) {
		ScanCopyContext ctx = CONTEXT_FROM_OBJECT_OPERATIONS (idle_func_object_ops, &data->private_gray_queue);

		SGEN_ASSERT (0, !sgen_gray_object_queue_is_empty (&data->private_gray_queue), "How is our gray queue empty if we just got work?");

		sgen_drain_gray_stack (ctx);
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
	SGEN_ASSERT (0, sgen_get_major_collector ()->is_concurrent,
			"Why should we init the distribute gray queue if we don't need it?");
	init_distribute_gray_queue ();
}

void
sgen_workers_init (int num_workers, SgenWorkerCallback callback)
{
	int i;
	void **workers_data_ptrs = (void **)alloca(num_workers * sizeof(void *));

	if (!sgen_get_major_collector ()->is_concurrent) {
		sgen_thread_pool_init (num_workers, thread_pool_init_func, NULL, NULL, NULL);
		return;
	}

	mono_os_mutex_init (&finished_lock);
	//g_print ("initing %d workers\n", num_workers);

	workers_num = num_workers;

	workers_data = (WorkerData *)sgen_alloc_internal_dynamic (sizeof (WorkerData) * num_workers, INTERNAL_MEM_WORKER_DATA, TRUE);
	memset (workers_data, 0, sizeof (WorkerData) * num_workers);

	init_distribute_gray_queue ();

	for (i = 0; i < num_workers; ++i)
		workers_data_ptrs [i] = (void *) &workers_data [i];

	worker_init_cb = callback;

	sgen_thread_pool_init (num_workers, thread_pool_init_func, marker_idle_func, continue_idle_func, workers_data_ptrs);

	mono_counters_register ("# workers finished", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_num_finished);
}

void
sgen_workers_stop_all_workers (void)
{
	finish_callback = NULL;
	mono_memory_write_barrier ();
	forced_stop = TRUE;

	sgen_thread_pool_wait_for_all_jobs ();
	sgen_thread_pool_idle_wait ();
	SGEN_ASSERT (0, sgen_workers_all_done (), "Can only signal enqueue work when in no work state");
}

void
sgen_workers_start_all_workers (SgenObjectOperations *object_ops, SgenWorkersFinishCallback callback)
{
	forced_stop = FALSE;
	idle_func_object_ops = object_ops;
	finish_callback = callback;
	mono_memory_write_barrier ();

	sgen_workers_ensure_awake ();
}

void
sgen_workers_join (void)
{
	int i;

	sgen_thread_pool_wait_for_all_jobs ();
	sgen_thread_pool_idle_wait ();
	SGEN_ASSERT (0, sgen_workers_all_done (), "Can only signal enqueue work when in no work state");

	/* At this point all the workers have stopped. */

	SGEN_ASSERT (0, sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue), "Why is there still work left to do?");
	for (i = 0; i < workers_num; ++i)
		SGEN_ASSERT (0, sgen_gray_object_queue_is_empty (&workers_data [i].private_gray_queue), "Why is there still work left to do?");
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

	for (i = 0; i < workers_num; ++i) {
		if (!sgen_gray_object_queue_is_empty (&workers_data [i].private_gray_queue))
			return TRUE;
	}

	return FALSE;
}

gboolean
sgen_workers_all_done (void)
{
	int i;

	for (i = 0; i < workers_num; i++) {
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
sgen_workers_take_from_queue_and_awake (SgenGrayQueue *queue)
{
	gboolean wake = FALSE;

	for (;;) {
		GrayQueueSection *section = sgen_gray_object_dequeue_section (queue);
		if (!section)
			break;
		sgen_section_gray_queue_enqueue (&workers_distribute_gray_queue, section);
		wake = TRUE;
	}

	if (wake) {
		SGEN_ASSERT (0, sgen_concurrent_collection_in_progress (), "Why is there work to take when there's no concurrent collection in progress?");
		sgen_workers_ensure_awake ();
	}
}

SgenObjectOperations*
sgen_workers_get_idle_func_object_ops (void)
{
	return idle_func_object_ops;
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
	return (workers_num > 1) ? workers_num * 4 : 1;
}

void
sgen_workers_foreach (SgenWorkerCallback callback)
{
	int i;

	for (i = 0; i < workers_num; i++)
		callback (&workers_data [i]);
}

#endif
