/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

#ifndef __MONO_PROFILER_LEGACY_H__
#define __MONO_PROFILER_LEGACY_H__

#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-forward.h>
#include <mono/metadata/object-forward.h>
#include <mono/metadata/profiler.h>

/*
 * The following code is here to maintain compatibility with a few profiler API
 * functions used by Xamarin.{Android,iOS,Mac} so that they keep working
 * regardless of which system Mono version is used.
 *
 * TODO: Remove this some day if we're OK with breaking compatibility.
 */

typedef void *MonoLegacyProfiler;

typedef void (*MonoLegacyProfileFunc) (MonoLegacyProfiler *prof);
typedef void (*MonoLegacyProfileThreadFunc) (MonoLegacyProfiler *prof, uintptr_t tid);
typedef void (*MonoLegacyProfileGCFunc) (MonoLegacyProfiler *prof, MonoProfilerGCEvent event, int generation);
typedef void (*MonoLegacyProfileGCResizeFunc) (MonoLegacyProfiler *prof, int64_t new_size);
typedef void (*MonoLegacyProfileJitResult) (MonoLegacyProfiler *prof, MonoMethod *method, MonoJitInfo *jinfo, int result);
typedef void (*MonoLegacyProfileAllocFunc) (MonoLegacyProfiler *prof, MonoObject *obj, MonoClass *klass);
typedef void (*MonoLegacyProfileMethodFunc) (MonoLegacyProfiler *prof, MonoMethod *method);
typedef void (*MonoLegacyProfileExceptionFunc) (MonoLegacyProfiler *prof, MonoObject *object);
typedef void (*MonoLegacyProfileExceptionClauseFunc) (MonoLegacyProfiler *prof, MonoMethod *method, int clause_type, int clause_num);

MONO_DEPRECATED void mono_profiler_install (MonoLegacyProfiler *prof, MonoLegacyProfileFunc callback);
MONO_DEPRECATED void mono_profiler_install_thread (MonoLegacyProfileThreadFunc start, MonoLegacyProfileThreadFunc end);
MONO_DEPRECATED void mono_profiler_install_gc (MonoLegacyProfileGCFunc callback, MonoLegacyProfileGCResizeFunc heap_resize_callback);
MONO_DEPRECATED void mono_profiler_install_jit_end (MonoLegacyProfileJitResult end);
MONO_DEPRECATED void mono_profiler_set_events (int flags);
MONO_DEPRECATED void mono_profiler_install_allocation (MonoLegacyProfileAllocFunc callback);
MONO_DEPRECATED void mono_profiler_install_enter_leave (MonoLegacyProfileMethodFunc enter, MonoLegacyProfileMethodFunc fleave);
MONO_DEPRECATED void mono_profiler_install_exception (MonoLegacyProfileExceptionFunc throw_callback, MonoLegacyProfileMethodFunc exc_method_leave, MonoLegacyProfileExceptionClauseFunc clause_callback);

#endif // __MONO_PROFILER_LEGACY_H__
