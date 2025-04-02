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
#include <mono/metadata/profiler-private.h>

struct _MonoProfiler {
	gboolean verbose;
};

static MonoProfiler browser_profiler;
static double desired_sample_interval_ms;
static MonoCallSpec callspec;

#ifdef HOST_BROWSER

typedef struct _ProfilerStackFrame ProfilerStackFrame;

struct _ProfilerStackFrame {
	MonoMethod *method;
	gpointer interp_frame;
	double start;
	bool should_record;
};

enum { MAX_STACK_DEPTH = 600 };
static ProfilerStackFrame profiler_stack_frames[MAX_STACK_DEPTH];
// -1 means empty stack, we keep counting even after MAX_STACK_DEPTH is reached
static int top_stack_frame_index;

static double last_sample_time;
static int prev_skips_per_period;
static int skips_per_period;
static int sample_skip_counter;

double mono_wasm_profiler_now ();
void mono_wasm_profiler_record (MonoMethod *method, double start);

static bool should_record_frame (double now)
{
	if (sample_skip_counter < skips_per_period) {
		return FALSE;
	}

	// timer resolution in non-isolated contexts: 100 microseconds (decimal number)
	double ms_since_last_sample = now - last_sample_time;

	if (desired_sample_interval_ms > 0 && last_sample_time != 0) {
		// recalculate ideal number of skips per period
		double skips_per_ms = ((double)sample_skip_counter) / ms_since_last_sample;
		double newskips_per_period = (skips_per_ms * ((double)desired_sample_interval_ms));
		skips_per_period = ((newskips_per_period + ((double)sample_skip_counter) + ((double)prev_skips_per_period)) / 3);
		prev_skips_per_period = sample_skip_counter;
	} else {
		skips_per_period = 0;
	}
	last_sample_time = now;
	sample_skip_counter = 0;

	return TRUE;
}

static void
method_enter (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	sample_skip_counter++;

	top_stack_frame_index++;
	if (top_stack_frame_index < MAX_STACK_DEPTH) {
		ProfilerStackFrame *newframe = &profiler_stack_frames[top_stack_frame_index];
		double now = newframe->start = mono_wasm_profiler_now ();
		newframe->should_record = should_record_frame (now);
		newframe->method = method;
		newframe->interp_frame = ctx ? ctx->interp_frame : NULL;
	}
}

static void
method_samplepoint (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	// enter/leave are not balanced, perhaps due to different callspecs between AOT and interpreter
	g_assert(top_stack_frame_index >= 0);

	sample_skip_counter++;

	bool is_over = top_stack_frame_index >= MAX_STACK_DEPTH;
	int top_index = is_over ? MAX_STACK_DEPTH - 1 : top_stack_frame_index;
	ProfilerStackFrame *top_frame = &profiler_stack_frames[top_index];

	if (!is_over) {
		g_assertf(top_frame->method == method, "method_exc_leave: %d method mismatch top_frame %s != leave %s\n", top_stack_frame_index, mono_method_get_full_name (top_frame->method), mono_method_get_full_name (method));
		g_assertf(!ctx || !top_frame->interp_frame || top_frame->interp_frame == ctx->interp_frame, "method_exc_leave: %d interp_frame mismatch top_frame %p != leave %p\n", top_stack_frame_index, top_frame->interp_frame, ctx->interp_frame);
	}

	if (!top_frame->should_record)
	{
		top_frame->should_record = should_record_frame (mono_wasm_profiler_now ());
	}
}

static void
method_leave (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	// enter/leave are not balanced, perhaps due to different callspecs between AOT and interpreter
	g_assert(top_stack_frame_index >= 0);
	
	sample_skip_counter++;

	bool is_over = top_stack_frame_index >= MAX_STACK_DEPTH;
	int top_index = is_over ? MAX_STACK_DEPTH - 1 : top_stack_frame_index;
	ProfilerStackFrame *top_frame = &profiler_stack_frames[top_index];
	
	if (!is_over) {
		g_assertf(top_frame->method == method, "method_exc_leave: %d method mismatch top_frame %s != leave %s\n", top_stack_frame_index, mono_method_get_full_name (top_frame->method), mono_method_get_full_name (method));
		g_assertf(!ctx || !top_frame->interp_frame || top_frame->interp_frame == ctx->interp_frame, "method_exc_leave: %d interp_frame mismatch top_frame %p != leave %p\n", top_stack_frame_index, top_frame->interp_frame, ctx->interp_frame);
	}
	
	// pop top frame
	top_stack_frame_index--;

	if (top_frame->should_record || should_record_frame (mono_wasm_profiler_now ()))
	{
		// propagate should_record to parent, if any
		if(top_index > 0)
		{
			profiler_stack_frames[top_index - 1].should_record = TRUE;
		}

		mono_wasm_profiler_record (method, top_frame->start);
	}
}

static void
method_exc_leave (MonoProfiler *prof, MonoMethod *method, MonoObject *exc)
{
	// enter/leave are not balanced, perhaps due to different callspecs between AOT and interpreter
	g_assert(top_stack_frame_index >= 0);
	
	sample_skip_counter++;

	bool is_over = top_stack_frame_index >= MAX_STACK_DEPTH;
	int top_index = is_over ? MAX_STACK_DEPTH - 1 : top_stack_frame_index;
	ProfilerStackFrame *top_frame = &profiler_stack_frames[top_index];
	
	if (top_frame->should_record || should_record_frame (mono_wasm_profiler_now ()))
	{
		// propagate should_record to parent, if any
		if(top_index > 0)
		{
			profiler_stack_frames[top_index - 1].should_record = TRUE;
		}

		mono_wasm_profiler_record (method, top_frame->start);
	}

	// pop top frame
	top_stack_frame_index--;

	is_over = top_stack_frame_index >= MAX_STACK_DEPTH;
	if (!is_over) {
		top_index = is_over ? MAX_STACK_DEPTH - 1 : top_stack_frame_index;
		top_frame = &profiler_stack_frames[top_index];
		g_assertf(top_frame->method == method, "method_exc_leave: %d method mismatch top_frame %s != leave %s\n", top_stack_frame_index, mono_method_get_full_name (top_frame->method), mono_method_get_full_name (method));
	}
}

static void
tail_call (MonoProfiler *prof, MonoMethod *method, MonoMethod *target)
{
	method_leave (prof, method, NULL);
}

#endif /* HOST_BROWSER */

static MonoProfilerCallInstrumentationFlags
method_filter (MonoProfiler *prof, MonoMethod *method)
{
	if (callspec.len > 0 &&
		!mono_callspec_eval (method, &callspec))
		return MONO_PROFILER_CALL_INSTRUMENTATION_NONE;

	return 	MONO_PROFILER_CALL_INSTRUMENTATION_SAMPLEPOINT |
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

static void
parse_arg (const char *arg)
{
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
	else if (match_option (arg, "interval", &val)) {
		char *end;
		desired_sample_interval_ms = strtod (val, &end);
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


/**
 * mono_profiler_init_browser:
 * the entry point
 */
void
mono_profiler_init_browser (const char *desc)
{
	desired_sample_interval_ms = 10;// ms
	memset (&callspec, 0, sizeof (MonoCallSpec));

	// browser:
	if (desc && desc [7] == ':') {
		parse_args (desc + 8);
	}

	MonoProfilerHandle handle = mono_profiler_create (&browser_profiler);

	mono_profiler_set_call_instrumentation_filter_callback (handle, method_filter);

	if (mono_jit_aot_compiling ()) {
		return;
	}

#ifdef HOST_BROWSER
	top_stack_frame_index = -1;
	last_sample_time = 0;
	prev_skips_per_period = 1;
	skips_per_period = 1;
	sample_skip_counter = 1;

	// install this only in production run, not in AOT run
	mono_profiler_set_method_samplepoint_callback (handle, method_samplepoint);
	mono_profiler_set_method_enter_callback (handle, method_enter);
	mono_profiler_set_method_leave_callback (handle, method_leave);
	mono_profiler_set_method_tail_call_callback (handle, tail_call);
	mono_profiler_set_method_exception_leave_callback (handle, method_exc_leave);
#endif /* HOST_BROWSER */
}
