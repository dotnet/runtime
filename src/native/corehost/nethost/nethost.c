// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "nethost.h"
#include <error_codes.h>
#include <fxr_resolver.h>
#include <pal.h>
#include <trace.h>
#include <utils.h>

#include <stddef.h>
#include <stdlib.h>
#include <string.h>

// Swallow the trace messages so we don't output to stderr of a process that we
// do not own unless tracing is enabled.
static void __cdecl no_trace(const pal_char_t* msg)
{
    (void)msg;
}

static int get_hostfxr_path_internal(
    char_t* buffer,
    size_t* buffer_size,
    const struct get_hostfxr_parameters* parameters)
{
    size_t min_parameters_size = offsetof(struct get_hostfxr_parameters, dotnet_root) + sizeof(const char_t*);
    if (parameters != NULL && parameters->size < min_parameters_size)
    {
        trace_error(_X("Invalid size for get_hostfxr_parameters. Expected at least %zu"), min_parameters_size);
        return InvalidArgFailure;
    }

    pal_dll_t fxr = NULL;
    pal_char_t* fxr_path = NULL;
    if (!fxr_resolver_try_get_existing_fxr(&fxr, &fxr_path))
    {
        if (parameters != NULL && parameters->dotnet_root != NULL)
        {
            trace_info(_X("Using dotnet root parameter [%s] as runtime location."), parameters->dotnet_root);
            if (!fxr_resolver_try_get_path_from_dotnet_root(parameters->dotnet_root, &fxr_path))
                return CoreHostLibMissingFailure;
        }
        else
        {
            pal_char_t* root_path = NULL;
            if (parameters != NULL && parameters->assembly_path != NULL)
                root_path = utils_get_directory(parameters->assembly_path);

            pal_char_t* dotnet_root = NULL;
            bool resolved = fxr_resolver_try_get_path(root_path, fxr_search_location_default, NULL, &dotnet_root, &fxr_path);
            free(root_path);
            free(dotnet_root);
            if (!resolved)
                return CoreHostLibMissingFailure;
        }
    }

    size_t len = pal_strlen(fxr_path);
    size_t required_size = len + 1; // null terminator

    size_t input_buffer_size = *buffer_size;
    *buffer_size = required_size;
    if (buffer == NULL || input_buffer_size < required_size)
    {
        free(fxr_path);
        return HostApiBufferTooSmall;
    }

    memcpy(buffer, fxr_path, len * sizeof(char_t));
    buffer[len] = _X('\0');
    free(fxr_path);
    return Success;
}

NETHOST_API int NETHOST_CALLTYPE get_hostfxr_path(
    char_t* buffer,
    size_t* buffer_size,
    const struct get_hostfxr_parameters* parameters)
{
    if (buffer_size == NULL)
        return InvalidArgFailure;

    trace_setup();

    // Swallow traces for the duration of the call and restore the previous error
    // writer before returning (equivalent to the C++ error_writer_scope_t).
    trace_error_writer_fn previous_writer = trace_set_error_writer(no_trace);

    int rc = get_hostfxr_path_internal(buffer, buffer_size, parameters);

    trace_set_error_writer(previous_writer);
    return rc;
}
