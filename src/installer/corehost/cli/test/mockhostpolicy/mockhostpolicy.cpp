// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include "pal.h"
#include "trace.h"
#include "error_codes.h"
#include "host_interface.h"
#include <hostpolicy.h>

std::vector<char> tostr(const pal::char_t* value)
{
    std::vector<char> vect;
    pal::pal_utf8string(pal::string_t(value), &vect);
    return vect;
}

void print_strarr(const char* prefix, const strarr_t& arr)
{
    if (arr.len == 0)
    {
        std::cout << prefix << "<empty>" << std::endl;
        return;
    }

    for (size_t i = 0; i < arr.len; i++)
    {
        std::cout << prefix << tostr(arr.arr[i]).data() << std::endl;
    }
}

SHARED_API int HOSTPOLICY_CALLTYPE corehost_load(host_interface_t* init)
{
    trace::setup();

    trace::verbose(_X("--- Invoked hostpolicy mock - corehost_load"));

    std::cout << "--- Invoked hostpolicy mock - corehost_load" << std::endl;
    std::cout << "mock version: " << init->version_hi << " " << init->version_lo << std::endl;
    if (init->config_keys.len == 0)
    {
        std::cout << "mock config: <empty>" << std::endl;
    }
    else
    {
        for (size_t i = 0; i < init->config_keys.len; i++)
        {
            std::cout << "mock config: " << tostr(init->config_keys.arr[i]).data() << "=" << tostr(init->config_values.arr[i]).data() << std::endl;
        }
    }
    std::cout << "mock fx_dir: " << tostr(init->fx_dir).data() << std::endl;
    std::cout << "mock fx_name: " << tostr(init->fx_name).data() << std::endl;
    std::cout << "mock deps_file: " << tostr(init->deps_file).data() << std::endl;
    std::cout << "mock is_framework_dependent: " << init->is_framework_dependent << std::endl;
    print_strarr("mock probe_paths: ", init->probe_paths);
    std::cout << "mock host_mode: " << init->host_mode << std::endl;
    std::cout << "mock tfm: " << tostr(init->tfm).data() << std::endl;
    std::cout << "mock additional_deps_serialized: " << tostr(init->additional_deps_serialized).data() << std::endl;
    std::cout << "mock fx_ver: " << tostr(init->fx_ver).data() << std::endl;
    print_strarr("mock fx_names: ", init->fx_names);
    print_strarr("mock fx_dirs: ", init->fx_dirs);
    print_strarr("mock fx_requested_versions: ", init->fx_requested_versions);
    print_strarr("mock fx_found_versions: ", init->fx_found_versions);
    std::cout << "mock host_command:" << tostr(init->host_command).data() << std::endl;
    std::cout << "mock host_info_host_path:" << tostr(init->host_info_host_path).data() << std::endl;
    std::cout << "mock host_info_dotnet_root:" << tostr(init->host_info_dotnet_root).data() << std::endl;
    std::cout << "mock host_info_app_path:" << tostr(init->host_info_app_path).data() << std::endl;

    if (init->fx_names.len == 0)
    {
        std::cout << "mock frameworks: <empty>" << std::endl;
    }
    else
    {
        for (size_t i = 0; i < init->fx_names.len; i++)
        {
            std::cout << "mock frameworks: "
                << tostr(init->fx_names.arr[i]).data() << " "
                << tostr(init->fx_found_versions.arr[i]).data() << " [requested: "
                << tostr(init->fx_requested_versions.arr[i]).data() << "] [path: "
                << tostr(init->fx_dirs.arr[i]).data() << "]"
                << std::endl;
        }
    }

    return StatusCode::Success;
}

SHARED_API int HOSTPOLICY_CALLTYPE corehost_main(const int argc, const pal::char_t* argv[])
{
    trace::verbose(_X("--- Invoked hostpolicy mock - corehost_main"));
    return StatusCode::Success;
}

SHARED_API int HOSTPOLICY_CALLTYPE corehost_unload()
{
    trace::verbose(_X("--- Invoked hostpolicy mock - corehost_unload"));
    return StatusCode::Success;
}

SHARED_API corehost_error_writer_fn HOSTPOLICY_CALLTYPE corehost_set_error_writer(corehost_error_writer_fn error_writer)
{
    trace::verbose(_X("--- Invoked hostpolicy mock - corehost_set_error_writer"));
    return nullptr;
}

