/*
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#include <glib.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/io-layer/io-layer.h>

typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;
typedef struct _MonoDebuggerIOLayer		MonoDebuggerIOLayer;

typedef enum {
	MONO_DEBUGGER_EVENT_BREAKPOINT,
	MONO_DEBUGGER_EVENT_ADD_MODULE,
	MONO_DEBUGGER_EVENT_RELOAD_SYMTABS,
	MONO_DEBUGGER_EVENT_UNHANDLED_EXCEPTION,
	MONO_DEBUGGER_EVENT_EXCEPTION,
	MONO_DEBUGGER_EVENT_THROW_EXCEPTION
} MonoDebuggerEvent;

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	MonoMethodDesc *desc;
};

/*
 * Address of the x86 trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
extern guint8 *mono_generic_trampoline_code;

#ifndef PLATFORM_WIN32

/*
 * Functions we export to the debugger.
 */
struct _MonoDebuggerIOLayer
{
	void (*InitializeCriticalSection) (WapiCriticalSection *section);
	void (*DeleteCriticalSection) (WapiCriticalSection *section);
	gboolean (*TryEnterCriticalSection) (WapiCriticalSection *section);
	void (*EnterCriticalSection) (WapiCriticalSection *section);
	void (*LeaveCriticalSection) (WapiCriticalSection *section);

	guint32 (*WaitForSingleObject) (gpointer handle, guint32 timeout, 
					gboolean alertable);
	guint32 (*SignalObjectAndWait) (gpointer signal_handle, gpointer wait,
					guint32 timeout, gboolean alertable);
	guint32 (*WaitForMultipleObjects) (guint32 numobjects, gpointer *handles,
				      gboolean waitall, guint32 timeout, gboolean alertable);

	gpointer (*CreateSemaphore) (WapiSecurityAttributes *security,
				     gint32 initial, gint32 max,
				     const gunichar2 *name);
	gboolean (*ReleaseSemaphore) (gpointer handle, gint32 count, gint32 *prevcount);

	gpointer (*CreateThread) (WapiSecurityAttributes *security,
				  guint32 stacksize, WapiThreadStart start,
				  gpointer param, guint32 create, guint32 *tid);
	guint32 (*GetCurrentThreadId) (void);
};

extern MonoDebuggerIOLayer mono_debugger_io_layer;

#endif

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_initialize                  (void);
void            mono_debugger_cleanup                     (void);

void            mono_debugger_lock                        (void);
void            mono_debugger_unlock                      (void);
void            mono_debugger_event                       (MonoDebuggerEvent event, guint64 data, guint64 arg);

void            mono_debugger_add_symbol_file             (MonoDebugHandle *handle);
void            mono_debugger_start_add_type              (MonoDebugHandle *symfile, MonoClass *klass);
void            mono_debugger_add_type                    (MonoDebugHandle *symfile, MonoClass *klass);
void            mono_debugger_add_builtin_types           (MonoDebugHandle *symfile);

void            mono_debugger_add_method                  (MonoDebugMethodJitInfo *jit);

void            mono_debugger_add_wrapper                 (MonoMethod *wrapper,
							   MonoDebugMethodJitInfo *jit,
							   gpointer addr);


int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
int             mono_debugger_insert_breakpoint           (const gchar *method_name, gboolean include_namespace);
int             mono_debugger_method_has_breakpoint       (MonoMethod *method);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

gpointer        mono_debugger_create_notification_function (gpointer *notification_address);

MonoObject     *mono_debugger_runtime_invoke              (MonoMethod *method, void *obj,
							   void **params, MonoObject **exc);

guint32         mono_debugger_lookup_type                 (const gchar *type_name);
gint32          mono_debugger_lookup_assembly             (const gchar *name);
gboolean        mono_debugger_unhandled_exception         (gpointer addr, gpointer stack, MonoObject *exc);
void            mono_debugger_handle_exception            (gpointer addr, gpointer stack, MonoObject *exc);
gboolean        mono_debugger_throw_exception             (gpointer addr, gpointer stack, MonoObject *exc);



void *
mono_vtable_get_static_field_data (MonoVTable *vt);


MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod (MonoReflectionAssembly *assembly, guint32 token);

int
ves_icall_MonoDebugger_GetMethodToken (MonoReflectionAssembly *assembly, MonoReflectionMethod *method);

MonoReflectionType *
ves_icall_MonoDebugger_GetLocalTypeFromSignature (MonoReflectionAssembly *assembly, MonoArray *signature);

MonoReflectionType *
ves_icall_MonoDebugger_GetType (MonoReflectionAssembly *assembly, guint32 token);

gchar *
mono_debugger_check_runtime_version (const char *filename);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
