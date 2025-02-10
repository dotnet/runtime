/*
 * browser.c: Exporting profiler events to browser dev tools.

 * Copyright 2022 Microsoft Corporation
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <config.h>

#include <mono/metadata/profiler.h>
#include <mono/metadata/callspec.h>
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
static MonoCallSpec callspec;


#ifdef HOST_WASM

void
mono_wasm_profiler_enter ();

void
mono_wasm_profiler_leave (MonoMethod *method);

void
mono_wasm_profiler_samplepoint ();

static void
method_enter (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	mono_wasm_profiler_enter ();
}

static void
method_samplepoint (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	mono_wasm_profiler_samplepoint ();
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
	if (callspec.len > 0 &&
		!mono_callspec_eval (method, &callspec))
		return MONO_PROFILER_CALL_INSTRUMENTATION_NONE;

	return 	MONO_PROFILER_CALL_INSTRUMENTATION_SAMPLEPOINT_CONTEXT |
			MONO_PROFILER_CALL_INSTRUMENTATION_ENTER |
			MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE |
			MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL |
			MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE;
}


MONO_API void
mono_profiler_init_browser (const char *desc);

static gboolean
match_option (const char *arg, const char *opt_name, const char **rval)
{
	if (rval) {
		const char *end = strchr (arg, '=');

		*rval = NULL;
		if (!end)
			return !strcmp (arg, opt_name);

		if (strncmp (arg, opt_name, strlen (opt_name)) || (end - arg) > (ptrdiff_t)strlen (opt_name) + 1)
			return FALSE;
		*rval = end + 1;
		return TRUE;
	} else {
		//FIXME how should we handle passing a value to an arg that doesn't expect it?
		return !strcmp (arg, opt_name);
	}
}

/**
 * mono_profiler_init_browser:
 * the entry point
 */
void
mono_profiler_init_browser (const char *desc)
{
	// browser:
	if (!desc && desc [7] == ':') {
		const char *arg = desc + 8;
		const char *val;

		if (match_option (arg, "callspec", &val)) {
			if (!val)
				val = "";
			if (val[0] == '\"')
				++val;
			char *spec = g_strdup (val);
			size_t speclen = strlen (val);
			if (speclen > 0 && spec[speclen - 1] == '\"')
				spec[speclen - 1] = '\0';
			char *errstr;
			if (!mono_callspec_parse (spec, &callspec, &errstr)) {
				mono_profiler_printf_err ("Could not parse callspec '%s': %s", spec, errstr);
				g_free (errstr);
				mono_callspec_cleanup (&callspec);
			}
			g_free (spec);
		}
	}

	MonoProfilerHandle handle = mono_profiler_create (&browser_profiler);

	mono_profiler_set_call_instrumentation_filter_callback (handle, method_filter);

	if (mono_jit_aot_compiling ()) {
		return;
	}

#ifdef HOST_WASM
	// install this only in production run, not in AOT run
	mono_profiler_set_method_samplepoint_callback (handle, method_samplepoint);
	mono_profiler_set_method_enter_callback (handle, method_enter);
	mono_profiler_set_method_leave_callback (handle, method_leave);
	mono_profiler_set_method_tail_call_callback (handle, tail_call);
	mono_profiler_set_method_exception_leave_callback (handle, method_exc_leave);
#endif /* HOST_WASM */
}
