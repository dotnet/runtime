#ifndef _MONO_THREADPOOL_MICROSOFT_H_
#define _MONO_THREADPOOL_MICROSOFT_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/exception.h>

#define SMALL_STACK (sizeof (gpointer) * 32 * 1024)

enum {
	STATUS_NOT_INITIALIZED,
	STATUS_INITIALIZING,
	STATUS_INITIALIZED,
	STATUS_CLEANING_UP,
	STATUS_CLEANED_UP,
};

typedef struct _MonoNativeOverlapped MonoNativeOverlapped;

static void G_GNUC_UNUSED
mono_threadpool_ms_init (void)
{
	/* Initialization is done lazily */
}

static void G_GNUC_UNUSED
mono_threadpool_ms_init_tls (void)
{
	/* The WSQ is now implemented in managed */
}

void
mono_threadpool_ms_cleanup (void);

MonoAsyncResult *
mono_threadpool_ms_begin_invoke (MonoDomain *domain, MonoObject *target, MonoMethod *method, gpointer *params);
MonoObject *
mono_threadpool_ms_end_invoke (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc);

gboolean
mono_threadpool_ms_remove_domain_jobs (MonoDomain *domain, int timeout);

void
mono_threadpool_ms_suspend (void);
void
mono_threadpool_ms_resume (void);

static gboolean G_GNUC_UNUSED
mono_threadpool_ms_is_queue_array (MonoArray *arr)
{
	/* The queue is in managed code */
	return FALSE;
}

void
ves_icall_System_Threading_Microsoft_ThreadPool_GetAvailableThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads);
void
ves_icall_System_Threading_Microsoft_ThreadPool_GetMinThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads);
void
ves_icall_System_Threading_Microsoft_ThreadPool_GetMaxThreadsNative (gint32 *worker_threads, gint32 *completion_port_threads);
MonoBoolean
ves_icall_System_Threading_Microsoft_ThreadPool_SetMinThreadsNative (gint32 worker_threads, gint32 completion_port_threads);
MonoBoolean
ves_icall_System_Threading_Microsoft_ThreadPool_SetMaxThreadsNative (gint32 worker_threads, gint32 completion_port_threads);
void
ves_icall_System_Threading_Microsoft_ThreadPool_InitializeVMTp (MonoBoolean *enable_worker_tracking);
MonoBoolean
ves_icall_System_Threading_Microsoft_ThreadPool_NotifyWorkItemComplete (void);
void
ves_icall_System_Threading_Microsoft_ThreadPool_NotifyWorkItemProgressNative (void);
void
ves_icall_System_Threading_Microsoft_ThreadPool_ReportThreadStatus (MonoBoolean is_working);
MonoBoolean
ves_icall_System_Threading_Microsoft_ThreadPool_RequestWorkerThread (void);

MonoBoolean
ves_icall_System_Threading_Microsoft_ThreadPool_PostQueuedCompletionStatus (MonoNativeOverlapped *native_overlapped);

MonoBoolean
ves_icall_System_Threading_Microsoft_ThreadPool_BindIOCompletionCallbackNative (gpointer file_handle);

MonoBoolean
ves_icall_System_Threading_Microsoft_ThreadPool_IsThreadPoolHosted (void);

/* Internals */

void
mono_threadpool_ms_enqueue_work_item (MonoDomain *domain, MonoObject *work_item);

#endif // _MONO_THREADPOOL_MICROSOFT_H_
