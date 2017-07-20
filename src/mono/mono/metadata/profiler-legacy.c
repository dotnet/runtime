/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

#include <mono/metadata/profiler-private.h>

/*
 * The point of this file is to maintain compatibility with a few profiler API
 * functions used by Xamarin.{Android,iOS,Mac} so that they keep working
 * regardless of which system Mono version is used.
 *
 * TODO: Remove this some day if we're OK with breaking compatibility.
 */

typedef void *MonoLegacyProfiler;

typedef void (*MonoProfileFunc) (MonoLegacyProfiler *prof);
typedef void (*MonoProfileThreadFunc) (MonoLegacyProfiler *prof, uintptr_t tid);
typedef void (*MonoProfileGCFunc) (MonoLegacyProfiler *prof, MonoProfilerGCEvent event, int generation);
typedef void (*MonoProfileGCResizeFunc) (MonoLegacyProfiler *prof, int64_t new_size);
typedef void (*MonoProfileJitResult) (MonoLegacyProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo, int result);

struct _MonoProfiler {
	MonoProfilerHandle handle;
	MonoLegacyProfiler *profiler;
	MonoProfileFunc shutdown_callback;
	MonoProfileThreadFunc thread_start, thread_end;
	MonoProfileGCFunc gc_event;
	MonoProfileGCResizeFunc gc_heap_resize;
	MonoProfileJitResult jit_end2;
};

static MonoProfiler *current;

MONO_API void mono_profiler_install (MonoLegacyProfiler *prof, MonoProfileFunc callback);
MONO_API void mono_profiler_install_thread (MonoProfileThreadFunc start, MonoProfileThreadFunc end);
MONO_API void mono_profiler_install_gc (MonoProfileGCFunc callback, MonoProfileGCResizeFunc heap_resize_callback);
MONO_API void mono_profiler_install_jit_end (MonoProfileJitResult end);
MONO_API void mono_profiler_set_events (int flags);

static void
shutdown_cb (MonoProfiler *prof)
{
	prof->shutdown_callback (prof->profiler);
}

void
mono_profiler_install (MonoLegacyProfiler *prof, MonoProfileFunc callback)
{
	current = g_new0 (MonoProfiler, 1);
	current->handle = mono_profiler_create (current);
	current->profiler = prof;
	current->shutdown_callback = callback;

	if (callback)
		mono_profiler_set_runtime_shutdown_end_callback (current->handle, shutdown_cb);
}

static void
thread_start_cb (MonoProfiler *prof, uintptr_t tid)
{
	prof->thread_start (prof->profiler, tid);
}

static void
thread_stop_cb (MonoProfiler *prof, uintptr_t tid)
{
	prof->thread_end (prof->profiler, tid);
}

void
mono_profiler_install_thread (MonoProfileThreadFunc start, MonoProfileThreadFunc end)
{
	current->thread_start = start;
	current->thread_end = end;

	if (start)
		mono_profiler_set_thread_started_callback (current->handle, thread_start_cb);

	if (end)
		mono_profiler_set_thread_stopped_callback (current->handle, thread_stop_cb);
}

static void
gc_event_cb (MonoProfiler *prof, MonoProfilerGCEvent event, uint32_t generation)
{
	prof->gc_event (prof->profiler, event, generation);
}

static void
gc_resize_cb (MonoProfiler *prof, uintptr_t size)
{
	prof->gc_heap_resize (prof->profiler, size);
}

void
mono_profiler_install_gc (MonoProfileGCFunc callback, MonoProfileGCResizeFunc heap_resize_callback)
{
	current->gc_event = callback;
	current->gc_heap_resize = heap_resize_callback;

	if (callback)
		mono_profiler_set_gc_event_callback (current->handle, gc_event_cb);

	if (heap_resize_callback)
		mono_profiler_set_gc_resize_callback (current->handle, gc_resize_cb);
}

static void
jit_done_cb (MonoProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo)
{
	prof->jit_end2 (prof->profiler, method, jinfo, 0);
}

static void
jit_failed_cb (MonoProfiler *prof, MonoMethod *method)
{
	prof->jit_end2 (prof->profiler, method, NULL, 1);
}

void
mono_profiler_install_jit_end (MonoProfileJitResult end)
{
	current->jit_end2 = end;

	if (end) {
		mono_profiler_set_jit_done_callback (current->handle, jit_done_cb);
		mono_profiler_set_jit_failed_callback (current->handle, jit_failed_cb);
	}
}

void
mono_profiler_set_events (int flags)
{
	/* Do nothing. */
}
