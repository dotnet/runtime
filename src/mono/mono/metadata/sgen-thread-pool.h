/*
 * sgen-thread-pool.h: Threadpool for all concurrent GC work.
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

#ifndef __MONO_SGEN_THREAD_POOL_H__
#define __MONO_SGEN_THREAD_POOL_H__

typedef struct _SgenThreadPoolJob SgenThreadPoolJob;

typedef void (*SgenThreadPoolJobFunc) (void *thread_data, SgenThreadPoolJob *job);

struct _SgenThreadPoolJob {
	const char *name;
	SgenThreadPoolJobFunc func;
	size_t size;
	volatile gint32 state;
};

typedef void (*SgenThreadPoolThreadInitFunc) (void*);
typedef void (*SgenThreadPoolIdleJobFunc) (void*);
typedef gboolean (*SgenThreadPoolContinueIdleJobFunc) (void);

void sgen_thread_pool_init (int num_threads, SgenThreadPoolThreadInitFunc init_func, SgenThreadPoolIdleJobFunc idle_func, SgenThreadPoolContinueIdleJobFunc continue_idle_func, void **thread_datas);

SgenThreadPoolJob* sgen_thread_pool_job_alloc (const char *name, SgenThreadPoolJobFunc func, size_t size);
/* This only needs to be called on jobs that are not enqueued. */
void sgen_thread_pool_job_free (SgenThreadPoolJob *job);

void sgen_thread_pool_job_enqueue (SgenThreadPoolJob *job);
/* This must only be called after the job has been enqueued. */
void sgen_thread_pool_job_wait (SgenThreadPoolJob *job);

void sgen_thread_pool_idle_signal (void);
void sgen_thread_pool_idle_wait (void);

void sgen_thread_pool_wait_for_all_jobs (void);

gboolean sgen_thread_pool_is_thread_pool_thread (MonoNativeThreadId thread);

#endif
