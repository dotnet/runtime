// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(void, mono_profiler_load, (const char *desc))
MONO_API_FUNCTION(MonoProfilerHandle, mono_profiler_create, (MonoProfiler *prof))
MONO_API_FUNCTION(void, mono_profiler_set_cleanup_callback, (MonoProfilerHandle handle, MonoProfilerCleanupCallback cb))

MONO_API_FUNCTION(mono_bool, mono_profiler_enable_coverage, (void))
MONO_API_FUNCTION(void, mono_profiler_set_coverage_filter_callback, (MonoProfilerHandle handle, MonoProfilerCoverageFilterCallback cb))
MONO_API_FUNCTION(mono_bool, mono_profiler_get_coverage_data, (MonoProfilerHandle handle, MonoMethod *method, MonoProfilerCoverageCallback cb))

MONO_API_FUNCTION(mono_bool, mono_profiler_enable_sampling, (MonoProfilerHandle handle))
MONO_API_FUNCTION(mono_bool, mono_profiler_set_sample_mode, (MonoProfilerHandle handle, MonoProfilerSampleMode mode, uint32_t freq))
MONO_API_FUNCTION(mono_bool, mono_profiler_get_sample_mode, (MonoProfilerHandle handle, MonoProfilerSampleMode *mode, uint32_t *freq))

MONO_API_FUNCTION(mono_bool, mono_profiler_enable_allocations, (void))
MONO_API_FUNCTION(mono_bool, mono_profiler_enable_clauses, (void))

MONO_API_FUNCTION(void, mono_profiler_set_call_instrumentation_filter_callback, (MonoProfilerHandle handle, MonoProfilerCallInstrumentationFilterCallback cb))
MONO_API_FUNCTION(mono_bool, mono_profiler_enable_call_context_introspection, (void))
MONO_API_FUNCTION(void *, mono_profiler_call_context_get_this, (MonoProfilerCallContext *context))
MONO_API_FUNCTION(void *, mono_profiler_call_context_get_argument, (MonoProfilerCallContext *context, uint32_t position))
MONO_API_FUNCTION(void *, mono_profiler_call_context_get_local, (MonoProfilerCallContext *context, uint32_t position))
MONO_API_FUNCTION(void *, mono_profiler_call_context_get_result, (MonoProfilerCallContext *context))
MONO_API_FUNCTION(void, mono_profiler_call_context_free_buffer, (void *buffer))
