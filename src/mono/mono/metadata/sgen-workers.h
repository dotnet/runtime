/*
 * sgen-workers.c: Worker threads for parallel and concurrent GC.
 *
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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

#ifndef __MONO_SGEN_WORKER_H__
#define __MONO_SGEN_WORKER_H__

#define STEALABLE_STACK_SIZE	512


typedef struct _WorkerData WorkerData;
struct _WorkerData {
	MonoNativeThreadId thread;
	void *major_collector_data;

	SgenGrayQueue private_gray_queue; /* only read/written by worker thread */

	mono_mutex_t stealable_stack_mutex;
	volatile int stealable_stack_fill;
	char *stealable_stack [STEALABLE_STACK_SIZE];
};

typedef void (*JobFunc) (WorkerData *worker_data, void *job_data);

typedef struct _JobQueueEntry JobQueueEntry;
struct _JobQueueEntry {
	JobFunc func;
	void *data;

	volatile JobQueueEntry *next;
};

void sgen_workers_init (int num_workers) MONO_INTERNAL;
void sgen_workers_start_all_workers (void) MONO_INTERNAL;
gboolean sgen_workers_have_started (void) MONO_INTERNAL;
void sgen_workers_wake_up_all (void) MONO_INTERNAL;
void sgen_workers_init_distribute_gray_queue (void) MONO_INTERNAL;
void sgen_workers_enqueue_job (JobFunc func, void *data) MONO_INTERNAL;
void sgen_workers_wait_for_jobs (void) MONO_INTERNAL;
void sgen_workers_start_marking (void) MONO_INTERNAL;
void sgen_workers_distribute_gray_queue_sections (void) MONO_INTERNAL;
void sgen_workers_reset_data (void) MONO_INTERNAL;
void sgen_workers_join (void) MONO_INTERNAL;
gboolean sgen_workers_all_done (void) MONO_INTERNAL;
SgenSectionGrayQueue* sgen_workers_get_distribute_section_gray_queue (void) MONO_INTERNAL;

#endif
