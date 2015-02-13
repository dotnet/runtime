/*
 * sgen-thread-pool.c: Threadpool for all concurrent GC work.
 *
 * Copyright (C) 2015 Xamarin Inc
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

#include "mono/metadata/sgen-gc.h"
#include "mono/metadata/sgen-thread-pool.h"
#include "mono/metadata/sgen-pointer-queue.h"
#include "mono/utils/mono-mutex.h"
#include "mono/utils/mono-threads.h"

static mono_mutex_t lock;
static mono_cond_t work_cond;
static mono_cond_t done_cond;

static MonoNativeThreadId thread;

/* Only accessed with the lock held. */
static SgenPointerQueue job_queue;

enum {
	STATE_WAITING,
	STATE_IN_PROGRESS,
	STATE_DONE
};

/* Assumes that the lock is held. */
static SgenThreadPoolJob*
get_job (void)
{
	for (size_t i = 0; i < job_queue.next_slot; ++i) {
		SgenThreadPoolJob *job = job_queue.data [i];
		if (job->state == STATE_WAITING)
			return job;
	}
	return NULL;
}

/* Assumes that the lock is held. */
static void
remove_job (SgenThreadPoolJob *job)
{
	gboolean found = FALSE;
	SGEN_ASSERT (0, job->state == STATE_DONE, "Why are we removing a job that's not done?");
	for (size_t i = 0; i < job_queue.next_slot; ++i) {
		if (job_queue.data [i] == job) {
			job_queue.data [i] = NULL;
			found = TRUE;
			break;
		}
	}
	SGEN_ASSERT (0, found, "Why is the job we're trying to remove not in the queue?");
	sgen_pointer_queue_remove_nulls (&job_queue);
}

static mono_native_thread_return_t
thread_func (void *arg)
{
	mono_thread_info_register_small_id ();

	mono_mutex_lock (&lock);
	for (;;) {
		SgenThreadPoolJob *job;

		while (!(job = get_job ()))
			mono_cond_wait (&work_cond, &lock);
		SGEN_ASSERT (0, job->state == STATE_WAITING, "The job we got is in the wrong state.  Should be waiting.");
		job->state = STATE_IN_PROGRESS;
		mono_mutex_unlock (&lock);

		job->func (job);

		mono_mutex_lock (&lock);
		SGEN_ASSERT (0, job->state == STATE_IN_PROGRESS, "The job should still be in progress.");
		job->state = STATE_DONE;
		remove_job (job);
		/*
		 * Only the main GC thread will ever wait on the done condition, so we don't
		 * have to broadcast.
		 */
		mono_cond_signal (&done_cond);
	}
}

void
sgen_thread_pool_init (int num_threads)
{
	SGEN_ASSERT (0, num_threads == 1, "We only support 1 thread pool thread for now.");

	mono_mutex_init (&lock);
	mono_cond_init (&work_cond, NULL);
	mono_cond_init (&done_cond, NULL);

	mono_native_thread_create (&thread, thread_func, NULL);
}

void
sgen_thread_pool_job_init (SgenThreadPoolJob *job, SgenThreadPoolJobFunc func)
{
	job->state = STATE_WAITING;
	job->func = func;
}

void
sgen_thread_pool_job_enqueue (SgenThreadPoolJob *job)
{
	mono_mutex_lock (&lock);

	sgen_pointer_queue_add (&job_queue, job);
	/*
	 * FIXME: We could check whether there is a job in progress.  If there is, there's
	 * no need to signal the condition, at least as long as we have only one thread.
	 */
	mono_cond_signal (&work_cond);

	mono_mutex_unlock (&lock);
}

void
sgen_thread_pool_job_wait (SgenThreadPoolJob *job)
{
	mono_mutex_lock (&lock);

	while (job->state != STATE_DONE)
		mono_cond_wait (&done_cond, &lock);

	mono_mutex_unlock (&lock);
}

void
sgen_thread_pool_wait_for_all_jobs (void)
{
	mono_mutex_lock (&lock);

	while (!sgen_pointer_queue_is_empty (&job_queue))
		mono_cond_wait (&done_cond, &lock);

	mono_mutex_unlock (&lock);
}

#endif
