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
#include <mono/mini/mini-runtime.h>

struct _MonoProfiler {
	gboolean verbose;
};

static MonoProfiler browser_profiler;
MonoProfilerHandle profiler_handle;
static double desired_sample_interval_ms;
static MonoCallSpec callspec;
static bool needs_balanced_events = true;

void mono_profiler_init_browser_aot (const char *desc);
void mono_profiler_init_browser_devtools (const char *desc);
MonoProfilerHandle mono_profiler_init_browser_eventpipe (void);
void mono_profiler_fini_browser_eventpipe (void);

#ifdef HOST_BROWSER

typedef struct _ProfilerStackFrame ProfilerStackFrame;

struct _ProfilerStackFrame {
	MonoMethod *method;
	gpointer sp;
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

static gpointer get_last_sp ()
{
	// TODO add support for AOT. We need some monotonic stack pointer from WASM C/AOT.
	// maybe MonoProfilerCallContext* could be used for that? 

	MonoLMF *lmf = mono_get_lmf ();

	if(((gsize)lmf->previous_lmf) & 2) {
		MonoLMFExt *ext = (MonoLMFExt*)lmf;
		return ext->interp_exit_data;
	}

	return NULL;
}

static bool should_record_frame (double now)
{
	if (sample_skip_counter < skips_per_period) {
		return FALSE;
	}

	if (now == 0.0) {
		now = mono_wasm_profiler_now ();
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
	g_assertf(top_stack_frame_index < MAX_STACK_DEPTH, "Stack too deep for dev tools profiler \n");
	ProfilerStackFrame *newframe = &profiler_stack_frames[top_stack_frame_index];
	double now = newframe->start = mono_wasm_profiler_now ();
	newframe->should_record = should_record_frame (now);
	newframe->method = method;
	newframe->sp = get_last_sp ();
	g_assertf(newframe->sp != NULL, "method_enter: stack frame is NULL for method %s\n", mono_method_get_full_name (method));
}

static void
record_and_mark_parent (ProfilerStackFrame *top_frame, int top_index)
{
	if (top_frame->should_record) {
		if (top_index > 0) {
			profiler_stack_frames[top_index - 1].should_record = TRUE;
		}
		mono_wasm_profiler_record (top_frame->method, top_frame->start);
	}
}

static void
pop_and_record_frames (bool leaving, MonoMethod *method)
{
	// enter/leave are not balanced, perhaps due to different callspecs between AOT and interpreter
	g_assert(top_stack_frame_index >= 0);

	sample_skip_counter++;

	// stack pointers grow down
	gpointer sp = get_last_sp ();
	if (sp == NULL) {
		g_warning ("pop_and_record_frames: stack frame is NULL for method %s\n", mono_method_get_full_name (method));
		return;
	}

	ProfilerStackFrame *top_frame;
	while(true) {
		g_assertf(top_stack_frame_index >= 0, "pop_and_record_frames: stack underflow\n");

		top_frame = &profiler_stack_frames[top_stack_frame_index];
		
		// don't call mono_wasm_profiler_now until we know we need to record
		top_frame->should_record = top_frame->should_record || should_record_frame (0.0);

		g_assert (top_frame->sp <= sp);

		// events from exception handling are not called for all frames
		// we are using stack pointer to determine what is the top of the profiler stack
		if (top_frame->sp < sp) {
			record_and_mark_parent (top_frame, top_stack_frame_index);

			// pop the top frame
			top_stack_frame_index--;
		}
		else if (top_frame->sp == sp) {
			if (leaving) {
				g_assertf(top_frame->method == method, "pop_and_record_frames: %d method mismatch top_frame %s != leave %s\n", top_stack_frame_index, mono_method_get_full_name (top_frame->method), mono_method_get_full_name (method));

				record_and_mark_parent (top_frame, top_stack_frame_index);

				// pop the frame we are leaving
				top_stack_frame_index--;
			}

			return;
		}
	}
}

static void
method_samplepoint (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	g_assert(top_stack_frame_index >= 0);

	sample_skip_counter++;

	ProfilerStackFrame *top_frame = &profiler_stack_frames[top_stack_frame_index];

	top_frame->should_record = top_frame->should_record || should_record_frame (0.0);
}

static void
method_leave (MonoProfiler *prof, MonoMethod *method, MonoProfilerCallContext *ctx)
{
	pop_and_record_frames (TRUE, method);
}

static void
method_exc_leave (MonoProfiler *prof, MonoMethod *method, MonoObject *exc)
{
	pop_and_record_frames (FALSE, method);
}

static void
tail_call (MonoProfiler *prof, MonoMethod *method, MonoMethod *target)
{
	pop_and_record_frames (TRUE, method);
	// TODO is this event duplicated with method_enter?
	method_enter (prof, target, NULL);
}

#endif /* HOST_BROWSER */

static MonoProfilerCallInstrumentationFlags
method_filter (MonoProfiler *prof, MonoMethod *method)
{
	if (!mono_callspec_eval (method, &callspec))
		return MONO_PROFILER_CALL_INSTRUMENTATION_NONE;

	if (needs_balanced_events) {
		// this is for the dev tools profiler
		// event handlers in this file require that enter/leave are balanced
		return 	MONO_PROFILER_CALL_INSTRUMENTATION_SAMPLEPOINT |
				MONO_PROFILER_CALL_INSTRUMENTATION_ENTER |
				MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE |
				MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL |
				MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE;
	} else {
		// sampling stack traces only requires subset of events
		// see ep-rt-mono-runtime-provider.c
		return 	MONO_PROFILER_CALL_INSTRUMENTATION_SAMPLEPOINT |
				MONO_PROFILER_CALL_INSTRUMENTATION_ENTER |
				MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE;
	}
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
	else if (match_option (arg, "eventpipe", NULL)) {
		needs_balanced_events = FALSE;
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


static void
mono_profiler_init_browser_common (const char *desc)
{
	desired_sample_interval_ms = 10;// ms
	memset (&callspec, 0, sizeof (MonoCallSpec));

	parse_args (desc);

	profiler_handle = mono_profiler_create (&browser_profiler);
	if (callspec.enabled) {
		mono_profiler_set_call_instrumentation_filter_callback (profiler_handle, method_filter);
	} else {
		g_warning ("Profiler: callspec is not enabled, no events will be generated.");
	}
}

// is both for eventpipe and devtools
void
mono_profiler_init_browser_aot (const char *desc)
{
	mono_profiler_init_browser_common (desc);
}

void
mono_profiler_init_browser_devtools (const char *desc)
{
	mono_profiler_init_browser_common (desc);

#ifdef HOST_BROWSER
	top_stack_frame_index = -1;
	last_sample_time = 0;
	prev_skips_per_period = 1;
	skips_per_period = 1;
	sample_skip_counter = 1;

	// install this only in production run, not in AOT run
	if(callspec.enabled) {
		mono_profiler_set_method_samplepoint_callback (profiler_handle, method_samplepoint);
		mono_profiler_set_method_enter_callback (profiler_handle, method_enter);
		mono_profiler_set_method_leave_callback (profiler_handle, method_leave);
		mono_profiler_set_method_tail_call_callback (profiler_handle, tail_call);
		mono_profiler_set_method_exception_leave_callback (profiler_handle, method_exc_leave);
	}
#endif /* HOST_BROWSER */
}

MonoProfilerHandle 
mono_profiler_init_browser_eventpipe (void)
{
	char *desc = g_getenv ("DOTNET_WasmPerformanceInstrumentation");
	mono_profiler_init_browser_common (desc);
	free(desc);
	return profiler_handle;
}

void
mono_profiler_fini_browser_eventpipe (void)
{
	mono_profiler_set_call_instrumentation_filter_callback (profiler_handle, NULL);
}