/*
 * This is a private header file.
 * The API in here is undocumented and may only be used by the JIT to
 * communicate with the debugger.
 */

#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#include <glib.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/utils/mono-codeman.h>
#include <mono/io-layer/io-layer.h>

typedef enum {
	MONO_DEBUGGER_EVENT_INITIALIZE_MANAGED_CODE	= 1,
	MONO_DEBUGGER_EVENT_INITIALIZE_CORLIB,
	MONO_DEBUGGER_EVENT_JIT_BREAKPOINT,
	MONO_DEBUGGER_EVENT_INITIALIZE_THREAD_MANAGER,
	MONO_DEBUGGER_EVENT_ACQUIRE_GLOBAL_THREAD_LOCK,
	MONO_DEBUGGER_EVENT_RELEASE_GLOBAL_THREAD_LOCK,
	MONO_DEBUGGER_EVENT_WRAPPER_MAIN,
	MONO_DEBUGGER_EVENT_MAIN_EXITED,
	MONO_DEBUGGER_EVENT_UNHANDLED_EXCEPTION,
	MONO_DEBUGGER_EVENT_THROW_EXCEPTION,
	MONO_DEBUGGER_EVENT_HANDLE_EXCEPTION,
	MONO_DEBUGGER_EVENT_THREAD_CREATED,
	MONO_DEBUGGER_EVENT_THREAD_CLEANUP,
	MONO_DEBUGGER_EVENT_GC_THREAD_CREATED,
	MONO_DEBUGGER_EVENT_GC_THREAD_EXITED,
	MONO_DEBUGGER_EVENT_REACHED_MAIN,
	MONO_DEBUGGER_EVENT_FINALIZE_MANAGED_CODE,
	MONO_DEBUGGER_EVENT_LOAD_MODULE,
	MONO_DEBUGGER_EVENT_UNLOAD_MODULE,
	MONO_DEBUGGER_EVENT_DOMAIN_CREATE,
	MONO_DEBUGGER_EVENT_DOMAIN_UNLOAD
} MonoDebuggerEvent;

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_initialize                    (gboolean use_debugger);
void            mono_debugger_cleanup                       (void);

void            mono_debugger_lock                          (void);
void            mono_debugger_unlock                        (void);
void            mono_debugger_event                         (MonoDebuggerEvent event, guint64 data, guint64 arg);

MonoObject     *mono_debugger_runtime_invoke                (MonoMethod *method, void *obj,
							     void **params, MonoObject **exc);

void *
mono_vtable_get_static_field_data (MonoVTable *vt);

gchar *
mono_debugger_check_runtime_version (const char *filename);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
