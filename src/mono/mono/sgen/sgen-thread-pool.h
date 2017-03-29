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

typedef struct _SgenThreadPoolJob SgenThreadPoolJob;
typedef struct _SgenThreadPool SgenThreadPool;
typedef struct _SgenThreadPoolData SgenThreadPoolData;

typedef void (*SgenThreadPoolJobFunc) (void *thread_data, SgenThreadPoolJob *job);
typedef void (*SgenThreadPoolThreadInitFunc) (void*);
typedef void (*SgenThreadPoolIdleJobFunc) (void*);
typedef gboolean (*SgenThreadPoolContinueIdleJobFunc) (void*);
typedef gboolean (*SgenThreadPoolShouldWorkFunc) (void*);

struct _SgenThreadPoolJob {
	const char *name;
	SgenThreadPoolJobFunc func;
	size_t size;
	volatile gint32 state;
};

#define MAX_NUM_THREADS 8

struct _SgenThreadPool {
	mono_mutex_t lock;
	mono_cond_t work_cond;
	mono_cond_t done_cond;

	int threads_num;
	MonoNativeThreadId threads [MAX_NUM_THREADS];

	/* Only accessed with the lock held. */
	SgenPointerQueue job_queue;

	SgenThreadPoolThreadInitFunc thread_init_func;
	SgenThreadPoolIdleJobFunc idle_job_func;
	SgenThreadPoolContinueIdleJobFunc continue_idle_job_func;
	SgenThreadPoolShouldWorkFunc should_work_func;

	volatile gboolean threadpool_shutdown;
	volatile int threads_finished;
};

struct _SgenThreadPoolData {
	SgenThreadPool *pool;
};

void sgen_thread_pool_init (SgenThreadPool *pool, int num_threads, SgenThreadPoolThreadInitFunc init_func, SgenThreadPoolIdleJobFunc idle_func, SgenThreadPoolContinueIdleJobFunc continue_idle_func, SgenThreadPoolShouldWorkFunc should_work_func, SgenThreadPoolData **thread_datas);

void sgen_thread_pool_shutdown (SgenThreadPool *pool);

SgenThreadPoolJob* sgen_thread_pool_job_alloc (const char *name, SgenThreadPoolJobFunc func, size_t size);
/* This only needs to be called on jobs that are not enqueued. */
void sgen_thread_pool_job_free (SgenThreadPoolJob *job);

void sgen_thread_pool_job_enqueue (SgenThreadPool *pool, SgenThreadPoolJob *job);
/* This must only be called after the job has been enqueued. */
void sgen_thread_pool_job_wait (SgenThreadPool *pool, SgenThreadPoolJob *job);

void sgen_thread_pool_idle_signal (SgenThreadPool *pool);
void sgen_thread_pool_idle_wait (SgenThreadPool *pool);

void sgen_thread_pool_wait_for_all_jobs (SgenThreadPool *pool);

int sgen_thread_pool_is_thread_pool_thread (SgenThreadPool *pool, MonoNativeThreadId thread);

#endif
