/**
 * \file
 * Threading API
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc
 */

#ifndef _MONO_METADATA_THREADS_H_
#define _MONO_METADATA_THREADS_H_

#include <mono/utils/mono-publib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>

MONO_BEGIN_DECLS

/* This callback should return TRUE if the runtime must wait for the thread, FALSE otherwise */
typedef mono_bool (*MonoThreadManageCallback) (MonoThread* thread);

MONO_API void mono_thread_init (MonoThreadStartCB start_cb,
			      MonoThreadAttachCB attach_cb);
MONO_API void mono_thread_cleanup (void);
MONO_API void mono_thread_manage(void);

MONO_API MonoThread *mono_thread_current (void);

MONO_API void        mono_thread_set_main (MonoThread *thread);
MONO_API MonoThread *mono_thread_get_main (void);

MONO_API MONO_RT_EXTERNAL_ONLY void mono_thread_stop (MonoThread *thread);

MONO_API void mono_thread_new_init (intptr_t tid, void* stack_start,
				  void* func);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_thread_create (MonoDomain *domain, void* func, void* arg);

MONO_API MonoThread *mono_thread_attach (MonoDomain *domain);
MONO_API void mono_thread_detach (MonoThread *thread);
MONO_API void mono_thread_exit (void);

MONO_API char   *mono_thread_get_name_utf8 (MonoThread *thread);
MONO_API int32_t mono_thread_get_managed_id (MonoThread *thread);

MONO_API void     mono_thread_set_manage_callback (MonoThread *thread, MonoThreadManageCallback func);

MONO_API void mono_threads_set_default_stacksize (uint32_t stacksize);
MONO_API uint32_t mono_threads_get_default_stacksize (void);

MONO_API void mono_threads_request_thread_dump (void);

MONO_API mono_bool mono_thread_is_foreign (MonoThread *thread);

MONO_API MONO_RT_EXTERNAL_ONLY mono_bool
mono_thread_detach_if_exiting (void);

MONO_END_DECLS

#endif /* _MONO_METADATA_THREADS_H_ */
