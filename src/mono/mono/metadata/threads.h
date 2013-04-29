/*
 * threads.h: Threading API
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

extern void mono_thread_init (MonoThreadStartCB start_cb,
			      MonoThreadAttachCB attach_cb);
extern void mono_thread_cleanup (void);
extern void mono_thread_manage(void);

extern MonoThread *mono_thread_current (void);

extern void        mono_thread_set_main (MonoThread *thread);
extern MonoThread *mono_thread_get_main (void);

extern void mono_thread_stop (MonoThread *thread);

extern void mono_thread_new_init (intptr_t tid, void* stack_start,
				  void* func);
extern void mono_thread_create (MonoDomain *domain, void* func, void* arg);
extern MonoThread *mono_thread_attach (MonoDomain *domain);
extern void mono_thread_detach (MonoThread *thread);
extern void mono_thread_exit (void);

void     mono_thread_set_manage_callback (MonoThread *thread, MonoThreadManageCallback func);

extern void mono_threads_set_default_stacksize (uint32_t stacksize);
extern uint32_t mono_threads_get_default_stacksize (void);

void mono_threads_request_thread_dump (void);

mono_bool mono_thread_is_foreign (MonoThread *thread);

MONO_END_DECLS

#endif /* _MONO_METADATA_THREADS_H_ */
