// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <fx_definition.h>
#include <host_interface.h>
#include <error_codes.h>

using corehost_load_fn = int(*) (const host_interface_t* init);
using corehost_unload_fn = int(*) ();
using corehost_error_writer_fn = void(*) (const pal::char_t* message);
using corehost_set_error_writer_fn = corehost_error_writer_fn(*) (corehost_error_writer_fn error_writer);

struct hostpolicy_contract
{
    // Required API contracts
    corehost_load_fn load;
    corehost_unload_fn unload;

    // 3.0+ contracts
    corehost_set_error_writer_fn set_error_writer;
};

namespace hostpolicy_resolver
{
    int load(
        const pal::string_t& lib_dir,
        pal::dll_t* h_host,
        hostpolicy_contract &host_contract);
    bool try_get_dir(
        host_mode_t mode,
        const pal::string_t& dotnet_root,
        const fx_definition_vector_t& fx_definitions,
        const pal::string_t& app_candidate,
        const pal::string_t& specified_deps_file,
        const std::vector<pal::string_t>& probe_realpaths,
        pal::string_t* impl_dir);
};
