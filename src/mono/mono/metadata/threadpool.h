#ifndef _MONO_THREADPOOL_H_
#define _MONO_THREADPOOL_H_

#include <mono/metadata/object-internals.h>
#include <mono/metadata/reflection.h>

extern int mono_max_worker_threads;

/* No managed code here */
void mono_thread_pool_init (void);

MonoAsyncResult *
mono_thread_pool_add     (MonoObject *target, MonoMethodMessage *msg, 
			  MonoDelegate *async_callback, MonoObject *state);

MonoObject *
mono_thread_pool_finish (MonoAsyncResult *ares, MonoArray **out_args, 
			 MonoObject **exc);

void mono_thread_pool_cleanup (void);

void
ves_icall_System_Threading_ThreadPool_GetAvailableThreads (int *workerThreads,
							   int *completionPortThreads);

void
ves_icall_System_Threading_ThreadPool_GetMaxThreads (int *workerThreads,
						     int *completionPortThreads);

void
ves_icall_System_Threading_ThreadPool_GetMinThreads (gint *workerThreads, 
								gint *completionPortThreads);

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMinThreads (gint workerThreads, 
								gint completionPortThreads);

MonoBoolean
ves_icall_System_Threading_ThreadPool_BindHandle (gpointer handle);

#endif
