/**
 * \file
 */

#ifndef _MONO_METADATA_THREADPOOL_IO_H_
#define _MONO_METADATA_THREADPOOL_IO_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object-internals.h>

typedef struct _MonoIOSelectorJob MonoIOSelectorJob;

void
ves_icall_System_IOSelector_Add (gpointer handle, MonoIOSelectorJob *job);

void
ves_icall_System_IOSelector_Remove (gpointer handle);

void
mono_threadpool_io_remove_socket (int fd);
void
mono_threadpool_io_remove_domain_jobs (MonoDomain *domain);
void
mono_threadpool_io_cleanup (void);

#endif /* _MONO_METADATA_THREADPOOL_IO_H_ */
