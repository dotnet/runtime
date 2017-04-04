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
#include "mono/utils/mono-os-mutex.h"

enum {
	STATE_WAITING,
	STATE_IN_PROGRESS,
	STATE_DONE
};

/* Assumes that the lock is held. */
static SgenThreadPoolJob*
get_job_and_set_in_progress (SgenThreadPool *pool)
{
	for (size_t i = 0; i < pool->job_queue.next_slot; ++i) {
		SgenThreadPoolJob *job = (SgenThreadPoolJob *)pool->job_queue.data [i];
		if (job->state == STATE_WAITING) {
			job->state = STATE_IN_PROGRESS;
			return job;
		}
	}
	return NULL;
}

/* Assumes that the lock is held. */
static ssize_t
find_job_in_queue (SgenThreadPool *pool, SgenThreadPoolJob *job)
{
	for (ssize_t i = 0; i < pool->job_queue.next_slot; ++i) {
		if (pool->job_queue.data [i] == job)
			return i;
	}
	return -1;
}

/* Assumes that the lock is held. */
static void
remove_job (SgenThreadPool *pool, SgenThreadPoolJob *job)
{
	ssize_t index;
	SGEN_ASSERT (0, job->state == STATE_DONE, "Why are we removing a job that's not done?");
	index = find_job_in_queue (pool, job);
	SGEN_ASSERT (0, index >= 0, "Why is the job we're trying to remove not in the queue?");
	pool->job_queue.data [index] = NULL;
	sgen_pointer_queue_remove_nulls (&pool->job_queue);
	sgen_thread_pool_job_free (job);
}

static gboolean
continue_idle_job (SgenThreadPool *pool, void *thread_data)
{
	if (!pool->continue_idle_job_func)
		return FALSE;
	return pool->continue_idle_job_func (thread_data);
}

static gboolean
should_work (SgenThreadPool *pool, void *thread_data)
{
	if (!pool->should_work_func)
		return TRUE;
	return pool->should_work_func (thread_data);
}

static mono_native_thread_return_t
thread_func (SgenThreadPoolData *thread_data)
{
	SgenThreadPool *pool = thread_data->pool;

	pool->thread_init_func (thread_data);

	mono_os_mutex_lock (&pool->lock);
	for (;;) {
		gboolean do_idle;
		SgenThreadPoolJob *job;

		if (!should_work (pool, thread_data) && !pool->threadpool_shutdown) {
			mono_os_cond_wait (&pool->work_cond, &pool->lock);
			continue;
		}
		/*
		 * It's important that we check the continue idle flag with the lock held.
		 * Suppose we didn't check with the lock held, and the result is FALSE.  The
		 * main thread might then set continue idle and signal us before we can take
		 * the lock, and we'd lose the signal.
		 */
		do_idle = continue_idle_job (pool, thread_data);
		job = get_job_and_set_in_progress (pool);

		if (!job && !do_idle && !pool->threadpool_shutdown) {
			/*
			 * pthread_cond_wait() can return successfully despite the condition
			 * not being signalled, so we have to run this in a loop until we
			 * really have work to do.
			 */
			mono_os_cond_wait (&pool->work_cond, &pool->lock);
			continue;
		}

		mono_os_mutex_unlock (&pool->lock);

		if (job) {
			job->func (thread_data, job);

			mono_os_mutex_lock (&pool->lock);

			SGEN_ASSERT (0, job->state == STATE_IN_PROGRESS, "The job should still be in progress.");
			job->state = STATE_DONE;
			remove_job (pool, job);
			/*
			 * Only the main GC thread will ever wait on the done condition, so we don't
			 * have to broadcast.
			 */
			mono_os_cond_signal (&pool->done_cond);
		} else if (do_idle) {
			SGEN_ASSERT (0, pool->idle_job_func, "Why do we have idle work when there's no idle job function?");
			do {
				pool->idle_job_func (thread_data);
				do_idle = continue_idle_job (pool, thread_data);
			} while (do_idle && !pool->job_queue.next_slot);

			mono_os_mutex_lock (&pool->lock);

			if (!do_idle)
				mono_os_cond_signal (&pool->done_cond);
		} else {
			SGEN_ASSERT (0, pool->threadpool_shutdown, "Why did we unlock if no jobs and not shutting down?");
			mono_os_mutex_lock (&pool->lock);
			pool->threads_finished++;
			mono_os_cond_signal (&pool->done_cond);
			mono_os_mutex_unlock (&pool->lock);
			return 0;
		}
	}

	return (mono_native_thread_return_t)0;
}

void
sgen_thread_pool_init (SgenThreadPool *pool, int num_threads, SgenThreadPoolThreadInitFunc init_func, SgenThreadPoolIdleJobFunc idle_func, SgenThreadPoolContinueIdleJobFunc continue_idle_func, SgenThreadPoolShouldWorkFunc should_work_func_p, SgenThreadPoolData **thread_datas)
{
	int i;

	SGEN_ASSERT (0, num_threads > 0, "Why are we creating a threadpool with no threads?");

	pool->threads_num = (num_threads < MAX_NUM_THREADS) ? num_threads : MAX_NUM_THREADS;

	mono_os_mutex_init (&pool->lock);
	mono_os_cond_init (&pool->work_cond);
	mono_os_cond_init (&pool->done_cond);

	pool->thread_init_func = init_func;
	pool->idle_job_func = idle_func;
	pool->continue_idle_job_func = continue_idle_func;
	pool->should_work_func = should_work_func_p;

	sgen_pointer_queue_init (&pool->job_queue, 0);
	pool->threads_finished = 0;
	pool->threadpool_shutdown = FALSE;

	for (i = 0; i < pool->threads_num; i++) {
		thread_datas [i]->pool = pool;
		mono_native_thread_create (&pool->threads [i], thread_func, thread_datas [i]);
	}
}

void
sgen_thread_pool_shutdown (SgenThreadPool *pool)
{
	if (!pool)
		return;

	mono_os_mutex_lock (&pool->lock);
	pool->threadpool_shutdown = TRUE;
	mono_os_cond_broadcast (&pool->work_cond);
	while (pool->threads_finished < pool->threads_num)
		mono_os_cond_wait (&pool->done_cond, &pool->lock);
	mono_os_mutex_unlock (&pool->lock);

	mono_os_mutex_destroy (&pool->lock);
	mono_os_cond_destroy (&pool->work_cond);
	mono_os_cond_destroy (&pool->done_cond);
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
sgen_thread_pool_job_enqueue (SgenThreadPool *pool, SgenThreadPoolJob *job)
{
	mono_os_mutex_lock (&pool->lock);

	sgen_pointer_queue_add (&pool->job_queue, job);
	mono_os_cond_signal (&pool->work_cond);

	mono_os_mutex_unlock (&pool->lock);
}

void
sgen_thread_pool_job_wait (SgenThreadPool *pool, SgenThreadPoolJob *job)
{
	SGEN_ASSERT (0, job, "Where's the job?");

	mono_os_mutex_lock (&pool->lock);

	while (find_job_in_queue (pool, job) >= 0)
		mono_os_cond_wait (&pool->done_cond, &pool->lock);

	mono_os_mutex_unlock (&pool->lock);
}

void
sgen_thread_pool_idle_signal (SgenThreadPool *pool)
{
	SGEN_ASSERT (0, pool->idle_job_func, "Why are we signaling idle without an idle function?");

	mono_os_mutex_lock (&pool->lock);

	if (pool->continue_idle_job_func (NULL))
		mono_os_cond_broadcast (&pool->work_cond);

	mono_os_mutex_unlock (&pool->lock);
}

void
sgen_thread_pool_idle_wait (SgenThreadPool *pool)
{
	SGEN_ASSERT (0, pool->idle_job_func, "Why are we waiting for idle without an idle function?");

	mono_os_mutex_lock (&pool->lock);

	while (pool->continue_idle_job_func (NULL))
		mono_os_cond_wait (&pool->done_cond, &pool->lock);

	mono_os_mutex_unlock (&pool->lock);
}

void
sgen_thread_pool_wait_for_all_jobs (SgenThreadPool *pool)
{
	mono_os_mutex_lock (&pool->lock);

	while (!sgen_pointer_queue_is_empty (&pool->job_queue))
		mono_os_cond_wait (&pool->done_cond, &pool->lock);

	mono_os_mutex_unlock (&pool->lock);
}

/* Return 0 if is not a thread pool thread or the thread number otherwise */
int
sgen_thread_pool_is_thread_pool_thread (SgenThreadPool *pool, MonoNativeThreadId some_thread)
{
	int i;

	if (!pool)
		return 0;

	for (i = 0; i < pool->threads_num; i++) {
		if (some_thread == pool->threads [i])
			return i + 1;
	}

	return 0;
}

#endif
