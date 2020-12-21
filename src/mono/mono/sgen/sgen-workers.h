/**
 * \file
 * Worker threads for parallel and concurrent GC.
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
typedef struct _WorkerContext WorkerContext;

typedef gint32 State;

typedef void (*SgenWorkersFinishCallback) (void);
typedef void (*SgenWorkerCallback) (WorkerData *data);

struct _WorkerData {
	gint32 state;
	SgenGrayQueue private_gray_queue; /* only read/written by worker thread */
	/*
	 * Workers allocate major objects only from here. It has same structure as the
	 * global one. This is normally accessed from the worker_block_free_list_key.
	 * We hold it here so we can clear free lists from all threads before sweep
	 * starts.
	 */
	gpointer free_block_lists;
	WorkerContext *context;

	/* Work time distribution. Measured in ticks. */
	gint64 major_scan_time, los_scan_time, total_time;
	/*
	 * When changing the state of the worker from not working to work enqueued
	 * we set the timestamp so we can compute for how long the worker did actual
	 * work during the phase
	 */
	gint64 last_start;
};

struct _WorkerContext {
	int workers_num;
	int active_workers_num;
	volatile gboolean started;
	volatile gboolean forced_stop;
	WorkerData *workers_data;

	/*
	 * When using multiple workers, we need to have the last worker
	 * enqueue the preclean jobs (if there are any). This lock ensures
	 * that when the last worker takes it, all the other workers have
	 * gracefully finished, so it can restart them.
	 */
	mono_mutex_t finished_lock;
	volatile gboolean workers_finished;
	int worker_awakenings;

	SgenSectionGrayQueue workers_distribute_gray_queue;

	SgenObjectOperations * volatile idle_func_object_ops;
	SgenObjectOperations *idle_func_object_ops_par, *idle_func_object_ops_nopar;

	/*
	 * finished_callback is called only when the workers finish work normally (when they
	 * are not forced to finish). The callback is used to enqueue preclean jobs.
	 */
	volatile SgenWorkersFinishCallback finish_callback;

	int generation;
	int thread_pool_context;
};

void sgen_workers_create_context (int generation, int num_workers);
void sgen_workers_stop_all_workers (int generation);
void sgen_workers_set_num_active_workers (int generation, int num_workers);
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
void sgen_workers_start_all_workers (int generation, SgenObjectOperations *object_ops_nopar, SgenObjectOperations *object_ops_par, SgenWorkersFinishCallback finish_job);
#else
#define sgen_workers_start_all_workers(...)
#endif
void sgen_workers_enqueue_job (int generation, SgenThreadPoolJob *job, gboolean enqueue);
void sgen_workers_join (int generation);
gboolean sgen_workers_have_idle_work (int generation);
gboolean sgen_workers_all_done (void);
void sgen_workers_assert_gray_queue_is_empty (int generation);
void sgen_workers_take_from_queue (int generation, SgenGrayQueue *queue);
SgenObjectOperations* sgen_workers_get_idle_func_object_ops (WorkerData *worker);
int sgen_workers_get_job_split_count (int generation);
int sgen_workers_get_active_worker_count (int generation);
#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC
void sgen_workers_foreach (int generation, SgenWorkerCallback callback);
#else
#define sgen_workers_foreach(...)
#endif
gboolean sgen_workers_is_worker_thread (MonoNativeThreadId id);

#endif
