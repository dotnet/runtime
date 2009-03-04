#ifndef __DEBUG_MINI_H__
#define __DEBUG_MINI_H__

#include <mono/metadata/class-internals.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"

typedef struct _MonoDebuggerThreadInfo MonoDebuggerThreadInfo;
extern MonoDebuggerThreadInfo *mono_debugger_thread_table;

typedef enum {
	MONO_DEBUGGER_EXCEPTION_ACTION_NONE		= 0,
	MONO_DEBUGGER_EXCEPTION_ACTION_STOP		= 1,
	MONO_DEBUGGER_EXCEPTION_ACTION_STOP_UNHANDLED	= 2
} MonoDebuggerExceptionAction;

void
mono_debugger_thread_created (gsize tid, MonoThread *thread, MonoJitTlsData *jit_tls);

void
mono_debugger_thread_cleanup (MonoJitTlsData *jit_tls);

void
mono_debugger_extended_notification (MonoDebuggerEvent event, guint64 data, guint64 arg);

void
mono_debugger_trampoline_compiled (MonoMethod *method, const guint8 *code);

gboolean
mono_debugger_unhandled_exception (gpointer addr, gpointer stack, MonoObject *exc);

void
mono_debugger_handle_exception (gpointer addr, gpointer stack, MonoObject *exc);

MonoDebuggerExceptionAction
mono_debugger_throw_exception (gpointer addr, gpointer stack, MonoObject *exc);

MonoObject *
mono_debugger_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc);

/*
 * This is the old breakpoint interface.
 * It isn't used by the debugger anymore, but still when using the `--break' command
 * line argument.
 */

int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

#endif
