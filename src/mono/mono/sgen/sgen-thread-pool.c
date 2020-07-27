/**
 * \file
 * Threadpool for all concurrent GC work.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-thread-pool.h"
#include "mono/sgen/sgen-client.h"
#include "mono/utils/mono-os-mutex.h"


#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
static mono_mutex_t lock;
static mono_cond_t work_cond;
static mono_cond_t done_cond;

static int threads_num;
static MonoNativeThreadId threads [SGEN_THREADPOOL_MAX_NUM_THREADS];
static int threads_context [SGEN_THREADPOOL_MAX_NUM_THREADS];

static volatile gboolean threadpool_shutdown;
static volatile int threads_finished;

static int contexts_num;
static SgenThreadPoolContext pool_contexts [SGEN_THREADPOOL_MAX_NUM_CONTEXTS];

enum {
	STATE_WAITING,
	STATE_IN_PROGRESS,
	STATE_DONE
};

/* Assumes that the lock is held. */
static SgenThreadPoolJob*
get_job_and_set_in_progress (SgenThreadPoolContext *context)
{
	for (size_t i = 0; i < context->job_queue.next_slot; ++i) {
		SgenThreadPoolJob *job = (SgenThreadPoolJob *)context->job_queue.data [i];
		if (job->state == STATE_WAITING) {
			job->state = STATE_IN_PROGRESS;
			return job;
		}
	}
	return NULL;
}

/* Assumes that the lock is held. */
static ssize_t
find_job_in_queue (SgenThreadPoolContext *context, SgenThreadPoolJob *job)
{
	for (ssize_t i = 0; i < context->job_queue.next_slot; ++i) {
		if (context->job_queue.data [i] == job)
			return i;
	}
	return -1;
}

/* Assumes that the lock is held. */
static void
remove_job (SgenThreadPoolContext *context, SgenThreadPoolJob *job)
{
	ssize_t index;
	SGEN_ASSERT (0, job->state == STATE_DONE, "Why are we removing a job that's not done?");
	index = find_job_in_queue (context, job);
	SGEN_ASSERT (0, index >= 0, "Why is the job we're trying to remove not in the queue?");
	context->job_queue.data [index] = NULL;
	sgen_pointer_queue_remove_nulls (&context->job_queue);
	sgen_thread_pool_job_free (job);
}

static gboolean
continue_idle_job (SgenThreadPoolContext *context, void *thread_data)
{
	if (!context->continue_idle_job_func)
		return FALSE;
	return context->continue_idle_job_func (thread_data, context - pool_contexts);
}

static gboolean
should_work (SgenThreadPoolContext *context, void *thread_data)
{
	if (!context->should_work_func)
		return TRUE;
	return context->should_work_func (thread_data);
}

/*
 * Tells whether we should lock and attempt to get work from
 * a higher priority context.
 */
static gboolean
has_priority_work (int worker_index, int current_context)
{
	int i;

	for (i = 0; i < current_context; i++) {
		SgenThreadPoolContext *context = &pool_contexts [i];
		void *thread_data;

		if (worker_index >= context->num_threads)
			continue;
		thread_data = (context->thread_datas) ? context->thread_datas [worker_index] : NULL;
		if (!should_work (context, thread_data))
			continue;
		if (context->job_queue.next_slot > 0)
			return TRUE;
		if (continue_idle_job (context, thread_data))
			return TRUE;
	}

	/* Return if job enqueued on current context. Jobs have priority over idle work */
	if (pool_contexts [current_context].job_queue.next_slot > 0)
		return TRUE;

	return FALSE;
}

/*
 * Gets the highest priority work. If there is none, it waits
 * for work_cond. Should always be called with lock held.
 */
static void
get_work (int worker_index, int *work_context, int *do_idle, SgenThreadPoolJob **job)
{
	while (!threadpool_shutdown) {
		int i;

		for (i = 0; i < contexts_num; i++) {
			SgenThreadPoolContext *context = &pool_contexts [i];
			void *thread_data;

			if (worker_index >= context->num_threads)
				continue;
			thread_data = (context->thread_datas) ? context->thread_datas [worker_index] : NULL;

			if (!should_work (context, thread_data))
				continue;

			/*
			 * It's important that we check the continue idle flag with the lock held.
			 * Suppose we didn't check with the lock held, and the result is FALSE.  The
			 * main thread might then set continue idle and signal us before we can take
			 * the lock, and we'd lose the signal.
			 */
			*do_idle = continue_idle_job (context, thread_data);
			*job = get_job_and_set_in_progress (context);

			if (*job || *do_idle) {
				*work_context = i;
				return;
			}
		}

		/*
		 * Nothing to do on any context
		 * pthread_cond_wait() can return successfully despite the condition
		 * not being signalled, so we have to run this in a loop until we
		 * really have work to do.
		 */
		mono_os_cond_wait (&work_cond, &lock);
	}
}

static mono_native_thread_return_t
thread_func (void *data)
{
	int worker_index = (int)(gsize)data;
	int current_context;
	void *thread_data = NULL;

	sgen_client_thread_register_worker ();

	for (current_context = 0; current_context < contexts_num; current_context++) {
		if (worker_index >= pool_contexts [current_context].num_threads ||
				!pool_contexts [current_context].thread_init_func)
			break;

		thread_data = (pool_contexts [current_context].thread_datas) ? pool_contexts [current_context].thread_datas [worker_index] : NULL;
		pool_contexts [current_context].thread_init_func (thread_data);
	}

	current_context = 0;

	mono_os_mutex_lock (&lock);
	for (;;) {
		gboolean do_idle = FALSE;
		SgenThreadPoolJob *job = NULL;
		SgenThreadPoolContext *context = NULL;

		threads_context [worker_index] = -1;
		get_work (worker_index, &current_context, &do_idle, &job);
		threads_context [worker_index] = current_context;

		if (!threadpool_shutdown) {
			context = &pool_contexts [current_context];
			thread_data = (context->thread_datas) ? context->thread_datas [worker_index] : NULL;
		}

		mono_os_mutex_unlock (&lock);

		if (job) {
			job->func (thread_data, job);

			mono_os_mutex_lock (&lock);

			SGEN_ASSERT (0, job->state == STATE_IN_PROGRESS, "The job should still be in progress.");
			job->state = STATE_DONE;
			remove_job (context, job);
			/*
			 * Only the main GC thread will ever wait on the done condition, so we don't
			 * have to broadcast.
			 */
			mono_os_cond_signal (&done_cond);
		} else if (do_idle) {
			SGEN_ASSERT (0, context->idle_job_func, "Why do we have idle work when there's no idle job function?");
			do {
				context->idle_job_func (thread_data);
				do_idle = continue_idle_job (context, thread_data);
			} while (do_idle && !has_priority_work (worker_index, current_context));

			mono_os_mutex_lock (&lock);

			if (!do_idle)
				mono_os_cond_signal (&done_cond);
		} else {
			SGEN_ASSERT (0, threadpool_shutdown, "Why did we unlock if no jobs and not shutting down?");
			mono_os_mutex_lock (&lock);
			threads_finished++;
			mono_os_cond_signal (&done_cond);
			mono_os_mutex_unlock (&lock);
			return 0;
		}
	}

	return 0;
}

int
sgen_thread_pool_create_context (int num_threads, SgenThreadPoolThreadInitFunc init_func, SgenThreadPoolIdleJobFunc idle_func, SgenThreadPoolContinueIdleJobFunc continue_idle_func, SgenThreadPoolShouldWorkFunc should_work_func, void **thread_datas)
{
	int context_id = contexts_num;

	SGEN_ASSERT (0, contexts_num < SGEN_THREADPOOL_MAX_NUM_CONTEXTS, "Maximum sgen thread pool contexts reached");

	pool_contexts [context_id].thread_init_func = init_func;
	pool_contexts [context_id].idle_job_func = idle_func;
	pool_contexts [context_id].continue_idle_job_func = continue_idle_func;
	pool_contexts [context_id].should_work_func = should_work_func;
	pool_contexts [context_id].thread_datas = thread_datas;

	SGEN_ASSERT (0, num_threads <= SGEN_THREADPOOL_MAX_NUM_THREADS, "Maximum sgen thread pool threads exceeded");

	pool_contexts [context_id].num_threads = num_threads;

	sgen_pointer_queue_init (&pool_contexts [contexts_num].job_queue, 0);

	contexts_num++;

	return context_id;
}

void
sgen_thread_pool_start (void)
{
	int i;

	for (i = 0; i < contexts_num; i++) {
		if (threads_num < pool_contexts [i].num_threads)
			threads_num = pool_contexts [i].num_threads;
	}

	if (!threads_num)
		return;

	mono_os_mutex_init (&lock);
	mono_os_cond_init (&work_cond);
	mono_os_cond_init (&done_cond);

	threads_finished = 0;
	threadpool_shutdown = FALSE;

	for (i = 0; i < threads_num; i++) {
		mono_native_thread_create (&threads [i], (gpointer)thread_func, (void*)(gsize)i);
	}
}

void
sgen_thread_pool_shutdown (void)
{
	if (!threads_num)
		return;

	mono_os_mutex_lock (&lock);
	threadpool_shutdown = TRUE;
	mono_os_cond_broadcast (&work_cond);
	while (threads_finished < threads_num)
		mono_os_cond_wait (&done_cond, &lock);
	mono_os_mutex_unlock (&lock);

	mono_os_mutex_destroy (&lock);
	mono_os_cond_destroy (&work_cond);
	mono_os_cond_destroy (&done_cond);

	for (int i = 0; i < threads_num; i++) {
		mono_threads_add_joinable_thread ((gpointer)(gsize)threads [i]);
	}
}

SgenThreadPoolJob*
sgen_thread_pool_job_alloc (const char *name, SgenThreadPoolJobFunc func, size_t size)
{
	SgenThreadPoolJob *job = (SgenThreadPoolJob *)sgen_alloc_internal_dynamic (size, INTERNAL_MEM_THREAD_POOL_JOB, TRUE);
	job->name = name;
	job->size = size;
	job->state = STATE_WAITING;
	job->func = func;
	return job;
}

void
sgen_thread_pool_job_free (SgenThreadPoolJob *job)
{
	sgen_free_internal_dynamic (job, job->size, INTERNAL_MEM_THREAD_POOL_JOB);
}

void
sgen_thread_pool_job_enqueue (int context_id, SgenThreadPoolJob *job)
{
	mono_os_mutex_lock (&lock);

	sgen_pointer_queue_add (&pool_contexts [context_id].job_queue, job);
	mono_os_cond_broadcast (&work_cond);

	mono_os_mutex_unlock (&lock);
}

void
sgen_thread_pool_job_wait (int context_id, SgenThreadPoolJob *job)
{
	SGEN_ASSERT (0, job, "Where's the job?");

	mono_os_mutex_lock (&lock);

	while (find_job_in_queue (&pool_contexts [context_id], job) >= 0)
		mono_os_cond_wait (&done_cond, &lock);

	mono_os_mutex_unlock (&lock);
}

void
sgen_thread_pool_idle_signal (int context_id)
{
	SGEN_ASSERT (0, pool_contexts [context_id].idle_job_func, "Why are we signaling idle without an idle function?");

	mono_os_mutex_lock (&lock);

	if (pool_contexts [context_id].continue_idle_job_func (NULL, context_id))
		mono_os_cond_broadcast (&work_cond);

	mono_os_mutex_unlock (&lock);
}

void
sgen_thread_pool_idle_wait (int context_id, SgenThreadPoolContinueIdleWaitFunc continue_wait)
{
	SGEN_ASSERT (0, pool_contexts [context_id].idle_job_func, "Why are we waiting for idle without an idle function?");

	mono_os_mutex_lock (&lock);

	while (continue_wait (context_id, threads_context))
		mono_os_cond_wait (&done_cond, &lock);

	mono_os_mutex_unlock (&lock);
}

void
sgen_thread_pool_wait_for_all_jobs (int context_id)
{
	mono_os_mutex_lock (&lock);

	while (!sgen_pointer_queue_is_empty (&pool_contexts [context_id].job_queue))
		mono_os_cond_wait (&done_cond, &lock);

	mono_os_mutex_unlock (&lock);
}

/* Return 0 if is not a thread pool thread or the thread number otherwise */
int
sgen_thread_pool_is_thread_pool_thread (MonoNativeThreadId some_thread)
{
	int i;

	for (i = 0; i < threads_num; i++) {
		if (some_thread == threads [i])
			return i + 1;
	}

	return 0;
}
#else

int
sgen_thread_pool_create_context (int num_threads, SgenThreadPoolThreadInitFunc init_func, SgenThreadPoolIdleJobFunc idle_func, SgenThreadPoolContinueIdleJobFunc continue_idle_func, SgenThreadPoolShouldWorkFunc should_work_func, void **thread_datas)
{
	return 0;
}

void
sgen_thread_pool_start (void)
{
}

void
sgen_thread_pool_shutdown (void)
{
}

SgenThreadPoolJob*
sgen_thread_pool_job_alloc (const char *name, SgenThreadPoolJobFunc func, size_t size)
{
	SgenThreadPoolJob *job = (SgenThreadPoolJob *)sgen_alloc_internal_dynamic (size, INTERNAL_MEM_THREAD_POOL_JOB, TRUE);
	job->name = name;
	job->size = size;
	job->func = func;
	return job;
}

void
sgen_thread_pool_job_free (SgenThreadPoolJob *job)
{
	sgen_free_internal_dynamic (job, job->size, INTERNAL_MEM_THREAD_POOL_JOB);
}

void
sgen_thread_pool_job_enqueue (int context_id, SgenThreadPoolJob *job)
{
}

void
sgen_thread_pool_job_wait (int context_id, SgenThreadPoolJob *job)
{
}

void
sgen_thread_pool_idle_signal (int context_id)
{
}

void
sgen_thread_pool_idle_wait (int context_id, SgenThreadPoolContinueIdleWaitFunc continue_wait)
{
}

void
sgen_thread_pool_wait_for_all_jobs (int context_id)
{
}

int
sgen_thread_pool_is_thread_pool_thread (MonoNativeThreadId some_thread)
{
	return 0;
}

#endif

#endif
