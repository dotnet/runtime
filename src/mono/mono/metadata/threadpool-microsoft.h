#ifndef _MONO_THREADPOOL_MICROSOFT_H_
#define _MONO_THREADPOOL_MICROSOFT_H_

#include <glib.h>

typedef struct _MonoRuntimeWorkItem MonoRuntimeWorkItem;
typedef struct _MonoRegisteredWaitHandle MonoRegisteredWaitHandle;
typedef struct _MonoNativeOverlapped MonoNativeOverlapped;

static void
mono_thread_pool_ms_init (void)
{
	/* Initialization is done lazily */
}

static void
mono_thread_pool_ms_init_tls (void)
{
	/* The WSQ is now implemented in managed */
}

void
mono_thread_pool_ms_cleanup (void);

MonoAsyncResult *
mono_thread_pool_ms_add (MonoObject *target, MonoMethodMessage *msg, MonoDelegate *async_callback, MonoObject *state);
MonoObject *
mono_thread_pool_ms_finish (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc);

gboolean
mono_thread_pool_ms_remove_domain_jobs (MonoDomain *domain, int timeout);
void
mono_thread_pool_ms_remove_socket (int sock);

void
mono_thread_pool_ms_suspend (void);
void
mono_thread_pool_ms_resume (void);

static gboolean
mono_thread_pool_ms_is_queue_array (MonoArray *arr)
{
	/* The queue is in managed code */
	return FALSE;
}

void
ves_icall_System_Threading_MonoRuntimeWorkItem_ExecuteWorkItem (MonoRuntimeWorkItem *rwi);

void
ves_icall_System_Threading_Microsoft_ThreadPool_GetAvailableThreadsNative (gint *worker_threads, gint *completion_port_threads);
void
ves_icall_System_Threading_Microsoft_ThreadPool_GetMinThreadsNative (gint *worker_threads, gint *completion_port_threads);
void
ves_icall_System_Threading_Microsoft_ThreadPool_GetMaxThreadsNative (gint *worker_threads, gint *completion_port_threads);
gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_SetMinThreadsNative (gint worker_threads, gint completion_port_threads);
gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_SetMaxThreadsNative (gint worker_threads, gint completion_port_threads);
void
ves_icall_System_Threading_Microsoft_ThreadPool_InitializeVMTp (gboolean *enable_worker_tracking);
gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_NotifyWorkItemComplete (void);
void
ves_icall_System_Threading_Microsoft_ThreadPool_NotifyWorkItemProgressNative (void);
void
ves_icall_System_Threading_Microsoft_ThreadPool_ReportThreadStatus (gboolean is_working);
gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_RequestWorkerThread (void);
gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_PostQueuedCompletionStatus (MonoNativeOverlapped *native_overlapped);
gpointer
ves_icall_System_Threading_Microsoft_ThreadPool_RegisterWaitForSingleObjectNative (MonoWaitHandle *wait_handle, MonoObject *state, guint timeout_internal, gboolean execute_only_once, MonoRegisteredWaitHandle *registered_wait_handle, gint stack_mark, gboolean compress_stack);
gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_BindIOCompletionCallbackNative (gpointer file_handle);

static gboolean
ves_icall_System_Threading_Microsoft_ThreadPool_IsThreadPoolHosted (void)
{
	return FALSE;
}

#endif // _MONO_THREADPOOL_MICROSOFT_H_