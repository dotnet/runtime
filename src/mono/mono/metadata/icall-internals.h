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

// On Windows platform implementation of bellow methods are hosted in separate source file
// icall-windows.c or icall-windows-*.c. On other platforms the implementation is still keept
// in icall.c still declared as static and in some places even inlined.
#ifdef HOST_WIN32
void
mono_icall_make_platform_path (gchar *path);

const gchar *
mono_icall_get_file_path_prefix (const gchar *path);

gpointer
mono_icall_module_get_hinstance (MonoImage *image);

MonoStringHandle
mono_icall_get_machine_name (MonoError *error);

int
mono_icall_get_platform (void);

MonoStringHandle
mono_icall_get_new_line (MonoError *error);

MonoBoolean
mono_icall_is_64bit_os (void);

MonoArrayHandle
mono_icall_get_environment_variable_names (MonoError *error);

void
mono_icall_set_environment_variable (MonoString *name, MonoString *value);

MonoStringHandle
mono_icall_get_windows_folder_path (int folder, MonoError *error);

void
mono_icall_write_windows_debug_string (const gunichar2 *message);

gint32
mono_icall_wait_for_input_idle (gpointer handle, gint32 milliseconds);
#endif  /* HOST_WIN32 */

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
