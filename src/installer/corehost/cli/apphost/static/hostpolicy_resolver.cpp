// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cassert>
#include <error_codes.h>
#include <fx_definition.h>
#include "hostpolicy_resolver.h"
#include <pal.h>
#include <trace.h>

extern "C"
{
    int HOSTPOLICY_CALLTYPE corehost_load(const host_interface_t* init);
    int HOSTPOLICY_CALLTYPE corehost_unload();
    corehost_error_writer_fn HOSTPOLICY_CALLTYPE corehost_set_error_writer(corehost_error_writer_fn error_writer);
    int HOSTPOLICY_CALLTYPE corehost_initialize(const corehost_initialize_request_t *init_request, uint32_t options, /*out*/ corehost_context_contract *context_contract);
    int HOSTPOLICY_CALLTYPE corehost_main(const int argc, const pal::char_t* argv[]);
    int HOSTPOLICY_CALLTYPE corehost_main_with_output_buffer(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size);
}

int hostpolicy_resolver::load(
    const pal::string_t& lib_dir,
    pal::dll_t* dll,
    hostpolicy_contract_t &hostpolicy_contract)
{
    trace::info(_X("Using internal hostpolicy"));

    hostpolicy_contract.load = corehost_load;
    hostpolicy_contract.unload = corehost_unload;
    hostpolicy_contract.set_error_writer = corehost_set_error_writer;
    hostpolicy_contract.initialize = corehost_initialize;
    hostpolicy_contract.corehost_main = corehost_main;
    hostpolicy_contract.corehost_main_with_output_buffer = corehost_main_with_output_buffer;

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
    // static apphost is not supposed to be used in a framework-dependent app
    assert(!get_app(fx_definitions).get_runtime_config().get_is_framework_dependent());
    assert(mode == host_mode_t::apphost);

    impl_dir->assign(dotnet_root);
    return true;
}
