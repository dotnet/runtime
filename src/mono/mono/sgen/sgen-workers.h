/*
 * sgen-workers.c: Worker threads for parallel and concurrent GC.
 *
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_SGEN_WORKER_H__
#define __MONO_SGEN_WORKER_H__

#include "mono/sgen/sgen-thread-pool.h"

typedef struct _WorkerData WorkerData;
struct _WorkerData {
	gint32 state;
	SgenGrayQueue private_gray_queue; /* only read/written by worker thread */
};

void sgen_workers_init (int num_workers);
void sgen_workers_stop_all_workers (void);
void sgen_workers_start_all_workers (SgenObjectOperations *object_ops, SgenThreadPoolJob *finish_job);
void sgen_workers_init_distribute_gray_queue (void);
void sgen_workers_enqueue_job (SgenThreadPoolJob *job, gboolean enqueue);
void sgen_workers_wait_for_jobs_finished (void);
void sgen_workers_distribute_gray_queue_sections (void);
void sgen_workers_reset_data (void);
void sgen_workers_join (void);
gboolean sgen_workers_have_idle_work (void);
gboolean sgen_workers_all_done (void);
gboolean sgen_workers_are_working (void);
void sgen_workers_assert_gray_queue_is_empty (void);
void sgen_workers_take_from_queue_and_awake (SgenGrayQueue *queue);

#endif
