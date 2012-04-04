/*
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
void sgen_workers_init_distribute_gray_queue (void) MONO_INTERNAL;
void sgen_workers_enqueue_job (JobFunc func, void *data) MONO_INTERNAL;
void sgen_workers_start_marking (void) MONO_INTERNAL;
void sgen_workers_distribute_gray_queue_sections (void) MONO_INTERNAL;
void sgen_workers_reset_data (void) MONO_INTERNAL;
void sgen_workers_join (void) MONO_INTERNAL;
gboolean sgen_workers_is_distributed_queue (SgenGrayQueue *queue) MONO_INTERNAL;
SgenGrayQueue* sgen_workers_get_distribute_gray_queue (void) MONO_INTERNAL;

#endif
