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

#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC

static WorkerContext worker_contexts [GENERATION_MAX];

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

static guint64 stat_workers_num_finished;

static gboolean
set_state (WorkerData *data, State old_state, State new_state)
{
	SGEN_ASSERT (0, old_state != new_state, "Why are we transitioning to the same state?");
	if (new_state == STATE_NOT_WORKING)
		SGEN_ASSERT (0, old_state == STATE_WORKING, "We can only transition to NOT WORKING from WORKING");
	else if (new_state == STATE_WORKING)
		SGEN_ASSERT (0, old_state == STATE_WORK_ENQUEUED, "We can only transition to WORKING from WORK ENQUEUED");

	return mono_atomic_cas_i32 (&data->state, new_state, old_state) == old_state;
}

static gboolean
state_is_working_or_enqueued (State state)
{
	return state == STATE_WORKING || state == STATE_WORK_ENQUEUED;
}

static void
sgen_workers_ensure_awake (WorkerContext *context)
{
	int i;
	gboolean need_signal = FALSE;

	/*
	 * All workers are awaken, make sure we reset the parallel context.
	 * We call this function only when starting the workers so nobody is running,
	 * or when the last worker is enqueuing preclean work. In both cases we can't
	 * have a worker working using a nopar context, which means it is safe.
	 */
	context->idle_func_object_ops = (context->active_workers_num > 1) ? context->idle_func_object_ops_par : context->idle_func_object_ops_nopar;
	context->workers_finished = FALSE;

	for (i = 0; i < context->active_workers_num; i++) {
		State old_state;
		gboolean did_set_state;

		do {
			old_state = context->workers_data [i].state;

			if (old_state == STATE_WORK_ENQUEUED)
				break;

			did_set_state = set_state (&context->workers_data [i], old_state, STATE_WORK_ENQUEUED);

			if (did_set_state && old_state == STATE_NOT_WORKING)
				context->workers_data [i].last_start = sgen_timestamp ();
		} while (!did_set_state);

		if (!state_is_working_or_enqueued (old_state))
			need_signal = TRUE;
	}

	if (need_signal)
		sgen_thread_pool_idle_signal (context->thread_pool_context);
}

static void
worker_try_finish (WorkerData *data)
{
	State old_state;
	int i, working = 0;
	WorkerContext *context = data->context;
	gint64 last_start = data->last_start;

	++stat_workers_num_finished;

	mono_os_mutex_lock (&context->finished_lock);

	for (i = 0; i < context->active_workers_num; i++) {
		if (state_is_working_or_enqueued (context->workers_data [i].state))
			working++;
	}

	if (working == 1) {
		SgenWorkersFinishCallback callback = context->finish_callback;
		SGEN_ASSERT (0, context->idle_func_object_ops == context->idle_func_object_ops_nopar, "Why are we finishing with parallel context");
		/* We are the last one left. Enqueue preclean job if we have one and awake everybody */
		SGEN_ASSERT (0, data->state != STATE_NOT_WORKING, "How did we get from doing idle work to NOT WORKING without setting it ourselves?");
		if (callback) {
			context->finish_callback = NULL;
			callback ();
			context->worker_awakenings = 0;
			/* Make sure each worker has a chance of seeing the enqueued jobs */
			sgen_workers_ensure_awake (context);
			SGEN_ASSERT (0, data->state == STATE_WORK_ENQUEUED, "Why did we fail to set our own state to ENQUEUED");

			/*
			 * Log to be able to get the duration of normal concurrent M&S phase.
			 * Worker indexes are 1 based, since 0 is logically considered gc thread.
			 */
			sgen_binary_protocol_worker_finish_stats (GPTRDIFF_TO_INT (data - &context->workers_data [0] + 1), context->generation, context->forced_stop, data->major_scan_time, data->los_scan_time, data->total_time + sgen_timestamp () - last_start);
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
		context->idle_func_object_ops = context->idle_func_object_ops_nopar;

	context->workers_finished = TRUE;
	mono_os_mutex_unlock (&context->finished_lock);

	data->total_time += (sgen_timestamp () - last_start);
	sgen_binary_protocol_worker_finish_stats (GPTRDIFF_TO_INT (data - &context->workers_data [0] + 1), context->generation, context->forced_stop, data->major_scan_time, data->los_scan_time, data->total_time);

	sgen_gray_object_queue_trim_free_list (&data->private_gray_queue);
	return;

work_available:
	mono_os_mutex_unlock (&context->finished_lock);
}

void
sgen_workers_enqueue_job (int generation, SgenThreadPoolJob *job, gboolean enqueue)
{
	if (!enqueue) {
		job->func (NULL, job);
		sgen_thread_pool_job_free (job);
		return;
	}

	sgen_thread_pool_job_enqueue (worker_contexts [generation].thread_pool_context, job);
}

/*
 * LOCKING: Assumes the GC lock is held.
 */

void
sgen_workers_enqueue_deferred_job (int generation, SgenThreadPoolJob *job, gboolean enqueue)
{
	if (!enqueue) {
		job->func (NULL, job);
		sgen_thread_pool_job_free (job);
		return;
	}

	sgen_thread_pool_job_enqueue_deferred (worker_contexts [generation].thread_pool_context, job);
}

/*
 * LOCKING: Assumes the GC lock is held.
 */

void
sgen_workers_flush_deferred_jobs (int generation, gboolean signal)
{
	sgen_thread_pool_flush_deferred_jobs (generation, signal);
}

static gboolean
workers_get_work (WorkerData *data)
{
	SgenMajorCollector *major = sgen_get_major_collector ();
	SgenMinorCollector *minor = sgen_get_minor_collector ();
	GrayQueueSection *section;

	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));
	g_assert (major->is_concurrent || minor->is_parallel);

	section = sgen_section_gray_queue_dequeue (&data->context->workers_distribute_gray_queue);
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
	WorkerContext *context = data->context;
	int i, current_worker;

	if ((generation == GENERATION_OLD && !major->is_parallel) ||
			(generation == GENERATION_NURSERY && !minor->is_parallel))
		return FALSE;

	/* If we're parallel, steal from other workers' private gray queues  */
	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));

	current_worker = (int) (data - context->workers_data);

	for (i = 1; i < context->active_workers_num && !section; i++) {
		int steal_worker = (current_worker + i) % context->active_workers_num;
		if (state_is_working_or_enqueued (context->workers_data [steal_worker].state))
			section = sgen_gray_object_steal_section (&context->workers_data [steal_worker].private_gray_queue);
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
	g_assert (sgen_get_concurrent_collection_in_progress ());
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

	if (!major->is_concurrent && !minor->is_parallel)
		return;

	init_private_gray_queue (data);

	/* Separate WorkerData for same thread share free_block_lists */
	if (major->is_parallel || minor->is_parallel)
		major->init_block_free_lists (&data->free_block_lists);
}

static gboolean
sgen_workers_are_working (WorkerContext *context)
{
	int i;

	for (i = 0; i < context->active_workers_num; i++) {
		if (state_is_working_or_enqueued (context->workers_data [i].state))
			return TRUE;
	}
	return FALSE;
}

static gboolean
continue_idle_func (void *data_untyped, int thread_pool_context)
{
	if (data_untyped)
		return state_is_working_or_enqueued (((WorkerData*)data_untyped)->state);

	/* Return if any of the threads is working in the context */
	if (worker_contexts [GENERATION_NURSERY].workers_num && worker_contexts [GENERATION_NURSERY].thread_pool_context == thread_pool_context)
		return sgen_workers_are_working (&worker_contexts [GENERATION_NURSERY]);
	if (worker_contexts [GENERATION_OLD].workers_num && worker_contexts [GENERATION_OLD].thread_pool_context == thread_pool_context)
		return sgen_workers_are_working (&worker_contexts [GENERATION_OLD]);

	g_assert_not_reached ();
	return FALSE;
}

static gboolean
should_work_func (void *data_untyped)
{
	WorkerData *data = (WorkerData*)data_untyped;
	WorkerContext *context = data->context;
	int current_worker = (int) (data - context->workers_data);

	return context->started && current_worker < context->active_workers_num && state_is_working_or_enqueued (data->state);
}

static void
marker_idle_func (void *data_untyped)
{
	WorkerData *data = (WorkerData *)data_untyped;
	WorkerContext *context = data->context;

	SGEN_ASSERT (0, continue_idle_func (data_untyped, context->thread_pool_context), "Why are we called when we're not supposed to work?");

	if (data->state == STATE_WORK_ENQUEUED) {
		set_state (data, STATE_WORK_ENQUEUED, STATE_WORKING);
		SGEN_ASSERT (0, data->state != STATE_NOT_WORKING, "How did we get from WORK ENQUEUED to NOT WORKING?");
	}

	if (!context->forced_stop && (!sgen_gray_object_queue_is_empty (&data->private_gray_queue) || workers_get_work (data) || workers_steal_work (data))) {
		ScanCopyContext ctx = CONTEXT_FROM_OBJECT_OPERATIONS (context->idle_func_object_ops, &data->private_gray_queue);

		SGEN_ASSERT (0, !sgen_gray_object_queue_is_empty (&data->private_gray_queue), "How is our gray queue empty if we just got work?");

		sgen_drain_gray_stack (ctx);

		if (data->private_gray_queue.num_sections >= SGEN_WORKER_MIN_SECTIONS_SIGNAL
				&& context->workers_finished && context->worker_awakenings < context->active_workers_num) {
			/* We bound the number of worker awakenings just to be sure */
			context->worker_awakenings++;
			mono_os_mutex_lock (&context->finished_lock);
			sgen_workers_ensure_awake (context);
			mono_os_mutex_unlock (&context->finished_lock);
		}
	} else {
		worker_try_finish (data);
	}
}

static void
init_distribute_gray_queue (WorkerContext *context)
{
	sgen_section_gray_queue_init (&context->workers_distribute_gray_queue, TRUE,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL);
}

void
sgen_workers_create_context (int generation, int num_workers)
{
	static gboolean stat_inited = FALSE;
	int i;
	WorkerData **workers_data_ptrs;
	WorkerContext *context = &worker_contexts [generation];

	SGEN_ASSERT (0, !context->workers_num, "We can't init the worker context for a generation twice");

	mono_os_mutex_init (&context->finished_lock);

	context->generation = generation;
	context->workers_num = (num_workers > SGEN_THREADPOOL_MAX_NUM_THREADS) ? SGEN_THREADPOOL_MAX_NUM_THREADS : num_workers;
	context->active_workers_num = context->workers_num;

	context->workers_data = (WorkerData *)sgen_alloc_internal_dynamic (sizeof (WorkerData) * context->workers_num, INTERNAL_MEM_WORKER_DATA, TRUE);
	memset (context->workers_data, 0, sizeof (WorkerData) * context->workers_num);

	init_distribute_gray_queue (context);

	workers_data_ptrs = (WorkerData**)sgen_alloc_internal_dynamic (context->workers_num * sizeof (WorkerData*), INTERNAL_MEM_WORKER_DATA, TRUE);
	for (i = 0; i < context->workers_num; ++i) {
		workers_data_ptrs [i] = &context->workers_data [i];
		context->workers_data [i].context = context;
	}

	context->thread_pool_context = sgen_thread_pool_create_context (context->workers_num, thread_pool_init_func, marker_idle_func, continue_idle_func, should_work_func, (void**)workers_data_ptrs);

	if (!stat_inited) {
		mono_counters_register ("# workers finished", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_num_finished);
		stat_inited = TRUE;
	}
}

/* This is called with thread pool lock so no context switch can happen */
static gboolean
continue_idle_wait (int calling_context, int *threads_context)
{
	WorkerContext *context;
	int i;

	if (worker_contexts [GENERATION_OLD].workers_num && calling_context == worker_contexts [GENERATION_OLD].thread_pool_context)
		context = &worker_contexts [GENERATION_OLD];
	else if (worker_contexts [GENERATION_NURSERY].workers_num && calling_context == worker_contexts [GENERATION_NURSERY].thread_pool_context)
		context = &worker_contexts [GENERATION_NURSERY];
	else
		g_assert_not_reached ();

	/*
	 * We assume there are no pending jobs, since this is called only after
	 * we waited for all the jobs.
	 */
	for (i = 0; i < context->active_workers_num; i++) {
		if (threads_context [i] == calling_context)
			return TRUE;
	}

	if (sgen_workers_have_idle_work (context->generation) && !context->forced_stop)
		return TRUE;

	/*
	 * At this point there are no jobs to be done, and no objects to be scanned
	 * in the gray queues. We can simply asynchronously finish all the workers
	 * from the context that were not finished already (due to being stuck working
	 * in another context)
	 */

	for (i = 0; i < context->active_workers_num; i++) {
		if (context->workers_data [i].state == STATE_WORK_ENQUEUED)
			set_state (&context->workers_data [i], STATE_WORK_ENQUEUED, STATE_WORKING);
		if (context->workers_data [i].state == STATE_WORKING)
			worker_try_finish (&context->workers_data [i]);
	}

	return FALSE;
}


void
sgen_workers_stop_all_workers (int generation)
{
	WorkerContext *context = &worker_contexts [generation];

	mono_os_mutex_lock (&context->finished_lock);
	context->finish_callback = NULL;
	mono_os_mutex_unlock (&context->finished_lock);

	context->forced_stop = TRUE;

	sgen_thread_pool_wait_for_all_jobs (context->thread_pool_context);
	sgen_thread_pool_idle_wait (context->thread_pool_context, continue_idle_wait);
	SGEN_ASSERT (0, !sgen_workers_are_working (context), "Can only signal enqueue work when in no work state");

	context->started = FALSE;
}

void
sgen_workers_set_num_active_workers (int generation, int num_workers)
{
	WorkerContext *context = &worker_contexts [generation];
	if (num_workers) {
		SGEN_ASSERT (0, num_workers <= context->workers_num, "We can't start more workers than we initialized");
		context->active_workers_num = num_workers;
	} else {
		context->active_workers_num = context->workers_num;
	}
}

void
sgen_workers_start_all_workers (int generation, SgenObjectOperations *object_ops_nopar, SgenObjectOperations *object_ops_par, SgenWorkersFinishCallback callback)
{
	WorkerContext *context = &worker_contexts [generation];
	int i;
	SGEN_ASSERT (0, !context->started, "Why are we starting to work without finishing previous cycle");
	SGEN_ASSERT (0, !sgen_thread_pool_have_deferred_jobs (generation), "All deferred jobs should have been flushed");

	context->idle_func_object_ops_par = object_ops_par;
	context->idle_func_object_ops_nopar = object_ops_nopar;
	context->forced_stop = FALSE;
	context->finish_callback = callback;
	context->worker_awakenings = 0;
	context->started = TRUE;

	for (i = 0; i < context->active_workers_num; i++) {
		context->workers_data [i].major_scan_time = 0;
		context->workers_data [i].los_scan_time = 0;
		context->workers_data [i].total_time = 0;
		context->workers_data [i].last_start = 0;
	}
	mono_memory_write_barrier ();

	/*
	 * We expect workers to start finishing only after all of them were awaken.
	 * Otherwise we might think that we have fewer workers and use wrong context.
	 */
	mono_os_mutex_lock (&context->finished_lock);
	sgen_workers_ensure_awake (context);
	mono_os_mutex_unlock (&context->finished_lock);
}

void
sgen_workers_join (int generation)
{
	WorkerContext *context = &worker_contexts [generation];
	int i;

	SGEN_ASSERT (0, !context->finish_callback, "Why are we joining concurrent mark early");

	sgen_thread_pool_wait_for_all_jobs (context->thread_pool_context);
	sgen_thread_pool_idle_wait (context->thread_pool_context, continue_idle_wait);
	SGEN_ASSERT (0, !sgen_workers_are_working (context), "Can only signal enqueue work when in no work state");

	/* At this point all the workers have stopped. */

	SGEN_ASSERT (0, sgen_section_gray_queue_is_empty (&context->workers_distribute_gray_queue), "Why is there still work left to do?");
	for (i = 0; i < context->active_workers_num; ++i)
		SGEN_ASSERT (0, sgen_gray_object_queue_is_empty (&context->workers_data [i].private_gray_queue), "Why is there still work left to do?");

	context->started = FALSE;
}

/*
 * Can only be called if the workers are not working in the
 * context and there are no pending jobs.
 */
gboolean
sgen_workers_have_idle_work (int generation)
{
	WorkerContext *context = &worker_contexts [generation];
	int i;

	if (!sgen_section_gray_queue_is_empty (&context->workers_distribute_gray_queue))
		return TRUE;

	for (i = 0; i < context->active_workers_num; ++i) {
		if (!sgen_gray_object_queue_is_empty (&context->workers_data [i].private_gray_queue))
			return TRUE;
	}

	return FALSE;
}

gboolean
sgen_workers_all_done (void)
{
	if (worker_contexts [GENERATION_NURSERY].workers_num && sgen_workers_are_working (&worker_contexts [GENERATION_NURSERY]))
		return FALSE;
	if (worker_contexts [GENERATION_OLD].workers_num && sgen_workers_are_working (&worker_contexts [GENERATION_OLD]))
		return FALSE;

	return TRUE;
}

void
sgen_workers_assert_gray_queue_is_empty (int generation)
{
	SGEN_ASSERT (0, sgen_section_gray_queue_is_empty (&worker_contexts [generation].workers_distribute_gray_queue), "Why is the workers gray queue not empty?");
}

void
sgen_workers_take_from_queue (int generation, SgenGrayQueue *queue)
{
	WorkerContext *context = &worker_contexts [generation];

	sgen_gray_object_spread (queue, sgen_workers_get_job_split_count (generation));

	for (;;) {
		GrayQueueSection *section = sgen_gray_object_dequeue_section (queue);
		if (!section)
			break;
		sgen_section_gray_queue_enqueue (&context->workers_distribute_gray_queue, section);
	}

	SGEN_ASSERT (0, !sgen_workers_are_working (context), "We should fully populate the distribute gray queue before we start the workers");
}

SgenObjectOperations*
sgen_workers_get_idle_func_object_ops (WorkerData *worker)
{
	g_assert (worker->context->idle_func_object_ops);
	return worker->context->idle_func_object_ops;
}

/*
 * If we have a single worker, splitting into multiple jobs makes no sense. With
 * more than one worker, we split into a larger number of jobs so that, in case
 * the work load is uneven, a worker that finished quickly can take up more jobs
 * than another one.
 *
 * We also return 1 if there is no worker context for that generation.
 */
int
sgen_workers_get_job_split_count (int generation)
{
	return (worker_contexts [generation].active_workers_num > 1) ? worker_contexts [generation].active_workers_num * 4 : 1;
}

int
sgen_workers_get_active_worker_count (int generation)
{
	return (worker_contexts [generation].active_workers_num);
}

void
sgen_workers_foreach (int generation, SgenWorkerCallback callback)
{
	WorkerContext *context = &worker_contexts [generation];
	int i;

	for (i = 0; i < context->workers_num; i++)
		callback (&context->workers_data [i]);
}

gboolean
sgen_workers_is_worker_thread (MonoNativeThreadId id)
{
	return sgen_thread_pool_is_thread_pool_thread (id);
}

#else
// Single theaded sgen-workers impl

void
sgen_workers_enqueue_job (int generation, SgenThreadPoolJob *job, gboolean enqueue)
{
	if (!enqueue) {
		job->func (NULL, job);
		sgen_thread_pool_job_free (job);
		return;
	}
}

void
sgen_workers_enqueue_deferred_job (int generation, SgenThreadPoolJob *job, gboolean enqueue)
{
	sgen_workers_enqueue_job (generation, job, enqueue);
}

void sgen_workers_flush_deferred_jobs (int generation, gboolean signal)
{
}

gboolean
sgen_workers_all_done (void)
{
	return TRUE;
}

void
sgen_workers_assert_gray_queue_is_empty (int generation)
{
}

SgenObjectOperations*
sgen_workers_get_idle_func_object_ops (WorkerData *worker)
{
	g_assert (worker->context->idle_func_object_ops);
	return worker->context->idle_func_object_ops;
}

int
sgen_workers_get_job_split_count (int generation)
{
	return 1;
}

int
sgen_workers_get_active_worker_count (int generation)
{
	return 0;
}

gboolean
sgen_workers_have_idle_work (int generation)
{
	return FALSE;
}

gboolean
sgen_workers_is_worker_thread (MonoNativeThreadId id)
{
	return FALSE;
}

void
sgen_workers_join (int generation)
{
}

void
sgen_workers_set_num_active_workers (int generation, int num_workers)
{
}

void
sgen_workers_stop_all_workers (int generation)
{
}

void
sgen_workers_take_from_queue (int generation, SgenGrayQueue *queue)
{
}

#endif //#ifdef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
#endif // #ifdef HAVE_SGEN_GC
