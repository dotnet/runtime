/**
 * \file
 * Types for the debugger state machine and wire protocol
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#ifndef __MONO_DEBUGGER_STATE_MACHINE__
#define __MONO_DEBUGGER_STATE_MACHINE__


#include <glib.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/json.h>
#include "debugger-agent.h"

typedef enum {
	MONO_DEBUGGER_STARTED = 0,
	MONO_DEBUGGER_RESUMED = 1,
	MONO_DEBUGGER_SUSPENDED = 2,
	MONO_DEBUGGER_TERMINATED = 3,
} MonoDebuggerThreadState;

void
mono_debugger_log_init (void);

void
mono_debugger_log_free (void);

void
mono_debugger_log_exit (int exit_code);

void
mono_debugger_log_add_bp (gpointer key, MonoMethod *method, long il_offset);

void
mono_debugger_log_remove_bp (gpointer key, MonoMethod *method, long il_offset);

void
mono_debugger_log_command (const char *command_set, const char *command, guint8 *buf, int len);

void
mono_debugger_log_event (DebuggerTlsData *tls, const char *event, guint8 *buf, int len);

void
mono_debugger_log_bp_hit (DebuggerTlsData *tls, MonoMethod *method, long il_offset);

void
mono_debugger_log_resume (DebuggerTlsData *tls);

void
mono_debugger_log_suspend (DebuggerTlsData *tls);

#if 0
#define DEBUGGER_STATE_MACHINE_DEBUG(level, ...)
#else
#define DEBUGGER_STATE_MACHINE_DEBUG(level, ...) g_async_safe_printf(__VA_ARGS__)
#endif

void
mono_debugger_state (JsonWriter *writer);

char *
mono_debugger_state_str (void);

#endif // __MONO_DEBUGGER_STATE_MACHINE__ 
