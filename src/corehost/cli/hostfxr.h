// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _COREHOST_CLI_HOSTFXR_H_
#define _COREHOST_CLI_HOSTFXR_H_

#include <pal.h>

// Forward declaration of required custom feature APIs
using hostfxr_main_fn = int32_t(*)(const int argc, const pal::char_t* argv[]);
using hostfxr_main_startupinfo_fn = int32_t(*)(
    const int argc,
    const pal::char_t* argv[],
    const pal::char_t* host_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* app_path);

enum class hostfxr_delegate_type
{
    com_activation,
    load_in_memory_assembly,
    winrt_activation,
    com_register,
    com_unregister
};

using hostfxr_main_fn = int32_t(*)(const int argc, const pal::char_t* argv[]);
using hostfxr_main_startupinfo_fn = int32_t(*)(
    const int argc,
    const pal::char_t* argv[],
    const pal::char_t* host_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* app_path);
using hostfxr_error_writer_fn = void(*)(const pal::char_t* message);
using hostfxr_set_error_writer_fn = hostfxr_error_writer_fn(*)(hostfxr_error_writer_fn error_writer);

using hostfxr_handle = void*;
struct hostfxr_initialize_parameters
{
    size_t size;
    const pal::char_t *host_path;
    const pal::char_t *dotnet_root;
};

using hostfxr_initialize_for_dotnet_command_line_fn = int32_t(__cdecl *)(
    int argc,
    const pal::char_t *argv[],
    const hostfxr_initialize_parameters *parameters,
    /*out*/ hostfxr_handle *host_context_handle);
using hostfxr_initialize_for_runtime_config_fn = int32_t(__cdecl *)(
    const pal::char_t *runtime_config_path,
    const hostfxr_initialize_parameters*parameters,
    /*out*/ hostfxr_handle *host_context_handle);

using hostfxr_get_runtime_property_value_fn = int32_t(__cdecl *)(
    const hostfxr_handle host_context_handle,
    const pal::char_t *name,
    /*out*/ const pal::char_t **value);
using hostfxr_set_runtime_property_value_fn = int32_t(__cdecl *)(
    const hostfxr_handle host_context_handle,
    const pal::char_t *name,
    const pal::char_t *value);
using hostfxr_get_runtime_properties_fn = int32_t(__cdecl *)(
    const hostfxr_handle host_context_handle,
    /*inout*/ size_t * count,
    /*out*/ const pal::char_t **keys,
    /*out*/ const pal::char_t **values);

using hostfxr_run_app_fn = int32_t(__cdecl *)(const hostfxr_handle host_context_handle);
using hostfxr_get_runtime_delegate_fn = int32_t(__cdecl *)(
    const hostfxr_handle host_context_handle,
    hostfxr_delegate_type type,
    /*out*/ void **delegate);

using hostfxr_close_fn = int32_t(__cdecl *)(const hostfxr_handle host_context_handle);

#endif //_COREHOST_CLI_HOSTFXR_H_
