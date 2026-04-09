/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#ifndef __MONO_PROFILER_H__
#define __MONO_PROFILER_H__

#include <mono/metadata/details/profiler-types.h>

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

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/profiler-functions.h>
#undef MONO_API_FUNCTION

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
