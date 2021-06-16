// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOSTPOLICY_RESOLVER_H__
#define __HOSTPOLICY_RESOLVER_H__

#include <fx_definition.h>
#include <host_interface.h>
#include <error_codes.h>
#include <corehost_context_contract.h>
#include <hostpolicy.h>

struct hostpolicy_contract_t
{
    // Required API contracts
    corehost_load_fn load;
    corehost_unload_fn unload;

    // 3.0+ contracts
    corehost_set_error_writer_fn set_error_writer;
    corehost_initialize_fn initialize;

    // 5.0+ contracts
    corehost_main_fn corehost_main;
    corehost_main_with_output_buffer_fn corehost_main_with_output_buffer;
};

namespace hostpolicy_resolver
{
    int load(
        const pal::string_t& lib_dir,
        pal::dll_t* dll,
        hostpolicy_contract_t &hostpolicy_contract);
    bool try_get_dir(
        host_mode_t mode,
        const pal::string_t& dotnet_root,
        const fx_definition_vector_t& fx_definitions,
        const pal::string_t& app_candidate,
        const pal::string_t& specified_deps_file,
        const std::vector<pal::string_t>& probe_realpaths,
        pal::string_t* impl_dir);
};

#endif // __HOSTPOLICY_RESOLVER_H__
