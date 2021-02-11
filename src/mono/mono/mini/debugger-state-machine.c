/**
 * \file
 * Support for verbose unmanaged crash dumps
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#include <config.h>
#include <glib.h>

#include <mono/utils/json.h>
#include <mono/mini/debugger-state-machine.h>
#include <mono/mini/debugger-state-machine.h>
#include <mono/metadata/object-internals.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/debugger-engine.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-flight-recorder.h>

static const char *
mono_debug_log_thread_state_to_string (MonoDebuggerThreadState state)
{
	switch (state) {
	case MONO_DEBUGGER_SUSPENDED: return "suspended";
	case MONO_DEBUGGER_RESUMED: return "resumed";
	case MONO_DEBUGGER_TERMINATED: return "terminated";
	case MONO_DEBUGGER_STARTED: return "started";
	default:
		g_assert_not_reached ();
	}
}

typedef enum {
	DEBUG_LOG_ILLEGAL = 0x0,
	DEBUG_LOG_STATE_CHANGE = 0x1,
	DEBUG_LOG_BREAKPOINT = 0x2,
	DEBUG_LOG_COMMAND = 0x3,
	DEBUG_LOG_EVENT = 0x4,
	DEBUG_LOG_EXIT = 0x5
} MonoDebugLogKind;

// Number of messages
#define MONO_MAX_DEBUGGER_LOG_LEN 65
// Length of each message
#define MONO_MAX_DEBUGGER_MSG_LEN 200

typedef struct {
	MonoDebugLogKind kind;
	intptr_t tid;
	char message [MONO_MAX_DEBUGGER_MSG_LEN];
} MonoDebugLogItem;

static const char *
mono_debug_log_kind_to_string (MonoDebugLogKind kind)
{
	switch (kind) {
	case DEBUG_LOG_STATE_CHANGE: return "transition";
	case DEBUG_LOG_BREAKPOINT: return "breakpoint";
	case DEBUG_LOG_COMMAND: return "command";
	case DEBUG_LOG_EVENT: return "event";
	case DEBUG_LOG_EXIT: return "exit";
	default:
		g_assert_not_reached ();
	}
}

#define MONO_DEBUGGER_LOG_FREED -1
static MonoFlightRecorder *debugger_log;
static GPtrArray *breakpoint_copy;

void
mono_debugger_log_init (void)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		g_error ("Attempted to initialize debugger log after cleanup");

	debugger_log = mono_flight_recorder_init (MONO_MAX_DEBUGGER_LOG_LEN, sizeof (MonoDebugLogItem));
	breakpoint_copy = g_ptr_array_new ();
}

void
mono_debugger_log_free (void)
{
	MonoFlightRecorder *log = debugger_log;
	debugger_log = (MonoFlightRecorder*)GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED);

	mono_memory_barrier ();
	mono_flight_recorder_free (log);
}

void
mono_debugger_log_command (const char *command_set, const char *command, guint8 *buf, int len)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	// FIXME: print the array in a format that can be decoded / printed?
	char *msg = g_strdup_printf ("Command Logged: %s %s Response: %d", command_set, command, len);
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_COMMAND;
	payload.tid = 0x0;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

void
mono_debugger_log_event (DebuggerTlsData *tls, const char *event, guint8 *buf, int len)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	// FIXME: print the array in a format that can be decoded / printed?
	intptr_t tid = mono_debugger_tls_thread_id (tls);
	char *msg = g_strdup_printf ("Event logged of type %s Response: %d", event, len);
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_EVENT;
	payload.tid = tid;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

void
mono_debugger_log_exit (int exit_code)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	char *msg = g_strdup_printf ("Exited with code %d", exit_code);
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_EXIT;
	payload.tid = 0x0;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

void
mono_debugger_log_add_bp (gpointer bp, MonoMethod *method, long il_offset)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	MonoCoopMutex *debugger_log_mutex = mono_flight_recorder_mutex (debugger_log);
	mono_coop_mutex_lock (debugger_log_mutex);
	g_ptr_array_add (breakpoint_copy, bp);
	mono_coop_mutex_unlock (debugger_log_mutex);

	char *msg = g_strdup_printf ("Add breakpoint %s %lu", method ? mono_method_full_name (method, TRUE) : "No method", il_offset);
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_BREAKPOINT;
	payload.tid = 0x0;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

void
mono_debugger_log_remove_bp (gpointer bp, MonoMethod *method, long il_offset)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	MonoCoopMutex *debugger_log_mutex = mono_flight_recorder_mutex (debugger_log);
	mono_coop_mutex_lock (debugger_log_mutex);
	g_ptr_array_remove (breakpoint_copy, bp);
	mono_coop_mutex_unlock (debugger_log_mutex);

	char *msg = g_strdup_printf ("Remove breakpoint %s %lu", method ? mono_method_full_name (method, TRUE) : "No method", il_offset);
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_BREAKPOINT;
	payload.tid = 0x0;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

void
mono_debugger_log_bp_hit (DebuggerTlsData *tls, MonoMethod *method, long il_offset)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	intptr_t tid = mono_debugger_tls_thread_id (tls);
	char *msg = g_strdup_printf ("Hit breakpoint %s %lu", method ? mono_method_full_name (method, TRUE) : "No method", il_offset);
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_BREAKPOINT;
	payload.tid = tid;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

void
mono_debugger_log_resume (DebuggerTlsData *tls)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	intptr_t tid = mono_debugger_tls_thread_id (tls);
	MonoDebuggerThreadState prev_state = mono_debugger_get_thread_state (tls);
	g_assert (prev_state == MONO_DEBUGGER_SUSPENDED || prev_state == MONO_DEBUGGER_STARTED);
	mono_debugger_set_thread_state (tls, prev_state, MONO_DEBUGGER_RESUMED);

	char *msg = g_strdup_printf ("Resuming 0x%p from state %s", (void*)tid, mono_debug_log_thread_state_to_string (prev_state));
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_STATE_CHANGE;
	payload.tid = tid;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

void
mono_debugger_log_suspend (DebuggerTlsData *tls)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	intptr_t tid = mono_debugger_tls_thread_id (tls);
	MonoDebuggerThreadState prev_state = mono_debugger_get_thread_state (tls);
	g_assert (prev_state == MONO_DEBUGGER_RESUMED || prev_state == MONO_DEBUGGER_STARTED);
	mono_debugger_set_thread_state (tls, prev_state, MONO_DEBUGGER_SUSPENDED);

	char *msg = g_strdup_printf ("Suspending 0x%p from state %s", (void*)tid, mono_debug_log_thread_state_to_string (prev_state));
	MonoDebugLogItem payload;
	payload.kind = DEBUG_LOG_STATE_CHANGE;
	payload.tid = tid;
	g_snprintf ((gchar *) &payload.message, MONO_MAX_DEBUGGER_MSG_LEN, "%s", msg);
	mono_flight_recorder_append (debugger_log, &payload);
}

typedef struct {
	JsonWriter *writer;
	gboolean not_first;
} DebuggerThreadIterState;

// FIXME: log TCP handshake / connection state
static void
dump_thread_state (gpointer key, gpointer value, gpointer user_data)
{
	DebuggerTlsData *debugger_tls = (DebuggerTlsData *) value;
	DebuggerThreadIterState *data = (DebuggerThreadIterState *) user_data;

	if (data->not_first)
		mono_json_writer_printf (data->writer, ",\n");
	else
		data->not_first = TRUE;

	mono_json_writer_indent (data->writer);
	mono_json_writer_object_begin (data->writer);

	mono_json_writer_indent (data->writer);
	mono_json_writer_object_key(data->writer, "thread_id");
	mono_json_writer_printf (data->writer, "\"0x%x\",\n", mono_debugger_tls_thread_id (debugger_tls));

	mono_json_writer_indent (data->writer);
	mono_json_writer_object_key (data->writer, "thread_state");
	const char *state = mono_debug_log_thread_state_to_string (mono_debugger_get_thread_state (debugger_tls));
	mono_json_writer_printf (data->writer, "\"%s\"\n", state);

	mono_json_writer_indent_pop (data->writer);
	mono_json_writer_indent (data->writer);
	mono_json_writer_object_end (data->writer);
}

void
mono_debugger_state (JsonWriter *writer)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return;

	MonoCoopMutex *debugger_log_mutex = mono_flight_recorder_mutex (debugger_log);
	mono_coop_mutex_lock (debugger_log_mutex);
	mono_json_writer_object_begin(writer);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "debugger_state");
	mono_json_writer_object_begin(writer);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "thread_states");
	mono_json_writer_array_begin (writer);
	mono_json_writer_indent_push (writer);

	DebuggerThreadIterState iterState;
	iterState.writer = writer;
	iterState.not_first = FALSE;
	MonoGHashTable *thread_to_tls = mono_debugger_get_thread_states ();
	mono_g_hash_table_foreach (thread_to_tls, dump_thread_state, &iterState);
	mono_json_writer_printf (writer, "\n");

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_array_end (writer);

	mono_json_writer_printf (writer, ",\n");

	// FIXME: Log breakpoint state
	if (breakpoint_copy->len > 0) {
		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "breakpoints");
		mono_json_writer_array_begin (writer);

		for (int i=0; i < breakpoint_copy->len; i++) {
			MonoBreakpoint *bp = (MonoBreakpoint *) g_ptr_array_index (breakpoint_copy, i);

			mono_json_writer_indent (writer);
			mono_json_writer_object_begin(writer);

			mono_json_writer_indent (writer);
			mono_json_writer_object_key(writer, "method");
			mono_json_writer_printf (writer, "\"%s\",\n", bp->method ? mono_method_full_name (bp->method, TRUE) : "No method");

			mono_json_writer_indent (writer);
			mono_json_writer_object_key(writer, "il_offset");
			mono_json_writer_printf (writer, "\"0x%x\",\n", bp->il_offset);

			mono_json_writer_indent_pop (writer);
			mono_json_writer_indent (writer);
			mono_json_writer_object_end (writer);
			mono_json_writer_printf (writer, ",\n");
		}

		mono_json_writer_indent_pop (writer);
		mono_json_writer_indent (writer);
		mono_json_writer_array_end (writer);
		mono_json_writer_printf (writer, ",\n");
	}

	// Log history
	MonoFlightRecorderIter diter;
	mono_flight_recorder_iter_init (debugger_log, &diter);

	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "debugger_history");
	mono_json_writer_array_begin (writer);

	gboolean first = TRUE;
	MonoDebugLogItem item;
	MonoFlightRecorderHeader header;

	while (mono_flight_recorder_iter_next (&diter, &header, (gpointer *) &item)) {
		if (!first)
			mono_json_writer_printf (writer, ",\n");
		else
			first = FALSE;

		mono_json_writer_indent (writer);
		mono_json_writer_object_begin(writer);

		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "kind");
		mono_json_writer_printf (writer, "\"%s\",\n", mono_debug_log_kind_to_string (item.kind));

		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "tid");
		mono_json_writer_printf (writer, "\"0x%x\",\n", item.tid);

		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "message");
		mono_json_writer_printf (writer, "\"%s\",\n", item.message);

		mono_json_writer_indent (writer);
		mono_json_writer_object_key(writer, "counter");
		mono_json_writer_printf (writer, "\"%d\"\n", header.counter);

		mono_json_writer_indent_pop (writer);
		mono_json_writer_indent (writer);
		mono_json_writer_object_end (writer);
	}
	mono_json_writer_printf (writer, "\n");

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_array_end (writer);
	mono_json_writer_printf (writer, ",\n");

	mono_flight_recorder_iter_destroy (&diter);

	// Log client/connection state
	gboolean disconnected = mono_debugger_is_disconnected ();
	mono_json_writer_indent (writer);
	mono_json_writer_object_key(writer, "client_state");
	mono_json_writer_printf (writer, "\"%s\"\n", disconnected ? "disconnected" : "connected");

	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);
	mono_json_writer_printf (writer, "\n");

	mono_json_writer_indent_pop (writer);
	mono_json_writer_indent (writer);
	mono_json_writer_object_end (writer);

	mono_coop_mutex_unlock (debugger_log_mutex);
}

char *
mono_debugger_state_str (void)
{
	if (debugger_log == GINT_TO_POINTER (MONO_DEBUGGER_LOG_FREED))
		return NULL;

	JsonWriter writer;
	mono_json_writer_init (&writer);
	mono_debugger_state (&writer);

	char *result = g_strdup(writer.text->str);
	mono_json_writer_destroy (&writer);

	return result;
}

