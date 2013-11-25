#ifndef __DEBUG_MINI_H__
#define __DEBUG_MINI_H__

#include <mono/metadata/class-internals.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"

typedef struct _MonoDebuggerThreadInfo MonoDebuggerThreadInfo;
extern MonoDebuggerThreadInfo *mono_debugger_thread_table;

/*
 * Internal exception API.
 */

typedef enum {
	MONO_DEBUGGER_EXCEPTION_ACTION_NONE		= 0,
	MONO_DEBUGGER_EXCEPTION_ACTION_STOP		= 1,
	MONO_DEBUGGER_EXCEPTION_ACTION_STOP_UNHANDLED	= 2
} MonoDebuggerExceptionAction;

/*
 * This is the old breakpoint interface.
 * It isn't used by the debugger anymore, but still when using the `--break' command
 * line argument.
 */

int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

#endif
