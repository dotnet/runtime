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

NETHOST_API int NETHOST_CALLTYPE nethost_get_hostfxr_path(
    char_t * result_buffer,
    size_t buffer_size,
    size_t * out_buffer_required_size,
    const char_t * assembly_path)
{
    if (out_buffer_required_size == nullptr)
        return StatusCode::InvalidArgFailure;

    trace::setup();
    error_writer_scope_t writer_scope(swallow_trace);

    pal::string_t root_path;
    if (assembly_path != nullptr)
        root_path = get_directory(assembly_path);

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if(!fxr_resolver::try_get_path(root_path, &dotnet_root, &fxr_path))
        return StatusCode::CoreHostLibMissingFailure;

    size_t len = fxr_path.length();
    size_t required_size = len + 1; // null terminator

    *out_buffer_required_size = required_size;
    if (result_buffer == nullptr || buffer_size < required_size)
        return StatusCode::HostApiBufferTooSmall;

    fxr_path.copy(result_buffer, len);
    result_buffer[len] = '\0';
    return StatusCode::Success;
}