// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef CLR_H
#define CLR_H

#include "pal.h"
#include "trace.h"

namespace coreclr
{
    typedef void* host_handle_t;
    typedef unsigned int domain_id_t;

    bool bind(const pal::string_t& libcoreclr_path);

    void unload();

    pal::hresult_t initialize(
        const char* exe_path,
        const char* app_domain_friendly_name,
        const char** property_keys,
        const char** property_values,
        int property_count,
        host_handle_t* host_handle,
        domain_id_t* domain_id);

    pal::hresult_t shutdown(host_handle_t host_handle, domain_id_t domain_id, int* latchedExitCode);

    pal::hresult_t execute_assembly(
        host_handle_t host_handle,
        domain_id_t domain_id,
        int argc,
        const char** argv,
        const char* managed_assembly_path,
        unsigned int* exit_code);
};

#endif
