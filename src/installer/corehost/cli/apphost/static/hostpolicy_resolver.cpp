// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cassert>
#include <error_codes.h>
#include <fx_definition.h>
#include "hostpolicy_resolver.h"
#include <pal.h>
#include <trace.h>

extern "C"
{
    int corehost_load(const host_interface_t* init);
    int corehost_unload();
    corehost_error_writer_fn corehost_set_error_writer(corehost_error_writer_fn error_writer);
    int corehost_initialize(const corehost_initialize_request_t *init_request, int32_t options, /*out*/ corehost_context_contract *context_contract);
}

int hostpolicy_resolver::load(
    const pal::string_t& lib_dir,
    pal::dll_t* dll,
    hostpolicy_contract_t &hostpolicy_contract)
{
    static hostpolicy_contract_t contract;

    trace::info(_X("Using internal hostpolicy"));

    contract.load = corehost_load;
    contract.unload = corehost_unload;
    contract.set_error_writer = corehost_set_error_writer;
    contract.initialize = corehost_initialize;

    hostpolicy_contract = contract;
    *dll = nullptr;

    return StatusCode::Success;
}

bool hostpolicy_resolver::try_get_dir(
    host_mode_t mode,
    const pal::string_t& dotnet_root,
    const fx_definition_vector_t& fx_definitions,
    const pal::string_t& app_candidate,
    const pal::string_t& specified_deps_file,
    const std::vector<pal::string_t>& probe_realpaths,
    pal::string_t* impl_dir)
{
    // Get the expected directory that would contain hostpolicy.
    pal::string_t expected;
    if (get_app(fx_definitions).get_runtime_config().get_is_framework_dependent())
    {
        // The hostpolicy is required to be in the root framework's location
        expected.assign(get_root_framework(fx_definitions).get_dir());
        assert(pal::directory_exists(expected));
    }
    else
    {
        // Native apps can be activated by muxer, native exe host or "corehost"
        // 1. When activated with dotnet.exe or corehost.exe, check for hostpolicy in the deps dir or
        //    app dir.
        // 2. When activated with native exe, the standalone host, check own directory.
        assert(mode != host_mode_t::invalid);
        switch (mode)
        {
        case host_mode_t::apphost:
        case host_mode_t::libhost:
            expected = dotnet_root;
            break;

        default:
            expected = get_directory(specified_deps_file.empty() ? app_candidate : specified_deps_file);
            break;
        }
    }

    // Assume the internal hostpolicy is in the expected location.
    impl_dir->assign(expected);
    return true;
}
