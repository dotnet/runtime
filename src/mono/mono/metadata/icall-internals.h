/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ICALL_INTERNALS_H__
#define __MONO_METADATA_ICALL_INTERNALS_H__

#include <config.h>
#include <glib.h>
#include <mono/metadata/object-internals.h>

gconstpointer
mono_lookup_internal_call_full (MonoMethod *method, gboolean warn_on_missing, mono_bool *uses_handles, mono_bool *foreign);

MONO_PAL_API void
mono_add_internal_call_with_flags (const char *name, const void* method, gboolean cooperative);

MONO_PROFILER_API void
mono_add_internal_call_internal (const char *name, gconstpointer method);

MonoAssembly*
mono_runtime_get_caller_from_stack_mark (MonoStackCrawlMark *stack_mark);

typedef enum {
	MONO_ICALL_FLAGS_NONE = 0,
	MONO_ICALL_FLAGS_FOREIGN = 1 << 1,
	MONO_ICALL_FLAGS_USES_HANDLES = 1 << 2,
	MONO_ICALL_FLAGS_COOPERATIVE = 1 << 3,
	MONO_ICALL_FLAGS_NO_WRAPPER = 1 << 4
} MonoInternalCallFlags;

gconstpointer
mono_lookup_internal_call_full_with_flags (MonoMethod *method, gboolean warn_on_missing, guint32 *flags);

void
mono_dangerous_add_internal_call_coop (const char *name, const void* method);

void
mono_dangerous_add_internal_call_no_wrapper (const char *name, const void* method);

gboolean
mono_is_missing_icall_addr (gconstpointer addr);

#ifdef __cplusplus

#if !HOST_ANDROID

#include <type_traits>

#endif

template <typename T>
#if HOST_ANDROID
inline void
#else
inline typename std::enable_if<std::is_function<T>::value ||
			       std::is_function<typename std::remove_pointer<T>::type>::value >::type
#endif
mono_add_internal_call_with_flags (const char *name, T method, gboolean cooperative)
{
	return mono_add_internal_call_with_flags (name, (const void*)method, cooperative);
}

template <typename T>
#if HOST_ANDROID
inline void
#else
inline typename std::enable_if<std::is_function<T>::value ||
			       std::is_function<typename std::remove_pointer<T>::type>::value >::type
#endif
mono_add_internal_call_internal (const char *name, T method)
{
	return mono_add_internal_call_internal (name, (const void*)method);
}

#endif // __cplusplus

#endif /* __MONO_METADATA_ICALL_INTERNALS_H__ */
