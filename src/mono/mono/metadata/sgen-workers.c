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
static void *workers_gc_thread_major_collector_data = NULL;

static SgenSectionGrayQueue workers_distribute_gray_queue;
static gboolean workers_distribute_gray_queue_inited;

static volatile gboolean workers_marking = FALSE;
static gboolean workers_started = FALSE;

typedef union {
	gint32 value;
	struct {
		/*
		 * Decremented by the main thread and incremented by
		 * worker threads.
		 */
		guint32 num_waiting : 8;
		/* Set by worker threads and reset by the main thread. */
		guint32 done_posted : 1;
		/* Set by the main thread. */
		guint32 gc_in_progress : 1;
	} data;
} State;

static volatile State workers_state;

static MonoSemType workers_waiting_sem;
static MonoSemType workers_done_sem;

static volatile int workers_job_queue_num_entries = 0;
static volatile JobQueueEntry *workers_job_queue = NULL;
static LOCK_DECLARE (workers_job_queue_mutex);
static int workers_num_jobs_enqueued = 0;
static volatile int workers_num_jobs_finished = 0;

static long long stat_workers_stolen_from_self_lock;
static long long stat_workers_stolen_from_self_no_lock;
static long long stat_workers_stolen_from_others;
static long long stat_workers_num_waited;

static gboolean
set_state (State old_state, State new_state)
{
	return InterlockedCompareExchange (&workers_state.value,
			new_state.value, old_state.value) == old_state.value;
}

static void
workers_wake_up (int max)
{
	int i;

	for (i = 0; i < max; ++i) {
		State old_state, new_state;
		do {
			old_state = new_state = workers_state;
			/*
			 * We must not wake workers up once done has
			 * been posted.
			 */
			if (old_state.data.done_posted)
				return;
			if (old_state.data.num_waiting == 0)
				return;
			--new_state.data.num_waiting;
		} while (!set_state (old_state, new_state));
		MONO_SEM_POST (&workers_waiting_sem);
	}
}

static void
workers_wake_up_all (void)
{
	workers_wake_up (workers_num);
}

void
sgen_workers_wake_up_all (void)
{
	g_assert (workers_state.data.gc_in_progress);
	workers_wake_up_all ();
}

static void
workers_wait (void)
{
	State old_state, new_state;
	++stat_workers_num_waited;
	do {
		old_state = new_state = workers_state;
		/*
		 * Only the last worker thread awake can set the done
		 * posted flag, and since we're awake and haven't set
		 * it yet, it cannot be set.
		 */
		g_assert (!old_state.data.done_posted);
		++new_state.data.num_waiting;
		/*
		 * This is the only place where we use
		 * workers_gc_in_progress in the worker threads.
		 */
		if (new_state.data.num_waiting == workers_num && !old_state.data.gc_in_progress)
			new_state.data.done_posted = 1;
	} while (!set_state (old_state, new_state));
	mono_memory_barrier ();
	if (new_state.data.done_posted)
		MONO_SEM_POST (&workers_done_sem);
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

	g_assert (workers_state.data.gc_in_progress);

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
	while (workers_num_jobs_finished < workers_num_jobs_enqueued) {
		State state = workers_state;
		g_assert (state.data.gc_in_progress);
		g_assert (!state.data.done_posted);
		if (state.data.num_waiting == workers_num)
			workers_wake_up_all ();
		g_usleep (1000);
	}
}

static gboolean
workers_dequeue_and_do_job (WorkerData *data)
{
	JobQueueEntry *entry;

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

	SGEN_ATOMIC_ADD (workers_num_jobs_finished, 1);

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
	SgenMajorCollector *major;
	int i;

	g_assert (sgen_gray_object_queue_is_empty (&data->private_gray_queue));

	/* Try to steal from our own stack. */
	if (workers_steal (data, data, TRUE))
		return TRUE;

	/* From another worker. */
	for (i = 0; i < workers_num; ++i) {
		WorkerData *victim_data = &workers_data [i];
		if (data == victim_data)
			continue;
		if (workers_steal (data, victim_data, TRUE))
			return TRUE;
	}

	/*
	 * If we're concurrent or parallel, from the workers
	 * distribute gray queue.
	 */
	major = sgen_get_major_collector ();
	if (major->is_concurrent || major->is_parallel) {
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
workers_gray_queue_share_redirect (SgenGrayQueue *queue)
{
	GrayQueueSection *section;
	WorkerData *data = queue->alloc_prepare_data;

	if (data->stealable_stack_fill) {
		/*
		 * There are still objects in the stealable stack, so
		 * wake up any workers that might be sleeping
		 */
		if (workers_state.data.gc_in_progress)
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

	if (sgen_gray_object_queue_is_empty (queue))
		workers_steal (data, data, FALSE);

	mono_mutex_unlock (&data->stealable_stack_mutex);

	if (workers_state.data.gc_in_progress)
		workers_wake_up_all ();
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
	sgen_gray_object_queue_init_with_alloc_prepare (&data->private_gray_queue,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL,
			workers_gray_queue_share_redirect, data);
}

static mono_native_thread_return_t
workers_thread_func (void *data_untyped)
{
	WorkerData *data = data_untyped;
	SgenMajorCollector *major = sgen_get_major_collector ();

	mono_thread_info_register_small_id ();

	if (major->init_worker_thread)
		major->init_worker_thread (data->major_collector_data);

	init_private_gray_queue (data);

	for (;;) {
		gboolean did_work = FALSE;

		while (workers_dequeue_and_do_job (data)) {
			did_work = TRUE;
			/* FIXME: maybe distribute the gray queue here? */
		}

		if (workers_marking && (!sgen_gray_object_queue_is_empty (&data->private_gray_queue) || workers_get_work (data))) {
			SgenObjectOperations *ops = sgen_concurrent_collection_in_progress ()
				? &major->major_concurrent_ops
				: &major->major_ops;
			ScanCopyContext ctx = { ops->scan_object, NULL, &data->private_gray_queue };

			g_assert (!sgen_gray_object_queue_is_empty (&data->private_gray_queue));

			while (!sgen_drain_gray_stack (32, ctx))
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

static void
init_distribute_gray_queue (gboolean locked)
{
	if (workers_distribute_gray_queue_inited) {
		g_assert (sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue));
		g_assert (!workers_distribute_gray_queue.locked == !locked);
		return;
	}

	sgen_section_gray_queue_init (&workers_distribute_gray_queue, locked,
			sgen_get_major_collector ()->is_concurrent ? concurrent_enqueue_check : NULL);
	workers_distribute_gray_queue_inited = TRUE;
}

void
sgen_workers_init_distribute_gray_queue (void)
{
	if (!collection_needs_workers ())
		return;

	init_distribute_gray_queue (sgen_get_major_collector ()->is_concurrent || sgen_get_major_collector ()->is_parallel);
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

	init_distribute_gray_queue (sgen_get_major_collector ()->is_concurrent || sgen_get_major_collector ()->is_parallel);

	if (sgen_get_major_collector ()->alloc_worker_data)
		workers_gc_thread_major_collector_data = sgen_get_major_collector ()->alloc_worker_data ();

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
	State old_state, new_state;
	int i;

	if (!collection_needs_workers ())
		return;

	if (sgen_get_major_collector ()->init_worker_thread)
		sgen_get_major_collector ()->init_worker_thread (workers_gc_thread_major_collector_data);

	old_state = new_state = workers_state;
	g_assert (!old_state.data.gc_in_progress);
	new_state.data.gc_in_progress = TRUE;

	workers_marking = FALSE;

	g_assert (workers_job_queue_num_entries == 0);
	workers_num_jobs_enqueued = 0;
	workers_num_jobs_finished = 0;

	if (workers_started) {
		g_assert (old_state.data.done_posted);
		if (old_state.data.num_waiting != workers_num) {
			g_error ("Expecting all %d sgen workers to be parked, but only %d are",
					workers_num, old_state.data.num_waiting);
		}

		/* Clear the done posted flag */
		new_state.data.done_posted = 0;
		if (!set_state (old_state, new_state))
			g_assert_not_reached ();

		workers_wake_up_all ();
		return;
	}

	g_assert (!old_state.data.done_posted);

	if (!set_state (old_state, new_state))
		g_assert_not_reached ();

	for (i = 0; i < workers_num; ++i)
		workers_start_worker (i);

	workers_started = TRUE;
}

gboolean
sgen_workers_have_started (void)
{
	return workers_state.data.gc_in_progress;
}

void
sgen_workers_start_marking (void)
{
	if (!collection_needs_workers ())
		return;

	g_assert (workers_started && workers_state.data.gc_in_progress);
	g_assert (!workers_marking);

	workers_marking = TRUE;

	workers_wake_up_all ();
}

void
sgen_workers_join (void)
{
	State old_state, new_state;
	int i;

	if (!collection_needs_workers ())
		return;

	do {
		old_state = new_state = workers_state;
		g_assert (old_state.data.gc_in_progress);
		g_assert (!old_state.data.done_posted);

		new_state.data.gc_in_progress = 0;
	} while (!set_state (old_state, new_state));

	if (new_state.data.num_waiting == workers_num) {
		/*
		 * All the workers have shut down but haven't posted
		 * the done semaphore yet, or, if we come from below,
		 * haven't done all their work yet.
		 *
		 * It's not a big deal to wake them up again - they'll
		 * just do one iteration of their loop trying to find
		 * something to do and then go back to waiting again.
		 */
	reawaken:
		workers_wake_up_all ();
	}
	MONO_SEM_WAIT (&workers_done_sem);

	old_state = new_state = workers_state;
	g_assert (old_state.data.num_waiting == workers_num);
	g_assert (old_state.data.done_posted);

	if (workers_job_queue_num_entries || !sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue)) {
		/*
		 * There's a small race condition that we avoid here.
		 * It's possible that a worker thread runs out of
		 * things to do, so it goes to sleep.  Right at that
		 * moment a new job is enqueued, but the thread is
		 * still registered as running.  Now the threads are
		 * joined, and we wait for the semaphore.  Only at
		 * this point does the worker go to sleep, and posts
		 * the semaphore, because workers_gc_in_progress is
		 * already FALSE.  The job is still in the queue,
		 * though.
		 *
		 * Clear the done posted flag.
		 */
		new_state.data.done_posted = 0;
		if (!set_state (old_state, new_state))
			g_assert_not_reached ();
		goto reawaken;
	}

	/* At this point all the workers have stopped. */

	workers_marking = FALSE;

	if (sgen_get_major_collector ()->reset_worker_data) {
		for (i = 0; i < workers_num; ++i)
			sgen_get_major_collector ()->reset_worker_data (workers_data [i].major_collector_data);
	}

	g_assert (workers_job_queue_num_entries == 0);
	g_assert (sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue));
	for (i = 0; i < workers_num; ++i) {
		g_assert (!workers_data [i].stealable_stack_fill);
		g_assert (sgen_gray_object_queue_is_empty (&workers_data [i].private_gray_queue));
	}
}

gboolean
sgen_workers_all_done (void)
{
	State state = workers_state;
	/*
	 * Can only be called while the collection is still in
	 * progress, i.e., before done has been posted.
	 */
	g_assert (state.data.gc_in_progress);
	g_assert (!state.data.done_posted);
	return state.data.num_waiting == workers_num;
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

SgenSectionGrayQueue*
sgen_workers_get_distribute_section_gray_queue (void)
{
	return &workers_distribute_gray_queue;
}

void
sgen_workers_reset_data (void)
{
	if (sgen_get_major_collector ()->reset_worker_data)
		sgen_get_major_collector ()->reset_worker_data (workers_gc_thread_major_collector_data);
}
#endif
