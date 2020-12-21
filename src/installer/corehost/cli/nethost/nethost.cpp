// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "nethost.h"
#include <error_codes.h>
#include <fxr_resolver.h>
#include <pal.h>
#include <trace.h>
#include <utils.h>

namespace
{
    // Swallow the trace messages so we don't output to stderr of a process that we do not own unless tracing is enabled.
    void __cdecl swallow_trace(const pal::char_t* msg)
    {
        (void)msg;
    }
}

NETHOST_API int NETHOST_CALLTYPE get_hostfxr_path(
    char_t * buffer,
    size_t * buffer_size,
    const struct get_hostfxr_parameters *parameters)
{
    if (buffer_size == nullptr)
        return StatusCode::InvalidArgFailure;

    trace::setup();
    error_writer_scope_t writer_scope(swallow_trace);

    size_t min_parameters_size = offsetof(get_hostfxr_parameters, dotnet_root) + sizeof(const char_t*);
    if (parameters != nullptr && parameters->size < min_parameters_size)
    {
        trace::error(_X("Invalid size for get_hostfxr_parameters. Expected at least %d"), min_parameters_size);
        return StatusCode::InvalidArgFailure;
    }

    pal::dll_t fxr;
    pal::string_t fxr_path;
    if (!fxr_resolver::try_get_existing_fxr(&fxr, &fxr_path))
    {
        if (parameters != nullptr && parameters->dotnet_root != nullptr)
        {
            pal::string_t dotnet_root = parameters->dotnet_root;
            trace::info(_X("Using dotnet root parameter [%s] as runtime location."), dotnet_root.c_str());
            if (!fxr_resolver::try_get_path_from_dotnet_root(dotnet_root, &fxr_path))
                return StatusCode::CoreHostLibMissingFailure;
        }
        else
        {
            pal::string_t root_path;
            if (parameters != nullptr && parameters->assembly_path != nullptr)
                root_path = get_directory(parameters->assembly_path);

            pal::string_t dotnet_root;
            if (!fxr_resolver::try_get_path(root_path, &dotnet_root, &fxr_path))
                return StatusCode::CoreHostLibMissingFailure;
        }
    }

    size_t len = fxr_path.length();
    size_t required_size = len + 1; // null terminator

    size_t input_buffer_size = *buffer_size;
    *buffer_size = required_size;
    if (buffer == nullptr || input_buffer_size < required_size)
        return StatusCode::HostApiBufferTooSmall;

    fxr_path.copy(buffer, len);
    buffer[len] = '\0';
    return StatusCode::Success;
}
