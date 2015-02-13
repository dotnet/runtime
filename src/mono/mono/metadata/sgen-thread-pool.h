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

typedef void (*SgenThreadPoolJobFunc) (SgenThreadPoolJob *job);

struct _SgenThreadPoolJob {
	SgenThreadPoolJobFunc func;
	volatile gint32 state;
};

void sgen_thread_pool_init (int num_threads);

void sgen_thread_pool_job_init (SgenThreadPoolJob *job, SgenThreadPoolJobFunc func);
void sgen_thread_pool_job_enqueue (SgenThreadPoolJob *job);
void sgen_thread_pool_job_wait (SgenThreadPoolJob *job);

void sgen_thread_pool_wait_for_all_jobs (void);

#endif
