// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "nethost.h"
#include <error_codes.h>
#include <fxr_resolver.h>
#include <pal.h>
#include <trace.h>
#include <utils.h>

namespace
{
    // Swallow the trace messages so we don't output to stderr of a process that we do not own unless tracing is enabled.
    void swallow_trace(const pal::char_t* msg)
    {
        (void)msg;
    }
}

NETHOST_API int NETHOST_CALLTYPE get_hostfxr_path(
    char_t * buffer,
    size_t * buffer_size,
    const char_t * assembly_path)
{
    if (buffer_size == nullptr)
        return StatusCode::InvalidArgFailure;

    trace::setup();
    error_writer_scope_t writer_scope(swallow_trace);

    pal::string_t root_path;
    if (assembly_path != nullptr)
        root_path = get_directory(assembly_path);

    pal::dll_t fxr;
    pal::string_t fxr_path;
    if (!fxr_resolver::try_get_existing_fxr(&fxr, &fxr_path))
    {
        pal::string_t dotnet_root;
        if(!fxr_resolver::try_get_path(root_path, &dotnet_root, &fxr_path))
            return StatusCode::CoreHostLibMissingFailure;
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
