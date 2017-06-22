/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

#ifndef __MONO_PROFILER_PRIVATE_H__
#define __MONO_PROFILER_PRIVATE_H__

#include <mono/metadata/profiler.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-os-semaphore.h>

struct _MonoProfilerDesc {
	MonoProfilerHandle next;
	MonoProfiler *prof;
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
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef _MONO_PROFILER_EVENT
};

typedef struct {
	gboolean startup_done;
	MonoProfilerHandle profilers;
	mono_lazy_init_t coverage_status;
	mono_mutex_t coverage_mutex;
	GHashTable *coverage_hash;
	MonoProfilerHandle sampling_owner;
	MonoSemType sampling_semaphore;
	MonoProfilerSampleMode sample_mode;
	uint64_t sample_freq;
	gboolean allocations;

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
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef _MONO_PROFILER_EVENT
} MonoProfilerState;

extern MonoProfilerState mono_profiler_state;

typedef struct {
	guint32 entries;
	struct {
		guchar *cil_code;
		guint32 count;
	} data [1];
} MonoProfilerCoverageInfo;

void mono_profiler_started (void);
void mono_profiler_cleanup (void);

static inline gboolean
mono_profiler_installed (void)
{
	return !!mono_profiler_state.profilers;
}

MonoProfilerCoverageInfo *mono_profiler_coverage_alloc (MonoMethod *method, guint32 entries);
void mono_profiler_coverage_free (MonoMethod *method);

gboolean mono_profiler_should_instrument_method (MonoMethod *method, gboolean entry);

gboolean mono_profiler_sampling_enabled (void);
void mono_profiler_sampling_thread_sleep (void);

static inline gboolean
mono_profiler_allocations_enabled (void)
{
	return mono_profiler_state.allocations;
}

#define _MONO_PROFILER_EVENT(name, ...) \
	void mono_profiler_raise_ ## name (__VA_ARGS__);
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
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef _MONO_PROFILER_EVENT

// These are the macros the rest of the runtime should use.

#define MONO_PROFILER_ENABLED(name) \
	G_UNLIKELY (mono_profiler_state.name ## _count)

#define MONO_PROFILER_RAISE(name, args) \
	do { \
		if (MONO_PROFILER_ENABLED (name)) \
			mono_profiler_raise_ ## name args; \
	} while (0)

#endif // __MONO_PROFILER_PRIVATE_H__
