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

typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;

typedef enum {
	MONO_DEBUGGER_EVENT_INITIALIZE_MANAGED_CODE	= 1,
	MONO_DEBUGGER_EVENT_ADD_MODULE,
	MONO_DEBUGGER_EVENT_RELOAD_SYMTABS,
	MONO_DEBUGGER_EVENT_METHOD_COMPILED,
	MONO_DEBUGGER_EVENT_JIT_BREAKPOINT,
	MONO_DEBUGGER_EVENT_INITIALIZE_THREAD_MANAGER,
	MONO_DEBUGGER_EVENT_ACQUIRE_GLOBAL_THREAD_LOCK,
	MONO_DEBUGGER_EVENT_RELEASE_GLOBAL_THREAD_LOCK,
	MONO_DEBUGGER_EVENT_WRAPPER_MAIN,
	MONO_DEBUGGER_EVENT_MAIN_EXITED,
	MONO_DEBUGGER_EVENT_UNHANDLED_EXCEPTION,
	MONO_DEBUGGER_EVENT_THREAD_CREATED,
	MONO_DEBUGGER_EVENT_THREAD_ABORT,
	MONO_DEBUGGER_EVENT_THREAD_EXITED,
	MONO_DEBUGGER_EVENT_THROW_EXCEPTION,
	MONO_DEBUGGER_EVENT_HANDLE_EXCEPTION,
	MONO_DEBUGGER_EVENT_REACHED_MAIN
} MonoDebuggerEvent;

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	MonoMethodDesc *desc;
};

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_initialize                  (gboolean use_debugger);
void            mono_debugger_cleanup                     (void);

void            mono_debugger_lock                        (void);
void            mono_debugger_unlock                      (void);
void            mono_debugger_event                       (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_add_symbol_file             (MonoDebugHandle *handle);
void            mono_debugger_start_add_type              (MonoDebugHandle *symfile, MonoClass *klass);

int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

guint8         *mono_debugger_create_notification_function(MonoCodeManager *codeman);

MonoObject     *mono_debugger_runtime_invoke              (MonoMethod *method, void *obj,
							   void **params, MonoObject **exc);

gboolean        mono_debugger_lookup_type                 (const gchar *type_name);
gint32          mono_debugger_lookup_assembly             (const gchar *name);

void *
mono_vtable_get_static_field_data (MonoVTable *vt);

gchar *
mono_debugger_check_runtime_version (const char *filename);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
