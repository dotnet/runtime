#ifndef _MONO_THREADPOOL_MS_IO_H_
#define _MONO_THREADPOOL_MS_IO_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/socket-io.h>

typedef struct _MonoIOSelectorJob MonoIOSelectorJob;

void
ves_icall_System_IOSelector_Add (gpointer handle, MonoIOSelectorJob *job);

void
ves_icall_System_IOSelector_Remove (gpointer handle);

void
mono_threadpool_ms_io_remove_socket (int fd);
void
mono_threadpool_ms_io_remove_domain_jobs (MonoDomain *domain);
void
mono_threadpool_ms_io_cleanup (void);

#endif /* _MONO_THREADPOOL_MS_IO_H_ */
