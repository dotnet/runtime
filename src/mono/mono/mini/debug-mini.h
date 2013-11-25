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

#endif
