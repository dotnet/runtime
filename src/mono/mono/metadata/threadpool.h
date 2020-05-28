/**
 * \file
 */

#ifndef _MONO_METADATA_THREADPOOL_H_
#define _MONO_METADATA_THREADPOOL_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/exception.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/icalls.h>

void
mono_threadpool_cleanup (void);

MonoAsyncResult *
mono_threadpool_begin_invoke (MonoDomain *domain, MonoObject *target, MonoMethod *method, gpointer *params, MonoError *error);
MonoObject *
mono_threadpool_end_invoke (MonoAsyncResult *ares, MonoArray **out_args, MonoObject **exc, MonoError *error);

gboolean
mono_threadpool_remove_domain_jobs (MonoDomain *domain, int timeout);

void
mono_threadpool_suspend (void);
void
mono_threadpool_resume (void);

/* Internals */

gboolean
mono_threadpool_enqueue_work_item (MonoDomain *domain, MonoObject *work_item, MonoError *error);

#endif // _MONO_METADATA_THREADPOOL_H_
