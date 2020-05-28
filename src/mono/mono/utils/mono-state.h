/**
 * \file
 * Support for cooperative creation of unmanaged state dumps
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#ifndef __MONO_UTILS_NATIVE_STATE__
#define __MONO_UTILS_NATIVE_STATE__

#ifndef DISABLE_CRASH_REPORTING

#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-context.h>
#include <mono/metadata/threads-types.h>
#include <mono/utils/json.h>

#define MONO_NATIVE_STATE_PROTOCOL_VERSION "0.0.6"

typedef enum {
	MonoSummaryNone = 0,
	MonoSummarySetup,
	MonoSummarySuspendHandshake,
	MonoSummaryUnmanagedStacks,
	MonoSummaryManagedStacks,
	MonoSummaryStateWriter,
	MonoSummaryStateWriterDone,
	MonoSummaryMerpWriter,
	MonoSummaryMerpInvoke,
	MonoSummaryCleanup,
	MonoSummaryDone,

	MonoSummaryDoubleFault
} MonoSummaryStage;

typedef struct {
	char *output_str;
	int len;
	int allocated_len;
	int indent;
} MonoStateWriter;

typedef struct {
	gpointer *mem;
	gsize size;

	// File Information
	gint handle;
	gint64 tag;
} MonoStateMem;

// Logging
gboolean
mono_summarize_set_timeline_dir (const char *directory);

void
mono_summarize_timeline_start (const char *dump_reason);

void
mono_summarize_timeline_phase_log (MonoSummaryStage stage);

void
mono_summarize_double_fault_log (void);

MonoSummaryStage
mono_summarize_timeline_read_level (const char *directory, gboolean clear);

// Enable checked-build assertions on summary workflow
// Turns all potential hangs into instant faults
void
mono_summarize_toggle_assertions (gboolean enable);

// Json State Writer

/*
 * These use static memory, can only be called once
 */

void
mono_summarize_native_state_begin (MonoStateWriter *writer, gchar *mem, int size);

char *
mono_summarize_native_state_end (MonoStateWriter *writer);

void
mono_summarize_native_state_add_thread (MonoStateWriter *writer, MonoThreadSummary *thread, MonoContext *ctx, gboolean crashing_thread);

/*
 * These use memory from the caller
 */
void
mono_state_writer_init (MonoStateWriter *writer, gchar *output_str, int len);

void
mono_native_state_init (MonoStateWriter *writer);

char *
mono_native_state_emit (MonoStateWriter *writer);

char *
mono_native_state_free (MonoStateWriter *writer, gboolean free_data);

void
mono_native_state_add_thread (MonoStateWriter *writer, MonoThreadSummary *thread, MonoContext *ctx, gboolean first_thread, gboolean crashing_thread);

void
mono_crash_dump (const char *jsonFile, MonoStackHash *hashes);

// Signal-safe file allocators

gboolean
mono_state_alloc_mem (MonoStateMem *mem, long tag, size_t size);

void
mono_state_free_mem (MonoStateMem *mem);

char*
mono_crash_save_failfast_msg (char *msg);

const char*
mono_crash_get_failfast_msg (void);

void
mono_create_crash_hash_breadcrumb (MonoThreadSummary *thread);

#endif // DISABLE_CRASH_REPORTING

// Dump context functions (enter/leave)

gboolean
mono_dump_start (void);
gboolean
mono_dump_complete (void);

#endif // MONO_UTILS_NATIVE_STATE
