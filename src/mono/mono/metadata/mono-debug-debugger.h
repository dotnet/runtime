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
	MONO_DEBUGGER_EVENT_DOMAIN_UNLOAD,
	MONO_DEBUGGER_EVENT_CLASS_INITIALIZED,
	MONO_DEBUGGER_EVENT_INTERRUPTION_REQUEST,
	MONO_DEBUGGER_EVENT_CREATE_APPDOMAIN,
	MONO_DEBUGGER_EVENT_UNLOAD_APPDOMAIN,

	/* Obsolete, only for backwards compatibility with older debugger versions */
	MONO_DEBUGGER_EVENT_OLD_TRAMPOLINE		= 256,

	MONO_DEBUGGER_EVENT_TRAMPOLINE			= 512
} MonoDebuggerEvent;

extern volatile gint32 _mono_debugger_interruption_request;

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_initialize                    (gboolean use_debugger);
void            mono_debugger_cleanup                       (void);

void            mono_debugger_lock                          (void);
void            mono_debugger_unlock                        (void);
void            mono_debugger_event                         (MonoDebuggerEvent event, guint64 data, guint64 arg);

void *
mono_vtable_get_static_field_data (MonoVTable *vt);

gchar *
mono_debugger_check_runtime_version (const char *filename);

void
mono_debugger_class_initialized (MonoClass *klass);

void
mono_debugger_check_interruption (void);

void
mono_debugger_event_create_appdomain (MonoDomain *domain, gchar *shadow_path);

void
mono_debugger_event_unload_appdomain (MonoDomain *domain);

MonoDebugMethodAddressList *
mono_debugger_insert_method_breakpoint (MonoMethod *method, guint64 idx);

int
mono_debugger_remove_method_breakpoint (guint64 index);

void
mono_debugger_check_breakpoints (MonoMethod *method, MonoDebugMethodAddress *debug_info);

MonoClass *
mono_debugger_register_class_init_callback (MonoImage *image, const gchar *full_name,
					    guint32 token, guint32 index);

void
mono_debugger_remove_class_init_callback (int index);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
