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

static gboolean workers_started = FALSE;

enum {
	STATE_NOT_WORKING,
	STATE_WORKING,
	STATE_NURSERY_COLLECTION
} WorkersStateName;

/*
 * | state                    | num_awake | num_posted                 | post_done |
 * |--------------------------+-----------+----------------------------+-----------|
 * | STATE_NOT_WORKING        | 0         | *                          |         0 |
 * | STATE_WORKING            | > 0       | <= workers_num - num_awake |         * |
 * | STATE_NURSERY_COLLECTION | *         | <= workers_num - num_awake |         1 |
 * | STATE_NURSERY_COLLECTION | 0         | 0                          |         0 |
 */
typedef union {
	gint32 value;
	struct {
		guint state : 4; /* WorkersStateName */
		/* Number of worker threads awake. */
		guint num_awake : 8;
		/* The state of the waiting semaphore. */
		guint num_posted : 8;
		/* Whether to post `workers_done_sem` */
		guint post_done : 1;
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

static guint64 stat_workers_stolen_from_self_lock;
static guint64 stat_workers_stolen_from_self_no_lock;
static guint64 stat_workers_stolen_from_others;
static guint64 stat_workers_num_waited;

static gboolean
set_state (State old_state, State new_state)
{
	if (old_state.data.state == STATE_NURSERY_COLLECTION)
		SGEN_ASSERT (0, new_state.data.state != STATE_NOT_WORKING, "Can't go from nursery collection to not working");

	return InterlockedCompareExchange (&workers_state.value,
			new_state.value, old_state.value) == old_state.value;
}

static void
assert_not_working (State state)
{
	SGEN_ASSERT (0, state.data.state == STATE_NOT_WORKING, "Can only signal enqueue work when in no work state");
	SGEN_ASSERT (0, state.data.num_awake == 0, "No workers can be awake when not working");
	SGEN_ASSERT (0, state.data.num_posted == 0, "Can't have posted already");
	SGEN_ASSERT (0, !state.data.post_done, "post_done can only be set when working");

}

static void
assert_working (State state, gboolean from_worker)
{
	SGEN_ASSERT (0, state.data.state == STATE_WORKING, "A worker can't wait without being in working state");
	if (from_worker)
		SGEN_ASSERT (0, state.data.num_awake > 0, "How can we be awake, yet we are not counted?");
	else
		SGEN_ASSERT (0, state.data.num_awake + state.data.num_posted > 0, "How can we be working, yet no worker threads are awake or to be awoken?");
	SGEN_ASSERT (0, state.data.num_awake + state.data.num_posted <= workers_num, "There are too many worker threads awake");
}

static void
assert_nursery_collection (State state, gboolean from_worker)
{
	SGEN_ASSERT (0, state.data.state == STATE_NURSERY_COLLECTION, "Must be in the nursery collection state");
	if (from_worker) {
		SGEN_ASSERT (0, state.data.num_awake > 0, "We're awake, but num_awake is zero");
		SGEN_ASSERT (0, state.data.post_done, "post_done must be set in the nursery collection state");
	}
	SGEN_ASSERT (0, state.data.num_awake <= workers_num, "There are too many worker threads awake");
	if (!state.data.post_done) {
		SGEN_ASSERT (0, state.data.num_awake == 0, "Once done has been posted no threads can be awake");
		SGEN_ASSERT (0, state.data.num_posted == 0, "Once done has been posted no thread must be awoken");
	}
}

static void
assert_working_or_nursery_collection (State state)
{
	if (state.data.state == STATE_WORKING)
		assert_working (state, TRUE);
	else
		assert_nursery_collection (state, TRUE);
}

static void
workers_signal_enqueue_work (int num_wake_up, gboolean from_nursery_collection)
{
	State old_state = workers_state;
	State new_state = old_state;
	int i;
	gboolean did_set_state;

	SGEN_ASSERT (0, num_wake_up <= workers_num, "Cannot wake up more workers than are present");

	if (from_nursery_collection)
		assert_nursery_collection (old_state, FALSE);
	else
		assert_not_working (old_state);

	new_state.data.state = STATE_WORKING;
	new_state.data.num_posted = num_wake_up;

	did_set_state = set_state (old_state, new_state);
	SGEN_ASSERT (0, did_set_state, "Nobody else should be mutating the state");

	for (i = 0; i < num_wake_up; ++i)
		MONO_SEM_POST (&workers_waiting_sem);
}

static void
workers_signal_enqueue_work_if_necessary (int num_wake_up)
{
	if (workers_state.data.state == STATE_NOT_WORKING)
		workers_signal_enqueue_work (num_wake_up, FALSE);
}

void
sgen_workers_ensure_awake (void)
{
	SGEN_ASSERT (0, workers_state.data.state != STATE_NURSERY_COLLECTION, "Can't wake workers during nursery collection");
	workers_signal_enqueue_work_if_necessary (workers_num);
}

static void
workers_wait (void)
{
	State old_state, new_state;
	gboolean post_done;

	++stat_workers_num_waited;

	do {
		new_state = old_state = workers_state;

		assert_working_or_nursery_collection (old_state);

		--new_state.data.num_awake;
		post_done = FALSE;
		if (!new_state.data.num_awake && !new_state.data.num_posted) {
			/* We are the last thread to go to sleep. */
			if (old_state.data.state == STATE_WORKING)
				new_state.data.state = STATE_NOT_WORKING;

			new_state.data.post_done = 0;
			if (old_state.data.post_done)
				post_done = TRUE;
		}
	} while (!set_state (old_state, new_state));

	if (post_done)
		MONO_SEM_POST (&workers_done_sem);

	MONO_SEM_WAIT (&workers_waiting_sem);

	do {
		new_state = old_state = workers_state;

		SGEN_ASSERT (0, old_state.data.num_posted > 0, "How can we be awake without the semaphore having been posted?");
		SGEN_ASSERT (0, old_state.data.num_awake < workers_num, "There are too many worker threads awake");

		--new_state.data.num_posted;
		++new_state.data.num_awake;

		assert_working_or_nursery_collection (new_state);
	} while (!set_state (old_state, new_state));
}

static gboolean
collection_needs_workers (void)
{
	return sgen_collection_is_concurrent ();
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

	if (workers_state.data.state != STATE_NURSERY_COLLECTION)
		workers_signal_enqueue_work_if_necessary (num_entries < workers_num ? num_entries : workers_num);
}

void
sgen_workers_wait_for_jobs_finished (void)
{
	// FIXME: implement this properly
	while (workers_num_jobs_finished < workers_num_jobs_enqueued) {
		workers_signal_enqueue_work_if_necessary (workers_num);
		/* FIXME: sleep less? */
		g_usleep (1000);
	}
}

void
sgen_workers_signal_start_nursery_collection_and_wait (void)
{
	State old_state, new_state;

	do {
		new_state = old_state = workers_state;

		new_state.data.state = STATE_NURSERY_COLLECTION;

		if (old_state.data.state == STATE_NOT_WORKING) {
			assert_not_working (old_state);
		} else {
			assert_working (old_state, FALSE);
			SGEN_ASSERT (0, !old_state.data.post_done, "We are not waiting for the workers");

			new_state.data.post_done = 1;
		}
	} while (!set_state (old_state, new_state));

	if (new_state.data.post_done)
		MONO_SEM_WAIT (&workers_done_sem);

	old_state = workers_state;
	assert_nursery_collection (old_state, FALSE);
	SGEN_ASSERT (0, !old_state.data.post_done, "We got the semaphore, so it must have been posted");
}

void
sgen_workers_signal_finish_nursery_collection (void)
{
	State old_state = workers_state;

	assert_nursery_collection (old_state, FALSE);
	SGEN_ASSERT (0, !old_state.data.post_done, "We are finishing the nursery collection, so we should have waited for the semaphore earlier");

	workers_signal_enqueue_work (workers_num, TRUE);
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
		memcpy (queue->first->entries,
				victim_data->stealable_stack + victim_data->stealable_stack_fill - num + n,
				sizeof (GrayQueueEntry) * m);
		queue->first->size = m;

		/*
		 * DO NOT move outside this loop
		 * Doing so trigger "assert not reached" in sgen-scan-object.h : we use the queue->cursor
		 * to compute the size of the first section during section allocation (via alloc_prepare_func
		 * -> workers_gray_queue_share_redirect -> sgen_gray_object_dequeue_section) which will be then
		 * set to 0, because queue->cursor is still pointing to queue->first->entries [-1], thus
		 * losing objects in the gray queue.
		 */
		queue->cursor = queue->first->entries + queue->first->size - 1;
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
	for (i = data->index + 1; i < workers_num + data->index; ++i) {
		WorkerData *victim_data = &workers_data [i % workers_num];
		g_assert (data != victim_data);
		if (workers_steal (data, victim_data, TRUE))
			return TRUE;
	}

	/*
	 * If we're concurrent or parallel, from the workers
	 * distribute gray queue.
	 */
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
workers_gray_queue_share_redirect (SgenGrayQueue *queue)
{
	GrayQueueSection *section;
	WorkerData *data = queue->alloc_prepare_data;

	if (data->stealable_stack_fill) {
		/*
		 * There are still objects in the stealable stack, so
		 * wake up any workers that might be sleeping
		 */
		workers_signal_enqueue_work_if_necessary (workers_num);
		return;
	}

	/* The stealable stack is empty, so fill it. */
	mono_mutex_lock (&data->stealable_stack_mutex);

	while (data->stealable_stack_fill < STEALABLE_STACK_SIZE &&
			(section = sgen_gray_object_dequeue_section (queue))) {
		int num = MIN (section->size, STEALABLE_STACK_SIZE - data->stealable_stack_fill);

		memcpy (data->stealable_stack + data->stealable_stack_fill,
				section->entries + section->size - num,
				sizeof (GrayQueueEntry) * num);

		section->size -= num;
		data->stealable_stack_fill += num;

		if (section->size)
			sgen_gray_object_enqueue_section (queue, section);
		else
			sgen_gray_object_free_queue_section (section);
	}

	if (sgen_gray_object_queue_is_empty (queue))
		workers_steal (data, data, FALSE);

	mono_mutex_unlock (&data->stealable_stack_mutex);

	workers_signal_enqueue_work_if_necessary (workers_num);
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

		SGEN_ASSERT (0, sgen_get_current_collection_generation () != GENERATION_NURSERY, "Why are we doing work while there's a nursery collection happening?");

		while (workers_state.data.state == STATE_WORKING && workers_dequeue_and_do_job (data)) {
			did_work = TRUE;
			/* FIXME: maybe distribute the gray queue here? */
		}

		if (!sgen_gray_object_queue_is_empty (&data->private_gray_queue) || workers_get_work (data)) {
			SgenObjectOperations *ops = sgen_concurrent_collection_in_progress ()
				? &major->major_concurrent_ops
				: &major->major_ops;
			ScanCopyContext ctx = { ops->scan_object, NULL, &data->private_gray_queue };

			g_assert (!sgen_gray_object_queue_is_empty (&data->private_gray_queue));

			while (!sgen_drain_gray_stack (32, ctx)) {
				if (workers_state.data.state == STATE_NURSERY_COLLECTION)
					workers_wait ();

				workers_gray_queue_share_redirect (&data->private_gray_queue);
			}
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

	init_distribute_gray_queue (sgen_get_major_collector ()->is_concurrent);
}

void
sgen_workers_init (int num_workers)
{
	int i;

	if (!sgen_get_major_collector ()->is_concurrent)
		return;

	//g_print ("initing %d workers\n", num_workers);

	workers_num = num_workers;

	workers_data = sgen_alloc_internal_dynamic (sizeof (WorkerData) * num_workers, INTERNAL_MEM_WORKER_DATA, TRUE);
	memset (workers_data, 0, sizeof (WorkerData) * num_workers);

	MONO_SEM_INIT (&workers_waiting_sem, 0);
	MONO_SEM_INIT (&workers_done_sem, 0);

	init_distribute_gray_queue (sgen_get_major_collector ()->is_concurrent);

	if (sgen_get_major_collector ()->alloc_worker_data)
		workers_gc_thread_major_collector_data = sgen_get_major_collector ()->alloc_worker_data ();

	for (i = 0; i < workers_num; ++i) {
		workers_data [i].index = i;

		/* private gray queue is inited by the thread itself */
		mono_mutex_init (&workers_data [i].stealable_stack_mutex);
		workers_data [i].stealable_stack_fill = 0;

		if (sgen_get_major_collector ()->alloc_worker_data)
			workers_data [i].major_collector_data = sgen_get_major_collector ()->alloc_worker_data ();
	}

	LOCK_INIT (workers_job_queue_mutex);

	sgen_register_fixed_internal_mem_type (INTERNAL_MEM_JOB_QUEUE_ENTRY, sizeof (JobQueueEntry));

	mono_counters_register ("Stolen from self lock", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_stolen_from_self_lock);
	mono_counters_register ("Stolen from self no lock", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_stolen_from_self_no_lock);
	mono_counters_register ("Stolen from others", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_stolen_from_others);
	mono_counters_register ("# workers waited", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_workers_num_waited);
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
	gboolean result;

	if (!collection_needs_workers ())
		return;

	if (sgen_get_major_collector ()->init_worker_thread)
		sgen_get_major_collector ()->init_worker_thread (workers_gc_thread_major_collector_data);

	old_state = new_state = workers_state;
	assert_not_working (old_state);

	g_assert (workers_job_queue_num_entries == 0);
	workers_num_jobs_enqueued = 0;
	workers_num_jobs_finished = 0;

	if (workers_started) {
		workers_signal_enqueue_work (workers_num, FALSE);
		return;
	}

	new_state.data.state = STATE_WORKING;
	new_state.data.num_awake = workers_num;
	result = set_state (old_state, new_state);
	SGEN_ASSERT (0, result, "Nobody else should have modified the state - workers have not been started yet");

	for (i = 0; i < workers_num; ++i)
		workers_start_worker (i);

	workers_started = TRUE;
}

gboolean
sgen_workers_have_started (void)
{
	return workers_started;
}

void
sgen_workers_join (void)
{
	State old_state;
	int i;

	if (!collection_needs_workers ())
		return;

	for (;;) {
		old_state = workers_state;
		SGEN_ASSERT (0, old_state.data.state != STATE_NURSERY_COLLECTION, "Can't be in nursery collection when joining");

		if (old_state.data.state == STATE_WORKING) {
			State new_state = old_state;

			SGEN_ASSERT (0, !old_state.data.post_done, "Why is post_done already set?");
			new_state.data.post_done = 1;
			if (!set_state (old_state, new_state))
				continue;

			MONO_SEM_WAIT (&workers_done_sem);

			old_state = workers_state;
		}

		assert_not_working (old_state);

		/*
		 * Checking whether there is still work left and, if not, going to sleep,
		 * are two separate actions that are not performed atomically by the
		 * workers.  Therefore there's a race condition where work can be added
		 * after they've checked for work, and before they've gone to sleep.
		 */
		if (!workers_job_queue_num_entries && sgen_section_gray_queue_is_empty (&workers_distribute_gray_queue))
			break;

		workers_signal_enqueue_work (workers_num, FALSE);
	}

	/* At this point all the workers have stopped. */

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
	return workers_state.data.state == STATE_NOT_WORKING;
}

gboolean
sgen_workers_are_working (void)
{
	State state = workers_state;
	return state.data.num_awake > 0 || state.data.num_posted > 0;
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
