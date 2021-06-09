/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#ifndef __MONO_PROFILER_PRIVATE_H__
#define __MONO_PROFILER_PRIVATE_H__

#include <mono/metadata/class-internals.h>
#include <mono/metadata/profiler.h>
#include <mono/utils/mono-context.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-os-semaphore.h>
#include <mono/metadata/icalls.h>

struct _MonoProfilerDesc {
	MonoProfilerHandle next;
	MonoProfiler *prof;
	volatile gpointer cleanup_callback;
	volatile gpointer coverage_filter;
	volatile gpointer call_instrumentation_filter;

#define _MONO_PROFILER_EVENT(name) \
	volatile gpointer name ## _cb;
#define MONO_PROFILER_EVENT_0(name, type) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_1(name, type, arg1_type, arg1_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_2(name, type, arg1_type, arg1_name, arg2_type, arg2_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_3(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_4(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_5(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name, arg5_type, arg5_name) \
	_MONO_PROFILER_EVENT(name)
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef MONO_PROFILER_EVENT_5
#undef _MONO_PROFILER_EVENT
};

typedef struct {
	gboolean startup_done;

	MonoProfilerHandle profilers;

	gboolean code_coverage;
	mono_mutex_t coverage_mutex;
	GHashTable *coverage_hash;

	MonoProfilerHandle sampling_owner;
	MonoSemType sampling_semaphore;
	MonoProfilerSampleMode sample_mode;
	guint32 sample_freq;

	gboolean allocations;

	gboolean clauses;

	gboolean call_contexts;
	void (*context_enable) (void);
	gpointer (*context_get_this) (MonoProfilerCallContext *);
	gpointer (*context_get_argument) (MonoProfilerCallContext *, guint32);
	gpointer (*context_get_local) (MonoProfilerCallContext *, guint32);
	gpointer (*context_get_result) (MonoProfilerCallContext *);
	void (*context_free_buffer) (gpointer);

#define _MONO_PROFILER_EVENT(name) \
	volatile gint32 name ## _count;
#define MONO_PROFILER_EVENT_0(name, type) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_1(name, type, arg1_type, arg1_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_2(name, type, arg1_type, arg1_name, arg2_type, arg2_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_3(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_4(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name) \
	_MONO_PROFILER_EVENT(name)
#define MONO_PROFILER_EVENT_5(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name, arg5_type, arg5_name) \
	_MONO_PROFILER_EVENT(name)
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef MONO_PROFILER_EVENT_5
#undef _MONO_PROFILER_EVENT
} MonoProfilerState;

extern MonoProfilerState mono_profiler_state;

typedef struct {
	guchar *cil_code;
	guint32 count;
} MonoProfilerCoverageInfoEntry;

typedef struct {
	guint32 entries;
	MonoProfilerCoverageInfoEntry data [MONO_ZERO_LEN_ARRAY];
} MonoProfilerCoverageInfo;

void mono_profiler_started (void);

static inline gboolean
mono_profiler_installed (void)
{
	return !!mono_profiler_state.profilers;
}

gboolean mono_profiler_coverage_instrumentation_enabled (MonoMethod *method);
MonoProfilerCoverageInfo *mono_profiler_coverage_alloc (MonoMethod *method, guint32 entries);

struct _MonoProfilerCallContext {
	/*
	 * Must be the first field (the JIT relies on it). Only filled out if this
	 * is a JIT frame; otherwise, zeroed.
	 */
	MonoContext context;
	/*
	 * A non-NULL MonoInterpFrameHandle if this is an interpreter frame.
	 */
	gpointer interp_frame;
	MonoMethod *method;
	/*
	 * Points to the return value for an epilogue context. For a prologue, this
	 * is set to NULL.
	 */
	gpointer return_value;
	/*
	 * Points to an array of addresses of stack slots holding the arguments.
	 */
	gpointer *args;
};

MonoProfilerCallInstrumentationFlags mono_profiler_get_call_instrumentation_flags (MonoMethod *method);

gboolean mono_profiler_sampling_enabled (void);
void mono_profiler_sampling_thread_post (void);
void mono_profiler_sampling_thread_wait (void);

static inline gboolean
mono_profiler_allocations_enabled (void)
{
	return mono_profiler_state.allocations;
}

static inline gboolean
mono_profiler_clauses_enabled (void)
{
	return mono_profiler_state.clauses;
}

#define _MONO_PROFILER_EVENT(name, ...) \
	ICALL_EXPORT void mono_profiler_raise_ ## name (__VA_ARGS__);
#define MONO_PROFILER_EVENT_0(name, type) \
	_MONO_PROFILER_EVENT(name, void)
#define MONO_PROFILER_EVENT_1(name, type, arg1_type, arg1_name) \
	_MONO_PROFILER_EVENT(name, arg1_type arg1_name)
#define MONO_PROFILER_EVENT_2(name, type, arg1_type, arg1_name, arg2_type, arg2_name) \
	_MONO_PROFILER_EVENT(name, arg1_type arg1_name, arg2_type arg2_name)
#define MONO_PROFILER_EVENT_3(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name) \
	_MONO_PROFILER_EVENT(name, arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name)
#define MONO_PROFILER_EVENT_4(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name) \
	_MONO_PROFILER_EVENT(name, arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name, arg4_type arg4_name)
#define MONO_PROFILER_EVENT_5(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name, arg5_type, arg5_name) \
	_MONO_PROFILER_EVENT(name, arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name, arg4_type arg4_name, arg5_type arg5_name)
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef MONO_PROFILER_EVENT_5
#undef _MONO_PROFILER_EVENT

/* These are the macros the rest of the runtime should use. */

#define MONO_PROFILER_ENABLED(name) \
	G_UNLIKELY (mono_profiler_state.name ## _count)

#define MONO_PROFILER_RAISE(name, args) \
	do { \
		if (MONO_PROFILER_ENABLED (name)) \
			mono_profiler_raise_ ## name args; \
	} while (0)

#endif // __MONO_PROFILER_PRIVATE_H__
