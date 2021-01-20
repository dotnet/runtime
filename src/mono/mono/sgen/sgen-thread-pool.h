/**
 * \file
 * Threadpool for all concurrent GC work.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_SGEN_THREAD_POOL_H__
#define __MONO_SGEN_THREAD_POOL_H__

#include "mono/sgen/sgen-pointer-queue.h"
#include "mono/utils/mono-threads.h"

#define SGEN_THREADPOOL_MAX_NUM_THREADS 8
#define SGEN_THREADPOOL_MAX_NUM_CONTEXTS 3

typedef struct _SgenThreadPoolJob SgenThreadPoolJob;
typedef struct _SgenThreadPoolContext SgenThreadPoolContext;

typedef void (*SgenThreadPoolJobFunc) (void *thread_data, SgenThreadPoolJob *job);
typedef void (*SgenThreadPoolThreadInitFunc) (void*);
typedef void (*SgenThreadPoolIdleJobFunc) (void*);
typedef gboolean (*SgenThreadPoolContinueIdleJobFunc) (void*, int);
typedef gboolean (*SgenThreadPoolShouldWorkFunc) (void*);
typedef gboolean (*SgenThreadPoolContinueIdleWaitFunc) (int, int*);

struct _SgenThreadPoolJob {
	const char *name;
	SgenThreadPoolJobFunc func;
	size_t size;
	volatile gint32 state;
};

struct _SgenThreadPoolContext {
	/* Only accessed with the lock held. */
	SgenPointerQueue job_queue;

	/*
	 * LOCKING: Assumes the GC lock is held.
	 */
	void **deferred_jobs;
	int deferred_jobs_len;
	int deferred_jobs_count;

	SgenThreadPoolThreadInitFunc thread_init_func;
	SgenThreadPoolIdleJobFunc idle_job_func;
	SgenThreadPoolContinueIdleJobFunc continue_idle_job_func;
	SgenThreadPoolShouldWorkFunc should_work_func;

	void **thread_datas;
	int num_threads;
};


int sgen_thread_pool_create_context (int num_threads, SgenThreadPoolThreadInitFunc init_func, SgenThreadPoolIdleJobFunc idle_func, SgenThreadPoolContinueIdleJobFunc continue_idle_func, SgenThreadPoolShouldWorkFunc should_work_func, void **thread_datas);
void sgen_thread_pool_start (void);

void sgen_thread_pool_shutdown (void);

SgenThreadPoolJob* sgen_thread_pool_job_alloc (const char *name, SgenThreadPoolJobFunc func, size_t size);
/* This only needs to be called on jobs that are not enqueued. */
void sgen_thread_pool_job_free (SgenThreadPoolJob *job);

void sgen_thread_pool_job_enqueue (int context_id, SgenThreadPoolJob *job);

/*
 * LOCKING: Assumes the GC lock is held.
 */
void sgen_thread_pool_job_enqueue_deferred (int context_id, SgenThreadPoolJob *job);

/*
 * LOCKING: Assumes the GC lock is held.
 */
void sgen_thread_pool_flush_deferred_jobs (int context_id, gboolean signal);

gboolean sgen_thread_pool_have_deferred_jobs (int context_id);

/* This must only be called after the job has been enqueued. */
void sgen_thread_pool_job_wait (int context_id, SgenThreadPoolJob *job);

void sgen_thread_pool_idle_signal (int context_id);
void sgen_thread_pool_idle_wait (int context_id, SgenThreadPoolContinueIdleWaitFunc continue_wait);

void sgen_thread_pool_wait_for_all_jobs (int context_id);

int sgen_thread_pool_is_thread_pool_thread (MonoNativeThreadId thread);

#endif
