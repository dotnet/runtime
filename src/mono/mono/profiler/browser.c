/*
 * browser.c: Exporting profiler events to browser dev tools.

 * Copyright 2022 Microsoft Corporation
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <config.h>

#include "browser.h"
#include "helper.h"

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
/*
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-proclib.h>
*/

#ifdef TARGET_WASM


struct _MonoProfiler {
	/* TODO config
	int max_call_depth;
	gboolean disable;
	*/
	int duration;
	gboolean verbose;
};

static MonoProfiler browser_profiler;

static gboolean
match_option (const char *arg, const char *opt_name, const char **rval)
{
	if (rval) {
		const char *end = strchr (arg, '=');

		*rval = NULL;
		if (!end)
			return !strcmp (arg, opt_name);

		if (strncmp (arg, opt_name, strlen (opt_name)) || (end - arg) > GSIZE_TO_SSIZE(strlen (opt_name)) + 1)
			return FALSE;
		*rval = end + 1;
		return TRUE;
	} else {
		//FIXME how should we handle passing a value to an arg that doesn't expect it?
		return !strcmp (arg, opt_name);
	}
}

static void
parse_arg (const char *arg)
{
	const char *val;

	if (match_option (arg, "duration", &val)) {
		char *end;
		browser_profiler.duration = strtoul (val, &end, 10);
	} else if (match_option (arg, "verbose", NULL)) {
		browser_profiler.verbose = TRUE;
	} else {
		mono_profiler_printf_err ("Could not parse argument: %s", arg);
	}
}

static void
parse_args (const char *desc)
{
	const char *p;
	gboolean in_quotes = FALSE;
	char quote_char = '\0';
	char *buffer = g_malloc (strlen (desc) + 1);
	int buffer_pos = 0;

	for (p = desc; *p; p++){
		switch (*p){
		case ',':
			if (!in_quotes) {
				if (buffer_pos != 0){
					buffer [buffer_pos] = 0;
					parse_arg (buffer);
					buffer_pos = 0;
				}
			} else {
				buffer [buffer_pos++] = *p;
			}
			break;

		case '\\':
			if (p [1]) {
				buffer [buffer_pos++] = p[1];
				p++;
			}
			break;
		case '\'':
		case '"':
			if (in_quotes) {
				if (quote_char == *p)
					in_quotes = FALSE;
				else
					buffer [buffer_pos++] = *p;
			} else {
				in_quotes = TRUE;
				quote_char = *p;
			}
			break;
		default:
			buffer [buffer_pos++] = *p;
			break;
		}
	}

	if (buffer_pos != 0) {
		buffer [buffer_pos] = 0;
		parse_arg (buffer);
	}

	g_free (buffer);
}

#ifdef HOST_WASM

static void
runtime_initialized (MonoProfiler *profiler)
{
	mono_profiler_printf ("runtime_initialized");
}

static void
prof_jit_done (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	mono_profiler_printf ("prof_jit_done %s", mono_method_full_name (method, 1));
}

static void
prof_inline_method (MonoProfiler *prof, MonoMethod *method, MonoMethod *inlined_method)
{
	prof_jit_done (prof, inlined_method, NULL);
}

static void
method_enter (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	mono_wasm_profiler_enter ();
	//mono_profiler_printf ("method_enter %s", mono_method_full_name (method, 1));
}

static void
method_leave (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	mono_wasm_profiler_leave (method);
	// mono_profiler_printf ("method_leave %s", mono_method_full_name (method, 1));
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
	mono_profiler_printf_err ("method_filter");

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
	browser_profiler.duration = -1;

	parse_args (desc [strlen ("browser")] == ':' ? desc + strlen ("browser") + 1 : "");

	MonoProfilerHandle handle = mono_profiler_create (&browser_profiler);

	mono_profiler_set_call_instrumentation_filter_callback (handle, method_filter);

	if (mono_jit_aot_compiling ()) {
		return;
	}

#ifdef HOST_WASM
	mono_profiler_set_runtime_initialized_callback (handle, runtime_initialized);
	mono_profiler_set_jit_done_callback (handle, prof_jit_done);
	mono_profiler_set_inline_method_callback (handle, prof_inline_method);
	mono_profiler_set_method_enter_callback (handle, method_enter);
	mono_profiler_set_method_leave_callback (handle, method_leave);
	mono_profiler_set_method_tail_call_callback (handle, tail_call);
	mono_profiler_set_method_exception_leave_callback (handle, method_exc_leave);

	// TODO 
	// MONO_PROFILER_RAISE (assembly_loading, (ass));
	// MONO_PROFILER_RAISE (assembly_loaded, (ass));

	// MONO_PROFILER_RAISE (class_loading, (klass));
	// MONO_PROFILER_RAISE (class_failed, (klass));
	// MONO_PROFILER_RAISE (class_failed, (klass));

	// MONO_PROFILER_RAISE (gc_finalizing, ());
	// MONO_PROFILER_RAISE (gc_finalized, ());

	// MONO_PROFILER_RAISE (gc_event, (MONO_GC_EVENT_END
#endif /* HOST_WASM */
}

#endif /* TARGET_WASM */
