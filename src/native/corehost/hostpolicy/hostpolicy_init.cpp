// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "hostpolicy_init.h"
#include <trace.h>
#include "bundle/runner.h"

void make_palstr_arr(size_t argc, const pal::char_t** argv, std::vector<pal::string_t>* out)
{
    out->reserve(argc);
    for (size_t i = 0; i < argc; ++i)
    {
        out->push_back(argv[i]);
    }
}

bool hostpolicy_init_t::init(host_interface_t* input, hostpolicy_init_t* init)
{
    // Check if there are any breaking changes.
    if (input->version_hi != HOST_INTERFACE_LAYOUT_VERSION_HI)
    {
        trace::error(_X("The version of the data layout used to initialize %s is [0x%04zx]; expected version [0x%04x]"), LIBHOSTPOLICY_NAME, input->version_hi, HOST_INTERFACE_LAYOUT_VERSION_HI);
        return false;
    }

    trace::verbose(_X("Reading from host interface version: [0x%04zx:%zd] to initialize policy version: [0x%04x:%d]"), input->version_hi, input->version_lo, HOST_INTERFACE_LAYOUT_VERSION_HI, HOST_INTERFACE_LAYOUT_VERSION_LO);

    // This check is to ensure is an old hostfxr can still load new hostpolicy.
    // We should not read garbage due to potentially shorter struct size

    pal::string_t fx_requested_ver;

    if (input->version_lo >= offsetof(host_interface_t, host_mode) + sizeof(input->host_mode))
    {
        make_palstr_arr(input->config_keys.len, input->config_keys.arr, &init->cfg_keys);
        make_palstr_arr(input->config_values.len, input->config_values.arr, &init->cfg_values);

        init->deps_file = input->deps_file;
        init->is_framework_dependent = input->is_framework_dependent;

        make_palstr_arr(input->probe_paths.len, input->probe_paths.arr, &init->probe_paths);

        init->patch_roll_forward = input->patch_roll_forward;
        init->prerelease_roll_forward = input->prerelease_roll_forward;
        init->host_mode = static_cast<host_mode_t>(input->host_mode);
    }
    else
    {
        trace::error(_X("The size of the data layout used to initialize %s is %zd; expected at least %d"), LIBHOSTPOLICY_NAME, input->version_lo, 
            offsetof(host_interface_t, host_mode) + sizeof(input->host_mode));
    }

    // An old hostfxr may not provide these fields.
    // The version_lo (sizeof) the old hostfxr saw at build time will be
    // smaller and we should not attempt to read the fields in that case.
    if (input->version_lo >= offsetof(host_interface_t, tfm) + sizeof(input->tfm))
    {
        init->tfm = input->tfm;
    }
    
    if (input->version_lo >= offsetof(host_interface_t, fx_ver) + sizeof(input->fx_ver))
    {
        init->additional_deps_serialized = input->additional_deps_serialized;
        fx_requested_ver = input->fx_ver;
    }

    if (input->version_lo >= offsetof(host_interface_t, fx_names) + sizeof(input->fx_names))
    {
        size_t fx_count = input->fx_names.len;
        assert(fx_count > 0);
        assert(fx_count == input->fx_dirs.len);
        assert(fx_count == input->fx_requested_versions.len);
        assert(fx_count == input->fx_found_versions.len);

        std::vector<pal::string_t> fx_names;
        std::vector<pal::string_t> fx_dirs;
        std::vector<pal::string_t> fx_requested_versions;
        std::vector<pal::string_t> fx_found_versions;

        make_palstr_arr(input->fx_names.len, input->fx_names.arr, &fx_names);
        make_palstr_arr(input->fx_dirs.len, input->fx_dirs.arr, &fx_dirs);
        make_palstr_arr(input->fx_requested_versions.len, input->fx_requested_versions.arr, &fx_requested_versions);
        make_palstr_arr(input->fx_found_versions.len, input->fx_found_versions.arr, &fx_found_versions);

        init->fx_definitions.reserve(fx_count);
        for (size_t i = 0; i < fx_count; ++i)
        {
            auto fx = new fx_definition_t(fx_names[i], fx_dirs[i], fx_requested_versions[i], fx_found_versions[i]);
            init->fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));
        }
    }
    else
    {
        // Backward compat; create the fx_definitions[0] and [1] from the previous information
        init->fx_definitions.reserve(2);

        auto fx = new fx_definition_t();
        init->fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));

        if (init->is_framework_dependent)
        {
            pal::string_t fx_dir = input->fx_dir;
            pal::string_t fx_name = input->fx_name;

            // The found_ver was not passed previously, so obtain that from fx_dir
            pal::string_t fx_found_ver;
            size_t index = fx_dir.rfind(DIR_SEPARATOR);
            if (index != pal::string_t::npos)
            {
                fx_found_ver = fx_dir.substr(index + 1);
            }

            fx = new fx_definition_t(fx_name, fx_dir, fx_requested_ver, fx_found_ver);
            init->fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));
        }
    }

    // Initialize the host command
    init_host_command(input, init);

    if (input->version_lo >= offsetof(host_interface_t, host_info_host_path) + sizeof(input->host_info_host_path))
    {
        init->host_info.host_path = input->host_info_host_path;
        init->host_info.dotnet_root = input->host_info_dotnet_root;
        init->host_info.app_path = input->host_info_app_path;
        // For the backwards compat case, this will be later initialized with argv[0]
    }

    if (input->version_lo >= offsetof(host_interface_t, single_file_bundle_header_offset) + sizeof(input->single_file_bundle_header_offset))
    {
        if (input->single_file_bundle_header_offset != 0)
        {
            static bundle::runner_t bundle_runner(input->host_info_host_path, input->host_info_app_path, input->single_file_bundle_header_offset);
            bundle::info_t::the_app = &bundle_runner;
        }
    }

    return true;
}

void hostpolicy_init_t::init_host_command(host_interface_t* input, hostpolicy_init_t* init)
{
    if (input->version_lo >= offsetof(host_interface_t, host_command) + sizeof(input->host_command))
    {
        init->host_command = input->host_command;
    }
}
