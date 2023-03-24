/*
 * browser.c: Exporting profiler events to browser dev tools.

 * Copyright 2022 Microsoft Corporation
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <config.h>

#include <mono/metadata/profiler.h>
#include <mono/utils/mono-logger-internals.h>
#include <string.h>
#include <errno.h>
#include <stdlib.h>
#include <glib.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-publib.h>
#include <mono/jit/jit.h>

struct _MonoProfiler {
	gboolean verbose;
};

static MonoProfiler browser_profiler;

#ifdef HOST_WASM

void
mono_wasm_profiler_enter ();

void
mono_wasm_profiler_leave (MonoMethod *method);

static void
method_enter (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	mono_wasm_profiler_enter ();
}

static void
method_leave (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	mono_wasm_profiler_leave (method);
}

static void
tail_call (MonoProfiler *prof, MonoMethod *method, MonoMethod *target)
{
	method_leave (prof, method, NULL);
}

static void
method_exc_leave (MonoProfiler *prof, MonoMethod *method, MonoObject *exc)
{
	method_leave (prof, method, NULL);
}

#endif /* HOST_WASM */

static MonoProfilerCallInstrumentationFlags
method_filter (MonoProfiler *prof, MonoMethod *method)
{
	// TODO filter by namespace ?
	return MONO_PROFILER_CALL_INSTRUMENTATION_ENTER |
	       MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE |
	       MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL |
	       MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE;
}


MONO_API void
mono_profiler_init_browser (const char *desc);

/**
 * mono_profiler_init_browser:
 * the entry point
 */
void
mono_profiler_init_browser (const char *desc)
{
	MonoProfilerHandle handle = mono_profiler_create (&browser_profiler);

	mono_profiler_set_call_instrumentation_filter_callback (handle, method_filter);

	if (mono_jit_aot_compiling ()) {
		return;
	}

#ifdef HOST_WASM
	// install this only in production run, not in AOT run
	mono_profiler_set_method_enter_callback (handle, method_enter);
	mono_profiler_set_method_leave_callback (handle, method_leave);
	mono_profiler_set_method_tail_call_callback (handle, tail_call);
	mono_profiler_set_method_exception_leave_callback (handle, method_exc_leave);
#endif /* HOST_WASM */
}
