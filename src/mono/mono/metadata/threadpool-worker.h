/**
 * \file
 */

#ifndef _MONO_METADATA_THREADPOOL_WORKER_H
#define _MONO_METADATA_THREADPOOL_WORKER_H

#include <glib.h>

typedef void (*MonoThreadPoolWorkerCallback)(void);

void
mono_threadpool_worker_init (MonoThreadPoolWorkerCallback callback);

void
mono_threadpool_worker_cleanup (void);

void
mono_threadpool_worker_request (void);

gboolean
mono_threadpool_worker_notify_completed (void);

gint32
mono_threadpool_worker_get_min (void);
gboolean
mono_threadpool_worker_set_min (gint32 value);

gint32
mono_threadpool_worker_get_max (void);
gboolean
mono_threadpool_worker_set_max (gint32 value);

void
mono_threadpool_worker_set_suspended (gboolean suspended);

#endif /* _MONO_METADATA_THREADPOOL_WORKER_H */
