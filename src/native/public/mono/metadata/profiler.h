/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#ifndef __MONO_PROFILER_H__
#define __MONO_PROFILER_H__

#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/object.h>

MONO_BEGIN_DECLS

/**
 * This value will be incremented whenever breaking changes to the profiler API
 * are made. This macro is intended for use in profiler modules that wish to
 * support older versions of the profiler API.
 *
 * Version 2:
 * - Major overhaul of the profiler API.
 * Version 3:
 * - Added mono_profiler_enable_clauses (). This must now be called to enable
 *   raising exception_clause events.
 * - The exception argument to exception_clause events can now be NULL for
 *   finally clauses invoked in the non-exceptional case.
 * - The type argument to exception_clause events will now correctly indicate
 *   that the catch portion of the clause is being executed in the case of
 *   try-filter-catch clauses.
 * - Removed the iomap_report event.
 * - Removed the old gc_event event and renamed gc_event2 to gc_event.
 */
#define MONO_PROFILER_API_VERSION 3

typedef struct _MonoProfiler MonoProfiler;
typedef struct _MonoProfilerDesc *MonoProfilerHandle;

typedef void (*MonoProfilerCleanupCallback) (MonoProfiler *prof);

MONO_API void mono_profiler_load (const char *desc);
MONO_API MonoProfilerHandle mono_profiler_create (MonoProfiler *prof);
MONO_API void mono_profiler_set_cleanup_callback (MonoProfilerHandle handle, MonoProfilerCleanupCallback cb);

typedef struct {
	MonoMethod *method;
	uint32_t il_offset;
	uint32_t counter;
	const char *file_name;
	uint32_t line;
	uint32_t column;
} MonoProfilerCoverageData;

typedef mono_bool (*MonoProfilerCoverageFilterCallback) (MonoProfiler *prof, MonoMethod *method);
typedef void (*MonoProfilerCoverageCallback) (MonoProfiler *prof, const MonoProfilerCoverageData *data);

MONO_API mono_bool mono_profiler_enable_coverage (void);
MONO_API void mono_profiler_set_coverage_filter_callback (MonoProfilerHandle handle, MonoProfilerCoverageFilterCallback cb);
MONO_API mono_bool mono_profiler_get_coverage_data (MonoProfilerHandle handle, MonoMethod *method, MonoProfilerCoverageCallback cb);

typedef enum {
	/**
	 * Do not perform sampling. Will make the sampling thread sleep until the
	 * sampling mode is changed to one of the below modes.
	 */
	MONO_PROFILER_SAMPLE_MODE_NONE = 0,
	/**
	 * Try to base sampling frequency on process activity. Falls back to
	 * MONO_PROFILER_SAMPLE_MODE_REAL if such a clock is not available.
	 */
	MONO_PROFILER_SAMPLE_MODE_PROCESS = 1,
	/**
	 * Base sampling frequency on wall clock time. Uses a monotonic clock when
	 * available (all major platforms).
	 */
	MONO_PROFILER_SAMPLE_MODE_REAL = 2,
} MonoProfilerSampleMode;

MONO_API mono_bool mono_profiler_enable_sampling (MonoProfilerHandle handle);
MONO_API mono_bool mono_profiler_set_sample_mode (MonoProfilerHandle handle, MonoProfilerSampleMode mode, uint32_t freq);
MONO_API mono_bool mono_profiler_get_sample_mode (MonoProfilerHandle handle, MonoProfilerSampleMode *mode, uint32_t *freq);

MONO_API mono_bool mono_profiler_enable_allocations (void);
MONO_API mono_bool mono_profiler_enable_clauses (void);

typedef struct _MonoProfilerCallContext MonoProfilerCallContext;

typedef enum {
	/**
	 * Do not instrument calls.
	 */
	MONO_PROFILER_CALL_INSTRUMENTATION_NONE = 0,
	/**
	 * Instrument method entries.
	 */
	MONO_PROFILER_CALL_INSTRUMENTATION_ENTER = 1 << 1,
	/**
	 * Also capture a call context for method entries.
	 */
	MONO_PROFILER_CALL_INSTRUMENTATION_ENTER_CONTEXT = 1 << 2,
	/**
	 * Instrument method exits.
	 */
	MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE = 1 << 3,
	/**
	 * Also capture a call context for method exits.
	 */
	MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE_CONTEXT = 1 << 4,
	/**
	 * Instrument method exits as a result of a tail call.
	 */
	MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL = 1 << 5,
	/**
	 * Instrument exceptional method exits.
	 */
	MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE = 1 << 6,
} MonoProfilerCallInstrumentationFlags;

typedef MonoProfilerCallInstrumentationFlags (*MonoProfilerCallInstrumentationFilterCallback) (MonoProfiler *prof, MonoMethod *method);

MONO_API void mono_profiler_set_call_instrumentation_filter_callback (MonoProfilerHandle handle, MonoProfilerCallInstrumentationFilterCallback cb);
MONO_API mono_bool mono_profiler_enable_call_context_introspection (void);
MONO_API void *mono_profiler_call_context_get_this (MonoProfilerCallContext *context);
MONO_API void *mono_profiler_call_context_get_argument (MonoProfilerCallContext *context, uint32_t position);
MONO_API void *mono_profiler_call_context_get_local (MonoProfilerCallContext *context, uint32_t position);
MONO_API void *mono_profiler_call_context_get_result (MonoProfilerCallContext *context);
MONO_API void mono_profiler_call_context_free_buffer (void *buffer);

typedef enum {
	/**
	 * The \c data parameter is a \c MonoMethod pointer.
	 */
	MONO_PROFILER_CODE_BUFFER_METHOD = 0,
	/**
	 * \deprecated No longer used.
	 */
	MONO_PROFILER_CODE_BUFFER_METHOD_TRAMPOLINE = 1,
	/**
	 * The \c data parameter is a \c MonoMethod pointer.
	 */
	MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE = 2,
	MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE = 3,
	MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE = 4,
	/**
	 * The \c data parameter is a C string.
	 */
	MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE = 5,
	MONO_PROFILER_CODE_BUFFER_HELPER = 6,
	/**
	 * \deprecated No longer used.
	 */
	MONO_PROFILER_CODE_BUFFER_MONITOR = 7,
	MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE = 8,
	MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING = 9,
} MonoProfilerCodeBufferType;

typedef enum {
	MONO_GC_EVENT_PRE_STOP_WORLD = 6,
	/**
	 * When this event arrives, the GC and suspend locks are acquired.
	 */
	MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED = 10,
	MONO_GC_EVENT_POST_STOP_WORLD = 7,
	MONO_GC_EVENT_START = 0,
	MONO_GC_EVENT_END = 5,
	MONO_GC_EVENT_PRE_START_WORLD = 8,
	/**
	 * When this event arrives, the GC and suspend locks are released.
	 */
	MONO_GC_EVENT_POST_START_WORLD_UNLOCKED = 11,
	MONO_GC_EVENT_POST_START_WORLD = 9,
} MonoProfilerGCEvent;

/*
 * The macros below will generate the majority of the callback API. Refer to
 * mono/metadata/profiler-events.h for a list of callbacks. They are expanded
 * like so:
 *
 * typedef void (*MonoProfilerRuntimeInitializedCallback (MonoProfiler *prof);
 * MONO_API void mono_profiler_set_runtime_initialized_callback (MonoProfiler *prof, MonoProfilerRuntimeInitializedCallback cb);
 *
 * typedef void (*MonoProfilerRuntimeShutdownCallback (MonoProfiler *prof);
 * MONO_API void mono_profiler_set_runtime_shutdown_callback (MonoProfiler *prof, MonoProfilerRuntimeShutdownCallback cb);
 *
 * typedef void (*MonoProfilerContextLoadedCallback (MonoProfiler *prof);
 * MONO_API void mono_profiler_set_context_loaded_callback (MonoProfiler *prof, MonoProfilerContextLoadedCallback cb);
 *
 * typedef void (*MonoProfilerContextUnloadedCallback (MonoProfiler *prof);
 * MONO_API void mono_profiler_set_context_unloaded_callback (MonoProfiler *prof, MonoProfilerContextUnloadedCallback cb);
 *
 * Etc.
 *
 * To remove a callback, pass NULL instead of a valid function pointer.
 * Callbacks can be changed at any point, but note that doing so is inherently
 * racy with respect to threads that aren't suspended, i.e. you may still see a
 * call from another thread right after you change a callback.
 *
 * These functions are async safe.
 */

#define _MONO_PROFILER_EVENT(type, ...) \
	typedef void (*MonoProfiler ## type ## Callback) (__VA_ARGS__);
#define MONO_PROFILER_EVENT_0(name, type) \
		_MONO_PROFILER_EVENT(type, MonoProfiler *prof)
#define MONO_PROFILER_EVENT_1(name, type, arg1_type, arg1_name) \
		_MONO_PROFILER_EVENT(type, MonoProfiler *prof, arg1_type arg1_name)
#define MONO_PROFILER_EVENT_2(name, type, arg1_type, arg1_name, arg2_type, arg2_name) \
		_MONO_PROFILER_EVENT(type, MonoProfiler *prof, arg1_type arg1_name, arg2_type arg2_name)
#define MONO_PROFILER_EVENT_3(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name) \
		_MONO_PROFILER_EVENT(type, MonoProfiler *prof, arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name)
#define MONO_PROFILER_EVENT_4(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name) \
		_MONO_PROFILER_EVENT(type, MonoProfiler *prof, arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name, arg4_type arg4_name)
#define MONO_PROFILER_EVENT_5(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name, arg5_type, arg5_name) \
		_MONO_PROFILER_EVENT(type, MonoProfiler *prof, arg1_type arg1_name, arg2_type arg2_name, arg3_type arg3_name, arg4_type arg4_name, arg5_type arg5_name)
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef MONO_PROFILER_EVENT_5
#undef _MONO_PROFILER_EVENT

#define _MONO_PROFILER_EVENT(name, type) \
	MONO_API void mono_profiler_set_ ## name ## _callback (MonoProfilerHandle handle, MonoProfiler ## type ## Callback cb);
#define MONO_PROFILER_EVENT_0(name, type) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_1(name, type, arg1_type, arg1_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_2(name, type, arg1_type, arg1_name, arg2_type, arg2_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_3(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_4(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name) \
	_MONO_PROFILER_EVENT(name, type)
#define MONO_PROFILER_EVENT_5(name, type, arg1_type, arg1_name, arg2_type, arg2_name, arg3_type, arg3_name, arg4_type, arg4_name, arg5_type, arg5_name) \
	_MONO_PROFILER_EVENT(name, type)
#include <mono/metadata/profiler-events.h>
#undef MONO_PROFILER_EVENT_0
#undef MONO_PROFILER_EVENT_1
#undef MONO_PROFILER_EVENT_2
#undef MONO_PROFILER_EVENT_3
#undef MONO_PROFILER_EVENT_4
#undef MONO_PROFILER_EVENT_5
#undef _MONO_PROFILER_EVENT

MONO_END_DECLS

#endif // __MONO_PROFILER_H__
