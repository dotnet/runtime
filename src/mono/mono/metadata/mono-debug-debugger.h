#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#include <glib.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>

typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;

typedef enum {
	MONO_DEBUGGER_EVENT_TYPE_ADDED,
	MONO_DEBUGGER_EVENT_METHOD_ADDED,
	MONO_DEBUGGER_EVENT_BREAKPOINT_TRAMPOLINE
} MonoDebuggerEvent;

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	gboolean use_trampoline;
	MonoMethodDesc *desc;
};

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, gpointer data2);

void           mono_debugger_event                  (MonoDebuggerEvent event, gpointer data, gpointer data2);
int            mono_debugger_insert_breakpoint_full (MonoMethodDesc *desc, gboolean use_trampoline);
int            mono_debugger_remove_breakpoint      (int breakpoint_id);
int            mono_debugger_insert_breakpoint      (const gchar *method_name, gboolean include_namespace);
int            mono_debugger_method_has_breakpoint  (MonoMethod* method, gboolean use_trampoline);
void           mono_debugger_trampoline_breakpoint_callback (void);

gpointer       mono_debugger_create_notification_function (gpointer *notification_address);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
