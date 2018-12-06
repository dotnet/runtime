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

#define MONO_NATIVE_STATE_PROTOCOL_VERSION "0.0.2"

typedef enum {
	MonoSummaryNone = 0,
	MonoSummarySetup = 1,
	MonoSummarySuspendHandshake = 2,
	MonoSummaryDumpTraversal = 3,
	MonoSummaryStateWriter = 4,
	MonoSummaryMerpWriter = 5,
	MonoSummaryMerpInvoke = 6,
	MonoSummaryCleanup = 7,
	MonoSummaryDone = 8,

	MonoSummaryDoubleFault = 9
} MonoSummaryStage;

typedef struct {
	char *output_str;
	int len;
	int allocated_len;
	int indent;
} MonoStateWriter;

MONO_BEGIN_DECLS

// Logging
gboolean
mono_summarize_set_timeline_dir (const char *directory);

void
mono_summarize_timeline_start (void);

void
mono_summarize_timeline_phase_log (MonoSummaryStage stage);

void
mono_summarize_double_fault_log (void);

MonoSummaryStage
mono_summarize_timeline_read_level (const char *directory, gboolean clear);

// Json State Writer

/*
 * These use static memory, can only be called once
 */

void
mono_summarize_native_state_begin (char *mem, int size);

char *
mono_summarize_native_state_end (void);

void
mono_summarize_native_state_add_thread (MonoThreadSummary *thread, MonoContext *ctx, gboolean crashing_thread);

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

MONO_END_DECLS
#endif // DISABLE_CRASH_REPORTING

#endif // MONO_UTILS_NATIVE_STATE
