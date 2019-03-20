// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    load_in_memory_assembly
};

using hostfxr_get_delegate_fn = int32_t(*)(
    const pal::char_t* host_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* app_path,
    hostfxr_delegate_type type,
    void** delegate);

using hostfxr_main_fn = int32_t(*)(const int argc, const pal::char_t* argv[]);
using hostfxr_main_startupinfo_fn = int32_t(*)(
    const int argc,
    const pal::char_t* argv[],
    const pal::char_t* host_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* app_path);
using hostfxr_error_writer_fn = void(*)(const pal::char_t* message);
using hostfxr_set_error_writer_fn = hostfxr_error_writer_fn(*)(hostfxr_error_writer_fn error_writer);

#endif //_COREHOST_CLI_HOSTFXR_H_
