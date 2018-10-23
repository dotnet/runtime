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

MONO_BEGIN_DECLS

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
mono_native_state_init (JsonWriter *writer);

char *
mono_native_state_emit (JsonWriter *writer);

char *
mono_native_state_free (JsonWriter *writer, gboolean free_data);

void
mono_native_state_add_thread (JsonWriter *writer, MonoThreadSummary *thread, MonoContext *ctx, gboolean first_thread, gboolean crashing_thread);

void
mono_crash_dump (const char *jsonFile, MonoStackHash *hashes);

MONO_END_DECLS
#endif // DISABLE_CRASH_REPORTING

#endif // MONO_UTILS_NATIVE_STATE
