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
#include "utils/mono-counters.h"

static int workers_num;
static WorkerData *workers_data;
static WorkerData workers_gc_thread_data;

static SgenGrayQueue workers_distribute_gray_queue;

static volatile gboolean workers_gc_in_progress = FALSE;
static volatile gboolean workers_marking = FALSE;
static gboolean workers_started = FALSE;
static volatile int workers_num_waiting = 0;
static MonoSemType workers_waiting_sem;
static MonoSemType workers_done_sem;
static volatile int workers_done_posted = 0;

static volatile int workers_job_queue_num_entries = 0;
static volatile JobQueueEntry *workers_job_queue = NULL;
static LOCK_DECLARE (workers_job_queue_mutex);
static int workers_num_jobs_enqueued = 0;
static volatile int workers_num_jobs_finished = 0;

static long long stat_workers_stolen_from_self_lock;
static long long stat_workers_stolen_from_self_no_lock;
static long long stat_workers_stolen_from_others;
static long long stat_workers_num_waited;

static void
workers_wake_up (int max)
{
	int i;

	for (i = 0; i < max; ++i) {
		int num;
		do {
			num = workers_num_waiting;
			if (num == 0)
				return;
		} while (InterlockedCompareExchange (&workers_num_waiting, num - 1, num) != num);
		MONO_SEM_POST (&workers_waiting_sem);
	}
}

static void
workers_wake_up_all (void)
{
	workers_wake_up (workers_num);
}

static void
workers_wait (void)
{
	int num;
	++stat_workers_num_waited;
	do {
		num = workers_num_waiting;
	} while (InterlockedCompareExchange (&workers_num_waiting, num + 1, num) != num);
	if (num + 1 == workers_num && !workers_gc_in_progress) {
		/* Make sure the done semaphore is only posted once. */
		int posted;
		do {
			posted = workers_done_posted;
			if (posted)
				break;
		} while (InterlockedCompareExchange (&workers_done_posted, 1, 0) != 0);
		if (!posted)
			MONO_SEM_POST (&workers_done_sem);
	}
	MONO_SEM_WAIT (&workers_waiting_sem);
}

static gboolean
collection_needs_workers (void)
{
	return sgen_collection_is_parallel () || sgen_collection_is_concurrent ();
}

void
sgen_workers_enqueue_job (JobFunc func, void *data)
{
	int num_entries;
	JobQueueEntry *entry;

	if (!collection_needs_workers ()) {
		func (NULL, data);
		return;
	}

	entry = sgen_alloc_internal (INTERNAL_MEM_JOB_QUEUE_ENTRY);
	entry->func = func;
	entry->data = data;

	mono_mutex_lock (&workers_job_queue_mutex);
	entry->next = workers_job_queue;
	workers_job_queue = entry;
	num_entries = ++workers_job_queue_num_entries;
	++workers_num_jobs_enqueued;
	mono_mutex_unlock (&workers_job_queue_mutex);

	workers_wake_up (num_entries);
}

void
sgen_workers_wait_for_jobs (void)
{
	// FIXME: implement this properly
	while (workers_num_jobs_finished < workers_num_jobs_enqueued)
		g_usleep (1000);
}

static gboolean
workers_dequeue_and_do_job (WorkerData *data)
{
	JobQueueEntry *entry;
	int num_finished;

	/*
	 * At this point the GC might not be running anymore.  We
	 * could have been woken up by a job that was then taken by
	 * another thread, after which the collection finished, so we
	 * first have to successfully dequeue a job before doing
	 * anything assuming that the collection is still ongoing.
	 */

	if (!workers_job_queue_num_entries)
		return FALSE;

	mono_mutex_lock (&workers_job_queue_mutex);
	entry = (JobQueueEntry*)workers_job_queue;
	if (entry) {
		workers_job_queue = entry->next;
		--workers_job_queue_num_entries;
	}
	mono_mutex_unlock (&workers_job_queue_mutex);

	if (!entry)
		return FALSE;

	g_assert (collection_needs_workers ());

	entry->func (data, entry->data);
	sgen_free_internal (entry, INTERNAL_MEM_JOB_QUEUE_ENTRY);

	do {
		num_finished = workers_num_jobs_finished;
	} while (InterlockedCompareExchange (&workers_num_jobs_finished, num_finished + 1, num_finished) != num_finished);

	return TRUE;
}

static gboolean
workers_steal (WorkerData *data, WorkerData *victim_data, gboolean lock)
{
	SgenGrayQueue *queue = &data->private_gray_queue;
	int num, n;

	g_assert (!queue->first);

	if (!victim_data->stealable_stack_fill)
		return FALSE;

	if (lock && mono_mutex_trylock (&victim_data->stealable_stack_mutex))
		return FALSE;

	n = num = (victim_data->stealable_stack_fill + 1) / 2;
	/* We're stealing num entries. */

	while (n > 0) {
		int m = MIN (SGEN_GRAY_QUEUE_SECTION_SIZE, n);
		n -= m;

		sgen_gray_object_alloc_queue_section (queue);
		memcpy (queue->first->objects,
				victim_data->stealable_stack + victim_data->stealable_stack_fill - num + n,
				sizeof (char*) * m);
		queue->first->end = m;
	}

	victim_data->stealable_stack_fill -= num;

	if (lock)
		mono_mutex_unlock (&victim_data->stealable_stack_mutex);

	if (data == victim_data) {
		if (lock)
			stat_workers_stolen_from_self_lock += num;
		else
			stat_workers_stolen_from_self_no_lock += num;
	} else {
		stat_workers_stolen_from_others += num;
	}

	return num != 0;
}

static gboolean
workers_get_work (WorkerData *data)
{
	int i;

	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));

	/* Try to steal from our own stack. */
	if (workers_steal (data, data, TRUE))
		return TRUE;

	/* Then from the GC thread's stack. */
	if (workers_steal (data, &workers_gc_thread_data, TRUE))
		return TRUE;

	/* Finally, from another worker. */
	for (i = 0; i < workers_num; ++i) {
		WorkerData *victim_data = &workers_data [i];
		if (data == victim_data)
			continue;
		if (workers_steal (data, victim_data, TRUE))
			return TRUE;
	}

	/* Nobody to steal from */
	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));
	return FALSE;
}

static void
workers_gray_queue_share_redirect (SgenGrayQueue *queue)
{
	GrayQueueSection *section;
	WorkerData *data = queue->alloc_prepare_data;

	if (data->stealable_stack_fill) {
		/*
		 * There are still objects in the stealable stack, so
		 * wake up any workers that might be sleeping
		 */
		if (workers_gc_in_progress)
			workers_wake_up_all ();
		return;
	}

	/* The stealable stack is empty, so fill it. */
	mono_mutex_lock (&data->stealable_stack_mutex);

	while (data->stealable_stack_fill < STEALABLE_STACK_SIZE &&
			(section = sgen_gray_object_dequeue_section (queue))) {
		int num = MIN (section->end, STEALABLE_STACK_SIZE - data->stealable_stack_fill);

		memcpy (data->stealable_stack + data->stealable_stack_fill,
				section->objects + section->end - num,
				sizeof (char*) * num);

		section->end -= num;
		data->stealable_stack_fill += num;

		if (section->end)
			sgen_gray_object_enqueue_section (queue, section);
		else
			sgen_gray_object_free_queue_section (section);
	}

	if (data != &workers_gc_thread_data && sgen_gray_object_queue_is_empty (queue))
		workers_steal (data, data, FALSE);

	mono_mutex_unlock (&data->stealable_stack_mutex);

	if (workers_gc_in_progress)
		workers_wake_up_all ();
}

static void
concurrent_enqueue_check (SgenGrayQueue *queue, char *obj)
{
	g_assert (!sgen_ptr_in_nursery (obj));
	g_assert (SGEN_LOAD_VTABLE (obj));
}

static void
init_private_gray_queue (WorkerData *data)
{
	sgen_gray_object_queue_init_with_alloc_prepare (&data->private_gray_queue,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL,
			workers_gray_queue_share_redirect, data);
}

static mono_native_thread_return_t
workers_thread_func (void *data_untyped)
{
	WorkerData *data = data_untyped;

	mono_thread_info_register_small_id ();

	if (sgen_get_major_collector ()->init_worker_thread)
		sgen_get_major_collector ()->init_worker_thread (data->major_collector_data);

	init_private_gray_queue (data);

	for (;;) {
		gboolean did_work = FALSE;

		while (workers_dequeue_and_do_job (data)) {
			did_work = TRUE;
			/* FIXME: maybe distribute the gray queue here? */
		}

		if (workers_marking && (!sgen_gray_object_queue_is_empty (&data->private_gray_queue) || workers_get_work (data))) {
			g_assert (!sgen_gray_object_queue_is_empty (&data->private_gray_queue));

			while (!sgen_drain_gray_stack (&data->private_gray_queue, sgen_get_major_collector ()->major_ops.scan_object, 32))
				workers_gray_queue_share_redirect (&data->private_gray_queue);
			g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));

			init_private_gray_queue (data);

			did_work = TRUE;
		}

		if (!did_work)
			workers_wait ();
	}

	/* dummy return to make compilers happy */
	return NULL;
}

void
sgen_workers_distribute_gray_queue_sections (void)
{
	if (!collection_needs_workers ())
		return;

	workers_gray_queue_share_redirect (&workers_distribute_gray_queue);
}

static void
init_distribute_gray_queue (void)
{
	sgen_gray_object_queue_init_with_alloc_prepare (&workers_distribute_gray_queue,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL,
			workers_gray_queue_share_redirect, &workers_gc_thread_data);
}

void
sgen_workers_init_distribute_gray_queue (void)
{
	if (!collection_needs_workers ())
		return;

	init_distribute_gray_queue ();
}

void
sgen_workers_init (int num_workers)
{
	int i;

	if (!sgen_get_major_collector ()->is_parallel && !sgen_get_major_collector ()->is_concurrent)
		return;

	//g_print ("initing %d workers\n", num_workers);

	workers_num = num_workers;

	workers_data = sgen_alloc_internal_dynamic (sizeof (WorkerData) * num_workers, INTERNAL_MEM_WORKER_DATA, TRUE);
	memset (workers_data, 0, sizeof (WorkerData) * num_workers);

	MONO_SEM_INIT (&workers_waiting_sem, 0);
	MONO_SEM_INIT (&workers_done_sem, 0);

	init_distribute_gray_queue ();
	mono_mutex_init (&workers_gc_thread_data.stealable_stack_mutex, NULL);
	workers_gc_thread_data.stealable_stack_fill = 0;

	if (sgen_get_major_collector ()->alloc_worker_data)
		workers_gc_thread_data.major_collector_data = sgen_get_major_collector ()->alloc_worker_data ();

	for (i = 0; i < workers_num; ++i) {
		/* private gray queue is inited by the thread itself */
		mono_mutex_init (&workers_data [i].stealable_stack_mutex, NULL);
		workers_data [i].stealable_stack_fill = 0;

		if (sgen_get_major_collector ()->alloc_worker_data)
			workers_data [i].major_collector_data = sgen_get_major_collector ()->alloc_worker_data ();
	}

	LOCK_INIT (workers_job_queue_mutex);

	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_JOB_QUEUE_ENTRY, sizeof (JobQueueEntry));

	mono_counters_register ("Stolen from self lock", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_workers_stolen_from_self_lock);
	mono_counters_register ("Stolen from self no lock", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_workers_stolen_from_self_no_lock);
	mono_counters_register ("Stolen from others", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_workers_stolen_from_others);
	mono_counters_register ("# workers waited", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_workers_num_waited);
}

/* only the GC thread is allowed to start and join workers */

static void
workers_start_worker (int index)
{
	g_assert (index >= 0 && index < workers_num);

	g_assert (!workers_data [index].thread);
	mono_native_thread_create (&workers_data [index].thread, workers_thread_func, &workers_data [index]);
}

void
sgen_workers_start_all_workers (void)
{
	int i;

	if (!collection_needs_workers ())
		return;

	if (sgen_get_major_collector ()->init_worker_thread)
		sgen_get_major_collector ()->init_worker_thread (workers_gc_thread_data.major_collector_data);

	g_assert (!workers_gc_in_progress);
	workers_gc_in_progress = TRUE;
	workers_marking = FALSE;
	workers_done_posted = 0;

	g_assert (workers_job_queue_num_entries == 0);
	workers_num_jobs_enqueued = 0;
	workers_num_jobs_finished = 0;

	if (workers_started) {
		if (workers_num_waiting != workers_num)
			g_error ("Expecting all %d sgen workers to be parked, but only %d are", workers_num, workers_num_waiting);
		workers_wake_up_all ();
		return;
	}

	for (i = 0; i < workers_num; ++i)
		workers_start_worker (i);

	workers_started = TRUE;
}

void
sgen_workers_start_marking (void)
{
	if (!collection_needs_workers ())
		return;

	g_assert (workers_started && workers_gc_in_progress);
	g_assert (!workers_marking);

	workers_marking = TRUE;

	workers_wake_up_all ();
}

void
sgen_workers_join (void)
{
	int i;

	if (!collection_needs_workers ())
		return;

	g_assert (sgen_gray_object_queue_is_empty (&workers_gc_thread_data.private_gray_queue));
	g_assert (sgen_gray_object_queue_is_empty (&workers_distribute_gray_queue));

	g_assert (workers_gc_in_progress);
	workers_gc_in_progress = FALSE;
	if (workers_num_waiting == workers_num) {
		/*
		 * All the workers might have shut down at this point
		 * and posted the done semaphore but we don't know it
		 * yet.  It's not a big deal to wake them up again -
		 * they'll just do one iteration of their loop trying to
		 * find something to do and then go back to waiting
		 * again.
		 */
		workers_wake_up_all ();
	}
	MONO_SEM_WAIT (&workers_done_sem);
	workers_marking = FALSE;

	if (sgen_get_major_collector ()->reset_worker_data) {
		for (i = 0; i < workers_num; ++i)
			sgen_get_major_collector ()->reset_worker_data (workers_data [i].major_collector_data);
	}

	g_assert (workers_done_posted);

	g_assert (!workers_gc_thread_data.stealable_stack_fill);
	g_assert (sgen_gray_object_queue_is_empty (&workers_gc_thread_data.private_gray_queue));
	for (i = 0; i < workers_num; ++i) {
		g_assert (!workers_data [i].stealable_stack_fill);
		g_assert (sgen_gray_object_queue_is_empty (&workers_data [i].private_gray_queue));
	}
}

gboolean
sgen_workers_all_done (void)
{
	return workers_num_waiting == workers_num;
}

gboolean
sgen_is_worker_thread (MonoNativeThreadId thread)
{
	int i;

	if (sgen_get_major_collector ()->is_worker_thread && sgen_get_major_collector ()->is_worker_thread (thread))
		return TRUE;

	for (i = 0; i < workers_num; ++i) {
		if (workers_data [i].thread == thread)
			return TRUE;
	}
	return FALSE;
}

gboolean
sgen_workers_is_distributed_queue (SgenGrayQueue *queue)
{
	return queue == &workers_distribute_gray_queue;
}

SgenGrayQueue*
sgen_workers_get_distribute_gray_queue (void)
{
	return &workers_distribute_gray_queue;
}

void
sgen_workers_reset_data (void)
{
	if (sgen_get_major_collector ()->reset_worker_data)
		sgen_get_major_collector ()->reset_worker_data (workers_gc_thread_data.major_collector_data);

}
#endif
