/*
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#if !defined _IN_THE_MONO_DEBUGGER
#error "<mono/metadata/mono-debug-debugger.h> is a private header file only intended to be used by the debugger."
#endif

#include <glib.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/io-layer/io-layer.h>

typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;
typedef struct _MonoDebuggerIOLayer		MonoDebuggerIOLayer;
typedef struct _MonoDebuggerInfo		MonoDebuggerInfo;

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
	MONO_DEBUGGER_EVENT_THROW_EXCEPTION,
	MONO_DEBUGGER_EVENT_HANDLE_EXCEPTION
} MonoDebuggerEvent;

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	MonoMethodDesc *desc;
};

/*
 * Address of the x86 trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
extern guint8 *mono_trampoline_code [];

/*
 * There's a global data symbol called `MONO_DEBUGGER__debugger_info' which
 * contains pointers to global variables and functions which must be accessed
 * by the debugger.
 */
struct _MonoDebuggerInfo {
	guint64 magic;
	guint32 version;
	guint32 total_size;
	guint32 symbol_table_size;
	guint32 dummy;
	guint8 ***mono_trampoline_code;
	MonoSymbolTable **symbol_table;
	guint64 (*compile_method) (guint64 method_argument);
	guint64 (*get_virtual_method) (guint64 object_argument, guint64 method_argument);
	guint64 (*get_boxed_object_method) (guint64 klass_argument, guint64 val_argument);
	guint64 (*insert_breakpoint) (guint64 method_argument, const gchar *string_argument);
	guint64 (*remove_breakpoint) (guint64 breakpoint);
	MonoInvokeFunc runtime_invoke;
	guint64 (*create_string) (guint64 dummy_argument, const gchar *string_argument);
	guint64 (*class_get_static_field_data) (guint64 klass);
	guint64 (*lookup_class) (guint64 image_argument, guint64 token_arg);
	guint64 (*lookup_type) (guint64 dummy_argument, const gchar *string_argument);
	guint64 (*lookup_assembly) (guint64 dummy_argument, const gchar *string_argument);
	guint64 (*run_finally) (guint64 argument1, guint64 argument2);
};

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_initialize                  (gboolean use_debugger);
void            mono_debugger_cleanup                     (void);

void            mono_debugger_lock                        (void);
void            mono_debugger_unlock                      (void);
void            mono_debugger_event                       (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_add_symbol_file             (MonoDebugHandle *handle);
void            mono_debugger_start_add_type              (MonoDebugHandle *symfile, MonoClass *klass);
void            mono_debugger_add_builtin_types           (MonoDebugHandle *symfile);

int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

MonoObject     *mono_debugger_runtime_invoke              (MonoMethod *method, void *obj,
							   void **params, MonoObject **exc);

gboolean        mono_debugger_lookup_type                 (const gchar *type_name);
gint32          mono_debugger_lookup_assembly             (const gchar *name);

void *
mono_vtable_get_static_field_data (MonoVTable *vt);

gchar *
mono_debugger_check_runtime_version (const char *filename);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
