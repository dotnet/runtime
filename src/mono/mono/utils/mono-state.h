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

#ifdef TARGET_OSX

#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-context.h>
#include <mono/metadata/threads-types.h>

#define MONO_NATIVE_STATE_PROTOCOL_VERSION "0.0.1"

MONO_BEGIN_DECLS

void
mono_summarize_native_state_begin (void);

char *
mono_summarize_native_state_end (void);

void
mono_summarize_native_state_add_thread (MonoThreadSummary *thread, MonoContext *ctx);

MONO_END_DECLS
#endif // TARGET_OSX

#endif // MONO_UTILS_NATIVE_STATE
