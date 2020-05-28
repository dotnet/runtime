/**
 * \file
 */

#ifndef _MONO_METADATA_THREADPOOL_IO_H_
#define _MONO_METADATA_THREADPOOL_IO_H_

#include <config.h>
#include <glib.h>

#ifndef ENABLE_NETCORE

#include <mono/metadata/object-internals.h>
#include <mono/metadata/icalls.h>

typedef struct _MonoIOSelectorJob MonoIOSelectorJob;

TYPED_HANDLE_DECL (MonoIOSelectorJob);

ICALL_EXPORT
void
ves_icall_System_IOSelector_Remove (gpointer handle);

void
mono_threadpool_io_remove_socket (int fd);
void
mono_threadpool_io_remove_domain_jobs (MonoDomain *domain);
void
mono_threadpool_io_cleanup (void);

#endif /* ENABLE_NETCORE */

#endif /* _MONO_METADATA_THREADPOOL_IO_H_ */
