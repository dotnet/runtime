#ifndef _MONO_THREADPOOL_H_
#define _MONO_THREADPOOL_H_

#include <mono/metadata/object-internals.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/socket-io.h>

/* No managed code here */
void mono_thread_pool_init (void) MONO_INTERNAL;

void icall_append_job (MonoObject *ar) MONO_INTERNAL;
void icall_append_io_job (MonoObject *target, MonoSocketAsyncResult *state) MONO_INTERNAL;
MonoAsyncResult *
mono_thread_pool_add     (MonoObject *target, MonoMethodMessage *msg, 
			  MonoDelegate *async_callback, MonoObject *state) MONO_INTERNAL;

MonoObject *
mono_thread_pool_finish (MonoAsyncResult *ares, MonoArray **out_args, 
			 MonoObject **exc) MONO_INTERNAL;

void mono_thread_pool_cleanup (void) MONO_INTERNAL;

gboolean mono_thread_pool_remove_domain_jobs (MonoDomain *domain, int timeout) MONO_INTERNAL;

void
ves_icall_System_Threading_ThreadPool_GetAvailableThreads (int *workerThreads,
							   int *completionPortThreads) MONO_INTERNAL;

void
ves_icall_System_Threading_ThreadPool_GetMaxThreads (int *workerThreads,
						     int *completionPortThreads) MONO_INTERNAL;

void
ves_icall_System_Threading_ThreadPool_GetMinThreads (gint *workerThreads, 
								gint *completionPortThreads) MONO_INTERNAL;

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMinThreads (gint workerThreads, 
								gint completionPortThreads) MONO_INTERNAL;

MonoBoolean
ves_icall_System_Threading_ThreadPool_SetMaxThreads (gint workerThreads, 
								gint completionPortThreads) MONO_INTERNAL;

typedef void  (*MonoThreadPoolFunc) (gpointer user_data);
void mono_install_threadpool_thread_hooks (MonoThreadPoolFunc start_func, MonoThreadPoolFunc finish_func, gpointer user_data);

typedef void  (*MonoThreadPoolItemFunc) (gpointer user_data);
void mono_install_threadpool_item_hooks (MonoThreadPoolItemFunc begin_func, MonoThreadPoolItemFunc end_func, gpointer user_data);

#endif

