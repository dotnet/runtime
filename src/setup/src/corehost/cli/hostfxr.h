// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __HOSTFXR_H__
#define __HOSTFXR_H__

#include <stddef.h>
#include <stdint.h>

#if defined(_WIN32)
    #define HOSTFXR_CALLTYPE __cdecl
    #ifdef _WCHAR_T_DEFINED
        typedef wchar_t char_t;
    #else
        typedef unsigned short char_t;
    #endif
#else
    #define HOSTFXR_CALLTYPE
    typedef char char_t;
#endif

enum hostfxr_delegate_type
{
    hdt_com_activation,
    hdt_load_in_memory_assembly,
    hdt_winrt_activation,
    hdt_com_register,
    hdt_com_unregister,
    hdt_load_assembly_and_get_function_pointer
};

typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_main_fn)(const int argc, const char_t **argv);
typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_main_startupinfo_fn)(
    const int argc,
    const char_t **argv,
    const char_t *host_path,
    const char_t *dotnet_root,
    const char_t *app_path);

typedef void(HOSTFXR_CALLTYPE *hostfxr_error_writer_fn)(const char_t *message);
typedef hostfxr_error_writer_fn(HOSTFXR_CALLTYPE *hostfxr_set_error_writer_fn)(hostfxr_error_writer_fn error_writer);

typedef void* hostfxr_handle;
struct hostfxr_initialize_parameters
{
    size_t size;
    const char_t *host_path;
    const char_t *dotnet_root;
};

typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_initialize_for_dotnet_command_line_fn)(
    int argc,
    const char_t **argv,
    const struct hostfxr_initialize_parameters *parameters,
    /*out*/ hostfxr_handle *host_context_handle);
typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_initialize_for_runtime_config_fn)(
    const char_t *runtime_config_path,
    const struct hostfxr_initialize_parameters *parameters,
    /*out*/ hostfxr_handle *host_context_handle);

typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_get_runtime_property_value_fn)(
    const hostfxr_handle host_context_handle,
    const char_t *name,
    /*out*/ const char_t **value);
typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_set_runtime_property_value_fn)(
    const hostfxr_handle host_context_handle,
    const char_t *name,
    const char_t *value);
typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_get_runtime_properties_fn)(
    const hostfxr_handle host_context_handle,
    /*inout*/ size_t * count,
    /*out*/ const char_t **keys,
    /*out*/ const char_t **values);

typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_run_app_fn)(const hostfxr_handle host_context_handle);
typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_get_runtime_delegate_fn)(
    const hostfxr_handle host_context_handle,
    enum hostfxr_delegate_type type,
    /*out*/ void **delegate);

typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_close_fn)(const hostfxr_handle host_context_handle);

#endif //__HOSTFXR_H__
