#ifndef _MONO_THREADPOOL_IO_MS_H_
#define _MONO_THREADPOOL_IO_MS_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/socket-io.h>

typedef struct _MonoSocketRuntimeWorkItem MonoSocketRuntimeWorkItem;

gboolean
mono_threadpool_ms_is_io (MonoObject *target, MonoObject *state);

MonoAsyncResult *
mono_threadpool_ms_io_add (MonoAsyncResult *ares, MonoSocketAsyncResult *sockares);
void
mono_threadpool_ms_io_remove_socket (int fd);
void
mono_threadpool_ms_io_remove_domain_jobs (MonoDomain *domain);
void
mono_threadpool_ms_io_cleanup (void);

void
mono_threadpool_io_enqueue_socket_async_result (MonoDomain *domain, MonoSocketAsyncResult *sockares);

void
ves_icall_System_Net_Sockets_MonoSocketRuntimeWorkItem_ExecuteWorkItem (MonoSocketRuntimeWorkItem *rwi);

#endif /* _MONO_THREADPOOL_IO_MS_H_ */