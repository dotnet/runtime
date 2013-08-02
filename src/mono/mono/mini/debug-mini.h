#ifndef __DEBUG_MINI_H__
#define __DEBUG_MINI_H__

#include <mono/metadata/class-internals.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"

typedef struct _MonoDebuggerThreadInfo MonoDebuggerThreadInfo;
extern MonoDebuggerThreadInfo *mono_debugger_thread_table;

MONO_API void
mono_debugger_thread_created (gsize tid, MonoThread *thread, MonoJitTlsData *jit_tls, gpointer func);

MONO_API void
mono_debugger_thread_cleanup (MonoJitTlsData *jit_tls);

MONO_API void
mono_debugger_extended_notification (MonoDebuggerEvent event, guint64 data, guint64 arg);

MONO_API void
mono_debugger_trampoline_compiled (const guint8 *trampoline, MonoMethod *method, const guint8 *code);

MONO_API void
mono_debugger_call_exception_handler (gpointer addr, gpointer stack, MonoObject *exc);

MONO_API gboolean
mono_debugger_handle_exception (MonoContext *ctx, MonoObject *obj);

MONO_API MonoObject *
mono_debugger_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc);

MONO_API gboolean
mono_debugger_abort_runtime_invoke (void);

/*
 * Internal exception API.
 */

typedef enum {
	MONO_DEBUGGER_EXCEPTION_ACTION_NONE		= 0,
	MONO_DEBUGGER_EXCEPTION_ACTION_STOP		= 1,
	MONO_DEBUGGER_EXCEPTION_ACTION_STOP_UNHANDLED	= 2
} MonoDebuggerExceptionAction;

MonoDebuggerExceptionAction
_mono_debugger_throw_exception (gpointer addr, gpointer stack, MonoObject *exc);

gboolean
_mono_debugger_unhandled_exception (gpointer addr, gpointer stack, MonoObject *exc);

/*
 * This is the old breakpoint interface.
 * It isn't used by the debugger anymore, but still when using the `--break' command
 * line argument.
 */

int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

#endif
