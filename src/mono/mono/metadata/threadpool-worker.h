
#ifndef _MONO_METADATA_THREADPOOL_WORKER_H
#define _MONO_METADATA_THREADPOOL_WORKER_H

typedef struct MonoThreadPoolWorker MonoThreadPoolWorker;

typedef void (*MonoThreadPoolWorkerCallback)(gpointer);

void
mono_threadpool_worker_init (MonoThreadPoolWorker **worker);

void
mono_threadpool_worker_cleanup (MonoThreadPoolWorker *worker);

void
mono_threadpool_worker_enqueue (MonoThreadPoolWorker *worker, MonoThreadPoolWorkerCallback callback, gpointer data);

gboolean
mono_threadpool_worker_notify_completed (MonoThreadPoolWorker *worker);

gint32
mono_threadpool_worker_get_min (MonoThreadPoolWorker *worker);
gboolean
mono_threadpool_worker_set_min (MonoThreadPoolWorker *worker, gint32 value);

gint32
mono_threadpool_worker_get_max (MonoThreadPoolWorker *worker);
gboolean
mono_threadpool_worker_set_max (MonoThreadPoolWorker *worker, gint32 value);

void
mono_threadpool_worker_set_suspended (MonoThreadPoolWorker *worker, gboolean suspended);

#endif /* _MONO_METADATA_THREADPOOL_WORKER_H */
