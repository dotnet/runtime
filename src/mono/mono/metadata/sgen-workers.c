/*
 * sgen-workers.c: Worker threads for parallel and concurrent GC.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-workers.h"
#include "metadata/sgen-thread-pool.h"
#include "utils/mono-counters.h"

static int workers_num;
static WorkerData *workers_data;

static SgenSectionGrayQueue workers_distribute_gray_queue;
static gboolean workers_distribute_gray_queue_inited;

enum {
	STATE_NOT_WORKING,
	STATE_WORKING,
	STATE_NURSERY_COLLECTION
} WorkersStateName;

typedef gint32 State;

static volatile State workers_state;

static guint64 stat_workers_num_finished;

static gboolean
set_state (State old_state, State new_state)
{
	if (old_state == STATE_NURSERY_COLLECTION)
		SGEN_ASSERT (0, new_state != STATE_NOT_WORKING, "Can't go from nursery collection to not working");

	return InterlockedCompareExchange (&workers_state, new_state, old_state) == old_state;
}

static void
assert_not_working (State state)
{
	SGEN_ASSERT (0, state == STATE_NOT_WORKING, "Can only signal enqueue work when in no work state");
}

static void
assert_working (State state)
{
	SGEN_ASSERT (0, state == STATE_WORKING, "A worker can't wait without being in working state");
}

static void
assert_nursery_collection (State state)
{
	SGEN_ASSERT (0, state == STATE_NURSERY_COLLECTION, "Must be in the nursery collection state");
}

static void
assert_working_or_nursery_collection (State state)
{
	if (state != STATE_WORKING)
		assert_nursery_collection (state);
}

static void
workers_signal_enqueue_work (gboolean from_nursery_collection)
{
	State old_state = workers_state;
	gboolean did_set_state;

	if (from_nursery_collection)
		assert_nursery_collection (old_state);
	else
		assert_not_working (old_state);

	did_set_state = set_state (old_state, STATE_WORKING);
	SGEN_ASSERT (0, did_set_state, "Nobody else should be mutating the state");

	sgen_thread_pool_idle_signal ();
}

static void
workers_signal_enqueue_work_if_necessary (void)
{
	if (workers_state == STATE_NOT_WORKING)
		workers_signal_enqueue_work (FALSE);
}

void
sgen_workers_ensure_awake (void)
{
	SGEN_ASSERT (0, workers_state != STATE_NURSERY_COLLECTION, "Can't wake workers during nursery collection");
	workers_signal_enqueue_work_if_necessary ();
}

static void
worker_finish (void)
{
	State old_state;

	++stat_workers_num_finished;

	do {
		old_state = workers_state;

		assert_working_or_nursery_collection (old_state);
		if (old_state == STATE_NURSERY_COLLECTION)
			return;

		/* We are the last thread to go to sleep. */
	} while (!set_state (old_state, STATE_NOT_WORKING));
}

static gboolean
collection_needs_workers (void)
{
	return sgen_collection_is_concurrent ();
}

void
sgen_workers_enqueue_job (SgenThreadPoolJob *job)
{
	if (!collection_needs_workers ()) {
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
}

void
sgen_workers_signal_start_nursery_collection_and_wait (void)
{
	State old_state;

	do {
		old_state = workers_state;

		if (old_state != STATE_NOT_WORKING)
			assert_working (old_state);
	} while (!set_state (old_state, STATE_NURSERY_COLLECTION));

	sgen_thread_pool_idle_wait ();

	assert_nursery_collection (workers_state);
}

void
sgen_workers_signal_finish_nursery_collection (void)
{
	assert_nursery_collection (workers_state);
	workers_signal_enqueue_work (TRUE);
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
concurrent_enqueue_check (char *obj)
{
	g_assert (sgen_concurrent_collection_in_progress ());
	g_assert (!sgen_ptr_in_nursery (obj));
	g_assert (SGEN_LOAD_VTABLE (obj));
}

static void
init_private_gray_queue (WorkerData *data)
{
	sgen_gray_object_queue_init (&data->private_gray_queue,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL);
}

static void
thread_pool_init_func (void *data_untyped)
{
	WorkerData *data = data_untyped;
	SgenMajorCollector *major = sgen_get_major_collector ();

	mono_thread_info_register_small_id ();

	if (!major->is_concurrent)
		return;

	init_private_gray_queue (data);
}

static gboolean
marker_idle_func (void *data_untyped)
{
	WorkerData *data = data_untyped;
	SgenMajorCollector *major = sgen_get_major_collector ();

	if (workers_state != STATE_WORKING)
		return FALSE;

	SGEN_ASSERT (0, sgen_get_current_collection_generation () != GENERATION_NURSERY, "Why are we doing work while there's a nursery collection happening?");

	if (!sgen_gray_object_queue_is_empty (&data->private_gray_queue) || workers_get_work (data)) {
		SgenObjectOperations *ops = sgen_concurrent_collection_in_progress ()
			? &major->major_concurrent_ops
			: &major->major_ops;
		ScanCopyContext ctx = { ops->scan_object, NULL, &data->private_gray_queue };

		SGEN_ASSERT (0, !sgen_gray_object_queue_is_empty (&data->private_gray_queue), "How is our gray queue empty if we just got work?");

		sgen_drain_gray_stack (32, ctx);

		return TRUE;
	}

	worker_finish ();

	return FALSE;
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
	SGEN_ASSERT (0, sgen_get_major_collector ()->is_concurrent && collection_needs_workers (),
			"Why should we init the distribute gray queue if we don't need it?");
	init_distribute_gray_queue ();
}

void
sgen_workers_init (int num_workers)
{
	int i;
	void *workers_data_ptrs [num_workers];

	if (!sgen_get_major_collector ()->is_concurrent) {
		sgen_thread_pool_init (num_workers, thread_pool_init_func, NULL, NULL);
		return;
	}

	//g_print ("initing %d workers\n", num_workers);

	workers_num = num_workers;

	workers_data = sgen_alloc_internal_dynamic (sizeof (WorkerData) * num_workers, INTERNAL_MEM_WORKER_DATA, TRUE);
	memset (workers_data, 0, sizeof (WorkerData) * num_workers);

	init_distribute_gray_queue ();

	for (i = 0; i < workers_num; ++i)
		workers_data_ptrs [i] = &workers_data [i];

	sgen_thread_pool_init (num_workers, thread_pool_init_func, marker_idle_func, workers_data_ptrs);

	mono_counters_register ("# workers finished", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_num_finished);
}

void
sgen_workers_start_all_workers (void)
{
	if (!collection_needs_workers ())
		return;

	workers_signal_enqueue_work (FALSE);
}

void
sgen_workers_join (void)
{
	int i;

	if (!collection_needs_workers ())
		return;

	sgen_thread_pool_wait_for_all_jobs ();

	for (;;) {
		SGEN_ASSERT (0, workers_state != STATE_NURSERY_COLLECTION, "Can't be in nursery collection when joining");
		sgen_thread_pool_idle_wait ();
		assert_not_working (workers_state);

		/*
		 * Checking whether there is still work left and, if not, going to sleep,
		 * are two separate actions that are not performed atomically by the
		 * workers.  Therefore there's a race condition where work can be added
		 * after they've checked for work, and before they've gone to sleep.
		 */
		if (sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue))
			break;

		workers_signal_enqueue_work (FALSE);
	}

	/* At this point all the workers have stopped. */

	g_assert (sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue));
	for (i = 0; i < workers_num; ++i)
		g_assert (sgen_gray_object_queue_is_empty (&workers_data [i].private_gray_queue));
}

gboolean
sgen_workers_all_done (void)
{
	return workers_state == STATE_NOT_WORKING;
}

gboolean
sgen_workers_are_working (void)
{
	return workers_state == STATE_WORKING;
}

void
sgen_workers_wait (void)
{
	sgen_thread_pool_idle_wait ();
	SGEN_ASSERT (0, sgen_workers_all_done (), "Why are the workers not done after we wait for them?");
}

SgenSectionGrayQueue*
sgen_workers_get_distribute_section_gray_queue (void)
{
	return &workers_distribute_gray_queue;
}

#endif
